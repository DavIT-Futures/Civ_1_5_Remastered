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

Testy różnicowe (`Civilization1Remaster.Tests/Differential`, README tam) porównują z oryginalnym kodem wszystkie przepisane
funkcje, których golden master nie wykonuje (albo nie wszystkie gałęzie): `VisitHut`, `ShiftUnit`, `DisplayBattleScreen`,
`LocateNearestFriendlyCity`, `EliminateSeaUnit`, `RestoreUnitOrders`, `AttachSmog`, `RenderScoreIcon`, `BlendTerrainTile`,
`ObtainTerrainTypeFromScreen`, `LocateNearestCityOfPlayer`, `ObtainSpaceshipPartValue`, `WipeSpaceshipContent`,
`AssignReplayCityContent(Alt)`, `RenewCityScreen`, `DisplayKingMessage`, `FreshWorldChoices` (2026-10-12; każdy test
sprawdza, że jego scenariusze trafiają wszystkie gałęzie, i łapie celowo wprowadzony błąd).

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
- Ruch jednostki (2026-10-12): `ShiftUnit` rozbite na `RevealSurroundings` (człowiek widzi pola wokół, 2 pola dalej według
  zasięgu wzroku typu jednostki, `UnitKindTable.Sight`), `MeetNearTile` / `MeetNearCity` / `MeetNearUnit` / `ConfrontNearUnit`
  (sąsiednie pola: oddanie jednostek przechowywanych w mieście, wzajemne zobaczenie, pobudka stosu, kasowanie rozkazów „go to”,
  propozycja pokoju, `SendUnitToStrategicLocation`), `MeetFarTile` / `SpotFarUnit` / `SpottedByFarUnit` (pola 2 dalej)
  i `RepaintRingTile`. Ramka stosu zostaje (patrz błędy zachowane celowo).
- Reszta `UnitManagement` (2026-10-12): `VisitHut` (zaawansowane plemię, złoto, zwoje, najemnicy, barbarzyńcy; golden master
  odwiedza wioskę tylko raz, pozostałe wyniki porównane z oryginałem ręcznie), `ChoreographUnitCombat` (animacja ataku),
  `DisplayBattleScreen` (lista jednostek stosu do obudzenia; golden master jej nie otwiera, porównana ręcznie, z bilansem
  pokazywania i ukrywania kursora myszy), `RenderBusyIndicator` (w rzeczywistości ramka ekranu z kawałków 8 x 8).
  W `UnitManagement` nie ma już kodu w stylu asm (zostały tylko celowo zachowane ramki stosu).
- Małe funkcje z kilku modułów (2026-10-12): `ObtainCityLabel` (`TradeAndConquest`), `LocateNearestCityOfPlayer`
  (`GovernmentAndDiplomacy`, w rzeczywistości odległość do najbliższego miasta z pałacem), `DisplayDialog`, `RepaintMainScreen`,
  `ObtainPalaceScoreForLevel` (`TurnEvents`), `RenderFrame`, `AttachSmog`, `RenderMiniMapDot` (`CityManagement`),
  `ObtainTerrainTypeFromScreen`, `BlendTerrainTile` (`GameInitAndIntro`), `RenderScoreIcon` (`CityFoundingAndScore`),
  `FreeStrategicLocation`, `AffixStrategicLocation`, `DisableStrategicLocation` (`AIPlayer`). Golden master nie wykonuje
  `AttachSmog`, `RenderScoreIcon`, `BlendTerrainTile`, `LocateNearestCityOfPlayer` i `ObtainTerrainTypeFromScreen`
  (porównane z oryginałem ręcznie).
- Kolejne 6 modułów (2026-10-12): `RenewCityScreen` (`ScienceAndAdvisors`), `RefreshCityProduction` (`AIPlayer`),
  `AwaitForKeyPressOrMouseClick` (`TradeAndConquest`), `AwaitForKey` (`PlayerTurn`), `FreshWorldChoices` (`GameStartup`),
  `ShrinkTextToWidth` (`TextBlockRenderer`). Golden master nie wykonuje `RenewCityScreen` i `FreshWorldChoices`
  (porównane ręcznie).
