# Contributing to EveLens

EveLens is an open source character monitoring and skill planning tool for EVE Online. It runs on Windows, macOS and Linux.

The source is open under GPL v2. Bug reports, diagnoses, ideas, translations and pull requests are all welcome, and all credited.

---

## Report a Bug

Bug reports are the most valuable contribution. File an issue at [github.com/aliacollins/evelens/issues](https://github.com/aliacollins/evelens/issues) with:

- What happened
- What you expected
- Steps to reproduce, if you can
- Screenshots or crash logs

If you run 10 or more characters, your reports are especially useful.

## Diagnose a Bug

If you have read the source and found the cause of a problem, describe it in the issue: which behaviour is wrong, where it comes from, and how you confirmed it. You are credited for the diagnosis.

## Send a Pull Request

Pull requests are welcome. EveLens handles ESI tokens for many characters, so every pull request goes through a security review before it can be merged, and that takes time. These rules keep the review possible:

- **Open an issue first** for anything larger than a small fix, so the approach is agreed before you write it.
- **One change per pull request.** Keep it small: a few files, a few hundred lines at most.
- **Source and tests only.** A pull request may change `.cs` and `.axaml` files under `src/` and `tests/EveLens.Tests/`, and add a line to `CHANGELOG.md`.
- **Not accepted in a pull request:** project and build files, scripts, tools, packages or package versions, data files, images, fonts, binaries, and the tests under `tests/EveLens.Tests/Architecture/`. If your change needs one of these, say so in the issue and the maintainer makes that part.
- **Say so up front** if the change adds or alters network access, file access, running other programs, token or credential handling, or updating. Those changes need agreement in an issue before any code.
- **Add tests** for the behaviour you change.
- **Do not force-push after review has started.** Add new commits instead.

What to expect:

- A pull request may be merged as it is, merged with follow-up changes by the maintainer, or re-written by the maintainer from your idea. In every case you are credited by name.
- A pull request that breaks the rules above is closed with a note on what to change.
- Pull requests are merged by the maintainer from the reviewed commit. Your authorship is kept on the commit.

## Suggest a Feature

Open a discussion at [github.com/aliacollins/evelens/discussions](https://github.com/aliacollins/evelens/discussions) and describe what you want to do in game terms. Screenshots and examples from your own characters help.

## Translate

Translations of the interface text are welcome. Open a discussion to offer a language or a correction, and attach the text as a plain text file.

## Report a Security Problem

Do not open a public issue. See [SECURITY.md](SECURITY.md).

---

## Credit

Everyone whose report, diagnosis, idea, translation or code ends up in a release is thanked by name in the release notes.

---

## Building From Source

You are free to build, run and fork EveLens under GPL v2.

Requirements: the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and Git.

```bash
git clone https://github.com/aliacollins/evelens.git
cd evelens
dotnet build
dotnet test
dotnet run --project src/EveLens.Avalonia/EveLens.Avalonia.csproj
```

---

## Community

- Be constructive and respectful. We are all here because we care about EVE tools.
- Questions about how EveLens works are welcome in discussions.

---

Fly safe. o7
