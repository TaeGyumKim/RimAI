using System.Collections.Generic;
using System.Linq;
using RimAI.Core;
using RimAI.Settings;
using RimWorld;
using Verse;

namespace RimAI.Zones
{
    /// <summary>
    /// 구역/지정 자동화 서브시스템
    /// 재배 구역, 채굴 지정, 벌목 지정 등을 자동으로 관리합니다.
    /// </summary>
    public class ZoneDesignationSubsystem : RimAISubsystemBase
    {
        private Dictionary<Map, int> lastZoneActionTick = new Dictionary<Map, int>();
        private const int ZONE_COOLDOWN = 300; // 5초

        public override string Name => "Zones";
        public override int Priority => 45;

        public ZoneDesignationSubsystem()
        {
            baseUpdateInterval = 120; // 2초마다 체크
        }

        public override void Update(Map map)
        {
            if (!ShouldUpdate()) return;
            if (!Enabled) return;

            try
            {
                // 쿨다운 체크
                if (!CanDoZoneAction(map)) return;

                // 1. 재배 구역 확인 및 생성
                CheckGrowingZones(map);

                // 2. 채굴 지정
                CheckMiningDesignations(map);

                // 3. 벌목 지정
                CheckChoppingDesignations(map);

                // 4. 사냥 지정
                CheckHuntingDesignations(map);
            }
            catch (System.Exception ex)
            {
                LogError($"구역/지정 업데이트 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 재배 구역 확인 및 생성
        /// </summary>
        private void CheckGrowingZones(Map map)
        {
            var existingZones = map.zoneManager.AllZones.OfType<Zone_Growing>().ToList();
            int colonistCount = map.mapPawns.FreeColonistsSpawnedCount;

            // 콜로니스트당 최소 25칸의 재배 구역
            int desiredArea = colonistCount * 25;
            int currentArea = existingZones.Sum(z => z.Cells.Count);

            if (currentArea < desiredArea)
            {
                // 새 재배 구역 생성
                CreateGrowingZone(map, 5, 5); // 5x5 구역
                lastZoneActionTick[map] = Find.TickManager.TicksGame;
                LogInfo($"재배 구역 생성 (현재: {currentArea}, 목표: {desiredArea})");
            }
        }

        /// <summary>
        /// 재배 구역 생성
        /// </summary>
        private void CreateGrowingZone(Map map, int width, int height)
        {
            // 좋은 토양 찾기
            IntVec3? location = FindGoodSoilLocation(map, width, height);
            if (location == null) return;

            try
            {
                Zone_Growing zone = new Zone_Growing(map.zoneManager);

                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < height; z++)
                    {
                        IntVec3 cell = location.Value + new IntVec3(x, 0, z);
                        if (cell.InBounds(map) && cell.GetZone(map) == null &&
                            cell.GetTerrain(map).fertility > 0)
                        {
                            zone.AddCell(cell);
                        }
                    }
                }

                if (zone.Cells.Count > 0)
                {
                    map.zoneManager.RegisterZone(zone);

                    // 감자 심기 (기본)
                    var potatoDef = DefDatabase<ThingDef>.GetNamedSilentFail("Plant_Potato");
                    if (potatoDef != null)
                    {
                        zone.SetPlantDefToGrow(potatoDef);
                    }

                    StoryLogger.Construction.InfrastructurePlan("재배 구역");
                }
            }
            catch (System.Exception ex)
            {
                LogError($"재배 구역 생성 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 좋은 토양 위치 찾기
        /// </summary>
        private IntVec3? FindGoodSoilLocation(Map map, int width, int height)
        {
            var center = map.Center;

            // 중심에서 가까운 비옥한 땅 찾기
            for (int radius = 10; radius < 50; radius += 5)
            {
                foreach (var cell in GenRadial.RadialCellsAround(center, radius, true))
                {
                    if (!cell.InBounds(map)) continue;

                    // 이미 구역이 있으면 스킵
                    if (cell.GetZone(map) != null) continue;

                    // 비옥도 확인
                    var terrain = cell.GetTerrain(map);
                    if (terrain.fertility < 0.7f) continue;

                    // 건물이 없어야 함
                    if (cell.GetFirstBuilding(map) != null) continue;

                    // 지붕이 없어야 함 (햇빛 필요)
                    if (cell.Roofed(map)) continue;

                    // 충분한 공간 확인
                    bool hasSpace = true;
                    for (int x = 0; x < width && hasSpace; x++)
                    {
                        for (int z = 0; z < height && hasSpace; z++)
                        {
                            IntVec3 checkCell = cell + new IntVec3(x, 0, z);
                            if (!checkCell.InBounds(map) ||
                                checkCell.GetZone(map) != null ||
                                checkCell.GetFirstBuilding(map) != null ||
                                checkCell.Roofed(map))
                            {
                                hasSpace = false;
                            }
                        }
                    }

                    if (hasSpace) return cell;
                }
            }

            return null;
        }

        /// <summary>
        /// 채굴 지정 확인
        /// </summary>
        private void CheckMiningDesignations(Map map)
        {
            // 철이 부족하면 채굴 지정
            int steel = map.resourceCounter.GetCount(ThingDefOf.Steel);
            if (steel < 200)
            {
                DesignateMining(map, 10); // 10개 지정
            }
        }

        /// <summary>
        /// 채굴 지정
        /// </summary>
        private void DesignateMining(Map map, int count)
        {
            var mineableRocks = map.listerThings.AllThings
                .Where(t => t.def.mineable && t.def.building?.mineableThing != null)
                .OrderBy(t => t.Position.DistanceTo(map.Center))
                .Take(count * 3);

            int designated = 0;
            foreach (var rock in mineableRocks)
            {
                if (designated >= count) break;

                // 이미 지정되어 있으면 스킵
                if (map.designationManager.DesignationOn(rock, DesignationDefOf.Mine) != null)
                    continue;

                // 접근 가능한지 확인
                if (!rock.Position.Standable(map)) continue;

                map.designationManager.AddDesignation(new Designation(rock, DesignationDefOf.Mine));
                designated++;
            }

            if (designated > 0)
            {
                lastZoneActionTick[map] = Find.TickManager.TicksGame;
                LogInfo($"채굴 지정: {designated}개");
            }
        }

        /// <summary>
        /// 벌목 지정 확인
        /// </summary>
        private void CheckChoppingDesignations(Map map)
        {
            // 목재가 부족하면 벌목 지정
            int wood = map.resourceCounter.GetCount(ThingDefOf.WoodLog);
            if (wood < 300)
            {
                DesignateChopping(map, 15);
            }
        }

        /// <summary>
        /// 벌목 지정
        /// </summary>
        private void DesignateChopping(Map map, int count)
        {
            var trees = map.listerThings.AllThings
                .Where(t => t.def.plant?.IsTree == true && t.def.plant.harvestWork > 0)
                .OrderBy(t => t.Position.DistanceTo(map.Center))
                .Take(count * 2);

            int designated = 0;
            foreach (var tree in trees)
            {
                if (designated >= count) break;

                if (map.designationManager.DesignationOn(tree, DesignationDefOf.CutPlant) != null)
                    continue;

                map.designationManager.AddDesignation(new Designation(tree, DesignationDefOf.CutPlant));
                designated++;
            }

            if (designated > 0)
            {
                lastZoneActionTick[map] = Find.TickManager.TicksGame;
                LogInfo($"벌목 지정: {designated}개");
            }
        }

        /// <summary>
        /// 사냥 지정 확인
        /// </summary>
        private void CheckHuntingDesignations(Map map)
        {
            // 음식이 부족하면 사냥 지정
            var foodState = RimAIManager.Instance?.GetSubsystem<Food.FoodSubsystem>()?.GetState(map);
            if (foodState != null && foodState.DaysUntilStarvation < 5)
            {
                DesignateHunting(map, 3);
            }
        }

        /// <summary>
        /// 사냥 지정
        /// </summary>
        private void DesignateHunting(Map map, int count)
        {
            var huntableAnimals = map.mapPawns.AllPawnsSpawned
                .Where(p => p.RaceProps.Animal &&
                           p.Faction == null &&
                           !p.RaceProps.predator && // 포식자 제외
                           p.RaceProps.baseBodySize >= 0.3f) // 너무 작은 동물 제외
                .OrderBy(p => p.Position.DistanceTo(map.Center))
                .Take(count * 2);

            int designated = 0;
            foreach (var animal in huntableAnimals)
            {
                if (designated >= count) break;

                if (map.designationManager.DesignationOn(animal, DesignationDefOf.Hunt) != null)
                    continue;

                map.designationManager.AddDesignation(new Designation(animal, DesignationDefOf.Hunt));
                designated++;
            }

            if (designated > 0)
            {
                lastZoneActionTick[map] = Find.TickManager.TicksGame;
                LogInfo($"사냥 지정: {designated}마리");
            }
        }

        private bool CanDoZoneAction(Map map)
        {
            if (!lastZoneActionTick.TryGetValue(map, out var lastTick))
                return true;

            return (Find.TickManager.TicksGame - lastTick) >= ZONE_COOLDOWN;
        }

        public override List<RimAIAction> GetProposedActions(Map map)
        {
            return new List<RimAIAction>(); // 직접 실행
        }

        public override void ExecuteAction(RimAIAction action)
        {
            // 직접 실행 방식
        }

        public override void CleanupMap(Map map)
        {
            lastZoneActionTick.Remove(map);
        }
    }
}
