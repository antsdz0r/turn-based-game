"""Regions update: new monsters, bosses, weapons, FX and two new backgrounds.
Run: python3 make_art_regions.py <Art dir>"""
import os, sys, random
from PIL import Image
import make_art as base
from make_art import Canvas, hexc

OUT = sys.argv[1]
base.OUT = OUT


def rat():
    c = Canvas(32, 32)
    F, FS, FD = '#7a6a5a', '#5a4c40', '#3a3028'
    for (ox, oy) in [(4, 0), (13, 6)]:  # two rats
        c.rect(6 + ox, 18 + oy, 14 + ox, 22 + oy, F); c.rect(7 + ox, 17 + oy, 12 + ox, 17 + oy, F)
        c.rect(12 + ox, 20 + oy, 14 + ox, 22 + oy, FS)
        c.rect(14 + ox, 18 + oy, 17 + ox, 21 + oy, F); c.px(18 + ox, 20 + oy, '#e0a0a0'); c.px(16 + ox, 19 + oy, '#ff3b3b')
        c.pts([(14 + ox, 17 + oy), (15 + ox, 16 + oy)], '#d09090')
        c.pts([(5 + ox, 21 + oy), (4 + ox, 20 + oy), (3 + ox, 20 + oy), (2 + ox, 21 + oy)], '#c08a8a')
        c.pts([(8 + ox, 23 + oy), (12 + ox, 23 + oy)], FD)
    c.outline(); c.mirror(); return c


def moth():
    c = Canvas(32, 32)
    W, WS, B = '#c9b8e0', '#8a7aa8', '#4a3a5a'
    for y in range(6, 20):
        w = 9 - abs(y - 12) // 2
        c.hline(15 - w - 5, 14, y, W if y % 3 else WS)
        c.hline(17, 17 + w + 5, y, W if y % 3 else WS)
    c.pts([(6, 10), (8, 14), (24, 10), (22, 14)], '#ffe27a')  # eye spots
    c.rect(14, 8, 17, 22, B); c.rect(15, 5, 16, 7, B); c.pts([(13, 3), (18, 3), (14, 4), (17, 4)], '#c9b8e0')
    c.px(15, 6, '#ff6aff'); c.px(16, 6, '#ff6aff')
    for y in (11, 14, 17): c.hline(14, 17, y, '#6a5a7a')
    c.outline(); c.mirror(); return c


def thrall():
    c = Canvas(32, 32)
    S, SS, SD = '#6a9a9a', '#4a7272', '#2e4a4a'
    R = '#3a4a5a'
    c.rect(11, 22, 13, 29, SS); c.rect(16, 22, 18, 29, SD)
    c.rect(9, 12, 20, 22, R); c.rect(18, 12, 20, 22, '#2a3644')
    c.pts([(10, 22), (12, 23), (15, 22), (19, 23)], R)  # tattered
    c.rect(12, 4, 18, 11, S); c.rect(17, 4, 18, 11, SS); c.clear(12, 4); c.clear(18, 4)
    c.px(14, 7, '#bfffff'); c.px(17, 7, '#bfffff'); c.hline(14, 17, 9, SD)
    c.pts([(12, 5), (13, 4), (11, 6), (11, 7)], '#3a5a3a')  # seaweed hair
    c.rect(20, 13, 24, 16, S); c.rect(24, 14, 25, 17, SS)  # reaching arm
    c.rect(6, 13, 9, 18, S)
    c.pts([(10, 16), (13, 18), (17, 15)], '#5a8a5a')  # barnacles
    c.outline(); c.mirror(); return c


