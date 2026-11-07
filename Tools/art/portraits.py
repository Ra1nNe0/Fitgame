"""Портреты для диалогов (64x64, показываются крупно в окне катсцены).
Каждый портрет собирается из объёмных форм (shade.py): лицо, волосы, одежда, детали,
затем — цветной контур. Есть варианты эмоций: normal, happy, angry (у кого-то worried)."""
import math
from px import Canvas
from shade import (ramp, shade, ellipsoid, mask_ellipse, mask_poly, mask_rect, mask_capsule, mask_taper,
                   selout, paint, line_strokes, BAYER)

S = 64
SKIN, WH, HB, BK, ST, GO, GR, LI, CY, RE, PU, RK, LV, WD = (ramp(p) for p in
    ('sk', 'wh', 'hb', 'bk', 'st', 'go', 'gr', 'li', 'cy', 're', 'pu', 'rk', 'lv', 'wd'))


def background(c, top='bg2', bottom='bg0', glow=None):
    """Тёмный фон с мягким свечением за головой и дизерингом."""
    cols = ['bg0', 'bg1', 'bg2', 'bg3']
    for y in range(S):
        for x in range(S):
            d = math.hypot((x - 30) / 34.0, (y - 26) / 30.0)
            v = max(0.0, 1.0 - d) * 3.2
            i = int(v)
            if v - i > (BAYER[y % 4][x % 4] + 0.5) / 16.0:
                i += 1
            c.p[y][x] = cols[min(3, i)]
    if glow:
        for y in range(S):
            for x in range(S):
                d = math.hypot((x - 32) / 26.0, (y - 24) / 22.0)
                if d < 1 and (x + y) % 2 == 0 and d > 0.75:
                    c.p[y][x] = glow


def figure_layer():
    return Canvas(S, S)


def compose(bg, fig):
    selout(fig)
    for y in range(S):
        for x in range(S):
            if fig.p[y][x] is not None:
                bg.p[y][x] = fig.p[y][x]
    return bg


def eye(c, x, y, iris, expr, look=1):
    """Глаз 4x3: белок, радужка с бликом, веко сверху."""
    if expr == 'happy':
        c.line(x, y + 1, x + 1, y, 'sk1'); c.line(x + 2, y, x + 3, y + 1, 'sk1')
        return
    c.rect(x, y, 4, 3, 'wh4')
    c.rect(x + look, y, 2, 3, iris[2])
    c.px(x + look, y, iris[1]); c.px(x + look + 1, y, iris[1])
    c.px(x + look + 1, y + 2, iris[4])
    c.px(x + look, y + 1, 'bk0')
    c.px(x + look + 1, y + 1, 'wh5')        # блик
    c.rect(x - 1, y - 1, 6, 1, 'sk1')        # верхнее веко / ресницы
    if expr == 'angry':
        c.rect(x, y, 4, 1, 'sk2')


