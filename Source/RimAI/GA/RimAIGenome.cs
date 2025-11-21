using System;
using System.IO;
using Verse;

namespace RimAI.GA
{
    /// <summary>
    /// RimAI GA 최적화를 위한 Genome (파라미터 세트)
    /// 각 서브시스템의 가중치, 임계값, 쿨다운 등을 포함
    /// </summary>
    public class RimAIGenome
    {
        // === Genome 메타 정보 ===
        /// <summary>Genome ID</summary>
        public string GenomeId { get; set; } = "default";

        /// <summary>생성 시각</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>세대 번호 (GA에서 사용)</summary>
        public int Generation { get; set; } = 0;

        /// <summary>부모 ID (교배 시 추적용)</summary>
        public string ParentId1 { get; set; } = "";
        public string ParentId2 { get; set; } = "";

        // === 서브시스템 우선순위 가중치 (0.0 - 2.0) ===
        /// <summary>식량 서브시스템 가중치</summary>
        public float FoodPriorityWeight { get; set; } = 1.0f;

        /// <summary>전투 서브시스템 가중치</summary>
        public float CombatPriorityWeight { get; set; } = 1.0f;

        /// <summary>건설 서브시스템 가중치</summary>
        public float ConstructionPriorityWeight { get; set; } = 1.0f;

        /// <summary>생산 서브시스템 가중치</summary>
        public float ProductionPriorityWeight { get; set; } = 1.0f;

        /// <summary>연구 서브시스템 가중치</summary>
        public float ResearchPriorityWeight { get; set; } = 1.0f;

        // === 식량 관리 파라미터 ===
        /// <summary>식량 위기 임계값 (일 단위, 기본 4.0일)</summary>
        public float FoodCrisisThreshold { get; set; } = 4.0f;

        /// <summary>식량 위험 임계값 (일 단위, 기본 7.0일)</summary>
        public float FoodWarningThreshold { get; set; } = 7.0f;

        /// <summary>식량 안정 임계값 (일 단위, 기본 15.0일)</summary>
        public float FoodStableThreshold { get; set; } = 15.0f;

        /// <summary>겨울 대비 시작 일수 (겨울 X일 전, 기본 30일)</summary>
        public int WinterPrepDays { get; set; } = 30;

        // === 건설 관리 파라미터 ===
        /// <summary>침대 여유분 (콜로니스트 수 + X, 기본 2)</summary>
        public int BedBuffer { get; set; } = 2;

        /// <summary>방어 시설 비율 (콜로니스트당 X개, 기본 2.0)</summary>
        public float DefensePerColonist { get; set; } = 2.0f;

        /// <summary>건설 쿨다운 (틱, 기본 3600 = 1분)</summary>
        public int ConstructionCooldown { get; set; } = 3600;

        // === 생산 관리 파라미터 ===
        /// <summary>자원 부족 임계값 - 강철 (기본 100)</summary>
        public int SteelShortageThreshold { get; set; } = 100;

        /// <summary>자원 부족 임계값 - 컴포넌트 (기본 5)</summary>
        public int ComponentShortageThreshold { get; set; } = 5;

        /// <summary>자원 여유 임계값 - 강철 (기본 500)</summary>
        public int SteelSurplusThreshold { get; set; } = 500;

        /// <summary>자원 여유 임계값 - 컴포넌트 (기본 20)</summary>
        public int ComponentSurplusThreshold { get; set; } = 20;

        /// <summary>생산 쿨다운 (틱, 기본 3600 = 1분)</summary>
        public int ProductionCooldown { get; set; } = 3600;

        /// <summary>의약품 목표 (콜로니스트당 X개, 기본 10)</summary>
        public int MedicinePerColonist { get; set; } = 10;

        // === 전투 관리 파라미터 ===
        /// <summary>위협 인식 거리 (타일, 기본 30)</summary>
        public float ThreatDetectionRange { get; set; } = 30f;

        /// <summary>전투 시작 적 수 임계값 (기본 3)</summary>
        public int CombatStartThreshold { get; set; } = 3;

