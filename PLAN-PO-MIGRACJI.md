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

### B1. Wyniki przez `return` zamiast przez AX (848 miejsc)

Metoda po metodzie: `ObtainCitizenType(ea); x = RegAcc` → `x = ObtainCitizenType(ea)`. Najpierw te, których wynik
nikt nie czyta z rejestru po drodze. Uwaga: kod spoza przepisanych modułów (okienka, grafika) też może czytać AX.

### B2. Zmienne lokalne pod numerami → zmienne i parametry C# (2648 miejsc)

Największa porcja pracy, ale najprostsza do zrozumienia. Zaczynać od funkcji z małą ramką; `HandleCity` (268 bajtów
dzielonych przez ~60 metod) i `HandlePlayerTurn` na końcu. W `HandleCity` przyda się klasa „stan rundy miasta” z polami
zamiast `0xe8`, `0xea`, `0xfc`…

### B3. SI/DI i ramki stosu (596 + 245 miejsc)

Gdy funkcja nie ma już zmiennych lokalnych w pamięci, ramkę (`EnterFrame`) i zapis SI/DI można usunąć. Haczyk z tej
sesji: menu (`OperateMenu`, `PreserveIndexRegisters`) odkłada SI/DI na stos, a test porównuje ten obszar. Po B0 to
porównanie przestanie wymuszać odtwarzanie SI/DI.

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
