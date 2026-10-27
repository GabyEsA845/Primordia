"""Música del piso 2 (océano cámbrico), según PROMPT_piso2.md.

Todo en re, como el resto del juego: Someras en re dórico, Arrecife en re menor natural, Abismo en re frigio, el
laboratorio en re menor y los jefes en re frigio (Anomalocaris) y re menor (Euriptérido). Cada franja tiene dos
capas (exploración y combate) que comparten tempo, progresión, melodía y duración exacta: el juego las reproduce a
la vez y funde una con otra. Los bucles usan Song2, que filtra los graves de forma circular (empalme sin salto)."""

import numpy as np

from instrumentos_piso2 import (D_AEOLIAN, D_DORIAN, D_PHRYGIAN, Song2, add_echo, i_glass, i_marimba, i_tide, i_tremolo,
                                sea_kit, song_line)
from musica import (MUSIC, bass_note, bubble_bed, fade, i_bass, i_choir, i_flute, i_harp, i_lead, i_pad,
                    i_saw_bass, i_sub, make_kit, play_pad, section_melody, voicing)
from sintesis import midi


def with_durs(pairs, end):
    """[(pulso, midi)] → [(pulso, duración hasta la siguiente nota, midi)]."""
    out = []
    for i, (b, m) in enumerate(pairs):
        nxt = pairs[i + 1][0] if i + 1 < len(pairs) else end
        out.append((b, nxt - b, m))
    return out


def shift(mel, beats):
    return [(b + beats, d, m) for b, d, m in mel]


def arpeggio(s, b0, ch, pattern, step, gain, pan=-0.3, rev=0.3, lo=62, accent=None):
    tones = voicing(ch, lo) + [m + 12 for m in voicing(ch, lo)]
    for e, idx in enumerate(pattern):
        g = gain * (1.4 if accent and e in accent else 1.0)
        s.add(b0 + e * step, i_marimba(midi(tones[idx])), pan, g, rev)


def drums(s, kit, b0, kick=(0, 2), snare=(1, 3), hat_step=0.5, gk=0.3, gs=0.18, gh=0.05):
    for b in kick:
        s.add(b0 + b, kit["kick"], 0, gk, 0.05)
    for b in snare:
        s.add(b0 + b, kit["snare"], 0.1, gs, 0.15)
    if hat_step:
        for i in range(int(4 / hat_step)):
            s.add(b0 + i * hat_step, kit["hat"], 0.35, gh * (1.3 if i % 2 == 0 else 1.0), 0.05)


# ================================================================== Someras

def shallows_material():
    progA = ["Dm", "G", "Dm", "C", "Dm", "G", "Am", "C"]
    progB = ["G", "Am", "Dm", "C", "G", "Em", "Am", "C"]
    melA = with_durs([(0, 74), (1.5, 76), (2, 77), (4, 79), (5.5, 77), (6, 76), (8, 74), (9, 72), (10, 69),
                      (12, 71), (13, 72), (14, 74), (16, 74), (17.5, 76), (18, 77), (20, 81), (21.5, 79), (22, 74),
                      (24, 72), (25, 71), (26, 69), (28, 76), (30, 72)], 32)
    melB = section_melody(np.random.default_rng(211), progB, 8, 4, 64, 81, D_DORIAN)
    return progA + progA + progB, melA, shift(melB, 64)


