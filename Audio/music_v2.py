"""Orchestral music for Ashenbound, rendered to seamlessly looping .ogg files.
Instruments are synthesized (ensemble strings, brass, formant choir, Karplus-Strong harp, bells, taiko);
the mix gets a convolution hall whose tail is folded back to the start so the loop has no seam."""
import subprocess
import numpy as np
from scipy.signal import butter, sosfilt, fftconvolve
from scipy.io import wavfile

SR = 44100
rng = np.random.default_rng(3)

def hz(m): return 440.0 * 2 ** ((m - 69) / 12)
def lp(x, f, order=2): return sosfilt(butter(order, f, 'low', fs=SR, output='sos'), x)
def bp(x, f, q=6.0):
    return sosfilt(butter(2, [f / (1 + 1 / (2 * q)), f * (1 + 1 / (2 * q))], 'band', fs=SR, output='sos'), x)

def env(n, a, d, s, r):
    e = np.full(n, s, float)
    na, nd, nr = min(int(a * SR), n), int(d * SR), int(r * SR)
    e[:na] = np.linspace(0, 1, na, endpoint=False)
    nd = min(nd, n - na); e[na:na + nd] = np.linspace(1, s, nd, endpoint=False)
    if 0 < nr < n: e[-nr:] *= np.linspace(1, 0, nr)
    return e

def saw(f, n, phase=0.0):
    t = np.arange(n) / SR
    return 2 * ((f * t + phase) % 1.0) - 1

def strings(m, dur, bright=2200):
    n = int((dur + 0.25) * SR); t = np.arange(n) / SR
    vib = 1 + 0.004 * np.sin(2 * np.pi * 5.2 * t) * np.clip(t / 0.3, 0, 1)
    x = sum(saw(hz(m) * (1 + d) * vib, n, rng.random()) for d in (-0.008, -0.003, 0, 0.004, 0.009)) / 5
    return lp(x, bright) * env(n, 0.03, 0.08, 0.75, 0.22)

def brass(m, dur):
    n = int((dur + 0.2) * SR)
    x = sum(saw(hz(m) * (1 + d), n, rng.random()) for d in (-0.004, 0, 0.005)) / 3
    e = env(n, 0.05, 0.25, 0.8, 0.18)
    return (0.45 * lp(x, 3200) * np.clip(1 - np.arange(n) / (0.35 * SR), 0, 1) + 0.75 * lp(x, 1100)) * e

def choir(m, dur):
    n = int((dur + 0.6) * SR); t = np.arange(n) / SR
    vib = 1 + 0.006 * np.sin(2 * np.pi * 4.6 * t + rng.random() * 6)
    src = sum(saw(hz(m) * (1 + d) * vib, n, rng.random()) for d in (-0.01, 0, 0.011)) / 3
    return (bp(src, 730) + 0.6 * bp(src, 1090) + 0.25 * bp(src, 2440)) * env(n, 0.35, 0.3, 0.85, 0.55) * 2.2

_harp_cache = {}
def harp(m, dur=1.6):
    key = (m, dur)
    if key not in _harp_cache:
        n = int(dur * SR); period = int(SR / hz(m))
        buf = np.random.default_rng(m).uniform(-1, 1, period)
        out = np.zeros(n)
        # vectorised Karplus-Strong: process one period at a time
        for start in range(0, n, period):
            seg = buf.copy()
            out[start:start + period] = seg[:min(period, n - start)]
            buf = 0.4985 * (seg + np.roll(seg, -1))
        _harp_cache[key] = lp(out, 5000) * env(n, 0.002, 0.1, 0.8, 0.4)
    return _harp_cache[key]

def bell(m, dur=2.0):
    n = int(dur * SR); t = np.arange(n) / SR
    f = hz(m)
    x = np.sin(2 * np.pi * f * t) + 0.5 * np.sin(2 * np.pi * f * 2.76 * t) * np.exp(-t * 3) + 0.25 * np.sin(2 * np.pi * f * 5.4 * t) * np.exp(-t * 6)
    return x * np.exp(-t * 1.6) * env(n, 0.003, 0.05, 1, 0.3) * 0.6

