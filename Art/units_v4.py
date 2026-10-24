"""Hero mastery costumes: a full redesign of every hero (new silhouette, outfit and weapon), unlocked by mastery.
Saved as Units/{hero}_c.png and Portraits/{hero}_c.png. Run from the Art folder: python units_v4.py"""
from style_kit import Spr
import units_v3  # noqa: F401  (registers the newer heroes and enemies in units_v2)
import units_v2 as U

BLACKIRON = ['0b0910', '1d1726', '352a45', '6b6386']
EMBERCRACK = 'f28a3a'
NAVY = ['0b0910', '1d3340', '2a5560', '6f9be0']
SUN = ['5c4228', 'b89262', 'ffd76b', 'fff6d8']
WHITE = ['433758', '8a86a6', 'd8d6e6', 'fff6d8']
FOX = ['3a2a18', 'a63d25', 'f28a3a', 'ffd7a0']
BEAR = ['1d1726', '3a2a18', '5c4228', '8a6640']
FEATHER = ['0b0910', '141019', '352a45', '554a6d']
BRASS = ['3a2a18', '8a6640', 'd8a040', 'ffe09a']
SCALE = ['2a1414', '74291f', 'a63d25', 'f28a3a']


def vanguard_c():
    """Cinderforged Juggernaut: black-iron plate split by ember cracks, a horned great helm, tower shield and warhammer."""
    s = Spr()
    s.part(lambda d: d.polygon([(20, 24), (36, 24), (42, 61), (14, 61)]), ['0b0910', '2a1414', '74291f', 'a63d25'], round_=0.3)  # tattered cape
    s.flat(lambda d: (d.line([(16, 61), (19, 56)]), d.line([(24, 61), (26, 57)]), d.line([(34, 61), (33, 57)])), '0b0910')
    s.part(lambda d: (d.rectangle([22, 44, 29, 57]), d.rectangle([31, 44, 38, 57])), BLACKIRON)                      # greaves
    s.part(lambda d: (d.polygon([(20, 56), (30, 56), (31, 62), (19, 62)]), d.polygon([(31, 56), (39, 56), (42, 62), (31, 62)])), BLACKIRON)
    s.part(lambda d: d.polygon([(18, 24), (42, 24), (40, 46), (20, 46)]), BLACKIRON, round_=0.7)                    # massive cuirass
    s.flat(lambda d: (d.line([(26, 28), (29, 34), (27, 40)]), d.line([(35, 30), (33, 37)])), EMBERCRACK)          # ember cracks
    s.glow(28, 34, 'ffb85a', 4, 0.45)
    s.part(lambda d: d.rectangle([19, 42, 41, 45]), 'leather', round_=0)
    s.part(lambda d: d.polygon([(14, 18), (26, 18), (27, 28), (12, 28)]), BLACKIRON)                                # spiked pauldrons
    s.part(lambda d: d.polygon([(34, 18), (46, 18), (48, 28), (33, 28)]), BLACKIRON)
    s.part(lambda d: (d.polygon([(14, 18), (11, 12), (17, 18)]), d.polygon([(46, 18), (50, 12), (44, 18)])), 'bone')
    s.part(lambda d: d.rounded_rectangle([22, 4, 39, 23], 3), BLACKIRON)                                            # bucket helm
    s.flat(lambda d: d.rectangle([30, 12, 39, 14]), '0b0910')                                                       # visor slit
    s.flat(lambda d: d.rectangle([31, 13, 37, 13]), EMBERCRACK); s.glow(34, 13, 'ffb85a', 4, 0.6)                 # burning eyes
    s.part(lambda d: (d.polygon([(23, 6), (15, 0), (19, 8)]), d.polygon([(37, 5), (44, 0), (40, 8)])), 'bone')    # horns
    # warhammer raised behind
    s.part(lambda d: d.line([(18, 30), (10, 8)], width=3), 'wood')
    s.part(lambda d: d.rectangle([3, 2, 17, 10]), BLACKIRON, round_=0.4)
    s.flat(lambda d: d.line([(4, 3), (16, 3)]), '8a86a6')
    # tower shield in front
    s.part(lambda d: d.rounded_rectangle([36, 22, 52, 56], 3), BLACKIRON, round_=0.6)
    s.flat(lambda d: d.line([(36, 22), (52, 22), (52, 56), (36, 56), (36, 22)], width=1), '8a6640')
    s.flat(lambda d: (d.line([(44, 26), (44, 52)], width=2), d.line([(39, 34), (49, 34)], width=2)), EMBERCRACK)
    s.glow(44, 34, 'ffb85a', 6, 0.4)
    return s.finish(rim='f28a3a', rim_side=-1)


