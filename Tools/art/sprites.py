"""Враги, препятствия, предметы, боссы и иконки."""
import math
from px import Canvas


def blob(c, cx, cy, rx, ry, base, shade, light, clip_y=None, wrap=False):
    """Закрашенный эллипс с тенью снизу и бликом сверху слева."""
    for y in range(int(cy - ry - 1), int(cy + ry + 2)):
        if clip_y is not None and y > clip_y:
            continue
        for x in range(int(cx - rx - 1), int(cx + rx + 2)):
            nx, ny = (x - cx) / rx, (y - cy) / ry
            d = nx * nx + ny * ny
            if d <= 1.0:
                col = base
                if ny > 0.45 or (nx > 0.55 and ny > 0.0):
                    col = shade
                if light and (nx + 0.45) ** 2 + (ny + 0.5) ** 2 < 0.07:
                    col = light
                c.px(x, y, col, wrap)


def rot90(c):
    r = Canvas(c.h, c.w)
    for y in range(c.h):
        for x in range(c.w):
            r.p[x][c.h - 1 - y] = c.p[y][x]
    return r


# ------------------------------------------------------------------ враги
def slime(frame):
    c = Canvas(16, 16)
    if frame == 0:
        blob(c, 7.5, 10, 6.2, 5.2, 'lime', 'green', 'white', clip_y=14)
        ey = 9
    else:
        blob(c, 7.5, 11, 7.2, 4.0, 'lime', 'green', 'white', clip_y=14)
        ey = 10
    c.rect(3, ey, 1, 2, 'ink'); c.rect(6, ey, 1, 2, 'ink')
    c.px(4, ey + 3, 'forest'); c.px(5, ey + 3, 'forest')
    c.outline('ink')
    return c


def bat(frame):
    c = Canvas(16, 16)
    wy = 4 if frame == 0 else 10
    for side in (-1, 1):
        for i in range(1, 7):
            x = 7.5 + side * (1.5 + i)
            y0 = 8 + (wy - 8) * i / 6.0
            c.line(x, y0, x, 8 + (1 if frame == 0 else -1), 'purple')
            c.px(x, y0, 'magenta')
        c.px(7.5 + side * 7.5, wy + (1 if frame == 0 else -1), 'magenta')
    blob(c, 7.5, 8, 3.0, 3.2, 'purple', 'purple', None)
    c.px(5, 4, 'purple'); c.px(10, 4, 'purple'); c.px(5, 5, 'purple'); c.px(10, 5, 'purple')
    c.px(6, 7, 'yellow'); c.px(8, 7, 'yellow')
    c.px(6, 10, 'white'); c.px(8, 10, 'white')
    c.outline('ink')
    return c


def spikes():
    c = Canvas(16, 16)
    for k in range(3):
        bx = 1 + k * 5
        for i in range(8):
            w = 1 + i // 2
            y = 6 + i
            x0 = bx + 2 - (w - 1) // 2 - (w - 1) % 2 * 0
            for j in range(w):
                x = bx + 2 - w // 2 + j
                col = 'white' if j == 0 else ('slate' if j == w - 1 and w > 1 else 'silver')
                c.px(x, y, col)
    c.rect(0, 14, 16, 1, 'slate'); c.rect(0, 15, 16, 1, 'steel')
    c.outline('ink')
    return c


def log_frames():
    c = Canvas(16, 16)
    blob(c, 7.5, 7.5, 6.6, 6.6, 'wood', 'bark', None)
    blob(c, 7.5, 7.5, 4.4, 4.4, 'peach', 'peach', None)
    blob(c, 7.5, 7.5, 2.4, 2.4, 'wood', 'wood', None)
    c.px(7, 7, 'bark'); c.px(8, 8, 'bark')
    c.line(8, 2, 8, 4, 'bark'); c.px(12, 9, 'bark'); c.px(3, 10, 'dbark')
    c.outline('ink')
    out = [c]
    for _ in range(3):
        out.append(rot90(out[-1]))
    return out


def rock_frames():
    c = Canvas(16, 16)
    blob(c, 7.5, 7.5, 6.6, 6.2, 'gray', 'slate', 'silver')
    c.line(4, 9, 7, 11, 'steel'); c.line(9, 4, 11, 6, 'steel'); c.px(10, 10, 'steel'); c.px(5, 5, 'silver')
    c.outline('ink')
    out = [c]
    for _ in range(3):
        out.append(rot90(out[-1]))
    return out


