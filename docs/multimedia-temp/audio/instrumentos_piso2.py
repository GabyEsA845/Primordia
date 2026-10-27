"""Instrumentos del piso 2 (océano cámbrico): marimba de conchas, canto de las profundidades, pads que ondulan,
armónica de cristal, percusión de mar y eco largo. Los usan muestrario_piso2.py y, cuando se aprueben, la música
del piso 2. Todo en re (tonalidad común del juego); cada franja cambia el modo."""

import numpy as np

import musica
from musica import (adsr, bp, bubble, click, dist, expenv, glide, hp, lp, noise, saw, sine, swell, thump, tri)
from sintesis import SR, midi, n, tt

# Acordes que faltaban en musica.CHORDS (nota grave en midi y tipo)
musica.CHORDS.update({
    "G": (55, (0, 4, 7)), "Em": (52, (0, 3, 7)), "Am": (57, (0, 3, 7)), "Cm": (48, (0, 3, 7)),
})
D_DORIAN = {2, 4, 5, 7, 9, 11, 0}     # Someras: re menor con si natural, luminoso
D_AEOLIAN = {2, 4, 5, 7, 9, 10, 0}    # Arrecife: re menor natural
D_PHRYGIAN = {2, 3, 5, 7, 9, 10, 0}   # Abismo: con mi bemol, oscuro


# ================================================================ instrumentos

def i_marimba(f, d=None, tau=None, shell=1.0):
    """Marimba de conchas: golpe de madera hueca (fundamental + parcial ×3,93) con un chasquido de concha
    agudo y muy corto al principio. Los graves duran más que los agudos."""
    tau = tau or float(np.clip(0.5 * (196 / f) ** 0.6, 0.08, 0.9))
    dd = tau * 5
    t = tt(dd)
    body = sine(f, dd) * np.exp(-t / tau) + sine(f * 3.93, dd) * np.exp(-t / (tau * 0.22)) * 0.3
    shell_tick = sine(f * 9.3, dd) * np.exp(-t / 0.01) * 0.14 * shell
    rng = np.random.default_rng(int(f * 10))
    mallet = bp(noise(0.012, rng), 1500, 6000) * expenv(0.012, 0.003, 0.0005) * 0.25 * shell
    x = body + shell_tick + mallet
    return lp(x * np.clip(t / 0.0015, 0, 1)[: len(x)], 7000)


def i_tide(f, d, rate=0.18, phase0=0.0, dark=500, bright=2000, attack=1.0, release=1.5):
    """Pad que ondula: el filtro se abre y se cierra despacio (una ola cada ~5,5 s) y el volumen respira con él."""
    dd = d + release
    s = sum(saw(f * r, dd) for r in (1.0, 1.005, 0.995)) / 3 + tri(f / 2, dd) * 0.5
    t = tt(dd)
    m = 0.5 - 0.5 * np.cos(2 * np.pi * rate * t + phase0)
    x = lp(s, dark) * (1 - m) + lp(s, bright) * m
    return x * (0.8 + 0.2 * m) * adsr(dd, attack, 0.5, 0.85, release)


def i_glass(f, d):
    """Armónica de cristal: seno casi puro que entra despacio, con batido y un temblor suave."""
    dd = d + 0.6
    t = tt(dd)
    s = sine(f, dd) + sine(f * 1.003, dd) * 0.4 + sine(f * 2, dd) * 0.22 + sine(f * 3.01, dd) * 0.06
    s = s * (1 - 0.12 * (0.5 + 0.5 * np.sin(2 * np.pi * 6 * t)))
    return s * adsr(dd, 0.25, 0.3, 0.8, 0.6)


def song_line(notes, beat, glide_s=0.22, total=None, low=1.0):
    """Canto de las profundidades: una sola voz continua que se desliza de nota en nota (portamento), con vibrato
    lento que crece en cada nota y algo de aliento. notes = [(pulso, duración en pulsos, midi)]."""
    end = max(b + d for b, d, _ in notes) * beat + 0.8
    N = n(total or end)
    target = np.full(N, np.log(midi(notes[0][2])))
    amp = np.zeros(N)
    depth = np.zeros(N)
    for b, d, m in notes:
        i0, i1 = int(b * beat * SR), min(N, int((b + d) * beat * SR))
        target[i0:] = np.log(midi(m))
        dur = (i1 - i0) / SR
        env = adsr(dur + 0.4, 0.14, 0.25, 0.8, 0.45)
        j1 = min(N, i0 + len(env))
        amp[i0:j1] = np.maximum(amp[i0:j1], env[: j1 - i0])
        ramp = np.clip(np.arange(j1 - i0) / SR / 0.6, 0, 1)
        depth[i0:j1] = np.maximum(depth[i0:j1], ramp * 0.011)
    # portamento: el logaritmo de la frecuencia sigue al objetivo con un filtro de un polo
    a = np.exp(-1 / (glide_s / 3 * SR))
    from scipy import signal
    logf = signal.lfilter([1 - a], [1, -a], target - target[0]) + target[0]
    t = np.arange(N) / SR
    f = np.exp(logf) * low * (1 + depth * np.sin(2 * np.pi * 4.3 * t))
    ph = 2 * np.pi * np.cumsum(f) / SR
    voice = np.sin(ph) + 0.32 * np.sin(2 * ph) + 0.14 * np.sin(3 * ph) + 0.05 * np.sin(5 * ph)
    rng = np.random.default_rng(7)
    breath = bp(rng.uniform(-1, 1, N), 250, 1100) * 0.05
    return lp((voice + breath) * amp, 2600)


