using System.Linq;
using RimWorld;
using Verse;

namespace RimAI.Construction
{
    /// <summary>
    /// 콜로니의 건설 상태를 분석
    ///
    /// 성능 최적화:
    /// - 인프라 분석 단일 순회 (3번 → 1번)
    /// - ResourceCounter 사용 (O(1) 조회)
    /// - 조기 종료 추가
    /// </summary>
    public static class ConstructionAnalyzer
    {
        public static ColonyConstructionState AnalyzeMap(Map map)
        {
            var state = new ColonyConstructionState();

            // 콜로니스트 수
            state.ColonistCount = map.mapPawns.FreeColonistsSpawnedCount;

            // 침대 분석
            AnalyzeBeds(map, state);

            // 인프라 분석
            AnalyzeInfrastructure(map, state);

            // 방어 시설 분석
            AnalyzeDefenses(map, state);

            // 건설 작업 분석
            AnalyzeConstructions(map, state);

            // 자원 분석
            AnalyzeResources(map, state);

            return state;
        }

        private static void AnalyzeBeds(Map map, ColonyConstructionState state)
        {
            var beds = map.listerBuildings.AllBuildingsColonistOfClass<Building_Bed>();

            state.TotalBeds = 0;
            state.AssignedBeds = 0;

            foreach (var bed in beds)
            {
                if (bed.def.building.bed_humanlike)
                {
                    state.TotalBeds += bed.SleepingSlotsCount;

                    if (bed.OwnersForReading.Any())
                    {
                        state.AssignedBeds += bed.OwnersForReading.Count();
                    }
                }
            }
        }

        /// <summary>
        /// 인프라 분석 (최적화됨 - 단일 순회)
        /// </summary>
        private static void AnalyzeInfrastructure(Map map, ColonyConstructionState state)
        {
            // 초기화
            state.HasKitchen = false;
            state.HasWorkshop = false;
            state.HasResearchBench = false;

            // 단일 순회로 모든 건물 체크 (이전: 3번 순회 → 현재: 1번)
            foreach (var building in map.listerBuildings.allBuildingsColonist)
            {
                // 주방 확인 (요리대)
                if (!state.HasKitchen && building.def.building?.isMealSource == true)
                    state.HasKitchen = true;

                // 작업장 확인 (제작대)
                if (!state.HasWorkshop &&
                    (building.def.defName.Contains("TableMachining") ||
                     building.def.defName.Contains("Workbench")))
                    state.HasWorkshop = true;

                // 연구대 확인
                if (!state.HasResearchBench && building.def.defName.Contains("ResearchBench"))
                    state.HasResearchBench = true;

                // 조기 종료: 모두 찾았으면 더 이상 순회하지 않음
                if (state.HasKitchen && state.HasWorkshop && state.HasResearchBench)
                    break;
            }

            // 창고 확인 (스톡파일 구역)
            state.HasStorageRoom = map.zoneManager.AllZones
                .OfType<Zone_Stockpile>()
                .Any();
        }

        private static void AnalyzeDefenses(Map map, ColonyConstructionState state)
        {
            state.DefenseStructures = 0;
            state.Walls = 0;
            state.Doors = 0;
            state.Turrets = 0;

            foreach (var building in map.listerBuildings.allBuildingsColonist)
            {
                // 샌드백, 바리케이드
                if (building.def.defName.Contains("Sandbag") ||
                    building.def.defName.Contains("Barricade"))
                {
                    state.DefenseStructures++;
                }

                // 벽
                if (building.def.building?.isNaturalRock == false &&
                    building.def.graphicData?.linkFlags == LinkFlags.Wall)
                {
                    state.Walls++;
                }

                // 문
                if (building is Building_Door)
                {
                    state.Doors++;
                }

                // 터렛
                if (building.def.building?.IsTurret == true)
                {
                    state.Turrets++;
                }
            }
        }

        private static void AnalyzeConstructions(Map map, ColonyConstructionState state)
        {
            // 청사진
            state.ActiveBlueprints = map.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint).Count;

            // 건설 프레임
            state.ActiveConstructions = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingFrame).Count;
        }

        /// <summary>
        /// 자원 분석 (최적화됨 - ResourceCounter 사용)
        ///
        /// ResourceCounter는 내부적으로 자원 수를 관리하므로 O(1) 조회 가능
        /// 이전: HaulableEver 순회 (수천 개 아이템) → 현재: 직접 조회 (즉시)
        /// </summary>
        private static void AnalyzeResources(Map map, ColonyConstructionState state)
        {
            // ResourceCounter 사용 (O(1) 조회)
            state.AvailableWood = map.resourceCounter.GetCount(ThingDefOf.WoodLog);
            state.AvailableSteel = map.resourceCounter.GetCount(ThingDefOf.Steel);

            // 돌은 여러 종류가 있으므로 합산
            state.AvailableStone = 0;
            state.AvailableStone += map.resourceCounter.GetCount(ThingDefOf.BlocksGranite);
            state.AvailableStone += map.resourceCounter.GetCount(ThingDefOf.BlocksLimestone);
            state.AvailableStone += map.resourceCounter.GetCount(ThingDefOf.BlocksMarble);
            state.AvailableStone += map.resourceCounter.GetCount(ThingDefOf.BlocksSandstone);
            state.AvailableStone += map.resourceCounter.GetCount(ThingDefOf.BlocksSlate);
        }
    }
}