def arcanist_c():
    """Storm Sage: layered navy robes with lightning runes, a deep hood and a crown of floating rune stones, crystal staff."""
    s = Spr()
    s.part(lambda d: d.polygon([(20, 24), (38, 24), (46, 62), (12, 62)]), NAVY, round_=0.5)                          # robe
    s.part(lambda d: d.polygon([(24, 36), (34, 36), (38, 62), (20, 62)]), ['141019', '1d1726', '352a45', '554a6d'])  # under-robe
    s.flat(lambda d: (d.line([(15, 54), (19, 48), (17, 44)]), d.line([(40, 50), (37, 44), (41, 40)])), '9fd8ff')   # lightning runes
    s.flat(lambda d: d.line([(12, 61), (46, 61)], width=2), '6f9be0')
    s.part(lambda d: d.polygon([(16, 26), (24, 25), (22, 44), (12, 42)]), NAVY)                                       # back sleeve
    s.part(lambda d: d.polygon([(20, 6), (38, 6), (41, 28), (17, 28)]), NAVY, round_=0.7)                            # deep hood
    s.flat(lambda d: d.polygon([(27, 12), (37, 12), (38, 25), (28, 26)]), '0b0910')                                  # face in shadow
    s.part(lambda d: d.polygon([(28, 19), (37, 19), (35, 31), (31, 36), (29, 30)]), 'cream')                         # long beard
    s.dots([(31, 16), (35, 16)], '9fd8ff'); s.glow(33, 16, '9fd8ff', 3, 0.7)                                         # glowing eyes
    for x, y in ((18, 2), (28, 0), (38, 2), (45, 8)):                                                                 # rune-stone crown
        s.part(lambda d, x=x, y=y: d.polygon([(x - 2, y + 1), (x, y - 1), (x + 2, y + 1), (x, y + 4)]), 'rock', round_=0.6)
        s.dot(x, y + 1, '9fd8ff')
    s.glow(28, 2, '9fd8ff', 8, 0.25)
    s.part(lambda d: d.line([(47, 14), (45, 62)], width=3), ['1d1726', '352a45', '6b6386', 'b0aec8'])                 # iron staff
    s.part(lambda d: d.polygon([(34, 27), (46, 31), (45, 37), (33, 35)]), NAVY)
    s.part(lambda d: d.ellipse([43, 30, 48, 35]), 'skin')
    s.part(lambda d: d.polygon([(47, 2), (51, 9), (47, 16), (43, 9)]), ['1d3340', '3f8a8a', '9fd8ff', 'f4f2fa'], round_=0.8)  # crystal
    s.glow(47, 9, 'd8f4ff', 6, 0.6)
    s.flat(lambda d: (d.line([(51, 9), (55, 5), (54, 12), (58, 9)])), 'd9c8ff')                                      # arcing spark
    return s.finish(rim='9fd8ff', rim_side=1)


def warden_c():
    """Sunpriestess: white-and-gold vestments, a radiant halo disc behind her head and a sun-disc staff."""
    s = Spr()
    s.part(lambda d: d.ellipse([16, 0, 44, 28]), SUN, round_=0.9, cast=False)                                         # halo disc
    s.flat(lambda d: d.ellipse([19, 3, 41, 25]), 'b89262')
    s.part(lambda d: d.ellipse([20, 4, 40, 24]), ['b89262', 'ffd76b', 'ffe09a', 'fff6d8'], round_=1.0, cast=False)
    s.glow(30, 14, 'fff6d8', 14, 0.35)
    s.part(lambda d: d.polygon([(21, 24), (38, 24), (45, 62), (14, 62)]), WHITE, round_=0.5)                          # vestments
    s.part(lambda d: d.polygon([(26, 26), (33, 26), (36, 62), (23, 62)]), SUN)                                        # gold stole
    s.flat(lambda d: (d.polygon([(29, 36), (32, 40), (29, 44), (26, 40)]), d.line([(29, 48), (29, 58)])), 'fff6d8')
    s.flat(lambda d: d.line([(14, 61), (45, 61)], width=2), 'ffd76b')
    s.part(lambda d: d.ellipse([25, 9, 35, 21]), 'skin')                                                             # face
    s.part(lambda d: d.polygon([(23, 8), (36, 7), (37, 12), (31, 10), (25, 14), (24, 26), (21, 24)]), 'hair')        # braided crown
    s.flat(lambda d: d.line([(25, 9), (35, 8)]), 'ffd76b')
    s.dots([(30, 15), (33, 15)], '141019'); s.dot(32, 19, 'a63d25')
    s.part(lambda d: d.polygon([(17, 27), (24, 26), (22, 40), (14, 38)]), WHITE)                                      # back arm
    s.part(lambda d: d.polygon([(36, 27), (44, 31), (42, 36), (35, 33)]), WHITE)                                      # front arm
    s.part(lambda d: d.ellipse([41, 30, 46, 35]), 'skin')
    s.part(lambda d: d.line([(44, 12), (43, 62)], width=2), SUN)                                                     # sun staff
    s.part(lambda d: d.ellipse([39, 3, 49, 13]), SUN, round_=0.9)
    s.flat(lambda d: [d.line([(44, 8), (44 + dx, 8 + dy)]) for dx, dy in ((7, 0), (-7, 0), (0, -7), (5, 5), (-5, 5), (5, -5), (-5, -5))], 'ffe09a')
    s.glow(44, 8, 'fff6d8', 6, 0.6)
    return s.finish(rim='ffd76b', rim_side=-1)


