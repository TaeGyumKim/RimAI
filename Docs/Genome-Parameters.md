# RimAI Genome 파라미터 분류

Genome 파라미터는 GA(유전 알고리즘) 최적화 여부에 따라 분류됩니다.

## 분류 원칙

| 분류 | 설명 | GA 튜닝 |
|------|------|---------|
| **Core Tunable** | GA가 적극적으로 튜닝하는 핵심 파라미터 | O |
| **Secondary Tunable** | GA가 튜닝할 수 있지만 덜 중요한 파라미터 | O (선택적) |
| **Fixed** | 고정값, 버그 유발 가능성으로 튜닝 제외 | X |
| **Meta** | Genome 식별용 메타 정보 | X |

---

## 1. Meta (메타 정보) - GA 튜닝 제외

게임 플레이에 영향 없음. GA 추적용.

| 파라미터 | 타입 | 설명 |
|----------|------|------|
| `GenomeId` | string | Genome 고유 식별자 |
| `CreatedAt` | DateTime | 생성 시각 |
| `Generation` | int | 세대 번호 |
| `ParentId1` | string | 부모 1 ID (교배 시) |
| `ParentId2` | string | 부모 2 ID (교배 시) |

---

## 2. Core Tunable (핵심 튜닝 대상)

GA가 적극적으로 튜닝해야 하는 핵심 파라미터.
엔딩 달성과 플레이 품질에 직접적 영향.

### 2.1 서브시스템 가중치

| 파라미터 | 타입 | 범위 | 기본값 | 설명 |
|----------|------|------|--------|------|
| `FoodPriorityWeight` | float | 0.5-1.5 | 1.0 | 식량 서브시스템 가중치 |
| `CombatPriorityWeight` | float | 0.5-1.5 | 1.0 | 전투 서브시스템 가중치 |
| `ConstructionPriorityWeight` | float | 0.5-1.5 | 1.0 | 건설 서브시스템 가중치 |
| `ProductionPriorityWeight` | float | 0.5-1.5 | 1.0 | 생산 서브시스템 가중치 |
| `ResearchPriorityWeight` | float | 0.5-1.5 | 1.0 | 연구 서브시스템 가중치 |

**GA 영향**: 어떤 활동에 우선순위를 둘지 결정. 엔딩 속도와 생존에 직접 영향.

### 2.2 플레이 스타일 편향

| 파라미터 | 타입 | 범위 | 기본값 | 설명 |
|----------|------|------|--------|------|
| `DefensiveBias` | float | 0.5-1.5 | 1.0 | 방어 성향 (요새 스타일) |
| `ExpansionBias` | float | 0.5-1.5 | 1.0 | 확장 성향 |
| `ResearchBias` | float | 0.5-1.5 | 1.0 | 연구 성향 |
| `WelfareBias` | float | 0.5-1.5 | 1.0 | 복지 성향 (무드 우선) |

**GA 영향**: 의사결정 편향. 플레이 스타일과 엔딩 경로에 영향.

### 2.3 식량 관리

| 파라미터 | 타입 | 범위 | 기본값 | 설명 |
|----------|------|------|--------|------|
| `FoodCrisisThreshold` | float | 2.0-6.0 | 4.0 | 식량 위기 임계값 (일) |
| `FoodWarningThreshold` | float | 5.0-10.0 | 7.0 | 식량 위험 임계값 (일) |
| `FoodStableThreshold` | float | 10.0-20.0 | 15.0 | 식량 안정 임계값 (일) |
| `WinterPrepDays` | int | 20-40 | 30 | 겨울 대비 시작 일수 |

**GA 영향**: 식량 위기 빈도와 식량 관리 효율. 생존에 직접 영향.

### 2.4 연구 우선순위

| 파라미터 | 타입 | 범위 | 기본값 | 설명 |
|----------|------|------|--------|------|
| `ResearchCombatPriority` | float | 0.5-1.5 | 1.0 | 전투 기술 연구 우선순위 |
| `ResearchEconomyPriority` | float | 0.5-1.5 | 1.0 | 경제 기술 연구 우선순위 |
| `ResearchMedicalPriority` | float | 0.5-1.5 | 1.0 | 의료 기술 연구 우선순위 |

