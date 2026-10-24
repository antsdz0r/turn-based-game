"""Heroes and monsters in the lit-pixel style. 64x64 (bosses 80x80), feet on the bottom rows.
Everything is drawn facing right; enemies are mirrored on save so they face the party."""
from style_kit import Spr, R, hx

# ═══════════════════════════ HEROES ═══════════════════════════

def vanguard():
    s = Spr()
    s.part(lambda d: d.polygon([(21, 22), (35, 22), (40, 60), (15, 61)]), 'crimson', round_=0.3)            # cape
    s.part(lambda d: d.rectangle([24, 42, 29, 57]), 'metal')                                                  # back leg
    s.part(lambda d: d.rectangle([31, 42, 36, 57]), 'metal')                                                  # front leg
    s.part(lambda d: d.polygon([(23, 56), (30, 56), (31, 62), (22, 62)]), 'iron')                            # boots
    s.part(lambda d: d.polygon([(31, 56), (37, 56), (40, 62), (31, 62)]), 'iron')
    s.part(lambda d: d.polygon([(22, 36), (38, 36), (40, 49), (20, 49)]), 'red')                              # tabard skirt
    s.flat(lambda d: d.line([(20, 49), (40, 49)]), 'ffb85a')
    s.part(lambda d: d.rounded_rectangle([21, 22, 39, 40], 4), 'metal')                                       # breastplate
    s.part(lambda d: d.polygon([(25, 25), (35, 25), (35, 38), (25, 38)]), 'red', round_=0.2)                 # tabard
    s.flat(lambda d: (d.line([(30, 27), (30, 36)]), d.line([(27, 30), (33, 30)])), 'ffb85a')                 # emblem
    s.part(lambda d: d.rectangle([21, 37, 39, 39]), 'leather', round_=0)                                       # belt
    s.dots([(30, 38), (31, 38)], 'ffe09a')
    s.part(lambda d: d.ellipse([17, 19, 28, 29]), 'metal')                                                    # pauldrons
    s.part(lambda d: d.ellipse([32, 19, 43, 29]), 'metal')
    s.part(lambda d: d.ellipse([23, 6, 38, 23]), 'metal')                                                     # helm
    s.part(lambda d: d.rectangle([29, 7, 31, 22]), 'pale', round_=0)                                          # helm ridge
    s.flat(lambda d: d.rectangle([31, 13, 38, 15]), '0b0910')                                                 # visor slit
    s.dots([(34, 18), (36, 18), (35, 20)], '141019')
    s.part(lambda d: d.polygon([(28, 7), (24, 1), (16, 3), (12, 10), (19, 8), (24, 9)]), 'red')              # plume
    # sword arm raised behind
    s.part(lambda d: d.line([(21, 25), (16, 31)], width=5), 'metal')
    s.part(lambda d: d.polygon([(13, 1), (17, 1), (17, 27), (13, 27)]), 'cream', round_=0.2)                 # blade
    s.flat(lambda d: d.line([(15, 2), (15, 26)]), 'fff6d8')
    s.part(lambda d: d.rectangle([9, 27, 21, 29]), 'gold', round_=0)                                          # crossguard
    s.part(lambda d: d.rectangle([13, 30, 16, 35]), 'leather', round_=0)
    s.dot(14, 36, 'ffb85a')
    # kite shield in front
    s.part(lambda d: d.polygon([(36, 23), (51, 23), (51, 39), (43.5, 51), (36, 39)]), 'blue')
    s.flat(lambda d: d.line([(36, 23), (51, 23), (51, 39), (43.5, 51), (36, 39), (36, 23)], width=1), 'b89262')
    s.flat(lambda d: (d.line([(43, 27), (43, 44)], width=2), d.line([(39, 32), (48, 32)], width=2)), 'ffb85a')
    return s.finish(rim='ffb85a', rim_side=-1)


def arcanist():
    s = Spr()
    s.part(lambda d: d.polygon([(21, 25), (37, 25), (44, 62), (14, 62)]), 'purple')                            # robe
    s.flat(lambda d: d.line([(14, 61), (44, 61)], width=2), 'ffb85a')                                         # hem
    s.flat(lambda d: d.line([(30, 38), (31, 60)], width=1), 'b89262')
    s.dots([(22, 50), (36, 46), (26, 56), (39, 56), (20, 42)], 'ffe09a')                                      # stars
    s.part(lambda d: d.rectangle([21, 37, 38, 39]), 'gold', round_=0)                                          # sash
    s.part(lambda d: d.polygon([(17, 27), (24, 26), (22, 44), (14, 42)]), 'purple')                            # back sleeve
    s.part(lambda d: d.ellipse([24, 13, 36, 25]), 'skin')                                                     # face
    s.part(lambda d: d.polygon([(23, 21), (37, 21), (36, 33), (31, 42), (25, 33)]), 'cream')                  # beard
    s.flat(lambda d: d.line([(28, 22), (36, 22)], width=2), 'f4f2fa')                                         # moustache
    s.dots([(31, 18), (34, 18)], '141019')
    s.flat(lambda d: d.line([(30, 16), (35, 16)]), 'd8d6e6')                                                  # brows
    s.dot(37, 20, 'c98a6b')                                                                                    # nose
    s.part(lambda d: d.ellipse([15, 13, 42, 19]), 'purple', round_=0.2)                                       # hat brim
    s.part(lambda d: d.polygon([(21, 15), (37, 15), (31, 5), (24, 0), (17, 2), (25, 6)]), 'purple')           # bent hat
    s.part(lambda d: d.rectangle([21, 13, 37, 15]), 'gold', round_=0)
    s.dot(29, 14, 'ffe09a')
    # staff + front sleeve + hand
    s.part(lambda d: d.line([(46, 11), (44, 62)], width=3), 'wood')
    s.part(lambda d: d.polygon([(35, 27), (45, 32), (44, 38), (34, 36)]), 'purple')
    s.part(lambda d: d.ellipse([42, 31, 47, 36]), 'skin')
    s.part(lambda d: d.polygon([(43, 11), (49, 11), (50, 4), (46, 7), (44, 3), (42, 7)]), 'wood')             # claw head
    s.part(lambda d: d.ellipse([42, 3, 50, 11]), 'fire', round_=0.9)                                          # fire orb
    s.glow(46, 7, 'fff6d8', 3, 0.7)
    return s.finish(rim='f28a3a', rim_side=1)


def warden():
    s = Spr()
    s.part(lambda d: d.polygon([(22, 24), (37, 24), (43, 62), (16, 62)]), 'cream')                             # robe
    s.flat(lambda d: d.line([(16, 61), (43, 61)], width=2), 'ffb85a')
    s.part(lambda d: d.polygon([(22, 30), (37, 41), (37, 45), (22, 34)]), 'ember', round_=0.2)                # sash
    s.part(lambda d: d.polygon([(28, 40), (33, 40), (36, 60), (25, 60)]), 'gold', round_=0.4)                  # front panel
    s.flat(lambda d: d.line([(30, 44), (30, 56)]), 'f28a3a')
    s.part(lambda d: d.polygon([(19, 9), (38, 9), (40, 30), (17, 30)]), 'cream', round_=0.6)                   # hood/veil
    s.part(lambda d: d.ellipse([25, 11, 36, 23]), 'skin')                                                     # face
    s.part(lambda d: d.polygon([(23, 12), (27, 10), (26, 26), (22, 28)]), 'hair')                              # auburn hair
    s.part(lambda d: d.polygon([(36, 12), (38, 14), (37, 24), (35, 22)]), 'hair')
    s.flat(lambda d: d.line([(25, 11), (36, 11)]), 'ffb85a')                                                  # circlet
    s.dot(31, 10, 'fff6d8')
    s.dots([(31, 17), (34, 17)], '141019')
    s.dot(33, 21, 'a63d25')
    # lantern in the back hand
    s.part(lambda d: d.polygon([(17, 27), (23, 26), (20, 40), (14, 38)]), 'cream')
    s.part(lambda d: d.line([(16, 39), (16, 43)]), 'iron')
    s.part(lambda d: d.rectangle([12, 43, 20, 52]), 'gold', round_=0.2)
    s.part(lambda d: d.rectangle([14, 45, 18, 50]), 'fire', round_=0.9)
    s.glow(16, 47, 'fff6d8', 3, 0.6)
    # mace in the front hand
    s.part(lambda d: d.polygon([(35, 26), (43, 31), (41, 36), (34, 33)]), 'cream')
    s.part(lambda d: d.line([(41, 35), (49, 20)], width=2), 'wood')
    s.part(lambda d: d.ellipse([40, 33, 45, 38]), 'skin')
    s.part(lambda d: d.ellipse([45, 13, 54, 22]), 'gold')
    s.dots([(49, 12), (54, 17), (44, 17), (49, 22)], 'ffb85a')
    s.glow(49, 17, 'ffe09a', 4, 0.35)
    return s.finish(rim='ffb85a', rim_side=-1)


