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
        /// <summary>엔딩 도달 여부 (true = 성공, false = 전멸/실패)</summary>
        public bool Ended { get; set; } = false;

        /// <summary>엔딩 이유 (ShipLaunch, ColonyDestroyed, AllColonistsDead, etc.)</summary>
        public string EndReason { get; set; } = "Unknown";

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
        /// GA fitness 점수 계산
        /// 높을수록 좋은 결과
        /// </summary>
        public float CalculateFitness()
        {
            float fitness = 0f;

            // 1. 생존 일수 (기본 점수)
            fitness += TotalDaysSurvived * 1f;

            // 2. 엔딩 도달 시 큰 보너스
            if (Ended)
            {
                fitness += 5000f;

                // 엔딩이 빠를수록 보너스 (효율성)
                if (EndDay > 0 && EndDay < 365 * 5) // 5년 이내
                {
                    fitness += (365 * 5 - EndDay) * 2f;
                }
            }

            // 3. 콜로니스트 생존 (사망 페널티)
            fitness -= ColonistDeaths * 200f;

            // 4. 콜로니 성장 보너스
            fitness += MaxColonistCount * 50f;

            // 5. 무드 관리 보너스
            fitness += AverageMood * 10f;

            // 6. 정신 붕괴 페널티
            fitness -= MentalBreakCount * 50f;

            // 7. 위기 대응 능력
            if (CombatCount > 0)
            {
                float winRate = (float)CombatVictories / CombatCount;
                fitness += winRate * 500f;
            }
            fitness -= FoodCrisesCount * 100f;

            // 8. 경제/발전 보너스
            fitness += FinalWealth * 0.1f;
            fitness += ResearchScore * 100f;

            // 9. 전멸 페널티
            if (!Ended && FinalColonistCount == 0)
            {
                fitness -= 3000f;
            }

            return fitness;
        }

        /// <summary>
        /// JSON으로 직렬화
        /// </summary>
        public string ToJson()
        {
            try
            {
                // Verse의 SavedGameLoaderNow를 사용하지 않고 수동 JSON 생성
                return System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI-GA] Metrics JSON 직렬화 실패: {ex.Message}");
                return "{}";
            }
        }

        /// <summary>
        /// JSON에서 역직렬화
        /// </summary>
        public static RimAIRunMetrics FromJson(string json)
        {
            try
            {
                var metrics = System.Text.Json.JsonSerializer.Deserialize<RimAIRunMetrics>(json);
                return metrics ?? new RimAIRunMetrics();
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI-GA] Metrics JSON 역직렬화 실패: {ex.Message}");
                return new RimAIRunMetrics();
            }
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