def music_floor2_shallows_explore():
    prog, melA, melB = shallows_material()
    s = Song2(100, 24 * 4, seed=201)
    k = sea_kit(s.rng)
    play_pad(s, prog, inst=i_tide, gain=0.07, lo=57, rev=0.5, dark=700, bright=2600)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        arpeggio(s, b0, ch, [0, 1, 2, 3, 2, 1, 0, 1], 0.5, 0.11, accent=(0, 2, 4, 6))
        root = bass_note(ch, 38)
        s.add(b0, i_sub(midi(root), s.secs(3.5)), 0, 0.2, 0.1)
        s.add(b0, i_marimba(midi(root + 12)), 0, 0.18, 0.2)
        s.add(b0 + 2.5, i_marimba(midi(root + 12)), 0, 0.12, 0.2)
        for beat in (1.5, 3.5):
            s.add(b0 + beat, k["shells"], 0.4, 0.08, 0.2)
    for b, d, m in melA + melB:  # A y B: marimba con eco
        add_echo(s, b, i_marimba(midi(m)), 0.15, 0.28, 0.35, every=0.75, fb=0.42, repeats=3)
    for b, d, m in shift(melA, 32):  # A': arpa, con la marimba una octava arriba como brillo
        s.add(b, i_harp(midi(m), s.secs(d)), 0.15, 0.22, 0.45)
        s.add(b, i_marimba(midi(m + 12)), -0.4, 0.08, 0.5)
    bubble_bed(s, 80, 0.05, 900, 2600)
    return s.render(rt=2.0, wet=0.5, bright=5000)


def music_floor2_shallows_combat():
    prog, melA, melB = shallows_material()
    s = Song2(100, 24 * 4, seed=202)
    k = sea_kit(s.rng)
    kit = make_kit(s.rng)
    play_pad(s, prog, inst=i_tide, gain=0.05, lo=57, rev=0.4, dark=900, bright=3000, rate=0.3)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        r = bass_note(ch, 38)
        for e, m in enumerate((r, r, r + 7, r, r + 12, r, r + 7, r + 12)):
            s.add(b0 + e * 0.5, i_bass(midi(m), s.secs(0.4)), 0, 0.26 if e % 2 == 0 else 0.2, 0.05)
        drums(s, kit, b0, kick=(0, 2, 2.75) if bar % 2 else (0, 2), gk=0.32, gs=0.17, gh=0.05)
        s.add(b0 + 1.5, k["wood_lo"], -0.2, 0.25, 0.15)
        s.add(b0 + 3.5, k["wood_hi"], 0.25, 0.2, 0.15)
        arpeggio(s, b0, ch, [0, 1, 2, 3, 4, 3, 2, 1] * 2, 0.25, 0.07, pan=-0.35, rev=0.25, accent=(0, 4, 8, 12))
        if bar % 8 == 0:
            s.add(b0, kit["crash"], 0, 0.1, 0.3)
    for b, d, m in melA + shift(melA, 32) + melB:
        add_echo(s, b, i_marimba(midi(m)), 0.15, 0.27, 0.3, every=0.75, fb=0.35, repeats=2)
        s.add(b, i_lead(midi(m), s.secs(d) * 0.9, 0.25, 3000), 0.1, 0.06, 0.25)
    return s.render(rt=1.6, wet=0.4, bright=6000)


# ================================================================= Arrecife

def reef_material():
    progA = ["Dm", "Bb", "C", "Dm", "Gm", "Bb", "Am", "Dm"]
    progB = ["Bb", "C", "Am", "Dm", "Gm", "C", "A", "A"]
    melA = [(0, .75, 69), (.75, .75, 72), (1.5, 1.5, 74), (3, 1, 77), (4, 1.5, 74), (5.5, .5, 72), (6, 2, 70),
            (8, .75, 72), (8.75, .75, 74), (9.5, 1.5, 76), (11, 1, 79), (12, 3, 74), (15, 1, 72),
            (16, .75, 70), (16.75, .75, 72), (17.5, 1.5, 74), (19, 1, 77), (20, 1.5, 79), (21.5, .5, 77),
            (22, 2, 74), (24, 1, 72), (25, 1, 69), (26, 2, 72), (28, 3, 74), (31, 1, 72)]
    melB = section_melody(np.random.default_rng(221), progB, 8, 4, 67, 84, D_AEOLIAN)
    return progA + progA + progB, melA, shift(melB, 64)


