using System;
using System.Collections.Generic;
using System.Linq;
using GATrainer.Models;

namespace GATrainer.GA
{
    /// <summary>
    /// GA 핵심 연산 (Selection, Crossover, Mutation)
    /// </summary>
    public static class GAOperations
    {
        private static Random random = new Random();

        /// <summary>
        /// Tournament Selection
        /// 랜덤하게 K개 개체를 선택하고 그 중 최고 fitness를 반환
        /// </summary>
        public static Individual TournamentSelection(Population population, int tournamentSize = 3)
        {
            if (population.EvaluatedCount < tournamentSize)
            {
                throw new InvalidOperationException($"Population에 평가된 개체가 {tournamentSize}개 미만입니다.");
            }

            var evaluatedIndividuals = population.Individuals
                .Where(i => i.HasMetrics)
                .ToList();

            var tournament = new List<Individual>();
            for (int i = 0; i < tournamentSize; i++)
            {
                var randomIndex = random.Next(evaluatedIndividuals.Count);
                tournament.Add(evaluatedIndividuals[randomIndex]);
            }

            return tournament.OrderByDescending(i => i.Fitness).First();
        }

        /// <summary>
        /// Roulette Wheel Selection (Fitness Proportionate Selection)
        /// Fitness에 비례하는 확률로 선택
        /// </summary>
        public static Individual RouletteWheelSelection(Population population)
        {
            var evaluatedIndividuals = population.Individuals
                .Where(i => i.HasMetrics)
                .ToList();

            if (!evaluatedIndividuals.Any())
            {
                throw new InvalidOperationException("평가된 개체가 없습니다.");
            }

            // 모든 fitness를 양수로 만들기 (최소값이 음수일 수 있음)
            float minFitness = evaluatedIndividuals.Min(i => i.Fitness);
            float offset = minFitness < 0 ? -minFitness + 1 : 0;

            float totalFitness = evaluatedIndividuals.Sum(i => i.Fitness + offset);
            float randomValue = (float)random.NextDouble() * totalFitness;

            float cumulativeFitness = 0f;
            foreach (var individual in evaluatedIndividuals)
            {
                cumulativeFitness += individual.Fitness + offset;
                if (cumulativeFitness >= randomValue)
                {
                    return individual;
                }
            }

            // fallback (거의 발생하지 않음)
            return evaluatedIndividuals.Last();
        }

        /// <summary>
        /// Top-N Selection
        /// 상위 N개 개체를 부모 풀로 사용
        /// </summary>
        public static List<Individual> TopNSelection(Population population, int n)
        {
            return population.GetTopN(n);
        }

        /// <summary>
        /// Elitism
        /// 상위 N개 개체를 다음 세대에 그대로 유지
        /// </summary>
        public static List<Individual> Elitism(Population population, int eliteCount)
        {
            var elites = population.GetTopN(eliteCount);

            // 세대 번호 증가 및 새 개체로 복사
            var nextGeneration = population.Generation + 1;
            return elites.Select(e => new Individual(
                CopyGenome(e.Genome, nextGeneration),
                null // Metrics는 복사하지 않음 (새로 평가 필요)
            )).ToList();
        }

        /// <summary>
        /// Crossover (교배)
        /// 두 부모 개체를 교배하여 자식 개체 생성
        /// </summary>
        public static Individual Crossover(Individual parent1, Individual parent2, int generation)
        {
            var childGenome = RimAIGenome.Crossover(parent1.Genome, parent2.Genome, generation);
            return new Individual(childGenome, null);
        }

        /// <summary>
        /// Mutation (돌연변이)
        /// 개체의 Genome에 돌연변이 적용
        /// </summary>
        public static void Mutate(Individual individual, float mutationRate = 0.1f)
        {
            individual.Genome.Mutate(mutationRate);
        }

        /// <summary>
        /// 다음 세대 생성
        /// </summary>
        /// <param name="currentPopulation">현재 세대</param>
        /// <param name="populationSize">다음 세대 크기</param>
        /// <param name="eliteCount">엘리트 개체 수 (상위 N개 유지)</param>
        /// <param name="mutationRate">돌연변이 확률 (0.0 - 1.0)</param>
        /// <param name="selectionMethod">선택 방법 (tournament, roulette, topN)</param>
        /// <returns>다음 세대 Population</returns>
        public static Population GenerateNextGeneration(
            Population currentPopulation,
            int populationSize,
            int eliteCount = 2,
            float mutationRate = 0.1f,
            string selectionMethod = "tournament")
        {
            if (currentPopulation.EvaluatedCount == 0)
            {
                throw new InvalidOperationException("현재 세대에 평가된 개체가 없습니다.");
            }

            var nextGeneration = currentPopulation.Generation + 1;
            var nextPopulation = new Population(nextGeneration);

            // 1. Elitism (상위 개체 유지)
            var elites = Elitism(currentPopulation, eliteCount);
            nextPopulation.AddIndividuals(elites);

            Console.WriteLine($"[GA] Elitism: 상위 {eliteCount}개 개체 유지");

            // 2. Selection + Crossover + Mutation으로 나머지 개체 생성
            int remainingCount = populationSize - eliteCount;

            for (int i = 0; i < remainingCount; i++)
            {
                // 부모 선택
                Individual parent1, parent2;

                switch (selectionMethod.ToLower())
                {
                    case "roulette":
                        parent1 = RouletteWheelSelection(currentPopulation);
                        parent2 = RouletteWheelSelection(currentPopulation);
                        break;

                    case "topn":
                        var topN = TopNSelection(currentPopulation, Math.Min(10, currentPopulation.EvaluatedCount));
                        parent1 = topN[random.Next(topN.Count)];
                        parent2 = topN[random.Next(topN.Count)];
                        break;

                    case "tournament":
                    default:
                        parent1 = TournamentSelection(currentPopulation, tournamentSize: 3);
                        parent2 = TournamentSelection(currentPopulation, tournamentSize: 3);
                        break;
                }

                // 교배
                var child = Crossover(parent1, parent2, nextGeneration);

                // 돌연변이
                Mutate(child, mutationRate);

                nextPopulation.AddIndividual(child);
            }

            Console.WriteLine($"[GA] Selection + Crossover + Mutation: {remainingCount}개 자식 생성");
            Console.WriteLine($"[GA] 다음 세대 생성 완료: Gen {nextGeneration}, Size: {nextPopulation.Size}");

            return nextPopulation;
        }

        /// <summary>
        /// Genome 복사 (세대 번호만 변경)
        /// </summary>
        private static RimAIGenome CopyGenome(RimAIGenome source, int newGeneration)
        {
            var json = source.ToJson();
            var copy = RimAIGenome.FromJson(json);
            copy.Generation = newGeneration;
            copy.GenomeId = $"gen{newGeneration}_elite_{Guid.NewGuid().ToString().Substring(0, 8)}";
            return copy;
        }
    }
}
