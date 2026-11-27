# Computer sieges and pillage rewards

Status: **implemented** (`GameModel/Rules/SiegePlanner.cs`, `Core.Game/Opponents/ComputerPlayer.Siege.cs`,
pillage rewards in `GameModel/Rules/PillageRewards.cs` and `TurnController.PillageTile`).

## Why

The original computer sent its attackers to an enemy city one at a time, and each attacked as soon as it arrived.
A defended city beat them one by one.

## How the computer besieges a city now

Every computer attacker (land unit of the attack group) within 5 tiles of a seen enemy city on its landmass
takes part in a siege of that city (unless it is on its way somewhere else):

1. **Advance**: it walks to a free tile next to the city, preferring tiles where fewer of its own units stand
   (so the city is surrounded from every side), then near ones and good defensive ground.
2. **Hold**: next to the city it waits until the ring holds `defenders + 2` attackers (at least 3, at most 8,
   never more than the free tiles around the city) and they expect to kill all defenders. The expectation adds,
   for each attacker, its chance to beat the city's best defender in a battle fought in rounds, counted with the
   same strengths as `Battles` (walls, Stockade, fortress, terrain, veterans, hit points).
3. **Assault**: then every attacker around the city attacks in the same turn; attackers arriving later that turn
   join at once. An empty city is entered at once.
4. **Patience**: after 5 turns around the city the ring attacks when it expects to kill 60% of the defenders.
5. **Raid**: when all attackers within 10 tiles together expect to kill less than half the defenders (or the ring
   waited and is too weak), they pillage the enemy land around the city instead. A damaged unit goes for
   irrigation, the others for mines, railroads and roads.

An attacker that cannot get closer for 3 turns, or whose order did not move it, moves as before.

## Pillage rewards (every player)

| Destroyed | Reward |
|---|---|
| Irrigation | the unit gets all its hit points back |
| Mine | 40 gold |
| Railroad | 20 gold |
| Road | 20 gold |

Whoever pillages gets the reward: the human player, a computer civilization, and the barbarians too (their own
pillage, `TurnController.BarbarianPillages`, takes irrigation or a mine and is rewarded the same way).

## Measurement

`ProbeSimulation` (see `AI_EXPANSION_PLAN.md`) prints the siege orders given; `CIV_SIMULATE_NO_SIEGES=1` switches
the sieges off. 60 rounds on CIVIL0, CIVIL3, CIVIL6 and CIVIL14 played without errors, with rings gathering, waiting
and attacking together, and hopeless sieges turning to raids.

## Nightly game

`NightlyComputerGameTests` (workflow `.github/workflows/nightly-ai.yml`, every night at 2:00 UTC and on demand) plays
200 rounds with every civilization computer controlled on a new Europe game (`ComputerExpansionSimulation.NewEuropeGame`),
CIVIL10 (small islands), CIVIL12 (a large continent) and CIVIL15 (a game at war). It fails on an exception, a round
that hangs (over 120 s), a unit of an unknown kind, or a unit or city off the map, and keeps the cities of every
civilization per round as CSV files in the run's artifact. Locally:

```bash
CIV_NIGHTLY=1 CIV_NIGHTLY_ROUNDS=200 CIV_NIGHTLY_SAVES=EUROPE,CIVIL10 CIV_NIGHTLY_REPORT=report dotnet test --filter NightlyComputerGameTests
```
