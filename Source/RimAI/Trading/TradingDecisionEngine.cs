using System.Linq;
using System.Text;
using RimAI.Core;

namespace RimAI.Trading
{
    /// <summary>
    /// 무역 관련 작업 우선순위를 결정하는 의사결정 엔진
    ///
    /// 핵심 알고리즘:
    /// 1. 자원 부족 시 상인 방문 → 즉시 구매
    /// 2. 자원 초과 시 → 판매 권장
    /// 3. 카라반 파견 조건 분석
    /// 4. 엔딩 퀘스트 진행 시 특별 로직
    /// </summary>
    public static class TradingDecisionEngine
    {
        // === 임계값 설정 ===
        private const int MIN_SILVER_FOR_PURCHASE = 100;
        private const int MIN_COLONISTS_FOR_CARAVAN = 3;
        private const float MIN_SELLABLE_VALUE_FOR_TRADE = 500f;

        /// <summary>
        /// 콜로니 무역 상태를 분석하여 작업 우선순위 결정
        /// </summary>
        public static TradingDecision MakeDecision(ColonyTradingState state)
        {
            var decision = new TradingDecision();
            var reasoning = new StringBuilder();

            // === 1. 현재 상인 상태 확인 ===
            if (state.HasVisitingTrader || state.HasOrbitalTrader)
            {
                reasoning.AppendLine($"[상인] 방문중:{state.VisitingTraders.Count} 궤도선:{state.OrbitalTraders.Count}");
                DecideTradeWithVisitor(state, decision, reasoning);
            }
            else
            {
                reasoning.AppendLine("[상인] 현재 상인 없음");
                decision.TradePriority = TradingPriority.None;
            }

            // === 2. 카라반 파견 결정 ===
            DecideCaravan(state, decision, reasoning);

            // === 3. 엔딩 퀘스트 특별 로직 ===
            DecideEndgameStrategy(state, decision, reasoning);

            decision.Reasoning = reasoning.ToString();
            return decision;
        }

        /// <summary>
        /// 방문 상인과의 무역 결정
        /// </summary>
        private static void DecideTradeWithVisitor(ColonyTradingState state, TradingDecision decision, StringBuilder reasoning)
        {
            reasoning.AppendLine($"  실버:{state.TotalSilver} | 판매가치:{state.TotalSellableValue:F0}");

            // 부족 자원 확인
            int shortageCount = 0;
            if (state.SteelShortage) shortageCount++;
            if (state.ComponentShortage) shortageCount++;
            if (state.MedicineShortage) shortageCount++;
            if (state.FoodShortage) shortageCount++;

            reasoning.AppendLine($"  부족:[강철:{state.SteelShortage} 컴포넌트:{state.ComponentShortage} 의약품:{state.MedicineShortage} 식량:{state.FoodShortage}]");

            // 구매 결정
            if (shortageCount > 0 && state.TotalSilver >= MIN_SILVER_FOR_PURCHASE)
            {
                decision.TradePriority = shortageCount >= 2 ? TradingPriority.Critical : TradingPriority.High;
                decision.RecommendedPurchases.AddRange(state.NeededItems.OrderByDescending(i => i.Priority));

                reasoning.AppendLine($"→ 구매 필요! {shortageCount}종 자원 부족");

                // 스토리 로그
                if (shortageCount >= 2)
                {
                    StoryLogger.Trading.CriticalPurchase(shortageCount);
                }
                else
                {
                    StoryLogger.Trading.TraderArrived();
                }
            }

            // 판매 결정
            if (state.TotalSellableValue >= MIN_SELLABLE_VALUE_FOR_TRADE)
            {
                decision.RecommendedSales.AddRange(state.SellableItems.OrderByDescending(i => i.MarketValue));

                if (decision.TradePriority < TradingPriority.Normal)
                {
                    decision.TradePriority = TradingPriority.Normal;
                }

                reasoning.AppendLine($"→ 판매 권장: {state.SellableItems.Count}종 ({state.TotalSellableValue:F0} 가치)");

                // 스토리 로그
                StoryLogger.Trading.SellingSurplus((int)state.TotalSellableValue);
            }

            // 실버 부족으로 구매 불가
            if (shortageCount > 0 && state.TotalSilver < MIN_SILVER_FOR_PURCHASE)
            {
                reasoning.AppendLine("→ 실버 부족! 판매 먼저 필요");

                if (state.TotalSellableValue >= MIN_SELLABLE_VALUE_FOR_TRADE)
                {
                    decision.TradePriority = TradingPriority.High;
                }
            }
        }

