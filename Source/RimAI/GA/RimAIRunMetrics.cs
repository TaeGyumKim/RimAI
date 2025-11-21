using System;
using System.IO;
using Verse;

namespace RimAI.GA
{
    /// <summary>
    /// RimAI 게임 한 판의 결과 metrics
    /// GA 최적화를 위해 게임 종료 시 JSON으로 저장됨
    /// </summary>
    public class RimAIRunMetrics
    {
        // === 게임 세션 정보 ===
        /// <summary>게임 세션 ID (고유 식별자)</summary>
        public string SessionId { get; set; } = "";

        /// <summary>맵 시드</summary>
        public string Seed { get; set; } = "";

        /// <summary>시작 시각 (UTC)</summary>
        public DateTime StartTime { get; set; }

        /// <summary>종료 시각 (UTC)</summary>
        public DateTime EndTime { get; set; }

        /// <summary>Genome ID (사용된 파라미터 세트)</summary>
        public string GenomeId { get; set; } = "";

        // === 엔딩 정보 ===
        /// <summary>엔딩 이유 (enum으로 관리)</summary>
        public EndReason EndReasonEnum { get; set; } = GA.EndReason.Unknown;

        /// <summary>엔딩 도달 여부 (true = 성공, false = 전멸/실패)</summary>
        public bool Ended
        {
            get => EndReasonEnum.IsSuccess();
            set { /* JSON 역직렬화 호환성 위해 유지, 실제로는 EndReasonEnum 사용 */ }
        }

        /// <summary>엔딩 이유 (문자열, JSON 호환성 위해 유지)</summary>
        public string EndReason
        {
            get => EndReasonEnum.ToEnglishString();
            set => EndReasonEnum = EndReasonExtensions.FromString(value);
        }

        /// <summary>엔딩 도달 시 in-game 일수 (없으면 마지막 일수)</summary>
        public int EndDay { get; set; } = 0;

        /// <summary>총 생존 일수</summary>
        public int TotalDaysSurvived { get; set; } = 0;

        // === 콜로니 통계 ===
        /// <summary>최종 콜로니스트 수</summary>
        public int FinalColonistCount { get; set; } = 0;

        /// <summary>최대 콜로니스트 수 (게임 중 최고치)</summary>
        public int MaxColonistCount { get; set; } = 0;

        /// <summary>콜로니스트 사망 수</summary>
        public int ColonistDeaths { get; set; } = 0;

        /// <summary>동물 사망 수</summary>
        public int AnimalDeaths { get; set; } = 0;

        // === 무드 통계 ===
        /// <summary>평균 무드 (0-100)</summary>
        public float AverageMood { get; set; } = 50f;

        /// <summary>최저 무드 (0-100)</summary>
        public float LowestMood { get; set; } = 100f;

        /// <summary>정신 붕괴 횟수</summary>
        public int MentalBreakCount { get; set; } = 0;

        // === 위기 통계 ===
        /// <summary>식량 위기 횟수 (심각한 부족 구간)</summary>
        public int FoodCrisesCount { get; set; } = 0;

        /// <summary>심각한 사건 횟수 (대규모 레이드, 기계 클러스터 등)</summary>
        public int SevereIncidentsCount { get; set; } = 0;

        /// <summary>전투 횟수</summary>
        public int CombatCount { get; set; } = 0;

        /// <summary>전투 승리 횟수</summary>
        public int CombatVictories { get; set; } = 0;

        // === 경제/발전 통계 ===
        /// <summary>최종 부(wealth)</summary>
        public float FinalWealth { get; set; } = 0f;

        /// <summary>최종 건설 부</summary>
        public float FinalBuildingsWealth { get; set; } = 0f;

        /// <summary>최종 아이템 부</summary>
        public float FinalItemsWealth { get; set; } = 0f;

        /// <summary>연구 점수 (완료된 연구 프로젝트 수)</summary>
        public int ResearchScore { get; set; } = 0;

        /// <summary>최종 기술 레벨 (평균 연구 진행도 0-1)</summary>
        public float TechLevel { get; set; } = 0f;

