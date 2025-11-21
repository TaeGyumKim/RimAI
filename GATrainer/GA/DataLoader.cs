using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GATrainer.Models;

namespace GATrainer.GA
{
    /// <summary>
    /// Genome과 Metrics JSON 파일을 로드하는 유틸리티
    /// </summary>
    public static class DataLoader
    {
        /// <summary>
        /// 디렉토리에서 모든 Genome JSON 파일 로드
        /// </summary>
        public static List<RimAIGenome> LoadGenomes(string genomesDir)
        {
            var genomes = new List<RimAIGenome>();

            if (!Directory.Exists(genomesDir))
            {
                Console.WriteLine($"[DataLoader] Genome 디렉토리 없음: {genomesDir}");
                return genomes;
            }

            var jsonFiles = Directory.GetFiles(genomesDir, "*.json");

            foreach (var filePath in jsonFiles)
            {
                try
                {
                    var genome = RimAIGenome.LoadFromFile(filePath);
                    genomes.Add(genome);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DataLoader] Genome 로드 실패: {filePath} - {ex.Message}");
                }
            }

            Console.WriteLine($"[DataLoader] {genomes.Count}개 Genome 로드 완료");
            return genomes;
        }

        /// <summary>
        /// 디렉토리에서 모든 Metrics JSON 파일 로드
        /// </summary>
        public static List<RimAIRunMetrics> LoadMetrics(string runsDir)
        {
            var metricsList = new List<RimAIRunMetrics>();

            if (!Directory.Exists(runsDir))
            {
                Console.WriteLine($"[DataLoader] Runs 디렉토리 없음: {runsDir}");
                return metricsList;
            }

            var jsonFiles = Directory.GetFiles(runsDir, "*.json");

            foreach (var filePath in jsonFiles)
            {
                try
                {
                    var metrics = RimAIRunMetrics.LoadFromFile(filePath);
                    metricsList.Add(metrics);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DataLoader] Metrics 로드 실패: {filePath} - {ex.Message}");
                }
            }

            Console.WriteLine($"[DataLoader] {metricsList.Count}개 Metrics 로드 완료");
            return metricsList;
        }

        /// <summary>
        /// Genome과 Metrics를 매칭하여 Individual 리스트 생성
        /// </summary>
        public static List<Individual> LoadPopulationFromFiles(string genomesDir, string runsDir)
        {
            var genomes = LoadGenomes(genomesDir);
            var metricsList = LoadMetrics(runsDir);

            // GenomeId를 키로 하는 Metrics 딕셔너리 생성
            var metricsDict = metricsList
                .GroupBy(m => m.GenomeId)
                .ToDictionary(g => g.Key, g => g.First()); // 중복 시 첫 번째만 사용

            var individuals = new List<Individual>();

            foreach (var genome in genomes)
            {
                // 해당 Genome의 Metrics가 있으면 연결, 없으면 null
                RimAIRunMetrics? metrics = null;
                if (metricsDict.ContainsKey(genome.GenomeId))
                {
                    metrics = metricsDict[genome.GenomeId];
                }

                individuals.Add(new Individual(genome, metrics));
            }

            Console.WriteLine($"[DataLoader] {individuals.Count}개 Individual 생성 (평가 완료: {individuals.Count(i => i.HasMetrics)}개)");
            return individuals;
        }

        /// <summary>
        /// Population에서 Genome들을 지정된 디렉토리에 저장
        /// </summary>
        public static void SaveGenomes(Population population, string outputDir)
        {
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            int savedCount = 0;
            foreach (var individual in population.Individuals)
            {
                string fileName = $"{individual.Genome.GenomeId}.json";
                string filePath = Path.Combine(outputDir, fileName);

                try
                {
                    individual.Genome.SaveToFile(filePath);
                    savedCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DataLoader] Genome 저장 실패: {fileName} - {ex.Message}");
                }
            }

            Console.WriteLine($"[DataLoader] {savedCount}개 Genome 저장 완료: {outputDir}");
        }

        /// <summary>
        /// 최고 개체의 Genome을 current.json으로 저장
        /// </summary>
        public static void SaveBestGenomeAsCurrent(Population population, string outputDir)
        {
            var best = population.BestIndividual;
            if (best == null)
            {
                Console.WriteLine("[DataLoader] 최고 개체 없음 (평가된 개체가 없음)");
                return;
            }

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string currentPath = Path.Combine(outputDir, "current.json");
            best.Genome.SaveToFile(currentPath);

            Console.WriteLine($"[DataLoader] 최고 개체를 current.json으로 저장 완료 (Fitness: {best.Fitness:F0})");
        }
    }
}
