"""Shared tools for the 'lit pixel' art style: palette, shading ramps, sprite builder, quantizer."""
import numpy as np
from PIL import Image, ImageDraw
from scipy.ndimage import distance_transform_edt

def hx(h):
    h = h.lstrip('#')
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))

# The Ashen palette: cold violet shadows, warm ember lights, teal magic, moss, skin.
PALETTE_HEX = """
0b0910 141019 1d1726 282033 352a45 433758 554a6d 6b6386 8a86a6 b0aec8 d8d6e6 f4f2fa
2a1414 4a1e1e 74291f a63d25 d85f2a f28a3a ffb85a ffe09a fff6d8
1d3340 2a5560 3f8a8a 6fd0c0 b8f5e4
3a2a18 5c4228 8a6640 b89262
6a2848 a8406a
233a22 3d6230 6f9a40 b3d05a
8f5a48 c98a6b e8b796
""".split()
PALETTE = [hx(c) for c in PALETTE_HEX]
PAL = np.array(PALETTE, dtype=np.float32)
BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0 - 0.5

R = {  # 4-step ramps, dark → light
    'metal': ['352a45', '6b6386', 'b0aec8', 'f4f2fa'],
    'iron': ['141019', '352a45', '554a6d', '8a86a6'],
    'red': ['2a1414', '74291f', 'a63d25', 'd85f2a'],
    'crimson': ['1d1726', '4a1e1e', '74291f', 'a63d25'],
    'blue': ['141019', '1d3340', '2a5560', '3f8a8a'],
    'teal': ['1d3340', '2a5560', '3f8a8a', '6fd0c0'],
    'gold': ['5c4228', '8a6640', 'ffb85a', 'ffe09a'],
    'purple': ['1d1726', '433758', '6a2848', 'a8406a'],
    'violet': ['141019', '282033', '433758', '6b6386'],
    'bone': ['554a6d', '8a86a6', 'd8d6e6', 'fff6d8'],
    'cream': ['8a86a6', 'b0aec8', 'd8d6e6', 'f4f2fa'],
    'moss': ['141019', '233a22', '3d6230', '6f9a40'],
    'leaf': ['233a22', '3d6230', '6f9a40', 'b3d05a'],
    'wood': ['3a2a18', '5c4228', '8a6640', 'b89262'],
    'leather': ['2a1414', '3a2a18', '5c4228', '8a6640'],
    'skin': ['8f5a48', 'c98a6b', 'e8b796', 'ffe09a'],
    'pale': ['554a6d', '8a86a6', 'b0aec8', 'd8d6e6'],
    'ghoul': ['1d3340', '433758', '6b6386', '8a86a6'],
    'ember': ['74291f', 'd85f2a', 'f28a3a', 'ffb85a'],
    'fire': ['d85f2a', 'f28a3a', 'ffe09a', 'fff6d8'],
    'rock': ['0b0910', '1d1726', '282033', '433758'],
    'sea': ['141019', '1d3340', '2a5560', '6fd0c0'],
    'flesh': ['4a1e1e', '74291f', '8f5a48', 'c98a6b'],
    'hair': ['2a1414', '4a1e1e', '74291f', 'a63d25'],
    'dark': ['0b0910', '141019', '1d1726', '282033'],
}


class Ink:
    """ImageDraw wrapper that fills shapes by default."""
    def __init__(self, d): self.d = d
    def __getattr__(self, name):
        fn = getattr(self.d, name)
        def call(*a, **k):
            if name in ('polygon', 'rectangle', 'ellipse', 'rounded_rectangle', 'pieslice', 'chord') and 'fill' not in k:
                k['fill'] = 255
            if name in ('line', 'arc', 'point') and 'fill' not in k:
                k['fill'] = 255
            return fn(*a, **k)
        return call


