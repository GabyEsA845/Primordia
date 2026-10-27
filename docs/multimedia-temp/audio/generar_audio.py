"""Genera toda la música y los efectos de sonido de Primordia según PROMPT.md (piso 1) y PROMPT_piso2.md.

Uso: python generar_audio.py [nombre ...]
Sin argumentos genera todo; con nombres (p. ej. Player_Shot Music_Shop) solo esos.
Salida: SFX/*.wav, Music/*.ogg, muestrario.html y muestrario/ (bucles repetidos dos
veces para escuchar el empalme)."""

import html
import json
import re
import subprocess
import sys
import tempfile
import zlib
from pathlib import Path

import imageio_ffmpeg
import numpy as np

from efectos import SFX
from musica import MUSIC
import efectos_piso2  # noqa: F401,E402  (registran sus sonidos en SFX y MUSIC al importarse)
import musica_piso2  # noqa: F401,E402
from sintesis import SR, finish_sfx, write_wav

ROOT = Path(__file__).resolve().parent
OUT_SFX = ROOT / "SFX"
OUT_MUSIC = ROOT / "Music"
OUT_PREVIEW = ROOT / "muestrario"
FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()

# Sonidos que se repiten mucho en el juego: intervalo real entre repeticiones (ms)
RHYTHM = {
    "SFX_Player_Shot": 200, "SFX_Mouth_Stinger_Shot": 250, "SFX_Mouth_Mandibles_Bite": 400,
    "SFX_Mouth_Spores_Puff": 800, "SFX_Boss_Virus_Spiral": 70, "SFX_Boss_Amoeba_Barrage": 90,
    "SFX_Biomass_Pickup_Small": 110, "SFX_Enemy_Hit": 200, "SFX_Projectile_Impact": 150,
    "SFX_Limbs_Hyphae_Trail": 300,
    # piso 2
    "SFX_Mouth_Cnidocytes_Shot": 333, "SFX_Mouth_JawPlates_Bite": 500, "SFX_Organ_BroodSac_LarvaBite": 180,
    "SFX_Limbs_Hallucigenia_Strike": 250, "SFX_Skin_Trilobite_Bounce": 300,
}


# ============================================================ lectura del prompt

def read_prompt():
    """Filas de las tablas de los encargos (PROMPT*.md): nombre → (categoría, variantes, duración, cuándo, descripción)."""
    rows, category = {}, ""
    lines = [l for f in sorted(ROOT.glob("PROMPT*.md")) for l in f.read_text(encoding="utf-8").splitlines()]
    for line in lines:
        if line.startswith("## ") or line.startswith("### "):
            category = line.lstrip("# ").strip()
        m = re.match(r"^\| `([A-Za-z0-9_]+)(?:\.(?:wav|ogg))?` \|(.*)\|\s*$", line)
        if not m:
            continue
        name = m.group(1)
        cells = [c.strip() for c in m.group(2).split("|")]
        if len(cells) == 4 and cells[0].isdigit():  # efectos: var, duración, cuándo, descripción
            rows[name] = dict(cat=category, var=int(cells[0]), dur=cells[1], when=cells[2], desc=cells[3])
        else:  # música y ambiente: uso, duración, ambiente/notas
            rows[name] = dict(cat=category, var=1, dur=cells[1], when=cells[0], desc=cells[2])
    return rows


def md_inline(text):
    text = html.escape(text)
    text = re.sub(r"\*\*(.+?)\*\*", r"<strong>\1</strong>", text)
    return re.sub(r"`(.+?)`", r"<code>\1</code>", text)


# ================================================================== generación

def rng_for(name, v):
    return np.random.default_rng(zlib.crc32(f"{name}#{v}".encode()))


def sfx_files(name, spec):
    base = f"SFX_{name}"
    if spec["variants"] == 1:
        return [(base, 0)]
    return [(f"{base}_{v + 1:02d}", v) for v in range(spec["variants"])]


def encode_ogg(x, path):
    with tempfile.TemporaryDirectory() as tmp:
        wav = Path(tmp) / "tmp.wav"
        write_wav(wav, x)
        subprocess.run([FFMPEG, "-y", "-loglevel", "error", "-i", str(wav), "-c:a", "libvorbis",
                        "-q:a", "5", str(path)], check=True)