def reef_groove(s, k, b0, gain=1.0, bar=0):
    for beat, name, g in ((0, "wood_lo", 0.5), (2.75, "wood_lo", 0.35), (1.5, "wood_hi", 0.3),
                          (3.5, "wood_hi", 0.25), (1, "stone", 0.32), (3, "stone", 0.32)):
        s.add(b0 + beat, k[name], 0.25 if "hi" in name else -0.1, g * gain, 0.15)
    for i in range(16):
        if i not in (0, 8):
            s.add(b0 + i * 0.25, k["chitin"], 0.45, (0.2 if i % 4 == 2 else 0.09) * gain, 0.1)
    for beat in (0.5, 1.5, 2.5, 3.5):
        s.add(b0 + beat, k["shells"], -0.45, 0.16 * gain, 0.1)
    if bar % 2 == 1:
        s.add(b0 + 3.75, k["big_bubble"], 0.3, 0.2 * gain, 0.4)


def reef_bass(s, b0, ch, gain=0.3, saw_gain=0.0):
    r = bass_note(ch, 38)
    for beat, m in zip((0, 0.75, 1.5, 2.5, 3, 3.5), (r, r, r + 12, r, r + 7, r + 12)):
        s.add(b0 + beat, i_bass(midi(m), s.secs(0.35)), 0, gain, 0.05)
        if saw_gain:
            s.add(b0 + beat, i_saw_bass(midi(m), s.secs(0.3), 700), 0, saw_gain, 0.05)


def music_floor2_reef_explore():
    prog, melA, melB = reef_material()
    s = Song2(108, 24 * 4, seed=203)
    k = sea_kit(s.rng)
    play_pad(s, prog, inst=i_tide, gain=0.05, lo=57, rev=0.4, rate=0.3)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        reef_groove(s, k, b0, 0.85, bar)
        reef_bass(s, b0, ch)
        tones = voicing(ch, 62)
        for e, idx in enumerate([0, 2, 1, 0, 2, 1, 0, 1]):
            s.add(b0 + e * 0.5, i_marimba(midi(tones[idx])), -0.35, 0.17 if e in (0, 3, 6) else 0.1, 0.2)
    for b, d, m in melA + melB:
        add_echo(s, b, i_flute(midi(m), s.secs(d) * 0.9), 0.2, 0.22, 0.3, every=0.5, fb=0.3, repeats=2)
    for b, d, m in shift(melA, 32):  # A': la marimba dobla a la flauta
        add_echo(s, b, i_flute(midi(m), s.secs(d) * 0.9), 0.2, 0.18, 0.3, every=0.5, fb=0.3, repeats=2)
        s.add(b, i_marimba(midi(m)), -0.2, 0.16, 0.3)
    return s.render(rt=1.5, wet=0.4, bright=6000)


def music_floor2_reef_combat():
    prog, melA, melB = reef_material()
    s = Song2(108, 24 * 4, seed=204)
    k = sea_kit(s.rng)
    kit = make_kit(s.rng)
    play_pad(s, prog, inst=i_tide, gain=0.04, lo=57, rev=0.35, rate=0.45, bright=2600)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        reef_groove(s, k, b0, 1.0, bar)
        reef_bass(s, b0, ch, 0.28, 0.14)
        for beat in (0, 2):
            s.add(b0 + beat, k["deep_drum"], 0, 0.35, 0.1)
        drums(s, kit, b0, kick=(), snare=(1, 3), hat_step=0.25, gs=0.17, gh=0.035)
        tones = voicing(ch, 62)
        for e, idx in enumerate([0, 2, 1, 0, 2, 1, 0, 1] * 2):
            s.add(b0 + e * 0.25, i_marimba(midi(tones[idx])), -0.35, 0.12 if e in (0, 3, 6, 8, 11, 14) else 0.07, 0.2)
        if bar % 8 == 0:
            s.add(b0, kit["crash"], 0, 0.1, 0.3)
    for b, d, m in melA + shift(melA, 32) + melB:
        add_echo(s, b, i_flute(midi(m), s.secs(d) * 0.9), 0.2, 0.18, 0.25, every=0.5, fb=0.25, repeats=1)
        s.add(b, i_lead(midi(m), s.secs(d) * 0.9, 0.25, 3200), -0.1, 0.07, 0.2)
    return s.render(rt=1.3, wet=0.35, bright=6500)


# =================================================================== Abismo

