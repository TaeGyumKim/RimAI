using HarmonyLib;
using RimAI.Food;
using RimAI.Construction;
using RimAI.Research;
using RimAI.Production;
using RimAI.Medical;
using Verse;

namespace RimAI.Core.Patches
{
    /// <summary>
    /// Game 초기화 시 RimAIManager 및 모든 서브시스템을 등록
    /// </summary>
    [HarmonyPatch(typeof(Game))]
    [HarmonyPatch(nameof(Game.FinalizeInit))]
    public static class Game_FinalizeInit_RimAIManager_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Game __instance)
        {
            try
            {
                // RimAIManager가 이미 등록되어 있는지 확인
                var existingManager = __instance.GetComponent<RimAIManager>();

                if (existingManager == null)
                {
                    // RimAIManager를 GameComponent로 추가
                    var manager = new RimAIManager(__instance);
                    __instance.components.Add(manager);

                    // Genome 자동 로드
                    manager.AutoLoadGenome();

                    // 모든 서브시스템 등록
                    RegisterAllSubsystems(manager);

                    Log.Message("[RimAI] RimAIManager 및 모든 서브시스템 등록 완료");
                }
                else
                {
                    // 기존 매니저가 있으면 Genome 자동 로드
                    existingManager.AutoLoadGenome();
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RimAI] RimAIManager 등록 중 오류: {ex}");
            }
        }

        /// <summary>
        /// 모든 서브시스템을 RimAIManager에 등록
        /// </summary>
        private static void RegisterAllSubsystems(RimAIManager manager)
        {
            // 1. 전투 서브시스템 (우선순위: 150 - 최우선)
            manager.RegisterSubsystem(new RimAI.Combat.CombatDefenseSubsystem());

            // 2. 식량 서브시스템 (우선순위: 100)
            manager.RegisterSubsystem(new FoodSubsystem());

            // 3. 의료 서브시스템 (우선순위: 95 - 식량 다음)
            manager.RegisterSubsystem(new MedicalSubsystem());

            // 4. 건설 서브시스템 (우선순위: 50)
            manager.RegisterSubsystem(new ConstructionSubsystem());

            // 5. 생산 서브시스템 (우선순위: 30)
            manager.RegisterSubsystem(new ProductionSubsystem());

            // 6. 연구 서브시스템 (우선순위: 30)
            manager.RegisterSubsystem(new ResearchSubsystem());
        }
    }
}
