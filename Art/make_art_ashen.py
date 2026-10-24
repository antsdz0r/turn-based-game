"""Ashenbound art: new heroes/enemies, portraits, icons. Run: python3 make_art_ashen.py <Art dir>"""
import os, sys, shutil
from PIL import Image, ImageDraw
import make_art as base
from make_art import Canvas, hexc, OUTLINE

OUT = sys.argv[1]


def shadowblade():
    c = Canvas(32, 32)
    C, CS, CD = '#2f4a52', '#22363d', '#16242a'
    T, K, B = '#3fa39a', '#d9a988', '#c8ccd4'
    # legs, crouched
    c.rect(11, 22, 13, 28, CS); c.rect(16, 23, 18, 28, CD); c.hline(10, 13, 29, '#1a1a22'); c.hline(16, 19, 29, '#1a1a22')
    # body
    c.rect(11, 13, 18, 22, C); c.rect(16, 13, 18, 22, CS)
    c.hline(11, 18, 19, '#4a3424'); c.px(14, 19, '#c9a55a')
    # scarf
    c.rect(12, 11, 18, 13, T); c.pts([(10, 12), (9, 13), (8, 14), (10, 13)], T); c.px(8, 15, '#2b7d75')
    # hood + masked face
    c.rect(12, 4, 18, 11, C); c.rect(17, 4, 18, 11, CS); c.clear(12, 4); c.clear(18, 4); c.px(11, 6, C); c.px(11, 7, C)
    c.rect(15, 7, 18, 9, K); c.px(17, 8, '#101418'); c.rect(15, 9, 18, 10, T)
    # arms + daggers
    c.rect(18, 15, 21, 17, CS); c.rect(21, 15, 22, 16, K)
    c.pts([(23, 14), (24, 13), (25, 12), (26, 11)], B); c.px(22, 15, '#c9a55a')
    c.rect(8, 16, 10, 18, CS); c.px(8, 19, K)
    c.pts([(7, 20), (6, 21), (5, 22)], B)
    c.outline(); return c


def ranger():
    c = Canvas(32, 32)
    G, GS, GD = '#4f7a3a', '#3b5e2b', '#28401d'
    L, K, W = '#7a5532', '#d9a988', '#8c6a42'
    # legs
    c.rect(12, 23, 14, 28, '#5a4030'); c.rect(16, 23, 18, 28, '#46321f'); c.hline(11, 14, 29, '#2e2018'); c.hline(16, 19, 29, '#2e2018')
    # cloak and tunic
    c.rect(11, 12, 19, 23, G); c.rect(17, 12, 19, 23, GS); c.rect(13, 14, 16, 21, L); c.hline(11, 19, 19, '#3a2a1a')
    c.pts([(10, 22), (10, 23), (20, 22)], GD)
    # quiver on the back
    c.rect(8, 9, 10, 17, '#6b4a2b'); c.pts([(8, 7), (9, 6), (10, 7), (9, 8)], '#e6e0cf'); c.px(10, 6, '#b8322a')
    # hood + face
    c.rect(12, 4, 18, 12, G); c.rect(17, 4, 18, 12, GS); c.clear(12, 4); c.clear(18, 4)
    c.rect(14, 7, 18, 11, K); c.px(17, 9, '#1e1420'); c.hline(14, 18, 6, GD); c.px(14, 11, '#b98563')
    # bow (front)
    c.vline(24, 9, 23, W); c.pts([(23, 8), (22, 7), (23, 24), (22, 25)], W)
    c.vline(22, 8, 24, '#d9d0b5')
    c.rect(19, 15, 23, 16, GS); c.px(23, 15, K)
    c.hline(18, 26, 14, '#cfc6aa'); c.px(27, 14, '#9aa7b5')
    c.outline(); return c


def bulwark():
    c = Canvas(32, 32)
    I, IS, ID = '#4a4552', '#36323d', '#24212a'
    E = '#ff8a2c'
    # thick legs
    c.rect(9, 23, 13, 29, IS); c.rect(16, 23, 20, 29, ID)
    # massive body
    c.rect(7, 10, 21, 23, I); c.rect(18, 10, 21, 23, IS)
    c.pts([(10, 14), (11, 15), (12, 16), (12, 17), (11, 18)], E)  # ember cracks
    c.pts([(15, 19), (16, 20), (17, 20)], E)
    # head
    c.rect(11, 4, 18, 10, IS); c.rect(12, 6, 17, 7, ID); c.px(15, 6, E); c.px(16, 6, '#ffd36a')
    c.pts([(10, 4), (19, 4), (10, 5), (19, 5)], ID)
    # tower shield (front)
    c.rect(20, 8, 26, 26, '#5a5466'); c.rect(25, 8, 26, 26, '#403b4a')
    c.hline(20, 26, 8, '#8a8296'); c.vline(20, 8, 26, '#8a8296')
    c.rect(22, 14, 24, 18, '#2a2733'); c.px(23, 16, E)
    c.outline(); c.mirror(); return c


