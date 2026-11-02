# Civ 1.5 Remastered

A C# remaster of Civilization (1991) for DOS. The original `CIV.EXE` is translated function by function into C#, so
the game plays like it did in 1991, with the rough edges fixed:

- **Battles in rounds:** units have hit points, so a weak unit no longer destroys a much stronger one in one roll.
- **Healing:** damaged units heal every turn, in cities, fortified, or resting near the coast.
- **Railroads:** cities have railroads, and moves along railroads are free also with GoTo.
- **Load Game** in the game menu, with an autosave every turn.

The full list of changes, plans and how it works: [the project website](website/).

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

## Website

The website lives in `website/` (Astro) and is published to GitHub Pages by `.github/workflows/website.yml`.

```bash
cd website && npm install && npm run dev
```
