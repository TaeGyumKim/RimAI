using System;
using System.Collections.Generic;
using System.Linq;

namespace GATrainer.Models
{
    /// <summary>
    /// GA Population (여러 Individual의 집합)
    /// </summary>
    public class Population
    {
        public List<Individual> Individuals { get; private set; }

        /// <summary>
        /// 현재 세대 번호
        /// </summary>
        public int Generation { get; private set; }

        /// <summary>
        /// Population 크기
        /// </summary>
        public int Size => Individuals.Count;

        /// <summary>
        /// Metrics가 있는 개체 수
        /// </summary>
        public int EvaluatedCount => Individuals.Count(i => i.HasMetrics);

        /// <summary>
        /// 평균 Fitness
        /// </summary>
        public float AverageFitness => Individuals.Where(i => i.HasMetrics).Average(i => i.Fitness);

        /// <summary>
        /// 최고 Fitness
        /// </summary>
        public float BestFitness => Individuals.Where(i => i.HasMetrics).Max(i => i.Fitness);

        /// <summary>
        /// 최저 Fitness
        /// </summary>
        public float WorstFitness => Individuals.Where(i => i.HasMetrics).Min(i => i.Fitness);

        /// <summary>
        /// 최고 개체
        /// </summary>
        public Individual? BestIndividual => Individuals
            .Where(i => i.HasMetrics)
            .OrderByDescending(i => i.Fitness)
            .FirstOrDefault();

        public Population(int generation = 0)
        {
            Generation = generation;
            Individuals = new List<Individual>();
        }

        /// <summary>
        /// 개체 추가
        /// </summary>
        public void AddIndividual(Individual individual)
        {
            Individuals.Add(individual);
        }

        /// <summary>
        /// 여러 개체 추가
        /// </summary>
        public void AddIndividuals(IEnumerable<Individual> individuals)
        {
            Individuals.AddRange(individuals);
        }

        /// <summary>
        /// Fitness 기준 정렬 (내림차순)
        /// </summary>
        public void SortByFitness()
        {
            Individuals = Individuals
                .Where(i => i.HasMetrics)
                .OrderByDescending(i => i.Fitness)
                .ToList();
        }

        /// <summary>
        /// 상위 N개 개체 가져오기
        /// </summary>
        public List<Individual> GetTopN(int n)
        {
            return Individuals
                .Where(i => i.HasMetrics)
                .OrderByDescending(i => i.Fitness)
                .Take(n)
                .ToList();
        }

        /// <summary>
        /// 요약 정보 출력
        /// </summary>
        public override string ToString()
        {
            if (EvaluatedCount == 0)
            {
                return $"Population [Gen {Generation}] Size: {Size}, No evaluations yet";
            }

            return $"Population [Gen {Generation}] Size: {Size}, Evaluated: {EvaluatedCount}\n" +
                   $"  Best: {BestFitness:F0}, Avg: {AverageFitness:F0}, Worst: {WorstFitness:F0}";
        }

        /// <summary>
        /// 상세 통계 출력
        /// </summary>
        public string GetDetailedStats()
        {
            if (EvaluatedCount == 0)
                return "No evaluations yet";

            var evaluated = Individuals.Where(i => i.HasMetrics).ToList();
            var avgDays = evaluated.Average(i => i.Metrics!.TotalDaysSurvived);
            var endedCount = evaluated.Count(i => i.Metrics!.Ended);
            var endedRate = (float)endedCount / evaluated.Count * 100f;

            return $"=== Population [Gen {Generation}] 통계 ===\n" +
                   $"총 개체 수: {Size}\n" +
                   $"평가 완료: {EvaluatedCount}\n" +
                   $"평균 생존 일수: {avgDays:F1}일\n" +
                   $"엔딩 도달: {endedCount}개 ({endedRate:F1}%)\n" +
                   $"Fitness - 최고: {BestFitness:F0}, 평균: {AverageFitness:F0}, 최저: {WorstFitness:F0}";
        }
    }
}