        // === 생산/건설 통계 ===
        /// <summary>건설된 건물 수</summary>
        public int BuildingsConstructed { get; set; } = 0;

        /// <summary>생산된 아이템 수</summary>
        public int ItemsProduced { get; set; } = 0;

        // === 점수 계산 (GA fitness) ===
        /// <summary>
        /// GA fitness 점수 계산 v2.0
        /// 높을수록 좋은 결과
        ///
        /// 설계 원칙:
        /// - 엔딩 도달이 최우선 목표
        /// - 생존, 무드, 경제를 균형있게 고려
        /// - "엔딩만 빨리 찍는 쓰레기 전략" 방지 (사망/무드 페널티)
        /// - 관람용 퀄리티 (무드, 정신 붕괴) 중시
        /// </summary>
        public float CalculateFitness()
        {
            float fitness = 0f;

            // 1. 생존 일수 (기본 점수: 1점/일)
            fitness += TotalDaysSurvived * 1f;

            // 2. 엔딩 도달 시 대형 보너스
            if (EndReasonEnum.IsSuccess())
            {
                // 2-1. 엔딩 기본 보너스
                fitness += 5000f;

                // 2-2. 빠른 엔딩 보너스 (5년 이내)
                // 엔딩이 빠를수록 효율적이지만, 너무 빠르면 품질이 떨어짐
                if (EndDay > 0 && EndDay < 365 * 5)
                {
                    // 3년(1095일) 이후부터 보너스 시작 (너무 빠른 엔딩 방지)
                    if (EndDay >= 365 * 3)
                    {
                        fitness += (365 * 5 - EndDay) * 2f;
                    }
                }
            }
            else if (EndReasonEnum.IsTimeout())
            {
                // 타임아웃 시 약간의 페널티 (엔딩 도달하지 못함)
                fitness -= 1000f;
            }

            // 3. 콜로니스트 생존 (사망 페널티)
            // "엔딩만 빨리 찍는 쓰레기 전략" 방지 핵심
            fitness -= ColonistDeaths * 200f;

            // 3-1. 과도한 사망 추가 페널티 (5명 이상 사망)
            if (ColonistDeaths >= 5)
            {
                fitness -= (ColonistDeaths - 4) * 300f; // 5명부터 300점씩 추가 페널티
            }

            // 4. 콜로니 성장 보너스
            fitness += MaxColonistCount * 50f;

            // 4-1. 최종 생존자 보너스 (엔딩 시 살아있는 사람 수)
            if (EndReasonEnum.IsSuccess())
            {
                fitness += FinalColonistCount * 100f; // 엔딩 시 생존자가 많으면 큰 보너스
            }

            // 5. 무드 관리 (관람용 퀄리티 핵심)
            fitness += AverageMood * 10f;

            // 5-1. 최저 무드 페널티 (너무 낮은 무드는 관람 퀄리티 저하)
            if (LowestMood < 30f)
            {
                fitness -= (30f - LowestMood) * 20f; // 30 미만일수록 페널티
            }

            // 6. 정신 붕괴 페널티 (관람용 퀄리티)
            fitness -= MentalBreakCount * 50f;

            // 6-1. 과도한 정신 붕괴 추가 페널티 (10회 이상)
            if (MentalBreakCount >= 10)
            {
                fitness -= (MentalBreakCount - 9) * 100f; // 10회부터 100점씩 추가 페널티
            }

            // 7. 위기 대응 능력
            // 7-1. 전투 승률
            if (CombatCount > 0)
            {
                float winRate = (float)CombatVictories / CombatCount;
                fitness += winRate * 500f;

                // 전투 경험 보너스 (전투를 피하지 않고 이기는 것이 중요)
                if (winRate >= 0.8f && CombatCount >= 5)
                {
                    fitness += 200f; // 80% 이상 승률 + 5회 이상 전투
                }
            }

            // 7-2. 식량 위기 페널티
            fitness -= FoodCrisesCount * 100f;

            // 7-3. 과도한 식량 위기 추가 페널티 (5회 이상)
            if (FoodCrisesCount >= 5)
            {
                fitness -= (FoodCrisesCount - 4) * 200f; // 5회부터 200점씩 추가 페널티
            }

            // 8. 경제/발전 보너스
            fitness += FinalWealth * 0.1f;
            fitness += ResearchScore * 100f;

            // 8-1. 기술 발전 보너스
            if (TechLevel >= 0.7f)
            {
                fitness += 500f; // 높은 기술 레벨 보너스
            }

            // 9. 전멸 대형 페널티
            if (EndReasonEnum.IsWipeout())
            {
                fitness -= 3000f;

                // 빠른 전멸 추가 페널티 (1년 이내 전멸)
                if (TotalDaysSurvived < 365)
                {
                    fitness -= 2000f;
                }
            }

            // 10. 콜로니 포기 페널티
            if (EndReasonEnum == GA.EndReason.ColonyAbandoned)
            {
                fitness -= 2000f;
            }

            return fitness;
        }

