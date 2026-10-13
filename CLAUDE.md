# Civilization1Remaster

C# remake of Civilization 1 (DOS): `Civilization1Remaster.Core` is the original CIV.EXE translated to emulated x86 C#
(`Civilization1Remaster.Machine`, the CPU), `Civilization1Remaster.Desktop` is the Avalonia window. The user is Polish: answer in Polish.

## Current work: migration asm → C#

The translated code is being rewritten as clean C# **without any change of behaviour** (improvements wait until the end).
Before working on it read `MIGRATION-BACKLOG.md` ("Jak pracować", "Następny krok") and use the project skill `migrate-batch`
(`.claude/skills/migrate-batch/SKILL.md`) for each batch.

- Commit straight to `main`, no branches; end commit messages with the `Co-Authored-By` line.
- Every rewritten function gets a differential test (`Civilization1Remaster.Tests/Differential`, README there) and a mutation check;
  the golden master (`Civilization1Remaster.Tests/GoldenMaster`, README there) must stay green.
- Tests: `dotnet test` finds no tests; run `Civilization1Remaster.Tests/bin/Debug/net10.0/Civilization1Remaster.Tests.exe` and
  read only filtered output (`grep -E "\[FAIL\]|Total:"`). They need the `CIVILIZATION` game folder (not in the repository).
- After a batch: `dotnet build Civilization1Remaster.sln`; the game is
  `Civilization1Remaster.Desktop/bin/Debug/net10.0/Civilization1Remaster.Desktop.exe`. Locked files = the game is running: ask the
  user to close it, never kill it.
- Write long code with the Write tool into the scratchpad and swap methods with `replace_methods.py`; long Bash heredocs break.