def shadowblade():
    s = Spr()
    s.part(lambda d: d.polygon([(23, 22), (14, 26), (5, 36), (12, 34), (8, 42), (20, 32)]), 'crimson')       # scarf tail
    s.part(lambda d: d.polygon([(21, 43), (27, 43), (23, 58), (16, 58)]), 'violet')                            # back leg
    s.part(lambda d: d.polygon([(32, 43), (38, 43), (43, 58), (36, 58)]), 'violet')                            # front leg
    s.part(lambda d: d.polygon([(14, 57), (24, 57), (25, 62), (13, 62)]), 'leather')
    s.part(lambda d: d.polygon([(35, 57), (44, 57), (47, 62), (35, 62)]), 'leather')
    s.part(lambda d: d.polygon([(23, 25), (38, 23), (41, 44), (21, 45)]), 'violet')                            # torso (leaning)
    s.part(lambda d: d.rectangle([21, 40, 40, 42]), 'leather', round_=0)                                       # belt
    s.part(lambda d: d.rectangle([22, 42, 26, 46]), 'leather')                                                 # pouch
    s.dots([(31, 41), (32, 41)], 'b89262')
    s.part(lambda d: d.line([(25, 29), (36, 37)], width=2), 'leather')                                          # strap
    s.part(lambda d: d.polygon([(25, 20), (39, 18), (39, 25), (25, 27)]), 'crimson')                          # scarf
    s.part(lambda d: d.polygon([(24, 8), (37, 8), (40, 20), (26, 24), (19, 13)]), 'violet')                    # hood
    s.flat(lambda d: d.polygon([(30, 12), (38, 12), (39, 20), (31, 21)]), '141019')                            # face in shadow
    s.dots([(34, 16), (37, 16)], 'ff7ab0' if False else 'a8406a')
    s.glow(35.5, 16, 'a8406a', 3, 0.6)
    s.part(lambda d: d.polygon([(31, 19), (39, 18), (39, 22), (31, 23)]), 'crimson', round_=0)                 # mask
    # daggers
    s.part(lambda d: d.polygon([(36, 29), (44, 33), (42, 37), (35, 34)]), 'violet')                            # front arm
    s.part(lambda d: d.ellipse([41, 32, 45, 36]), 'leather')
    s.part(lambda d: d.polygon([(45, 32), (57, 29), (45, 35)]), 'cream', round_=0.1)
    s.flat(lambda d: d.line([(44, 31), (44, 37)]), 'ffb85a')
    s.part(lambda d: d.polygon([(22, 28), (16, 36), (19, 38), (25, 31)]), 'violet')                            # back arm
    s.part(lambda d: d.polygon([(15, 37), (18, 37), (17, 50)]), 'cream', round_=0.1)                           # reverse grip
    s.flat(lambda d: d.line([(14, 36), (20, 36)]), 'ffb85a')
    return s.finish(rim='a8406a', rim_side=1)


def ranger():
    s = Spr()
    s.part(lambda d: d.polygon([(15, 14), (22, 13), (23, 36), (17, 37)]), 'leather')                         # quiver
    s.dots([(16, 11), (18, 10), (20, 12)], 'd8d6e6')
    s.dots([(16, 12), (18, 11), (20, 13)], 'a63d25')
    s.flat(lambda d: (d.line([(17, 12), (17, 15)]), d.line([(19, 11), (19, 15)])), 'b89262')
    s.part(lambda d: d.polygon([(21, 20), (35, 20), (39, 58), (17, 58)]), 'moss', round_=0.4)               # cloak
    s.part(lambda d: d.rectangle([24, 42, 29, 57]), 'leather')
    s.part(lambda d: d.rectangle([31, 42, 36, 57]), 'leather')
    s.part(lambda d: d.polygon([(23, 55), (30, 55), (31, 62), (22, 62)]), 'wood')
    s.part(lambda d: d.polygon([(31, 55), (37, 55), (40, 62), (31, 62)]), 'wood')
    s.part(lambda d: d.rounded_rectangle([22, 24, 37, 44], 3), 'leaf')                                        # tunic
    s.part(lambda d: d.rectangle([22, 39, 37, 41]), 'leather', round_=0)
    s.dot(30, 40, 'ffb85a')
    s.part(lambda d: d.polygon([(21, 18), (37, 18), (34, 27), (24, 27)]), 'moss')                             # hood (down)
    s.part(lambda d: d.ellipse([24, 8, 36, 22]), 'skin')                                                      # face
    s.part(lambda d: d.polygon([(23, 6), (36, 6), (37, 12), (30, 10), (24, 14), (21, 20), (20, 12)]), 'gold')  # hair
    s.part(lambda d: d.polygon([(22, 12), (16, 22), (19, 23), (24, 16)]), 'gold')                            # ponytail
    s.dots([(31, 14), (34, 14)], '141019')
    s.dots([(32, 17), (34, 17)], 'c98a6b')                                                                     # freckles
    s.dot(33, 19, 'a63d25')
    # bow and nocked arrow
    s.flat(lambda d: d.arc([33, 9, 52, 57], 285, 75, width=2), 'b89262')
    s.flat(lambda d: d.arc([34, 10, 51, 56], 285, 75, width=1), '5c4228')
    s.flat(lambda d: d.line([(45, 11), (45, 55)]), 'd8d6e6')
    s.part(lambda d: d.polygon([(34, 28), (45, 32), (44, 36), (33, 33)]), 'leaf')                              # arm
    s.part(lambda d: d.ellipse([43, 31, 48, 36]), 'skin')
    s.flat(lambda d: d.line([(30, 33), (55, 33)]), 'b89262')
    s.dots([(55, 32), (56, 33), (55, 34)], 'f4f2fa')
    return s.finish(rim='ffb85a', rim_side=-1)


def keeper():
    s = Spr()
    s.part(lambda d: d.polygon([(22, 22), (37, 22), (45, 62), (13, 62)]), 'cream', round_=0.6)
    s.flat(lambda d: (d.line([(13, 61), (45, 61)], width=2), d.line([(29, 30), (29, 60)])), 'ffb85a')
    s.part(lambda d: d.polygon([(20, 6), (38, 6), (41, 28), (17, 28)]), 'pale', round_=0.6)                   # hood
    s.part(lambda d: d.ellipse([24, 10, 35, 22]), 'skin')
    s.part(lambda d: d.polygon([(23, 10), (35, 10), (34, 13), (24, 14)]), 'pale', round_=0)
    s.dots([(28, 16), (32, 16)], '1d3340')
    s.part(lambda d: d.polygon([(21, 28), (38, 28), (36, 36), (23, 36)]), 'cream')                            # arms together
    s.part(lambda d: d.ellipse([25, 26, 34, 35]), 'fire', round_=1.0)                                         # ember in hands
    s.glow(29.5, 30.5, 'fff6d8', 4, 0.8)
    s.part(lambda d: (d.ellipse([21, 31, 26, 36]), d.ellipse([33, 31, 38, 36])), 'skin')
    return s.finish(rim='b8f5e4', rim_side=1)


# ═══════════════════════════ MONSTERS ═══════════════════════════

def _skeleton_body(s, ox=0):
    s.part(lambda d: (d.line([(26 + ox, 42), (22 + ox, 61)], width=3), d.line([(33 + ox, 42), (37 + ox, 61)], width=3)), 'bone')
    s.part(lambda d: (d.ellipse([19 + ox, 58, 25 + ox, 62]), d.ellipse([35 + ox, 58, 41 + ox, 62])), 'bone')
    s.part(lambda d: d.polygon([(23 + ox, 39), (37 + ox, 39), (38 + ox, 46), (22 + ox, 46)]), 'crimson', round_=0.2)  # loincloth
    s.part(lambda d: d.rectangle([28 + ox, 22, 31 + ox, 42]), 'bone', round_=0.2)                                     # spine
    for i, y in enumerate(range(24, 38, 4)):
        s.part(lambda d, y=y: d.arc([21 + ox, y - 2, 38 + ox, y + 6], 190, 350, width=2), 'bone', round_=0)
    s.part(lambda d: d.ellipse([22 + ox, 37, 37 + ox, 42]), 'bone')                                                   # pelvis
    s.part(lambda d: d.ellipse([22 + ox, 6, 38 + ox, 22]), 'bone')                                                    # skull
    s.part(lambda d: d.rectangle([27 + ox, 19, 37 + ox, 24]), 'bone', round_=0.2)                                     # jaw
    s.flat(lambda d: (d.line([(29 + ox, 21), (36 + ox, 21)])), '554a6d')
    s.dots([(29 + ox, 22), (31 + ox, 22), (33 + ox, 22), (35 + ox, 22)], 'd8d6e6')
    s.flat(lambda d: (d.ellipse([27 + ox, 11, 31 + ox, 16]), d.ellipse([33 + ox, 11, 37 + ox, 16])), '0b0910')         # sockets
    s.dots([(29 + ox, 13), (35 + ox, 13)], 'ffb85a')
    s.glow(29 + ox, 13, 'f28a3a', 2.5, 0.6); s.glow(35 + ox, 13, 'f28a3a', 2.5, 0.6)
    s.flat(lambda d: d.line([(32 + ox, 16), (32 + ox, 17)]), '554a6d')


def skeleton():
    s = Spr()
    _skeleton_body(s)
    s.part(lambda d: d.polygon([(10, 4), (14, 6), (17, 33), (13, 34)]), 'wood')                                     # rusty club-sword
    s.dots([(12, 10), (13, 16), (14, 22)], '8a3b26' if False else '74291f')
    s.part(lambda d: d.line([(16, 32), (24, 26)], width=2), 'bone')
    s.part(lambda d: d.ellipse([37, 23, 53, 41]), 'crimson')                                                          # round shield
    s.part(lambda d: d.ellipse([42, 28, 48, 36]), 'iron')
    s.flat(lambda d: d.ellipse([37, 23, 53, 41], outline=255, fill=None, width=1), '8a6640')
    return s.finish(rim='ffb85a', rim_side=1)


def skeleton_archer():
    s = Spr()
    _skeleton_body(s, -2)
    s.part(lambda d: d.polygon([(19, 4), (36, 4), (40, 14), (34, 10), (22, 10), (17, 16)]), 'moss')                 # tattered hood
    s.flat(lambda d: d.arc([34, 6, 52, 58], 285, 75, width=2), '8a6640')
    s.flat(lambda d: d.line([(45, 8), (45, 56)]), 'b0aec8')
    s.part(lambda d: d.line([(34, 28), (44, 32)], width=2), 'bone')
    s.flat(lambda d: d.line([(28, 32), (54, 32)]), 'b89262')
    return s.finish(rim='ffb85a', rim_side=1)


def ghoul():
    s = Spr()
    s.part(lambda d: d.polygon([(18, 44), (24, 44), (22, 61), (14, 61)]), 'ghoul')
    s.part(lambda d: d.polygon([(32, 44), (38, 44), (44, 61), (36, 61)]), 'ghoul')
    s.part(lambda d: d.ellipse([14, 20, 42, 50]), 'ghoul')                                                           # hunched body
    s.part(lambda d: d.polygon([(16, 38), (40, 38), (42, 48), (14, 48)]), 'leather', round_=0.2)                     # rags
    s.flat(lambda d: (d.line([(18, 48), (19, 51)]), d.line([(26, 48), (25, 52)]), d.line([(36, 48), (37, 51)])), '3a2a18')
    for x in (20, 24, 28):
        s.flat(lambda d, x=x: d.arc([x, 26, x + 8, 34], 200, 340), '433758')                                     # ribs showing
    s.part(lambda d: d.ellipse([30, 14, 46, 30]), 'ghoul')                                                           # head thrust forward
    s.flat(lambda d: d.polygon([(39, 24), (47, 23), (46, 28), (39, 28)]), '1d1726')                                  # mouth
    s.dots([(41, 24), (43, 24), (45, 24), (42, 27), (44, 27)], 'd8d6e6')
    s.dots([(38, 19), (42, 19)], 'b3d05a'); s.glow(40, 19, 'b3d05a', 3, 0.5)
    s.part(lambda d: d.line([(34, 30), (46, 44)], width=4), 'ghoul')                                                 # long arm
    for i in range(3): s.flat(lambda d, i=i: d.line([(45 + i, 44), (49 + i * 2, 52 + i)], width=1), 'd8d6e6')      # claws
    s.part(lambda d: d.line([(20, 30), (12, 44)], width=4), 'ghoul')
    for i in range(3): s.flat(lambda d, i=i: d.line([(11 + i, 44), (9 + i * 2, 51)], width=1), 'd8d6e6')
    return s.finish(rim='b3d05a', rim_side=1, rim_strength=0.35)


