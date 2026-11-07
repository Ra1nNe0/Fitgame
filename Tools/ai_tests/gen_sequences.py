"""Синтетические последовательности поз для проверки классификатора движений.
3D-модель тела -> проекция в камеру -> пакеты FQ1, как от настоящего сервера.
Каждая попытка подписана ожидаемым ответом."""
import math, random, sys

W, H, F = 640, 480, 461.0
FPS = 30.0
import os
NOISE_I = float(os.environ.get("NI", "0.003")); NOISE_W = float(os.environ.get("NW", "0.012"))

def v_add(a, b): return (a[0]+b[0], a[1]+b[1], a[2]+b[2])
def v_mul(a, k): return (a[0]*k, a[1]*k, a[2]*k)
def lerp(a, b, t): return tuple(a[i] + (b[i]-a[i])*t for i in range(3))

class Body:
    """Суставы в метрах: x вправо, y ВВЕРХ от пола, z к камере отрицательный."""
    def __init__(self, rng):
        s = rng.uniform(0.9, 1.1)   # рост
        self.s = s
        self.thigh, self.shin, self.torso = 0.46*s, 0.44*s, 0.50*s
        self.upper, self.fore = 0.29*s, 0.27*s

    def pose(self, jump=0.0, squat=0.0, arms=None, sway=(0,0), lean=0.0):
        s = self.s
        ank_h = 0.08
        theta = 180 - 100*squat                     # угол в колене
        bend = 180 - theta
        a = math.radians(0.45*bend); b = math.radians(0.55*bend)
        J = {}
        for side, sx in (('L', -1), ('R', 1)):
            ank = (sx*0.13*s + sway[0], ank_h + jump, 0.0)
            knee = v_add(ank, (0, self.shin*math.cos(a), -self.shin*math.sin(a)))
            hip = v_add(knee, (-sx*0.01, self.thigh*math.cos(b), self.thigh*math.sin(b)))
            hip = (sx*0.12*s + sway[0], hip[1], hip[2])
            J['ank'+side], J['knee'+side], J['hip'+side] = ank, knee, hip
        hipc = lerp(J['hipL'], J['hipR'], 0.5)
        phi = math.radians(0.3*bend + lean)
        up = (0, math.cos(phi), -math.sin(phi))
        shc = v_add(hipc, v_mul(up, self.torso))
        shc = (shc[0] + sway[1], shc[1], shc[2])
        J['shL'] = v_add(shc, (-0.20*s, 0, 0)); J['shR'] = v_add(shc, (0.20*s, 0, 0))
        J['nose'] = v_add(shc, v_mul(up, 0.22*s)); J['nose'] = v_add(J['nose'], (0, 0, -0.08))
        arms = arms or {}
        for side, sx in (('L', -1), ('R', 1)):
            sh = J['sh'+side]
            spec = arms.get(side, ('down', 0))
            el, wr = self.arm(sh, sx, spec)
            J['el'+side], J['wr'+side] = el, wr
        return J

    def arm(self, sh, sx, spec):
        kind, t = spec
        U, Fo = self.upper, self.fore
        down_el = v_add(sh, (sx*0.04, -U, 0)); down_wr = v_add(down_el, (sx*0.01, -Fo, -0.02))
        if kind == 'down':
            return down_el, down_wr
        if kind == 'hips':      # руки на поясе
            return v_add(sh, (sx*0.18, -U*0.8, 0.05)), v_add(sh, (sx*0.08, -U-0.08, 0))
        guard_el = v_add(sh, (sx*0.03, -U*0.85, -0.12)); guard_wr = v_add(sh, (-sx*0.02, -0.06, -0.26))
        if kind in ('punch_fwd', 'punch_side', 'punch_half'):
            if kind == 'punch_side':
                ext_el, ext_wr = v_add(sh, (sx*U, 0, 0)), v_add(sh, (sx*(U+Fo), 0.01, 0))
            else:
                ext_el, ext_wr = v_add(sh, (0, -0.01, -U)), v_add(sh, (0, 0.0, -(U+Fo)))
            if kind == 'punch_half':
                ext_wr = lerp(guard_wr, ext_wr, 0.45); ext_el = lerp(guard_el, ext_el, 0.45)
            return lerp(guard_el, ext_el, t), lerp(guard_wr, ext_wr, t)
        if kind == 'guard':
            return guard_el, guard_wr
        if kind in ('raise_fwd', 'raise_side', 'raise_half', 'raise_bent', 'raise_low'):
            ang = math.radians({'raise_half': 95, 'raise_low': 150}.get(kind, 180)*t)
            if kind == 'raise_side':
                d = (sx*math.sin(ang), -math.cos(ang), 0)
            else:
                d = (0, -math.cos(ang), -math.sin(ang))
            el = v_add(sh, v_mul(d, U))
            if kind == 'raise_bent':   # локти согнуты (руки за голову)
                wr = v_add(el, v_mul((-sx*0.7, 0.3, 0.2), Fo))
            else:
                wr = v_add(el, v_mul(d, Fo))
            return el, wr
        if kind == 'swing':     # мах рукой при ходьбе
            ang = math.radians(35*t)
            d = (0, -math.cos(ang), -math.sin(ang))
            el = v_add(sh, v_mul(d, U)); wr = v_add(el, v_mul((0, -math.cos(ang*1.4), -math.sin(ang*1.4)), Fo))
            return el, wr
        if kind == 'scratch':   # почесать голову одной рукой
            el = v_add(sh, (sx*0.25, 0.15, -0.05)); wr = v_add(sh, (sx*0.05, 0.28, -0.05))
            return lerp(down_el, el, t), lerp(down_wr, wr, t)
        if kind == 'clap':      # хлопок перед грудью
            el = v_add(sh, (sx*0.05, -0.22, -0.18)); wr = v_add(sh, (-sx*0.17, -0.12, -0.32))
            return lerp(down_el, el, t), lerp(down_wr, wr, t)
        raise ValueError(kind)

