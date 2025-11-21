using System.Collections.Generic;
using RimAI.Core;
using Verse;

namespace RimAI.Food
{
    /// <summary>
    /// 식량 자동화 서브시스템
    /// 식량 상태를 분석하고 작업 우선순위를 조정합니다.
    /// </summary>
    public class FoodSubsystem : RimAISubsystemBase
    {
        // 맵별 의사결정 캐시
        private Dictionary<Map, FoodDecision> mapDecisions = new Dictionary<Map, FoodDecision>();
        private Dictionary<Map, ColonyFoodState> mapStates = new Dictionary<Map, ColonyFoodState>();

        public override string Name => "Food";
        public override int Priority => 100; // 가장 높은 우선순위 (생존 필수)

        public FoodSubsystem()
        {
            updateInterval = 300; // 5초마다 업데이트
        }

        public override void Initialize()
        {
            base.Initialize();
            Enabled = RimAI_Settings.EnableFoodAutomation;
        }

        public override void Update(Map map)
        {
            if (!ShouldUpdate()) return;
            if (!Enabled) return;

            try
            {
                // 1. 상태 분석
                var state = FoodAnalyzer.AnalyzeMap(map);
                mapStates[map] = state;

                // 2. 의사결정
                var decision = FoodDecisionEngine.MakeDecision(state);
                mapDecisions[map] = decision;

                // 3. 디버그 로그
                if (Prefs.DevMode && RimAI_Settings.EnableDetailedLogging)
                {
                    Log.Message($"[RimAI-Food] {state}");
                    Log.Message($"[RimAI-Food] {decision}");
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RimAI-Food] 맵 {map} 업데이트 중 오류: {ex}");
            }
        }

        public override List<RimAIAction> GetProposedActions(Map map)
        {
            var actions = new List<RimAIAction>();

            if (!mapDecisions.TryGetValue(map, out var decision))
                return actions;

            // 식량 서브시스템은 WorkGiver 패치를 통해 직접 작동하므로
            // 특별한 액션을 제안하지 않습니다.
            // 대신 의사결정 정보를 다른 시스템이 참조할 수 있도록 합니다.

            // 예외: Critical 상황에서는 경고 액션 생성
            if (decision.HarvestingPriority == FoodWorkPriority.Critical ||
                decision.CookingPriority == FoodWorkPriority.Critical)
            {
                var state = mapStates[map];
                actions.Add(new RimAIAction
                {
                    Type = RimAIActionType.EmergencyResponse,
                    Priority = RimAIActionPriority.Critical,
                    SourceSubsystem = Name,
                    TargetMap = map,
                    Description = $"식량 위기! 생존 {state.DaysUntilStarvation:F1}일"
                });
            }

            return actions;
        }

        public override void ExecuteAction(RimAIAction action)
        {
            // 식량 서브시스템은 WorkGiver 패치를 통해 자동 실행되므로
            // 특별한 실행 로직이 필요 없습니다.
            if (action.Type == RimAIActionType.EmergencyResponse)
            {
                // 긴급 상황 로그만 출력
                Log.Warning($"[RimAI-Food] {action.Description}");
            }
        }

        /// <summary>
        /// 특정 맵의 현재 의사결정 가져오기 (외부 접근용)
        /// </summary>
        public FoodDecision? GetDecision(Map map)
        {
            if (mapDecisions.TryGetValue(map, out var decision))
                return decision;
            return null;
        }

        /// <summary>
        /// 특정 맵의 현재 식량 상태 가져오기 (외부 접근용)
        /// </summary>
        public ColonyFoodState? GetState(Map map)
        {
            if (mapStates.TryGetValue(map, out var state))
                return state;
            return null;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // 필요시 상태 저장/로드 로직 추가
        }

        public override string GetDebugInfo(Map map)
        {
            if (!mapStates.TryGetValue(map, out var state))
                return base.GetDebugInfo(map);

            return $"[{Name}] {state}";
        }
    }
}
