using UnityEngine;
using Verse;

namespace RimAI.Settings
{
    /// <summary>
    /// RimAI 설정 UI
    /// </summary>
    public static class RimAISettingsUI
    {
        private const float PRESET_BUTTON_HEIGHT = 40f;
        private const float SECTION_GAP = 20f;

        /// <summary>
        /// 설정 창 그리기
        /// </summary>
        public static void DrawSettingsWindow(Rect inRect, RimAISettings settings)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            // === 프리셋 버튼 ===
            DrawPresetButtons(listing, settings);
            listing.Gap(SECTION_GAP);

            // === 마스터 스위치 ===
            listing.CheckboxLabeled("전체 자동화 활성화", ref settings.masterEnabled,
                "모든 RimAI 자동화 기능을 켜거나 끕니다.");
            listing.Gap();

            if (!settings.masterEnabled)
            {
                listing.Label("(전체 자동화가 비활성화되어 있습니다)");
            }
            else
            {
                // === 서브시스템별 개입 강도 ===
                DrawSectionHeader(listing, "서브시스템 개입 강도");

                settings.foodIntensity = DrawIntensitySlider(listing, "식량 관리", settings.foodIntensity,
                    "농사, 사냥, 요리 등 식량 관련 자동화 강도");

                settings.constructionIntensity = DrawIntensitySlider(listing, "건설/확장", settings.constructionIntensity,
                    "침대, 인프라, 방어 시설 등 건설 자동화 강도");

                settings.researchIntensity = DrawIntensitySlider(listing, "연구 선택", settings.researchIntensity,
                    "콜로니 상황에 맞는 연구 자동 선택 강도");

                settings.combatIntensity = DrawIntensitySlider(listing, "전투 대응", settings.combatIntensity,
                    "적 습격 시 자동 징집 및 방어 강도");

                listing.Gap(SECTION_GAP);

                // === 카메라 설정 ===
                DrawSectionHeader(listing, "카메라 설정");
                listing.CheckboxLabeled("시네마틱 카메라 활성화", ref settings.cinematicCameraEnabled,
                    "전투 시 카메라가 자동으로 전장을 추적합니다.");
                listing.Gap(SECTION_GAP);

                // === 로그 설정 ===
                DrawSectionHeader(listing, "로그 상세도");
                DrawLogLevelButtons(listing, settings);
            }

            listing.End();
        }

        /// <summary>
        /// 프리셋 버튼 그리기
        /// </summary>
        private static void DrawPresetButtons(Listing_Standard listing, RimAISettings settings)
        {
            listing.Label("빠른 프리셋:");

            var rect = listing.GetRect(PRESET_BUTTON_HEIGHT);
            var buttonWidth = rect.width / 3f - 5f;

            // 완전 방치 관람 버튼
            var fullAutoRect = new Rect(rect.x, rect.y, buttonWidth, rect.height);
            if (Widgets.ButtonText(fullAutoRect, "완전 방치 관람"))
            {
                settings.ApplyPreset(RimAIPreset.FullAuto);
                Messages.Message("프리셋 적용: 완전 방치 관람 모드", MessageTypeDefOf.PositiveEvent, false);
            }
            if (Mouse.IsOver(fullAutoRect))
            {
                TooltipHandler.TipRegion(fullAutoRect,
                    "모든 자동화를 최대로 설정합니다.\n식량/건설/연구/전투 모두 적극 자동화.\n관람만 하고 싶을 때 추천합니다.");
            }

            // 위기만 대응 버튼
            var crisisRect = new Rect(fullAutoRect.xMax + 10f, rect.y, buttonWidth, rect.height);
            if (Widgets.ButtonText(crisisRect, "위기만 대응"))
            {
                settings.ApplyPreset(RimAIPreset.CrisisOnly);
                Messages.Message("프리셋 적용: 위기만 자동 대응 모드", MessageTypeDefOf.PositiveEvent, false);
            }
            if (Mouse.IsOver(crisisRect))
            {
                TooltipHandler.TipRegion(crisisRect,
                    "식량과 전투만 자동화합니다.\n건설/연구는 최소 개입으로 설정.\n생존만 맡기고 싶을 때 추천합니다.");
            }

            // 디버그 버튼
            var debugRect = new Rect(crisisRect.xMax + 10f, rect.y, buttonWidth, rect.height);
            if (Widgets.ButtonText(debugRect, "디버그 모드"))
            {
                settings.ApplyPreset(RimAIPreset.Debug);
                Messages.Message("프리셋 적용: 디버그 모드", MessageTypeDefOf.NeutralEvent, false);
            }
            if (Mouse.IsOver(debugRect))
            {
                TooltipHandler.TipRegion(debugRect,
                    "모든 자동화를 최대로 + 로그를 아주 상세하게 출력합니다.\n개발/테스트 용도입니다.");
            }
        }