def cultist():
    s = Spr()
    s.part(lambda d: d.polygon([(21, 20), (37, 20), (44, 62), (14, 62)]), 'red')
    s.flat(lambda d: d.line([(14, 61), (44, 61)], width=2), '2a1414')
    s.part(lambda d: d.polygon([(26, 22), (32, 22), (33, 60), (25, 60)]), 'crimson', round_=0.2)
    s.part(lambda d: d.polygon([(20, 4), (36, 4), (40, 24), (18, 24)]), 'red')                                     # hood
    s.flat(lambda d: d.polygon([(26, 10), (37, 10), (37, 22), (27, 22)]), '0b0910')
    s.part(lambda d: d.polygon([(29, 11), (37, 11), (36, 20), (30, 20)]), 'bone', round_=0.3)                      # bone mask
    s.flat(lambda d: (d.rectangle([31, 14, 32, 15]), d.rectangle([34, 14, 35, 15])), '0b0910')
    s.part(lambda d: d.polygon([(34, 28), (44, 30), (44, 35), (34, 34)]), 'red')                                    # arm
    s.part(lambda d: d.ellipse([43, 26, 53, 36]), 'fire', round_=1.0)                                               # fire in palm
    s.glow(48, 31, 'fff6d8', 4, 0.7)
    s.part(lambda d: d.polygon([(22, 28), (14, 34), (17, 38), (24, 33)]), 'red')
    return s.finish(rim='f28a3a', rim_side=1)


def wraith():
    s = Spr()
    s.part(lambda d: d.polygon([(19, 14), (39, 14), (48, 50), (40, 45), (34, 60), (28, 47), (20, 60), (16, 46), (8, 52)]), 'sea', round_=0.8)
    s.part(lambda d: d.polygon([(38, 26), (52, 34), (49, 38), (37, 32)]), 'sea')                                      # reaching arm
    for i in range(3): s.flat(lambda d, i=i: d.line([(50, 34 + i), (56, 33 + i * 2)]), 'b8f5e4')
    s.part(lambda d: d.ellipse([19, 2, 39, 26]), 'teal')                                                             # hood
    s.flat(lambda d: d.ellipse([23, 8, 37, 24]), '0b0910')
    s.dots([(28, 15), (29, 15), (33, 15), (34, 15)], 'b8f5e4')
    s.glow(28.5, 15, 'b8f5e4', 3, 0.6); s.glow(33.5, 15, 'b8f5e4', 3, 0.6)
    s.flat(lambda d: d.arc([27, 18, 35, 23], 20, 160), '2a5560')
    return s.finish(rim='b8f5e4', rim_side=1)


def bulwark():
    s = Spr()
    s.part(lambda d: d.rectangle([21, 42, 28, 58]), 'iron'); s.part(lambda d: d.rectangle([31, 42, 38, 58]), 'iron')
    s.part(lambda d: (d.rectangle([19, 56, 29, 62]), d.rectangle([30, 56, 41, 62])), 'rock')
    s.part(lambda d: d.rounded_rectangle([16, 18, 42, 44], 5), 'iron')                                              # bulky armor
    s.part(lambda d: (d.ellipse([11, 14, 25, 28]), d.ellipse([33, 14, 47, 28])), 'iron')
    s.dots([(14, 18), (44, 18), (18, 24)], '8a86a6')
    s.part(lambda d: d.rounded_rectangle([22, 2, 38, 20], 4), 'iron')                                               # bucket helm
    s.flat(lambda d: d.rectangle([28, 9, 38, 11]), '0b0910')
    s.dots([(32, 10), (35, 10)], 'f28a3a'); s.glow(33.5, 10, 'f28a3a', 3, 0.5)
    s.part(lambda d: d.polygon([(35, 16), (53, 14), (53, 56), (35, 58)]), 'iron', round_=0.3)                      # tower shield
    s.flat(lambda d: d.line([(35, 16), (53, 14), (53, 56), (35, 58), (35, 16)]), '8a6640')
    s.part(lambda d: d.ellipse([40, 30, 49, 39]), 'crimson')
    s.dots([(38, 19), (50, 18), (38, 54), (50, 53)], 'b89262')
    return s.finish(rim='ffb85a', rim_side=1, rim_strength=0.4)


def blight():
    s = Spr()
    s.part(lambda d: (d.rectangle([22, 46, 28, 60]), d.rectangle([33, 46, 39, 60])), 'moss')
    s.part(lambda d: d.ellipse([12, 18, 48, 54]), 'moss', round_=0.9)                                               # bloated belly
    s.part(lambda d: d.ellipse([22, 28, 40, 48]), 'leaf', round_=0.9)
    for (x, y, r) in ((18, 30, 3), (40, 36, 4), (30, 46, 3), (24, 22, 2), (44, 26, 2)):
        s.part(lambda d, x=x, y=y, r=r: d.ellipse([x - r, y - r, x + r, y + r]), ['3d6230', '6f9a40', 'b3d05a', 'fff6d8'], round_=1.0)
    s.part(lambda d: d.ellipse([24, 6, 40, 22]), 'moss')                                                            # small head
    s.flat(lambda d: d.ellipse([32, 14, 39, 19]), '1d1726')
    s.dots([(29, 11), (35, 11)], 'b3d05a')
    s.flat(lambda d: d.line([(35, 19), (37, 26), (36, 30)]), '6f9a40')                                             # drool
    s.part(lambda d: d.line([(16, 30), (8, 44)], width=4), 'moss'); s.part(lambda d: d.line([(44, 30), (50, 44)], width=4), 'moss')
    return s.finish(rim='b3d05a', rim_side=1, rim_strength=0.35)


def necromancer():
    s = Spr()
    s.part(lambda d: d.polygon([(20, 20), (38, 20), (46, 62), (12, 62)]), 'dark', round_=0.5)
    s.flat(lambda d: d.line([(12, 61), (46, 61)], width=2), '3d6230')
    s.dots([(20, 44), (36, 40), (28, 52), (40, 54)], '6f9a40')                                                     # runes
    s.part(lambda d: d.polygon([(20, 2), (37, 2), (41, 24), (17, 24)]), 'dark', round_=0.5)
    s.flat(lambda d: d.polygon([(26, 9), (37, 9), (37, 21), (27, 21)]), '0b0910')
    s.dots([(31, 14), (35, 14)], 'b3d05a'); s.glow(33, 14, 'b3d05a', 3, 0.7)
    s.part(lambda d: (d.ellipse([17, 18, 25, 25]), d.ellipse([34, 18, 42, 25])), 'bone')                          # bone pauldrons
    s.part(lambda d: d.line([(46, 8), (44, 62)], width=3), 'wood')
    s.part(lambda d: d.ellipse([40, 0, 52, 12]), 'bone')                                                            # skull staff
    s.flat(lambda d: (d.rectangle([43, 5, 44, 7]), d.rectangle([48, 5, 49, 7])), '0b0910')
    s.glow(46, 6, 'b3d05a', 5, 0.35)
    s.part(lambda d: d.polygon([(35, 28), (45, 30), (44, 35), (34, 34)]), 'dark')
    s.part(lambda d: d.ellipse([42, 30, 47, 35]), 'pale')
    return s.finish(rim='b3d05a', rim_side=1, rim_strength=0.45)


def hollow_king():
    s = Spr(80, 80)
    s.part(lambda d: d.polygon([(26, 26), (50, 26), (60, 78), (14, 78)]), 'purple', round_=0.5)                     # royal robe
    s.flat(lambda d: (d.line([(14, 77), (60, 77)], width=2), d.line([(37, 40), (37, 76)], width=2)), 'ffb85a')
    s.part(lambda d: d.polygon([(22, 24), (54, 24), (50, 34), (26, 34)]), 'cream', round_=0.3)                     # fur collar
    s.part(lambda d: d.rectangle([34, 30, 40, 48]), 'bone', round_=0.2)
    for y in range(32, 46, 4):
        s.part(lambda d, y=y: d.arc([27, y - 2, 47, y + 6], 190, 350, width=2), 'bone', round_=0)
    s.part(lambda d: d.ellipse([33, 35, 41, 43]), 'fire', round_=1.0)                                               # stolen ember heart
    s.glow(37, 39, 'fff6d8', 6, 0.8)
    s.part(lambda d: d.ellipse([27, 6, 47, 26]), 'bone')                                                            # skull
    s.part(lambda d: d.rectangle([33, 21, 46, 27]), 'bone', round_=0.2)
    s.flat(lambda d: (d.ellipse([32, 12, 37, 18]), d.ellipse([40, 12, 45, 18])), '0b0910')
    s.dots([(34, 15), (42, 15)], 'ffe09a'); s.glow(34, 15, 'f28a3a', 3, 0.7); s.glow(42, 15, 'f28a3a', 3, 0.7)
    s.dots([(35, 24), (37, 24), (39, 24), (41, 24), (43, 24)], 'd8d6e6')
    s.part(lambda d: d.polygon([(27, 8), (27, 0), (31, 5), (35, 0), (37, 5), (40, 0), (43, 5), (47, 0), (47, 8)]), 'gold')  # crown
    s.dots([(31, 6), (37, 6), (43, 6)], 'a63d25')
    # greatsword planted in front
    s.part(lambda d: d.polygon([(58, 6), (63, 6), (63, 58), (58, 58)]), 'cream', round_=0.2)
    s.flat(lambda d: d.line([(60, 8), (60, 56)]), 'fff6d8')
    s.part(lambda d: d.rectangle([52, 58, 69, 61]), 'gold', round_=0)
    s.part(lambda d: d.rectangle([58, 62, 63, 70]), 'leather')
    s.part(lambda d: d.polygon([(46, 34), (58, 52), (56, 56), (44, 40)]), 'purple')
    s.part(lambda d: d.ellipse([54, 52, 61, 59]), 'bone')
    return s.finish(rim='f28a3a', rim_side=1)


