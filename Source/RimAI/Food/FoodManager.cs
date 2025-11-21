using System.Collections.Generic;
using Verse;

namespace RimAI.Food
{
    /// <summary>
    /// 식량 자동화 시스템의 중앙 관리자
    /// 주기적으로 콜로니 상태를 분석하고 의사결정을 업데이트합니다.
    /// </summary>
    public class FoodManager : GameComponent
    {
        // 업데이트 주기 (틱 단위, 60 틱 = 1초)
        private const int UPDATE_INTERVAL = 300; // 5초마다 업데이트

        // 맵별 의사결정 캐시
        private Dictionary<Map, FoodDecision> mapDecisions = new Dictionary<Map, FoodDecision>();
        private Dictionary<Map, ColonyFoodState> mapStates = new Dictionary<Map, ColonyFoodState>();

        private int tickCounter = 0;

        public FoodManager(Game game)
        {
        }

        /// <summary>
        /// 매 틱마다 호출됨
        /// </summary>
        public override void GameComponentTick()
        {
            base.GameComponentTick();

            tickCounter++;

            // 일정 주기마다 업데이트
            if (tickCounter >= UPDATE_INTERVAL)
            {
                tickCounter = 0;
                UpdateAllMaps();
            }
        }

        /// <summary>
        /// 모든 맵의 식량 상태를 분석하고 의사결정 업데이트
        /// </summary>
        private void UpdateAllMaps()
        {
            if (Current.Game == null) return;

            foreach (var map in Find.Maps)
            {
                // 플레이어 콜로니가 있는 맵만 처리
                if (!map.IsPlayerHome) continue;

                try
                {
                    UpdateMap(map);
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[RimAI] 맵 {map} 업데이트 중 오류: {ex}");
                }
            }
        }

        /// <summary>
        /// 특정 맵의 상태 분석 및 의사결정 업데이트
        /// </summary>
        private void UpdateMap(Map map)
        {
            // 1. 상태 분석
            var state = FoodAnalyzer.AnalyzeMap(map);
            mapStates[map] = state;

            // 2. 의사결정
            var decision = FoodDecisionEngine.MakeDecision(state);
            mapDecisions[map] = decision;

            // 3. 디버그 로그 (개발 중에만)
            if (Prefs.DevMode && RimAI_Settings.EnableDetailedLogging)
            {
                Log.Message($"[RimAI] {state}");
                Log.Message($"[RimAI] {decision}");
            }
        }

        /// <summary>
        /// 특정 맵의 현재 의사결정 가져오기
        /// </summary>
        public FoodDecision? GetDecision(Map map)
        {
            if (mapDecisions.TryGetValue(map, out var decision))
                return decision;
            return null;
        }

        /// <summary>
        /// 특정 맵의 현재 식량 상태 가져오기
        /// </summary>
        public ColonyFoodState? GetState(Map map)
        {
            if (mapStates.TryGetValue(map, out var state))
                return state;
            return null;
        }

        /// <summary>
        /// 세이브 파일 저장 시
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            // 필요시 상태 저장/로드 로직 추가
        }

        /// <summary>
        /// 싱글톤 인스턴스 접근
        /// </summary>
        public static FoodManager? Instance
        {
            get
            {
                if (Current.Game == null) return null;
                return Current.Game.GetComponent<FoodManager>();
            }
        }
    }

    /// <summary>
    /// RimAI 설정
    /// </summary>
    public static class RimAI_Settings
    {
        /// <summary>상세 로그 활성화 여부</summary>
        public static bool EnableDetailedLogging = true;

        /// <summary>식량 자동화 활성화 여부</summary>
        public static bool EnableFoodAutomation = true;
    }
}