def shadowblade_c():
    """Kitsune: a white fox mask with tall ears, a short wrapped jacket, a fox-tail scarf and twin curved blades."""
    s = Spr()
    s.part(lambda d: d.polygon([(22, 24), (12, 26), (2, 34), (8, 36), (4, 42), (14, 38), (22, 32)]), FOX, round_=0.8)   # fox-tail scarf
    s.flat(lambda d: d.polygon([(2, 34), (6, 35), (4, 38)]), 'fff6d8')
    s.part(lambda d: d.polygon([(21, 43), (27, 43), (23, 58), (16, 58)]), ['0b0910', '141019', '352a45', '554a6d'])   # back leg
    s.part(lambda d: d.polygon([(32, 43), (38, 43), (43, 58), (36, 58)]), ['0b0910', '141019', '352a45', '554a6d'])
    s.part(lambda d: (d.polygon([(14, 57), (24, 57), (25, 62), (13, 62)]), d.polygon([(35, 57), (44, 57), (47, 62), (35, 62)])), 'dark')
    s.part(lambda d: d.polygon([(22, 25), (38, 23), (41, 44), (20, 45)]), ['1d1726', '74291f', 'a63d25', 'd85f2a'])   # wrapped jacket
    s.flat(lambda d: d.line([(26, 25), (36, 41)], width=2), 'fff6d8')                                                # cross wrap
    s.part(lambda d: d.rectangle([20, 40, 41, 42]), 'dark', round_=0)
    s.flat(lambda d: d.polygon([(28, 41), (32, 41), (30, 45)]), 'ffd76b')                                            # tassel
    s.part(lambda d: d.polygon([(24, 10), (38, 9), (41, 21), (26, 25), (20, 15)]), ['0b0910', '141019', '352a45', '554a6d'])  # hair
    s.part(lambda d: d.polygon([(28, 11), (39, 11), (44, 17), (39, 22), (29, 22)]), WHITE, round_=0.8)                # fox mask
    s.part(lambda d: (d.polygon([(28, 12), (27, 2), (33, 10)]), d.polygon([(35, 11), (38, 2), (40, 12)])), WHITE)     # tall ears
    s.flat(lambda d: (d.line([(29, 5), (30, 10)]), d.line([(37, 5), (37, 10)])), 'a63d25')
    s.flat(lambda d: (d.line([(33, 15), (36, 14)]), d.line([(33, 18), (36, 19)]), d.line([(38, 16), (41, 16)])), 'a63d25')  # red markings
    s.dot(38, 15, 'ffd76b'); s.glow(38, 15, 'ffd76b', 3, 0.6)
    # twin curved blades
    s.part(lambda d: d.polygon([(36, 29), (44, 33), (42, 37), (35, 34)]), ['1d1726', '74291f', 'a63d25', 'd85f2a'])
    s.part(lambda d: d.ellipse([41, 32, 45, 36]), 'skin')
    s.flat(lambda d: d.arc([42, 18, 62, 38], 230, 330, width=2), 'f4f2fa')
    s.flat(lambda d: d.line([(44, 30), (44, 37)]), 'ffd76b')
    s.part(lambda d: d.polygon([(22, 28), (16, 36), (19, 38), (25, 31)]), ['1d1726', '74291f', 'a63d25', 'd85f2a'])
    s.flat(lambda d: d.arc([6, 34, 24, 54], 60, 170, width=2), 'f4f2fa')
    return s.finish(rim='f28a3a', rim_side=1)


