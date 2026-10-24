"""Ashenbound soundtrack, part 3: a distinct composition for every region plus a lo-fi campfire track.

Each region has its own key, mode, tempo, melody, harmony, groove and instrument palette:
  crypt   - D minor dirge-march: pipe organ, low strings, funeral bell, taiko march
  drowned - F# minor, sunken: glass bells, triplet harp, sub bass, water drips, muffled
  frost   - E minor, crystalline: celesta, pizzicato ostinato, shimmering pad, sparse frame drum
  storm   - C minor, relentless: 16th-note strings, brass fanfare, toms, thunder rolls
  cinder  - A phrygian dominant, ritual: distorted low brass, chanting choir, heavy taiko
  camp    - F major lo-fi: FM electric piano, swung drums, warm bass, vinyl and fire crackle

Every track is rendered as a seamless loop (the reverb tail is folded back onto the start).
Usage:  python music_v3.py            (writes Music/*.ogg)
"""
import subprocess
import numpy as np
from scipy.signal import butter, sosfilt, fftconvolve
from scipy.io import wavfile

from music_v2 import (SR, hz, lp, bp, env, saw, strings, brass, choir, harp, bell, taiko, frame_drum, swell)

rng = np.random.default_rng(11)


def hp(x, f, order=2): return sosfilt(butter(order, f, 'high', fs=SR, output='sos'), x)


# ═════════════════════════ Instruments ═════════════════════════

def organ(m, dur):
    """Pipe organ: drawbar harmonics, slow chorus, soft attack."""
    n = int((dur + 0.3) * SR); t = np.arange(n) / SR
    f = hz(m)
    x = np.zeros(n)
    for mult, amp in ((0.5, 0.5), (1, 1.0), (2, 0.6), (3, 0.25), (4, 0.3), (6, 0.1), (8, 0.12)):
        x += amp * np.sin(2 * np.pi * f * mult * t * (1 + 0.0015 * np.sin(2 * np.pi * 0.7 * t)))
    return x / 3 * env(n, 0.06, 0.1, 0.9, 0.28)


def pizz(m):
    """Pizzicato string: a damped pluck with a woody body."""
    n = int(0.6 * SR); t = np.arange(n) / SR
    x = sum(saw(hz(m) * (1 + d), n, rng.random()) for d in (-0.004, 0.004)) / 2
    return lp(x, 1800 + 2200 * np.exp(-t * 20).mean()) * np.exp(-t * 9) * env(n, 0.002, 0.02, 1, 0.05)


def celesta(m, dur=1.6):
    n = int(dur * SR); t = np.arange(n) / SR; f = hz(m)
    x = np.sin(2 * np.pi * f * t) + 0.35 * np.sin(2 * np.pi * f * 4 * t) * np.exp(-t * 8) + 0.15 * np.sin(2 * np.pi * f * 3.01 * t) * np.exp(-t * 5)
    return x * np.exp(-t * 2.4) * env(n, 0.002, 0.02, 1, 0.2) * 0.55


def glass(m, dur=2.5):
    """Glass harmonica / bowl: pure partials with slow beating."""
    n = int(dur * SR); t = np.arange(n) / SR; f = hz(m)
    x = np.sin(2 * np.pi * f * t) + 0.5 * np.sin(2 * np.pi * f * 1.003 * t) + 0.2 * np.sin(2 * np.pi * f * 2.4 * t) * np.exp(-t * 2)
    return x * env(n, 0.08, 0.3, 0.6, 1.0) * np.exp(-t * 0.8) * 0.4


def pad(m, dur, cutoff=1600, attack=0.8):
    n = int((dur + 1.0) * SR)
    x = sum(saw(hz(m) * (1 + d), n, rng.random()) for d in (-0.012, -0.005, 0.0, 0.006, 0.013)) / 5
    return lp(x, cutoff) * env(n, attack, 0.4, 0.8, 1.0)


def sub(m, dur):
    n = int((dur + 0.1) * SR); t = np.arange(n) / SR
    x = np.sin(2 * np.pi * hz(m) * t) + 0.25 * np.sin(4 * np.pi * hz(m) * t)
    return np.tanh(1.3 * x) * env(n, 0.01, 0.1, 0.85, 0.08)


def growl_brass(m, dur):
    """Low brass pushed into saturation (cinder)."""
    n = int((dur + 0.15) * SR)
    x = sum(saw(hz(m) * (1 + d), n, rng.random()) for d in (-0.006, 0, 0.007)) / 3
    x = np.tanh(3.0 * lp(x, 900))
    return x * env(n, 0.02, 0.15, 0.75, 0.1) * 0.6


