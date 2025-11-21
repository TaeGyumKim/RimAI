#!/bin/bash
# RimAI GA Initialization Script - Bash
# Usage: ./init-ga.sh [population_size] [generation]

set -e

# Arguments
POPULATION_SIZE=${1:-20}
GENERATION=${2:-0}

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
EXPERIMENT_DIR="$ROOT_DIR/ga_experiments/generation_$GENERATION"
GA_TRAINER_EXE="$ROOT_DIR/GATrainer/bin/Debug/net472/GATrainer.exe"

echo -e "${CYAN}=== RimAI GA Initialization ===${NC}"
echo -e "${YELLOW}Generation: $GENERATION${NC}"
echo -e "${YELLOW}Population Size: $POPULATION_SIZE${NC}"
echo ""

# Validate GATrainer
if [ ! -f "$GA_TRAINER_EXE" ]; then
    echo -e "${RED}Error: GATrainer.exe not found at: $GA_TRAINER_EXE${NC}"
    echo -e "${YELLOW}Building GATrainer...${NC}"

    cd "$ROOT_DIR/GATrainer"
    dotnet build
    cd "$ROOT_DIR"

    if [ ! -f "$GA_TRAINER_EXE" ]; then
        echo -e "${RED}Error: Failed to build GATrainer${NC}"
        exit 1
    fi
fi

# Create experiment directory
echo -e "${GREEN}[1/2] Creating experiment directory...${NC}"

GENOMES_DIR="$EXPERIMENT_DIR/genomes"
RUNS_DIR="$EXPERIMENT_DIR/runs"

if [ -d "$EXPERIMENT_DIR" ]; then
    echo -e "${YELLOW}Warning: Experiment directory already exists: $EXPERIMENT_DIR${NC}"
    echo -n "Do you want to overwrite? (y/N): "
    read response

    if [ "$response" != "y" ] && [ "$response" != "Y" ]; then
        echo -e "${RED}Aborted.${NC}"
        exit 1
    fi

    rm -rf "$EXPERIMENT_DIR"
fi

mkdir -p "$GENOMES_DIR"
mkdir -p "$RUNS_DIR"

echo -e "Created: $EXPERIMENT_DIR"
echo ""

# Generate initial population
echo -e "${GREEN}[2/2] Generating initial population...${NC}"

"$GA_TRAINER_EXE" init $POPULATION_SIZE "$GENOMES_DIR"

echo ""
echo -e "${CYAN}=== Complete ===${NC}"
echo -e "${GREEN}Initial population created at: $GENOMES_DIR${NC}"
echo ""
echo -e "${YELLOW}Next steps:${NC}"
echo -e "  1. Review generated genomes in: $GENOMES_DIR"
echo -e "  2. Run GA cycle: $SCRIPT_DIR/run-ga-cycle.sh $GENERATION"
echo ""
