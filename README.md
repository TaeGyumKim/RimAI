# RimAI - RimWorld Auto Pilot Mod

RimWorld 자동 플레이 모드. 식량, 건설, 연구, 생산, 의료, 전투 등 대부분의 결정을 자동으로 처리하는 "오토파일럿" 시스템입니다.

## 프로젝트 개요

RimAI는 RimWorld의 기본 AI 위에 올라가는 **상위 의사결정 레이어**를 제공하여, 플레이어가 거의 개입하지 않아도 콜로니가 효율적으로 운영될 수 있도록 돕습니다.

### 주요 목표
- 작업 우선순위 자동 조정
- 식량/농사/채집/요리 자동 관리
- 건설 및 생산 계획 자동화
- 연구 방향 자동 결정
- 위기 상황 자동 대응

### 현재 버전
**v0.3.0 (알파)** - 3단계: 중앙 브레인 + 건설/연구 자동화

## 폴더 구조

```
RimAI/
├── About/                      # 모드 메타데이터
│   └── About.xml              # 모드 정보 (이름, 설명, 버전, 의존성)
├── Assemblies/                # 빌드된 DLL 파일
│   └── RimAI.dll              # (빌드 후 생성됨)
├── Docs/                      # 설계 문서
│   ├── RimWorld-WorkSystem-Overview.md
│   ├── Stage2-FoodAutomation-Design.md
│   └── Stage3-CentralBrain-Architecture.md
├── Source/                    # C# 소스 코드
│   └── RimAI/
│       ├── RimAI.csproj       # C# 프로젝트 파일
│       ├── RimAI_Mod.cs       # Harmony 부트스트랩 클래스
│       ├── Core/              # 코어 시스템
│       │   ├── IRimAISubsystem.cs    # 서브시스템 인터페이스
│       │   ├── RimAIAction.cs        # 액션 데이터 구조
│       │   ├── RimAIManager.cs       # 중앙 브레인
│       │   ├── ColonyScanner.cs      # 콜로니 스캐너
│       │   └── Patches/
│       │       └── GamePatches.cs    # 게임 초기화 패치
│       ├── Food/              # 식량 자동화 서브시스템
│       │   ├── FoodSubsystem.cs      # 식량 서브시스템
│       │   ├── FoodState.cs          # 상태 데이터
│       │   ├── FoodAnalyzer.cs       # 상태 분석기
│       │   ├── FoodDecisionEngine.cs # 의사결정 엔진
│       │   └── Patches/
│       │       └── WorkGiverPatches.cs
│       ├── Construction/      # 건설/확장 서브시스템
│       │   ├── ConstructionSubsystem.cs
│       │   ├── ConstructionState.cs
│       │   └── ConstructionAnalyzer.cs
│       └── Research/          # 연구 자동화 서브시스템
│           └── ResearchSubsystem.cs
├── Defs/                      # XML 정의 파일 (향후 추가)
├── .gitignore                 # Git 제외 파일 목록
└── README.md                  # 이 파일
```

## 빌드 방법

### 사전 요구사항
- .NET Framework 4.7.2 이상
- .NET SDK (dotnet CLI)
- RimWorld 게임 설치 (Steam 또는 GOG)

### 1. RimWorld 경로 설정

`Source/RimAI/RimAI.csproj` 파일을 열고, 자신의 RimWorld 설치 경로를 확인하세요.

기본 경로:
- **Windows**: `C:\Program Files (x86)\Steam\steamapps\common\RimWorld`
- **Linux**: `~/.steam/steam/steamapps/common/RimWorld`
- **Mac**: `~/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Contents/Resources/Data/Managed`

프로젝트 파일에서 경로가 올바르게 설정되어 있는지 확인하세요.

### 2. Harmony DLL 준비

Harmony 모드를 RimWorld에 설치하거나, Harmony DLL 파일을 다운로드하여 `Assemblies/` 폴더에 복사합니다.

```bash
# Harmony 다운로드 (또는 Steam Workshop에서 설치)
# https://github.com/pardeike/HarmonyRimWorld/releases/latest
```

### 3. 빌드 실행

프로젝트 루트에서 다음 명령어를 실행:

```bash
cd Source/RimAI
dotnet build -c Release
```

빌드가 성공하면 `Assemblies/RimAI.dll` 파일이 생성됩니다.

### 4. RimWorld Mods 폴더에 배포

전체 모드 폴더를 RimWorld의 Mods 디렉토리로 복사합니다.

**RimWorld Mods 경로:**
- **Windows**: `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\`
- **Linux**: `~/.steam/steam/steamapps/common/RimWorld/Mods/`
- **Mac**: `~/Library/Application Support/RimWorld/Mods/`

```bash
# 예시 (Linux/Mac)
cp -r /path/to/RimAI ~/.steam/steam/steamapps/common/RimWorld/Mods/

