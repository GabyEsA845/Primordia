"""Piezas de la sala del Euriptérido (piso 2, Abismo): las rocas que se derrumban y el derrumbe de la cueva.

- Roca agrietada (marca R del plano): la roca grande del Abismo de siempre (environment/Abyss/deco/Obstacle_RockBig)
  con unas grietas finas, para que el jugador atento no se fíe de ella. Bloquea como las demás. 32×32.
- Derrumbe: tiembla, las grietas se abren, se parte en cuatro trozos que caen hacia fuera y levanta una nube de
  polvo; al final quedan los escombros. Solo visual (no hace daño). Lienzo de 48×48, centrado en la roca.
- Escombros: montón bajo de piedras que se queda en el suelo, sin colisión (se pasa y se dispara por encima). 32×32.

Derrumbe de la cueva (crujidos en la fase 1, derrumbe permanente en la fase 2). Los avisos usan el mismo lenguaje que los
del Anomalocaris: borde rojo discontinuo y relleno rojo oscuro translúcido.
- Sombra de la piedra que cae: crece ~0,8 s donde caerá (1 u de radio), con chinas que ya caen. 32×32.
- Piedra que cae del techo (gira al caer; el juego la baja desde arriba). 24×24.
- Impacto: golpe, polvo y esquirlas; no deja nada. 48×48.
- Chinas decorativas que caen por la pantalla (sin daño) y su polvillo al tocar el suelo. 8×8 y 16×16.
- Suelo que se hunde: grietas que se abren en una zona de 3×3 casillas durante 1,5 s y se hunde; después el juego
  pone ahí las fosas del Abismo. 48×48.

Hecho con la skill primordia-sprites (pixel.py): 16 px por unidad, luz arriba a la izquierda, contorno OUTL.
Paleta del Abismo (references/pisos.md): piedra casi negra azulada con vetas cian.

Uso: python generar_sala_euripterido.py [carpeta_salida]   (por defecto, la carpeta de este script)
"""
import math, os, random, sys

def _skill_scripts():
    d = os.path.dirname(os.path.abspath(__file__))
    while True:
        p = os.path.join(d, '.claude', 'skills', 'primordia-sprites', 'scripts')
        if os.path.isdir(p): return p
        if os.path.dirname(d) == d: raise SystemExit('No encuentro .claude/skills/primordia-sprites/scripts')
        d = os.path.dirname(d)
sys.path.insert(0, _skill_scripts())
from pixel import *
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = sys.argv[1] if len(sys.argv) > 1 else HERE
ROOM = os.path.join(OUT, 'EurypteridRoom')
BG = C('#0b1220')                         # suelo del Abismo
BASE_ROCK = os.path.join(HERE, '..', 'environment', 'Abyss', 'deco', 'Obstacle_RockBig.png')

# --- paleta (la de la roca del Abismo, generar_temas_piso2.py) ---------------------------------------
ROCK = ['#0e1628', '#1c2c48', '#2e4468', '#4c6894']
CRACK = C('#05070e')                      # fondo de la grieta, más oscuro que la piedra
VEIN = C('#36d8d0')
DUST = [(70, 88, 120), (104, 124, 158), (150, 170, 200)]   # polvo gris azulado del fondo
WARN = (255, 80, 60)                      # rojo de aviso (el de los marcadores del Anomalocaris)
EMBER = [C('#7a2018'), C('#c8402c'), C('#ff7a5a')]

# Grietas: líneas quebradas desde lo alto de la roca hacia abajo (en píxeles del lienzo de 32).
CRACKS = [
    [(9, 10), (10, 13), (8, 16), (9, 19), (8, 22)],
    [(10, 13), (12, 15)],
    [(24, 9), (23, 12), (25, 15), (24, 18)],
]

def _line(p, q):
    (x0, y0), (x1, y1) = p, q
    n = max(abs(x1 - x0), abs(y1 - y0))
    return [(round(x0 + (x1 - x0) * i / n), round(y0 + (y1 - y0) * i / n)) for i in range(n + 1)]

def _crack_pixels():
    px = []
    for path in CRACKS:
        for a, b in zip(path, path[1:]):
            for p in _line(a, b):
                if p not in px: px.append(p)
    return px