        /// <summary>전투 종료 대기 시간 (틱, 기본 2500)</summary>
        public int CombatEndDelay { get; set; } = 2500;

        // === 연구 관리 파라미터 ===
        /// <summary>연구 우선순위 - 전투 기술 (0.0 - 2.0)</summary>
        public float ResearchCombatPriority { get; set; } = 1.0f;

        /// <summary>연구 우선순위 - 경제 기술 (0.0 - 2.0)</summary>
        public float ResearchEconomyPriority { get; set; } = 1.0f;

        /// <summary>연구 우선순위 - 의료 기술 (0.0 - 2.0)</summary>
        public float ResearchMedicalPriority { get; set; } = 1.0f;

        // === 플레이 스타일 편향 (0.0 - 2.0) ===
        /// <summary>방어 편향 (Fortress 스타일)</summary>
        public float DefensiveBias { get; set; } = 1.0f;

        /// <summary>확장 편향 (Expansion 스타일)</summary>
        public float ExpansionBias { get; set; } = 1.0f;

        /// <summary>연구 편향 (Researcher 스타일)</summary>
        public float ResearchBias { get; set; } = 1.0f;

        /// <summary>복지 편향 (콜로니스트 무드 우선)</summary>
        public float WelfareBias { get; set; } = 1.0f;

        // === 업데이트 주기 배율 (0.5 - 2.0) ===
        /// <summary>전체 업데이트 속도 배율 (낮을수록 자주 업데이트)</summary>
        public float UpdateSpeedMultiplier { get; set; } = 1.0f;

        /// <summary>
        /// 기본값으로 Genome 생성
        /// </summary>
        public static RimAIGenome CreateDefault()
        {
            return new RimAIGenome
            {
                GenomeId = "default",
                Generation = 0
            };
        }

        /// <summary>
        /// 랜덤 Genome 생성 (GA 초기 세대용)
        /// </summary>
        public static RimAIGenome CreateRandom(int generation = 0)
        {
            var random = new System.Random();
            var genome = new RimAIGenome
            {
                GenomeId = $"gen{generation}_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Generation = generation,
                CreatedAt = DateTime.UtcNow
            };

            // 가중치 (0.5 - 1.5)
            genome.FoodPriorityWeight = RandomFloat(random, 0.5f, 1.5f);
            genome.CombatPriorityWeight = RandomFloat(random, 0.5f, 1.5f);
            genome.ConstructionPriorityWeight = RandomFloat(random, 0.5f, 1.5f);
            genome.ProductionPriorityWeight = RandomFloat(random, 0.5f, 1.5f);
            genome.ResearchPriorityWeight = RandomFloat(random, 0.5f, 1.5f);

            // 식량 임계값
            genome.FoodCrisisThreshold = RandomFloat(random, 2.0f, 6.0f);
            genome.FoodWarningThreshold = RandomFloat(random, 5.0f, 10.0f);
            genome.FoodStableThreshold = RandomFloat(random, 10.0f, 20.0f);
            genome.WinterPrepDays = random.Next(20, 40);

            // 건설 파라미터
            genome.BedBuffer = random.Next(1, 4);
            genome.DefensePerColonist = RandomFloat(random, 1.0f, 3.0f);
            genome.ConstructionCooldown = random.Next(1800, 7200); // 30초 ~ 2분

            // 생산 파라미터
            genome.SteelShortageThreshold = random.Next(50, 150);
            genome.ComponentShortageThreshold = random.Next(3, 10);
            genome.SteelSurplusThreshold = random.Next(300, 700);
            genome.ComponentSurplusThreshold = random.Next(15, 30);
            genome.ProductionCooldown = random.Next(1800, 7200);
            genome.MedicinePerColonist = random.Next(5, 15);

            // 전투 파라미터
            genome.ThreatDetectionRange = RandomFloat(random, 20f, 40f);
            genome.CombatStartThreshold = random.Next(2, 5);
            genome.CombatEndDelay = random.Next(1500, 3600);

            // 연구 우선순위
            genome.ResearchCombatPriority = RandomFloat(random, 0.5f, 1.5f);
            genome.ResearchEconomyPriority = RandomFloat(random, 0.5f, 1.5f);
            genome.ResearchMedicalPriority = RandomFloat(random, 0.5f, 1.5f);

            // 플레이 스타일 편향
            genome.DefensiveBias = RandomFloat(random, 0.5f, 1.5f);
            genome.ExpansionBias = RandomFloat(random, 0.5f, 1.5f);
            genome.ResearchBias = RandomFloat(random, 0.5f, 1.5f);
            genome.WelfareBias = RandomFloat(random, 0.5f, 1.5f);

            // 업데이트 속도
            genome.UpdateSpeedMultiplier = RandomFloat(random, 0.7f, 1.3f);

            return genome;
        }

