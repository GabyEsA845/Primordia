"""Efectos de sonido y ambientes del piso 2 (océano cámbrico), según PROMPT_piso2.md. Cada generador recibe el rng
y el índice de variante y devuelve la señal mono sin normalizar, como en efectos.py.

Paleta: el océano entero. Las Bocas y mutaciones suenan a agua grande, concha, hueso y quitina; los enemigos, a
caparazón, gelatina translúcida, arena y esponja; los jefes, enormes. Lo que lleva nota, en re."""

import numpy as np

from efectos import A6, D6, F6, bell, sfx
from sintesis import bubbles, midi as midi_hz
from efectos import pv as _pv
from sintesis import (SR, bp, bubble, click, dist, expenv, fm, glide, hp, lp, mix, n, noise, noise_sweep, place,
                      reverb, saw, sine, swell, thump, tri, tt)
from sintesis import padded

for _name in ("bp", "bubble", "bubbles", "click", "dist", "expenv", "fm", "glide", "hp", "lp", "mix", "noise", "noise_sweep",
              "reverb", "saw", "sine", "swell", "thump", "tri", "tt"):
    globals()[_name] = padded(globals()[_name])

pv = _pv


def grains(d, rng, count, lo, hi, span=None, gain=(0.4, 1.0), g_d=0.005, accel=0.0):
    """Clics de grano (arena, crujidos, grietas) repartidos en 'span' segundos; accel > 0 los junta al final."""
    buf = np.zeros(n(d))
    span = span or d * 0.9
    for i in range(count):
        x = i / max(1, count - 1)
        t = span * (x ** (1 + accel)) if accel else rng.uniform(0, span)
        g = bp(noise(g_d, rng), lo, hi) * expenv(g_d, g_d / 3, 0.0003)
        place(buf, g, t, rng.uniform(*gain))
    return buf


def loop_band(d, rng, lo, hi):
    """Ruido en una banda, ya cíclico (para bucles)."""
    N = n(d)
    X = np.fft.rfft(rng.standard_normal(N))
    f = np.fft.rfftfreq(N, 1 / SR)
    X *= ((f >= lo) & (f <= hi))
    x = np.fft.irfft(X, N)
    return x / (np.max(np.abs(x)) + 1e-9)


def clonk(rng, f, d=0.08, shell=0.5):
    """Golpe de placa ósea o caparazón: tono que cae un poco, parcial inarmónico y chasquido."""
    body = sine(glide(f, f * 0.7, d)) * expenv(d, d / 4, 0.0005)
    part = sine(glide(f * 2.64, f * 1.9, d * 0.5)) * expenv(d * 0.5, d / 10, 0.0005) * 0.5
    tick = bp(noise(0.01, rng), 1500, 5000) * expenv(0.01, 0.002, 0.0003) * shell
    return body + part + tick


# ====================================================================== Bocas

@sfx("Mouth_Cnidocytes_Shot", 3, 0.35)
def cnidocytes_shot(rng, v):
    p = pv(rng, v)
    d = 0.08
    snap = bp(noise(0.015, rng), 2500, 7000) * expenv(0.015, 0.004, 0.0003)
    twang = tri(glide(1800 * p, 900 * p, 0.06)) * expenv(0.06, 0.016, 0.0005)
    wet = bubble(1300 * p, 0.05, 1.4, 0.012)
    return mix(d, (0, snap, 0.7), (0, twang), (0.008, wet, 0.4))


@sfx("Mouth_Cnidocytes_Fan", 1, 0.4)
def cnidocytes_fan(rng, v):
    d = 0.2
    parts = []
    for i, p in enumerate((1.0, 1.12, 0.9)):
        parts.append((i * 0.025, tri(glide(1700 * p, 850 * p, 0.07)) * expenv(0.07, 0.02, 0.0005), 0.8))
        parts.append((i * 0.025, bp(noise(0.015, rng), 2500, 7000) * expenv(0.015, 0.004, 0.0003), 0.5))
    body = thump(260, 120, 0.1, 0.03)
    return mix(d, (0, body, 0.4), *parts)