def taiko(big=True):
    n = int(0.9 * SR); t = np.arange(n) / SR
    f = 46 + 80 * np.exp(-t * 28)
    body = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * (5 if big else 9))
    slap = lp(rng.uniform(-1, 1, n), 1800) * np.exp(-t * 60)
    return np.tanh(1.6 * body + 0.5 * slap) * (1.0 if big else 0.6)

def frame_drum():
    n = int(0.25 * SR); t = np.arange(n) / SR
    return bp(rng.uniform(-1, 1, n), 900, 2) * np.exp(-t * 30) * 1.6

def swell(dur):
    n = int(dur * SR); noise = rng.uniform(-1, 1, n)
    return (lp(noise, 9000) - lp(noise, 2500)) * np.linspace(0, 1, n) ** 2.5 * 0.6


MEL_A = [
    [(74, 1.5), (76, .5), (77, 1), (81, 1)], [(79, 1.5), (77, .5), (74, 2)],
    [(72, 1), (77, 1), (81, 1.5), (79, .5)], [(76, 2), (72, 1), (76, 1)],
    [(74, 1.5), (76, .5), (77, 1), (81, 1)], [(86, 2), (84, 1), (82, 1)],
    [(81, 1), (79, 1), (77, 1), (76, 1)], [(76, 2), (73, 2)],
]
MEL_B = [
    [(82, 2), (81, 1), (79, 1)], [(81, 3), (77, 1)], [(77, 2), (79, 1), (81, 1)], [(76, 4)],
    [(86, 2), (84, 1), (82, 1)], [(81, 2), (77, 2)], [(79, 1), (81, 1), (82, 1), (84, 1)], [(85, 4)],
]
CHORDS_A = [0, -4, 3, -2]
CHORDS_B = [5, 0, -4, -5]
QUALITY = {0: 'm', -4: 'M', 3: 'M', -2: 'M', 5: 'm', -5: 'M'}
OSTINATO = [0, 0, 7, 0, 12, 0, 7, 10]