def decoded_samples(path):
    raw = subprocess.run([FFMPEG, "-loglevel", "error", "-i", str(path), "-f", "s16le", "-ac", "1", "-"],
                         check=True, capture_output=True).stdout
    return len(raw) // 2


def generate(only=None):
    OUT_SFX.mkdir(exist_ok=True)
    OUT_MUSIC.mkdir(exist_ok=True)
    OUT_PREVIEW.mkdir(exist_ok=True)
    info = {}
    for name, spec in SFX.items():
        for fname, v in sfx_files(name, spec):
            if only and name not in only and fname not in only:
                continue
            x = finish_sfx(spec["fn"](rng_for(name, v), v), spec["loop"])
            write_wav(OUT_SFX / f"{fname}.wav", x)
            if spec["loop"]:
                write_wav(OUT_PREVIEW / f"{fname}_x2.wav", np.concatenate([x, x]))
            info[fname] = dict(samples=len(x), peak=float(np.max(np.abs(x))),
                               start=float(np.max(np.abs(x[:64]))), loop=spec["loop"])
            print(f"  {fname}.wav  {len(x) / SR:.2f} s")
    for name, (fn, loop, _) in MUSIC.items():
        if only and name not in only:
            continue
        print(f"  {name}.ogg ...", end="", flush=True)
        x = fn()
        encode_ogg(x, OUT_MUSIC / f"{name}.ogg")
        if loop:
            encode_ogg(np.concatenate([x, x], axis=1), OUT_PREVIEW / f"{name}_x2.ogg")
        info[name] = dict(samples=x.shape[1], peak=float(np.max(np.abs(x))),
                          decoded=decoded_samples(OUT_MUSIC / f"{name}.ogg"))
        print(f" {x.shape[1] / SR:.2f} s")
    return info


# ================================================================== muestrario

PAGE = """<!doctype html>
<html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Muestrario de audio</title>
<style>
:root {{ --bg:#f4f6f2; --card:#fff; --ink:#1d2a24; --soft:#5d6d65; --line:#d6ddd8; --accent:#2f8f5b; }}
@media (prefers-color-scheme: dark) {{ :root {{ --bg:#0f1a16; --card:#16261f; --ink:#e4efe8; --soft:#93a89c; --line:#2a3d34; --accent:#6fd39a; }} }}
* {{ box-sizing:border-box }}
body {{ margin:0; background:var(--bg); color:var(--ink); font:15px/1.45 system-ui,sans-serif; }}
main {{ max-width:1100px; margin:0 auto; padding:24px 16px 64px; }}
h1 {{ margin:0 0 4px; font-size:26px }} h2 {{ margin:36px 0 12px; font-size:19px; border-bottom:1px solid var(--line); padding-bottom:6px }}
.lead {{ color:var(--soft); margin:0 0 8px }}
.grid {{ display:grid; grid-template-columns:repeat(auto-fill,minmax(330px,1fr)); gap:12px }}
.card {{ background:var(--card); border:1px solid var(--line); border-radius:10px; padding:12px 14px; }}
.name {{ font:600 13px ui-monospace,Consolas,monospace; word-break:break-all }}
.meta {{ color:var(--soft); font-size:12.5px; margin:2px 0 6px }}
.when {{ font-size:13px; margin:0 0 4px }} .desc {{ font-size:13px; color:var(--soft); margin:0 0 8px }}
audio {{ width:100%; height:34px; margin-top:4px }}
.row {{ display:flex; gap:8px; flex-wrap:wrap; margin-top:6px }}
button {{ background:transparent; color:var(--accent); border:1px solid var(--accent); border-radius:6px; padding:3px 9px; font-size:12.5px; cursor:pointer }}
button:hover {{ background:var(--accent); color:var(--card) }}
code {{ font-size:12px }}
</style></head><body><main>
<h1>Primordia: muestrario de audio</h1>
<p class="lead">{summary}</p>
<p class="lead">«Vol.» es el volumen propuesto para el juego (0 a 1) y su grupo del mezclador. En los bucles, «Empalme ×2» reproduce el archivo dos veces seguidas para oír el salto. En los sonidos frecuentes, «Ritmo real» los repite 20 veces al ritmo del juego alternando variantes.</p>
{sections}
</main>
<script>
function rhythm(files, ms, vol) {{
  let i = 0;
  const id = setInterval(() => {{
    const a = new Audio(files[i % files.length]); a.volume = vol; a.play();
    if (++i >= 20) clearInterval(id);
  }}, ms);
}}
</script>
</body></html>
"""


