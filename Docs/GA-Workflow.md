# RimAI GA Optimization Workflow

RimAI의 유전 알고리즘(GA) 최적화 시스템은 게임 플레이 결과를 자동으로 분석하고 더 나은 파라미터를 찾아내는 시스템입니다.

## 목표

**"엔딩을 빠르고 안정적으로 도달하는 AI 파라미터 찾기"**

- 우주선 엔딩, 로열티 엔딩 등 정상 엔딩 도달
- 콜로니 전멸 없이 생존
- 콜로니스트 사망 최소화
- 높은 무드 유지 (관람용 퀀리티)
- 식량/전투 위기 최소화

## 디렉터리 구조

### RimAI 모드 내부 (게임 실행 중)

RimWorld 설정 디렉터리:
- **Windows**: `C:\Users\<user>\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config\RimAI\GA\`
- **Linux**: `~/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Config/RimAI/GA/`
- **Mac**: `~/Library/Application Support/unity.Ludeon Studios.RimWorld by Ludeon Studios/Config/RimAI/GA/`

```
Config/RimAI/GA/
├── genomes/              # Genome JSON 파일들
│   ├── current.json      # 현재 사용 중인 Genome (게임 시작 시 자동 로드)
│   ├── gen0_abc123.json  # Gen 0 개체들
│   ├── gen0_def456.json
│   └── ...
└── runs/                 # 게임 플레이 결과 Metrics
    ├── rimai_20250121_120000_abc123.json  # 세션별 metrics
    ├── rimai_20250121_134500_def456.json
    └── ...
```

### GATrainer 작업 디렉터리 (외부)

```
RimAI/
├── GATrainer/            # GA 트레이너 콘솔 앱
│   ├── bin/Debug/net472/GATrainer.exe
│   └── ...
└── ga_experiments/       # GA 실험 데이터 (사용자가 생성)
    ├── generation_0/     # Gen 0 데이터
    │   ├── genomes/      # Gen 0 Genome 파일들
    │   └── runs/         # Gen 0 플레이 결과
    ├── generation_1/     # Gen 1 데이터
    │   ├── genomes/
    │   └── runs/
    └── generation_2/
        ├── genomes/
        └── runs/
```

## 파일 포맷

### Genome JSON 포맷

파일명: `gen{세대}_{고유ID}.json` 또는 `current.json`

```json
{
  "GenomeId": "gen0_abc123",
  "Generation": 0,
  "CreatedAt": "2025-01-21T12:00:00Z",
  "ParentId1": "",
  "ParentId2": "",

  "FoodPriorityWeight": 1.2,
  "CombatPriorityWeight": 0.9,
  "ConstructionPriorityWeight": 1.0,
  "ProductionPriorityWeight": 1.1,
  "ResearchPriorityWeight": 0.8,

  "FoodCrisisThreshold": 4.5,
  "FoodWarningThreshold": 7.2,
  "FoodStableThreshold": 15.0,
  "WinterPrepDays": 30,

  "BedBuffer": 2,
  "DefensePerColonist": 2.0,
  "ConstructionCooldown": 3600,

  "SteelShortageThreshold": 100,
  "ComponentShortageThreshold": 5,
  "SteelSurplusThreshold": 500,
  "ComponentSurplusThreshold": 20,
  "ProductionCooldown": 3600,
  "MedicinePerColonist": 10,

  "ThreatDetectionRange": 30.0,
  "CombatStartThreshold": 3,
  "CombatEndDelay": 2500,

  "ResearchCombatPriority": 1.0,
  "ResearchEconomyPriority": 1.0,
  "ResearchMedicalPriority": 1.0,

  "DefensiveBias": 1.0,
  "ExpansionBias": 1.0,
  "ResearchBias": 1.0,
  "WelfareBias": 1.0,

  "UpdateSpeedMultiplier": 1.0
}
```

### Metrics JSON 포맷

파일명: `rimai_{날짜}_{시간}_{GenomeId}.json`

```json
{
  "SessionId": "rimai_20250121_120000",
  "Seed": "randomseed123",
  "StartTime": "2025-01-21T12:00:00Z",
  "EndTime": "2025-01-21T14:30:00Z",
  "GenomeId": "gen0_abc123",

  "Ended": true,
  "EndReason": "ShipLaunched",
  "EndDay": 342,
  "TotalDaysSurvived": 342,

  "FinalColonistCount": 8,
  "MaxColonistCount": 10,
  "ColonistDeaths": 2,
  "AnimalDeaths": 5,

  "AverageMood": 65.5,
  "LowestMood": 32.1,
  "MentalBreakCount": 3,

  "FoodCrisesCount": 1,
  "SevereIncidentsCount": 4,
  "CombatCount": 12,
  "CombatVictories": 10,

  "FinalWealth": 125000.0,
  "FinalBuildingsWealth": 80000.0,
  "FinalItemsWealth": 45000.0,
  "ResearchScore": 25,
  "TechLevel": 0.6,

  "BuildingsConstructed": 150,
  "ItemsProduced": 320
}
```

## GA 한 싸이클 워크플로우

### Phase 0: 준비 (최초 1회)

```bash
# 1. GATrainer 빌드
cd GATrainer
dotnet build

