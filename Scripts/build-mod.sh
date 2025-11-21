#!/bin/bash
# build-mod.sh
# RimAI 모드 빌드 스크립트 (Linux/Mac)

set -e

# 색상 정의
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# 기본값
CONFIGURATION="Release"
CLEAN=false
DEPLOY=false
RIMWORLD_PATH=""

# 사용법 출력
usage() {
    echo "Usage: $0 [-c|--clean] [-d|--deploy] [-p|--path RIMWORLD_PATH] [--debug]"
    echo ""
    echo "Options:"
    echo "  -c, --clean    Clean build outputs before building"
    echo "  -d, --deploy   Deploy to RimWorld Mods folder"
    echo "  -p, --path     RimWorld installation path"
    echo "  --debug        Build in Debug configuration"
    echo "  -h, --help     Show this help message"
}

# 옵션 파싱
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--clean)
            CLEAN=true
            shift
            ;;
        -d|--deploy)
            DEPLOY=true
            shift
            ;;
        -p|--path)
            RIMWORLD_PATH="$2"
            shift 2
            ;;
        --debug)
            CONFIGURATION="Debug"
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            usage
            exit 1
            ;;
    esac
done

echo -e "${CYAN}==========================================${NC}"
echo -e "${CYAN}  RimAI Mod Build Script${NC}"
echo -e "${CYAN}==========================================${NC}"

# 스크립트 디렉토리 및 프로젝트 루트
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
SOURCE_DIR="$PROJECT_ROOT/Source/RimAI"
ASSEMBLIES_DIR="$PROJECT_ROOT/Assemblies"

echo -e "${CYAN}Project Root: $PROJECT_ROOT${NC}"
echo -e "${CYAN}Configuration: $CONFIGURATION${NC}"

# Clean 옵션
if [ "$CLEAN" = true ]; then
    echo -e "\n${CYAN}Cleaning build outputs...${NC}"

    rm -rf "$SOURCE_DIR/bin" 2>/dev/null || true
    rm -rf "$SOURCE_DIR/obj" 2>/dev/null || true
    rm -rf "$ASSEMBLIES_DIR" 2>/dev/null || true

    echo -e "${GREEN}Clean completed.${NC}"
fi

# Assemblies 디렉토리 생성
mkdir -p "$ASSEMBLIES_DIR"

# 빌드 실행
echo -e "\n${CYAN}Building RimAI...${NC}"
cd "$SOURCE_DIR"

if dotnet build -c "$CONFIGURATION"; then
    echo -e "${GREEN}Build succeeded!${NC}"
else
    echo -e "${RED}Build failed!${NC}"
    exit 1
fi

cd "$PROJECT_ROOT"

# DLL 복사
echo -e "\n${CYAN}Copying DLL to Assemblies...${NC}"
DLL_SOURCE="$SOURCE_DIR/bin/$CONFIGURATION/net472/RimAI.dll"
DLL_DEST="$ASSEMBLIES_DIR/RimAI.dll"

if [ -f "$DLL_SOURCE" ]; then
    cp "$DLL_SOURCE" "$DLL_DEST"
    echo -e "${GREEN}DLL copied: $DLL_DEST${NC}"
else
    echo -e "${YELLOW}Warning: DLL not found at $DLL_SOURCE${NC}"
fi

# Deploy 옵션
if [ "$DEPLOY" = true ]; then
    echo -e "\n${CYAN}Deploying to RimWorld Mods folder...${NC}"

    # RimWorld 경로 자동 탐지
    if [ -z "$RIMWORLD_PATH" ]; then
        POSSIBLE_PATHS=(
            "$HOME/.steam/steam/steamapps/common/RimWorld"
            "$HOME/.local/share/Steam/steamapps/common/RimWorld"
            "$HOME/Library/Application Support/Steam/steamapps/common/RimWorld"
        )

        for path in "${POSSIBLE_PATHS[@]}"; do
            if [ -d "$path" ]; then
                RIMWORLD_PATH="$path"
                break
            fi
        done
    fi

    if [ -z "$RIMWORLD_PATH" ]; then
        echo -e "${YELLOW}Warning: RimWorld path not found. Specify with -p${NC}"
    else
        MODS_PATH="$RIMWORLD_PATH/Mods/RimAI"

        # 기존 모드 폴더 삭제
        rm -rf "$MODS_PATH" 2>/dev/null || true

        # 필요한 폴더만 복사
        mkdir -p "$MODS_PATH"

        for folder in About Assemblies Defs Languages Patches Textures; do
            if [ -d "$PROJECT_ROOT/$folder" ]; then
                cp -r "$PROJECT_ROOT/$folder" "$MODS_PATH/"
            fi
        done

        echo -e "${GREEN}Deployed to: $MODS_PATH${NC}"
    fi
fi

# 빌드 정보 출력
echo -e "\n${CYAN}==========================================${NC}"
echo -e "${CYAN}  Build Summary${NC}"
echo -e "${CYAN}==========================================${NC}"

if [ -f "$DLL_DEST" ]; then
    DLL_SIZE=$(du -h "$DLL_DEST" | cut -f1)
    DLL_DATE=$(date -r "$DLL_DEST" "+%Y-%m-%d %H:%M:%S")

    echo -e "${CYAN}DLL: $DLL_DEST${NC}"
    echo -e "${CYAN}Size: $DLL_SIZE${NC}"
    echo -e "${CYAN}Modified: $DLL_DATE${NC}"
fi

echo -e "\n${GREEN}Build completed successfully!${NC}"
