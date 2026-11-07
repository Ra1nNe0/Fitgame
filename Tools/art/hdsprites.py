"""Враги, предметы и боссы, нарисованные вручную сразу в HD (32 пикселя на единицу).

Каждая форма заливается объёмно (shade.py): свет сверху-слева, отражённый свет снизу,
мягкий дизеринг на переходах и цветной контур вместо чёрного. Размеры холстов и положение
фигур совпадают со старыми спрайтами, поэтому хитбоксы и анимации в игре не меняются.
"""
import math
from px import Canvas
from shade import (ramp, shade, ellipsoid, selout, paint, line_strokes,
                   mask_ellipse, mask_poly, mask_rect, mask_capsule, mask_taper)

GR, PU, RE, CY, GO, ST, RK, LV, WD, WH, BK = (ramp(p) for p in ('gr', 'pu', 're', 'cy', 'go', 'st', 'rk', 'lv', 'wd', 'wh', 'bk'))


def done(c):
    selout(c)
    return c


def shine(c, pts, col='wh5'):
    for x, y in pts:
        c.px(x, y, col)


# ------------------------------------------------------------------ слизень
def slime(frame):
    c = Canvas(32, 32)
    if frame == 0:
        body = (mask_ellipse(16, 21, 12, 11) | mask_ellipse(16, 27, 14, 4)) & mask_rect(0, 0, 32, 31)
        top = 10
    else:
        body = (mask_ellipse(16, 24, 14, 8.5) | mask_ellipse(16, 28, 15.5, 3.5)) & mask_rect(0, 0, 32, 31)
        top = 16
    shade(c, body, GR, bulge=7, contrast=1.15)
    # прозрачная «желешка»: светлая полоса и внутренние пузырьки
    ellipsoid(c, 8, 27, 1.4, 1.4, ['gr3', 'gr4', 'gr4', 'gr5', 'gr5', 'gr5'])
    ellipsoid(c, 24, 28, 1, 1, ['gr4'] * 6)
    shine(c, [(9, top + 2), (10, top + 1), (11, top + 1), (8, top + 3), (12, top + 1)], 'gr5')
    shine(c, [(10, top + 2), (11, top + 2)], 'wh5')
    # глаза
    ey = top + (7 if frame == 0 else 4)
    for ex in (12, 20):
        ellipsoid(c, ex, ey, 2.6, 3.2 if frame == 0 else 2.4, WH, ambient=0.6)
        c.rect(ex, ey - 1, 2, 3 if frame == 0 else 2, 'bk0')
        c.px(ex, ey - 1, 'wh5')
    # рот
    my = ey + (5 if frame == 0 else 4)
    c.line(13, my, 19, my, 'gr0')
    c.px(14, my + 1, 're3'); c.px(15, my + 1, 're3')
    # капли у основания
    c.px(3, 30, 'gr2'); c.px(29, 30, 'gr2')
    return done(c)