def rat():
    s = Spr()
    def one(ox, oy, sc=1.0):
        s.part(lambda d: d.ellipse([ox, oy, ox + int(22 * sc), oy + int(13 * sc)]), 'violet', round_=0.8)            # body
        s.part(lambda d: d.polygon([(ox + int(18 * sc), oy + 1), (ox + int(30 * sc), oy + int(7 * sc)),
                                    (ox + int(19 * sc), oy + int(12 * sc))]), 'violet')                              # snout
        s.dot(ox + int(29 * sc), oy + int(7 * sc), 'a8406a')
        s.dot(ox + int(22 * sc), oy + int(5 * sc), 'd85f2a')
        s.part(lambda d: d.ellipse([ox + int(15 * sc), oy - 2, ox + int(20 * sc), oy + 3]), 'flesh')                    # ear
        s.flat(lambda d: d.line([(ox, oy + int(8 * sc)), (ox - int(10 * sc), oy + int(12 * sc)), (ox - int(14 * sc), oy + int(8 * sc))]), 'c98a6b')
        s.dots([(ox + 6, oy + int(13 * sc)), (ox + 14, oy + int(13 * sc))], 'c98a6b')
    one(18, 30, 0.9)
    one(6, 47, 1.0)
    one(30, 48, 1.05)
    return s.finish(rim='ffb85a', rim_side=1, rim_strength=0.4)


def moth():
    s = Spr()
    wing = ['433758', '8a86a6', 'b0aec8', 'd8d6e6']
    s.part(lambda d: d.polygon([(30, 30), (4, 6), (2, 26), (12, 40)]), wing, round_=0.6)                            # back wing
    s.part(lambda d: d.polygon([(30, 34), (10, 44), (14, 56), (28, 44)]), wing, round_=0.6)
    s.part(lambda d: d.polygon([(32, 30), (58, 4), (62, 26), (50, 40)]), wing, round_=0.6)                          # front wing
    s.part(lambda d: d.polygon([(32, 34), (52, 44), (48, 56), (34, 44)]), wing, round_=0.6)
    for cx, cy in ((12, 20), (52, 18)):
        s.part(lambda d, cx=cx, cy=cy: d.ellipse([cx - 5, cy - 5, cx + 5, cy + 5]), ['1d1726', '6a2848', 'a8406a', 'ffb85a'], round_=1.0)
        s.dot(cx, cy, '0b0910')
    s.part(lambda d: d.ellipse([27, 22, 36, 52]), ['3a2a18', '5c4228', '8a6640', 'b89262'], round_=0.9)           # body
    for y in range(28, 50, 4): s.flat(lambda d, y=y: d.line([(28, y), (35, y)]), '3a2a18')
    s.part(lambda d: d.ellipse([27, 16, 37, 26]), 'wood')
    s.dots([(34, 20), (35, 21)], 'b8f5e4'); s.glow(34.5, 20.5, 'b8f5e4', 3, 0.5)
    s.flat(lambda d: (d.line([(32, 16), (28, 6), (24, 4)]), d.line([(34, 16), (38, 6), (42, 4)])), 'b89262')     # antennae
    return s.finish(rim='b8f5e4', rim_side=1, rim_strength=0.4)


def thrall():
    s = Spr()
    s.part(lambda d: (d.rectangle([21, 44, 28, 60]), d.rectangle([32, 44, 39, 60])), 'ghoul')
    s.part(lambda d: d.rounded_rectangle([16, 20, 44, 48], 6), 'ghoul', round_=0.8)                                  # bloated torso
    s.part(lambda d: d.polygon([(17, 36), (43, 36), (44, 50), (16, 50)]), 'iron', round_=0.3)                         # rusted mail skirt
    s.dots([(20, 40), (25, 44), (32, 41), (38, 46), (41, 39)], 'a63d25')                                              # rust
    s.part(lambda d: d.ellipse([22, 4, 40, 22]), 'ghoul')
    s.part(lambda d: d.polygon([(20, 8), (42, 8), (42, 12), (20, 12)]), 'iron', round_=0)                             # helmet rim
    s.part(lambda d: d.ellipse([22, 0, 40, 12]), 'iron')
    s.flat(lambda d: d.ellipse([31, 13, 38, 19]), '141019')
    s.dots([(33, 15), (36, 15)], '6fd0c0'); s.glow(34.5, 15, '6fd0c0', 3, 0.5)
    for x0, top in ((18, 10), (28, 6), (40, 10)):                                                                     # seaweed
        s.flat(lambda d, x0=x0, top=top: d.line([(x0, top), (x0 - 2, top + 8), (x0 + 1, top + 16), (x0 - 1, top + 24)], width=2), '3d6230')
    s.dots([(24, 26), (25, 27), (37, 30), (38, 30), (30, 24)], 'b0aec8')                                              # barnacles
    s.part(lambda d: d.line([(42, 26), (50, 40)], width=6), 'ghoul')
    s.part(lambda d: d.ellipse([46, 38, 55, 47]), 'ghoul')
    s.part(lambda d: d.line([(18, 26), (12, 42)], width=6), 'ghoul')
    return s.finish(rim='6fd0c0', rim_side=1, rim_strength=0.45)


def siren():
    s = Spr()
    s.part(lambda d: d.polygon([(22, 36), (40, 36), (44, 56), (52, 62), (34, 60), (16, 62), (20, 52)]), 'sea', round_=0.6)  # tail/skirt
    for y in range(40, 58, 4): s.flat(lambda d, y=y: d.arc([20, y, 42, y + 6], 20, 160), '6fd0c0')                   # scales
    s.part(lambda d: d.polygon([(23, 20), (37, 20), (40, 38), (21, 38)]), 'teal', round_=0.6)                         # torso
    s.part(lambda d: d.polygon([(21, 8), (30, 4), (39, 10), (36, 36), (30, 30), (16, 38), (18, 18)]), 'purple')        # long hair
    s.part(lambda d: d.ellipse([24, 8, 37, 22]), 'teal')                                                              # face
    s.dots([(32, 14), (35, 14)], 'b8f5e4'); s.glow(33.5, 14, 'b8f5e4', 3, 0.5)
    s.flat(lambda d: d.ellipse([32, 17, 35, 20]), '1d1726')                                                           # singing mouth
    s.part(lambda d: d.polygon([(19, 6), (23, 2), (24, 10)]), 'teal')                                                 # fin-ears
    s.part(lambda d: d.polygon([(36, 22), (48, 16), (48, 20), (38, 27)]), 'teal')                                     # raised arm
    s.flat(lambda d: (d.arc([46, 8, 58, 20], 300, 60), d.arc([48, 4, 62, 22], 300, 60)), 'b8f5e4')                   # song waves
    s.dots([(24, 24), (34, 24), (29, 26)], 'f4f2fa')                                                                  # pearls
    return s.finish(rim='b8f5e4', rim_side=1)


def leech():
    import math
    s = Spr()
    body = ['2a1414', '6a2848', 'a8406a', 'c98a6b']
    pts = [(6, 58), (13, 57), (20, 54), (26, 48), (30, 41), (32, 34), (35, 27)]                                   # rearing up
    for i, (x, y) in enumerate(pts):
        r = 6 + i * 0.5
        s.part(lambda d, x=x, y=y, r=r: d.ellipse([x - r, y - r, x + r, y + r]), body, round_=0.9, cast=False)
    for (x, y) in pts[1:-1]:
        s.flat(lambda d, x=x, y=y: d.arc([x - 7, y - 7, x + 7, y + 7], 120, 200), '2a1414')                       # segment rings
    s.dots([(10, 56), (17, 55), (23, 51), (28, 45)], 'ffe09a')                                                       # glistening
    s.part(lambda d: d.ellipse([32, 12, 50, 30]), body, round_=0.9)                                                # head
    s.flat(lambda d: d.ellipse([38, 15, 50, 27]), '1d1726')                                                         # round maw
    for a in range(0, 360, 40):
        s.dot(int(44 + 4.5 * math.cos(math.radians(a))), int(21 + 4.5 * math.sin(math.radians(a))), 'f4f2fa')
    s.dot(44, 21, '74291f')
    return s.finish(rim='ffb85a', rim_side=1, rim_strength=0.4)


def imp():
    s = Spr()
    s.part(lambda d: d.polygon([(24, 26), (6, 12), (10, 30), (20, 34)]), 'crimson', round_=0.4)                     # wing
    s.part(lambda d: d.polygon([(34, 26), (52, 10), (50, 28), (38, 34)]), 'crimson', round_=0.4)
    s.part(lambda d: (d.line([(26, 44), (22, 58)], width=3), d.line([(33, 44), (37, 58)], width=3)), 'red')
    s.flat(lambda d: d.line([(24, 46), (14, 52), (10, 46), (8, 48)], width=2), 'a63d25')                              # tail
    s.dot(7, 47, 'f28a3a')
    s.part(lambda d: d.ellipse([20, 26, 38, 48]), 'red', round_=0.8)
    s.part(lambda d: d.ellipse([20, 10, 40, 30]), 'red', round_=0.8)                                                  # big head
    s.part(lambda d: (d.polygon([(23, 13), (20, 2), (27, 11)]), d.polygon([(35, 12), (40, 2), (37, 14)])), 'bone')  # horns
    s.flat(lambda d: (d.ellipse([27, 17, 31, 21]), d.ellipse([33, 17, 37, 21])), 'ffe09a')
    s.dots([(30, 19), (36, 19)], '0b0910')
    s.flat(lambda d: d.arc([27, 20, 38, 27], 20, 160), '2a1414')
    s.part(lambda d: d.line([(36, 32), (44, 28)], width=3), 'red')
    s.part(lambda d: d.ellipse([42, 18, 52, 28]), 'fire', round_=1.0)
    s.glow(47, 23, 'fff6d8', 4, 0.7)
    return s.finish(rim='ffe09a', rim_side=1, rim_strength=0.4)


