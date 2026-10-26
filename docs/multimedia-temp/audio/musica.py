"""Música de Primordia: un secuenciador pequeño, instrumentos chiptune y las 11 pistas.

Todas las pistas están en re menor (o su relativo, fa mayor, en la tienda). Los
bucles duran un número exacto de compases y tanto las notas que se salen del final
como la reverberación vuelven al principio, así el empalme no se oye."""

import numpy as np
from scipy import signal

from sintesis import (SR, adsr, bp, bubble, circular_conv, click, dist, expenv, fade, fm, glide,
                      hp, impulse_response, lp, midi, n, noise, noise_sweep, rms_db, saw, sine,
                      soft_limit, square, svf, swell, thump, tri, vibrato)
from sintesis import padded

for _name in ("adsr", "bp", "bubble", "click", "dist", "expenv", "fade", "fm", "glide", "hp", "lp",
              "noise", "noise_sweep", "saw", "sine", "square", "svf", "swell", "thump", "tri", "vibrato"):
    globals()[_name] = padded(globals()[_name])


# ================================================================ secuenciador

class Song:
    def __init__(self, bpm, beats, loop=True, tail=0.0, seed=0, bpb=4):
        spb = SR * 60 / bpm
        assert abs(spb - round(spb)) < 1e-6, f"{bpm} BPM no da un pulso entero de muestras"
        self.spb = int(round(spb))
        self.beat = self.spb / SR
        self.bpb = bpb
        self.N = self.spb * beats
        self.loop = loop
        self.total = self.N + (n(tail) if not loop and tail > 0 else 0)
        self.dry = np.zeros((2, self.total))
        self.wet = np.zeros((2, self.total))
        self.rng = np.random.default_rng(seed)

    def _put(self, buf, x, i):
        L = self.total
        if not self.loop:
            if i >= L:
                return
            end = min(L, i + len(x))
            buf[i:end] += x[: end - i]
            return
        i %= L
        while len(x):
            take = min(len(x), L - i)
            buf[i:i + take] += x[:take]
            x = x[take:]
            i = 0

    def add(self, beat, x, pan=0.0, gain=1.0, rev=0.2):
        i = int(round(beat * self.spb))
        a = (pan + 1) * np.pi / 4
        gl, gr = np.cos(a) * np.sqrt(2), np.sin(a) * np.sqrt(2)
        for ch, g in ((0, gl), (1, gr)):
            self._put(self.dry[ch], x * gain * g, i)
            if rev > 0:
                self._put(self.wet[ch], x * gain * g * rev, i)

    def secs(self, beats):
        return beats * self.beat

    def render(self, rt=1.6, wet=0.5, bright=5000, target_db=-17.0):
        out = self.dry.copy()
        for ch in range(2):
            ir = impulse_response(rt, self.rng, bright=bright)
            if self.loop:
                out[ch] += circular_conv(self.wet[ch], ir) * wet
            else:
                out[ch] += signal.fftconvolve(self.wet[ch], ir)[: self.total] * wet
        out -= out.mean(axis=1, keepdims=True)
        out = hp_stereo(out, 30)
        gain = 10 ** ((target_db - rms_db(out)) / 20)
        return soft_limit(out * gain)


def hp_stereo(x, fc):
    if x.shape[1] < SR:
        return x
    sos = signal.butter(1, fc / (SR / 2), "highpass", output="sos")
    return np.stack([signal.sosfiltfilt(sos, c) for c in x])


# ================================================================ instrumentos

def i_pad(f, d, bright=1400, attack=0.25, release=0.5):
    dd = d + release
    s = sum(saw(f * r, dd) for r in (1.0, 1.004, 0.996)) / 3
    return lp(s, bright) * adsr(dd, attack, 0.3, 0.8, release)


def i_choir(f, d):
    dd = d + 0.4
    s = sum(saw(vibrato(f * r, dd, 5, 0.006, 0.1), dd) for r in (1.0, 1.006))
    s = bp(s, 400, 1600) + bp(s, 2200, 2800) * 0.3
    return s * adsr(dd, 0.15, 0.2, 0.85, 0.4)


def i_bell(f, d, tau=0.5, ratio=3.0, index=1.0):
    dd = max(d, tau * 3.5)
    return fm(f, dd, ratio, index, tau, tau * 0.4) + sine(f * 2, dd) * expenv(dd, tau * 0.5) * 0.15


def i_crystal(f, d):
    dd = 0.35
    return fm(f, dd, 3.01, 1.4, 0.09, 0.04)


def i_pluck(f, d, tau=0.18):
    dd = d + 0.2
    s = saw(f, dd) * 0.6 + square(f, dd, 0.3) * 0.4
    return hp(lp(s, 2800), 150) * expenv(dd, tau, 0.002)


def i_harp(f, d):
    dd = max(d, 1.0)
    s = tri(f, dd) + sine(f * 2, dd) * 0.3
    return lp(s, 4000) * expenv(dd, 0.35, 0.002)


def i_lead(f, d, duty=0.25, bright=4000, depth=0.005):
    dd = d + 0.08
    s = square(vibrato(f, dd, 5.5, depth, 0.15), dd, duty)
    return lp(s, bright) * adsr(dd, 0.01, 0.08, 0.7, 0.08)


