"""Weapons, portraits and UI textures in the lit-pixel style."""
import numpy as np
from PIL import Image, ImageDraw
from style_kit import Spr, hx
import units_v2 as U

def weapon(kind):
    s = Spr(24, 48, light=(-0.7, -0.6))
    if kind == 'sword':
        s.part(lambda d: d.polygon([(10, 2), (14, 2), (14, 34), (10, 34)]), 'cream', round_=0.2)
        s.part(lambda d: d.polygon([(10, 2), (12, 0), (14, 2)]), 'cream')
        s.flat(lambda d: d.line([(12, 3), (12, 33)]), 'fff6d8')
        s.part(lambda d: d.rectangle([4, 34, 19, 37]), 'gold', round_=0)
        s.part(lambda d: d.rectangle([10, 38, 13, 44]), 'leather'); s.part(lambda d: d.ellipse([9, 43, 14, 47]), 'gold')
    elif kind == 'greatsword':
        s.part(lambda d: d.polygon([(8, 4), (15, 4), (15, 34), (8, 34)]), 'cream', round_=0.2)
        s.part(lambda d: d.polygon([(8, 4), (11, 0), (15, 4)]), 'cream')
        s.flat(lambda d: d.line([(11, 4), (11, 33)], width=2), 'fff6d8')
        s.part(lambda d: d.polygon([(1, 34), (22, 34), (20, 38), (3, 38)]), 'gold', round_=0)
        s.part(lambda d: d.rectangle([10, 38, 13, 45]), 'leather'); s.part(lambda d: d.ellipse([9, 44, 14, 47]), 'gold')
    elif kind == 'dagger':
        s.part(lambda d: d.polygon([(10, 18), (14, 18), (13, 36), (11, 36)]), 'cream')
        s.part(lambda d: d.polygon([(10, 18), (12, 12), (14, 18)]), 'cream')
        s.part(lambda d: d.rectangle([7, 36, 17, 38]), 'gold', round_=0)
        s.part(lambda d: d.rectangle([10, 39, 13, 45]), 'violet')
    elif kind == 'mace':
        s.part(lambda d: d.rectangle([10, 14, 13, 46]), 'wood')
        s.part(lambda d: d.ellipse([5, 4, 18, 17]), 'gold')
        s.dots([(11, 2), (11, 18), (3, 10), (19, 10), (5, 4), (17, 4)], 'ffb85a')
        s.part(lambda d: d.rectangle([9, 38, 14, 40]), 'gold', round_=0)
    elif kind == 'axe':
        s.part(lambda d: d.rectangle([10, 8, 13, 47]), 'wood')
        s.part(lambda d: d.polygon([(12, 2), (22, 0), (24, 12), (20, 20), (12, 16)]), 'metal', round_=0.4)
        s.part(lambda d: d.polygon([(12, 4), (4, 2), (2, 10), (12, 14)]), 'metal', round_=0.4)
        s.flat(lambda d: d.line([(22, 1), (24, 12), (20, 19)]), 'f4f2fa')
        s.part(lambda d: d.rectangle([9, 40, 14, 42]), 'gold', round_=0)
    elif kind == 'club':
        s.part(lambda d: d.polygon([(9, 46), (14, 46), (18, 6), (6, 8)]), 'wood')
        s.part(lambda d: d.ellipse([4, 2, 20, 16]), 'bone')
        s.dots([(9, 20), (14, 26), (10, 32)], '5c4228')
    return s.finish(rim=None)

def claw_fx():
    img = Image.new('RGBA', (16, 16)); d = ImageDraw.Draw(img)
    for i in range(3):
        d.line([(3 + i * 4, 1), (1 + i * 4, 14)], fill=hx('fff6d8') + (255,), width=1)
    return img

def portrait(hero):
    """Head-and-shoulders crop of the sprite on an ember-lit vignette, 64x64."""
    spr = U.HEROES[hero]().image()
    a = np.asarray(spr)[..., 3] > 0
    ys, xs = np.nonzero(a)
    top = ys.min()
    cx = int(np.median(xs[ys < top + 22]))
    dy = {'arcanist': 9, 'vanguard': 3, 'warden': 2}.get(hero, 0)
    y0 = max(0, top - 2 + dy)
    box = (cx - 16, y0, cx + 16, y0 + 32)
    crop = spr.crop(box).resize((64, 64), Image.NEAREST)
    bg = Image.new('RGBA', (64, 64))
    yy, xx = np.mgrid[0:64, 0:64]
    r = np.hypot(xx - 30, yy - 26) / 44
    c0, c1 = np.array(hx('433758'), float), np.array(hx('0b0910'), float)
    arr = (c0 + (c1 - c0) * np.clip(r, 0, 1)[..., None]).astype(np.uint8)
    bg = Image.fromarray(np.dstack([arr, np.full((64, 64), 255, np.uint8)]), 'RGBA')
    bg.alpha_composite(crop)
    d = ImageDraw.Draw(bg)
    d.rectangle([0, 0, 63, 63], outline=hx('8a6640') + (255,), width=2)
    return bg

