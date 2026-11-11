"""Europe for Civ 1.5 Remastered: 80 x 50 tiles from 25W to 52E and 29N to 71.5N (plate carree).

Land and sea come from a land mask (land.py, c.npy); the terrain follows simple geographic rules
and hand drawn regions (mountain ranges, forests, steppes, marshes, deserts) and rivers.
Output: europe.txt (80 x 50 terrain letters), europe_preview.png.
"""
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

W, H = 80, 50
LON0, LON1, LAT0, LAT1 = -25.0, 52.0, 29.0, 71.5
frac = np.load(sys.argv[1])
out = sys.argv[2]

def tile(lon, lat):
    return (int((lon - LON0) / (LON1 - LON0) * W), int((LAT1 - lat) / (LAT1 - LAT0) * H))

def lonlat(x, y):
    return (LON0 + (x + .5) * (LON1 - LON0) / W, LAT1 - (y + .5) * (LAT1 - LAT0) / H)

land = frac > 0.45

# Straits kept open (sea routes), and small islands kept as land.
def sea(lon, lat):
    x, y = tile(lon, lat); land[y, x] = False
def ground(lon, lat):
    x, y = tile(lon, lat); land[y, x] = True
for p in [(-5.6, 35.95), (26.4, 40.2), (29.0, 41.1), (12.6, 55.7), (10.9, 55.4), (36.6, 45.3), (9.5, 57.5)]:
    sea(*p)
for p in [(9.0, 40.0), (9.1, 42.1), (14.2, 37.5), (24.9, 35.2), (33.2, 35.1), (2.9, 39.6)]:
    ground(*p)

# The Caspian and Azov seas (the mask counts the Caspian as land).
caspian = [(46.8, 46.3), (49.5, 46.9), (52, 45.0), (52, 37.4), (50.2, 36.8), (49, 37.6), (49.3, 40.2), (47.6, 41.6), (47.4, 43.6), (46.9, 44.7)]
for y in range(H):
    for x in range(W):
        lon, lat = lonlat(x, y)
        if lon > 45:
            n = len(caspian); c = False
            for i in range(n):
                x1, y1 = caspian[i]; x2, y2 = caspian[(i + 1) % n]
                if (y1 > lat) != (y2 > lat) and lon < (x2 - x1) * (lat - y1) / (y2 - y1) + x1:
                    c = not c
            if c:
                land[y, x] = False

# Terrain letters: o ocean, d desert, p plains, g grassland, f forest, h hills, m mountains,
# t tundra, a arctic, s swamp, j jungle, r river.
grid = np.full((H, W), 'o', dtype='<U1')

def inside(poly, lon, lat):
    n = len(poly); c = False
    for i in range(n):
        x1, y1 = poly[i]; x2, y2 = poly[(i + 1) % n]
        if (y1 > lat) != (y2 > lat) and lon < (x2 - x1) * (lat - y1) / (y2 - y1) + x1:
            c = not c
    return c