@sfx("Mouth_JawPlates_Bite", 3, 0.6)
def jaw_plates_bite(rng, v):
    p = pv(rng, v, 0.07)
    d = 0.15
    wet = lp(noise(0.08, rng), 700) * expenv(0.08, 0.02)
    return mix(d, (0, clonk(rng, 470 * p, 0.07), 0.9), (0.03, clonk(rng, 390 * p, 0.08), 1.0), (0.03, wet, 0.3))


@sfx("Mouth_JawPlates_BiteHit", 2, 0.65)
def jaw_plates_bite_hit(rng, v):
    d = 0.15
    crunch = grains(0.08, rng, 8, 900, 3200, 0.07)
    crack = bp(noise(0.05, rng), 600, 2500) * expenv(0.05, 0.012)
    thud = thump(150 * pv(rng, v), 55, 0.12, 0.04)
    return mix(d, (0, thud), (0, crunch, 0.8), (0.004, crack, 0.6))


@sfx("Mouth_JawPlates_Guillotine", 1, 0.75)
def jaw_plates_guillotine(rng, v):
    d = 0.3
    first = clonk(rng, 340, 0.08)
    second = clonk(rng, 300, 0.1)
    slam = dist(thump(120, 40, 0.22, 0.08) + lp(noise(0.08, rng), 1200) * expenv(0.08, 0.02), 1.6)
    return mix(d, (0, first, 0.8), (0.09, second), (0.09, slam, 0.9))


@sfx("Status_Shatter", 1, 0.55)
def status_shatter(rng, v):
    d = 0.25
    crack = grains(0.22, rng, 12, 1800, 6000, 0.2, accel=-0.4)
    groan = sine(glide(320, 210, 0.15)) * expenv(0.15, 0.05, 0.002)
    sizzle = hp(noise(0.2, rng), 5000) * expenv(0.2, 0.05) * 0.2
    return mix(d, (0, crack), (0, groan, 0.5), (0.02, sizzle))


@sfx("Mouth_ArmCrown_Cast", 1, 0.5)
def arm_crown_cast(rng, v):
    d = 0.25
    swirl = noise_sweep(d, 250, 1200, rng, 3.0) * swell(d, 0.6)
    tone = sine(glide(200, 400, d)) * swell(d, 0.6) * 0.2
    return swirl + tone


@sfx("Mouth_ArmCrown_Whirl_Loop", 1, 0.35, loop=True)
def arm_crown_whirl(rng, v):
    """1 s cíclico: dos bandas de agua que se turnan dos veces por segundo (el giro) y una succión grave."""
    d = 1.0
    t = np.arange(n(d)) / n(d)
    m = 0.5 + 0.5 * np.sin(2 * np.pi * 2 * t)
    water = loop_band(d, rng, 300, 900) * (1 - m) + loop_band(d, rng, 900, 2600) * m * 0.7
    suck = loop_band(d, rng, 80, 260) * (0.7 + 0.3 * np.sin(2 * np.pi * 4 * t))
    buf = water * 0.6 + suck * 0.5
    for _ in range(6):
        place(buf, lp(bubble(rng.uniform(300, 700), 0.08, 1.4), 1800), rng.uniform(0, d), 0.25, True)
    return buf


@sfx("Mouth_ArmCrown_Tide", 1, 0.6)
def arm_crown_tide(rng, v):
    d = 0.6
    boom = thump(90, 45, 0.3, 0.1)
    crash = lp(noise(0.5, rng), 1500) * expenv(0.5, 0.12, 0.01)
    spread = noise_sweep(0.4, 1500, 400, rng, 1.5) * swell(0.4, 0.15)
    return mix(d, (0, boom), (0, crash, 0.7), (0.05, spread, 0.5))


# ================================================ Extremidades, Piel y Órgano

@sfx("Limbs_Tail_Dodge", 2, 0.5)
def tail_dodge(rng, v):
    p = pv(rng, v, 0.08)
    d = 0.3
    jet = noise_sweep(d, 250 * p, 1500 * p, rng, 2.0) * swell(d, 0.2)
    flick = thump(180 * p, 80, 0.1, 0.03)
    trail = mix(0.3, (0.1, bubble(500 * p, 0.08, 1.5), 0.3), (0.16, bubble(650 * p, 0.07, 1.5), 0.25))
    return mix(d, (0, jet), (0, flick, 0.6), (0, trail))