- Następne 6 modułów (2026-10-12): `ObtainSpaceshipPartValue` (`SpaceshipScreen`), `AssignReplayCityContent`,
  `AssignReplayCityContentAlt` (`GameReplay`), `RenderCityResourceBar` (`CityView`), `ComposeProductionOption`
  (`ScienceAndAdvisors`), `DisplayKingMessage` (`MeetWithKing`), `WipeSpaceshipContent` (`StartGameMenu`). Golden master
  wykonuje tylko `RenderCityResourceBar` i `ComposeProductionOption`, reszta porównana ręcznie.
- Kolejne 7 modułów (2026-10-12): `ObtainFirstPrerequisite`, `ObtainSecondPrerequisite`, `ObtainTechnologyCategory`
  (`Civilopedia`), `ObtainDistanceToReplayCity` (`GameReplay`, ramka stosu zostaje: bez miasta bliżej niż 999 oryginał zwraca
  niezainicjowaną zmienną lokalną), `ActivateHueCycle`, `DeactivateHueCycle` (`SystemServices`), `WipeHallOfFame` (`HallOfFame`),
  `PullAndAssignColors` (`CommonUtilities`), `RenderCityPicture` (`CityView`), `RenderPalacePart` (`Palace`). Wszystkie z testami
  różnicowymi. `ReflowText` odłożone: używa bufora tekstu w ramce stosu.
- Kolejne 7 modułów (2026-10-12): `PickNewCityProduction` (`AIPlayer`), `ObtainReplayPlayerLabel` (`GameReplay`),
  `RenderCityPictureRegion`, `SeizeCityScenery` (`CityView`), `RenderTutorialPointer` (`MainIntro`), `ReleaseKingResources`,
  `DisplayKingDialog` (`MeetWithKing`), `DisplayCivilopediaMenu` (`GameMenus`), `ImportGovernmentGraphics` (`StartGameMenu`).
  `ObtainReplayPlayerLabel` i `ImportGovernmentGraphics` zostawiają ramkę stosu jako bufor tekstu. Wszystkie z testami różnicowymi.
- Mysz i 5 kolejnych modułów (2026-10-12): `RenderMouseCursor`, `BlankMouseCursor`, `AssignMousePosition` (`SystemServices`),
  `DisplayMouseCursor` (`GameStartup`), `DisplayFoundCityMessage` (`NewCityScreen`), `DisplayWhichArmy` (`ArmySelection`),
  `VerifyDrive` (`GameLoadAndSave`), `ReflowText` (`TextBlockRenderer`), `DisplayCivilopedia` (`Civilopedia`). `VerifyDrive`
  (zapytanie do BIOS) i `ReflowText` (reszta tekstu) zostawiają ramkę stosu jako bufor. Golden master wykonuje tylko
  `DisplayMouseCursor` i `ReflowText`, wszystkie z testami różnicowymi (`BatchNineDifferentialTests`). Uwaga: stan myszy
  (0x586e - 0x5873) jest poza migawką (`GameSnapshot.IgnoredDataRanges`), test go podaje w `Watched`. Gałąź „dysk gotowy”
  `VerifyDrive` wymaga napędu A: lub B:, sprawdzona raz z tymczasową podróbką napędu A: w `BiosDiskCall`.
- Kolejne 4 moduły (2026-10-12): `ImportHallOfFame`, `StoreHallOfFame` (`HallOfFame`), `DisplayCityFoundedMessage`
  (`NewCityScreen`), `LogAudience` (`MeetWithKing`), `ComputeCityPlunder` (`TradeAndConquest`, dzielenie słów ze znakiem przez
  nowe `CPU.SignedQuotient`, z tymi samymi wyjątkami co `DivideSignedWord`; test `ProcessorVariableTests`). Testy różnicowe
  w `BatchTenDifferentialTests`. Wnioski: gdy oryginał rzuca wyjątek, zostawia ramkę i rejestry (gra i tak staje), więc test
  przywraca SP/BP/SI/DI po wyjątku oryginału i nie porównuje AX; pusta nazwa w `DisplayCityFoundedMessage` (odczyt bajtu przed
  tekstem) jest nieosiągalna, bo `PullTextInput` zostawia co najmniej spację.