def abyss_material():
    progA = ["Dm", "Dm", "Eb", "Eb", "Dm", "Dm", "Cm", "Eb"]
    progB = ["Gm", "Gm", "Eb", "Eb", "Bb", "Bb", "Cm", "Eb"]
    notesA = [(0, 3, 50), (3, 1, 51), (4, 4, 53), (8, 2, 51), (10, 2, 50), (12, 4, 46),
              (16, 3, 50), (19, 1, 53), (20, 4, 57), (24, 2, 55), (26, 2, 51), (28, 3.5, 51)]
    notesB = [(0, 3, 55), (3, 1, 53), (4, 4, 50), (8, 2, 51), (10, 2, 50), (12, 4, 46),
              (16, 3, 53), (19, 1, 55), (20, 4, 58), (24, 2, 55), (26, 2, 51), (28, 3.5, 51)]
    return progA + progB, notesA + shift(notesB, 32)


def abyss_bed(s, k, prog, notes, canto_gain=0.45):
    for bar, ch in enumerate(prog):
        s.add(bar * 4, i_sub(midi(bass_note(ch, 38)), s.secs(4)), 0, 0.2, 0.1)
    s.add(0, song_line(notes, s.beat), 0, canto_gain, 0.6)
    for beat in range(2, len(prog) * 4, 8):
        add_echo(s, beat, k["sonar"], 0.3, 0.1, 0.4, every=1.5, fb=0.5, repeats=4, darken=4000)


def music_floor2_abyss_explore():
    prog, notes = abyss_material()
    s = Song2(80, 16 * 4, seed=205)
    k = sea_kit(s.rng)
    play_pad(s, prog, inst=i_tide, gain=0.09, lo=50, rev=0.6, dark=300, bright=1100, rate=0.12)
    abyss_bed(s, k, prog, notes)
    for bar in range(len(prog)):
        s.add(bar * 4, k["heartbeat"], 0, 0.3, 0.1)
    for _ in range(14):
        s.add(s.rng.uniform(0, 64), k["big_bubble"], s.rng.uniform(-0.7, 0.7), 0.12, 0.6)
    return s.render(rt=3.2, wet=0.65, bright=2500)


def music_floor2_abyss_combat():
    prog, notes = abyss_material()
    s = Song2(80, 16 * 4, seed=206)
    k = sea_kit(s.rng)
    play_pad(s, prog, inst=i_tide, gain=0.06, lo=50, rev=0.5, dark=350, bright=1300, rate=0.2)
    play_pad(s, prog, inst=i_tremolo, gain=0.035, lo=50, rev=0.4, bright=1200, rate=8.0)
    abyss_bed(s, k, prog, notes, 0.4)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        r = bass_note(ch, 38)
        for e in range(8):
            s.add(b0 + e * 0.5, i_bass(midi(r + (12 if e == 6 else 0)), s.secs(0.4), 500), 0, 0.27 if e in (0, 3, 6) else 0.17, 0.05)
        for beat in (0, 2.5):
            s.add(b0 + beat, k["deep_drum"], 0, 0.4, 0.15)
        for beat in (0, 2):
            s.add(b0 + beat, k["heartbeat"], 0, 0.25, 0.1)
        s.add(b0 + 3.5, k["wood_lo"], -0.2, 0.2, 0.2)
        for i in range(16):
            s.add(b0 + i * 0.25, k["shells"], 0.45, 0.05 if i % 2 else 0.08, 0.1)
    return s.render(rt=2.6, wet=0.55, bright=3000)


# ============================================================ Laboratorio