def fireball(frame):
    c = Canvas(16, 16)
    tail = [(9, 8, 3), (12, 7 + frame, 2), (14, 9 - frame, 1)]
    for x, y, r in tail:
        blob(c, x, y, r, r, 'red', 'crimson', None)
    c.px(13 + frame, 5 + frame * 5, 'orange'); c.px(15, 8, 'red')
    blob(c, 5.5, 8, 4.6, 4.6, 'orange', 'red', None)
    blob(c, 5, 8, 3.0, 3.0, 'amber', 'orange', None)
    blob(c, 4.5, 8, 1.6, 1.6, 'yellow', 'yellow', None)
    c.px(4, 7, 'white')
    c.outline('dbark')
    return c


def orb(frame):
    c = Canvas(16, 16)
    blob(c, 7.5, 7.5, 5.6, 5.6, 'blue', 'navy', None)
    blob(c, 7.5, 7.5, 3.8, 3.8, 'cyan', 'blue', None)
    blob(c, 7, 7, 1.6, 1.6, 'white', 'white', None)
    sp = [(1, 2), (14, 3), (13, 13), (2, 12)] if frame == 0 else [(3, 0), (15, 7), (11, 15), (0, 8)]
    for x, y in sp:
        c.px(x, y, 'cyan')
    c.outline('ink')
    for x, y in sp:
        c.px(x, y, 'white')
    return c


def crate():
    c = Canvas(32, 32)
    c.rect(2, 3, 28, 28, 'wood')
    for x in (9, 16, 23):                                   # доски
        c.rect(x, 6, 1, 22, 'bark')
    for i in range(22):                                     # диагональная распорка
        c.rect(5 + i, 6 + i, 3, 1, 'bark'); c.px(5 + i, 6 + i, 'peach')
    c.rect(2, 3, 28, 3, 'bark'); c.rect(2, 28, 28, 3, 'bark'); c.rect(2, 3, 3, 28, 'bark'); c.rect(27, 3, 3, 28, 'bark')
    c.rect(3, 4, 26, 1, 'peach'); c.rect(3, 4, 1, 26, 'peach')
    c.rect(2, 30, 28, 1, 'dbark'); c.rect(29, 3, 1, 28, 'dbark')
    for x, y in [(3, 4), (27, 4), (3, 28), (27, 28)]:
        c.rect(x, y, 2, 2, 'silver'); c.px(x + 1, y + 1, 'gray')
    c.outline('ink')
    return c


def coin(frame):
    c = Canvas(16, 16)
    rx = [4.6, 3.2, 1.0, 3.2][frame]
    blob(c, 7.5, 8, rx, 4.6, 'yellow', 'amber', None)
    if rx > 3:
        blob(c, 7.5, 8, rx - 1.8, 2.8, 'amber', 'amber', None)
        c.px(7 - int(rx) + 2, 5, 'white'); c.px(7 - int(rx) + 1, 6, 'white')
    elif rx > 1:
        c.px(6, 5, 'white')
    else:
        c.rect(7, 4, 2, 9, 'yellow'); c.rect(8, 4, 1, 9, 'amber')
    c.outline('dbark')
    return c


HEART = [
    "................",
    "................",
    "..aaa....aaa....",
    ".awwaa..aaaaa...",
    ".awaaaaaaaaaa...",
    ".aaaaaaaaaaab...",
    ".aaaaaaaaaaab...",
    "..aaaaaaaaab....",
    "...aaaaaaab.....",
    "....aaaaab......",
    ".....aaab.......",
    "......ab........",
    "................",
]


def heart(empty=False):
    c = Canvas(16, 16)
    leg = {'a': 'steel', 'b': 'night', 'w': 'slate'} if empty else {'a': 'red', 'b': 'crimson', 'w': 'white'}
    c.stamp(HEART, 1, 1, leg)
    c.outline('ink')
    return c


