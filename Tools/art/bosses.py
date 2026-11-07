"""Боссы в HD (128x128, два кадра анимации), нарисованные вручную объёмным пиксель-артом."""
import math
from px import Canvas
from shade import (ramp, shade, ellipsoid, selout, paint, line_strokes,
                   mask_ellipse, mask_poly, mask_rect, mask_capsule)

GR, PU, RE, CY, GO, ST, RK, LV, WD, WH, BK, HB = (ramp(p) for p in ('gr', 'pu', 're', 'cy', 'go', 'st', 'rk', 'lv', 'wd', 'wh', 'bk', 'hb'))
S = 128


def eye(c, x, y, rx, ry, iris, look=(1, 0)):
    ellipsoid(c, x, y, rx, ry, WH, ambient=0.55)
    ix, iy = x + look[0] * rx * 0.35, y + look[1] * ry * 0.3
    ellipsoid(c, ix, iy, rx * 0.55, ry * 0.6, iris, ambient=0.4)
    c.rect(int(ix - rx * 0.2), int(iy - ry * 0.25), max(2, int(rx * 0.4)), max(2, int(ry * 0.5)), 'bk0')
    c.rect(int(ix - rx * 0.45), int(iy - ry * 0.45), 2, 2, 'wh5')


def brow(c, x0, y0, x1, y1, col, thick=3):
    for k in range(thick):
        c.line(x0, y0 + k, x1, y1 + k, col)


def lava_line(c, a, b, phase=0):
    n = int(max(abs(b[0] - a[0]), abs(b[1] - a[1]))) + 1
    for k in range(n + 1):
        x = int(a[0] + (b[0] - a[0]) * k / n)
        y = int(a[1] + (b[1] - a[1]) * k / n)
        c.px(x, y, 'lv4' if (k + phase * 2) % 6 < 2 else 'lv3')
        c.px(x + 1, y, 'lv2')
        if k % 4 == 0:
            c.px(x - 1, y, 'lv1')


# ------------------------------------------------------------------ Король слизней
def slime_king(frame):
    c = Canvas(S, S)
    squash = frame == 1
    cy, rx, ry = (80, 50, 42) if not squash else (86, 54, 36)
    body = (mask_ellipse(64, cy, rx, ry) | mask_ellipse(64, 114, rx + 4, 9)) & mask_rect(0, 0, S, 123)
    shade(c, body, GR, bulge=26, contrast=1.15)
    top = cy - ry
    # пузырьки и проглоченная косточка внутри желе
    for (x, y, r) in ((30, 98, 3), (96, 104, 2.5), (40, 112, 2), (88, 88, 2), (104, 96, 1.5)):
        ellipsoid(c, x, y, r, r, ['gr3', 'gr4', 'gr4', 'gr5', 'gr5', 'gr5'])
    paint(c, mask_capsule(70, 108, 82, 104, 1.6), 'gr4')
    # блик
    shade(c, mask_ellipse(40, top + 16, 10, 5), ['gr4', 'gr4', 'gr5', 'gr5', 'wh5', 'wh5'], bulge=3, ambient=0.6)
    c.rect(36, top + 13, 3, 2, 'wh5')
    # лицо
    ey = cy - 8
    for ex in (46, 82):
        eye(c, ex, ey, 9, 10 if not squash else 8, ['gr0', 'gr1', 'gr2', 'gr2', 'gr3', 'gr3'], look=(0.4, 0.2))
    brow(c, 34, ey - 15, 56, ey - 10, 'gr0')
    brow(c, 72, ey - 10, 94, ey - 15, 'gr0')
    # рот с зубами и языком
    mouth = mask_ellipse(64, ey + 24, 20, 9 if not squash else 7) - mask_rect(0, 0, S, ey + 20)
    paint(c, mouth, 'gr0')
    paint(c, mask_ellipse(64, ey + 30, 10, 4) & mouth, 're2')
    for x in range(46, 84, 6):
        c.rect(x, ey + 20, 4, 4, 'wh4'); c.rect(x, ey + 20, 4, 1, 'wh5')
    c.px(84, ey + 30, 'gr4'); c.rect(84, ey + 31, 2, 4, 'gr4'); c.px(85, ey + 35, 'gr5')     # слюна
    # корона
    t = top + 2
    cr = mask_poly([(40, t + 4), (42, t - 18), (51, t - 6), (64, t - 24), (77, t - 6), (86, t - 18), (88, t + 4), (64, t + 8)])
    shade(c, cr, GO, bulge=4, contrast=1.3)
    shade(c, mask_rect(40, t - 2, 49, 6), GO, bulge=2, contrast=1.2, bias=-0.1)
    for x, col in ((50, RE), (64, CY), (78, RE)):
        ellipsoid(c, x, t + 1, 3, 3, col, ambient=0.5)
    for x, y in ((42, t - 18), (64, t - 24), (86, t - 18)):
        ellipsoid(c, x, y - 1, 2.2, 2.2, GO)
    selout(c)
    return c