# 2. 실험 디렉터리 생성
mkdir -p ga_experiments/generation_0/genomes
mkdir -p ga_experiments/generation_0/runs
```

### Phase 1: 초기 세대 생성 (Gen 0)

```bash
# 1. 초기 Genome 20개 생성
cd GATrainer
./bin/Debug/net472/GATrainer.exe init 20 ../ga_experiments/generation_0/genomes

# 결과: generation_0/genomes/ 에 20개의 gen0_*.json 파일 생성
```

### Phase 2: 게임 플레이 (각 Genome마다)

#### Windows 예시:

```powershell
# 1. Genome 파일 복사
$ConfigDir = "$env:USERPROFILE\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config\RimAI\GA"

# 2. Gen 0 Genome들을 RimAI 설정 폴더로 복사
Copy-Item ga_experiments\generation_0\genomes\*.json "$ConfigDir\genomes\"

# 3. 각 Genome으로 게임 플레이 (20번 반복)
foreach ($genome in Get-ChildItem "$ConfigDir\genomes\gen0_*.json") {
    # 3-1. current.json으로 복사
    Copy-Item $genome.FullName "$ConfigDir\genomes\current.json"

    # 3-2. RimWorld 실행
    Write-Host "Playing with genome: $($genome.Name)"
    # RimWorld를 실행하고 새 게임 시작
    # (수동: RimWorld 실행 → 새 게임 → 엔딩/전멸까지 플레이)

    # 3-3. 게임 종료 후 metrics가 runs/ 폴더에 자동 저장됨
    # 파일명: rimai_{날짜}_{시간}_{GenomeId}.json

    # 3-4. 다음 Genome으로 계속...
}

# 4. 모든 Metrics 수집
Copy-Item "$ConfigDir\runs\*.json" ga_experiments\generation_0\runs\
```

#### Linux/Mac 예시:

```bash
# 1. 설정 디렉터리 변수
CONFIG_DIR="$HOME/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Config/RimAI/GA"

