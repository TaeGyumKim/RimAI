using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimAI.Food
{
    /// <summary>
    /// 콜로니의 식량 상태를 나타내는 데이터 클래스
    /// </summary>
    public class ColonyFoodState
    {
        // === 식량 저장량 ===

        /// <summary>현재 저장된 총 영양분 (nutrition)</summary>
        public float TotalNutrition { get; set; }

        /// <summary>생고기 영양분</summary>
        public float RawFoodNutrition { get; set; }

        /// <summary>조리된 식사 영양분</summary>
        public float MealNutrition { get; set; }

        /// <summary>농작물 영양분 (수확 가능한 것)</summary>
        public float CropNutrition { get; set; }

        // === 소비 정보 ===

        /// <summary>콜로니스트 수</summary>
        public int ColonistCount { get; set; }

        /// <summary>동물 수 (식량 소비하는)</summary>
        public int AnimalCount { get; set; }

        /// <summary>하루 예상 소비 영양분</summary>
        public float DailyConsumption { get; set; }

        /// <summary>현재 저장량으로 버틸 수 있는 일수</summary>
        public float DaysUntilStarvation { get; set; }

        // === 농사 정보 ===

        /// <summary>농장 구역 목록</summary>
        public List<Zone_Growing> GrowingZones { get; set; } = new List<Zone_Growing>();

        /// <summary>총 농장 타일 수</summary>
        public int TotalFarmTiles { get; set; }

        /// <summary>심어진 작물 타일 수</summary>
        public int SownTiles { get; set; }

        /// <summary>빈 농장 타일 수 (파종 가능)</summary>
        public int EmptyFarmTiles { get; set; }

        /// <summary>수확 가능한 작물 수</summary>
        public int HarvestableCrops { get; set; }

        // === 환경 정보 ===

        /// <summary>현재 계절</summary>
        public Season CurrentSeason { get; set; }

        /// <summary>현재 온도 (섭씨)</summary>
        public float Temperature { get; set; }

        /// <summary>농사 가능 여부 (계절/온도 기반)</summary>
        public bool CanGrow { get; set; }

        /// <summary>겨울까지 남은 일수</summary>
        public int DaysUntilWinter { get; set; }

        // === 작업 상태 ===

        /// <summary>현재 파종 중인 폰 수</summary>
        public int PawnsSowing { get; set; }

        /// <summary>현재 수확 중인 폰 수</summary>
        public int PawnsHarvesting { get; set; }

        /// <summary>현재 요리 중인 폰 수</summary>
        public int PawnsCooking { get; set; }

        /// <summary>현재 사냥 중인 폰 수</summary>
        public int PawnsHunting { get; set; }

        /// <summary>
        /// 식량 위기 수준을 계산
        /// </summary>
        /// <returns>0=안전, 1=주의, 2=경고, 3=위기</returns>
        public int GetCrisisLevel()
        {
            if (DaysUntilStarvation < 1.0f) return 3; // 위기: 1일 미만
            if (DaysUntilStarvation < 3.0f) return 2; // 경고: 3일 미만
            if (DaysUntilStarvation < 7.0f) return 1; // 주의: 7일 미만
            return 0; // 안전: 7일 이상
        }

        /// <summary>
        /// 농장 활용률 계산 (0.0 ~ 1.0)
        /// </summary>
        public float GetFarmUtilization()
        {
            if (TotalFarmTiles == 0) return 0f;
            return (float)SownTiles / TotalFarmTiles;
        }

        /// <summary>
        /// 디버그용 문자열 출력
        /// </summary>
        public override string ToString()
        {
            return $"[FoodState] 영양분:{TotalNutrition:F1} | 소비:{DailyConsumption:F1}/day | " +
                   $"버틸일:{DaysUntilStarvation:F1}일 | 위기레벨:{GetCrisisLevel()} | " +
                   $"농장:{SownTiles}/{TotalFarmTiles} | 수확가능:{HarvestableCrops}";
        }
    }

    /// <summary>
    /// 식량 관련 작업 우선순위
    /// </summary>
    public enum FoodWorkPriority
    {
        /// <summary>작업 필요 없음</summary>
        None = 0,

        /// <summary>낮은 우선순위</summary>
        Low = 1,

        /// <summary>보통 우선순위</summary>
        Normal = 2,

        /// <summary>높은 우선순위</summary>
        High = 3,

        /// <summary>최우선 - 생존 필수</summary>
        Critical = 4
    }

    /// <summary>
    /// 의사결정 결과 - 어떤 작업을 어떤 우선순위로 해야 하는지
    /// </summary>
    public class FoodDecision
    {
        /// <summary>파종 우선순위</summary>
        public FoodWorkPriority SowingPriority { get; set; }

        /// <summary>수확 우선순위</summary>
        public FoodWorkPriority HarvestingPriority { get; set; }

        /// <summary>요리 우선순위</summary>
        public FoodWorkPriority CookingPriority { get; set; }

        /// <summary>사냥 우선순위</summary>
        public FoodWorkPriority HuntingPriority { get; set; }

        /// <summary>채집 우선순위</summary>
        public FoodWorkPriority ForagingPriority { get; set; }

        /// <summary>의사결정 이유 (디버그용)</summary>
        public string Reasoning { get; set; } = "";

        /// <summary>
        /// 디버그용 출력
        /// </summary>
        public override string ToString()
        {
            return $"[Decision] 파종:{SowingPriority} | 수확:{HarvestingPriority} | " +
                   $"요리:{CookingPriority} | 사냥:{HuntingPriority} | 채집:{ForagingPriority}\n" +
                   $"이유: {Reasoning}";
        }
    }
}