IDX = {'nose': 0, 'shL': 11, 'shR': 12, 'elL': 13, 'elR': 14, 'wrL': 15, 'wrR': 16,
       'hipL': 23, 'hipR': 24, 'kneeL': 25, 'kneeR': 26, 'ankL': 27, 'ankR': 28}

class Camera:
    def __init__(self, dist, height=1.1):
        self.dist, self.height = dist, height
    def project(self, p):
        z = self.dist + p[2]
        u = W/2 + F*p[0]/z
        v = H/2 + F*(self.height - p[1])/z
        return u/W, v/H

def packet(seq, J, cam, rng, noise_img=NOISE_I, noise_w=NOISE_W):
    pts = [None]*33
    hipc = lerp(J['hipL'], J['hipR'], 0.5)
    for name, i in IDX.items():
        pts[i] = J[name]
    # остальные точки: лицо у носа, кисти у запястий, стопы у лодыжек
    for i in range(1, 11): pts[i] = v_add(J['nose'], (0.02*(i-5), 0.03, 0.02))
    for i, w in ((17, 'wrL'), (19, 'wrL'), (21, 'wrL'), (18, 'wrR'), (20, 'wrR'), (22, 'wrR')):
        pts[i] = v_add(J[w], (0, -0.04, -0.02))
    for i, a in ((29, 'ankL'), (31, 'ankL'), (30, 'ankR'), (32, 'ankR')):
        pts[i] = v_add(J[a], (0, -0.05, -0.06 if i > 30 else 0.03))
    img, wl = [], []
    for idx, p in enumerate(pts):
        u, v = cam.project(p)
        u += rng.gauss(0, noise_img); v += rng.gauss(0, noise_img)
        inside = -0.02 < v < 1.0 and 0 < u < 1
        vis = 0.98 if inside else 0.08
        q = p
        if CROP and not inside and v < 0 and 15 <= idx <= 22:
            # точка выше кадра: нейросеть «теряет» её и ставит примерно на уровень плеч
            q = (p[0], hipc[1] + 0.45, p[2])
            v = 0.03
        img += [u, v, vis]
        wl += [q[0]-hipc[0] + rng.gauss(0, noise_w), -(q[1]-hipc[1]) + rng.gauss(0, noise_w), (q[2]-hipc[2]) + rng.gauss(0, noise_w*1.5)]
    f = lambda arr: ",".join("%.4f" % x for x in arr)
    return "FQ1|pose|%d|30.0|%d|%d|%s|%s|" % (seq, W, H, f(img), f(wl))

