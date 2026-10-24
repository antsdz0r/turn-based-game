"""Extra SFX for Ashenbound. Run: python3 make_audio_ashen.py <Audio dir>"""
import sys, os
import numpy as np
import make_audio as m

OUT = sys.argv[1]
def save(name, x): m.write(os.path.join(OUT, 'Sfx', name + '.wav'), m.norm(x, 0.8))

d = 0.6; n = int(d * m.SR)
ping = sum(m.osc('sine', f, d) * m.exp_env(n, 0.15) for f in (1760, 2637, 3951)) * 0.3
save('parry', ping + m.highpass_fast(m.noise(d), 4000) * m.exp_env(n, 0.02) * 0.8)
d = 0.25; n = int(d * m.SR)
save('miss', m.lowpass(m.noise(d), np.linspace(2500, 600, n)) * np.sin(np.linspace(0, np.pi, n)) ** 2 * 0.8)
buf = np.zeros(int(m.SR * 0.4))
for i, f in enumerate([1568, 2093]):
    dd = 0.2; nn = int(dd * m.SR)
    m.mix_into(buf, m.osc('square', f, dd, 0.5) * m.exp_env(nn, 0.06) * 0.3, i * 0.07)
save('coin', buf)
d = 0.9; n = int(d * m.SR)
save('charge', m.lowpass_fast(m.osc('saw', np.linspace(60, 160, n), d) + m.osc('saw', np.linspace(61, 162, n), d), 900) * m.env(n, 0.3, 0.1, 0.8, 0.2) * 0.6)
print('ok')