def golem():
    s = Spr()
    s.part(lambda d: (d.rectangle([18, 44, 28, 60]), d.rectangle([32, 44, 42, 60])), 'rock')
    s.part(lambda d: d.polygon([(14, 20), (46, 18), (48, 46), (12, 48)]), 'rock', round_=0.6)                        # boulder torso
    s.part(lambda d: d.ellipse([22, 4, 38, 20]), 'rock')
    s.part(lambda d: (d.ellipse([4, 22, 18, 50]), d.ellipse([42, 22, 58, 50])), 'rock', round_=0.7)                  # huge fists
    lava = 'f28a3a'
    for line in ([(18, 24), (24, 30), (22, 38), (28, 44)], [(36, 22), (34, 30), (40, 36)], [(8, 30), (12, 38)],
                 [(48, 30), (52, 40)], [(26, 10), (30, 14), (34, 12)]):
        s.flat(lambda d, line=line: d.line(line, width=1), lava)
    s.glow(24, 32, 'ffb85a', 6, 0.35); s.glow(38, 30, 'ffb85a', 5, 0.3)
    s.flat(lambda d: d.rectangle([27, 10, 36, 13]), 'ffe09a'); s.glow(31, 11, 'fff6d8', 4, 0.6)                    # eye slit
    return s.finish(rim='f28a3a', rim_side=1, rim_strength=0.5)


def colossus():
    s = Spr(80, 80)
    s.part(lambda d: (d.line([(30, 50), (22, 77)], width=8), d.line([(48, 50), (56, 77)], width=8)), 'bone')       # legs
    s.part(lambda d: (d.ellipse([16, 72, 30, 79]), d.ellipse([48, 72, 62, 79])), 'pale')
    s.part(lambda d: d.ellipse([24, 42, 56, 58]), 'bone', round_=0.6)                                             # pelvis
    s.part(lambda d: d.polygon([(14, 16), (64, 16), (58, 48), (20, 48)]), ['1d1726', '282033', '352a45', '433758'])  # dark cavity
    s.part(lambda d: d.rectangle([36, 14, 42, 50]), 'bone', round_=0.3)                                          # spine
    for y in range(18, 46, 6):                                                                                     # huge ribs
        s.part(lambda d, y=y: d.arc([12, y - 6, 66, y + 12], 195, 345, width=4), 'bone', round_=0.2)
    s.part(lambda d: d.ellipse([33, 34, 45, 46]), 'fire', round_=1.0)                                             # grave-fire core
    s.glow(39, 40, 'ffe09a', 7, 0.6)
    s.part(lambda d: (d.ellipse([4, 8, 28, 30]), d.ellipse([50, 8, 74, 30])), 'bone', round_=0.7)                # skull shoulders
    for cx in (16, 62):
        s.flat(lambda d, cx=cx: (d.ellipse([cx - 6, 15, cx - 2, 20]), d.ellipse([cx + 2, 15, cx + 6, 20])), '0b0910')
        s.dots([(cx - 4, 17), (cx + 4, 17)], 'f28a3a')
    s.part(lambda d: d.ellipse([28, 0, 50, 22]), 'bone')                                                          # main skull
    s.part(lambda d: d.rectangle([33, 17, 48, 24]), 'bone', round_=0.2)
    s.flat(lambda d: (d.ellipse([32, 6, 38, 13]), d.ellipse([41, 6, 47, 13])), '0b0910')
    s.dots([(35, 9), (44, 9)], 'ffe09a'); s.glow(35, 9, 'f28a3a', 3, 0.8); s.glow(44, 9, 'f28a3a', 3, 0.8)
    s.dots([(35, 22), (38, 22), (41, 22), (44, 22), (47, 22)], 'f4f2fa')
    s.part(lambda d: d.line([(8, 26), (4, 54)], width=7), 'bone')                                                # back arm
    s.part(lambda d: d.ellipse([0, 52, 10, 62]), 'pale')
    s.part(lambda d: d.line([(68, 26), (72, 44)], width=7), 'bone')                                              # club arm
    s.part(lambda d: d.polygon([(66, 44), (74, 6), (80, 8), (74, 46)]), 'wood')                                  # femur club
    s.part(lambda d: (d.ellipse([70, 0, 80, 10]), d.ellipse([66, 40, 76, 50])), 'bone')
    s.dots([(73, 20), (75, 28), (72, 34)], 'a63d25')
    return s.finish(rim='ffb85a', rim_side=1)


def matriarch():
    s = Spr(80, 80)
    for i, (x0, x1) in enumerate(((14, 6), (24, 18), (36, 36), (48, 56), (58, 72))):                                 # tentacle skirt
        s.part(lambda d, x0=x0, x1=x1: d.line([(x0 + 6, 50), (x0 + 3, 64), (x1, 78)], width=6), 'sea', round_=0.5)
    s.part(lambda d: d.polygon([(26, 30), (54, 30), (62, 60), (18, 60)]), 'sea', round_=0.6)
    s.part(lambda d: d.polygon([(30, 18), (50, 18), (54, 40), (26, 40)]), 'teal', round_=0.7)                        # torso
    s.part(lambda d: d.polygon([(24, 6), (40, 0), (56, 8), (54, 44), (44, 30), (36, 30), (24, 46)]), 'purple')        # hair
    s.part(lambda d: d.ellipse([31, 4, 49, 24]), 'teal')
    s.dots([(38, 13), (43, 13)], 'b8f5e4'); s.glow(40.5, 13, 'b8f5e4', 4, 0.6)
    s.flat(lambda d: d.arc([37, 16, 45, 21], 20, 160), '1d3340')
    s.part(lambda d: d.polygon([(30, 6), (32, -4), (36, 4), (40, -6), (44, 4), (48, -4), (50, 6)]), ['3a2a18', 'a8406a', 'd85f2a', 'ffb85a'])  # coral crown
    s.dots([(34, 30), (40, 32), (46, 30)], 'f4f2fa')
    s.part(lambda d: d.line([(52, 24), (66, 36)], width=4), 'teal')                                                  # arm with trident
    s.part(lambda d: d.line([(70, 4), (62, 74)], width=2), 'gold')
    s.part(lambda d: (d.line([(64, 6), (70, 4), (76, 8)], width=2), d.line([(70, 4), (70, -2)], width=2)), 'gold')
    s.part(lambda d: d.line([(28, 24), (16, 40)], width=4), 'teal')
    return s.finish(rim='b8f5e4', rim_side=1)


def berserker():
    s = Spr()
    s.part(lambda d: d.polygon([(19, 19), (39, 19), (41, 30), (17, 30)]), ['3a2a18', '5c4228', '8a6640', 'b0aec8'], round_=0.7)  # fur mantle
    s.part(lambda d: (d.rectangle([23, 42, 29, 57]), d.rectangle([32, 42, 38, 57])), 'skin')                        # bare legs
    s.part(lambda d: d.polygon([(21, 55), (30, 55), (31, 62), (20, 62)]), 'leather')
    s.part(lambda d: d.polygon([(32, 55), (38, 55), (41, 62), (32, 62)]), 'leather')
    s.part(lambda d: d.polygon([(21, 38), (40, 38), (42, 50), (19, 50)]), 'leather', round_=0.3)                   # kilt
    s.flat(lambda d: (d.line([(24, 40), (23, 50)]), d.line([(30, 40), (30, 50)]), d.line([(36, 40), (38, 50)])), '2a1414')
    s.part(lambda d: d.rounded_rectangle([22, 24, 39, 40], 5), 'skin', round_=0.8)                                  # torso
    s.part(lambda d: d.polygon([(22, 27), (39, 27), (39, 33), (22, 33)]), 'leather', round_=0.2)                   # chest wrap
    s.flat(lambda d: (d.line([(26, 34), (28, 37)]), d.line([(33, 35), (35, 38)])), 'a63d25')                       # war paint
    s.part(lambda d: d.rectangle([21, 38, 40, 40]), 'iron', round_=0)
    s.dot(30, 39, 'ffb85a')
    s.part(lambda d: d.polygon([(19, 8), (38, 6), (42, 22), (36, 30), (30, 18), (22, 28), (16, 20)]), 'hair')     # wild red mane
    s.part(lambda d: d.ellipse([25, 9, 37, 22]), 'skin')
    s.part(lambda d: d.polygon([(24, 8), (36, 6), (37, 11), (25, 12)]), 'hair')
    s.dots([(32, 14), (35, 14)], '141019')
    s.flat(lambda d: (d.line([(31, 16), (33, 17)]), d.line([(35, 16), (36, 17)])), 'd85f2a')                       # warpaint stripes
    s.flat(lambda d: d.line([(32, 19), (35, 19)]), '74291f')
    s.part(lambda d: (d.polygon([(23, 6), (20, 0), (26, 5)]), d.polygon([(35, 5), (40, 0), (37, 7)])), 'bone')  # horned band
    # great axe held across the body
    s.part(lambda d: d.line([(14, 54), (46, 10)], width=3), 'wood')
    s.part(lambda d: d.polygon([(40, 4), (52, 2), (56, 12), (50, 20), (42, 14)]), 'metal', round_=0.4)             # axe head
    s.flat(lambda d: d.line([(52, 3), (56, 12), (50, 19)]), 'f4f2fa')
    s.part(lambda d: (d.ellipse([20, 32, 27, 39]), d.ellipse([33, 22, 40, 29])), 'skin')                            # hands on the haft
    s.dots([(17, 50), (16, 52)], 'a63d25')
    return s.finish(rim='f28a3a', rim_side=-1)


