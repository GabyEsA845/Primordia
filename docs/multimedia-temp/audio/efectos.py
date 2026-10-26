"""Efectos de sonido y ambientes de Primordia. Cada generador recibe el rng y el
índice de variante (0, 1, 2) y devuelve la señal mono sin normalizar.

Paleta: jugador en tonos medios y limpios (burbujas, pops); enemigos graves y
viscosos; virus eléctrico y cristalino; ameba de gelatina gigante; veneno y
hongos con siseos y polvo; interfaz con blips de burbuja en re menor."""

import numpy as np

from sintesis import (SR, adsr, am, bp, bubble, bubbles, circular_conv, click, crush, delay,
                      dist, expenv, fade, fit, fm, glide, hp, impulse_response, lp, midi, mix, n,
                      noise, noise_sweep, place, reverb, saw, sine, square, svf, swell, thump, tri,
                      tt, vibrato)
from sintesis import padded

# Las señales de los efectos se suman aunque duren distinto (la más corta se rellena)
for _name in ("adsr", "am", "bp", "bubble", "bubbles", "click", "crush", "delay", "dist", "expenv",
              "fade", "fit", "fm", "glide", "hp", "lp", "mix", "noise", "noise_sweep", "reverb", "saw",
              "sine", "square", "svf", "swell", "thump", "tri", "tt", "vibrato"):
    globals()[_name] = padded(globals()[_name])

# Notas de re menor que usan la interfaz y los jingles
D4, F4, A4, C5, D5, E5, F5, G5, A5, Bb5, C6, D6, E6, F6, A6 = (
    midi(m) for m in (62, 65, 69, 72, 74, 76, 77, 79, 81, 82, 84, 86, 88, 89, 93))
Fs4, Fs5, Fs6 = midi(66), midi(78), midi(90)

SFX = {}


def sfx(name, variants=1, vol=0.6, group="SFX", loop=False):
    def deco(fn):
        SFX[name] = dict(fn=fn, variants=variants, vol=vol, group=group, loop=loop)
        return fn
    return deco


def pv(rng, v, step=0.06, jitter=0.02):
    """Factor de tono de la variante: cada una algo distinta."""
    return 1 + (v - 1) * step + rng.uniform(-jitter, jitter)


def note(f, d, wave="tri", tau=None, duty=0.25):
    tau = tau or d / 2.5
    w = {"tri": tri, "sine": sine, "sq": lambda f, d: square(f, d, duty), "saw": saw}[wave]
    return w(f, d) * expenv(d, tau, 0.002)


def arp(freqs, step, d_note, wave="tri", tau=None, gains=None):
    parts = []
    for i, f in enumerate(freqs):
        g = gains[i] if gains else 1.0
        parts.append((i * step, note(f, d_note, wave, tau), g))
    return mix(0.01, *parts)


def bell(f, d, tau=None, ratio=3.5, index=1.6):
    return fm(f, d, ratio, index, tau or d / 3)


# =================================================================== jugador

@sfx("Player_Shot", 3, 0.35)
def player_shot(rng, v):
    p = pv(rng, v)
    d = 0.08
    b = bubble(950 * p, d, 1.55, 0.022)
    c = square(1900 * p, 0.012, 0.5) * expenv(0.012, 0.004)
    return lp(mix(d, (0, b), (0, c, 0.15)), 6000)


@sfx("Player_Dodge", 2, 0.5)
def player_dodge(rng, v):
    d = 0.25
    w = noise_sweep(d, 350 * pv(rng, v, 0.1), 1900, rng, 2.5) * swell(d, 0.25)
    tone = sine(glide(300, 520, d)) * swell(d, 0.3) * 0.25
    return w + tone


@sfx("Player_DodgeReady", 1, 0.45)
def player_dodge_ready(rng, v):
    return mix(0.1, (0, bell(A5, 0.08, 0.04, 2.0, 1.0), 0.7),
               (0.035, bell(D6, 0.09, 0.05, 2.0, 1.0)))


@sfx("Player_Hurt", 2, 0.9)
def player_hurt(rng, v):
    p = pv(rng, v, 0.08)
    d = 0.3
    body = thump(230 * p, 70, d, 0.09)
    splash = lp(noise(0.12, rng), 2200) * expenv(0.12, 0.035)
    moan = square(vibrato(glide(420 * p, 260 * p, d), d, 18, 0.03, 0), d, 0.3)
    moan = lp(moan, 1800) * expenv(d, 0.1, 0.01) * 0.35
    return dist(mix(d, (0, body), (0, splash, 0.6), (0.01, moan)), 1.8)


@sfx("Player_Death", 1, 0.9)
def player_death(rng, v):
    d = 1.2
    boom = thump(140, 38, 0.6, 0.18)
    burst = lp(noise(0.3, rng), 1800) * expenv(0.3, 0.08)
    parts = [(0, boom), (0, burst, 0.7)]
    t = 0.08
    f = 1300
    while t < 1.05:
        parts.append((t, bubble(f, 0.09, 1.5), 0.5 * (1.1 - t)))
        t += rng.uniform(0.04, 0.11) + t * 0.05
        f *= rng.uniform(0.86, 0.95)
    return reverb(mix(d, *parts), rng, 0.6, 0.25)[: n(d)]


@sfx("Player_Heal", 1, 0.55)
def player_heal(rng, v):
    a = arp([D5, F5, A5, D6], 0.06, 0.2, "tri", 0.09)
    b = bubbles(0.5, rng, 6, 900, 2200, (0.04, 0.08), gains=(0.15, 0.3))
    return reverb(mix(0.5, (0, a), (0.05, b)), rng, 0.5, 0.2)[: n(0.5)]


@sfx("Player_LowHealth_Loop", 1, 0.3, loop=True)
def player_low_health(rng, v):
    d = 1.0
    buf = np.zeros(n(d))
    beat = lp(thump(70, 42, 0.22, 0.07), 400)
    place(buf, beat, 0.0, 1.0, True)
    place(buf, beat, 0.27, 0.7, True)
    return buf


@sfx("Player_AbilityReady", 1, 0.45)
def player_ability_ready(rng, v):
    return mix(0.15, (0, bell(D6, 0.15, 0.05, 2.0, 1.2)), (0, sine(D5, 0.15) * expenv(0.15, 0.04), 0.3))


@sfx("Player_AbilityNotReady", 1, 0.3)
def player_ability_not_ready(rng, v):
    d = 0.1
    return lp(square(glide(180, 140, d), d, 0.5), 700) * expenv(d, 0.03)


# ======================================================= Boca: ataque principal

@sfx("Mouth_Mandibles_Bite", 3, 0.6)
def mandibles_bite(rng, v):
    p = pv(rng, v, 0.07)
    d = 0.15
    snap = lambda f: bp(noise(0.03, rng), 900 * f, 4000 * f) * expenv(0.03, 0.008)
    tone = sine(glide(380 * p, 160 * p, 0.08)) * expenv(0.08, 0.025)
    wet = lp(noise(0.1, rng), 900) * expenv(0.1, 0.03)
    return mix(d, (0, snap(p), 0.9), (0.028, snap(p * 0.8)), (0.03, tone, 0.8), (0.03, wet, 0.4))


@sfx("Mouth_Mandibles_BiteHit", 2, 0.65)
def mandibles_bite_hit(rng, v):
    d = 0.15
    parts = []
    for i in range(6):
        g = bp(noise(0.006, rng), 1200, 3500) * expenv(0.006, 0.002)
        parts.append((i * 0.011 + rng.uniform(0, 0.004), g, 1 - i * 0.12))
    thud = thump(160 * pv(rng, v), 60, 0.12, 0.04)
    return mix(d, (0, thud), *parts)


