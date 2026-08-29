# Acknowledgements

EveLens stands on other people's work. This file names it.

## Heritage

- **EVEMon** — created by **Jimi (Six Anari)** in 2006, maintained by the EVEMon
  Development Team for over a decade, and carried through 2021 by **Peter Han**
  (peterhaneve/evemon). EveLens is a direct descendant of that codebase and
  would not exist without it.

## The 3D render pipeline

The optional SKINR Studio renderer is built on:

- **Carbon Engine & Trinity** — CCP Games' game engine and renderer, released
  as open source under the MIT license (2026). Every ship EveLens renders is
  drawn by the engine EVE Online itself runs on.

- **CarbonEngineJS — `runtime-resource`** by **T'amber** (Caldari Prime Pony
  Club), MIT. The gr2 → cmf geometry bridge: most ship geometry on CCP's CDN is
  in a licensed middleware format the open engine release cannot read, and this
  package — the product of years of independent EVE rendering research — is what
  converts it into geometry Trinity understands. It ships in the render runtime
  with its LICENSE, NOTICE, and per-format notices intact, and it saved this
  project months. https://www.npmjs.com/package/@carbonenginejs/runtime-resource

- **Node.js** — hosts the geometry converter in an isolated, sandboxed
  process (MIT).

Full license texts for the runtime ship inside it: `THIRD-PARTY-LICENSES.md`
at the runtime root, plus per-package `LICENSE`/`NOTICE` files alongside the code.

## The application

EveLens itself (GPL v2) is built with **.NET** and **Avalonia UI**, renders
images with **SkiaSharp**, updates itself with **Velopack**, and is tested with
**xUnit**, **FluentAssertions**, and **NSubstitute**. UI infrastructure uses
**CommunityToolkit.Mvvm**.

## The community

Features and fixes throughout EveLens trace back to GitHub issues, translations,
and testing from EVE players — credited per release in [CHANGELOG.md](CHANGELOG.md)
and in the release notes.

EVE Online, and all related logos and assets, are the intellectual property of
CCP hf. EveLens is not affiliated with or endorsed by CCP Games.