**GA 영향**: 어떤 연구를 먼저 할지 결정. 엔딩 속도와 생존에 영향.

---

## 3. Secondary Tunable (보조 튜닝 대상)

GA가 튜닝할 수 있지만, 핵심보다 덜 중요한 파라미터.
일부는 고정해도 됨.

### 3.1 건설 관리

| 파라미터 | 타입 | 범위 | 기본값 | 설명 |
|----------|------|------|--------|------|
| `BedBuffer` | int | 1-4 | 2 | 침대 여유분 |
| `DefensePerColonist` | float | 1.0-3.0 | 2.0 | 콜로니스트당 방어 시설 |
| `ConstructionCooldown` | int | 1800-7200 | 3600 | 건설 쿨다운 (틱) |

### 3.2 생산 관리

| 파라미터 | 타입 | 범위 | 기본값 | 설명 |
|----------|------|------|--------|------|
| `SteelShortageThreshold` | int | 50-150 | 100 | 강철 부족 임계값 |
| `ComponentShortageThreshold` | int | 3-10 | 5 | 컴포넌트 부족 임계값 |
| `SteelSurplusThreshold` | int | 300-700 | 500 | 강철 여유 임계값 |
| `ComponentSurplusThreshold` | int | 15-30 | 20 | 컴포넌트 여유 임계값 |
| `ProductionCooldown` | int | 1800-7200 | 3600 | 생산 쿨다운 (틱) |
| `MedicinePerColonist` | int | 5-15 | 10 | 콜로니스트당 의약품 목표 |

### 3.3 전투 관리

| 파라미터 | 타입 | 범위 | 기본값 | 설명 |
|----------|------|------|--------|------|
| `ThreatDetectionRange` | float | 20-40 | 30 | 위협 인식 거리 (타일) |
| `CombatStartThreshold` | int | 2-5 | 3 | 전투 시작 적 수 임계값 |
| `CombatEndDelay` | int | 1500-3600 | 2500 | 전투 종료 대기 (틱) |

---

## 4. Fixed (고정값)

GA 튜닝에서 제외. 잘못된 값은 버그를 유발할 수 있음.

| 파라미터 | 타입 | 범위 | 기본값 | 제외 이유 |
|----------|------|------|--------|-----------|
| `UpdateSpeedMultiplier` | float | 0.7-1.3 | 1.0 | 성능 영향, 튜닝 불필요 |

---

## 5. Settings ↔ Genome 매핑

사용자 설정과 Genome의 관계.

### 레이어 구조

```
┌──────────────────────────────────────────────────┐
│                   사용자 UI                        │
│  (ModSettings: 프리셋, 개입 강도, 플레이 스타일)    │
└──────────────────────────────────────────────────┘
                        ↓
┌──────────────────────────────────────────────────┐
│                GenomePreset                       │
│  (프리셋 → Genome 파라미터 변환)                   │
└──────────────────────────────────────────────────┘
                        ↓
┌──────────────────────────────────────────────────┐
│                   RimAIGenome                     │
│  (실제 게임에 사용되는 파라미터)                    │
└──────────────────────────────────────────────────┘
                        ↓
┌──────────────────────────────────────────────────┐
│              GA Trainer (외부)                    │
│  (Metrics 분석 → 다음 세대 Genome 생성)           │
└──────────────────────────────────────────────────┘
```

### ModSettings → GenomePreset 매핑

| RimAIPlayStyle | GenomePresetType | 설명 |
|----------------|------------------|------|
| Balanced | Default | 균형 발전 |
| Fortress | Defensive | 방어 중심 |
| Agricultural | SafeEnding | 안정적 엔딩 |
| Nomadic | FastEnding | 빠른 엔딩 |
| Researcher | ResearchFocused | 연구 집중 |

### AutomationIntensity → Genome 영향

| Intensity | 영향 |
|-----------|------|
| Off | Genome 무시, 서브시스템 비활성화 |
| Low | Genome 가중치 * 0.5 |
| Medium | Genome 가중치 * 0.8 |
| High | Genome 가중치 * 1.0 |
| Full | Genome 가중치 * 1.5 |

