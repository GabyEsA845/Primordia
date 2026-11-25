"""Música del piso 3 (pantano carbonífero), según PROMPT_piso3.md.

Todo en re, como el resto del juego. Cada tramo del incendio que avanza cambia el modo: pantano verde en re dórico
(98 BPM), bosque humeante en re eólico (105 BPM) y tierra quemada en re frigio dominante (re, mi bemol, fa sostenido;
112 BPM). Cada tramo tiene dos capas (exploración y combate) que comparten tempo, progresión, melodía y duración
exacta. Jefes: Meganeura en re frigio (126 → 140 BPM en la tormenta), Arthropleura en re eólico (84 → 105 BPM en la
rueda), Megarachne en re con mi bemol (120 → 126 BPM enfadada) y Lepidodendron en re frigio (72 → 90 BPM). Los
bucles usan Song2 (filtro de graves circular: empalme sin salto)."""

import numpy as np

from instrumentos_piso2 import D_AEOLIAN, D_DORIAN, D_PHRYGIAN, Song2, add_echo, i_marimba, i_tide, i_tremolo, sea_kit
from musica import (CHORDS, MUSIC, Song, bass_note, fade, i_bass, i_choir, i_flute, i_harp, i_lead, i_saw_bass, i_sub,
                    make_kit, play_pad, section_melody, voicing)
from sintesis import (SR, adsr, bp, bubble, click, dist, expenv, glide, hp, lp, midi, n, noise, noise_sweep, saw, sine,
                      swell, thump, tri, tt)

from sintesis import padded  # noqa: E402

# Las señales se suman aunque midan distinto (Sig rellena con ceros), como en efectos_piso2/3.
for _name in ("bp", "bubble", "click", "dist", "expenv", "glide", "hp", "lp", "noise", "saw", "sine", "swell", "thump",
              "tri"):
    globals()[_name] = padded(globals()[_name])

D_PHRYG_DOM = {2, 3, 6, 7, 9, 10, 0}      # tierra quemada: re, mi bemol, fa sostenido… (frigio dominante)


# ================================================================ instrumentos del piso 3

def i_reed(f, d):
    """Flauta de caña: la flauta del piso 1 con aliento de caña y un poco de vibrato tardío."""
    x = i_flute(f, d)
    rng = np.random.default_rng(int(f))
    breath = bp(rng.uniform(-1, 1, len(x)), f * 1.5, min(f * 6, 9000)) * 0.08
    env = np.ones(len(x))
    env[: n(0.04)] = np.linspace(0, 1, n(0.04))
    return (x + breath * env) * 0.9


def i_wood(f, d=None):
    """Marimba de madera del pantano: la marimba del piso 2 con menos concha y más cuerpo."""
    return i_marimba(f, shell=0.35)


def i_brass(f, d, bright=1800, attack=0.12):
    """Metal grave que se hincha (tierra quemada): sierras desafinadas con el filtro que se abre."""
    dd = d + 0.3
    s = (saw(f, dd) + saw(f * 1.006, dd) + saw(f * 0.995, dd)) / 3
    t = tt(dd)
    opening = np.clip(t / max(attack * 3, 0.05), 0, 1)
    x = lp(s, 500) * (1 - opening) + lp(s, bright) * opening
    return dist(x * 0.8, 1.4) * adsr(dd, attack, 0.2, 0.8, 0.3)


def i_wood_organ(f, d):
    """«Órgano» de madera (Lepidodendron): tubos de madera, entrada lenta y un soplo al empezar."""
    dd = d + 0.5
    s = tri(f, dd) + sine(f * 2, dd) * 0.45 + sine(f * 3, dd) * 0.2 + sine(f * 0.5, dd) * 0.3
    rng = np.random.default_rng(int(f * 3))
    chiff = bp(rng.uniform(-1, 1, n(0.08)), f * 2, min(f * 8, 9000)) * expenv(0.08, 0.02, 0.003) * 0.3
    x = s * adsr(dd, 0.09, 0.2, 0.85, 0.5)
    x[: len(chiff)] += chiff
    return lp(x, 3500)


def i_silk(f, d=None, tau=0.5):
    """Cuerda de seda pulsada (Megarachne): armónicos que caen, grave y tenso."""
    dd = tau * 4
    x = sum(sine(f * k, dd) * expenv(dd, tau / k, 0.001) / k for k in (1, 2, 3, 4, 5))
    return lp(x, 2500)


def i_bow(f, d):
    """Madera frotada (el gemido del árbol): sierra con tirones del arco por formantes de madera."""
    dd = d + 0.3
    t = tt(dd)
    stick = 1 + 0.015 * np.sign(np.sin(2 * np.pi * 8 * t))
    x = saw(f * stick, dd)
    x = bp(x, 80, 900) + bp(x, 250, 500) * 0.6
    return dist(x * 0.6, 1.4) * adsr(dd, 0.3, 0.3, 0.8, 0.3)


# ================================================================ percusión del pantano

