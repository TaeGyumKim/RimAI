# RimAI 생산 자동화 시스템 설계 문서

## 1. 개요

### 목표
작업대(WorkTable)의 제작 Bill을 자동으로 생성/조정/중단하여, 콜로니의 생산을 완전히 관람용으로 자동화합니다.

### 핵심 원칙
1. **생존 우선**: 무기, 갑옷, 겨울옷 등 필수 아이템 우선 생산
2. **자원 보호**: 과도한 생산으로 자원 고갈 방지
3. **플레이 스타일 반영**: Fortress는 방어구 중심, Agricultural은 의류/식량 중심
4. **관람 경험**: 의미 있는 결정은 한국어 스토리 로그로 표시

---

## 2. 생산 자동화 범위

### 2.1 다룰 작업대 종류

| 작업대 | 주요 생산 아이템 | 우선순위 |
|--------|-----------------|---------|
| **CraftingSpot** (수작업) | 활, 나무 클럽, 간단한 옷 | 낮음 |
| **TableMachining** (제작대) | 총, 수류탄, 컴포넌트 | 높음 |
| **TableSmithy** (대장간) | 근접 무기, 갑옷 | 높음 |
| **ElectricTailorBench** (재봉대) | 의류, 방어구 | 중간 |
| **DrugLab** (약물 실험실) | 의약품 | 중간 |
| **ElectricSmelter** (용광로) | 강철 재활용 | 낮음 |

### 2.2 생산 카테고리

#### A. 필수 생산 (생존 직결)
- **무기**: 콜로니스트당 1개 이상 (원거리 무기 우선)
- **방어구**: 플레이트 아머, 플랙 재킷 등 (Fortress 스타일에서 중요)
- **겨울옷**: 겨울 30일 전부터 준비 (방한복, 파카)
- **의약품**: 콜로니스트당 10개 이상 비축

#### B. 품질 관리 생산
- **의류**: 품질 "보통" 이상만 유지, 낡은 옷 교체
- **방어구**: 품질 "좋음" 이상 목표 (생존율 증가)
- **무기**: 품질 "보통" 이상

#### C. 자원 재활용
- **용광로**: 무기/갑옷 녹여서 강철 확보
- **부품 제작**: 컴포넌트 부족 시 강철로 제작

---

## 3. RimAIAction 정의

### 3.1 새 액션 타입 추가

```csharp
public enum RimAIActionType
{
    // 기존 액션들...

    /// <summary>작업대에 Bill 추가</summary>
    CreateBill,

    /// <summary>기존 Bill 수정 (목표 수량, 반복 조건 등)</summary>
    ModifyBill,

    /// <summary>Bill 일시 중단/재개</summary>
    PauseBill,

    /// <summary>Bill 삭제</summary>
    DeleteBill,
}
```

### 3.2 ProductionAction 커스텀 데이터

```csharp
public class ProductionActionData
{
    /// <summary>대상 작업대</summary>
    public Building_WorkTable WorkTable { get; set; }

    /// <summary>레시피 Def</summary>
    public RecipeDef Recipe { get; set; }

    /// <summary>목표 수량 (-1 = 무한 반복)</summary>
    public int TargetCount { get; set; } = -1;

    /// <summary>일시 중단 여부</summary>
    public bool Paused { get; set; } = false;

    /// <summary>기존 Bill (수정/삭제 시)</summary>
    public Bill Existing Bill { get; set; }

    /// <summary>품질 요구사항 (의류/무기용)</summary>
    public QualityCategory MinQuality { get; set; } = QualityCategory.Normal;

    /// <summary>재질 요구사항 (강철, 천 등)</summary>
    public ThingDef StuffDef { get; set; }
}
```

---

## 4. ProductionState 설계

### 4.1 데이터 구조

```csharp
public class ColonyProductionState
{
    // === 작업대 정보 ===
    public List<WorkTableInfo> WorkTables { get; set; }

    // === 아이템 재고 ===
    public Dictionary<ThingDef, int> ItemInventory { get; set; }

    // === 무기 재고 ===
    public int MeleeWeapons { get; set; }
    public int RangedWeapons { get; set; }
    public int ColonistsWithWeapons { get; set; }

    // === 방어구 재고 ===
    public int Armors { get; set; }
    public int ColonistsWithArmor { get; set; }

    // === 의류 재고 ===
    public int WinterClothes { get; set; }
    public int SummerClothes { get; set; }
    public int TatteredClothes { get; set; } // 낡은 옷

    // === 자원 재고 ===
    public int AvailableSteel { get; set; }
    public int AvailableComponents { get; set; }
    public int AvailableCloth { get; set; }
    public int AvailableLeather { get; set; }
    public int Medicine { get; set; }

    // === 시즌 정보 ===
    public Season CurrentSeason { get; set; }
    public int DaysUntilWinter { get; set; }

    // === 콜로니스트 정보 ===
    public int ColonistCount { get; set; }

    // === 헬퍼 메서드 ===
    public bool NeedMoreWeapons() => ColonistsWithWeapons < ColonistCount;
    public bool NeedMoreArmor() => ColonistsWithArmor < ColonistCount / 2;
    public bool NeedWinterPreparation() => DaysUntilWinter < 30 && WinterClothes < ColonistCount;
    public bool IsResourceScarce() => AvailableSteel < 100 || AvailableComponents < 5;
}

public class WorkTableInfo
{
    public Building_WorkTable Building { get; set; }
    public string DefName { get; set; }
    public List<Bill> Bills { get; set; }
    public bool IsPowered { get; set; } // 전력 필요 시 작동 중인지
    public int QueuedWork { get; set; } // 대기 중인 작업 수
}
```

