"""Láminas de escala del Arthropleura (tanda 6, paso 0): NO es el arte final.

Un Arthropleura provisional (placas ovaladas con paranotos de punta dorada, segmentos de 48 px = 3 u) sobre salas del
Bosque humeante armadas con los tiles de la tanda 1a, con la rana del jugador (32 px) y el recuadro de la cámara del
juego (480×270 px = 30×16,9 u, la de CoreSetup.cs). Sirve para decidir el tamaño del jefe y de su sala.

- F1: claro pequeño (suelo 20×11 u, la sala Small). El cuerpo rodea la sala por dentro de la espesura (fantasma) y se
  ve en los huecos; la cabeza asoma por uno.
- F2 versión A: sala ×3 de área (suelo 35×19 u). F2 versión B: sala ×3 de lado (suelo 60×33 u). El cuerpo cruza la
  sala como una pared. Recuadros: cámara normal y alejada.
- F3: el anillo cerrándose alrededor del jugador (en la sala A).

Uso: python generar_escala_arthropleura.py          (láminas del paso 0)
     python generar_escala_arthropleura.py grande   (sala B de 60×33 u y segmentos de 64 px)
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
MT = os.path.normpath(os.path.join(HERE, '..', '..'))
sys.path.insert(0, os.path.join(MT, 'floor3-environment'))
import generar_tilesets_piso3 as T                     # sample_room() y los tiles del tramo
from base_piso3 import *                               # noqa: F401,F403
from PIL import Image, ImageDraw, ImageFont

U = 16                                                 # px por unidad
CAM = (480, 270)                                       # vista del juego en px
SEG_W = 48                                             # ancho del segmento (con los paranotos)
STEP = 32                                              # distancia entre segmentos a lo largo del cuerpo (2 u)
N_SEG = 22
K = 1.0                                                # escala del dibujo provisional (segmento / 48 px)
PLATE = ramp('#5a3a5a', 5)
GOLD = ramp('#c89a48', 4)
PROPS = os.path.join(MT, 'floor3-environment', 'Smoke', 'props')
FROG = os.path.join(MT, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')

def font(n):
    for name in ('arialbd.ttf', 'arial.ttf', 'DejaVuSans.ttf'):
        try: return ImageFont.truetype(name, n)
        except OSError: pass
    return ImageFont.load_default()

# -------------------------------------------------------------------------------------------------
# Sala con los tiles del Bosque humeante
# -------------------------------------------------------------------------------------------------
_B = None
def room(rows):
    global _B
    if _B is None: _B = T.build('Smoke')
    T.SAMPLE = rows
    return T.sample_room(_B, 1)[1]

def rect_room(W, H, floor, mud=(), gaps=(), carve=()):
    """Mapa W×H de pared con el suelo floor = (x0, y0, x1, y1) inclusive; mud y gaps son listas de rectángulos."""
    g = [['#'] * W for _ in range(H)]
    def fill(r, c):
        x0, y0, x1, y1 = r
        for y in range(max(0, y0), min(H, y1 + 1)):
            for x in range(max(0, x0), min(W, x1 + 1)): g[y][x] = c
    fill(floor, '.')
    for r in gaps: fill(r, '.')
    for r in mud: fill(r, '~')
    for (x, y) in carve: g[y][x] = '.'
    return [''.join(r) for r in g]

def frame(path, i, w):
    im = Image.open(path).convert('RGBA')
    return im.crop((i * w, 0, i * w + w, im.height))

def props(im, spots, seed):
    rnd = random.Random(seed)
    kinds = [('Fern_Leafy_Sway', 84), ('Fern_Tree_Sway', 84), ('Fern_Bent_Sway', 88), ('Fern_Young_Sway', 44),
             ('Fern_Sparse_Sway', 92)]
    for (x, y, k) in spots:
        if k == 'fern': name, w = rnd.choice(kinds); spr = frame(os.path.join(PROPS, name + '.png'), 0, w)
        elif k == 'tree': spr = Image.open(os.path.join(PROPS, 'Obstacle_Sigillaria.png')).convert('RGBA')
        elif k == 'log': spr = Image.open(os.path.join(PROPS, 'Obstacle_FallenLog.png')).convert('RGBA')
        elif k == 'sac': spr = frame(os.path.join(PROPS, 'Methane_Sac_Idle.png'), 0, 24)
        elif k == 'pool': spr = frame(os.path.join(PROPS, 'Puddle_Sludge_Island.png'), 0, 50)
        else: continue
        if rnd.random() < .5 and k == 'fern': spr = spr.transpose(Image.FLIP_LEFT_RIGHT)
        im.alpha_composite(spr, (int(x * U - spr.width / 2), int(y * U - spr.height + (spr.height / 2 if k == 'pool' else 4))))

# -------------------------------------------------------------------------------------------------
# Cuerpo provisional
# -------------------------------------------------------------------------------------------------
def chain(path, n, step):
    """Reparte n puntos a lo largo de la polilínea path (en px) cada step px, desde el principio (la cabeza)."""
    out, acc, want = [], 0.0, 0.0
    for (ax, ay), (bx, by) in zip(path, path[1:]):
        L = math.hypot(bx - ax, by - ay)
        while want <= acc + L and len(out) < n:
            t = (want - acc) / L if L else 0
            ang = math.atan2(ay - by, ax - bx)              # mirando hacia la cabeza
            out.append((ax + (bx - ax) * t, ay + (by - ay) * t, ang))
            want += step
        acc += L
    return out

def poly(cx, cy, ang, pts):
    ca, sa = math.cos(ang), math.sin(ang)
    return [(cx + (u * ca - v * sa) * K, cy + (u * sa + v * ca) * K) for (u, v) in pts]

def oval(n, a, b, du=0):
    return [(du + a * math.cos(2 * math.pi * k / n), b * math.sin(2 * math.pi * k / n)) for k in range(n)]

def draw_seg(d, x, y, ang, kind, ghost=False):
    oc = (215, 180, 235, 200) if ghost else OUTL                 # fantasma: silueta clara bajo la espesura
    fillc = lambda c: ((120, 80, 130, 90) if ghost else C(c))
    if kind == 'seg':
        for s in (-1, 1):                                       # patas
            for k in (-6, 2):
                d.line(poly(x, y, ang, [(k, s * 14), (k - 5, s * 23)]), fill=oc, width=max(1, round(2 * K)))
        for s in (-1, 1):                                       # paranotos con pincho dorado
            p = poly(x, y, ang, [(8, s * 10), (2, s * 24), (-6, s * 22), (-10, s * 10)])
            d.polygon(p, fill=fillc(PLATE[1]), outline=oc)
            d.polygon(poly(x, y, ang, [(2, s * 24), (-6, s * 22), (-2, s * 19)]), fill=fillc(GOLD[2]), outline=oc)
        d.polygon(poly(x, y, ang, oval(16, 12, 15)), fill=fillc(PLATE[2]), outline=oc)
        if not ghost:
            d.line(poly(x, y, ang, [(-8, -10), (-8, 10)]), fill=C(PLATE[1]), width=2)   # borde de la placa
            d.line(poly(x, y, ang, [(6, -6), (8, 2)]), fill=C(PLATE[3]), width=1)
    elif kind == 'head':
        for s in (-1, 1):                                       # antenas
            d.line(poly(x, y, ang, [(14, s * 8), (30, s * 18), (40, s * 14)]), fill=C(PLATE[1]) if not ghost else oc, width=max(1, round(2 * K)))
            d.polygon(poly(x, y, ang, [(10, s * 6), (26, s * 12), (30, s * 3), (18, s * 2)]), fill=fillc(GOLD[2]), outline=oc)   # mandíbulas
        d.polygon(poly(x, y, ang, oval(18, 15, 19)), fill=fillc(PLATE[2]), outline=oc)
        if not ghost:
            for s in (-1, 1): d.ellipse([p for q in poly(x, y, ang, [(7, s * 9)]) for p in (q[0] - 2 * K, q[1] - 2 * K, q[0] + 2 * K, q[1] + 2 * K)], fill=C('#e04030'))
    elif kind == 'tail':
        for s in (-1, 1):
            d.line(poly(x, y, ang, [(-6, s * 5), (-22, s * 10)]), fill=C(GOLD[1]) if not ghost else oc, width=max(1, round(3 * K)))
        d.polygon(poly(x, y, ang, oval(14, 10, 11)), fill=fillc(PLATE[1]), outline=oc)

def arthropleura(im, path, hidden=lambda x, y: False, n=N_SEG):
    """Dibuja el cuerpo a lo largo de path (px); hidden(x, y) = segmento bajo la espesura (se dibuja fantasma)."""
    pts = chain(path, n + 2, STEP)
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    for i in range(len(pts) - 1, -1, -1):                       # de la cola a la cabeza
        x, y, a = pts[i]
        kind = 'head' if i == 0 else 'tail' if i == len(pts) - 1 else 'seg'
        draw_seg(d, x, y, a, kind, ghost=hidden(x, y))
    im.alpha_composite(lay)
    return pts

def arrows(d, cx, cy, targets, s, zoom=1.0):
    """Flechas en el borde de la cámara hacia la cabeza y la cola cuando están fuera de pantalla."""
    hw, hh = CAM[0] * zoom / 2 / U, CAM[1] * zoom / 2 / U
    for (tx, ty, col, label) in targets:
        dx, dy = tx - cx, ty - cy
        if abs(dx) <= hw and abs(dy) <= hh: continue
        k = min(hw / abs(dx) if dx else 1e9, hh / abs(dy) if dy else 1e9) * .96
        ex, ey = (cx + dx * k) * U * s, (cy + dy * k) * U * s
        a = math.atan2(dy, dx); L = 22
        tri = [(ex + math.cos(a) * L * .6, ey + math.sin(a) * L * .6),
               (ex + math.cos(a + 2.5) * L * .5, ey + math.sin(a + 2.5) * L * .5),
               (ex + math.cos(a - 2.5) * L * .5, ey + math.sin(a - 2.5) * L * .5)]
        d.polygon(tri, fill=col, outline=(10, 10, 14))
        d.text((ex - math.cos(a) * 30 - 20, ey - math.sin(a) * 26 - 8), label, fill=col, font=font(14), stroke_width=3, stroke_fill=(10, 10, 14))

def player(im, x, y):
    f = frame(FROG, 0, 32)
    im.alpha_composite(f, (int(x * U - 16), int(y * U - 16)))

# -------------------------------------------------------------------------------------------------
# Anotaciones (sobre la imagen ampliada)
# -------------------------------------------------------------------------------------------------
def cam_box(d, cx, cy, zoom, col, label, s):
    w, h = CAM[0] * zoom * s, CAM[1] * zoom * s
    x0, y0 = cx * U * s - w / 2, cy * U * s - h / 2
    dash = 10
    for k in range(0, int(w), dash * 2):
        d.line([(x0 + k, y0), (x0 + min(w, k + dash), y0)], fill=col, width=3)
        d.line([(x0 + k, y0 + h), (x0 + min(w, k + dash), y0 + h)], fill=col, width=3)
    for k in range(0, int(h), dash * 2):
        d.line([(x0, y0 + k), (x0, y0 + min(h, k + dash))], fill=col, width=3)
        d.line([(x0 + w, y0 + k), (x0 + w, y0 + min(h, k + dash))], fill=col, width=3)
    d.text((max(8, x0 + 8), max(6, y0 + 6)), label, fill=col, font=font(14 if s < 3 else 18), stroke_width=3, stroke_fill=(10, 10, 14))

def finish(im, title, notes, cams, s, out, marks=None):
    big = im.resize((im.width * s, im.height * s), Image.NEAREST)
    d = ImageDraw.Draw(big)
    for (cx, cy, z, col, label) in cams: cam_box(d, cx, cy, z, col, label, s)
    if marks: arrows(d, marks[0], marks[1], marks[2], s)
    bar_y = big.height - 30                                      # regla: 3 u = 1 segmento
    d.rectangle([20, bar_y, 20 + SEG_W * s, bar_y + 8], fill=(240, 230, 200))
    d.text((28 + SEG_W * s, bar_y - 6), f"{SEG_W // 16} u = 1 segmento ({SEG_W} px)", fill=(240, 230, 200), font=font(16), stroke_width=3, stroke_fill=(10, 10, 14))
    head = Image.new('RGBA', (big.width, 30 + 22 * len(notes)), (16, 18, 16, 255))
    hd = ImageDraw.Draw(head)
    hd.text((14, 6), title, fill=(255, 240, 200), font=font(20))
    for k, n_ in enumerate(notes): hd.text((14, 32 + 22 * k), n_, fill=(200, 210, 200), font=font(15))
    full = Image.new('RGBA', (big.width, big.height + head.height), (16, 18, 16, 255))
    full.alpha_composite(head, (0, 0)); full.alpha_composite(big, (0, head.height))
    full.convert('RGB').save(out)
    print('  ', os.path.basename(out), full.size)

WHITE = (255, 255, 255)
YEL = (255, 214, 90)
ORA = (255, 140, 60)

# -------------------------------------------------------------------------------------------------
# Láminas
# -------------------------------------------------------------------------------------------------
def lamina_f1():
    W, H = 40, 31
    fx0, fy0, fx1, fy1 = 10, 10, 29, 20                             # suelo 20×11
    ring = (6.5, 6.5, 33.5, 24.5)                                   # por dónde reptea, dentro de la espesura (u)
    gaps = [(14, 6, 15, 9), (24, 6, 25, 9), (6, 14, 9, 15), (30, 16, 33, 17), (19, 21, 20, 25)]
    rows = rect_room(W, H, (fx0, fy0, fx1, fy1), mud=[(12, 18, 14, 19)], gaps=gaps)
    im = room(rows)
    props(im, [(13, 12, 'fern'), (27, 13, 'fern'), (16, 19.5, 'fern'), (25.5, 18.5, 'sac'), (12, 12.5, 'sac')], 3)
    open_ = lambda x, y: rows[int(y // U)][int(x // U)] != '#' if 0 <= int(y // U) < H and 0 <= int(x // U) < W else False
    hidden = lambda x, y: not open_(x, y)
    rx0, ry0, rx1, ry1 = [v * U for v in ring]
    hx = 14.5 * U
    path = [(hx, 11.5 * U), (hx, ry0), (rx0, ry0), (rx0, ry1), (rx1, ry1), (rx1, ry0), (24.5 * U, ry0)]
    arthropleura(im, path, hidden)
    player(im, 20, 16)
    finish(im, 'Arthropleura · F1 en el claro pequeño (suelo 20×11 u)',
           ['Segmentos de 48 px (3 u), 22 segmentos + cabeza y cola ≈ 49 u de largo. Fantasma = bajo la espesura; sólido = en los huecos.',
            'La cabeza asoma por el hueco de arriba a la izquierda. Recuadro blanco = lo que ve la cámara (30×16,9 u).'],
           [(20, 16, 1, WHITE, 'cámara (30×16,9 u)')], 2, os.path.join(HERE, 'vista_previa_escala_F1.png'))

def lamina_f2(version):
    if version == 'A':
        fw, fh, s, zooms = 35, 19, 2, [(1, WHITE, 'cámara normal'), (1.3, YEL, 'cámara alejada ×1,3 (cabe toda la sala)')]
    else:
        fw, fh, s, zooms = 60, 33, 1, [(1, WHITE, 'cámara normal'), (1.5, YEL, 'alejada ×1,5'), (2.2, ORA, 'alejada ×2,2 (toda la sala)')]
    W, H = fw + 4, fh + 6
    fx0, fy0 = 2, 3
    rnd = random.Random(7 if version == 'A' else 9)
    mud = []
    for k in range(3 if version == 'A' else 7):
        x = rnd.randint(fx0 + 3, fx0 + fw - 7); y = rnd.randint(fy0 + 2, fy0 + fh - 5)
        mud.append((x, y, x + rnd.randint(2, 4), y + rnd.randint(1, 3)))
    rows = rect_room(W, H, (fx0, fy0, fx0 + fw - 1, fy0 + fh - 1), mud=mud)
    im = room(rows)
    spots = []
    for k in range(int(fw * fh / 45)):
        x = rnd.uniform(fx0 + 2, fx0 + fw - 2); y = rnd.uniform(fy0 + 2, fy0 + fh - 1)
        spots.append((x, y, rnd.choice(['fern', 'fern', 'fern', 'tree', 'log', 'sac', 'pool'])))
    props(im, sorted(spots, key=lambda p: p[1]), 5)
    cx, cy = fx0 + fw / 2, fy0 + fh / 2
    if version == 'A':                                              # el cuerpo cruza la sala como una pared en S
        path = [(fx0 + fw * .78, fy0 + fh * .2), (fx0 + fw * .62, fy0 + fh * .35), (fx0 + fw * .45, fy0 + fh * .55),
                (fx0 + fw * .30, fy0 + fh * .78), (fx0 + fw * .12, fy0 + fh * .85), (fx0 + fw * .05, fy0 + fh * .55),
                (fx0 + fw * .1, fy0 + fh * .15)]
        px, py = fx0 + fw * .7, fy0 + fh * .62
    else:
        path = [(fx0 + fw * .62, fy0 + fh * .22), (fx0 + fw * .52, fy0 + fh * .42), (fx0 + fw * .45, fy0 + fh * .62),
                (fx0 + fw * .32, fy0 + fh * .72), (fx0 + fw * .2, fy0 + fh * .6), (fx0 + fw * .15, fy0 + fh * .3)]
        px, py = fx0 + fw * .62, fy0 + fh * .6
    arthropleura(im, [(x * U, y * U) for (x, y) in path])
    player(im, px, py)
    cams = [(px, py, 1, WHITE, 'cámara normal (30×16,9 u)')] + [(cx, cy, z, c, l) for (z, c, l) in zooms[1:]]
    title = ('Arthropleura · F2, versión A: sala ×3 de área (suelo 35×19 u)' if version == 'A'
             else 'Arthropleura · F2, versión B: sala ×3 de lado (suelo 60×33 u)')
    notes = ['El cuerpo (≈ 49 u) cruza la sala como una pared. Helechos, Sigillarias, troncos, odres y charcas de lodo repartidos.',
             'Recuadros: lo que ve la cámara normal centrada en la rana y la cámara alejada centrada en la sala.']
    if version == 'B': notes.append('Ojo: con la cámara a ×2,2 la rana (2 u) se vería de ~14 px de pantalla: muy pequeña.')
    finish(im, title, notes, cams, s, os.path.join(HERE, f'vista_previa_escala_F2_{version}.png'))

def lamina_f3():
    fw, fh = 35, 19
    W, H = fw + 4, fh + 6
    fx0, fy0 = 2, 3
    rows = rect_room(W, H, (fx0, fy0, fx0 + fw - 1, fy0 + fh - 1), mud=[(6, 6, 8, 7), (28, 16, 31, 18)])
    im = room(rows)
    cx, cy = fx0 + fw / 2, fy0 + fh / 2 + .5
    R = 7.4
    path = [((cx + R * math.cos(a)) * U, (cy + R * .92 * math.sin(a)) * U) for a in
            [-.35 - k * 2 * math.pi / 60 for k in range(0, 56)]]   # casi cerrado: queda un hueco
    arthropleura(im, path)
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)   # fuego en círculo (aviso)
    for k in range(40):
        a = k * 2 * math.pi / 40
        x, y = (cx + (R - 2.6) * math.cos(a)) * U, (cy + (R - 2.6) * .92 * math.sin(a)) * U
        d.ellipse([x - 3, y - 3, x + 3, y + 3], fill=(248, 140, 42, 200))
    im.alpha_composite(lay)
    player(im, cx, cy)
    finish(im, 'Arthropleura · F3: el anillo se cierra (en la sala A)',
           ['Anillo de ≈ 15 u de diámetro con el cuerpo entero; queda un hueco entre la cabeza y la cola para escapar.',
            'Puntos naranjas = fuego en círculo dentro del anillo. Recuadro = cámara normal.'],
           [(cx, cy, 1, WHITE, 'cámara normal (30×16,9 u)')], 2, os.path.join(HERE, 'vista_previa_escala_F3.png'))

# -------------------------------------------------------------------------------------------------
# Versión grande (pedida por el usuario): sala B de 60×33 u y un cuerpo que la cruza de punta a punta
# -------------------------------------------------------------------------------------------------
def grande(seg_px=64, n=28, step_u=2.6):
    global K, STEP, N_SEG, SEG_W
    K, STEP, N_SEG, SEG_W = seg_px / 48, step_u * U, n, seg_px
    largo = n * step_u + 2 * step_u
    tag = f'{seg_px} px ({seg_px / 16:g} u), {n} segmentos a {step_u:g} u ≈ {largo:.0f} u de largo'
    return largo, tag

def big_room(seed):
    fw, fh = 60, 33
    W, H, fx0, fy0 = fw + 4, fh + 6, 2, 3
    rnd = random.Random(seed)
    mud = []
    for k in range(7):
        x = rnd.randint(fx0 + 3, fx0 + fw - 7); y = rnd.randint(fy0 + 2, fy0 + fh - 5)
        mud.append((x, y, x + rnd.randint(2, 4), y + rnd.randint(1, 3)))
    rows = rect_room(W, H, (fx0, fy0, fx0 + fw - 1, fy0 + fh - 1), mud=mud)
    im = room(rows)
    spots = []
    for k in range(int(fw * fh / 45)):
        x = rnd.uniform(fx0 + 2, fx0 + fw - 2); y = rnd.uniform(fy0 + 2, fy0 + fh - 1)
        spots.append((x, y, rnd.choice(['fern', 'fern', 'fern', 'tree', 'log', 'sac', 'pool'])))
    props(im, sorted(spots, key=lambda p: p[1]), 5)
    return im, fw, fh, fx0, fy0

def lamina_f2_grande():
    largo, tag = grande()
    im, fw, fh, fx0, fy0 = big_room(9)
    pts = [(fx0 + 1.5 + (fw - 3) * t, fy0 + fh * (.5 + .17 * math.sin(t * 2 * math.pi * .9 + .6))) for t in [k / 40 for k in range(41)]]
    pts = pts[::-1]                                                   # la cabeza a la derecha
    pts.append((fx0 + 2.5, fy0 + fh * .12))                           # lo que sobra se curva por la esquina
    body = arthropleura(im, [(x * U, y * U) for (x, y) in pts])
    px, py = fx0 + fw * .58, fy0 + fh * .8
    player(im, px, py)
    head, tail = body[0], body[-1]
    cams = [(px, py, 1, WHITE, 'cámara normal ×1 (30×16,9 u)'), (px, py, 1.5, YEL, 'cámara ×1,5 (45×25 u)')]
    finish(im, 'Arthropleura grande · F2 en la sala B (suelo 60×33 u)',
           [f'Segmento {tag}: cruza la sala de punta a punta y aún se curva por la esquina.',
            'Cámaras centradas en la rana (la cámara sigue al jugador y no se aleja al centro de la sala, como en Gungeon).',
            'Flechas = indicadores de cabeza (rojo) y cola (dorado) en el borde de la cámara normal cuando están fuera.'],
           cams, 1, os.path.join(HERE, 'vista_previa_escala_F2_B_grande.png'),
           marks=(px, py, [(head[0] / U, head[1] / U, (240, 70, 60), 'cabeza'), (tail[0] / U, tail[1] / U, (232, 190, 90), 'cola')]))

def lamina_f1_grande():
    largo, tag = grande()
    W, H = 42, 33
    fx0, fy0, fx1, fy1 = 11, 11, 30, 21
    ring = (6, 6, 36, 27)
    gaps = [(15, 6, 16, 10), (25, 6, 26, 10), (6, 15, 10, 16), (31, 17, 36, 18), (20, 22, 21, 27)]
    rows = rect_room(W, H, (fx0, fy0, fx1, fy1), mud=[(13, 19, 15, 20)], gaps=gaps)
    im = room(rows)
    props(im, [(14, 13, 'fern'), (28, 14, 'fern'), (17, 20.5, 'fern'), (26.5, 19.5, 'sac'), (13, 13.5, 'sac')], 3)
    open_ = lambda x, y: rows[int(y // U)][int(x // U)] != '#' if 0 <= int(y // U) < H and 0 <= int(x // U) < W else False
    rx0, ry0, rx1, ry1 = [(v + .5) * U for v in ring]
    hx = 15.5 * U
    path = [(hx, 12.5 * U), (hx, ry0), (rx0, ry0), (rx0, ry1), (rx1, ry1), (rx1, ry0), (hx, ry0)]
    arthropleura(im, path, lambda x, y: not open_(x, y))
    player(im, 21, 17)
    finish(im, 'Arthropleura grande · F1 en el claro pequeño (suelo 20×11 u)',
           [f'Segmento {tag}. Cubre unas 4/5 partes de la vuelta por la espesura (≈ 98 u); queda un tramo libre.',
            'Para que los segmentos de 4 u quepan, la espesura alrededor del claro tiene que ser más gruesa (≈ 5 u).'],
           [(21, 17, 1, WHITE, 'cámara normal (30×16,9 u)')], 2, os.path.join(HERE, 'vista_previa_escala_F1_grande.png'))

def lamina_f3_grande():
    largo, tag = grande()
    im, fw, fh, fx0, fy0 = big_room(11)
    cx, cy = fx0 + fw / 2, fy0 + fh / 2
    R = largo / (2 * math.pi) * .97
    path = [((cx + R * math.cos(a)) * U, (cy + R * .9 * math.sin(a)) * U) for a in [-.3 - k * 2 * math.pi / 72 for k in range(0, 70)]]
    arthropleura(im, path)
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    for k in range(60):
        a = k * 2 * math.pi / 60
        x, y = (cx + (R - 3.6) * math.cos(a)) * U, (cy + (R - 3.6) * .9 * math.sin(a)) * U
        d.ellipse([x - 3, y - 3, x + 3, y + 3], fill=(248, 140, 42, 200))
    im.alpha_composite(lay)
    player(im, cx, cy)
    finish(im, 'Arthropleura grande · F3: el anillo se cierra (sala B)',
           [f'Segmento {tag}: con el cuerpo entero, el anillo mide ≈ {2 * R:.0f} u de ancho.',
            'Con la cámara ×1 no se ve el anillo entero (solo 16,9 u de alto); con ×1,5 entra justo.'],
           [(cx, cy, 1, WHITE, 'cámara ×1'), (cx, cy, 1.5, YEL, 'cámara ×1,5')], 1,
           os.path.join(HERE, 'vista_previa_escala_F3_grande.png'))

if __name__ == '__main__':
    if 'grande' in sys.argv[1:]:
        lamina_f2_grande(); lamina_f1_grande(); lamina_f3_grande()
    else:
        lamina_f1(); lamina_f2('A'); lamina_f2('B'); lamina_f3()
