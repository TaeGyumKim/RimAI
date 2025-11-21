using RimWorld;
using Verse;

namespace RimAI.Core
{
    /// <summary>
    /// RimAI 액션 타입
    /// </summary>
    public enum RimAIActionType
    {
        /// <summary>식량 관련 작업 우선순위 조정</summary>
        AdjustFoodPriority,

        /// <summary>건설 청사진 배치</summary>
        PlaceBlueprint,

        /// <summary>건물 건설 지시</summary>
        ConstructBuilding,

        /// <summary>연구 프로젝트 선택</summary>
        SelectResearch,

        /// <summary>자원 할당</summary>
        AllocateResources,

        /// <summary>위기 대응</summary>
        EmergencyResponse,

        /// <summary>기타</summary>
        Other
    }

    /// <summary>
    /// RimAI 액션 우선순위
    /// </summary>
    public enum RimAIActionPriority
    {
        /// <summary>매우 낮음</summary>
        VeryLow = 0,

        /// <summary>낮음</summary>
        Low = 1,

        /// <summary>보통</summary>
        Normal = 2,

        /// <summary>높음</summary>
        High = 3,

        /// <summary>긴급</summary>
        Critical = 4
    }

    /// <summary>
    /// RimAI가 제안하는 액션
    /// 서브시스템이 생성하고 중앙 브레인이 승인/실행합니다.
    /// </summary>
    public class RimAIAction
    {
        /// <summary>액션 타입</summary>
        public RimAIActionType Type { get; set; }

        /// <summary>우선순위</summary>
        public RimAIActionPriority Priority { get; set; }

        /// <summary>제안한 서브시스템 이름</summary>
        public string SourceSubsystem { get; set; } = "";

        /// <summary>대상 맵</summary>
        public Map? TargetMap { get; set; }

        /// <summary>대상 위치 (건설/청사진용)</summary>
        public IntVec3? TargetCell { get; set; }

        /// <summary>대상 ThingDef (건설용)</summary>
        public ThingDef? TargetThingDef { get; set; }

        /// <summary>대상 ResearchProjectDef (연구용)</summary>
        public ResearchProjectDef? TargetResearch { get; set; }

        /// <summary>회전 정보 (건설용)</summary>
        public Rot4 Rotation { get; set; } = Rot4.North;

        /// <summary>추가 데이터 (서브시스템별 커스텀 데이터)</summary>
        public object? CustomData { get; set; }

        /// <summary>액션 설명 (디버그용)</summary>
        public string Description { get; set; } = "";

        /// <summary>예상 비용/자원</summary>
        public int EstimatedCost { get; set; } = 0;

        /// <summary>
        /// 액션이 실행 가능한지 검증
        /// </summary>
        public bool IsValid()
        {
            if (TargetMap == null) return false;

            switch (Type)
            {
                case RimAIActionType.PlaceBlueprint:
                case RimAIActionType.ConstructBuilding:
                    return TargetCell.HasValue && TargetThingDef != null;

                case RimAIActionType.SelectResearch:
                    return TargetResearch != null;

                default:
                    return true;
            }
        }

        /// <summary>
        /// 디버그 출력
        /// </summary>
        public override string ToString()
        {
            return $"[{SourceSubsystem}] {Type} - {Description} (우선순위: {Priority})";
        }
    }
}