def ranger_c():
    """Wildwarden: a stag-skull headdress with antlers, a cloak of leaves, and a living longbow wrapped in vines."""
    s = Spr()
    s.part(lambda d: d.polygon([(20, 20), (36, 20), (42, 60), (14, 60)]), ['233a22', '3d6230', '6f9a40', 'b3d05a'], round_=0.4)  # leaf cloak
    for x, y in ((16, 56), (22, 52), (38, 56), (34, 48), (18, 44), (40, 44)):                                       # leaf scales
        s.flat(lambda d, x=x, y=y: d.polygon([(x, y), (x + 3, y - 2), (x + 2, y + 2)]), '233a22')
    s.part(lambda d: (d.rectangle([24, 44, 29, 57]), d.rectangle([31, 44, 36, 57])), 'leather')
    s.part(lambda d: (d.polygon([(23, 55), (30, 55), (31, 62), (22, 62)]), d.polygon([(31, 55), (37, 55), (40, 62), (31, 62)])), 'wood')
    s.part(lambda d: d.rounded_rectangle([22, 24, 37, 44], 3), 'leather')                                            # leather jerkin
    s.flat(lambda d: d.line([(24, 30), (35, 30)]), '8a6640')
    s.part(lambda d: d.ellipse([24, 10, 36, 23]), 'skin')                                                           # face (shadowed below the skull)
    s.flat(lambda d: d.rectangle([24, 10, 36, 14]), '0b0910')
    s.dots([(31, 16), (34, 16)], '141019'); s.flat(lambda d: d.line([(30, 19), (35, 20)]), '3d6230')             # green warpaint
    s.part(lambda d: d.polygon([(22, 4), (38, 4), (40, 12), (34, 15), (26, 15), (20, 12)]), 'bone', round_=0.8)    # stag skull
    s.flat(lambda d: (d.ellipse([26, 7, 29, 10]), d.ellipse([32, 7, 35, 10])), '0b0910')
    for side in (-1, 1):                                                                                             # antlers
        cx = 30 + side * 7
        s.part(lambda d, cx=cx, side=side: (d.line([(cx, 5), (cx + side * 6, -1)], width=2), d.line([(cx + side * 3, 2), (cx + side * 9, 3)], width=2),
                                             d.line([(cx + side * 5, 0), (cx + side * 4, -4)], width=1)), 'bone', round_=0.2)
    # living bow
    s.flat(lambda d: d.arc([32, 7, 53, 59], 285, 75, width=3), '5c4228')
    s.flat(lambda d: d.arc([33, 8, 52, 58], 285, 75, width=1), '8a6640')
    for y in (14, 24, 40, 50): s.flat(lambda d, y=y: d.ellipse([48, y, 52, y + 3]), '6f9a40')                         # leaves on the bow
    s.flat(lambda d: d.line([(45, 10), (45, 56)]), 'd8d6e6')
    s.part(lambda d: d.polygon([(34, 28), (45, 32), (44, 36), (33, 33)]), 'leather')
    s.part(lambda d: d.ellipse([43, 31, 48, 36]), 'skin')
    s.flat(lambda d: d.line([(30, 33), (56, 33)]), 'b89262')
    s.flat(lambda d: d.polygon([(56, 31), (60, 33), (56, 35)]), 'b3d05a'); s.glow(57, 33, 'b3d05a', 4, 0.5)        # thorn arrowhead
    return s.finish(rim='b3d05a', rim_side=-1)