def music_exchange():
    """Sala de cambio (laboratorio de Darwin), para todos los pisos: 16 compases de 6/8."""
    prog = ["Dm", "Dm", "Bb", "Bb", "Gm", "Gm", "A", "A", "Gm", "Gm", "Dm", "Dm", "Bb", "Bb", "A", "A"]
    s = Song2(150, 16 * 6, seed=207, bpb=6)
    k = sea_kit(s.rng)
    mel = [(0, 3, 74), (3, 2, 77), (5, 1, 76), (6, 6, 74), (12, 3, 70), (15, 2, 74), (17, 1, 72), (18, 6, 70),
           (24, 3, 67), (27, 2, 70), (29, 1, 74), (30, 6, 72), (36, 3, 73), (39, 2, 76), (41, 1, 73), (42, 6, 69),
           (48, 3, 70), (51, 2, 74), (53, 1, 72), (54, 6, 70), (60, 3, 69), (63, 2, 72), (65, 1, 74), (66, 6, 77),
           (72, 3, 74), (75, 2, 72), (77, 1, 70), (78, 6, 74), (84, 3, 76), (87, 2, 73), (89, 1, 71), (90, 6, 69)]
    for b, d, m in mel:
        s.add(b, i_glass(midi(m), s.secs(d) * 0.95), 0.1, 0.26, 0.5)
    play_pad(s, prog, inst=i_pad, gain=0.05, lo=57, rev=0.5, bright=900)
    for bar, ch in enumerate(prog):
        b0 = bar * 6
        v = voicing(ch, 50)
        r = bass_note(ch, 38) + 12
        for beat, m in zip(range(6), (r, v[2], r + 12, v[1] + 12, r + 12, v[2])):
            s.add(b0 + beat, i_marimba(midi(m)), -0.2, 0.14 if beat in (0, 3) else 0.09, 0.3)
        s.add(b0 + 3, k["big_bubble"], 0.4, 0.1, 0.4)
        for beat in (0, 3):
            s.add(b0 + beat, k["chitin"], 0.5, 0.05, 0.2)
    bubble_bed(s, 140, 0.05, 300, 1400)
    return s.render(rt=2.0, wet=0.5, bright=4000)


# ============================================================ Anomalocaris

ANOMA_A = ["Dm", "Dm", "Eb", "Dm", "Dm", "Dm", "Bb", "Eb"]
ANOMA_B = ["Gm", "Gm", "Eb", "Eb", "Dm", "Dm", "Cm", "Eb"]
ANOMA_MOTIF = [(0, 62), (0.5, 63), (1, 62), (1.5, 60)]  # re–mi bemol–re–do: el semitono del frigio