regions = [
    # (terrain, polygon in lon/lat) - later entries win
    ('f', [(4, 58), (31, 58), (31, 66.5), (4, 66.5)]),                      # Scandinavian taiga
    ('f', [(28, 56), (52, 56), (52, 66), (28, 66)]),                        # Russian taiga
    ('g', [(-10, 43), (20, 43), (20, 55), (-10, 55)]),                      # temperate west
    ('g', [(14, 47), (28, 47), (28, 54.5), (14, 54.5)]),                    # Poland, Hungary
    ('p', [(-9, 37), (3, 37), (3, 43), (-9, 43)]),                          # Meseta
    ('p', [(28, 44), (52, 44), (52, 52), (28, 52)]),                        # Pontic steppe
    ('p', [(40, 52), (52, 52), (52, 56), (40, 56)]),                        # Volga steppe
    ('p', [(26, 36.5), (44, 36.5), (44, 40.5), (26, 40.5)]),                # Anatolia
    ('g', [(26, 50), (40, 50), (40, 56), (26, 56)]),                        # Ukraine and west Russia
    ('f', [(23, 51), (32, 51), (32, 56), (23, 56)]),                        # Belarus forests
    ('s', [(24, 51.3), (30.5, 51.3), (30.5, 52.6), (24, 52.6)]),            # Polesie marshes
    ('f', [(6, 47.5), (15, 47.5), (15, 51), (6, 51)]),                      # German uplands forest
    ('f', [(15, 49), (24, 49), (24, 50.5), (15, 50.5)]),                    # Carpathian foreland
    ('f', [(15, 42), (24, 42), (24, 46), (15, 46)]),                        # Balkan forests
    ('h', [(-5.5, 56), (-2, 56), (-2, 58.7), (-5.5, 58.7)]),                # Scottish Highlands
    ('h', [(1.5, 44.2), (4.5, 44.2), (4.5, 46.2), (1.5, 46.2)]),            # Massif Central
    ('h', [(12, 48.8), (16.5, 48.8), (16.5, 50.8), (12, 50.8)]),            # Bohemia
    ('h', [(11, 41), (16, 41), (16, 44), (11, 44)]),                        # Apennines
    ('h', [(29, 37), (40, 37), (40, 39.5), (29, 39.5)]),                    # Anatolian plateau
    ('h', [(-9, 38), (-3, 38), (-3, 40), (-9, 40)]),                        # Sierra Morena
    ('m', [(-1.8, 42.4), (3, 42.4), (3, 43.1), (-1.8, 43.1)]),              # Pyrenees
    ('m', [(5.8, 45.2), (16, 45.8), (16, 47.4), (6, 46.7)]),                # Alps
    ('m', [(18.5, 48.7), (25.5, 47.0), (26.8, 45.4), (22.5, 45.0), (18.5, 49.6)]),  # Carpathians
    ('m', [(15.5, 44.0), (20, 41.5), (21.5, 42.2), (17, 44.8)]),            # Dinaric Alps
    ('m', [(22, 39), (24, 39), (24, 42), (22, 42)]),                        # Pindus, Rhodope
    ('m', [(38.5, 42.3), (48, 41.2), (48, 43.2), (39, 44)]),                # Caucasus
    ('m', [(-9.6, 30.5), (-4, 31.2), (2, 34.5), (8, 35.5), (8, 36.4), (-1, 35.1), (-9.6, 31.7)]),  # Atlas
    ('m', [(6, 60), (9, 59), (16, 66), (21, 69.5), (18, 69.8), (12, 66)]),  # Scandinavian mountains
    ('m', [(41, 37), (46, 37), (46, 39.5), (41, 39.5)]),                    # Armenian highlands
    ('m', [(44.5, 34), (48, 33), (51, 35.5), (49, 37), (46, 36)]),          # Zagros, Alborz
    ('h', [(-23, 63.5), (-14, 63.5), (-14, 66), (-23, 66)]),                # Iceland
    ('d', [(-25, 29), (52, 29), (52, 33.4), (-25, 33.4)]),                  # Sahara, Libya, Arabia
    ('p', [(-10, 31.5), (11, 31.5), (11, 37.5), (-10, 37.5)]),              # Maghreb coast
    ('d', [(43, 33), (52, 33), (52, 36), (43, 36)]),                        # Syrian desert edge
    ('g', [(29.5, 30.0), (32.3, 30.0), (32.5, 31.6), (29.2, 31.6)]),        # Nile delta
    ('p', [(34, 31), (37, 31), (37, 36.5), (34, 36.5)]),                    # Levant
    ('g', [(43.5, 30.5), (48, 30.5), (48, 33.6), (43.5, 33.6)]),            # Mesopotamia
    ('p', [(46, 44), (52, 44), (52, 48.5), (46, 48.5)]),                    # Caspian lowland
]

for y in range(H):
    for x in range(W):
        if not land[y, x]:
            continue
        lon, lat = lonlat(x, y)
        t = 'g'
        if lat > 66:
            t = 't'
        elif lat > 60 and lon > 28:
            t = 'f'
        for terrain, poly in regions:
            if inside(poly, lon, lat):
                t = terrain
        if lat > 69.5:
            t = 't'
        if lon < -13 and lat > 63:
            t = 'h' if 64.2 < lat < 65.6 and -20 < lon < -15 else 't'
        # A natural mix: some tiles of the open lands differ from their region.
        r = ((x * 73856093) ^ (y * 19349663) ^ 0x5bd1e995) % 100
        if t == 'g':
            t = 'p' if r < 14 else 'f' if r < 24 else 'h' if r < 28 else 'g'
        elif t == 'p':
            t = 'g' if r < 12 else 'h' if r < 18 else 'f' if r < 22 and lat > 40 else 'p'
        elif t == 'f':
            t = 'g' if r < 8 and lat < 60 else 's' if r < 12 and lat > 58 else 'h' if r < 16 else 'f'
        elif t == 'h':
            t = 'p' if r < 20 else 'm' if r < 28 else 'h'
        elif t == 'd':
            t = 'p' if r < 6 else 'h' if r < 10 else 'd'
        elif t == 't':
            t = 'f' if r < 10 and lat < 68 else 't'
        grid[y, x] = t