@sfx("Mouth_Mandibles_Lunge", 1, 0.45)
def mandibles_lunge(rng, v):
    d = 0.12
    return noise_sweep(d, 300, 1300, rng, 2.0) * swell(d, 0.35)


@sfx("Mouth_Stinger_Shot", 3, 0.35)
def stinger_shot(rng, v):
    p = pv(rng, v)
    d = 0.08
    tone = sine(glide(2600 * p, 1700 * p, d)) * expenv(d, 0.02)
    hiss = hp(noise(d, rng), 5000) * expenv(d, 0.015)
    return tone + hiss * 0.4


@sfx("Mouth_Spores_Puff", 2, 0.5)
def spores_puff(rng, v):
    d = 0.3
    p = pv(rng, v, 0.1)
    dust = bp(noise(d, rng), 500 * p, 2200 * p) * expenv(d, 0.08, 0.015)
    squish = sine(glide(190 * p, 110 * p, 0.15)) * expenv(0.15, 0.05, 0.01)
    grit = hp(noise(d, rng), 4000) * expenv(d, 0.05, 0.01) * 0.15
    return dust + squish * 0.5 + grit


@sfx("Mouth_Spore_Pop", 2, 0.3)
def spore_pop(rng, v):
    d = 0.08
    return lp(bubble(620 * pv(rng, v, 0.1), d, 1.3, 0.02), 2500)


# ================================================ Extremidades, Piel y Órgano

@sfx("Limbs_Fins_Dodge", 2, 0.5)
def fins_dodge(rng, v):
    d = 0.35
    stroke = lambda f: noise_sweep(0.17, 300 * f, 1500 * f, rng, 2.2) * swell(0.17, 0.3)
    p = pv(rng, v, 0.08)
    return mix(d, (0, stroke(p)), (0.14, stroke(p * 1.12), 0.85),
               (0, sine(glide(260, 420, d)) * swell(d, 0.5) * 0.15))


@sfx("Limbs_Legs_Push", 2, 0.55)
def legs_push(rng, v):
    d = 0.2
    p = pv(rng, v, 0.08)
    toc = lambda f: (sine(f, 0.03) * expenv(0.03, 0.008) + click(rng) * 0.5)
    return mix(d, (0, toc(900 * p)), (0.05, toc(760 * p)), (0.06, thump(180, 60, 0.14, 0.045), 0.9))


@sfx("Limbs_Legs_Stomp", 1, 0.7)
def legs_stomp(rng, v):
    d = 0.4
    boom = thump(100, 38, d, 0.12)
    rumble = am(lp(noise(d, rng), 500) * expenv(d, 0.12), 22, 0.6)
    return dist(boom + rumble * 0.6, 1.5)


@sfx("Limbs_Fins_Ambush", 1, 0.5)
def fins_ambush(rng, v):
    d = 0.4
    shimmer = am(sine(glide(420, 1700, d)), 28, 0.5) * swell(d, 0.6)
    chord = sum(square(f, d, 0.25) for f in (D5, A5)) * swell(d, 0.7) * 0.12
    return lp(shimmer * 0.6 + chord, 5000)


@sfx("Limbs_Hyphae_Trail", 3, 0.2)
def hyphae_trail(rng, v):
    d = 0.15
    parts = []
    for i in range(rng.integers(3, 6)):
        c = bp(noise(0.004, rng), 2500, 6000) * expenv(0.004, 0.0012)
        parts.append((rng.uniform(0, 0.11), c, rng.uniform(0.4, 1)))
    return mix(d, *parts)


@sfx("Skin_Thorns", 2, 0.45)
def skin_thorns(rng, v):
    d = 0.12
    jitter = lp(rng.standard_normal(n(d)), 300)
    f = 1200 * pv(rng, v, 0.1) * (1 + 0.6 * jitter)
    zap = square(f, d, 0.4) * expenv(d, 0.03)
    return lp(zap, 6000) * 0.6 + hp(noise(d, rng), 4000) * expenv(d, 0.02) * 0.4


@sfx("Skin_Quills", 1, 0.6)
def skin_quills(rng, v):
    d = 0.4
    whoosh = noise_sweep(0.3, 4000, 900, rng, 1.5) * expenv(0.3, 0.1, 0.004)
    parts = [(0, whoosh)]
    for i in range(8):
        parts.append((i * 0.012, sine(glide(3200 - i * 120, 2200, 0.04)) * expenv(0.04, 0.01), 0.3))
    return mix(d, *parts)


@sfx("Skin_Plates_Block", 1, 0.7)
def plates_block(rng, v):
    d = 0.3
    partials = sum(sine(f, d) * expenv(d, tau) * g
                   for f, tau, g in ((250, 0.09, 1), (612, 0.05, 0.5), (1130, 0.03, 0.3)))
    knock = bp(noise(0.04, rng), 300, 1200) * expenv(0.04, 0.01)
    return partials + knock


@sfx("Skin_Plates_Restore", 1, 0.45)
def plates_restore(rng, v):
    d = 0.3
    ping = lambda f: sum(sine(f * r, 0.15) * expenv(0.15, 0.04) * g for r, g in ((1, 1), (2.76, 0.4)))
    return mix(d, (0, ping(1400)), (0.07, ping(1870)), (0, click(rng), 0.6))


@sfx("Skin_Camo_On", 1, 0.45)
def camo_on(rng, v):
    d = 0.5
    chord = sum(tri(glide(f, f * 0.7, d), d) for f in (A5, D6)) * 0.4
    air = lp(noise(d, rng), 2500) * 0.2
    return (chord + air) * expenv(d, 0.18, 0.02)


@sfx("Skin_Camo_Off", 1, 0.45)
def camo_off(rng, v):
    d = 0.25
    chord = sum(tri(glide(f * 0.7, f, d), d) for f in (A5, D6)) * 0.4
    return (chord + lp(noise(d, rng), 3000) * 0.15) * swell(d, 0.4)


@sfx("Skin_Camo_Strike", 1, 0.55)
def camo_strike(rng, v):
    d = 0.2
    shk = hp(noise(0.12, rng), 3000) * expenv(0.12, 0.03, 0.001)
    ping = sine(2100, d) * expenv(d, 0.05) + sine(3150, d) * expenv(d, 0.03) * 0.4
    return shk * 0.7 + ping * 0.5


@sfx("Organ_Ink", 1, 0.7)
def organ_ink(rng, v):
    d = 1.0
    blorp = sine(vibrato(glide(160, 80, 0.4), 0.4, 9, 0.08, 0)) * expenv(0.4, 0.15, 0.01)
    cloud = lp(noise(d, rng), 700) * swell(d, 0.15, 1.2)
    bub = bubbles(d, rng, 7, 250, 700, (0.05, 0.12), gains=(0.2, 0.45))
    return mix(d, (0, blorp), (0, cloud, 0.55), (0.1, bub))


@sfx("Organ_Shockwave", 1, 0.75)
def organ_shockwave(rng, v):
    d = 0.6
    t = tt(d)
    f = glide(420, 55, d)
    body = np.sin(np.cumsum(2 * np.pi * f / SR) + 2.5 * np.exp(-t / 0.15) * np.sin(2 * np.pi * 3 * np.cumsum(f) / SR))
    body *= expenv(d, 0.22, 0.004)
    sparks = np.zeros(n(d))
    for _ in range(10):
        place(sparks, bp(noise(0.008, rng), 2500, 8000) * expenv(0.008, 0.002), rng.uniform(0, 0.3), rng.uniform(0.3, 0.8))
    return body + sparks * 0.5 + lp(noise(d, rng), 900) * expenv(d, 0.1) * 0.4


