# Fixed maps

`EUROPE.PIC` (in `Civ_1_5_Remastered.Core.Engine/Assets/Pictures`) is the map of the EUROPE choice of the
main menu: 80 x 50 tiles from 25W to 52E and 29N to 71.5N, with a rich New World across the Atlantic at the
left edge. It has the format of the original `MAP.PIC` (EARTH): one colour per terrain in the top left
80 x 50 pixels. The starts of the leaders are in `EuropeMap`.

```bash
# Land and sea from a land mask (pip install --target pyenv global-land-mask), already in europe_land.npy:
python3 -I land.py pyenv -25 52 29 71.5 0 europe_land.npy
# Terrain (mountains, forests, steppes, rivers, the New World) into europe.txt and a preview:
python3 -I terrain.py europe_land.npy .
python3 -I preview.py
# The map picture in the game format, next to europe.txt:
CIV_MAP_GRID=$PWD/europe.txt dotnet test ../../Civ_1_5_Remastered.Tests --filter MapPictureBuilder
```

The map wraps east to west: the two last columns are sea, so the New World is about five tiles of sea away
from Russia and Persia, and seven to ten from Ireland and Spain.