- Kolejne 5 modułów (2026-10-12): `RepaintMapTile` (`MapManagement`),
  `ChooseNationToView` (`SpaceshipScreen`), `RenderReplayPreamble` (`GameReplay`), `DisplayCivilizationScenery` (`TurnEvents`,
  ramka stosu zostaje jako bufor nazwy pliku), `EliminateSpaceship` (`TradeAndConquest`). Testy w `BatchElevenDifferentialTests`;
  migawka nie widzi plików, więc test zapisu powtórki kopiuje treść pliku do segmentu danych (0xbc00).
- `ReimposeStrategicLocations` (`AIPlayer`, 2026-10-12, po przeniesieniu rejestrów przerwań poza stos, patrz „Śmieci na stosie”),
  test w `BatchElevenDifferentialTests`.
- Kolejne 5 modułów (2026-10-12): `DisplayNewCityLocation` (`NewCityScreen`), `ImportSpaceScenery` (`SpaceshipScreen`, ramka
  zostaje jako bufor nazwy pliku), `DisplayHelpPopupForHuman` (`InGameHelp`), `StoreGame` (`GameLoadAndSave`; test porównuje
  zapisane pliki .SVE/.MAP/.HP przez hasz w segmencie danych, tylko poziomy 0 - 4: wskaźnik nazwy poziomu 5 trafia w śmieci),
  `RenderIcon` (`Civilopedia`). Testy w `BatchTwelveDifferentialTests`.
- Kolejne 5 modułów (2026-10-12): `DisplayHelpPopup` (`InGameHelp`), `FireSpaceship` (`SpaceshipScreen`),
  `RenderReportPreamble` (`AdvisorReports`), `DisplayCityLabelPrompt` (`NewCityScreen`), `AffirmBreakTreaty` (`UnitCombat`).
  Testy w `BatchThirteenDifferentialTests`. Przy okazji: ślad wywołań testów różnicowych zawiera teraz hasze ekranów
  z chwili każdego naciśnięcia klawisza (okna znikające przed końcem funkcji), a `FloodBox` loguje swoje argumenty
  szesnastkowo (wcześniej `{style:4}` wypisywało zawsze „4”, więc kolor nie był porównywany).
- Kolejne 4 moduły (2026-10-12): `EnactNuclearFallout` (`UnitCombat`), `SwapTechnology` (`GovernmentAndDiplomacy`, wynik
  w AX), `RenderSpaceScenery` (`SpaceshipScreen`), `ManageMenuBar` (`GameMenus`). Testy w `BatchFourteenDifferentialTests`.
  Uwaga: liczniki taktów cykli kolorów (segment 0x35cf, co 0x66 bajtów od 0x1c) odlicza przerwanie zegara, więc zależą od
  liczby dostępów do pamięci; testy porównują cykle bez nich (`HueCyclesWithoutCounters`).
- Kolejne 4 moduły (2026-10-12): `ObtainDistanceToCapital` (`CivilizationSchism`, w rzeczywistości najbardziej centralne
  miasto gracza), `RenderStarField` (`SpaceshipScreen`), `ProduceCloudLayer` (`GameInitAndIntro`), `DisplayConquerorScreen`
  (`GameReplay`, ramka stosu zostaje: wołana funkcja zostawia zmieniony BP, oryginał kończy z takim stosem). Testy
  w `BatchFifteenDifferentialTests`.
- Kolejne 4 moduły (2026-10-12): `VerifyCoastalTile` (`PlayerTurn`), `RepaintUnitTile` (`MapManagement`),
  `DisplayRevolutionScreen` (`StartGameMenu`), `DisplayLabelInputBox` (`NewCityScreen`, ramka stosu zostaje jako bufor
  nazwy do porównania). Testy w `BatchSixteenDifferentialTests` (stosy na morzu budowane przez scenariusze). Uwaga dla testów:
  `make` scenariusza nie może wołać funkcji gry z dziennikiem na `pair.Original` (zmienia liczniki `call.` tylko jednej gry);
  do odczytu mapy `Chart.Terrain`.
