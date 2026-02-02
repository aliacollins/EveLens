#!/usr/bin/env python3
"""
EVEMon Export Validator

This script reads the CSV exported from EVEMon (Ctrl+Shift+D in Plan Window)
and validates the calculations against the official EVE Online formulas.

Usage:
    python validate_evemon_export.py <path_to_csv>
"""

import csv
import sys
from dataclasses import dataclass
from typing import List, Optional, Tuple


@dataclass
class SkillEntry:
    """Parsed skill entry from EVEMon export"""
    name: str
    level: int
    rank: int
    primary_attr: str
    secondary_attr: str
    primary_value: float
    secondary_value: float
    booster_bonus: int
    sp_to_train: int
    sp_per_hour_omega: float
    training_rate: float
    training_time_hours: float
    training_time_formatted: str
    old_training_time_hours: float
    time_saved_hours: float
    booster_remaining_hours: float


def calculate_expected_sp_per_hour(primary: float, secondary: float) -> float:
    """Calculate expected SP/hour using EVE formula: Primary*60 + Secondary*30 (Omega rate)"""
    return primary * 60 + secondary * 30


def calculate_expected_training_time(sp: int, sp_per_hour_omega: float, training_rate: float) -> float:
    """Calculate expected training time in hours, accounting for clone state"""
    actual_sp_per_hour = sp_per_hour_omega * training_rate
    if actual_sp_per_hour <= 0:
        return float('inf')
    return sp / actual_sp_per_hour


def format_time(hours: float) -> str:
    """Format hours into readable time"""
    if hours == float('inf'):
        return "inf"
    if hours < 0:
        return f"-{format_time(abs(hours))}"
    total_seconds = int(hours * 3600)
    days, remainder = divmod(total_seconds, 86400)
    hours_part, remainder = divmod(remainder, 3600)
    minutes, seconds = divmod(remainder, 60)

    if days > 0:
        return f"{days}d {hours_part}h {minutes}m"
    if hours_part > 0:
        return f"{hours_part}h {minutes}m {seconds}s"
    return f"{minutes}m {seconds}s"


def validate_entry(entry: SkillEntry, tolerance: float = 0.01) -> List[str]:
    """
    Validate a single entry against expected calculations.
    Returns list of errors (empty if valid).
    """
    errors = []

    # Validate SP/hour calculation (Omega rate)
    expected_sp_hour = calculate_expected_sp_per_hour(entry.primary_value, entry.secondary_value)
    sp_hour_diff = abs(entry.sp_per_hour_omega - expected_sp_hour)
    if sp_hour_diff > 1:  # Allow 1 SP/hour rounding difference
        errors.append(f"SP/hour mismatch: EVEMon={entry.sp_per_hour_omega:.0f}, Expected={expected_sp_hour:.0f}")

    # Validate training time calculation (accounting for training rate)
    expected_time = calculate_expected_training_time(entry.sp_to_train, entry.sp_per_hour_omega, entry.training_rate)
    time_diff = abs(entry.training_time_hours - expected_time)
    if time_diff > tolerance:
        errors.append(f"Training time mismatch: EVEMon={entry.training_time_hours:.4f}h, "
                     f"Expected={expected_time:.4f}h (diff={time_diff:.4f}h)")

    # Validate time saved (should be old_time - new_time)
    # Note: time_saved can be negative if booster expired (old < new shouldn't happen though)
    expected_saved = entry.old_training_time_hours - entry.training_time_hours
    saved_diff = abs(entry.time_saved_hours - expected_saved)
    if entry.old_training_time_hours > 0 and saved_diff > tolerance:
        errors.append(f"Time saved mismatch: EVEMon={entry.time_saved_hours:.4f}h, "
                     f"Expected={expected_saved:.4f}h")

    return errors


