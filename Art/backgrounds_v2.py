"""Lit region backgrounds (640x360 native, saved x2). Light is baked: ambient, torches, shafts, bloom, fog,
then everything is dithered into the Ashen palette."""
import math, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from style_kit import hx, quantize_rgb

W, H = 640, 360
FLOOR = 196                 # wall/floor seam (the game's battle baseline sits at y=218)
TORCHES = [(128, 86), (512, 86)]   # where the game draws its torch props (0.2 / 0.8 of the width)
yy, xx = np.mgrid[0:H, 0:W]


def bricks(alb, base, mortar, rows=9, cols=18, top=0, bottom=FLOOR, jitter=0.3, seed=1):
    for y in range(top, bottom):
        row = y // rows
        for x in range(W):
            off = (row % 2) * (cols // 2)
            bx = (x + off) // cols
            m = (y % rows == 0) or ((x + off) % cols == 0)
            rnd = ((bx * 73856093) ^ (row * 19349663) ^ seed) % 1000 / 1000.0
            alb[y, x] = mortar if m else base * (1 - jitter / 2 + jitter * rnd)


def arch(d, cx, top, w, bottom, fill):
    d.rectangle([cx - w // 2, top + w // 2, cx + w // 2, bottom], fill=fill)
    d.pieslice([cx - w // 2, top, cx + w // 2, top + w], 180, 360, fill=fill)
    d.polygon([(cx - w // 2 + 6, top + w // 2), (cx, top - 12), (cx + w // 2 - 6, top + w // 2)], fill=fill)


def add_point(light, x, y, color, radius, power):
    r = np.hypot(xx - x, (yy - y) * 1.15)
    light += np.array(color)[None, None] * (power * np.clip(1 - r / radius, 0, 1) ** 2)[..., None]


def shaft_mask(cx, cy, spread=80, start_half=18, fade=0.55, drift=30):
    s = np.zeros((H, W), np.float32)
    for y in range(int(cy), H):
        t = (y - cy) / (H - cy)
        half = start_half + spread * t
        center = cx + drift * t
        s[y] = np.clip(1 - np.abs(xx[y] - center) / half, 0, 1) * (fade - 0.35 * t)
    return np.clip(s, 0, 1)


def finish(scene, shaft, shaft_color, fog_color, fog_y=FLOOR + 6, bloom=1.3, sparks=None):
    scene += np.clip(1 - np.abs(yy - fog_y) / 22.0, 0, 1)[..., None] * np.array(fog_color)
    scene += (shaft ** 1.5)[..., None] * np.array(shaft_color)
    bright = np.clip(scene - 190, 0, 255)
    blur = np.asarray(Image.fromarray(bright.clip(0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(9)), np.float32)
    scene += blur * bloom
    px = quantize_rgb(np.clip(scene, 0, 255))
    rnd = random.Random(5)
    for _ in range(60 if sparks else 0):
        ex, ey = rnd.randrange(W), rnd.randrange(30, FLOOR + 40)
        px[ey, ex] = hx(rnd.choice(sparks))
    return Image.fromarray(px.astype(np.uint8))


def floor_tiles(alb, base, line, start=FLOOR):
    for y in range(start, H):
        t = (y - start) / (H - start)
        alb[y, :] = np.array(base) * (0.65 + 0.55 * t)
    img = Image.fromarray(alb.clip(0, 255).astype(np.uint8)); d = ImageDraw.Draw(img)
    y, step = start, 5
    while y < H:
        d.line([0, y, W, y], fill=line); y += step; step = int(step * 1.45) + 1
    for k in range(-14, 15):
        d.line([320 + k * 11, start, 320 + k * 64, H], fill=line)
    return np.asarray(img, np.float32).copy()


def torch_glow_baked(scene, x, y):
    """The game draws the sconce + flame; we only bake a warm halo onto the wall around it."""
    pass


# ═════════════════════════ The Ashen Crypt ═════════════════════════
def crypt():
    alb = np.zeros((H, W, 3), np.float32)
    bricks(alb, np.array(hx('433758'), np.float32), np.array(hx('1d1726'), np.float32))
    for y in range(FLOOR): alb[y] *= 0.5 + 0.5 * (y / FLOOR)
    img = Image.fromarray(alb.clip(0, 255).astype(np.uint8)); d = ImageDraw.Draw(img)
    for cx in (62, 578):
        arch(d, cx, 62, 96, FLOOR, hx('141019')); arch(d, cx, 72, 78, FLOOR, hx('0b0910'))
    arch(d, 320, 24, 150, FLOOR, hx('1d1726')); arch(d, 320, 36, 128, FLOOR, hx('141019'))
    for px_ in (222, 418):
        d.rectangle([px_ - 16, 0, px_ + 16, FLOOR], fill=hx('554a6d'))
        for k in range(-12, 13, 6): d.line([px_ + k, 0, px_ + k, FLOOR], fill=hx('433758'))
        d.rectangle([px_ - 20, FLOOR - 12, px_ + 20, FLOOR], fill=hx('6b6386'))
        d.rectangle([px_ - 20, 16, px_ + 20, 25], fill=hx('6b6386'))
    # banners between arches
    for bx in (170, 470):
        d.rectangle([bx - 12, 20, bx + 12, 70], fill=hx('74291f'))
        d.polygon([(bx - 12, 70), (bx, 80), (bx + 12, 70)], fill=hx('74291f'))
        d.line([bx, 30, bx, 62], fill=hx('ffb85a'), width=2); d.line([bx - 7, 40, bx + 7, 40], fill=hx('ffb85a'), width=2)
    alb = np.asarray(img, np.float32).copy()
    emis = np.zeros_like(alb)
    # rose window
    cx, cy, r = 320, 74, 36
    dist = np.hypot(xx - cx, yy - cy); ang = (np.arctan2(yy - cy, xx - cx) + math.pi) / (2 * math.pi)
    win = dist < r
    glass = [hx(c) for c in ('ffb85a', 'd85f2a', '6fd0c0', 'a8406a', 'ffe09a', '3f8a8a')]
    seg = ((ang * 12).astype(int) + (dist < r * 0.5) * 3) % len(glass)
    lead = (np.abs((ang * 12) % 1 - 0.5) > 0.44) | (np.abs(dist - r * 0.5) < 1.2) | (dist > r - 2)
    for i, c in enumerate(glass): emis[win & (seg == i) & ~lead] = np.array(c) * 0.95
    alb[win] = np.array(hx('141019'))
    alb = floor_tiles(alb, hx('352a45'), hx('1d1726'))
    light = np.zeros_like(alb) + np.array([0.5, 0.5, 0.72])
    for tx, ty in TORCHES: add_point(light, tx, ty - 10, (1.0, 0.66, 0.38), 270, 1.25)
    add_point(light, 320, 250, (1.0, 0.8, 0.5), 220, 0.35)
    shaft = shaft_mask(cx, cy)
    light += shaft[..., None] * np.array([1.0, 0.85, 0.6])
    scene = alb * light + emis
    img = finish(scene, shaft, [60, 48, 30], [16, 16, 26], sparks=['ffb85a', 'f28a3a', 'ffe09a'])
    d = ImageDraw.Draw(img)
    for cxh, ln in ((24, 110), (612, 80), (636, 140)):           # hanging chains
        for k in range(0, ln, 6): d.ellipse([cxh - 2, k, cxh + 2, k + 7], outline=hx('0b0910'), width=2)
    return img


# ═════════════════════════ The Drowned Halls ═════════════════════════
def drowned():
    alb = np.zeros((H, W, 3), np.float32)
    bricks(alb, np.array(hx('2a5560'), np.float32) * 0.8, np.array(hx('141019'), np.float32), seed=7)
    for y in range(FLOOR): alb[y] *= 0.45 + 0.55 * (y / FLOOR)
    img = Image.fromarray(alb.clip(0, 255).astype(np.uint8)); d = ImageDraw.Draw(img)
    for cx in (62, 578):
        arch(d, cx, 62, 96, FLOOR, hx('141019')); arch(d, cx, 72, 78, FLOOR, hx('0b0910'))
    arch(d, 320, 28, 160, FLOOR, hx('141019'))
    for px_ in (222, 418):
        d.rectangle([px_ - 16, 0, px_ + 16, FLOOR], fill=hx('2a5560'))
        for k in range(-12, 13, 6): d.line([px_ + k, 0, px_ + k, FLOOR], fill=hx('1d3340'))
        # barnacles / coral on pillars
        rnd = random.Random(px_)
        for _ in range(18):
            bx, by = px_ + rnd.randint(-15, 15), rnd.randint(FLOOR - 70, FLOOR)
            d.ellipse([bx - 2, by - 2, bx + 2, by + 2], fill=hx(rnd.choice(['6a2848', 'a8406a', '8a86a6'])))
    # seaweed curtains from the ceiling
    rnd = random.Random(3)
    for _ in range(26):
        sx = rnd.randrange(W); ln = rnd.randint(20, 90); ph = rnd.random() * 6
        pts = [(sx + 4 * math.sin(ph + k / 7), k) for k in range(0, ln, 3)]
        d.line(pts, fill=hx(rnd.choice(['233a22', '3d6230'])), width=3)
    alb = np.asarray(img, np.float32).copy()
    emis = np.zeros_like(alb)
    # broken ceiling hole: pale moonlight from above
    cx, cy = 320, 60
    ang = np.arctan2(yy - cy, xx - cx)
    jag = 20 + 5 * np.sin(ang * 7) + 3 * np.sin(ang * 13 + 1)
    rr = np.hypot((xx - cx) / 1.5, yy - cy)
    hole = rr < jag
    emis[hole] = np.array(hx('b8f5e4')) * np.clip(1.1 - rr[hole][:, None] / 28, 0.5, 1)
    alb[hole] = 0
    # flooded floor: dark water with ripples and reflections
    for y in range(FLOOR, H):
        t = (y - FLOOR) / (H - FLOOR)
        alb[y, :] = np.array(hx('1d3340')) * (0.7 + 0.6 * t)
    img = Image.fromarray(alb.clip(0, 255).astype(np.uint8)); d = ImageDraw.Draw(img)
    rnd = random.Random(9)
    for _ in range(140):
        y = rnd.randrange(FLOOR + 2, H); x = rnd.randrange(W); ln = rnd.randint(6, 30)
        d.line([x, y, x + ln, y], fill=hx(rnd.choice(['2a5560', '3f8a8a'])))
    alb = np.asarray(img, np.float32).copy()
    # glowing crystals (lanterns) replace torches here
    for (tx, ty) in TORCHES:                     # crystal clusters growing from the wall
        for k, (ox, h) in enumerate(((-6, 8), (-2, 16), (3, 12), (7, 7), (0, 6))):
            for j in range(h):
                wdt = max(1, 2 - j // 6)
                emis[ty + 8 - j, tx + ox - wdt:tx + ox + wdt] = np.array(hx('b8f5e4' if j > h - 4 else '6fd0c0'))
    light = np.zeros_like(alb) + np.array([0.42, 0.58, 0.72])
    for tx, ty in TORCHES: add_point(light, tx, ty, (0.4, 1.0, 0.9), 220, 1.4)
    shaft = shaft_mask(cx, cy, spread=70, start_half=16, drift=-20)
    light += shaft[..., None] * np.array([0.6, 1.0, 1.0])
    # reflection of the shaft on the water
    refl = np.clip(1 - np.abs(xx - 300) / 60, 0, 1) * (yy > FLOOR) * 0.5 * ((yy % 3) == 0)
    light += refl[..., None] * np.array([0.6, 1.0, 1.0])
    scene = alb * light + emis
    return finish(scene, shaft, [30, 60, 60], [10, 26, 30], fog_y=FLOOR + 2, sparks=['6fd0c0', 'b8f5e4'])


# ═════════════════════════ The Cinder Throne ═════════════════════════
def cinder():
    alb = np.zeros((H, W, 3), np.float32)
    bricks(alb, np.array(hx('282033'), np.float32), np.array(hx('0b0910'), np.float32), rows=11, cols=22, seed=11)
    for y in range(FLOOR): alb[y] *= 0.6 + 0.4 * (y / FLOOR)
    img = Image.fromarray(alb.clip(0, 255).astype(np.uint8)); d = ImageDraw.Draw(img)
    for cx in (62, 578):
        arch(d, cx, 62, 96, FLOOR, hx('141019')); arch(d, cx, 72, 78, FLOOR, hx('0b0910'))
    arch(d, 320, 20, 176, FLOOR, hx('141019'))
    for px_ in (218, 422):
        d.rectangle([px_ - 18, 0, px_ + 18, FLOOR], fill=hx('1d1726'))
        d.rectangle([px_ - 22, FLOOR - 12, px_ + 22, FLOOR], fill=hx('282033'))
    alb = np.asarray(img, np.float32).copy()
    emis = np.zeros_like(alb)
    # lava cracks running down the walls
    rnd = random.Random(21)
    crack = Image.new('L', (W, H), 0); cd = ImageDraw.Draw(crack)
    for _ in range(12):
        x, y = rnd.randrange(W), rnd.randrange(0, 90)
        pts = [(x, y)]
        for _ in range(rnd.randint(4, 9)):
            x += rnd.randint(-8, 8); y += rnd.randint(6, 20); pts.append((x, y))
        cd.line(pts, fill=255, width=2)
    cm = (np.asarray(crack) > 0) & (yy < FLOOR)
    emis[cm] = np.array(hx('f28a3a'))
    # far throne in the central arch, backlit by a lava fall
    lava = (np.abs(xx - 320) < 60) & (yy > 40) & (yy < FLOOR)
    noise = np.sin(yy * 0.09 + np.sin(xx * 0.21) * 2.0) * 0.5 + np.sin(xx * 0.37 + yy * 0.02) * 0.3
    heat = np.clip(0.55 + noise * 0.45 - np.abs(xx - 320) / 140, 0, 1)
    lava_col = np.array(hx('d85f2a')) + (np.array(hx('ffe09a')) - np.array(hx('d85f2a'))) * heat[..., None]
    emis[lava] = lava_col[lava] * 0.95
    img = Image.fromarray(alb.clip(0, 255).astype(np.uint8)); d = ImageDraw.Draw(img)
    throne = Image.new('L', (W, H), 0); td = ImageDraw.Draw(throne)
    td.polygon([(296, FLOOR - 10), (296, 90), (304, 80), (312, 92), (320, 74), (328, 92), (336, 80), (344, 90), (344, FLOOR - 10)], fill=255)
    td.rectangle([270, FLOOR - 14, 370, FLOOR], fill=255)
    tm = np.asarray(throne) > 0
    emis[tm] = 0
    alb[tm] = np.array(hx('0b0910'))
    # obsidian floor with glowing seams
    alb = floor_tiles(alb, hx('1d1726'), hx('0b0910'))
    seam = np.zeros((H, W), bool)
    rnd = random.Random(4)
    fl = Image.new('L', (W, H), 0); fd = ImageDraw.Draw(fl)
    for _ in range(12):
        x = rnd.randrange(W); y = rnd.randrange(FLOOR + 10, H)
        fd.line([x, y, x + rnd.randint(-40, 40), y + rnd.randint(4, 20)], fill=255, width=1)
    emis[np.asarray(fl) > 0] = np.array(hx('d85f2a'))
    light = np.zeros_like(alb) + np.array([0.6, 0.42, 0.42])
    for tx, ty in TORCHES: add_point(light, tx, ty - 10, (1.0, 0.55, 0.25), 240, 1.1)
    add_point(light, 320, 150, (1.0, 0.5, 0.2), 300, 1.1)       # lava fall lights the hall
    add_point(light, 320, 330, (1.0, 0.4, 0.15), 260, 0.6)      # heat from below
    shaft = np.zeros((H, W), np.float32)
    scene = alb * light + emis
    return finish(scene, shaft, [0, 0, 0], [40, 16, 10], fog_y=FLOOR + 8, bloom=1.5, sparks=['ffb85a', 'f28a3a', 'd85f2a', 'ffe09a'])


# ═════════════════════════ The Frozen Ossuary ═════════════════════════
def frost():
    alb = np.zeros((H, W, 3), np.float32)
    bricks(alb, np.array(hx('6b6386'), np.float32) * 0.85, np.array(hx('352a45'), np.float32), seed=31)
    for y in range(FLOOR): alb[y] *= 0.55 + 0.45 * (y / FLOOR)
    img = Image.fromarray(alb.clip(0, 255).astype(np.uint8)); d = ImageDraw.Draw(img)
    for cx in (62, 578):
        arch(d, cx, 62, 96, FLOOR, hx('1d1726')); arch(d, cx, 72, 78, FLOOR, hx('141019'))
    arch(d, 320, 26, 150, FLOOR, hx('1d3340'))
    # skull-lined ossuary niches
    rnd = random.Random(8)
    for px_ in (222, 418):
        d.rectangle([px_ - 16, 0, px_ + 16, FLOOR], fill=hx('8a86a6'))
        for yy_ in range(20, FLOOR - 16, 14):
            d.ellipse([px_ - 6, yy_, px_ + 6, yy_ + 10], fill=hx('d8d6e6'))
            d.point((px_ - 2, yy_ + 4), fill=hx('1d1726')); d.point((px_ + 2, yy_ + 4), fill=hx('1d1726'))
    # icicles hanging from the ceiling
    for _ in range(70):
        x = rnd.randrange(W); ln = rnd.randint(4, 22)
        d.polygon([(x - 2, 0), (x + 2, 0), (x, ln)], fill=hx(rnd.choice(['b8f5e4', 'd8d6e6', 'f4f2fa'])))
    alb = np.asarray(img, np.float32).copy()
    emis = np.zeros_like(alb)
    # a frozen waterfall in the central arch, glowing from within
    fall = (np.abs(xx - 320) < 54) & (yy > 50) & (yy < FLOOR)
    streak = np.sin(xx * 0.7) * 0.5 + 0.5
    col = np.array(hx('6fd0c0')) * (0.55 + 0.35 * streak[..., None]) + np.array(hx('f4f2fa')) * (0.3 * (streak[..., None] > 0.85))
    emis[fall] = col[fall] * 0.8
    alb[fall] = 0
    # snowy floor
    alb = floor_tiles(alb, hx('8a86a6'), hx('554a6d'))
    img = Image.fromarray(alb.clip(0, 255).astype(np.uint8)); d = ImageDraw.Draw(img)
    for _ in range(40):
        x = rnd.randrange(W); y = rnd.randrange(FLOOR, H); w = rnd.randint(12, 40)
        d.ellipse([x - w, y - 3, x + w, y + 3], fill=hx(rnd.choice(['d8d6e6', 'b0aec8'])))
    alb = np.asarray(img, np.float32).copy()
    light = np.zeros_like(alb) + np.array([0.5, 0.6, 0.78])
    for tx, ty in TORCHES: add_point(light, tx, ty - 10, (1.0, 0.66, 0.38), 230, 1.0)
    add_point(light, 320, 140, (0.5, 1.0, 1.0), 260, 0.9)
    shaft = shaft_mask(320, 40, spread=60, start_half=14, drift=10)
    light += shaft[..., None] * np.array([0.7, 1.0, 1.0])
    scene = alb * light + emis
    return finish(scene, shaft, [40, 60, 70], [24, 30, 40], sparks=['f4f2fa', 'b8f5e4'])


# ═════════════════════════ The Storm Spire ═════════════════════════
def storm():
    alb = np.zeros((H, W, 3), np.float32)
    bricks(alb, np.array(hx('352a45'), np.float32), np.array(hx('0b0910'), np.float32), rows=10, cols=20, seed=41)
    for y in range(FLOOR): alb[y] *= 0.5 + 0.5 * (y / FLOOR)
    emis = np.zeros_like(alb)
    # three tall windows open onto the storm
    rnd = random.Random(12)
    for cx, w in ((120, 70), (320, 110), (520, 70)):
        win = (np.abs(xx - cx) < w / 2) & (yy > 40) & (yy < FLOOR - 20)
        top = (np.hypot(xx - cx, yy - 40) < w / 2) & (yy <= 40)
        m = win | top
        cloud = 0.5 + 0.5 * np.sin(xx * 0.05 + yy * 0.08) * np.sin(xx * 0.013 - yy * 0.05)
        sky = np.array(hx('282033')) + (np.array(hx('6b6386')) - np.array(hx('282033'))) * cloud[..., None]
        emis[m] = sky[m] * 0.9
        alb[m] = 0
        # rain streaks inside the windows
        for _ in range(int(w * 1.2)):
            x = cx + rnd.uniform(-w / 2, w / 2); y = rnd.uniform(20, FLOOR - 30)
            for k in range(8):
                xi, yi = int(x - k * 0.4), int(y + k)
                if 0 <= xi < W and 0 <= yi < H and m[yi, xi]: emis[yi, xi] = np.array(hx('b0aec8'))
    # a frozen-in-time lightning bolt in the middle window
    bolt = Image.new('L', (W, H), 0); bd = ImageDraw.Draw(bolt)
    pts = [(330, 24)]; x, y = 330, 24
    while y < FLOOR - 30:
        x += rnd.randint(-10, 10); y += rnd.randint(8, 16); pts.append((x, y))
    bd.line(pts, fill=255, width=2)
    bd.line([pts[3], (pts[3][0] + 24, pts[3][1] + 30)], fill=255, width=1)
    bm = np.asarray(bolt) > 0
    emis[bm] = np.array(hx('fff6d8'))
    img = Image.fromarray(alb.clip(0, 255).astype(np.uint8)); d = ImageDraw.Draw(img)
    for px_ in (222, 418):
        d.rectangle([px_ - 14, 0, px_ + 14, FLOOR], fill=hx('282033'))
        for yy_ in range(10, FLOOR, 24): d.line([px_ - 14, yy_, px_ + 14, yy_], fill=hx('433758'))
    alb = np.asarray(img, np.float32).copy()
    # wet, reflective floor
    alb = floor_tiles(alb, hx('282033'), hx('141019'))
    light = np.zeros_like(alb) + np.array([0.48, 0.48, 0.66])
    for tx, ty in TORCHES: add_point(light, tx, ty - 10, (1.0, 0.62, 0.32), 220, 1.0)
    add_point(light, 330, 120, (0.75, 0.8, 1.0), 300, 0.9)
    refl = np.clip(1 - np.abs(xx - 330) / 70, 0, 1) * (yy > FLOOR) * 0.6 * ((yy % 3) == 0)
    light += refl[..., None] * np.array([0.8, 0.85, 1.0])
    shaft = shaft_mask(320, 60, spread=90, start_half=40, fade=0.35)
    light += shaft[..., None] * np.array([0.7, 0.75, 1.0])
    scene = alb * light + emis
    return finish(scene, shaft, [30, 30, 50], [16, 16, 28], bloom=1.5, sparks=['d8d6e6'])


if __name__ == '__main__':
    for name, fn in (('crypt', crypt), ('drowned', drowned), ('frost', frost), ('storm', storm), ('cinder', cinder)):
        img = fn()
        img.resize((W * 2, H * 2), Image.NEAREST).save(f'Backgrounds/{name}.png')
        print('saved', name)
