"""Фоны четырёх миров, нарисованные сразу в HD (1024x512 слои, земля 1024x128).

Слои те же, что раньше: дальний (небо и горизонт, непрозрачный), ближний (деревья, сталактиты,
колонны — с прозрачностью) и земля. Всё бесшовно по горизонтали: каждая форма рисуется
«с переносом» через край текстуры, а линии рельефа — суммы синусов с целым числом волн.
"""
import math
import random
from px import Canvas, PAL
from shade import shade, ellipsoid, mask_ellipse, mask_poly, mask_rect, mask_capsule, mask_taper, line_strokes, BAYER

W, H = 1024, 512
TOP = 152        # выше камера не заглядывает
HOR = 428        # верх земли
GH = 128         # высота текстуры земли


def C(hexs):
    """Цвет по hex — регистрируем в палитре и возвращаем имя."""
    name = '#' + hexs
    PAL[name] = hexs
    return name


def R(*hexes):
    return [C(h) for h in hexes]


class Wrap:
    """Обёртка холста: всё, что нарисовано за краем, появляется с другой стороны."""
    def __init__(self, c):
        self.c = c

    def px(self, x, y, col):
        self.c.px(x, y, col, wrap=True)


def noise(x, y, seed=0):
    n = (x * 374761393 + y * 668265263 + seed * 982451653) & 0xffffffff
    n = ((n ^ (n >> 13)) * 1274126177) & 0xffffffff
    return (n & 0xffff) / 65536.0


def sky(c, y0, y1, colors, rows=None):
    n = len(colors) - 1
    for y in range(rows[0] if rows else 0, rows[1] if rows else c.h):
        t = min(max((y - y0) / float(y1 - y0), 0.0), 1.0) * n
        i = min(int(t), n - 1)
        f = t - i
        for x in range(c.w):
            c.p[y][x] = colors[i + 1] if f > (BAYER[y % 4][x % 4] + 0.5) / 16.0 else colors[i]


def ridge(seed, base, amps):
    r = random.Random(seed)
    ph = [r.uniform(0, 6.28) for _ in amps]
    return [base + sum(a * math.sin(2 * math.pi * k * x / W + p) for (k, a), p in zip(amps, ph)) for x in range(W)]


