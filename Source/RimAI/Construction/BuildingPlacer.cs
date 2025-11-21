using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimAI.Construction
{
    /// <summary>
    /// 건물 타입 (배치 전략 결정용)
    /// </summary>
    public enum BuildingType
    {
        Bedroom,      // 침실
        DiningRoom,   // 식당
        Storage,      // 창고
        Defense,      // 방어 시설
        Infrastructure // 기타 인프라
    }

    /// <summary>
    /// 건물 배치 위치 찾기 알고리즘
    ///
    /// 나선형 탐색으로 거점 근처 최적 위치를 찾습니다.
    /// 지형, 충돌, 접근성을 검증하고 점수를 계산합니다.
    /// </summary>
    public static class BuildingPlacer
    {
        // 탐색 설정
        private const int MAX_SEARCH_RADIUS = 50; // 최대 탐색 반경
        private const float MIN_SCORE = 50f;      // 최소 점수 (이하는 실패)
        private const float GOOD_SCORE = 85f;     // 좋은 점수 (조기 종료)

        /// <summary>
        /// 건물 배치 최적 위치 찾기 (메인 API)
        /// </summary>
        /// <param name="map">맵</param>
        /// <param name="buildingDef">건물 Def</param>
        /// <param name="size">건물 크기 (예: 3x4)</param>
        /// <param name="type">건물 타입</param>
        /// <param name="anchorPoint">거점 위치 (null이면 콜로니 중심)</param>
        /// <returns>최적 위치, 실패 시 null</returns>
        public static IntVec3? FindBestLocation(
            Map map,
            ThingDef buildingDef,
            IntVec2 size,
            BuildingType type,
            IntVec3? anchorPoint = null)
        {
            if (map == null || buildingDef == null)
                return null;

            // 거점 결정
            IntVec3 anchor = anchorPoint ?? GetColonyCenter(map);
            if (!anchor.IsValid || !anchor.InBounds(map))
                return null;

            // 나선형 탐색
            IntVec3? bestLocation = null;
            float bestScore = MIN_SCORE;

            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(anchor, MAX_SEARCH_RADIUS, true))
            {
                // 맵 경계 체크
                if (!candidate.InBounds(map))
                    continue;

                // 위치 검증
                if (!IsValidLocation(map, candidate, size, buildingDef))
                    continue;

                // 점수 계산
                float score = ScoreLocation(map, candidate, size, type, anchor);

                // 최고 점수 갱신
                if (score > bestScore)
                {
                    bestScore = score;
                    bestLocation = candidate;

                    // 조기 종료: 충분히 좋은 위치 발견
                    if (score >= GOOD_SCORE)
                        break;
                }
            }

            return bestLocation;
        }

        /// <summary>
        /// 위치 유효성 검증
        /// </summary>
        private static bool IsValidLocation(Map map, IntVec3 position, IntVec2 size, ThingDef buildingDef)
        {
            // 1. 크기 범위 내 모든 셀 체크
            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    IntVec3 cell = position + new IntVec3(x, 0, z);

                    // 맵 경계
                    if (!cell.InBounds(map))
                        return false;

                    // 지형 체크
                    if (!IsTerrainSuitable(cell, map))
                        return false;

                    // 충돌 체크
                    if (HasObstacle(cell, map, buildingDef))
                        return false;
                }
            }

            // 2. 접근성 체크 (중심 셀이 도달 가능한가?)
            IntVec3 centerCell = position + new IntVec3(size.x / 2, 0, size.z / 2);
            if (!map.reachability.CanReachColony(centerCell))
                return false;

            return true;
        }

        /// <summary>
        /// 지형이 건설에 적합한지 확인
        /// </summary>
        private static bool IsTerrainSuitable(IntVec3 cell, Map map)
        {
            TerrainDef terrain = cell.GetTerrain(map);

            // 물/깊은 물 제외
            if (terrain.IsWater)
                return false;

            // 건설 불가 지형
            if (!terrain.affordances.Contains(TerrainAffordanceDefOf.Heavy))
                return false;

            // 서 있을 수 있어야 함
            if (!cell.Standable(map))
                return false;

            return true;
        }

        /// <summary>
        /// 장애물 존재 여부 확인
        /// </summary>
        private static bool HasObstacle(IntVec3 cell, Map map, ThingDef buildingDef)
        {
            // 기존 건물
            Building building = cell.GetFirstBuilding(map);
            if (building != null)
                return true;

            // 기존 청사진/공사 프레임 - ThingGrid 사용
            var thingsAtCell = map.thingGrid.ThingsListAt(cell);
            foreach (var thing in thingsAtCell)
            {
                if (thing.def.IsBlueprint || thing.def.isFrame)
                    return true;
            }

            // 이동 불가능한 물건 (큰 돌, 잔해 등)
            List<Thing> things = map.thingGrid.ThingsListAt(cell);
            foreach (var thing in things)
            {
                if (thing.def.passability == Traversability.Impassable)
                    return true;

                // 식물 (나무 등) - 건물 타입에 따라 다름
                if (thing is Plant plant && plant.def.plant.harvestWork > 300) // 큰 나무
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 위치 점수 계산 (0~100)
        /// </summary>
        private static float ScoreLocation(Map map, IntVec3 position, IntVec2 size, BuildingType type, IntVec3 anchor)
        {
            float score = 60f; // 기본 점수

            // 1. 거점 거리 (가까울수록 좋음)
            float distance = position.DistanceTo(anchor);
            float distanceScore = Mathf.Lerp(40f, 0f, distance / MAX_SEARCH_RADIUS);
            score += distanceScore;

            // 2. 지형 품질 (평평하고 비옥한 땅 선호)
            IntVec3 centerCell = position + new IntVec3(size.x / 2, 0, size.z / 2);
            float fertility = map.fertilityGrid.FertilityAt(centerCell);
            score += fertility * 5f; // 최대 +5

            // 3. 자연 벽 인접 (벽 건설 절약)
            int naturalWalls = CountNaturalWallsAdjacent(map, position, size);
            score += naturalWalls * 3f; // 벽 1개당 +3

            // 4. 건물 타입별 추가 점수
            score += GetTypeSpecificScore(map, position, size, type, anchor);

            // 5. 평지 보너스 (모든 셀이 같은 높이)
            if (IsLevelGround(map, position, size))
                score += 10f;

            return Mathf.Clamp(score, 0f, 100f);
        }

        /// <summary>
        /// 건물 타입별 특화 점수
        /// </summary>
        private static float GetTypeSpecificScore(Map map, IntVec3 position, IntVec2 size, BuildingType type, IntVec3 anchor)
        {
            switch (type)
            {
                case BuildingType.Bedroom:
                    // 실내 선호, 조용한 곳
                    return IsIndoors(map, position) ? 15f : 0f;

                case BuildingType.DiningRoom:
                    // 중앙, 주방 근처
                    float distanceToKitchen = DistanceToNearest(map, position, b => b.def.building?.isMealSource == true);
                    return distanceToKitchen < 15f ? 15f : 0f;

                case BuildingType.Storage:
                    // 넓은 공간, 접근 쉬운 곳
                    return IsOpenArea(map, position, size) ? 10f : 0f;

                case BuildingType.Defense:
                    // 외곽, 위협 방향
                    float distanceFromCenter = position.DistanceTo(anchor);
                    return distanceFromCenter > 20f ? 20f : 0f; // 외곽 선호

                default:
                    return 0f;
            }
        }

        /// <summary>
        /// 인접 자연 벽 개수
        /// </summary>
        private static int CountNaturalWallsAdjacent(Map map, IntVec3 position, IntVec2 size)
        {
            int count = 0;

            for (int x = -1; x <= size.x; x++)
            {
                for (int z = -1; z <= size.z; z++)
                {
                    // 경계만 체크
                    if (x != -1 && x != size.x && z != -1 && z != size.z)
                        continue;

                    IntVec3 cell = position + new IntVec3(x, 0, z);
                    if (!cell.InBounds(map))
                        continue;

                    Building building = cell.GetFirstBuilding(map);
                    if (building != null && building.def.building?.isNaturalRock == true)
                        count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 평지 여부 (높이 차이 없음)
        /// </summary>
        private static bool IsLevelGround(Map map, IntVec3 position, IntVec2 size)
        {
            TerrainDef firstTerrain = position.GetTerrain(map);

            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    IntVec3 cell = position + new IntVec3(x, 0, z);
                    if (!cell.InBounds(map))
                        return false;

                    // 같은 지형인지 확인 (간단한 평지 판정)
                    if (cell.GetTerrain(map) != firstTerrain)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 실내 여부 (지붕이 있는가)
        /// </summary>
        private static bool IsIndoors(Map map, IntVec3 position)
        {
            return position.Roofed(map);
        }

        /// <summary>
        /// 개방된 공간 여부
        /// </summary>
        private static bool IsOpenArea(Map map, IntVec3 position, IntVec2 size)
        {
            // 주변 8칸도 비어있는지 확인
            for (int x = -1; x <= size.x; x++)
            {
                for (int z = -1; z <= size.z; z++)
                {
                    IntVec3 cell = position + new IntVec3(x, 0, z);
                    if (!cell.InBounds(map))
                        continue;

                    if (cell.GetFirstBuilding(map) != null)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 가장 가까운 건물까지 거리
        /// </summary>
        private static float DistanceToNearest(Map map, IntVec3 position, Predicate<Building> predicate)
        {
            float minDistance = float.MaxValue;

            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (predicate(building))
                {
                    float distance = position.DistanceTo(building.Position);
                    if (distance < minDistance)
                        minDistance = distance;
                }
            }

            return minDistance;
        }

        /// <summary>
        /// 콜로니 중심 계산
        /// </summary>
        private static IntVec3 GetColonyCenter(Map map)
        {
            // 모든 콜로니스트의 평균 위치
            var colonists = map.mapPawns.FreeColonistsSpawned;

            if (colonists.Count == 0)
            {
                // 콜로니스트가 없으면 맵 중심
                return map.Center;
            }

            int sumX = 0;
            int sumZ = 0;

            foreach (var colonist in colonists)
            {
                sumX += colonist.Position.x;
                sumZ += colonist.Position.z;
            }

            return new IntVec3(sumX / colonists.Count, 0, sumZ / colonists.Count);
        }

        /// <summary>
        /// 간단한 위치 찾기 (빠른 버전, 첫 번째 유효 위치 반환)
        /// </summary>
        public static IntVec3? FindQuickLocation(Map map, ThingDef buildingDef, IntVec2 size)
        {
            IntVec3 anchor = GetColonyCenter(map);

            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(anchor, 30, true))
            {
                if (!candidate.InBounds(map))
                    continue;

                if (IsValidLocation(map, candidate, size, buildingDef))
                    return candidate;
            }

            return null;
        }
    }
}
