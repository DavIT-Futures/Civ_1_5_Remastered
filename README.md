# Civilization1Remaster

A C# remaster of Civilization (1991) for DOS. The original `CIV.EXE` is translated function by function
into C# that runs on an emulated 8086 machine; the window is Avalonia, the original sound drivers run in a small
machine code interpreter.

## What you need

- .NET 10 SDK
- The original game files of Civilization 1 for DOS in a folder named `CIVILIZATION` next to
  `Civilization1Remaster.sln`. They are not part of this repository.

## Build and run

```bash
dotnet build Civilization1Remaster.sln
```

```bash
dotnet run --project Civilization1Remaster.Desktop
```

```bash
dotnet test Civilization1Remaster.sln
```

## History

The history keeps every change with its date, but only the latest commits build: files that are no longer part of
the project were removed from the whole history, so older commits miss some of the code and data they used. To build
and play, use the tip of `main`.

## Layout

| Project | What it holds |
|---|---|
| `Civilization1Remaster.Machine` | the emulated processor, memory and the C runtime of the original |
| `Civilization1Remaster.Imaging` | screens, pictures and fonts |
| `Civilization1Remaster.Acoustics` | sound drivers and synthesizers |
| `Civilization1Remaster.Foundation` | streams, compression, randomness, logging |
| `Civilization1Remaster.GameModel` | game state and rules |
| `Civilization1Remaster.Core.Shared` | service interfaces, game memory, tables and texts |
| `Civilization1Remaster.Core.Engine` | drawing, sessions and platform services |
| `Civilization1Remaster.Core.Game` | the game logic |
| `Civilization1Remaster.Core.UI` | the game screens |
| `Civilization1Remaster.Core` | `GameEngine`, which puts the services together |
| `Civilization1Remaster.Desktop` | the Avalonia window |
| `Civilization1Remaster.Tests` | tests, one folder per project |