        /// <summary>
        /// 두 Genome을 교배 (Crossover)
        /// </summary>
        public static RimAIGenome Crossover(RimAIGenome parent1, RimAIGenome parent2, int generation)
        {
            var random = new System.Random();
            var child = new RimAIGenome
            {
                GenomeId = $"gen{generation}_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Generation = generation,
                CreatedAt = DateTime.UtcNow,
                ParentId1 = parent1.GenomeId,
                ParentId2 = parent2.GenomeId
            };

            // 각 파라미터마다 50% 확률로 부모 선택
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

        /// <summary>
        /// 돌연변이 (Mutation)
        /// </summary>
        public void Mutate(float mutationRate = 0.1f)
        {
            var random = new System.Random();

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

            if (random.NextDouble() < mutationRate) SteelShortageThreshold = random.Next(50, 150);
            if (random.NextDouble() < mutationRate) ComponentShortageThreshold = random.Next(3, 10);

            if (random.NextDouble() < mutationRate) DefensiveBias = RandomFloat(random, 0.5f, 1.5f);
            if (random.NextDouble() < mutationRate) ExpansionBias = RandomFloat(random, 0.5f, 1.5f);
            if (random.NextDouble() < mutationRate) ResearchBias = RandomFloat(random, 0.5f, 1.5f);
            if (random.NextDouble() < mutationRate) WelfareBias = RandomFloat(random, 0.5f, 1.5f);
        }

        /// <summary>
        /// JSON으로 직렬화 (수동 구현 - .NET Framework 4.7.2 호환)
        /// </summary>
        public string ToJson()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                var culture = System.Globalization.CultureInfo.InvariantCulture;
                sb.AppendLine("{");
                sb.AppendLine($"  \"GenomeId\": \"{EscapeJson(GenomeId)}\",");
                sb.AppendLine($"  \"CreatedAt\": \"{CreatedAt:O}\",");
                sb.AppendLine($"  \"Generation\": {Generation},");
                sb.AppendLine($"  \"ParentId1\": \"{EscapeJson(ParentId1)}\",");
                sb.AppendLine($"  \"ParentId2\": \"{EscapeJson(ParentId2)}\",");
                sb.AppendLine($"  \"FoodPriorityWeight\": {FoodPriorityWeight.ToString(culture)},");
                sb.AppendLine($"  \"CombatPriorityWeight\": {CombatPriorityWeight.ToString(culture)},");
                sb.AppendLine($"  \"ConstructionPriorityWeight\": {ConstructionPriorityWeight.ToString(culture)},");
                sb.AppendLine($"  \"ProductionPriorityWeight\": {ProductionPriorityWeight.ToString(culture)},");
                sb.AppendLine($"  \"ResearchPriorityWeight\": {ResearchPriorityWeight.ToString(culture)},");
                sb.AppendLine($"  \"FoodCrisisThreshold\": {FoodCrisisThreshold.ToString(culture)},");
                sb.AppendLine($"  \"FoodWarningThreshold\": {FoodWarningThreshold.ToString(culture)},");
                sb.AppendLine($"  \"FoodStableThreshold\": {FoodStableThreshold.ToString(culture)},");
                sb.AppendLine($"  \"WinterPrepDays\": {WinterPrepDays},");
                sb.AppendLine($"  \"BedBuffer\": {BedBuffer},");
                sb.AppendLine($"  \"DefensePerColonist\": {DefensePerColonist.ToString(culture)},");
                sb.AppendLine($"  \"ConstructionCooldown\": {ConstructionCooldown},");
                sb.AppendLine($"  \"SteelShortageThreshold\": {SteelShortageThreshold},");
                sb.AppendLine($"  \"ComponentShortageThreshold\": {ComponentShortageThreshold},");
                sb.AppendLine($"  \"SteelSurplusThreshold\": {SteelSurplusThreshold},");
                sb.AppendLine($"  \"ComponentSurplusThreshold\": {ComponentSurplusThreshold},");
                sb.AppendLine($"  \"ProductionCooldown\": {ProductionCooldown},");
                sb.AppendLine($"  \"MedicinePerColonist\": {MedicinePerColonist},");
                sb.AppendLine($"  \"ThreatDetectionRange\": {ThreatDetectionRange.ToString(culture)},");
                sb.AppendLine($"  \"CombatStartThreshold\": {CombatStartThreshold},");
                sb.AppendLine($"  \"CombatEndDelay\": {CombatEndDelay},");
                sb.AppendLine($"  \"ResearchCombatPriority\": {ResearchCombatPriority.ToString(culture)},");
                sb.AppendLine($"  \"ResearchEconomyPriority\": {ResearchEconomyPriority.ToString(culture)},");
                sb.AppendLine($"  \"ResearchMedicalPriority\": {ResearchMedicalPriority.ToString(culture)},");
                sb.AppendLine($"  \"DefensiveBias\": {DefensiveBias.ToString(culture)},");
                sb.AppendLine($"  \"ExpansionBias\": {ExpansionBias.ToString(culture)},");
                sb.AppendLine($"  \"ResearchBias\": {ResearchBias.ToString(culture)},");
                sb.AppendLine($"  \"WelfareBias\": {WelfareBias.ToString(culture)},");
                sb.AppendLine($"  \"UpdateSpeedMultiplier\": {UpdateSpeedMultiplier.ToString(culture)}");
                sb.AppendLine("}");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI-GA] Genome JSON 직렬화 실패: {ex.Message}");
                return "{}";
            }
        }

        /// <summary>
        /// JSON에서 역직렬화 (수동 구현 - .NET Framework 4.7.2 호환)
        /// </summary>
        public static RimAIGenome FromJson(string json)
        {
            try
            {
                var genome = new RimAIGenome();
                genome.GenomeId = ParseJsonString(json, "GenomeId") ?? "default";
                genome.ParentId1 = ParseJsonString(json, "ParentId1") ?? "";
                genome.ParentId2 = ParseJsonString(json, "ParentId2") ?? "";
                genome.Generation = ParseJsonInt(json, "Generation");
                genome.FoodPriorityWeight = ParseJsonFloat(json, "FoodPriorityWeight", 1.0f);
                genome.CombatPriorityWeight = ParseJsonFloat(json, "CombatPriorityWeight", 1.0f);
                genome.ConstructionPriorityWeight = ParseJsonFloat(json, "ConstructionPriorityWeight", 1.0f);
                genome.ProductionPriorityWeight = ParseJsonFloat(json, "ProductionPriorityWeight", 1.0f);
                genome.ResearchPriorityWeight = ParseJsonFloat(json, "ResearchPriorityWeight", 1.0f);
                genome.FoodCrisisThreshold = ParseJsonFloat(json, "FoodCrisisThreshold", 4.0f);
                genome.FoodWarningThreshold = ParseJsonFloat(json, "FoodWarningThreshold", 7.0f);
                genome.FoodStableThreshold = ParseJsonFloat(json, "FoodStableThreshold", 15.0f);
                genome.WinterPrepDays = ParseJsonInt(json, "WinterPrepDays", 30);
                genome.BedBuffer = ParseJsonInt(json, "BedBuffer", 2);
                genome.DefensePerColonist = ParseJsonFloat(json, "DefensePerColonist", 2.0f);
                genome.ConstructionCooldown = ParseJsonInt(json, "ConstructionCooldown", 3600);
                genome.SteelShortageThreshold = ParseJsonInt(json, "SteelShortageThreshold", 100);
                genome.ComponentShortageThreshold = ParseJsonInt(json, "ComponentShortageThreshold", 5);
                genome.SteelSurplusThreshold = ParseJsonInt(json, "SteelSurplusThreshold", 500);
                genome.ComponentSurplusThreshold = ParseJsonInt(json, "ComponentSurplusThreshold", 20);
                genome.ProductionCooldown = ParseJsonInt(json, "ProductionCooldown", 3600);
                genome.MedicinePerColonist = ParseJsonInt(json, "MedicinePerColonist", 10);
                genome.ThreatDetectionRange = ParseJsonFloat(json, "ThreatDetectionRange", 30f);
                genome.CombatStartThreshold = ParseJsonInt(json, "CombatStartThreshold", 3);
                genome.CombatEndDelay = ParseJsonInt(json, "CombatEndDelay", 2500);
                genome.ResearchCombatPriority = ParseJsonFloat(json, "ResearchCombatPriority", 1.0f);
                genome.ResearchEconomyPriority = ParseJsonFloat(json, "ResearchEconomyPriority", 1.0f);
                genome.ResearchMedicalPriority = ParseJsonFloat(json, "ResearchMedicalPriority", 1.0f);
                genome.DefensiveBias = ParseJsonFloat(json, "DefensiveBias", 1.0f);
                genome.ExpansionBias = ParseJsonFloat(json, "ExpansionBias", 1.0f);
                genome.ResearchBias = ParseJsonFloat(json, "ResearchBias", 1.0f);
                genome.WelfareBias = ParseJsonFloat(json, "WelfareBias", 1.0f);
                genome.UpdateSpeedMultiplier = ParseJsonFloat(json, "UpdateSpeedMultiplier", 1.0f);
                return genome;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI-GA] Genome JSON 역직렬화 실패: {ex.Message}");
                return CreateDefault();
            }
        }

        private static string EscapeJson(string s) => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";

        private static string ParseJsonString(string json, string key)
        {
            var pattern = $"\"{key}\"\\s*:\\s*\"([^\"]*)\"";
            var match = System.Text.RegularExpressions.Regex.Match(json, pattern);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static int ParseJsonInt(string json, string key, int defaultValue = 0)
        {
            var pattern = $"\"{key}\"\\s*:\\s*(-?\\d+)";
            var match = System.Text.RegularExpressions.Regex.Match(json, pattern);
            return match.Success && int.TryParse(match.Groups[1].Value, out int val) ? val : defaultValue;
        }

        private static float ParseJsonFloat(string json, string key, float defaultValue = 0f)
        {
            var pattern = $"\"{key}\"\\s*:\\s*(-?[\\d.]+)";
            var match = System.Text.RegularExpressions.Regex.Match(json, pattern);
            return match.Success && float.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float val) ? val : defaultValue;
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
                Log.Message($"[RimAI-GA] Genome 저장 완료: {filePath}");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI-GA] Genome 파일 저장 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 파일에서 로드
        /// </summary>
        public static RimAIGenome LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Log.Warning($"[RimAI-GA] Genome 파일 없음: {filePath}");
                    return CreateDefault();
                }

                string json = File.ReadAllText(filePath);
                return FromJson(json);
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI-GA] Genome 파일 로드 실패: {ex.Message}");
                return CreateDefault();
            }
        }

        /// <summary>
        /// 디버그 출력용 요약
        /// </summary>
        public override string ToString()
        {
            return $"[Genome:{GenomeId}] Gen:{Generation} | " +
                   $"Weight(F:{FoodPriorityWeight:F2} C:{CombatPriorityWeight:F2} B:{ConstructionPriorityWeight:F2} P:{ProductionPriorityWeight:F2} R:{ResearchPriorityWeight:F2}) | " +
                   $"Food({FoodCrisisThreshold:F1}/{FoodWarningThreshold:F1}) | " +
                   $"Steel({SteelShortageThreshold}/{SteelSurplusThreshold}) | " +
                   $"Bias(D:{DefensiveBias:F2} E:{ExpansionBias:F2} R:{ResearchBias:F2})";
        }

        // === 헬퍼 메서드 ===
        private static float RandomFloat(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
