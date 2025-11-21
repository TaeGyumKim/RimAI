# 4단계: 전투/위기 대응 자동화 + 시네마틱 카메라

## 개요

4단계에서는 레이드, 메카노이드, 동물 습격 등 **모든 위협 상황에 자동으로 대응**하는 전투 시스템을 구현합니다. 플레이어 개입 없이 콜로니가 스스로 방어하며, 관람용 **시네마틱 카메라 연출**로 영화처럼 전투를 감상할 수 있습니다.

### 주요 목표
1. **완전 자동 전투**: 적 감지 → Draft → 전투 → 위협 해제 → Undraft
2. **전투력 기반 의사결정**: 아군/적 전투력 비교로 전략 결정
3. **시네마틱 카메라**: 전투 장면 자동 추적 및 줌
4. **우선순위 시스템**: 전투 중에는 다른 활동 일시 정지

---

## 시스템 아키텍처

```
┌────────────────────────────────────────────────────┐
│          CombatDefenseSubsystem (우선순위: 150)     │
│  - 1초마다 위협 스캔                                │
│  - Draft/Undraft 자동 관리                         │
│  - 시네마틱 카메라 연출                             │
└──────────┬─────────────────────────────────────────┘
           │
    ┌──────┴──────┬──────────────┬──────────────┐
    ▼             ▼              ▼              ▼
┌──────────┐ ┌───────────┐ ┌──────────────┐ ┌─────────────┐
│ThreatState│ │Threat     │ │CombatDefense │ │Cinematic    │
│          │ │Analyzer   │ │Subsystem     │ │CameraHelper │
│위협 데이터│ │위협 평가  │ │전투 실행     │ │카메라 연출  │
└──────────┘ └───────────┘ └──────────────┘ └─────────────┘
```

---

## 1. 위협 상태 평가 시스템

### 1.1 ThreatLevel (위협 레벨)

**위치**: `Source/RimAI/Combat/ThreatState.cs`

```csharp
public enum ThreatLevel
{
    None = 0,     // 위협 없음 - 평시
    Low = 1,      // 소규모 위협 (동물, 소수 적)
    Medium = 2,   // 중간 규모 레이드
    High = 3,     // 대규모 레이드
    Critical = 4  // 압도적인 위협
}
```

**결정 기준** (`ThreatAnalyzer.DetermineThreatLevel()`):

```
전투력 비율 = 아군 전투력 / 적 전투력

조건 1: 전투력 비율 < 0.5 OR 적 수 > 20   → Critical
조건 2: 전투력 비율 < 0.8 OR 적 수 > 10   → High
조건 3: 전투력 비율 < 1.2 OR 적 수 > 5    → Medium
조건 4: 그 외                             → Low
```

---

### 1.2 CombatPosture (전투 태세)

```csharp
public enum CombatPosture
{
    Peaceful,  // 평시 - 일상 작업
    Alert,     // 경계 - 준비 태세
    Combat,    // 전투 - 적극 대응
    Retreat    // 철수 - 후퇴
}
```

**결정 기준** (`ThreatAnalyzer.DetermineCombatPosture()`):

```
ThreatLevel.None     → Peaceful
ThreatLevel.Low      → Alert
ThreatLevel.Medium/High → Combat
ThreatLevel.Critical:
  - 전투력 비율 < 0.3 → Retreat (압도적으로 불리)
  - 그 외 → Combat
```

---

### 1.3 ColonyThreatState (위협 상태 데이터)

**주요 속성**:

```csharp
// 위협 정보
- CurrentThreatLevel      // 현재 위협 레벨
- TotalEnemies            // 적 수
- EnemyCombatPower        // 적 전투력
- ThreatCenter            // 위협 중심 위치

// 아군 정보
- CombatCapablePawns      // 전투 가능한 콜로니스트 목록
- AllyCombatPower         // 아군 전투력
- DraftedPawns            // Draft 상태인 폰 수

// 전투 상태
- InCombat                // 전투 진행 중
- CombatStartTick         // 전투 시작 시각
- LastEnemySeenTick       // 마지막 적 발견 시각
```

