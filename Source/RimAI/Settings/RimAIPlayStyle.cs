using System;

namespace RimAI.Settings
{
    /// <summary>
    /// RimAI 플레이 스타일 (스토리 패턴)
    /// 각 스타일은 서브시스템 가중치와 의사결정 편향을 조정하여
    /// 관찰 가능한 스토리를 만들어냅니다.
    /// </summary>
    public enum RimAIPlayStyle
    {
        /// <summary>
        /// 균형 발전 - 모든 분야 조화롭게 발전
        /// </summary>
        Balanced,

        /// <summary>
        /// 요새 콜로니 - 방어와 안전이 최우선
        /// </summary>
        Fortress,

        /// <summary>
        /// 농업 제국 - 광활한 농장과 식량 생산 중심
        /// </summary>
        Agricultural,

        /// <summary>
        /// 유목 생존자 - 빠른 확장, 최소주의, 효율성
        /// </summary>
        Nomadic,

        /// <summary>
        /// 기술 연구소 - 빠른 기술 발전과 첨단 장비
        /// </summary>
        Researcher
    }

    /// <summary>
    /// 플레이 스타일별 설정 프로필
    /// </summary>
    public static class RimAIPlayStyleProfile
    {
        /// <summary>
        /// 플레이 스타일 설명 가져오기
        /// </summary>
        public static string GetDescription(RimAIPlayStyle style)
        {
            switch (style)
            {
                case RimAIPlayStyle.Balanced:
                    return "모든 분야를 균형있게 발전시킵니다. 생존과 성장을 조화롭게 추구합니다.";

                case RimAIPlayStyle.Fortress:
                    return "방어가 최우선입니다. 두꺼운 벽, 포탑, 킬박스로 요새를 건설합니다. 외출을 최소화하고 안전을 추구합니다.";

                case RimAIPlayStyle.Agricultural:
                    return "광활한 농장을 건설합니다. 식량을 과잉 생산하고 대규모 창고를 관리합니다. 자급자족을 넘어 무역까지 목표합니다.";

                case RimAIPlayStyle.Nomadic:
                    return "최소주의 생존 방식입니다. 필수 인프라만 빠르게 건설하고, 효율적으로 자원을 활용합니다. 빠른 기술 발전을 추구합니다.";

                case RimAIPlayStyle.Researcher:
                    return "기술 발전이 최우선입니다. 연구 시설을 확충하고 첨단 장비를 빠르게 확보합니다. 지식이 곧 생존입니다.";

                default:
                    return "알 수 없는 플레이 스타일";
            }
        }

        /// <summary>
        /// 플레이 스타일별 서브시스템 우선순위 가중치
        /// </summary>
        public static class SubsystemWeights
        {
            // 기본 우선순위: Combat(150), Food(100), Construction(80), Research(60)

            public static (int combat, int food, int construction, int research) GetWeights(RimAIPlayStyle style)
            {
                switch (style)
                {
                    case RimAIPlayStyle.Balanced:
                        return (150, 100, 80, 60); // 기본값

                    case RimAIPlayStyle.Fortress:
                        return (200, 90, 120, 70); // 전투와 건설 높음

                    case RimAIPlayStyle.Agricultural:
                        return (120, 150, 90, 50); // 식량 최우선, 연구 낮음

                    case RimAIPlayStyle.Nomadic:
                        return (130, 110, 60, 100); // 건설 최소, 연구 높음

                    case RimAIPlayStyle.Researcher:
                        return (100, 80, 70, 180); // 연구 압도적 우선

                    default:
                        return (150, 100, 80, 60);
                }
            }
        }

        /// <summary>
        /// 플레이 스타일별 행동 편향
        /// </summary>
        public static class ActionBias
        {
            /// <summary>
            /// 방어 시설 건설 편향 (1.0 = 기본, 2.0 = 2배 선호)
            /// </summary>
            public static float GetDefenseBuildingBias(RimAIPlayStyle style)
            {
                switch (style)
                {
                    case RimAIPlayStyle.Fortress: return 3.0f;
                    case RimAIPlayStyle.Agricultural: return 0.7f;
                    case RimAIPlayStyle.Nomadic: return 0.5f;
                    case RimAIPlayStyle.Researcher: return 0.8f;
                    default: return 1.0f;
                }
            }

            /// <summary>
            /// 농장 크기 선호도 (1.0 = 기본, 2.0 = 2배 큰 농장)
            /// </summary>
            public static float GetFarmSizeBias(RimAIPlayStyle style)
            {
                switch (style)
                {
                    case RimAIPlayStyle.Agricultural: return 2.5f;
                    case RimAIPlayStyle.Fortress: return 1.2f; // 자급자족 중시
                    case RimAIPlayStyle.Nomadic: return 0.6f; // 최소한
                    case RimAIPlayStyle.Researcher: return 0.8f;
                    default: return 1.0f;
                }
            }