# ------------------------------------------------------------------ летучая мышь
def bat(frame):
    c = Canvas(32, 32)
    up = frame == 0
    for sgn in (-1, 1):
        cx = 16
        if up:
            pts = [(cx + sgn * 4, 15), (cx + sgn * 9, 8), (cx + sgn * 15, 7), (cx + sgn * 16, 13),
                   (cx + sgn * 13, 13), (cx + sgn * 11, 17), (cx + sgn * 8, 15), (cx + sgn * 6, 19)]
        else:
            pts = [(cx + sgn * 4, 14), (cx + sgn * 10, 15), (cx + sgn * 16, 19), (cx + sgn * 15, 24),
                   (cx + sgn * 12, 21), (cx + sgn * 10, 24), (cx + sgn * 8, 20), (cx + sgn * 5, 19)]
        wing = mask_poly(pts)
        shade(c, wing, ['pu0', 'pu1', 'pu2', 'pu2', 'pu3', 'pu4'], bulge=2, contrast=1.1)
        # пальцы-косточки крыла
        a = pts[1]
        for b in (pts[3], pts[5]):
            c.line(int(a[0]), int(a[1]), int(b[0]), int(b[1]), 'pu1')
    body = mask_ellipse(16, 16, 5.5, 6.5)
    shade(c, body, PU, bulge=4)
    line_strokes(c, body, 'pu2', every=3, angle=0.2, seed=4, keep=lambda x, y: y > 17)
    for ex in (-3, 3):                                       # уши
        e = mask_poly([(16 + ex - 2, 11), (16 + ex * 1.4, 6), (16 + ex + 2 * (1 if ex > 0 else -1) * 0, 11)])
        shade(c, e | mask_poly([(16 + ex - 1.5, 11), (16 + ex * 1.3, 6.5), (16 + ex + 1.5, 11)]), PU, bulge=1)
    for ex in (14, 18):
        c.px(ex, 15, 'go4'); c.px(ex, 14, 'go3')
        c.px(ex + (0 if ex == 14 else 0), 16, 'go2')
    c.px(15, 19, 'wh4'); c.px(17, 19, 'wh4')
    c.line(15, 18, 17, 18, 'pu0')
    return done(c)


# ------------------------------------------------------------------ огненный шар
def fireball(frame):
    c = Canvas(32, 32)
    ph = frame * 1.7
    # хвост из языков пламени (летит влево — хвост справа)
    for i, (y, ln, w) in enumerate(((11, 14, 3), (16, 17, 4.5), (21, 13, 3), (14, 10, 2.5), (19, 11, 2.5))):
        sway = math.sin(ph + i) * 1.5
        tail = mask_taper([(11, y), (14 + ln * 0.6, y + sway), (14 + ln, y + sway * 0.4 - 1)], w, 0.6)
        shade(c, tail, ['lv0', 'lv1', 'lv1', 'lv2', 'lv2', 'lv3'], bulge=2)
    core = mask_ellipse(11, 16, 9, 9)
    shade(c, core, ['lv1', 'lv2', 'lv3', 'lv4', 'lv5', 'lv5'], bulge=6, ambient=0.35, contrast=1.1)
    ellipsoid(c, 9, 14, 4, 4, ['lv4', 'lv4', 'lv5', 'lv5', 'wh5', 'wh5'], ambient=0.6)
    for (x, y) in ((24, 6 + frame), (28, 25 - frame), (21, 27), (30, 12 + frame * 2)):
        c.px(x, y, 'lv4'); c.px(x + 1, y, 'lv2')
    return done(c)


# ------------------------------------------------------------------ синий шар (отбить в босса)
def orb(frame):
    c = Canvas(32, 32)
    glow = mask_ellipse(16, 16, 13 + frame, 13 + frame) - mask_ellipse(16, 16, 10, 10)
    for x, y in glow:
        if (x + y + frame) % 2 == 0:
            c.px(x, y, 'cy1')
    ellipsoid(c, 16, 16, 10, 10, ['cy0', 'cy1', 'cy2', 'cy3', 'cy4', 'cy5'], ambient=0.3, contrast=1.15)
    ellipsoid(c, 13, 13, 3.5, 3, ['cy4', 'cy5', 'cy5', 'wh5', 'wh5', 'wh5'], ambient=0.7)
    # внутренняя спираль энергии
    for t in range(18):
        a = t * 0.45 + frame * 1.2
        r = 2 + t * 0.35
        c.px(int(16 + math.cos(a) * r), int(16 + math.sin(a) * r), 'cy4')
    for (x, y) in ((3, 4), (28, 6), (27, 28), (4, 26)) if frame else ((6, 3), (30, 15), (16, 30), (1, 16)):
        c.px(x, y, 'wh5'); c.px(x + 1, y, 'cy4'); c.px(x - 1, y, 'cy4'); c.px(x, y + 1, 'cy4'); c.px(x, y - 1, 'cy4')
    return c