def mountains(c, line, lit, mid, dark, snow=None, snow_line=None, streak=None, fade=None, seed=0):
    """Горный хребет: склоны к свету светлее, полосы-осыпи, снежные шапки и дымка у подножия."""
    for x in range(W):
        y0 = int(line[x])
        s = line[(x + 7) % W] - line[(x - 7) % W]
        for y in range(max(0, y0), c.h):
            d = y - y0
            # склон к свету светлее; граница света «гуляет» по диагонали, как осыпи
            k = s + (noise((x + d) // 5, d // 9, seed) - 0.5) * 6
            col = lit if k < -2 else (mid if k < 3 else dark)
            if streak and noise((x - d // 2) // 3, d // 10, seed) < 0.18 and d > 4:
                col = streak if col != dark else dark
            if snow and snow_line is not None and y < snow_line + noise(x // 4, 0, seed) * 10 and d < 26:
                col = snow if s < 0.8 else mid
            if fade and d > fade[0]:
                k = (d - fade[0]) / float(fade[1])
                if k > (BAYER[y % 4][x % 4] + 0.5) / 16.0:
                    col = fade[2]
            c.p[y][x] = col


def glow(c, cx, cy, r, col, strength=0.6, only=None, wrap=True):
    """Свечение дизерингом: чем ближе к центру, тем плотнее точки цвета col."""
    for y in range(int(cy - r), int(cy + r) + 1):
        if not 0 <= y < c.h:
            continue
        for x in range(int(cx - r), int(cx + r) + 1):
            d = math.hypot(x - cx, y - cy) / r
            if d >= 1:
                continue
            xx = x % c.w
            if only is not None and c.p[y][xx] not in only:
                continue
            if (1 - d) ** 1.6 * strength > (BAYER[y % 4][xx % 4] + 0.5) / 16.0:
                c.p[y][xx] = col


def cloud(c, x, y, w, ramp_, r):
    """Объёмное облако из нескольких шаров с плоским низом."""
    wc = Wrap(c)
    m = set()
    k = max(3, int(w / 14))
    for i in range(k):
        cx = x + i * w / (k - 1) - w / 2
        rr = r * (0.55 + 0.45 * math.sin(math.pi * (i + 0.5) / k)) * (0.8 + 0.4 * noise(i, int(x), 7))
        m |= mask_ellipse(cx, y - rr * 0.35, rr, rr * 0.75)
    m = {(px_, py_) for (px_, py_) in m if py_ <= y + 2}
    shade(wc, m, ramp_, bulge=r * 0.6, ambient=0.35, contrast=0.9)


def wrap_mask(m):
    return {(x % W, y) for x, y in m}


def leafy(c, m, colors, seed, bulge=10, bias=0.0, dots=None):
    """Крона/куст: объём + россыпь «листочков» светлее сверху и темнее снизу."""
    wc = Wrap(c)
    shade(wc, m, colors, bulge=bulge, bias=bias, contrast=1.15)
    if dots:
        hi, lo = dots
        ys = [p[1] for p in m]
        top, bot = min(ys), max(ys)
        for (x, y) in m:
            n = noise(x, y, seed)
            t = (y - top) / float(max(1, bot - top))
            if n < 0.05 and t < 0.55:
                wc.px(x, y, hi); wc.px(x + 1, y, hi)
            elif n > 0.955 and t > 0.4:
                wc.px(x, y, lo); wc.px(x, y + 1, lo)


def flame(c, x, y, size, ramp_, phase=0):
    wc = Wrap(c)
    m = mask_taper([(x, y), (x + math.sin(phase) * 1.5, y - size * 0.6), (x, y - size * 1.4)], size * 0.55, 0.6)
    shade(wc, m, ramp_, bulge=size * 0.4, ambient=0.5, light=(0, 0.3, 1))
    inner = mask_taper([(x, y), (x, y - size * 0.8)], size * 0.3, 0.6)
    shade(wc, inner, ramp_[3:] + [ramp_[-1]] * 3, bulge=2, ambient=0.8)


# ================================================================== ЛЕС
SKY_F = R('2a5fb0', '3478c8', '4590d8', '5aa8e2', '78c0ea', '9ad6f0', 'c4ecf4')
MTN_FAR = R('8fb4d8', '7a9cc4', '6886b0')
MTN_MID = R('6f97a8', '587e92', '44667c')
HILL = R('5aa84a', '3f8a40', '2c6a38', '1f4e30')
LEAF = R('10301e', '1a4a28', '276636', '3a8540', '5aa84a', '8ccf62')
LEAF_DARK = R('0c2418', '143a22', '1e522c', '2c6a36', '3f8540', '5aa84a')
BARK = R('2a160e', '43261a', '5e3a24', '7a5030', '9a6a40', 'b88a58')
SOIL = R('3a2218', '533222', '6e4630', '8a5c3c', 'a8764c')
STONE = R('2c2c3a', '46485c', '646a80', '8890a6', 'b4bccc', 'dfe4ee')


def forest():
    r = random.Random(11)
    far = Canvas(W, H)
    sky(far, TOP - 40, 380, SKY_F)
    glow(far, 860, 250, 70, C('e8f6f8'), 0.35)
    ellipsoid(Wrap(far), 860, 250, 22, 22, R('fff3b0', 'fff6c8', 'fffbe0', 'ffffff', 'ffffff', 'ffffff'), ambient=0.8)
    for i in range(9):
        cloud(far, r.randrange(W), r.randrange(TOP + 10, 300), r.randrange(70, 150),
              R('9ab8d8', 'b8d0e8', 'd4e4f2', 'eaf2fa', 'ffffff', 'ffffff'), r.randrange(14, 24))
    a = ridge(1, 300, [(2, 40), (3, 26), (7, 10), (13, 4)])
    mountains(far, a, MTN_FAR[0], MTN_FAR[1], MTN_FAR[2], snow=C('f4f8ff'), snow_line=min(a) + 24, seed=1)
    b = ridge(2, 340, [(3, 22), (5, 14), (11, 6)])
    mountains(far, b, MTN_MID[0], MTN_MID[1], MTN_MID[2], seed=2, fade=(40, 40, C('7eb0b4')))
    # дальний лес: ряды ёлок в дымке
    for row, (base, col, hi, n) in enumerate(((372, C('3c7a6a'), C('5a9a80'), 90), (392, C('2e6450'), C('4a8a64'), 70))):
        line = ridge(5 + row, base, [(4, 8), (9, 4)])
        for x in range(W):
            for y in range(int(line[x]), H):
                far.p[y][x] = col
        for _ in range(n):
            x = r.randrange(W)
            h = r.randrange(26, 46) - row * 4
            yb = int(line[x]) + 6
            for i in range(h):
                w = int((i + 1) * 0.36) + (1 if i % 7 > 4 else 0)
                for xx in range(x - w, x + w + 1):
                    far.px(xx, yb - h + i, hi if xx < x - w * 0.3 else col, wrap=True)
    hill = ridge(7, 412, [(3, 8), (6, 5)])
    for x in range(W):
        for y in range(int(hill[x]), H):
            d = y - int(hill[x])
            far.p[y][x] = HILL[0] if d < 3 else (HILL[1] if d < 12 else HILL[2])

    near = Canvas(W, H)
    wn = Wrap(near)
    xs = [60 + i * 170 + r.randrange(-30, 30) for i in range(6)]
    for x in xs:
        sc = r.uniform(0.75, 1.1)
        th = int(r.randrange(170, 230) * sc)
        top = HOR - th
        # ствол с корнями
        trunk = mask_poly([(x - 13, H), (x - 9, top + 20), (x + 9, top + 20), (x + 13, H)])
        for sgn in (-1, 1):
            trunk |= mask_taper([(x + sgn * 8, HOR - 20), (x + sgn * 22, HOR - 2), (x + sgn * 32, HOR + 6)], 6, 2)
        trunk = wrap_mask(trunk)
        shade(wn, trunk, BARK, bulge=9, contrast=1.1)
        line_strokes(wn, trunk, BARK[1], every=4, angle=0.05, seed=x, gaps=0.45)
        # ветки
        for sgn, yy in ((-1, top + 50), (1, top + 70)):
            shade(wn, wrap_mask(mask_taper([(x, yy + 20), (x + sgn * 26, yy), (x + sgn * 44, yy - 10)], 5, 2)), BARK, bulge=3)
        # крона: задние шары темнее, передние светлее
        cy = top - 10
        back = set()
        for (dx, dy, rr) in ((-52, 18, 34), (52, 20, 34), (0, -30, 40), (-30, -14, 32), (32, -12, 32)):
            back |= mask_ellipse(x + dx * sc + r.randrange(-6, 7), cy + dy * sc, rr * sc, rr * sc * 0.85)
        leafy(near, wrap_mask(back), LEAF_DARK, seed=x, bulge=16, bias=-0.05, dots=(LEAF_DARK[5], LEAF_DARK[1]))
        for (dx, dy, rr) in ((-34, 22, 28), (36, 26, 26), (0, 4, 34), (-14, -30, 26), (20, -26, 24)):
            m = mask_ellipse(x + (dx + r.randrange(-6, 7)) * sc, cy + dy * sc, rr * sc, rr * sc * 0.82)
            leafy(near, wrap_mask(m), LEAF, seed=x + dx, bulge=13, dots=(LEAF[5], LEAF[1]))
        # свисающая лиана
        if r.random() < 0.6:
            vx = x + r.choice((-40, 36))
            for y in range(cy + 20, cy + 20 + r.randrange(60, 120)):
                wn.px(vx + int(math.sin(y * 0.08) * 2), y, LEAF[2])
                if y % 9 == 0:
                    wn.px(vx + int(math.sin(y * 0.08) * 2) + 1, y, LEAF[4])
    # кусты, папоротники, грибы и цветы у тропы
    for _ in range(14):
        x = r.randrange(W)
        w, h = r.randrange(26, 48), r.randrange(16, 28)
        m = mask_ellipse(x, HOR + 4, w, h) | mask_ellipse(x - w * 0.5, HOR + 8, w * 0.6, h * 0.8) | mask_ellipse(x + w * 0.5, HOR + 8, w * 0.6, h * 0.7)
        leafy(near, wrap_mask(m), LEAF, seed=x, bulge=10, dots=(LEAF[5], LEAF[1]))
        for k in range(r.randrange(0, 4)):
            fx, fy = x + r.randrange(-w, w), HOR - h + r.randrange(4, h)
            col = r.choice((C('ff7a9a'), C('ffe070'), C('ffffff'), C('b48cff')))
            for dx, dy in ((0, 0), (1, 0), (0, 1), (1, 1)):
                wn.px(fx + dx, fy + dy, col)
    for _ in range(6):
        x = r.randrange(W)
        shade(wn, wrap_mask(mask_rect(x - 2, HOR - 10, 5, 12)), R('c8b8a0', 'd8ccb8', 'e8e0d0', 'f4f0e8', 'ffffff', 'ffffff'), bulge=2)
        ellipsoid(wn, x, HOR - 12, 8, 5, R('6e1424', 'a8213a', 'e0404e', 'ff6a6a', 'ff9a8a', 'ffd0c0'))
        for dx in (-4, 1, 4):
            wn.px(x + dx, HOR - 14, C('ffffff'))

    g = Canvas(W, GH)
    for y in range(GH):
        for x in range(W):
            n = noise(x, y, 3)
            band = 0 if y < 40 else (1 if y < 80 else 2)
            col = SOIL[3 - band]
            if n < 0.12:
                col = SOIL[2 - band] if band < 2 else SOIL[0]
            elif n > 0.96:
                col = SOIL[4 - band]
            g.p[y][x] = col
    wg = Wrap(g)
    for _ in range(70):                                     # камешки
        x, y = r.randrange(W), r.randrange(26, GH - 4)
        rr = r.uniform(2, 6)
        ellipsoid(wg, x, y, rr * 1.3, rr, STONE[:5], ambient=0.3)
    for _ in range(8):                                      # корни
        x = r.randrange(W)
        pts = [(x, 20), (x + r.randrange(-20, 20), 50), (x + r.randrange(-40, 40), 90)]
        shade(wg, wrap_mask(mask_taper(pts, 3.5, 1)), BARK, bulge=2)
    grass = ridge(9, 16, [(37, 2), (61, 1.5), (13, 2)])
    GRASS = R('1f4e30', '2c6a38', '3f8a40', '5aa84a', '7cc45a', 'a8e070')
    for x in range(W):
        t = int(grass[x])
        for y in range(0, t + 6):
            if y < t:
                k = 5 if y < 3 else (4 if y < 6 else (3 if y < t - 3 else 2))
                g.p[y][x] = GRASS[k] if not (y > 3 and noise(x, y, 5) < 0.15) else GRASS[k - 1]
            elif y < t + 3 and noise(x, 0, 6) < 0.6:               # травинки свисают на землю
                g.p[y][x] = GRASS[1]
            elif y < t + 6 and noise(x, 1, 6) < 0.18:
                g.p[y][x] = GRASS[1]
        if noise(x, 3, 7) < 0.02:
            g.p[1][x] = C('ffe070')
    return far, near, g


# ================================================================== ПЕЩЕРА
CAVE_BG = R('0a0918', '10102a', '18193c', '20244e', '2a3060')
ROCK_C = R('0c0c1a', '1a1c36', '2a2f52', '3e4672', '58649a', '8290c0')
CRYS = R('06303a', '0b5a6a', '138f9e', '2ccbd4', '7cf0f0', 'd8ffff')
CRYS_P = R('2a0e3a', '4a1a6a', '7a2fa0', 'b058d0', 'e098f0', 'fad8ff')


def crystal(c, x, base, h, w, ramp_, lean=0.0):
    wc = Wrap(c)
    tip = (x + lean * h, base - h)
    m = mask_poly([(x - w, base), (x - w * 0.8, base - h * 0.7), tip, (x + w * 0.8, base - h * 0.7), (x + w, base)])
    shade(wc, wrap_mask(m), ramp_, normals=lambda px_, py_: ((-0.7 if px_ < x + lean * (base - py_) else 0.5), -0.3, 0.7), contrast=1.2, ambient=0.3)
    for k in range(int(h * 0.6)):
        wc.px(x - w * 0.3 + lean * k, base - h * 0.15 - k, ramp_[5])


def cave():
    r = random.Random(22)
    far = Canvas(W, H)
    sky(far, TOP, HOR, CAVE_BG)
    back = ridge(3, 330, [(2, 50), (5, 22), (11, 8)])
    mountains(far, back, C('2a3060'), C('222850'), C('1a1e40'), seed=3)
    top = ridge(4, 200, [(3, 30), (7, 16), (17, 7)])
    for x in range(W):                                     # свод
        for y in range(0, int(top[x])):
            far.p[y][x] = C('141632') if y < top[x] - 5 else C('222850')
    wf = Wrap(far)
    for _ in range(40):                                    # далёкие сталактиты
        x = r.randrange(W)
        ln, hw = r.randrange(20, 70), r.randrange(5, 12)
        m = wrap_mask(mask_poly([(x - hw, top[x] - 4), (x + hw, top[x] - 4), (x + r.randrange(-3, 4), top[x] + ln)]))
        shade(wf, m, R('10122a', '161832', '1c2040', '22284c', '2a3058', '323a66'), bulge=4)
    for _ in range(24):                                    # светящиеся жилы и кристаллы вдали
        x = r.randrange(W)
        y = int(back[x]) + r.randrange(4, 30)
        ramp_ = r.choice((CRYS, CRYS, CRYS_P))
        glow(far, x, y - 6, r.randrange(18, 34), ramp_[1], 0.5, only=None)
        crystal(far, x, y, r.randrange(10, 22), r.randrange(3, 6), ramp_, lean=r.uniform(-0.3, 0.3))
    lake = ridge(8, 410, [(5, 3)])
    for x in range(W):                                     # подземное озеро с отблесками
        for y in range(int(lake[x]), H):
            far.p[y][x] = C('0c1430') if (y + x // 6) % 9 else C('1a3a60')
    for _ in range(120):
        x, y = r.randrange(W), r.randrange(TOP, 400)
        far.px(x, y, r.choice((CRYS[3], CRYS_P[3], C('3a4070'))))

    near = Canvas(W, H)
    wn = Wrap(near)
    for i in range(8):
        x = 40 + i * 128 + r.randrange(-30, 30)
        ln = r.randrange(80, 160)
        hw = r.randrange(18, 30)
        tite = mask_poly([(x - hw - 10, 0), (x + hw + 10, 0), (x + hw, TOP + 10), (x + hw * 0.4, TOP + ln * 0.7), (x + 2, TOP + ln),
                          (x - hw * 0.5, TOP + ln * 0.6), (x - hw, TOP + 10)])
        shade(wn, wrap_mask(tite), ROCK_C, bulge=14, contrast=1.2)
        line_strokes(wn, wrap_mask(tite), ROCK_C[1], every=6, angle=0.08, seed=i, gaps=0.5)
        if r.random() < 0.7:                               # капля на кончике
            wn.px(x + 2, TOP + ln + 6, CRYS[4]); wn.px(x + 2, TOP + ln + 7, CRYS[3])
        # сталагмит
        x2 = x + 64 + r.randrange(-16, 16)
        h2 = r.randrange(50, 110)
        hw2 = r.randrange(16, 26)
        mite = mask_poly([(x2 - hw2 - 6, H), (x2 - hw2, HOR), (x2 - hw2 * 0.4, HOR - h2 * 0.7), (x2, HOR - h2),
                          (x2 + hw2 * 0.5, HOR - h2 * 0.6), (x2 + hw2, HOR), (x2 + hw2 + 6, H)])
        shade(wn, wrap_mask(mite), ROCK_C, bulge=12, contrast=1.2)
        line_strokes(wn, wrap_mask(mite), ROCK_C[1], every=7, angle=0.1, seed=i + 9, gaps=0.5)
        # кристаллы, растущие из камня
        ramp_ = CRYS if i % 3 else CRYS_P
        for k in range(r.randrange(2, 5)):
            cx = x2 + r.randrange(-hw2, hw2)
            glow(near, cx, HOR - 10, 30, ramp_[1], 0.35, only=[None])
            crystal(near, cx, HOR + 6, r.randrange(18, 44), r.randrange(4, 8), ramp_, lean=r.uniform(-0.4, 0.4))
    # светящиеся грибы
    for _ in range(10):
        x = r.randrange(W)
        glow(near, x, HOR - 6, 22, CRYS[1], 0.4, only=[None])
        shade(wn, wrap_mask(mask_rect(x - 1, HOR - 8, 3, 12)), R('5a6a8a', '7a8aa8', '9aaac4', 'c0cce0', 'e0e8f4', 'ffffff'), bulge=1)
        ellipsoid(wn, x, HOR - 9, 6, 3.5, CRYS, ambient=0.5)

    g = Canvas(W, GH)
    for y in range(GH):
        for x in range(W):
            n = noise(x // 2, y // 2, 4)
            col = ROCK_C[4] if y < 10 else (ROCK_C[3] if y < 50 else ROCK_C[2])
            if n < 0.1:
                col = ROCK_C[1]
            elif n > 0.95:
                col = ROCK_C[4]
            g.p[y][x] = col
    wg = Wrap(g)
    for k in range(0, W, 64):                               # плиты пола
        x0 = k + r.randrange(0, 20)
        for y in range(6, 44):
            wg.px(x0 + (y // 9), y, ROCK_C[0])
        for x in range(k, k + 64):
            wg.px(x, 44 + (k // 64) % 2 * 2, ROCK_C[1])
    for x in range(W):
        for y in range(4):
            g.p[y][x] = ROCK_C[5] if y == 0 else ROCK_C[4]
    for _ in range(18):
        x = r.randrange(W)
        crystal(g, x, 30 + r.randrange(0, 50), r.randrange(8, 16), r.randrange(2, 4), r.choice((CRYS, CRYS_P)), lean=r.uniform(-0.5, 0.5))
    for _ in range(10):                                    # лужицы с отблеском
        x, y = r.randrange(W), r.randrange(14, 40)
        for xx in range(x - 12, x + 12):
            for yy in range(y - 2, y + 2):
                if ((xx - x) / 12.0) ** 2 + ((yy - y) / 2.5) ** 2 < 1:
                    wg.px(xx, yy, C('1a3a60'))
        wg.px(x - 4, y - 1, CRYS[4]); wg.px(x - 3, y - 1, CRYS[4])
    return far, near, g


# ================================================================== ВУЛКАН
SKY_V = R('1a0a1e', '2e0e24', '4a1428', '6e1c26', '962a20', 'c0461c', 'e06a1e')
BASALT = R('0e0a0e', '1c1418', '2c2024', '3e2e30', '544044', '6e5658')
LAVA = R('5a0c0c', 'a8200e', 'e8501a', 'ff8c2a', 'ffc84a', 'fff2a8')
SMOKE = R('140e14', '20181e', '2e242a', '3e3238', '52464a', '6a5e60')


def lava_vein(c, pts, width=2):
    wc = Wrap(c)
    m = wrap_mask(mask_taper(pts, width, width * 0.6))
    for (x, y) in m:
        wc.px(x, y, LAVA[3] if noise(x, y, 8) < 0.7 else LAVA[4])
    for (x, y) in list(m)[::7]:
        wc.px(x, y, LAVA[5])


def volcano():
    r = random.Random(33)
    far = Canvas(W, H)
    sky(far, TOP - 20, HOR - 20, SKY_V)
    # большой вулкан с дымом
    cone = [380 - max(0.0, 1 - abs(((x - 600) / 360.0))) ** 1.4 * 150 for x in range(W)]
    cone = [min(y, 380 + 10 * math.sin(x * 0.02)) for x, y in enumerate(cone)]
    crater = 600
    mountains(far, [max(c, 242) if abs(x - crater) < 30 else c for x, c in enumerate(cone)], C('5a3238'), C('42262e'), C('2e1a22'),
              seed=4)
    for k in range(3):                                     # лава стекает по склонам
        x0 = crater + (k - 1) * 16
        pts = [(x0, 246)]
        for j in range(1, 4 + k % 2):
            pts.append((pts[-1][0] + r.randrange(-10, 10) + (k - 1) * 14, 246 + j * 24))
        lava_vein(far, pts, 2.6 - j * 0.1)
    glow(far, crater, 240, 70, LAVA[1], 0.4)
    for i in range(10):                                    # клубы дыма
        y = 232 - i * 11
        for k in range(3):
            x = crater + i * 13 + r.randrange(-6 - i * 2, 7 + i * 2)
            rr = 8 + i * 2.6 + r.uniform(-3, 3)
            ellipsoid(Wrap(far), x, y + r.randrange(-5, 6), rr, rr * 0.75, SMOKE, ambient=0.25, light=(0.3, 0.8, 0.5))
    near_r = ridge(5, 390, [(3, 16), (7, 8), (15, 3)])
    mountains(far, near_r, C('2c1a20'), C('22141a'), C('180e14'), seed=5)
    for x in range(W):
        for y in range(HOR - 14, H):
            far.p[y][x] = LAVA[1] if (y + x // 5) % 7 else LAVA[2]
    for _ in range(160):
        x, y = r.randrange(W), r.randrange(TOP, 400)
        far.px(x, y, r.choice((LAVA[3], LAVA[4], C('3e2e30'))))

    near = Canvas(W, H)
    wn = Wrap(near)
    for i in range(7):
        x = 70 + i * 146 + r.randrange(-30, 30)
        h = r.randrange(120, 210)
        w = r.randrange(22, 36)
        cols = set()
        for k in range(3):                                 # базальтовые столбы
            cx = x + (k - 1) * w * 0.9
            ch = h * (0.75 + 0.25 * (1 - abs(k - 1))) + r.randrange(-10, 10)
            col = mask_poly([(cx - w * 0.5, H), (cx - w * 0.5, HOR - ch + 6), (cx - w * 0.2, HOR - ch), (cx + w * 0.3, HOR - ch + 2),
                             (cx + w * 0.5, HOR - ch + 8), (cx + w * 0.5, H)])
            shade(wn, wrap_mask(col), BASALT, bulge=8, contrast=1.25)
            cols |= col
            for yy in range(int(HOR - ch + 20), HOR, r.randrange(22, 34)):
                for xx in range(int(cx - w * 0.5), int(cx + w * 0.5)):
                    wn.px(xx, yy, BASALT[0])
        # отсвет лавы снизу справа
        for (xx, yy) in wrap_mask(cols):
            if yy > HOR - 60 and near.get(xx, yy) in (BASALT[1], BASALT[2]) and noise(xx, yy, 9) < (yy - (HOR - 60)) / 80.0:
                near.p[yy][xx] = C('5a2018')
        if i % 2 == 0:                                     # лавопад
            lx = x + r.randrange(-10, 10)
            for yy in range(int(HOR - h * 0.6), HOR + 4):
                for dx in range(-3, 4):
                    wn.px(lx + dx + int(math.sin(yy * 0.1) * 1.5), yy, LAVA[3] if abs(dx) < 2 else LAVA[2])
            glow(near, lx, HOR - 10, 40, LAVA[2], 0.35, only=[None])
    for _ in range(5):                                     # обугленные деревья
        x = r.randrange(W)
        trunk = mask_taper([(x, HOR + 4), (x + 2, HOR - 40), (x - 1, HOR - 70)], 4, 1.5)
        trunk |= mask_taper([(x + 1, HOR - 36), (x + 16, HOR - 54)], 2, 0.8) | mask_taper([(x, HOR - 50), (x - 14, HOR - 64)], 2, 0.8)
        shade(wn, wrap_mask(trunk), BASALT, bulge=2)
        for k in range(3):
            wn.px(x + 1, HOR - 10 - k * 14, LAVA[2])

    g = Canvas(W, GH)
    for y in range(GH):
        for x in range(W):
            n = noise(x // 2, y // 2, 10)
            col = BASALT[3] if y < 8 else (BASALT[2] if y < 50 else BASALT[1])
            if n < 0.12:
                col = BASALT[1]
            elif n > 0.95:
                col = BASALT[4]
            g.p[y][x] = col
    for x in range(W):
        g.p[0][x] = BASALT[5]
        g.p[1][x] = BASALT[4]
    for k in range(16):                                    # раскалённые трещины
        x = r.randrange(W)
        pts = [(x, 6)]
        for j in range(4):
            pts.append((pts[-1][0] + r.randrange(-18, 18), pts[-1][1] + r.randrange(14, 26)))
        lava_vein(g, pts, 1.6)
        glow(g, x, 30, 26, C('5a2018'), 0.5, only=[BASALT[1], BASALT[2], BASALT[3]])
    for _ in range(40):
        x, y = r.randrange(W), r.randrange(12, GH - 4)
        ellipsoid(Wrap(g), x, y, r.uniform(2, 5), r.uniform(1.5, 3.5), BASALT, ambient=0.3)
    return far, near, g


# ================================================================== ЗАМОК
SKY_C = R('0a0818', '120e28', '1c1438', '281c4a', '36265a', '4a3468')
CSTONE = R('100e1a', '1e1a2c', '2e2a42', '45405c', '625c7c', '8a84a2')
BANNER = R('1a0e26', '33183f', '55265e', '7e3a84', 'b05cae', 'e89ce0')
GOLD = R('4a2a12', '8a4f12', 'c8821a', 'eab43a', 'fde07a', 'fffbd0')
FIRE = R('a8200e', 'e8501a', 'ff8c2a', 'ffc84a', 'fff2a8', 'ffffff')


def castle():
    r = random.Random(44)
    far = Canvas(W, H)
    sky(far, TOP - 40, HOR, SKY_C)
    for _ in range(260):                                   # звёзды
        x, y = r.randrange(W), r.randrange(0, 360)
        col = r.choice((C('ffffff'), C('c8c0ff'), C('8a84c2'), C('8a84c2')))
        far.px(x, y, col)
        if r.random() < 0.08:
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                far.px(x + dx, y + dy, C('8a84c2'), wrap=True)
    glow(far, 220, 210, 100, C('3a2e6a'), 0.6)
    ellipsoid(Wrap(far), 220, 210, 34, 34, R('8a88b0', 'b0b0d0', 'd0d0e8', 'e8e8f6', 'f8f8ff', 'ffffff'), ambient=0.4)
    for (cx, cy, rr) in ((208, 200, 6), (232, 222, 4), (226, 196, 3), (210, 226, 3)):   # кратеры
        ellipsoid(Wrap(far), cx, cy, rr, rr * 0.8, R('8a88b0', '9a98c0', 'b0b0d0', 'c0c0dc', 'd0d0e8', 'd0d0e8'), light=(0.5, 0.6, 0.6))
    for i in range(5):
        cloud(far, r.randrange(W), r.randrange(240, 330), r.randrange(120, 220), R('140e28', '1c1438', '241a44', '2e2250', '382a5c', '423266'), r.randrange(14, 22))
    a = ridge(6, 360, [(2, 30), (5, 14), (11, 5)])
    mountains(far, a, C('2a2248'), C('221c3c'), C('1a1630'), seed=6)
    # силуэт замка Лорда Лени с окнами
    cx = 720
    base = 400
    for (dx, w, h, roof) in ((-120, 40, 120, 1), (-70, 60, 90, 0), (0, 70, 190, 1), (70, 60, 100, 0), (125, 44, 140, 1)):
        x0 = cx + dx - w // 2
        far.rect(x0, base - h, w, h + 40, C('16122a'), wrap=True)
        for k in range(0, w, 10):                          # зубцы
            far.rect(x0 + k, base - h - 6, 6, 6, C('16122a'), wrap=True)
        if roof:
            for i in range(40):
                hw = int(i * (w * 0.6) / 40)
                far.rect(cx + dx - hw, base - h - 46 + i, hw * 2 + 1, 1, C('1e1838'), wrap=True)
            far.rect(cx + dx, base - h - 60, 1, 14, C('1e1838'), wrap=True)
            far.rect(cx + dx + 1, base - h - 60, 8, 5, BANNER[3], wrap=True)
        for wy in range(base - h + 20, base - 10, 26):
            for wx in range(x0 + 8, x0 + w - 8, 16):
                if noise(wx, wy, 11) < 0.55:
                    far.rect(wx, wy, 4, 7, C('fde07a'), wrap=True)
                    far.px(wx, wy, C('fffbd0'), wrap=True)
                    glow(far, wx + 2, wy + 3, 9, C('3a2e4a'), 0.4, only=[C('16122a')])
    hill = ridge(7, 404, [(3, 6), (7, 3)])
    for x in range(W):
        for y in range(int(hill[x]), H):
            far.p[y][x] = C('120e22')

    near = Canvas(W, H)
    wn = Wrap(near)
    for i in range(5):
        x = 100 + i * 205 + r.randrange(-12, 12)
        # колонна
        col = mask_rect(x - 22, TOP - 40, 44, H)
        shade(wn, wrap_mask(col), CSTONE, bulge=12, contrast=1.15)
        for yy in range(TOP - 40, H, 24):                  # кладка
            off = 0 if (yy // 24) % 2 else 22
            for xx in range(x - 22, x + 22):
                wn.px(xx, yy, CSTONE[1])
            wn.px(x - 22 + off, yy + 12, CSTONE[1])
            for k in range(12):
                wn.px(x - 1 + (11 if (yy // 24) % 2 else -11), yy + k, CSTONE[1])
        cap = mask_rect(x - 30, HOR - 300, 60, 14) | mask_rect(x - 30, HOR - 16, 60, 16)
        shade(wn, wrap_mask(cap), CSTONE, bulge=4, contrast=1.2, bias=0.05)
        # арка между колоннами
        ax = x + 102
        arch = set()
        for t in range(0, 181, 2):
            a = math.radians(t)
            for rr in range(72, 84):
                arch.add((int(ax + math.cos(a) * rr), int(HOR - 300 + 40 - math.sin(a) * rr * 0.55)))
        shade(wn, wrap_mask(arch | mask_rect(ax - 84, TOP - 40, 168, HOR - 300 + 40 - (TOP - 40) - 40)), CSTONE, bulge=6, bias=-0.08)
        # знамя
        bx = ax
        ban = mask_poly([(bx - 26, HOR - 250), (bx + 26, HOR - 250), (bx + 26, HOR - 120), (bx, HOR - 100), (bx - 26, HOR - 120)])
        shade(wn, wrap_mask(ban), BANNER, bulge=6, normals=lambda px_, py_, bx=bx: (math.sin((px_ - bx) * 0.25) * 0.6, -0.2, 0.8))
        for yy in (HOR - 246, HOR - 128):
            for xx in range(bx - 26, bx + 27):
                wn.px(xx, yy, GOLD[3]); wn.px(xx, yy + 1, GOLD[2])
        shade(wn, wrap_mask(mask_rect(bx - 30, HOR - 256, 60, 6)), GOLD, bulge=2)
        # эмблема: сонный полумесяц
        moon = mask_ellipse(bx, HOR - 186, 13, 13) - mask_ellipse(bx + 6, HOR - 190, 11, 11)
        shade(wn, wrap_mask(moon), GOLD, bulge=3)
        for k, (zx, zy) in enumerate(((bx + 8, HOR - 206), (bx + 14, HOR - 214))):
            for t in range(4 - k):
                wn.px(zx + t, zy, GOLD[4]); wn.px(zx + t, zy + 3 - k, GOLD[4]); wn.px(zx + 3 - k - t, zy + t, GOLD[4])
        # факел на колонне
        tx = x
        shade(wn, wrap_mask(mask_poly([(tx - 6, HOR - 200), (tx + 6, HOR - 200), (tx + 3, HOR - 176), (tx - 3, HOR - 176)])), GOLD, bulge=2)
        glow(near, tx, HOR - 214, 46, C('5a3a3a'), 0.45, only=[CSTONE[2], CSTONE[3], CSTONE[4], CSTONE[1]])
        flame(near, tx, HOR - 200, 9, FIRE, phase=i)

    g = Canvas(W, GH)
    for y in range(GH):
        for x in range(W):
            g.p[y][x] = CSTONE[2] if y < 60 else CSTONE[1]
    wg = Wrap(g)
    for row, y0 in enumerate(range(18, GH, 22)):           # брусчатка
        off = (row % 2) * 24
        for x0 in range(-off, W, 48):
            m = mask_rect(x0 + 1, y0 + 1, 46, 20)
            shade(wg, wrap_mask(m), CSTONE[1:] + [CSTONE[5]], bulge=4, bias=-0.06 * row + (noise(x0, y0, 12) - 0.5) * 0.2)
    # красная ковровая дорожка с золотой каймой
    for x in range(W):
        for y in range(0, 18):
            if y < 2 or y > 15:
                g.p[y][x] = GOLD[3] if y in (1, 16) else GOLD[1]
            else:
                g.p[y][x] = C('a8213a') if y < 6 else (C('8a1a30') if y < 12 else C('6e1424'))
                if (x + y * 2) % 24 == 0 and 5 < y < 12:
                    g.p[y][x] = GOLD[2]
    return far, near, g


def build():
    out = {}
    for name, fn in (('forest', forest), ('cave', cave), ('volcano', volcano), ('castle', castle)):
        far, near, g = fn()
        out['bg_%s_far' % name] = far
        out['bg_%s_near' % name] = near
        out['bg_%s_ground' % name] = g
    return out


if __name__ == '__main__':
    import sys
    from PIL import Image
    which = sys.argv[2:] or ['forest', 'cave', 'volcano', 'castle']
    fns = {'forest': forest, 'cave': cave, 'volcano': volcano, 'castle': castle}
    frames = []
    for w in which:
        far, near, g = fns[w]()
        im = Image.new('RGBA', (W, 360), (0, 0, 0, 255))
        im.alpha_composite(far.image(1).crop((0, TOP, W, H)))
        im.alpha_composite(near.image(1).crop((0, TOP, W, H)))
        im.alpha_composite(g.image(1).crop((0, 0, W, 84)), (0, HOR - TOP))
        frames.append(im)
    out = Image.new('RGBA', (W, sum(f.height for f in frames)))
    y = 0
    for f in frames:
        out.paste(f, (0, y)); y += f.height
    out.save(sys.argv[1])
