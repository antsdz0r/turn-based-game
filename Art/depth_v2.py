"""Battle depth layers: foreground silhouettes per region, a light shaft and a vignette.
Foregrounds are 640x360 (drawn 2x in game like the backgrounds) and stay at the screen edges and top,
so they frame the battle without covering the units. Run from the Art folder: python depth_v2.py"""
import math, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from style_kit import hx

W, H = 640, 360


def pillar(d, x0, w, top, col, edge, capital=True):
    d.rectangle([x0, top, x0 + w, H], fill=col)
    d.line([(x0 + (w - 2 if x0 < W / 2 else 1), top), (x0 + (w - 2 if x0 < W / 2 else 1), H)], fill=edge)   # inner rim
    if capital:
        d.rectangle([x0 - 4, top + 40, x0 + w + 4, top + 50], fill=col)
        d.line([(x0 - 4, top + 50), (x0 + w + 4, top + 50)], fill=edge)


def chain(d, x, length, col, edge):
    for y in range(0, length, 7):
        if (y // 7) % 2 == 0: d.ellipse([x - 2, y, x + 2, y + 7], outline=col, width=1)
        else: d.line([(x, y), (x, y + 7)], fill=col, width=2)
    d.point((x - 1, length - 2), fill=edge)


def crypt():
    img = Image.new('RGBA', (W, H)); d = ImageDraw.Draw(img); r = random.Random(1)
    col, edge = hx('0b0910') + (255,), hx('433758') + (255,)
    pillar(d, 0, 16, 0, col, edge); pillar(d, W - 16, 16, 0, col, edge)
    for x, n in ((70, 70), (84, 46), (560, 80), (574, 52), (300, 30)): chain(d, x, n, col, edge)
    # Cobwebs in the top corners.
    for cx, sgn in ((16, 1), (W - 16, -1)):
        for k in range(5): d.line([(cx, 0), (cx + sgn * (20 + k * 8), 26 - k * 5)], fill=hx('554a6d') + (140,))
        for rad in (10, 18, 26): d.arc([cx - rad, -rad, cx + rad, rad], 0 if sgn > 0 else 90, 90 if sgn > 0 else 180, fill=hx('554a6d') + (120,))
    return img


def drowned():
    img = Image.new('RGBA', (W, H)); d = ImageDraw.Draw(img); r = random.Random(2)
    col, edge = hx('0b0910') + (255,), hx('2a5560') + (255,)
    pillar(d, 0, 14, 0, col, edge, capital=False); pillar(d, W - 14, 14, 0, col, edge, capital=False)
    for _ in range(26):                                                       # hanging kelp
        x = r.choice([r.randint(10, 160), r.randint(480, 630)]); length = r.randint(20, 110)
        pts = [(x + math.sin(y / 9 + x) * 3, y) for y in range(0, length, 4)]
        d.line(pts, fill=hx('1d3340') + (255,), width=r.choice([2, 3]))
        d.point(pts[-1], fill=hx('3f8a8a') + (255,))
    for _ in range(12):                                                       # drips
        x, y = r.randint(20, W - 20), r.randint(0, 40)
        d.line([(x, y), (x, y + 3)], fill=hx('6fd0c0') + (180,))
    return img


def frost():
    img = Image.new('RGBA', (W, H)); d = ImageDraw.Draw(img); r = random.Random(3)
    col, edge = hx('141019') + (255,), hx('b0aec8') + (255,)
    pillar(d, 0, 14, 0, col, edge); pillar(d, W - 14, 14, 0, col, edge)
    x = 14
    while x < W - 14:                                                          # a fringe of icicles
        w = r.randint(4, 9); length = r.randint(6, 46 if (x < 170 or x > 470) else 20)
        d.polygon([(x, 0), (x + w, 0), (x + w // 2, length)], fill=hx('1d3340') + (255,))
        d.line([(x + 1, 0), (x + w // 2, length - 2)], fill=hx('b8f5e4') + (200,))
        x += w + r.randint(0, 6)
    return img


def storm():
    img = Image.new('RGBA', (W, H)); d = ImageDraw.Draw(img); r = random.Random(4)
    col, edge = hx('0b0910') + (255,), hx('8a86a6') + (255,)
    pillar(d, 0, 18, 0, col, edge); pillar(d, W - 18, 18, 0, col, edge)
    for x in (6, W - 10):                                                      # cracks with a lightning glint
        y = 60
        for _ in range(6):
            nx, ny = x + r.randint(-2, 2), y + r.randint(10, 22)
            d.line([(x, y), (nx, ny)], fill=hx('d8d6e6') + (200,)); x, y = nx, ny
    for x, n in ((120, 60), (520, 90), (534, 60)): chain(d, x, n, col, edge)
    return img


def cinder():
    img = Image.new('RGBA', (W, H)); d = ImageDraw.Draw(img); r = random.Random(5)
    col, edge = hx('0b0910') + (255,), hx('a63d25') + (255,)
    pillar(d, 0, 16, 0, col, edge); pillar(d, W - 16, 16, 0, col, edge)
    for x in (4, 8, W - 12, W - 6):                                            # glowing ember cracks in the pillars
        y = r.randint(40, 120)
        for _ in range(5):
            ny = y + r.randint(8, 18); d.line([(x, y), (x + r.randint(-2, 2), ny)], fill=hx('f28a3a') + (255,)); y = ny
    x = 16
    while x < W - 16:                                                          # stalactites of slag
        w = r.randint(6, 14); length = r.randint(4, 34 if (x < 170 or x > 470) else 12)
        d.polygon([(x, 0), (x + w, 0), (x + w // 2, length)], fill=col)
        d.point((x + w // 2, length - 1), fill=hx('d85f2a') + (255,))
        x += w + r.randint(2, 10)
    for x, n in ((96, 80), (548, 70)):
        chain(d, x, n, col, edge)
        d.rectangle([x - 6, n, x + 6, n + 12], outline=col, width=2)         # a hanging cage
    return img


def shaft():
    """A soft vertical light beam (white; tinted in game, drawn with additive blending)."""
    w, h = 64, 256
    x = np.linspace(-1, 1, w)[None, :]; y = np.linspace(0, 1, h)[:, None]
    a = np.clip(1 - np.abs(x) ** 1.6, 0, 1) * (1 - y) ** 1.3
    img = np.zeros((h, w, 4), np.uint8); img[..., :3] = 255; img[..., 3] = (a * 255).astype(np.uint8)
    return Image.fromarray(img, 'RGBA')


def vignette():
    w, h = 320, 180
    yy, xx = np.mgrid[0:h, 0:w]
    v = np.clip((np.hypot((xx - w / 2) / (w / 2), (yy - h / 2) / (h / 2)) - 0.55) / 0.75, 0, 1) ** 1.6
    img = np.zeros((h, w, 4), np.uint8); img[..., 3] = (v * 190).astype(np.uint8)
    return Image.fromarray(img, 'RGBA').filter(ImageFilter.GaussianBlur(2))


if __name__ == "__main__":
    for name, fn in (("crypt", crypt), ("drowned", drowned), ("frost", frost), ("storm", storm), ("cinder", cinder)):
        fn().resize((W * 2, H * 2), Image.NEAREST).save(f"Backgrounds/{name}_fg.png")
    shaft().save("FX/shaft.png")
    vignette().save("FX/vignette.png")
    print("depth layers written")
