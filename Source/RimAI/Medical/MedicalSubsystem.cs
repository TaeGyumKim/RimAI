using System.Collections.Generic;
using System.Linq;
using RimAI.Core;
using RimWorld;
using Verse;

namespace RimAI.Medical
{
    /// <summary>
    /// 의료 자동화 서브시스템
    /// 콜로니의 의료 상태를 분석하고 치료 우선순위를 조정합니다.
    ///
    /// 주요 기능:
    /// - 부상자/환자 자동 감지 및 우선순위 지정
    /// - 의료 침대 자동 확보 권장
    /// - 의약품 재고 관리
    /// - 면역 경쟁 모니터링
    /// </summary>
    public class MedicalSubsystem : RimAISubsystemBase
    {
        // 맵별 의사결정 캐시
        private Dictionary<Map, MedicalDecision> mapDecisions = new Dictionary<Map, MedicalDecision>();
        private Dictionary<Map, ColonyMedicalState> mapStates = new Dictionary<Map, ColonyMedicalState>();

        public override string Name => "Medical";
        public override int Priority => 95; // 식량 다음으로 높은 우선순위 (생존 필수)

        public MedicalSubsystem()
        {
            baseUpdateInterval = 180; // 3초마다 업데이트 (의료는 더 빠른 반응 필요)
        }

        public override void Initialize()
        {
            base.Initialize();
            LogInfo("의료 서브시스템 초기화 완료");
        }

        public override void Update(Map map)
        {
            if (!ShouldUpdate()) return;
            if (!Enabled) return;

            try
            {
                // 1. 상태 분석
                var state = MedicalAnalyzer.AnalyzeMap(map);
                mapStates[map] = state;

                // 2. 의사결정
                var decision = MedicalDecisionEngine.MakeDecision(state);
                mapDecisions[map] = decision;

                // 3. 디버그 로그
                LogDetailed($"{state}");
                LogDetailed($"{decision}");
            }
            catch (System.Exception ex)
            {
                LogError($"맵 {map} 업데이트 중 오류: {ex}");
            }
        }

        public override List<RimAIAction> GetProposedActions(Map map)
        {
            var actions = new List<RimAIAction>();

            if (!mapDecisions.TryGetValue(map, out var decision))
                return actions;

            if (!mapStates.TryGetValue(map, out var state))
                return actions;

            // 위기 상황에서는 긴급 액션 생성
            if (decision.TreatmentPriority == MedicalWorkPriority.Critical)
            {
                actions.Add(new RimAIAction
                {
                    Type = RimAIActionType.EmergencyResponse,
                    Priority = RimAIActionPriority.Critical,
                    SourceSubsystem = Name,
                    TargetMap = map,
                    Description = $"의료 위기! 중상:{state.CriticalPawns} 출혈:{state.BleedingPawns}"
                });
            }

            // 침대 부족 시 건설 액션 제안
            if (decision.AdditionalBedsNeeded > 0)
            {
                actions.Add(new RimAIAction
                {
                    Type = RimAIActionType.Construction,
                    Priority = decision.BedPriority == MedicalWorkPriority.High
                        ? RimAIActionPriority.High
                        : RimAIActionPriority.Normal,
                    SourceSubsystem = Name,
                    TargetMap = map,
                    Description = $"의료 침대 {decision.AdditionalBedsNeeded}개 건설 필요"
                });
            }

            // 의약품 생산 필요 시
            if (decision.MedicineProductionPriority >= MedicalWorkPriority.High)
            {
                actions.Add(new RimAIAction
                {
                    Type = RimAIActionType.Production,
                    Priority = RimAIActionPriority.High,
                    SourceSubsystem = Name,
                    TargetMap = map,
                    Description = $"의약품 생산 필요 (현재: {state.TotalMedicine}개)"
                });
            }

            // 약초 재배 권장 시
            if (decision.ShouldGrowHerbs)
            {
                actions.Add(new RimAIAction
                {
                    Type = RimAIActionType.Farming,
                    Priority = RimAIActionPriority.Normal,
                    SourceSubsystem = Name,
                    TargetMap = map,
                    Description = "약초 재배 권장"
                });
            }

            return actions;
        }

        public override void ExecuteAction(RimAIAction action)
        {
            // 의료 서브시스템은 주로 정보 제공 및 우선순위 조정 역할
            // 실제 치료는 RimWorld 기본 시스템이 처리
            if (action.Type == RimAIActionType.EmergencyResponse)
            {
                Log.Warning($"[RimAI-Medical] {action.Description}");
            }
            else
            {
                LogInfo(action.Description);
            }
        }

        /// <summary>
        /// 특정 맵의 현재 의사결정 가져오기 (외부 접근용)
        /// </summary>
        public MedicalDecision? GetDecision(Map map)
        {
            if (mapDecisions.TryGetValue(map, out var decision))
                return decision;
            return null;
        }

        /// <summary>
        /// 특정 맵의 현재 의료 상태 가져오기 (외부 접근용)
        /// </summary>
        public ColonyMedicalState? GetState(Map map)
        {
            if (mapStates.TryGetValue(map, out var state))
                return state;
            return null;
        }

        /// <summary>
        /// 글리터월드 의약품 사용 권장 여부
        /// </summary>
        public bool ShouldUseGlitterworldMedicine(Map map)
        {
            if (mapDecisions.TryGetValue(map, out var decision))
                return decision.UseGlitterworldMedicine;
            return false;
        }

        /// <summary>
        /// 특정 폰의 치료 우선순위 가져오기
        /// </summary>
        public int GetTreatmentPriority(Map map, Pawn pawn)
        {
            if (!mapStates.TryGetValue(map, out var state))
                return 0;

            // 출혈/중상 환자 최우선
            if (pawn.health?.hediffSet?.hediffs?.Any(h => h.Bleeding) == true)
                return 100;

            // 감염 환자 높은 우선순위
            if (pawn.health?.hediffSet?.hediffs?.Any(h => h.def == RimWorld.HediffDefOf.WoundInfection) == true)
                return 80;

            // 면역 경쟁 중인 환자
            var immunityRace = state.ImmunityRaces.FirstOrDefault(r => r.Patient == pawn);
            if (immunityRace != null && immunityRace.AtRisk)
                return 90;

            // 일반 치료 필요 환자
            if (pawn.health?.hediffSet?.hediffs?.Any(h => h.TendableNow()) == true)
                return 50;

            return 0;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // 필요시 상태 저장/로드 로직 추가
        }

        public override string GetDebugInfo(Map map)
        {
            if (!mapStates.TryGetValue(map, out var state))
                return base.GetDebugInfo(map);

            return $"[{Name}] {state}";
        }

        /// <summary>
        /// 맵 데이터 정리 (메모리 누수 방지)
        /// </summary>
        public override void CleanupMap(Map map)
        {
            if (map == null) return;

            mapDecisions.Remove(map);
            mapStates.Remove(map);

            LogInfo($"맵 {map.Index} 데이터 정리 완료");
        }
    }
}
