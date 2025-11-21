using System.Text;
using RimAI.Core;
using Verse;

namespace RimAI.GA
{
    /// <summary>
    /// GA 디버그 도구
    /// Genome 및 Metrics 요약을 로그로 출력
    /// </summary>
    public static class GADebugTools
    {
        /// <summary>
        /// 현재 Genome 요약 출력
        /// </summary>
        public static void LogCurrentGenome()
        {
            var manager = RimAIManager.Instance;
            if (manager == null)
            {
                Log.Warning("[RimAI-GA] RimAIManager 인스턴스 없음");
                return;
            }

            var genome = manager.GetCurrentGenome();
            if (genome == null)
            {
                Log.Warning("[RimAI-GA] Genome 없음");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== RimAI Genome 요약 ===");
            sb.AppendLine($"ID: {genome.GenomeId}");
            sb.AppendLine($"세대: {genome.Generation}");
            sb.AppendLine($"생성 시각: {genome.CreatedAt:yyyy-MM-dd HH:mm:ss}");

            if (!string.IsNullOrEmpty(genome.ParentId1))
            {
                sb.AppendLine($"부모: {genome.ParentId1} x {genome.ParentId2}");
            }

            sb.AppendLine();
            sb.AppendLine("### 서브시스템 가중치");
            sb.AppendLine($"  식량:   {genome.FoodPriorityWeight:F2}");
            sb.AppendLine($"  전투:   {genome.CombatPriorityWeight:F2}");
            sb.AppendLine($"  건설:   {genome.ConstructionPriorityWeight:F2}");
            sb.AppendLine($"  생산:   {genome.ProductionPriorityWeight:F2}");
            sb.AppendLine($"  연구:   {genome.ResearchPriorityWeight:F2}");

            sb.AppendLine();
            sb.AppendLine("### 식량 관리");
            sb.AppendLine($"  위기 임계값:   {genome.FoodCrisisThreshold:F1}일");
            sb.AppendLine($"  경고 임계값:   {genome.FoodWarningThreshold:F1}일");
            sb.AppendLine($"  안정 임계값:   {genome.FoodStableThreshold:F1}일");
            sb.AppendLine($"  겨울 준비:     {genome.WinterPrepDays}일 전");

            sb.AppendLine();
            sb.AppendLine("### 건설 관리");
            sb.AppendLine($"  침대 여유:     {genome.BedBuffer}개");
            sb.AppendLine($"  방어 비율:     {genome.DefensePerColonist:F1}개/인");
            sb.AppendLine($"  쿨다운:        {genome.ConstructionCooldown}틱 ({genome.ConstructionCooldown / 60}초)");

            sb.AppendLine();
            sb.AppendLine("### 생산 관리");
            sb.AppendLine($"  강철 부족:     {genome.SteelShortageThreshold}");
            sb.AppendLine($"  강철 여유:     {genome.SteelSurplusThreshold}");
            sb.AppendLine($"  부품 부족:     {genome.ComponentShortageThreshold}");
            sb.AppendLine($"  부품 여유:     {genome.ComponentSurplusThreshold}");
            sb.AppendLine($"  의약품 목표:   {genome.MedicinePerColonist}개/인");
            sb.AppendLine($"  쿨다운:        {genome.ProductionCooldown}틱 ({genome.ProductionCooldown / 60}초)");

            sb.AppendLine();
            sb.AppendLine("### 전투 관리");
            sb.AppendLine($"  위협 인식:     {genome.ThreatDetectionRange:F1} 타일");
            sb.AppendLine($"  전투 시작:     적 {genome.CombatStartThreshold}명 이상");
            sb.AppendLine($"  전투 종료:     {genome.CombatEndDelay}틱 ({genome.CombatEndDelay / 60}초)");

            sb.AppendLine();
            sb.AppendLine("### 연구 우선순위");
            sb.AppendLine($"  전투 기술:     {genome.ResearchCombatPriority:F2}");
            sb.AppendLine($"  경제 기술:     {genome.ResearchEconomyPriority:F2}");
            sb.AppendLine($"  의료 기술:     {genome.ResearchMedicalPriority:F2}");

            sb.AppendLine();
            sb.AppendLine("### 플레이 스타일 편향");
            sb.AppendLine($"  방어:          {genome.DefensiveBias:F2}");
            sb.AppendLine($"  확장:          {genome.ExpansionBias:F2}");
            sb.AppendLine($"  연구:          {genome.ResearchBias:F2}");
            sb.AppendLine($"  복지:          {genome.WelfareBias:F2}");

            sb.AppendLine();
            sb.AppendLine($"업데이트 속도 배율: {genome.UpdateSpeedMultiplier:F2}");

            sb.AppendLine("=============================");

            Log.Message(sb.ToString());
        }

        /// <summary>
        /// 현재 Metrics 요약 출력
        /// </summary>
        public static void LogCurrentMetrics()
        {
            var collector = MetricsCollector.Instance;
            if (collector == null)
            {
                Log.Warning("[RimAI-GA] MetricsCollector 인스턴스 없음");
                return;
            }

            var metrics = collector.GetCurrentMetrics();
            if (metrics == null)
            {
                Log.Warning("[RimAI-GA] Metrics 없음");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== RimAI Metrics 요약 ===");
            sb.AppendLine($"Session ID: {metrics.SessionId}");
            sb.AppendLine($"Genome ID:  {metrics.GenomeId}");
            sb.AppendLine($"Seed:       {metrics.Seed}");
            sb.AppendLine($"시작 시각:  {metrics.StartTime:yyyy-MM-dd HH:mm:ss}");

            sb.AppendLine();
            sb.AppendLine("### 생존 정보");
            sb.AppendLine($"  생존 일수:     {metrics.TotalDaysSurvived}일");
            sb.AppendLine($"  엔딩 도달:     {(metrics.Ended ? "✓" : "✗")} ({metrics.EndReason})");
            if (metrics.Ended)
            {
                sb.AppendLine($"  엔딩 일수:     {metrics.EndDay}일");
            }

            sb.AppendLine();
            sb.AppendLine("### 콜로니스트");
            sb.AppendLine($"  현재 인원:     {metrics.FinalColonistCount}명");
            sb.AppendLine($"  최대 인원:     {metrics.MaxColonistCount}명");
            sb.AppendLine($"  사망:          {metrics.ColonistDeaths}명");
            sb.AppendLine($"  동물 사망:     {metrics.AnimalDeaths}마리");

            sb.AppendLine();
            sb.AppendLine("### 무드");
            sb.AppendLine($"  평균 무드:     {metrics.AverageMood:F1}%");
            sb.AppendLine($"  최저 무드:     {metrics.LowestMood:F1}%");
            sb.AppendLine($"  정신 붕괴:     {metrics.MentalBreakCount}회");

            sb.AppendLine();
            sb.AppendLine("### 위기");
            sb.AppendLine($"  식량 위기:     {metrics.FoodCrisesCount}회");
            sb.AppendLine($"  심각한 사건:   {metrics.SevereIncidentsCount}회");
            sb.AppendLine($"  전투:          {metrics.CombatCount}회");
            sb.AppendLine($"  전투 승리:     {metrics.CombatVictories}회");

            if (metrics.CombatCount > 0)
            {
                float winRate = (float)metrics.CombatVictories / metrics.CombatCount * 100f;
                sb.AppendLine($"  승률:          {winRate:F1}%");
            }

            sb.AppendLine();
            sb.AppendLine("### 경제/발전");
            sb.AppendLine($"  총 부:         {metrics.FinalWealth:F0}");
            sb.AppendLine($"  건물 부:       {metrics.FinalBuildingsWealth:F0}");
            sb.AppendLine($"  아이템 부:     {metrics.FinalItemsWealth:F0}");
            sb.AppendLine($"  연구 완료:     {metrics.ResearchScore}개");
            sb.AppendLine($"  기술 레벨:     {metrics.TechLevel:F2}");
            sb.AppendLine($"  건설:          {metrics.BuildingsConstructed}개");

            sb.AppendLine();
            sb.AppendLine($"### Fitness 점수: {metrics.CalculateFitness():F0}");

            sb.AppendLine("=============================");

            Log.Message(sb.ToString());
        }

        /// <summary>
        /// Genome과 Metrics 모두 출력
        /// </summary>
        public static void LogAll()
        {
            LogCurrentGenome();
            Log.Message(""); // 빈 줄
            LogCurrentMetrics();
        }

        /// <summary>
        /// 랜덤 Genome 생성 및 저장 (테스트용)
        /// </summary>
        public static void GenerateAndSaveRandomGenome(int generation = 0)
        {
            var genome = RimAIGenome.CreateRandom(generation);

            var manager = RimAIManager.Instance;
            if (manager != null)
            {
                manager.SetGenome(genome);
                manager.SaveCurrentGenome();
                Log.Message($"[RimAI-GA] 랜덤 Genome 생성 및 저장 완료: {genome.GenomeId}");
            }
            else
            {
                Log.Warning("[RimAI-GA] RimAIManager 인스턴스 없음");
            }
        }

        /// <summary>
        /// 기본 Genome으로 재설정
        /// </summary>
        public static void ResetToDefaultGenome()
        {
            var genome = RimAIGenome.CreateDefault();

            var manager = RimAIManager.Instance;
            if (manager != null)
            {
                manager.SetGenome(genome);
                manager.SaveCurrentGenome();
                Log.Message($"[RimAI-GA] 기본 Genome으로 재설정 완료");
            }
            else
            {
                Log.Warning("[RimAI-GA] RimAIManager 인스턴스 없음");
            }
        }
    }
}
