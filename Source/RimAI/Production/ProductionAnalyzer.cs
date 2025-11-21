using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimAI.Production
{
    /// <summary>
    /// 콜로니의 생산 상태를 분석
    ///
    /// 성능 최적화:
    /// - ResourceCounter 사용 (O(1) 조회)
    /// - Lister 활용 (효율적인 순회)
    /// - 조기 종료 (필요한 정보만 수집)
    /// </summary>
    public static class ProductionAnalyzer
    {
        /// <summary>
        /// 맵의 생산 상태 분석
        /// </summary>
        public static ColonyProductionState AnalyzeMap(Map map)
        {
            var state = new ColonyProductionState();

            // 콜로니스트 수
            state.ColonistCount = map.mapPawns.FreeColonistsSpawnedCount;

            // 1. 작업대 스캔
            AnalyzeWorkTables(map, state);

            // 2. 무기/방어구/의류 재고 분석
            AnalyzeEquipment(map, state);

            // 3. 자원 재고 분석
            AnalyzeResources(map, state);

            // 4. 시즌 정보
            AnalyzeSeason(map, state);

            return state;
        }

        /// <summary>
        /// 작업대 분석
        /// </summary>
        private static void AnalyzeWorkTables(Map map, ColonyProductionState state)
        {
            var allBuildings = map.listerBuildings.allBuildingsColonist;

            foreach (var building in allBuildings)
            {
                // Building_WorkTable만 필터링
                if (!(building is Building_WorkTable workTable))
                    continue;

                var info = new WorkTableInfo
                {
                    Building = workTable,
                    DefName = workTable.def.defName
                };

                // Bill 목록 복사
                if (workTable.billStack != null && workTable.billStack.Bills != null)
                {
                    info.Bills = new List<Bill>(workTable.billStack.Bills);
                    info.QueuedWork = info.Bills.Count(b => !b.suspended);
                }

                // 전력 체크
                var powerComp = workTable.GetComp<CompPowerTrader>();
                if (powerComp != null)
                {
                    info.IsPowered = powerComp.PowerOn;
                }
                else
                {
                    info.IsPowered = true; // 전력 불필요
                }

                state.WorkTables.Add(info);
            }
        }

        /// <summary>
        /// 무기/방어구/의류 재고 분석
        /// </summary>
        private static void AnalyzeEquipment(Map map, ColonyProductionState state)
        {
            state.MeleeWeapons = 0;
            state.RangedWeapons = 0;
            state.ColonistsWithWeapons = 0;
            state.Armors = 0;
            state.ColonistsWithArmor = 0;
            state.WinterClothes = 0;
            state.SummerClothes = 0;
            state.TatteredClothes = 0;

            // 1. 콜로니스트 장비 분석
            var colonists = map.mapPawns.FreeColonists;
            foreach (var pawn in colonists)
            {
                if (pawn.Dead || pawn.Downed)
                    continue;

                // 무기 체크
                if (pawn.equipment?.Primary != null)
                {
                    state.ColonistsWithWeapons++;
                }

                // 갑옷 체크 (상의에 방어구가 있으면)
                var bodyArmor = pawn.apparel?.WornApparel
                    .FirstOrDefault(ap => ap.def.apparel.bodyPartGroups.Any(bpg =>
                        bpg.defName == "Torso"));

                if (bodyArmor != null && bodyArmor.def.StatBaseDef.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp) > 0.3f)
                {
                    state.ColonistsWithArmor++;
                }

                // 낡은 옷 체크 (50% 이하 내구도)
                if (pawn.apparel?.WornApparel != null)
                {
                    foreach (var apparel in pawn.apparel.WornApparel)
                    {
                        float hitPointsPct = (float)apparel.HitPoints / apparel.MaxHitPoints;
                        if (hitPointsPct < 0.5f)
                        {
                            state.TatteredClothes++;
                        }

                        // 겨울옷 체크 (방한 > 0.3)
                        if (apparel.def.StatBaseDef.GetStatValueAbstract(StatDefOf.Insulation_Cold) > 0.3f)
                        {
                            state.WinterClothes++;
                        }
                    }
                }
            }

            // 2. 창고 무기/방어구 재고 분석
            var weapons = map.listerThings.ThingsInGroup(ThingRequestGroup.Weapon)
                .Where(t => t.Faction == Faction.OfPlayer && !t.IsForbidden(Faction.OfPlayer));

            foreach (var weapon in weapons)
            {
                if (weapon.def.IsRangedWeapon)
                    state.RangedWeapons++;
                else if (weapon.def.IsMeleeWeapon)
                    state.MeleeWeapons++;
            }

            // 3. 창고 방어구 재고
            var apparels = map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel)
                .Where(t => t.Faction == Faction.OfPlayer && !t.IsForbidden(Faction.OfPlayer));

            foreach (var apparel in apparels)
            {
                // 갑옷 (방어력 > 0.3)
                if (apparel.def.StatBaseDef.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp) > 0.3f)
                {
                    state.Armors++;
                }

                // 겨울옷 체크
                if (apparel.def.StatBaseDef.GetStatValueAbstract(StatDefOf.Insulation_Cold) > 0.3f)
                {
                    state.WinterClothes++;
                }

                // 여름옷 체크 (방열 > 0.3)
                if (apparel.def.StatBaseDef.GetStatValueAbstract(StatDefOf.Insulation_Heat) > 0.3f)
                {
                    state.SummerClothes++;
                }
            }
        }

        /// <summary>
        /// 자원 재고 분석 (최적화: ResourceCounter 사용)
        /// </summary>
        private static void AnalyzeResources(Map map, ColonyProductionState state)
        {
            // ResourceCounter 사용 (O(1) 조회)
            state.AvailableSteel = map.resourceCounter.GetCount(ThingDefOf.Steel);
            state.AvailableComponents = map.resourceCounter.GetCount(ThingDefOf.ComponentIndustrial);

            // 천 (여러 종류 합산)
            state.AvailableCloth = 0;
            state.AvailableCloth += map.resourceCounter.GetCount(ThingDefOf.Cloth);
            // 다른 천 종류가 있다면 추가

            // 가죽 (여러 종류 합산)
            state.AvailableLeather = 0;
            var leatherDefs = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.IsLeather);
            foreach (var leatherDef in leatherDefs)
            {
                state.AvailableLeather += map.resourceCounter.GetCount(leatherDef);
            }

            // 의약품
            state.Medicine = map.resourceCounter.GetCount(ThingDefOf.MedicineIndustrial);
            state.Medicine += map.resourceCounter.GetCount(ThingDefOf.MedicineHerbal);
        }

        /// <summary>
        /// 시즌 정보 분석
        /// </summary>
        private static void AnalyzeSeason(Map map, ColonyProductionState state)
        {
            state.CurrentSeason = GenLocalDate.Season(map);

            // 겨울까지 남은 일수 계산
            int currentDay = GenLocalDate.DayOfYear(map);
            int winterStartDay = GetSeasonStartDay(Season.Winter, map);

            if (state.CurrentSeason == Season.Winter)
            {
                state.DaysUntilWinter = 0;
            }
            else
            {
                if (currentDay < winterStartDay)
                {
                    state.DaysUntilWinter = winterStartDay - currentDay;
                }
                else
                {
                    // 다음 해 겨울
                    int daysInYear = GenDate.DaysPerYear;
                    state.DaysUntilWinter = daysInYear - currentDay + winterStartDay;
                }
            }
        }

        /// <summary>
        /// 시즌 시작 일 계산 (근사치)
        /// </summary>
        private static int GetSeasonStartDay(Season season, Map map)
        {
            // RimWorld의 계절은 위도에 따라 다름
            // 간단하게 1년을 4등분으로 계산
            int daysInYear = GenDate.DaysPerYear;
            int daysPerSeason = daysInYear / 4;

            // 봄(0), 여름(1), 가을(2), 겨울(3)
            int seasonIndex = (int)season;

            // 남반구면 반대로
            if (map.Tile >= 0) // 북반구 (단순화)
            {
                return seasonIndex * daysPerSeason;
            }
            else // 남반구
            {
                seasonIndex = (seasonIndex + 2) % 4; // 6개월 차이
                return seasonIndex * daysPerSeason;
            }
        }

        /// <summary>
        /// 특정 레시피가 연구 완료되었는지 확인
        /// </summary>
        public static bool IsRecipeAvailable(RecipeDef recipe)
        {
            if (recipe == null)
                return false;

            // 연구 요구사항 체크
            if (recipe.researchPrerequisite != null)
            {
                return recipe.researchPrerequisite.IsFinished;
            }

            if (recipe.researchPrerequisites != null)
            {
                return recipe.researchPrerequisites.All(r => r.IsFinished);
            }

            // 연구 요구사항 없으면 사용 가능
            return true;
        }

        /// <summary>
        /// 특정 레시피를 만들 수 있는 작업대가 있는지 확인
        /// </summary>
        public static bool HasWorkTableForRecipe(ColonyProductionState state, RecipeDef recipe)
        {
            if (recipe?.AllRecipeUsers == null)
                return false;

            foreach (var workTableDef in recipe.AllRecipeUsers)
            {
                if (state.WorkTables.Any(wt => wt.DefName == workTableDef.defName))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 레시피를 만들 수 있는 작업대 찾기
        /// </summary>
        public static WorkTableInfo? FindWorkTableForRecipe(ColonyProductionState state, RecipeDef recipe)
        {
            if (recipe?.AllRecipeUsers == null)
                return null;

            foreach (var workTableDef in recipe.AllRecipeUsers)
            {
                var workTable = state.WorkTables.FirstOrDefault(wt => wt.DefName == workTableDef.defName);
                if (workTable != null && workTable.IsPowered)
                    return workTable;
            }

            // 전력 없는 작업대라도 반환
            foreach (var workTableDef in recipe.AllRecipeUsers)
            {
                var workTable = state.WorkTables.FirstOrDefault(wt => wt.DefName == workTableDef.defName);
                if (workTable != null)
                    return workTable;
            }

            return null;
        }
    }
}
