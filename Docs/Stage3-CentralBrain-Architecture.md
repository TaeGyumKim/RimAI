# 3단계: 중앙 브레인 아키텍처 및 건설/연구 자동화

## 개요

3단계에서는 RimAI를 **모듈형 서브시스템 아키텍처**로 재구성하고, 건설/확장 및 연구 자동화를 추가합니다.

### 주요 변경사항
1. **중앙 브레인 시스템** (`RimAIManager`) 도입
2. **서브시스템 아키텍처** - 모든 기능을 독립적인 서브시스템으로 분리
3. **건설/확장 자동화** - 기본 인프라 자동 구축
4. **연구 자동화** - 상황에 맞는 연구 자동 선택
5. **쿨다운 시스템** - 방치 모드에 적합한 느긋한 자동화

---

## 아키텍처 개요

```
┌───────────────────────────────────────────────────────────┐
│                   RimAIManager (중앙 브레인)                │
│  - GameComponent로 등록                                    │
│  - 모든 서브시스템 관리                                     │
│  - 액션 수집, 우선순위 조정, 충돌 해결, 실행                │
└─────────────┬─────────────────────────────────────────────┘
              │
      ┌───────┴────────┬──────────────┬────────────┐
      ▼                ▼              ▼            ▼
┌──────────────┐ ┌──────────────┐ ┌──────────┐ ┌────────┐
│FoodSubsystem │ │Construction  │ │Research  │ │ ...    │
│              │ │Subsystem     │ │Subsystem │ │        │
│우선순위: 100 │ │우선순위: 50  │ │우선순위:30│ │        │
└──────────────┘ └──────────────┘ └──────────┘ └────────┘
```

---

## 1. 핵심 인터페이스 및 데이터 구조

### 1.1 IRimAISubsystem (인터페이스)

**위치**: `Source/RimAI/Core/IRimAISubsystem.cs`

모든 자동화 서브시스템이 구현해야 하는 공통 인터페이스:

```csharp
public interface IRimAISubsystem
{
    string Name { get; }              // 서브시스템 이름
    int Priority { get; }             // 우선순위 (높을수록 우선)
    bool Enabled { get; set; }        // 활성화 여부

    void Initialize();                // 초기화
    void Update(Map map);             // 주기적 업데이트
    List<RimAIAction> GetProposedActions(Map map); // 액션 제안
    void ExecuteAction(RimAIAction action);        // 액션 실행
    void ExposeData();                // 세이브/로드
    string GetDebugInfo(Map map);     // 디버그 정보
}
```

**RimAISubsystemBase** 추상 클래스 제공:
- 공통 로직 (틱 카운터, 업데이트 간격 등)
- 상속 받아 사용하면 편리

---

### 1.2 RimAIAction (액션 데이터 구조)

**위치**: `Source/RimAI/Core/RimAIAction.cs`

서브시스템이 제안하고 중앙 브레인이 실행하는 액션:

```csharp
public class RimAIAction
{
    RimAIActionType Type;            // 액션 타입
    RimAIActionPriority Priority;    // 우선순위
    string SourceSubsystem;          // 제안 서브시스템
    Map? TargetMap;                  // 대상 맵
    IntVec3? TargetCell;             // 대상 위치
    ThingDef? TargetThingDef;        // 대상 건물
    ResearchProjectDef? TargetResearch; // 대상 연구
    string Description;              // 설명

    bool IsValid();                  // 유효성 검증
}
```

**액션 타입**:
- `AdjustFoodPriority` - 식량 작업 우선순위 조정
- `PlaceBlueprint` - 청사진 배치
- `ConstructBuilding` - 건물 건설
- `SelectResearch` - 연구 선택
- `EmergencyResponse` - 위기 대응

---

## 2. RimAIManager (중앙 브레인)

**위치**: `Source/RimAI/Core/RimAIManager.cs`

**역할**:
1. 모든 서브시스템 등록 및 관리
2. 매 틱마다 서브시스템 업데이트
3. 액션 수집 및 우선순위 정렬
4. 충돌 해결 (같은 위치에 여러 건설 등)
5. 액션 실행

**동작 흐름**:

```
1초마다:
  └─> ProcessAllMaps()
       ├─> 각 맵별로:
       │    ├─> 모든 서브시스템 Update(map)
       │    ├─> GetProposedActions(map)로 액션 수집
       │    ├─> 우선순위 정렬 및 충돌 해결
       │    └─> ExecuteAction() 실행 (최대 5개/틱)
       └─> 끝
```