- Kolejne 4 moduły (2026-10-12): `ReshuffleCitizens` (`CityManagement`), `AttachStrategicLocation` (`AIPlayer`),
  `LocateBestGuardian` (`PlayerTurn`, w rzeczywistości następna jednostka stosu do aktywowania), `ArrangeTextRow`
  (`TextBlockRenderer`). Testy w `BatchSeventeenDifferentialTests`.
- Kolejne 4 moduły (2026-10-12): `RenderTextInputField` (`NewCityScreen`), `QueryForDisk` (`GameStartup`, ramka zostaje jako
  bufor komunikatu), `ExplodeNuclearWeapon` (`UnitCombat`), `DisplayUnitInfo` (`ArmySelection`). Testy w
  `BatchEighteenDifferentialTests`. `ExplodeNuclearWeapon` zostawia ramkę, zapis SI i SI = 2 * kierunek w pętli: wołana funkcja
  kopiuje SI do DI, który wraca do wywołującego (rejestry zachowane jak w oryginale, bez tego DI się różni).
- Kolejne 2 moduły (2026-10-12): `DisplayWorldMenu`, `DisplayAdvisorMenu` (`GameMenus`, wspólne `RestoreMenuScreen`),
  `RenderScoreListRecord` (`CivilizationInfo`). Testy w `BatchNineteenDifferentialTests`: pozycję, od której startuje menu,
  ustawia 0x2f9a, więc skrypt klawiszy to samo Enter. Ślad wywołań maskuje teraz adresy stosu także zapisane dziesiętnie
  (np. `RenderCensusLine`).
- Kolejne 4 moduły (2026-10-13): `RenderTextChunk` (`TextBlockRenderer`), `RenderMiniMap` (`CityManagement`),
  `ImportKingScenery` (`MeetWithKing`, ramka zostaje jako bufor nazw plików, z ostatnią wartością licznika pętli),
  `MayAIEnterTile` (`AIPlayer`, w rzeczywistości kierunek pola oceanu do wyładunku z największą liczbą pustych pól lądu wokół).
  Testy w `BatchTwentyDifferentialTests`. Dane spoza gry rzucają wyjątek w obu wersjach, a rejestry po wyjątku się różnią
  (oryginał zostawia ramkę): tekst od wiersza > 192 rysuje się poza ekranem, ustrój 6 z technologią 20 to brakujący `back3m.pic`.

## Następny krok

Dalej małe metody: `python Civilization1Remaster.Tests/Differential/Reference/find_asm_methods.py Civilization1Remaster.Core 60`
(metody z co najmniej 2 liniami `goto L` / `FlagSet` / `Compare…` / `SumWord` itd., od najkrótszej). Kandydaci:
`DisplayInterfaceTutorial` (`MainIntro`), `PostCityDefenders` (`AIPlayer`),
`RenderExplosion` (`DiplomatActions`, kandydat ze „Śmieci na stosie”), `RenderReplayMapTile`
(`GameReplay`), `RenderPlayerComparisonChart` (`AdvisorReports`),
`EliminateCivilization` (`StartGameMenu`), `AttachHallOfFameRecord` (`HallOfFame`), `DisplayStoreGameDialog` (`GameLoadAndSave`),
reszta generatora mapy w `GameInitAndIntro`. Funkcje niskopoziomowe `SystemServices` (`DeactivateMouse`, `PointerEvent`,
`RenderCursorToScreen`, `RecoverScreenUnderCursor`, kolory i zegar: `PrepareTimer`, `RefreshHueCycling`, `AssignHueCycleSpan`,
`ColorsFadeStep`, `FadeColors`, `FloodColors`, `ImportColorsFromBuffer`) i `SnapHeapPointer` (`VGADriver`) na koniec: działają
w przerwaniach albo na rejestrach. Stan 2026-10-13: 140 metod w stylu asm, ok. 58 tys. z 74 tys. linii `Civilization1Remaster.Core`. Najwięcej `goto` mają `CityManagement` (ok. 1540), `PlayerTurn` (1180) i `AIPlayer` (870).

## Błędy przetłumaczonego kodu zachowane celowo (baseline je ma, naprawić na końcu)

Kod C# przetłumaczony z asm ma miejscami inne zachowanie niż DOS-owy CIV.EXE. Testy porównują z tłumaczeniem, więc na razie
te błędy są odtwarzane jawnie:

