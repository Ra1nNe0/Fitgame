"""Макет кадра игры 320x180 для оценки графики."""
from PIL import Image
import hero, sprites, backgrounds

H_ = hero.build(); S_ = sprites.build(); B_ = backgrounds.build()

def frame(world, items, boss=None, off=0):
    im = Image.new('RGBA', (320, 180), (0, 0, 0, 255))
    far = B_['bg_%s_far' % world].image(); near = B_['bg_%s_near' % world].image(); gr = B_['bg_%s_ground' % world].image()
    im.alpha_composite(far.crop((off // 6, 76, off // 6 + 320, 256)))
    im.alpha_composite(near.crop((off // 2, 76, off // 2 + 320, 256)))
    im.alpha_composite(gr.crop((off, 0, off + 320, 42)), (0, 138))
    def put(c, x, base=138):
        s = c.image(); im.alpha_composite(s, (int(x - s.width / 2), int(base - s.height)))
    for name, x, lift in items:
        c = S_.get(name) or H_.get(name)
        put(c, x, 138 - lift)
    if boss:
        put(S_[boss], 250, 138)
    return im.resize((960, 540), Image.NEAREST)

if __name__ == '__main__':
    frame('forest', [('hero_run_0', 56, 0), ('spikes_0', 130, 0), ('slime_0', 190, 0), ('bat_0', 250, 26), ('coin_0', 290, 50), ('cue_jump_0', 130, 22)]).save('mock_forest.png')
    frame('cave', [('hero_squat_0', 56, 0), ('bat_1', 110, 26), ('log_0', 180, 0), ('crate_0', 240, 0), ('cue_squat_0', 110, 48)], off=90).save('mock_cave.png')
    frame('volcano', [('hero_jump_0', 56, 30), ('rock_0', 90, 0), ('fireball_0', 170, 26), ('orb_0', 210, 14)], boss='boss_golem_0', off=40).save('mock_volcano.png')
    frame('castle', [('hero_punch_0', 56, 0), ('orb_1', 84, 14), ('heart_0', 150, 52)], boss='boss_sloth_0', off=200).save('mock_castle.png')