def swamp_kit(rng):
    """Troncos huecos, croar grave como bombo, semillas, chasquidos de brasa y trueno."""
    k = sea_kit(rng)
    k.update(make_kit(rng))
    for name, f0 in (("log_lo", 110), ("log_mid", 165), ("log_hi", 245)):
        k[name] = sine(glide(f0, f0 * 0.8, 0.35, "fast")) * expenv(0.35, 0.09, 0.001) + \
            sine(glide(f0 * 2.4, f0 * 2.0, 0.12)) * expenv(0.12, 0.025) * 0.35 + click(rng, 0.004, 1800) * 0.25
    t = tt(0.3)
    frog = np.sin(2 * np.pi * 55 * t) * (np.sin(2 * np.pi * 24 * t) > 0) * expenv(0.3, 0.1, 0.002)
    k["frog"] = lp(frog, 400) * 0.8 + thump(110, 50, 0.2, 0.06) * 0.6
    k["seeds"] = bp(noise(0.08, rng), 3000, 9000) * swell(0.08, 0.3) * 0.8
    crack = np.zeros(n(0.12))
    for _ in range(10):
        g = bp(noise(0.004, rng), 1500, 7000) * expenv(0.004, 0.0012, 0.0002)
        i = int(rng.uniform(0, 0.11) * SR)
        crack[i:i + len(g)] += g * rng.uniform(0.3, 1)
    k["ember"] = crack
    k["thunder"] = dist(thump(80, 28, 1.2, 0.4) + lp(noise(1.2, rng), 300) * expenv(1.2, 0.35), 1.8)
    k["chitin_lo"] = bp(noise(0.03, rng), 1000, 3000) * expenv(0.03, 0.006, 0.0003) + thump(300, 180, 0.04, 0.01) * 0.4
    return k


# ================================================================ ayudas

def with_durs(pairs, end):
    out = []
    for i, (b, m) in enumerate(pairs):
        nxt = pairs[i + 1][0] if i + 1 < len(pairs) else end
        out.append((b, nxt - b, m))
    return out


def shift(mel, beats):
    return [(b + beats, d, m) for b, d, m in mel]


def arp(s, b0, ch, pattern, step, gain, inst=i_wood, pan=-0.3, rev=0.3, lo=62):
    tones = voicing(ch, lo) + [m + 12 for m in voicing(ch, lo)]
    for e, idx in enumerate(pattern):
        s.add(b0 + e * step, inst(midi(tones[idx])), pan, gain * (1.3 if e % 4 == 0 else 1.0), rev)


def ostinato(s, b0, notes, step, inst, gain, pan=0.0, rev=0.15, dur=None):
    for e, m in enumerate(notes):
        x = inst(midi(m), s.secs(dur or step * 0.9)) if inst not in (i_wood, i_silk) else inst(midi(m))
        s.add(b0 + e * step, x, pan, gain * (1.25 if e % 4 == 0 else 1.0), rev)


def chord_hits(s, b0, ch, beats, inst, gain, lo=62, dur=0.3, rev=0.3):
    for beat in beats:
        for k2, m in enumerate(voicing(ch, lo)):
            s.add(b0 + beat, inst(midi(m), s.secs(dur)), (k2 - 1) * 0.5, gain, rev)


def melody(s, mel, inst, gain, pan=0.1, rev=0.3, echo=False, transpose=0):
    for b, d, m in mel:
        x = inst(midi(m + transpose), s.secs(d) * 0.92) if inst not in (i_wood, i_silk) else inst(midi(m + transpose))
        if echo:
            add_echo(s, b, x, pan, gain, rev, every=0.75, fb=0.35, repeats=2)
        else:
            s.add(b, x, pan, gain, rev)


# ================================================================ pantano verde (re dórico, 98 BPM)

GREEN_A = ["Dm", "G", "Dm", "C", "Dm", "G", "Am", "C"]
GREEN_B = ["F", "C", "G", "Dm", "Am", "Em", "G", "C"]


def green_material():
    melA = with_durs([(0, 74), (1, 77), (1.5, 76), (2, 74), (4, 71), (5, 72), (6, 74), (8, 74), (9, 76), (10, 77),
                      (11, 79), (12, 81), (14, 79), (16, 77), (17, 76), (18, 74), (20, 71), (21, 72), (22, 69),
                      (24, 72), (25, 71), (26, 69), (28, 74), (30, 76)], 32)
    melB = section_melody(np.random.default_rng(311), GREEN_B, 8, 4, 64, 81, D_DORIAN)
    return GREEN_A + GREEN_A + GREEN_B, melA, shift(melB, 64)


def green_bed(s, k, prog):
    play_pad(s, prog, inst=i_tide, gain=0.06, lo=57, rev=0.5, dark=600, bright=2200)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        r = bass_note(ch, 38)
        s.add(b0, i_sub(midi(r), s.secs(3.5)), 0, 0.18, 0.1)
        s.add(b0, i_wood(midi(r + 12)), 0, 0.16, 0.2)
        s.add(b0 + 2.5, i_wood(midi(r + 12)), 0, 0.1, 0.2)


