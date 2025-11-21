using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimAI.Trading
{
    /// <summary>
    /// 콜로니의 무역 상태를 나타내는 데이터 클래스
    /// </summary>
    public class ColonyTradingState
    {
        // === 현재 상인 정보 ===

        /// <summary>맵에 있는 상인 캐러밴 목록</summary>
        public List<Pawn> VisitingTraders { get; set; } = new List<Pawn>();

        /// <summary>상인이 방문 중인지 여부</summary>
        public bool HasVisitingTrader => VisitingTraders.Count > 0;

        /// <summary>궤도 무역선 목록</summary>
        public List<TradeShip> OrbitalTraders { get; set; } = new List<TradeShip>();

        /// <summary>궤도 무역선 있는지 여부</summary>
        public bool HasOrbitalTrader => OrbitalTraders.Count > 0;

        // === 자원 상태 ===

        /// <summary>보유 실버</summary>
        public int TotalSilver { get; set; }

        /// <summary>판매 가능 품목 (초과 자원)</summary>
        public List<ThingCount> SellableItems { get; set; } = new List<ThingCount>();

        /// <summary>판매 가능 품목 총 예상 가치</summary>
        public float TotalSellableValue { get; set; }

        /// <summary>필요 품목 목록 (부족 자원)</summary>
        public List<ThingDefCount> NeededItems { get; set; } = new List<ThingDefCount>();

        // === 자원 부족 여부 ===

        /// <summary>강철 부족</summary>
        public bool SteelShortage { get; set; }

        /// <summary>컴포넌트 부족</summary>
        public bool ComponentShortage { get; set; }

        /// <summary>의약품 부족</summary>
        public bool MedicineShortage { get; set; }

        /// <summary>음식 부족</summary>
        public bool FoodShortage { get; set; }

        // === 자원 초과 여부 ===

        /// <summary>강철 초과</summary>
        public bool SteelSurplus { get; set; }

        /// <summary>가죽 초과</summary>
        public bool LeatherSurplus { get; set; }

        /// <summary>천 초과</summary>
        public bool ClothSurplus { get; set; }

        /// <summary>약 초과</summary>
        public bool DrugSurplus { get; set; }

        // === 카라반 정보 ===

        /// <summary>출발 가능한 콜로니스트 수</summary>
        public int AvailableColonistsForCaravan { get; set; }

        /// <summary>카라반용 동물 수</summary>
        public int PackAnimals { get; set; }

        /// <summary>가까운 정착지 목록</summary>
        public List<Settlement> NearbySettlements { get; set; } = new List<Settlement>();

        // === 퀘스트 정보 ===

        /// <summary>활성 퀘스트 수</summary>
        public int ActiveQuestCount { get; set; }

        /// <summary>엔딩 관련 퀘스트 진행 중 여부</summary>
        public bool HasEndgameQuest { get; set; }

        /// <summary>우주선 건설 진행도</summary>
        public float ShipProgress { get; set; }

        /// <summary>
        /// 무역 위기 수준을 계산
        /// </summary>
        /// <returns>0=안정, 1=기회, 2=권장, 3=필수</returns>
        public int GetTradingPriority()
        {
            // 자원 부족이 심각하고 상인이 있으면 필수
            if (HasVisitingTrader || HasOrbitalTrader)
            {
                int shortageCount = 0;
                if (SteelShortage) shortageCount++;
                if (ComponentShortage) shortageCount++;
                if (MedicineShortage) shortageCount++;
                if (FoodShortage) shortageCount++;

                if (shortageCount >= 2)
                    return 3; // 필수

                if (shortageCount >= 1)
                    return 2; // 권장
            }

            // 상인이 있고 판매 가능 물품이 많으면 기회
            if ((HasVisitingTrader || HasOrbitalTrader) && TotalSellableValue > 1000)
                return 1;

            return 0;
        }

        /// <summary>
        /// 디버그용 문자열 출력
        /// </summary>
        public override string ToString()
        {
            return $"[TradingState] 상인:{(HasVisitingTrader ? "방문중" : "없음")} " +
                   $"궤도선:{(HasOrbitalTrader ? "있음" : "없음")} | " +
                   $"실버:{TotalSilver} | 판매가치:{TotalSellableValue:F0} | " +
                   $"부족:[S:{SteelShortage} C:{ComponentShortage} M:{MedicineShortage}]";
        }
    }

    /// <summary>
    /// 아이템 수량 정보
    /// </summary>
    public class ThingCount
    {
        public Thing Thing { get; set; }
        public int Count { get; set; }
        public float MarketValue { get; set; }

        public ThingCount(Thing thing, int count)
        {
            Thing = thing;
            Count = count;
            MarketValue = thing.def.BaseMarketValue * count;
        }
    }

    /// <summary>
    /// 필요 아이템 정의
    /// </summary>
    public class ThingDefCount
    {
        public ThingDef ThingDef { get; set; }
        public int NeededCount { get; set; }
        public int CurrentCount { get; set; }
        public TradingPriority Priority { get; set; }

        public int Shortage => NeededCount - CurrentCount;
    }

    /// <summary>
    /// 무역 관련 작업 우선순위
    /// </summary>
    public enum TradingPriority
    {
        /// <summary>무역 필요 없음</summary>
        None = 0,

        /// <summary>여유 있을 때 무역</summary>
        Low = 1,

        /// <summary>무역 권장</summary>
        Normal = 2,

        /// <summary>무역 필요</summary>
        High = 3,

        /// <summary>무역 필수</summary>
        Critical = 4
    }

    /// <summary>
    /// 무역 의사결정 결과
    /// </summary>
    public class TradingDecision
    {
        /// <summary>무역 실행 우선순위</summary>
        public TradingPriority TradePriority { get; set; }

        /// <summary>구매 추천 품목</summary>
        public List<ThingDefCount> RecommendedPurchases { get; set; } = new List<ThingDefCount>();

        /// <summary>판매 추천 품목</summary>
        public List<ThingCount> RecommendedSales { get; set; } = new List<ThingCount>();

        /// <summary>카라반 파견 권장 여부</summary>
        public bool ShouldSendCaravan { get; set; }

        /// <summary>카라반 목적지</summary>
        public Settlement CaravanDestination { get; set; }

        /// <summary>의사결정 이유</summary>
        public string Reasoning { get; set; } = "";

        public override string ToString()
        {
            return $"[TradingDecision] 우선순위:{TradePriority} | " +
                   $"구매:{RecommendedPurchases.Count}종 | 판매:{RecommendedSales.Count}종 | " +
                   $"카라반:{(ShouldSendCaravan ? "권장" : "불필요")}\n" +
                   $"이유: {Reasoning}";
        }
    }
}
