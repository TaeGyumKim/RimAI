using HarmonyLib;
using Verse;

namespace RimAI
{
    /// <summary>
    /// RimAI 모드의 메인 진입점
    /// Harmony 패치를 초기화하고 모드를 부트스트랩합니다.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class RimAI_Mod
    {
        public const string HarmonyId = "rimai.autopilot";
        public const string ModName = "RimAI - Auto Pilot";
        public const string Version = "0.1.0";

        private static Harmony? harmonyInstance;

        /// <summary>
        /// 정적 생성자 - 게임 시작 시 자동으로 호출됨
        /// </summary>
        static RimAI_Mod()
        {
            try
            {
                Log.Message($"[{ModName}] v{Version} 초기화 시작...");

                // Harmony 인스턴스 생성 및 패치 적용
                harmonyInstance = new Harmony(HarmonyId);
                harmonyInstance.PatchAll();

                Log.Message($"[{ModName}] Harmony 패치 적용 완료");
                Log.Message($"[{ModName}] 초기화 성공!");
            }
            catch (System.Exception ex)
            {
                Log.Error($"[{ModName}] 초기화 중 오류 발생: {ex}");
            }
        }

        /// <summary>
        /// Harmony 인스턴스 접근자
        /// </summary>
        public static Harmony? Harmony => harmonyInstance;
    }
}
