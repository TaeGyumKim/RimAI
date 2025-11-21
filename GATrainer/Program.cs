using System;
using System.IO;
using System.Linq;
using GATrainer.GA;
using GATrainer.Models;

namespace GATrainer
{
    /// <summary>
    /// RimAI GA Trainer - 유전 알고리즘 트레이너 콘솔 앱
    ///
    /// 사용법:
    /// 1. 초기 세대 생성: GATrainer.exe init <개체수> <출력디렉토리>
    /// 2. 다음 세대 생성: GATrainer.exe evolve <genomes디렉토리> <runs디렉토리> <출력디렉토리> [옵션]
    /// 3. 통계 보기: GATrainer.exe stats <genomes디렉토리> <runs디렉토리>
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== RimAI GA Trainer ===");
            Console.WriteLine();

            if (args.Length == 0)
            {
                ShowUsage();
                return;
            }

            string command = args[0].ToLower();

            try
            {
                switch (command)
                {
                    case "init":
                        CommandInit(args);
                        break;

                    case "evolve":
                        CommandEvolve(args);
                        break;

                    case "stats":
                        CommandStats(args);
                        break;

                    case "help":
                        ShowUsage();
                        break;

                    default:
                        Console.WriteLine($"알 수 없는 명령: {command}");
                        ShowUsage();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[오류] {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// 사용법 출력
        /// </summary>
        static void ShowUsage()
        {
            Console.WriteLine("사용법:");
            Console.WriteLine();
            Console.WriteLine("  1. 초기 세대 생성:");
            Console.WriteLine("     GATrainer init <개체수> <출력디렉토리>");
            Console.WriteLine("     예: GATrainer init 20 ./genomes");
            Console.WriteLine();
            Console.WriteLine("  2. 다음 세대 생성:");
            Console.WriteLine("     GATrainer evolve <genomes디렉토리> <runs디렉토리> <출력디렉토리> [옵션]");
            Console.WriteLine("     옵션:");
            Console.WriteLine("       --size=N          다음 세대 크기 (기본: 현재 세대와 동일)");
            Console.WriteLine("       --elite=N         엘리트 개체 수 (기본: 2)");
            Console.WriteLine("       --mutation=N      돌연변이 확률 0.0-1.0 (기본: 0.1)");
            Console.WriteLine("       --selection=TYPE  선택 방법 tournament|roulette|topN (기본: tournament)");
            Console.WriteLine("     예: GATrainer evolve ./genomes ./runs ./genomes_next --elite=3 --mutation=0.15");
            Console.WriteLine();
            Console.WriteLine("  3. 통계 보기:");
            Console.WriteLine("     GATrainer stats <genomes디렉토리> <runs디렉토리>");
            Console.WriteLine("     예: GATrainer stats ./genomes ./runs");
            Console.WriteLine();
        }

        /// <summary>
        /// init 명령: 초기 세대 생성
        /// </summary>
        static void CommandInit(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("[init] 인자 부족: GATrainer init <개체수> <출력디렉토리>");
                return;
            }

            int populationSize = int.Parse(args[1]);
            string outputDir = args[2];

            Console.WriteLine($"[init] 초기 세대 생성: {populationSize}개 개체");
            Console.WriteLine($"[init] 출력 디렉토리: {outputDir}");

            var population = new Population(generation: 0);

            for (int i = 0; i < populationSize; i++)
            {
                var genome = RimAIGenome.CreateRandom(generation: 0);
                population.AddIndividual(new Individual(genome));
            }

            DataLoader.SaveGenomes(population, outputDir);

            Console.WriteLine();
            Console.WriteLine($"[init] 완료: {outputDir} 에 {populationSize}개 Genome 저장");
        }

        /// <summary>
        /// evolve 명령: 다음 세대 생성
        /// </summary>
        static void CommandEvolve(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("[evolve] 인자 부족: GATrainer evolve <genomes디렉토리> <runs디렉토리> <출력디렉토리> [옵션]");
                return;
            }

            string genomesDir = args[1];
            string runsDir = args[2];
            string outputDir = args[3];

            // 옵션 파싱
            int? populationSize = null;
            int eliteCount = 2;
            float mutationRate = 0.1f;
            string selectionMethod = "tournament";

            for (int i = 4; i < args.Length; i++)
            {
                if (args[i].StartsWith("--size="))
                    populationSize = int.Parse(args[i].Substring("--size=".Length));
                else if (args[i].StartsWith("--elite="))
                    eliteCount = int.Parse(args[i].Substring("--elite=".Length));
                else if (args[i].StartsWith("--mutation="))
                    mutationRate = float.Parse(args[i].Substring("--mutation=".Length));
                else if (args[i].StartsWith("--selection="))
                    selectionMethod = args[i].Substring("--selection=".Length);
            }

            Console.WriteLine($"[evolve] Genomes 디렉토리: {genomesDir}");
            Console.WriteLine($"[evolve] Runs 디렉토리: {runsDir}");
            Console.WriteLine($"[evolve] 출력 디렉토리: {outputDir}");
            Console.WriteLine();

            // 현재 세대 로드
            var individuals = DataLoader.LoadPopulationFromFiles(genomesDir, runsDir);

            if (individuals.Count == 0)
            {
                Console.WriteLine("[evolve] 로드된 개체가 없습니다. 먼저 'init' 명령으로 초기 세대를 생성하세요.");
                return;
            }

            var currentGeneration = individuals.Max(i => i.Genome.Generation);
            var currentPopulation = new Population(currentGeneration);
            currentPopulation.AddIndividuals(individuals);

            Console.WriteLine($"[evolve] 현재 세대: Gen {currentGeneration}, 크기: {currentPopulation.Size}");
            Console.WriteLine(currentPopulation);
            Console.WriteLine();

            if (currentPopulation.EvaluatedCount == 0)
            {
                Console.WriteLine("[evolve] 평가된 개체가 없습니다. 게임을 실행하여 metrics를 수집하세요.");
                return;
            }

            // 다음 세대 크기 결정
            int nextSize = populationSize ?? currentPopulation.Size;

            Console.WriteLine($"[evolve] 다음 세대 설정:");
            Console.WriteLine($"  - 크기: {nextSize}");
            Console.WriteLine($"  - 엘리트: {eliteCount}");
            Console.WriteLine($"  - 돌연변이 확률: {mutationRate:F2}");
            Console.WriteLine($"  - 선택 방법: {selectionMethod}");
            Console.WriteLine();

            // 다음 세대 생성
            var nextPopulation = GAOperations.GenerateNextGeneration(
                currentPopulation,
                populationSize: nextSize,
                eliteCount: eliteCount,
                mutationRate: mutationRate,
                selectionMethod: selectionMethod
            );

            // 저장
            DataLoader.SaveGenomes(nextPopulation, outputDir);
            DataLoader.SaveBestGenomeAsCurrent(currentPopulation, outputDir);

            Console.WriteLine();
            Console.WriteLine("[evolve] 완료!");
            Console.WriteLine($"  - 다음 세대: Gen {nextPopulation.Generation}, 크기: {nextPopulation.Size}");
            Console.WriteLine($"  - 저장 위치: {outputDir}");

            // 최고 개체 요약
            var best = currentPopulation.BestIndividual;
            if (best != null)
            {
                Console.WriteLine();
                Console.WriteLine("=== 최고 개체 (current.json) ===");
                Console.WriteLine(best);
                Console.WriteLine($"Genome: {best.Genome}");
            }
        }

        /// <summary>
        /// stats 명령: 통계 출력
        /// </summary>
        static void CommandStats(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("[stats] 인자 부족: GATrainer stats <genomes디렉토리> <runs디렉토리>");
                return;
            }

            string genomesDir = args[1];
            string runsDir = args[2];

            Console.WriteLine($"[stats] Genomes 디렉토리: {genomesDir}");
            Console.WriteLine($"[stats] Runs 디렉토리: {runsDir}");
            Console.WriteLine();

            // 데이터 로드
            var individuals = DataLoader.LoadPopulationFromFiles(genomesDir, runsDir);

            if (individuals.Count == 0)
            {
                Console.WriteLine("[stats] 로드된 개체가 없습니다.");
                return;
            }

            var generation = individuals.Max(i => i.Genome.Generation);
            var population = new Population(generation);
            population.AddIndividuals(individuals);

            Console.WriteLine(population.GetDetailedStats());
            Console.WriteLine();

            // 상위 5개 개체 출력
            var topN = population.GetTopN(Math.Min(5, population.EvaluatedCount));
            Console.WriteLine("=== 상위 5개 개체 ===");
            for (int i = 0; i < topN.Count; i++)
            {
                Console.WriteLine($"[{i + 1}] {topN[i]}");
            }

            // 최고 개체 상세 정보
            var best = population.BestIndividual;
            if (best != null)
            {
                Console.WriteLine();
                Console.WriteLine("=== 최고 개체 상세 ===");
                Console.WriteLine($"Genome: {best.Genome}");
                Console.WriteLine($"Metrics: {best.Metrics}");
            }
        }
    }
}