---

## 5. ProductionAnalyzer 설계

### 5.1 분석 로직

```csharp
public static ColonyProductionState AnalyzeMap(Map map)
{
    var state = new ColonyProductionState();

    // 1. 작업대 스캔
    AnalyzeWorkTables(map, state);

    // 2. 무기/방어구/의류 재고 분석
    AnalyzeEquipment(map, state);

    // 3. 자원 재고 분석
    AnalyzeResources(map, state);

    // 4. 시즌 정보
    AnalyzeSeason(map, state);

    // 5. 콜로니스트 상태
    state.ColonistCount = map.mapPawns.FreeColonistsSpawnedCount;

    return state;
}
```

### 5.2 성능 최적화

- **ResourceCounter 사용**: O(1) 자원 조회
- **Lister 활용**: `map.listerThings.ThingsInGroup()` 사용
- **캐싱**: 작업대 목록은 변경이 드물므로 캐싱 고려
- **조기 종료**: 필요한 정보만 수집

---

## 6. ProductionSubsystem 로직

### 6.1 의사결정 흐름

```
Update(Map map)
  ↓
AnalyzeMap() → ProductionState
  ↓
위기 상황 체크
  - 무기 부족? → 긴급 무기 생산
  - 겨울 임박? → 겨울옷 생산
  - 자원 부족? → 비필수 Bill 중단
  ↓
GetProposedActions()
  - Priority 계산 (생존 > 품질 > 편의)
  - 자원 가용성 체크
  - 플레이 스타일 반영
  ↓
RimAIManager 승인
  ↓
ExecuteAction()
  - Bill 생성/수정/삭제/중단
  - 스토리 로그 출력
```

### 6.2 우선순위 휴리스틱

| 조건 | 우선순위 | 생산 아이템 |
|------|----------|------------|
| 콜로니스트 무기 없음 | Critical | 총, 활 |
| 겨울 20일 전 + 겨울옷 부족 | High | 파카, 방한복 |
| 콜로니스트 절반 이상 갑옷 없음 | High | 플레이트 아머 |
| 의약품 < 10개 | High | 의약품 |
| 의류 낡음 | Normal | 셔츠, 바지 |
| 자원 여유 + 품질 개선 | Low | 고급 의류, 예술품 |

### 6.3 자원 관리

#### 자원 부족 시 (강철 < 100, 컴포넌트 < 5)
- 비필수 Bill 일시 중단
- 긴급 생산 (무기, 방어구)만 유지
- 스토리 로그: "자원 부족으로 비필수 생산을 중단합니다."

#### 자원 여유 시 (강철 > 500, 컴포넌트 > 20)
- 품질 개선 생산 (좋은 품질 이상)
- 예비 무기/갑옷 생산
- 스토리 로그: "자원 여유로 고품질 장비 생산을 시작합니다."

---

## 7. RimWorld Bill API 연동

### 7.1 주요 API

```csharp
// Bill 생성
Bill_Production bill = new Bill_Production(recipeDef);
workTable.billStack.AddBill(bill);

// Bill 설정
bill.repeatMode = BillRepeatModeDefOf.TargetCount;
bill.targetCount = 10;
bill.pauseWhenSatisfied = true;
bill.unpauseWhenYouHave = 5;

// 품질 필터
bill.qualityRange = new QualityRange(QualityCategory.Normal, QualityCategory.Legendary);

// 재질 필터
bill.ingredientFilter.SetAllow(ThingDefOf.Steel, true);

// Bill 삭제
workTable.billStack.Delete(bill);

// Bill 일시 중단
bill.suspended = true;
```

### 7.2 Bill 검색

```csharp
// 특정 레시피의 Bill 찾기
Bill existingBill = workTable.billStack.Bills
    .FirstOrDefault(b => b.recipe == recipeDef);

// 모든 작업대의 특정 레시피 Bill 찾기
var allBills = map.listerBuildings.allBuildingsColonist
    .OfType<Building_WorkTable>()
    .SelectMany(wt => wt.billStack.Bills)
    .Where(b => b.recipe.defName.Contains("Apparel"));
```

---

## 8. 설정/프리셋 통합

### 8.1 개입 강도 레벨

