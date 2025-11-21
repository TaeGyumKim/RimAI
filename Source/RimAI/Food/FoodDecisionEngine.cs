using System.Text;
using RimAI.Core;
using Verse;

namespace RimAI.Food
{
    /// <summary>
    /// 식량 관련 작업 우선순위를 결정하는 의사결정 엔진
    ///
    /// 핵심 알고리즘:
    /// 1. 식량 위기 레벨에 따라 즉각 대응 작업 우선순위 상승 (요리, 사냥)
    /// 2. 중장기 대응: 농사 가능 시기에 농장 활용도 최대화
    /// 3. 계절 고려: 겨울 대비 수확/저장 우선순위 상승
    /// 4. 작업 균형: 같은 작업에 너무 많은 폰이 몰리면 우선순위 하락
    /// </summary>
    public static class FoodDecisionEngine
    {
        // === 임계값 설정 ===
        private const float FARM_UTILIZATION_TARGET = 0.9f; // 농장 90% 활용 목표
        private const float FARM_UTILIZATION_MINIMUM = 0.5f; // 최소 50% 활용
        private const int MAX_WORKERS_PER_TASK = 3; // 한 작업당 최대 폰 수

        /// <summary>
        /// 콜로니 식량 상태를 분석하여 작업 우선순위 결정
        /// </summary>
        /// <param name="state">분석된 식량 상태</param>
        /// <returns>작업 우선순위 결정 결과</returns>
        public static FoodDecision MakeDecision(ColonyFoodState state)
        {
            var decision = new FoodDecision();
            var reasoning = new StringBuilder();

            // === 1. 즉각 대응: 식량 위기 레벨 기반 ===
            var crisisLevel = state.GetCrisisLevel();
            reasoning.AppendLine($"[위기 레벨: {crisisLevel}] 생존 일수: {state.DaysUntilStarvation:F1}일");

            switch (crisisLevel)
            {
                case 3: // 위기 (1일 미만)
                    reasoning.AppendLine("→ 위기! 즉시 식량 확보 필요");
                    decision.CookingPriority = FoodWorkPriority.Critical;
                    decision.HuntingPriority = FoodWorkPriority.Critical;
                    decision.HarvestingPriority = FoodWorkPriority.Critical;
                    decision.ForagingPriority = FoodWorkPriority.Critical;

                    // 스토리 로그
                    StoryLogger.Food.Emergency(state.DaysUntilStarvation);
                    break;

                case 2: // 경고 (3일 미만)
                    reasoning.AppendLine("→ 경고! 식량 확보 우선");
                    decision.CookingPriority = FoodWorkPriority.High;
                    decision.HuntingPriority = FoodWorkPriority.High;
                    decision.HarvestingPriority = FoodWorkPriority.High;
                    decision.ForagingPriority = FoodWorkPriority.Normal;

                    // 스토리 로그
                    StoryLogger.Food.Warning(state.DaysUntilStarvation);
                    break;

                case 1: // 주의 (7일 미만)
                    reasoning.AppendLine("→ 주의: 식량 보충 권장");
                    decision.CookingPriority = FoodWorkPriority.Normal;
                    decision.HuntingPriority = FoodWorkPriority.Normal;
                    decision.HarvestingPriority = FoodWorkPriority.Normal;
                    break;

                default: // 안전
                    reasoning.AppendLine("→ 안전: 정상 운영");
                    decision.CookingPriority = FoodWorkPriority.Low;
                    decision.HuntingPriority = FoodWorkPriority.Low;
                    decision.HarvestingPriority = FoodWorkPriority.Normal;

                    // 식량 과잉 시 스토리 로그 (10일 이상)
                    if (state.DaysUntilStarvation > 10f)
                    {
                        StoryLogger.Food.Surplus();
                    }
                    break;
            }

            // === 2. 중장기 대응: 농사 우선순위 ===
            DecideFarmingPriority(state, decision, reasoning);

            // === 3. 계절 고려: 겨울 대비 ===
            AdjustForSeason(state, decision, reasoning);

            // === 4. 작업 균형: 폰 분산 ===
            BalanceWorkload(state, decision, reasoning);

            // === 5. 생고기 vs 조리된 식사 균형 ===
            AdjustCookingPriority(state, decision, reasoning);

            decision.Reasoning = reasoning.ToString();
            return decision;
        }

        /// <summary>
        /// 농사 작업 우선순위 결정
        /// </summary>
        private static void DecideFarmingPriority(ColonyFoodState state, FoodDecision decision, StringBuilder reasoning)
        {
            // 농사가 불가능한 환경이면 우선순위 낮춤
            if (!state.CanGrow)
            {
                reasoning.AppendLine("[농사] 농사 불가능 (겨울 또는 온도 낮음)");
                decision.SowingPriority = FoodWorkPriority.None;
                return;
            }

            var utilization = state.GetFarmUtilization();
            reasoning.AppendLine($"[농사] 활용률: {utilization:P0} ({state.SownTiles}/{state.TotalFarmTiles})");

            // 농장이 없으면 우선순위 없음
            if (state.TotalFarmTiles == 0)
            {
                reasoning.AppendLine("→ 농장 없음");
                decision.SowingPriority = FoodWorkPriority.None;
                return;
            }

            // 활용률에 따라 파종 우선순위 결정
            if (utilization < FARM_UTILIZATION_MINIMUM)
            {
                reasoning.AppendLine("→ 농장 활용률 매우 낮음! 파종 최우선");
                decision.SowingPriority = FoodWorkPriority.Critical;
            }
            else if (utilization < FARM_UTILIZATION_TARGET)
            {
                reasoning.AppendLine("→ 농장 활용률 낮음, 파종 우선");
                decision.SowingPriority = FoodWorkPriority.High;
            }
            else
            {
                reasoning.AppendLine("→ 농장 활용률 양호");
                decision.SowingPriority = FoodWorkPriority.Normal;
            }

            // 수확 가능한 작물이 많으면 수확 우선순위 상승
            if (state.HarvestableCrops > state.ColonistCount * 10)
            {
                reasoning.AppendLine($"→ 수확 가능 작물 많음 ({state.HarvestableCrops}개)");
                decision.HarvestingPriority = FoodWorkPriority.High;
            }
        }

