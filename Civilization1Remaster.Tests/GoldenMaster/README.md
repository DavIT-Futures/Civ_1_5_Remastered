# Golden master tests

A regression net for the refactoring of the emulated x86 code (`Civilization1Remaster.Core`) into clean C# rules
(`Civilization1Remaster.GameModel/Rules`). The tests play whole turns of the real game, without a window, and compare
the game state after every turn with a baseline recorded from the code **before** the refactoring.
A refactoring that changes nothing in the game keeps them green; one that changes a rule, a random draw or a memory
byte of the game turns them red and tells in which turn and in which value the game went another way.

## What a run does

`HeadlessGame` (no Avalonia, no sound, no real time):

1. copies the game folder `CIVILIZATION` (needed, not in the repository) to a temporary folder without the sound
   drivers (`*.CVL`), and puts the saved games of the test (`Saves/AUTO00 - AUTO14`, `.SVE`, `.MAP`, `.HP`) into it,
   so the autosaves the game writes every turn and the player's own autosaves don't matter;
2. prepares the machine (`GameEngine.PrepareMachine`), skips the intro and the menus, seeds the C runtime random
   generator with `1991` (instead of the clock) and loads the autosave like *Load Game* during play;
3. plays 5 turns (`TurnEvents.HandleGameEnd`, one turn of all players). The human player is played by
   `ScriptedKeyboard`: when the game looks for a key, it types the next one of `Space, Enter, '1'`
   (skip the unit, accept the default, first part of the palace);
4. after loading and after every turn takes a `GameSnapshot`.

Determinism comes from test seams that do nothing in the desktop game:
`CPU.FixedClockSpan` (instead of the real time a timer interrupt after every 50 calls of game functions, at every wait for
the timer (`Idle`) or a key, and after every 16 reads of the timer variables 0x40 – 0x6f or polls of the music position
(`CPU.PollMusic`, a one-line seam in `SystemServices.ObtainSongPosition` of both games) for the loops that wait without
calling a game function; the calls are counted in every journal the game switches to (`CPU.Journal`) – since B6.0, 2026-10-25; before, after every 10 000 memory accesses, which moved the interrupts whenever
the code made fewer accesses), `CPU.InputStarved` (the scripted keyboard), `LogWrapper.ScopeListener` (call counts),
and a stable hash for the city picture in `CityView` (`string.GetHashCode` is different in every process).
The data moved out of the emulated memory into C# is written back at its address before the data segment is hashed
(`Dual/MovedData.Project`), so the baselines stay valid while the state leaves the memory.
`GoldenMasterDeterminismTests` plays the same game twice and requires identical snapshots.

## What is compared

`GameSnapshot` is a sorted list of `key=value` (0 and empty values are left out):

| Channel   | Content | Fails the test |
|-----------|---------|----------------|
| `state.`  | every field of `GameState` by reflection: players, cities, units, gold, science, relations, hit points, ... (number arrays in chunks of 16 items) | yes |
| `engine.` | the global variables of `GameEngine` | yes |
| `mem.`    | FNV hash of every 256-byte page of the data segment `0x3b01` (units at `0x81d4 + 0x600 * player + 0xc * unit`, ...), without the timer variables, the path of CIV.EXE and the stack | yes |
| `map.`    | hash of every 10 rows of the map picture (screen 2: terrain, roads, irrigation, ...) | yes |
| `rng.`    | hash of the state of the random generator: catches a different number of random draws | yes |
| `cpu.`    | stack pointer between turns: the original code leaves it where it was | yes |
| `call.`   | how many times each game function was called in the turn (processor Journal) | no, it is shown to locate a divergence; refactored functions disappear from it on purpose |
| `input.`  | keys typed in the turn | no |

A failure looks like:

```
AUTO03 diverges from the baseline (Source: commit a3b5a3b ...), first divergence after turn 2 (3 values, expected -> actual):
  state.Participants[4].Gold: 312 -> 309
  ...
call counts that differ in that turn (they show where the paths split):
  call.ComputeCityTrade: 40 -> (0)
```

## Baselines

`Baselines/AUTOnn.txt`: a header, then `turn 0` (after loading) with all values and every next turn with only the
values that changed (`-key` = back to 0). The `call.` and `input.` values are written for every turn.

The committed baselines were made from commit **a3b5a3b** ("Add window close confirmation dialog and remove legacy
WinForms project"), the last commit before the first refactoring (075f753). Its game code is identical to **d175cdb**
(the original WinForms version): between them only the host changed (`IGameHost` instead of `MainForm` in `GameEngine`
and `VGADriver`, the Avalonia window). a3b5a3b is used because it already has the headless-capable `Civilization1Remaster.Core`.

The original code throws `IndexOutOfRangeException` in 7 of the 15 runs (the map visibility array at the poles and
`MissingTroops[-1]` in `UnitManagement.EliminateUnit`), so two crash guards that HEAD also has were added to it
(`baseline-crash-guards.patch`): `ChartSightGrid` instead of `sbyte[,]` and the bounds check of `MissingTroops`.
Both change only what happens where the original code crashed. HEAD's other change in `EliminateUnit` (leave at once
for an eliminated unit) is **not** in the baseline, it also changes cases that didn't crash.