def tom(pitch=90):
    n = int(0.5 * SR); t = np.arange(n) / SR
    f = pitch + 60 * np.exp(-t * 25)
    return np.tanh(1.4 * np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 9))


def thunder(dur):
    n = int(dur * SR); t = np.arange(n) / SR
    x = lp(rng.uniform(-1, 1, n), 220, 4) * (np.exp(-t * 1.2) * (1 + 0.8 * np.sin(2 * np.pi * 3.3 * t) ** 2))
    return x * 7 * env(n, 0.05, 0.1, 1, 0.5)


def drip():
    n = int(0.25 * SR); t = np.arange(n) / SR
    f = 1400 + 1200 * np.exp(-t * 40)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 22) * 0.35


# ── Lo-fi kit ──
def epiano(m, dur, vel=1.0):
    """Rhodes-like FM electric piano: bell-ish tine on attack mellowing into a warm tone."""
    n = int((dur + 0.6) * SR); t = np.arange(n) / SR; f = hz(m)
    index = 1.6 * np.exp(-t * 3.5) + 0.25
    x = np.sin(2 * np.pi * f * t + index * np.sin(2 * np.pi * f * t))
    tine = np.sin(2 * np.pi * f * 7.0 * t) * np.exp(-t * 30) * 0.12
    trem = 1 + 0.12 * np.sin(2 * np.pi * 4.8 * t)
    return (x + tine) * trem * env(n, 0.004, 0.6, 0.45, 0.5) * 0.32 * vel


def lofi_kick():
    n = int(0.4 * SR); t = np.arange(n) / SR
    f = 50 + 70 * np.exp(-t * 35)
    return np.tanh(2 * np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 10)) * 0.9


def lofi_snare():
    n = int(0.3 * SR); t = np.arange(n) / SR
    noise = bp(rng.uniform(-1, 1, n), 2200, 1.2) * np.exp(-t * 18)
    body = np.sin(2 * np.pi * 190 * t) * np.exp(-t * 25)
    return lp(noise * 1.4 + body * 0.6, 5000) * 0.6


def lofi_hat(vel=1.0):
    n = int(0.08 * SR); t = np.arange(n) / SR
    return hp(rng.uniform(-1, 1, n), 7000) * np.exp(-t * 60) * 0.25 * vel


def crackle(dur, density=30, gain=0.08):
    n = int(dur * SR); x = np.zeros(n)
    for i in rng.integers(0, n - 200, int(density * dur)):
        x[i:i + 60] += rng.uniform(-1, 1, 60) * np.exp(-np.arange(60) / 8) * rng.uniform(0.3, 1.0)
    return lp(x, 6000) * gain


def fire(dur):
    """Campfire: low roar plus random pops."""
    n = int(dur * SR)
    roar = lp(rng.uniform(-1, 1, n), 400) * (0.6 + 0.4 * lp(rng.uniform(0, 1, n), 2)) * 0.18
    return roar + crackle(dur, density=9, gain=0.22)


# ═════════════════════════ Engine ═════════════════════════