def berserker_c():
    """Bear Jarl: a whole bear pelt worn as a hood (snout, ears and teeth), iron bracers, and two hand axes."""
    s = Spr()
    s.part(lambda d: d.polygon([(16, 14), (42, 14), (46, 44), (12, 46)]), BEAR, round_=0.8)                         # bear pelt cloak
    s.part(lambda d: (d.rectangle([23, 42, 29, 57]), d.rectangle([32, 42, 38, 57])), 'leather')
    s.part(lambda d: (d.polygon([(21, 55), (30, 55), (31, 62), (20, 62)]), d.polygon([(32, 55), (38, 55), (41, 62), (32, 62)])), BEAR)
    s.part(lambda d: d.polygon([(21, 38), (40, 38), (42, 50), (19, 50)]), 'iron', round_=0.3)                       # mail skirt
    s.flat(lambda d: [d.line([(20 + k * 3, 42), (20 + k * 3, 49)]) for k in range(8)], '352a45')
    s.part(lambda d: d.rounded_rectangle([22, 24, 39, 40], 5), 'skin', round_=0.8)                                  # bare chest
    s.flat(lambda d: (d.line([(25, 28), (36, 36)], width=2)), 'leather')
    s.flat(lambda d: (d.line([(26, 33), (29, 30)]), d.line([(32, 37), (35, 34)])), '6f9be0')                       # blue woad
    s.part(lambda d: d.polygon([(19, 3), (39, 2), (44, 9), (41, 13), (21, 13), (17, 9)]), BEAR, round_=0.9)       # bear head on top
    s.part(lambda d: (d.ellipse([18, 0, 24, 6]), d.ellipse([33, -1, 39, 5])), BEAR)                               # ears
    s.flat(lambda d: (d.ellipse([20, 1, 22, 3]), d.ellipse([35, 0, 37, 2])), '8a6640')
    s.part(lambda d: d.polygon([(39, 4), (50, 6), (51, 11), (41, 12)]), BEAR, round_=0.7)                         # snout
    s.flat(lambda d: d.ellipse([48, 5, 51, 8]), '0b0910')
    s.dot(37, 6, 'ffb85a'); s.glow(37, 6, 'ffb85a', 3, 0.5)                                                         # pelt eye
    s.flat(lambda d: [d.polygon([(x, 12), (x + 1, 15), (x + 2, 12)]) for x in (24, 29, 34, 39, 45)], 'fff6d8')    # upper fangs
    s.part(lambda d: d.ellipse([25, 12, 37, 24]), 'skin')                                                           # face below the jaw
    s.dots([(31, 17), (34, 17)], '141019')
    s.flat(lambda d: (d.line([(30, 15), (32, 16)]), d.line([(34, 16), (36, 15)])), '3a2a18')                       # brows
    s.part(lambda d: d.polygon([(26, 21), (37, 21), (35, 30), (31, 33), (28, 29)]), 'hair')                         # braided beard
    s.flat(lambda d: d.line([(31, 25), (31, 32)]), 'ffb85a')
    # twin axes
    s.part(lambda d: (d.ellipse([39, 28, 45, 34]), d.ellipse([17, 30, 23, 36])), 'skin')
    s.part(lambda d: (d.rectangle([38, 30, 46, 33]), d.rectangle([16, 32, 24, 35])), 'iron', round_=0)              # bracers
    s.part(lambda d: d.line([(43, 32), (52, 14)], width=2), 'wood')
    s.part(lambda d: d.polygon([(49, 12), (58, 10), (58, 20), (52, 19)]), 'metal', round_=0.3)
    s.part(lambda d: d.line([(19, 34), (8, 20)], width=2), 'wood')
    s.part(lambda d: d.polygon([(4, 14), (11, 18), (8, 24), (2, 22)]), 'metal', round_=0.3)
    s.flat(lambda d: (d.line([(58, 11), (58, 19)]), d.line([(2, 15), (2, 22)])), 'f4f2fa')
    return s.finish(rim='ffb85a', rim_side=-1)


