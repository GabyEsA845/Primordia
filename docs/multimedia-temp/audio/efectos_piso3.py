"""Efectos de sonido y ambientes del piso 3 (pantano carbonífero), según PROMPT_piso3.md. Cada generador recibe el rng
y el índice de variante y devuelve la señal mono sin normalizar, como en efectos.py.

Paleta: aire caliente y húmedo, vida por todas partes. Anfibios que croan, insectos que pasan, lodo que burbujea y
gotea; más adelante fuego, metano y veneno. Lo que lleva nota, en re (el fondo respira sobre re y la)."""

import numpy as np

from efectos import fizz, loop_noise, loop_sine, puff, pv, sfx
from efectos_piso2 import grains, loop_band
from sintesis import (SR, am, bp, bubble, bubbles, circular_conv, click, dist, expenv, glide, hp, impulse_response, lp,
                      mix, n, noise, noise_sweep, place, reverb, saw, sine, svf, swell, thump, tri)
from sintesis import padded

# Como en efectos_piso2: las señales se suman aunque midan distinto (Sig rellena con ceros).
for _name in ("am", "bp", "bubble", "bubbles", "click", "dist", "expenv", "glide", "hp", "lp", "mix", "noise",
              "noise_sweep", "reverb", "saw", "sine", "svf", "swell", "thump", "tri"):
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


# ================================================================ Ayudas de fuego, gas y lodo

