#!/bin/bash
# RimAI GA Cycle Runner - Bash Script
# Usage: ./run-ga-cycle.sh 0 20 3 0.15 tournament

set -e

# Arguments
GENERATION=${1}
POPULATION_SIZE=${2:-20}
ELITE_COUNT=${3:-3}
MUTATION_RATE=${4:-0.15}
SELECTION_METHOD=${5:-tournament}
SKIP_GAMEPLAY=${SKIP_GAMEPLAY:-false}
ONLY_STATS=${ONLY_STATS:-false}

if [ -z "$GENERATION" ]; then
    echo "Usage: $0 <generation> [population_size] [elite_count] [mutation_rate] [selection_method]"
    echo "Example: $0 0 20 3 0.15 tournament"
    echo ""
    echo "Environment variables:"
    echo "  SKIP_GAMEPLAY=true     Skip game play phase"
    echo "  ONLY_STATS=true        Only show statistics"
    exit 1
fi

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

# Paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
CONFIG_DIR="$HOME/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Config/RimAI/GA"
EXPERIMENT_DIR="$ROOT_DIR/ga_experiments/generation_$GENERATION"
NEXT_GENERATION=$((GENERATION + 1))
NEXT_EXPERIMENT_DIR="$ROOT_DIR/ga_experiments/generation_$NEXT_GENERATION"
GA_TRAINER_EXE="$ROOT_DIR/GATrainer/bin/Debug/net472/GATrainer.exe"

echo -e "${CYAN}=== RimAI GA Cycle Runner ===${NC}"
echo -e "${YELLOW}Generation: $GENERATION -> $NEXT_GENERATION${NC}"
echo -e "${YELLOW}Population Size: $POPULATION_SIZE${NC}"
echo -e "${YELLOW}Elite Count: $ELITE_COUNT${NC}"
echo -e "${YELLOW}Mutation Rate: $MUTATION_RATE${NC}"
echo -e "${YELLOW}Selection Method: $SELECTION_METHOD${NC}"
echo ""

# Validate paths
if [ ! -f "$GA_TRAINER_EXE" ]; then
    echo -e "${RED}Error: GATrainer.exe not found at: $GA_TRAINER_EXE${NC}"
    echo "Please build GATrainer first: cd GATrainer && dotnet build"
    exit 1
fi

if [ ! -d "$EXPERIMENT_DIR" ]; then
    echo -e "${RED}Error: Experiment directory not found: $EXPERIMENT_DIR${NC}"
    echo "Please create it or run 'init' command first"
    exit 1
fi

# Only show stats
if [ "$ONLY_STATS" = "true" ]; then
    echo -e "${GREEN}[1/5] Statistics Only Mode${NC}"
    echo -e "Loading generation $GENERATION statistics..."

    GENOMES_DIR="$EXPERIMENT_DIR/genomes"
    RUNS_DIR="$EXPERIMENT_DIR/runs"

    "$GA_TRAINER_EXE" stats "$GENOMES_DIR" "$RUNS_DIR"
    exit 0
fi

# Step 1: Copy genomes to RimWorld config
if [ "$SKIP_GAMEPLAY" != "true" ]; then
    echo -e "${GREEN}[1/5] Copying genomes to RimWorld config...${NC}"

    GENOMES_DIR="$EXPERIMENT_DIR/genomes"
    TARGET_GENOMES_DIR="$CONFIG_DIR/genomes"

    mkdir -p "$TARGET_GENOMES_DIR"

    GENOME_FILES=("$GENOMES_DIR"/gen${GENERATION}_*.json)

    if [ ! -e "${GENOME_FILES[0]}" ]; then
        echo -e "${RED}Error: No genome files found in: $GENOMES_DIR${NC}"
        exit 1
    fi

    COUNT=0
    for file in "${GENOME_FILES[@]}"; do
        cp "$file" "$TARGET_GENOMES_DIR/"
        COUNT=$((COUNT + 1))
    done

    echo -e "Copied $COUNT genome files"
    echo ""

    # Step 2: Game play instructions
    echo -e "${GREEN}[2/5] Game Play Phase${NC}"
    echo -e "Please play RimWorld with each genome:"
    echo ""

    COUNTER=1
    for file in "${GENOME_FILES[@]}"; do
        BASENAME=$(basename "$file")
        echo -e "  ${YELLOW}[$COUNTER/$COUNT] $BASENAME${NC}"
        echo -e "    ${GRAY}-> Copy to current.json: cp '$file' '$TARGET_GENOMES_DIR/current.json'${NC}"
        echo -e "    ${GRAY}-> Launch RimWorld -> New Game -> Play until ending/wipe${NC}"
        echo -e "    ${GRAY}-> Metrics will be saved automatically to runs/${NC}"
        echo ""
        COUNTER=$((COUNTER + 1))
    done

    echo -e "${CYAN}Press Enter when all games are finished...${NC}"
    read

    # Step 3: Collect metrics
    echo -e "${GREEN}[3/5] Collecting metrics...${NC}"

    SOURCE_RUNS_DIR="$CONFIG_DIR/runs"
    TARGET_RUNS_DIR="$EXPERIMENT_DIR/runs"

    mkdir -p "$TARGET_RUNS_DIR"

    if [ -d "$SOURCE_RUNS_DIR" ]; then
        METRICS_COUNT=$(find "$SOURCE_RUNS_DIR" -name "*.json" -type f | wc -l)

        if [ "$METRICS_COUNT" -eq 0 ]; then
            echo -e "${YELLOW}Warning: No metrics files found in: $SOURCE_RUNS_DIR${NC}"
        else
            cp "$SOURCE_RUNS_DIR"/*.json "$TARGET_RUNS_DIR/" 2>/dev/null || true
            echo -e "Collected $METRICS_COUNT metrics files"
        fi
    else
        echo -e "${YELLOW}Warning: Runs directory not found: $SOURCE_RUNS_DIR${NC}"
    fi

    echo ""
else
    echo -e "${YELLOW}[1-3/5] Skipping game play phase (SKIP_GAMEPLAY=true)${NC}"
    echo ""
fi

# Step 4: Show statistics
echo -e "${GREEN}[4/5] Generation $GENERATION Statistics${NC}"

GENOMES_DIR="$EXPERIMENT_DIR/genomes"
RUNS_DIR="$EXPERIMENT_DIR/runs"

"$GA_TRAINER_EXE" stats "$GENOMES_DIR" "$RUNS_DIR"

echo ""

# Step 5: Generate next generation
echo -e "${GREEN}[5/5] Generating next generation...${NC}"

NEXT_GENOMES_DIR="$NEXT_EXPERIMENT_DIR/genomes"
NEXT_RUNS_DIR="$NEXT_EXPERIMENT_DIR/runs"

mkdir -p "$NEXT_GENOMES_DIR"
mkdir -p "$NEXT_RUNS_DIR"

"$GA_TRAINER_EXE" evolve \
    "$GENOMES_DIR" \
    "$RUNS_DIR" \
    "$NEXT_GENOMES_DIR" \
    --size=$POPULATION_SIZE \
    --elite=$ELITE_COUNT \
    --mutation=$MUTATION_RATE \
    --selection=$SELECTION_METHOD

echo ""
echo -e "${CYAN}=== Complete ===${NC}"
echo -e "${GREEN}Generation $NEXT_GENERATION is ready at: $NEXT_GENOMES_DIR${NC}"
echo ""
echo -e "${YELLOW}Next steps:${NC}"
echo -e "  1. Review next generation genomes in: $NEXT_GENOMES_DIR"
echo -e "  2. Run next cycle: $0 $NEXT_GENERATION"
echo ""
