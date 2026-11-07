"""Герой «Фит»: собирается из частей тела по координатам суставов."""
import math
from px import Canvas

HEAD = [
    "...hhhhhhh..",
    "..hhhhhLLhh.",
    ".HhhhhhhhhhH",
    ".Hhhhhhhhhhh",
    ".rrrrrrrrrrr",
    ".RRrrrrrrrrr",
    ".Hdssssesses",
    ".Hdssssesses",
    "..dsssssspps",
    "..ddsssssss.",
    "...ddddddd..",
]
LEG = {'L': 'peach', 'h': 'rust', 'H': 'brick', 'r': 'red', 'R': 'crimson', 's': 'skin', 'd': 'dskin',
       'e': 'ink', 'p': 'pink'}


def polar(x, y, ang, length):
    a = math.radians(ang)
    return x + math.sin(a) * length, y + math.cos(a) * length


def limb(c, x, y, a1, l1, a2, l2, col1, col2, t):
    """Конечность из двух сегментов. Угол 0 — вниз, +90 — вперёд (вправо)."""
    kx, ky = polar(x, y, a1, l1)
    ex, ey = polar(kx, ky, a2, l2)
    c.line(x, y, kx, ky, col1, t)
    c.line(kx, ky, ex, ey, col2, t)
    return ex, ey


def draw_hero(pose):
    """pose: dict с ключами hip_y, lean, head, legs F/B, arms F/B и т.д."""
    c = Canvas(64, 64)
    hip_y = pose.get('hip_y', 20) + 30
    cx = 31 + pose.get('dx', 0)
    torso_h = 7
    lean = pose.get('lean', 0)          # сдвиг плеч вперёд в пикселях
    sh_y = hip_y - torso_h              # линия плеч
    tail = pose.get('tail', 0)

    # --- задняя нога и задняя рука (темнее) ---
    fa1, fa2 = pose['leg_b']
    fx, fy = limb(c, cx - 1, hip_y, fa1, 5, fa2, 5, 'night', 'dskin', 3)
    shoe(c, fx, fy, back=True)
    al1, al2 = pose.get('al', (4, 4))
    aa1, aa2 = pose['arm_b']
    if not pose.get('arms_over'):
        limb(c, cx - 2 + lean, sh_y + 1, aa1, al1, aa2, al2, 'dskin', 'dskin', 2)

    # --- торс ---
    for i in range(torso_h):
        k = i / (torso_h - 1)
        ox = round(lean * (1 - k))
        c.rect(cx - 3 + ox, sh_y + i, 7, 1, 'blue')
        c.px(cx - 3 + ox, sh_y + i, 'navy')
        c.px(cx - 2 + ox, sh_y + i, 'navy')
    c.rect(cx - 3, hip_y - 1, 7, 1, 'amber')           # пояс
    c.rect(cx - 3, hip_y, 7, 2, 'steel')               # шорты
    c.rect(cx - 3, hip_y, 2, 2, 'night')

    # --- передняя нога ---
    fa1, fa2 = pose['leg_f']
    fx, fy = limb(c, cx + 2, hip_y + 1, fa1, 5, fa2, 5, 'steel', 'skin', 3)
    shoe(c, fx, fy, back=False)

    # --- голова ---
    hx = cx - 6 + lean + pose.get('head_dx', 0)
    hy = sh_y - 11 + pose.get('head_dy', 0)
    c.stamp(HEAD, hx, hy, LEG)
    # хвостики повязки развеваются сзади
    c.px(hx, hy + 4, 'red')
    c.px(hx - 1, hy + 4 + tail, 'red')
    c.px(hx - 2, hy + 5 + tail, 'crimson')
    c.px(hx - 1, hy + 6, 'crimson')
    c.px(hx - 2, hy + 7 - tail, 'red')
    if pose.get('hurt'):
        c.rect(hx + 7, hy + 6, 1, 2, 'skin'); c.rect(hx + 10, hy + 6, 1, 2, 'skin')
        c.line(hx + 6, hy + 6, hx + 8, hy + 7, 'ink'); c.line(hx + 9, hy + 6, hx + 11, hy + 7, 'ink')
    if pose.get('smile'):
        c.rect(hx + 8, hy + 9, 3, 1, 'pink')

    # --- передняя рука ---
    if pose.get('arms_over'):
        bx, by = limb(c, cx - 3 + lean, sh_y + 1, aa1, al1, aa2, al2, 'dskin', 'dskin', 2)
        if pose.get('open'):
            c.rect(round(bx) - 1, round(by) - 1, 3, 2, 'dskin')
    aa1, aa2 = pose['arm_f']
    ex, ey = limb(c, cx + 3 + lean, sh_y + 1, aa1, al1, aa2, al2, 'skin', 'skin', 2)
    if pose.get('open'):
        c.rect(round(ex) - 1, round(ey) - 1, 3, 2, 'skin')
    if pose.get('fist'):
        c.rect(round(ex), round(ey) - 1, 3, 3, 'skin')
        c.px(round(ex) + 2, round(ey) + 1, 'dskin')
    if pose.get('fist_b'):
        pass

    c.outline('ink')
    return c


