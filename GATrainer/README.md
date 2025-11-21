# RimAI GA Trainer

RimAI의 유전 알고리즘 최적화를 위한 콘솔 트레이너 앱입니다.

## 📦 구조

```
GATrainer/
├── Models/
│   ├── Individual.cs         # GA 개체 (Genome + Metrics + Fitness)
│   ├── Population.cs         # 세대 관리
│   ├── RimAIGenome.cs        # 유전자 (파라미터 세트)
│   └── RimAIRunMetrics.cs    # 게임 플레이 결과
├── GA/
│   ├── GAOperations.cs       # GA 연산 (Selection, Crossover, Mutation)
│   └── DataLoader.cs         # 파일 I/O 유틸리티
├── Program.cs                # 메인 진입점
├── GATrainer.csproj          # 프로젝트 파일
└── README.md                 # 이 문서
```

## 🚀 빌드

```bash
cd GATrainer
dotnet build
```

빌드 결과: `bin/Debug/net472/GATrainer.exe`

## 📖 사용법

### 1. 초기 세대 생성 (Gen 0)

첫 세대는 랜덤 파라미터로 생성됩니다.

```bash
GATrainer init <개체수> <출력디렉토리>
```

**예시:**
```bash
GATrainer init 20 ./genomes_gen0
```

**결과:**
- `genomes_gen0/` 디렉토리에 20개의 Genome JSON 파일이 생성됨
- 각 파일: `gen0_<id>.json`

### 2. 게임 실행 (RimAI에서)

생성된 Genome 파일들을 RimAI의 설정 디렉토리로 복사:

**Windows:**
```powershell
Copy-Item genomes_gen0/*.json C:\Users\<사용자>\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config\RimAI\GA\genomes\
```

**Linux/Mac:**
```bash
cp genomes_gen0/*.json ~/.config/unity3d/Ludeon\ Studios/RimWorld\ by\ Ludeon\ Studios/Config/RimAI/GA/genomes/
```

각 Genome으로 게임을 실행:
1. 특정 Genome을 `current.json`으로 복사
2. RimWorld 실행 → 새 게임 시작
3. 게임 종료 (엔딩 or 전멸)
4. Metrics가 `Config/RimAI/GA/runs/<session_id>.json`에 저장됨
5. 다음 Genome으로 반복

### 3. 다음 세대 생성

모든 Genome이 평가되면 (게임 실행 완료), 다음 세대를 생성합니다.

```bash
GATrainer evolve <genomes디렉토리> <runs디렉토리> <출력디렉토리> [옵션]
```

**옵션:**
- `--size=N`: 다음 세대 크기 (기본: 현재 세대와 동일)
- `--elite=N`: 엘리트 개체 수 (기본: 2, 상위 N개 유지)
- `--mutation=N`: 돌연변이 확률 0.0-1.0 (기본: 0.1)
- `--selection=TYPE`: 선택 방법 - `tournament`, `roulette`, `topN` (기본: tournament)

**예시:**
```bash
GATrainer evolve ./genomes_gen0 ./runs ./genomes_gen1 --elite=3 --mutation=0.15
```

**결과:**
- `genomes_gen1/` 디렉토리에 다음 세대 Genome 파일들이 생성됨
- `current.json`: 최고 fitness를 가진 Genome (RimAI가 자동으로 로드)

### 4. 통계 보기

현재 세대의 통계를 확인:

```bash
GATrainer stats <genomes디렉토리> <runs디렉토리>
```

**예시:**
```bash
GATrainer stats ./genomes_gen0 ./runs
```

**출력 예시:**
```
=== Population [Gen 0] 통계 ===
총 개체 수: 20
평가 완료: 18
평균 생존 일수: 245.3일
엔딩 도달: 2개 (11.1%)
Fitness - 최고: 8523.0, 평균: 2156.7, 최저: -1203.0

=== 상위 5개 개체 ===
[1] Individual [Gen 0] gen0_3a7f2b1c - Fitness: 8523, Days: 342, Ended: True
[2] Individual [Gen 0] gen0_9c4e5d8a - Fitness: 6782, Days: 298, Ended: False
...
```

## 🔄 전체 워크플로우

### Gen 0 → Gen 1

```bash
# 1. 초기 세대 생성
GATrainer init 20 ./genomes_gen0

# 2. Genome 파일 복사 (RimAI 설정 디렉토리로)
cp genomes_gen0/*.json ~/.config/unity3d/.../RimAI/GA/genomes/

# 3. 각 Genome으로 게임 플레이 (20번)
#    - gen0_<id>.json을 current.json으로 복사
#    - RimWorld 실행 → 게임 종료
#    - runs/<session_id>.json 생성됨

# 4. Runs 디렉토리에서 metrics 수집
cp ~/.config/unity3d/.../RimAI/GA/runs/*.json ./runs/

# 5. 다음 세대 생성
GATrainer evolve ./genomes_gen0 ./runs ./genomes_gen1

# 6. 통계 확인
GATrainer stats ./genomes_gen0 ./runs
```

### Gen 1 → Gen 2