def card(title, meta, row, players, buttons=""):
    return (f'<div class="card"><div class="name">{html.escape(title)}</div><div class="meta">{meta}</div>'
            f'<p class="when">{md_inline(row["when"])}</p><p class="desc">{md_inline(row["desc"])}</p>'
            f'{players}<div class="row">{buttons}</div></div>')


def build_page(rows, info):
    groups = {}
    for name, spec in SFX.items():
        key = f"SFX_{name}"
        files = [f for f, _ in sfx_files(name, spec)]
        durs = [info[f]["samples"] / SR for f in files if f in info]
        if not durs:
            continue
        row = rows.get(key, dict(cat="Sin categoría", when="", desc=""))
        dur = f"{min(durs):.2f} s" if len(set(round(d, 2) for d in durs)) == 1 else f"{min(durs):.2f}–{max(durs):.2f} s"
        meta = f"{len(files)} variante{'s' if len(files) > 1 else ''} · {dur} · Vol. {spec['vol']:.2f} ({spec['group']})"
        players = "".join(f'<audio controls preload="none" src="SFX/{f}.wav"></audio>' for f in files)
        buttons = ""
        if spec["loop"]:
            buttons += f'<button onclick="new Audio(\'muestrario/{files[0]}_x2.wav\').play()">Empalme ×2</button>'
        if key in RHYTHM:
            lst = ",".join(f"'SFX/{f}.wav'" for f in files)
            buttons += f'<button onclick="rhythm([{lst}],{RHYTHM[key]},{spec["vol"]})">Ritmo real</button>'
        groups.setdefault(row["cat"], []).append(card(key + (".wav" if len(files) == 1 else "_0N.wav"), meta, row, players, buttons))
    for name, (fn, loop, vol) in MUSIC.items():
        if name not in info:
            continue
        row = rows.get(name, dict(cat="Música", when="", desc=""))
        meta = f"{info[name]['samples'] / SR:.2f} s · {'bucle' if loop else 'sin bucle'} · Vol. {vol:.2f} (Music)"
        players = f'<audio controls preload="none" src="Music/{name}.ogg"></audio>'
        buttons = f'<button onclick="new Audio(\'muestrario/{name}_x2.ogg\').play()">Empalme ×2</button>' if loop else ""
        groups.setdefault(row["cat"], []).append(card(name + ".ogg", meta, row, players, buttons))
    order = ["Música", "Ambiente"] + [c for c in groups if c not in ("Música", "Ambiente")]
    sections = "".join(f'<h2>{html.escape(c)}</h2><div class="grid">{"".join(groups[c])}</div>' for c in order if c in groups)
    n_sfx = sum(1 for f in OUT_SFX.glob("*.wav"))
    n_mus = sum(1 for f in OUT_MUSIC.glob("*.ogg"))
    size = lambda d, pat: sum(f.stat().st_size for f in d.glob(pat)) / 1e6
    summary = (f"{n_sfx} efectos .wav ({size(OUT_SFX, '*.wav'):.1f} MB) y {n_mus} pistas .ogg "
               f"({size(OUT_MUSIC, '*.ogg'):.1f} MB), generados por <code>generar_audio.py</code>.")
    (ROOT / "muestrario.html").write_text(PAGE.format(summary=summary, sections=sections), encoding="utf-8")


# ================================================================ comprobaciones

