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
- A test also checks that its scenarios met every branch of the function (for example every outcome of a hut), otherwise
  a green test would not say much.

Not compared: the stack below SP (the original code leaves its locals there, and a later function may read them before
setting them, see `ShiftUnitFrame` in `UnitManagement`); the golden master is still needed for that.

## Adding a function

1. Copy its original code into the reference class of its module, from the commit before the rewrite
   (`git show <commit>:Civilization1Remaster.Core/Game/UnitManagement.cs`, then the method as it is).
2. Write the scenarios: random inputs with a fixed seed (`new Random(bay)`) plus the edge cases, set up identically in both
   games through `Prepare`, and an assertion that every branch was met.
3. Check that the test catches a mistake: change the rewritten function a little (for example a constant), the test must
   fail, then undo it.

## Running

```bash
./bin/Debug/net10.0/Civilization1Remaster.Tests.exe -class "*Differential*"
```

They run with `dotnet test` too, and are skipped without the `CIVILIZATION` folder.