def i_flute(f, d):
    dd = d + 0.15
    s = sine(vibrato(f, dd, 5, 0.006, 0.2), dd) + tri(f * 2, dd) * 0.08
    return s * adsr(dd, 0.07, 0.1, 0.85, 0.15)


def i_bass(f, d, bright=900):
    dd = d + 0.03
    s = tri(f, dd) + lp(square(f, dd, 0.5), bright) * 0.35
    return s * adsr(dd, 0.004, 0.1, 0.75, 0.03)


def i_saw_bass(f, d, bright=900):
    dd = d + 0.02
    s = saw(f, dd) + square(f / 2, dd, 0.5) * 0.4
    return dist(lp(s, bright), 1.5) * adsr(dd, 0.003, 0.05, 0.6, 0.02)


def i_sub(f, d):
    dd = d + 0.3
    return sine(f, dd) * adsr(dd, 0.05, 0.2, 0.9, 0.3)


def i_wobble(f, d, rate):
    dd = d + 0.05
    t = np.arange(n(dd)) / SR
    s = saw(f, dd) + saw(f * 1.005, dd) * 0.6 + sine(f / 2, dd) * 0.8
    fc = 180 + 1100 * (0.5 - 0.5 * np.cos(2 * np.pi * rate * t))
    return dist(svf(s * 0.6, fc, 2.5), 1.5) * adsr(dd, 0.01, 0.1, 0.85, 0.05)


# --------------------------------------------------------------- percusión

def make_kit(rng):
    k = {}
    k["kick"] = thump(170, 45, 0.3, 0.09) + click(rng, 0.004, 2000) * 0.3
    k["kick_soft"] = lp(thump(120, 45, 0.25, 0.07), 300)
    k["kick_big"] = dist(thump(150, 35, 0.6, 0.18) + lp(noise(0.1, rng), 600) * expenv(0.1, 0.03), 1.6)
    k["snare"] = bp(noise(0.18, rng), 900, 7000) * expenv(0.18, 0.055) + tri(190, 0.1) * expenv(0.1, 0.03) * 0.6
    k["snare_big"] = bp(noise(0.35, rng), 600, 6000) * expenv(0.35, 0.1) + tri(160, 0.15) * expenv(0.15, 0.05) * 0.7
    k["hat"] = hp(noise(0.04, rng), 7000) * expenv(0.04, 0.012)
    k["hat_open"] = hp(noise(0.2, rng), 6000) * expenv(0.2, 0.07)
    k["shaker"] = bp(noise(0.07, rng), 4000, 10000) * swell(0.07, 0.3)
    k["crash"] = hp(noise(1.5, rng), 3500) * expenv(1.5, 0.45, 0.002)
    k["tom_hi"] = sine(glide(260, 160, 0.25)) * expenv(0.25, 0.08)
    k["tom_lo"] = sine(glide(160, 90, 0.3)) * expenv(0.3, 0.1)
    k["boom"] = dist(thump(110, 30, 0.9, 0.3) + lp(noise(0.4, rng), 500) * expenv(0.4, 0.12), 1.8)
    return k


def drop(rng, lo=500, hi=1000):
    return bubble(np.exp(rng.uniform(np.log(lo), np.log(hi))), 0.12, 1.6, 0.04)


# ================================================================= armonía

CHORDS = {  # nota grave (midi) y tipo
    "Dm": (50, (0, 3, 7)), "Bb": (46, (0, 4, 7)), "F": (53, (0, 4, 7)), "C": (48, (0, 4, 7)),
    "Gm": (55, (0, 3, 7)), "A": (45, (0, 4, 7)), "Eb": (51, (0, 4, 7)), "D": (50, (0, 4, 7)),
}
D_MINOR = {2, 4, 5, 7, 9, 10, 0}
F_MAJOR = {5, 7, 9, 10, 0, 2, 4}  # mismas notas: la tienda gira alrededor de fa
D_PHRYGIAN = {2, 3, 5, 7, 9, 10, 0}  # con mi bemol, para la ameba


def pcs(ch):
    root, q = CHORDS[ch]
    return {(root + i) % 12 for i in q}


def voicing(ch, lo=57):
    """Las notas del acorde dentro de la octava que empieza en lo."""
    return sorted(lo + ((p - lo) % 12) for p in pcs(ch))


def bass_note(ch, lo=38):
    root = CHORDS[ch][0] % 12
    return lo + ((root - lo) % 12)


# ================================================================== melodías

