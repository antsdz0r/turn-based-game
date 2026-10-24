"""Redesigns of the monsters that read as goofy (round, smiling, blocky) - meaner silhouettes, gaunter anatomy,
sharper shapes, glowing eyes - plus the new regions' monsters and the two new heroes.
Importing this module patches units_v2.HEROES / ENEMIES, so ui_v2.py renders the new versions."""
from style_kit import Spr, R, hx
import units_v2 as U
from units_v2 import ICE, FROST, STORMY, SPARK

FEATHER = ['0b0910', '1d1726', '352a45', '6b6386']
ASH = ['141019', '282033', '554a6d', '8a86a6']
DROWN = ['141019', '1d3340', '2a5560', '6b9a9a']
SICK = ['141019', '233a22', '3d6230', '8a9a40']
BILE = ['233a22', '6f9a40', 'b3d05a', 'e8f5a0']
DEMON = ['141019', '2a1414', '74291f', 'a63d25']
NAVY = ['0b0910', '141019', '1d3340', '2a5560']
FLESHD = ['141019', '2a1414', '4a1e1e', '8f5a48']


# ═══════════════════════════ Redesigns ═══════════════════════════

def harpy():
    """Gaunt bird-woman with ragged storm wings raised, talons and burning eyes."""
    s = Spr()
    s.part(lambda d: d.polygon([(24, 26), (4, 4), (9, 14), (1, 17), (9, 23), (2, 30), (13, 31), (22, 36)]), FEATHER)   # back wing
    s.flat(lambda d: d.line([(5, 6), (22, 32)]), '433758')
    s.part(lambda d: (d.line([(28, 46), (25, 57)], width=2), d.line([(33, 46), (36, 57)], width=2)), 'bone')            # bird legs
    s.part(lambda d: (d.polygon([(21, 57), (28, 56), (27, 62), (20, 62)]), d.polygon([(34, 56), (41, 57), (42, 62), (35, 62)])), 'iron')
    s.dots([(20, 62), (23, 62), (42, 62), (39, 62)], 'd8d6e6')                                                           # talons
    s.part(lambda d: d.polygon([(23, 36), (38, 36), (37, 48), (24, 48)]), FEATHER)                                       # feather skirt
    for x in (24, 28, 32, 36): s.flat(lambda d, x=x: d.polygon([(x, 46), (x + 2, 46), (x + 1, 51)]), '1d1726')
    s.part(lambda d: d.polygon([(26, 21), (36, 21), (35, 38), (27, 38)]), 'pale')                                        # gaunt torso
    s.flat(lambda d: (d.line([(28, 28), (34, 28)]), d.line([(28, 31), (34, 31)])), '8a86a6')                             # ribs
    s.part(lambda d: d.polygon([(22, 8), (14, 16), (16, 24), (20, 18), (19, 28), (25, 20)]), FEATHER)                    # wild hair streaming back
    s.part(lambda d: d.polygon([(24, 6), (37, 5), (38, 10), (33, 9), (26, 12)]), FEATHER)                              # crest of feathers
    s.part(lambda d: d.polygon([(27, 10), (37, 10), (36, 20), (33, 23), (28, 20)]), ['433758', '8a86a6', 'b0aec8', 'd8d6e6'])   # gaunt face
    s.flat(lambda d: (d.line([(30, 15), (32, 15)]), d.line([(34, 15), (36, 15)])), 'ffe09a')                            # slanted burning eyes
    s.glow(33, 15, 'ffb85a', 4, 0.6)
    s.flat(lambda d: d.polygon([(32, 18), (37, 18), (35, 21)]), '0b0910')                                                # open shriek
    s.dots([(33, 18), (35, 18)], 'f4f2fa')
    s.part(lambda d: d.polygon([(34, 24), (59, 2), (55, 12), (63, 13), (55, 20), (61, 27), (48, 29), (37, 33)]), FEATHER)   # front wing
    s.flat(lambda d: (d.line([(58, 4), (38, 30)]), d.line([(62, 14), (44, 30)])), 'd8d6e6')
    return s.finish(rim='d8d6e6', rim_side=1, rim_strength=0.45)


def imp():
    """Wiry ember demon: hunched, long arms, curved horns, tattered wings, a toothy leer."""
    s = Spr()
    s.part(lambda d: d.polygon([(22, 26), (6, 12), (10, 22), (3, 24), (12, 30), (8, 36), (20, 34)]), DEMON)               # tattered wing
    s.part(lambda d: d.line([(22, 46), (12, 54), (6, 50), (8, 46)], width=2), DEMON)                                     # tail
    s.part(lambda d: d.polygon([(4, 44), (10, 46), (6, 50)]), 'ember')
    s.part(lambda d: (d.line([(25, 46), (20, 56), (24, 62)], width=3), d.line([(33, 46), (38, 56), (35, 62)], width=3)), DEMON)   # digitigrade legs
    s.part(lambda d: d.polygon([(22, 28), (36, 26), (38, 40), (34, 48), (24, 48), (21, 40)]), DEMON, round_=0.6)         # hunched body
    s.flat(lambda d: (d.line([(25, 34), (28, 38)]), d.line([(33, 33), (31, 39)])), 'f28a3a')                               # glowing cracks
    s.part(lambda d: d.ellipse([27, 12, 41, 28]), DEMON)                                                                   # head thrust forward
    s.part(lambda d: d.polygon([(28, 14), (22, 8), (24, 2), (26, 8), (31, 12)]), 'bone')                                   # curved horns
    s.part(lambda d: d.polygon([(37, 13), (40, 6), (45, 3), (42, 9), (40, 15)]), 'bone')
    s.flat(lambda d: (d.line([(32, 18), (34, 19)]), d.line([(37, 18), (39, 17)])), 'ffe09a'); s.glow(36, 18, 'ffb85a', 4, 0.6)
    s.flat(lambda d: d.polygon([(31, 22), (41, 21), (39, 25), (33, 25)]), '0b0910')                                      # leer
    s.dots([(32, 22), (34, 22), (36, 22), (38, 22), (40, 21)], 'fff6d8')
    s.part(lambda d: d.line([(36, 30), (48, 38), (52, 32)], width=2), DEMON)                                             # long arm
    for k in range(3): s.flat(lambda d, k=k: d.line([(52, 32), (55 + k, 28 + k * 2)]), 'bone')                          # claws
    s.part(lambda d: d.ellipse([52, 18, 62, 28]), 'fire', round_=0.9); s.glow(57, 23, 'fff6d8', 6, 0.7)                 # fireball
    return s.finish(rim='ffb85a', rim_side=1)


