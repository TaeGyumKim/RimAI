using System.Collections.Generic;
using RimAI.Settings;
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

        /// <summary>
        /// 특정 맵 데이터 정리 (맵 언로드 시 호출, 메모리 누수 방지)
        /// </summary>
        void CleanupMap(Map map);
    }

    /// <summary>
    /// 서브시스템 기본 추상 클래스
    /// 공통 로직을 제공합니다.
    /// </summary>
    public abstract class RimAISubsystemBase : IRimAISubsystem
    {
        protected int tickCounter = 0;
        protected int baseUpdateInterval = 300; // 기본 5초
        protected int updateInterval = 300;
        private bool _enabled = true;

        public abstract string Name { get; }
        public abstract int Priority { get; }
        public bool Enabled { get => _enabled; set => _enabled = value; }

        /// <summary>
        /// 설정 참조
        /// </summary>
        protected RimAISettings Settings => RimAI_Mod.Settings;

        /// <summary>
        /// 현재 서브시스템의 개입 강도
        /// </summary>
        protected AutomationIntensity CurrentIntensity => Settings.GetIntensity(Name);

        public virtual void Initialize()
        {
            // 개입 강도에 따라 업데이트 간격 조정
            UpdateIntervalFromSettings();

            LogInfo($"{Name} 서브시스템 초기화 (강도: {CurrentIntensity}, 간격: {updateInterval}틱)");
        }

        public abstract void Update(Map map);
        public abstract List<RimAIAction> GetProposedActions(Map map);
        public abstract void ExecuteAction(RimAIAction action);

        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref _enabled, $"rimai_{Name}_enabled", true);
        }

        public virtual string GetDebugInfo(Map map)
        {
            return $"[{Name}] Enabled: {Enabled}, Intensity: {CurrentIntensity}";
        }

        /// <summary>
        /// 틱 카운터 증가 및 업데이트 필요 여부 확인
        /// </summary>
        protected bool ShouldUpdate()
        {
            // 설정에서 비활성화되어 있으면 업데이트 안 함
            if (!Settings.IsSubsystemEnabled(Name))
            {
                return false;
            }

            tickCounter++;
            if (tickCounter >= updateInterval)
            {
                tickCounter = 0;
                UpdateIntervalFromSettings(); // 간격 재계산
                return true;
            }
            return false;
        }

        /// <summary>
        /// 설정에서 업데이트 간격 업데이트
        /// </summary>
        protected void UpdateIntervalFromSettings()
        {
            float multiplier = RimAISettings.GetUpdateIntervalMultiplier(CurrentIntensity);
            updateInterval = (int)(baseUpdateInterval * multiplier);
        }

        /// <summary>
        /// 로그 출력 헬퍼 (로그 레벨 고려)
        /// </summary>
        protected void LogInfo(string message)
        {
            if (Settings.ShouldLog(LogLevel.Normal))
            {
                Log.Message($"[RimAI-{Name}] {message}");
            }
        }

        /// <summary>
        /// 상세 로그 출력
        /// </summary>
        protected void LogDetailed(string message)
        {
            if (Settings.ShouldLog(LogLevel.Detailed))
            {
                Log.Message($"[RimAI-{Name}] {message}");
            }
        }

        /// <summary>
        /// 디버그 로그 출력
        /// </summary>
        protected void LogDebug(string message)
        {
            if (Settings.ShouldLog(LogLevel.Debug))
            {
                Log.Message($"[RimAI-{Name}-DEBUG] {message}");
            }
        }

        /// <summary>
        /// 경고 로그 (항상 출력)
        /// </summary>
        protected void LogWarning(string message)
        {
            Log.Warning($"[RimAI-{Name}] {message}");
        }

        /// <summary>
        /// 오류 로그 (항상 출력)
        /// </summary>
        protected void LogError(string message)
        {
            Log.Error($"[RimAI-{Name}] {message}");
        }

        /// <summary>
        /// 특정 맵 데이터 정리 (메모리 누수 방지)
        /// 서브시스템에서 오버라이드하여 맵별 캐시 정리
        /// </summary>
        public virtual void CleanupMap(Map map)
        {
            // 기본 구현: 아무것도 하지 않음
            // 서브시스템에서 필요시 오버라이드
        }
    }
}
