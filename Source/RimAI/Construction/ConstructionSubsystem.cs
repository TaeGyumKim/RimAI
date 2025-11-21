using System.Collections.Generic;
using RimAI.Core;
using RimWorld;
using Verse;

namespace RimAI.Construction
{
    /// <summary>
    /// 건설/확장 자동화 서브시스템
    /// 기본 인프라, 거주 시설, 방어 구조물을 자동으로 건설합니다.
    /// </summary>
    public class ConstructionSubsystem : RimAISubsystemBase
    {
        // 쿨다운 관리 (맵별)
        private Dictionary<Map, int> lastConstructionTick = new Dictionary<Map, int>();
        private const int CONSTRUCTION_COOLDOWN = 60000; // 1000초 (약 16분) - 큰 변화는 드물게

        // 맵별 상태 캐시
        private Dictionary<Map, ColonyConstructionState> mapStates = new Dictionary<Map, ColonyConstructionState>();

        public override string Name => "Construction";
        public override int Priority => 50; // 중간 우선순위

        public ConstructionSubsystem()
        {
            updateInterval = 600; // 10초마다 업데이트
        }

        public override void Initialize()
        {
            base.Initialize();
            Enabled = RimAI_Settings.EnableConstructionAutomation;
        }

        public override void Update(Map map)
        {
            if (!ShouldUpdate()) return;
            if (!Enabled) return;

            try
            {
                // 상태 분석
                var state = ConstructionAnalyzer.AnalyzeMap(map);
                mapStates[map] = state;

                // 디버그 로그
                if (Prefs.DevMode && RimAI_Settings.EnableDetailedLogging)
                {
                    Log.Message($"[RimAI-Construction] {state}");
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RimAI-Construction] 맵 {map} 업데이트 중 오류: {ex}");
            }
        }

        public override List<RimAIAction> GetProposedActions(Map map)
        {
            var actions = new List<RimAIAction>();

            if (!mapStates.TryGetValue(map, out var state))
                return actions;

            // 쿨다운 체크 - 너무 자주 건설하지 않음
            if (!CanConstruct(map))
            {
                return actions;
            }

            // 1. 침대 부족 시 침대 건설 (최우선)
            if (state.NeedMoreBeds())
            {
                actions.Add(new RimAIAction
                {
                    Type = RimAIActionType.ConstructBuilding,
                    Priority = RimAIActionPriority.High,
                    SourceSubsystem = Name,
                    TargetMap = map,
                    TargetThingDef = ThingDefOf.Bed, // 간단한 침대
                    Description = $"침대 부족 ({state.TotalBeds}/{state.ColonistCount}) - 침대 건설 필요"
                });
            }

            // 2. 기본 인프라 부족
            if (state.NeedBasicInfrastructure())
            {
                if (!state.HasKitchen)
                {
                    actions.Add(new RimAIAction
                    {
                        Type = RimAIActionType.ConstructBuilding,
                        Priority = RimAIActionPriority.High,
                        SourceSubsystem = Name,
                        TargetMap = map,
                        Description = "주방 없음 - 요리대 건설 필요"
                    });
                }

                if (!state.HasStorageRoom)
                {
                    actions.Add(new RimAIAction
                    {
                        Type = RimAIActionType.Other,
                        Priority = RimAIActionPriority.Normal,
                        SourceSubsystem = Name,
                        TargetMap = map,
                        Description = "창고 구역 없음 - 스톡파일 구역 생성 필요"
                    });
                }
            }

            // 3. 방어 시설 부족
            if (state.NeedDefenses() && state.ColonistCount >= 3)
            {
                actions.Add(new RimAIAction
                {
                    Type = RimAIActionType.ConstructBuilding,
                    Priority = RimAIActionPriority.Normal,
                    SourceSubsystem = Name,
                    TargetMap = map,
                    Description = $"방어 시설 부족 ({state.DefenseStructures}) - 샌드백 건설 권장"
                });
            }

            return actions;
        }

        public override void ExecuteAction(RimAIAction action)
        {
            if (action.TargetMap == null) return;

            // 실제 건설 로직
            // 현재는 로그만 출력하고, 향후 확장 시 실제 청사진 배치 구현
            Log.Message($"[RimAI-Construction] {action.Description}");

            // TODO: 실제 건설 구현
            // - 적절한 위치 찾기 (빈 공간, 접근 가능한 곳)
            // - GenConstruct.PlaceBlueprintForBuild() 사용
            // - 자원 확인

            // 쿨다운 설정
            if (action.Type == RimAIActionType.ConstructBuilding ||
                action.Type == RimAIActionType.PlaceBlueprint)
            {
                lastConstructionTick[action.TargetMap] = Find.TickManager.TicksGame;
            }
        }

        /// <summary>
        /// 건설 가능 여부 확인 (쿨다운)
        /// </summary>
        private bool CanConstruct(Map map)
        {
            if (!lastConstructionTick.TryGetValue(map, out var lastTick))
            {
                return true; // 처음 건설
            }

            int currentTick = Find.TickManager.TicksGame;
            return (currentTick - lastTick) >= CONSTRUCTION_COOLDOWN;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref lastConstructionTick, "lastConstructionTick");
        }

        public override string GetDebugInfo(Map map)
        {
            if (!mapStates.TryGetValue(map, out var state))
                return base.GetDebugInfo(map);

            var info = $"[{Name}] {state}\n";

            if (lastConstructionTick.TryGetValue(map, out var lastTick))
            {
                int ticksSinceLastConstruction = Find.TickManager.TicksGame - lastTick;
                int secondsRemaining = (CONSTRUCTION_COOLDOWN - ticksSinceLastConstruction) / 60;
                info += $"쿨다운: {secondsRemaining}초 남음\n";
            }

            return info;
        }
    }
}