class Track:
    def __init__(self, name, bpm, bars, beats_per_bar=4):
        self.name, self.bpm = name, bpm
        self.beat = 60 / bpm
        self.bar = beats_per_bar * self.beat
        self.bars = bars
        self.loop_n = int(bars * self.bar * SR)
        self.N = self.loop_n + int(6 * SR)
        self.L = np.zeros(self.N); self.R = np.zeros(self.N)
        self.chords = []      # (bar, root, minor) for every bar: boss variants layer on top of these
        self.lines = []       # (bar0, line) for every melody phrase

    def at(self, bar, beat=0.0):
        return bar * self.bar + beat * self.beat

    def place(self, sig, t, pan=0.0, gain=1.0):
        i = int(t * SR)
        if i >= self.N or i < 0: return
        sig = sig[:self.N - i] * gain
        self.L[i:i + len(sig)] += sig * np.sqrt(0.5 * (1 - pan))
        self.R[i:i + len(sig)] += sig * np.sqrt(0.5 * (1 + pan))

    def melody(self, voice, bar0, line, pan=0.0, gain=0.4, shift=0, legato=0.95, record=True):
        if record: self.lines.append((bar0, [(m + shift if m is not None else None, d) for m, d in line]))
        t = self.at(bar0)
        for m, beats in line:
            if m is not None:
                self.place(voice(m + shift, beats * self.beat * legato), t, pan, gain)
            t += beats * self.beat

    def render(self, verb=2.6, wet=0.55, tone=None, drive=1.5, wobble=0.0):
        def ir(seconds, seed):
            r = np.random.default_rng(seed); n = int(seconds * SR); t = np.arange(n) / SR
            return lp(r.uniform(-1, 1, n) * np.exp(-t * 6.5 / seconds), 6000) * 0.12
        outL = self.L * 0.82 + fftconvolve(self.L, ir(verb, 1))[:self.N] * wet
        outR = self.R * 0.82 + fftconvolve(self.R, ir(verb, 2))[:self.N] * wet
        if tone:                                                   # filter BEFORE folding, so the seam stays clean
            outL, outR = lp(outL, tone), lp(outR, tone)
        mix = np.stack([outL, outR], 1)
        tail = mix[self.loop_n:].copy()
        mix = mix[:self.loop_n]
        mix[:len(tail)] += tail[:self.loop_n]                     # seamless loop: ring-out folds onto the start
        if wobble:                                                 # tape wow: a whole number of cycles per loop
            t = np.arange(len(mix)) / SR
            cycles = max(1, round(0.3 * self.loop_n / SR))
            mix *= (1 + wobble * np.sin(2 * np.pi * cycles * t / (self.loop_n / SR)))[:, None]
        mix /= np.max(np.abs(mix)) + 1e-9
        mix = np.tanh(mix * drive) / np.tanh(drive)
        wav = f"/tmp/{self.name}.wav"
        wavfile.write(wav, SR, (mix * 30000).astype(np.int16))
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", wav, "-c:a", "libvorbis", "-q:a", "5",
                        f"Music/{self.name}.ogg"], check=True)
        print(f"{self.name}: {self.loop_n / SR:.1f}s loop, {self.bars} bars @ {self.bpm} bpm")


def triad(root, minor):
    return [root, root + (3 if minor else 4), root + 7]


# ═════════════════════════ Boss variants ═════════════════════════

def boss_layer(T):
    """Turns a region theme into its boss theme: same melody and harmony, but faster (set by the caller),
    a war choir on every chord, brass doubling the melody an octave down, a pulsing low drone,
    driving eighth-note taiko and a tom roll into every phrase."""
    for b, root, minor in T.chords:
        t0 = T.at(b)
        for m in triad(root + 12, minor):
            T.place(choir(m, T.bar * 0.96), t0, rng.uniform(-0.6, 0.6), 0.11)
        T.place(sub(root - 12, T.bar * 0.95), t0, 0, 0.3)
        for k in range(8):
            T.place(taiko(k % 4 == 0), t0 + k * T.beat / 2, -0.15 + 0.3 * (k % 2), 0.42 if k % 2 == 0 else 0.26)
        if b % 4 == 3:
            for k in range(8): T.place(tom(130 - 10 * k), t0 + 2 * T.beat + k * T.beat / 4, -0.5 + k * 0.14, 0.4)
    for bar0, line in list(T.lines):
        T.melody(lambda m, d: brass(m - 12, d), bar0, line, -0.2, 0.3, record=False)


# ═════════════════════════ The Ashen Crypt ═════════════════════════

