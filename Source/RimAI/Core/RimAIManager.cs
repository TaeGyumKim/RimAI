using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimAI.Core
{
    /// <summary>
    /// RimAI 중앙 브레인
    /// 모든 서브시스템을 관리하고 액션을 조율합니다.
    /// </summary>
    public class RimAIManager : GameComponent
    {
        // 업데이트 주기 (틱 단위)
        private const int TICK_INTERVAL = 60; // 1초마다 틱 처리

        // 등록된 서브시스템 목록
        private List<IRimAISubsystem> subsystems = new List<IRimAISubsystem>();

        // 실행 대기 중인 액션 큐 (맵별)
        private Dictionary<Map, List<RimAIAction>> pendingActions = new Dictionary<Map, List<RimAIAction>>();

        // 최근 실행된 액션 로그 (디버그용)
        private List<RimAIAction> executedActions = new List<RimAIAction>();
        private const int MAX_ACTION_LOG = 100;

        private int tickCounter = 0;

        public RimAIManager(Game game)
        {
        }

        /// <summary>
        /// 서브시스템 등록
        /// </summary>
        public void RegisterSubsystem(IRimAISubsystem subsystem)
        {
            if (!subsystems.Contains(subsystem))
            {
                subsystems.Add(subsystem);
                subsystems = subsystems.OrderByDescending(s => s.Priority).ToList();
                subsystem.Initialize();
                Log.Message($"[RimAI] 서브시스템 등록: {subsystem.Name} (우선순위: {subsystem.Priority})");
            }
        }

        /// <summary>
        /// 매 틱마다 호출
        /// </summary>
        public override void GameComponentTick()
        {
            base.GameComponentTick();

            tickCounter++;

            if (tickCounter >= TICK_INTERVAL)
            {
                tickCounter = 0;
                ProcessAllMaps();
            }
        }

        /// <summary>
        /// 모든 맵 처리
        /// </summary>
        private void ProcessAllMaps()
        {
            if (Current.Game == null) return;

            foreach (var map in Find.Maps)
            {
                if (!map.IsPlayerHome) continue;

                try
                {
                    ProcessMap(map);
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[RimAI] 맵 {map} 처리 중 오류: {ex}");
                }
            }
        }

        /// <summary>
        /// 특정 맵 처리
        /// </summary>
        private void ProcessMap(Map map)
        {
            // 전투 상태 확인
            bool inCombat = IsMapInCombat(map);

            // 1. 모든 서브시스템 업데이트
            foreach (var subsystem in subsystems)
            {
                if (!subsystem.Enabled) continue;

                // 전투 중일 때는 비전투 서브시스템 일시 정지
                if (inCombat && ShouldPauseDuringCombat(subsystem))
                {
                    continue;
                }

                try
                {
                    subsystem.Update(map);
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[RimAI] {subsystem.Name} 업데이트 중 오류: {ex}");
                }
            }

            // 2. 액션 수집 및 우선순위 조정
            CollectAndPrioritizeActions(map, inCombat);

            // 3. 액션 실행
            ExecutePendingActions(map);
        }

        /// <summary>
        /// 맵이 전투 중인지 확인
        /// </summary>
        private bool IsMapInCombat(Map map)
        {
            // Combat 서브시스템에서 전투 상태 확인
            var combatSubsystem = GetSubsystem<RimAI.Combat.CombatDefenseSubsystem>();
            if (combatSubsystem != null)
            {
                return combatSubsystem.IsInCombat(map);
            }

            return false;
        }

        /// <summary>
        /// 전투 중 일시 정지해야 하는 서브시스템인지 확인
        /// </summary>
        private bool ShouldPauseDuringCombat(IRimAISubsystem subsystem)
        {
            // Combat, Food는 항상 작동
            if (subsystem.Name == "Combat" || subsystem.Name == "Food")
                return false;

            // Construction, Production, Research는 전투 중 일시 정지
            if (subsystem.Name == "Construction" || subsystem.Name == "Production" || subsystem.Name == "Research")
                return true;

            return false;
        }

        /// <summary>
        /// 모든 서브시스템에서 제안된 액션 수집 및 우선순위 정렬
        /// </summary>
        private void CollectAndPrioritizeActions(Map map, bool inCombat)
        {
            var allActions = new List<RimAIAction>();

            foreach (var subsystem in subsystems)
            {
                if (!subsystem.Enabled) continue;

                try
                {
                    var actions = subsystem.GetProposedActions(map);
                    if (actions != null && actions.Count > 0)
                    {
                        allActions.AddRange(actions);
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[RimAI] {subsystem.Name}에서 액션 수집 중 오류: {ex}");
                }
            }

            // 우선순위 정렬 및 충돌 해결
            var resolvedActions = ResolveConflicts(allActions);

            // 맵별 대기 큐에 추가
            if (!pendingActions.ContainsKey(map))
            {
                pendingActions[map] = new List<RimAIAction>();
            }

            pendingActions[map].AddRange(resolvedActions);
        }

        /// <summary>
        /// 액션 충돌 해결 및 우선순위 조정
        /// </summary>
        private List<RimAIAction> ResolveConflicts(List<RimAIAction> actions)
        {
            if (actions.Count == 0) return actions;

            // 1. 우선순위 순으로 정렬 (Critical → VeryLow)
            var sorted = actions
                .Where(a => a.IsValid())
                .OrderByDescending(a => (int)a.Priority)
                .ThenByDescending(a => GetSubsystemPriority(a.SourceSubsystem))
                .ToList();

            // 2. 충돌 제거 (같은 위치에 여러 건설 액션 등)
            var resolved = new List<RimAIAction>();
            var occupiedCells = new HashSet<IntVec3>();

            foreach (var action in sorted)
            {
                // 건설 액션의 경우 위치 중복 체크
                if (action.Type == RimAIActionType.PlaceBlueprint ||
                    action.Type == RimAIActionType.ConstructBuilding)
                {
                    if (action.TargetCell.HasValue && occupiedCells.Contains(action.TargetCell.Value))
                    {
                        continue; // 이미 다른 액션이 선점
                    }

                    if (action.TargetCell.HasValue)
                    {
                        occupiedCells.Add(action.TargetCell.Value);
                    }
                }

                resolved.Add(action);
            }

            return resolved;
        }

        /// <summary>
        /// 서브시스템 우선순위 가져오기
        /// </summary>
        private int GetSubsystemPriority(string subsystemName)
        {
            var subsystem = subsystems.FirstOrDefault(s => s.Name == subsystemName);
            return subsystem?.Priority ?? 0;
        }

        /// <summary>
        /// 대기 중인 액션 실행
        /// </summary>
        private void ExecutePendingActions(Map map)
        {
            if (!pendingActions.TryGetValue(map, out var actions)) return;
            if (actions.Count == 0) return;

            // 한 번에 너무 많은 액션을 실행하지 않도록 제한
            const int MAX_ACTIONS_PER_TICK = 5;

            int executed = 0;
            var toRemove = new List<RimAIAction>();

            foreach (var action in actions)
            {
                if (executed >= MAX_ACTIONS_PER_TICK) break;

                try
                {
                    // 서브시스템에 실행 위임
                    var subsystem = subsystems.FirstOrDefault(s => s.Name == action.SourceSubsystem);
                    if (subsystem != null && subsystem.Enabled)
                    {
                        subsystem.ExecuteAction(action);

                        // 실행 로그 기록
                        executedActions.Add(action);
                        if (executedActions.Count > MAX_ACTION_LOG)
                        {
                            executedActions.RemoveAt(0);
                        }

                        if (RimAI_Mod.Settings.ShouldLog(RimAI.Settings.LogLevel.Detailed))
                        {
                            Log.Message($"[RimAI] 액션 실행: {action}");
                        }

                        executed++;
                    }

                    toRemove.Add(action);
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[RimAI] 액션 실행 중 오류: {action} - {ex}");
                    toRemove.Add(action);
                }
            }

            // 실행된 액션 제거
            foreach (var action in toRemove)
            {
                actions.Remove(action);
            }
        }

        /// <summary>
        /// 특정 서브시스템 가져오기
        /// </summary>
        public T? GetSubsystem<T>() where T : class, IRimAISubsystem
        {
            return subsystems.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        public string GetDebugInfo()
        {
            var info = $"[RimAI Manager]\n";
            info += $"등록된 서브시스템: {subsystems.Count}\n";

            foreach (var subsystem in subsystems)
            {
                info += $"  - {subsystem.Name} (우선순위: {subsystem.Priority}, 활성: {subsystem.Enabled})\n";
            }

            info += $"\n최근 실행된 액션: {executedActions.Count}개\n";
            foreach (var action in executedActions.TakeLast(5))
            {
                info += $"  - {action}\n";
            }

            return info;
        }

        /// <summary>
        /// 세이브 파일 저장/로드
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();

            foreach (var subsystem in subsystems)
            {
                try
                {
                    subsystem.ExposeData();
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[RimAI] {subsystem.Name} ExposeData 중 오류: {ex}");
                }
            }

            // 로드 후 유효하지 않은 맵 데이터 정리 (메모리 누수 방지)
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                CleanupInvalidMaps();
            }
        }

        /// <summary>
        /// 유효하지 않은 맵 데이터 정리 (메모리 누수 방지)
        /// </summary>
        private void CleanupInvalidMaps()
        {
            // pendingActions에서 유효하지 않은 맵 제거
            var invalidMaps = new List<Map>();
            foreach (var map in pendingActions.Keys)
            {
                if (map == null || map.Index < 0 || !Find.Maps.Contains(map))
                {
                    invalidMaps.Add(map);
                }
            }

            foreach (var map in invalidMaps)
            {
                pendingActions.Remove(map);

                // 모든 서브시스템에 맵 정리 통지
                foreach (var subsystem in subsystems)
                {
                    try
                    {
                        subsystem.CleanupMap(map);
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"[RimAI] {subsystem.Name} 맵 정리 중 오류: {ex}");
                    }
                }

                Log.Message($"[RimAI] 맵 {map?.Index ?? -1} 데이터 정리 완료 (메모리 누수 방지)");
            }

            // ThreatAnalyzer 정적 캐시 완전 정리
            if (invalidMaps.Count > 0)
            {
                Combat.ThreatAnalyzer.ClearCache();
            }
        }

        /// <summary>
        /// 싱글톤 인스턴스 접근
        /// </summary>
        public static RimAIManager? Instance
        {
            get
            {
                if (Current.Game == null) return null;
                return Current.Game.GetComponent<RimAIManager>();
            }
        }
    }

    /// <summary>
    /// RimAI 전역 설정
    /// </summary>
    public static class RimAI_Settings
    {
        /// <summary>상세 로그 활성화</summary>
        public static bool EnableDetailedLogging = true;

        /// <summary>식량 자동화 활성화</summary>
        public static bool EnableFoodAutomation = true;

        /// <summary>건설 자동화 활성화</summary>
        public static bool EnableConstructionAutomation = true;

        /// <summary>연구 자동화 활성화</summary>
        public static bool EnableResearchAutomation = true;
    }
}
