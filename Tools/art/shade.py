"""Инструменты «объёмного» пиксель-арта: заливка формы с освещением.

Форма задаётся маской (множество пикселей). Для каждой точки считается «высота» по расстоянию
до края (форма как подушка) или точная нормаль эллипсоида, затем — освещённость от света
сверху-слева и цвет из рампы (от тёмного к светлому) с упорядоченным дизерингом на границах.
Так любая плоская фигура получает объём, блики и мягкие тени, как в рисованном пиксель-арте.
"""
import math
from px import Canvas, PAL

# ------------------------------------------------------------------ расширенная палитра
EXTRA = {
    # кожа
    'sk0': '5a2f2a', 'sk1': '8a4a3a', 'sk2': 'b8735a', 'sk3': 'dda07f', 'sk4': 'f2c6a2', 'sk5': 'fde3c8',
    # белые/серебряные волосы и ткань
    'wh0': '3b4157', 'wh1': '646f8c', 'wh2': '98a5c0', 'wh3': 'c8d2e4', 'wh4': 'eef2fa', 'wh5': 'ffffff',
    # тёмно-синие волосы
    'hb0': '0d1024', 'hb1': '16203f', 'hb2': '1f3466', 'hb3': '2f5296', 'hb4': '4b78c4', 'hb5': '86b4f0',
    # чёрный металл / кожа
    'bk0': '07070d', 'bk1': '12121c', 'bk2': '1f2030', 'bk3': '33364c', 'bk4': '535a78', 'bk5': '8a93b5',
    # сталь
    'st0': '1c2133', 'st1': '343c56', 'st2': '55607f', 'st3': '8390ac', 'st4': 'b8c3d8', 'st5': 'eaf0ff',
    # золото
    'go0': '4a2a12', 'go1': '8a4f12', 'go2': 'c8821a', 'go3': 'eab43a', 'go4': 'fde07a', 'go5': 'fffbd0',
    # зелёная подкладка / слизь
    'gr0': '0c2618', 'gr1': '17462a', 'gr2': '24703a', 'gr3': '3f9e45', 'gr4': '79d05c', 'gr5': 'c6f59a',
    # сирень
    'li0': '2e1d42', 'li1': '553a75', 'li2': '8564a8', 'li3': 'b294d0', 'li4': 'd8c2ea', 'li5': 'f4ecfa',
    # бирюза (глаза, кристаллы)
    'cy0': '06303a', 'cy1': '0b5a6a', 'cy2': '138f9e', 'cy3': '2ccbd4', 'cy4': '7cf0f0', 'cy5': 'd8ffff',
    # красный
    're0': '3a0a14', 're1': '6e1424', 're2': 'a8213a', 're3': 'e0404e', 're4': 'ff7a72', 're5': 'ffc2b0',
    # фиолетовый (Лорд Лень, летучие мыши)
    'pu0': '1a0e26', 'pu1': '33183f', 'pu2': '55265e', 'pu3': '7e3a84', 'pu4': 'b05cae', 'pu5': 'e89ce0',
    # камень/лава
    'rk0': '1e1418', 'rk1': '3a2628', 'rk2': '5c3c36', 'rk3': '86584a', 'rk4': 'b07f68', 'rk5': 'd8ae92',
    'lv0': '5a0c0c', 'lv1': 'a8200e', 'lv2': 'e8501a', 'lv3': 'ff8c2a', 'lv4': 'ffc84a', 'lv5': 'fff2a8',
    # дерево
    'wd0': '2a160e', 'wd1': '4a2816', 'wd2': '6e3e20', 'wd3': '98602e', 'wd4': 'c48a48', 'wd5': 'e8bc7a',
    # фон портретов
    'bg0': '0b0d1a', 'bg1': '141a30', 'bg2': '1e2846', 'bg3': '2b3a60',
}
PAL.update({k: v for k, v in EXTRA.items() if k not in PAL})


