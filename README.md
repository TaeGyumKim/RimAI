# RimAI - RimWorld Auto Pilot Mod

RimWorld **완전 자동화** 모드. 식량, 건설, 연구, 전투 등 대부분의 결정을 자동으로 처리하여 **진짜 "관람용 방치 플레이"**를 가능하게 하는 오토파일럿 시스템입니다.

## 프로젝트 개요

RimAI는 RimWorld의 기본 AI 위에 올라가는 **상위 의사결정 레이어**를 제공하여, 플레이어가 거의 개입하지 않아도 콜로니가 효율적으로 운영될 수 있도록 돕습니다.

### 주요 기능
- ✅ **식량 자동 관리**: 농사/사냥/요리 우선순위 자동 조절
- ✅ **건설 자동화**: 침대, 인프라, 방어 시설 자동 계획
- ✅ **연구 자동 선택**: 콜로니 상황에 맞는 최적 연구 선택
- ✅ **전투 자동 대응**: 적 습격 시 자동 징집/해제 + 시네마틱 카메라
- ✅ **설정 시스템**: 프리셋 및 세부 조정으로 개입 강도 제어
- 🎬 **관람 모드**: 그냥 앉아서 콜로니가 스스로 성장하는 걸 구경하세요!

### 현재 버전
**v0.4.0 (베타)** - Stage 5: 설정/UX 레이어 + 완전한 관람 모드

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

## 설치 및 사용

### 설치

1. RimWorld를 실행합니다.
2. 메인 메뉴에서 **Mods** 클릭
3. **RimAI - Auto Pilot** 모드를 찾아 활성화
4. 게임을 재시작하거나 새 게임/기존 저장 파일 로드

### 설정

**모드 설정 메뉴 접근**:
1. 게임 내 또는 메인 메뉴에서 **옵션 (Options)** 선택
2. **모드 설정 (Mod Settings)** 탭 클릭
3. **RimAI - Auto Pilot** 선택

**빠른 시작 (프리셋)**:
- **완전 방치 관람**: 모든 자동화 최대 - 그냥 관람만!
- **위기만 자동 대응**: 식량/전투만 자동화, 건설/연구는 직접
- **디버그 모드**: 개발/테스트용 (모든 로그 출력)

**세부 설정**:
- 서브시스템별 개입 강도 (OFF/낮음/중간/높음/최대)
- 시네마틱 카메라 ON/OFF
- 로그 상세도 (최소/일반/상세/디버그)

📘 **상세 설정 가이드**: [Settings-Guide.md](Docs/Settings-Guide.md) 참고

### 로그 파일 위치 (문제 발생 시)
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

### ✅ Stage 1: 프로젝트 스캐폴딩 (완료)
- [x] RimWorld 모드 기본 구조 생성
- [x] Harmony 부트스트랩 코드 작성
- [x] 게임 로드 시 콜로니 상태 스캔

### ✅ Stage 2: 식량 자동화 (완료)
- [x] 식량 저장량 모니터링 (FoodAnalyzer)
- [x] 농사/사냥/요리 우선순위 자동 조정 (FoodDecisionEngine)
- [x] 계절별 농사 대응 (겨울 대비, 봄 파종)
- [x] WorkGiver 패치를 통한 작업 제어

### ✅ Stage 3: 중앙 브레인 + 건설/연구 (완료)
- [x] 중앙 브레인 시스템 (RimAIManager)
- [x] 서브시스템 아키텍처 (IRimAISubsystem)
- [x] 액션 기반 의사결정 + 우선순위 조정
- [x] 건설 서브시스템 (침대, 인프라, 방어 시설)
- [x] 연구 서브시스템 (휴리스틱 기반 선택)
- [x] 쿨다운 시스템 (방치 모드 최적화)

### ✅ Stage 4: 전투 자동화 + 시네마틱 카메라 (완료)
- [x] 위협 평가 시스템 (전투력 계산, 위협 레벨)
- [x] 전투 자동 대응 (자동 징집/해제)
- [x] 시네마틱 카메라 (전투 위치 추적 + 줌)
- [x] 중앙 브레인 전투 우선순위 조정

### ✅ Stage 5: 설정/UX 레이어 (완료 - 현재)
- [x] ModSettings 기반 설정 시스템
- [x] 설정 UI (DoSettingsWindowContents)
- [x] 프리셋 시스템 (완전 방치, 위기만 대응, 디버그)
- [x] 서브시스템별 개입 강도 조절 (OFF~최대)
- [x] 로그 레벨 설정 (최소~디버그)
- [x] 설정 가이드 문서

### 📋 Stage 6: 건설 청사진 구현 (예정)
- [ ] 실제 청사진 배치 로직
- [ ] 적절한 위치 찾기 알고리즘
- [ ] 방 레이아웃 플래너
- [ ] 자원 가용성 확인

### 📋 Stage 7: 의료 자동화 (예정)
- [ ] 부상자 자동 치료 우선순위
- [ ] 의료 용품 자동 관리
- [ ] 침대 할당 최적화

### 📋 Stage 8: 생산 자동화 (예정)
- [ ] 생산 작업 자동 계획
- [ ] 자원 관리 시스템
- [ ] 무기/방어구 생산 우선순위

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