def frame_texture():
    """9-slice panel: dark glass, double gold rule, diamond studs that sit on the corners (expand margin 6)."""
    S, M = 48, 6
    img = Image.new('RGBA', (S, S)); d = ImageDraw.Draw(img)
    d.rounded_rectangle([M, M, S - 1 - M, S - 1 - M], 6, fill=(11, 9, 16, 236), outline=hx('8a6640') + (255,), width=2)
    d.rounded_rectangle([M + 5, M + 5, S - 1 - M - 5, S - 1 - M - 5], 3, outline=hx('5c4228') + (255,), width=1)
    for cx, cy in ((M, M), (S - 1 - M, M), (M, S - 1 - M), (S - 1 - M, S - 1 - M)):
        d.polygon([(cx, cy - 5), (cx + 5, cy), (cx, cy + 5), (cx - 5, cy)], fill=hx('ffb85a') + (255,), outline=hx('3a2a18') + (255,))
        d.point((cx - 1, cy - 1), fill=hx('ffe09a') + (255,))
    return img

def bar_texture(c1, c2):
    """Vertical bar fill: highlight line on top, darker bottom."""
    img = Image.new('RGBA', (4, 10))
    for y in range(10):
        t = y / 9
        col = tuple(int(c2[k] + (c1[k] - c2[k]) * t) for k in range(3))
        if y == 1: col = tuple(min(255, int(v * 1.25 + 30)) for v in col)
        for x in range(4): img.putpixel((x, y), col + (255,))
    return img

def segments():
    img = Image.new('RGBA', (12, 4))
    for y in range(4): img.putpixel((11, y), (0, 0, 0, 110))
    return img

def pip(state):
    img = Image.new('RGBA', (11, 11)); d = ImageDraw.Draw(img)
    col = {'on': ('ffb85a', 'ffe09a'), 'off': ('282033', '433758'), 'broken': ('d85f2a', 'f28a3a')}[state]
    d.polygon([(5, 0), (10, 5), (5, 10), (0, 5)], fill=hx(col[0]) + (255,), outline=hx('0b0910') + (255,))
    d.point((4, 3), fill=hx(col[1]) + (255,)); d.point((5, 2), fill=hx(col[1]) + (255,))
    return img

def fog():
    rng = np.random.default_rng(2)
    w, h = 512, 64
    n = np.zeros((h, w))
    for scale, amp in ((64, 1.0), (32, 0.5), (16, 0.25)):
        g = rng.random((h // scale + 2, w // scale + 1))
        g[:, -1] = g[:, 0]
        big = np.asarray(Image.fromarray((g * 255).astype(np.uint8)).resize((w + scale, h + 2 * scale), Image.BICUBIC), float)[:h, :w] / 255
        n += big * amp
    n /= n.max()
    vert = np.sin(np.linspace(0, np.pi, h)) ** 1.5
    a = (np.clip(n - 0.25, 0, 1) * vert[:, None] * 150).astype(np.uint8)
    rgb = np.zeros((h, w, 3), np.uint8) + np.array(hx('b0aec8'), np.uint8)
    return Image.fromarray(np.dstack([rgb, a]), 'RGBA')

if __name__ == '__main__':
    for k in ('sword', 'greatsword', 'dagger', 'mace', 'club', 'axe'): weapon(k).image().save(f'FX/weapon_{k}.png')
    claw_fx().save('FX/claw.png')
    for h in ('vanguard', 'arcanist', 'warden', 'shadowblade', 'ranger', 'berserker', 'hexer'): portrait(h).save(f'Portraits/{h}.png')
    frame_texture().save('UI/frame.png')
    bar_texture(hx('2a5560'), hx('6fd0c0')).save('UI/bar_hero.png')
    bar_texture(hx('74291f'), hx('f28a3a')).save('UI/bar_enemy.png')
    bar_texture(hx('2a5560'), hx('b8f5e4')).save('UI/bar_shield.png')
    segments().save('UI/bar_segments.png')
    for st in ('on', 'off', 'broken'): pip(st).save(f'UI/pip_{st}.png')
    fog().save('FX/fog.png')
    for name, fn in list(U.HEROES.items()) + list(U.ENEMIES.items()):
        fn().image(flip=name in U.ENEMIES).save(f'Units/{name}.png')
    print('ok')