        /// <summary>
        /// 계절에 따른 우선순위 조정
        /// </summary>
        private static void AdjustForSeason(ColonyFoodState state, FoodDecision decision, StringBuilder reasoning)
        {
            reasoning.AppendLine($"[계절] {state.CurrentSeason}, 겨울까지 {state.DaysUntilWinter}일");

            // 겨울이 임박하면 수확 및 식량 저장 우선순위 상승
            if (state.DaysUntilWinter > 0 && state.DaysUntilWinter <= 5)
            {
                reasoning.AppendLine("→ 겨울 임박! 수확 및 저장 최우선");
                decision.HarvestingPriority = FoodWorkPriority.Critical;
                decision.CookingPriority = BoostPriority(decision.CookingPriority);

                // 스토리 로그
                StoryLogger.Food.WinterPreparation(state.DaysUntilWinter);
            }
            else if (state.DaysUntilWinter > 0 && state.DaysUntilWinter <= 15)
            {
                reasoning.AppendLine("→ 겨울 대비 필요");
                decision.HarvestingPriority = BoostPriority(decision.HarvestingPriority);

                // 스토리 로그 (15일 때만)
                if (state.DaysUntilWinter == 15)
                {
                    StoryLogger.Food.WinterPreparation(state.DaysUntilWinter);
                }
            }

            // 봄에는 파종 우선순위 상승
            if (state.CurrentSeason == RimWorld.Season.Spring && state.CanGrow)
            {
                reasoning.AppendLine("→ 봄: 파종 적기");
                decision.SowingPriority = BoostPriority(decision.SowingPriority);

                // 스토리 로그
                StoryLogger.Food.SpringSowing();
            }

            // 수확 시즌
            if (state.HarvestableCrops > state.ColonistCount * 5)
            {
                StoryLogger.Food.HarvestSeason();
            }
        }

        /// <summary>
        /// 작업 균형 조정 - 한 작업에 너무 많은 폰이 몰리는 것 방지
        /// </summary>
        private static void BalanceWorkload(ColonyFoodState state, FoodDecision decision, StringBuilder reasoning)
        {
            reasoning.AppendLine($"[작업중] 파종:{state.PawnsSowing} 수확:{state.PawnsHarvesting} " +
                               $"요리:{state.PawnsCooking} 사냥:{state.PawnsHunting}");

            // 이미 충분한 폰이 작업 중이면 우선순위 낮춤
            if (state.PawnsSowing >= MAX_WORKERS_PER_TASK)
            {
                reasoning.AppendLine("→ 파종 인력 충분");
                decision.SowingPriority = ReducePriority(decision.SowingPriority);
            }

            if (state.PawnsHarvesting >= MAX_WORKERS_PER_TASK)
            {
                reasoning.AppendLine("→ 수확 인력 충분");
                decision.HarvestingPriority = ReducePriority(decision.HarvestingPriority);
            }

            if (state.PawnsCooking >= MAX_WORKERS_PER_TASK)
            {
                reasoning.AppendLine("→ 요리 인력 충분");
                decision.CookingPriority = ReducePriority(decision.CookingPriority);
            }

            if (state.PawnsHunting >= MAX_WORKERS_PER_TASK)
            {
                reasoning.AppendLine("→ 사냥 인력 충분");
                decision.HuntingPriority = ReducePriority(decision.HuntingPriority);
            }
        }

        /// <summary>
        /// 요리 우선순위 조정 - 생고기 vs 조리된 식사 균형
        /// </summary>
        private static void AdjustCookingPriority(ColonyFoodState state, FoodDecision decision, StringBuilder reasoning)
        {
            // 조리된 식사가 충분하면 요리 우선순위 낮춤
            var mealRatio = state.TotalNutrition > 0 ? state.MealNutrition / state.TotalNutrition : 0f;
            reasoning.AppendLine($"[요리] 식사 비율: {mealRatio:P0} (식사:{state.MealNutrition:F1} / 전체:{state.TotalNutrition:F1})");

            if (mealRatio > 0.7f)
            {
                reasoning.AppendLine("→ 조리된 식사 충분");
                decision.CookingPriority = ReducePriority(decision.CookingPriority);
            }
            else if (mealRatio < 0.3f && state.RawFoodNutrition > 5f)
            {
                reasoning.AppendLine("→ 생고기 많음, 요리 필요");
                decision.CookingPriority = BoostPriority(decision.CookingPriority);
            }
        }

        /// <summary>
        /// 우선순위 한 단계 상승
        /// </summary>
        private static FoodWorkPriority BoostPriority(FoodWorkPriority current)
        {
            if (current < FoodWorkPriority.Critical)
                return current + 1;
            return current;
        }

        /// <summary>
        /// 우선순위 한 단계 하락
        /// </summary>
        private static FoodWorkPriority ReducePriority(FoodWorkPriority current)
        {
            if (current > FoodWorkPriority.None)
                return current - 1;
            return current;
        }
    }
}
