# Leader portraits

Draft portraits of the leaders the remaster adds. They ship in
`Civ_1_5_Remastered.Core.Engine/Assets/Pictures`:

| File | Leader | Shown |
|---|---|---|
| KING14 | Casimir, royal robe | ancient sceneries |
| KING15 | Tokugawa | always |
| KING16 | Casimir, parade uniform | modern monarchy |
| KING17 | Casimir, field uniform | modern despotism / anarchy |
| KING18 | Casimir, party tunic | communism |
| KING19 | Casimir, suit | modern republic / democracy |

The bodies (`*_body.png`, 139 x 133, transparent background) come from the portraits in
`website/public/screens/dlc-leaders.png`.

```bash
python3 casimir_variants.py casimir_body.png
for v in "robe KING14" "parade KING16" "field KING17" "tunic KING18" "suit KING19"; do set -- $v; python3 make_sheet.py casimir_$1_body.png $2 41 14 23 26 29; done
python3 make_sheet.py tokugawa_body.png KING15 33 25 21 25 29
CIV_KING_ART=$PWD dotnet test ../../Civ_1_5_Remastered.Tests --filter KingPortraitBuilder
```

The last step writes `KINGnn.PIC` and `KINGnn.PAL`; copy them to the assets folder. The face position
on the audience screen is in `LeaderPortraits.Face`.