def crypt(boss=False):
    """D minor funeral march. Organ carries the theme, the bell tolls every phrase."""
    T = Track("boss_crypt" if boss else "battle", 116 * (1.12 if boss else 1), 36)
    Dm, Bb, Gm, A, F, C = (50, 1), (46, 0), (43, 1), (45, 0), (41, 0), (48, 0)
    prog_a = [Dm, Bb, Gm, A, Dm, Bb, Gm, A]
    prog_b = [F, C, Dm, Bb, Gm, A, Dm, A]
    mel_a = [[(74, 2), (77, 1), (76, 1)], [(74, 1.5), (72, .5), (70, 2)], [(67, 1), (70, 1), (74, 1), (79, 1)], [(76, 2), (73, 2)],
             [(74, 2), (81, 2)], [(82, 1.5), (81, .5), (79, 1), (77, 1)], [(76, 1), (79, 1), (77, 1), (76, 1)], [(74, 4)]]
    mel_b = [[(81, 2), (84, 2)], [(79, 3), (76, 1)], [(77, 2), (81, 2)], [(86, 4)],
             [(82, 2), (81, 1), (79, 1)], [(76, 2), (81, 2)], [(77, 1), (76, 1), (74, 2)], [(73, 4)]]
    # form: intro 4 | A 8 | B 8 | A 8 | outro 4 + 4 (B-chords, organ alone)
    sections = [('I', prog_a[:4]), ('A', prog_a), ('B', prog_b), ('A', prog_a), ('O', prog_a[:4] + prog_b[4:])]
    b = 0
    for sec, prog in sections:
        for i, (root, minor) in enumerate(prog):
            t0 = T.at(b)
            chord = triad(root + 12, minor)
            T.chords.append((b, root, minor))
            T.place(sum(organ(m, T.bar) for m in chord) / 2, t0, 0.0, 0.32)
            T.place(strings(root - 12, T.bar * 0.98, 900), t0, -0.2, 0.35)
            # Low string ostinato: root-root-fifth-root in eighths.
            if sec != 'I':
                for k, iv in enumerate([0, 0, 7, 0, 12, 0, 7, 0]):
                    T.place(strings(root + iv, T.beat * 0.45, 1500), t0 + k * T.beat / 2, -0.35, 0.28 if k % 2 == 0 else 0.2)
            # Funeral march drums.
            for k in range(4):
                if k in (0, 2): T.place(taiko(), t0 + k * T.beat, -0.1, 0.75 if sec != 'O' else 0.4)
                if sec in 'AB' and k == 3: T.place(taiko(False), t0 + (k + 0.5) * T.beat, 0.2, 0.45)
            if i % 4 == 0: T.place(bell(62, 4.0), t0, 0.3, 0.5)                          # the toll
            if sec == 'B':
                for m in chord: T.place(choir(m + 12, T.bar * 0.96), t0, rng.uniform(-0.5, 0.5), 0.12)
            if i % 4 == 3: T.place(swell(T.bar), t0, 0, 0.35)
            b += 1
        if sec == 'A':
            start = b - 8
            T.melody(lambda m, d: organ(m, d) * 0.9, start, [n for bar_ in mel_a for n in bar_], 0.1, 0.42)
            T.melody(lambda m, d: strings(m, d, 3200), start, [n for bar_ in mel_a for n in bar_], -0.1, 0.18)
        if sec == 'B':
            start = b - 8
            T.melody(lambda m, d: brass(m - 12, d), start, [n for bar_ in mel_b for n in bar_], -0.1, 0.45)
            T.melody(lambda m, d: strings(m, d, 3500), start, [n for bar_ in mel_b for n in bar_], 0.2, 0.2)
    if boss: boss_layer(T)
    T.render(verb=3.0, wet=0.6, tone=7000)


# ═════════════════════════ The Drowned Halls ═════════════════════════

def drowned(boss=False):
    """F# minor, slow and submerged: glass bells over triplet harp, a sub bass that breathes."""
    T = Track("boss_drowned" if boss else "battle_drowned", 92 * (1.12 if boss else 1), 32)
    Fsm, D, A, E, Bm, Cs, G = (54, 1), (50, 0), (45, 0), (52, 0), (47, 1), (49, 0), (43, 0)
    prog_a = [Fsm, D, A, E, Fsm, D, Bm, Cs]
    prog_b = [Bm, Fsm, G, Cs, Bm, Fsm, G, Cs]
    mel_a = [[(73, 3), (78, 1)], [(76, 2), (73, 2)], [(76, 1), (74, 1), (73, 1), (69, 1)], [(71, 4)],
             [(73, 2), (78, 1), (81, 1)], [(80, 2), (76, 2)], [(78, 1), (76, 1), (74, 1), (73, 1)], [(73, 4)]]
    mel_b = [[(86, 2), (85, 2)], [(81, 4)], [(83, 2), (86, 2)], [(85, 4)],
             [(74, 2), (73, 2)], [(69, 4)], [(71, 2), (74, 2)], [(73, 4)]]
    sections = [('I', prog_a[:4]), ('A', prog_a), ('B', prog_b), ('A', prog_a), ('O', prog_a[:4])]
    b = 0
    for sec, prog in sections:
        for i, (root, minor) in enumerate(prog):
            t0 = T.at(b)
            chord = triad(root, minor)
            T.chords.append((b, root, minor))
            T.place(sum(pad(m + 12, T.bar, 1100, 1.2) for m in chord) / 2, t0, 0, 0.28)
            T.place(sub(root - 12, T.bar * 0.9), t0, 0, 0.32)
            # Triplet harp arpeggio rising and falling, like light through water.
            arp = [0, 7, 12, minor and 15 or 16, 19, 24, 19, minor and 15 or 16, 12, 7, 12, 19]
            for k, iv in enumerate(arp):
                T.place(harp(root + 12 + iv), t0 + k * T.beat / 3, 0.45 if k % 2 else -0.45, 0.13)
            if sec in 'AB':
                T.place(frame_drum(), t0, -0.2, 0.35)
                T.place(frame_drum(), t0 + 2.5 * T.beat, 0.2, 0.25)
                T.place(taiko(False), t0 + 2 * T.beat, 0, 0.3)
            for _ in range(rng.integers(1, 4)):                                        # drips
                T.place(drip(), t0 + rng.uniform(0, T.bar), rng.uniform(-0.8, 0.8), 0.5)
            if sec == 'B':
                for m in chord: T.place(choir(m + 12, T.bar * 0.96), t0, rng.uniform(-0.5, 0.5), 0.13)
            b += 1
        if sec == 'A':
            T.melody(lambda m, d: glass(m, max(d, 1.5)), b - 8, [n for bar_ in mel_a for n in bar_], -0.1, 0.45)
            T.melody(lambda m, d: bell(m + 12, 2.0), b - 8, [n for bar_ in mel_a for n in bar_], 0.3, 0.12)
        if sec == 'B':
            T.melody(lambda m, d: strings(m, d, 2600), b - 8, [n for bar_ in mel_b for n in bar_], 0.0, 0.32)
    if boss: boss_layer(T)
    T.render(verb=4.0, wet=0.8, tone=4200, drive=1.3)


