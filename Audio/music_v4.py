"""Ashenbound soundtrack, part 4: the two newer regions (and their boss variants).
  garden  - G dorian in a lilting 6/8: wooden flute, plucked harp ostinato, frame drums, creaking branches, insect clicks
  archive - Bb minor, hushed: harpsichord theme, music-box celesta, a ticking clock, whispering choir and a low drone
Usage:  python music_v4.py [garden|archive|boss_garden|boss_archive]   (writes Music/*.ogg)
"""
import sys
import numpy as np
from music_v2 import SR, hz, lp, bp, env, saw, strings, choir, harp, bell, taiko, frame_drum, swell
from music_v3 import Track, triad, boss_layer, pad, sub, celesta, pizz, hp, rng


def flute(m, dur):
    """Wooden flute: a breathy sine with delayed vibrato."""
    n = int((dur + 0.15) * SR); t = np.arange(n) / SR; f = hz(m)
    vib = 1 + 0.006 * np.sin(2 * np.pi * 5.2 * t) * np.clip(t / 0.35, 0, 1)
    ph = 2 * np.pi * np.cumsum(f * vib) / SR
    x = np.sin(ph) + 0.18 * np.sin(2 * ph) + 0.06 * np.sin(3 * ph)
    breath = bp(rng.normal(0, 1, n), f * 1.4, 1.5) * 0.12
    return (x + breath) * env(n, 0.07, 0.1, 0.85, 0.12) * 0.5


def harpsichord(m, dur=1.2):
    """A bright plucked keyboard: detuned saws, a fast decay and a body resonance."""
    n = int(dur * SR); t = np.arange(n) / SR
    x = sum(saw(hz(m) * (1 + d), n, rng.random()) for d in (-0.002, 0.0, 0.003)) / 3
    x = hp(lp(x, 5200), 180)
    return x * np.exp(-t * 3.2) * env(n, 0.001, 0.01, 1, 0.08) * 0.5


def creak():
    """A branch creaking: a slow, resonant, pitch-bending scrape."""
    n = int(0.7 * SR); t = np.arange(n) / SR
    f = 180 + 60 * np.sin(2 * np.pi * 1.4 * t)
    x = np.sign(np.sin(2 * np.pi * np.cumsum(f) / SR)) * (rng.random(n) < 0.6)
    return bp(x.astype(float), 520, 2.0) * env(n, 0.1, 0.2, 0.6, 0.2) * 0.25


def click():
    """An insect click in the undergrowth."""
    n = int(0.03 * SR); t = np.arange(n) / SR
    return hp(rng.normal(0, 1, n), 4000) * np.exp(-t * 300) * 0.4


def tick(high=True):
    """A clock tick."""
    n = int(0.05 * SR); t = np.arange(n) / SR
    return bp(rng.normal(0, 1, n), 3200 if high else 2000, 3.0) * np.exp(-t * 160) * 0.6


# ═════════════════════════ The Rotting Garden ═════════════════════════

def garden(boss=False):
    """G dorian, 6/8. Each 'beat' is an eighth note, six per bar."""
    T = Track("boss_garden" if boss else "battle_garden", 216 * (1.12 if boss else 1), 32, beats_per_bar=6)
    Gm, F, C, Dm, Eb, Bb = (55, 1), (53, 0), (48, 0), (50, 1), (51, 0), (46, 0)
    prog_a = [Gm, F, C, Gm, Gm, F, Eb, Dm]
    prog_b = [Eb, Bb, F, C, Eb, Bb, C, Dm]
    # Melody in eighths (6 per bar): a lilting folk line with the dorian raised sixth (E).
    mel_a = [[(74, 3), (72, 1), (70, 2)], [(69, 2), (70, 1), (72, 3)], [(76, 3), (74, 1), (72, 2)], [(74, 6)],
             [(79, 3), (77, 1), (76, 2)], [(77, 2), (76, 1), (74, 3)], [(75, 2), (74, 1), (72, 2), (70, 1)], [(69, 6)]]
    mel_b = [[(70, 3), (72, 3)], [(74, 3), (77, 3)], [(76, 2), (74, 2), (72, 2)], [(72, 6)],
             [(75, 3), (77, 3)], [(79, 3), (82, 3)], [(81, 2), (79, 2), (76, 2)], [(74, 6)]]
    sections = [('I', prog_a[:4]), ('A', prog_a), ('B', prog_b), ('A', prog_a), ('O', prog_a[4:])]
    b = 0
    for sec, prog in sections:
        for i, (root, minor) in enumerate(prog):
            t0 = T.at(b)
            chord = triad(root, minor)
            T.chords.append((b, root, minor))
            T.place(sum(pad(m + 12, T.bar, 1400, 1.0) for m in chord) / 3, t0, 0, 0.18)
            T.place(strings(root - 12, T.bar * 0.97, 700), t0, -0.2, 0.3)
            # Harp ostinato in 6/8: root-fifth-third-fifth-octave-fifth.
            for k, iv in enumerate([0, 7, 3 if minor else 4, 7, 12, 7]):
                T.place(harp(root + 12 + iv), t0 + k * T.beat, 0.4 if k % 2 else -0.3, 0.16)
            if sec != 'I':
                T.place(frame_drum(), t0, -0.15, 0.45)
                T.place(frame_drum(), t0 + 3 * T.beat, 0.15, 0.32)
                T.place(frame_drum(), t0 + 5 * T.beat, 0.3, 0.16)
                if i % 2 == 1: T.place(taiko(False), t0 + 3 * T.beat, 0, 0.3)
            for _ in range(rng.integers(2, 6)): T.place(click(), t0 + rng.uniform(0, T.bar), rng.uniform(-0.9, 0.9), 0.25)
            if rng.random() < 0.35: T.place(creak(), t0 + rng.uniform(0, T.bar * 0.6), rng.uniform(-0.8, 0.8), 0.4)
            if sec == 'B':
                for m in chord: T.place(choir(m + 12, T.bar * 0.96), t0, rng.uniform(-0.5, 0.5), 0.1)
            if i % 4 == 3: T.place(swell(T.bar), t0, 0, 0.22)
            b += 1
        if sec == 'A':
            T.melody(flute, b - 8, [n for bar_ in mel_a for n in bar_], 0.1, 0.5)
            T.melody(lambda m, d: pizz(m - 12), b - 8, [n for bar_ in mel_a for n in bar_], -0.3, 0.14, record=False)
        if sec == 'B':
            T.melody(lambda m, d: strings(m, d, 2600), b - 8, [n for bar_ in mel_b for n in bar_], 0.0, 0.3)
            T.melody(flute, b - 8, [n for bar_ in mel_b for n in bar_], 0.2, 0.22, shift=12, record=False)
    if boss: boss_layer(T)
    T.render(verb=3.2, wet=0.62, tone=6500)


