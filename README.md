# Civ 1.5 Remastered

A C# remaster of Civilization (1991) for DOS. The original `CIV.EXE` is translated function by function
into C# that runs on an emulated 8086 machine; the window is Avalonia, the original sound drivers run in a small
machine code interpreter.

## What you need

- .NET 10 SDK
- The original game files of Civilization 1 for DOS in a folder named `CIVILIZATION` next to
  `Civ_1_5_Remastered.sln`. They are not part of this repository.

## Build and run

```bash
dotnet build Civ_1_5_Remastered.sln
```

```bash
dotnet run --project Civ_1_5_Remastered.Desktop
```

```bash
dotnet test Civ_1_5_Remastered.sln
```

## History

The history keeps every change with its date, but only the latest commits build: files that are no longer part of
the project were removed from the whole history, so older commits miss some of the code and data they used. To build
and play, use the tip of `main`.

## Layout

| Project | What it holds |
|---|---|
| `Civ_1_5_Remastered.Machine` | the emulated processor, memory and the C runtime of the original |
| `Civ_1_5_Remastered.Imaging` | screens, pictures and fonts |
| `Civ_1_5_Remastered.Acoustics` | sound drivers and synthesizers |
| `Civ_1_5_Remastered.Foundation` | streams, compression, randomness, logging |
| `Civ_1_5_Remastered.GameModel` | game state and rules |
| `Civ_1_5_Remastered.Core.Shared` | service interfaces, game memory, tables and texts |
| `Civ_1_5_Remastered.Core.Engine` | drawing, sessions and platform services |
| `Civ_1_5_Remastered.Core.Game` | the game logic |
| `Civ_1_5_Remastered.Core.UI` | the game screens |
| `Civ_1_5_Remastered.Core` | `GameEngine`, which puts the services together |
| `Civ_1_5_Remastered.Desktop` | the Avalonia window |
| `Civ_1_5_Remastered.Tests` | tests, one folder per project |