@sfx("Organ_Shockwave_Paralyze", 1, 0.55)
def shockwave_paralyze(rng, v):
    d = 0.5
    buzz = lp(square(60, d, 0.5), 2500) * am(np.ones(n(d)), 13, 0.7) * expenv(d, 0.2)
    crack = np.zeros(n(d))
    for _ in range(26):
        place(crack, hp(noise(0.005, rng), 2000) * expenv(0.005, 0.0015), rng.uniform(0, 0.45), rng.uniform(0.3, 1))
    return buzz * 0.4 + crack * 0.8


@sfx("Organ_Shockwave_ProjectileBreak", 2, 0.25)
def shockwave_break(rng, v):
    d = 0.1
    return bell(3000 * pv(rng, v, 0.1), d, 0.025, 2.41, 1.5)


@sfx("Organ_Vacuole", 1, 0.6)
def organ_vacuole(rng, v):
    d = 0.7
    parts = []
    for i, f in enumerate((210, 260, 190, 240)):
        parts.append((i * 0.11, lp(bubble(f, 0.13, 1.4, 0.05), 1200), 0.9))
    parts.append((0.48, bubble(700, 0.18, 1.8, 0.06), 0.7))
    return mix(d, *parts)


@sfx("Organ_Vacuole_Charge", 1, 0.25)
def vacuole_charge(rng, v):
    return bubble(1250, 0.1, 1.6, 0.025)


# ============================================== mejoras, linajes y beneficios

@sfx("Mutation_Gain", 1, 0.7)
def mutation_gain(rng, v):
    d = 0.6
    bub = np.zeros(n(d))
    for i in range(14):
        t0 = 0.3 * (i / 14) ** 0.7
        place(bub, bubble(rng.uniform(350, 900) * (1 + i * 0.05), 0.07, 1.6), t0, 0.25 + i * 0.03)
    minor = sum(tri(f, 0.25) for f in (D4, F4, A4)) * adsr(0.25, 0.02, 0.1, 0.8, 0.05)
    major = sum(tri(f, 0.35) for f in (D4, Fs4, A4, D5)) * expenv(0.35, 0.15, 0.01)
    return lp(mix(d, (0, bub), (0.05, minor, 0.25), (0.28, major, 0.3)), 6000)


@sfx("Mutation_LevelUp", 1, 0.6)
def mutation_level_up(rng, v):
    a = arp([D5, F5, A5, D6, F6], 0.055, 0.16, "sq", 0.06)
    sh = hp(noise(0.6, rng), 6000) * swell(0.6, 0.5) * 0.05
    return lp(reverb(mix(0.6, (0, a, 0.5), (0, sh)), rng, 0.5, 0.25)[: n(0.6)], 7000)


@sfx("Mutation_LevelUp_Gold", 1, 0.7)
def mutation_level_up_gold(rng, v):
    d = 1.0
    a = arp([D5, F5, A5, C6, D6, F6, A6], 0.05, 0.18, "sq", 0.07)
    bells = mix(0.6, (0, bell(D6, 0.6, 0.25, 3.5, 1.2)), (0.04, bell(Fs6, 0.55, 0.22, 3.5, 1.2), 0.7),
                (0.08, bell(A6, 0.5, 0.2, 3.5, 1.2), 0.6))
    chord = sum(tri(f, 0.6) for f in (D4, Fs4, A4, D5)) * expenv(0.6, 0.3, 0.01)
    return lp(reverb(mix(d, (0, a, 0.45), (0.35, bells, 0.6), (0.35, chord, 0.3)), rng, 0.8, 0.25)[: n(d)], 8000)


@sfx("Lineage_Complete", 1, 0.75)
def lineage_complete(rng, v):
    d = 0.8
    flash = hp(noise(0.5, rng), 5000) * expenv(0.5, 0.12, 0.005) * 0.25
    stab = lp(sum(saw(f, d) for f in (D4, Fs4, A4, D5)), 2500) * expenv(d, 0.3, 0.004) * 0.3
    b = bell(D6, d, 0.3, 3.5, 1.5)
    return reverb(stab + flash + b * 0.4, rng, 0.9, 0.3)[: n(d)]


@sfx("Lineage_Lost", 1, 0.55)
def lineage_lost(rng, v):
    d = 0.4
    pof = lp(noise(0.15, rng), 1500) * expenv(0.15, 0.04)
    down = tri(glide(520, 140, d), d) * expenv(d, 0.15, 0.01)
    return pof * 0.6 + down * 0.5


@sfx("Lineage_Finisher", 2, 0.55)
def lineage_finisher(rng, v):
    d = 0.15
    return dist(thump(140 * pv(rng, v, 0.08), 48, d, 0.05) + click(rng, 0.008, 1500) * 0.6, 2.0)


@sfx("Lineage_Barrier", 1, 0.5)
def lineage_barrier(rng, v):
    d = 0.4
    leaf = noise_sweep(d, 800, 3000, rng, 3.0) * swell(d, 0.6) * 0.5
    tone = mix(d, (0, note(G5, 0.2, "tri", 0.08)), (0.09, note(D6, 0.3, "tri", 0.12)))
    return leaf + tone * 0.5


@sfx("Lineage_BarrierBlock", 1, 0.6)
def lineage_barrier_block(rng, v):
    d = 0.3
    parts = []
    for i in range(9):
        parts.append((i * 0.014 + rng.uniform(0, 0.006), bp(noise(0.008, rng), 2000, 5000) * expenv(0.008, 0.003), 1 - i * 0.08))
    tone = tri(glide(D6, G5, d), d) * expenv(d, 0.1)
    return mix(d, (0, tone, 0.35), *parts)


def puff(rng, d=0.18, lo=400, hi=1800, tau=0.05):
    return bp(noise(d, rng), lo, hi) * expenv(d, tau, 0.008)


@sfx("Lineage_Colonize", 1, 0.55)
def lineage_colonize(rng, v):
    d = 0.6
    parts = []
    for i, t0 in enumerate((0, 0.11, 0.24, 0.37)):
        f = rng.uniform(0.85, 1.2)
        parts.append((t0, puff(rng, lo=400 * f, hi=1800 * f), 0.8))
        parts.append((t0, sine(glide(260 * f, 150 * f, 0.08)) * expenv(0.08, 0.025), 0.5))
    return mix(d, *parts)


@sfx("Lineage_Retract", 1, 0.55)
def lineage_retract(rng, v):
    d = 0.3
    hollow = sine(glide(200, 170, d)) * expenv(d, 0.08) + sine(glide(470, 400, d)) * expenv(d, 0.05) * 0.5
    return hollow + bp(noise(0.03, rng), 200, 900) * expenv(0.03, 0.01) * 0.6


@sfx("Lineage_StingingTrail", 1, 0.4)
def lineage_stinging_trail(rng, v):
    d = 0.4
    return am(bp(noise(d, rng), 1500, 5000), 42, 0.8) * swell(d, 0.25) * 0.8


@sfx("Lineage_PuffOnHit", 1, 0.6)
def lineage_puff_on_hit(rng, v):
    d = 0.5
    return puff(rng, d, 250, 1500, 0.14) + sine(glide(150, 90, 0.2)) * expenv(0.2, 0.07) * 0.4