def music_boss_anomalocaris():
    """Acecho y cerco: un depredador a la caza, en re frigio a 126 BPM. Batería completa, bajo en semicorcheas con
    el semitono del frigio, melodía amenazante de lead con eco, golpes de acordes y sonar. Estructura A A B A."""
    prog = ANOMA_A + ANOMA_A + ANOMA_B + ANOMA_A
    s = Song2(126, 32 * 4, seed=208)
    k = sea_kit(s.rng)
    kit = make_kit(s.rng)
    play_pad(s, prog, inst=i_tremolo, gain=0.045, lo=50, rev=0.35, bright=2200, rate=13.0)
    melA = [(0, 1, 74), (1, .5, 75), (1.5, .5, 74), (2, 1, 72), (3, 1, 70), (4, 2, 69), (6, 1, 70), (7, 1, 72),
            (8, 1.5, 74), (9.5, .5, 75), (10, 2, 77), (12, 1, 75), (13, 1, 74), (14, 2, 72),
            (16, 1, 74), (17, .5, 75), (17.5, .5, 74), (18, 1, 72), (19, 1, 70), (20, 2, 69), (22, 1, 67),
            (23, 1, 69), (24, 1, 70), (25, 1, 72), (26, 1, 74), (27, 1, 75), (28, 4, 74)]
    melB = section_melody(np.random.default_rng(231), ANOMA_B, 8, 4, 65, 81, D_PHRYGIAN)
    mel = melA + shift(melA, 32) + shift(melB, 64) + shift(melA, 96)
    for b, d, m in mel:
        add_echo(s, b, i_lead(midi(m), s.secs(d) * 0.92, 0.25, 3800, 0.008), 0.1, 0.13, 0.25, every=0.75,
                 fb=0.35, repeats=2, darken=2500)
        if b >= 96:  # la última A, doblada una octava abajo con la marimba
            s.add(b, i_marimba(midi(m - 12)), -0.25, 0.14, 0.25)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        r = bass_note(ch, 38)
        for e, m in enumerate((r, r, r + 1, r, r, r + 12, r, r + 1) * 2):
            g = 0.24 if e % 4 == 0 else 0.15
            s.add(b0 + e * 0.25, i_bass(midi(m), s.secs(0.22), 900), 0, g, 0.03)
            if e % 2 == 0:
                s.add(b0 + e * 0.25, i_saw_bass(midi(m), s.secs(0.2), 800), 0, 0.09, 0.03)
        drums(s, kit, b0, kick=(0, 1.5, 2, 2.75) if bar % 2 else (0, 0.75, 2, 3.5), snare=(1, 3),
              hat_step=0.25, gk=0.34, gs=0.22, gh=0.04)
        for beat in (0, 2):
            s.add(b0 + beat, k["deep_drum"], 0, 0.3, 0.12)
        for beat in (0, 2.5):  # golpes de acorde
            for k2, m in enumerate(voicing(ch, 62)):
                s.add(b0 + beat, i_lead(midi(m), s.secs(0.3), 0.5, 2500, 0), (k2 - 1) * 0.5, 0.045, 0.3)
        for i in range(16):
            s.add(b0 + i * 0.25, k["chitin"], 0.5, 0.07 if i % 4 == 2 else 0.035, 0.1)
        if bar % 4 == 0:
            add_echo(s, b0, k["sonar"], 0.35, 0.07, 0.4, every=1.5, fb=0.5, repeats=3, darken=4000)
        if bar % 8 == 0:
            s.add(b0, kit["crash"], 0, 0.14, 0.3)
        if bar % 8 == 7:  # redoble de toms antes de cada sección
            for e, name in enumerate(("tom_hi", "tom_hi", "tom_lo", "tom_lo")):
                s.add(b0 + 3 + e * 0.25, kit[name], 0.3 - e * 0.2, 0.3, 0.2)
    for bar in list(range(16, 24)) + list(range(24, 32)):
        for k2, m in enumerate(voicing(prog[bar], 62)):
            s.add(bar * 4, i_choir(midi(m), s.secs(3.8)), (k2 - 1) * 0.4, 0.035, 0.5)
    return s.render(rt=1.8, wet=0.4, bright=5000)


def music_boss_anomalocaris_frenzy():
    """Frenesí: la misma idea, a 140 BPM y desatada."""
    prog = ANOMA_A + ANOMA_B + ANOMA_A
    s = Song2(140, 24 * 4, seed=209)
    k = sea_kit(s.rng)
    kit = make_kit(s.rng)
    play_pad(s, prog, inst=i_tremolo, gain=0.06, lo=50, rev=0.35, bright=2500, rate=14.0)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        r = bass_note(ch, 38)
        for e, m in enumerate((r, r, r + 1, r, r, r + 12, r, r + 1)):
            s.add(b0 + e * 0.5, i_bass(midi(m), s.secs(0.4), 800), 0, 0.26, 0.05)
            s.add(b0 + e * 0.5, i_saw_bass(midi(m), s.secs(0.3), 700), 0, 0.12, 0.05)
        for beat in range(4):
            s.add(b0 + beat, k["deep_drum"], 0, 0.32, 0.12)
        drums(s, kit, b0, kick=(), snare=(1, 3), hat_step=0.25, gs=0.2, gh=0.04)
        for rep in range(2):
            for b, m in ANOMA_MOTIF:
                s.add(b0 + rep * 2 + b, i_marimba(midi(m + 12 * rep)), -0.15, 0.17, 0.25)
        if bar % 4 == 0:
            add_echo(s, b0, k["sonar"], 0.3, 0.08, 0.3, every=0.75, fb=0.5, repeats=3, darken=4000)
        if bar % 8 == 0:
            s.add(b0, kit["crash"], 0, 0.12, 0.3)
    return s.render(rt=1.6, wet=0.4, bright=5000)


# ============================================================== Euriptérido

EURY_A = ["Dm", "Dm", "Bb", "Bb", "Gm", "Gm", "A", "A"]
EURY_B = ["Bb", "Bb", "C", "C", "Dm", "Dm", "A", "A"]
EURY_PROG = EURY_A + EURY_A + EURY_B + EURY_A