@sfx("Limbs_Tail_Bubbles", 1, 0.4)
def tail_bubbles(rng, v):
    d = 0.3
    parts = [(i * 0.05 + rng.uniform(0, 0.02), lp(bubble(f, rng.uniform(0.12, 0.18), 1.35), 2000), 0.8)
             for i, f in enumerate((340, 470, 400))]
    blub = lp(noise(0.08, rng), 600) * expenv(0.08, 0.02)
    return mix(d, (0, blub, 0.3), *parts)


@sfx("Limbs_Tail_BubblePop", 2, 0.5)
def tail_bubble_pop(rng, v):
    p = pv(rng, v)
    d = 0.15
    return mix(d, (0, bubble(260 * p, 0.12, 2.0, 0.03)), (0, thump(160 * p, 60, 0.1, 0.03), 0.7),
               (0, click(rng, 0.005, 1500), 0.4))


@sfx("Limbs_Hallucigenia_Place", 2, 0.45)
def hallucigenia_place(rng, v):
    d = 0.15
    def stab():
        return hp(noise(0.02, rng), 3000) * expenv(0.02, 0.005, 0.0003) + grains(0.06, rng, 6, 2000, 6000, 0.05) * 0.5
    return mix(d, (0, stab()), (0.05 + 0.01 * v, stab(), 0.8))


@sfx("Limbs_Hallucigenia_Strike", 2, 0.45)
def hallucigenia_strike(rng, v):
    p = pv(rng, v)
    d = 0.1
    return mix(d, (0, click(rng, 0.004, 3000), 0.8), (0, sine(glide(2400 * p, 1800 * p, 0.03)) * expenv(0.03, 0.008)),
               (0.003, thump(200, 90, 0.06, 0.015), 0.5))


@sfx("Limbs_Hallucigenia_Launch", 1, 0.5)
def hallucigenia_launch(rng, v):
    d = 0.25
    parts = [(0, click(rng, 0.006, 2000), 0.8)]
    for i, f in enumerate((3000, 3400, 2700)):
        w = sine(glide(f, f * 0.6, 0.18)) * expenv(0.18, 0.06, 0.003) * 0.6 + \
            hp(noise(0.18, rng), 4000) * expenv(0.18, 0.04) * 0.3
        parts.append((0.01 + i * 0.02, w, 0.7))
    return mix(d, *parts)


@sfx("Skin_Trilobite_Bounce", 2, 0.5)
def trilobite_bounce(rng, v):
    p = pv(rng, v)
    d = 0.15
    tink = fm(1800 * p, 0.15, 1.41, 1.5, 0.03)
    rise = sine(glide(1500 * p, 2200 * p, 0.05)) * expenv(0.05, 0.015)
    return mix(d, (0, click(rng, 0.004, 3000), 0.5), (0, tink), (0.01, rise, 0.4))


@sfx("Skin_Trilobite_Roll", 1, 0.5)
def trilobite_roll(rng, v):
    d = 0.3
    parts = [(i * 0.035, clonk(rng, 700 + i * 90, 0.04, 0.8), 0.5 + i * 0.08) for i in range(5)]
    parts.append((0.2, clonk(rng, 520, 0.08), 1.0))
    return mix(d, *parts)


@sfx("Skin_Wiwaxia_Volley", 1, 0.5)
def wiwaxia_volley(rng, v):
    d = 0.3
    whoosh = noise_sweep(0.2, 800, 3000, rng, 2.0) * swell(0.2, 0.3)
    parts = [(0, whoosh, 0.5)]
    for i in range(5):
        f = 1600 + i * 180
        scale = bp(noise(0.04, rng), 2000, 6000) * expenv(0.04, 0.01) + fm(f, 0.06, 1.41, 1.0, 0.015) * 0.6
        parts.append((i * 0.03, scale, 0.7))
    return mix(d, *parts)


