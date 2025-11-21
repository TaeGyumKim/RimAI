using System;
using System.IO;
using System.Linq;
using RimWorld;
using Verse;

namespace RimAI.GA
{
    /// <summary>
    /// 게임 중 metrics를 수집하고 엔딩 시 저장하는 클래스
    /// GameComponent로 등록되어 매 틱마다 통계 갱신
    /// </summary>
    public class MetricsCollector : GameComponent
    {
        private RimAIRunMetrics currentMetrics = new RimAIRunMetrics();
        private bool metricsInitialized = false;
        private bool metricsAlreadySaved = false; // 중복 저장 방지
        private int tickCounter = 0;
        private const int UPDATE_INTERVAL = 2500; // 약 1분마다 갱신

        // 타임아웃 설정 (기본: 5년 = 1825일)
        private const int TIMEOUT_DAYS = 1825;
        private int lastTimeoutCheckDay = 0;

        // 누적 통계
        private float totalMoodSum = 0f;
        private int moodSampleCount = 0;

        public MetricsCollector(Game game)
        {
        }

        /// <summary>
        /// 게임 시작 시 초기화
        /// </summary>
        public override void StartedNewGame()
        {
            base.StartedNewGame();
            InitializeMetrics();
        }

        /// <summary>
        /// 세이브 로드 시 초기화
        /// </summary>
        public override void LoadedGame()
        {
            base.LoadedGame();
            if (!metricsInitialized)
            {
                InitializeMetrics();
            }
        }

        /// <summary>
        /// Metrics 초기화
        /// </summary>
        private void InitializeMetrics()
        {
            currentMetrics = new RimAIRunMetrics();

            // Session ID 생성 (타임스탬프 기반)
            currentMetrics.SessionId = $"rimai_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            currentMetrics.StartTime = DateTime.UtcNow;

            // Seed 가져오기
            if (Find.World != null)
            {
                currentMetrics.Seed = Find.World.info.seedString;
            }

            // Genome ID 가져오기
            var manager = Core.RimAIManager.Instance;
            if (manager != null)
            {
                // TODO: Genome 연동 후 실제 ID 가져오기
                currentMetrics.GenomeId = "default";
            }

            metricsInitialized = true;

            Log.Message($"[RimAI-GA] Metrics 수집 시작: {currentMetrics.SessionId}");
        }

        /// <summary>
        /// 매 틱마다 호출
        /// </summary>
        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (!metricsInitialized)
                return;

            tickCounter++;

            if (tickCounter >= UPDATE_INTERVAL)
            {
                tickCounter = 0;
                UpdateMetrics();
                CheckTimeout(); // 타임아웃 체크
            }
        }