# ========================================================= estados de enemigos

def fizz(rng, d, density=600):
    buf = hp(noise(d, rng), 4000) * 0.25
    for _ in range(int(d * density)):
        place(buf, bp(noise(0.003, rng), 3000, 9000) * expenv(0.003, 0.001), rng.uniform(0, d), rng.uniform(0.2, 0.9))
    return buf


@sfx("Status_Poison", 2, 0.4)
def status_poison(rng, v):
    d = 0.3
    return fizz(rng, d) * expenv(d, 0.12, 0.01) + bubble(800 * pv(rng, v, 0.1), 0.08, 1.5) * 0.3


@sfx("Status_PoisonBurst", 1, 0.6)
def status_poison_burst(rng, v):
    d = 0.4
    pop = lp(noise(0.06, rng), 2500) * expenv(0.06, 0.015) + thump(200, 80, 0.1, 0.03)
    bub = bubbles(d, rng, 8, 500, 1500, (0.04, 0.09), gains=(0.2, 0.5))
    return mix(d, (0, pop), (0.02, fizz(rng, 0.35) * expenv(0.35, 0.12), 0.8), (0.03, bub))


@sfx("Status_Bleed", 1, 0.45)
def status_bleed(rng, v):
    d = 0.25
    cut = noise_sweep(0.1, 3500, 1200, rng, 3) * expenv(0.1, 0.035, 0.001)
    sq = lp(bubble(240, 0.15, 0.7, 0.05), 900)
    return mix(d, (0, cut), (0.04, sq, 0.6))


@sfx("Status_Blind", 1, 0.4)
def status_blind(rng, v):
    d = 0.3
    parts = [(i * 0.06 + rng.uniform(0, 0.02), lp(bubble(rng.uniform(180, 420), 0.07, rng.uniform(0.6, 1.6)), 1500), 0.8)
             for i in range(5)]
    return mix(d, *parts)


@sfx("Status_Stun", 1, 0.45)
def status_stun(rng, v):
    d = 0.3
    tweet = lambda f: square(vibrato(glide(f, f * 1.25, 0.06), 0.06, 40, 0.04, 0), 0.06, 0.25) * expenv(0.06, 0.025)
    return lp(mix(d, (0, tweet(2000)), (0.08, tweet(2600)), (0.16, tweet(2200)), (0.23, tweet(2700), 0.7)), 7000)


@sfx("Status_SporeCloud", 1, 0.5)
def status_spore_cloud(rng, v):
    d = 0.5
    open_ = noise_sweep(d, 300, 1800, rng, 1.5) * swell(d, 0.3)
    return open_ + fizz(rng, d, 200) * swell(d, 0.5) * 0.4


@sfx("Status_Suspicion", 1, 0.5)
def status_suspicion(rng, v):
    d = 0.3
    a = square(D5, 0.1, 0.25) * expenv(0.1, 0.05)
    b = square(glide(F5, A5, 0.17, "fast"), 0.17, 0.25) * expenv(0.17, 0.08)
    return lp(mix(d, (0, a), (0.12, b)), 4500)


@sfx("Status_Discovered", 1, 0.6)
def status_discovered(rng, v):
    d = 0.25
    hit = lambda: square(vibrato(A6, 0.09, 30, 0.01, 0), 0.09, 0.5) * expenv(0.09, 0.05)
    return lp(mix(d, (0, hit()), (0.11, hit())), 6000)


@sfx("Status_Panic", 1, 0.45)
def status_panic(rng, v):
    d = 0.5
    moan = tri(vibrato(glide(620, 380, d), d, 11, 0.04, 0), d) * swell(d, 0.2)
    return lp(moan, 3000)


# ============================================================ enemigos piso 1

@sfx("Enemy_Hit", 3, 0.45)
def enemy_hit(rng, v):
    d = 0.1
    p = pv(rng, v, 0.08)
    return sine(glide(320 * p, 170 * p, d)) * expenv(d, 0.03) + lp(noise(0.03, rng), 1400) * expenv(0.03, 0.008) * 0.6


@sfx("Enemy_Death", 3, 0.6)
def enemy_death(rng, v):
    d = 0.35
    p = pv(rng, v, 0.08)
    burst = lp(noise(0.12, rng), 1600) * expenv(0.12, 0.03)
    body = sine(glide(210 * p, 70 * p, 0.2)) * expenv(0.2, 0.06)
    bub = mix(0.2, *((0.03 + i * 0.05, bubble(rng.uniform(500, 1000), 0.07, 1.7), 0.35) for i in range(3)))
    return mix(d, (0, burst, 0.8), (0, body), (0.05, bub))


@sfx("Enemy_Bacteria_Aggro", 2, 0.4)
def bacteria_aggro(rng, v):
    d = 0.3
    p = pv(rng, v, 0.1)
    f = vibrato(glide(450 * p, 750 * p, d), d, 24, 0.06, 0)
    return bp(saw(f, d), 500, 2500) * swell(d, 0.3) * 1.5


@sfx("Enemy_Amoeba_Windup", 1, 0.55)
def amoeba_windup(rng, v):
    d = 0.4
    g = am(sine(glide(150, 520, d)), 18, 0.6) * (np.linspace(0.3, 1, n(d)) ** 1.5)
    return lp(g + am(lp(noise(d, rng), 800), 18, 0.7) * 0.3, 2500)


@sfx("Enemy_Amoeba_Shot", 2, 0.5)
def amoeba_shot(rng, v):
    d = 0.15
    p = pv(rng, v)
    return sine(glide(280 * p, 115 * p, d)) * expenv(d, 0.05) + lp(noise(0.04, rng), 1200) * expenv(0.04, 0.01) * 0.5


@sfx("Enemy_Amoeba_Split", 1, 0.55)
def amoeba_split(rng, v):
    d = 0.5
    stretch = sine(vibrato(glide(190, 360, 0.3), 0.3, 12, 0.05, 0)) * swell(0.3, 0.8)
    plop = lambda f: lp(bubble(f, 0.09, 1.5, 0.03), 1500)
    return mix(d, (0, stretch, 0.6), (0.3, plop(380)), (0.38, plop(480)))


@sfx("Enemy_SmallAmoeba_Death", 2, 0.5)
def small_amoeba_death(rng, v):
    d = 0.2
    p = pv(rng, v)
    burst = lp(noise(0.06, rng), 2500) * expenv(0.06, 0.015)
    body = sine(glide(420 * p, 160 * p, 0.12)) * expenv(0.12, 0.035)
    return mix(d, (0, burst, 0.7), (0, body), (0.05, bubble(1200 * p, 0.06, 1.6), 0.3))


@sfx("Projectile_Impact", 3, 0.3)
def projectile_impact(rng, v):
    d = 0.08
    return hp(noise(0.03, rng), 2000) * expenv(0.03, 0.007) + bubble(700 * pv(rng, v, 0.1), 0.05, 1.4) * 0.4


# ====================================================== jefe: virus gigante

def crystals(rng, d, count, lo=1800, hi=4200):
    buf = np.zeros(n(d))
    for _ in range(count):
        place(buf, bell(rng.uniform(lo, hi), 0.2, 0.06, 2.41, 1.8), rng.uniform(0, d - 0.1), rng.uniform(0.2, 0.6))
    return buf


