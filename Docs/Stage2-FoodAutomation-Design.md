# 2단계: 식량/농사 자동화 설계 문서

## 개요

RimAI 2단계에서는 콜로니의 식량 상태를 지속적으로 모니터링하고, 상황에 맞는 작업 우선순위를 자동으로 조정하는 시스템을 구현합니다.

## 시스템 아키텍처

```
┌─────────────────────────────────────────────────────────────┐
│                    FoodManager (GameComponent)               │
│  - 5초마다 모든 맵 업데이트                                    │
│  - 상태 분석 → 의사결정 → 캐시 저장                           │
└────────────────┬────────────────────────────────────────────┘
                 │
        ┌────────┴──────────┐
        │                   │
        ▼                   ▼
┌──────────────┐    ┌──────────────────┐
│FoodAnalyzer  │    │FoodDecisionEngine│
│              │    │                  │
│맵 상태 스캔  │───▶│ 우선순위 계산    │
│              │    │                  │
└──────────────┘    └──────────────────┘
        │                   │
        ▼                   ▼
┌──────────────┐    ┌──────────────────┐
│ColonyFoodState│    │  FoodDecision    │
│              │    │                  │
│- 식량량       │    │- 파종 우선순위   │
│- 농장 상태    │    │- 수확 우선순위   │
│- 계절/환경    │    │- 요리 우선순위   │
│- 작업 상태    │    │- 사냥 우선순위   │
└──────────────┘    └──────────────────┘
                             │
                             ▼
                    ┌──────────────────┐
                    │ Harmony Patches  │
                    │                  │
                    │- WorkGiver패치   │
                    │- 우선순위 조정   │
                    └──────────────────┘
```

## 핵심 클래스 설명

### 1. ColonyFoodState (데이터 모델)

**위치**: `Source/RimAI/Food/FoodState.cs`

**역할**: 콜로니의 현재 식량 상태를 담는 데이터 클래스

**주요 속성**:
- `TotalNutrition`: 총 영양분
- `DaysUntilStarvation`: 버틸 수 있는 일수
- `TotalFarmTiles`, `SownTiles`: 농장 활용률
- `CurrentSeason`, `CanGrow`: 계절 및 농사 가능 여부
- `PawnsSowing`, `PawnsHarvesting`: 현재 작업 중인 폰 수

**주요 메서드**:
- `GetCrisisLevel()`: 식량 위기 수준 (0=안전, 3=위기)
- `GetFarmUtilization()`: 농장 활용률 (0.0~1.0)

---

### 2. FoodAnalyzer (분석기)

**위치**: `Source/RimAI/Food/FoodAnalyzer.cs`

**역할**: 맵의 실제 상태를 스캔하여 ColonyFoodState 생성

**핵심 메서드**:
```csharp
public static ColonyFoodState AnalyzeMap(Map map)
```

**분석 항목**:
1. 식량 저장량 (생고기, 조리된 식사, 작물 등)
2. 콜로니스트 및 동물 수
3. 일일 소비량 및 생존 일수 계산
4. 농장 상태 (Zone_Growing 스캔)
5. 환경 정보 (계절, 온도, 겨울까지 일수)
6. 현재 작업 중인 폰 통계

**사용 예시**:
```csharp
var state = FoodAnalyzer.AnalyzeMap(map);
Log.Message($"생존 일수: {state.DaysUntilStarvation}일");
```

---

### 3. FoodDecisionEngine (의사결정 엔진)

**위치**: `Source/RimAI/Food/FoodDecisionEngine.cs`

**역할**: ColonyFoodState를 입력받아 작업 우선순위 결정

**핵심 메서드**:
```csharp
public static FoodDecision MakeDecision(ColonyFoodState state)
```

**의사결정 알고리즘**:

#### 단계 1: 즉각 대응 (식량 위기 레벨 기반)
```
생존 일수 < 1일  → 위기 (Critical) - 요리/사냥/수확 최우선
생존 일수 < 3일  → 경고 (High) - 식량 확보 우선
생존 일수 < 7일  → 주의 (Normal) - 보충 권장
생존 일수 >= 7일 → 안전 (Low) - 정상 운영
```

#### 단계 2: 중장기 대응 (농사 우선순위)
```
농장 활용률 < 50% → 파종 Critical
농장 활용률 < 90% → 파종 High
수확 가능 작물 > 콜로니스트*10 → 수확 High
```

#### 단계 3: 계절 고려
```
겨울까지 <= 5일  → 수확/저장 Critical
겨울까지 <= 15일 → 수확 우선순위 +1
봄(Spring) → 파종 우선순위 +1
```

#### 단계 4: 작업 균형
```
작업 중인 폰 >= 3명 → 해당 작업 우선순위 -1
```

#### 단계 5: 요리 균형
```
조리된 식사 비율 > 70% → 요리 우선순위 -1
조리된 식사 비율 < 30% && 생고기 > 5 → 요리 우선순위 +1
```

**출력**: `FoodDecision` 객체
- 각 작업의 우선순위 (None, Low, Normal, High, Critical)
- 의사결정 이유 (디버그용)

---

### 4. FoodManager (중앙 관리자)

**위치**: `Source/RimAI/Food/FoodManager.cs`

**역할**: GameComponent로 등록되어 주기적으로 업데이트 수행

**동작 방식**:
1. 매 5초마다 `UpdateAllMaps()` 호출
2. 각 맵에 대해:
   - FoodAnalyzer로 상태 분석
   - FoodDecisionEngine으로 의사결정
   - 결과를 캐시에 저장
3. Harmony 패치에서 캐시된 의사결정을 참조

**싱글톤 접근**:
```csharp
var manager = FoodManager.Instance;
var decision = manager?.GetDecision(map);
```