def parse_csv(filepath: str) -> Tuple[List[SkillEntry], dict]:
    """Parse EVEMon validation CSV export"""
    entries = []
    metadata = {}

    with open(filepath, 'r', encoding='utf-8-sig') as f:
        lines = f.readlines()

    # Parse metadata lines (start with #)
    data_lines = []
    for line in lines:
        line = line.strip()
        if line.startswith('#'):
            # Parse metadata
            if 'CloneStatus:' in line:
                parts = line.replace('#', '').strip().split(',')
                for part in parts:
                    if ':' in part:
                        key, value = part.split(':', 1)
                        metadata[key.strip()] = value.strip()
            elif 'HasBoosterInjections:' in line:
                metadata['HasBoosterInjections'] = 'True' in line
        elif line:
            data_lines.append(line)

    # Parse CSV data
    if not data_lines:
        return entries, metadata

    # Create CSV reader from data lines
    import io
    csv_content = '\n'.join(data_lines)
    reader = csv.DictReader(io.StringIO(csv_content))

    for row in reader:
        # Skip empty rows or summary rows
        if not row.get('SkillName') or row['SkillName'] == 'TOTAL':
            continue

        try:
            entry = SkillEntry(
                name=row['SkillName'],
                level=int(row['Level']),
                rank=int(row['Rank']),
                primary_attr=row['PrimaryAttr'],
                secondary_attr=row['SecondaryAttr'],
                primary_value=float(row['PrimaryValue']),
                secondary_value=float(row['SecondaryValue']),
                booster_bonus=int(row['BoosterBonus']),
                sp_to_train=int(row['SPToTrain']),
                sp_per_hour_omega=float(row['SPPerHourOmega']),
                training_rate=float(row['TrainingRate']),
                training_time_hours=float(row['TrainingTimeHours']),
                training_time_formatted=row['TrainingTimeFormatted'],
                old_training_time_hours=float(row['OldTrainingTimeHours']),
                time_saved_hours=float(row['TimeSavedHours']),
                booster_remaining_hours=float(row['BoosterRemainingHours'])
            )
            entries.append(entry)
        except (ValueError, KeyError) as e:
            print(f"Warning: Could not parse row: {row} - {e}")

    return entries, metadata


def main():
    if len(sys.argv) < 2:
        print("Usage: python validate_evemon_export.py <path_to_csv>")
        print("\nTo export from EVEMon: Press Ctrl+Shift+D in the Plan Window")
        sys.exit(1)

    filepath = sys.argv[1]
    print(f"Validating: {filepath}")
    print("=" * 80)

    entries, metadata = parse_csv(filepath)

    if metadata:
        print("\nMetadata:")
        for key, value in metadata.items():
            print(f"  {key}: {value}")

    if not entries:
        print("\nNo valid entries found in CSV")
        sys.exit(1)

    # Determine clone status
    training_rate = entries[0].training_rate if entries else 1.0
    clone_status = "Alpha" if training_rate < 1.0 else "Omega"

    print(f"\nClone Status: {clone_status} (training rate: {training_rate}x)")
    print(f"Total skills: {len(entries)}")

    total_errors = 0
    total_time = 0
    total_old_time = 0
    total_time_saved = 0
    has_booster = any(e.booster_bonus > 0 for e in entries)

    print(f"\n{'Skill':<30} {'Lvl':<4} {'SP/h':>7} {'Rate':>5} {'Time':>12} {'OldTime':>12} {'Saved':>10} {'Status':<8}")
    print("-" * 100)

    for entry in entries:
        errors = validate_entry(entry)
        status = "OK" if not errors else "ERROR"

        total_time += entry.training_time_hours
        total_old_time += entry.old_training_time_hours
        if entry.time_saved_hours > 0:
            total_time_saved += entry.time_saved_hours

        saved_str = format_time(entry.time_saved_hours) if entry.time_saved_hours > 0 else "-"
        old_time_str = format_time(entry.old_training_time_hours) if entry.old_training_time_hours > 0 else "-"

        booster_marker = f"[+{entry.booster_bonus}]" if entry.booster_bonus > 0 else ""

        print(f"{entry.name:<30} {entry.level:<4} {entry.sp_per_hour_omega:>7.0f} {entry.training_rate:>5.1f} "
              f"{format_time(entry.training_time_hours):>12} {old_time_str:>12} {saved_str:>10} {status:<8} {booster_marker}")

        if errors:
            total_errors += len(errors)
            for error in errors:
                print(f"  ERROR: {error}")

    print("-" * 100)
    print(f"{'SUMMARY':<30}")
    print(f"  Total entries: {len(entries)}")
    print(f"  Calculation errors: {total_errors}")
    print(f"  Training time (with booster): {format_time(total_time)}")
    if total_old_time > 0:
        print(f"  Training time (baseline): {format_time(total_old_time)}")
        print(f"  Total time saved: {format_time(total_time_saved)}")
        if total_old_time > 0:
            percent_saved = (total_time_saved / total_old_time) * 100
            print(f"  Percentage saved: {percent_saved:.1f}%")

    if total_errors == 0:
        print("\n[PASS] All calculations match expected formulas!")
    else:
        print(f"\n[FAIL] {total_errors} calculation errors found")
        sys.exit(1)


if __name__ == "__main__":
    main()
