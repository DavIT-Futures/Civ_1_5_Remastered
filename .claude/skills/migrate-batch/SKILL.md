---
name: migrate-batch
description: Jedna porcja migracji asm → C# w Civilization1Remaster (przepisanie 3–4 funkcji w stylu asm na czysty C# bez zmiany zachowania, z testami różnicowymi, mutacjami, pełnymi testami, commitem i buildem). Używaj, gdy użytkownik prosi o „kolejną porcję”, „następne porcje” albo pętlę migracji.
---

# Porcja migracji asm → C#

Cel: funkcje `Civilization1Remaster.Core` w stylu przetłumaczonego asm (goto, `FlagSet`, `CompareWord`…) przepisać na czytelny
C# o **identycznym zachowaniu** (pamięć, ekrany, ślad wywołań, rejestry, wyjątki). Szczegóły i historia: `MIGRATION-BACKLOG.md`
(„Jak pracować”, „Następny krok”, „Śmieci na stosie”, „Błędy zachowane celowo”), `Civilization1Remaster.Tests/Differential/README.md`,
`Civilization1Remaster.Tests/GoldenMaster/README.md`. Odpowiadaj po polsku, commituj na `main`.

Narzędzia (`Civilization1Remaster.Tests/Differential/Reference`, uruchamiać z katalogu głównego repo):
- `find_asm_methods.py Civilization1Remaster.Core 60` – kandydaci od najkrótszych;
- `extract_reference.py <commit HEAD> <plik .cs> <właściwość GameEngine> <metody…>` – kopia oryginału do testu (właściwości:
  `grep -n "public <Klasa> " Civilization1Remaster.Core/GameEngine.cs`; nową klasę `XReference` dopisać do `ReferenceClasses.cs`; stałe klasy, np. `HandBayTally`, trzeba w kopii poprzedzić nazwą klasy);
- `replace_methods.py <plik .cs> <plik z metodami>` – podmienia metody po nazwie, nowe pomocnicze wstawia za poprzednią;
- `check_mutations.py <mutations.json> [--jobs N]` – kontrola mutacjami, równolegle (domyślnie 4) w trwałych kopiach repo
  `.mutants/w1…` (git worktree, budowanie przyrostowe); drzewo robocze nietknięte, więc można dalej pracować (nie budować
  tylko `Civilization1Remaster.Tests` w drzewie, gdy biegnie pełny zestaw).

Testy zawsze w **Release** z `-parallelMode all` (ok. 7 razy szybciej niż Debug, te same wyniki, także golden master):
`dotnet build Civilization1Remaster.Tests -c Release -v q -nologo`, potem
`T=Civilization1Remaster.Tests/bin/Release/net10.0/Civilization1Remaster.Tests.exe`. Filtry: kilka `-class "*X*"` = suma;
`-method "*NazwaTestu"` (wzorzec `*Klasa*.Metoda` nie działa); `-namespace` z `-class` = iloczyn (zwykle 0 testów).

## Strategia testów (od 2026-10-19, po partii 125)

- **Porcja = 3 – 4 kawałki** (funkcje albo fragmenty dużej metody). Po każdym kawałku tylko jego test różnicowy
  (`$T -method "*NazwaTestu"`, 20 – 60 s). Golden master, mutacje, commit – raz na porcję (jeden commit na kawałek albo na
  porcję, wpisy w backlogu dla każdego kawałka).
- **Testy z tabel przypadków, nie z losowania.** Nowe testy: jawna lista przypadków (rekord z parametrami), każdy scenariusz
  z punktu kontrolnego (`fresh: true`), więc dopisanie przypadku nie zmienia pozostałych i wcześniej złapane mutacje nie trzeba
  powtarzać. Przypadki dobierać do gałęzi nowego kodu. Istniejące testy losowe są zamrożone – nie dopisywać do nich wariantów
  (każda zmiana przesuwa losowanie); nowe pokrycie przez nowe metody testowe z tabelami. `HandleCity`: tabele w
  `Tests/Dual/HandleCityCaseTables.cs`, sprawdzane na podwójnej grze (`DualCityCaseTests`, losowe: `DualCityRandomTests`).