        /// <summary>
        /// Metrics 갱신
        /// </summary>
        private void UpdateMetrics()
        {
            if (Current.Game == null)
                return;

            try
            {
                // 플레이어 홈 맵 찾기
                Map homeMap = Find.Maps.FirstOrDefault(m => m.IsPlayerHome);
                if (homeMap == null)
                    return;

                // 생존 일수
                currentMetrics.TotalDaysSurvived = GenDate.DaysPassed;

                // 콜로니스트 통계
                var colonists = homeMap.mapPawns.FreeColonists.ToList();
                currentMetrics.FinalColonistCount = colonists.Count;
                if (colonists.Count > currentMetrics.MaxColonistCount)
                {
                    currentMetrics.MaxColonistCount = colonists.Count;
                }

                // 무드 통계
                if (colonists.Count > 0)
                {
                    float currentMood = colonists.Average(c => c.needs?.mood?.CurLevelPercentage ?? 0.5f) * 100f;
                    totalMoodSum += currentMood;
                    moodSampleCount++;
                    currentMetrics.AverageMood = totalMoodSum / moodSampleCount;

                    if (currentMood < currentMetrics.LowestMood)
                    {
                        currentMetrics.LowestMood = currentMood;
                    }
                }

                // 부(wealth) 통계
                if (homeMap.wealthWatcher != null)
                {
                    currentMetrics.FinalWealth = homeMap.wealthWatcher.WealthTotal;
                    currentMetrics.FinalBuildingsWealth = homeMap.wealthWatcher.WealthBuildings;
                    currentMetrics.FinalItemsWealth = homeMap.wealthWatcher.WealthItems;
                }

                // 연구 통계
                if (Find.ResearchManager != null)
                {
                    currentMetrics.ResearchScore = DefDatabase<ResearchProjectDef>.AllDefs
                        .Count(r => r.IsFinished);

                    int totalProjects = DefDatabase<ResearchProjectDef>.AllDefs.Count();
                    if (totalProjects > 0)
                    {
                        currentMetrics.TechLevel = (float)currentMetrics.ResearchScore / totalProjects;
                    }
                }

                // 건설 통계 (대략적)
                currentMetrics.BuildingsConstructed = homeMap.listerBuildings.allBuildingsColonist.Count;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI-GA] Metrics 갱신 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 콜로니스트 사망 기록
        /// </summary>
        public void OnColonistDied()
        {
            currentMetrics.ColonistDeaths++;
        }

        /// <summary>
        /// 동물 사망 기록
        /// </summary>
        public void OnAnimalDied()
        {
            currentMetrics.AnimalDeaths++;
        }

        /// <summary>
        /// 정신 붕괴 기록
        /// </summary>
        public void OnMentalBreak()
        {
            currentMetrics.MentalBreakCount++;
        }

        /// <summary>
        /// 식량 위기 기록
        /// </summary>
        public void OnFoodCrisis()
        {
            currentMetrics.FoodCrisesCount++;
        }

        /// <summary>
        /// 심각한 사건 기록
        /// </summary>
        public void OnSevereIncident()
        {
            currentMetrics.SevereIncidentsCount++;
        }

        /// <summary>
        /// 전투 시작 기록
        /// </summary>
        public void OnCombatStart()
        {
            currentMetrics.CombatCount++;
        }

        /// <summary>
        /// 전투 승리 기록
        /// </summary>
        public void OnCombatVictory()
        {
            currentMetrics.CombatVictories++;
        }

        /// <summary>
        /// 게임 종료 시 호출 (엔딩 or 전멸)
        /// </summary>
        public void OnGameEnded(EndReason endReason)
        {
            if (!metricsInitialized)
                return;

            if (metricsAlreadySaved)
            {
                Log.Message($"[RimAI-GA] Metrics 이미 저장됨, 중복 저장 방지");
                return;
            }

            try
            {
                // 최종 갱신
                UpdateMetrics();

                // 엔딩 정보
                currentMetrics.EndReasonEnum = endReason;
                currentMetrics.EndDay = GenDate.DaysPassed;
                currentMetrics.EndTime = DateTime.UtcNow;

                // 저장
                SaveMetrics();

                metricsAlreadySaved = true;

                // 요약 로그
                Log.Message($"[RimAI-GA] 게임 종료: {currentMetrics.EndReasonEnum.ToKoreanString()} (Fitness: {currentMetrics.CalculateFitness():F0})");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI-GA] 게임 종료 처리 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Metrics를 JSON 파일로 저장
        /// </summary>
        private void SaveMetrics()
        {
            try
            {
                // 저장 디렉토리 생성
                string baseDir = Path.Combine(GenFilePaths.ConfigFolderPath, "RimAI", "GA", "runs");
                Directory.CreateDirectory(baseDir);

                // 파일 경로
                string fileName = $"{currentMetrics.SessionId}.json";
                string filePath = Path.Combine(baseDir, fileName);

                // 저장
                currentMetrics.SaveToFile(filePath);

                // Fitness 점수 로그
                float fitness = currentMetrics.CalculateFitness();
                Log.Message($"[RimAI-GA] Fitness 점수: {fitness:F0}");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI-GA] Metrics 저장 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 타임아웃 체크 (설정된 일수 초과 시 자동 종료)
        /// </summary>
        private void CheckTimeout()
        {
            if (metricsAlreadySaved)
                return;

            int currentDay = GenDate.DaysPassed;

            // 하루에 한 번만 체크
            if (currentDay <= lastTimeoutCheckDay)
                return;

            lastTimeoutCheckDay = currentDay;

            // 타임아웃 체크
            if (currentDay >= TIMEOUT_DAYS)
            {
                Log.Warning($"[RimAI-GA] 타임아웃 도달: {currentDay}일 (제한: {TIMEOUT_DAYS}일)");
                OnGameEnded(EndReason.Timeout);
            }
            else if (currentDay >= TIMEOUT_DAYS - 30)
            {
                // 30일 전부터 경고
                Log.Message($"[RimAI-GA] 타임아웃 임박: {currentDay}/{TIMEOUT_DAYS}일");
            }
        }

        /// <summary>
        /// 현재 metrics 가져오기 (디버그용)
        /// </summary>
        public RimAIRunMetrics GetCurrentMetrics()
        {
            UpdateMetrics(); // 최신 상태로 갱신
            return currentMetrics;
        }

        /// <summary>
        /// 세이브 데이터 저장/로드
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();

            // Metrics는 세이브에 저장하지 않음 (매번 새로 시작)
            // 단, 로드 시 초기화 플래그는 저장
            Scribe_Values.Look(ref metricsInitialized, "rimai_metrics_initialized", false);
        }

        /// <summary>
        /// 싱글톤 인스턴스 접근
        /// </summary>
        public static MetricsCollector? Instance
        {
            get
            {
                if (Current.Game == null)
                    return null;
                return Current.Game.GetComponent<MetricsCollector>();
            }
        }
    }
}
