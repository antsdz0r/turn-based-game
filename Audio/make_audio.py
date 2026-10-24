"""Original procedural SFX + music for TBRPG. Run: python3 make_audio.py <out_dir>
Writes Audio/Sfx/*.wav and Audio/Music/*.wav (music is converted to .ogg afterwards if ffmpeg exists)."""
import os, sys, wave
import numpy as np

SR = 44100
OUT = sys.argv[1]
rng = np.random.default_rng(3)


# ───────────── primitives ─────────────

def t_axis(dur):
    return np.arange(int(SR * dur)) / SR


def osc(kind, freq, dur, duty=0.5):
    t = t_axis(dur)
    f = np.broadcast_to(np.asarray(freq, dtype=float), t.shape) if np.ndim(freq) else np.full(t.shape, float(freq))
    phase = np.cumsum(f) / SR
    p = phase % 1.0
    if kind == 'sine':
        return np.sin(2 * np.pi * phase)
    if kind == 'square':
        return np.where(p < duty, 1.0, -1.0)
    if kind == 'tri':
        return 4 * np.abs(p - 0.5) - 1
    if kind == 'saw':
        return 2 * p - 1
    raise ValueError(kind)


def noise(dur):
    return rng.uniform(-1, 1, int(SR * dur))


def env(n, a=0.005, d=0.1, s=0.0, r=0.05, sustain_time=None):
    """ADSR over n samples."""
    a_n, d_n, r_n = int(a * SR), int(d * SR), int(r * SR)
    hold = n - a_n - d_n - r_n
    if hold < 0:
        hold = 0
    e = np.concatenate([
        np.linspace(0, 1, max(a_n, 1)),
        np.linspace(1, s, max(d_n, 1)),
        np.full(hold, s),
        np.linspace(s, 0, max(r_n, 1)),
    ])
    if len(e) < n:
        e = np.concatenate([e, np.zeros(n - len(e))])
    return e[:n]


def exp_env(n, decay):
    return np.exp(-np.arange(n) / SR / decay)


def lowpass(x, cutoff):
    """One-pole lowpass. cutoff may be an array (sweeps)."""
    cutoff = np.broadcast_to(np.asarray(cutoff, dtype=float), x.shape)
    alpha = 1 - np.exp(-2 * np.pi * cutoff / SR)
    y = np.empty_like(x)
    acc = 0.0
    for i in range(len(x)):
        acc += alpha[i] * (x[i] - acc)
        y[i] = acc
    return y


def lowpass_fast(x, cutoff):
    from scipy.signal import lfilter
    alpha = 1 - np.exp(-2 * np.pi * cutoff / SR)
    return lfilter([alpha], [1, -(1 - alpha)], x)


def highpass_fast(x, cutoff):
    return x - lowpass_fast(x, cutoff)


def bandpass(x, lo, hi):
    return highpass_fast(lowpass_fast(x, hi), lo)


def reverb(x, mix=0.25, decay=0.45):
    out = np.concatenate([x, np.zeros(int(SR * 1.2))])
    wet = np.zeros_like(out)
    for delay, g in [(0.031, 0.8), (0.047, 0.7), (0.071, 0.6), (0.113, 0.5), (0.167, 0.4)]:
        d = int(delay * SR)
        buf = np.zeros_like(out)
        buf[d:] = out[:-d]
        # feedback comb
        from scipy.signal import lfilter
        a = np.zeros(d + 1); a[0] = 1; a[d] = -decay * g
        wet += lfilter([1], a, buf) * g
    wet = lowpass_fast(wet, 3500)
    return out * (1 - mix) + wet * mix / 3


def mix_into(buf, x, start):
    s = int(start * SR)
    e = min(len(buf), s + len(x))
    if e > s:
        buf[s:e] += x[:e - s]


def norm(x, peak=0.85):
    m = np.max(np.abs(x))
    return x if m == 0 else x / m * peak