def blight():
    c = Canvas(32, 32)
    G, GS, GD = '#9aae3a', '#76882a', '#526020'
    P = '#d9e86a'
    # squat bloated body
    for y in range(14, 28):
        half = 9 - abs(y - 21) // 2
        c.hline(15 - half, 15 + half, y, G)
        c.hline(15 + half - 3, 15 + half, y, GS)
    c.hline(7, 23, 28, GD)
    # head with wide mouth
    c.rect(15, 10, 24, 17, G); c.rect(22, 10, 24, 17, GS)
    c.pts([(17, 11), (21, 11)], '#ffef6a'); c.pts([(17, 12), (21, 12)], '#1a1408')
    c.hline(16, 24, 15, '#3a1a1a'); c.pts([(18, 16), (21, 16)], '#e8e2c8')
    c.pts([(22, 17), (22, 18), (23, 19)], '#c8f04a')  # drool
    # pustules
    for (x, y) in [(10, 18), (13, 22), (9, 24), (16, 25), (12, 16)]:
        c.px(x, y, P); c.px(x + 1, y, GD)
    # legs
    c.rect(9, 27, 11, 29, GD); c.rect(19, 27, 21, 29, GD)
    c.outline(); c.mirror(); return c


def hollow_king():
    c = Canvas(40, 40)
    B, BS, BD = '#ece6d4', '#bdb49c', '#8a826c'
    R, RS = '#5a1f3a', '#3e1428'
    G, E = '#d9b44a', '#ff8a2c'
    # cape
    for i, y in enumerate(range(12, 37)):
        half = min(6 + i // 2, 15)
        c.hline(19 - half, 19 + half - 1, y, R)
        c.hline(19 + half - 5, 19 + half - 1, y, RS)
    c.hline(4, 34, 37, '#2a0f1e')
    # legs
    c.vline(16, 30, 37, B); c.vline(22, 30, 37, BS)
    # ribcage with ember heart
    c.rect(14, 14, 24, 26, '#1a1018')
    for y in (15, 18, 21, 24):
        c.hline(14, 24, y, B); c.px(24, y, BS)
    c.vline(19, 14, 27, BS)
    c.rect(18, 19, 20, 21, E); c.px(19, 20, '#ffe08a')
    # shoulders
    c.rect(10, 12, 28, 14, '#3a3442'); c.pts([(10, 11), (28, 11)], G)
    # skull
    c.rect(15, 4, 23, 12, B); c.rect(22, 4, 23, 12, BS); c.clear(15, 4); c.clear(23, 4)
    c.rect(17, 7, 18, 8, '#140e16'); c.rect(21, 7, 21, 8, '#140e16'); c.px(17, 7, E); c.px(21, 7, E)
    c.hline(17, 22, 11, BD); c.pts([(18, 12), (20, 12), (22, 12)], B)
    # crown
    c.hline(14, 24, 3, G); c.pts([(14, 2), (16, 1), (16, 2), (19, 0), (19, 1), (19, 2), (22, 1), (22, 2), (24, 2)], G)
    c.pts([(16, 3), (19, 3), (22, 3)], E)
    # greatsword
    c.vline(31, 4, 30, '#9aa7b5'); c.vline(32, 4, 30, '#6a7584'); c.px(31, 3, '#cfd8e2')
    c.hline(28, 35, 31, G); c.rect(31, 32, 32, 35, '#4a321c'); c.px(31, 36, G)
    c.rect(27, 26, 30, 30, B)
    c.outline(); c.mirror(); return c


def portrait(sprite_path, bg_hex, crop=(8, 2, 26, 18)):
    """Bust portrait: crop head/shoulders, scale x3, on a gradient with a gold frame. 64x64."""
    spr = Image.open(sprite_path).convert('RGBA').crop(crop)
    spr = spr.resize((spr.width * 3, spr.height * 3), Image.NEAREST)
    size = 64
    img = Image.new('RGBA', (size, size))
    bg = hexc(bg_hex)
    for y in range(size):
        k = 0.35 + 0.45 * (y / size)
        for x in range(size):
            img.putpixel((x, y), (int(bg[0] * k), int(bg[1] * k), int(bg[2] * k), 255))
    ox = (size - spr.width) // 2
    oy = size - spr.height
    img.alpha_composite(spr, (ox, oy))
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, size - 1, size - 1], outline=hexc('#c9a55a'), width=2)
    d.rectangle([2, 2, size - 3, size - 3], outline=hexc('#3a2a1a'), width=1)
    return img