def check(rows, info, full):
    problems = []
    expected = {}
    for name, row in rows.items():
        expected[name] = row["var"]
    produced = {f"SFX_{k}": s["variants"] for k, s in SFX.items()}
    produced.update({k: 1 for k in MUSIC})
    if full:
        for name in expected.keys() - produced.keys():
            problems.append(f"falta en el script: {name}")
        for name in produced.keys() - expected.keys():
            problems.append(f"no está en el prompt: {name}")
        for name in expected.keys() & produced.keys():
            if expected[name] != produced[name]:
                problems.append(f"{name}: {produced[name]} variantes, el prompt pide {expected[name]}")
        listed = {f"{f}.wav" for k, s in SFX.items() for f, _ in sfx_files(k, s)} | {f"{k}.ogg" for k in MUSIC}
        on_disk = {f.name for f in OUT_SFX.glob("*")} | {f.name for f in OUT_MUSIC.glob("*")}
        for extra in sorted(on_disk - listed):
            problems.append(f"archivo de más en la salida: {extra}")
    for name, i in info.items():
        if i["peak"] > 0.8913 + 1e-4:
            problems.append(f"{name}: pico {20 * np.log10(i['peak']):.2f} dBFS")
        if "start" in i and not i["loop"] and i["start"] < 1e-3:
            problems.append(f"{name}: empieza en silencio")
        if "decoded" in i and i["decoded"] != i["samples"]:
            problems.append(f"{name}: el .ogg decodifica {i['decoded']} muestras en lugar de {i['samples']}")
        # «unos 200 KB»: los efectos de 2,5–3 s que pide el prompt ya ocupan 220–265 KB en WAV mono
        if "SFX_" in name and i["samples"] * 2 > 300_000 and "Loop" not in name:
            problems.append(f"{name}: más de 300 KB")
    a, b = info.get("Music_Floor1_Explore"), info.get("Music_Floor1_Combat")
    if a and b and a["samples"] != b["samples"]:
        problems.append("las dos capas del piso 1 no duran lo mismo")
    fin = info.get("Music_Boss_Amoeba_Final")
    if fin and fin["samples"] != 30 * SR:
        problems.append(f"Music_Boss_Amoeba_Final dura {fin['samples'] / SR:.4f} s")
    return problems


def write_manifest():
    """sonidos.json: lo lee el menú Primordia > Audio > Importar audio (volumen, grupo y bucle de cada sonido)."""
    sounds = [dict(id=name, files=[f"{f}.wav" for f, _ in sfx_files(name, spec)], volume=spec["vol"],
                   group=spec["group"], loop=spec["loop"]) for name, spec in SFX.items()]
    tracks = [dict(id=name, file=f"{name}.ogg", volume=vol, loop=loop) for name, (_, loop, vol) in MUSIC.items()]
    (ROOT / "sonidos.json").write_text(json.dumps(dict(sounds=sounds, tracks=tracks), indent=1, ensure_ascii=False),
                                       encoding="utf-8")


def main():
    only = set(sys.argv[1:]) or None
    rows = read_prompt()
    print("Generando…")
    info = generate(only)
    write_manifest()
    build_page(rows, {**info, **{p.stem: dict(samples=_wav_len(p)) for p in OUT_SFX.glob('*.wav') if p.stem not in info},
                      **{p.stem: dict(samples=_ogg_len(p)) for p in OUT_MUSIC.glob('*.ogg') if p.stem not in info}})
    problems = check(rows, info, only is None)
    sfx_n, mus_n = len(list(OUT_SFX.glob("*.wav"))), len(list(OUT_MUSIC.glob("*.ogg")))
    sfx_mb = sum(f.stat().st_size for f in OUT_SFX.glob("*.wav")) / 1e6
    mus_mb = sum(f.stat().st_size for f in OUT_MUSIC.glob("*.ogg")) / 1e6
    print(f"\nSFX/: {sfx_n} archivos, {sfx_mb:.2f} MB · Music/: {mus_n} archivos, {mus_mb:.2f} MB")
    print("Comprobaciones: " + ("todo bien" if not problems else f"{len(problems)} avisos"))
    for p in problems:
        print("  - " + p)


def _wav_len(p):
    import wave
    with wave.open(str(p)) as w:
        return w.getnframes()


def _ogg_len(p):
    return decoded_samples(p)


if __name__ == "__main__":
    main()
