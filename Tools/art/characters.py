"""Персонажи по рисункам игрока: Король и Рыцарь — в удвоенном разрешении.
Холст 128x128, 32 пикселя = 1 игровая единица (герой в игре того же размера, что и раньше,
но пикселей в нём вчетверо больше). Позы берутся из hero.poses() и масштабируются в 2 раза.
Имена кадров: hero_king_run_0, hero_knight_idle_1 и т.д. Все кадры перечисляются в HD-списке (hd.py).
"""
import math
from px import Canvas, PAL

PAL.setdefault('lilac', 'c9a3d9')
PAL.setdefault('dlilac', '9a72b8')
PAL.setdefault('llilac', 'e6cdf0')
PAL.setdefault('gold', 'e8b13a')

S = 2          # масштаб относительно старого героя
SIZE = 128

# ------------------------------------------------------------------ головы (смотрят вправо)
KING_HEAD = [
    "......k..k..k..k........",
    "......kk.kk.kk.kk.......",
    ".....kkkkkkkkkkkkk......",
    ".....knkkkkykkkknk......",
    ".....kkkkkkkkkkkkk......",
    "....SWWWWWWWWWWWWWS.....",
    "...SWWWWWWWWWWWWWWWS....",
    "..SWWWWWWWWWWWWWWWWWS...",
    "..SWWWWWWWWWWWWWWWWWWS..",
    ".SWWWWWWWWWWWWWWWSSssss.",
    ".SWWWWWWWWWWWSsssssssss.",
    ".SWWWWWWWWWWSddssssssss.",
    ".SWWWWWWWWWWSdPPPPPPPPs.",
    ".SWWWWWWWWWWSdsWesPPPPs.",
    ".SWWWWWWWWWWSdsWesPPPPss",
    ".SWWWWWWWWWWSdssssPPPsss",
    ".SWWWWWWWWWSSddssssssss.",
    ".SWWWWWWWWWSddsBBBBBBBs.",
    ".SWWWWWWWWWSdBBBBrrrBBB.",
    ".SWWWWWWWWWSdBBBBBBBBBB.",
    ".GSWWWWWWWWS.BBBbBBbBBB.",
    ".GSWWWWWWWS..bBBBBbBBB..",
    "..GSWWWWWWS...bBBBbBBB..",
    "..GSWWWWWS.....bBBBBb...",
    "...GSWWWS.......bBBb....",
    "....GSSS.........bb.....",
]
KING_LEG = {'W': 'white', 'S': 'silver', 'G': 'gray', 's': 'skin', 'd': 'dskin', 'e': 'ink',
            'P': 'ink', 'k': 'ink', 'n': 'steel', 'y': 'yellow', 'B': 'white', 'b': 'silver', 'r': 'dskin'}
KING_FACE = (15, 13)


def _trim_back(rows, start, cut):
    out = []
    for j, row in enumerate(rows):
        if j >= start:
            row = '.' * cut + ('S' if row[cut] in 'WSG' else row[cut]) + row[cut + 1:]
        out.append(row)
    return out


KING_HEAD = _trim_back(KING_HEAD, 9, 3)      # где глаз (для обиженного/весёлого лица)

