# Changelog

All notable changes to EVEMon will be documented in this file.

## [Unreleased]
- Fix CRLF line ending mismatch in release script README regexes
- Fix promotion pipeline: README validation, auto-release, CHANGELOG fallback, installer link and channel marker updates

## [5.1.3-alpha.1] - 2026-02-09
- Fix #14: Virtual-mode ListView crash when opening assets with 500+ items
- Fix #15: Font rendering quality — ClearType for overview, Segoe UI for footer/assets
- Fix #17: 60+ character tick cascade — 89% handler reduction, re-entrancy guard
- Add one-click crash reporting with 8-phase PII sanitizer and webhook pipeline
- Redesign crash dialog with prominent Submit Report button
- Add ESI key status indicators (No API Key, Connecting, Re-auth Required, Error)
- Simplify blank character creation (single-click instead of save-to-XML)
- Harden settings migration for imported/blank characters
- Guard UpdateManager against null TopicAddress/PatchAddress
- Fix promote.ps1 detached HEAD bug during merge to alpha/beta

## [5.1.2] - 2026-02-02
- First stable release with .NET 8 migration, auto-update, performance improvements