def hexer_c():
    """Grave Witch: a half-skull mask, a ragged cloak of black feathers, a raven on her shoulder and a candle-lit bone wand."""
    s = Spr()
    s.part(lambda d: d.polygon([(20, 18), (38, 18), (48, 62), (10, 62)]), FEATHER, round_=0.6)                       # feather cloak
    for k in range(9):                                                                                                # ragged feather hem
        x = 11 + k * 4
        s.flat(lambda d, x=x: d.polygon([(x, 58), (x + 4, 58), (x + 1, 63)]), '141019')
    for x, y in ((16, 40), (22, 50), (36, 46), (40, 56), (28, 36)): s.flat(lambda d, x=x, y=y: d.line([(x, y), (x + 3, y + 4)]), '554a6d')
    s.part(lambda d: d.polygon([(25, 24), (35, 24), (36, 46), (24, 46)]), ['2a1414', '6a2848', 'a8406a', 'd88aa8'])  # crimson bodice
    s.flat(lambda d: [d.line([(26, 28 + k * 4), (34, 28 + k * 4)]) for k in range(4)], '141019')                    # lacing
    s.part(lambda d: d.ellipse([24, 8, 36, 22]), 'pale')                                                            # face
    s.part(lambda d: d.polygon([(30, 8), (37, 9), (37, 19), (33, 22), (30, 20)]), 'bone', round_=0.7)               # half-skull mask
    s.flat(lambda d: d.ellipse([32, 12, 35, 15]), '0b0910'); s.dot(33, 13, 'a8406a'); s.glow(33, 13, 'a8406a', 3, 0.7)
    s.flat(lambda d: [d.line([(31 + k * 2, 19), (31 + k * 2, 21)]) for k in range(3)], '141019')                     # skull teeth
    s.dot(27, 14, '141019')
    s.part(lambda d: d.polygon([(21, 4), (36, 3), (40, 12), (36, 9), (30, 6), (24, 10), (22, 24), (19, 20)]), ['0b0910', '141019', '352a45', '554a6d'])  # black hair
    # raven on the back shoulder
    s.part(lambda d: d.ellipse([12, 14, 22, 22]), FEATHER, round_=0.8)
    s.part(lambda d: d.ellipse([16, 10, 22, 16]), FEATHER)
    s.flat(lambda d: d.polygon([(22, 12), (26, 13), (22, 14)]), 'b89262'); s.dot(20, 12, 'ffb85a')
    s.flat(lambda d: d.polygon([(12, 18), (6, 22), (12, 21)]), '141019')
    # bone wand with a candle
    s.part(lambda d: d.polygon([(35, 26), (45, 30), (44, 35), (34, 33)]), FEATHER)
    s.part(lambda d: d.ellipse([43, 28, 48, 33]), 'pale')
    s.part(lambda d: d.line([(46, 30), (52, 14)], width=2), 'bone')
    s.part(lambda d: d.rectangle([50, 9, 53, 14]), 'cream', round_=0)
    s.flat(lambda d: d.polygon([(51, 5), (53, 8), (51, 9), (50, 8)]), 'ffe09a'); s.glow(51, 7, 'ffb85a', 5, 0.7)
    return s.finish(rim='a8406a', rim_side=1)


def alchemist_c():
    """Clockwork Tinkerer: brass goggles, a leather apron, a mechanical arm and a backpack of bubbling tanks and pipes."""
    s = Spr()
    s.part(lambda d: d.rounded_rectangle([10, 18, 22, 44], 3), BRASS, round_=0.7)                                    # backpack tank
    s.part(lambda d: d.rounded_rectangle([12, 21, 19, 34], 2), ['233a22', '6f9a40', 'b3d05a', 'e8f5a0'], round_=1.0)  # glass window
    s.glow(15, 27, 'b3d05a', 4, 0.5)
    s.flat(lambda d: d.line([(16, 18), (16, 10), (24, 10)], width=2), '8a6640')                                     # pipe
    s.dots([(15, 7), (17, 5), (14, 3)], 'b0aec8')                                                                  # steam
    s.part(lambda d: (d.rectangle([24, 46, 29, 57]), d.rectangle([31, 46, 36, 57])), 'leather')
    s.part(lambda d: (d.polygon([(22, 55), (30, 55), (31, 62), (21, 62)]), d.polygon([(31, 55), (38, 55), (40, 62), (31, 62)])), BRASS)
    s.part(lambda d: d.polygon([(21, 24), (38, 24), (40, 48), (19, 48)]), ['1d3340', '2a5560', '3f8a8a', '6fd0c0'])  # teal shirt
    s.part(lambda d: d.polygon([(24, 30), (36, 30), (38, 52), (22, 52)]), 'leather', round_=0.4)                    # apron
    s.flat(lambda d: d.rectangle([27, 38, 33, 42]), '8a6640')                                                       # pocket
    s.dots([(28, 37), (30, 36), (32, 37)], 'd8d6e6')                                                               # tools
    s.part(lambda d: d.ellipse([24, 9, 36, 23]), 'skin')
    s.part(lambda d: d.polygon([(22, 6), (37, 6), (37, 11), (24, 12), (22, 20)]), ['3a2a18', '5c4228', '8a6640', 'b89262'])   # messy hair
    s.part(lambda d: (d.ellipse([27, 9, 32, 14]), d.ellipse([32, 9, 37, 14])), BRASS, round_=0.9)                   # goggles on the brow
    s.flat(lambda d: (d.ellipse([28, 10, 31, 13]), d.ellipse([33, 10, 36, 13])), '6fd0c0')
    s.dots([(31, 17), (34, 17)], '141019'); s.flat(lambda d: d.line([(31, 20), (34, 20)]), '74291f')
    s.part(lambda d: d.polygon([(25, 20), (36, 21), (35, 24), (26, 24)]), 'hair')                                  # stubble
    # mechanical arm holding a smoking flask
    s.part(lambda d: d.polygon([(35, 27), (43, 30), (42, 35), (34, 33)]), BRASS, round_=0.6)
    s.flat(lambda d: (d.line([(37, 29), (37, 33)]), d.line([(40, 30), (40, 34)])), '3a2a18')
    s.part(lambda d: d.polygon([(42, 30), (46, 28), (48, 32), (44, 35)]), BRASS)
    s.part(lambda d: d.polygon([(44, 20), (48, 20), (51, 30), (41, 30)]), ['233a22', 'a8406a', 'd88aa8', 'f4f2fa'], round_=0.9)  # flask
    s.flat(lambda d: d.rectangle([45, 17, 47, 20]), 'b89262'); s.glow(46, 26, 'd88aa8', 5, 0.5)
    return s.finish(rim='ffd76b', rim_side=-1, rim_strength=0.45)


