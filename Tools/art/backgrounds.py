"""Фоны четырёх миров: дальний слой, ближний слой и земля. Всё бесшовно по горизонтали."""
import math
import random
from px import Canvas
from sprites import blob

W, H = 512, 256
TOP = 76        # выше этой строки камера не видит
HORIZON = 214   # верх земли
BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def gradient(c, y0, y1, colors):
    n = len(colors) - 1
    for y in range(c.h):
        t = min(max((y - y0) / float(y1 - y0), 0.0), 1.0) * n
        i = min(int(t), n - 1)
        f = t - i
        for x in range(c.w):
            c.p[y][x] = colors[i + 1] if f > (BAYER[y % 4][x % 4] + 0.5) / 16.0 else colors[i]


def skyline(seed, base, amps):
    """Периодическая линия высот: сумма синусов с целым числом волн на ширину текстуры."""
    r = random.Random(seed)
    ph = [r.uniform(0, 6.28) for _ in amps]
    return [base + sum(a * math.sin(2 * math.pi * k * x / W + p) for (k, a), p in zip(amps, ph)) for x in range(W)]


def fill_below(c, line, col, top_col=None, depth=2, step=1):
    for x in range(W):
        y0 = int(line[x] // step * step)
        for y in range(max(y0, 0), c.h):
            c.p[y][x] = col
        if top_col:
            for d in range(depth):
                if 0 <= y0 + d < c.h:
                    c.p[y0 + d][x] = top_col


def triangle(c, cx, base_y, half, height, col, light=None):
    for i in range(height):
        w = int(half * (i + 1) / height)
        y = base_y - height + i
        for x in range(cx - w, cx + w + 1):
            c.px(x, y, light if (light and x < cx - w * 0.2) else col, wrap=True)


# ------------------------------------------------------------------ лес
def forest():
    r = random.Random(11)
    far = Canvas(W, H)
    gradient(far, TOP, HORIZON - 30, ['blue', 'blue', 'cyan', 'cyan', 'white'])
    for _ in range(7):                                         # облака
        x, y = r.randrange(W), r.randrange(TOP + 10, TOP + 70)
        for k in range(4):
            blob(far, x + k * 9, y + r.randrange(-2, 3), r.randrange(8, 13), r.randrange(4, 7), 'white', 'silver', None, wrap=True)
    m = skyline(1, 150, [(2, 22), (3, 14), (7, 6)])
    for x in range(W):
        y0 = int(m[x])
        for y in range(y0, H):
            far.p[y][x] = 'gray' if (x + y) % 37 < 18 and y - y0 < 40 else 'slate'
        for d in range(0, max(0, 8 - int((m[x] - 110) / 5))):
            if y0 + d < H:
                far.p[y0 + d][x] = 'white'
    h = skyline(2, 188, [(3, 9), (5, 5), (9, 2)])
    fill_below(far, h, 'forest', 'green', 2)
    for _ in range(46):                                        # ёлочки на холмах
        x = r.randrange(W)
        triangle(far, x, int(h[x]) + 3, r.randrange(3, 6), r.randrange(9, 16), 'dteal')

    near = Canvas(W, H)
    xs = list(range(20, W, 84))
    for i, x in enumerate(xs):
        x += r.randrange(-14, 15)
        th = r.randrange(60, 86)
        near.rect(x - 4, HORIZON - th, 9, th + 44, 'bark', wrap=True)
        near.rect(x - 4, HORIZON - th, 3, th + 44, 'dbark', wrap=True)
        near.rect(x + 3, HORIZON - th, 1, th + 44, 'wood', wrap=True)
        cy = HORIZON - th
        for (dx, dy, rx, ry) in ((0, -12, 26, 20), (-18, 0, 18, 14), (17, 2, 19, 14), (2, -28, 17, 13)):
            blob(near, x + dx, cy + dy, rx, ry, 'green', 'forest', None, wrap=True)
        for _ in range(26):
            a, d = r.uniform(0, 6.28), r.uniform(0, 1)
            px_, py_ = x + math.cos(a) * 24 * d - 4, cy - 14 + math.sin(a) * 20 * d - 4
            if near.get(int(px_) % W, int(py_)) == 'green':
                near.px(px_, py_, 'lime', wrap=True); near.px(px_ + 1, py_, 'lime', wrap=True)
    for _ in range(9):                                         # кусты
        x = r.randrange(W)
        blob(near, x, HORIZON + 2, r.randrange(9, 15), r.randrange(6, 10), 'green', 'forest', 'lime', wrap=True)

    g = Canvas(W, 64)
    g.rect(0, 0, W, 64, 'wood')
    for y in range(64):
        for x in range(W):
            v = r.random()
            if y > 40 or (y > 24 and v < 0.35):
                g.p[y][x] = 'bark'
            if v < 0.03 and y > 7:
                g.p[y][x] = 'peach' if y < 30 else 'dbark'
    for x in range(W):
        t = 5 + (1 if (x // 3) % 4 == 0 else 0) + (1 if (x // 7) % 3 == 0 else 0)
        for y in range(t):
            g.p[y][x] = 'lime' if y < 2 else 'green'
        g.p[t][x] = 'forest'
        if x % 11 == 3:
            g.p[0][x] = 'yellow' if (x // 11) % 4 == 0 else 'lime'
    return far, near, g


# ------------------------------------------------------------------ пещера
def cave():
    r = random.Random(22)
    far = Canvas(W, H)
    gradient(far, TOP, HORIZON, ['ink', 'ink', 'night', 'night', 'ink'])
    a = skyline(3, 150, [(2, 26), (5, 12), (11, 5)])
    fill_below(far, a, 'night', 'steel', 1, step=4)
    b = skyline(4, 104, [(3, 14), (7, 9), (13, 4)])          # сталактиты сверху
    for x in range(W):
        for y in range(0, int(b[x] // 3 * 3)):
            if far.p[y][x] in ('ink', 'night'):
                far.p[y][x] = 'night' if y < int(b[x]) - 3 else 'steel'
    for _ in range(26):                                        # кристаллы
        x = r.randrange(W)
        y = int(a[x] // 4 * 4) + r.randrange(0, 3)
        col, hi = r.choice([('blue', 'cyan'), ('purple', 'magenta'), ('blue', 'cyan')])
        hgt = r.randrange(5, 12)
        for i in range(hgt):
            w = 1 + min(i, hgt - i) // 2
            far.rect(x - w, y - i, w * 2 + 1, 1, col, wrap=True)
            far.px(x - w, y - i, hi, wrap=True)
        far.px(x, y - hgt - 2, 'white', wrap=True)
    for _ in range(60):
        far.px(r.randrange(W), r.randrange(TOP, 200), r.choice(['steel', 'slate', 'night']))

    near = Canvas(W, H)
    for i in range(7):
        x = 30 + i * 73 + r.randrange(-15, 16)
        ln = r.randrange(34, 74)
        hw = r.randrange(9, 16)
        for d in range(ln):                                    # сталактит
            w = int(hw * (1 - d / float(ln)) ** 1.3) + 1
            near.rect(x - w, TOP + d, w * 2, 1, 'steel', wrap=True)
            near.rect(x - w, TOP + d, max(1, w // 3), 1, 'slate', wrap=True)
            near.rect(x + w - max(1, w // 3), TOP + d, max(1, w // 3), 1, 'night', wrap=True)
        near.rect(x - hw - 4, TOP - 80, hw * 2 + 8, 80, 'steel', wrap=True)
        x2 = x + 36 + r.randrange(-8, 9)
        ln2 = r.randrange(18, 46)
        hw2 = r.randrange(8, 14)
        for d in range(ln2 + 44):                              # сталагмит
            k = max(0.0, 1 - d / float(ln2))
            w = int(hw2 * (1 - k) ** 0.8) + 1
            y = HORIZON - ln2 + d
            near.rect(x2 - w, y, w * 2, 1, 'steel', wrap=True)
            near.rect(x2 - w, y, max(1, w // 3), 1, 'slate', wrap=True)
            near.rect(x2 + w - max(1, w // 3), y, max(1, w // 3), 1, 'night', wrap=True)
    near.outline('ink')

    g = Canvas(W, 64)
    g.rect(0, 0, W, 64, 'steel')
    for y in range(64):
        for x in range(W):
            v = r.random()
            if y > 36 or (y > 20 and v < 0.3):
                g.p[y][x] = 'night'
            if v < 0.02 and y > 8:
                g.p[y][x] = 'slate'
    for x in range(W):
        t = 3 + (1 if (x // 5) % 3 == 0 else 0)
        for y in range(t):
            g.p[y][x] = 'gray' if y == 0 else 'slate'
        g.p[t][x] = 'night'
    for x0 in range(0, W, 32):                                 # швы плит
        xo = x0 + r.randrange(0, 12)
        for y in range(5, 20):
            g.px(xo + (y // 6), y, 'night')
    for _ in range(14):
        x, y = r.randrange(W), r.randrange(10, 40)
        g.px(x, y, 'cyan'); g.px(x + 1, y, 'blue'); g.px(x, y + 1, 'blue')
    return far, near, g


# ------------------------------------------------------------------ вулкан
def volcano():
    r = random.Random(33)
    far = Canvas(W, H)
    gradient(far, TOP, HORIZON - 14, ['dbark', 'dbark', 'crimson', 'red', 'orange', 'amber'])
    for _ in range(8):                                         # дым
        x, y = r.randrange(W), r.randrange(TOP + 6, TOP + 60)
        for k in range(5):
            blob(far, x + k * 10, y + r.randrange(-3, 4), r.randrange(9, 15), r.randrange(4, 7), 'bark', 'dbark', None, wrap=True)
    for cx, hgt, hw in ((120, 96, 120), (380, 74, 100)):      # вулканы
        for d in range(hgt + 40):
            k = d / float(hgt)
            w = int(18 + (hw - 18) * min(k, 1.4) ** 1.25)
            y = HORIZON - hgt + d
            far.rect(cx - w, y, w * 2, 1, 'dbark', wrap=True)
            far.rect(cx - w, y, max(2, w // 6), 1, 'bark', wrap=True)
        ty = HORIZON - hgt
        far.rect(cx - 16, ty - 2, 32, 3, 'orange', wrap=True); far.rect(cx - 12, ty - 3, 24, 2, 'yellow', wrap=True)
        for sx in (-9, 2, 10):                                 # потоки лавы
            x = cx + sx
            for d in range(r.randrange(30, 70)):
                x += r.choice([-1, 0, 0, 1])
                far.px(x, ty + d, 'orange', wrap=True); far.px(x + 1, ty + d, 'amber' if d % 3 else 'yellow', wrap=True)
    rdg = skyline(5, 196, [(4, 7), (9, 4), (17, 2)])
    fill_below(far, rdg, 'ink', 'dbark', 1, step=2)

    near = Canvas(W, H)
    for i in range(6):
        x = 40 + i * 85 + r.randrange(-18, 19)
        hgt = r.randrange(40, 92)
        hw = r.randrange(10, 18)
        for d in range(hgt + 44):
            k = min(d / float(hgt), 1.0)
            w = int(2 + hw * k ** 0.7) + (1 if (d // 5) % 2 else 0)
            y = HORIZON - hgt + d
            near.rect(x - w, y, w * 2, 1, 'ink', wrap=True)
            near.rect(x - w, y, max(1, w // 4), 1, 'dbark', wrap=True)
        for _ in range(3):
            lx, ly = x + r.randrange(-hw // 2, hw // 2), HORIZON - r.randrange(4, hgt // 2 + 5)
            for d in range(r.randrange(6, 14)):
                near.px(lx + (d // 3) % 2, ly + d, 'orange', wrap=True)
    for _ in range(40):                                        # искры
        x, y = r.randrange(W), r.randrange(TOP + 10, HORIZON)
        near.px(x, y, r.choice(['amber', 'orange', 'yellow']), wrap=True)

    g = Canvas(W, 64)
    g.rect(0, 0, W, 64, 'dbark')
    for y in range(64):
        for x in range(W):
            v = r.random()
            if y > 30 or (y > 16 and v < 0.35):
                g.p[y][x] = 'ink'
            if v < 0.02 and y > 6:
                g.p[y][x] = 'bark'
    for x in range(W):
        t = 3 + (1 if (x // 4) % 3 == 0 else 0)
        for y in range(t):
            g.p[y][x] = 'wood' if y == 0 else 'bark'
        g.p[t][x] = 'ink'
    for x0 in range(10, W, 46):                                # лавовые трещины
        x, y = x0 + r.randrange(0, 20), 4
        for d in range(r.randrange(10, 26)):
            x += r.choice([-1, 0, 1, 1])
            y += 1
            g.px(x, y, 'orange', wrap=True); g.px(x + 1, y, 'yellow' if d % 2 else 'amber', wrap=True)
    return far, near, g


# ------------------------------------------------------------------ замок
def castle():
    r = random.Random(44)
    far = Canvas(W, H)
    gradient(far, TOP, HORIZON - 20, ['ink', 'night', 'night', 'steel', 'steel'])
    for _ in range(110):
        far.px(r.randrange(W), r.randrange(TOP, HORIZON - 50), r.choice(['white', 'silver', 'slate', 'slate']))
    blob(far, 400, TOP + 38, 17, 17, 'sand', 'peach', None)
    for x, y in ((394, TOP + 32), (406, TOP + 44), (398, TOP + 46)):
        blob(far, x, y, 3, 3, 'peach', 'peach', None)
    x = 0
    while x < W:                                               # силуэт замка
        w = r.randrange(26, 60)
        tall = r.random() < 0.4
        hgt = r.randrange(70, 104) if tall else r.randrange(34, 58)
        y0 = HORIZON - hgt
        far.rect(x, y0, w, hgt + 44, 'ink', wrap=True)
        far.rect(x, y0, 2, hgt + 44, 'steel', wrap=True)
        far.rect(x, y0, w, 1, 'steel', wrap=True)
        for bx in range(x, x + w, 8):                          # зубцы
            far.rect(bx, y0 - 5, 5, 5, 'ink', wrap=True)
            far.rect(bx, y0 - 5, 5, 1, 'steel', wrap=True)
        if tall:
            triangle(far, x + w // 2, y0 - 4, w // 2 + 2, 22, 'crimson', 'red')
            far.rect(x + w // 2, y0 - 36, 1, 10, 'silver', wrap=True); far.rect(x + w // 2 + 1, y0 - 36, 6, 4, 'red', wrap=True)
        for _ in range(r.randrange(1, 4)):
            wx, wy = x + r.randrange(5, max(6, w - 6)), y0 + r.randrange(10, max(11, hgt - 12))
            far.rect(wx, wy, 3, 5, 'amber', wrap=True); far.px(wx + 1, wy, 'yellow', wrap=True)
        x += w

    near = Canvas(W, H)
    for i in range(4):
        x = 40 + i * 128
        near.rect(x - 9, TOP - 10, 18, HORIZON - TOP + 60, 'slate', wrap=True)
        near.rect(x - 9, TOP - 10, 4, HORIZON - TOP + 60, 'gray', wrap=True)
        near.rect(x + 5, TOP - 10, 4, HORIZON - TOP + 60, 'steel', wrap=True)
        for y in range(TOP, HORIZON, 14):
            near.rect(x - 9, y, 18, 1, 'steel', wrap=True)
        near.rect(x - 13, HORIZON - 8, 26, 8, 'gray', wrap=True); near.rect(x - 13, HORIZON - 2, 26, 2, 'steel', wrap=True)
        near.rect(x - 13, TOP + 12, 26, 6, 'gray', wrap=True)
        bx = x + 40                                            # знамя
        near.rect(bx - 8, TOP + 14, 16, 44, 'crimson', wrap=True); near.rect(bx - 6, TOP + 14, 12, 42, 'red', wrap=True)
        triangle(near, bx, TOP + 67, 8, 9, 'crimson')
        near.rect(bx - 10, TOP + 12, 20, 2, 'amber', wrap=True)
        for (dx, dy) in ((-2, 10), (0, 9), (2, 10), (-3, 13), (3, 13), (-2, 16), (0, 17), (2, 16), (0, 13)):
            near.px(bx + dx, TOP + 16 + dy, 'amber', wrap=True)            # герб «Z»
        tx = x + 84                                            # факел
        near.rect(tx - 1, HORIZON - 60, 3, 12, 'bark', wrap=True)
        blob(near, tx, HORIZON - 64, 4, 5, 'orange', 'red', None, wrap=True)
        blob(near, tx, HORIZON - 63, 2, 3, 'yellow', 'amber', None, wrap=True)
    near.outline('ink')

    g = Canvas(W, 64)
    g.rect(0, 0, W, 64, 'slate')
    for y in range(64):
        row = (y - 6) // 10
        for x in range(W):
            off = 16 if row % 2 else 0
            if y >= 6 and ((y - 6) % 10 == 0 or (x + off) % 32 == 0):
                g.p[y][x] = 'steel'
            elif y >= 6 and (y - 6) % 10 == 1:
                g.p[y][x] = 'gray'
            if y > 44:
                g.p[y][x] = 'steel' if g.p[y][x] != 'steel' else 'night'
    g.rect(0, 0, W, 5, 'red'); g.rect(0, 0, W, 1, 'amber'); g.rect(0, 4, W, 1, 'crimson'); g.rect(0, 5, W, 1, 'ink')
    for x in range(0, W, 16):
        g.px(x + 4, 2, 'amber'); g.px(x + 12, 2, 'crimson')
    return far, near, g


WORLDS = {'forest': forest, 'cave': cave, 'volcano': volcano, 'castle': castle}


def build():
    out = {}
    for name, fn in WORLDS.items():
        far, near, ground = fn()
        out['bg_%s_far' % name] = far
        out['bg_%s_near' % name] = near
        out['bg_%s_ground' % name] = ground
    return out
