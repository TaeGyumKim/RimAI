using System;
using Verse;

namespace RimAI.Core
{
    /// <summary>
    /// RimAI 스토리 로그 시스템
    /// 의사결정을 자연스러운 한국어 문장으로 변환하여
    /// 플레이어가 관람하면서 스토리를 느낄 수 있도록 합니다.
    /// </summary>
    public static class StoryLogger
    {
        private const string PREFIX = "[RimAI 스토리]";

        /// <summary>
        /// 스토리 로그 출력 (플레이 스타일 고려)
        /// </summary>
        public static void Log(string message, bool playStylePrefix = true)
        {
            if (!RimAI_Mod.Settings.ShouldLogStory())
                return;

            string finalMessage = message;

            // 플레이 스타일 프리픽스 추가
            if (playStylePrefix)
            {
                var stylePrefix = Settings.RimAIPlayStyleProfile.StoryKeywords
                    .GetStoryPrefix(RimAI_Mod.Settings.playStyle);
                finalMessage = $"{PREFIX} [{stylePrefix}] {message}";
            }
            else
            {
                finalMessage = $"{PREFIX} {message}";
            }

            Verse.Log.Message(finalMessage);
        }

        /// <summary>
        /// 플레이 스타일 관련 flavor text 포함 로그
        /// </summary>
        public static void LogWithFlavor(string mainMessage, int flavorIndex = -1)
        {
            if (!RimAI_Mod.Settings.ShouldLogStory())
                return;

            var style = RimAI_Mod.Settings.playStyle;
            var stylePrefix = Settings.RimAIPlayStyleProfile.StoryKeywords.GetStoryPrefix(style);

            string message = $"{PREFIX} [{stylePrefix}] {mainMessage}";

            // flavor text 추가 (선택적)
            if (flavorIndex >= 0)
            {
                var flavors = Settings.RimAIPlayStyleProfile.StoryKeywords.GetFlavorTexts(style);
                if (flavorIndex < flavors.Length)
                {
                    message += $" {flavors[flavorIndex]}";
                }
            }

            Verse.Log.Message(message);
        }

        /// <summary>
        /// 식량 관련 스토리 로그
        /// </summary>
        public static class Food
        {
            public static void Crisis(float daysLeft)
            {
                Log($"식량 위기 감지! 남은 생존 일수: {daysLeft:F1}일. 농업과 사냥을 최우선으로 전환합니다.");
            }

            public static void Emergency(float daysLeft)
            {
                Log($"긴급 식량 부족! ({daysLeft:F1}일치) 모든 폰을 식량 확보에 투입합니다.");
            }

            public static void Warning(float daysLeft)
            {
                Log($"식량 재고 경고. {daysLeft:F1}일치 남음. 생산량을 늘리겠습니다.");
            }

            public static void Stable()
            {
                Log($"식량 상태 안정. 여유로운 농업 관리를 이어갑니다.");
            }

            public static void Surplus()
            {
                var style = RimAI_Mod.Settings.playStyle;
                if (style == Settings.RimAIPlayStyle.Agricultural)
                {
                    Log($"풍요로운 수확! 농업 제국의 창고가 가득 찹니다.");
                }
                else
                {
                    Log($"식량 과잉 생산. 농업 인력을 다른 작업으로 전환합니다.");
                }
            }

            public static void WinterPreparation(int daysUntilWinter)
            {
                Log($"겨울 대비: {daysUntilWinter}일 후 겨울. 식량 비축을 시작합니다.");
            }

            public static void SpringSowing()
            {
                Log($"봄이 왔습니다! 대규모 파종을 시작합니다.");
            }

            public static void HarvestSeason()
            {
                Log($"수확의 계절입니다. 모든 작물을 거두어들이겠습니다.");
            }
        }

        /// <summary>
        /// 전투 관련 스토리 로그
        /// </summary>
        public static class Combat
        {
            public static void ThreatDetected(int enemyCount, string threatLevel)
            {
                var style = RimAI_Mod.Settings.playStyle;

                if (style == Settings.RimAIPlayStyle.Fortress)
                {
                    Log($"적 {enemyCount}명 감지! 방어 태세로 전환합니다. 요새를 지켜냅니다.");
                }
                else if (style == Settings.RimAIPlayStyle.Nomadic)
                {
                    Log($"위협 발견 ({enemyCount}명)! 신속하게 대응합니다.");
                }
                else
                {
                    Log($"적 습격! {enemyCount}명의 {threatLevel} 위협. 전투 준비!");
                }
            }