def hexer():
    s = Spr()
    for (x, y) in ((9, 26), (52, 18), (14, 46)):                                                                    # floating hex orbs
        s.part(lambda d, x=x, y=y: d.ellipse([x - 3, y - 3, x + 3, y + 3]), ['433758', '6a2848', 'a8406a', 'f4f2fa'], round_=1.0)
        s.glow(x, y, 'a8406a', 4, 0.4)
    s.part(lambda d: d.polygon([(22, 22), (37, 22), (45, 62), (13, 62)]), 'violet', round_=0.6)                     # robe
    s.flat(lambda d: d.line([(13, 61), (45, 61)], width=2), '6a2848')
    for (x, y) in ((20, 50), (30, 44), (38, 54), (26, 58)):                                                         # sigils
        s.flat(lambda d, x=x, y=y: (d.line([(x - 2, y), (x + 2, y)]), d.line([(x, y - 2), (x, y + 2)])), 'a8406a')
    s.part(lambda d: d.rectangle([21, 34, 38, 36]), 'purple', round_=0)
    s.part(lambda d: d.polygon([(21, 4), (36, 4), (41, 30), (17, 30)]), 'dark', round_=0.6)                         # veil
    s.part(lambda d: d.ellipse([25, 10, 36, 23]), 'pale')                                                             # pale face
    s.part(lambda d: d.polygon([(24, 10), (36, 10), (36, 14), (24, 15)]), 'dark', round_=0)
    s.dots([(31, 17), (34, 17)], 'a8406a'); s.glow(32.5, 17, 'a8406a', 3, 0.6)
    s.flat(lambda d: d.line([(31, 20), (34, 20)]), '6a2848')                                                          # dark lips
    s.part(lambda d: d.polygon([(20, 2), (37, 2), (32, 0), (25, 0)]), 'dark')
    s.dots([(28, 3), (29, 3)], 'ffb85a')                                                                               # veil jewel
    s.part(lambda d: d.polygon([(36, 26), (46, 30), (45, 35), (35, 33)]), 'violet')                                  # casting arm
    s.part(lambda d: d.ellipse([44, 28, 50, 34]), 'pale')
    for i in range(3): s.flat(lambda d, i=i: d.line([(49, 29 + i * 2), (54, 26 + i * 4)]), 'a8406a')             # hex sparks
    s.glow(51, 31, 'a8406a', 5, 0.5)
    s.part(lambda d: d.polygon([(22, 26), (15, 36), (18, 38), (24, 31)]), 'violet')
    return s.finish(rim='a8406a', rim_side=1)


def mimic():
    s = Spr()
    s.part(lambda d: d.polygon([(10, 34), (54, 34), (52, 60), (12, 60)]), 'wood', round_=0.5)                       # chest body
    s.flat(lambda d: (d.line([(10, 40), (54, 40)], width=2), d.line([(11, 54), (53, 54)], width=2)), '8a6640')
    s.part(lambda d: (d.rectangle([14, 34, 18, 60]), d.rectangle([46, 34, 50, 60])), 'gold', round_=0.3)           # bands
    s.part(lambda d: d.rectangle([28, 42, 36, 50]), 'gold')                                                         # lock
    s.flat(lambda d: d.rectangle([31, 45, 33, 48]), '1d1726')
    s.flat(lambda d: d.polygon([(12, 34), (52, 34), (56, 22), (16, 16)]), '1d1726')                                  # open maw
    s.part(lambda d: d.polygon([(8, 14), (14, 2), (58, 8), (58, 22), (16, 18)]), 'wood', round_=0.5)                # lid
    s.part(lambda d: (d.rectangle([16, 4, 20, 18]), d.rectangle([46, 6, 50, 20])), 'gold', round_=0.3)
    for x in range(16, 54, 5):                                                                                       # teeth
        s.flat(lambda d, x=x: d.polygon([(x, 18), (x + 4, 19), (x + 2, 24)]), 'f4f2fa')
        s.flat(lambda d, x=x: d.polygon([(x, 34), (x + 4, 34), (x + 2, 29)]), 'd8d6e6')
    s.part(lambda d: d.polygon([(30, 30), (44, 28), (58, 34), (62, 42), (56, 38), (46, 34)]), body_ramp := ['4a1e1e', 'a8406a', 'c98a6b', 'e8b796'])  # tongue
    s.dots([(26, 25), (36, 24)], 'ffe09a'); s.glow(26, 25, 'f28a3a', 3, 0.6); s.glow(36, 24, 'f28a3a', 3, 0.6)   # eyes in the dark
    s.dots([(20, 60), (44, 60)], '3a2a18')
    return s.finish(rim='ffb85a', rim_side=1, rim_strength=0.4)


# ═══════════════════════════ FROZEN OSSUARY ═══════════════════════════

ICE = ['1d3340', '6b6386', 'b0aec8', 'f4f2fa']
FROST = ['2a5560', '6fd0c0', 'b8f5e4', 'f4f2fa']

def wight():
    s = Spr()
    s.part(lambda d: (d.rectangle([23, 42, 29, 58]), d.rectangle([32, 42, 38, 58])), 'iron')
    s.part(lambda d: (d.polygon([(21, 56), (30, 56), (31, 62), (20, 62)]), d.polygon([(32, 56), (39, 56), (41, 62), (32, 62)])), 'rock')
    s.part(lambda d: d.polygon([(20, 22), (40, 22), (42, 46), (18, 46)]), ICE, round_=0.6)                               # frosted mail
    s.part(lambda d: d.polygon([(22, 36), (38, 36), (40, 50), (20, 50)]), 'blue', round_=0.3)                            # tattered tabard
    s.flat(lambda d: (d.line([(22, 50), (21, 53)]), d.line([(30, 50), (31, 54)]), d.line([(37, 50), (38, 53)])), '1d3340')
    s.part(lambda d: (d.ellipse([16, 18, 27, 28]), d.ellipse([33, 18, 44, 28])), ICE)
    s.part(lambda d: d.ellipse([23, 4, 38, 22]), 'bone')                                                                 # skull
    s.part(lambda d: d.polygon([(22, 10), (39, 10), (38, 4), (34, 0), (27, 0), (23, 4)]), ICE)                           # frozen helm
    s.flat(lambda d: (d.ellipse([27, 12, 31, 16]), d.ellipse([33, 12, 37, 16])), '0b0910')
    s.dots([(29, 14), (35, 14)], 'b8f5e4'); s.glow(29, 14, '6fd0c0', 3, 0.6); s.glow(35, 14, '6fd0c0', 3, 0.6)
    for x, y in ((20, 26), (40, 24), (24, 44), (36, 46), (26, 3), (36, 3)):                                              # icicles
        s.flat(lambda d, x=x, y=y: d.polygon([(x - 1, y), (x + 1, y), (x, y + 4)]), 'f4f2fa')
    s.part(lambda d: d.polygon([(44, 4), (48, 4), (48, 34), (44, 34)]), FROST, round_=0.2)                               # rime blade
    s.part(lambda d: d.rectangle([40, 34, 52, 36]), 'iron', round_=0)
    s.part(lambda d: d.line([(40, 27), (45, 34)], width=4), ICE)
    return s.finish(rim='b8f5e4', rim_side=1)


def wisp():
    s = Spr()
    s.part(lambda d: d.polygon([(24, 30), (32, 54), (28, 60), (36, 52), (40, 30)]), FROST, round_=0.8)                    # trailing tail
    s.part(lambda d: d.ellipse([18, 12, 46, 40]), FROST, round_=1.0)                                                       # glowing core
    s.part(lambda d: d.ellipse([24, 18, 40, 34]), ['6fd0c0', 'b8f5e4', 'f4f2fa', 'fff6d8'], round_=1.0)
    s.glow(32, 26, 'fff6d8', 10, 0.5)
    s.dots([(28, 25), (35, 25)], '1d3340'); s.dot(31, 30, '1d3340')
    for x, y in ((12, 18), (50, 22), (16, 40), (48, 42), (32, 6)):
        s.part(lambda d, x=x, y=y: d.polygon([(x, y - 3), (x + 2, y), (x, y + 3), (x - 2, y)]), FROST, round_=0.5)       # ice shards orbiting
    return s.finish(rim='f4f2fa', rim_side=1, rim_strength=0.3)


def hound():
    s = Spr()
    s.part(lambda d: (d.line([(16, 44), (12, 60)], width=3), d.line([(22, 44), (22, 60)], width=3)), 'bone')         # hind legs
    s.part(lambda d: (d.line([(40, 44), (42, 60)], width=3), d.line([(46, 42), (50, 60)], width=3)), 'bone')          # fore legs
    s.part(lambda d: d.polygon([(10, 34), (48, 30), (50, 44), (12, 46)]), 'bone', round_=0.4)                          # body
    s.part(lambda d: d.rectangle([12, 34, 48, 37]), 'pale', round_=0)                                                  # spine ridge
    for x in range(16, 46, 5): s.flat(lambda d, x=x: d.line([(x, 38), (x + 1, 45)]), '554a6d')                     # ribs
    s.flat(lambda d: d.line([(10, 36), (2, 30), (0, 24)], width=2), 'b0aec8')                                            # tail
    s.part(lambda d: d.polygon([(44, 22), (58, 26), (62, 34), (52, 38), (44, 34)]), 'bone')                             # skull snout
    s.flat(lambda d: d.polygon([(52, 33), (62, 34), (54, 37)]), '1d1726')
    s.dots([(54, 34), (57, 34), (60, 34)], 'f4f2fa')
    s.part(lambda d: d.polygon([(44, 22), (46, 14), (50, 22)]), 'bone')                                                # ear
    s.dot(51, 27, 'f28a3a'); s.glow(51, 27, 'f28a3a', 3, 0.7)
    for x in (14, 22, 34): s.flat(lambda d, x=x: d.polygon([(x, 32), (x + 2, 28), (x + 4, 32)]), 'b8f5e4')         # frost spikes
    return s.finish(rim='b8f5e4', rim_side=1, rim_strength=0.4)


def phylactery():
    s = Spr()
    s.part(lambda d: d.rectangle([20, 52, 44, 60]), 'iron', round_=0.3)                                                 # pedestal
    s.part(lambda d: d.polygon([(24, 48), (40, 48), (44, 52), (20, 52)]), 'metal')
    s.part(lambda d: d.ellipse([18, 16, 46, 50]), ['1d3340', '2a5560', '6fd0c0', 'b8f5e4'], round_=1.0)                 # crystal jar
    s.part(lambda d: d.ellipse([26, 24, 38, 40]), ['3f8a8a', '6fd0c0', 'b8f5e4', 'fff6d8'], round_=1.0)                  # trapped soul
    s.glow(32, 32, 'fff6d8', 8, 0.6)
    s.part(lambda d: d.rectangle([24, 10, 40, 16]), 'iron', round_=0.2)                                                 # lid
    s.part(lambda d: d.ellipse([28, 4, 36, 12]), 'bone')                                                                 # skull finial
    s.flat(lambda d: (d.point((30, 8)), d.point((34, 8))), '0b0910')
    for a, b in (((18, 26), (12, 20)), ((46, 26), (52, 18)), ((22, 46), (14, 50)), ((42, 46), (50, 52))):
        s.flat(lambda d, a=a, b=b: d.line([a, b], width=1), 'b8f5e4')                                                    # soul chains
    return s.finish(rim='b8f5e4', rim_side=1, rim_strength=0.3)


