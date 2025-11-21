using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimAI.Production
{
    /// <summary>
    /// 콜로니의 생산 상태
    /// </summary>
    public class ColonyProductionState
    {
        // === 작업대 정보 ===
        public List<WorkTableInfo> WorkTables { get; set; } = new List<WorkTableInfo>();

        // === 아이템 재고 (범용) ===
        public Dictionary<ThingDef, int> ItemInventory { get; set; } = new Dictionary<ThingDef, int>();

        // === 무기 재고 ===
        public int MeleeWeapons { get; set; }
        public int RangedWeapons { get; set; }
        public int ColonistsWithWeapons { get; set; }

        // === 방어구 재고 ===
        public int Armors { get; set; }
        public int ColonistsWithArmor { get; set; }

        // === 의류 재고 ===
        public int WinterClothes { get; set; }
        public int SummerClothes { get; set; }
        public int TatteredClothes { get; set; } // 낡은 옷 (50% 이하 내구도)

        // === 자원 재고 ===
        public int AvailableSteel { get; set; }
        public int AvailableComponents { get; set; }
        public int AvailableCloth { get; set; }
        public int AvailableLeather { get; set; }
        public int Medicine { get; set; }

        // === 시즌 정보 ===
        public Season CurrentSeason { get; set; }
        public int DaysUntilWinter { get; set; }
        public bool IsWinterSoon => DaysUntilWinter < 30 && CurrentSeason != Season.Winter;

        // === 콜로니스트 정보 ===
        public int ColonistCount { get; set; }

        // === 헬퍼 메서드 ===

        /// <summary>
        /// 무기 부족 여부 (콜로니스트당 1개 이상 필요)
        /// </summary>
        public bool NeedMoreWeapons()
        {
            return ColonistsWithWeapons < ColonistCount;
        }

        /// <summary>
        /// 방어구 부족 여부 (절반 이상이 갑옷 필요)
        /// </summary>
        public bool NeedMoreArmor()
        {
            return ColonistsWithArmor < ColonistCount / 2;
        }

        /// <summary>
        /// 겨울 준비 필요 여부
        /// </summary>
        public bool NeedWinterPreparation()
        {
            return IsWinterSoon && WinterClothes < ColonistCount;
        }

        /// <summary>
        /// 자원 부족 여부 (강철 < 100 또는 컴포넌트 < 5)
        /// </summary>
        public bool IsResourceScarce()
        {
            return AvailableSteel < 100 || AvailableComponents < 5;
        }

        /// <summary>
        /// 자원 여유 여부 (강철 > 500, 컴포넌트 > 20)
        /// </summary>
        public bool HasResourceSurplus()
        {
            return AvailableSteel > 500 && AvailableComponents > 20;
        }

        /// <summary>
        /// 특정 작업대가 있는지 확인
        /// </summary>
        public bool HasWorkTable(string defNameContains)
        {
            return WorkTables.Any(wt => wt.DefName.Contains(defNameContains));
        }

        /// <summary>
        /// 특정 작업대 찾기
        /// </summary>
        public WorkTableInfo? FindWorkTable(string defNameContains)
        {
            return WorkTables.FirstOrDefault(wt => wt.DefName.Contains(defNameContains));
        }

        public override string ToString()
        {
            return $"[Production] 작업대:{WorkTables.Count} | 무기:{ColonistsWithWeapons}/{ColonistCount} | " +
                   $"갑옷:{ColonistsWithArmor}/{ColonistCount} | 겨울옷:{WinterClothes} | " +
                   $"자원(강철:{AvailableSteel}, 부품:{AvailableComponents})";
        }
    }

    /// <summary>
    /// 작업대 정보
    /// </summary>
    public class WorkTableInfo
    {
        /// <summary>작업대 건물</summary>
        public Building_WorkTable Building { get; set; }

        /// <summary>DefName (예: TableMachining)</summary>
        public string DefName { get; set; } = "";

        /// <summary>현재 Bill 목록</summary>
        public List<Bill> Bills { get; set; } = new List<Bill>();

        /// <summary>전력 필요 여부 및 작동 중인지</summary>
        public bool IsPowered { get; set; } = true;

        /// <summary>대기 중인 작업 수</summary>
        public int QueuedWork { get; set; }

        /// <summary>
        /// 특정 레시피의 Bill이 있는지 확인
        /// </summary>
        public bool HasBillForRecipe(RecipeDef recipe)
        {
            return Bills.Any(b => b.recipe == recipe);
        }

        /// <summary>
        /// 특정 레시피의 Bill 찾기
        /// </summary>
        public Bill? FindBill(RecipeDef recipe)
        {
            return Bills.FirstOrDefault(b => b.recipe == recipe);
        }

        public override string ToString()
        {
            return $"{DefName} (Bills:{Bills.Count}, Powered:{IsPowered}, Queue:{QueuedWork})";
        }
    }

    /// <summary>
    /// 생산 액션 커스텀 데이터
    /// RimAIAction.CustomData에 저장하여 사용
    /// </summary>
    public class ProductionActionData
    {
        /// <summary>대상 작업대</summary>
        public Building_WorkTable? WorkTable { get; set; }

        /// <summary>레시피 Def</summary>
        public RecipeDef? Recipe { get; set; }

        /// <summary>목표 수량 (-1 = 무한 반복)</summary>
        public int TargetCount { get; set; } = -1;

        /// <summary>일시 중단 여부</summary>
        public bool Paused { get; set; } = false;

        /// <summary>기존 Bill (수정/삭제 시)</summary>
        public Bill? ExistingBill { get; set; }

        /// <summary>품질 요구사항 (의류/무기용)</summary>
        public QualityCategory MinQuality { get; set; } = QualityCategory.Normal;

        /// <summary>재질 요구사항 (강철, 천 등)</summary>
        public ThingDef? StuffDef { get; set; }

        /// <summary>반복 모드</summary>
        public BillRepeatModeDef? RepeatMode { get; set; }

        /// <summary>
        /// Bill 설명 (디버그용)
        /// </summary>
        public string GetDescription()
        {
            string desc = Recipe?.label ?? "알 수 없는 레시피";
            if (TargetCount > 0)
                desc += $" x{TargetCount}";
            if (Paused)
                desc += " (일시중단)";
            return desc;
        }
    }

    /// <summary>
    /// 생산 우선순위
    /// </summary>
    public enum ProductionPriority
    {
        /// <summary>없음</summary>
        None = 0,

        /// <summary>낮음 (편의, 품질 개선)</summary>
        Low = 1,

        /// <summary>보통 (의류 교체, 일반 생산)</summary>
        Normal = 2,

        /// <summary>높음 (무기, 갑옷, 겨울 준비)</summary>
        High = 3,

        /// <summary>긴급 (생존 직결)</summary>
        Critical = 4
    }
}
