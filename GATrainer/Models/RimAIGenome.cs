using System;
using System.IO;
using System.Text.Json;

namespace GATrainer.Models
{
    /// <summary>
    /// RimAI GA 최적화를 위한 Genome (파라미터 세트)
    /// 각 서브시스템의 가중치, 임계값, 쿨다운 등을 포함
    /// </summary>
    public class RimAIGenome
    {
        // === Genome 메타 정보 ===
        public string GenomeId { get; set; } = "default";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int Generation { get; set; } = 0;
        public string ParentId1 { get; set; } = "";
        public string ParentId2 { get; set; } = "";

        // === 서브시스템 우선순위 가중치 (0.5 - 1.5) ===
        public float FoodPriorityWeight { get; set; } = 1.0f;
        public float CombatPriorityWeight { get; set; } = 1.0f;
        public float ConstructionPriorityWeight { get; set; } = 1.0f;
        public float ProductionPriorityWeight { get; set; } = 1.0f;
        public float ResearchPriorityWeight { get; set; } = 1.0f;

        // === 식량 관리 파라미터 ===
        public float FoodCrisisThreshold { get; set; } = 4.0f;
        public float FoodWarningThreshold { get; set; } = 7.0f;
        public float FoodStableThreshold { get; set; } = 15.0f;
        public int WinterPrepDays { get; set; } = 30;

        // === 건설 관리 파라미터 ===
        public int BedBuffer { get; set; } = 2;
        public float DefensePerColonist { get; set; } = 2.0f;
        public int ConstructionCooldown { get; set; } = 3600;

        // === 생산 관리 파라미터 ===
        public int SteelShortageThreshold { get; set; } = 100;
        public int ComponentShortageThreshold { get; set; } = 5;
        public int SteelSurplusThreshold { get; set; } = 500;
        public int ComponentSurplusThreshold { get; set; } = 20;
        public int ProductionCooldown { get; set; } = 3600;
        public int MedicinePerColonist { get; set; } = 10;

        // === 전투 관리 파라미터 ===
        public float ThreatDetectionRange { get; set; } = 30f;
        public int CombatStartThreshold { get; set; } = 3;
        public int CombatEndDelay { get; set; } = 2500;

        // === 연구 관리 파라미터 ===
        public float ResearchCombatPriority { get; set; } = 1.0f;
        public float ResearchEconomyPriority { get; set; } = 1.0f;
        public float ResearchMedicalPriority { get; set; } = 1.0f;

        // === 플레이 스타일 편향 (0.5 - 1.5) ===
        public float DefensiveBias { get; set; } = 1.0f;
        public float ExpansionBias { get; set; } = 1.0f;
        public float ResearchBias { get; set; } = 1.0f;
        public float WelfareBias { get; set; } = 1.0f;

        // === 업데이트 주기 배율 (0.7 - 1.3) ===
        public float UpdateSpeedMultiplier { get; set; } = 1.0f;

        public static RimAIGenome CreateDefault()
        {
            return new RimAIGenome
            {
                GenomeId = "default",
                Generation = 0
            };
        }

        public static RimAIGenome CreateRandom(int generation = 0)
        {
            var random = new Random();
            var genome = new RimAIGenome
            {
                GenomeId = $"gen{generation}_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Generation = generation,
                CreatedAt = DateTime.UtcNow
            };

            genome.FoodPriorityWeight = RandomFloat(random, 0.5f, 1.5f);
            genome.CombatPriorityWeight = RandomFloat(random, 0.5f, 1.5f);
            genome.ConstructionPriorityWeight = RandomFloat(random, 0.5f, 1.5f);
            genome.ProductionPriorityWeight = RandomFloat(random, 0.5f, 1.5f);
            genome.ResearchPriorityWeight = RandomFloat(random, 0.5f, 1.5f);

            genome.FoodCrisisThreshold = RandomFloat(random, 2.0f, 6.0f);
            genome.FoodWarningThreshold = RandomFloat(random, 5.0f, 10.0f);
            genome.FoodStableThreshold = RandomFloat(random, 10.0f, 20.0f);
            genome.WinterPrepDays = random.Next(20, 40);

            genome.BedBuffer = random.Next(1, 4);
            genome.DefensePerColonist = RandomFloat(random, 1.0f, 3.0f);
            genome.ConstructionCooldown = random.Next(1800, 7200);

            genome.SteelShortageThreshold = random.Next(50, 150);
            genome.ComponentShortageThreshold = random.Next(3, 10);
            genome.SteelSurplusThreshold = random.Next(300, 700);
            genome.ComponentSurplusThreshold = random.Next(15, 30);
            genome.ProductionCooldown = random.Next(1800, 7200);
            genome.MedicinePerColonist = random.Next(5, 15);

            genome.ThreatDetectionRange = RandomFloat(random, 20f, 40f);
            genome.CombatStartThreshold = random.Next(2, 5);
            genome.CombatEndDelay = random.Next(1500, 3600);

            genome.ResearchCombatPriority = RandomFloat(random, 0.5f, 1.5f);
            genome.ResearchEconomyPriority = RandomFloat(random, 0.5f, 1.5f);
            genome.ResearchMedicalPriority = RandomFloat(random, 0.5f, 1.5f);

            genome.DefensiveBias = RandomFloat(random, 0.5f, 1.5f);
            genome.ExpansionBias = RandomFloat(random, 0.5f, 1.5f);
            genome.ResearchBias = RandomFloat(random, 0.5f, 1.5f);
            genome.WelfareBias = RandomFloat(random, 0.5f, 1.5f);

            genome.UpdateSpeedMultiplier = RandomFloat(random, 0.7f, 1.3f);

            return genome;
        }