- **Mutacje: 8 – 12 na porcję**, tylko w rozgałęzieniach i zapisach stanu (bez każdej granicy `<`/`<=`), jedna runda,
  `--jobs 6`. Ocalała: jeden celowany przypadek w tabeli albo opis (nieosiągalna / równoważna / martwy zapis) – bez kolejnych
  rund.
- **Pełny zestaw tylko przy kamieniach milowych:** po zmianie wspólnego kodu (`Civilization1Remaster.Machine`, harness,
  `UnitMemory`, `MapLayers`, `CommonUtilities`), po skończeniu dużego fragmentu (np. całej metody) i na koniec sesji.
  Między nimi golden master (16 s) na porcję. **Wyjątek – B2 (zmienne lokalne ze stosu): pełny zestaw po każdej porcji**,
  bo usunięcie zapisu do ramki może zmienić śmieci, które inna funkcja na tej samej głębokości stosu czyta jako
  niezainicjowaną zmienną (`RenderCitizenIcons` 0x2, `HandlePlayerTurn` 0x12 / 0x24 / 0x36 / 0x3e …); wychodzi to w testach
  innych modułów. Taki zapis zostaje (fizycznie na stos, obok zmiennej C#).

## Kroki

1. `get_usage`: 5-godzinny ≥ 95% → czekać na reset; tygodniowy ≥ 92% → skończyć z podsumowaniem.
2. Wybrać 4 funkcje z różnych modułów (`find_asm_methods.py`, lista „Następny krok”). Pominąć te ze stosem jako buforem
   powrotów, splątanymi skokami albo czytające niezainicjowane zmienne – zapisać powód w backlogu.
3. Przeczytać oryginał w całości (sed -n), przepisać do pliku w scratchpadzie (narzędzie Write, nie heredoc). Zasady:
   - Wiersz `OpenScope` i `CloseScope` identyczny z oryginałem; te same wywołania funkcji gry w tej samej kolejności, z tymi
     samymi argumentami (także wywołania „bez sensu”, np. wynik nieużywany). Wartości z `GameState` czytać tylko tam, gdzie oryginał
     je czyta (indeks spoza tablicy = wyjątek, którego oryginał nie rzuca).
   - Arytmetyka 16-bitowa: `(short)`/`(ushort)` jak w oryginale; porównania ze znakiem (`IsLess`, `IsGreater`) vs bez (`IsBelow`,
     `IsAbove`); `HalveAccumulator` = dzielenie ze znakiem w stronę zera; dzielenie słów przez `CPU.SignedQuotient`.
   - Rejestry: AX zwracany (i AX zostawiany przez oryginał, np. `0x600 * gracz` z liczenia adresu – sprawdza test z `Register.Acc`),
     SI/DI zachowane. **Ramkę stosu (`EnterFrame`, `Preserve…`) zostawić**, gdy funkcja woła coś, co czyta śmieci spod stosu
     (`RenderCitizenIcons`, `ObtainDistanceToReplayCity`, `HandleCity`, …) albo zmienia SI (`VisitEachUnitInStack`, `TallyUnits…InStack`), oraz
     gdy bufor (nazwa pliku, lista) jest w ramce. `DisplayMenu` / `DisplayDialog` zostawiają w DI wartość z SI – ustawić SI jak oryginał.
   - Zegar testów tyka co N dostępów `CPU.Pull…/Emit…` (`StackWord`/`Unstack…`, `Storage` i `rep movsb` się nie liczą, ale
     zmienne lokalne `PullLocalWord`/`EmitLocalWord` tak – kod czekający na muzykę albo 0x5c zostawia je w ramce i czyta tyle
     razy co oryginał, porcje 72 – 73). Kopię `rep movs` robić
     przez `processor.Storage`; w kodzie, w którym liczy się moment tyknięcia (palety, cykle kolorów, oczekiwanie na zegar),
     zachować liczbę dostępów do pamięci (porcja 51).
   - Jednostki: `UnitMemory` (pamięć 0x81d4), typy: `UnitKindTable`, mapa: `MapOps.Chart` (`MapLayers`), stałe: `MapRules`.
4. `extract_reference.py` (z commita HEAD, **przed** podmianą), potem `replace_methods.py`, build Release (jak wyżej).
5. Test różnicowy `BatchNNDifferentialTests.cs` (wzory: poprzednie Batch*): scenariusze z `new Random(bay)`, lista gałęzi, które muszą
   wystąpić. Pułapki: losować tylko w `make` (nigdy w `Prepare` – wykonuje się dwa razy), `make` nie woła funkcji gry na
   `pair.Original`; dane spoza zakresu gry (tekst poniżej wiersza 192, ustrój 6, za dużo wierszy raportu) rzucają wyjątek w obu
   wersjach z różnymi rejestrami – ograniczyć do realnych; pliki i tabele spoza segmentu 0x3b01 przez `Watched` (okno w 0x3772 najwyżej do 0x3931, dalej jest segment danych
   ze zmiennymi zegara) albo hasz w 0xbc00; obrazki z `ScreenToPicture` (poza migawką i śladem) narysować na ekranie po wywołaniu.
   `GameSnapshot` pomija zmienne zegara 0x40 – 0x6f, stan myszy 0x586e – 0x5873 i stos – co funkcja tam pisze, idzie do `Watched`.
   Ziarno losowania z zegara ściennego (`SeedRandomizer`, `time()`): w `Prepare` ustawić `game.Libc.Clock` na stały moment.
   Ekrany w trakcie (przewijanie, animacja) widać w śladzie tylko przy `keyboard.KeyTyping` – można go wołać z własnego
   `game.Processor.InputStarved` przy każdym zapytaniu o klawisz (porcja 72).
   Uruchomić: `$T -parallelMode all -class "*BatchNN*" 2>&1 | grep -E "\[FAIL\]|Total:|differs|^\s+(reg|cpu|mem|state|screen|trace|call)[.:]|No scenario"`.
   **Zawieszenia:** `DifferentialPair.Run` przerywa scenariusz po `RunSeconds` (120 s; 30 s nie starczało tworzeniu świata przy pełnym zestawie) na grę (`ScenarioHangException` z
   dziennika albo z pulsu CPU co 0x40000 odczytów pamięci) i test pada z nazwą scenariusza i ostatnią funkcją – nie wisi
   godzinami. Test, który zmienia stan (budynki, jednostki, ustrój, przesunięte miasta), uruchamiać z `fresh: true` w `Check`:
   każdy scenariusz startuje z punktu kontrolnego po wczytaniu (`GameCheckpoint`: głęboka kopia `GameState`, pamięć, rejestry,
   ekrany; ok. 0,1 s); bez tego zmiany kumulują się i oryginał zapętla się na nienaturalnym stanie (porcja 120).
   Nowe losowania dopisywać na końcu `make`, żeby nie przesuwać scenariuszy, które już łapały mutacje.
   Szukanie miejsca pętli: `HeadlessGame.StartProbe(plik)` (co 2 s liczba wywołań i ostatnia funkcja; stały licznik = pętla
   bez wywołań funkcji gry). Dawniej: odtworzyć sekwencję na jednej `HeadlessGame` (w `DifferentialPair` wyjątki są przechwytywane), z zegarem
   wołającym `Processor.WhenApplicationQuits()` (wyjątek ze stosem) i `ScopeListener` na wszystkich dziennikach; scenariusz,
   w którym obie wersje rzucają wyjątek, zostawia grę w połowie – następne scenariusze mogą wtedy wisieć (porcja 55).
   Gdy obie wersje rzucą ten sam wyjątek, a ramka zostaje, test wygląda na zgodny – etykieta gałęzi powinna potwierdzać,
   że funkcja doszła do końca (porcja 64: tura człowieka ze skryptem [Enter] nigdy się nie kończy).
   Różnica, której nie da się wyjaśnić: tymczasowo obie lambdy z kopią referencyjną (oryginał kontra oryginał) – jeśli dalej
   jest, to artefakt testu (np. ścieżka CIV.EXE w buforze 0xba06 nadpisana długim tekstem, dane spoza zakresu gry).
   Tura gracza (`HandlePlayerTurn`, porcje 90 – 92): cała tura w obu grach; tura człowieka przez własne `InputStarved`
   (klawisze i kliknięcia: 0x586e / 0x5870 / 0x5874 przy 0x1a3c = 1 tylko na czas jednego kliknięcia, potem klawisze
   Space, Enter, '1', Escape – bez Escape wskaźnik GoTo albo wybrana jednostka czeka na klawisze bez końca); rozkazy, których
   jednostka nie wykona (rozkazy osadników dla innych, bity rozkazów samolotu), zapętlają turę (limit 20000 klawiszy, ok.
   40 s). Porównywać ramkę i 0x80 bajtów stosu pod nią (`ShiftUnit` zostawia tam SI dla `ShiftAIUnit`).
6. Mutacje: `mutations.json` z jedną mutacją na funkcję (w gałęzi trudnej do trafienia), `python …/check_mutations.py mutations.json`
   (zawieszony scenariusz pada po 120 s = CAUGHT; proces dłuższy niż 400 s jest zabijany z dziećmi = CAUGHT (hang)).
   Budżet: 8 – 12 mutacji na porcję (patrz „Strategia testów”) (jedna mutacja ≈ 10 s budowania + czas testu; 4 workery). Po zmianie testu powtarzać tylko
   ocalałe i mutacje gałęzi, których dotyczy zmiana – nigdy całego zbioru z poprzednich porcji (godziny, porcje 116 – 119).
   Długie polecenia zawsze w tle (`run_in_background`), nie przez `timeout` w Bash na pierwszym planie.
   Każda musi być CAUGHT; SURVIVED = albo słaby test (dodać scenariusze), albo mutacja nieosiągalna (zmienić). Mutacja musi
   leżeć w funkcji, którą sprawdza podany test (porcje 68 / 70: mutacje w `SeizeCity` / `FocusMapView` „przeżywały” w testach
   innych funkcji).
7. Po każdej porcji: golden master + testy porcji (ok. 30 s):
   `$T -parallelMode all -class "*GoldenMaster*" -class "*BatchNN*" 2>&1 | grep -E "\[FAIL\]|Total:"`.
   Pełny zestaw (ok. 13 min w Release): przy kamieniach milowych (wyżej), przed końcem sesji i po każdej zmianie wspólnego kodu (`Civilization1Remaster.Machine`,
   `MapLayers`, `UnitMemory`, `CommonUtilities`, `GameSnapshot`/harness): `$T -parallelMode all 2>&1 | grep -E "\[FAIL\]|Total:"`.
   Uzasadnienie: test różnicowy funkcji G woła bieżący kod innych funkcji w obu grach, więc błąd w przepisanej F wykrywa test F
   i golden master, nie stare testy.
8. `MIGRATION-BACKLOG.md`: wpis porcji (wnioski!), lista kandydatów, stan (`find_asm_methods.py … 0 | tail -1`).
9. `git add` tylko plików porcji, commit na `main` z `Co-Authored-By`, `dotnet build Civilization1Remaster.sln -v q -nologo`
   (zablokowane pliki = gra uruchomiona: nie zabijać procesu, poprosić o zamknięcie).

## Praca bez użytkownika

Laptop zasypia po zamknięciu klapy (keep-awake blokuje tylko usypianie z bezczynności) – przed długą pracą poprosić o otwarty
laptop na zasilaniu albo ustawienie „Nic nie rób” dla pokrywy. Pętli nie czyścić przez `clear_session("self")` – kasuje
zaplanowane wybudzenie. Chmura odpada: testy wymagają folderu `CIVILIZATION` (poza repo).