# ------------------------------------------------------------------ Король
def king(expr='normal'):
    c = Canvas(S, S); background(c)
    f = figure_layer()
    # меховой плащ на плечах
    fur = mask_ellipse(32, 66, 34, 18) | mask_ellipse(10, 60, 12, 10) | mask_ellipse(54, 60, 12, 10)
    shade(f, fur, WH, bulge=7)
    for (x, y) in fur:                       # ворс: тёмные и светлые чёрточки
        if (x * 7 + y * 13) % 11 == 0: f.px(x, y, 'wh2')
        if (x * 5 + y * 3) % 17 == 0: f.px(x, y, 'wh5')
    # кафтан и золотая перевязь
    coat = mask_poly([(18, 64), (22, 50), (42, 50), (48, 64)])
    shade(f, coat, WH, bulge=3)
    sash = mask_poly([(42, 49), (50, 52), (33, 64), (23, 64)])
    shade(f, sash, GO, bulge=2, contrast=1.2)
    for i in range(3):
        f.px(28 + i * 5, 62 - i * 4, 'go5')
    # длинные волосы за головой
    hair_back = mask_poly([(16, 16), (30, 8), (36, 20), (31, 50), (24, 58), (15, 54), (12, 32)])
    shade(f, hair_back, WH, bulge=6, bias=-0.08)
    line_strokes(f, hair_back, 'wh1', every=4, angle=0.2, keep=lambda x, y: y > 20, seed=3)
    line_strokes(f, hair_back, 'wh5', every=5, angle=0.2, phase=2, keep=lambda x, y: y > 16 and x < 22, seed=5)
    # ухо
    ellipsoid(f, 25, 30, 2.5, 4, SKIN)
    f.px(25, 30, 'sk1')
    # лицо
    face = mask_ellipse(35, 29, 11.5, 13.5)
    shade(f, face, SKIN, bulge=6)
    # нос
    nose = mask_poly([(43, 25), (48, 33), (47, 35), (43, 35)])
    shade(f, nose, SKIN, bulge=2, bias=0.1)
    f.px(45, 34, 'sk1'); f.px(46, 34, 'sk0')
    # морщины
    for (x, y) in ((33, 20), (34, 20), (35, 20), (36, 21), (32, 22), (33, 22), (35, 22)):
        f.px(x, y, 'sk2')
    f.line(44, 25, 46, 26, 'sk2')
    # повязка на глазу (ближний глаз) и ремешок
    patch = mask_ellipse(33, 27, 3.6, 3.2)
    shade(f, patch, BK, bulge=2)
    f.line(30, 25, 22, 19, 'bk1'); f.line(30, 26, 22, 20, 'bk2')
    f.line(36, 25, 40, 22, 'bk1')
    # дальний глаз
    eye(f, 40, 26, ST, expr, look=1)
    # брови: густые белые
    if expr == 'angry':
        f.line(37, 22, 44, 24, 'wh5'); f.line(37, 23, 44, 25, 'wh3')
        f.line(29, 22, 35, 24, 'wh5')
    else:
        f.line(38, 23, 44, 22, 'wh5'); f.line(38, 24, 44, 23, 'wh3')
        f.line(29, 23, 35, 22, 'wh5')
    # борода и усы
    beard = mask_poly([(25, 33), (30, 36), (44, 36), (46, 40), (42, 50), (35, 57), (29, 52), (25, 42)])
    shade(f, beard, WH, bulge=4)
    line_strokes(f, beard, 'wh2', every=3, angle=-0.15, keep=lambda x, y: y > 39, seed=7)
    line_strokes(f, beard, 'wh5', every=4, angle=-0.15, phase=1, keep=lambda x, y: y > 38 and x < 36, seed=9)
    mus = mask_taper([(31, 38), (37, 36), (44, 37), (47, 40)], 2.2, 1.0)
    shade(f, mus, WH, bulge=2, bias=0.1)
    if expr == 'happy':
        f.line(36, 41, 42, 41, 'sk1'); f.px(36, 40, 'sk1'); f.px(42, 40, 'sk1')
        f.rect(37, 41, 5, 1, 're2')
    elif expr == 'angry':
        f.rect(36, 40, 7, 3, 'sk0'); f.rect(37, 40, 5, 1, 'wh4')
    else:
        f.line(37, 41, 42, 41, 'sk1')
    # чёлка
    fr = mask_poly([(22, 14), (40, 12), (44, 17), (38, 19), (34, 16), (30, 20), (26, 24), (22, 22)])
    shade(f, fr, WH, bulge=3)
    line_strokes(f, fr, 'wh3', every=3, angle=0.6)
    # корона: чёрная, с шипами и золотым камнем
    band = mask_poly([(19, 13), (45, 11), (46, 16), (20, 18)])
    spikes = set()
    for sx, h in ((21, 9), (26, 11), (31, 13), (36, 12), (41, 10), (45, 8)):
        spikes |= mask_poly([(sx - 2, 14), (sx + 0.5, 14 - h), (sx + 2.5, 14)])
    shade(f, band | spikes, BK, bulge=2, contrast=1.3)
    ellipsoid(f, 33, 14, 2.2, 1.8, GO)
    f.px(32, 13, 'go5')
    for x in (23, 28, 38, 43):
        f.px(x, 15, 'go3')
    return compose(c, f)