def music_floor3_green_explore():
    prog, melA, melB = green_material()
    s = Song2(98, 24 * 4, seed=301)
    k = swamp_kit(s.rng)
    green_bed(s, k, prog)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        arp(s, b0, ch, [0, 2, 1, 3, 2, 4, 3, 1], 0.5, 0.09)
        s.add(b0, k["frog"], 0, 0.12, 0.2)
        for beat in (1.5, 3.5):
            s.add(b0 + beat, k["seeds"], 0.4, 0.06, 0.2)
        if bar % 2 == 1:
            s.add(b0 + 3, k["log_hi"], -0.3, 0.09, 0.3)
    melody(s, melA + melB, i_reed, 0.2, echo=True)
    for b, d, m in shift(melA, 32):                     # A': la marimba lleva la melodía, la caña contesta arriba
        add_echo(s, b, i_wood(midi(m)), 0.15, 0.24, 0.35, every=0.75, fb=0.4, repeats=3)
        if d >= 1:
            s.add(b + 0.5, i_reed(midi(m + 12), s.secs(d * 0.5)), -0.3, 0.06, 0.4)
    return s.render(rt=2.2, wet=0.5, bright=5000)


def music_floor3_green_combat():
    prog, melA, melB = green_material()
    s = Song2(98, 24 * 4, seed=302)
    k = swamp_kit(s.rng)
    green_bed(s, k, prog)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        arp(s, b0, ch, [0, 2, 1, 3, 2, 4, 3, 1] * 2, 0.25, 0.06, rev=0.25)
        r = bass_note(ch, 38)
        for e, m in enumerate((r, r, r + 12, r, r + 7, r, r + 12, r + 7)):
            s.add(b0 + e * 0.5, i_bass(midi(m), s.secs(0.4), 900), 0, 0.22 if e % 2 == 0 else 0.16, 0.05)
        for beat in (0, 1.5, 2, 3.25):                  # tambores de tronco en un ritmo sincopado
            s.add(b0 + beat, k["frog"] if beat in (0, 2) else k["log_lo"], 0, 0.3 if beat in (0, 2) else 0.22, 0.12)
        for beat in (1, 3):
            s.add(b0 + beat, k["log_mid"], 0.2, 0.25, 0.15)
        for i in range(8):
            s.add(b0 + i * 0.5, k["seeds"], 0.4, 0.07 if i % 2 else 0.1, 0.05)
        s.add(b0 + 3.5, k["log_hi"], -0.3, 0.16, 0.2)
        if bar % 8 == 0:
            s.add(b0, k["crash"], 0, 0.08, 0.3)
    melody(s, melA + shift(melA, 32) + melB, i_reed, 0.2, echo=True)
    for b, d, m in shift(melA, 32):
        s.add(b, i_wood(midi(m - 12)), -0.25, 0.12, 0.25)
    return s.render(rt=1.7, wet=0.4, bright=6000)


# ================================================================ bosque humeante (re eólico, 105 BPM)

SMOKE_A = ["Dm", "Bb", "Gm", "A", "Dm", "Bb", "C", "A"]
SMOKE_B = ["Gm", "Dm", "Bb", "C", "Gm", "A", "Dm", "A"]


def smoke_material():
    melA = section_melody(np.random.default_rng(321), SMOKE_A, 8, 4, 62, 77, D_AEOLIAN, sparse=True)
    melB = section_melody(np.random.default_rng(322), SMOKE_B, 8, 4, 64, 79, D_AEOLIAN)
    return SMOKE_A + SMOKE_A + SMOKE_B, melA, shift(melB, 64)


def smoke_bed(s, prog, gain=0.07):
    play_pad(s, prog, inst=i_tide, gain=gain, lo=55, rev=0.55, dark=450, bright=1500, rate=0.12)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        r = bass_note(ch, 38)
        s.add(b0, i_sub(midi(r), s.secs(3.8)), 0, 0.2, 0.1)


def music_floor3_smoke_explore():
    prog, melA, melB = smoke_material()
    s = Song2(105, 24 * 4, seed=331)
    k = swamp_kit(s.rng)
    smoke_bed(s, prog)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        r = bass_note(ch, 50)
        ostinato(s, b0, (r, r + 7, r + 12, r + 7) * 2, 0.5, i_tremolo, 0.03, pan=-0.2, rev=0.3)
        s.add(b0, k["log_lo"], 0, 0.14, 0.3)
        s.add(b0 + 2.5, k["log_lo"], 0.1, 0.08, 0.3)
        if bar % 4 == 3:
            s.add(b0 + 3, k["ember"], 0.4, 0.08, 0.4)
    melody(s, melA + shift(melA, 32), i_reed, 0.15, pan=0.15, rev=0.45, echo=True, transpose=-12)
    melody(s, melB, i_wood, 0.18, echo=True)
    return s.render(rt=2.4, wet=0.55, bright=4000)


