# Changelog

All notable changes to EveLens will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed

- The About window's License page now lists the 3D render pipeline's third-party components (Carbon Trinity, CarbonEngineJS runtime-resource, Node.js) with their licenses -- previously these appeared only in the license files inside the render runtime bundle.

## [1.5.1] - 2026-08-28

### Added

- **macOS now ships a proper .dmg installer.** Drag EveLens to Applications the way every Mac app installs -- that Finder drag is also what tells macOS Gatekeeper the install is deliberate, which is essential for auto-updates to work (see the fix below). The .dmg is code-signed and notarized, and it is now the one macOS download -- the loose .zip archives are retired, because unzip-and-move installs are exactly what broke auto-updates (see below).

### Fixed

- **Solved: macOS updates that download but never install.** The root cause of the "version never changes" reports: when EveLens is placed in Applications without a Finder drag (Terminal move, unzip in place, or run straight from Downloads), macOS Gatekeeper silently runs a read-only mirror of the app -- App Translocation -- and the updater's final swap is refused every time, invisibly, after the app has already exited. EveLens now detects this state at startup and offers a one-click repair (moves itself properly into Applications, clears the quarantine flag, relaunches); the Download & Restart button also refuses honestly with an explanation instead of pretending to succeed. New installs avoid the trap entirely via the .dmg.

- An update that fails to install now says so, and why, instead of quietly leaving you on the old version. The updater's own log joins EveLens's local diagnostics on your machine (EveLens never sends anything anywhere), with usernames and identifiers scrubbed out just in case you ever choose to attach it to a bug report.

## [1.5.0] - 2026-08-28

**The SKINR update -- the biggest release in EveLens history**, packed with new features, engineering overhauls, and community-driven fixes. The headline: EveLens now renders your actual SKIN designs on your actual ships, in real 3D, using **CCP's own Carbon Engine and Trinity graphics engine** -- the same renderer EVE Online itself uses, which CCP open-sourced under MIT. A game engine, running inside a character tool. Around it, the four biggest changes EveLens has ever shipped at once:

- **Carbon & Trinity, now in EveLens** -- CCP's open-sourced game engine renders everything in the new SKINR Studio: your designs in 3D, Photo Op fleet portraits, and the entire Paragon Hub marketplace as real renders
- **macOS, first class** -- code-signed, notarized, and self-updating in place; no more Gatekeeper warnings, no more manual downloads
- **.NET 10 and Avalonia 12** -- the whole app moved to the newest runtime and UI framework, on all three platforms
- **Doctrine Designer, round two** -- a full fleet-readiness workbench, built with the community issue by issue

Everything below shipped and hardened across seventeen betas.

### The SKINR Studio

- **Your designs, in real 3D** -- rendered by Trinity, the engine New Eden runs on, so every nanocoating, every reflection, every running light is exactly what the game shows. The same hull, camera and lighting for every design: you judge the SKIN, not the screenshot. Five environments: CCP's own studio, a real station hangar, cycling nebulas, hard sunlight, and a beauty pass. (Supercapitals skip the hangar -- a titan doesn't fit in a station bay, in the game or here.)
- **The Paragon Hub, browsable** -- every design on EVE's SKIN marketplace as a real render, side by side. Ship, class, faction, creator and tier arrive instantly, and ready-made preview images fill the grid in seconds. One opt-in consent covers both.
- **Photo Op** -- assemble your own ships into a fleet formation around your primary and frame the shot: your Rifter escorted by your own battleships, every hull wearing its own SKIN. The screenshot you always wanted the game to take.
- **A one-time engine download** -- the 3D renderer is a separate add-on you install once; without it, everything still works through community preview images.
- **A first-run walkthrough** -- the studio introduces itself: what it is, the one ESI permission it needs, and the optional renderer.

### macOS

- **Signed and notarized** -- Gatekeeper opens EveLens with a double-click.
- **Self-updating** -- Check for Updates offers Download & Restart; the .app swaps in place wherever you keep it and relaunches on the new version. Linux gets background update checks too.

### Doctrine Designer

- **Import from anywhere** -- alliance .emp/.xml files, clipboard doctrine pings ("Skill Name V", roman or numeric), or any character's existing plan.
- **Whole groups at once** -- one click subscribes every member of an Overview group.
- **"Show only missing"** -- collapses the comparison to who still needs what, without the sea of green checkmarks.
- **A way out** -- every character card has a remove button now; doctrines used to be a one-way door.
- **Missing SP on every badge** -- the injector math, done: each character shows the skill points they're short of the whole doctrine.
- **An aligned comparison** -- each character's summary card IS its column header now; cards, names and checkmarks scroll as one system, with frozen panes both ways.

### The Plan Editor, rebuilt

- **A schedule, not a spreadsheet** -- plans group into attribute segments with real header rows, gold "REMAP BEFORE" dividers mark exactly where each scheduled remap lands, and the header leads with total training time and a live optimization badge that re-judges itself whenever you reorder skills.
- **Direct manipulation** -- click a level pip to retarget a skill, drag to reorder, select a skill for an inspector with prerequisites, what it unlocks, and a locate-in-plan jump.
- **The Attribute Optimizer got honest** -- remaps are placed globally against your whole remap budget (including "no remap at all" when one saves nothing), a proposed-schedule timeline explains WHY each remap lands where it does, and an active booster is called out instead of reading as bad values.
- **Skill priorities, made real** -- every plan row shows its priority, "Group by Priority" turns long plans into milestone bands, and prerequisites always stay ahead of the skills that need them.

### Also in this release

- **Overview, reworked** -- drag characters onto each other to form groups, sort and density controls, group totals at a glance, saved character comparisons, cards that scale with your font size.
- **Skill search looks inside descriptions** -- find skills by what they do, not just what they're called.
- **EVE static data build 3480926** (August 2026) -- all game data regenerated from CCP's Static Data Export.

### Fixed

- The Paragon Hub no longer freezes the app while it identifies designs; grids are virtualized and thumbnails slot in one card at a time.
- Supercapitals no longer clip through the Hangar environment -- titans and supercarriers simply don't offer it, like the game itself.
- Doctrine comparison ticks stay under their character to the last column, and scrollbars are visible at rest app-wide.
- Skill Planner keeps your hand-made order, roman-numeral skill lists import, and Change Priority actually responds.
- Dozens more community-reported fixes across the betas.

### Thank you