# ═════════════════════════ The Frozen Ossuary ═════════════════════════

def frost(boss=False):
    """E minor, glittering and brittle: pizzicato ostinato, celesta theme, a high shimmer."""
    T = Track("boss_frost" if boss else "battle_frost", 108 * (1.12 if boss else 1), 36)
    Em, C, D, Bm, Am, B = (52, 1), (48, 0), (50, 0), (47, 1), (45, 1), (47, 0)
    prog_a = [Em, C, D, Bm, Em, C, Am, B]
    prog_b = [Am, Em, C, B, Am, Em, C, B]
    mel_a = [[(83, 1), (88, 1), (86, 1), (83, 1)], [(84, 2), (79, 2)], [(81, 1), (83, 1), (86, 1), (90, 1)], [(90, 2), (86, 2)],
             [(88, 1.5), (86, .5), (83, 2)], [(84, 1), (88, 1), (91, 2)], [(88, 1), (86, 1), (84, 1), (81, 1)], [(83, 4)]]
    mel_b = [[(72, 2), (76, 2)], [(79, 4)], [(76, 2), (79, 1), (84, 1)], [(83, 4)],
             [(81, 2), (84, 2)], [(83, 2), (79, 2)], [(76, 1), (78, 1), (79, 1), (81, 1)], [(75, 4)]]
    sections = [('I', prog_a[:4]), ('A', prog_a), ('B', prog_b), ('A', prog_a), ('O', prog_a[4:] + prog_a[:4])]
    b = 0
    for sec, prog in sections:
        for i, (root, minor) in enumerate(prog):
            t0 = T.at(b)
            chord = triad(root, minor)
            T.chords.append((b, root, minor))
            T.place(sum(pad(m + 24, T.bar, 5000, 1.5) for m in chord) / 3, t0, 0, 0.12)   # high shimmer
            T.place(strings(root - 12, T.bar * 0.98, 800), t0, -0.2, 0.3)
            for k, iv in enumerate([0, 7, 3 if minor else 4, 7, 12, 7, 3 if minor else 4, 7]):
                T.place(pizz(root + iv), t0 + k * T.beat / 2, -0.4 + 0.1 * (k % 3), 0.35)
            if sec != 'I':
                T.place(frame_drum(), t0, -0.1, 0.4)
                T.place(frame_drum(), t0 + 1.5 * T.beat, 0.2, 0.25)
                T.place(taiko(False), t0 + 2 * T.beat, 0, 0.4)
                if i % 2 == 1: T.place(frame_drum(), t0 + 3.5 * T.beat, 0.3, 0.25)
            if i % 2 == 0: T.place(bell(chord[2] + 36, 2.5), t0 + rng.uniform(0, T.bar), rng.uniform(-0.7, 0.7), 0.1)
            if sec == 'B':
                for m in chord: T.place(choir(m + 12, T.bar * 0.96), t0, rng.uniform(-0.5, 0.5), 0.11)
            if i % 4 == 3: T.place(swell(T.bar), t0, 0, 0.25)
            b += 1
        if sec == 'A':
            T.melody(lambda m, d: celesta(m, 1.8), b - 8, [n for bar_ in mel_a for n in bar_], 0.15, 0.6)
            T.melody(lambda m, d: celesta(m - 12, 1.8), b - 8, [n for bar_ in mel_a for n in bar_], -0.15, 0.25)
        if sec == 'B':
            T.melody(lambda m, d: strings(m, d, 2800), b - 8, [n for bar_ in mel_b for n in bar_], 0.0, 0.38)
    if boss: boss_layer(T)
    T.render(verb=3.4, wet=0.7, tone=8000)


