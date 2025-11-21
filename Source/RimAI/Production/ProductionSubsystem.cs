using System.Collections.Generic;
using System.Linq;
using RimAI.Core;
using RimAI.Settings;
using RimWorld;
using Verse;

namespace RimAI.Production
{
    /// <summary>
    /// 생산 자동화 서브시스템
    /// 작업대 Bill을 자동으로 생성/조정/중단하여 완전 자동 플레이를 가능하게 합니다.
    /// </summary>
    public class ProductionSubsystem : RimAISubsystemBase
    {
        public override string Name => "Production";
        public override int Priority => 30; // 식량(50) 다음, 건설(20) 보다 높음

        // 맵별 상태 캐시
        private Dictionary<Map, ColonyProductionState> mapStates = new Dictionary<Map, ColonyProductionState>();

        // 마지막 Bill 변경 시간 (쿨다운)
        private Dictionary<Map, int> lastBillChangeTick = new Dictionary<Map, int>();
        private const int BILL_CHANGE_COOLDOWN = 3600; // 1분 (60초 * 60틱)

        public override void Initialize()
        {
            base.Initialize();
            baseUpdateInterval = 600; // 10초마다 체크
            updateInterval = baseUpdateInterval;
        }

        public override void Update(Map map)
        {
            if (!ShouldUpdate())
                return;

            // 맵 상태 분석 및 캐싱
            var state = ProductionAnalyzer.AnalyzeMap(map);
            mapStates[map] = state;

            LogDebug($"생산 상태: {state}");
        }

        public override List<RimAIAction> GetProposedActions(Map map)
        {
            var actions = new List<RimAIAction>();

            // 상태가 없으면 분석
            if (!mapStates.TryGetValue(map, out var state))
            {
                state = ProductionAnalyzer.AnalyzeMap(map);
                mapStates[map] = state;
            }

            // 개입 강도 체크
            var intensity = CurrentIntensity;
            if (intensity == AutomationIntensity.Off)
                return actions;

            // 쿨다운 체크
            if (!CanChangeBills(map))
            {
                LogDebug("생산 Bill 변경 쿨다운 중");
                return actions;
            }

            // 1. 긴급 생산 (무기, 의약품)
            AddEmergencyProductionActions(map, state, actions);

            // 2. 필수 생산 (갑옷, 겨울옷)
            AddEssentialProductionActions(map, state, actions);

            // 3. 품질 관리 (의류 교체, 고급 장비)
            if (intensity >= AutomationIntensity.High)
            {
                AddQualityManagementActions(map, state, actions);
            }

            // 4. 자원 관리 (Bill 중단/재개)
            ManageResourceAllocation(map, state, actions);

            return actions;
        }