# ------------------------------------------------------------------ Рыцарь Лира
def knight(expr='normal'):
    c = Canvas(S, S); background(c)
    f = figure_layer()
    # сиреневый плащ за спиной
    cape = mask_poly([(2, 64), (6, 46), (18, 44), (22, 64)])
    shade(f, cape, LI, bulge=4)
    line_strokes(f, cape, 'li2', every=4, angle=0.35, seed=4)
    # длинные волосы за спиной
    hb = mask_poly([(14, 14), (30, 6), (44, 10), (40, 30), (34, 52), (26, 62), (12, 60), (9, 34)])
    shade(f, hb, HB, bulge=6, bias=-0.06)
    line_strokes(f, hb, 'hb1', every=3, angle=0.15, keep=lambda x, y: y > 22, seed=2)
    line_strokes(f, hb, 'hb4', every=5, angle=0.15, phase=1, keep=lambda x, y: 18 < y < 40 and x < 20, seed=6)
    # плечи: белая рубашка с пышными рукавами
    sh = mask_ellipse(34, 66, 25, 14)
    shade(f, sh, WH, bulge=6)
    line_strokes(f, sh, 'wh2', every=5, angle=1.2, seed=8)
    # корсет
    cor = mask_poly([(25, 55), (43, 55), (41, 64), (27, 64)])
    shade(f, cor, BK, bulge=3, contrast=1.2)
    for y in (57, 60, 63):
        f.px(29, y, 'go4'); f.px(39, y, 'go4')
    f.line(34, 55, 34, 64, 'bk3')
    # наплечник с шипами (ближнее плечо)
    pa = mask_ellipse(16, 56, 11, 7)
    shade(f, pa, ST, bulge=4, contrast=1.25)
    for sx, sy, tx, ty in ((9, 52, 5, 44), (15, 50, 14, 41), (21, 51, 24, 43)):
        sp = mask_poly([(sx - 2, sy), (tx, ty), (sx + 2, sy)])
        shade(f, sp, BK, bulge=1, contrast=1.3)
    f.line(8, 58, 24, 58, 'st1')
    # высокий ворот
    col_ = mask_poly([(27, 42), (41, 42), (43, 54), (25, 54)])
    shade(f, col_, WH, bulge=3)
    f.line(34, 45, 34, 54, 'wh2')
    # шея
    paint(f, mask_rect(31, 38, 7, 6), 'sk2')
    # лицо с острым подбородком
    face = mask_ellipse(35, 28, 10, 12) | mask_poly([(28, 34), (43, 32), (39, 41), (35, 43), (31, 40)])
    shade(f, face, SKIN, bulge=6, bias=0.04)
    # глаза
    eye(f, 28, 28, CY, expr, look=2)
    eye(f, 38, 28, CY, expr, look=1)
    if expr != 'happy':
        f.px(27, 27, 'bk0'); f.px(42, 27, 'bk0')            # длинные ресницы
    # брови
    if expr == 'angry':
        f.line(27, 24, 32, 26, 'hb1'); f.line(37, 26, 42, 24, 'hb1')
    else:
        f.line(27, 25, 32, 24, 'hb1'); f.line(37, 24, 42, 25, 'hb1')
    # нос и губы
    f.px(40, 33, 'sk2'); f.px(41, 34, 'sk1')
    if expr == 'happy':
        f.line(35, 37, 40, 37, 're2'); f.px(34, 36, 're2'); f.px(41, 36, 're2'); f.rect(36, 38, 4, 1, 're4')
    elif expr == 'angry':
        f.rect(36, 37, 4, 2, 're1'); f.rect(36, 37, 4, 1, 'wh4')
    else:
        f.line(36, 37, 40, 37, 're3'); f.px(37, 38, 're4'); f.px(38, 38, 're4')
    f.px(31, 34, 're4'); f.px(42, 33, 're4')                 # румянец
    # чёлка и прядь у лица
    bang = mask_poly([(22, 12), (44, 12), (45, 22), (40, 18), (36, 23), (32, 18), (27, 24), (24, 30), (21, 26)])
    shade(f, bang, HB, bulge=3)
    line_strokes(f, bang, 'hb4', every=4, angle=0.9, seed=11)
    lock = mask_taper([(24, 22), (23, 32), (25, 42), (24, 50)], 2.6, 1.2)
    shade(f, lock, HB, bulge=2)
    lock2 = mask_taper([(44, 16), (46, 26), (45, 34)], 2.0, 0.8)
    shade(f, lock2, HB, bulge=2)
    return compose(c, f)


