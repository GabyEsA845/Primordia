"""
generar_mutaciones_piso1.py - Primordia: Hifas, Camuflaje y Vacuola digestiva.

Las 3 mutaciones nuevas del piso 1 (completan los linajes del Moho, el Cuesco de lobo,
el Calamar, el Escorpión y la Tortuga):
  Skin/   Skin_Camouflage_*        piel moteada de cromatóforos (sustituye a la célula)
  Limbs/  Limbs_Hyphae_*           filamentos de hifa que se arrastran detrás de la célula
  Organ/  Organ_DigestiveVacuole_* vacuola ámbar con comida dentro; Activate: digiere y cura
  Icons/  Icon_<mutación>_16/32
  Status/ Suspect ("?" del enemigo que sospecha) y Scared (asustado por el Pánico)
  Effects/ Mycelium (mancha de micelio, en bucle) y Camouflage_Hide (destello al ocultarse)

Reutiliza generar_evoluciones.py y, a través de él, los demás generadores (célula,
paletas, contorno #181425, parámetros de cada cuadro, iconos).

Uso:
  python generar_mutaciones_piso1.py [carpeta_multimedia_temp] [carpeta_salida]
"""
import importlib.util
import math
import os
import sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, '..')
OUT = sys.argv[2] if len(sys.argv) > 2 else HERE

_argv = sys.argv
sys.argv = ['generar_evoluciones.py', ROOT, os.devnull]
_spec = importlib.util.spec_from_file_location('generar_evoluciones', os.path.join(ROOT, 'evolutions', 'generar_evoluciones.py'))
E = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(E)
sys.argv = _argv
M = E.M
U = E.U

C, CA, OUTL = M.C, M.CA, M.OUTL
Canvas, body, features, rot = M.Canvas, M.body, M.features, M.rot
shade_v, blob, thick = M.shade_v, M.blob, M.thick
Cv, ell = U.Cv, U.ell
GREEN, RED, WHITE, PALE = M.GREEN, M.RED, M.WHITE, M.PALE
LIME, PURP = M.LIME, M.PURP
FACINGS, FRAMES, PANIMS = M.FACINGS, M.FRAMES, M.PANIMS
BG = C('#16352e')


# ========================================================== 1. CAMUFLAJE (Piel)
CAMO_R = (7.0, 6.5)                      # un poco mayor que la célula; colisión 0,45 y Boca a 7 px
CAMO_DARK = ['#123024', '#1a4232', '#245a40']
CAMO_OLIVE = ['#3e4c1c', '#5a6a28', '#7a8a36']
# manchas (x, y, radio en px, tipo) en coordenadas del cuerpo: giran con él al rodar
CAMO_SPOTS = [(-0.45, -0.35, 1.7, 'd'), (0.38, -0.5, 1.3, 'o'), (0.52, 0.22, 1.6, 'd'),
              (-0.15, 0.48, 1.4, 'o'), (-0.68, 0.18, 1.1, 'o'), (0.08, -0.02, 1.0, 'd'), (0.2, 0.6, 0.9, 'd')]


def camouflage(facing='side', squash=0.0, bob=0, lean=0.0, pal=GREEN, angle=0.0, eyes='open', grow=0.0, n=24):
    cv = Canvas(n)
    cx, cy, rx, ry = M.geom(n, *CAMO_R, squash, bob, grow, maxr=n / 2 - 1.6)
    body(cv, cx, cy, rx, ry, pal, lean)
    if pal is GREEN:
        rim = 1 - 1.2 / min(rx, ry)
        for (x, y) in list(cv.px):
            px, py = x + .5, y + .5
            t = (cy - py) / ry
            sx = px - cx - lean * max(0, t)
            nx, ny = sx / rx, (py - cy) / ry
            if math.hypot(nx, ny) > rim:                  # el borde queda verde
                continue
            ux, uy = rot(nx, ny, -angle)
            for (sx_, sy_, r, kind) in CAMO_SPOTS:
                if math.hypot((ux - sx_) * rx, (uy - sy_) * ry) <= r:
                    v = shade_v(nx, ny)
                    ramp = CAMO_DARK if kind == 'd' else CAMO_OLIVE
                    cv.px[(x, y)] = C(ramp[2 if v > 0.4 else 1 if v > -0.1 else 0])
                    break
    features(cv, cx + lean * 0.5, cy, rx, ry, pal, facing, angle, eyes)
    cv.outline()
    return cv


