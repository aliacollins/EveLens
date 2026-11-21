# Contributing to EveLens

EveLens is an open source character monitoring and skill planning tool for EVE Online. It runs on Windows, macOS and Linux.

The source is open under GPL v2. Code contributions are not accepted: every line in EveLens is written by the maintainer, and pull requests can only be opened by the maintainer. There are still several ways to help, and they are all credited.

---

## Report a Bug

Bug reports are the most valuable contribution. File an issue at [github.com/aliacollins/evelens/issues](https://github.com/aliacollins/evelens/issues) with:

- What happened
- What you expected
- Steps to reproduce, if you can
- Screenshots or crash logs

If you run 10 or more characters, your reports are especially useful.

## Diagnose a Bug

If you have read the source and found the cause of a problem, describe it in the issue: which behaviour is wrong, where it comes from, and how you confirmed it. The fix is then written and tested by the maintainer, and you are credited for the diagnosis.

Please describe the cause in words. Patches, diffs and code attachments are not applied.

## Suggest a Feature

Open a discussion at [github.com/aliacollins/evelens/discussions](https://github.com/aliacollins/evelens/discussions) and describe what you want to do in game terms. Screenshots and examples from your own characters help.

## Translate

Translations of the interface text are welcome. Open a discussion to offer a language or a correction, and attach the text as a plain text file.

## Report a Security Problem

Do not open a public issue. See [SECURITY.md](SECURITY.md).

---

## Credit

Everyone whose report, diagnosis, idea or translation ends up in a release is thanked by name in the release notes.

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
