# Acknowledgements

EveLens stands on other people's work. This file names it.

## Heritage

- **EVEMon** — created by **Jimi (Six Anari)** in 2006, maintained by the EVEMon
  Development Team for over a decade, and carried through 2021 by **Peter Han**
  (peterhaneve/evemon). EveLens is a direct descendant of that codebase and
  would not exist without it.

## The application

EveLens itself (GPL v2) is built with:

- **.NET** -- runtime and base libraries (MIT)
- **Avalonia UI** -- cross-platform UI framework (MIT)
- **SkiaSharp** -- image rendering (MIT)
- **Velopack** -- installer and auto-updates (MIT)
- **.NET Community Toolkit** -- MVVM infrastructure (MIT)
- **DesktopNotifications** -- native desktop notifications (MIT)
- **MailKit** -- email notifications (MIT)
- **YamlDotNet** -- YAML parsing (MIT)
- **Google APIs Client Library for .NET** -- Google Calendar and Drive integration (Apache 2.0)

It is tested with **xUnit**, **FluentAssertions**, and **NSubstitute**.

## Earlier releases

EveLens 1.5.0 to 1.5.2 included an optional SKINR 3D renderer, since removed. It
was built on:

- **Carbon Engine & Trinity** -- CCP Games' game engine and renderer, open source
  under the MIT license.
- **CarbonEngineJS -- `runtime-resource`** by **T'amber** (Caldari Prime Pony
  Club), MIT -- the gr2 to cmf geometry converter.
  https://www.npmjs.com/package/@carbonenginejs/runtime-resource
- **Node.js** (MIT) -- hosted the geometry converter.

## The community

Features and fixes throughout EveLens trace back to GitHub issues, translations,
and testing from EVE players — credited per release in [CHANGELOG.md](CHANGELOG.md)
and in the release notes.

EVE Online, and all related logos and assets, are the intellectual property of
CCP hf. EveLens is not affiliated with or endorsed by CCP Games.
