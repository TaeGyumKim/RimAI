using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimAI.Food.Patches
{
    /// <summary>
    /// WorkGiver_GrowerSow (파종) 패치
    /// 의사결정 엔진의 파종 우선순위에 따라 작업을 활성화/비활성화
    /// </summary>
    [HarmonyPatch(typeof(WorkGiver_GrowerSow))]
    [HarmonyPatch(nameof(WorkGiver_GrowerSow.ShouldSkip))]
    public static class WorkGiver_GrowerSow_ShouldSkip_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref bool __result, Pawn pawn)
        {
            if (!RimAI_Settings.EnableFoodAutomation) return;

            var manager = FoodManager.Instance;
            if (manager == null) return;

            var decision = manager.GetDecision(pawn.Map);
            if (decision == null) return;

            // 우선순위가 None이면 작업 스킵
            if (decision.SowingPriority == FoodWorkPriority.None)
            {
                __result = true;
            }
            // 우선순위가 Critical이면 무조건 활성화
            else if (decision.SowingPriority == FoodWorkPriority.Critical)
            {
                __result = false;
            }
            // 나머지는 기본 로직 유지
        }
    }

    /// <summary>
    /// WorkGiver_GrowerHarvest (수확) 패치
    /// </summary>
    [HarmonyPatch(typeof(WorkGiver_GrowerHarvest))]
    [HarmonyPatch(nameof(WorkGiver_GrowerHarvest.ShouldSkip))]
    public static class WorkGiver_GrowerHarvest_ShouldSkip_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref bool __result, Pawn pawn)
        {
            if (!RimAI_Settings.EnableFoodAutomation) return;

            var manager = FoodManager.Instance;
            if (manager == null) return;

            var decision = manager.GetDecision(pawn.Map);
            if (decision == null) return;

            if (decision.HarvestingPriority == FoodWorkPriority.None)
            {
                __result = true;
            }
            else if (decision.HarvestingPriority == FoodWorkPriority.Critical)
            {
                __result = false;
            }
        }
    }

    /// <summary>
    /// WorkGiver_HunterHunt (사냥) 패치
    /// </summary>
    [HarmonyPatch(typeof(WorkGiver_HunterHunt))]
    [HarmonyPatch(nameof(WorkGiver_HunterHunt.ShouldSkip))]
    public static class WorkGiver_HunterHunt_ShouldSkip_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref bool __result, Pawn pawn)
        {
            if (!RimAI_Settings.EnableFoodAutomation) return;

            var manager = FoodManager.Instance;
            if (manager == null) return;

            var decision = manager.GetDecision(pawn.Map);
            if (decision == null) return;

            if (decision.HuntingPriority == FoodWorkPriority.None)
            {
                __result = true;
            }
            else if (decision.HuntingPriority == FoodWorkPriority.Critical)
            {
                __result = false;
            }
        }
    }

    /// <summary>
    /// WorkGiver_DoBill 패치 (요리 포함)
    /// 요리 작업의 우선순위 조정
    ///
    /// 참고: DoBill은 요리뿐만 아니라 모든 작업대 작업을 포함하므로,
    /// 요리 작업만 필터링해야 합니다.
    /// </summary>
    [HarmonyPatch(typeof(WorkGiver_DoBill))]
    [HarmonyPatch(nameof(WorkGiver_DoBill.ShouldSkip))]
    public static class WorkGiver_DoBill_ShouldSkip_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref bool __result, Pawn pawn, bool forced)
        {
            if (!RimAI_Settings.EnableFoodAutomation) return;
            if (forced) return; // 플레이어가 강제 지시한 경우 무시

            var manager = FoodManager.Instance;
            if (manager == null) return;

            var decision = manager.GetDecision(pawn.Map);
            if (decision == null) return;

            // 요리 작업 여부는 실제 Job 할당 시점에 판단하기 어려우므로,
            // 여기서는 Critical 우선순위일 때만 활성화
            if (decision.CookingPriority == FoodWorkPriority.Critical)
            {
                __result = false;
            }
        }
    }

    /// <summary>
    /// Pawn_WorkSettings.GetPriority() 패치
    /// 작업 우선순위를 동적으로 조정
    ///
    /// 이 패치는 더 세밀한 제어를 위한 대안 방식입니다.
    /// ShouldSkip 패치와 함께 사용할 수 있습니다.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_WorkSettings))]
    [HarmonyPatch(nameof(Pawn_WorkSettings.GetPriority))]
    public static class Pawn_WorkSettings_GetPriority_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref int __result, Pawn_WorkSettings __instance, WorkTypeDef w)
        {
            if (!RimAI_Settings.EnableFoodAutomation) return;
            if (__result == 0) return; // 비활성화된 작업은 건드리지 않음

            var pawn = __instance.pawn;
            if (pawn?.Map == null) return;

            var manager = FoodManager.Instance;
            if (manager == null) return;

            var decision = manager.GetDecision(pawn.Map);
            if (decision == null) return;

            // 작업 타입에 따라 우선순위 부스트
            int boost = 0;

            if (w == WorkTypeDefOf.Growing)
            {
                boost = GetPriorityBoost(decision.SowingPriority, decision.HarvestingPriority);
            }
            else if (w == WorkTypeDefOf.Cooking)
            {
                boost = GetPriorityBoost(decision.CookingPriority);
            }
            else if (w == WorkTypeDefOf.Hunting)
            {
                boost = GetPriorityBoost(decision.HuntingPriority);
            }

            // 우선순위 조정 (1~4 범위 유지)
            if (boost != 0)
            {
                __result = UnityEngine.Mathf.Clamp(__result + boost, 1, 4);
            }
        }

        /// <summary>
        /// FoodWorkPriority를 우선순위 부스트 값으로 변환
        /// </summary>
        private static int GetPriorityBoost(params FoodWorkPriority[] priorities)
        {
            var maxPriority = FoodWorkPriority.None;
            foreach (var p in priorities)
            {
                if (p > maxPriority) maxPriority = p;
            }

            return maxPriority switch
            {
                FoodWorkPriority.Critical => 2,  // +2 부스트
                FoodWorkPriority.High => 1,      // +1 부스트
                FoodWorkPriority.Normal => 0,    // 변경 없음
                FoodWorkPriority.Low => -1,      // -1 감소
                FoodWorkPriority.None => -2,     // -2 감소
                _ => 0
            };
        }
    }
}