def crackle(rng, d, count, lo=1800, hi=7000, span=None, accel=0.0, gain=(0.3, 1.0)):
    """Crepitar: chasquidos de brasa (grano fino) con algún «pop» más grave entre medias."""
    buf = grains(d, rng, count, lo, hi, span, gain, 0.004, accel)
    for _ in range(max(1, count // 6)):
        pop = bp(noise(0.012, rng), 500, 1800) * expenv(0.012, 0.003, 0.0003)
        place(buf, pop, rng.uniform(0, (span or d) * 0.95), rng.uniform(0.3, 0.7))
    return buf


def roar(rng, d, fc=500, peak=0.3, power=1.5):
    """Rugido de llama: ruido grave con un temblor rápido, que crece y se va."""
    x = lp(noise(d, rng), fc) * swell(d, peak, power)
    return am(x, rng.uniform(9, 14), 0.35)


def whoosh(rng, d, f0, f1, q=1.5):
    """Soplo: ruido por una banda que se desplaza (prender, rasgar, succionar)."""
    return noise_sweep(d, f0, f1, rng, q) * swell(d, 0.25, 1.2)


def splat(rng, d, lo, hi, tau, bub_lo, bub_hi, count=3):
    """Chapoteo: ruido en banda con caída rápida y unas burbujas."""
    body = bp(noise(d, rng), lo, hi) * expenv(d, tau, 0.0008)
    return body + bubbles(d, rng, count, bub_lo, bub_hi, (0.03, 0.07), 1.6, (0.25, 0.6))


# ================================================================ Reacciones: fuego

@sfx("Fire_Ignite", 3, 0.5)
def fire_ignite(rng, v):
    p = pv(rng, v)
    d = 0.35
    fuf = whoosh(rng, 0.25, 300 * p, 2400 * p, 1.2)
    body = roar(rng, 0.3, 900 * p, 0.2, 1.3)
    sparks = crackle(rng, 0.3, 10, span=0.28, gain=(0.2, 0.6))
    return mix(d, (0, click(rng, 0.004, 2500), 0.4), (0, fuf), (0.03, body, 0.8), (0.05, sparks, 0.5))


@sfx("Fire_Burn_Loop", 1, 0.3, loop=True)
def fire_burn_loop(rng, v):
    d = 4.0
    N = n(d)
    t = np.arange(N) / N
    base = loop_noise(d, rng, 380, 2)
    base /= np.max(np.abs(base))
    flick = loop_band(d, rng, 6, 14)                       # el temblor de la llama, cíclico
    base *= 0.75 + 0.25 * flick
    base *= 0.85 + 0.15 * np.sin(2 * np.pi * 2 * t)
    buf = base * 0.35
    for _ in range(70):
        g = bp(noise(0.004, rng), 1800, 7000) * expenv(0.004, 0.0012, 0.0003)
        place(buf, g, rng.uniform(0, d), rng.uniform(0.15, 0.6), True)
    for _ in range(12):
        pop = bp(noise(0.015, rng), 400, 1500) * expenv(0.015, 0.004, 0.0003)
        place(buf, pop, rng.uniform(0, d), rng.uniform(0.3, 0.7), True)
    return buf


@sfx("Fire_Extinguish", 2, 0.5)
def fire_extinguish(rng, v):
    p = pv(rng, v)
    d = 0.6
    hiss = svf(noise(d, rng), glide(7000 * p, 2500 * p, d), 0.9, "hp") * expenv(d, 0.2, 0.004)
    drop = bubble(1100 * p, 0.06, 1.8)
    sizzle = crackle(rng, 0.25, 8, 3000, 8000, gain=(0.15, 0.4))
    return mix(d, (0, drop, 0.5), (0.01, hiss), (0, sizzle, 0.5))


@sfx("Status_Burn_Start", 2, 0.5)
def status_burn_start(rng, v):
    p = pv(rng, v)
    d = 0.4
    wrap = whoosh(rng, 0.3, 500 * p, 1800 * p, 1.4)
    body = roar(rng, 0.38, 700 * p, 0.35, 1.4)
    sparks = crackle(rng, 0.35, 12, span=0.33, gain=(0.2, 0.7))
    return mix(d, (0, click(rng, 0.004, 2000), 0.4), (0, wrap, 0.9), (0.02, body), (0.04, sparks, 0.6))


@sfx("Status_Burn_Tick", 3, 0.3)
def status_burn_tick(rng, v):
    p = pv(rng, v, 0.08)
    d = 0.08
    snap = bp(noise(0.01, rng), 1500 * p, 6000 * p) * expenv(0.01, 0.0025, 0.0003)
    ember = lp(noise(0.07, rng), 900 * p) * expenv(0.07, 0.015, 0.0005)
    pops = crackle(rng, 0.06, 4, span=0.05, gain=(0.2, 0.5))
    return mix(d, (0, snap), (0, ember, 0.5), (0.008, pops, 0.5))


# ================================================================ Reacciones: metano

@sfx("Methane_Explosion", 3, 0.75)
def methane_explosion(rng, v):
    p = pv(rng, v, 0.07)
    d = 0.9
    boom = dist(thump(140 * p, 38 * p, 0.6, 0.16), 1.8)
    hollow = sine(glide(220 * p, 90 * p, 0.25)) * expenv(0.25, 0.06, 0.001)   # el hueco del gas que se abre
    blast = lp(noise(0.5, rng), 1600) * expenv(0.5, 0.09, 0.0005)
    fire = roar(rng, 0.7, 1100, 0.15, 1.2)
    sparks = crackle(rng, 0.8, 26, span=0.75, accel=0.6, gain=(0.15, 0.6))
    return mix(d, (0, click(rng, 0.006, 1500), 0.6), (0, boom), (0, hollow, 0.5), (0, blast, 0.8),
               (0.04, fire, 0.7), (0.1, sparks, 0.5))


@sfx("Methane_FogFlash", 1, 0.6)
def methane_fog_flash(rng, v):
    d = 1.2
    fc = np.concatenate([glide(400, 2600, d * 0.6, "lin"), glide(2600, 700, d * 0.4, "lin")])[:n(d)]
    run = svf(noise(d, rng)[:len(fc)], fc, 1.1, "bp") * swell(d, 0.45, 1.4)[:len(fc)]
    rumble = lp(noise(d, rng), 260) * swell(d, 0.4, 1.6)
    sparks = crackle(rng, d, 40, span=d * 0.95, gain=(0.15, 0.5))
    return mix(d, (0, click(rng, 0.005, 1500), 0.4), (0, run), (0, rumble, 0.9), (0.05, sparks, 0.45))


@sfx("Methane_Mist_Loop", 1, 0.18, loop=True)
def methane_mist_loop(rng, v):
    d = 6.0
    N = n(d)
    t = np.arange(N) / N
    hiss = loop_band(d, rng, 1500, 5500) * (0.7 + 0.3 * np.sin(2 * np.pi * 2 * t))
    low = loop_noise(d, rng, 200, 2)
    buf = hiss * 0.15 + low / np.max(np.abs(low)) * 0.35
    for _ in range(14):
        place(buf, bubble(np.exp(rng.uniform(np.log(250), np.log(600))), rng.uniform(0.06, 0.12), 1.5),
              rng.uniform(0, d), rng.uniform(0.08, 0.18), True)
    return buf


@sfx("Methane_GasBag_Burst", 2, 0.55)
def gasbag_burst(rng, v):
    p = pv(rng, v)
    d = 0.3
    rip = grains(0.06, rng, 9, 800 * p, 3500 * p, 0.05)
    pop = thump(320 * p, 120 * p, 0.08, 0.02)
    gas = whoosh(rng, 0.25, 2500 * p, 600 * p, 1.0)
    return mix(d, (0, pop, 0.8), (0, rip, 0.7), (0.02, gas))


@sfx("Methane_GasBag_Regrow", 1, 0.35)
def gasbag_regrow(rng, v):
    d = 0.8
    infl = svf(noise(d, rng), glide(250, 900, d), 2.5, "bp") * swell(d, 0.85, 1.5)
    tone = sine(glide(90, 180, d)) * swell(d, 0.8, 2) * 0.5
    wet = bubbles(d, rng, 4, 300, 700, (0.05, 0.1), 1.5, (0.2, 0.5))
    return mix(d, (0, click(rng, 0.004, 1200), 0.2), (0, infl), (0, tone), (0, wet, 0.6))


@sfx("Methane_Bubble_Warn", 1, 0.55)
def methane_bubble_warn(rng, v):
    d = 1.0
    buf = np.zeros(n(d))
    t = 0.0
    while t < d - 0.06:                                      # cada vez más deprisa y más agudo
        x = t / d
        f = 150 + 260 * x ** 1.5                             # banda media: se oye también en un portátil
        place(buf, lp(bubble(f * rng.uniform(0.95, 1.05), 0.12, 1.6), 1800), t, 0.4 + 0.6 * x)
        place(buf, lp(bubble(f * 0.5, 0.14, 1.4), 600), t, 0.3)
        t += 0.16 - 0.12 * x
    ramp = np.linspace(0, 1, n(d))
    rumble = lp(noise(d, rng), 180) * (0.2 + 0.8 * ramp ** 2)
    hiss = bp(noise(d, rng), 1500, 5000) * ramp ** 2.5 * 0.2
    return buf + rumble * 0.5 + hiss


@sfx("Methane_Bubble_Pop", 2, 0.6)
def methane_bubble_pop(rng, v):
    p = pv(rng, v)
    d = 0.5
    blop = lp(bubble(75 * p, 0.25, 1.9), 800)
    splash = splat(rng, 0.2, 250, 1500, 0.05, 300, 900, 4)
    gas = whoosh(rng, 0.35, 1800 * p, 500 * p, 1.0)
    return mix(d, (0, blop), (0.05, splash, 0.6), (0.06, gas, 0.6))


# ================================================================ Reacciones: veneno

@sfx("Poison_Puddle", 3, 0.4)
def poison_puddle(rng, v):
    p = pv(rng, v)
    d = 0.25
    sp = splat(rng, 0.15, 400 * p, 2200 * p, 0.03, 600 * p, 1400 * p, 3)
    fz = fizz(rng, 0.22, 500) * expenv(0.22, 0.08, 0.01)
    return mix(d, (0, sp), (0.02, fz, 0.6))


@sfx("Poison_ToxicCloud", 1, 0.55)
def poison_toxic_cloud(rng, v):
    d = 1.0
    rise = svf(noise(d, rng), glide(500, 3500, d), 1.3, "bp") * swell(d, 0.4, 1.4)
    acid = fizz(rng, d, 450) * swell(d, 0.3, 1.2)
    bub = bubbles(d, rng, 10, 500, 1300, (0.04, 0.09), 1.7, (0.2, 0.5))
    low = whoosh(rng, 0.6, 200, 600, 1.0)
    return mix(d, (0, click(rng, 0.004, 1500), 0.3), (0, rise), (0, acid, 0.5), (0, bub, 0.5), (0, low, 0.8))


@sfx("Poison_Bulb_Burst", 2, 0.55)
def poison_bulb_burst(rng, v):
    p = pv(rng, v)
    d = 0.45
    pop = thump(260 * p, 90 * p, 0.12, 0.03) + bp(noise(0.03, rng), 600, 2500) * expenv(0.03, 0.006, 0.0003)
    jet = noise_sweep(0.25, 2800 * p, 900 * p, rng, 1.2) * expenv(0.25, 0.07, 0.002)
    sp = splat(rng, 0.2, 400, 2000, 0.04, 500, 1200, 4)
    fz = fizz(rng, 0.3, 400) * expenv(0.3, 0.1, 0.01)
    return mix(d, (0, pop), (0.01, jet, 0.8), (0.08, sp, 0.6), (0.06, fz, 0.4))


@sfx("Poison_Bulb_Regrow", 1, 0.3)
def poison_bulb_regrow(rng, v):
    d = 0.8
    creak = grains(d, rng, 14, 700, 2600, d * 0.8, (0.2, 0.6), 0.006)
    swellx = svf(noise(d, rng), glide(300, 1100, d), 2.0, "bp") * swell(d, 0.85, 1.6)
    return mix(d, (0, click(rng, 0.004, 1500), 0.2), (0, creak, 0.6), (0, swellx))


# ================================================================ Fumarolas

@sfx("Fumarole_Warn", 1, 0.6)
def fumarole_warn(rng, v):
    d = 1.0
    N = n(d)
    ramp = np.linspace(0, 1, N)
    rumble = lp(noise(d, rng), 220) * (0.2 + 0.8 * ramp ** 1.6)
    gurgle = np.zeros(N)
    t = 0.0
    while t < d - 0.05:
        x = t / d
        place(gurgle, lp(bubble(rng.uniform(180, 280) * (1 + x), 0.08, 1.6), 2000), t, 0.3 + 0.7 * x)
        t += rng.uniform(0.07, 0.12) * (1 - 0.6 * x)
    hiss = hp(noise(d, rng), 2000) * ramp ** 2.2 * 0.4
    return rumble + gurgle * 0.7 + hiss


@sfx("Fumarole_Erupt", 2, 0.7)
def fumarole_erupt(rng, v):
    p = pv(rng, v)
    d = 0.8
    blast = dist(thump(120 * p, 50 * p, 0.3, 0.1), 1.5)
    jet = svf(noise(d, rng), glide(1200 * p, 3500 * p, d), 0.8, "bp") * expenv(d, 0.3, 0.003)
    steam = hp(noise(d, rng), 4000) * expenv(d, 0.25, 0.005) * 0.4
    fire = roar(rng, 0.7, 900, 0.1, 1.0)
    return mix(d, (0, click(rng, 0.005, 1500), 0.5), (0, blast), (0, jet), (0, steam), (0.03, fire, 0.7))


# ================================================================ Vegetación

def leaves(rng, d, lo=1500, hi=6000, count=18, span=None):
    """Roce de frondas: grano blando y un soplo de hojas."""
    swish = bp(noise(d, rng), lo, hi) * swell(d, 0.4, 1.5) * 0.5
    return swish + grains(d, rng, count, lo, hi, span or d * 0.85, (0.15, 0.5), 0.008)


@sfx("Fern_Rustle", 3, 0.3)
def fern_rustle(rng, v):
    p = pv(rng, v, 0.08)
    d = 0.25
    return mix(d, (0, click(rng, 0.003, 3000), 0.15), (0, leaves(rng, d, 1400 * p, 5500 * p, 16)))


@sfx("Fern_Burn", 2, 0.55)
def fern_burn(rng, v):
    p = pv(rng, v)
    d = 0.9
    flare = whoosh(rng, 0.4, 400 * p, 2500 * p, 1.1)
    body = roar(rng, 0.85, 1000 * p, 0.25, 1.4)
    crack = crackle(rng, 0.85, 45, 1500, 7500, 0.8, accel=-0.3, gain=(0.2, 0.9))
    return mix(d, (0, click(rng, 0.004, 2000), 0.4), (0, flare), (0.02, body, 0.8), (0.03, crack, 0.7))


@sfx("Brush_BurnAway", 1, 0.55)
def brush_burn_away(rng, v):
    d = 1.2
    snaps = grains(0.7, rng, 18, 600, 3000, 0.65, (0.3, 1.0), 0.01, accel=0.5)
    crumble = grains(0.9, rng, 50, 1500, 6000, 0.85, (0.1, 0.4), 0.004)
    sigh = whoosh(rng, 0.9, 1500, 300, 0.9)
    thud = thump(110, 50, 0.3, 0.1)
    return mix(d, (0, click(rng, 0.005, 1500), 0.3), (0, snaps), (0.25, crumble, 0.6), (0.3, sigh, 0.7),
               (0.55, thud, 0.5))


# ================================================================ Lodo y agua

@sfx("Mud_Sink_Loop", 1, 0.4, loop=True)
def mud_sink_loop(rng, v):
    d = 2.0
    N = n(d)
    t = np.arange(N) / N
    suck = loop_band(d, rng, 120, 600) * (0.6 + 0.4 * np.sin(2 * np.pi * 2 * t))
    buf = suck * 0.4
    for i in range(6):
        place(buf, lp(bubble(rng.uniform(70, 130), rng.uniform(0.15, 0.25), 1.4), 700),
              i * d / 6 + rng.uniform(-0.05, 0.05), rng.uniform(0.5, 0.9), True)
    for _ in range(4):
        sq = noise_sweep(0.12, 800, 250, rng, 2.0) * expenv(0.12, 0.04, 0.003)
        place(buf, sq, rng.uniform(0, d), 0.3, True)
    return buf


@sfx("Mud_Escape", 2, 0.5)
def mud_escape(rng, v):
    p = pv(rng, v)
    d = 0.4
    suck = noise_sweep(0.15, 300 * p, 1400 * p, rng, 2.0) * expenv(0.15, 0.06, 0.002)
    plop = lp(bubble(160 * p, 0.12, 2.2), 1500)
    drips = bubbles(0.25, rng, 3, 500, 1000, (0.03, 0.06), 1.8, (0.2, 0.4))
    return mix(d, (0, suck, 0.7), (0.1, plop), (0.15, drips, 0.6))


@sfx("Mud_Drown", 1, 0.6)
def mud_drown(rng, v):
    d = 0.9
    gulp = lp(sine(glide(160, 55, 0.35)) * expenv(0.35, 0.12, 0.002), 600)
    bub = bubbles(0.6, rng, 9, 90, 300, (0.06, 0.14), 1.5, (0.3, 0.8))
    thud = thump(100, 40, 0.3, 0.08)
    return mix(d, (0, click(rng, 0.004, 900), 0.3), (0, gulp), (0.15, bub, 0.6), (0.55, thud, 0.8))


@sfx("Mud_Splash", 3, 0.4)
def mud_splash(rng, v):
    p = pv(rng, v)
    d = 0.15
    return splat(rng, d, 150 * p, 900 * p, 0.03, 150 * p, 400 * p, 2) + thump(140 * p, 60, 0.08, 0.02) * 0.6


@sfx("Water_Splash", 3, 0.35)
def water_splash(rng, v):
    p = pv(rng, v)
    d = 0.15
    return splat(rng, d, 700 * p, 4500 * p, 0.025, 700 * p, 1800 * p, 3)


# ================================================================ Telarañas

@sfx("Web_Stick", 2, 0.45)
def web_stick(rng, v):
    p = pv(rng, v)
    d = 0.25
    stretch = tri(glide(500 * p, 260 * p, 0.2)) * expenv(0.2, 0.07, 0.002) * 0.6
    tack = bp(noise(0.08, rng), 900, 4000) * expenv(0.08, 0.02, 0.0005)
    snap = click(rng, 0.004, 3500)
    return mix(d, (0, tack, 0.7), (0, stretch), (0.17, snap, 0.5))


@sfx("Web_Burn", 2, 0.45)
def web_burn(rng, v):
    d = 0.5
    sizz = crackle(rng, d, 40, 3000, 9000, d * 0.9, accel=-0.5, gain=(0.15, 0.7))
    puffx = whoosh(rng, 0.3, 800, 3000, 1.2)
    return mix(d, (0, click(rng, 0.003, 3000), 0.3), (0, sizz), (0, puffx, 0.5))


# ================================================================ Jugador: esquivas de los cuerpos

from sintesis import midi as _midi  # noqa: E402

D5, F5, A5, D6, A6 = _midi(74), _midi(77), _midi(81), _midi(86), _midi(93)


@sfx("Player_Dodge_Frog", 3, 0.45)
def dodge_frog(rng, v):
    p = pv(rng, v)
    d = 0.25
    spring = sine(glide(260 * p, 620 * p, 0.14)) * expenv(0.14, 0.05, 0.002)
    push = bp(noise(0.03, rng), 600, 2500) * expenv(0.03, 0.008, 0.0003)
    air = whoosh(rng, 0.18, 700 * p, 2200 * p, 1.4)
    wet = bubble(900 * p, 0.05, 1.6)
    return mix(d, (0, push, 0.6), (0, spring), (0.02, air, 0.5), (0.1, wet, 0.25))


@sfx("Player_Dodge_Diplocaulus", 3, 0.45)
def dodge_diplocaulus(rng, v):
    p = pv(rng, v)
    d = 0.3
    fc = 900 * p * (1 + 0.8 * np.sin(np.linspace(0, 2 * np.pi, n(d))) ** 2)   # va y vuelve
    swish = svf(noise(d, rng), fc, 1.6, "bp") * swell(d, 0.4, 1.3)
    skin = bp(noise(d, rng), 1500, 4000) * swell(d, 0.5, 2) * 0.25
    return mix(d, (0, click(rng, 0.003, 2500), 0.3), (0, swish), (0, skin))


@sfx("Player_Dodge_Scorpion", 3, 0.45)
def dodge_scorpion(rng, v):
    p = pv(rng, v)
    d = 0.35
    curl = clack(rng, 1400 * p) * 0.8
    roll = np.zeros(n(d))
    t = 0.05
    while t < d - 0.03:                                       # tamborileo de quitina que rueda
        place(roll, clack(rng, rng.uniform(900, 1500) * p, 0.02), t, rng.uniform(0.25, 0.55))
        t += rng.uniform(0.035, 0.05)
    rumble = lp(noise(d, rng), 600) * swell(d, 0.4, 1.2) * 0.3
    return mix(d, (0, curl), (0, roll), (0, rumble))


def clack(rng, f, d=0.03):
    """Chasquido de quitina: un tono corto inarmónico y grano."""
    return (sine(glide(f, f * 0.85, d)) * expenv(d, d / 5, 0.0003)
            + bp(noise(d, rng), 2000, 6000) * expenv(d, d / 8, 0.0002) * 0.6)


# ================================================================ Boca: Escupitajo venenoso

@sfx("Mouth_Spit_Shoot", 3, 0.5)
def spit_shoot(rng, v):
    p = pv(rng, v)
    d = 0.2
    gulp = sine(glide(300 * p, 520 * p, 0.08)) * expenv(0.08, 0.03, 0.002)
    ptoo = bp(noise(0.06, rng), 900 * p, 3500 * p) * expenv(0.06, 0.015, 0.0005)
    blob = bubble(650 * p, 0.09, 1.8)
    return mix(d, (0, gulp, 0.6), (0.05, ptoo), (0.06, blob, 0.6))


@sfx("Mouth_Spit_Trail", 3, 0.18)
def spit_trail(rng, v):
    p = pv(rng, v, 0.1)
    d = 0.08
    return mix(d, (0, bubble(700 * p, 0.06, 1.5), 0.7), (0, bp(noise(0.02, rng), 800, 2500) * expenv(0.02, 0.005, 0.0003), 0.4))


@sfx("Mouth_Spit_Burst", 2, 0.6)
def spit_burst(rng, v):
    p = pv(rng, v)
    d = 0.45
    thud = thump(240 * p, 100 * p, 0.12, 0.035)
    sp = splat(rng, 0.25, 500 * p, 3000 * p, 0.05, 500, 1300, 5)
    fz = fizz(rng, 0.35, 450) * expenv(0.35, 0.12, 0.01)
    return mix(d, (0, click(rng, 0.004, 1500), 0.4), (0, thud), (0, sp, 0.8), (0.05, fz, 0.5))


@sfx("Mouth_Spit_Split", 1, 0.45)
def spit_split(rng, v):
    d = 0.3
    parts = [(i * 0.03, bubble(f, 0.08, 1.9), 0.8) for i, f in enumerate((700, 820, 640, 900))]
    return mix(d, (0, click(rng, 0.003, 2500), 0.3), *parts, (0, bp(noise(0.1, rng), 800, 3000) * expenv(0.1, 0.03), 0.4))


# ================================================================ Boca: Peonza

SPIN_D = 0.5     # bucle de 0,5 s: 4 batidos de aire (8 por segundo)


@sfx("Mouth_Spin_Start", 1, 0.45)
def spin_start(rng, v):
    d = 0.2
    up = svf(noise(d, rng), glide(400, 1600, d), 1.5, "bp") * swell(d, 0.9, 1.3)
    return mix(d, (0, click(rng, 0.003, 2000), 0.3), (0, up))


@sfx("Mouth_Spin_Loop", 1, 0.35, loop=True)
def spin_loop(rng, v):
    d = SPIN_D
    N = n(d)
    t = np.arange(N) / N
    air = loop_band(d, rng, 600, 2400)
    beat = 0.2 + 0.8 * np.abs(np.sin(np.pi * 4 * t)) ** 2         # 4 batidos por bucle
    hum = loop_sine(330, d) * 0.2 + loop_sine(660, d) * 0.08
    return air * beat * 0.6 + hum * beat


@sfx("Mouth_Spin_Hit", 3, 0.4)
def spin_hit(rng, v):
    p = pv(rng, v, 0.08)
    d = 0.08
    slap = bp(noise(0.03, rng), 700 * p, 3000 * p) * expenv(0.03, 0.007, 0.0003)
    flesh = thump(260 * p, 140 * p, 0.07, 0.015)
    return mix(d, (0, slap), (0, flesh, 0.7))


@sfx("Mouth_Spin_Dizzy", 1, 0.4)
def spin_dizzy(rng, v):
    d = 0.5
    parts = []
    for i, f in enumerate((A5, D6, A5)):
        wob = f * (1 + 0.03 * np.sin(2 * np.pi * 9 * np.arange(n(0.14)) / SR))
        parts.append((i * 0.13, tri(wob) * expenv(0.14, 0.05, 0.003), 0.7))
    return mix(d, *parts)


@sfx("Mouth_Spin_Deflect", 2, 0.45)
def spin_deflect(rng, v):
    p = pv(rng, v)
    d = 0.12
    ping = sine(glide(1800 * p, 2600 * p, 0.08)) * expenv(0.08, 0.025, 0.0005)
    return mix(d, (0, click(rng, 0.004, 3000), 0.6), (0, ping, 0.7), (0, bubble(1200 * p, 0.05, 1.5), 0.3))


@sfx("Mouth_Spin_Boomerang", 1, 0.55)
def spin_boomerang(rng, v):
    d = 0.6
    N = n(d)
    x = np.linspace(0, 1, N)
    shape = np.sin(np.pi * x)                                  # se aleja (más grave y flojo) y vuelve
    fc = 2000 - 1300 * shape
    air = svf(noise(d, rng), fc, 3.0, "bp")
    beat = 0.3 + 0.7 * np.abs(np.sin(2 * np.pi * 8 * x * d)) ** 2
    whistle = np.sin(2 * np.pi * np.cumsum(1300 - 650 * shape) / SR) * 0.35   # silbido que baja y vuelve (doppler)
    gain = 1 - 0.75 * shape
    return (air * beat + whistle) * gain


# ================================================================ Boca: Chorro de vinagre

@sfx("Mouth_Vinegar_Start", 1, 0.45)
def vinegar_start(rng, v):
    d = 0.15
    squeeze = thump(300, 180, 0.05, 0.015)
    hiss = svf(noise(d, rng), glide(1500, 5000, d), 0.9, "bp") * swell(d, 0.8, 1.2)
    return mix(d, (0, squeeze, 0.5), (0.01, hiss))


@sfx("Mouth_Vinegar_Loop", 1, 0.35, loop=True)
def vinegar_loop(rng, v):
    d = 1.0
    N = n(d)
    t = np.arange(N) / N
    spray = loop_band(d, rng, 2500, 7000) * (0.85 + 0.15 * np.sin(2 * np.pi * 6 * t))
    body = loop_band(d, rng, 700, 1800) * 0.4
    buf = spray * 0.5 + body
    for _ in range(14):
        place(buf, bubble(rng.uniform(800, 1500), 0.04, 1.6), rng.uniform(0, d), rng.uniform(0.1, 0.25), True)
    return buf


@sfx("Mouth_Vinegar_Hit", 3, 0.25)
def vinegar_hit(rng, v):
    p = pv(rng, v, 0.08)
    d = 0.06
    return mix(d, (0, bp(noise(0.04, rng), 1500 * p, 6000 * p) * expenv(0.04, 0.008, 0.0003)),
               (0.005, fizz(rng, 0.05, 600) * expenv(0.05, 0.015, 0.002), 0.4))


@sfx("Mouth_Vinegar_Empty", 1, 0.45)
def vinegar_empty(rng, v):
    d = 0.35
    parts = []
    for i, (t0, g) in enumerate(((0, 1.0), (0.1, 0.7), (0.19, 0.45), (0.26, 0.25))):
        puff_ = bp(noise(0.05, rng), 1200, 4500) * expenv(0.05, 0.012, 0.0005)
        parts.append((t0, puff_, g))
        parts.append((t0, bubble(500 - 60 * i, 0.05, 1.3), g * 0.4))
    return mix(d, *parts)


@sfx("Mouth_Vinegar_Ready", 1, 0.35)
def vinegar_ready(rng, v):
    d = 0.25
    gurgle = bubbles(0.12, rng, 4, 500, 1100, (0.03, 0.06), 1.7, (0.3, 0.6))
    shine = sine(D6, 0.15) * expenv(0.15, 0.05, 0.003) * 0.5 + sine(A6, 0.15) * expenv(0.15, 0.04, 0.003) * 0.25
    return mix(d, (0, gurgle), (0.09, shine))


@sfx("Mouth_Vinegar_Splash", 2, 0.45)
def vinegar_splash(rng, v):
    p = pv(rng, v)
    d = 0.25
    sp = splat(rng, 0.2, 1200 * p, 6000 * p, 0.035, 900, 2000, 4)
    return mix(d, (0, click(rng, 0.003, 3000), 0.4), (0, sp), (0.02, fizz(rng, 0.2, 500) * expenv(0.2, 0.06, 0.005), 0.4))


# ================================================================ Patas de salto

@sfx("Limbs_Jump_Takeoff", 3, 0.45)
def jump_takeoff(rng, v):
    p = pv(rng, v)
    d = 0.15
    kick = thump(220 * p, 120 * p, 0.06, 0.015)
    up = whoosh(rng, 0.13, 600 * p, 2400 * p, 1.6)
    return mix(d, (0, kick, 0.7), (0, up))


@sfx("Limbs_Jump_Land", 3, 0.5)
def jump_land(rng, v):
    p = pv(rng, v)
    d = 0.2
    thud = thump(180 * p, 70 * p, 0.15, 0.04)
    soft = bp(noise(0.06, rng), 400, 1800) * expenv(0.06, 0.015, 0.0005)
    return mix(d, (0, thud), (0, soft, 0.6))


@sfx("Limbs_Jump_Stun", 1, 0.6)
def jump_stun(rng, v):
    d = 0.4
    slam = dist(thump(160, 55, 0.25, 0.07), 1.5)
    wave = bp(noise(0.3, rng), 500, 2000) * expenv(0.3, 0.08, 0.002)
    buzz = tri(A5 * (1 + 0.02 * np.sin(2 * np.pi * 14 * np.arange(n(0.3)) / SR))) * expenv(0.3, 0.12, 0.01)
    return mix(d, (0, slam), (0, wave, 0.5), (0.05, buzz, 0.25))


# ================================================================ Patas hilanderas

def twang(rng, f, d, bright=1.0):
    """Hilo de seda tenso: cuerda pulsada fina con un poco de roce."""
    s = tri(f, d) * expenv(d, d / 3, 0.001) + sine(f * 2.01, d) * expenv(d, d / 5, 0.001) * 0.4
    return s + hp(noise(d, rng), 4000) * expenv(d, 0.01, 0.0005) * 0.3 * bright


@sfx("Limbs_Spinner_Thread", 2, 0.4)
def spinner_thread(rng, v):
    p = pv(rng, v)
    d = 0.3
    draw = svf(noise(0.18, rng), glide(2000 * p, 4500 * p, 0.18), 3.0, "bp") * swell(0.18, 0.6, 1.2)
    return mix(d, (0, draw, 0.6), (0.16, twang(rng, A5 * p, 0.14), 0.8))


@sfx("Limbs_Spinner_Snare", 1, 0.35)
def spinner_snare(rng, v):
    d = 0.3
    vib = twang(rng, D5, 0.28) * (1 + 0.4 * np.sin(2 * np.pi * 18 * np.arange(n(0.28)) / SR))
    tack = bp(noise(0.05, rng), 900, 3500) * expenv(0.05, 0.012, 0.0005)
    return mix(d, (0, tack, 0.6), (0, vib, 0.8))


@sfx("Limbs_Spinner_Web", 1, 0.5)
def spinner_web(rng, v):
    d = 0.5
    parts = [(i * 0.025, twang(rng, f, 0.4), 0.6) for i, f in enumerate((D5, F5, A5, D6))]
    tack = bp(noise(0.06, rng), 900, 3500) * expenv(0.06, 0.015, 0.0005)
    return mix(d, (0, tack, 0.6), *parts)


# ================================================================ Piel anfibia

@sfx("Skin_Mucus_Charged", 1, 0.4)
def mucus_charged(rng, v):
    d = 0.4
    swellx = svf(noise(d, rng), glide(500, 1500, d), 2.5, "bp") * swell(d, 0.7, 1.5) * 0.6
    wet = bubbles(d, rng, 4, 600, 1200, (0.04, 0.08), 1.6, (0.2, 0.4))
    shine = sine(D6, 0.2) * expenv(0.2, 0.07, 0.004) * 0.35
    return mix(d, (0, click(rng, 0.003, 1500), 0.2), (0, swellx), (0, wet, 0.6), (0.22, shine))


@sfx("Skin_Mucus_Absorb", 2, 0.55)
def mucus_absorb(rng, v):
    p = pv(rng, v)
    d = 0.3
    blop = lp(bubble(260 * p, 0.18, 1.4), 1500)
    hit = bp(noise(0.03, rng), 500, 2000) * expenv(0.03, 0.008, 0.0003)
    wob = am(lp(noise(0.25, rng), 900) * expenv(0.25, 0.08, 0.002), 14, 0.6)
    return mix(d, (0, hit, 0.6), (0, blop), (0.02, wob, 0.4))


@sfx("Skin_Mucus_Reflect", 1, 0.5)
def mucus_reflect(rng, v):
    d = 0.3
    boing = sine(glide(400, 1100, 0.15)) * expenv(0.15, 0.06, 0.002)
    return mix(d, (0, bp(noise(0.03, rng), 600, 2500) * expenv(0.03, 0.008, 0.0003), 0.5), (0, boing),
               (0.08, bubble(1200, 0.06, 1.6), 0.4))


# ================================================================ Escamas carbonizadas

@sfx("Skin_Char_Kindle", 1, 0.45)
def char_kindle(rng, v):
    d = 0.4
    glow = roar(rng, d, 600, 0.6, 1.5)
    embers = crackle(rng, d, 16, 1800, 6500, d * 0.9, gain=(0.15, 0.5))
    return mix(d, (0, click(rng, 0.003, 2000), 0.25), (0, glow, 0.9), (0, embers, 0.6))


@sfx("Skin_Char_Flare", 1, 0.6)
def char_flare(rng, v):
    d = 0.6
    burst = whoosh(rng, 0.35, 300, 2200, 1.0)
    ring = roar(rng, 0.5, 1200, 0.15, 1.2)
    thud = thump(200, 80, 0.15, 0.04)
    sparks = crackle(rng, 0.5, 24, span=0.45, gain=(0.2, 0.6))
    return mix(d, (0, click(rng, 0.004, 1800), 0.4), (0, thud, 0.6), (0, burst), (0.03, ring), (0.05, sparks, 0.5))


# ================================================================ Órganos

@sfx("Organ_Methane_Release", 1, 0.5)
def methane_release(rng, v):
    d = 0.6
    squeeze = thump(240, 120, 0.1, 0.03)
    gas = svf(noise(0.5, rng), glide(1800, 500, 0.5), 1.2, "bp") * swell(0.5, 0.2, 1.1)
    bub = bubbles(0.5, rng, 7, 250, 600, (0.06, 0.12), 1.5, (0.2, 0.5))
    return mix(d, (0, squeeze, 0.5), (0.02, gas), (0.05, bub, 0.6))


@sfx("Organ_Methane_Cloud_Loop", 1, 0.15, loop=True)
def methane_cloud_loop(rng, v):
    d = 2.0
    N = n(d)
    t = np.arange(N) / N
    buf = loop_band(d, rng, 300, 1400) * (0.6 + 0.4 * np.sin(2 * np.pi * 2 * t)) * 0.4
    buf += loop_band(d, rng, 2500, 5000) * 0.08
    for _ in range(7):
        place(buf, bubble(rng.uniform(250, 500), rng.uniform(0.06, 0.12), 1.5), rng.uniform(0, d), rng.uniform(0.2, 0.4), True)
    return buf


@sfx("Organ_Pheromone_Cast", 1, 0.5)
def pheromone_cast(rng, v):
    d = 0.6
    spray = bp(noise(0.3, rng), 3000, 8000) * expenv(0.3, 0.1, 0.003) * 0.5
    ab5 = _midi(80)
    chord = sum(tri(f * (1 + 0.004 * k), 0.55) * expenv(0.55, 0.11, 0.01) for k, f in enumerate((D5, ab5, D6)))
    bend = np.linspace(1, 0.94, n(0.55))                         # el acorde se tuerce hacia abajo
    chord = chord * 0.35
    warped = tri(D5 * bend) * expenv(0.55, 0.13, 0.01) * 0.4
    return mix(d, (0, spray), (0.03, chord), (0.03, warped))


@sfx("Status_Confused", 1, 0.4)
def status_confused(rng, v):
    d = 0.5
    N = n(0.45)
    x = np.arange(N) / SR
    f = A5 * (1 + 0.06 * np.sin(2 * np.pi * 7 * x)) * np.linspace(1, 0.94, N)
    trill = tri(f) * expenv(0.45, 0.18, 0.003)
    return mix(d, (0, click(rng, 0.003, 3000), 0.2), (0, trill))


# ================================================================ Ayudas del ámbar, parásitos y hongos

from efectos import bell  # noqa: E402

D4, F4, A4 = _midi(62), _midi(65), _midi(69)


def amber_chime(f, d, index=0.9):
    """Cristal cálido de ámbar: campana FM suave (poco brillo) con un parcial grave."""
    x = bell(f, d, d / 3, 2.0, index) + sine(f * 0.5, d) * expenv(d, d / 4, 0.003) * 0.3
    return x * np.minimum(1, np.linspace(3.3, 0, len(x)))         # se apaga del todo, sin corte


def resin(rng, d, f0=180, f1=120):
    """Resina espesa: tono grave que se arrastra con ruido viscoso que tiembla despacio."""
    goo = am(lp(noise(d, rng), 700) * swell(d, 0.35, 1.3), rng.uniform(5, 8), 0.5)
    tone = sine(glide(f0, f1, d)) * swell(d, 0.4, 1.5) * 0.5
    return goo + tone


def shards(rng, d, count, lo=2500, hi=6500, accel=0.5):
    """Esquirlas que caen tintineando: tonos cortos agudos cada vez más separados."""
    buf = np.zeros(n(d))
    for i in range(count):
        x = (i / max(1, count - 1)) ** (1 + accel)
        f = np.exp(rng.uniform(np.log(lo), np.log(hi)))
        dd = rng.uniform(0.03, 0.07)
        place(buf, sine(f, dd) * expenv(dd, dd / 4, 0.0003), x * d * 0.9, rng.uniform(0.2, 0.7) * (1 - 0.5 * x))
    return buf


def crack(rng, d, lo=800, hi=4500, count=10):
    return grains(d, rng, count, lo, hi, d * 0.9, (0.3, 1.0), 0.006, accel=-0.3)


def suck(rng, f=500, d=0.1):
    """Chupetón de ventosa: soplo corto hacia arriba que se cierra en un «tsk» húmedo."""
    s = noise_sweep(d, f, f * 2.5, rng, 3.0) * swell(d, 0.6, 1.3)
    return mix(d, (0, s), (d * 0.75, bubble(f * 1.6, 0.04, 1.4), 0.4))


def spore_puff(rng, d=0.15, lo=500, hi=2200):
    return puff(rng, d, lo, hi, d / 3)


def glimmer(rng, d, count=5):
    """Titileo bioluminiscente: notas suaves y agudas de la escala de re que aparecen y se apagan."""
    buf = np.zeros(n(d))
    for _ in range(count):
        f = rng.choice([D6, F5 * 2, A5 * 2, _midi(88)])
        dd = rng.uniform(0.12, 0.2)
        place(buf, sine(f, dd) * swell(dd, 0.3, 1.5), rng.uniform(0, d - dd), rng.uniform(0.15, 0.35))
    return buf


# ================================================================ Especiales del ámbar

@sfx("Room_AmberBlock_Break", 1, 0.65)
def amber_block_break(rng, v):
    d = 1.0
    creak = crack(rng, 0.35, 500, 3000, 22)
    split = thump(220, 90, 0.2, 0.06) + bp(noise(0.08, rng), 600, 3000) * expenv(0.08, 0.02, 0.0005)
    sh = shards(rng, 0.55, 14)
    chord = sum(amber_chime(f, 0.5) for f in (D5, F5, A5)) * 0.25
    return mix(d, (0, click(rng, 0.004, 1500), 0.3), (0, creak, 1.4), (0.3, split), (0.32, sh, 0.8), (0.4, chord))


@sfx("Special_Cerci_Dash", 3, 0.45)
def cerci_dash(rng, v):
    p = pv(rng, v)
    d = 0.15
    legs = grains(0.11, rng, 12, 2500 * p, 7000 * p, 0.1, (0.3, 0.8), 0.003)
    air = whoosh(rng, 0.12, 900 * p, 3000 * p, 1.5)
    return mix(d, (0, legs), (0.01, air, 0.6))


@sfx("Special_Cerci_Ready", 1, 0.35)
def cerci_ready(rng, v):
    d = 0.2
    tap = lambda: bp(noise(0.01, rng), 3000, 8000) * expenv(0.01, 0.002, 0.0003)
    return mix(d, (0, tap(), 0.6), (0.05, tap(), 0.5), (0.08, amber_chime(A5 * 2, 0.12), 0.4))


@sfx("Special_Capsule_Encase", 1, 0.6)
def capsule_encase(rng, v):
    d = 0.5
    wrap = resin(rng, 0.35, 220, 150)
    set_ = amber_chime(D5, 0.3) * 0.5 + amber_chime(A5, 0.25) * 0.3
    tick = crack(rng, 0.12, 1500, 5000, 6)
    return mix(d, (0, click(rng, 0.003, 1200), 0.3), (0, wrap), (0.25, tick, 0.5), (0.28, set_))


@sfx("Special_Capsule_Heal", 1, 0.55)
def capsule_heal(rng, v):
    d = 0.6
    chord = sum(lp(tri(f, 0.55), 1800) * swell(0.55, 0.3, 1.4) for f in (D4, F4, A4, D5)) * 0.3
    glow = amber_chime(D6, 0.4) * 0.25
    return mix(d, (0, click(rng, 0.003, 1500), 0.15), (0, chord), (0.15, glow))


@sfx("Special_Capsule_Hit", 2, 0.45)
def capsule_hit(rng, v):
    p = pv(rng, v)
    d = 0.1
    knock = lp(amber_chime(400 * p, 0.1, 0.5), 1500)
    return mix(d, (0, bp(noise(0.02, rng), 500, 2000) * expenv(0.02, 0.005, 0.0003), 0.6), (0, knock))


@sfx("Special_Capsule_Shatter", 2, 0.6)
def capsule_shatter(rng, v):
    p = pv(rng, v)
    d = 0.5
    burst = bp(noise(0.12, rng), 1200, 6000) * expenv(0.12, 0.025, 0.0003)
    body = amber_chime(D5 * p, 0.3) * 0.5
    sh = shards(rng, 0.45, 12)
    return mix(d, (0, click(rng, 0.004, 2000), 0.5), (0, burst), (0, body), (0.03, sh, 0.8),
               (0, thump(200 * p, 90, 0.12, 0.03), 0.5))


@sfx("Special_Drop_Shoot", 2, 0.45)
def drop_shoot(rng, v):
    p = pv(rng, v)
    d = 0.25
    plop = lp(bubble(300 * p, 0.15, 1.5), 1400)
    honey = resin(rng, 0.2, 260 * p, 200 * p) * 0.6
    return mix(d, (0, click(rng, 0.003, 1500), 0.3), (0, plop), (0.02, honey))


@sfx("Special_Drop_Encase", 2, 0.55)
def drop_encase(rng, v):
    p = pv(rng, v)
    d = 0.45
    cover = resin(rng, 0.25, 200 * p, 140 * p)
    harden = crack(rng, 0.2, 1200, 5000, 10)
    set_ = amber_chime(F5 * p, 0.25) * 0.4
    return mix(d, (0, bp(noise(0.03, rng), 400, 1800) * expenv(0.03, 0.008, 0.0003), 0.6), (0, cover),
               (0.15, harden, 0.6), (0.2, set_))


@sfx("Special_Drop_Hit", 3, 0.45)
def drop_hit(rng, v):
    p = pv(rng, v)
    d = 0.1
    knock = lp(amber_chime(300 * p, 0.1, 0.5), 1200)
    return mix(d, (0, bp(noise(0.02, rng), 400, 1800) * expenv(0.02, 0.005, 0.0003), 0.7), (0, knock))


@sfx("Special_Drop_Shatter", 2, 0.55)
def drop_shatter(rng, v):
    p = pv(rng, v)
    d = 0.5
    burst = bp(noise(0.1, rng), 1500, 7000) * expenv(0.1, 0.02, 0.0003)
    body = amber_chime(A5 * p, 0.22) * 0.4
    sh = shards(rng, 0.45, 18, 3000, 7500)
    return mix(d, (0, click(rng, 0.004, 2500), 0.5), (0, burst), (0, body), (0.02, sh, 0.8))


@sfx("Special_Drop_Fossil", 1, 0.6)
def drop_fossil(rng, v):
    d = 0.6
    freeze = crack(rng, 0.35, 400, 2500, 16)
    stone = sum(lp(amber_chime(f, 0.5, 0.6), 1200) for f in (D4, A4)) * 0.5
    thud = thump(140, 60, 0.25, 0.08)
    return mix(d, (0, click(rng, 0.004, 1200), 0.3), (0, freeze, 0.8), (0.3, thud, 0.6), (0.32, stone))


@sfx("Special_Drop_Ready", 1, 0.35)
def drop_ready(rng, v):
    d = 0.25
    form = lp(bubble(500, 0.1, 1.4), 1500) * 0.6
    return mix(d, (0, form), (0.07, amber_chime(D6, 0.18), 0.5))


# ================================================================ Clases

@sfx("Lineage_Parasite_Attach", 3, 0.45)
def parasite_attach(rng, v):
    p = pv(rng, v)
    d = 0.15
    return mix(d, (0, bp(noise(0.02, rng), 600, 2500) * expenv(0.02, 0.005, 0.0003), 0.5), (0, suck(rng, 450 * p, 0.12)))


@sfx("Lineage_Parasite_Suck", 3, 0.15)
def parasite_suck(rng, v):
    p = pv(rng, v, 0.1)
    d = 0.08
    return suck(rng, 380 * p, 0.07)


@sfx("Lineage_Parasite_Drain", 1, 0.5)
def parasite_drain(rng, v):
    d = 0.5
    long_suck = noise_sweep(0.3, 300, 1100, rng, 3.0) * swell(0.3, 0.7, 1.4)
    beat = thump(120, 70, 0.12, 0.04)
    warm = lp(tri(D5, 0.25), 1500) * expenv(0.25, 0.07, 0.01) * 0.3
    return mix(d, (0, click(rng, 0.003, 1200), 0.2), (0, long_suck), (0.3, beat, 0.8), (0.3, warm))


@sfx("Lineage_Parasite_Jump", 2, 0.45)
def parasite_jump(rng, v):
    p = pv(rng, v)
    d = 0.35
    pop = bubble(700 * p, 0.04, 1.3)
    fly = sine(glide(1400 * p, 700 * p, 0.3)) * swell(0.3, 0.3, 1.2) * 0.4
    wet = bp(noise(0.3, rng), 800, 2500) * swell(0.3, 0.3, 1.2) * 0.3
    return mix(d, (0, click(rng, 0.003, 2500), 0.5), (0, pop, 0.6), (0.02, fly), (0.02, wet), (0.3, suck(rng, 450, 0.05), 0.5))


@sfx("Lineage_Colonize_Floor3", 1, 0.55)
def colonize_floor3(rng, v):
    d = 0.6
    parts = [(t0, spore_puff(rng, 0.15, 400 * f, 1800 * f), 0.8)
             for t0, f in zip((0, 0.1, 0.21, 0.33), rng.uniform(0.85, 1.2, 4))]
    return mix(d, *parts, (0.1, glimmer(rng, 0.5, 6), 0.8))


@sfx("Lineage_Mushroom_Sprout", 2, 0.5)
def mushroom_sprout(rng, v):
    p = pv(rng, v)
    d = 0.4
    push = crack(rng, 0.15, 400, 2000, 8)
    pop = spore_puff(rng, 0.2, 450 * p, 2000 * p)
    return mix(d, (0, push, 0.7), (0.1, thump(200 * p, 120, 0.06, 0.02), 0.5), (0.12, pop), (0.15, glimmer(rng, 0.25, 3), 0.7))


# ================================================================ Toques de los linajes

@sfx("Lineage_Eryops_Wade_Loop", 1, 0.3, loop=True)
def eryops_wade_loop(rng, v):
    d = 1.0
    N = n(d)
    t = np.arange(N) / N
    stroke = 0.35 + 0.65 * np.abs(np.sin(np.pi * 2 * t)) ** 3        # 2 brazadas por bucle
    buf = loop_band(d, rng, 150, 900) * stroke * 0.6
    for k in range(2):
        place(buf, lp(bubble(rng.uniform(110, 160), 0.15, 1.4), 800), k * 0.5 + 0.2, 0.6, True)
        place(buf, noise_sweep(0.15, 300, 900, rng, 2.0) * expenv(0.15, 0.05, 0.003), k * 0.5 + 0.1, 0.25, True)
    return buf


@sfx("Lineage_Eryops_Emerge", 1, 0.5)
def eryops_emerge(rng, v):
    d = 0.4
    out = noise_sweep(0.2, 250, 1200, rng, 2.0) * expenv(0.2, 0.07, 0.003)
    plop = lp(bubble(150, 0.12, 2.0), 1200)
    clods = grains(0.25, rng, 6, 300, 1200, 0.22, (0.3, 0.8), 0.015)
    return mix(d, (0, out, 0.7), (0.08, plop), (0.15, clods, 0.7))


@sfx("Lineage_Xenacanthus_Spine", 2, 0.45)
def xenacanthus_spine(rng, v):
    p = pv(rng, v)
    d = 0.15
    stab = sine(glide(1600 * p, 900 * p, 0.05)) * expenv(0.05, 0.012, 0.0003)
    bone = bp(noise(0.04, rng), 1500, 5000) * expenv(0.04, 0.008, 0.0003)
    return mix(d, (0, bone), (0, stab, 0.7), (0.01, thump(220, 120, 0.08, 0.02), 0.5))


@sfx("Lineage_GasSting_Toxify", 2, 0.5)
def gassting_toxify(rng, v):
    p = pv(rng, v)
    d = 0.5
    hiss = svf(noise(d, rng), glide(1200 * p, 3000 * p, d), 1.5, "bp") * swell(d, 0.3, 1.3) * 0.6
    sour = tri(glide(D5 * p, _midi(73) * p, 0.4), 0.4) * expenv(0.4, 0.15, 0.01) * 0.25   # se agria: re → do#
    return mix(d, (0, click(rng, 0.003, 2000), 0.2), (0, hiss), (0.02, fizz(rng, 0.4, 500) * expenv(0.4, 0.15, 0.01), 0.5),
               (0.05, bubbles(0.4, rng, 6, 500, 1100, (0.04, 0.08), 1.6, (0.2, 0.5)), 0.6), (0.05, sour))


@sfx("Lineage_Centipede_Puddle", 3, 0.4)
def centipede_puddle(rng, v):
    p = pv(rng, v)
    d = 0.2
    legs = grains(0.1, rng, 14, 2000 * p, 6000 * p, 0.09, (0.2, 0.6), 0.003)
    sp = splat(rng, 0.1, 400 * p, 2200 * p, 0.025, 600, 1300, 2)
    return mix(d, (0, legs), (0.09, sp), (0.1, fizz(rng, 0.1, 500) * expenv(0.1, 0.04, 0.003), 0.4))


@sfx("Lineage_Tinder_Shoot", 2, 0.5)
def tinder_shoot(rng, v):
    p = pv(rng, v)
    d = 0.25
    ptoo = bp(noise(0.05, rng), 800 * p, 3000 * p) * expenv(0.05, 0.012, 0.0005)
    puffx = spore_puff(rng, 0.12, 500 * p, 2000 * p)
    sparks = crackle(rng, 0.2, 8, span=0.18, gain=(0.2, 0.6))
    return mix(d, (0, ptoo), (0.02, puffx, 0.6), (0.03, sparks, 0.6))


@sfx("Lineage_Tinder_Trail", 3, 0.2)
def tinder_trail(rng, v):
    p = pv(rng, v, 0.1)
    d = 0.1
    return mix(d, (0, bp(noise(0.01, rng), 1500 * p, 6000 * p) * expenv(0.01, 0.003, 0.0003)),
               (0.005, crackle(rng, 0.08, 4, span=0.06, gain=(0.2, 0.5)), 0.6),
               (0, lp(noise(0.09, rng), 800) * expenv(0.09, 0.025, 0.001), 0.4))


@sfx("Lineage_Tinder_Burst", 1, 0.6)
def tinder_burst(rng, v):
    d = 0.6
    puffx = spore_puff(rng, 0.25, 400, 1800)
    catch = whoosh(rng, 0.3, 400, 2400, 1.1)
    rain = crackle(rng, 0.5, 30, span=0.48, accel=0.6, gain=(0.15, 0.6))
    return mix(d, (0, thump(200, 90, 0.12, 0.03), 0.6), (0, puffx), (0.04, catch, 0.8), (0.06, rain, 0.6))


@sfx("Lineage_Zombie_Rise", 1, 0.55)
def zombie_rise(rng, v):
    d = 0.5
    creak = grains(0.4, rng, 12, 300, 1500, 0.38, (0.3, 0.9), 0.012)
    groan = lp(saw(glide(70, 95, 0.45), 0.45), 600) * swell(0.45, 0.5, 1.5) * 0.4
    sprout = spore_puff(rng, 0.15, 500, 2000)
    return mix(d, (0, click(rng, 0.004, 1000), 0.3), (0, creak), (0, groan), (0.3, sprout, 0.7), (0.3, glimmer(rng, 0.2, 2), 0.5))


@sfx("Lineage_Zombie_Collapse", 1, 0.5)
def zombie_collapse(rng, v):
    d = 0.5
    crumble = grains(0.4, rng, 30, 800, 4000, 0.38, (0.15, 0.6), 0.005, accel=0.3)
    dust = spore_puff(rng, 0.35, 300, 1500)
    return mix(d, (0, thump(150, 60, 0.2, 0.06), 0.7), (0, crumble), (0.05, dust, 0.8))


@sfx("Lineage_Lamprey_Attach", 2, 0.45)
def lamprey_attach(rng, v):
    p = pv(rng, v)
    d = 0.2
    return mix(d, (0, suck(rng, 450 * p, 0.09)), (0.09, suck(rng, 520 * p, 0.09), 0.55))


@sfx("Lineage_Leech_Burst", 1, 0.6)
def leech_burst(rng, v):
    d = 0.5
    burst = splat(rng, 0.25, 300, 2000, 0.05, 300, 900, 5) + thump(220, 90, 0.12, 0.03) * 0.8
    parts = [(0.08 + i * 0.07, suck(rng, rng.uniform(400, 650), 0.07), 0.45) for i in range(4)]
    return mix(d, (0, burst), *parts)


# ================================================================ Ayudas de enemigos

D3, A3 = _midi(50), _midi(57)


def cane(rng, f, d, q=6.0):
    """Golpe hueco de caña o tallo: ruido corto por un resonador y un tono que cae un poco."""
    exc = noise(0.01, rng) * expenv(0.01, 0.002, 0.0002)
    res = svf(np.pad(exc, (0, n(d) - len(exc))), np.full(n(d), f), q, "bp") * expenv(d, d / 3, 0.0005)
    tone = sine(glide(f, f * 0.9, d)) * expenv(d, d / 4, 0.0005) * 0.4
    return res * 3 + tone


def buzz(rng, f, d, wing=55, bright=1800):
    """Zumbido de alas: diente de sierra con el batido de las alas, filtrado."""
    t = np.arange(n(d)) / SR
    wave = saw(f, d) * (0.55 + 0.45 * np.sin(2 * np.pi * wing * t))
    return lp(wave, bright)


def chitter(rng, d, count, lo=1500, hi=5000):
    return grains(d, rng, count, lo, hi, d * 0.9, (0.2, 0.7), 0.004)


def hurt_crunch(rng, p, lo, hi, thump_f, d=0.1):
    return mix(d, (0, bp(noise(0.03, rng), lo * p, hi * p) * expenv(0.03, 0.007, 0.0003)),
               (0, thump(thump_f * p, thump_f * 0.5 * p, 0.08, 0.02), 0.7))


# ================================================================ Calamites

@sfx("Enemy_Calamites_Hurt", 3, 0.5)
def calamites_hurt(rng, v):
    p = pv(rng, v)
    d = 0.12
    return mix(d, (0, cane(rng, 420 * p, 0.12)), (0, bp(noise(0.02, rng), 800, 3000) * expenv(0.02, 0.005, 0.0003), 0.4))


@sfx("Enemy_Calamites_Death", 1, 0.6)
def calamites_death(rng, v):
    d = 0.8
    snaps = [(t0, cane(rng, f, 0.15), 0.8) for t0, f in ((0, 380), (0.12, 330), (0.22, 290))]
    leaves_ = leaves(rng, 0.5, 1200, 4500, 20) * 0.6
    fall = thump(130, 50, 0.3, 0.1)
    return mix(d, *snaps, (0.25, leaves_), (0.35, fall, 0.7))


@sfx("Enemy_Calamites_Sprout", 2, 0.45)
def calamites_sprout(rng, v):
    p = pv(rng, v)
    d = 0.35
    soil = grains(0.12, rng, 8, 300, 1500, 0.1, (0.3, 0.8), 0.01)
    stretch = sine(glide(200 * p, 380 * p, 0.25)) * swell(0.25, 0.4, 1.4) * 0.5
    return mix(d, (0, soil), (0.05, stretch), (0.06, bp(noise(0.25, rng), 500, 1800) * swell(0.25, 0.4, 1.4), 0.3))


@sfx("Enemy_Calamites_SporeRing", 1, 0.55)
def calamites_spore_ring(rng, v):
    d = 0.3
    parts = [(i * 0.018, puff(rng, 0.12, 300 * f, 1200 * f, 0.03), 0.6) for i, f in enumerate(rng.uniform(0.8, 1.2, 10))]
    return mix(d, (0, thump(180, 90, 0.12, 0.04), 0.6), *parts)


@sfx("Enemy_CalamitesSprout_Hurt", 2, 0.35)
def calamites_sprout_hurt(rng, v):
    p = pv(rng, v)
    return mix(0.08, (0, cane(rng, 650 * p, 0.07, 5.0)))


@sfx("Enemy_CalamitesSprout_Death", 2, 0.4)
def calamites_sprout_death(rng, v):
    p = pv(rng, v)
    d = 0.25
    return mix(d, (0, cane(rng, 600 * p, 0.1, 5.0)), (0.04, grains(0.15, rng, 6, 800, 3000, 0.12, (0.2, 0.6), 0.006), 0.6))


@sfx("Enemy_CalamitesSprout_Wither", 1, 0.45)
def calamites_sprout_wither(rng, v):
    d = 0.6
    dry = grains(0.55, rng, 40, 1000, 5000, 0.5, (0.1, 0.5), 0.004)
    sigh = noise_sweep(0.5, 1200, 300, rng, 1.0) * swell(0.5, 0.3, 1.2)
    return mix(d, (0, click(rng, 0.003, 1500), 0.2), (0, dry), (0, sigh, 0.6))


# ================================================================ Medullosa

@sfx("Enemy_Medullosa_Hurt", 3, 0.5)
def medullosa_hurt(rng, v):
    p = pv(rng, v)
    return hurt_crunch(rng, p, 300, 1500, 200, 0.12) + lp(noise(0.12, rng), 600) * expenv(0.12, 0.03, 0.001) * 0.3


@sfx("Enemy_Medullosa_Death", 1, 0.6)
def medullosa_death(rng, v):
    d = 0.7
    fibre = grains(0.45, rng, 22, 300, 1800, 0.42, (0.3, 0.9), 0.012, accel=0.3)
    slump = thump(120, 45, 0.35, 0.1)
    fronds = leaves(rng, 0.5, 800, 3500, 14) * 0.6
    return mix(d, (0, fibre), (0.15, fronds), (0.3, slump, 0.8))


@sfx("Enemy_Medullosa_Launch", 2, 0.55)
def medullosa_launch(rng, v):
    p = pv(rng, v)
    d = 0.3
    tunc = cane(rng, 260 * p, 0.2, 4.0) + thump(160 * p, 70, 0.15, 0.04)
    air = noise_sweep(0.2, 400, 1500, rng, 1.5) * expenv(0.2, 0.06, 0.003)
    return mix(d, (0, tunc), (0.02, air, 0.5))


@sfx("Enemy_Medullosa_Fall", 1, 0.45)
def medullosa_fall(rng, v):
    d = 1.1
    N = n(d)
    x = np.linspace(0, 1, N)
    whistle = sine(glide(1400, 600, d)) * (0.1 + 0.9 * x ** 2)
    air = bp(noise(d, rng), 500, 2000) * (0.1 + 0.9 * x ** 2.5) * 0.4
    return whistle * 0.5 + air


@sfx("Enemy_Medullosa_Impact", 2, 0.6)
def medullosa_impact(rng, v):
    p = pv(rng, v)
    d = 0.4
    thud = thump(170 * p, 60, 0.2, 0.05)
    sp = splat(rng, 0.25, 300 * p, 1800 * p, 0.05, 400, 1000, 4)
    fz = fizz(rng, 0.3, 400) * expenv(0.3, 0.1, 0.01)
    return mix(d, (0, click(rng, 0.004, 1200), 0.4), (0, thud), (0, sp, 0.8), (0.05, fz, 0.4))


# ================================================================ Libélula gigante

@sfx("Enemy_Dragonfly_Buzz_Loop", 1, 0.3, loop=True)
def dragonfly_buzz_loop(rng, v):
    d = 1.0
    N = n(d)
    t = np.arange(N) / N
    f = round(D3 * d) / d                                         # ciclos enteros por bucle
    wave = sum(np.sin(2 * np.pi * f * k * t * d) / k for k in range(1, 9))
    wings = 0.5 + 0.5 * np.sin(2 * np.pi * 40 * t)
    drift = 0.8 + 0.2 * np.sin(2 * np.pi * t)
    return wave * wings * drift                                  # sin filtro: el filtro no es cíclico


@sfx("Enemy_Dragonfly_Warn", 1, 0.6)
def dragonfly_warn(rng, v):
    d = 0.7
    N = n(d)
    x = np.linspace(0, 1, N)
    f = D3 * (1 + 1.0 * x ** 1.5)
    t = np.arange(N) / SR
    wave = np.sin(2 * np.pi * np.cumsum(f) / SR)
    wave = sum(np.sin(2 * np.pi * np.cumsum(f * k) / SR) / k for k in range(1, 7))
    wings = 0.5 + 0.5 * np.sin(2 * np.pi * (40 + 30 * x) * t)
    return lp(wave * wings, 2500) * (0.3 + 0.7 * x)


@sfx("Enemy_Dragonfly_Dive", 2, 0.6)
def dragonfly_dive(rng, v):
    p = pv(rng, v)
    d = 0.4
    pass_ = buzz(rng, glide(A3 * 2 * p, A3 * p, d), d, 60, 2500) * swell(d, 0.4, 1.5)
    air = noise_sweep(d, 2500, 600, rng, 1.2) * swell(d, 0.4, 1.4)
    return mix(d, (0, click(rng, 0.003, 2000), 0.3), (0, pass_), (0, air, 0.7))


@sfx("Enemy_Dragonfly_Land", 2, 0.45)
def dragonfly_land(rng, v):
    p = pv(rng, v)
    d = 0.27
    feet = chitter(rng, 0.08, 5, 1200 * p, 4000 * p)
    fold = clack(rng, 900 * p, 0.04)
    return mix(d, (0, thump(200 * p, 100, 0.08, 0.02), 0.6), (0, feet), (0.15, fold, 0.6))


@sfx("Enemy_Dragonfly_Hurt", 3, 0.45)
def dragonfly_hurt(rng, v):
    p = pv(rng, v)
    return mix(0.1, (0, clack(rng, 1100 * p, 0.05)), (0, chitter(rng, 0.06, 5, 1500, 5000), 0.6))


@sfx("Enemy_Dragonfly_Death", 1, 0.55)
def dragonfly_death(rng, v):
    d = 0.6
    fade_buzz = buzz(rng, glide(A3, D3 * 0.8, 0.45), 0.45, 45, 1800) * expenv(0.45, 0.15, 0.003)
    crash = chitter(rng, 0.15, 8, 1000, 4000)
    return mix(d, (0, fade_buzz), (0.35, thump(170, 70, 0.15, 0.04), 0.6), (0.36, crash, 0.7))


# ================================================================ Escarabajo bombardero

@sfx("Enemy_Bombardier_Warn", 1, 0.6)
def bombardier_warn(rng, v):
    d = 0.6
    N = n(d)
    x = np.linspace(0, 1, N)
    buf = np.zeros(N)
    t = 0.0
    while t < d - 0.03:                                          # hierve cada vez más deprisa
        k = t / d
        place(buf, bubble(rng.uniform(250, 400) * (1 + 1.2 * k), 0.05, 1.7), t, 0.3 + 0.7 * k)
        t += 0.07 - 0.05 * k
    pressure = bp(noise(d, rng), 300, 1500) * (0.1 + 0.9 * x ** 2) * 0.6
    hiss = hp(noise(d, rng), 3000) * x ** 3 * 0.3
    return buf + pressure + hiss


@sfx("Enemy_Bombardier_Flame_Start", 1, 0.6)
def bombardier_flame_start(rng, v):
    d = 0.15
    pff = bp(noise(0.1, rng), 600, 4000) * expenv(0.1, 0.025, 0.0003)
    return mix(d, (0, click(rng, 0.004, 1500), 0.6), (0, thump(220, 90, 0.1, 0.025), 0.7), (0, pff))


@sfx("Enemy_Bombardier_Flame_Loop", 1, 0.5, loop=True)
def bombardier_flame_loop(rng, v):
    d = 1.0
    N = n(d)
    t = np.arange(N) / N
    jet = loop_band(d, rng, 400, 3000)
    pulses = 0.55 + 0.45 * np.abs(np.sin(np.pi * 20 * t)) ** 3     # 20 pulsos químicos por segundo
    low = loop_noise(d, rng, 300, 2)
    buf = jet * pulses * 0.6 + low / np.max(np.abs(low)) * 0.4
    for _ in range(40):
        g = bp(noise(0.004, rng), 2000, 7000) * expenv(0.004, 0.0012, 0.0003)
        place(buf, g, rng.uniform(0, d), rng.uniform(0.15, 0.5), True)
    return buf


@sfx("Enemy_Bombardier_Flame_End", 1, 0.5)
def bombardier_flame_end(rng, v):
    d = 0.3
    last = bp(noise(0.08, rng), 500, 3000) * expenv(0.08, 0.02, 0.0003)
    tail = crackle(rng, 0.25, 12, span=0.24, gain=(0.15, 0.5))
    return mix(d, (0, last), (0.02, tail, 0.7), (0, lp(noise(0.2, rng), 500) * expenv(0.2, 0.06, 0.002), 0.5))


@sfx("Enemy_Bombardier_Hurt", 3, 0.5)
def bombardier_hurt(rng, v):
    p = pv(rng, v)
    shell = clack(rng, 600 * p, 0.06) + cane(rng, 500 * p, 0.08, 8.0) * 0.5
    return mix(0.1, (0, shell))


@sfx("Enemy_Bombardier_Death", 1, 0.6)
def bombardier_death(rng, v):
    d = 0.4
    split = grains(0.2, rng, 10, 600, 3000, 0.18, (0.3, 1.0), 0.008)
    pff = bp(noise(0.15, rng), 600, 3500) * expenv(0.15, 0.04, 0.0005)
    return mix(d, (0, clack(rng, 550, 0.06)), (0.02, split), (0.25, pff, 0.7), (0.25, thump(160, 70, 0.15, 0.04), 0.5))


# ================================================================ Araña de las frondas

@sfx("Enemy_Spider_FernShake_Loop", 1, 0.25, loop=True)
def spider_fern_shake_loop(rng, v):
    d = 1.0
    N = n(d)
    t = np.arange(N) / N
    trem = 0.4 + 0.6 * np.abs(np.sin(np.pi * 7 * t))             # 7 temblores por bucle
    buf = loop_band(d, rng, 1200, 5000) * trem * 0.4
    for _ in range(20):
        g = bp(noise(0.008, rng), 1200, 4500) * expenv(0.008, 0.003, 0.0005)
        place(buf, g, rng.uniform(0, d), rng.uniform(0.2, 0.5), True)
    return buf


@sfx("Enemy_Spider_Emerge", 1, 0.55)
def spider_emerge(rng, v):
    d = 0.4
    burst = leaves(rng, 0.25, 1000, 4500, 16)
    legs = chitter(rng, 0.25, 14, 1200, 4000)
    return mix(d, (0, click(rng, 0.004, 1500), 0.3), (0, burst), (0.1, legs, 0.7), (0, thump(200, 100, 0.08, 0.02), 0.4))


@sfx("Enemy_Spider_Silk", 2, 0.45)
def spider_silk(rng, v):
    p = pv(rng, v)
    d = 0.3
    jet = svf(noise(0.25, rng), glide(1500 * p, 3500 * p, 0.25), 2.5, "bp") * expenv(0.25, 0.09, 0.002)
    return mix(d, (0, bp(noise(0.02, rng), 500, 2000) * expenv(0.02, 0.005, 0.0003), 0.5), (0, jet),
               (0.18, twang(rng, D5 * 0.5 * p, 0.12), 0.4))


@sfx("Enemy_Spider_Leap", 2, 0.45)
def spider_leap(rng, v):
    p = pv(rng, v)
    d = 0.3
    return mix(d, (0, chitter(rng, 0.05, 5, 1000, 3500)), (0.02, whoosh(rng, 0.2, 500 * p, 1800 * p, 1.4), 0.7),
               (0.22, chitter(rng, 0.06, 6, 1000, 3500), 0.8))


@sfx("Enemy_Spider_Hurt", 3, 0.45)
def spider_hurt(rng, v):
    p = pv(rng, v)
    d = 0.1
    shriek = saw(glide(700 * p, 500 * p, 0.07), 0.07) * expenv(0.07, 0.025, 0.002)
    return mix(d, (0, bp(shriek, 500, 2500), 0.5), (0, clack(rng, 800 * p, 0.04)))


@sfx("Enemy_Spider_Death", 1, 0.55)
def spider_death(rng, v):
    d = 0.6
    shriek = bp(saw(glide(750, 300, 0.45), 0.45), 400, 2500) * expenv(0.45, 0.14, 0.003)
    curl = chitter(rng, 0.4, 18, 1000, 4000)
    return mix(d, (0, shriek, 0.6), (0.05, curl), (0.4, thump(160, 70, 0.12, 0.03), 0.5))


# ================================================================ Sapo de fango

def toad_croak(rng, f, d, pulses=22):
    """Croar del sapo: el del ambiente (re) más cerca y con más cuerpo."""
    return croak(rng, f, d, pulses) * 1.5 + lp(saw(f, d), 500) * swell(d, 0.3, 1.5) * 0.3


@sfx("Enemy_Toad_Croak", 2, 0.5)
def toad_croak_sfx(rng, v):
    f = (D3, A3 * 0.5 * 2)[v - 1]
    d = 0.5
    return mix(d, (0, toad_croak(rng, f, 0.45)))


@sfx("Enemy_Toad_Spit", 2, 0.55)
def toad_spit(rng, v):
    p = pv(rng, v)
    d = 0.3
    gargle = am(lp(noise(0.15, rng), 800), 30, 0.7) * expenv(0.15, 0.06, 0.003)
    spit = bp(noise(0.08, rng), 300 * p, 1500 * p) * expenv(0.08, 0.02, 0.0005)
    return mix(d, (0, gargle, 0.7), (0.12, spit), (0.13, lp(bubble(180 * p, 0.12, 1.7), 900), 0.7))


@sfx("Enemy_Toad_BallSplat", 2, 0.5)
def toad_ball_splat(rng, v):
    p = pv(rng, v)
    d = 0.3
    return mix(d, (0, thump(150 * p, 60, 0.15, 0.04)), (0, splat(rng, 0.22, 150 * p, 900 * p, 0.05, 150, 400, 3), 0.9))


@sfx("Enemy_Toad_Dive", 1, 0.55)
def toad_dive(rng, v):
    d = 0.4
    plunge = lp(noise(0.12, rng), 900) * expenv(0.12, 0.03, 0.0005)
    gulp = lp(bubble(110, 0.2, 1.6), 800)
    bub = bubbles(0.3, rng, 5, 90, 250, (0.05, 0.1), 1.5, (0.3, 0.6))
    return mix(d, (0, plunge), (0.02, gulp), (0.1, bub, 0.6))


@sfx("Enemy_Toad_Surface", 1, 0.5)
def toad_surface(rng, v):
    d = 0.3
    return mix(d, (0, lp(bubble(150, 0.12, 2.0), 1000)), (0.08, splat(rng, 0.2, 200, 1000, 0.05, 300, 800, 3), 0.6))


@sfx("Enemy_Toad_Swell", 1, 0.6)
def toad_swell(rng, v):
    d = 0.5
    N = n(d)
    x = np.linspace(0, 1, N)
    tone = saw(glide(90, 260, d), d) * (0.2 + 0.8 * x ** 1.5)
    stretch = svf(noise(d, rng), glide(300, 1500, d), 3.0, "bp") * (0.2 + 0.8 * x ** 2)
    return lp(tone, 900) * 0.6 + stretch * 0.5


@sfx("Enemy_Toad_Burst", 1, 0.7)
def toad_burst(rng, v):
    d = 0.35
    pop = dist(thump(200, 60, 0.2, 0.05), 1.5)
    wet = splat(rng, 0.35, 200, 1500, 0.07, 200, 700, 6)
    return mix(d, (0, click(rng, 0.004, 1200), 0.4), (0, pop), (0, wet))


@sfx("Enemy_Toad_Hurt", 3, 0.5)
def toad_hurt(rng, v):
    p = pv(rng, v)
    d = 0.1
    slap = bp(noise(0.03, rng), 300 * p, 1500 * p) * expenv(0.03, 0.008, 0.0003)
    croc = lp(saw(glide(140 * p, 110 * p, 0.08), 0.08), 700) * expenv(0.08, 0.03, 0.002)
    return mix(d, (0, slap), (0, croc, 0.6))


@sfx("Enemy_Toad_Death", 1, 0.55)
def toad_death(rng, v):
    d = 0.6
    broken = croak(rng, A3 * 0.5, 0.35, 16) * 1.5 * np.linspace(1, 0.3, n(0.35))
    return mix(d, (0, broken), (0.3, thump(120, 50, 0.2, 0.06), 0.7), (0.32, splat(rng, 0.2, 150, 900, 0.05, 150, 400, 3), 0.6))


# ================================================================ Trampas

@sfx("Trap_Carnivore_Snap", 2, 0.65)
def carnivore_snap(rng, v):
    p = pv(rng, v)
    d = 0.2
    shut = thump(240 * p, 90 * p, 0.12, 0.03) + cane(rng, 350 * p, 0.1, 3.0) * 0.6
    wet = bp(noise(0.06, rng), 400, 2500) * expenv(0.06, 0.015, 0.0003)
    return mix(d, (0, click(rng, 0.004, 1500), 0.5), (0, shut), (0.005, wet, 0.6))


@sfx("Trap_Carnivore_Chew", 1, 0.5)
def carnivore_chew(rng, v):
    d = 1.0
    buf = np.zeros(n(d))
    for i in range(4):
        t0 = i * 0.24
        chew = lp(noise(0.16, rng), 700) * expenv(0.16, 0.05, 0.005)
        place(buf, chew, t0, 0.8)
        place(buf, grains(0.1, rng, 5, 300, 1500, 0.08, (0.2, 0.6), 0.01), t0 + 0.02, 0.5)
        place(buf, thump(130, 80, 0.08, 0.02), t0, 0.4)
    return buf


@sfx("Trap_Carnivore_Release", 1, 0.55)
def carnivore_release(rng, v):
    d = 0.3
    spit = bp(noise(0.12, rng), 300, 1800) * expenv(0.12, 0.035, 0.0005)
    return mix(d, (0, spit), (0.02, lp(bubble(160, 0.12, 1.8), 900), 0.6),
               (0.05, splat(rng, 0.15, 300, 1500, 0.04, 400, 900, 2), 0.5))


@sfx("Trap_Carnivore_Eat", 1, 0.6)
def carnivore_eat(rng, v):
    d = 0.7
    gulp = lp(sine(glide(180, 60, 0.4), 0.4) * expenv(0.4, 0.14, 0.003), 600)
    burp = lp(saw(glide(85, 70, 0.2), 0.2), 500) * expenv(0.2, 0.08, 0.01) * am(np.ones(n(0.2)), 25, 0.6)
    return mix(d, (0, lp(noise(0.1, rng), 700) * expenv(0.1, 0.03, 0.001), 0.6), (0, gulp), (0.45, burp, 0.5))


@sfx("Trap_Carnivore_Open", 1, 0.45)
def carnivore_open(rng, v):
    d = 0.3
    peel = noise_sweep(0.2, 400, 1600, rng, 2.0) * swell(0.2, 0.5, 1.3)
    tack = bp(noise(0.04, rng), 800, 3000) * expenv(0.04, 0.01, 0.0005)
    return mix(d, (0, tack, 0.5), (0, peel, 0.7), (0.15, tack, 0.4))


@sfx("Trap_Carnivore_Wither", 1, 0.45)
def carnivore_wither(rng, v):
    d = 0.8
    shrivel = grains(0.6, rng, 30, 500, 3000, 0.55, (0.15, 0.6), 0.006)
    sigh = noise_sweep(0.6, 900, 200, rng, 1.0) * swell(0.6, 0.3, 1.2)
    return mix(d, (0, click(rng, 0.003, 1200), 0.2), (0, shrivel), (0, sigh, 0.7), (0.55, thump(110, 50, 0.2, 0.06), 0.5))


@sfx("Trap_Mayfly_Swarm_Loop", 1, 0.35, loop=True)
def mayfly_swarm_loop(rng, v):
    d = 2.0
    N = n(d)
    t = np.arange(N) / SR
    buf = np.zeros(N)
    for _ in range(14):                                        # muchas alitas finas, desordenadas
        f = round(rng.uniform(380, 900) * d) / d
        wing = round(rng.uniform(90, 140) * d) / d
        ph = rng.uniform(0, 2 * np.pi)
        buf += np.sin(2 * np.pi * f * t + ph) * (0.5 + 0.5 * np.sin(2 * np.pi * wing * t + ph)) * rng.uniform(0.4, 1)
    swirl = 0.75 + 0.25 * np.sin(2 * np.pi * 2 * t / d)
    return buf * swirl + loop_band(d, rng, 2000, 6000) * 0.3


@sfx("Trap_Mayfly_Sting", 3, 0.35)
def mayfly_sting(rng, v):
    p = pv(rng, v, 0.1)
    d = 0.1
    parts = [(rng.uniform(0, 0.05), bp(noise(0.012, rng), 2500 * p, 7000 * p) * expenv(0.012, 0.003, 0.0002), rng.uniform(0.4, 1))
             for _ in range(5)]
    return mix(d, *parts)


@sfx("Trap_Mayfly_Burn", 1, 0.55)
def mayfly_burn(rng, v):
    d = 1.2
    N = n(0.6)
    t = np.arange(N) / SR
    dying = sum(np.sin(2 * np.pi * rng.uniform(400, 900) * t) for _ in range(6)) * (0.5 + 0.5 * np.sin(2 * np.pi * 110 * t))
    dying = bp(dying, 300, 3000) * np.linspace(1, 0, N) ** 1.5
    sizzle = crackle(rng, 0.6, 30, 2500, 8000, 0.55, gain=(0.2, 0.7))
    embers = crackle(rng, 0.7, 18, 1500, 5000, 0.65, accel=0.5, gain=(0.1, 0.4))
    return mix(d, (0, whoosh(rng, 0.25, 600, 2500, 1.2), 0.6), (0, dying, 0.5), (0, sizzle, 0.7), (0.5, embers, 0.6))


# ================================================================ Ayudas de salas

B5, E5, G5 = _midi(83), _midi(76), _midi(79)


def wood_note(f, d):
    """Marimba de madera: fundamental con el parcial ×4 que se apaga antes."""
    return (sine(f, d) * expenv(d, d / 3, 0.002) + sine(f * 4, d) * expenv(d, d / 12, 0.001) * 0.35
            + sine(f * 10, d) * expenv(d, d / 40, 0.0005) * 0.1)


def roots(rng, d, count, lo=200, hi=1400, accel=0.0):
    """Raíces que se retuercen: crujidos de madera viva, más graves y largos que los de las hojas."""
    return grains(d, rng, count, lo, hi, d * 0.9, (0.3, 1.0), 0.015, accel)


def lub_dub(f=70):
    """Latido doble."""
    return mix(0.35, (0, thump(f * 1.6, f, 0.14, 0.05)), (0.17, thump(f * 1.4, f * 0.9, 0.14, 0.05), 0.7))


# ================================================================ Puertas, oleadas y sala limpia

@sfx("Room_Roots_Close", 1, 0.6)
def roots_close(rng, v):
    d = 0.6
    twist = roots(rng, 0.4, 16, accel=0.4)
    lock = thump(150, 60, 0.2, 0.06) + cane(rng, 240, 0.15, 3.0) * 0.6
    return mix(d, (0, click(rng, 0.004, 1000), 0.3), (0, twist), (0.38, lock), (0.38, roots(rng, 0.15, 5), 0.6))


@sfx("Room_Roots_Open", 1, 0.55)
def roots_open(rng, v):
    d = 0.7
    unlock = cane(rng, 260, 0.12, 3.0)
    drag = roots(rng, 0.55, 14, 250, 1200)
    breath = noise_sweep(0.5, 300, 1200, rng, 1.0) * swell(0.5, 0.4, 1.3)
    return mix(d, (0, unlock, 0.8), (0.05, drag), (0.1, breath, 0.5))


@sfx("Room_Cleared_Floor3", 1, 0.55)
def room_cleared_floor3(rng, v):
    d = 0.8
    notes = [(0, wood_note(D5, 0.3), 0.8), (0.11, wood_note(F5, 0.3), 0.8), (0.22, wood_note(B5, 0.5), 1.0),
             (0.22, wood_note(D6, 0.5), 0.35)]
    frog = croak(rng, A3, 0.25, 26) * 0.6
    return mix(d, *notes, (0.5, frog, 0.4))


@sfx("Room_WaveStart_Floor3", 1, 0.6)
def wave_start_floor3(rng, v):
    d = 0.6
    call = croak(rng, D3 * 0.5, 0.5, 14) * 1.5
    horn = lp(saw(glide(D3 * 0.5, D3 * 0.52, 0.5), 0.5), 700) * swell(0.5, 0.3, 1.4) * 0.5
    return mix(d, (0, click(rng, 0.003, 800), 0.2), (0, call), (0, horn), (0, dist(horn, 2.0) * 0.3))


# ================================================================ Cofres de cono

def cone_open(rng, scale, d, big):
    buf = np.zeros(n(d))
    count = 9 if big else 6
    for i in range(count):
        t0 = i * (0.07 if big else 0.06)
        place(buf, cane(rng, rng.uniform(300, 600) / scale, 0.08, 4.0), t0, 0.6)
        place(buf, roots(rng, 0.06, 3, 600, 2500), t0, 0.4)
    warm = wood_note(D5 / scale, 0.4) * 0.5 + wood_note(A5 / scale, 0.4) * 0.3
    place(buf, warm, count * (0.07 if big else 0.06), 0.8)
    if big:
        shine = sine(glide(D6, A6, 0.4), 0.4) * swell(0.4, 0.6, 1.5) * 0.25
        place(buf, shine, 0.6, 1.0)
    return buf


@sfx("Room_ConeChest_Open", 1, 0.6)
def cone_chest_open(rng, v):
    return cone_open(rng, 1.0, 0.8, False)


@sfx("Room_ConeChest_Open_Big", 1, 0.7)
def cone_chest_open_big(rng, v):
    return cone_open(rng, 1.4, 1.05, True) + mix(1.05, (0, thump(110, 50, 0.3, 0.1), 0.5))


# ================================================================ Teletransporte de tronco hueco

@sfx("Room_Teleport_Wake", 1, 0.5)
def teleport_wake(rng, v):
    d = 0.6
    stretch = roots(rng, 0.35, 10, 250, 1200)
    hum = (sine(D3, 0.45) + sine(A3, 0.45) * 0.5) * swell(0.45, 0.8, 1.5)
    return mix(d, (0, click(rng, 0.003, 1000), 0.2), (0, stretch), (0.15, hum, 0.5),
               (0.15, dist(hum, 2.0) * 0.2))


@sfx("Room_Teleport_Warp", 1, 0.6)
def teleport_warp(rng, v):
    d = 0.9
    suck_in = noise_sweep(0.7, 2500, 300, rng, 2.0) * swell(0.7, 0.8, 1.4)
    rise = sine(glide(D3, D5, 0.7), 0.7) * swell(0.7, 0.85, 1.6) * 0.5
    plop = lp(bubble(90, 0.2, 1.8, 0.05), 700)
    return mix(d, (0, click(rng, 0.003, 1500), 0.3), (0, suck_in), (0, rise), (0, dist(rise, 2.0) * 0.2), (0.68, plop))


# ================================================================ Corazones

@sfx("Room_Heart_Drop", 1, 0.5)
def heart_drop(rng, v):
    d = 0.5
    shine = sine(glide(A6, D6, 0.3), 0.3) * expenv(0.3, 0.1, 0.002) * 0.4
    return mix(d, (0, lub_dub(80), 0.9), (0.05, shine))


@sfx("Room_HeartPod_Send", 1, 0.5)
def heartpod_send(rng, v):
    d = 1.1
    N = n(1.0)
    x = np.linspace(0, 1, N)
    whistle = sine(glide(A5, D5, 1.0), 1.0) * (1 - x) ** 1.5 * 0.4
    air = bp(noise(1.0, rng), 800, 3000) * (1 - x) ** 2 * 0.3
    return mix(d, (0, lub_dub(80), 0.8), (0.05, whistle), (0.05, air))


@sfx("Room_HeartPod_Take", 1, 0.55)
def heartpod_take(rng, v):
    d = 0.5
    pod = cane(rng, 380, 0.12, 4.0) + roots(rng, 0.1, 4, 600, 2500) * 0.5
    warm = lp(tri(D5, 0.3), 1500) * expenv(0.3, 0.1, 0.01) * 0.3
    return mix(d, (0, pod), (0.1, lub_dub(75), 0.8), (0.12, warm))


@sfx("Room_HeartPod_Empty", 1, 0.45)
def heartpod_empty(rng, v):
    d = 0.25
    return mix(d, (0, cane(rng, 300, 0.08, 6.0)), (0.11, cane(rng, 260, 0.1, 6.0), 0.8))


# ================================================================ Cucarachas y bichos

def roach_legs(rng, d, count, lo=2500, hi=7000):
    return grains(d, rng, count, lo, hi, d * 0.95, (0.2, 0.7), 0.003)


@sfx("Roach_Emerge", 2, 0.4)
def roach_emerge(rng, v):
    p = pv(rng, v)
    d = 0.3
    scratch = bp(noise(0.12, rng), 1500 * p, 5000 * p) * expenv(0.12, 0.04, 0.002)
    return mix(d, (0, scratch, 0.8), (0.08, roach_legs(rng, 0.2, 14), 0.8))


@sfx("Roach_Scurry_Loop", 1, 0.3, loop=True)
def roach_scurry_loop(rng, v):
    d = 0.5
    buf = np.zeros(n(d))
    for i in range(30):                                       # pasos regulares, algo irregulares
        g = bp(noise(0.004, rng), 2500, 7000) * expenv(0.004, 0.0012, 0.0002)
        place(buf, g, (i / 30) * d + rng.uniform(-0.003, 0.003), rng.uniform(0.3, 0.8), True)
    return buf + loop_band(d, rng, 3000, 7000) * 0.05


@sfx("Roach_Grab", 2, 0.45)
def roach_grab(rng, v):
    p = pv(rng, v)
    d = 0.25
    bite = clack(rng, 1300 * p, 0.03)
    jingle = sine(A6 * p, 0.12) * expenv(0.12, 0.04, 0.001) * 0.3
    return mix(d, (0, bite), (0.04, jingle), (0.03, roach_legs(rng, 0.15, 6), 0.4))


@sfx("Roach_Hide", 1, 0.4)
def roach_hide(rng, v):
    d = 0.3
    scratch = bp(noise(0.25, rng), 1200, 4500) * swell(0.25, 0.15, 1.0)
    return mix(d, (0, roach_legs(rng, 0.12, 8), 0.7), (0.05, scratch * np.linspace(1, 0, n(0.25)) ** 1.5, 0.8))


@sfx("Roach_Hurt", 2, 0.4)
def roach_hurt(rng, v):
    p = pv(rng, v)
    return mix(0.08, (0, clack(rng, 1500 * p, 0.03)), (0, bp(noise(0.02, rng), 2000, 6000) * expenv(0.02, 0.005, 0.0002), 0.5))


@sfx("Roach_Death", 1, 0.45)
def roach_death(rng, v):
    d = 0.4
    crunch = grains(0.1, rng, 8, 1200, 5000, 0.08, (0.4, 1.0), 0.005)
    squeak = sine(glide(2600, 1700, 0.12), 0.12) * expenv(0.12, 0.04, 0.002) * 0.3
    return mix(d, (0, crunch), (0.05, squeak), (0.1, roach_legs(rng, 0.2, 6, 2000, 5000), 0.3))


@sfx("Critter_Scurry", 2, 0.2)
def critter_scurry(rng, v):
    d = 0.25
    legs = roach_legs(rng, d, 12, 2000, 6000) * np.linspace(1, 0.2, n(d))
    return mix(d, (0, legs))


# ================================================================ Sala secreta

E4b = _midi(63)   # mi bemol: el color frigio del secreto


@sfx("Room_RootWall_HeartPlace", 1, 0.65)
def rootwall_heart_place(rng, v):
    d = 0.8
    fit = cane(rng, 220, 0.12, 3.0)
    chord = sum(lp(tri(f, 0.6), 1600) * expenv(0.6, 0.2, 0.01) for f in (D4, E4b, A4)) * 0.3
    return mix(d, (0, fit, 0.6), (0.05, lub_dub(60)), (0.1, chord))


@sfx("Room_RootWall_Open", 1, 0.7)
def rootwall_open(rng, v):
    d = 1.6
    glow = sum(sine(f, 1.4) for f in (D4, A4, D5)) * swell(1.4, 0.65, 1.5) * 0.15
    creak = roots(rng, 0.9, 30, 200, 1300, accel=-0.3)
    tear = dist(thump(110, 40, 0.4, 0.12), 1.8) + roots(rng, 0.3, 14, 300, 2000)
    part = noise_sweep(0.6, 400, 1500, rng, 1.0) * swell(0.6, 0.3, 1.3) * 0.6
    chord = sum(lp(tri(f, 0.7), 1800) * expenv(0.7, 0.25, 0.02) for f in (D4, E4b, A4, D5)) * 0.25
    return mix(d, (0, click(rng, 0.004, 900), 0.3), (0, glow, 1.4), (0, creak, 1.5), (0.88, tear, 0.5), (0.9, part),
               (0.9, chord))


@sfx("Room_PurpleFlower_Pickup", 1, 0.55)
def purple_flower_pickup(rng, v):
    d = 0.4
    bells = [(i * 0.05, bell(f, 0.3, 0.08, 3.5, 1.2), 0.5) for i, f in enumerate((A5, D6, E4b * 4))]
    return mix(d, (0, leaves(rng, 0.12, 2000, 6000, 6), 0.4), *bells)


@sfx("Room_PurpleSeed_Sprout", 1, 0.65)
def purple_seed_sprout(rng, v):
    d = 1.0
    grow = roots(rng, 0.6, 14, 400, 2000, accel=-0.2) * 0.7
    rise = sine(glide(D4, D5, 0.6), 0.6) * swell(0.6, 0.9, 1.6) * 0.3
    chord = sum(lp(tri(f, 0.4), 1800) * expenv(0.4, 0.13, 0.01) for f in (D5, E4b * 2, A5)) * 0.25
    return mix(d, (0, click(rng, 0.003, 1500), 0.2), (0, grow), (0, rise), (0.6, lub_dub(70)), (0.6, chord))


# ================================================================ Salas especiales

@sfx("Room_Log_Step", 3, 0.35)
def log_step(rng, v):
    p = pv(rng, v)
    d = 0.15
    return mix(d, (0, cane(rng, 230 * p, 0.12, 3.0)), (0, roots(rng, 0.08, 3, 300, 1200), 0.5),
               (0.03, lp(bubble(140 * p, 0.08, 1.5), 700), 0.3))


@sfx("Room_Log_Bubble", 2, 0.3)
def log_bubble(rng, v):
    p = pv(rng, v)
    d = 0.3
    return mix(d, (0, lp(bubble(110 * p, 0.2, 1.8), 900)), (0.18, lp(noise(0.03, rng), 900) * expenv(0.03, 0.008, 0.0005), 0.3))


@sfx("Room_Coal_Ignite", 1, 0.65)
def coal_ignite(rng, v):
    d = 0.8
    catch = whoosh(rng, 0.4, 200, 1500, 1.0)
    body = roar(rng, 0.75, 500, 0.3, 1.3)
    deep = dist(thump(100, 45, 0.4, 0.15), 1.4)
    rock = crackle(rng, 0.7, 20, 800, 4000, 0.65, gain=(0.2, 0.6))
    return mix(d, (0, click(rng, 0.004, 1000), 0.3), (0, deep, 0.7), (0, catch), (0.05, body), (0.1, rock, 0.6))


@sfx("Room_Coal_Burn_Loop", 1, 0.35, loop=True)
def coal_burn_loop(rng, v):
    d = 3.0
    N = n(d)
    t = np.arange(N) / N
    base = loop_noise(d, rng, 260, 2)
    base = base / np.max(np.abs(base)) * (0.8 + 0.2 * loop_band(d, rng, 3, 8))
    buf = base * 0.5 + loop_sine(D3 * 0.25, d) * 0.08
    for _ in range(30):
        pop = bp(noise(0.012, rng), 700, 3500) * expenv(0.012, 0.003, 0.0003)
        place(buf, pop, rng.uniform(0, d), rng.uniform(0.2, 0.6), True)
    return buf


@sfx("Room_FireFront_Loop", 1, 0.45, loop=True)
def fire_front_loop(rng, v):
    d = 3.0
    N = n(d)
    t = np.arange(N) / N
    base = loop_noise(d, rng, 700, 2)
    base /= np.max(np.abs(base))
    gusts = 0.65 + 0.35 * np.sin(2 * np.pi * 2 * t) * np.sin(2 * np.pi * 3 * t + 0.7)
    buf = base * gusts * 0.5 + loop_band(d, rng, 1500, 5000) * gusts * 0.12
    for _ in range(150):
        g = bp(noise(0.005, rng), 1500, 7000) * expenv(0.005, 0.0015, 0.0003)
        place(buf, g, rng.uniform(0, d), rng.uniform(0.15, 0.7), True)
    for _ in range(25):
        pop = bp(noise(0.015, rng), 400, 1600) * expenv(0.015, 0.004, 0.0003)
        place(buf, pop, rng.uniform(0, d), rng.uniform(0.3, 0.8), True)
    return buf


@sfx("Room_Stump_Light_Loop", 1, 0.25, loop=True)
def stump_light_loop(rng, v):
    d = 4.0
    N = n(d)
    t = np.arange(N) / N
    choir = sum(loop_sine(f, d) * (0.6 + 0.4 * np.sin(2 * np.pi * k * t + k)) for k, f in
                enumerate((D4, A4, D5, F5 * 1.0), start=1))
    choir += sum(loop_sine(f * 1.003, d) for f in (D4, A4)) * 0.5            # batido lento, que vibra
    buf = choir * 0.12 + loop_band(d, rng, 2000, 6000) * 0.04
    for _ in range(12):
        f = rng.choice([D6, A6, _midi(98)])
        dd = rng.uniform(0.15, 0.3)
        place(buf, sine(f, dd) * swell(dd, 0.3, 1.5), rng.uniform(0, d), rng.uniform(0.06, 0.15), True)
    return buf


# ================================================================ Jefe: Meganeura

D2 = _midi(38)
EB3 = _midi(51)   # mi bemol: el color frigio de la tormenta


def giant_wings(rng, d, flap=9.0, f=D2, air=0.6, flap_curve=None):
    """Alas de libélula gigante: zumbido grave (con armónico medio audible) y un soplo en cada aletazo."""
    N = n(d)
    t = np.arange(N) / SR
    rate = flap if flap_curve is None else flap_curve
    ph = 2 * np.pi * np.cumsum(np.broadcast_to(rate, (N,))) / SR
    beat = 0.35 + 0.65 * np.abs(np.sin(ph / 2)) ** 2
    ff = np.broadcast_to(f, (N,))
    tone = np.sin(2 * np.pi * np.cumsum(ff) / SR)
    tone = tone + 0.5 * np.sin(2 * 2 * np.pi * np.cumsum(ff) / SR) + 0.3 * np.sin(3 * 2 * np.pi * np.cumsum(ff) / SR)
    body = dist(tone * 0.6, 2.0) * beat
    whoosh_ = bp(noise(d, rng), 300, 1600) * beat ** 2 * air
    return body * 0.6 + whoosh_


def screech(rng, d, f0, f1, rough=1.0):
    """Chillido de insecto gigante: sierra áspera con temblor rápido, filtrada en medios."""
    N = n(d)
    f = np.asarray(glide(f0, f1, d))[:N]
    jitter = 1 + 0.03 * rough * np.sin(2 * np.pi * 37 * np.arange(N) / SR)
    x = saw(f * jitter, d)[:N] * (0.6 + 0.4 * np.sin(2 * np.pi * 23 * np.arange(N) / SR))
    return dist(bp(x, 600, 4500), 1.6)


def thunder(rng, d, crack_gain=0.6):
    """Trueno: chasquido y un retumbo grave que rueda (golpes irregulares que se alejan)."""
    N = n(d)
    rumble = lp(noise(d, rng), 180) * expenv(d, d / 2.5, 0.01)
    roll = loop_band(d, rng, 0.8, 4.0) if d > 0.5 else np.ones(N)
    rumble = rumble * (0.55 + 0.45 * roll[:N])
    mid = lp(noise(d, rng), 700) * expenv(d, d / 6, 0.003) * 0.5
    crack_ = bp(noise(0.08, rng), 1500, 8000) * expenv(0.08, 0.02, 0.0003)
    return mix(d, (0, rumble), (0, mid), (0, crack_, crack_gain))


def zap(rng, d, f=110.0, bright=1.0):
    """Electricidad: zumbido de red áspero con chispazos."""
    N = n(d)
    t = np.arange(N) / SR
    hum = np.sign(np.sin(2 * np.pi * f * t)) * 0.4 + saw(f * 2, d)[:N] * 0.3
    sparks = np.zeros(N)
    for _ in range(int(d * 40 * bright)):
        place(sparks, hp(noise(0.006, rng), 3000) * expenv(0.006, 0.0015, 0.0002), rng.uniform(0, d), rng.uniform(0.3, 1.0))
    return bp(hum, 100, 3000) + sparks * 0.6 * bright


def bolt(rng, d=0.8, big=True):
    crack_ = hp(noise(0.06, rng), 1500) * expenv(0.06, 0.015, 0.0002) + grains(0.06, rng, 12, 3000, 10000, 0.05, (0.5, 1.0), 0.002)
    sizzle = grains(0.2, rng, 30, 2500, 9000, 0.18, (0.2, 0.8), 0.003)
    boom = dist(thump(110, 35, 0.4, 0.12), 1.6)
    return mix(d, (0, crack_, 1.8), (0.005, sizzle, 1.0), (0.01, boom, 0.7 if big else 0.5),
               (0.05, thunder(rng, d - 0.05, 0.2), 0.5))


@sfx("Boss_Meganeura_Intro", 1, 0.75)
def meganeura_intro(rng, v):
    d = 1.6
    N = n(1.4)
    x = np.linspace(0, 1, N)
    approach = giant_wings(rng, 1.4, 9.0, D2 * (0.95 + 0.1 * x)) * (0.1 + 0.9 * x ** 1.5)
    flaps = [(1.4 + k * 0.1, bp(noise(0.12, rng), 200, 1200) * expenv(0.12, 0.04, 0.003), 0.9) for k in range(2)]
    perch = clack(rng, 700, 0.05) + thump(130, 60, 0.15, 0.04)
    return mix(d, (0, click(rng, 0.003, 800), 0.2), (0, approach), *flaps, (1.55, perch, 0.8))


@sfx("Boss_Meganeura_Wings_Loop", 1, 0.3, loop=True)
def meganeura_wings_loop(rng, v):
    d = 1.0
    N = n(d)
    t = np.arange(N) / N
    f = round(D2 * d) / d
    beat = 0.35 + 0.65 * np.abs(np.sin(np.pi * 9 * t)) ** 2       # 9 aletazos por bucle
    tone = sum(np.sin(2 * np.pi * f * k * t * d) / k for k in (1, 2, 3))
    air = loop_band(d, rng, 300, 1600)
    return dist(tone * 0.6, 2.0) * beat * 0.6 + air * beat ** 2 * 0.6


@sfx("Boss_Meganeura_TakeOff", 1, 0.65)
def meganeura_takeoff(rng, v):
    d = 0.8
    N = n(d)
    x = np.linspace(0, 1, N)
    wings = giant_wings(rng, d, 6 + 8 * x, D2 * (1 + 0.5 * x)) * (1 - 0.6 * x ** 2)
    return mix(d, (0, click(rng, 0.003, 800), 0.2), (0, wings))


@sfx("Boss_Meganeura_Mark", 1, 0.5)
def meganeura_mark(rng, v):
    d = 1.1
    buf = np.zeros(n(d))
    t = 0.0
    while t < d - 0.05:
        k = t / d
        pulse = giant_wings(rng, 0.08, 20, D2 * 2) * expenv(0.08, 0.03, 0.002)
        place(buf, pulse, t, 0.5 + 0.5 * k)
        t += 0.22 - 0.13 * k
    return buf


@sfx("Boss_Meganeura_MarkLock", 1, 0.65)
def meganeura_mark_lock(rng, v):
    d = 0.7
    N = n(d)
    x = np.linspace(0, 1, N)
    tense = giant_wings(rng, d, 20 + 20 * x, D2 * 2 * (1 + 0.5 * x), 0.3) * (0.3 + 0.7 * x ** 1.5)
    hiss = hp(noise(d, rng), 3000) * x ** 3 * 0.3
    return tense + hiss


@sfx("Boss_Meganeura_Dive", 2, 0.7)
def meganeura_dive(rng, v):
    p = pv(rng, v)
    d = 0.4
    tear = svf(noise(d, rng), glide(3000 * p, 600 * p, d), 1.2, "bp") * swell(d, 0.6, 1.3)
    wings = giant_wings(rng, d, 25, glide(D2 * 3 * p, D2 * p, d), 0.3) * swell(d, 0.6, 1.3)
    return mix(d, (0, click(rng, 0.003, 1500), 0.3), (0, tear), (0, wings, 0.6))


@sfx("Boss_Meganeura_DiveImpact", 2, 0.75)
def meganeura_dive_impact(rng, v):
    p = pv(rng, v)
    d = 0.5
    slam = dist(thump(120 * p, 40, 0.35, 0.1), 1.6)
    dust = lp(noise(0.3, rng), 1500) * expenv(0.3, 0.08, 0.001)
    shake = giant_wings(rng, 0.3, 14, D2 * 2, 0.4) * expenv(0.3, 0.1, 0.003)
    return mix(d, (0, click(rng, 0.004, 1200), 0.5), (0, slam), (0, dust, 0.6), (0.12, shake, 0.4))


@sfx("Boss_Meganeura_TrunkStuck", 1, 0.7)
def meganeura_trunk_stuck(rng, v):
    d = 1.5
    thunk = cane(rng, 180, 0.3, 3.0) + dist(thump(130, 50, 0.3, 0.08), 1.4)
    crack_ = grains(0.25, rng, 12, 300, 2000, 0.22, (0.3, 1.0), 0.012)
    N = n(1.25)
    x = np.linspace(0, 1, N)
    flail = giant_wings(rng, 1.25, 14 + 4 * np.sin(2 * np.pi * 3 * x), D2 * 1.5, 0.8) * (0.5 + 0.5 * x)
    free = noise_sweep(0.2, 600, 2000, rng, 1.5) * swell(0.2, 0.3, 1.2)
    return mix(d, (0, thunk), (0, crack_, 0.7), (0.2, flail, 0.6), (1.3, free, 0.6))


@sfx("Boss_Meganeura_Gust", 1, 0.75)
def meganeura_gust(rng, v):
    d = 0.7
    wind = svf(noise(d, rng), glide(400, 1600, d), 0.8, "bp") * swell(d, 0.35, 1.3)
    flaps = [(t0, bp(noise(0.2, rng), 150, 900) * expenv(0.2, 0.06, 0.003) + thump(90, 45, 0.2, 0.06), 0.8)
             for t0 in (0, 0.33)]
    return mix(d, (0, wind), *flaps)


@sfx("Boss_Meganeura_FallBurning", 1, 0.75)
def meganeura_fall_burning(rng, v):
    d = 1.0
    cry = screech(rng, 0.3, 1100, 800) * expenv(0.3, 0.1, 0.003)
    fire = roar(rng, 0.7, 1200, 0.3, 1.3) + crackle(rng, 0.7, 30, span=0.65, gain=(0.2, 0.6)) * 0.6
    crash = dist(thump(110, 35, 0.35, 0.1), 1.6)
    return mix(d, (0, cry, 0.6), (0.05, fire), (0.6, crash))


@sfx("Boss_Meganeura_Hurt", 3, 0.6)
def meganeura_hurt(rng, v):
    p = pv(rng, v)
    d = 0.1
    return mix(d, (0, clack(rng, 650 * p, 0.06)), (0, thump(180 * p, 80, 0.09, 0.02), 0.7),
               (0, chitter(rng, 0.07, 6, 1200, 4000), 0.5))


@sfx("Boss_Meganeura_Cry", 1, 0.7)
def meganeura_cry(rng, v):
    d = 0.8
    s = screech(rng, 0.7, 1300, 900) * swell(0.7, 0.2, 1.2)
    crack_ = screech(rng, 0.15, 900, 600, 3.0) * expenv(0.15, 0.05, 0.002)
    return mix(d, (0, s), (0.55, crack_, 0.6))


@sfx("Boss_Meganeura_EggLay", 1, 0.6)
def meganeura_egg_lay(rng, v):
    d = 0.6
    plop = lp(bubble(180, 0.2, 1.6), 1000) + am(lp(noise(0.3, rng), 800) * expenv(0.3, 0.1, 0.003), 12, 0.6)
    return mix(d, (0, plop), (0.1, bubbles(0.45, rng, 8, 250, 700, (0.05, 0.1), 1.6, (0.2, 0.5)), 0.6))


@sfx("Boss_Meganeura_Egg_Hit", 3, 0.45)
def meganeura_egg_hit(rng, v):
    p = pv(rng, v)
    return mix(0.08, (0, thump(260 * p, 160 * p, 0.07, 0.02)), (0, bp(noise(0.02, rng), 400, 1800) * expenv(0.02, 0.005, 0.0003), 0.4))


@sfx("Boss_Meganeura_Egg_Break", 2, 0.6)
def meganeura_egg_break(rng, v):
    p = pv(rng, v)
    d = 0.35
    return mix(d, (0, thump(220 * p, 90, 0.12, 0.03)), (0, splat(rng, 0.3, 300 * p, 2000 * p, 0.06, 400, 1000, 5)))


@sfx("Boss_Meganeura_Egg_Hatch", 1, 0.55)
def meganeura_egg_hatch(rng, v):
    d = 0.5
    rip = noise_sweep(0.2, 500, 2000, rng, 2.5) * swell(0.2, 0.6, 1.2)
    out = splat(rng, 0.25, 300, 1500, 0.05, 300, 800, 3)
    return mix(d, (0, rip), (0.18, out), (0.3, chitter(rng, 0.15, 6, 1000, 3500), 0.5))


@sfx("Boss_Meganeura_Nymph_Warn", 1, 0.5)
def nymph_warn(rng, v):
    d = 0.4
    buf = np.zeros(n(d))
    for i, t0 in enumerate((0, 0.13, 0.23, 0.3, 0.35)):
        place(buf, clack(rng, 900 + 150 * i, 0.03), t0, 0.5 + 0.12 * i)
    return buf


@sfx("Boss_Meganeura_Nymph_Lash", 2, 0.6)
def nymph_lash(rng, v):
    p = pv(rng, v)
    d = 0.24
    crack_ = noise_sweep(0.12, 800 * p, 3500 * p, rng, 2.0) * expenv(0.12, 0.04, 0.001)
    wet = bp(noise(0.06, rng), 600, 2500) * expenv(0.06, 0.015, 0.0005)
    return mix(d, (0, crack_), (0.06, wet, 0.7), (0.06, clack(rng, 800 * p, 0.03), 0.5))


@sfx("Boss_Meganeura_Nymph_Pull", 1, 0.55)
def nymph_pull(rng, v):
    d = 0.4
    yank = sine(glide(300, 160, 0.3), 0.3) * expenv(0.3, 0.1, 0.002)
    drag = bp(noise(0.35, rng), 300, 1500) * swell(0.35, 0.3, 1.2)
    return mix(d, (0, yank), (0.03, drag, 0.6))


@sfx("Boss_Meganeura_Nymph_Hurt", 3, 0.4)
def nymph_hurt(rng, v):
    p = pv(rng, v)
    return mix(0.08, (0, thump(300 * p, 150, 0.06, 0.015)), (0, bp(noise(0.03, rng), 800, 3000) * expenv(0.03, 0.007, 0.0003), 0.6))


@sfx("Boss_Meganeura_Nymph_Death", 2, 0.5)
def nymph_death(rng, v):
    p = pv(rng, v)
    d = 0.3
    return mix(d, (0, splat(rng, 0.2, 400 * p, 2000 * p, 0.04, 500, 1100, 3)),
               (0.02, sine(glide(2000 * p, 1300 * p, 0.1), 0.1) * expenv(0.1, 0.03, 0.002), 0.3))


@sfx("Boss_Meganeura_Screech", 1, 0.8)
def meganeura_screech(rng, v):
    d = 1.0
    s = screech(rng, 0.9, 1500, 1000, 1.5) * swell(0.9, 0.15, 1.0)
    N = n(0.9)
    waves = lp(noise(0.9, rng), 160) * (0.5 + 0.5 * np.sin(2 * np.pi * 5 * np.arange(N) / SR)) * expenv(0.9, 0.4, 0.01)
    return mix(d, (0, click(rng, 0.004, 1000), 0.3), (0, s), (0, waves, 0.8), (0, dist(waves, 2.0) * 0.3))


@sfx("Boss_Meganeura_Land", 1, 0.65)
def meganeura_land(rng, v):
    d = 0.4
    feet = [(t0, thump(150, 60, 0.12, 0.03), 0.7) for t0 in (0, 0.05, 0.09)]
    fold = bp(noise(0.15, rng), 300, 1500) * expenv(0.15, 0.05, 0.002)
    return mix(d, *feet, (0.15, fold, 0.6), (0.2, clack(rng, 600, 0.05), 0.6))


@sfx("Boss_Meganeura_StormEntry", 1, 0.85)
def meganeura_storm_entry(rng, v):
    d = 2.8                                                   # el trueno de los 2,8 s lo toca el código (Bolt_Strike + Thunder)
    cry = screech(rng, 1.4, 1400, 900, 1.5) * swell(1.4, 0.15, 1.0)
    wind = svf(noise(2.6, rng), glide(300, 900, 2.6), 0.9, "bp") * swell(2.6, 0.85, 1.3)
    ash = grains(2.4, rng, 120, 3000, 9000, 2.3, (0.05, 0.25), 0.002)
    x = np.linspace(0, 1, n(0.4))
    rain = (hp(noise(0.4, rng), 2500) * 0.4 + grains(0.4, rng, 80, 2000, 8000, 0.38, (0.1, 0.4), 0.002)) * np.minimum(1, x * 2)
    return mix(d, (0, click(rng, 0.004, 1000), 0.3), (0, cry, 0.8), (0, wind, 0.6), (0.2, ash, 0.5), (2.4, rain, 0.45))


@sfx("Boss_Meganeura_Thunder", 3, 0.6)
def meganeura_thunder(rng, v):
    return thunder(rng, 1.5 * pv(rng, v, 0.05, 0.0), 0.3)


@sfx("Boss_Meganeura_BigBolt_Warn", 1, 0.65)
def big_bolt_warn(rng, v):
    d = 2.0
    N = n(d)
    x = np.linspace(0, 1, N)
    hum = zap(rng, d, 110, 0.4) * (0.15 + 0.85 * x ** 1.4)
    rise = sine(glide(EB3 * 2, D5 * 2, d), d) * x ** 2 * 0.25        # mi bemol → re: el frigio que se resuelve en el golpe
    lock = zap(rng, 0.5, 220, 1.5) * np.linspace(0.3, 1, n(0.5))
    return mix(d, (0, click(rng, 0.003, 2000), 0.2), (0, hum), (0, rise), (1.5, lock, 0.6))


@sfx("Boss_Meganeura_LooseBolt_Warn", 1, 0.5)
def loose_bolt_warn(rng, v):
    d = 1.0
    N = n(d)
    x = np.linspace(0, 1, N)
    fine = grains(d, rng, 60, 3000, 9000, d * 0.98, (0.1, 0.5), 0.002, accel=-0.5) * (0.3 + 0.7 * x)
    hum = zap(rng, d, 220, 0.3) * x ** 2 * 0.4
    return mix(d, (0, click(rng, 0.003, 3000), 0.2), (0, fine), (0, hum))


@sfx("Boss_Meganeura_Bolt_Strike", 3, 0.8)
def bolt_strike(rng, v):
    return bolt(rng, 0.8 * pv(rng, v, 0.05, 0.0))


@sfx("Boss_Meganeura_Electrocuted", 1, 0.8)
def meganeura_electrocuted(rng, v):
    d = 1.0
    shock = zap(rng, 0.8, 130, 1.5) * (0.7 + 0.3 * np.sin(2 * np.pi * 15 * np.arange(n(0.8)) / SR))
    cry = screech(rng, 0.5, 1600, 1100, 3.0) * expenv(0.5, 0.2, 0.003)
    return mix(d, (0, shock), (0, cry, 0.5), (0.8, dist(thump(110, 40, 0.2, 0.06), 1.5), 0.9))


@sfx("Boss_Meganeura_Puddle_Electric_Loop", 1, 0.3, loop=True)
def puddle_electric_loop(rng, v):
    d = 1.0
    N = n(d)
    t = np.arange(N) / N
    hum = sum(np.sin(2 * np.pi * k * 110 * t * d) / k for k in (1, 2, 3, 5)) * 0.4
    buf = hum * (0.8 + 0.2 * np.sin(2 * np.pi * 6 * t)) + loop_band(d, rng, 2500, 7000) * 0.15
    for _ in range(25):
        place(buf, hp(noise(0.006, rng), 3000) * expenv(0.006, 0.0015, 0.0002), rng.uniform(0, d), rng.uniform(0.3, 0.9), True)
    return buf


@sfx("Boss_Meganeura_Ring_Loop", 1, 0.35, loop=True)
def ring_loop(rng, v):
    d = 1.0
    N = n(d)
    t = np.arange(N) / N
    hum = sum(np.sin(2 * np.pi * k * 220 * t * d) / k for k in (1, 2, 3))
    spin = 0.5 + 0.5 * np.sin(2 * np.pi * 4 * t)                 # 4 vueltas por segundo
    sweep = loop_band(d, rng, 800, 3000) * spin
    return hum * (0.6 + 0.4 * spin) * 0.4 + sweep * 0.4


@sfx("Boss_Meganeura_Sigillaria_Burn", 1, 0.7)
def sigillaria_burn(rng, v):
    d = 0.9
    burst = grains(0.25, rng, 20, 600, 4000, 0.22, (0.3, 1.0), 0.01) + dist(thump(140, 50, 0.25, 0.07), 1.4)
    sparks = crackle(rng, 0.7, 35, span=0.65, gain=(0.2, 0.7))
    fire = roar(rng, 0.7, 900, 0.2, 1.3)
    return mix(d, (0, burst), (0.05, sparks, 0.6), (0.1, fire, 0.8))


@sfx("Boss_Meganeura_Death", 1, 0.85)
def meganeura_death(rng, v):
    d = 2.8
    cry = screech(rng, 0.6, 1500, 700, 2.0) * expenv(0.6, 0.25, 0.003)
    snap = grains(0.15, rng, 14, 500, 4000, 0.12, (0.4, 1.0), 0.008) + clack(rng, 500, 0.08)
    N = n(1.6)
    x = np.linspace(0, 1, N)
    spin = giant_wings(rng, 1.6, 6 + 3 * np.sin(2 * np.pi * 4 * x), D2 * (1.4 - 0.6 * x), 0.5)
    spin = spin * (0.6 + 0.4 * np.abs(np.sin(2 * np.pi * 2 * x)))   # gira: va y viene
    splash = dist(thump(110, 35, 0.4, 0.12), 1.4) + splat(rng, 0.6, 200, 2500, 0.12, 200, 700, 10)
    return mix(d, (0, cry, 0.8), (0.6, snap, 0.8), (0.6, spin, 0.7), (2.2, splash))


@sfx("Boss_Meganeura_StormFade", 1, 0.55)
def meganeura_storm_fade(rng, v):
    d = 3.0
    N = n(d)
    x = np.linspace(0, 1, N)
    rain = (hp(noise(d, rng), 2500) * 0.4 + grains(d, rng, 300, 2000, 8000, d, (0.1, 0.4), 0.002)) * (1 - x) ** 2
    far = thunder(rng, 1.8, 0.0) * 0.4
    drips = np.zeros(N)
    for _ in range(14):
        place(drips, bubble(rng.uniform(900, 1800), 0.05, 2.0), rng.uniform(0.8, d - 0.1), rng.uniform(0.1, 0.3))
    return mix(d, (0, rain), (0.5, far), (0, drips, 0.6))


@sfx("Amb_Floor3_Rain_Loop", 1, 0.35, loop=True)
def amb_rain_loop(rng, v):
    d = 8.0
    N = n(d)
    t = np.arange(N) / N
    gusts = 0.7 + 0.3 * np.sin(2 * np.pi * 2 * t) * np.sin(2 * np.pi * 3 * t + 1)
    hiss = loop_band(d, rng, 2000, 9000) * gusts * 0.35
    body = loop_band(d, rng, 300, 1500) * gusts * 0.2
    buf = hiss + body
    for _ in range(900):                                    # gotas sobre hojas y charcas
        g = bp(noise(0.004, rng), 1500, 7000) * expenv(0.004, 0.0012, 0.0002)
        place(buf, g, rng.uniform(0, d), rng.uniform(0.05, 0.3), True)
    for _ in range(60):
        place(buf, bubble(rng.uniform(700, 1600), 0.04, 2.0), rng.uniform(0, d), rng.uniform(0.05, 0.15), True)
    return buf


# ================================================================ Jefe: Arthropleura

def many_legs(rng, d, rate=70, lo=500, hi=2600, wave=3.0, wrap=False):
    """Cientos de patas: golpecitos de quitina muy seguidos, en olas que recorren el cuerpo."""
    N = n(d)
    buf = np.zeros(N)
    count = int(d * rate)
    for i in range(count):
        t0 = rng.uniform(0, d)
        g = 0.5 + 0.5 * np.sin(2 * np.pi * wave * t0 / d * d + rng.uniform(-0.3, 0.3)) if wave else 1.0
        tap = bp(noise(0.006, rng), lo, hi) * expenv(0.006, 0.0018, 0.0002)
        place(buf, tap, t0, (0.2 + 0.8 * g) * rng.uniform(0.4, 1.0), wrap)
    return buf


def shell_knock(rng, f, d=0.1):
    """Quitina gruesa: madera y piedra, grave y sorda."""
    return lp(clack(rng, f, d) + cane(rng, f * 0.8, d, 5.0) * 0.6, 2500)


def spiracle_hiss(rng, d, rattle=1.0):
    """Siseo enorme por los espiráculos con traqueteo de caparazón."""
    N = n(d)
    hiss = bp(noise(d, rng), 1200, 6000) * swell(d, 0.3, 1.2)
    rat = am(bp(noise(d, rng), 300, 1500), 28, 0.9) * swell(d, 0.4, 1.4) * rattle
    low = lp(noise(d, rng), 200) * swell(d, 0.3, 1.4)
    return hiss * 0.6 + rat * 0.5 + low * 0.5 + dist(low, 2.5) * 0.2


def rumble(rng, d, fc=150):
    x = lp(noise(d, rng), fc)
    return x + dist(x * 2, 2.0) * 0.25


def tree_fall(rng, d, p=1.0):
    creak = cane(rng, 160 * p, 0.4, 2.0) + grains(0.5, rng, 18, 200, 1500, 0.45, (0.3, 1.0), 0.015, accel=-0.4)
    snap = grains(0.12, rng, 14, 400, 3000, 0.1, (0.5, 1.0), 0.01) + dist(thump(160 * p, 60, 0.15, 0.04), 1.4)
    crash = dist(thump(90 * p, 30, 0.5, 0.15), 1.8) + leaves(rng, 0.5, 700, 4000, 30) * 0.8
    return mix(d, (0, creak, 0.7), (0.4, snap), (d - 0.6, crash))


@sfx("Boss_Arthropleura_Intro", 1, 0.8)
def arthro_intro(rng, v):
    d = 1.5
    N = n(1.3)
    x = np.linspace(0, 1, N)
    approach = (rumble(rng, 1.3) * 1.2 + leaves(rng, 1.3, 600, 3500, 50) * 0.8 + many_legs(rng, 1.3, 90)) * (0.15 + 0.85 * x ** 1.3)
    return mix(d, (0, click(rng, 0.004, 600), 0.2), (0, approach), (1.2, spiracle_hiss(rng, 0.3, 0.6), 0.7))


@sfx("Boss_Arthropleura_Legs_Loop", 1, 0.4, loop=True)
def arthro_legs_loop(rng, v):
    d = 1.0
    N = n(d)
    t = np.arange(N) / N
    buf = many_legs(rng, d, 160, 400, 2400, 3.0, wrap=True)
    for i in range(12):                                         # golpes graves de patas, regulares
        place(buf, thump(140, 70, 0.07, 0.02), i / 12 * d, 0.35, True)
    scrape = loop_band(d, rng, 300, 1200) * (0.6 + 0.4 * np.sin(2 * np.pi * 3 * t)) * 0.15
    return buf + scrape


@sfx("Boss_Arthropleura_Rustle_Loop", 1, 0.35, loop=True)
def arthro_rustle_loop(rng, v):
    d = 2.0
    N = n(d)
    t = np.arange(N) / N
    fronds = loop_band(d, rng, 800, 4500) * (0.5 + 0.5 * np.sin(2 * np.pi * 4 * t) ** 2) * 0.4
    low = loop_noise(d, rng, 120, 2)
    buf = fronds + low / np.max(np.abs(low)) * 0.5
    buf += lp(many_legs(rng, d, 80, 300, 1500, 4.0, wrap=True), 1500) * 0.6
    for _ in range(14):
        place(buf, grains(0.08, rng, 4, 500, 2500, 0.07, (0.3, 0.8), 0.01), rng.uniform(0, d), 0.6, True)
    return buf


@sfx("Boss_Arthropleura_Charge_Warn", 1, 0.7)
def arthro_charge_warn(rng, v):
    d = 1.4
    N = n(d)
    x = np.linspace(0, 1, N)
    buf = np.zeros(N)
    t = 0.0
    while t < d - 0.03:                                         # mandíbulas que traquetean cada vez más deprisa
        k = t / d
        place(buf, shell_knock(rng, 500 + 300 * k, 0.05), t, 0.4 + 0.6 * k)
        t += 0.14 - 0.1 * k
    hiss = bp(noise(d, rng), 1500, 6000) * x ** 3 * 0.5
    low = rumble(rng, d) * (0.3 + 0.7 * x) * 0.6
    return buf + hiss + low


@sfx("Boss_Arthropleura_Charge", 2, 0.8)
def arthro_charge(rng, v):
    p = pv(rng, v)
    d = 0.6
    burst = dist(thump(110 * p, 40, 0.4, 0.12), 1.6)
    legs = many_legs(rng, 0.55, 200, 400, 2200, 6.0)
    air = noise_sweep(0.5, 500 * p, 1800 * p, rng, 1.0) * swell(0.5, 0.3, 1.2)
    return mix(d, (0, click(rng, 0.005, 800), 0.4), (0, burst), (0, legs, 0.9), (0, air, 0.6))


@sfx("Boss_Arthropleura_EdgeBreak", 1, 0.8)
def arthro_edge_break(rng, v):
    d = 0.8
    snaps = grains(0.3, rng, 26, 300, 3000, 0.28, (0.4, 1.0), 0.012)
    boom = dist(thump(100, 35, 0.4, 0.12), 1.8)
    debris = leaves(rng, 0.6, 600, 4000, 40) + grains(0.6, rng, 20, 200, 1200, 0.55, (0.2, 0.7), 0.015)
    return mix(d, (0, click(rng, 0.005, 800), 0.4), (0, boom), (0, snaps), (0.08, debris, 0.7))


@sfx("Boss_Arthropleura_Shell_Hit", 3, 0.5)
def arthro_shell_hit(rng, v):
    p = pv(rng, v)
    return mix(0.1, (0, shell_knock(rng, 420 * p, 0.09)))


@sfx("Boss_Arthropleura_Plate_Open", 1, 0.6)
def arthro_plate_open(rng, v):
    d = 0.4
    lift = grains(0.3, rng, 10, 300, 1800, 0.28, (0.4, 1.0), 0.015)
    wet = am(lp(noise(0.3, rng), 900) * swell(0.3, 0.5, 1.3), 8, 0.6)
    pulse = thump(90, 55, 0.15, 0.05)
    return mix(d, (0, lift), (0.08, wet, 0.6), (0.25, pulse, 0.6))


@sfx("Boss_Arthropleura_Plate_Close", 1, 0.55)
def arthro_plate_close(rng, v):
    d = 0.4
    return mix(d, (0, grains(0.2, rng, 6, 300, 1500, 0.18, (0.3, 0.8), 0.015)), (0.2, shell_knock(rng, 350, 0.12)),
               (0.2, thump(150, 60, 0.12, 0.03), 0.6))


@sfx("Boss_Arthropleura_Hurt", 3, 0.6)
def arthro_hurt(rng, v):
    p = pv(rng, v)
    d = 0.1
    flesh = thump(200 * p, 80, 0.09, 0.025) + bp(noise(0.05, rng), 300, 1500) * expenv(0.05, 0.012, 0.0003)
    wet = lp(bubble(220 * p, 0.06, 1.4), 1200)
    return mix(d, (0, flesh), (0.005, wet, 0.5))


@sfx("Boss_Arthropleura_Ignite", 1, 0.7)
def arthro_ignite(rng, v):
    d = 0.6
    flare = whoosh(rng, 0.35, 300, 2000, 1.0)
    fire = roar(rng, 0.55, 900, 0.25, 1.3)
    return mix(d, (0, click(rng, 0.004, 1200), 0.3), (0, flare), (0.03, fire, 0.8), (0.1, spiracle_hiss(rng, 0.45, 0.5), 0.6))


@sfx("Boss_Arthropleura_Fuse_Loop", 1, 0.45, loop=True)
def arthro_fuse_loop(rng, v):
    d = 1.0
    N = n(d)
    t = np.arange(N) / N
    surge = 0.4 + 0.6 * np.abs(np.sin(np.pi * 2.5 * t)) ** 2      # tirones: 0,4 s por pieza
    base = loop_noise(d, rng, 600, 2)
    buf = base / np.max(np.abs(base)) * surge * 0.4
    for _ in range(70):
        g = bp(noise(0.005, rng), 1500, 7000) * expenv(0.005, 0.0015, 0.0003)
        place(buf, g, rng.uniform(0, d), rng.uniform(0.15, 0.7), True)
    for _ in range(10):
        place(buf, clack(rng, rng.uniform(500, 900), 0.03), rng.uniform(0, d), 0.3, True)
    return buf


@sfx("Boss_Arthropleura_Hiss", 2, 0.7)
def arthro_hiss(rng, v):
    return spiracle_hiss(rng, 0.8 * pv(rng, v, 0.05, 0.0))


@sfx("Boss_Arthropleura_WallCollapse", 1, 0.85)
def arthro_wall_collapse(rng, v):
    d = 3.2
    hiss = spiracle_hiss(rng, 0.7)
    push = dist(thump(80, 30, 0.6, 0.2), 1.8) + rumble(rng, 0.6) * 0.6
    buf = mix(d, (0, hiss, 0.7), (0.6, push))
    for i in range(7):                                         # árboles y tierra que caen en cadena (0,8–2,2 s)
        t0 = 0.8 + i * 0.2 + rng.uniform(-0.04, 0.04)
        place(buf, grains(0.15, rng, 10, 300, 2500, 0.13, (0.4, 1.0), 0.012), t0, 0.7)
        place(buf, dist(thump(rng.uniform(70, 110), 30, 0.35, 0.1), 1.6), t0 + 0.05, 0.6)
    N = n(2.4)
    settle = (rumble(rng, 2.4) + leaves(rng, 2.4, 500, 3000, 60) * 0.6) * np.concatenate(
        [np.linspace(0.3, 1, n(1.4)), np.linspace(1, 0, N - n(1.4))])
    place(buf, settle, 0.8, 0.6)
    return buf


@sfx("Boss_Arthropleura_Stomp_Warn", 1, 0.7)
def arthro_stomp_warn(rng, v):
    d = 0.9
    N = n(d)
    x = np.linspace(0, 1, N)
    lift = grains(0.6, rng, 20, 300, 1600, 0.55, (0.3, 0.9), 0.015)
    hum = sine(glide(D2, D2 * 2, d), d) * (0.03 + 0.97 * x ** 2.5)
    return mix(d, (0, lift, 1.0), (0, hum, 0.3), (0, dist(hum, 2.5) * 0.15), (0, rumble(rng, d) * x ** 2.5, 0.5))


@sfx("Boss_Arthropleura_Stomp_Wave", 3, 0.75)
def arthro_stomp_wave(rng, v):
    p = pv(rng, v)
    d = 0.5
    slam = dist(thump(100 * p, 32, 0.4, 0.12), 2.0)
    wave = noise_sweep(0.45, 900, 250, rng, 1.0) * expenv(0.45, 0.15, 0.002)
    return mix(d, (0, click(rng, 0.005, 700), 0.4), (0, slam), (0.02, wave, 0.6))


@sfx("Boss_Arthropleura_TreeFall", 2, 0.75)
def arthro_tree_fall(rng, v):
    return tree_fall(rng, 1.2, pv(rng, v))


@sfx("Boss_Arthropleura_Stunned", 1, 0.75)
def arthro_stunned(rng, v):
    d = 1.0
    crash = shell_knock(rng, 300, 0.15) + cane(rng, 170, 0.3, 2.5) + dist(thump(110, 40, 0.3, 0.1), 1.6)
    flail = many_legs(rng, 0.8, 120, 400, 2200, 7.0) * np.linspace(1, 0.3, n(0.8))
    return mix(d, (0, crash), (0.15, flail, 0.8))


@sfx("Boss_Arthropleura_EatFern", 2, 0.65)
def arthro_eat_fern(rng, v):
    d = 0.6
    buf = np.zeros(n(d))
    for i in range(4):
        t0 = i * 0.14
        place(buf, grains(0.1, rng, 10, 700, 4000, 0.09, (0.3, 1.0), 0.006), t0, 0.8)
        place(buf, shell_knock(rng, 450 * pv(rng, v), 0.05), t0, 0.5)
    return buf + mix(d, (0, leaves(rng, d, 1000, 4500, 20), 0.4))


@sfx("Boss_Arthropleura_Douse", 1, 0.75)
def arthro_douse(rng, v):
    d = 1.0
    plunge = dist(thump(90, 30, 0.4, 0.12), 1.5) + splat(rng, 0.4, 150, 1200, 0.1, 100, 400, 8)
    steam = svf(noise(0.8, rng), glide(6000, 2000, 0.8), 0.8, "hp") * expenv(0.8, 0.3, 0.005)
    bub = bubbles(0.8, rng, 12, 80, 250, (0.08, 0.18), 1.5, (0.3, 0.8))
    return mix(d, (0, plunge), (0.05, steam, 0.8), (0.15, bub, 0.6))


@sfx("Boss_Arthropleura_Ring_Close", 1, 0.75)
def arthro_ring_close(rng, v):
    d = 2.2
    N = n(d)
    x = np.linspace(0, 1, N)
    legs = many_legs(rng, d, 120, 400, 2400, 5.0)
    buf = np.zeros(N)
    t = 0.0
    while t < d - 0.03:                                       # crujidos de segmentos que suben de tono y se juntan
        k = t / d
        place(buf, shell_knock(rng, 300 + 400 * k, 0.05), t, 0.4 + 0.6 * k)
        t += 0.2 - 0.15 * k
    return legs * (0.4 + 0.6 * x) + buf + rumble(rng, d) * (0.3 + 0.7 * x) * 0.6


@sfx("Boss_Arthropleura_Squeeze", 1, 0.75)
def arthro_squeeze(rng, v):
    d = 0.6
    press = grains(0.45, rng, 24, 200, 1400, 0.42, (0.3, 1.0), 0.015, accel=0.5)
    return mix(d, (0, press), (0.4, dist(thump(130, 45, 0.2, 0.06), 1.6)), (0, rumble(rng, 0.5) * swell(0.5, 0.8, 1.5), 0.6))


@sfx("Boss_Arthropleura_Expel", 1, 0.65)
def arthro_expel(rng, v):
    d = 0.4
    boing = sine(glide(150, 380, 0.15), 0.15) * expenv(0.15, 0.06, 0.002)
    return mix(d, (0, shell_knock(rng, 400, 0.08), 0.7), (0, boing, 0.7), (0.03, whoosh(rng, 0.3, 500, 2000, 1.3), 0.7))


@sfx("Boss_Arthropleura_Coil", 1, 0.75)
def arthro_coil(rng, v):
    d = 1.0
    buf = np.zeros(n(d))
    t = 0.0
    i = 0
    while t < 0.85:                                           # segmentos que se pliegan, cada vez más seguidos
        place(buf, shell_knock(rng, 380 + 12 * i, 0.06), t, 0.5 + 0.5 * t / 0.85)
        t += 0.09 - 0.05 * t / 0.85
        i += 1
    return buf + mix(d, (0, spiracle_hiss(rng, 0.5, 0.4), 0.4), (0.85, dist(thump(120, 45, 0.15, 0.04), 1.5), 0.8))


@sfx("Boss_Arthropleura_Roll_Loop", 1, 0.5, loop=True)
def arthro_roll_loop(rng, v):
    d = 1.0
    N = n(d)
    t = np.arange(N) / N
    base = loop_noise(d, rng, 160, 2)
    base = base / np.max(np.abs(base)) * (0.7 + 0.3 * np.sin(2 * np.pi * 4 * t))   # 4 vueltas por segundo
    buf = base * 0.6 + dist(base, 2.5) * 0.15
    for i in range(16):                                       # placas que golpean al rodar
        place(buf, shell_knock(rng, rng.uniform(280, 420), 0.05), i / 16 * d, rng.uniform(0.3, 0.6), True)
    return buf


@sfx("Boss_Arthropleura_Roll_Crash", 2, 0.85)
def arthro_roll_crash(rng, v):
    p = pv(rng, v)
    d = 0.5
    crash = dist(thump(110 * p, 35, 0.45, 0.14), 2.0) + shell_knock(rng, 320 * p, 0.15)
    debris = grains(0.5, rng, 24, 300, 2500, 0.45, (0.2, 0.7), 0.012)
    rattle = am(bp(noise(0.4, rng), 400, 1800), 30, 0.9) * expenv(0.4, 0.15, 0.003)
    return mix(d, (0, click(rng, 0.005, 800), 0.4), (0, crash), (0.03, debris, 0.6), (0.05, rattle, 0.5))


@sfx("Boss_Arthropleura_Death", 1, 0.85)
def arthro_death(rng, v):
    d = 3.2
    buf = mix(d, (0, spiracle_hiss(rng, 0.9), 0.8))
    for i in range(28):                                       # los 28 segmentos caen uno tras otro
        t0 = 0.3 + i * 0.09
        place(buf, shell_knock(rng, 420 - 6 * i + rng.uniform(-20, 20), 0.07), t0, 0.5 + 0.3 * rng.random())
        if i % 4 == 0:
            place(buf, thump(130 - i, 50, 0.12, 0.04), t0, 0.4)
    place(buf, dist(thump(90, 30, 0.5, 0.15), 1.8), 2.85, 0.9)
    smoke = hp(noise(1.2, rng), 2500) * swell(1.2, 0.4, 1.2) * 0.15
    place(buf, smoke, 2.0, 1.0)
    return buf


@sfx("Boss_Arthropleura_Smolder_Loop", 1, 0.2, loop=True)
def arthro_smolder_loop(rng, v):
    d = 4.0
    N = n(d)
    t = np.arange(N) / N
    hiss = loop_band(d, rng, 2500, 7000) * (0.7 + 0.3 * np.sin(2 * np.pi * 2 * t)) * 0.12
    low = loop_noise(d, rng, 250, 2)
    buf = hiss + low / np.max(np.abs(low)) * 0.25
    for _ in range(40):
        g = bp(noise(0.006, rng), 1500, 6000) * expenv(0.006, 0.002, 0.0003)
        place(buf, g, rng.uniform(0, d), rng.uniform(0.15, 0.5), True)
    return buf


# ================================================================ Minijefe: Megarachne

def cable(rng, f, d, bright=1.0):
    """Cable de seda: cuerda grave pulsada (Karplus sencillo con armónicos que caen) y un roce."""
    s = sum(sine(f * k, d) * expenv(d, d / (1.5 * k), 0.001) / k for k in (1, 2, 3, 4))
    return s + hp(noise(d, rng), 3000) * expenv(d, 0.01, 0.0005) * 0.2 * bright


def spider_screech(rng, d, f0, f1):
    """Chillido áspero de la Megarachne: más grave y rasposo que el de la libélula."""
    return screech(rng, d, f0, f1, 2.0) * 0.8 + bp(noise(d, rng), 2000, 6000) * swell(d, 0.3, 1.2) * 0.2


def spider_steps(rng, d, rate=24, lo=900, hi=3500, wrap=False):
    buf = np.zeros(n(d))
    for i in range(int(d * rate)):
        t0 = (i / (d * rate)) * d + rng.uniform(-0.006, 0.006)
        place(buf, clack(rng, rng.uniform(lo, hi) * 0.5, 0.025), t0 % d if wrap else max(0, t0), rng.uniform(0.3, 0.8), wrap)
    return buf


@sfx("Boss_Megarachne_Intro", 1, 0.8)
def megarachne_intro(rng, v):
    d = 2.0
    rustle = grains(0.6, rng, 20, 1200, 5000, 0.55, (0.2, 0.7), 0.004)
    eyes = [(0.5 + i * 0.07, bell(f, 0.25, 0.08, 2.0, 1.0), 0.35)
            for i, f in enumerate((D5, EB3 * 4, D5 * 2, EB3 * 4, D5, EB3 * 8, D5 * 2, EB3 * 4))]
    unreel = svf(noise(0.6, rng), glide(2500, 900, 0.6), 4.0, "bp") * swell(0.6, 0.3, 1.2) * 0.6
    threat = spider_screech(rng, 0.6, 600, 450) * swell(0.6, 0.3, 1.2)
    legs = spider_steps(rng, 0.25, 30)
    return mix(d, (0, click(rng, 0.004, 1500), 0.4), (0, rustle, 3.0), (0, lp(noise(0.6, rng), 900) * swell(0.6, 0.5, 1.2), 0.5), *eyes, (1.0, unreel), (1.0, cable(rng, D3, 0.6), 0.5), (1.4, legs, 0.7), (1.4, threat, 0.8))


@sfx("Boss_Megarachne_CableRun_Loop", 1, 0.4, loop=True)
def megarachne_cable_run_loop(rng, v):
    d = 0.6
    buf = spider_steps(rng, d, 30, 1000, 3000, wrap=True) * 0.8
    for i in range(6):                                        # el cable vibra con cada par de pasos
        place(buf, cable(rng, D3 if i % 2 == 0 else EB3, 0.2, 0.4), i / 6 * d, 0.35, True)
    return buf


@sfx("Boss_Megarachne_Walk_Loop", 1, 0.4, loop=True)
def megarachne_walk_loop(rng, v):
    d = 0.6
    buf = spider_steps(rng, d, 24, 700, 2500, wrap=True)
    for i in range(8):
        place(buf, thump(170, 90, 0.05, 0.015), i / 8 * d, 0.3, True)
    return buf


@sfx("Boss_Megarachne_Climb", 1, 0.55)
def megarachne_climb(rng, v):
    d = 0.4
    reel = svf(noise(0.35, rng), glide(900, 2500, 0.35), 4.0, "bp") * swell(0.35, 0.6, 1.2)
    return mix(d, (0, reel, 0.7), (0, spider_steps(rng, 0.35, 30), 0.6), (0.3, cable(rng, EB3 * 2, 0.1), 0.4))


@sfx("Boss_Megarachne_Drop", 1, 0.55)
def megarachne_drop(rng, v):
    d = 0.48
    unreel = svf(noise(0.35, rng), glide(2500, 800, 0.35), 4.0, "bp") * swell(0.35, 0.3, 1.2)
    return mix(d, (0, unreel, 0.7), (0.36, thump(170, 70, 0.1, 0.03), 0.7), (0.36, spider_steps(rng, 0.1, 50), 0.6))


@sfx("Boss_Megarachne_Jump_Warn", 1, 0.65)
def megarachne_jump_warn(rng, v):
    d = 1.0
    N = n(d)
    x = np.linspace(0, 1, N)
    buf = np.zeros(N)
    t = 0.0
    while t < d - 0.05:                                       # el cable cruje cada vez más tenso (sube de tono)
        k = t / d
        place(buf, cable(rng, D3 * (1 + 0.6 * k), 0.15, 0.6), t, 0.4 + 0.6 * k)
        t += 0.15 - 0.1 * k
    hiss = bp(noise(d, rng), 1500, 6000) * x ** 4 * 0.5
    return buf + hiss


@sfx("Boss_Megarachne_Jump_Land", 2, 0.7)
def megarachne_jump_land(rng, v):
    p = pv(rng, v)
    d = 0.4
    slam = dist(thump(140 * p, 50, 0.25, 0.07), 1.5)
    stab = spider_steps(rng, 0.08, 100, 800, 2500)
    return mix(d, (0, click(rng, 0.004, 1000), 0.4), (0, slam), (0, stab, 0.7), (0.02, lp(noise(0.25, rng), 1500) * expenv(0.25, 0.06, 0.001), 0.4))


@sfx("Boss_Megarachne_SilkVolley", 1, 0.6)
def megarachne_silk_volley(rng, v):
    d = 0.5
    squeeze = lp(noise(0.3, rng), 600) * swell(0.3, 0.9, 1.5) * 0.6
    puffs = [(0.36 + i * 0.018, svf(noise(0.08, rng), glide(1500, 3500, 0.08), 3.0, "bp") * expenv(0.08, 0.025, 0.001), 0.7)
             for i in range(6)]
    return mix(d, (0, squeeze), *puffs)


@sfx("Boss_Megarachne_Silk_Splat", 3, 0.3)
def megarachne_silk_splat(rng, v):
    p = pv(rng, v)
    d = 0.12
    return mix(d, (0, bp(noise(0.05, rng), 500 * p, 2500 * p) * expenv(0.05, 0.012, 0.0003)), (0.01, twang(rng, 450 * p, 0.08, 0.2), 0.3))


@sfx("Boss_Megarachne_Net_Warn", 1, 0.55)
def megarachne_net_warn(rng, v):
    d = 1.2
    N = n(d)
    x = np.linspace(0, 1, N)
    buf = np.zeros(N)
    t = 0.0
    while t < d - 0.05:
        k = t / d
        place(buf, twang(rng, rng.choice([D5, EB3 * 4, A5]) * (1 + 0.3 * k), 0.12, 0.5), t, 0.3 + 0.7 * k)
        t += 0.12 - 0.07 * k
    weave = bp(noise(d, rng), 2000, 6000) * (0.2 + 0.8 * x ** 2) * 0.25
    return buf + weave


@sfx("Boss_Megarachne_Net_Drop", 1, 0.65)
def megarachne_net_drop(rng, v):
    d = 0.5
    whistle = svf(noise(0.4, rng), glide(3000, 900, 0.4), 2.0, "bp") * swell(0.4, 0.7, 1.4)
    land = thump(160, 70, 0.12, 0.03) + grains(0.1, rng, 16, 800, 4000, 0.08, (0.3, 0.9), 0.004)
    return mix(d, (0, whistle, 0.7), (0.4, land))


@sfx("Boss_Megarachne_Net_Trap", 1, 0.55)
def megarachne_net_trap(rng, v):
    d = 0.4
    pull = sum(twang(rng, f, 0.3, 0.3) for f in (D5, EB3 * 4))
    return mix(d, (0, bp(noise(0.08, rng), 800, 3500) * expenv(0.08, 0.02, 0.0005), 0.6), (0, pull, 0.5),
               (0.05, sine(glide(400, 250, 0.25), 0.25) * expenv(0.25, 0.08, 0.002), 0.5))


@sfx("Boss_Megarachne_Sac_Weave", 1, 0.5)
def megarachne_sac_weave(rng, v):
    d = 0.9
    N = n(d)
    t = np.arange(N) / N
    spin = bp(noise(d, rng), 1200, 4500) * (0.5 + 0.5 * np.sin(2 * np.pi * 7 * t)) * swell(d, 0.5, 1.2)
    return mix(d, (0, click(rng, 0.003, 2000), 0.2), (0, spin), (0, lp(noise(d, rng), 600) * swell(d, 0.6, 1.4), 0.4))


@sfx("Boss_Megarachne_Sac_Tremble", 1, 0.55)
def megarachne_sac_tremble(rng, v):
    d = 1.0
    N = n(d)
    x = np.linspace(0, 1, N)
    buf = np.zeros(N)
    t = 0.0
    while t < d - 0.04:
        k = t / d
        place(buf, thump(160 + 80 * k, 100, 0.06, 0.02), t, 0.3 + 0.7 * k)
        place(buf, spider_steps(rng, 0.04, 120, 1500, 4000), t + 0.02, 0.2 + 0.4 * k)
        t += 0.16 - 0.11 * k
    return buf + bp(noise(d, rng), 400, 1500) * x ** 2 * 0.2


@sfx("Boss_Megarachne_Sac_Hit", 3, 0.4)
def megarachne_sac_hit(rng, v):
    p = pv(rng, v)
    return mix(0.08, (0, thump(220 * p, 130, 0.06, 0.02)), (0, bp(noise(0.03, rng), 600, 2500) * expenv(0.03, 0.008, 0.0003), 0.5))


@sfx("Boss_Megarachne_Sac_Break", 1, 0.55)
def megarachne_sac_break(rng, v):
    d = 0.5
    rip = noise_sweep(0.2, 800, 3000, rng, 2.0) * swell(0.2, 0.4, 1.2)
    squeak = sine(glide(2400, 1500, 0.15), 0.15) * expenv(0.15, 0.05, 0.002) * 0.3
    return mix(d, (0, thump(200, 90, 0.1, 0.03), 0.6), (0, rip), (0.2, squeak), (0.2, splat(rng, 0.2, 400, 2000, 0.04, 500, 1100, 2), 0.4))


@sfx("Boss_Megarachne_Sac_Burst", 1, 0.65)
def megarachne_sac_burst(rng, v):
    d = 0.6
    pop = dist(thump(200, 70, 0.15, 0.04), 1.4) + bp(noise(0.08, rng), 500, 3000) * expenv(0.08, 0.02, 0.0003)
    scatter = spider_steps(rng, 0.45, 60, 1800, 5000) * np.linspace(1, 0.4, n(0.45))
    return mix(d, (0, pop), (0.06, scatter, 0.8))


@sfx("Boss_Megarachne_Brood_Loop", 1, 0.25, loop=True)
def megarachne_brood_loop(rng, v):
    d = 0.5
    buf = np.zeros(n(d))
    for _ in range(60):
        g = bp(noise(0.004, rng), 2500, 7000) * expenv(0.004, 0.0012, 0.0002)
        place(buf, g, rng.uniform(0, d), rng.uniform(0.2, 0.8), True)
    return buf


@sfx("Boss_Megarachne_Brood_Bite", 3, 0.4)
def megarachne_brood_bite(rng, v):
    p = pv(rng, v)
    return mix(0.1, (0, clack(rng, 1600 * p, 0.03)), (0.02, clack(rng, 1500 * p, 0.03), 0.7))


@sfx("Boss_Megarachne_Brood_Death", 3, 0.4)
def megarachne_brood_death(rng, v):
    p = pv(rng, v)
    d = 0.2
    return mix(d, (0, bp(noise(0.04, rng), 800, 3500) * expenv(0.04, 0.01, 0.0003)),
               (0.01, sine(glide(2600 * p, 1800 * p, 0.1), 0.1) * expenv(0.1, 0.03, 0.002), 0.3))


@sfx("Boss_Megarachne_Cable_Weave", 1, 0.5)
def megarachne_cable_weave(rng, v):
    d = 3.0
    N = n(d)
    x = np.linspace(0, 1, N)
    draw = svf(noise(d, rng), glide(1200, 3500, d), 5.0, "bp") * (0.4 + 0.6 * x) * 0.35
    buf = draw + spider_steps(rng, d, 10, 700, 2200) * 0.4
    for i in range(6):                                        # tañidos que suben al tensarse
        place(buf, cable(rng, D3 * (1 + i * 0.12), 0.4, 0.5), 0.3 + i * 0.45, 0.4 + 0.1 * i)
    return buf


@sfx("Boss_Megarachne_Cable_Ignite", 1, 0.55)
def megarachne_cable_ignite(rng, v):
    d = 0.5
    return mix(d, (0, click(rng, 0.003, 2500), 0.3), (0, whoosh(rng, 0.3, 600, 3000, 1.3), 0.7),
               (0.02, crackle(rng, 0.45, 30, 2500, 8000, 0.42, gain=(0.2, 0.7)), 0.8))


@sfx("Boss_Megarachne_Cable_Fuse_Loop", 1, 0.45, loop=True)
def megarachne_cable_fuse_loop(rng, v):
    d = 1.0
    N = n(d)
    t = np.arange(N) / N
    buf = loop_band(d, rng, 2500, 7000) * 0.15
    for _ in range(90):
        g = bp(noise(0.004, rng), 2500, 8000) * expenv(0.004, 0.0012, 0.0002)
        place(buf, g, rng.uniform(0, d), rng.uniform(0.2, 0.8), True)
    f = round(EB3 * d) / d
    hum = sum(np.sin(2 * np.pi * f * k * t * d) / k for k in (1, 2, 3)) * (0.6 + 0.4 * np.sin(2 * np.pi * 6 * t))
    return buf + hum * 0.15


@sfx("Boss_Megarachne_Cable_Snap", 1, 0.65)
def megarachne_cable_snap(rng, v):
    d = 0.45
    snap = cable(rng, D3, 0.5, 1.5) * np.concatenate([np.ones(n(0.08)), np.linspace(1, 0, n(0.5) - n(0.08)) ** 3])
    whip = noise_sweep(0.15, 1000, 4000, rng, 2.0) * expenv(0.15, 0.04, 0.001)
    return mix(d, (0, click(rng, 0.004, 2000), 0.5), (0, snap), (0.02, whip, 0.7))


@sfx("Boss_Megarachne_Fall_Stunned", 1, 0.75)
def megarachne_fall_stunned(rng, v):
    d = 1.0
    cry = spider_screech(rng, 0.3, 700, 500) * expenv(0.3, 0.1, 0.003)
    fall = dist(thump(130, 45, 0.3, 0.09), 1.5)
    flail = spider_steps(rng, 0.55, 40, 800, 2500) * np.linspace(1, 0.3, n(0.55))
    return mix(d, (0, cry, 0.7), (0.3, fall), (0.4, flail, 0.7))


@sfx("Boss_Megarachne_Hurt", 3, 0.55)
def megarachne_hurt(rng, v):
    p = pv(rng, v)
    d = 0.1
    return mix(d, (0, clack(rng, 700 * p, 0.05)), (0, spider_screech(rng, 0.08, 800 * p, 650 * p) * expenv(0.08, 0.025, 0.002), 0.4),
               (0, thump(180 * p, 90, 0.07, 0.02), 0.5))


@sfx("Boss_Megarachne_Rage_Screech", 1, 0.85)
def megarachne_rage_screech(rng, v):
    d = 2.5
    s = spider_screech(rng, 2.48, 550, 750) * swell(2.48, 0.4, 1.0)
    buf = mix(d, (0, click(rng, 0.004, 1000), 0.3), (0, s))
    for i in range(8):                                        # todos los cables vibran a la vez
        place(buf, cable(rng, (D3, EB3)[i % 2] * (1 + 0.5 * (i // 2)), 1.2, 0.6), 0.5 + i * 0.06, 0.25)
    place(buf, cane(rng, 140, 0.8, 2.0) + grains(0.8, rng, 14, 200, 1200, 0.75, (0.2, 0.7), 0.015), 1.4, 0.6)
    return buf


@sfx("Boss_Megarachne_Nest_Wither", 1, 0.6)
def megarachne_nest_wither(rng, v):
    d = 1.3
    cracks = grains(1.2, rng, 26, 200, 1400, 1.1, (0.3, 1.0), 0.015, accel=0.3)
    dry = grains(1.3, rng, 60, 1500, 6000, 1.25, (0.1, 0.4), 0.004)
    sigh = noise_sweep(1.3, 1200, 250, rng, 1.0) * swell(1.3, 0.3, 1.2)
    return mix(d, (0, click(rng, 0.004, 900), 0.2), (0, cracks), (0.1, dry, 0.6), (0, sigh, 0.6))


@sfx("Boss_Megarachne_Nest_Burn", 1, 0.75)
def megarachne_nest_burn(rng, v):
    d = 1.5
    up = svf(noise(1.0, rng), glide(300, 2500, 1.0), 1.0, "bp") * swell(1.0, 0.5, 1.3)
    fire = roar(rng, 1.4, 1000, 0.35, 1.4)
    silk = crackle(rng, 1.3, 70, 2500, 9000, 1.25, gain=(0.15, 0.6))
    return mix(d, (0, click(rng, 0.004, 1500), 0.3), (0, up), (0.1, fire), (0.15, silk, 0.7))


@sfx("Boss_Megarachne_Web_Collapse", 1, 0.7)
def megarachne_web_collapse(rng, v):
    d = 1.0
    buf = np.zeros(n(d))
    for i in range(9):
        t0 = i * 0.07 * (1 - i * 0.04)
        place(buf, twang(rng, rng.choice([D5, EB3 * 4, A5, D5 * 2]) * rng.uniform(0.97, 1.03), 0.15, 1.2), t0, 0.5)
    place(buf, thump(140, 60, 0.3, 0.09) + leaves(rng, 0.35, 1000, 5000, 20) * 0.6, 0.6, 0.9)
    return buf


@sfx("Boss_Megarachne_Death", 1, 0.8)
def megarachne_death(rng, v):
    d = 1.35
    cry = spider_screech(rng, 0.6, 750, 350) * expenv(0.6, 0.25, 0.003)
    back = dist(thump(120, 40, 0.35, 0.1), 1.6)
    curl = np.zeros(n(0.8))
    for i in range(8):                                        # las ocho patas se encogen
        place(curl, clack(rng, 650 - 30 * i, 0.04), i * 0.09, 0.6)
    return mix(d, (0, cry, 0.8), (0.5, back), (0.65, curl, 0.8))


@sfx("Boss_Megarachne_Cables_Drop", 1, 0.6)
def megarachne_cables_drop(rng, v):
    d = 1.2
    buf = np.zeros(n(d))
    for i in range(5):
        t0 = i * 0.15 + rng.uniform(-0.03, 0.03)
        f = (D3, EB3)[i % 2] * (1.2 - 0.05 * i)
        sag = sum(sine(glide(f * k, f * k * 0.8, 0.5), 0.5) * expenv(0.5, 0.15 / k, 0.001) / k for k in (1, 2, 3))
        place(buf, sag, t0, 0.4)
        place(buf, thump(150, 70, 0.08, 0.02) + bp(noise(0.06, rng), 400, 2000) * expenv(0.06, 0.015, 0.0005), t0 + 0.35, 0.5)
    return buf


@sfx("Boss_Megarachne_Cocoon_Fall", 1, 0.55)
def megarachne_cocoon_fall(rng, v):
    d = 0.9
    unreel = svf(noise(0.7, rng), glide(2500, 800, 0.7), 4.0, "bp") * swell(0.7, 0.4, 1.2)
    return mix(d, (0, click(rng, 0.003, 2000), 0.3), (0, unreel, 0.6), (0.75, thump(150, 70, 0.15, 0.05), 0.8),
               (0.75, bp(noise(0.1, rng), 400, 1800) * expenv(0.1, 0.03, 0.001), 0.5))


@sfx("Boss_Megarachne_Cocoon_Open", 1, 0.65)
def megarachne_cocoon_open(rng, v):
    d = 0.8
    rip = noise_sweep(0.3, 700, 3000, rng, 2.0) * swell(0.3, 0.5, 1.2)
    shine = sine(glide(A5, D6, 0.4), 0.4) * expenv(0.4, 0.15, 0.003) * 0.35
    return mix(d, (0, rip, 0.8), (0.25, lub_dub(75)), (0.3, shine))


# ================================================================ Jefe secreto: Lepidodendron ancestral

FS4, FS5 = _midi(66), _midi(78)


def wood_groan(rng, d, f0, f1, rough=1.0):
    """Gemido de madera: un arco que frota el tronco (sierra con tirones) por formantes de madera."""
    N = n(d)
    f = np.asarray(glide(f0, f1, d))[:N]
    stick = 1 + 0.02 * rough * np.sign(np.sin(2 * np.pi * 9 * np.arange(N) / SR))      # tirones del arco
    x = saw(f * stick, d)[:N]
    body = bp(x, 80, 900) + bp(x, 250, 500) * 0.8
    fric = bp(noise(d, rng), 300, 1500) * 0.15
    return dist((body + fric) * 0.7, 1.5)


def choir(freqs, d, swell_peak=0.5):
    N = n(d)
    t = np.arange(N) / SR
    out = np.zeros(N)
    for k, f in enumerate(freqs):
        vib = 1 + 0.004 * np.sin(2 * np.pi * (5 + 0.3 * k) * t + k)
        ph = 2 * np.pi * np.cumsum(f * vib) / SR
        out += (np.sin(ph) + 0.3 * np.sin(2 * ph) + 0.15 * np.sin(3 * ph)) / len(freqs)
    return lp(out, 3000) * swell(d, swell_peak, 1.5)


def soft_bell(f, d, tau, ratio=2.0, index=1.0, attack=0.015):
    """Campana sin golpe: el ataque sube en unos milisegundos (para luces y recompensas)."""
    x = bell(f, d, tau, ratio, index)
    return x * np.minimum(1, np.arange(len(x)) / (attack * SR))


def big_wood_hit(rng, f, d=0.1, dull=1.0):
    return lp(cane(rng, f, d, 4.0) + thump(f * 0.7, f * 0.35, d, d / 3), 2500 / dull)


@sfx("Boss_Lepidodendron_Intro", 1, 0.8)
def lepido_intro(rng, v):
    d = 3.0
    groan = wood_groan(rng, 2.4, D2, D2 * 1.12) * swell(2.4, 0.45, 1.3)
    canopy = leaves(rng, 2.5, 600, 4000, 60) * swell(2.5, 0.5, 1.2)
    chord = choir([D3, EB3, A3, D4], 2.2, 0.6) * 0.6
    return mix(d, (0, click(rng, 0.004, 600), 0.2), (0, groan), (0.2, canopy, 0.5), (0.8, chord))


@sfx("Boss_Lepidodendron_Creak_Loop", 1, 0.25, loop=True)
def lepido_creak_loop(rng, v):
    d = 4.0
    N = n(d)
    t = np.arange(N) / N
    breath = loop_noise(d, rng, 120, 2)
    breath = breath / np.max(np.abs(breath)) * (0.6 + 0.4 * np.sin(2 * np.pi * 2 * t))
    buf = breath * 0.4 + loop_sine(D2, d) * 0.05 + loop_band(d, rng, 2000, 6000) * (0.5 + 0.5 * np.sin(2 * np.pi * 3 * t)) * 0.06
    for t0 in (0.6, 2.4):                                     # dos crujidos lentos de tronco por bucle
        place(buf, wood_groan(rng, 0.8, D3 * rng.uniform(0.9, 1.1), D3 * rng.uniform(0.95, 1.2), 2.0) * swell(0.8, 0.5, 1.3), t0, 0.3, True)
    for _ in range(12):
        place(buf, grains(0.08, rng, 3, 300, 1500, 0.07, (0.3, 0.8), 0.012), rng.uniform(0, d), 0.4, True)
    return buf


@sfx("Boss_Lepidodendron_RootWall_Hit", 3, 0.5)
def lepido_wall_hit(rng, v):
    return mix(0.1, (0, big_wood_hit(rng, 300 * pv(rng, v), 0.1)))


@sfx("Boss_Lepidodendron_RootWall_KnotHit", 3, 0.6)
def lepido_knot_hit(rng, v):
    p = pv(rng, v)
    d = 0.12
    return mix(d, (0, cane(rng, 380 * p, 0.12, 2.5)), (0, grains(0.08, rng, 5, 500, 2500, 0.07, (0.4, 1.0), 0.01), 0.6),
               (0.01, lp(bubble(250 * p, 0.06, 1.4), 1200), 0.4))


@sfx("Boss_Lepidodendron_RootWall_Ignite", 1, 0.7)
def lepido_wall_ignite(rng, v):
    d = 0.6
    return mix(d, (0, click(rng, 0.004, 1000), 0.3), (0, whoosh(rng, 0.35, 250, 1800, 1.0)), (0.03, roar(rng, 0.55, 700, 0.3, 1.3), 0.8),
               (0.1, wood_groan(rng, 0.45, D3, D3 * 0.9) * swell(0.45, 0.4, 1.3), 0.4))


@sfx("Boss_Lepidodendron_RootWall_Burn_Loop", 1, 0.4, loop=True)
def lepido_wall_burn_loop(rng, v):
    d = 2.0
    N = n(d)
    t = np.arange(N) / N
    base = loop_noise(d, rng, 350, 2)
    buf = base / np.max(np.abs(base)) * (0.75 + 0.25 * np.sin(2 * np.pi * 3 * t)) * 0.5
    for _ in range(60):
        g = bp(noise(0.006, rng), 1200, 6000) * expenv(0.006, 0.0018, 0.0003)
        place(buf, g, rng.uniform(0, d), rng.uniform(0.15, 0.6), True)
    for _ in range(10):                                       # savia que estalla
        place(buf, lp(bubble(rng.uniform(250, 500), 0.05, 1.8), 1500) + bp(noise(0.015, rng), 500, 2000) * expenv(0.015, 0.004, 0.0003),
              rng.uniform(0, d), 0.5, True)
    return buf


@sfx("Boss_Lepidodendron_RootWall_Retract", 1, 0.75)
def lepido_wall_retract(rng, v):
    d = 1.2
    drag = roots(rng, 1.0, 26, 200, 1200, accel=0.2)
    soil = lp(noise(1.1, rng), 600) * swell(1.1, 0.4, 1.3)
    groan = wood_groan(rng, 1.0, D3, D2) * swell(1.0, 0.3, 1.3)
    return mix(d, (0, click(rng, 0.004, 800), 0.3), (0, drag), (0, soil, 0.6), (0.05, groan, 0.5))


@sfx("Boss_Lepidodendron_RootWall_Crumble", 1, 0.75)
def lepido_wall_crumble(rng, v):
    d = 1.2
    crack = grains(0.4, rng, 18, 300, 2500, 0.38, (0.4, 1.0), 0.012)
    ash = grains(1.0, rng, 120, 1500, 7000, 0.95, (0.05, 0.35), 0.003, accel=0.4)
    sigh = noise_sweep(1.0, 1500, 250, rng, 1.0) * swell(1.0, 0.3, 1.2)
    return mix(d, (0, crack), (0, thump(120, 45, 0.3, 0.1), 0.6), (0.2, ash, 0.8), (0.1, sigh, 0.6))


@sfx("Boss_Lepidodendron_Wave_Spawn", 2, 0.55)
def lepido_wave_spawn(rng, v):
    p = pv(rng, v)
    d = 0.5
    burst = grains(0.15, rng, 10, 300, 1500, 0.13, (0.4, 1.0), 0.012) + thump(170 * p, 70, 0.15, 0.04)
    return mix(d, (0, burst), (0.08, spore_puff(rng, 0.3, 400 * p, 1800 * p), 0.8))


@sfx("Boss_Lepidodendron_SporeArc", 1, 0.6)
def lepido_spore_arc(rng, v):
    d = 0.6
    blow = lp(noise(0.3, rng), 500) * expenv(0.3, 0.1, 0.005) + thump(100, 50, 0.2, 0.06)
    up = [(0.05 + i * 0.04, svf(noise(0.25, rng), glide(800, 3000, 0.25), 3.0, "bp") * expenv(0.25, 0.08, 0.002), 0.4) for i in range(5)]
    return mix(d, (0, blow), *up)


@sfx("Boss_Lepidodendron_SporeRain_Warn", 1, 0.6)
def lepido_spore_rain_warn(rng, v):
    d = 1.2
    N = n(d)
    x = np.linspace(0, 1, N)
    fall = svf(noise(d, rng), glide(4000, 1200, d), 2.0, "bp") * (0.15 + 0.85 * x ** 2)
    patter = grains(d, rng, 50, 2000, 6000, d * 0.98, (0.05, 0.4), 0.003, accel=-0.6) * x
    return mix(d, (0, click(rng, 0.003, 2500), 0.2), (0, fall), (0, patter, 0.6))


@sfx("Boss_Lepidodendron_Spore_Impact", 3, 0.4)
def lepido_spore_impact(rng, v):
    p = pv(rng, v)
    d = 0.25
    return mix(d, (0, thump(200 * p, 90, 0.08, 0.02), 0.6), (0, spore_puff(rng, 0.22, 400 * p, 1800 * p)))


@sfx("Boss_Lepidodendron_Phase_Final", 1, 0.85)
def lepido_phase_final(rng, v):
    d = 1.5
    groan = wood_groan(rng, 1.4, D2 * 1.2, D2 * 0.95) * swell(1.4, 0.35, 1.2)
    canopy = leaves(rng, 1.3, 500, 4000, 80) * swell(1.3, 0.4, 1.0)
    cones = [(0.4 + i * 0.18, cane(rng, rng.uniform(200, 280), 0.2, 3.0), 0.5) for i in range(4)]
    return mix(d, (0, click(rng, 0.004, 600), 0.3), (0, groan), (0.1, canopy, 0.6), *cones, (0, dist(lp(noise(1.2, rng), 120), 2.0) * swell(1.2, 0.3, 1.2), 0.4))


@sfx("Boss_Lepidodendron_Trunk_Hit", 3, 0.5)
def lepido_trunk_hit(rng, v):
    return mix(0.1, (0, big_wood_hit(rng, 200 * pv(rng, v), 0.1, 2.0)))


@sfx("Boss_Lepidodendron_Cone_Warn", 1, 0.6)
def lepido_cone_warn(rng, v):
    d = 1.5
    N = n(d)
    x = np.linspace(0, 1, N)
    buf = np.zeros(N)
    t = 0.0
    while t < d - 0.05:
        k = t / d
        place(buf, cane(rng, 180 + 120 * k, 0.12, 3.0), t, 0.3 + 0.7 * k)
        t += 0.25 - 0.17 * k
    return buf + wood_groan(rng, d, D3, D3 * 1.3) * (0.05 + 0.6 * x ** 2) * 0.5


@sfx("Boss_Lepidodendron_Cone_Descend", 1, 0.7)
def lepido_cone_descend(rng, v):
    d = 1.1
    bend = wood_groan(rng, 0.72, D3 * 1.2, D3 * 0.8) * swell(0.72, 0.6, 1.3)
    whistle = svf(noise(0.38, rng), glide(2500, 600, 0.38), 2.0, "bp") * swell(0.38, 0.8, 1.5)
    return mix(d, (0, click(rng, 0.003, 1000), 0.2), (0, bend, 0.7), (0.7, whistle))


@sfx("Boss_Lepidodendron_Cone_Slam", 2, 0.85)
def lepido_cone_slam(rng, v):
    p = pv(rng, v)
    d = 0.6
    slam = dist(thump(110 * p, 35, 0.45, 0.13), 1.9) + big_wood_hit(rng, 230 * p, 0.2)
    chips = grains(0.4, rng, 22, 500, 3500, 0.38, (0.2, 0.7), 0.008)
    return mix(d, (0, click(rng, 0.005, 800), 0.4), (0, slam), (0.02, chips, 0.7))


@sfx("Boss_Lepidodendron_Cone_Lift", 1, 0.55)
def lepido_cone_lift(rng, v):
    d = 1.0
    pull = wood_groan(rng, 0.9, D3 * 0.8, D3 * 1.2) * swell(0.9, 0.4, 1.2)
    return mix(d, (0, cane(rng, 240, 0.12, 3.0), 0.5), (0.05, pull, 0.7), (0, roots(rng, 0.8, 10, 300, 1400), 0.5))


@sfx("Boss_Lepidodendron_Armor_Hit", 3, 0.5)
def lepido_armor_hit(rng, v):
    p = pv(rng, v)
    return mix(0.1, (0, cane(rng, 450 * p, 0.08, 4.0)), (0, grains(0.07, rng, 6, 600, 3000, 0.06, (0.3, 0.8), 0.008), 0.6))


@sfx("Boss_Lepidodendron_Armor_Break", 1, 0.75)
def lepido_armor_break(rng, v):
    d = 0.7
    buf = np.zeros(n(d))
    for i in range(7):
        place(buf, cane(rng, rng.uniform(300, 550), 0.1, 3.0), i * 0.06, 0.6)
        place(buf, grains(0.08, rng, 6, 500, 3000, 0.07, (0.3, 1.0), 0.01), i * 0.06, 0.5)
    place(buf, leaves(rng, 0.3, 800, 4000, 14), 0.35, 0.5)
    return buf


@sfx("Boss_Lepidodendron_Cone_Hurt", 3, 0.55)
def lepido_cone_hurt(rng, v):
    p = pv(rng, v)
    return mix(0.1, (0, thump(220 * p, 100, 0.09, 0.025)), (0, cane(rng, 350 * p, 0.08, 3.0), 0.6),
               (0.005, lp(bubble(300 * p, 0.05, 1.5), 1500), 0.4))


@sfx("Boss_Lepidodendron_Cone_Break", 1, 0.8)
def lepido_cone_break(rng, v):
    d = 0.9
    burst = dist(thump(140, 50, 0.3, 0.09), 1.6) + grains(0.3, rng, 26, 400, 3000, 0.28, (0.4, 1.0), 0.01)
    scales = grains(0.5, rng, 18, 800, 3500, 0.48, (0.2, 0.6), 0.012, accel=0.5)
    groan = wood_groan(rng, 0.6, D3, D3 * 0.75) * swell(0.6, 0.3, 1.3)
    return mix(d, (0, burst), (0.05, spore_puff(rng, 0.35, 400, 1800), 0.7), (0.1, scales, 0.7), (0.2, groan, 0.5))


@sfx("Boss_Lepidodendron_RootLash_Warn", 1, 0.65)
def lepido_root_lash_warn(rng, v):
    d = 0.8
    N = n(d)
    x = np.linspace(0, 1, N)
    rum = lp(noise(d, rng), 200) * (0.2 + 0.8 * x ** 1.5)
    crack = grains(d, rng, 30, 300, 2000, d * 0.98, (0.1, 0.8), 0.01, accel=-0.6) * (0.3 + 0.7 * x)
    return rum + dist(rum, 2.5) * 0.2 + crack


@sfx("Boss_Lepidodendron_RootLash", 2, 0.8)
def lepido_root_lash(rng, v):
    p = pv(rng, v)
    d = 0.5
    burst = grains(0.12, rng, 12, 300, 1800, 0.1, (0.5, 1.0), 0.012) + dist(thump(130 * p, 45, 0.25, 0.07), 1.6)
    crack_ = noise_sweep(0.15, 600 * p, 3000 * p, rng, 2.0) * expenv(0.15, 0.04, 0.001)
    return mix(d, (0, click(rng, 0.004, 800), 0.4), (0, burst), (0.05, crack_, 0.8), (0.08, lp(noise(0.3, rng), 1200) * expenv(0.3, 0.08, 0.002), 0.4))


@sfx("Boss_Lepidodendron_SpikeRing_Warn", 1, 0.6)
def lepido_spike_ring_warn(rng, v):
    d = 0.78
    N = n(d)
    x = np.linspace(0, 1, N)
    buf = np.zeros(N)
    for i in range(10):                                       # crujido que da la vuelta, cada vez más rápido
        t0 = d * (1 - (1 - i / 10) ** 1.6) * 0.95
        place(buf, cane(rng, 220 + 20 * i, 0.08, 3.0), t0, 0.3 + 0.07 * i)
    return buf + lp(noise(d, rng), 250) * x ** 2 * 0.5


@sfx("Boss_Lepidodendron_SpikeRing", 1, 0.8)
def lepido_spike_ring(rng, v):
    d = 0.35
    buf = np.zeros(n(d))
    for i in range(10):
        place(buf, cane(rng, rng.uniform(350, 600), 0.1, 3.0) + grains(0.05, rng, 3, 400, 2000, 0.04, (0.4, 1.0), 0.01),
              rng.uniform(0, 0.04), 0.4)
    return buf + mix(d, (0, dist(thump(140, 50, 0.3, 0.08), 1.5), 0.7))


@sfx("Boss_Lepidodendron_Corner_Warn", 1, 0.6)
def lepido_corner_warn(rng, v):
    d = 1.0
    N = n(d)
    x = np.linspace(0, 1, N)
    shake = roots(rng, d, 26, 200, 1400, accel=-0.5) * (0.3 + 0.7 * x)
    rum = lp(noise(d, rng), 160) * (0.1 + 0.9 * x ** 2)
    return shake + rum * 0.7 + dist(rum, 2.5) * 0.2


@sfx("Boss_Lepidodendron_Corner_Strike", 1, 0.8)
def lepido_corner_strike(rng, v):
    d = 0.6
    hit = lambda f: dist(thump(f, 40, 0.35, 0.1), 1.7) + grains(0.2, rng, 14, 300, 2000, 0.18, (0.3, 1.0), 0.012)
    return mix(d, (0, click(rng, 0.004, 800), 0.3), (0, hit(120)), (0.08, hit(105), 0.8),
               (0.1, lp(noise(0.4, rng), 1200) * expenv(0.4, 0.1, 0.002), 0.4))


@sfx("Boss_Lepidodendron_Methane_Warn", 1, 0.55)
def lepido_methane_warn(rng, v):
    d = 1.0
    N = n(d)
    x = np.linspace(0, 1, N)
    buf = np.zeros(N)
    t = 0.0
    while t < d - 0.05:
        k = t / d
        place(buf, lp(bubble(rng.uniform(140, 260) * (1 + 0.8 * k), 0.12, 1.6), 1800), t, 0.4 + 0.6 * k)
        t += 0.12 - 0.07 * k
    return buf + bp(noise(d, rng), 1200, 4000) * x ** 2 * 0.25


@sfx("Boss_Lepidodendron_Tree_Shake", 1, 0.85)
def lepido_tree_shake(rng, v):
    d = 1.0
    hit = dist(thump(100, 35, 0.4, 0.12), 1.8) + big_wood_hit(rng, 170, 0.25)
    canopy = leaves(rng, 0.9, 500, 4000, 70) * swell(0.9, 0.25, 1.0)
    groan = wood_groan(rng, 0.8, D2 * 1.3, D2) * swell(0.8, 0.4, 1.3)
    return mix(d, (0, click(rng, 0.005, 800), 0.4), (0, hit), (0.05, canopy, 0.7), (0.1, groan, 0.6))


@sfx("Boss_Lepidodendron_Death_Crack", 1, 0.85)
def lepido_death_crack(rng, v):
    d = 1.6
    N = n(d)
    x = np.linspace(0, 1, N)
    groan = wood_groan(rng, d, D2, D2 * 1.4, 1.5) * (0.2 + 0.8 * x)
    glow = choir([D4, EB3 * 2, A4], 0.8, 0.8) * 0.5
    cracks = np.zeros(N)
    for i in range(5):                                        # 5 grietas en 0,9 s (desde 0,7 s)
        place(cracks, grains(0.15, rng, 14, 300, 3000, 0.13, (0.4, 1.0), 0.01) + cane(rng, 200 - 15 * i, 0.15, 2.5), 0.7 + i * 0.18, 0.6 + 0.1 * i)
    return mix(d, (0, click(rng, 0.004, 600), 0.3), (0, groan, 0.6), (0, glow), (0, cracks))


@sfx("Boss_Lepidodendron_Death_Fall", 1, 0.9)
def lepido_death_fall(rng, v):
    d = 2.4
    N = n(1.77)
    x = np.linspace(0, 1, N)
    tear = wood_groan(rng, 1.77, D2 * 1.5, D2 * 0.8, 2.0) * (0.6 + 0.4 * x)
    rip = grains(1.7, rng, 50, 300, 2500, 1.65, (0.2, 0.8), 0.012, accel=0.5)
    whoosh_ = svf(noise(1.77, rng), glide(2500, 400, 1.77), 1.0, "bp") * x ** 2
    impact = dist(thump(80, 28, 0.6, 0.2), 2.2) + lp(noise(0.6, rng), 1500) * expenv(0.6, 0.12, 0.001)
    settle = (lp(noise(0.6, rng), 400) + leaves(rng, 0.6, 500, 3000, 30) * 0.6) * np.linspace(1, 0, n(0.6)) ** 1.5
    return mix(d, (0, tear, 0.6), (0, rip, 0.7), (0, whoosh_, 0.8), (1.77, impact), (1.8, settle, 0.7))


@sfx("Boss_Lepidodendron_LightColumn", 1, 0.7)
def lepido_light_column(rng, v):
    d = 2.5
    ch = choir([D4, FS4, A4, D5, FS5], 2.4, 0.4)
    bells = [(0.3 + i * 0.25, soft_bell(f, 0.8, 0.25), 0.25) for i, f in enumerate((D5 * 2, A5, FS5 * 2, D6, A5 * 2))]
    motes = glimmer(rng, 2.2, 10)
    return mix(d, (0, click(rng, 0.003, 3000), 0.15), (0, ch), *bells, (0.2, motes, 0.6))


@sfx("Boss_Lepidodendron_Reward_Descend", 1, 0.6)
def lepido_reward_descend(rng, v):
    d = 2.2
    notes = (D6 * 2, A5 * 2, FS5 * 2, D6, A5, FS5, D5)
    bells = [(0.4 + i * 0.25, soft_bell(f, 0.6, 0.2, 2.0, 0.9), 0.3) for i, f in enumerate(notes)]
    return mix(d, (0, click(rng, 0.003, 3000), 0.1), *bells, (2.0, lub_dub(80), 0.7))


@sfx("Boss_Lepidodendron_Heart_Pickup", 1, 0.65)
def lepido_heart_pickup(rng, v):
    d = 0.8
    chord = sum(lp(tri(f, 0.6), 2000) * expenv(0.6, 0.2, 0.005) for f in (D4, FS4, A4, D5)) * 0.3
    return mix(d, (0, lub_dub(80)), (0.15, chord), (0.2, amber_chime(D6, 0.5), 0.3))


@sfx("Boss_Lepidodendron_PurpleSpore_Pickup", 1, 0.65)
def lepido_purple_spore_pickup(rng, v):
    d = 1.0
    chord = sum(lp(tri(f, 0.9), 1800) * expenv(0.9, 0.3, 0.005) * (1 + 0.15 * np.sin(2 * np.pi * 6 * np.arange(n(0.9)) / SR))
                for f in (D4, EB3 * 2, A4, D5)) * 0.3
    return mix(d, (0, spore_puff(rng, 0.2, 800, 3000), 0.4), (0.05, chord), (0.1, glimmer(rng, 0.7, 5), 0.6))


@sfx("Boss_Lepidodendron_Stump_Enter", 1, 0.55)
def lepido_stump_enter(rng, v):
    d = 0.8
    return mix(d, (0, cane(rng, 200, 0.2, 3.0), 0.6), (0, roots(rng, 0.3, 8, 300, 1400), 0.5),
               (0.15, noise_sweep(0.6, 400, 2000, rng, 1.0) * swell(0.6, 0.4, 1.3), 0.5), (0.2, choir([D5, A5], 0.6, 0.5), 0.4))


@sfx("Boss_Lepidodendron_Relic_Pickup", 1, 0.75)
def lepido_relic_pickup(rng, v):
    d = 1.5
    vib = roots(rng, 0.4, 10, 200, 1200)
    ch = choir([D3, A3, D4, FS4, A4, D5], 1.4, 0.3)
    shine = sine(glide(D5, D6, 0.8), 0.8) * swell(0.8, 0.6, 1.5) * 0.25
    return mix(d, (0, click(rng, 0.003, 1500), 0.2), (0, vib, 0.5), (0.05, ch), (0.3, shine), (0.5, glimmer(rng, 0.9, 6), 0.6))


# ================================================================ Ambientes de los tramos y salas del piso 3

def amb_base(rng, d, fc=220, breaths=3, drone=(73.4, 110.0), drone_gain=0.09):
    N = n(d)
    t = np.arange(N) / N
    air = loop_noise(d, rng, fc, 2)
    air = air / np.max(np.abs(air)) * (0.7 + 0.3 * np.sin(2 * np.pi * breaths * t))
    dr = sum(loop_sine(f, d) for f in drone) * (0.75 + 0.25 * np.sin(2 * np.pi * 2 * t + 1.0))
    return air * 0.45 + dr * drone_gain


@sfx("Amb_Floor3_Smoke_Loop", 1, 0.3, loop=True)
def amb_floor3_smoke(rng, v):
    """Bosque humeante: el pantano con humo; crepitar lejano, ramas que crujen, menos vida y un viento cálido."""
    d = 24.0
    buf = amb_base(rng, d, 200, 3, (73.4, 73.9, 110.0))
    N = n(d)
    t = np.arange(N) / N
    buf += loop_band(d, rng, 600, 2500) * (0.5 + 0.5 * np.sin(2 * np.pi * 2 * t - 0.5)) ** 2 * 0.08   # viento cálido
    for _ in range(260):                                       # crepitar lejano
        g = bp(noise(0.005, rng), 1200, 5000) * expenv(0.005, 0.0015, 0.0003)
        place(buf, g, rng.uniform(0, d), rng.uniform(0.02, 0.08), True)
    for _ in range(8):                                         # ramas que crujen
        place(buf, cane(rng, rng.uniform(150, 260), 0.3, 2.5) * 0.4, rng.uniform(0, d), rng.uniform(0.04, 0.08), True)
    for t0 in (4.0, 15.5):                                     # un croar suelto, lejano
        place(buf, croak(rng, 110.0, 0.35, 20), t0, 0.05, True)
    for _ in range(4):
        place(buf, insect_pass(rng, rng.uniform(1.6, 2.6)), rng.uniform(0, d), 0.025, True)
    return buf * 0.8 + circular_conv(buf, impulse_response(2.4, rng, bright=1800)) * 0.4


@sfx("Amb_Floor3_Burnt_Loop", 1, 0.3, loop=True)
def amb_floor3_burnt(rng, v):
    """Tierra quemada: viento seco que sopla a ráfagas, brasas que crepitan cerca, ceniza que cae; sin vida."""
    d = 24.0
    N = n(d)
    t = np.arange(N) / N
    gust = (0.5 + 0.5 * np.sin(2 * np.pi * 3 * t) * np.sin(2 * np.pi * 2 * t + 0.8)) ** 1.5
    buf = amb_base(rng, d, 300, 2, (73.4, 77.8), 0.06)        # re con mi bemol: inquietante
    buf += loop_band(d, rng, 400, 3000) * gust * 0.18
    buf += loop_band(d, rng, 4000, 9000) * gust * 0.03
    for _ in range(420):
        g = bp(noise(0.005, rng), 1500, 7000) * expenv(0.005, 0.0015, 0.0003)
        place(buf, g, rng.uniform(0, d), rng.uniform(0.02, 0.12), True)
    for _ in range(18):                                       # brasas que estallan
        place(buf, bp(noise(0.015, rng), 400, 1600) * expenv(0.015, 0.004, 0.0003), rng.uniform(0, d), rng.uniform(0.05, 0.12), True)
    return buf * 0.85 + circular_conv(buf, impulse_response(1.6, rng, bright=2500)) * 0.3


@sfx("Amb_Floor3_BossRoom_Loop", 1, 0.3, loop=True)
def amb_floor3_boss_room(rng, v):
    """Salas de los jefes del piso 3 antes y después del combate: el claro cerrado, grave y tenso, que respira."""
    d = 20.0
    buf = amb_base(rng, d, 150, 2, (36.7, 73.4, 77.8), 0.1)
    for _ in range(6):
        place(buf, cane(rng, rng.uniform(120, 200), 0.5, 2.0) * 0.4, rng.uniform(0, d), rng.uniform(0.04, 0.07), True)
    for _ in range(10):
        place(buf, leaves(rng, rng.uniform(0.6, 1.2), 600, 3500, 12), rng.uniform(0, d), rng.uniform(0.03, 0.06), True)
    for _ in range(6):
        f = np.exp(rng.uniform(np.log(60), np.log(110)))
        place(buf, lp(bubble(f, rng.uniform(0.25, 0.4), 1.15), 700), rng.uniform(0, d), rng.uniform(0.08, 0.12), True)
    return buf * 0.8 + circular_conv(buf, impulse_response(2.8, rng, bright=1500)) * 0.45


@sfx("Amb_Floor3_Amber_Loop", 1, 0.3, loop=True)
def amb_floor3_amber(rng, v):
    """Sala del ámbar: refugio cálido que el fuego no toca; resina que gotea despacio, brillos de cristal y un
    zumbido dorado en re."""
    d = 20.0
    N = n(d)
    t = np.arange(N) / N
    buf = amb_base(rng, d, 260, 2, (146.8, 220.0), 0.05)
    glow = (loop_sine(293.7, d) + loop_sine(294.4, d) * 0.7 + loop_sine(440.0, d) * 0.3) * (0.6 + 0.4 * np.sin(2 * np.pi * 3 * t))
    buf += glow * 0.025
    for _ in range(16):                                       # resina que gotea, espesa
        f = np.exp(rng.uniform(np.log(400), np.log(800)))
        place(buf, lp(bubble(f, 0.1, 1.3), 2000), rng.uniform(0, d), rng.uniform(0.04, 0.08), True)
    for _ in range(14):                                       # destellos de cristal
        f = rng.choice([D6, A6, _midi(98)])
        place(buf, soft_bell(f, 0.6, 0.2, 2.0, 0.7, 0.01), rng.uniform(0, d), rng.uniform(0.015, 0.035), True)
    for _ in range(3):                                        # un insecto atrapado zumba muy lejos
        place(buf, insect_pass(rng, rng.uniform(1.6, 2.4)), rng.uniform(0, d), 0.015, True)
    return buf * 0.8 + circular_conv(buf, impulse_response(2.6, rng, bright=3000)) * 0.45