**주요 메서드**:

```csharp
// 전투력 비율
public float PowerRatio
{
    get => AllyCombatPower / EnemyCombatPower;
}

// 위협 해제 확인
public bool IsThreatCleared()
{
    if (TotalEnemies == 0)
    {
        int ticksSinceLastEnemy = TicksGame - LastEnemySeenTick;
        return ticksSinceLastEnemy > 300; // 5초
    }
    return false;
}
```

---

## 2. 전투력 계산 (ThreatAnalyzer.CalculateCombatPower())

### 2.1 계산 공식

```python
전투력 = 기본값(10) * 체력비율

# 무기 보너스
if 무기 있음:
    전투력 += 무기_데미지
    전투력 *= (사거리 / 10 + 1)  # 사거리 보너스

# 갑옷 보너스
for 갑옷 in 착용_갑옷:
    if 갑옷.방어력 > 0:
        전투력 += 5

# 스킬 보너스
사격_스킬 = pawn.skills.Shooting.Level
근접_스킬 = pawn.skills.Melee.Level
전투력 *= (1 + (사격_스킬 + 근접_스킬) / 40)

# 메카노이드는 2배 강함
if pawn.IsMechanoid:
    전투력 *= 2
```

### 2.2 예시

**콜로니스트 (무기: 볼트 액션 라이플, 사격 스킬 8)**:
```
기본: 10
체력: 10 * 1.0 = 10
무기: 10 + 15 (데미지) = 25
사거리: 25 * (35/10 + 1) = 25 * 4.5 = 112.5
스킬: 112.5 * (1 + 8/40) = 112.5 * 1.2 = 135

→ 전투력: 135
```

**메카노이드 센티피드**:
```
기본: 10
메카노이드: 10 * 2 = 20
무장: 20 + 30 = 50
...
→ 전투력: 약 300-400 (강력!)
```

---

## 3. CombatDefenseSubsystem (전투 서브시스템)

### 3.1 기본 동작 흐름

```
1초마다:
  ├─> ThreatAnalyzer.AnalyzeMap(map)
  │    ├─> 모든 폰 스캔
  │    ├─> 적/아군 분류
  │    ├─> 전투력 계산
  │    └─> 위협 레벨 결정
  │
  ├─> GetProposedActions()
  │    ├─> ThreatLevel.None → "전투 종료" 액션
  │    ├─> ThreatLevel.Low → "경계" 액션
  │    └─> ThreatLevel.Medium/High/Critical → "전투 개시" 액션
  │
  └─> ExecuteAction()
       ├─> StartCombat() → Draft all + 카메라 이동
       └─> EndCombat() → Undraft all + 카메라 복원
```

---

### 3.2 전투 시작 (StartCombat)

```csharp
private void StartCombat(Map map, ColonyThreatState state)
{
    // 1. 전투 중으로 표시
    mapsInCombat.Add(map);
    state.InCombat = true;
    state.CombatStartTick = TicksGame;

    Log.Warning($"전투 시작! 위협: {state.CurrentThreatLevel}, 적: {state.TotalEnemies}명");

    // 2. 카메라 이동 (전투 위치로)
    CinematicCameraHelper.FocusOnCombat(map, state.ThreatCenter);

    // 3. 전투 가능한 모든 폰 Draft
    foreach (var pawn in state.CombatCapablePawns)
    {
        if (!pawn.Drafted)
            pawn.drafter.Drafted = true;
    }

    // 4. 방어 위치 할당 (선택사항 - vanilla AI가 잘 함)
    // AssignDefensivePositions(map, state);
}
```

---

### 3.3 전투 종료 (EndCombat)

