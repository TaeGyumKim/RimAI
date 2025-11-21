using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimAI.GA.Patches
{
    /// <summary>
    /// 게임 종료 시점을 감지하여 metrics를 저장하는 패치들
    /// </summary>
    public static class GameEndPatches
    {
        /// <summary>
        /// 우주선 발사 시 (승리 엔딩)
        /// </summary>
        [HarmonyPatch(typeof(Building_ShipComputerCore), nameof(Building_ShipComputerCore.TryLaunch))]
        public static class ShipLaunch_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(bool __result)
            {
                if (!__result)
                    return;

                try
                {
                    var collector = MetricsCollector.Instance;
                    if (collector != null)
                    {
                        collector.OnGameEnded("ShipLaunch", true);
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[RimAI-GA] ShipLaunch 패치 오류: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 게임 종료 조건 체크 (전멸 등)
        /// </summary>
        [HarmonyPatch(typeof(GameEnder), nameof(GameEnder.CheckOrUpdateGameOver))]
        public static class GameOver_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(GameEnder __instance)
            {
                try
                {
                    // 게임 오버 상태인지 확인
                    if (__instance.gameEnding)
                    {
                        var collector = MetricsCollector.Instance;
                        if (collector != null)
                        {
                            // 전멸 원인 파악
                            string endReason = "Unknown";
                            bool success = false;

                            // 콜로니스트가 전멸했는지 확인
                            Map homeMap = Find.Maps?.FirstOrDefault(m => m.IsPlayerHome);
                            if (homeMap != null)
                            {
                                int colonistCount = homeMap.mapPawns.FreeColonistsSpawnedCount;
                                if (colonistCount == 0)
                                {
                                    endReason = "AllColonistsDead";
                                    success = false;
                                }
                            }

                            collector.OnGameEnded(endReason, success);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[RimAI-GA] GameOver 패치 오류: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 폰 사망 시 (콜로니스트 vs 동물 구분)
        /// </summary>
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
        public static class PawnDeath_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(Pawn __instance)
            {
                try
                {
                    if (__instance.Faction != Faction.OfPlayer)
                        return;

                    var collector = MetricsCollector.Instance;
                    if (collector == null)
                        return;

                    if (__instance.RaceProps.Humanlike)
                    {
                        collector.OnColonistDied();
                        Log.Message($"[RimAI-GA] 콜로니스트 사망: {__instance.Name}");
                    }
                    else if (__instance.RaceProps.Animal)
                    {
                        collector.OnAnimalDied();
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[RimAI-GA] PawnDeath 패치 오류: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 정신 붕괴 시작 시
        /// </summary>
        [HarmonyPatch(typeof(MentalStateHandler), nameof(MentalStateHandler.TryStartMentalState))]
        public static class MentalBreak_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(bool __result, Pawn ___pawn)
            {
                if (!__result)
                    return;

                try
                {
                    if (___pawn.Faction != Faction.OfPlayer)
                        return;

                    var collector = MetricsCollector.Instance;
                    if (collector != null)
                    {
                        collector.OnMentalBreak();
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[RimAI-GA] MentalBreak 패치 오류: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 게임 컴포넌트 등록 (MetricsCollector)
        /// </summary>
        [HarmonyPatch(typeof(Game), nameof(Game.FinalizeInit))]
        public static class Game_FinalizeInit_MetricsCollector_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(Game __instance)
            {
                try
                {
                    // MetricsCollector가 이미 등록되어 있는지 확인
                    var existingCollector = __instance.GetComponent<MetricsCollector>();

                    if (existingCollector == null)
                    {
                        var collector = new MetricsCollector(__instance);
                        __instance.components.Add(collector);
                        Log.Message("[RimAI-GA] MetricsCollector 등록 완료");
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[RimAI-GA] MetricsCollector 등록 중 오류: {ex}");
                }
            }
        }
    }
}