@sfx("Skin_Wiwaxia_Orbit", 1, 0.4)
def wiwaxia_orbit(rng, v):
    d = 2.0
    t = tt(d)
    spin = 0.5 + 0.5 * np.sin(2 * np.pi * 6 * t)
    tinkle = grains(d, rng, 90, 2500, 7000, d * 0.95) * (0.4 + 0.6 * spin)
    hum = sine(320 * (1 + 0.02 * np.sin(2 * np.pi * 6 * t))) * swell(d, 0.5, 1.0) * 0.25
    release = noise_sweep(0.25, 800, 3000, rng, 2.0) * swell(0.25, 0.3)
    return mix(d, (0, tinkle * swell(d, 0.3, 1.0)), (0, hum), (1.75, release, 0.5))


@sfx("Organ_BroodSac_Open", 1, 0.55)
def brood_sac_open(rng, v):
    d = 0.6
    burst = lp(noise(0.1, rng), 1200) * expenv(0.1, 0.025)
    squelch = sine(glide(300, 150, 0.2)) * expenv(0.2, 0.06) * (0.7 + 0.3 * np.sin(2 * np.pi * 40 * tt(0.2)))
    scurry = grains(0.45, rng, 14, 3000, 6500, 0.4, gain=(0.2, 0.5))
    return mix(d, (0, burst, 0.6), (0, squelch, 0.6), (0.02, bubble(420, 0.1, 1.6), 0.4), (0.12, scurry))


@sfx("Organ_BroodSac_LarvaBite", 3, 0.25)
def larva_bite(rng, v):
    p = pv(rng, v)
    d = 0.08
    nib = lambda: bp(noise(0.006, rng), 1500 * p, 4000 * p) * expenv(0.006, 0.002, 0.0003)
    tone = sine(glide(1200 * p, 900 * p, 0.03)) * expenv(0.03, 0.008)
    return mix(d, (0, nib()), (0.02, nib(), 0.8), (0, tone, 0.4))


@sfx("Organ_BroodSac_Split", 1, 0.4)
def brood_sac_split(rng, v):
    return mix(0.2, (0, bubble(900, 0.06, 1.5), 0.8), (0.07, bubble(1200, 0.06, 1.5), 0.7))


@sfx("Organ_CompoundEyes_On", 1, 0.55)
def compound_eyes_on(rng, v):
    d = 0.6
    slide = tri(glide(900, 200, d)) * swell(d, 0.15, 1.0)
    wash = noise_sweep(d, 3000, 300, rng, 1.5) * swell(d, 0.2)
    sub = sine(glide(110, 55, d)) * swell(d, 0.5)
    return slide * 0.6 + wash * 0.4 + sub * 0.5


@sfx("Organ_CompoundEyes_Off", 1, 0.45)
def compound_eyes_off(rng, v):
    d = 0.4
    slide = tri(glide(220, 900, d)) * swell(d, 0.8, 1.0) * np.clip(np.linspace(4, 0, n(d)), 0, 1)
    wash = noise_sweep(d, 300, 3000, rng, 1.5) * swell(d, 0.8)
    return mix(d, (0, click(rng, 0.004, 2000), 0.2), (0, slide, 0.6), (0, wash, 0.4))


# ================================================= Especiales (almejas grandes)

@sfx("Special_Flagella_Lash", 2, 0.6)
def flagella_lash(rng, v):
    """Silbido que crece y chasquido justo a los 0,4 s, cuando entra el daño."""
    p = pv(rng, v)
    d = 0.5
    whoosh = noise_sweep(0.4, 400 * p, 3000 * p, rng, 3.0) * swell(0.4, 0.97, 2.0)
    crack = hp(noise(0.02, rng), 2500) * expenv(0.02, 0.004, 0.0002) + thump(260, 110, 0.06, 0.015) * 0.5
    return mix(d, (0, click(rng, 0.004, 1500), 0.15), (0, whoosh, 0.6), (0.4, crack))


@sfx("Special_Flagella_Ready", 1, 0.35)
def flagella_ready(rng, v):
    return mix(0.15, (0, bell(A6, 0.12, 0.035, 2.0, 1.0), 0.7), (0.02, bell(D6, 0.1, 0.03, 3.0, 0.8), 0.4))


def iridescent(rng, p=1.0, top=A6):
    sweep = sine(glide(1200 * p, 3200 * p, 0.12)) * expenv(0.12, 0.05, 0.002) * 0.5
    return mix(0.2, (0, sweep), (0.05, bell(D6, 0.15, 0.04, 2.0, 1.2), 0.6), (0.08, bell(top, 0.12, 0.04, 2.0, 1.0), 0.5))


