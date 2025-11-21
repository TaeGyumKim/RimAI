using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimAI.Construction
{
    /// <summary>
    /// 청사진 배치 실행 결과
    /// </summary>
    public class BlueprintResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int PlacedCount { get; set; } // 배치된 청사진 개수
    }

    /// <summary>
    /// 청사진 배치 실행기
    ///
    /// RimWorld API를 사용하여 실제로 청사진을 맵에 배치합니다.
    /// 자원 확인, 충돌 체크, 에러 처리를 담당합니다.
    /// </summary>
    public static class BlueprintExecutor
    {
        /// <summary>
        /// 단일 건물 청사진 배치
        /// </summary>
        /// <param name="map">맵</param>
        /// <param name="buildingDef">건물 Def</param>
        /// <param name="position">위치</param>
        /// <param name="stuffDef">재질 (null이면 자동 선택)</param>
        /// <param name="rotation">회전 (기본: North)</param>
        /// <returns>배치 결과</returns>
        public static BlueprintResult PlaceSingleBuilding(
            Map map,
            ThingDef buildingDef,
            IntVec3 position,
            ThingDef stuffDef = null,
            Rot4? rotation = null)
        {
            var result = new BlueprintResult();

            try
            {
                // 1. 입력 검증
                if (map == null || buildingDef == null || !position.IsValid)
                {
                    result.ErrorMessage = "잘못된 입력 파라미터";
                    return result;
                }

                // 2. 재질 결정
                ThingDef finalStuff = stuffDef ?? SelectBestStuff(map, buildingDef);
                if (finalStuff == null && buildingDef.MadeFromStuff)
                {
                    result.ErrorMessage = "적합한 재질을 찾을 수 없습니다";
                    return result;
                }

                // 3. 자원 확인
                if (!HasSufficientResources(map, buildingDef, finalStuff))
                {
                    result.ErrorMessage = "자원 부족";
                    return result;
                }

                // 4. 배치 가능 여부 확인
                Rot4 finalRotation = rotation ?? Rot4.North;
                AcceptanceReport canPlace = GenConstruct.CanPlaceBlueprintAt(
                    buildingDef,
                    position,
                    finalRotation,
                    map,
                    false, // godMode
                    null   // thing to ignore
                );

                if (!canPlace.Accepted)
                {
                    result.ErrorMessage = $"배치 불가: {canPlace.Reason}";
                    return result;
                }

                // 5. 청사진 배치
                GenConstruct.PlaceBlueprintForBuild(
                    buildingDef,
                    position,
                    map,
                    finalRotation,
                    Faction.OfPlayer,
                    finalStuff
                );

                result.Success = true;
                result.PlacedCount = 1;
                return result;
            }
            catch (System.Exception ex)
            {
                result.ErrorMessage = $"청사진 배치 중 오류: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 벽 라인 청사진 배치 (직선)
        /// </summary>
        public static BlueprintResult PlaceWallLine(
            Map map,
            IntVec3 start,
            IntVec3 end,
            ThingDef stuffDef = null)
        {
            var result = new BlueprintResult { PlacedCount = 0 };

            try
            {
                // 재질 결정 (돌벽 우선)
                ThingDef wallStuff = stuffDef ?? SelectBestStuff(map, ThingDefOf.Wall);
                if (wallStuff == null)
                {
                    result.ErrorMessage = "벽 재질이 없습니다";
                    return result;
                }

                // 시작점에서 끝점까지 직선 셀 가져오기
                List<IntVec3> cells = GenSight.PointsOnLineOfSight(start, end).ToList();

                foreach (IntVec3 cell in cells)
                {
                    if (!cell.InBounds(map))
                        continue;

                    // 이미 벽이 있으면 스킵
                    if (cell.GetFirstBuilding(map)?.def == ThingDefOf.Wall)
                        continue;

                    // 청사진 배치
                    var singleResult = PlaceSingleBuilding(map, ThingDefOf.Wall, cell, wallStuff);
                    if (singleResult.Success)
                    {
                        result.PlacedCount++;
                    }
                }

                result.Success = result.PlacedCount > 0;
                return result;
            }
            catch (System.Exception ex)
            {
                result.ErrorMessage = $"벽 라인 배치 중 오류: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 방 청사진 배치 (벽 + 문 + 가구)
        /// </summary>
        public static BlueprintResult PlaceRoom(
            Map map,
            IntVec3 position,
            IntVec2 size,
            ThingDef furnitureDef = null) // 예: 침대
        {
            var result = new BlueprintResult { PlacedCount = 0 };

            try
            {
                // 재질 선택
                ThingDef wallStuff = SelectBestStuff(map, ThingDefOf.Wall);
                if (wallStuff == null)
                {
                    result.ErrorMessage = "벽 재질이 없습니다";
                    return result;
                }

                // 1. 벽 배치 (4면)
                for (int x = 0; x < size.x; x++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        // 경계만 벽
                        bool isEdge = (x == 0 || x == size.x - 1 || z == 0 || z == size.z - 1);
                        if (!isEdge)
                            continue;

                        // 문 위치 제외 (남쪽 중앙)
                        bool isDoorSpot = (z == 0 && x == size.x / 2);
                        if (isDoorSpot)
                            continue;

                        IntVec3 cell = position + new IntVec3(x, 0, z);
                        var wallResult = PlaceSingleBuilding(map, ThingDefOf.Wall, cell, wallStuff);
                        if (wallResult.Success)
                            result.PlacedCount++;
                    }
                }

                // 2. 문 배치 (남쪽 중앙)
                IntVec3 doorPos = position + new IntVec3(size.x / 2, 0, 0);
                var doorResult = PlaceSingleBuilding(map, ThingDefOf.Door, doorPos, wallStuff);
                if (doorResult.Success)
                    result.PlacedCount++;

                // 3. 가구 배치 (중앙)
                if (furnitureDef != null)
                {
                    IntVec3 furniturePos = position + new IntVec3(size.x / 2, 0, size.z / 2);
                    var furnitureResult = PlaceSingleBuilding(map, furnitureDef, furniturePos);
                    if (furnitureResult.Success)
                        result.PlacedCount++;
                }

                result.Success = result.PlacedCount > 0;
                return result;
            }
            catch (System.Exception ex)
            {
                result.ErrorMessage = $"방 배치 중 오류: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 스톡파일 구역 생성
        /// </summary>
        public static BlueprintResult CreateStockpileZone(
            Map map,
            IntVec3 position,
            IntVec2 size)
        {
            var result = new BlueprintResult();

            try
            {
                // 구역 생성
                Zone_Stockpile zone = new Zone_Stockpile(
                    StorageSettingsPreset.DefaultStockpile,
                    map.zoneManager
                );

                // 셀 추가
                int addedCount = 0;
                for (int x = 0; x < size.x; x++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        IntVec3 cell = position + new IntVec3(x, 0, z);

                        if (!cell.InBounds(map))
                            continue;

                        // 건설 가능 지형인지 확인
                        if (!cell.Standable(map))
                            continue;

                        zone.AddCell(cell);
                        addedCount++;
                    }
                }

                if (addedCount == 0)
                {
                    result.ErrorMessage = "스톡파일 구역을 배치할 수 있는 셀이 없습니다";
                    return result;
                }

                // 맵에 등록
                map.zoneManager.RegisterZone(zone);

                result.Success = true;
                result.PlacedCount = 1;
                return result;
            }
            catch (System.Exception ex)
            {
                result.ErrorMessage = $"스톡파일 구역 생성 중 오류: {ex.Message}";
                return result;
            }
        }

        // 캐시된 ThingDef 참조 (DefDatabase로 안전하게 조회)
        private static ThingDef _blocksLimestone;
        private static ThingDef BlocksLimestoneDef => _blocksLimestone ?? (_blocksLimestone = DefDatabase<ThingDef>.GetNamedSilentFail("BlocksLimestone"));

        /// <summary>
        /// 최적 재질 선택 (목재 > 돌 > 철)
        /// </summary>
        private static ThingDef SelectBestStuff(Map map, ThingDef buildingDef)
        {
            if (!buildingDef.MadeFromStuff)
                return null;

            // 건물이 사용할 수 있는 재질 카테고리
            var categories = buildingDef.stuffCategories;
            if (categories == null || categories.Count == 0)
                return null;

            // 우선순위: 목재 > 돌 > 철 - DefDatabase로 안전하게 조회
            List<ThingDef> candidates = new List<ThingDef>
            {
                ThingDefOf.WoodLog,
                DefDatabase<ThingDef>.GetNamedSilentFail("BlocksGranite"),
                BlocksLimestoneDef,
                DefDatabase<ThingDef>.GetNamedSilentFail("BlocksMarble"),
                DefDatabase<ThingDef>.GetNamedSilentFail("BlocksSandstone"),
                DefDatabase<ThingDef>.GetNamedSilentFail("BlocksSlate"),
                ThingDefOf.Steel
            };

            foreach (var candidate in candidates)
            {
                if (candidate == null)
                    continue;

                // 카테고리 확인
                if (candidate.stuffProps?.categories == null)
                    continue;

                bool matchesCategory = categories.Any(c =>
                    candidate.stuffProps.categories.Contains(c));

                if (!matchesCategory)
                    continue;

                // 자원 보유량 확인 (최소 10개면 건설 시도)
                int available = map.resourceCounter.GetCount(candidate);
                if (available >= 10) // 최소 10개 이상
                    return candidate;
            }

            return null;
        }

        /// <summary>
        /// 자원 충분 여부 확인
        /// </summary>
        private static bool HasSufficientResources(Map map, ThingDef buildingDef, ThingDef stuffDef)
        {
            // 건설 비용 계산
            List<ThingDefCountClass> costs = buildingDef.CostListAdjusted(stuffDef, true);

            if (costs == null || costs.Count == 0)
                return true; // 비용이 없으면 OK

            // 각 자원이 충분한지 확인 (청사진만 배치하면 되므로 정확한 자원만 있으면 됨)
            foreach (var cost in costs)
            {
                int required = cost.count;
                int available = map.resourceCounter.GetCount(cost.thingDef);

                // 정확한 양만 필요 (청사진 배치이므로)
                if (available < required)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 건설 비용 계산 (디버그용)
        /// </summary>
        public static string GetCostDescription(ThingDef buildingDef, ThingDef stuffDef)
        {
            List<ThingDefCountClass> costs = buildingDef.CostListAdjusted(stuffDef, true);

            if (costs == null || costs.Count == 0)
                return "비용 없음";

            return string.Join(", ", costs.Select(c => $"{c.thingDef.label} x{c.count}"));
        }
    }
}