# 예시 (Windows)
xcopy /E /I "C:\path\to\RimAI" "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\RimAI"
```

또는 심볼릭 링크를 생성하여 개발 편의성을 높일 수 있습니다:

```bash
# Linux/Mac
ln -s /path/to/RimAI ~/.steam/steam/steamapps/common/RimWorld/Mods/RimAI

# Windows (관리자 권한 필요)
mklink /D "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\RimAI" "C:\path\to\RimAI"
```

## 설치 및 테스트

1. RimWorld를 실행합니다.
2. 메인 메뉴에서 **Mods** 클릭
3. **RimAI - Auto Pilot** 모드를 찾아 활성화
4. 게임을 재시작하거나 새 게임/기존 저장 파일 로드
5. 로그 파일(`Player.log`)을 확인하여 모드가 정상 작동하는지 확인

### 로그 파일 위치
- **Windows**: `C:\Users\[사용자명]\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`
- **Linux**: `~/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Player.log`
- **Mac**: `~/Library/Logs/Ludeon Studios/RimWorld by Ludeon Studios/Player.log`

게임 로드 시 다음과 같은 로그가 출력되어야 합니다:

```
[RimAI - Auto Pilot] v0.1.0 초기화 시작...
[RimAI - Auto Pilot] Harmony 패치 적용 완료
[RimAI - Auto Pilot] 초기화 성공!
[RimAI] 게임 초기화 완료, 콜로니 스캔 시작
[RimAI] ===== 콜로니 상태 스캔 시작 =====
...
```

## 개발 로드맵

### ✅ 1단계: 프로젝트 스캐폴딩 + 최소 동작 (완료)
- [x] RimWorld 모드 기본 구조 생성
- [x] Harmony 부트스트랩 코드 작성
- [x] 게임 로드 시 콜로니 상태 스캔 및 로그 출력

### ✅ 2단계: 식량 자동화 (완료)
- [x] 식량 저장량 모니터링 시스템 (FoodAnalyzer)
- [x] 농사 작업 우선순위 자동 조정 (FoodDecisionEngine)
- [x] 사냥 작업 자동 지시
- [x] 요리 작업 자동 관리
- [x] 계절별 농사 대응 (겨울 대비, 봄 파종)
- [x] WorkGiver 패치를 통한 작업 제어

### ✅ 3단계: 중앙 브레인 + 건설/연구 자동화 (완료 - 현재)
- [x] 중앙 브레인 시스템 (RimAIManager) 구축
- [x] 서브시스템 아키텍처 (IRimAISubsystem 인터페이스)
- [x] 액션 기반 의사결정 (RimAIAction 데이터 구조)
- [x] 우선순위 조정 및 충돌 해결 시스템
- [x] 건설 서브시스템 (침대, 주방, 방어 시설 자동 구축)
- [x] 연구 서브시스템 (휴리스틱 기반 연구 자동 선택)
- [x] 쿨다운 시스템 (방치 모드 최적화)
- [ ] 건설 실제 청사진 배치 로직 (4단계에서 완성)
- [ ] 채집 작업 자동 지시 (4단계에서 추가)

### 📋 4단계: 건설 완성 및 생산 자동화 (예정)
- [ ] 실제 청사진 배치 구현
- [ ] 적절한 위치 찾기 알고리즘
- [ ] 방 레이아웃 플래너
- [ ] 생산 작업 자동 계획
- [ ] 자원 관리 시스템

### 📋 5단계: 위기 대응 (예정)
- [ ] 전투 상황 자동 대응
- [ ] 의료 응급 처치 자동화
- [ ] 재난 상황 자동 관리

## 기술 스택

- **언어**: C# (.NET Framework 4.7.2)
- **게임**: RimWorld 1.4, 1.5
- **패칭 라이브러리**: Harmony 2.3.3
- **빌드 도구**: .NET SDK

## 기여 방법

이 프로젝트는 활발히 개발 중입니다. 기여를 환영합니다!

1. 이 레포지토리를 Fork합니다.
2. 새 기능 브랜치를 생성합니다 (`git checkout -b feature/amazing-feature`)
3. 변경사항을 커밋합니다 (`git commit -m 'Add some amazing feature'`)
4. 브랜치에 푸시합니다 (`git push origin feature/amazing-feature`)
5. Pull Request를 생성합니다.

## 라이선스

이 프로젝트는 MIT 라이선스 하에 배포됩니다.

## 참고 자료

- [RimWorld 모딩 위키](https://rimworldwiki.com/wiki/Modding_Tutorials)
- [Harmony 문서](https://harmony.pardeike.net/)
- [RimWorld API 문서](https://spdskatr.github.io/RimWorld-Decompile/)

## 문의

문제가 발생하거나 제안사항이 있으시면 GitHub Issues에 등록해주세요.

---

**Happy Modding!** 🎮