def write(path, x):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    x = np.clip(x, -1, 1)
    data = (x * 32767).astype(np.int16)
    with wave.open(path, 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(data.tobytes())


def note(n):
    """MIDI note number -> Hz"""
    return 440.0 * 2 ** ((n - 69) / 12)


# ───────────── SFX ─────────────

def sfx():
    out = {}
    # UI
    n = int(0.06 * SR)
    out['click'] = osc('square', 880, 0.06, 0.25) * exp_env(n, 0.015) * 0.5
    out['blip'] = osc('square', 660, 0.03, 0.5) * exp_env(int(0.03 * SR), 0.01) * 0.25
    # whoosh / swing
    d = 0.22; n = int(d * SR)
    sweep = np.linspace(600, 3000, n) * np.sin(np.linspace(0, np.pi, n)) + 300
    out['swing'] = lowpass(noise(d), sweep) * np.sin(np.linspace(0, np.pi, n)) ** 2 * 1.6
    # slash impact
    d = 0.25; n = int(d * SR)
    hit = bandpass(noise(d), 1500, 7000) * exp_env(n, 0.04)
    thump = osc('sine', np.linspace(180, 60, n), d) * exp_env(n, 0.06)
    out['slash_hit'] = hit * 0.9 + thump * 0.8
    # hurt (hero takes damage)
    d = 0.3; n = int(d * SR)
    out['hurt'] = (osc('square', np.linspace(300, 90, n), d, 0.4) * exp_env(n, 0.08) * 0.5
                   + lowpass_fast(noise(d), 1200) * exp_env(n, 0.05) * 0.8)
    # fire cast + hit
    d = 0.45; n = int(d * SR)
    out['fire_cast'] = lowpass(noise(d), np.linspace(400, 2500, n)) * env(n, 0.05, 0.1, 0.7, 0.2) * 1.4
    d = 0.6; n = int(d * SR)
    boom = osc('sine', np.linspace(120, 40, n), d) * exp_env(n, 0.15)
    crackle = lowpass(noise(d), np.linspace(4000, 300, n)) * exp_env(n, 0.2)
    out['fire_hit'] = boom * 0.9 + crackle * 1.2
    # ice
    d = 0.5; n = int(d * SR)
    chime = sum(osc('sine', f, d) * exp_env(n, 0.12 + i * 0.03) for i, f in enumerate([2093, 2637, 3136, 4186]))
    out['ice_cast'] = chime * 0.25 + highpass_fast(noise(d), 5000) * exp_env(n, 0.05) * 0.4
    d = 0.35; n = int(d * SR)
    shatter = highpass_fast(noise(d), 3000) * exp_env(n, 0.06)
    ping = osc('tri', np.linspace(3000, 1800, n), d) * exp_env(n, 0.08)
    out['ice_hit'] = shatter * 0.8 + ping * 0.4
    # holy
    d = 1.0; n = int(d * SR)
    bell = sum(osc('sine', note(m), d) * exp_env(n, 0.35) for m in (84, 88, 91, 96)) * 0.25
    out['holy'] = reverb(bell + highpass_fast(noise(d), 6000) * exp_env(n, 0.1) * 0.15, 0.4)
    # heal: rising arpeggio
    buf = np.zeros(int(SR * 1.0))
    for i, m in enumerate([72, 76, 79, 84, 88]):
        dd = 0.35; nn = int(dd * SR)
        mix_into(buf, osc('tri', note(m), dd) * exp_env(nn, 0.12) * 0.4, i * 0.07)
    out['heal'] = reverb(buf, 0.35)
    # arrow
    d = 0.18; n = int(d * SR)
    twang = osc('tri', np.linspace(500, 300, n), d) * exp_env(n, 0.05) * 0.5
    out['arrow'] = twang + bandpass(noise(d), 2000, 6000) * np.sin(np.linspace(0, np.pi, n)) * 0.7
    # dark bolt
    d = 0.5; n = int(d * SR)
    wob = osc('saw', 110 + 40 * np.sin(np.linspace(0, 30, n)), d)
    out['dark_cast'] = lowpass_fast(wob, 900) * env(n, 0.05, 0.1, 0.6, 0.25) * 0.8
    # buff / guard
    buf = np.zeros(int(SR * 0.5))
    for i, m in enumerate([60, 64, 67, 72]):
        mix_into(buf, osc('square', note(m), 0.12, 0.25) * exp_env(int(0.12 * SR), 0.06) * 0.3, i * 0.05)
    out['buff'] = buf
    d = 0.6; n = int(d * SR)
    clang = sum(osc('sine', f, d) * exp_env(n, 0.12) for f in (523, 1307, 2205, 3311)) * 0.3
    out['guard'] = clang + bandpass(noise(d), 2000, 8000) * exp_env(n, 0.02) * 0.6
    # enemy buff (growl)
    d = 0.6; n = int(d * SR)
    growl = lowpass_fast(osc('saw', 70 + 15 * np.sin(np.linspace(0, 60, n)), d) * (1 + 0.5 * noise(d)), 600)
    out['growl'] = growl * env(n, 0.05, 0.1, 0.8, 0.25) * 0.9
    # lock break (single) and BREAK!
    d = 0.3; n = int(d * SR)
    out['lock'] = osc('square', 1760, d, 0.5) * exp_env(n, 0.04) * 0.3 + osc('sine', 2637, d) * exp_env(n, 0.1) * 0.3
    d = 1.2; n = int(d * SR)
    crash = highpass_fast(noise(d), 1500) * exp_env(n, 0.25)
    low = osc('sine', np.linspace(90, 30, n), d) * exp_env(n, 0.35)
    glass = sum(osc('sine', f, d) * exp_env(n, 0.3) for f in (1568, 2349, 3520)) * 0.2
    out['break'] = reverb(crash * 0.8 + low * 1.0 + glass, 0.3)
    # death
    d = 0.8; n = int(d * SR)
    out['death'] = (osc('square', np.linspace(400, 40, n), d, 0.3) * exp_env(n, 0.3) * 0.4
                    + lowpass(noise(d), np.linspace(3000, 200, n)) * exp_env(n, 0.3) * 0.6)
    # summon
    d = 1.0; n = int(d * SR)
    out['summon'] = reverb(lowpass_fast(osc('saw', np.linspace(55, 110, n), d) + osc('saw', np.linspace(58, 116, n), d), 800)
                           * env(n, 0.3, 0.2, 0.7, 0.4) * 0.6, 0.4)
    # turn drum
    d = 0.5; n = int(d * SR)
    out['turn'] = osc('sine', np.linspace(110, 45, n), d) * exp_env(n, 0.18) + lowpass_fast(noise(d), 800) * exp_env(n, 0.04) * 0.5
    # jingles
    def jingle(notes, step, wave_kind='square', length=0.5):
        total = step * len(notes) + length + 0.3
        b = np.zeros(int(SR * total))
        for i, m in enumerate(notes):
            if m is None: continue
            dur = length if i == len(notes) - 1 else step * 1.5
            nn = int(dur * SR)
            tone = osc(wave_kind, note(m), dur, 0.5) * 0.25 + osc('tri', note(m - 12), dur) * 0.25
            mix_into(b, tone * env(nn, 0.005, 0.1, 0.6, 0.1), i * step)
        return reverb(b, 0.3)
    out['victory'] = jingle([62, 65, 69, 74, None, 72, 74, 77, 81], 0.12, length=1.0)
    out['defeat'] = jingle([69, 68, 67, 62], 0.3, 'tri', length=1.4)
    out['levelup'] = jingle([67, 71, 74, 79, 83], 0.07, length=0.6)
    for k, v in out.items():
        write(os.path.join(OUT, 'Sfx', f'{k}.wav'), norm(v, 0.8))


# ───────────── Music ─────────────

def drums(buf, bpm, bars, pattern):
    step = 60 / bpm / 4
    for bar in range(bars):
        for i, ch in enumerate(pattern):
            t = (bar * 16 + i) * step
            if ch == 'k':
                d = 0.3; n = int(d * SR)
                mix_into(buf, osc('sine', np.linspace(120, 40, n), d) * exp_env(n, 0.12) * 0.7, t)
            elif ch == 's':
                d = 0.2; n = int(d * SR)
                mix_into(buf, (bandpass(noise(d), 800, 5000) * exp_env(n, 0.06) * 0.35
                               + osc('tri', 180, d) * exp_env(n, 0.04) * 0.2), t)
            elif ch == 'h':
                d = 0.05; n = int(d * SR)
                mix_into(buf, highpass_fast(noise(d), 7000) * exp_env(n, 0.012) * 0.12, t)


def chord_tones(root, minor=True):
    return [root, root + (3 if minor else 4), root + 7]


def battle_theme(bpm=132, name='battle', boss=False):
    # D minor: Dm  Bb  C  A   |  Dm  Gm  Bb  A
    prog = [(50, True), (46, False), (48, False), (45, False), (50, True), (43, True), (46, False), (45, False)]
    if boss:
        prog = [(50, True), (51, False), (50, True), (49, False), (50, True), (46, False), (43, True), (45, False)]
    bars = len(prog) * 2
    step = 60 / bpm / 4
    total = bars * 16 * step
    buf = np.zeros(int(SR * (total + 0.01)))
    # melody (scale degrees as midi), two 8-bar phrases; None = rest, '-' = hold
    mel_a = [74, None, 77, 76, 74, None, 72, None, 70, None, 69, None, 70, 72, 74, None,
             72, None, 70, 69, 67, None, 69, None, 72, None, 76, None, 73, None, None, None]
    mel_b = [81, None, 79, 77, 76, None, 74, None, 77, None, 76, 74, 72, None, 70, None,
             69, 70, 72, 74, 76, None, 77, None, 76, None, 73, None, 69, None, None, None]
    melody = (mel_a + mel_b) * 2  # 8th notes
    for bar in range(bars):
        root, minor = prog[bar % len(prog)]
        tones = chord_tones(root, minor)
        bar_t = bar * 16 * step
        # bass: driving 8ths, root/octave
        for i in range(8):
            m = root - 12 + (12 if i % 4 == 3 else 0)
            d = step * 2 * 0.9; n = int(d * SR)
            mix_into(buf, lowpass_fast(osc('saw' if boss else 'tri', note(m), d), 700) * env(n, 0.005, 0.08, 0.6, 0.03) * 0.45, bar_t + i * step * 2)
        # arpeggio 16ths
        for i in range(16):
            m = tones[[0, 1, 2, 1][i % 4]] + 12 + (12 if i >= 8 and boss else 0)
            d = step * 0.9; n = int(d * SR)
            mix_into(buf, osc('square', note(m), d, 0.25) * exp_env(n, 0.05) * 0.08, bar_t + i * step)
        # pad
        d = 16 * step; n = int(d * SR)
        pad = sum(osc('saw', note(t + 12) * 1.003, d) + osc('saw', note(t + 12) * 0.997, d) for t in tones)
        mix_into(buf, lowpass_fast(pad, 1200) * env(n, 0.2, 0.2, 0.7, 0.3) * 0.03, bar_t)
    # melody
    for i, m in enumerate(melody):
        if m is None: continue
        length = 1
        while i + length < len(melody) and melody[i + length] is None and length < 2:
            length += 1
        d = step * 2 * length * 0.95; n = int(d * SR)
        tone = osc('square', note(m) * (1 + 0.004 * np.sin(np.linspace(0, d * 30, n))), d, 0.5)
        mix_into(buf, lowpass_fast(tone, 3000) * env(n, 0.01, 0.1, 0.6, 0.05) * 0.13, i * step * 2)
    pattern = 'k.h.s.h.k.hks.h.' if not boss else 'k.hkskh.k.hksksh'
    drums(buf, bpm, bars, pattern)
    write(os.path.join(OUT, 'Music', f'{name}.wav'), norm(buf, 0.8))


def menu_theme():
    bpm = 72
    prog = [(50, True), (43, True), (46, False), (45, False), (50, True), (48, False), (43, True), (45, False)]
    step = 60 / bpm / 4
    bars = 16
    total = bars * 16 * step
    buf = np.zeros(int(SR * (total + 0.01)))
    melody = [74, None, None, 72, 74, None, 77, None, 76, None, None, None, 74, None, None, None,
              70, None, None, 72, 74, None, 70, None, 69, None, None, None, None, None, None, None,
              74, None, None, 77, 81, None, 79, None, 77, None, 76, None, 74, None, None, None,
              72, None, 70, None, 69, None, 67, None, 69, None, None, None, None, None, None, None]
    for bar in range(bars):
        root, minor = prog[bar % len(prog)]
        tones = chord_tones(root, minor)
        bar_t = bar * 16 * step
        d = 16 * step; n = int(d * SR)
        pad = sum(osc('saw', note(t) * 1.004, d) + osc('saw', note(t) * 0.996, d) for t in tones + [root + 12])
        mix_into(buf, lowpass_fast(pad, 700) * env(n, 1.0, 0.5, 0.8, 1.0) * 0.05, bar_t)
        d = 16 * step; n = int(d * SR)
        mix_into(buf, osc('sine', note(root - 12), d) * env(n, 0.3, 0.5, 0.7, 0.8) * 0.35, bar_t)
        # slow plucked arpeggio
        for i, idx in enumerate([0, 1, 2, 1, 0, 2, 1, 2]):
            dd = 1.2; nn = int(dd * SR)
            mix_into(buf, osc('tri', note(tones[idx] + 12), dd) * exp_env(nn, 0.35) * 0.12, bar_t + i * step * 2)
    for i, m in enumerate(melody * 2):
        if m is None: continue
        d = step * 4 * 1.4; n = int(d * SR)
        tone = osc('tri', note(m), d) * 0.7 + osc('sine', note(m + 12), d) * 0.3
        mix_into(buf, tone * env(n, 0.02, 0.3, 0.5, 0.4) * 0.22, i * step * 2)
    write(os.path.join(OUT, 'Music', 'menu.wav'), norm(reverb(buf, 0.35)[:len(buf)], 0.75))


if __name__ == '__main__':
    sfx()
    battle_theme()
    battle_theme(150, 'boss', boss=True)
    menu_theme()
    print('done')