def lich():
    s = Spr(80, 80)
    s.part(lambda d: d.polygon([(26, 24), (52, 24), (64, 78), (12, 78)]), ['1d3340', '2a5560', '3f8a8a', '6fd0c0'], round_=0.6)  # ice robe
    s.flat(lambda d: (d.line([(12, 77), (64, 77)], width=2), d.line([(38, 36), (38, 76)], width=2)), 'b8f5e4')
    for x, y in ((18, 62), (30, 70), (48, 64), (58, 72)):
        s.flat(lambda d, x=x, y=y: (d.line([(x - 2, y), (x + 2, y)]), d.line([(x, y - 2), (x, y + 2)])), 'f4f2fa')     # frost sigils
    s.part(lambda d: d.polygon([(20, 22), (58, 22), (54, 34), (24, 34)]), ICE, round_=0.4)                              # frost mantle
    s.part(lambda d: d.ellipse([28, 6, 50, 28]), 'bone')                                                                 # skull
    s.flat(lambda d: (d.ellipse([33, 13, 38, 19]), d.ellipse([41, 13, 46, 19])), '0b0910')
    s.dots([(35, 16), (43, 16)], 'b8f5e4'); s.glow(35, 16, '6fd0c0', 4, 0.7); s.glow(43, 16, '6fd0c0', 4, 0.7)
    s.flat(lambda d: d.line([(35, 24), (44, 24)]), '554a6d')
    s.part(lambda d: d.polygon([(28, 8), (24, -4), (31, 4), (35, -6), (39, 3), (44, -6), (47, 4), (54, -4), (50, 8)]), FROST)   # icicle crown
    s.part(lambda d: d.line([(56, 26), (66, 40)], width=4), ICE)                                                         # arm
    s.part(lambda d: d.line([(68, 8), (64, 78)], width=3), 'iron')                                                       # staff
    s.part(lambda d: d.polygon([(62, 0), (74, 0), (76, 10), (68, 16), (60, 10)]), FROST, round_=0.8)                    # ice crystal head
    s.glow(68, 8, 'fff6d8', 6, 0.6)
    s.part(lambda d: d.line([(24, 26), (12, 42)], width=4), ICE)
    s.part(lambda d: d.ellipse([6, 38, 16, 48]), FROST, round_=1.0); s.glow(11, 43, 'b8f5e4', 5, 0.5)                  # frost in hand
    return s.finish(rim='b8f5e4', rim_side=1)


# ═══════════════════════════ STORM SPIRE ═══════════════════════════

STORMY = ['141019', '282033', '433758', '8a86a6']
SPARK = ['433758', '8a86a6', 'd8d6e6', 'fff6d8']

def harpy():
    s = Spr()
    s.part(lambda d: d.polygon([(28, 26), (4, 8), (0, 22), (10, 30), (2, 38), (22, 34)]), STORMY, round_=0.5)      # back wing
    s.part(lambda d: (d.line([(28, 46), (24, 58)], width=2), d.line([(34, 46), (38, 58)], width=2)), 'gold')       # legs
    s.flat(lambda d: (d.line([(22, 58), (26, 60)]), d.line([(36, 60), (40, 58)])), 'f4f2fa')                          # talons
    s.part(lambda d: d.ellipse([22, 22, 42, 48]), STORMY, round_=0.8)                                                  # feathered body
    s.part(lambda d: d.ellipse([26, 8, 40, 24]), 'skin')                                                               # face
    s.part(lambda d: d.polygon([(24, 8), (40, 6), (44, 14), (38, 12), (30, 10), (22, 18)]), 'violet')                 # wild hair
    s.dots([(34, 15), (37, 15)], 'b8f5e4'); s.glow(35.5, 15, 'd8d6e6', 3, 0.5)
    s.flat(lambda d: d.line([(34, 20), (38, 20)]), '74291f')
    s.part(lambda d: d.polygon([(36, 26), (62, 6), (64, 20), (54, 28), (62, 34), (42, 34)]), STORMY, round_=0.5)     # front wing
    for x, y in ((48, 14), (56, 12), (54, 24)):
        s.flat(lambda d, x=x, y=y: d.line([(x, y), (x + 3, y + 3), (x + 1, y + 5)]), 'fff6d8')                         # sparks in the feathers
    return s.finish(rim='d8d6e6', rim_side=1, rim_strength=0.4)


def construct():
    s = Spr()
    s.part(lambda d: (d.rectangle([18, 44, 28, 60]), d.rectangle([34, 44, 44, 60])), 'gold', round_=0.4)            # brass legs
    s.part(lambda d: (d.rectangle([16, 58, 30, 62]), d.rectangle([32, 58, 46, 62])), 'iron')
    s.part(lambda d: d.rounded_rectangle([14, 18, 48, 46], 6), 'gold', round_=0.6)                                    # brass hull
    s.part(lambda d: d.ellipse([24, 24, 38, 38]), ['1d3340', '3f8a8a', '6fd0c0', 'fff6d8'], round_=1.0)               # lightning core
    s.glow(31, 31, 'fff6d8', 7, 0.6)
    s.flat(lambda d: (d.line([(26, 31), (30, 27), (31, 33), (36, 29)])), 'fff6d8')
    for x, y in ((17, 22), (45, 22), (17, 42), (45, 42)): s.dot(x, y, '3a2a18')                                      # rivets
    s.part(lambda d: d.rounded_rectangle([22, 4, 40, 18], 3), 'gold')                                                 # head
    s.flat(lambda d: d.rectangle([26, 9, 37, 12]), '6fd0c0'); s.glow(31, 10, 'b8f5e4', 4, 0.5)                       # visor
    s.flat(lambda d: (d.line([(28, 4), (26, 0)]), d.line([(34, 4), (36, 0)])), 'iron')                              # antennae
    s.dots([(26, 0), (36, 0)], 'fff6d8')
    s.part(lambda d: (d.rectangle([4, 20, 14, 40]), d.rectangle([48, 20, 58, 40])), 'gold', round_=0.4)              # piston arms
    s.part(lambda d: (d.rectangle([2, 38, 14, 48]), d.rectangle([48, 38, 60, 48])), 'iron', round_=0.4)
    return s.finish(rim='6fd0c0', rim_side=1, rim_strength=0.45)


def stormcaller():
    s = Spr()
    s.part(lambda d: d.polygon([(21, 22), (37, 22), (44, 62), (14, 62)]), STORMY, round_=0.5)
    s.flat(lambda d: d.line([(14, 61), (44, 61)], width=2), 'b0aec8')
    for y in (34, 44, 54):
        s.flat(lambda d, y=y: d.line([(18, y), (22, y - 3), (26, y), (30, y - 3), (34, y), (38, y - 3)]), '8a86a6')      # zigzag runes
    s.part(lambda d: d.polygon([(20, 4), (36, 4), (40, 26), (18, 26)]), STORMY, round_=0.5)                           # hood
    s.flat(lambda d: d.polygon([(26, 10), (37, 10), (37, 22), (27, 22)]), '0b0910')
    s.dots([(31, 15), (35, 15)], 'fff6d8'); s.glow(33, 15, 'd8d6e6', 3, 0.7)
    s.part(lambda d: d.polygon([(36, 26), (44, 18), (47, 21), (39, 30)]), STORMY)                                      # raised arm
    s.part(lambda d: d.ellipse([42, 12, 50, 20]), 'pale')
    s.flat(lambda d: (d.line([(46, 12), (49, 6), (46, 4), (50, -2)], width=1), d.line([(48, 14), (54, 8), (52, 4), (57, 0)], width=1)), 'fff6d8')  # lightning
    s.glow(48, 10, 'fff6d8', 6, 0.5)
    s.part(lambda d: d.polygon([(22, 28), (14, 34), (17, 38), (24, 32)]), STORMY)
    return s.finish(rim='d8d6e6', rim_side=1, rim_strength=0.45)


def tyrant():
    s = Spr(80, 80)
    s.part(lambda d: d.polygon([(24, 20), (12, 30), (8, 64), (20, 56), (24, 40)]), ['141019', '1d1726', '282033', '433758'], round_=0.4)  # storm cape
    s.part(lambda d: (d.rectangle([28, 52, 37, 74]), d.rectangle([42, 52, 51, 74])), STORMY)
    s.part(lambda d: (d.polygon([(26, 72), (38, 72), (39, 79), (25, 79)]), d.polygon([(41, 72), (52, 72), (54, 79), (41, 79)])), 'rock')
    s.part(lambda d: d.polygon([(24, 22), (56, 22), (58, 54), (22, 54)]), STORMY, round_=0.6)                          # armored torso
    s.part(lambda d: d.polygon([(32, 26), (48, 26), (46, 46), (34, 46)]), SPARK, round_=0.5)                            # breastplate
    s.flat(lambda d: d.line([(36, 30), (41, 36), (38, 38), (43, 44)], width=2), 'fff6d8'); s.glow(40, 37, 'fff6d8', 5, 0.4)  # lightning sigil
    s.part(lambda d: (d.ellipse([16, 16, 32, 30]), d.ellipse([48, 16, 64, 30])), SPARK)                                # pauldrons
    s.part(lambda d: d.rounded_rectangle([30, 2, 50, 22], 5), STORMY)                                                   # great helm
    s.flat(lambda d: d.rectangle([34, 10, 50, 13]), 'fff6d8'); s.glow(42, 11, 'd8d6e6', 5, 0.6)                        # glowing visor
    s.part(lambda d: (d.polygon([(30, 6), (22, -4), (32, 2)]), d.polygon([(50, 6), (58, -4), (48, 2)])), SPARK)       # horn crests
    s.part(lambda d: d.line([(56, 28), (66, 44)], width=6), STORMY)                                                     # spear arm
    s.part(lambda d: d.line([(72, 2), (60, 78)], width=3), 'iron')                                                      # spear
    s.part(lambda d: d.polygon([(70, 0), (78, -2), (76, 10), (71, 12)]), SPARK)
    for a, b in (((74, 12), (66, 22)), ((76, 4), (80, 14)), ((70, 26), (62, 30))):
        s.flat(lambda d, a=a, b=b: d.line([a, b]), 'fff6d8')
    s.glow(74, 6, 'fff6d8', 6, 0.6)
    s.part(lambda d: d.line([(24, 28), (16, 46)], width=6), STORMY)
    return s.finish(rim='d8d6e6', rim_side=1)


