using HarmonyLib;
using Verse;

namespace RimAI.Food.Patches
{
    /// <summary>
    /// Game 초기화 시 FoodManager를 GameComponent로 등록
    /// </summary>
    [HarmonyPatch(typeof(Game))]
    [HarmonyPatch(nameof(Game.FinalizeInit))]
    public static class Game_FinalizeInit_FoodManager_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Game __instance)
        {
            try
            {
                // FoodManager가 이미 등록되어 있는지 확인
                var existingManager = __instance.GetComponent<FoodManager>();

                if (existingManager == null)
                {
                    // FoodManager를 GameComponent로 추가
                    var manager = new FoodManager(__instance);
                    __instance.components.Add(manager);

                    Log.Message("[RimAI] FoodManager 등록 완료");
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RimAI] FoodManager 등록 중 오류: {ex}");
            }
        }
    }
}