def ramp(prefix, n=6):
    return ['%s%d' % (prefix, i) for i in range(n)]


BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]
LIGHT = (-0.55, -0.65, 0.52)   # свет сверху-слева, чуть спереди


def _norm(v):
    l = math.sqrt(sum(a * a for a in v)) or 1.0
    return tuple(a / l for a in v)


L = _norm(LIGHT)


# ------------------------------------------------------------------ маски
def mask_ellipse(cx, cy, rx, ry):
    s = set()
    for y in range(int(cy - ry - 1), int(cy + ry + 2)):
        for x in range(int(cx - rx - 1), int(cx + rx + 2)):
            if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1.0:
                s.add((x, y))
    return s


def mask_poly(points):
    """Многоугольник (список вершин)."""
    xs = [p[0] for p in points]; ys = [p[1] for p in points]
    s = set()
    n = len(points)
    for y in range(int(min(ys)), int(max(ys)) + 2):
        for x in range(int(min(xs)), int(max(xs)) + 2):
            px_, py_ = x + 0.5, y + 0.5
            inside = False
            j = n - 1
            for i in range(n):
                xi, yi = points[i]; xj, yj = points[j]
                if (yi > py_) != (yj > py_) and px_ < (xj - xi) * (py_ - yi) / (yj - yi + 1e-9) + xi:
                    inside = not inside
                j = i
            if inside:
                s.add((x, y))
    return s


def mask_rect(x, y, w, h):
    return {(xx, yy) for yy in range(int(y), int(y + h)) for xx in range(int(x), int(x + w))}


def mask_capsule(x0, y0, x1, y1, r):
    """Толстый отрезок со скруглёнными концами (рука, нога, прядь)."""
    s = set()
    dx, dy = x1 - x0, y1 - y0
    ll = dx * dx + dy * dy or 1e-9
    for y in range(int(min(y0, y1) - r - 1), int(max(y0, y1) + r + 2)):
        for x in range(int(min(x0, x1) - r - 1), int(max(x0, x1) + r + 2)):
            t = max(0.0, min(1.0, ((x + 0.5 - x0) * dx + (y + 0.5 - y0) * dy) / ll))
            qx, qy = x0 + dx * t, y0 + dy * t
            if (x + 0.5 - qx) ** 2 + (y + 0.5 - qy) ** 2 <= r * r:
                s.add((x, y))
    return s


def mask_taper(points, r0, r1):
    """Ломаная, толщина меняется от r0 до r1 (пряди волос, хвосты, плащи)."""
    s = set()
    n = len(points) - 1
    for i in range(n):
        a, b = points[i], points[i + 1]
        steps = int(max(abs(b[0] - a[0]), abs(b[1] - a[1]))) + 1
        for k in range(steps + 1):
            t = (i + k / steps) / n
            x = a[0] + (b[0] - a[0]) * k / steps
            y = a[1] + (b[1] - a[1]) * k / steps
            r = r0 + (r1 - r0) * t
            s |= mask_ellipse(x, y, max(0.6, r), max(0.6, r))
    return s


# ------------------------------------------------------------------ освещение
def _distance(mask):
    """Расстояние до края маски (шахматная метрика, два прохода)."""
    INF = 10 ** 6
    d = {p: INF for p in mask}
    for p in mask:
        x, y = p
        if (x - 1, y) not in mask or (x + 1, y) not in mask or (x, y - 1) not in mask or (x, y + 1) not in mask:
            d[p] = 1
    order = sorted(mask, key=lambda p: (p[1], p[0]))
    for x, y in order:
        for dx, dy, w in ((-1, 0, 1), (0, -1, 1), (-1, -1, 1.41), (1, -1, 1.41)):
            q = (x + dx, y + dy)
            if q in d and d[q] + w < d[(x, y)]:
                d[(x, y)] = d[q] + w
    for x, y in reversed(order):
        for dx, dy, w in ((1, 0, 1), (0, 1, 1), (1, 1, 1.41), (-1, 1, 1.41)):
            q = (x + dx, y + dy)
            if q in d and d[q] + w < d[(x, y)]:
                d[(x, y)] = d[q] + w
    return d