def siren():
    c = Canvas(32, 32)
    S, SS, H = '#8fc9d9', '#5a9ab0', '#1a5a7a'
    # tail
    for i, y in enumerate(range(18, 29)):
        x0 = 12 + i // 3
        c.hline(x0, x0 + 5 - i // 5, y, '#3a8a9a')
    c.pts([(15, 29), (14, 30), (18, 29), (19, 30), (16, 29)], '#2a6a7a')
    # torso
    c.rect(12, 11, 18, 18, S); c.rect(17, 11, 18, 18, SS)
    c.pts([(13, 14), (16, 14)], '#ffd0e0')
    # head + flowing hair
    c.rect(13, 4, 18, 10, S); c.rect(15, 6, 17, 8, '#d0f4ff'); c.px(16, 7, '#1a2a3a'); c.px(17, 7, '#1a2a3a')
    for y in range(3, 20):
        c.px(12 - (y % 3 == 0), y, H); c.px(11, y, H) if y > 6 else None
    c.hline(12, 18, 3, H)
    # arms raised singing
    c.rect(19, 9, 21, 12, S); c.pts([(22, 7), (22, 8), (23, 6)], S)
    c.pts([(24, 4), (26, 3), (25, 6)], '#bfeaff')  # notes
    c.outline(); c.mirror(); return c


def leech():
    c = Canvas(32, 32)
    L, LS, LD = '#5a3a4a', '#40283a', '#2a1824'
    for x in range(6, 25):
        h = 4 + int(2 * abs(((x - 6) % 8) - 4) / 4)
        c.vline(x, 26 - h, 27, L)
        c.vline(x, 27 - 1, 27, LS)
    c.rect(22, 18, 27, 25, L); c.rect(25, 19, 27, 24, LS)
    for i, (x, y) in enumerate([(27, 21), (27, 22), (26, 20), (26, 23)]): c.px(x, y, '#ff4a6a')
    c.pts([(28, 20), (28, 23), (29, 21), (29, 22)], '#e8e2c8')  # teeth ring
    for x in (9, 13, 17, 21): c.px(x, 22, '#8a5a6a')
    c.outline(); c.mirror(); return c


def imp():
    c = Canvas(32, 32)
    R, RS, RD = '#d0452a', '#a0321e', '#6a1e12'
    c.rect(13, 24, 14, 28, RS); c.rect(17, 24, 18, 28, RD)
    c.rect(12, 16, 19, 24, R); c.rect(17, 16, 19, 24, RS)
    c.rect(12, 9, 19, 16, R); c.rect(18, 9, 19, 16, RS)
    c.pts([(12, 8), (11, 7), (11, 6), (19, 8), (20, 7), (20, 6)], '#2a1a1a')  # horns
    c.pts([(14, 12), (17, 12)], '#ffe08a'); c.hline(14, 17, 14, '#3a0a0a')
    # wings
    for y in range(12, 20):
        c.hline(5 + (y - 12) // 2, 11, y, '#5a1a1a')
    c.pts([(20, 18), (21, 17), (22, 16), (23, 15), (24, 14)], RD)  # tail
    c.pts([(25, 13), (25, 14), (26, 14)], '#ffb03a')
    # fire in hand
    c.rect(20, 10, 22, 12, '#ffb03a'); c.px(21, 9, '#ffe08a'); c.px(21, 11, '#fff2b0')
    c.outline(); c.mirror(); return c


def golem():
    c = Canvas(32, 32)
    R, RS, RD = '#4a3a36', '#36292a', '#221a1a'
    L = '#ff8a2c'
    c.rect(8, 22, 13, 29, RS); c.rect(17, 22, 22, 29, RD)
    c.rect(6, 9, 23, 22, R); c.rect(19, 9, 23, 22, RS)
    c.rect(11, 3, 18, 9, R); c.rect(16, 3, 18, 9, RS)
    c.pts([(13, 6), (16, 6)], '#ffe08a')
    for (x, y) in [(9, 12), (10, 13), (11, 14), (11, 15), (15, 17), (16, 18), (17, 18), (20, 12), (20, 13), (14, 10)]:
        c.px(x, y, L)
    c.rect(2, 12, 6, 20, RS); c.rect(23, 12, 27, 20, R)  # massive arms
    c.rect(1, 19, 6, 23, R); c.rect(23, 19, 28, 23, RS)
    c.pts([(3, 21), (25, 21)], L)
    c.outline(); c.mirror(); return c


def colossus():
    c = Canvas(40, 40)
    B, BS, BD = '#ece6d4', '#bdb49c', '#8a826c'
    E = '#7affd0'
    c.rect(13, 30, 16, 38, B); c.rect(23, 30, 26, 38, BS); c.hline(11, 17, 38, BS); c.hline(22, 28, 38, BD)
    c.rect(12, 26, 27, 30, B); c.rect(18, 27, 21, 29, BD)
    c.vline(19, 13, 26, BS)
    for y in (14, 17, 20, 23):
        c.hline(11, 28, y, B); c.px(28, y, BS); c.px(11, y, BS)
    c.rect(18, 16, 20, 19, '#2a1a24'); c.px(19, 17, E)
    c.rect(6, 10, 33, 13, B); c.rect(4, 9, 8, 12, BS); c.rect(31, 9, 35, 12, BS)
    c.rect(14, 1, 25, 9, B); c.rect(23, 1, 25, 9, BS); c.clear(14, 1); c.clear(25, 1)
    c.rect(16, 4, 18, 5, '#140e16'); c.rect(21, 4, 23, 5, '#140e16'); c.px(17, 4, E); c.px(22, 4, E)
    c.hline(16, 23, 8, BD)
    c.rect(3, 13, 5, 24, B); c.rect(34, 13, 36, 24, BS)  # arms
    c.rect(33, 4, 37, 14, '#7a5532'); c.rect(32, 1, 38, 5, '#8c6a42')  # club raised
    c.rect(1, 24, 6, 27, B)
    c.outline(); c.mirror(); return c


def matriarch():
    c = Canvas(40, 40)
    S, SS, SD = '#3a7a8a', '#2a5a6a', '#1a3a4a'
    G = '#bfffff'
    # coiling lower body
    for y in range(22, 38):
        x0 = 8 + abs(y - 30) // 2
        c.hline(x0, 31 - abs(y - 30) // 3, y, S if (y // 2) % 2 else SS)
    c.hline(8, 31, 38, SD)
    # torso
    c.rect(14, 12, 25, 22, '#5a9aaa'); c.rect(22, 12, 25, 22, SS)
    c.pts([(16, 16), (17, 17), (21, 16), (22, 17)], '#8fd0e0')
    # head with crown of coral
    c.rect(15, 4, 24, 12, '#5a9aaa'); c.rect(22, 4, 24, 12, SS)
    c.pts([(17, 7), (21, 7)], G); c.pts([(17, 8), (21, 8)], '#0a2a3a'); c.hline(17, 21, 10, SD)
    c.pts([(14, 3), (15, 1), (16, 2), (19, 0), (19, 1), (19, 2), (22, 2), (23, 1), (24, 3)], '#ff7a8a')
    # long hair/tendrils
    for y in range(5, 26):
        c.px(13 - (y % 4 == 0), y, SD); c.px(26 + (y % 4 == 0), y, SD)
    # trident
    c.vline(33, 3, 34, '#c9a55a'); c.hline(30, 36, 4, '#c9a55a'); c.pts([(30, 2), (30, 3), (33, 1), (33, 2), (36, 2), (36, 3)], '#e0c870')
    c.rect(26, 15, 32, 17, '#5a9aaa')
    c.outline(); c.mirror(); return c


# ───────────── Weapons (hilt at bottom-center, blade pointing up) ─────────────

def weapon(kind):
    c = Canvas(12, 24)
    if kind == 'sword':
        c.rect(5, 2, 6, 15, '#eef3f8'); c.vline(6, 2, 15, '#9aa7b5'); c.px(5, 1, '#eef3f8')
        c.hline(2, 9, 16, '#e2b84a'); c.rect(5, 17, 6, 20, '#5a3d24'); c.rect(5, 21, 6, 21, '#e2b84a')
    elif kind == 'greatsword':
        c.rect(4, 0, 7, 15, '#9aa7b5'); c.vline(7, 0, 15, '#6a7584'); c.vline(4, 0, 15, '#cfd8e2')
        c.hline(1, 10, 16, '#d9b44a'); c.rect(5, 17, 6, 21, '#4a321c'); c.px(5, 22, '#ff8a2c'); c.px(6, 22, '#ff8a2c')
    elif kind == 'dagger':
        c.rect(5, 9, 6, 16, '#d9dde4'); c.vline(6, 9, 16, '#9aa7b5'); c.px(5, 8, '#d9dde4')
        c.hline(3, 8, 17, '#3fa39a'); c.rect(5, 18, 6, 20, '#2a2430')
    elif kind == 'mace':
        c.rect(5, 9, 6, 21, '#6b4a2b'); c.rect(3, 3, 8, 8, '#a9a9b5'); c.pts([(2, 5), (9, 5), (5, 2), (6, 2)], '#d9dde4')
        c.rect(4, 4, 7, 7, '#ffb03a')
    elif kind == 'club':
        c.rect(5, 12, 6, 21, '#7a5532'); c.rect(3, 2, 8, 11, '#8c6a42'); c.vline(8, 2, 11, '#6b4a2b'); c.px(4, 4, '#bdb49c')
    c.outline(); return c


def claw_fx():
    c = Canvas(16, 16)
    for k in range(3):
        for i in range(10):
            x, y = 2 + i + k * 2, 12 - i + k
            c.px(x, y, '#ffffff' if 2 <= i <= 7 else '#ffd0d0')
            c.px(x + 1, y, '#ffe0e0')
    return c


def magic_circle():
    c = Canvas(32, 12)
    for x in range(32):
        for y in range(12):
            dx, dy = (x - 15.5) / 15.5, (y - 5.5) / 5.5
            d = dx * dx + dy * dy
            if 0.72 < d < 1.0 or 0.28 < d < 0.36:
                c.px(x, y, '#ffffff')
    for (x, y) in [(15, 0), (16, 0), (0, 5), (31, 6), (15, 11), (16, 11)]:
        c.px(x, y, '#ffffff')
    return c


def recolor_background(src, dst, palette, features):
    img = Image.open(src).convert('RGBA')
    W, H = img.size
    out = Image.new('RGBA', (W, H))
    for y in range(H):
        for x in range(W):
            r, g, b, a = img.getpixel((x, y))
            lum = (0.3 * r + 0.59 * g + 0.11 * b) / 255
            pr, pg, pb = palette(lum, x, y)
            out.putpixel((x, y), (int(pr), int(pg), int(pb), 255))
    features(out)
    out.save(dst)


def drowned_palette(lum, x, y):
    k = lum * 2.2
    floor = y >= 96
    if floor:
        ripple = 18 if (x + y * 3) % 23 == 0 or (x * 2 + y) % 31 == 0 else 0
        return 26 * k + ripple, 84 * k + 34 + ripple, 96 * k + 44 + ripple
    return 40 * k + 4, 98 * k + 10, 114 * k + 18


def drowned_features(img):
    rnd = random.Random(11)
    W, H = img.size
    for _ in range(9):  # hanging kelp
        x = rnd.randrange(8, W - 8)
        length = rnd.randrange(14, 40)
        for y in range(0, length):
            xx = x + int(2 * ((y // 5) % 2))
            img.putpixel((xx, y), hexc('#2f5a3a'))
            if y % 3 == 0: img.putpixel((xx + 1, y), hexc('#3f7a4a'))
    for _ in range(40):  # wet glints
        x, y = rnd.randrange(W), rnd.randrange(96, H)
        img.putpixel((x, y), hexc('#7fd4d0'))


def cinder_palette(lum, x, y):
    k = lum * 2.2
    return 112 * k + 12, 42 * k + 6, 36 * k + 8


def cinder_features(img):
    rnd = random.Random(5)
    W, H = img.size
    for _ in range(26):  # glowing lava cracks
        x, y = rnd.randrange(W), rnd.randrange(20, H - 4)
        for _ in range(rnd.randrange(6, 16)):
            if 0 <= x < W and 0 <= y < H:
                img.putpixel((x, y), hexc(rnd.choice(['#ff8a2c', '#ff6a1a', '#ffb03a'])))
            x += rnd.choice((-1, 1, 1)); y += rnd.choice((0, 1)) if y < 96 else rnd.choice((0, 0, 1, -1))
    for x in range(W):  # lava glow along the floor line
        for i in range(2):
            img.putpixel((x, 96 + i), hexc('#ff6a1a' if (x // 7) % 3 else '#ffb03a'))


if __name__ == '__main__':
    for name, fn in [('rat', rat), ('moth', moth), ('thrall', thrall), ('siren', siren), ('leech', leech),
                     ('imp', imp), ('golem', golem), ('colossus', colossus), ('matriarch', matriarch)]:
        fn().save(f'Units/{name}.png')
    for kind in ['sword', 'greatsword', 'dagger', 'mace', 'club']:
        weapon(kind).save(f'FX/weapon_{kind}.png')
    claw_fx().save('FX/claw.png')
    magic_circle().save('FX/circle.png')
    bg = os.path.join(OUT, 'Backgrounds')
    recolor_background(os.path.join(bg, 'crypt.png'), os.path.join(bg, 'drowned.png'), drowned_palette, drowned_features)
    recolor_background(os.path.join(bg, 'crypt.png'), os.path.join(bg, 'cinder.png'), cinder_palette, cinder_features)
    print('done')