def build_skin():
    anims = M.skin_anims(lambda **k: camouflage(**k))
    anims['Death'] = M.skin_death(lambda *a, **k: camouflage(*a, **k), 'spike')
    return anims


# ============================================================ 2. HIFAS (Extremidades)
HYPHA = ['#5a4e3c', '#9a8a6c', '#d8ccae', '#fff8e8']


def hypha(cv, root, ang, length, phase, wave=0.35, branch=True, alpha=235):
    """Filamento que ondula; a media altura le sale una rama más corta."""
    x, y = root
    steps = max(2, int(length / 0.5))
    for s in range(steps):
        t = s / steps
        a = ang + wave * math.sin(phase + t * 5.0)
        x += math.cos(a) * 0.5
        y += math.sin(a) * 0.5
        cv.put(x, y, CA(HYPHA[2] if t < 0.55 else HYPHA[1], alpha))
        if branch and s == steps // 2:
            hypha(cv, (x, y), a + (0.75 if math.sin(phase) > 0 else -0.75), length * 0.4, phase + 1.3, wave, False, alpha)
    cv.put(x, y, CA(HYPHA[3], 255))                           # punta clara


def hyphae_frame(facing, p, anim, f):
    cv = Canvas(M.LN)
    cx, cy, rx, ry = M.cell_geom(p)
    moving = anim == 'Move'
    ph = f * math.pi / 2
    grow = [0.0, 0.6, 1.2, 0.6][f] if moving else [0.0, 0.3, 0.5, 0.3][f]
    if facing == 'side':
        # se arrastran detrás (a la izquierda) y un par por debajo
        for k, (dy, a, L) in enumerate(((-2.5, math.pi + 0.35, 9.0), (0.0, math.pi + 0.05, 10.5),
                                         (2.5, math.pi - 0.3, 9.0), (4.0, math.pi / 2 + 0.6, 6.0))):
            root = (cx - rx * 0.6, cy + dy * ry / 6.0) if k < 3 else (cx - 2, cy + ry * 0.7)
            hypha(cv, root, a, L + grow * (1.5 if moving else 0.8), ph + k * 1.1, 0.45 if moving else 0.3)
    else:
        n_ = 6
        for k in range(n_):
            a = (math.pi / 2 if facing == 'down' else -math.pi / 2) + (k - (n_ - 1) / 2) * 0.62
            if facing == 'up':
                a += math.pi                                       # de espaldas, hacia abajo también se ven
            root = (cx + math.cos(a) * rx * 0.7, cy + math.sin(a) * ry * 0.7)
            hypha(cv, root, a, 7.5 + grow * (1.2 if moving else 0.6) + (k % 2) * 1.5, ph + k, 0.4 if moving else 0.28)
    M.translucent_outline(cv, 120)
    return cv


def build_limbs():
    d = {}
    for anim in ('Idle', 'Move'):
        for facing in FACINGS:
            d[f'{anim}_{facing}'] = [hyphae_frame(facing, p, anim, i) for i, p in enumerate(FRAMES[f'{anim}_{facing}'])]
    return d


# ===================================================== 3. VACUOLA DIGESTIVA (Órgano)
VAC = ['#7a4418', '#c07228', '#f0aa4c', '#ffe2a4']
FOOD = ['#2a4e1c', '#3e7a2a', '#68a83c']


