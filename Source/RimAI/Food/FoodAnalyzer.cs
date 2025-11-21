using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimAI.Food
{
    /// <summary>
    /// 콜로니의 식량 상태를 분석하는 클래스
    /// </summary>
    public static class FoodAnalyzer
    {
        // 폰 하루 평균 영양분 소비량 (게임 내부 값 참조)
        private const float PAWN_DAILY_NUTRITION = 1.6f;
        private const float ANIMAL_DAILY_NUTRITION = 0.8f; // 평균값

        /// <summary>
        /// 특정 맵의 식량 상태를 분석
        /// </summary>
        public static ColonyFoodState AnalyzeMap(Map map)
        {
            var state = new ColonyFoodState();

            // 1. 식량 저장량 분석
            AnalyzeFoodStorage(map, state);

            // 2. 콜로니스트 및 동물 수 분석
            AnalyzePopulation(map, state);

            // 3. 소비량 및 생존 일수 계산
            CalculateConsumption(state);

            // 4. 농장 상태 분석
            AnalyzeFarms(map, state);

            // 5. 환경 정보 분석
            AnalyzeEnvironment(map, state);

            // 6. 현재 작업 중인 폰 분석
            AnalyzeWorkingPawns(map, state);

            return state;
        }

        /// <summary>
        /// 식량 저장량 분석
        /// </summary>
        private static void AnalyzeFoodStorage(Map map, ColonyFoodState state)
        {
            state.TotalNutrition = 0f;
            state.RawFoodNutrition = 0f;
            state.MealNutrition = 0f;
            state.CropNutrition = 0f;

            // 맵의 모든 식량 아이템 탐색
            var foodThings = map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSource);

            foreach (var thing in foodThings)
            {
                if (thing.def.IsIngestible)
                {
                    float nutrition = thing.GetStatValue(StatDefOf.Nutrition) * thing.stackCount;

                    state.TotalNutrition += nutrition;

                    // 식량 타입별 분류
                    if (thing.def.IsMeat || thing.def == ThingDefOf.Meat_Human)
                    {
                        state.RawFoodNutrition += nutrition;
                    }
                    else if (thing.def.IsCorpse)
                    {
                        // 시체는 영양분으로 간주하지 않음 (인육 제외)
                        state.TotalNutrition -= nutrition;
                    }
                    else if (thing.def.ingestible?.preferability >= FoodPreferability.MealAwful)
                    {
                        state.MealNutrition += nutrition;
                    }
                    else if (thing is Plant plant && plant.HarvestableNow)
                    {
                        state.CropNutrition += nutrition;
                    }
                }
            }
        }

        /// <summary>
        /// 콜로니스트 및 동물 수 분석
        /// </summary>
        private static void AnalyzePopulation(Map map, ColonyFoodState state)
        {
            state.ColonistCount = map.mapPawns.FreeColonistsSpawnedCount;

            // 식량을 소비하는 동물만 카운트
            state.AnimalCount = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer)
                .Count(p => p.RaceProps.Animal && p.RaceProps.EatsFood);
        }

        /// <summary>
        /// 소비량 및 생존 일수 계산
        /// </summary>
        private static void CalculateConsumption(ColonyFoodState state)
        {
            state.DailyConsumption =
                (state.ColonistCount * PAWN_DAILY_NUTRITION) +
                (state.AnimalCount * ANIMAL_DAILY_NUTRITION);

            if (state.DailyConsumption > 0f)
            {
                state.DaysUntilStarvation = state.TotalNutrition / state.DailyConsumption;
            }
            else
            {
                state.DaysUntilStarvation = 999f; // 소비자가 없으면 무한
            }
        }

        /// <summary>
        /// 농장 상태 분석
        /// </summary>
        private static void AnalyzeFarms(Map map, ColonyFoodState state)
        {
            state.GrowingZones.Clear();
            state.TotalFarmTiles = 0;
            state.SownTiles = 0;
            state.EmptyFarmTiles = 0;
            state.HarvestableCrops = 0;

            // 모든 농장 구역 찾기
            var zones = map.zoneManager.AllZones.OfType<Zone_Growing>();
            state.GrowingZones.AddRange(zones);

            foreach (var zone in zones)
            {
                foreach (var cell in zone.Cells)
                {
                    state.TotalFarmTiles++;

                    var plant = cell.GetPlant(map);

                    if (plant != null && zone.GetPlantDefToGrow() == plant.def)
                    {
                        state.SownTiles++;

                        if (plant.HarvestableNow)
                        {
                            state.HarvestableCrops++;
                        }
                    }
                    else if (plant == null || !plant.sown)
                    {
                        // 빈 타일 또는 잡초만 있는 타일
                        state.EmptyFarmTiles++;
                    }
                }
            }
        }

        /// <summary>
        /// 환경 정보 분석
        /// </summary>
        private static void AnalyzeEnvironment(Map map, ColonyFoodState state)
        {
            state.CurrentSeason = GenLocalDate.Season(map);
            state.Temperature = map.mapTemperature.OutdoorTemp;

            // 농사 가능 여부 판단
            state.CanGrow = CanGrowNow(map);

            // 겨울까지 남은 일수 계산
            state.DaysUntilWinter = CalculateDaysUntilWinter(map);
        }

        /// <summary>
        /// 현재 작업 중인 폰 분석
        /// </summary>
        private static void AnalyzeWorkingPawns(Map map, ColonyFoodState state)
        {
            state.PawnsSowing = 0;
            state.PawnsHarvesting = 0;
            state.PawnsCooking = 0;
            state.PawnsHunting = 0;

            foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
            {
                if (pawn.CurJob == null) continue;

                var jobDef = pawn.CurJob.def;

                if (jobDef == JobDefOf.Sow)
                    state.PawnsSowing++;
                else if (jobDef == JobDefOf.Harvest)
                    state.PawnsHarvesting++;
                else if (jobDef == JobDefOf.DoBill && IsCookingBill(pawn.CurJob))
                    state.PawnsCooking++;
                else if (jobDef == JobDefOf.Hunt)
                    state.PawnsHunting++;
            }
        }

        /// <summary>
        /// 현재 농사가 가능한지 판단
        /// </summary>
        private static bool CanGrowNow(Map map)
        {
            var season = GenLocalDate.Season(map);
            var temp = map.mapTemperature.OutdoorTemp;

            // 겨울이 아니고, 온도가 0도 이상이면 가능
            return season != Season.Winter && temp > 0f;
        }

        /// <summary>
        /// 겨울까지 남은 일수 계산
        /// </summary>
        private static int CalculateDaysUntilWinter(Map map)
        {
            var currentSeason = GenLocalDate.Season(map);
            var currentDay = GenLocalDate.DayOfSeason(map);
            const int daysPerSeason = 15; // RimWorld 기본값

            switch (currentSeason)
            {
                case Season.Spring:
                    return (daysPerSeason - currentDay) + daysPerSeason * 2; // 봄 남은일 + 여름 + 가을
                case Season.Summer:
                    return (daysPerSeason - currentDay) + daysPerSeason; // 여름 남은일 + 가을
                case Season.Fall:
                    return daysPerSeason - currentDay; // 가을 남은일
                case Season.Winter:
                    return 0; // 이미 겨울
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Job이 요리 작업인지 판단
        /// </summary>
        private static bool IsCookingBill(Job job)
        {
            if (job.bill == null) return false;

            var billDef = job.bill.recipe;
            return billDef != null && billDef.ProducedThingDef?.IsIngestible == true;
        }
    }
}