@sfx("Boss_Virus_Intro", 1, 0.8)
def virus_intro(rng, v):
    d = 1.5
    buzz = saw(vibrato(55, d, 7, 0.01, 0), d) + saw(55.6, d) * 0.7
    buzz = svf(buzz, glide(150, 2500, d), 1.5) * swell(d, 0.85, 1.5)
    locks = mix(d, (1.1, click(rng, 0.01, 1500)), (1.22, click(rng, 0.01, 1500)), (1.32, click(rng, 0.01, 1200)))
    return reverb(buzz * 0.7 + crystals(rng, d, 12) * np.linspace(0, 1, n(d)) * 0.5 + locks, rng, 1.0, 0.25)[: n(d)]


@sfx("Boss_Virus_Spiral", 3, 0.18)
def virus_spiral(rng, v):
    d = 0.06
    p = pv(rng, v, 0.08)
    return lp(square(1800 * p, d, 0.3), 5000) * expenv(d, 0.012) + hp(noise(d, rng), 5000) * expenv(d, 0.006) * 0.4


@sfx("Boss_Virus_BurstWindup", 1, 0.6)
def virus_burst_windup(rng, v):
    d = 0.6
    ramp = np.linspace(0.2, 1, n(d)) ** 2
    return (sine(glide(200, 1300, d)) * am(np.ones(n(d)), 30, 0.6) + lp(saw(glide(100, 400, d), d), 3000) * 0.3) * ramp


@sfx("Boss_Virus_Burst", 1, 0.75)
def virus_burst(rng, v):
    d = 0.5
    shot = lp(noise(0.15, rng), 3500) * expenv(0.15, 0.04) + thump(320, 60, 0.3, 0.09)
    shot = shot + crystals(rng, 0.3, 4, 2500, 4000) * 0.4
    return delay(shot, 0.11, 0.4, 0.35, 3)[: n(d)]


@sfx("Boss_Virus_SummonWindup", 1, 0.55)
def virus_summon_windup(rng, v):
    d = 0.6
    bub = np.zeros(n(d))
    for i in range(18):
        t0 = d * (i / 18) ** 0.6
        place(bub, lp(bubble(rng.uniform(180, 450), 0.08, 1.5), 1800), t0, 0.3 + i * 0.04)
    whine = sine(glide(300, 700, d)) * np.linspace(0, 0.35, n(d))
    return bub + whine


@sfx("Boss_Virus_Summon", 1, 0.55)
def virus_summon(rng, v):
    d = 0.4
    plop = lambda f: lp(bubble(f, 0.12, 1.5, 0.04), 1500) + lp(noise(0.03, rng), 1000) * expenv(0.03, 0.01) * 0.5
    return mix(d, (0, plop(260)), (0.18, plop(330)))


@sfx("Boss_Virus_Phase2", 1, 0.9)
def virus_phase2(rng, v):
    d = 1.5
    f = glide(55, 130, d)
    cluster = sum(saw(f * r, d) for r in (1, 1.007, 1.5, 2.01))
    cluster = dist(svf(cluster, glide(300, 3000, d), 1.2), 4) * swell(d, 0.8, 1.2)
    crack = lp(noise(0.2, rng), 4000) * expenv(0.2, 0.05)
    parts = [(0, cluster, 0.6), (0, crack, 0.8), (0, thump(120, 40, 0.6, 0.2))]
    for _ in range(16):
        parts.append((rng.uniform(0.2, 1.3), hp(noise(0.01, rng), 2000) * expenv(0.01, 0.003), rng.uniform(0.3, 0.7)))
    return reverb(mix(d, *parts), rng, 1.0, 0.25)[: n(d)]


@sfx("Boss_Virus_Hurt", 3, 0.5)
def virus_hurt(rng, v):
    d = 0.12
    p = pv(rng, v, 0.07)
    return bell(700 * p, d, 0.04, 2.41, 2.0) * 0.6 + thump(150 * p, 60, d, 0.035)


@sfx("Boss_Virus_Death", 1, 1.0)
def virus_death(rng, v):
    d = 2.5
    parts = []
    t = 0.0
    while t < 1.6:  # cortocircuito cada vez más seguido
        dur = rng.uniform(0.01, 0.04)
        parts.append((t, hp(noise(dur, rng), 1500) * expenv(dur, dur / 3), rng.uniform(0.4, 1)))
        t += max(0.012, 0.15 * (1 - t / 1.6) ** 2 + rng.uniform(0, 0.02))
    buzz = saw(glide(110, 35, 1.8), 1.8) * expenv(1.8, 0.7) * 0.4
    crack = lp(noise(0.5, rng), 3000) * expenv(0.5, 0.15)
    boom = thump(120, 30, 1.0, 0.35)
    bub = bubbles(0.9, rng, 22, 300, 1500, (0.05, 0.14), gains=(0.2, 0.5))
    parts += [(0, buzz), (1.5, crack), (1.5, boom), (1.55, bub), (1.5, crystals(rng, 0.6, 8), 0.4)]
    return reverb(mix(d, *parts), rng, 1.4, 0.3)[: n(d)]


# ======================================================= jefe: ameba gigante

def gurgle(rng, d, count, lo=90, hi=260):
    return lp(bubbles(d, rng, count, lo, hi, (0.08, 0.18), 1.4, (0.3, 0.8)), 900)


@sfx("Boss_Amoeba_Intro", 1, 0.8)
def amoeba_intro(rng, v):
    d = 1.5
    mass = sine(vibrato(glide(75, 45, d), d, 4, 0.05, 0)) * swell(d, 0.25, 1.2)
    settle = lp(noise(d, rng), 400) * swell(d, 0.3) * 0.5
    return reverb(mass + settle + gurgle(rng, d, 8), rng, 1.0, 0.2)[: n(d)]


@sfx("Boss_Amoeba_Warning", 1, 0.7)
def amoeba_warning(rng, v):
    d = 0.6
    f = np.concatenate([np.full(n(0.42), 90.0), glide(90, 170, d - 0.42)])[: n(d)]
    f = fit(f, d)
    body = sine(f) + tri(f * 2, d) * 0.3
    trem = am(body, 14, 0.8) * np.linspace(0.4, 1, n(d))
    squeeze = noise_sweep(0.18, 200, 900, rng, 3) * swell(0.18, 0.9)
    return lp(mix(d, (0, trem), (0.42, squeeze, 0.5)), 2000)


@sfx("Boss_Amoeba_Launch", 2, 0.7)
def amoeba_launch(rng, v):
    d = 0.3
    p = pv(rng, v, 0.08)
    w = noise_sweep(d, 200 * p, 950 * p, rng, 1.8) * swell(d, 0.25)
    return w + sine(glide(80, 150, d)) * swell(d, 0.3) * 0.6


@sfx("Boss_Amoeba_WallSlam", 3, 0.9)
def amoeba_wall_slam(rng, v):
    d = 0.5
    p = pv(rng, v, 0.08)
    boom = thump(110 * p, 32, d, 0.15)
    splash = lp(noise(0.25, rng), 1200 * p) * expenv(0.25, 0.06)
    wet = hp(noise(0.1, rng), 3000) * expenv(0.1, 0.02) * 0.3
    return dist(boom + splash * 0.8 + wet, 1.6)


def growl(rng, d, f0=70, f1=70, drive=3.0):
    jitter = lp(rng.standard_normal(n(d)), 40) * 25
    f = glide(f0, f1, d) * (1 + jitter * 0.01)
    g = saw(f, d) + saw(f * 1.01, d) * 0.7 + square(f * 0.5, d) * 0.4
    return dist(lp(g, 1100), drive)


