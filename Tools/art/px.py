"""Мини-библиотека для рисования пиксельной графики (палитра ENDESGA 32)."""
from PIL import Image
import math

PAL = {
    'brick': 'be4a2f', 'rust': 'd77643', 'sand': 'ead4aa', 'peach': 'e4a672', 'wood': 'b86f50',
    'bark': '733e39', 'dbark': '3e2731', 'crimson': 'a22633', 'red': 'e43b44', 'orange': 'f77622',
    'amber': 'feae34', 'yellow': 'fee761', 'lime': '63c74d', 'green': '3e8948', 'forest': '265c42',
    'dteal': '193c3e', 'navy': '124e89', 'blue': '0099db', 'cyan': '2ce8f5', 'white': 'ffffff',
    'silver': 'c0cbdc', 'gray': '8b9bb4', 'slate': '5a6988', 'steel': '3a4466', 'night': '262b44',
    'ink': '181425', 'hot': 'ff0044', 'purple': '68386c', 'magenta': 'b55088', 'pink': 'f6757a',
    'skin': 'e8b796', 'dskin': 'c28569',
}


def rgb(name):
    h = PAL[name]
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)


class Canvas:
    def __init__(self, w, h, fill=None):
        self.w, self.h = w, h
        self.p = [[fill] * w for _ in range(h)]

    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.p[y][x]
        return None

    def px(self, x, y, c, wrap=False):
        x, y = int(round(x)), int(round(y))
        if wrap:
            x %= self.w
        if 0 <= x < self.w and 0 <= y < self.h:
            self.p[y][x] = c

    def rect(self, x, y, w, h, c, wrap=False):
        for yy in range(int(y), int(y + h)):
            for xx in range(int(x), int(x + w)):
                self.px(xx, yy, c, wrap)

    def line(self, x0, y0, x1, y1, c, t=1):
        n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
        o = (t - 1) // 2
        for i in range(n + 1):
            k = i / n
            x = x0 + (x1 - x0) * k
            y = y0 + (y1 - y0) * k
            self.rect(round(x) - o, round(y) - o, t, t, c)

    def ellipse(self, cx, cy, rx, ry, c, wrap=False):
        for y in range(int(cy - ry - 1), int(cy + ry + 2)):
            for x in range(int(cx - rx - 1), int(cx + rx + 2)):
                if ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1.0:
                    self.px(x, y, c, wrap)

    def stamp(self, rows, ox, oy, legend, flip=False):
        for j, row in enumerate(rows):
            r = row[::-1] if flip else row
            for i, ch in enumerate(r):
                if ch in legend and legend[ch]:
                    self.px(ox + i, oy + j, legend[ch])

    def blit(self, other, ox, oy, wrap=False):
        for y in range(other.h):
            for x in range(other.w):
                c = other.p[y][x]
                if c:
                    self.px(ox + x, oy + y, c, wrap)

    def outline(self, c, diagonal=False):
        """Тёмный контур вокруг всех непрозрачных пикселей."""
        src = [row[:] for row in self.p]
        nb = [(1, 0), (-1, 0), (0, 1), (0, -1)]
        if diagonal:
            nb += [(1, 1), (-1, 1), (1, -1), (-1, -1)]
        for y in range(self.h):
            for x in range(self.w):
                if src[y][x] is None:
                    for dx, dy in nb:
                        xx, yy = x + dx, y + dy
                        if 0 <= xx < self.w and 0 <= yy < self.h and src[yy][xx] is not None:
                            self.p[y][x] = c
                            break

    def replace(self, a, b):
        for y in range(self.h):
            for x in range(self.w):
                if self.p[y][x] == a:
                    self.p[y][x] = b

    def flipped(self):
        c = Canvas(self.w, self.h)
        c.p = [row[::-1] for row in self.p]
        return c

    def image(self, scale=1):
        im = Image.new('RGBA', (self.w, self.h), (0, 0, 0, 0))
        for y in range(self.h):
            for x in range(self.w):
                c = self.p[y][x]
                if c:
                    im.putpixel((x, y), rgb(c) if isinstance(c, str) else c)
        if scale != 1:
            im = im.resize((self.w * scale, self.h * scale), Image.NEAREST)
        return im

    def save(self, path):
        self.image().save(path)


def sheet(canvases, scale=6, bg=(60, 70, 100, 255), cols=8, pad=4):
    """Контактный лист для просмотра нескольких спрайтов сразу."""
    cw = max(c.w for c in canvases) * scale + pad
    ch = max(c.h for c in canvases) * scale + pad
    rows = (len(canvases) + cols - 1) // cols
    im = Image.new('RGBA', (cw * min(cols, len(canvases)) + pad, ch * rows + pad), bg)
    for i, c in enumerate(canvases):
        s = c.image(scale)
        im.alpha_composite(s, (pad + (i % cols) * cw, pad + (i // cols) * ch))
    return im
