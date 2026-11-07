"""Синтетические позы для ритм-режима: руки в стороны, вверх, присед, аккорд «руки в стороны»
и «бытовые» движения, которые НЕ должны нажимать стрелки. Модель тела и камера — из gen_sequences.py.
Метка попытки — какие стрелки должны сработать: L, R, U, D, LR или «-» (ничего)."""
import random, sys
import gen_sequences
from gen_sequences import Body, Camera, packet, bump, smooth, FPS, trial_punch, trial_squat, trial_idle

def hold_pose(rng, arms_fn, squat=0.0):
    rise, hold, fall = rng.uniform(0.2, 0.4), rng.uniform(0.25, 0.6), rng.uniform(0.2, 0.4)
    def f(t):
        k = bump(t, rise, hold, fall)
        return dict(arms=arms_fn(k), squat=squat*k)
    return f, rise+hold+fall

def side(rng, which, amount=0.5):
    # raise_side: 0.5 = рука горизонтально в сторону; L у модели — слева на экране (картинка зеркальная)
    a = rng.uniform(0.46, 0.56)*amount/0.5
    if which == 'LR':
        return hold_pose(rng, lambda k: {'L': ('raise_side', a*k), 'R': ('raise_side', a*k)})
    other = 'R' if which == 'L' else 'L'
    return hold_pose(rng, lambda k: {which: ('raise_side', a*k), other: ('down', 0)})

def up(rng, kind):
    return hold_pose(rng, lambda k: {'L': (kind, k), 'R': (kind, k)})

def build(seed, dist):
    rng = random.Random(seed)
    body, cam = Body(rng), Camera(dist, gen_sequences.CAM_HEIGHT)
    trials = []
    for _ in range(6):
        trials.append(('L', side(rng, 'L')))
        trials.append(('R', side(rng, 'R')))
        trials.append(('LR', side(rng, 'LR')))
        trials.append(('U', up(rng, 'raise_fwd')))
        trials.append(('U', up(rng, 'raise_side')))   # руки вверх через стороны: проходят «в сторону», но это не стрелка
        trials.append(('D', trial_squat(rng, depth=rng.uniform(0.5, 1.0), arms=rng.choice(['fwd', 'hips', None]))))
        trials.append(('D', trial_squat(rng, depth=rng.uniform(0.5, 1.0))))
        # не должны нажимать ничего
        trials.append(('-', side(rng, rng.choice('LR'), amount=0.22)))         # рука чуть отведена
        trials.append(('-', trial_punch(rng, 'punch_fwd')))                     # удар вперёд, к камере
        trials.append(('-', trial_squat(rng, depth=rng.uniform(0.10, 0.18))))  # лёгкое пружинение
        for k in ('sway', 'walk', 'scratch', 'clap', 'guard'):
            trials.append(('-', trial_idle(rng, k)))
    rng.shuffle(trials)

    lines, seq, t = [], 0, 0.0
    def emit(params):
        nonlocal seq, t
        lines.append("%.4f|%s" % (t, packet(seq, body.pose(**params), cam, rng)))
        seq += 1; t += 1/FPS
    for _ in range(int(1.5*FPS)): emit({})
    for label, (fn, dur) in trials:
        lines.append("#LABEL|%.4f|%.4f|%s" % (t, dur, label))
        for i in range(int(dur*FPS)): emit(fn(i/FPS))
        for _ in range(int(rng.uniform(0.6, 1.0)*FPS)): emit({})
    return lines

if __name__ == '__main__':
    if len(sys.argv) > 3 and 'crop' in sys.argv[3]:   # кисти над головой выходят за верх кадра
        gen_sequences.CROP = True
        gen_sequences.CAM_HEIGHT = 0.7
    print("\n".join(build(int(sys.argv[1]), float(sys.argv[2]))))