@sfx("Boss_Amoeba_Enrage", 1, 0.8)
def amoeba_enrage(rng, v):
    d = 1.2
    g = growl(rng, d, 65, 85) * swell(d, 0.6)
    return g * 0.6 + gurgle(rng, d, 14, 120, 400) * 0.6


@sfx("Boss_Amoeba_Split", 1, 0.8)
def amoeba_split_boss(rng, v):
    d = 1.0
    a = sine(vibrato(glide(150, 105, 0.75), 0.75, 7, 0.04, 0))
    b = sine(vibrato(glide(150, 210, 0.75), 0.75, 6, 0.04, 0))
    tear = np.zeros(n(0.75))
    for _ in range(30):
        place(tear, bp(noise(0.006, rng), 400, 2000) * expenv(0.006, 0.002), rng.uniform(0, 0.7), rng.uniform(0.2, 0.7))
    stretch = (a + b) * swell(0.75, 0.7) * 0.5 + tear * 0.6
    plop = lambda f: lp(bubble(f, 0.15, 1.4, 0.05), 1200) + thump(130, 50, 0.15, 0.05) * 0.6
    return mix(d, (0, stretch), (0.72, plop(180)), (0.8, plop(240)))


@sfx("Boss_Amoeba_Bounce", 2, 0.6)
def amoeba_bounce(rng, v):
    d = 0.25
    t = tt(d)
    f = 280 * pv(rng, v, 0.08) * (1 + 0.35 * np.exp(-t / 0.08) * np.sin(2 * np.pi * 18 * t))
    return sine(f) * expenv(d, 0.08) + thump(150, 70, 0.1, 0.03) * 0.5


@sfx("Boss_Amoeba_Merge", 1, 0.8)
def amoeba_merge(rng, v):
    d = 3.0
    a = sine(vibrato(glide(85, 110, d, "lin"), d, 3, 0.03, 0))
    b = sine(vibrato(glide(140, 110, d, "lin"), d, 3.7, 0.03, 0))
    ramp = np.linspace(0.25, 1, n(d)) ** 1.5
    body = (a + b) * 0.5 * ramp
    swish = lp(noise(d, rng), glide(200, 700, d)[0]) * ramp * 0.4
    end = thump(110, 40, 0.5, 0.2)
    return reverb(mix(d, (0, body), (0, swish), (0, gurgle(rng, d, 22, 100, 320) * ramp), (2.65, end, 0.8)), rng, 1.2, 0.25)[: n(d)]


@sfx("Boss_Amoeba_Fury", 1, 0.95)
def amoeba_fury(rng, v):
    d = 2.0
    roar = growl(rng, 1.8, 55, 110, 4.0) * swell(1.8, 0.85, 1.3)
    bub = gurgle(rng, 1.8, 30, 150, 600) * np.linspace(0.2, 1, n(1.8))
    hit = dist(thump(130, 32, 0.4, 0.15) + lp(noise(0.15, rng), 2000) * expenv(0.15, 0.04), 2)
    return mix(d, (0, roar, 0.6), (0, bub, 0.5), (1.78, hit))


@sfx("Boss_Amoeba_Barrage", 3, 0.18)
def amoeba_barrage(rng, v):
    d = 0.06
    p = pv(rng, v, 0.08)
    return sine(glide(190 * p, 115 * p, d)) * expenv(d, 0.018) + click(rng, 0.004, 1500) * 0.2


@sfx("Boss_Amoeba_Immune", 2, 0.45)
def amoeba_immune(rng, v):
    d = 0.1
    p = pv(rng, v, 0.06)
    return lp(sine(140 * p, d) * expenv(d, 0.03) + tri(280 * p, d) * expenv(d, 0.015) * 0.4, 1200)


@sfx("Boss_Amoeba_Exhausted_Loop", 1, 0.45, loop=True)
def amoeba_exhausted(rng, v):
    d = 2.0
    buf = np.zeros(n(d))
    breath = lambda dd, fc: lp(noise(dd, rng), fc) * swell(dd, 0.4, 1.5)
    place(buf, breath(0.7, 600), 0.0, 0.8, True)   # inspira
    place(buf, breath(0.8, 400), 0.85, 1.0, True)  # espira
    for _ in range(4):
        place(buf, lp(bubble(rng.uniform(140, 260), 0.12, 1.3), 900), rng.uniform(0, d), 0.25, True)
    return buf


@sfx("Boss_Amoeba_Hurt", 3, 0.5)
def amoeba_hurt(rng, v):
    d = 0.12
    p = pv(rng, v, 0.07)
    return sine(vibrato(glide(170 * p, 100 * p, d), d, 25, 0.06, 0)) * expenv(d, 0.04) + lp(noise(0.03, rng), 900) * expenv(0.03, 0.01) * 0.5


@sfx("Boss_Amoeba_Death", 1, 1.0)
def amoeba_death(rng, v):
    d = 2.5
    slide = sine(vibrato(glide(110, 30, 1.8), 1.8, 5, 0.05, 0)) * expenv(1.8, 0.7, 0.02)
    collapse = lp(noise(1.6, rng), 600) * swell(1.6, 0.3) * 0.5
    pop = lp(noise(0.15, rng), 3000) * expenv(0.15, 0.04) + bubble(260, 0.4, 2.2, 0.12)
    bub = bubbles(0.7, rng, 16, 400, 1400, (0.05, 0.12), gains=(0.2, 0.45))
    return reverb(mix(d, (0, slide), (0, collapse), (0, gurgle(rng, 1.6, 20, 80, 300)), (1.75, pop), (1.8, bub)),
                  rng, 1.4, 0.3)[: n(d)]


@sfx("Cinematic_Bars", 1, 0.5)
def cinematic_bars(rng, v):
    d = 0.6
    return lp(noise(d, rng), 500) * swell(d, 0.5) * 0.8 + sine(glide(70, 55, d)) * swell(d, 0.5) * 0.5


# ================================================ salas, mapa y recompensas

@sfx("Room_DoorsClose", 1, 0.7)
def doors_close(rng, v):
    d = 0.5
    tense = sine(glide(120, 230, 0.35)) * swell(0.35, 0.8)
    air = lp(noise(0.35, rng), 900) * swell(0.35, 0.8) * 0.4
    seal = thump(140, 45, 0.15, 0.05)
    return mix(d, (0, tense, 0.6), (0, air), (0.33, seal))


@sfx("Room_DoorsOpen", 1, 0.55)
def doors_open(rng, v):
    d = 0.5
    relax = sine(glide(230, 120, 0.4)) * expenv(0.4, 0.15, 0.01)
    release = noise_sweep(0.45, 600, 2200, rng, 1.5) * swell(0.45, 0.2) * 0.5
    return mix(d, (0, relax, 0.5), (0.02, release))


@sfx("Room_Cleared", 1, 0.6)
def room_cleared(rng, v):
    d = 0.8
    a = arp([F5, A5, D6], 0.1, 0.3, "sq", 0.12, [0.8, 0.8, 1])
    jingle = mix(d, (0, a, 0.45), (0.2, bell(D6, 0.5, 0.2, 2.0, 1.0), 0.3))
    return reverb(lp(jingle, 6000), rng, 0.6, 0.25)[: n(d)]


@sfx("Room_Exit", 1, 0.65)
def room_exit(rng, v):
    d = 0.8
    swirl = am(noise_sweep(d, 300, 2500, rng, 2), 9, 0.6) * swell(d, 0.6)
    rise = sine(glide(220, 660, d)) * swell(d, 0.7) * 0.3
    return swirl + rise