---

## Harmony 패치 전략

### 패치 위치 1: WorkGiver.ShouldSkip() (추천)

**대상 클래스**:
- `WorkGiver_GrowerSow` - 파종
- `WorkGiver_GrowerHarvest` - 수확
- `WorkGiver_HunterHunt` - 사냥
- `WorkGiver_DoBill` - 요리 (간접)

**패치 방식**: Postfix

**동작**:
```csharp
[HarmonyPatch(typeof(WorkGiver_GrowerSow), nameof(WorkGiver_GrowerSow.ShouldSkip))]
public static class WorkGiver_GrowerSow_ShouldSkip_Patch
{
    [HarmonyPostfix]
    public static void Postfix(ref bool __result, Pawn pawn)
    {
        var decision = FoodManager.Instance?.GetDecision(pawn.Map);

        if (decision?.SowingPriority == FoodWorkPriority.None)
            __result = true;  // 작업 스킵
        else if (decision?.SowingPriority == FoodWorkPriority.Critical)
            __result = false; // 무조건 활성화
    }
}
```

**장점**:
- 특정 작업만 선택적으로 활성화/비활성화 가능
- 게임 기본 로직 유지
- 다른 모드와 충돌 최소화

---

### 패치 위치 2: Pawn_WorkSettings.GetPriority() (보조)

**대상**: `Pawn_WorkSettings.GetPriority()`

**패치 방식**: Postfix

**동작**:
```csharp
[HarmonyPostfix]
public static void Postfix(ref int __result, WorkTypeDef w, Pawn pawn)
{
    var decision = FoodManager.Instance?.GetDecision(pawn.Map);

    if (w == WorkTypeDefOf.Growing)
    {
        int boost = GetPriorityBoost(decision.SowingPriority);
        __result = Mathf.Clamp(__result + boost, 1, 4);
    }
}
```

**우선순위 부스트 값**:
- Critical: +2
- High: +1
- Normal: 0
- Low: -1
- None: -2

**장점**:
- 플레이어의 수동 설정과 혼합 가능
- 더 세밀한 우선순위 조정

---

### 패치 위치 3: Game.FinalizeInit() (필수)

**대상**: `Game.FinalizeInit()`

**역할**: FoodManager를 GameComponent로 등록

```csharp
[HarmonyPostfix]
public static void Postfix(Game __instance)
{
    var manager = new FoodManager(__instance);
    __instance.components.Add(manager);
}
```

---

## 테스트 시나리오

### 시나리오 1: 식량 위기 상황
```
조건:
- 식량: 10 영양분
- 콜로니스트: 5명
- 생존 일수: 1.25일 (< 3일)

예상 결과:
- 요리: Critical
- 사냥: Critical
- 수확: Critical
- 파종: Normal (장기 대응)
```

### 시나리오 2: 농장 방치 상황
```
조건:
- 식량: 충분 (20일분)
- 농장: 100타일, 심어진 것: 30타일 (30%)
- 계절: 봄

예상 결과:
- 파종: Critical (활용률 < 50%)
- 요리: Low
- 사냥: Low
```

### 시나리오 3: 겨울 대비
```
조건:
- 겨울까지: 4일
- 수확 가능 작물: 50개

예상 결과:
- 수확: Critical
- 요리: High
- 파종: None (겨울 임박)
```

---

## 확장 가능성

### 2.1단계 추가 기능:
- 채집(Foraging) 작업 우선순위
- 야생 식물 수확 지시
- 냉장고 관리 (부패 방지)

### 2.2단계:
- 기아 상태 폰 우선 급식
- 식량 타입별 선호도 (단순 식사 vs 호화 식사)
- 동물 도살 자동화

### 3단계 연계:
- 건설 우선순위와 통합 (냉장고 건설 우선)
- 전투 중 식량 비축 전략

---

## 설정 옵션

`RimAI_Settings` 클래스를 통해 사용자 커스터마이징 가능:

```csharp
public static class RimAI_Settings
{
    public static bool EnableFoodAutomation = true;
    public static bool EnableDetailedLogging = true;

    // 향후 추가 가능:
    // public static float FarmUtilizationTarget = 0.9f;
    // public static int MaxWorkersPerTask = 3;
}
```

---

## 성능 고려사항

- **업데이트 주기**: 5초 (300틱) - 적절한 반응성과 성능 균형
- **캐시 사용**: 매 틱마다 재계산하지 않고 캐시 참조
- **조건부 로깅**: Dev 모드에서만 상세 로그 출력

---

## 디버그 방법

Dev 모드 활성화 후 게임 로드 시 로그 확인:

```
[RimAI] FoodManager 등록 완료
[RimAI] [FoodState] 영양분:125.3 | 소비:8.0/day | 버틸일:15.7일 | 위기레벨:0 | 농장:75/100 | 수확가능:12
[RimAI] [Decision] 파종:Normal | 수확:Normal | 요리:Low | 사냥:Low | 채집:None
이유: [계절] Spring, 겨울까지 45일
→ 봄: 파종 적기
```

---

## 참고: RimWorld WorkGiver 실행 순서

```
1. Pawn_JobTracker.TryFindAndStartJob()
   ↓
2. ThinkNode Tree 순회
   ↓
3. JobGiver_Work.TryIssueJobPackage()
   ↓
4. WorkGiver.ShouldSkip() 체크  ← 우리가 패치하는 지점
   ↓
5. WorkGiver.HasJobOnThing() / JobOnThing() 호출
   ↓
6. Job 반환 → JobDriver 실행
```

우리의 패치는 4번 단계에서 작업을 필터링하여,
불필요한 작업은 아예 시도하지 않도록 합니다.
