using System.Collections.Generic;
using System.Linq;
using RimAI.Core;
using RimAI.Settings;
using RimWorld;
using Verse;

namespace RimAI.Work
{
    /// <summary>
    /// 작업 우선순위 자동 관리 서브시스템
    /// 모든 폰이 적절한 작업을 수행하도록 우선순위를 자동으로 설정합니다.
    /// </summary>
    public class WorkPrioritySubsystem : RimAISubsystemBase
    {
        private Dictionary<Map, int> lastWorkUpdateTick = new Dictionary<Map, int>();
        private Dictionary<Map, bool> initialSetupDone = new Dictionary<Map, bool>();
        private const int WORK_UPDATE_COOLDOWN = 600; // 10초마다 체크

        public override string Name => "WorkPriority";
        public override int Priority => 95; // 매우 높은 우선순위

        public WorkPrioritySubsystem()
        {
            baseUpdateInterval = 60; // 1초마다 체크
        }

        public override void Update(Map map)
        {
            if (!ShouldUpdate()) return;
            if (!Enabled) return;

            try
            {
                // 초기 설정
                if (!initialSetupDone.ContainsKey(map) || !initialSetupDone[map])
                {
                    DoInitialWorkSetup(map);
                    initialSetupDone[map] = true;
                }

                // 주기적 업데이트
                if (CanUpdateWork(map))
                {
                    UpdateAllPawnWorkPriorities(map);
                    lastWorkUpdateTick[map] = Find.TickManager.TicksGame;
                }

                // 긴급 작업 확인 - 항상
                CheckUrgentWork(map);
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RimAI-WorkPriority] 업데이트 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 게임 시작 시 초기 작업 설정
        /// </summary>
        private void DoInitialWorkSetup(Map map)
        {
            Log.Message("[RimAI-WorkPriority] === 초기 작업 설정 시작 ===");

            var colonists = map.mapPawns.FreeColonistsSpawned.ToList();
            foreach (var pawn in colonists)
            {
                SetupPawnWorkPriorities(pawn);
            }

            Log.Message($"[RimAI-WorkPriority] {colonists.Count}명의 작업 우선순위 설정 완료");
        }

        /// <summary>
        /// 개별 폰의 작업 우선순위 설정
        /// </summary>
        private void SetupPawnWorkPriorities(Pawn pawn)
        {
            if (pawn.workSettings == null) return;
            if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Firefighter)) return; // 작업 불가 폰

            // 모든 작업 활성화 (우선순위 3으로)
            foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (pawn.WorkTypeIsDisabled(workType)) continue;

                int currentPriority = pawn.workSettings.GetPriority(workType);
                if (currentPriority == 0) // 비활성화된 작업만
                {
                    pawn.workSettings.SetPriority(workType, 3);
                }
            }