**우선순위 조정 규칙**:
```
1. 액션 우선순위 (Critical > High > Normal > Low > VeryLow)
2. 서브시스템 우선순위 (Food:100 > Construction:50 > Research:30)
3. 충돌 해결 (같은 위치는 먼저 온 액션 선택)
```

**싱글톤 접근**:
```csharp
var manager = RimAIManager.Instance;
var foodSubsystem = manager?.GetSubsystem<FoodSubsystem>();
```

---

## 3. 서브시스템 상세

### 3.1 FoodSubsystem (식량)

**우선순위**: 100 (가장 높음 - 생존 필수)

**역할**:
- 기존 FoodManager 로직을 IRimAISubsystem으로 리팩토링
- 식량 상태 분석 및 작업 우선순위 결정
- WorkGiver 패치를 통해 작동 (액션 제안은 최소)

**주요 특징**:
- 5초마다 업데이트
- Critical 상황 시 긴급 액션 생성
- Harmony 패치로 직접 WorkGiver 제어

---

### 3.2 ConstructionSubsystem (건설/확장)

**우선순위**: 50 (중간)

**위치**: `Source/RimAI/Construction/ConstructionSubsystem.cs`

**역할**:
- 기본 인프라 자동 구축 (침대, 주방, 창고)
- 방어 시설 자동 배치
- 거주 구역 확장

**분석 항목** (`ConstructionAnalyzer.cs`):
```csharp
- 침대 수 / 콜로니스트 수
- 주방, 작업장, 연구대, 창고 유무
- 방어 구조물 (샌드백, 벽, 터렛) 수
- 건설 중인 청사진/프레임 수
- 자원 (목재, 철, 돌) 보유량
```

**의사결정 로직**:
```
1. 침대 부족 (침대 < 콜로니스트 + 2) → 침대 건설 High
2. 주방 없음 → 요리대 건설 High
3. 창고 없음 → 스톡파일 구역 생성 Normal
4. 방어 부족 (방어구조물 < 콜로니스트*2) → 샌드백 건설 Normal
```

**쿨다운 시스템**:
- **1000초 (약 16분)마다 한 번만 건설**
- 화면이 산만해지지 않도록 "큰 변화는 드물게"
- `lastConstructionTick` 딕셔너리로 맵별 관리

**실행 예시**:
```csharp
// 현재는 로그만 출력 (TODO: 실제 청사진 배치 구현)
Log.Message("[RimAI-Construction] 침대 부족 - 침대 건설 필요");

// 향후 구현:
// GenConstruct.PlaceBlueprintForBuild(ThingDefOf.Bed, position, map, ...);
```

---

### 3.3 ResearchSubsystem (연구)

**우선순위**: 30 (낮음 - 생존/건설 이후)

**위치**: `Source/RimAI/Research/ResearchSubsystem.cs`

**역할**:
- 현재 연구 중인 프로젝트가 없을 때 자동 선택
- 콜로니 상태에 맞는 최적 연구 결정

**의사결정 휴리스틱** (점수 기반):

```python
점수 = 기본점수(1000/(비용+100)) + 카테고리 보너스

카테고리 보너스:
- 식량 관련 (Food, Farm, Hydroponic): +100
- 전기 관련 (Electric, Battery, Solar): +90
- 의료 관련 (Medicine, Hospital): +80
- 방어 관련 (Turret, Defense, Armor): +60
- 현재 기술 레벨에 맞음: +50
- 기초 연구 (선행 연구 ≤1): +40
```

**예시**:
```
사용 가능 연구:
1. Hydroponics (수경재배) - 175점 (식량+100 + 전기+90 ...)
2. Solar Panel (태양 전지) - 150점 (전기+90 ...)
3. Gun Turret (터렛) - 120점 (방어+60 ...)

→ Hydroponics 선택!
```

**쿨다운 시스템**:
- **500초 (약 8분)마다 한 번만 연구 변경**
- 연구가 완료되면 다음 연구 자동 시작
- 현재 연구 중이면 변경하지 않음

---

## 4. 우선순위 및 충돌 해결

### 4.1 서브시스템 우선순위

```
FoodSubsystem       100  (생존 최우선)
ConstructionSubsystem 50  (인프라 구축)
ResearchSubsystem     30  (장기 발전)
```

