using System.Text;
using RimAI.Core;

namespace RimAI.Medical
{
    /// <summary>
    /// 의료 관련 작업 우선순위를 결정하는 의사결정 엔진
    ///
    /// 핵심 알고리즘:
    /// 1. 중상/출혈 환자 → 최우선 치료
    /// 2. 면역 경쟁 상황 → 집중 치료
    /// 3. 침대 부족 → 건설 우선순위 상승
    /// 4. 의약품 부족 → 생산/재배 권장
    /// </summary>
    public static class MedicalDecisionEngine
    {
        // === 임계값 설정 ===
        private const int BED_BUFFER = 2;           // 여유 침대 수
        private const float GLITTERWORLD_THRESHOLD = 0.3f; // 면역 경쟁 심각도 임계값

        /// <summary>
        /// 콜로니 의료 상태를 분석하여 작업 우선순위 결정
        /// </summary>
        public static MedicalDecision MakeDecision(ColonyMedicalState state)
        {
            var decision = new MedicalDecision();
            var reasoning = new StringBuilder();

            // === 1. 즉각 대응: 위기 레벨 기반 ===
            var crisisLevel = state.GetCrisisLevel();
            reasoning.AppendLine($"[위기 레벨: {crisisLevel}] 환자:{state.PatientsWaiting}대기 중상:{state.CriticalPawns} 출혈:{state.BleedingPawns}");

            switch (crisisLevel)
            {
                case 3: // 위기 (중상/출혈)
                    reasoning.AppendLine("→ 의료 위기! 즉시 치료 필요");
                    decision.TreatmentPriority = MedicalWorkPriority.Critical;
                    decision.BedPriority = MedicalWorkPriority.High;
                    decision.UseGlitterworldMedicine = state.GlitterworldMedicine > 0;

                    // 스토리 로그
                    StoryLogger.Medical.Emergency(state.CriticalPawns, state.BleedingPawns);
                    break;

                case 2: // 경고 (면역 위험/감염 다수)
                    reasoning.AppendLine("→ 의료 경고! 치료 우선");
                    decision.TreatmentPriority = MedicalWorkPriority.High;
                    decision.BedPriority = MedicalWorkPriority.Normal;
                    break;

                case 1: // 주의 (대기 환자/의약품 부족)
                    reasoning.AppendLine("→ 의료 주의: 치료 권장");
                    decision.TreatmentPriority = MedicalWorkPriority.Normal;
                    decision.BedPriority = MedicalWorkPriority.Low;
                    break;

                default: // 안전
                    reasoning.AppendLine("→ 의료 상황 안정");
                    decision.TreatmentPriority = MedicalWorkPriority.Low;
                    decision.BedPriority = MedicalWorkPriority.None;
                    break;
            }

            // === 2. 침대 확보 결정 ===
            DecideBedPriority(state, decision, reasoning);

            // === 3. 의약품 관리 ===
            DecideMedicineManagement(state, decision, reasoning);

            // === 4. 면역 경쟁 대응 ===
            DecideImmunityResponse(state, decision, reasoning);

            // === 5. 의료 연구 우선순위 ===
            DecideMedicalResearch(state, decision, reasoning);

            // === 6. 의사 배치 균형 ===
            BalanceDoctorWorkload(state, decision, reasoning);

            decision.Reasoning = reasoning.ToString();
            return decision;
        }

        /// <summary>
        /// 의료 침대 확보 우선순위 결정
        /// </summary>
        private static void DecideBedPriority(ColonyMedicalState state, MedicalDecision decision, StringBuilder reasoning)
        {
            reasoning.AppendLine($"[침대] 가용:{state.AvailableMedicalBeds}/{state.TotalMedicalBeds} 점유:{state.OccupiedMedicalBeds}");

            int patientsNeedBed = state.PawnsNeedingTreatment.Count;
            int bedsNeeded = patientsNeedBed + BED_BUFFER - state.TotalMedicalBeds;

            if (bedsNeeded > 0)
            {
                reasoning.AppendLine($"→ 의료 침대 {bedsNeeded}개 부족!");
                decision.AdditionalBedsNeeded = bedsNeeded;
                decision.BedPriority = BoostPriority(decision.BedPriority);

                // 스토리 로그
                StoryLogger.Medical.BedShortage(bedsNeeded);
            }
            else if (state.AvailableMedicalBeds < BED_BUFFER)
            {
                reasoning.AppendLine("→ 여유 침대 부족, 추가 건설 권장");
                decision.AdditionalBedsNeeded = BED_BUFFER - state.AvailableMedicalBeds;
            }
            else
            {
                reasoning.AppendLine("→ 침대 상황 양호");
            }
        }

