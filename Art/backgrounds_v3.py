"""Backgrounds for the two new regions, in the same lit style as backgrounds_v2 (and their foreground layers).
Run from the Art folder: python backgrounds_v3.py"""
import math, random
import numpy as np
from PIL import Image, ImageDraw
from style_kit import hx
from backgrounds_v2 import W, H, FLOOR, TORCHES, xx, yy, bricks, arch, add_point, shaft_mask, finish, floor_tiles


# ═════════════════════════ The Rotting Garden ═════════════════════════
def garden():
    alb = np.zeros((H, W, 3), np.float32)
    bricks(alb, np.array(hx('233a22'), np.float32) * 1.1, np.array(hx('141019'), np.float32), seed=21)
    for y in range(FLOOR): alb[y] *= 0.45 + 0.55 * (y / FLOOR)
    img = Image.fromarray(alb.clip(0, 255).astype(np.uint8)); d = ImageDraw.Draw(img)
    rnd = random.Random(5)
    for cx in (62, 578):
        arch(d, cx, 62, 96, FLOOR, hx('141019')); arch(d, cx, 72, 78, FLOOR, hx('0b0910'))
    # A tall broken greenhouse window in the middle, letting moonlight in.
    arch(d, 320, 18, 170, FLOOR - 40, hx('1d3340'))
    for k in range(-3, 4): d.line([320 + k * 22, 30, 320 + k * 22, FLOOR - 40], fill=hx('141019'), width=3)
    for y in (70, 110, 150): d.line([238, y, 402, y], fill=hx('141019'), width=3)
    for _ in range(9):                                                         # missing panes
        gx, gy = rnd.randint(244, 390), rnd.randint(40, FLOOR - 50)
        d.rectangle([gx, gy, gx + 18, gy + 34], fill=hx('0b0910'))
    for px_ in (210, 430):                                                      # pillars strangled by roots
        d.rectangle([px_ - 16, 0, px_ + 16, FLOOR], fill=hx('352a45'))
        for k in range(6):
            x0 = px_ - 18 + rnd.randint(0, 36)
            pts = [(x0 + math.sin(y / 11 + k) * 6, y) for y in range(0, FLOOR, 6)]
            d.line(pts, fill=hx('3a2a18'), width=rnd.randint(3, 6))
    for _ in range(40):                                                         # vines from the ceiling
        x = rnd.randint(0, W); length = rnd.randint(20, 120)
        pts = [(x + math.sin(y / 8 + x) * 4, y) for y in range(0, length, 4)]
        d.line(pts, fill=hx('3d6230'), width=2)
        for y in range(6, length, 14): d.ellipse([pts[y // 4][0] - 3, y - 2, pts[y // 4][0] + 3, y + 2], fill=hx('6f9a40'))
    alb = np.asarray(img, np.float32).copy()
    emis = np.zeros_like(alb)
    for _ in range(60):                                                         # glowing fungi on walls and floor edge
        fx, fy = rnd.randint(0, W - 1), rnd.choice([rnd.randint(120, FLOOR - 2), rnd.randint(FLOOR, FLOOR + 30)])
        c = np.array(hx(rnd.choice(['b3d05a', 'd88aa8', 'e8f5a0'])))
        emis[max(0, fy - 1):fy + 2, max(0, fx - 1):fx + 2] = c * 0.9
    alb = floor_tiles(alb, hx('233a22'), hx('141019'))
    for _ in range(26):                                                          # moss patches on the floor
        mx, my = rnd.randint(0, W), rnd.randint(FLOOR + 6, H - 4)
        rr = rnd.randint(8, 28)
        m = np.hypot((xx - mx) / rr, (yy - my) / (rr * 0.35)) < 1
        alb[m] = alb[m] * 0.5 + np.array(hx('3d6230')) * 0.5
    light = np.zeros_like(alb) + np.array([0.45, 0.6, 0.55])
    for tx, ty in TORCHES: add_point(light, tx, ty - 10, (0.9, 0.75, 0.45), 240, 1.0)
    shaft = shaft_mask(320, 40, spread=110, start_half=60, fade=0.55, drift=20)
    light += shaft[..., None] * np.array([0.65, 0.95, 0.85])
    add_point(light, 320, 260, (0.6, 1.0, 0.7), 220, 0.3)
    scene = alb * light + emis
    return finish(scene, shaft, [30, 50, 40], [14, 22, 16], bloom=1.4, sparks=['e8f5a0', 'b3d05a', 'd88aa8'])


# ═════════════════════════ The Silent Archive ═════════════════════════
def archive():
    alb = np.zeros((H, W, 3), np.float32)
    alb[:FLOOR] = np.array(hx('3a2a18'), np.float32)
    for y in range(FLOOR): alb[y] *= 0.45 + 0.55 * (y / FLOOR)
    img = Image.fromarray(alb.clip(0, 255).astype(np.uint8)); d = ImageDraw.Draw(img)
    rnd = random.Random(9)
    spines = ['74291f', '2a5560', '6a2848', '5c4228', '233a22', '433758', '8a6640', 'a63d25']
    for x0, x1 in ((0, 200), (440, 640)):                                       # walls of bookshelves
        for shelf_y in range(14, FLOOR - 10, 26):
            d.rectangle([x0, shelf_y + 22, x1, shelf_y + 25], fill=hx('5c4228'))
            x = x0 + 2
            while x < x1 - 4:
                w = rnd.randint(3, 6); h = rnd.randint(14, 21)
                d.rectangle([x, shelf_y + 22 - h, x + w - 1, shelf_y + 21], fill=hx(rnd.choice(spines)))
                if rnd.random() < 0.3: d.line([x, shelf_y + 22 - h + 3, x + w - 1, shelf_y + 22 - h + 3], fill=hx('ffb85a'))
                x += w + (1 if rnd.random() < 0.85 else 6)
        for k in range(x0, x1 + 1, 50): d.rectangle([k, 0, k + 5, FLOOR], fill=hx('3a2a18'))
    d.line([(150, 30), (186, FLOOR)], fill=hx('8a6640'), width=3); d.line([(166, 30), (202, FLOOR)], fill=hx('8a6640'), width=3)   # ladder
    for y in range(40, FLOOR, 14): d.line([(150 + (y - 30) * 36 // (FLOOR - 30), y), (166 + (y - 30) * 36 // (FLOOR - 30), y)], fill=hx('8a6640'), width=2)
    arch(d, 320, 20, 200, FLOOR, hx('141019'))                                   # a vast arched window onto nothing
    alb = np.asarray(img, np.float32).copy()
    emis = np.zeros_like(alb)
    cx, cy, r = 320, 96, 64
    dist = np.hypot(xx - cx, yy - cy); ang = (np.arctan2(yy - cy, xx - cx) + math.pi) / (2 * math.pi)
    win = dist < r
    ring = (np.abs(dist - r * 0.66) < 1.5) | (np.abs(dist - r * 0.33) < 1.5) | (np.abs((ang * 16) % 1 - 0.5) > 0.46)
    emis[win & ~ring] = np.array(hx('a8406a')) * 0.5 + np.array(hx('6b6386')) * 0.5
    emis[win & ring] = np.array(hx('ffe09a')) * 0.8                             # a glowing arcane sigil in the glass
    alb[win] = np.array(hx('141019'))
    for _ in range(14):                                                          # floating candles
        fx_, fy_ = rnd.randint(60, 580), rnd.randint(30, 150)
        alb[fy_:fy_ + 5, fx_:fx_ + 2] = np.array(hx('e8dcc0'))
        emis[fy_ - 2:fy_, fx_:fx_ + 2] = np.array(hx('ffe09a'))
    alb = floor_tiles(alb, hx('5c4228'), hx('3a2a18'))
    light = np.zeros_like(alb) + np.array([0.5, 0.45, 0.62])
    for tx, ty in TORCHES: add_point(light, tx, ty - 10, (1.0, 0.75, 0.45), 260, 1.15)
    shaft = shaft_mask(cx, cy + 20, spread=100, start_half=50, fade=0.5)
    light += shaft[..., None] * np.array([0.9, 0.6, 1.0])
    add_point(light, 320, 255, (1.0, 0.8, 0.6), 200, 0.35)
    scene = alb * light + emis
    return finish(scene, shaft, [50, 30, 60], [20, 16, 26], bloom=1.4, sparks=['ffe09a', 'c070ff'])


# ═════════════════════════ Foregrounds (same style as depth_v2) ═════════════════════════
def garden_fg():
    from depth_v2 import pillar
    img = Image.new('RGBA', (W, H)); d = ImageDraw.Draw(img); r = random.Random(11)
    col, edge = hx('0b0910') + (255,), hx('3d6230') + (255,)
    pillar(d, 0, 16, 0, col, edge); pillar(d, W - 16, 16, 0, col, edge)
    for _ in range(30):                                                          # hanging vines with leaves
        x = r.choice([r.randint(10, 170), r.randint(470, 630)]); length = r.randint(20, 120)
        pts = [(x + math.sin(y / 9 + x) * 3, y) for y in range(0, length, 4)]
        d.line(pts, fill=hx('141019') + (255,), width=2)
        for y in range(8, length, 12): d.ellipse([pts[y // 4][0] - 3, y - 2, pts[y // 4][0] + 2, y + 2], fill=hx('233a22') + (255,))
    for x in (6, W - 10):                                                        # thorny root on the pillar
        for y in range(40, 200, 14): d.polygon([(x, y), (x + 4, y - 4), (x + 3, y + 2)], fill=hx('3d6230') + (255,))
    return img


def archive_fg():
    from depth_v2 import pillar
    img = Image.new('RGBA', (W, H)); d = ImageDraw.Draw(img); r = random.Random(12)
    col, edge = hx('0b0910') + (255,), hx('8a6640') + (255,)
    pillar(d, 0, 18, 0, col, edge); pillar(d, W - 18, 18, 0, col, edge)
    for x, n in ((90, 60), (560, 80)):                                           # hanging lamps on chains
        for y in range(0, n, 7): d.line([(x, y), (x, y + 5)], fill=col, width=2)
        d.polygon([(x - 6, n), (x + 6, n), (x + 4, n + 10), (x - 4, n + 10)], fill=col)
        d.point((x, n + 6), fill=hx('ffe09a') + (255,))
    for _ in range(14):                                                          # loose pages drifting at the top
        px_, py_ = r.randint(20, W - 20), r.randint(0, 70)
        d.polygon([(px_, py_), (px_ + 6, py_ + 2), (px_ + 4, py_ + 8), (px_ - 2, py_ + 6)], fill=hx('e8dcc0') + (160,))
    return img


if __name__ == '__main__':
    for name, fn in (('garden', garden), ('archive', archive)):
        fn().resize((W * 2, H * 2), Image.NEAREST).save(f'Backgrounds/{name}.png')
    garden_fg().resize((W * 2, H * 2), Image.NEAREST).save('Backgrounds/garden_fg.png')
    archive_fg().resize((W * 2, H * 2), Image.NEAREST).save('Backgrounds/archive_fg.png')
    print('saved garden, archive')
