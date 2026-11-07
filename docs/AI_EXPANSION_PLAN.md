# AI expansion plan

Status: **implemented** (stages 1-3). Stage 4 (measurement in real games) is up to play-testing.

Decisions taken: city spacing 5 tiles, 4 on a small landmass (< 80 land tiles); Chieftain and Warlord keep the
mild original expansion (only the site planner); islands are colonized in the same step.

Implementation: `GameModel/Rules/CitySitePlanner.cs` (pure, tested), `Core.Game/Opponents/ComputerExpansion.cs`
(levels, targets, reservations, harbours, ships), `ComputerPlayer.Expansion.cs` (settlers and colony ships),
`ResearchOffice.Expansion.cs` (production priority).

| Level | City target | Settlers at once |
|---|---|---|
| Prince | 10 | 1 + cities/4 |
| King | 16 | 1 + cities/3 |
| Emperor | 24 | 2 + cities/3 |

The leader's expansion trait adds or removes up to 4 cities.

## Why the computer founds few cities

1. **Spacing rule** (`Core.Game/Opponents/ComputerPlayer.Military.cs`, `ExploreAround`): every map tile has a
   precomputed site value 8..15 (map plane 2, x+80). A computer settler founds a city only when the value is
   >= 8, it is the best of the neighbouring tiles and `15 - distance to nearest city <= value`. Average sites
   (8-10) therefore need 5-7 tiles from the nearest city; the 45-tile ring around a city is lowered to 8.
2. **Random wandering** (`ComputerPlayer.Settlers.cs`, `DirectSettlers`): looking for a site, a settler walks
   4 tiles in a random direction instead of heading for the best free site.
3. **Rare settler production** (`Core.Game/Research/ResearchOffice.Production.cs`, `ScoreUnitChoice`): settlers
   are not even a candidate when the AI already has one (or another city builds one) and the food surplus is
   small, so usually only one settler exists at a time.
4. **Distractions**: terrain improvement (`ImproveSettlersTile`) comes before site search; once the continent
   holds more cities than `ConstructionSpotTally / 8`, settlers join cities (`ComputerPlayer.UnitMoves.cs`,
   `DecideBeforeFight`).

## Plan

### Stage 1 - expansion priority
- **City site planner** (pure, testable like `SettlerPlanner`): scan known tiles of the continent within ~12
  tiles; score = map site value + food/shields/trade of the 21 tiles + coast/river + specials - travel time
  (via `GotoPathfinder`) - overlap with own cities; minimum distance 3 from own cities, 4 from foreign ones.
  Settler goes there with GoTo and founds the city; sites are reserved so two settlers never pick the same one.
- **Expansion phase in production**: while free sites exist and the city target is not reached, cities of size
  >= 2 (>= 3 under Monarchy) with food surplus get a high Settlers priority, after one defender; up to about
  one settler per two cities.
- **Settler duties**: found cities first; improve terrain only when no site is left or the target is reached;
  join cities only when the continent is full.

### Stage 2 - difficulty scaling
| Level | Target / pace |
|---|---|
| Chieftain, Warlord | near original (only the site planner) |
| Prince | moderate expansion |
| King | strong expansion, spacing 3 |
| Emperor | maximum: top settler priority, dense packing |

The leader "expansion" trait (leader table field at 0x32, read at `0x1514 + 0x3a * leader`) scales the target.

### Stage 3 - islands
When the continent is full: Trireme/Sail + Settlers + defender to the nearest free island, reusing
`ComputerPlayer.Transport.cs`.

### Stage 4 - measurement
- Unit tests of the site planner (spacing, scoring, reservations).
- Control report: cities per computer civilization at turns 50/100/150, before and after, to tune thresholds.

## Risks
Harder games on Emperor (intended), less free land for the player and earlier border contact; old saves keep
working but the AI plays by the new rules at once.

## Decisions
1. Spacing: 5 tiles, 4 on small islands.
2. Chieftain/Warlord: near original (site planner only).
3. Islands: colonized now.
