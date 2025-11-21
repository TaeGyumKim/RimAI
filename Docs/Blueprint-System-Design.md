# RimAI 청사진 시스템 설계 문서

## 개요
RimAI가 실제로 청사진을 배치하여 콜로니를 자동으로 건설하는 시스템의 설계 문서입니다.
플레이어가 아무것도 하지 않아도 최소한의 기초 인프라가 자동으로 생성되도록 합니다.

**목표**: "죽지 않고 자동으로 굴러가는 콜로니"를 넘어 "스스로 확장하는 콜로니"

---

## 📋 요구사항

### 최소 기능
1. **기본 침실**: 침대 + 벽 + 문 (3x4 크기)
2. **식당/공용공간**: 테이블 + 의자 4개 (6x6 크기)
3. **간단한 창고**: 벽으로 둘러싸인 스톡파일 구역 (5x5 크기)
4. **기초 방어**: 벽/샌드백 라인 (외곽 방어선)

### 제약사항
1. **지형 체크**: 물/깊은 물/건설 불가 지형 피하기
2. **충돌 방지**: 기존 건물/청사진과 겹치지 않도록
3. **접근성**: 문으로 연결, 도달 불가능한 곳에 건설 X
4. **자원 확인**: 건설에 필요한 자원이 충분한지 확인

### 성능/UX 제약
1. **쿨다운**: 대규모 건설은 in-game 며칠마다만 (현재 16분)
2. **개입 강도**: RimAISettings.constructionIntensity에 따라 건설량 조절
3. **플레이 스타일**: Fortress는 방어 3배, Agricultural은 창고 2배 등

---

## 🏗️ 시스템 아키텍처

### 데이터 흐름

```
1. ColonyScanner/ConstructionAnalyzer
   ↓ (상태 분석)
2. ConstructionSubsystem.GetProposedActions()
   ↓ (필요 구조물 목록 생성)
3. BuildingPlacer (위치 찾기 알고리즘)
   ↓ (최적 위치 계산)
4. BlueprintExecutor (청사진 배치)
   ↓ (RimWorld API 호출)
5. GenConstruct.PlaceBlueprintForBuild()
   ↓ (실제 청사진 생성)
6. 콜로니스트가 건설 작업 수행
```

### 주요 컴포넌트

#### 1. BuildingPlacer (위치 찾기)
- **역할**: 건물을 배치할 최적 위치 찾기
- **입력**: Map, 건물 타입, 크기, 제약조건
- **출력**: IntVec3 (셀 좌표) 또는 null
- **전략 패턴**: 건물 타입별 다른 전략 사용 가능

#### 2. BlueprintExecutor (청사진 배치)
- **역할**: RimWorld API를 사용하여 실제 청사진 배치
- **입력**: Map, ThingDef, 위치, 재질, 회전
- **출력**: bool (성공/실패)
- **책임**: 자원 확인, 충돌 체크, API 호출

#### 3. BuildingTemplate (건물 템플릿)
- **역할**: 사전 정의된 건물 레이아웃 저장
- **종류**: 침실, 식당, 창고, 방어선
- **구조**: 벽/문/가구 위치의 상대 좌표 목록

---

## 🎯 구현 전략

### Phase 1: 단순 건물 배치 (현재 목표)
- 단일 건물 (침대, 테이블, 샌드백)
- 간단한 위치 찾기 (거점 근처 빈 공간)
- 플레이 스타일 무시 (기본값만)

### Phase 2: 방/구역 배치
- 벽으로 둘러싸인 방 생성
- 복합 구조물 (침실 = 벽 + 문 + 침대)
- 스톡파일 구역 자동 생성

### Phase 3: 스마트 배치
- 기존 구조물과 연결 (복도, 문 연결)
- 효율적인 레이아웃 (주방-식당 인접)
- 플레이 스타일 반영 (Fortress는 외벽 중심)

---

## 📐 위치 찾기 알고리즘 설계

### 기본 전략: "거점 근처 빈 공간"

```
입력:
- Map map
- IntVec3 anchorPoint (거점 위치, 기본: 콜로니 중심)
- IntVec2 size (건물 크기, 예: 3x4)
- BuildingType type (침대/테이블/방어 등)

출력:
- IntVec3? location (위치, 실패 시 null)

알고리즘:
1. anchorPoint에서 시작하여 나선형으로 확장
2. 각 위치에서 size 크기의 사각형 체크:
   a. 모든 셀이 건설 가능한가? (지형 체크)
   b. 기존 건물/청사진과 겹치는가?
   c. 도달 가능한가? (PathFinder 사용)
3. 점수 계산:
   - 거점에서 거리 (가까울수록 좋음)
   - 평평한 지형 (fertility 높음)
   - 자연 벽 인접 (벽 건설 절약)
4. 최고 점수 위치 반환
```