# ═════════════════════════ The Silent Archive ═════════════════════════

def archive(boss=False):
    """Bb minor, hushed: harpsichord theme over a ticking clock and a whispering choir."""
    T = Track("boss_archive" if boss else "battle_archive", 96 * (1.12 if boss else 1), 32)
    Bbm, Gb, Db, Ab, Ebm, F, Cdim = (46, 1), (42, 0), (49, 0), (44, 0), (51, 1), (41, 0), (48, 1)
    prog_a = [Bbm, Gb, Db, Ab, Bbm, Gb, Ebm, F]
    prog_b = [Ebm, Bbm, Gb, F, Ebm, Bbm, Gb, F]
    mel_a = [[(77, 1), (78, 1), (77, 1), (73, 1)], [(70, 2), (73, 1), (78, 1)], [(77, 1.5), (75, .5), (73, 2)], [(72, 4)],
             [(77, 1), (82, 1), (81, 1), (77, 1)], [(78, 2), (75, 2)], [(75, 1), (73, 1), (70, 1), (73, 1)], [(72, 4)]]
    mel_b = [[(70, 2), (73, 2)], [(77, 4)], [(78, 2), (82, 2)], [(81, 4)],
             [(82, 2), (78, 2)], [(77, 3), (73, 1)], [(74, 2), (78, 2)], [(77, 4)]]
    sections = [('I', prog_a[:4]), ('A', prog_a), ('B', prog_b), ('A', prog_a), ('O', prog_a[4:])]
    b = 0
    for sec, prog in sections:
        for i, (root, minor) in enumerate(prog):
            t0 = T.at(b)
            chord = triad(root, minor)
            T.chords.append((b, root, minor))
            T.place(sub(root - 12, T.bar * 0.95), t0, 0, 0.26)
            T.place(sum(pad(m + 12, T.bar, 900, 1.6) for m in chord) / 3, t0, 0, 0.2)
            for k in range(4):                                                          # the clock
                T.place(tick(k % 2 == 0), t0 + k * T.beat, 0.6 if k % 2 else -0.6, 0.22)
            if sec != 'I':
                # Harpsichord continuo: broken chord in eighths.
                for k, iv in enumerate([0, 12, 7, 12, 3 if minor else 4, 12, 7, 12]):
                    T.place(harpsichord(root + 12 + iv, 0.6), t0 + k * T.beat / 2, -0.3, 0.13)
                T.place(taiko(False), t0, 0, 0.3)
                if i % 2 == 1: T.place(frame_drum(), t0 + 2.5 * T.beat, 0.2, 0.25)
            if sec == 'B' or (sec == 'A' and i >= 4):
                for m in chord: T.place(choir(m + 12, T.bar * 0.96) * 0.7, t0, rng.uniform(-0.6, 0.6), 0.1)
            if i % 4 == 0: T.place(bell(chord[0] + 24, 3.0), t0, 0.4, 0.14)
            if i % 4 == 3: T.place(swell(T.bar), t0, 0, 0.2)
            b += 1
        if sec == 'A':
            T.melody(lambda m, d: harpsichord(m, max(d, 0.9)), b - 8, [n for bar_ in mel_a for n in bar_], 0.15, 0.55)
            T.melody(lambda m, d: celesta(m + 12, 1.8), b - 8, [n for bar_ in mel_a for n in bar_], -0.2, 0.18, record=False)
        if sec == 'B':
            T.melody(lambda m, d: strings(m, d, 2400), b - 8, [n for bar_ in mel_b for n in bar_], 0.0, 0.32)
            T.melody(lambda m, d: celesta(m + 12, 1.8), b - 8, [n for bar_ in mel_b for n in bar_], 0.3, 0.2, record=False)
    if boss: boss_layer(T)
    T.render(verb=4.2, wet=0.75, tone=6000, drive=1.3)


if __name__ == "__main__":
    tracks = {"garden": garden, "archive": archive,
              "boss_garden": lambda: garden(boss=True), "boss_archive": lambda: archive(boss=True)}
    for name in (sys.argv[1:] or tracks):
        tracks[name]()
