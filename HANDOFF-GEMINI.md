# Przekazanie pracy: Civilization1Remaster (migracja asm → C#)

Jesteś agentem programistą przejmującym trwający projekt. Czytaj uważnie: projekt ma twarde zasady, a większość pułapek
poniżej kosztowała już godziny. **Odpowiadaj użytkownikowi po polsku.** Kod, komentarze w kodzie i komunikaty commitów
piszemy po angielsku (styl jak w istniejącym kodzie: pełne zdania, rzeczowo).

## 1. Projekt

Remaster Civilization 1 (DOS, 1991) w C# / .NET 10, Windows. Repozytorium: `C:\1\github\Civilization1xDejf_beta`.

- `Civilization1Remaster.Machine` – emulowany procesor x86 (`CPU`): rejestry, stos, pamięć, przerwania zegara i myszy.
- `Civilization1Remaster.Core` – CIV.EXE przetłumaczony z asm na C#, przepisywany na czysty C#. Moduły w `Game/`, `UI/`,
  `Engine/`, `Video/`, `Emulation/`.
- `Civilization1Remaster.Desktop` – okno Avalonia (gra do ręcznych testów:
  `Civilization1Remaster.Desktop/bin/Debug/net10.0/Civilization1Remaster.Desktop.exe`).
- `Civilization1Remaster.Original` – zamrożona kopia gry sprzed migracji (do testów „podwójnej gry”).
- `Civilization1Remaster.Tests` – testy (xUnit v3).

Dokumenty, które trzeba przeczytać przed pracą:
- `CLAUDE.md` (krótkie zasady), `PLAN-PO-MIGRACJI.md` (plan faz i szczegółowy dziennik ostatnich zmian – czytaj od końca),
  `MIGRATION-BACKLOG.md` (historia porcji, lista usprawnień na koniec, błędy oryginału zachowane celowo),
  `.claude/skills/migrate-batch/SKILL.md` (procedura porcji migracji, pułapki testów),
  `Civilization1Remaster.Tests/Differential/README.md`, `Civilization1Remaster.Tests/GoldenMaster/README.md`.

## 2. Zasady (twarde)

1. **Bez zmiany zachowania gry.** Migracja zmienia tylko kod, nie to, co gra robi. Usprawnienia i poprawki błędów oryginału
   wchodzą wyłącznie za przełącznikami `TurnEvents.Classic…` (np. `ClassicHealing`, `ClassicRailroads`,
   `ClassicLabelNumbers`): wyłączony = remaster (gra), włączony = jak oryginał (testy porównujące z oryginałem włączają
   wszystkie przez `HeadlessGame.TurnClassic`).
2. **Golden master musi zostać zielony** (`Tests/GoldenMaster`, baseline nagrane z remastera). Nie nagrywaj baseline'u
   ponownie bez wyraźnej zgody użytkownika.
3. **Commity prosto na `main`**, bez gałęzi. Commituj tylko po zielonym pełnym zestawie. `git add` tylko plików, które
   zmieniłeś. Nie pushuj, jeśli użytkownik nie poprosi.
4. **Po zmianach zbuduj całe rozwiązanie:** `dotnet build Civilization1Remaster.sln -v q -nologo`. Zablokowane pliki =
   gra jest uruchomiona: poproś użytkownika o jej zamknięcie, **nigdy nie zabijaj procesu gry**.
5. **Każde uruchomienie testów z limitem czasu** (pełny zestaw zwykle 10 – 16 min → limit 25 min; pojedyncze testy → 5 min).
   Po przekroczeniu zatrzymaj tylko proces testów i zgłoś to jako błąd. Przykład (PowerShell):
   ```powershell
   $exe = "C:\1\github\Civilization1xDejf_beta\Civilization1Remaster.Tests\bin\Release\net10.0\Civilization1Remaster.Tests.exe"
   $p = Start-Process -FilePath $exe -ArgumentList @("-parallelMode","all") -RedirectStandardOutput full.log -RedirectStandardError full.err -NoNewWindow -PassThru
   $null = $p.Handle
   if (-not $p.WaitForExit(25*60*1000)) { Stop-Process -Id $p.Id -Force; "TIMEOUT" } else { Get-Content full.log | Select-String "\[FAIL\]|Total:" }
   ```