def music_boss_eurypterid():
    """Pesado y pétreo, pero con empuje: re menor a 120 BPM. Bombo grave sincopado, caja grande, riff de bajo
    distorsionado en corcheas, melodía de lead, coro, y un derrumbe con plato cada 4 compases. A A B A."""
    s = Song2(120, 32 * 4, seed=210)
    k = sea_kit(s.rng)
    kit = make_kit(s.rng)
    play_pad(s, EURY_PROG, inst=i_tremolo, gain=0.04, lo=50, rev=0.35, bright=1800, rate=9.0)
    melA = [(0, 1.5, 74), (1.5, .5, 72), (2, 1, 74), (3, 1, 77), (4, 3, 76), (7, 1, 74),
            (8, 1.5, 74), (9.5, .5, 72), (10, 2, 70), (12, 2, 77), (14, 2, 74),
            (16, 1.5, 70), (17.5, .5, 72), (18, 1, 74), (19, 1, 70), (20, 4, 67),
            (24, 1, 69), (25, 1, 73), (26, 1, 76), (27, 1, 73), (28, 4, 69)]
    melB = section_melody(np.random.default_rng(241), EURY_B, 8, 4, 65, 81, D_AEOLIAN)
    mel = melA + shift(melA, 32) + shift(melB, 64) + shift(melA, 96)
    for b, d, m in mel:
        add_echo(s, b, i_lead(midi(m), s.secs(d) * 0.92, 0.5, 3000, 0.006), 0.1, 0.11, 0.25, every=0.5,
                 fb=0.3, repeats=2, darken=2500)
        s.add(b, i_lead(midi(m - 12), s.secs(d) * 0.92, 0.25, 2000, 0.006), -0.15, 0.06, 0.25)
    for bar, ch in enumerate(EURY_PROG):
        b0 = bar * 4
        r = bass_note(ch, 38)
        for e, m in enumerate((r, r, r + 3, r, r + 5, r + 3, r - 2, r)):
            s.add(b0 + e * 0.5, i_saw_bass(midi(m), s.secs(0.42), 650), 0, 0.2 if e % 2 == 0 else 0.15, 0.04)
            s.add(b0 + e * 0.5, i_bass(midi(m - 12), s.secs(0.42), 400), 0, 0.14, 0.03)
        for beat in (0, 0.75, 2, 2.5):
            s.add(b0 + beat, kit["kick_big"], 0, 0.3, 0.05)
        for beat in (0, 2):
            s.add(b0 + beat, k["deep_drum"], 0, 0.3, 0.12)
        for beat in (1, 3):
            s.add(b0 + beat, kit["snare_big"], 0.05, 0.24, 0.3)
            s.add(b0 + beat, k["stone"], 0.2, 0.22, 0.2)
        for i in range(8):
            s.add(b0 + i * 0.5, kit["hat"], 0.35, 0.06 if i % 2 == 0 else 0.04, 0.05)
            s.add(b0 + i * 0.5 + 0.25, k["chitin"], 0.45, 0.04, 0.1)
        if bar % 4 == 0:  # derrumbe
            s.add(b0, kit["boom"], 0, 0.32, 0.3)
            s.add(b0, kit["crash"], 0, 0.13, 0.3)
            s.add(b0 + 0.05, k["stone"], -0.4, 0.25, 0.4)
        if bar % 8 == 7:
            for e, name in enumerate(("tom_hi", "tom_lo", "tom_hi", "tom_lo", "tom_lo", "tom_lo")):
                s.add(b0 + 2.5 + e * 0.25, kit[name], 0.3 - e * 0.12, 0.32, 0.2)
        if bar >= 8:
            for k2, m in enumerate(voicing(ch, 57)):
                s.add(b0, i_choir(midi(m), s.secs(3.8)), (k2 - 1) * 0.4, 0.035 if bar < 16 else 0.045, 0.5)
    for _ in range(30):
        s.add(s.rng.uniform(0, 128), k["shells"], s.rng.uniform(-0.8, 0.8), 0.05, 0.3)
    return s.render(rt=1.7, wet=0.4, bright=4500)


