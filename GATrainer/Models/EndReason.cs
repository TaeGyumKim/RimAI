namespace GATrainer.Models
{
    /// <summary>
    /// 게임 종료 이유
    /// GA fitness 계산 시 사용됨
    /// </summary>
    public enum EndReason
    {
        /// <summary>알 수 없음 (오류 또는 미분류)</summary>
        Unknown = 0,

        // === 성공 엔딩 ===

        /// <summary>우주선 발사 (가장 일반적인 승리)</summary>
        ShipLaunched = 100,

        /// <summary>로열티 퀘스트 완료 (제국 귀족 엔딩)</summary>
        RoyalAscent = 101,

        /// <summary>아코넥서스 퀘스트 완료 (Ideology DLC)</summary>
        Archonexus = 102,

        /// <summary>기타 mod 엔딩</summary>
        OtherVictory = 199,

        // === 실패 엔딩 ===

        /// <summary>모든 콜로니스트 사망 (전멸)</summary>
        AllColonistsDead = 200,

        /// <summary>콜로니 포기 (플레이어가 메뉴에서 포기)</summary>
        ColonyAbandoned = 201,

        /// <summary>타임아웃 (설정된 일수 초과, 엔딩 미도달)</summary>
        Timeout = 202,

        /// <summary>플레이어가 게임 종료 (수동 종료)</summary>
        ManualExit = 203,

        /// <summary>기타 실패 원인</summary>
        OtherFailure = 299,
    }

    /// <summary>
    /// EndReason 확장 메서드
    /// </summary>
    public static class EndReasonExtensions
    {
        /// <summary>성공 엔딩인지 확인</summary>
        public static bool IsSuccess(this EndReason reason)
        {
            int value = (int)reason;
            return value >= 100 && value < 200;
        }

        /// <summary>실패 엔딩인지 확인</summary>
        public static bool IsFailure(this EndReason reason)
        {
            int value = (int)reason;
            return value >= 200 && value < 300;
        }

        /// <summary>타임아웃인지 확인</summary>
        public static bool IsTimeout(this EndReason reason)
        {
            return reason == EndReason.Timeout;
        }

        /// <summary>전멸인지 확인</summary>
        public static bool IsWipeout(this EndReason reason)
        {
            return reason == EndReason.AllColonistsDead;
        }

        /// <summary>한글 설명</summary>
        public static string ToKoreanString(this EndReason reason)
        {
            switch (reason)
            {
                case EndReason.ShipLaunched:
                    return "우주선 발사";
                case EndReason.RoyalAscent:
                    return "로열티 엔딩";
                case EndReason.Archonexus:
                    return "아코넥서스 엔딩";
                case EndReason.OtherVictory:
                    return "기타 승리";
                case EndReason.AllColonistsDead:
                    return "전멸";
                case EndReason.ColonyAbandoned:
                    return "콜로니 포기";
                case EndReason.Timeout:
                    return "타임아웃";
                case EndReason.ManualExit:
                    return "수동 종료";
                case EndReason.OtherFailure:
                    return "기타 실패";
                default:
                    return "알 수 없음";
            }
        }

        /// <summary>영문 설명</summary>
        public static string ToEnglishString(this EndReason reason)
        {
            switch (reason)
            {
                case EndReason.ShipLaunched:
                    return "Ship Launched";
                case EndReason.RoyalAscent:
                    return "Royal Ascent";
                case EndReason.Archonexus:
                    return "Archonexus";
                case EndReason.OtherVictory:
                    return "Other Victory";
                case EndReason.AllColonistsDead:
                    return "All Colonists Dead";
                case EndReason.ColonyAbandoned:
                    return "Colony Abandoned";
                case EndReason.Timeout:
                    return "Timeout";
                case EndReason.ManualExit:
                    return "Manual Exit";
                case EndReason.OtherFailure:
                    return "Other Failure";
                default:
                    return "Unknown";
            }
        }

        /// <summary>문자열을 EndReason으로 변환</summary>
        public static EndReason FromString(string reasonString)
        {
            if (string.IsNullOrEmpty(reasonString))
                return EndReason.Unknown;

            // 기존 문자열 호환성 유지
            switch (reasonString)
            {
                case "ShipLaunch":
                case "ShipLaunched":
                    return EndReason.ShipLaunched;
                case "RoyalAscent":
                    return EndReason.RoyalAscent;
                case "Archonexus":
                    return EndReason.Archonexus;
                case "AllColonistsDead":
                    return EndReason.AllColonistsDead;
                case "ColonyAbandoned":
                    return EndReason.ColonyAbandoned;
                case "Timeout":
                    return EndReason.Timeout;
                case "ManualExit":
                    return EndReason.ManualExit;
                default:
                    // Enum.TryParse 시도
                    if (System.Enum.TryParse(reasonString, out EndReason result))
                        return result;
                    return EndReason.Unknown;
            }
        }
    }
}
