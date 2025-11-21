using System;

namespace GATrainer.Models
{
    /// <summary>
    /// GA에서 다루는 개체 (Genome + Metrics + Fitness)
    /// </summary>
    public class Individual
    {
        /// <summary>
        /// 유전자 (파라미터 집합)
        /// </summary>
        public RimAIGenome Genome { get; set; }

        /// <summary>
        /// 실제 플레이 결과 (null이면 아직 플레이 안 함)
        /// </summary>
        public RimAIRunMetrics? Metrics { get; set; }

        /// <summary>
        /// Fitness 점수 (Metrics가 있으면 계산됨)
        /// </summary>
        public float Fitness
        {
            get
            {
                if (Metrics == null)
                    return 0f;
                return Metrics.CalculateFitness();
            }
        }

        /// <summary>
        /// Metrics가 있는지 (플레이 완료 여부)
        /// </summary>
        public bool HasMetrics => Metrics != null;

        public Individual(RimAIGenome genome, RimAIRunMetrics? metrics = null)
        {
            Genome = genome ?? throw new ArgumentNullException(nameof(genome));
            Metrics = metrics;
        }

        public override string ToString()
        {
            string metricsInfo = HasMetrics
                ? $"Fitness: {Fitness:F0}, Days: {Metrics!.TotalDaysSurvived}, Ended: {Metrics.Ended}"
                : "No metrics yet";
            return $"Individual [Gen {Genome.Generation}] {Genome.GenomeId} - {metricsInfo}";
        }
    }
}