def vacuole(f, scale=1.0, n=16, food=1.0):
    cv = Canvas(n)
    o = (n - 16) / 2
    cx, cy = M.OCX + o, M.OCY + o
    blob(cv, cx, cy, 3.8 * scale, 2.9 * scale, VAC, alpha=205)
    # trocitos de comida que dan vueltas mientras se digieren
    for k in range(3):
        if k >= int(round(3 * food)):
            break
        a = f * math.pi / 2 * 0.6 + k * 2 * math.pi / 3
        x, y = cx + math.cos(a) * 1.5 * scale, cy + math.sin(a) * 1.0 * scale
        cv.put_in(x, y, FOOD[1])
        cv.put_in(x + 1, y, FOOD[2] if k == 0 else FOOD[0])
    cv.put_in(cx - 1.8 * scale, cy - 1.3 * scale, '#fff4d8')     # brillo húmedo
    M.translucent_outline(cv)
    return cv


def vacuole_activate():
    fr = [vacuole(0, 0.7, n=32)]                                 # se contrae y digiere
    cx, cy = 16, 16 + (M.OCY - 8)
    for k, (r, al) in enumerate(((5.0, 235), (8.0, 210), (10.5, 150))):
        cv = Canvas(32)
        if k == 0:
            blob(cv, cx, cy, 2.4, 1.8, VAC, alpha=230)
        for j in range(6):                                       # gotas ámbar que suben
            a = -math.pi / 2 + (j - 2.5) * 0.45
            blob(cv, cx + math.cos(a) * r, cy + math.sin(a) * r * 0.9, 1.4, 1.4, VAC, alpha=al)
        M.translucent_outline(cv, 150)
        for j, (dx, dy) in enumerate(((-5, -6), (4, -8), (0, -11))):   # crucecitas de curación
            if j <= k:
                E.cross(cv, cx + dx, cy + dy - k * 1.5)
        fr.append(cv)
    return fr


def build_organ():
    return {'Idle': [vacuole(f, s) for f, s in enumerate((0.94, 1.0, 1.05, 1.0))], 'Activate': vacuole_activate()}


# ================================================================== 4. ICONOS
def ic_hyphae(n):
    cv = Cv(n)
    k = n / 16
    for j, (x, y, a, L) in enumerate(((2, 13, -0.75, 13), (3, 14.5, -0.35, 11), (5, 14.5, -1.15, 10))):
        hypha(cv, (x * k, y * k), a, L * k, j * 1.7, 0.3, True, 255)
    ell(cv, 2.2 * k, 13.8 * k, 1.8 * k, 1.6 * k, ['#5a4e3c', '#9a8a6c', '#d8ccae', '#fff8e8'])
    cv.outline()
    return cv


def ic_camouflage(n):
    cv = Cv(n)
    k = n / 16
    r = 6.4 * k
    c = n / 2
    ell(cv, c, c, r, r * 0.92, GREEN['ramp'])
    for (sx, sy, sr, kind) in CAMO_SPOTS:
        ramp = CAMO_DARK if kind == 'd' else CAMO_OLIVE
        ell(cv, c + sx * r * 0.85, c + sy * r * 0.85, sr * k * 0.75 + 0.3, sr * k * 0.7 + 0.3, [ramp[0], ramp[1], ramp[2], ramp[2]])
    cv.outline()
    # la mitad derecha se desvanece: casi invisible
    for (x, y), col in list(cv.px.items()):
        if x + .5 > c + 0.5 * k and (x + y) % 2 == 0:
            cv.px[(x, y)] = col[:3] + (90,)
    return cv


def ic_vacuole(n):
    cv = Cv(n)
    k = n / 16
    ell(cv, 7.5 * k, 8.5 * k, 5.8 * k, 4.8 * k, VAC)
    for (x, y, c_) in ((6, 8, FOOD[1]), (7, 8, FOOD[2]), (9, 10, FOOD[0]), (8, 6.5, FOOD[1])):
        for dx in range(max(1, int(k))):
            for dy in range(max(1, int(k))):
                cv.put_in(x * k + dx, y * k + dy, c_)
    cv.put_in(4.6 * k, 6 * k, '#fff4d8')
    cv.outline()
    E.cross(cv, 12.5 * k, 3.5 * k + 0.5)                      # cura
    return cv


