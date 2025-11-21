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
    /// 게임 시작 직후부터 자원 수집, 구역 생성, 채굴/벌목/사냥을 자동으로 관리합니다.
    /// </summary>
    public class ZoneDesignationSubsystem : RimAISubsystemBase
    {
        private Dictionary<Map, int> lastZoneActionTick = new Dictionary<Map, int>();
        private Dictionary<Map, bool> initialSetupDone = new Dictionary<Map, bool>();
        private const int ZONE_COOLDOWN = 60; // 1초 - 매우 빠르게

        public override string Name => "ZoneDesignation"; // 설정과 일치해야 함!
        public override int Priority => 90; // 높은 우선순위 - 자원 수집이 먼저

        public ZoneDesignationSubsystem()
        {
            baseUpdateInterval = 30; // 0.5초마다 체크 - 매우 적극적
        }

        public override void Update(Map map)
        {
            if (!ShouldUpdate()) return;
            if (!Enabled) return;

            try
            {
                // 게임 시작 직후 초기 설정
                if (!initialSetupDone.ContainsKey(map) || !initialSetupDone[map])
                {
                    DoInitialSetup(map);
                    initialSetupDone[map] = true;
                    return;
                }

                // 쿨다운 체크
                if (!CanDoZoneAction(map)) return;

                // 1. 스톡파일 구역 확인
                CheckStockpileZones(map);

                // 2. 재배 구역 확인 및 생성
                CheckGrowingZones(map);

                // 3. 슬래그/컴포넌트 수집 지정
                CheckResourceGathering(map);

                // 4. 채굴 지정
                CheckMiningDesignations(map);

                // 5. 벌목 지정
                CheckChoppingDesignations(map);

                // 6. 사냥 지정
                CheckHuntingDesignations(map);

                // 7. 운반 지정
                CheckHaulingDesignations(map);
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RimAI-ZoneDesignation] 업데이트 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 게임 시작 직후 초기 설정 - 매우 중요!
        /// </summary>
        private void DoInitialSetup(Map map)
        {
            Log.Message("[RimAI-ZoneDesignation] === 초기 설정 시작 ===");

            // 1. 스톡파일 구역 생성
            CreateInitialStockpile(map);

            // 2. 재배 구역 생성
            CreateGrowingZone(map, 6, 6);

            // 3. 모든 슬래그/스틸 청크 운반 지정
            DesignateAllChunksForHauling(map);

            // 4. 초기 채굴 지정
            DesignateMining(map, 20);

            // 5. 초기 벌목 지정
            DesignateChopping(map, 20);

            // 6. 안전한 동물 사냥 지정
            DesignateHunting(map, 5);

            Log.Message("[RimAI-ZoneDesignation] === 초기 설정 완료 ===");
        }

        /// <summary>
        /// 초기 스톡파일 구역 생성
        /// </summary>
        private void CreateInitialStockpile(Map map)
        {
            var existingStockpiles = map.zoneManager.AllZones.OfType<Zone_Stockpile>().ToList();
            if (existingStockpiles.Any()) return;

            // 맵 중앙 근처에 스톡파일 생성
            IntVec3? location = FindOpenLocation(map, 7, 7, map.Center);
            if (location == null)
            {
                Log.Warning("[RimAI-ZoneDesignation] 스톡파일 위치를 찾을 수 없음");
                return;
            }

            try
            {
                Zone_Stockpile zone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);

                int added = 0;
                for (int x = 0; x < 7; x++)
                {
                    for (int z = 0; z < 7; z++)
                    {
                        IntVec3 cell = location.Value + new IntVec3(x, 0, z);
                        if (cell.InBounds(map) && cell.Standable(map) && cell.GetZone(map) == null)
                        {
                            zone.AddCell(cell);
                            added++;
                        }
                    }
                }

                if (added > 0)
                {
                    map.zoneManager.RegisterZone(zone);
                    Log.Message($"[RimAI-ZoneDesignation] 스톡파일 구역 생성: {added}칸");
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RimAI-ZoneDesignation] 스톡파일 생성 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 스톡파일 구역 확인
        /// </summary>
        private void CheckStockpileZones(Map map)
        {
            var stockpiles = map.zoneManager.AllZones.OfType<Zone_Stockpile>().ToList();
            int totalCells = stockpiles.Sum(z => z.Cells.Count);

            // 콜로니스트당 최소 30칸의 저장 공간
            int colonistCount = map.mapPawns.FreeColonistsSpawnedCount;
            int desiredCells = colonistCount * 30 + 50; // 기본 50칸 + 콜로니스트당 30칸

            if (totalCells < desiredCells)
            {
                // 추가 스톡파일 생성
                IntVec3? location = FindOpenLocation(map, 5, 5, map.Center);
                if (location != null)
                {
                    CreateStockpileAt(map, location.Value, 5, 5);
                    lastZoneActionTick[map] = Find.TickManager.TicksGame;
                }
            }
        }

        /// <summary>
        /// 특정 위치에 스톡파일 생성
        /// </summary>
        private void CreateStockpileAt(Map map, IntVec3 position, int width, int height)
        {
            try
            {
                Zone_Stockpile zone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);

                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < height; z++)
                    {
                        IntVec3 cell = position + new IntVec3(x, 0, z);
                        if (cell.InBounds(map) && cell.Standable(map) && cell.GetZone(map) == null)
                        {
                            zone.AddCell(cell);
                        }
                    }
                }

                if (zone.Cells.Count > 0)
                {
                    map.zoneManager.RegisterZone(zone);
                    Log.Message($"[RimAI-ZoneDesignation] 추가 스톡파일 생성: {zone.Cells.Count}칸");
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RimAI-ZoneDesignation] 스톡파일 생성 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 모든 청크/슬래그 운반 지정
        /// </summary>
        private void DesignateAllChunksForHauling(Map map)
        {
            // 슬래그 청크, 스틸 청크, 컴팩트 머시너리 등 찾기
            var haulableItems = map.listerThings.AllThings
                .Where(t => t.def.EverHaulable &&
                           t.Faction == null &&
                           !t.IsForbidden(Faction.OfPlayer) &&
                           (t.def.defName.Contains("Chunk") ||
                            t.def.defName.Contains("Slag") ||
                            t.def.defName.Contains("Compacted") ||
                            t.def.defName.Contains("Steel") ||
                            t.def.defName.Contains("Component") ||
                            t.def.defName.Contains("Silver") ||
                            t.def.defName.Contains("Gold") ||
                            t.def.defName.Contains("Plasteel") ||
                            t.def == ThingDefOf.Steel ||
                            t.def == ThingDefOf.ComponentIndustrial))
                .ToList();

            int hauled = 0;
            foreach (var item in haulableItems)
            {
                // Forbid 해제
                if (item.IsForbidden(Faction.OfPlayer))
                {
                    item.SetForbidden(false, false);
                    hauled++;
                }
            }

            if (hauled > 0)
            {
                Log.Message($"[RimAI-ZoneDesignation] 자원 운반 활성화: {hauled}개");
            }
        }

        /// <summary>
        /// 자원 수집 지정 확인
        /// </summary>
        private void CheckResourceGathering(Map map)
        {
            // 금지된 아이템 해제
            var forbiddenItems = map.listerThings.AllThings
                .Where(t => t.def.EverHaulable &&
                           t.IsForbidden(Faction.OfPlayer) &&
                           t.Faction == null &&
                           IsValuableResource(t.def))
                .Take(20);

            int unforbidden = 0;
            foreach (var item in forbiddenItems)
            {
                item.SetForbidden(false, false);
                unforbidden++;
            }

            if (unforbidden > 0)
            {
                lastZoneActionTick[map] = Find.TickManager.TicksGame;
                Log.Message($"[RimAI-ZoneDesignation] 자원 금지 해제: {unforbidden}개");
            }
        }

        /// <summary>
        /// 가치 있는 자원인지 확인
        /// </summary>
        private bool IsValuableResource(ThingDef def)
        {
            if (def == null) return false;

            // 기본 자원
            if (def == ThingDefOf.Steel || def == ThingDefOf.WoodLog ||
                def == ThingDefOf.ComponentIndustrial || def == ThingDefOf.Silver ||
                def == ThingDefOf.Gold || def == ThingDefOf.Plasteel ||
                def == ThingDefOf.Uranium || def == ThingDefOf.Jade)
                return true;

            // 이름으로 확인
            string name = def.defName.ToLower();
            return name.Contains("chunk") || name.Contains("slag") ||
                   name.Contains("compacted") || name.Contains("component") ||
                   name.Contains("steel") || name.Contains("medicine") ||
                   name.Contains("meal") || name.Contains("food");
        }

        /// <summary>
        /// 운반 지정 확인
        /// </summary>
        private void CheckHaulingDesignations(Map map)
        {
            // 바닥에 있는 운반 가능 아이템 확인
            var itemsOnGround = map.listerThings.AllThings
                .Where(t => t.def.EverHaulable &&
                           t.Faction == null &&
                           !t.IsForbidden(Faction.OfPlayer) &&
                           t.Position.GetZone(map) == null) // 구역 밖에 있는 것
                .Take(30);

            // 슬래그 청크 해체 지정
            var slagChunks = map.listerThings.AllThings
                .Where(t => t.def.defName.Contains("Chunk") && t.def.defName.Contains("Slag"))
                .Take(10);

            foreach (var chunk in slagChunks)
            {
                if (map.designationManager.DesignationOn(chunk, DesignationDefOf.Deconstruct) == null &&
                    map.designationManager.DesignationOn(chunk, DesignationDefOf.Haul) == null)
                {
                    // 슬래그 청크 운반 활성화
                    chunk.SetForbidden(false, false);
                }
            }
        }

        /// <summary>
        /// 재배 구역 확인 및 생성
        /// </summary>
        private void CheckGrowingZones(Map map)
        {
            var existingZones = map.zoneManager.AllZones.OfType<Zone_Growing>().ToList();
            int colonistCount = map.mapPawns.FreeColonistsSpawnedCount;

            // 콜로니스트당 최소 30칸의 재배 구역
            int desiredArea = colonistCount * 30 + 36; // 기본 6x6 + 콜로니스트당 30칸
            int currentArea = existingZones.Sum(z => z.Cells.Count);

            if (currentArea < desiredArea)
            {
                // 새 재배 구역 생성
                CreateGrowingZone(map, 6, 6);
                lastZoneActionTick[map] = Find.TickManager.TicksGame;
            }
        }

        /// <summary>
        /// 재배 구역 생성
        /// </summary>
        private void CreateGrowingZone(Map map, int width, int height)
        {
            // 좋은 토양 찾기
            IntVec3? location = FindGoodSoilLocation(map, width, height);
            if (location == null)
            {
                Log.Warning("[RimAI-ZoneDesignation] 재배 구역 위치를 찾을 수 없음");
                return;
            }

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

                    Log.Message($"[RimAI-ZoneDesignation] 재배 구역 생성: {zone.Cells.Count}칸");
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RimAI-ZoneDesignation] 재배 구역 생성 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 열린 위치 찾기
        /// </summary>
        private IntVec3? FindOpenLocation(Map map, int width, int height, IntVec3 near)
        {
            for (int radius = 5; radius < 60; radius += 5)
            {
                foreach (var cell in GenRadial.RadialCellsAround(near, radius, true))
                {
                    if (!cell.InBounds(map)) continue;
                    if (cell.GetZone(map) != null) continue;
                    if (!cell.Standable(map)) continue;

                    // 충분한 공간 확인
                    bool hasSpace = true;
                    for (int x = 0; x < width && hasSpace; x++)
                    {
                        for (int z = 0; z < height && hasSpace; z++)
                        {
                            IntVec3 checkCell = cell + new IntVec3(x, 0, z);
                            if (!checkCell.InBounds(map) ||
                                checkCell.GetZone(map) != null ||
                                !checkCell.Standable(map))
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
        /// 좋은 토양 위치 찾기
        /// </summary>
        private IntVec3? FindGoodSoilLocation(Map map, int width, int height)
        {
            var center = map.Center;

            for (int radius = 10; radius < 60; radius += 5)
            {
                foreach (var cell in GenRadial.RadialCellsAround(center, radius, true))
                {
                    if (!cell.InBounds(map)) continue;
                    if (cell.GetZone(map) != null) continue;

                    var terrain = cell.GetTerrain(map);
                    if (terrain.fertility < 0.7f) continue;
                    if (cell.GetFirstBuilding(map) != null) continue;
                    if (cell.Roofed(map)) continue;

                    bool hasSpace = true;
                    for (int x = 0; x < width && hasSpace; x++)
                    {
                        for (int z = 0; z < height && hasSpace; z++)
                        {
                            IntVec3 checkCell = cell + new IntVec3(x, 0, z);
                            if (!checkCell.InBounds(map) ||
                                checkCell.GetZone(map) != null ||
                                checkCell.GetFirstBuilding(map) != null ||
                                checkCell.Roofed(map) ||
                                checkCell.GetTerrain(map).fertility < 0.5f)
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
        /// 채굴 지정 확인 - 더 적극적으로
        /// </summary>
        private void CheckMiningDesignations(Map map)
        {
            // 철 확인
            int steel = map.resourceCounter.GetCount(ThingDefOf.Steel);

            // 항상 채굴 - 철이 500 미만이면
            if (steel < 500)
            {
                DesignateMining(map, 15);
            }
        }

        /// <summary>
        /// 채굴 지정
        /// </summary>
        private void DesignateMining(Map map, int count)
        {
            // 컴팩티드 스틸, 컴팩티드 머시너리 우선
            var mineableThings = map.listerThings.AllThings
                .Where(t => t.def.mineable && t.def.building?.mineableThing != null)
                .OrderByDescending(t =>
                {
                    // 컴팩티드 스틸/머시너리 우선
                    if (t.def.defName.Contains("Compacted")) return 100;
                    if (t.def.building.mineableThing == ThingDefOf.Steel) return 50;
                    if (t.def.building.mineableThing == ThingDefOf.ComponentIndustrial) return 80;
                    return 1;
                })
                .ThenBy(t => t.Position.DistanceTo(map.Center))
                .Take(count * 3);

            int designated = 0;
            foreach (var rock in mineableThings)
            {
                if (designated >= count) break;

                if (map.designationManager.DesignationOn(rock, DesignationDefOf.Mine) != null)
                    continue;

                // 접근 가능한지 대략적으로 확인
                bool hasAdjacentStandable = false;
                foreach (var adj in GenAdjFast.AdjacentCells8Way(rock.Position))
                {
                    if (adj.InBounds(map) && adj.Standable(map))
                    {
                        hasAdjacentStandable = true;
                        break;
                    }
                }

                if (!hasAdjacentStandable) continue;

                map.designationManager.AddDesignation(new Designation(rock, DesignationDefOf.Mine));
                designated++;
            }

            if (designated > 0)
            {
                lastZoneActionTick[map] = Find.TickManager.TicksGame;
                Log.Message($"[RimAI-ZoneDesignation] 채굴 지정: {designated}개");
            }
        }

        /// <summary>
        /// 벌목 지정 확인 - 더 적극적으로
        /// </summary>
        private void CheckChoppingDesignations(Map map)
        {
            int wood = map.resourceCounter.GetCount(ThingDefOf.WoodLog);

            // 목재가 500 미만이면 벌목
            if (wood < 500)
            {
                DesignateChopping(map, 20);
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
                Log.Message($"[RimAI-ZoneDesignation] 벌목 지정: {designated}개");
            }
        }

        /// <summary>
        /// 사냥 지정 확인 - 더 적극적으로
        /// </summary>
        private void CheckHuntingDesignations(Map map)
        {
            // 음식 상태 확인
            var foodState = RimAIManager.Instance?.GetSubsystem<Food.FoodSubsystem>()?.GetState(map);

            // 음식이 10일 미만이거나 확인 불가면 사냥
            if (foodState == null || foodState.DaysUntilStarvation < 10)
            {
                DesignateHunting(map, 5);
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
                           !p.RaceProps.predator &&
                           p.RaceProps.baseBodySize >= 0.3f &&
                           p.RaceProps.baseBodySize <= 2.0f) // 너무 큰 동물 제외
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
                Log.Message($"[RimAI-ZoneDesignation] 사냥 지정: {designated}마리");
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
            initialSetupDone.Remove(map);
        }
    }
}