class Spr:
    def __init__(self, w=64, h=64, light=(-0.6, -0.8)):
        self.w, self.h, self.light = w, h, light
        self.px = np.zeros((h, w, 4), np.uint8)

    def mask(self, fn):
        m = Image.new('L', (self.w, self.h), 0)
        fn(Ink(ImageDraw.Draw(m)))
        return np.asarray(m) > 0

    def part(self, fn, ramp, round_=0.55, contrast=1.0, cast=True, dither=True, light=None):
        """Shaded part: directional light + rounded volume (distance to edge) + ordered dither."""
        if isinstance(ramp, str): ramp = R[ramp]
        m = self.mask(fn)
        ys, xs = np.nonzero(m)
        if len(xs) == 0: return m
        lx, ly = light or self.light
        x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
        u = (xs - x0) / max(1, x1 - x0) - 0.5
        v = (ys - y0) / max(1, y1 - y0) - 0.5
        edt = distance_transform_edt(m)[ys, xs]
        vol = edt / max(1.0, edt.max())
        lum = 0.5 - (u * lx + v * ly) * 0.95 * contrast + (vol - 0.45) * round_
        if dither: lum = lum + BAYER[ys % 4, xs % 4] * 0.2
        k = np.clip((lum * len(ramp)).astype(int), 0, len(ramp) - 1)
        if cast:  # ambient occlusion: what's under the new part (1px down/right of it) gets darker
            shadow = np.zeros_like(m)
            shadow[1:, :] |= m[:-1, :]
            shadow[:, 1:] |= m[:, :-1]
            shadow &= ~m & (self.px[..., 3] > 0)
            self.px[shadow, :3] = (self.px[shadow, :3] * 0.68).astype(np.uint8)
        for i, c in enumerate(ramp):
            sel = k == i
            self.px[ys[sel], xs[sel], :3] = hx(c)
            self.px[ys[sel], xs[sel], 3] = 255
        return m

    def flat(self, fn, color):
        if color in R: color = R[color][2]   # a ramp name means its light-mid tone
        m = self.mask(fn)
        self.px[m, :3] = hx(color)
        self.px[m, 3] = 255
        return m

    def dot(self, x, y, color):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[y, x, :3] = hx(color); self.px[y, x, 3] = 255

    def dots(self, pts, color):
        for x, y in pts: self.dot(x, y, color)

    def glow(self, x, y, color, r=3.0, strength=0.6):
        """Soft emissive bloom painted onto existing pixels (for eyes, orbs, embers)."""
        yy, xx = np.mgrid[0:self.h, 0:self.w]
        f = np.clip(1 - np.hypot(xx - x, yy - y) / r, 0, 1) ** 2 * strength
        a = self.px[..., 3] > 0
        c = np.array(hx(color), np.float32)
        base = self.px[..., :3].astype(np.float32)
        self.px[..., :3] = np.where(a[..., None], base + (c - base) * f[..., None], base).astype(np.uint8)

    def finish(self, rim='ffb85a', rim_side=-1, rim_strength=0.55, quantize=True):
        a = self.px[..., 3] > 0
        if quantize:
            flat = self.px[..., :3].reshape(-1, 1, 3).astype(np.float32)
            idx = np.argmin(((flat - PAL[None]) ** 2).sum(-1), axis=1)
            self.px[..., :3] = PAL[idx].reshape(self.h, self.w, 3).astype(np.uint8)
        # Rim light on the edge facing the light source
        if rim:
            shifted = np.roll(a, -rim_side, axis=1)
            shifted[:, 0 if rim_side < 0 else -1] = False
            rimpx = a & ~shifted
            rc = np.array(hx(rim), np.float32)
            base = self.px[..., :3].astype(np.float32)
            self.px[rimpx, :3] = (base[rimpx] + (rc - base[rimpx]) * rim_strength).astype(np.uint8)
            if quantize:
                flat = self.px[rimpx, :3].reshape(-1, 1, 3).astype(np.float32)
                idx = np.argmin(((flat - PAL[None]) ** 2).sum(-1), axis=1)
                self.px[rimpx, :3] = PAL[idx].astype(np.uint8)
        # Selective outline: each outline pixel is a very dark version of the colour it borders.
        pad = np.pad(a, 1)
        edge = (pad[:-2, 1:-1] | pad[2:, 1:-1] | pad[1:-1, :-2] | pad[1:-1, 2:]) & ~a
        src = self.px[..., :3].astype(np.float32)
        acc = np.zeros_like(src); cnt = np.zeros(a.shape, np.float32)
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            sh = np.roll(np.roll(src * a[..., None], dy, 0), dx, 1)
            sa = np.roll(np.roll(a, dy, 0), dx, 1)
            acc += sh; cnt += sa
        col = acc / np.maximum(cnt, 1)[..., None]
        dark = col * 0.22 + np.array(hx('0b0910'), np.float32) * 0.78
        flat = dark[edge].reshape(-1, 1, 3)
        idx = np.argmin(((flat - PAL[None]) ** 2).sum(-1), axis=1)
        self.px[edge, :3] = PAL[idx].astype(np.uint8)
        self.px[edge, 3] = 255
        return self

    def image(self, flip=False):
        img = Image.fromarray(self.px, 'RGBA')
        return img.transpose(Image.FLIP_LEFT_RIGHT) if flip else img


def quantize_rgb(rgb, strength=22.0):
    """Ordered-dither a float RGB image into the palette."""
    h, w, _ = rgb.shape
    ty = np.tile(BAYER, (h // 4 + 1, w // 4 + 1))[:h, :w]
    d = np.clip(rgb + ty[..., None] * strength, 0, 255)
    out = np.empty_like(d)
    for y0 in range(0, h, 64):   # chunked to keep memory low
        flat = d[y0:y0 + 64].reshape(-1, 1, 3)
        idx = np.argmin(((flat - PAL[None]) ** 2).sum(-1), axis=1)
        out[y0:y0 + 64] = PAL[idx].reshape(-1, w, 3)
    return out
