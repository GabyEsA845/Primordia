"""Plantilla de los módulos de audio de un piso nuevo (efectos_pisoN.py y musica_pisoN.py).

Copiar la parte de EFECTOS a docs/multimedia-temp/audio/efectos_pisoN.py y la de MÚSICA a musica_pisoN.py,
cambiar los nombres de ejemplo por los de las tablas de PROMPT_pisoN.md y borrar lo que no se use. Los dos
módulos registran en los mismos SFX y MUSIC que el piso 1, así generar_audio.py, sonidos.json y el muestrario
los tratan igual. En generar_audio.py, importarlos después de efectos y musica:

    from efectos import SFX
    from musica import MUSIC
    import efectos_pisoN, musica_pisoN  # noqa: F401  (registran sus sonidos al importarse)

y leer las tablas de todos los encargos en read_prompt() (sorted(ROOT.glob("PROMPT*.md"))).
"""

# ================================================================== EFECTOS
import numpy as np

from efectos import arp, bell, loop_noise, loop_sine, note, puff, pv, sfx
from sintesis import (SR, bp, bubble, bubbles, circular_conv, click, dist, expenv, fm, glide, hp,
                      impulse_response, lp, midi, mix, n, noise, noise_sweep, place, reverb, sine,
                      svf, swell, thump, tri)
from sintesis import padded

# Las señales se suman aunque duren distinto (la más corta se rellena con silencio)
for _name in ("bp", "bubble", "bubbles", "click", "dist", "expenv", "fm", "glide", "hp", "lp", "mix",
              "noise", "noise_sweep", "reverb", "sine", "svf", "swell", "thump", "tri"):
    globals()[_name] = padded(globals()[_name])

# Notas en re (tonalidad común del juego) con el modo del piso, para que los avisos con nota casen con la música
D3, A3, D4 = (midi(m) for m in (50, 57, 62))


# --------------------------------------------------------- enemigo de ejemplo

@sfx("Enemy_Example_Windup", 1, 0.6)
def example_windup(rng, v):
    """Aviso de 0,6 s (lo que dura el aviso en el código): sube de tono y de brillo y llega arriba al final."""
    d = 0.6
    # Desde un 15 % y no desde cero: finish_sfx recorta el silencio inicial y el aviso quedaría más corto
    body = tri(glide(D3, D3 * 2, d)) * (0.15 + 0.85 * swell(d, 0.95, 1.5))
    grit = svf(noise(d, rng), glide(300, 2500, d), 1.0, "lp") * swell(d, 0.95, 2.0) * 0.3  # corte que se abre
    return body + grit


@sfx("Enemy_Example_Hit", 3, 0.4)
def example_hit(rng, v):
    """Golpe frecuente: ataque + cuerpo, sin cola; cada variante algo distinta (pv)."""
    k = pv(rng, v)
    attack = click(rng, 0.006, 2500)
    body = thump(220 * k, 90 * k, 0.1, 0.03) + bp(noise(0.08, rng), 300, 1500) * expenv(0.08, 0.02) * 0.5
    return mix(0.1, (0, attack, 0.6), (0, body))


# ----------------------------------------------------------- bucle de ejemplo

@sfx("Amb_Example_Loop", 1, 0.25, loop=True)
def example_loop(rng, v):
    """Todo cíclico: loop_noise, loop_sine, modulación con vueltas enteras, place(wrap=True), circular_conv."""
    d = 16.0
    t = np.arange(n(d)) / n(d)
    rumble = loop_noise(d, rng, 120, 3)
    rumble /= np.max(np.abs(rumble))
    buf = rumble * (0.7 + 0.3 * np.sin(2 * np.pi * 2 * t)) * 0.6 + loop_sine(D3 / 4, d) * 0.1
    for _ in range(20):
        place(buf, lp(bubble(rng.uniform(150, 600), 0.12, 1.4), 1500), rng.uniform(0, d), 0.1, True)
    return buf * 0.8 + circular_conv(buf, impulse_response(2.0, rng, bright=1500)) * 0.4


# =================================================================== MÚSICA
# (en musica_pisoN.py)

from musica import (MUSIC, Song, bubble_bed, i_bass, i_bell, make_kit, play_melody, play_pad,  # noqa: E402
                    section_melody)

D_DORIAN = {2, 4, 5, 7, 9, 11, 0}  # re dórico: re menor con si natural


def example_material():
    """Progresión y melodía compartidas por las dos capas (así se funden sin chocar)."""
    rng = np.random.default_rng(201)
    prog = ["Dm"] * 8  # cambiar por acordes de CHORDS (añadir los que falten) en re, con el modo del piso
    mel = section_melody(rng, prog, 8, 4, 62, 81, D_DORIAN)
    return prog, mel


def music_example_explore():
    prog, mel = example_material()
    s = Song(90, len(prog) * 4, seed=21)  # BPM con pulso entero de muestras (tecnica.md)
    play_pad(s, prog, gain=0.16, bright=1000)
    play_melody(s, mel, lambda f, d: i_bell(f, d, 0.5, 3.0, 0.8), gain=0.3, rev=0.5)
    bubble_bed(s, 40, 0.06, 200, 900)
    return s.render(rt=2.4, wet=0.6, bright=3000)


def music_example_combat():
    prog, mel = example_material()
    s = Song(90, len(prog) * 4, seed=22)  # mismo tempo y compases que la capa de exploración
    kit = make_kit(s.rng)
    play_pad(s, prog, gain=0.14, bright=1400)
    for bar in range(len(prog)):
        s.add(bar * 4, kit["kick"], 0, 0.5, 0.05)
        s.add(bar * 4 + 2, kit["snare"], 0, 0.35, 0.2)
        s.add(bar * 4, i_bass(midi(40), s.secs(1.8)), 0, 0.4, 0.1)
    play_melody(s, mel, lambda f, d: i_bell(f, d, 0.4, 3.0, 1.0), gain=0.32, rev=0.4)
    return s.render(rt=2.0, wet=0.5, bright=4000)


MUSIC.update({
    "Music_Example_Explore": (music_example_explore, True, 0.6),
    "Music_Example_Combat": (music_example_combat, True, 0.6),
})
