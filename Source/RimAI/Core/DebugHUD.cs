using System.Text;
using UnityEngine;
using Verse;

namespace RimAI.Core
{
    /// <summary>
    /// RimAI 디버그 HUD
    /// 화면에 실시간 서브시스템 상태를 오버레이로 표시합니다.
    ///
    /// 단축키: Ctrl+Shift+R로 토글
    /// </summary>
    public class RimAIDebugHUD : GameComponent
    {
        private static bool isVisible = false;
        private static Rect windowRect = new Rect(10, 10, 400, 300);
        private static Vector2 scrollPosition = Vector2.zero;

        // 캐시된 상태 문자열 (매 프레임 업데이트하지 않음)
        private string cachedStatus = "";
        private int updateCounter = 0;
        private const int UPDATE_INTERVAL = 60; // 1초마다 업데이트

        public RimAIDebugHUD(Game game)
        {
        }

        /// <summary>
        /// HUD 표시 토글
        /// </summary>
        public static void ToggleVisibility()
        {
            isVisible = !isVisible;
            Log.Message($"[RimAI] Debug HUD: {(isVisible ? "ON" : "OFF")}");
        }

        /// <summary>
        /// HUD 표시 여부
        /// </summary>
        public static bool IsVisible => isVisible;

        /// <summary>
        /// 매 프레임 GUI 렌더링
        /// </summary>
        public override void GameComponentOnGUI()
        {
            base.GameComponentOnGUI();

            // 단축키 체크: Ctrl+Shift+R
            if (Event.current.type == EventType.KeyDown &&
                Event.current.keyCode == KeyCode.R &&
                Event.current.control &&
                Event.current.shift)
            {
                ToggleVisibility();
                Event.current.Use();
            }

            if (!isVisible) return;

            // 디버그 모드가 아니면 표시하지 않음
            if (!RimAI_Mod.Settings.ShouldLog(Settings.LogLevel.Debug))
            {
                // 하지만 수동으로 켰으면 표시
            }

            windowRect = GUILayout.Window(
                9999123, // 고유 ID
                windowRect,
                DrawWindow,
                "RimAI Debug HUD",
                GUILayout.Width(400),
                GUILayout.Height(300)
            );
        }

        /// <summary>
        /// 윈도우 내용 그리기
        /// </summary>
        private void DrawWindow(int windowId)
        {
            // 업데이트 카운터
            updateCounter++;
            if (updateCounter >= UPDATE_INTERVAL)
            {
                updateCounter = 0;
                UpdateCachedStatus();
            }

            // 스크롤 뷰
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true
            };

            GUILayout.Label(cachedStatus, labelStyle);

            GUILayout.EndScrollView();

            // 닫기 버튼
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Refresh"))
            {
                UpdateCachedStatus();
            }

            if (GUILayout.Button("Close"))
            {
                isVisible = false;
            }

            GUILayout.EndHorizontal();

            // 드래그 가능하게
            GUI.DragWindow();
        }

        /// <summary>
        /// 상태 문자열 업데이트
        /// </summary>
        private void UpdateCachedStatus()
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== RimAI Status ===");
            sb.AppendLine();

            // 현재 맵
            Map map = Find.CurrentMap;
            if (map == null)
            {
                sb.AppendLine("No map loaded");
                cachedStatus = sb.ToString();
                return;
            }

            sb.AppendLine($"Map: {map.info?.parent?.Label ?? "Unknown"}");
            sb.AppendLine($"Day: {GenDate.DaysPassed}");
            sb.AppendLine();

            // RimAIManager 상태
            var manager = RimAIManager.Instance;
            if (manager == null)
            {
                sb.AppendLine("RimAIManager not initialized");
                cachedStatus = sb.ToString();
                return;
            }

            // Genome 정보
            var genome = manager.GetCurrentGenome();
            sb.AppendLine($"Genome: {genome?.GenomeId ?? "None"}");
            sb.AppendLine($"Generation: {genome?.Generation ?? 0}");
            sb.AppendLine();

            // 서브시스템 상태
            sb.AppendLine("=== Subsystems ===");

            // Food
            var foodSub = manager.GetSubsystem<Food.FoodSubsystem>();
            if (foodSub != null)
            {
                var foodState = foodSub.GetState(map);
                sb.AppendLine($"[Food] {(foodSub.Enabled ? "ON" : "OFF")}");
                if (foodState != null)
                {
                    sb.AppendLine($"  Days: {foodState.DaysUntilStarvation:F1} | Crisis: {foodState.GetCrisisLevel()}");
                }
            }

            // Medical
            var medSub = manager.GetSubsystem<Medical.MedicalSubsystem>();
            if (medSub != null)
            {
                var medState = medSub.GetState(map);
                sb.AppendLine($"[Medical] {(medSub.Enabled ? "ON" : "OFF")}");
                if (medState != null)
                {
                    sb.AppendLine($"  Patients: {medState.PatientsWaiting} | Beds: {medState.AvailableMedicalBeds}/{medState.TotalMedicalBeds}");
                }
            }

            // Combat
            var combatSub = manager.GetSubsystem<Combat.CombatDefenseSubsystem>();
            if (combatSub != null)
            {
                sb.AppendLine($"[Combat] {(combatSub.Enabled ? "ON" : "OFF")}");
                sb.AppendLine($"  InCombat: {combatSub.IsInCombat(map)}");
            }

            // Trading
            var tradeSub = manager.GetSubsystem<Trading.TradingSubsystem>();
            if (tradeSub != null)
            {
                var tradeState = tradeSub.GetState(map);
                sb.AppendLine($"[Trading] {(tradeSub.Enabled ? "ON" : "OFF")}");
                if (tradeState != null)
                {
                    sb.AppendLine($"  Trader: {(tradeState.HasVisitingTrader ? "Yes" : "No")} | Silver: {tradeState.TotalSilver}");
                }
            }

            // Construction
            var consSub = manager.GetSubsystem<Construction.ConstructionSubsystem>();
            if (consSub != null)
            {
                sb.AppendLine($"[Construction] {(consSub.Enabled ? "ON" : "OFF")}");
            }

            // Production
            var prodSub = manager.GetSubsystem<Production.ProductionSubsystem>();
            if (prodSub != null)
            {
                sb.AppendLine($"[Production] {(prodSub.Enabled ? "ON" : "OFF")}");
            }

            // Research
            var resSub = manager.GetSubsystem<Research.ResearchSubsystem>();
            if (resSub != null)
            {
                sb.AppendLine($"[Research] {(resSub.Enabled ? "ON" : "OFF")}");
                var current = Find.ResearchManager?.GetProject();
                if (current != null)
                {
                    sb.AppendLine($"  Current: {current.label}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("=== GA Metrics ===");

            // Metrics 정보
            var collector = GA.MetricsCollector.Instance;
            if (collector != null)
            {
                var metrics = collector.GetCurrentMetrics();
                sb.AppendLine($"Deaths: {metrics.ColonistDeaths}");
                sb.AppendLine($"Breaks: {metrics.MentalBreakCount}");
                sb.AppendLine($"AvgMood: {metrics.AverageMood:F1}");
            }

            sb.AppendLine();
            sb.AppendLine("Press Ctrl+Shift+R to toggle");

            cachedStatus = sb.ToString();
        }
    }
}