@sfx("Transition_Jump", 1, 0.55)
def transition_jump(rng, v):
    """La criatura toma impulso y salta hacia el centro de la fosa."""
    d = 0.45
    push = thump(180, 70, 0.12, 0.04) * 0.6
    whoosh = noise_sweep(d, 450, 2400, rng, 2.2) * swell(d, 0.45) * 0.7
    rise = tri(glide(260, 620, d)) * swell(d, 0.5) * 0.2
    return mix(d, (0, push), (0, whoosh), (0, rise))


@sfx("Transition_Fall", 1, 0.65)
def transition_fall(rng, v):
    """Cae a la fosa: un silbido que baja hacia lo hondo y acaba en un 'bloop' grave al llegar al agua del fondo."""
    d = 2.0
    fall = 1.0
    whistle = noise_sweep(fall, 2200, 160, rng, 2.5) * swell(fall, 0.25) * 0.6
    sink = sine(glide(520, 90, fall)) * swell(fall, 0.3) * 0.25
    plunge = np.zeros(n(d))
    place(plunge, lp(bubble(95, 0.35, 1.2), 600) * 0.9, fall - 0.05)
    place(plunge, thump(90, 35, 0.4, 0.15) * 0.7, fall - 0.05)
    for k in range(6):  # burbujas que suben tras el chapuzón
        place(plunge, bubble(300 + k * 90 + rng.uniform(-30, 30), 0.07, 1.8) * 0.25, fall + 0.1 + k * 0.07)
    dry = mix(d, (0, whistle), (0, sink), (0, plunge))
    return fade(reverb(dry, rng, rt=1.8, wet=0.35, bright=1500, tail=False), fout=0.4)


@sfx("Biomass_Pickup_Small", 3, 0.35)
def biomass_small(rng, v):
    d = 0.1
    p = pv(rng, v, 0.05)
    return bubble(1250 * p, d, 1.5, 0.025) * 0.8 + square(A5 * p * 2, 0.04, 0.25) * expenv(0.04, 0.012) * 0.15


@sfx("Biomass_Pickup_Big", 1, 0.5)
def biomass_big(rng, v):
    d = 0.25
    a = mix(d, (0, bubble(900, 0.1, 1.5, 0.03)), (0.06, bubble(1350, 0.12, 1.5, 0.04)))
    sh = bell(A6, d, 0.08, 2.0, 0.8) * 0.25
    return a + sh


@sfx("Biomass_Magnet", 1, 0.45)
def biomass_magnet(rng, v):
    d = 0.6
    return noise_sweep(d, 300, 3200, rng, 3) * swell(d, 0.85) + sine(glide(300, 1200, d)) * swell(d, 0.85) * 0.2


@sfx("Mutation_ChoiceOpen", 1, 0.6)
def mutation_choice_open(rng, v):
    d = 0.5
    parts = [(i * 0.09, bubble(f, 0.12, 1.3, 0.04), 0.7) for i, f in enumerate((D5, F5, A5))]
    sh = hp(noise(0.25, rng), 5000) * swell(0.25, 0.3) * 0.12
    return reverb(mix(d, *parts, (0.27, sh), (0.27, bell(D6, 0.22, 0.1, 2, 1), 0.3)), rng, 0.5, 0.2)[: n(d)]


@sfx("Mutation_Bubble_Spawn", 1, 0.5)
def mutation_bubble_spawn(rng, v):
    d = 0.4
    return bubbles(d, rng, 7, 400, 1100, (0.06, 0.14), 1.4, (0.4, 0.8))


@sfx("Mutation_Bubble_Pickup", 1, 0.65)
def mutation_bubble_pickup(rng, v):
    d = 0.4
    pop = bubble(420, 0.18, 2.2, 0.06) + lp(noise(0.03, rng), 4000) * expenv(0.03, 0.008) * 0.5
    return mix(d, (0, pop), (0.05, bell(A5, 0.3, 0.12, 2.0, 1.0), 0.35))


@sfx("Mutation_Bubble_Burst", 1, 0.45)
def mutation_bubble_burst(rng, v):
    d = 0.3
    pop = lambda f: bubble(f, 0.08, 1.8, 0.02) + hp(noise(0.02, rng), 3000) * expenv(0.02, 0.005) * 0.4
    return mix(d, (0, pop(700)), (0.12, pop(820)))


# ================================================================== tienda

@sfx("Shop_Approach", 1, 0.3)
def shop_approach(rng, v):
    d = 0.2
    return bell(2000, d, 0.06, 2.0, 0.8) * 0.6 + hp(noise(d, rng), 7000) * expenv(d, 0.05) * 0.1


@sfx("Shop_Purchase", 1, 0.7)
def shop_purchase(rng, v):
    d = 0.6
    ka = click(rng, 0.015, 1500) + bp(noise(0.03, rng), 1500, 5000) * expenv(0.03, 0.008)
    shell = lambda f: sum(sine(f * r, 0.4) * expenv(0.4, 0.12 / r) * g for r, g in ((1, 1), (2.76, 0.5), (5.4, 0.25)))
    bub = bubbles(0.4, rng, 6, 900, 2400, (0.04, 0.08), gains=(0.15, 0.3))
    return reverb(mix(d, (0, ka, 0.7), (0.06, shell(1320)), (0.14, shell(1760)), (0.1, bub)), rng, 0.6, 0.2)[: n(d)]


@sfx("Shop_CantAfford", 1, 0.45)
def shop_cant_afford(rng, v):
    d = 0.25
    a = lp(square(midi(57), 0.11, 0.5), 900) * expenv(0.11, 0.06)
    b = lp(square(midi(53), 0.13, 0.5), 900) * expenv(0.13, 0.07)
    return mix(d, (0, a), (0.11, b))


@sfx("Shop_Darwin_Cheer", 2, 0.55)
def darwin_cheer(rng, v):
    d = 0.6
    parts = []
    t = 0.0
    base = 330 * pv(rng, v, 0.1)
    for i in range(6):
        sd = rng.uniform(0.06, 0.09)
        f = base * 2 ** (rng.choice([0, 2, 3, 5, 7]) / 12) * (1 + i * 0.03)
        syl = square(vibrato(glide(f, f * rng.uniform(0.92, 1.12), sd), sd, 9, 0.02, 0), sd, 0.3)
        syl = lp(syl, 1800 + rng.uniform(0, 800)) * adsr(sd, 0.008, 0.02, 0.7, 0.02)
        parts.append((t, syl, 0.7))
        t += sd + rng.uniform(0.005, 0.02)
    parts.append((t, square(vibrato(glide(base * 1.5, base * 2, 0.12), 0.12, 9, 0.02, 0), 0.12, 0.3) * expenv(0.12, 0.06), 0.6))
    parts.append((0.1, bubbles(0.45, rng, 5, 900, 2000, (0.04, 0.08), gains=(0.1, 0.25))))
    return mix(d, *parts)


@sfx("Shop_HeartContainer", 1, 0.75)
def shop_heart_container(rng, v):
    d = 0.8
    beat = lp(thump(90, 45, 0.25, 0.08), 600)
    chord = arp([D5, Fs5, A5, D6], 0.07, 0.4, "tri", 0.18)
    return reverb(mix(d, (0, beat), (0.22, beat, 0.8), (0.3, chord, 0.4), (0.3, bell(D6, 0.45, 0.18, 2, 1), 0.3)),
                  rng, 0.6, 0.2)[: n(d)]


# ================================================================ interfaz

