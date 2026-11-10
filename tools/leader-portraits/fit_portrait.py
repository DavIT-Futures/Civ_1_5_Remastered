"""Cuts a generated leader portrait out of its flat green background and fits it into the 139 x 133
body of a KING sheet. Usage: fit_portrait.py source.png target_body.png left top width"""
import sys
import numpy as np
from PIL import Image

source, target = sys.argv[1], sys.argv[2]
left, top, width = map(int, sys.argv[3:6])
W, H = 139, 133
height = round(width * H / W)
a = np.asarray(Image.open(source).convert('RGB')).astype(float)
r, g, b = a[..., 0], a[..., 1], a[..., 2]
# Green screen: how much greener than the other channels; soft edge, and green spill taken out.
excess = g - np.maximum(r, b)
alpha = (1 - ((excess - 40) / 60).clip(0, 1))
a[..., 1] = np.minimum(g, np.maximum(r, b) + 10)
crop = (left, top, left + width, top + height)
pre = Image.fromarray(np.dstack([a * alpha[..., None], alpha * 255]).clip(0, 255).astype(np.uint8), 'RGBA').crop(crop)
# Premultiplied downscale, so the edges do not pick up the background.
small = np.asarray(pre.resize((W, H), Image.LANCZOS)).astype(float)
al = small[..., 3:4] / 255
rgb = np.where(al > 0, small[..., :3] / np.maximum(al, 1e-3), 0)
out = np.dstack([rgb.clip(0, 255), np.where(al[..., 0] > 0.5, 255, 0)]).astype(np.uint8)
Image.fromarray(out, 'RGBA').save(target)
