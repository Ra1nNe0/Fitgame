"""Скины героя, помощник Пульс, логотип, элементы интерфейса и иконки."""
import math
from PIL import Image, ImageDraw, ImageFont
from px import Canvas, PAL
from sprites import blob
import hero
import os

FONT_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'PressStart2P-Regular.ttf')

# ------------------------------------------------------------------ скины героя
SKINS = {
    'classic': {},
    'forest': {'red': 'yellow', 'crimson': 'amber', 'blue': 'green', 'navy': 'forest',
               'rust': 'bark', 'brick': 'dbark', 'peach': 'wood', 'steel': 'wood', 'night': 'bark'},
    'neon': {'red': 'cyan', 'crimson': 'blue', 'blue': 'magenta', 'navy': 'purple', 'amber': 'cyan',
             'rust': 'yellow', 'brick': 'amber', 'peach': 'white', 'steel': 'night', 'night': 'ink'},
    'ninja': {'blue': 'night', 'navy': 'ink', 'amber': 'red', 'rust': 'night', 'brick': 'ink',
              'peach': 'steel', 'steel': 'ink', 'night': 'ink', 'white': 'slate', 'silver': 'steel', 'gray': 'night'},
    'champion': {'red': 'white', 'crimson': 'silver', 'blue': 'yellow', 'navy': 'amber', 'amber': 'red',
                 'rust': 'sand', 'brick': 'peach', 'peach': 'white', 'steel': 'crimson', 'night': 'brick',
                 'white': 'yellow', 'silver': 'amber'},
}


def recolor(canvas, mapping):
    out = Canvas(canvas.w, canvas.h)
    for y in range(canvas.h):
        for x in range(canvas.w):
            c = canvas.p[y][x]
            out.p[y][x] = mapping.get(c, c) if c else None
    return out


def skins():
    base = hero.build()
    result = {}
    for skin, mapping in SKINS.items():
        for name, canvas in base.items():
            # hero_run_0 -> hero_<skin>_run_0
            result[name.replace('hero_', 'hero_%s_' % skin, 1)] = recolor(canvas, mapping)
    return result


# ------------------------------------------------------------------ Пульс — помощник-тренер
def pulse(frame):
    """Маленький робот в форме сердца: экран-лицо, антенна, ручки."""
    c = Canvas(32, 32)
    bob = 1 if frame in (1, 3) else 0
    cy = 15 + bob
    # тело-сердце
    blob(c, 11, cy - 3, 6, 6, 'red', 'crimson', 'pink')
    blob(c, 20, cy - 3, 6, 6, 'red', 'crimson', None)
    for i in range(10):
        w = 13 - i * 1.3
        c.rect(int(15.5 - w), cy + i - 2, int(2 * w) + 1, 1, 'red' if i < 6 else 'crimson')
    c.rect(14, cy + 8, 3, 1, 'crimson')
    # экран-лицо
    c.rect(9, cy - 5, 14, 8, 'night')
    c.rect(9, cy - 5, 14, 1, 'steel')
    if frame == 3:      # радуется: глаза дугой
        for ex in (12, 18):
            c.px(ex, cy - 1, 'cyan'); c.px(ex + 1, cy - 2, 'cyan'); c.px(ex + 2, cy - 1, 'cyan')
    else:
        blink = frame == 1
        for ex in (12, 18):
            c.rect(ex, cy - 3 + (1 if blink else 0), 2, 1 if blink else 3, 'cyan')
    if frame == 2:      # говорит
        c.rect(14, cy, 4, 2, 'pink')
    else:
        c.rect(14, cy + 1, 4, 1, 'cyan')
    # антенна
    c.rect(15, cy - 11, 1, 4, 'silver')
    blob(c, 15.5, cy - 12, 1.6, 1.6, 'yellow', 'amber', None)
    # ручки и «турбины»
    c.rect(3, cy, 3, 2, 'silver'); c.rect(26, cy - 1 + bob, 3, 2, 'silver')
    c.rect(2, cy - 1, 2, 2, 'white'); c.rect(28, cy - 2 + bob, 2, 2, 'white')
    c.outline('ink')
    # огонёк реактивного ранца
    flame = 'yellow' if frame % 2 == 0 else 'amber'
    c.rect(14, cy + 10, 3, 2, flame); c.px(15, cy + 12, 'orange')
    return c