@sfx("UI_Hover", 1, 0.25, "UI")
def ui_hover(rng, v):
    d = 0.05
    return sine(glide(D6 * 2, A5 * 2, d)) * expenv(d, 0.01)


@sfx("UI_Click", 1, 0.45, "UI")
def ui_click(rng, v):
    d = 0.1
    return bubble(A5, d, 1.5, 0.03) + click(rng, 0.005, 3000) * 0.3


@sfx("UI_Back", 1, 0.4, "UI")
def ui_back(rng, v):
    d = 0.1
    return sine(glide(A5, D5, d)) * expenv(d, 0.03) + click(rng, 0.005, 2000) * 0.25


@sfx("UI_CardSelect", 1, 0.55, "UI")
def ui_card_select(rng, v):
    d = 0.4
    return mix(d, (0, bubble(D5, 0.1, 1.5, 0.03)), (0.07, note(D5, 0.12, "sq", 0.05), 0.25),
               (0.14, note(A5, 0.25, "sq", 0.1), 0.3), (0.14, bell(A5 * 2, 0.25, 0.1, 2, 1), 0.2))


@sfx("UI_StartGame", 1, 0.65, "UI")
def ui_start_game(rng, v):
    d = 1.0
    parts = []
    f = 1600
    for i in range(12):
        parts.append((i * 0.05, bubble(f, 0.08, 1.4), 0.5))
        f *= 0.88
    boom = lp(thump(110, 40, 0.6, 0.25), 400)
    return reverb(mix(d, *parts, (0.4, boom, 0.8), (0.4, lp(noise(0.5, rng), 600) * swell(0.5, 0.2), 0.4)),
                  rng, 0.8, 0.25)[: n(d)]


@sfx("UI_PauseOpen", 1, 0.45, "UI")
def ui_pause_open(rng, v):
    d = 0.25
    return lp(sine(glide(400, 190, d)) * expenv(d, 0.09) + noise(d, rng) * swell(d, 0.2) * 0.3, 700)


@sfx("UI_PauseClose", 1, 0.45, "UI")
def ui_pause_close(rng, v):
    d = 0.25
    return lp(sine(glide(190, 400, d)) * swell(d, 0.4) + noise(d, rng) * swell(d, 0.6) * 0.3, 1200)


@sfx("UI_MapOpen", 1, 0.45, "UI")
def ui_map_open(rng, v):
    d = 0.3
    parts = [(i * 0.04, bell(f, 0.15, 0.04, 2.41, 1.2), 0.6) for i, f in enumerate((1500, 1900, 2400, 3000))]
    return mix(d, *parts, (0, noise_sweep(0.2, 1000, 5000, rng, 3) * swell(0.2, 0.7), 0.3))


@sfx("UI_MapClose", 1, 0.4, "UI")
def ui_map_close(rng, v):
    d = 0.2
    parts = [(i * 0.035, bell(f, 0.12, 0.035, 2.41, 1.2), 0.6) for i, f in enumerate((2600, 1900, 1400))]
    return mix(d, *parts)


# ================================================================= ambiente

def loop_noise(d, rng, fc, order=2):
    """Ruido filtrado en frecuencia, ya cíclico (el final enlaza con el principio)."""
    N = n(d)
    X = np.fft.rfft(rng.standard_normal(N))
    f = np.fft.rfftfreq(N, 1 / SR)
    X *= 1 / np.sqrt(1 + (f / fc) ** (2 * order))
    return np.fft.irfft(X, N)


def loop_sine(f, d, amp=1.0):
    """Seno con un número entero de ciclos en el bucle."""
    k = max(1, round(f * d))
    return amp * np.sin(2 * np.pi * k * np.arange(n(d)) / n(d))


@sfx("Amb_Floor1_Loop", 1, 0.25, loop=True)
def amb_floor1(rng, v):
    d = 24.0
    N = n(d)
    rumble = loop_noise(d, rng, 180, 3)
    rumble /= np.max(np.abs(rumble))
    slow = 0.75 + 0.25 * np.sin(2 * np.pi * 2 * np.arange(N) / N)  # dos vaivenes por bucle
    buf = rumble * slow * 0.5
    for _ in range(70):
        f = np.exp(rng.uniform(np.log(300), np.log(1800)))
        place(buf, bubble(f, rng.uniform(0.04, 0.1), 1.6), rng.uniform(0, d), rng.uniform(0.04, 0.16), True)
    for _ in range(30):  # chasquidos microscópicos
        place(buf, bp(noise(0.003, rng), 2000, 7000) * expenv(0.003, 0.001), rng.uniform(0, d), rng.uniform(0.02, 0.06), True)
    ir = impulse_response(1.5, rng, bright=2500)
    return buf * 0.8 + circular_conv(buf, ir) * 0.35


@sfx("Amb_BossRoom_Loop", 1, 0.3, loop=True)
def amb_boss_room(rng, v):
    d = 16.0
    N = n(d)
    rumble = loop_noise(d, rng, 120, 3)
    rumble /= np.max(np.abs(rumble))
    hum = loop_sine(55, d) + loop_sine(55.5, d) * 0.8 + loop_sine(82.5, d) * 0.3
    hum *= 0.7 + 0.3 * np.sin(2 * np.pi * 3 * np.arange(N) / N)
    buf = rumble * 0.6 + hum * 0.12
    for _ in range(45):
        f = np.exp(rng.uniform(np.log(150), np.log(900)))
        place(buf, lp(bubble(f, rng.uniform(0.06, 0.14), 1.5), 1500), rng.uniform(0, d), rng.uniform(0.05, 0.15), True)
    ir = impulse_response(2.0, rng, bright=1500)
    return buf * 0.8 + circular_conv(buf, ir) * 0.4


@sfx("Amb_Floor2_Loop", 1, 0.3, loop=True)
def amb_floor2(rng, v):
    """Océano cámbrico: mar abierto y profundo. Sube junto a la fosa de la sala de paso y será el ambiente del piso 2."""
    d = 24.0
    N = n(d)
    t = np.arange(N) / N
    rumble = loop_noise(d, rng, 90, 3)
    rumble /= np.max(np.abs(rumble))
    # Oleaje lento: tres vaivenes por bucle, con el agua que se mueve por encima.
    swell_env = 0.65 + 0.35 * np.sin(2 * np.pi * 3 * t)
    current = loop_noise(d, rng, 700, 2)
    current = current / np.max(np.abs(current)) * (0.5 + 0.5 * np.sin(2 * np.pi * 3 * t - 0.8)) ** 2
    # Un fondo grave que respira, como la luz del fondo de la fosa.
    drone = loop_sine(41.2, d) + loop_sine(61.8, d) * 0.45 + loop_sine(82.6, d) * 0.15
    drone *= 0.75 + 0.25 * np.sin(2 * np.pi * 4 * t)
    buf = rumble * swell_env * 0.55 + current * 0.12 + drone * 0.1
    for _ in range(26):  # burbujas grandes y lentas, más graves que en la sopa
        f = np.exp(rng.uniform(np.log(110), np.log(520)))
        place(buf, lp(bubble(f, rng.uniform(0.1, 0.22), 1.3), 1200), rng.uniform(0, d), rng.uniform(0.06, 0.16), True)
    for _ in range(5):  # corrientes que pasan a lo lejos
        w = 2.5
        place(buf, bp(noise(w, rng), 180, 900) * swell(w, 0.5), rng.uniform(0, d), rng.uniform(0.08, 0.14), True)
    ir = impulse_response(3.0, rng, bright=1100)
    return buf * 0.75 + circular_conv(buf, ir) * 0.45