@sfx("Special_Cuticle_Reflect", 2, 0.5)
def cuticle_reflect(rng, v):
    # la segunda variante remata en fa en lugar de la (las dos en re)
    return mix(0.2, (0, click(rng, 0.004, 3000), 0.4), (0, iridescent(rng, pv(rng, v, 0.05), (A6, F6)[v])))


@sfx("Special_Cuticle_Stun", 1, 0.55)
def cuticle_stun(rng, v):
    return mix(0.2, (0, thump(200, 70, 0.12, 0.035), 0.8), (0, click(rng, 0.005, 2000), 0.5), (0, iridescent(rng), 0.7))


@sfx("Special_Bud_Spawn", 1, 0.5)
def bud_spawn(rng, v):
    d = 0.6
    inflate = sine(glide(200, 600, 0.4)) * swell(0.4, 0.85, 1.5)
    wet = lp(noise(0.4, rng), 900) * swell(0.4, 0.8) * 0.3
    echo = bubble(950, 0.08, 1.55, 0.022)  # la burbuja del disparo del jugador, la "voz" de la copia
    return mix(d, (0, inflate, 0.6), (0, wet), (0.38, echo, 0.7), (0.48, echo, 0.35))


@sfx("Special_Bud_Dissolve", 1, 0.45)
def bud_dissolve(rng, v):
    d = 0.5
    parts = [(i * 0.05 + rng.uniform(0, 0.015), bubble(600 * 1.13 ** i, 0.07, 1.5), 0.8 - i * 0.08) for i in range(9)]
    return mix(d, *parts)


@sfx("Special_Unstable_Crack", 1, 0.6)
def unstable_crack(rng, v):
    d = 0.25
    crack = grains(0.12, rng, 9, 2500, 8000, 0.1, accel=-0.3)
    glass = fm(2100, d, 2.76, 2.0, 0.08) * 0.6 + fm(2800, d, 2.76, 1.2, 0.05) * 0.3
    return mix(d, (0, crack), (0.01, glass))


# ================================================================== Linajes

@sfx("Lineage_Carrion_Eat", 2, 0.5)
def carrion_eat(rng, v):
    p = pv(rng, v)
    d = 0.25
    crunch = grains(0.12, rng, 7, 800 * p, 2600 * p, 0.1)
    gulp = sine(glide(260 * p, 140 * p, 0.12)) * expenv(0.12, 0.04)
    grime = dist(lp(noise(0.15, rng), 1200) * expenv(0.15, 0.04), 2.0) * 0.4
    return mix(d, (0, crunch), (0.02, gulp, 0.6), (0, grime))


# ================================================================== Enemigos

@sfx("Enemy_Shell_Hit", 3, 0.45)
def shell_hit(rng, v):
    p = pv(rng, v)
    return mix(0.1, (0, clonk(rng, 300 * p, 0.05, 0.7)), (0, lp(noise(0.04, rng), 900) * expenv(0.04, 0.01), 0.3))


@sfx("Enemy_Jelly_Hit", 3, 0.45)
def jelly_hit(rng, v):
    p = pv(rng, v)
    d = 0.1
    wobble = sine(glide(900 * p, 600 * p, d)) * expenv(d, 0.03, 0.001) * (0.75 + 0.25 * np.sin(2 * np.pi * 35 * tt(d)))
    return mix(d, (0, bubble(700 * p, 0.08, 1.3, 0.02)), (0, wobble, 0.5), (0, hp(noise(0.03, rng), 6000) * expenv(0.03, 0.006), 0.15))


@sfx("Enemy_Sponge_Hit", 2, 0.45)
def sponge_hit(rng, v):
    p = pv(rng, v)
    return mix(0.1, (0, lp(noise(0.08, rng), 500) * expenv(0.08, 0.02, 0.001)),
               (0, lp(thump(140 * p, 80 * p, 0.07, 0.02), 400), 0.8))


