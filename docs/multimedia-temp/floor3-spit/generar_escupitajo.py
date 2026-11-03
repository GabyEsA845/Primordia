"""Escupitajo venenoso de la rana (piso 3), segunda versión: la bola rueda por el suelo y deja una banda de baba continua.

- Bola que rueda (side/down/up, 8 cuadros): luz fija arriba a la izquierda; lo que gira son las manchas y burbujas de
  dentro (una esfera en 3D que rota sobre el eje del rodar). Va aplastada contra el suelo con un pie pegajoso, una
  sombra de contacto y, detrás, el arranque de la banda.
- Hinchado final (side/down/up, 6 cuadros, 0,6 s): sigue rodando, crece hasta 1,3 veces, la piel se tensa y parpadea
  dos veces cada vez más rápido. Después va el reventón de siempre (floor3-bodies/Effects/Fx_PoisonSpit_Burst).
- Banda de baba por sellos solapados (4 variantes): cada sello tiene dos capas, el borde (debajo de todos los cuerpos)
  y el cuerpo, así la unión de muchos sellos forma una banda sin costuras en cualquier ángulo. Cada sello nace grueso y
  brillante (la cola pegajosa pegada a la bola), se queda húmedo y se seca agrietándose. Brillos aparte.
- Bolitas de la Camada (III): ruedan igual (4 cuadros) y dejan una banda fina.
Escenas de revisión en gif/escenas/. Skill primordia-sprites (pixel.py).

Uso: python generar_escupitajo.py [carpeta_salida]   (por defecto, la carpeta de este script)
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
SKILL = os.path.join(HERE, '..', '..', '..', '.claude', 'skills', 'primordia-sprites', 'scripts')
sys.path.insert(0, os.path.abspath(SKILL))
from pixel import *
from PIL import Image

OUT = sys.argv[1] if len(sys.argv) > 1 else HERE
BG = C('#1c2e20')
VENOM = ['#2e5a14', '#5aa020', '#a8e040', '#e8ffc0']        # veneno (reacciones)
VDEEP = '#24480f'                                           # manchas hondas sobre la parte en sombra
VMID = '#4a8c1c'
FLASH = ['#5aa020', '#a8e040', '#d8f890', '#f2ffd8']         # parpadeo del hinchado
TRAIL_SHADOW = '#1f3812'                                    # canto inferior de la banda (grosor de la baba)
DRY = [('#4a8c1c', '#5aa020', '#2e5a14'),                   # (cuerpo, borde claro, borde oscuro) al secarse
       ('#3f7418', '#4a8c1c', '#284c12'),
       ('#365e18', '#3f6e1a', '#24420f'),
       ('#30521a', '#365e18', '#223c10')]

ROLL_MS, SWELL_MS, SMALL_MS = 135, 100, 110
BALL_R, SMALL_R = 5.5, 2.6
CONTACT = {24: (12, 16.5), 12: (6, 8.5)}                     # punto de apoyo en el suelo dentro de la celda

# =================================================================================================
# Bola que rueda
# =================================================================================================
def _norm(v):
    l = math.sqrt(sum(c * c for c in v)); return tuple(c / l for c in v)

def _rot(v, k, th):
    """Rodrigues: gira v sobre el eje unitario k."""
    c, s = math.cos(th), math.sin(th)
    cx = (k[1] * v[2] - k[2] * v[1], k[2] * v[0] - k[0] * v[2], k[0] * v[1] - k[1] * v[0])
    d = sum(a * b for a, b in zip(k, v))
    return tuple(v[i] * c + cx[i] * s + k[i] * d * (1 - c) for i in range(3))

# Eje del rodar en cada vista (y hacia abajo, z hacia la cámara). side: rueda a la derecha (la parte de arriba va hacia
# la derecha), algo inclinado para la vista 3/4; down: la cara de delante baja; up: la cara de delante sube.
AXES = {'side': _norm((0, .35, .94)), 'down': (-1, 0, 0), 'up': (1, 0, 0)}

# Manchas en la superficie: (dirección, tipo, tamaño). 'deep' = mancha honda, 'bub' = burbuja con brillo.
SPOTS = [((.5, -.4, .77), 'deep', 1.4), ((-.6, .2, .77), 'bub', 0), ((.1, .62, .78), 'deep', 1.1),
         ((-.3, -.72, .62), 'bub', 1), ((.82, .3, .48), 'bub', 0), ((-.2, .1, -.97), 'deep', 1.3),
         ((.3, -.2, -.93), 'bub', 1), ((0, -.9, -.4), 'deep', 1.0), ((-.75, -.3, -.6), 'bub', 0),
         ((.6, .72, -.3), 'deep', 1.2), ((-.8, .55, .1), 'deep', 1.0)]
SPOTS = [(_norm(v), t, s) for v, t, s in SPOTS]

def ball(cv, view, th, r, contact, swell=0.0, flash=False, spots=SPOTS, neck=True, bumps=0.0, shadow=True):
    """Bola viscosa apoyada en el suelo en contact (x, y). th: ángulo girado (rad). swell 0..1: hinchado hasta 1,3."""
    gx, gy = contact
    r = r * (1 + .3 * swell)
    wob = .05 * math.sin(2 * th)                            # se estira y se aplasta dos veces por vuelta
    sx, sy = (1.06 + wob, .94 - wob) if view == 'side' else (1.0 + wob, .97 - wob)
    cx, cy = gx, gy - r * .95 * sy
    lobes = [(cx, cy, r, sx, sy),
             (cx - r * .55, cy + r * .78 * sy, r * .45, 1.25, .8),       # pie pegado al suelo
             (cx + r * .5, cy + r * .8 * sy, r * .42, 1.25, .8)]
    r0 = r / (1 + .3 * swell)
    neck = neck and swell < .5                                           # al hincharse tira de la cola y la recoge
    if neck and view == 'side':                                          # arranque de la banda, detrás
        lobes += [(cx - r0 * .98, cy + r * .82 * sy, r0 * .36, 1.5, .8), (cx - r0 * 1.36, cy + r * .9 * sy, r0 * .24, 1.6, .8)]
    if neck and view == 'up':
        lobes += [(cx, cy + r * 1.2, r0 * .38, 1.4, .8)]
    if bumps:                                                            # la piel cede en bultos antes de estallar
        for a in (-55, 35, 160, 250):
            ar = math.radians(a)
            lobes.append((cx + math.cos(ar) * r * .82 * sx, cy + math.sin(ar) * r * .82 * sy, r * .3 * bumps, 1, 1))
    def inside(x, y):
        return sum(rr * rr / max(.01, ((x - lx) / ax) ** 2 + ((y - ly) / ay) ** 2) for lx, ly, rr, ax, ay in lobes) >= 1.0
    if shadow:                                                           # sombra de contacto
        for y in range(int(gy - 2), int(gy + 3)):
            for x in range(int(gx - r * 1.5), int(gx + r * 1.5) + 1):
                if ((x + .5 - gx) / (r * 1.3)) ** 2 + ((y + .5 - gy - .6) / 1.9) ** 2 <= 1:
                    cv.put(x, y, C(hexc(OUTL), 110))
    pal = FLASH if flash else VENOM
    t = Canvas(cv.w, cv.h)
    fill(t, inside, cx, cy, r * 1.15 * sx, r * 1.15 * sy, pal, box=(cx - 3 * r, cy - 2 * r, cx + 2 * r, cy + 2 * r))
    body = set(t.px)
    idx = {C(c): i for i, c in enumerate(pal)}
    k = AXES[view]
    for v, kind, size in spots:                                          # manchas que giran con la bola
        x, y, z = _rot(v, k, th)
        if z < .12: continue
        px, py = cx + x * r * sx * .92, cy + y * r * sy * .92
        if kind == 'deep':
            rad = size * (.45 + .55 * z) * (r / BALL_R) ** .6 * (1 + .5 * swell)
            for yy in range(int(py - rad) - 1, int(py + rad) + 2):
                for xx in range(int(px - rad) - 1, int(px + rad) + 2):
                    if (xx, yy) not in body: continue
                    if ((xx + .5 - px) / rad) ** 2 + ((yy + .5 - py) / (rad * (.55 + .45 * z))) ** 2 > 1: continue
                    i = idx.get(t.px[(xx, yy)], 1)
                    if flash: c = pal[max(1, i - 1)]
                    elif swell > .5: c = pal[max(1, i - 1)]                 # la piel tensa aclara las manchas
                    else: c = VDEEP if i == 0 else pal[i - 1]
                    t.px[(xx, yy)] = C(c)
        else:
            if (int(px), int(py)) in body and (int(px) - 1, int(py) - 1) in body:
                t.put(px, py, C(pal[0] if not flash else pal[1])); t.put(px - 1, py - 1, C(pal[3]))
                if size and z > .5 and (int(px) + 1, int(py)) in body: t.put(px + 1, py, C(pal[0] if not flash else pal[1]))
    hx, hy = cx - r * .5 * sx, cy - r * .55 * sy                       # brillo húmedo fijo (la luz no gira)
    for dx, dy in ((0, 0), (1, 0), (0, 1)) + (((2, 0), (-1, 1)) if r > 6 else ()):
        t.put_in(hx + dx, hy + dy, C(pal[3]))
    if swell > .35:                                                      # tensión: rayitas claras en el borde de luz
        for a in (200, 235):
            ar = math.radians(a)
            t.put_in(cx + math.cos(ar) * r * .8 * sx, cy + math.sin(ar) * r * .8 * sy, C(pal[3]))
    t.outline(); t.clean()
    for p, c in t.px.items(): cv.px[p] = c
    return cv

def roll_frames(view, n, r, size):
    return [ball(Canvas(size), view, i / n * 2 * math.pi, r, CONTACT[size]) for i in range(n)]

def swell_frames(view):
    """6 cuadros de 100 ms: sigue rodando, se hincha a 1,3 y parpadea en los cuadros 2, 4 y 5."""
    out = []
    for i in range(6):
        s = (i + 1) / 6
        out.append(ball(Canvas(24), view, i / 8 * 2 * math.pi, BALL_R, CONTACT[24], swell=s,
                        flash=i in (2, 4, 5), bumps=max(0, (i - 3) / 2) if i >= 4 else 0))
    return out

SMALL_SPOTS = [((.5, -.3, .8), 'bub', 0), ((-.4, .5, .75), 'deep', .9), ((.1, .2, -.97), 'bub', 0),
               ((-.3, -.8, -.5), 'deep', .9)]
SMALL_SPOTS = [(_norm(v), t, s) for v, t, s in SMALL_SPOTS]

def small_frames(view):
    return [ball(Canvas(12), view, i / 4 * 2 * math.pi, SMALL_R, CONTACT[12], spots=SMALL_SPOTS, neck=False)
            for i in range(4)]

# =================================================================================================
# Banda de baba (sellos)
# =================================================================================================
def stamp_mask(n, rx, ry, seed, grow=1.0, crack=0.0, sats=0):
    """Mancha irregular centrada en la celda (metabolas). crack 0..1: se rompe en escamas al secarse.
    sats: gotas sueltas salpicadas a los lados."""
    rnd = random.Random(seed)
    c = n / 2
    lobes = [(c, c, .62)]
    for k in range(4):
        a = k * math.pi / 2 + rnd.uniform(-.5, .5)
        d = rnd.uniform(.3, .42)
        lobes.append((c + math.cos(a) * d * rx, c + math.sin(a) * d * ry, rnd.uniform(.34, .44)))
    for k in range(sats):
        a = rnd.choice((-1, 1)) * rnd.uniform(1.2, 1.9)
        lobes.append((c + math.cos(a) * 1.25 * rx, c + math.sin(a) * 1.95 * ry, rnd.uniform(.13, .16)))
    pts = set()
    noise = random.Random(seed * 7 + 1)
    holes = {(x, y): noise.random() for y in range(n) for x in range(n)}
    for y in range(n):
        for x in range(n):
            v = sum(rr * rr / max(.001, ((x + .5 - lx) / (rx * grow)) ** 2 + ((y + .5 - ly) / (ry * grow)) ** 2)
                    for lx, ly, rr in lobes)
            if v < 1.0: continue
            if crack and holes[(x, y)] < crack * (1.25 - min(1, v - 1.0) * .8): continue   # grietas, sobre todo al borde
            pts.add((x, y))
    return pts

def stamp_layers(n, pts, body_c, light_c, dark_c, specks=(), speck_c=VMID):
    """Capa de borde (1 px alrededor, claro arriba a la izquierda y oscuro abajo a la derecha, y el canto de abajo)
    y capa de cuerpo (color liso: se tapa con los sellos vecinos sin dejar costuras)."""
    rim, body = Canvas(n), Canvas(n)
    for (x, y) in pts:
        body.put(x, y, C(body_c))
    for (sx, sy) in specks:
        if (sx, sy) in pts and (sx + 1, sy) in pts:
            body.put(sx, sy, C(speck_c)); body.put(sx + 1, sy, C(speck_c))
    for y in range(n):
        for x in range(n):
            if (x, y) in pts: continue
            up = (x, y + 1) in pts                                # el cuerpo está debajo: borde de luz (solo arriba)
            dn = (x, y - 1) in pts or (x - 1, y) in pts
            if dn: rim.put(x, y, C(dark_c))
            elif up: rim.put(x, y, C(light_c))
            elif (x + 1, y) in pts: rim.put(x, y, C(body_c))       # a la izquierda, sin borde claro: no parece un tubo
    for (x, y) in list(rim.px):                                    # canto de abajo: grosor de la baba
        if rim.px[(x, y)] == C(dark_c) and (x, y - 1) in pts and (x, y + 1) not in pts and (x, y + 1) not in rim.px:
            rim.put(x, y + 1, C(TRAIL_SHADOW))
    return rim, body

def trail_variant(n, rx, ry, seed, specks=True, sats=0):
    """8 cuadros por capa: 0-2 nace (grueso y brillante: la cola pegada a la bola), 2 se mantiene húmedo, 3-7 se seca."""
    rnd = random.Random(seed + 99)
    sp = [(int(n / 2 + rnd.uniform(-rx * .6, rx * .5)), int(n / 2 + rnd.uniform(-ry * .5, ry * .5))) for _ in range(3)] if specks else ()
    rims, bodies = [], []
    states = [(1.3, VENOM[2], VENOM[3], VENOM[1], 0), (1.14, VENOM[2], VENOM[3], VENOM[0], 0), (1.0, VENOM[1], VENOM[2], VENOM[0], 0)]
    for k, (b, l, d) in enumerate(DRY):
        states.append((1.0 - .06 * k, b, l, d, (0, .12, .32, .55)[k]))
    states.append((.62, DRY[3][0], DRY[3][1], DRY[3][2], .72))
    for g, b, l, d, cr in states:
        pts = stamp_mask(n, rx, ry, seed, g, cr, sats)
        rim, body = stamp_layers(n, pts, b, l, d, sp if cr < .3 else ())
        rims.append(rim); bodies.append(body)
    return rims, bodies

def shine(i):
    """Brillo húmedo que titila (se pone sobre uno de cada tres sellos mientras está húmedo)."""
    cv = Canvas(8)
    L = ((3, 4), (2, 5), (2, 5), (3, 4))[i]
    for x in range(L[0], L[1]): cv.put(x, 3, C(VENOM[3]))
    if i == 1: cv.put(5, 5, C(VENOM[2]))
    if i == 2: cv.put(2, 4, C(VENOM[2])); cv.put(6, 2, C(VENOM[3]))
    return cv

TRAIL = dict(n=20, rx=4.6, ry=3.6, step=4, jitter=1)        # banda de ~10 px; un sello cada 4 px (0,25 u)
THIN = dict(n=14, rx=2.2, ry=1.8, step=2, jitter=0)          # banda fina de la Camada (~4-5 px); un sello cada 2 px, ondulada
# Variantes de la banda: (escala, semilla, gotas sueltas). Se eligen al azar; la 5 es un goterón más ancho y sale poco.
TRAIL_VARIANTS = [(.88, 31, 0), (.96, 48, 0), (1.0, 65, 0), (1.04, 82, 0), (1.12, 99, 0), (1.22, 116, 0)]
THIN_VARIANTS = [(.72, 7, 0), (.88, 20, 0), (1.0, 33, 0), (1.12, 46, 0), (1.5, 59, 0)]   # la última, goterón poco frecuente

def trail_set(cfg, variants, specks):
    return [trail_variant(cfg['n'], cfg['rx'] * k, cfg['ry'] * k, seed, specks, sats) for k, seed, sats in variants]

# =================================================================================================
# Reventón irregular y su charco salpicado
# =================================================================================================
BURST_N, SPLAT_N = 64, 64
SPLAT_R = 19.0                                             # como el charco grande de antes (~1,2 u); el juego lo escala

def _splat_plan(seed=8):
    """Direcciones del reventón compartidas con el charco: 3 chorretones principales desiguales y gotas sueltas,
    más cargadas hacia esos lados (así las gotas caen donde luego hay lenguas de charco)."""
    rnd = random.Random(seed)
    mains = [rnd.uniform(0, 2 * math.pi)]
    mains += [mains[0] + rnd.uniform(1.7, 2.3), mains[0] + rnd.uniform(3.6, 4.4)]
    tongues = [(a, rnd.uniform(.95, 1.3), rnd.uniform(.85, 1.15)) for a in mains]      # (ángulo, largo, grosor)
    for k in range(4):                                                                 # lenguas menores entre medias
        tongues.append((mains[k % 3] + rnd.choice((-1, 1)) * rnd.uniform(.6, 1.0), rnd.uniform(.55, .8), rnd.uniform(.55, .75)))
    drops = []
    for k in range(17):
        if k < 11:
            a = rnd.choice(mains) + rnd.gauss(0, .38)
            d = rnd.uniform(.7, 1.2)
        else:
            a = rnd.uniform(0, 2 * math.pi); d = rnd.uniform(.45, .85)
        drops.append((a, d, rnd.uniform(.55, 1.6)))
    return tongues, drops

def splat_mask(n, r, seed=8, grow=1.0, dry=0.0):
    rnd = random.Random(seed * 5 + 3)
    tongues, drops = _splat_plan(seed)
    c = n / 2
    lobes = [(c, c, r * .4 * grow)]
    caps = []                                              # bultos y lenguas: unión de círculos (no se funden en un óvalo)
    for k in range(9):                                     # cuerpo con bultos desiguales
        a = k * 2 * math.pi / 9 + rnd.uniform(-.3, .3)
        d = rnd.uniform(.28, .52)
        caps.append((c + math.cos(a) * r * d * grow, c + math.sin(a) * r * d * grow, r * rnd.uniform(.17, .31) * grow))
    for (a, ln, th) in tongues:                            # lenguas finas que se adelgazan, con una gota en la punta
        for j in range(1, 9):
            t_ = j / 8
            caps.append((c + math.cos(a) * r * (.3 + .68 * t_) * ln * grow, c + math.sin(a) * r * (.3 + .68 * t_) * ln * grow,
                          r * (.26 - .19 * t_ ** .8) * th * grow * (1 - .3 * dry)))
        caps.append((c + math.cos(a) * r * 1.1 * ln * grow, c + math.sin(a) * r * 1.1 * ln * grow, max(1.1, r * .1 * th * grow * (1 - .4 * dry))))
    pts = set()
    for y in range(n):
        for x in range(n):
            yy = (y + .5 - c) / .72 + c
            v = sum(rr * rr / max(.01, (x + .5 - lx) ** 2 + (yy - ly) ** 2) for lx, ly, rr in lobes)
            if v >= 1.0 or any((x + .5 - lx) ** 2 + (yy - ly) ** 2 <= rr * rr for lx, ly, rr in caps): pts.add((x, y))
    for (a, d, sz) in drops[11:]:                          # gotas sueltas que no tocan el cuerpo
        gx, gy = c + math.cos(a) * r * (1.05 + .3 * d), c + math.sin(a) * r * (1.05 + .3 * d) * .72
        rr = max(.9, sz * 1.1 * grow * (1 - .4 * dry))
        for y in range(int(gy - rr) - 1, int(gy + rr) + 2):
            for x in range(int(gx - rr) - 1, int(gx + rr) + 2):
                if ((x + .5 - gx) / rr) ** 2 + ((y + .5 - gy) / (rr * .75)) ** 2 <= 1: pts.add((x, y))
    if dry:                                                # se encoge y se agrieta por los bordes
        noise = random.Random(seed + 77)
        pts = {p for p in pts if not (any(q not in pts for q in ((p[0] + 1, p[1]), (p[0] - 1, p[1]), (p[0], p[1] + 1), (p[0], p[1] - 1)))
                                      and noise.random() < dry * .9)}
    return {p for p in pts if 0 <= p[0] < n and 0 <= p[1] < n}

def splat_paint(n, pts, alpha=235, dry=0.0, bubbles=()):
    """Mismo sombreado que los charcos de veneno: plano, sin contorno, borde oscuro abajo a la derecha y claro arriba a la
    izquierda, manchas más hondas en el centro y brillos."""
    cv = Canvas(n)
    pal = VENOM if dry < .5 else [VENOM[0], VENOM[0], VMID, VENOM[1]]
    c = n / 2
    rnd = random.Random(41)
    deep = [(c + rnd.uniform(-6, 6), c + rnd.uniform(-4, 4), rnd.uniform(3, 5.5)) for _ in range(4)]
    for (x, y) in pts:
        edge_dr = (x + 1, y) not in pts or (x, y + 1) not in pts
        edge_ul = (x - 1, y) not in pts or (x, y - 1) not in pts
        inner = all((x + dx, y + dy) in pts for dx in (-2, 2) for dy in (-2, 2))
        col = pal[0] if edge_dr else pal[2] if edge_ul else pal[1]
        if inner and any((x - a) ** 2 + ((y - b) / .8) ** 2 < rr * rr for a, b, rr in deep): col = VMID
        cv.put(x, y, C(col, alpha))
    if dry < .5:
        for (hx, hy, L) in ((c - 9, c - 5, 4), (c - 2, c - 3, 2), (c + 5, c - 6, 2)):
            for k in range(L):
                if (int(hx + k), int(hy)) in pts and (int(hx + k), int(hy) - 1) in pts: cv.put(hx + k, hy, C(pal[3], alpha))
    for (bx, by, br) in bubbles:
        bx, by = int(bx), int(by)
        if (bx, by) not in pts or (bx + 1, by + 1) not in pts: continue
        if br <= 0: cv.put(bx, by, C(VENOM[2]))
        elif br < 1.5:
            for (dx, dy, col) in ((0, 0, VENOM[3]), (1, 0, VENOM[2]), (0, 1, VENOM[1]), (1, 1, VENOM[0])): cv.put(bx + dx, by + dy, C(col))
        else:
            for (dx, dy) in ((-1, 0), (1, 0), (0, -1), (0, 1)): cv.put(bx + dx, by + dy, C(VENOM[2]))
    return cv

def splat_sheets():
    n, r = SPLAT_N, SPLAT_R
    appear = [splat_paint(n, splat_mask(n, r, grow=g)) for g in (.55, 1.1, 1.0)]
    idle = []
    for i in range(4):
        c = n / 2
        bb = [(c - 5, c + 2, (i % 4) * .9), (c + 6, c - 1, ((i + 2) % 4) * .9), (c, c + 5, ((i + 1) % 4) * .9)]
        idle.append(splat_paint(n, splat_mask(n, r, grow=1.0 + .025 * math.sin(i * math.pi / 2)), bubbles=bb))
    fade = [splat_paint(n, splat_mask(n, r, dry=d), alpha=int(235 - 70 * d), dry=d) for d in (.35, .7, 1.0)]
    return {'Fx_PoisonSplat_Appear': appear, 'Fx_PoisonSplat_Idle': idle, 'Fx_PoisonSplat_Fade': fade}

def drop_(cv, x, y, r):
    """Gota suelta con contorno y brillo."""
    t = Canvas(cv.w, cv.h)
    if r < 1.1:
        t.put(x, y, C(VENOM[1])); t.put(x - 1, y, C(VENOM[2]))
    else:
        ellipse(t, x, y, r, r * .9, VENOM)
        t.put_in(x - r * .45, y - r * .45, C(VENOM[3]))
    t.outline()
    for p, col in t.px.items():
        if p not in cv.px or col != OUTL: cv.px[p] = col

def burst_frame(i):
    """Reventón de radio 1,5 u (24 px), irregular: la bola se aplasta y revienta a chorretones desiguales, más cargados
    hacia tres lados; gotas de tamaños distintos que vuelan a distancias distintas, se estiran en vuelo y caen.
    Cuadro que cuenta: el 1."""
    cv = Canvas(BURST_N); c = BURST_N / 2
    tongues, drops = _splat_plan()
    rnd = random.Random(12)
    if i == 0:                                              # se aplasta contra el suelo, ya deformada
        ball(cv, 'down', .4, BALL_R * 1.3, (c, c + 7), swell=.15, flash=True, bumps=1.0, neck=False)
        return cv
    if i == 1:                                              # revienta: núcleo deforme con chorretones que salen
        t = Canvas(BURST_N)
        lobes = [(c, c, 7.0)]
        for (a, ln, th) in tongues:
            for j in range(1, 4):
                lobes.append((c + math.cos(a) * (4 + 3.2 * j) * ln, c + math.sin(a) * (4 + 3.2 * j) * ln * .82, (4.2 - j) * th))
        for k in range(5):
            a = rnd.uniform(0, 2 * math.pi); lobes.append((c + math.cos(a) * 5, c + math.sin(a) * 4, rnd.uniform(2, 3.4)))
        inside = lambda x, y: sum(rr * rr / max(.01, (x - lx) ** 2 + (y - ly) ** 2) for lx, ly, rr in lobes) >= 1.0
        fill(t, inside, c, c, 10, 9, FLASH, box=(c - 22, c - 20, c + 22, c + 20))
        t.outline(); t.clean()
        for p, col in t.px.items(): cv.px[p] = col
        for (a, d, sz) in drops[:9]:
            drop_(cv, c + math.cos(a) * 13 * d, c + math.sin(a) * 11 * d, 1.4 * sz)
        sparks(cv, c - 3, c - 3, 2)
        return cv
    prog = (0, 0, .55, .8, .95, 1.0, 1.0)[i]
    landed = set()
    if i <= 3:                                              # restos del núcleo, rotos en pedazos
        for (dx, dy, rr) in ((-2, 1, 4.2), (3, -1, 3.4), (0, 3, 2.6)):
            q = (4.6 - i) / 2.6
            if rr * q > .9: drop_(cv, c + dx * (1 + i * .4), c + dy * (1 + i * .4), rr * q)
    for (a, d, sz) in drops:
        dist = 24 * d * prog
        x, y = c + math.cos(a) * dist, c + math.sin(a) * dist * .82
        if i <= 4:
            s_ = max(.7, (1.7 - .25 * (i - 2)) * sz)
            if s_ > 1.4 and i <= 3:                         # las gordas se estiran en vuelo
                t = Canvas(BURST_N)
                rot_ellipse(t, x, y, s_ * 1.7, s_ * .85, math.atan2(math.sin(a) * .82, math.cos(a)), VENOM)
                t.put_in(x - s_ * .4, y - s_ * .4, C(VENOM[3]))
                t.outline()
                for p, col in t.px.items(): cv.px.setdefault(p, col)
            else:
                drop_(cv, x, y, s_)
            if d * sz > .8:                                  # estela
                for k in (1, 2):
                    cv.put_out(c + math.cos(a) * (dist - 2.4 * k), c + math.sin(a) * (dist - 2.4 * k) * .82, C(VENOM[2 if k == 1 else 1]))
        else:                                                # caen y salpican el suelo
            rr = 2.0 * sz * (1.15 if i == 5 else .85)
            landed |= {(xx, yy) for yy in range(int(y - rr) - 1, int(y + rr) + 2) for xx in range(int(x - rr * 1.3) - 1, int(x + rr * 1.3) + 2)
                       if ((xx + .5 - x) / (rr * 1.3)) ** 2 + ((yy + .5 - y) / (rr * .8)) ** 2 <= 1 and 0 <= xx < BURST_N and 0 <= yy < BURST_N}
    if landed:
        t = splat_paint(BURST_N, landed, alpha=235 if i == 5 else 170)
        for p, v in t.px.items(): cv.px.setdefault(p, v)
    return cv

# =================================================================================================
# Escenas de revisión
# =================================================================================================
def ground(w, h, seed=3, tones=('#1c2e20', '#213626', '#273f2b', '#2f4a32')):
    rnd = random.Random(seed)
    im = Image.new('RGBA', (w, h), C(tones[0]))
    for _ in range(w * h // 18):
        x, y = rnd.randrange(w), rnd.randrange(h)
        t = C(tones[rnd.choice((1, 1, 1, 2, 2, 3))])
        for dx in range(rnd.choice((1, 2, 3))):
            if x + dx < w: im.putpixel((x + dx, y), t)
    return im

def paste(im, cv, x, y, flip=False):
    sp = as_img(cv)
    if flip: sp = sp.transpose(Image.FLIP_LEFT_RIGHT)
    im.alpha_composite(sp, (int(round(x)), int(round(y))))

class Spit:
    """Simula una bola del juego: avanza en línea recta, deja sellos y se hincha y estalla al final."""
    def __init__(s, x, y, dx, dy, speed=32, life=4.0, trail_life=3.0, small=False):
        l = math.hypot(dx, dy); s.d = (dx / l, dy / l)
        s.x0, s.y0, s.speed, s.life, s.tl, s.small = x, y, speed, life, trail_life, small
        s.cfg = THIN if small else TRAIL
        s.stamps = []                                         # (x, y, nacimiento, variante, brillo)
        s.dist = 0.0
        s.rnd = random.Random(int(x * 31 + y * 17 + dx * 7 + dy * 13))
        s.phase = s.rnd.uniform(0, 6.3)
    def view(s):
        dx, dy = s.d
        if abs(dx) >= abs(dy): return 'side', dx < 0
        return ('down' if dy > 0 else 'up'), False
    def pos(s, t):
        d = s.speed * min(t, s.life)
        return s.x0 + s.d[0] * d, s.y0 + s.d[1] * d
    def update(s, t):
        d = s.speed * min(t, s.life)
        while s.dist <= d:
            if s.small:                                       # la fina serpentea un poco: onda suave más algo de ruido
                j = round(1.3 * math.sin(s.dist / 7 + s.phase) + s.rnd.uniform(-.45, .45))
            else:
                j = s.rnd.choice((-1, 0, 0, 1)) * s.cfg['jitter']   # vaivén lateral de 1 px
            x, y = s.x0 + s.d[0] * s.dist - s.d[1] * j, s.y0 + s.d[1] * s.dist + s.d[0] * j
            nv = len(THIN_VARIANTS if s.small else TRAIL_VARIANTS)
            v = s.rnd.randrange(nv - 1) if s.rnd.random() > (.12 if s.small else .08) else nv - 1
            s.stamps.append((round(x), round(y), s.dist / s.speed, v, s.rnd.random() < .25))
            s.dist += s.cfg['step']

def stamp_frame(age, life):
    if age < .09: return 0
    if age < .18: return 1
    if age < life - .5: return 2
    k = 3 + int((age - (life - .5)) / .1)
    return k if k <= 7 else None

def render_scene(path, spits, seconds, size, burst, pud_big, pud_small, minis_from=None, ms=50, bg_tones=None):
    w, h = size
    base = ground(w, h, tones=bg_tones) if bg_tones else ground(w, h)
    T = {False: trail_set(TRAIL, TRAIL_VARIANTS, True), True: trail_set(THIN, THIN_VARIANTS, False)}
    SH = [shine(i) for i in range(4)]
    sheets = {}
    for small in (False, True):
        for v in ('side', 'down', 'up'):
            sheets[(small, v, 'roll')] = small_frames(v) if small else roll_frames(v, 8, BALL_R, 24)
            if not small: sheets[(small, v, 'swell')] = swell_frames(v)
    frames, spawned = [], False
    spits = list(spits)
    for f in range(int(seconds * 1000 / ms)):
        t = f * ms / 1000
        im = base.copy()
        if minis_from and not spawned and t >= minis_from[0].life + .42:
            sp = minis_from[0]; ex, ey = sp.pos(sp.life)
            for a in range(4):
                ang = a * math.pi / 2
                m = Spit(ex, ey, math.cos(ang), math.sin(ang), speed=40, life=1.0, trail_life=3.0, small=True)
                m.t0 = t; spits.append(m)
            spawned = True
        layers = ([], [], [], [], [])                          # bordes, cuerpos, brillos, charcos y reventones, bolas
        for sp in spits:
            lt = t - getattr(sp, 't0', 0.0)
            if lt < 0: continue
            sp.update(lt)
            cfg = sp.cfg; n = cfg['n']
            for j, (x, y, born, v, shn) in enumerate(sp.stamps):
                k = stamp_frame(lt - born, sp.tl)
                if k is None: continue
                rims, bodies = T[sp.small][v]
                layers[0].append((rims[k], x - n / 2, y - n / 2)); layers[1].append((bodies[k], x - n / 2, y - n / 2))
                if not sp.small and k == 2 and shn:
                    layers[2].append((SH[int((lt * 1000 / 140) + j) % 4], x - 6, y - 6))
            x, y = sp.pos(lt)
            view, flip = sp.view()
            cs = 12 if sp.small else 24
            gx, gy = CONTACT[cs]
            if lt < sp.life:
                if sp.small:
                    fr = sheets[(True, view, 'roll')][int(lt * 1000 / SMALL_MS) % 4]
                elif lt >= sp.life - .6:
                    fr = sheets[(False, view, 'swell')][min(5, int((lt - (sp.life - .6)) * 1000 / SWELL_MS))]
                else:
                    fr = sheets[(False, view, 'roll')][int(lt * 1000 / ROLL_MS) % 8]
                layers[4].append((fr, x - (cs - gx if flip else gx), y - gy, flip))
            else:
                bt = lt - sp.life
                if sp.small:
                    k = int(bt * 1000 / 90)
                    if k < len(pud_small): layers[3].append((pud_small[k], x - 8, y - 8))
                else:
                    k = int(bt * 1000 / 60)
                    pk = int(bt * 1000 / 90)
                    if pk < len(pud_big): layers[3].append((pud_big[pk], x - SPLAT_N / 2, y - SPLAT_N / 2))
                    if k < len(burst): layers[3].append((burst[k], x - BURST_N / 2, y - BURST_N / 2 - 3))
        for L in layers[:4]:
            for item in L: paste(im, item[0], item[1], item[2])
        for fr, x, y, flip in layers[4]: paste(im, fr, x, y, flip)
        frames.append(im)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    big = [fr.resize((w * 3, h * 3), Image.NEAREST).convert('RGB') for fr in frames]
    big[0].save(path, save_all=True, append_images=big[1:], duration=ms, loop=0)
    return frames

def load_sheet(path, cell):
    im = Image.open(path).convert('RGBA')
    return [im.crop((i * cell, 0, (i + 1) * cell, cell)) for i in range(im.width // cell)]

# =================================================================================================
# Salida
# =================================================================================================
if __name__ == '__main__':
    E = {}
    for v in ('side', 'down', 'up'):
        E[f'Proj_PoisonSpit_Roll_{v}'] = roll_frames(v, 8, BALL_R, 24)
    for v in ('side', 'down', 'up'):
        E[f'Proj_PoisonSpit_Swell_{v}'] = swell_frames(v)
    for v in ('side', 'down', 'up'):
        E[f'Proj_PoisonSpitSmall_Roll_{v}'] = small_frames(v)
    T = {}
    for k, (rims, bodies) in enumerate(trail_set(TRAIL, TRAIL_VARIANTS, True)):
        T[f'Fx_SlimeTrail_Rim_{k}'] = rims; T[f'Fx_SlimeTrail_Body_{k}'] = bodies
    for k, (rims, bodies) in enumerate(trail_set(THIN, THIN_VARIANTS, False)):
        T[f'Fx_SlimeTrailThin_Rim_{k}'] = rims; T[f'Fx_SlimeTrailThin_Body_{k}'] = bodies
    T['Fx_SlimeTrail_Shine'] = [shine(i) for i in range(4)]
    E2 = {'Fx_PoisonSpit_Burst': [burst_frame(i) for i in range(7)]}
    E2.update(splat_sheets())
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    for name, frames in E2.items():
        save_sheet(os.path.join(OUT, 'Effects', name + '.png'), frames)
        gif(os.path.join(OUT, 'gif', name + '.gif'), frames, 60 if 'Burst' in name else 90, BG, scale=4)
    preview(os.path.join(OUT, 'vista_previa_reventon.png'), E2, BG, scale=4)
    for name, frames in {**E, **T}.items():
        save_sheet(os.path.join(OUT, 'Effects', name + '.png'), frames)
    for name, frames in E.items():
        gif(os.path.join(OUT, 'gif', name + '.gif'), frames, SWELL_MS if 'Swell' in name else SMALL_MS if 'Small' in name else ROLL_MS, BG, scale=6)
    preview(os.path.join(OUT, 'vista_previa_bola.png'), E, BG, scale=6)
    preview(os.path.join(OUT, 'vista_previa_banda.png'), T, BG, scale=6)

    old = os.path.join(HERE, '..', 'floor3-bodies', 'Effects')
    burst = E2['Fx_PoisonSpit_Burst']
    pud_big = E2['Fx_PoisonSplat_Appear'] + E2['Fx_PoisonSplat_Idle'] * 6 + [E2['Fx_PoisonSplat_Fade'][0]] * 6
    pud_small = load_sheet(os.path.join(old, 'Fx_PoisonPuddleSmall_Appear.png'), 16) + load_sheet(os.path.join(old, 'Fx_PoisonPuddleSmall_Fade.png'), 16)
    esc = os.path.join(OUT, 'gif', 'escenas')
    scene = render_scene(os.path.join(esc, 'escena_direcciones.gif'),
                         [Spit(20, 30, 1, 0), Spit(200, 14, 0, 1), Spit(120, 148, 0, -1), Spit(236, 150, -1, -.5)],
                         8.0, (256, 176), burst, pud_big, pud_small)
    first = Spit(30, 60, 1, 0)
    render_scene(os.path.join(esc, 'escena_camada.gif'), [first], 8.5, (224, 120), burst, pud_big, pud_small, minis_from=[first])
    # Fotogramas fijos de la escena sobre los tres tramos del piso, para la revisión
    stills = []
    for tones in (('#1c2e20', '#213626', '#273f2b', '#2f4a32'), ('#22261f', '#2a2e25', '#32352b', '#3a3d31'),
                  ('#1d1a1d', '#252125', '#2e292b', '#363033')):
        fr = render_scene(os.path.join(esc, '_tmp.gif'), [Spit(14, 22, 1, 0), Spit(150, 8, 0, 1), Spit(104, 92, -1, -.5)],
                          3.0, (176, 104), burst, pud_big, pud_small, bg_tones=tones)
        stills.append(fr[-1])
    os.remove(os.path.join(esc, '_tmp.gif'))
    W, H = stills[0].size
    sheet = Image.new('RGBA', (W * 3 + 8, H), C('#000000'))
    for i, s in enumerate(stills): sheet.alpha_composite(s, (i * (W + 4), 0))
    sheet.resize((sheet.width * 3, H * 3), Image.NEAREST).save(os.path.join(OUT, 'fondos_banda.png'))
    print('Listo:', OUT)