# ------------------------------------------------------------------ Пульс — робот-тренер
def pulse(expr='normal'):
    c = Canvas(S, S); background(c, glow='re1')
    f = figure_layer()
    # антенна
    f.line(32, 14, 32, 6, 'st2'); f.line(33, 14, 33, 6, 'st3')
    ellipsoid(f, 32.5, 5, 3, 3, GO)
    # тело-сердце
    heart = mask_ellipse(23, 24, 13, 12) | mask_ellipse(42, 24, 13, 12) | mask_poly([(11, 28), (54, 28), (33, 56)])
    shade(f, heart, RE, bulge=9, contrast=1.15)
    # экран-лицо
    scr = mask_poly([(18, 21), (47, 21), (47, 38), (18, 38)])
    shade(f, scr, ['bg0', 'bg1', 'bg2', 'bg3', 'st1', 'st2'], bulge=2)
    f.line(19, 21, 46, 21, 'st3')
    cy_ = 'cy4'
    if expr == 'happy':
        for x0 in (22, 37):
            f.line(x0, 30, x0 + 3, 26, cy_); f.line(x0 + 3, 26, x0 + 6, 30, cy_)
        f.line(28, 33, 37, 33, cy_); f.px(27, 32, cy_); f.px(38, 32, cy_)
    elif expr == 'worried':
        for x0 in (22, 37):
            f.rect(x0 + 1, 27, 4, 4, cy_)
        f.line(21, 24, 27, 26, cy_); f.line(38, 26, 44, 24, cy_)
        f.line(29, 34, 36, 34, cy_); f.px(28, 35, cy_); f.px(37, 35, cy_)
    else:
        for x0 in (22, 37):
            f.rect(x0 + 1, 26, 4, 5, cy_); f.px(x0 + 1, 26, 'cy5')
        f.line(29, 34, 36, 34, cy_)
    for y in range(22, 38, 2):                                  # строчки экрана
        for x in range(19, 47):
            if f.p[y][x] == 'bg0':
                f.p[y][x] = 'bg1'
    # ручки
    ar = mask_capsule(10, 34, 4, 44, 2.5)
    shade(f, ar, ST, bulge=2)
    ar2 = mask_capsule(55, 34, 60, 42, 2.5)
    shade(f, ar2, ST, bulge=2)
    ellipsoid(f, 4, 45, 3, 3, ST); ellipsoid(f, 60, 43, 3, 3, ST)
    # блик
    f.px(16, 15, 're5'); f.px(17, 14, 're5'); f.px(15, 17, 're4')
    return compose(c, f)


# ------------------------------------------------------------------ Король слизней
def slime_king(expr='normal'):
    c = Canvas(S, S); background(c, glow='gr1')
    f = figure_layer()
    body = mask_ellipse(32, 44, 30, 22)
    shade(f, body, GR, bulge=12, contrast=1.1)
    for (x, y) in ((14, 40), (12, 44), (50, 50), (20, 56)):      # пузырьки внутри
        ellipsoid(f, x, y, 1.6, 1.6, ['gr3', 'gr4', 'gr4', 'gr5', 'gr5', 'gr5'])
    f.px(11, 33, 'gr5'); f.px(12, 32, 'gr5'); f.px(13, 32, 'gr5')
    # глаза
    for ex in (22, 40):
        ellipsoid(f, ex, 38, 5, 6, WH)
        f.rect(ex + 1 if ex == 22 else ex, 37, 3, 4, 'bk0')
        f.px(ex + 1, 37, 'wh5')
        if expr == 'angry':
            f.line(ex - 5, 31 if ex == 22 else 33, ex + 5, 33 if ex == 22 else 31, 'gr0')
    # рот с зубами
    f.rect(23, 48, 18, 6, 'gr0')
    for x in range(24, 41, 3):
        f.rect(x, 48, 2, 2, 'wh4')
    f.rect(26, 52, 12, 2, 're2')
    f.px(41, 54, 'gr4'); f.px(41, 55, 'gr4'); f.px(41, 56, 'gr5')        # слюнка
    # корона
    cr = mask_poly([(16, 22), (20, 8), (25, 17), (32, 4), (39, 17), (44, 8), (48, 22)])
    shade(f, cr, GO, bulge=3, contrast=1.25)
    for x, col in ((24, 're3'), (32, 'cy3'), (40, 're3')):
        ellipsoid(f, x, 18, 1.8, 1.8, RE if col == 're3' else CY)
    return compose(c, f)


