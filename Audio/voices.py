"""Monster voices for Ashenbound, version 2: a Klatt-style cascade formant synthesizer.

Why it sounds better than v1:
  * Glottal source: a Rosenberg pulse (smooth opening, sharp closing) instead of a filtered sawtooth,
    with pitch jitter, amplitude shimmer and breath noise that pulses with each vocal-fold cycle.
  * Vocal tract: four resonators in CASCADE with time-varying frequencies, and vowels glide into each other
    (coarticulation) instead of switching abruptly.
  * Consonants: plosive bursts, fricative noise through their own resonances, nasals and trills, all placed
    on one continuous timeline with the vowels, so a phrase flows like speech.
  * Monster character: throat size (formant scale), pitch, a growl made of an undertone and fast amplitude
    flutter, rattling clicks for skeletons, ring modulation for machines, a lockstep voice an octave down for
    bosses, then saturation, EQ and a damped room reverb.

Creatures that don't speak use dedicated designs: growls/roars (noise and an undertone through sweeping
formants), screeches and cackles.

Output: Voice/{voice}_{talk|attack|hurt|death}_{n}.ogg (the game picks clips by these names).
Usage: python voices.py   (numpy, scipy, ffmpeg)
"""
import os, sys, subprocess
import numpy as np
from scipy.signal import butter, sosfilt, lfilter, fftconvolve
from scipy.io import wavfile

SR = 32000
OUT = "Voice"
BLOCK = 64

# Formants (F1..F4 Hz, bandwidths) for an adult voice; "throat" scales them.
VOWELS = {
    'a': (730, 1090, 2440, 3400), 'e': (530, 1840, 2480, 3500), 'i': (300, 2250, 3000, 3700),
    'o': (570, 840, 2410, 3300), 'u': (320, 870, 2240, 3300), 'ə': (500, 1500, 2500, 3500),
}
BANDWIDTHS = (80, 110, 150, 250)


def lp(x, f, order=2): return sosfilt(butter(order, min(f, SR / 2 - 100), 'low', fs=SR, output='sos'), x)
def hp(x, f, order=2): return sosfilt(butter(order, f, 'high', fs=SR, output='sos'), x)
def bp(x, f, q=3.0):
    lo, hi = f / (1 + 1 / (2 * q)), min(f * (1 + 1 / (2 * q)), SR / 2 - 100)
    return sosfilt(butter(2, [lo, hi], 'band', fs=SR, output='sos'), x)


def resonator_coeffs(f, bw):
    T = 1 / SR
    C = -np.exp(-2 * np.pi * bw * T)
    B = 2 * np.exp(-np.pi * bw * T) * np.cos(2 * np.pi * f * T)
    A = 1 - B - C
    return [A], [1, -B, -C]


