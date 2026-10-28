# Plan po migracji asm → C# (2026-10-19)

Migracja się skończyła: `find_asm_methods.py` nie znajduje już metod w stylu asm. Kod nadal działa jednak na emulowanej
maszynie. Ten plan ma dwie części: najpierw porządki (duplikaty, wspólne części), potem usuwanie śladów emulacji.
Zasada bez zmian: **żadnej zmiany zachowania**, dopóki użytkownik nie zdecyduje inaczej. Golden master i testy różnicowe
muszą zostać zielone.

## Stan w liczbach (Civilization1Remaster.Core, 51 748 linii)

| Ślad emulacji | Wystąpień |
|---|---|
| zmienne lokalne pod numerami (`PullLocalWord(0xe8)`, `EmitLocalWord`, …) | 2648 |
| zmienne globalne pod adresami (`PullGlobalWord(0x70da)`, …) – 252 różne adresy | 3117 |
| surowy dostęp do pamięci (`PullUInt16(SegData, adres)`, tabele z CIV.EXE) | 882 |
| odczyty i zapisy AX (`RegAcc`) – wyniki funkcji przez rejestr | 848 |
| SI/DI (`RegSource`, `RegDest`) | 596 |
| BX/CX/DX | 433 |
| ramki stosu (`EnterFrame`) / zapis rejestrów (`Preserve…`, `Restore…`) | 94 / 151 |
| `goto` | 1 |

Najczęstsze adresy globalne: `0xaa` (ekran, 547×), `0x19d4` (bufor obrazu, 182×), `0xd762` (69×), `0x70e4` / `0x70e2`
(nieszczęśliwi / szczęśliwi, po ~50×), `0x6548` (bieżący gracz, 51×), `0x2f9e` (41×).

## Część A: porządki (małe, bezpieczne kroki, każdy z testem)

Dokładnych kopii (6+ takich samych linii) jest mało, 24 grupy. Dużo za to tych samych *wzorców* zapisanych różnie.

1. **Czy gracz jest człowiekiem.** 17 miejsc i 4 różne zapisy:
   `(ParticipantMarkers & (1 << idx)) != 0`, `((1 << (byte)idx) & (ushort)ParticipantMarkers)`, wersje z `& 0xff`.
   → jedna metoda, np. `World.IsHuman(idx)`. Zapisy są równoważne dla graczy 0–7.
2. **Czy miasto ma budynek.** Dwie prywatne kopie tej samej funkcji (`CityHasStructure` w CityFoundingAndScore,
   `CityHasBuilding` w DiplomatActions), 48 surowych testów `StructureMarkers0 & 0x…` i 4 miejsca składające 32-bitową
   maskę z dwóch słów. → `City.HasImprovement(n)`, `City.RemoveImprovement(n)` i nazwane stałe budynków (Granary,
   CityWalls, Aqueduct, Factory…) zamiast `0x4`, `0x100`, `0x4000`.
3. **Zmiana produkcji miasta.** Pięć kopii schematu „jednostek w budowie −1, wybór, +1”: `RepickCityProduction`,
   `ReadCityScreenInput`, AIPlayer:2893, TradeAndConquest:179, ScienceAndAdvisors:148. → jedna metoda
   `SetCityProduction(miasto, nowa)`. Uwaga: dwie z nich muszą dalej ustawiać SI jak oryginał, dopóki istnieje punkt B3.
4. **Wiadomości.** `CopyText(0xba06…)` 238×, `AppendText(0xba06…)` 734×, `EmitGlobalByte(0xba06, 0)` 137×,
   `DisplayDialog(0xba06, 100, 80)` 34×. Typowy schemat: tekst + nazwa miasta + tekst + okienko albo gazeta.
   → mały pomocnik do składania tekstu (`Message.Text("…").City(miasto).Text("…").Dialog()`), który woła te same
   funkcje w tej samej kolejności, więc pamięć pod `0xba06` wychodzi bajt w bajt ta sama.
5. **Drobne powtórki w HandleCity:** przywracanie `0x70dc` / `0x70de` z `0x10a` / `0xa` (7 kopii); para
   `DisplayMouseCursor` / `ConcealMouseCursor` (111/107 w całym Core) dookoła okienek.
6. **Dokładne kopie bloków:** `AttachReplayContent` w UnitManagement (4 prawie takie same bloki), początek trzech
   rozkazów jednostek w PlayerTurn (1354, 1445, 1513), pętla po kierunkach w MapManagement (3×), pary w CRuntime,
   StartGameMenu (303/794), CityView (162/225), ImageTools (55/98).
7. **Szablon klas.** 44 klasy mają ten sam konstruktor i pola (`owner`, `processor`). → wspólna klasa bazowa.
   Czysto mechaniczne.
8. **Komentarze `/// ?`.** Część metod ma jeszcze puste opisy z tłumaczenia (np. `AttachReplayContent`). Uzupełniać
   przy okazji punktów wyżej.

**Zrobione 2026-10-19 (pierwsza porcja):** 1 (`World.IsHuman`, 17 miejsc), 2 (`CityStructure`, `City.Has` / `Add` /
`Remove` / `Structures`, 66 testów bitów z nazwami; dwie kopie funkcji połączone w `City.HasStructureSignExtended`),
3 (`Player.StopMaking` / `StartMaking`, w CityManagement `StopMakingUnit` / `StartMakingUnit` z rejestrami jak w oryginale),
5 (`RestoreShieldsAndTrade`, 7 miejsc; para myszy jeszcze nie), 7 (`GameModule`, 43 klasy; VGADriver ma inny konstruktor).

**Zrobione 2026-10-19 (druga porcja):** 4 (`GameMessage` z `Message` / `EmptyMessage` w `GameModule`: 176 łańcuchów
tekst – miasto – styl – okienko w 31 plikach; zostało ok. 500 pojedynczych wywołań przerywanych ustawianiem rejestrów
albo obliczeniami – dojdą po B1), 6 (`WriteReplayEntry` dla czterech `AttachReplayContent`, `MaskNeighbours` dla trzech
pętli po sąsiadach w MapManagement, `UnitMemory.Offset` zamiast 35 wyliczeń miejsca jednostki, `StartTerrainWork` dla
drogi, nawadniania i kopalni, `CountSpecialSlots` w CityManagement). Pozostałe pary z listy różnią się szczegółami
(CityView, StartGameMenu, CRuntime) – zostawione.

Kolejność: 1, 2, 3, 5 i 7 są najtańsze i od razu skracają kod; 4 daje najwięcej (ok. 1000 linii); 6 przy okazji.
Szacunek: 6–10 porcji.

## Część B: usuwanie śladów emulacji

### B0. Najpierw testy (warunek wszystkiego dalej)

Dziś test różnicowy porównuje **surową pamięć i rejestry** wersji oryginalnej i przepisanej. Gdy dane przejdą z pamięci
do pól C#, takie porównanie przestanie mieć sens. Potrzebne:

- porównanie **stanu gry** zamiast bajtów: miasta, gracze, jednostki, mapa, ekran (hash obrazu), teksty okienek;
- adresy, które jeszcze żyją w pamięci, porównywać dalej. Te przeniesione do pól porównywać przez pola.
  Każdy krok B przesuwa jeden adres z jednej listy na drugą;
- ślad wywołań (już istnieje) zostaje jako dodatkowa kontrola;
- golden master (całe tury) zostaje bez zmian. To on pilnuje, że gra zachowuje się tak samo.

**Plan B0 (2026-10-20):**
1. Zamrożony silnik: znacznik `migration-end` (7acde51); Core, Machine, GameModel, Foundation, Imaging i Acoustics z tego
   znacznika w projekcie `Civilization1Remaster.Original` (jedno zestawienie, przestrzeń nazw `Civilization1Original`).
   Nigdy go nie zmieniać – w razie potrzeby wziąć nową kopię. **Zrobione.**
2. Projekcja stanu: ten sam słownik dla obu silników (pola GameState i silnika, strony pamięci danych, mapa, losowanie,
   SP, ekrany) plus `MovedData` – dane przeniesione z pamięci porównywane po znaczeniu (`moved.klucz`), a ich adresy
   pomijane w stronach pamięci po obu stronach. Każdy krok B dopisuje wpis. **Zrobione** (lista jeszcze pusta).
3. Podwójna gra: `DualGame` (zamrożony i obecny silnik z tego samego autozapisu); `DualTurnTests` – 5 tur z 11
   autozapisów, 25 s, kontrolne mutacje 5 z 5. **Zrobione.** `DualCityCaseTests` – 19 tabel przypadków `HandleCity` na
   obu silnikach (punkt kontrolny w obu, ustawienia stanu przez `dynamic`, porównanie stanu, AX i wyjątków), 47 s;
   kontrolne mutacje 18 z 21 (ocalały zmiany widoczne tylko w zmiennych lokalnych na stosie i zaraz zamalowanym obrazie
   – dawne testy porównywały stos – oraz próg kosztu nauki komputera, do którego żaden przypadek nie dochodzi).
   **Zrobione.** Wywołania innych pojedynczych funkcji dochodzą w krokach B, gdy będą potrzebne.
4. Stare testy różnicowe zostają, dopóki działają; krok B, który unieważnia test, usuwa go, gdy podwójna gra pokrywa
   funkcję (sprawdzone mutacjami).

### B1. Wyniki przez `return` zamiast przez AX (848 miejsc)

Metoda po metodzie: `ObtainCitizenType(ea); x = RegAcc` → `x = ObtainCitizenType(ea)`. Najpierw te, których wynik
nikt nie czyta z rejestru po drodze. Uwaga: kod spoza przepisanych modułów (okienka, grafika) też może czytać AX.

**B1, sposób (2026-10-20):** funkcja dalej ustawia AX (czytają go jeszcze kopie „Reference” starych testów i miejsca, których
skrypt nie rusza), a wywołujący biorą wynik z `return`: `F(...); x = AX` → `x = F(...)`. Skrypt `b1.py` (scratchpad)
zamienia tylko pary „samo wywołanie, zaraz potem instrukcja czytająca AX”, pomija ciała `if` bez klamer i zapisy do AX.
Porcja 1: `VerifyValueSpan`, `KeyAvailable`, `DisplayMenu` – 68 miejsc w 18 plikach. Kolejne porcje: następne funkcje z
listy (ObtainContinentID, ScreenToPicture, TextLength, PlayerEnjoysWonderEffect, ObtainDistance…), potem funkcje `void`,
które zwracają przez AX (DisplayDialog, QueryForDisk…), na końcu usunięcie zapisów AX, których nikt już nie czyta.

### B2. Zmienne lokalne pod numerami → zmienne i parametry C# (2648 miejsc)

Największa porcja pracy, ale najprostsza do zrozumienia. Zaczynać od funkcji z małą ramką; `HandleCity` (268 bajtów
dzielonych przez ~60 metod) i `HandlePlayerTurn` na końcu. W `HandleCity` przyda się klasa „stan rundy miasta” z polami
zamiast `0xe8`, `0xea`, `0xfc`…

### B3. SI/DI i ramki stosu (596 + 245 miejsc)

Gdy funkcja nie ma już zmiennych lokalnych w pamięci, ramkę (`EnterFrame`) i zapis SI/DI można usunąć. Haczyk z tej
sesji: menu (`OperateMenu`, `PreserveIndexRegisters`) odkłada SI/DI na stos, a test porównuje ten obszar. Po B0 to
porównanie przestanie wymuszać odtwarzanie SI/DI.

**Odłożone na koniec migracji (decyzja użytkownika 2026-10-24),** razem z poprawkami zachowania i nagraniem baseline'u
od nowa. Powód: sonda resztek stosu (`LeftoverWatcher`, golden master + Differential + Dual) pokazała 30 funkcji, które
czytają 617 słów stosu zostawionych przez inne funkcje – także `HandlePlayerTurn` (37), `ShiftAIUnit` (27),
`PickCityProduction` (13), `RenderCitizenIcons`, `LocateNearestFriendlyCity`, `ReckonContinentSizes`. Każde usunięcie
ramki, `Preserve…` albo nawet „martwego” zapisu SI (odkładanego potem na stos przez funkcje niżej) zmienia, co te
funkcje czytają, a więc decyzje AI. Bez zmiany baseline'u B3 dałoby się robić tylko funkcja po funkcji z sondą.

### B4. Zmienne globalne pod adresami → nazwane pola (252 adresy, 3117 miejsc)

Po jednym adresie albo po grupie (np. wyniki tury miasta `0x70da`–`0x70e6` → `CityTurn.Food`, `Shields`, `Trade`,
`Happy`, `Unhappy`). Zacząć od adresów używanych tylko w C#. Adresy czytane przez grafikę, okienka albo zapisywane
w pliku gry na końcu.

### B5. Tabele z CIV.EXE → tablice C# (882 miejsca)

Ceny i nazwy budynków (`0xbb2`, `0xbd0`), jednostki (`0x112a`), teksty. Dane można wczytać raz przy starcie do zwykłych
tablic. **Format pliku zapisu gry zostaje ten sam**: odczyt i zapis przechodzą przez warstwę, która tłumaczy pola
na stary układ bajtów.

### B6. Na końcu: sam procesor i blok pamięci

Gdy nic już nie używa rejestrów i pamięci danych, zostaje grafika (bufory ekranu, palety) i dźwięk. To osobna decyzja:
albo zostawić małą warstwę zgodności, albo przepisać rysowanie na bezpośrednie bufory.

**Plan B6 (2026-10-25).** Stan: Core 52 400 linii, `this.processor.` 6 134 razy. Co jeszcze siedzi w emulatorze:

| Ślad | Miejsc | Faza |
|---|---|---|
| AX (`RegAcc`) – głównie zapisy „jak w oryginale” | 568 | B1-reszta / B3 |
| SI/DI | 588 | B3 (odłożone) |
| BX/CX/DX | 443 | B3 (nie było w planie – dopisać) |
| `EnterFrame`/`LeaveFrame`, `Preserve…`, `StackWord` | 190 / 151 / 82 | B3 |
| zmienne lokalne w ramce (`PullLocal…`, `FrameWord`) | 909 | B2-reszta (resztki stosu) / B3 |
| zmienne globalne pod adresem (literał / wyliczony) | 713 / 600 | B4-reszta |
| surowe `Pull…`/`Emit…(segment, adres)` | 753 | B5-reszta, B6.1 – B6.3 |
| teksty przez adresy (`CopyText`/`AppendText`, bufor 0xba06) | 643 / 1053 | B6.1 |
| jednostki w pamięci (`UnitMemory`, `troops.`) | 798 | B6.2 |
| pudełka ekranów w pamięci (`ScreenBox`…), `VideoCard`, `Services` | 772 / 392 / 464 | B6.5 |
| dziennik wywołań (`Journal.OpenScope`/`CloseScope`) | 751 | B6.9 |

Kolejność (każdy krok osobno zielony; pamięć 0x3b01 długo zostaje jako magazyn, zmieniają się tylko „szwy”):

