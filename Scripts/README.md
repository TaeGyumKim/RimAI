# RimAI GA Scripts

GA 최적화 워크플로우를 자동화하는 스크립트 모음입니다.

## 스크립트 목록

### 1. init-ga (초기화)

초기 세대(Gen 0) Genome을 생성합니다.

**PowerShell (Windows):**
```powershell
.\init-ga.ps1 -PopulationSize 20 -Generation 0
```

**Bash (Linux/Mac):**
```bash
./init-ga.sh 20 0
```

**Parameters:**
- `PopulationSize`: 초기 개체 수 (기본: 20)
- `Generation`: 세대 번호 (기본: 0)

**Output:**
- `ga_experiments/generation_0/genomes/` - 생성된 Genome JSON 파일들
- `ga_experiments/generation_0/runs/` - 빈 디렉터리 (Metrics 수집용)

### 2. run-ga-cycle (GA 싸이클 실행)

한 세대의 전체 GA 싸이클을 실행합니다:
1. Genome을 RimWorld 설정 폴더로 복사
2. 게임 플레이 (수동)
3. Metrics 수집
4. 통계 출력
5. 다음 세대 생성

**PowerShell (Windows):**
```powershell
# 기본 설정
.\run-ga-cycle.ps1 -Generation 0

# 상세 설정
.\run-ga-cycle.ps1 -Generation 0 `
    -PopulationSize 20 `
    -EliteCount 3 `
    -MutationRate 0.15 `
    -SelectionMethod tournament

# 게임 플레이 건너뛰기 (이미 플레이 완료한 경우)
.\run-ga-cycle.ps1 -Generation 0 -SkipGamePlay

# 통계만 보기
.\run-ga-cycle.ps1 -Generation 0 -OnlyStats
```

**Bash (Linux/Mac):**
```bash
# 기본 설정
./run-ga-cycle.sh 0

# 상세 설정
./run-ga-cycle.sh 0 20 3 0.15 tournament

# 게임 플레이 건너뛰기
SKIP_GAMEPLAY=true ./run-ga-cycle.sh 0

# 통계만 보기
ONLY_STATS=true ./run-ga-cycle.sh 0
```

**Parameters:**
- `Generation`: 현재 세대 번호 (필수)
- `PopulationSize`: 다음 세대 크기 (기본: 20)
- `EliteCount`: 엘리트 개체 수 (기본: 3)
- `MutationRate`: 돌연변이 확률 (기본: 0.15)
- `SelectionMethod`: 선택 방법 - tournament/roulette/topN (기본: tournament)

**Flags (PowerShell):**
- `-SkipGamePlay`: 게임 플레이 단계 건너뛰기
- `-OnlyStats`: 통계만 출력하고 종료

**Environment Variables (Bash):**
- `SKIP_GAMEPLAY=true`: 게임 플레이 단계 건너뛰기
- `ONLY_STATS=true`: 통계만 출력하고 종료

## 전체 워크플로우 예시

### Gen 0 → Gen 1

```bash
# 1. 초기 세대 생성
./init-ga.sh 20 0

# 2. Gen 0 싸이클 실행
./run-ga-cycle.sh 0 20 3 0.15
#    - Genome 복사 완료
#    - 게임 플레이: 20개 Genome 각각으로 플레이
#    - Metrics 수집
#    - 통계 출력
#    - Gen 1 생성

# 결과: ga_experiments/generation_1/ 에 Gen 1 Genome들이 생성됨
```

### Gen 1 → Gen 2

```bash
# Gen 1 싸이클 실행
./run-ga-cycle.sh 1 20 3 0.10
#    - Gen 1 Genome들로 게임 플레이
#    - Gen 2 생성
```

### Gen 2 → Gen 3 (수렴 단계)

```bash
# 엘리트 증가, 돌연변이 감소
./run-ga-cycle.sh 2 20 5 0.05
```

## 디렉터리 구조

스크립트 실행 후 디렉터리 구조:

