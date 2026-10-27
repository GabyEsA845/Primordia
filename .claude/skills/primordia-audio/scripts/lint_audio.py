"""Revisión automática del audio de Primordia (docs/multimedia-temp/audio).

Uso:
    python lint_audio.py [carpeta] [--solo PATRÓN ...] [--espectros CARPETA] [--tabla]

Sin carpeta revisa docs/multimedia-temp/audio. --solo limita la revisión de archivos a los nombres que
encajan con algún patrón (fnmatch sobre el nombre sin extensión, p. ej. 'SFX_Boss_Eurypterid*'); las
comprobaciones del conjunto (sonidos.json, Sounds.cs, capas) se hacen siempre. --espectros guarda por cada
archivo revisado un PNG con la forma de onda y el espectrograma (frecuencia logarítmica, 40 Hz–16 kHz) para
mirarlo con Read. --tabla imprime duración, pico y RMS de cada archivo revisado.

Comprueba:
  efectos (.wav): mono, 44 100 Hz y 16 bits; nombre SFX_<Categoría>_<Nombre>[_0N]; pico ≤ −1 dBFS (y aviso
    suave si queda muy por debajo); sin silencio al principio ni cola de silencio larga; sin chasquido al final;
    sin corriente continua; los _Loop enlazan sin salto; variantes numeradas seguidas y distintas; tamaño.
  música (.ogg): estéreo; pico; sonoridad de los bucles parecida entre sí; empalme de los bucles; los
    fragmentos terminan en silencio; las capas Music_<X>_Explore y Music_<X>_Combat duran lo mismo; tamaño total.
  conjunto: cada archivo de sonidos.json existe y no sobra ninguno; cada nombre de Sounds.cs y cada pista
    "Music_…" del código está en sonidos.json.
"""
import fnmatch
import json
import re
import subprocess
import sys
import wave
from pathlib import Path

import numpy as np

SR = 44100
PEAK = 10 ** (-1 / 20)
REPO = Path(__file__).resolve().parents[4]
SCRIPTS = REPO / "Assets/_Primordia/Scripts"

warns, looks = [], []


def warn(name, msg):
    warns.append(f"  ! {name}: {msg}")


def look(name, msg):
    looks.append(f"  ? {name}: {msg}")


def db(v):
    return 20 * np.log10(max(float(v), 1e-12))


def rms(x):
    return float(np.sqrt(np.mean(np.square(x)))) if x.size else 0.0


# ------------------------------------------------------------------ lectura

def read_wav(path):
    with wave.open(str(path)) as w:
        ch, sw, sr, nf = w.getnchannels(), w.getsampwidth(), w.getframerate(), w.getnframes()
        raw = w.readframes(nf)
    if sw != 2:
        return None, ch, sr, sw
    x = np.frombuffer(raw, "<i2").astype(float) / 32768
    return x.reshape(-1, ch).T, ch, sr, sw


def ffmpeg():
    import imageio_ffmpeg
    return imageio_ffmpeg.get_ffmpeg_exe()


def read_ogg(path):
    exe = ffmpeg()
    probe = subprocess.run([exe, "-hide_banner", "-i", str(path)], capture_output=True, text=True,
                           encoding="utf-8", errors="replace").stderr
    m = re.search(r"Audio: \w+, (\d+) Hz, (\w+)", probe)
    sr, layout = (int(m.group(1)), m.group(2)) if m else (0, "?")
    raw = subprocess.run([exe, "-loglevel", "error", "-i", str(path), "-f", "s16le", "-ac", "2", "-"],
                         capture_output=True, check=True).stdout
    x = np.frombuffer(raw, "<i2").astype(float) / 32768
    return x.reshape(-1, 2).T, layout, sr


# ------------------------------------------------------------- comprobaciones

def seam_jump(x):
    """Salto del final al principio comparado con los saltos normales entre muestras seguidas."""
    d = np.abs(np.diff(x))
    typical = np.percentile(d, 99) if d.size else 0
    return abs(x[0] - x[-1]), typical