# ------------------------------------------------------------------ шипы
def spikes():
    c = Canvas(32, 32)
    base = mask_rect(0, 27, 32, 5)
    shade(c, base, ['rk0', 'rk1', 'rk2', 'rk2', 'rk3', 'rk4'], bulge=2)
    for x in range(0, 32, 4):
        c.px(x + 1, 29, 'rk1')
    for cx, h in ((5, 17), (16, 20), (27, 17)):
        sp = mask_poly([(cx - 5, 28), (cx, 28 - h), (cx + 5, 28)])
        n = lambda x, y, cx=cx: ((x + 0.5 - cx) / 5.0, -0.25, 0.85)
        shade(c, sp, ST, normals=lambda x, y, cx=cx: (max(-1, min(1, (x + 0.5 - cx) / 4.0)), -0.2, 0.8), contrast=1.3)
        c.line(cx - 1, 28 - h + 3, cx - 2, 26, 'st5')
        c.px(cx, 28 - h, 'wh5')
        c.px(cx, 28 - h + 1, 're3')          # кровавый кончик для узнаваемости
    return done(c)


# ------------------------------------------------------------------ ящик
def crate():
    c = Canvas(64, 64)
    box = mask_rect(4, 6, 56, 56)
    shade(c, box, WD, bulge=3, contrast=0.9, bias=-0.05)
    # доски
    for y in (6, 24, 42):
        plank = mask_rect(8, y + 2, 48, 15)
        shade(c, plank, ['wd1', 'wd2', 'wd3', 'wd3', 'wd4', 'wd5'], bulge=2)
        line_strokes(c, plank, 'wd2', every=4, angle=1.5708, seed=y, gaps=0.4)
        c.line(8, y + 17, 55, y + 17, 'wd0')
    # каркас
    for m in (mask_rect(4, 6, 56, 4), mask_rect(4, 58, 56, 4), mask_rect(4, 6, 5, 56), mask_rect(55, 6, 5, 56)):
        shade(c, m, ['wd0', 'wd1', 'wd2', 'wd3', 'wd4', 'wd4'], bulge=2)
    diag = mask_capsule(9, 56, 54, 11, 2.4)
    shade(c, diag, ['wd0', 'wd1', 'wd2', 'wd3', 'wd4', 'wd5'], bulge=2)
    for (x, y) in ((6, 8), (57, 8), (6, 59), (57, 59)):
        ellipsoid(c, x, y, 2, 2, ST)
    for (x, y) in ((11, 54), (52, 13)):
        ellipsoid(c, x, y, 1.5, 1.5, ST)
    return done(c)


# ------------------------------------------------------------------ бревно (катится)
def log(frame):
    c = Canvas(32, 32)
    disc = mask_ellipse(16, 16, 14.5, 14.5)
    shade(c, disc, WD, bulge=3, contrast=0.9)
    face = mask_ellipse(16, 16, 11, 11)
    shade(c, face, ['wd2', 'wd3', 'wd4', 'wd4', 'wd5', 'wd5'], bulge=2, contrast=0.6)
    for r in (8, 5, 2.5):                          # годовые кольца
        ring = mask_ellipse(16, 16, r + 0.7, r + 0.7) - mask_ellipse(16, 16, r - 0.3, r - 0.3)
        paint(c, ring, 'wd3')
    ang = frame * math.pi / 2
    for k in range(3):                             # трещины крутятся вместе с бревном
        a = ang + k * 2.1
        for t in range(3, 11):
            c.px(int(16 + math.cos(a) * t + 0.5), int(16 + math.sin(a) * t + 0.5), 'wd1')
    c.px(16, 16, 'wd1')
    for k in range(10):                            # кора
        a = ang + k * 0.63
        c.px(int(16 + math.cos(a) * 13.3), int(16 + math.sin(a) * 13.3), 'wd0')
    return done(c)