# =========================================================== 5. MARCAS DE ESTADO
QMARK = ['.###.', '#...#', '....#', '...#.', '..#..', '.....', '..#..']
SUSPECT = ['#8a6a14', '#e8c040', '#fff0a0']


def suspect(f):
    """Enemigo que sospecha del jugador camuflado: interrogación amarilla que bota (12x12)."""
    cv = Cv(12)
    oy = [0, -1, 0, 0][f]
    for j, row in enumerate(QMARK):
        for i, ch in enumerate(row):
            if ch == '#':
                cv.put(3 + i, 2 + j + oy, SUSPECT[2] if j < 2 else SUSPECT[1])
    cv.outline()
    return cv


SWEAT = ['#3a6a9a', '#7ab4e8', '#d8f0ff']


def scared(f):
    """Asustado por el Pánico del Camuflaje: dos gotas de sudor que resbalan a un lado de la cabeza (12x12)."""
    cv = Cv(12)
    for k, (x, y0) in enumerate(((3, 2), (8, 4))):
        y = y0 + (f + k * 2) % 4
        cv.put(x, y, SWEAT[2])
        cv.put(x - 1, y + 1, SWEAT[1]); cv.put(x, y + 1, SWEAT[2]); cv.put(x + 1, y + 1, SWEAT[1])
        cv.put(x - 1, y + 2, SWEAT[0]); cv.put(x, y + 2, SWEAT[1]); cv.put(x + 1, y + 2, SWEAT[0])
    cv.outline()
    return cv


# ================================================================= 6. EFECTOS
def mycelium(f):
    """Mancha de micelio en el suelo (radio 0,5 = 8 px): hilos claros que salen del centro (16x16)."""
    cv = Cv(16)
    for k in range(7):
        a = k * 2 * math.pi / 7 + 0.3
        hypha(cv, (8 + math.cos(a), 8 + math.sin(a) * 0.8), a, 5.5 + (k % 3) * 0.6, f * 0.8 + k, 0.4, k % 2 == 0, 200)
    blob(cv, 8, 8, 1.6, 1.3, HYPHA, alpha=210)
    for (x, y), col in list(cv.px.items()):                    # algo transparente: es el suelo
        cv.px[(x, y)] = col[:3] + (min(col[3], 200),)
    return cv


def camouflage_hide(f):
    """Destello al ocultarse: motas verdes que se cierran sobre la célula y se apagan (24x24)."""
    cv = Cv(24)
    r = [10, 8, 6, 4.5, 3][f]
    al = [255, 230, 190, 140, 80][f]
    for k in range(8):
        a = k * math.pi / 4 + f * 0.3
        x, y = 12 + math.cos(a) * r, 12 + math.sin(a) * r * 0.9
        cv.put(x, y, CA(GREEN['ramp'][3], al))
        if f < 3:
            cv.put(x + 1, y, CA(CAMO_OLIVE[2], al))
    return cv


# ================================================================== GUARDAR
SHEETS = []


def as_img(x):
    return x if isinstance(x, Image.Image) else x.img()


