using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimAI.Trading
{
    /// <summary>
    /// 콜로니의 무역 상태를 분석하는 클래스
    ///
    /// 분석 항목:
    /// - 현재 상인 (방문/궤도)
    /// - 자원 부족/초과
    /// - 판매/구매 추천 품목
    /// - 카라반 파견 가능 여부
    /// </summary>
    public static class TradingAnalyzer
    {
        // 자원 임계값
        private const int STEEL_SHORTAGE_THRESHOLD = 100;
        private const int STEEL_SURPLUS_THRESHOLD = 500;
        private const int COMPONENT_SHORTAGE_THRESHOLD = 5;
        private const int MEDICINE_SHORTAGE_THRESHOLD = 5;
        private const float FOOD_DAYS_SHORTAGE_THRESHOLD = 5f;

        // 판매 임계값 (이 이상이면 판매 권장)
        private const int LEATHER_SURPLUS_THRESHOLD = 200;
        private const int CLOTH_SURPLUS_THRESHOLD = 200;

        /// <summary>
        /// 특정 맵의 무역 상태를 분석
        /// </summary>
        public static ColonyTradingState AnalyzeMap(Map map)
        {
            var state = new ColonyTradingState();

            // 1. 현재 상인 분석
            AnalyzeTraders(map, state);

            // 2. 자원 현황 분석
            AnalyzeResources(map, state);

            // 3. 판매 가능 품목 분석
            AnalyzeSellableItems(map, state);

            // 4. 필요 품목 분석
            AnalyzeNeededItems(map, state);

            // 5. 카라반 가능 여부 분석
            AnalyzeCaravanCapability(map, state);

            // 6. 퀘스트 상태 분석
            AnalyzeQuests(map, state);

            return state;
        }

        /// <summary>
        /// 현재 상인 분석
        /// </summary>
        private static void AnalyzeTraders(Map map, ColonyTradingState state)
        {
            state.VisitingTraders.Clear();
            state.OrbitalTraders.Clear();

            // 방문 상인 찾기
            foreach (var pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.TraderKind != null && pawn.Faction != Faction.OfPlayer)
                {
                    state.VisitingTraders.Add(pawn);
                }
            }

            // 궤도 무역선 찾기
            var passingShips = map.passingShipManager?.passingShips;
            if (passingShips != null)
            {
                state.OrbitalTraders.AddRange(passingShips.OfType<TradeShip>());
            }
        }

        /// <summary>
        /// 자원 현황 분석
        /// </summary>
        private static void AnalyzeResources(Map map, ColonyTradingState state)
        {
            // 실버 계산
            state.TotalSilver = map.resourceCounter.GetCount(ThingDefOf.Silver);

            // 강철 확인
            int steel = map.resourceCounter.GetCount(ThingDefOf.Steel);
            state.SteelShortage = steel < STEEL_SHORTAGE_THRESHOLD;
            state.SteelSurplus = steel > STEEL_SURPLUS_THRESHOLD;

            // 컴포넌트 확인
            int components = map.resourceCounter.GetCount(ThingDefOf.ComponentIndustrial);
            state.ComponentShortage = components < COMPONENT_SHORTAGE_THRESHOLD;

            // 의약품 확인
            int medicine = CountMedicine(map);
            state.MedicineShortage = medicine < MEDICINE_SHORTAGE_THRESHOLD;

            // 음식 확인 (Food 서브시스템과 연동)
            float foodDays = EstimateFoodDays(map);
            state.FoodShortage = foodDays < FOOD_DAYS_SHORTAGE_THRESHOLD;

            // 가죽/천 확인
            int leather = CountLeather(map);
            state.LeatherSurplus = leather > LEATHER_SURPLUS_THRESHOLD;

            int cloth = map.resourceCounter.GetCount(ThingDefOf.Cloth);
            state.ClothSurplus = cloth > CLOTH_SURPLUS_THRESHOLD;
        }

        /// <summary>
        /// 판매 가능 품목 분석
        /// </summary>
        private static void AnalyzeSellableItems(Map map, ColonyTradingState state)
        {
            state.SellableItems.Clear();
            state.TotalSellableValue = 0f;

            // 초과 자원 판매 추천
            var things = map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableAlways);

            foreach (var thing in things)
            {
                if (thing.IsForbidden(Faction.OfPlayer))
                    continue;

                // 판매 가치가 있는 것만
                if (thing.MarketValue <= 0)
                    continue;

                // 초과 자원 확인
                bool shouldSell = ShouldSellItem(map, thing, state);

                if (shouldSell)
                {
                    var thingCount = new ThingCount(thing, thing.stackCount);
                    state.SellableItems.Add(thingCount);
                    state.TotalSellableValue += thingCount.MarketValue;
                }
            }
        }

        /// <summary>
        /// 특정 아이템을 판매해야 하는지 결정
        /// </summary>
        private static bool ShouldSellItem(Map map, Thing thing, ColonyTradingState state)
        {
            // 가죽 초과 시 판매
            if (thing.def.IsLeather && state.LeatherSurplus)
                return true;

            // 천 초과 시 판매
            if (thing.def == ThingDefOf.Cloth && state.ClothSurplus)
                return true;

            // 마약류 (Beer 제외) 판매
            if (thing.def.IsDrug && thing.def != ThingDefOf.Beer)
                return true;

            // 무기/갑옷 (품질 낮은 것) 판매
            if ((thing.def.IsWeapon || thing.def.IsApparel) &&
                thing.TryGetComp<CompQuality>()?.Quality <= QualityCategory.Poor)
                return true;

            // 예술품 판매
            if (thing.def.IsWithinCategory(ThingCategoryDefOf.BuildingsArt))
                return true;

            return false;
        }

        /// <summary>
        /// 필요 품목 분석
        /// </summary>
        private static void AnalyzeNeededItems(Map map, ColonyTradingState state)
        {
            state.NeededItems.Clear();

            // 강철 부족
            if (state.SteelShortage)
            {
                int current = map.resourceCounter.GetCount(ThingDefOf.Steel);
                state.NeededItems.Add(new ThingDefCount
                {
                    ThingDef = ThingDefOf.Steel,
                    NeededCount = STEEL_SHORTAGE_THRESHOLD * 2,
                    CurrentCount = current,
                    Priority = TradingPriority.High
                });
            }

            // 컴포넌트 부족
            if (state.ComponentShortage)
            {
                int current = map.resourceCounter.GetCount(ThingDefOf.ComponentIndustrial);
                state.NeededItems.Add(new ThingDefCount
                {
                    ThingDef = ThingDefOf.ComponentIndustrial,
                    NeededCount = COMPONENT_SHORTAGE_THRESHOLD * 2,
                    CurrentCount = current,
                    Priority = TradingPriority.Critical
                });
            }

            // 의약품 부족
            if (state.MedicineShortage)
            {
                int current = CountMedicine(map);
                state.NeededItems.Add(new ThingDefCount
                {
                    ThingDef = ThingDefOf.MedicineIndustrial,
                    NeededCount = MEDICINE_SHORTAGE_THRESHOLD * 2,
                    CurrentCount = current,
                    Priority = TradingPriority.High
                });
            }
        }

        /// <summary>
        /// 카라반 파견 가능 여부 분석
        /// </summary>
        private static void AnalyzeCaravanCapability(Map map, ColonyTradingState state)
        {
            state.NearbySettlements.Clear();

            // 출발 가능한 콜로니스트 수
            state.AvailableColonistsForCaravan = map.mapPawns.FreeColonistsSpawned
                .Count(p => !p.Downed && !p.InMentalState && p.health.capacities.CapableOf(PawnCapacityDefOf.Moving));

            // 짐짓는 동물 수
            state.PackAnimals = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer)
                .Count(p => p.RaceProps.Animal && p.RaceProps.packAnimal && !p.Downed);

            // 가까운 정착지 찾기 (10타일 이내)
            var mapTile = map.Tile;
            foreach (var worldObject in Find.WorldObjects.AllWorldObjects)
            {
                if (worldObject is Settlement settlement &&
                    settlement.Faction != Faction.OfPlayer &&
                    !settlement.Faction.HostileTo(Faction.OfPlayer))
                {
                    float distance = Find.WorldGrid.ApproxDistanceInTiles(mapTile, settlement.Tile);
                    if (distance <= 10)
                    {
                        state.NearbySettlements.Add(settlement);
                    }
                }
            }
        }

        /// <summary>
        /// 퀘스트 상태 분석
        /// </summary>
        private static void AnalyzeQuests(Map map, ColonyTradingState state)
        {
            state.ActiveQuestCount = Find.QuestManager?.QuestsListForReading?.Count(q => !q.Historical) ?? 0;

            // 엔딩 관련 퀘스트 확인
            state.HasEndgameQuest = Find.QuestManager?.QuestsListForReading?
                .Any(q => !q.Historical && (
                    q.root?.defName?.Contains("Ship") == true ||
                    q.root?.defName?.Contains("Royal") == true ||
                    q.root?.defName?.Contains("Archonexus") == true
                )) ?? false;

            // 우주선 건설 진행도
            var shipParts = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial)
                .Where(t => t.def.defName.Contains("Ship"))
                .ToList();

            state.ShipProgress = shipParts.Count > 0 ? shipParts.Count / 10f : 0f; // 대략적인 진행도
        }

        /// <summary>
        /// 의약품 총 수량
        /// </summary>
        private static int CountMedicine(Map map)
        {
            int total = 0;
            total += map.resourceCounter.GetCount(ThingDefOf.MedicineHerbal);
            total += map.resourceCounter.GetCount(ThingDefOf.MedicineIndustrial);
            total += map.resourceCounter.GetCount(ThingDefOf.MedicineUltratech);
            return total;
        }

        /// <summary>
        /// 가죽 총 수량
        /// </summary>
        private static int CountLeather(Map map)
        {
            return map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableAlways)
                .Where(t => t.def.IsLeather)
                .Sum(t => t.stackCount);
        }

        /// <summary>
        /// 예상 식량 일수
        /// </summary>
        private static float EstimateFoodDays(Map map)
        {
            float totalNutrition = 0f;
            var foods = map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSource);

            foreach (var food in foods)
            {
                if (food.def.IsIngestible)
                {
                    totalNutrition += food.def.GetStatValueAbstract(StatDefOf.Nutrition) * food.stackCount;
                }
            }

            int colonists = map.mapPawns.FreeColonistsSpawnedCount;
            if (colonists <= 0) return 999f;

            return totalNutrition / (colonists * 1.6f); // 1인당 하루 영양분 약 1.6
        }
    }
}
