#!/usr/bin/env python3
"""
EVE Online Skill Training Time Validator

This tool validates EVEMon's skill training time calculations against
the official EVE Online formulas from EVE University Wiki.

Formulas:
- SP/hour (Omega) = Primary × 60 + Secondary × 30
- SP/hour (Alpha) = Primary × 30 + Secondary × 15
- SP for level = 250 × rank × sqrt(32)^(level-1)
- Training time = SP to train / SP per hour

Cerebral Accelerators add a flat bonus to ALL attributes.
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
    """Character attributes including implants and boosters"""
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

    # Booster bonus (applies to ALL attributes)
    booster_bonus: int = 0

    def get_effective(self, attr_name: str) -> int:
        """Get effective attribute value including implants and boosters"""
        base = getattr(self, attr_name)
        implant = getattr(self, f"{attr_name[:3]}_implant")
        return base + implant + self.booster_bonus

    @property
    def effective_intelligence(self) -> int:
        return self.intelligence + self.int_implant + self.booster_bonus

    @property
    def effective_perception(self) -> int:
        return self.perception + self.per_implant + self.booster_bonus

    @property
    def effective_charisma(self) -> int:
        return self.charisma + self.cha_implant + self.booster_bonus

    @property
    def effective_willpower(self) -> int:
        return self.willpower + self.wil_implant + self.booster_bonus

    @property
    def effective_memory(self) -> int:
        return self.memory + self.mem_implant + self.booster_bonus


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

    Omega: SP/hour = Primary × 60 + Secondary × 30
    Alpha: SP/hour = Primary × 30 + Secondary × 15
    """
    # Get effective attribute values
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
        return "∞"

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
                          booster_bonus: int = 0, booster_hours: float = 0,
                          clone_state: CloneState = CloneState.OMEGA):
    """
    Validate training time for a single skill, optionally with a booster.

    If booster_hours > 0, calculates split training (boosted + non-boosted).
    """
    print(f"\n{'='*60}")
    print(f"Skill: {skill.name} (Rank {skill.rank})")
    print(f"Training: Level {skill.current_level} → Level {target_level}")
    print(f"Attributes: {skill.primary_attr.capitalize()} (Primary), "
          f"{skill.secondary_attr.capitalize()} (Secondary)")
    print(f"{'='*60}")

    # Calculate SP needed
    sp_needed = skill.sp_to_train(target_level)
    print(f"\nSP to train: {sp_needed:,}")

    # Calculate without booster
    sp_per_hour_base = calculate_sp_per_hour(attrs, skill, clone_state)
    time_base_hours = calculate_training_time(sp_needed, sp_per_hour_base)

    # Get attribute values for display
    attr_map = {
        'intelligence': attrs.effective_intelligence,
        'perception': attrs.effective_perception,
        'charisma': attrs.effective_charisma,
        'willpower': attrs.effective_willpower,
        'memory': attrs.effective_memory,
    }

    primary_val = attr_map[skill.primary_attr]
    secondary_val = attr_map[skill.secondary_attr]

    print(f"\n--- Without Booster ---")
    print(f"Primary ({skill.primary_attr}): {primary_val}")
    print(f"Secondary ({skill.secondary_attr}): {secondary_val}")
    print(f"SP/hour: {sp_per_hour_base:,.0f}")
    print(f"Training time: {format_time(time_base_hours)}")

    # Calculate with booster if specified
    if booster_bonus > 0:
        # Create boosted attributes
        boosted_attrs = Attributes(
            intelligence=attrs.intelligence,
            perception=attrs.perception,
            charisma=attrs.charisma,
            willpower=attrs.willpower,
            memory=attrs.memory,
            int_implant=attrs.int_implant,
            per_implant=attrs.per_implant,
            cha_implant=attrs.cha_implant,
            wil_implant=attrs.wil_implant,
            mem_implant=attrs.mem_implant,
            booster_bonus=booster_bonus
        )

        sp_per_hour_boosted = calculate_sp_per_hour(boosted_attrs, skill, clone_state)

        boosted_primary = attr_map[skill.primary_attr] + booster_bonus
        boosted_secondary = attr_map[skill.secondary_attr] + booster_bonus

        print(f"\n--- With +{booster_bonus} Booster ---")
        print(f"Primary ({skill.primary_attr}): {boosted_primary} (+{booster_bonus})")
        print(f"Secondary ({skill.secondary_attr}): {boosted_secondary} (+{booster_bonus})")
        print(f"SP/hour: {sp_per_hour_boosted:,.0f}")

        if booster_hours > 0 and booster_hours * sp_per_hour_boosted < sp_needed:
            # Split calculation - booster expires mid-training
            sp_trained_boosted = booster_hours * sp_per_hour_boosted
            sp_remaining = sp_needed - sp_trained_boosted
            time_remaining = sp_remaining / sp_per_hour_base
            total_time = booster_hours + time_remaining

            print(f"Booster duration: {format_time(booster_hours)}")
            print(f"SP trained while boosted: {sp_trained_boosted:,.0f}")
            print(f"SP remaining after booster: {sp_remaining:,.0f}")
            print(f"Time for remaining SP: {format_time(time_remaining)}")
            print(f"Total training time: {format_time(total_time)}")
        else:
            # Full training under booster
            time_boosted_hours = calculate_training_time(sp_needed, sp_per_hour_boosted)
            print(f"Training time (full booster): {format_time(time_boosted_hours)}")

        # Show time saved
        time_boosted_full = calculate_training_time(sp_needed, sp_per_hour_boosted)
        time_saved = time_base_hours - time_boosted_full
        print(f"\n--- Time Saved (full duration) ---")
        print(f"Without booster: {format_time(time_base_hours)}")
        print(f"With booster:    {format_time(time_boosted_full)}")
        print(f"Time saved:      {format_time(time_saved)} ({time_saved/time_base_hours*100:.1f}%)")