# ------------------------------------------------------------------ логотип
def logo():
    font = ImageFont.truetype(FONT_PATH, 16)
    text = 'FITQUEST'
    w = 8 * 16 + 2
    im = Image.new('L', (w, 18), 0)
    ImageDraw.Draw(im).text((1, 1), text, font=font, fill=255)
    c = Canvas(w + 6, 26)
    grad = ['yellow', 'yellow', 'yellow', 'amber', 'amber', 'amber', 'orange', 'orange', 'orange',
            'red', 'red', 'red', 'crimson', 'crimson', 'crimson', 'crimson', 'crimson', 'crimson']
    for y in range(18):
        for x in range(w):
            if im.getpixel((x, y)) > 127:
                c.px(x + 2, y + 2, grad[y])
    # блик на верхних пикселях букв
    for y in range(c.h):
        for x in range(c.w):
            if c.p[y][x] and (y == 0 or c.p[y - 1][x] is None):
                c.p[y][x] = 'white'
    shadow = Canvas(c.w, c.h)
    for y in range(c.h):
        for x in range(c.w):
            if c.p[y][x]:
                shadow.px(x + 2, y + 2, 'night')
    shadow.outline('ink')
    c.outline('ink')
    out = Canvas(c.w + 2, c.h + 2)
    out.blit(shadow, 0, 0)
    out.blit(c, 0, 0)
    return out


# ------------------------------------------------------------------ интерфейс
def panel(fill, light, dark):
    c = Canvas(16, 16)
    c.rect(1, 1, 14, 14, fill)
    c.rect(1, 1, 14, 1, light); c.rect(1, 1, 1, 14, light)
    c.rect(1, 14, 14, 1, dark); c.rect(14, 1, 1, 14, dark)
    c.rect(0, 2, 1, 12, 'ink'); c.rect(15, 2, 1, 12, 'ink'); c.rect(2, 0, 12, 1, 'ink'); c.rect(2, 15, 12, 1, 'ink')
    c.px(1, 1, 'ink'); c.px(14, 1, 'ink'); c.px(1, 14, 'ink'); c.px(14, 14, 'ink')
    return c


def button(pressed=False):
    """Серая кнопка: игра подкрашивает её в нужный цвет (цвет умножается)."""
    c = Canvas(16, 16)
    top = 2 if pressed else 1
    c.rect(1, top, 14, 14 - top + (1 if pressed else -1), (255, 255, 255, 255))
    if not pressed:
        c.rect(1, 13, 14, 2, (150, 150, 160, 255))       # нижняя грань
        c.rect(2, 2, 12, 1, (255, 255, 255, 255))
        c.rect(1, 12, 14, 1, (210, 210, 218, 255))
    else:
        c.rect(1, 2, 14, 1, (190, 190, 200, 255))
    ink = PAL['ink']
    for x in range(2, 14):
        c.px(x, 0 if not pressed else 1, 'ink'); c.px(x, 15, 'ink')
    for y in range(2 if not pressed else 3, 14):
        c.px(0, y, 'ink'); c.px(15, y, 'ink')
    c.px(1, 1 if not pressed else 2, 'ink'); c.px(14, 1 if not pressed else 2, 'ink'); c.px(1, 14, 'ink'); c.px(14, 14, 'ink')
    return c


