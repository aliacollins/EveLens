#!/usr/bin/env python3
"""
EVE Online Skill Training Time Validator

This tool validates EveLens's skill training time calculations against
the official EVE Online formulas from EVE University Wiki.

Formulas:
- SP/hour (Omega) = Primary x 60 + Secondary x 30
- SP/hour (Alpha) = Primary x 30 + Secondary x 15
- SP for level = 250 x rank x sqrt(32)^(level-1)
- Training time = SP to train / SP per hour
"""

import math
import argparse
from dataclasses import dataclass
from typing import Optional
from enum import Enum

# SP required for each level (multiplied by skill rank)
# Formula: 250 * rank * sqrt(32)^(level-1)
SQRT32 = math.sqrt(32)
SP_MULTIPLIERS = {
    1: 250,
    2: 250 * SQRT32,        # ~1,414
    3: 250 * SQRT32**2,     # 8,000
    4: 250 * SQRT32**3,     # ~45,255
    5: 250 * SQRT32**4,     # 256,000
}

# Total SP for each level (cumulative)
TOTAL_SP_FOR_LEVEL = {
    0: 0,
    1: 250,
    2: 1415,      # 250 + 1,414
    3: 8000,      # Actually 1,415 + 6,585 but EVE rounds
    4: 45255,
    5: 256000,
}


class CloneState(Enum):
    OMEGA = "omega"
    ALPHA = "alpha"


@dataclass
class Attributes:
    """Character attributes including implants"""
    intelligence: int = 17  # Base is 17-27 depending on remap
    perception: int = 17
    charisma: int = 17
    willpower: int = 17
    memory: int = 17

    # Implant bonuses (0-6 typically)
    int_implant: int = 0
    per_implant: int = 0
    cha_implant: int = 0
    wil_implant: int = 0
    mem_implant: int = 0

    @property
    def effective_intelligence(self) -> int:
        return self.intelligence + self.int_implant

    @property
    def effective_perception(self) -> int:
        return self.perception + self.per_implant

    @property
    def effective_charisma(self) -> int:
        return self.charisma + self.cha_implant

    @property
    def effective_willpower(self) -> int:
        return self.willpower + self.wil_implant

    @property
    def effective_memory(self) -> int:
        return self.memory + self.mem_implant


@dataclass
class Skill:
    """Represents a skill to train"""
    name: str
    rank: int  # Skill multiplier/difficulty (1-16)
    primary_attr: str  # 'intelligence', 'perception', etc.
    secondary_attr: str
    current_level: int = 0
    current_sp: int = 0

    def sp_for_level(self, level: int) -> int:
        """Total SP required to complete a level"""
        if level < 1 or level > 5:
            return 0
        return int(250 * self.rank * (SQRT32 ** (level - 1)))

    def total_sp_at_level(self, level: int) -> int:
        """Total cumulative SP at a given level"""
        total = 0
        for lvl in range(1, level + 1):
            total += self.sp_for_level(lvl)
        return total

    def sp_to_train(self, target_level: int) -> int:
        """SP needed to train from current to target level"""
        if target_level <= self.current_level:
            return 0
        target_sp = self.total_sp_at_level(target_level)
        current_total = self.total_sp_at_level(self.current_level) + self.current_sp
        return max(0, target_sp - current_total)


def calculate_sp_per_hour(attrs: Attributes, skill: Skill,
                          clone_state: CloneState = CloneState.OMEGA) -> float:
    """
    Calculate SP per hour for training a skill.

    Omega: SP/hour = Primary x 60 + Secondary x 30
    Alpha: SP/hour = Primary x 30 + Secondary x 15
    """
    attr_map = {
        'intelligence': attrs.effective_intelligence,
        'perception': attrs.effective_perception,
        'charisma': attrs.effective_charisma,
        'willpower': attrs.effective_willpower,
        'memory': attrs.effective_memory,
    }

    primary = attr_map[skill.primary_attr]
    secondary = attr_map[skill.secondary_attr]

    if clone_state == CloneState.OMEGA:
        return primary * 60 + secondary * 30
    else:  # Alpha
        return primary * 30 + secondary * 15


def calculate_training_time(sp: int, sp_per_hour: float) -> float:
    """Calculate training time in hours"""
    if sp_per_hour <= 0:
        return float('inf')
    return sp / sp_per_hour


def format_time(hours: float) -> str:
    """Format hours into days, hours, minutes, seconds"""
    if hours == float('inf'):
        return "inf"

    total_seconds = int(hours * 3600)
    days, remainder = divmod(total_seconds, 86400)
    hours_part, remainder = divmod(remainder, 3600)
    minutes, seconds = divmod(remainder, 60)

    parts = []
    if days > 0:
        parts.append(f"{days}d")
    if hours_part > 0:
        parts.append(f"{hours_part}h")
    if minutes > 0:
        parts.append(f"{minutes}m")
    if seconds > 0 or not parts:
        parts.append(f"{seconds}s")

    return " ".join(parts)


