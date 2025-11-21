using System.Collections.Generic;
using System.Linq;
using RimAI.Core;
using RimWorld;
using Verse;

namespace RimAI.Research
{
    /// <summary>
    /// 연구 자동화 서브시스템
    /// 콜로니 상태에 따라 최적의 연구를 자동 선택합니다.
    /// </summary>
    public class ResearchSubsystem : RimAISubsystemBase
    {
        // 연구 변경 쿨다운 (맵별)
        private Dictionary<Map, int> lastResearchChangeTick = new Dictionary<Map, int>();
        private const int RESEARCH_CHANGE_COOLDOWN = 30000; // 500초 (약 8분) - 연구는 드물게 변경

        public override string Name => "Research";
        public override int Priority => 30; // 낮은 우선순위 (생존/건설 이후)

        public ResearchSubsystem()
        {
            baseUpdateInterval = 1200; // 20초마다 업데이트
        }

        public override void Initialize()
        {
            base.Initialize();
            // 설정은 베이스 클래스에서 자동 처리됨
        }

        public override void Update(Map map)
        {
            if (!ShouldUpdate()) return;
            if (!Enabled) return;

            // 연구는 맵별이 아니라 전역이므로 첫 번째 맵에서만 처리
            if (map != Find.Maps.FirstOrDefault()) return;

            try
            {
                // 현재 연구 중인 프로젝트가 있으면 변경하지 않음
                if (Find.ResearchManager.GetProject() != null)
                {
                    LogDetailed($"현재 연구 중: {Find.ResearchManager.GetProject().label}");
                    return;
                }

                // 쿨다운 체크
                if (!CanChangeResearch(map))
                {
                    return;
                }

                // 디버그 로그
                LogDetailed("연구 프로젝트 없음, 자동 선택 시도");
            }
            catch (System.Exception ex)
            {
                LogError($"업데이트 중 오류: {ex}");
            }
        }

        public override List<RimAIAction> GetProposedActions(Map map)
        {
            var actions = new List<RimAIAction>();

            // 연구는 전역이므로 첫 번째 맵에서만 처리
            if (map != Find.Maps.FirstOrDefault()) return actions;

            // 현재 연구 중이면 액션 제안하지 않음
            if (Find.ResearchManager.GetProject() != null) return actions;

            // 쿨다운 체크
            if (!CanChangeResearch(map)) return actions;

            // 최적의 연구 선택
            var bestResearch = SelectBestResearch(map);

            if (bestResearch != null)
            {
                actions.Add(new RimAIAction
                {
                    Type = RimAIActionType.SelectResearch,
                    Priority = GetResearchPriority(bestResearch),
                    SourceSubsystem = Name,
                    TargetMap = map,
                    TargetResearch = bestResearch,
                    Description = $"연구 선택: {bestResearch.label}"
                });
            }

            return actions;
        }

        public override void ExecuteAction(RimAIAction action)
        {
            if (action.Type != RimAIActionType.SelectResearch) return;
            if (action.TargetResearch == null) return;

            try
            {
                // 연구 프로젝트 시작
                Find.ResearchManager.SetCurrentProject(action.TargetResearch);

                LogInfo($"연구 시작: {action.TargetResearch.label}");

                // 쿨다운 설정
                if (action.TargetMap != null)
                {
                    lastResearchChangeTick[action.TargetMap] = Find.TickManager.TicksGame;
                }
            }
            catch (System.Exception ex)
            {
                LogError($"연구 선택 중 오류: {ex}");
            }
        }

