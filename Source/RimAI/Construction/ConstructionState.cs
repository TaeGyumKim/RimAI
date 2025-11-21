using System.Collections.Generic;
using Verse;

namespace RimAI.Construction
{
    /// <summary>
    /// 콜로니의 건설 상태
    /// </summary>
    public class ColonyConstructionState
    {
        // === 거주 구역 ===
        public int TotalBeds { get; set; }
        public int AssignedBeds { get; set; }
        public int ColonistCount { get; set; }

        // === 작업 인프라 ===
        public bool HasKitchen { get; set; }
        public bool HasWorkshop { get; set; }
        public bool HasResearchBench { get; set; }
        public bool HasStorageRoom { get; set; }

        // === 전력/조명 ===
        public bool HasPowerGenerator { get; set; }
        public int PowerGenerators { get; set; }
        public int StandingLamps { get; set; }
        public bool NeedMoreLighting { get; set; }

        // === 가구 ===
        public int Tables { get; set; }
        public int Chairs { get; set; }
        public bool HasDiningArea { get; set; }

        // === 방어 시설 ===
        public int DefenseStructures { get; set; } // 샌드백, 바리케이드 등
        public int Walls { get; set; }
        public int Doors { get; set; }
        public int Turrets { get; set; }

        // === 건설 작업 ===
        public int ActiveBlueprints { get; set; } // 현재 건설 중인 청사진 수
        public int ActiveConstructions { get; set; } // 건설 중인 건물 수

        // === 자원 ===
        public int AvailableWood { get; set; }
        public int AvailableSteel { get; set; }
        public int AvailableStone { get; set; }

        /// <summary>
        /// 침대가 부족한지 확인
        /// </summary>
        public bool NeedMoreBeds()
        {
            return TotalBeds < ColonistCount + 2; // 여유분 2개
        }

        /// <summary>
        /// 기본 인프라가 부족한지 확인
        /// </summary>
        public bool NeedBasicInfrastructure()
        {
            return !HasKitchen || !HasStorageRoom;
        }

        /// <summary>
        /// 전력 시설이 필요한지 확인
        /// </summary>
        public bool NeedPower()
        {
            return !HasPowerGenerator; // 항상 전력 필요
        }

        /// <summary>
        /// 연구대가 필요한지 확인
        /// </summary>
        public bool NeedResearchBench()
        {
            return !HasResearchBench; // 항상 연구대 필요
        }

        /// <summary>
        /// 식탁이 필요한지 확인
        /// </summary>
        public bool NeedDiningArea()
        {
            return !HasDiningArea; // 항상 식탁 필요
        }

        /// <summary>
        /// 조명이 필요한지 확인
        /// </summary>
        public bool NeedLighting()
        {
            return NeedMoreLighting; // 전력 없어도 조명 필요 (횃불 등)
        }

        /// <summary>
        /// 방어 시설이 부족한지 확인
        /// </summary>
        public bool NeedDefenses()
        {
            return DefenseStructures < ColonistCount * 3; // 콜로니스트당 3개의 방어 구조물
        }

        public override string ToString()
        {
            return $"[Construction] 침대:{TotalBeds}/{ColonistCount} | 청사진:{ActiveBlueprints} | " +
                   $"방어:{DefenseStructures} | 자원(목재:{AvailableWood}, 철:{AvailableSteel})";
        }
    }

    /// <summary>
    /// 건설 우선순위
    /// </summary>
    public enum ConstructionPriority
    {
        None = 0,
        Low = 1,
        Normal = 2,
        High = 3,
        Critical = 4
    }
}