def check_sfx(path, info):
    name = path.stem
    x, ch, sr, sw = read_wav(path)
    if x is None:
        warn(name, f"{sw * 8} bits (deben ser 16)")
        return
    if ch != 1:
        warn(name, f"{ch} canales (los efectos van en mono)")
    if sr != SR:
        warn(name, f"{sr} Hz (deben ser 44 100)")
    x = x.mean(axis=0)
    loop = "_Loop" in name
    if not re.match(r"^SFX_[A-Z][A-Za-z0-9]*(_[A-Z0-9][A-Za-z0-9]*)+$", name):
        warn(name, "nombre fuera del patrón SFX_<Categoría>_<Nombre>[_0N]")
    peak = np.max(np.abs(x)) if x.size else 0
    info[name] = dict(dur=len(x) / SR, peak=peak, rms=rms(x), x=x, loop=loop)
    if peak > PEAK * 1.006:
        warn(name, f"pico {db(peak):.2f} dBFS (máximo −1)")
    elif peak < PEAK * 10 ** (-2 / 20):
        look(name, f"pico {db(peak):.1f} dBFS: no está normalizado (finish_sfx lo deja en −1)")
    if abs(np.mean(x)) > 0.01:
        warn(name, f"corriente continua {np.mean(x):+.3f}")
    if loop:
        jump, typical = seam_jump(x)
        if jump > max(3 * typical, 0.01):
            warn(name, f"el bucle salta {jump:.3f} al volver al principio (lo normal entre muestras: {typical:.3f})")
        return
    if np.max(np.abs(x[:64])) < 1e-3:
        warn(name, "empieza en silencio")
    if abs(x[-1]) > 0.01:
        warn(name, f"termina en {x[-1]:+.3f}: chasquido (falta el fundido de 5 ms)")
    quiet = np.abs(x) < peak * 10 ** (-60 / 20)
    tail = len(x) - (np.max(np.nonzero(~quiet)) + 1 if (~quiet).any() else 0)
    if tail > 0.15 * SR:
        look(name, f"{tail / SR:.2f} s de silencio al final")
    if path.stat().st_size > 300_000:
        look(name, f"{path.stat().st_size / 1000:.0f} KB (objetivo ≤ 200–300 KB)")


def check_variants(info):
    groups = {}
    for name in info:
        m = re.match(r"^(.*)_(\d\d)$", name)
        if m:
            groups.setdefault(m.group(1), []).append((int(m.group(2)), name))
    for base, items in groups.items():
        nums = sorted(i for i, _ in items)
        if nums != list(range(1, len(nums) + 1)):
            warn(base, f"variantes {nums}: deben ir de _01 a _{len(nums):02d} sin huecos")
        if len(items) == 1:
            look(base, "una sola variante numerada (con una, el archivo va sin _01)")
        xs = [info[n]["x"] for _, n in sorted(items)]
        for i in range(len(xs)):
            for j in range(i + 1, len(xs)):
                a, b = xs[i], xs[j]
                if len(a) == len(b) and np.max(np.abs(a - b)) < 1e-3:
                    warn(base, f"las variantes {i + 1} y {j + 1} son iguales")


def check_music(path, info, loops):
    name = path.stem
    x, layout, sr = read_ogg(path)
    if layout != "stereo":
        warn(name, f"{layout} (la música va en estéreo)")
    if sr != SR:
        warn(name, f"{sr} Hz (deben ser 44 100)")
    peak = np.max(np.abs(x))
    loop = loops.get(name, True)
    info[name] = dict(dur=x.shape[1] / SR, peak=peak, rms=rms(x), x=x.mean(axis=0), loop=loop, samples=x.shape[1])
    # El Vorbis rebasa un poco el pico del original en los tramos densos: unas pocas muestras no se oyen.
    clipped = int(np.sum(np.abs(x) >= 0.999))
    if clipped > 20:
        warn(name, f"{clipped} muestras saturan a 0 dBFS (renderizar con más margen, p. ej. ceiling −1,5 dBFS)")
    elif peak > PEAK * 10 ** (0.3 / 20):
        look(name, f"pico {db(peak):.2f} dBFS tras el Vorbis ({clipped} muestras a 0 dBFS)")
    if loop:
        for c in range(2):
            jump, typical = seam_jump(x[c])
            if jump > max(4 * typical, 0.02):
                warn(name, f"el bucle salta {jump:.3f} en el canal {c} (lo normal: {typical:.3f})")
    else:
        end = x[:, -int(0.05 * SR):]
        if db(rms(end)) > -50:
            look(name, f"no termina en silencio (los últimos 50 ms a {db(rms(end)):.0f} dBFS)")