def music_floor3_smoke_combat():
    prog, melA, melB = smoke_material()
    s = Song2(105, 24 * 4, seed=332)
    k = swamp_kit(s.rng)
    smoke_bed(s, prog, 0.05)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        r = bass_note(ch, 38)
        for e in range(8):
            m = (r, r, r + 12, r)[e % 4]
            s.add(b0 + e * 0.5, i_saw_bass(midi(m), s.secs(0.35), 800), 0, 0.16, 0.05)
            s.add(b0 + e * 0.5, i_bass(midi(m), s.secs(0.4), 700), 0, 0.18, 0.05)
        rr = bass_note(ch, 50)
        ostinato(s, b0, (rr, rr + 7, rr + 12, rr + 7) * 4, 0.25, i_tremolo, 0.025, pan=-0.2, rev=0.25)
        for beat in (0, 0.75, 2, 2.75):
            s.add(b0 + beat, k["log_lo"], 0, 0.3, 0.1)
        for beat in (1, 3):
            s.add(b0 + beat, k["snare"], 0.1, 0.14, 0.2)
            s.add(b0 + beat, k["log_mid"], 0.2, 0.2, 0.15)
        for i in range(8):
            s.add(b0 + i * 0.5 + 0.25, k["seeds"], 0.4, 0.06, 0.05)
        if bar % 8 == 7:
            for e, name in enumerate(("log_hi", "log_hi", "log_mid", "log_lo")):
                s.add(b0 + 3 + e * 0.25, k[name], 0.3 - e * 0.2, 0.25, 0.2)
        if bar % 8 == 0:
            s.add(b0, k["crash"], 0, 0.08, 0.3)
    melody(s, melA + shift(melA, 32) + melB, i_lead, 0.08, rev=0.3, echo=True)
    melody(s, melA + shift(melA, 32), i_wood, 0.12, pan=-0.2, transpose=-12)
    return s.render(rt=1.8, wet=0.45, bright=5000)


# ================================================================ tierra quemada (re frigio dominante, 112 BPM)

BURNT_A = ["D", "Eb", "D", "Cm", "D", "Eb", "Gm", "D"]
BURNT_B = ["Cm", "Eb", "D", "D", "Gm", "Eb", "D", "Eb"]


def burnt_material():
    melA = with_durs([(0, 74), (1, 75), (1.5, 74), (2, 72), (2.5, 70), (3, 69), (4, 66), (5, 67), (6, 69),
                      (8, 70), (9, 69), (10, 67), (11, 66), (12, 63), (13, 62), (16, 74), (17, 75), (18, 78),
                      (19, 79), (20, 81), (22, 79), (23, 78), (24, 75), (25, 74), (26, 72), (27, 70), (28, 69),
                      (30, 66)], 32)
    melB = section_melody(np.random.default_rng(341), BURNT_B, 8, 4, 62, 81, D_PHRYG_DOM)
    return BURNT_A + BURNT_A + BURNT_B, melA, shift(melB, 64)


def burnt_bed(s, k, prog, gain=0.05):
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        r = bass_note(ch, 38)
        s.add(b0, i_sub(midi(r), s.secs(3.6)), 0, 0.18, 0.1)
        for k2, m in enumerate(voicing(ch, 55)):          # metales que se hinchan cada dos compases
            if bar % 2 == 0:
                s.add(b0, i_brass(midi(m), s.secs(7.5), 1400, 0.8), (k2 - 1) * 0.4, gain, 0.45)
        for beat in (0.5, 1.75, 3.25):
            s.add(b0 + beat, k["ember"], 0.4 * (-1) ** bar, 0.08, 0.3)


def music_floor3_burnt_explore():
    prog, melA, melB = burnt_material()
    s = Song2(112, 24 * 4, seed=351)
    k = swamp_kit(s.rng)
    burnt_bed(s, k, prog)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        r = bass_note(ch, 50)
        for e, m in enumerate((r, r + 1, r, r + 12)):
            s.add(b0 + e, i_harp(midi(m), s.secs(0.8)), -0.25, 0.12, 0.35)
        s.add(b0, k["deep_drum"], 0, 0.14, 0.3)
    melody(s, melA + shift(melA, 32), i_reed, 0.18, echo=True)
    melody(s, melB, i_wood, 0.2, echo=True)
    return s.render(rt=2.0, wet=0.45, bright=4500)


def music_floor3_burnt_combat():
    prog, melA, melB = burnt_material()
    s = Song2(112, 24 * 4, seed=352)
    k = swamp_kit(s.rng)
    burnt_bed(s, k, prog, 0.045)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        r = bass_note(ch, 38)
        for e, m in enumerate((r, r, r + 1, r, r + 12, r, r + 1, r)):
            s.add(b0 + e * 0.5, i_bass(midi(m), s.secs(0.4), 800), 0, 0.22, 0.05)
            s.add(b0 + e * 0.5, i_saw_bass(midi(m), s.secs(0.25), 700), 0, 0.1, 0.05)
        for beat in (0, 1.5, 2.5):
            s.add(b0 + beat, k["deep_drum"], 0, 0.28, 0.12)
        for beat in (1, 3):
            s.add(b0 + beat, k["snare_big"], 0.1, 0.13, 0.2)
        for i in range(16):
            s.add(b0 + i * 0.25, k["ember"] if i % 2 else k["hat"], 0.35, 0.06 if i % 4 else 0.09, 0.05)
        chord_hits(s, b0, ch, (0, 2.5), i_brass, 0.035, 62, 0.4)
        if bar % 8 == 0:
            s.add(b0, k["crash"], 0, 0.1, 0.3)
    melody(s, melA + shift(melA, 32) + melB, i_lead, 0.09, echo=True)
    melody(s, melA + shift(melA, 32), i_reed, 0.1, pan=-0.2, transpose=-12)
    return s.render(rt=1.7, wet=0.4, bright=5500)