def _base():
    im = Image.open(BASE_ROCK).convert('RGBA')
    cv = Canvas(32)
    for y in range(32):
        for x in range(32):
            c = im.getpixel((x, y))
            if c[3]: cv.px[(x, y)] = c
    return cv

def cracked_rock(widen=0):
    """Roca grande del Abismo con grietas. widen: 0 sutil (la de la sala), 1-2 abiertas (el derrumbe)."""
    cv = _base()
    for x, y in _crack_pixels():
        if (x, y) not in cv.px or cv.px[(x, y)] == OUTL: continue
        cv.put(x, y, CRACK)
        # labio iluminado abajo a la derecha de la grieta (la luz entra por arriba a la izquierda)
        if widen == 0 and cv.px.get((x + 1, y)) not in (None, CRACK, OUTL, VEIN):
            cv.put(x + 1, y, C(ROCK[2]))
        for k in range(1, widen + 1):
            if cv.px.get((x - k, y)) not in (None, OUTL): cv.put(x - k, y, CRACK)
            if cv.px.get((x + k, y)) not in (None, OUTL, CRACK): cv.put(x + k, y, C(ROCK[3]) if k == widen else CRACK)
    return cv

# --- trozos y escombros ------------------------------------------------------------------------------
# Trozos de la roca al partirse: (dx, dy) desde el centro, radios y giro.
CHUNKS = [(-6, -4, 7, 5.5, 0.5), (6, -5, 6.5, 5, -0.4), (-5, 4, 6.5, 5, -0.3), (6, 4, 6, 5, 0.6)]

# Piedras de los escombros: (dx, dy, rx, ry, giro, tono).
STONES = [(-7, 2, 3.5, 2.5, 0.3, 0), (6, 3, 3.5, 2.4, -0.4, 0), (-1, 4, 3, 2.2, 0.1, -1), (0, -1, 3.2, 2.4, 0.6, 0),
          (-9, -2, 2.2, 1.7, -0.5, -1), (9, -1, 2.4, 1.8, 0.4, -1), (3, -4, 2, 1.6, 0, -1), (-4, -4, 2.2, 1.6, 0.9, 0),
          (11, 4, 1.6, 1.3, 0, -1), (-12, 3, 1.6, 1.2, 0, -1)]
GRAVEL = [(-6, 6), (4, 7), (9, 6), (-10, 5), (12, 1), (-13, 0), (1, 7), (-3, -6), (7, -4)]

def _stones(cv, cx, cy, t=1.0):
    """Escombros centrados en (cx, cy). t < 1: las piedras aún van cayendo desde el centro."""
    for dx, dy, rx, ry, ang, tone in STONES:
        rot_ellipse(cv, cx + dx * t, cy + dy * t - (1 - t) * 3, rx, ry, ang, ROCK, shift=tone)
    cv.outline(); cv.clean()
    for dx, dy in GRAVEL:
        cv.put_out(cx + dx * t, cy + dy * t, C(ROCK[1]))
    if t >= 1:
        cv.put_in(cx + 1, cy - 2, VEIN)    # una veta que sigue brillando entre las piedras
        cv.put_in(cx - 7, cy + 1, C('#1d6a78'))

def rubble():
    cv = Canvas(32)
    _stones(cv, 16, 17)
    return cv

def _dust(cv, cx, cy, t, fade):
    """Nube de polvo irregular que se abre y se posa; fade 0..1 la va borrando."""
    rnd = random.Random(4)
    spread = 5 + t * 11
    for k in range(18):
        ang = rnd.uniform(0, 2 * math.pi)
        dist = spread * math.sqrt(rnd.uniform(0.15, 1))
        r = 1.8 + rnd.uniform(0, 1.6) + t * 1.4
        px, py = cx + math.cos(ang) * dist, cy + math.sin(ang) * dist * 0.6 + 2 + t * 1.5
        a = int((150 - 50 * t) * (1 - fade) * rnd.uniform(0.7, 1))
        if a <= 10: continue
        for yy in range(int(py - r), int(py + r) + 1):
            for xx in range(int(px - r), int(px + r) + 1):
                d = math.hypot(xx + .5 - px, yy + .5 - py)
                if d > r or not (1 <= xx < cv.w - 1 and 1 <= yy < cv.h - 1): continue
                tone = 2 if d < r * 0.45 and yy < py else 1 if d < r * 0.8 else 0
                cv.put_out(xx, yy, DUST[tone] + (a,))