@sfx("Enemy_Trilobite_Warn", 1, 0.6)
def trilobite_warn(rng, v):
    """Aviso de 0,6 s: escarba cada vez más rápido y la tensión sube hasta la arrancada."""
    d = 0.6
    scratch = grains(d, rng, 45, 1500, 4500, d * 0.98, accel=-0.5) * np.linspace(0.3, 1, n(d))
    tension = tri(glide(150, 320, d)) * (0.2 + 0.8 * swell(d, 0.97, 1.5)) * 0.35
    return mix(d, (0, click(rng, 0.004, 2000), 0.4), (0, scratch), (0, lp(tension, 1500)))


@sfx("Enemy_Trilobite_Charge", 1, 0.55)
def trilobite_charge(rng, v):
    d = 0.3
    legs = grains(0.18, rng, 12, 2000, 5000, 0.17)
    rush = noise_sweep(d, 300, 1200, rng, 2.0) * swell(d, 0.2)
    return mix(d, (0, legs, 0.7), (0, rush), (0, thump(200, 110, 0.08, 0.02), 0.5))


@sfx("Enemy_Trilobite_Crash", 2, 0.6)
def trilobite_crash(rng, v):
    p = pv(rng, v)
    d = 0.4
    rock = bp(noise(0.06, rng), 1000, 3000) * expenv(0.06, 0.015)
    flail = grains(0.25, rng, 10, 2000, 5000, 0.24, gain=(0.2, 0.5))
    return mix(d, (0, clonk(rng, 220 * p, 0.12)), (0, rock, 0.7), (0, thump(100, 45, 0.15, 0.05), 0.8), (0.12, flail))


@sfx("Enemy_Trilobite_Death", 2, 0.6)
def trilobite_death(rng, v):
    p = pv(rng, v)
    d = 0.4
    crack = grains(0.12, rng, 10, 1200, 5000, 0.1, accel=-0.3)
    parts = [(0, crack), (0.01, clonk(rng, 180 * p, 0.12), 0.8)]
    for i in range(5):
        parts.append((0.08 + i * 0.05, bubble(500 * p * 1.15 ** i, 0.07, 1.5), 0.4))
    return mix(d, *parts)


@sfx("Enemy_Jelly_Ring", 2, 0.5)
def jelly_ring(rng, v):
    p = pv(rng, v)
    d = 0.3
    pulse = lp(thump(220 * p, 120 * p, 0.2, 0.08), 600)
    bloom = noise_sweep(d, 400, 1600, rng, 2.0) * swell(d, 0.3)
    parts = [(0, pulse), (0, bloom, 0.4)]
    for i in range(8):  # el anillo de 8
        parts.append((0.05 + i * 0.018, bubble(rng.uniform(900, 1500) * p, 0.05, 1.4), 0.18))
    return mix(d, *parts)


@sfx("Enemy_Jelly_Death", 2, 0.55)
def jelly_death(rng, v):
    p = pv(rng, v)
    d = 0.4
    squelch = sine(glide(500 * p, 250 * p, 0.15)) * expenv(0.15, 0.05, 0.002)
    fizz_up = bubbles(0.38, rng, 12, 1000 * p, 2500 * p, (0.03, 0.06), gains=(0.3, 0.7))
    return mix(d, (0, squelch, 0.7), (0.02, fizz_up))


@sfx("Enemy_Ottoia_Warn", 1, 0.6)
def ottoia_warn(rng, v):
    """Aviso de 0,8 s: la arena se levanta desde abajo y crece hasta el mordisco."""
    d = 0.8
    sand = grains(d, rng, 60, 1200, 5000, d * 0.98, accel=-0.4) * np.linspace(0.25, 1, n(d))
    rumble = lp(noise(d, rng), 220) * (0.2 + 0.8 * swell(d, 0.97, 1.5))
    rise = sine(glide(80, 170, d)) * (0.2 + 0.8 * swell(d, 0.97, 1.5)) * 0.5
    return sand * 0.6 + rumble + rise


@sfx("Enemy_Ottoia_Bite", 2, 0.65)
def ottoia_bite(rng, v):
    p = pv(rng, v)
    d = 0.25
    burst = bp(noise(0.15, rng), 800, 5000) * expenv(0.15, 0.04, 0.001)
    snap = lambda: bp(noise(0.02, rng), 1000 * p, 4000 * p) * expenv(0.02, 0.005, 0.0003)
    return mix(d, (0, burst, 0.6), (0, thump(180 * p, 70, 0.12, 0.035)), (0.04, snap()), (0.07, snap(), 0.8))


