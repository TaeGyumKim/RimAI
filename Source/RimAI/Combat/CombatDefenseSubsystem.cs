using System.Collections.Generic;
using System.Linq;
using RimAI.Core;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimAI.Combat
{
    /// <summary>
    /// 전투/방어 자동화 서브시스템
    /// 레이드, 메카노이드, 동물 습격 등에 자동 대응합니다.
    /// </summary>
    public class CombatDefenseSubsystem : RimAISubsystemBase
    {
        // 맵별 위협 상태 캐시
        private Dictionary<Map, ColonyThreatState> mapThreatStates = new Dictionary<Map, ColonyThreatState>();

        // 맵별 방어 위치 캐시
        private Dictionary<Map, IntVec3> defensivePositions = new Dictionary<Map, IntVec3>();

        // 전투 중인 맵 추적
        private HashSet<Map> mapsInCombat = new HashSet<Map>();

        public override string Name => "Combat";
        public override int Priority => 150; // 최우선 (Food보다 높음)

        public CombatDefenseSubsystem()
        {
            updateInterval = 60; // 1초마다 업데이트 (전투는 빠른 반응 필요)
        }

        public override void Initialize()
        {
            base.Initialize();
            Enabled = RimAI_Settings.EnableCombatAutomation;
        }

        public override void Update(Map map)
        {
            if (!ShouldUpdate()) return;
            if (!Enabled) return;

            try
            {
                // 위협 상태 분석
                var state = ThreatAnalyzer.AnalyzeMap(map);

                // 전투 중 상태 업데이트
                UpdateCombatState(map, state);

                // 캐시 저장
                mapThreatStates[map] = state;

                // 디버그 로그
                if (Prefs.DevMode && RimAI_Settings.EnableDetailedLogging)
                {
                    if (state.CurrentThreatLevel != ThreatLevel.None)
                    {
                        Log.Message($"[RimAI-Combat] {state}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RimAI-Combat] 맵 {map} 업데이트 중 오류: {ex}");
            }
        }

        public override List<RimAIAction> GetProposedActions(Map map)
        {
            var actions = new List<RimAIAction>();

            if (!mapThreatStates.TryGetValue(map, out var state))
                return actions;

            // 위협 레벨에 따른 액션 제안
            switch (state.CurrentThreatLevel)
            {
                case ThreatLevel.None:
                    // 전투가 끝났으면 Draft 해제
                    if (mapsInCombat.Contains(map))
                    {
                        actions.Add(new RimAIAction
                        {
                            Type = RimAIActionType.Other,
                            Priority = RimAIActionPriority.High,
                            SourceSubsystem = Name,
                            TargetMap = map,
                            Description = "위협 해제 - 전투 종료, Draft 해제"
                        });
                    }
                    break;

                case ThreatLevel.Low:
                    // 경계 태세 - 무기 장비 확인
                    actions.Add(new RimAIAction
                    {
                        Type = RimAIActionType.Other,
                        Priority = RimAIActionPriority.Normal,
                        SourceSubsystem = Name,
                        TargetMap = map,
                        Description = $"경계: 소규모 위협 감지 ({state.TotalEnemies}명)"
                    });
                    break;

                case ThreatLevel.Medium:
                case ThreatLevel.High:
                case ThreatLevel.Critical:
                    // 전투 태세 - Draft 및 방어 위치 이동
                    actions.Add(new RimAIAction
                    {
                        Type = RimAIActionType.EmergencyResponse,
                        Priority = RimAIActionPriority.Critical,
                        SourceSubsystem = Name,
                        TargetMap = map,
                        TargetCell = state.ThreatCenter,
                        Description = $"{state.CurrentThreatLevel} 위협! 적 {state.TotalEnemies}명 - 전투 개시",
                        CustomData = state
                    });
                    break;
            }

            return actions;
        }

        public override void ExecuteAction(RimAIAction action)
        {
            if (action.TargetMap == null) return;

            var map = action.TargetMap;

            if (!mapThreatStates.TryGetValue(map, out var state))
                return;

            switch (state.CurrentThreatLevel)
            {
                case ThreatLevel.None:
                    // 전투 종료 - Draft 해제
                    EndCombat(map, state);
                    break;

                case ThreatLevel.Low:
                    // 경계 태세 - 로그만
                    Log.Message($"[RimAI-Combat] {action.Description}");
                    break;

                case ThreatLevel.Medium:
                case ThreatLevel.High:
                case ThreatLevel.Critical:
                    // 전투 개시
                    StartCombat(map, state);
                    break;
            }
        }

        /// <summary>
        /// 전투 시작
        /// </summary>
        private void StartCombat(Map map, ColonyThreatState state)
        {
            // 전투 중으로 표시
            if (!mapsInCombat.Contains(map))
            {
                mapsInCombat.Add(map);
                state.InCombat = true;
                state.CombatStartTick = Find.TickManager.TicksGame;

                Log.Warning($"[RimAI-Combat] 전투 시작! 위협 레벨: {state.CurrentThreatLevel}, 적: {state.TotalEnemies}명");

                // 카메라 이동 (전투 위치로)
                CinematicCameraHelper.FocusOnCombat(map, state.ThreatCenter);
            }

            // 전투 가능한 모든 폰 Draft
            DraftCombatPawns(map, state);

            // 방어 위치로 이동 (RimWorld의 기본 AI 활용)
            AssignDefensivePositions(map, state);
        }

        /// <summary>
        /// 전투 종료
        /// </summary>
        private void EndCombat(Map map, ColonyThreatState state)
        {
            if (!mapsInCombat.Contains(map)) return;

            mapsInCombat.Remove(map);
            state.InCombat = false;

            Log.Message($"[RimAI-Combat] 전투 종료! 지속시간: {state.CombatDurationSeconds:F1}초");

            // 모든 폰 Undraft
            UndraftAllPawns(map);

            // 카메라 줌 아웃
            CinematicCameraHelper.ResetCamera();
        }

        /// <summary>
        /// 전투 가능한 폰들 Draft
        /// </summary>
        private void DraftCombatPawns(Map map, ColonyThreatState state)
        {
            int draftedCount = 0;

            foreach (var pawn in state.CombatCapablePawns)
            {
                if (!pawn.Drafted)
                {
                    pawn.drafter.Drafted = true;
                    draftedCount++;
                }
            }

            if (draftedCount > 0)
            {
                Log.Message($"[RimAI-Combat] {draftedCount}명 Draft 완료");
            }
        }

        /// <summary>
        /// 모든 폰 Undraft
        /// </summary>
        private void UndraftAllPawns(Map map)
        {
            var colonists = map.mapPawns.FreeColonistsSpawned;
            int undraftedCount = 0;

            foreach (var pawn in colonists)
            {
                if (pawn.Drafted)
                {
                    pawn.drafter.Drafted = false;
                    undraftedCount++;
                }
            }

            if (undraftedCount > 0)
            {
                Log.Message($"[RimAI-Combat] {undraftedCount}명 Undraft 완료 - 일상 복귀");
            }
        }

        /// <summary>
        /// 방어 위치 할당 (RimWorld의 기본 전투 AI 활용)
        /// </summary>
        private void AssignDefensivePositions(Map map, ColonyThreatState state)
        {
            // RimWorld의 기본 전투 AI는 Draft 상태에서 자동으로 작동합니다.
            // 폰들은 자동으로 커버를 찾고 적을 공격합니다.
            //
            // 여기서는 "집결 위치"만 제안하고, 실제 전투는 vanilla AI에 맡깁니다.

            // 방어 위치 계산 (콜로니 중심과 위협 사이)
            var colonyCenter = CalculateColonyCenter(map);
            var defensivePos = CalculateDefensivePosition(colonyCenter, state.ThreatCenter);

            defensivePositions[map] = defensivePos;

            // Draft된 폰들을 방어 위치 근처로 이동 명령
            // (선택사항 - vanilla AI가 알아서 잘 하므로 생략 가능)
            // foreach (var pawn in state.CombatCapablePawns)
            // {
            //     if (pawn.Drafted)
            //     {
            //         Job job = JobMaker.MakeJob(JobDefOf.Goto, defensivePos);
            //         pawn.jobs.TryTakeOrderedJob(job);
            //     }
            // }
        }

        /// <summary>
        /// 콜로니 중심 위치 계산
        /// </summary>
        private IntVec3 CalculateColonyCenter(Map map)
        {
            var buildings = map.listerBuildings.allBuildingsColonist;

            if (!buildings.Any())
            {
                // 건물이 없으면 콜로니스트 위치 평균
                var colonists = map.mapPawns.FreeColonistsSpawned;
                if (colonists.Any())
                {
                    float avgX = colonists.Average(p => p.Position.x);
                    float avgZ = colonists.Average(p => p.Position.z);
                    return new IntVec3((int)avgX, 0, (int)avgZ);
                }

                return map.Center;
            }

            float x = buildings.Average(b => b.Position.x);
            float z = buildings.Average(b => b.Position.z);
            return new IntVec3((int)x, 0, (int)z);
        }

        /// <summary>
        /// 방어 위치 계산 (콜로니와 위협 사이)
        /// </summary>
        private IntVec3 CalculateDefensivePosition(IntVec3 colonyCenter, IntVec3 threatCenter)
        {
            // 콜로니 중심에서 위협 방향으로 30% 지점
            float t = 0.3f;
            int x = (int)(colonyCenter.x + (threatCenter.x - colonyCenter.x) * t);
            int z = (int)(colonyCenter.z + (threatCenter.z - colonyCenter.z) * t);

            return new IntVec3(x, 0, z);
        }

        /// <summary>
        /// 전투 상태 업데이트
        /// </summary>
        private void UpdateCombatState(Map map, ColonyThreatState state)
        {
            // 이전 상태와 비교
            if (mapThreatStates.TryGetValue(map, out var prevState))
            {
                // 전투 시작 틱 유지
                if (prevState.InCombat)
                {
                    state.InCombat = true;
                    state.CombatStartTick = prevState.CombatStartTick;
                }

                // 마지막 적 발견 틱 유지
                if (state.TotalEnemies == 0 && prevState.LastEnemySeenTick > 0)
                {
                    state.LastEnemySeenTick = prevState.LastEnemySeenTick;
                }
            }
        }

        /// <summary>
        /// 특정 맵이 전투 중인지 확인
        /// </summary>
        public bool IsInCombat(Map map)
        {
            return mapsInCombat.Contains(map);
        }

        /// <summary>
        /// 위협 상태 가져오기
        /// </summary>
        public ColonyThreatState? GetThreatState(Map map)
        {
            if (mapThreatStates.TryGetValue(map, out var state))
                return state;
            return null;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // mapsInCombat는 휘발성이므로 저장하지 않음
        }

        public override string GetDebugInfo(Map map)
        {
            if (!mapThreatStates.TryGetValue(map, out var state))
                return base.GetDebugInfo(map);

            var info = $"[{Name}] {state}\n";

            if (state.InCombat)
            {
                info += $"전투 지속 시간: {state.CombatDurationSeconds:F1}초\n";
            }

            if (defensivePositions.TryGetValue(map, out var defPos))
            {
                info += $"방어 위치: {defPos}\n";
            }

            return info;
        }
    }
}
