using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimAI.Combat
{
    /// <summary>
    /// 콜로니의 위협 상태를 분석
    ///
    /// 성능 최적화:
    /// - 전투력 계산 캐시 (5초 유효)
    /// - 중복 순회 제거
    /// - 조기 종료 추가
    /// </summary>
    public static class ThreatAnalyzer
    {
        // 전투력 캐시 (5초 유효)
        private static Dictionary<Pawn, CachedCombatPower> combatPowerCache = new Dictionary<Pawn, CachedCombatPower>();
        private const int CACHE_VALID_TICKS = 300; // 5초

        // 캐시 정리 주기
        private static int lastCacheCleanupTick = 0;
        private const int CACHE_CLEANUP_INTERVAL = 1800; // 30초마다 정리

        /// <summary>
        /// 전투력 캐시 엔트리
        /// </summary>
        private struct CachedCombatPower
        {
            public float power;
            public int calculatedTick;

            public bool IsValid(int currentTick)
            {
                return (currentTick - calculatedTick) < CACHE_VALID_TICKS;
            }
        }

        /// <summary>
        /// 맵의 위협 상태 분석
        /// </summary>
        public static ColonyThreatState AnalyzeMap(Map map)
        {
            var state = new ColonyThreatState();

            // 조기 종료: 플레이어 홈이 아니면 스킵
            if (!map.IsPlayerHome)
            {
                return state;
            }

            // 1. 적 분석
            AnalyzeEnemies(map, state);

            // 조기 종료: 적이 없으면 아군 분석 스킵
            if (state.TotalEnemies == 0)
            {
                state.CurrentThreatLevel = ThreatLevel.None;
                state.CurrentPosture = CombatPosture.Peaceful;
                return state;
            }

            // 2. 아군 분석
            AnalyzeAllies(map, state);

            // 3. 위협 레벨 결정
            DetermineThreatLevel(state);

            // 4. 전투 태세 결정
            DetermineCombatPosture(state);

            // 5. 캐시 정리 (주기적)
            CleanupCacheIfNeeded();

            return state;
        }

        /// <summary>
        /// 적 분석 (최적화됨)
        /// </summary>
        private static void AnalyzeEnemies(Map map, ColonyThreatState state)
        {
            state.EnemyPawns.Clear();
            state.TotalEnemies = 0;
            state.EnemyCombatPower = 0f;

            // 맵의 모든 폰 검사 (RimWorld 내장 API 활용)
            var allPawns = map.mapPawns.AllPawnsSpawned;

            // 위협 중심 계산용 (한 번의 순회로 처리)
            int sumX = 0, sumZ = 0;
            int enemyCount = 0;

            foreach (var pawn in allPawns)
            {
                // 조기 종료: 죽었거나 쓰러진 폰 스킵
                if (pawn == null || pawn.Dead || pawn.Downed)
                    continue;

                // 적대적인 폰만 카운트
                if (IsHostileTo(pawn, Faction.OfPlayer))
                {
                    state.EnemyPawns.Add(pawn);
                    state.TotalEnemies++;
                    enemyCount++;

                    // 위협 중심 계산용 좌표 합산
                    sumX += pawn.Position.x;
                    sumZ += pawn.Position.z;

                    // 전투력 계산 (캐시 활용)
                    state.EnemyCombatPower += GetCachedCombatPower(pawn);
                }
            }

            // 위협 중심 위치 계산 (한 번의 순회로 완료)
            if (enemyCount > 0)
            {
                state.ThreatCenter = new IntVec3(sumX / enemyCount, 0, sumZ / enemyCount);
            }

            // 마지막 적 발견 시각 업데이트
            if (state.TotalEnemies > 0)
            {
                state.LastEnemySeenTick = Find.TickManager.TicksGame;
            }
        }

        /// <summary>
        /// 아군 분석 (최적화됨)
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

                    // 전투력 계산 (캐시 활용)
                    state.AllyCombatPower += GetCachedCombatPower(pawn);
                }
            }
        }

        /// <summary>
        /// 캐시된 전투력 가져오기 (성능 최적화 핵심)
        /// </summary>
        private static float GetCachedCombatPower(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
                return 0f;

            int currentTick = Find.TickManager.TicksGame;

            // 캐시 확인
            if (combatPowerCache.TryGetValue(pawn, out var cached))
            {
                if (cached.IsValid(currentTick))
                {
                    return cached.power;
                }
            }

            // 캐시 미스 - 새로 계산
            float power = CalculateCombatPower(pawn);

            // 캐시 저장
            combatPowerCache[pawn] = new CachedCombatPower
            {
                power = power,
                calculatedTick = currentTick
            };

            return power;
        }

        /// <summary>
        /// 캐시 정리 (주기적)
        /// </summary>
        private static void CleanupCacheIfNeeded()
        {
            int currentTick = Find.TickManager.TicksGame;

            if (currentTick - lastCacheCleanupTick < CACHE_CLEANUP_INTERVAL)
                return;

            lastCacheCleanupTick = currentTick;

            // 유효하지 않은 캐시 제거 (죽은 폰, 오래된 캐시)
            var toRemove = new List<Pawn>();

            foreach (var kvp in combatPowerCache)
            {
                var pawn = kvp.Key;
                var cached = kvp.Value;

                // 폰이 죽었거나, 캐시가 오래되었으면 제거
                if (pawn == null || pawn.Destroyed || !cached.IsValid(currentTick))
                {
                    toRemove.Add(pawn);
                }
            }

            foreach (var pawn in toRemove)
            {
                combatPowerCache.Remove(pawn);
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
        ///
        /// 주의: 이 메서드는 직접 호출하지 말고 GetCachedCombatPower()를 사용하세요!
        /// </summary>
        private static float CalculateCombatPower(Pawn pawn)
        {
            if (pawn == null || pawn.Dead) return 0f;

            float power = 10f; // 기본 전투력

            // 체력
            power *= pawn.health.summaryHealth.SummaryHealthPercent;

            // 무기
            var primaryEquipment = pawn.equipment?.Primary;
            if (primaryEquipment != null && primaryEquipment.def.Verbs != null && primaryEquipment.def.Verbs.Count > 0)
            {
                // FirstOrDefault() 대신 [0] 접근
                var verb = primaryEquipment.def.Verbs[0];
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

        /// <summary>
        /// 캐시 완전 초기화 (맵 변경 등)
        /// </summary>
        public static void ClearCache()
        {
            combatPowerCache.Clear();
            lastCacheCleanupTick = 0;
        }

        /// <summary>
        /// 특정 맵의 모든 Pawn 캐시 제거 (맵 언로드 시 호출)
        /// 메모리 누수 방지
        /// </summary>
        public static void ClearMapCache(Map map)
        {
            if (map == null) return;

            var toRemove = new List<Pawn>();

            foreach (var kvp in combatPowerCache)
            {
                var pawn = kvp.Key;
                if (pawn != null && pawn.Map == map)
                {
                    toRemove.Add(pawn);
                }
            }

            foreach (var pawn in toRemove)
            {
                combatPowerCache.Remove(pawn);
            }
        }
    }
}