def hound():
    """Skeletal hound: lean, ribcage showing, spiked spine, skull muzzle with green fire eyes."""
    s = Spr()
    s.part(lambda d: d.line([(10, 34), (4, 28), (2, 22)], width=2), 'bone')                                               # tail bones
    for x0, x1 in ((14, 10), (20, 22), (40, 38), (46, 50)):                                                              # jointed legs
        s.part(lambda d, x0=x0, x1=x1: d.line([(x0, 40), (x1, 50), (x0 + (2 if x1 > x0 else -2), 61)], width=3), ASH)
    s.part(lambda d: d.polygon([(10, 30), (44, 26), (48, 34), (40, 42), (14, 42)]), ASH, round_=0.5)                     # torso hide
    for x in range(18, 40, 4): s.flat(lambda d, x=x: d.line([(x, 31), (x + 1, 40)]), 'd8d6e6')                          # ribcage
    for x in range(12, 46, 5): s.flat(lambda d, x=x: d.polygon([(x, 30), (x + 2, 22), (x + 4, 29)]), 'b0aec8')          # spine spikes
    s.part(lambda d: d.polygon([(42, 24), (56, 24), (63, 30), (60, 36), (46, 36)]), 'bone')                               # skull muzzle
    s.flat(lambda d: d.polygon([(52, 32), (63, 31), (61, 36), (52, 36)]), '0b0910')                                      # open jaw
    s.dots([(54, 32), (57, 32), (60, 32), (55, 35), (58, 35)], 'f4f2fa')
    s.flat(lambda d: d.ellipse([47, 26, 51, 30]), '0b0910'); s.dot(49, 28, 'b3d05a'); s.glow(49, 28, 'b3d05a', 4, 0.8)
    s.part(lambda d: d.polygon([(44, 24), (46, 16), (49, 23)]), ASH)                                                     # ear
    return s.finish(rim='b3d05a', rim_side=1, rim_strength=0.35)


def rat():
    """A pack of three plague rats: mangy, hunched, red-eyed, bigger than before."""
    s = Spr()
    def one(ox, oy, sc):
        s.part(lambda d: d.line([(ox + 2, oy + 6), (ox - 8 * sc, oy + 2), (ox - 12 * sc, oy - 4)], width=1), 'flesh')    # tail
        s.part(lambda d: d.ellipse([ox, oy - 8 * sc, ox + 20 * sc, oy + 6]), ['0b0910', '1d1726', '3a2a18', '5c4228'], round_=0.7)
        s.part(lambda d: d.polygon([(ox + 16 * sc, oy - 6 * sc), (ox + 27 * sc, oy + 1), (ox + 16 * sc, oy + 4)]), ['0b0910', '1d1726', '3a2a18', '5c4228'])
        s.flat(lambda d: d.point((ox + 21 * sc, oy - 2 * sc)), 'ff3b3b'); s.glow(ox + 21 * sc, oy - 2 * sc, 'ff6b6b', 2.5, 0.6)
        s.part(lambda d: d.ellipse([ox + 14 * sc, oy - 10 * sc, ox + 18 * sc, oy - 6 * sc]), 'flesh')                   # ear
        s.flat(lambda d: (d.line([(ox + 6, oy + 5), (ox + 6, oy + 8)]), d.line([(ox + 15 * sc, oy + 4), (ox + 16 * sc, oy + 8)])), '8f5a48')
        for k in range(4): s.dot(int(ox + 4 + k * 4 * sc), int(oy - 7 * sc), '554a6d')                                   # mangy fur tufts
    one(4, 40, 1.0); one(30, 52, 1.1); one(8, 56, 0.9)
    return s.finish(rim='c98a6b', rim_side=1, rim_strength=0.35)


