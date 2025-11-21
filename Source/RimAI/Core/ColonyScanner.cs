using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimAI.Core
{
    /// <summary>
    /// 콜로니 및 맵 상태를 스캔하고 분석하는 코어 클래스
    /// 게임 로드 시 현재 상태를 파악하여 로그로 출력합니다.
    /// </summary>
    public static class ColonyScanner
    {
        /// <summary>
        /// 현재 게임의 모든 맵과 콜로니 상태를 스캔
        /// </summary>
        public static void ScanAll()
        {
            if (Current.Game == null)
            {
                Log.Warning("[RimAI] 게임이 로드되지 않았습니다.");
                return;
            }

            Log.Message("[RimAI] ===== 콜로니 상태 스캔 시작 =====");

            var maps = Find.Maps;
            Log.Message($"[RimAI] 총 맵 개수: {maps.Count}");

            foreach (var map in maps)
            {
                ScanMap(map);
            }

            Log.Message("[RimAI] ===== 콜로니 상태 스캔 완료 =====");
        }

        /// <summary>
        /// 특정 맵의 상태를 스캔
        /// </summary>
        private static void ScanMap(Map map)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"\n[RimAI] --- 맵: {map.ToString()} ---");

            // 콜로니 폰 정보
            var colonists = map.mapPawns.FreeColonistsSpawned;
            sb.AppendLine($"[RimAI] 자유 콜로니스트: {colonists.Count()}명");

            if (colonists.Any())
            {
                foreach (var colonist in colonists.Take(5)) // 최대 5명만 표시
                {
                    var health = colonist.health?.summaryHealth?.SummaryHealthPercent ?? 0f;
                    var mood = colonist.needs?.mood?.CurLevel ?? 0f;
                    sb.AppendLine($"  - {colonist.Name?.ToStringShort ?? "Unknown"}: " +
                                $"체력 {health:P0}, 기분 {mood:P0}");
                }
                if (colonists.Count() > 5)
                {
                    sb.AppendLine($"  ... 외 {colonists.Count() - 5}명");
                }
            }

            // 동물 정보
            var animals = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer)
                .Where(p => p.RaceProps.Animal);
            sb.AppendLine($"[RimAI] 동물: {animals.Count()}마리");

            // 자원 정보
            var itemStacks = map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver);
            sb.AppendLine($"[RimAI] 운반 가능한 아이템: {itemStacks.Count()}개");

            // 식량 정보
            var foods = map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSource);
            sb.AppendLine($"[RimAI] 식량 자원: {foods.Count()}개");

            // 건설 중인 구조물
            var construction = map.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint);
            sb.AppendLine($"[RimAI] 건설 중인 청사진: {construction.Count()}개");

            // 작업 지시
            var designations = map.designationManager.allDesignations;
            sb.AppendLine($"[RimAI] 활성 작업 지시: {designations.Count}개");

            Log.Message(sb.ToString());
        }
    }

    /// <summary>
    /// 게임 로드 시 ColonyScanner를 자동으로 실행하는 Harmony 패치
    /// Verse.Game.LoadGame() 메서드 실행 후 스캔을 시작합니다.
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.FinalizeInit))]
    public static class Game_FinalizeInit_Patch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
                Log.Message("[RimAI] 게임 초기화 완료, 콜로니 스캔 시작");
                ColonyScanner.ScanAll();
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RimAI] 콜로니 스캔 중 오류 발생: {ex}");
            }
        }
    }
}
