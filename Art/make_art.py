"""Generates original pixel art for TBRPG. Run: python3 make_art.py <out_dir>"""
import os, sys, random
from PIL import Image

OUT = sys.argv[1]
OUTLINE = (18, 13, 24, 255)


def hexc(h, a=255):
    h = h.lstrip('#')
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


class Canvas:
    def __init__(self, w, h):
        self.img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
        self.w, self.h = w, h

    def px(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.img.putpixel((x, y), hexc(c) if isinstance(c, str) else c)

    def clear(self, x, y):
        self.px(x, y, (0, 0, 0, 0))

    def rect(self, x0, y0, x1, y1, c):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.px(x, y, c)

    def hline(self, x0, x1, y, c):
        self.rect(x0, y, x1, y, c)

    def vline(self, x, y0, y1, c):
        self.rect(x, y0, x, y1, c)

    def pts(self, pts, c):
        for x, y in pts:
            self.px(x, y, c)

    def outline(self, color=OUTLINE):
        src = self.img.copy()
        for y in range(self.h):
            for x in range(self.w):
                if src.getpixel((x, y))[3] != 0:
                    continue
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < self.w and 0 <= ny < self.h and src.getpixel((nx, ny))[3] > 0:
                        self.img.putpixel((x, y), color)
                        break

    def mirror(self):
        self.img = self.img.transpose(Image.FLIP_LEFT_RIGHT)

    def save(self, name):
        path = os.path.join(OUT, name)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        self.img.save(path)


# ─────────────── Heroes (face right) ───────────────

def knight():
    c = Canvas(32, 32)
    A, S, D = '#d3dae4', '#8f9cb0', '#56627a'
    R, RD, Y, B = '#b8322a', '#7a1f1a', '#e2b84a', '#5a3d24'
    # legs + boots
    c.rect(12, 23, 14, 27, S); c.rect(16, 23, 18, 27, D)
    c.rect(11, 28, 14, 29, D); c.rect(16, 28, 19, 29, '#3d4659')
    # torso armour
    c.rect(11, 13, 19, 22, A); c.rect(18, 13, 19, 22, S)
    # tabard
    c.rect(13, 14, 17, 22, R); c.rect(16, 14, 17, 22, RD)
    c.rect(14, 16, 15, 19, Y); c.hline(13, 16, 17, Y)
    c.hline(11, 19, 21, B); c.px(15, 21, Y)
    # pauldrons
    c.rect(10, 13, 12, 15, S); c.hline(10, 12, 13, A)
    # helmet
    c.rect(12, 5, 18, 12, A); c.rect(17, 5, 18, 12, S); c.rect(12, 11, 18, 12, S)
    c.clear(12, 5); c.clear(18, 5)
    c.hline(14, 18, 8, '#141018'); c.px(17, 10, '#141018'); c.px(15, 10, '#141018')
    c.vline(15, 5, 7, S)
    # plume
    c.pts([(15, 4), (14, 3), (13, 3), (12, 3), (11, 4), (10, 5), (14, 4), (13, 4)], R)
    c.pts([(12, 4), (11, 5), (10, 6)], RD)
    # sword (behind, left hand)
    c.rect(8, 3, 9, 16, '#eef3f8'); c.vline(9, 3, 16, '#9aa7b5'); c.px(8, 2, '#eef3f8')
    c.hline(6, 11, 17, Y); c.rect(8, 18, 9, 20, B); c.px(8, 21, Y); c.px(9, 21, Y)
    c.rect(10, 16, 11, 19, S)
    # shield (front)
    c.rect(19, 14, 24, 22, '#34579a'); c.rect(23, 14, 24, 22, '#253f73')
    c.hline(19, 24, 14, Y); c.vline(19, 14, 22, Y); c.vline(24, 14, 22, Y)
    c.hline(20, 23, 23, '#34579a'); c.px(20, 23, Y); c.px(23, 23, Y)
    c.hline(21, 22, 24, Y)
    c.rect(21, 16, 22, 21, Y); c.hline(20, 23, 18, Y)
    c.outline(); return c


def cleric():
    c = Canvas(32, 32)
    W, WS, WD = '#efe8d6', '#c4b799', '#8f8268'
    Y, YD, K, KS = '#e8c24f', '#b08a2a', '#e6b48f', '#b98563'
    # robe (wide at bottom)
    for i, y in enumerate(range(13, 30)):
        half = 4 + i // 3
        c.hline(15 - half, 15 + half - 1, y, W)
        c.hline(15 + half - 3, 15 + half - 1, y, WS)
    c.hline(10, 20, 29, WD)
    c.rect(14, 13, 16, 29, Y); c.vline(16, 13, 29, YD)  # gold stole
    c.hline(11, 19, 20, YD)
    # hood + face
    c.rect(11, 5, 19, 13, W); c.rect(17, 5, 19, 13, WS)
    c.clear(11, 5); c.clear(19, 5); c.clear(11, 6)
    c.rect(14, 7, 18, 11, K); c.vline(14, 7, 11, KS)
    c.px(17, 9, '#2a1d1a'); c.px(18, 11, KS)
    c.hline(13, 18, 6, WS)
    # arm + staff
    c.rect(19, 15, 21, 19, WS)
    c.rect(22, 6, 22, 29, '#7a5532'); c.vline(23, 6, 29, '#56391f')
    c.rect(21, 2, 24, 5, Y); c.pts([(22, 1), (23, 1), (20, 3), (25, 3), (20, 4), (25, 4), (22, 6)], Y)
    c.rect(22, 3, 23, 4, '#fff7c4')
    c.rect(21, 17, 22, 18, K)
    c.outline(); return c


def mage():
    c = Canvas(32, 32)
    P, PS, PD = '#7a56b8', '#5a3c8c', '#3c275e'
    G, K, KS, BE = '#d9b44a', '#e6b48f', '#b98563', '#d9d9e0'
    # robe
    for i, y in enumerate(range(14, 30)):
        half = 4 + i // 3
        c.hline(15 - half, 15 + half - 1, y, P)
        c.hline(15 + half - 3, 15 + half - 1, y, PS)
    c.hline(10, 20, 29, PD); c.hline(11, 19, 21, G)
    # beard
    c.rect(14, 12, 18, 17, BE); c.rect(15, 18, 17, 19, BE); c.px(16, 20, BE); c.vline(18, 12, 16, '#a9a9b5')
    # face
    c.rect(14, 9, 18, 11, K); c.vline(14, 9, 11, KS); c.px(17, 10, '#1e1420')
    # hat
    c.hline(10, 21, 8, PD); c.hline(11, 20, 7, P)
    c.rect(12, 5, 18, 6, P); c.rect(13, 3, 17, 4, P); c.rect(14, 1, 16, 2, P); c.px(13, 1, P); c.px(12, 2, P)
    c.hline(12, 18, 6, G); c.vline(17, 1, 5, PS)
    # staff + orb
    c.vline(23, 7, 29, '#6b4a2b'); c.vline(24, 9, 29, '#4a321c')
    c.rect(22, 3, 25, 6, '#ff8a3d'); c.rect(23, 4, 24, 5, '#ffe08a'); c.px(22, 3, '#ffd06a')
    c.clear(22, 6); c.clear(25, 6); c.clear(25, 3)
    c.rect(20, 15, 22, 18, PS); c.rect(22, 16, 23, 17, K)
    c.outline(); return c


# ─────────────── Enemies (drawn facing right, then mirrored to face left) ───────────────

def ghoul():
    c = Canvas(32, 32)
    G, GS, GD = '#8fae78', '#647f52', '#435838'
    C, R = '#3a2f3f', '#ff3b3b'
    # legs (bent)
    c.rect(10, 24, 12, 29, GS); c.rect(16, 24, 18, 29, GD)
    c.hline(9, 12, 29, GD); c.hline(16, 19, 29, '#2f3f27')
    # ragged pants
    c.rect(9, 21, 19, 24, C); c.pts([(9, 25), (12, 25), (16, 25), (19, 25)], C)
    # hunched body
    c.rect(7, 12, 18, 21, G); c.rect(6, 13, 8, 19, G); c.rect(7, 10, 13, 12, G)
    c.rect(6, 11, 10, 14, GS); c.rect(16, 13, 18, 21, GS)
    c.pts([(11, 15), (12, 16), (11, 17), (12, 18)], GD)  # ribs
    c.pts([(14, 15), (15, 16), (14, 17), (15, 18)], GD)
    # head forward
    c.rect(17, 9, 23, 15, G); c.rect(22, 11, 24, 15, G); c.rect(17, 14, 23, 15, GS)
    c.px(21, 11, R); c.px(22, 11, R); c.px(23, 12, '#b22020')
    c.hline(20, 24, 16, '#e8e2c8'); c.pts([(21, 17), (23, 17)], '#e8e2c8')
    c.hline(19, 23, 9, GS)
    # arms reaching forward with claws
    c.rect(17, 17, 22, 19, GS); c.rect(22, 18, 25, 20, G)
    c.pts([(26, 18), (27, 19), (26, 20), (27, 21), (26, 21)], '#efe9d2')
    c.rect(19, 21, 23, 22, GD); c.pts([(24, 22), (25, 23), (24, 23)], '#efe9d2')
    c.outline(); c.mirror(); return c


def skeleton(archer=False):
    c = Canvas(32, 32)
    B, BS, BD = '#ece6d4', '#bdb49c', '#8a826c'
    E = '#140e16'
    # legs
    c.vline(13, 22, 28, B); c.vline(17, 22, 28, BS)
    c.hline(12, 14, 29, B); c.hline(16, 18, 29, BS)
    c.pts([(13, 25), (17, 25)], BD)
    # pelvis
    c.rect(12, 20, 18, 21, B); c.px(15, 21, BD)
    # spine + ribs
    c.vline(15, 12, 19, BS)
    for y in (13, 15, 17):
        c.hline(12, 18, y, B); c.px(18, y, BS)
    # skull
    c.rect(12, 4, 19, 10, B); c.rect(18, 4, 19, 10, BS)
    c.clear(12, 4); c.clear(19, 4)
    c.rect(15, 6, 16, 7, E); c.rect(18, 6, 18, 7, E)
    c.px(17, 8, BD); c.hline(14, 19, 10, BS); c.pts([(15, 11), (17, 11), (19, 11)], B)
    if not archer:
        # back arm and club raised
        c.vline(11, 13, 17, BS); c.vline(19, 13, 17, B); c.vline(20, 11, 13, B)
        c.rect(20, 3, 22, 10, '#7a5532'); c.vline(22, 3, 10, '#56391f')
        c.rect(19, 1, 23, 4, '#8c6a42'); c.px(23, 2, '#bdb49c'); c.px(19, 1, '#bdb49c')
    else:
        H, HS = '#3d3a52', '#2a2838'
        # hood/cloak
        c.rect(11, 3, 19, 5, H); c.vline(11, 3, 9, H); c.px(12, 3, HS); c.vline(19, 3, 4, HS)
        c.rect(10, 12, 12, 21, H); c.pts([(10, 22), (11, 22)], H)
        # bow (front)
        c.vline(23, 9, 21, '#8c6a42'); c.pts([(22, 8), (22, 22), (21, 7), (21, 23)], '#8c6a42')
        c.vline(21, 8, 22, '#d9d0b5')
        c.hline(18, 22, 15, B)  # arm holding bow
        c.hline(17, 25, 13, '#cfc6aa'); c.px(26, 13, '#9aa7b5')  # arrow
    c.outline(); c.mirror(); return c


# ─────────────── Props, FX, icons ───────────────

def torch():
    c = Canvas(8, 16)
    c.rect(3, 4, 4, 13, '#6b4a2b'); c.vline(4, 4, 13, '#4a321c')
    c.rect(2, 3, 5, 4, '#56627a'); c.rect(3, 14, 4, 15, '#3d4659'); c.hline(1, 6, 10, '#56627a')
    c.outline(); return c


def cultist():
    c = Canvas(32, 32)
    R, RS, RD = '#8e2a2a', '#651c1f', '#3f1014'
    K, Y = '#c99a7a', '#c9a55a'
    for i, y in enumerate(range(12, 30)):
        half = 4 + i // 3
        c.hline(15 - half, 15 + half - 1, y, R)
        c.hline(15 + half - 3, 15 + half - 1, y, RS)
    c.hline(10, 20, 29, RD); c.hline(11, 19, 20, '#2a1a1a'); c.px(15, 20, Y)
    # pointed hood with dark face
    c.rect(11, 5, 19, 13, R); c.rect(17, 5, 19, 13, RS); c.rect(13, 2, 17, 4, R); c.rect(14, 0 + 1, 16, 1, R)
    c.clear(11, 5); c.clear(19, 5)
    c.rect(14, 7, 18, 11, '#1a0f14'); c.px(16, 9, '#ffcf4a'); c.px(18, 9, '#ffcf4a')
    # arm + curved dagger
    c.rect(19, 15, 22, 18, RS); c.rect(22, 16, 23, 17, K)
    c.rect(24, 13, 24, 17, '#d9dde4'); c.px(25, 12, '#d9dde4'); c.px(24, 18, Y)
    # sigil on robe
    c.pts([(13, 23), (14, 22), (15, 23), (14, 24), (14, 25)], Y)
    c.outline(); c.mirror(); return c


def wraith():
    c = Canvas(32, 32)
    W, WS, WD = '#cfe3f2', '#93b6d1', '#5f7f9e'
    # tattered floating body
    for i, y in enumerate(range(10, 28)):
        half = 5 + i // 4
        for x in range(15 - half, 15 + half):
            if y > 22 and (x + y) % 3 == 0:
                continue
            c.px(x, y, W if x < 15 + half - 3 else WS)
    c.hline(9, 21, 27, (0, 0, 0, 0)); c.pts([(10, 26), (13, 27), (17, 27), (20, 26)], WD)
    # hood + void face
    c.rect(11, 3, 19, 12, W); c.rect(17, 3, 19, 12, WS); c.clear(11, 3); c.clear(19, 3)
    c.rect(14, 6, 18, 10, '#0c1a2a'); c.px(15, 8, '#7fffd4'); c.px(17, 8, '#7fffd4')
    # long reaching arm
    c.rect(19, 13, 24, 15, WS); c.pts([(25, 13), (26, 12), (25, 15), (26, 16), (25, 14), (26, 14)], W)
    c.outline((30, 50, 80, 255)); c.mirror(); return c


def necromancer():
    c = Canvas(32, 32)
    P, PS, PD = '#3a2650', '#2a1b3a', '#1a1024'
    G, GL, B = '#58ff8a', '#c9ffd6', '#e6e0cf'
    # wide robe
    for i, y in enumerate(range(11, 30)):
        half = 5 + i // 2
        half = min(half, 12)
        c.hline(15 - half, 15 + half - 1, y, P)
        c.hline(15 + half - 4, 15 + half - 1, y, PS)
    c.hline(3, 26, 29, PD)
    c.rect(13, 11, 17, 29, '#5a3d78'); c.vline(15, 12, 28, G)  # glowing trim
    c.pts([(13, 18), (17, 18), (13, 24), (17, 24)], G)
    # shoulders with spikes
    c.rect(8, 10, 22, 13, PS); c.pts([(8, 9), (8, 8), (22, 9), (22, 8), (7, 7), (23, 7)], B)
    # horned hood + skull face
    c.rect(11, 2, 19, 10, P); c.rect(18, 2, 19, 10, PS)
    c.pts([(10, 2), (9, 1), (9, 0), (20, 2), (21, 1), (21, 0)], B)
    c.rect(13, 4, 17, 9, B); c.rect(14, 6, 14, 6, '#0b0710'); c.px(16, 6, '#0b0710')
    c.px(14, 6, G); c.px(16, 6, G); c.hline(14, 16, 8, '#8a826c')
    # skull staff
    c.vline(25, 6, 29, '#5a4030'); c.vline(26, 8, 29, '#3c2a1e')
    c.rect(24, 2, 27, 5, B); c.px(25, 3, G); c.px(26, 3, G); c.hline(24, 27, 6, '#8a826c')
    c.pts([(23, 1), (28, 1), (25, 0), (26, 0)], GL)
    c.rect(22, 15, 24, 17, PS)
    c.outline(); c.mirror(); return c


def darkbolt():
    c = Canvas(9, 9)
    c.rect(2, 2, 6, 6, '#7b3fbf'); c.rect(1, 3, 7, 5, '#7b3fbf'); c.rect(3, 1, 5, 7, '#7b3fbf')
    c.rect(3, 3, 5, 5, '#d39cff'); c.px(4, 4, '#ffffff')
    return c


def campfire():
    c = Canvas(20, 8)
    c.rect(2, 4, 17, 6, '#5a3d24'); c.rect(4, 3, 15, 4, '#7a5532'); c.hline(1, 18, 7, '#3a3442')
    c.pts([(0, 6), (19, 6), (1, 5), (18, 5)], '#4a4552')
    c.outline(); return c


def fireball():
    c = Canvas(9, 9)
    c.rect(2, 2, 6, 6, '#ff6a2a'); c.rect(1, 3, 7, 5, '#ff6a2a'); c.rect(3, 1, 5, 7, '#ff6a2a')
    c.rect(3, 3, 5, 5, '#ffd36a'); c.px(4, 4, '#fff6d0')
    return c


def shard():
    c = Canvas(10, 5)
    c.hline(1, 6, 2, '#bfeaff'); c.hline(2, 8, 2, '#e9f9ff'); c.pts([(1, 1), (1, 3), (2, 1), (2, 3)], '#7fd4ff'); c.px(9, 2, '#ffffff')
    c.hline(3, 6, 1, '#7fd4ff'); c.hline(3, 6, 3, '#4fa8d8')
    return c


def arrow():
    c = Canvas(13, 5)
    c.hline(2, 10, 2, '#cfc6aa'); c.pts([(11, 2), (12, 2), (10, 1), (10, 3)], '#9aa7b5')
    c.pts([(0, 1), (1, 1), (0, 3), (1, 3), (2, 1), (2, 3)], '#b8322a')
    return c


def sparkle():
    c = Canvas(7, 7)
    c.vline(3, 0, 6, '#fff3b0'); c.hline(0, 6, 3, '#fff3b0'); c.px(3, 3, '#ffffff')
    c.pts([(2, 2), (4, 2), (2, 4), (4, 4)], '#ffd86a')
    return c


def slash():
    c = Canvas(16, 16)
    pts = [(3, 13), (4, 12), (5, 11), (6, 10), (7, 9), (8, 8), (9, 7), (10, 6), (11, 5), (12, 4), (13, 3)]
    for x, y in pts:
        c.px(x, y, '#ffffff'); c.px(x + 1, y, '#dfe8ff'); c.px(x, y + 1, '#9fb4ff')
    return c


def icon(kind):
    c = Canvas(11, 11)
    if kind == 'slash':
        for i in range(2, 9): c.px(i, 10 - i, '#e6ecf4')
        c.pts([(2, 6), (4, 8), (3, 7)], '#e2b84a'); c.px(1, 9, '#8c6a42')
    elif kind == 'fire':
        c.rect(3, 5, 7, 9, '#ff6a2a'); c.rect(4, 3, 6, 4, '#ff6a2a'); c.px(5, 1, '#ff6a2a'); c.px(5, 2, '#ff6a2a')
        c.px(7, 4, '#ff6a2a'); c.rect(4, 6, 6, 9, '#ffc04a'); c.px(5, 8, '#fff2b0')
    elif kind == 'ice':
        c.vline(5, 1, 9, '#bfeaff'); c.hline(1, 9, 5, '#bfeaff')
        c.pts([(3, 3), (7, 3), (3, 7), (7, 7), (2, 2), (8, 2), (2, 8), (8, 8)], '#7fd4ff'); c.px(5, 5, '#ffffff')
    elif kind == 'holy':
        c.rect(4, 1, 6, 9, '#ffe27a'); c.rect(2, 3, 8, 5, '#ffe27a'); c.vline(6, 1, 9, '#d9b44a'); c.hline(2, 8, 5, '#d9b44a')
    elif kind == 'attack':
        for i in range(1, 8): c.px(i + 1, 9 - i, '#ff9b9b')
        c.pts([(1, 6), (4, 9), (2, 7), (3, 8)], '#ffd0d0'); c.px(1, 9, '#ff9b9b')
    elif kind == 'buff':
        c.vline(5, 2, 9, '#ffb347'); c.pts([(4, 3), (6, 3), (3, 4), (7, 4), (2, 5), (8, 5)], '#ffb347')
    elif kind == 'heal':
        c.rect(4, 1, 6, 9, '#6be07a'); c.rect(1, 4, 9, 6, '#6be07a')
    elif kind == 'guard':
        c.rect(2, 1, 8, 6, '#6f9be0'); c.rect(3, 7, 7, 8, '#6f9be0'); c.px(5, 9, '#6f9be0'); c.vline(5, 2, 7, '#cfe0ff')
    elif kind == 'arrow_down':
        c.hline(1, 9, 2, '#ffd76b'); c.hline(2, 8, 3, '#ffd76b'); c.hline(3, 7, 4, '#ffd76b'); c.hline(4, 6, 5, '#ffd76b'); c.px(5, 6, '#ffd76b')
    c.outline(); return c


def background():
    W, H = 320, 180
    img = Image.new('RGBA', (W, H))
    rnd = random.Random(7)
    floor_y = 96
    # stone brick wall
    for y in range(floor_y):
        for x in range(W):
            img.putpixel((x, y), hexc('#2a2533'))
    bh, bw = 12, 24
    for row in range(floor_y // bh + 1):
        off = (bw // 2) * (row % 2)
        for col in range(-1, W // bw + 2):
            x0, y0 = col * bw + off, row * bh
            base = rnd.choice(['#3a3445', '#363041', '#3e3849', '#332e3d', '#403a4c'])
            for y in range(y0 + 1, min(y0 + bh, floor_y)):
                for x in range(x0 + 1, x0 + bw):
                    if 0 <= x < W:
                        col_ = hexc(base)
                        if y == y0 + 1 or x == x0 + 1:
                            col_ = tuple(min(255, v + 10) for v in col_[:3]) + (255,)
                        if y == y0 + bh - 1 or x == x0 + bw - 1:
                            col_ = tuple(max(0, v - 10) for v in col_[:3]) + (255,)
                        if rnd.random() < 0.04:
                            col_ = tuple(max(0, v - 14) for v in col_[:3]) + (255,)
                        img.putpixel((x, y), col_)
    # cracks / moss
    for _ in range(14):
        x, y = rnd.randrange(W), rnd.randrange(10, floor_y - 10)
        for _ in range(rnd.randrange(4, 10)):
            if 0 <= x < W and 0 <= y < floor_y:
                img.putpixel((x, y), hexc('#1f1b26'))
            x += rnd.choice((-1, 0, 1)); y += 1
    for _ in range(60):
        x, y = rnd.randrange(W), rnd.randrange(floor_y - 30, floor_y)
        img.putpixel((x, y), hexc(rnd.choice(['#3f5236', '#4a5f3d', '#36472f'])))
    # banners
    for bx in (104, 204):
        for y in range(10, 50):
            for x in range(bx, bx + 14):
                c = '#6e1f1c' if x < bx + 11 else '#4e1513'
                if y > 44 and abs(x - (bx + 7)) < (y - 44):
                    continue
                img.putpixel((x, y), hexc(c))
        for x in range(bx - 1, bx + 15):
            img.putpixel((x, 9), hexc('#8c6a42'))
        for y in range(18, 36):
            img.putpixel((bx + 6, y), hexc('#d9b44a'))
            img.putpixel((bx + 7, y), hexc('#d9b44a'))
        for x in range(bx + 3, bx + 11):
            img.putpixel((x, 23), hexc('#d9b44a'))
    # floor
    for y in range(floor_y, H):
        t = (y - floor_y) / (H - floor_y)
        for x in range(W):
            img.putpixel((x, y), hexc('#24202b'))
    rows = [floor_y, floor_y + 9, floor_y + 20, floor_y + 34, floor_y + 52, H]
    for r in range(len(rows) - 1):
        y0, y1 = rows[r], rows[r + 1]
        tw = 30 + r * 8
        off = rnd.randrange(tw)
        for col in range(-1, W // tw + 2):
            x0 = col * tw - off
            shade = rnd.choice(['#2e2937', '#2b2633', '#322c3c', '#29242f'])
            for y in range(y0 + 1, y1):
                for x in range(x0 + 1, x0 + tw):
                    if 0 <= x < W:
                        c = hexc(shade)
                        if y == y0 + 1:
                            c = tuple(min(255, v + 8) for v in c[:3]) + (255,)
                        img.putpixel((x, y), c)
    # wall base shadow
    for x in range(W):
        for i in range(3):
            img.putpixel((x, floor_y + i), hexc('#16131b'))
    # vignette
    for y in range(H):
        for x in range(W):
            dx, dy = (x - W / 2) / (W / 2), (y - H / 2) / (H / 2)
            d = min(1.0, (dx * dx + dy * dy) ** 0.5)
            k = max(0.0, d - 0.55) * 1.2
            r, g, b, a = img.getpixel((x, y))
            img.putpixel((x, y), (int(r * (1 - k)), int(g * (1 - k)), int(b * (1 - k)), 255))
    c = Canvas(W, H); c.img = img; return c


if __name__ == '__main__':
    for name, fn in [('knight', knight), ('cleric', cleric), ('mage', mage),
                     ('ghoul', ghoul), ('skeleton', skeleton), ('skeleton_archer', lambda: skeleton(True)),
                     ('cultist', cultist), ('wraith', wraith), ('necromancer', necromancer)]:
        fn().save(f'Units/{name}.png')
    torch().save('Props/torch.png')
    campfire().save('Props/campfire.png')
    darkbolt().save('FX/darkbolt.png')
    background().save('Backgrounds/crypt.png')
    for name, fn in [('fireball', fireball), ('shard', shard), ('arrow', arrow), ('sparkle', sparkle), ('slash', slash)]:
        fn().save(f'FX/{name}.png')
    for k in ['slash', 'fire', 'ice', 'holy', 'attack', 'buff', 'heal', 'guard', 'arrow_down']:
        icon(k).save(f'UI/icon_{k}.png')
    print('done')
