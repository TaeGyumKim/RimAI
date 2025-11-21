using HarmonyLib;
using RimAI.Food;
using RimAI.Construction;
using RimAI.Research;
using RimAI.Production;
using RimAI.Medical;
using RimAI.Trading;
using RimAI.Zones;
using RimAI.Equipment;
using RimAI.Work;
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

                    // Debug HUD 등록
                    var existingHUD = __instance.GetComponent<RimAIDebugHUD>();
                    if (existingHUD == null)
                    {
                        __instance.components.Add(new RimAIDebugHUD(__instance));
                    }

                    Log.Message("[RimAI] RimAIManager 및 모든 서브시스템 등록 완료 (Ctrl+Shift+R: Debug HUD)");
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

            // 3. 작업 우선순위 서브시스템 (우선순위: 95 - 매우 중요!)
            manager.RegisterSubsystem(new WorkPrioritySubsystem());

            // 4. 의료 서브시스템 (우선순위: 95 - 식량 다음)
            manager.RegisterSubsystem(new MedicalSubsystem());

            // 5. 구역/지정 서브시스템 (우선순위: 90 - 자원 수집!)
            manager.RegisterSubsystem(new ZoneDesignationSubsystem());

            // 6. 장비 서브시스템 (우선순위: 55)
            manager.RegisterSubsystem(new EquipmentSubsystem());

            // 7. 건설 서브시스템 (우선순위: 50)
            manager.RegisterSubsystem(new ConstructionSubsystem());

            // 8. 무역 서브시스템 (우선순위: 40)
            manager.RegisterSubsystem(new TradingSubsystem());

            // 9. 생산 서브시스템 (우선순위: 30)
            manager.RegisterSubsystem(new ProductionSubsystem());

            // 10. 연구 서브시스템 (우선순위: 30)
            manager.RegisterSubsystem(new ResearchSubsystem());
        }
    }
}
