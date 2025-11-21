using System;
using System.IO;
using System.Text.Json;

namespace GATrainer.Models
{
    /// <summary>
    /// RimAI 게임 한 판의 결과 metrics
    /// GA 최적화를 위해 게임 종료 시 JSON으로 저장됨
    /// </summary>
    public class RimAIRunMetrics
    {
        // === 게임 세션 정보 ===
        public string SessionId { get; set; } = "";
        public string Seed { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string GenomeId { get; set; } = "";

        // === 엔딩 정보 ===
        public bool Ended { get; set; } = false;
        public string EndReason { get; set; } = "Unknown";
        public int EndDay { get; set; } = 0;
        public int TotalDaysSurvived { get; set; } = 0;

        // === 콜로니 통계 ===
        public int FinalColonistCount { get; set; } = 0;
        public int MaxColonistCount { get; set; } = 0;
        public int ColonistDeaths { get; set; } = 0;
        public int AnimalDeaths { get; set; } = 0;

        // === 무드 통계 ===
        public float AverageMood { get; set; } = 50f;
        public float LowestMood { get; set; } = 100f;
        public int MentalBreakCount { get; set; } = 0;

        // === 위기 통계 ===
        public int FoodCrisesCount { get; set; } = 0;
        public int SevereIncidentsCount { get; set; } = 0;
        public int CombatCount { get; set; } = 0;
        public int CombatVictories { get; set; } = 0;

        // === 경제/발전 통계 ===
        public float FinalWealth { get; set; } = 0f;
        public float FinalBuildingsWealth { get; set; } = 0f;
        public float FinalItemsWealth { get; set; } = 0f;
        public int ResearchScore { get; set; } = 0;
        public float TechLevel { get; set; } = 0f;

        // === 생산/건설 통계 ===
        public int BuildingsConstructed { get; set; } = 0;
        public int ItemsProduced { get; set; } = 0;

        /// <summary>
        /// GA fitness 점수 계산 (높을수록 좋은 결과)
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

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }

        public static RimAIRunMetrics FromJson(string json)
        {
            try
            {
                var metrics = JsonSerializer.Deserialize<RimAIRunMetrics>(json);
                return metrics ?? new RimAIRunMetrics();
            }
            catch
            {
                return new RimAIRunMetrics();
            }
        }

        public void SaveToFile(string filePath)
        {
            File.WriteAllText(filePath, ToJson());
        }

        public static RimAIRunMetrics LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return new RimAIRunMetrics();

            string json = File.ReadAllText(filePath);
            return FromJson(json);
        }

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