### 건물 타입별 전략

#### 침실 (Bedroom)
- **위치**: 콜로니 중심 근처, 실내
- **요구사항**: 3x4 크기, 벽 3면, 문 1개
- **우선순위**: 기존 방 인접 > 독립 건물

#### 식당 (DiningRoom)
- **위치**: 중앙, 주방 근처
- **요구사항**: 6x6 크기
- **우선순위**: 주방 인접 > 콜로니 중심

#### 창고 (Storage)
- **위치**: 주변부, 넓은 공간
- **요구사항**: 5x5 크기
- **우선순위**: 넓은 공간 > 접근성

#### 방어선 (Defense)
- **위치**: 콜로니 외곽
- **요구사항**: 샌드백 라인 (직선 5~10개)
- **우선순위**: 위협 방향 > 출입구 보호

---

## 🔨 RimWorld API 사용

### 청사진 배치 API

```csharp
// 1. 단일 건물 청사진
GenConstruct.PlaceBlueprintForBuild(
    ThingDef entDef,           // 건물 Def (예: ThingDefOf.Bed)
    IntVec3 center,            // 위치
    Map map,                   // 맵
    Rot4 rotation,             // 회전 (Rot4.North 등)
    Faction faction,           // 파벌 (Faction.OfPlayer)
    ThingDef stuffDef          // 재질 (예: ThingDefOf.WoodLog)
);

// 2. 벽 청사진
GenConstruct.PlaceBlueprintForBuild(
    ThingDefOf.Wall,
    position,
    map,
    Rot4.North,
    Faction.OfPlayer,
    ThingDefOf.BlocksGranite  // 돌벽
);

// 3. 스톡파일 구역
Zone_Stockpile zone = new Zone_Stockpile(
    StorageSettingsPreset.DefaultStockpile,
    map.zoneManager
);
zone.AddCell(cell);  // 각 셀 추가
map.zoneManager.RegisterZone(zone);
```

### 지형 체크 API

```csharp
// 건설 가능 여부
bool canBuild = GenConstruct.CanPlaceBlueprintAt(
    entDef, center, rotation, map
).Accepted;

// 지형 타입
TerrainDef terrain = cell.GetTerrain(map);
bool isWater = terrain.IsWater;
bool isPassable = cell.Standable(map);

// 기존 건물 체크
Building existing = cell.GetFirstBuilding(map);
bool hasBlueprint = map.blueprintGrid[cell] != null;
```

### 자원 체크

```csharp
// 건설 비용 계산
List<ThingDefCountClass> costs = entDef.CostListAdjusted(stuffDef);

// 자원 보유량 확인
int available = map.resourceCounter.GetCount(ThingDefOf.WoodLog);

// 비용 충족 여부
bool canAfford = costs.All(cost =>
    map.resourceCounter.GetCount(cost.thingDef) >= cost.count
);
```

---

## ⚙️ 설정 및 쿨다운

### 개입 강도별 건설량

| Intensity | 건설 빈도 | 한 번에 배치 | 설명 |
|-----------|----------|-------------|------|
| **Off** | 없음 | 0 | 건설 비활성화 |
| **Low** | 30분 | 1~2개 | 긴급 상황만 (침대 부족) |
| **Medium** | 16분 | 2~3개 | 기본 인프라 |
| **High** | 10분 | 3~5개 | 적극적 확장 |
| **Full** | 5분 | 5~10개 | 최대 자동화 |

### 플레이 스타일별 편향

| Style | 침대 | 식당 | 창고 | 방어 |
|-------|------|------|------|------|
| **Balanced** | 1.0x | 1.0x | 1.0x | 1.0x |
| **Fortress** | 0.8x | 0.8x | 1.0x | 3.0x |
| **Agricultural** | 1.0x | 1.2x | 2.0x | 0.7x |
| **Nomadic** | 0.6x | 0.6x | 0.5x | 0.5x |
| **Researcher** | 1.0x | 1.0x | 1.0x | 0.8x |

### 쿨다운 관리

```csharp
// 현재 구현 (ConstructionSubsystem.cs:16)
private const int CONSTRUCTION_COOLDOWN = 60000; // 16분

// 개입 강도별 쿨다운 배수
float multiplier = RimAISettings.GetUpdateIntervalMultiplier(intensity);
int effectiveCooldown = (int)(CONSTRUCTION_COOLDOWN * multiplier);

// 쿨다운 체크
bool canConstruct = (currentTick - lastTick) >= effectiveCooldown;
```

---

