using System.Collections.Generic;
using Verse;

namespace RimAI.Core
{
    /// <summary>
    /// RimAI 서브시스템 인터페이스
    /// 모든 자동화 서브시스템(식량, 건설, 연구 등)이 구현해야 하는 공통 인터페이스
    /// </summary>
    public interface IRimAISubsystem
    {
        /// <summary>
        /// 서브시스템 이름
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 서브시스템 우선순위 (높을수록 우선)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 서브시스템 활성화 여부
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// 서브시스템 초기화
        /// </summary>
        void Initialize();

        /// <summary>
        /// 주기적 업데이트 (중앙 브레인이 호출)
        /// </summary>
        /// <param name="map">대상 맵</param>
        void Update(Map map);

        /// <summary>
        /// 현재 제안하는 액션 목록 가져오기
        /// </summary>
        /// <param name="map">대상 맵</param>
        /// <returns>제안 액션 리스트</returns>
        List<RimAIAction> GetProposedActions(Map map);

        /// <summary>
        /// 액션 실행 (중앙 브레인의 승인 후)
        /// </summary>
        /// <param name="action">실행할 액션</param>
        void ExecuteAction(RimAIAction action);

        /// <summary>
        /// 세이브 파일 저장/로드
        /// </summary>
        void ExposeData();

        /// <summary>
        /// 디버그 정보 문자열
        /// </summary>
        string GetDebugInfo(Map map);
    }

    /// <summary>
    /// 서브시스템 기본 추상 클래스
    /// 공통 로직을 제공합니다.
    /// </summary>
    public abstract class RimAISubsystemBase : IRimAISubsystem
    {
        protected int tickCounter = 0;
        protected int updateInterval = 300; // 기본 5초

        public abstract string Name { get; }
        public abstract int Priority { get; }
        public bool Enabled { get; set; } = true;

        public virtual void Initialize()
        {
            Log.Message($"[RimAI] {Name} 서브시스템 초기화");
        }

        public abstract void Update(Map map);
        public abstract List<RimAIAction> GetProposedActions(Map map);
        public abstract void ExecuteAction(RimAIAction action);

        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref Enabled, $"rimai_{Name}_enabled", true);
        }

        public virtual string GetDebugInfo(Map map)
        {
            return $"[{Name}] Enabled: {Enabled}";
        }

        /// <summary>
        /// 틱 카운터 증가 및 업데이트 필요 여부 확인
        /// </summary>
        protected bool ShouldUpdate()
        {
            tickCounter++;
            if (tickCounter >= updateInterval)
            {
                tickCounter = 0;
                return true;
            }
            return false;
        }
    }
}