```csharp
private void EndCombat(Map map, ColonyThreatState state)
{
    mapsInCombat.Remove(map);
    state.InCombat = false;

    Log.Message($"전투 종료! 지속시간: {state.CombatDurationSeconds:F1}초");

    // 1. 모든 폰 Undraft
    foreach (var colonist in map.mapPawns.FreeColonistsSpawned)
    {
        if (colonist.Drafted)
            colonist.drafter.Drafted = false;
    }

    // 2. 카메라 복원
    CinematicCameraHelper.ResetCamera();
}
```

---

### 3.4 위협 해제 로직

**조건**:
1. 적이 모두 제거됨 (사망, 도망, 항복)
2. 마지막 적 발견 후 5초 경과

```csharp
public bool IsThreatCleared()
{
    if (TotalEnemies == 0)
    {
        int ticksSinceLastEnemy = TicksGame - LastEnemySeenTick;
        return ticksSinceLastEnemy > 300; // 5초 (300틱)
    }
    return false;
}
```

**처리**:
- 위협이 해제되면 `ThreatLevel.None`으로 변경
- `GetProposedActions()`에서 "전투 종료" 액션 생성
- `ExecuteAction()`에서 `EndCombat()` 호출

---

## 4. 시네마틱 카메라 시스템

### 4.1 CinematicCameraHelper

**위치**: `Source/RimAI/Combat/CinematicCameraHelper.cs`

RimWorld의 `Find.CameraDriver`를 활용하여 카메라를 제어합니다.

#### 주요 기능

**1. 전투 위치로 카메라 이동 및 줌**

```csharp
public static void FocusOnCombat(Map map, IntVec3 focusPosition)
{
    var cameraDriver = Find.CameraDriver;

    // 원래 상태 저장
    if (!isCinematicMode)
    {
        originalPosition = cameraDriver.MapPosition;
        originalZoom = cameraDriver.rootSize;
        isCinematicMode = true;
    }

    // 전투 위치로 즉시 점프
    cameraDriver.JumpToCurrentMapLoc(focusPosition);

    // 줌 레벨 조정 (가까이)
    cameraDriver.SetRootSize(COMBAT_ZOOM_LEVEL); // 4.5
}
```

**2. 카메라 복원**

```csharp
public static void ResetCamera()
{
    var cameraDriver = Find.CameraDriver;

    // 줌 레벨 복원 (멀리)
    cameraDriver.SetRootSize(NORMAL_ZOOM_LEVEL); // 12

    isCinematicMode = false;
}
```

**3. 특정 폰 추적** (선택사항)

```csharp
public static void TrackPawn(Pawn pawn)
{
    if (pawn.Spawned)
    {
        Find.CameraDriver.JumpToCurrentMapLoc(pawn.Position);
    }
}
```

---

### 4.2 기술적 분석

**가능한 것**:
- ✅ 카메라 위치 이동 (`JumpToCurrentMapLoc`)
- ✅ 줌 레벨 조정 (`SetRootSize`)
- ✅ 특정 위치로 즉시 이동
- ✅ 두 위치 사이 프레이밍 (`FrameTwoPositions`)

**제한 사항**:
- ❌ 부드러운 카메라 패닝 (RimWorld API 제한)
  - `JumpToCurrentMapLoc`은 즉시 이동만 가능
  - 부드러운 이동은 게임 내부 로직 필요
- ❌ 카메라 각도 변경 (탑다운 고정)
- ❌ 슬로우 모션 효과 (게임 속도 변경은 가능하지만 권장하지 않음)

**대안**:
- 즉시 점프 + 적절한 줌 레벨로 "시네마틱 느낌" 연출
- 전투 시작/종료 시점에만 카메라 이동 (너무 빈번하면 어지러움)

---

### 4.3 사용 예시

```csharp
// 전투 시작 시
CinematicCameraHelper.FocusOnCombat(map, threatCenter);
// → 전투 위치로 카메라 즉시 이동 + 줌 인

// 전투 종료 시
CinematicCameraHelper.ResetCamera();
// → 원래 줌 레벨로 복원

// 특정 폰 추적 (선택사항)
CinematicCameraHelper.TrackPawn(importantPawn);
```