# ------------------------------------------------------------------ Королева мышей
def bat_queen(frame):
    c = Canvas(S, S)
    up = frame == 0
    for sgn in (-1, 1):
        sh = (64 + sgn * 18, 46)
        if up:
            tips = [(64 + sgn * 46, 22), (64 + sgn * 62, 34), (64 + sgn * 62, 62)]
            low = [(64 + sgn * 54, 66), (64 + sgn * 42, 58), (64 + sgn * 34, 72), (64 + sgn * 22, 66)]
        else:
            tips = [(64 + sgn * 44, 58), (64 + sgn * 62, 78), (64 + sgn * 56, 100)]
            low = [(64 + sgn * 48, 96), (64 + sgn * 40, 92), (64 + sgn * 32, 102), (64 + sgn * 22, 84)]
        wing = mask_poly([sh] + tips + low)
        shade(c, wing, ['pu0', 'pu1', 'pu1', 'pu2', 'pu3', 'pu4'], bulge=4, contrast=1.1)
        for p in low[::2]:                          # перепонки
            c.line(sh[0], sh[1], p[0], p[1], 'pu1')
        for p in tips:                              # косточки и когти
            shade(c, mask_capsule(sh[0], sh[1], p[0], p[1], 1.6), PU, bulge=1)
            c.px(p[0], p[1] - 1, 'wh4')
    # тело в меху
    body = mask_ellipse(64, 66, 22, 28)
    shade(c, body, PU, bulge=12)
    line_strokes(c, body, 'pu2', every=3, angle=0.3, seed=5, gaps=0.35, keep=lambda x, y: y > 60)
    chest = mask_ellipse(64, 78, 12, 14)
    shade(c, chest, ['pu2', 'pu3', 'pu3', 'pu4', 'pu4', 'pu5'], bulge=6)
    line_strokes(c, chest, 'pu3', every=3, angle=1.57, seed=9, gaps=0.3)
    # голова и уши
    shade(c, mask_ellipse(64, 42, 18, 15), PU, bulge=9)
    for sgn in (-1, 1):
        shade(c, mask_poly([(64 + sgn * 6, 32), (64 + sgn * 18, 8), (64 + sgn * 18, 36)]), PU, bulge=3)
        shade(c, mask_poly([(64 + sgn * 10, 32), (64 + sgn * 17, 14), (64 + sgn * 16, 33)]),
              ['re0', 're1', 're1', 're2', 're2', 're3'], bulge=2)
    # мордочка
    shade(c, mask_ellipse(64, 50, 9, 6), ['pu2', 'pu3', 'pu3', 'pu4', 'pu4', 'pu5'], bulge=3)
    shade(c, mask_poly([(60, 47), (64, 42), (68, 47), (64, 49)]), ['pu1', 'pu2', 'pu2', 'pu3', 'pu3', 'pu4'], bulge=1)
    c.px(62, 47, 'pu0'); c.px(66, 47, 'pu0')
    c.line(58, 53, 70, 53, 'pu0')
    c.rect(60, 54, 2, 4, 'wh4'); c.rect(67, 54, 2, 4, 'wh4'); c.px(60, 57, 'wh5'); c.px(67, 57, 'wh5')
    # горящие глаза и брови
    for ex in (55, 73):
        ellipsoid(c, ex, 39, 5, 4, RE, ambient=0.55)
        c.rect(ex - 1, 37, 2, 2, 're5')
    brow(c, 49, 32, 59, 35, 'pu0', 2)
    brow(c, 69, 35, 79, 32, 'pu0', 2)
    # серебряная диадема
    shade(c, mask_poly([(48, 30), (64, 20), (80, 30), (77, 32), (64, 25), (51, 32)]), ST, bulge=1.5, contrast=1.35)
    ellipsoid(c, 64, 24, 3, 3, RE, ambient=0.5)
    # лапки
    for x in (56, 72):
        shade(c, mask_capsule(x, 92, x + (-2 if x < 64 else 2), 100, 2.4), PU, bulge=1)
        c.px(x - 2, 101, 'wh4'); c.px(x + 2, 101, 'wh4')
    selout(c)
    return c


