using RimAI.Settings;

namespace RimAI.GA
{
    /// <summary>
    /// Genome 프리셋 타입
    /// 사용자 선택 또는 GA 최적화 결과에 따라 결정
    /// </summary>
    public enum GenomePresetType
    {
        /// <summary>기본값 (균형)</summary>
        Default,

        /// <summary>빠른 엔딩 지향 (효율적 리소스 사용)</summary>
        FastEnding,

        /// <summary>안정적 엔딩 지향 (리스크 최소화)</summary>
        SafeEnding,

        /// <summary>관람용 스토리 중시 (드라마틱한 전개)</summary>
        Entertaining,

        /// <summary>방어적 플레이 (Fortress 스타일)</summary>
        Defensive,

        /// <summary>연구 집중 (Researcher 스타일)</summary>
        ResearchFocused,

        /// <summary>GA 최적화 결과 (current.json에서 로드)</summary>
        GAOptimized,

        /// <summary>사용자 정의</summary>
        Custom
    }

    /// <summary>
    /// Genome 프리셋 생성 및 관리
    /// </summary>
    public static class GenomePresets
    {
        /// <summary>
        /// 프리셋에 맞는 Genome 생성
        /// </summary>
        public static RimAIGenome CreateFromPreset(GenomePresetType presetType)
        {
            var genome = new RimAIGenome
            {
                GenomeId = $"preset_{presetType.ToString().ToLower()}",
                Generation = 0
            };

            switch (presetType)
            {
                case GenomePresetType.FastEnding:
                    ApplyFastEndingPreset(genome);
                    break;

                case GenomePresetType.SafeEnding:
                    ApplySafeEndingPreset(genome);
                    break;

                case GenomePresetType.Entertaining:
                    ApplyEntertainingPreset(genome);
                    break;

                case GenomePresetType.Defensive:
                    ApplyDefensivePreset(genome);
                    break;

                case GenomePresetType.ResearchFocused:
                    ApplyResearchFocusedPreset(genome);
                    break;

                case GenomePresetType.Default:
                default:
                    // 기본값 그대로 사용
                    break;
            }

            return genome;
        }

        /// <summary>
        /// 빠른 엔딩 지향 프리셋
        /// - 연구 우선순위 높음
        /// - 건설 효율 중시
        /// - 리스크 테이킹
        /// </summary>
        private static void ApplyFastEndingPreset(RimAIGenome genome)
        {
            genome.GenomeId = "preset_fast_ending";

            // 서브시스템 가중치: 연구 > 생산 > 건설 > 식량 > 전투
            genome.ResearchPriorityWeight = 1.4f;
            genome.ProductionPriorityWeight = 1.2f;
            genome.ConstructionPriorityWeight = 1.1f;
            genome.FoodPriorityWeight = 0.9f;
            genome.CombatPriorityWeight = 0.8f;

            // 식량: 최소한만 유지
            genome.FoodCrisisThreshold = 3.0f;  // 위기 임계값 낮춤
            genome.FoodWarningThreshold = 5.0f;
            genome.FoodStableThreshold = 10.0f; // 안정 임계값 낮춤
            genome.WinterPrepDays = 20;         // 겨울 대비 기간 줄임

            // 건설: 빠르게 진행
            genome.ConstructionCooldown = 2400; // 쿨다운 줄임

            // 연구 우선순위: 경제/기술 중심
            genome.ResearchEconomyPriority = 1.4f;
            genome.ResearchCombatPriority = 0.8f;
            genome.ResearchMedicalPriority = 0.9f;

            // 플레이 스타일: 연구 편향
            genome.ResearchBias = 1.4f;
            genome.DefensiveBias = 0.7f;
            genome.ExpansionBias = 1.2f;
            genome.WelfareBias = 0.8f;
        }

