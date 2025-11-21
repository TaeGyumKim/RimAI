using System.Linq;
using RimWorld;
using Verse;

namespace RimAI.Construction
{
    /// <summary>
    /// 콜로니의 건설 상태를 분석
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

        private static void AnalyzeInfrastructure(Map map, ColonyConstructionState state)
        {
            // 주방 확인 (요리대)
            state.HasKitchen = map.listerBuildings.allBuildingsColonist
                .Any(b => b.def.building?.isMealSource == true);

            // 작업장 확인 (제작대)
            state.HasWorkshop = map.listerBuildings.allBuildingsColonist
                .Any(b => b.def.defName.Contains("TableMachining") ||
                          b.def.defName.Contains("Workbench"));

            // 연구대 확인
            state.HasResearchBench = map.listerBuildings.allBuildingsColonist
                .Any(b => b.def.defName.Contains("ResearchBench"));

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

        private static void AnalyzeResources(Map map, ColonyConstructionState state)
        {
            state.AvailableWood = 0;
            state.AvailableSteel = 0;
            state.AvailableStone = 0;

            // 자원 스택 분석
            var haulables = map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver);

            foreach (var thing in haulables)
            {
                if (thing.def.defName.Contains("Wood"))
                {
                    state.AvailableWood += thing.stackCount;
                }
                else if (thing.def.defName.Contains("Steel"))
                {
                    state.AvailableSteel += thing.stackCount;
                }
                else if (thing.def.IsStuff && thing.def.stuffProps?.categories?.Contains(StuffCategoryDefOf.Stony) == true)
                {
                    state.AvailableStone += thing.stackCount;
                }
            }
        }
    }
}
