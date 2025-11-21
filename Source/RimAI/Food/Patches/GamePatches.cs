using HarmonyLib;
using Verse;

namespace RimAI.Food.Patches
{
    /// <summary>
    /// Game 초기화 시 FoodSubsystem 관련 로그
    /// FoodSubsystem은 RimAIManager에서 관리되므로 별도 GameComponent 불필요
    /// </summary>
    [HarmonyPatch(typeof(Game))]
    [HarmonyPatch(nameof(Game.FinalizeInit))]
    public static class Game_FinalizeInit_FoodSubsystem_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Game __instance)
        {
            try
            {
                Log.Message("[RimAI] FoodSubsystem 초기화 (RimAIManager에서 관리)");
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RimAI] Food 패치 초기화 중 오류: {ex}");
            }
        }
    }
}