### 4.2 액션 우선순위

```
Critical (4) - 긴급 (식량 위기, 생명 위협)
High     (3) - 높음 (침대 부족, 주방 없음)
Normal   (2) - 보통 (방어 구축, 연구 선택)
Low      (1) - 낮음
VeryLow  (0) - 매우 낮음
```

### 4.3 충돌 해결 알고리즘

**RimAIManager.ResolveConflicts()**:

```csharp
1. 액션 우선순위 순으로 정렬 (Critical → VeryLow)
2. 같은 우선순위면 서브시스템 우선순위로 정렬
3. 건설 액션의 경우:
   - 같은 위치에 여러 건설 액션 → 첫 번째만 선택
   - occupiedCells 해시셋으로 중복 제거
4. 최종 정렬된 액션 리스트 반환
```

---

## 5. 쿨다운 시스템 (방치 모드 최적화)

### 5.1 설계 철학

> **"큰 변화는 드물게, 하지만 의미 있게"**

사용자가 게임을 방치하고 관람할 때, 화면이 너무 산만하면 안 됨:
- ✅ 천천히 성장하는 콜로니
- ❌ 매 초마다 건물이 생기고 사라지는 혼란

### 5.2 쿨다운 설정

| 서브시스템 | 업데이트 간격 | 액션 쿨다운 | 비고 |
|-----------|--------------|------------|------|
| Food | 5초 | 없음 | WorkGiver로 즉각 반응 |
| Construction | 10초 | 1000초 (16분) | 건설은 드물게 |
| Research | 20초 | 500초 (8분) | 연구 변경은 느긋하게 |

### 5.3 쿨다운 구현

```csharp
// 맵별 마지막 실행 틱 저장
private Dictionary<Map, int> lastConstructionTick = new Dictionary<Map, int>();
private const int CONSTRUCTION_COOLDOWN = 60000; // 틱

// 실행 가능 여부 확인
private bool CanConstruct(Map map)
{
    if (!lastConstructionTick.TryGetValue(map, out var lastTick))
        return true; // 처음 실행

    int currentTick = Find.TickManager.TicksGame;
    return (currentTick - lastTick) >= CONSTRUCTION_COOLDOWN;
}

// 실행 후 쿨다운 설정
lastConstructionTick[map] = Find.TickManager.TicksGame;
```

---

## 6. Harmony 패치 업데이트

### 6.1 Game 초기화 패치

**위치**: `Source/RimAI/Core/Patches/GamePatches.cs`

**변경 사항**:
- 기존: `FoodManager` 단독 등록
- 신규: `RimAIManager` 등록 + 모든 서브시스템 자동 등록

```csharp
[HarmonyPatch(typeof(Game), nameof(Game.FinalizeInit))]
public static class Game_FinalizeInit_RimAIManager_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Game __instance)
    {
        var manager = new RimAIManager(__instance);
        __instance.components.Add(manager);

        // 모든 서브시스템 등록
        manager.RegisterSubsystem(new FoodSubsystem());
        manager.RegisterSubsystem(new ConstructionSubsystem());
        manager.RegisterSubsystem(new ResearchSubsystem());
    }
}
```

### 6.2 Food WorkGiver 패치 업데이트

**위치**: `Source/RimAI/Food/Patches/WorkGiverPatches.cs`

**변경 사항**:
```csharp
// 기존
var manager = FoodManager.Instance;
var decision = manager.GetDecision(pawn.Map);

// 신규
var manager = RimAIManager.Instance;
var foodSubsystem = manager.GetSubsystem<FoodSubsystem>();
var decision = foodSubsystem.GetDecision(pawn.Map);
```

---

## 7. 파일 구조

```
RimAI/
├── Source/RimAI/
│   ├── Core/
│   │   ├── IRimAISubsystem.cs         # 서브시스템 인터페이스
│   │   ├── RimAIAction.cs             # 액션 데이터 구조
│   │   ├── RimAIManager.cs            # 중앙 브레인
│   │   ├── ColonyScanner.cs           # 기존 스캐너
│   │   └── Patches/
│   │       └── GamePatches.cs         # 게임 초기화 패치
│   │
│   ├── Food/
│   │   ├── FoodSubsystem.cs           # (리팩토링)
│   │   ├── FoodState.cs
│   │   ├── FoodAnalyzer.cs
│   │   ├── FoodDecisionEngine.cs
│   │   └── Patches/
│   │       └── WorkGiverPatches.cs    # (업데이트)
│   │
│   ├── Construction/
│   │   ├── ConstructionSubsystem.cs   # 건설 서브시스템
│   │   ├── ConstructionState.cs       # 상태 데이터
│   │   └── ConstructionAnalyzer.cs    # 상태 분석기
│   │
│   └── Research/
│       └── ResearchSubsystem.cs       # 연구 서브시스템
│
└── Docs/
    └── Stage3-CentralBrain-Architecture.md  # 이 문서
```