def vine():
    c = Canvas(16, 128)
    for y in range(0, 118):
        x = 7.5 + math.sin(y / 9.0) * 1.6
        c.px(x, y, 'green'); c.px(x + 1, y, 'forest')
        if y % 13 == 4:
            s = 1 if (y // 13) % 2 == 0 else -1
            c.px(x + 2 * s, y, 'lime'); c.px(x + 3 * s, y, 'lime'); c.px(x + 3 * s, y + 1, 'green')
            c.px(x + 4 * s, y - 1, 'lime'); c.px(x + 2 * s, y - 1, 'lime')
    # кольцо-петля, за которую хватается герой
    for a in range(0, 360, 12):
        x = 7.5 + math.cos(math.radians(a)) * 4.2
        y = 121 + math.sin(math.radians(a)) * 4.2
        c.px(x, y, 'lime' if a > 180 else 'green')
    c.outline('ink')
    return c


def pit():
    c = Canvas(64, 32)
    for y in range(0, 32):
        inset = max(0, 3 - y // 2)
        for x in range(8 + inset, 56 - inset):
            col = 'ink'
            if x < 11 + inset or x > 52 - inset:
                col = 'dbark'
            if y < 2:
                col = 'dbark'
            c.px(x, y, col)
    for x, y in [(16, 9), (30, 14), (44, 7), (24, 22), (40, 25), (50, 18), (13, 27)]:
        c.px(x, y, 'night'); c.px(x + 1, y, 'night')
    return c


def poof(frame):
    c = Canvas(16, 16)
    r = [2.5, 5.0, 7.0][frame]
    n = 8
    for i in range(n):
        a = math.radians(i * 360 / n + 20)
        x, y = 7.5 + math.cos(a) * r, 7.5 + math.sin(a) * r
        size = [2, 2, 1][frame]
        c.rect(round(x) - size // 2, round(y) - size // 2, size, size, 'white' if i % 2 == 0 else 'yellow')
    if frame == 0:
        blob(c, 7.5, 7.5, 2.5, 2.5, 'white', 'white', None)
    return c


# ------------------------------------------------------------------ иконки-подсказки
CUES = {
    'jump': ('lime', 'forest', [
        "....ww....",
        "...wwww...",
        "..wwwwww..",
        ".wwwwwwww.",
        "....ww....",
        "....ww....",
        "....ww....",
        "....ww....",
    ]),
    'squat': ('blue', 'navy', [
        "....ww....",
        "....ww....",
        "....ww....",
        "....ww....",
        ".wwwwwwww.",
        "..wwwwww..",
        "...wwww...",
        "....ww....",
    ]),
    'punch': ('red', 'crimson', [
        "..........",
        "..wwwwww..",
        ".wwkwkwkw.",
        ".wwwwwwww.",
        ".wwwwwwww.",
        "ww.wwwwww.",
        "ww..wwww..",
        "..........",
    ]),
    'hands': ('amber', 'rust', [
        "..w....w..",
        ".www..www.",
        "wwwwwwwwww",
        "..w....w..",
        "..w....w..",
        "..w....w..",
        "..w....w..",
        "..........",
    ]),
}


def cue(name):
    base, dark, rows = CUES[name]
    c = Canvas(16, 16)
    c.rect(2, 1, 12, 14, base); c.rect(1, 2, 14, 12, base)
    c.rect(2, 14, 12, 1, dark); c.rect(14, 3, 1, 10, dark); c.rect(2, 13, 12, 1, dark)
    c.stamp(rows, 3, 3, {'w': 'white', 'k': dark})
    c.outline('ink')
    return c


# ------------------------------------------------------------------ боссы (64x64)
def king_slime(frame):
    c = Canvas(64, 64)
    if frame == 0:
        cy, rx, ry = 42, 25, 19
    else:
        cy, rx, ry = 45, 27, 16
    blob(c, 31.5, cy, rx, ry, 'lime', 'green', None, clip_y=60)
    # нижняя тёмная кромка и блик
    for x in range(64):
        for y in range(56, 61):
            if c.get(x, y) in ('green', 'lime'):
                c.px(x, y, 'forest' if y >= 58 else 'green')
    blob(c, 17, cy - ry * 0.55, 5, 2.5, 'white', 'white', None)
    c.px(25, int(cy - ry * 0.7), 'white')
    top = int(cy - ry)
    # корона
    c.rect(22, top - 6, 20, 5, 'yellow'); c.rect(22, top - 2, 20, 2, 'amber')
    for i, x in enumerate((22, 26, 31, 36, 40)):
        h = 4 if i % 2 == 0 else 2
        c.rect(x, top - 6 - h, 2, h, 'yellow')
    c.rect(31, top - 4, 2, 2, 'red'); c.rect(25, top - 4, 2, 2, 'cyan'); c.rect(37, top - 4, 2, 2, 'cyan')
    # глаза (смотрит влево, на игрока) и пасть
    ey = cy - 6
    for ex in (15, 30):
        c.rect(ex, ey, 8, 7, 'white')
        c.rect(ex, ey + 2, 4, 5, 'ink')
        c.px(ex + 1, ey + 3, 'white')
        c.line(ex - 1, ey - 3, ex + 8, ey, 'forest', 2) if ex == 30 else c.line(ex - 1, ey, ex + 8, ey - 3, 'forest', 2)
    my = cy + 6
    c.rect(14, my, 26, 6, 'dteal')
    for x in range(15, 39, 5):
        c.rect(x, my, 3, 2, 'white')
    c.rect(18, my + 4, 3, 2, 'white'); c.rect(30, my + 4, 3, 2, 'white')
    c.outline('ink')
    return c


def bat_queen(frame):
    c = Canvas(64, 64)
    up = frame == 0
    cy = 34
    for i in range(0, 24):
        k = i / 23.0
        if up:
            top = cy - 4 - 14 * math.sin(k * math.pi * 0.5) + 6 * k * k
            bot = cy + 8 - 4 * k - abs(math.sin(k * math.pi * 3)) * 4
        else:
            top = cy - 4 + 16 * k * k
            bot = top + 9 - 6 * k + abs(math.sin(k * math.pi * 3)) * 3
        top, bot = int(round(top)), int(round(max(bot, top + 2)))
        for x in (24 - i, 39 + i):
            c.rect(x, top, 1, bot - top + 1, 'purple')
            c.rect(x, top, 1, 2, 'magenta')
            if i in (7, 15):
                c.rect(x, top + 2, 1, bot - top - 1, 'ink')
    blob(c, 31.5, cy + 2, 10, 13, 'purple', 'ink', None)
    blob(c, 31.5, cy - 8, 9, 8, 'purple', 'purple', None)
    for side in (-1, 1):                                   # уши
        for i in range(8):
            c.rect(31.5 + side * (4 + i * 0.5) - 1, cy - 22 + i, 3, 1, 'purple')
        c.rect(31.5 + side * 6 - 0.5, cy - 18, 1, 4, 'magenta')
    c.rect(26, cy - 21, 12, 2, 'amber'); c.rect(31, cy - 24, 2, 3, 'amber'); c.px(31, cy - 22, 'red')   # тиара
    for ex in (24, 33):                                    # глаза
        c.rect(ex, cy - 11, 6, 3, 'yellow'); c.rect(ex, cy - 11, 2, 3, 'ink')
        c.rect(ex, cy - 13, 6, 1, 'ink')
    c.rect(25, cy - 4, 12, 3, 'ink')                        # пасть с клыками
    c.rect(26, cy - 4, 2, 3, 'white'); c.rect(34, cy - 4, 2, 3, 'white')
    c.rect(28, cy + 2, 8, 9, 'magenta'); c.rect(29, cy + 3, 6, 7, 'pink')
    c.rect(27, cy + 15, 3, 3, 'ink'); c.rect(34, cy + 15, 3, 3, 'ink')
    c.outline('ink')
    return c


def golem(frame):
    c = Canvas(64, 64)
    d = frame
    def block(x, y, w, h):
        c.rect(x, y, w, h, 'bark'); c.rect(x, y, w, 2, 'wood'); c.rect(x, y, 2, h, 'wood')
        c.rect(x, y + h - 3, w, 3, 'dbark'); c.rect(x + w - 3, y, 3, h, 'dbark')
    block(16, 22, 34, 26)                      # туловище
    block(22, 6 + d, 22, 16)                   # голова
    block(4, 24 + d * 2, 12, 22)               # левая рука (к игроку)
    block(50, 26 - d * 2, 12, 20)              # правая рука
    block(18, 48, 12, 13); block(36, 48, 12, 13)   # ноги
    lava = 'yellow' if frame else 'amber'
    for pts in ([(20, 28), (26, 34), (24, 40), (30, 45)], [(44, 26), (38, 32), (42, 38), (36, 44)],
                [(8, 30 + d * 2), (11, 36 + d * 2), (7, 42 + d * 2)], [(54, 30), (57, 38)]):
        for a, b in zip(pts, pts[1:]):
            c.line(a[0], a[1], b[0], b[1], 'orange', 2)
            c.line(a[0], a[1], b[0], b[1], lava, 1)
    c.rect(25, 11 + d, 6, 4, 'yellow'); c.rect(35, 11 + d, 6, 4, 'yellow')      # глаза
    c.rect(25, 11 + d, 3, 4, 'white'); c.rect(35, 11 + d, 3, 4, 'white')
    c.rect(24, 9 + d, 8, 2, 'dbark'); c.rect(34, 9 + d, 8, 2, 'dbark')
    c.rect(27, 18 + d, 12, 2, 'orange'); c.rect(29, 18 + d, 2, 2, 'yellow'); c.rect(35, 18 + d, 2, 2, 'yellow')
    c.outline('ink')
    return c


def lord_sloth(frame):
    """Лорд Лень: дремлет в парящем кресле, в ночном колпаке."""
    c = Canvas(64, 64)
    b = frame                                    # покачивание
    # кресло
    c.rect(30, 14 + b, 26, 38, 'crimson'); c.rect(32, 16 + b, 22, 34, 'red')
    for y in range(20 + b, 46 + b, 8):
        for x in range(36, 52, 8):
            c.px(x, y, 'amber')
    c.rect(10, 44 + b, 46, 10, 'crimson'); c.rect(10, 44 + b, 46, 3, 'red')
    c.rect(8, 36 + b, 8, 12, 'red'); c.rect(8, 36 + b, 8, 2, 'pink')         # подлокотник
    c.rect(10, 54 + b, 46, 2, 'amber')
    for x in (12, 22, 32, 42, 52):
        c.rect(x, 56 + b, 2, 3 - b, 'amber')
    # мантия
    for y in range(26 + b, 46 + b):
        k = (y - 26 - b) / 20.0
        w = 14 + int(10 * k)
        c.rect(30 - w // 2 - 2, y, w, 1, 'purple')
        c.px(30 - w // 2 - 2, y, 'magenta'); c.px(30 - w // 2 + w - 3, y, 'ink')
    c.rect(18, 44 + b, 24, 2, 'magenta')
    c.rect(24, 28 + b, 3, 14, 'amber')            # застёжка
    # голова
    blob(c, 27, 20 + b, 8, 7.5, 'silver', 'gray', None)
    c.rect(20, 20 + b, 5, 1, 'ink'); c.rect(27, 20 + b, 5, 1, 'ink')       # полузакрытые глаза
    c.rect(21, 21 + b, 2, 1, 'cyan'); c.rect(28, 21 + b, 2, 1, 'cyan')
    c.rect(19, 17 + b, 6, 1, 'slate'); c.rect(27, 17 + b, 6, 1, 'slate')   # брови
    c.rect(23, 25 + b, 6, 1, 'ink'); c.px(22, 26 + b, 'ink'); c.px(29, 26 + b, 'ink')   # недовольный рот
    # ночной колпак
    for i in range(10):
        c.rect(20 + i, 12 + b - i // 2, 16 - i, 2, 'blue')
        if i % 3 == 0:
            c.rect(20 + i, 12 + b - i // 2, 16 - i, 1, 'navy')
    c.rect(19, 13 + b, 18, 3, 'white')
    c.line(34, 8 + b, 42, 6 + b + b, 'blue', 2); c.line(42, 6 + b + b, 44, 12 + b + b, 'blue', 2)
    blob(c, 44.5, 14 + b + b, 2.6, 2.6, 'white', 'silver', None)
    # рука со скипетром (в сторону игрока)
    c.rect(12, 32 + b, 8, 4, 'purple'); c.rect(9, 32 + b, 4, 4, 'silver')
    c.line(10, 18 + b, 10, 50 + b, 'amber', 2)
    blob(c, 10.5, 15 + b, 3.6, 3.6, 'cyan', 'blue', 'white')
    # буквы Z
    for (zx, zy, s) in ((48, 4 - b, 5), (56, 0 + b, 4)):
        c.rect(zx, zy, s, 1, 'white'); c.rect(zx, zy + s - 1, s, 1, 'white')
        c.line(zx + s - 1, zy, zx, zy + s - 1, 'white')
    c.outline('ink')
    return c


def build():
    S = {}
    for i in range(2):
        S['slime_%d' % i] = slime(i)
        S['bat_%d' % i] = bat(i)
        S['fireball_%d' % i] = fireball(i)
        S['orb_%d' % i] = orb(i)
        S['boss_slime_%d' % i] = king_slime(i)
        S['boss_bat_%d' % i] = bat_queen(i)
        S['boss_golem_%d' % i] = golem(i)
        S['boss_sloth_%d' % i] = lord_sloth(i)
    S['spikes_0'] = spikes()
    S['crate_0'] = crate()
    for i, f in enumerate(log_frames()):
        S['log_%d' % i] = f
    for i, f in enumerate(rock_frames()):
        S['rock_%d' % i] = f
    for i in range(4):
        S['coin_%d' % i] = coin(i)
    for i in range(3):
        S['poof_%d' % i] = poof(i)
    S['heart_0'] = heart(False)
    S['heart_empty_0'] = heart(True)
    S['vine_0'] = vine()
    S['pit_0'] = pit()
    for n in CUES:
        S['cue_%s_0' % n] = cue(n)
    return S


if __name__ == '__main__':
    from px import sheet
    S = build()
    small = [v for k, v in S.items() if v.w <= 32 and v.h <= 32]
    sheet(small, scale=8, cols=10).save('preview_small.png')
    big = [v for k, v in S.items() if v.w == 64 and v.h == 64]
    sheet(big, scale=4, cols=4).save('preview_boss.png')
    print(len(S), 'sprites')