# ------------------------------------------------------------------ камень босса
def rock(frame):
    c = Canvas(32, 32)
    a = frame * math.pi / 2
    pts = []
    for k in range(9):
        t = a + k * 2 * math.pi / 9
        r = 13.5 + (2 if k % 3 == 0 else 0) - (1.5 if k % 4 == 1 else 0)
        pts.append((16 + math.cos(t) * r, 16 + math.sin(t) * r * 0.95))
    m = mask_poly(pts)
    shade(c, m, ['rk0', 'st1', 'st2', 'st3', 'st4', 'st5'], bulge=5, contrast=1.1)
    for k in range(3):
        t = a + k * 2.2
        x0, y0 = 16 + math.cos(t) * 3, 16 + math.sin(t) * 3
        c.line(int(x0), int(y0), int(x0 + math.cos(t + 0.5) * 7), int(y0 + math.sin(t + 0.5) * 7), 'st1')
    for k in range(4):
        t = a + 1 + k * 1.6
        c.px(int(16 + math.cos(t) * 8), int(16 + math.sin(t) * 8), 'gr2')    # мох
    return done(c)


# ------------------------------------------------------------------ монета (вращается)
def coin(frame):
    c = Canvas(32, 32)
    w = (11.5, 7.5, 3.5, 7.5)[frame]
    disc = mask_ellipse(16, 16.5, w, 11)
    shade(c, disc, GO, bulge=3 if frame != 2 else 1.5, contrast=1.15)
    if frame != 2:
        inner = mask_ellipse(16, 16.5, w - 2.5, 8.5)
        shade(c, inner, ['go1', 'go2', 'go3', 'go3', 'go4', 'go4'], bulge=2, light=(0.5, 0.6, 0.6))
        # корона на монете
        k = w / 11.5
        crown = mask_poly([(16 - 4 * k, 20), (16 - 4.5 * k, 13), (16 - 2 * k, 16), (16, 12), (16 + 2 * k, 16), (16 + 4.5 * k, 13), (16 + 4 * k, 20)])
        shade(c, crown, GO, bulge=1, contrast=1.3)
        c.px(int(16 - w * 0.5), 10, 'go5'); c.px(int(16 - w * 0.5) + 1, 9, 'wh5')
    else:
        c.line(16, 7, 16, 25, 'go4')
    return done(c)


# ------------------------------------------------------------------ облачко-пуф
def poof(frame):
    c = Canvas(32, 32)
    r = (5, 9, 12)[frame]
    for k in range(7):
        a = k * 2 * math.pi / 7 + frame * 0.4
        x, y = 16 + math.cos(a) * r, 16 + math.sin(a) * r
        rr = (3.5, 3.2, 2)[frame]
        ellipsoid(c, x, y, rr, rr, ['wh1', 'wh2', 'wh3', 'wh4', 'wh5', 'wh5'], ambient=0.45)
    if frame < 2:
        ellipsoid(c, 16, 16, 4 - frame * 2, 4 - frame * 2, ['wh2', 'wh3', 'wh4', 'wh5', 'wh5', 'wh5'])
    for k in range(5):
        a = k * 1.3 + frame
        c.px(int(16 + math.cos(a) * (r + 3)), int(16 + math.sin(a) * (r + 3)), 'go4')
    return c


# ------------------------------------------------------------------ сердце
def heart(empty=False):
    c = Canvas(32, 32)
    m = mask_ellipse(10.5, 11.5, 7.5, 6.8) | mask_ellipse(21.5, 11.5, 7.5, 6.8) | mask_poly([(3.3, 13), (16, 27.5), (28.7, 13), (16, 10)])
    if empty:
        shade(c, m, ['bk1', 'bk2', 'bk3', 'bk3', 'bk4', 'bk4'], bulge=4)
        inner = mask_ellipse(10.5, 11.5, 4.5, 4) | mask_ellipse(21.5, 11.5, 4.5, 4) | mask_poly([(6.5, 13), (16, 23), (25.5, 13), (16, 12)])
        paint(c, inner, 'bk1')
    else:
        shade(c, m, RE, bulge=6, contrast=1.15)
        shine(c, [(8, 8), (9, 7), (10, 7), (7, 9), (7, 10), (11, 7)], 'wh5')
        shine(c, [(9, 9), (8, 11)], 're5')
    return done(c)