def collapse():
    frames = []
    def rock_frame(dx, widen, chips=()):
        cv = Canvas(48)
        rock = cracked_rock(widen)
        for (x, y), c in rock.px.items(): cv.px[(x + 8 + dx, y + 8)] = c
        for x, y in chips: cv.put(x, y, C(ROCK[2]))
        return cv
    frames.append(rock_frame(1, 0))
    frames.append(rock_frame(-1, 1))
    frames.append(rock_frame(1, 2, chips=((21, 10), (27, 9), (17, 13))))
    # se parte: los trozos se separan, caen hacia fuera y encogen (se hunden en el polvo)
    for i, t in enumerate((0.15, 0.45, 0.8)):
        cv = Canvas(48)
        for dx, dy, rx, ry, ang in CHUNKS:
            out = 1 + t * 2
            rot_ellipse(cv, 24 + dx * out, 25 + dy * out * 0.8 + t * 3, rx * (1 - 0.5 * t), ry * (1 - 0.55 * t), ang + t * 0.8,
                        ROCK, shift=-1 if t > 0.5 else 0)
        if t < 0.5:
            for dx, dy, *_ in CHUNKS:
                cv.put_in(24 + dx * 0.6, 24 + dy * 0.6, VEIN)
        cv.outline(); cv.clean()
        for k in range(6):      # chinas que saltan
            ang = k * 1.05 + 0.3
            cv.put_out(24 + math.cos(ang) * (8 + t * 12), 22 + math.sin(ang) * (6 + t * 8) - (1 - t) * 4, C(ROCK[2]))
        _dust(cv, 24, 24, t, 0)
        frames.append(cv)
    # el polvo se posa y quedan los escombros (el último cuadro es el sprite de escombros)
    for t, fade in ((0.85, 0.2), (1.0, 0.5), (1.0, 0.8), (1.0, 1.0)):
        cv = Canvas(48)
        _stones(cv, 24, 25, t)
        _dust(cv, 24, 24, 1.0, fade)
        frames.append(cv)
    return frames

# --- derrumbe de la cueva ------------------------------------------------------------------------------
def _dashed_ring(cv, cx, cy, r, phase, alpha=230, dash=10):
    """Borde discontinuo de aviso de radio r, con los trazos girando con phase."""
    for y in range(int(cy - r) - 2, int(cy + r) + 3):
        for x in range(int(cx - r) - 2, int(cx + r) + 3):
            d = math.hypot(x + .5 - cx, y + .5 - cy)
            a = math.atan2(y + .5 - cy, x + .5 - cx)
            if r - 1.1 <= d <= r and int((a + math.pi + phase) / (math.pi / dash)) % 2 == 0:
                cv.put(x, y, WARN + (alpha,))

def fall_shadow(k, n=6):
    """Sombra que crece donde caerá la piedra: oscura en el centro, borde rojo; las chinas ya caen."""
    cv = Canvas(32)
    t = k / (n - 1)
    r = 6 + t * 9
    for y in range(32):
        for x in range(32):
            d = math.hypot(x + .5 - 16, y + .5 - 16)
            if d < r - 1:
                cv.put(x, y, (8, 4, 12, int(80 + 90 * t)) if d < r * 0.55 else (110, 24, 22, int(50 + 60 * t)))
    _dashed_ring(cv, 16, 16, r, k * 0.25, dash=6 + k)
    rnd = random.Random(k + 11)
    for _ in range(2 + k):                # chinas que caen antes que la piedra
        cv.put(16 + rnd.randint(-int(r * 0.6), int(r * 0.6)), 16 + rnd.randint(-int(r * 0.5), int(r * 0.5)), C(ROCK[3]))
    return cv

def falling_rock(k, n=4):
    """Piedra del techo que gira al caer, con una veta cian."""
    cv = Canvas(24)
    ang = k / n * math.pi
    rot_ellipse(cv, 12, 12, 8, 6.2, ang, ROCK)
    rot_ellipse(cv, 12 + math.cos(ang + 2.2) * 4, 12 + math.sin(ang + 2.2) * 3, 4, 3.4, ang + 0.6, ROCK)
    cv.outline(); cv.clean()
    for i in range(4):
        cv.put_in(12 + math.cos(ang) * (i - 1.5) * 1.6, 12 + math.sin(ang) * (i - 1.5) * 1.6 - 1, VEIN)
    return cv