---

## 6. GA 튜닝 전략

### 초기 세대 (Gen 0)

모든 Core Tunable + 일부 Secondary Tunable 포함.
넓은 범위로 랜덤 생성.

### 중기 세대 (Gen 5-10)

Core Tunable 중심으로 튜닝.
Secondary Tunable은 기본값 또는 좁은 범위.

### 후기 세대 (Gen 10+)

Core Tunable 중 가장 영향력 있는 것만 미세 튜닝.
나머지는 최적값으로 고정.

---

## 7. 프리셋별 권장 GA 시드

GA를 실행할 때 어떤 프리셋에서 시작할지 선택 가능.

| 목표 | 권장 시드 프리셋 | 이유 |
|------|-----------------|------|
| 빠른 엔딩 | FastEnding | 연구/생산 우선, 빠른 수렴 |
| 안정적 엔딩 | SafeEnding | 생존 우선, 안전한 탐색 |
| 관람 품질 | Entertaining | 무드/균형 중시 |
| 모든 가능성 | Default + Random | 넓은 탐색 공간 |

---

## 8. 파라미터 영향도 분석

GA 결과 분석 시 어떤 파라미터가 Fitness에 영향을 주는지 확인.

### 예상 영향도 (높음 → 낮음)

1. **FoodPriorityWeight** - 식량 위기 빈도에 직접 영향
2. **ResearchPriorityWeight** - 엔딩 속도에 직접 영향
3. **DefensiveBias** - 전투 생존율에 영향
4. **WelfareBias** - 무드/정신 붕괴에 영향
5. **FoodCrisisThreshold** - 식량 관리 민감도
6. **CombatPriorityWeight** - 전투 대응 효율

### 상관관계 분석 포인트

- `FoodPriorityWeight` ↑ → `FoodCrisesCount` ↓
- `DefensiveBias` ↑ → `ColonistDeaths` ↓
- `WelfareBias` ↑ → `AverageMood` ↑, `MentalBreakCount` ↓
- `ResearchPriorityWeight` ↑ → `EndDay` ↓ (빠른 엔딩)

---

## 9. 권장 튜닝 범위

GA CreateRandom()에서 사용하는 범위.

### Core Tunable

```csharp
// 서브시스템 가중치
FoodPriorityWeight: 0.5 - 1.5
CombatPriorityWeight: 0.5 - 1.5
ConstructionPriorityWeight: 0.5 - 1.5
ProductionPriorityWeight: 0.5 - 1.5
ResearchPriorityWeight: 0.5 - 1.5

// 플레이 스타일 편향
DefensiveBias: 0.5 - 1.5
ExpansionBias: 0.5 - 1.5
ResearchBias: 0.5 - 1.5
WelfareBias: 0.5 - 1.5

// 식량 관리
FoodCrisisThreshold: 2.0 - 6.0
FoodWarningThreshold: 5.0 - 10.0
FoodStableThreshold: 10.0 - 20.0
WinterPrepDays: 20 - 40

// 연구 우선순위
ResearchCombatPriority: 0.5 - 1.5
ResearchEconomyPriority: 0.5 - 1.5
ResearchMedicalPriority: 0.5 - 1.5
```

### Secondary Tunable (선택적)

```csharp
// 건설
BedBuffer: 1 - 4
DefensePerColonist: 1.0 - 3.0
ConstructionCooldown: 1800 - 7200

// 생산
SteelShortageThreshold: 50 - 150
ComponentShortageThreshold: 3 - 10

// 전투
ThreatDetectionRange: 20 - 40
CombatStartThreshold: 2 - 5
```

---

## 10. 참고 파일

- **Genome 정의**: `Source/RimAI/GA/RimAIGenome.cs`
- **프리셋 정의**: `Source/RimAI/GA/GenomePreset.cs`
- **Settings 정의**: `Source/RimAI/Settings/RimAISettings.cs`
- **PlayStyle 정의**: `Source/RimAI/Settings/RimAIPlayStyle.cs`
- **Fitness 함수**: `Source/RimAI/GA/RimAIRunMetrics.cs`