        /// <summary>
        /// 최적의 연구 프로젝트 선택
        /// </summary>
        private ResearchProjectDef? SelectBestResearch(Map map)
        {
            // 사용 가능한 연구 프로젝트 목록
            var availableProjects = DefDatabase<ResearchProjectDef>.AllDefs
                .Where(p => p.CanStartNow && !p.IsFinished)
                .ToList();

            if (!availableProjects.Any()) return null;

            // 우선순위 점수 계산
            var scoredProjects = availableProjects
                .Select(p => new { Project = p, Score = CalculateResearchScore(p, map) })
                .OrderByDescending(x => x.Score)
                .ToList();

            LogDetailed("상위 3개 연구:");
            foreach (var item in scoredProjects.Take(3))
            {
                LogDetailed($"  - {item.Project.label}: {item.Score:F1}점");
            }

            return scoredProjects.FirstOrDefault()?.Project;
        }

        /// <summary>
        /// 연구 우선순위 점수 계산 (휴리스틱)
        /// </summary>
        private float CalculateResearchScore(ResearchProjectDef research, Map map)
        {
            float score = 0f;

            // 1. 기본 점수 (낮은 비용 = 높은 점수)
            score += 1000f / (research.baseCost + 100f);

            // 2. 카테고리별 점수
            if (research.techLevel <= Faction.OfPlayer.def.techLevel)
            {
                score += 50f; // 현재 기술 레벨에 맞는 연구 우선
            }

            // 3. 식량 관련 연구 (생존 우선)
            if (research.defName.Contains("Food") ||
                research.defName.Contains("Farm") ||
                research.defName.Contains("Hydroponic"))
            {
                score += 100f;
            }

            // 4. 의료 연구
            if (research.defName.Contains("Medicine") ||
                research.defName.Contains("Hospital"))
            {
                score += 80f;
            }

            // 5. 방어 연구
            if (research.defName.Contains("Turret") ||
                research.defName.Contains("Defense") ||
                research.defName.Contains("Armor"))
            {
                score += 60f;
            }

            // 6. 기초 연구 우선 (선행 연구 적음)
            var prerequisites = research.prerequisites?.Count ?? 0;
            if (prerequisites <= 1)
            {
                score += 40f;
            }

            // 7. 전기 관련 연구 (초반 필수)
            if (research.defName.Contains("Electric") ||
                research.defName.Contains("Battery") ||
                research.defName.Contains("Solar"))
            {
                score += 90f;
            }

            return score;
        }

        /// <summary>
        /// 연구의 액션 우선순위 결정
        /// </summary>
        private RimAIActionPriority GetResearchPriority(ResearchProjectDef research)
        {
            // 식량/의료 관련은 높은 우선순위
            if (research.defName.Contains("Food") ||
                research.defName.Contains("Medicine") ||
                research.defName.Contains("Hydroponic"))
            {
                return RimAIActionPriority.High;
            }

            // 나머지는 일반 우선순위
            return RimAIActionPriority.Normal;
        }

        /// <summary>
        /// 연구 변경 가능 여부 확인 (쿨다운)
        /// </summary>
        private bool CanChangeResearch(Map map)
        {
            if (!lastResearchChangeTick.TryGetValue(map, out var lastTick))
            {
                return true;
            }

            int currentTick = Find.TickManager.TicksGame;
            return (currentTick - lastTick) >= RESEARCH_CHANGE_COOLDOWN;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref lastResearchChangeTick, "lastResearchChangeTick");
        }

        public override string GetDebugInfo(Map map)
        {
            var info = $"[{Name}] Enabled: {Enabled}\n";

            if (Find.ResearchManager.GetProject() != null)
            {
                var progress = Find.ResearchManager.GetProject().ProgressPercent;
                info += $"현재 연구: {Find.ResearchManager.GetProject().label} ({progress:P0})\n";
            }
            else
            {
                info += "현재 연구: 없음\n";
            }

            if (lastResearchChangeTick.TryGetValue(map, out var lastTick))
            {
                int ticksSinceChange = Find.TickManager.TicksGame - lastTick;
                int secondsRemaining = (RESEARCH_CHANGE_COOLDOWN - ticksSinceChange) / 60;
                info += $"쿨다운: {secondsRemaining}초 남음\n";
            }

            return info;
        }
    }
}