def impact(k, n=6):
    """La piedra se rompe contra el suelo: destello, esquirlas que saltan y polvo que se posa."""
    cv = Canvas(48)
    t = k / (n - 1)
    rnd = random.Random(21)
    if k < 2:                             # restos de la piedra aplastándose
        ellipse(cv, 24, 25, 8 - k * 2, 5 - k * 2, ROCK)
        cv.outline(); cv.clean()
    if k == 0:
        for dx, dy in ((0, -8), (-10, -1), (10, -1)): sparks(cv, 24 + dx, 24 + dy, 1)
    for i in range(9):                    # esquirlas
        a = i / 9 * 2 * math.pi + rnd.uniform(-0.2, 0.2)
        dist = 5 + t * 15 * rnd.uniform(0.7, 1)
        x, y = 24 + math.cos(a) * dist, 24 + math.sin(a) * dist * 0.7 - math.sin(t * math.pi) * 5
        if k < n - 1:
            cv.put_out(x, y, C(ROCK[3] if i % 3 == 0 else ROCK[2]))
            if k < 3: cv.put_out(x + 1, y, C(ROCK[1]))
    _dust(cv, 24, 22, 0.2 + t * 0.7, max(0, (t - 0.4) / 0.6))
    return cv

def pebble(v):
    """China decorativa que cae por la pantalla (tres formas)."""
    cv = Canvas(8)
    rx, ry = ((2.6, 2), (2, 1.6), (3, 1.8))[v]
    rot_ellipse(cv, 4, 4, rx, ry, v * 0.7, ROCK)
    cv.outline(); cv.clean()
    return cv

def pebble_puff(k, n=4):
    """Polvillo de una china al tocar el suelo."""
    cv = Canvas(16)
    t = (k + 1) / n
    for i in range(5):
        a = math.pi + i / 4 * math.pi
        x, y = 8 + math.cos(a) * (2 + t * 5), 10 + math.sin(a) * (1 + t * 2.5)
        alpha = int(170 * (1 - t) + 85)
        cv.put(x, y, DUST[2] + (alpha,))
        if k < 3: cv.put(x + 1, y, DUST[1] + (alpha,))
    if k == 0: cv.put(8, 10, C(ROCK[2]))
    return cv

# Grietas del suelo que se hunde: ramas desde el centro de la zona de 3×3 (48×48).
FLOOR_CRACKS = [[(24, 24), (20, 19), (17, 12), (12, 7)], [(24, 24), (30, 20), (36, 18), (41, 12)],
                [(24, 24), (27, 31), (25, 37), (29, 42)], [(24, 24), (17, 27), (10, 29), (6, 35)],
                [(20, 19), (14, 20), (8, 17)], [(30, 20), (33, 26), (40, 29)], [(27, 31), (33, 35)], [(17, 27), (19, 34), (15, 40)]]

def _in_zone(x, y, margin=0.0):
    """Dentro del cuadrado de 3×3 casillas con las esquinas redondeadas (radio 5), encogido margin px."""
    cx, cy = min(max(x, 5 + margin), 43 - margin), min(max(y, 5 + margin), 43 - margin)
    return math.hypot(x - cx, y - cy) <= 5 - 0 * margin and 0.5 + margin <= x <= 47.5 - margin and 0.5 + margin <= y <= 47.5 - margin

def _zone_ring(cv, phase, alpha):
    """Borde discontinuo de aviso alrededor de las 3×3 casillas (esquinas redondeadas, como queda la fosa)."""
    for y in range(48):
        for x in range(48):
            px, py = x + .5, y + .5
            if _in_zone(px, py) and not _in_zone(px, py, 1.2) and int((x + y) / 4 + phase) % 2 == 0:
                cv.put(x, y, WARN + (alpha,))