```bash
# 1. 새 세대 Genome 복사
cp genomes_gen1/*.json ~/.config/unity3d/.../RimAI/GA/genomes/

# 2. 게임 플레이 (Gen 1 개체들)

# 3. Metrics 수집
cp ~/.config/unity3d/.../RimAI/GA/runs/*.json ./runs/

# 4. 다음 세대 생성
GATrainer evolve ./genomes_gen1 ./runs ./genomes_gen2

# 반복...
```

## 🧬 GA 알고리즘 상세

### Selection (선택)

1. **Tournament Selection** (기본)
   - 랜덤하게 K개(기본 3개) 개체를 선택
   - 그 중 최고 fitness를 부모로 선택
   - 다양성 유지에 효과적

2. **Roulette Wheel Selection**
   - Fitness에 비례하는 확률로 선택
   - 높은 fitness일수록 선택 확률 높음

3. **Top-N Selection**
   - 상위 N개 개체 중에서 균등하게 선택
   - 빠른 수렴, 다양성은 낮음

### Crossover (교배)

- **Uniform Crossover** 사용
- 각 파라미터마다 50% 확률로 부모1 또는 부모2에서 선택
- 예시:
  ```
  Parent1: FoodWeight=1.2, CombatWeight=0.8
  Parent2: FoodWeight=0.9, CombatWeight=1.3
  → Child: FoodWeight=0.9 (P2), CombatWeight=0.8 (P1)
  ```

### Mutation (돌연변이)

- 각 파라미터마다 독립적으로 돌연변이 확률 적용 (기본 10%)
- 돌연변이 발생 시 해당 파라미터를 랜덤 값으로 재설정
- 지역 최적해 탈출에 효과적

### Elitism (엘리트 보존)

- 상위 N개(기본 2개) 개체를 다음 세대에 그대로 유지
- 최고 해가 손실되는 것을 방지

### Fitness 함수

```
Fitness = TotalDaysSurvived * 1
        + (Ended ? 5000 : 0)                    // 엔딩 도달 보너스
        + (EndDay < 1825 ? (1825-EndDay)*2 : 0) // 빠른 엔딩 보너스
        - ColonistDeaths * 200                  // 사망 페널티
        + MaxColonistCount * 50                 // 인구 성장
        + AverageMood * 10                      // 무드 관리
        - MentalBreakCount * 50                 // 정신 붕괴 페널티
        + CombatWinRate * 500                   // 전투 승률
        - FoodCrisesCount * 100                 // 식량 위기 페널티
        + FinalWealth * 0.1                     // 경제
        + ResearchScore * 100                   // 기술 발전
        - (전멸 ? 3000 : 0)                     // 전멸 페널티
```

## 📊 디렉토리 구조 예시

```
RimAI/
├── GATrainer/          # 이 프로젝트
│   ├── bin/
│   │   └── Debug/net472/GATrainer.exe
│   └── ...
├── ga_experiments/     # 실험 데이터
│   ├── genomes_gen0/   # Gen 0 Genomes
│   │   ├── gen0_3a7f2b1c.json
│   │   ├── gen0_9c4e5d8a.json
│   │   └── ...
│   ├── genomes_gen1/   # Gen 1 Genomes
│   ├── genomes_gen2/   # Gen 2 Genomes
│   └── runs/           # 모든 세대의 Metrics
│       ├── rimai_20250101_120000.json
│       ├── rimai_20250101_134523.json
│       └── ...
```

## 🎯 권장 설정

### 초기 세대 (Gen 0)
- 개체 수: 20-30개
- 다양성 확보를 위해 랜덤 생성

### 중간 세대 (Gen 1-10)
- 개체 수: 동일 유지
- Elite: 2-3개
- Mutation: 0.1-0.15 (10-15%)
- Selection: Tournament (다양성 유지)

### 후기 세대 (Gen 10+)
- Elite: 3-5개 (좋은 해 보존)
- Mutation: 0.05-0.1 (수렴 속도 향상)
- Selection: Tournament 또는 Top-N

## ⚠️ 주의사항

1. **Metrics 파일명과 GenomeId 매칭**
   - Metrics의 `GenomeId` 필드가 Genome의 파일명과 일치해야 함
   - RimAI는 current.json 로드 시 자동으로 GenomeId를 설정

2. **중복 실행**
   - 같은 Genome으로 여러 번 플레이 가능 (다른 시드)
   - 중복 시 첫 번째 Metrics만 사용됨

3. **평가되지 않은 개체**
   - Metrics가 없는 Genome은 다음 세대 생성 시 제외됨
   - 모든 Genome을 평가한 후 evolve 실행 권장

## 🔧 트러블슈팅

### "평가된 개체가 없습니다"
- `runs/` 디렉토리에 Metrics JSON 파일이 있는지 확인
- Metrics의 `GenomeId`가 Genome 파일명과 일치하는지 확인

### "Tournament size보다 개체가 적습니다"
- 평가된 개체 수가 3개 미만일 때 발생
- `--selection=topN` 옵션 사용 또는 더 많은 게임 플레이 필요

### Fitness가 음수
- 정상입니다. 전멸하고 빨리 죽으면 음수 가능
- 세대가 진행되면서 점차 개선됨

## 📚 참고

- RimAI 모드: `Source/RimAI/`
- Genome 파라미터 설명: `Source/RimAI/GA/RimAIGenome.cs`
- Fitness 함수: `Source/RimAI/GA/RimAIRunMetrics.cs`