        /// <summary>
        /// 카라반 파견 결정
        /// </summary>
        private static void DecideCaravan(ColonyTradingState state, TradingDecision decision, StringBuilder reasoning)
        {
            reasoning.AppendLine($"[카라반] 가용인원:{state.AvailableColonistsForCaravan} 짐동물:{state.PackAnimals} 근처정착지:{state.NearbySettlements.Count}");

            // 카라반 파견 조건 체크
            bool canSendCaravan =
                state.AvailableColonistsForCaravan >= MIN_COLONISTS_FOR_CARAVAN &&
                state.NearbySettlements.Count > 0;

            if (!canSendCaravan)
            {
                reasoning.AppendLine("→ 카라반 파견 불가 (인원/정착지 부족)");
                return;
            }

            // 상인이 없고 자원 부족이 심각하면 카라반 파견 권장
            if (!state.HasVisitingTrader && !state.HasOrbitalTrader)
            {
                int shortageCount = 0;
                if (state.SteelShortage) shortageCount++;
                if (state.ComponentShortage) shortageCount++;
                if (state.MedicineShortage) shortageCount++;

                if (shortageCount >= 2)
                {
                    decision.ShouldSendCaravan = true;
                    decision.CaravanDestination = state.NearbySettlements.FirstOrDefault();

                    reasoning.AppendLine($"→ 카라반 파견 권장: 자원 부족 {shortageCount}종");

                    // 스토리 로그
                    StoryLogger.Trading.CaravanRecommended(decision.CaravanDestination?.Label ?? "근처 정착지");
                }
            }

            // 판매 가치가 높으면 카라반 파견 고려
            if (state.TotalSellableValue >= 2000 && !state.HasVisitingTrader && !state.HasOrbitalTrader)
            {
                decision.ShouldSendCaravan = true;
                decision.CaravanDestination = state.NearbySettlements.FirstOrDefault();

                reasoning.AppendLine($"→ 카라반 파견 권장: 판매 가치 높음 ({state.TotalSellableValue:F0})");
            }
        }

        /// <summary>
        /// 엔딩 퀘스트 특별 전략
        /// </summary>
        private static void DecideEndgameStrategy(ColonyTradingState state, TradingDecision decision, StringBuilder reasoning)
        {
            if (!state.HasEndgameQuest && state.ShipProgress <= 0)
            {
                return;
            }

            reasoning.AppendLine($"[엔딩] 퀘스트:{state.ActiveQuestCount} 엔딩퀘스트:{state.HasEndgameQuest} 우주선:{state.ShipProgress:P0}");

            // 우주선 건설 중이면 컴포넌트/고급 자원 구매 최우선
            if (state.ShipProgress > 0)
            {
                reasoning.AppendLine("→ 우주선 건설 중: 컴포넌트/고급 자원 구매 우선");

                if (state.HasVisitingTrader || state.HasOrbitalTrader)
                {
                    // 고급 컴포넌트, 플라스틸, 골드 구매 추천
                    decision.TradePriority = BoostPriority(decision.TradePriority);

                    // 스토리 로그
                    StoryLogger.Trading.EndgamePriority("우주선 자원");
                }
            }

            // 엔딩 퀘스트 진행 중
            if (state.HasEndgameQuest)
            {
                reasoning.AppendLine("→ 엔딩 퀘스트 진행 중: 퀘스트 자원 확보 우선");

                // 스토리 로그
                StoryLogger.Trading.EndgamePriority("퀘스트 자원");
            }
        }

        /// <summary>
        /// 우선순위 한 단계 상승
        /// </summary>
        private static TradingPriority BoostPriority(TradingPriority current)
        {
            if (current < TradingPriority.Critical)
                return current + 1;
            return current;
        }
    }
}