```
RimAI/
├── Scripts/                    # 이 디렉터리
│   ├── init-ga.ps1
│   ├── init-ga.sh
│   ├── run-ga-cycle.ps1
│   ├── run-ga-cycle.sh
│   └── README.md
├── GATrainer/                  # GA 트레이너 앱
│   └── bin/Debug/net472/GATrainer.exe
└── ga_experiments/             # 실험 데이터 (자동 생성)
    ├── generation_0/
    │   ├── genomes/            # Gen 0 Genome 파일들
    │   │   ├── gen0_abc123.json
    │   │   └── ...
    │   └── runs/               # Gen 0 플레이 결과
    │       ├── rimai_20250121_120000_abc123.json
    │       └── ...
    ├── generation_1/
    │   ├── genomes/
    │   └── runs/
    └── generation_2/
        ├── genomes/
        └── runs/
```

## RimWorld 설정 디렉터리

스크립트는 다음 경로로 Genome/Metrics를 복사합니다:

**Windows:**
```
C:\Users\<user>\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config\RimAI\GA\
├── genomes/        # Genome 파일 복사 대상
└── runs/           # Metrics 자동 저장 위치
```

**Linux:**
```
~/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Config/RimAI/GA/
├── genomes/
└── runs/
```

**Mac:**
```
~/Library/Application Support/unity.Ludeon Studios.RimWorld by Ludeon Studios/Config/RimAI/GA/
├── genomes/
└── runs/
```

## 게임 플레이 가이드

`run-ga-cycle` 스크립트가 게임 플레이 단계에서 멈추면:

1. **RimWorld 설정 폴더로 이동**
   ```bash
   # Linux/Mac
   cd ~/.config/unity3d/Ludeon\ Studios/RimWorld\ by\ Ludeon\ Studios/Config/RimAI/GA/genomes
   ```

2. **각 Genome으로 게임 플레이**
   - Gen N Genome을 `current.json`으로 복사:
     ```bash
     cp gen0_abc123.json current.json
     ```
   - RimWorld 실행
   - 새 게임 시작
   - 엔딩 도달 또는 전멸까지 플레이
   - 게임 종료 시 Metrics가 자동으로 `runs/` 폴더에 저장됨
   - 다음 Genome으로 반복

3. **스크립트로 돌아가기**
   - 모든 Genome 플레이 완료 후
   - 스크립트 프롬프트에서 Enter 입력
   - 자동으로 Metrics 수집 및 다음 세대 생성

## 팁

### 빠른 테스트

작은 Population으로 빠르게 테스트:
```bash
./init-ga.sh 5 0
./run-ga-cycle.sh 0 5 1 0.20
```

### 통계만 확인

게임 플레이 없이 현재 세대 통계만 보기:
```bash
# PowerShell
.\run-ga-cycle.ps1 -Generation 0 -OnlyStats

# Bash
ONLY_STATS=true ./run-ga-cycle.sh 0
```

### 게임 플레이 이미 완료한 경우

Metrics만 수집하고 다음 세대 생성:
```bash
# PowerShell
.\run-ga-cycle.ps1 -Generation 0 -SkipGamePlay

# Bash
SKIP_GAMEPLAY=true ./run-ga-cycle.sh 0
```

### 병렬 플레이

여러 PC에서 동시에 플레이하려면:
1. `genomes/` 폴더를 각 PC로 복사
2. 각 PC에서 일부 Genome만 플레이 (예: PC1은 1-10, PC2는 11-20)
3. 모든 `runs/` 결과를 하나의 폴더로 수집
4. 다음 세대 생성

## 문제 해결

### "GATrainer.exe not found"

GATrainer를 빌드하세요:
```bash
cd GATrainer
dotnet build
```

### "No genome files found"

`init-ga` 스크립트를 먼저 실행하세요:
```bash
./init-ga.sh 20 0
```

### "평가된 개체가 없습니다"

게임 플레이를 완료하고 Metrics가 수집되었는지 확인:
```bash
ls ga_experiments/generation_0/runs/
```

파일이 없으면 게임을 플레이하고 Metrics를 수집하세요.

### 스크립트 실행 권한 오류 (Linux/Mac)

실행 권한을 추가하세요:
```bash
chmod +x Scripts/*.sh
```

## 참고 자료

- **전체 워크플로우 문서**: `Docs/GA-Workflow.md`
- **GATrainer 사용법**: `GATrainer/README.md`
- **Genome 파라미터 설명**: `Source/RimAI/GA/RimAIGenome.cs`
- **Fitness 함수**: `Source/RimAI/GA/RimAIRunMetrics.cs`