def spellblade_c():
    """Dragon Knight: red dragon-scale armor, a horned helm with a dragon crest, a crimson cape and a flaming greatsword."""
    s = Spr()
    s.part(lambda d: d.polygon([(20, 22), (34, 22), (40, 61), (12, 61)]), ['2a1414', '74291f', 'a63d25', 'd85f2a'], round_=0.3)  # cape
    s.part(lambda d: (d.rectangle([24, 44, 29, 57]), d.rectangle([31, 44, 36, 57])), SCALE)
    s.part(lambda d: (d.polygon([(22, 56), (30, 56), (31, 62), (21, 62)]), d.polygon([(31, 56), (38, 56), (40, 62), (31, 62)])), BLACKIRON)
    s.part(lambda d: d.polygon([(20, 24), (39, 24), (41, 46), (18, 46)]), SCALE, round_=0.7)                        # scale cuirass
    for y in range(27, 45, 4):
        for x in range(21 + (y // 4) % 2 * 2, 39, 4): s.flat(lambda d, x=x, y=y: d.arc([x, y, x + 4, y + 4], 0, 180), '2a1414')  # scales
    s.part(lambda d: d.rectangle([18, 42, 41, 45]), 'gold', round_=0)
    s.part(lambda d: (d.polygon([(15, 20), (26, 20), (25, 28), (13, 27)]), d.polygon([(34, 20), (44, 20), (46, 27), (34, 28)])), SCALE)  # pauldrons
    s.part(lambda d: d.rounded_rectangle([23, 5, 38, 22], 4), SCALE, round_=0.8)                                    # dragon helm
    s.flat(lambda d: d.polygon([(30, 12), (39, 11), (38, 17), (30, 16)]), '0b0910')
    s.dots([(34, 14), (37, 14)], 'ffe09a'); s.glow(35.5, 14, 'ffb85a', 3, 0.6)
    s.part(lambda d: (d.polygon([(24, 8), (14, 2), (20, 10)]), d.polygon([(26, 6), (20, 0), (25, 6)])), 'bone')    # swept horns
    s.part(lambda d: d.polygon([(29, 5), (33, 0), (37, 5)]), 'gold')                                               # crest
    s.part(lambda d: d.polygon([(38, 14), (44, 16), (38, 19)]), SCALE)                                              # snout guard
    # flaming greatsword raised
    s.part(lambda d: d.line([(36, 28), (42, 22)], width=4), SCALE)
    s.part(lambda d: d.polygon([(40, 22), (44, 18), (60, 1), (62, 3), (46, 22)]), ['74291f', 'd85f2a', 'f28a3a', 'fff6d8'], round_=0.4)
    s.flat(lambda d: d.line([(45, 19), (60, 3)]), 'fff6d8')
    for x, y in ((52, 6), (56, 3), (49, 11), (58, 8)): s.flat(lambda d, x=x, y=y: d.polygon([(x, y), (x + 2, y - 4), (x + 3, y + 1)]), 'ffb85a')
    s.glow(54, 8, 'ffe09a', 8, 0.5)
    s.part(lambda d: d.rectangle([37, 22, 47, 24]), 'gold', round_=0)
    return s.finish(rim='f28a3a', rim_side=1)


def ember_wyrm():
    """The Ember Wyrm (secret boss): an obsidian dragon cracked with molten light, coiled, wings half spread, head raised."""
    s = Spr(80, 80)
    OBS = ['0b0910', '1d1726', '352a45', '554a6d']
    MOLTEN = 'f28a3a'
    s.part(lambda d: d.polygon([(10, 30), (2, 6), (14, 14), (22, 4), (28, 18), (34, 10), (36, 34)]), ['141019', '2a1414', '74291f', 'a63d25'], round_=0.4)  # back wing
    s.flat(lambda d: (d.line([(10, 30), (14, 14)]), d.line([(20, 30), (22, 4)]), d.line([(30, 32), (34, 10)])), '0b0910')
    s.part(lambda d: d.polygon([(4, 70), (16, 60), (34, 64), (52, 72), (70, 74), (78, 79), (40, 79), (8, 78)]), OBS, round_=0.7)   # coiled tail
    s.part(lambda d: d.ellipse([14, 36, 58, 76]), OBS, round_=0.9)                                                            # body
    s.part(lambda d: d.polygon([(26, 46), (46, 44), (50, 72), (24, 72)]), ['5c4228', 'a63d25', 'd85f2a', 'f28a3a'], round_=0.8)  # glowing belly plates
    s.flat(lambda d: [d.line([(26, y), (48, y)]) for y in range(50, 72, 5)], '74291f')
    for pts in (((18, 50), (22, 56), (19, 62)), ((52, 46), (55, 54)), ((30, 40), (36, 43))):                                  # molten cracks
        s.flat(lambda d, pts=pts: d.line(list(pts)), MOLTEN)
    s.glow(36, 58, 'ffb85a', 12, 0.35)
    s.part(lambda d: (d.polygon([(20, 70), (28, 70), (30, 79), (16, 79)]), d.polygon([(44, 70), (52, 70), (56, 79), (42, 79)])), OBS)  # claws
    s.flat(lambda d: [d.polygon([(x, 77), (x + 2, 79), (x - 1, 79)]) for x in (17, 21, 25, 44, 48, 52)], 'e8dcc0')
    s.part(lambda d: d.polygon([(40, 44), (48, 22), (56, 14), (62, 18), (54, 30), (50, 48)]), OBS, round_=0.8)               # long neck
    for k in range(5): s.flat(lambda d, k=k: d.polygon([(44 + k * 3, 36 - k * 5), (42 + k * 3, 30 - k * 5), (47 + k * 3, 33 - k * 5)]), '74291f')  # neck spines
    s.part(lambda d: d.polygon([(52, 8), (66, 6), (78, 12), (78, 18), (66, 22), (54, 20)]), OBS, round_=0.7)                 # head
    s.flat(lambda d: d.polygon([(66, 17), (78, 16), (78, 19), (67, 21)]), '0b0910')                                          # jaw line
    s.flat(lambda d: d.polygon([(68, 19), (78, 18), (79, 22), (70, 23)]), MOLTEN); s.glow(76, 20, 'ffe09a', 6, 0.6)       # fire in the maw
    s.dot(66, 11, 'ffe09a'); s.glow(66, 11, 'ffb85a', 4, 0.8)                                                                # burning eye
    s.part(lambda d: (d.polygon([(56, 8), (48, 0), (58, 5)]), d.polygon([(60, 7), (56, -2), (63, 6)])), 'bone', round_=0.2)  # horns
    s.part(lambda d: d.polygon([(46, 30), (60, 26), (74, 32), (70, 40), (58, 38)]), ['141019', '2a1414', '74291f', 'a63d25'], round_=0.4)  # front wing (folded)
    for x, y in ((70, 30), (76, 24), (62, 4)): s.dot(x, y, 'ffb85a')                                                         # embers
    return s.finish(rim='f28a3a', rim_side=1)


COSTUMES = {'vanguard_c': vanguard_c, 'arcanist_c': arcanist_c, 'warden_c': warden_c, 'shadowblade_c': shadowblade_c,
            'ranger_c': ranger_c, 'berserker_c': berserker_c, 'hexer_c': hexer_c, 'alchemist_c': alchemist_c,
            'spellblade_c': spellblade_c}
U.HEROES.update(COSTUMES)

if __name__ == '__main__':
    from PIL import Image
    import ui_v2
    for name, fn in COSTUMES.items():
        fn().image().save(f'Units/{name}.png')
        ui_v2.portrait(name).save(f'Portraits/{name}.png')
    ember_wyrm().image(flip=True).save('Units/ember_wyrm.png')
    print('costumes:', ', '.join(COSTUMES))