def leech():
    """Bog leech rearing up: glistening segmented body ending in a round maw ringed with teeth."""
    s = Spr()
    segs = [(6, 52, 14), (14, 48, 15), (22, 42, 15), (30, 34, 14), (36, 26, 13)]
    for x, y, r in segs:
        s.part(lambda d, x=x, y=y, r=r: d.ellipse([x, y - r // 2, x + r, y + r // 2]), FLESHD, round_=0.9)
    for x, y, r in segs: s.flat(lambda d, x=x, y=y, r=r: d.arc([x + 2, y - r // 2 + 2, x + r - 2, y + r // 2 - 2], 200, 300), 'c98a6b')   # slick highlight
    s.part(lambda d: d.ellipse([42, 10, 60, 28]), FLESHD, round_=0.6)                                                     # head
    s.flat(lambda d: d.ellipse([46, 14, 58, 26]), '2a1414')                                                               # maw
    for k in range(10):
        import math
        a = k / 10 * 2 * math.pi
        s.dot(int(52 + 5 * math.cos(a)), int(20 + 5 * math.sin(a)), 'fff6d8')                                             # ring of teeth
    s.flat(lambda d: d.ellipse([50, 18, 54, 22]), '0b0910')
    for x in (12, 26, 40): s.flat(lambda d, x=x: d.line([(x, 54), (x, 60)]), '6f9a40')                                  # slime drips
    s.part(lambda d: d.ellipse([0, 56, 30, 63]), SICK, round_=0.2)                                                        # bog water
    return s.finish(rim='c98a6b', rim_side=1, rim_strength=0.4)


def thrall():
    """Drowned thrall: bloated, hunched, waterlogged, seaweed hanging off a hooded corpse, chained wrist."""
    s = Spr()
    s.part(lambda d: (d.rectangle([22, 44, 28, 58]), d.rectangle([32, 44, 38, 58])), DROWN)
    s.part(lambda d: (d.polygon([(20, 57), (29, 57), (30, 62), (19, 62)]), d.polygon([(31, 57), (40, 57), (41, 62), (31, 62)])), NAVY)
    s.part(lambda d: d.polygon([(18, 26), (42, 24), (46, 46), (14, 48)]), DROWN, round_=0.8)                             # bloated torso
    s.part(lambda d: d.polygon([(16, 28), (30, 24), (28, 48), (14, 48)]), NAVY, round_=0.3)                              # rag cloak
    for x in (16, 22, 40, 44): s.flat(lambda d, x=x: d.line([(x, 30), (x - 1, 52)]), '3d6230')                         # seaweed
    s.part(lambda d: d.polygon([(22, 10), (38, 10), (42, 26), (20, 28)]), NAVY, round_=0.6)                              # hood, hunched low
    s.part(lambda d: d.ellipse([28, 14, 40, 26]), DROWN)                                                                   # swollen face
    s.flat(lambda d: (d.ellipse([31, 17, 34, 20]), d.ellipse([36, 17, 39, 20])), 'b8f5e4'); s.glow(35, 18, 'b8f5e4', 4, 0.6)   # dead pale eyes
    s.flat(lambda d: d.line([(32, 23), (38, 24)]), '141019')
    s.part(lambda d: d.line([(42, 30), (50, 42)], width=4), DROWN)                                                       # heavy arm
    s.part(lambda d: d.ellipse([46, 40, 54, 48]), DROWN)
    s.part(lambda d: d.line([(50, 48), (54, 58), (50, 62)], width=1), 'iron')                                             # broken chain
    s.dots([(53, 52), (52, 56)], '8a86a6')
    return s.finish(rim='6fd0c0', rim_side=1, rim_strength=0.4)


def blight():
    """Blightspitter: bloated plague toad-thing, pustules, a gaping mouth dripping bile."""
    s = Spr()
    s.part(lambda d: (d.polygon([(12, 46), (20, 46), (16, 62), (8, 62)]), d.polygon([(40, 46), (48, 46), (52, 62), (44, 62)])), SICK)   # squat legs
    s.part(lambda d: d.ellipse([8, 18, 52, 56]), SICK, round_=0.9)                                                        # bloated body
    for x, y, r in ((16, 26, 4), (40, 22, 5), (22, 44, 3), (44, 40, 4), (30, 20, 3)):
        s.part(lambda d, x=x, y=y, r=r: d.ellipse([x, y, x + r * 2, y + r * 2]), BILE, round_=1.0)                       # pustules
    s.flat(lambda d: d.polygon([(30, 30), (54, 28), (52, 40), (32, 40)]), '0b0910')                                      # gaping mouth
    s.dots([(34, 30), (38, 29), (42, 29), (46, 29), (50, 29)], 'e8f5a0')
    s.flat(lambda d: (d.line([(40, 40), (40, 50)], width=2), d.line([(48, 40), (49, 47)])), 'b3d05a')                    # bile drips
    s.glow(42, 35, 'b3d05a', 7, 0.35)
    s.flat(lambda d: (d.ellipse([34, 21, 39, 26]), d.ellipse([43, 20, 48, 25])), 'e8f5a0')
    s.dots([(36, 23), (45, 22)], '0b0910')
    return s.finish(rim='b3d05a', rim_side=1, rim_strength=0.4)


def siren():
    """Sea witch: long dark hair, finned arms raised in song, an eel tail coiling beneath."""
    s = Spr()
    s.part(lambda d: d.line([(28, 46), (18, 54), (26, 60), (44, 58), (54, 50)], width=7), DROWN)                       # coiled eel tail
    s.flat(lambda d: d.line([(18, 54), (26, 60), (44, 58)], width=1), '6fd0c0')
    s.part(lambda d: d.polygon([(54, 50), (62, 42), (60, 54)]), ['1d3340', '2a5560', '6fd0c0', 'b8f5e4'])               # tail fin
    s.part(lambda d: d.polygon([(24, 24), (36, 24), (34, 48), (26, 48)]), ['1d3340', '2a5560', '6b9a9a', 'b0aec8'])     # body
    for y in (36, 40, 44): s.flat(lambda d, y=y: d.line([(26, y), (34, y)]), '1d3340')                                  # scales
    s.part(lambda d: d.polygon([(22, 8), (34, 6), (36, 12), (30, 40), (20, 34)]), ['0b0910', '141019', '1d3340', '2a5560'])   # long dark hair
    s.part(lambda d: d.ellipse([26, 9, 37, 22]), ['2a5560', '6b9a9a', 'b0aec8', 'd8d6e6'])                               # face
    s.flat(lambda d: (d.line([(30, 14), (32, 14)]), d.line([(34, 14), (36, 14)])), 'b8f5e4'); s.glow(33, 14, '6fd0c0', 4, 0.6)
    s.flat(lambda d: d.ellipse([32, 17, 36, 21]), '0b0910')                                                              # singing mouth
    s.part(lambda d: d.line([(34, 26), (44, 14), (48, 6)], width=3), ['2a5560', '6b9a9a', 'b0aec8', 'd8d6e6'])        # raised arm
    s.part(lambda d: d.polygon([(40, 20), (50, 12), (46, 22)]), ['1d3340', '2a5560', '6fd0c0', 'b8f5e4'])               # arm fin
    for k, (x, y) in enumerate(((50, 4), (54, 10), (56, 2))): s.flat(lambda d, x=x, y=y: d.arc([x, y, x + 4, y + 4], 0, 180), '6fd0c0')   # song ripples
    return s.finish(rim='6fd0c0', rim_side=1, rim_strength=0.45)


def lich():
    """Tall gaunt lich-queen: deep hooded robes edged in frost, skull face with cold fire, a floating crystal staff."""
    s = Spr(80, 80)
    s.part(lambda d: d.polygon([(28, 22), (50, 22), (60, 79), (18, 79)]), NAVY, round_=0.5)                              # deep robes
    s.part(lambda d: d.polygon([(34, 30), (44, 30), (48, 79), (30, 79)]), ['141019', '1d3340', '2a5560', '3f8a8a'], round_=0.3)
    s.flat(lambda d: (d.line([(18, 78), (60, 78)], width=2), d.line([(28, 22), (18, 78)]), d.line([(50, 22), (60, 78)])), 'b8f5e4')   # frost trim
    for x, y in ((24, 66), (52, 70), (38, 56)): s.flat(lambda d, x=x, y=y: (d.line([(x - 2, y), (x + 2, y)]), d.line([(x, y - 2), (x, y + 2)])), '6fd0c0')
    s.part(lambda d: d.polygon([(26, 6), (52, 6), (56, 26), (22, 28)]), NAVY, round_=0.7)                                # tall hood
    s.flat(lambda d: d.polygon([(31, 10), (47, 10), (48, 26), (30, 26)]), '0b0910')
    s.part(lambda d: d.polygon([(33, 12), (45, 12), (44, 24), (34, 24)]), 'bone')                                         # gaunt skull
    s.flat(lambda d: (d.rectangle([35, 15, 37, 17]), d.rectangle([41, 15, 43, 17])), '0b0910')
    s.dots([(36, 16), (42, 16)], 'b8f5e4'); s.glow(36, 16, '6fd0c0', 4, 0.8); s.glow(42, 16, '6fd0c0', 4, 0.8)
    s.flat(lambda d: (d.line([(36, 21), (42, 21)]), d.line([(37, 22), (41, 22)])), '554a6d')
    s.part(lambda d: d.polygon([(28, 6), (30, -2), (33, 5), (37, -4), (39, 4), (41, -4), (45, 5), (48, -2), (50, 6)]), FROST)   # ice crown
    s.part(lambda d: d.line([(50, 28), (62, 38)], width=3), NAVY)                                                         # thin arm
    for k in range(3): s.flat(lambda d, k=k: d.line([(62, 38), (66 + k, 36 + k * 2)]), 'bone')                          # bony fingers
    s.part(lambda d: d.line([(70, 12), (68, 74)], width=2), 'iron')                                                      # floating staff
    s.part(lambda d: d.polygon([(64, 0), (76, 0), (78, 10), (70, 18), (62, 10)]), FROST, round_=0.8); s.glow(70, 8, 'fff6d8', 7, 0.6)
    s.part(lambda d: d.line([(28, 28), (16, 44)], width=3), NAVY)
    s.part(lambda d: d.ellipse([8, 40, 18, 50]), FROST, round_=1.0); s.glow(13, 45, 'b8f5e4', 6, 0.5)                    # frost orb
    return s.finish(rim='b8f5e4', rim_side=1)


def colossus():
    """Bone Colossus: a hunched giant of fused bones, ribcage around a grave-fire core, skulls studding its shoulders."""
    s = Spr(80, 80)
    s.part(lambda d: (d.polygon([(22, 56), (32, 56), (30, 74), (18, 74)]), d.polygon([(46, 56), (56, 56), (60, 74), (48, 74)])), ['352a45', '8a86a6', 'b0aec8', 'd8d6e6'])   # legs
    s.part(lambda d: (d.polygon([(14, 72), (32, 72), (34, 79), (12, 79)]), d.polygon([(46, 72), (64, 72), (66, 79), (44, 79)])), ASH)
    s.part(lambda d: d.polygon([(18, 24), (60, 20), (64, 50), (54, 60), (24, 60), (14, 50)]), ['141019', '352a45', '8a86a6', 'd8d6e6'], round_=0.7)   # bone torso, grave-dark
    s.flat(lambda d: d.ellipse([28, 30, 50, 52]), '1d1726')                                                              # ribcage hollow
    for y in (32, 37, 42, 47): s.flat(lambda d, y=y: d.arc([24, y - 6, 54, y + 6], 200, 340), 'fff6d8')               # ribs
    s.part(lambda d: d.ellipse([34, 36, 44, 46]), 'fire', round_=1.0); s.glow(39, 41, 'ffe09a', 9, 0.6)                  # grave-fire core
    s.part(lambda d: d.polygon([(32, 6), (40, 2), (52, 4), (56, 12), (52, 22), (36, 22), (30, 14)]), ['433758', '8a86a6', 'b0aec8', 'd8d6e6'])   # angular skull, low-slung
    s.flat(lambda d: d.line([(32, 9), (54, 8)], width=2), '352a45')                                                      # heavy brow
    s.flat(lambda d: (d.polygon([(36, 10), (42, 11), (41, 15), (36, 14)]), d.polygon([(45, 11), (51, 10), (51, 14), (46, 15)])), '0b0910')
    s.dots([(39, 13), (48, 12)], 'ffb85a'); s.glow(44, 12, 'ffb85a', 7, 0.6)
    s.flat(lambda d: d.polygon([(36, 18), (52, 18), (50, 24), (38, 24)]), '0b0910')                                      # hanging jaw
    s.dots([(38, 18), (41, 18), (44, 18), (47, 18), (50, 18), (40, 23), (46, 23)], 'd8d6e6')
    for x, y in ((14, 18), (58, 14), (8, 28)):                                                                             # skulls in the shoulders
        s.part(lambda d, x=x, y=y: d.ellipse([x, y, x + 10, y + 10]), ['352a45', '8a86a6', 'b0aec8', 'd8d6e6'])
        s.flat(lambda d, x=x, y=y: (d.rectangle([x + 2, y + 3, x + 3, y + 5]), d.rectangle([x + 6, y + 3, x + 7, y + 5])), '0b0910')
    s.part(lambda d: d.polygon([(14, 26), (6, 50), (10, 66), (16, 64), (14, 48), (22, 32)]), ['352a45', '8a86a6', 'b0aec8', 'd8d6e6'])   # long arm to the ground
    s.part(lambda d: d.polygon([(60, 24), (72, 44), (70, 66), (64, 64), (64, 46), (56, 32)]), ['352a45', '8a86a6', 'b0aec8', 'd8d6e6'])
    for x in (8, 12, 64, 68): s.flat(lambda d, x=x: d.line([(x, 66), (x - 1, 72)]), 'fff6d8')                          # finger bones
    return s.finish(rim='ffb85a', rim_side=1)


# ═══════════════════════════ The Rotting Garden ═══════════════════════════

THORN = ['141019', '233a22', '3d6230', '6f9a40']
PETAL = ['4a1e1e', '6a2848', 'a8406a', 'd88aa8']
BARK = ['0b0910', '3a2a18', '5c4228', '8a6640']


def thornling():
    s = Spr()
    s.part(lambda d: (d.line([(24, 44), (20, 62)], width=4), d.line([(36, 44), (40, 62)], width=4)), BARK)               # root legs
    s.part(lambda d: d.polygon([(20, 22), (40, 20), (44, 46), (18, 46)]), BARK, round_=0.6)                              # bark body
    for x, y in ((18, 26), (42, 30), (16, 38), (44, 40), (28, 18), (36, 16)):
        s.flat(lambda d, x=x, y=y: d.polygon([(x, y), (x + 3, y - 4), (x + 4, y + 1)]), '6f9a40')                       # thorns
    s.part(lambda d: d.ellipse([24, 6, 40, 22]), BARK)
    s.flat(lambda d: (d.ellipse([28, 11, 32, 15]), d.ellipse([34, 11, 38, 15])), 'b3d05a'); s.glow(33, 13, 'b3d05a', 4, 0.6)
    s.flat(lambda d: d.line([(29, 18), (37, 18)]), '0b0910')
    s.part(lambda d: d.line([(40, 26), (54, 18), (58, 26)], width=3), THORN)                                             # whip vine
    for k in range(4): s.dot(44 + k * 4, 20 - (k % 2) * 2, 'b3d05a')
    return s.finish(rim='b3d05a', rim_side=1, rim_strength=0.35)


def spore_puffer():
    s = Spr()
    s.part(lambda d: d.line([(30, 40), (30, 62)], width=4), BARK)                                                         # stalk
    s.part(lambda d: d.ellipse([10, 8, 50, 44]), PETAL, round_=0.9)                                                       # swollen cap
    for x, y in ((16, 16), (36, 12), (24, 28), (42, 30), (30, 20)):
        s.part(lambda d, x=x, y=y: d.ellipse([x, y, x + 6, y + 6]), BILE, round_=1.0)                                    # spore sacs
    s.flat(lambda d: d.ellipse([22, 34, 38, 42]), '2a1414')                                                              # gill mouth
    for x in range(24, 38, 3): s.flat(lambda d, x=x: d.line([(x, 34), (x, 41)]), '6a2848')
    for x, y in ((6, 4), (52, 8), (56, 20), (4, 20), (48, 0)): s.dot(x, y, 'e8f5a0')                                    # drifting spores
    s.glow(30, 24, 'b3d05a', 8, 0.25)
    return s.finish(rim='d88aa8', rim_side=1, rim_strength=0.4)


def strangler():
    s = Spr()
    for k, (x0, x1) in enumerate(((14, 8), (22, 18), (38, 42), (46, 52))):
        s.part(lambda d, x0=x0, x1=x1: d.line([(x0, 40), (x1, 62)], width=3), THORN)                                    # rooted tendrils
    s.part(lambda d: d.polygon([(18, 18), (42, 14), (48, 44), (14, 44)]), THORN, round_=0.7)                              # coiled body of vines
    for y in (22, 30, 38): s.flat(lambda d, y=y: d.line([(16, y), (46, y - 4)]), '3d6230')
    s.part(lambda d: d.ellipse([22, 2, 42, 20]), PETAL, round_=0.8)                                                        # flower head
    s.flat(lambda d: d.ellipse([28, 7, 38, 16]), '0b0910')                                                               # maw
    for k in range(6): s.dot(29 + k * 2 - (k > 2) * 6, 8 + (k > 2) * 6, 'fff6d8')
    s.part(lambda d: d.line([(44, 24), (56, 30), (60, 22), (56, 16)], width=3), THORN)                                   # grasping vine
    return s.finish(rim='d88aa8', rim_side=1, rim_strength=0.4)


def stalker():
    s = Spr()
    s.part(lambda d: (d.line([(22, 42), (14, 52), (18, 62)], width=2), d.line([(30, 42), (28, 62)], width=2), d.line([(36, 40), (44, 52), (42, 62)], width=2)), THORN)
    s.part(lambda d: d.polygon([(14, 34), (40, 30), (46, 38), (20, 44)]), THORN, round_=0.6)                             # mantis body
    s.part(lambda d: d.polygon([(38, 30), (46, 12), (50, 14), (44, 32)]), THORN)                                          # thorax up
    s.part(lambda d: d.polygon([(44, 6), (56, 8), (54, 16), (46, 16)]), THORN)                                            # head
    s.flat(lambda d: d.ellipse([50, 8, 55, 13]), 'ff6b6b'); s.glow(52, 10, 'ff6b6b', 4, 0.6)
    s.part(lambda d: d.line([(46, 18), (58, 26), (54, 34)], width=3), ['233a22', '6f9a40', 'b3d05a', 'e8f5a0'])         # scythe arm
    s.part(lambda d: d.line([(44, 20), (36, 4), (30, 10)], width=3), ['233a22', '6f9a40', 'b3d05a', 'e8f5a0'])
    s.part(lambda d: d.polygon([(16, 32), (4, 22), (8, 34)]), ['1d3340', '3f8a8a', '6fd0c0', 'b8f5e4'])                   # gauzy wing
    return s.finish(rim='b3d05a', rim_side=1, rim_strength=0.4)


def briar_witch():
    s = Spr()
    s.part(lambda d: d.polygon([(21, 24), (37, 24), (46, 62), (12, 62)]), THORN, round_=0.5)                              # dress of vines
    for x in (14, 22, 32, 42): s.flat(lambda d, x=x: d.polygon([(x, 62), (x + 3, 54), (x + 6, 62)]), '233a22')
    for x, y in ((18, 40), (36, 34), (28, 50)): s.part(lambda d, x=x, y=y: d.ellipse([x, y, x + 6, y + 6]), PETAL)    # roses
    s.part(lambda d: d.ellipse([24, 8, 36, 22]), ['3d6230', '6f9a40', 'b3d05a', 'e8f5a0'])                                # bark-green skin
    s.part(lambda d: d.polygon([(20, 6), (24, -2), (28, 6), (32, -3), (36, 6), (40, 0), (40, 10), (20, 10)]), BARK)     # antler crown
    s.flat(lambda d: (d.line([(28, 14), (30, 14)]), d.line([(32, 14), (34, 14)])), 'ffe09a'); s.glow(31, 14, 'ffb85a', 4, 0.5)
    s.part(lambda d: d.line([(34, 28), (46, 18)], width=2), THORN)
    s.part(lambda d: d.line([(48, 4), (44, 62)], width=2), BARK)                                                          # thorn staff
    s.part(lambda d: d.ellipse([42, 0, 54, 12]), PETAL, round_=0.9); s.glow(48, 6, 'd88aa8', 6, 0.5)
    return s.finish(rim='d88aa8', rim_side=1)


def seed_pod():
    s = Spr()
    s.part(lambda d: d.ellipse([18, 26, 46, 60]), ['233a22', '3d6230', '6f9a40', 'b3d05a'], round_=0.9)
    for y in (32, 40, 48): s.flat(lambda d, y=y: d.arc([20, y - 8, 44, y + 8], 200, 340), '233a22')
    s.flat(lambda d: d.line([(32, 26), (32, 18), (36, 14)], width=2), '3d6230')
    s.glow(32, 44, 'e8f5a0', 9, 0.35)                                                                                     # something glows inside
    return s.finish(rim='b3d05a', rim_side=1, rim_strength=0.4)


def mother_bloom():
    s = Spr(80, 80)
    for k, (x0, x1) in enumerate(((20, 6), (30, 22), (50, 58), (60, 76))):
        s.part(lambda d, x0=x0, x1=x1: d.line([(x0, 56), (x1, 79)], width=4), BARK)                                     # great roots
    s.part(lambda d: d.polygon([(16, 30), (64, 30), (68, 62), (12, 62)]), THORN, round_=0.7)                              # massive stem body
    for x, y in ((18, 40), (60, 36), (24, 54), (56, 52)): s.flat(lambda d, x=x, y=y: d.polygon([(x, y), (x + 4, y - 6), (x + 6, y + 1)]), 'b3d05a')
    for k in range(7):                                                                                                     # crown of petals
        import math
        a = math.pi * (0.9 + k * 0.2)
        cx, cy = 40 + 20 * math.cos(a), 22 + 14 * math.sin(a)
        s.part(lambda d, cx=cx, cy=cy: d.ellipse([cx - 8, cy - 8, cx + 8, cy + 8]), PETAL, round_=0.8)
    s.part(lambda d: d.ellipse([26, 12, 54, 38]), ['4a1e1e', '6a2848', 'a8406a', 'd88aa8'], round_=0.9)
    s.flat(lambda d: d.ellipse([31, 17, 49, 33]), '0b0910')                                                              # devouring maw
    for k in range(12):
        import math
        a = k / 12 * 2 * math.pi
        s.dot(int(40 + 8 * math.cos(a)), int(25 + 7 * math.sin(a)), 'fff6d8')
    s.glow(40, 25, 'b3d05a', 6, 0.4)
    s.part(lambda d: d.line([(64, 36), (76, 24), (78, 12)], width=3), THORN)                                             # lashing vines
    s.part(lambda d: d.line([(16, 36), (4, 26), (2, 14)], width=3), THORN)
    return s.finish(rim='d88aa8', rim_side=1)


# ═══════════════════════════ The Silent Archive ═══════════════════════════

PARCH = ['5c4228', '8a6640', 'b89262', 'e8dcc0']
INK = ['0b0910', '141019', '1d1726', '352a45']
STONE = ['141019', '352a45', '554a6d', '8a86a6']
BRASS = ['3a2a18', '8a6640', 'ffb85a', 'ffe09a']


def tome():
    s = Spr()
    s.part(lambda d: d.polygon([(10, 22), (32, 28), (32, 50), (10, 44)]), ['2a1414', '4a1e1e', '74291f', 'a63d25'])       # covers, flapping
    s.part(lambda d: d.polygon([(32, 28), (54, 22), (54, 44), (32, 50)]), ['2a1414', '4a1e1e', '74291f', 'a63d25'])
    s.part(lambda d: d.polygon([(13, 24), (32, 30), (32, 46), (13, 41)]), PARCH)                                           # pages
    s.part(lambda d: d.polygon([(32, 30), (51, 24), (51, 41), (32, 46)]), PARCH)
    for y in (31, 35, 39): s.flat(lambda d, y=y: (d.line([(16, y - 3), (29, y + 1)]), d.line([(35, y + 1), (48, y - 3)])), '5c4228')
    s.flat(lambda d: d.ellipse([28, 30, 36, 38]), '0b0910'); s.glow(32, 34, 'c070ff', 5, 0.7)                            # eye in the spine
    s.flat(lambda d: d.point((32, 34)), 'f4f2fa')
    for x, y in ((8, 14), (56, 16), (20, 8), (44, 6)): s.dot(x, y, 'c070ff')                                             # floating glyphs
    return s.finish(rim='ffb85a', rim_side=1, rim_strength=0.4)


def ink_wraith():
    s = Spr()
    s.part(lambda d: d.polygon([(22, 18), (40, 18), (48, 60), (34, 52), (28, 62), (22, 52), (12, 60)]), INK, round_=0.7)   # dripping ink body
    for x in (16, 26, 36, 44): s.flat(lambda d, x=x: d.line([(x, 52), (x, 60)], width=2), '0b0910')
    s.part(lambda d: d.ellipse([24, 4, 40, 22]), INK)
    s.flat(lambda d: (d.ellipse([27, 11, 31, 15]), d.ellipse([33, 11, 37, 15])), 'f4f2fa'); s.glow(32, 13, 'b0aec8', 5, 0.6)
    s.part(lambda d: d.line([(38, 26), (52, 20), (58, 26)], width=3), INK)                                               # quill-hand
    s.part(lambda d: d.polygon([(56, 26), (62, 14), (60, 28)]), ['554a6d', '8a86a6', 'd8d6e6', 'f4f2fa'])
    return s.finish(rim='b0aec8', rim_side=1, rim_strength=0.45)


def gargoyle():
    s = Spr()
    s.part(lambda d: d.polygon([(22, 22), (6, 6), (8, 18), (2, 22), (14, 30)]), STONE)                                    # stone wing
    s.part(lambda d: (d.polygon([(18, 44), (26, 44), (24, 60), (14, 60)]), d.polygon([(34, 44), (42, 44), (46, 60), (36, 60)])), STONE)
    s.part(lambda d: d.rectangle([10, 58, 50, 62]), STONE, round_=0.2)                                                    # plinth
    s.part(lambda d: d.polygon([(16, 24), (44, 22), (48, 46), (14, 46)]), STONE, round_=0.7)
    s.part(lambda d: d.ellipse([28, 6, 46, 26]), STONE)
    s.part(lambda d: (d.polygon([(30, 8), (26, 0), (33, 6)]), d.polygon([(42, 8), (46, 0), (40, 6)])), STONE)           # horns
    s.flat(lambda d: (d.ellipse([33, 12, 36, 15]), d.ellipse([39, 12, 42, 15])), 'ffb85a'); s.glow(37, 13, 'ffb85a', 5, 0.5)
    s.flat(lambda d: d.polygon([(32, 19), (44, 19), (42, 23), (34, 23)]), '0b0910')
    s.dots([(34, 19), (37, 19), (40, 19)], 'd8d6e6')
    s.part(lambda d: d.line([(44, 28), (54, 38), (52, 46)], width=4), STONE)
    for k in range(3): s.flat(lambda d, k=k: d.line([(52, 46), (50 + k * 2, 50)]), 'd8d6e6')
    for x, y in ((20, 30), (36, 36)): s.flat(lambda d, x=x, y=y: d.line([(x, y), (x + 4, y + 6)]), '141019')             # cracks
    return s.finish(rim='ffb85a', rim_side=1, rim_strength=0.35)


def quill_swarm():
    s = Spr()
    import math
    for k in range(9):
        a = k * 0.7; r = 8 + (k % 3) * 7
        x, y = 32 + r * math.cos(a), 32 + r * math.sin(a) * 0.8
        s.part(lambda d, x=x, y=y, a=a: d.polygon([(x, y), (x + 9 * math.cos(a + 1.9), y + 9 * math.sin(a + 1.9)), (x + 2, y + 2)]),
               ['554a6d', '8a86a6', 'd8d6e6', 'f4f2fa'])
        s.flat(lambda d, x=x, y=y: d.point((x, y)), '1d1726')
    for k in range(6): s.dot(12 + k * 8, 50 + (k % 2) * 4, '1d1726')                                                       # ink spatters
    s.glow(32, 32, 'c070ff', 10, 0.25)
    return s.finish(rim='d8d6e6', rim_side=1, rim_strength=0.4)


def index_golem():
    s = Spr()
    s.part(lambda d: (d.rectangle([18, 46, 28, 60]), d.rectangle([36, 46, 46, 60])), BARK)
    s.part(lambda d: (d.rectangle([16, 58, 30, 62]), d.rectangle([34, 58, 48, 62])), STONE)
    s.part(lambda d: d.rectangle([12, 16, 52, 48]), BARK, round_=0.5)                                                       # a walking bookcase
    for y in (20, 30, 40):
        s.flat(lambda d, y=y: d.line([(14, y + 8), (50, y + 8)], width=1), '0b0910')
        for x in range(15, 50, 4): s.flat(lambda d, x=x, y=y: d.rectangle([x, y, x + 2, y + 7]), ['a63d25', '3f8a8a', 'ffb85a', '6a2848'][(x // 4 + y) % 4])
    s.part(lambda d: d.rectangle([22, 2, 42, 16]), BRASS, round_=0.4)                                                      # brass reading-lamp head
    s.flat(lambda d: d.rectangle([26, 7, 38, 11]), 'ffe09a'); s.glow(32, 9, 'fff6d8', 6, 0.6)
    s.part(lambda d: (d.rectangle([2, 20, 12, 40]), d.rectangle([52, 20, 62, 40])), BARK, round_=0.4)
    return s.finish(rim='ffb85a', rim_side=1, rim_strength=0.4)


def archivist():
    s = Spr(80, 80)
    s.part(lambda d: d.polygon([(26, 24), (54, 24), (66, 79), (14, 79)]), INK, round_=0.5)                                # ink-black robes
    for k in range(5): s.flat(lambda d, k=k: d.line([(20 + k * 10, 79), (24 + k * 8, 50)]), '352a45')
    s.part(lambda d: d.polygon([(30, 24), (50, 24), (52, 60), (28, 60)]), PARCH, round_=0.3)                              # parchment stole
    for y in range(30, 58, 5): s.flat(lambda d, y=y: d.line([(32, y), (48, y)]), '5c4228')
    s.part(lambda d: d.polygon([(26, 4), (54, 4), (58, 26), (22, 26)]), INK, round_=0.7)                                  # hood
    s.flat(lambda d: d.polygon([(32, 8), (48, 8), (49, 24), (31, 24)]), '0b0910')
    for x in (35, 40, 45): s.flat(lambda d, x=x: d.ellipse([x - 2, 12, x + 2, 16]), 'c070ff')                            # three eyes
    s.glow(40, 14, 'c070ff', 7, 0.7)
    for k, (x, y) in enumerate(((8, 20), (66, 14), (4, 44), (70, 40))):                                                    # orbiting books
        s.part(lambda d, x=x, y=y: d.polygon([(x, y), (x + 9, y - 3), (x + 10, y + 6), (x + 1, y + 9)]), ['2a1414', '4a1e1e', '74291f', 'a63d25'])
    s.part(lambda d: d.line([(54, 30), (64, 22)], width=3), INK)
    s.part(lambda d: d.polygon([(62, 18), (72, 12), (74, 20), (66, 26)]), PARCH)                                           # open grimoire
    s.glow(68, 19, 'c070ff', 6, 0.5)
    s.part(lambda d: d.line([(26, 30), (16, 36)], width=3), INK)
    return s.finish(rim='c070ff', rim_side=1)


# ═══════════════════════════ New heroes ═══════════════════════════

def alchemist():
    """Corvin, plague-doctor alchemist: beaked mask, long coat, bandolier of vials, a flask in hand."""
    s = Spr()
    s.part(lambda d: d.polygon([(21, 24), (37, 24), (42, 60), (16, 60)]), ['0b0910', '1d1726', '352a45', '554a6d'], round_=0.3)   # long coat
    s.part(lambda d: (d.rectangle([24, 50, 29, 58]), d.rectangle([31, 50, 36, 58])), 'leather')
    s.part(lambda d: (d.polygon([(22, 57), (30, 57), (31, 62), (21, 62)]), d.polygon([(31, 57), (38, 57), (40, 62), (31, 62)])), 'iron')
    s.part(lambda d: d.line([(22, 28), (36, 44)], width=3), 'leather')                                                     # bandolier
    for k, c in enumerate(('6f9a40', 'a8406a', '3f8a8a', 'ffb85a')): s.flat(lambda d, k=k, c=c: d.rectangle([24 + k * 3, 31 + k * 4, 25 + k * 3, 33 + k * 4]), c)
    s.part(lambda d: d.ellipse([16, 6, 42, 12]), ['0b0910', '1d1726', '352a45', '554a6d'], round_=0.2)                   # wide hat
    s.part(lambda d: d.polygon([(22, 8), (36, 8), (34, 0), (24, 0)]), ['0b0910', '1d1726', '352a45', '554a6d'])
    s.part(lambda d: d.ellipse([24, 11, 35, 23]), 'bone')                                                                  # mask
    s.part(lambda d: d.polygon([(33, 16), (46, 22), (33, 22)]), 'bone')                                                     # the beak
    s.flat(lambda d: d.ellipse([28, 14, 32, 18]), '6fd0c0'); s.glow(30, 16, '6fd0c0', 3, 0.5)                             # glass lens
    s.part(lambda d: d.polygon([(34, 28), (42, 30), (41, 34), (33, 33)]), ['0b0910', '1d1726', '352a45', '554a6d'])
    s.part(lambda d: d.ellipse([41, 26, 49, 36]), ['233a22', '6f9a40', 'b3d05a', 'e8f5a0'], round_=1.0)                  # bubbling flask
    s.flat(lambda d: d.rectangle([44, 22, 46, 26]), 'b89262'); s.glow(45, 31, 'b3d05a', 5, 0.5)
    return s.finish(rim='b3d05a', rim_side=-1, rim_strength=0.45)


def spellblade():
    """Isolde, spellblade: light armor, a long blade wreathed in shifting elemental fire, braided hair."""
    s = Spr()
    s.part(lambda d: (d.rectangle([24, 44, 29, 57]), d.rectangle([31, 44, 36, 57])), 'leather')
    s.part(lambda d: (d.polygon([(22, 56), (30, 56), (31, 62), (21, 62)]), d.polygon([(31, 56), (38, 56), (40, 62), (31, 62)])), 'iron')
    s.part(lambda d: d.polygon([(21, 24), (38, 24), (40, 46), (19, 46)]), 'metal')                                          # cuirass
    s.part(lambda d: d.polygon([(26, 26), (33, 26), (33, 44), (26, 44)]), 'teal', round_=0.2)                               # tabard
    s.flat(lambda d: d.polygon([(29, 30), (32, 34), (29, 38), (26, 34)]), 'b8f5e4')
    s.part(lambda d: d.polygon([(19, 40), (40, 40), (42, 50), (17, 50)]), 'teal')                                           # skirt
    s.part(lambda d: d.ellipse([24, 8, 36, 22]), 'skin')
    s.part(lambda d: d.polygon([(22, 7), (37, 6), (37, 12), (27, 11), (23, 18)]), ['141019', '433758', '8a86a6', 'd8d6e6'])   # silver hair
    s.part(lambda d: d.line([(23, 14), (18, 30)], width=3), ['141019', '433758', '8a86a6', 'd8d6e6'])                     # braid
    s.dots([(31, 15), (34, 15)], '141019')
    s.part(lambda d: d.line([(36, 28), (42, 22)], width=4), 'metal')                                                        # sword arm raised
    s.part(lambda d: d.polygon([(41, 22), (44, 19), (58, 2), (60, 4), (46, 22)]), ['1d3340', '3f8a8a', '6fd0c0', 'f4f2fa'], round_=0.3)  # rune blade
    s.flat(lambda d: d.line([(45, 19), (58, 4)]), 'f4f2fa')
    s.flat(lambda d: d.polygon([(52, 6), (56, 2), (60, 8)]), 'f28a3a'); s.glow(54, 10, 'ffb85a', 6, 0.4)                 # elemental flare
    s.part(lambda d: d.rectangle([38, 22, 46, 24]), 'gold', round_=0)
    return s.finish(rim='6fd0c0', rim_side=1, rim_strength=0.45)


REDESIGNED = {'harpy': harpy, 'imp': imp, 'hound': hound, 'rat': rat, 'leech': leech, 'thrall': thrall,
              'blight': blight, 'siren': siren, 'lich': lich, 'colossus': colossus}
NEW_ENEMIES = {'thornling': thornling, 'spore_puffer': spore_puffer, 'strangler': strangler, 'stalker': stalker,
               'briar_witch': briar_witch, 'seed_pod': seed_pod, 'mother_bloom': mother_bloom,
               'tome': tome, 'ink_wraith': ink_wraith, 'gargoyle': gargoyle, 'quill_swarm': quill_swarm,
               'index_golem': index_golem, 'archivist': archivist}
NEW_HEROES = {'alchemist': alchemist, 'spellblade': spellblade}

U.ENEMIES.update(REDESIGNED)
U.ENEMIES.update(NEW_ENEMIES)
U.HEROES.update(NEW_HEROES)

if __name__ == "__main__":
    for name, fn in {**REDESIGNED, **NEW_ENEMIES}.items():
        fn().image(flip=True).save(f'Units/{name}.png')
    for name, fn in NEW_HEROES.items():
        fn().image().save(f'Units/{name}.png')
    print("units_v3 written")