KNIGHT_HEAD = [
    ".......NNNNNNNN.........",
    ".....NNHHHHHHHHNN.......",
    "....NHHHHLLLHHHHHN......",
    "...NHHHHHHHLLLHHHHN.....",
    "..NHHHHHHHHHHHHHHHHN....",
    "..NHHHHHHHHHHHHHHHHHN...",
    ".NHHHHHHHHHHHHHHHHHHHN..",
    ".NHHHHHHHHHHHHHHHHHHHHN.",
    ".NHHHHHHHHHHHHHHHHHHHHHN",
    ".NHHHHHHHHHHHHHHsHHHHHs.",
    ".NHHHHHHHHHHHssssHHHsss.",
    ".NHHHHHHHHHHdssssssssss.",
    ".NHHHHHHHHHdssEEsssEEss.",
    ".NHHHHHHHHHdssWcsssWcss.",
    ".NHHHHHHHHHdssWcsssWcsss",
    ".NHHHHHHHHHddsssssssssss",
    ".NHHHHHHHHHHdsssssssssd.",
    ".NHHHHHHHHHHddssssspps..",
    ".NHHHHHHHHHHHddsssssss..",
    ".NHHHHHHHHHHH.ddddddd...",
    ".NHHHHHHHHHHH...........",
    "..NHHHHHHHHHN...........",
    "..NHHHHHHHHN............",
    "...NHHHHHHN.............",
    "...NHHHHHN..............",
    "....NHHHN...............",
    ".....NHN................",
]
KNIGHT_LEG = {'N': 'night', 'H': 'navy', 'L': 'blue', 's': 'skin', 'd': 'dskin', 'E': 'ink',
              'W': 'white', 'c': 'cyan', 'p': 'pink'}
KNIGHT_FACE = (14, 13)


def stamp_flow(c, rows, ox, oy, legend, flow, from_row, strands=None):
    strands = strands or {}
    """Штамп головы; ряды ниже from_row (длинные волосы) отлетают назад на бегу."""
    for j, row in enumerate(rows):
        shift = -round(max(0, j - from_row) * 0.45 * flow)
        for i, ch in enumerate(row):
            col = legend.get(ch)
            if j > 8 and i < 12 and i % 4 == 1 and ch in strands:
                col = strands[ch]
            if col:
                c.px(ox + i + (shift if i < 13 else 0), oy + j, col)


# ------------------------------------------------------------------ части тела
def polar(x, y, ang, length):
    a = math.radians(ang)
    return x + math.sin(a) * length, y + math.cos(a) * length


def seg(c, x0, y0, x1, y1, base, shade, t):
    """Отрезок конечности с тенью на задней стороне."""
    c.line(x0, y0, x1, y1, shade, t)
    c.line(x0 + 1, y0, x1 + 1, y1, base, t - 1)


def limb(c, x, y, a1, l1, a2, l2, up, low, t1, t2):
    kx, ky = polar(x, y, a1, l1)
    ex, ey = polar(kx, ky, a2, l2)
    seg(c, x, y, kx, ky, up[0], up[1], t1)
    seg(c, kx, ky, ex, ey, low[0], low[1], t2)
    return kx, ky, ex, ey


def boot(c, x, y, cols, heel=False):
    x, y = round(x), round(y)
    main, sole, light = cols
    c.rect(x - 2, y - 2, 6, 3, main)
    c.rect(x - 2, y + 1, 9, 2, main)
    c.rect(x - 2, y + 3, 9, 1, sole)
    c.px(x + 4, y + 1, light); c.px(x + 5, y + 1, light)
    if heel:
        c.rect(x - 2, y + 4, 2, 2, sole)


def hand(c, x, y, col, shade, size=3):
    x, y = round(x), round(y)
    c.rect(x - 1, y - 1, size, size, col)
    c.px(x - 1, y + size - 2, shade)


def dither_px(c, x, y, a, b):
    c.px(x, y, a if (x + y) % 2 == 0 else b)


