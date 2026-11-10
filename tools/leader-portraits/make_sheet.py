"""Turns a 139 x 133 leader body (transparent background) into a KING sheet: 320 x 200 colour indexes
(.raw) and a 6-bit palette (.pal6). Colours 64..142 hold the portrait; 0 is transparent.
Usage: make_sheet.py body.png target face_left face_top mouth_row mouth_left mouth_right"""
import sys
import numpy as np
from PIL import Image

body_path, target = sys.argv[1], sys.argv[2]
bx, by, row, x0, x1 = map(int, sys.argv[3:8])
body = Image.open(body_path).convert('RGBA')
px = np.asarray(body)
opaque = px[..., 3] > 127
# The face gets its own share of the colours, or the clothes take them all and the skin turns flat.
head = np.zeros(opaque.shape, bool)
head[0:64, 48:92] = True
def colours(selected, count):
    pixels = px[..., :3][selected & opaque].reshape(-1, 1, 3)
    q = Image.fromarray(pixels.astype(np.uint8)).quantize(colors=count, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    return np.array(q.getpalette()[:count * 3]).reshape(count, 3)
pal = np.vstack([colours(head, 40), colours(~head, 39)])
flat = px[..., :3].reshape(-1, 1, 3).astype(int)
idx = (((flat - pal[None, :, :]) ** 2).sum(2).argmin(1)).reshape(opaque.shape) + 64
idx[~opaque] = 0
sheet = np.zeros((200, 320), np.uint8)
sheet[67:200, 181:320] = idx
face = idx[by:by + 49, bx:bx + 59]
place = 0
cells = []
for f in range(14):
    if f in (8, 11):
        place += 2
    h, v = (place % 5) * 60 + 1, (place // 5) * 50
    sheet[v + 1:v + 50, h:h + 59] = face
    cells.append((h, v))
    place += 1
full = np.zeros((256, 3), int)
full[64:143] = pal
lum = pal.sum(1)
near = lambda t: 64 + int(((pal - np.array(t)) ** 2).sum(1).argmin())
dark, mid = near((40, 26, 20)), near((80, 56, 44))
def put(cell, y, x, c):
    h, v = cells[cell]
    if sheet[v + 1 + y, h + x] != 0:
        sheet[v + 1 + y, h + x] = c
for x in range(x0, x1 + 1):
    put(9, row, x, dark)
    put(8, row + 1, x, dark)
for x in range(x0 + 1, x1):
    put(9, row + 1, x, mid)
    put(8, row, x, dark)
    put(8, row + 2, x, mid)
open(target + '.raw', 'wb').write(sheet.tobytes())
open(target + '.pal6', 'wb').write((full // 4).astype(np.uint8).tobytes())
Image.fromarray(full[sheet].astype(np.uint8)).save(target + '_sheet.png')