def smooth(t):  # 0..1 -> 0..1 плавно
    return 0.5 - 0.5*math.cos(math.pi*max(0, min(1, t)))

def bump(t, rise, hold, fall):
    if t < rise: return smooth(t/rise)
    if t < rise+hold: return 1.0
    if t < rise+hold+fall: return 1 - smooth((t-rise-hold)/fall)
    return 0.0

# Каждая попытка: (ожидаемый ответ, длительность, функция t -> параметры позы)
def trial_jump(rng, h=None, arms=False):
    h = h if h is not None else rng.uniform(0.07, 0.20)
    dip = rng.uniform(0.0, 0.25)
    def f(t):
        # 0..0.18 присед-замах, 0.18..0.18+air полёт, затем приземление
        air = 2*math.sqrt(2*max(h, 0.01)/9.8)
        if t < 0.18: return dict(squat=dip*smooth(t/0.18))
        if t < 0.18+air:
            x = (t-0.18)/air
            a = {'L': ('raise_fwd', 0.9), 'R': ('raise_fwd', 0.9)} if arms else None
            return dict(jump=4*h*x*(1-x), squat=dip*(1-smooth(x*4)), arms=a)
        x = t-0.18-air
        return dict(squat=dip*0.8*bump(x, 0.08, 0.02, 0.15))
    air = 2*math.sqrt(2*max(h, 0.01)/9.8)
    return f, 0.18+air+0.3

def trial_squat(rng, depth=None, arms='fwd'):
    d = depth if depth is not None else rng.uniform(0.45, 1.0)
    rise, hold, fall = rng.uniform(0.35, 0.7), rng.uniform(0.1, 0.5), rng.uniform(0.35, 0.6)
    def f(t):
        k = bump(t, rise, hold, fall)
        a = None
        if arms == 'fwd': a = {'L': ('raise_fwd', 0.5*k), 'R': ('raise_fwd', 0.5*k)}
        elif arms == 'hips': a = {'L': ('hips', 0), 'R': ('hips', 0)}
        return dict(squat=d*k, arms=a)
    return f, rise+hold+fall

def trial_punch(rng, kind='punch_fwd', side=None):
    side = side or rng.choice('LR')
    other = 'R' if side == 'L' else 'L'
    pre, ext, hold, back = rng.uniform(0.15, 0.3), rng.uniform(0.10, 0.18), rng.uniform(0.05, 0.2), rng.uniform(0.15, 0.3)
    def f(t):
        if t < pre:
            k = smooth(t/pre)    # поднять руку в стойку
            return dict(arms={side: ('guard', 0), other: ('guard', 0)}) if k > 0.5 else dict(arms={side: ('down', 0)})
        x = t-pre
        e = bump(x, ext, hold, back)
        return dict(arms={side: (kind, e), other: ('guard', 0)})
    return f, pre+ext+hold+back+0.15

def trial_hands(rng, kind='raise_fwd'):
    rise, hold, fall = rng.uniform(0.3, 0.5), rng.uniform(0.25, 0.6), rng.uniform(0.3, 0.5)
    def f(t):
        k = bump(t, rise, hold, fall)
        return dict(arms={'L': (kind, k), 'R': (kind, k)})
    return f, rise+hold+fall