        /// <summary>
        /// JSON으로 직렬화 (수동 구현 - .NET Framework 4.7.2 호환)
        /// </summary>
        public string ToJson()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine($"  \"SessionId\": \"{EscapeJson(SessionId)}\",");
                sb.AppendLine($"  \"Seed\": \"{EscapeJson(Seed)}\",");
                sb.AppendLine($"  \"StartTime\": \"{StartTime:O}\",");
                sb.AppendLine($"  \"EndTime\": \"{EndTime:O}\",");
                sb.AppendLine($"  \"GenomeId\": \"{EscapeJson(GenomeId)}\",");
                sb.AppendLine($"  \"EndReason\": \"{EndReason}\",");
                sb.AppendLine($"  \"EndDay\": {EndDay},");
                sb.AppendLine($"  \"TotalDaysSurvived\": {TotalDaysSurvived},");
                sb.AppendLine($"  \"FinalColonistCount\": {FinalColonistCount},");
                sb.AppendLine($"  \"MaxColonistCount\": {MaxColonistCount},");
                sb.AppendLine($"  \"ColonistDeaths\": {ColonistDeaths},");
                sb.AppendLine($"  \"AnimalDeaths\": {AnimalDeaths},");
                sb.AppendLine($"  \"AverageMood\": {AverageMood.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                sb.AppendLine($"  \"LowestMood\": {LowestMood.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                sb.AppendLine($"  \"MentalBreakCount\": {MentalBreakCount},");
                sb.AppendLine($"  \"FoodCrisesCount\": {FoodCrisesCount},");
                sb.AppendLine($"  \"SevereIncidentsCount\": {SevereIncidentsCount},");
                sb.AppendLine($"  \"CombatCount\": {CombatCount},");
                sb.AppendLine($"  \"CombatVictories\": {CombatVictories},");
                sb.AppendLine($"  \"FinalWealth\": {FinalWealth.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                sb.AppendLine($"  \"FinalBuildingsWealth\": {FinalBuildingsWealth.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                sb.AppendLine($"  \"FinalItemsWealth\": {FinalItemsWealth.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                sb.AppendLine($"  \"ResearchScore\": {ResearchScore},");
                sb.AppendLine($"  \"TechLevel\": {TechLevel.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                sb.AppendLine($"  \"BuildingsConstructed\": {BuildingsConstructed},");
                sb.AppendLine($"  \"ItemsProduced\": {ItemsProduced}");
                sb.AppendLine("}");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI-GA] Metrics JSON 직렬화 실패: {ex.Message}");
                return "{}";
            }
        }

        /// <summary>
        /// JSON에서 역직렬화 (수동 구현 - .NET Framework 4.7.2 호환)
        /// </summary>
        public static RimAIRunMetrics FromJson(string json)
        {
            try
            {
                var metrics = new RimAIRunMetrics();
                metrics.SessionId = ParseJsonString(json, "SessionId") ?? "";
                metrics.Seed = ParseJsonString(json, "Seed") ?? "";
                metrics.GenomeId = ParseJsonString(json, "GenomeId") ?? "";
                metrics.EndReason = ParseJsonString(json, "EndReason") ?? "";
                metrics.EndDay = ParseJsonInt(json, "EndDay");
                metrics.TotalDaysSurvived = ParseJsonInt(json, "TotalDaysSurvived");
                metrics.FinalColonistCount = ParseJsonInt(json, "FinalColonistCount");
                metrics.MaxColonistCount = ParseJsonInt(json, "MaxColonistCount");
                metrics.ColonistDeaths = ParseJsonInt(json, "ColonistDeaths");
                metrics.AnimalDeaths = ParseJsonInt(json, "AnimalDeaths");
                metrics.AverageMood = ParseJsonFloat(json, "AverageMood");
                metrics.LowestMood = ParseJsonFloat(json, "LowestMood");
                metrics.MentalBreakCount = ParseJsonInt(json, "MentalBreakCount");
                metrics.FoodCrisesCount = ParseJsonInt(json, "FoodCrisesCount");
                metrics.SevereIncidentsCount = ParseJsonInt(json, "SevereIncidentsCount");
                metrics.CombatCount = ParseJsonInt(json, "CombatCount");
                metrics.CombatVictories = ParseJsonInt(json, "CombatVictories");
                metrics.FinalWealth = ParseJsonFloat(json, "FinalWealth");
                metrics.FinalBuildingsWealth = ParseJsonFloat(json, "FinalBuildingsWealth");
                metrics.FinalItemsWealth = ParseJsonFloat(json, "FinalItemsWealth");
                metrics.ResearchScore = ParseJsonInt(json, "ResearchScore");
                metrics.TechLevel = ParseJsonFloat(json, "TechLevel");
                metrics.BuildingsConstructed = ParseJsonInt(json, "BuildingsConstructed");
                metrics.ItemsProduced = ParseJsonInt(json, "ItemsProduced");
                return metrics;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI-GA] Metrics JSON 역직렬화 실패: {ex.Message}");
                return new RimAIRunMetrics();
            }
        }

        private static string EscapeJson(string s) => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";

        private static string ParseJsonString(string json, string key)
        {
            var pattern = $"\"{key}\"\\s*:\\s*\"([^\"]*)\"";
            var match = System.Text.RegularExpressions.Regex.Match(json, pattern);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static int ParseJsonInt(string json, string key)
        {
            var pattern = $"\"{key}\"\\s*:\\s*(-?\\d+)";
            var match = System.Text.RegularExpressions.Regex.Match(json, pattern);
            return match.Success && int.TryParse(match.Groups[1].Value, out int val) ? val : 0;
        }

        private static float ParseJsonFloat(string json, string key)
        {
            var pattern = $"\"{key}\"\\s*:\\s*(-?[\\d.]+)";
            var match = System.Text.RegularExpressions.Regex.Match(json, pattern);
            return match.Success && float.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float val) ? val : 0f;
        }

        /// <summary>
        /// 파일로 저장
        /// </summary>
        public void SaveToFile(string filePath)
        {
            try
            {
                string json = ToJson();
                File.WriteAllText(filePath, json);
                Log.Message($"[RimAI-GA] Metrics 저장 완료: {filePath}");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI-GA] Metrics 파일 저장 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 파일에서 로드
        /// </summary>
        public static RimAIRunMetrics LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Log.Warning($"[RimAI-GA] Metrics 파일 없음: {filePath}");
                    return new RimAIRunMetrics();
                }

                string json = File.ReadAllText(filePath);
                return FromJson(json);
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI-GA] Metrics 파일 로드 실패: {ex.Message}");
                return new RimAIRunMetrics();
            }
        }

        /// <summary>
        /// 디버그 출력용 요약
        /// </summary>
        public override string ToString()
        {
            return $"[Metrics] Session:{SessionId} | Days:{TotalDaysSurvived} | " +
                   $"Ended:{Ended} ({EndReason}) | Colonists:{FinalColonistCount}/{MaxColonistCount} | " +
                   $"Deaths:{ColonistDeaths} | Mood:{AverageMood:F1} | " +
                   $"Wealth:{FinalWealth:F0} | Research:{ResearchScore} | " +
                   $"Fitness:{CalculateFitness():F0}";
        }
    }
}