        /// <summary>
        /// 안정적 엔딩 지향 프리셋
        /// - 생존 최우선
        /// - 리스크 회피
        /// - 충분한 자원 비축
        /// </summary>
        private static void ApplySafeEndingPreset(RimAIGenome genome)
        {
            genome.GenomeId = "preset_safe_ending";

            // 서브시스템 가중치: 식량 > 전투 > 건설 > 생산 > 연구
            genome.FoodPriorityWeight = 1.3f;
            genome.CombatPriorityWeight = 1.2f;
            genome.ConstructionPriorityWeight = 1.1f;
            genome.ProductionPriorityWeight = 1.0f;
            genome.ResearchPriorityWeight = 0.9f;

            // 식량: 넉넉하게 유지
            genome.FoodCrisisThreshold = 5.0f;
            genome.FoodWarningThreshold = 10.0f;
            genome.FoodStableThreshold = 20.0f; // 안정 임계값 높임
            genome.WinterPrepDays = 40;         // 겨울 대비 기간 늘림

            // 자원: 넉넉하게 비축
            genome.SteelShortageThreshold = 150;
            genome.ComponentShortageThreshold = 8;
            genome.SteelSurplusThreshold = 700;
            genome.ComponentSurplusThreshold = 30;

            // 전투: 조심스럽게
            genome.ThreatDetectionRange = 35f;  // 위협 인식 거리 늘림
            genome.CombatStartThreshold = 2;    // 낮은 임계값 (빠른 대응)

            // 플레이 스타일: 방어/복지 편향
            genome.DefensiveBias = 1.3f;
            genome.WelfareBias = 1.2f;
            genome.ResearchBias = 0.9f;
            genome.ExpansionBias = 0.8f;
        }

        /// <summary>
        /// 관람용 스토리 중시 프리셋
        /// - 드라마틱한 전개
        /// - 높은 무드 유지
        /// - 적절한 긴장감
        /// </summary>
        private static void ApplyEntertainingPreset(RimAIGenome genome)
        {
            genome.GenomeId = "preset_entertaining";

            // 서브시스템 가중치: 균형있게 (약간의 변동성)
            genome.FoodPriorityWeight = 1.1f;
            genome.CombatPriorityWeight = 1.0f;
            genome.ConstructionPriorityWeight = 1.0f;
            genome.ProductionPriorityWeight = 1.0f;
            genome.ResearchPriorityWeight = 1.0f;

            // 식량: 균형 (너무 여유롭지 않게)
            genome.FoodCrisisThreshold = 4.0f;
            genome.FoodWarningThreshold = 7.0f;
            genome.FoodStableThreshold = 15.0f;

            // 플레이 스타일: 복지 중시 (무드 관리)
            genome.WelfareBias = 1.4f;         // 무드 관리 강화
            genome.DefensiveBias = 1.0f;
            genome.ResearchBias = 1.0f;
            genome.ExpansionBias = 1.0f;

            // 업데이트 속도: 약간 빠르게 (반응적인 플레이)
            genome.UpdateSpeedMultiplier = 0.9f;
        }

        /// <summary>
        /// 방어적 플레이 프리셋 (Fortress)
        /// - 요새 건설
        /// - 방어 시설 집중
        /// - 보수적 확장
        /// </summary>
        private static void ApplyDefensivePreset(RimAIGenome genome)
        {
            genome.GenomeId = "preset_defensive";

            // 서브시스템 가중치: 전투 > 건설 > 식량 > 생산 > 연구
            genome.CombatPriorityWeight = 1.4f;
            genome.ConstructionPriorityWeight = 1.3f;
            genome.FoodPriorityWeight = 1.0f;
            genome.ProductionPriorityWeight = 0.9f;
            genome.ResearchPriorityWeight = 0.8f;

            // 건설: 방어 시설 많이
            genome.BedBuffer = 3;              // 여유 침대
            genome.DefensePerColonist = 3.0f;  // 방어 시설 증가
            genome.ConstructionCooldown = 3000; // 적당한 쿨다운

            // 전투: 적극적 대응
            genome.ThreatDetectionRange = 40f; // 넓은 위협 인식
            genome.CombatStartThreshold = 2;
            genome.CombatEndDelay = 3000;      // 전투 종료 대기 늘림

            // 연구 우선순위: 전투 기술 집중
            genome.ResearchCombatPriority = 1.5f;
            genome.ResearchEconomyPriority = 0.8f;
            genome.ResearchMedicalPriority = 1.0f;

            // 플레이 스타일: 방어 편향
            genome.DefensiveBias = 1.5f;
            genome.ExpansionBias = 0.6f;
            genome.ResearchBias = 0.8f;
            genome.WelfareBias = 1.0f;
        }