- `VisitEachUnitInStack` zostawia SI = `0x600 * gracz` (tłumaczenie nie zachowywało SI). `AIPlayer` (L2947, ok. linii 4330)
  trzyma w SI sumę ataku stosu, woła `TallyUnitsByDomainInStack(…, 1)` i dzieli przez SI, więc dzieli przez `0x600 * gracz`
  zamiast przez atak. Po naprawie: AI inaczej ocenia ataki (zmiana baseline).
- `LocateNearestFriendlyCity`: bez własnego miasta i lotniskowca cel kroku to niezainicjowane zmienne lokalne (śmieci ze stosu).
  Odtworzone przez odczyt ramki stosu. Po naprawie: np. kierunek 0 (brak ruchu).
- `ShiftAIUnit` (`AIPlayer`, ok. linii 2143) na jednej ścieżce czyta zmienną lokalną 0xc (indeks miasta) przed jej ustawieniem.
  `HandlePlayerTurn` woła `ShiftUnit` i `ShiftAIUnit` z tej samej głębokości stosu, więc dostaje typ terenu zostawiony przez
  `ShiftUnit` (bez tego `IndexOutOfRangeException` w `Towns`). Dlatego `ShiftUnit` zachowuje `EnterFrame(0x16)` i na końcu
  zapisuje w ramce końcowe wartości swoich zmiennych lokalnych (`ShiftUnitFrame`). Po naprawie `ShiftAIUnit` można usunąć ramkę.

## Śmieci na stosie (zmienne lokalne czytane przed zapisem)

