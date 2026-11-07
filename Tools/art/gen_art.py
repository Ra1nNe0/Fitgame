"""Генерирует всю графику FitQuest в папку Assets/Resources/Art.
Запуск:  python gen_art.py <путь к папке Art>

Мир игры рисуется в двойном разрешении (32 пикселя на единицу): персонажи (characters.py
+ светотень charshade.py), враги, предметы и боссы (hdsprites.py, bosses.py) и фоны (hdbg.py)
нарисованы сразу в HD. Остальное (подсказки движений) удваивается через hd.py.
Интерфейс (иконки, кнопки, логотип) и портреты для катсцен (portraits.py) — в обычном разрешении.
"""
import os
import sys
import sprites
import extra
import characters
import hd
import newsprites
import portraits
import hdsprites
import hdbg
import charshade


def main(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    flat = {}
    flat.update({k: v for k, v in extra.build().items() if not k.startswith('hero_')})   # старые образы убраны
    flat.update(portraits.build())               # лица для катсцен (64x64, крупный пиксель)
    detailed = {}
    detailed.update({k: hd.sprite(v) for k, v in sprites.build().items()})
    detailed.update(hdsprites.build())           # враги, предметы и боссы, нарисованные вручную
    detailed.update(hdbg.build())                # фоны миров
    detailed.update({k: charshade.apply(v) for k, v in characters.build().items()})
    detailed.update(newsprites.build())          # новые спрайты, нарисованные сразу в HD
    for k in detailed:
        flat.pop(k, None)
    for name, canvas in list(flat.items()) + list(detailed.items()):
        canvas.save(os.path.join(out_dir, name + '.png'))
    with open(os.path.join(out_dir, 'hd_manifest.txt'), 'w') as f:
        f.write('\n'.join(sorted(detailed)) + '\n')
    print('saved', len(flat), 'flat +', len(detailed), 'HD sprites to', out_dir)


if __name__ == '__main__':
    main(sys.argv[1] if len(sys.argv) > 1 else 'out')