ICONS = {
    'star': ({'y': 'yellow', 'a': 'amber', 'w': 'white'}, [
        "......y.....",
        ".....yyy....",
        ".....yyy....",
        "yyyyywyyyyyy",
        ".yyyyyyyyya.",
        "..yyyyyyya..",
        "...yyyyyy...",
        "..yyyy.yya..",
        "..yya...ya..",
        ".yaa.....aa.",
    ]),
    'star_empty': ({'y': 'steel', 'a': 'night', 'w': 'slate'}, None),
    'lock': ({'g': 'silver', 'd': 'gray', 'k': 'amber', 'y': 'yellow', 'b': 'rust'}, [
        "...dddd.....",
        "..d....d....",
        "..d....d....",
        "..d....d....",
        ".kyyyyyyk...",
        ".kyybbyyk...",
        ".kyybbyyk...",
        ".kyyybyyk...",
        ".kkkkkkkk...",
    ]),
    'shield': ({'c': 'cyan', 'b': 'blue', 'w': 'white', 'n': 'navy'}, [
        ".nnnnnnnnnn.",
        ".nwccccccbn.",
        ".nwccccccbn.",
        ".nccccccbbn.",
        ".nccccccbbn.",
        "..nccccbbn..",
        "..nccccbbn..",
        "...nccbbn...",
        "....nbbn....",
        ".....nn.....",
    ]),
    'magnet': ({'r': 'red', 'c': 'crimson', 's': 'silver', 'w': 'white'}, [
        "..rrrrrrrr..",
        ".rrwrrrrrrr.",
        "rrr......crr",
        "rr........cr",
        "rr........cr",
        "rr........cr",
        "ss........ss",
        "ws........ws",
    ]),
    'crown': ({'y': 'yellow', 'a': 'amber', 'r': 'red', 'c': 'cyan'}, [
        "y....y....y.",
        "yy..yyy..yy.",
        "yyy.yyy.yyy.",
        "yyyyyyyyyyy.",
        "yyryycyyryy.",
        "yyyyyyyyyyy.",
        "aaaaaaaaaaa.",
    ]),
    'trophy': ({'y': 'yellow', 'a': 'amber', 'w': 'white', 'b': 'bark'}, [
        "yyyyyyyyyy..",
        "ywyyyyyyay..",
        "yyyyyyyyay..",
        ".yyyyyyaa...",
        "..yyyyaa....",
        "....ya......",
        "....ya......",
        "..aaaaaa....",
        "..bbbbbb....",
    ]),
    'gear': ({'g': 'silver', 'd': 'gray'}, [
        "....gg......",
        ".gg.gg.gg...",
        ".gggggggg...",
        "..gg..gg....",
        "ggg....ggg..",
        "ggg....ggg..",
        "..gg..dd....",
        ".ggggdddd...",
        ".gg.dd.dd...",
        "....dd......",
    ]),
    'camera': ({'g': 'slate', 'b': 'night', 'c': 'cyan', 'w': 'white', 'r': 'red'}, [
        "...gggg.....",
        "gggggggggg..",
        "gbbbbbbbbg..",
        "gbbcccwbbgr.",
        "gbbcbbcbbg..",
        "gbbcbbcbbg..",
        "gbbbccbbbg..",
        "gggggggggg..",
    ]),
    'play': ({'g': 'lime', 'd': 'green'}, [
        "gg..........",
        "gggg........",
        "gggggg......",
        "gggggggg....",
        "gggggggg....",
        "ggggggdd....",
        "ggggdd......",
        "ggdd........",
    ]),
    'cart': ({'s': 'silver', 'd': 'gray', 'y': 'yellow'}, [
        "ss..........",
        ".s.yyyyyyy..",
        ".ssyyyyyyy..",
        "..syyyyyys..",
        "..ssssssss..",
        "...s....s...",
        "..dd...dd...",
    ]),
    'chart': ({'g': 'lime', 'b': 'blue', 'y': 'yellow', 'w': 'white'}, [
        "w...........",
        "w.......yy..",
        "w.......yy..",
        "w....bb.yy..",
        "w....bb.yy..",
        "w.gg.bb.yy..",
        "w.gg.bb.yy..",
        "wwwwwwwwwww.",
    ]),
    'check': ({'g': 'lime', 'd': 'green'}, [
        "..........gg",
        ".........ggd",
        "........ggd.",
        "gg.....ggd..",
        "dgg...ggd...",
        ".dgg.ggd....",
        "..dgggd.....",
        "...ddd......",
    ]),
    'back': ({'w': 'white', 's': 'silver'}, [
        "....w.......",
        "...ww.......",
        "..wwwwwwwww.",
        ".wwwwwwwwww.",
        "..sssssssss.",
        "...ss.......",
        "....s.......",
    ]),
    'flame': ({'r': 'red', 'o': 'orange', 'y': 'yellow', 'w': 'white'}, [
        ".....r......",
        "....rr......",
        "...rror.....",
        "..rrooorr...",
        "..rooyoor...",
        ".rroyyyorr..",
        ".rooywyoor..",
        ".rooyyyoor..",
        "..rrooorr...",
    ]),
    'medal': ({'b': 'blue', 'r': 'red', 'y': 'yellow', 'a': 'amber', 'w': 'white'}, [
        ".bb...rr....",
        "..bb.rr.....",
        "...bbr......",
        "...yyyy.....",
        "..ywyyya....",
        "..yyayya....",
        "..yyyyya....",
        "...aaaa.....",
    ]),
    'keyboard': ({'g': 'silver', 'd': 'slate', 'w': 'white'}, [
        "gggggggggggg",
        "gwdwdwdwdwdg",
        "gdwdwdwdwdwg",
        "gwdwwwwwwdwg",
        "gggggggggggg",
    ]),
    'film': ({'g': 'slate', 'w': 'white', 'b': 'night', 'r': 'red'}, [
        "gggggggggg..",
        "gwbwbwbwbg..",
        "gbbbbbbbbgr.",
        "gbbrrbbbbgrr",
        "gbbbbbbbbgr.",
        "gwbwbwbwbg..",
        "gggggggggg..",
    ]),
    'brain': ({'p': 'pink', 'm': 'magenta', 'w': 'white'}, [
        "..pppppp....",
        ".ppwpmppp...",
        "ppppmpppmp..",
        "pmppmpppmp..",
        "pmmppmmppp..",
        "pppppppmpp..",
        ".pppmppppp..",
        "..pppppp....",
        "....pp......",
    ]),
    'pause': ({'w': 'white', 's': 'silver'}, [
        "ww..ww......",
        "ww..ww......",
        "ww..ww......",
        "ww..ww......",
        "ww..ww......",
        "ss..ss......",
    ]),
    'map': ({'s': 'sand', 'p': 'peach', 'r': 'red', 'g': 'green'}, [
        "sss.ppp.sss.",
        "sgsspppssss.",
        "sggspprsrss.",
        "sssspprrrss.",
        "ssssppprsss.",
        "sssspppssgs.",
        "sss.ppp.sss.",
    ]),
}