        /// <summary>
        /// 섹션 헤더 그리기
        /// </summary>
        private static void DrawSectionHeader(Listing_Standard listing, string title)
        {
            Text.Font = GameFont.Medium;
            listing.Label(title);
            Text.Font = GameFont.Small;
            listing.GapLine();
        }

        /// <summary>
        /// 개입 강도 슬라이더 그리기
        /// </summary>
        private static AutomationIntensity DrawIntensitySlider(Listing_Standard listing, string label,
            AutomationIntensity current, string tooltip)
        {
            var rect = listing.GetRect(30f);
            var labelRect = new Rect(rect.x, rect.y, rect.width * 0.4f, rect.height);
            var sliderRect = new Rect(labelRect.xMax + 10f, rect.y, rect.width * 0.4f, rect.height);
            var valueRect = new Rect(sliderRect.xMax + 10f, rect.y, rect.width * 0.15f, rect.height);

            // 라벨
            Widgets.Label(labelRect, label);
            if (!tooltip.NullOrEmpty() && Mouse.IsOver(labelRect))
            {
                TooltipHandler.TipRegion(labelRect, tooltip);
            }

            // 슬라이더
            float sliderValue = (float)current;
            float newValue = Widgets.HorizontalSlider(sliderRect, sliderValue, 0f, 4f, false, null, "OFF", "FULL", 1f);
            var newIntensity = (AutomationIntensity)Mathf.RoundToInt(newValue);

            // 현재 값 표시
            string intensityLabel = GetIntensityLabel(newIntensity);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(valueRect, intensityLabel);
            Text.Anchor = TextAnchor.UpperLeft;

            return newIntensity;
        }

        /// <summary>
        /// 로그 레벨 버튼 그리기
        /// </summary>
        private static void DrawLogLevelButtons(Listing_Standard listing, RimAISettings settings)
        {
            var rect = listing.GetRect(30f);
            var buttonWidth = rect.width / 4f - 5f;

            for (int i = 0; i < 4; i++)
            {
                var level = (LogLevel)i;
                var buttonRect = new Rect(rect.x + i * (buttonWidth + 6.67f), rect.y, buttonWidth, rect.height);
                bool isSelected = settings.logLevel == level;

                if (isSelected)
                {
                    GUI.color = Color.green;
                }

                if (Widgets.ButtonText(buttonRect, GetLogLevelLabel(level)))
                {
                    settings.logLevel = level;
                    settings.currentPreset = RimAIPreset.Custom; // 수동 변경 시 Custom으로
                }

                GUI.color = Color.white;

                if (Mouse.IsOver(buttonRect))
                {
                    TooltipHandler.TipRegion(buttonRect, GetLogLevelTooltip(level));
                }
            }
        }

        /// <summary>
        /// 개입 강도 라벨 가져오기
        /// </summary>
        private static string GetIntensityLabel(AutomationIntensity intensity)
        {
            switch (intensity)
            {
                case AutomationIntensity.Off:
                    return "OFF";
                case AutomationIntensity.Low:
                    return "낮음";
                case AutomationIntensity.Medium:
                    return "중간";
                case AutomationIntensity.High:
                    return "높음";
                case AutomationIntensity.Full:
                    return "최대";
                default:
                    return "?";
            }
        }

        /// <summary>
        /// 로그 레벨 라벨 가져오기
        /// </summary>
        private static string GetLogLevelLabel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Minimal:
                    return "최소";
                case LogLevel.Normal:
                    return "일반";
                case LogLevel.Detailed:
                    return "상세";
                case LogLevel.Debug:
                    return "디버그";
                default:
                    return "?";
            }
        }

        /// <summary>
        /// 로그 레벨 툴팁 가져오기
        /// </summary>
        private static string GetLogLevelTooltip(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Minimal:
                    return "오류와 경고만 로그에 출력합니다.";
                case LogLevel.Normal:
                    return "주요 이벤트만 로그에 출력합니다. (권장)";
                case LogLevel.Detailed:
                    return "모든 결정과 행동을 로그에 출력합니다.";
                case LogLevel.Debug:
                    return "개발자용 디버그 정보까지 모두 출력합니다.";
                default:
                    return "";
            }
        }
    }
}
