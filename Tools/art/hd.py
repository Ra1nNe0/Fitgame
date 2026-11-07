"""Удвоение разрешения графики (32 пикселя на игровую единицу вместо 16).
Препятствия, враги, боссы и фоны увеличиваются алгоритмом EPX/Scale2x: он скругляет
диагонали и края, поэтому картинка становится детальнее, а не просто крупнее.
У спрайтов с контуром контур заново рисуется тонким (1 пиксель на новом масштабе).
Имена HD-картинок перечисляются в Resources/Art/hd_manifest.txt — по нему игра понимает,
что у картинки 32 пикселя на единицу.
"""
from px import Canvas


def epx(c):
    out = Canvas(c.w * 2, c.h * 2)
    g = c.get
    for y in range(c.h):
        for x in range(c.w):
            p = c.p[y][x]
            a, b, cc, d = g(x, y - 1), g(x + 1, y), g(x - 1, y), g(x, y + 1)
            e0 = e1 = e2 = e3 = p
            if cc == a and cc != d and a != b: e0 = a
            if a == b and a != cc and b != d: e1 = b
            if d == cc and d != b and cc != a: e2 = cc
            if b == d and b != a and d != cc: e3 = d
            out.p[2 * y][2 * x] = e0
            out.p[2 * y][2 * x + 1] = e1
            out.p[2 * y + 1][2 * x] = e2
            out.p[2 * y + 1][2 * x + 1] = e3
    return out


def strip_outline(c, ink='ink'):
    """Убрать внешний контур (тёмные пиксели, касающиеся прозрачности)."""
    out = Canvas(c.w, c.h)
    out.p = [row[:] for row in c.p]
    for y in range(c.h):
        for x in range(c.w):
            if c.p[y][x] == ink:
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    xx, yy = x + dx, y + dy
                    if not (0 <= xx < c.w and 0 <= yy < c.h) or c.p[yy][xx] is None:
                        out.p[y][x] = None
                        break
    return out


def has_outline(c, ink='ink'):
    edge = ink_edge = 0
    for y in range(c.h):
        for x in range(c.w):
            v = c.p[y][x]
            if v is None:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                xx, yy = x + dx, y + dy
                if not (0 <= xx < c.w and 0 <= yy < c.h) or c.p[yy][xx] is None:
                    edge += 1
                    ink_edge += v == ink
                    break
    return edge > 0 and ink_edge > edge * 0.7


def sprite(c):
    """Спрайт с контуром: тонкий контур на удвоенном масштабе."""
    if has_outline(c):
        big = epx(strip_outline(c))
        big.outline('ink')
        return big
    return epx(c)


def background(c):
    return epx(c)