def check_music_set(info, music_bytes):
    layers = {}
    for name, i in info.items():
        m = re.match(r"^Music_(.+)_(Explore|Combat)$", name)
        if m and "samples" in i:
            layers.setdefault(m.group(1), {})[m.group(2)] = i["samples"]
    for key, d in layers.items():
        if len(d) == 2 and d["Explore"] != d["Combat"]:
            warn(f"Music_{key}", f"las capas Explore ({d['Explore']}) y Combat ({d['Combat']}) no duran lo mismo en muestras")
    loops = {n: db(i["rms"]) for n, i in info.items() if n.startswith("Music_") and i["loop"]}
    if len(loops) >= 3:
        med = float(np.median(list(loops.values())))
        for n, v in loops.items():
            if abs(v - med) > 3:
                look(n, f"RMS {v:.1f} dBFS, {v - med:+.1f} dB respecto a la mediana de los bucles ({med:.1f})")
    floors = {m.group(1) for n in info for m in [re.match(r"^Music_Floor(\d+)_", n)] if m}
    limit = 12 * max(1, len(floors))
    if music_bytes / 1e6 > limit:
        look("Music/", f"{music_bytes / 1e6:.1f} MB en total (objetivo ≤ 12 MB por piso: {limit} MB)")


def check_manifest(folder, sfx_names, music_names):
    path = folder / "sonidos.json"
    if not path.exists():
        warn("sonidos.json", "no existe (lo escribe generar_audio.py)")
        return {}, set()
    data = json.loads(path.read_text(encoding="utf-8"))
    listed_sfx = {f[:-4] for s in data["sounds"] for f in s["files"]}
    listed_mus = {t["file"][:-4] for t in data["tracks"]}
    for n in sorted(listed_sfx - sfx_names):
        warn(n, "está en sonidos.json pero no en SFX/")
    for n in sorted(sfx_names - listed_sfx):
        warn(n, "está en SFX/ pero no en sonidos.json (sobra o falta regenerar)")
    for n in sorted(listed_mus - music_names):
        warn(n, "está en sonidos.json pero no en Music/")
    for n in sorted(music_names - listed_mus):
        warn(n, "está en Music/ pero no en sonidos.json")
    ids = {s["id"] for s in data["sounds"]} | {t["id"] for t in data["tracks"]}
    loops = {t["id"]: t["loop"] for t in data["tracks"]}
    return loops, ids


def check_code(ids):
    sounds_cs = SCRIPTS / "Audio/Sounds.cs"
    if sounds_cs.exists():
        for const, value in re.findall(r"const string (\w+) = \"([^\"]+)\"", sounds_cs.read_text(encoding="utf-8")):
            if value not in ids:
                warn(f"Sounds.{const}", f"pide \"{value}\", que no está en sonidos.json")
    used = set()
    for cs in SCRIPTS.rglob("*.cs"):
        used |= set(re.findall(r"\"(Music_[A-Za-z0-9_]+)\"", cs.read_text(encoding="utf-8", errors="replace")))
    for n in sorted(used - ids):
        warn(n, "lo usa el código pero no está en sonidos.json")


# ------------------------------------------------------------- espectrogramas

