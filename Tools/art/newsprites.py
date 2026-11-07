"""Новые спрайты, нарисованные сразу в HD (32 пикселя на единицу):
блок-загадка «?», самоцвет, пыль из-под ног, тень героя и частицы окружения для каждого мира."""
import math
from px import Canvas


def mystery(frame):
    """Золотой блок с вопросом. Кадр 1 — блик сместился."""
    c = Canvas(32, 32)
    c.rect(2, 2, 28, 28, 'amber')
    c.rect(2, 2, 28, 3, 'yellow')
    c.rect(2, 2, 3, 28, 'yellow')
    c.rect(2, 26, 28, 4, 'orange')
    c.rect(27, 2, 3, 28, 'orange')
    for x, y in ((5, 5), (25, 5), (5, 25), (25, 25)):          # заклёпки
        c.rect(x, y, 2, 2, 'brick')
    q = [
        "...######...",
        "..########..",
        ".###....###.",
        ".###....###.",
        "........###.",
        "......####..",
        ".....####...",
        ".....###....",
        ".....###....",
        "............",
        ".....###....",
        ".....###....",
    ]
    c.stamp(q, 11, 10, {'#': 'brick'})
    c.stamp(q, 10, 9, {'#': 'white'})
    sx = 6 + frame * 12
    for i in range(6):                                         # блик
        c.px(sx + i, 6 + i, 'white')
        c.px(sx + i + 1, 6 + i, 'yellow')
    c.outline('ink')
    return c


def gem(frame):
    c = Canvas(24, 24)
    rows = [
        "....AAAAAAAA....",
        "...ABBBACCCDA...",
        "..ABBBAACCCDDA..",
        ".AAAAAAAAAAAAAA.",
        ".AEEEEFFFFGGGGA.",
        "..AEEEEFFFGGGA..",
        "...AEEEFFFGGA...",
        "....AEEFFGGA....",
        ".....AEFFGA.....",
        "......AFFA......",
        ".......AA.......",
    ]
    leg = {'A': 'navy', 'B': 'cyan', 'C': 'white', 'D': 'blue', 'E': 'blue', 'F': 'cyan', 'G': 'navy'}
    c.stamp(rows, 4, 6, leg)
    c.outline('ink')
    if frame in (1, 2):
        x, y = (17, 5) if frame == 1 else (7, 3)
        c.px(x, y, 'white'); c.px(x - 1, y + 1, 'white'); c.px(x + 1, y + 1, 'white'); c.px(x, y + 2, 'white')
        c.px(x, y + 1, 'white')
    return c


def dust(frame):
    c = Canvas(16, 16)
    r = 2 + frame * 2
    for i, (dx, dy) in enumerate(((0, 0), (-3, 1), (3, 2))):
        rr = max(1, r - i)
        c.ellipse(8 + dx - frame, 11 + dy - frame, rr, rr * 0.7, 'sand' if frame < 2 else 'silver')
    return c


def shadow():
    c = Canvas(48, 8)
    c.ellipse(24, 4, 18, 3, 'ink')
    return c


def particle(kind, frame):
    c = Canvas(8, 8)
    if kind == 'leaf':
        rows = ["..##....", ".#ll#...", "#llll#..", ".#llll#.", "..#lll#.", "...#d#..", "....#...", "........"]
        if frame: rows = [r[::-1] for r in rows]
        c.stamp(rows, 0, 0, {'#': 'forest', 'l': 'lime', 'd': 'green'})
    elif kind == 'spark':      # кристальная искра (пещера)
        col = 'cyan' if frame == 0 else 'white'
        c.px(3, 1, col); c.px(3, 5, col); c.px(1, 3, col); c.px(5, 3, col)
        c.rect(2, 2, 3, 3, col); c.px(3, 3, 'white')
    elif kind == 'ember':      # уголёк (вулкан)
        c.rect(2, 2, 3, 3, 'orange' if frame == 0 else 'amber')
        c.px(3, 3, 'yellow'); c.px(3, 1, 'red')
    elif kind == 'mote':       # пылинка в свете факелов (замок)
        c.rect(3, 3, 2, 2, 'amber' if frame == 0 else 'yellow')
        c.px(3, 2, 'peach')
    return c


def build():
    out = {}
    for i in range(2):
        out['mystery_%d' % i] = mystery(i)
    for i in range(4):
        out['gem_%d' % i] = gem(i)
    for i in range(3):
        out['dust_%d' % i] = dust(i)
    out['shadow_0'] = shadow()
    for k in ('leaf', 'spark', 'ember', 'mote'):
        for i in range(2):
            out['particle_%s_%d' % (k, i)] = particle(k, i)
    return out


if __name__ == '__main__':
    from px import sheet
    sheet(list(build().values()), scale=8, cols=9).save('preview_new.png')