        public static RimAIGenome Crossover(RimAIGenome parent1, RimAIGenome parent2, int generation)
        {
            var random = new Random();
            var child = new RimAIGenome
            {
                GenomeId = $"gen{generation}_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Generation = generation,
                CreatedAt = DateTime.UtcNow,
                ParentId1 = parent1.GenomeId,
                ParentId2 = parent2.GenomeId
            };

            child.FoodPriorityWeight = random.Next(2) == 0 ? parent1.FoodPriorityWeight : parent2.FoodPriorityWeight;
            child.CombatPriorityWeight = random.Next(2) == 0 ? parent1.CombatPriorityWeight : parent2.CombatPriorityWeight;
            child.ConstructionPriorityWeight = random.Next(2) == 0 ? parent1.ConstructionPriorityWeight : parent2.ConstructionPriorityWeight;
            child.ProductionPriorityWeight = random.Next(2) == 0 ? parent1.ProductionPriorityWeight : parent2.ProductionPriorityWeight;
            child.ResearchPriorityWeight = random.Next(2) == 0 ? parent1.ResearchPriorityWeight : parent2.ResearchPriorityWeight;

            child.FoodCrisisThreshold = random.Next(2) == 0 ? parent1.FoodCrisisThreshold : parent2.FoodCrisisThreshold;
            child.FoodWarningThreshold = random.Next(2) == 0 ? parent1.FoodWarningThreshold : parent2.FoodWarningThreshold;
            child.FoodStableThreshold = random.Next(2) == 0 ? parent1.FoodStableThreshold : parent2.FoodStableThreshold;
            child.WinterPrepDays = random.Next(2) == 0 ? parent1.WinterPrepDays : parent2.WinterPrepDays;

            child.BedBuffer = random.Next(2) == 0 ? parent1.BedBuffer : parent2.BedBuffer;
            child.DefensePerColonist = random.Next(2) == 0 ? parent1.DefensePerColonist : parent2.DefensePerColonist;
            child.ConstructionCooldown = random.Next(2) == 0 ? parent1.ConstructionCooldown : parent2.ConstructionCooldown;

            child.SteelShortageThreshold = random.Next(2) == 0 ? parent1.SteelShortageThreshold : parent2.SteelShortageThreshold;
            child.ComponentShortageThreshold = random.Next(2) == 0 ? parent1.ComponentShortageThreshold : parent2.ComponentShortageThreshold;
            child.SteelSurplusThreshold = random.Next(2) == 0 ? parent1.SteelSurplusThreshold : parent2.SteelSurplusThreshold;
            child.ComponentSurplusThreshold = random.Next(2) == 0 ? parent1.ComponentSurplusThreshold : parent2.ComponentSurplusThreshold;
            child.ProductionCooldown = random.Next(2) == 0 ? parent1.ProductionCooldown : parent2.ProductionCooldown;
            child.MedicinePerColonist = random.Next(2) == 0 ? parent1.MedicinePerColonist : parent2.MedicinePerColonist;

            child.ThreatDetectionRange = random.Next(2) == 0 ? parent1.ThreatDetectionRange : parent2.ThreatDetectionRange;
            child.CombatStartThreshold = random.Next(2) == 0 ? parent1.CombatStartThreshold : parent2.CombatStartThreshold;
            child.CombatEndDelay = random.Next(2) == 0 ? parent1.CombatEndDelay : parent2.CombatEndDelay;

            child.ResearchCombatPriority = random.Next(2) == 0 ? parent1.ResearchCombatPriority : parent2.ResearchCombatPriority;
            child.ResearchEconomyPriority = random.Next(2) == 0 ? parent1.ResearchEconomyPriority : parent2.ResearchEconomyPriority;
            child.ResearchMedicalPriority = random.Next(2) == 0 ? parent1.ResearchMedicalPriority : parent2.ResearchMedicalPriority;

            child.DefensiveBias = random.Next(2) == 0 ? parent1.DefensiveBias : parent2.DefensiveBias;
            child.ExpansionBias = random.Next(2) == 0 ? parent1.ExpansionBias : parent2.ExpansionBias;
            child.ResearchBias = random.Next(2) == 0 ? parent1.ResearchBias : parent2.ResearchBias;
            child.WelfareBias = random.Next(2) == 0 ? parent1.WelfareBias : parent2.WelfareBias;

            child.UpdateSpeedMultiplier = random.Next(2) == 0 ? parent1.UpdateSpeedMultiplier : parent2.UpdateSpeedMultiplier;

            return child;
        }