# ================================================================ Meganeura (re frigio, 126 → 140 BPM)

MEGA_A = ["Dm", "Eb", "Dm", "Cm", "Dm", "Eb", "Bb", "Eb"]
MEGA_B = ["Gm", "Eb", "Cm", "Dm", "Gm", "Eb", "Cm", "Eb"]


def wing_ostinato(s, b0, ch, gain, step=0.25):
    """Las alas: marimba en semicorcheas que tiembla entre la tónica y el semitono del frigio."""
    r = bass_note(ch, 62)
    ostinato(s, b0, (r, r + 1, r, r + 7) * int(1 / step), step, i_wood, gain, pan=-0.3, rev=0.2)


def music_boss_meganeura():
    prog = MEGA_A + MEGA_B + MEGA_A
    s = Song2(126, 24 * 4, seed=361)
    k = swamp_kit(s.rng)
    play_pad(s, prog, inst=i_tremolo, gain=0.04, lo=50, rev=0.35, bright=2200, rate=12.0)
    melA = with_durs([(0, 74), (1, 75), (2, 77), (3, 75), (4, 74), (6, 70), (8, 72), (9, 74), (10, 75), (12, 79),
                      (14, 77), (16, 74), (17, 75), (18, 74), (19, 72), (20, 70), (22, 69), (24, 70), (26, 72),
                      (28, 74), (30, 75)], 32)
    melB = section_melody(np.random.default_rng(362), MEGA_B, 8, 4, 65, 82, D_PHRYGIAN)
    melody(s, melA + shift(melB, 32) + shift(melA, 64), i_lead, 0.12, echo=True)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        wing_ostinato(s, b0, ch, 0.07)
        r = bass_note(ch, 38)
        for e, m in enumerate((r, r, r + 1, r) * 2):
            s.add(b0 + e * 0.5, i_bass(midi(m), s.secs(0.4), 900), 0, 0.22, 0.05)
        for beat in (0, 1.5, 2, 3.5):
            s.add(b0 + beat, k["kick"], 0, 0.3, 0.05)
        for beat in (1, 3):
            s.add(b0 + beat, k["snare"], 0.1, 0.18, 0.15)
        for i in range(8):
            s.add(b0 + i * 0.5, k["hat"], 0.35, 0.05, 0.05)
        if bar % 8 == 0:
            s.add(b0, k["crash"], 0, 0.12, 0.3)
        if bar % 8 == 7:
            for e in range(4):
                s.add(b0 + 3 + e * 0.25, k["tom_hi" if e < 2 else "tom_lo"], 0.3 - e * 0.2, 0.28, 0.2)
    return s.render(rt=1.8, wet=0.4, bright=5000)


def music_boss_meganeura_storm():
    prog = MEGA_A + MEGA_B + MEGA_A + MEGA_B[:4]
    bars = len(prog)
    s = Song2(140, bars * 4, seed=363)
    k = swamp_kit(s.rng)
    play_pad(s, prog, inst=i_tremolo, gain=0.06, lo=50, rev=0.4, bright=2800, rate=15.0)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        wing_ostinato(s, b0, ch, 0.08, 0.25)
        r = bass_note(ch, 38)
        for e, m in enumerate((r, r + 1, r, r + 1, r, r + 12, r + 1, r) * 2):
            s.add(b0 + e * 0.25, i_bass(midi(m), s.secs(0.22), 900), 0, 0.2 if e % 4 == 0 else 0.13, 0.03)
        for beat in range(4):
            s.add(b0 + beat, k["kick_big"], 0, 0.22, 0.08)
        for beat in (1, 3):
            s.add(b0 + beat, k["snare_big"], 0.1, 0.18, 0.2)
        for i in range(16):
            s.add(b0 + i * 0.25, k["hat"], 0.35, 0.045, 0.05)
        chord_hits(s, b0, ch, (0, 1.5, 3), i_lead, 0.04, 62, 0.25)
        if bar % 3 == 1:                                    # truenos sueltos, fuera de compás
            s.add(b0 + s.rng.choice([0.5, 1.75, 2.25, 3.5]), k["thunder"], s.rng.uniform(-0.6, 0.6), 0.35, 0.5)
        if bar % 4 == 0:
            for k2, m in enumerate((62, 63, 69, 70)):        # racimo disonante que entra con la tormenta
                s.add(b0, i_choir(midi(m), s.secs(3.6)), (k2 - 1.5) * 0.4, 0.035, 0.5)
        if bar % 8 == 0:
            s.add(b0, k["crash"], 0, 0.14, 0.3)
    mel = section_melody(np.random.default_rng(364), prog, bars, 4, 67, 86, D_PHRYGIAN)
    melody(s, mel, i_lead, 0.1, echo=True)
    rain = hp(noise(s.N / SR, s.rng), 4000) * 0.05
    s.add(0, rain, 0, 0.5, 0.2)
    return s.render(rt=2.0, wet=0.45, bright=5500, target_db=-18)


