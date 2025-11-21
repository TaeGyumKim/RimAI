using Verse;

namespace RimAI.Settings
{
    /// <summary>
    /// 자동화 개입 강도
    /// </summary>
    public enum AutomationIntensity
    {
        /// <summary>비활성화</summary>
        Off = 0,
        /// <summary>최소 개입 (긴급 상황만)</summary>
        Low = 1,
        /// <summary>중간 개입 (일반적)</summary>
        Medium = 2,
        /// <summary>높은 개입 (적극적)</summary>
        High = 3,
        /// <summary>완전 자동화 (모든 결정)</summary>
        Full = 4
    }

    /// <summary>
    /// 로그 상세도
    /// </summary>
    public enum LogLevel
    {
        /// <summary>최소 로그 (오류/경고만)</summary>
        Minimal = 0,
        /// <summary>일반 로그 (주요 이벤트)</summary>
        Normal = 1,
        /// <summary>상세 로그 (모든 결정)</summary>
        Detailed = 2,
        /// <summary>디버그 로그 (개발용)</summary>
        Debug = 3
    }

    /// <summary>
    /// 설정 프리셋
    /// </summary>
    public enum RimAIPreset
    {
        /// <summary>사용자 정의</summary>
        Custom = 0,
        /// <summary>완전 방치 관람 모드</summary>
        FullAuto = 1,
        /// <summary>위기만 자동 대응</summary>
        CrisisOnly = 2,
        /// <summary>디버그/실험 모드</summary>
        Debug = 3
    }

    /// <summary>
    /// RimAI 모드 설정 (저장/로드)
    /// </summary>
    public class RimAISettings : ModSettings
    {
        // === 마스터 스위치 ===
        public bool masterEnabled = true;

        // === 서브시스템별 개입 강도 ===
        public AutomationIntensity foodIntensity = AutomationIntensity.High;
        public AutomationIntensity constructionIntensity = AutomationIntensity.High;
        public AutomationIntensity researchIntensity = AutomationIntensity.High;
        public AutomationIntensity combatIntensity = AutomationIntensity.High;
        public AutomationIntensity medicalIntensity = AutomationIntensity.High;
        public AutomationIntensity tradingIntensity = AutomationIntensity.High;
        public AutomationIntensity productionIntensity = AutomationIntensity.High;
        public AutomationIntensity equipmentIntensity = AutomationIntensity.High;
        public AutomationIntensity zoneIntensity = AutomationIntensity.High;

        // === 카메라 설정 ===
        public bool cinematicCameraEnabled = true;

        // === 로그 설정 ===
        public LogLevel logLevel = LogLevel.Debug; // 기본 디버그로 - 무슨 일이 일어나는지 볼 수 있게

        // === 프리셋 ===
        public RimAIPreset currentPreset = RimAIPreset.FullAuto;

        // === 플레이 스타일 (스토리 패턴) ===
        public RimAIPlayStyle playStyle = RimAIPlayStyle.Balanced;

        // === 스토리 로그 활성화 ===
        public bool storyLoggingEnabled = true;

        /// <summary>
        /// 설정 저장
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref masterEnabled, "masterEnabled", true);

            Scribe_Values.Look(ref foodIntensity, "foodIntensity", AutomationIntensity.High);
            Scribe_Values.Look(ref constructionIntensity, "constructionIntensity", AutomationIntensity.High);
            Scribe_Values.Look(ref researchIntensity, "researchIntensity", AutomationIntensity.High);
            Scribe_Values.Look(ref combatIntensity, "combatIntensity", AutomationIntensity.High);
            Scribe_Values.Look(ref medicalIntensity, "medicalIntensity", AutomationIntensity.High);
            Scribe_Values.Look(ref tradingIntensity, "tradingIntensity", AutomationIntensity.High);
            Scribe_Values.Look(ref productionIntensity, "productionIntensity", AutomationIntensity.High);
            Scribe_Values.Look(ref equipmentIntensity, "equipmentIntensity", AutomationIntensity.High);
            Scribe_Values.Look(ref zoneIntensity, "zoneIntensity", AutomationIntensity.High);

