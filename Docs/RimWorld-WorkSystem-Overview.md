# RimWorld Work System 개요

## 1. RimWorld의 작업 시스템 구조

RimWorld의 AI 작업 시스템은 계층적으로 구성되어 있습니다:

```
Pawn (폰/개체)
  └─> JobDriver (작업 드라이버 - 실제 행동 실행)
       └─> Job (작업 - 구체적인 일)
            └─> WorkGiver (작업 제공자 - 가능한 일 찾기)
                 └─> WorkTypeDef (작업 타입 정의)
```

### 1.1 핵심 컴포넌트

#### **WorkTypeDef** (XML에서 정의)
- 작업의 카테고리 (예: `Growing`, `Cooking`, `Hunting`, `Plant cutting`)
- 우선순위 순서 및 활성화 여부
- 파일 위치: `RimWorld/Data/Core/Defs/WorkTypeDefs/WorkTypes.xml`

예시:
```xml
<WorkTypeDef>
  <defName>Growing</defName>
  <labelShort>grow</labelShort>
  <pawnLabel>grower</pawnLabel>
  <gerundLabel>growing</gerundLabel>
  <description>Sow and harvest crops.</description>
  <naturalPriority>3000</naturalPriority>
  <workTags>
    <li>PlantWork</li>
    <li>ManualDumb</li>
  </workTags>
</WorkTypeDef>
```

#### **WorkGiver** (C# 클래스)
- **역할**: 특정 작업 타입에서 폰이 할 수 있는 구체적인 일을 찾아줌
- **위치**: `RimWorld.WorkGiver_*` 클래스들
- **핵심 메서드**:
  - `HasJobOnThing(Pawn pawn, Thing thing)`: 특정 사물에 대해 작업 가능 여부
  - `JobOnThing(Pawn pawn, Thing thing)`: 실제 Job 객체 반환
  - `ShouldSkip(Pawn pawn)`: 이 WorkGiver를 건너뛸지 판단

**주요 식량 관련 WorkGiver들:**
- `WorkGiver_GrowerSow`: 농작물 파종
- `WorkGiver_GrowerHarvest`: 수확
- `WorkGiver_PlantsCut`: 식물 자르기
- `WorkGiver_CookFillHopper`: 요리용 재료 채우기
- `WorkGiver_DoBill`: 요리 작업 (Bill 시스템)
- `WorkGiver_HunterHunt`: 사냥

#### **Job** (작업 인스턴스)
- **역할**: 구체적인 작업 명령 (예: "A 타일에 감자 심기", "B 동물 사냥하기")
- **주요 속성**:
  - `def`: JobDef (작업 정의)
  - `targetA`, `targetB`, `targetC`: 작업 대상
  - `count`: 작업 횟수/수량

#### **JobDriver** (작업 실행)
- **역할**: Job을 실제로 수행하는 로직 (이동, 애니메이션, 완료 처리)
- **구조**: `IEnumerable<Toil>` - 작업 단계들의 시퀀스
- 예: `JobDriver_PlantSow` - 이동 → 파종 애니메이션 → 완료

---

## 2. 작업 우선순위 결정 과정

RimWorld는 매 틱마다 폰이 다음 작업을 선택할 때 다음 과정을 거칩니다:

```
1. Pawn_JobTracker.TryFindAndStartJob()
   ↓
2. ThinkNode Tree 순회 (우선순위 순으로)
   ↓
3. WorkGiver 순회 (WorkTypeDef의 naturalPriority + 플레이어 설정)
   ↓
4. 각 WorkGiver가 JobOnThing() 호출
   ↓
5. 첫 번째로 유효한 Job 반환 → 시작
```

### 2.1 ThinkTree 구조

RimWorld는 **ThinkTree**라는 우선순위 기반 결정 트리를 사용합니다.

주요 ThinkNode:
- `ThinkNode_Priority`: 자식 노드들을 순서대로 시도
- `ThinkNode_ConditionalPawn`: 조건부 실행
- `ThinkNode_JobGiver`: 실제 Job을 생성하는 노드
- `JobGiver_Work`: 일반 작업 할당 (WorkGiver 시스템 사용)

