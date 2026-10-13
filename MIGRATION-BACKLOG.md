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
- Kolejne 4 moduły (2026-10-13): `PostCityDefenders` (`AIPlayer`, w rzeczywistości wysyła do miasta załadowane transportowce
  AI na morzu; `AIPlayer` ma teraz `UnitMemory`), `RenderReplayMapTile` (`GameReplay`, ramka zostaje: woła
  `ObtainDistanceToReplayCity`, która czyta śmieci spod stosu, więc musi być wołana na tej samej głębokości),
  `RenderPlayerComparisonChart` (`AdvisorReports`, raport straconych jednostek), `DisplayInterfaceTutorial` (`MainIntro`, SI = 2 *
  kierunek przy wskaźnikach jak w oryginale: `DisplayMenu` zostawia w DI wartość z SI, DI wraca do wołającego). Testy w
  `BatchTwentyOneDifferentialTests`; raport strat z więcej niż ok. 23 typami jednostek wychodzi poza ekran (wyjątek w obu).
- Kolejne 4 moduły (2026-10-13): `RenderUnitCountsChart` (`AdvisorReports`, raport wojskowy), `DumpReplayCities` (`GameReplay`,
  mapa z nazwami miast jako tekst do pliku powtórki; test porównuje długość i hasz pliku w segmencie danych),
  `ObtainKingSalutation` (`MeetWithKing`), `EliminateCivilization` (`StartGameMenu`, z pomocniczym `OwnsCity`). Testy w
  `BatchTwentyTwoDifferentialTests`.