            Scribe_Values.Look(ref cinematicCameraEnabled, "cinematicCameraEnabled", true);
            Scribe_Values.Look(ref logLevel, "logLevel", LogLevel.Normal);
            Scribe_Values.Look(ref currentPreset, "currentPreset", RimAIPreset.FullAuto);
            Scribe_Values.Look(ref playStyle, "playStyle", RimAIPlayStyle.Balanced);
            Scribe_Values.Look(ref storyLoggingEnabled, "storyLoggingEnabled", true);
        }

        /// <summary>
        /// 프리셋 적용
        /// </summary>
        public void ApplyPreset(RimAIPreset preset)
        {
            currentPreset = preset;

            switch (preset)
            {
                case RimAIPreset.FullAuto:
                    // 완전 방치 관람 모드
                    masterEnabled = true;
                    foodIntensity = AutomationIntensity.High;
                    constructionIntensity = AutomationIntensity.High;
                    researchIntensity = AutomationIntensity.High;
                    combatIntensity = AutomationIntensity.High;
                    medicalIntensity = AutomationIntensity.High;
                    tradingIntensity = AutomationIntensity.High;
                    productionIntensity = AutomationIntensity.High;
                    equipmentIntensity = AutomationIntensity.High;
                    zoneIntensity = AutomationIntensity.High;
                    cinematicCameraEnabled = true;
                    logLevel = LogLevel.Normal;
                    break;

                case RimAIPreset.CrisisOnly:
                    // 위기만 자동 대응
                    masterEnabled = true;
                    foodIntensity = AutomationIntensity.High;
                    constructionIntensity = AutomationIntensity.Low;
                    researchIntensity = AutomationIntensity.Low;
                    combatIntensity = AutomationIntensity.High;
                    medicalIntensity = AutomationIntensity.High;
                    tradingIntensity = AutomationIntensity.Low;
                    productionIntensity = AutomationIntensity.Low;
                    equipmentIntensity = AutomationIntensity.Medium;
                    zoneIntensity = AutomationIntensity.Low;
                    cinematicCameraEnabled = true;
                    logLevel = LogLevel.Normal;
                    break;

                case RimAIPreset.Debug:
                    // 디버그/실험 모드
                    masterEnabled = true;
                    foodIntensity = AutomationIntensity.Full;
                    constructionIntensity = AutomationIntensity.Full;
                    researchIntensity = AutomationIntensity.Full;
                    combatIntensity = AutomationIntensity.Full;
                    medicalIntensity = AutomationIntensity.Full;
                    tradingIntensity = AutomationIntensity.Full;
                    productionIntensity = AutomationIntensity.Full;
                    equipmentIntensity = AutomationIntensity.Full;
                    zoneIntensity = AutomationIntensity.Full;
                    cinematicCameraEnabled = true;
                    logLevel = LogLevel.Debug;
                    break;

                case RimAIPreset.Custom:
                default:
                    // 사용자 정의는 현재 설정 유지
                    break;
            }
        }

        /// <summary>
        /// 특정 서브시스템이 활성화되어 있는지 확인
        /// </summary>
        public bool IsSubsystemEnabled(string subsystemName)
        {
            if (!masterEnabled) return false;

            switch (subsystemName)
            {
                case "Food":
                    return foodIntensity != AutomationIntensity.Off;
                case "Construction":
                    return constructionIntensity != AutomationIntensity.Off;
                case "Research":
                    return researchIntensity != AutomationIntensity.Off;
                case "Combat":
                    return combatIntensity != AutomationIntensity.Off;
                case "Medical":
                    return medicalIntensity != AutomationIntensity.Off;
                case "Trading":
                    return tradingIntensity != AutomationIntensity.Off;
                case "Production":
                    return productionIntensity != AutomationIntensity.Off;
                case "Equipment":
                    return equipmentIntensity != AutomationIntensity.Off;
                case "ZoneDesignation":
                    return zoneIntensity != AutomationIntensity.Off;
                default:
                    return true; // 알려지지 않은 서브시스템도 기본 활성화
            }
        }

        /// <summary>
        /// 특정 서브시스템의 개입 강도 가져오기
        /// </summary>
        public AutomationIntensity GetIntensity(string subsystemName)
        {
            if (!masterEnabled) return AutomationIntensity.Off;

            switch (subsystemName)
            {
                case "Food":
                    return foodIntensity;
                case "Construction":
                    return constructionIntensity;
                case "Research":
                    return researchIntensity;
                case "Combat":
                    return combatIntensity;
                case "Medical":
                    return medicalIntensity;
                case "Trading":
                    return tradingIntensity;
                case "Production":
                    return productionIntensity;
                case "Equipment":
                    return equipmentIntensity;
                case "ZoneDesignation":
                    return zoneIntensity;
                default:
                    return AutomationIntensity.High; // 기본값은 High
            }
        }

        /// <summary>
        /// 로그 출력 여부 확인
        /// </summary>
        public bool ShouldLog(LogLevel messageLevel)
        {
            return (int)logLevel >= (int)messageLevel;
        }

        /// <summary>
        /// 개입 강도에 따른 업데이트 간격 배수 계산
        /// </summary>
        /// <param name="intensity">개입 강도</param>
        /// <returns>기본 간격에 곱할 배수 (낮을수록 더 자주 업데이트)</returns>
        public static float GetUpdateIntervalMultiplier(AutomationIntensity intensity)
        {
            switch (intensity)
            {
                case AutomationIntensity.Off:
                    return float.MaxValue; // 사실상 비활성화
                case AutomationIntensity.Low:
                    return 3.0f; // 3배 느림 (긴급 상황만)
                case AutomationIntensity.Medium:
                    return 1.5f; // 1.5배 느림
                case AutomationIntensity.High:
                    return 1.0f; // 정상 속도
                case AutomationIntensity.Full:
                    return 0.5f; // 2배 빠름 (적극적)
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// 개입 강도에 따른 우선순위 배수 계산
        /// </summary>
        public static float GetPriorityMultiplier(AutomationIntensity intensity)
        {
            switch (intensity)
            {
                case AutomationIntensity.Off:
                    return 0f;
                case AutomationIntensity.Low:
                    return 0.5f; // 우선순위 절반
                case AutomationIntensity.Medium:
                    return 0.8f;
                case AutomationIntensity.High:
                    return 1.0f;
                case AutomationIntensity.Full:
                    return 1.5f; // 우선순위 50% 증가
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// 플레이 스타일 적용 (프리셋과 별개로 작동)
        /// </summary>
        public void ApplyPlayStyle(RimAIPlayStyle style)
        {
            playStyle = style;
            currentPreset = RimAIPreset.Custom; // 스타일 변경 시 프리셋은 Custom으로

            // 스타일별로 개입 강도 자동 조정은 하지 않음
            // 대신 서브시스템에서 playStyle을 참조하여 행동을 변경
        }

        /// <summary>
        /// 현재 플레이 스타일의 서브시스템 우선순위 가져오기
        /// </summary>
        public (int combat, int food, int construction, int research) GetPlayStylePriorities()
        {
            return RimAIPlayStyleProfile.SubsystemWeights.GetWeights(playStyle);
        }

        /// <summary>
        /// 스토리 로그 출력 여부
        /// </summary>
        public bool ShouldLogStory()
        {
            return storyLoggingEnabled && (int)logLevel >= (int)LogLevel.Normal;
        }
    }
}
