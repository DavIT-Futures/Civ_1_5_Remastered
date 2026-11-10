# Leader portraits

Portraits of the leaders the remaster adds. They ship in `Civ_1_5_Remastered.Core.Engine/Assets/Pictures`;
the face position on the audience screen is in `LeaderPortraits.Face`.

| File | Leader | Shown |
|---|---|---|
| KING14 | Casimir, royal robe | every scenery except the ones below |
| KING15 | Tokugawa | always |
| KING16 | Casimir, suit (`casimir_suit.webp`) | modern republic / democracy |
| KING17 | Casimir, field uniform (`casimir_field.webp`) | modern despotism / anarchy |
| KING18 | Casimir, party tunic (`casimir_tunic.webp`) | communism |
| KING19 | Casimir, parade uniform (`casimir_parade.webp`) | modern monarchy |

A new portrait from a generated picture on a flat green background:

```bash
# Cut it out and fit it into the 139 x 133 body: left, top and width of the crop in the source.
python3 fit_portrait.py casimir_suit.webp casimir_suit_body.png 80 30 924
# Sheet with the 14 face frames: face cell left/top in the body (eyes at top+9, mouth below),
# then the mouth row and its left/right column inside the cell for the speaking frames.
python3 make_sheet.py casimir_suit_body.png KING16 33 22 27 22 29
# KINGnn.PIC and KINGnn.PAL in the game format.
CIV_KING_ART=$PWD dotnet test ../../Civ_1_5_Remastered.Tests --filter KingPortraitBuilder
```

The field uniform: `fit_portrait.py casimir_field.webp casimir_field_body.png 80 4 924`, then
`make_sheet.py casimir_field_body.png KING17 33 26 27 22 29`; the tunic (KING18) takes the same numbers.
The parade uniform, whose crown reaches the top: crop top 0 and `make_sheet.py casimir_parade_body.png KING19 33 25 27 22 29`.

The face is drawn at (90 + left, top + 2) on the audience screen.