def floor_crack(k, n=8):
    """El suelo se agrieta durante 1,5 s (cuadros 0-5, una vez) y se hunde (6-7); luego aparece la fosa."""
    cv = Canvas(48)
    if k < 6:
        t = (k + 1) / 6
        segs = []
        for path in FLOOR_CRACKS[:4 + int(t * 4)]:
            for a, b in zip(path, path[1:]):
                segs += _line(a, b)
        grow = int(len(segs) * min(1, 0.35 + t))
        _zone_ring(cv, k * 0.5, 210 if k % 2 == 0 else 160)
        for i, (x, y) in enumerate(segs[:grow]):
            cv.put(x, y, CRACK)
            if t > 0.5: cv.put(x + 1, y, CRACK)
            if i % 5 == k % 5: cv.put(x, y - 1, EMBER[2 if t > 0.6 else 1])   # brillos rojizos en la grieta
            elif i % 3 == 0: cv.put_out(x - 1, y, EMBER[0])
        for y in range(48):               # suelo que se oscurece
            for x in range(48):
                if _in_zone(x + .5, y + .5, 1.2): cv.put_out(x, y, (110, 24, 22, int(25 + 50 * t)))
    else:
        sink = k - 5                      # trozos del suelo que se hunden en la oscuridad
        rnd = random.Random(31)
        for i in range(7):
            x, y = rnd.uniform(12, 36), rnd.uniform(12, 36)
            rot_ellipse(cv, x, y + sink, 4.5 - sink * 1.3, 3.2 - sink, rnd.uniform(0, 3), ROCK, shift=-sink)
        cv.outline(); cv.clean()
        for y in range(48):
            for x in range(48):
                if _in_zone(x + .5, y + .5): cv.put_out(x, y, (3, 4, 9, 120 + sink * 60))
        _dust(cv, 24, 22, 0.5 + sink * 0.3, 0.3 * sink)
    return cv

# --- salida ----------------------------------------------------------------------------------------
if __name__ == '__main__':
    os.makedirs(ROOM, exist_ok=True)
    rock, rub, fall = cracked_rock(), rubble(), collapse()
    rock.img().save(os.path.join(ROOM, 'Obstacle_RockCracked.png'))
    rub.img().save(os.path.join(ROOM, 'Deco_Rubble.png'))
    sheets = {
        'Fx_RockCollapse': fall,
        'Fx_RockFallShadow': [fall_shadow(k) for k in range(6)],
        'Proj_FallingRock': [falling_rock(k) for k in range(4)],
        'Fx_RockImpact': [impact(k) for k in range(6)],
        'Fx_FallingPebble': [pebble(v) for v in range(3)],
        'Fx_PebblePuff': [pebble_puff(k) for k in range(4)],
        'Fx_FloorCrack': [floor_crack(k) for k in range(8)],
    }
    for name, frames in sheets.items():
        save_sheet(os.path.join(ROOM, name + '.png'), frames)

    plain = _base()
    anims = {'Roca normal / agrietada / escombros': [plain, rock, rub]}
    anims.update(sheets)
    preview(os.path.join(OUT, 'vista_previa_sala_euripterido.png'), anims, BG, scale=4)

    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    gif(os.path.join(OUT, 'gif', 'Fx_RockCollapse.gif'), fall + [fall[-1]] * 6,
        [90, 90, 90, 70, 70, 80, 90, 110, 130, 160] + [160] * 6, BG, scale=5)
    # Caída completa: la sombra crece, la piedra baja desde arriba y se rompe.
    seq = []
    for k in range(6):
        frame = Image.new('RGBA', (48, 48))
        frame.alpha_composite(as_img(sheets['Fx_RockFallShadow'][k]), (8, 8))
        if k >= 3:
            frame.alpha_composite(as_img(sheets['Proj_FallingRock'][k % 4]), (12, max(0, 12 - (5 - k) * 10)))
        seq.append(frame)
    seq += [as_img(f) for f in sheets['Fx_RockImpact']]
    gif(os.path.join(OUT, 'gif', 'Fx_RockFall.gif'), seq + [Image.new('RGBA', (48, 48))] * 4,
        [130] * 6 + [70] * 6 + [130] * 4, BG, scale=5)
    crack = [as_img(f) for f in sheets['Fx_FloorCrack']]
    gif(os.path.join(OUT, 'gif', 'Fx_FloorCrack.gif'), crack + [Image.new('RGBA', (48, 48))] * 3,
        [250] * 6 + [120, 120] + [200] * 3, BG, scale=5)
