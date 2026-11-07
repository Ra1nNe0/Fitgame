"""Объёмная светотень для готовых спрайтов героев.

Рисунок героя остаётся прежним, меняются только оттенки: каждая деталь (участок одного цвета)
получает свой небольшой объём, а вся фигура — общий, со светом сверху-слева. Тени уходят
в холодный фиолетовый, света — в тёплый, переходы смягчены без шума: у мелких деталей дизеринг только мешает.
Внешний чёрный контур заменяется тёмным оттенком соседнего цвета (selout), как в рисованных играх.
"""
import math
from px import PAL, rgb
from shade import _distance, BAYER, _norm

L = _norm((-0.6, -0.7, 0.45))
OUTLINE = 'ink'


def _hex(rgb_):
    return '%02x%02x%02x' % tuple(max(0, min(255, int(round(v)))) for v in rgb_[:3])


def _name(rgb_):
    h = _hex(rgb_)
    n = '#' + h
    PAL[n] = h
    return n


def tone(col, level):
    """Оттенок цвета: level < 0 — тень (темнее и холоднее), > 0 — свет (светлее и теплее)."""
    r, g, b, _ = rgb(col)
    if level < 0:
        k = 0.78 ** (-level)
        cool = (48, 36, 92)
        m = 0.16 * (-level)
        return _name(((r * k) * (1 - m) + cool[0] * m, (g * k) * (1 - m) + cool[1] * m, (b * k) * (1 - m) + cool[2] * m))
    if level > 0:
        warm = (255, 246, 214)
        m = 0.22 * level
        return _name((r + (warm[0] - r) * m, g + (warm[1] - g) * m, b + (warm[2] - b) * m))
    return col


def _components(c, mask):
    seen, comps = set(), []
    for p in mask:
        if p in seen:
            continue
        col = c.p[p[1]][p[0]]
        stack, comp = [p], set()
        seen.add(p)
        while stack:
            x, y = stack.pop()
            comp.add((x, y))
            for q in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if q in mask and q not in seen and c.p[q[1]][q[0]] == col:
                    seen.add(q)
                    stack.append(q)
        comps.append(comp)
    return comps


def apply(c, body_bulge=9.0, part_bulge=2.5):
    filled = {(x, y) for y in range(c.h) for x in range(c.w) if c.p[y][x] is not None}
    edge = {(x, y) for (x, y) in filled if c.p[y][x] == OUTLINE and any(
        q not in filled for q in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)))}
    body = filled - edge
    if not body:
        return c

    def heights(mask, bulge):
        d = _distance(mask)
        return {p: math.sqrt(max(0.0, 1 - (1 - min(v, bulge) / bulge) ** 2)) * bulge for p, v in d.items()}
    hb = heights(body, body_bulge)
    hp = {}
    for comp in _components(c, body):
        if len(comp) > 40:                    # мелочь (лицо, глаза, пуговицы) не трогаем
            hp.update(heights(comp, part_bulge))
    src = [row[:] for row in c.p]
    for (x, y) in body:
        col = src[y][x]
        if col == OUTLINE or (x, y) not in hp:
            continue                          # внутренние чёрные линии и чёрная одежда остаются графикой
        def H(q):
            return hb.get(q, 0) * 0.35 + hp.get(q, 0)
        gx = H((x + 1, y)) - H((x - 1, y))
        gy = H((x, y + 1)) - H((x, y - 1))
        nx, ny, nz = _norm((-gx * 0.6, -gy * 0.6, 1.0))
        lam = nx * L[0] + ny * L[1] + nz * L[2]           # ~0.45 для плоского участка
        v = (lam - 0.45) * 3.2
        level = -1 if v < -0.55 else (1 if v > 0.7 else 0)
        c.p[y][x] = tone(col, level)
    # цветной контур: снаружи вместо чёрного — тёмный оттенок соседа
    for (x, y) in edge:
        best = None
        for q in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if q in body and src[q[1]][q[0]] != OUTLINE:
                best = src[q[1]][q[0]]
                break
        if best is not None:
            r, g, b, _ = rgb(best)
            c.p[y][x] = _name((r * 0.28 + 10, g * 0.24 + 8, b * 0.3 + 18))
    return c