- Kolejne 4 moduły (2026-10-13): `AttachHallOfFameRecord` (`HallOfFame`, zwraca teraz miejsce także w C#), `WelcomeEmissary`
  (`MeetWithKing`, z `AskToReceiveEmissary`, `ReceiveEmissary`, `LeaderRecord`), `RenderCensusLine` (`WorldMap`, wartości czytane
  z pamięci na bieżąco, bo pętla neguje je także dla człowieka), `RenderUnitOnWorldMap` (`MapManagement`; na mapie świata
  oryginał zwraca w AX 0xc * jednostka, zostawione przez adres jednostki). Testy w `BatchTwentyThreeDifferentialTests`.
  Wniosek: mutacja granicy, której nie da się osiągnąć (wiersz 16 * k + 8 nigdy nie jest 0xc0), nie jest dowodem słabego testu.
- Kolejne 3 moduły (2026-10-13): `DisplayGameFigures`, `DisplayWondersOfTheWorld` (`WorldMap`), `RenderCityTile`
  (`CityManagement`), `EnactGlobalWarming` (`GameInitAndIntro`). Testy w `BatchTwentyFourDifferentialTests`.
  `ChooseTechnologyToTake` (`TradeAndConquest`) odłożone: lista technologii w ramce stosu, adresy powrotu odkładane na stos
  przed `DisplayKingDialog` i niezainicjowana zmienna 4, gdy żadna technologia nie ma wartości > -1.
- Kolejne 3 moduły (2026-10-13): `DisplayCityStatusReport` (z `AppendProductionCost`), `DisplayAttitudeReport` (`AdvisorReports`,
  ramka zostaje: `RenderCitizenIcons` czyta śmieci spod stosu, bez ramki rysowała obywateli 2 piksele dalej),
  `DisplayTerrainTutorial` (`MainIntro`, wiadomości na początku gry), `RenderUnitSprite` (`MapManagement`, AX na końcu
  0x600 * gracz jak w oryginale). Testy w `BatchTwentyFiveDifferentialTests`.
- Kolejne 4 moduły (2026-10-13): `DisplayScienceReport` (`AdvisorReports`), `FinalizeProduction` (`ScienceAndAdvisors`,
  w rzeczywistości zniszczenie miasta; ramka i zapis SI/DI zostają, bo `VisitEachUnitInStack` zmienia SI), `DisplayTileInfo`
  (`InGameHelp`, z `DisplayImprovementHints`), `ProduceElevation` (`GameInitAndIntro`, w rzeczywistości rzeki nowej mapy).
  Testy w `BatchTwentySixDifferentialTests`. Uwaga dla testów: `DisplayStoryIntro` zapisuje zegar / 60 pod 0x6800, a zegar
  testów zależy od liczby dostępów do pamięci, więc test rzek zeruje 0x6800 po wywołaniu.
- Kolejne 4 moduły (2026-10-13): `MayCityWorkTile` (`CityManagement`, w rzeczywistości plon pola: żywność, tarcze, handel;
  ustrój gracza 0x6548 czytany tylko tam, gdzie oryginał go czyta), `RenderStatusBar` (`TurnEvents`, z `AnnouncePopulationMark`;
  ramka, zapis SI, adres powrotu przy `ObtainFreeStorage` i SI = nowy próg przed `DisplayDialog` zostają), `DisplayGameResultsScreen`
  (`HallOfFame`, z `PullRankTitle`), `BuyUnit` (`DiplomatActions`, z `OfferBribe`; ramka i zapis SI/DI zostają, bo
  `TallyUnitsByDomainInStack` zmienia SI). Testy w `BatchTwentySevenDifferentialTests`. `ComputeSpaceshipSuccess` (`SpaceshipScreen`)
  odłożone: rozmieszczanie części statku AI, splątane skoki między pętlami.
- Kolejne 4 moduły (2026-10-13): `DisplayTopCities` (`HallOfFame`, lista 5 miast w ramce stosu jak w oryginale, ramka zostaje
  też dla `RenderCitizenIcons`), `RenderCityBuildings` (`CityView`, z `CutWonderPictures`), `DisplayCivilizationGraph` (`WorldMap`,
  z `InGraph`, `HistoryScore`; nazwy graczy 1 - 8, gracz 8 czyta następną tabelę jak w oryginale), `DisplayCivilizationScoreList`
  (`CivilizationInfo`, z `AnyPlayerHasUnits`). Testy w `BatchTwentyEightDifferentialTests`.
- Kolejne 4 moduły (2026-10-13): `RenderExplosion` (`DiplomatActions`, 28 obrazków wybuchu w ramce stosu jak w oryginale, z końcową
  wartością licznika; zmienna 0x38 to pierwszy obrazek, zapisywany przez adres, nie śmieci), `DisplayStoreGameDialog`
  (`GameLoadAndSave`; „Game NOT saved” i błędy 0x681c są nieosiągalne, nic w C# nie ustawia 0x681c), `DisplayGameMenu`
  (`GameMenus`, z `ChooseOptions`), `HandleAllCities` (`ScienceAndAdvisors`, ramka zostaje: **`HandleCity` czyta śmieci spod
  stosu** – bez ramki AUTO11 rozjechał się w 4. turze, plon miasta 2; końcowe wartości zmiennych zapisane w ramce). Testy w
  `BatchTwentyNineDifferentialTests` (pliki zapisu przez `BatchTwelveDifferentialTests.ShowSavedFiles`, teraz `internal`
  z miejscem w 0xbc00). `extract_reference.py` nie kwalifikuje stałych klasy (`HandBayTally`, `GameLoadAndSave.FetchSessionAsk`):
  w kopii referencyjnej dopisać nazwę klasy / przestrzeni nazw.
- Kolejne 4 moduły (2026-10-13): `ImportGame` (`GameLoadAndSave`, ramka zostaje jako bufor nazw; `fetchingBackup` jest teraz
  publiczną właściwością `FetchingBackup`, żeby test i kopia referencyjna mogły ją ustawić), `RenderPalaceSection` (`Palace`,
  z `LeftNeighbourHigher`), `SendUnitToStrategicLocation` (`AIPlayer`, z `SendUnitOfRole`), `ImportTextFromFile`
  (`TextBlockRenderer`, ramka zostaje: linia i wpis indeksu w ramce, końcowe wartości zmiennych zapisane; bajt 0x101 to starszy
  bajt hasza 0x102, więc hasz = młodszy bajt sumy). Testy w `BatchThirtyDifferentialTests`: zapis z rokiem p.n.e. kopiowany
  w `Prepare` z AUTO00.SVE, gałęzie tekstu rozpoznawane po zmiennych zostawionych w ramce pod SP (SP - 2 - offset), wyszukiwanie
  bez indeksu na syntetycznym pliku `MADE.TXT` (pliki gry mają pusty indeks „00” i pustą linię zaraz po nim).
- Kolejne 4 moduły (2026-10-13): `ChoreographKingLikeness` (`MeetWithKing`, tabele w segmencie 0x36fa, ES zostawiony tam jak
  w oryginale), `ExamineCity` (`DiplomatActions`, w rzeczywistości śledztwo w mieście; 4 obrazki w ramce, końcowe wartości
  liczników zapisane), `FoundTradeRoute` (`TradeAndConquest`, z `RouteTrade`; kontynent miasta macierzystego czytany pod (x, x)
  jak w oryginale; SI/DI oddawane wołającemu z zmiennych C#, bo `DisplayDialog` zostawia SI w DI), `RefreshPeaceGraphContent`
  (`TurnEvents`, ranking potęgi; ramka zostaje: potęgi w ramce, a ranking bez potęgi > -1 bierze gracza z poprzedniego miejsca
  albo zmienną 0x1a z ramki; `DivideByPowerOfTwo` odtwarza `DivideAccumulatorByPowerOfTwo` także dla -32768). Testy
  w `BatchThirtyOneDifferentialTests`. Wnioski: okno `Watched` w segmencie 0x3772 nie może sięgać 0x3932 (to już segment danych
  0x3b01 ze zmiennymi zegara, które zależą od liczby dostępów do pamięci); ekran miasta po śledztwie jest nieosiągalny ze
  skryptową klawiaturą (animacja kończy się tylko klawiszem, który zostaje w buforze).
- Kolejne 4 moduły (2026-10-13): `LocateRouteStep` (`Pathfinding`, z `BlockMarked`, `CornerMatches`, `DivideByFour`; nowa
  klasa `PathfindingReference`), `AIWeighsTreaty` (`GovernmentAndDiplomacy`, z `WeighTreaty`), `QueryForStoreDrive`
  (`GameLoadAndSave`, z `AppendDriveLetter`), `ImportPalaceCastle` (`Palace`, ramka zostaje jako bufor nazwy pliku, końcowe
  wartości zmiennych zapisane). Testy w `BatchThirtyTwoDifferentialTests`. Wnioski: **obrazki wycięte przez `ScreenToPicture`
  nie są w migawce** (rejestr obrazków VGA), a `ScreenToPicture` nie ma wpisu w śladzie wywołań – zły wycinek wykrywa dopiero
  test, który rysuje obrazki na ekranie (`ShowPictures`); losowe dane dla funkcji z wczesnymi wyjściami (`AIWeighsTreaty`) muszą
  być rzadkie i realne, inaczej scenariusze nie dochodzą do końcowego rachunku. `extract_reference.py` bierze trzeci argument
  dosłownie jako właściwość silnika (np. `Routing`, `Politics`, `Audience`), nie nazwę klasy.
- Kolejne 4 moduły (2026-10-13): `SwitchGovernment` (`GovernmentAndDiplomacy`, z `RegimeKnown`; SI/DI oddawane z zmiennych
  C#), `DisplayNewspaper` (`Newspaper`, nowa klasa `NewspaperReference`; ramka zostaje jako bufor tematu „*NEWSx”; po szukaniu
  miasta SI = 0x1c * 127 jak w oryginale, bo `DisplayMenu` zostawia SI w DI, a oryginał nie odtwarza DI – bez tego test wykazał
  różnicę DI), `PullTextInput` (`NewCityScreen`, ramka zostaje jako bufor Insert), `DisplayIntelligenceReport` (`AdvisorReports`).
  Testy w `BatchThirtyThreeDifferentialTests` (klawisze rozszerzone w skrypcie jako 0 i kod skanowania, np. 0, 0x4b = w lewo;
  powtórka przez `Watched` (0x3725, 0, 0x3dc0)). Wniosek: funkcja, która zachowuje tylko SI (`PreserveSource`) i woła
  `DisplayMenu`/`DisplayDialog`, oddaje wołającemu w DI swoje SI – trzeba odtworzyć SI z oryginału.
- Kolejne 4 moduły (2026-10-13): `RenderPalace` (`TurnEvents`, mały pałac na ekranach wyników; ramka zostaje: poziomy i style
  w ramce, lewy sąsiad sekcji 0 to nigdy niezapisane słowo 0x34 ramki, prawy sąsiad sekcji 8 to styl sekcji 0; końcowe
  wartości zmiennych zapisane), `FocusMapView` (`MapManagement`), `BargainPeaceTreaty` (`GovernmentAndDiplomacy`, z `Bargain`,
  `WantsWar`, `AppendPair`), `DisplayCivilizationOverview` (`CivilizationInfo`). Testy w `BatchThirtyFourDifferentialTests`.
  Wnioski: zanegowany warunek (`L15e3` w `RenderPalace`: skok przy „<= 3” omija zerowanie) łatwo odwrócić – test różnicowy
  złapał to od razu (golden master tej funkcji nie wykonuje); mutacja ostatniego wiersza mapki w `FocusMapView` jest równoważna
  (ramka `RenderShadowBox` go zasłania).
- Kolejne 4 moduły (2026-10-13): `ComputeInciteCost` (`DiplomatActions`, w rzeczywistości bunt miasta, z `Revolt`; ramka
  zostaje: woła `HandleCity`; szukanie miasta rywala kończy się na pierwszym mieście, które się nie nadaje – zachowane),
  `SituateSpecialTerrain` (`GameInitAndIntro`, wartość miejsc na miasta nowej mapy, z `OffRiverPattern`; wagi terenów w tablicy
  C#: zakłada mapę w kolorach terenów jak przy generowaniu – inny kolor dałby teren > 23, a oryginał czytałby wtedy ramkę),
  `DisplayHallOfFame` (`HallOfFame`, z `RenderFameRecord`; ES ustawiany tam, gdzie oryginał, bo ocena czyta wynik przez ES
  zostawiony przez wołane funkcje), `DisplayEpicChronicle` (`WorldMap`, ramka zostaje: wartości w ramce, wartość barbarzyńców
  dla miar 3 i 4 nigdy nieustawiona; woła `HandleCity` i `RenderPalace`). Testy w `BatchThirtyFiveDifferentialTests`.
  Wnioski: **oryginał kontra oryginał** (obie lambdy z kopią referencyjną) szybko pokazuje artefakt testu – tu tekst dłuższy niż
  ok. 40 znaków nadpisuje w buforze 0xba06 ścieżkę CIV.EXE, inną w każdej grze testowej (krótkie nazwy w scenariuszach),
  a poziom trudności 5 rekordu Hall of Fame wskazuje tytuł poza tabelą (gra zapisuje tylko 0–4).
- Kolejne 4 moduły (2026-10-13): `RenderUnitInfo` (`PlayerTurn`, z `RenderUnitPanel`; jednostki wspierane przez miasto rysowane
  przez jednostkę 127 jak w oryginale), `DisplayOrdersMenu` (`GameMenus`, z `ChooseOrder`, `AppendChangeOfTerrain`; klawisze
  w liście C#, SI/DI oddawane z zmiennych C#), `DisplayTradeReport` (`AdvisorReports`, ramka zostaje: woła `HandleCity`, koszty
  budynków w ramce), `DisplayArmyList` (`ArmySelection`, z `RenderStrategicTile`; nieznana rola zostawia tekst poprzedniej
  jak w oryginale). Testy w `BatchThirtySixDifferentialTests`.
- Mała porcja 2 funkcji (2026-10-13): `RenderCitySceneryForPlayer`, `RenderCitySceneryForHuman` (`CityView`, parady obywateli,
  ze wspólnymi `CutParadePictures`, `ParadeInterrupted`; ramka zostaje: obrazki w ramce, końcowe wartości zmiennych zapisane;
  zmienna 0x20 z listy „Śmieci na stosie” to pierwszy obrazek zapisywany przez adres). Testy w
  `BatchThirtySevenDifferentialTests`: skryptowa klawiatura kończy paradę po dwóch klatkach, więc test przesuwa kolumny postaci
  (0x4f42) na ekran.
- Jedna funkcja (2026-10-13): `RenderCitizenIcons` (`CityManagement`, z `RenderCitizen`). Ramka zostaje: odstęp po zadowolonych
  zależy od słowa 0x2 ramki, którego funkcja nigdy nie zapisuje (prawdopodobnie miało być 0x6) – czytane jak w oryginale; test
  ustawia to słowo pod SP (SP - 4), żeby trafić obie gałęzie. Testy w `BatchThirtyEightDifferentialTests`.
- Porcja 39 (2026-10-13): `RenderCityScenery` (`CityView`, obywatele w widoku miasta), `AttachSpaceshipPart`
  (`SpaceshipScreen`, nowa część statku), `DisplayMilitaryReport` (`AdvisorReports`, raport ministra spraw zagranicznych),
  `ObtainNavigationKey` (`DialogDrawing`, kody klawiszy nawigacji jako `switch`). Ramki zostają we wszystkich czterech: obrazki
  i lista graczy wierszy są w ramce, końcowe wartości zmiennych zapisane („0x16” z listy śmieci w `RenderCityScenery` to pierwszy
  obrazek zapisywany przez adres). Testy w `BatchThirtyNineDifferentialTests`. Wnioski: okno `Watched` segmentu 0x3772 tylko do
  0x38f0 (dalej jest segment danych 0x3b01 ze słowami zegara, (0x3772, 0, 0x8000) dawało różnice zależne od liczby dostępów);
  trzymany przycisk myszy (0x5872) zapętla `AwaitForKey` w obu wersjach – kliknięcie dawać jednorazowo przez 0x5874; klawisz do
  `ReadKey` można włożyć wprost do `VideoCard.Tokens` w `Prepare`; mutacja na granicy równości dwóch odległości przeżyła
  (losowe zapełnienie nie daje remisów) – wybrana inna.
- Porcja 40 (2026-10-13): `ChoreographKingSpeaking` (`MeetWithKing`, klatka animacji władcy), `DisplayCivilizationDestroyedScreen`
  (`GameReplay`, ekran zniszczonej cywilizacji ze ścianą przywódców), `LabelLandmasses` (`GameInitAndIntro`, połączenia bloków 4 x 4
  nowej mapy), `DisplayTravellersReport` (`TurnEvents`, barbarzyńcy z morza i najazd na miasto). Ramki zostają (nazwy plików
  w ramce, końcowe wartości zmiennych zapisane; w `LabelLandmasses` zmienne ustawiane tylko na niektórych ścieżkach śledzone jako
  `null` = niezapisane). Potwierdzone śmieci: `DisplayTravellersReport` przekazuje niezapisaną zmienną 0x2 jako gracza do
  `VerifyTileOccupation` (zachowane, test ustawia słowo pod SP) i czyta ES:0x4f4 przy ES zostawionym przez wywołane funkcje.
  Testy w `BatchFortyDifferentialTests`: najazd wymaga złota gracza (szansa = łup %), więc test daje mu złoto i część miast;
  `LabelLandmasses` czyta mapę z ekranu (kolory oceanu z tabeli 0x2ba6), test rysuje losowy ląd. Mutacja pozy 2 przy kroku 6
  przeżyła (krok 6 nie kończy gestu, a przy 0x67f0 = 1 poza nie jest rysowana) – wybrana inna.
- Porcja 41 (2026-10-13): `DisplayKingScreen` (`MeetWithKing`), `DisplayWorldMap` (`WorldMap`), `HandleEndOfTurnEvents`
  (`TurnEvents`, zanieczyszczenie i cuda budowane przez rywali), `EarnTechnology` (`ScienceAndAdvisors`, nowa technologia). Ramki
  zostają (nazwa pliku w ramce, końcowe wartości zmiennych). `EarnTechnology` ustawia SI / DI przed wywołaniami jak oryginał
  (wywoływane funkcje mogą je widzieć) i zostawia odkładanie adresu powrotu wokół `DisplayKingDialog`; martwy kod wymiany
  ambasadorów (warunek `((Relations == 0) & 0x40)` zawsze fałszywy) usunięty, zostały tylko zapisy SI / DI. Wnioski:
  `PlayerKnowsTechnology` nie ustawia AX – oryginał zapisuje jej wynik (0x67ee), nie AX (pierwsza wersja zapisywała AX
  i test to złapał); `AppendText` / `CopyText` zostawiają w AX adres celu, więc test AX po nich jest zawsze prawdziwy; obrazki
  z `ScreenToPicture` test rysuje po wywołaniu (inaczej mutacja położenia twarzy przeżywa). Testy w
  `BatchFortyOneDifferentialTests`.
- Porcja 42 (2026-10-13): `ChartRoute` (`Pathfinding`, przeszukiwanie wszerz po blokach 4 x 4), `HandlePeaceTreaties`
  (`GovernmentAndDiplomacy`), `ImportPalaceGraphics` (`Palace`), `ReckonContinentSizes` (`GameInitAndIntro`, morskie połączenia
  bloków). Ramki zostają. Śmieci ze stosu potwierdzone i zachowane: `ChartRoute` porównuje zmienną 0xa, zanim ją ustawi, gdy cel
  jest w bloku jednostki (tylko przy remisie 0x63); `ImportPalaceGraphics` czyta 0x18 (najdalszy zbudowany segment, niezapisany,
  gdy segment 8 jest zbudowany) i 0x12 (ostatni lewy koniec, niezapisany bez lewego końca); `ReckonContinentSizes` nie zeruje
  0xe (morze sąsiada) przed próbkowaniem, więc sąsiad bez morza porównuje poprzednią wartość z ramki. `ChartRoute` bez kroku
  zwraca w AX wiersz celu (bajt 0x81db), nie 0. `HandlePeaceTreaties` ustawia SI / DI jak oryginał; groźba emisariusza bierze
  nazwę z 0x1992 + 16 * gracz (DI = gracz << 4, patrz błędy). Testy w `BatchFortyTwoDifferentialTests` ustawiają te słowa pod
  SP. Pełny zestaw po porcji 42.

## Następny krok

Dalej małe metody: `python Civilization1Remaster.Tests/Differential/Reference/find_asm_methods.py Civilization1Remaster.Core 60`
(metody z co najmniej 2 liniami `goto L` / `FlagSet` / `Compare…` / `SumWord` itd., od najkrótszej). Kandydaci
(`GameLoadAndSave` i `Newspaper` nie mają już metod w stylu asm): `ChooseTechnologyToTake` (`TradeAndConquest`, patrz wyżej),
`TitleLogoAndMainChoices` (`GameStartup`), `DisplayStoryIntro`, `ProduceTerrain` (`GameInitAndIntro`),
`CharacterizeStartLocation`, `DisplayTribeSelection` (`StartGameMenu`), `EstablishCity` (`CityFoundingAndScore`),
`OperateMenu`, `DisplayMenu` (`DialogDrawing`), `SeizeCity` (`TradeAndConquest`), `DiplomatArrivesAtCity` (`DiplomatActions`),
`DisplayCivilopediaIndex` (`Civilopedia`), `ShiftToward`, `CarryOutGoto` (`Pathfinding`), `DisplayCivilizationScreen` (`MainIntro`),
`ConstructCivilization`, `ConfigureFreshGame` (`StartGameMenu`), `UncoverTechnology` (`ScienceAndAdvisors`), `DisplayPalace`,
`HandleCityDisasters` (`CityFoundingAndScore`), reszta generatora mapy w `GameInitAndIntro`. Funkcje niskopoziomowe `SystemServices` (`DeactivateMouse`, `PointerEvent`,
`RenderCursorToScreen`, `RecoverScreenUnderCursor`, kolory i zegar: `PrepareTimer`, `RefreshHueCycling`, `AssignHueCycleSpan`,
`ColorsFadeStep`, `FadeColors`, `FloodColors`, `ImportColorsFromBuffer`) i `SnapHeapPointer` (`VGADriver`) na koniec: działają
w przerwaniach albo na rejestrach. Stan 2026-10-13 (po porcji 42): 57 metod w stylu asm, ok. 40,8 tys. z 66,6 tys. linii `Civilization1Remaster.Core`. Najwięcej `goto` mają `CityManagement` (ok. 1540), `PlayerTurn` (1180) i `AIPlayer` (870).

## Błędy przetłumaczonego kodu zachowane celowo (baseline je ma, naprawić na końcu)

Kod C# przetłumaczony z asm ma miejscami inne zachowanie niż DOS-owy CIV.EXE. Testy porównują z tłumaczeniem, więc na razie
te błędy są odtwarzane jawnie:

- `HandlePeaceTreaties`: groźba emisariusza (L0468) bierze nazwę gracza z `word[0x1992 + DI]`, gdzie DI = gracz << 4 (ustawione
  dla relacji), czyli spoza tablicy nazw – tekst z przypadkowego wskaźnika. Po naprawie: 0x1992 + 2 * gracz.
- `EarnTechnology`: pierwsze odkrycie technologii, która dezaktualizuje cud (tabela 0x10fe), czyta `Towns[MarvelTownIdx[cud]]`
  także dla niezbudowanego cudu (-1) albo zniszczonego miasta (128), więc rzuca `IndexOutOfRangeException` (DOS czytał pamięć
  przed tablicą). Do potwierdzenia w grze; po naprawie: sprawdzić -1 / 128 przed odczytem.
- `AttachSpaceshipPart`: gdy nie ma wolnego miejsca danego rodzaju, komponent lub moduł trafia na koniec tabeli 0x375a (wiersz
  0xff, czyli 12 bajtów przed siatką gracza), a struktura w slot 0x63 (poza tabelą); komunikat „No further … required” jest
  nieosiągalny (szukanie nigdy nie kończy się na 0xffff). Po naprawie: komunikat i bit w 0x687a + 2 * rodzaj.
- `DisplayMilitaryReport`: wiersz gracza bez kontaktu nie zapisuje swojego wpisu listy w ramce, więc klawisz 1 – 7 albo kliknięcie
  w ten wiersz czyta śmieci ze stosu; śmieci ≥ 8 dają `IndexOutOfRangeException` w `Relations` (DOS czytał pamięć). Po naprawie:
  wpis 0xffff / pominięcie wiersza.
- `VisitEachUnitInStack` zostawia SI = `0x600 * gracz` (tłumaczenie nie zachowywało SI). `AIPlayer` (L2947, ok. linii 4330)
  trzyma w SI sumę ataku stosu, woła `TallyUnitsByDomainInStack(…, 1)` i dzieli przez SI, więc dzieli przez `0x600 * gracz`
  zamiast przez atak. Po naprawie: AI inaczej ocenia ataki (zmiana baseline).
- `LocateNearestFriendlyCity`: bez własnego miasta i lotniskowca cel kroku to niezainicjowane zmienne lokalne (śmieci ze stosu).
  Odtworzone przez odczyt ramki stosu. Po naprawie: np. kierunek 0 (brak ruchu).
- `DisplayWondersOfTheWorld`: cud zniszczonego miasta (`MarvelTownIdx` = 128) daje odczyt `Towns[128]` przed sprawdzeniem 128,
  więc ekran cudów rzuca `IndexOutOfRangeException` (DOS czytał tam śmieci). Po naprawie: właściciel czytany dopiero dla miasta < 128.
- `ShiftAIUnit` (`AIPlayer`, ok. linii 2143) na jednej ścieżce czyta zmienną lokalną 0xc (indeks miasta) przed jej ustawieniem.
  `HandlePlayerTurn` woła `ShiftUnit` i `ShiftAIUnit` z tej samej głębokości stosu, więc dostaje typ terenu zostawiony przez
  `ShiftUnit` (bez tego `IndexOutOfRangeException` w `Towns`). Dlatego `ShiftUnit` zachowuje `EnterFrame(0x16)` i na końcu
  zapisuje w ramce końcowe wartości swoich zmiennych lokalnych (`ShiftUnitFrame`). Po naprawie `ShiftAIUnit` można usunąć ramkę.

- `ImportTextFromFile` (`TextBlockRenderer`): znaleziony bez indeksu temat zapisuje swoje miejsce w indeksie pod
  `((hash >> (5 + hash)) << 1` zamiast `(((hash >> 5) + hash) << 1` (pierwszeństwo operatorów w tłumaczeniu), więc indeks się nie
  uzupełnia. Do tego emulowany `fscanf("%[^
]
")` (`CRuntime.ScanStream`) zwraca -1 dla pustej linii (DOS-owy `
` w formacie
  pomija wszystkie białe znaki), więc szukanie od 0x200 kończy się na pustej linii za indeksem: w plikach gry temat bez wpisu
  w indeksie nigdy się nie znajduje. Po naprawie: zmiana plików tekstowych (zapis indeksu) i może tekstów pomocy.

## Śmieci na stosie (zmienne lokalne czytane przed zapisem)

Kilka przetłumaczonych funkcji czyta zmienną lokalną, zanim ją ustawi, czyli to, co zostało pod SP (`ShiftAIUnit` 0xc,
`LocateNearestFriendlyCity` 0x14/0x16, `ObtainDistanceToReplayCity`). Do 2026-10-12 trafiały tam też rejestry odkładane przez
przerwanie zegara, którego moment w testach zależy od liczby dostępów do pamięci, więc przepisana `ReimposeStrategicLocations`
(mniej dostępów) wywracała AUTO07 w `ShiftAIUnit`. Od tego dnia przerwania (zegar i mysz) trzymają rejestry w zmiennych C#
(`baseline-interrupt-registers.patch`, baseline'y wygenerowane od nowa z oryginału, zmienił się tylko nagłówek), więc śmieci
zależą już tylko od kodu na tej samej głębokości. Potwierdzone w golden master: `HandleCity` (`CityManagement`) – funkcja, która ją
woła, zostawia ramkę (`HandleAllCities`, `DisplayTopCities`). Nadal: usunięta ramka może zmienić to, co przeczyta taka funkcja (golden master
to wyłapie, testy różnicowe nie porównują stosu pod SP). Kandydaci ze skanu „czytana, nigdy nie zapisana” (część to tablice
i bufory zapisywane przez adres, do sprawdzenia przy przepisywaniu): `RenderCitizenIcons` 0x2 (potwierdzone, zachowane), `ExamineCity` 0x8, `DisplayTravellersReport` 0x2 (potwierdzone, zachowane), `RenderCityScenery` 0x16 (obrazek zapisywany przez adres).

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

Start: przeczytać ten plik, `CLAUDE.md`, skill `.claude/skills/migrate-batch/SKILL.md` (procedura porcji krok po kroku),
`Civilization1Remaster.Tests/GoldenMaster/README.md` i `Civilization1Remaster.Tests/Differential/README.md`, potem wziąć porcję
z „Następny krok” (zwykle 4 funkcje z różnych modułów).

- Testy w **Release** z `-parallelMode all` (2026-10-13: ok. 7 razy szybciej niż Debug, wyniki te same, golden master też):
  `dotnet build Civilization1Remaster.Tests -c Release -v q -nologo`,
  `Civilization1Remaster.Tests/bin/Release/net10.0/Civilization1Remaster.Tests.exe -parallelMode all 2>&1 | grep -E "\[FAIL\]|Total:"`
  (`dotnet test` mówi „zero testów”). Pełny zestaw ok. 5,5 min (Debug: 12–14 min); po każdej porcji wystarcza golden master + testy
  porcji (`-class "*GoldenMaster*" -class "*BatchNN*"`, ok. 30 s), pełny zestaw co 4 porcje, na koniec sesji i po zmianie wspólnego
  kodu. Testy różnicowe to ok. 88% czasu, najdłuższe: `PickNewCityProduction`, `DisplayCivilopedia`, `MayCityWorkTile`.
- Narzędzia w `Civilization1Remaster.Tests/Differential/Reference`: `find_asm_methods.py` (kandydaci), `extract_reference.py`
  (kopia oryginału; właściwość silnika: `grep -n "public <Klasa> " Civilization1Remaster.Core/GameEngine.cs`),
  `replace_methods.py <plik .cs> <plik z nowymi metodami>` (podmienia metody po nazwie, nowe pomocnicze wstawia za poprzednią),
  `check_mutations.py <mutations.json>` (wszystkie mutacje porcji z zabezpieczeniem pracy patchem, Release), `mutate.py`.
  Nowe klasy referencyjne dopisać do `Reference/ReferenceClasses.cs`.
- Pułapki testów: losować tylko w `make`, nigdy w `Prepare` (wykonuje się w obu grach); `make` nie może wołać funkcji gry
  z dziennikiem na `pair.Original` (liczniki `call.` tylko jednej gry); funkcja gałęzi (`Branch`) nie może zmieniać gry; stan
  zmieniany w `Prepare` zostaje na następne scenariusze. Pliki i stan myszy porównać przez segment danych / `Watched` / hasz w 0xbc00.
  Dane spoza zakresu gry (tekst od wiersza > 192, ustrój 6, raport z ponad ok. 20 wierszami) rzucają wyjątek w obu wersjach, ale
  oryginał zostawia ramkę, więc rejestry się różnią – ograniczać do realnych. Rzadkie gałęzie wymuszać w scenariuszach (np. małe
  miasta, wartość warstwy 2 mapy). Zegar testów tyka co 10 000 dostępów do pamięci: wartości pochodne zegara (0x6800) wyzerować.
  Mutacja nieosiągalnej wartości (wiersz 16 * k + 8 nigdy nie jest 0xc0) przeżywa – wybrać inną.
- Zachowanie rejestrów i stosu: AX zostawiony przez oryginał bywa ważny (`RenderUnitSprite`: 0x600 * gracz, `RenderUnitOnWorldMap`:
  0xc * jednostka); `DisplayMenu`/`DisplayDialog` zostawiają w DI wartość z SI – ustawić SI jak oryginał; `VisitEachUnitInStack`
  i `TallyUnits…InStack` zmieniają SI – zostawić `PreserveIndexRegisters`; funkcje czytające śmieci spod stosu (`RenderCitizenIcons`,
  `ObtainDistanceToReplayCity`) wołać na tej samej głębokości – zostawić ramkę. Wartości `GameState` czytać tylko tam, gdzie oryginał.
- Wiersz `OpenScope` przepisanej funkcji identyczny z oryginałem (ślad wywołań w testach różnicowych porównuje argumenty).
- Długie heredoki w Bash potrafią się nie sparsować: kod zapisywać narzędziem Write do pliku w scratchpadzie.
- Na koniec: ten plik zaktualizowany, commit na main (z `Co-Authored-By`), `dotnet build Civilization1Remaster.sln`
  (gra: `Civilization1Remaster.Desktop/bin/Debug/net10.0/Civilization1Remaster.Desktop.exe`). Gdy build zgłasza zablokowane
  pliki, gra jest uruchomiona: poprosić użytkownika o jej zamknięcie, nie zabijać procesu.
- Praca bez użytkownika: laptop zasypia po zamknięciu klapy (keep-awake blokuje tylko bezczynność) – potrzebny otwarty laptop na
  zasilaniu albo „Nic nie rób” dla pokrywy. W pętli nie używać `clear_session("self")` (kasuje zaplanowane wybudzenie).

## Zmiany hosta (nie dotyczą logiki gry, zostają)

Okno Avalonia: ustawienia skali i pełnego ekranu, skróty klawiszowe osadników, potwierdzenie zamknięcia, logowanie crashy.