@sfx("Enemy_Ottoia_Sink", 1, 0.45)
def ottoia_sink(rng, v):
    d = 0.4
    swallow = noise_sweep(d, 2000, 300, rng, 1.5) * expenv(d, 0.15, 0.005)
    sand = grains(0.3, rng, 15, 1500, 5000, 0.25, gain=(0.2, 0.6))
    glug = sine(glide(150, 80, 0.2)) * expenv(0.2, 0.06)
    return mix(d, (0, swallow, 0.7), (0, sand, 0.6), (0.1, glug, 0.6))


@sfx("Enemy_Ottoia_Death", 1, 0.55)
def ottoia_death(rng, v):
    d = 0.4
    deflate = lp(tri(glide(300, 90, 0.35)), 1500) * expenv(0.35, 0.15, 0.003) * \
        (0.7 + 0.3 * np.sin(2 * np.pi * 25 * tt(0.35)))
    splash = grains(0.15, rng, 12, 1200, 5000, 0.12)
    return mix(d, (0, deflate), (0, splash, 0.6), (0, thump(160, 70, 0.1, 0.03), 0.5))


@sfx("Enemy_Vauxia_Pulse", 1, 0.5)
def vauxia_pulse(rng, v):
    """Pulso cálido y hueco: una respiración que se expande, con una quinta (re–la) que sube."""
    d = 0.5
    breath = swell(d, 0.35, 1.5)
    chord = (sine(glide(midi_hz(62), midi_hz(62) * 1.02, d)) + sine(midi_hz(69), d) * 0.6) * breath * 0.4
    hollow = bp(noise(d, rng), 300, 600) * breath * 0.5
    air = lp(noise(d, rng), 1200) * breath * 0.2
    return chord + hollow + air


@sfx("Enemy_Shield_Block", 2, 0.45)
def shield_block(rng, v):
    p = pv(rng, v)
    d = 0.12
    knock = thump(300 * p, 200 * p, 0.08, 0.02)
    ping = fm(900 * p, d, 1.0, 1.2, 0.03)
    return lp(mix(d, (0, knock), (0, ping, 0.5)), 3500)


@sfx("Enemy_Vauxia_Death", 1, 0.55)
def vauxia_death(rng, v):
    d = 0.5
    squeeze = lp(noise(0.3, rng), 1400) * expenv(0.3, 0.1, 0.005) * (0.6 + 0.4 * (rng.uniform(0, 1, n(0.3)) > 0.7))
    squelch = sine(glide(400, 150, 0.25)) * expenv(0.25, 0.08)
    drips = bubbles(0.45, rng, 6, 250, 600, (0.06, 0.12), gains=(0.3, 0.6))
    return mix(d, (0, squeeze, 0.6), (0, squelch, 0.6), (0.08, drips))


@sfx("Enemy_Pikaia_Dash", 2, 0.4)
def pikaia_dash(rng, v):
    p = pv(rng, v)
    d = 0.25
    parts = [(i * 0.028 + rng.uniform(0, 0.01), noise_sweep(0.1, 600 * p, 2000 * p, rng, 2.5) * swell(0.1, 0.4),
              0.5 + 0.1 * i) for i in range(5)]
    return mix(d, *parts)


@sfx("Enemy_Pikaia_Death", 2, 0.45)
def pikaia_death(rng, v):
    p = pv(rng, v)
    d = 0.2
    parts = [(0, bubble(1200 * p, 0.06, 1.5), 0.8), (0, click(rng, 0.004, 2500), 0.4)]
    for i in range(3):
        parts.append((0.04 + i * 0.035, bubble(1500 * p * 1.1 ** i, 0.04, 1.5), 0.35))
    return mix(d, *parts)


@sfx("Enemy_PitFall", 1, 0.5)
def enemy_pit_fall(rng, v):
    d = 0.6
    whistle = sine(glide(900, 150, d)) * expenv(d, 0.25, 0.003)
    air = noise_sweep(d, 2000, 300, rng, 2.0) * expenv(d, 0.2, 0.005) * 0.4
    return reverb(whistle + air, rng, 0.8, 0.3)[: n(d)]