## 📝 스토리 로그 메시지

### 건설 계획 로그

```csharp
// 침대 건설
StoryLogger.Construction.BedShortage(needed: 2);
// → "[RimAI 스토리] [플레이 스타일] 침대 부족! 2개 추가 건설을 계획합니다."

// 인프라 건설
StoryLogger.Construction.InfrastructurePlan("주방");
// → "[RimAI 스토리] [균형 발전] 주방 건설을 계획합니다. 콜로니가 발전합니다."
// → "[RimAI 스토리] [유목 생존] 필수 시설만 건설합니다: 주방"

// 방어선 건설
StoryLogger.Construction.DefenseBuilding();
// → "[RimAI 스토리] [방어 중심] 방어 시설 건설을 계획합니다. 벽을 높이고 방어선을 강화합니다."

// 영토 확장
StoryLogger.Construction.ExpansionDecision();
// → "[RimAI 스토리] [농업 제국] 영토 확장을 계획합니다. 농장을 확장하여 식량 생산을 늘립니다."
```

---

## 🔄 실행 흐름 예시

### 시나리오: 침대 2개 부족

```
[Tick 1000] ConstructionAnalyzer.AnalyzeMap()
  → state.TotalBeds = 3
  → state.ColonistCount = 5
  → state.NeedMoreBeds() = true (3 < 5 + 2)

[Tick 1000] ConstructionSubsystem.GetProposedActions()
  → RimAIAction 생성: "침대 건설 필요"
  → Priority: High

[Tick 1000] RimAIManager.ExecutePendingActions()
  → ConstructionSubsystem.ExecuteAction(action)

[Tick 1000] BuildingPlacer.FindBestLocation()
  → 거점 = 콜로니 중심 (30, 50)
  → 나선형 탐색...
  → 후보 1: (32, 51) - 점수 85
  → 후보 2: (35, 50) - 점수 90 ← 선택!
  → 반환: (35, 50)

[Tick 1000] BlueprintExecutor.PlaceBlueprint()
  → 자원 체크: 목재 35개 필요, 보유 120개 ✓
  → 충돌 체크: 깨끗한 공간 ✓
  → GenConstruct.PlaceBlueprintForBuild()
  → 청사진 배치 성공!

[Tick 1000] StoryLogger.Construction.BedShortage(2)
  → 로그: "[RimAI 스토리] 침대 부족! 2개 추가 건설을 계획합니다."

[Tick 1000+] 콜로니스트가 자동으로 건설 작업 수행
```

---

## 🚧 제한사항 및 향후 확장

### 현재 제한사항
- 복잡한 레이아웃 불가 (직선 벽만)
- 기존 구조물과 연결 불가 (독립 건물만)
- 미적 고려 없음 (효율성만)

### Phase 2 확장
- 복합 구조물: 벽 + 문 + 가구를 하나의 "방"으로
- 스마트 연결: 기존 복도에 문 추가
- 효율적 레이아웃: 주방-식당 인접, 침실 군집

### Phase 3 확장
- AI 학습: 플레이어가 선호하는 레이아웃 학습
- 동적 조정: 계절/이벤트에 따라 건설 우선순위 변경
- 미적 요소: 장식, 바닥재, 조명 자동 배치

---

## 📊 성능 고려사항

### 위치 탐색 최적화
- **최대 탐색 반경**: 50셀 (너무 멀면 포기)
- **조기 종료**: 점수 95 이상이면 즉시 반환
- **캐싱**: 최근 탐색 결과 5분간 캐싱

### 건설 빈도 제한
- **쿨다운**: 최소 5분 ~ 최대 30분
- **동시 청사진**: 최대 3~5개
- **자원 여유**: 50% 이상 남을 때만 건설

---

## 🧪 테스트 체크리스트

### 단위 테스트
- [ ] BuildingPlacer.FindBestLocation() - 다양한 지형
- [ ] BlueprintExecutor.PlaceBlueprint() - 성공/실패 케이스
- [ ] 자원 부족 시 건설 취소
- [ ] 충돌 감지 (기존 건물/청사진)

### 통합 테스트
- [ ] 새 게임 시작 → 3시간 방치 → 침실 3개 이상 생성
- [ ] 식당/창고 자동 생성 확인
- [ ] 방어선 자동 생성 (Fortress 스타일)
- [ ] 쿨다운 동작 확인

### 성능 테스트
- [ ] 위치 탐색 시간 < 100ms
- [ ] 청사진 배치 시간 < 50ms
- [ ] 메모리 누수 없음 (12시간 플레이)

---

**작성자**: RimAI Team
**버전**: v0.7.0 (Blueprint System)
**최종 수정**: 2025-01-21