# Rivers: lon/lat polylines; a tile on the line becomes river (land only).
rivers = {
    'Danube': [(8.5, 48.0), (13, 48.6), (16.4, 48.2), (18.9, 47.6), (19.0, 46.0), (20.5, 44.8), (22.5, 44.6), (25.5, 43.7), (28.0, 44.2), (29.0, 45.2)],
    'Rhine': [(7.6, 47.7), (8.2, 49.9), (6.9, 50.9), (6.1, 51.8)],
    'Vistula': [(18.9, 49.8), (21.0, 50.4), (21.3, 51.9), (19.0, 52.7), (18.8, 54.2)],
    'Dnieper': [(30.4, 53.9), (30.5, 50.4), (33.4, 49.1), (35.1, 47.8), (33.4, 46.6)],
    'Volga': [(44.0, 56.3), (49.0, 55.8), (48.6, 53.2), (46.0, 51.5), (45.0, 48.8), (47.5, 46.6)],
    'Loire': [(4.0, 45.2), (2.3, 47.5), (-0.5, 47.4), (-2.0, 47.3)],
    'Po': [(7.6, 45.0), (10.0, 45.1), (12.2, 44.95)],
    'Tagus': [(-1.8, 40.4), (-5.0, 39.9), (-8.0, 39.4), (-9.1, 38.8)],
    'Nile': [(32.6, 29.0), (31.2, 30.1), (31.0, 31.0)],
    'Euphrates': [(38.3, 37.0), (40.2, 35.2), (43.6, 33.4), (46.2, 31.6), (47.6, 30.5)],
}
for name, line in rivers.items():
    for (a, b), (c, d) in zip(line, line[1:]):
        steps = int(max(abs(c - a), abs(d - b)) * 4) + 1
        for i in range(steps + 1):
            lon = a + (c - a) * i / steps; lat = b + (d - b) * i / steps
            x, y = tile(lon, lat)
            if 0 <= x < W and 0 <= y < H and grid[y, x] != 'o':
                grid[y, x] = 'r'

# Dardanelles open. The map wraps: beyond the east edge lies the New World, so the last two columns
# are sea (east of the Urals) and the Caspian keeps a shore of steppe and desert on its east.
grid[36, 54] = 'o'
for y in range(H):
    for x in (78, 79, 0, 1):
        grid[y, x] = 'o'
    if 26 <= y <= 41 and grid[y, 77] == 'o':
        grid[y, 77] = 'd' if y > 33 else 'p'

# The New World: a narrow, rich land beyond the Atlantic, at the left edge. Two columns of ocean keep it
# apart from the east edge (the map wraps), and about ten of Atlantic from Europe, so only ships that
# leave the coast (Navigation, Magellan's) reach it. Rich: grassland, rivers, hills, some forest; no one starts there.
import math
for y in range(12, 48):
    centre = 5.6 + 1.3 * math.sin(y / 5.0)
    half = 2.2 + 0.7 * math.sin(y / 3.1 + 1.0)
    if 25 <= y <= 27:
        half -= 1.2  # an isthmus
    for x in range(3, 11):
        if abs(x + 0.5 - centre) <= max(half, 0.6):
            r = ((x * 2654435761) ^ (y * 40503) ^ 0x9e3779b9) % 100
            if y < 16:
                t = 'f' if r < 45 else 't' if r < 60 else 'h' if r < 75 else 'g'
            elif y > 42:
                t = 'g' if r < 35 else 'f' if r < 60 else 'h' if r < 75 else 'p'
            else:
                t = 'g' if r < 40 else 'p' if r < 55 else 'h' if r < 72 else 'f' if r < 88 else 'm' if r < 92 else 'g'
            grid[y, x] = t
    if 18 <= y <= 40 and not 24 <= y <= 28:
        x = int(centre)
        if 3 <= x < 11 and grid[y, x] != 'o':
            grid[y, x] = 'r'  # the great river of the New World

np.save(out + '/europe_grid.npy', grid)
with open(out + '/europe.txt', 'w') as f:
    for row in grid:
        f.write(''.join(row) + '\n')

# Preview in the colours of the original map file.
colours = {'o': (0, 0, 170), 'd': (255, 255, 85), 'p': (170, 85, 0), 'g': (85, 255, 85), 'f': (0, 170, 0),
           'h': (255, 85, 85), 'm': (255, 85, 255), 't': (170, 170, 170), 'a': (255, 255, 255), 's': (0, 170, 170),
           'j': (85, 255, 255), 'r': (85, 85, 255)}
S = 12
img = Image.new('RGB', (W * S, H * S))
d = ImageDraw.Draw(img)
for y in range(H):
    for x in range(W):
        d.rectangle([x * S, y * S, x * S + S - 1, y * S + S - 1], fill=colours[grid[y, x]])
img.save(out + '/europe_plain.png')