| 강도 | Bill 생성 | Bill 수정 | Bill 중단/삭제 | 설명 |
|------|----------|----------|---------------|------|
| **Off** | ❌ | ❌ | ❌ | 생산 자동화 안 함 |
| **Low** | ❌ | ✅ (목표 수량만) | ❌ | 기존 Bill만 조정 |
| **Medium** | ✅ (필수만) | ✅ | ✅ (일시 중단만) | 무기/갑옷/겨울옷만 생성 |
| **High** | ✅ | ✅ | ✅ | 모든 생산 관리 |
| **Full** | ✅ | ✅ | ✅ (삭제 포함) | 완전 자동화 |

### 8.2 플레이 스타일별 편향

#### Fortress (요새)
- 방어구 생산 2배
- 무기 생산 1.5배
- 의류 생산 0.8배 (최소한만)

#### Agricultural (농업)
- 의류 생산 1.5배 (농부 옷)
- 의약품 생산 1.2배
- 무기 생산 0.8배

#### Nomadic (유목)
- 가벼운 무기 선호 (활, 권총)
- 무거운 갑옷 회피
- 빠른 생산 (목표 수량 적게)

#### Researcher (연구)
- 고급 무기 선호 (충전 라이플)
- 컴포넌트 비축
- 품질 "좋음" 이상 목표

#### Balanced (균형)
- 모든 카테고리 균등 생산

---

## 9. 스토리 로그 메시지

### 9.1 생산 결정

```csharp
StoryLogger.Production.WeaponShortage(int needed)
→ "[RimAI 스토리] [플레이스타일] 무기가 부족합니다! {needed}개 생산을 시작합니다."

StoryLogger.Production.WinterPreparation(int daysLeft)
→ "[RimAI 스토리] 겨울 대비를 위해 겨울 옷 생산 Bill을 추가합니다. ({daysLeft}일 남음)"

StoryLogger.Production.ResourceShortage(string resourceName)
→ "[RimAI 스토리] {resourceName} 부족으로 비필수 생산을 일시 중단합니다."

StoryLogger.Production.QualityUpgrade(string itemType)
→ "[RimAI 스토리] 자원 여유로 고품질 {itemType} 생산을 시작합니다."

StoryLogger.Production.ArmorProduction(int count)
→ "[RimAI 스토리] [요새] 방어력 강화! 갑옷 {count}벌 생산을 계획합니다."
```

### 9.2 생산 중단

```csharp
StoryLogger.Production.PausedDueToResources(string billName)
→ "[RimAI 스토리] 자원 부족으로 '{billName}' 생산을 일시 중단합니다."

StoryLogger.Production.ResumedProduction(string billName)
→ "[RimAI 스토리] 자원 확보! '{billName}' 생산을 재개합니다."
```

---

## 10. 구현 단계

### Phase 1: 기본 구현 (현재 구현 목표)
- ✅ ProductionState 정의
- ✅ ProductionAnalyzer 구현
- ✅ ProductionSubsystem 구현
- ✅ Bill 생성/수정/중단 API 연동
- ✅ 설정/로그 통합

### Phase 2: 고급 기능 (향후 확장)
- 품질별 필터링 (좋은 품질 이상만 보관)
- 재질 최적화 (강철 vs 플라스틸)
- 스킬 기반 작업 할당 (높은 스킬 폰 우선)
- 시즌별 의류 자동 전환 (여름옷 ↔ 겨울옷)
- 예술품/가구 생산 (콜로니 무드 관리)

---

## 11. 테스트 체크리스트

### 기본 동작
- [ ] 새 게임 시작 → 제작대 건설 → 무기 Bill 자동 생성
- [ ] 무기 부족 → Bill 생성 → 스토리 로그 확인
- [ ] 자원 부족 → 비필수 Bill 중단 확인
- [ ] 겨울 30일 전 → 겨울옷 Bill 생성 확인

### 플레이 스타일
- [ ] Fortress: 방어구 생산 2배 확인
- [ ] Agricultural: 의류 생산 1.5배 확인
- [ ] Nomadic: 최소 생산 확인

### 예외 상황
- [ ] 작업대 파괴 시 오류 없이 처리
- [ ] 전력 부족 시 Bill 일시 중단
- [ ] 레시피 연구 미완료 시 Bill 생성 안 함

### 성능
- [ ] 12시간 플레이 → 메모리 누수 없음
- [ ] Update() 호출 시간 < 50ms

---

## 12. 향후 확장 아이디어

### 스마트 생산
- AI가 전투 패턴 분석 → 적 무기에 맞는 갑옷 생산
- 계절 변화 예측 → 미리 의류 준비
- 트레이더 도착 예상 → 판매용 아이템 생산

### 생산 체인 최적화
- 강철 부족 → 용광로 가동 → 무기 재활용
- 컴포넌트 부족 → 제작대 Bill 자동 생성
- 천 부족 → 양모 수확 우선순위 상승

### UI 통합
- 생산 현황 대시보드
- Bill 자동화 상태 표시
- 수동 개입 옵션 (특정 Bill은 자동화 제외)

---

**설계 문서 버전**: 1.0
**작성일**: 2025-11-21
**Stage**: 8 - Production Automation