Since 2026-10-12 the baselines also have `baseline-interrupt-registers.patch`: the timer interrupt (`CPU`, `ClockInterrupt`)
and the mouse interrupt keep the interrupted registers and flags in C# variables instead of pushing them onto the stack of
the game. The game reads some locals before it sets them (`ShiftAIUnit`), so what an interrupt left below SP mattered,
and it depended on when the interrupt came (after a number of memory accesses): a rewritten function with fewer accesses
moved it and AUTO07 threw. With the patch the original game plays the 15 saves exactly as before (only the header of the
baselines changed), and the garbage below SP depends only on the code.

### Regenerating the baselines

From the code in your working copy (only when a change of the game is intended, otherwise the net is gone):

```bash
cd Civilization1Remaster.Tests
dotnet build
CIV1_GOLDEN_MASTER_UPDATE=1 CIV1_GOLDEN_MASTER_SOURCE="commit $(git rev-parse --short HEAD)" ./bin/Debug/net10.0/Civilization1Remaster.Tests.exe -class "*GoldenMasterAuto*"
```

(PowerShell: `$env:CIV1_GOLDEN_MASTER_UPDATE=1` before the command.) `CIV1_GOLDEN_MASTER_SOURCE` is written into the
header. Review the diff of `Baselines/` before committing it.

**Since phase 1 (2026-10-26) the committed baselines are those of the remaster**: recorded from the working copy as above
with `HeadlessGame` playing the remaster (every `TurnHooks.Classic…` switch off, the default; `classic: true` turns them
all on, as `DifferentialPair` and `DualGame` do for the comparisons with the original code). The classic baselines, made
from the original code as below, are at the git tag `classic-final`. The report of the change (per autosave: identical
after loading, first differences in turn 1 – healing `Vitals`, the random generator of the two disaster draws – growing
to 36 – 172 values in turn 5 as the computer players move otherwise) is in PLAN-PO-MIGRACJI.md, phase 1.

From the original code (how the classic baselines, tag `classic-final`, were made):

```bash
git worktree add --detach ../civ1-baseline a3b5a3b
# the test seams: the commit "Add test seams for a deterministic headless run" of this branch
git -C ../civ1-baseline cherry-pick --no-commit <seams commit>
git -C ../civ1-baseline apply "$PWD/Civilization1Remaster.Tests/GoldenMaster/baseline-crash-guards.patch"
git -C ../civ1-baseline apply "$PWD/Civilization1Remaster.Tests/GoldenMaster/baseline-interrupt-registers.patch"
cp -r CIVILIZATION ../civ1-baseline/
cp -r Civilization1Remaster.Tests/GoldenMaster Civilization1Remaster.Tests/Civilization1Remaster.Tests.csproj ../civ1-baseline/Civilization1Remaster.Tests/
cd ../civ1-baseline/Civilization1Remaster.Tests
dotnet build
CIV1_GOLDEN_MASTER_UPDATE=1 CIV1_GOLDEN_MASTER_SOURCE="commit a3b5a3b + crash guards + test seams + interrupt registers outside the stack" ./bin/Debug/net10.0/Civilization1Remaster.Tests.exe -class "*GoldenMasterAuto*"
cp GoldenMaster/Baselines/* <repository>/Civilization1Remaster.Tests/GoldenMaster/Baselines/
```

A change of the harness that changes the snapshots (other keys, other saves, more turns) needs new baselines made this way.

From the frozen game (`Civilization1Remaster.Original`, the code at the end of the migration, with the current harness):
add `CIV1_GOLDEN_MASTER_FROZEN=1` to the command above (working copy). B6.0 (2026-10-25) checked the new clock this way:
the frozen game with the clock by calls gives the same behaviour channels as the committed baselines (only the header
and the informational `call.` counts differ), so the baselines from a3b5a3b were kept.

## Running

Run the test executable (`dotnet test` finds no tests): each autosave is its own class, so they run in parallel. In Release
with `-parallelMode all` the 15 autosaves take about 30 seconds on 16 cores (Debug: several minutes, the same results):
`./bin/Release/net10.0/Civilization1Remaster.Tests.exe -parallelMode all -class "*GoldenMaster*"`.
Without the `CIVILIZATION` folder they are skipped. One autosave:

```bash
./bin/Debug/net10.0/Civilization1Remaster.Tests.exe -class "*GoldenMasterAuto03"
```

When a run hangs (the key script doesn't get through a screen, the limit is 20 000 keys a turn), set
`CIV1_GOLDEN_MASTER_TRACE=1`: the last called game function is written every 2 seconds to `%TEMP%\Civ1GoldenMaster-AUTOnn.log`.
The journal of all calls: `CIVILIZATION1REMASTER_LOG=1` writes `Log.txt` next to the test executable; it also reports
stack leaks ("Stack leak detected in function ...").

## State

Since 2026-10-12 the code on `main` matches the baselines: the refactorings that changed the game were reverted or
fixed (see `MIGRATION-BACKLOG.md` in the repository root, also for the improvements that wait for the end of the
migration and the rules learned: units live in the emulated memory at `0x81d4`, not in `GameState...Troops`; keep the
register contract of a function; remove a frame only with all its locals).
