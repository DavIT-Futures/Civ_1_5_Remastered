"""Casimir in the dress of each era: robe (ancient), parade uniform (modern monarchy),
field uniform with cap (modern despotism), party tunic (communism), suit (modern republic).

Input: casimir_body.png (139 x 133, transparent background). Output: <name>_body.png per variant.
"""
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

S = 4  # drawing scale
W, H = 139, 133
src = Image.open(sys.argv[1] if len(sys.argv) > 1 else 'casimir_body.png').convert('RGBA')
a = np.asarray(src).astype(float)
lum = (a[..., 0] * 0.3 + a[..., 1] * 0.59 + a[..., 2] * 0.11) / 255.0
lum = (lum - np.percentile(lum[a[..., 3] > 0], 2)) / (np.percentile(lum[a[..., 3] > 0], 98) - np.percentile(lum[a[..., 3] > 0], 2))
lum = lum.clip(0, 1)


def gradient(stops):
    xs = [s[0] for s in stops]
    out = np.zeros(lum.shape + (3,))
    for c in range(3):
        out[..., c] = np.interp(lum, xs, [s[1][c] for s in stops])
    return out


skin = gradient([(0, (28, 16, 12)), (0.35, (92, 58, 38)), (0.62, (176, 122, 92)), (0.85, (226, 176, 142)), (1, (248, 214, 186))])
gold = gradient([(0, (40, 22, 6)), (0.4, (130, 86, 20)), (0.75, (214, 170, 60)), (1, (255, 236, 150))])
brocade = gradient([(0, (26, 10, 12)), (0.35, (88, 26, 30)), (0.6, (140, 64, 48)), (0.8, (200, 160, 100)), (1, (240, 224, 180))])


def mask(points, scale=1):
    m = Image.new('L', (W * scale, H * scale), 0)
    ImageDraw.Draw(m).polygon([(x * scale, y * scale) for x, y in points], fill=255)
    return m


HEAD = [(54, 17), (85, 17), (85, 36), (83, 46), (77, 56), (69, 61), (61, 56), (55, 46), (52, 30)]
CROWN = [(53, 0), (87, 0), (87, 19), (53, 19)]


def layer(rgb, alpha_mask):
    alpha = np.asarray(alpha_mask).astype(float) / 255.0 * (a[..., 3] / 255.0)
    return Image.fromarray(np.dstack([rgb, alpha * 255]).astype(np.uint8), 'RGBA')


def head(crown):
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    img.alpha_composite(layer(skin, mask(HEAD)))
    if crown:
        img.alpha_composite(layer(gold, mask(CROWN)))
    return img


def hair(draw):
    # Dark hair where the crown was.
    draw.ellipse([55 * S, 9 * S, 84 * S, 30 * S], fill=(46, 28, 18, 255))
    draw.ellipse([58 * S, 11 * S, 72 * S, 18 * S], fill=(70, 44, 28, 255))


def cap(draw, cloth, band):
    draw.polygon([(51 * S, 13 * S), (88 * S, 13 * S), (85 * S, 3 * S), (54 * S, 3 * S)], fill=cloth)
    draw.ellipse([50 * S, 0, 89 * S, 9 * S], fill=cloth)
    draw.rectangle([54 * S, 12 * S, 85 * S, 17 * S], fill=band)
    draw.polygon([(55 * S, 17 * S), (84 * S, 17 * S), (80 * S, 21 * S), (59 * S, 21 * S)], fill=(18, 16, 14, 255))
    draw.ellipse([66 * S, 5 * S, 73 * S, 12 * S], fill=(222, 182, 70, 255))