---

## 5. 중앙 브레인 통합

### 5.1 우선순위 시스템

```
서브시스템 우선순위:
Combat       150  (최우선)
Food         100  (생존)
Construction  50  (인프라)
Research      30  (발전)
```

**전투 중 일시 정지**:
- Combat, Food: 항상 작동
- Construction, Research: 전투 중 일시 정지

---

### 5.2 ProcessMap 수정

```csharp
private void ProcessMap(Map map)
{
    // 1. 전투 상태 확인
    bool inCombat = IsMapInCombat(map);

    // 2. 서브시스템 업데이트
    foreach (var subsystem in subsystems)
    {
        // 전투 중일 때는 비전투 서브시스템 일시 정지
        if (inCombat && ShouldPauseDuringCombat(subsystem))
            continue;

        subsystem.Update(map);
    }

    // 3. 액션 수집 및 실행
    CollectAndPrioritizeActions(map, inCombat);
    ExecutePendingActions(map);
}
```

---

### 5.3 일시 정지 로직

```csharp
private bool ShouldPauseDuringCombat(IRimAISubsystem subsystem)
{
    // Combat, Food는 항상 작동
    if (subsystem.Name == "Combat" || subsystem.Name == "Food")
        return false;

    // Construction, Research는 전투 중 일시 정지
    if (subsystem.Name == "Construction" || subsystem.Name == "Research")
        return true;

    return false;
}
```

**이유**:
- **Food**: 전투 중에도 식량 부족은 치명적
- **Combat**: 당연히 작동
- **Construction**: 전투 중 건설은 비효율적
- **Research**: 전투 중 연구는 불필요

---

## 6. RimWorld 전투 AI 활용

### 6.1 설계 철학

> **"바퀴를 재발명하지 말자"**

RimWorld는 이미 훌륭한 전투 AI를 가지고 있습니다:
- Draft된 폰은 자동으로 적을 찾아 공격
- 자동으로 커버 찾기 및 이동
- 자동으로 사거리 유지
- 부상자 자동 후퇴

**RimAI의 역할**:
- Draft/Undraft 타이밍만 결정
- 카메라 연출
- 전투 시작/종료 알림

---

### 6.2 Lord/Duty 시스템 (선택사항)

RimWorld의 고급 AI 시스템을 활용하려면:

```csharp
// Lord: 그룹 행동 관리
// Duty: 개별 폰의 역할

// 예: 방어 Lord 생성 (고급)
LordJob_DefendPoint defendJob = new LordJob_DefendPoint(defensivePosition);
Lord lord = LordMaker.MakeNewLord(Faction.OfPlayer, defendJob, map, colonists);
```

**현재 구현**:
- Draft만 사용 (간단하고 효과적)
- vanilla 전투 AI에 맡김

**향후 확장**:
- 특정 위치 방어 명령
- 팀 분할 (원거리/근거리)
- 후퇴 명령

---

## 7. 전투 시나리오 예시

### 시나리오 1: 소규모 레이드

```
0초: 적 3명 감지
  → ThreatLevel: Low
  → CombatPosture: Alert
  → 로그만 출력: "경계: 소규모 위협 감지 (3명)"

5초: 적이 접근
  → ThreatLevel: Medium
  → CombatPosture: Combat
  → 전투 시작: Draft all + 카메라 이동

30초: 적 모두 제거
  → ThreatLevel: None
  → 전투 종료: Undraft all + 카메라 복원
```

---

### 시나리오 2: 대규모 레이드

```
0초: 적 15명 감지
  → ThreatLevel: High
  → CombatPosture: Combat
  → 전투 시작: Draft all + 카메라 이동
  → 로그: "HIGH 위협! 적 15명 - 전투 개시"

60초: 치열한 전투
  → 일부 적 제거, 아군 부상
  → ThreatLevel: Medium (적 수 감소)

120초: 적 모두 제거
  → ThreatLevel: None
  → 전투 종료
  → 로그: "전투 종료! 지속시간: 120초"
```