def spectrogram(name, i, out):
    from PIL import Image, ImageDraw
    from scipy import signal
    x = i["x"]
    W, HW, HS = 640, 60, 200
    img = Image.new("RGB", (W, HW + HS + 18), (16, 20, 28))
    d = ImageDraw.Draw(img)
    # forma de onda: envolvente de pico por columna
    cols = np.array_split(np.abs(x), W)
    for c, seg in enumerate(cols):
        h = (seg.max() if seg.size else 0) / PEAK * (HW / 2 - 2)
        d.line([(c, 18 + HW / 2 - h), (c, 18 + HW / 2 + h)], fill=(110, 200, 140))
    # espectrograma con frecuencia logarítmica
    nper = 512 if len(x) < SR // 2 else 2048
    f, t, Z = signal.stft(x, SR, nperseg=nper, noverlap=nper * 3 // 4)
    mag = 20 * np.log10(np.abs(Z) + 1e-9)
    mag = np.clip((mag - (mag.max() - 80)) / 80, 0, 1)
    fy = np.exp(np.linspace(np.log(16000), np.log(40), HS))
    rows = np.clip(np.searchsorted(f, fy), 0, len(f) - 1)
    colx = np.clip((np.arange(W) / W * mag.shape[1]).astype(int), 0, mag.shape[1] - 1)
    m = mag[rows][:, colx]
    stops = np.array([[16, 20, 28], [70, 30, 110], [200, 70, 70], [250, 180, 60], [255, 250, 200]], float)
    k = m * (len(stops) - 1)
    lo = np.floor(k).astype(int).clip(0, len(stops) - 2)
    rgb = stops[lo] + (stops[lo + 1] - stops[lo]) * (k - lo)[..., None]
    img.paste(Image.fromarray(rgb.astype(np.uint8)), (0, 18 + HW))
    for hz in (100, 1000, 10000):
        y = 18 + HW + int(np.interp(np.log(hz), [np.log(40), np.log(16000)], [HS - 1, 0]))
        d.line([(0, y), (W, y)], fill=(90, 90, 110))
        d.text((3, y - 11), f"{hz // 1000} kHz" if hz >= 1000 else f"{hz} Hz", fill=(170, 170, 190))
    d.text((3, 3), f"{name}  {i['dur']:.2f} s  pico {db(i['peak']):.1f} dBFS  RMS {db(i['rms']):.1f} dBFS"
                   + ("  (bucle)" if i["loop"] else ""), fill=(230, 230, 240))
    img.save(out / f"{name}.png")


# --------------------------------------------------------------------- main

def main(argv):
    sys.stdout.reconfigure(encoding="utf-8")  # la consola de Windows no sabe escribir «−» ni «≤»
    args, solo, spec_dir, table = [], [], None, False
    it = iter(argv)
    for a in it:
        if a == "--solo":
            solo.append(next(it))
        elif a == "--espectros":
            spec_dir = Path(next(it))
        elif a == "--tabla":
            table = True
        elif a in ("-h", "--help"):
            print(__doc__)
            return 0
        else:
            args.append(a)
    folder = Path(args[0]) if args else REPO / "docs/multimedia-temp/audio"
    sfx_files = sorted((folder / "SFX").glob("*.wav"))
    music_files = sorted((folder / "Music").glob("*.ogg"))
    pick = (lambda p: any(fnmatch.fnmatch(p.stem, s) for s in solo)) if solo else (lambda p: True)

    loops, ids = check_manifest(folder, {p.stem for p in sfx_files}, {p.stem for p in music_files})
    check_code(ids)
    info = {}
    for p in sfx_files:
        if pick(p):
            check_sfx(p, info)
    check_variants(info)
    for p in music_files:
        if pick(p):
            check_music(p, info, loops)
    check_music_set(info, sum(p.stat().st_size for p in music_files))

    if table:
        print(f"{'archivo':48} {'dur':>6} {'pico':>6} {'RMS':>6}")
        for n, i in info.items():
            print(f"{n:48} {i['dur']:6.2f} {db(i['peak']):6.1f} {db(i['rms']):6.1f}")
        print()
    if spec_dir:
        spec_dir.mkdir(parents=True, exist_ok=True)
        for n, i in info.items():
            spectrogram(n, i, spec_dir)
        print(f"Espectrogramas: {spec_dir} ({len(info)} archivos)")
    for l in warns + looks:
        print(l)
    print(f"\nRevisados: {sum(1 for n in info if n.startswith('SFX_'))} efectos y "
          f"{sum(1 for n in info if n.startswith('Music_'))} pistas · {len(warns)} avisos, {len(looks)} a revisar")
    return 1 if warns else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