# ------------------------------------------------------------------ Магмовый голем
def golem(frame):
    c = Canvas(S, S)
    b = frame                                       # «дыхание»: торс чуть поднимается
    RKR = ['rk0', 'rk1', 'rk2', 'rk3', 'rk4', 'rk5']

    def block(pts, cracks=(), bulge=6):
        m = mask_poly(pts)
        shade(c, m, RKR, bulge=bulge, contrast=1.15)
        line_strokes(c, m, 'rk2', every=5, angle=0.7, seed=len(pts), gaps=0.55)
        for p, q in cracks:
            lava_line(c, p, q, b)
        return m
    # ноги
    block([(34, 96), (58, 96), (56, 122), (30, 122)], [((44, 100), (40, 116))])
    block([(70, 96), (94, 96), (98, 122), (72, 122)], [((84, 102), (88, 118))])
    # торс
    block([(26, 40 + b), (102, 40 + b), (108, 74), (96, 100), (32, 100), (20, 74)],
          [((48, 52), (58, 66)), ((58, 66), (52, 80)), ((52, 80), (60, 94)), ((82, 50), (74, 64)), ((74, 64), (80, 78))], bulge=14)
    # раскалённое ядро в груди
    shade(c, mask_ellipse(64, 66 + b, 9, 8), ['lv1', 'lv2', 'lv3', 'lv4', 'lv5', 'lv5'], bulge=4, ambient=0.5)
    for k in range(8):
        a = k * math.pi / 4 + frame * 0.4
        c.px(int(64 + math.cos(a) * 12), int(66 + b + math.sin(a) * 11), 'lv3')
    # руки-глыбы
    for sgn in (-1, 1):
        x0 = 64 + sgn * 48
        block([(x0 - 12, 44 + b), (x0 + 12, 44 + b), (x0 + 14, 92), (x0 - 12, 96)],
              [((x0, 54), (x0 + sgn * 3, 70)), ((x0 + sgn * 3, 70), (x0 - sgn * 2, 84))])
        shade(c, mask_ellipse(x0, 102, 12, 9), RKR, bulge=6)
        for k in range(3):
            c.line(x0 - 8 + k * 7, 98, x0 - 8 + k * 7, 104, 'rk1')
    # голова
    block([(46, 12 - b), (82, 12 - b), (86, 40), (42, 40)], bulge=8)
    brow(c, 46, 20 - b, 82, 20 - b, 'rk1', 2)
    for ex in (55, 73):
        shade(c, mask_rect(ex - 6, 24 - b, 12, 6), ['lv2', 'lv3', 'lv4', 'lv4', 'lv5', 'lv5'], bulge=2, ambient=0.6)
        c.rect(ex - 2, 25 - b, 4, 3, 'wh5')
    shade(c, mask_rect(52, 33 - b, 24, 4), ['lv1', 'lv2', 'lv2', 'lv3', 'lv3', 'lv4'], bulge=1)
    for x in range(54, 76, 5):
        c.rect(x, 33 - b, 2, 2, 'rk2')
    # дымок
    for k, (x, y) in enumerate(((58, 6), (66, 2), (74, 5))):
        if (k + frame) % 2 == 0:
            ellipsoid(c, x, y, 3, 2.4, ['st1', 'st2', 'st2', 'st3', 'st3', 'st4'], ambient=0.5)
    selout(c)
    return c