---

### 시나리오 3: 압도적인 위협

```
0초: 적 25명 감지 (메카노이드 포함)
  → ThreatLevel: Critical
  → 전투력 비율: 0.25 (아군 열세)
  → CombatPosture: Retreat (!)
  → 로그: "Critical 위협! 전투력 부족 - 철수 권장"

현재 구현: Draft + 전투 (vanilla AI에 맡김)
향후 확장: 실제 후퇴 명령 (안전한 위치로 이동)
```

---

## 8. 설정 옵션

```csharp
public static class RimAI_Settings
{
    // 전투 자동화
    public static bool EnableCombatAutomation = true;

    // 시네마틱 카메라
    public static bool EnableCinematicCamera = true;

    // 상세 로그
    public static bool EnableDetailedLogging = true;
}
```

**향후 확장**:
- 전투 태세 임계값 조정
- 카메라 줌 레벨 사용자 설정
- 후퇴 전투력 비율 조정

---

## 9. 성능 최적화

### 9.1 업데이트 주기

```
Combat: 1초마다 (60틱)
Food: 5초마다 (300틱)
Construction: 10초마다 (600틱)
Research: 20초마다 (1200틱)
```

전투는 빠른 반응이 필요하므로 1초마다 업데이트합니다.

---

### 9.2 전투력 계산 캐싱

전투력은 매번 재계산하지만, 무거운 계산은 없습니다:
- 간단한 곱셈/덧셈
- 폰당 약 10-20개의 연산

대규모 레이드(50명)에서도 충분히 빠릅니다.

---

## 10. 파일 구조

```
Source/RimAI/Combat/
├── ThreatState.cs              (150줄) - 위협 상태 데이터
├── ThreatAnalyzer.cs           (300줄) - 위협 평가 및 전투력 계산
├── CombatDefenseSubsystem.cs   (350줄) - 전투 서브시스템
└── CinematicCameraHelper.cs    (150줄) - 카메라 연출

Source/RimAI/Core/
├── RimAIManager.cs             (업데이트) - 전투 중 일시 정지 로직
└── Patches/GamePatches.cs      (업데이트) - Combat 서브시스템 등록

Docs/
└── Stage4-Combat-Automation.md (이 문서)
```

---

## 11. 향후 확장

### 11.1 고급 전투 전략
- [ ] 팀 분할 (원거리/근거리)
- [ ] 특정 위치 방어 명령
- [ ] 실제 후퇴 명령 구현
- [ ] 부상자 자동 구출

### 11.2 카메라 개선
- [ ] 전투 하이라이트 자동 감지
- [ ] 멀티 뷰 (여러 전투 동시 발생 시)
- [ ] 사용자 커스텀 카메라 프리셋

### 11.3 알림 시스템
- [ ] 전투 시작 알림 (사운드)
- [ ] 전투 종료 요약 (사상자, 전리품)
- [ ] 위협 레벨 변화 알림

---

## 12. 테스트 체크리스트

- [ ] 소규모 레이드 (적 1-5명)
- [ ] 중규모 레이드 (적 6-15명)
- [ ] 대규모 레이드 (적 16-30명)
- [ ] 메카노이드 습격
- [ ] 야생 동물 습격
- [ ] 여러 맵에서 동시 전투
- [ ] 카메라 이동 및 복원
- [ ] 전투 종료 후 일상 복귀

---

**4단계 완료!**

이제 RimAI는:
- ✅ **완전 자동 전투**: 감지 → 대응 → 종료
- ✅ **전투력 기반 의사결정**: 수치 기반 전략
- ✅ **시네마틱 카메라**: 영화처럼 관람 가능
- ✅ **우선순위 시스템**: 전투 > 생존 > 발전

**진정한 방치 모드 완성! 🎮✨**