6. Gdy użytkownik testuje ręcznie i prosi, żeby nie odpalać testów – nie odpalaj, dopóki nie powie.

## 3. Testy

- Budowanie testów: `dotnet build Civilization1Remaster.Tests -c Release -v q -nologo` (zawsze Release – ok. 7x szybciej).
- `dotnet test` nie znajduje testów – uruchamiaj exe: `Civilization1Remaster.Tests/bin/Release/net10.0/Civilization1Remaster.Tests.exe -parallelMode all`
  i czytaj tylko przefiltrowane wyjście (`[FAIL]`, `Total:`). Filtry: `-method "*NazwaTestu"`, `-class "*Klasa*"` (kilka = suma).
- Testy potrzebują folderu gry `CIVILIZATION` (oryginalne pliki, poza repo) – jest na maszynie użytkownika.
- Stan obecny: **803/803 zielone** (ostatni commit przed przekazaniem).
- Rodzaje: golden master (pełne tury z autozapisów vs baseline), testy różnicowe (`Tests/Differential`, `BatchNN…`:
  funkcja przepisana vs kopia oryginału w `Differential/Reference`, porównanie stanu gry, AX/DX, śladu wywołań, SP),
  testy podwójnej gry (`Tests/Dual`: gra vs zamrożona `Original`), testy reguł (tabele przypadków).
- Kontrola mutacjami: `python Civilization1Remaster.Tests/Differential/Reference/check_mutations.py mutations.json --jobs 4`
  (format pliku w nagłówku skryptu; 8 – 12 mutacji na porcję, w gałęziach i zapisach stanu).
- `DifferentialPair` i testy podwójnej gry po wyjątku w scenariuszu przywracają SP/BP (wersje zostawiają różne ramki).

## 4. Pułapki narzędzi (ważne!)

- Środowisko: Windows, Git Bash + PowerShell 5.1. Pliki mają CRLF w kopii roboczej (`core.autocrlf=true`, w repo LF).
  `sed -i` w Git Bash zamienia CRLF na LF – to nieszkodliwe (git normalizuje), ale skrypty Pythona porównujące tekst
  muszą najpierw zamienić `\r\n` na `\n` i przy zapisie przywrócić.
- **Heredoki w Bash zjadają ukośniki odwrotne** (`\n`, `\r`, `\(` w Pythonie / regexach) – dłuższe skrypty zapisuj do pliku
  narzędziem do zapisu plików, nie heredokiem. W skryptach używaj `chr(10)` / `chr(13)` zamiast `'\n'`.
- Podmiana metod: `python Civilization1Remaster.Tests/Differential/Reference/replace_methods.py <plik.cs> <plik z metodami>`.
- Kopia oryginału do testu: `extract_reference.py <commit> <plik.cs> <właściwość GameEngine> <metody…>`.
- Nie podstawiaj wyrażeń z wywołaniami funkcji gry w miejsce odczytu rejestru – zmienia ślad wywołań, który testy
  porównują; użyj zmiennej lokalnej w miejscu przypisania.
- Pętle czekające na mysz/klawisz muszą wołać `processor.PumpMessages()` (robią to `ObtainMouseState`,
  `ObtainAndWipeMouseClick`, `KeyAvailable`) – inaczej gra zamarza po kliknięciu (naprawione, nie psuj).

## 5. Technika „sondy” (sprawdzona w tej migracji)

Gdy nie da się statycznie udowodnić, że zmiana nie zmienia zachowania: dodaj **tymczasową** sondę w kodzie (np. przy
każdym odczycie rejestru zapisz do pliku, czy jest równy wartości z C#; przy każdym przywróceniu rejestru – czy go
zmienia), puść pełny zestaw, zmieniaj tylko miejsca potwierdzone (i osiągnięte w testach), sondę usuń przed commitem.
Przy dużej zmianie, która psuje testy – bisekcja (połowa zmian, szybki zestaw kontrolny 30 – 60 s).

## 6. Co zrobiono ostatnio (2026-10-26)