def validate_single_skill(attrs: Attributes, skill: Skill, target_level: int,
                          clone_state: CloneState = CloneState.OMEGA):
    """Validate training time for a single skill."""
    print(f"\n{'='*60}")
    print(f"Skill: {skill.name} (Rank {skill.rank})")
    print(f"Training: Level {skill.current_level} -> Level {target_level}")
    print(f"Attributes: {skill.primary_attr.capitalize()} (Primary), "
          f"{skill.secondary_attr.capitalize()} (Secondary)")
    print(f"{'='*60}")

    sp_needed = skill.sp_to_train(target_level)
    print(f"\nSP to train: {sp_needed:,}")

    sp_per_hour = calculate_sp_per_hour(attrs, skill, clone_state)
    time_hours = calculate_training_time(sp_needed, sp_per_hour)

    attr_map = {
        'intelligence': attrs.effective_intelligence,
        'perception': attrs.effective_perception,
        'charisma': attrs.effective_charisma,
        'willpower': attrs.effective_willpower,
        'memory': attrs.effective_memory,
    }

    primary_val = attr_map[skill.primary_attr]
    secondary_val = attr_map[skill.secondary_attr]

    print(f"Primary ({skill.primary_attr}): {primary_val}")
    print(f"Secondary ({skill.secondary_attr}): {secondary_val}")
    print(f"SP/hour: {sp_per_hour:,.0f}")
    print(f"Training time: {format_time(time_hours)}")


def validate_plan(attrs: Attributes, skills: list,
                  clone_state: CloneState = CloneState.OMEGA):
    """Validate training time for a plan (list of skills)."""
    print(f"\n{'='*60}")
    print(f"PLAN VALIDATION")
    print(f"{'='*60}")

    total_time = 0

    print(f"\n{'Skill':<30} {'Level':<8} {'SP':>10} {'Time':>12}")
    print("-" * 60)

    for skill, target_level in skills:
        sp_needed = skill.sp_to_train(target_level)
        sp_per_hour = calculate_sp_per_hour(attrs, skill, clone_state)
        time_hours = calculate_training_time(sp_needed, sp_per_hour)
        total_time += time_hours

        print(f"{skill.name:<30} {skill.current_level}->{target_level:<5} "
              f"{sp_needed:>10,} {format_time(time_hours):>12}")

        skill.current_level = target_level
        skill.current_sp = 0

    print("-" * 60)
    print(f"{'TOTAL':<30} {'':<8} {'':<10} {format_time(total_time):>12}")


def interactive_mode():
    """Interactive mode for quick calculations"""
    print("\n" + "="*60)
    print("EVE Online Training Time Calculator - Interactive Mode")
    print("="*60)

    print("\nEnter character attributes (base + implants):")
    try:
        primary = int(input("Primary attribute value: "))
        secondary = int(input("Secondary attribute value: "))
        skill_rank = int(input("Skill rank (multiplier): "))
        current_level = int(input("Current skill level (0-4): "))
        target_level = int(input("Target skill level (1-5): "))

        skill = Skill(
            name="Test Skill",
            rank=skill_rank,
            primary_attr="perception",
            secondary_attr="willpower",
            current_level=current_level
        )

        attrs = Attributes(
            perception=primary,
            willpower=secondary
        )

        validate_single_skill(attrs, skill, target_level)

    except ValueError as e:
        print(f"Invalid input: {e}")


def example_usage():
    """Show example usage with typical scenario"""
    print("\n" + "="*60)
    print("EXAMPLE: Training Time Calculation")
    print("="*60)

    # Typical character attributes (remapped for combat skills)
    attrs = Attributes(
        intelligence=17,
        perception=27,      # Maxed
        charisma=17,
        willpower=21,       # Secondary focus
        memory=17,
        per_implant=5,      # +5 implant
        wil_implant=5,      # +5 implant
    )

    gunnery = Skill(
        name="Gunnery",
        rank=1,
        primary_attr="perception",
        secondary_attr="willpower",
        current_level=0
    )

    print("\n--- Single Skill: Gunnery 0->5 ---")
    validate_single_skill(attrs, gunnery, 5)

    # Example plan with multiple skills
    print("\n" + "="*60)
    print("EXAMPLE: Plan with Multiple Skills")
    print("="*60)

    skills = [
        (Skill("Gunnery", 1, "perception", "willpower", 0), 5),
        (Skill("Small Hybrid Turret", 1, "perception", "willpower", 0), 5),
        (Skill("Motion Prediction", 2, "perception", "willpower", 0), 4),
        (Skill("Rapid Firing", 2, "perception", "willpower", 0), 4),
        (Skill("Sharpshooter", 2, "perception", "willpower", 0), 4),
    ]

    validate_plan(attrs, skills)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="EVE Online Training Time Validator")
    parser.add_argument("-i", "--interactive", action="store_true",
                        help="Run in interactive mode")
    parser.add_argument("-e", "--example", action="store_true",
                        help="Show example usage")

    args = parser.parse_args()

    if args.interactive:
        interactive_mode()
    elif args.example:
        example_usage()
    else:
        example_usage()
        print("\n\nUse -i for interactive mode, -e for examples")
