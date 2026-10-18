# Migracja asm → C#: lista na później

Zasada od 2026-10-12: najpierw migracja kodu emulowanego (asm-style C#) na czytelny C# **bez żadnej zmiany zachowania**.
Każdy krok musi przechodzić testy golden master (`Civilization1Remaster.Tests/GoldenMaster`, porównanie z oryginalnym
kodem gry). Usprawnienia i zmiany reguł gry wchodzą dopiero na sam koniec, gdy wszystko będzie przeniesione i przetestowane.

## Usprawnienia wycofane z gry (do zaimplementowania na końcu)

| Usprawnienie | Skąd (commit) | Co zostało w kodzie |
|---|---|---|
| ~~Szybsze leczenie~~ – **włączone 2026-10-14** na prośbę użytkownika (wyjątek od zasady): 2 HP na turę w mieście (wszystkie jednostki), lądowe poza miastem 1 HP ufortyfikowane, okręty poza miastem 1 HP przy lądzie, do 3 pól od własnego miasta, gdy Sentry albo bez ruchu w tej turze (okręty nie mogą się fortyfikować, wersja z eed8efb wymagała fortyfikacji, więc nie działała), samoloty tylko w mieście | eed8efb | `TurnEvents.MendFortifiedUnits` + `HealingRules`; stare leczenie za przełącznikiem `TurnEvents.ClassicHealing`, który `HeadlessGame` włącza, bo baseline golden master nagrano ze starym; testy `HealingRulesTests`, `HealingInGameTests` |
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
- Porcja 43, trzy duże funkcje (2026-10-13): `SeizeCity` (`TradeAndConquest`, zdobycie miasta), `DiplomatArrivesAtCity`
  (`DiplomatActions`, menu dyplomaty: ambasada, śledztwo, kradzież, sabotaż, podburzanie, audiencja), `ShiftToward`
  (`Pathfinding`, koszt drogi w oknie 16 x 16 wokół celu z pamięcią 0x6796 / 0x6798). Ramki zostają, zmienne lokalne lustrowane do
  ramki przy przypisaniu (`ShiftToward` ma ich ok. 25 na różnych ścieżkach – prościej niż śledzić „niezapisane” na końcu). SI / DI
  ustawiane przed wywołaniami jak w oryginale: `SeizeCity` woła `HandleCity`, która czyta śmieci spod stosu, a tam trafiają SI / DI
  odkładane przez wcześniej wywołane funkcje. Wnioski: sabotaż czyta budynek b przez 16-bitową maskę rozszerzoną znakiem (budynek 15
  liczy też wszystkie 16 – 31, 16 – 23 nigdy) – zachowane w `CityHasBuilding`; `ShiftToward` po wyborze lepszego kroku liczy tłok
  + odległość drugi raz (gałąź „równy koszt”), zachowane. Testy w `BatchFortyThreeDifferentialTests`: wybory menu dyplomaty przez
  skrypt strzałek w dół i Enter, losowe bity `ConditionMarker` (mutacja bitu 0x04 przeżywała, bo w zapisach go nie ma).
- Porcja 44, dwie duże funkcje (2026-10-13): `CarryOutGoto` (`Pathfinding`, krok jednostki do celu: obok celu, okno 16 x 16,
  długa droga, najtańszy sąsiad z karą za skręt), `UncoverTechnology` (`ScienceAndAdvisors`, wybór technologii AI, odkrycie
  i menu następnego badania człowieka). `UncoverTechnology` zachowuje tylko SI, a `DisplayMenu` zostawia w DI wartość SI, więc
  SI ustawiane w pętlach dokładnie jak w oryginale (DI po wyjściu zależy od SI przy ostatnim menu). Lista wyborów menu w ramce
  (ponowne menu po Civilopedii czyta te same wpisy). `AbsoluteValue` nie ma zakresu w dzienniku ani skutków ubocznych – liczone
  wprost przez `Math.Abs`. Wnioski: test `UncoverTechnology` z 100 scenariuszami zawieszał się w 82. także przy oryginale
  w obu grach (artefakt klawiatury skryptowej) – ograniczony do 80; mutacja `turn > 3` przy karze za skręt jest równoważna
  (8 - 4 = 4), wybrana inna. Testy w `BatchFortyFourDifferentialTests`.
- Porcja 45, dwie funkcje (2026-10-13): `HandleCityDisasters` (`CityFoundingAndScore`, losowe katastrofy i bunt miasta z
  przekupieniem przez zamożniejszego sąsiada), `CharacterizeStartLocation` (`StartGameMenu`, ocena pozycji startowych
  i wyrównanie szans: osadnicy, technologie). Ramki zostają (tablice kontynentów i punktów w ramce, `HandleCity` czyta spod stosu),
  SI / DI ustawiane jak w oryginale. Wnioski: bunt szuka zamożniejszego miasta tylko od miasta 0 do pierwszego zniszczonego albo
  do samego miasta (warunek pętli) – zachowane; mutacja w gałęzi żądań przeżywała, bo bunt z przewagą nieszczęśliwych był
  rzadki – test ma większe miasta, 250 scenariuszy i gałąź „demand”; mutacja z `
` w `mutations.json` nie trafia w tekst
  (lepiej mutować warunki). `DisplayPalace` odłożona: przekazuje argumenty przez stos i w gałęzi „jeden sąsiad” czyta
  nieaktualne SI / DI (adresy obrazków), trzeba odtworzyć rejestry dokładnie. Testy w `BatchFortyFiveDifferentialTests`.
- Porcja 46, jedna funkcja (2026-10-13): `EstablishCity` (`CityFoundingAndScore`, założenie miasta: wolne miejsce, nazwa
  z puli przywódcy, pole, flagi wybrzeża i rzeki, mapa sił na ekranie 2). Zachowuje tylko SI, więc SI ustawiane jak w oryginale
  (okna zostawiają je w DI). `WipeCitySquare` zrobiona publiczną (kopia referencyjna ją woła). Wnioski: przywódca 0 daje nazwę
  0xfff0 (porównanie ze znakiem, zachowane – wyjątek w obu); odwołane nazwanie zostawia nazwę zajętą. Testy w
  `BatchFortySixDifferentialTests`. Pełny zestaw po porcji 46: 596 testów zielonych.
- Porcja 47, jedna funkcja (2026-10-13): `ProduceTerrain` (`GameInitAndIntro`, numeracja kontynentów i mórz nowej mapy:
  etykiety w `ChartSight`, rozmiary w 0xdcfe / 0xdd7e, ranking 15 największych w ramce). Ramka zostaje, zmienne lustrowane
  przy przypisaniu. Wnioski: etykieta od 0x40 (przy zajętych rozmiarach) czyta „numer” spoza tablicy w ramce, czyli zmienne
  lokalne (x 0x24, y 0xa8, liczniki terenów…) zmieniane w pętli rysowania – pierwsza wersja ich nie zapisywała w tej pętli
  i test to złapał; łączenie etykiet przenosi rozmiar w każdym kroku wewnętrznej pętli (80 * wierszy razy, zachowane).
  Test: `DisplayStoryIntro` zależy od zegara testów (tyka z dostępami do pamięci), więc test zamraża opowieść (0x3b62 = 1,
  0x6802 = 0, 0x6806 = 0x7fff). `DivideCivilization` (`CivilizationSchism`, ok. 740 linii) odłożona na początek nowego
  kontekstu. Testy w `BatchFortySevenDifferentialTests`.
- Porcja 48, jedna funkcja (2026-10-13): `DivideCivilization` (`CivilizationSchism`, wojna domowa: nowa cywilizacja w wolnym
  slocie, podział kontynentów albo najdalszych miast, przejście jednostek ze stosami). Ramka zostaje. Wnioski: jednostka bez
  miasta macierzystego (0xff) wyrzuca `IndexOutOfRangeException` na `Towns[255]`, a nieznaleziona stolica na `Towns[0xffff]` –
  zachowane, z AX jak w oryginale (0x1c * indeks, wyliczane tuż przed odczytem; test to sprawdza w rejestrach); technologie
  „kopiowane” na samego gracza, druga relacja wojny w wierszu gracza (patrz błędy). Test: ponowny podział tych samych
  cywilizacji (i losowe 0xdc6a) zapętla oryginał na zawsze (najdalsze miasto nieaktualne, rozmiar 0) – tylko 5 scenariuszy,
  „brak wolnego slotu” przez licznik jednostek 1. Testy w `BatchFortyEightDifferentialTests`.
- Porcja 49, mała (2026-10-14): `DisplayScreen` i `RenderPictureToScreenAlternate` (`SystemServices`), wspólny ogon w
  `ClearPointerLatch`. Wnioski: pętla asm ustawia zatrzask wskaźnika 0x5402 na 1 i od razu zmniejsza do 0, więc jej
  `RenderBufferToScreen` nigdy się nie wykonuje – zostaje tylko zapis 0 przy narysowanym wskaźniku (0x5403). Ten sam ogon mają
  `RenderCursorToScreen` i `RecoverScreenUnderCursor` (odłożone, przerwania). Obrazek do testu z `ScreenToPicture` w `Prepare`.
  Testy w `BatchFortyNineDifferentialTests`.
- Porcja 50, mała (2026-10-14): `RecoverScreenUnderCursor` i `RenderCursorToScreen` (`SystemServices`), ten sam ogon
  `ClearPointerLatch`. Nie działają w przerwaniu (woła je tylko `RenderMouseCursor` / `BlankMouseCursor`), więc nie musiały
  czekać na koniec. Wnioski: `VGADriver.RenderBufferToScreen` nic nie rysuje – znacznik bufora `glob15d2StagingMarker` nigdy
  nie jest ustawiany – więc mutacja „bez wywołania” przeżywa (równoważna; wywołanie zostaje, bo jest w śladzie wideo).
  `RenderCursorToScreen` nie rysuje wskaźnika (robi to host). Testy w `BatchFiftyDifferentialTests`.
- Porcja 51 (2026-10-14): `ImportColorsFromBuffer`, `FloodColors` (wspólne `FadeToTargetColors`) i `FadeColors` (`SystemServices`).
  Wnioski: **liczba kroków zegara ma znaczenie**. Zegar testów (`FixedClockSpan`) liczy tylko dostępy przez `CPU.Pull…/Emit…`;
  `rep movsb` (`RepeatCopyStringByte`), stos i `Storage` ich nie liczą. Kopia palety przez `PullGlobalByte/EmitGlobalByte`
  dodała 1536 kroków, tyknięcie z aktywnym cyklem kolorów wypadało przed `PullCurrentColors` i inna paleta trafiała do
  0xbd06 (wykrył to pełny zestaw: `DisplayCivilizationDestroyedScreen` w porcji 40). Kopie jak `rep movsb` robić przez
  `processor.Storage`, a w funkcjach zależnych od zegara zachować liczbę dostępów do pamięci (zapis, odczyt i negacja 0x62
  w `FadeColors`). Test ma scenariusze z cyklem kolorów. Prędkość 0x66 < 0xe00 (mniej niż 256 kolorów na tyknięcie) jest
  spoza zakresu gry (`GaugeColorsPerformance` daje zawsze 65530): przerwanie `AssignColors` wychodzi wtedy poza kolor 255
  i rzuca wyjątek tam, gdzie wypadnie tyknięcie. Testy w `BatchFiftyOneDifferentialTests`.
- Porcja 52 (2026-10-14): `LaunchTimer`, `HaltTimer`, `PrepareTimer`, `AssignHueCycleSpan` (`SystemServices`). Wnioski:
  kalibracja `PrepareTimer` w emulatorze zawsze mierzy to samo (0xff9c do czasu 0x42, tempo 0 → wartości domyślne 0x4dae
  i 5), więc jest stałą. Działa przed `AllowClock = true`, dlatego test woła ją z wyłączonym zegarem (inna liczba dostępów
  niż w oryginale przesuwałaby tyknięcia w następnych scenariuszach). Zmienne zegara 0x42 – 0x5b porównuje `Watched`,
  rekordy cykli kolorów (segment 0x35cf) też. Rejestry jak w oryginale (AX, BX, CX, DX). Testy w `BatchFiftyTwoDifferentialTests`.
- Porcja 53 (2026-10-14): usunięty martwy kod sterty `VGADriver` (`PrepareHeap`, `SnapHeapPointer`, `ReserveFromHeap` i ich
  pola `glob89e` – `glob8b0`): nic ich nie woła (zostały z tłumaczenia sterownika). Bez testu różnicowego (nie ma czego
  porównać), golden master i pełny zestaw. Uwaga na przyszłość: `RenderBufferToScreen` też nic nie robi (znacznik
  `glob15d2StagingMarker` nigdy nie jest ustawiany), ale jest wołany – usunąć dopiero na końcu, razem ze śladem wideo.
- Porcja 54 (2026-10-15): `DisplayStoryIntro` (`GameInitAndIntro`, opowieść przy tworzeniu świata: automat stanów 0x3b62
  wołany wielokrotnie przez generator mapy), rozbita na `StoryStep`, `TellStoryLine`, `ShowStoryPicture`, `EndStory`. Ramka
  zostaje (nazwa pliku `birthN.pic` / `.pal` w bp - 0x10). Wnioski do testów: muzyka bez dźwięku przesuwa się o 1 na tyknięcie
  (`SoundDriver.stagingPlace`), więc test między wywołaniami czekającymi (AX = 1) woła `Idle()` – jak generator mapy, który
  w tym czasie pracuje. Klawiatura testów daje klawisz co drugie zapytanie i klawisze się kumulują (nikt ich nie czyta), więc
  „brak klawisza” trzeba przygotować w `Prepare` (wyczyścić kolejkę zaraz po podanym klawiszu). Odczyty pamięci w `make`
  i w funkcji gałęzi tylko przez `Processor.Storage` (`PullGlobalWord` liczy krok zegara jednej gry). W STORY.TXT nie ma linii
  o długości 20, więc mutacja granicy `> 0x13` jest równoważna. Testy w `BatchFiftyFourDifferentialTests`.
- Porcja 55 (2026-10-15): `TitleLogoAndMainChoices` (`GameStartup`, menu główne po tytule), rozbita na
  `AwaitTitleSongMeasure`, `MakeNewWorld`, `ShowMadeWorld`, `CustomizeWorld`, `ChooseWorldOption` (4 opcje świata jedną
  funkcją). Ramka zostaje: pozycja muzyki w bp - 2 to jedyny dostęp do pamięci w pętli czekania na takt, a bez dostępów zegar
  testów nie tyka i pozycja nie rośnie. Wnioski do testów: menu tytułowe i generator świata działają **raz na uruchomienie
  programu** (`RunGame`) i zakładają stan świeżego programu: licznik lądu chmur 0x67fe = 0 (nie jest zerowany; drugi świat
  dostaje od razu ~1000, więc bez lądu i wzgórz – `ProduceElevation` losuje pole wzgórza w nieskończoność), opowieść od
  początku, pusty ekran generatora i ekran mapy bez numerów kontynentów (inaczej `SituateSpecialTerrain` rzuca na
  `Landmasses`). W grze to nieosiągalne (Hall of Fame → nowa gra też działa), ale drugi świat w jednej grze testowej wisi lub
  rzuca w obu wersjach – dlatego każdy wybór na **świeżej parze gier** (`Theory`). Tylko VGA: wybór w trybie EGA kończy się
  wyjątkiem w `DisplayTribeSelection` (brak ekranu 3, porcja 56) w obu wersjach, a test uznaje to za zgodność i zostawia grę
  w połowie (przełączony dziennik itd.), więc następny scenariusz zachowuje się dziwnie. Diagnoza zawieszenia: nasłuch
  `ScopeListener` na wszystkich dziennikach (`Journal`, `PrologueJournal`, `VideoJournal`, `IrqJournal`), a dokładne
  miejsce: zegar w osobnym wątku wołający `Processor.WhenApplicationQuits()` (wyjątek ze stosem przy następnym dostępie do
  pamięci; w `DifferentialPair` wyjątek jest przechwytywany – odtworzyć sekwencję na jednej `HeadlessGame`). Pozycję
  muzyki ustawia `Audio.Prepare` + `PerformNextStep` w `Prepare`; zmienne zegara (0x42 – 0x5b) w `Watched` pokazują, jak
  długo czekało. Nazwy obrazków pod 0x1b33 i 0x1c02 to ten sam tekst (scope `ImportPictureOrColors` zapisuje nazwę), więc
  ich zamiana jest równoważna.
  Testy w `BatchFiftyFiveDifferentialTests`.
- Porcja 56 (2026-10-15): `DisplayTribeSelection` (`StartGameMenu`) – mimo nazwy wycina sprite'y gry z arkuszy do tablic
  obrazków (numery obrazków zależą od kolejności `ScreenToPicture`); rozbita na `StorePicture`, `CutVgaUnitSprites`,
  `CutEgaUnitSprites`, `CutPlayerCitySprites`. Nazwy nie zmieniono (scope w śladzie). Bez ramki. Wnioski: migawka widzi tylko
  numery obrazków, nie ich treść – test rysuje każdy wycięty obrazek w rogu ekranu 0 i zapisuje hasz pod 0xbc00 (bez tego
  mutacje współrzędnych wycinania przeżywały). **Ścieżka EGA (`CutEgaUnitSprites`) bez testu**: gry testowe działają
  w VGA bez ekranu 3, arkusz EGA rzuca wyjątek w obu wersjach (oryginał zostawia ramkę, inne SP). Sprawdzona tylko
  czytaniem; do sprawdzenia, jeśli kiedyś będzie gra EGA w testach. Testy w `BatchFiftySixDifferentialTests`.
- Porcja 57 (2026-10-15): `ConstructCivilization` (`StartGameMenu`, nowa cywilizacja: wymazanie gracza, technologie
  w toczącej się grze, lider, miejsce startu, nazwy, osadnik, usunięcie barbarzyńców w promieniu 8), rozbita na
  `RemakeCivilization`, `ShareKnownTechnologies`, `SettleCivilization`, `ChooseFreeLeader`, `ChooseStartPlace`,
  `NameCivilization`. Ramka zostaje (wołane funkcje widzą stos na tej samej głębokości), liczniki miast innych graczy na
  kontynentach (w oryginale tablica w ramce bp - 0x32) w tablicy C#. Wnioski do testów: granice trzeba wymuszać – runda
  dokładnie na granicy trudności (350 - 50 * trudność), barbarzyńcy rozstawieni gęsto na mapie (w zapisie żadnych w pobliżu
  miejsca startu); lider 8 ma tylko gracz 0 (nie dostaje lidera), więc mutacja `>= 8` → `> 8` jest równoważna.
  Odłożona `DisplayCivilizationScreen` (`MainIntro`, lądowanie na Alfa Centauri): przewijanie zależy od liczby tyknięć zegara
  na klatkę (0x5c), czyli od liczby dostępów do pamięci – wierne przepisanie wymagałoby odtworzenia każdego dostępu.
  Testy w `BatchFiftySevenDifferentialTests`.
- Porcja 58 (2026-10-15): `ConfigureFreshGame` (`StartGameMenu`, wybory nowej gry i jej start), rozbita na
  `ChooseDifficulty`, `RenderDifficultyPortrait`, `ChooseCompetition`, `WipeWorld`, `ChooseTribe`, `StartCivilizations`,
  `WelcomeHumanPlayer`. Ramka zostaje. Błąd tłumaczenia zachowany: pętla zerująca `TroopsRuined` przy tworzeniu cywilizacji
  używa licznika wcześniejszej pętli (zawsze gracz 6). Wnioski do testów: `DisplayMenu` zeruje linię domyślną 0x2f9a po
  każdym menu, więc linie wybiera skrypt klawiszy (k razy 0x5000 = strzałka w dół, potem Enter; pierwszy klawisz skryptu
  idzie przed pierwsze menu); Esc w menu plemienia daje losowe plemię. Nowa gra zakłada świeżą mapę: na mapie zapisu są
  miasta, których nowa gra już nie zna, a osadnik obok takiego pola rzuca w `ConstructUnitsFromCityList` w obu wersjach –
  `Prepare` czyści bit miasta na mapie (`MapLayers.ClearImprovements(1, …)`), każdy scenariusz na świeżej parze gier.
  Różnica tylko w SI (oryginał ustawia SI w ciele funkcji) to znak, że obie wersje rzuciły wyjątek w środku.
  Testy w `BatchFiftyEightDifferentialTests`.
- Porcja 59 (2026-10-15): `DisplayCivilopediaIndex` (`Civilopedia`, strona indeksu: alfabetyczne scalanie 5 list nazw
  w kolumny, wybór myszą albo klawiszami), rozbita na `ListCivilopediaEntries`, `FindNextCivilopediaEntry`,
  `ChooseCivilopediaEntryWithMouse`, `ChooseCivilopediaEntryWithKeys`, `ShowCivilopediaChoice`. Ramka zostaje (trzy bufory
  nazw w ramce). Test: `Theory` po skryptach klawiszy, w każdym losowe części / strony / tryb myszy. Granica „linia tuż za
  ostatnim wpisem” (zwraca 0) jest osiągalna tylko kliknięciem myszy, a testy nie mają kliknięć – mutacja `<=` → `<` tam
  przeżywa. Wpis 0 też zwraca 0 (nieodróżnialny od „brak wpisu” po AX). Testy w `BatchFiftyNineDifferentialTests`.
- Porcja 60, mała (2026-10-15): końcówka `GameEngine.PrepareMachine` (kod startowy programu: stos na końcu segmentu
  danych, granice stosu 0x5890 / 0x588c, wolna pamięć 0x588a, zmiana rozmiaru bloku programu jak DOS – CF / AX / BX, segment
  programu 0x5901, DS = ES = SS) bez arytmetyki rejestrów; rejestry na wyjściu jak w oryginale. Usunięta pętla „czyszcząca”
  segment danych: jej warunek (`idx < 0x70ec && idx > 0x70ec + 0xdff`) nigdy nie jest prawdziwy. Bez testu różnicowego
  (funkcja przygotowuje maszynę przed wczytaniem gry); sprawdza ją golden master (baseline z oryginału, każdy test ładuje
  grę przez nią) – mutacje 0x588a i 0x5901 łapie `PlayedTurnsMatchBaseline`.
- Porcja 61 (2026-10-15): `RenderSpaceship` (`SpaceshipScreen`, ekran statku: części, liczniki, masa, zaopatrzenie,
  energia, paliwo, czas lotu, szansa, rok przylotu, animacja gwiazd, potwierdzenie startu), rozbita na
  `RenderSpaceshipTitle`, `CountSpaceshipParts`, `RenderSpaceshipFigures`, `MoveSpaceshipStars`. Ramka zostaje: klawisz
  animacji (bp - 0x16) jest czytany także wtedy, gdy kliknięcie go nie ustawiło (śmieci ze stosu). Błędy tłumaczenia
  zachowane (patrz lista błędów). Wnioski do testów: próg czasu lotu (15 lat) jest w testach niewidoczny – przy losowych
  układach lot jest krótki albo szansa i tak spada do 0 (`VerifyValueSpan`), więc mutacja progu przeżywa; testowane
  mutacjami wzoru szansy i czasu. Testy w `BatchSixtyOneDifferentialTests`.
- Porcja 62 (2026-10-15): `DisplayCensus` (`WorldMap`, demografia: statystyki miast z `HandleCity(…, -1)` i pól lądu,
  13 wierszy rankingu), rozbita na `GatherCensus` (sumy w klasie `CensusTally` zamiast tablic w ramce), `RenderCensus`,
  `RenderCensusRow`, `RenderCensusFigure`. Ramka zostaje: wartości wiersza w bp - 0xa8 czyta `RenderCensusLine`, a
  `HandleCity` czyta śmieci spod stosu. Błąd tłumaczenia zachowany: wielkość rodziny wypisywana jako „x/10.x/10”.
  `replace_methods.py` nie obsługuje zagnieżdżonej klasy w pliku z metodami – wstawić ją osobno. Wnioski do testów:
  zanieczyszczenie miasta w zapisach z wczesnej gry jest zawsze ujemne (dodatnie dopiero przy dużej produkcji), więc mutacje
  jego wzoru przeżywają; sam wiersz jest obserwowany (mutacja warunku `> 0` złapana). Testy w `BatchSixtyTwoDifferentialTests`.
- Porcja 63 (2026-10-15): `DisplayReplay` (`GameReplay`, odtwarzanie historii gry: tryb z okna 0x3fb0 – 1 wolno, 2 krok
  po kroku z Esc, 3 do replay.txt, 4 tylko ekrany zagłady), rozbita na `ReplayGame`, `ReplayEvent` (13 rodzajów zdarzeń),
  `ReplayCityEvent`, `AppendReplayCityName`, `ReplayRanking`; zapisani liderzy w tablicy C# zamiast w ramce. Wnioski do
  testów: zdarzenia nadpisują się w pasku u góry, więc migawka widzi tylko ostatnie – test hashuje replay.txt (tryb 3) pod
  0xbc00; pierwszy klawisz skryptu ginie przed oknem (tryby trzeba wybierać z klawiszem „do zgubienia” na początku – bez
  tego tryby 1 i 2 się nie wykonywały, a mutacje w nich przeżywały); zmienne zegara nie nadają się do `Watched` po długim
  odtwarzaniu (inna liczba dostępów do pamięci), więc pauza trybu 1 (`AwaitTime(30)`) jest dla testu niewidoczna; tryb 2
  bez Esc w skrypcie wyczerpuje limit klawiszy („headless game is stuck”). Testy w `BatchSixtyThreeDifferentialTests`.
- Porcja 64 (2026-10-15): `HandleGameEnd` (`TurnEvents`, cała tura gry: gracze po kolei, zdarzenia końca tury, rok,
  autozapis, co 50 tur status i ranking do historii, kronika, punkty za cuda i zanieczyszczenia, pałac, koniec gry), rozbita
  na `PlayTurnOfPlayers`, `PlayTurnOfPlayer`, `FinishTurn`, `StrikeRandomCities`, `WarnOfBankruptcy`, `AdvanceYear`,
  `NoteStatusInReplay`, `ScoreWondersAndPollution`, `GrowPalace`, `ChooseGameEnd`, `EndGame`. Ramka zostaje (`HandleCity`
  przez `HandleAllCities` czyta śmieci spod stosu; ranking budowany w ramce bp - 0x14). Błędy tłumaczenia zachowane (patrz
  lista): wpis 11 historii ze starszym bajtem populacji zawsze 0, pętla katastrof z licznikiem = numer miasta + 1. Ranking
  (wpis 12) podawany od ostatniego słowa ramki – tak jest w oryginale. Wnioski do testów: ze skryptem [Enter] tura człowieka
  nigdy się nie kończy, obie wersje stają na limicie klawiszy, a że ramka zostaje w obu, rejestry się zgadzają i test
  „przechodzi” (mutacje przeżywały) – etykieta gałęzi musi potwierdzać, że funkcja doszła do końca; skrypt domyślny
  (golden master) kończy turę. `DirtyCellTally` jest w zapisach zerem – ustawiony w `Prepare`. Pięć wybranych scenariuszy
  (ok. 17 s). Testy w `BatchSixtyFourDifferentialTests`.
- Porcja 65 (2026-10-16): `ComputeSpaceshipSuccess` (`SpaceshipScreen`, AI buduje część statku), rozbita na
  `ShipArrivesFirstSoon`, `SpaceshipPlaceAddress`, `SpaceshipPlaceCell`, `BuildSpaceshipPart`; „splątane skoki” to jedna
  pętla szukania z trzema wyjściami. Ramka zostaje. Wnioski: pola tabeli 0x375a są ułożone rosnąco według rodzaju, więc
  pasujące pole zawsze leży przed pierwszym wolnym polem wyższej klasy – „rezygnacja 4 pola po pierwszym wolnym, gdy gracz
  zna technologię części” daje ten sam wynik (1, bez budowy) co dojście do końca tabeli; mutacje tej logiki przeżywają
  (równoważne dla tej tabeli). Technologia części −1 liczy się jako znana (test). Testy w `BatchSixtyFiveDifferentialTests`.
- Porcja 66, mała (2026-10-16): `OnClockBeat` (`SystemServices`, tyknięcie zegara z przerwania: licznik 0x5c, kolory,
  muzyka; przy tempie 4 co 7. tyknięcie bez muzyki) – pierwsza funkcja przerwania. Dostępy do pamięci jak w oryginale
  (od nich zależą tyknięcia zegara testów); golden master, testy zależne od zegara (porcje 51, 55) i pełny zestaw zielone.
  Krok muzyki w emulatorze zawsze zwraca 0, więc zmiany szybkości po nim są nieosiągalne w testach (zachowane).
  Testy w `BatchSixtySixDifferentialTests`.
- Porcja 67, mała (2026-10-16): `ColorsFadeStep` i `ColorsFadeStepInterrupt` (`SystemServices`, dwie identyczne kopie
  kroku przejścia kolorów) – jedna implementacja `MixFadeColours`. Kolor = górne słowo (z * ~mix + do * mix), zaokrąglone
  bitem 15 dolnego słowa (przesunięcie BP z przeniesieniem do BX, nie przesunięcie całości). Dostępy do pamięci jak
  w oryginale (zapis kolorów bezpośrednio, jak stosb), AX / BX / CX / DX na wyjściu jak w oryginale (test porcji 51 je
  porównuje). Mutacja `> 0xffff` → `> 0xfffe` równoważna. Testy w `BatchSixtySevenDifferentialTests`.
- Porcja 68 (2026-10-16): `DisplayHelpTopic` (`InGameHelp`), `ChooseTechnologyToTake` (`TradeAndConquest`, wybór technologii po
  zdobyciu miasta albo w dyplomacji), `OperateMenu` i `DisplayMenu` (`DialogDrawing`, układ i obsługa menu). W `InGameHelp`,
  `TradeAndConquest` i `DialogDrawing` nie ma już metod w stylu asm. Ramki zostają: w `DisplayHelpTopic` bufor tematu w bp - 0x18;
  w `ChooseTechnologyToTake` lista 72 technologii w bp - 0x94, zmienna 0x4 (najlepsza dla AI, niezapisana gdy żadna nie ma
  wartości > -1) i odkładanie adresu powrotu przed `DisplayKingDialog`; w `OperateMenu` tytuł doradcy w bp - 0x50 oraz
  niezdejmowane `StackWord(hue)` przed `RenderTextToScreen0` na liniach bez znacznika wyboru. Wnioski: gdy dwie funkcje z tej
  samej klasy są przepisywane razem i jedna woła drugą (`DisplayMenu` → `OperateMenu`), w kopii referencyjnej trzeba zmienić
  wywołanie na `new DialogDrawingReference(this.owner).OperateMenu(...)`, inaczej obie strony testu wołają nową wersję. Testy w
  `BatchSixtyEightDifferentialTests`.
- Porcja 69 (2026-10-16): `OpeningSequence` (`GameInitAndIntro`, generowanie świata / ładowanie `earth.mp` z opowieścią) oraz
  `SettleBattle` (`UnitCombat`, rozstrzyganie i animacja bitwy, dyplomacja po ataku, okup za wodza barbarzyńców, zniszczenie stosu
  i obrońcy miasta). W `GameInitAndIntro` i `UnitCombat` nie ma już metod w stylu asm. Ramki zostają: w `OpeningSequence` zmienne
  lokalne 0x2–0xe i wywołanie `ProduceTerrain` na tej samej głębokości stosu; w `SettleBattle` wywołania `VisitEachUnitInStack` /
  `TallyUnitsByDomainInStack` (zmieniają SI) i `DisplayDialog` (kopiuje SI do DI). Wnioski do testów: `OpeningSequence` nie zeruje
  globalnego licznika pikseli chmur `0x67fe`, więc w kolejnych scenariuszach trzeba go zerować w `Prepare` (inaczej powstaje tylko
  jedna chmura, brak wzgórz `0xc` i `ProduceElevation` losuje źródło rzeki w nieskończoność). Testy w
  `BatchSixtyNineDifferentialTests`.
- Porcja 70 (2026-10-16): `RenderMapTile` (`MapManagement`, rysowanie pojedynczego kafelka mapy świata i widoku mapy,
  wybrzeża, ujścia rzek, nakładki terenu, ulepszenia, miasta i flagi jednostek) oraz `ManageBarbarianUnit` (`AIPlayer`,
  ruchy jednostek barbarzyńców: transportowce morskie z desantem na ląd, atak na miasta i okup, pościg za najbliższym wrogim
  miastem lub jednostką, eliminacja na biegunach). W `MapManagement` nie ma już metod w stylu asm (100% czyste C#). Ramki zostają:
  w `RenderMapTile` zmienne lokalne ramki dla ekranu i ulepszeń; w `ManageBarbarianUnit` zachowanie i przywracanie rejestrów
  indeksowych (`PreserveIndexRegisters` / `RestoreIndexRegisters`) oraz ramka 0x54 ze wzorem na rejestr AX przy polarnym
  `EliminateUnit`. Wnioski do testów: `RenderMapTile` przy `0xd762 == 0` (grafika bez animacji cykli kolorów) odwołuje się do
  prostokąta pod `0x19fc` operującego na ekranie 3, dlatego w testach należy zarezerwować ekran 3 (`game.VideoCard.ReserveScreen(3)`).
  Kolor oceanu na warstwie terenu to `1` (mapowany przez tabelę `0x2ba6` na teren `0xa`). Testy w `BatchSeventyDifferentialTests`.
- Porcja 71 (2026-10-16): `DisplayPalace` (`Palace`, ekran pałacu z prezentacją graficzną, interaktywną rozbudową skrzydeł, wież,
  zamku i ogrodów) oraz `DisplayCivilopediaRecord` (`Civilopedia`, wyświetlanie szczegółów encyklopedii dla technologii, budynków/cudów,
  jednostek wojskowych, ukształtowania terenu i ogólnych pojęć cywilizacyjnych). W `Palace` i `Civilopedia` nie ma już metod w stylu asm (100% czyste C#).
  Ramki zostają: zachowanie i odtworzenie rejestrów indeksowych (`PreserveIndexRegisters` / `RestoreIndexRegisters`) oraz ramki lokalne.
  Wnioski do testów: `DisplayPalace` korzysta z `ScriptedKeyboard` do wyboru sekcji i stylów ('1' = 0x31) oraz zatwierdzania dialogów (0x0d).
  Testy w `BatchSeventyOneDifferentialTests`.
- Porcja 72 (2026-10-16, poprawiona 2026-10-17): `DisplayMainIntro` oraz `DisplayCivilizationScreen` (`MainIntro`, intro gry
  z przewijaniem napisów (credits.txt) i planety w takt muzyki, logo i wybór z menu klawiszem z intro; ekran lądowania statku
  na Alfa Centauri). W `MainIntro` nie ma już metod w stylu asm. Pierwsza wersja (inny model, „bez testów”) miała błędy:
  czekanie na muzykę w złą stronę, inny odczyt klatki statku (0xb226 zamiast 0xb23b), brak zapisu 0x32d6, mniej dostępów do
  pamięci. Przepisane od nowa wiernie: zmienne lokalne zostają w ramce i są czytane tyle razy co w oryginale (od liczby dostępów
  zależą tyknięcia zegara testów, a od nich muzyka i 0x5c), podział na `FindIntroDisks`, `DetectGraphicsDriver`,
  `ReserveIntroScreens`, `LoadIntroPictures`, `ScrollCredits`, `ScrollPlanet`, `TakeIntroChoice`, `ShowTitleLogo`, `CloseIntro`
  oraz `ImportShipPictures`, `PrepareLandingText`, `FlyShip`, `RenderShipFrame`, `CutNextTextPart`, `ShowLanding`,
  `RenderClosingText`, `CloseLanding`. Licznik klatek statku (bp - 0x208) nie jest ustawiany przed pierwszym odczytem (śmieci ze
  stosu) – zachowane. Wnioski do testów: „ChanceRoot o 1 większy” to nie dodatkowe losowanie, tylko ziarno z zegara ściennego
  (`SeedRandomizer` → `time()` w sekundach) – test ustawia `game.Libc.Clock` (nowy szew w `CRuntime`) na stały moment w obu
  grach; poprzednie maskowanie (ChanceRoot / SeedRandom po przebiegu) usunięte. Intro do końca: `InputStarved` w `Prepare`
  podmieniony tak, że zapytania o klawisz (KeyAvailable) nie dostają klawisza, a każde wywołuje `keyboard.KeyTyping`, więc ekrany
  każdej kolumny trafiają do śladu (bez tego zmiana napisów była niewidoczna); klawisz czekający w `VideoCard.Tokens` daje wybór.
  Klawisz, który przerwie intro, zostaje w buforze, więc ścieżka „przerwane napisy + logo” nie istnieje. Mutacja równoważna:
  `MusicReachedColumn` `<=` → `<` (jeden krok muzyki dłużej na kolumnę; kolumny i ekrany te same, przed logo intro czeka na
  pozycję 0xd00). Testy w `BatchSeventyTwoDifferentialTests`.
- Porcja 73 (2026-10-17): `RunGame` (`GameStartup`, wejście do gry: intro, kontrola pamięci, kursor myszy, menu, nowy świat,
  pętla tur), `ClockInterrupt`, `RefreshHueCycling`, `PointerEvent` (`SystemServices`, przerwania zegara i myszy, krok cykli
  kolorów). Zaczęta przez inny model; poprawione: porównania ze znakiem w `RunGame` (oryginał: `jle` / `jge`), zmienna 0x4
  i odkładanie adresów powrotu zostają w ramce / na stosie, w `ClockInterrupt` i `RefreshHueCycling` przywrócone podwójne odczyty
  (0x46, 0x44, licznik cyklu) i kolejność odczytów, zawijanie cyklu jak `jge` po `sub` (pozycja < krok, nie wynik < 0). Testy
  różnicowe z etykietami z efektów; `GameSnapshot` pomija zmienne zegara 0x40 – 0x6f i stan myszy 0x586e – 0x5873, a cykle
  kolorów są w segmencie 0x35cf – wszystko przez `Watched` (wcześniejszy test `ClockInterrupt` nic nie porównywał).
  `RunGame` testowany do wyjątku limitu klawiszy (`TurnKeyLimit` = 300): obie wersje grają intro, menu, nowy świat i tury, a
  ślad, stan i ekrany do tego miejsca muszą się zgadzać. Mutacja `pozycja < krok` → `<=` równoważna (przy równości wynik 0
  w obu). Testy w `BatchSeventyThreeDifferentialTests`.
- Poprawki po porcjach 68 – 72 (2026-10-17): `SettleBattle` AUTO02/0 – obie wersje rzucały ten sam
  `IndexOutOfRangeException` (scenariusz kasował wszystkie miasta, a mapa nadal je pokazywała; `RenderCityOnTile` nie znajdował
  miasta), różnił się tylko SI zostawiony przez oryginał w połowie. Scenariusz używa teraz wolnego slotu miasta; etykiety gałęzi
  z efektów (kontakt 0x20 w relacji, wywołania `EliminateUnit` / `ExplodeNuclearWeapon` / `TallyUnitsByDomainInStack`, złoto
  za wodza). „Przeżyłe” mutacje porcji 68 / 70 były wstawione w `SeizeCity` (DrawInt(2)) i `FocusMapView` (kropki miast), a
  sprawdzane testami `ChooseTechnologyToTake` / `RenderMapTile` – właściwe testy (`SeizeCity…`, `FocusMapView…`) je łapią.
  Pełny zestaw wisiał przez pierwszą wersję intro (czekanie na muzykę bez końca w testach, które wołają intro / ekran
  cywilizacji: `TitleLogoAndMainChoices`, `ConfigureFreshGame`, `RenderSpaceship`, `DisplayReplay`, …).

- Porcja 74 (2026-10-17): `OperateAITurn` (`AIPlayer`, przegląd gracza co turę: jednostki i miasta na kontynentach, bloki
  4 x 4 mapy w 0x3772:0x2438, podejście do każdego kontynentu, rząd, porzucanie przestarzałych jednostek, wysyłanie jednostek do
  miejsc strategicznych), rozbita na `CountUnits` / `CountUnit`, `CountCities` / `CountOwnCity` / `MayAimAtCity`,
  `CountUsedBlocks`, `ChooseContinentApproaches` / `ChooseContinentApproach`, `ChooseGovernment`, `DirectUnits`,
  `GiveUpObsoleteUnit`, `SendUnitToBestLocation`, `LocationFits`, `WeighLocation`, `MayTakeLocation`. Ramka zostaje i zmienne
  lokalne dostają te same wartości co w oryginale (zapis przy każdym przypisaniu, czytane są zmienne C#); tablica „wysłanych”
  jednostek miejsc strategicznych zostaje w ramce (bp - 0x44). Błąd tłumaczenia zachowany: jednostki znane w mieście gracza
  (`Unfamiliar`) przypisują (`=`, nie `+=`) siłę gracza i atak kontynentu. Pola `Assault` / `Guard` kontynentu dostają moc
  obrony / ataku (nazwy odwrotnie niż w tabeli typów jednostek) – zachowane. Wnioski do testów: funkcja etykiet gałęzi czyta
  pamięć przez `Processor.Storage` (odczyt przez `CPU.Pull…` liczy się do zegara testów tylko w jednej grze; poprawione też
  w testach 72 i 73); scenariusze wymuszają rzadkie gałęzie: jednostki z rozkazami (bity 2, 3) przy miejscach o priorytecie 2,
  bombowiec z dwoma miejscami ważonymi na remis (mnożnik 2 / 4 zmienia wybór), gracz z dokładnie 10 / 11 miastami w turze
  rządu, gracz bez miast i jednostek na końcu (eliminacja). Testy w `BatchSeventyFourDifferentialTests`.
- Porcja 75 (2026-10-17): `PickCityProduction` (`ScienceAndAdvisors`, wybór produkcji miasta: tekst menu i lista wyborów
  w ramce, ocena potrzeb miasta, punktacja wyborów, wybór komputera, doradcy, menu, Civilopedia, strony „more...”), rozbita na
  `ListProductionChoices` / `ListUnitChoices` / `ListImprovementChoices` / `ListWonderChoices`, `AssessCityNeeds`,
  `AssessCityDefence`, `ScoreProductionChoices` / `ScoreImprovementChoice` / `ScoreDiplomatChoice` / `ScoreUnitChoice`,
  `DecideProduction`, `ShowProductionAdvice`. Zmienne lokalne są właściwościami czytającymi i zapisującymi ramkę (te same
  wartości zostają pod SP, lista wyborów jest w ramce); pętla `while` zastępuje skoki do L0447 (strony, doradcy, Civilopedia).
  Zachowane: wczesne odpowiedzi (milicja 1, barbarzyńcy 3 / 7) nie przywracają `TroopTally` przeliczonego od nowa. Wnioski do
  testów: test porównuje ramkę funkcji (`Watched` od SP - 2 - 0x14a) – mutacje punktacji są widoczne tylko w najlepszych
  wynikach i liście, które tam zostają; menu w testach idzie ścieżką myszy, jeśli 0x1a3c ≠ 0 (stan przycisków z zapisu) –
  scenariusze wyłączają mysz i wpisują klawisze przez własne `InputStarved` (kolejka klawiszy scenariusza, co drugie zapytanie
  bez klawisza, limit), bo klawisze wpisane z góry do `VideoCard.Tokens` zużywa wcześniej `HandleCity`. Mutacja nie złapana:
  dolna granica wyniku karawany (`Score < 3`) – wymaga miasta z handlem ≥ 16, takiego nie ma w zapisach 1, 11, 14; próg
  stronicowania (dokładnie 31 pozycji) też niesprawdzony (mutacja zastąpiona warunkiem miasta budującego samo). Testy w
  `BatchSeventyFiveDifferentialTests`.
- Porcja 76 (2026-10-17): `LaunchAudience` (`MeetWithKing`, audiencja u przywódcy komputera: siła na kontynentach,
  broń jądrowa, cuda, wrogowie, potem wymiana technologii, sojusz, trybut, żądanie technologii, groźba, pokój i jego oferty,
  wypowiedzenie wojny, rozmowa w czasie pokoju, rada doradcy), rozbita na `WeighContinents`, `WeighNuclearWeapons`,
  `WeighEnemies`, `ExchangeTechnologies`, `AskForAlliance`, `DemandTribute`, `DemandTechnology`, `Threaten`, `OfferTreaty`
  / `OfferTechnologyForPeace` / `OfferGoldForPeace` / `OfferEverythingForPeace`, `DeclareWar`, `WelcomeFriendship`,
  `NegotiateUnderPeace` / `DemandGoldUnderPeace` / `AskToAttack`, `AdviseAboutLeader`. Zmienne lokalne w ramce (jak porcja 75).
  Błędy tłumaczenia zachowane: złoto za pokój jest odejmowane od słowa pod SI (= gracz << 4, ustawiane tam, gdzie oryginał je
  ustawia), a nie od przywódcy; przy żądaniu złota w czasie pokoju człowiek dostaje odpowiedź dialogu zamiast kwoty. SI / DI
  ustawiane tylko tam, gdzie oryginał potem z nich czyta (nazwy w „declare war”). Test porównuje ramkę, wpisuje odpowiedzi
  dialogów przez `InputStarved` i wymusza tryby: wojna z zagrożonym przywódcą (pokój, technologia, złoto, wszystko, ostrzeżenie
  doradcy przy 5 miastach), pokój z wymianą technologii (tylko dwóch graczy przy życiu, czasem zagrożony przywódca, który nie
  negocjuje). Testy w `BatchSeventySixDifferentialTests`.
- Porcja 77 (2026-10-17): pierwszy kawałek `ShiftAIUnit` (`AIPlayer`, ok. 3150 linii, ruch jednostki komputera). Wydzielone i
  przepisane: początek (barbarzyńcy → `ManageBarbarianUnit`; `PlanAirUnit`: bombowiec bez paliwa i myśliwiec bez ruchów
  czekają 0x68; `SendFighter`: najbliższy widziany bombowiec człowieka w zasięgu albo własne miasto najbliższe temu bombowcowi,
  0x20 bez miasta) i rozpoznanie otoczenia (`SurveyAIUnit`: zmienne lokalne 0x4c, 0x30, 0x36, 0x8, 0x34, 0x2a, 0x4, 0x32, 0x42,
  0x56, 0x1a, 0x2, 0x50 jak w oryginale, SI i AX na końcu też). Wszystkie skoki do tego fragmentu były wewnątrz niego; reszta
  funkcji (L0ff1 i dalej) zostaje w stylu asm na następne porcje. Zachowane: warunek „za blisko” sprawdza ostatnią zmierzoną
  odległość do bombowca (0x12), a nie najmniejszą. Test (cała `ShiftAIUnit` z kopią referencyjną, porównanie ramki) wymusza
  bombowce, myśliwce z małą liczbą ruchów, bombowiec człowieka blisko i daleko, gracza bez miast i myśliwca w mieście.
  Niesprawdzona granica: miasto dokładnie w zasięgu myśliwca (mutacja zamieniona na odwrócenie warunku). Testy w
  `BatchSeventySevenDifferentialTests`.
- Porcja 78 (2026-10-17): drugi kawałek `ShiftAIUnit` – broń jądrowa komputera (`AimNuclearWeapon`, `IsNuclearTarget`,
  `ScoreNuclearTarget`, `HasCityNear`): cel to miasto wroga (bit 0x80 bez pokoju) większe niż 4, gdy gracz nie jest dużo
  silniejszy, wróg jest ostrzeżony albo nie ma własnej broni; wynik z pól wokół (+1 wroga, −2 własne, −99 innych) i połowy
  wielkości, z własnym miastem w 16 polach; przy wyniku ≥ 10 i co najmniej dwóch broniach bomba staje na wolnym polu obok
  (odpowiedź: kierunek ^ 4), inaczej 0x20. Zachowane: dla martwego miasta albo z bitem 0 drugiego słowa „wynik” porównywany z
  najlepszym to numer właściciela (zmienna 0x3a jest najpierw właścicielem). Test (scenariusze z relacjami, siłami, bronią
  wrogów i rosnącymi miastami, porównanie ramki; etykieta „za mało broni” z wyniku 0x28 w ramce) w
  `BatchSeventyEightDifferentialTests`.
- Porcja 79 (2026-10-17): trzeci kawałek `ShiftAIUnit` – dyplomaci (z celem idą dalej, obok cudzego miasta idą do niego,
  we własnym mieście są rozwiązywani; wspólne wyjścia L12c0 / L12d7 zostają, bo skacze do nich reszta funkcji) i eksploracja
  (`ExploreAround`: kierunek z największą liczbą nieznanych pól na warstwie ekranu 2, wioski, wędrówka w pierwszej turze z
  0xd76a, oznaczenie miejsca strategicznego; `VisitAsAmbassador`: osadnicy przy mieście człowieka, który ma więcej technologii).
  L1417 (odpowiedź 0x62) zostaje jako cel z dalszej części. Wnioski do testów: wartości warstwy nieznanych pól ustawiane wprost
  `VideoCard.AssignDot(2, x + 0x50, y, …)`; wioski z `MapRules.IsHutPlace` i nieodwiedzone pole w `ChartSight`; tryb „pierwsza
  tura” zostawiał `RoundTally = 0` i 0xd76a dla kolejnych scenariuszy (fałszywe gałęzie) – każdy scenariusz ustawia 0xd76a.
  Testy w `BatchSeventyNineDifferentialTests`.
- Porcja 80 (2026-10-17): czwarty kawałek `ShiftAIUnit` – obrońca w mieście (`GuardCity`): sam albo lepszy od przywódcy
  stosu zostaje (0x66); wzywa obrońców, gdy jest ich mniej niż wielkość miasta / potrzeba + 1 (potrzeba 4, 3 z jednostką
  innego gracza obok, −1 z pałacem, +1 przy pokojowym podejściu na kontynencie); przywódca stosu fortyfikuje się, póki jednostek
  za mało; co ósma tura jednostka traci rozkazy. Błąd tłumaczenia zachowany: po `TallyUnitsByDomainInStack` (zostawia SI =
  0x600 × gracz) stan jednostki jest przywracany „pod SI”, czyli zapisywany pod 0x600 × gracz zamiast do jednostki – golden master
  to złapał (pamięć 0x0c00). Test buduje stosy 1 – 4 obrońców w mieście (pierścień `FollowingInStack`, `SituateUnitOnTile`),
  z losową wielkością, pałacem i stanem. Testy w `BatchEightyDifferentialTests`.
- Porcja 81 (2026-10-17): piąty kawałek `ShiftAIUnit` – przegląd stosu atakującego / transportowca (`SurveyStack`: role
  nieufortyfikowanych członków w 0x1e, jednostki lądowe w 0x54, przy podejściu 5 zwalnianie ufortyfikowanych ponad piątą część
  wielkości miasta 0xc). Zmienna 0xc jest czytana bez ustawienia (śmieci ze stosu) – w testach `ShiftAIUnit` (77 – 81) ramka
  dostaje w 0xc typ terenu, jak w grze po `ShiftUnit`; bez tego obie wersje rzucały ten sam wyjątek w różnych miejscach
  (różne rejestry). Sztuczne stosy budowane przez `SituateUnitOnTile` w mieście zapętlały oryginał – test bierze istniejące stosy
  z zapisów. Niezłapana mutacja: bit 0 stanu zwalnianego członka (`& 0xf7` → `& 0xf6`) – w scenariuszach bez wpływu na wynik.
  Testy w `BatchEightyOneDifferentialTests`.
- Porcja 82 (2026-10-17): szósty kawałek `ShiftAIUnit` – transportowiec z małym ładunkiem (`ChooseStagingCity`): najbliższe
  nadmorskie miasto gracza na kontynencie z siłą ataku ≥ 16 (puste miasto z atakującymi +16, przy podejściu 5 −obrona / 4,
  inaczej +16) jest celem, a przy podejściu 5 i małej odległości oznacza tam miejsca strategiczne. Mutacja równoważna:
  `approach != 0` → `!= 1` (oznaczanie zachodzi tylko przy podejściu 5). Test (transportowce z zapisów, siła i podejście na
  kontynentach) w `BatchEightyTwoDifferentialTests`.
- Porcja 83 (2026-10-17): siódmy kawałek `ShiftAIUnit` – transportowiec z ładunkiem przy lądzie (`PlanTransportLanding`):
  pole lądu obok (wolne albo własne z mniej niż 2 jednostkami, nie ulepszone pole człowieka przy pokoju) na kontynencie, gdzie
  ładunek jest potrzebny, z ≥ 5 miejscami pod miasta i z dala od biegunów; z ruchami > 3 wejście (`MayAIEnterTile`), inaczej
  rozładunek (0x75); bez pola co czwartą turę zapomina cel, bez ruchów oznacza pół drogi do celu. Mapy z zapisów nie mają lądu
  w rzędach 2 i 0x2f, więc test zamienia tam jedno pole w ląd (granice biegunów), przenosi stos do zatoczki (najlepszy kierunek 0
  z `MayAIEnterTile`) i stawia obok 2 jednostki gracza albo ulepszone pola człowieka. Wszystkie mutacje złapane. Test w
  `BatchEightyThreeDifferentialTests`.
- Porcja 84 (2026-10-17): ósmy kawałek `ShiftAIUnit` – cel jednostki bez celu (`ChooseTransportDestination`): najbliższe
  wrogie miasto (co ósme od tury, bez partnerów pod pokojem, przy silnym człowieku tylko jego), inaczej najbliższe własne
  miasto ≥ 8 pól dalej na innym kontynencie (ładunek nie pasujący do podejścia liczy się podwójnie), inaczej losowe pole lądu
  bez miast gracza. Docierają tu tylko transportowce i okręty atakujące na morzu (rola 3 na lądzie i w mieście kończy
  wcześniej), dlatego test bierze takie jednostki poza miastami, siłę ≥ 16 na kontynentach (bez lądowania), pokój ze
  wszystkimi, osadników z inną jednostką na żaglowcu i obcą jednostkę obok (0x4c). Niezłapane: `0x4a == 0x10` → `== 0xffff`
  (0x10 jest kontynentem tylko dla jednostki na lądzie, a ta tu nie dociera – w żadnym scenariuszu z zapisów); równoważna:
  SI = `p * 0x600` przed losowym polem (kod po L1f44 ustawia SI przed odczytem). Test w `BatchEightyFourDifferentialTests`.
- Porcja 85 (2026-10-17): dziewiąty kawałek `ShiftAIUnit` – `StandsOnHumanFields` (wolna jednostka przy mieście człowieka
  bez pokoju na nawadnianym / górniczym polu: 0x50) i osadnicy (`DirectSettlers` z `ImproveSettlersTile`,
  `ChooseSettlersTile`, `BuildSettlersRoad`): nawadnianie / kopalnia wg kosztu ruchu, drogi, twierdza (bit 0x40), pole przy
  małym mieście, wędrówka wg wzoru 0x3772:0x2438. DI i ES odtworzone jak w oryginale (L2520 ich nie ustawia). Test przenosi
  osadników obok własnych miast, a wolne jednostki obok miast człowieka, losuje ulepszenia pól 5 x 5, technologie 5 / 0x13 /
  0x31, bit 2 markerów, wodza, liczby miast i miejsc, wielkości miast, wzór wędrówki i obcą jednostkę obok. Wszystkie
  mutacje złapane. Test w `BatchEightyFiveDifferentialTests`.
- Porcja 86 (2026-10-17): dziesiąty kawałek `ShiftAIUnit` – `DecideBeforeFight` (L2520): jednostka z celem idzie dalej
  (atakujący tylko bez obcej jednostki obok), obrońca na lądzie fortyfikuje się przy własnym mieście albo przy wrogu, osadnicy
  na kontynencie z dość miastami budują miasto w małym mieście (0x62) albo do niego idą; inaczej start rozglądania (0x28,
  0x2c, 0x5c, kierunek zapomniany przy wrogu, milicja przy podejściu 1 liczy się jako obrońca). Atakujący na lądzie tu nie
  docierają, więc test bierze okręty atakujące (obcy statek obok), obrońców w stosie poza miastem, na morzu i 3 pola od
  miasta. Wszystkie mutacje złapane. Test w `BatchEightySixDifferentialTests`.
- Porcja 87 (2026-10-17): jedenasty kawałek `ShiftAIUnit` – koniec pętli szukania walki: `ScoreSurroundings` (bez wroga
  obok: +8 za nieoznaczone pole lądu 4 kroki dalej wg wzoru 0x2438, +2 za nieznane graczowi pola wokół, -2 za jednostki,
  wartość terenu dla osadników; przy wrogu i < 3 ruchach skalowanie udziałem atakujących w stosie; najlepszy wynik 0x28 /
  kierunek 0x2c / znacznik wroga 0x52) i `ChooseHeading` (L2f94). Pułapka: oryginał dzieli przez SI zostawione przez
  drugie `TallyUnitsByDomainInStack`, nie przez liczbę ograniczoną `VerifyValueSpan` (pierwsza wersja dzieliła przez tę
  liczbę i golden master to złapał). Wszystkie mutacje złapane. Test w `BatchEightySevenDifferentialTests`.
- Porcja 88 (2026-10-18): dwunasty kawałek `ShiftAIUnit` – ocena pola i walki w pętli szukania walki (L2857 – L2dbd):
  `ScoreTile` (pierwszy wynik pola: osadnicy 0, na statku pomijają pole bez obcej jednostki obok; niewidoczna jednostka na
  lądzie kość 3 minus obrona terenu, inaczej kość 5 i siła własnego stosu – dzielona przez SI zostawione przez
  `TallyUnitsByDomainInStack`, jak w oryginale – albo 4 x obrona terenu; samoloty kość 3; kara za odległość od kierunku
  jednostki), `WeighUnitAtTile` (wolne pole: obce miasto 999, wioska +20; własna jednostka minus obrona; obca w wojnie:
  najlepszy obrońca, `SettleBattle` x liczba jednostek / cena, x3 w mieście, x3 milicja przy podejściu 1, x2 przy słabym
  stosie, od 6 wynik x4, inaczej -999 i możliwy zakup `BuyUnit`) i `BuyUnitAtPeace`. Test: milicja (rola 1 przy podejściu 1)
  i obrońcy w stosach, okręty, osadnicy, bombowiec (zapisy nie mają samolotów: jednostka stosu staje się bombowcem z paliwem,
  myśliwiec bez celu tu nie dochodzi) z obcym samolotem obok, stos przeniesiony obok obcego miasta w wojnie, obca jednostka
  obok (status, relacje, mocny teren), własna jednostka, sztuczne obce miasto bez jednostek (usuwane w następnym scenariuszu),
  wioska. Pułapki testu: walki wcześniejszych scenariuszy zostawiają znak miasta bez miasta (`SettleBattle` rzucał
  `Towns[-1]` w obu wersjach, różnił się tylko SI) – każdy scenariusz czyści takie znaki; obca jednostka obok musi być
  jednostką komputera dla zakupu w pokoju. Mutacje: równoważna `Tally(…, 3) << 1` → `<< 2` (dzielenie przez 0x600 x gracz daje
  0); niesprawdzona granica `6 <= 0x5a` (mutacja na 7 przeżywa, odwrócenie warunku i 3 złapane). Test w
  `BatchEightyEightDifferentialTests`.
- Porcja 89 (2026-10-18): ostatni kawałek `ShiftAIUnit` – filtr kierunku (`MayAimAtDirection`, L2643 – L2857: poza
  mapą, morze dla jednostki lądowej, dyplomata przed stosem zastępuje następna jednostka stosu, wolne pole, które jednostka
  może zająć, dla jednostki lądowej przy obcej jednostce (0x4c), obca jednostka dla jednostki lądowej na morzu, ląd dla okrętu
  – poza obcą jednostką dla okrętu na morzu, nie łodzi podwodnej – i własny stos ≥ 2) oraz zamiana szkieletu metody na
  `ShiftAIUnit` (ramka, barbarzyńcy) + `DecideAIUnitMove` (zwykłe `return` zamiast skoków do L2fd0, pętla `for` po 8
  kierunkach). `ShiftAIUnit` nie ma już kodu w stylu asm. W `WeighUnitAtTile` / `BuyUnitAtPeace` odtworzone SI przed
  `SettleBattle` i DI w ścieżce zakupu: test porcji 83 trafia scenariusz, w którym `SettleBattle` rzuca `Towns[-1]` w obu
  wersjach (znak miasta bez miasta) i różnił się tylko SI. Test: stos obok obcego stosu z dyplomatą z przodu (albo z samych
  dyplomatów), obok własnego stosu, stos lądowy przeniesiony na morze (zapisy nie mają jednostek lądowych na morzu) z obcą
  jednostką na lądzie, okręty i transportowce przy wybrzeżu z obcą jednostką na lądzie (i znakiem obcego statku obok, inaczej
  płyną do celu), łódź podwodna, okręt w mieście, samotny atakujący w pustym własnym mieście z własną jednostką obok (0x32 = 1:
  miejsce strategiczne). Wszystkie mutacje złapane. Test w `BatchEightyNineDifferentialTests`.
- Porcja 90 (2026-10-18): pierwszy kawałek `HandlePlayerTurn` (`PlayerTurn`, ok. 5650 linii, tura gracza): początek tury
  (`PrepareTurn`: 0x6b90, 0x20, 0xe, 0x4, dla człowieka tylko on grany przez człowieka i pomoc w turach 20 / 60 i 40 / 80,
  ruchy jednostek – 3 na pole typu, statki +3 z cudem 4 albo 8, bez ruchów przy 0xdf60 = 1 – i zmienne 0x6, 0x8, 0x28,
  0x2e, 0x46) i początek ruchu jednostki (`StartUnitMove`: pusty slot, wartownik / ufortyfikowana, bez ruchów – następna;
  typ od 0x1c staje się milicją, w pierwszym przebiegu `ShiftUnit` w miejscu, fortyfikująca się staje się ufortyfikowana:
  jednostka komputera w mieście ≥ 3 dostaje je jako macierzyste, w macierzystym bez obcej jednostki obok trafia na listę
  obrońców). SI odtworzone jak w oryginale (gracz albo jednostka). Kopia referencyjna `PlayerTurnReference.HandlePlayerTurn`
  wymagała ręcznej poprawki (pełna nazwa `Civilization1Remaster.Engine.GameLoadAndSave`, inna przestrzeń nazw). Test: cała
  tura gracza (komputera albo człowieka ze skryptem klawiszy) z turą 20 / 40 / 60 / 80, 0xdf60, fortyfikującymi się
  jednostkami (z miastem macierzystym albo bez), typem od 0x1c, cudem 4 / 8 w mieście gracza i wielkościami miast; 72 tury
  w ok. 10 s. Wszystkie mutacje złapane. Test w `BatchNinetyDifferentialTests`.
- Porcja 91 (2026-10-18): drugi kawałek `HandlePlayerTurn`: `FocusOnUnit` (L0356: jednostka człowieka bez rozkazów poza
  widokiem mapy 0xd4cc / 0xd75e czeka na drugi przebieg, inaczej widok przesuwa się do niej, 0x46, panel jednostki w turze 0),
  L043b – L047f bez skoków (czekanie na klawisz dla gracza granego przez człowieka, kursor myszy, 0xa / 0xc / 0x36 / 0x28),
  `ObtainAIOrder` (rozkaz z `ShiftAIUnit`, po > 4 próbach 0x20 i zapomniany cel) i `ObtainStandingOrder` (jednostka z
  rozkazami albo celem: 'r', 'i' / 'm', 'f', 'p', 'h' dla samolotu). Poprawka porcji 90: `ShiftUnit` odkłada SI na stos
  (tam, gdzie potem szuka go `ShiftAIUnit`), a oryginał woła go z SI = adres jednostki – test 91 to złapał w ramce
  (słowo pod BP-0x1a: 0x1908 zamiast 0x1800), test porcji 90 nie. Test (`BatchNinetyOneDifferentialTests`): tury człowieka,
  komputera i komputera granego przez człowieka (bit w `ParticipantMarkers`), losowy widok mapy, tura 0, 0xb278, rozkazy
  osadników (0x40, 0x80, 0xc0, 0x82, 0x02), cele, bombowiec z celem; porównuje ramkę i 0x80 bajtów stosu pod nią. Pułapki:
  rozkazy osadników dla innych jednostek albo bity rozkazów samolotu sprawiają, że tura człowieka nigdy się nie kończy
  (skrypt klawiszy bez końca, ok. 40 s na turę albo zawieszenie); `check_mutations.py` ma teraz limit czasu (600 s,
  zawieszony mutant = CAUGHT (hang)). Mutacja niezłapana: 'h' samolotu → 'i' (samolot z celem idzie najpierw do celu, klucz
  nie ma znaczenia; samolot z bitami rozkazów zawiesza turę testu). Test w `BatchNinetyOneDifferentialTests`.
- Porcja 92 (2026-10-18): trzeci kawałek `HandlePlayerTurn` – wybór człowieka (L05cc – L0d00): `TakeHumanOrder` (panel
  jednostki, pomoc pierwszej tury i dla osadników, migająca jednostka, klawisz – Enter przy zaznaczonym polu działa jak
  kliknięcie – albo kliknięcie: pasek menu, mapa świata, pałac, panel pod nim przy braku jednostki = koniec tury),
  `LocateClickedTile`, `ShowPalace`, `TakeHeldClick` (przytrzymane kliknięcie obok jednostki = cel, także przez linię
  zmiany daty; na widzianym polu Civilopedia terenu), `TakeClick` (wskaźnik GoTo: cel osiągalny dla jednostki; miasto
  człowieka: ekran miasta; własna jednostka: wybór ze stosu; inaczej przesunięcie widoku) i `ForgetMarkedTile`; wynik
  `HumanOrder` mówi, dokąd skacze reszta funkcji. Test (`BatchNinetyTwoDifferentialTests`): tura człowieka z własnym
  `InputStarved` – klawisze (Space, Enter, 'g') i kliknięcia (0x586e / 0x5870 / 0x5874 przy 0x1a3c = 1 od kliknięcia do
  następnego klawisza; przytrzymane obok aktywnej jednostki z lokalnej 0x32 ramki), kliknięcia na krawędziach pól, tura 0,
  jednostki-wartownicy, 0xd806, potem Space, Enter, '1', Escape. Pułapki: mysz włączona na stałe zawiesza ekran miasta,
  wyłączona zaraz po kliknięciu zeruje pozycję puszczonego przycisku (`ObtainMouseState`); bez Escape tura utyka (limit
  klawiszy). Niesprawdzone (mutacje przeżywają): linia zmiany daty przy przytrzymanym kliknięciu, cel GoTo okrętu na morzu,
  tryb GoTo 2 (0x36 = 2, 'g' przełącza tylko 0 / 1), kliknięta własna jednostka-wartownik (bit 0 kontra 3). Test w
  `BatchNinetyTwoDifferentialTests`.
- Porcja 93 (2026-10-18): czwarty kawałek `HandlePlayerTurn` – wstęp rozkazu i proste klawisze: `PrepareOrder` (L0d0a:
  jednostka z celem albo bez jednostki idzie dalej przez `CarryOutGoto`, inaczej zapomina kierunek; miejsce 0x2c / 0x30),
  `DisbandUnit` ('D'), `PillageTile` ('P': jednostka lądowa – nie dyplomata, karawana, myśliwiec – poza miastem z ruchami
  niszczy nawadnianie i kopalnię, inaczej kolej, inaczej drogę; pole widziane przez człowieka zapamiętane, traktat z
  właścicielem najbliższego miasta, miejsce strategiczne dla gracza w DI – jak w oryginale), `SkipUnit` (Space), czekanie
  ('w', bez metody), `SentryUnit` ('s'), `UnloadUnit` ('u': transportowiec albo lotniskowiec w stosie budzi ładunek) i
  `ToggleGoTo` ('g', zmiana hosta z kwadratowym kursorem). Etykiety rozdziału (L0d75, L0d9b, L0f1f, …) zostają do zamiany na
  `switch` na końcu. Test (`BatchNinetyThreeDifferentialTests`, wzór 92): tura człowieka z losowymi klawiszami, ulepszenia
  pól pod jednostkami, cele jednostek lądowych, pierwsza jednostka stosu zamieniona w transportowiec (zapisy nie mają statków
  z ładunkiem) i 'u' na początku. Pułapki: 's', 'g', Enter zapętla oryginał bez pytania o klawisze (GoTo bez aktywnej
  jednostki), więc 'g' tylko parami; cele okrętów na lądzie też zawieszały turę. `DiscardTileImprovements` nie ma wpisu w
  dzienniku – etykieta grabieży z `WipeDiplomacyFlags`. Mutacje równoważne / nieosiągalne: ruchy 0 przed `EliminateUnit`
  ('D'), typ < 0x1a → < 0x19 (broń jądrowa człowieka), `LeftoverSteps == 0` → `== 1` w grabieży (aktywna jednostka ma ruchy;
  1 tylko po ruchu po drodze), lotniskowiec w 'u' (brak w zapisach), `0x36 ^ 1` → `^ 3` (0x36 porównywane tylko z 0 i 2).
- Porcja 94 (2026-10-18): piąty kawałek `HandlePlayerTurn` – klawisze 'h' (`OrderHome`: w mieście miasto macierzyste,
  samolot leci do najbliższego własnego miasta – skok do L1c10 – a bez niego człowiek zapomina rozkaz, komputer traci
  samolot), 'p' (`CleanPollution`: osadnicy sprzątają skażenie przez 4 tury, bez skażenia rozkaz zapomniany, inni –
  pomoc) i 'f' (`FortifyOrBuildFortress`: jednostka lądowa fortyfikuje się – komputer w mieście bierze je jako macierzyste –
  osadnicy z Construction budują twierdzę do obrony terenu 0x3e + 4; 0x3e to teren ostatnio klikniętego / sprawdzanego
  pola, jak czyta oryginał). Test (`BatchNinetyFourDifferentialTests`, wzór 93): skażenie albo twierdza pod osadnikami,
  ich licznik pracy, Construction, jednostki w miastach bez miasta macierzystego, pierwsza ruszająca się jednostka jako
  bombowiec z 'h' na początku. Mutacje: równoważna `& 0xfd` → `& 0xfc` (bit 0 aktywnej jednostki jest 0); fortyfikacja
  komputera w mieście łapie test tur komputera (`HandlePlayerTurnOrdersMatchOriginalCode`); niesprawdzona granica
  `work > licznik` → `>=` (teren 0x3e nieznany w scenariuszu).

## Następny krok

Zostały duże metody: `python Civilization1Remaster.Tests/Differential/Reference/find_asm_methods.py Civilization1Remaster.Core 99999`
(`HandlePlayerTurn`, `HandleCity`). Dzielić na kawałki:
wydzielać fragmenty jako osobne metody z własnymi testami, jedna sensowna porcja na commit.
Stan 2026-10-18 (po porcji 94): 2 metody w stylu asm (`HandlePlayerTurn`, `HandleCity`); `ShiftAIUnit` gotowa. `HandlePlayerTurn` przepisywana kawałkami od początku (następny: klawisze r L1400, i L158c, m L17c4, b L19bf, potem rozdział L1aed jako `switch`; test wzorem `BatchNinetyFourDifferentialTests`). Test: cała tura gracza (`BatchNinetyDifferentialTests` jako wzór).

## Błędy przetłumaczonego kodu zachowane celowo (baseline je ma, naprawić na końcu)

Kod C# przetłumaczony z asm ma miejscami inne zachowanie niż DOS-owy CIV.EXE. Testy porównują z tłumaczeniem, więc na razie
te błędy są odtwarzane jawnie:

- `HandleGameEnd`: co 50 tur wpis 11 historii dostaje jako starszy bajt populacji `(bajt & 0xff00) >> 8`, czyli zawsze 0;
  losowe katastrofy miast: licznik pętli to numer wylosowanego miasta + 1 (pętla kończy się po pierwszym mieście, chyba
  że wylosowano miasto 0). Po naprawie: starszy bajt populacji, dwa losowania.
- `DisplayCensus`: wielkość rodziny wypisywana jako „x/10.x/10” zamiast „x/10.x%10”. Po naprawie: reszta z dzielenia.
- `RenderSpaceship`: obrazek części statku spod `word[część << 1 + 0x682a]` – w C# to przesunięcie o 0x682b, czyli o 11 bitów,
  więc adres jest zły (obrazek zwykle nie istnieje i nic się nie rysuje); masa i czas lotu wypisywane jako „x/10,x/10” zamiast
  „x/10,x%10”. Po naprawie: `word[0x682a + 2 * część]`, reszta z dzielenia.
- `ConfigureFreshGame`: zerowanie `TroopsRuined` po każdej `ConstructCivilization` dotyczy zawsze gracza 6 (licznik innej
  pętli). Po naprawie: gracz z pętli (albo usunąć, jeśli `ConstructCivilization` to robi).
- `DivideCivilization`: technologie przepisywane z gracza na niego samego (nowa cywilizacja zaczyna bez technologii), flaga 9
  wojny ustawiana drugi raz w relacji gracza zamiast nowej cywilizacji, jednostka bez miasta macierzystego (0xff) poza miastem
  rzuca wyjątek (`Towns[255]`), brak miasta do oddania zapętla podział. Po naprawie: kopia technologii, relacja obustronna,
  sprawdzenie 0xff i warunek końca pętli.
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
