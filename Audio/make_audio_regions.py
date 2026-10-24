"""Region battle themes. Run: python3 make_audio_regions.py <Audio dir>"""
import os, sys
import numpy as np
import make_audio as m

OUT = sys.argv[1]
SR = m.SR


def theme(name, bpm, prog, melody, lead='square', bass='tri', arp_duty=0.25, pattern='k.h.s.h.k.hks.h.', pad_cut=1200, lead_cut=3000):
    bars = len(prog) * 2
    step = 60 / bpm / 4
    total = bars * 16 * step
    buf = np.zeros(int(SR * (total + 0.01)))
    for bar in range(bars):
        root, minor = prog[bar % len(prog)]
        tones = m.chord_tones(root, minor)
        t0 = bar * 16 * step
        for i in range(8):
            n = root - 12 + (12 if i % 4 == 3 else 0)
            d = step * 2 * 0.9; k = int(d * SR)
            m.mix_into(buf, m.lowpass_fast(m.osc(bass, m.note(n), d), 700) * m.env(k, 0.005, 0.08, 0.6, 0.03) * 0.45, t0 + i * step * 2)
        for i in range(16):
            n = tones[[0, 2, 1, 2][i % 4]] + 12
            d = step * 0.9; k = int(d * SR)
            m.mix_into(buf, m.osc('square', m.note(n), d, arp_duty) * m.exp_env(k, 0.05) * 0.07, t0 + i * step)
        d = 16 * step; k = int(d * SR)
        pad = sum(m.osc('saw', m.note(t + 12) * 1.003, d) + m.osc('saw', m.note(t + 12) * 0.997, d) for t in tones)
        m.mix_into(buf, m.lowpass_fast(pad, pad_cut) * m.env(k, 0.2, 0.2, 0.7, 0.3) * 0.035, t0)
    mel = melody * 2
    for i, n in enumerate(mel):
        if n is None: continue
        d = step * 2 * 0.95 * (2 if i + 1 < len(mel) and mel[i + 1] is None else 1); k = int(d * SR)
        tone = m.osc(lead, m.note(n), d, 0.5) if lead == 'square' else m.osc(lead, m.note(n), d)
        m.mix_into(buf, m.lowpass_fast(tone, lead_cut) * m.env(k, 0.01, 0.1, 0.6, 0.05) * 0.14, i * step * 2)
    m.drums(buf, bpm, bars, pattern)
    m.write(os.path.join(OUT, 'Music', f'{name}.wav'), m.norm(m.reverb(buf, 0.25)[:len(buf)], 0.8))


# Drowned Halls: E minor, slower, triangle lead, watery reverb
drowned_prog = [(52, True), (48, False), (55, False), (50, False), (52, True), (45, True), (48, False), (47, False)]
drowned_mel = [76, None, 79, None, 78, 76, 74, None, 71, None, None, 74, 76, None, None, None,
               79, None, 78, 76, 74, None, 76, None, 71, None, 72, None, 71, None, None, None,
               76, None, 79, 81, 83, None, 81, None, 79, None, 78, None, 76, None, None, None,
               74, None, 76, 78, 79, None, 78, None, 75, None, None, None, None, None, None, None]
theme('battle_drowned', 118, drowned_prog, drowned_mel, lead='tri', bass='tri', arp_duty=0.5,
      pattern='k...h.s.k.h.s..h', lead_cut=4000)

# Cinder Throne: C minor, fast, saw bass, aggressive drums
cinder_prog = [(48, True), (44, False), (46, False), (43, False), (48, True), (51, False), (50, False), (43, False)]
cinder_mel = [72, None, 75, 77, 79, None, 77, 75, 74, None, 72, None, 70, None, 67, None,
              68, None, 70, 72, 74, None, 75, None, 74, None, 72, None, 71, None, None, None,
              79, None, 80, 79, 77, None, 75, None, 77, None, 75, 74, 72, None, 70, None,
              68, 70, 72, 74, 75, None, 74, None, 71, None, None, None, 67, None, None, None]
theme('battle_cinder', 146, cinder_prog, cinder_mel, lead='square', bass='saw', arp_duty=0.125,
      pattern='k.hkskh.k.hksksh', pad_cut=900)
print('ok')