# ------------------------------------------------------------------ плащи
def cape(c, cx, sh_y, lean, flow, n, w0, cols, fur=False):
    main, shade, light = cols
    for i in range(n):
        k = i / (n - 1)
        back = round(i * (0.12 + 0.5 * flow))
        w = w0 + round(k * 8)
        right = cx - 1 + round(lean * (1 - k)) - back
        left = right - w
        for x in range(left, right + 1):
            if fur:
                col = main
                if (x * 3 + i * 5) % 7 == 0: col = light
                if (x + i * 2) % 9 == 0: col = shade
            else:
                col = main
                fold = (x - left + i // 4) % 6
                if fold == 0: col = shade
                elif fold == 3: col = light
            c.px(x, sh_y + i, col)
        c.px(left, sh_y + i, shade)
    # нижний край
    if not fur:
        for x in range(cx - w0 - 10, cx):
            pass


# ------------------------------------------------------------------ персонажи
STYLES = {
    'king': dict(
        thigh_b=('night', 'ink'), shin_b=('ink', 'ink'), thigh_f=('dteal', 'night'), shin_f=('night', 'ink'),
        boot=('ink', 'ink', 'steel'), boot_b=('ink', 'ink', 'night'),
        sleeve_b=('silver', 'gray'), glove_b=('night', 'ink'), sleeve_f=('white', 'silver'), glove_f=('night', 'ink'),
        head=KING_HEAD, legend=KING_LEG, strands={'W': 'silver'}, face=KING_FACE, hair_row=19, head_dy=-4,
    ),
    'knight': dict(
        thigh_b=('ink', 'ink'), shin_b=('ink', 'ink'), thigh_f=('night', 'ink'), shin_f=('night', 'ink'),
        boot=('ink', 'ink', 'slate'), boot_b=('ink', 'ink', 'steel'),
        sleeve_b=('silver', 'gray'), glove_b=('steel', 'night'), sleeve_f=('white', 'silver'), glove_f=('slate', 'steel'),
        head=KNIGHT_HEAD, legend=KNIGHT_LEG, strands={'H': 'night'}, face=KNIGHT_FACE, hair_row=18, head_dy=0,
    ),
}


def torso_king(c, cx, sh_y, hip_y, lean, th, flow):
    # длинный белый кафтан до колен
    for i in range(th + 11):
        k = min(1.0, i / (th - 1))
        ox = round(lean * (1 - k))
        flare = max(0, i - th) // 2
        x0 = cx - 6 + ox - flare - (round(flow * (i - th)) if i > th else 0)
        x1 = cx + 7 + ox
        for x in range(x0, x1):
            col = 'white'
            if x <= x0 + 1: col = 'silver'
            if x == x1 - 1: col = 'silver'
            c.px(x, sh_y + i, col)
        if i >= 3:
            c.px(cx + 2 + ox, sh_y + i, 'forest' if i < th + 1 else 'green')    # зелёная подкладка
            c.px(cx + 3 + ox, sh_y + i, 'forest' if i < th + 1 else 'green')
        if i in (3, 7, 11, 15):
            c.px(cx + 5 + ox, sh_y + i, 'amber')                                # пуговицы
    # золотая перевязь наискосок
    for i in range(14):
        x = cx + 5 - i + lean
        y = sh_y + 1 + i
        c.px(x, y, 'yellow'); c.px(x + 1, y, 'amber'); c.px(x - 1, y, 'gold')
    c.rect(cx - 6, hip_y - 1, 13, 2, 'amber')
    c.rect(cx - 6, hip_y - 1, 13, 1, 'yellow')
    c.rect(cx + 1, hip_y - 2, 3, 4, 'gold')
    # меховой воротник
    for x in range(cx - 8 + lean, cx + 8 + lean):
        for y in range(sh_y - 3, sh_y + 2):
            if abs(x - (cx + lean)) + abs(y - sh_y) * 2 < 11:
                c.px(x, y, 'white' if (x * 5 + y * 3) % 4 else 'silver')


def sword(c, cx, hip_y):
    c.line(cx - 4, hip_y + 1, cx - 15, hip_y + 11, 'silver', 2)
    c.line(cx - 4, hip_y + 2, cx - 15, hip_y + 12, 'gray', 1)
    c.rect(cx - 5, hip_y - 1, 4, 2, 'gold')
    c.rect(cx - 2, hip_y - 2, 2, 2, 'amber')


def scepter(c, x, y):
    x, y = round(x), round(y)
    c.line(x, y + 4, x, y - 12, 'gold', 2)
    c.px(x, y - 12, 'yellow')
    # жар-птица
    c.rect(x - 2, y - 17, 5, 4, 'amber')
    c.rect(x - 1, y - 16, 3, 2, 'yellow')
    c.px(x + 3, y - 17, 'orange'); c.px(x + 4, y - 18, 'red')
    c.px(x - 3, y - 18, 'orange'); c.px(x - 4, y - 20, 'red'); c.px(x - 3, y - 20, 'orange')
    c.px(x - 1, y - 19, 'red'); c.px(x, y - 20, 'orange')


def torso_knight(c, cx, sh_y, hip_y, lean, th):
    for i in range(th):
        k = i / (th - 1)
        ox = round(lean * (1 - k))
        if i < 5:   # белая рубашка
            c.rect(cx - 5 + ox, sh_y + i, 11, 1, 'white')
            c.px(cx - 5 + ox, sh_y + i, 'silver'); c.px(cx - 4 + ox, sh_y + i, 'silver')
        else:       # чёрный корсет, сужается к талии
            pinch = 1 if 8 <= i <= 13 else 0
            c.rect(cx - 5 + ox + pinch, sh_y + i, 11 - pinch, 1, 'ink')
            c.px(cx - 4 + ox + pinch, sh_y + i, 'night')
            c.px(cx + 4 + ox, sh_y + i, 'night')
            if i % 2 == 1:
                c.px(cx + 2 + ox, sh_y + i, 'amber')      # заклёпки
                c.px(cx - 2 + ox, sh_y + i, 'gold')
    # высокий ворот
    c.rect(cx - 2 + lean, sh_y - 3, 5, 3, 'white')
    c.px(cx - 2 + lean, sh_y - 3, 'silver')
    # синий пояс с лентой
    c.rect(cx - 5, hip_y - 2, 11, 2, 'blue')
    c.rect(cx - 5, hip_y - 2, 11, 1, 'cyan')
    c.rect(cx - 7, hip_y - 1, 2, 4, 'blue'); c.px(cx - 8, hip_y + 3, 'navy')
    c.rect(cx - 5, hip_y, 11, 3, 'night')


def pauldron(c, x, y, main, dark, light):
    x, y = round(x), round(y)
    c.ellipse(x, y, 4, 3, main)
    c.rect(x - 4, y + 1, 9, 2, dark)
    c.px(x - 1, y - 2, light); c.px(x, y - 2, light)
    # шипы
    c.line(x - 2, y - 3, x - 3, y - 6, 'ink', 1)
    c.line(x + 1, y - 3, x + 1, y - 7, 'ink', 1)
    c.line(x + 3, y - 2, x + 5, y - 5, 'ink', 1)


def draw(name, pose):
    st = STYLES[name]
    c = Canvas(SIZE, SIZE)
    hip_y = (pose.get('hip_y', 20) + 30) * S - 8
    cx = 62 + pose.get('dx', 0) * S
    th = 18
    lean = pose.get('lean', 0) * S
    sh_y = hip_y - th
    tail = pose.get('tail', 0)
    hurt = pose.get('hurt')
    flow = 0.0 if hurt else min(1.0, max(0.0, pose.get('lean', 0) * 0.45 + abs(tail) * 0.35))
    al1, al2 = pose.get('al', (4, 4))
    al1, al2 = round(al1 * 2.5), round(al2 * 2.5)

    # плащ сзади
    if name == 'king':
        cape(c, cx, sh_y - 1, lean, flow, 30, 7, ('silver', 'gray', 'white'), fur=True)
    else:
        cape(c, cx - 1, sh_y, lean, flow, 28, 4, ('lilac', 'dlilac', 'llilac'))

    # задняя нога и рука
    fa1, fa2 = pose['leg_b']
    _, _, fx, fy = limb(c, cx - 2, hip_y, fa1, 12, fa2, 12, st['thigh_b'], st['shin_b'], 5, 5)
    boot(c, fx, fy, st['boot_b'], heel=name == 'knight')
    aa1, aa2 = pose['arm_b']
    if not pose.get('arms_over'):
        _, _, bx, by = limb(c, cx - 3 + lean, sh_y + 2, aa1, al1, aa2, al2, st['sleeve_b'], st['glove_b'], 5, 4)
        hand(c, bx, by, st['glove_b'][0], st['glove_b'][1])

    # передняя нога и торс
    fa1, fa2 = pose['leg_f']
    if name == 'knight':
        torso_knight(c, cx, sh_y, hip_y, lean, th)
        _, _, fx, fy = limb(c, cx + 3, hip_y + 2, fa1, 12, fa2, 12, st['thigh_f'], st['shin_f'], 5, 5)
        boot(c, fx, fy, st['boot'], heel=True)
        c.px(round(fx) - 1, round(fy) - 6, 'slate')     # блик на колготках
    else:
        _, _, fx, fy = limb(c, cx + 3, hip_y + 2, fa1, 12, fa2, 12, st['thigh_f'], st['shin_f'], 5, 5)
        boot(c, fx, fy, st['boot'])
        torso_king(c, cx, sh_y, hip_y, lean, th, flow)
        sword(c, cx, hip_y)

    # голова
    hx = cx - 12 + lean + pose.get('head_dx', 0) * S
    hy = sh_y - 22 + pose.get('head_dy', 0) * S + st['head_dy']
    stamp_flow(c, st['head'], hx, hy, st['legend'], flow, st['hair_row'], st['strands'])
    ex, ey = st['face']
    if hurt:   # зажмуренные глаза
        c.rect(hx + ex - 1, hy + ey, 3, 2, 'skin')
        c.line(hx + ex - 1, hy + ey, hx + ex + 1, hy + ey + 1, 'ink')
        if name == 'knight':
            c.rect(hx + ex + 4, hy + ey, 3, 2, 'skin')
            c.line(hx + ex + 4, hy + ey, hx + ex + 6, hy + ey + 1, 'ink')
    if pose.get('smile'):
        if name == 'knight':
            c.rect(hx + 17, hy + 17, 4, 1, 'crimson'); c.px(hx + 16, hy + 16, 'crimson')
        else:
            c.rect(hx + 16, hy + 18, 4, 1, 'crimson')

    # передняя рука
    if pose.get('arms_over'):
        _, _, bx, by = limb(c, cx - 4 + lean, sh_y + 2, aa1, al1, aa2, al2, st['sleeve_b'], st['glove_b'], 5, 4)
        hand(c, bx, by, st['glove_b'][0], st['glove_b'][1], 4)
    if name == 'knight':
        pauldron(c, cx - 4 + lean, sh_y + 2, 'steel', 'night', 'slate')
    aa1, aa2 = pose['arm_f']
    _, _, ex2, ey2 = limb(c, cx + 5 + lean, sh_y + 2, aa1, al1, aa2, al2, st['sleeve_f'], st['glove_f'], 5, 4)
    if pose.get('fist'):
        hand(c, ex2, ey2, st['glove_f'][0], st['glove_f'][1], 5)
    else:
        hand(c, ex2, ey2, st['glove_f'][0], st['glove_f'][1], 4 if pose.get('open') else 3)
    if name == 'king' and not pose.get('arms_over') and not pose.get('fist') and not hurt:
        scepter(c, ex2, ey2)
    if name == 'knight':
        pauldron(c, cx + 5 + lean, sh_y + 2, 'slate', 'steel', 'gray')

    c.outline('ink')
    return c


def build():
    import hero
    out = {}
    for name in STYLES:
        for frame, pose in hero.poses().items():
            out[frame.replace('hero_', 'hero_%s_' % name, 1)] = draw(name, pose)
    return out


if __name__ == '__main__':
    from px import sheet
    cs = build()
    sheet(list(cs.values()), scale=4, cols=13).save('preview_characters.png')
    print(len(cs))