---

## 8. 사용 예시

### 8.1 새 서브시스템 추가

```csharp
// 1. IRimAISubsystem 구현
public class TradeSubsystem : RimAISubsystemBase
{
    public override string Name => "Trade";
    public override int Priority => 40;

    public override void Update(Map map) { /* 분석 */ }
    public override List<RimAIAction> GetProposedActions(Map map) { /* 액션 제안 */ }
    public override void ExecuteAction(RimAIAction action) { /* 실행 */ }
}

// 2. RimAIManager에 등록 (GamePatches.cs)
manager.RegisterSubsystem(new TradeSubsystem());
```

### 8.2 디버그 정보 확인

```csharp
var manager = RimAIManager.Instance;
Log.Message(manager.GetDebugInfo());

// 출력:
// [RimAI Manager]
// 등록된 서브시스템: 3
//   - Food (우선순위: 100, 활성: True)
//   - Construction (우선순위: 50, 활성: True)
//   - Research (우선순위: 30, 활성: True)
//
// 최근 실행된 액션: 5개
//   - [Food] EmergencyResponse - 식량 위기! 생존 1.2일
//   - [Construction] ConstructBuilding - 침대 부족...
```

---

## 9. 향후 확장 계획

### 9.1 Construction 완성
- [ ] 실제 청사진 배치 로직 구현
- [ ] 적절한 위치 찾기 알고리즘
- [ ] 방 레이아웃 플래너

### 9.2 Research 개선
- [ ] 더 정교한 우선순위 휴리스틱
- [ ] 기술 트리 분석 (장기 목표 설정)
- [ ] 연구 완료 시 연계 건설 (예: 수경재배 연구 완료 → 수경재배 시설 건설)

### 9.3 새 서브시스템
- [ ] TradeSubsystem - 자동 교역
- [ ] CombatSubsystem - 전투 대응
- [ ] MedicalSubsystem - 의료 관리

### 9.4 사용자 설정
- [ ] Mod Settings UI
- [ ] 서브시스템별 활성화/비활성화
- [ ] 쿨다운 시간 조정
- [ ] 우선순위 커스터마이징

---

## 10. 테스트 시나리오

### 시나리오 1: 신규 콜로니 시작

```
초기 상태:
- 콜로니스트: 3명
- 침대: 0개
- 식량: 50 영양분
- 주방: 없음
- 연구: 없음

예상 액션 순서:
1. [Food-Critical] 식량 보충 (생존 일수 3일)
2. [Construction-High] 침대 3개 건설
3. [Construction-High] 요리대 건설
4. [Research-Normal] Hydroponics 연구 시작
5. (16분 후) [Construction-Normal] 샌드백 건설
```

### 시나리오 2: 방치 모드 관람

```
플레이어가 화면을 보면서 방치:

0분: 게임 시작
5분: 식량 작업 우선순위 자동 조정 (즉각)
16분: 침대 건설 (쿨다운 완료)
24분: 연구 변경 (Hydroponics → Solar Panel)
32분: 샌드백 건설 (쿨다운 완료)

→ 느긋하게 천천히 성장하는 콜로니 관람 가능
```

---

## 11. 설정 옵션

```csharp
public static class RimAI_Settings
{
    public static bool EnableDetailedLogging = true;

    // 서브시스템별 활성화
    public static bool EnableFoodAutomation = true;
    public static bool EnableConstructionAutomation = true;
    public static bool EnableResearchAutomation = true;
}
```

---

**3단계 완료!**

이제 RimAI는:
- ✅ 식량 자동 관리
- ✅ 건설/확장 자동 진행
- ✅ 연구 자동 선택
- ✅ 방치 모드에 적합한 느긋한 자동화

콜로니가 플레이어 없이도 서서히 성장할 수 있습니다! 🎉