RHYTHMS_44 = [
    [(0, 1), (1, .5), (1.5, .5), (2, 1), (3, 1), (4, 1.5), (5.5, .5), (6, 2)],
    [(0, .5), (.5, .5), (1, 1), (2, .5), (2.5, 1.5), (4, 1), (5, 1), (6, 1), (7, 1)],
    [(0, 1.5), (1.5, .5), (2, 2), (4, .5), (4.5, .5), (5, 1), (6, 2)],
    [(0, .75), (.75, .75), (1.5, .5), (2, 2), (4, .75), (4.75, .75), (5.5, .5), (6, 1), (7, 1)],
]
RHYTHMS_44_SPARSE = [
    [(0, 2), (2, 1), (3, 1), (4, 3), (7, 1)],
    [(0, 3), (3, 1), (4, 2), (6, 2)],
    [(1, 1), (2, 2), (4, 1.5), (5.5, .5), (6, 2)],
]
CADENCE_44 = [(0, 1), (1, 1), (2, 2), (4, 4)]
RHYTHMS_34 = [
    [(0, 1), (1, 1), (2, 1), (3, 2), (5, 1)],
    [(0, 1.5), (1.5, .5), (2, 1), (3, 1), (4, 1), (5, 1)],
    [(0, 2), (2, 1), (3, 1.5), (4.5, .5), (5, 1)],
]
CADENCE_34 = [(0, 1), (1, 1), (2, 1), (3, 3)]


def scale_notes(lo, hi, scale):
    return [m for m in range(lo, hi + 1) if m % 12 in scale]


def contour(rng, k):
    steps = [0] + list(rng.choice([-2, -1, -1, 0, 1, 1, 1, 2, 3, -3], size=k - 1))
    return steps