def validate_plan(attrs: Attributes, skills: list, booster_bonus: int = 0,
                  booster_hours: float = 0, clone_state: CloneState = CloneState.OMEGA):
    """
    Validate training time for a plan (list of skills).
    Tracks booster duration across skills.
    """
    print(f"\n{'='*60}")
    print(f"PLAN VALIDATION")
    if booster_bonus > 0:
        print(f"Booster: +{booster_bonus} for {format_time(booster_hours)}")
    print(f"{'='*60}")

    total_time_base = 0
    total_time_boosted = 0
    booster_remaining = booster_hours

    boosted_attrs = Attributes(
        intelligence=attrs.intelligence,
        perception=attrs.perception,
        charisma=attrs.charisma,
        willpower=attrs.willpower,
        memory=attrs.memory,
        int_implant=attrs.int_implant,
        per_implant=attrs.per_implant,
        cha_implant=attrs.cha_implant,
        wil_implant=attrs.wil_implant,
        mem_implant=attrs.mem_implant,
        booster_bonus=booster_bonus
    )

    print(f"\n{'Skill':<30} {'Level':<8} {'SP':>10} {'Base':>12} {'Boosted':>12} {'Saved':>10}")
    print("-" * 82)

    for skill, target_level in skills:
        sp_needed = skill.sp_to_train(target_level)
        sp_per_hour_base = calculate_sp_per_hour(attrs, skill, clone_state)
        sp_per_hour_boosted = calculate_sp_per_hour(boosted_attrs, skill, clone_state)

        time_base = calculate_training_time(sp_needed, sp_per_hour_base)
        total_time_base += time_base

        if booster_bonus > 0 and booster_remaining > 0:
            # Calculate with booster (possibly partial)
            sp_can_train_boosted = booster_remaining * sp_per_hour_boosted

            if sp_can_train_boosted >= sp_needed:
                # Full skill under booster
                time_actual = calculate_training_time(sp_needed, sp_per_hour_boosted)
                booster_remaining -= time_actual
            else:
                # Booster expires mid-skill
                time_boosted_part = booster_remaining
                sp_trained_boosted = time_boosted_part * sp_per_hour_boosted
                sp_remaining = sp_needed - sp_trained_boosted
                time_normal_part = calculate_training_time(sp_remaining, sp_per_hour_base)
                time_actual = time_boosted_part + time_normal_part
                booster_remaining = 0

            time_saved = time_base - time_actual
            total_time_boosted += time_actual
        else:
            time_actual = time_base
            time_saved = 0
            total_time_boosted += time_actual

        print(f"{skill.name:<30} {skill.current_level}→{target_level:<5} "
              f"{sp_needed:>10,} {format_time(time_base):>12} "
              f"{format_time(time_actual):>12} {format_time(time_saved):>10}")

        # Update skill state for next iteration
        skill.current_level = target_level
        skill.current_sp = 0

    print("-" * 82)
    print(f"{'TOTAL':<30} {'':<8} {'':<10} {format_time(total_time_base):>12} "
          f"{format_time(total_time_boosted):>12} {format_time(total_time_base - total_time_boosted):>10}")


def interactive_mode():
    """Interactive mode for quick calculations"""
    print("\n" + "="*60)
    print("EVE Online Training Time Calculator - Interactive Mode")
    print("="*60)

    # Get attributes
    print("\nEnter character attributes (base + implants):")
    try:
        primary = int(input("Primary attribute value: "))
        secondary = int(input("Secondary attribute value: "))
        skill_rank = int(input("Skill rank (multiplier): "))
        current_level = int(input("Current skill level (0-4): "))
        target_level = int(input("Target skill level (1-5): "))
        booster = int(input("Booster bonus (0 for none): "))

        # Create a simple skill
        skill = Skill(
            name="Test Skill",
            rank=skill_rank,
            primary_attr="perception",
            secondary_attr="willpower",
            current_level=current_level
        )

        # Create attributes
        attrs = Attributes(
            perception=primary,
            willpower=secondary
        )

        validate_single_skill(attrs, skill, target_level, booster)

    except ValueError as e:
        print(f"Invalid input: {e}")


def example_usage():
    """Show example usage with typical EVEMon scenario"""
    print("\n" + "="*60)
    print("EXAMPLE: Validating Booster Time Savings")
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

    # Example skill: Gunnery (Perception/Willpower, Rank 1)
    gunnery = Skill(
        name="Gunnery",
        rank=1,
        primary_attr="perception",
        secondary_attr="willpower",
        current_level=0
    )

    print("\n--- Single Skill: Gunnery 0→5 ---")
    validate_single_skill(attrs, gunnery, 5, booster_bonus=10, booster_hours=24)

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

    validate_plan(attrs, skills, booster_bonus=10, booster_hours=24)


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
        # Default: show example
        example_usage()
        print("\n\nUse -i for interactive mode, -e for examples")