def torso(cloth, dark, light):
    img = Image.new('RGBA', (W * S, H * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    body = [(56, 42), (83, 42), (96, 48), (118, 55), (128, 64), (133, 90), (134, 133), (5, 133), (6, 90), (12, 63), (22, 55), (44, 48)]
    d.polygon([(x * S, y * S) for x, y in body], fill=cloth)
    # Arms: sleeves a shade darker, with seams.
    d.polygon([(x * S, y * S) for x, y in [(12, 63), (24, 60), (30, 133), (5, 133), (6, 90)]], fill=dark)
    d.polygon([(x * S, y * S) for x, y in [(128, 64), (115, 60), (109, 133), (134, 133), (133, 90)]], fill=dark)
    d.line([(24 * S, 60 * S), (30 * S, 133 * S)], fill=light, width=S)
    d.line([(115 * S, 60 * S), (109 * S, 133 * S)], fill=light, width=S)
    return img, d


def shade(img):
    px = np.asarray(img).astype(float)
    h, w = px.shape[:2]
    ys, xs = np.mgrid[0:h, 0:w]
    # Light from the upper left, rounder towards the middle of the chest, folds as soft noise.
    across = 1 - np.abs(xs / w - 0.47) * 0.9
    down = 1 - (ys / h) * 0.25
    rng = np.random.default_rng(7)
    noise = Image.fromarray((rng.random((h // 6 + 1, w // 6 + 1)) * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC)
    folds = (np.asarray(noise).astype(float) / 255.0 - 0.5) * 0.18
    factor = (0.62 + 0.5 * across * down + folds).clip(0.4, 1.25)
    px[..., :3] = (px[..., :3] * factor[..., None]).clip(0, 255)
    return Image.fromarray(px.astype(np.uint8), 'RGBA')


def finish(img, crowned, extra=None):
    small = shade(img).resize((W, H), Image.LANCZOS)
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    out.alpha_composite(small)
    out.alpha_composite(head(crowned))
    if extra:
        hat = Image.new('RGBA', (W * S, H * S), (0, 0, 0, 0))
        extra(ImageDraw.Draw(hat))
        out.alpha_composite(hat.resize((W, H), Image.LANCZOS))
    px = np.asarray(out).copy()
    px[..., 3] = np.where(px[..., 3] > 110, 255, 0)
    return Image.fromarray(px, 'RGBA')


def hair_layer(img):
    d = ImageDraw.Draw(img)
    hair(d)


def robe():
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    out.alpha_composite(layer(brocade, Image.new('L', (W, H), 255)))
    out.alpha_composite(head(True))
    return out


def suit():
    img, d = torso((34, 40, 62, 255), (26, 30, 48, 255), (52, 60, 88, 255))
    hair_layer(img)
    # Shirt and tie below the beard, between the lapels.
    d.polygon([(x * S, y * S) for x, y in [(60, 50), (79, 50), (70, 96)]], fill=(236, 236, 230, 255))
    d.polygon([(x * S, y * S) for x, y in [(67, 52), (73, 52), (74, 88), (70, 96), (66, 88)]], fill=(156, 26, 34, 255))
    d.polygon([(x * S, y * S) for x, y in [(56, 44), (60, 50), (70, 96), (63, 92), (50, 54)]], fill=(44, 52, 80, 255))
    d.polygon([(x * S, y * S) for x, y in [(83, 44), (79, 50), (70, 96), (77, 92), (89, 54)]], fill=(44, 52, 80, 255))
    for y in (104, 118):
        d.ellipse([68 * S, y * S, 72 * S, (y + 4) * S], fill=(16, 18, 28, 255))
    d.polygon([(x * S, y * S) for x, y in [(92, 70), (102, 68), (103, 72), (92, 74)]], fill=(236, 236, 230, 255))
    return finish(img, False)


def tunic():
    img, d = torso((150, 146, 128, 255), (122, 118, 102, 255), (178, 174, 156, 255))
    hair_layer(img)
    d.line([(70 * S, 50 * S), (70 * S, 133 * S)], fill=(110, 106, 92, 255), width=S)
    for y in range(64, 130, 13):
        d.ellipse([68 * S, y * S, 72 * S, (y + 4) * S], fill=(196, 168, 90, 255))
    for x0 in (40, 84):
        d.rectangle([x0 * S, 70 * S, (x0 + 18) * S, 86 * S], outline=(110, 106, 92, 255), width=S)
        d.polygon([(x0 * S, 70 * S), ((x0 + 18) * S, 70 * S), ((x0 + 18) * S, 75 * S), ((x0 + 9) * S, 77 * S), (x0 * S, 75 * S)], fill=(134, 130, 112, 255))
    # A red star on the chest.
    cx, cy, r = 96 * S, 62 * S, 4 * S
    star = [(cx + r * np.cos(np.pi / 2 + i * np.pi / 5) * (1 if i % 2 == 0 else 0.45), cy - r * np.sin(np.pi / 2 + i * np.pi / 5) * (1 if i % 2 == 0 else 0.45)) for i in range(10)]
    d.polygon(star, fill=(200, 30, 30, 255))
    return finish(img, False)


def field_uniform():
    olive, dark, light = (92, 96, 52, 255), (72, 76, 40, 255), (116, 120, 70, 255)
    img, d = torso(olive, dark, light)
    d.line([(70 * S, 50 * S), (70 * S, 133 * S)], fill=dark, width=S)
    for y in range(64, 130, 13):
        d.ellipse([68 * S, y * S, 72 * S, (y + 4) * S], fill=(214, 176, 70, 255))
    # Shoulder boards, belt with a cross strap, ribbons.
    for x0, x1 in ((20, 44), (95, 119)):
        d.polygon([(x0 * S, 54 * S), (x1 * S, 49 * S), (x1 * S, 53 * S), (x0 * S, 58 * S)], fill=(210, 170, 60, 255))
    d.rectangle([8 * S, 108 * S, 131 * S, 114 * S], fill=(92, 52, 24, 255))
    d.rectangle([66 * S, 107 * S, 74 * S, 115 * S], fill=(214, 176, 70, 255))
    d.line([(100 * S, 52 * S), (40 * S, 110 * S)], fill=(92, 52, 24, 255), width=3 * S)
    colours = [(170, 30, 30), (30, 60, 150), (220, 190, 60), (30, 120, 60), (200, 200, 200), (150, 30, 120)]
    for i, c in enumerate(colours):
        x, y = 84 + (i % 3) * 6, 64 + (i // 3) * 4
        d.rectangle([x * S, y * S, (x + 5) * S, (y + 3) * S], fill=c + (255,))
    return finish(img, False, lambda dr: cap(dr, olive, (120, 30, 30, 255)))


def parade_uniform():
    navy, dark, light = (30, 36, 92, 255), (22, 26, 70, 255), (52, 60, 130, 255)
    img, d = torso(navy, dark, light)
    for y in range(62, 130, 12):
        d.ellipse([66 * S, y * S, 70 * S, (y + 4) * S], fill=(230, 196, 80, 255))
        d.ellipse([74 * S, y * S, 78 * S, (y + 4) * S], fill=(230, 196, 80, 255))
    # Gold epaulettes with fringe, a sash across the chest, a star of an order.
    for x0, x1 in ((16, 44), (95, 123)):
        d.ellipse([x0 * S, 47 * S, x1 * S, 59 * S], fill=(226, 186, 64, 255))
        for x in range(x0 + 2, x1 - 1, 3):
            d.line([(x * S, 56 * S), (x * S, 64 * S)], fill=(200, 160, 50, 255), width=S)
    d.polygon([(x * S, y * S) for x, y in [(96, 50), (110, 56), (40, 124), (26, 116)]], fill=(60, 120, 200, 255))
    cx, cy, r = 100 * S, 82 * S, 6 * S
    star = [(cx + r * np.cos(i * np.pi / 4) * (1 if i % 2 == 0 else 0.5), cy + r * np.sin(i * np.pi / 4) * (1 if i % 2 == 0 else 0.5)) for i in range(8)]
    d.polygon(star, fill=(236, 236, 236, 255))
    d.rectangle([8 * S, 110 * S, 131 * S, 116 * S], fill=(214, 176, 70, 255))
    return finish(img, True)


variants = {
    'casimir_robe': robe(),
    'casimir_parade': parade_uniform(),
    'casimir_field': field_uniform(),
    'casimir_tunic': tunic(),
    'casimir_suit': suit(),
}
for name, image in variants.items():
    image.save(name + '_body.png')

sheet = Image.new('RGB', (len(variants) * (W * 3 + 10), H * 3), (0, 120, 0))
for i, image in enumerate(variants.values()):
    bg = Image.new('RGBA', (W, H), (0, 120, 0, 255))
    bg.alpha_composite(image)
    sheet.paste(bg.convert('RGB').resize((W * 3, H * 3), Image.NEAREST), (i * (W * 3 + 10), 0))
sheet.save('casimir_variants.png')
