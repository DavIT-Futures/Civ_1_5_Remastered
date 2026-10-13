# Differential tests

The golden master plays only 5 turns of 15 autosaves, so many functions or branches are never run by it (huts, the stack
list, the spaceship, the replay, the map generator, ...). A differential test calls one rewritten function directly and
compares it with its original code, in many scenarios that the saves don't reach.

## How it works

- `Reference/*Reference.cs`: the original (translated asm) code of rewritten functions, **copied verbatim from git**, from
  the commit before the rewrite (never typed again by hand). Only the calls of other methods of the same class are
  redirected to the current game code (`this.owner.UnitOps.ConstructUnit(...)`), so a test checks exactly one function.
- `DifferentialPair`: two `HeadlessGame`s loaded from the same autosave with the same random seed. `Prepare` changes both
  in the same way (the scenario: positions of units, the year, the random seed, ...), `Run` calls the original code in one
  and the rewritten code in the other and compares everything: the `GameSnapshot` (game state, data segment, map, random
  generator, stack pointer, call counts), the pictures of all screens, the registers the callers depend on (SI, DI, BP, SP,
  and AX for a function that returns a value) and an exception, if one is thrown. While both games match, they stay
  identical, so the scenarios run one after the other without loading again.
- The order of the called game functions with their arguments (the journal scopes) must be the same too; it catches a
  wrong argument of a drawing call even when the picture is empty. Addresses in the stack (0xe8c0 - 0xf0bf) are left out of
  it: a buffer of a caller moves when a rewritten function has no stack frame any more.
- Every time the scripted keyboard types a key, the hash of every screen goes into that trace too: windows, messages and
  pictures that a function takes away again before it returns (the screen is put back from the back screen 0x19d4) are
  compared as the player saw them.
- The snapshot does not see files and leaves out the mouse state (0x586e - 0x5873); a test that needs them copies them into
  the data segment or names them in `Watched` (see `BatchElevenDifferentialTests`, `BatchTwelveDifferentialTests`).
- `GameSnapshot` sees only the data segment 0x3b01. A function that writes tables in other segments (the spaceship and the
  Hall of Fame at 0x3772, the replay at 0x3725, the colour cycles at 0x35cf, ...) names them in `Scenario.Watched`.
- A test also checks that its scenarios met every branch of the function (for example every outcome of a hut), otherwise
  a green test would not say much.

Not compared: the stack below SP (the original code leaves its locals there, and a later function may read them before
setting them, see `ShiftUnitFrame` in `UnitManagement`); the golden master is still needed for that.

## Adding a function

1. Generate its reference copy from the last commit with the original code (one file per function, the calls of other
   methods of its class go to the current code), and add the class to `Reference/ReferenceClasses.cs` when it is new:
   `python Civilization1Remaster.Tests/Differential/Reference/extract_reference.py <commit> Civilization1Remaster.Core/Game/UnitManagement.cs UnitOps ShiftUnit`
2. Write the scenarios with `DifferentialRun.Check` (see `UnitDifferentialTests.cs`): random inputs with a fixed seed (`new Random(bay)`) plus the edge cases, set up identically in both
   games through `Prepare`, and an assertion that every branch was met.
3. Check that the test catches a mistake: change the rewritten function a little (for example a constant), the test must
   fail, then undo it. Undo with a patch of the uncommitted work (`git diff > work.patch`, then `git checkout` and
   `git apply work.patch`), never with a bare `git checkout` of the folder.

## Running

In Release (about 7 times faster than Debug, the same results), all tests in parallel:

```bash
dotnet build -c Release
./bin/Release/net10.0/Civilization1Remaster.Tests.exe -parallelMode all -class "*Differential*"
```

They are skipped without the `CIVILIZATION` folder. `-method "*SomeTest"` runs one test; several `-class` filters add up.
`Reference/check_mutations.py` runs the mutation check of a batch (see the skill `.claude/skills/migrate-batch`).
