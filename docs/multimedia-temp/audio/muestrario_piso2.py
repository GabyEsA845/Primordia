"""Muestrario de instrumentos del piso 2: una demo corta de cada instrumento nuevo y un esbozo de unos 20 s de cada
franja (Someras, Arrecife, Abismo) y del laboratorio de Darwin, para elegirlos antes de escribir el encargo.

Uso: python muestrario_piso2.py [nombre ...]   (sin nombres, todo)
Salida: muestrario-piso2/*.ogg y muestrario_piso2.html."""

import html
import sys
from pathlib import Path

import numpy as np

from generar_audio import decoded_samples, encode_ogg
from instrumentos_piso2 import Song2, add_echo, i_glass, i_marimba, i_tide, sea_kit, song_line
from musica import Song, bass_note, bubble_bed, i_bass, i_flute, i_pad, i_sub, play_pad, voicing
from sintesis import SR, midi

ROOT = Path(__file__).resolve().parent
OUT = ROOT / "muestrario-piso2"


# ==================================================================== demos

def demo_marimba():
    s = Song(100, 16, loop=False, tail=2.0, seed=31)
    for bar, ch in enumerate(["Dm", "G", "C", "Dm"]):
        tones = voicing(ch, 62) + [m + 12 for m in voicing(ch, 62)]
        s.add(bar * 4, i_marimba(midi(bass_note(ch, 38) + 12)), -0.2, 0.3, 0.15)
        if bar == 3:
            for k, m in enumerate(tones):
                s.add(12 + k * 0.04, i_marimba(midi(m)), (k - 2.5) * 0.2, 0.18, 0.2)
            continue
        for e, idx in enumerate([0, 1, 2, 3, 4, 5, 4, 3]):
            s.add(bar * 4 + e * 0.5, i_marimba(midi(tones[idx])), 0.2, 0.22 if e % 2 == 0 else 0.16, 0.15)
    return s.render(rt=1.4, wet=0.35, bright=5000)


def demo_song():
    s = Song(72, 22, loop=False, tail=2.5, seed=32)
    notes = [(0, 3, 50), (3, 1, 53), (4, 4, 57), (8, 2, 55), (10, 2, 53), (12, 3, 50), (15, 1, 48), (16, 6, 50)]
    s.add(0, song_line(notes, s.beat), 0, 0.5, 0.5)
    s.add(0, i_sub(midi(38), s.secs(22)), 0, 0.14, 0.1)
    for k, m in enumerate(voicing("Dm", 50)):
        s.add(0, i_tide(midi(m), s.secs(22), dark=350, bright=1000), (k - 1) * 0.4, 0.05, 0.6)
    return s.render(rt=3.0, wet=0.6, bright=2500)


def demo_tide():
    s = Song(60, 16, loop=False, tail=2.0, seed=33)
    play_pad(s, ["Dm", "Bb", "C", "Dm"], bars_per_chord=1, inst=i_tide, gain=0.2, lo=50, rev=0.5)
    for bar, ch in enumerate(["Dm", "Bb", "C", "Dm"]):
        s.add(bar * 4, i_tide(midi(bass_note(ch, 38)), s.secs(4), dark=250, bright=700), 0, 0.25, 0.3)
    return s.render(rt=2.5, wet=0.5, bright=3000)


def reef_groove(s, k, start, bars, gain=1.0):
    for bar in range(bars):
        b0 = start + bar * 4
        for beat, name, g in ((0, "wood_lo", 0.5), (2.75, "wood_lo", 0.35), (1.5, "wood_hi", 0.3),
                              (3.5, "wood_hi", 0.25), (1, "stone", 0.32), (3, "stone", 0.32)):
            s.add(b0 + beat, k[name], 0.25 if "hi" in name else -0.1, g * gain, 0.15)
        for i in range(16):
            if i in (0, 8):
                continue
            s.add(b0 + i * 0.25, k["chitin"], 0.45, (0.2 if i % 4 == 2 else 0.09) * gain, 0.1)
        for beat in (0.5, 1.5, 2.5, 3.5):
            s.add(b0 + beat, k["shells"], -0.45, 0.16 * gain, 0.1)
        if bar % 2 == 1:
            s.add(b0 + 3.75, k["big_bubble"], 0.3, 0.2 * gain, 0.4)


def demo_percussion():
    s = Song(108, 25, loop=False, tail=1.5, seed=34)
    k = sea_kit(s.rng)
    for i, name in enumerate(("chitin2", "stone", "wood_lo", "wood_hi", "shells", "big_bubble", "deep_drum",
                              "heartbeat")):
        s.add(i, k[name], 0, 0.5, 0.2)
    reef_groove(s, k, 9, 4)
    return s.render(rt=1.2, wet=0.3, bright=6000)


