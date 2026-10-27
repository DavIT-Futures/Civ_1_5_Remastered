# Migracja asm → C#: lista na później

Zasada od 2026-10-12: najpierw migracja kodu emulowanego (asm-style C#) na czytelny C# **bez żadnej zmiany zachowania**.
Każdy krok musi przechodzić testy golden master (`Civilization1Remaster.Tests/GoldenMaster`, porównanie z oryginalnym
kodem gry). Usprawnienia i zmiany reguł gry wchodzą dopiero na sam koniec, gdy wszystko będzie przeniesione i przetestowane.

## Usprawnienia wycofane z gry (do zaimplementowania na końcu)

| Usprawnienie | Skąd (commit) | Co zostało w kodzie |
|---|---|---|
| ~~Szybsze leczenie~~ – **włączone 2026-10-14** na prośbę użytkownika (wyjątek od zasady): 2 HP na turę w mieście (wszystkie jednostki), lądowe poza miastem 1 HP ufortyfikowane, okręty poza miastem 1 HP przy lądzie, do 3 pól od własnego miasta, gdy Sentry albo bez ruchu w tej turze (okręty nie mogą się fortyfikować, wersja z eed8efb wymagała fortyfikacji, więc nie działała), samoloty tylko w mieście | eed8efb | `TurnEvents.MendFortifiedUnits` + `HealingRules`; stare leczenie za przełącznikiem `TurnEvents.ClassicHealing`, który `HeadlessGame` włącza, bo baseline golden master nagrano ze starym; testy `HealingRulesTests`, `HealingInGameTests` |
| **Koleje (remaster), włączone 2026-10-24** na prośbę użytkownika: miasto ma kolej, gdy gracz zna Railroad (w oryginale tylko drogę, więc pociąg tracił 1/3 ruchu w każdym mieście; rysowanie mapy i tak łączyło tory z miastem); darmowy przejazd po kolei także z GoTo i dla komputerów (w oryginale tylko człowiek ręcznie), dla nich najwyżej 30 darmowych kroków na turę, żeby żadna jednostka nie krążyła bez końca | – | `PlayerTurn.PayForMove` → `RideForFree` + `RailroadRules`; stare koleje za przełącznikiem `TurnEvents.ClassicRailroads` (`HeadlessGame` go włącza); testy `RailroadRulesTests`, `RailroadInGameTests` (także 3 tury z torami wszędzie). Premia +50% tarcz z kolei na polu miasta celowo bez zmian (bit kolei nie jest ustawiany) |
| Ekran „Obsolete Wonders” w menu raportów i dopisek „(Obsolete)” na liście cudów | a04e786 | nic (`WorldMap`, `GameMenus` przywrócone) |
| Rozszerzone raporty doradców: postęp badań „(x/y)”, nauka na turę, sumy handlu i plonów, podsumowanie miast | a04e786, 2c9ab40 | `AdvisorStatistics` (nieużywane). Uwaga: wersja z refaktora wołała `HandleCity`, które ma skutki uboczne, więc nowa wersja musi liczyć bez zmiany stanu |
| `EliminateUnit`: natychmiastowe wyjście dla już usuniętej jednostki | a04e786 | nic (zostało tylko sprawdzenie zakresu, jak w baseline) |
| **Nowe (prośba użytkownika 2026-10-26):** konwersja produkcji miasta na złoto albo naukę (jak „Coinage” z późniejszych Civ) – gdy miasto nie ma już co budować albo gracz tego chce; ok. 50% tarcz zamienia się na wybrany plon. Do ustalenia: pozycja w menu produkcji, wybór złoto / nauka, zachowanie AI. Za przełącznikiem `Classic…`, który testy włączają | – | nic |

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
- Porcja 95 (2026-10-18): szósty kawałek `HandlePlayerTurn` – praca osadników: `BuildRoad` ('r': droga, na drodze kolej z
  Railroad poza miastem, nie na kolei, rzeka 0xb tylko z Bridge Building; licznik w bajcie 5 do 2 – 4 dla kolei – razy
  bajt 0x28e terenu), `IrrigateTile` ('i': przy wodzie – bit 1 0xd7fa – nienawadniane pole; tabela 0x44a, 12 bajtów na
  teren: słowo +0 nowy teren / -1 niemożliwe / -2 tylko przy wybrzeżu, +2 licznik), `MineTile` ('m': słowo +4 / +6; zmiana
  terenu dopiero po drugim zwiększeniu licznika ponad 5) i wspólne `StopWork`, `DoTileWork`, `ChangeTerrain`,
  `FinishTileWork`, `TerrainWorkByte`. Nowy pomocnik testów `HumanTurnScript` (klawisze i kliknięcia tury człowieka, potem
  Space, Enter, '1', Escape) – wzór dla następnych testów tury. Test (`BatchNinetyFiveDifferentialTests`): ulepszenia pól
  osadników (też sama kolej), liczniki pracy, bit wody, Railroad, Bridge Building, osadnicy w miastach. Niesprawdzone:
  kolej w mieście (pola miast nie mają bitu drogi, więc warunek miasta nie decyduje), granica licznika kopalni (`<= 5`).
- Porcja 96 (2026-10-18): siódmy kawałek `HandlePlayerTurn` – `BuildCity` ('b': osadnicy na lądzie poza rzędami
  biegunowymi dołączają do miasta < 10 albo zakładają miasto – `EstablishCity` z lokalną 0x24, jak w oryginale) i rozdział
  rozkazu jako `switch` w `CarryOutOrder` (zamiast porównań i tabeli skoków L1aed / L1b38; L1b5e nie czyta AX); znikają
  etykiety obsługi klawiszy L0d75 – L19bf. Test (`BatchNinetySixDifferentialTests`, `HumanTurnScript`): wszystkie klawisze
  rozkazów (bez 'g' i 'D'), wielkości miast 7 – 11, pierwsza ruszająca się jednostka jako osadnicy (czasem w mieście).
  Niesprawdzone: rząd 0x2f (`>= 0x30` → `>= 0x2f`; mapy z zapisów nie mają tam lądu).
- Porcja 97 (2026-10-19): ósmy kawałek `HandlePlayerTurn` – klawisze ruchu: L1b5e (bez jednostki porusza się tylko
  zaznaczone pole), rozdział L32e4 jako `KeyDirection` (kierunki 1 – 8 od GoTo i komputera, strzałki i klawiatura
  numeryczna; porównania ze znakiem jak w oryginale – od 0x8000 brak kierunku) i 'c' (`CenterView`), znikają etykiety
  kierunków L1bb8 – L1c05; L1c10: `MoveMarkedTile` (zaznaczone pole o krok, widok przesuwa się poza ekranem) i
  `BarbarianPillages` (jednostka barbarzyńców – nie dyplomata, legion ani rycerze – schodząca z nawadnianego / górniczego pola
  poza miastem, najbliżej miasta człowieka, niszczy je i traci ruchy). Test (`BatchNinetySevenDifferentialTests`):
  strzałki, klawiatura numeryczna, 'c', 'g' – strzałki – 'g', tura barbarzyńców z jedną jednostką na ulepszonym polu obok
  miasta człowieka. Niesprawdzone: aktywna jednostka 127 (`>= 0x80` → `>= 0x7f`), zaznaczone pole w rzędzie 49.
- Porcja 98 (2026-10-19): dziewiąty kawałek `HandlePlayerTurn` – pole ruchu i strefa kontroli: `LocateMoveTarget`
  (L1d96: pole w kierunku 0x24 – 0x1c / 0x26 – teren pod jednostką 0x34 i tam 0x3e, ulepszenia 0x40, jednostka 0x18; jednostki
  inne niż lądowe idą do L219c) i `StoppedByZoneOfControl` (jednostka lądowa – nie dyplomata ani karawana – na lądzie, z pola
  przy obcej jednostce na wolne pole przy obcej jednostce: piknięcie dla człowieka, pomoc przy opcji, cel zapomniany).
  Test (`BatchNinetyEightDifferentialTests`): obca jednostka obok pierwszej ruszającej się jednostki człowieka (widok
  ustawiony na nią, inaczej czeka na drugi przebieg), strzałki w stronę pól przy obcej jednostce, opcja pomocy. Piknięcie
  (`PerformSound`) nie ma wpisu w dzienniku – etykieta z pomocy. Niesprawdzone: jednostka lądowa na morzu (0x34 = ocean),
  samolot traktowany jak jednostka lądowa (brak samolotów w zapisach).
- Porcja 99 (2026-10-19): dziesiąty kawałek `HandlePlayerTurn` – dyplomata i karawana przy mieście
  (`EnterCityAsDiplomatOrCaravan`, L1ed2 – L2157, wynik `MoveStep`): z morza nie do obcego miasta; dyplomata w obcym mieście
  działa (`DiplomatArrivesAtCity`; w mieście człowieka widoczny, widok i animacja ruchu); karawana pyta (we własnym mieście
  z cudem – produkcja poniżej -24 – też „Help build WONDER”, 0xb276 = 2 bliżej niż 10), chyba że miasto jest własne, bez cudu,
  bliżej niż 10 od miasta macierzystego i na tym samym kontynencie; szlak handlowy albo pomoc (10 x cena do materiałów)
  kończą rozkaz. Test (`BatchNinetyNineDifferentialTests`): pierwsza ruszająca się jednostka (człowieka albo komputera
  przy mieście człowieka) jako dyplomata / karawana obok miasta, strzałka do miasta, odpowiedzi w dialogu, produkcja miasta
  -30 … 3, miasto macierzyste blisko / daleko. Pułapka: przed ruchem jednostki gra opróżnia klawiaturę (`AwaitForKey`) i
  pierwszy klawisz przepada – potrzebny klawisz-ofiara ('1'); dopisane w `HumanTurnScript`. Niesprawdzone: odległość od
  miasta macierzystego dokładnie 9 (`< 10` → `< 9`).
- Porcja 100 (2026-10-19): jedenasty kawałek `HandlePlayerTurn` – `BribeUnitOnTheWay` (L2157: dyplomata obok obcej
  jednostki poza miastem przekupuje ją) i `CheckMoveTile` (L219c – L2329, wynik `MoveCheck`: jednostka lądowa na morze
  wchodzi na własny statek z miejscem – status i ruchy zapisywane przez SI zostawione przez `TallySpecialUnitsInStack`, czyli
  pod 0x600 x gracz, jak w oryginale – albo zostaje; przy obcej jednostce brak ataku z pokładu, okrętem podwodnym na ląd,
  jednostką komputera bez ataku, dyplomatą, na samolot poza miastem poza myśliwcem; inaczej atak L2329). Zapisy przez SI
  po wywołaniach (`BuyUnit`, `DisplayHelpForHuman`) zostają przez SI. Test (`BatchHundredDifferentialTests`): pierwsza
  ruszająca się jednostka człowieka w wybranym miejscu (ląd / morze), cel obok: własny transportowiec, puste morze, obca
  jednostka (bombowiec dla trybu samolotu), tryby dyplomaty, myśliwca, okrętu podwodnego. Niesprawdzone: miejsce na statku
  dokładnie 1 (`<= 0` → `<= 1`), bit 2 statusu pod 0x600 x gracz (`& 0xf3` → `& 0xf7`), komputer z atakiem 1 (`== 0` → `== 1`).
- Porcja 101 (2026-10-19): dwunasty kawałek `HandlePlayerTurn` – atak: `ConfirmAttack` (poniżej 3 ruchów człowiek
  potwierdza „Attack at x/3 strength?” – anulowanie zapomina cel – komputer atakuje tylko z 2 ruchami) i `FightAt`
  (najlepszy obrońca stosu, widok na jednostkę człowieka, 0x70d8 w czasie bitwy z człowiekiem, `SettleBattle`; atakujący
  traci 3 ruchy – bombowiec wszystkie – albo ginie (skok do L3c40); zaatakowane miasto: bit 0x10 dla komputera, maleje bez
  murów, nie z morza, na najłatwiejszym poziomie nie miasto człowieka; wielkość 0 – zniszczenie i może eliminacja
  cywilizacji; człowiek widzi nową wielkość; pole bez jednostek komputera – nowa produkcja na kontynencie). Test
  (`BatchHundredOneDifferentialTests`): pierwsza ruszająca się jednostka człowieka obok obcej jednostki albo miasta (obrońca
  wprowadzony, mury, wielkość 1 – 3, poziom 0 / 1, rodzaj atakującego, bombowiec), ruchy 1 – 6 przy 0xdf60 = 1 (tura nie daje
  nowych ruchów), odpowiedzi w dialogu. Niesprawdzone: komputer z 1 ruchem (`>= 2` → `>= 1`), poziom trudności przy ataku
  komputera na miasto człowieka – oba też w testach tur komputera.
- Porcja 102 (2026-10-19): trzynasty kawałek `HandlePlayerTurn` – koszt ruchu (`PayForMove`, L267b – L2884): statek nie
  wchodzi na ląd poza miastem; samolot / statek do obcego miasta bez jednostek – broń jądrowa wybucha, inne odmowa (pomoc);
  bez ruchów brak ruchu; jednostka lądowa: droga na obu polach 1, kolej pod nią 0 (człowiek bez GoTo), inaczej 3 x bajt 0x28e
  terenu, a przy niepełnych ruchach kostka może zatrzymać jednostkę (ruchy 0); inne jednostki 3; ruchy nie poniżej 0. Test
  (`BatchHundredTwoDifferentialTests`): pierwsza ruszająca się jednostka człowieka w wybranym miejscu, drogi / kolej na obu
  polach, ruchy 1 – 9 przy 0xdf60 = 1, transportowiec na ląd, broń jądrowa do obcego miasta, bombowiec do pustego obcego
  miasta. Klawisze po ruchu zerują ruchy jednostki (etykieta darmowego ruchu z konfiguracji, mutacje go sprawdzają);
  koszt 3 dla statków i dolną granicę 0 łapie test tur komputera.
- Porcja 103 (2026-10-19): czternasty kawałek `HandlePlayerTurn` – `ComputerMayMove` (L2884 – L29a0): jednostka komputera,
  która zawróciłaby (przeciwny kierunek do ostatniego) w stosie do 2 jednostek, zapomina kierunek i cel i stoi (zapis przez SI
  zostawione przez `TallyUnitsByDomainInStack`, jak w oryginale), inaczej kierunek 0x24 staje się jej kierunkiem; jednostka
  lądowa komputera (albo samolot z paliwem) nie dołącza do własnego stosu poza miastem z 4 jednostkami (2 bez ruchów). Test:
  istniejące testy tur komputera (`HandlePlayerTurnOrdersMatchOriginalCode`) łapią mutacje; niesprawdzona granica stosu 4
  (`? 4 : 2` → `? 5 : 2`).
- Porcja 104 (2026-10-19): piętnasty kawałek `HandlePlayerTurn` – terytorium (`EnterTerritory` z `MayTreatyStop`,
  L29a0 – L2b06): gracz terytorium pola z warstwy 2 (x + 80) do 0x2; jednostka lądowa (nie dyplomata ani karawana) z lądu
  na ulepszone pole cudzego terytorium, między człowiekiem a innym graczem, bez własnej jednostki tam: człowiek może zerwać
  traktat (cel zapomniany; dotrzymany – brak ruchu), komputer w pokoju z człowiekiem staje (ruchy 0); na ulepszonym polu
  cudzego terytorium jednostka zostaje pokazana jego graczowi. Test (`BatchHundredFourDifferentialTests`): pierwsza
  ruszająca się jednostka człowieka obok ulepszonego pola terytorium komputera, pokój / wojna, odpowiedzi w dialogu
  traktatu. Równoważne: bit pokazania (`& 0xe` → `& 0x6`, `1 <<` → `2 <<`) – ruch potem liczy widoczność od nowa;
  niesprawdzone: warunek „człowiek albo terytorium człowieka” dla dwóch graczy komputera.
- Porcja 105 (2026-10-19): szesnasty kawałek `HandlePlayerTurn` – `ShowMove` (L2b06 – L2cdd): kto widzi ruch (0x48 =
  gracz pola docelowego): barbarzyńca na lądzie widziany przez człowieka blisko jego miasta zostaje pokazany widzącym jego
  pole; wejście do obcego miasta widzianego przez człowieka – pokazane przy ambasadzie z graczem jednostki albo miasta; pole
  morskie z bitem 1 – graczowi pola; ruch pokazany człowiekowi (albo człowieka, albo komputera przy 0xd806) jest animowany z
  widokiem na nim (opcja 0x20 albo własny ruch człowieka), inaczej pole jest rysowane od nowa. Test
  (`BatchHundredFiveDifferentialTests`): tury komputera i barbarzyńców z 0xd806, opcją 0x20 / 0x10, ambasadami człowieka i całą
  mapą widzianą przez człowieka. Niesprawdzone: bity widoczności (barbarzyńca przy mieście człowieka, ambasada, pole morskie) –
  zerowane zaraz po ruchu (L2cdd), decydują tylko o rzadkiej animacji.
- Porcja 106 (2026-10-19): siedemnasty kawałek `HandlePlayerTurn` – `MoveUnit` (L2cdd – L2deb): jednostka przechodzi na
  0x1c / 0x26 (0x14 – następna jednostka starego stosu, 0x22 / 0x2a – stare miejsce; położenie i widoczność 0 zapisywane przez
  SI zostawione przez `DiscardUnitFromTile` / `SituateUnitOnTile`, jak w oryginale); jednostka komputera (nie samolot) traci
  rozkazy (zostają bity 4, 5); pole wychodzi z cudzego terytorium; u celu jednostka zapomina cel i kierunek (komputer traci
  ruchy). Testy: istniejące testy tur (komputera i terytorium). Równoważna: `& 0x30` → `& 0x31` (bit 0 ruszającej się jednostki
  jest 0); niesprawdzone: statki komputera z bitami rozkazów (`!= Air` → `!= Sea`).
- Porcja 107 (2026-10-19): osiemnasty kawałek `HandlePlayerTurn` – `TakeCargoAlong` z `CarryUnit` (L2deb – L2ff4):
  jednostka (nie samolot) schodząca z morza na ląd traci ruchy; statek z ładunkiem (albo lotniskowiec) zabiera ładunek
  (0x14 – następna jednostka starego stosu, 0x4a – licznik do 20, 0x3c – zabrane): jednostki lądowe (samoloty dla
  lotniskowca – wybór przez sztuczkę `cmp`/`sbb`/`neg` oryginału: grupa 1, gdy statek nie ma ładowności), wartownicy, z morza
  albo (komputer) bez rozkazów w stosie płyną razem, na transportowcu jako wartownicy, samoloty z rundą paliwa mniej; do
  zapełnienia (lotniskowiec i barbarzyńcy biorą wszystko). Testy: istniejące testy tur komputera. Niesprawdzone: lotniskowce
  (brak w zapisach), ładunek z bitem 3, pełna ładowność (`<=` → `<`) i barbarzyńcy (`!= 0` → `!= 1`).
- Porcja 108 (2026-10-19): dziewiętnasty kawałek `HandlePlayerTurn` – `ArriveAtTile` (L2ff4 – L3184): zdobycie obcego
  miasta (`SeizeCity`, jednostka pokazana jego graczowi, `TagTileOwned`), pole dla gracza (`MapAssignCityOwner`), rozejrzenie
  się (`ShiftUnit`), wioska (`VisitHut`, znacznik w `ChartSight`), jednostka (nie statek) na morzu budzi stos (0x1560),
  barbarzyńca widziany przez człowieka przy jego mieście. Testy: istniejące testy tur. Niesprawdzone: znacznik wioski
  (`|= 1` → `|= 2`) i barbarzyńca przy mieście człowieka (rzadkie w turach testowych).
- Porcja 109 (2026-10-19): dwudziesty kawałek `HandlePlayerTurn` – `FinishMove` (L3184 – L3365): ruch widziany przez
  człowieka (albo jego, albo komputera przy 0xd806) pokazuje jednostkę na mapie (widok na niej dla człowieka albo przy opcji
  0x20, krótkie czekanie przy ruchu komputera bez 0xb278); 0xa = 0; samolot w mieście ląduje (bez rozkazów, ruchów i celu),
  samolot na lotniskowcu dostaje paliwo (zapis przez SI zostawione przez `TallyUnitsOfTypeInStack`, jak w oryginale). Cały
  ruch jednostki (L1d96 – L3365) jest już metodami. Testy: istniejące testy tur; wszystkie mutacje złapane.
- Porcja 110 (2026-10-19): dwudziesty pierwszy kawałek `HandlePlayerTurn` – `CarryOutKey` (L3365 – L3c40, drzewo porównań
  zastąpione `switch`, bo szuka tylko równości): raporty F1 – F10 (inne przy 0xd806), widok przesuwany Shift+strzałką
  (`PanViewByDirection`, Alt+D przełącza 0xd806), Enter/Spacja bez jednostki kończą turę, stawki podatku i luksusów
  (`SelectTaxRate`, `SelectLuxuriesRate`), rewolucja 0xfffe (`StartRevolution`), zapis, dźwięk, wyjście (`ConfirmQuit`),
  Escape i Tab (pole zaznaczone: `ForgetMarkedTileOnScreen`, `ToggleMarkedTile`), menu paska (`ManageMenu`). Test
  `BatchHundredTenDifferentialTests` (klawisze z `HumanTurnScript`; przed klawiszem '1' dla `AwaitForKey` i Escape dla panelu
  osadnika `DisplayTileInfo`, który zjada klawisz; rewolucja z menu GAME, po którym `AwaitForKey` zjada jeszcze jeden
  klawisz; Tab też w turze bez ruchów, 0xdf60). Ocalałe: 0xdcfc = 2 (równoważne: tylko `== 0` i zaraz zerowane), 0x4 `^ 3`
  (równoważne: tylko porównanie z zerem, po turze martwe), SI `<< 2` przy nazwie w gazecie rewolucji (tekst widać tylko na
  ekranie gazety, test go nie łapie).
- Porcja 111 (2026-10-19): dwudziesty drugi kawałek `HandlePlayerTurn` – koniec spojrzenia na jednostkę i koniec tury
  (L3c40 – koniec): `ContinueOrFinishUnit` (jednostka z ruchami idzie dalej, chyba że 0x28 albo wyjście; inaczej koniec ruchu,
  0x1e = 0 przy czekaniu), `BurnPlaneFuel` (paliwo w mieście / na lotniskowcu, spalanie, utrata), `MayLoseTrireme` (trirema
  człowieka bez lądu wokół ginie raz na dwa); koniec przebiegu (drugi przebieg, spojrzenie na koniec tury przy opcji 4 albo
  bez klawisza jednostki, linia „end of turn” znika) zostaje w `HandlePlayerTurn` jako kilka linii. Test
  `BatchHundredElevenDifferentialTests` (samolot na lądzie / w mieście / na lotniskowcu, trirema na pełnym morzu / przy lądzie,
  jednostka lądowa czekająca 'w' z ruchami z 0xdf60); `BatchHundredTen` dostał losową opcję „End of Turn”. Wszystkie mutacje
  złapane (pusty slot `Kind == 0xff` łapie test rozkazów i ruchów).
- Porcja 112 (2026-10-19): ostatni kawałek `HandlePlayerTurn` – szkielet bez skoków: pętla przebiegów (nowy przebieg, gdy
  jakaś jednostka czekała; spojrzenie bez jednostki na koniec tury), `VisitUnit` (jednostka w przebiegu), `LookAtUnit`
  (spojrzenie: rozkazy aż do końca ruchów, mała maszyna stanów `LookStep`), `ObtainOrder`, `ChooseDirection`, `MoveTheUnit`
  (łańcuch ruchu bez etykiet). `HandlePlayerTurn` jest gotowa. `BatchHundredTen` dostał klawisze końca tury bez jednostek
  (klik w pasek menu, strzałka, Tab+strzałka, wyjście). Ocalałe: zlecenie stałe → `Order` zamiast `OrderOrKey` (równoważne:
  zlecenie stałe ma zawsze jednostkę), po walce `Key` zamiast `NextLook` (równoważne: klawisz ruchu nic nie robi w
  `CarryOutKey`), `continue` zamiast `break` po końcu tury w pętli jednostek i 0x32 przy wyjściu w `VisitUnit` (nieosiągalne:
  turę kończą tylko klawisze przy 0x80, a każde wyjście samo ustawia 0x32 = 0x80).
- Porcja 113 (2026-10-19): pierwszy kawałek `HandleCity` – `PrepareCity` (początek do L045f): lokalne i tablica 5 x 5 pól
  wokół (BP-0xa6) zerowane, materiały spoza 0 – 999 zerowane, po innych miastach 0xb2 (najbliższe), 0x6b30 (najbliższy pałac
  właściciela), pola w promieniu 2 obrabiane przez miasta do 5 pól dalej (i samo bliskie miasto, pole 20) w tablicy; 0x6548,
  0x48 (marnotrawstwo wg ustroju), 0xb882 (odkrycia zadowolenia bez cudu 0x1000), 0x4a (zadowoleni; dla komputera wg poziomu),
  0xe8b8 (specjaliści; w turze komputera miasto bez flagi 1 nie ma ich, z flagą robotnicy zdejmowani). L045f (powrót z ekranu
  miasta) zostaje etykietą. Test `BatchHundredThirteenDifferentialTests` (`HandleCity(miasto, -1 / 0 / 1)`, odkrycia zabrane
  albo nadane, bez pałaców, przesunięte bliskie miasto, poziom i lider, flaga 1; patrzy na ramkę i 0x180 bajtów pod nią).
  Ocalałe (martwe zapisy albo równoważne): końcowe 0xe4 = 5, 0xea = 21, 0x4c = 0x80 i SI przed `ObtainDistance` (nadpisywane
  przed odczytem), 0xb2 = 7 (nadpisywana przed odczytem w L34dd), `<=` zamiast `<` przy 0xb2 i 0x6b30 (przy równej odległości
  ta sama wartość), brak warunku odległości 0 (dwa miasta na jednym polu niemożliwe).
- Porcja 114 (2026-10-19): drugi kawałek `HandleCity` – `RenderCityHeaderAndMap` (L0497 – L0701, ekran miasta): jednostki
  miasta, tytuł „NAZWA (Pop:…)” (0xe8 = 1 + 2 + … + rozmiar), mapa miasta (widok na nim; pola niewidziane przez człowieka
  pomijane, pole innego gracza tylko odświeżane, pole obrabiane przez sąsiednie miasto w ramce). Test: `BatchHundredThirteen`.
  Ocalałe: końcowe 0xea (nadpisywane przed odczytem).
- Porcja 115 (2026-10-19): trzeci kawałek `HandleCity` – `FeedCity` (L0701 – L0965, tylko w turze miasta): flaga 0x80
  zdejmowana; przy zapasie żywności poniżej zera głoduje osadnik miasta (człowiek dostaje gazetę), a bez osadnika miasto ginie
  (błąd tłumaczenia, niżej); przy zapasie (rozmiar + 1) x 0x4a miasto rośnie (spichlerz zostawia 5 x rozmiar + 5), powyżej 10
  bez akweduktu (przy bicie 4 `DiagMarkers`) nie rośnie i człowiek dostaje komunikat. `CityManagement` dostało `UnitMemory`.
  Test `BatchHundredThirteen` dostał warianty żywności (poniżej zera, dość, dokładnie na granicy i o 1 mniej, rozmiar 10 – 11,
  spichlerz, akwedukt, bit 4, flaga 0x80). Wszystkie mutacje złapane.
- Porcja 116 (2026-10-19): czwarty kawałek `HandleCity` – `CountCityUnits` (L0965 – L0b4e): 0x50 = rozmiar + 1, 0xdeb8
  zajęte sloty `Unfamiliar` i utrzymywane jednostki (bez dyplomatów i karawan), 0xd2f6 te ponad rozmiar (wszystkie przy
  ustroju powyżej monarchii i bicie 2 `DiagMarkers`), 0x6546 jednostki atakujące poza miastem (samoloty zawsze), 0xe3c6
  osadnicy. Test `BatchHundredThirteen`: sloty (w małych miastach), jednostka miasta wybranego rodzaju, ustrój, bit 2; warianty
  stanu tylko poza ekranem miasta (znacznik 1 wtedy czeka bez końca), przesunięte bliskie miasto tylko przy -1 (w turze
  miasta oryginał może się zapętlić). Ocalałe: `<=` przy slotach (przy równości różnica 0), CX i końcowe 0xea (martwe zapisy).
- Porcja 117 (2026-10-19): piąty kawałek `HandleCity` – `MarkUnavailableTiles` (L0b4e – L0cd4): pola niedostępne dla
  robotników (tablica BP-0x3e): niewidziane przez właściciela (nie dla barbarzyńców), innego gracza (także w BP-0x8e; 0x70e8 = 1
  przy polu człowieka), obrabiane przez sąsiednie miasto; ich robotnicy zdejmowani; 4 słowa pod 0x70da zerowane. Test
  `BatchHundredThirteen`: miasto barbarzyńców (przy -1, osobno od bliskiego miasta – razem oryginał się zapętla). Ocalałe:
  czwarte słowo pod 0x70da i końcowe 0xea (nadpisywane przed odczytem).
- Porcja 118 (2026-10-19): szósty kawałek `HandleCity` – `KeepWorkers` (L0cd4 – L0e91, z `TestWorker`): miasto komputera
  co czwartą turę (bez flagi 1 i bez pola człowieka obok) wybiera robotników od nowa; inaczej 0x50 = rozmiar + 1 minus
  robotnicy: gdy nie za dużo, zostają (rysowani, w BP-0x3e i 0xee / 0xec), gdy za dużo, wszyscy schodzą (człowiek dostaje
  komunikat w turze). Test: przemieszani robotnicy, wyzerowana lokalna 0x16 (śmieci ze stosu), niewidziane pola na ekranie
  miasta. Ocalałe: pominięcie 0x50 == 0 w pętli (równoważne: robotników jest najwyżej 0x50), 0xb1e8 = 2 (tylko zapisywana,
  potem nadpisywana), warunek powrotu bez `0x50 != 0xe8b8` (przy równości 0x16 jest w testach zawsze 1) i bez `0x50 != 0`
  (zmienia tylko 0xe8b8, nadpisywane dalej).
- Porcja 119 (2026-10-19): siódmy kawałek `HandleCity` – `PickWorkers` (L0e91 – L12c2, z `LocateFreeTile`, `TakeWorker`):
  pole miasta (raz, lokalna 0x16), potem póki żywność nie wystarcza (albo miasto mniejsze niż 3) pole z największą żywnością
  (potem tarcze x 2 + handel), potem (miasto komputera z flagą 1 trzyma jednego specjalistę więcej) pola z najlepszą ważoną
  sumą: żywność x 16 / nadwyżka żywności, tarcze x (3 x rozmiar) / tarcze, handel x (2 x rozmiar) / handel (dzielniki 1 – 99);
  robotnicy i specjaliści (bity 26 – 31) zapisywani, 0xae = -0x50. Test: `BatchHundredThirteen`. Ocalałe: górny limit 99
  nadwyżki żywności (nieosiągalny w grze) i dolny limit 1 handlu (handel 0 – 1 przy doborze robotników nie wystąpił).
- Porcja 120 (2026-10-19): ósmy kawałek `HandleCity` – `PrepareCitizensAndFactories` (L12c2 – L14de, L12c2 zostaje etykietą
  powrotu z ekranu miasta): ikony obywateli (typ 3 dla specjalistów bez typu, reszta 0), fabryka / zakład (0xf8), zasilanie
  (0xac: elektrownie i cud 15 na tym samym kontynencie), czynnik zanieczyszczeń 0x6c98, tarcze 0x70dc powiększone o ćwierć
  zasilania i premii fabryki. Test `BatchHundredThirteen` przeszedł na `fresh: true` (punkt kontrolny przed każdym
  scenariuszem; wcześniej zmiany stanu kumulowały się i oryginał zapętlał się godzinami) i dostał warianty budynków i cudu 15.
  Wszystkie 21 mutacji złapane (równoległy `check_mutations.py`, 3 min).
- Strategia testów (2026-10-19, po wielogodzinnych zawieszeniach): limit czasu scenariusza w `DifferentialPair` (120 s,
  zawieszenie = błąd testu z nazwą scenariusza), `fresh: true` dla testów zmieniających stan, mutacje równolegle w `.mutants/`,
  budżet 15 – 25 mutacji na porcję, po zmianie testu powtarzać tylko ocalałe. Szczegóły w skillu `migrate-batch`.
- Porcja 121 (2026-10-19): dziewiąty kawałek `HandleCity` – `ProduceInCity` (L14de – L174b, wynik `ProductionStep`): żywność i
  tarcze tury (flaga 1: bez tarcz), jednostka gotowa przy cenie x 0x4a (osadnicy miasta rozmiaru 1 na najłatwiejszym poziomie
  czekają, komputer nie buduje dyplomatów, pierwsza jednostka rodzaju do powtórki, koszary = weteran, osadnicy zmniejszają
  miasto, miasto rozmiaru 1 ginie, chyba że to ostatnie miasto właściciela). Przy `TrueExtent--` flaga pochodzi z porównania
  rozmiaru z 1 – to samo co zero po zmniejszeniu, więc tu bez błędu. Nowy test `HandleCityProductionMatchesOriginalCode` w
  `BatchHundredThirteen` (osobny, bo warianty produkcji wypierały warianty żywności z jednego testu). Wszystkie mutacje złapane.
- Porcja 122 (2026-10-19): dziesiąty kawałek `HandleCity` – `FinishUnitOfCity` (L174b – L1b60, z `SendDiplomat`): miasto
  człowieka (albo bez jednostki) nie trzyma tarcz i pokazuje „X builds Y” dla osadników, dyplomatów i karawan; miasto komputera
  wysyła karawanę do obcego miasta z największym handlem (nie człowieka), ostatnia broń atomowa komputera zeruje licznik
  kontaktu człowieka, potem nowa produkcja. Test produkcji: karawana, broń atomowa (`ConstructUnit` zwiększa `LiveTroops`;
  licznik człowieka ustawiony, bo zwykle już jest -1). Ocalałe: gałąź dyplomaty komputera (`SendDiplomat`) – nieosiągalna:
  `ProduceInCity` nie buduje dyplomatów dla komputera, więc 0xba = -1 i kod idzie ścieżką człowieka.
- Porcja 123 (2026-10-19): jedenasty kawałek `HandleCity` – `CompleteImprovement` (L1b63 – L1e60, z `TestImprovement`):
  budynek albo cud gotowy przy cenie x 0x4a; cud zbudowany gdzie indziej albo budynek, który miasto ma, nic nie robi; test
  atomowy (0x29) w gazecie, budynek dopisany, cena zapłacona; miasto człowieka i każdy cud ogłaszane (cud komputera odsłania
  miasto; ekran miasta przy opcji 8 i dysku), tarcze zerowane; miasto komputera wybiera następną produkcję (po pałacu -2).
  Test produkcji: budynki i cuda (także zbudowane), opcja 8, 70 scenariuszy na grę. Ocalałe: `>= 0` przy liczeniu jednostki w
  produkcji po budynku (AI w grach testowych nie wybrało wtedy osadników), gałąź dyplomaty (nieosiągalna, porcja 122).
- Porcja 124 (2026-10-19): dwunasty kawałek `HandleCity` – `BuildSpaceshipPart` (L1e60 – L2010): część statku (0x16 – 0x18)
  gracza, którego statek nie odleciał: człowiek dokłada ją na ekranie statku, komputer (0xc8 = wynik `ComputeSpaceshipSuccess`)
  dostaje zwrot tarcz i buduje dalej, potem startuje według wyniku albo (przy 40 % zwycięstw komputera i 0x28 u człowieka),
  gdy jest pewny, człowiek prowadzi, przylatuje później albo trzeci gracz jest bogaty (złoto gracza o numerze `LiveRealms`,
  jak w tłumaczeniu). Nowy test `HandleCitySpaceshipMatchesOriginalCode` (miasta komputera z gotową częścią). **Niepokryte:**
  zwrot tarcz i decyzja o starcie (13 mutacji) – `ComputeSpaceshipSuccess` liczy od nowa `BotStarshipVictoryRatio` (zawsze 0)
  i zwraca niezerowy wynik tylko przy zapełnionym statku komputera (miejsca w 0x375a), a takiego stanu nie ma w zapisach gier;
  do zrobienia: scenariusz ze statkiem komputera prawie pełnym. Test produkcji wrócił do losowań z porcji 123.
- Porcja 125 (2026-10-19): trzynasty kawałek `HandleCity` – `ApplyImprovement` (L2010 – L220f): pałac (1) znika ze wszystkich
  miast właściciela; człowiek słyszy o przeniesieniu (u komputera tylko z ambasadą 0x40) i wtedy stolicą zostaje to miasto
  (`HorizBegin`, bit pałacu znowu; bez komunikatu zostaje zgaszony – jak w tłumaczeniu); cud 0x26 daje dwa odkrycia; cud 0x2b
  człowieka pokazuje wszystkie obce miasta. Test: produkcji. Ocalałe: 0x2f9e = 1 (nadpisywane wartością 5 przed dialogiem).
- Strategia testów od porcji 126 (2026-10-19): porcja = 3 – 4 kawałki, testy z tabel przypadków (`BatchHundredTwentySix`,
  rekord `CityCase`, każdy przypadek z punktu kontrolnego; losowe testy `BatchHundredThirteen` zamrożone), 8 – 15 mutacji na
  porcję w jednej rundzie (`--jobs 6`), pełny zestaw tylko przy kamieniach milowych. Szczegóły w skillu.
- Porcja 126 (2026-10-19), cztery kawałki `HandleCity` (L220f – L28d9): `AdviseHumanProduction` (miasto człowieka z doradcą
  0x10 wybiera produkcję, gdy nie ma tarcz albo budynek, który już ma; 0x63 gasi flagę), `BuyForComputerCity` z `BuyShortfall`
  (miasto komputera dokupuje tarcze za złoto według roli kontynentu, statku człowieka, flag 1 i 0x10, produkcji -1, warunku
  0x19 – porównanie `ConditionMarker`, jak w tłumaczeniu – i bogactwa), `RenderCityScreenFrames` (ramki i zakładki ekranu
  miasta, minimapa), `CountCorruptionAndTradeRoutes` (korupcja wg odległości od pałacu i ustroju, sąd / pałac połowią,
  demokracja zeruje; szlaki handlowe: 1/8 albo 1/16). Test `BatchHundredTwentySix` (25 przypadków, ok. 10 s). Mutacje: 15,
  ocalała jedna – połowienie przy pałacu (w wybranym mieście komputera handel za mały, korupcja przy komunizmie < 2).
- Porcja 127 (2026-10-19), trzy kawałki `HandleCity` (L28d9 – L2cce): `SplitTrade` (korupcja całego handlu, podział na luksusy
  0x70e0, podatki 0xe17a i naukę 0x70e6 wg stawek, specjaliści po 2, biblioteka i uniwersytet dodają po połowie luksusów i
  podatków – jak w tłumaczeniu), `CountScienceAndContent` (rynek i bank po połowie nauki, cud 0xc po 1/3, miasto cudu 10
  podwaja; zadowoleni 0x70e4: człowiek wg poziomu, ustroju, liczby miast i numeru miasta, komputer rozmiar - 3; 0x6542 nadmiar;
  przetasowanie), `ApplyLuxuries` (0x70e2 = połowa luksusów, drugie przetasowanie, obywatele na zakładce 1 ekranu miasta).
  `CityCase` dostał wybór miasta (pierwsze / ostatnie / największe). Mutacje: 14, wszystkie złapane (4 po celowanych przypadkach).
- Porcja 128 (2026-10-19), trzy kawałki `HandleCity` (L2cce – L32e0): `ApplyContentImprovements` z `DrawContentIcon`
  (koloseum 3; katedra przy odkryciu 0x1d 4, z cudem 9 6; świątynia 2 przy 0x1a, inaczej 1 przy 0x19, cud 6 tyle samo
  jeszcze raz; ikony na zakładce 1), `ApplyMilitaryHappiness` (poniżej republiki do 3 jednostek atakujących w mieście – stos
  najwyżej 32 kroki i sloty – uspokaja; w republice i demokracji każda jednostka atakująca poza miastem niezadowala, w
  demokracji podwójnie, w republice nie z cudem 16), `ApplyWonderHappiness` (cuda 2 i 0x15 +1 szczęśliwy, miasto cudu 11 bez
  niezadowolonych, cud 13 -2 na swoim kontynencie; bez bitu 2 `DiagMarkers` zadowolenie i luksusy zerowane). Test
  `HandleCityHappinessCasesMatchOriginalCode` (27 przypadków). Mutacje: 15, ocalała jedna – cud 6 (2 albo 1 zależnie od 0x1a):
  cud nie zadziałał w żadnym przypadku (prawdopodobnie przestarzały w zapisach gier).
- Porcja 129 (2026-10-19), pętla jednostek miasta `HandleCity` (L32e0 – L39ef) w pięciu kawałkach: `StartUnitSupport` (liczniki,
  sloty, miejsca na ekranie; od nowa po rozwiązaniu jednostki), `SupportUnit` (utrzymanie jednostki), `DisbandUnsupportedUnit`
  (bez tarcz – albo u komputera w republice / demokracji z flagą 1 co ósmą turę – najdalsza jednostka miasta odchodzi; przy
  braku tarcz z komunikatem dla człowieka i liczeniem od nowa, inaczej tylko jednostka lądowa nie-osadnik, flaga 1 gaszona,
  chyba że 0x8078 i właściciel zna mniej odkryć niż człowiek: 0xdb42 = -999), `DrawSupportedUnit` (tarcza, żywność osadników,
  niezadowolenie jednostki poza miastem, jednostka i kropka minimapy), `DrawUnitInCity` (jednostki stojące w mieście z
  miastem domowym, lista w BP-0x74). Test `HandleCitySupportCasesMatchOriginalCode` (11 przypadków). Mutacje: 15, wszystkie
  złapane (3 po celowanych przypadkach).
- Porcja 130 (2026-10-19), cztery kawałki `HandleCity` (L39ef – L3d54): `CountSupportCosts` (w turze: niezapłacone utrzymanie
  +5 do 0xe3c2, jednostki do rozmiaru +1, 0xdb42 spada o jednostki poza miastem x (5 z biblioteką, inaczej 7, minus słowo 2
  rekordu przywódcy pod 0x1516)), `RenderImprovementList` z `RenderImprovementName` (lista na ekranie miasta: cuda miasta,
  potem budynki od 0xda; pełna lista – 16 wierszy – kończy się na 0xfe i włącza następną stronę), `FinishImprovementList`
  (ramka, „więcej”, 0xde cena produkcji). Test `HandleCityListCasesMatchOriginalCode`. Mutacje: 13; ocalała jedna – czyszczenie
  bitu 0 w 0xca (w jednym przebiegu ekranu bit 0 jest zawsze 0: 0xca zerowane na starcie `HandleCity`).
- Porcja 131 (2026-10-19), trzy kawałki `HandleCity` (L3d54 – L40ba): `RenderProductionBox` (pole produkcji: 0x44 tarcz w
  wierszu wg ceny i zapasu, 1 – 99; przyciski Change / Buy miasta człowieka, jednostka albo nazwa budynku), `RenderShieldsInBox`
  (siatka tarcz), `RenderFoodBox` (pole żywności, linia spichlerza, zapas najwyżej do wzrostu). Test
  `HandleCityBoxCasesMatchOriginalCode`. Mutacje: 12; ocalała jedna – `80 / pudełko` vs `81 / pudełko` (równoważne dla każdego
  możliwego pudełka: 10 człowieka, parzyste 8 – 16 komputera).
- Porcja 132 (2026-10-19), dwa kawałki `HandleCity` (L40ba – L439e): `RenderResourceRows` (pole zasobów ekranu miasta: wiersze
  żywności, tarcz i handlu z przerwami po zjedzonej żywności, po opłaconym utrzymaniu i przed korupcją), `RenderTradeRow` z
  `RenderTradeIcons` (wiersz luksusów, podatków i nauki). Test `HandleCityRowCasesMatchOriginalCode`. Mutacje: 12; ocalała
  jedna – krok `116 / (x + 1)` wiersza tarcz i handlu (różni się od `x + 2` tylko przy wartościach, których przypadki nie
  ustawiają wprost, np. 15 albo 18).
- Porcja 133 (2026-10-19), trzy kawałki `HandleCity` (L439e – L4852): `RenderShortfalls` (brakująca żywność i tarcze
  utrzymania za wierszem, zacieniowane; korupcja w wierszu handlu), `RenderCitizensAndRoutes` (obywatele u góry ekranu,
  szlaki handlowe: nazwa i handel – ósma część z miastem obcym, szesnasta z własnym – na pierwszej zakładce, kropka na
  minimapie), `RenderCityDotAndPollution` (samo miasto na minimapie, zanieczyszczenie: tarcze przez dzielnik 0x6c98 mniej 20,
  ćwierć rozmiaru razy postępy 0xb882). Test `HandleCityShortfallCasesMatchOriginalCode`. Mutacje: 13, wszystkie złapane
  (dwie dopiero po przypadku z czterema postępami zanieczyszczenia). Ćwiartka liczona zwykłym dzieleniem: oryginał bierze
  wartość bezwzględną, różnica tylko dla -32768.
- Porcja 134 (2026-10-19), trzy kawałki `HandleCity` (L4852 – L4bba): `RenderExitAndBuyButtons` (przyciski i czy Change
  miga: budynek, który miasto ma, część wysłanego statku, zbudowany cud, jednostka przy 127), `AwaitCityScreenInput`
  (puszczenie przycisku myszy, potem klik, klawisz albo flaga 0xb8; miganie co 10 tyknięć), `ReadCityScreenInput` z
  `ChangeStep` (klawisz albo -1, przycisk Change albo A przełącza doradcę; doradca wybiera produkcję, 99 – od początku
  miasta). Test `HandleCityButtonCasesMatchOriginalCode`. Mutacje: 14; ocalały dwie – kolory migania (ekran miasta jest
  zamalowany po wyjściu, a test porównuje ekran tylko na końcu; liczba mignięć 0xc0 jest sprawdzana).
- Porcja 135 (2026-10-19), trzy kawałki `HandleCity` (L4bba – L4ff5): `HandleCityScreenButtons` (Buy, Change, zakładki I H M V
  albo klik 0x60 – 0xe0, widok miasta od razu), `BuyCityProduction` (koszt brakujących tarcz 0 – 999: jednostka podwojone
  plus dwudziesta część kwadratu, budynek podwojone, cud i część statku poczwórne, bez tarcz jeszcze razy dwa; w niepokojach
  jednostki nie kupi), `ChangeCityProduction`; `ChangeStep` przemianowany na `CityScreenStep`. Test
  `HandleCityPurchaseCasesMatchOriginalCode`. Mutacje: 16, wszystkie złapane (trzy po przypadkach na brzegach: klik tuż za
  zakładkami, dół przycisku Buy, złota równo na koszt).
- Porcja 136 (2026-10-19), trzy kawałki `HandleCity` (L4ff5 – L53c8): `ActivateUnitsInCity` z `UnitOfCityList` (A: kursor
  po jednostkach w mieście od pierwszej śpiącej lub zajętej, strzałki, Enter albo spacja budzi – śpiąca lub ufortyfikowana
  dostaje ruchy – i kasuje cel; Escape kończy), `PickImprovementToSell` (S, gdy w tej turze nic nie sprzedano: migający
  budynek, strzałki, Enter albo spacja sprzedaje – L5740), `ToggleSpecialist` (1 – 9: kolejny typ specjalisty, miasto
  mniejsze niż pięć dostaje komunikat). Test `HandleCityKeyCasesMatchOriginalCode` (pomocnicy `Garrison`, `Specialists`).
  Mutacje: 17, wszystkie złapane (sześć po drugim zestawie przypadków: jednostki bez ruchów, szóste miejsce, specjaliści).
- Porcja 137 (2026-10-19), cztery kawałki `HandleCity` (L53c8 – L58ac): `MoveCityMapCursor` (P albo flaga 0xb8: migająca
  ramka na mapie miasta, osiem kierunków w obszarze miasta na pola widziane przez człowieka, Enter – L5aa6, Escape kasuje
  flagę), `HandleListAndRename` z `TurnListPage` (M albo strzałka listy – druga strona, R albo klik w nazwę – zmiana nazwy,
  klik w linię listy – sprzedaż), `SellImprovement` (L5740: menu z ceną, Yes sprzedaje). `CityScreenStep` dostał `Leave` i
  `Sell`. Test `HandleCityListClickCasesMatchOriginalCode`. Mutacje: 17; po jednej rundzie przypadków ocalały: kolor ramki
  (zamalowana zaraz po narysowaniu – niewidoczne), dół listy `vert < 0x5e` (klik na 94 daje tę samą linię co 93) i
  warunek „już sprzedano” przy kliknięciu linii (przypadek z flagą 0x80 i Yes go nie rozróżnił – do sprawdzenia).
  Uwaga: `CopyLocalWord(źródło, cel)` – przy pierwszej wersji odwróciłem argumenty, test od razu złapał.
- Porcja 138 (2026-10-19), trzy kawałki `HandleCity` (L58ac – L5cba): `ClickCitizen` (klik w obywateli u góry: specjalista
  pod nim, przez szerokość 0xf4 ikony), `ClickUnitOfCity` (klik w jednostki miasta, sześć w rzędzie po 16: budzi jednostkę),
  `ToggleCityTile` (L5aa6: pole mapy miasta – środek zdejmuje wszystkich robotników, pole widziane dostaje robotnika albo go
  traci; wolne tylko, gdy słowo ramki dla niego jest 0; 0xae, 0x50, 0xe8b8 i górne sześć bitów robotników). Wspólne
  `CycleSpecialist`, `RefuseSpecialists` (też w `ToggleSpecialist`) i `WakeUnitOfCity` (też w `ActivateUnitsInCity`). Test
  `HandleCityClickCasesMatchOriginalCode`. **SI/DI**: `ToggleCityTile` odtwarza SI i DI oryginału – menu (`OperateMenu`,
  `PreserveIndexRegisters`) odkłada je na stos pod ramką HandleCity, a test porównuje ten obszar (bez tego dwa przypadki się
  różniły). Mutacje: 18; po jednej rundzie ocalały: granica liczby jednostek przy kliknięciu (`>=` 0x4e – przypadki nie
  znają liczby jednostek w mieście), `0xae > 0` (żaden przypadek nie kończy z 0xae równym 0), słowo ramki `- 0x8e`
  (brak pola zajętego przez kogoś innego) i zerowanie `LaborerMarkers1` przy kliknięciu środka (gra i tak przydziela
  robotników od nowa i nadpisuje oba słowa).
- Porcja 139 (2026-10-19), dwa kawałki `HandleCity` (L5cba – L606c): koniec ekranu miasta (0xd4cc, 0xd75e) zostaje w
  `HandleCity`, `ApplyCivilOrder` (niepokoje: wiadomość dla człowieka, zamieszki w widoku miasta przy opcji 8 raz, pomoc
  przy opcji 1, komputer wybiera inną produkcję, trwające niepokoje w demokracji obalają rząd; przywrócenie porządku;
  podatki 0xe17a poza anarchią), `RepickCityProduction` (też w `ChangeCityProduction`). SI odtwarzane w
  `RepickCityProduction` i `ReadCityScreenInput` (jak w porcji 138 – inaczej różnił się stos pod ramką). Test
  `HandleCityOrderCasesMatchOriginalCode`. Mutacje: 16; ocalały numer dźwięku (testy nie słyszą `PerformSound`) i
  `ConditionMarker |= 3` zamiast 1 (bit 2 jest dalej w turze liczony od nowa).
- Porcja 140 (2026-10-19), kawałek `HandleCity` L606c – L62d8: `ApplyCelebration` (święto „We love the ...”, flaga 0x40:
  bez nieszczęśliwych, większe niż 2, co najmniej połowa szczęśliwych, nie w anarchii; początek z wiadomością dla człowieka,
  trwające w republice i demokracji – wzrost przy nadwyżce żywności, od 10 tylko z akweduktem; koniec – „celebration
  canceled”). Test `HandleCityCelebrationCasesMatchOriginalCode`. Mutacje: 13; ocalały numer dźwięku, brak
  `RenderCitySceneryForHuman` (widok miasta zaraz zamalowany przez `RepaintMainScreen`), `0xe8 < -1` (0xe8 równe -1
  wymagałoby dokładnie jednego nieszczęśliwego ponad szczęśliwych) i `&= 0xbe` (bit 1 jest tu zawsze 0: porządek
  przywrócony wcześniej go kasuje).
- Porcja 141 (2026-10-19), kawałek `HandleCity` L62d8 – L64b8: `ApplyResearch` z `UncoverTechnologyWhenFlagged` (nauka
  miasta do badań właściciela: dla człowieka znany albo pusty cel zaczyna nowy, na najłatwiejszym poziomie podwójnie dla
  postępu znalezionego przez innych, cud 0x14 dodaje połowę; koszt 0xf4 razy liczba postępów, po przekroczeniu badania od
  nowa). Test `HandleCityScienceCasesMatchOriginalCode` (pomocnik `AtCost`). Mutacje: 16; ocalały trzy progi kosztu
  (`12 - postępy`, bez 0xd2de, `>=`): przypadki z kosztem wyliczonym w teście nie trafiły dokładnie w próg – w turze miasta
  coś jeszcze zmienia badania albo 0xd2de (do sprawdzenia).
- Porcja 142 (2026-10-19), kawałek `HandleCity` L64b8 – L672d: `ApplyScoreAndPollution` (punkty właściciela; tylko dla
  człowieka – inaczej skok do L68cc z pominięciem utrzymania budynków: zanieczyszczenie na polu miasta i awaria elektrowni
  atomowej w niepokojach), `CountPollution` (też w `RenderCityDotAndPollution`). Test
  `HandleCityPollutionCasesMatchOriginalCode`. Mutacje: 14; ocalały `DrawInt(21)` zamiast 20 (losowanie daje to samo poza
  rzadkim 20) i postęp 0x41 zamiast 0x42 (trzy przypadki ze znanym 0x41 nie wylosowały awarii).
- Porcja 143 (2026-10-19), kawałek `HandleCity` L672d – L6927: `PayImprovementUpkeep` (utrzymanie budynków miasta
  człowieka, nie w anarchii; pałac droższy od poziomu 2 i 4 i z postępami 0x22, 0x25; brak pieniędzy sprzedaje budynek –
  „can't maintain”), `FinishHandleCity` (L68cc: flaga 4 bez obcych jednostek obok – lista jednostek miasta zostaje; mysz;
  AX szczęśliwi mniej nieszczęśliwi). Test `HandleCityUpkeepCasesMatchOriginalCode`; `CheckCases` porównuje teraz także AX
  (`Register.Kept | Register.Acc` – wynik HandleCity). Mutacje: 16; ocalało `Gold >= -1` (żaden przypadek nie kończy
  dokładnie na -1). **Wszystkie kawałki asm `HandleCity` przepisane**: zostaje szkielet 296 linii z 38 `goto` między
  wywołaniami metod – do uporządkowania w strukturę (pętle i warunki) w kolejnych porcjach.
- Porcja 144 (2026-10-19): szkielet `HandleCity` ułożony bez `goto`: `HandleCity` (ramka, `RunCity` dla istniejącego miasta),
  `RunCity` (pętla: runda od początku – `StartCityRound`, ekonomia – `CountCityEconomy`, ekran miasta – `RunCityScreen` z
  `HandleCityScreenClicks`; potem `EndCityTurn` i `FinishHandleCity`), `ProduceInTurn`. Mutacje przepływu: 12 na testach
  tabel + 3 dopasowane; po przypadkach z miastem barbarzyńców złapane wszystkie poza krokiem `SettlersWait` (doradca
  w tym przypadku i tak nic nie zmienia – do sprawdzenia). Pełny zestaw zielony. **Koniec migracji asm → C#:**
  `find_asm_methods.py` nie znajduje już żadnej metody w stylu asm.

- Porządki po migracji, porcja 1 (2026-10-19, plan w `PLAN-PO-MIGRACJI.md`, część A): `World.IsHuman` (17 miejsc z 4 różnymi
  zapisami), `CityStructure` z `City.Has` / `Add` / `Remove` / `Structures` (66 testów bitów budynków; bit n = budynek
  n + 1 z listy gry – sprawdzone z tabelą w CIV.EXE; recykling, nie metro, daje 0x6c98 = 3), `City.HasStructureSignExtended`
  zamiast dwóch prywatnych kopii, `Player.StopMaking` / `StartMaking`, `StopMakingUnit` / `StartMakingUnit`,
  `RestoreShieldsAndTrade`, klasa bazowa `GameModule` dla 43 klas. Pełny zestaw zielony.
- Porządki po migracji, porcja 2 (2026-10-19, część A punkty 4 i 6): `GameMessage` (176 łańcuchów wiadomości), `WriteReplayEntry`,
  `MaskNeighbours`, `UnitMemory.Offset`, `StartTerrainWork`, `CountSpecialSlots`. Pełny zestaw zielony.
- Resztki asm (2026-10-20): `DeactivateMouse` bez `goto` (ostatnie w projekcie), `MultiplyWords` / `DivideWords` w `GameModule`
  zamiast sekwencji IMUL / CWD+IDIV w AIPlayer (6 miejsc) i DialogDrawing (2), `settlement.Structures` w CityView. Rejestry
  zostają ustawione jak w oryginale. Pełny zestaw zielony; mutacje: rozszerzenie znaku w `DivideWords` i warunek w
  `DeactivateMouse` przeżyły – w grze dzielne są nieujemne, a AX/BX po `DeactivateMouse` nikt nie czyta.
- Flaga przeniesienia (2026-10-20): `DosReleaseMemory` i `BiosDiskCall` zwracają `bool` (wywołujący pytali o AX, flagi nikt nie
  czytał), martwe zapisy `FlagSet.IsCarry` w `PrepareMachine` i `ObtainFreeStorage` usunięte. AX dalej ustawiany jak w
  oryginale. W Core nie ma już żadnego odwołania do flag procesora. Pełny zestaw zielony.
- B0, część 1 (2026-10-20): zamrożona gra (`Civilization1Remaster.Original`, znacznik `migration-end`), kopie pomocników
  testowych dla niej (`Tests/Frozen`), `DualGame`, `MovedData`, `DualTurnTests` (11 autozapisów × 5 tur, zgodne; mutacje
  5 z 5). Uwaga: `check_mutations.py` kopiuje do roboczych kopii tylko nowe pliki `.cs` – nowy projekt trzeba najpierw
  zacommitować.
- B0, część 2 (2026-10-20): `DualCityCaseTests` – wszystkie 19 tabel `HandleCity` na zamrożonej i obecnej grze; ustawienia
  przypadków (`CityCase.Setup`) i `HumanTurnScript.Install` przyjmują grę jako `dynamic`, tabele są `internal`. Mutacje 18 z
  21; pełny zestaw 744 zielony.
- B1, porcja 1 (2026-10-20): wynik przez `return` zamiast odczytu AX po `VerifyValueSpan`, `KeyAvailable`, `DisplayMenu`
  (68 miejsc, 18 plików); funkcje dalej ustawiają AX. Pełny zestaw 744 zielony.
- B1, porcja 2 (2026-10-20): wynik przez `return` po 19 funkcjach (ObtainContinentID, ScreenToPicture, TextLength,
  PlayerEnjoysWonderEffect, ObtainDistance, ObtainNavigationKey, ObtainTileOwner, ObtainTextWidth, VerifyMapLimits,
  ObtainDot, TunePosition, PickCityProduction, ObtainTerrainType, LocateNearestCity, ObtainCitizenType, AbsoluteValue,
  ObtainDistanceToCapital, ObtainTileImprovements, TallyUnitsByDomainInStack) – 117 miejsc w 25 plikach. Pełny zestaw zielony.
- B1, porcja 3 (2026-10-20): `DisplayDialog` i `QueryForDisk` zwracają wynik (`ushort`, dalej też w AX); 25 miejsc bierze go
  z `return`. Pozostałe „void z odczytem AX” z listy skryptu to fałszywe trafienia (AX z wcześniejszego wywołania) albo
  `TagTileOwned` – do obejrzenia ręcznie. Pełny zestaw zielony.
- B1, porcja 4 (2026-10-20): wynik przez `return` po kolejnych 38 funkcjach (teksty, pliki, okienka króla, jednostki, zapis
  gry, mysz…) – 47 miejsc. Pomijane: `FindSubtext` (AX to adres, nie wynik), `TellStream`, `CloseStream`. Pełny zestaw zielony.
- B1, porcja 5 (2026-10-20): 30 miejsc z komentarzem między wywołaniem a odczytem AX (skrypt przechodzi teraz przez
  komentarze) i 5 pętli `do … while (AX …)` z wywołaniem na końcu ciała (`KeyAvailable`, `CompareTextNoCase`,
  `ObtainSongPosition`) – warunek woła funkcję. Zostały odczyty, przed którymi stoi inne wywołanie albo funkcja bez wyniku w
  AX (`ObtainFirstPrerequisite`, `AwaitForKeyPressOrMouseClick`, `SeizeCityScenery`, `DisplayStoryIntro`) – ręcznie.
- B1, porcja 6 (2026-10-20), ręcznie 19 miejsc: odczyty AX zastąpione słowem, które właśnie zapisano (0x12, 0x3a, 0x1a,
  słowo opcji menu), wynikiem (`ObtainTileImprovements` & 1, `PickCityProduction`, `LocateNearestCity`, `LocateRouteStep`,
  `ShiftToward`, `ScreenToPicture`, `ObtainContinentID`); `DisplayStoryIntro`, `HandleCity` i `GameMessage.Dialog` zwracają
  wynik. Zostaje 9 odczytów po wywołaniach, gdzie AX ustawia funkcja głębiej (`AwaitForKeyPressOrMouseClick`,
  `ObtainFirstPrerequisite`, `SeizeCityScenery`, `TellStream`, `FindSubtext` – AX to adres), wynik ze `switch` (Palace),
  `RunGame` i zapis rejestrów w przerwaniu myszy – do etapu B3. Pełny zestaw zielony.
- B2, porcja 1 (2026-10-20): martwe zapisy zmiennych lokalnych (słowa ramki zapisywane, a w pliku nigdzie nieczytane) usunięte
  w CityFoundingAndScore, GovernmentAndDiplomacy, MapManagement, Pathfinding i UnitCombat – 173 linie (skrypt `b2dead.py`:
  pomija pliki z `FrameWord` / `RegFrame` i zapisy, których wartość coś woła). Słowo 0x20 ramki `HandlePlayerTurn`
  (ParticipantMarkers) zostaje: bez niego 17 starych testów `HandlePlayerTurn` się rozjechało, więc ktoś je czyta.
  Ważne odkrycie B2: zmienne lokalne z własną ramką są już w dużej części czyste – zostały w pamięci celowo, bo inne funkcje
  czytają niezainicjowane słowa stosu („śmieci” po wcześniejszych wywołaniach, np. `RenderCitizenIcons` 0x2,
  `LocateNearestFriendlyCity` 0x14 / 0x16, `ObtainDistanceToReplayCity` 0x4, HandleCity w `HandleAllCities`). Główna praca B2
  to grupy metod dzielących ramkę wołającego (CityManagement 800 odczytów, PlayerTurn 617, AIPlayer 506, …).
- B2, porcja 2 (2026-10-20): stare testy różnicowe `HandleCity` (porównujące też stos z kopią Reference) wycofane:
  `BatchHundredThirteenDifferentialTests` (losowe) przeniesione na podwójną grę jako `Dual/DualCityRandomTests` (te same
  scenariusze z Random(zatoka), „drogi” czytane z zamrożonej gry), tabele `BatchHundredTwentySix` jako
  `Dual/HandleCityCaseTables.cs` (sprawdza je `DualCityCaseTests`), `Reference/CityManagementReference.HandleCity.cs` usunięty.
  Stos HandleCity nie jest już porównywany, więc jej zmienne lokalne można przenosić.
- B2, porcja 3 (2026-10-23): zmienne lokalne `HandleCity` od `BP-0xa8` w górę przeniesione do pól klasy `CityRound` w `CityManagement` (każde wywołanie `HandleCity` ma własną instancję z racji rekurencji przez doradcę; dolne słowa ramki zostają, bo leżą w nich tablice `BP-0x3e`, `BP-0x74`, `BP-0x8e` i niezainicjowane słowa czytane przed zapisem jak `0x108`). W `DualGame` licznik kroków zegara (`FixedClockSpan`) jest zerowany na każdym wywołaniu funkcji gry po obu stronach, żeby mniejsza liczba dostępów do pamięci (po wyjęciu zmiennych lokalnych ze stosu) nie przesuwała momentu przerwania zegara podczas wygaszania palety (`0xbd06` / `0xbdee` przy widoku miasta). GoldenMaster i wszystkie testy Dual (49 testów) zielone.
- B2, porcja 4 (2026-10-23): zmienne lokalne wspólnych ramek przeniesione do zmiennych C#, parametrów i tablic C# w czterech modułach: `DivideCivilization` w `CivilizationSchism` (57 dostępów do ramki + tablica `continentShares` z `BP-0x44`), `DiplomatArrivesAtCity` w `DiplomatActions` (13 dostępów, `ownerIdx` jako parametr pomocników), `UncoverTechnology` w `ScienceAndAdvisors` (24 dostępy + tablica `choiceList` z `BP-0x3a`; bufor `pictureName` pod `BP-0x10` zostaje dla `CopyText` / `ImportPictureOrColors`) oraz pomocniki `OpeningSequence` (`SmoothDiagonalCoastlines`, `AssignLatitudeAndElevationTerrain`, `ApplyMoisturePasses`, `AgeTerrain`) w `GameInitAndIntro` (38 dostępów). GoldenMaster, Dual i testy różnicowe porcji zielone.
- B2, porcja 5 (2026-10-23): usunięte zmienne lokalne i tablice ramki stosu w trzech kolejnych modułach (82 dostępy): `Pathfinding` (20 dostępów w `StepTowardsGoal`, `ChooseGotoStep`, `SearchRouteBlocks`, `ChartWindowCosts`, `ChooseCheapestStep`, `CrowdAndDistance`; zostaje tylko niezainicjowany odczyt `0xa` w `SearchRouteBlocks`), `TradeAndConquest` (19 dostępów + tablica `choices` z `BP-0x94` przeniesiona do `short[0x48]` w `SeizeCity`, `SplitCapturedCivilization`, `MarkCapturedCity`, `ChooseTechnologyToTake`; zostaje tylko początkowy odczyt `0x4` w `ChooseTechnologyToTake`), `TurnEvents` (43 dostępy + tablice `ranking` z `BP-0x14` w `NoteStatusInReplay` i `powers` z `BP-0x16` w `RefreshPeaceGraphContent` przeniesione do tablic C#, usunięte martwe zapisy w `LandBarbariansFromSea`, `RaidHumanCity`, `RefreshPeaceGraphContent`, `RenderPalace`, `HandleEndOfTurnEvents`, `TryRivalWonder`; zostają niezainicjowane odczyty `0x2` w `RaidHumanCity` i `0x1a` w `RefreshPeaceGraphContent`). GoldenMaster, Dual i testy różnicowe porcji zielone.
- B2, porcja 6 (2026-10-23): usunięte zmienne lokalne i tablica ramki stosu w `AIPlayer.OperateAITurn` i pomocnikach (`CountUnits`, `CountUnit`, `CountCities`, `CountOwnCity`, `CountUsedBlocks`, `ChooseContinentApproaches`, `ChooseContinentApproach`, `DirectUnits`, `GiveUpObsoleteUnit`, `SendUnitToBestLocation`, `LocationFits`, `WeighLocation`, `MayTakeLocation` – 47 dostępów i tablica `LocationSentAddress` spod `BP-0x44` przeniesione do zmiennych C#, parametrów i `ushort[0x20]`), w `GameInitAndIntro` (`ProduceTerrain`, `LabelLandmasses`, `ReckonContinentSizes`, `LinkSeaBlock`, `SeaPassageOpen` – 39 dostępów; w `LabelLandmasses` i `ReckonContinentSizes` zostaje tylko słowo `0xe` czytane przed zapisem przy sąsiedzie bez morza) oraz w `CityManagement.RenderCitizenIcons` (3 martwe zapisy usunięte; zostaje odczyt `0x2`). GoldenMaster, Dual i testy różnicowe porcji zielone.
- B2, porcja 7 (2026-10-23): wyczyszczone zmienne lokalne i tablice ramek stosu w 12 modułach UI/Engine (~290 dostępów do ramki mniej): `MeetWithKing` (`LaunchAudience` i pomocniki – wszystkie zmienne lokalne `FrameWord` i tablica `EnemyTable` spod `BP-0x1e` przeniesione do pól/zmiennych C# i tablicy `short[8]`, usunięte martwe zapisy w `ChoreographKingSpeaking`, `AdvanceKingGesture`, `DisplayKingScreen`, `ImportKingScenery`; 0 dostępów do ramki w pliku), `DialogDrawing` (usunięte 42 martwe zapisy w `DisplayMenu`, `MeasureAndDrawMenuFrame`, `RenderMenuLines`; zostaje tylko zapis słowa `0x2` w `DisplayMenu` i `ObtainNavigationKey`, bo `RenderCitizenIcons` wołane z `HandleCity` na tej samej głębokości stosu czyta pozostawione tam słowo `0x2`), `TextBlockRenderer` (usunięte 7 martwych zapisów w `ImportTextFromFile`; 0 dostępów w pliku), `StartGameMenu` (tablica `points` w `CharacterizeStartLocation` przeniesiona do `short[8]`, usunięte nieużywane tablice `settlersPerContinent`/`continents` i 26 martwych zapisów; 0 dostępów w pliku), `WorldMap` (usunięte wszystkie 25 zapisów w `DisplayWorldMap`, `RenderWorldMapTile`, `DisplayEpicChronicle`; 0 dostępów w pliku), `Palace` (usunięte 15 dostępów w `ImportPalaceGraphics`, `RenderPalaceBush`, `ImportPalaceCastle`; zostają tylko `0x12` i `0x18` czytane bez inicjalizacji), `AdvisorReports` (tablica `costs` w `DisplayTradeReport` przeniesiona do `short[0x18]`, usunięte 19 martwych zapisów; zostaje `rowPlayers` pod `BP-0x16` czytana dla pustych wierszy w `DisplayMilitaryReport`), `GameReplay` (usunięte 13 martwych zapisów; zostaje odczyt `0x4` w `ObtainDistanceToReplayCity`), `CityView` (tablice `pictures` przeniesione do `ushort[]`, usunięte 14 martwych zapisów; 0 dostępów w pliku), `SpaceshipScreen` (usunięte 9 martwych zapisów; zostaje `0x16` w `MoveSpaceshipStars`), `Newspaper` (zastąpiony odczyt `0x7` przez `topic + 5` i usunięte 4 martwe zapisy; 0 dostępów w pliku), `NewCityScreen` (usunięte 4 martwe zapisy w `PullTextInput`; 0 dostępów w pliku). GoldenMaster, Dual i testy różnicowe porcji zielone.
- B2, porcja 8 (2026-10-23): `ScienceAndAdvisors.PickCityProduction` i pomocniki (`ListProductionChoices`, `ListUnitChoices`, `ListImprovementChoices`, `ListWonderChoices`, `AssessCityNeeds`, `AssessCityDefence`, `Score*Choice`, `DecideProduction`) – zmienne lokalne do pól/zmiennych C#, tablice ramki do `choiceTable`, `makingElsewhereTable`, `unitsOnContinentTable`; zostaje tylko zapis słowa `0x2` w `AssessCityNeeds` (czyta je `RenderCitizenIcons`). Uwaga: Escape w menu (`DisplayMenu` = 0xffff) wybiera wpis 0 – w oryginale przez zawinięcie 16-bitowego adresu ramki, w C# przez `(short)picked + 1`. Usunięte 4 martwe zapisy w `HandleAllCities` i `AnnounceTechnology`. `DiplomatActions` – usunięte wszystkie 19 zapisów i tablica `pictures` w ramce (`RenderExplosion`, `ComputeInciteCost`, `Revolt`, `ExamineCity`). `BatchSeventyFiveDifferentialTests` nie porównuje już ramki `PickCityProduction`. GoldenMaster, Dual i testy różnicowe porcji zielone.
- B2, porcja 9 (2026-10-23): `PlayerTurn.HandlePlayerTurn` i 65 metod pomocniczych – usunięte wszystkie odwołania `PullLocal*` i `EmitLocal*` (ponad 630 dostępów do ramki stosu `0x52`), przeniesione do właściwości i pól klasy `TurnLocals`. Zmienne czytane przed zapisem lub współdzielone przez nieczyszczoną ramkę stosu DOS (`0x12` `CursorVert`, `0x24` `Direction`, `0x36` `MouseCursorSet`, `0x3e` `TargetTerrain`) zachowują fizyczny zapis na stos przy modyfikacji, a `TurnLocals.FromFrame` inicjalizuje stan na wejściu w ramkę. W 17 plikach testów różnicowych (`BatchNinety*` – `BatchHundredEleven*`) wycofano porównywanie ramki stosu i dodano właściwość `ActiveTroop`. Wszystkie 50 testów (17 batchy różnicowych PlayerTurn, 11 DualTurnsAuto, DualCity, ScenarioGoldenMasterTests) zielone.
- B2, poprawka porcji 6 i 7 (2026-10-24): dwie zmiany zachowania znalezione pełnym zestawem (porcje sprawdzano tylko
  własnymi testami): `HandleGameEnd` (`ChoreographUnitMove(…, 8)` → 40959) i `DisplayAdvisorMenu` (ikony mieszkańców
  w raporcie nastrojów 2 piksele w bok). Przyczyna: zapisy uznane za martwe karmiły niezainicjowane odczyty innych funkcji
  na tej samej głębokości stosu. Sonda (ostatni zapis każdego słowa stosu + odczyty 0x12 w `PlayerTurn` i 0x2
  w `RenderCitizenIcons`) wskazała: tablicę „wysłanych” `DirectUnits` pod `BP-0x44` ramki `OperateAITurn` (jej słowa
  czyta `HandlePlayerTurn` jako 0x12, 0x24, 0x36, 0x3e) – zapisy na stos przywrócone obok tablicy C#
  (`LocationSentAddress`); `DisplayMenu` 0x6 (klawisz) i końcowe słowa ramek `DisplayTradeReport` (tablica `costs`
  pod `BP-0x34`) i `RenderMilitaryReportRow` (0x1a) – `DialogDrawing.cs` i `AdvisorReports.cs` przywrócone do stanu
  sprzed porcji 7 (zmiany tam tylko usuwały zapisy). Wniosek: w B2 zapis do ramki jest martwy dopiero, gdy nikt później
  nie czyta tego słowa stosu jako śmieci – po każdej porcji B2 pełny zestaw.
- B2, sonda odczytów śmieci (2026-10-24): narzędzie testowe `Tests/Dual/LeftoverWatcher.cs` (punkt zaczepienia
  `IStackWatcher` w `CPU`: ramki, odczyty i zapisy segmentu stosu) – każdy odczyt słowa ramki przed jego zapisem jest
  liczony z ostatnim autorem tego słowa i wartością; `CIV_LEFTOVERS=plik` włącza raport, `compare_leftovers.py` porównuje
  dwa raporty (czytelnicy z innymi wartościami i utraconymi autorami). Raporty dla każdego commitu B2 i jego rodzica
  (GoldenMaster + Differential + Dual) pokazały utracone zapisy w prawie każdej porcji, więc: cofnięte w całości
  B2 porcja 1 (0881a2a: `MapManagement.RenderMapTile` karmił `LocateNearestFriendlyCity` 0x14, `ShiftAIUnit` 0x21 / 0x22 /
  0x52, `HandlePlayerTurn` 0x24, `LaunchAudience`; `Pathfinding`, `UnitCombat`, `CityFoundingAndScore`,
  `GovernmentAndDiplomacy`), porcja 5 (c0a623f: końcowe zapisy `RenderPalace`, `RefreshPeaceGraphContent`,
  `ChooseTechnologyToTake`), porcja 8 (f5be1a7: ramka `PickCityProduction`, `RenderExplosion`); z porcji 6 cofnięte
  `CityManagement` (`RenderCitizenIcons`) i `GameInitAndIntro` (`ReckonContinentSizes` 0x8), z porcji 7 `TextBlockRenderer`
  i `Newspaper` (oraz wcześniej `DialogDrawing`, `AdvisorReports`); testy `BatchSeventyFive` i `BatchThirty` znów porównują
  te ramki. `CityRound` (HandleCity) i `TurnLocals` (HandlePlayerTurn) zapisują każdą wartość także do ramki (`Keep`) –
  nazwy zostają, obraz stosu jak w oryginale (0x108 zagnieżdżonej HandleCity dostawał 0xffff z 0xe8 zewnętrznej).
  Zostaje z B2: porcje 3, 4, 9 (z zapisami do ramki), AIPlayer z porcji 6 (z kopią tablicy pod `BP-0x44`), część porcji 7.
  Porównanie z raportem sprzed B2 (125a179): jedyny inny autor to `HandleCity` 0x108 (liczy się tylko 0xffff, ten sam).
  Uwaga do raportów: wartości tego samego czytelnika potrafią się różnić bez utraconego autora (przesunięcie zegara
  testów, inna kolejność) – znaczenie ma utracony autor; `HandlePlayerTurn` czyta całą ramkę na wejściu (`FromFrame`),
  więc ma dodatkowych autorów.
- B2, reszta porcji 7 sprawdzona (2026-10-24): raport ma teraz pokrycie (`#frame Funkcja<tab>ile` – ile razy testy
  otworzyły ramkę funkcji). Wszystkie 30 funkcji z ramką w `CityView`, `GameReplay`, `MeetWithKing`, `NewCityScreen`,
  `Palace`, `SpaceshipScreen`, `StartGameMenu`, `WorldMap` testy uruchamiają (10 – 38 tys. razy). Żaden czytelnik nie
  dostaje innych wartości; jedynie `MeetWithKing` (`LaunchAudience`, `ChoreographKingSpeaking`) przestał być ostatnim
  autorem bajtów buforów tekstu w `DisplayNewspaper`, `DisplayPalace`, `DisplayCityScreen`, ale te same bajty zostawia
  tam inna funkcja (ten sam tekst) – wartości identyczne przed porcją 7, po niej i teraz. Porcja 7 w tych plikach zostaje.
- B2, porcja 10 (2026-10-24): zmienne lokalne `ShiftAIUnit` (ramka 0x5e, 28 pomocników w `AIPlayer`: 412 odczytów,
  183 zapisy, 9 kopii, 4 inkrementacje, zapis bajtu 0x5e) jako nazwane właściwości klasy `UnitLook`, które czytają
  i zapisują słowa ramki na stosie dokładnie jak oryginał (`Horiz` 0x30, `BestScore` 0x28, `Harbour` 0xc …). Ta postać
  nie zmienia ani wartości, ani liczby dostępów do pamięci (zegar testów), ani odczytów śmieci: raport sondy po zmianie
  identyczny co do bajtu z raportem przed nią; pełny zestaw zielony; 8 mutacji offsetów w `UnitLook` (testy
  `ShiftAIUnit…MatchOriginalCode`). Od teraz B2 w tej postaci (nazwy na ramce), a zmienne C# bez ramki dopiero po decyzji
  o zmianach zachowania.
- B2, porcja 11 (2026-10-24): dolna ramka `HandleCity` (słowa poniżej BP-0xa8 i 0x108) jako nazwane właściwości
  `CityRound` na stosie (`Workers` 0x50, `TileFood` / `TileShields` / `TileTrade` 0x4 / 0xc / 0x46, `UnitsDrawn` 0x4e,
  `Found` 0x108 …): 120 odczytów, 57 zapisów. Tablice ramki (BP-0x3e, BP-0x74, BP-0x8e) zostają adresowane przez BP.
  Raport sondy identyczny co do bajtu, pełny zestaw zielony, 8 / 8 mutacji.
- B4, porcja 1 (2026-10-24): zmienne globalne jako nazwane właściwości `GameModule` (region „Global variables”), które
  czytają i zapisują segment danych dokładnie tam, gdzie oryginał (te same dostępy – zegar testów i raport sondy bez
  zmian): pudełka ekranów `ScreenBox` 0xaa, `BackScreenBox` 0x19d4, `PictureScreenBox` 0x19e8 (763 miejsca) i wyniki tury
  miasta `CityFood` / `CityShields` / `CityTrade` / `CityLuxuries` / `CityHappy` / `CityUnhappy` / `CityScience`
  0x70da – 0x70e6, `CityOwner` 0x6548, `CityTiles` 0x6540, `CitySpecialists` 0xe8b8, `FoundDistance` 0x6c9a,
  `ContentAdvances` 0xb882, `HumanTileNear` 0x70e8 (220 odczytów, 47 zapisów). Skrypt: lista adresów z nazwami → zamiana
  w klasach `GameModule` (inne klasy, np. sterowniki, zostają z adresami). Pełny zestaw zielony, raport identyczny,
  8 / 8 mutacji (`PictureScreenBox` łapie dopiero test podwójnej gry – golden master nie porównuje ekranów).
- B4, porcja 2 (2026-10-24): kolejne 14 zmiennych globalnych w `GameModule`: `CityScreenTab` 0x2496, `DialogStyle`
  0x2f9e, `GoalHoriz` / `GoalVert` 0x6590 / 0x6592, `KingStep` 0x67f4, `KingSpeaking` 0xd76c, `TextCursor` 0x68cc,
  `TurnPlayer` 0x6b90, `DisabledLines` 0xb276 (z `DisabledLinesLow` dla zapisów bajtu), `TechnologyLevel` 0xd2de,
  `VgaMode` 0xd762, `FoundPlayer` 0xd7f0, `MapRevealed` 0xd806, `GameEndRequest` 0xdc48 (228 odczytów, 122 zapisy;
  `GameMessage` spoza `GameModule` zostaje z adresem 0x2f9e). Pełny zestaw zielony, raport identyczny, 8 / 8 mutacji
  (dwie dopiero jako odwrócenie wartości: sąsiedni adres ma w testach to samo 0). Pułapka skryptu: plik czytany
  binarnie (`

`) i zapisany w trybie tekstowym dostaje `


` – narzędzie mutacji odrzuca wtedy łatkę; zapis z
  `newline=''`.
- B4, porcja 3 (2026-10-24): 17 zmiennych globalnych: `MapViewHoriz` / `MapViewVert` 0xd4cc / 0xd75e, `CityCorruption`
  0xd2e0, `CityUnitUpkeep` 0xd2f6, `CityTaxes` 0xe17a, `CitySettlers` 0xe3c6, `CityAttackersAway` 0x6546,
  `CitySpecialSlots` 0xdeb8, `DefaultLine` 0x2f9a, `MenuHelpTopic` 0x2f9c, `ChosenOrder` 0xd4ca, `KingAnswer` 0x3934,
  `MainMenuChoice` 0x6b32, `ReplayMode` 0x680a, `ReplayActor` 0x6810, `MousePresent` 0x1a3c, `PointerShowLevel` 0xdeea
  (181 odczytów, 89 zapisów). Pełny zestaw zielony, raport identyczny, mutacje (odwrócenie wartości) złapane – zapis
  `ChosenOrder` łapie test paska menu, odczyt test tury gracza.
- B4, porcja 4 (2026-10-24): `StoryState` 0x3b62, `StoryLineTime` 0x6806, `SaveDrive` 0x4384, `MapRedrawRequest`
  0x6b64, `PollutionFactor` 0x6c98, `CityAgain` 0xb1e8, `WorldMapView` 0xb278 (tylko odczyt), `UnitAtTilePlayer` 0xd20a
  (51 odczytów, 32 zapisy). Pełny zestaw zielony, raport identyczny, 8 / 8 mutacji (`MapRedrawRequest` czyta
  `RenewCityScreen`, test `DialogDifferentialTests.DialogsMatchOriginalCode`).
- B4, porcja 5 (2026-10-24): `ArrivedSpaceship` 0x1cf6, `StackKind` / `StackTally` 0x653a / 0x653c (wywołania
  `AddToGlobal(0x653c, …)` zostają z adresem), `ShownCity` 0x653e, `MenuResult` 0x654a, `GotoCost` 0x6794,
  `PalaceDistance` 0x6b30, `ConquestEnd` 0xb884, `EmissaryAudience` 0xd804, `UnitsHidden` 0xdcfc, `FirstMissingDisk`
  0xdeba (36 odczytów, 29 zapisów). Pełny zestaw zielony, raport identyczny; mutacji nie robiono (sesja zatrzymana po
  testach na prośbę użytkownika). Następna porcja przygotowana w scratchpadzie (`m_b4f.json`: PopulationMark,
  BirthPicture, AudienceKing, KingPause, ReplayCityDistance, CityNamesShown, KingPose, Climate, ScoreIconHoriz /
  ScoreIconVert, TextPass, EarthGame, SpaceshipColonists, MidTurnSave, LastSaveSlot).
- B4, porcja 6 (2026-10-24): `KingMood` 0x3936 (nastrój króla wybierający gest, 0xffff poza audiencją – okienka mają
  wtedy własną wysokość wiersza), `CodeText1` – `CodeText4` 0x30b8 – 0x30be (adresy tekstów kodów; odczyt z wyliczanym
  adresem w `TextBlockRenderer` zostaje), `MenuLineHeight` 0x2fa4, `MenuRightEdge` 0xde0e, `MenuLineCount` 0x658e,
  `PalacePartWidth` 0x4b2e, `KingPicture` 0x67f8, `StackDomainTally` 0x6532 (`AddToGlobal` zostaje z adresem) – 79
  odczytów, 38 zapisów, skrypt `b4f.py` w scratchpadzie (pułapka: zapisy w plikach CRLF – `$` w regexie stoi za `\r`).
  Pominięte, bo znaczenie niejasne albo podwójne: 0x6b8e (audiencja), 0x6542 (nadmiar obywateli / naukowcy), 0xdb38,
  0x6802 (blisko zmiennych zegara), 0x20f4, 0x3c62. Pełny zestaw 767/767. 9 / 9 mutacji w miejscach użycia (menu:
  test `OperateAndDisplayMenuMatchOriginalCode`, nie `DialogsMatchOriginalCode`; szerokość pałacu:
  `ImportPalaceGraphicsMatchesOriginalCode`, `DisplayPalace` jej nie czyta; `StackDomainTally`: golden master).
- B4, porcje 7 – 9 razem (2026-10-24, na prośbę użytkownika: trzy porcje i raz testy), 33 zmienne, 95 odczytów,
  58 zapisów, skrypt `b4g.py` + `names_b4g.py` w scratchpadzie.
  Porcja 7: `PopulationMark` 0x1c24, `LastSaveSlot` 0xe168, `MidTurnSave` 0xdf60, `SpaceshipColonists` 0xdeae,
  `EarthGame` 0xd76a, `Climate` 0x7efa, `KingPause` 0x67f6, `ReplayCityDistance` 0x6812, `ScoreIconHoriz` /
  `ScoreIconVert` 0xb25c / 0xb2bc, `BirthPicture` 0x3b64.
  Porcja 8: `MenuWidth` 0xd4c8, `MenuFirstChoice` 0xb1ec, `MenuChoiceCount` 0xd208, `MenuMeasuring` 0xd2e2,
  `MenuFlashHue` 0x654c, `MenuOptions` 0xd7f2, `PalaceFirstPicture` 0x6828, `PalaceLeftBushes` 0xd930,
  `StoryCycleRecord` / `StoryCycleCount` 0x3b66 / 0x3b68.
  Porcja 9: `CountLosses` 0x70d8, `StackLeader` 0x6530, `KingPose` 0x7ef4, `AudiencePlayer` 0x67f2,
  `AudienceWithoutPictures` 0x67f0, `ReplayFile` 0x680c, `CityLabelCount` 0x6c96, `HumanContinentAttackers` 0x2a6e,
  `DiskInUse` 0xe17c, `HallOfFameScore` 0xb77e, `PointerPicture` 0xd4dc, `UnitLimitTold` 0xd760.
  Pominięte (znaczenie niejasne): 0x6ed6 / 0x70ea, 0xde20, 0xb22a, 0x681a, 0x3484, 0xe3c2 / 0xdb42, 0xd206; bajty
  0xba0c (w buforze 0xba06), 0x1a22, 0x5403 (`SystemServices` nie jest `GameModule`), 0xb1d4.
  Pełny zestaw 767/767 (jeden przebieg na trzy porcje), mutacje 13 / 13 (w drugiej rundzie 5 z innymi testami: zapis
  `PalaceFirstPicture` łapie `ImportPalaceCastle`, nie `ImportPalaceGraphics`; `Climate` – `OpeningSequence`, nie
  `MapMaking`; `AudiencePlayer` – odczyt w `ChoreographKingLikeness`; `CityLabelCount` – `RenderMapTile`, nie
  `FocusMapView`; `PopulationMark` – `RenderStatusBar`, nie golden master).
- B4, porcje 10 – 14 razem (2026-10-24, na prośbę użytkownika: pięć porcji i raz testy), 40 zmiennych, 50 odczytów,
  31 zapis, skrypt `b4h.py` + `names_b4h.py` w scratchpadzie. Zostały już głównie adresy z 1 – 3 użyciami.
  Porcja 10 (historia, nowy świat, ekrany): `StoryFile` 0x6804, `StoryKeyAllowed` 0x67fc, `LandMass` /
  `Temperature` / `WorldAge` 0x7ef6 / 0x7ef8 / 0x7efc, `ScreenReserveFailed` 0x6dfc, `ScreenRepaintRequest` 0x1ae0,
  `CodeText5` 0x30c0, `EgaMode` 0xb20e.
  Porcja 11 (audiencja): `SavedScreenStyle` 0x67ec, `KingPictureLoaded` 0x67ea, `AdvisorPoseShown` 0x67e8,
  `KingScenePicture` 0xb1e6, `KingFacesPicture` 0x7efe.
  Porcja 12 (statek): `SpaceshipPropulsion` 0xdea8, `SpaceshipFuel` 0xdeaa, `SpaceshipSupport` 0xdeb0,
  `SpaceshipEnergy` 0xdeb2, `SpaceshipComponentsComplete` / `SpaceshipModulesComplete` 0x687c / 0x687e,
  `SpaceshipPicture` 0x682a.
  Porcja 13 (pliki, powtórka, Civilopedia, wpisywanie tekstu): `FileError` 0x681c, `SavedHumanPlayer` /
  `SavedDifficulty` / `SavedYear` 0x6822 / 0x6824 / 0x6826, `ReplayAtGameEnd` 0x680e, `CivilopediaNextPage` 0x6808,
  `SavedPointerShowLevel` 0x652e, `TextCursorColumn` / `TextCursorWidth` 0x68d0 / 0x68ce.
  Porcja 14 (jednostki, trasy, wojna, pałac, wynik): `StackDomainMode` 0x6536, `StackIndexLimit` 0x6538,
  `RoutePlaceHoriz` / `RoutePlaceVert` 0x6796 / 0x6798, `PlayerAtWar` 0x6ed4, `NoConquestAnimation` 0x28bc,
  `PalaceRightBushes` 0xd934, `WonderScore` / `CitizenScore` 0xd764 / 0xd766.
  Pominięte (niejasne): 0x2fa0, 0x2fa2, 0x2f98, 0x19fc, 0x8078, 0x67ee, 0x6534, 0x2fec, 0x2494, 0x6e92, 0x6b92,
  0x67fe, 0x67fa, 0x4b32, 0x1c28, 0xdc6a, 0x5116, 0x1a3e, 0x681e, 0x6834; 0x5876 / 0x5872 (`SystemServices`),
  0x687a (bajt).
  Pełny zestaw 767/767 (jeden przebieg na pięć porcji), mutacje 13 / 13 (dwie dopiero z innym testem:
  `SpaceshipSupport` – `RenderSpaceship`, nie `ComputeSpaceshipSuccess`; `CivilopediaNextPage` – `DisplayCivilopedia`,
  nie `DisplayCivilopediaIndex`).
- B4, porcje 15 – 19 razem (2026-10-24, pięć małych porcji i raz testy), 20 zmiennych, 52 odczyty, 54 zapisy, skrypt
  `b4i.py` + `names_b4i.py` w scratchpadzie. Znaczenie ustalone z dokładniejszego czytania kodu.
  Porcja 15 (okienka, pliki, Civilopedia): `MenuAnyKey` 0xd206, `MenuFooter` 0x2fa0, `LoadingGame` 0x681a,
  `LoadFile` 0x681e, `CivilopediaWithoutPictures` 0x3c62.
  Porcja 16 (tura miasta, ustrój komputerów): `CityExcessUnhappy` 0x6542, `AnarchyPressure` 0xe3c2,
  `DemocracyPressure` 0xdb42, `QuietUnitSetup` 0x20f4.
  Porcja 17 (widok mapy, nowy świat): `MapFocusLeft` / `MapFocusTop` 0x6ed6 / 0x70ea, `SpriteScreenBox` 0x19fc,
  `LightenedPixels` 0x67fe, `LowMemoryVga` 0x1a3e.
  Porcja 18 (wskaźnik myszy): `PointerImage` 0x5876, `PointerHotHoriz` / `PointerHotVert` 0x5878 / 0x587a.
  Porcja 19 (audiencja): `AudienceWelcomed` 0x6b8e, `KingModernPictures` 0x67ee, `GovernmentPicture` 0x804e.
  Pełny zestaw 767/767 (jeden przebieg), mutacje 11 / 11 (wskaźnik myszy bez testu różnicowego – zostaje golden
  master; pułapka: `check_mutations.py` czyta pliki z `\n`, więc kontekst mutacji pisać z `\n`, nie `\r\n`).
  **Stan B4 po tej rundzie:** w klasach `GameModule` zostały już prawie wyłącznie elementy tabel z CIV.EXE (np. nazwy
  cywilizacji 0x1982 / 0x1992 / 0x19a2, tabele 0xc10 – 0xca6, 0x81ae – 0x81ce, teksty 0x33a8 / 0x33aa – to B5),
  pojedyncze uchwyty obrazków o niepewnym przeznaczeniu (0xd4da, 0xb880, 0x6e9e, 0xb2ba, 0xdf0c, 0x6834, 0x32d6,
  0x817a) i flagi bez jasnego znaczenia (0x8078, 0xdb38, 0x6802, 0x3484, 0x2494, 0xde20, 0xb22a, 0x6b92, 0x67fa,
  0x6534, 0x2fec, 0x2fa2, 0x2f98, 0x1c28, 0xdc6a, 0x5116, 0x8066, 0xd2dc, 0x6820, 0x654e, 0x4b2c / 0x4b32 / 0x4b34,
  0xd922, 0x6e92) oraz dostępy bajtowe. Dalej B4 opłaca się tylko razem z B5 albo przy przepisywaniu tych funkcji.
- **B3 odłożone na koniec migracji** (decyzja użytkownika 2026-10-24, powód w `PLAN-PO-MIGRACJI.md`, B3): 30 funkcji czyta
  617 słów stosu zostawionych przez inne, więc każda zmiana ramek i SI/DI zmienia decyzje AI. Raport sondy „przed”
  i skan funkcji: scratchpad tej sesji (`lo_before.txt`, `b3scan.tsv`) – przy powrocie do B3 nagrać od nowa.
- B5, porcje 1 – 3 razem (2026-10-24): tabele słów z CIV.EXE jako akcesory `GameModule` (region „Tables of CIV.EXE”):
  `Nazwa(indeks)` = `PullGlobalWord((ushort)((indeks << 1) + baza))`, ten sam rodzaj dostępu (każdy odczyt CPU liczy
  się do zegara testów raz, `PullInt16` → `(short)Nazwa(...)`). `CivilizationName` 0x1982, `CivilizationPlural` 0x1992,
  `LeaderName` 0x19a2, `GovernmentName` 0x1966, `GovernmentTitle` 0x19b2, `PlayerColour` 0x1946, `DirectionHoriz` /
  `DirectionVert` 0x1882 / 0x18e4, `SpritePicture` 0xd4ce – 221 odczytów, skrypt `b5a.py` w scratchpadzie (forma
  `(ushort)((i << 1) + baza)` i `(i << 1) + baza`). Zostają z adresem: odczyty z już podwojonym indeksem (`names`,
  `personSlot`, `direction`, `dirOffset`, SI/DI/BX jako przesunięcie – ok. 80) i odczyty bajtowe; do przerobienia razem
  z tymi zmiennymi.
- B5, porcja 4 (2026-10-24, razem z 1 – 3 i jednym przebiegiem testów): adresy nazw w tabelach rekordów jako funkcje
  bez dostępu do pamięci (sama arytmetyka): `TechnologyText(i)` = 0x16 * i + 0x4da, `BuildingText` 0x1e * i + 0xbb8,
  `WonderText` 0x1e * i + 0xe6a, `UnitText` 0x22 * i + 0x112a – 44 miejsca w 7 postaciach rzutowań (skrypt `b5b.py`).
  Zostaje 8 z przesunięciem policzonym wcześniej (SI, `otherAddr`, `unitSpec`) i prywatne `MeetWithKing.TechnologyName`
  (duplikat `TechnologyText`). Następne: rekordy przywódców (0x14e2 / 0x14f2 / 0x1502 / 0x1516 / 0x1518 / 0x151a co
  0x3a – prawie wszystkie przez przesunięcie `leader` / `names`, więc razem z tymi zmiennymi), budynki 0xbb2 co 0x1e,
  teren 0x28e co 0x13, surowe odczyty tabeli jednostek 0x112a co 0x22 obok `UnitKindTable`.
  Porcje 1 – 4: pełny zestaw 767/767 (jeden przebieg), mutacje 13 / 13 (indeks ^ 1 w akcesorze; `DirectionVert` –
  golden master, zawieszenie = złapana).
- B5, porcje 5 – 8 razem (2026-10-24, reszta B5 naraz i raz testy), 172 miejsca, skrypt `b5c.py` w scratchpadzie
  (parser nawiasów: adres `(ushort)(L + baza)`, L = indeks rekordu w 7 zapisach mnożenia albo przesunięcie policzone
  wcześniej; odczyt = cały argument `PullGlobalWord` / `PullUInt16` / `PullInt16` / `PullUInt8` / `PullGlobalByte`,
  także przez `dataSeg` – zawsze `SegData`). Stałe pól: `UnitKindTable.Technology` (0x20), nowe `BuildingTable`
  (0xb9a co 0x1e: `Price`, `Upkeep`, `Technology`), `TerrainTable` (0x282 co 0x13: `MoveCost`), `LeaderTable`
  (0x14e2 co 0x3a: `Name`, `CivilizationPlural`, `Civilization`, `Leaning`, `ShortTune`, `Tune`) w
  `Game/RecordTables.cs`. Akcesory `GameModule`: `UnitKindWord(At)`, `BuildingWord(At)`, `TerrainField(At)` (bajt),
  `LeaderWord(At)`; adresy `UnitTextAt`, `BuildingRecordText(At)`, `TerrainText(At)`, `LeaderText(At)`; przez
  przesunięcie `CivilizationNameAt` … `SpritePictureAt`. 0xbd0 / 0xbd2 = pole rekordu `i + 1`.
  **Pułapka (złapana kompilacją):** rzutowanie odczytu ze znakiem (`PullInt16` / `PullInt8` → `(short)` / `(sbyte)`)
  trzeba rozpoznać po nazwie metody, nie po końcu prefiksu z segmentem – pierwsza wersja skryptu je gubiła (zmiana
  wartości ujemnych bez błędu kompilacji w arytmetyce); zmiany cofnięte i zrobione od nowa, 15 / 15 odczytów ze znakiem
  sprawdzonych parami. Zostają z adresem: bajty tabeli kierunków (`PullUInt8` 0x1882 / 0x18e4), adresy „slotów”
  (`nameSlot`, `pluralSlot`, `leader` w `MeetWithKing`, `colour` w `SpaceshipScreen` – zapisywane dalej jako adres),
  wieloliniowy odczyt obrazka w `Civilopedia`, `0x16d2` (cywilizacja rekordu przywódcy + 8) i `AIPlayer.TerrainByte`
  (pola 0x28e / 0x28f terenu przez własne przesunięcie).
  Pełny zestaw 767/767 (jeden przebieg), mutacje 14 / 14: 10 z 12 w akcesorach od razu; `LeaderWord` / `LeaderWordAt`
  na melodiach (`Tune`) przeżywają – `PerformSound` nie zostawia śladu w testach (równoważne) – więc sprawdzone na polu
  `Leaning` (`ConstructCivilization`, golden master przez `CityManagement`).

- B6.8, porcja 1 (2026-10-26): bufory nazw plików i tekstów przeniesione ze stosu do C# w `TurnEvents.DisplayCivilizationScenery`, `SpaceshipScreen.ImportSpaceScenery`, `Palace.ImportPalaceGraphics` (`RenderPalaceBush`, `ImportPalaceCastle`) oraz `GameLoadAndSave.ImportGame`. W `ImageTools` przeciążenia `ImportPictureOrColors` i `ImportIcon` przyjmujące `string`; w `CRuntime` metoda `ReadText`. Usunięto 5 ramek stosu (`EnterFrame`/`LeaveFrame`), pozostało 46. 8 z 8 mutacji złapanych (BatchEleven, BatchTwelve, BatchFortyTwo, BatchThirty, BatchThirtyTwo), Golden Master 16/16 zielony.
- B6.8, porcja 2 (2026-10-26): bufory nazw plików graficznych przeniesione ze stosu do C# w `StartGameMenu.ImportGovernmentGraphics` (`govt{regime}{tech}.pic`), `MeetWithKing.DisplayKingScreen` / `NameAdvisorsPicture` (`govt{regime}{tech}.pic`, `king{tens}{ones}.pic`), `MeetWithKing.ImportKingScenery` (`back{regime}{tech}.pic/.pal`, `king{tens}{ones}.pal`) oraz `GameInitAndIntro.DisplayStoryIntro` / `ShowStoryPicture` (`birth{N}.pic/.pal`). Usunięto 4 ramki stosu (`EnterFrame`/`LeaveFrame`), pozostały 42. 8 z 8 mutacji złapanych (BatchEight, BatchTwenty, BatchFortyOne, BatchFiftyFour), Golden Master 16/16 zielony.
- B6.8, porcja 3 (2026-10-26): bufory nazw plików i tekstów przeniesione ze stosu do C# w `GameReplay.ObtainReplayPlayerLabel` (bufor nazwy w liczbie mnogiej), `GameReplay.DisplayCivilizationDestroyedScreen` (`king{tens}{ones}.pic/.pal`, `slam2.pic`/`slag2.pic`) oraz `NewCityScreen.DisplayLabelInputBox` (bufor nazwy miasta na stosie do porównywania prefiksów). Usunięto 3 ramki stosu (`EnterFrame`/`LeaveFrame`), pozostało 39. 8 z 8 mutacji złapanych (BatchEight, BatchForty, BatchSixteen), Golden Master 16/16 zielony.
- B6.8, porcja 4 (2026-10-26): bufory i ramki stosu przeniesione do C# w `TextBlockRenderer.ReflowText` (bufor `rest` na 516 bajtów zastąpiony ciągiem C# z `PullText`), `NewCityScreen.PullTextInput` (bufor `kept` w klawiszu Insert zastąpiony ciągiem C#), `DialogDrawing.ObtainNavigationKey` (usunięto ramkę `0x2` i pole `obtainNavigationKeyFrame`) oraz `CommonUtilities.ReleaseResource` (usunięto pustą ramkę stosu C). Usunięto 4 ramki stosu (`EnterFrame`/`LeaveFrame`), pozostało 35. 8 z 8 mutacji złapanych (BatchNine, BatchThirtyThree, BatchThirtyNine), Golden Master 16/16 zielony.
- B6.8, porcja 5 (2026-10-26): bufory i ramki stosu przeniesione do C# w `GameInitAndIntro.LabelLandmasses` (usunięto ramkę 0x26 i pole `labelLandmassesFrame`), `GameLoadAndSave.VerifyDrive` (usunięto ramkę 0x12, `request` zastąpiony `BiosDiskCall`), `ScienceAndAdvisors.UncoverTechnology` / `AnnounceDiscovery` / `ShowDiscoveryPicture` (usunięto ramkę 0x46, `pictureName` jako string C# dla `.pic` i `.pal`), `TurnEvents.HandleEndOfTurnEvents` / `TryRivalWonder` (usunięto ramkę 0xe i pole `handleEndOfTurnEventsFrame`). Usunięto 4 ramki stosu (`EnterFrame`/`LeaveFrame`), pozostało 31. 8 z 8 mutacji złapanych (BatchNine, BatchForty, BatchFortyOne, BatchFortyFour), Golden Master 16/16 zielony.
- B6.8, porcja 6 (2026-10-26): ramki i bufory stosu przeniesione do C# w `TurnEvents.RenderStatusBar` (usunięto pustą ramkę 0x8), `GameReplay.ObtainDistanceToReplayCity` (usunięto ramkę 0x6 i pole `obtainDistanceToReplayCityFrame`, `nearest` w C#), `GameStartup.TitleLogoAndMainChoices` / `AwaitTitleSongMeasure` (usunięto ramkę 0x6 i pole `titleLogoAndMainChoicesFrame`), `SpaceshipScreen.RenderSpaceship` / `MoveSpaceshipStars` (usunięto ramkę 0x20 i pole `renderSpaceshipFrame`, `key` w C#) oraz `HallOfFame.DisplayTopCities` (usunięto ramkę 0x20, `scores` i `towns` w tablicach C#). Usunięto 5 ramek stosu (`EnterFrame`/`LeaveFrame`), pozostało 26. 10 z 10 mutacji złapanych (BatchTwentySeven, BatchSeven, BatchFiftyFive, BatchSixtyOne, BatchTwentyEight), Golden Master 16/16 zielony.
- B6.8, porcja 7 (2026-10-26): ramki i bufory stosu przeniesione do C# w `GameInitAndIntro.ReckonContinentSizes` / `LinkSeaBlock` / `SeaPassageOpen` (usunięto ramkę 0x18 i pole `reckonContinentSizesFrame`, `lastNextSea` przekazywane przez `ref`, czysta logika przejść morskich), `Pathfinding.ChartRoute` / `SearchRouteBlocks` (usunięto ramkę 0x3a i pole `routeFrame`), `Pathfinding.ShiftToward` / `ChooseCheapestStep` / `CrowdAndDistance` (usunięto ramkę 0x4a i pole `stepFrame`, `CrowdAndDistance` jako metoda pomocnicza C# zwracająca `short`, zmienna lokalna `bestCrowdAndDistance`) oraz `StartGameMenu.CharacterizeStartLocation` (usunięto ramkę 0x5e, usunięto nieużywane obliczenia `frame - 0x5a` / `0x1c`). Usunięto 4 ramki stosu (`EnterFrame`/`LeaveFrame`), pozostały 22. 8 z 8 mutacji złapanych (BatchFortyTwo, BatchFortyThree, BatchFortyFive), Golden Master 16/16 zielony.
- B6.8, porcja 8 (2026-10-26): eliminacja 6 ramek stosu w modułach UI i silnika: GameStartup.QueryForDisk (ramka 0x102), Newspaper.DisplayNewspaper (ramka 0x10, bufor *NEWS{A..N} w C#), Civilopedia.DisplayCivilopediaIndex (ramka 0x5c, bufory nazw), Civilopedia.DisplayCivilopediaRecord (ramka 0x34, bufor tagBuf), Palace.DisplayPalace (ramka 0x110) oraz InGameHelp.DisplayHelpTopic (ramka 0x18, bufor tematu pomocy). Usunięto 6 ramek stosu (EnterFrame/LeaveFrame), pozostało 16. 12 z 12 mutacji złapanych (BatchEighteen, BatchThirtyThree, BatchFiftyNine, BatchSeventyOne, BatchSixtyEight), Golden Master 16/16 zielony.
- B6.8, porcja 9 (2026-10-26): ramki i bufory stosu przeniesione do C# w TurnEvents.DisplayTravellersReport (usunięto ramkę 0x1c i pole displayTravellersReportFrame), TurnEvents.HandleGameEnd (usunięto ramkę 0x1a i pole handleGameEndFrame, ranking replayu spakowany w C# do tablicy bajtów) oraz WorldMap.DisplayCensus (usunięto ramkę 0xba, wartości linii spisu w pamięci roboczej). Usunięto 3 ramki stosu (EnterFrame/LeaveFrame), pozostało 13 (w tym 5 odłożonych do Fazy 3). 6 z 6 mutacji złapanych (BatchForty, BatchSixtyFour, BatchSixtyTwo), Golden Master 16/16 zielony.
- B6.8, porcja 10 (2026-10-26): ramki i bufory stosu przeniesione do C# w ScienceAndAdvisors.PickCityProduction (usunięto ramkę 0x14a, bufor klucza porady przeniesiony do pamięci roboczej) oraz MainIntro.DisplayMainIntro (usunięto ramkę 0x4e i pole displayMainIntroFrame, zmienne lokalne ekranów/kolumny/loga przeniesione do pól C#). Usunięto 2 ramki stosu (EnterFrame/LeaveFrame), pozostało 11 (w tym 5 odłożonych do Fazy 3). 4 z 4 mutacji złapanych (BatchSeventyFive, BatchSeventyTwo), Golden Master 16/16 zielony.
- B6.8, porcja 11 (2026-10-26): ramki i bufory stosu przeniesione do C# w CityView.DisplayCityScreen (usunięto ramkę 0x186, bufor tekstu ekranu miasta przeniesiony do pamięci roboczej) oraz TextBlockRenderer.ImportTextFromFile (usunięto ramkę 0x110, bufory linii i wpisu indeksu przeniesione ze stosu emulacji). Usunięto 2 ramki stosu (EnterFrame/LeaveFrame), pozostało 9 (w tym 6 odłożonych do Fazy 3). 4 z 4 mutacji złapanych (BatchThirty, GoldenMaster), Golden Master 16/16 zielony.
- B6.8, porcja 12 (2026-10-26): eliminacja ramki stosu w CityManagement.HandleCity (usunięto ramkę 0x10c EnterFrame(0x10c) / LeaveFrame(), CityRound używa własnej alokacji C#). Usunięto ostatnią standardową ramkę stosu emulacji. Pozostało dokładnie 8 ramek odłożonych do Fazy 3 (5 z wyciekiem stosu w DOS: OperateMenu, DivideCivilization, OpeningSequence, DisplayConquerorScreen, LaunchAudience; 1 ramka VGA: DuplicateScreenToMainScreen; 2 ze specjalnym zachowaniem klasycznym: ProduceTerrain [ClassicLabelNumbers] i DisplayCivilizationScreen [dostęp do tyknięć zegara]). 2 z 2 mutacji złapanych (GoldenMaster PlayedTurnsMatchBaseline), Golden Master 15/15 zielony.
- B6.4 (2026-10-27): eliminacja surowych odczytów i zapisów słów globalnych PullGlobalWord(0x...) / EmitGlobalWord(0x...) w klasach GameModule. Zmienne nazwane i dodane do GameGlobals (pola GlobalWord, wpisy w Layout i All) oraz właściwości dostępowe w GameModule: DialogNeedsRedraw (0xdb38), MenuMoreBox (0x8066), MenuLineOffsets (0x654e), ScoreVerticalShift (0x5116), SchismShareDc6a (0xdc6a), DoubleWidthText (0x6b8c), NewspaperCrowdPicture (0xdf0c), AudienceInitialPrompt (0xd2dc), UnitSupportCount (0xdeb6), ImportPlayerIndex (0x6820), TileFortressPicture (0xd4da), MiniSymbolPicture (0xb880), UnhappyCitizenPicture (0x6e9e), PollutionIconPicture (0xb2ba), SpaceshipHabitatPicture (0x6834), IntroReservedScreen (0x32d6), PalaceLeftWingWidth (0x4b2c), PalaceRightWingWidth (0x4b34), PalaceStyleShift (0xd922), PalaceGardenBackground (0xd932), SavedGovernmentTitleCommunism (0x33a8), SavedGovernmentTitleMonarchy (0x33aa). Zaktualizowano 17 plików w Civilization1Remaster.Core. 2/2 mutacje złapane (GoldenMaster PlayedTurnsMatchBaseline), Golden Master 15/15 zielony.
- B6.4 / B6.2 część 2 (2026-10-27): eliminacja surowych odwołań bajtowych i ostatnich uchwytów tabel. W Pathfinding.ComputeRouteCost zastąpiono EmitGlobalByte(0x87c9..0x87cb) przez this.troops.Assign(0, 127, ...) z UnitMemory (czyszcząc ostatnie bezpośrednie dostępy do pamięci jednostek). W GameGlobals i GameModule dodano zmienne: SpaceshipStarFieldFrame (0x687a), FallenLeadersCount (0xb1d4) i GameEndFlags (0x4cd9) i zaktualizowano moduły SpaceshipScreen, GameReplay i TurnEvents. W CityView wprowadzono akcesor CityStructurePicture(int) i CitySceneryHandle, eliminując surowe adresy 0x81c4, 0x81ce, 0x81ae, 0x81cc, 0x81b8, 0x817a. 2/2 mutacji złapanych (GoldenMaster PlayedTurnsMatchBaseline), Golden Master 15/15 zielony.
- B6.4 część 6 (2026-10-27): kompleksowa eliminacja surowych odwołań i adresów w module pałacu (Palace.cs), zapisie gry (GameLoadAndSave.cs), zdarzeniach tury (TurnEvents.cs) i menu startowym (StartGameMenu.cs). W Palace.cs nazwane stałe dialogów, zasobów i tablic wysokości/obrazów, akcesory poziomów/stylów/znaczników/obrazów pałacu oraz ogrodu. 2/2 mutacje złapane (RenderPalaceSectionMatchesOriginalCode, ImportPalaceCastleMatchesOriginalCode), Golden Master 15/15 zielony.
- B6.4 część 7 (2026-10-27): eliminacja surowych odwołań i adresów w HallOfFame.cs, Newspaper.cs, ArmySelection.cs oraz AIPlayer.cs. W HallOfFame.cs: stałe segmentu, bazy, rekordu i pól Hall of Fame (HallOfFameSegment, HallOfFameBaseAddress, OffsetScore..OffsetCivilizationPlural), akcesory rekordów i wyników, nazwane stałe napisów, er i tytułów. W Newspaper.cs: nazwane stałe tytułów, cen, mott, pliku tekstowego i menu. W ArmySelection.cs i AIPlayer.cs: stałe strategicznych lokacji graczy oraz ról taktycznych. 2/2 mutacje złapane (AttachHallOfFameRecordMatchesOriginalCode, DisplayNewspaperMatchesOriginalCode), Golden Master 15/15 zielony.
- B6.4 część 8 (2026-10-27): eliminacja surowych odwołań, tabel i adresów w InGameHelp.cs, CivilizationInfo.cs, Civilopedia.cs, RecordTables.cs, ScienceAndAdvisors.cs, UnitManagement.cs. W RecordTables.cs zdefiniowano stałe pól terenu (Food, Shields, Trade, DefenseBonus), strukturę TerrainImprovementTable oraz TechnologyTable (Prerequisite1Address, Prerequisite2Address, Stride). W InGameHelp.cs wprowadzono stałe plików pomocy i tematów (HelpFileNameAddress, HelpCitySiteTopicAddress, HelpIrrigateTopicAddress..). W CivilizationInfo.cs wprowadzono stałe nagłówków przeglądu cywilizacji. W Civilopedia.cs nazwane stałe CivilopediaConceptTitlesTableAddress, CivilopediaNoneStringAddress, CivilopediaAllowsStringAddress oraz użycie stałych tabel technologii i terenu. 2/2 mutacje złapane (DisplayCivilizationOverviewMatchesOriginalCode, DisplayHelpPopupMatchesOriginalCode), Golden Master 15/15 zielony.
- B6.4 część 9 (2026-10-27): eliminacja surowych adresów i tabel w widoku miasta, zdarzeniach tury, doradcach, oknach dialogowych i intro: CityView.cs, TurnEvents.cs, ScienceAndAdvisors.cs, DialogDrawing.cs, GameStartup.cs, MainIntro.cs. W CityView.cs: stałe pól i wymiarów prostokątów wycinków scenerii SceneryBoxStride, SceneryBoxLeft, SceneryBoxTop, SceneryBoxRight, SceneryBoxBottom, SceneryBoxWidth, SceneryBoxHeight oraz akcesory SceneryBoxField i SetSceneryBoxField; stałe obrazów parady ParadeAutomobilePictureAddress, ParadeTrainPictureAddress, ParadeWalkPictureAddress. W TurnEvents.cs: użycie ScienceLeadingFoundVia[34] zamiast bezpośredniego odczytu 0x3772:0x4f4; użycie BuildingWord(24 + wonderIdx, BuildingTable.Price/Technology) i BuildingRecordText(24 + wonderIdx) w TryRivalWonder zamiast wyliczeń na 0xe6a/0xe82/0xe86; nazwane stałe TravellersReportGunpowderAddress, TravellersReportIronWorkingAddress, TravellersReportAncientAddress, MenuBarGameLabelAddress..MenuBarCivilopediaLabelAddress, BankruptcyWarningDialogAddress, RetirementContinuePromptAddress, EndGameTitlePictureAddress, EndGameTitlePaletteAddress, EraBcStringAddress, EraAdStringAddress, AdvisorPaletteResourceAddress, PalaceSectionSpritesTableAddress. W ScienceAndAdvisors.cs: użycie ScienceLeadingFoundVia[56] zamiast 0x3772:0x520. W DialogDrawing.cs: stałe MenuItemHotkeysTableAddress, AdvisorTitlesTableAddress, AdvisorPortraitsTableAddress, MenuLineOffsetsTableAddress. W MainIntro.cs i GameStartup.cs: GameStartup.VideoDriverOptionAddress. 2/2 mutacje złapane (CityViewAndReplayNamesMatchOriginalCode, DisplayTravellersReportMatchesOriginalCode), Golden Master 15/15 zielony.
- B6.4 część 10 (2026-10-27): eliminacja surowych odwołań, adresów sprzętowych i tabel w CityView.cs, SystemServices.cs, GameInitAndIntro.cs, GameEngine.cs, GameMessage.cs. W GameMessage.cs: zastąpienie EmitGlobalWord(0x2f9e, style) przez this.game.Globals.DialogStyle.Value = style;. W GameEngine.cs: nazwane stałe procedur startowych i bloków DOS (StartupExecutablePathAddress, StartupDriveFlagAddress, StartupStackLimitAddress, StartupStackBaseAddress, StartupFreeMemoryAddress, StartupDataSegmentAddress, DosVersionAddress, DosDriveInfoAddress, DosArgumentBlockAddress, DosEnvironmentBlockAddress). W GameInitAndIntro.cs: stała SeaBlocksAlternateAddress oraz czytelne indeksowanie wiersza 11 (SeaBlocksAddress + 11, SeaBlocksAlternateAddress + 11) zamiast surowych 0x7f43 i 0x803a. W CityView.cs: nazwane stałe tabel i obrazów scenerii/cudów/budynków (WonderPicturesTableAddress, CityWallPicturesTableAddress, CityStructurePicturesTableAddress, CitySceneryPiecesTableAddress, CitySceneryPiecesPictureAddress, CityBuildingsModernPictureAddress, CityBuildingsAncientPictureAddress, CityWondersPictureAddress, CityWondersInMaskPictureAddress) oraz metody dostępowe WonderPicture, SetWonderPicture, CityWallPicture, SetCityWallPicture, SetCityStructurePicture. W SystemServices.cs: nazwane stałe rejestrów/liczników timera dźwięku i zegara (TimerTicksLowAddress..TimerSkipCounterAddress), miksera palety/przejść kolorów (HueMixerActiveAddress..HueMixerStepRateAddress) oraz wskaźnika aktywnego prostokąta (ActiveRectanglePointerAddress), zastępujące surowe adresy w funkcjach timera, przerwań zegara i miksera kolorów. 2/2 mutacje złapane (ReckonContinentSizesMatchesOriginalCode, RenderCityBuildingsMatchesOriginalCode), Golden Master 15/15 zielony.
- B6.4 część 11 (2026-10-27): eliminacja surowych adresów, tabel i segmentów w HallOfFame.cs, MeetWithKing.cs, Civilopedia.cs, GameReplay.cs, UnitManagement.cs, MapManagement.cs, CityView.cs, GameInitAndIntro.cs, WorldMap.cs, TurnEvents.cs. W MeetWithKing.cs: użycie LeaderWord(..., LeaderTable.ShortTune/Tune) zamiast surowych 0x1518/0x151a i metody LeaderRecord; nazwane stałe segmentów i tabel LeaderFontTableSegment (0x36fa), AdvisorKingVisualsSegment (0x36fa), AdvisorHorizOffsetsTableAddress (0x3afe), AdvisorSpritesTableAddress (0x804e), KingEyesPicturesTableAddress (0x7efe). W Civilopedia.cs: nazwane stałe segmentu i tabel wyboru (CivilopediaPageHistorySegment 0x3772, CivilopediaChosenIndexTableAddress 0x1c80, CivilopediaChosenCategoryTableAddress 0x36fc) oraz nagłówków i palet kategorii (CivilopediaSciencePaletteAddress, CivilopediaScienceCategoryHeaderAddress, CivilopediaBuildingsPictureAddress, CivilopediaBuildingPaletteAddress, CivilopediaBuildingCategoryHeaderAddress, CivilopediaWonderCategoryHeaderAddress, CivilopediaUnitPaletteAddress, CivilopediaUnitsCategoryHeaderAddress, CivilopediaUnitIconsTableAddress, CivilopediaTerrainPaletteAddress, TerrainPaletteBufferAddress, CivilopediaTerrainCategoryHeaderAddress, CivilopediaConceptsCategoryHeaderAddress). W GameReplay.cs: stałe kolumn tabeli miast powtórki ReplayCityActiveColumnOffset, ReplayCityXColumnOffset, ReplayCityYColumnOffset, stałe tablicy poległych liderów ReplayFallenLeadersSegment, ReplayFallenLeadersTableAddress oraz segmentu twarzy ReplayLeaderFaceSegment. W UnitManagement.cs i MapManagement.cs: zastąpienie surowych wyliczeń participantIdx * 0x600 + troopIdx * 0xc metodami this.Troops.Pull / PullSigned; użycie UnitKindWord ze stałymi UnitKindTable.LandGroup i UnitKindTable.GuardPower oraz TerrainTable.DefenseBonus. W CityView.cs: zastąpienie bezpośrednich odczytów segmentu 0x3767 w RenderCityMarvels akcesorem SceneryBoxField ze stałymi SceneryBoxLeft i SceneryBoxTop. W GameInitAndIntro.cs: nazwane stałe StorySegment (0x3710) oraz StoryTimingTableOffset (0x83) w pętli odtwarzania historii stworzenia świata. W WorldMap.cs i TurnEvents.cs: nazwany segment ReplayHistoryScoresSegment (0x3772) i stała kroku ReplayHistoryStride (8); stałe napisów cudów WondersOfTheWorldTitleAddress, WonderInCityStringAddress, WonderOfCityStringAddress. W HallOfFame.cs: użycie akcesorów FameScore, FameDifficulty, FameYear, FamePopulation, stałych RulerRankEntryStride (0x19) oraz HallOfFameRecordAddress i Offset... w RenderFameRecord, AttachHallOfFameRecord i DisplayCivilizationRating. 2/2 mutacje złapane (DisplayCivilopediaRecordMatchesOriginalCode, AttachHallOfFameRecordMatchesOriginalCode), Golden Master 15/15 zielony.
- B6.4 część 12 (2026-10-27): eliminacja surowych adresów, offsetów i segmentów pamięci w SpaceshipScreen.cs. Wprowadzono nazwane stałe segmentów i geometrii siatki statku kosmicznego SpaceshipLayoutSegment (0x375a), SpaceshipGridSegment (0x3772), SpaceshipGridBaseAddress (0x16e0), SpaceshipGridParticipantStride (0xb4), SpaceshipGridRowStride (12), SpaceshipSlotStride (3), SpaceshipSlotColumnOffset, SpaceshipSlotRowOffset, SpaceshipSlotKindOffset, znaczniki SpaceshipSlotEndMarker i SpaceshipSlotEmptyMarker (0xff). Dodano metody dostępowe LayoutSlotColumn, LayoutSlotRow, LayoutSlotKind, LayoutSlotMarker, SpaceshipGridCellAddress, ujednolicając adresację siatki w CountSpaceshipParts, ObtainSpaceshipPartValue, ComputeSpaceshipSuccess, BuildSpaceshipPart, AttachSpaceshipPart, SpaceshipSlotFree, FindSpaceshipSlot i ChooseSpaceshipStructureSlot. Wprowadzono nazwane stałe zasobów graficznych, palet, buforów oraz okien dialogowych (SpaceshipBackgroundPictureAddress, SpaceshipResourceBlockAddress, SpaceshipPaletteAddress, PaletteBufferAddress1, PaletteBufferAddress2, PaletteCopySourceAddress, PaletteCopyTargetAddress, SpaceshipPopulationHeaderAddress, SpaceshipMassHeaderAddress, SpaceshipFlightTimeHeaderAddress, SpaceshipSuccessRateHeaderAddress, ArrivalPassedStringAddress, ArrivalExpectedStringAddress, SpaceshipLaunchInstructionsStringAddress, SpaceshipComponentChoiceDialogAddress, SpaceshipModuleChoiceDialogAddress). 2/2 mutacje złapane (RenderSpaceshipMatchesOriginalCode), Golden Master 15/15 zielony.

## Następny krok

Stan 2026-10-24: migracja asm → C# zakończona (2026-10-19), B0 – B2 i B4 zrobione (B4 do granicy opisanej wyżej),
B3 odłożone na koniec, trwa B5 (tabele z CIV.EXE). Robić kilka porcji naraz, potem jeden pełny zestaw i jedna runda
mutacji (prośba użytkownika). Poprawki zachowania z listy niżej – za przełącznikami `Classic…` (remaster), po decyzji
użytkownika.

## Błędy przetłumaczonego kodu zachowane celowo (baseline je ma, naprawić na końcu)

Kod C# przetłumaczony z asm ma miejscami inne zachowanie niż DOS-owy CIV.EXE. Testy porównują z tłumaczeniem, więc na razie
te błędy są odtwarzane jawnie:

- **„… discover X from the Barbarians” przy zwykłych badaniach komputera (2026-10-24, zgłoszone przez użytkownika).**
  `AnnounceTechnology` (ScienceAndAdvisors): źródło 0 znaczy „z badań” (tak wołają `UncoverTechnology(gracz, 0)` po
  badaniach i chatka ze zwojami `EarnTechnology(gracz, tech, 0)`), i czasownik to słusznie „discover” (0x23d7). Dopisek
  „ from the <źródło>” sprawdza jednak `participantIdx1 == 0` przez AX (`or ax, ax`), a tłumaczenie zgubiło
  `mov ax, [participantIdx1]` – AX to wtedy 0xba06 (wynik AppendText), więc dopisek idzie zawsze, a gracz 0 to
  barbarzyńcy. W DOS-owym CIV.EXE komunikat kończy się na nazwie technologii. Widać to przy każdej technologii,
  którą AI (z kontaktem z człowiekiem) odkryje sama. Naprawa: warunek `participantIdx != participantIdx1 &&
  participantIdx1 != 0` (`TechnologyNewsRules.NamesSource`). **Naprawione w grze 2026-10-24** za przełącznikiem
  `TurnHooks.ClassicTechnologyNews` (jak `ClassicHealing`): `HeadlessGame` go włącza, więc testy, zamrożona gra
  i baseline się zgadzają; gra (Desktop) ma poprawny komunikat („Babylonians develop Mass Transit!”). Testy:
  `TechnologyNewsRulesTests`, `TechnologyNewsInGameTests` (oba warianty). Na końcu migracji usunąć przełącznik i nagrać
  baseline od nowa.
- `FeedCity` (`HandleCity`, L0819): po `TrueExtent--` przetłumaczony kod sprawdzał flagę Z z wcześniejszego porównania
  (0xe8 == -1, zawsze prawda), a nie wynik zmniejszenia, więc głodujące miasto bez osadnika zawsze ginie (w oryginale tylko przy
  rozmiarze 1; inaczej traci mieszkańca i zeruje zapas). **Naprawione w grze 2026-10-24** za przełącznikiem `TurnHooks.ClassicStarvation`
  (potwierdzone w tłumaczeniu z d175cdb: niezerowy rozmiar po `dec` skacze do L0863 – zapas 0, odświeżenie pola): miasto
  traci mieszkańca i zapas, ginie tylko przy rozmiarze 1 (`LoseStarvingCity`, `GrowCity` wydzielone z `FeedCity`).
  Testy: `StarvationInGameTests` (oba warianty, rozmiar 3 i 1; 3 mutacje złapane).
- **Miasto cudu spoza tablicy miast – wyjątek zamiast DOS-owego odczytu (2026-10-20, potwierdzone testem).** `MarvelTownIdx`
  trzyma -1 dla cudu niezbudowanego i 128 dla cudu w mieście zniszczonym (`FinalizeProduction`). DOS czytał wtedy rekord
  miasta przed albo za tablicą (śmieci, właściciel prawie nigdy się nie zgadza, więc cud po prostu nie działa); C# rzuca
  `IndexOutOfRangeException` przy `Towns[-1]` / `Towns[128]`. Błąd był już w tłumaczeniu przed migracją (d175cdb):
  - `PlayerEnjoysWonderEffect` (CityManagement): sprawdza tylko 0xffff, więc 128 → wyjątek, o ile cud nie jest przestarzały.
    Wołane w każdej turze miasta (cuda 2, 6, 9, 12, 13, 15, 16, 20, 21), przy rządach (1), przy wojnie AI (7), statkach (4, 8),
    Wielkiej Bibliotece (5) – więc zdobycie albo zniszczenie miasta wielkości 1 z nieprzestarzałym cudem wywraca grę;
  - `ApplyTechnology` (ScienceAndAdvisors, pętla „... cancels the effect of ...”): przy pierwszym odkryciu wynalazku, który
    unieważnia cud, czyta `Towns[MarvelTownIdx[cud]]` bez żadnego sprawdzenia, więc wyjątek dla cudu niezbudowanego (-1)
    i zniszczonego (128) – to może się zdarzać często (np. pierwsze odkrycie Invention bez Hanging Gardens);
  - `ScoreWondersAndPollution`, ranking cudów w `CityFoundingAndScore` i `WorldMap` sprawdzają -1 i 0x80, są bezpieczne;
    `Towns[MarvelTownIdx[15 / 13]]` w HandleCity i Hoover Dam w doradcy są wołane dopiero po `PlayerEnjoysWonderEffect`.
  Naprawa: indeks spoza 0 – 127 traktować jak „nie ten właściciel” (tak wychodziło w DOS-ie); test na oba przypadki
  (sonda: `MarvelTownIdx[21] = 128` po `FinalizeProduction` + HandleCity; `MarvelTownIdx[2] = -1 / 128`,
  `ScienceLeadingFoundVia[20] = 0`, `ApplyTechnology(1, 20)`).
  **Naprawione w grze 2026-10-24** za przełącznikiem `TurnHooks.ClassicWonderCities` (jak `ClassicTechnologyNews`):
  `WonderCityRules.ReadsCity` – cud -1 / 128 nie należy do nikogo w `PlayerEnjoysWonderEffect`, pętli „cancels the effect of”
  i Wielkiej Bibliotece (`EarnTechnology` → `ApplyTechnology`) oraz na ekranie cudów (`DisplayWondersOfTheWorld`, właściciel
  czytany tylko dla miasta z tablicy). `HeadlessGame` włącza przełącznik, więc testy i baseline zostają przy wyjątku. Testy:
  `WonderCityRulesTests`, `WonderCityInGameTests` (oba warianty, każde miejsce). Na końcu migracji usunąć przełącznik.

- `HandleGameEnd`: co 50 tur wpis 11 historii dostaje jako starszy bajt populacji `(bajt & 0xff00) >> 8`, czyli zawsze 0;
  losowe katastrofy miast: licznik pętli to numer wylosowanego miasta + 1 (pętla kończy się po pierwszym mieście, chyba
  że wylosowano miasto 0). Po naprawie: starszy bajt populacji, dwa losowania. **Naprawione w grze 2026-10-25** za
  przełącznikami `TurnHooks.ClassicReplayPopulation` i `ClassicDisasterDraws` (`TurnEndRules`): starszy bajt populacji,
  dwa losowania miast na turę (`StrikeCity` wydzielone). Testy: `TurnEndRulesTests` (reguły; w grze oba warianty:
  liczba losowań przez porównanie z generatorem o tym samym ziarnie, wpis 11 przy miastach wielkości 25).
- `DisplayCensus`: wielkość rodziny wypisywana jako „x/10.x/10” zamiast „x/10.x%10”. Po naprawie: reszta z dzielenia.
  **Naprawione w grze 2026-10-25** za przełącznikiem `TurnHooks.ClassicTenths` (`TenthsRules`, razem z masą i czasem
  lotu statku). Test: `TenthsAndSpaceshipTests.CensusWritesTheTenths` (w tym autozapisie „2.2” kontra „2.6”).
- `RenderSpaceship`: obrazek części statku spod `word[część << 1 + 0x682a]` – w C# to przesunięcie o 0x682b, czyli o 11 bitów,
  więc adres jest zły (obrazek zwykle nie istnieje i nic się nie rysuje); masa i czas lotu wypisywane jako „x/10,x/10” zamiast
  „x/10,x%10”. Po naprawie: `word[0x682a + 2 * część]`, reszta z dzielenia. **Naprawione w grze 2026-10-25** za
  przełącznikami `TurnHooks.ClassicSpaceshipPictures` (`SpaceshipPictureRules`) i `ClassicTenths`. Test:
  `TenthsAndSpaceshipTests.SpaceshipDrawsTheParts` (obrazek części na jej miejscu; masa i czas lotu tylko w teście
  reguły – oba teksty powstają jeden po drugim bez funkcji gry pomiędzy, więc w grze ich nie widać bez nowego szwu).
- `ConfigureFreshGame`: zerowanie `TroopsRuined` po każdej `ConstructCivilization` dotyczy zawsze gracza 6 (licznik innej
  pętli). Po naprawie: gracz z pętli (albo usunąć, jeśli `ConstructCivilization` to robi). **Naprawione w grze
  2026-10-25** za przełącznikiem `TurnHooks.ClassicFreshGameRuins` (`TurnEndRules.RuinedUnitsCleared`); test tylko reguły
  (nowa gra w teście to cały kreator).
- `DivideCivilization`: technologie przepisywane z gracza na niego samego (nowa cywilizacja zaczyna bez technologii), flaga 9
  wojny ustawiana drugi raz w relacji gracza zamiast nowej cywilizacji, jednostka bez miasta macierzystego (0xff) poza miastem
  rzuca wyjątek (`Towns[255]`), brak miasta do oddania zapętla podział. Po naprawie: kopia technologii, relacja obustronna,
  sprawdzenie 0xff i warunek końca pętli. **Wyjątek dla jednostki bez miasta macierzystego naprawiony w grze 2026-10-24**
  za przełącznikiem `TurnHooks.ClassicSchismHomes`: `HomeCityRules.HasHome` – taka jednostka liczy się jak jednostka
  z miastem gracza (zostaje przy starej cywilizacji; w mieście nowej przechodzi do właściciela miasta). Testy:
  `SchismAndReportCrashTests` (oba warianty, jednostka w mieście i poza nim). **Reszta naprawiona w grze 2026-10-24** za przełącznikiem
  `TurnHooks.ClassicSchism`: nowa cywilizacja dostaje kopię technologii gracza, flaga 9 wojny w jej relacji do gracza
  (jak w asm: DI = nowa << 4), oddawanie najdalszych miast kończy się, gdy nie ma już miasta dalej niż 1 pole (w
  tłumaczeniu: znowu miasto z poprzedniej rundy albo `Towns[0]` kogokolwiek – pusta wieczna pętla), strona bez miasta
  na stolicę nie rzuca wyjątku (`Towns[0xffff]`; komputer zostaje przy starej stolicy). Przy okazji znalezione dwa
  błędy `TransferUnitInCity` (jednostka w mieście innego właściciela niż właściciel jej miasta macierzystego): nowy
  właściciel brany z surowego bajtu danych pod `0x1c * miasto` (tłumaczenie zgubiło adres tablicy miast; asm czytał
  właściciela miasta) i typ czytany po `EliminateUnit` (0xff – tak samo w oryginale: jednostka znika, w C#
  `LiveTroops[-1]` rzuca wyjątek); naprawione tym samym przełącznikiem (właściciel miasta, typ sprzed usunięcia).
  Testy: `SchismRepairTests` (oba warianty; 7 mutacji złapanych).
- `HandlePeaceTreaties`: groźba emisariusza (L0468) bierze nazwę gracza z `word[0x1992 + DI]`, gdzie DI = gracz << 4 (ustawione
  dla relacji), czyli spoza tablicy nazw – tekst z przypadkowego wskaźnika. Po naprawie: 0x1992 + 2 * gracz.
  **Naprawione w grze 2026-10-25** za przełącznikiem `TurnHooks.ClassicEmissaryThreat` (`TurnEndRules.EmissaryEnemyName`):
  tekst *CRUSADE z KING.TXT („rid the world of the evil $THEM”) wymienia **wroga**, więc 0x1992 + 2 × wróg, jak przy
  trybucie. Test: `TurnEndRulesTests.ThreatOfAnEmissaryNamesTheEnemy` (w klasycznym wariancie wychodzi „Mr.”).
- `EarnTechnology`: pierwsze odkrycie technologii, która dezaktualizuje cud (tabela 0x10fe), czyta `Towns[MarvelTownIdx[cud]]`
  także dla niezbudowanego cudu (-1) albo zniszczonego miasta (128), więc rzuca `IndexOutOfRangeException` (DOS czytał pamięć
  przed tablicą). **Naprawione w grze 2026-10-24** (`ClassicWonderCities`, patrz wyżej).
- `AttachSpaceshipPart`: gdy nie ma wolnego miejsca danego rodzaju, komponent lub moduł trafia na koniec tabeli 0x375a (wiersz
  0xff, czyli 12 bajtów przed siatką gracza), a struktura w slot 0x63 (poza tabelą); komunikat „No further … required” jest
  nieosiągalny (szukanie nigdy nie kończy się na 0xffff). Po naprawie: komunikat i bit w 0x687a + 2 * rodzaj.
  **Naprawione w grze 2026-10-25** za przełącznikiem `TurnHooks.ClassicSpaceshipSlots` (`SpaceshipSlotRules`): brak
  miejsca daje 0xffff, rodzaj dostaje bit swojej linii dialogu (rodzaj − 3 × grupa; tłumaczenie przesuwało o sam rodzaj)
  i komunikat „No further Propulsion required.” (nazwa spod 0x4bd0 + 0x1a × rodzaj; tłumaczenie dodawało 3 × grupę
  drugi raz); struktura bez miejsca po wybranej stronie bierze drugą stronę, przy obu pełnych – brak miejsca. Testy:
  `SpaceshipSlotTests` (reguły, w grze oba warianty).
- `DisplayMilitaryReport`: wiersz gracza bez kontaktu nie zapisuje swojego wpisu listy w ramce, więc klawisz 1 – 7 albo kliknięcie
  w ten wiersz czyta śmieci ze stosu; śmieci ≥ 8 dają `IndexOutOfRangeException` w `Relations` (DOS czytał pamięć). Po naprawie:
  wpis 0xffff / pominięcie wiersza. **Naprawione w grze 2026-10-24** za przełącznikiem
  `TurnHooks.ClassicMilitaryReportRows`: lista graczy wierszy także w tablicy C# (-1 dla wiersza bez kontaktu, który nic
  nie pokazuje). Test: `SchismAndReportCrashTests` (stos pod SP zapełniony wartością 0x99).
- `VisitEachUnitInStack` zostawia SI = `0x600 * gracz` (tłumaczenie nie zachowywało SI). `AIPlayer` (L2947, ok. linii 4330)
  trzyma w SI sumę ataku stosu, woła `TallyUnitsByDomainInStack(…, 1)` i dzieli przez SI, więc dzieli przez `0x600 * gracz`
  zamiast przez atak. Po naprawie: AI inaczej ocenia ataki (zmiana baseline). **Naprawione w grze 2026-10-24** za
  przełącznikiem `TurnHooks.ClassicStackStrength`: w `AIPlayer.ScoreTile` dzielnik to pierwsza suma + 1
  (`StackStrengthRules.Divisor`) – obrońca dodaje 4 × obrona stosu / (jednostki + 1), atakujący 2 × atak / (obrona + 1);
  SI nadal ustawiane jak w oryginale. Testy: `StackStrengthTests` (reguła i 11 autozapisów × 5 tur: ruchy AI różnią się
  co najmniej w jednym). Mutacje: reguła i gałąź atakującego złapane; gałąź obrońcy wykonuje się (sonda: kilka razy
  w tych turach), ale nie przesądza o ruchu – zostaje test reguły.
- `LocateNearestFriendlyCity`: bez własnego miasta i lotniskowca cel kroku to niezainicjowane zmienne lokalne (śmieci ze stosu).
  Odtworzone przez odczyt ramki stosu. Po naprawie: np. kierunek 0 (brak ruchu). **Naprawione w grze 2026-10-25** za
  przełącznikiem `TurnHooks.ClassicStackLeftovers` (kierunek 0); ten sam przełącznik zeruje zmienne lokalne każdej
  nowej ramki (`CPU.ClearFrameLocals`), więc żadna funkcja nie czyta już śmieci ze stosu (`ShiftAIUnit` 0xc,
  `RenderCitizenIcons` 0x2, `DisplayTravellersReport` 0x2, `HandlePlayerTurn` …). Testy: `StackLeftoverTests`.
- `DisplayWondersOfTheWorld`: cud zniszczonego miasta (`MarvelTownIdx` = 128) daje odczyt `Towns[128]` przed sprawdzeniem 128,
  więc ekran cudów rzuca `IndexOutOfRangeException` (DOS czytał tam śmieci). Po naprawie: właściciel czytany dopiero dla miasta < 128. **Naprawione w grze 2026-10-24** (`ClassicWonderCities`).
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
