using System.Collections.Generic;
using System.Linq;
using RimAI.Core;
using RimAI.Settings;
using RimWorld;
using Verse;

namespace RimAI.Equipment
{
    /// <summary>
    /// 장비 자동화 서브시스템
    /// 무기, 방어구를 자동으로 착용시킵니다.
    /// </summary>
    public class EquipmentSubsystem : RimAISubsystemBase
    {
        private Dictionary<Map, int> lastEquipTick = new Dictionary<Map, int>();
        private const int EQUIP_COOLDOWN = 600; // 10초

        public override string Name => "Equipment";
        public override int Priority => 55; // 건설보다 약간 높음

        public EquipmentSubsystem()
        {
            baseUpdateInterval = 180; // 3초마다 체크
        }

        public override void Update(Map map)
        {
            if (!ShouldUpdate()) return;
            if (!Enabled) return;

            try
            {
                if (!CanEquip(map)) return;

                // 모든 콜로니스트 확인
                foreach (var colonist in map.mapPawns.FreeColonistsSpawned)
                {
                    // 무기 장착 확인
                    TryEquipBestWeapon(colonist, map);

                    // 방어구 장착 확인
                    TryEquipArmor(colonist, map);
                }

                lastEquipTick[map] = Find.TickManager.TicksGame;
            }
            catch (System.Exception ex)
            {
                LogError($"장비 업데이트 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 최고 무기 장착 시도
        /// </summary>
        private void TryEquipBestWeapon(Pawn pawn, Map map)
        {
            // 이미 무기가 있으면 스킵 (빈손이 아니면)
            if (pawn.equipment?.Primary != null) return;

            // 전투 가능한 폰인지 확인
            if (pawn.WorkTagIsDisabled(WorkTags.Violent)) return;

            // 사용 가능한 무기 찾기
            var availableWeapons = map.listerThings.ThingsInGroup(ThingRequestGroup.Weapon)
                .Where(w => !w.IsForbidden(pawn) &&
                           pawn.CanReserveAndReach(w, PathEndMode.Touch, Danger.Some))
                .OrderByDescending(w => GetWeaponScore(w, pawn))
                .ToList();

            if (availableWeapons.Count == 0) return;

            var bestWeapon = availableWeapons.First();

            // 장착 작업 생성
            var job = JobMaker.MakeJob(JobDefOf.Equip, bestWeapon);
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);

            LogInfo($"{pawn.LabelShort}에게 {bestWeapon.Label} 장착 지시");
        }

        /// <summary>
        /// 무기 점수 계산
        /// </summary>
        private float GetWeaponScore(Thing weapon, Pawn pawn)
        {
            float score = 0f;

            // 기본 DPS
            if (weapon.def.IsRangedWeapon)
            {
                // 사격 스킬 기반
                float shootingSkill = pawn.skills?.GetSkill(SkillDefOf.Shooting)?.Level ?? 0;
                score = shootingSkill * 10f;

                // 원거리 무기 기본 점수
                var verbProps = weapon.def.Verbs?.FirstOrDefault(v => v.defaultProjectile != null);
                if (verbProps != null)
                {
                    score += verbProps.defaultProjectile?.projectile?.GetDamageAmount(weapon) ?? 0;
                }
            }
            else if (weapon.def.IsMeleeWeapon)
            {
                // 근접 스킬 기반
                float meleeSkill = pawn.skills?.GetSkill(SkillDefOf.Melee)?.Level ?? 0;
                score = meleeSkill * 10f;

                // 근접 DPS
                score += weapon.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS) * 10f;
            }

            // 품질 보너스
            if (weapon.TryGetQuality(out var quality))
            {
                score += (int)quality * 5f;
            }

            return score;
        }

        /// <summary>
        /// 방어구 장착 시도
        /// </summary>
        private void TryEquipArmor(Pawn pawn, Map map)
        {
            // 현재 착용 중인 방어구 확인
            var currentArmor = pawn.apparel?.WornApparel ?? new List<Apparel>();

            // 헬멧 없으면 찾기
            bool hasHelmet = currentArmor.Any(a => a.def.apparel.bodyPartGroups.Any(
                bp => bp.defName.Contains("Head") || bp.defName.Contains("FullHead")));

            if (!hasHelmet)
            {
                TryWearBestApparel(pawn, map, ApparelLayerDefOf.Overhead);
            }

            // 갑옷 없으면 찾기
            bool hasBodyArmor = currentArmor.Any(a =>
                a.def.apparel.layers.Contains(ApparelLayerDefOf.Shell) ||
                a.def.apparel.layers.Contains(ApparelLayerDefOf.Middle));

            if (!hasBodyArmor)
            {
                TryWearBestApparel(pawn, map, ApparelLayerDefOf.Shell);
                TryWearBestApparel(pawn, map, ApparelLayerDefOf.Middle);
            }
        }

        /// <summary>
        /// 특정 레이어의 최고 의류 착용 시도
        /// </summary>
        private void TryWearBestApparel(Pawn pawn, Map map, ApparelLayerDef layer)
        {
            var availableApparel = map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel)
                .OfType<Apparel>()
                .Where(a => !a.IsForbidden(pawn) &&
                           a.def.apparel.layers.Contains(layer) &&
                           ApparelUtility.HasPartsToWear(pawn, a.def) &&
                           pawn.CanReserveAndReach(a, PathEndMode.Touch, Danger.Some))
                .OrderByDescending(a => GetArmorScore(a))
                .ToList();

            if (availableApparel.Count == 0) return;

            var bestApparel = availableApparel.First();

            // 착용 작업 생성
            var job = JobMaker.MakeJob(JobDefOf.Wear, bestApparel);
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);

            LogInfo($"{pawn.LabelShort}에게 {bestApparel.Label} 착용 지시");
        }

        /// <summary>
        /// 방어구 점수 계산
        /// </summary>
        private float GetArmorScore(Apparel apparel)
        {
            float score = 0f;

            // 방어력
            score += apparel.GetStatValue(StatDefOf.ArmorRating_Sharp) * 100f;
            score += apparel.GetStatValue(StatDefOf.ArmorRating_Blunt) * 50f;

            // 품질 보너스
            if (apparel.TryGetQuality(out var quality))
            {
                score += (int)quality * 10f;
            }

            return score;
        }

        private bool CanEquip(Map map)
        {
            if (!lastEquipTick.TryGetValue(map, out var lastTick))
                return true;

            return (Find.TickManager.TicksGame - lastTick) >= EQUIP_COOLDOWN;
        }

        public override List<RimAIAction> GetProposedActions(Map map)
        {
            return new List<RimAIAction>(); // 직접 실행
        }

        public override void ExecuteAction(RimAIAction action)
        {
            // 직접 실행 방식
        }

        public override void CleanupMap(Map map)
        {
            lastEquipTick.Remove(map);
        }
    }
}