            public static void CombatStart(int colonists, int enemies)
            {
                Log($"전투 개시! 아군 {colonists}명 vs 적군 {enemies}명. 승리를 향해 나아갑니다.");
            }

            public static void CombatVictory(float durationSeconds)
            {
                var style = RimAI_Mod.Settings.playStyle;

                if (style == Settings.RimAIPlayStyle.Fortress)
                {
                    Log($"전투 종료! ({durationSeconds:F0}초) 요새를 성공적으로 방어했습니다.");
                }
                else
                {
                    Log($"승리! 전투 지속 {durationSeconds:F0}초. 일상으로 복귀합니다.");
                }
            }

            public static void DefenseBuilding()
            {
                var style = RimAI_Mod.Settings.playStyle;

                if (style == Settings.RimAIPlayStyle.Fortress)
                {
                    LogWithFlavor("방어 시설 건설을 계획합니다.", 0); // "벽을 높이고 방어선을 강화합니다."
                }
                else
                {
                    Log($"방어 시설을 보강합니다. 안전이 최우선입니다.");
                }
            }

            public static void RetreatDecision()
            {
                Log($"압도적인 적! 철수를 권장합니다. 생존이 우선입니다.");
            }
        }

        /// <summary>
        /// 건설 관련 스토리 로그
        /// </summary>
        public static class Construction
        {
            public static void BedShortage(int needed)
            {
                Log($"침대 부족! {needed}개 추가 건설을 계획합니다.");
            }

            public static void InfrastructurePlan(string buildingType)
            {
                var style = RimAI_Mod.Settings.playStyle;

                if (style == Settings.RimAIPlayStyle.Nomadic)
                {
                    Log($"필수 시설만 건설합니다: {buildingType}");
                }
                else if (style == Settings.RimAIPlayStyle.Agricultural)
                {
                    if (buildingType.Contains("창고"))
                    {
                        LogWithFlavor($"{buildingType} 건설을 시작합니다.", 2); // "대규모 창고를 건설합니다."
                    }
                    else
                    {
                        Log($"{buildingType} 건설을 계획합니다.");
                    }
                }
                else
                {
                    Log($"{buildingType} 건설을 계획합니다. 콜로니가 발전합니다.");
                }
            }

            public static void ExpansionDecision()
            {
                var style = RimAI_Mod.Settings.playStyle;

                if (style == Settings.RimAIPlayStyle.Agricultural)
                {
                    LogWithFlavor("영토 확장을 계획합니다.", 0); // "농장을 확장하여 식량 생산을 늘립니다."
                }
                else if (style == Settings.RimAIPlayStyle.Fortress)
                {
                    Log($"외벽을 확장합니다. 더 넓은 방어선이 필요합니다.");
                }
                else
                {
                    Log($"콜로니 확장을 계획합니다. 더 많은 공간이 필요합니다.");
                }
            }

            public static void ResourceGathering(string resourceType)
            {
                Log($"{resourceType} 자원 확보가 필요합니다. 채집을 시작합니다.");
            }
        }

        /// <summary>
        /// 연구 관련 스토리 로그
        /// </summary>
        public static class Research
        {
            public static void NewResearch(string researchName)
            {
                var style = RimAI_Mod.Settings.playStyle;

                if (style == Settings.RimAIPlayStyle.Researcher)
                {
                    LogWithFlavor($"새로운 연구 시작: {researchName}", 1); // "첨단 기술 개발에 집중합니다."
                }
                else if (style == Settings.RimAIPlayStyle.Nomadic)
                {
                    Log($"빠른 기술 발전을 위해 {researchName} 연구를 시작합니다.");
                }
                else if (style == Settings.RimAIPlayStyle.Fortress)
                {
                    if (researchName.Contains("방어") || researchName.Contains("터렛") || researchName.Contains("갑옷"))
                    {
                        Log($"방어력 강화를 위한 연구: {researchName}");
                    }
                    else
                    {
                        Log($"새로운 연구: {researchName}");
                    }
                }
                else
                {
                    Log($"기술 발전을 위해 {researchName} 연구를 시작합니다.");
                }
            }

