# Migracja asm → C#: lista na później

Zasada od 2026-10-12: najpierw migracja kodu emulowanego (asm-style C#) na czytelny C# **bez żadnej zmiany zachowania**.
Każdy krok musi przechodzić testy golden master (`Civilization1Remaster.Tests/GoldenMaster`, porównanie z oryginalnym
kodem gry). Usprawnienia i zmiany reguł gry wchodzą dopiero na sam koniec, gdy wszystko będzie przeniesione i przetestowane.

## Usprawnienia wycofane z gry (do zaimplementowania na końcu)

| Usprawnienie | Skąd (commit) | Co zostało w kodzie |
|---|---|---|
| Szybsze leczenie: 2 HP na turę w mieście, okręty naprawiane przy wybrzeżu (do 3 pól od miasta) | eed8efb | `HealingRules`, `UnitHealth.MendAmount` (nieużywane), testy `HealingRulesTests` |
| Ekran „Obsolete Wonders” w menu raportów i dopisek „(Obsolete)” na liście cudów | a04e786 | nic (`WorldMap`, `GameMenus` przywrócone) |
| Rozszerzone raporty doradców: postęp badań „(x/y)”, nauka na turę, sumy handlu i plonów, podsumowanie miast | a04e786, 2c9ab40 | `AdvisorStatistics` (nieużywane). Uwaga: wersja z refaktora wołała `HandleCity`, które ma skutki uboczne, więc nowa wersja musi liczyć bez zmiany stanu |
| `EliminateUnit`: natychmiastowe wyjście dla już usuniętej jednostki | a04e786 | nic (zostało tylko sprawdzenie zakresu, jak w baseline) |

## Refaktory wycofane do oryginału (do zrobienia jeszcze raz, krok po kroku, z golden master)

Przepisane przez poprzedni model z błędami, przywrócone do stanu z a3b5a3b:

- `UnitCombat.cs`, `DiplomatActions.cs`, `TradeAndConquest.cs`: czytały i zapisywały `World.Participants[].Troops`,
  które jest **tylko kopią z chwili wczytania** gry; prawdziwe jednostki są w pamięci emulatora pod
  `0x81d4 + 0x600 * gracz + 0xc * jednostka`. Na przykład awans na weterana po walce znikał.
- `GovernmentAndDiplomacy.cs`: inna ocena traktatów przez AI (`AIWeighsTreaty`, `Relations`).
- `TurnEvents.cs`: zgubiona ramka stosu w `HandleEndOfTurnEvents` (crash „Stack overflow”), `DisplayFinalScore`
  wołane zawsze zamiast tylko przy włączonej opcji pałacu, zmienione leczenie.
- `Pathfinding.cs`: `MovementRules.DirectionTo` zwraca 0 dla celu, który nie jest sąsiednim polem; oryginał robi krok
  w stronę celu z każdej odległości (znak przesunięcia, zawinięcie przy |dx| >= 40).
- `CivilizationSchism.cs`: duże przepisanie, którego testy nie sprawdzają (schizma nie zdarza się w 5 turach z tych zapisów).
- `AdvisorReports.cs`: same usprawnienia (patrz wyżej) i zgubione zerowanie zmiennej lokalnej 0x2.

Nieużywane teraz klasy reguł (testy jednostkowe sprawdzają je tylko same ze sobą, **nie z oryginałem**) – przed
ponownym użyciem trzeba je porównać z kodem asm i z golden master: `CityConquestRules`, `CityGrowthAndProductionRules`,
`CityHappinessRules`, `DiplomatRules`, `GovernmentRules`, `HealingRules`, `PathfindingRules`, `SchismRules`,
`SettlerActionRules`, `TradeRules`, `TurnEventRules`, `UnitStack`, `AdvisorStatistics`, część `CombatRules`
i `MovementRules`. Testy `AssemblyParityDifferentialTests` porównują C# z ręcznie przepisanym C#, nie z emulowanym kodem.

## Refaktory, które zostały (przechodzą golden master)

- `CityFoundingAndScore`: `DisplayFinalScore` (poprawione: specjaliści dają 1 punkt, zachowany kontrakt rejestrów
  SI/AX dla `TurnEvents`), `HandleCityDisasters` (zaraza, głód).
- `CityManagement`: `ObtainCityTileIndex`, `AssignCitizenType`, `ObtainCitizenType`, `TallySpecialists`.
- `ScienceAndAdvisors`: `EarnTechnology`, `PlayerKnowsTechnology` (poprawione: technologia -1 = „bez wymagań” jest znana),
  `ObtainTechnologyInfo`.
- `UnitManagement`: `LocateBestGuardianStep`, `ContainsAdjacentForeignUnit`.
- `MapManagement` (2026-10-12): dostęp do mapy przez `MapLayers` (warstwy obrazu mapy), `MapRules` (granice, specjalne
  zasoby, wioski) i `UnitMemory` (adresy jednostek pod `0x81d4`): `VerifyMapLimits`, `ObtainTerrainType`, `ObtainTileOwner(Bits)`,
  `MapAssignCityOwner`, `TagTileOwned`, `ObtainUnitAtTile`, `ContainsRoad`, `ObtainTileImprovements`, `ObtainKnownTileImprovements`,
  `KeepKnownTileImprovements`, `DiscardTileImprovements`, `ContainsSpecialResource`, `ContainsHut`, `ObtainContinentID`,
  `ObtainLandMassSize`.

## Poprawki, które zostają (chronią przed crashami, baseline je ma)

- `ChartSightGrid` zamiast `sbyte[,]` (indeksy poza mapą przy biegunach).
- Sprawdzanie zakresu `MissingTroops` / `LiveTroops` w `EliminateUnit`.

## Zasady dla następnych refaktorów (wnioski)

1. Stan gry na żywo: miasta i gracze w `GameState`, ale **jednostki w pamięci emulatora** (`0x81d4`), nie w `Troops`. Do jednostek służy `UnitMemory`, do mapy `MapLayers`.
2. Zachować kontrakt rejestrów: wartość zwracana w AX (`RegAcc`), zachowane SI/DI/BP, jeśli wołający na nich polega.
3. Usuwając `EnterFrame` / `LeaveFrame`, usunąć wszystkie użycia zmiennych lokalnych i epilog.
4. Po każdym kroku: `dotnet test` (golden master ok. 2,5 min). Czerwony test = zmiana zachowania.

## Zmiany hosta (nie dotyczą logiki gry, zostają)

Okno Avalonia: ustawienia skali i pełnego ekranu, skróty klawiszowe osadników, potwierdzenie zamknięcia, logowanie crashy.