def shoe(c, x, y, back):
    x, y = round(x), round(y)
    c.rect(x - 1, y, 5, 2, 'silver' if back else 'white')
    c.rect(x - 1, y + 1, 5, 1, 'gray' if back else 'silver')
    c.px(x + 1, y, 'red')


def poses():
    P = {}
    run = [
        dict(leg_f=(55, 15), leg_b=(-35, -80), arm_f=(-45, 20), arm_b=(55, 120), hip_y=20, lean=1, tail=0),
        dict(leg_f=(5, -10), leg_b=(45, -85), arm_f=(-10, 40), arm_b=(15, 80), hip_y=19, lean=1, tail=1),
        dict(leg_f=(-35, -80), leg_b=(55, 15), arm_f=(55, 120), arm_b=(-45, 20), hip_y=20, lean=1, tail=0),
        dict(leg_f=(45, -85), leg_b=(5, -10), arm_f=(15, 80), arm_b=(-10, 40), hip_y=19, lean=1, tail=-1),
    ]
    for i, p in enumerate(run):
        P['hero_run_%d' % i] = p
    P['hero_idle_0'] = dict(leg_f=(8, 0), leg_b=(-8, 0), arm_f=(8, 10), arm_b=(-8, 0), hip_y=20)
    P['hero_idle_1'] = dict(leg_f=(14, -8), leg_b=(-14, 8), arm_f=(10, 14), arm_b=(-10, 0), hip_y=21, tail=1)
    P['hero_jump_0'] = dict(leg_f=(75, -35), leg_b=(25, -80), arm_f=(115, 145), arm_b=(95, 130), hip_y=19,
                            lean=1, tail=1, al=(5, 4))
    P['hero_squat_0'] = dict(leg_f=(88, -5), leg_b=(70, -10), arm_f=(80, 90), arm_b=(75, 85), hip_y=24, lean=2)
    P['hero_punch_0'] = dict(leg_f=(40, 5), leg_b=(-40, -20), arm_f=(90, 90), arm_b=(-60, 60), hip_y=21,
                             lean=2, fist=True, tail=1, al=(5, 5))
    P['hero_hands_0'] = dict(leg_f=(12, 0), leg_b=(-12, -10), arm_f=(165, 170), arm_b=(195, 190), hip_y=19,
                             smile=True, arms_over=True, open=True, al=(7, 6))
    P['hero_hurt_0'] = dict(leg_f=(50, 20), leg_b=(20, 30), arm_f=(-120, -150), arm_b=(-100, -140), hip_y=20,
                            lean=-2, hurt=True, tail=-1, al=(5, 4))
    P['hero_win_0'] = dict(leg_f=(10, 0), leg_b=(-10, 0), arm_f=(165, 170), arm_b=(-25, 45), hip_y=20,
                           fist=True, smile=True, al=(7, 6))
    P['hero_win_1'] = dict(leg_f=(25, -30), leg_b=(-25, -30), arm_f=(165, 170), arm_b=(-25, 45), hip_y=18,
                           fist=True, smile=True, tail=1, al=(7, 6))
    return P


def build():
    return {name: draw_hero(p) for name, p in poses().items()}


if __name__ == '__main__':
    from px import sheet
    cs = build()
    sheet(list(cs.values()), scale=5, cols=5).save('preview_hero.png')
    print(list(cs.keys()))
