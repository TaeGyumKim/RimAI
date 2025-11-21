using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimAI.Combat
{
    /// <summary>
    /// 콜로니의 위협 상태를 분석
    /// </summary>
    public static class ThreatAnalyzer
    {
        /// <summary>
        /// 맵의 위협 상태 분석
        /// </summary>
        public static ColonyThreatState AnalyzeMap(Map map)
        {
            var state = new ColonyThreatState();

            // 1. 적 분석
            AnalyzeEnemies(map, state);

            // 2. 아군 분석
            AnalyzeAllies(map, state);

            // 3. 위협 레벨 결정
            DetermineThreatLevel(state);

            // 4. 전투 태세 결정
            DetermineCombatPosture(state);

            return state;
        }

        /// <summary>
        /// 적 분석
        /// </summary>
        private static void AnalyzeEnemies(Map map, ColonyThreatState state)
        {
            state.EnemyPawns.Clear();
            state.TotalEnemies = 0;
            state.EnemyCombatPower = 0f;

            // 맵의 모든 폰 검사
            var allPawns = map.mapPawns.AllPawnsSpawned;

            foreach (var pawn in allPawns)
            {
                // 적대적인 폰만 카운트
                if (IsHostileTo(pawn, Faction.OfPlayer))
                {
                    state.EnemyPawns.Add(pawn);
                    state.TotalEnemies++;

                    // 전투력 계산
                    state.EnemyCombatPower += CalculateCombatPower(pawn);
                }
            }

            // 위협 중심 위치 계산
            if (state.EnemyPawns.Any())
            {
                float avgX = state.EnemyPawns.Average(p => p.Position.x);
                float avgZ = state.EnemyPawns.Average(p => p.Position.z);
                state.ThreatCenter = new IntVec3((int)avgX, 0, (int)avgZ);
            }

            // 마지막 적 발견 시각 업데이트
            if (state.TotalEnemies > 0)
            {
                state.LastEnemySeenTick = Find.TickManager.TicksGame;
            }
        }

        /// <summary>
        /// 아군 분석
        /// </summary>
        private static void AnalyzeAllies(Map map, ColonyThreatState state)
        {
            state.CombatCapablePawns.Clear();
            state.AllyCombatPower = 0f;
            state.DraftedPawns = 0;

            var colonists = map.mapPawns.FreeColonistsSpawned;

            foreach (var pawn in colonists)
            {
                // Draft 상태 카운트
                if (pawn.Drafted)
                {
                    state.DraftedPawns++;
                }

                // 전투 가능 여부 확인
                if (IsCombatCapable(pawn))
                {
                    state.CombatCapablePawns.Add(pawn);
                    state.AllyCombatPower += CalculateCombatPower(pawn);
                }
            }
        }

        /// <summary>
        /// 위협 레벨 결정
        /// </summary>
        private static void DetermineThreatLevel(ColonyThreatState state)
        {
            // 적이 없으면 위협 없음
            if (state.TotalEnemies == 0)
            {
                state.CurrentThreatLevel = ThreatLevel.None;
                return;
            }

            // 전투력 비율과 적 수로 위협 레벨 결정
            float powerRatio = state.PowerRatio;

            // 적 전투력이 압도적
            if (powerRatio < 0.5f || state.TotalEnemies > 20)
            {
                state.CurrentThreatLevel = ThreatLevel.Critical;
            }
            // 적이 우세
            else if (powerRatio < 0.8f || state.TotalEnemies > 10)
            {
                state.CurrentThreatLevel = ThreatLevel.High;
            }
            // 비슷하거나 약간 불리
            else if (powerRatio < 1.2f || state.TotalEnemies > 5)
            {
                state.CurrentThreatLevel = ThreatLevel.Medium;
            }
            // 소규모 위협
            else
            {
                state.CurrentThreatLevel = ThreatLevel.Low;
            }
        }

        /// <summary>
        /// 전투 태세 결정
        /// </summary>
        private static void DetermineCombatPosture(ColonyThreatState state)
        {
            switch (state.CurrentThreatLevel)
            {
                case ThreatLevel.None:
                    state.CurrentPosture = CombatPosture.Peaceful;
                    break;

                case ThreatLevel.Low:
                    state.CurrentPosture = CombatPosture.Alert;
                    break;

                case ThreatLevel.Medium:
                case ThreatLevel.High:
                    state.CurrentPosture = CombatPosture.Combat;
                    break;

                case ThreatLevel.Critical:
                    // 압도적으로 불리하면 철수
                    if (state.PowerRatio < 0.3f)
                    {
                        state.CurrentPosture = CombatPosture.Retreat;
                    }
                    else
                    {
                        state.CurrentPosture = CombatPosture.Combat;
                    }
                    break;
            }
        }

        /// <summary>
        /// 폰이 플레이어에게 적대적인지 확인
        /// </summary>
        private static bool IsHostileTo(Pawn pawn, Faction faction)
        {
            if (pawn == null || pawn.Dead || pawn.Downed) return false;

            // 플레이어 폰이 아니고
            if (pawn.Faction == faction) return false;

            // 적대 관계이거나
            if (pawn.Faction != null && pawn.Faction.HostileTo(faction))
                return true;

            // 야생 동물이 플레이어를 공격 중이면
            if (pawn.RaceProps.Animal && pawn.MentalStateDef != null)
            {
                var mentalState = pawn.MentalState;
                if (mentalState != null && mentalState.def.IsAggro)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 폰이 전투 가능한지 확인
        /// </summary>
        private static bool IsCombatCapable(Pawn pawn)
        {
            if (pawn == null || pawn.Dead) return false;
            if (pawn.Downed) return false;
            if (pawn.InMentalState) return false;

            // 건강 상태 확인
            if (pawn.health.summaryHealth.SummaryHealthPercent < 0.3f)
                return false;

            // 무기가 있거나 맨손 전투 가능
            return true;
        }

        /// <summary>
        /// 폰의 전투력 계산 (간단한 휴리스틱)
        /// </summary>
        private static float CalculateCombatPower(Pawn pawn)
        {
            if (pawn == null || pawn.Dead) return 0f;

            float power = 10f; // 기본 전투력

            // 체력
            power *= pawn.health.summaryHealth.SummaryHealthPercent;

            // 무기
            var primaryEquipment = pawn.equipment?.Primary;
            if (primaryEquipment != null)
            {
                var verb = primaryEquipment.def.Verbs?.FirstOrDefault();
                if (verb != null)
                {
                    // 무기 데미지 * 사거리
                    power += verb.defaultProjectile?.projectile?.GetDamageAmount(primaryEquipment) ?? 0f;
                    power *= (verb.range / 10f + 1f); // 사거리 보너스
                }
            }

            // 갑옷 (방어력)
            var apparels = pawn.apparel?.WornApparel;
            if (apparels != null)
            {
                foreach (var apparel in apparels)
                {
                    power += apparel.def.StatBaseDefined(StatDefOf.ArmorRating_Sharp) ? 5f : 0f;
                }
            }

            // 스킬 (Shooting, Melee)
            if (pawn.skills != null)
            {
                var shootingSkill = pawn.skills.GetSkill(SkillDefOf.Shooting)?.Level ?? 0;
                var meleeSkill = pawn.skills.GetSkill(SkillDefOf.Melee)?.Level ?? 0;
                power *= (1f + (shootingSkill + meleeSkill) / 40f);
            }

            // 메카노이드는 기본적으로 강함
            if (pawn.RaceProps.IsMechanoid)
            {
                power *= 2f;
            }

            return power;
        }
    }
}