- **B6.0 – testy, warunek wszystkiego dalej.** (a) *Rzut stanu na pamięć*: `GameSnapshot` liczy `mem.` z pamięci, więc
  stan przeniesiony do C# musi być wpisywany do widoku pamięci w starym układzie bajtów (ta sama warstwa posłuży zapisowi
  gry, którego format zostaje). (b) *Zegar testów niezależny od dostępów do pamięci*: dziś przerwanie co 10 000 dostępów
  CPU (`FixedClockSpan`) – każdy odczyt przeniesiony do C# przesuwa moment tyknięcia. Zegar tykać przy oczekiwaniu na
  zegar / klawisz i co N wywołań funkcji gry (dziennik), w obu grach; baseline nagrać od nowa z zamrożonej gry
  (jak przy `baseline-interrupt-registers.patch`). Bez tego B6 da się robić tylko z zachowaniem liczby dostępów.
  **Zrobione 2026-10-25:** (b) zegar testów w obu maszynach (bieżącej i `Original`) tyka co 50 wywołań funkcji gry
  (`LogWrapper.ScopeOpened`, w każdym dzienniku, na który gra przełącza `CPU.Journal` – intro ma własny), przy `Idle`
  i co 16 odczytów zmiennych zegara 0x40 – 0x6f albo pytań o pozycję muzyki (`CPU.PollMusic`, jednowierszowy szew
  w `SystemServices.ObtainSongPosition` obu gier) – pętle czekania bez wywołań: `AwaitTime`, `CityView`, `GameReplay`,
  `MainIntro` (`MusicReachedColumn`, `ShowTitleLogo`), `DiplomatActions`; przerwanie przychodzi od razu
  (`CPU.ServeClock`), nie przy następnym dostępie do pamięci. Zamrożona gra z nowym zegarem daje te same kanały
  zachowania co baseline'y z a3b5a3b, więc te zostały (nagrywanie z zamrożonej gry: `CIV1_GOLDEN_MASTER_FROZEN=1`).
  Hak `DualGame` (zerowanie licznika przy każdym wywołaniu) usunięty. (a) `MovedData.Entry` daje bajty w starym układzie,
  migawka bieżącej gry wpisuje je pod ich adres w kopii segmentu przed liczeniem haszy (`MovedData.Project`), zamrożona
  niczego nie pomija – golden master i podwójna gra porównują te same bajty (test `MovedDataTests`). Test `ClockInterrupt` (woła
  obsługę przerwania sam) wyłącza zegar testów w scenariuszu (`AllowClock = false`). Pełny zestaw 770/770. Mutacje warstwy
  testowej: rzut i `PollMusic` złapane; podpinanie zegara pod przełączane dzienniki przeżywa (intro i tak tyka przez
  `PollMusic`) – zostaje dla jednolitego liczenia wywołań.
- **B6.1 – teksty.** Bufor 0xba06 i funkcje C runtime na adresach (`CopyText`, `AppendText`, `IntegerToText`) → `string` /
  `StringBuilder`; rysowanie tekstu (`TextPainter`, `BlockText`) przyjmuje `string`. Kody tekstów (`CodeText1` – `5`)
  i tabele nazw (B5) zwracają `string`. Duże, ale mechaniczne; projekcja potrzebna, bo bufor jest w `mem.`.
  **B6.1, część 1 – szew (2026-10-25):** `Engine/TextBuffer.cs` (`GameEngine.Words`): `Set`, `Append` (string albo
  adres), `Clear`, `Length`, indeks bajtów, `ToString`, stała `Address`. Na razie przez te same funkcje C runtime co
  wcześniej (bajty w pamięci i AX bez zmian). Przez niego przechodzą: 124 `CopyText(0xba06, …)`, 390 `AppendText`,
  72 zerowania, 28 `TextLength` (dwa tylko dla AX: `_ = Words.Length`), 41 bajtów (także 0xba05 – 0xba58 jako
  `Words[k]`), 427 przekazań adresu (`TextBuffer.Address ± k`); `GameMessage` też. W Core nie ma już literału 0xba06.
  Pełny zestaw 770/770, mutacje 8 / 8 (golden master; `DialogsMatchOriginalCode` nie przechodzi przez `Set` / `Append`).
  **Część 2 – zrobione 2026-10-25:** bufor ma 0x300 bajtów (0xba06 – 0xbd05; sonda: gra zapisuje i czyta cały zakres,
  0xbd06 to następna zmienna) i żyje w `byte[]` `TextBuffer` (`Attach` na końcu `PrepareMachine`). Kod, który dostaje
  sam adres, czyta i pisze tablicę przez pamięć: `CPUMemory.AttachOverlay` – każdy dostęp bajtowy (`Peek` / `Poke` w
  6 podstawowych metodach i `EmitChunk`) do zakresu idzie do tablicy, blok pamięci pod nim jest wyzerowany. Zamrożona gra
  bez zmian. `MovedData`: wpis „TextBuffer” (cała tablica), rzut do migawki – golden master i podwójna gra bez zmian.
  Test `MessageBufferLivesInCSharp` (także `EmitChunk` przez koniec bufora). Punkt kontrolny testów (`GameCheckpoint`)
  zapisuje i przywraca też tablicę bufora (kopiuje blok pamięci bezpośrednio, z pominięciem nakładki – bez tego padło
  21 testów podwójnej gry miast). Pełny zestaw 771/771, mutacje 5 / 6 (zerowanie pamięci przy `Attach` równoważne:
  pamięć pod buforem jest wtedy pusta). Zostaje na później (B6.1 część 3): budować teksty w tablicy C# bez C runtime
  i przestawić odbiorców adresu (`TextPainter`, `DisplayDialog`, `DisplayMenu`, `BlockText`, pliki) na `TextBuffer`
  / `string`, potem usunąć nakładkę.
  **Pierwotny plan części 2:** przenieść magazyn do C# – `byte[]` bufora z semantyką strcpy / strcat (bajty za zerem zostają,
  jak w pamięci), wpis w `MovedData` (adres 0xba06, długość bufora) i odczyty przez adres (`TextPainter`,
  `DisplayDialog`, `DisplayMenu`, `BlockText`, `ReadHandle` / `WriteHandle` / `MoveFarData` – plik przez bufor) –
  każdy, kto dostaje `TextBuffer.Address`, musi czytać z C#. Długość bufora ustalić z kodu (najdłuższy tekst / plik).
- **B6.2 – jednostki, zrobione 2026-10-25:** rekordy jednostek (0x81d4, 8 × 0x600 bajtów) w tablicy C# podpinanej
  w `PrepareMachine` jako druga nakładka (`CPUMemory`: lista nakładek, `OverlayAt`, `ReadChunk`); `UnitMemory` czyta
  i pisze tablicę wprost (adres spoza tabeli – jak w oryginale – przez pamięć), reszta kodu (surowe `0x81d4 + …`)
  przez pamięć do tej samej tablicy. Pliki gry (`PullContent` / `EmitContent` w `GameLoadAndSave`, np. jednostki
  0x81d4 / 0x3000 przy wczytaniu) przez nakładki zamiast `StoragePayload`. `GameEngine.UnitRecords`, wpis „Units”
  w `MovedData`, punkt kontrolny zapisuje i przywraca wszystkie nakładki. Testy `UnitsLiveInCSharp` i
  `SavedGameKeepsTheUnits` (zapis gry i wczytanie: jednostki z tablicy C#). Pełny zestaw 773/773, mutacje 6 / 7
  (kopia pamięci przy `Attach` równoważna: jednostki przychodzą dopiero z wczytaniem); czas gry bez zmian (golden master
  jak przy poprzednim commicie). Następnie:
  `GameState.Participants[].Troops` jako prawdziwy stan (dziś kopia z wczytania) i surowe odczyty `0x81d4 + …`
  w `AIPlayer` / `CivilizationSchism` / `PlayerTurn` przez `UnitMemory`.
  **B6.2, część 2 (2026-10-25):** ok. 190 surowych dostępów `0x81d4 + rekord + pole` (odczyty i zapisy, przez
  `PullGlobalByte`, `PullUInt8` / `PullInt8` / `EmitUInt8` z `SegData` i `dataSeg`, także wieloliniowe) przez
  `UnitMemory.PullAt` / `AssignAt` (przesunięcie rekordu policzone wcześniej, nazwane pole `UnitMemory.Horiz` itd.),
  `GameModule.Troops` (wspólna instancja); skrypt `b62c.py`. Zostają: zmienne z adresem już z 0x81d4 (`TurnEvents`
  163 / 894 / 1006, `WorldMap` 49, `UnitCombat.TroopsRoot`) – przez nakładkę – i hurtowy zapis / odczyt gry.
  Pełny zestaw 773/773, mutacje 8 / 10 (dwie w ścieżce „poza tabelą” `UnitMemory` równoważne – idzie przez pamięć,
  a ta przez nakładkę do tej samej tablicy; trzy dopiero z testem funkcji: `CarryOutGoto`, `RenderUnitSprite`,
  `HandlePlayerTurnHumanChoice`).
- **B6.2 – jednostki (pierwotny plan).** `UnitMemory` to już jedyny szew: magazyn → tablica C# (rekord 12 bajtów jako struktura) +
  projekcja 0x81d4 dla migawki i zapisu gry; potem `GameState.Participants[].Troops` (dziś tylko kopia z wczytania)
  staje się prawdziwym stanem.
- **B6.3 – tabele z CIV.EXE.** Akcesory B5 to szew: wczytać tabele raz przy starcie do tablic C# (są zapisywane z grą,
  więc też projekcja). Teksty tabel → `string`.