        /// <summary>
        /// 의약품 관리 결정
        /// </summary>
        private static void DecideMedicineManagement(ColonyMedicalState state, MedicalDecision decision, StringBuilder reasoning)
        {
            reasoning.AppendLine($"[의약품] 약초:{state.HerbalMedicine} 일반:{state.StandardMedicine} " +
                                $"글리터:{state.GlitterworldMedicine} (인당:{state.MedicinePerColonist:F1})");

            // 의약품 부족 시
            if (state.MedicineShortage)
            {
                reasoning.AppendLine("→ 의약품 부족! 생산 우선");
                decision.MedicineProductionPriority = MedicalWorkPriority.High;
                decision.ShouldGrowHerbs = true;

                // 스토리 로그
                StoryLogger.Medical.MedicineShortage(state.TotalMedicine, state.TotalColonists);
            }
            else if (state.MedicinePerColonist < 10)
            {
                reasoning.AppendLine("→ 의약품 비축 권장");
                decision.MedicineProductionPriority = MedicalWorkPriority.Normal;
                decision.ShouldGrowHerbs = state.HerbalMedicine < state.StandardMedicine;
            }
            else
            {
                reasoning.AppendLine("→ 의약품 상황 양호");
                decision.MedicineProductionPriority = MedicalWorkPriority.Low;
            }

            // 글리터월드 의약품 사용 결정
            if (state.CriticalPawns > 0 || state.ImmunityAtRisk > 0)
            {
                decision.UseGlitterworldMedicine = state.GlitterworldMedicine > 0;
                if (decision.UseGlitterworldMedicine)
                {
                    reasoning.AppendLine("→ 글리터월드 의약품 사용 권장 (위급 상황)");
                }
            }
        }

        /// <summary>
        /// 면역 경쟁 대응 결정
        /// </summary>
        private static void DecideImmunityResponse(ColonyMedicalState state, MedicalDecision decision, StringBuilder reasoning)
        {
            if (state.ImmunityRaces.Count == 0)
            {
                return;
            }

            reasoning.AppendLine($"[면역 경쟁] {state.ImmunityRaces.Count}명 질병 중, {state.ImmunityAtRisk}명 위험");

            foreach (var race in state.ImmunityRaces)
            {
                if (race.AtRisk)
                {
                    reasoning.AppendLine($"  ⚠️ {race.Patient.Name.ToStringShort}: {race.Disease.label} " +
                                        $"(질병:{race.DiseaseSeverity:P0} vs 면역:{race.Immunity:P0})");

                    // 심각한 경우 글리터월드 의약품 사용
                    float diff = race.DiseaseSeverity - race.Immunity;
                    if (diff > GLITTERWORLD_THRESHOLD && state.GlitterworldMedicine > 0)
                    {
                        decision.UseGlitterworldMedicine = true;
                        reasoning.AppendLine("    → 글리터월드 의약품 사용 권장");

                        // 스토리 로그
                        StoryLogger.Medical.ImmunityRace(race.Patient.Name.ToStringShort,
                            race.Disease.label, race.WinProbability);
                    }
                }
            }

            // 면역 위험이 있으면 치료 우선순위 상승
            if (state.ImmunityAtRisk > 0)
            {
                decision.TreatmentPriority = BoostPriority(decision.TreatmentPriority);
                reasoning.AppendLine("→ 면역 경쟁 위험으로 치료 우선순위 상승");
            }
        }

        /// <summary>
        /// 의료 연구 우선순위 결정
        /// </summary>
        private static void DecideMedicalResearch(ColonyMedicalState state, MedicalDecision decision, StringBuilder reasoning)
        {
            // 기본적으로 낮은 우선순위
            decision.MedicalResearchPriority = MedicalWorkPriority.Low;

            // 의약품 생산이 시급하면 연구도 우선순위 상승
            if (decision.MedicineProductionPriority >= MedicalWorkPriority.High)
            {
                decision.MedicalResearchPriority = MedicalWorkPriority.Normal;
                reasoning.AppendLine("[연구] 의약품 연구 권장");
            }

            // 숙련 의사가 없으면 의료 연구 권장
            if (state.SkilledDoctors == 0 && state.TotalColonists >= 5)
            {
                decision.MedicalResearchPriority = MedicalWorkPriority.Normal;
                reasoning.AppendLine("[연구] 숙련 의사 부재, 의료 연구 권장");
            }
        }

        /// <summary>
        /// 의사 배치 균형
        /// </summary>
        private static void BalanceDoctorWorkload(ColonyMedicalState state, MedicalDecision decision, StringBuilder reasoning)
        {
            reasoning.AppendLine($"[의사] {state.DoctorsWorking}/{state.CapableDoctors} 활동 중 (숙련:{state.SkilledDoctors})");

            // 환자 대비 의사가 부족한 경우
            if (state.PatientsWaiting > state.CapableDoctors && state.CapableDoctors < 3)
            {
                reasoning.AppendLine("→ 의료 인력 부족! 추가 의사 양성 필요");

                // 스토리 로그
                StoryLogger.Medical.DoctorShortage(state.CapableDoctors, state.PatientsWaiting);
            }

            // 모든 의사가 이미 작업 중이면 다른 폰이 도와야 함
            if (state.DoctorsWorking >= state.CapableDoctors && state.PatientsWaiting > 0)
            {
                reasoning.AppendLine("→ 의사 모두 바쁨, 추가 지원 필요");
            }
        }

        /// <summary>
        /// 우선순위 한 단계 상승
        /// </summary>
        private static MedicalWorkPriority BoostPriority(MedicalWorkPriority current)
        {
            if (current < MedicalWorkPriority.Critical)
                return current + 1;
            return current;
        }

        /// <summary>
        /// 우선순위 한 단계 하락
        /// </summary>
        private static MedicalWorkPriority ReducePriority(MedicalWorkPriority current)
        {
            if (current > MedicalWorkPriority.None)
                return current - 1;
            return current;
        }
    }
}