파일 위치: `RimWorld/Data/Core/Defs/ThinkTreeDefs/`

---

## 3. 식량/농사 관련 핵심 클래스

### 3.1 식량 관련 클래스

| 클래스 | 역할 | 네임스페이스 |
|--------|------|--------------|
| `FoodUtility` | 식량 관련 유틸리티 함수 | `RimWorld` |
| `NutritionWantedByPlantGrower` | 식물 재배자가 필요한 영양분 | `RimWorld` |
| `ThingDefOf.Meals_*` | 식사 타입 정의 | `RimWorld` |
| `Pawn_NeedsTracker` | 폰의 필요 사항 관리 | `Verse` |

### 3.2 농사 관련 클래스

| 클래스 | 역할 | 네임스페이스 |
|--------|------|--------------|
| `Zone_Growing` | 농장 구역 | `RimWorld` |
| `Plant` | 식물 객체 | `RimWorld` |
| `WorkGiver_GrowerSow` | 파종 작업 제공자 | `RimWorld` |
| `WorkGiver_GrowerHarvest` | 수확 작업 제공자 | `RimWorld` |
| `PlantUtility` | 식물 관련 유틸리티 | `RimWorld` |

---

## 4. RimAI가 개입할 수 있는 지점

### 4.1 **WorkGiver 우선순위 조정** (추천)
**방법**: `Pawn_WorkSettings.SetPriority()` 패치

Harmony 패치 위치:
```csharp
[HarmonyPatch(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.GetPriority))]
```

**장점**:
- 게임의 기존 시스템 활용
- 비침습적
- 다른 모드와 호환성 높음

**단점**:
- 세밀한 제어 어려움

### 4.2 **JobGiver_Work.TryIssueJobPackage() 패치** (권장)
**방법**: WorkGiver 선택 전에 개입하여 우선순위 재정렬

```csharp
[HarmonyPatch(typeof(JobGiver_Work), nameof(JobGiver_Work.TryIssueJobPackage))]
```

**장점**:
- WorkGiver 레벨에서 직접 제어
- 상황별 우선순위 조정 가능

**단점**:
- 내부 구조 의존

### 4.3 **커스텀 ThinkNode 추가** (고급)
**방법**: 새로운 ThinkNode를 XML로 정의하고 우선순위를 높게 설정

**장점**:
- 완전한 제어
- 독립적인 의사결정 시스템

**단점**:
- 복잡도 높음
- 다른 모드와 충돌 가능성

### 4.4 **WorkGiver 자체를 패치** (최선)
**방법**: 개별 WorkGiver의 `ShouldSkip()` 또는 `PotentialWorkThingsGlobal()` 패치

```csharp
[HarmonyPatch(typeof(WorkGiver_GrowerSow), nameof(WorkGiver_GrowerSow.ShouldSkip))]
[HarmonyPatch(typeof(WorkGiver_GrowerHarvest), nameof(WorkGiver_GrowerHarvest.ShouldSkip))]
```

**장점**:
- 세밀한 제어
- 특정 작업만 선택적 활성화/비활성화 가능

---

## 5. RimAI 2단계 권장 전략

### 접근 방법:
1. **분석 레이어**: 콜로니 상태를 주기적으로 분석 (FoodAnalyzer)
2. **의사결정 레이어**: 우선순위 계산 (FoodDecisionEngine)
3. **적용 레이어**: WorkGiver.ShouldSkip() 패치로 작업 활성화/비활성화

### 패치 대상:
- `WorkGiver_GrowerSow.ShouldSkip()` - 파종 제어
- `WorkGiver_GrowerHarvest.ShouldSkip()` - 수확 제어
- `WorkGiver_PlantsCut.ShouldSkip()` - 수확 후 정리
- `WorkGiver_DoBill` (요리대) - 간접 제어 (Bill 우선순위)

### 주기적 업데이트:
```csharp
[HarmonyPatch(typeof(TickManager), nameof(TickManager.DoSingleTick))]
// 또는
[HarmonyPatch(typeof(Map), nameof(Map.MapUpdate))]
```

매 N틱마다 콜로니 상태 재분석 및 우선순위 갱신