        /// <summary>
        /// 연구 집중 프리셋
        /// - 빠른 기술 발전
        /// - 연구 시설 우선
        /// - 첨단 장비 추구
        /// </summary>
        private static void ApplyResearchFocusedPreset(RimAIGenome genome)
        {
            genome.GenomeId = "preset_research_focused";

            // 서브시스템 가중치: 연구 > 생산 > 식량 > 건설 > 전투
            genome.ResearchPriorityWeight = 1.5f;
            genome.ProductionPriorityWeight = 1.1f;
            genome.FoodPriorityWeight = 1.0f;
            genome.ConstructionPriorityWeight = 0.9f;
            genome.CombatPriorityWeight = 0.8f;

            // 생산: 컴포넌트 집중
            genome.ComponentShortageThreshold = 10;
            genome.ComponentSurplusThreshold = 40;

            // 연구 우선순위: 경제/기술 중심
            genome.ResearchEconomyPriority = 1.3f;
            genome.ResearchCombatPriority = 1.0f;
            genome.ResearchMedicalPriority = 1.2f;

            // 플레이 스타일: 연구 편향
            genome.ResearchBias = 1.5f;
            genome.ExpansionBias = 1.0f;
            genome.DefensiveBias = 0.7f;
            genome.WelfareBias = 0.9f;
        }

        /// <summary>
        /// RimAIPlayStyle에서 Genome 프리셋으로 변환
        /// </summary>
        public static GenomePresetType FromPlayStyle(RimAIPlayStyle playStyle)
        {
            switch (playStyle)
            {
                case RimAIPlayStyle.Fortress:
                    return GenomePresetType.Defensive;
                case RimAIPlayStyle.Researcher:
                    return GenomePresetType.ResearchFocused;
                case RimAIPlayStyle.Agricultural:
                    return GenomePresetType.SafeEnding;
                case RimAIPlayStyle.Nomadic:
                    return GenomePresetType.FastEnding;
                case RimAIPlayStyle.Balanced:
                default:
                    return GenomePresetType.Default;
            }
        }

        /// <summary>
        /// 프리셋 설명
        /// </summary>
        public static string GetDescription(GenomePresetType presetType)
        {
            switch (presetType)
            {
                case GenomePresetType.Default:
                    return "균형 잡힌 기본 설정. 모든 분야를 조화롭게 발전시킵니다.";

                case GenomePresetType.FastEnding:
                    return "빠른 엔딩 지향. 연구와 생산을 우선시하여 효율적으로 엔딩을 달성합니다. 리스크가 있을 수 있습니다.";

                case GenomePresetType.SafeEnding:
                    return "안정적 엔딩 지향. 생존과 자원 비축을 우선시하여 안전하게 엔딩을 달성합니다.";

                case GenomePresetType.Entertaining:
                    return "관람용 스토리 중시. 무드 관리와 균형 잡힌 플레이로 보기 좋은 게임을 만듭니다.";

                case GenomePresetType.Defensive:
                    return "방어적 플레이. 요새를 건설하고 방어 시설에 집중합니다. 안전하지만 느릴 수 있습니다.";

                case GenomePresetType.ResearchFocused:
                    return "연구 집중. 빠른 기술 발전으로 첨단 장비를 확보합니다.";

                case GenomePresetType.GAOptimized:
                    return "GA 최적화 결과. 유전 알고리즘으로 찾아낸 최적의 파라미터입니다.";

                case GenomePresetType.Custom:
                    return "사용자 정의. 수동으로 파라미터를 조정합니다.";

                default:
                    return "알 수 없는 프리셋";
            }
        }

        /// <summary>
        /// 프리셋 한글 이름
        /// </summary>
        public static string GetKoreanName(GenomePresetType presetType)
        {
            switch (presetType)
            {
                case GenomePresetType.Default:
                    return "기본 (균형)";
                case GenomePresetType.FastEnding:
                    return "빠른 엔딩";
                case GenomePresetType.SafeEnding:
                    return "안정적 엔딩";
                case GenomePresetType.Entertaining:
                    return "관람용 스토리";
                case GenomePresetType.Defensive:
                    return "방어적 (요새)";
                case GenomePresetType.ResearchFocused:
                    return "연구 집중";
                case GenomePresetType.GAOptimized:
                    return "GA 최적화";
                case GenomePresetType.Custom:
                    return "사용자 정의";
                default:
                    return "알 수 없음";
            }
        }
    }
}