def phrase(rhythm, steps, prog, start_bar, bpb, notes, cur, final_root=None):
    out = []
    for j, (b, dur) in enumerate(rhythm):
        bar = start_bar + int(b // bpb)
        chord = pcs(prog[bar % len(prog)])
        idx = int(np.clip(cur + steps[j], 0, len(notes) - 1))
        strong = (b % bpb == 0) or dur >= 1.5
        last = j == len(rhythm) - 1
        if last and final_root is not None:
            target = {final_root}
        elif strong:
            target = chord
        else:
            target = None
        if target:
            cands = [i for i, m in enumerate(notes) if m % 12 in target]
            idx = min(cands, key=lambda i: (abs(i - idx), i))
        out.append((start_bar * bpb + b, dur, notes[idx]))
        cur = idx
    return out, cur


def section_melody(rng, prog, bars, bpb, lo, hi, scale, sparse=False, cur=None):
    """Melodía de una sección: motivo, motivo repetido, otra idea y cadencia (cada 8 compases)."""
    notes = scale_notes(lo, hi, scale)
    cur = len(notes) // 2 if cur is None else cur
    bank = (RHYTHMS_34 if bpb == 3 else RHYTHMS_44_SPARSE if sparse else RHYTHMS_44)
    cad = CADENCE_34 if bpb == 3 else CADENCE_44
    out = []
    for blk in range(0, bars, 8):
        r1 = bank[rng.integers(len(bank))]
        c1 = contour(rng, len(r1))
        r2 = bank[rng.integers(len(bank))]
        c2 = contour(rng, len(r2))
        root = CHORDS[prog[(blk + 7) % len(prog)]][0] % 12
        for k, (r, c) in enumerate(((r1, c1), (r1, c1), (r2, c2), (cad, contour(rng, len(cad))))):
            if blk + k * 2 >= bars:
                break
            ph, cur = phrase(r, c, prog, blk + k * 2, bpb, notes, cur, root if k == 3 else None)
            out += ph
    return out


def play_melody(song, mel, inst, offset=0, gain=0.3, pan=0.0, rev=0.3, transpose=0):
    for b, dur, m in mel:
        song.add(offset + b, inst(midi(m + transpose), song.secs(dur) * 0.95), pan, gain, rev)


def play_pad(song, prog, bars_per_chord=1, inst=i_pad, gain=0.2, lo=57, rev=0.5, **kw):
    bpb = song.bpb
    for i, ch in enumerate(prog):
        d = song.secs(bpb * bars_per_chord)
        for k, m in enumerate(voicing(ch, lo)):
            song.add(i * bpb * bars_per_chord, inst(midi(m), d, **kw), (k - 1) * 0.35, gain, rev)


def bubble_bed(song, count, gain=0.08, lo=500, hi=2000):
    rng = song.rng
    total_beats = song.N / song.spb
    for _ in range(count):
        f = np.exp(rng.uniform(np.log(lo), np.log(hi)))
        song.add(rng.uniform(0, total_beats), bubble(f, rng.uniform(0.05, 0.12), 1.6),
                 rng.uniform(-0.8, 0.8), gain * rng.uniform(0.5, 1), 0.5)


# =================================================================== pistas

def floor1_material():
    rng = np.random.default_rng(101)
    progA = ["Dm", "Bb", "F", "C", "Dm", "Bb", "Gm", "A"]
    progB = ["Gm", "C", "F", "Dm", "Bb", "C", "Gm", "A"]
    prog = progA + progA + progB + progA
    melA = section_melody(rng, progA, 8, 4, 69, 86, D_MINOR)
    melA2 = [x for x in melA if x[0] < 16] + [x for x in section_melody(rng, progA, 8, 4, 69, 86, D_MINOR) if x[0] >= 16]
    melB = section_melody(rng, progB, 8, 4, 67, 84, D_MINOR)
    mel = melA + [(b + 32, d, m) for b, d, m in melA2] + [(b + 64, d, m) for b, d, m in melB] + \
        [(b + 96, d, m) for b, d, m in melA]
    return prog, mel


def music_floor1_explore():
    prog, mel = floor1_material()
    s = Song(100, 32 * 4, seed=1)
    kit = make_kit(s.rng)
    play_pad(s, prog, gain=0.16, bright=1200)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        root = midi(bass_note(ch))
        s.add(b0, i_bass(root, s.secs(1.8)), 0, 0.42, 0.1)
        s.add(b0 + 2, i_bass(root * 1.5, s.secs(0.9)), 0, 0.32, 0.1)
        s.add(b0 + 3.5, i_bass(root, s.secs(0.45)), 0, 0.28, 0.1)
        s.add(b0, kit["kick_soft"], 0, 0.35, 0.05)
        for beat in (1, 3):
            s.add(b0 + beat, drop(s.rng, 550, 900), s.rng.uniform(-0.4, 0.4), 0.22, 0.4)
        for e in range(8):
            s.add(b0 + e * 0.5, kit["shaker"], 0.3, 0.05 if e % 2 else 0.08, 0.1)
    play_melody(s, mel, lambda f, d: i_bell(f, d, 0.45, 3.0, 0.9), gain=0.34, pan=0.1, rev=0.45)
    play_melody(s, mel, lambda f, d: i_bell(f, d, 0.3, 3.0, 0.6), offset=0.75, gain=0.11, pan=-0.5, rev=0.6)
    bubble_bed(s, 120, 0.07)
    return s.render(rt=1.8, wet=0.55, bright=4000)


def music_floor1_combat():
    prog, mel = floor1_material()
    s = Song(100, 32 * 4, seed=2)
    kit = make_kit(s.rng)
    play_pad(s, prog, gain=0.11, bright=1500)
    arp_pat = [0, 1, 2, 3, 2, 1, 0, 1, 2, 3, 4, 3, 2, 1, 2, 3]
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        root = midi(bass_note(ch))
        for e, mult in enumerate((1, 1, 2, 1, 1.5, 1, 2, 1.5)):
            s.add(b0 + e * 0.5, i_saw_bass(root * mult, s.secs(0.42), 800), 0, 0.3, 0.05)
        for beat in (0, 2, 2.5):
            s.add(b0 + beat, kit["kick"], 0, 0.55, 0.05)
        for beat in (1, 3):
            s.add(b0 + beat, kit["snare"], 0, 0.38, 0.25)
        for e in range(16):
            s.add(b0 + e * 0.25, kit["hat"], 0.25, 0.14 if e % 4 == 0 else 0.07, 0.05)
        if bar % 8 == 0:
            s.add(b0, kit["crash"], -0.2, 0.18, 0.2)
        if bar % 4 == 3:
            s.add(b0 + 3.5, drop(s.rng, 700, 1100), 0.4, 0.25, 0.3)
        v = voicing(ch, 69)
        tones = v + [m + 12 for m in v]
        for e, k in enumerate(arp_pat):
            s.add(b0 + e * 0.25, lp(square(midi(tones[k % len(tones)]), s.secs(0.22), 0.5), 3500) * expenv(s.secs(0.22), 0.06),
                  -0.4 if e % 2 else 0.4, 0.08, 0.2)
    play_melody(s, mel, lambda f, d: i_lead(f, d, 0.25, 4500), gain=0.24, pan=0.05, rev=0.25)
    play_melody(s, mel, lambda f, d: i_bell(f, d, 0.3, 3.0, 0.9), gain=0.12, pan=-0.2, rev=0.35)
    bubble_bed(s, 60, 0.05)
    return s.render(rt=1.4, wet=0.4, bright=5000)


def music_main_menu():
    rng = np.random.default_rng(303)
    s = Song(72, 24 * 4, seed=3)
    progA = ["Dm", "C", "Bb", "C", "Dm", "F", "Gm", "A"]
    progB = ["Bb", "F", "Gm", "Dm", "Bb", "C", "Dm", "A"]
    prog = progA + progB + progA
    melA = section_melody(rng, progA, 8, 4, 64, 81, D_MINOR, sparse=True)
    melB = section_melody(rng, progB, 8, 4, 62, 79, D_MINOR, sparse=True)
    play_pad(s, prog, gain=0.2, bright=1000, attack=0.8, release=1.2)
    pattern = [0, 1, 2, 3, 4, 3, 2, 1]
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        s.add(b0, i_sub(midi(bass_note(ch)), s.secs(3.8)), 0, 0.35, 0.1)
        v = voicing(ch, 74)
        tones = v + [m + 12 for m in v]
        for e, k in enumerate(pattern):
            pan = np.sin((bar * 8 + e) / 9) * 0.6
            s.add(b0 + e * 0.5, i_bell(midi(tones[k]), s.secs(0.5), 0.7, 4.0, 0.6), pan, 0.08, 0.55)
        if 8 <= bar < 16:
            for e in range(4):
                s.add(b0 + e + 0.5, bp(noise(0.06, s.rng), 4000, 9000) * swell(0.06, 0.3), 0.4, 0.03, 0.3)
    play_melody(s, melA, i_flute, 0, 0.25, 0.1, 0.55)
    play_melody(s, melB, i_flute, 32, 0.25, -0.1, 0.55)
    play_melody(s, melA, i_flute, 64, 0.25, 0.1, 0.55, transpose=12)
    for c in range(10):  # columnas de burbujas que suben
        t0 = s.rng.uniform(0, 96)
        pan = s.rng.uniform(-0.7, 0.7)
        for j in range(6):
            s.add(t0 + j * 0.18, bubble(400 * 1.18 ** j, 0.09, 1.6), pan, 0.06, 0.6)
    bubble_bed(s, 60, 0.04, 800, 2500)
    return s.render(rt=2.6, wet=0.65, bright=3500, target_db=-18)


def music_shop():
    rng = np.random.default_rng(404)
    s = Song(144, 32 * 3, seed=4, bpb=3)
    kit = make_kit(s.rng)
    progA = ["F", "C", "Dm", "Bb", "F", "Gm", "C", "C"]
    progB = ["Bb", "F", "Gm", "Dm", "Bb", "F", "C", "C"]
    prog = progA + progA + progB + progA
    melA = section_melody(rng, progA, 8, 3, 72, 89, F_MAJOR)
    melB = section_melody(rng, progB, 8, 3, 70, 86, F_MAJOR)
    for bar, ch in enumerate(prog):
        b0 = bar * 3
        s.add(b0, i_bass(midi(bass_note(ch, 41)), s.secs(0.9)), 0, 0.42, 0.1)
        s.add(b0, kit["kick_soft"], 0, 0.25, 0.05)
        for beat in (1, 2):
            for k, m in enumerate(voicing(ch, 60)):
                s.add(b0 + beat, i_pluck(midi(m), s.secs(0.4), 0.12), (k - 1) * 0.4, 0.09, 0.25)
            s.add(b0 + beat, kit["shaker"], 0.3, 0.05, 0.1)
        if bar % 4 == 3:
            s.add(b0 + 2.5, drop(s.rng, 800, 1300), -0.4, 0.15, 0.4)
    celesta = lambda f, d: i_bell(f, d, 0.4, 4.0, 0.8)
    for off, mel in ((0, melA), (24, melA), (48, melB), (72, melA)):
        play_melody(s, mel, celesta, off, 0.33, 0.1, 0.35)
    play_melody(s, melB, lambda f, d: i_pluck(f, d, 0.15), 48, 0.12, -0.3, 0.3, transpose=-12)
    bubble_bed(s, 40, 0.05, 900, 2400)
    return s.render(rt=1.2, wet=0.4, bright=6000)


def music_boss_virus():
    rng = np.random.default_rng(505)
    s = Song(140, 40 * 4, seed=5)
    kit = make_kit(s.rng)
    progA = ["Dm", "Dm", "Bb", "A", "Dm", "Dm", "Gm", "A"]
    progB = ["Gm", "A", "Dm", "Bb", "Gm", "A", "Bb", "A"]
    progC = ["Dm", "Dm", "Dm", "Dm", "Bb", "Bb", "A", "A"]
    sections = [("A", progA), ("B", progB), ("C", progC), ("A", progA), ("B", progB)]
    melA = section_melody(rng, progA, 8, 4, 69, 86, D_MINOR)
    melB = section_melody(rng, progB, 8, 4, 69, 88, D_MINOR)
    lead = lambda f, d: dist(i_lead(f, d, 0.125, 3500, 0.008), 2.0)
    crystal_pat = [0, 2, 1, 3, 2, 4, 3, 5, 4, 2, 3, 1, 2, 0, 1, 3]
    for si, (name, prog) in enumerate(sections):
        base = si * 32
        for bar, ch in enumerate(prog):
            b0 = base + bar * 4
            root = midi(bass_note(ch))
            for k, m in enumerate(voicing(ch, 55)):
                s.add(b0, i_pad(midi(m), s.secs(4), 900), (k - 1) * 0.4, 0.09, 0.4)
            if name == "C":
                for e in range(8):
                    s.add(b0 + e * 0.5, i_saw_bass(root * (2 if e % 4 == 3 else 1), s.secs(0.4), 600), 0, 0.22, 0.05)
                s.add(b0, kit["kick"], 0, 0.5, 0.1)
                if bar % 2 == 1:
                    s.add(b0 + 2, kit["snare_big"], 0, 0.3, 0.5)
                for e in range(4):
                    s.add(b0 + e + 0.5, kit["hat_open"], 0.3, 0.06, 0.2)
                for k, m in enumerate(voicing(ch, 74) * 2):
                    s.add(b0 + k * 0.5, i_crystal(midi(m + (12 if k >= 3 else 0)), 0.3), (-0.6, 0.6)[k % 2], 0.08, 0.5)
                s.add(b0, i_choir(midi(voicing(ch, 69)[-1]), s.secs(4)), 0, 0.09, 0.5)
            else:
                for e in range(16):
                    mult = 2 if e % 4 == 3 else 1
                    s.add(b0 + e * 0.25, i_saw_bass(root * mult, s.secs(0.22), 1100), 0, 0.26, 0.03)
                for beat in range(4):
                    s.add(b0 + beat, kit["kick"], 0, 0.6, 0.05)
                    s.add(b0 + beat + 0.5, kit["hat_open"], 0.2, 0.08, 0.1)
                for beat in (1, 3):
                    s.add(b0 + beat, kit["snare"], 0, 0.42, 0.25)
                for e in range(16):
                    s.add(b0 + e * 0.25, kit["hat"], -0.25, 0.08, 0.05)
                v = voicing(ch, 74)
                tones = v + [m + 12 for m in v]
                for e, k in enumerate(crystal_pat):
                    s.add(b0 + e * 0.25, i_crystal(midi(tones[k]), 0.2), (-0.5, 0.5)[e % 2], 0.07, 0.3)
                if bar % 2 == 0:
                    stab = sum(lp(saw(midi(m), s.secs(0.4)), 2500) for m in voicing(ch, 62)) * expenv(s.secs(0.4), 0.08)
                    s.add(b0, stab, 0, 0.12, 0.3)
            if bar == 7:
                for e, t in enumerate(("tom_hi", "tom_hi", "tom_lo", "tom_lo")):
                    s.add(b0 + 3 + e * 0.25, kit[t], (0.4, -0.4)[e % 2], 0.35, 0.3)
            if bar == 0:
                s.add(b0, kit["crash"], 0, 0.2, 0.3)
        if name == "A":
            play_melody(s, melA, lead, base, 0.2, 0.05, 0.25)
        elif name == "B":
            play_melody(s, melB, lead, base, 0.2, 0.05, 0.25)
    for _ in range(14):  # chispazos eléctricos sueltos
        z = hp(noise(0.03, s.rng), 2500) * expenv(0.03, 0.008)
        s.add(s.rng.integers(0, 160) + 0.75, z, s.rng.uniform(-0.8, 0.8), 0.15, 0.4)
    return s.render(rt=1.3, wet=0.35, bright=6000)


def music_boss_amoeba():
    rng = np.random.default_rng(606)
    s = Song(90, 24 * 4, seed=6)
    kit = make_kit(s.rng)
    progA = ["Dm", "Dm", "Eb", "Dm", "Dm", "Dm", "C", "Eb"]
    progB = ["Gm", "Gm", "Eb", "Eb", "Bb", "A", "Dm", "A"]
    prog = progA + progB + progA
    melA = section_melody(rng, progA, 8, 4, 57, 74, D_PHRYGIAN, sparse=True)
    melB = section_melody(rng, progB, 8, 4, 60, 77, D_PHRYGIAN, sparse=True)
    swing = 0.09
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        root = midi(bass_note(ch, 33))
        s.add(b0, i_wobble(root, s.secs(1.45), 1 / s.beat), 0, 0.33, 0.05)
        s.add(b0 + 2.5 + swing, i_wobble(root, s.secs(0.9), 2 / s.beat), 0, 0.3, 0.05)
        s.add(b0 + 3.5 + swing, i_wobble(root * 2, s.secs(0.45), 2 / s.beat), 0, 0.22, 0.05)
        s.add(b0, kit["kick_big"], 0, 0.55, 0.1)
        s.add(b0 + 1.75, kit["kick"], 0, 0.45, 0.1)
        s.add(b0 + 2, kit["snare_big"], 0, 0.42, 0.5)
        for e in range(8):
            s.add(b0 + e * 0.5 + (swing if e % 2 else 0), kit["hat"], 0.3, 0.09 if e % 2 else 0.06, 0.1)
        if bar % 2 == 1:
            s.add(b0 + 3.5, kit["tom_lo"], -0.3, 0.35, 0.3)
            s.add(b0 + 3.75, kit["tom_lo"], 0.3, 0.3, 0.3)
        if bar % 4 == 0:
            s.add(b0, kit["boom"], 0, 0.4, 0.4)
        for k, m in enumerate(voicing(ch, 50)):
            s.add(b0, i_pad(midi(m), s.secs(4), 650), (k - 1) * 0.4, 0.12, 0.4)
        for _ in range(2):
            g = lp(bubble(s.rng.uniform(90, 220), 0.16, 1.4), 900)
            s.add(b0 + s.rng.uniform(0, 4), g, s.rng.uniform(-0.6, 0.6), 0.2, 0.4)
    eerie = lambda f, d: i_lead(f, d, 0.5, 1800, 0.012)
    play_melody(s, melA, eerie, 0, 0.22, -0.1, 0.5)
    play_melody(s, melB, eerie, 32, 0.22, -0.1, 0.5)
    play_melody(s, melB, lambda f, d: i_bell(f, d, 0.5, 3.0, 1.2), 32, 0.09, 0.4, 0.6, transpose=12)
    play_melody(s, melA, eerie, 64, 0.22, -0.1, 0.5)
    return s.render(rt=1.8, wet=0.45, bright=3500)


def music_boss_amoeba_final():
    """30,0 s exactos: 18 compases a 144 BPM, en 3 tramos de 6 compases que suben."""
    rng = np.random.default_rng(707)
    s = Song(144, 72, loop=False, tail=0.0, seed=7)
    kit = make_kit(s.rng)
    p1 = ["Dm", "Dm", "Bb", "Bb", "Gm", "A"]
    p2 = ["Dm", "Bb", "Gm", "A", "Dm", "A"]
    p3 = ["Dm", "Eb", "Dm", "Eb", "Bb", "A"]
    prog = p1 + p2 + p3
    mel2 = section_melody(rng, p2, 6, 4, 69, 86, D_MINOR)
    mel3 = section_melody(rng, p3, 6, 4, 74, 91, D_PHRYGIAN)
    for bar, ch in enumerate(prog[:17]):
        b0 = bar * 4
        seg = bar // 6
        root = midi(bass_note(ch))
        steps = 8 if seg == 0 else 16
        for e in range(steps):
            mult = 2 if e % 4 == 3 else 1
            s.add(b0 + e * 4 / steps, i_saw_bass(root * mult, s.secs(4 / steps * 0.85), 900 + seg * 300), 0, 0.28, 0.03)
        for beat in range(4):
            s.add(b0 + beat, kit["kick"], 0, 0.6, 0.05)
        if seg >= 1:
            for beat in (1, 3):
                s.add(b0 + beat, kit["snare"], 0, 0.42, 0.25)
        hats = 8 if seg == 0 else 16
        for e in range(hats):
            s.add(b0 + e * 4 / hats, kit["hat"], 0.25, 0.08, 0.05)
        if seg == 2:
            s.add(b0, kit["crash"], -0.3, 0.16, 0.3)
            if bar % 2 == 1:
                for e in range(8):
                    s.add(b0 + 2 + e * 0.25, kit["snare"], 0, 0.12 + e * 0.03, 0.2)
        v = voicing(ch, 69)
        tones = v + [m + 12 for m in v] + [m + 24 for m in v]
        for e in range(16):  # anillos: arpegio que sube
            s.add(b0 + e * 0.25, i_crystal(midi(tones[e % 6]), 0.2), -0.5, 0.06, 0.3)
            if seg >= 1:  # brazos en el otro sentido
                s.add(b0 + e * 0.25 + 0.125, i_crystal(midi(tones[5 - e % 6]), 0.2), 0.5, 0.05, 0.3)
        for k, m in enumerate(voicing(ch, 57)):
            s.add(b0, i_pad(midi(m), s.secs(4), 1000 + seg * 400), (k - 1) * 0.4, 0.09, 0.4)
        if seg == 2:
            s.add(b0, i_choir(midi(voicing(ch, 69)[-1]), s.secs(4)), 0, 0.1, 0.4)
    lead = lambda f, d: dist(i_lead(f, d, 0.25, 4000, 0.006), 1.6)
    play_melody(s, mel2, lead, 24, 0.2, 0, 0.25)
    play_melody(s, mel3, lead, 48, 0.2, 0.1, 0.25)
    play_melody(s, mel3, lead, 48, 0.12, -0.2, 0.25, transpose=-5)  # armonía a la cuarta
    rise = noise_sweep(s.secs(8), 300, 6000, s.rng, 1.5) * np.linspace(0, 1, n(s.secs(8))) ** 2
    s.add(62, rise, 0, 0.25, 0.3)
    # golpe final y corte seco
    stab = sum(dist(lp(saw(midi(m), s.secs(2)), 3000), 1.5) for m in (38, 50, 53, 57, 62)) * expenv(s.secs(2), 0.5)
    s.add(68, stab, 0, 0.2, 0.4)
    s.add(68, kit["kick_big"], 0, 0.8, 0.2)
    s.add(68, kit["crash"], 0, 0.3, 0.4)
    s.add(68, kit["boom"], 0, 0.5, 0.3)
    out = s.render(rt=1.3, wet=0.35, bright=6000)
    return np.stack([fade(c, 0, 0.03) for c in out])


# ----------------------------------------------------------------- fragmentos

def sting_lineage_evolution():
    s = Song(120, 4, loop=False, seed=8)  # 2,0 s
    for i, m in enumerate((62, 66, 69, 74, 78, 81, 86, 90)):
        s.add(i * 0.25, i_harp(midi(m), 1.0), -0.6 + i * 0.17, 0.3, 0.5)
    for k, m in enumerate((86, 90, 93)):
        s.add(2 + k * 0.08, i_bell(midi(m), 1.2, 0.5, 3.5, 1.2), (k - 1) * 0.5, 0.2, 0.6)
    for k, m in enumerate((50, 57, 62, 66, 69)):
        s.add(0, i_pad(midi(m), 1.6, 1800, attack=1.0, release=0.4), (k - 2) * 0.3, 0.14, 0.5)
    s.add(1.8, hp(noise(1.0, s.rng), 6000) * swell(1.0, 0.3) * 0.5, 0, 0.3, 0.6)
    out = s.render(rt=1.6, wet=0.6, bright=7000, target_db=-17)
    return np.stack([fade(c, 0.003, 0.3) for c in out])


def sting_boss_intro():
    s = Song(120, 5, loop=False, seed=9)  # 2,5 s
    kit = make_kit(s.rng)
    d = 2.45
    s.add(0, kit["boom"], 0, 0.9, 0.5)
    s.add(0, noise_sweep(d, 150, 4000, s.rng, 1.2) * np.linspace(0, 1, n(d)) ** 2, 0, 0.5, 0.3)
    f = glide(45, 180, d)
    cluster = sum(saw(f * r, d) for r in (1, 1.01, 1.5)) / 3
    s.add(0, dist(lp(cluster, 1500), 2) * np.linspace(0.1, 1, n(d)) ** 2, 0, 0.5, 0.3)
    for i in range(8):  # redoble que acelera
        s.add(2.4 + i * 0.25 * (0.85 ** i), kit["tom_lo"], (-0.3, 0.3)[i % 2], 0.2 + i * 0.04, 0.3)
    out = s.render(rt=1.2, wet=0.35, bright=5000, target_db=-16)
    return np.stack([fade(c, 0, 0.01) for c in out])


def sting_victory():
    s = Song(120, 11, loop=False, tail=1.0, seed=10)  # 6,5 s
    kit = make_kit(s.rng)
    lead = [(0, .5, 74), (.5, .5, 77), (1, .5, 81), (1.5, 1.5, 86), (3, .5, 84), (3.5, .5, 82),
            (4, 1, 81), (5, .5, 82), (5.5, .5, 84), (6, 4, 86)]
    for b, dd, m in lead:
        s.add(b, i_lead(midi(m), s.secs(dd) * 0.95, 0.25, 5000), 0.05, 0.3, 0.35)
        s.add(b, i_bell(midi(m), s.secs(dd), 0.4), -0.3, 0.12, 0.4)
    chords = [(0, 1.5, "Dm"), (1.5, 1.5, "Bb"), (3, 1, "C"), (4, 1, "F"), (5, 1, "Gm"), (6, 4, "D")]
    for b, dd, ch in chords:
        for k, m in enumerate(voicing(ch, 57)):
            s.add(b, i_pad(midi(m), s.secs(dd), 1800, attack=0.05), (k - 1) * 0.4, 0.13, 0.4)
        s.add(b, i_bass(midi(bass_note(ch)), s.secs(dd) * 0.9), 0, 0.4, 0.1)
    for i in range(8):
        s.add(5 + i * 0.125, kit["snare"], 0, 0.12 + i * 0.03, 0.2)
    s.add(6, kit["crash"], 0, 0.3, 0.4)
    s.add(6, kit["kick_big"], 0, 0.6, 0.2)
    for i in range(10):
        s.add(6 + i * 0.3, bubble(700 * 1.12 ** i, 0.1, 1.6), s.rng.uniform(-0.6, 0.6), 0.08, 0.6)
    out = s.render(rt=1.8, wet=0.5, bright=6000, target_db=-17)
    return np.stack([fade(c, 0.003, 0.8) for c in out])


def sting_defeat():
    s = Song(80, 6, loop=False, tail=0.5, seed=11)  # 5,0 s
    for b, dd, m in ((0, 1, 69), (1, 1, 67), (2, 1, 65), (3, 1, 64), (4, 2, 62)):
        s.add(b, i_flute(midi(m), s.secs(dd) * 0.95), 0.1, 0.32, 0.6)
    for b, dd, ch in ((0, 2, "Dm"), (2, 2, "Bb"), (4, 2.6, "Dm")):
        for k, m in enumerate(voicing(ch, 55)):
            s.add(b, i_pad(midi(m), s.secs(dd), 900, attack=0.3, release=0.8), (k - 1) * 0.4, 0.14, 0.5)
        s.add(b, i_sub(midi(bass_note(ch)), s.secs(dd)), 0, 0.3, 0.2)
    rise = sine(glide(260, 520, 1.6)) * swell(1.6, 0.5) * expenv(1.6, 0.9)
    s.add(3.6, rise, -0.3, 0.12, 0.6)
    s.add(5.3, bubble(520, 0.15, 1.4, 0.05), -0.3, 0.08, 0.7)
    out = s.render(rt=2.2, wet=0.6, bright=3000, target_db=-18)
    return np.stack([fade(c, 0.003, 1.0) for c in out])


MUSIC = {
    "Music_MainMenu": (music_main_menu, True, 0.7),
    "Music_Floor1_Explore": (music_floor1_explore, True, 0.6),
    "Music_Floor1_Combat": (music_floor1_combat, True, 0.6),
    "Music_Shop": (music_shop, True, 0.6),
    "Music_Boss_Virus": (music_boss_virus, True, 0.65),
    "Music_Boss_Amoeba": (music_boss_amoeba, True, 0.65),
    "Music_Boss_Amoeba_Final": (music_boss_amoeba_final, False, 0.7),
    "Music_Sting_LineageEvolution": (sting_lineage_evolution, False, 0.7),
    "Music_Sting_BossIntro": (sting_boss_intro, False, 0.7),
    "Music_Sting_Victory": (sting_victory, False, 0.7),
    "Music_Sting_Defeat": (sting_defeat, False, 0.7),
}
