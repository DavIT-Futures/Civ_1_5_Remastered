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
- Stosy jednostek i reszta mapy (2026-10-12): `AttachUnitToStack`, `DiscardUnitFromStack` (`UnitCombat`), `VisitEachUnitInStack`
  (+ `ForEachUnitInStack`), kroki `TallyUnitsByDomainStep`, `TallyUnitsOfTypeStep`, `TallyUnitsWithFlagStep`,
  `TallySpecialUnitsStep` (`UnitManagement`, z tablicą typów jednostek `UnitKindTable` pod `0x112a`), `SituateUnitOnTile`,
  `DiscardUnitFromTile`, `AssignTileImprovements`, `LocateCityAtTile` (`MapManagement`).
- Usuwanie jednostek i rozkazy (2026-10-12): `EliminateUnit` (statek transportowy tonący na oceanie zabiera ładunek, lotniskowiec
  samoloty), kroki stosu `VerifyUnitTerrainFlag`, `WipeUnitOrders`, `RestoreUnitOrders`, `RestoreUnitStatus`, `EliminateLandUnit`,
  `EliminateNonLandUnit`, `EliminateSeaUnit` (usuwa samoloty, `LandGroup` 1, mimo nazwy). Golden master nie wykonuje
  `RestoreUnitOrders` i `EliminateSeaUnit`, zostały porównane z oryginałem ręcznie.
- Obrońcy, strefa kontroli i nowe jednostki (2026-10-12): `LocateStackLeader`, `LocateBestGuardianInStack`, opakowania
  `Tally…InStack`, `MayOccupyTile`, `VerifyTileOccupation` (strefa kontroli; odkrywa człowiekowi sąsiednie obce jednostki i miasta),
  `ConstructUnit` (pierwszy wolny rekord 0 - 126, miasto macierzyste, paliwo samolotów). Tabele kierunków `0x1882`/`0x18e4`
  w `MapLayers`.
- Wyszukiwanie i odległości w `CommonUtilities` (2026-10-12): `LocateCityAtPosition`, `LocateNearestCity`, `LocateNearestUnit`,
  `ObtainDistance`, `MergeCoordinates`, `ObtainDistanceFromOffsets`, `ObtainPlayerPopulation` (reguły `MapRules.Distance`,
  `DoubledDistance`, `PopulationRules`, z testami względem oryginalnych wyrażeń).
- Jednostki przechowywane w mieście i powrót samolotów (2026-10-12): `KeepUnitsInCityList`, `ConstructUnitsFromCityList`,
  `LocateNearestFriendlyCity`. Golden master nie wykonuje `LocateNearestFriendlyCity` (samolot wracający do miasta lub
  lotniskowca), porównana z oryginałem ręcznie.
- Miganie aktywnej jednostki (2026-10-12): `ChoreographUnitMove` (jednostka na mapie, kropka na minimapie, ramka kursora;
  ta sama kolejność wywołań `KeyAvailable`, od której zależy klawiatura testów).
- Przewijanie widoku mapy (2026-10-12): `PanMapToPosition`.
- Cuda świata (2026-10-12): `ObtainWonderCityID`, `PlayerEnjoysWonderEffect` (+ `IsWonderObsolete`, tablica `0x10fe`).
- `PlayerTurn` (2026-10-12): `ObtainTileMoveCost`, `RepaintUnitIfVisible`, `RepaintTileIfVisible`.
- Kursor myszy (2026-10-12): `DisplayMouseCursorFully`, `ConcealMouseCursorToSavedLevel` (`GameStartup`).

## Następny krok

`ShiftUnit` w `UnitManagement` (ok. 770 linii, ruch jednostki) – rozbić na mniejsze części i przepisywać po kolei.
Potem `VisitHut`, `ChoreographUnitCombat`, `DisplayBattleScreen`. Najmniejsze metody w stylu asm można znaleźć skryptem,
który szuka `goto L`, `EnterFrame`, `ScaleIndex` itd. i sortuje po liczbie wywołań w kanale `call.` baseline.

## Błędy przetłumaczonego kodu zachowane celowo (baseline je ma, naprawić na końcu)

Kod C# przetłumaczony z asm ma miejscami inne zachowanie niż DOS-owy CIV.EXE. Testy porównują z tłumaczeniem, więc na razie
te błędy są odtwarzane jawnie:

- `VisitEachUnitInStack` zostawia SI = `0x600 * gracz` (tłumaczenie nie zachowywało SI). `AIPlayer` (L2947, ok. linii 4330)
  trzyma w SI sumę ataku stosu, woła `TallyUnitsByDomainInStack(…, 1)` i dzieli przez SI, więc dzieli przez `0x600 * gracz`
  zamiast przez atak. Po naprawie: AI inaczej ocenia ataki (zmiana baseline).
- `LocateNearestFriendlyCity`: bez własnego miasta i lotniskowca cel kroku to niezainicjowane zmienne lokalne (śmieci ze stosu).
  Odtworzone przez odczyt ramki stosu. Po naprawie: np. kierunek 0 (brak ruchu).

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
