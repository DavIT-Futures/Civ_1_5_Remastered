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
- `check_mutations.py <mutations.json>` – kontrola mutacjami z zabezpieczeniem pracy patchem.

Testy zawsze w **Release** z `-parallelMode all` (ok. 7 razy szybciej niż Debug, te same wyniki, także golden master):
`dotnet build Civilization1Remaster.Tests -c Release -v q -nologo`, potem
`T=Civilization1Remaster.Tests/bin/Release/net10.0/Civilization1Remaster.Tests.exe`. Filtry: kilka `-class "*X*"` = suma;
`-method "*NazwaTestu"` (wzorzec `*Klasa*.Metoda` nie działa); `-namespace` z `-class` = iloczyn (zwykle 0 testów).

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
   - Zegar testów tyka co N dostępów `CPU.Pull…/Emit…` (stos, `Storage` i `rep movsb` się nie liczą). Kopię `rep movs` robić
     przez `processor.Storage`; w kodzie, w którym liczy się moment tyknięcia (palety, cykle kolorów, oczekiwanie na zegar),
     zachować liczbę dostępów do pamięci (porcja 51).
   - Jednostki: `UnitMemory` (pamięć 0x81d4), typy: `UnitKindTable`, mapa: `MapOps.Chart` (`MapLayers`), stałe: `MapRules`.
4. `extract_reference.py` (z commita HEAD, **przed** podmianą), potem `replace_methods.py`, build Release (jak wyżej).
5. Test różnicowy `BatchNNDifferentialTests.cs` (wzory: poprzednie Batch*): scenariusze z `new Random(bay)`, lista gałęzi, które muszą
   wystąpić. Pułapki: losować tylko w `make` (nigdy w `Prepare` – wykonuje się dwa razy), `make` nie woła funkcji gry na
   `pair.Original`; dane spoza zakresu gry (tekst poniżej wiersza 192, ustrój 6, za dużo wierszy raportu) rzucają wyjątek w obu
   wersjach z różnymi rejestrami – ograniczyć do realnych; pliki i tabele spoza segmentu 0x3b01 przez `Watched` (okno w 0x3772 najwyżej do 0x3931, dalej jest segment danych
   ze zmiennymi zegara) albo hasz w 0xbc00; obrazki z `ScreenToPicture` (poza migawką i śladem) narysować na ekranie po wywołaniu.
   Uruchomić: `$T -parallelMode all -class "*BatchNN*" 2>&1 | grep -E "\[FAIL\]|Total:|differs|^\s+(reg|cpu|mem|state|screen|trace|call)[.:]|No scenario"`.
   Test wisi: odtworzyć sekwencję na jednej `HeadlessGame` (w `DifferentialPair` wyjątki są przechwytywane), z zegarem
   wołającym `Processor.WhenApplicationQuits()` (wyjątek ze stosem) i `ScopeListener` na wszystkich dziennikach; scenariusz,
   w którym obie wersje rzucają wyjątek, zostawia grę w połowie – następne scenariusze mogą wtedy wisieć (porcja 55).
   Różnica, której nie da się wyjaśnić: tymczasowo obie lambdy z kopią referencyjną (oryginał kontra oryginał) – jeśli dalej
   jest, to artefakt testu (np. ścieżka CIV.EXE w buforze 0xba06 nadpisana długim tekstem, dane spoza zakresu gry).
6. Mutacje: `mutations.json` z jedną mutacją na funkcję (w gałęzi trudnej do trafienia), `python …/check_mutations.py mutations.json`.
   Każda musi być CAUGHT; SURVIVED = albo słaby test (dodać scenariusze), albo mutacja nieosiągalna (zmienić).
7. Po każdej porcji: golden master + testy porcji (ok. 30 s):
   `$T -parallelMode all -class "*GoldenMaster*" -class "*BatchNN*" 2>&1 | grep -E "\[FAIL\]|Total:"`.
   Pełny zestaw (ok. 5,5 min w Release): co 4 porcje, przed końcem sesji i po każdej zmianie wspólnego kodu (`Civilization1Remaster.Machine`,
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