# ================================================================ Arthropleura (re eólico, 84 → 105 BPM)

ARTH_PROG = ["Dm", "Dm", "Bb", "A", "Dm", "Gm", "Bb", "A", "Gm", "Gm", "Dm", "Dm", "Bb", "C", "A", "A"]


def legs_roll(s, k, b0, gain, density=1.0):
    """Las patas: toms en olas de semicorcheas que van y vienen."""
    for i in range(16):
        if s.rng.random() > density:
            continue
        wave = 0.5 + 0.5 * np.sin(2 * np.pi * i / 16)
        s.add(b0 + i * 0.25, k["log_lo"] if i % 3 else k["log_mid"], (wave - 0.5) * 0.8, gain * (0.4 + 0.6 * wave), 0.1)


def music_boss_arthropleura():
    prog = ARTH_PROG
    s = Song2(84, len(prog) * 4, seed=371)
    k = swamp_kit(s.rng)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        r = bass_note(ch, 38)
        if bar % 2 == 0:
            for k2, m in enumerate(voicing(ch, 50)):
                s.add(b0, i_brass(midi(m), s.secs(7.6), 1100, 1.2), (k2 - 1) * 0.4, 0.06, 0.4)
        s.add(b0, k["kick_big"], 0, 0.4, 0.15)
        s.add(b0 + 2, k["deep_drum"], 0, 0.35, 0.15)
        s.add(b0 + 3.5, k["deep_drum"], 0, 0.2, 0.15)
        for e, m in enumerate((r, r, r + 12, r)):
            s.add(b0 + e, i_saw_bass(midi(m), s.secs(0.8), 600), 0, 0.2, 0.05)
        legs_roll(s, k, b0, 0.22, 0.85)
        for beat in (1, 3):
            s.add(b0 + beat, k["chitin_lo"], 0.3, 0.14, 0.15)
    mel = with_durs([(0, 62), (2, 65), (3, 64), (4, 62), (8, 70), (10, 69), (12, 67), (14, 69), (16, 74), (19, 72),
                     (20, 70), (22, 69), (24, 67), (26, 69), (28, 62)], 32)
    melody(s, mel + shift(mel, 32), i_brass, 0.07, pan=0.1, rev=0.4)
    return s.render(rt=2.0, wet=0.4, bright=4000, target_db=-17.5)


def music_boss_arthropleura_coil():
    prog = ARTH_PROG + ["Dm", "Bb", "A", "A"]
    s = Song2(105, len(prog) * 4, seed=372)
    k = swamp_kit(s.rng)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        r = bass_note(ch, 38)
        for e in range(8):
            m = (r, r + 12, r, r + 7)[e % 4]
            s.add(b0 + e * 0.5, i_saw_bass(midi(m), s.secs(0.4), 700), 0, 0.17, 0.05)
        for beat in (0, 1, 2, 3):
            s.add(b0 + beat, k["kick_big"], 0, 0.25, 0.1)
        for beat in (1, 3):
            s.add(b0 + beat, k["snare_big"], 0.1, 0.16, 0.2)
        legs_roll(s, k, b0, 0.28, 1.0)
        chord_hits(s, b0, ch, (0, 2.5), i_brass, 0.045, 55, 0.6, 0.35)
        if bar % 4 == 0:
            s.add(b0, k["crash"], 0, 0.1, 0.3)
    mel = section_melody(np.random.default_rng(373), prog, len(prog), 4, 62, 79, D_AEOLIAN)
    melody(s, mel, i_lead, 0.09, echo=True)
    return s.render(rt=1.7, wet=0.4, bright=4500, target_db=-17.5)


# ================================================================ Megarachne (re con mi bemol, 120 → 126 BPM)

ARACH_A = ["Dm", "Dm", "Eb", "Dm", "Gm", "Eb", "Dm", "Eb"]
ARACH_B = ["Cm", "Cm", "Eb", "Eb", "Gm", "Gm", "Eb", "D"]


def silk_ostinato(s, b0, ch, gain, step=0.5):
    r = bass_note(ch, 38) + 12
    ostinato(s, b0, (r, r + 1, r + 7, r + 1) * int(2 / step), step, i_silk, gain, pan=-0.2, rev=0.25)