- Faza 0 i 1 zakończone; faza 2 / B3 (emulowany stos → C#) prawie gotowa: lokalne zmienne funkcji są w C#
  (klasa `Engine/FrameWords` dla ramek ze wspólnymi tablicami, z zakresem zostawianym na stosie dla buforów).
- **Decyzja użytkownika:** śmieci ze stosu nie są opcją dla graczy – przełącznik `ClassicStackLeftovers` usunięty,
  `CPU.EnterFrame` zawsze zeruje zmienne lokalne. Usunięte ramki trzymane tylko „dla głębokości” i pary
  `Preserve`/`Restore`, które nic nie przywracały (potwierdzone sondą).
- `ClassicLabelNumbers`: etykieta kontynentu poza 1 – 0x3f dostaje numer 15 (oryginał czytał poza tablicą).
- Poprawka zamrożenia gry po kliknięciu (pętle myszy bez `PumpMessages`).
- 36 odczytów SI/DI zastąpionych zmiennymi C# (sonda).

## 7. Co zostało (liczone w `Civilization1Remaster.Core`)

| Co | Ile | Uwagi |
|---|---|---|
| `EnterFrame` | 51 | głównie bufory tekstów / nazw plików przekazywane przez adres; 4 ramki „przeciekające” (`DivideCivilization`, `OpeningSequence`, `DisplayConquerorScreen`, `LaunchAudience`) – oryginał na niektórych ścieżkach zostawia je na stosie, testy porównują SP |
| Pary `Preserve…`/`Restore…` | 44 | 31 naprawdę przywraca SI/DI (wywołana funkcja je zmienia) |
| Odczyty SI/DI | 24 | wartości zostawiane przez wywołane funkcje (np. `TallyUnitsByDomainInStack` zostawia SI = 0x600 × gracz) |
| `StackWord` | 36 | udawane adresy powrotu, ręczne odkładanie rejestrów |
| Zapisy AX/BX/CX/DX | ~880 | wyniki funkcji idą też przez rejestry; testy porównują AX/DX |

## 8. Plan (rekomendowana kolejność)

1. **B6.8 – bufory w C#:** teksty / nazwy plików przestają leżeć na stosie i w pamięci emulatora; funkcje tekstowe i
   plikowe (`Libc.CopyText`, `BlockText.ImportTextFromFile`, `OpenStream`…) dostają stringi zamiast adresów. Zwalnia to
   większość z 51 ramek. Rób małymi porcjami (3 – 4 funkcje), każda z testem różnicowym i mutacjami (patrz skill
   `migrate-batch`), pełny zestaw po porcji.
2. **Wyniki przez `return`, nie przez rejestry:** funkcje zostawiające wartość w SI/DI/AX zwracają ją jawnie; wołający
   przestają czytać rejestry → znikają odczyty SI/DI, przywracające pary `Preserve`/`Restore`, część zapisów AX…DX.
3. **Faza 3:** usunięcie porównań z oryginałem (zamrożona gra, `Differential/Reference`, testy podwójnej gry; testy
   różnicowe → testy zachowania z tabel przypadków), wtedy także ramek „przeciekających”, porównania SP i emulowanego stosu;
   B6.7 część 2 (zdarzenia okna zamiast `PumpMessages`, nowy zegar testów) → **drugie nagranie baseline'u** (za zgodą
   użytkownika); B6.9 (dziennik jako opcjonalne logowanie).
4. **Usprawnienia z backlogu** (za przełącznikami `Classic…`): ekran „Obsolete Wonders”, rozszerzone raporty doradców,
   konwersja produkcji miasta na złoto/naukę (~50% tarcz, gdy nie ma co budować albo na życzenie gracza).

## 9. Jak pracować z użytkownikiem

- Pisz krótko, po polsku, bez żargonu tam, gdzie się da; po każdym kroku: co zrobione, wynik testów, co dalej.
- Decyzje projektowe (zmiany zachowania, nowe przełączniki, nagranie baseline'u, usunięcie testów) – zapytaj przed.
- Na koniec porcji: wpis w `PLAN-PO-MIGRACJI.md` (co, wynik testów, lekcje), commit, build rozwiązania.

Zacznij od: przeczytania `PLAN-PO-MIGRACJI.md` (ostatnie wpisy) i skilla `migrate-batch`, potem zaproponuj użytkownikowi
pierwszą porcję B6.8 (3 – 4 funkcje z buforem na stosie, np. `Palace.ImportPalaceGraphics`, `TurnEvents.DisplayCivilizationScenery`,
`SpaceshipScreen`, `GameLoadAndSave`) i poczekaj na zgodę.