            public static void ResearchComplete(string researchName)
            {
                var style = RimAI_Mod.Settings.playStyle;

                if (style == Settings.RimAIPlayStyle.Researcher)
                {
                    Log($"연구 완료: {researchName}! 과학의 힘으로 한 걸음 더 나아갑니다.");
                }
                else
                {
                    Log($"{researchName} 연구 완료! 새로운 가능성이 열렸습니다.");
                }
            }

            public static void TechFocus()
            {
                var style = RimAI_Mod.Settings.playStyle;

                if (style == Settings.RimAIPlayStyle.Researcher)
                {
                    LogWithFlavor("연구 인력을 확충합니다.", 0); // "연구 시설을 확충합니다."
                }
            }
        }

        /// <summary>
        /// 생산 관련 스토리 로그
        /// </summary>
        public static class Production
        {
            public static void WeaponShortage(int needed)
            {
                var style = RimAI_Mod.Settings.playStyle;

                if (style == Settings.RimAIPlayStyle.Fortress)
                {
                    Log($"무기가 부족합니다! 방어력 강화를 위해 {needed}개 생산을 시작합니다.");
                }
                else
                {
                    Log($"무기가 부족합니다! {needed}개 생산을 시작합니다.");
                }
            }

            public static void ArmorProduction(int count)
            {
                var style = RimAI_Mod.Settings.playStyle;

                if (style == Settings.RimAIPlayStyle.Fortress)
                {
                    Log($"방어력 강화! 갑옷 {count}벌 생산을 계획합니다.");
                }
                else
                {
                    Log($"갑옷 {count}벌 생산을 시작합니다.");
                }
            }

            public static void WinterPreparation(int daysLeft)
            {
                Log($"겨울 대비를 위해 겨울 옷 생산을 시작합니다. ({daysLeft}일 남음)");
            }

            public static void ResourceShortage(string resourceName)
            {
                Log($"{resourceName} 부족으로 비필수 생산을 일시 중단합니다.");
            }

            public static void ResourceSurplus()
            {
                Log($"자원 여유로 고품질 장비 생산을 시작합니다.");
            }

            public static void QualityUpgrade(string itemType)
            {
                var style = RimAI_Mod.Settings.playStyle;

                if (style == Settings.RimAIPlayStyle.Researcher)
                {
                    Log($"고품질 {itemType} 생산으로 콜로니를 업그레이드합니다.");
                }
                else
                {
                    Log($"자원 여유로 고품질 {itemType} 생산을 시작합니다.");
                }
            }

            public static void PausedDueToResources(string billName)
            {
                Log($"자원 부족으로 '{billName}' 생산을 일시 중단합니다.");
            }

            public static void ResumedProduction(string billName)
            {
                Log($"자원 확보! '{billName}' 생산을 재개합니다.");
            }

            public static void ClothingReplacement()
            {
                Log($"낡은 옷을 교체합니다. 콜로니스트들의 만족도가 향상됩니다.");
            }

            public static void MedicineProduction(int target)
            {
                Log($"의약품 비축을 시작합니다. 목표: {target}개");
            }

            public static void EmergencyProduction(string itemType)
            {
                Log($"⚠️ 긴급 생산! {itemType}이(가) 즉시 필요합니다.", playStylePrefix: false);
            }
        }

        /// <summary>
        /// 일반 스토리 이벤트
        /// </summary>
        public static class General
        {
            public static void ColonyGrowing(int population)
            {
                Log($"콜로니 성장! 현재 인구: {population}명. 더 큰 미래를 향해 나아갑니다.");
            }

            public static void PlayStyleActivated(Settings.RimAIPlayStyle style)
            {
                var desc = Settings.RimAIPlayStyleProfile.GetDescription(style);
                Log($"플레이 스타일 설정: {style}. {desc}", playStylePrefix: false);
            }

            public static void SeasonChange(Season newSeason)
            {
                var seasonName = newSeason switch
                {
                    Season.Spring => "봄",
                    Season.Summer => "여름",
                    Season.Fall => "가을",
                    Season.Winter => "겨울",
                    _ => "미지의 계절"
                };

                Log($"{seasonName}이 왔습니다. 계절에 맞춰 전략을 조정합니다.");
            }

            public static void CrisisMode()
            {
                Log($"⚠️ 위기 상황! 모든 자원을 생존에 집중합니다.", playStylePrefix: false);
            }

            public static void RecoveryMode()
            {
                Log($"위기 극복! 정상 운영으로 돌아갑니다.", playStylePrefix: false);
            }
        }
    }
}