# ------------------------------------------------------------------ Лорд Лень на троне
def sloth(frame):
    c = Canvas(S, S)
    # трон со стёганой спинкой
    back = mask_poly([(66, 18), (118, 18), (120, 112), (64, 112)]) | mask_ellipse(92, 20, 26, 10)
    shade(c, back, ['re0', 're1', 're1', 're2', 're2', 're3'], bulge=8)
    for row, y in enumerate(range(32, 108, 14)):
        for x in range(74, 116, 14):
            xx = x + (7 if row % 2 else 0)
            if xx < 116:
                shade(c, mask_ellipse(xx, y, 2, 2), ['re0', 're0', 're1', 're1', 're2', 're2'], bulge=1)
    shade(c, mask_rect(62, 104, 60, 10) | mask_rect(62, 70, 8, 40) | mask_rect(114, 70, 8, 40), GO, bulge=3, contrast=1.2)
    for x in (64, 78, 92, 106, 118):
        shade(c, mask_rect(x - 2, 114, 5, 8), GO, bulge=2)
    ellipsoid(c, 92, 12, 4, 4, GO)
    shade(c, mask_ellipse(92, 100, 26, 7), PU, bulge=5)          # подушка
    # тело в мантии
    robe = mask_poly([(56, 56), (88, 50), (104, 70), (108, 104), (60, 106), (50, 80)])
    shade(c, robe, PU, bulge=12)
    line_strokes(c, robe, 'pu2', every=6, angle=1.2, seed=3, gaps=0.2)
    for x, y in ((56, 108), (70, 111)):                            # тапочки с помпонами
        shade(c, mask_ellipse(x, y, 8, 4.5), ['pu1', 'pu2', 'pu3', 'pu4', 'pu5', 'pu5'], bulge=3)
        ellipsoid(c, x - 6, y - 2, 2.5, 2.5, WH)
    shade(c, mask_ellipse(80, 82, 16, 14), ['pu1', 'pu2', 'pu3', 'pu3', 'pu4', 'pu5'], bulge=9)   # живот
    shade(c, mask_capsule(64, 76, 92, 72, 3), GO, bulge=2)        # пояс
    shade(c, mask_ellipse(74, 56, 22, 8), WH, bulge=5)            # меховой воротник
    for x in range(54, 96, 6):
        c.px(x, 58 + (x % 4) // 2, 'wh1')
    # рука со скипетром
    shade(c, mask_capsule(56, 64, 40, 82, 5), PU, bulge=4)
    shade(c, mask_rect(35, 30, 4, 90), GO, bulge=2, contrast=1.2)
    shade(c, mask_ellipse(38, 84, 5, 4.5), WH, bulge=3, bias=-0.1)
    ellipsoid(c, 37, 26, 7, 7, CY, ambient=0.4, contrast=1.2)
    c.rect(34, 22, 3, 2, 'wh5')
    for k in range(6):
        a = k * math.pi / 3 + frame * 0.5
        c.px(int(37 + math.cos(a) * 10), int(26 + math.sin(a) * 10), 'cy4' if k % 2 else 'cy2')
    # голова: серая, сонная
    shade(c, mask_ellipse(74, 40, 15, 14), WH, bulge=8, bias=-0.18)
    for ex in (68, 81):
        paint(c, mask_ellipse(ex, 40, 4.5, 3), 'wh1')
        c.line(ex - 4, 40 + frame, ex + 4, 40 + frame, 'wh0')
        if not frame:
            c.rect(ex - 1, 41, 2, 1, 're2')       # красный сонный глаз чуть видно
        c.line(ex - 3, 44, ex + 3, 44, 'pu2')     # мешки под глазами
    paint(c, mask_ellipse(75, 50, 3, 2.5 + frame * 1.5), 'pu0')   # зевок
    c.px(75, 49, 're2')
    # ночной колпак
    cap = mask_poly([(58, 34), (62, 22), (76, 16), (92, 20), (106, 32), (110, 42), (104, 40), (92, 30), (90, 34)])
    shade(c, cap, HB, bulge=6)
    shade(c, mask_capsule(58, 32, 90, 32, 3.2), WH, bulge=2)
    ellipsoid(c, 108, 44, 5, 5, WH)
    selout(c)
    # Zzz над головой (без контура, «плывут» по кадрам)
    for x, y, sz in ((94, 8, 7), (104, 0, 6), (113, -6, 5)):
        y += frame * 2
        if y < 0:
            continue
        for k in range(sz):
            c.px(x + k, y, 'cy4'); c.px(x + k, y + sz - 1, 'cy4')
            c.px(x + sz - 1 - k, y + k, 'cy4')
    return c


def build():
    out = {}
    for f in (0, 1):
        out['boss_slime_%d' % f] = slime_king(f)
        out['boss_bat_%d' % f] = bat_queen(f)
        out['boss_golem_%d' % f] = golem(f)
        out['boss_sloth_%d' % f] = sloth(f)
    return out


if __name__ == '__main__':
    import sys
    from px import sheet
    d = build()
    sheet([d[n] for n in sorted(d)], scale=2, cols=4).save(sys.argv[1])