        public override void ExecuteAction(RimAIAction action)
        {
            if (action.TargetMap == null)
                return;

            Map map = action.TargetMap;
            bool success = false;

            try
            {
                switch (action.Type)
                {
                    case RimAIActionType.CreateBill:
                        success = ExecuteCreateBill(map, action);
                        break;

                    case RimAIActionType.ModifyBill:
                        success = ExecuteModifyBill(map, action);
                        break;

                    case RimAIActionType.PauseBill:
                        success = ExecutePauseBill(map, action);
                        break;

                    case RimAIActionType.DeleteBill:
                        success = ExecuteDeleteBill(map, action);
                        break;
                }

                if (success)
                {
                    lastBillChangeTick[map] = Find.TickManager.TicksGame;
                    LogInfo($"✓ {action.Description}");
                }
            }
            catch (System.Exception ex)
            {
                LogError($"생산 액션 실행 중 오류: {ex.Message}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref lastBillChangeTick, "rimai_production_lastBillChange", LookMode.Reference, LookMode.Value);
        }

        public override string GetDebugInfo(Map map)
        {
            if (mapStates.TryGetValue(map, out var state))
            {
                return $"[Production] {state}";
            }
            return "[Production] No data";
        }

        public override void CleanupMap(Map map)
        {
            mapStates.Remove(map);
            lastBillChangeTick.Remove(map);
            LogInfo($"맵 {map.Index} 데이터 정리 완료");
        }

        // === 액션 생성 로직 ===

        /// <summary>
        /// 긴급 생산 액션 (무기, 의약품)
        /// </summary>
        private void AddEmergencyProductionActions(Map map, ColonyProductionState state, List<RimAIAction> actions)
        {
            // 1. 무기 부족 (Critical)
            if (state.NeedMoreWeapons())
            {
                int needed = state.ColonistCount - state.ColonistsWithWeapons;
                AddWeaponProductionAction(map, state, actions, needed, RimAIActionPriority.Critical);
            }

            // 2. 의약품 부족 (High)
            int medicineTarget = state.ColonistCount * 10;
            if (state.Medicine < medicineTarget)
            {
                AddMedicineProductionAction(map, state, actions, medicineTarget, RimAIActionPriority.High);
            }
        }

        /// <summary>
        /// 필수 생산 액션 (갑옷, 겨울옷)
        /// </summary>
        private void AddEssentialProductionActions(Map map, ColonyProductionState state, List<RimAIAction> actions)
        {
            // 개입 강도 Medium 이상부터 Bill 생성 가능
            if (CurrentIntensity < AutomationIntensity.Medium)
                return;

            // 1. 갑옷 부족 (High)
            if (state.NeedMoreArmor())
            {
                int needed = (state.ColonistCount / 2) - state.ColonistsWithArmor;
                AddArmorProductionAction(map, state, actions, needed, RimAIActionPriority.High);
            }

            // 2. 겨울 준비 (High)
            if (state.NeedWinterPreparation())
            {
                int needed = state.ColonistCount - state.WinterClothes;
                AddWinterClothesAction(map, state, actions, needed, RimAIActionPriority.High);
            }

            // 3. 낡은 옷 교체 (Normal)
            if (state.TatteredClothes > state.ColonistCount / 2)
            {
                AddClothingReplacementAction(map, state, actions, RimAIActionPriority.Normal);
            }
        }

        /// <summary>
        /// 품질 관리 액션 (고급 장비)
        /// </summary>
        private void AddQualityManagementActions(Map map, ColonyProductionState state, List<RimAIAction> actions)
        {
            // 자원 여유가 있을 때만
            if (!state.HasResourceSurplus())
                return;

            // 플레이 스타일에 따라 다른 품질 관리
            var playStyle = Settings.playStyle;

            if (playStyle == RimAIPlayStyle.Fortress)
            {
                // 고급 방어구 생산
                AddQualityArmorAction(map, state, actions, RimAIActionPriority.Low);
            }
            else if (playStyle == RimAIPlayStyle.Researcher)
            {
                // 고급 무기/컴포넌트 생산
                AddQualityWeaponAction(map, state, actions, RimAIActionPriority.Low);
            }
        }

        /// <summary>
        /// 자원 관리 (Bill 중단/재개)
        /// </summary>
        private void ManageResourceAllocation(Map map, ColonyProductionState state, List<RimAIAction> actions)
        {
            // 자원 부족 시 비필수 Bill 중단
            if (state.IsResourceScarce())
            {
                foreach (var workTable in state.WorkTables)
                {
                    foreach (var bill in workTable.Bills)
                    {
                        if (bill.suspended)
                            continue;

                        // 비필수 Bill인지 확인
                        if (IsNonEssentialBill(bill))
                        {
                            var action = CreatePauseBillAction(map, workTable.Building, bill, "자원 부족으로 중단");
                            action.Priority = RimAIActionPriority.Normal;
                            actions.Add(action);
                        }
                    }
                }
            }
            // 자원 여유 시 중단된 Bill 재개
            else if (!state.IsResourceScarce())
            {
                foreach (var workTable in state.WorkTables)
                {
                    foreach (var bill in workTable.Bills)
                    {
                        if (!bill.suspended)
                            continue;

                        var action = CreateResumeBillAction(map, workTable.Building, bill, "자원 확보로 재개");
                        action.Priority = RimAIActionPriority.Low;
                        actions.Add(action);
                    }
                }
            }
        }

        // === 개별 생산 액션 생성 ===

        private void AddWeaponProductionAction(Map map, ColonyProductionState state, List<RimAIAction> actions, int count, RimAIActionPriority priority)
        {
            // 무기 레시피 찾기 (활, 리볼버, 볼트액션 라이플 등)
            var weaponRecipes = new List<string> { "Make_Bow_Short", "Make_Gun_Revolver", "Make_Gun_BoltActionRifle" };

            foreach (var recipeName in weaponRecipes)
            {
                var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(recipeName);
                if (recipe == null || !ProductionAnalyzer.IsRecipeAvailable(recipe))
                    continue;

                var workTable = ProductionAnalyzer.FindWorkTableForRecipe(state, recipe);
                if (workTable == null)
                    continue;

                // 이미 Bill이 있으면 패스
                if (workTable.HasBillForRecipe(recipe))
                    continue;

                var action = CreateBillAction(map, workTable.Building, recipe, count);
                action.Priority = priority;
                action.Description = $"무기 {count}개 생산 ({recipe.label})";
                actions.Add(action);

                StoryLogger.Production.WeaponShortage(count);
                break; // 한 종류만 생산
            }
        }

        private void AddArmorProductionAction(Map map, ColonyProductionState state, List<RimAIAction> actions, int count, RimAIActionPriority priority)
        {
            // 플레이 스타일에 따라 배수 조정
            if (Settings.playStyle == RimAIPlayStyle.Fortress)
                count = (int)(count * 2f); // 요새: 2배

            var armorRecipes = new List<string> { "Make_Apparel_FlakVest", "Make_Apparel_PlateArmor" };

            foreach (var recipeName in armorRecipes)
            {
                var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(recipeName);
                if (recipe == null || !ProductionAnalyzer.IsRecipeAvailable(recipe))
                    continue;

                var workTable = ProductionAnalyzer.FindWorkTableForRecipe(state, recipe);
                if (workTable == null)
                    continue;

                if (workTable.HasBillForRecipe(recipe))
                    continue;

                var action = CreateBillAction(map, workTable.Building, recipe, count);
                action.Priority = priority;
                action.Description = $"갑옷 {count}벌 생산 ({recipe.label})";
                actions.Add(action);

                StoryLogger.Production.ArmorProduction(count);
                break;
            }
        }

        private void AddWinterClothesAction(Map map, ColonyProductionState state, List<RimAIAction> actions, int count, RimAIActionPriority priority)
        {
            var winterRecipes = new List<string> { "Make_Apparel_Parka", "Make_Apparel_Tuque" };

            foreach (var recipeName in winterRecipes)
            {
                var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(recipeName);
                if (recipe == null || !ProductionAnalyzer.IsRecipeAvailable(recipe))
                    continue;

                var workTable = ProductionAnalyzer.FindWorkTableForRecipe(state, recipe);
                if (workTable == null)
                    continue;

                if (workTable.HasBillForRecipe(recipe))
                    continue;

                var action = CreateBillAction(map, workTable.Building, recipe, count);
                action.Priority = priority;
                action.Description = $"겨울옷 {count}벌 생산 ({recipe.label})";
                actions.Add(action);

                StoryLogger.Production.WinterPreparation(state.DaysUntilWinter);
                break;
            }
        }

        private void AddMedicineProductionAction(Map map, ColonyProductionState state, List<RimAIAction> actions, int target, RimAIActionPriority priority)
        {
            var medicineRecipe = DefDatabase<RecipeDef>.GetNamedSilentFail("Make_MedicineIndustrial");
            if (medicineRecipe == null || !ProductionAnalyzer.IsRecipeAvailable(medicineRecipe))
                return;

            var workTable = ProductionAnalyzer.FindWorkTableForRecipe(state, medicineRecipe);
            if (workTable == null)
                return;

            // 이미 Bill이 있으면 목표 수량만 조정
            var existingBill = workTable.FindBill(medicineRecipe);
            if (existingBill != null && existingBill is Bill_Production productionBill)
            {
                if (productionBill.targetCount < target)
                {
                    var action = CreateModifyBillAction(map, workTable.Building, productionBill, target);
                    action.Priority = priority;
                    action.Description = $"의약품 목표 수량 조정 ({productionBill.targetCount} → {target})";
                    actions.Add(action);
                }
            }
            else
            {
                var action = CreateBillAction(map, workTable.Building, medicineRecipe, target);
                action.Priority = priority;
                action.Description = $"의약품 {target}개 생산";
                actions.Add(action);

                StoryLogger.Production.MedicineProduction(target);
            }
        }

        private void AddClothingReplacementAction(Map map, ColonyProductionState state, List<RimAIAction> actions, RimAIActionPriority priority)
        {
            var clothingRecipes = new List<string> { "Make_Apparel_Pants", "Make_Apparel_BasicShirt" };

            foreach (var recipeName in clothingRecipes)
            {
                var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(recipeName);
                if (recipe == null || !ProductionAnalyzer.IsRecipeAvailable(recipe))
                    continue;

                var workTable = ProductionAnalyzer.FindWorkTableForRecipe(state, recipe);
                if (workTable == null)
                    continue;

                if (workTable.HasBillForRecipe(recipe))
                    continue;

                int count = state.TatteredClothes / 2; // 낡은 옷 절반만 교체
                var action = CreateBillAction(map, workTable.Building, recipe, count);
                action.Priority = priority;
                action.Description = $"의류 교체 {count}벌 ({recipe.label})";
                actions.Add(action);

                StoryLogger.Production.ClothingReplacement();
                break;
            }
        }

        private void AddQualityArmorAction(Map map, ColonyProductionState state, List<RimAIAction> actions, RimAIActionPriority priority)
        {
            // 고급 갑옷 생산 (좋은 품질 이상)
            var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail("Make_Apparel_PlateArmor");
            if (recipe == null || !ProductionAnalyzer.IsRecipeAvailable(recipe))
                return;

            var workTable = ProductionAnalyzer.FindWorkTableForRecipe(state, recipe);
            if (workTable == null)
                return;

            int count = 2; // 고품질은 소량
            var action = CreateBillAction(map, workTable.Building, recipe, count, QualityCategory.Good);
            action.Priority = priority;
            action.Description = $"고급 갑옷 {count}벌 생산 (품질: 좋음 이상)";
            actions.Add(action);

            StoryLogger.Production.QualityUpgrade("갑옷");
        }

        private void AddQualityWeaponAction(Map map, ColonyProductionState state, List<RimAIAction> actions, RimAIActionPriority priority)
        {
            // 고급 무기 생산
            var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail("Make_Gun_AssaultRifle");
            if (recipe == null || !ProductionAnalyzer.IsRecipeAvailable(recipe))
                return;

            var workTable = ProductionAnalyzer.FindWorkTableForRecipe(state, recipe);
            if (workTable == null)
                return;

            int count = 2;
            var action = CreateBillAction(map, workTable.Building, recipe, count, QualityCategory.Good);
            action.Priority = priority;
            action.Description = $"고급 무기 {count}개 생산 (품질: 좋음 이상)";
            actions.Add(action);

            StoryLogger.Production.QualityUpgrade("무기");
        }

        // === 액션 생성 헬퍼 ===

        private RimAIAction CreateBillAction(Map map, Building_WorkTable workTable, RecipeDef recipe, int targetCount, QualityCategory minQuality = QualityCategory.Normal)
        {
            var data = new ProductionActionData
            {
                WorkTable = workTable,
                Recipe = recipe,
                TargetCount = targetCount,
                MinQuality = minQuality,
                RepeatMode = BillRepeatModeDefOf.TargetCount
            };

            return new RimAIAction
            {
                Type = RimAIActionType.CreateBill,
                SourceSubsystem = Name,
                TargetMap = map,
                CustomData = data,
                Description = $"Bill 생성: {recipe.label} x{targetCount}"
            };
        }

        private RimAIAction CreateModifyBillAction(Map map, Building_WorkTable workTable, Bill existingBill, int newTargetCount)
        {
            var data = new ProductionActionData
            {
                WorkTable = workTable,
                ExistingBill = existingBill,
                TargetCount = newTargetCount
            };

            return new RimAIAction
            {
                Type = RimAIActionType.ModifyBill,
                SourceSubsystem = Name,
                TargetMap = map,
                CustomData = data,
                Description = $"Bill 수정: {existingBill.LabelCap}"
            };
        }

        private RimAIAction CreatePauseBillAction(Map map, Building_WorkTable workTable, Bill bill, string reason)
        {
            var data = new ProductionActionData
            {
                WorkTable = workTable,
                ExistingBill = bill,
                Paused = true
            };

            return new RimAIAction
            {
                Type = RimAIActionType.PauseBill,
                SourceSubsystem = Name,
                TargetMap = map,
                CustomData = data,
                Description = $"Bill 중단: {bill.LabelCap} ({reason})"
            };
        }

        private RimAIAction CreateResumeBillAction(Map map, Building_WorkTable workTable, Bill bill, string reason)
        {
            var data = new ProductionActionData
            {
                WorkTable = workTable,
                ExistingBill = bill,
                Paused = false
            };

            return new RimAIAction
            {
                Type = RimAIActionType.PauseBill,
                SourceSubsystem = Name,
                TargetMap = map,
                CustomData = data,
                Description = $"Bill 재개: {bill.LabelCap} ({reason})"
            };
        }

        // === 액션 실행 로직 ===

        private bool ExecuteCreateBill(Map map, RimAIAction action)
        {
            if (!(action.CustomData is ProductionActionData data))
            {
                LogError("ProductionActionData가 없습니다");
                return false;
            }

            if (data.WorkTable == null || data.Recipe == null)
            {
                LogError("작업대 또는 레시피가 null입니다");
                return false;
            }

            // 1. 작업대에 Bill이 너무 많으면 생성 안 함 (성능 보호)
            if (BillManager.HasTooManyBills(data.WorkTable, 10))
            {
                LogWarning($"{data.WorkTable.def.label}에 Bill이 너무 많습니다");
                return false;
            }

            // 2. 이미 같은 레시피의 Bill이 있으면 생성 안 함
            var existingBill = BillManager.FindBill(data.WorkTable, data.Recipe);
            if (existingBill != null)
            {
                LogDetailed($"이미 {data.Recipe.label} Bill이 있습니다");
                return false;
            }

            // 3. 자원 가용성 체크
            if (!BillManager.HasSufficientResources(map, data.Recipe, data.TargetCount))
            {
                LogWarning($"{data.Recipe.label} 생산에 필요한 자원이 부족합니다");
                return false;
            }

            // 4. 재질 선택 (필요 시)
            ThingDef? stuffDef = data.StuffDef ?? BillManager.SelectBestStuff(map, data.Recipe);

            // 5. Bill 생성
            var result = BillManager.CreateBill(
                data.WorkTable,
                data.Recipe,
                data.TargetCount,
                data.MinQuality,
                stuffDef
            );

            if (result.Success)
            {
                LogInfo($"✓ Bill 생성: {data.Recipe.label} x{data.TargetCount}");
                return true;
            }
            else
            {
                LogError($"Bill 생성 실패: {result.ErrorMessage}");
                return false;
            }
        }

        private bool ExecuteModifyBill(Map map, RimAIAction action)
        {
            if (!(action.CustomData is ProductionActionData data))
            {
                LogError("ProductionActionData가 없습니다");
                return false;
            }

            if (data.ExistingBill == null)
            {
                LogError("ExistingBill이 null입니다");
                return false;
            }

            var result = BillManager.ModifyBill(data.ExistingBill, data.TargetCount);

            if (result.Success)
            {
                LogInfo($"✓ Bill 수정: {data.ExistingBill.LabelCap} → 목표 {data.TargetCount}");
                return true;
            }
            else
            {
                LogError($"Bill 수정 실패: {result.ErrorMessage}");
                return false;
            }
        }

        private bool ExecutePauseBill(Map map, RimAIAction action)
        {
            if (!(action.CustomData is ProductionActionData data))
            {
                LogError("ProductionActionData가 없습니다");
                return false;
            }

            if (data.ExistingBill == null)
            {
                LogError("ExistingBill이 null입니다");
                return false;
            }

            var result = BillManager.PauseBill(data.ExistingBill, data.Paused);

            if (result.Success)
            {
                string status = data.Paused ? "중단" : "재개";
                LogInfo($"✓ Bill {status}: {data.ExistingBill.LabelCap}");

                // 스토리 로그
                if (data.Paused)
                {
                    StoryLogger.Production.PausedDueToResources(data.ExistingBill.LabelCap);
                }
                else
                {
                    StoryLogger.Production.ResumedProduction(data.ExistingBill.LabelCap);
                }

                return true;
            }
            else
            {
                LogError($"Bill 중단/재개 실패: {result.ErrorMessage}");
                return false;
            }
        }

        private bool ExecuteDeleteBill(Map map, RimAIAction action)
        {
            if (!(action.CustomData is ProductionActionData data))
            {
                LogError("ProductionActionData가 없습니다");
                return false;
            }

            if (data.WorkTable == null || data.ExistingBill == null)
            {
                LogError("작업대 또는 ExistingBill이 null입니다");
                return false;
            }

            // 개입 강도 Full일 때만 삭제 가능
            if (CurrentIntensity != AutomationIntensity.Full)
            {
                LogWarning("개입 강도 Full이 아니면 Bill 삭제 불가");
                return false;
            }

            var result = BillManager.DeleteBill(data.WorkTable, data.ExistingBill);

            if (result.Success)
            {
                LogInfo($"✓ Bill 삭제: {data.ExistingBill.LabelCap}");
                return true;
            }
            else
            {
                LogError($"Bill 삭제 실패: {result.ErrorMessage}");
                return false;
            }
        }

        // === 헬퍼 메서드 ===

        private bool CanChangeBills(Map map)
        {
            if (!lastBillChangeTick.TryGetValue(map, out int lastTick))
                return true;

            int currentTick = Find.TickManager.TicksGame;
            return (currentTick - lastTick) >= BILL_CHANGE_COOLDOWN;
        }

        private bool IsNonEssentialBill(Bill bill)
        {
            if (bill.recipe == null)
                return true;

            // 무기, 갑옷, 의약품은 필수
            string label = bill.recipe.label.ToLower();
            if (label.Contains("weapon") || label.Contains("gun") || label.Contains("rifle"))
                return false;
            if (label.Contains("armor") || label.Contains("vest"))
                return false;
            if (label.Contains("medicine"))
                return false;

            // 나머지는 비필수
            return true;
        }
    }
}
