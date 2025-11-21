# RimAI - RimWorld Auto Pilot Mod

RimWorld **완전 자동화** 모드. 식량, 건설, 연구, 전투, 의료, 무역 등 대부분의 결정을 자동으로 처리하여 **진짜 "관람용 방치 플레이"**를 가능하게 하는 오토파일럿 시스템입니다.

## 현재 버전

**v1.0.0** - 완전한 자동화 + GA 최적화 시스템

## 주요 기능

### 서브시스템
| 서브시스템 | 우선순위 | 설명 |
|-----------|---------|------|
| **Combat** | 150 | 전투 자동 대응, 자동 징집/해제, 시네마틱 카메라 |
| **Food** | 100 | 농사/사냥/요리 우선순위 자동 조절, 계절 대응 |
| **Medical** | 95 | 부상/감염/질병 자동 치료, 면역 경쟁 모니터링 |
| **Construction** | 50 | 침대/인프라/방어시설 자동 건설 계획 |
| **Trading** | 40 | 상인 방문 감지, 자원 분석, 무역/카라반 권장 |
| **Production** | 30 | 작업대 Bill 자동 관리, 무기/갑옷/의약품 생산 |
| **Research** | 30 | 콜로니 상황에 맞는 최적 연구 자동 선택 |

### GA 최적화 시스템
- **유전 알고리즘(GA)** 기반 파라미터 최적화
- **Genome**: 서브시스템 가중치, 플레이 스타일 편향, 임계값 등 30+ 파라미터
- **Fitness 함수**: 엔딩 달성, 생존 일수, 사망자 수, 무드 등 종합 평가
- **GATrainer**: 외부 콘솔 앱으로 세대별 진화 실행

### 프리셋
| 프리셋 | 설명 |
|--------|------|
| **Default** | 균형 잡힌 기본 설정 |
| **FastEnding** | 빠른 엔딩 지향 (연구/생산 우선) |
| **SafeEnding** | 안정적 엔딩 (생존/자원 비축 우선) |
| **Entertaining** | 관람용 스토리 (무드/균형 중시) |
| **Defensive** | 방어적 플레이 (요새 스타일) |
| **ResearchFocused** | 연구 집중 (첨단 기술 추구) |

## 설치

### 1. 모드 설치
RimAI 폴더를 RimWorld Mods 디렉토리에 복사:

```bash
# Linux
cp -r RimAI ~/.steam/steam/steamapps/common/RimWorld/Mods/

# Windows
xcopy /E /I RimAI "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\RimAI"

# Mac
cp -r RimAI ~/Library/Application\ Support/RimWorld/Mods/
```

### 2. 의존성
- **Harmony**: [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) 또는 GitHub에서 설치

### 3. 게임 실행
1. RimWorld 실행
2. Mods 메뉴에서 **RimAI - Auto Pilot** 활성화
3. 게임 시작/로드

## 빌드

### 사전 요구사항
- .NET Framework 4.7.2
- .NET SDK

### 빌드 명령어

```bash
# Windows PowerShell
.\Scripts\build-mod.ps1

# Linux/Mac
./Scripts/build-mod.sh

# 또는 수동 빌드
cd Source/RimAI && dotnet build -c Release
```

## GA 최적화 사용법

### 1. 초기 세대 생성
```bash
# 20개 개체로 Gen 0 생성
./Scripts/init-ga.sh 20 0
```

### 2. GA 싸이클 실행
```bash
# Gen 0 싸이클 (게임 플레이 → Metrics 수집 → 다음 세대)
./Scripts/run-ga-cycle.sh 0 20 3 0.15
```

### 3. 최적화된 Genome 적용
GA가 찾은 최적 Genome은 자동으로 `current.json`에 저장되어 게임 시작 시 로드됩니다.

자세한 내용: [Docs/GA-Workflow.md](Docs/GA-Workflow.md)

## 폴더 구조

```
RimAI/
├── About/                      # 모드 메타데이터
├── Assemblies/                 # 빌드된 DLL
├── Docs/                       # 설계 문서
│   ├── GA-Workflow.md          # GA 워크플로우
│   ├── Genome-Parameters.md    # Genome 파라미터 분류
│   └── ...
├── Source/RimAI/               # C# 소스
│   ├── Core/                   # 중앙 브레인
│   ├── Food/                   # 식량 서브시스템
│   ├── Medical/                # 의료 서브시스템
│   ├── Combat/                 # 전투 서브시스템
│   ├── Construction/           # 건설 서브시스템
│   ├── Trading/                # 무역 서브시스템
│   ├── Production/             # 생산 서브시스템
│   ├── Research/               # 연구 서브시스템
│   ├── GA/                     # GA 최적화 시스템
│   └── Settings/               # 설정 시스템
├── GATrainer/                  # GA 트레이너 콘솔 앱
├── Scripts/                    # 빌드/GA 스크립트
└── ga_experiments/             # GA 실험 데이터 (자동 생성)
```

## 설정

### 게임 내 설정
옵션 → 모드 설정 → RimAI - Auto Pilot

### 설정 항목
- **플레이 스타일**: Balanced, Fortress, Researcher, Agricultural, Nomadic
- **개입 강도**: Off, Low, Medium, High, Full
- **서브시스템별 활성화/비활성화**
- **로그 레벨**: Minimal, Normal, Detailed, Debug
- **시네마틱 카메라**: On/Off

## 개발 로드맵

### 완료
- [x] Stage 1: 프로젝트 스캐폴딩
- [x] Stage 2: 식량 자동화
- [x] Stage 3: 중앙 브레인 + 건설/연구
- [x] Stage 4: 전투 자동화 + 시네마틱 카메라
- [x] Stage 5: 설정/UX 레이어
- [x] Stage 6: 청사진 자동 배치
- [x] Stage 7: 스토리텔링 시스템
- [x] Stage 8: 생산 자동화
- [x] Stage 9: GA 최적화 시스템
- [x] Stage 10: 의료/무역 서브시스템

### 예정
- [ ] Stage 11: 고급 청사진 (방 레이아웃)
- [ ] Stage 12: 멀티 콜로니 지원
- [ ] Stage 13: Steam Workshop 배포

## 기술 스택

- **언어**: C# (.NET Framework 4.7.2)
- **게임**: RimWorld 1.4, 1.5
- **패칭**: Harmony 2.3.3
- **최적화**: 유전 알고리즘 (GA)

## 문서

| 문서 | 설명 |
|------|------|
| [GA-Workflow.md](Docs/GA-Workflow.md) | GA 최적화 전체 워크플로우 |
| [Genome-Parameters.md](Docs/Genome-Parameters.md) | Genome 파라미터 분류 및 튜닝 범위 |
| [Settings-Guide.md](Docs/Settings-Guide.md) | 설정 가이드 |
| [Scripts/README.md](Scripts/README.md) | 스크립트 사용법 |

## 라이선스

MIT License

## 문의

GitHub Issues에 등록해주세요.

---

**Happy Modding!**