def demo_echo():
    s = Song(100, 16, loop=False, tail=4.0, seed=35)
    k = sea_kit(s.rng)
    for beat, m in ((0, 74), (2, 69), (4, 72), (6, 67), (8, 74), (10, 76), (12, 69)):
        add_echo(s, beat, i_marimba(midi(m)), 0, 0.3, 0.2, every=0.75, fb=0.5, repeats=5)
    add_echo(s, 13.5, k["sonar"], 0, 0.2, 0.3, every=1.5, fb=0.55, repeats=5, darken=4000)
    return s.render(rt=2.0, wet=0.4, bright=4000)


def demo_glass():
    s = Song(150, 24, loop=False, tail=2.0, seed=36, bpb=6)
    mel = [(0, 3, 74), (3, 2, 77), (5, 1, 76), (6, 4, 74), (10, 2, 70), (12, 3, 70), (15, 2, 74), (17, 1, 72),
           (18, 6, 73)]
    for b, d, m in mel:
        s.add(b, i_glass(midi(m), s.secs(d) * 0.95), 0.1, 0.3, 0.5)
    play_pad(s, ["Dm", "Bb", "Gm", "A"], inst=i_pad, gain=0.06, lo=57, rev=0.5, bright=900)
    return s.render(rt=2.2, wet=0.5, bright=4000)


# ================================================================= esbozos

def sketch_shallows():
    """Someras: agua clara con rayos de luz. Re dórico, 100 BPM."""
    prog = ["Dm", "G", "Dm", "C", "Dm", "G", "Am", "C"]
    s = Song2(100, 32, seed=41)
    k = sea_kit(s.rng)
    play_pad(s, prog, inst=i_tide, gain=0.07, lo=57, rev=0.5, dark=700, bright=2600)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        tones = voicing(ch, 62) + [voicing(ch, 62)[0] + 12]
        for e, idx in enumerate([0, 1, 2, 3, 2, 1, 0, 1]):
            s.add(b0 + e * 0.5, i_marimba(midi(tones[idx])), -0.3, 0.13 if e % 2 == 0 else 0.09, 0.3)
        root = bass_note(ch, 38)
        s.add(b0, i_sub(midi(root), s.secs(3.5)), 0, 0.2, 0.1)
        s.add(b0, i_marimba(midi(root + 12)), 0, 0.18, 0.2)
        s.add(b0 + 2.5, i_marimba(midi(root + 12)), 0, 0.12, 0.2)
        for beat in (1.5, 3.5):
            s.add(b0 + beat, k["shells"], 0.4, 0.08, 0.2)
    mel = [(0, 74), (1.5, 76), (2, 77), (4, 79), (5.5, 77), (6, 76), (8, 74), (9, 72), (10, 69), (12, 71), (13, 72),
           (14, 74), (16, 74), (17.5, 76), (18, 77), (20, 81), (21.5, 79), (22, 74), (24, 72), (25, 71), (26, 69),
           (28, 76), (30, 72)]
    for beat, m in mel:
        add_echo(s, beat, i_marimba(midi(m)), 0.15, 0.28, 0.35, every=0.75, fb=0.42, repeats=3)
    bubble_bed(s, 30, 0.05, 900, 2600)
    return s.render(rt=2.0, wet=0.5, bright=5000)


def sketch_reef():
    """Arrecife: lleno de vida y rítmico. Re menor natural, 108 BPM."""
    prog = ["Dm", "Bb", "C", "Dm", "Gm", "Bb", "Am", "Dm"]
    s = Song2(108, 32, seed=42)
    k = sea_kit(s.rng)
    play_pad(s, prog, inst=i_tide, gain=0.05, lo=57, rev=0.4, rate=0.3)
    reef_groove(s, k, 0, 8, 0.85)
    for bar, ch in enumerate(prog):
        b0 = bar * 4
        r = bass_note(ch, 38)
        for beat, m in zip((0, 0.75, 1.5, 2.5, 3, 3.5), (r, r, r + 12, r, r + 7, r + 12)):
            s.add(b0 + beat, i_bass(midi(m), s.secs(0.35)), 0, 0.3, 0.05)
        tones = voicing(ch, 62)
        for e, idx in enumerate([0, 2, 1, 0, 2, 1, 0, 1]):
            s.add(b0 + e * 0.5, i_marimba(midi(tones[idx])), -0.35, 0.17 if e in (0, 3, 6) else 0.1, 0.2)
    mel = [(0, .75, 69), (.75, .75, 72), (1.5, 1.5, 74), (3, 1, 77), (4, 1.5, 74), (5.5, .5, 72), (6, 2, 70),
           (8, .75, 72), (8.75, .75, 74), (9.5, 1.5, 76), (11, 1, 79), (12, 3, 74), (15, 1, 72),
           (16, .75, 70), (16.75, .75, 72), (17.5, 1.5, 74), (19, 1, 77), (20, 1.5, 79), (21.5, .5, 77),
           (22, 2, 74), (24, 1, 72), (25, 1, 69), (26, 2, 72), (28, 3, 74), (31, 1, 72)]
    for b, d, m in mel:
        add_echo(s, b, i_flute(midi(m), s.secs(d) * 0.9), 0.2, 0.22, 0.3, every=0.5, fb=0.3, repeats=2)
    return s.render(rt=1.5, wet=0.4, bright=6000)