def cascade(x, tracks, bws):
    """Time-varying cascade of resonators, processed in blocks with filter state carried across."""
    y = x.copy()
    for k, track in enumerate(tracks):
        out = np.empty_like(y)
        zi = np.zeros(2)
        for s in range(0, len(y), BLOCK):
            b, a = resonator_coeffs(float(track[min(s + BLOCK // 2, len(track) - 1)]), bws[k])
            seg, zi = lfilter(b, a, y[s:s + BLOCK], zi=zi)
            out[s:s + BLOCK] = seg
        y = out
    return y


class Voice:
    def __init__(self, f0=120, spread=0.25, throat=1.0, rough=0.0, breath=0.08, vibrato=0.0, ring=0.0, clicks=0.0,
                 double=0.0, verb=0.25, verb_len=0.8, speed=1.0, drive=1.3, vowels="aeiouə", bright=1.0, seed=0):
        self.f0, self.spread, self.throat, self.rough, self.breath = f0, spread, throat, rough, breath
        self.vibrato, self.ring, self.clicks, self.double = vibrato, ring, clicks, double
        self.verb, self.verb_len, self.speed, self.drive, self.vowels, self.bright = verb, verb_len, speed, drive, vowels, bright
        self.rng = np.random.default_rng(seed)

    # ── the plan of a phrase: a list of segments on one timeline ──
    def plan_phrase(self, syllables, contour):
        segs = []
        base = self.f0 * (1 + self.rng.uniform(-0.06, 0.06))
        for i in range(syllables):
            k = i / max(1, syllables - 1)
            shape = {"fall": 1.12 - 0.32 * k, "rise": 0.92 + 0.3 * k, "arch": 1 + 0.22 * np.sin(np.pi * k)}[contour]
            pitch = base * shape * (1 + self.spread * self.rng.uniform(-0.4, 0.4))
            stressed = self.rng.random() < 0.3
            cons = self.rng.choice(['k', 't', 'g', 'd', 'kh', 's', 'sh', 'h', 'r', 'm', 'n', 'v', 'z', '', ''])
            vow = self.rng.choice(list(self.vowels))
            dur = self.rng.uniform(0.08, 0.15) * (1.45 if stressed else 1.0) * (1.6 if i == syllables - 1 else 1.0) / self.speed
            if cons: segs.append(("c", cons, pitch, 1.0))
            segs.append(("v", vow, pitch * (1.08 if stressed else 1.0), dur, 1.25 if stressed else 1.0))
            if i < syllables - 1 and self.rng.random() < 0.15:
                segs.append(("pause", 0.07 / self.speed))
        return segs

    def render(self, segs, end_glide=0.92):
        """Turns segments into tracks (pitch, voicing, F1-F4, noise) and synthesizes them in one pass."""
        f0, voice_amp, noise_amp, noise_kind = [], [], [], []
        formants = [[] for _ in range(4)]
        last_vowel = VOWELS['ə']
        for seg in segs:
            if seg[0] == "pause":
                n = int(seg[1] * SR)
                f0 += [f0[-1] if f0 else self.f0] * n; voice_amp += [0.0] * n; noise_amp += [0.0] * n; noise_kind += [''] * n
                for k in range(4): formants[k] += [last_vowel[k]] * n
            elif seg[0] == "c":
                _, c, pitch, _ = seg
                n = int((0.07 if c in ('s', 'sh', 'z', 'h', 'kh', 'v') else 0.045) * SR / self.speed)
                voiced = c in ('g', 'd', 'm', 'n', 'r', 'z', 'v')
                env = np.exp(-np.arange(n) / (n * (0.5 if c in ('s', 'sh', 'z', 'h', 'kh', 'v') else 0.25)))
                f0 += [pitch] * n
                voice_amp += list((0.5 if c in ('m', 'n', 'r') else 0.25 if voiced else 0.0) * np.ones(n))
                noise_amp += list(env * (0.0 if c in ('m', 'n') else 0.7))
                noise_kind += [c] * n
                target = VOWELS['u'] if c in ('m', 'n') else last_vowel
                for k in range(4): formants[k] += [target[k]] * n
            else:
                _, v, pitch, dur, gain = seg
                n = int(dur * SR)
                glide = np.linspace(pitch, pitch * end_glide, n)
                f0 += list(glide)
                env = np.minimum(1, np.arange(n) / (0.02 * SR)) * np.minimum(1, (n - np.arange(n)) / (0.035 * SR))
                voice_amp += list(env * gain)
                noise_amp += [0.0] * n; noise_kind += [''] * n
                tgt = VOWELS[v]
                trans = min(n, int(0.035 * SR))                      # coarticulation: glide from the last vowel
                for k in range(4):
                    track = np.full(n, float(tgt[k]))
                    track[:trans] = np.linspace(last_vowel[k], tgt[k], trans)
                    formants[k] += list(track)
                last_vowel = tgt
        f0 = np.array(f0); voice_amp = np.array(voice_amp); noise_amp = np.array(noise_amp)
        n = len(f0)
        t = np.arange(n) / SR

        # Glottal source: Rosenberg pulses with jitter, vibrato and shimmer; its derivative is the excitation.
        jitter = 1 + 0.012 * lp(self.rng.standard_normal(n), 30) * 8
        f = f0 * jitter * (1 + self.vibrato * np.sin(2 * np.pi * 5.6 * t))
        phase = np.cumsum(f) / SR
        p = phase % 1.0
        Tp, Tn = 0.4, 0.16
        flow = np.where(p < Tp, 0.5 * (1 - np.cos(np.pi * p / Tp)),
                        np.where(p < Tp + Tn, np.cos(np.pi * (p - Tp) / (2 * Tn)), 0.0))
        src = np.diff(flow, prepend=0.0) * 30
        shimmer = 1 + 0.08 * lp(self.rng.standard_normal(n), 20) * 6
        src *= shimmer
        if self.rough > 0:   # growl: an undertone at half pitch plus fast flutter
            under = np.diff(np.where((phase / 2) % 1.0 < 0.5, 1.0, 0.0), prepend=0.0)
            src = src + under * self.rough * 1.2
            src *= 1 + self.rough * 0.6 * np.sin(2 * np.pi * 38 * t + 3 * np.sin(2 * np.pi * 7 * t))
        aspiration = self.rng.uniform(-1, 1, n) * (0.4 + 0.6 * (p < Tp + Tn))           # breath pulses with the folds
        excitation = src * voice_amp * (1 - self.breath) + aspiration * voice_amp * self.breath * 0.6

        tracks = [np.array(formants[k]) * self.throat for k in range(4)]
        tract = cascade(excitation, tracks, BANDWIDTHS)

        # Consonant noise: through its own place-of-articulation resonance.
        noise = self.rng.uniform(-1, 1, n) * noise_amp
        frics = np.zeros(n)
        kinds = np.array(noise_kind)
        for c, (centre, q, g) in {'s': (5200, 2, 0.5), 'sh': (2600, 2, 0.7), 'z': (5000, 2, 0.35), 'v': (1500, 1, 0.3),
                                  'h': (1300, 1, 0.45), 'kh': (1700, 1.5, 0.8), 'k': (1800, 3, 1.0), 'g': (1500, 3, 0.7),
                                  't': (3800, 3, 0.9), 'd': (3200, 3, 0.6), 'r': (1300, 2, 0.2)}.items():
            mask = kinds == c
            if mask.any(): frics += bp(noise * mask, centre * min(1.2, self.throat ** 0.5), q) * g
        out = tract * 0.06 + frics
        return out / (np.max(np.abs(out)) + 1e-9)

    # ── utterances ──
    def phrase(self, syllables, contour):
        return self.render(self.plan_phrase(syllables, contour))

    def shout(self):
        c = self.rng.choice(['h', 'kh', 'g', 'r'])
        v = self.rng.choice(['a', 'o', 'ə'])
        return self.render([("c", c, self.f0 * 1.4, 1), ("v", v, self.f0 * 1.55, 0.32 / self.speed, 1.5)], end_glide=0.72)

    def grunt(self):
        return self.render([("c", self.rng.choice(['h', 'g']), self.f0 * 1.2, 1),
                            ("v", self.rng.choice(['u', 'ə']), self.f0 * 1.2, 0.17, 1.3)], end_glide=0.7)

    def cry(self):
        v = self.rng.choice(['a', 'o'])
        return self.render([("c", 'h', self.f0 * 1.5, 1), ("v", v, self.f0 * 1.6, 0.7 / self.speed, 1.4),
                            ("v", 'u', self.f0 * 0.75, 0.45 / self.speed, 0.7)], end_glide=0.55)

    # ── character ──
    def finish(self, x):
        t = np.arange(len(x)) / SR
        if self.ring > 0:
            x = x * (1 - self.ring) + x * np.sin(2 * np.pi * 55 * t) * self.ring * 1.6
        if self.clicks > 0:
            for i in self.rng.integers(0, max(1, len(x) - 600), int(self.clicks * len(x) / SR * 12)):
                k = np.arange(400)
                x[i:i + 400] += bp(self.rng.uniform(-1, 1, 400), self.rng.uniform(2200, 3400), 5) * np.exp(-k / 50) * 0.9
        # Body EQ: a little low warmth, tame the harsh top.
        x = x + 0.25 * lp(x, 300) * (1.4 if self.throat < 0.9 else 0.6)
        x = lp(x, 6500 * self.bright)
        x = np.tanh(x / (np.max(np.abs(x)) + 1e-9) * self.drive)
        if self.verb > 0:   # damped room: early reflections + a decaying, darkening tail
            n = int(self.verb_len * SR); k = np.arange(n) / SR
            ir = self.rng.uniform(-1, 1, n) * np.exp(-k * 6.5 / self.verb_len)
            ir = lp(ir, 3500)
            for d, g in ((0.011, 0.5), (0.019, 0.35), (0.031, 0.25)): ir[int(d * SR)] += g
            wet = fftconvolve(x, ir)
            x = np.concatenate([x, np.zeros(len(wet) - len(x))]) + wet / (np.max(np.abs(wet)) + 1e-9) * self.verb
        x = x / (np.max(np.abs(x)) + 1e-9) * 0.9
        fade = int(0.008 * SR); x[:fade] *= np.linspace(0, 1, fade); x[-fade * 3:] *= np.linspace(1, 0, fade * 3)
        return x


def creature(v, kind, length=1.0):
    """Non-speaking monsters."""
    r = v.rng
    if kind in ("growl", "roar"):
        n = int(r.uniform(0.55, 0.95) * length * SR); t = np.arange(n) / SR
        f0 = v.f0 * (1 + 0.25 * np.sin(np.pi * t / t[-1])) * (1 + 0.03 * r.standard_normal(n).cumsum() / np.sqrt(np.arange(1, n + 1)))
        phase = np.cumsum(f0) / SR
        p = phase % 1.0
        pulse = np.diff(np.where(p < 0.35, np.sin(np.pi * p / 0.35), 0.0), prepend=0.0) * 20
        under = np.diff(np.where((phase / 2) % 1 < 0.4, 1.0, 0.0), prepend=0.0) * 1.5
        flutter = 1 + 0.8 * np.sin(2 * np.pi * r.uniform(28, 45) * t)
        src = (pulse + under) * flutter + r.uniform(-1, 1, n) * 0.6 * flutter
        sweep = np.linspace(0, 1, n)
        tracks = [np.interp(sweep, [0, 0.5, 1], [VOWELS['o'][k], VOWELS['a'][k], VOWELS['u'][k]]) * v.throat for k in range(4)]
        y = cascade(src, tracks, (120, 160, 220, 300))
        env = np.minimum(1, t / 0.08) * np.minimum(1, (t[-1] - t) / 0.2) * (0.7 + 0.3 * np.sin(np.pi * sweep))
        return y * env / (np.max(np.abs(y)) + 1e-9)
    if kind == "screech":
        n = int(r.uniform(0.35, 0.6) * length * SR); t = np.arange(n) / SR
        f = v.f0 * (1 + 0.45 * np.exp(-t * 7)) * (1 + 0.05 * np.sin(2 * np.pi * 34 * t))
        phase = np.cumsum(f) / SR
        src = np.sign(np.sin(2 * np.pi * phase)) * 0.5 + r.uniform(-1, 1, n) * 0.5
        y = cascade(src, [np.full(n, 1500.0 * v.throat), np.full(n, 3200.0 * v.throat)], (200, 400))
        return y * np.exp(-t * 2.2) * np.minimum(1, t / 0.01) / (np.max(np.abs(y)) + 1e-9)
    if kind == "cackle":
        segs = []
        for i in range(r.integers(4, 7)):
            segs += [("c", 'h', v.f0 * (1.35 - 0.05 * i), 1), ("v", 'e', v.f0 * (1.35 - 0.05 * i), 0.075, 1.0), ("pause", 0.045)]
        return v.render(segs, end_glide=0.85)
    raise ValueError(kind)


VOICES = {
    "bones": (dict(f0=150, throat=1.12, breath=0.35, clicks=1.0, verb=0.2, speed=1.15, vowels="aeoə", bright=1.1), "speech"),
    "ghoul": (dict(f0=62, throat=0.82, rough=0.8, breath=0.2, verb=0.15, drive=1.8), "growl"),
    "cultist": (dict(f0=98, throat=0.95, breath=0.5, verb=0.4, verb_len=1.2, speed=0.9), "speech"),
    "wraith": (dict(f0=140, throat=1.08, breath=0.92, verb=0.6, verb_len=1.8, speed=0.72, vowels="aouə", bright=0.9), "speech"),
    "siren": (dict(f0=310, throat=1.22, breath=0.12, vibrato=0.035, verb=0.55, verb_len=2.0, speed=0.65, vowels="aeiou"), "speech"),
    "beast": (dict(f0=75, throat=0.9, rough=0.9, breath=0.3, drive=2.0, verb=0.12), "growl"),
    "bird": (dict(f0=1250, throat=1.3, verb=0.25), "screech"),
    "imp": (dict(f0=300, throat=1.38, rough=0.15, breath=0.12, speed=1.5, verb=0.18, vowels="eiə", bright=1.1), "cackle"),
    "machine": (dict(f0=88, throat=1.0, ring=0.55, breath=0.02, speed=1.05, verb=0.22, drive=1.7, vowels="aoə"), "speech"),
    "giant": (dict(f0=52, throat=0.68, rough=0.55, breath=0.15, verb=0.45, verb_len=1.5, speed=0.7, drive=1.6, vowels="aouə", bright=0.8), "speech"),
    "knight": (dict(f0=92, throat=0.95, rough=0.12, breath=0.1, verb=0.35, verb_len=1.2, speed=0.9, vowels="aeoə"), "speech"),
    "boss_colossus": (dict(f0=46, throat=0.66, rough=0.6, breath=0.15, clicks=0.5, double=0.7, verb=0.6, verb_len=2.2, speed=0.62, drive=1.7, vowels="aoə", bright=0.8), "speech"),
    "boss_matriarch": (dict(f0=185, throat=1.05, breath=0.22, vibrato=0.03, double=0.45, verb=0.7, verb_len=2.4, speed=0.65, vowels="aeouə"), "speech"),
    "boss_lich": (dict(f0=165, throat=1.1, breath=0.78, double=0.4, verb=0.75, verb_len=2.6, speed=0.68, vowels="aeiouə", bright=0.95), "speech"),
    "boss_tyrant": (dict(f0=70, throat=0.8, rough=0.45, breath=0.12, ring=0.12, double=0.55, verb=0.5, verb_len=1.8, speed=0.75, drive=1.6, vowels="aoə"), "speech"),
    "boss_king": (dict(f0=58, throat=0.72, rough=0.35, breath=0.25, double=0.85, verb=0.8, verb_len=2.8, speed=0.6, drive=1.5, vowels="aouə", bright=0.85), "speech"),
    # The Rotting Garden and the Silent Archive.
    "plant": (dict(f0=96, throat=1.05, rough=0.65, breath=0.55, verb=0.2, drive=1.6), "growl"),
    "witch": (dict(f0=215, throat=1.18, rough=0.3, breath=0.55, verb=0.35, verb_len=1.3, speed=1.1, vowels="aeiə", bright=1.05), "speech"),
    "book": (dict(f0=120, throat=1.15, breath=0.95, clicks=0.6, verb=0.45, verb_len=1.6, speed=1.25, vowels="eiə", bright=1.15), "speech"),
    "boss_bloom": (dict(f0=200, throat=1.1, rough=0.25, breath=0.4, vibrato=0.02, double=0.6, verb=0.7, verb_len=2.4, speed=0.7, vowels="aeouə"), "speech"),
    "boss_archivist": (dict(f0=112, throat=1.0, breath=0.85, double=0.5, verb=0.8, verb_len=2.8, speed=0.74, vowels="aeiə", bright=0.95), "speech"),
    "boss_wyrm": (dict(f0=44, throat=0.66, rough=0.95, breath=0.35, verb=0.55, drive=2.2), "growl"),
}


def write(name, x):
    wav = f"/tmp/{name}.wav"
    wavfile.write(wav, SR, (x * 30000).astype(np.int16))
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", wav, "-c:a", "libvorbis", "-q:a", "4", f"{OUT}/{name}.ogg"], check=True)


def main():
    os.makedirs(OUT, exist_ok=True)
    only = set(sys.argv[1:])
    for seed, (name, (settings, style)) in enumerate(VOICES.items()):
        if only and name not in only: continue
        v = Voice(seed=seed * 101 + 7, **settings)
        # Bosses: a lockstep shadow voice an octave down (same random stream, so same syllables and timing).
        low = Voice(seed=seed * 101 + 7, **{**settings, "f0": settings["f0"] / 2, "throat": settings.get("throat", 1.0) * 0.85}) \
            if settings.get("double", 0) > 0 else None

        def both(make):
            x = make(v)
            if low is None: return x
            y = lp(make(low), 1600)
            n = max(len(x), len(y))
            return np.pad(x, (0, n - len(x))) + np.pad(y, (0, n - len(y))) * v.double

        clips = {}
        if style == "speech":
            clips["talk"] = []
            for _ in range(6):
                syll = int(v.rng.integers(3, 8)); contour = str(v.rng.choice(["fall", "rise", "arch"]))
                if low is not None: low.rng.integers(3, 8); low.rng.choice(["fall", "rise", "arch"])
                clips["talk"].append(both(lambda u: u.phrase(syll, contour)))
            clips["attack"] = [both(lambda u: u.shout()) for _ in range(3)]
            clips["hurt"] = [both(lambda u: u.grunt()) for _ in range(2)]
            clips["death"] = [both(lambda u: u.cry()) for _ in range(2)]
        else:
            clips["talk"] = [creature(v, style) for _ in range(4)]
            clips["attack"] = [creature(v, style, 0.8) for _ in range(3)]
            clips["hurt"] = [creature(v, style, 0.35) for _ in range(2)]
            clips["death"] = [np.concatenate([creature(v, style, 1.2), creature(v, style, 0.7) * 0.5]) for _ in range(2)]
        for cue, xs in clips.items():
            for i, x in enumerate(xs):
                write(f"{name}_{cue}_{i}", v.finish(np.asarray(x, dtype=np.float64)))
        print(name, {k: len(x) for k, x in clips.items()})


if __name__ == "__main__":
    main()