# ═════════════════════════ The Storm Spire ═════════════════════════

def storm(boss=False):
    """C minor at full gallop: 16th-note strings, brass fanfare, toms and thunder."""
    T = Track("boss_storm" if boss else "battle_storm", 152 * (1.12 if boss else 1), 40)
    Cm, Ab, Eb, Bb, Fm, G = (48, 1), (44, 0), (51, 0), (46, 0), (41, 1), (43, 0)
    prog_a = [Cm, Ab, Eb, Bb, Cm, Ab, Fm, G]
    prog_b = [Fm, Cm, Ab, G, Fm, Cm, Ab, G]
    mel_a = [[(79, 1), (79, .5), (80, .5), (79, 1), (75, 1)], [(72, 2), (75, 1), (77, 1)], [(79, 1.5), (82, .5), (80, 1), (79, 1)], [(77, 3), (74, 1)],
             [(79, 1), (79, .5), (80, .5), (79, 1), (75, 1)], [(84, 2), (82, 1), (80, 1)], [(79, 1), (77, 1), (75, 1), (74, 1)], [(72, 4)]]
    mel_b = [[(80, 2), (84, 2)], [(79, 4)], [(80, 2), (87, 2)], [(86, 4)],
             [(80, 2), (84, 2)], [(79, 4)], [(80, 1), (82, 1), (84, 1), (86, 1)], [(83, 4)]]
    sections = [('I', prog_a[:4]), ('A', prog_a), ('B', prog_b), ('A', prog_a), ('B', prog_b), ('O', prog_a[:4])]
    b = 0
    for sec, prog in sections:
        for i, (root, minor) in enumerate(prog):
            t0 = T.at(b)
            chord = triad(root, minor)
            T.chords.append((b, root, minor))
            # 16th-note string ostinato.
            pattern = [0, 0, 7, 0, 3 if minor else 4, 0, 7, 10, 0, 0, 7, 0, 12, 7, 3 if minor else 4, 7]
            for k, iv in enumerate(pattern):
                T.place(strings(root + iv, T.beat / 4 * 0.85, 2200), t0 + k * T.beat / 4, -0.3, 0.26 if k % 4 == 0 else 0.18)
            T.place(strings(root - 12, T.bar * 0.98, 700), t0, 0.1, 0.35)
            # Brass stabs on the off-beats.
            if sec != 'I':
                for k in (1.5, 3.5):
                    T.place(sum(brass(m, T.beat * 0.4) for m in chord) / 2, t0 + k * T.beat, 0.25, 0.3)
            # Driving drums: taiko on every beat, toms fill each 4th bar.
            for k in range(4):
                T.place(taiko(k % 2 == 0), t0 + k * T.beat, -0.1, 0.7 if k % 2 == 0 else 0.5)
                if sec != 'I': T.place(frame_drum(), t0 + (k + 0.5) * T.beat, 0.35, 0.25)
            if i % 4 == 3:
                for k in range(8): T.place(tom(120 - 9 * k), t0 + 2 * T.beat + k * T.beat / 4, -0.5 + k * 0.14, 0.45)
            if i % 8 == 0 and sec != 'I': T.place(thunder(3.0), t0, rng.uniform(-0.4, 0.4), 0.55)
            if sec == 'B':
                for m in chord: T.place(choir(m + 12, T.bar * 0.96), t0, rng.uniform(-0.5, 0.5), 0.14)
            b += 1
        if sec == 'A':
            T.melody(lambda m, d: brass(m - 12, d), b - 8, [n for bar_ in mel_a for n in bar_], -0.1, 0.5)
            T.melody(lambda m, d: strings(m, d, 3800), b - 8, [n for bar_ in mel_a for n in bar_], 0.2, 0.2)
        if sec == 'B':
            T.melody(lambda m, d: strings(m, d, 3800), b - 8, [n for bar_ in mel_b for n in bar_], 0.0, 0.38)
            T.melody(lambda m, d: brass(m - 24, d), b - 8, [n for bar_ in mel_b for n in bar_], -0.2, 0.3)
    if boss: boss_layer(T)
    T.render(verb=2.2, wet=0.48, drive=1.7)