def music_boss_megarachne():
    prog = ARACH_A + ARACH_B + ARACH_A
    s = Song2(120, 24 * 4, seed=381)
    k = swamp_kit(s.rng)
    play_pad(s, prog, inst=i_tremolo, gain=0.035, lo=50, rev=0.4, bright=1800, rate=9.0)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        silk_ostinato(s, b0, ch, 0.16)
        for beat in (0, 2.5):
            s.add(b0 + beat, k["kick"], 0, 0.28, 0.05)
        s.add(b0 + 2, k["snare"], 0.1, 0.15, 0.2)
        for i in range(16):
            s.add(b0 + i * 0.25, k["chitin"], 0.4, 0.05 if i % 4 else 0.08, 0.1)
        if bar % 8 == 0:
            s.add(b0, k["crash"], 0, 0.1, 0.3)
    melA = with_durs([(0, 74), (1.5, 75), (2, 74), (4, 69), (6, 70), (8, 74), (9.5, 75), (10, 77), (12, 75),
                      (14, 74), (16, 70), (17, 69), (18, 67), (20, 63), (22, 62), (24, 74), (26, 75), (28, 74)], 32)
    melB = section_melody(np.random.default_rng(382), ARACH_B, 8, 4, 63, 79, D_PHRYGIAN)
    melody(s, melA + shift(melB, 32) + shift(melA, 64), i_glass_like, 0.16, echo=True)
    return s.render(rt=1.8, wet=0.42, bright=4500)


def i_glass_like(f, d):
    """Voz aguda y fina para la Megarachne: seno con batido, como los ojos que destellan."""
    dd = d + 0.4
    x = sine(f, dd) + sine(f * 1.004, dd) * 0.5 + sine(f * 2, dd) * 0.2
    return x * adsr(dd, 0.02, 0.2, 0.7, 0.4)


def music_boss_megarachne_rage():
    prog = ARACH_A + ARACH_B + ARACH_A
    s = Song2(126, 24 * 4, seed=383)
    k = swamp_kit(s.rng)
    play_pad(s, prog, inst=i_tremolo, gain=0.05, lo=50, rev=0.35, bright=2400, rate=14.0)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        silk_ostinato(s, b0, ch, 0.16, 0.25)
        r = bass_note(ch, 38)
        for e, m in enumerate((r, r + 1, r, r) * 2):
            s.add(b0 + e * 0.5, i_saw_bass(midi(m), s.secs(0.35), 800), 0, 0.14, 0.05)
        for beat in (0, 1, 2, 3):
            s.add(b0 + beat, k["kick"], 0, 0.28, 0.05)
        for beat in (1, 3):
            s.add(b0 + beat, k["snare_big"], 0.1, 0.16, 0.2)
        for i in range(16):
            s.add(b0 + i * 0.25, k["chitin"], 0.4, 0.06 if i % 2 else 0.09, 0.1)
        if bar % 8 == 0:
            s.add(b0, k["crash"], 0, 0.12, 0.3)
        if bar % 8 == 7:
            for e in range(4):
                s.add(b0 + 3 + e * 0.25, k["tom_hi" if e < 2 else "tom_lo"], 0.3 - e * 0.2, 0.28, 0.2)
    mel = section_melody(np.random.default_rng(384), prog, 24, 4, 65, 84, D_PHRYGIAN)
    melody(s, mel, i_lead, 0.1, echo=True)
    return s.render(rt=1.7, wet=0.4, bright=5000)


# ================================================================ Lepidodendron (re frigio, 72 → 90 BPM)

LEPI_PROG = ["Dm", "Eb", "Dm", "Bb", "Gm", "Eb", "Cm", "Dm", "Dm", "Eb", "Gm", "Eb", "Bb", "Cm", "Eb", "Dm"]


def music_boss_lepidodendron():
    prog = LEPI_PROG
    s = Song2(72, len(prog) * 4, seed=391)
    k = swamp_kit(s.rng)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        for k2, m in enumerate(voicing(ch, 55)):
            s.add(b0, i_wood_organ(midi(m), s.secs(3.8)), (k2 - 1) * 0.4, 0.05, 0.55)
            s.add(b0, i_choir(midi(m + 12), s.secs(3.8)), (1 - k2) * 0.4, 0.03, 0.6)
        r = bass_note(ch, 38)
        s.add(b0, i_sub(midi(r), s.secs(3.8)), 0, 0.2, 0.1)
        s.add(b0, k["deep_drum"], 0, 0.3, 0.3)
        s.add(b0 + 2.5, k["log_lo"], 0, 0.15, 0.3)
        if bar % 4 == 1:
            s.add(b0 + 1, i_bow(midi(r), s.secs(2.5)), 0.3, 0.06, 0.5)    # el gemido del árbol
    mel = with_durs([(0, 74), (2, 75), (3, 74), (4, 70), (6, 72), (8, 74), (10, 77), (11, 75), (12, 74), (16, 74),
                     (18, 75), (20, 79), (22, 77), (24, 75), (26, 74), (28, 72), (30, 74)], 32)
    melody(s, mel + shift(mel, 32), i_reed, 0.14, rev=0.5, echo=True)
    return s.render(rt=2.6, wet=0.55, bright=4000)