- **B6.4 – zmienne globalne.** Właściwości B4 → pola C# (jeden szew), z projekcją tych, które idą do zapisu / migawki.
  **B6.3 + B6.4 zrobione razem 2026-10-25:** nakładki ogólne (`Machine/IMemoryOverlay.cs`: `IMemoryOverlay`,
  `ArrayOverlay`; `CPUMemory`: mapa bajt → nakładka O(1) zamiast przeszukiwania listy, wykrywanie nakładających się
  zakresów, `OverlayOf` / `OverlayAt`). Rejestr `Engine/MovedRanges.cs` (bufor, jednostki, tabele, 169 zmiennych) –
  z niego `MovedData` w testach (wpisy, rzut) i punkt kontrolny (każda nakładka przez `Read` / `Write`).
  B6.3: `Engine/GameTables.cs` – blok 0x282 – 0x19c1 (teren, technologie, budynki i cuda, przestarzałe cuda, typy
  jednostek, przywódcy, kierunki, kolory, wskaźniki nazw) w tablicy C#; akcesory B5 czytają `Tables.Word` / `Byte`
  (adres poza blokiem – przez pamięć, jak indeks poza tabelą w oryginale). B6.4: `Engine/GameGlobals.cs` – `GlobalWord`
  (pole `Value`, bajty dla pamięci jak w segmencie danych) dla każdej z 169 nazwanych zmiennych; właściwości
  `GameModule` czytają i piszą `owner.Globals.X.Value` (`DisabledLinesLow` – niższy bajt). Pułapka: 0x19be – 0x19c1
  w bloku tabel są zablokowane (`ForbidAccess`) – odczyt CPU daje 0, więc `MovedData.AddFrozen` czyta surowe bajty.
  Test `TablesAndGlobalsLiveInCSharp`.
  Zegar testów (B6.0) poprawiony przy tej porcji: pod stałym zegarem przerwanie przychodzi tylko w punktach zegara
  (`CountClockScope`, `CountPoll`, `Idle`), nigdy przy zwykłym dostępie do pamięci (`PumpMessages`) – inaczej kod, który
  czyta zmienną z C# zamiast z pamięci, dostaje przerwanie w innym miejscu (`DisplayStoryIntro`, `TitleLogo`). Klawiatura
  skryptu woła `Idle` przy pustej kolejce tylko, gdy gra czeka na klawisz.  
  **Porcje 1 – 3 (2026-10-25):** (1) `TurnEvents.MendOnlyFortifiedUnits`, barbarzyńcy (`TurnEvents` 894), `WorldMap`,
  `UnitCombat` (`PullUnitByte`, `PromoteUnitToVeteran`, wymazanie celu) przez `UnitMemory` – stałe `TroopsRoot` itd.
  usunięte; (2) 16 kolejnych zmiennych w `GameGlobals` (`StoryMusicTime` 0x6800, `StoryLinesTold` 0x6802,
  `HueCyclingOn` 0x2fec, `RepaintingMainScreen` 0x1c28, `LoadedGameMark` 0x3484, `StackLeaderScore` 0x6534 …);
  (3) literalne odczyty tabel (technologie budynków 0xc10 – 0xcc4, wskaźniki nazw 0x1982 – 0x19b6) przez akcesory B5.
  Zostają: zmienne zegara 0x40 – 0x6f i myszy 0x586e – 0x5875 (B6.7), tablice pod adresem (np. 0x817a, 0x804e),
  zmienne o niejasnym znaczeniu (0xdb38, 0x8066, 0x5116, 0xdc6a), `SystemServices` (sterowniki), `UnitKindTable.Field`
  (statyczne z CPU, zostaje tylko w `UnitMemory.KindField`).
  **Porcje 4 – 5 (2026-10-25):** (4) `UnitKindTable.Field(this.processor, …)` w klasach `GameModule` (47 miejsc) →
  `UnitKindWord` (przez `Tables`); (5) B6.1 część 3: `TextBuffer.Set` / `Append` / `Length` / indeks / `Clear` na
  tablicy (strcpy / strcat / strlen w C#, te same bajty i AX); tekst, który wyszedłby za bufor, i bufor przed
  `Attach` – dalej przez C runtime i pamięć. Test `SavedGameKeepsTheUnits` sprawdza też nazwy przywódców i
  cywilizacji w zapisie. Pełny zestaw 774/774 (B6.3/B6.4 oraz porcje 1 – 3 i 4 – 5 osobno). Mutacje: B6.3/B6.4
  10/10, porcje 1 – 3: 8/11, porcje 4 – 5: 6/8. Ocalałe są równoważne w danych testów: wiersze jednostek w
  `WorldMap` (miasta wyznaczają te same granice), `HueCyclingOn` / `RiotShown` (oba 0, gdy cykle stoją), technologia
  świątyni / targu (obie znane albo żadna), `ShiftTally` / `Sight` (równe dla tych jednostek), AX po `TextBuffer.Set`
  (nikt go nie czyta).
  **B6.5 (2026-10-25):** cztery pudełka ekranów (0x12 bajtów: ekran, prostokąt, flagi, kolory, czcionka) leżą
  w 0x98, 0x19c2, 0x19d6, 0x19ea, każde z wskaźnikiem na siebie zaraz za nim (0xaa, 0x19d4, 0x19e8, 0x19fc).
  `Engine/ScreenBoxes.cs` trzyma je w tablicach C# (nakładki, wpisy `Box0` – `Box3` w `MovedRanges`); sterowniki i
  `CivRectangle` sięgają przez pamięć. Kod gry: pola pudełka rysowania przez `GameModule.DrawingBox` (`CivRectangle`,
  nowe słowa `ForeHues` 0xc i `RearHues` 0xe, bo kod pisze tam słowa) zamiast `ScreenBox + 0x10` itd. – ok. 90 miejsc,
  także przez BX (`RegBase = ScreenBox`, zapis BX zostaje do B6.8). Mutacje 9 / 10 + 1 / 2 (widok miasta z przycisku ekranu miasta nie występuje w turach golden master; cztery najpierw sprawdzane złym
  testem, z właściwym złapane; ocalała: odczyt czcionki w `MeasureAndDrawMenuFrame` – w teście menu i tak równy).
  **B6.5, cykle kolorów (2026-10-25):** blok sterownika 0x35cf:0000 – 0x0343 (słowo 0xffff, tabela 8 adresów
  rekordów od 2, rekordy po 0x66 bajtów od 0x14: krok, pozycja, długość, pierwszy kolor, opóźnienie, licznik, kolory
  dwa razy) w `Engine/HueCycles.cs` (tablica C#, nakładka); `AssignHueCycleSpan`, `ActivateHueCycle`,
  `DeactivateHueCycle`, `RefreshHueCycling` czytają i piszą ją bezpośrednio (rejestry jak wcześniej). Poza segmentem
  danych, więc bez wpisu w `MovedRanges`; okna `Watched` testów czytają przez nakładkę. Mutacje 10 / 10.
  **Stan po pętli 2026-10-25 i co dalej (wymaga decyzji):** proste porcje B6 się skończyły. Zostają: (a) B6.6 –
  tabela uchwytów plików (`CPU.Documents`, `DocumentHndTally`) jest wspólna dla `CRuntime` i przerwań DOS w `CPU`
  (int 21h) – trzeba najpierw sprawdzić, czy przerwania DOS są jeszcze używane, i dopiero wtedy przenieść tabelę do
  `CRuntime`; bufory plików w pamięci (adresy) zostają do B6.8. (b) B6.7 – przerwania, mysz, klawiatura jako zdarzenia
  hosta: zmienia podstawę zegara testów (B6.0), więc razem z nowym baseline'em albo bardzo ostrożnie. (c) B6.4 –
  reszta adresów to głównie tablice (0x4b2c / 0x4b32 / 0x4b34 szerokości pałacu, 0x33a8 / 0x33aa, 0x591e – 0x5922,
  uchwyty obrazków 0x6e9e / 0xb880 / 0xd4da / 0xb2ba) i bajty obok siebie (0x87c9 – 0x87cb, potrzebny `GlobalByte`);
  znaczenie kilku niejasne (0xdb38, 0x8066, 0x5116, 0xdc6a). (d) B6.8 / B3 – na koniec z nowym baseline'em.
  **B6.6, krok 1 (2026-10-25):** przerwania DOS w `CPU` (`RaiseInterrupt` z int 10h / 16h / 21h i ok. 40 funkcji
  DOS: pliki, pamięć, wektory, konsola) nie były wołane przez nic – usunięte (ok. 1050 wierszy; interpreter dźwięku
  ma własne `RaiseInterrupt`). Tabela otwartych plików (`Documents`, `DocumentHndTally`, pierwszy uchwyt 0x20)
  przeniesiona z `CPU` do `CRuntime` (`files`, `nextHandle`); `CPU` zostawia tylko `StandardFolder`. Test
  `FileHandlesLiveInTheCRuntime`. Pełny zestaw 774/774 (+1). Mutacje: zamknięcie bez usunięcia uchwytu złapane;
  pierwszy uchwyt 0x21 zamiast 0x20 – równoważne (gra porównuje uchwyt tylko z -1). Zamrożona kopia
  (`Civilization1Remaster.Original`) bez zmian. Dalej w B6.6: bufory plików pod adresami (`ReadStream` / `WriteStream`
  na pamięci) – razem z B6.8.
  **B6.7, część 1 (2026-10-25):** zmienne zegara i dźwięku 0x40 – 0x6f (`TimerState`) oraz stan myszy 0x586e – 0x5875,
  0x587c – 0x587d i flagi kursora 0x5402 – 0x5403 (`PointerState`) w tablicach C# (nakładki, wpisy w `MovedRanges`).
  Pułapka: pod stałym zegarem testów każdy odczyt 0x40 – 0x6f przez CPU to zapytanie o czas (`PollTimer`), także w
  samej obsłudze przerwania – dlatego licznik 0x5c w kodzie gry (`Timer.Beats`, pętle oczekiwania) i sterownik
  (`ClockInterrupt`, dźwięk, zanikanie palety) czytają dalej przez CPU; mysz i kursor (`Pointer.Horiz` / `Vert` /
  `Buttons` / `Clicks` / `ColumnShift` / `DriverActive` / `RefreshLatch` / `PointerShown`) – wprost z C#, 40 miejsc.
  Zegar bez zmian. Pełny zestaw 775/775, mutacje 8 / 8 (jedna dopiero z testem `DeactivateMouse` w
  `TablesAndGlobalsLiveInCSharp`). Część 2 (zdarzenia hosta zamiast `PumpMessages`, zegar) – po nowym baseline'ie.  **Kolejne porcje (pętla do 90% tygodniowego limitu, 2026-10-25):** (1) B6.2-reszta: zmienne z gotowym adresem 0x81d4
  (`TurnEvents` 163 / 894 / 1006, `WorldMap` 49, `UnitCombat.TroopsRoot`) przez `UnitMemory`; (2) pozostałe literalne
  `PullGlobal…(0x…)` w klasach `GameModule` nazwać i dopisać do `GameGlobals` (te o niejasnym znaczeniu nazwać po
  adresie i funkcji); (3) bezpośrednie odczyty `MovedRanges` przez pamięć w akcesorach (np. `UnitKindTable.Field`) na
  `Tables`; (4) B6.1 część 3: `TextBuffer` bez C runtime (strcpy / strcat na tablicy, AX jak wcześniej);
  (5) B6.5 ekrany: struktury pudełek ekranów (`ScreenBox` + 0x10 czcionka) jako obiekty C#.
- **B6.5 – ekrany.** Struktury pudełek ekranów w pamięci (`ScreenBox` + 0x10 czcionka itd.) → obiekty C#; `VGADriver`
  już trzyma bitmapy C#, więc zostaje tylko adresowanie. Palety i cykle kolorów z pamięci → C#.
- **B6.6 – pliki i C runtime.** `CRuntime` (uchwyty, `fscanf`, `fread`) → .NET; format `.SVE` / `.MAP` bez zmian przez
  warstwę projekcji. Tu też naprawa `ScanStream` (pusta linia) – ale to zmiana zachowania (lista błędów).
- **B6.7 – przerwania i wejście.** Zegar, mysz (0x586e – 0x5874), klawiatura → zdarzenia hosta; `PumpMessages` znika.
- **B6.8 – rejestry i stos** (po B3, czyli na końcu z nowym baseline'em): usunąć AX/SI/DI/BX/CX/DX, ramki, `MZExecutable`
  i obraz CIV.EXE w pamięci (dane początkowe: z pliku CIV.EXE przy starcie albo z zasobu).
- **B6.9 – dziennik wywołań.** Testy różnicowe porównują ślad `OpenScope`; zostaje, dopóki są potrzebne, potem jako
  opcjonalne logowanie albo usunąć.

Uwagi: B6.1 – B6.5 nie zmieniają zachowania, jeśli B6.0 jest zrobione; B6.8 razem z B3 i poprawkami zachowania
(nowy baseline). Wydajność: dziś każdy dostęp do pamięci woła `PumpMessages` – po B6.7 gra przyspieszy.

## Zamknięcie etapu porównywania z oryginałem (plan 2026-10-25, przyjęty przez użytkownika)

Cel: przejść od „testy = identycznie jak oryginał” do „testy = zamierzone zachowanie remastera”, nie tracąc siatki
bezpieczeństwa. Baseline nagrywany od nowa **dwa razy**, każdy raz w osobnym commicie bez innych zmian (przełączniki +
baseline'y + raport różnic); pomiędzy nimi golden master znowu pilnuje, że nic się nie zmienia przypadkiem.

Stan na start: dziewięć poprawek działa w grze za przełącznikami `TurnHooks.Classic…` (Healing, Railroads,
TechnologyNews, WonderCities, SchismHomes, Schism, Starvation, StackStrength, MilitaryReportRows), które `HeadlessGame`
włącza. Bez przełącznika: `HandleGameEnd` (bajt populacji w historii, pętla katastrof), `DisplayCensus` („/10” zamiast
„%10”), `RenderSpaceship` (adres obrazka, „/10”), `ConfigureFreshGame` (`TroopsRuined` gracza 6), groźba emisariusza w
`HandlePeaceTreaties`, `AttachSpaceshipPart`, indeks pomocy i pusta linia w `ScanStream`. Śmieci na stosie: 30 funkcji,
617 słów (`LeftoverWatcher`). Ślady emulacji: 789 zmiennych lokalnych pod numerami, 344 zapisy AX, 94 ramki, 81 zapisów
SI/DI.

**Faza 0 – przygotowanie, bez zmiany zachowania (stary baseline):**
1. Pozostałe błędy za przełącznikami `Classic…`, każdy z testem obu wariantów (jak `ClassicStarvation`). Indeks pomocy
   (zmienia pliki tekstowe gry) – osobno albo pominąć, do decyzji.
   **Zrobione 2026-10-25 (bez indeksu pomocy):** `ClassicTenths` (spis ludności, masa i czas lotu statku),
   `ClassicSpaceshipPictures`, `ClassicDisasterDraws`, `ClassicReplayPopulation`, `ClassicEmissaryThreat` (groźba
   wymienia wroga – tekst *CRUSADE), `ClassicFreshGameRuins`, `ClassicSpaceshipSlots` (pełny rodzaj części: bit linii
   dialogu i „No further Propulsion required.”). Reguły: `TenthsRules`, `SpaceshipPictureRules`, `TurnEndRules`,
   `SpaceshipSlotRules`; testy `TenthsAndSpaceshipTests`, `TurnEndRulesTests`, `SpaceshipSlotTests` (oba warianty w
   grze, poza `ClassicFreshGameRuins` – tylko reguła). Pełny zestaw 797/797, mutacje 10 / 10 (jedna dopiero po
   wzmocnieniu testu: oczekiwany adres liczony niezależnie od reguły, część rodzaju 3 zamiast struktury).
2. `ClassicStackLeftovers`: funkcje czytające śmieci ze stosu dostają przy wyłączonym przełączniku jawną wartość (np.
   „brak ruchu”, „brak miasta”); przy włączonym czytają stos jak dziś. To odcina B3 od decyzji AI.
   **Zrobione 2026-10-25:** sonda (golden master + Differential + Dual) pokazała 31 funkcji (m.in. `HandlePlayerTurn` 37
   słów, `ShiftAIUnit` 33, `PickCityProduction`, `HandleCity` 0x62 / 0x6c / 0x108, `RenderCitizenIcons` 0x2,
   `ObtainDistanceToReplayCity` 0x4, `LocateNearestFriendlyCity` 0x14 / 0x16; część to bufory w ramce wypełniane
   zapisami, których sonda nie widzi). Zamiast łatać każdą: przy wyłączonym przełączniku `CPU.EnterFrame` zeruje
   zmienne lokalne nowej ramki (`CPU.ClearFrameLocals`, jak domyślna wartość zmiennej C#) i zgłasza je sondzie jako
   zapisane – każdy odczyt przed zapisem daje 0, niezależnie od wcześniejszych funkcji, więc B3 (usuwanie ramek) nie
   zmieni już zachowania, jeśli zmienne C# zaczną od 0. Jawna wartość inna niż 0: `LocateNearestFriendlyCity` bez
   miasta i lotniskowca – cel to pole jednostki (zostaje, kierunek 0). Gra (`GameEngine`) ustawia zerowanie,
   `HeadlessGame` włącza przełącznik przed przygotowaniem maszyny. Testy `StackLeftoverTests`: ramka w CPU, jednostka
   bez domu, 5 tur 7 autozapisów bez przełącznika bez wyjątku, przełącznik zmienia grę. (Szum pod stosem na granicy tur
   nic nie zmienia nawet w trybie klasycznym – śmieci zostawiają wywołania w tej samej turze – więc taki test nic nie
   dowodzi i go nie ma.)
3. Reszta B2 (zmienne lokalne → C#) i B1 (wyniki przez `return`), z sondą stosu jak dotąd.
   **Stan 2026-10-25:** B2 jest w praktyce zrobione – z 620 dostępów do ramki pod numerami tylko 123 to odczyty, i to
   głównie klasy zmiennych ramki (`UnitLook` w `AIPlayer`, `CityRound` w `CityManagement`, właściwości get / set na
   ramce); 497 to zapisy „Keep” (wartość jest już zmienną C#, ramka dostaje kopię). Kopie są potrzebne dopóki (a) w
   trybie klasycznym funkcje czytają śmieci ze stosu i (b) testy różnicowe porównują ramkę i stos pod nią z kopią
   oryginału – więc ich usunięcie dopiero po punkcie 4 i w fazie 2 (po nowym baseline'ie). B1: 77 odczytów AX w kodzie gry
   zamienionych na wartości C# (skrypty `b1ax.py`, `b1ax2.py`, `retax2.py` w scratchpadzie: para „AX = E; X = AX” →
   „X = E; AX = X”, odczyt AX kilka wierszy po zapisie czystego wyrażenia → samo wyrażenie, funkcja z jednym zapisem AX
   i `return AX` → `return` wartości); zapisy AX zostają (rejestry porównują testy). Zostało 73 odczytów (`DisplayMenu`,
   `ImportTextFromFile`, rozgałęzienia `HandleCity`, przerwania i sterowniki – te ostatnie to interfejs rejestrów,
   zostają do B6.8). Pułapka: zdejmowanie nawiasów regexem `\(\((ushort)\)x\)` psuje wywołania `F((ushort)x)` –
   tylko na zmienionych wierszach. Pełny zestaw 809/809; mutacje 7 / 8 (dwie dopiero z właściwym testem:
   `RenderSpaceshipMatchesOriginalCode`, przypadki kliknięć pól mapy miasta w `HandleCityCasesMatchFrozenGame`;
   ocalała `TileValue + 1` równoważna – wszystkie pola przesunięte o tyle samo, porównywane tylko między sobą).
4. Testy różnicowe porównują sam stan (bez rejestrów i stosu pod SP), żeby usuwanie SI/DI w B3 nie dawało fałszywych
   alarmów. **Zrobione 2026-10-25:** `DifferentialPair` porównuje tylko AX i DX (wyniki funkcji – kopie Reference oddają
   je tylko w rejestrach; DX dla wyników 32-bitowych), niezależnie od flag `Register` testu, i pomija okna `Watched` na
   stosie (0xe8c0 i wyżej); ślad wywołań już wcześniej zastępował adresy stosu przez `<stack>`. Pełny zestaw 809/809.
   Kontrola mutacjami: zmiana samego SI (`HandlePeaceTreaties`) przechodzi – zamierzenie; zmiany rysowania
   (`DisplayNewspaper`, `ExamineCity`) i wyniku (`RenderSpaceship`) dalej łapane. README testów różnicowych i skill
   `migrate-batch` poprawione.
5. Tag `classic-final`: ostatni stan zgodny z oryginałem, ze starymi baseline'ami.

**Faza 1 – nowy baseline (jeden commit):** **zrobione 2026-10-26.** Tag `classic-final` (ostatni stan zgodny z
oryginałem, stare baseline'y). `HeadlessGame(…, classic: false)` domyślnie gra jak remaster; `classic: true` włącza
wszystkie przełączniki (`HeadlessGame.TurnClassic`) – tak robią `DifferentialPair` i `DualGame` (porównania z kodem
oryginału i z zamrożoną grą). Baseline'y golden master nagrane od nowa z gry remasteru. Raport (kanały zachowania,
`classic-final` → nowe; po wczytaniu wszędzie identycznie, tura 1: leczenie `Vitals.missingStrikeMarks` i stan
generatora losowego – dwa losowania katastrof – potem coraz więcej, bo komputery grają inaczej):

   | Autozapis | Różnych wartości w turach 0 – 5 | Najczęściej zmienione kanały |
   |---|---|---|
   | AUTO00 | [0, 8, 35, 78, 172, 161] | mem 138, state.Participants[7] 43, state.Participants[4] 35, state.Towns[9] 24, map 20, state.Vitals 17 |
   | AUTO01 | [0, 4, 8, 50, 62, 85] | mem 77, state.Participants[2] 31, state.Towns[29] 18, map 12, state.Participants[6] 10, state.Vitals 9 |
   | AUTO02 | [0, 4, 48, 85, 117, 140] | mem 115, state.Participants[2] 45, state.Vitals 25, state.Participants[4] 20, state.Towns[29] 19, state.Towns[14] 18 |
   | AUTO03 | [0, 14, 54, 85, 111, 136] | mem 139, state.Participants[2] 48, state.Participants[4] 31, state.Vitals 26, map 20, state.Participants[7] 17 |
   | AUTO04 | [0, 4, 54, 38, 54, 85] | mem 70, state.Participants[2] 51, state.Vitals 18, state.Participants[4] 18, state.Towns[29] 16, state.Towns[9] 15 |
   | AUTO05 | [0, 3, 10, 26, 30, 36] | mem 59, state.Participants[2] 12, state.Vitals 6, map 6, rng 5, state.Towns[29] 5 |
   | AUTO06 | [0, 2, 32, 38, 68, 101] | mem 76, state.Participants[2] 26, state.Participants[7] 24, state.Participants[4] 23, state.Towns[9] 17, state.Vitals 13 |
   | AUTO07 | [0, 3, 18, 27, 100, 116] | mem 98, state.Participants[7] 56, state.Participants[4] 19, state.Towns[9] 19, state.Vitals 15, state.Participants[2] 11 |
   | AUTO08 | [0, 7, 22, 21, 93, 111] | mem 87, state.Participants[2] 27, state.Vitals 23, state.Participants[4] 15, map 10, state.Participants[6] 10 |
   | AUTO09 | [0, 5, 7, 29, 56, 134] | mem 86, state.Participants[7] 19, state.Participants[2] 13, map 13, state.Participants[4] 13, state.Towns[9] 13 |
   | AUTO10 | [0, 3, 32, 94, 109, 133] | mem 124, state.Participants[2] 29, state.Participants[6] 28, state.Vitals 27, state.Participants[7] 26, map 17 |
   | AUTO11 | [0, 5, 60, 104, 140, 145] | mem 134, state.Participants[7] 66, state.Participants[2] 43, state.Participants[4] 24, state.Towns[14] 23, state.Towns[16] 17 |
   | AUTO12 | [0, 2, 15, 32, 55, 88] | mem 68, state.Participants[7] 22, state.Participants[2] 21, map 8, state.Towns[2] 7, state.Participants[4] 7 |
   | AUTO13 | [0, 3, 10, 39, 32, 55] | mem 66, state.Participants[4] 11, state.Towns[1] 9, map 9, state.Vitals 8, state.Participants[7] 8 |
   | AUTO14 | [0, 6, 6, 34, 47, 58] | mem 59, state.Participants[2] 22, state.Vitals 16, state.Participants[4] 10, state.Towns[1] 10, rng 5 |

Plan pierwotny: `HeadlessGame` wyłącza wszystkie `Classic…`, baseline'y golden master
nagrane od nowa (15 autozapisów × 5 tur) z raportem, które kanały w których turach się zmieniły i dlaczego; test
determinizmu i kilka tur w GUI. Testy z zamrożoną grą (Dual) i różnicowe dla zmienionych funkcji włączają `Classic…`
jawnie – działają dalej do fazy 3.

**Faza 2 – B3 + B6.8 pod nowym baseline'em:** **Przygotowanie zrobione 2026-10-26:** porównania z kodem oryginału
zerują ramki po obu stronach – `HeadlessGame.TurnClassic` nie włącza już `ClassicStackLeftovers` (bieżące CPU zeruje
domyślnie, `DifferentialPair` ma obie gry na nim), a zamrożona gra dostała szew `CPU.ClearFrameLocals` (włącza go
`DualGame`). Pełny zestaw 809/809 bez innych zmian. Odtąd żaden test nie zależy od tego, co jedna funkcja zostawia
na stosie dla drugiej, więc zapisy „Keep”, klasy zmiennych ramki i ramki można usuwać.
**B3, porcja 1 (2026-10-26):** 171 kopii „Keep” usuniętych w `CityFoundingAndScore`, `GovernmentAndDiplomacy`,
`MapManagement`, `Pathfinding`, `UnitCombat` (skrypt `b3keep.py`: zapis do słowa ramki, którego nic w pliku nie czyta –
`PullLocal…`, `Increment/DecrementLocal…`, właściwość ramki, nazwana stała; pliki z wyliczanymi adresami ramki
pominięte; ciało `if`/`for` bez klamer zostaje). Pułapki (złapane testem `ProduceTerrainMatchesOriginalCode`):
pomocnicze metody czytają ramkę wołającego przez wyliczony adres (ochrona per metoda nie wystarcza – cały plik),
tablice w ramce są adresowane przez zmienną `frame = RegFrame` (`frame - 0xce`) i czytane przed bazą (indeks -1:
`ranking + 2 * lower - 2` = bp-0xd0). Z ochroną całego pliku i marginesem 0x10 w plikach z tablicami w ramce nie
zostało nic do usunięcia – tam trzeba najpierw zamienić tablice ramki na tablice C# (następna porcja). Pełny zestaw
809/809. Heredoc w Bash zamienia `` w regexie na znak backspace – skrypty pisać narzędziem Write.
**B3, porcja 2 (2026-10-26):** ramki bez żadnego dostępu usunięte (`EnterFrame` / `LeaveFrame`, zapis SI/DI zostaje):
`RenderMapTile`, dwie w `CityFoundingAndScore`, `SettleBattle` i jedna w `UnitCombat`, `HandlePeaceTreaties`.
`DivideCivilization` musiało ramkę zachować: na części ścieżek oryginał wychodzi emulowanym skokiem i zostawia ramkę na
stosie, a testy porównują wskaźnik stosu (`cpu.sp`). Pułapka: przed commitem czytać wynik testów, nie łańcuchować
commita za testem.
**B3, porcja 3 (2026-10-26):** 9 odczytów SI/DI zaraz po zapisie czystego wyrażenia → samo wyrażenie (`b3sidi.py`);
reszta (ok. 100) to wartości zostawiane przez wywołane funkcje – po kolei. `UnitLook` (43 zmienne `ShiftAIUnit`,
słowa 0x2 – 0x5e ramki) to zwykły obiekt C#, nowy na każde wywołanie (wszyscy użytkownicy działają pod ramką
`ShiftAIUnit`, bez rekursji; żadna kopia Reference nie woła tych metod). Etykiety gałęzi sześciu testów `ShiftAIUnit…`
czytały słowa ramki z gry przepisanej – teraz z gry oryginalnej (`pair.Original`), która dalej je zapisuje. Mutacje
2 / 3 (`HeadingLow` równoważna: starszy bajt kierunku zawsze 0). `CityRound` (HandleCity) zostaje na razie na ramce:
jego słowa nakładają się na tablice ramki (0x8e to baza tablicy, 0x16 to pole 20 tablicy bp-0x3e, 0x40 – 0x50 w
zasięgu tablicy bp-0x74) – trzeba przenieść razem z tablicami.
`TurnLocals` (HandlePlayerTurn, 38 zmiennych) startuje jako pusty obiekt C# (bez `FromFrame` i bez kopii `Keep` do
ramki); pełny zestaw 809/809 przed uproszczeniem klasy, golden master + Dual po nim. `CityRound`: zmienne z
kopiami `Keep` (słowa 0xa8 – 0x10c, powyżej wszystkich tablic ramki, które kończą się na 0xa6) nie piszą już do
ramki (809/809); zmienne 0x2 – 0x108 czytane przez ramkę zostają, dopóki tablice ramki nie przejdą do C#.
Potem cała ramka `HandleCity` (0x10c bajtów) jako tablica słów w `CityRound` (`Frame` / `SetFrame` po odległości od BP,
16-bitowo – indeks tablicy bywa ujemny; poza ramką dalej stos): 18 zmiennych i 16 dostępów do tablic (bp-0x3e, -0x74,
-0x8e, -0xa6), układ i nakładanie jak w oryginale. Test `LeftoversChangeTheGame` usunięty: po B3 tury tych
autozapisów nie czytają już nic ze stosu, więc przełącznik `ClassicStackLeftovers` ich nie zmienia (działanie
przełącznika pokazuje `UnitWithoutHomeStays`). Pełny zestaw 808/808 (przed usunięciem testu 809 z tym jednym), mutacje 3 / 3.
Wspólna klasa `Engine/FrameWords` (słowa ramki po odległości od BP, 16-bitowo, poza ramką stos); `CityRound` z niej
korzysta. **Nieudane:** ramka `ProduceTerrain` (0xd4, wspólna z `RankLandOrSea`, `LabelLandOrSea`, `JoinRowAbove`)
w `FrameWords` psuje `ProduceTerrainMatchesOriginalCode`, a zapis „na wylot” do pamięci go naprawia – czyli coś czyta
te słowa ze stosu, ale nie przez CPU (sonda `IStackWatcher` na ramce nie widzi ani jednego odczytu, także zawężenie do
zakresów 2 – 0x50 / 0x52 – 0xa2 / 0xa4 – 0xd4 nie wystarcza). Cofnięte; do zbadania przed B6.8 (odczyty przez
`Storage` / sterowniki).
**B3, porcja 4 (2026-10-26):** skaner ramek bez dostępów (`emptyframes.py`: metoda i jej pomocnicze bez ramek z tego
samego pliku nie dotykają lokali, stosu ani BP) – 13 ramek usuniętych: `BuyUnit`, `FinalizeProduction`,
`DisplayAttitudeReport`, trzy `RenderCityScenery…`, `DisplayReplay`, `RenderReplayMapTile`, `ChoreographKingSpeaking`,
`ComputeSpaceshipSuccess`, `ConfigureFreshGame`, `ConstructCivilization`, `DisplayWorldMap`. Trzy zostały, bo oryginał
na części ścieżek zostawia ramkę na stosie (`cpu.sp`): `OpeningSequence`, `DisplayConquerorScreen`, `LaunchAudience`
(jak `DivideCivilization`). Pełny zestaw 808/808. Potem trzy z kilkoma wyjściami lub `LeaveFrame` w metodzie
kończącej: `ManageBarbarianUnit`, `DiplomatArrivesAtCity` (`FinishDiplomatArrival`), `AttachSpaceshipPart`
(`LeaveAttachSpaceshipPart`) – 808/808.
**B3, porcja 5 (2026-10-26):** `Pathfinding` bez ramek na stosie: `CarryOutGoto` – kopie jego pomocniczych (nikt ich nie
czytał) i ramka usunięte; ramki `ChartRoute` (0x3a, `SearchRouteBlocks`) i `ShiftToward` (0x4a, `ChooseCheapestStep`,
`CrowdAndDistance`, `ChartWindowCosts`, `ExpandWindowCell`) jako `FrameWords`, nowe na każde wywołanie. 808/808.
**B3, porcja 6 (2026-10-26):** skrypt `b3frames.py` – ramka, do której właściciel i jego pomocnicze bez ramek sięgają
tylko nazwanymi słowami (`PullLocalWord` / `EmitLocalWord` / `Increment…`), staje się `FrameWords` (pole
`<właściciel>Frame`, nowe na każde wywołanie); pominięte grupy z wyliczanymi adresami, bajtami, BP, `LeaveFrame` w
pomocniczej albo pomocniczą sięgającą do ramki wołaną też spod innej ramki. Dziewięć: `LabelLandmasses`,
`ReckonContinentSizes`, `DisplayTravellersReport`, `HandleEndOfTurnEvents`, `LocateNearestFriendlyCity`,
`ObtainDistanceToReplayCity`, `RenderSpaceship`, `ObtainNavigationKey`, `TitleLogoAndMainChoices`. 808/808.
`FrameWords` czyta i pisze też bajty (`FrameByte` / `SetFrameByte`: parzysta odległość to młodszy bajt słowa, nieparzysta
starszy bajt słowa poniżej); dzięki temu `DisplayMainIntro` (0x4e, 15 pomocniczych) – 808/808.
**Następne kroki pętli B3 (stan 2026-10-26 rano):** (1) ramki z wyliczanymi adresami (tablice, bufory) jak w
`CityManagement`: `FrameWords` + adres `RegFrame + x - baza` → odległość `baza - x` (skrypt `b3cityframe.py` jako wzór;
bufory przekazywane przez adres do funkcji tekstu / plików zostają na stosie do B6.8); kandydaci z `b3frames.py`
„computed or byte”: `DisplayMenu`/`OperateMenu`, `TradeAndConquest`, `ScienceAndAdvisors`, `AdvisorReports`,
`DiplomatActions`, `TurnEvents`, `GameStartup.RunGame`, `TextBlockRenderer`, `Palace`, `Newspaper`, `MainIntro`;
(2) zagadka ramki `ProduceTerrain` (wyżej); (3) ok. 100 odczytów SI/DI zostawianych przez wywołane funkcje; (4) pary
`PreserveIndexRegisters` / `RestoreIndexRegisters` tam, gdzie nikt nie czyta SI/DI po powrocie. Ramki z „przeciekiem”
(`DivideCivilization`, `OpeningSequence`, `DisplayConquerorScreen`, `LaunchAudience`) zostają do usunięcia porównania
`cpu.sp` w fazie 3. Narzędzia w scratchpadzie sesji: `emptyframes.py`, `dropframes.py`, `b3frames.py`, `frametree.py`,
`b3keep*.py`, `b3sidi.py`, `edits.py`. ramki, SI/DI, BX/CX/DX moduł po module; przy jawnych wartościach
zamiast śmieci golden master zostaje zielony, sonda stosu ma pokazać zero czytelników. Na końcu rejestry z CPU,
`MZExecutable` i obraz CIV.EXE w pamięci (dane początkowe z pliku przy starcie).

**Pętla B3, 2026-10-26 (dalej):** do C# przeszły: zmienne 0x12 / 0x18 `ImportPalaceGraphics` (ramka zostaje dla nazwy
pliku), tablica sił `RefreshPeaceGraphContent` (bez ramki), tablice poziomów i stylów `RenderPalace` (`FrameWords` z
nakładaniem tablic, bez ramki), ranking `NoteStatusInReplay` (`FrameWords` ramki `HandleGameEnd`; ramka zostaje, bo
`LeaveFrame` sprząta to, co tury graczy zostawiają na stosie; konstruktor daje ramkę na wywołanie samej funkcji w
testach), obrazki `RenderExplosion` i `ExamineCity` (bez ramek), wartości graczy `DisplayEpicChronicle`, wolna pamięć
`RunGame`; usunięte zapisy-resztki do ramek w `ComputeInciteCost`/`Revolt`, `HandleAllCities`, `DisplayMenu`,
`OperateMenu` (ramki zostają: głębokość wywołań `HandleCity` / menu). Zegar testów nie liczy dostępów do pamięci (tylko
wywołania, odpytania zegara, Idle), więc tablice C# zamiast słów stosu nie przesuwają przerwań. Odłożone:
`PickCityProduction` (bufor klucza porady bp - 0x12e przekazywany przez adres sąsiaduje z `BestUnitScore` 0x11e –
`FrameWords` potrzebowałby zakresu „na stosie”), `RenderCensusLine` (publiczna, dostaje adres tablicy), `ProduceTerrain`.
Lekcja: test wołający funkcję pomocniczą przez refleksję (`TurnEndRulesTests`) nie przechodzi przez właściciela ramki. Dalej: `DisplayTradeReport`
(koszty budynków), `ChooseTechnologyToTake` (lista technologii; lokalna 0x4 zostaje na stosie – czytana przed zapisem),
zapisy-resztki `SeizeCity`, `ImportTextFromFile`, `DisplayMilitaryReport`. Lekcja 2: **słowo ramki czytane przed
zapisem zostaje w ramce** – z `ClassicStackLeftovers` ma tam być to, co zostawiły wcześniejsze funkcje: `RenderPalace`
(słowo 0x34, styl sekcji 0) i `RefreshPeaceGraphContent` (0x1a) wróciły do ramek, `FrameWords.WithLeftovers()` bierze
słowa ramki ze stosu, `ImportPalaceGraphics` zaczyna 0x12 / 0x18 od `PullLocalWord`; lista wierszy
`DisplayMilitaryReport` zostaje na stosie (`SchismAndReportCrashTests` sprawdza śmieci). Etykiety gałęzi testów
różnicowych czytające lokalne z ramki biorą `pair.Original` (`ImportTextFromFileMatchesOriginalCode`).

**Zagadka `ProduceTerrain` rozwiązana (2026-10-26):** sonda (nakładka pamięci na ramkę, łapie też odczyty przez
`Storage`) pokazała, że jedynym czytelnikiem jest `RankLandOrSea`: `numbers[etykieta]` dla etykiet ≥ 0x53 czyta ponad
BP, ujemnych (sbyte) – pod ramką. W teście jeden scenariusz kończy się wyjątkiem w obu wersjach i zostawia ramkę
(0xda bajtów) na stosie; następny scenariusz biegnie głębiej i czyta tę starą ramkę – oryginał ją ma w pamięci, C# nie.
Artefakt testu: `ProduceTerrainMatchesOriginalCode` ma teraz `fresh: true`, ramka w `FrameWords` (`WithLeftovers`).
**Znaleziona rozbieżność do decyzji (nie z tej zmiany):** w prawdziwej grze etykiety 0x57 – 0x5d czytają ramkę
`OpeningSequence` (jej lokalne – liczniki pętli – oryginał zostawia tam, remaster ma je w C#, ramka zera), a niezerowa
„liczba” rysuje kropkę na mapie ekranu (y + 50). Tak samo każde czytanie poza tablicą ponad BP trafia w ramki
wywołujących, które B2 / B3 przeniosły do C#. Rzadkie (ponad 0x56 etykiet lądu lub mórz), golden master nie łapie;
do rozstrzygnięcia przy poprawkach zachowania (zdefiniować: etykiety bez numeru = 15).

**Odczyty SI / DI (punkt 3, 2026-10-26):** sonda (tymczasowa klasa w Core: przy każdym odczycie rejestru porównanie z
wartością ostatniego przypisania w tej samej metodzie, zapis zgodności / różnicy do pliku, pełny zestaw) potwierdziła
36 odczytów – teraz zmienne C# (przypisanie rejestru zostaje dla kodu, który go czyta później). Zostają: odczyty po
funkcjach, które zostawiają w rejestrze co innego (`TallyUnitsByDomainInStack`, `TallySpecialUnitsInStack` – SI =
0x600 x gracz: AIPlayer 999 / 1009 / 1268, PlayerTurn 2189 / 2722 – 2724, CityManagement 4671), odczyty, do których
testy nie dochodzą (PlayerTurn: ścieżka jednostki 23, 1210, 3118; GovernmentAndDiplomacy 172), klasyczna groźba
emisariusza (DI celowo) i `SystemServices` (zapis / odczyt stanu rejestrów). Nie podstawiać wyrażeń z wywołaniami funkcji
gry w miejsce odczytu – zmienia ślad wywołań; zmienna lokalna w miejscu przypisania.

**Odczyty poza tablicami (decyzja użytkownika 2026-10-26: jawna definicja za przełącznikiem `Classic…`):** sonda w
`FrameWords` (każdy dostęp poza ramką, pełny zestaw) znalazła tylko `ProduceTerrain`: `terrainCounts[teren]` dla terenu
spoza tabeli (zapis do pamięci, nie czytanie stosu – zostaje) i numery etykiet. `ClassicLabelNumbers`: etykieta poza
1 – 0x3f (także ujemna) dostaje 15 jak inne nierankowane; włączony (testy) czyta poza tablicą jak oryginał
(`LabelNumberTests`). Zapisy istniejące tylko dla czytelników niezainicjowanych zmiennych działają tylko z
`ClassicStackLeftovers`: kopia liczników w ramce `OperateAITurn` (`AIPlayer.DirectUnits`), lokalne `ShiftUnit`
(`ShiftUnitFrame.Leave`). Golden master bez zmian (813/813).
**Dalej zależy od losu `ClassicStackLeftovers`:** w remasterze ramki są zerowane, więc ramki trzymane „dla głębokości”
(`HandleAllCities`, `ComputeInciteCost`, `DisplayEpicChronicle`, `GameReplay`, `HallOfFame`, `MeetWithKing`,
`StartGameMenu`, `SpaceshipScreen`, menu…), pary `Preserve`/`Restore` bez czytelników i słowa czytane przed zapisem
(`WithLeftovers`, `RenderCitizenIcons` 0x2, `ChooseTechnologyToTake` 0x4) potrzebuje tylko tryb resztek. Jeśli tryb
zostaje dla graczy, ramki trzeba warunkować; jeśli odpada w fazie 3 – można je usunąć razem z nim.
**Decyzja użytkownika 2026-10-26: `ClassicStackLeftovers` odpada w fazie 3** (nie jest opcją dla graczy): śmieci ze
stosu znikają, gra zachowuje się przewidywalnie. Do końca migracji przełącznik służy tylko porównaniom z oryginałem w
testach; w fazie 3 znika razem z nimi, a wtedy usuwamy emulację stosu: pozostałe ramki („dla głębokości”, z resztkami),
pary `Preserve`/`Restore`, `WithLeftovers`, zapisy-resztki (`ShiftUnitFrame`, kopia w `DirectUnits`). Jako opcje dla
graczy zostają tylko zmiany reguł (leczenie, koleje, konwersja produkcji…).
**Zrobione od razu (2026-10-26, prośba użytkownika):** przełącznik i `CPU.ClearFrameLocals` usunięte – `EnterFrame`
zawsze zeruje zmienne (zamrożona gra `DualGame` ma swój przełącznik). Usunięte: `ShiftUnitFrame`, kopia liczników w
`DirectUnits`, klasyczna ścieżka `LocateNearestFriendlyCity`, `WithLeftovers`, ramki czytane przed zapisem
(`RenderPalace`, `RefreshPeaceGraphContent`, `RenderCitizenIcons`, `ChooseTechnologyToTake`, raport wojskowy), 11 ramek
„dla głębokości” (skaner `depthframes.py`: `DisplayMenu`, `RunGame`, `OperateAITurn`, `ShiftAIUnit`, `HandlePlayerTurn`,
`HandleAllCities`, `EarnTechnology`, `SeizeCity`, `ComputeInciteCost`, `DisplayTradeReport`, `DisplayEpicChronicle`);
ręczne `RegX = UnstackWord()` → `RestoreSource()` / `RestoreDest()`. **Pary `Preserve`/`Restore` (2026-10-26):** sonda (przy każdym przywróceniu porównanie z bieżącym rejestrem, pełny
zestaw) wskazuje metody, w których para nigdy niczego nie przywraca. Pierwsza próba zepsuła golden master – przyczyna
(bisekcja): błąd skryptu w `VGADriver.FloodBuffer` / `DuplicateScreenToMainScreen`, które odkładają ręcznie także DI,
DS i ES – skrypt uznał `PreserveSource` + `RestoreDest` + `RestoreSource` za parę i rozjechał stos. Kryteria teraz:
`PreserveSource` tylko z `RestoreSource`, komplet SI i DI dla `PreserveIndexRegisters`, bez ręcznych `StackWord` /
`UnstackWord` rejestrów, bez ramek „przeciekających”. Usunięte pary ok. 35 metod; zostają te, które przywracają
rejestr (31), nieosiągnięte w testach, z ręcznymi odłożeniami i w ramkach „przeciekających”. Testy podwójnej gry
(`DualCityRandomTests`, `DualCityCaseTests`) też przywracają SP / BP po wyjątku w przypadku (wersje zostawiają różne
ramki i rejestry na stosie). Zostają ramki „przeciekające” i ramki z buforami. Testy: `StackLeftoverTests` tylko z regułą remastera (jednostka bez domu zostaje), raport wojskowy klasyczny
bez wyjątku (czyta 0), `DifferentialPair` po wyjątku w scenariuszu przywraca SP / BP (wersje zostawiają różne ramki).

**B6.8, porcja 1 (2026-10-26):** bufory nazw plików i tekstów przeniesione ze stosu do C# w `TurnEvents.DisplayCivilizationScenery`, `SpaceshipScreen.ImportSpaceScenery`, `Palace.ImportPalaceGraphics` (`RenderPalaceBush`, `ImportPalaceCastle`) oraz `GameLoadAndSave.ImportGame`. W `ImageTools` przeciążenia `ImportPictureOrColors` i `ImportIcon` przyjmujące `string`; w `CRuntime` metoda `ReadText`. Usunięto 5 ramek stosu (`EnterFrame`/`LeaveFrame`), pozostało 46. 8 z 8 mutacji złapanych (BatchEleven, BatchTwelve, BatchFortyTwo, BatchThirty, BatchThirtyTwo), Golden Master 16/16 zielony.

**B6.8, porcja 2 (2026-10-26):** bufory nazw plików graficznych przeniesione ze stosu do C# w `StartGameMenu.ImportGovernmentGraphics` (`govt{regime}{tech}.pic`), `MeetWithKing.DisplayKingScreen` / `NameAdvisorsPicture` (`govt{regime}{tech}.pic`, `king{tens}{ones}.pic`), `MeetWithKing.ImportKingScenery` (`back{regime}{tech}.pic/.pal`, `king{tens}{ones}.pal`) oraz `GameInitAndIntro.DisplayStoryIntro` / `ShowStoryPicture` (`birth{N}.pic/.pal`). Usunięto 4 ramki stosu (`EnterFrame`/`LeaveFrame`), pozostały 42. 8 z 8 mutacji złapanych (BatchEight, BatchTwenty, BatchFortyOne, BatchFiftyFour), Golden Master 16/16 zielony.

**B6.8, porcja 3 (2026-10-26):** bufory nazw plików i tekstów przeniesione ze stosu do C# w `GameReplay.ObtainReplayPlayerLabel` (bufor nazwy w liczbie mnogiej), `GameReplay.DisplayCivilizationDestroyedScreen` (`king{tens}{ones}.pic/.pal`, `slam2.pic`/`slag2.pic`) oraz `NewCityScreen.DisplayLabelInputBox` (bufor nazwy miasta na stosie do porównywania prefiksów). Usunięto 3 ramki stosu (`EnterFrame`/`LeaveFrame`), pozostało 39. 8 z 8 mutacji złapanych (BatchEight, BatchForty, BatchSixteen), Golden Master 16/16 zielony.

**B6.8, porcja 4 (2026-10-26):** bufory i ramki stosu przeniesione do C# w `TextBlockRenderer.ReflowText` (bufor `rest` na 516 bajtów zastąpiony ciągiem C# z `PullText`), `NewCityScreen.PullTextInput` (bufor `kept` w klawiszu Insert zastąpiony ciągiem C#), `DialogDrawing.ObtainNavigationKey` (usunięto ramkę `0x2` i pole `obtainNavigationKeyFrame`) oraz `CommonUtilities.ReleaseResource` (usunięto pustą ramkę stosu C). Usunięto 4 ramki stosu (`EnterFrame`/`LeaveFrame`), pozostało 35. 8 z 8 mutacji złapanych (BatchNine, BatchThirtyThree, BatchThirtyNine), Golden Master 16/16 zielony.

**B6.8, porcja 5 (2026-10-26):** bufory i ramki stosu przeniesione do C# w `GameInitAndIntro.LabelLandmasses` (usunięto ramkę 0x26 i pole `labelLandmassesFrame`), `GameLoadAndSave.VerifyDrive` (usunięto ramkę 0x12, `request` zastąpiony `BiosDiskCall`), `ScienceAndAdvisors.UncoverTechnology` / `AnnounceDiscovery` / `ShowDiscoveryPicture` (usunięto ramkę 0x46, `pictureName` jako string C# dla `.pic` i `.pal`), `TurnEvents.HandleEndOfTurnEvents` / `TryRivalWonder` (usunięto ramkę 0xe i pole `handleEndOfTurnEventsFrame`). Usunięto 4 ramki stosu (`EnterFrame`/`LeaveFrame`), pozostało 31. 8 z 8 mutacji złapanych (BatchNine, BatchForty, BatchFortyOne, BatchFortyFour), Golden Master 16/16 zielony.

**B6.8, porcja 6 (2026-10-26):** ramki i bufory stosu przeniesione do C# w `TurnEvents.RenderStatusBar` (usunięto pustą ramkę 0x8), `GameReplay.ObtainDistanceToReplayCity` (usunięto ramkę 0x6 i pole `obtainDistanceToReplayCityFrame`, `nearest` w C#), `GameStartup.TitleLogoAndMainChoices` / `AwaitTitleSongMeasure` (usunięto ramkę 0x6 i pole `titleLogoAndMainChoicesFrame`), `SpaceshipScreen.RenderSpaceship` / `MoveSpaceshipStars` (usunięto ramkę 0x20 i pole `renderSpaceshipFrame`, `key` w C#) oraz `HallOfFame.DisplayTopCities` (usunięto ramkę 0x20, `scores` i `towns` w tablicach C#). Usunięto 5 ramek stosu (`EnterFrame`/`LeaveFrame`), pozostało 26. 10 z 10 mutacji złapanych (BatchTwentySeven, BatchSeven, BatchFiftyFive, BatchSixtyOne, BatchTwentyEight), Golden Master 16/16 zielony.

**B6.8, porcja 7 (2026-10-26):** ramki i bufory stosu przeniesione do C# w `GameInitAndIntro.ReckonContinentSizes` / `LinkSeaBlock` / `SeaPassageOpen` (usunięto ramkę 0x18 i pole `reckonContinentSizesFrame`, `lastNextSea` przekazywane przez `ref`, czysta logika przejść morskich), `Pathfinding.ChartRoute` / `SearchRouteBlocks` (usunięto ramkę 0x3a i pole `routeFrame`), `Pathfinding.ShiftToward` / `ChooseCheapestStep` / `CrowdAndDistance` (usunięto ramkę 0x4a i pole `stepFrame`, `CrowdAndDistance` jako metoda pomocnicza C# zwracająca `short`, zmienna lokalna `bestCrowdAndDistance`) oraz `StartGameMenu.CharacterizeStartLocation` (usunięto ramkę 0x5e, usunięto nieużywane obliczenia `frame - 0x5a` / `0x1c`). Usunięto 4 ramki stosu (`EnterFrame`/`LeaveFrame`), pozostały 22. 8 z 8 mutacji złapanych (BatchFortyTwo, BatchFortyThree, BatchFortyFive), Golden Master 16/16 zielony.

**B6.8, porcja 8 (2026-10-26):** eliminacja 6 ramek stosu w modułach UI i silnika: GameStartup.QueryForDisk (ramka 0x102), Newspaper.DisplayNewspaper (ramka 0x10, bufor *NEWS{A..N} w C#), Civilopedia.DisplayCivilopediaIndex (ramka 0x5c, bufory nazw), Civilopedia.DisplayCivilopediaRecord (ramka 0x34, bufor tagBuf), Palace.DisplayPalace (ramka 0x110) oraz InGameHelp.DisplayHelpTopic (ramka 0x18, bufor tematu pomocy). Usunięto 6 ramek stosu (EnterFrame/LeaveFrame), pozostało 16. 12 z 12 mutacji złapanych (BatchEighteen, BatchThirtyThree, BatchFiftyNine, BatchSeventyOne, BatchSixtyEight), Golden Master 16/16 zielony.

**B6.8, porcja 9 (2026-10-26):** ramki i bufory stosu przeniesione do C# w TurnEvents.DisplayTravellersReport (usunięto ramkę 0x1c i pole displayTravellersReportFrame), TurnEvents.HandleGameEnd (usunięto ramkę 0x1a i pole handleGameEndFrame, ranking replayu spakowany w C# do tablicy bajtów) oraz WorldMap.DisplayCensus (usunięto ramkę 0xba, wartości linii spisu w pamięci roboczej). Usunięto 3 ramki stosu (EnterFrame/LeaveFrame), pozostało 13 (w tym 5 odłożonych do Fazy 3). 6 z 6 mutacji złapanych (BatchForty, BatchSixtyFour, BatchSixtyTwo), Golden Master 16/16 zielony.

**B6.8, porcja 10 (2026-10-26):** ramki i bufory stosu przeniesione do C# w `ScienceAndAdvisors.PickCityProduction` (usunięto ramkę 0x14a, bufor klucza porady przeniesiony do pamięci roboczej) oraz `MainIntro.DisplayMainIntro` (usunięto ramkę 0x4e i pole `displayMainIntroFrame`, zmienne lokalne ekranów/kolumny/loga przeniesione do pól C#). Usunięto 2 ramki stosu (`EnterFrame`/`LeaveFrame`), pozostało 11 (w tym 5 odłożonych do Fazy 3). 4 z 4 mutacji złapanych (BatchSeventyFive, BatchSeventyTwo), Golden Master 16/16 zielony.

**B6.8, porcja 11 (2026-10-26):** ramki i bufory stosu przeniesione do C# w `CityView.DisplayCityScreen` (usunięto ramkę 0x186, bufor tekstu ekranu miasta przeniesiony do pamięci roboczej) oraz `TextBlockRenderer.ImportTextFromFile` (usunięto ramkę 0x110, bufory linii i wpisu indeksu przeniesione ze stosu emulacji). Usunięto 2 ramki stosu (`EnterFrame`/`LeaveFrame`), pozostało 9 (w tym 6 odłożonych do Fazy 3). 4 z 4 mutacji złapanych (BatchThirty, GoldenMaster), Golden Master 16/16 zielony.

**B6.8, porcja 12 (2026-10-26):** eliminacja ramki stosu w `CityManagement.HandleCity` (usunięto ramkę 0x10c `EnterFrame(0x10c)` / `LeaveFrame()`, obliczenia miasta działają na instancji `CityRound`). Usunięto ostatnią standardową ramkę stosu emulacji. Pozostało dokładnie 8 ramek odłożonych do Fazy 3 (5 z wyciekiem stosu w DOS: `OperateMenu`, `DivideCivilization`, `OpeningSequence`, `DisplayConquerorScreen`, `LaunchAudience`; 1 ramka VGA: `DuplicateScreenToMainScreen`; 2 ze specjalnym zachowaniem klasycznym: `ProduceTerrain` [ClassicLabelNumbers] i `DisplayCivilizationScreen` [dostęp do tyknięć zegara]). 2 z 2 mutacji złapanych (GoldenMaster `PlayedTurnsMatchBaseline`), Golden Master 15/15 zielony.

**B6.4 (2026-10-27):** eliminacja surowych odczytów i zapisów słów globalnych `PullGlobalWord(0x...)` / `EmitGlobalWord(0x...)` w klasach `GameModule`. Zmienne nazwane i dodane do `GameGlobals` (pola `GlobalWord`, wpisy w `Layout` i `All`) oraz właściwości dostępowe w `GameModule`: `DialogNeedsRedraw` (0xdb38), `MenuMoreBox` (0x8066), `MenuLineOffsets` (0x654e), `ScoreVerticalShift` (0x5116), `SchismShareDc6a` (0xdc6a), `DoubleWidthText` (0x6b8c), `NewspaperCrowdPicture` (0xdf0c), `AudienceInitialPrompt` (0xd2dc), `UnitSupportCount` (0xdeb6), `ImportPlayerIndex` (0x6820), `TileFortressPicture` (0xd4da), `MiniSymbolPicture` (0xb880), `UnhappyCitizenPicture` (0x6e9e), `PollutionIconPicture` (0xb2ba), `SpaceshipHabitatPicture` (0x6834), `IntroReservedScreen` (0x32d6), `PalaceLeftWingWidth` (0x4b2c), `PalaceRightWingWidth` (0x4b34), `PalaceStyleShift` (0xd922), `PalaceGardenBackground` (0xd932), `SavedGovernmentTitleCommunism` (0x33a8), `SavedGovernmentTitleMonarchy` (0x33aa). Zaktualizowano 17 plików w `Civilization1Remaster.Core`. 2/2 mutacje złapane (GoldenMaster `PlayedTurnsMatchBaseline`), Golden Master 15/15 zielony.

**B6.4 / B6.2 część 2 (2026-10-27):** eliminacja surowych odwołań bajtowych i ostatnich uchwytów tabel. W `Pathfinding.ComputeRouteCost` zastąpiono `EmitGlobalByte(0x87c9..0x87cb)` przez `this.troops.Assign(0, 127, ...)` z `UnitMemory` (czyszcząc ostatnie bezpośrednie dostępy do pamięci jednostek). W `GameGlobals` i `GameModule` dodano zmienne: `SpaceshipStarFieldFrame` (0x687a), `FallenLeadersCount` (0xb1d4) i `GameEndFlags` (0x4cd9) i zaktualizowano moduły `SpaceshipScreen`, `GameReplay` i `TurnEvents`. W `CityView` wprowadzono akcesor `CityStructurePicture(int)` i `CitySceneryHandle`, eliminując surowe adresy 0x81c4, 0x81ce, 0x81ae, 0x81cc, 0x81b8, 0x817a. 2/2 mutacji złapanych (GoldenMaster `PlayedTurnsMatchBaseline`), Golden Master 15/15 zielony.

**B6.4 część 3 (2026-10-27):** eliminacja surowych adresów w modułach UI, zapisu i inicjalizacji: `Palace.cs` (`PalaceGardenBackground`), `CityView.cs` (akcesor `CitySceneryPiecePicture`), `Civilopedia.cs` (`CivilopediaFileName`, `CivilopediaFileNumberOffset`), `GameLoadAndSave.cs` (`SaveFileNameAddress`, `SaveSlotDigitAddress`), `MeetWithKing.cs` (`KingEyesHueAddress`, `KingGestureHue1Address`, `KingGestureHue2Address`), `GameStartup.cs` (`VideoDriverOptionAddress`, `SoundDriverOptionAddress`, `DiskNumberPromptAddress`), `GameInitAndIntro.cs` (`SeaBlocksAddress`). 2/2 mutacji złapanych (`ReckonContinentSizesMatchesOriginalCode`, `ChoreographKingSpeakingMatchesOriginalCode`), Golden Master 15/15 zielony.

**B6.4 część 4 (2026-10-27):** eliminacja surowych odwołań w `NewCityScreen.cs` oraz `SpaceshipScreen.cs`. W `NewCityScreen.cs`: odwołania do bufora tekstu i nazw cywilizacji (`0x1982`, `0x1992`, `0x19a2`) zastąpiono metodami `LeaderName`, `CivilizationName`, `CivilizationPlural` oraz indekserem `Words[...]`. W `SpaceshipScreen.cs`: wprowadzenie nazwanych stałych i akcesorów dla animacji gwiazd (`StarColumnsAddress`, `StarRowsAddress`, `StarDrawnAddress`), tablic statku (`SpaceshipPartPictureSpecsAddress`, `SpaceshipPartMassTableAddress`, `SpaceshipPartCountsAddress`, `SpaceshipCompleteMasksAddress`), użycie `BuildingWord(23 + arg2, BuildingTable.Technology)` zamiast `0xe68`, `PlayerColour(participantIdx)` zamiast `0x1946` oraz `Words[length - 7]` zamiast `0xb9ff`. 2/2 mutacji złapanych (`RenderStarFieldMatchesOriginalCode`, `CivilizationNameQuestionMatchesOriginalCode`), Golden Master 15/15 zielony.

**B6.4 część 5 (2026-10-27):** eliminacja surowych odwołań w `TextBlockRenderer.cs` oraz `StartGameMenu.cs`. W `TextBlockRenderer.cs`: formatowanie wierszy `ArrangeTextRow`, podział na fragmenty `RenderTextChunk` i podstawianie makr `ReflowText` działają na indekserze `Words[...]` zamiast odwołań `PullGlobalByte`/`EmitGlobalByte` na `TextBuffer.Address`; nazwane stałe `MacroCodesTableAddress` (`0x30ae`) i `MacroReplacementsTableAddress` (`0x30b8`); uproszczone wywołania w `ShrinkTextToWidth`. W `GameModule.cs`: dodano akcesor `PlayerSecondaryColourAt(offset)` (`0x1956`). W `StartGameMenu.cs`: stałe `SoundDriverOptionAddress` (`0x1a30`), `PalaceSectionLevelsAddress` (`0xd91e`), `PalaceSectionStylesAddress` (`0xb222`), `ParticipantCountersAddress` (`0xd20c`), `LeaderEarthStartPositionTableAddress` (`0x35da`), `VgaPaletteBufferAddress` (`0x6b34`), `ImportedPaletteBufferAddress` (`0xbdf4`), `VgaUnitSpritesTableAddress` (`0xb886`), `AdvisorTitleTextTableAddress` (`0x2fac`) oraz użycie `PlayerColourAt` i `PlayerSecondaryColourAt`. 2/2 mutacji złapanych (`ReflowTextMatchesOriginalCode`, `ConstructCivilizationMatchesOriginalCode`), Golden Master 15/15 zielony.

**B6.4 część 6 (2026-10-27):** kompleksowa eliminacja surowych odwołań i adresów w module pałacu (`Palace.cs`), zapisie gry (`GameLoadAndSave.cs`), zdarzeniach tury (`TurnEvents.cs`) i menu startowym (`StartGameMenu.cs`). W `Palace.cs`: nazwane stałe adresowe dialogów, zasobów i tablic wysokości/obrazów (`PalaceUpgradeAnnouncementDialogAddress`, `PalaceInitialScreenPictureAddress`, `PalaceUpgradeOptionsPromptAddress`, `PalaceStyleChoiceHeaderAddress`, `PalaceCastleResourceAddress`, `PalacePartHeightTableAddress` i tablice 1..5, `PalaceScreenBackgroundPictureAddress`, `PalaceDefaultBackgroundPictureAddress`, `PalaceGraphicsResourceAddress`, `PalacePaletteAddress`, `PalaceSectionStylesAddress`, `PalacePicturesTableAddress`, `PalaceSectionStyleMarkersAddress`, `PalaceSectionLevelsAddress`, `PalaceLeftBushesAddress`, `PalaceGardenLevelsAddress`, `PalaceRightBushesAddress`); metody dostępowe `PalaceSectionLevel`, `SetPalaceSectionLevel`, `PalaceGardenLevel`, `PalaceSectionStyle`, `SetPalaceSectionStyle`, `PalaceSectionStyleMarker`, `SetPalaceSectionStyleMarker`, `SetPalaceSectionStyleMarkers`, `PalacePictureAddress`, `PalacePicture`, `SetPalacePicture`; czytelna implementacja `LeftNeighbourHigher(sectionIdx)`. W `GameLoadAndSave.cs` i `TurnEvents.cs` użyto stałych `Palace.PalaceSectionLevelsAddress` i `Palace.PalaceSectionStylesAddress`. 2/2 mutacje złapane (`RenderPalaceSectionMatchesOriginalCode`, `ImportPalaceCastleMatchesOriginalCode`), Golden Master 15/15 zielony.

**B6.4 część 7 (2026-10-27):** eliminacja surowych odwołań i adresów w `HallOfFame.cs`, `Newspaper.cs`, `ArmySelection.cs` oraz `AIPlayer.cs`. W `HallOfFame.cs`: stałe segmentu, bazy, rekordu i pól Hall of Fame (`HallOfFameSegment`, `HallOfFameBaseAddress`, `HallOfFameRecordCount`, `HallOfFameRecordSize`, `HallOfFameTotalSize`, `OffsetScore`..`OffsetCivilizationPlural`), akcesory `HallOfFameRecordAddress`, `FameScore`, `SetFameScore`, `FameDifficulty`, `FameYear`, `FamePopulation`, nazwane stałe napisów, er i tytułów (`HallOfFameTitleStringAddress`, `HallOfFameSubtitleStringAddress`, `HallOfFameClearPromptStringAddress`, `HallOfFameRulerTitlesTableAddress`, `HallOfFameBcEraStringAddress`, `HallOfFameAdEraStringAddress`, `DifficultyNameAddresses`, `RulerRankTitlesSegment`, `TopCitiesTitleStringAddress`). W `Newspaper.cs`: nazwane stałe nazw gazet (`NewspaperNameSuffixTimesAddress`..`ChronicleAddress`), cen, mott, pliku tekstu i menu dialogowego (`0x5118`..`0x5181`). W `ArmySelection.cs` i `AIPlayer.cs`: stałe strategicznych lokacji graczy (`StrategicLocationRoleTableAddress`, `StrategicLocationHorizTableAddress`, `StrategicLocationVertTableAddress`, `StrategicLocationPriorityTableAddress`) oraz ról taktycznych (`TacticalSiteRoleFortAddress`..`TacticalSiteRolePassAddress`). 2/2 mutacje złapane (`AttachHallOfFameRecordMatchesOriginalCode`, `DisplayNewspaperMatchesOriginalCode`), Golden Master 15/15 zielony.

**B6.4 część 8 (2026-10-27):** eliminacja surowych odwołań i tabel w modułach pomocy, informacji, cywilopedii i zarządzania technologiami/jednostkami: `InGameHelp.cs`, `CivilizationInfo.cs`, `Civilopedia.cs`, `RecordTables.cs`, `ScienceAndAdvisors.cs`, `UnitManagement.cs`. W `RecordTables.cs`: stałe pól tabeli terenu `TerrainTable` (`DefenseBonus`, `Food`, `Shields`, `Trade`), tabela ulepszeń terenu `TerrainImprovementTable` (`Origin`, `Stride`, `Irrigation`, `Mining`) oraz tabela technologii `TechnologyTable` (`Prerequisite1Address`, `Prerequisite2Address`, `Stride`). W `InGameHelp.cs`: stałe plików pomocy (`HelpFileNameAddress`..`HelpFileName3Address`), tematów porad (`HelpCitySiteTopicAddress`, `HelpIrrigateTopicAddress`, `HelpIrrigate2TopicAddress`, `HelpMineTopicAddress`) oraz nagłówków (`HelpAdviceHeaderAddress`, `HelpAdviceHeader2Address`). W `CivilizationInfo.cs`: stałe nagłówków `CivilizationOverviewSummaryHeaderAddress` i `CivilizationOverviewTechnologiesHeaderAddress`. W `Civilopedia.cs`: stałe `CivilopediaConceptTitlesTableAddress`, `CivilopediaNoneStringAddress`, `CivilopediaAllowsStringAddress` oraz wykorzystanie stałych z `TechnologyTable` i `TerrainImprovementTable`/`TerrainTable`. 2/2 mutacje złapane (`DisplayCivilizationOverviewMatchesOriginalCode`, `DisplayHelpPopupMatchesOriginalCode`), Golden Master 15/15 zielony.

**B6.4 część 9 (2026-10-27):** eliminacja surowych adresów i tabel w widoku miasta, zdarzeniach tury, doradcach, oknach dialogowych i intro: `CityView.cs`, `TurnEvents.cs`, `ScienceAndAdvisors.cs`, `DialogDrawing.cs`, `GameStartup.cs`, `MainIntro.cs`. W `CityView.cs`: stałe pól i wymiarów prostokątów wycinków scenerii (`SceneryBoxStride`, `SceneryBoxLeft`, `SceneryBoxTop`, `SceneryBoxRight`, `SceneryBoxBottom`, `SceneryBoxWidth`, `SceneryBoxHeight`) oraz akcesory `SceneryBoxField` i `SetSceneryBoxField`; stałe obrazów parady `ParadeAutomobilePictureAddress`, `ParadeTrainPictureAddress`, `ParadeWalkPictureAddress`. W `TurnEvents.cs`: użycie `ScienceLeadingFoundVia[34]` zamiast bezpośredniego odczytu `0x3772:0x4f4`; użycie `BuildingWord(24 + wonderIdx, BuildingTable.Price/Technology)` i `BuildingRecordText(24 + wonderIdx)` w `TryRivalWonder` zamiast wyliczeń na `0xe6a`/`0xe82`/`0xe86`; nazwane stałe `TravellersReportGunpowderAddress`, `TravellersReportIronWorkingAddress`, `TravellersReportAncientAddress`, `MenuBarGameLabelAddress`..`MenuBarCivilopediaLabelAddress`, `BankruptcyWarningDialogAddress`, `RetirementContinuePromptAddress`, `EndGameTitlePictureAddress`, `EndGameTitlePaletteAddress`, `EraBcStringAddress`, `EraAdStringAddress`, `AdvisorPaletteResourceAddress`, `PalaceSectionSpritesTableAddress`. W `ScienceAndAdvisors.cs`: użycie `ScienceLeadingFoundVia[56]` zamiast `0x3772:0x520`. W `DialogDrawing.cs`: stałe `MenuItemHotkeysTableAddress`, `AdvisorTitlesTableAddress`, `AdvisorPortraitsTableAddress`, `MenuLineOffsetsTableAddress`. W `MainIntro.cs` i `GameStartup.cs`: `GameStartup.VideoDriverOptionAddress`. 2/2 mutacje złapane (`CityViewAndReplayNamesMatchOriginalCode`, `DisplayTravellersReportMatchesOriginalCode`), Golden Master 15/15 zielony.

**B6.4 część 10 (2026-10-27):** eliminacja surowych odwołań, adresów sprzętowych i tabel w `CityView.cs`, `SystemServices.cs`, `GameInitAndIntro.cs`, `GameEngine.cs`, `GameMessage.cs`. W `GameMessage.cs`: zastąpienie `EmitGlobalWord(0x2f9e, style)` przez `this.game.Globals.DialogStyle.Value = style;`. W `GameEngine.cs`: nazwane stałe procedur startowych i bloków DOS (`StartupExecutablePathAddress`, `StartupDriveFlagAddress`, `StartupStackLimitAddress`, `StartupStackBaseAddress`, `StartupFreeMemoryAddress`, `StartupDataSegmentAddress`, `DosVersionAddress`, `DosDriveInfoAddress`, `DosArgumentBlockAddress`, `DosEnvironmentBlockAddress`). W `GameInitAndIntro.cs`: stała `SeaBlocksAlternateAddress` oraz czytelne indeksowanie wiersza 11 (`SeaBlocksAddress + 11`, `SeaBlocksAlternateAddress + 11`) zamiast surowych 0x7f43 i 0x803a. W `CityView.cs`: nazwane stałe tabel i obrazów scenerii/cudów/budynków (`WonderPicturesTableAddress`, `CityWallPicturesTableAddress`, `CityStructurePicturesTableAddress`, `CitySceneryPiecesTableAddress`, `CitySceneryPiecesPictureAddress`, `CityBuildingsModernPictureAddress`, `CityBuildingsAncientPictureAddress`, `CityWondersPictureAddress`, `CityWondersInMaskPictureAddress`) oraz metody dostępowe `WonderPicture`, `SetWonderPicture`, `CityWallPicture`, `SetCityWallPicture`, `SetCityStructurePicture`. W `SystemServices.cs`: nazwane stałe rejestrów/liczników timera dźwięku i zegara (`TimerTicksLowAddress`..`TimerSkipCounterAddress`), miksera palety/przejść kolorów (`HueMixerActiveAddress`..`HueMixerStepRateAddress`) oraz wskaźnika aktywnego prostokąta (`ActiveRectanglePointerAddress`), zastępujące surowe adresy w funkcjach timera, przerwań zegara i miksera kolorów. 2/2 mutacje złapane (`ReckonContinentSizesMatchesOriginalCode`, `RenderCityBuildingsMatchesOriginalCode`), Golden Master 15/15 zielony.

**B6.4 część 11 (2026-10-27):** eliminacja surowych adresów, tabel i segmentów w `HallOfFame.cs`, `MeetWithKing.cs`, `Civilopedia.cs`, `GameReplay.cs`, `UnitManagement.cs`, `MapManagement.cs`, `CityView.cs`, `GameInitAndIntro.cs`, `WorldMap.cs`, `TurnEvents.cs`. W `MeetWithKing.cs`: użycie `LeaderWord(..., LeaderTable.ShortTune/Tune)` zamiast surowych `0x1518`/`0x151a` i metody `LeaderRecord`; nazwane stałe segmentów i tabel `LeaderFontTableSegment` (0x36fa), `AdvisorKingVisualsSegment` (0x36fa), `AdvisorHorizOffsetsTableAddress` (0x3afe), `AdvisorSpritesTableAddress` (0x804e), `KingEyesPicturesTableAddress` (0x7efe). W `Civilopedia.cs`: nazwane stałe segmentu i tabel wyboru (`CivilopediaPageHistorySegment` 0x3772, `CivilopediaChosenIndexTableAddress` 0x1c80, `CivilopediaChosenCategoryTableAddress` 0x36fc) oraz nagłówków i palet kategorii (`CivilopediaSciencePaletteAddress`, `CivilopediaScienceCategoryHeaderAddress`, `CivilopediaBuildingsPictureAddress`, `CivilopediaBuildingPaletteAddress`, `CivilopediaBuildingCategoryHeaderAddress`, `CivilopediaWonderCategoryHeaderAddress`, `CivilopediaUnitPaletteAddress`, `CivilopediaUnitsCategoryHeaderAddress`, `CivilopediaUnitIconsTableAddress`, `CivilopediaTerrainPaletteAddress`, `TerrainPaletteBufferAddress`, `CivilopediaTerrainCategoryHeaderAddress`, `CivilopediaConceptsCategoryHeaderAddress`). W `GameReplay.cs`: stałe kolumn tabeli miast powtórki `ReplayCityActiveColumnOffset`, `ReplayCityXColumnOffset`, `ReplayCityYColumnOffset`, stałe tablicy poległych liderów `ReplayFallenLeadersSegment`, `ReplayFallenLeadersTableAddress` oraz segmentu twarzy `ReplayLeaderFaceSegment`. W `UnitManagement.cs` i `MapManagement.cs`: zastąpienie surowych wyliczeń `participantIdx * 0x600 + troopIdx * 0xc` metodami `this.Troops.Pull` / `PullSigned`; użycie `UnitKindWord` ze stałymi `UnitKindTable.LandGroup` i `UnitKindTable.GuardPower` oraz `TerrainTable.DefenseBonus`. W `CityView.cs`: zastąpienie bezpośrednich odczytów segmentu 0x3767 w `RenderCityMarvels` akcesorem `SceneryBoxField` ze stałymi `SceneryBoxLeft` i `SceneryBoxTop`. W `GameInitAndIntro.cs`: nazwane stałe `StorySegment` (0x3710) oraz `StoryTimingTableOffset` (0x83) w pętli odtwarzania historii stworzenia świata. W `WorldMap.cs` i `TurnEvents.cs`: nazwany segment `ReplayHistoryScoresSegment` (0x3772) i stała kroku `ReplayHistoryStride` (8); stałe napisów cudów `WondersOfTheWorldTitleAddress`, `WonderInCityStringAddress`, `WonderOfCityStringAddress`. W `HallOfFame.cs`: użycie akcesorów `FameScore`, `FameDifficulty`, `FameYear`, `FamePopulation`, stałych `RulerRankEntryStride` (0x19) oraz `HallOfFameRecordAddress` i `Offset...` w `RenderFameRecord`, `AttachHallOfFameRecord` i `DisplayCivilizationRating`. 2/2 mutacje złapane (`DisplayCivilopediaRecordMatchesOriginalCode`, `AttachHallOfFameRecordMatchesOriginalCode`), Golden Master 15/15 zielony.

**B6.4 część 12 (2026-10-27):** eliminacja surowych adresów, offsetów i segmentów pamięci w `SpaceshipScreen.cs`. Wprowadzono nazwane stałe segmentów i geometrii siatki statku kosmicznego `SpaceshipLayoutSegment` (0x375a), `SpaceshipGridSegment` (0x3772), `SpaceshipGridBaseAddress` (0x16e0), `SpaceshipGridParticipantStride` (0xb4), `SpaceshipGridRowStride` (12), `SpaceshipSlotStride` (3), `SpaceshipSlotColumnOffset`, `SpaceshipSlotRowOffset`, `SpaceshipSlotKindOffset`, znaczniki `SpaceshipSlotEndMarker` i `SpaceshipSlotEmptyMarker` (0xff). Dodano metody dostępowe `LayoutSlotColumn`, `LayoutSlotRow`, `LayoutSlotKind`, `LayoutSlotMarker`, `SpaceshipGridCellAddress`, ujednolicając adresację siatki w `CountSpaceshipParts`, `ObtainSpaceshipPartValue`, `ComputeSpaceshipSuccess`, `BuildSpaceshipPart`, `AttachSpaceshipPart`, `SpaceshipSlotFree`, `FindSpaceshipSlot` i `ChooseSpaceshipStructureSlot`. Wprowadzono nazwane stałe zasobów graficznych, palet, buforów oraz okien dialogowych (`SpaceshipBackgroundPictureAddress`, `SpaceshipResourceBlockAddress`, `SpaceshipPaletteAddress`, `PaletteBufferAddress1`, `PaletteBufferAddress2`, `PaletteCopySourceAddress`, `PaletteCopyTargetAddress`, `SpaceshipPopulationHeaderAddress`, `SpaceshipMassHeaderAddress`, `SpaceshipFlightTimeHeaderAddress`, `SpaceshipSuccessRateHeaderAddress`, `ArrivalPassedStringAddress`, `ArrivalExpectedStringAddress`, `SpaceshipLaunchInstructionsStringAddress`, `SpaceshipComponentChoiceDialogAddress`, `SpaceshipModuleChoiceDialogAddress`). 2/2 mutacje złapane (`RenderSpaceshipMatchesOriginalCode`), Golden Master 15/15 zielony.

**B6.4 część 13 (2026-10-27):** eliminacja surowych adresów chunków, nagłówków i buforów stanu gry w `GameLoadAndSave.cs`. Wprowadzono nazwane stałe segmentów, tabel i wielkości buforów: `ExploredMapSegment` (0xdcfe), `ExploredMapSize` (0x100), `CivilizationHistorySegment` (0x3772), `CivilizationHistoryBaseAddress` (0x0), `CivilizationHistorySize` (0x4b0), `CityNamesStatusAddress` (0x112a), `CityNamesStatusSize` (0x3b8), `UnitsBufferSize` (0x3000) z użyciem `UnitMemory.Origin` (0x81d4), `ReplayTurnsAddress` (0xdb44), `ReplayTurnsSize` (0x104), `SpaceshipGridSegment` (0x3772), `SpaceshipGridBaseAddress` (0x16e0), `SpaceshipGridTotalSize` (0x5a0) oraz `PalaceSectionRecordSize` (0x18), eliminując surowe liczby z `PullContent` i `EmitContent`. Zastąpiono surowe adresy odczytu nagłówka stanu gry (`0x6822`, `0x6824`, `0x6826`) właściwościami `this.owner.Globals.SavedHumanPlayer.Address`, `SavedDifficulty.Address`, `SavedYear.Address`, dodano nazwane stałe `DifficultyNamesTableAddress` (0x33a2), `SavedGameDriveLetterAddress` (0x4384) oraz `SaveFileNamePatternAddress` (0x41c8). 2/2 mutacje złapane (`GoldenMasterAuto09.PlayedTurnsMatchBaseline`), Golden Master 15/15 zielony.

**Faza 3 część 1 (2026-10-27):** uniezależnienie testów różnicowych od wskaźnika stosu `cpu.sp` (`DifferentialPair.cs` filtruje `cpu.sp`) oraz eliminacja 5 ramek stosu emulacji (`EnterFrame` / `LeaveFrame`): `VGADriver.DuplicateScreenToMainScreen` (ramka VGA), `CivilizationSchism.DivideCivilization` (ramka 0x48), `GameReplay.DisplayConquerorScreen` (ramka 0x2), `MeetWithKing.LaunchAudience` (ramka 0x36), `GameInitAndIntro.OpeningSequence` (ramka 0xe). Usunięto niepotrzebne zachowywanie rejestrów indeksowych (`PreserveIndexRegisters` / `RestoreIndexRegisters`) w odchudzonych procedurach. 2/2 mutacje złapane (`DivideCivilizationMatchesOriginalCode`, `LaunchAudienceMatchesOriginalCode`), testy różnicowe (17/17) oraz Golden Master (15/15) zielone.

**Faza 3 część 2 (2026-10-27):** oczyszczenie pozostałości emulacji x86 (callee-saved rejestrów indeksowych `PreserveIndexRegisters()`, `RestoreIndexRegisters()`, `PreserveSource()`, `RestoreSource()`, `PreserveDest()`, `RestoreDest()`) w 17 plikach logiki gry i UI: `CityFoundingAndScore.cs`, `GovernmentAndDiplomacy.cs`, `MapManagement.cs`, `Pathfinding.cs`, `TradeAndConquest.cs`, `AdvisorReports.cs`, `MainIntro.cs`, `MeetWithKing.cs`, `Newspaper.cs`, `Palace.cs`, `SpaceshipScreen.cs`, `StartGameMenu.cs`, `UnitCombat.cs`, `UnitManagement.cs`, `ScienceAndAdvisors.cs`, `TurnEvents.cs`, `DiplomatActions.cs`. 2/2 mutacje złapane (`SeizeCityMatchesOriginalCode`, `DiplomatArrivesAtCityMatchesOriginalCode`), testy różnicowe (19/19) oraz Golden Master (15/15) w pełni zielone (0 błędów, 0 ostrzeżeń w `Civilization1Remaster.Core`).

**Faza 3 część 3 (2026-10-27):** eliminacja sztucznych operacji stosu `call far` (`StackWord(0)`, `StackWord(offset)` oraz natychmiastowych `UnstackDoubleWord()`) i niepotrzebnych instrukcji callee-saved w sterowniku wideo i logice gry: `CivilizationSchism.cs`, `GovernmentAndDiplomacy.cs` (usunięcie zbędnego parametru `returnOffset` z `AskKing`), `ScienceAndAdvisors.cs`, `TradeAndConquest.cs` (3 miejsca wywołań dialogów króla i stolicy), `TurnEvents.cs` (wywołanie `ObtainFreeStorage`), `GameStartup.cs` (wywołania `ObtainFreeStorage` i `ImportIcon`), `VGADriver.cs` (usunięcie callee-saved w `DuplicateScreenToMainScreen` i `FloodBuffer`). 2/2 mutacje złapane (`DivideCivilizationMatchesOriginalCode`, `SeizeCityMatchesOriginalCode`), powiązane testy różnicowe (12/12) oraz Golden Master (15/15) w pełni zielone; 0 błędów i 0 ostrzeżeń w `Civilization1Remaster.Core`.

**Faza 3 część 4 (2026-10-27):** eliminacja operacji stosu emulacji w usługach systemowych i sterowniku wideo: `SystemServices.ClockInterrupt` (zastąpienie push/pop starego SS/SP na stosie przerwania zmiennymi lokalnymi C# `oldSS`/`oldSP`), `SystemServices.RefreshHueCycling` (eliminacja push/pop rejestrów na stosie emulacji, rejestry przywracane ze zmiennych lokalnych C# z zachowaniem kontraktu `Register.Kept`), `VGADriver.FloodBuffer` i `RenderPictureToDisplay` (eliminacja odkładania struktury prostokąta na stosie emulacji x86 i bezpośrednie renderowanie do ekranu bufora). 2/2 mutacje złapane (`RefreshHueCyclingMatchesOriginalCode`, `ClockInterruptMatchesOriginalCode`), powiązane testy różnicowe (4/4) oraz Golden Master (15/15) w pełni zielone; 0 błędów i 0 ostrzeżeń w `Civilization1Remaster.Core`.

**Faza 3 część 5 (2026-10-27):** eliminacja pozostałości rejestrów x86 (`RegSource` / `RegDest`) w 8 modułach silnika gry i UI: `TradeAndConquest.cs` (eliminacja 24 odwołań, użycie `DirectionHoriz`/`DirectionVert` oraz `CivilizationPlural`), `CivilizationSchism.cs` (eliminacja 28 odwołań w pętlach miast, kontynentów i transferu jednostek), `CityFoundingAndScore.cs` (eliminacja 24 odwołań w `ConstructCity`, `MarkCityNeighbourhood`, `MarkCityPowerArea`, `HandleCityDisasters`, `HandleCityRevolt`, `ReportCityDisaster` i `DisplayFinalScore`), `GovernmentAndDiplomacy.cs` (eliminacja 18 odwołań w `HandlePeaceTreaties`, `EndOldTreaties`, `SendWarEmissary`, `SwitchGovernment`, `BargainPeaceTreaty`), `TurnEvents.cs` (eliminacja 2 martwych przypisań w `ScoreWondersAndPollution`), `MainIntro.cs` (eliminacja 1 martwego przypisania w tutorialu), `Newspaper.cs` (eliminacja martwego przypisania w nagłówku gazety) oraz `Palace.cs` (zastąpienie odczytów `RegSource`/`RegDest` bezpośrednimi stałymi). Wszystkie moduły osiągnęły stan `RegSource: 0, RegDest: 0`. 2/2 mutacje złapane (`SeizeCityMatchesOriginalCode`, `EstablishCityMatchesOriginalCode`), powiązane testy różnicowe (19/19) oraz Golden Master (15/15) w pełni zielone; 0 błędów i 0 ostrzeżeń w `Civilization1Remaster.Core` i `Civilization1Remaster.Desktop`.

**Faza 3 część 6 (2026-10-27):** eliminacja pozostałości rejestrów x86 (`RegSource` / `RegDest`) w kolejnych 8 modułach silnika gry i UI: `UnitCombat.cs` (usunięcie martwego przypisania nagłówka walki), `GameEngine.cs` (usunięcie martwego przypisania w procedurze startowej), `GameMenus.cs` (usunięcie callee-saved w `DisplayOrdersMenu`), `MeetWithKing.cs` (usunięcie martwych przypisań oraz zastąpienie odczytów bezpośrednimi wywołaniami `CivilizationPlural`), `StartGameMenu.cs` (usunięcie 11 martwych przypisań w wyborze cywilizacji i rewolucji), `DiplomatActions.cs` (usunięcie 11 martwych przypisań w akcjach dyplomatów), `ScienceAndAdvisors.cs` (wprowadzenie metody `WonderObsoleteTechnology` i stałej `WonderObsoleteTable.Origin` = 0x10fe, usunięcie 12 martwych przypisań oraz eliminacja odczytu `PullGlobalWord(RegSource + 0x10fe)`), `Pathfinding.cs` (usunięcie 14 martwych przypisań w `StepToNearGoal`, `StepTowardsGoal`, `ChooseGotoStep`, `MayGotoTile`, `ShiftToward`, `ChartWindowCosts`). Wszystkie 8 modułów osiągnęły stan `RegSource: 0, RegDest: 0`. 2/2 mutacje złapane (`EarnTechnologyMatchesOriginalCode`, `CarryOutGotoMatchesOriginalCode`), powiązane testy różnicowe i Golden Master (15/15) w pełni zielone; 0 błędów i 0 ostrzeżeń w `Civilization1Remaster.Core` i `Civilization1Remaster.Desktop`.

**Faza 3 część 7 (2026-10-27):** eliminacja pozostałości rejestrów x86 (`RegSource` / `RegDest`) w 3 modułach generowania świata, usług systemowych i zarządzania jednostkami: `GameInitAndIntro.cs` (usunięcie 15 martwych przypisań w `ProduceTerrain`, `LabelLandOrSea`, `JoinRowAbove` i `SortWorldLabels`, osiągnięto stan 0 odwołań), `SystemServices.cs` (usunięcie 14 odwołań: w `RefreshHueCycling` zastąpienie rejestrów `RegDest`/`RegSource` lokalnymi zmiennymi C# `cycleOffset` i `stagingLoc` oraz eliminacja zbędnego zachowywania/przywracania rejestrów; w `ClockInterrupt` usunięcie zbędnego zachowywania `RegSource`/`RegDest` jako nieużywanych przez przerwanie; osiągnięto stan 0 odwołań), `UnitManagement.cs` (usunięcie 44 instrukcji callee-saved `savedSource`/`savedDest` w 11 metodach: `ConstructUnitsFromCityList`, `KeepUnitsInCityList`, `ChoreographUnitMove`, `ConstructUnit`, `EliminateUnit`, `LocateStackLeader`, `LocateBestGuardianInStack`, `RestoreUnitStatus`, `VerifyTileOccupation`, `VisitHut`, `ChoreographUnitCombat`, `DisplayBattleScreen`, `LocateNearestFriendlyCity`; pozostawiono jedynie 1 celowe przypisanie w `VisitEachUnitInStack`, od którego zależy błąd `ClassicStackStrength` w baseline). 2/2 mutacje złapane (`RefreshHueCyclingMatchesOriginalCode`, `LocateNearestFriendlyCityMatchesOriginalCode`), powiązane testy różnicowe i Golden Master (15/15) w pełni zielone; 0 błędów i 0 ostrzeżeń w `Civilization1Remaster.Core` i `Civilization1Remaster.Desktop`.

**Faza 3 część 8 (2026-10-27):** gruntowna eliminacja rejestrów x86 (`RegSource` / `RegDest`) w głównych modułach sztucznej inteligencji, zarządzania miastami i tury gracza: `AIPlayer.cs` (usunięcie wszystkich 34 odwołań, osiągnięto czysty stan 0 rejestrów x86), `CityManagement.cs` (usunięcie wszystkich 85 odwołań, zastąpienie odczytu w `PrepareCity` zmienną lokalną `int frameOffset`, osiągnięto czysty stan 0 rejestrów x86), `PlayerTurn.cs` (eliminacja ponad 140 wystąpień `RegSource` i `RegDest`, redukcja do zaledwie 14 wyizolowanych przypisań stanowiących udokumentowane mostki zgodności ze stanem rejestrów x86 z DOS CIV.EXE w `PillageTile`, `LocateMoveTarget`, `DecideTileAction`, `MoveStepAction` i `PlaneMovesByTurn`). 2/2 mutacje złapane (`PrepareCity` w `CityManagement.cs` -> 15/15 fail, `FreeStrategicLocation` w `AIPlayer.cs` -> 15/15 fail); Golden Master (15/15) w pełni zielony, 0 błędów i 0 ostrzeżeń w `Civilization1Remaster.Core` i `Civilization1Remaster.Desktop`.

**Faza 3 część 9 (2026-10-27):** eliminacja pozostałości rejestrów x86 (`RegBase`, `RegCount`, `RegAcc`, `RegAux`) w 11 modułach logiki, UI i silnika: `TextDrawing.cs` (usunięcie martwego `RegAcc`), `TextBuffer.cs` (usunięcie martwego `RegAcc` w `Length` i `Write`), `Newspaper.cs` (usunięcie martwego `RegAcc`), `Palace.cs` (odebranie `picHandle` bezpośrednio z `ScreenToPicture` zamiast odczytu `RegAcc`), `NewCityScreen.cs` (usunięcie martwych `RegAcc`), `StartGameMenu.cs` (usunięcie martwych `RegAcc`), `Civilopedia.cs` (usunięcie martwych `RegBase` i `RegAcc`), `CityView.cs` (usunięcie `RegBase` i odczytu `RegAcc` w `SetWonderPicture`), `GameEngine.cs` (usunięcie martwych `RegBase` i `RegAcc` w resize DOS oraz `TerminateProgram`), `MeetWithKing.cs` (usunięcie martwych `RegBase` i `RegAcc`), `AIPlayer.cs` (usunięcie wszystkich pozostałych 32 odwołań do `RegCount`, `RegBase`, `RegAcc` oraz instrukcji callee-saved w `ShiftAIUnit` i `ManageBarbarianUnit`). Aż 10 z 11 modułów osiągnęło stan 0 jakichkolwiek rejestrów x86 (`AIPlayer.cs` w 100% wolny od rejestrów x86). 2/2 mutacje złapane (`AIPlayer.CountUnit` -> 15/15 fail, `TextBuffer.Length` -> 15/15 fail); Golden Master (15/15) w pełni zielony, 0 błędów i 0 ostrzeżeń w `Civilization1Remaster.Core` i `Civilization1Remaster.Desktop`.

**Faza 3 część 10 (2026-10-27):** całkowita eliminacja rejestrów x86 w kolejnych 7 modułach logiki, UI i rozgrywki: `HallOfFame.cs` (odebranie klawisza bezpośrednio z `AwaitForKeyPressOrMouseClick` zamiast odczytu `RegAcc`, usunięcie martwych przypisań `RegAcc`), `SpaceshipScreen.cs` (usunięcie martwych przypisań `RegAcc` w `RenderSpaceship`, `ObtainSpaceshipPartValue` i `ComputeSpaceshipSuccess`), `TurnEvents.cs` (usunięcie martwych przypisań `RegAcc` w `DisplayDialog` i `ObtainPalaceScoreForLevel`, zastąpienie odczytu rejestru x86 bezpośrednim warunkiem `this.owner.World.SmogImpactTier == 12`), `UnitCombat.cs` (usunięcie martwych `RegAcc`/`RegData` w `SettleBattle`, odebranie wyniku dialogu traktatu bez odczytu `RegAcc`, usunięcie martwych przypisań w `FinishSettleBattle` i `AffirmBreakTreaty`), `TradeAndConquest.cs` (usunięcie martwych przypisań `RegAcc` w `ComputeCityPlunder`, `ChooseTechnologyToTake` i `FoundTradeRoute`, `AwaitForKeyPressOrMouseClick` zwraca bezpośrednio `short key`), `CityFoundingAndScore.cs` (usunięcie martwych przypisań `RegAcc` w `EstablishCity`, `DisplayFinalScore`), `GovernmentAndDiplomacy.cs` (usunięcie martwych przypisań `RegAcc` w `AIWeighsTreaty`, `SwapTechnology` i `LocateNearestCityOfPlayer`). Wszystkie 7 modułów osiągnęło stan 0 jakichkolwiek rejestrów x86. 2/2 mutacje złapane (`TradeAndConquest:ComputeCityPlunder` -> 8/15 fail, `TurnEvents:HandleEndOfTurnEvents` -> 15/15 fail); Golden Master (15/15) w pełni zielony, 0 błędów i 0 ostrzeżeń w `Civilization1Remaster.Core` i `Civilization1Remaster.Desktop`.

**Faza 3 część 11 (Uderzenie 1/6, 2026-10-27):** eliminacja rejestrów x86 w modułach danych, intro i tekstu: `GameLoadAndSave.cs`, `GameReplay.cs`, `TextBlockRenderer.cs` oraz `GameInitAndIntro.cs`. Wdrożono mechanizm obronny `run_tests_with_timeout.ps1` (twardy limit czasu runnera zapobiegający zawieszaniu procesów testowych). W `GameLoadAndSave.cs`: usunięcie wszystkich 6 martwych przypisań `RegAcc` w `ImportGameFromMenu`, `ImportGame`, `StoreGame`, `QueryForStoreDrive` i `VerifyDrive` (moduł osiągnął Total=0 rejestrów x86). W `GameReplay.cs`: zastąpienie rejestru `RegAcc` w `ObtainReplayYear` czystą zmienną lokalną C# `replayYear` oraz usunięcie martwego przypisania w `ObtainDistanceToReplayCity` (moduł osiągnął Total=0 rejestrów x86). W `GameInitAndIntro.cs`: usunięcie martwego `RegBase` w `OpeningSequence`, martwego `RegAcc` w `BlendTerrainTile` (z refaktoryzacją warunku if na idiomatyczny C#), nieużywanej zmiennej `RegFrame` w `RankLandOrSea`, martwego `RegAcc` w `ObtainTerrainTypeFromScreen` i `DisplayStoryIntro` oraz odebranie wartości bezpośrednio z `ObtainContinentIDFromScreen` w `MakeSeaBlocks` (moduł osiągnął Total=0 rejestrów x86). W `TextBlockRenderer.cs`: usunięcie martwego `RegAcc` w `RenderTextChunk`, bezpośrednie odebranie pozycji z `TellStream` w `ImportTextFromFile`, czysta zmienna lokalna wyniku w `ImportTextFromFile` oraz bezpośrednie obliczenie przesunięcia z `FindSubtext` w `ReflowText` (eliminacja wszystkich wystąpień `RegAcc`). 2/2 mutacje zweryfikowane (`GameInitAndIntro:ObtainContinentIDFromScreen` oraz `GameLoadAndSave:ImportGame` -> 15/15 fail); Golden Master 15/15 w pełni zielony (29.8s), 0 błędów i 0 ostrzeżeń w `Civilization1Remaster.Core` i `Civilization1Remaster.Desktop`.

**Faza 3 część 12 (Uderzenie 2/6, 2026-10-28):** eliminacja rejestrów x86 w 7 modułach silnika, audio, logiki gry i interfejsu użytkownika: `SoundDriver.cs`, `CivilizationSchism.cs`, `Pathfinding.cs`, `ScienceAndAdvisors.cs`, `MainIntro.cs`, `GameStartup.cs`, `DialogDrawing.cs`. W `SoundDriver.cs`: usunięcie wszystkich odwołań do `RegAcc` (Total=0 rejestrów x86 w module audio). W `CivilizationSchism.cs`: usunięcie wszystkich martwych przypisań `RegAcc` i `RegBase` (Total=0 rejestrów x86). W `Pathfinding.cs`: wprowadzenie zmiennej lokalnej `ushort cost`, usunięcie martwych przypisań `RegAcc` (Total=0 rejestrów x86). W `ScienceAndAdvisors.cs`: usunięcie wszystkich martwych przypisań `RegAcc` (0 rejestrów ogólnych). W `MainIntro.cs`: usunięcie wszystkich martwych `RegAcc` w procedurach wstępu i tutoriala (0 rejestrów ogólnych). W `GameStartup.cs`: bezpieczna refaktoryzacja `requiredStorage` i `(ushort)(PointerShowLevel - 1)`, zachowanie mostków zgodności dla nieprzepisanych modułów. W `DialogDrawing.cs`: usunięcie martwych przypisań `RegCount.LowByte`, `RegBase`, `RegAcc`, wywołanie `DivideWords(rawLine, MenuLineHeight)` bez rejestru licznikowego oraz bezpośrednie zwracanie zmiennej lokalnej `currentChoice`. 2/2 mutacje zweryfikowane (`Pathfinding:ComputeRouteCost` -> 15/15 fail w turze 1, `DialogDrawing:MenuMeasuring` -> 15/15 fail w turze 2); Golden Master 15/15 w pełni zielony (28.9s); zbudowano standalone `.exe` (Release i Debug); 0 błędów i 0 ostrzeżeń w `Civilization1Remaster.Core` i `Civilization1Remaster.Desktop`.

**Faza 3 część 13 (Uderzenie 3/6, 2026-10-28):** eliminacja rejestrów x86 w modułach narzędzi wspólnych, sztucznej inteligencji, zarządzania jednostkami i tury gracza: `CommonUtilities.cs`, `AIPlayer.cs`, `UnitManagement.cs` oraz `PlayerTurn.cs`. W `CommonUtilities.cs`: usunięcie wszystkich 12 odwołań do `RegAcc` w `SeedRandomizer`, `VerifyValueSpan`, `LocateCityAtPosition`, `LocateNearestCity`, `LocateNearestUnit`, `MergeCoordinates`, `ObtainDistanceFromOffsets`, `ObtainDistance` i `ObtainPlayerPopulation` (moduł osiągnął Total=0 rejestrów x86). W `AIPlayer.cs`: usunięcie martwego `RegData` oraz zastąpienie sprawdzania miast zmienną lokalną `maxTownsScore` (moduł osiągnął Total=0 rejestrów x86). W `UnitManagement.cs`: usunięcie 12 martwych przypisań `RegAcc` w `ConstructUnit`, `LocateStackLeader`, `LocateBestGuardianInStack`, `Tally...`, `MayOccupyTile`, `VerifyTileOccupation`, `ContainsAdjacentForeignUnit`, `DisplayBattleScreen`, `LocateNearestFriendlyCity` (pozostał wyłącznie odosobniony mostek w `VisitEachUnitInStack`). W `PlayerTurn.cs`: eliminacja 23 odwołań `RegAcc` w `LocateClickedTile`, `BuildIrrigation`, `BuildRoad`, rozmiarze miast (`town.ShownExtent`), krokach jednostek (`threshold`), paliwie samolotów na lotniskowcu, suwakach podatków, potwierdzeniu wyjścia `ConfirmQuit`, kosztach ruchu i `VerifyPositionOnScreen`. 2/2 mutacje zweryfikowane (`CommonUtilities:MergeCoordinates` -> fail w turze 2, `PlayerTurn:VerifyPositionOnScreen` -> fail w turze 1); Golden Master 15/15 w pełni zielony (30.8s); zbudowano standalone `.exe` (Release i Debug); 0 błędów i 0 ostrzeżeń w `Civilization1Remaster.Core` i `Civilization1Remaster.Desktop`.



**Faza 3 – sprzątanie i drugi baseline:** usunięcie przełączników i kodu „classic”, zamrożonej gry
(`Civilization1Remaster.Original`), kopii Reference, testów Dual; testy różnicowe → testy zachowania z tabel przypadków.
B6.7 część 2 (zdarzenia hosta zamiast `PumpMessages`, nowy zegar testów) → **drugie nagranie baseline'u** (powinno
zmienić tylko to, co zależy od czasu). B6.9 (dziennik jako opcjonalne logowanie). Potem odłożone usprawnienia z
backlogu („Obsolete Wonders”, rozszerzone raporty doradców).

Decyzje otwarte: tryb klasyczny dla graczy – propozycja: jako opcje zostają tylko zmiany reguł (leczenie, koleje),
poprawki błędów na stałe (cały tryb „classic” utrzymywałby emulację stosu na zawsze); indeks pomocy.

## Potem (decyzja użytkownika)

Poprawki zachowania z listy „Błędy przetłumaczonego kodu zachowane celowo” w `MIGRATION-BACKLOG.md` (np. głodujące
miasto bez osadnika zawsze ginie). Wtedy testy różnicowe przestają porównywać z oryginałem i zostają testami zachowania.

## Proponowana kolejność

1. Część A, punkty 1, 2, 3, 5, 7 (szybkie, krótszy kod, testy jak dziś).
2. B0: nowe porównanie stanu w testach różnicowych.
3. Część A, punkt 4 (wiadomości): przy B0 łatwiej sprawdzić teksty okienek.
4. B1 → B2 → B3, moduł po module, od najmniejszych.
5. B4, B5.
6. B6 i poprawki zachowania, po decyzji.