# =================================================================== percusión

def sea_kit(rng):
    """Percusión de mar: quitina, piedra, tambores de madera, conchas, burbujas grandes, tambor grave y latido."""
    k = {}
    k["chitin"] = hp(noise(0.012, rng), 3000) * expenv(0.012, 0.002, 0.0003) + \
        bp(noise(0.03, rng), 3500, 5200) * expenv(0.03, 0.006) * 0.6
    k["chitin2"] = k["chitin"] + np.pad(k["chitin"] * 0.7, (n(0.028), 0))
    k["stone"] = bp(noise(0.08, rng), 1200, 2600) * expenv(0.08, 0.012, 0.0005) + \
        sine(glide(420, 300, 0.06)) * expenv(0.06, 0.015) * 0.6 + thump(220, 120, 0.08, 0.02) * 0.4
    for name, f0 in (("wood_lo", 150), ("wood_hi", 235)):
        k[name] = sine(glide(f0, f0 * 0.79, 0.4, "fast")) * expenv(0.4, 0.12, 0.001) + \
            sine(glide(f0 * 2.71, f0 * 2.14, 0.15)) * expenv(0.15, 0.03) * 0.4 + click(rng, 0.004, 2500) * 0.2
    grains = np.zeros(n(0.12))
    for _ in range(26):
        g = bp(noise(0.004, rng), 3000, 8000) * expenv(0.004, 0.001, 0.0002)
        i = int(rng.uniform(0, 0.11) * SR)
        grains[i:i + len(g)] += g * rng.uniform(0.3, 1)
    k["shells"] = grains * swell(0.12, 0.35)
    k["big_bubble"] = lp(bubble(rng.uniform(160, 260), 0.35, 1.5, 0.12), 1200)
    k["deep_drum"] = dist(thump(110, 45, 0.6, 0.2) + lp(noise(0.15, rng), 400) * expenv(0.15, 0.04) * 0.5, 1.3)
    beat1 = lp(thump(90, 50, 0.25, 0.07), 220)
    k["heartbeat"] = beat1 + np.pad(beat1 * 0.75, (n(0.2), 0))
    k["sonar"] = sine(midi(86), 1.4) * expenv(1.4, 0.35, 0.003) + sine(midi(98), 0.4) * expenv(0.4, 0.05) * 0.12
    return k


# ============================================================ secuenciador

def hp_circular(x, fc):
    """Quita los graves inaudibles como el filtro de musica.hp_stereo, pero en frecuencia y de forma circular,
    así el final de un bucle sigue enlazando con el principio (sosfiltfilt deja un salto en el empalme)."""
    N = x.shape[1]
    f = np.fft.rfftfreq(N, 1 / SR)
    h = (f / fc) ** 2 / (1 + (f / fc) ** 2)  # módulo de un paso alto de orden 1 aplicado ida y vuelta
    return np.fft.irfft(np.fft.rfft(x, axis=1) * h, N, axis=1)


class Song2(musica.Song):
    """Song del piso 2: igual que la del piso 1, pero los bucles se filtran de forma circular."""

    def render(self, *a, **k):
        if not self.loop:
            return super().render(*a, **k)
        orig = musica.hp_stereo
        musica.hp_stereo = hp_circular
        try:
            return super().render(*a, **k)
        finally:
            musica.hp_stereo = orig


# ======================================================================= eco

def add_echo(song, beat, x, pan=0.0, gain=1.0, rev=0.2, every=0.75, fb=0.5, repeats=4, darken=3000):
    """Nota con eco largo de ida y vuelta: cada repetición más baja, más oscura y en el lado contrario."""
    song.add(beat, x, pan, gain, rev)
    for k in range(1, repeats + 1):
        side = -0.6 if k % 2 else 0.6
        song.add(beat + k * every, lp(x, darken / k), side, gain * fb ** k, rev + 0.1)