# ═════════════════════════ The Cinder Throne ═════════════════════════

def cinder(boss=False):
    """A phrygian dominant ritual: chanting choir, growling low brass, relentless taiko."""
    T = Track("boss_cinder" if boss else "battle_cinder", 140 * (1.12 if boss else 1), 32)
    Am, Bb, Gm, A, Dm, E = (45, 1), (46, 0), (43, 1), (45, 0), (50, 1), (40, 0)
    prog_a = [Am, Bb, Gm, A, Am, Bb, E, A]
    prog_b = [Dm, Bb, E, A, Dm, Bb, E, A]
    mel_a = [[(69, 1), (70, 1), (73, 1), (74, 1)], [(76, 2), (77, 1), (76, 1)], [(74, 1), (73, 1), (70, 1), (69, 1)], [(68, 2), (69, 2)],
             [(76, 1), (77, 1), (80, 1), (81, 1)], [(82, 2), (81, 2)], [(80, 1), (77, 1), (76, 1), (74, 1)], [(76, 4)]]
    mel_b = [[(77, 2), (81, 2)], [(86, 4)], [(83, 2), (80, 2)], [(81, 4)],
             [(86, 2), (89, 2)], [(89, 2), (86, 2)], [(88, 1), (86, 1), (85, 1), (83, 1)], [(85, 4)]]
    sections = [('I', prog_a[:4]), ('A', prog_a), ('B', prog_b), ('A', prog_a), ('O', prog_a[:4])]
    b = 0
    for sec, prog in sections:
        for i, (root, minor) in enumerate(prog):
            t0 = T.at(b)
            chord = triad(root, minor)
            T.chords.append((b, root, minor))
            # Growling ostinato: root, root, flat-two, root.
            for k, iv in enumerate([0, 0, 1, 0, 0, 0, 1, 3]):
                T.place(growl_brass(root - 12 + iv, T.beat * 0.45), t0 + k * T.beat / 2, -0.2, 0.5)
            # Low chanting choir on every bar.
            for m in (root, root + 7):
                T.place(choir(m, T.bar * 0.96), t0, rng.uniform(-0.4, 0.4), 0.16 if sec != 'I' else 0.1)
            # Heavy taiko: big on 1 and 3, rolls into every second bar.
            for k in range(4):
                if k in (0, 2): T.place(taiko(), t0 + k * T.beat, -0.1, 0.85)
                if sec != 'I' and k in (1, 3): T.place(taiko(False), t0 + k * T.beat, 0.2, 0.55)
                if sec in 'AB': T.place(taiko(False), t0 + (k + 0.75) * T.beat, -0.25, 0.3)
            if i % 2 == 1 and sec != 'I':
                for k in range(4): T.place(taiko(False), t0 + 3 * T.beat + k * T.beat / 4, 0, 0.45)
            if sec == 'B':
                for m in chord: T.place(choir(m + 12, T.bar * 0.96), t0, rng.uniform(-0.5, 0.5), 0.15)
                T.place(sum(brass(m, T.bar * 0.9) for m in chord) / 3, t0, 0.2, 0.25)
            if i % 4 == 3: T.place(swell(T.bar), t0, 0, 0.4)
            b += 1
        if sec == 'A':
            T.melody(lambda m, d: brass(m, d), b - 8, [n for bar_ in mel_a for n in bar_], -0.1, 0.45)
            T.melody(lambda m, d: strings(m + 12, d, 3500), b - 8, [n for bar_ in mel_a for n in bar_], 0.2, 0.2)
        if sec == 'B':
            T.melody(lambda m, d: strings(m, d, 3800), b - 8, [n for bar_ in mel_b for n in bar_], 0.0, 0.4)
            T.melody(lambda m, d: choir(m - 12, d), b - 8, [n for bar_ in mel_b for n in bar_], 0.0, 0.14)
    if boss: boss_layer(T)
    T.render(verb=2.4, wet=0.5, drive=1.8)


# ═════════════════════════ Campfire (rest sites) ═════════════════════════

