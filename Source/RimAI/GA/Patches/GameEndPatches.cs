using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace RimAI.GA.Patches
{
    /// <summary>
    /// 게임 종료 시점을 감지하여 metrics를 저장하는 패치들
    /// v2.0: EndReason enum 사용, 더 많은 엔딩 케이스 처리
    /// </summary>
    public static class GameEndPatches
    {
        /// <summary>
        /// 우주선 카운트다운 완료 시 (가장 일반적인 승리 엔딩)
        /// ShipCountdown.CountdownEnded가 true가 되면 우주선 발사 완료
        /// </summary>
        [HarmonyPatch(typeof(ShipCountdown), nameof(ShipCountdown.ShipCountdownUpdate))]
        public static class ShipLaunch_Patch
        {
            private static bool wasCountingDown = false;

            [HarmonyPostfix]
            public static void Postfix()
            {
                try
                {
                    // 카운트다운이 진행 중이었는데 끝났으면 발사 완료
                    bool isCountingDown = ShipCountdown.CountingDown;

                    if (wasCountingDown && !isCountingDown)
                    {
                        var collector = MetricsCollector.Instance;
                        if (collector != null)
                        {
                            collector.OnGameEnded(EndReason.ShipLaunched);
                            Log.Message("[RimAI-GA] 우주선 발사 엔딩 감지");
                        }
                    }

                    wasCountingDown = isCountingDown;
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[RimAI-GA] ShipLaunch 패치 오류: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 게임 종료 조건 체크 (전멸, 포기 등)
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
                    if (!__instance.gameEnding)
                        return;

                    var collector = MetricsCollector.Instance;
                    if (collector == null)
                        return;

                    // 전멸 원인 파악
                    EndReason endReason = DetermineGameOverReason();

                    collector.OnGameEnded(endReason);
                    Log.Message($"[RimAI-GA] ❌ 게임 오버: {endReason.ToKoreanString()}");
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[RimAI-GA] GameOver 패치 오류: {ex.Message}");
                }
            }

            /// <summary>
            /// 게임 오버 원인 판단
            /// </summary>
            private static EndReason DetermineGameOverReason()
            {
                // 1. 콜로니스트가 전멸했는지 확인
                Map homeMap = Find.Maps?.FirstOrDefault(m => m.IsPlayerHome);
                if (homeMap != null)
                {
                    int colonistCount = homeMap.mapPawns.FreeColonistsSpawnedCount;
                    if (colonistCount == 0)
                    {
                        return EndReason.AllColonistsDead;
                    }
                }

                // 2. 플레이어가 포기했는지 확인 (전멸은 아닌데 종료)
                // (RimWorld에서는 명시적인 포기 플래그가 없으므로 추정)
                return EndReason.ColonyAbandoned;
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
                        Log.Message($"[RimAI-GA] 💀 콜로니스트 사망: {__instance.Name}");
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