# 2. Gen 0 Genome들을 RimAI 설정 폴더로 복사
cp ga_experiments/generation_0/genomes/*.json "$CONFIG_DIR/genomes/"

# 3. 각 Genome으로 게임 플레이 (수동 반복)
for genome in "$CONFIG_DIR/genomes"/gen0_*.json; do
    # 3-1. current.json으로 복사
    cp "$genome" "$CONFIG_DIR/genomes/current.json"

    # 3-2. RimWorld 실행
    echo "Playing with genome: $(basename $genome)"
    # (수동: RimWorld 실행 → 새 게임 → 엔딩/전멸까지 플레이)

    # 3-3. 게임 종료 후 metrics 자동 저장됨
    # 3-4. 다음 Genome으로...
done

# 4. 모든 Metrics 수집
cp "$CONFIG_DIR/runs"/*.json ga_experiments/generation_0/runs/
```

**게임 플레이 시나리오**:
- **맵 생성**: Seed 고정 (선택사항) 또는 랜덤
- **난이도**: 커스텀 (적절한 도전 수준)
- **시나리오**: Crashlanded 또는 원하는 시나리오
- **종료 조건**:
  - 엔딩 도달 (우주선 발사, 로열티 퀘스트 완료 등)
  - 콜로니 전멸
  - 타임아웃 (예: in-game 5년 = 1825일)

### Phase 3: 다음 세대 생성 (Gen 0 → Gen 1)

```bash
# 1. 통계 확인
./bin/Debug/net472/GATrainer.exe stats \
    ../ga_experiments/generation_0/genomes \
    ../ga_experiments/generation_0/runs

# 출력 예시:
# === Population [Gen 0] 통계 ===
# 총 개체 수: 20
# 평가 완료: 18
# 평균 생존 일수: 245.3일
# 엔딩 도달: 2개 (11.1%)
# Fitness - 최고: 8523.0, 평균: 2156.7, 최저: -1203.0

# 2. 다음 세대 디렉터리 생성
mkdir -p ../ga_experiments/generation_1/genomes
mkdir -p ../ga_experiments/generation_1/runs

# 3. 다음 세대 Genome 생성
./bin/Debug/net472/GATrainer.exe evolve \
    ../ga_experiments/generation_0/genomes \
    ../ga_experiments/generation_0/runs \
    ../ga_experiments/generation_1/genomes \
    --size=20 \
    --elite=3 \
    --mutation=0.15 \
    --selection=tournament

# 결과:
# - generation_1/genomes/ 에 20개의 gen1_*.json 파일 생성
# - generation_1/genomes/current.json = 최고 fitness를 가진 Genome
```

### Phase 4: Gen 1 플레이 (Phase 2 반복)

```bash
# 1. Gen 1 Genome들을 RimAI 설정 폴더로 복사
cp ga_experiments/generation_1/genomes/*.json "$CONFIG_DIR/genomes/"

# 2. 각 Genome으로 게임 플레이 (20번)
# (Phase 2와 동일한 과정)

# 3. Metrics 수집
cp "$CONFIG_DIR/runs"/*.json ga_experiments/generation_1/runs/
```

### Phase 5: Gen 1 → Gen 2 (Phase 3 반복)

```bash
# 1. 통계 확인
./bin/Debug/net472/GATrainer.exe stats \
    ../ga_experiments/generation_1/genomes \
    ../ga_experiments/generation_1/runs

# 2. Gen 2 생성
mkdir -p ../ga_experiments/generation_2/genomes
mkdir -p ../ga_experiments/generation_2/runs

./bin/Debug/net472/GATrainer.exe evolve \
    ../ga_experiments/generation_1/genomes \
    ../ga_experiments/generation_1/runs \
    ../ga_experiments/generation_2/genomes \
    --elite=4 \
    --mutation=0.10

# 3. 이후 세대 계속 반복...
```

## Fitness 함수 (v1.0)

현재 구현된 Fitness 함수:

```
Fitness = TotalDaysSurvived * 1
        + (Ended ? 5000 : 0)                          // 엔딩 도달 대형 보너스
        + (EndDay < 1825 ? (1825 - EndDay) * 2 : 0)  // 빠른 엔딩 보너스 (5년 이내)
        - ColonistDeaths * 200                        // 사망 페널티
        + MaxColonistCount * 50                       // 인구 성장 보너스
        + AverageMood * 10                            // 무드 관리 보너스
        - MentalBreakCount * 50                       // 정신 붕괴 페널티
        + (CombatCount > 0 ? (CombatVictories / CombatCount) * 500 : 0)  // 전투 승률
        - FoodCrisesCount * 100                       // 식량 위기 페널티
        + FinalWealth * 0.1                           // 경제 발전
        + ResearchScore * 100                         // 기술 발전
        - (FinalColonistCount == 0 && !Ended ? 3000 : 0)  // 전멸 대형 페널티
```

**주요 설계 의도**:
- **엔딩 도달 최우선**: +5000 보너스로 엔딩을 최고 목표로 설정
- **빠른 엔딩 장려**: 5년 이내 엔딩 시 추가 보너스 (효율성)
- **생존 중시**: 사망 -200, 전멸 -3000으로 강한 페널티
- **관람 퀄리티**: 무드 +10 * 평균, 정신붕괴 -50으로 "보기 좋은 플레이" 유도
- **전투 능력**: 승률에 따른 보너스
- **위기 대응**: 식량 위기 -100으로 안정성 유도

## 엔딩 종류 및 감지

### 지원하는 엔딩

1. **ShipLaunched**: 우주선 발사 (가장 일반적)
2. **RoyalAscent**: 로열티 퀘스트 완료
3. **Archonexus**: 아코넥서스 퀘스트 완료
4. **Other**: 기타 mod 엔딩

### 실패 케이스

1. **AllColonistsDead**: 모든 콜로니스트 사망 (전멸)
2. **ColonyAbandoned**: 플레이어가 콜로니 포기
3. **Timeout**: 설정된 일수 초과 (엔딩 미도달)

### 감지 메커니즘

- **Harmony Patches**: `GameEndPatches.cs`에서 RimWorld 엔딩 이벤트 감지
  - `Building_ShipComputerCore.TryLaunch()`: 우주선 발사
  - `GameEnder.CheckOrUpdateGameOver()`: 게임 오버
  - `Pawn.Kill()`: 콜로니스트 사망 추적
  - `MentalStateHandler.TryStartMentalState()`: 정신 붕괴 추적

## GA 파라미터 권장 설정

### 초기 세대 (Gen 0-2)

- **Population Size**: 20-30개
- **Elite Count**: 2개 (다양성 유지)
- **Mutation Rate**: 0.15-0.20 (높은 탐색)
- **Selection**: Tournament (다양성 최고)

**목적**: 파라미터 공간을 넓게 탐색

### 중기 세대 (Gen 3-10)

- **Population Size**: 20-30개 (동일 유지)
- **Elite Count**: 3-4개
- **Mutation Rate**: 0.10-0.15 (중간)
- **Selection**: Tournament

**목적**: 좋은 영역을 찾으면서 다양성 유지

### 후기 세대 (Gen 10+)

- **Population Size**: 15-20개 (수렴 가속)
- **Elite Count**: 5-7개 (최고 해 보존)
- **Mutation Rate**: 0.05-0.10 (낮은 변이)
- **Selection**: Tournament 또는 Top-N

**목적**: 최적해로 빠르게 수렴

## 자동화 스크립트 템플릿

### Windows PowerShell 스크립트

`run-ga-cycle.ps1`:

```powershell
param(
    [int]$Generation = 0,
    [int]$PopulationSize = 20,
    [int]$EliteCount = 3,
    [double]$MutationRate = 0.15
)

$ConfigDir = "$env:USERPROFILE\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config\RimAI\GA"
$ExperimentDir = "ga_experiments\generation_$Generation"
$NextGeneration = $Generation + 1
$NextExperimentDir = "ga_experiments\generation_$NextGeneration"

# 1. 현재 세대 Genome 복사
Write-Host "Copying genomes to RimWorld config..."
Copy-Item "$ExperimentDir\genomes\*.json" "$ConfigDir\genomes\"

# 2. 게임 플레이 (수동)
Write-Host "Please play RimWorld with each genome."
Write-Host "Press Enter when all games are finished..."
Read-Host

# 3. Metrics 수집
Write-Host "Collecting metrics..."
Copy-Item "$ConfigDir\runs\*.json" "$ExperimentDir\runs\"

# 4. 통계 확인
Write-Host "`nGeneration $Generation Statistics:"
& GATrainer\bin\Debug\net472\GATrainer.exe stats "$ExperimentDir\genomes" "$ExperimentDir\runs"

# 5. 다음 세대 생성
Write-Host "`nGenerating next generation..."
New-Item -ItemType Directory -Force -Path "$NextExperimentDir\genomes"
New-Item -ItemType Directory -Force -Path "$NextExperimentDir\runs"

& GATrainer\bin\Debug\net472\GATrainer.exe evolve `
    "$ExperimentDir\genomes" `
    "$ExperimentDir\runs" `
    "$NextExperimentDir\genomes" `
    --size=$PopulationSize `
    --elite=$EliteCount `
    --mutation=$MutationRate

Write-Host "`nGeneration $NextGeneration is ready!"
```

### Linux/Mac Bash 스크립트

`run-ga-cycle.sh`:

```bash
#!/bin/bash

GENERATION=${1:-0}
POPULATION_SIZE=${2:-20}
ELITE_COUNT=${3:-3}
MUTATION_RATE=${4:-0.15}

CONFIG_DIR="$HOME/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Config/RimAI/GA"
EXPERIMENT_DIR="ga_experiments/generation_$GENERATION"
NEXT_GENERATION=$((GENERATION + 1))
NEXT_EXPERIMENT_DIR="ga_experiments/generation_$NEXT_GENERATION"

# 1. 현재 세대 Genome 복사
echo "Copying genomes to RimWorld config..."
cp "$EXPERIMENT_DIR/genomes"/*.json "$CONFIG_DIR/genomes/"

# 2. 게임 플레이 (수동)
echo "Please play RimWorld with each genome."
echo "Press Enter when all games are finished..."
read

# 3. Metrics 수집
echo "Collecting metrics..."
cp "$CONFIG_DIR/runs"/*.json "$EXPERIMENT_DIR/runs/"

# 4. 통계 확인
echo -e "\nGeneration $GENERATION Statistics:"
GATrainer/bin/Debug/net472/GATrainer.exe stats \
    "$EXPERIMENT_DIR/genomes" \
    "$EXPERIMENT_DIR/runs"

# 5. 다음 세대 생성
echo -e "\nGenerating next generation..."
mkdir -p "$NEXT_EXPERIMENT_DIR/genomes"
mkdir -p "$NEXT_EXPERIMENT_DIR/runs"

GATrainer/bin/Debug/net472/GATrainer.exe evolve \
    "$EXPERIMENT_DIR/genomes" \
    "$EXPERIMENT_DIR/runs" \
    "$NEXT_EXPERIMENT_DIR/genomes" \
    --size=$POPULATION_SIZE \
    --elite=$ELITE_COUNT \
    --mutation=$MUTATION_RATE

echo -e "\nGeneration $NEXT_GENERATION is ready!"
```

## 예상 진행 시간

- **Gen 0 생성**: 1분
- **게임 플레이** (20개 Genome):
  - 게임당 평균 1-3시간 (엔딩 도달 시)
  - 전멸 시 10분-1시간
  - **총 예상**: 20-60시간 (병렬 플레이 시 단축 가능)
- **다음 세대 생성**: 1분
- **한 싸이클 총 시간**: 약 1-3일 (플레이 시간 포함)

## 트러블슈팅

### "평가된 개체가 없습니다"

**원인**: Metrics JSON 파일이 없거나, GenomeId가 매칭되지 않음

**해결**:
1. `runs/` 디렉터리에 JSON 파일이 있는지 확인
2. Metrics의 `GenomeId` 필드가 Genome 파일명과 일치하는지 확인
3. RimAI가 `current.json`을 제대로 로드했는지 로그 확인

### Genome이 로드되지 않음

**원인**: `current.json` 파일이 없거나 형식이 잘못됨

**해결**:
1. `genomes/current.json` 파일 존재 확인
2. JSON 형식이 올바른지 확인 (JSON validator 사용)
3. RimWorld 로그 확인: `Player.log` 파일에서 `[RimAI-GA]` 메시지 검색

### Fitness가 음수

**정상입니다**. 전멸하고 빨리 죽으면 음수 가능합니다.
- 세대가 진행되면서 점차 개선됨
- Gen 0-2는 대부분 음수 또는 낮은 양수
- Gen 5+ 부터 안정적으로 높은 fitness 나타남

### 게임이 너무 느림

**원인**: RimWorld의 틱 속도가 느림

**해결**:
1. 게임 속도를 3배속(Ultra Speed)으로 설정
2. Dev Mode에서 4배속 가능
3. 시뮬레이션 우선 시 그래픽 품질 낮추기

## 참고 자료

- **RimAI Genome 파라미터 설명**: `Source/RimAI/GA/RimAIGenome.cs`
- **Fitness 함수 구현**: `Source/RimAI/GA/RimAIRunMetrics.cs`
- **GATrainer 사용법**: `GATrainer/README.md`
- **엔딩 감지 패치**: `Source/RimAI/GA/Patches/GameEndPatches.cs`