def render(name, *, key=0, bpm=132, form="IAABBAAO", drums=1.0, brass_lead=True, bell_lead=False,
           harp_ost=False, choir_all=False, tone=None, verb=2.4, wet=0.55, ost=True, sixteenths=False):
    """form: one letter per 4 bars. I=intro, A=main theme, B=choir bridge, O=outro (loops back to I)."""
    beat = 60 / bpm; bar = 4 * beat
    bars = len(form) * 4
    loop_n = int(bars * bar * SR)
    N = loop_n + int(5 * SR)
    L = np.zeros(N); Rr = np.zeros(N)

    def place(sig, t, pan=0.0, gain=1.0):
        i = int(t * SR)
        if i >= N: return
        sig = sig[:N - i] * gain
        L[i:i + len(sig)] += sig * np.sqrt(0.5 * (1 - pan))
        Rr[i:i + len(sig)] += sig * np.sqrt(0.5 * (1 + pan))

    D = 50 + key
    for b in range(bars):
        t0 = b * bar
        sec = form[b // 4]
        within = b % 4
        prog = CHORDS_B if sec == 'B' else CHORDS_A if sec in 'AI' else [0, 0, -4, -5]
        c = prog[b % 4]
        root = D + c
        third = 3 if QUALITY[c] == 'm' else 4
        theme_bar = (b - form.index(sec) * 4) % 8 if sec in 'AB' else 0

        if ost:
            pattern = OSTINATO * (2 if sixteenths else 1)
            step = beat / (4 if sixteenths else 2)
            for i, s_ in enumerate(pattern):
                if harp_ost:
                    place(harp(root - 12 + s_ + 12), t0 + i * step, 0.4, 0.22)
                else:
                    place(strings(root - 12 + s_, step * 0.9, 1600), t0 + i * step, -0.3, 0.5 if i % 2 == 0 else 0.36)
        if sec != 'I':
            for iv in (0, third, 7): place(strings(root + iv, bar * 0.98, 1400), t0, 0.25, 0.2)
        if sec == 'B' or choir_all or (sec == 'A' and b >= bars // 2):
            for iv in (12, 12 + third, 19): place(choir(root + iv, bar * 0.96), t0, rng.uniform(-0.5, 0.5), 0.15)
        if sec == 'A' and not harp_ost:
            arp = [0, third, 7, 12, 19, 12, 7, third]
            for i, iv in enumerate(arp): place(harp(root + 24 + iv), t0 + i * beat / 2, 0.55, 0.15)
        if sec in 'AB':
            line = (MEL_B if sec == 'B' else MEL_A)[theme_bar]
            tb = t0
            for m, beats in line:
                m += key
                if bell_lead:
                    place(bell(m), tb, -0.1, 0.35)
                    if sec == 'B': place(choir(m - 12, beats * beat), tb, 0.2, 0.12)
                elif sec == 'B':
                    place(strings(m, beats * beat * 0.95, 3200), tb, 0.0, 0.4)
                    place(choir(m - 12, beats * beat), tb, -0.2, 0.13)
                elif brass_lead:
                    place(brass(m - 12, beats * beat * 0.95), tb, -0.1, 0.48)
                    place(strings(m, beats * beat * 0.95, 3500), tb, 0.2, 0.18)
                else:
                    place(strings(m, beats * beat * 0.95, 3000), tb, 0.0, 0.4)
                tb += beats * beat
        if drums > 0:
            for k in range(4):
                tb = t0 + k * beat
                if sec == 'O' and within == 3 and k > 0: continue
                if k in (0, 2): place(taiko(), tb, -0.1, 0.8 * drums)
                if sec != 'I' and k in (1, 3): place(taiko(False), tb, 0.15, 0.65 * drums)
                if drums >= 1.2: place(taiko(False), tb + beat / 2, -0.2, 0.4 * drums)
                if sec in 'AB': place(frame_drum(), tb + beat / 2, 0.4, 0.22 * min(drums, 1.0))
            if within == 3 and sec in 'IAB':
                for k in range(4): place(taiko(False), t0 + 3 * beat + k * beat / 4, 0, 0.5 * drums)
        if within == 3: place(swell(bar), t0, 0, 0.45)

    # ── Mix and make it loop seamlessly ──
    def ir(seconds, seed):
        r = np.random.default_rng(seed); n = int(seconds * SR); t = np.arange(n) / SR
        return lp(r.uniform(-1, 1, n) * np.exp(-t * 6.5 / seconds), 6000) * 0.12
    outL = L * 0.82 + fftconvolve(L, ir(verb, 1))[:N] * wet
    outR = Rr * 0.82 + fftconvolve(Rr, ir(verb, 2))[:N] * wet
    mix = np.stack([outL, outR], 1)
    tail = mix[loop_n:].copy()
    mix = mix[:loop_n]
    mix[:len(tail)] += tail[:loop_n]                       # fold the ring-out back onto the start
    if tone: mix = np.stack([lp(mix[:, 0], tone), lp(mix[:, 1], tone)], 1)
    mix /= np.max(np.abs(mix)) + 1e-9
    mix = np.tanh(mix * 1.5) / np.tanh(1.5)
    wav = f"/tmp/{name}.wav"
    wavfile.write(wav, SR, (mix * 30000).astype(np.int16))
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", wav, "-c:a", "libvorbis", "-q:a", "5", f"Music/{name}.ogg"], check=True)
    print(name, round(loop_n / SR, 1), "s")


if __name__ == "__main__":
    render("menu", key=0, bpm=76, form="IBBA", drums=0, brass_lead=False, harp_ost=True, choir_all=True, verb=3.2, wet=0.7)
    render("battle", key=0, bpm=132, form="IAABBAAO")
    render("battle_drowned", key=-5, bpm=104, form="IAABBAAO", drums=0.55, bell_lead=True, harp_ost=True,
           choir_all=True, tone=4200, verb=3.6, wet=0.75)
    render("battle_cinder", key=2, bpm=150, form="IAABBAAO", drums=1.3, sixteenths=True, verb=2.0, wet=0.45)
    render("battle_frost", key=-3, bpm=112, form="IAABBAAO", drums=0.7, bell_lead=True, harp_ost=True, choir_all=True, tone=6000, verb=3.2, wet=0.7)
    render("battle_storm", key=1, bpm=146, form="IAABBAAO", drums=1.35, sixteenths=True, verb=2.2, wet=0.5)
    render("boss", key=-2, bpm=140, form="IAABBAAO", drums=1.25, choir_all=True, sixteenths=True, verb=2.6, wet=0.55)