# ═══════════════════════════ NEW ENEMIES (one per region) ═══════════════════════════

def bellringer():
    """Crypt: a hunched sexton who rings a funeral bell to rouse the dead."""
    s = Spr()
    s.part(lambda d: d.polygon([(20, 26), (36, 24), (42, 62), (14, 62)]), 'violet')                              # ragged robe
    s.flat(lambda d: d.line([(14, 61), (42, 61)], width=2), '282033')
    for x in (16, 22, 30, 38): s.flat(lambda d, x=x: d.polygon([(x, 62), (x + 3, 57), (x + 6, 62)]), '141019')  # tattered hem
    s.part(lambda d: d.polygon([(18, 14), (34, 12), (38, 30), (20, 32)]), 'violet', round_=0.6)                 # hood, hunched forward
    s.flat(lambda d: d.polygon([(26, 18), (36, 17), (37, 27), (28, 28)]), '0b0910')
    s.dots([(31, 22), (34, 22)], 'ffb85a'); s.glow(32.5, 22, 'ffb85a', 3, 0.5)
    s.part(lambda d: d.line([(44, 6), (40, 62)], width=3), 'wood')                                                # bell pole
    s.part(lambda d: d.line([(44, 6), (54, 8)], width=2), 'wood')
    s.part(lambda d: d.polygon([(48, 10), (58, 10), (61, 26), (45, 26)]), 'gold', round_=0.7)                    # the bell
    s.flat(lambda d: d.line([(45, 26), (61, 26)], width=2), '8a6640')
    s.part(lambda d: d.ellipse([51, 25, 55, 29]), 'iron')
    s.part(lambda d: d.polygon([(34, 30), (42, 26), (44, 30), (36, 34)]), 'violet')                               # arm on the pole
    s.part(lambda d: d.ellipse([39, 26, 44, 31]), 'pale')
    return s.finish(rim='ffb85a', rim_side=1)


def shellback():
    """Drowned Halls: a barnacled crab the size of a cart. Armored; lightning cracks the shell."""
    s = Spr()
    for x in (10, 18, 40, 48):                                                                                     # legs
        s.part(lambda d, x=x: d.line([(x + 4, 44), (x, 54), (x + 3, 62)], width=3), 'flesh')
    s.part(lambda d: d.ellipse([6, 22, 58, 52]), ['1d3340', '2a5560', '3f8a8a', '6fd0c0'], round_=0.9)           # shell
    for x, y in ((18, 28), (30, 25), (42, 29), (24, 38), (38, 40)):
        s.part(lambda d, x=x, y=y: d.ellipse([x, y, x + 6, y + 5]), 'bone')                                       # barnacles
    s.part(lambda d: d.ellipse([16, 42, 50, 52]), 'flesh', round_=0.4)                                            # underbelly
    s.part(lambda d: (d.line([(28, 30), (26, 18)], width=2), d.line([(36, 30), (38, 18)], width=2)), 'flesh')   # eye stalks
    s.part(lambda d: (d.ellipse([23, 14, 29, 20]), d.ellipse([35, 14, 41, 20])), 'bone')
    s.dots([(26, 17), (38, 17)], '0b0910')
    # One huge pincer, raised
    s.part(lambda d: d.polygon([(48, 34), (56, 26), (60, 30), (52, 38)]), 'flesh')
    s.part(lambda d: d.polygon([(54, 12), (63, 16), (62, 30), (52, 26)]), ['4a1e1e', '74291f', 'a63d25', 'd85f2a'], round_=0.6)
    s.part(lambda d: d.polygon([(50, 8), (56, 6), (58, 14), (52, 18)]), ['4a1e1e', '74291f', 'a63d25', 'd85f2a'])
    return s.finish(rim='6fd0c0', rim_side=1, rim_strength=0.45)


def rime_sentinel():
    """Frozen Ossuary: an ice totem with a trapped face. It shields allies and chills the party."""
    s = Spr()
    s.part(lambda d: d.polygon([(18, 56), (46, 56), (50, 62), (14, 62)]), 'rock')                                  # base stone
    s.part(lambda d: d.polygon([(22, 8), (42, 8), (46, 56), (18, 56)]), ICE, round_=0.8)                          # ice pillar
    s.part(lambda d: d.polygon([(24, 2), (32, -4), (40, 2), (42, 10), (22, 10)]), FROST)                          # crown spikes
    s.part(lambda d: d.polygon([(16, 20), (8, 14), (12, 26), (18, 30)]), FROST)                                     # side shards
    s.part(lambda d: d.polygon([(46, 22), (56, 16), (52, 30), (46, 32)]), FROST)
    s.flat(lambda d: d.ellipse([25, 18, 39, 34]), '1d3340')                                                         # the trapped face
    s.dots([(29, 24), (35, 24)], 'b8f5e4'); s.glow(32, 24, 'f4f2fa', 5, 0.6)
    s.flat(lambda d: d.line([(29, 30), (35, 30)]), 'b8f5e4')
    for y in (40, 46, 52): s.flat(lambda d, y=y: d.line([(22, y), (42, y - 2)]), 'b0aec8')                          # carved runes
    return s.finish(rim='b8f5e4', rim_side=1, rim_strength=0.5)


def ballista():
    """Storm Spire: a brass siege ballista on wheels. It charges bolts at the back row."""
    s = Spr()
    s.part(lambda d: (d.ellipse([8, 46, 24, 62]), d.ellipse([38, 46, 54, 62])), 'wood', round_=0.8)                # wheels
    s.part(lambda d: (d.ellipse([13, 51, 19, 57]), d.ellipse([43, 51, 49, 57])), 'gold')
    s.part(lambda d: d.rectangle([10, 40, 52, 48]), 'wood', round_=0.3)                                            # carriage
    s.part(lambda d: d.polygon([(26, 40), (36, 40), (34, 28), (28, 28)]), 'gold', round_=0.5)                       # mount
    s.part(lambda d: d.polygon([(6, 30), (60, 22), (61, 26), (7, 34)]), 'wood')                                    # stock, aimed up-forward
    s.part(lambda d: d.polygon([(40, 10), (46, 12), (34, 46), (30, 44)]), 'wood')                                  # bow arms
    s.flat(lambda d: (d.line([(43, 11), (10, 31)]), d.line([(32, 45), (10, 31)])), 'd8d6e6')                       # string
    s.part(lambda d: d.polygon([(10, 30), (62, 21), (63, 24), (11, 33)]), 'iron', round_=0.2)                      # the bolt
    s.flat(lambda d: d.polygon([(58, 19), (64, 22), (59, 26)]), 'f4f2fa')
    s.part(lambda d: d.ellipse([22, 20, 32, 30]), SPARK)                                                            # storm coil
    s.glow(27, 25, 'fff6d8', 5, 0.5)
    return s.finish(rim='d8d6e6', rim_side=1, rim_strength=0.45)


def ash_priest():
    """Cinder Throne: a priest of the Hollow King swinging a censer of burning ash."""
    s = Spr()
    s.part(lambda d: d.polygon([(21, 24), (37, 24), (44, 62), (14, 62)]), 'crimson')                               # robe
    s.flat(lambda d: d.line([(14, 61), (44, 61)], width=2), 'ffb85a')
    s.part(lambda d: d.polygon([(26, 26), (32, 26), (34, 60), (24, 60)]), 'gold', round_=0.3)                       # stole
    s.flat(lambda d: (d.line([(29, 34), (29, 42)]), d.line([(26, 37), (32, 37)])), 'a63d25')
    s.part(lambda d: d.polygon([(21, 6), (37, 6), (40, 26), (18, 26)]), 'crimson', round_=0.6)                     # tall hood
    s.part(lambda d: d.polygon([(26, 0), (32, -2), (34, 8), (24, 8)]), 'crimson')
    s.part(lambda d: d.polygon([(26, 12), (37, 12), (37, 24), (27, 24)]), 'bone')                                   # bone mask
    s.dots([(30, 17), (34, 17)], '0b0910')
    s.flat(lambda d: d.line([(29, 21), (35, 21)]), '554a6d')
    s.part(lambda d: d.polygon([(34, 28), (43, 32), (41, 36), (33, 33)]), 'crimson')                                 # arm
    s.part(lambda d: d.line([(42, 34), (48, 46)], width=1), 'iron')                                                 # censer chain
    s.part(lambda d: d.ellipse([44, 44, 54, 54]), 'gold', round_=0.8)                                               # censer
    s.part(lambda d: d.ellipse([46, 40, 52, 46]), 'fire', round_=0.9)
    s.glow(49, 44, 'ffe09a', 6, 0.6)
    for x, y in ((50, 36), (47, 32), (52, 28)): s.dot(x, y, '8a86a6')                                                # smoke
    return s.finish(rim='ffb85a', rim_side=1)


HEROES = {'vanguard': vanguard, 'arcanist': arcanist, 'warden': warden, 'shadowblade': shadowblade,
          'ranger': ranger, 'cleric': keeper, 'berserker': berserker, 'hexer': hexer}
ENEMIES = {'skeleton': skeleton, 'skeleton_archer': skeleton_archer, 'ghoul': ghoul, 'cultist': cultist,
           'wraith': wraith, 'bulwark': bulwark, 'blight': blight, 'necromancer': necromancer,
           'hollow_king': hollow_king, 'rat': rat, 'moth': moth, 'thrall': thrall, 'siren': siren,
           'leech': leech, 'imp': imp, 'golem': golem, 'colossus': colossus, 'matriarch': matriarch,
           'mimic': mimic, 'wight': wight, 'wisp': wisp, 'hound': hound, 'phylactery': phylactery, 'lich': lich,
           'harpy': harpy, 'construct': construct, 'stormcaller': stormcaller, 'tyrant': tyrant,
           'bellringer': bellringer, 'shellback': shellback, 'rime_sentinel': rime_sentinel, 'ballista': ballista,
           'ash_priest': ash_priest}