Kilka przetłumaczonych funkcji czyta zmienną lokalną, zanim ją ustawi, czyli to, co zostało pod SP (`ShiftAIUnit` 0xc,
`LocateNearestFriendlyCity` 0x14/0x16, `ObtainDistanceToReplayCity`). Do 2026-10-12 trafiały tam też rejestry odkładane przez
przerwanie zegara, którego moment w testach zależy od liczby dostępów do pamięci, więc przepisana `ReimposeStrategicLocations`
(mniej dostępów) wywracała AUTO07 w `ShiftAIUnit`. Od tego dnia przerwania (zegar i mysz) trzymają rejestry w zmiennych C#
(`baseline-interrupt-registers.patch`, baseline'y wygenerowane od nowa z oryginału, zmienił się tylko nagłówek), więc śmieci
zależą już tylko od kodu na tej samej głębokości. Nadal: usunięta ramka może zmienić to, co przeczyta taka funkcja (golden master
to wyłapie, testy różnicowe nie porównują stosu pod SP). Kandydaci ze skanu „czytana, nigdy nie zapisana” (część to tablice
i bufory zapisywane przez adres, do sprawdzenia przy przepisywaniu): `RenderCitizenIcons` 0x2, `ExamineCity` 0x8,
`RenderExplosion` 0x38, `DisplayTravellersReport` 0x2, `RenderCityScenery` 0x16, `RenderCitySceneryForPlayer/ForHuman` 0x20.

## Poprawki, które zostają (chronią przed crashami, baseline je ma)

- `ChartSightGrid` zamiast `sbyte[,]` (indeksy poza mapą przy biegunach).
- Sprawdzanie zakresu `MissingTroops` / `LiveTroops` w `EliminateUnit`.

## Zasady dla następnych refaktorów (wnioski)

1. Stan gry na żywo: miasta i gracze w `GameState`, ale **jednostki w pamięci emulatora** (`0x81d4`), nie w `Troops`. Do jednostek służy `UnitMemory`, do mapy `MapLayers`.
2. Zachować kontrakt rejestrów: wartość zwracana w AX (`RegAcc`), zachowane SI/DI/BP, jeśli wołający na nich polega.
3. Usuwając `EnterFrame` / `LeaveFrame`, usunąć wszystkie użycia zmiennych lokalnych i epilog. Uwaga: inne przetłumaczone
   funkcje czytają czasem niezainicjowane zmienne lokalne, czyli to, co na tej głębokości stosu zostawiła poprzednia funkcja
   (`ShiftUnit` → `ShiftAIUnit`). Crash albo rozjazd po usunięciu ramki = ramkę trzeba zostawić z tymi samymi wartościami.
4. Po każdym kroku: `dotnet test` (golden master ok. 2,5 min). Czerwony test = zmiana zachowania. Gdy `dotnet test` mówi
   „Uruchomiono zero testów”, uruchomić `Civilization1Remaster.Tests/bin/Debug/net10.0/Civilization1Remaster.Tests.exe`.
5. Funkcja, której golden master nie wykonuje (albo nie wszystkie jej gałęzie), dostaje test różnicowy w tym samym
   commicie co przepisanie, ze sprawdzeniem, że scenariusze trafiają każdą gałąź, i z próbą celowego błędu.

## Jak pracować (dla nowej sesji)

Start: przeczytać ten plik, `Civilization1Remaster.Tests/GoldenMaster/README.md` i `Civilization1Remaster.Tests/Differential/README.md`,
potem wziąć porcję z „Następny krok” (zwykle po 1–2 funkcje z ok. 7 modułów).

- Wąskim gardłem są tokeny, nie czas: po każdej zmianie pełny zestaw testów (ok. 7 min), ale czytać tylko przefiltrowany wynik:
  `dotnet build Civilization1Remaster.Tests -v q -nologo` i
  `Civilization1Remaster.Tests/bin/Debug/net10.0/Civilization1Remaster.Tests.exe 2>&1 | grep -E "\[FAIL\]|Total:"`
  (`dotnet test` mówi „zero testów”). Powtarzalny kod generować skryptami (`extract_reference.py`).
- Narzędzia w `Civilization1Remaster.Tests/Differential/Reference`: `find_asm_methods.py` (kandydaci), `extract_reference.py`
  (kopia oryginału, właściwość silnika np. `CityOps`, `MapOps`, `Bot`, `TurnLogic`, `RocketScreen`, `MenuSystem` - patrz
  `GameEngine.cs`), `replace_methods.py <plik .cs> <plik z nowymi metodami>` (podmienia metody po nazwie, zachowuje końce linii
  i BOM; nowej metody pomocniczej nie dodaje - wstawić osobno), `mutate.py <plik> <stary> <nowy>` (mutacja, tekst musi być
  jednoznaczny). Nowe klasy referencyjne dopisać do `Reference/ReferenceClasses.cs`.
- Pułapki testów: `make` nie może wołać funkcji gry z dziennikiem na `pair.Original` (liczniki `call.` tylko jednej gry), funkcja
  gałęzi (`Branch`) nie może zmieniać gry (zmienia tylko grę przepisaną); stan zmieniany w `Prepare` zostaje na następne
  scenariusze (w obu grach). Pliki i stan myszy porównać przez segment danych / `Watched`.
- Każda przepisana funkcja: test różnicowy (wzory: `Differential/BatchSevenDifferentialTests.cs`, `BatchEightDifferentialTests.cs`),
  lista gałęzi do trafienia, `Watched` dla tabel spoza segmentu danych 0x3b01, kontrola mutacjami z pracą zabezpieczoną patchem
  (`git diff -- Civilization1Remaster.Core > batch.patch`, po mutacjach `git checkout -- Civilization1Remaster.Core && git apply batch.patch`).
- Wiersz `OpenScope` przepisanej funkcji identyczny z oryginałem (ślad wywołań w testach różnicowych porównuje argumenty).
- Długie heredoki w Bash potrafią się nie sparsować: kod zapisywać narzędziem Write do pliku, metody podmieniać skryptem Pythona
  (od komentarza `///` metody do zamykającego `\t\t}` po `CloseScope`).
- Na koniec: ten plik zaktualizowany, commit na main (z `Co-Authored-By`), `dotnet build Civilization1Remaster.sln`
  (gra: `Civilization1Remaster.Desktop/bin/Debug/net10.0/Civilization1Remaster.Desktop.exe`). Gdy build zgłasza zablokowane
  pliki, gra jest uruchomiona: poprosić użytkownika o jej zamknięcie, nie zabijać procesu.

## Zmiany hosta (nie dotyczą logiki gry, zostają)

Okno Avalonia: ustawienia skali i pełnego ekranu, skróty klawiszowe osadników, potwierdzenie zamknięcia, logowanie crashy.