            // 스킬 기반 최적화
            OptimizePriorityBySkills(pawn);
        }

        /// <summary>
        /// 스킬 기반 우선순위 최적화
        /// </summary>
        private void OptimizePriorityBySkills(Pawn pawn)
        {
            if (pawn.skills == null) return;

            // 각 스킬별 최고 스킬을 가진 작업 우선
            var skillWorkMap = new Dictionary<SkillDef, WorkTypeDef[]>
            {
                { SkillDefOf.Construction, new[] { WorkTypeDefOf.Construction } },
                { SkillDefOf.Mining, new[] { WorkTypeDefOf.Mining } },
                { SkillDefOf.Plants, new[] { WorkTypeDefOf.Growing } },
                { SkillDefOf.Shooting, new[] { WorkTypeDefOf.Hunting } },
                { SkillDefOf.Medicine, new[] { WorkTypeDefOf.Doctor } },
                { SkillDefOf.Cooking, GetWorkTypeByName("Cooking") },
                { SkillDefOf.Crafting, GetWorkTypeByName("Crafting", "Smithing", "Tailoring") },
                { SkillDefOf.Artistic, GetWorkTypeByName("Art") },
                { SkillDefOf.Intellectual, new[] { WorkTypeDefOf.Research } }
            };

            foreach (var kvp in skillWorkMap)
            {
                var skill = pawn.skills.GetSkill(kvp.Key);
                if (skill == null || skill.TotallyDisabled) continue;

                int skillLevel = skill.Level;
                int priority = skillLevel >= 10 ? 1 : skillLevel >= 6 ? 2 : 3;

                foreach (var workType in kvp.Value)
                {
                    if (workType != null && !pawn.WorkTypeIsDisabled(workType))
                    {
                        pawn.workSettings.SetPriority(workType, priority);
                    }
                }
            }

            // 필수 작업은 모두 우선순위 설정
            SetEssentialWorkPriorities(pawn);
        }

        /// <summary>
        /// 필수 작업 우선순위 설정
        /// </summary>
        private void SetEssentialWorkPriorities(Pawn pawn)
        {
            // 소방은 모두 최우선
            if (!pawn.WorkTypeIsDisabled(WorkTypeDefOf.Firefighter))
                pawn.workSettings.SetPriority(WorkTypeDefOf.Firefighter, 1);

            // 환자 돌봄도 중요
            if (!pawn.WorkTypeIsDisabled(WorkTypeDefOf.Doctor))
            {
                var medSkill = pawn.skills?.GetSkill(SkillDefOf.Medicine);
                if (medSkill != null && medSkill.Level >= 4)
                    pawn.workSettings.SetPriority(WorkTypeDefOf.Doctor, 1);
            }

            // 운반은 모두 활성화
            if (!pawn.WorkTypeIsDisabled(WorkTypeDefOf.Hauling))
                pawn.workSettings.SetPriority(WorkTypeDefOf.Hauling, 3);

            // 청소도 모두 활성화
            var cleaningWork = GetWorkTypeByName("Cleaning").FirstOrDefault();
            if (cleaningWork != null && !pawn.WorkTypeIsDisabled(cleaningWork))
                pawn.workSettings.SetPriority(cleaningWork, 4);
        }

        /// <summary>
        /// 이름으로 WorkTypeDef 찾기
        /// </summary>
        private WorkTypeDef[] GetWorkTypeByName(params string[] names)
        {
            var result = new List<WorkTypeDef>();
            foreach (var name in names)
            {
                var workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(name);
                if (workType != null) result.Add(workType);
            }
            return result.ToArray();
        }

        /// <summary>
        /// 모든 폰의 작업 우선순위 업데이트
        /// </summary>
        private void UpdateAllPawnWorkPriorities(Map map)
        {
            var colonists = map.mapPawns.FreeColonistsSpawned.ToList();
            foreach (var pawn in colonists)
            {
                // 새로 추가된 폰 설정
                if (pawn.workSettings != null)
                {
                    bool hasAnyWork = false;
                    foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                    {
                        if (!pawn.WorkTypeIsDisabled(workType) &&
                            pawn.workSettings.GetPriority(workType) > 0)
                        {
                            hasAnyWork = true;
                            break;
                        }
                    }

                    if (!hasAnyWork)
                    {
                        SetupPawnWorkPriorities(pawn);
                        Log.Message($"[RimAI-WorkPriority] {pawn.LabelShort}의 작업 설정 완료");
                    }
                }
            }
        }

        /// <summary>
        /// 긴급 작업 확인 및 할당
        /// </summary>
        private void CheckUrgentWork(Map map)
        {
            // 부상자 치료 필요
            CheckMedicalEmergency(map);

            // 화재 진압 필요
            CheckFireEmergency(map);

            // 굶주림 방지
            CheckStarvationEmergency(map);
        }

        /// <summary>
        /// 의료 응급 상황 확인
        /// </summary>
        private void CheckMedicalEmergency(Map map)
        {
            var injuredPawns = map.mapPawns.FreeColonistsSpawned
                .Where(p => p.health.HasHediffsNeedingTend())
                .ToList();

            if (injuredPawns.Count > 0)
            {
                // 의사 스킬이 있는 폰의 Doctor 우선순위 높이기
                var doctors = map.mapPawns.FreeColonistsSpawned
                    .Where(p => !p.Downed &&
                               !p.WorkTypeIsDisabled(WorkTypeDefOf.Doctor) &&
                               p.skills?.GetSkill(SkillDefOf.Medicine)?.Level >= 3)
                    .ToList();

                foreach (var doctor in doctors)
                {
                    doctor.workSettings?.SetPriority(WorkTypeDefOf.Doctor, 1);
                }
            }
        }

        /// <summary>
        /// 화재 응급 상황 확인
        /// </summary>
        private void CheckFireEmergency(Map map)
        {
            var fires = map.listerThings.ThingsOfDef(ThingDefOf.Fire);
            if (fires.Count > 0)
            {
                // 모든 폰의 소방 우선순위 최고로
                foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
                {
                    if (!pawn.Downed && !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Firefighter))
                    {
                        pawn.workSettings?.SetPriority(WorkTypeDefOf.Firefighter, 1);
                    }
                }
            }
        }

        /// <summary>
        /// 굶주림 응급 상황 확인
        /// </summary>
        private void CheckStarvationEmergency(Map map)
        {
            var starvingPawns = map.mapPawns.FreeColonistsSpawned
                .Where(p => p.needs?.food?.CurLevelPercentage < 0.2f)
                .ToList();

            if (starvingPawns.Count > 0)
            {
                // 요리사 우선순위 높이기
                var cookingWork = GetWorkTypeByName("Cooking").FirstOrDefault();
                if (cookingWork != null)
                {
                    var cooks = map.mapPawns.FreeColonistsSpawned
                        .Where(p => !p.Downed &&
                                   !p.WorkTypeIsDisabled(cookingWork) &&
                                   p.skills?.GetSkill(SkillDefOf.Cooking)?.Level >= 2)
                        .ToList();

                    foreach (var cook in cooks)
                    {
                        cook.workSettings?.SetPriority(cookingWork, 1);
                    }
                }

                // 사냥꾼 우선순위 높이기
                var hunters = map.mapPawns.FreeColonistsSpawned
                    .Where(p => !p.Downed &&
                               !p.WorkTypeIsDisabled(WorkTypeDefOf.Hunting))
                    .ToList();

                foreach (var hunter in hunters)
                {
                    hunter.workSettings?.SetPriority(WorkTypeDefOf.Hunting, 1);
                }
            }
        }

        private bool CanUpdateWork(Map map)
        {
            if (!lastWorkUpdateTick.TryGetValue(map, out var lastTick))
                return true;

            return (Find.TickManager.TicksGame - lastTick) >= WORK_UPDATE_COOLDOWN;
        }

        public override List<RimAIAction> GetProposedActions(Map map)
        {
            return new List<RimAIAction>();
        }

        public override void ExecuteAction(RimAIAction action)
        {
        }

        public override void CleanupMap(Map map)
        {
            lastWorkUpdateTick.Remove(map);
            initialSetupDone.Remove(map);
        }
    }
}