def icon(kind):
    c = Canvas(11, 11)
    P = c.pts
    if kind == 'physical':
        for i in range(2, 9): c.px(i, 10 - i, '#e6ecf4')
        P([(2, 6), (4, 8), (3, 7)], '#c9a55a'); c.px(1, 9, '#8c6a42')
    elif kind == 'lightning':
        P([(6, 0), (5, 1), (5, 2), (4, 3), (4, 4), (3, 5), (4, 5), (5, 5), (6, 5), (6, 6), (5, 7), (5, 8), (4, 9), (4, 10)], '#c9a0ff')
        P([(7, 0), (6, 1), (6, 2), (5, 3), (5, 4), (7, 5), (6, 7)], '#f0e0ff')
    elif kind == 'poison':
        c.rect(3, 4, 7, 9, '#8fd14a'); c.rect(4, 2, 6, 3, '#8fd14a'); c.px(5, 1, '#8fd14a'); P([(4, 6), (6, 7)], '#2a4010')
    elif kind == 'burn':
        c.rect(3, 5, 7, 9, '#ff6a2a'); c.rect(4, 3, 6, 4, '#ff6a2a'); c.px(5, 1, '#ff6a2a'); c.px(5, 2, '#ff6a2a'); c.rect(4, 6, 6, 9, '#ffc04a')
    elif kind == 'bleed':
        P([(5, 1), (4, 3), (6, 3)], '#d0303a'); c.rect(4, 4, 6, 7, '#d0303a'); c.rect(3, 5, 7, 6, '#d0303a'); c.px(4, 5, '#ff8a8a')
    elif kind == 'regen':
        c.rect(4, 1, 6, 9, '#6be07a'); c.rect(1, 4, 9, 6, '#6be07a'); c.px(5, 5, '#d0ffd8')
    elif kind == 'stun':
        for (x, y) in [(1, 3), (5, 1), (9, 3), (3, 7), (7, 7)]:
            c.px(x, y, '#ffd76b'); c.px(x, y + 1, '#ffd76b'); c.px(x + 1, y, '#fff2b0')
    elif kind == 'shield':
        c.rect(2, 1, 8, 6, '#6f9be0'); c.rect(3, 7, 7, 8, '#6f9be0'); c.px(5, 9, '#6f9be0'); c.vline(5, 2, 7, '#cfe0ff')
    elif kind == 'marked':
        c.rect(2, 2, 8, 8, '#ff4a4a'); c.rect(3, 3, 7, 7, (0, 0, 0, 0)); c.px(5, 5, '#ff4a4a'); P([(5, 0), (5, 10), (0, 5), (10, 5)], '#ff4a4a')
    elif kind == 'weakened':
        c.vline(5, 1, 7, '#b07cff'); P([(4, 7), (6, 7), (3, 6), (7, 6), (2, 5), (8, 5)], '#b07cff'); c.hline(2, 8, 9, '#b07cff')
    elif kind == 'empowered':
        c.vline(5, 2, 9, '#ffb347'); P([(4, 3), (6, 3), (3, 4), (7, 4), (2, 5), (8, 5)], '#ffb347')
    elif kind == 'taunt':
        c.rect(3, 1, 7, 3, '#ff9a5a'); c.rect(4, 4, 6, 6, '#ff9a5a'); c.px(5, 8, '#ff9a5a'); c.px(5, 9, '#ff9a5a')
    elif kind == 'slowed':
        c.rect(2, 1, 8, 1, '#7fd4ff'); c.rect(2, 9, 8, 9, '#7fd4ff'); P([(3, 2), (7, 2), (4, 3), (6, 3), (5, 4), (5, 5), (4, 6), (6, 6), (3, 7), (7, 7), (3, 8), (7, 8)], '#7fd4ff')
        c.rect(4, 7, 6, 8, '#bfeaff')
    elif kind == 'defending':
        c.rect(2, 1, 8, 6, '#a9a9b5'); c.rect(3, 7, 7, 8, '#a9a9b5'); c.px(5, 9, '#a9a9b5'); c.hline(3, 7, 3, '#5a5466')
    elif kind == 'riposte':
        for i in range(1, 10): c.px(i, i, '#e6ecf4'); c.px(10 - i, i, '#ffd76b')
    elif kind == 'guarded':
        c.rect(2, 1, 8, 6, '#8fb3ff'); c.rect(3, 7, 7, 8, '#8fb3ff'); c.px(5, 9, '#8fb3ff'); c.rect(4, 3, 6, 5, '#ffffff')
    # map nodes
    elif kind == 'node_battle':
        for i in range(1, 9): c.px(i + 1, 9 - i, '#e6ecf4'); c.px(i, i, '#e6ecf4')
        P([(1, 7), (3, 9), (7, 9), (9, 7)], '#c9a55a')
    elif kind == 'node_elite':
        c.rect(2, 2, 8, 7, '#ece6d4'); P([(3, 4), (4, 4), (6, 4), (7, 4)], '#140e16'); c.hline(3, 7, 8, '#ece6d4'); P([(4, 9), (6, 9)], '#ece6d4')
        P([(1, 1), (9, 1), (1, 0), (9, 0)], '#ff6a4a')
    elif kind == 'node_treasure':
        c.rect(1, 4, 9, 9, '#8c6a42'); c.rect(1, 2, 9, 4, '#a57d4e'); c.hline(1, 9, 5, '#d9b44a'); c.rect(4, 4, 6, 6, '#ffd76b')
    elif kind == 'node_shop':
        c.rect(3, 3, 7, 9, '#ffd76b'); c.rect(2, 4, 8, 8, '#ffd76b'); c.rect(4, 4, 6, 8, '#d9a42a'); c.px(5, 2, '#ffd76b')
    elif kind == 'node_rest':
        c.rect(2, 8, 8, 9, '#6b4a2b'); c.rect(4, 4, 6, 7, '#ff8a2c'); c.px(5, 2, '#ffb347'); c.px(5, 3, '#ffb347'); c.px(5, 6, '#ffe08a')
    elif kind == 'node_event':
        P([(3, 1), (4, 1), (5, 1), (6, 1), (7, 2), (7, 3), (6, 4), (5, 5), (5, 6), (5, 9)], '#bfeaff'); c.px(2, 2, '#bfeaff')
    elif kind == 'node_boss':
        c.hline(1, 9, 8, '#ffd76b'); c.rect(1, 5, 9, 7, '#ffd76b'); P([(1, 3), (1, 4), (5, 2), (5, 3), (5, 4), (9, 3), (9, 4)], '#ffd76b')
        P([(3, 6), (5, 6), (7, 6)], '#ff4a4a')
    # items
    elif kind == 'weapon':
        for i in range(2, 9): c.px(i, 10 - i, '#cfd8e2')
        P([(2, 6), (4, 8), (3, 7)], '#c9a55a'); c.px(1, 9, '#8c6a42')
    elif kind == 'armor':
        c.rect(2, 2, 8, 8, '#8a97ab'); c.rect(4, 1, 6, 1, '#8a97ab'); c.rect(4, 3, 6, 7, '#b8322a'); c.hline(2, 8, 9, '#56627a')
    elif kind == 'trinket':
        c.hline(3, 7, 1, '#c9a55a'); P([(2, 2), (8, 2), (2, 3), (8, 3)], '#c9a55a'); c.rect(3, 4, 7, 8, '#4fa8d8'); c.rect(4, 5, 5, 6, '#bfeaff')
    elif kind == 'potion':
        c.rect(4, 1, 6, 3, '#a9a9b5'); c.rect(2, 4, 8, 9, '#d0303a'); c.rect(3, 5, 4, 6, '#ff9a9a')
    elif kind == 'tonic':
        c.rect(4, 1, 6, 3, '#a9a9b5'); c.rect(2, 4, 8, 9, '#ff8a2c'); c.rect(3, 5, 4, 6, '#ffe08a')
    elif kind == 'panacea':
        c.rect(4, 1, 6, 3, '#a9a9b5'); c.rect(2, 4, 8, 9, '#6be07a'); c.rect(3, 5, 4, 6, '#d0ffd8')
    elif kind == 'bomb':
        c.rect(2, 4, 8, 9, '#3a3442'); c.rect(3, 3, 7, 10, '#3a3442'); c.px(7, 2, '#8c6a42'); c.px(8, 1, '#ffb347'); c.px(4, 5, '#6a6478')
    elif kind == 'flask':
        c.rect(4, 1, 6, 3, '#a9a9b5'); c.rect(3, 4, 7, 9, '#7fd4ff'); c.rect(2, 6, 8, 9, '#7fd4ff'); c.rect(3, 6, 4, 7, '#e9f9ff')
    elif kind == 'feather':
        for i in range(1, 9): c.px(i + 1, 9 - i, '#ffb347')
        P([(4, 4), (5, 3), (6, 2), (5, 5), (6, 4), (7, 3), (3, 6), (4, 7)], '#ff6a2a'); c.px(1, 10, '#ffe08a')
    elif kind == 'relic':
        P([(5, 0), (4, 1), (6, 1), (3, 2), (7, 2), (2, 3), (8, 3)], '#ffffff'); c.rect(2, 4, 8, 5, '#ffffff'); P([(3, 6), (7, 6), (4, 7), (6, 7), (5, 8), (5, 9)], '#ffffff')
        c.rect(4, 3, 6, 5, '#e0e0e0')
    elif kind == 'coin':
        c.rect(3, 1, 7, 9, '#ffd76b'); c.rect(2, 2, 8, 8, '#ffd76b'); c.rect(4, 3, 6, 7, '#d9a42a'); c.px(5, 5, '#fff2b0')
    elif kind == 'move':
        c.hline(1, 9, 3, '#e6ecf4'); P([(8, 2), (8, 4), (7, 1), (7, 5)], '#e6ecf4')
        c.hline(1, 9, 7, '#e6ecf4'); P([(2, 6), (2, 8), (3, 5), (3, 9)], '#e6ecf4')
    elif kind == 'defend':
        c.rect(2, 1, 8, 6, '#a9a9b5'); c.rect(3, 7, 7, 8, '#a9a9b5'); c.px(5, 9, '#a9a9b5'); c.vline(5, 2, 7, '#ffffff')
    elif kind == 'bag':
        c.rect(2, 4, 8, 9, '#8c6a42'); c.rect(3, 3, 7, 3, '#8c6a42'); c.hline(4, 6, 2, '#c9a55a'); c.px(5, 6, '#c9a55a')
    elif kind == 'charging':
        for i in range(0, 11, 2): c.px(i, 10 - i // 2 * 2 if i < 6 else i - 2, '#ffa040')
        c.rect(4, 3, 6, 7, '#ffa040'); c.px(5, 1, '#fff2b0'); c.px(5, 9, '#fff2b0')
    elif kind == 'heal_ally':
        c.rect(4, 1, 6, 9, '#6be07a'); c.rect(1, 4, 9, 6, '#6be07a')
    elif kind == 'summon':
        c.rect(2, 2, 8, 7, '#58ff8a'); P([(3, 4), (4, 4), (6, 4), (7, 4)], '#0b2a14'); c.hline(3, 7, 8, '#58ff8a'); P([(4, 9), (6, 9)], '#58ff8a')
    c.outline(); return c


if __name__ == '__main__':
    U = os.path.join(OUT, 'Units')
    shadowblade().save('Units/shadowblade.png')
    ranger().save('Units/ranger.png')
    bulwark().save('Units/bulwark.png')
    blight().save('Units/blight.png')
    hollow_king().save('Units/hollow_king.png')
    for hero_id, src in [('vanguard', 'knight'), ('arcanist', 'mage'), ('warden', 'cleric')]:
        shutil.copy(os.path.join(U, f'{src}.png'), os.path.join(U, f'{hero_id}.png'))
    os.makedirs(os.path.join(OUT, 'Portraits'), exist_ok=True)
    for hero_id, color, crop in [('vanguard', '#7a8ca8', (7, 1, 25, 17)), ('arcanist', '#7a56b8', (8, 0, 26, 16)),
                                 ('warden', '#c9b26b', (8, 1, 26, 17)), ('shadowblade', '#3fa39a', (8, 2, 26, 18)),
                                 ('ranger', '#6f9a4a', (8, 2, 26, 18))]:
        portrait(os.path.join(U, f'{hero_id}.png'), color, crop).save(os.path.join(OUT, 'Portraits', f'{hero_id}.png'))
    kinds = ['physical', 'lightning', 'poison', 'burn', 'bleed', 'regen', 'stun', 'shield', 'marked', 'weakened', 'empowered',
             'taunt', 'slowed', 'defending', 'riposte', 'guarded', 'node_battle', 'node_elite', 'node_treasure', 'node_shop',
             'node_rest', 'node_event', 'node_boss', 'weapon', 'armor', 'trinket', 'potion', 'tonic', 'panacea', 'bomb', 'flask',
             'feather', 'relic', 'coin', 'move', 'defend', 'bag', 'charging', 'heal_ally', 'summon']
    for k in kinds:
        icon(k).save(f'UI/icon_{k}.png')
    print('done')
