"""Sala del ámbar del piso 3 (paso 6, bloque 2; ver README).

Idea: un claro bajo un Lepidodendron herido que sangra resina; el ámbar se ha apoderado de toda la sala. Igual en
los tres tramos del incendio (es un refugio que el fuego no toca).
- Tileset propio `Amber/`: el mismo motor que los tramos del piso (generar_tilesets_piso3.build, mismas piezas y
  nombres, así encaja con WallRules/RoomPainter) con paletas de resina y detalles de ámbar en el suelo.
- Piezas (`pieces/`): el Lepidodendron gigante y herido de la pared de arriba y el reguero de resina que baja hasta
  el bloque, base del bloque (charco y raíces), resplandor, bloques de ámbar pequeños con insectos, charcos de
  resina que se funden con el suelo, cuentas que destellan y, opcional, heridas pequeñas en la pared.
Hecho con la skill primordia-sprites (pixel.py): 16 px por unidad, luz arriba a la izquierda, contorno OUTL.

Uso: python generar_sala_ambar.py
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
TEMP = os.path.normpath(os.path.join(HERE, '..'))
ENV = os.path.join(TEMP, 'floor3-environment')
sys.path.insert(0, ENV)
from base_piso3 import *
import base_piso3
from PIL import Image

AMB = [C(c) for c in AMBER]                     # #6a2e0e #b0581a #e89a30 #ffcc5a #fff0b0
INSECT = C('#2a1206')

# --- el tramo «Amber»: paletas de resina para el motor del tileset ---------------------------------
base_piso3.TRAMOS['Amber'] = dict(
    nombre='Sala del ámbar',
    floor=['#24160e', '#2c1b11', '#352114', '#402818'],          # tierra empapada de resina (oscura: el ámbar es lo que brilla)
    fleck=['#6a2e0e', '#8a4416', '#b0581a'],
    face=['#1c120c', '#2a1a10', '#3c2514', '#54331a', '#704622'],
    cap=['#1e140c', '#28190e', '#342112', '#432c17'],
    vein=['#b0581a', '#e89a30', '#ffcc5a'],
    leaf=['#2e1c0e', '#4a2e14', '#6e481c', '#946424'],          # hojas doradas, empapadas
    bark=['#24180e', '#3a2614', '#56381a', '#7a5226', '#a8763a'],  # corteza teñida de resina
    root=['#1e1410', '#33241a', '#4c3828', '#6a4e34', '#8e6e48'],
    seal=['#b0581a', '#e89a30', '#ffcc5a', '#fff0b0'],
)
import generar_tilesets_piso3 as TS
TS.SEED['Amber'] = 420

# --- detalles de ámbar en el suelo (sustituyen a Floor_1..5) ---------------------------------------
def bead(cv, x, y, big=False):
    """Cuenta de resina en el suelo: borde oscuro, cuerpo ámbar y brillo arriba a la izquierda."""
    pts = [(0, 0), (1, 0), (0, 1), (1, 1)] + ([(2, 0), (2, 1), (0, 2), (1, 2), (2, 2)] if big else [])
    for (dx, dy) in pts: cv.put(x + dx, y + dy, AMB[2] if dx + dy < (2 if big else 1) else AMB[1])
    cv.put(x, y, AMB[4] if big else AMB[3])
    for (dx, dy) in ((0, 2 + big), (1, 2 + big), (2 + big, 1), (2 + big, 0)) if True else (): cv.put_out(x + dx, y + dy, AMB[0]) if False else None
    cv.put(x + (2 if big else 1), y + (3 if big else 2), AMB[0]); cv.put(x + (1 if big else 0), y + (3 if big else 2), AMB[0])

def floor_amber(base, k, seed):
    cv = copy_canvas(base, Canvas); rnd = random.Random(seed + k * 13)
    if k == 1:                                                         # cuentas sueltas
        bead(cv, 4, 5, True); bead(cv, 10, 10)
    elif k == 2:                                                       # grieta rellena de resina
        x, y = 4, 9
        for i in range(7):
            cv.put(x, y, AMB[1]); cv.put(x, y - 1, AMB[2] if i % 3 else AMB[3])
            x += 1; y += (1 if i % 4 == 1 else -1 if i % 4 == 3 else 0)
    elif k == 3:                                                       # costra de resina
        for (dx, dy) in ((0, 0), (1, 0), (2, 0), (3, 1), (0, 1), (1, 1), (2, 1), (1, 2), (2, 2)):
            cv.put(6 + dx, 7 + dy, AMB[1] if dy else AMB[2])
        cv.put(7, 7, AMB[3]); cv.put(9, 9, AMB[0]); cv.put(10, 8, AMB[0])
    elif k == 4:                                                       # mosquito atrapado en una gota
        for dx in range(5):
            for dy in range(3): cv.put(5 + dx, 6 + dy, AMB[2] if dy < 2 else AMB[1])
        cv.put(5, 6, AMB[4]); cv.put(7, 7, INSECT); cv.put(8, 7, INSECT); cv.put(6, 6, AMB[3])
        for dx in range(5): cv.put(5 + dx, 9, AMB[0])
        bead(cv, 11, 11)
    else:                                                              # ramita de raíz con resina
        rt = [C(c) for c in base_piso3.TRAMOS['Amber']['root']]
        for i in range(7): cv.put(3 + i, 11 - i // 3, rt[2] if i % 2 else rt[3])
        bead(cv, 10, 7)
    return cv

def build_tileset():
    b = TS.build('Amber')
    T = b['tiles']; base = T['Floor_0']
    for k in range(1, 6): T[f'Floor_{k}'] = floor_amber(base, k, 7)
    return b

# =================================================================================================
# Piezas
# =================================================================================================
def tree_wound(f):
    """Herida del Lepidodendron en la cara de la pared (32×32, encima de 2×2 celdas de cara): tajo vertical en
    forma de lente con el borde dentado, los labios de corteza arrancada y astillada, la resina que llena el fondo y
    rebosa en chorreones; un brillo recorre la resina (4 cuadros)."""
    cv = Canvas(32, 32); bark = [C(c) for c in base_piso3.TRAMOS['Amber']['bark']]
    cx, top, bot = 16, 2, 23
    def half(y):                                                          # medio ancho del tajo a la altura y
        u = (y - top) / (bot - top)
        if not 0 <= u <= 1: return -1
        return 5.4 * math.sin(u * math.pi) ** .7 + (1 if y % 3 == 0 else 0) * (u > .15 and u < .85)
    for y in range(32):
        hw = half(y)
        for x in range(32):
            dx = abs(x + .5 - cx)
            if hw > 0 and dx < hw:
                u = (y - top) / (bot - top)
                cv.put(x, y, PASSAGE if u < .35 else AMB[0] if u < .45 else AMB[2] if x < cx and u < .8 else AMB[1])
            elif hw > 0 and dx < hw + 2.2:                                 # labios de corteza arrancada (claros)
                cv.put(x, y, bark[4] if x < cx else bark[3])
    for (x, y) in ((cx - 6, 7), (cx + 6, 10), (cx - 7, 13), (cx + 5, 16)):  # astillas que saltan
        cv.put(x, y, bark[4]); cv.put(x + (1 if x > cx else -1), y - 1, bark[3])
    cv.outline(); cv.clean()
    for (x, L) in ((14, 5), (17, 3), (19, 4)):                            # rebosa y chorrea por debajo
        for j in range(L):
            y = bot + 1 + j
            cv.put(x, y, AMB[3] if j % 4 else AMB[2]); cv.put(x + 1, y, AMB[1])
        for (dx, dy, c) in ((-1, 0, AMB[1]), (0, 0, AMB[3]), (1, 0, AMB[2]), (2, 0, AMB[0]), (0, 1, AMB[1]), (1, 1, AMB[0])):
            cv.put(x + dx, bot + 1 + L + dy, c)
    gl = [(14, 15), (15, 16), (16, 17), (17, 16)][f]                     # brillo que recorre la resina
    cv.put(gl[0], gl[1], AMB[4]); cv.put(gl[0] + 1, gl[1], AMB[3])
    return cv

def big_tree(f, cap):
    """Lepidodendron gigante y herido (96×84) que sale de la pared de arriba. La copa es la propia espesura de la
    pared (el tile Wall_Cap repetido, alineado a la rejilla: el sprite va en x múltiplo de 16 e y = 0 de la sala), con
    el borde de abajo irregular y frondas que cuelgan; el tronco de cojinetes en rombo se ensancha en contrafuertes y
    raíces; un tajo dentado con astillas deja salir la resina, que baja como una cortina ancha por la corteza y se
    encharca en el pie (y = 80, ya en el suelo: de ahí sale el reguero hasta el bloque). Brillos que bajan (4 cuadros)."""
    W, H = 96, 84
    cv = Canvas(W, H); bark = [C(c) for c in base_piso3.TRAMOS['Amber']['bark']]
    tx, tw, foot = 26, 44, 78
    bark_trunk(cv, tx, tw, 10, foot, bark, seed=5)
    for y in range(foot - 16, foot + 1):                                  # contrafuertes
        e = (y - (foot - 16)) * .55
        for side in (-1, 1):
            for k in range(1, int(e) + 1):
                x = tx - k if side < 0 else tx + tw - 1 + k
                cv.put(x, y, bark[3] if side < 0 and k < 3 else bark[2] if (x + y) % 5 else bark[1])
    rt = bark
    for (x0, x1, y1) in ((tx - 6, tx - 20, foot + 5), (tx + 6, tx - 2, foot + 6), (tx + tw + 5, tx + tw + 20, foot + 5),
                         (tx + tw - 8, tx + tw + 4, foot + 6)):
        root(cv, [(x0, foot - 2), ((x0 + x1) / 2, foot + 1), (x1, y1 - 1), (x1 + (x1 - x0) * .2, y1)], 4.0, 1.4, rt, box=(0, 0, W, H))
    cx, top, bot = tx + tw / 2 - 1, 24, 52                                # el tajo dentado
    jit = [random.Random(y).uniform(-1.6, 1.6) for y in range(H)]
    def half(y):
        u = (y - top) / (bot - top)
        return 9 * math.sin(u * math.pi) ** .55 + jit[y] if 0 <= u <= 1 else -1
    for y in range(top, bot + 1):
        hw = half(y); u = (y - top) / (bot - top)
        for x in range(int(cx - hw - 4), int(cx + hw + 5)):
            dx = abs(x + .5 - cx)
            if dx < hw: cv.put(x, y, PASSAGE if u < .28 else AMB[0] if u < .36 else AMB[3] if x < cx - hw * .3 and u < .7 else AMB[2] if x < cx + 1 else AMB[1])
            elif dx < hw + 2 + (y % 3 == 0): cv.put(x, y, bark[4] if x < cx else bark[3])
    for (x, y, d) in ((cx - 12, 30, -1), (cx + 11, 34, 1), (cx - 11, 42, -1), (cx + 12, 46, 1), (cx - 3, top - 2, -1)):   # astillas
        for k in range(3): cv.put(x + d * k, y - k, bark[4] if k < 2 else bark[3])
    # copa: la espesura de la pared, con el borde de abajo irregular
    for y in range(0, 20):
        for x in range(W):
            edge = 14 + 3 * math.sin(x * .45) + 2 * math.sin(x * 1.3 + 1)
            side = min(x, W - 1 - x)
            if y < edge and side > 2 + max(0, 6 - y) * 0: cv.put(x, y, cap.px.get((x % 16, y % 16), cap.px[(0, 0)]))
    lf = [C(c) for c in base_piso3.TRAMOS['Amber']['leaf']]
    cv.outline(); cv.clean()
    for k in range(12):
        x = 4 + k * 8; y = int(14 + 3 * math.sin(x * .45) + 2 * math.sin(x * 1.3 + 1))
        frondlet(cv, x, y - 1, 1.3 + (k % 3) * .25, 8, lf, droop=.5, lw=1.8)
    # la resina rebosa del tajo y baja como una cortina hasta el pie
    for y in range(bot - 3, foot + 2):
        u = (y - bot) / max(1, foot - bot)
        w = 3.5 + u * 6 + math.sin(y * .7) * 1.2
        for x in range(int(cx - w - 1), int(cx + w + 2)):
            dx = x + .5 - cx
            if abs(dx) < w:
                c = AMB[3] if dx < -w * .45 and (y // 2) % 3 else AMB[2] if dx < w * .35 else AMB[1]
                cv.put(x, y, c)
    for k, dx in enumerate((-7, -2, 4, 8)):                               # gotas que cuelgan del borde de la cortina
        L = 2 + (k * 3) % 4
        for j in range(L): cv.put(cx + dx, foot - 1 + j, AMB[2]); cv.put(cx + dx + 1, foot - 1 + j, AMB[1])
    for y in range(foot - 1, foot + 6):                                   # charco al pie
        for x in range(int(cx - 13), int(cx + 14)):
            dd = math.hypot((x + .5 - cx) / 13, (y + .5 - foot - 2) / 3.6)
            if dd < 1: cv.put_out(x, y, AMB[0] if dd > .8 else AMB[1]) if (x, y) in cv.px and cv.px[(x, y)] in bark else cv.put(x, y, AMB[0] if dd > .8 else AMB[1] if y > foot + 1 else AMB[2])
    for k in range(4):                                                    # brillos que bajan por la cortina
        y = bot + ((f * 6 + k * 7) % (foot - bot))
        x = cx - 2 - (y - bot) / max(1, foot - bot) * 3
        cv.put(x, y, AMB[4]); cv.put(x, y + 1, AMB[3])
    gl = [(cx - 5, 38), (cx - 4, 40), (cx - 3, 42), (cx - 4, 44)][f]
    cv.put(gl[0], gl[1], AMB[4]); cv.put(gl[0] + 1, gl[1], AMB[3])
    return cv

def resin_stream(f, L=40):
    """Reguero de resina que baja del pie del árbol hasta la base del bloque (28×L, 6 cuadros): cinta que serpentea,
    borde translúcido que se funde con el suelo, centro ámbar y brillos que corren hacia abajo. Sin contorno."""
    W = 28; cv = Canvas(W, L)
    for y in range(L):
        u = y / L
        cx = W / 2 + math.sin(u * 5.2) * 4
        hw = 3.2 + 1.3 * math.sin(u * 9 + 1)
        for x in range(W):
            d = abs(x + .5 - cx) / hw
            if d < .55: cv.put(x, y, AMB[2] if x < cx else AMB[1])
            elif d < .85: cv.put(x, y, AMB[1])
            elif d < 1.1: cv.put(x, y, AMB[0][:3] + (200,))
            elif d < 1.4 and (x + y) % 2 == 0: cv.put(x, y, AMB[0][:3] + (110,))
    for k in range(4):
        u = ((f / 6) + k / 4) % 1; y = int(u * (L - 2))
        cx = W / 2 + math.sin(y / L * 5.2) * 4
        cv.put(cx - 1, y, AMB[4]); cv.put(cx - 1, y + 1, AMB[3]); cv.put(cx, y + 1, AMB[3])
    return cv

def poly_inside(pts):
    def f_(x, y):
        x += .5; y += .5; inside = False; j = len(pts) - 1
        for i in range(len(pts)):
            xi, yi = pts[i]; xj, yj = pts[j]
            if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi) + xi: inside = not inside
            j = i
        return inside
    return f_

CHUNKS = {          # lienzo, forma, insecto
    'A': ((18, 20), ('oval', 8.5, 11, 5.5, 7), 'mosquito'),
    'B': ((26, 22), ('poly', [(3, 14), (6, 5), (14, 2), (22, 6), (23, 15), (15, 19), (6, 19)]), 'beetle'),
    'C': ((16, 26), ('poly', [(4, 22), (3, 12), (7, 2), (12, 9), (13, 22), (8, 24)]), 'fly'),
    'D': ((28, 20), ('poly', [(2, 16), (3, 8), (7, 6), (9, 9), (12, 4), (16, 7), (19, 3), (24, 7), (26, 16), (14, 18)]), 'spider'),
    'E': ((28, 18), ('blobs', [(8, 11, 5, 4.5), (17, 9, 6, 5.5), (23, 13, 3.5, 3)]), 'ants'),
}

def insect(cv, kind, x, y, inside):
    c = blend(INSECT, AMB[1], 70)
    def put(px_, py_):
        if inside(px_, py_): cv.put(px_, py_, c)
    if kind == 'mosquito':
        for dy in range(-3, 3): put(x, y + dy)
        for (dx, dy) in ((-1, -2), (-2, -1), (1, -2), (2, -1), (-2, 1), (-3, 2), (2, 1), (3, 2)): put(x + dx, y + dy)
        cv.put(x - 1, y - 1, blend(AMB[3], AMB[4], 120)) if inside(x - 1, y - 1) else None   # ala que brilla
    elif kind == 'beetle':
        for dx in (-2, -1, 0, 1, 2):
            for dy in (-2, -1, 0, 1, 2):
                if abs(dx) + abs(dy) < 4: put(x + dx, y + dy)
        for side in (-1, 1):
            for k in (-1, 0, 1): put(x + side * 3, y + k * 2); put(x + side * 4, y + k * 2 + side * (k or 1))
        put(x, y - 3)
    elif kind == 'fly':
        for dy in range(-2, 3): put(x, y + dy); put(x + 1, y + dy) if dy > -2 else None
        for (dx, dy) in ((-1, -1), (-2, -1), (-2, 0), (2, -1), (3, -1), (3, 0)): put(x + dx, y + dy)
    elif kind == 'spider':
        for (dx, dy) in ((0, 0), (1, 0), (0, 1), (1, 1), (0, -1)): put(x + dx, y + dy)
        for side in (-1, 1):
            for k in range(4):
                lx = x + (2 if side > 0 else -1) + side * (k // 2); ly = y - 1 + k
                put(lx, ly); put(lx + side, ly + (1 if k > 1 else -1))
    else:                                                                 # hormigas: tres puntitos en fila, varias
        for (ax, ay) in ((-6, 1), (2, -1), (7, 2)):
            for dx in (0, 1, 2): put(x + ax + dx, y + ay)
            put(x + ax + 1, y + ay - 1); put(x + ax + 1, y + ay + 1)

def amber_chunk(kind, seed):
    """Bloque de ámbar pequeño con un insecto dentro (como el bloque grande, en otras formas y tamaños): borde claro
    arriba a la izquierda, centro más hondo, una arista con brillo y el bicho oscurecido por la resina."""
    (W, H), shape, bug = CHUNKS[kind]
    cv = Canvas(W, H)
    if shape[0] == 'oval':
        _, cx, cy, rx, ry = shape; inside = lambda x, y: ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 <= 1
    elif shape[0] == 'poly':
        inside = poly_inside(shape[1])
    else:
        bl = shape[1]; inside = lambda x, y: any(((x + .5 - a) / c) ** 2 + ((y + .5 - b) / d) ** 2 <= 1 for (a, b, c, d) in bl)
    pts = [(x, y) for y in range(H) for x in range(W) if inside(x, y)]
    xs = [x for x, _ in pts]; ys = [y for _, y in pts]; cx, cy = sum(xs) / len(xs), sum(ys) / len(ys)
    x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
    for (x, y) in pts:
        edge = sum((x + dx, y + dy) not in pts for dx in (-1, 0, 1) for dy in (-1, 0, 1)) if False else \
               min(abs(x - x0), abs(x1 - x), abs(y - y0), abs(y1 - y))
        u = (x - x0) / max(1, x1 - x0) + (y - y0) / max(1, y1 - y0)
        rim = not all(inside(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1), (2, 0), (0, 2), (-2, 0), (0, -2)))
        if rim: c = AMB[3] if u < .8 else AMB[1] if u > 1.3 else AMB[2]
        else: c = AMB[2] if u < .9 else AMB[1]
        if not rim and abs(x - cx) < (x1 - x0) * .22 and abs(y - cy) < (y1 - y0) * .22: c = AMB[1]   # centro hondo
        cv.put(x, y, c)
    insect(cv, bug, round(cx), round(cy), inside)
    rnd = random.Random(seed)                                              # arista con brillo
    hl = [p for p in pts if (p[0] - x0) + (p[1] - y0) < 5 and inside(p[0] + 1, p[1] + 1)]
    for p in hl[:3]: cv.put(p[0] + 1, p[1] + 1, AMB[4])
    cv.outline(); cv.clean()
    ground_shadow(cv, cx, y1 + 1.5, (x1 - x0) * .5 + 1, 1.4)
    return cv

def ground_shadow(cv, cx, cy, rx, ry):
    for y in range(int(cy - ry), int(cy + ry) + 1):
        for x in range(int(cx - rx), int(cx + rx) + 1):
            if ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 <= 1: cv.put_out(x, y, (24, 20, 37, 90))

POOLS = {'A': ('redondo', 34, 18), 'B': ('alargado', 42, 16), 'C': ('lobulado', 36, 20)}

def amber_pool(kind, seed):
    """Charco de resina que se funde con el suelo: sin borde duro; el borde es ámbar oscuro translúcido y punteado,
    el centro algo más claro con un brillo, y tonos bajos (no compite con el bloque)."""
    shape, w, h = POOLS[kind]
    mask = {(x + 2, y + 2) for (x, y) in blob(seed, shape, w, h)}
    cv = Canvas(w + 4, h + 4)
    dist = {p: 1 for p in mask if any((p[0] + dx, p[1] + dy) not in mask for dx, dy in N4)}
    for k in (2, 3, 4):
        for p in [p for p in mask if p not in dist and any(dist.get((p[0] + dx, p[1] + dy)) == k - 1 for dx, dy in N4)]: dist[p] = k
    for (x, y) in mask:
        d = dist.get((x, y), 5)
        if d == 1:
            if (x + y) % 2 == 0: cv.put(x, y, AMB[0][:3] + (120,))
        elif d == 2: cv.put(x, y, AMB[0][:3] + (200,))
        elif d == 3: cv.put(x, y, AMB[0] if (x + y) % 2 else AMB[1])
        else: cv.put(x, y, AMB[1])
    inner = sorted(p for p in mask if dist.get(p, 5) >= 4)
    rnd = random.Random(seed)
    for _ in range(max(1, len(inner) // 40)):
        if not inner: break
        x, y = rnd.choice(inner)
        for j in range(3):
            if (x + j, y) in mask and dist.get((x + j, y), 5) >= 4: cv.put(x + j, y, AMB[3] if j == 1 else AMB[2])
    return cv

def block_base():
    """Base del bloque de ámbar (80×44, el bloque va en el centro): charco grande de resina con raíces del árbol
    teñidas de resina que salen de debajo en abanico y se agarran al suelo, para que el bloque no parezca pegado."""
    W, H = 80, 44
    cv = Canvas(W, H); cx, cy = W / 2, H / 2 + 3
    nz = noise(9)
    for y in range(H):
        for x in range(W):
            d = math.hypot((x + .5 - cx) / 34, (y + .5 - cy) / 15) + (nz(x, y, 4) - .5) * .3
            if d < 1:
                c = AMB[0] if d > .88 else AMB[1] if d > .55 else AMB[2] if (x - cx) + (y - cy) < 4 else AMB[1]
                cv.put(x, y, c)
    rcv = Canvas(W, H); rt = [C(c) for c in base_piso3.TRAMOS['Amber']['bark']]
    for a in (math.pi * .95, math.pi * 1.2, math.pi * .7, math.pi * .05, -math.pi * .2, math.pi * .3, math.pi * .55, math.pi * 1.45):
        L = 26 if abs(math.cos(a)) > .6 else 15
        x0, y0 = cx + math.cos(a) * 9, cy + math.sin(a) * 4
        pts = [(x0 + math.cos(a) * L * t_, y0 + math.sin(a) * L * t_ * .5 + math.sin(t_ * 3 + a) * 1.2) for t_ in (0, .35, .7, 1)]
        root(rcv, pts, 2.6, .9, rt, box=(0, 0, W, H))
    rcv.outline(); rcv.clean()
    for p_, c in rcv.px.items(): cv.px[p_] = c
    rnd = random.Random(3)
    for _ in range(8):                                                   # resina que cubre las raíces a trozos
        x, y = rnd.randrange(10, 70), rnd.randrange(10, 38)
        if (x, y) in rcv.px and rcv.px[(x, y)] != OUTL: cv.put(x, y, AMB[3]); cv.put(x + 1, y, AMB[2])
    return cv

BEADS = {'A': ((5, 8, True),), 'B': ((4, 9, False), (9, 6, True)), 'C': ((3, 7, False), (8, 10, True), (11, 5, False))}

def amber_beads(kind, f):
    """Cuentas de ámbar sueltas por el suelo; una destella en el cuadro 3 (6 cuadros, 16×16)."""
    cv = Canvas(16, 16)
    for (x, y, big) in BEADS[kind]:
        r = 1.6 if big else 1.1
        ellipse(cv, x, y, r + .3, r, AMB[:4] if big else AMB[1:])
    cv.outline(); cv.clean()
    x, y, big = BEADS[kind][-1]
    if f == 3: sparks(cv, x - 1, y - 2, 2)
    elif f in (2, 4): cv.put(x - 1, y - 1, AMB[4])
    return cv

def amber_glow(f):
    """Resplandor cálido bajo el bloque (96×56, translúcido, 8 cuadros que respiran). En Unity, mejor con mezcla
    aditiva, y además una luz 2D ámbar."""
    cv = Canvas(96, 56); br = .82 + .18 * math.sin(f / 8 * 2 * math.pi)
    for y in range(56):
        for x in range(96):
            d = math.hypot((x + .5 - 48) / 48, (y + .5 - 28) / 28)
            if d < 1:
                a = (1 - d) ** 1.4 * 95 * br
                if a > 6: cv.put(x, y, (255, 186, 90, int(a)))
    return cv

# =================================================================================================
# Sala de muestra y salida
# =================================================================================================
AMBER_ROOM = [  # 24×15 como Room_Special_Amber (paredes incluidas); 'L' puerta izquierda
    "########################",
    "########################",
    "########################",
    "########################",
    "##....................##",
    "##....................##",
    "##....................##",
    "L.....................##",
    "L.....................##",
    "##....................##",
    "##....................##",
    "##....................##",
    "##....................##",
    "########################",
    "########################",
]

def scene(b, P, f=0):
    TS.SAMPLE = AMBER_ROOM
    big, small = TS.sample_room(b)
    im = small.convert('RGBA'); W, H = im.size
    out = Image.new('RGBA', (480, 270), (14, 10, 8, 255)); ox, oy = (480 - W) // 2, (270 - H) // 2
    out.alpha_composite(im, (ox, oy))
    def at(img, x, y): out.alpha_composite(img, (ox + x, oy + y))
    at(P['Fx_AmberGlow'][f % 8].img(), 192 - 48, 136 - 32)
    at(P['AmberBlock_Base'][0].img(), 192 - 40, 136 - 24)
    for (k, x, y) in (('A', 60, 150), ('B', 250, 180), ('C', 300, 100)): at(P[f'Deco_AmberPool_{k}'][0].img(), x, y)
    tree = P['Deco_BigTree'][f % 4].img(); at(tree, 144, 0)
    at(P['Deco_ResinStream'][f % 6].img(), 192 - 14, 80)
    for k, (kind, x, y) in enumerate((('A', 60, 120), ('B', 150, 80), ('C', 250, 120), ('B', 320, 140), ('A', 180, 180), ('C', 90, 150))):
        at(P[f'AmberBeads_{kind}'][(f + k * 2) % 6].img(), x, y)
    blk = Image.open(os.path.join(ENV, 'pieces', 'AmberBlock_Idle.png')).convert('RGBA').crop((40 * (f % 4), 0, 40 * (f % 4) + 40, 44))
    at(blk, 192 - 20, 136 - 34)
    for (k, x, y) in (('A', 90, 96), ('B', 300, 150), ('C', 120, 186), ('D', 230, 196), ('E', 50, 110)):
        im_ = P[f'AmberChunk_{k}'][0].img(); at(im_, x - im_.width // 2, y - im_.height)
    return out

if __name__ == '__main__':
    b = build_tileset()
    d = os.path.join(HERE, 'Amber'); os.makedirs(os.path.join(d, 'tiles'), exist_ok=True); os.makedirs(os.path.join(d, 'deco'), exist_ok=True)
    for name in TS.TILE_ORDER: b['tiles'][name].img().save(os.path.join(d, 'tiles', name + '.png'))
    TS.grid([b['tiles'][n] for n in TS.TILE_ORDER], 8).save(os.path.join(d, 'Tileset_Amber.png'))
    for name, cv in b['spills'].items(): cv.img().save(os.path.join(d, 'deco', name + '.png'))
    b['arch'].img().save(os.path.join(d, 'Doorway_Top.png'))
    for side, (close, opn) in b['doors'].items():
        save_sheet(os.path.join(d, f'Door_{side}_Close.png'), close); save_sheet(os.path.join(d, f'Door_{side}_Open.png'), opn)
    P = {'Deco_TreeWound': [tree_wound(f) for f in range(4)], 'Deco_BigTree': [big_tree(f, b['tiles']['Wall_Cap']) for f in range(4)],
         'Deco_ResinStream': [resin_stream(f) for f in range(6)],
         'AmberBlock_Base': [block_base()], 'Fx_AmberGlow': [amber_glow(f) for f in range(8)]}
    for i, k in enumerate(POOLS): P[f'Deco_AmberPool_{k}'] = [amber_pool(k, 30 + i * 4)]
    for k in BEADS: P[f'AmberBeads_{k}'] = [amber_beads(k, f) for f in range(6)]
    for i, k in enumerate(CHUNKS): P[f'AmberChunk_{k}'] = [amber_chunk(k, 50 + i)]
    pd = os.path.join(HERE, 'pieces'); os.makedirs(pd, exist_ok=True)
    for name, frames in P.items(): save_sheet(os.path.join(pd, name + '.png'), frames)
    frames = [scene(b, P, f).resize((960, 540), Image.NEAREST) for f in range(8)]
    frames[0].save(os.path.join(HERE, 'maqueta_sala_ambar.png'))
    os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True)
    frames[0].save(os.path.join(HERE, 'gif', 'escena_sala_ambar.gif'), save_all=True, append_images=frames[1:], duration=140, loop=0, disposal=2)
    preview(os.path.join(HERE, 'vista_previa_piezas.png'), {k: v for k, v in P.items()}, C('#24160e'), scale=4)
    print('Listo en', HERE)