def save(group, name, frames, ms=None):
    frames = [as_img(f) for f in frames]
    w, h = frames[0].size
    os.makedirs(os.path.join(OUT, group), exist_ok=True)
    sh = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, im in enumerate(frames):
        sh.paste(im, (i * w, 0))
    sh.save(os.path.join(OUT, group, name + '.png'))
    if len(frames) > 1:
        os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
        sc = max(1, min(10, 240 // max(w, h)))
        out = []
        for im in frames:
            b = Image.new('RGBA', (w, h), BG)
            b.alpha_composite(im)
            out.append(b.resize((w * sc, h * sc), Image.NEAREST).convert('RGB'))
        out[0].save(os.path.join(OUT, 'gif', name + '.gif'), save_all=True, append_images=out[1:], duration=ms or 100, loop=0)
    SHEETS.append((group, name, len(frames), w, h, ms))
    return frames


def build():
    out = {}
    skin = build_skin()
    for name, frs in skin.items():
        anim = name.split('_')[0]
        out[f'Skin_Camouflage_{name}'] = save('Skin', f'Skin_Camouflage_{name}', frs, M.MS[anim])
    for name, frs in build_limbs().items():
        out[f'Limbs_Hyphae_{name}'] = save('Limbs', f'Limbs_Hyphae_{name}', frs, M.MS[name.split('_')[0]])
    for name, frs in build_organ().items():
        out[f'Organ_DigestiveVacuole_{name}'] = save('Organ', f'Organ_DigestiveVacuole_{name}', frs, M.MS[name])
    for m, fn in (('Hyphae', ic_hyphae), ('Camouflage', ic_camouflage), ('DigestiveVacuole', ic_vacuole)):
        for n in (16, 32):
            out[f'Icon_{m}_{n}'] = save('Icons', f'Icon_{m}_{n}', [fn(n)])
    out['Suspect'] = save('Status', 'Suspect', [suspect(f) for f in range(4)], 150)
    out['Scared'] = save('Status', 'Scared', [scared(f) for f in range(4)], 110)
    out['Mycelium'] = save('Effects', 'Mycelium', [mycelium(f) for f in range(4)], 160)
    out['Camouflage_Hide'] = save('Effects', 'Camouflage_Hide', [camouflage_hide(f) for f in range(5)], 70)
    return out


# ============================================================ VISTA PREVIA
def preview(out, path):
    S = 6
    W = 56
    rows = [
        ('CAMUFLAJE', [out[f'Skin_Camouflage_Idle_{f}'][0] for f in FACINGS] + out['Skin_Camouflage_Move_side'] + out['Skin_Camouflage_Death'][:4]),
        ('HIFAS', [compose_cell(f, out[f'Limbs_Hyphae_Move_{f}'][i], None, i) for f in FACINGS for i in (0, 2)]),
        ('VACUOLA', [compose_cell('down', None, out['Organ_DigestiveVacuole_Idle'][i], i) for i in range(4)]
         + out['Organ_DigestiveVacuole_Activate']),
        ('ICONOS Y MARCAS', [out[f'Icon_{m}_32'] [0] for m in ('Hyphae', 'Camouflage', 'DigestiveVacuole')]
         + [out[f'Icon_{m}_16'][0] for m in ('Hyphae', 'Camouflage', 'DigestiveVacuole')]
         + out['Suspect'][:2] + out['Scared'][:2]),
        ('EFECTOS', out['Mycelium'] + out['Camouflage_Hide']),
    ]
    width = 200 + max(len(r[1]) for r in rows) * (W * S // 2 + 8)
    im = Image.new('RGBA', (width, len(rows) * (W * S // 2 + 30) + 20), BG)
    for r, (label, frames) in enumerate(rows):
        y = 20 + r * (W * S // 2 + 30)
        E.G.ptext(im, (12, y + 60), label, (214, 255, 154), scale=2)
        x = 200
        for fr in frames:
            sc = max(1, min(S, (W * S // 2) // max(fr.width, fr.height)))
            big = fr.resize((fr.width * sc, fr.height * sc), Image.NEAREST)
            im.alpha_composite(big, (x, y + (W * S // 2 - big.height) // 2))
            x += W * S // 2 + 8
    im.convert('RGB').save(path)


def compose_cell(facing, limbs, organ, i):
    """La célula con las Hifas detrás o la Vacuola dentro, en un lienzo de 32x32."""
    canvas = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
    if limbs is not None:
        canvas.alpha_composite(limbs)
    cell = PANIMS[f'Idle_{facing}'][i % 4].img()
    canvas.alpha_composite(cell, (8, 8))
    if organ is not None:
        canvas.alpha_composite(organ, (8, 8))
    return canvas


def main():
    out = build()
    preview(out, os.path.join(OUT, 'vista_previa_mutaciones_piso1.png'))
    for row in SHEETS:
        print(*row)


if __name__ == '__main__':
    main()