            /// <summary>
            /// 연구 속도 편향 (1.0 = 기본, 2.0 = 2배 빠른 연구)
            /// </summary>
            public static float GetResearchSpeedBias(RimAIPlayStyle style)
            {
                switch (style)
                {
                    case RimAIPlayStyle.Researcher: return 3.0f;
                    case RimAIPlayStyle.Nomadic: return 1.5f;
                    case RimAIPlayStyle.Fortress: return 1.0f;
                    case RimAIPlayStyle.Agricultural: return 0.7f;
                    default: return 1.0f;
                }
            }

            /// <summary>
            /// 건설 최소주의 (true = 필수만 건설)
            /// </summary>
            public static bool GetMinimalistBuilding(RimAIPlayStyle style)
            {
                return style == RimAIPlayStyle.Nomadic;
            }

            /// <summary>
            /// 식량 비축 목표 배율 (1.0 = 기본 7일, 2.0 = 14일)
            /// </summary>
            public static float GetFoodStorageGoalMultiplier(RimAIPlayStyle style)
            {
                switch (style)
                {
                    case RimAIPlayStyle.Agricultural: return 3.0f; // 21일치
                    case RimAIPlayStyle.Fortress: return 2.0f; // 14일치
                    case RimAIPlayStyle.Nomadic: return 0.7f; // 5일치
                    case RimAIPlayStyle.Researcher: return 0.9f;
                    default: return 1.0f; // 7일치
                }
            }

            /// <summary>
            /// 방어 태세 민감도 (1.0 = 기본, 2.0 = 2배 민감)
            /// </summary>
            public static float GetDefenseSensitivity(RimAIPlayStyle style)
            {
                switch (style)
                {
                    case RimAIPlayStyle.Fortress: return 1.5f; // 조기 경계
                    case RimAIPlayStyle.Nomadic: return 1.2f; // 빠른 대응
                    case RimAIPlayStyle.Agricultural: return 0.8f; // 여유로움
                    case RimAIPlayStyle.Researcher: return 0.9f;
                    default: return 1.0f;
                }
            }
        }

        /// <summary>
        /// 플레이 스타일에 맞는 스토리 키워드
        /// </summary>
        public static class StoryKeywords
        {
            public static string GetStoryPrefix(RimAIPlayStyle style)
            {
                switch (style)
                {
                    case RimAIPlayStyle.Fortress:
                        return "방어 중심";
                    case RimAIPlayStyle.Agricultural:
                        return "농업 제국";
                    case RimAIPlayStyle.Nomadic:
                        return "유목 생존";
                    case RimAIPlayStyle.Researcher:
                        return "기술 연구";
                    default:
                        return "균형 발전";
                }
            }

            public static string[] GetFlavorTexts(RimAIPlayStyle style)
            {
                switch (style)
                {
                    case RimAIPlayStyle.Fortress:
                        return new[]
                        {
                            "벽을 높이고 방어선을 강화합니다.",
                            "적의 침입에 대비한 킬박스를 준비합니다.",
                            "포탑 배치를 최적화합니다.",
                            "외출을 자제하고 안전을 확보합니다."
                        };

                    case RimAIPlayStyle.Agricultural:
                        return new[]
                        {
                            "농장을 확장하여 식량 생산을 늘립니다.",
                            "풍요로운 수확을 위해 파종을 시작합니다.",
                            "대규모 창고를 건설합니다.",
                            "식량 과잉 생산으로 안정을 추구합니다."
                        };

                    case RimAIPlayStyle.Nomadic:
                        return new[]
                        {
                            "필수 시설만 빠르게 건설합니다.",
                            "효율적인 자원 활용을 계획합니다.",
                            "최소한의 인프라로 생존합니다.",
                            "빠른 기술 발전을 추구합니다."
                        };

                    case RimAIPlayStyle.Researcher:
                        return new[]
                        {
                            "연구 시설을 확충합니다.",
                            "첨단 기술 개발에 집중합니다.",
                            "지식이 곧 생존입니다.",
                            "과학의 힘으로 난관을 극복합니다."
                        };

                    default:
                        return new[]
                        {
                            "균형있게 발전합니다.",
                            "모든 분야를 조화롭게 관리합니다.",
                            "안정적인 성장을 추구합니다."
                        };
                }
            }
        }
    }
}
