using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimAI.Medical
{
    /// <summary>
    /// 콜로니의 의료 상태를 나타내는 데이터 클래스
    /// </summary>
    public class ColonyMedicalState
    {
        // === 환자 정보 ===

        /// <summary>치료가 필요한 폰 목록</summary>
        public List<Pawn> PawnsNeedingTreatment { get; set; } = new List<Pawn>();

        /// <summary>치료 대기 중인 폰 수</summary>
        public int PatientsWaiting { get; set; }

        /// <summary>현재 치료 받는 중인 폰 수</summary>
        public int PatientsBeingTreated { get; set; }

        /// <summary>감염된 폰 수</summary>
        public int InfectedPawns { get; set; }

        /// <summary>출혈 중인 폰 수</summary>
        public int BleedingPawns { get; set; }

        /// <summary>중상 폰 수 (즉시 치료 필요)</summary>
        public int CriticalPawns { get; set; }

        /// <summary>경상 폰 수</summary>
        public int MinorInjuredPawns { get; set; }

        /// <summary>질병 걸린 폰 수</summary>
        public int DiseasedPawns { get; set; }

        // === 의료 시설 ===

        /// <summary>총 의료 침대 수</summary>
        public int TotalMedicalBeds { get; set; }

        /// <summary>사용 가능한 의료 침대 수</summary>
        public int AvailableMedicalBeds { get; set; }

        /// <summary>사용 중인 의료 침대 수</summary>
        public int OccupiedMedicalBeds { get; set; }

        /// <summary>침대 부족 여부</summary>
        public bool BedShortage { get; set; }

        // === 의약품 정보 ===

        /// <summary>총 의약품 수 (모든 종류)</summary>
        public int TotalMedicine { get; set; }

        /// <summary>약초 (Herbal Medicine)</summary>
        public int HerbalMedicine { get; set; }

        /// <summary>일반 의약품 (Medicine)</summary>
        public int StandardMedicine { get; set; }

        /// <summary>글리터월드 의약품 (GlitterworldMedicine)</summary>
        public int GlitterworldMedicine { get; set; }

        /// <summary>의약품 부족 여부</summary>
        public bool MedicineShortage { get; set; }

        /// <summary>콜로니스트당 의약품 비율</summary>
        public float MedicinePerColonist { get; set; }

        // === 의료 인력 ===

        /// <summary>의료 작업 가능한 폰 수</summary>
        public int CapableDoctors { get; set; }

        /// <summary>숙련 의사 수 (의료 스킬 10 이상)</summary>
        public int SkilledDoctors { get; set; }

        /// <summary>현재 치료 중인 의사 수</summary>
        public int DoctorsWorking { get; set; }

        /// <summary>총 콜로니스트 수</summary>
        public int TotalColonists { get; set; }

        // === 면역 현황 ===

        /// <summary>면역 진행 중인 폰 목록 (질병 vs 면역 경쟁)</summary>
        public List<ImmunityRace> ImmunityRaces { get; set; } = new List<ImmunityRace>();

        /// <summary>면역 경쟁에서 위험한 폰 수</summary>
        public int ImmunityAtRisk { get; set; }

        /// <summary>
        /// 의료 위기 수준을 계산
        /// </summary>
        /// <returns>0=안전, 1=주의, 2=경고, 3=위기</returns>
        public int GetCrisisLevel()
        {
            // 중상자/출혈 환자가 있으면 위기
            if (CriticalPawns > 0 || BleedingPawns > 0)
                return 3;

            // 면역 경쟁에서 위험한 폰이 있으면 경고
            if (ImmunityAtRisk > 0)
                return 2;

            // 감염자가 많으면 주의
            if (InfectedPawns >= TotalColonists * 0.3f)
                return 2;

            // 치료 대기 환자가 많으면 주의
            if (PatientsWaiting >= 3)
                return 1;

            // 의약품 부족 시 주의
            if (MedicinePerColonist < 3)
                return 1;

            return 0; // 안전
        }

        /// <summary>
        /// 의료 침대 활용률 계산 (0.0 ~ 1.0)
        /// </summary>
        public float GetBedUtilization()
        {
            if (TotalMedicalBeds == 0) return 0f;
            return (float)OccupiedMedicalBeds / TotalMedicalBeds;
        }

        /// <summary>
        /// 의료 인력 충분 여부 (환자 대비)
        /// </summary>
        public bool HasEnoughDoctors()
        {
            int totalPatients = PatientsWaiting + PatientsBeingTreated;
            return CapableDoctors >= totalPatients || CapableDoctors >= 2;
        }

        /// <summary>
        /// 디버그용 문자열 출력
        /// </summary>
        public override string ToString()
        {
            return $"[MedicalState] 환자:{PatientsWaiting}대기/{PatientsBeingTreated}치료중 | " +
                   $"중상:{CriticalPawns} 출혈:{BleedingPawns} 감염:{InfectedPawns} | " +
                   $"침대:{AvailableMedicalBeds}/{TotalMedicalBeds} | " +
                   $"의약품:{TotalMedicine}(인당 {MedicinePerColonist:F1}) | " +
                   $"의사:{DoctorsWorking}/{CapableDoctors}";
        }
    }

    /// <summary>
    /// 면역 경쟁 정보 (질병 진행 vs 면역 획득)
    /// </summary>
    public class ImmunityRace
    {
        /// <summary>환자 폰</summary>
        public Pawn Patient { get; set; }

        /// <summary>질병 종류</summary>
        public HediffDef Disease { get; set; }

        /// <summary>질병 진행도 (0.0 ~ 1.0)</summary>
        public float DiseaseSeverity { get; set; }

        /// <summary>면역력 (0.0 ~ 1.0)</summary>
        public float Immunity { get; set; }

        /// <summary>면역이 질병을 앞서는지 여부</summary>
        public bool IsWinning => Immunity > DiseaseSeverity;

        /// <summary>위험 상황 (면역이 질병보다 뒤처짐)</summary>
        public bool AtRisk => DiseaseSeverity - Immunity > 0.1f;

        /// <summary>예상 결과 (이길 확률)</summary>
        public float WinProbability
        {
            get
            {
                float diff = Immunity - DiseaseSeverity;
                // 간단한 추정: 면역이 5% 앞서면 90% 승리, 5% 뒤처지면 50% 승리
                if (diff >= 0.1f) return 0.95f;
                if (diff >= 0.05f) return 0.90f;
                if (diff >= 0f) return 0.75f;
                if (diff >= -0.05f) return 0.50f;
                if (diff >= -0.1f) return 0.30f;
                return 0.10f;
            }
        }

        public override string ToString()
        {
            return $"{Patient?.Name?.ToStringShort ?? "?"}: {Disease?.label ?? "?"} " +
                   $"(질병:{DiseaseSeverity:P0} vs 면역:{Immunity:P0}, {(IsWinning ? "이기는 중" : "지는 중")})";
        }
    }

    /// <summary>
    /// 의료 관련 작업 우선순위
    /// </summary>
    public enum MedicalWorkPriority
    {
        /// <summary>작업 필요 없음</summary>
        None = 0,

        /// <summary>낮은 우선순위</summary>
        Low = 1,

        /// <summary>보통 우선순위</summary>
        Normal = 2,

        /// <summary>높은 우선순위</summary>
        High = 3,

        /// <summary>최우선 - 생명 위험</summary>
        Critical = 4
    }

    /// <summary>
    /// 의사결정 결과 - 어떤 의료 작업을 어떤 우선순위로 해야 하는지
    /// </summary>
    public class MedicalDecision
    {
        /// <summary>부상자 치료 우선순위</summary>
        public MedicalWorkPriority TreatmentPriority { get; set; }

        /// <summary>병상 확보 우선순위</summary>
        public MedicalWorkPriority BedPriority { get; set; }

        /// <summary>의약품 생산 우선순위</summary>
        public MedicalWorkPriority MedicineProductionPriority { get; set; }

        /// <summary>의료 연구 우선순위</summary>
        public MedicalWorkPriority MedicalResearchPriority { get; set; }

        /// <summary>글리터월드 의약품 사용 권장 여부</summary>
        public bool UseGlitterworldMedicine { get; set; }

        /// <summary>약초 재배 권장 여부</summary>
        public bool ShouldGrowHerbs { get; set; }

        /// <summary>추가 의료 침대 필요 수</summary>
        public int AdditionalBedsNeeded { get; set; }

        /// <summary>의사결정 이유 (디버그용)</summary>
        public string Reasoning { get; set; } = "";

        /// <summary>
        /// 디버그용 출력
        /// </summary>
        public override string ToString()
        {
            return $"[MedicalDecision] 치료:{TreatmentPriority} | 침대확보:{BedPriority} | " +
                   $"의약품생산:{MedicineProductionPriority} | 연구:{MedicalResearchPriority}\n" +
                   $"글리터월드:{UseGlitterworldMedicine} | 약초재배:{ShouldGrowHerbs} | 침대필요:{AdditionalBedsNeeded}\n" +
                   $"이유: {Reasoning}";
        }
    }
}