# ------------------------------------------------------------------ Королева мышей
def bat_queen(expr='normal'):
    c = Canvas(S, S); background(c, glow='pu1')
    f = figure_layer()
    # крылья-плащ
    wing = mask_poly([(0, 64), (2, 40), (12, 30), (16, 44), (24, 40), (28, 64)]) | \
        mask_poly([(64, 64), (62, 40), (52, 30), (48, 44), (40, 40), (36, 64)])
    shade(f, wing, PU, bulge=4, bias=-0.1)
    line_strokes(f, wing, 'pu1', every=6, angle=0.6, gaps=0.0, seed=1)
    # уши
    for pts in ([(14, 22), (10, 2), (24, 14)], [(50, 22), (54, 2), (40, 14)]):
        e = mask_poly(pts)
        shade(f, e, PU, bulge=3)
        (bx, by), (tx, ty), (ix, iy) = pts
        inner = mask_poly([((bx * 2 + ix) // 3, by - 1), (tx + (2 if tx < 32 else -2), ty + 5), ((bx + ix * 2) // 3, iy + 3)])
        shade(f, inner, ['re0', 're1', 're1', 're2', 're2', 're3'], bulge=2)
    # голова в меху
    head = mask_ellipse(32, 32, 17, 15)
    shade(f, head, PU, bulge=8)
    line_strokes(f, head, 'pu2', every=3, angle=1.3, seed=13, keep=lambda x, y: y > 36 or x < 20 or x > 44)
    # мордочка
    mz = mask_ellipse(32, 39, 7, 5)
    shade(f, mz, ['pu2', 'pu3', 'pu3', 'pu4', 'pu4', 'pu5'], bulge=3)
    nose = mask_poly([(30, 37), (32, 33), (34, 37), (32, 38)])
    shade(f, nose, ['pu1', 'pu2', 'pu2', 'pu3', 'pu3', 'pu4'], bulge=1)
    f.px(31, 37, 'pu0'); f.px(33, 37, 'pu0')
    f.px(26, 23, 'pu5'); f.px(27, 22, 'pu5'); f.px(38, 22, 'pu4')       # блики на лбу
    # глаза: красные, светятся
    for ex in (24, 40):
        ellipsoid(f, ex, 29, 4, 3.4, RE, ambient=0.5)
        f.rect(ex - 1, 28, 2, 2, 're5')
        if expr == 'angry':
            f.line(ex - 4, 25 if ex == 24 else 27, ex + 4, 27 if ex == 24 else 25, 'pu0')
    # клыки и рот
    f.line(28, 42, 36, 42, 'pu0')
    f.rect(29, 43, 1, 3, 'wh4'); f.rect(35, 43, 1, 3, 'wh4')
    # диадема
    dia = mask_poly([(22, 18), (32, 12), (42, 18), (40, 20), (32, 16), (24, 20)])
    shade(f, dia, ST, bulge=1, contrast=1.3)
    ellipsoid(f, 32, 16, 2, 2, RE)
    return compose(c, f)


# ------------------------------------------------------------------ Магмовый голем
def golem(expr='normal'):
    c = Canvas(S, S); background(c, glow='lv0')
    f = figure_layer()
    sh = mask_poly([(0, 64), (4, 46), (20, 40), (44, 40), (60, 46), (64, 64)])
    shade(f, sh, RK, bulge=6)
    head = mask_poly([(14, 10), (50, 8), (54, 30), (48, 46), (16, 46), (10, 28)])
    shade(f, head, RK, bulge=7, contrast=1.2)
    # трещины с лавой
    for pts in ([(20, 12), (24, 20), (21, 27), (25, 33)], [(46, 14), (42, 22), (45, 28)], [(10, 52), (16, 56), (14, 62)],
                [(52, 50), (48, 56), (54, 62)], [(30, 40), (33, 44), (31, 48)]):
        for i in range(len(pts) - 1):
            f.line(pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1], 'lv2')
            f.line(pts[i][0] + 1, pts[i][1], pts[i + 1][0] + 1, pts[i + 1][1], 'lv4')
    # брови-уступы и глаза
    f.rect(17, 20, 13, 3, 'rk1'); f.rect(35, 20, 13, 3, 'rk1')
    for ex in (20, 38):
        f.rect(ex, 24, 7, 4, 'lv3'); f.rect(ex + 1, 25, 5, 2, 'lv5')
        if expr == 'angry':
            f.line(ex - 1, 22 if ex == 20 else 24, ex + 7, 24 if ex == 20 else 22, 'rk0')
    # рот-разлом
    f.rect(22, 36, 20, 4, 'rk0'); f.rect(24, 37, 16, 2, 'lv2')
    for x in range(24, 40, 4):
        f.rect(x, 36, 2, 1, 'rk2')
    return compose(c, f)


# ------------------------------------------------------------------ Лорд Лень
def sloth(expr='normal'):
    c = Canvas(S, S); background(c, glow='pu1')
    f = figure_layer()
    robe = mask_ellipse(32, 68, 30, 18)
    shade(f, robe, PU, bulge=8)
    line_strokes(f, robe, 'pu2', every=5, angle=1.4, seed=21)
    # меховой воротник
    col_ = mask_ellipse(32, 52, 20, 6)
    shade(f, col_, WH, bulge=3)
    # лицо: серо-зелёное, вытянутое
    grey = ['bk1', 'bk2', 'st1', 'st2', 'st3', 'st4']
    face = mask_ellipse(32, 34, 13, 15)
    shade(f, face, grey, bulge=7, bias=0.05)
    # мешки под глазами и сонные глаза
    for ex in (24, 37):
        f.line(ex, 33, ex + 5, 33, 'bk2')
        f.line(ex, 35, ex + 5, 35, 'pu2')
        if expr == 'angry':
            f.rect(ex, 30, 6, 3, 'wh4'); f.rect(ex + 2, 30, 2, 3, 're2'); f.line(ex, 29, ex + 5, 30 if ex == 24 else 29, 'bk0')
        else:
            f.rect(ex, 31, 6, 2, 'bk1'); f.line(ex, 31, ex + 5, 31, 'st1')       # веки опущены
    f.px(33, 37, 'bk2'); f.px(32, 38, 'bk1')
    # рот: зевает
    if expr == 'angry':
        f.rect(27, 42, 10, 2, 'bk0')
    else:
        m = mask_ellipse(32, 43, 4, 3)
        paint(f, m, 'bk0'); f.rect(30, 44, 4, 1, 're2')
    # ночной колпак
    cap = mask_poly([(17, 26), (24, 12), (40, 8), (54, 14), (60, 24), (52, 22), (46, 18), (47, 26)])
    shade(f, cap, ['hb1', 'hb2', 'hb3', 'hb4', 'hb5', 'hb5'], bulge=4)
    band = mask_poly([(16, 24), (48, 22), (48, 28), (16, 29)])
    shade(f, band, WH, bulge=2)
    ellipsoid(f, 59, 26, 4, 4, WH)
    # Zzz
    if expr != 'angry':
        for (x, y, k) in ((6, 10, 4), (11, 4, 3)):
            f.line(x, y, x + k, y, 'cy4'); f.line(x + k, y, x, y + k, 'cy4'); f.line(x, y + k, x + k, y + k, 'cy4')
    return compose(c, f)


PORTRAITS = {
    'king': (king, ('normal', 'happy', 'angry')),
    'knight': (knight, ('normal', 'happy', 'angry')),
    'pulse': (pulse, ('normal', 'happy', 'worried')),
    'slime': (slime_king, ('normal', 'angry')),
    'bat': (bat_queen, ('normal', 'angry')),
    'golem': (golem, ('normal', 'angry')),
    'sloth': (sloth, ('normal', 'angry')),
}


def build():
    out = {}
    for name, (fn, exprs) in PORTRAITS.items():
        for e in exprs:
            out['portrait_%s_%s' % (name, e)] = fn(e)
    return out


if __name__ == '__main__':
    from px import sheet
    import sys
    sheet(list(build().values()), scale=4, cols=6).save(sys.argv[1] if len(sys.argv) > 1 else 'preview_portraits.png')