def camp():
    """Lo-fi by the fire: swung jazz chords on an electric piano, dusty drums, warm bass, crackle."""
    T = Track("camp", 78, 24)
    swing = 0.62                                                              # eighth-note swing ratio
    # (bass, voicing): Gm9, C13, Fmaj9, Dm9 | Bbmaj7, Am7, Gm7, C7sus
    chords_a = [(43, [53, 58, 62, 69]), (36, [58, 64, 69, 74]), (41, [57, 60, 64, 67]), (38, [53, 57, 60, 64])]
    chords_b = [(46, [57, 62, 65, 69]), (45, [55, 60, 64, 67]), (43, [53, 58, 62, 65]), (36, [53, 58, 62, 67])]
    form = chords_a * 2 + chords_b + chords_a + chords_b + chords_a     # 24 bars
    # A lazy lead line that only plays in the second half of each 8 bars.
    lead = [[(None, 2), (74, 1), (72, 1)], [(70, 1.5), (69, .5), (67, 2)], [(69, 1), (72, 1), (76, 2)], [(74, 4)],
            [(None, 1), (77, 1), (76, 1), (74, 1)], [(72, 2), (69, 2)], [(70, 1), (69, 1), (67, 1), (65, 1)], [(67, 4)]]

    def swung(beat_pos):
        whole, frac = int(beat_pos), beat_pos - int(beat_pos)
        return whole + (swing if abs(frac - 0.5) < 1e-6 else frac)

    for b, (bass, voicing) in enumerate(form):
        t0 = T.at(b)
        vel = 0.9 + 0.2 * rng.random()
        # Chord hits: beat 1 (long) and the swung "and" of 2 (short).
        for k, m in enumerate(voicing):
            T.place(epiano(m, T.beat * 2.2, vel), t0 + k * 0.012, -0.2 + 0.13 * k, 0.5)
            T.place(epiano(m, T.beat * 0.8, vel * 0.6), t0 + swung(1.5) * T.beat + k * 0.01, -0.2 + 0.13 * k, 0.35)
        # Warm bass: root on 1, fifth on 3, a passing note on the swung "and" of 4.
        T.place(sub(bass, T.beat * 1.6) * 0.8, t0, 0, 0.55)
        T.place(sub(bass + 7, T.beat * 1.2) * 0.8, t0 + 2 * T.beat, 0, 0.45)
        T.place(sub(bass + (10 if b % 2 else 5), T.beat * 0.4) * 0.8, t0 + swung(3.5) * T.beat, 0, 0.3)
        # Dusty, laid-back drums.
        T.place(lofi_kick(), t0, 0, 0.8)
        T.place(lofi_kick(), t0 + swung(2.5) * T.beat, 0, 0.45)
        T.place(lofi_kick(), t0 + 2 * T.beat + (0.0 if b % 4 != 3 else 0.0), 0, 0.6 if b % 2 else 0.0)
        for k in (1, 3): T.place(lofi_snare(), t0 + k * T.beat + 0.025, 0.1, 0.55)
        for k in range(8):
            T.place(lofi_hat(0.6 + 0.4 * rng.random() * (1 if k % 2 == 0 else 0.6)), t0 + swung(k / 2) * T.beat, 0.35, 0.8)
        # Lead line in the back half of each 8-bar phrase.
        if b % 8 == 4:
            phrase = lead[(b // 8) % 2 * 4:(b // 8) % 2 * 4 + 4] if False else lead[0:4] if (b // 8) % 2 == 0 else lead[4:8]
            T.melody(lambda m, d: epiano(m + 12, d, 0.8) * 0.9, b, [n for bar_ in phrase for n in bar_], 0.25, 0.42, legato=0.9)
    total = T.bars * T.bar
    T.place(crackle(total + 1, density=40, gain=0.06), 0, 0.0, 1.0)            # vinyl
    T.place(fire(total + 1), 0, -0.3, 1.0)                                      # the campfire
    T.render(verb=1.6, wet=0.35, tone=3800, drive=1.2, wobble=0.03)


if __name__ == "__main__":
    import sys
    tracks = {"crypt": crypt, "drowned": drowned, "frost": frost, "storm": storm, "cinder": cinder, "camp": camp}
    for key in ("crypt", "drowned", "frost", "storm", "cinder"):
        tracks[f"boss_{key}"] = (lambda f: lambda: f(boss=True))(tracks[key])
    for name in (sys.argv[1:] or tracks):
        tracks[name]()