def trial_idle(rng, kind):
    dur = rng.uniform(1.5, 2.5)
    def f(t):
        if kind == 'sway':
            return dict(sway=(0.04*math.sin(t*2.1), 0.03*math.sin(t*1.3)))
        if kind == 'walk':   # ходьба на месте
            ph = math.sin(t*2*math.pi*1.6)
            return dict(jump=0.015*abs(ph), arms={'L': ('swing', ph), 'R': ('swing', -ph)})
        if kind == 'scratch':
            return dict(arms={'R': ('scratch', bump(t, 0.4, 0.8, 0.4))})
        if kind == 'clap':
            return dict(arms={'L': ('clap', bump(t % 0.8, 0.2, 0.1, 0.2)), 'R': ('clap', bump(t % 0.8, 0.2, 0.1, 0.2))})
        if kind == 'bow':    # кивок/наклон корпуса
            return dict(lean=25*bump(t, 0.5, 0.3, 0.5))
        if kind == 'guard':
            return dict(arms={'L': ('guard', 0), 'R': ('guard', 0)})
        if kind == 'step':   # шаг ближе к камере
            return dict()
        raise ValueError(kind)
    return f, dur

LOW_RAISE = CROP = False
CAM_HEIGHT = 1.1


def build(seed, dist):
    rng = random.Random(seed)
    body = Body(rng)
    cam = Camera(dist, CAM_HEIGHT)
    trials = []
    for _ in range(6):
        trials.append(('Jump', trial_jump(rng)))
        trials.append(('Jump', trial_jump(rng, arms=True)))
        trials.append(('Squat', trial_squat(rng, depth=rng.uniform(0.6, 1.0), arms=rng.choice(['fwd', 'hips', None]))))
        trials.append(('Squat', trial_squat(rng, depth=rng.uniform(0.6, 1.0))))
        trials.append(('Punch', trial_punch(rng, 'punch_fwd')))
        trials.append(('Punch', trial_punch(rng, 'punch_side')))
        trials.append(('HandsUp', trial_hands(rng, 'raise_fwd')))
        trials.append(('HandsUp', trial_hands(rng, 'raise_side')))
        if LOW_RAISE:
            trials.append(('HandsUp', trial_hands(rng, 'raise_low')))   # руки «к макушке», не до конца
        trials.append(('Weak:Squat', trial_squat(rng, depth=rng.uniform(0.12, 0.22))))
        trials.append(('Weak:Jump', trial_jump(rng, h=rng.uniform(0.02, 0.035))))
        trials.append(('Weak:Punch', trial_punch(rng, 'punch_half')))
        trials.append(('Weak:HandsUp', trial_hands(rng, 'raise_half')))
        for k in ('sway', 'walk', 'scratch', 'clap', 'bow', 'guard'):
            trials.append(('Idle', trial_idle(rng, k)))
    rng.shuffle(trials)

    lines, seq, t = [], 0, 0.0
    def emit(params):
        nonlocal seq, t
        J = body.pose(**params)
        lines.append("%.4f|%s" % (t, packet(seq, J, cam, rng)))
        seq += 1; t += 1/FPS
    for _ in range(int(2.5*FPS)): emit({})          # калибровка
    for label, (fn, dur) in trials:
        lines.append("#LABEL|%.4f|%.4f|%s" % (t, dur, label))
        n = int(dur*FPS)
        for i in range(n): emit(fn(i/FPS))
        for _ in range(int(rng.uniform(0.8, 1.3)*FPS)): emit({})
    return lines

if __name__ == '__main__':
    seed, dist = int(sys.argv[1]), float(sys.argv[2])
    mode = sys.argv[3] if len(sys.argv) > 3 else ''
    LOW_RAISE = 'low' in mode
    CROP = 'crop' in mode
    CAM_HEIGHT = 0.7 if CROP else 1.1
    print("\n".join(build(seed, dist)))