def sketch_abyss():
    """Abismo: enorme y lento. Re frigio, 80 BPM."""
    prog = ["Dm", "Dm", "Eb", "Eb", "Dm", "Dm", "Cm", "Eb"]
    s = Song2(80, 32, seed=43)
    k = sea_kit(s.rng)
    play_pad(s, prog, inst=i_tide, gain=0.09, lo=50, rev=0.6, dark=300, bright=1100, rate=0.12)
    for bar, ch in enumerate(prog):
        s.add(bar * 4, i_sub(midi(bass_note(ch, 38)), s.secs(4)), 0, 0.2, 0.1)
        s.add(bar * 4, k["heartbeat"], 0, 0.3, 0.1)
    notes = [(0, 3, 50), (3, 1, 51), (4, 4, 53), (8, 2, 51), (10, 2, 50), (12, 4, 46),
             (16, 3, 50), (19, 1, 53), (20, 4, 57), (24, 2, 55), (26, 2, 51), (28, 3.5, 51)]
    s.add(0, song_line(notes, s.beat), 0, 0.45, 0.6)
    for beat in (2, 10, 18, 26):
        add_echo(s, beat, k["sonar"], 0.3, 0.1, 0.4, every=1.5, fb=0.5, repeats=4, darken=4000)
    for _ in range(8):
        s.add(s.rng.uniform(0, 32), k["big_bubble"], s.rng.uniform(-0.7, 0.7), 0.12, 0.6)
    return s.render(rt=3.2, wet=0.65, bright=2500)


def sketch_lab():
    """Laboratorio de Darwin (sala de cambio): misterioso y curioso. Re menor, 6/8 lento."""
    prog = ["Dm", "Dm", "Bb", "Bb", "Gm", "Gm", "A", "A"]
    s = Song2(150, 48, seed=44, bpb=6)
    k = sea_kit(s.rng)
    mel = [(0, 3, 74), (3, 2, 77), (5, 1, 76), (6, 6, 74), (12, 3, 70), (15, 2, 74), (17, 1, 72), (18, 6, 70),
           (24, 3, 67), (27, 2, 70), (29, 1, 74), (30, 6, 72), (36, 3, 73), (39, 2, 76), (41, 1, 73), (42, 6, 69)]
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
    bubble_bed(s, 70, 0.05, 300, 1400)
    return s.render(rt=2.0, wet=0.5, bright=4000)


DEMOS = [
    ("Inst_ShellMarimba", demo_marimba, "Instrumentos", "Marimba de conchas",
     "Sustituye a las campanas del piso 1 como voz principal: golpe de madera hueca, cálido, con un chasquido de "
     "concha muy corto al principio. Arpegios sobre re menor, sol, do y re menor."),
    ("Inst_DeepSong", demo_song, "Instrumentos", "Canto de las profundidades",
     "Voz grave que se desliza de una nota a otra, con vibrato lento y algo de aliento, como un canto de cetáceo. "
     "Lo más reconocible del piso; sobre todo para el Abismo."),
    ("Inst_TidePad", demo_tide, "Instrumentos", "Pads que ondulan",
     "Acordes largos cuyo brillo y volumen suben y bajan despacio, como la marea (una ola cada ~5 s)."),
    ("Inst_SeaPercussion", demo_percussion, "Instrumentos", "Percusión de mar",
     "Primero cada pieza sola: quitina, piedra, tambor de madera grave, tambor de madera agudo, conchas, burbuja "
     "grande, tambor hondo (para el combate) y latido. Luego un ritmo de arrecife con todas."),
    ("Inst_LongEcho", demo_echo, "Instrumentos", "Eco largo",
     "Notas sueltas de marimba cuyo eco va y viene de un lado a otro, cada vez más oscuro, y un pulso de sonar "
     "al final. Da la sensación de agua abierta."),
    ("Inst_GlassHarmonica", demo_glass, "Instrumentos", "Armónica de cristal",
     "Para el laboratorio de Darwin: seno casi puro que entra despacio, con un leve temblor."),
    ("Sketch_Shallows", sketch_shallows, "Esbozos de franja (bucles)", "Someras · re dórico · 100 BPM",
     "Agua clara con rayos de luz: marimba brillante en arpegios, melodía de marimba con eco, pads claros, pocos "
     "graves, conchas suaves y burbujitas."),
    ("Sketch_Reef", sketch_reef, "Esbozos de franja (bucles)", "Arrecife · re menor natural · 108 BPM",
     "Lleno de vida: percusión de conchas, quitina y madera con síncopas, bajo pulsado, ostinato de marimba en "
     "3+3+2 y flauta con eco corto."),
    ("Sketch_Abyss", sketch_abyss, "Esbozos de franja (bucles)", "Abismo · re frigio · 80 BPM",
     "Enorme y lento: el canto de las profundidades delante, pads oscuros, subgrave, latido en cada compás, "
     "sonar con eco y burbujas grandes."),
    ("Sketch_Lab", sketch_lab, "Esbozos de franja (bucles)", "Laboratorio de Darwin · re menor · 6/8",
     "Sala de cambio: armónica de cristal, marimba suave en 6/8, la cápsula burbujeando y un tic de quitina como "
     "un reloj de laboratorio."),
]


