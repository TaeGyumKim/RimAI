using HarmonyLib;
using RimAI.Settings;
using UnityEngine;
using Verse;

namespace RimAI
{
    /// <summary>
    /// RimAI 모드의 메인 진입점
    /// Harmony 패치를 초기화하고 설정 UI를 제공합니다.
    /// </summary>
    public class RimAI_Mod : Mod
    {
        public const string HarmonyId = "rimai.autopilot";
        public const string ModName = "RimAI - Auto Pilot";
        public const string Version = "0.6.0";

        private static Harmony? harmonyInstance;
        private static RimAISettings? settings;

        /// <summary>
        /// 모드 생성자 - RimWorld가 모드 로드 시 호출
        /// </summary>
        public RimAI_Mod(ModContentPack content) : base(content)
        {
            try
            {
                Log.Message($"[{ModName}] v{Version} 초기화 시작...");

                // 설정 로드
                settings = GetSettings<RimAISettings>();

                // 기본 프리셋 적용 (최초 실행 시)
                if (settings.currentPreset == RimAIPreset.Custom)
                {
                    settings.ApplyPreset(RimAIPreset.FullAuto);
                }

                Log.Message($"[{ModName}] 설정 로드 완료 (프리셋: {settings.currentPreset})");
            }
            catch (System.Exception ex)
            {
                Log.Error($"[{ModName}] 초기화 중 오류 발생: {ex}");
            }
        }

        /// <summary>
        /// 설정 메뉴 이름
        /// </summary>
        public override string SettingsCategory()
        {
            return ModName;
        }

        /// <summary>
        /// 설정 UI 렌더링
        /// </summary>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            if (settings == null) return;

            RimAISettingsUI.DrawSettingsWindow(inRect, settings);
            base.DoSettingsWindowContents(inRect);
        }

        /// <summary>
        /// 설정 인스턴스 접근자
        /// </summary>
        public static RimAISettings Settings => settings ?? new RimAISettings();

        /// <summary>
        /// Harmony 인스턴스 접근자
        /// </summary>
        public static Harmony? Harmony => harmonyInstance;

        /// <summary>
        /// Harmony 초기화 (게임 시작 시 호출)
        /// </summary>
        [StaticConstructorOnStartup]
        public static class Bootstrap
        {
            static Bootstrap()
            {
                try
                {
                    // Harmony 인스턴스 생성 및 패치 적용
                    harmonyInstance = new Harmony(HarmonyId);
                    harmonyInstance.PatchAll();

                    Log.Message($"[{ModName}] Harmony 패치 적용 완료");
                    Log.Message($"[{ModName}] 초기화 성공!");
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[{ModName}] Harmony 패치 중 오류 발생: {ex}");
                }
            }
        }
    }
}
