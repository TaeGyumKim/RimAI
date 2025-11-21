using System.Collections.Generic;
using RimAI.Core;
using RimWorld;
using Verse;

namespace RimAI.Construction
{
    /// <summary>
    /// 건설/확장 자동화 서브시스템
    /// 기본 인프라, 거주 시설, 방어 구조물을 자동으로 건설합니다.
    /// </summary>
    public class ConstructionSubsystem : RimAISubsystemBase
    {
        // 쿨다운 관리 (맵별)
        private Dictionary<Map, int> lastConstructionTick = new Dictionary<Map, int>();
        private const int CONSTRUCTION_COOLDOWN = 60000; // 1000초 (약 16분) - 큰 변화는 드물게

        // 맵별 상태 캐시
        private Dictionary<Map, ColonyConstructionState> mapStates = new Dictionary<Map, ColonyConstructionState>();

        public override string Name => "Construction";
        public override int Priority => 50; // 중간 우선순위

        public ConstructionSubsystem()
        {
            baseUpdateInterval = 600; // 10초마다 업데이트
        }

        public override void Initialize()
        {
            base.Initialize();
            // 설정은 베이스 클래스에서 자동 처리됨
        }

        public override void Update(Map map)
        {
            if (!ShouldUpdate()) return;
            if (!Enabled) return;

            try
            {
                // 상태 분석
                var state = ConstructionAnalyzer.AnalyzeMap(map);
                mapStates[map] = state;

                // 디버그 로그
                LogDetailed($"{state}");
            }
            catch (System.Exception ex)
            {
                LogError($"맵 {map} 업데이트 중 오류: {ex}");
            }
        }

        public override List<RimAIAction> GetProposedActions(Map map)
        {
            var actions = new List<RimAIAction>();

            if (!mapStates.TryGetValue(map, out var state))
                return actions;

            // 쿨다운 체크 - 너무 자주 건설하지 않음
            if (!CanConstruct(map))
            {
                return actions;
            }

            // 1. 침대 부족 시 침대 건설 (최우선)
            if (state.NeedMoreBeds())
            {
                actions.Add(new RimAIAction
                {
                    Type = RimAIActionType.ConstructBuilding,
                    Priority = RimAIActionPriority.High,
                    SourceSubsystem = Name,
                    TargetMap = map,
                    TargetThingDef = ThingDefOf.Bed, // 간단한 침대
                    Description = $"침대 부족 ({state.TotalBeds}/{state.ColonistCount}) - 침대 건설 필요"
                });
            }

            // 2. 기본 인프라 부족
            if (state.NeedBasicInfrastructure())
            {
                if (!state.HasKitchen)
                {
                    actions.Add(new RimAIAction
                    {
                        Type = RimAIActionType.ConstructBuilding,
                        Priority = RimAIActionPriority.High,
                        SourceSubsystem = Name,
                        TargetMap = map,
                        Description = "주방 없음 - 요리대 건설 필요"
                    });
                }

                if (!state.HasStorageRoom)
                {
                    actions.Add(new RimAIAction
                    {
                        Type = RimAIActionType.Other,
                        Priority = RimAIActionPriority.Normal,
                        SourceSubsystem = Name,
                        TargetMap = map,
                        Description = "창고 구역 없음 - 스톡파일 구역 생성 필요"
                    });
                }
            }

            // 3. 방어 시설 부족
            if (state.NeedDefenses() && state.ColonistCount >= 3)
            {
                actions.Add(new RimAIAction
                {
                    Type = RimAIActionType.ConstructBuilding,
                    Priority = RimAIActionPriority.Normal,
                    SourceSubsystem = Name,
                    TargetMap = map,
                    Description = $"방어 시설 부족 ({state.DefenseStructures}) - 샌드백 건설 권장"
                });
            }

            return actions;
        }

