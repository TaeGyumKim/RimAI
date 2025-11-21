using System.Collections.Generic;
using RimAI.Core;
using Verse;

namespace RimAI.Trading
{
    /// <summary>
    /// 무역 자동화 서브시스템
    /// 상인 방문 감지, 자원 분석, 무역 권장 사항을 제공합니다.
    ///
    /// 주요 기능:
    /// - 상인 방문 시 자동 분석
    /// - 부족/초과 자원 기반 구매/판매 권장
    /// - 카라반 파견 권장
    /// - 엔딩 퀘스트 지원
    /// </summary>
    public class TradingSubsystem : RimAISubsystemBase
    {
        // 맵별 의사결정 캐시
        private Dictionary<Map, TradingDecision> mapDecisions = new Dictionary<Map, TradingDecision>();
        private Dictionary<Map, ColonyTradingState> mapStates = new Dictionary<Map, ColonyTradingState>();

        // 상인 방문 알림 쿨다운 (중복 알림 방지)
        private Dictionary<Map, int> traderAlertCooldown = new Dictionary<Map, int>();
        private const int ALERT_COOLDOWN_TICKS = 2500; // 약 42초

        public override string Name => "Trading";
        public override int Priority => 40; // 건설보다 낮고 연구보다 높음

        public TradingSubsystem()
        {
            baseUpdateInterval = 600; // 10초마다 업데이트 (무역은 자주 체크 불필요)
        }

        public override void Initialize()
        {
            base.Initialize();
            LogInfo("무역 서브시스템 초기화 완료");
        }

        public override void Update(Map map)
        {
            if (!ShouldUpdate()) return;
            if (!Enabled) return;

            try
            {
                // 1. 상태 분석
                var state = TradingAnalyzer.AnalyzeMap(map);
                var previousState = mapStates.TryGetValue(map, out var prev) ? prev : null;
                mapStates[map] = state;

                // 2. 상인 방문 알림 (새로운 상인이 왔을 때만)
                CheckTraderAlert(map, state, previousState);

                // 3. 의사결정
                var decision = TradingDecisionEngine.MakeDecision(state);
                mapDecisions[map] = decision;

                // 4. 디버그 로그
                LogDetailed($"{state}");
                LogDetailed($"{decision}");
            }
            catch (System.Exception ex)
            {
                LogError($"맵 {map} 업데이트 중 오류: {ex}");
            }
        }

        /// <summary>
        /// 상인 방문 알림 체크
        /// </summary>
        private void CheckTraderAlert(Map map, ColonyTradingState current, ColonyTradingState? previous)
        {
            // 쿨다운 체크
            if (traderAlertCooldown.TryGetValue(map, out int cooldown) && cooldown > 0)
            {
                traderAlertCooldown[map] = cooldown - baseUpdateInterval;
                return;
            }

            // 새로운 상인 감지
            bool newTrader = false;

            if (current.HasVisitingTrader && (previous == null || !previous.HasVisitingTrader))
            {
                newTrader = true;
            }

            if (current.HasOrbitalTrader && (previous == null || !previous.HasOrbitalTrader))
            {
                newTrader = true;
            }

            if (newTrader)
            {
                // 스토리 로그
                StoryLogger.Trading.TraderArrived();

                // 부족 자원이 있으면 추가 알림
                int shortageCount = 0;
                if (current.SteelShortage) shortageCount++;
                if (current.ComponentShortage) shortageCount++;
                if (current.MedicineShortage) shortageCount++;

                if (shortageCount > 0)
                {
                    LogInfo($"상인 방문! 부족 자원 {shortageCount}종 구매 권장");
                }

                // 쿨다운 설정
                traderAlertCooldown[map] = ALERT_COOLDOWN_TICKS;
            }
        }

        public override List<RimAIAction> GetProposedActions(Map map)
        {
            var actions = new List<RimAIAction>();

            if (!mapDecisions.TryGetValue(map, out var decision))
                return actions;

            if (!mapStates.TryGetValue(map, out var state))
                return actions;

            // 무역 우선순위가 높으면 액션 생성
            if (decision.TradePriority >= TradingPriority.High)
            {
                actions.Add(new RimAIAction
                {
                    Type = RimAIActionType.Trade,
                    Priority = decision.TradePriority == TradingPriority.Critical
                        ? RimAIActionPriority.Critical
                        : RimAIActionPriority.High,
                    SourceSubsystem = Name,
                    TargetMap = map,
                    Description = $"무역 권장: 구매 {decision.RecommendedPurchases.Count}종, 판매 {decision.RecommendedSales.Count}종"
                });
            }

            // 카라반 파견 권장
            if (decision.ShouldSendCaravan)
            {
                actions.Add(new RimAIAction
                {
                    Type = RimAIActionType.Caravan,
                    Priority = RimAIActionPriority.Normal,
                    SourceSubsystem = Name,
                    TargetMap = map,
                    Description = $"카라반 파견 권장: {decision.CaravanDestination?.Label ?? "근처 정착지"}"
                });
            }

            return actions;
        }

        public override void ExecuteAction(RimAIAction action)
        {
            // 무역 서브시스템은 주로 정보 제공 역할
            // 실제 무역은 플레이어가 UI로 수행
            LogInfo(action.Description);
        }

        /// <summary>
        /// 특정 맵의 현재 의사결정 가져오기
        /// </summary>
        public TradingDecision? GetDecision(Map map)
        {
            if (mapDecisions.TryGetValue(map, out var decision))
                return decision;
            return null;
        }

        /// <summary>
        /// 특정 맵의 현재 무역 상태 가져오기
        /// </summary>
        public ColonyTradingState? GetState(Map map)
        {
            if (mapStates.TryGetValue(map, out var state))
                return state;
            return null;
        }

        /// <summary>
        /// 상인이 방문 중인지 확인
        /// </summary>
        public bool HasTrader(Map map)
        {
            if (mapStates.TryGetValue(map, out var state))
                return state.HasVisitingTrader || state.HasOrbitalTrader;
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }

        public override string GetDebugInfo(Map map)
        {
            if (!mapStates.TryGetValue(map, out var state))
                return base.GetDebugInfo(map);

            return $"[{Name}] {state}";
        }

        public override void CleanupMap(Map map)
        {
            if (map == null) return;

            mapDecisions.Remove(map);
            mapStates.Remove(map);
            traderAlertCooldown.Remove(map);

            LogInfo($"맵 {map.Index} 데이터 정리 완료");
        }
    }
}