def music_boss_lepidodendron_final():
    prog = LEPI_PROG + ["Gm", "Eb", "Cm", "Dm"]
    s = Song2(90, len(prog) * 4, seed=392)
    k = swamp_kit(s.rng)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        r = bass_note(ch, 38)
        for k2, m in enumerate(voicing(ch, 55)):
            s.add(b0, i_choir(midi(m + 12), s.secs(3.8)), (k2 - 1) * 0.4, 0.04, 0.55)
        organ = [voicing(ch, 62)[i % 3] + (12 if i % 6 >= 3 else 0) for i in range(8)]
        ostinato(s, b0, organ, 0.5, i_wood_organ, 0.04, pan=-0.2, rev=0.35, dur=0.45)
        for e, m in enumerate((r, r, r + 1, r) * 2):
            s.add(b0 + e * 0.5, i_bass(midi(m), s.secs(0.45), 700), 0, 0.2, 0.05)
        for beat in (0, 1.5, 2, 3):
            s.add(b0 + beat, k["deep_drum"] if beat in (0, 2) else k["log_lo"], 0, 0.3, 0.12)
        for beat in (1, 3):
            s.add(b0 + beat, k["snare_big"], 0.1, 0.13, 0.25)
        for i in range(8):
            s.add(b0 + i * 0.5, k["seeds"], 0.4, 0.06, 0.05)
        if bar % 4 == 0:
            s.add(b0, i_bow(midi(r), s.secs(3)), 0.3, 0.05, 0.5)
        if bar % 8 == 0:
            s.add(b0, k["crash"], 0, 0.1, 0.35)
    mel = section_melody(np.random.default_rng(393), prog, len(prog), 4, 62, 81, D_PHRYGIAN)
    melody(s, mel, i_lead, 0.09, echo=True)
    return s.render(rt=2.2, wet=0.45, bright=4500)


# ================================================================ fragmentos

def sting_secret_boss():
    """Presentación de jefe secreto: golpe grave, acorde frigio morado que se abre en el coro y campanas."""
    s = Song(90, 5, loop=False, tail=1.5, seed=395)         # ~3,3 s
    k = swamp_kit(s.rng)
    s.add(0, k["boom"], 0, 0.8, 0.5)
    for k2, m in enumerate((50, 51, 57, 62, 63)):
        s.add(0, i_choir(midi(m), 3.0), (k2 - 2) * 0.3, 0.07, 0.6)
        s.add(0, i_wood_organ(midi(m), 2.8), (2 - k2) * 0.3, 0.05, 0.5)
    for i, m in enumerate((74, 75, 81, 86)):
        s.add(1 + i * 0.5, i_glass_like(midi(m), 1.2), (-0.4, 0.4)[i % 2], 0.08, 0.6)
    out = s.render(rt=2.4, wet=0.5, bright=5000, target_db=-17)
    return np.stack([fade(c, 0, 0.6) for c in out])


def sting_relic():
    """Reliquia: acorde mayor amplio en re (fa sostenido), arpa que sube y brillo dorado."""
    s = Song(90, 4, loop=False, tail=1.6, seed=396)         # ~3 s
    for k2, m in enumerate((50, 57, 62, 66, 69, 74)):
        s.add(0, i_choir(midi(m), 2.6), (k2 - 2.5) * 0.25, 0.06, 0.6)
    for i, m in enumerate((62, 66, 69, 74, 78, 81, 86)):
        s.add(i * 0.25, i_harp(midi(m), 1.5), (i / 3 - 1), 0.12, 0.5)
    s.add(1.8, sine(midi(93), 1.2) * expenv(1.2, 0.4, 0.01), 0.2, 0.1, 0.7)
    out = s.render(rt=2.4, wet=0.5, bright=6000, target_db=-16)
    return np.stack([fade(c, 0, 0.6) for c in out])


MUSIC.update({
    "Music_Floor3_Green_Explore": (music_floor3_green_explore, True, 0.6),
    "Music_Floor3_Green_Combat": (music_floor3_green_combat, True, 0.6),
    "Music_Floor3_Smoke_Explore": (music_floor3_smoke_explore, True, 0.6),
    "Music_Floor3_Smoke_Combat": (music_floor3_smoke_combat, True, 0.6),
    "Music_Floor3_Burnt_Explore": (music_floor3_burnt_explore, True, 0.6),
    "Music_Floor3_Burnt_Combat": (music_floor3_burnt_combat, True, 0.6),
    "Music_Boss_Meganeura": (music_boss_meganeura, True, 0.65),
    "Music_Boss_Meganeura_Storm": (music_boss_meganeura_storm, True, 0.65),
    "Music_Boss_Arthropleura": (music_boss_arthropleura, True, 0.65),
    "Music_Boss_Arthropleura_Coil": (music_boss_arthropleura_coil, True, 0.65),
    "Music_Boss_Megarachne": (music_boss_megarachne, True, 0.65),
    "Music_Boss_Megarachne_Rage": (music_boss_megarachne_rage, True, 0.65),
    "Music_Boss_Lepidodendron": (music_boss_lepidodendron, True, 0.65),
    "Music_Boss_Lepidodendron_Final": (music_boss_lepidodendron_final, True, 0.65),
    "Music_Sting_SecretBoss": (sting_secret_boss, False, 0.7),
    "Music_Sting_Relic": (sting_relic, False, 0.7),
})