        public override void ExecuteAction(RimAIAction action)
        {
            if (action.TargetMap == null) return;

            Map map = action.TargetMap;
            bool success = false;

            try
            {
                // 액션 타입별 처리
                if (action.Type == RimAIActionType.ConstructBuilding)
                {
                    success = ExecuteConstructBuilding(map, action);
                }
                else if (action.Type == RimAIActionType.Other && action.Description.Contains("창고"))
                {
                    success = ExecuteCreateStorage(map, action);
                }

                // 성공 시 쿨다운 설정
                if (success)
                {
                    lastConstructionTick[map] = Find.TickManager.TicksGame;
                    LogInfo($"✓ {action.Description}");
                }
            }
            catch (System.Exception ex)
            {
                LogError($"건설 실행 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 건물 건설 실행
        /// </summary>
        private bool ExecuteConstructBuilding(Map map, RimAIAction action)
        {
            ThingDef buildingDef = action.TargetThingDef;

            // Def가 없으면 설명에서 추론
            if (buildingDef == null)
            {
                if (action.Description.Contains("침대"))
                    buildingDef = ThingDefOf.Bed;
                else if (action.Description.Contains("주방") || action.Description.Contains("요리"))
                    buildingDef = ThingDefOf.Stove;
                else if (action.Description.Contains("샌드백"))
                    buildingDef = ThingDefOf.Sandbags;
                else if (action.Description.Contains("바리케이드"))
                    buildingDef = ThingDefOf.Barricade;
                else
                    return false; // 알 수 없는 건물
            }

            // 플레이 스타일 반영
            int count = GetBuildCountByPlayStyle(buildingDef);

            int successCount = 0;
            for (int i = 0; i < count; i++)
            {
                // 위치 찾기
                BuildingType type = GetBuildingType(buildingDef);
                IntVec2 size = GetBuildingSize(buildingDef);

                IntVec3? location = BuildingPlacer.FindBestLocation(
                    map, buildingDef, size, type);

                if (location == null)
                {
                    LogWarning($"건설 위치를 찾을 수 없음: {buildingDef.label}");
                    continue;
                }

                // 청사진 배치
                BlueprintResult result = BlueprintExecutor.PlaceSingleBuilding(
                    map, buildingDef, location.Value);

                if (result.Success)
                {
                    successCount++;

                    // 스토리 로그
                    StoryLogger.Construction.InfrastructurePlan(buildingDef.label);
                }
                else
                {
                    LogWarning($"청사진 배치 실패: {result.ErrorMessage}");
                }
            }

            return successCount > 0;
        }

        /// <summary>
        /// 창고 구역 생성 실행
        /// </summary>
        private bool ExecuteCreateStorage(Map map, RimAIAction action)
        {
            // 위치 찾기 (넓은 공간)
            IntVec2 size = new IntVec2(5, 5);
            IntVec3? location = BuildingPlacer.FindBestLocation(
                map, null, size, BuildingType.Storage);

            if (location == null)
            {
                LogWarning("창고 위치를 찾을 수 없음");
                return false;
            }

            // 스톡파일 구역 생성
            BlueprintResult result = BlueprintExecutor.CreateStockpileZone(
                map, location.Value, size);

            if (result.Success)
            {
                StoryLogger.Construction.InfrastructurePlan("창고 구역");
                return true;
            }
            else
            {
                LogWarning($"창고 구역 생성 실패: {result.ErrorMessage}");
                return false;
            }
        }

        /// <summary>
        /// 건물 타입 추론
        /// </summary>
        private BuildingType GetBuildingType(ThingDef def)
        {
            if (def == ThingDefOf.Bed || def == ThingDefOf.DoubleBed)
                return BuildingType.Bedroom;
            else if (def == ThingDefOf.Table || def == ThingDefOf.TableShort)
                return BuildingType.DiningRoom;
            else if (def == ThingDefOf.Sandbags || def == ThingDefOf.Barricade)
                return BuildingType.Defense;
            else
                return BuildingType.Infrastructure;
        }

        /// <summary>
        /// 건물 크기 추론
        /// </summary>
        private IntVec2 GetBuildingSize(ThingDef def)
        {
            if (def.size.x > 0 && def.size.z > 0)
                return def.size;

            // 기본값
            return new IntVec2(1, 1);
        }

        /// <summary>
        /// 플레이 스타일에 따른 건설 개수
        /// </summary>
        private int GetBuildCountByPlayStyle(ThingDef def)
        {
            var style = RimAI_Mod.Settings.playStyle;

            // 침대
            if (def == ThingDefOf.Bed)
            {
                if (style == Settings.RimAIPlayStyle.Nomadic)
                    return 1; // 최소한만
                else
                    return 2; // 기본 2개
            }

            // 방어 시설
            if (def == ThingDefOf.Sandbags || def == ThingDefOf.Barricade)
            {
                if (style == Settings.RimAIPlayStyle.Fortress)
                    return 5; // 방어 중시
                else if (style == Settings.RimAIPlayStyle.Nomadic)
                    return 1; // 최소한
                else
                    return 3; // 기본
            }

            // 기타
            return 1;
        }

        /// <summary>
        /// 건설 가능 여부 확인 (쿨다운)
        /// </summary>
        private bool CanConstruct(Map map)
        {
            if (!lastConstructionTick.TryGetValue(map, out var lastTick))
            {
                return true; // 처음 건설
            }

            int currentTick = Find.TickManager.TicksGame;
            return (currentTick - lastTick) >= CONSTRUCTION_COOLDOWN;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref lastConstructionTick, "lastConstructionTick");
        }

        public override string GetDebugInfo(Map map)
        {
            if (!mapStates.TryGetValue(map, out var state))
                return base.GetDebugInfo(map);

            var info = $"[{Name}] {state}\n";

            if (lastConstructionTick.TryGetValue(map, out var lastTick))
            {
                int ticksSinceLastConstruction = Find.TickManager.TicksGame - lastTick;
                int secondsRemaining = (CONSTRUCTION_COOLDOWN - ticksSinceLastConstruction) / 60;
                info += $"쿨다운: {secondsRemaining}초 남음\n";
            }

            return info;
        }

        /// <summary>
        /// 맵 데이터 정리 (메모리 누수 방지)
        /// </summary>
        public override void CleanupMap(Map map)
        {
            if (map == null) return;

            mapStates.Remove(map);
            lastConstructionTick.Remove(map);

            LogInfo($"맵 {map.Index} 데이터 정리 완료");
        }
    }
}
