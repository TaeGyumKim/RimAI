using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimAI.Combat
{
    /// <summary>
    /// 위협 레벨
    /// </summary>
    public enum ThreatLevel
    {
        /// <summary>위협 없음 - 평시</summary>
        None = 0,

        /// <summary>낮음 - 소규모 위협 (동물, 소수 적)</summary>
        Low = 1,

        /// <summary>보통 - 중간 규모 레이드</summary>
        Medium = 2,

        /// <summary>높음 - 대규모 레이드</summary>
        High = 3,

        /// <summary>위기 - 압도적인 위협</summary>
        Critical = 4
    }

    /// <summary>
    /// 전투 태세
    /// </summary>
    public enum CombatPosture
    {
        /// <summary>평시 - 일상 작업</summary>
        Peaceful,

        /// <summary>경계 - 준비 태세</summary>
        Alert,

        /// <summary>전투 - 적극 대응</summary>
        Combat,

        /// <summary>철수 - 후퇴</summary>
        Retreat
    }

    /// <summary>
    /// 콜로니 위협 상태
    /// </summary>
    public class ColonyThreatState
    {
        // === 위협 정보 ===

        /// <summary>현재 위협 레벨</summary>
        public ThreatLevel CurrentThreatLevel { get; set; }

        /// <summary>현재 전투 태세</summary>
        public CombatPosture CurrentPosture { get; set; }

        /// <summary>적 폰 목록</summary>
        public List<Pawn> EnemyPawns { get; set; } = new List<Pawn>();

        /// <summary>총 적 수</summary>
        public int TotalEnemies { get; set; }

        /// <summary>적 전투력 점수</summary>
        public float EnemyCombatPower { get; set; }

        /// <summary>위협 중심 위치</summary>
        public IntVec3 ThreatCenter { get; set; }

        // === 아군 정보 ===

        /// <summary>전투 가능한 콜로니스트</summary>
        public List<Pawn> CombatCapablePawns { get; set; } = new List<Pawn>();

        /// <summary>아군 전투력 점수</summary>
        public float AllyCombatPower { get; set; }

        /// <summary>Draft 상태인 폰 수</summary>
        public int DraftedPawns { get; set; }

        // === 전투 상태 ===

        /// <summary>전투 진행 중</summary>
        public bool InCombat { get; set; }

        /// <summary>전투 시작 시각 (틱)</summary>
        public int CombatStartTick { get; set; }

        /// <summary>마지막 적 발견 시각 (틱)</summary>
        public int LastEnemySeenTick { get; set; }

        /// <summary>전투 지속 시간 (초)</summary>
        public float CombatDurationSeconds
        {
            get
            {
                if (!InCombat) return 0f;
                return (Find.TickManager.TicksGame - CombatStartTick) / 60f;
            }
        }

        /// <summary>
        /// 전투력 비율 (아군/적)
        /// </summary>
        public float PowerRatio
        {
            get
            {
                if (EnemyCombatPower <= 0f) return 999f;
                return AllyCombatPower / EnemyCombatPower;
            }
        }

        /// <summary>
        /// 위협이 해제되었는지 확인
        /// </summary>
        public bool IsThreatCleared()
        {
            // 적이 없고 일정 시간이 지났으면 해제
            if (TotalEnemies == 0)
            {
                int ticksSinceLastEnemy = Find.TickManager.TicksGame - LastEnemySeenTick;
                return ticksSinceLastEnemy > 300; // 5초
            }

            return false;
        }

        /// <summary>
        /// 디버그 문자열
        /// </summary>
        public override string ToString()
        {
            return $"[Threat] 레벨:{CurrentThreatLevel} | 태세:{CurrentPosture} | " +
                   $"적:{TotalEnemies}({EnemyCombatPower:F0}) | " +
                   $"아군:{CombatCapablePawns.Count}({AllyCombatPower:F0}) | " +
                   $"비율:{PowerRatio:F2} | 전투중:{InCombat}";
        }
    }
}