def shade(c, mask, colors, bulge=4.0, light=None, ambient=0.18, contrast=1.0, rim=True, dither=True,
          normals=None, bias=0.0):
    """Залить маску рампой colors с объёмом «подушки».
    bulge — насколько округлая форма (радиус скругления в пикселях);
    normals — своя функция (x, y) -> нормаль (для эллипсоидов)."""
    if not mask:
        return
    Lv = _norm(light) if light else L
    if normals is None:
        d = _distance(mask)
        h = {}
        for p, v in d.items():
            t = min(v, bulge) / bulge
            h[p] = math.sqrt(max(0.0, 1 - (1 - t) ** 2)) * bulge
    n = len(colors)
    for (x, y) in mask:
        if normals is not None:
            nx, ny, nz = normals(x, y)
        else:
            hx = h.get((x + 1, y), 0) - h.get((x - 1, y), 0)
            hy = h.get((x, y + 1), 0) - h.get((x, y - 1), 0)
            nx, ny, nz = _norm((-hx * 0.5, -hy * 0.5, 1.0))
        lam = nx * Lv[0] + ny * Lv[1] + nz * Lv[2]
        v = ambient + (1 - ambient) * max(0.0, lam)
        v = 0.5 + (v - 0.5) * contrast + bias
        # отражённый свет снизу-справа по краю
        if rim and normals is None and (nx > 0.35 and ny > 0.2):
            v += 0.12
        f = max(0.0, min(0.999, v)) * (n - 1)
        i = int(f)
        frac = f - i
        if dither and frac > (BAYER[y % 4][x % 4] + 0.5) / 16.0 * 1.0 and abs(frac - 0.5) < 0.22:
            i += 1
        elif frac > 0.72:
            i += 1
        c.px(x, y, colors[max(0, min(n - 1, i))])


def ellipsoid(c, cx, cy, rx, ry, colors, **kw):
    m = mask_ellipse(cx, cy, rx, ry)

    def nrm(x, y):
        ux, uy = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
        z = math.sqrt(max(0.0, 1 - ux * ux - uy * uy))
        return _norm((ux, uy, z * 1.1))
    shade(c, m, colors, normals=nrm, **kw)
    return m


def selout(c, dark_of=None, default='bk0'):
    """Цветной контур: вместо чёрного — самый тёмный оттенок соседнего цвета."""
    src = [row[:] for row in c.p]
    for y in range(c.h):
        for x in range(c.w):
            if src[y][x] is not None:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                xx, yy = x + dx, y + dy
                if 0 <= xx < c.w and 0 <= yy < c.h and src[yy][xx] is not None:
                    col = src[yy][xx]
                    out = default
                    if dark_of and col in dark_of:
                        out = dark_of[col]
                    elif isinstance(col, str) and col[-1:].isdigit() and col[:-1] + '0' in PAL:
                        out = col[:-1] + '0'
                    c.p[y][x] = out
                    break


def paint(c, mask, col):
    for x, y in mask:
        c.px(x, y, col)


def line_strokes(c, mask, col, every=3, angle=0.0, phase=0, keep=None, wave=0.8, gaps=0.25, seed=1):
    """Тонкие штрихи внутри маски (пряди, складки, волокна): слегка волнистые и с разрывами."""
    ca, sa = math.cos(angle), math.sin(angle)
    for x, y in mask:
        v = -x * sa + y * ca                     # вдоль штриха
        u = x * ca + y * sa + math.sin(v * 0.45 + seed) * wave
        k = int(math.floor(u))
        if k % every != phase or (keep is not None and not keep(x, y)):
            continue
        hsh = ((k * 73856093) ^ (int(v // 5) * 19349663) ^ seed) & 1023
        if hsh / 1023.0 < gaps:
            continue
        c.px(x, y, col)