        public void Mutate(float mutationRate = 0.1f)
        {
            var random = new Random();

            if (random.NextDouble() < mutationRate) FoodPriorityWeight = RandomFloat(random, 0.5f, 1.5f);
            if (random.NextDouble() < mutationRate) CombatPriorityWeight = RandomFloat(random, 0.5f, 1.5f);
            if (random.NextDouble() < mutationRate) ConstructionPriorityWeight = RandomFloat(random, 0.5f, 1.5f);
            if (random.NextDouble() < mutationRate) ProductionPriorityWeight = RandomFloat(random, 0.5f, 1.5f);
            if (random.NextDouble() < mutationRate) ResearchPriorityWeight = RandomFloat(random, 0.5f, 1.5f);

            if (random.NextDouble() < mutationRate) FoodCrisisThreshold = RandomFloat(random, 2.0f, 6.0f);
            if (random.NextDouble() < mutationRate) FoodWarningThreshold = RandomFloat(random, 5.0f, 10.0f);
            if (random.NextDouble() < mutationRate) FoodStableThreshold = RandomFloat(random, 10.0f, 20.0f);
            if (random.NextDouble() < mutationRate) WinterPrepDays = random.Next(20, 40);

            if (random.NextDouble() < mutationRate) BedBuffer = random.Next(1, 4);
            if (random.NextDouble() < mutationRate) DefensePerColonist = RandomFloat(random, 1.0f, 3.0f);
            if (random.NextDouble() < mutationRate) ConstructionCooldown = random.Next(1800, 7200);

            if (random.NextDouble() < mutationRate) SteelShortageThreshold = random.Next(50, 150);
            if (random.NextDouble() < mutationRate) ComponentShortageThreshold = random.Next(3, 10);
            if (random.NextDouble() < mutationRate) SteelSurplusThreshold = random.Next(300, 700);
            if (random.NextDouble() < mutationRate) ComponentSurplusThreshold = random.Next(15, 30);
            if (random.NextDouble() < mutationRate) ProductionCooldown = random.Next(1800, 7200);
            if (random.NextDouble() < mutationRate) MedicinePerColonist = random.Next(5, 15);

            if (random.NextDouble() < mutationRate) ThreatDetectionRange = RandomFloat(random, 20f, 40f);
            if (random.NextDouble() < mutationRate) CombatStartThreshold = random.Next(2, 5);
            if (random.NextDouble() < mutationRate) CombatEndDelay = random.Next(1500, 3600);

            if (random.NextDouble() < mutationRate) ResearchCombatPriority = RandomFloat(random, 0.5f, 1.5f);
            if (random.NextDouble() < mutationRate) ResearchEconomyPriority = RandomFloat(random, 0.5f, 1.5f);
            if (random.NextDouble() < mutationRate) ResearchMedicalPriority = RandomFloat(random, 0.5f, 1.5f);

            if (random.NextDouble() < mutationRate) DefensiveBias = RandomFloat(random, 0.5f, 1.5f);
            if (random.NextDouble() < mutationRate) ExpansionBias = RandomFloat(random, 0.5f, 1.5f);
            if (random.NextDouble() < mutationRate) ResearchBias = RandomFloat(random, 0.5f, 1.5f);
            if (random.NextDouble() < mutationRate) WelfareBias = RandomFloat(random, 0.5f, 1.5f);

            if (random.NextDouble() < mutationRate) UpdateSpeedMultiplier = RandomFloat(random, 0.7f, 1.3f);
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }

        public static RimAIGenome FromJson(string json)
        {
            try
            {
                var genome = JsonSerializer.Deserialize<RimAIGenome>(json);
                return genome ?? CreateDefault();
            }
            catch
            {
                return CreateDefault();
            }
        }

        public void SaveToFile(string filePath)
        {
            File.WriteAllText(filePath, ToJson());
        }

        public static RimAIGenome LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return CreateDefault();

            string json = File.ReadAllText(filePath);
            return FromJson(json);
        }

        public override string ToString()
        {
            return $"[Genome:{GenomeId}] Gen:{Generation} | " +
                   $"Weight(F:{FoodPriorityWeight:F2} C:{CombatPriorityWeight:F2} B:{ConstructionPriorityWeight:F2} P:{ProductionPriorityWeight:F2} R:{ResearchPriorityWeight:F2}) | " +
                   $"Food({FoodCrisisThreshold:F1}/{FoodWarningThreshold:F1}) | " +
                   $"Steel({SteelShortageThreshold}/{SteelSurplusThreshold}) | " +
                   $"Bias(D:{DefensiveBias:F2} E:{ExpansionBias:F2} R:{ResearchBias:F2})";
        }

        private static float RandomFloat(Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