def icon(name):
    legend, rows = ICONS[name]
    if rows is None:            # пустая звезда = контур звезды
        rows = ICONS['star'][1]
        legend = {'y': 'steel', 'a': 'night', 'w': 'slate'}
    c = Canvas(16, 16)
    h = len(rows); w = len(rows[0])
    c.stamp(rows, (16 - w) // 2, (16 - h) // 2, legend)
    c.outline('ink')
    return c


def shield_bubble():
    c = Canvas(48, 48)
    for a in range(0, 360, 2):
        for r, col in ((21.5, 'cyan'), (20.5, 'blue')):
            x = 23.5 + math.cos(math.radians(a)) * r
            y = 23.5 + math.sin(math.radians(a)) * r
            c.px(x, y, col)
    for a in range(200, 250, 3):
        c.px(23.5 + math.cos(math.radians(a)) * 17, 23.5 + math.sin(math.radians(a)) * 17, 'white')
    return c


def build():
    out = {}
    out.update(skins())
    for i in range(4):
        out['pulse_%d' % i] = pulse(i)
    out['logo_0'] = logo()
    out['ui_panel'] = panel('night', 'steel', 'ink')
    out['ui_panel_light'] = panel('sand', 'white', 'peach')
    out['ui_button'] = button(False)
    out['ui_button_pressed'] = button(True)
    for name in ICONS:
        out['icon_' + name] = icon(name)
    out['shield_0'] = shield_bubble()
    return out