# ================================================================== página

PAGE = """<!doctype html>
<html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Instrumentos del piso 2</title>
<style>
:root {{ --bg:#eef4f6; --card:#fff; --ink:#14262e; --soft:#56707a; --line:#d0dde2; --accent:#1f7a99; }}
@media (prefers-color-scheme: dark) {{ :root {{ --bg:#0a1a22; --card:#11262f; --ink:#e0eef3; --soft:#8eaab4; --line:#22404c; --accent:#5fc3e4; }} }}
* {{ box-sizing:border-box }}
body {{ margin:0; background:var(--bg); color:var(--ink); font:15px/1.45 system-ui,sans-serif; }}
main {{ max-width:1000px; margin:0 auto; padding:24px 16px 64px; }}
h1 {{ margin:0 0 4px; font-size:26px }} h2 {{ margin:32px 0 12px; font-size:19px; border-bottom:1px solid var(--line); padding-bottom:6px }}
.lead {{ color:var(--soft); margin:0 0 8px }}
.grid {{ display:grid; grid-template-columns:repeat(auto-fill,minmax(300px,1fr)); gap:12px }}
.card {{ background:var(--card); border:1px solid var(--line); border-radius:10px; padding:12px 14px; }}
.title {{ font-weight:600 }} .meta {{ color:var(--soft); font:12px ui-monospace,Consolas,monospace; margin:2px 0 6px }}
.desc {{ font-size:13.5px; color:var(--soft); margin:0 0 8px }}
audio {{ width:100% }}
</style></head><body><main>
<h1>Piso 2: muestrario de instrumentos</h1>
<p class="lead">Pruebas para elegir el sonido del océano cámbrico antes de escribir el encargo. Todo en re, como el resto
del juego; cada franja cambia el modo. Los esbozos son bucles: déjalos sonar dos vueltas para oír el empalme.</p>
{sections}
</main></body></html>
"""


def build_page(lengths):
    groups = {}
    for name, _, group, title, desc in DEMOS:
        if name not in lengths:
            continue
        groups.setdefault(group, []).append(
            f'<div class="card"><div class="title">{html.escape(title)}</div>'
            f'<div class="meta">{name}.ogg · {lengths[name]:.1f} s</div><p class="desc">{html.escape(desc)}</p>'
            f'<audio controls preload="none" {"loop " if name.startswith("Sketch") else ""}'
            f'src="muestrario-piso2/{name}.ogg"></audio></div>')
    sections = "".join(f'<h2>{html.escape(g)}</h2><div class="grid">{"".join(c)}</div>' for g, c in groups.items())
    (ROOT / "muestrario_piso2.html").write_text(PAGE.format(sections=sections), encoding="utf-8")


def main():
    only = set(sys.argv[1:])
    OUT.mkdir(exist_ok=True)
    lengths = {}
    for name, fn, *_ in DEMOS:
        path = OUT / f"{name}.ogg"
        if only and name not in only:
            if path.exists():
                lengths[name] = decoded_samples(path) / SR
            continue
        print(f"  {name} ...", end="", flush=True)
        x = fn()
        encode_ogg(x, path)
        lengths[name] = x.shape[1] / SR
        print(f" {lengths[name]:.1f} s, pico {20 * np.log10(np.max(np.abs(x))):.1f} dBFS")
    build_page(lengths)


if __name__ == "__main__":
    main()
