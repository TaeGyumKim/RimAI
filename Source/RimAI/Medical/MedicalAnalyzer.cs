using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimAI.Medical
{
    /// <summary>
    /// 콜로니의 의료 상태를 분석하는 클래스
    ///
    /// 분석 항목:
    /// - 환자 현황 (부상, 감염, 질병)
    /// - 의료 시설 (침대, 의약품)
    /// - 의료 인력 (의사 스킬/가용성)
    /// - 면역 경쟁 (질병 vs 면역)
    /// </summary>
    public static class MedicalAnalyzer
    {
        // 의사 스킬 기준
        private const int SKILLED_DOCTOR_THRESHOLD = 10;
        private const int CAPABLE_DOCTOR_THRESHOLD = 4;

        // 의약품 부족 기준 (인당)
        private const float MEDICINE_SHORTAGE_THRESHOLD = 5f;

        /// <summary>
        /// 특정 맵의 의료 상태를 분석
        /// </summary>
        public static ColonyMedicalState AnalyzeMap(Map map)
        {
            var state = new ColonyMedicalState();

            // 1. 환자 현황 분석
            AnalyzePatients(map, state);

            // 2. 의료 시설 분석
            AnalyzeMedicalFacilities(map, state);

            // 3. 의약품 분석
            AnalyzeMedicine(map, state);

            // 4. 의료 인력 분석
            AnalyzeDoctors(map, state);

            // 5. 면역 경쟁 분석
            AnalyzeImmunityRaces(map, state);

            return state;
        }

        /// <summary>
        /// 환자 현황 분석
        /// </summary>
        private static void AnalyzePatients(Map map, ColonyMedicalState state)
        {
            state.PawnsNeedingTreatment.Clear();
            state.TotalColonists = map.mapPawns.FreeColonistsSpawnedCount;

            foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
            {
                if (pawn.Dead) continue;

                // 건강 상태 분석
                var healthState = AnalyzePawnHealth(pawn);

                if (healthState.NeedsTreatment)
                {
                    state.PawnsNeedingTreatment.Add(pawn);

                    // 분류
                    if (healthState.IsCritical)
                        state.CriticalPawns++;
                    else if (healthState.HasMinorInjury)
                        state.MinorInjuredPawns++;

                    if (healthState.IsBleeding)
                        state.BleedingPawns++;

                    if (healthState.HasInfection)
                        state.InfectedPawns++;

                    if (healthState.HasDisease)
                        state.DiseasedPawns++;
                }

                // 현재 치료 중인지 확인
                if (pawn.CurJob?.def == JobDefOf.TendPatient)
                {
                    state.PatientsBeingTreated++;
                }
            }

            state.PatientsWaiting = state.PawnsNeedingTreatment.Count - state.PatientsBeingTreated;

            // 동물도 확인 (간단히)
            foreach (var animal in map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
            {
                if (!animal.RaceProps.Animal) continue;
                if (NeedsTreatment(animal))
                {
                    state.PawnsNeedingTreatment.Add(animal);
                }
            }
        }

        /// <summary>
        /// 개별 폰의 건강 상태 분석
        /// </summary>
        private static PawnHealthAnalysis AnalyzePawnHealth(Pawn pawn)
        {
            var analysis = new PawnHealthAnalysis();

            if (pawn.health?.hediffSet == null)
                return analysis;

            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                // 출혈 확인
                if (hediff.Bleeding)
                {
                    analysis.IsBleeding = true;
                    analysis.NeedsTreatment = true;
                }

                // 치료 필요 확인
                if (hediff.TendableNow())
                {
                    analysis.NeedsTreatment = true;

                    // 심각도에 따라 분류
                    if (hediff.Severity >= 0.5f || hediff.def.lethalSeverity > 0)
                    {
                        analysis.IsCritical = true;
                    }
                    else
                    {
                        analysis.HasMinorInjury = true;
                    }
                }

                // 감염 확인
                if (hediff.def == HediffDefOf.WoundInfection)
                {
                    analysis.HasInfection = true;
                    analysis.NeedsTreatment = true;
                }

                // 질병 확인 (면역이 필요한 것)
                if (hediff.def.makesSickThought && hediff.def.CompProps<HediffCompProperties_Immunizable>() != null)
                {
                    analysis.HasDisease = true;
                    analysis.NeedsTreatment = true;
                }
            }

            // 전체 건강 수치 확인
            if (pawn.health.summaryHealth.SummaryHealthPercent < 0.5f)
            {
                analysis.IsCritical = true;
            }

            return analysis;
        }

        /// <summary>
        /// 폰이 치료가 필요한지 간단 확인
        /// </summary>
        private static bool NeedsTreatment(Pawn pawn)
        {
            if (pawn.health?.hediffSet == null) return false;

            return pawn.health.hediffSet.hediffs.Any(h => h.TendableNow());
        }

        /// <summary>
        /// 의료 시설 분석 (침대)
        /// </summary>
        private static void AnalyzeMedicalFacilities(Map map, ColonyMedicalState state)
        {
            state.TotalMedicalBeds = 0;
            state.AvailableMedicalBeds = 0;
            state.OccupiedMedicalBeds = 0;

            // 모든 침대 찾기
            var beds = map.listerThings.ThingsInGroup(ThingRequestGroup.Bed)
                .OfType<Building_Bed>();

            foreach (var bed in beds)
            {
                // 플레이어 소유의 의료용 침대만
                if (bed.Faction != Faction.OfPlayer)
                    continue;

                if (!bed.Medical)
                    continue;

                state.TotalMedicalBeds += bed.SleepingSlotsCount;

                // 사용 중인지 확인
                bool isOccupied = bed.CurOccupants?.Any() == true;

                if (isOccupied)
                {
                    state.OccupiedMedicalBeds += bed.SleepingSlotsCount;
                }
                else if (bed.AnyUnoccupiedSleepingSlot)
                {
                    state.AvailableMedicalBeds += bed.SleepingSlotsCount;
                }
            }

            // 침대 부족 판단
            int totalPatients = state.PawnsNeedingTreatment.Count;
            state.BedShortage = state.AvailableMedicalBeds < totalPatients;
        }

        /// <summary>
        /// 의약품 분석
        /// </summary>
        private static void AnalyzeMedicine(Map map, ColonyMedicalState state)
        {
            state.HerbalMedicine = 0;
            state.StandardMedicine = 0;
            state.GlitterworldMedicine = 0;

            // 모든 의약품 찾기
            var medicines = map.listerThings.ThingsInGroup(ThingRequestGroup.Medicine);

            foreach (var medicine in medicines)
            {
                // 플레이어가 접근 가능한 것만
                if (!medicine.IsForbidden(Faction.OfPlayer))
                {
                    if (medicine.def == ThingDefOf.MedicineHerbal)
                    {
                        state.HerbalMedicine += medicine.stackCount;
                    }
                    else if (medicine.def == ThingDefOf.MedicineIndustrial)
                    {
                        state.StandardMedicine += medicine.stackCount;
                    }
                    else if (medicine.def == ThingDefOf.MedicineUltratech)
                    {
                        state.GlitterworldMedicine += medicine.stackCount;
                    }
                }
            }

            state.TotalMedicine = state.HerbalMedicine + state.StandardMedicine + state.GlitterworldMedicine;

            // 인당 의약품 비율 계산
            if (state.TotalColonists > 0)
            {
                state.MedicinePerColonist = (float)state.TotalMedicine / state.TotalColonists;
            }

            // 부족 판단
            state.MedicineShortage = state.MedicinePerColonist < MEDICINE_SHORTAGE_THRESHOLD;
        }

        /// <summary>
        /// 의료 인력 분석
        /// </summary>
        private static void AnalyzeDoctors(Map map, ColonyMedicalState state)
        {
            state.CapableDoctors = 0;
            state.SkilledDoctors = 0;
            state.DoctorsWorking = 0;

            foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
            {
                if (pawn.Dead || pawn.Downed) continue;

                // 의료 작업 가능한지 확인
                if (pawn.workSettings?.GetPriority(WorkTypeDefOf.Doctor) > 0)
                {
                    int medicalSkill = pawn.skills?.GetSkill(SkillDefOf.Medicine)?.Level ?? 0;

                    if (medicalSkill >= CAPABLE_DOCTOR_THRESHOLD)
                    {
                        state.CapableDoctors++;

                        if (medicalSkill >= SKILLED_DOCTOR_THRESHOLD)
                        {
                            state.SkilledDoctors++;
                        }
                    }
                }

                // 현재 치료 작업 중인지 확인
                if (pawn.CurJob?.def == JobDefOf.TendPatient ||
                    pawn.CurJob?.def == JobDefOf.Rescue)
                {
                    state.DoctorsWorking++;
                }
            }
        }

        /// <summary>
        /// 면역 경쟁 분석 (질병 vs 면역)
        /// </summary>
        private static void AnalyzeImmunityRaces(Map map, ColonyMedicalState state)
        {
            state.ImmunityRaces.Clear();
            state.ImmunityAtRisk = 0;

            foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
            {
                if (pawn.Dead) continue;

                foreach (var hediff in pawn.health.hediffSet.hediffs)
                {
                    // 면역이 필요한 질병인지 확인
                    var immunizable = hediff.TryGetComp<HediffComp_Immunizable>();
                    if (immunizable == null) continue;

                    var race = new ImmunityRace
                    {
                        Patient = pawn,
                        Disease = hediff.def,
                        DiseaseSeverity = hediff.Severity,
                        Immunity = immunizable.Immunity
                    };

                    state.ImmunityRaces.Add(race);

                    if (race.AtRisk)
                    {
                        state.ImmunityAtRisk++;
                    }
                }
            }
        }

        /// <summary>
        /// 개별 폰 건강 분석 결과
        /// </summary>
        private class PawnHealthAnalysis
        {
            public bool NeedsTreatment { get; set; }
            public bool IsCritical { get; set; }
            public bool HasMinorInjury { get; set; }
            public bool IsBleeding { get; set; }
            public bool HasInfection { get; set; }
            public bool HasDisease { get; set; }
        }
    }
}