1.5.0 was shaped issue by issue with the people who use it: @odon (the Doctrine Designer's round two and the SKINR feedback that drove a dozen fixes), @Mordano (skill priorities and plan ordering), @jackmurray (who pushed for the .NET 10 move), @shawndibble (the Attribute Optimizer's remap-placement bug), @Dureiken (booster-aware optimization), @Kickunio and @Agge65 (the Overview's grouping and badge fixes), and everyone who filed, tested, and re-tested across seventeen betas. o7

## [1.5.0-beta.17] - 2026-08-28

### Fixed

- **Doctrine Designer: columns stopped drifting** -- the header row's character cells carried a few pixels of margin the body cells didn't, so with many characters the checkmarks slid further out from under their names with every column to the right. (odon)

### Changed

- **Doctrine Designer: the character cards ARE the columns now** -- the summary cards (portrait, time remaining, missing SP, Create Plan) sit directly on top of their own skill columns and scroll with them, instead of floating in a separate strip above the table. One aligned system: card, name, and every tick below share the same column edge by construction.

## [1.5.0-beta.16] - 2026-08-28

### Added

- **macOS: in-place auto-updates** -- the Mac app now updates itself the way the Windows app does: it checks in the background, and "Check for Updates" offers Download & Restart instead of a link to the download page. The update swaps the .app bundle in place (wherever you keep it -- Applications or Downloads both work) and relaunches. This build is the last one Mac users need to download by hand; from the next release onward the updater takes over.

## [1.5.0-beta.15] - 2026-08-28

### Fixed

- **macOS: the app really opens now** -- beta.14's launch fix was correct but never got to run: the signed app died even earlier, inside the .NET runtime bootstrap ("Failed to create CoreCLR"). Notarization requires Apple's hardened runtime, and the hardened runtime forbids the JIT memory .NET needs unless the app is signed with the allow-jit entitlement -- which our Windows-side signing tool silently dropped when signing the bundle. The signing now attaches the entitlements and refuses to produce a build without them. (beta.14's macOS download was replaced in place with a corrected build the same day.)

## [1.5.0-beta.14] - 2026-08-28

### Changed

- **Supercapitals no longer offer a Hangar environment at all** -- beta.13 greyed the Hangar pill out for titans and supercarriers with a "coming later" tooltip; the honest answer is simpler. No station bay is authored at that scale, so for supercapital hulls the pill is now gone entirely -- Studio, Space, Sunlight and Beauty remain -- and every other ship keeps the full set. If a super arrives while the hangar is showing, the stage still returns to Studio on its own.

### Fixed

- **macOS: the app opens again** -- beta.13's macOS build was our first single-file bundle (Apple's notarization rules effectively require it), and a single-file app reports no on-disk location for itself. EveLens read its own version from that location on startup, so it quit before the first window could appear. The version now comes from metadata compiled into the binary, which works in every publish shape on every platform. Windows and Linux were never affected.

## [1.5.0-beta.13] - 2026-08-28

### Added

- **Doctrine Designer: "Show only missing"** -- One toggle collapses the comparison to the characters with at least one missing skill and the skills missing on at least one of them -- the "who still needs what" view, without the sea of green checkmarks. (odon)
- **Doctrine Designer: missing SP on character badges** -- Characters who aren't fully trained now show how many skill points they're short of the whole doctrine, so sizing an injector or SP-bundle purchase doesn't need a calculator. (odon)

### Fixed

- **Supercapitals no longer clip through the Hangar environment** -- The bay the Hangar renders is a normal station interior, and just like in the game, a titan doesn't fit in one. The Hangar pill now declines supercapital hulls with an explanation (a Keepstar-scale bay is planned); if a super arrives while the hangar is showing, the stage returns to Studio.

- **The Paragon Hub no longer freezes EveLens while it works** -- Opening the Hub used to build a card for every one of ~3,400 market designs at once, then rebuild all of them every few hundred milliseconds while designs were being identified (one ESI answer per 150 ms, each one triggering a full rebuild) -- and since the whole app shares one UI thread, EveLens itself went unresponsive for minutes. The grid is now virtualized (only the cards on screen exist), status-line updates no longer touch the grid at all, and a freshly rendered thumbnail slots into its one card in place (#139).
- **The stage's loading card stays off the Paragon Hub** -- Opening the Hub while a collection was still loading painted "Preparing N ship parts..." over the marketplace, narrating a stage the user couldn't see. The overlay now belongs to the stage alone and comes back only if you return mid-load.

### Changed

- **The macOS app is now code-signed and notarized** -- Apple's Gatekeeper opens EveLens with a double-click instead of a "damaged or unidentified developer" warning. The build also became a single-file bundle (one executable plus native libraries), which is what makes a valid signature possible; datafiles moved to the bundle's Resources folder where macOS expects them.
- **The Hub identifies every design in one request** -- Instead of each install walking ESI recipes one-by-one to learn which ship each market design belongs to ("identifying 2,550 designs..." for minutes), the Hub now fetches the EveLens hub's pre-resolved catalog in a single request: names, hulls, classes, factions, creators and tiers arrive before the first paint. Covered by the same community-previews consent as the thumbnail shelf; declining it keeps the old client-side walk.
- **Community preview images load like a website now** -- Cards pull their ready-made thumbnails from the shelf a handful at a time in parallel, instead of waiting in the GPU renderer's one-at-a-time queue behind designs that actually need rendering. A full grid fills in seconds; the local renderer only works on designs the shelf doesn't have.

## [1.5.0-beta.12] - 2026-08-27

### Fixed

- **The Omega/Alpha badge on overview cards no longer clips at large font sizes** -- The badge's column was sized to the portrait's pixel width, which the badge text outgrows the moment the font scale passes 110%. The column now follows its content, so the badge fits at every scale and in every language (#72).

## [1.5.0-beta.11] - 2026-08-27

### Fixed

- **Doctrine Designer: scrolling the skill column no longer desyncs the rows** -- The frozen skill column quietly accepted the mouse wheel on its own, so scrolling with the cursor over skill names moved them out of step with the training times. All three panes of the comparison table now mirror each other in both directions, whichever one the wheel lands on (#137).
- **Scrollbars are actually visible now, app-wide** -- Two theme bugs stacked up: the custom scrollbar style forced every scrollbar to 10px *wide* (right for vertical bars, but it squashed horizontal ones into an invisible nub), and Avalonia's default auto-hide kept even correctly-sized bars invisible until the pointer hovered right over them -- so the theme's "visible at rest" intent never actually happened anywhere. Both fixed; wide views like the Doctrine Designer comparison now show their scrollbars at rest (#137).
- **SKINR's landing actually knows who owns designs now** -- The monitored SKINR license routes were registered but never fetched: the scheduler only runs non-core endpoints a character-monitor tab has enabled, and SKINR has no tab. The scope grant itself is now the opt-in (a new scope-activated endpoint class), so counts populate on the monitor's cadence, persist across restarts, and the landing lists every character with SKINR access up front -- no more empty page until you search (#139).
- **SKINR landing never lies about an unfetched collection** -- "no designs yet" only appears after a real answer from ESI; until then the card says it's still checking. Opening a collection also writes the live result back to the landing's data, so the two can no longer disagree (#139).
- **Picking a character from the SKINR top-right picker works from the landing** -- it used to load the collection behind the still-visible landing page, which read as nothing happening (#139).
- **The SKINR loader now covers every switch** -- design swaps after the first one, character switches, and environment changes all show the centered progress card. The old logic hid the loader on the first incoming frame, and after a swap the engine still delivers a few frames of the *previous* design, killing the loader instantly; it now waits for the first frame of the design you actually asked for (#139).
- **The Hub button on SKINR's left rail is clickable across its whole face on first open** -- its background brush was unset, which in Avalonia makes everything but the glyph text click-through until the first visit assigned a brush (#139).
- **Granting SKINR access keeps you where you were** -- Authenticating a character from the chooser used to yank you onto that character's (still empty) stage. You now stay on the chooser, the card moves up into "has access", and its design count arrives within seconds instead of waiting for the monitor's next SKINR pass (#139).
- **Opening a collection puts a ship on the stage** -- The studio used to open on an empty stage with a strip of tiles and no hint that one needed clicking. The first design now stages itself automatically (#139).
- **An empty collection says so plainly** -- A character who owns no designs got the search-filter message ("no designs match"), as though something were filtered out. It now names the real situation (#139).
- **The SKINR landing behaves on a brand-new install** -- With no characters yet, it used to offer a "find a character" search box over an empty page, announce that every character had granted access, and hide the only button that could change that. It now says there are no characters yet and keeps the way in visible; the search box appears only once the roster is large enough to need it (#139).
- **SKINR's status line no longer leaks filesystem paths** -- With no 3D engine installed, the renderer's internal search report (environment variable names, local paths) reached the status pill again. It goes to the diagnostic log, in full, and the interface shows one plain sentence.

### Added

- **SKINR shows your collection size** -- the design band now carries "N designs in collection" alongside the design name, so the answer to "how many do I own?" doesn't require counting tiles (#139).
- **A first-run walkthrough for the SKINR studio** -- Until any character has SKINR access, the landing becomes a three-step introduction: what the studio is (Collection, Paragon Hub, the stage), then granting the one extra ESI permission that lets EveLens see your designs, then the optional one-download 3D renderer. The left rail stays dimmed while the walkthrough runs and the Hub button pulses as it unlocks, so it's clear the studio just opened rather than something being disabled. It runs once -- after that the landing goes straight to the character chooser.
- **The SKINR chooser groups characters by access** -- Characters who granted SKINR access come first, and those who haven't follow under their own heading, so a large roster reads at a glance instead of mixing the two (#139).

## [1.5.0-beta.10] - 2026-08-27

### Added

- **SKINR knows your collections without being asked** -- Your SKINR design and component licenses are now first-class monitored data, refreshed on ESI's own cache schedule alongside skills, assets, and everything else -- and only for characters who granted SKINR access. Counts survive restarts, update live, and never cost a request more than ESI's cadence allows.
- **The SKINR studio opens with a landing, not a guess** -- First open asks whose collection to show: a card grid of exactly the characters who own designs (portrait, design count), one line for characters who haven't granted access yet, and a search that finds everyone. Your choice is remembered, so next time the studio goes straight to your ship -- and clicking Collection in the rail brings the chooser back (#139).
- **Loading is front and center** -- Opening a collection now shows a centered progress overlay instead of a bottom-corner whisper (#139).
- **The character picker shows who needs access** -- Characters without the SKINR permission appear dimmed with a "needs access" tag, and granting access lands you on that character instead of silently staying where you were (#139).
- **Space cycles, and now says so** -- The Space environment deals a different nebula on each click; the active pill now carries a cycle mark and a tooltip so the deck is discoverable (#139).
- **Doctrine Designer: import from clipboard** -- Paste a doctrine ping (one "Skill Name V" per line, roman or numeric levels) straight into a new doctrine (#137).
- **Doctrine Designer: the comparison table grew frozen panes** -- The skill column and character header row stay on screen while the grid scrolls both ways, and wide comparisons finally get a horizontal scrollbar (#137).
- **Skill priorities are now a real feature of the plan editor** -- Priority used to be settable but invisible: nothing displayed it and nothing ordered by it. Now every skill row shows its priority (a chip in its own column -- quiet at the default, tinted when you've changed it), and a new "Group by Priority" toolbar toggle orders the plan into priority bands with 1 training first, so long plans can carry milestones. Prerequisites always stay ahead of the skills that need them, and raising a skill's priority pulls its prerequisites up along with it (#135, and the milestone use case from #136).

### Fixed

- **"Amarr Titan V" now imports** -- Clipboard skill lists accept roman numeral levels everywhere, matching what the game itself copies; digits keep working (#137).
- **Changing a priority can no longer scramble your plan order** -- The priority-change machinery inherited a convention from the old editor that rebuilt the plan from the currently sorted view, which would have silently made a temporary sort permanent. It now always preserves your manual order.

### Changed

- **Updated to EVE static data build 3480926** (released 2026-08-26) -- all game data regenerated from CCP's latest Static Data Export.

## [1.5.0-beta.9] - 2026-08-26

### Added

- **Doctrine Designer: import a doctrine straight from a file** -- Alliance skill plans shared as .emp or plan .xml files no longer have to be routed through a character's plan first; the new "Import from File" button in the sidebar reads them directly into a doctrine (#137).
- **Doctrine Designer: add characters by group** -- The Add Character dialog now lists your Overview groups first; one click subscribes every member of a group, so checking all your mains against a doctrine is a single action (#137).
- **Doctrine Designer: remove a character from a doctrine** -- Each character card in the comparison now has a remove button. There was previously no way out at all once a character was added (#137).

### Fixed

- **Skill Planner keeps your skill order** -- Adding a skill to a hand-ordered plan rearranged the whole plan: the prerequisite-order pass was grouping skills by training attribute on every change, whether or not any sort was active. Manual order is now only touched when a prerequisite genuinely forces it, and attribute grouping happens only when Group by Attr is on (#136).
- **Skill Planner: Change Priority now works** -- Right-click > Change Priority silently cycled the priority with no visible response, which read as doing nothing. It is now a proper submenu showing priorities 1-5 with the current one marked (#135).
- **Doctrine Designer shows full character names** -- The comparison table's column headers truncated names to the first word, which made same-firstname alts indistinguishable. Full names now, trimmed with a tooltip when the column is tight (#137).

## [1.5.0-beta.8] - 2026-08-26

### Fixed

- **Sort and reorder controls appear when the second character arrives** -- The overview's sort, density, and reorder controls only exist with 2+ characters, but adding the second character took the incremental card-update path that never re-evaluates the toolbar -- so the controls stayed hidden until an app restart. Crossing the threshold (either direction) now rebuilds the header.

## [1.5.0-beta.7] - 2026-08-26

### Fixed

- **Status strip no longer prints your file paths** -- When the 3D engine is missing, the SKINR status line showed the renderer's full internal search report: environment variable names and local filesystem paths. That report belongs in the diagnostic log (where it still goes, complete); the interface now says one plain sentence.

## [1.5.0-beta.6] - 2026-08-26

### Fixed

- **SKINR marketplace no longer hangs the app** -- Every refresh of the marketplace grid re-decoded every card's image from scratch on the UI thread (and re-converted every hull render -- five designs on one hull paid five times). On a Mac this pinned the interface for 14 seconds at a 4 GB memory footprint and had to be force-closed. Card art is now decoded once at card size and reused across rebuilds, hull renders are fetched once per ship, and memory stays flat.
- **Mac: the SKINR window is usable without the renderer** -- On Apple Silicon the marketplace's background thumbnail engine tried to render every design through a 3D engine that is not installed yet: hundreds of doomed attempts that kept the window busy and every card stuck on "preparing". Without a local engine it now fills cards only from opted-in community previews and parks the rest quietly -- and the moment the Metal runtime installs, those designs get their real renders.
- **macOS and Linux now get update checks** -- The auto-updater only worked on Windows (the mac .app and Linux archives are hand-packaged, so the update engine considered itself not installed and went silent). Both platforms now check GitHub Releases in the background on the same schedule as Windows and notify when a newer build for your channel exists; Help > Check for Updates opens the download page. Version comparison is numeric now too -- the old manual check ordered beta.10 before beta.4 and misread the channel, so it could claim you were current when you were not.

## [1.5.0-beta.4] - 2026-08-26

### Fixed

- **Mac: SKINR no longer dangles an Install button that cannot work** -- On Apple Silicon, clicking a design could surface the runtime install offer, whose Install click then reported "service unreachable - try again" forever: the Metal runtime is not published yet, and one code path missed that check. The availability decision now lives in one place, so every path shows the honest "Metal renderer coming" message until the day the Mac runtime actually ships.

## [1.5.0-beta.3] - 2026-08-26

### Fixed

- **3D geometry now converts on installed builds** -- Beta.2's SKINR studio could fail with "Could not prepare this hull's geometry": the gr2-to-cmf converter and the Node runtime it runs on existed only on development machines. Render Runtime 1.0.3 now carries both (converter bundle + a checksum-pinned Node), verified by the signed manifest like everything else in the package, and EveLens looks for them exactly there. Update the runtime when prompted.
- **The 3D renderer now updates in place** -- EveLens only ever offered the runtime download when none was installed; a newer announced version was invisible to existing installs. The SKINR window now offers the update on open -- same consent panel, same verification chain -- and a running renderer restarts onto the new version without closing the window.

## [1.5.0-beta.2] - 2026-08-26

### The SKINR Studio

The biggest feature EveLens has ever shipped: a full 3D ship studio built on CCP's own open-source Trinity engine, rendering your SKINR designs -- and the entire Paragon Hub marketplace -- with the game's real models, materials, and lighting.

- **Your designs, in real 3D** -- Open the SKINR window and your ship fills it: the actual hull, wearing the actual nanocoating, with the design's name, tier, and hull identity overlaid. A design carousel along the bottom shows real rendered thumbnails of your collection (captured as you browse, instant forever after), and live search filters designs by name or hull. Drag to rotate -- flick the ship and it keeps spinning, gliding to a stop like it has mass; grab it anytime to stop it. An environment switcher offers five looks -- Studio, Hangar, Space, Sunlight, and Beauty -- applied without rebuilding the ship. The studio wears its own violet identity as EveLens's creative-marketplace space.
- **Lit like the game lights them** -- Hulls read from every angle, gold coatings keep their warmth at any zoom, and the Triglavian orb casts its red glow onto the surrounding plating -- every light driven by its own authored position and colour, on every hull that carries one.
- **A real station hangar** -- The Hangar environment parks your ship inside the actual Jita 4-4 Caldari Navy docking bay: the holo-lit landing pad, the traffic decks, the bay's own authored lighting. Every hull floats at a pad clearance scaled to its size, freighters included, and the camera skims the deck but never orbits below it. Depth fog fades distant decks into a steel-grey haze, so the interior reads with real scale. The Space environment cycles through eight real region nebulae -- click it again for the next sky, each lit by that region's own sun. A design details panel (hull dimensions from the game data, design attributes, local favorites, copy-to-clipboard sharing) rounds out the studio.
- **Browse the Paragon Hub marketplace** -- The Hub tab is a live marketplace browser: the public Paragon Hub feed, one card per design, with the design's name, creator, cheapest PLEX ask, and how many listings are buyable right now. Search by name or creator, filter to a specific ship, and click any card to see that design rendered in full 3D on its hull -- owned or not, the public recipe renders exactly the same. The browse is ship-first: open the Hub while viewing a design and it starts on "Find a design for your <ship>"; a market tree (ship class > faction > ship, with design counts) drives the browse like EVE's own market window. A background renderer quietly fills every card with a real skinned render of that exact design -- same camera, same lighting, so designs are judged side by side rather than through mismatched screenshots. Until a design's render is ready its card shows the plain base hull, dimmed and labelled, so a stock photo can never be mistaken for the skin. Design details are cached on disk, so the ship filter fills instantly from the second browse on. Price history, recommendations and value labels are the next phases.
- **Photo Op: assemble your fleet** -- Pick up to ten of your other designs as wingmen and a formation to fly them in -- Vic, Line Abreast, Echelon, Column, or Wall -- with spacing computed from each hull's real measured size, so a shuttle and a battleship both sit right. Switching formations re-forms the fleet instantly, and every ship is freely placeable in 3D: Ctrl+drag slides it in the plane you're looking at, Ctrl+scroll pushes it deeper, orbit the camera and repeat. The camera pulls back to frame the formation without giving up close zoom on a single hull. Your ships, your skins, one screenshot.
- **Fast to come back to** -- The renderer keeps the last few built ships parked off-stage, so revisiting a recently viewed design brings the finished ship back instantly instead of rebuilding it from scratch.
- **The 3D engine is a separate one-time download** -- 3D rendering uses the EveLens Rendering Runtime, offered on first SKINR use. The consent panel shows exactly what it is before you agree -- name, version, size, download host, and full SHA-256 -- and every package is verified against a signing key built into EveLens (signature, then every file's hash) before anything runs. Every executable and native module in the package also carries its own Windows code signature: two locks, different doors. The runtime is required only for 3D; all of EveLens works without it. The SKINR studio carries CCP's trademark notice alongside the art it renders.
- **Everything else works without a character** -- The character picker appears only in Collection, where "whose ships" is the question; the Paragon Hub marketplace shows the same designs to everyone.
- **macOS is on the runway** -- EveLens itself ships for Apple Silicon today, and the 3D renderer's Metal build is in active CI. The switch is server-side: the day the Mac runtime publishes, this very release starts offering the install -- no app update required. Until then the SKINR window says so honestly, and everything else in it already works.

### Added

- **Saved character comparisons** -- The Skill Comparison window can now save a named set of characters and reload it in one click, so your routine "compare my 5 industry alts" check no longer means hand-picking them every time. The add-character list is also alphabetical now instead of order-added (Discussion #105, thanks AnszaKalltiern).
- **Group directly on the overview -- drag a card onto a card** -- Creating and managing character groups no longer needs a dialog: drop one card on another to form a group, drop a card on a group to add it, drag a card out of its group to remove it, and everything glides into place with smooth animations. Double-click a group name to rename it; right-click for rename, reorder, and delete. A one-time tip (and tooltips on every group header) teach the gestures. The Manage Groups window is retired -- one dialog fewer.
- **Sort and density controls on the overview** -- A sort dropdown orders characters within each group by name, skill points, or "needs attention" (paused queues first, then whoever finishes soonest), alongside your own drag-defined order. A Compact density mode trims the cards to fit ~40% more characters per screen -- built for multi-account fleets (Discussion #46, Issue #72).
- **Group totals at a glance** -- Every group header now shows combined skill points, ISK, and how many members are actively training (privacy mode masks the numbers, as everywhere).
- **The Plan Editor, rebuilt from the ground up** -- The training queue now reads like a schedule instead of a spreadsheet: plans group into attribute segments with real header rows (dominant attribute pair, skill count, duration, attribute chips), gold "REMAP BEFORE" dividers mark exactly where each scheduled remap lands, and a teal header shows the attributes you are actually flying with today. Skills gain direct manipulation -- click a level pip to retarget a skill up or down, hover for a drag grip, select a skill for a details inspector with prerequisites, what it unlocks, and a Locate-in-plan jump. The header leads with what matters: total training time in large type, and a live optimization badge that says either "Optimized for <clone> clone" or how much a remap pass could still save -- judged against the clone you optimized for, and re-judged automatically whenever you reorder skills (a remap divider that no longer matches its block turns amber and says so). The optimizer's suggestions and your manual order stay honest with each other.
- **A consistent icon language** -- Every glyph in the app now comes from a single professional icon font (VS Code's Codicons) instead of a mixture of emoji and unicode symbols -- one visual voice across buttons, badges, and status surfaces.

### Changed

- **SDE updated to build 3470007 (August 2026)** -- All game data regenerated from CCP's latest Static Data Export: 52,863 types including the new SKINR component data. The EVE Accuracy Suite verified all training math is unchanged.
- **Grouped overview now uses the whole screen** -- Small character groups share a row (each section exactly as wide as its cards) while large groups keep a full-width row with wrapping cards, and the layout re-flows as you resize the window. A dozen per-account groups now fill a widescreen monitor instead of scrolling off the bottom with half the display empty (Issue #72, thanks Agge65).
- **Character cards grow with your font size** -- Card dimensions now scale with the Appearance font setting, so text no longer clips at 110%+ on high-DPI monitors (Issue #72).
- **Comparison header stays frozen while scrolling** -- The character-name header row in Skill Comparison now locks to the top like a spreadsheet freeze pane, so you can always see whose column is whose deep in a long skill list (Discussion #93).
- **Skill search now looks inside descriptions** -- Searching "powergrid" in the skill browser used to find nothing useful because the skill is named "Power Grid Management" -- with a space. The text filter (plan editor and Skills tab alike) now matches skill descriptions too, so functional searches like "capacitor", "velocity", or "powergrid" surface every skill that affects them (Discussion #116, thanks OS17279).

### Fixed

- **The Attribute Optimizer places remaps globally** -- Fixed a segmentation bug where attribute boundaries compared only primary attributes: plans transitioning from Memory/Perception to Memory/Intelligence were treated as one continuous Memory block, which incorrectly placed the remap on the first skill (Issue #122, thanks shawndibble). Boundaries now use the ordered primary/secondary pair, and remap placement is selected globally against the available remap budget, weighted by remaining training time -- including the options of a mid-plan first remap (train the prefix on current attributes) and of recommending no remap at all when one saves nothing meaningful. More available remaps can never produce a slower plan. And when an attribute booster is active, the optimizer says so -- live attributes beat any legal remap until it expires, which used to read as "bad values" (thanks Dureiken).
- **The Optimizer window tells the story** -- redesigned around a proposed-schedule timeline: a "Keep current attributes" card when the plan starts on your current spread, one card per remap ("Remap before X") with attribute chips and the dominant skill focus, a details inspector explaining WHY each remap lands where it does, a proportional time bar, and a headline with time saved, percent faster and the projected finish date.
- **Skill Farm counts Omega per account, not per character** -- The monthly economics now model reality: one Omega (500 PLEX) per account plus an MCT certificate (485 PLEX) per extra training character. Set how many characters share an account (default 3), or assign explicit account labels per character in the new Acct column -- characters sharing a label share one Omega (Issue #124, thanks IlliumIv).
- **The Safe-for-Work setting is gone** -- it never did anything in the new UI, and a toggle that visibly changes nothing erodes trust in every toggle around it (Issue #123, thanks IlliumIv).
- **External Calendar settings are hidden** -- cloud calendar authentication is currently disabled in EveLens, so offering a full configuration pane with no way to sign in was a trap. It returns when cloud services do (Issue #125, thanks rodrigoleme).

## [1.5.0-beta.1] - 2026-08-19

### Changed

- **EveLens now runs on .NET 10** -- The whole app moved from .NET 8 (support ends November 2026) to .NET 10 LTS (supported through 2028), along with its dependency stack: Avalonia 12.1, SkiaSharp 3, and current Microsoft libraries. Releases stay self-contained, so nothing to install -- updates arrive like any other. Under the hood the migration came with a behavioral audit that hardened skill-point estimation, PI cycle math, date parsing under non-English locales, and settings-load diagnostics against runtime edge cases. The solution now builds with zero known dependency vulnerabilities. Thanks to jackmurray for the nudge and the first migration PR (#106).
- **Avalonia 12 + compiled bindings** -- The UI framework jumped to Avalonia 12.1 (with SkiaSharp 3), bringing its reworked compositor and rendering pipeline, lower idle CPU, and -- on Linux -- the first native .NET screen-reader/accessibility support (AT-SPI2). Every data binding in EveLens is now compiled and type-checked at build time, so an entire class of "this column just stopped showing data" bugs can no longer ship.
- **Updater engine upgraded (Velopack 1.2)** -- The auto-update machinery moved from a years-old prerelease to the current stable line, picking up a long tail of updater fixes (update-locator and macOS update handling among them).
- **Leaner, more portable internals** -- Two legacy dependencies are gone: SharpZipLib (replaced by the runtime's built-in compression, with byte-format compatibility tests proving old cloud backups stay readable) and System.Drawing/GDI+ (whose Windows-only image code crashed Linux/macOS whenever it snuck in -- reintroducing it is now a compile error, not a runtime crash report).

### Fixed

- **Deleting a character group no longer crashes the Manage Groups window** -- The delete button sits inside the group chip, so its click also triggered the chip's expand/collapse toggle, which re-selected the group that had just been removed and crashed the reorder panel. The click now stops at the delete button, and the reorder panel tolerates a vanished group either way. Thanks to jpn-1 for the exact diagnosis (Issue #78).
- **Skill point estimates can no longer overflow with stale queue data** -- A skill queue entry whose end time had gone stale (e.g. the app waking from sleep before the next ESI refresh) combined with a skill missing from the datafiles could push the estimated-SP arithmetic past what an integer holds. Estimates are now clamped to the entry's own start/end SP window, and the current PI extraction cycle is clamped to the program's real cycle range the same way. Groundwork for the .NET 10 runtime, where the old overflow behavior would have silently corrupted stored skill points instead of being masked.

## [1.4.0-beta.4] - 2026-06-04

### Added

- **Korean language support (한국어)** -- Full UI translation (464 strings) plus 50,000+ CCP official SDE translations for skills, ships, items, and blueprints. Select "한국어 (Korean)" in Settings > Appearance. Community translation contributed by a Korean EVE player. (Discussion #79)
- **Skill Farm: configurable SP base per character** — Set a custom SP floor for each farm character (click the "Base" column). Characters with PI, mining, or other utility skills won't count that SP as extractable. Defaults to 5M (CCP minimum), saves per-character and persists across sessions.
- **"What's New" dialog on update** — Shows release notes the first time you open EveLens after installing a new version. Grouped by category (Added, Changed, Fixed) with color coding. Only shows once per version.
- **Doctrine Designer** — Create shared skill templates, assign multiple characters, compare training times side-by-side. Import from existing plans, generate personal plans for each character with one click. (Tools → Doctrine Designer, Ctrl+G)
- **Chinese language support (简体中文)** — Full UI translation with 300+ localized strings, 50,000+ CCP official SDE translations for skills, ships, items, and blueprints. Language picker in Settings → Appearance. Auto-restart on language change.
- **CSV export** — Export skills and training queue to CSV files from the Skills and Queue tabs
- **Skill Farm Dashboard: sort by column** — Click any column header to sort ascending/descending. "Add All Eligible" button to batch-add characters with 5.5M+ SP.
- **Plan editor: attribute group headers** — "Group by Attr" now shows color-coded section headers with skill count and training time per attribute group
- **Plan editor: specific prereq error messages** — Blocked drag now shows "Cruiser IV needs Cruiser III first" instead of generic error
- **Plan editor: double-click hint tooltip** — Rows show "Drag to reorder · Double-click for details" on hover
- **Plan editor: "Hide Maxed" skill filter** -- New filter button in the skill browser hides skills you've already trained to Level V, so you only see what's left to train (#71)
- **ESI timer tooltips** — Hover the status bar countdown for an explanation of what it means
- **Custom browser setting** — Choose which browser opens for ESI authentication (auto-detect or specific browser)
- **SDE updated to build 3328718** — 51,551 types (+1,378 new), 2,697 groups (+95 new) with full Chinese translations

### Changed

- **Plan editor: whole-row drag** — Entire row is now draggable with a 5px movement threshold (grip dots column removed). Click-to-select and drag-to-reorder coexist naturally.
- **Release asset naming** — macOS and Linux release assets now use channel-based names (e.g. `EveLens-stable-linux-x86_64.AppImage`) so download links never go stale between versions
- **EveLens branded icons** — All platform icons (Windows, macOS, Linux) replaced with proper EveLens logo at all resolutions

### Fixed

- **Plan editor: Delete key removed the wrong skill** -- Pressing Delete deleted the top skill in the queue (and its dependents) instead of the skill you had selected. Delete now acts on your actual selection, and supports multi-select (#80)
- **Planetary Interaction: idle colonies stopped showing red** -- Colony health was frozen at the moment data first loaded, so a colony that went idle while EveLens was open never turned red until restart. Extractor state is now computed live and the dashboard repaints when colony data refreshes or an extractor finishes (#66)
- **Planetary Interaction: final product showed "Unknown"** -- Actively-extracting colonies route material onward immediately, leaving the extractor's contents empty, so EveLens couldn't name the product. It now reads the extractor's declared output type, resolving the correct product (#66)
- **Planetary Interaction: stray horizontal scrollbar and right-side gap** -- The colony detail view showed an unwanted horizontal scrollbar with empty space to the right of the production chain until you resized the window. The layout now fills the available width correctly (#66)
- **Website download links 404** — macOS and Linux download links on evelens.dev now point to stable channel-named assets that persist across releases (#64)
- **Plan editor drag: scroll offset bug** — Dragging while scrolled down no longer maps to wrong row positions (#59)
- **Plan editor: Alt+Up/Down keyboard shortcuts** — Now wired to actual queue selection instead of first/last item (#59)
- **Attribute optimizer: "Reset to Current" showed 3 everywhere** — Now uses character's actual ESI attributes instead of default scratchpad values (#60)
- **Attribute optimizer: inconsistent training times** — Manual point adjustments now compute duration directly, avoiding StartTime/BestScratchpad mismatch (#60)
- **Group by attribute: button did nothing visible** — Now injects color-coded attribute group headers and shows active state on button (#61)
- **Windows taskbar icon reverted to default** — Explicitly set after InitializeComponent to survive theme loading (#58)
- **macOS: Cmd+W didn't close plan windows** — Added Meta modifier check alongside Control (#59)
- **macOS: menu title showed "Avalonia Application"** — Set Application.Name="EveLens" in App.axaml
- **macOS: Unicode character names broken** — Added cross-platform font fallback chain (Segoe UI, Helvetica Neue, Noto Sans, DejaVu Sans)
- **macOS: dock icon showed generic app icon** — Proper hi-res .icns now embedded in .app bundle
- **Linux: AppImage had 1px placeholder icon** — Now uses real 256px EveLens icon
- **Doctrine Designer crash without characters** — No longer crashes when opened before any characters are loaded

## [1.2.1] - 2026-04-09

### Fixed

- **Plan training time was significantly underestimated** -- cerebral accelerator (booster) bonuses were being applied permanently to all skills in a plan, regardless of booster expiry. A 225-day plan would show as ~163 days. EveLens now calculates training time using base attributes + implants only, matching the EVE client exactly.

### Changed

- Removed all cerebral accelerator infrastructure from training calculations. Booster support will be redesigned in a future release with explicit user controls rather than unreliable auto-detection.
- Added `MaxEffectiveAttributePoints` constant (32) with regression tests enforcing the attribute cap: no attribute used in training calculations can exceed base (27) + implant (5).

## [1.2.0] - 2026-04-05
- 1.2.0: Plan Editor drag-reorder, Skill Farm Dashboard, plan import fix, keyboard shortcuts, queue health cards
## [1.2.0] - 2026-04-05

### Added

- **Drag-to-reorder in Plan Editor** -- grab the grip handle to reorder skills in your training plan. Multi-select with shift/ctrl+click, drag as a group. Prerequisite constraints enforced in real-time (blue indicator = valid, red = blocked). Toast notifications on success/failure. Ghost placeholder shows original position during drag. Press animation with scale transition for tactile feedback
- **Chain ribbons** -- colored left-edge strips visually group related skills by training goal. Colors are stable (deterministic from goal skill ID). Chain position drives ribbon corner radius (first/mid/last/solo)
- **Timeline minimap** -- proportional colored bar at the top of the plan showing time distribution by chain. Legend chips show chain names
- **Goal inference engine** -- automatically detects training goals (leaf skills with no in-plan dependents) and assigns prerequisite chains. Shared prerequisites resolved by first-claimer rule
- **Skill Farm Dashboard** -- full economics dashboard for skill extraction characters. ESI Jita pricing for PLEX and extractors, per-character tax from Accounting skill, extraction readiness tracking, monthly profit projections, and Omega sustainability analysis. Privacy mode support hides character names for streaming/screenshots
- **Recent Plans menu** -- the Plans menu now shows the 5 most recently opened plans per character with training time, for quick access without going through Manage Plans
- **Skill detail sidebar** -- double-click any skill in the Plan Editor to see description, unlocked skills, enabled items (with icons), and plan-to actions in the right panel. Click unlocked skills to drill into the prerequisite tree
- **Skill browser filters** -- four filter modes in the Plan Editor's Skills tab: All Skills, Trained, Have Prerequisites, and Untrained
- **Keyboard shortcuts** -- Ctrl+Q (quit), Ctrl+W (close plan window), Ctrl+Shift+W (close all child windows), Ctrl+N (new plan), Ctrl+M (manage plans), Ctrl+, (settings). All shown in menus and Help > Keyboard Shortcuts dialog with OS-specific labels
- **Reverse skill/item lookups** -- new StaticSkills.GetDependentSkills() and GetItemsRequiringSkill() for browsing what a skill unlocks
- **Plan activity tracking** -- plans now track when they were last opened via LastActivity timestamp, persisted across sessions
- **Queue health on overview cards** -- theme-aware card tints across all 6 palettes show queue status at a glance. Status dots with labels: green (>5 days), yellow (<5 days), red (<24 hours), dark red (empty), gray (paused). Click a status dot to navigate to that character
- **Plan import/export overhaul** -- Import Fit now handles .emp plan files, .txt plan exports, and EVE game clipboard format ("Skill Name 3"). Clipboard copy outputs game-compatible format for direct paste into EVE skill queue
- **Plan import regression tests** -- 19 tests covering XML round-trip, .emp format detection (plain XML + gzip), BOM handling, edge cases, and revision parsing ([#51])

### Changed

- **macOS install instructions** -- simplified to xattr-only method since right-click Open and Privacy Settings don't work with unsigned apps (Gatekeeper reports "broken" not "unsigned")
- **Skill browser attribute filter** defaults to "All Attributes" instead of auto-selecting the detected remap
- **Plan Editor sidebar** widened to 320px for better content layout
- **Gmail-style mail view** -- split view with mail list on left, reading pane on right. Click a mail to read inline instead of opening a separate window. Body auto-loads when ESI finishes downloading. Flat list sorted newest first
- **Employment history list view** -- toggle between horizontal Timeline and vertical List view. Card-row design with corp logos, date range, duration badge, and "Current" indicator. View preference saved across sessions
- **Skill level breakdown tooltip** -- hover the stats line in the character header to see skill counts at each level (V:3 IV:5 III:8 etc.)

### Fixed

- **Plan import was creating empty plans ([#51])** -- the file was read for its name but entries were never imported. Fixed across all import paths (Plans menu, Manage Plans, Plan Editor). Reported by [@TinkeringGoblin](https://github.com/TinkeringGoblin)
- **Plan import gzip error** -- .emp files exported by EveLens are plain XML, but import assumed gzip. Now auto-detects format
- **Skill browser collapsed after Plan To ([#52])** -- adding a skill from the browser no longer resets expand/collapse state of categories. Reported by [@NotmoGit](https://github.com/NotmoGit)
- **Windows shutdown hang ([#53])** -- settings save now runs with a 3-second timeout. If disk I/O is slow, the app exits cleanly instead of blocking Windows shutdown. Reported by [@Kickunio](https://github.com/Kickunio)
- **Market transaction item names** -- item names were blank because the ESI>model layer never resolved TypeID to TypeName (Phoenix refactoring regression). Now falls back to StaticItems lookup
- **Wallet journal "Undefined"** -- new ESI ref types not in the 2018 RefTypes.xml mapping showed as "Undefined". Now preserves the raw ESI string and humanizes it (e.g. "player_trading" > "Player Trading")
- **Unicode ship names** -- ship names with non-ASCII characters (e.g. ♪ ♥ ♪) were displayed as literal \uNNNN escape sequences instead of rendered glyphs. All JSON serialization paths now preserve unicode as-is
- **App hangs on quit with child windows open** -- closing the app while a Plan Editor or other child window was open caused the process to hang and become a zombie (macOS). Child windows are now tracked and closed before shutdown
- **Plan window blocks main window** -- child windows no longer force themselves above the main window on macOS. All windows are independent and freely switchable via Alt+Tab / Cmd+`
- **New Plan dialog keyboard focus ([#50])** -- TextBox now receives focus immediately on open. Typing replaces the default "Plan N" name without needing to click first. Reported by [@AnszaKalltiern](https://github.com/AnszaKalltiern)

### Removed

- **Queue health flyout** -- the clock icon and flyout in the status bar have been replaced by the overview card tints and status dots, which are more scalable and visible

## [1.1.0] - 2026-03-29

### Added

- **Character Skill Comparison** -- compare up to 10 characters side-by-side with theme-aware level blocks, differences-only toggle, and auto-sizing columns ([#45])
- **Variable font scaling** -- a Font Size slider in Settings > Appearance scales all text from 80% to 150%. Every font in the app (895 values across 71 files) now uses a 7-tier type scale derived from a single base size. Changes apply live as you drag the slider and persist across sessions. Architecture tests prevent hardcoded font sizes from creeping back in
- **Untrained filter** -- new filter button in the Skills tab shows skills not yet injected ([#33])
- **Queue health monitor** -- a clock icon in the status bar shows how many character queues need attention. Click it to see all characters sorted by urgency with countdown timers and end dates. Click any character to jump straight to their Queue tab ([#43])
- **Queue end date** in the Queue tab -- the status bar now shows when the queue finishes and a countdown timer so you know exactly when to refresh your training plan
- **Add Character card** -- a ghost card in the character overview lets you add new characters without navigating menus. The portrait strip also has a `+` button for quick access ([#41])
- **Add Another flow** -- after adding a character via SSO, you can immediately add another without reopening the dialog. Characters are auto-imported on successful login, no extra confirmation step needed
- **Group and character reorder** -- click a group chip to expand a member reorder panel with ▲ ▼ buttons. ◀ ▶ moves groups left/right to change their display order in the Overview and portrait strip ([#42])
- **Group dividers in portrait strip** -- visible separator lines between groups for clearer visual separation ([#42])
- **Help text in Manage Groups** -- guidance text explains how to assign characters, reorder members, and manage groups ([#42])

### Changed

- **Manage Character Groups** completely redesigned -- tag-based UI shows each character with colored group tags. Click `+ Assign` to pick a group from a radio-button flyout. Groups are managed inline with rename and delete icons ([#42])
- **Group colors in portrait strip** -- characters are ordered by group with colored accent bars under their portraits ([#42])
- **Blueprint browser** uses the same hierarchical tree as Ships and Items -- no more duplicate "Amarr" entries. The full market group path is preserved with a "Can Build Only" filter ([#39])
- **Consistent skill counts** -- unpublished skills are now filtered uniformly across the Skills tab, Plan Editor, and Character Comparison ([#37], [#33])

### Fixed

- **Queue Health now shows all characters** -- previously only "monitored" characters (a legacy EVEMon concept with no UI toggle) appeared in the Queue Health flyout and badge. Characters migrated from old EVEMon settings could become invisible ghosts. All characters are now guaranteed to be monitored on import ([#47])
- **Queue Health flyout scrolls** -- added ScrollViewer with max height to prevent the flyout from overflowing off-screen with many characters
- **Full character names in Comparison** -- column headers and portraits now show full names instead of first name only ([#45])
- **Live font scaling** -- code-behind windows (Manage Groups, Comparison, Skills, Overview, dialogs) now rebuild on font scale change instead of showing stale sizes ([#42])
- **ESI token race condition** -- requests no longer fire with expired tokens. Tokens refresh proactively 100 seconds before expiry, and a pre-flight check blocks any request when the token is expired or refreshing. This prevents the error budget depletion that caused the scheduler to back off for 20+ hours with 30+ characters ([#34])
- **401 vs 403 distinction** -- expired tokens (401) are now treated as transient and don't trigger "re-authentication required" notifications. Only permanent auth failures like revoked scopes (403) trigger that message. The scheduler re-enqueues 401s after 15 seconds instead of suspending all jobs ([#34])
- **Startup token refresh** -- all ESI tokens are refreshed during the splash screen before the scheduler starts dispatching, preventing the burst of 401s that occurred on app launch ([#34])
- **TextBox auto-focus** -- dialog text inputs now auto-focus on open across all dialogs: Create Blank Character, Manage Groups, Manage Plans, Implant Sets, and Skill Constellation search ([#42])
- **macOS .app bundle** -- the app was not recognised by macOS because executable permissions were lost during packaging. Now built via WSL with proper Unix permissions

### Removed

- **Google Analytics tracker** -- removed dead code that hashed MAC addresses for fingerprinting. Never had callers, never had consent ([#40])
- **In-game browser server** -- removed legacy IGB HTTP server (5 files) that could bind port 80. CCP retired the IGB years ago ([#40])

## [1.0.0] - 2026-03-23

### Added

- **Auto-updates** via Velopack with delta downloads across Windows, Linux, and macOS
- **Windows code signing** -- eliminates SmartScreen warnings and false-positive antivirus detections
- "Check for Updates" in the Help menu with release notes in the update dialog

### Changed

- Update system completely replaced -- Velopack handles all packaging and delivery
- Build and release pipeline moved to GitHub Actions

## [1.0.0-beta.2] - 2026-03-19

### Added

- **ESI health tracking** -- smart per-endpoint health states replace noisy error notifications. You'll see one clear message when something breaks, and a recovery message when it's fixed -- no more walls of error spam ([#34])
- **Health indicators** on the character overview -- green (healthy), yellow (degraded), red (failing)
- **Live diagnostic viewer** in the Debug menu -- real-time log with filters for ESI, events, warnings, and scheduler activity
- **SDE update to Catalyst expansion** (March 18, 2026) -- 5 new skills, 82 new item types, carrier/fighter/FAX/Black Ops balance changes

### Fixed

- ~100 ESI error entries flooding the activity log during brief connectivity issues ([#34])
- "19 hours until next refresh" showing stale times when error cache expired
- Debug builds now use a separate data folder to avoid contaminating production settings

## [1.0.0-beta.1] - 2026-03-16

### Added

- Window position and size remembered across restarts, including multi-monitor setups

## [1.0.0-alpha.1] - 2026-02-25

### Added

- **Cross-platform support** -- Windows x64, Linux x64, macOS Apple Silicon
- **Modern dark UI** built on Avalonia, replacing the legacy WinForms interface
- **Smart ESI scheduler** with priority queue, per-character rate limiting, and phased cold start
- **19 character tabs** -- Skills, Assets, Market Orders, Contracts, Mail, Industry Jobs, Wallet, Notifications, Kill Log, Planetary, and more
- **Plan Editor** with skill browser, training time calculator, and attribute optimizer
- **Settings migration** -- existing EVEMon settings imported automatically on first launch
- **TCP diagnostic stream** on port 5555 for real-time structured debugging

### Changed

- Complete rewrite from monolithic EVEMon to modular EveLens architecture
- Rebranded from EVEMon to EveLens -- Character Intelligence for EVE Online

[#33]: https://github.com/aliacollins/EveLens/discussions/33
[#34]: https://github.com/aliacollins/EveLens/issues/34
[#37]: https://github.com/aliacollins/EveLens/issues/37
[#38]: https://github.com/aliacollins/EveLens/issues/38
[#39]: https://github.com/aliacollins/EveLens/issues/39
[#40]: https://github.com/aliacollins/EveLens/issues/40
[#41]: https://github.com/aliacollins/EveLens/issues/41
[#42]: https://github.com/aliacollins/EveLens/issues/42
[#43]: https://github.com/aliacollins/EveLens/issues/43
[#45]: https://github.com/aliacollins/EveLens/issues/45
[#47]: https://github.com/aliacollins/EveLens/issues/47
[#50]: https://github.com/aliacollins/EveLens/issues/50
[#51]: https://github.com/aliacollins/EveLens/issues/51
[#52]: https://github.com/aliacollins/EveLens/issues/52
[#53]: https://github.com/aliacollins/EveLens/issues/53
[unreleased]: https://github.com/aliacollins/evelens/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/aliacollins/evelens/compare/v1.1.0-beta.1...v1.1.0
[1.1.0-beta.1]: https://github.com/aliacollins/evelens/compare/v1.0.0...v1.1.0-beta.1
[1.0.0]: https://github.com/aliacollins/evelens/compare/v1.0.0-beta.2...v1.0.0
[1.0.0-beta.2]: https://github.com/aliacollins/evelens/compare/v1.0.0-beta.1...v1.0.0-beta.2
[1.0.0-beta.1]: https://github.com/aliacollins/evelens/compare/v1.0.0-alpha.1...v1.0.0-beta.1
[1.0.0-alpha.1]: https://github.com/aliacollins/evelens/releases/tag/v1.0.0-alpha.1