def music_boss_eurypterid_last_stand():
    """Último aliento: 25,0 s exactos (50 pulsos a 120 BPM) en tres tramos de 8, 8 y 9 s; corte seco."""
    s = Song2(120, 50, loop=False, tail=0.0, seed=212)
    k = sea_kit(s.rng)
    kit = make_kit(s.rng)
    chords = ["Dm", "Bb", "Gm", "A"]
    for bar in range(13):
        b0 = bar * 4
        ch = chords[bar % 4]
        part = 0 if b0 < 16 else 1 if b0 < 32 else 2
        r = bass_note(ch, 38)
        for e in range(8):
            s.add(b0 + e * 0.5, i_saw_bass(midi(r + (12 if e % 4 == 3 else 0)), s.secs(0.4), 600 + 300 * part), 0,
                  0.22, 0.05)
        for k2, m in enumerate(voicing(ch, 50)):
            s.add(b0, i_tremolo(midi(m), s.secs(4), 12 + 3 * part, 1400 + 600 * part), (k2 - 1) * 0.4,
                  0.05 + 0.02 * part, 0.4)
        for beat in range(4):
            s.add(b0 + beat, k["deep_drum"], 0, 0.33, 0.12)
        for beat in (1, 3):
            s.add(b0 + beat, k["stone"], 0.15, 0.28, 0.2)
        if part == 0:  # anillos
            arpeggio(s, b0, ch, [0, 1, 2, 3, 2, 1, 0, 1], 0.5, 0.1, rev=0.25)
        if part >= 1:  # espirales
            arpeggio(s, b0, ch, list(range(6)) * 3 + [5, 4], 0.2, 0.08, rev=0.2)
            drums(s, kit, b0, kick=(), snare=(1, 3), hat_step=0.25, gs=0.18, gh=0.04)
        if part == 2:  # todo a la vez
            drums(s, kit, b0, kick=(0, 0.5, 1.5, 2, 2.5, 3.5), snare=(), hat_step=0, gk=0.25)
            for k2, m in enumerate(voicing(ch, 62)):
                s.add(b0, i_choir(midi(m), s.secs(3.8)), (k2 - 1) * 0.4, 0.05, 0.4)
            s.add(b0, i_lead(midi(voicing(ch, 74)[0]), s.secs(1.8), 0.25, 3500), 0, 0.09, 0.3)
            s.add(b0 + 2, i_lead(midi(voicing(ch, 74)[1]), s.secs(1.8), 0.25, 3500), 0, 0.09, 0.3)
    for i in range(16):  # redoble que crece en el último compás y medio
        s.add(44 + i * 0.375, kit["snare"], 0.1, 0.08 + 0.012 * i, 0.1)
    for beat in (0, 16, 32):
        s.add(beat, kit["crash"], 0, 0.14, 0.3)
    out = s.render(rt=1.6, wet=0.4, bright=5000)
    return np.stack([fade(c, 0, 0.03) for c in out])


MUSIC.update({
    "Music_Floor2_Shallows_Explore": (music_floor2_shallows_explore, True, 0.6),
    "Music_Floor2_Shallows_Combat": (music_floor2_shallows_combat, True, 0.6),
    "Music_Floor2_Reef_Explore": (music_floor2_reef_explore, True, 0.6),
    "Music_Floor2_Reef_Combat": (music_floor2_reef_combat, True, 0.6),
    "Music_Floor2_Abyss_Explore": (music_floor2_abyss_explore, True, 0.6),
    "Music_Floor2_Abyss_Combat": (music_floor2_abyss_combat, True, 0.6),
    "Music_Exchange": (music_exchange, True, 0.6),
    "Music_Boss_Anomalocaris": (music_boss_anomalocaris, True, 0.65),
    "Music_Boss_Anomalocaris_Frenzy": (music_boss_anomalocaris_frenzy, True, 0.65),
    "Music_Boss_Eurypterid": (music_boss_eurypterid, True, 0.65),
    "Music_Boss_Eurypterid_LastStand": (music_boss_eurypterid_last_stand, False, 0.7),
})