# ------------------------------------------------------------------ лиана над ямой
def vine():
    c = Canvas(32, 256)
    path = [(15 + math.sin(y * 0.06) * 3, y) for y in range(0, 246, 6)]
    stem = mask_taper(path, 2.6, 2.0)
    shade(c, stem, ['gr0', 'gr1', 'gr2', 'gr2', 'gr3', 'gr4'], bulge=2)
    line_strokes(c, stem, 'gr1', every=3, angle=0.9, seed=2, gaps=0.3)
    for i, y in enumerate(range(10, 240, 17)):     # листья по очереди слева и справа
        x = 15 + math.sin(y * 0.06) * 3
        sgn = 1 if i % 2 else -1
        leaf = mask_taper([(x, y), (x + sgn * 5, y - 2), (x + sgn * 9, y + 1)], 2.8, 0.6)
        shade(c, leaf, GR, bulge=2)
        c.line(int(x), y, int(x + sgn * 7), y - 1, 'gr2')
    # узел-ручка внизу
    knot = mask_ellipse(15 + math.sin(246 * 0.06) * 3, 248, 5, 4)
    shade(c, knot, WD, bulge=3)
    return done(c)


# ------------------------------------------------------------------ яма
def pit():
    c = Canvas(128, 64)
    hole = mask_poly([(16, 0), (112, 0), (108, 64), (20, 64)])
    for x, y in hole:
        t = y / 64.0
        col = 'bk2' if t < 0.15 else ('bk1' if t < 0.45 else 'bk0')
        if 0.12 < t < 0.18 and (x + y) % 2:
            col = 'bk1'
        c.px(x, y, col)
    for side, x0 in ((-1, 16), (1, 111)):          # каменные стенки
        wall = mask_poly([(x0, 0), (x0 - side * 8, 0), (x0 - side * 7 + side * (-4), 64), (x0 - side * 4 + side * (-4) * 0, 64)])
        wall = mask_rect(x0 - 4 if side < 0 else x0 - 3, 0, 8, 64) & mask_poly([(0, 0), (128, 0), (128, 64), (0, 64)])
        shade(c, wall, ['rk0', 'rk1', 'rk2', 'rk3', 'rk3', 'rk4'], bulge=2)
        for y in range(4, 64, 9):
            c.line(x0 - 4 if side < 0 else x0 - 3, y, x0 + 3 if side < 0 else x0 + 4, y + 1, 'rk1')
    for (x, y) in ((40, 48), (72, 54), (90, 40)):   # блики далеко внизу
        c.px(x, y, 'bk3')
    return c


def build():
    out = {}
    for f in (0, 1):
        out['slime_%d' % f] = slime(f)
        out['bat_%d' % f] = bat(f)
        out['fireball_%d' % f] = fireball(f)
        out['orb_%d' % f] = orb(f)
    out['spikes_0'] = spikes()
    out['crate_0'] = crate()
    for f in range(4):
        out['log_%d' % f] = log(f)
        out['rock_%d' % f] = rock(f)
        out['coin_%d' % f] = coin(f)
    for f in range(3):
        out['poof_%d' % f] = poof(f)
    out['heart_0'] = heart()
    out['heart_empty_0'] = heart(True)
    out['vine_0'] = vine()
    out['pit_0'] = pit()
    import bosses
    out.update(bosses.build())
    return out


if __name__ == '__main__':
    import sys
    from px import sheet
    d = build()
    small = ['slime_0', 'slime_1', 'bat_0', 'bat_1', 'fireball_0', 'fireball_1', 'orb_0', 'orb_1', 'spikes_0',
             'log_0', 'log_1', 'rock_0', 'coin_0', 'coin_1', 'coin_2', 'poof_0', 'poof_1', 'poof_2', 'heart_0', 'heart_empty_0']
    sheet([d[n] for n in small], scale=4, cols=10).save(sys.argv[1])
