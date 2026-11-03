"""Efectos de sonido y ambientes del piso 3 (pantano carbonífero), según PROMPT_piso3.md. Cada generador recibe el rng
y el índice de variante y devuelve la señal mono sin normalizar, como en efectos.py.

Paleta: aire caliente y húmedo, vida por todas partes. Anfibios que croan, insectos que pasan, lodo que burbujea y
gotea; más adelante fuego, metano y veneno. Lo que lleva nota, en re (el fondo respira sobre re y la)."""

import numpy as np

from efectos import loop_noise, loop_sine, sfx
from sintesis import bp, bubble, circular_conv, expenv, hp, impulse_response, lp, n, noise, place, sine, swell
from sintesis import padded

# Como en efectos_piso2: las señales se suman aunque midan distinto (Sig rellena con ceros).
for _name in ("bp", "bubble", "expenv", "hp", "lp", "noise", "sine", "swell"):
    globals()[_name] = padded(globals()[_name])


# ================================================================ Ambientes

def croak(rng, f, d, pulses):
    """Croar de anfibio: tren de pulsos graves (la garganta que vibra) con un formante nasal."""
    N = n(d)
    t = np.arange(N) / 44100
    gate = (np.sin(2 * np.pi * pulses * t) > 0.2).astype(float)
    tone = np.sin(2 * np.pi * f * t) + 0.5 * np.sin(2 * np.pi * f * 2.02 * t) + 0.25 * np.sin(2 * np.pi * f * 3.1 * t)
    body = bp(tone * gate, f * 1.8, f * 6) * swell(d, 0.35, 1.5)
    return lp(body, 1600)


def insect_pass(rng, d):
    """Zumbido que pasa de un lado a otro: alas a 40-60 Hz sobre un tono que sube y baja al acercarse."""
    N = n(d)
    t = np.arange(N) / 44100
    f0 = rng.uniform(170, 260)
    bend = 1 + 0.06 * np.sin(np.pi * t / d - np.pi / 2)        # sube al acercarse y baja al alejarse
    phase = 2 * np.pi * np.cumsum(f0 * bend) / 44100
    wings = 0.6 + 0.4 * np.sin(2 * np.pi * rng.uniform(40, 60) * t)
    tone = (np.sin(phase) + 0.4 * np.sin(2 * phase) + 0.2 * np.sin(3 * phase)) * wings
    return bp(tone, 250, 2400) * swell(d, 0.5, 1.6)


@sfx("Amb_Floor3_Loop", 1, 0.3, loop=True)
def amb_floor3(rng, v):
    """Pantano carbonífero: aire caliente y húmedo, croar lejano, insectos que pasan, lodo que burbujea y gotas que
    caen del musgo, sobre un fondo grave que respira en re y la. Sube junto a la charca de la sala de paso del piso 2 y
    es el ambiente del pantano verde."""
    d = 24.0
    N = n(d)
    t = np.arange(N) / N
    # Aire pesado: rumor grave que respira tres veces por bucle.
    air = loop_noise(d, rng, 220, 2)
    air /= np.max(np.abs(air))
    breath = 0.7 + 0.3 * np.sin(2 * np.pi * 3 * t)
    # Fondo en re y la, con un batido lento que lo hace húmedo y cerrado.
    drone = loop_sine(73.4, d) + loop_sine(73.9, d) * 0.6 + loop_sine(110.0, d) * 0.4 + loop_sine(36.7, d) * 0.3
    drone *= 0.75 + 0.25 * np.sin(2 * np.pi * 2 * t + 1.0)
    # Frondas mecidas por el aire caliente.
    leaves = loop_noise(d, rng, 4000, 2) - loop_noise(d, rng, 1200, 2) * 0.8
    leaves = leaves / np.max(np.abs(leaves)) * (0.5 + 0.5 * np.sin(2 * np.pi * 3 * t - 1.4)) ** 3
    buf = air * breath * 0.45 + drone * 0.09 + leaves * 0.05

    # Croar lejano: grupos de 2 o 3 croares, graves (re y la), repartidos por el bucle.
    for t0 in (1.5, 6.8, 11.2, 15.9, 20.4):
        f = rng.choice([110.0, 146.8, 98.0]) * rng.uniform(0.98, 1.02)
        for k in range(rng.integers(2, 4)):
            c = croak(rng, f, rng.uniform(0.28, 0.45), rng.uniform(18, 26))
            place(buf, c, t0 + rng.uniform(-0.6, 0.6) + k * rng.uniform(0.5, 0.7), rng.uniform(0.07, 0.12), True)
    # Insectos que pasan (lejos, suaves: no se confunden con los enemigos).
    for _ in range(6):
        w = rng.uniform(1.6, 3.0)
        place(buf, insect_pass(rng, w), rng.uniform(0, d), rng.uniform(0.025, 0.045), True)
    # Burbujas gordas de lodo y metano que revientan con un «blop» grave.
    for _ in range(9):
        f = np.exp(rng.uniform(np.log(60), np.log(140)))
        blop = lp(bubble(f, rng.uniform(0.22, 0.38), 1.15), 700)
        pop = lp(noise(0.03, rng), 900) * expenv(0.03, 0.008)
        at = rng.uniform(0, d)
        place(buf, blop, at, rng.uniform(0.1, 0.16), True)
        place(buf, pop, at + 0.18, rng.uniform(0.03, 0.06), True)
    # Gotas que caen del musgo a los charcos.
    for _ in range(26):
        f = np.exp(rng.uniform(np.log(900), np.log(1900)))
        drip = bubble(f, 0.05, 2.2) + hp(noise(0.004, rng), 3000) * expenv(0.004, 0.001) * 0.3
        place(buf, drip, rng.uniform(0, d), rng.uniform(0.02, 0.05), True)
    # Un poco de espacio: el pantano es abierto pero la espesura lo cierra.
    return buf * 0.8 + circular_conv(buf, impulse_response(2.2, rng, bright=2200)) * 0.38
