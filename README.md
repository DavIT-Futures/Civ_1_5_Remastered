# Civ 1.5 Remastered

A C# remaster of Civilization (1991) for DOS. The original `CIV.EXE` is translated function by function into C#, so
the game plays like it did in 1991, with the rough edges fixed:

- **Battles in rounds:** units have hit points, so a weak unit no longer destroys a much stronger one in one roll.
- **Healing:** damaged units heal every turn, in cities, fortified, or resting near the coast.
- **Railroads:** cities have railroads, and moves along railroads are free also with GoTo.
- **Load Game** in the game menu, with an autosave every turn.

The full list of changes, plans and how it works: [the project website](https://davit-futures.github.io/Civ_1_5_Remastered/).

## Play

1. Download the latest release from [Releases](https://github.com/DavIT-Futures/Civ_1_5_Remastered/releases) and unpack it.
2. Put the files of Civilization 1 for DOS in a folder named `CIVILIZATION` next to `Civ_1_5_Remastered.Desktop.exe`.
   The game files are not part of this repository; to find a copy, search for
   [Civilization 1991 DOS](https://www.google.com/search?q=Civilization+1991+DOS).
3. Run `Civ_1_5_Remastered.Desktop.exe`.

## Build from source

You need the .NET 10 SDK and the `CIVILIZATION` folder next to `Civ_1_5_Remastered.sln`.

```bash
dotnet build Civ_1_5_Remastered.sln
```

```bash
dotnet run --project Civ_1_5_Remastered.Desktop
```

```bash
dotnet test Civ_1_5_Remastered.sln
```

## Legal

Civ 1.5 Remastered is an unofficial fan project. It is not affiliated with or endorsed by MicroProse, Firaxis Games or
Take-Two Interactive. Civilization is a trademark of its owners. This repository contains no files of the original
game. The code is under the MIT license, see `LICENSE`.
