"""Espesura de la sala de la Arthropleura (bosque humeante): arte final.

Vista desde arriba, en tres capas (fondo oscuro de frondas, hojarasca ocre y coronas de helecho arborescente que
sobresalen). Se abre en cualquier ángulo (embestidas), así que va por AUTOTILE como el lodo:
- `Thicket/Thicket_Inner_0..5` (16×16): interior; 0 lisa, 1-5 con detalle dentro.
- `Thicket/Thicket_Edge_<m>_<c>` (16×16, 47 piezas): m = lados abiertos (N=1, E=2, S=4, O=8), c = esquinas abiertas
  (NE=1, SE=2, SO=4, NO=8; solo si sus dos lados están cerrados). Lo abierto es transparente (se ve el suelo); el borde
  de abajo lleva una cortina corta de frondas y sombra.
- `Thicket/Thicket_Crown_0..3` (32×32): coronas encima de bloques de 2×2 celdas de interior (el código las quita si se
  rompe alguna de sus celdas).

Uso: python generar_espesura_arthropleura.py
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', '..', 'floor3-environment'))
from base_piso3 import *
from PIL import Image

OUT = os.path.join(HERE, 'Thicket')
T = tram('Smoke')
CAP, LEAF, VEIN = T['cap'], T['leaf'], T['vein']
DEEP = [C('#0c0e0b'), C('#121510'), C('#181b15'), CAP[0]]
CROWN = [LEAF[1], LEAF[2], LEAF[3], VEIN[1]]
OCHRE = [VEIN[0], VEIN[1], VEIN[2], C('#d0b060')]
CROWN_SIZE = 40
SHADOW = (10, 12, 8, 120)
HAZE = C('#8a8a7a')

def hsh(x, y, s=0): return ((x * 73856093) ^ (y * 19349663) ^ (s * 83492791)) & 0x7fffffff

# =================================================================================================
# Interior
# =================================================================================================
def base16(seed=700):
    """Capas 0 y 1, que dan la vuelta (iguales en todas las piezas, así todo encaja)."""
    cv = WrapCanvas(16, 16, True, True)
    n = pnoise(seed, 16, 4)
    for y in range(16):
        for x in range(16):
            cv.px[(x, y)] = DEEP[dither(1.2 + n(x, y) * 1.6, x, y) % 4]
    rnd = random.Random(seed)
    for layer, count in ((0, 4), (1, 4)):
        for k in range(count):
            cx, cy = rnd.uniform(0, 16), rnd.uniform(0, 16)
            nf = rnd.randrange(4, 7); a0 = rnd.uniform(0, 6.28)
            for f in range(nf):
                frondlet(cv, cx, cy, a0 + f * 6.283 / nf + rnd.uniform(-.25, .25), rnd.uniform(5, 9), LEAF, .15, 2, None, 3 - layer)
    for k in range(2):                                # velo de humo entre las hojas
        yy = 4 + k * 8; ph = rnd.uniform(0, 6)
        for x in range(16):
            if math.sin(x * math.pi / 8 + ph) > .2:
                y = yy + round(math.sin(x * math.pi / 4 + ph))
                if (x + y) % 2 == 0: cv.px[(x, y % 16)] = blend(cv.px[(x, y % 16)], HAZE, 55)
    return cv

CAPDIR = os.path.join(HERE, '..', '..', 'floor3-environment', 'Smoke', 'tiles')
def load16(name):
    im = Image.open(os.path.join(CAPDIR, name + '.png')).convert('RGBA'); cv = Canvas(16, 16)
    for y in range(16):
        for x in range(16): cv.px[(x, y)] = im.getpixel((x, y))
    return cv

def inner(base, k):
    if k < 4: return load16('Wall_Cap' if k == 0 else f'Wall_Cap_{k}')   # la tapa aprobada del tramo humeante
    cv = copy_canvas(base); rnd = random.Random(900 + k)
    cv.clip = (2, 2, 14, 14)
    cx, cy = rnd.uniform(6, 10), rnd.uniform(6, 10)
    if k in (1, 4):                                   # mata clara que asoma
        nf = rnd.randrange(5, 7); a0 = rnd.uniform(0, 6.28)
        for f in range(nf): frondlet(cv, cx, cy, a0 + f * 6.283 / nf, rnd.uniform(4, 6), LEAF, .1, 2, None, 1)
    if k == 2:                                        # fronda reseca, ocre
        for f in range(2): frondlet(cv, cx - 4, cy + f * 3 - 1, rnd.uniform(-.4, .4) + f * .5, rnd.uniform(8, 10), OCHRE, .25, 1.6, None, 2)
    if k == 3:                                        # hueco oscuro entre las copas
        hx, hy = (cx + 2, cy + 2) if k == 4 else (cx, cy)
        for y in range(16):
            for x in range(16):
                d = ((x + .5 - hx) / 4.6) ** 2 + ((y + .5 - hy) / 3.4) ** 2 + (hsh(x, y, k) % 7) * .04
                if d < 1 and ((x + y) % 2 or d < .6): cv.put(x, y, DEEP[1] if d < .5 else DEEP[2])
    if k == 5:                                        # humo que sale de la espesura
        for j in range(9):
            x, y = cx - 4 + j, cy + round(math.sin(j * .8) * 1.5)
            for dy in (0, 1):
                if (x + dy) % 2 == 0 and (x, y + dy) in cv.px: cv.px[(int(x), int(y + dy))] = blend(cv.px[(int(x), int(y + dy))], HAZE, 110)
    cv.clip = None
    return cv

def crown(k):
    """Corona de helecho arborescente vista desde arriba, con su sombra sobre la copa."""
    S = CROWN_SIZE
    cv = Canvas(S, S); rnd = random.Random(1200 + k)
    cx, cy = S / 2 - .5 + rnd.uniform(-1, 1), S / 2 - 1 + rnd.uniform(-1, 1)
    part = Canvas(S, S); box = (0, 0, S, S)
    nf = rnd.randrange(9, 12); a0 = rnd.uniform(0, 6.28)
    for f in range(nf):                               # anillo de fuera: frondas largas que se arquean, más oscuras
        a = a0 + f * 6.283 / nf + rnd.uniform(-.15, .15)
        frondlet(part, cx, cy, a, rnd.uniform(14, 17.5) * (.8 + .05 * k), CROWN if f % 5 else OCHRE, .18, 3.2, box, 1 if math.sin(a) > -.3 else 0)
    nf2 = rnd.randrange(6, 8); a1 = a0 + .4
    for f in range(nf2):                              # anillo de dentro: frondas jóvenes, con luz
        a = a1 + f * 6.283 / nf2
        frondlet(part, cx, cy, a, rnd.uniform(7, 10) * (.8 + .05 * k), CROWN, .05, 2.6, box, 0)
    for (x, y) in list(part.px):                      # sombra abajo a la derecha
        q = (x + 3, y + 4)
        if q not in part.px and 0 <= q[0] < S and 0 <= q[1] < S: cv.px[q] = SHADOW[:3] + (100,)
    for p, c in part.px.items(): cv.px[p] = c
    for y in range(S):                                # cogollo oscuro con un báculo enrollado
        for x in range(S):
            d = math.hypot(x + .5 - cx, (y + .5 - cy) * 1.1)
            if d < 1.3: cv.px[(x, y)] = DEEP[2]
    cv.put(cx, cy - 1, VEIN[2]); cv.put(cx + 1, cy - 1, VEIN[1])
    return cv


TUFT = [C(c) for c in ('#1c1e14', '#2c2e1a', '#424222', '#605c2c', '#857c44')]
def tree_crown(k):
    """Copa de Lepidodendron vista desde arriba: ramas que se bifurcan dos veces y penachos de hojas en las puntas."""
    S = 48; cv = Canvas(S, S); part = Canvas(S, S); rnd = random.Random(1500 + k)
    cx, cy = S / 2 - .5, S / 2 - 1
    bark = T['bark']; tufts = []
    def branch(x0, y0, x1, y1, w):
        n = int(math.hypot(x1 - x0, y1 - y0) * 2) + 1
        for i in range(n + 1):
            t = i / n; x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
            for d in range(w): part.put(x + d * .5, y + d * .5, bark[2] if d == 0 else bark[1])
    nb = rnd.randrange(3, 5); a0 = rnd.uniform(0, 6.28)
    for i in range(nb):
        a = a0 + i * 6.283 / nb + rnd.uniform(-.3, .3); L1 = rnd.uniform(6, 9)
        x1, y1 = cx + math.cos(a) * L1, cy + math.sin(a) * L1 * .9
        branch(cx, cy, x1, y1, 2)
        for j in (-1, 1):
            a2 = a + j * rnd.uniform(.35, .6); L2 = rnd.uniform(6, 9)
            x2, y2 = x1 + math.cos(a2) * L2, y1 + math.sin(a2) * L2 * .9
            x2, y2 = max(7, min(S - 8, x2)), max(7, min(S - 8, y2))
            branch(x1, y1, x2, y2, 1); tufts.append((x2, y2, rnd.uniform(4.2, 5.6)))
    for (tx, ty, r) in sorted(tufts, key=lambda q: q[1]):          # penachos: cúpula sombreada y agujas alrededor
        ri = r * .55
        fill(part, lambda px, py: ((px - tx) / ri) ** 2 + ((py - ty) / (ri * .85)) ** 2 <= 1, tx, ty, ri, ri * .85, TUFT[:4])
        for n in range(22):                           # agujas largas, como hierba, que salen del centro del penacho
            a = n * 6.283 / 22 + rnd.uniform(-.12, .12); L = rnd.uniform(r * .6, r + 1.5)
            for st in range(int(L)):
                x, y = tx + math.cos(a) * (ri * .5 + st), ty + math.sin(a) * (ri * .45 + st * .85)
                lit = math.cos(a) * -.6 + math.sin(a) * -.8 > 0
                part.put(x, y, TUFT[3] if lit and st < L * .4 else TUFT[2] if lit or st < L * .3 else TUFT[1] if st < L - 1 else TUFT[0])
        part.put(tx - r * .4, ty - r * .4, TUFT[4])
    for (x, y) in list(part.px):                      # sombra abajo a la derecha
        q = (x + 3, y + 4)
        if q not in part.px and 0 <= q[0] < S and 0 <= q[1] < S: cv.px[q] = SHADOW[:3] + (105,)
    for p_, c in part.px.items(): cv.px[p_] = c
    return cv

# =================================================================================================
# Bordes por autotile (16 máscaras de lados × esquinas válidas = 47 piezas)
# =================================================================================================
EPH = {'N': .7, 'E': 2.1, 'S': 3.5, 'W': 4.0}
def ed(u, side, D): return D + 1.3 * math.sin(2 * math.pi * u / 16 + EPH[side]) + .6 * math.sin(4 * math.pi * u / 16 + EPH[side] * 1.7)
DS = 9.5                                              # el borde de abajo se mete más: deja sitio a la cortina
def curl(x, v=0): return 5 + (hsh(x % 16, 1, 5 + v * 11) % 3) + (1 if (x + v * 2) % 5 == 2 else 0)

def classify(m, c, v=0):
    st = {}
    for y in range(16):
        for x in range(16):
            px, py = x + .5, y + .5
            o = (m & 1 and py < ed(px, 'N', 3)) or (m & 2 and 16 - px < ed(py, 'E', 3)) or (m & 8 and px < ed(py, 'W', 3))
            if c & 1 and math.hypot(16 - px, py) < 6 + .8 * math.sin(px): o = True          # NE
            if c & 8 and math.hypot(px, py) < 6 + .8 * math.sin(py): o = True               # NO
            R = 7                                     # esquinas de fuera redondeadas
            for (a, b, kx, ky) in ((1, 8, 0, 0), (1, 2, 16, 0)):
                if m & a and m & b and abs(px - kx) < R and abs(py - ky) < R:
                    if math.hypot(R - abs(px - kx), R - abs(py - ky)) > R: o = True
            if o: st[(x, y)] = 'open'; continue
            yb = 99                                   # dónde acaba la copa por abajo
            if m & 4: yb = 16 - ed(px, 'S', DS)
            R = 7
            if m & 4 and m & 2 and 16 - px < R: yb -= R - math.sqrt(max(0, R * R - (R - (16 - px)) ** 2))
            if m & 4 and m & 8 and px < R: yb -= R - math.sqrt(max(0, R * R - (R - px) ** 2))
            if c & 4 and px < DS: yb = min(yb, 16 - ed(px, 'S', DS) * math.sqrt(max(0, 1 - (px / DS) ** 2)))        # SO
            if c & 2 and 16 - px < DS: yb = min(yb, 16 - ed(px, 'S', DS) * math.sqrt(max(0, 1 - ((16 - px) / DS) ** 2)))   # SE
            if py >= yb: st[(x, y)] = 'cur' if py < yb + curl(x, v) else 'sh' if py < yb + curl(x, v) + 2 else 'open'
            else: st[(x, y)] = 'can'
    if m & 2:                                         # sombra en el suelo a la derecha
        for y in range(16):
            for x in range(15, -1, -1):
                if st[(x, y)] != 'open': break
                if x > 0 and st[(x - 1, y)] in ('can', 'cur'): st[(x, y)] = 'sh'
                if x > 1 and st[(x - 2, y)] in ('can', 'cur') and (x + y) % 2: st[(x, y)] = 'sh'
    return st

def edge_tile(base, m, c, v=0):
    st = classify(m, c, v)
    cv = Canvas(16, 16)
    for (x, y), s in st.items():
        if s == 'can':
            nb = [(dx, dy) for dx, dy in N4 if st.get((x + dx, y + dy), 'can') != 'can' and 0 <= x + dx < 16 and 0 <= y + dy < 16]
            if not nb: cv.px[(x, y)] = base.px[(x, y)]
            elif (0, -1) in nb or (-1, 0) in nb: cv.px[(x, y)] = VEIN[1] if (x + y) % 5 == 0 else LEAF[2]   # filo con luz
            elif (0, 1) in nb: cv.px[(x, y)] = LEAF[2] if x % 3 else LEAF[3]          # arriba de la cortina
            else: cv.px[(x, y)] = LEAF[1]
        elif s == 'cur':                              # cortina de frondas y tallos colgando
            top = y
            while st.get((x, top - 1)) == 'cur': top -= 1
            j = y - top; L = curl(x, v)
            col = hsh(x % 16, 3, 7 + v * 13) % 5
            if v == 1 and x % 16 in (4, 11): col = 9           # fronda seca, ocre
            if v == 2 and x % 16 == 7: col = 8                 # raíz que cuelga
            if col == 9: c_ = OCHRE[2] if j == 0 else OCHRE[1] if j < L - 1 else OCHRE[0]
            elif col == 8: c_ = T['root'][3] if j % 3 else T['root'][2]
            elif col == 0: c_ = DEEP[1] if j < L - 1 else DEEP[2]
            elif j == L - 1: c_ = VEIN[1] if col == 2 else LEAF[2]
            else: c_ = LEAF[2] if (col + j) % 3 == 0 else LEAF[1] if j < L - 2 else LEAF[0]
            cv.px[(x, y)] = c_
        elif s == 'sh':
            cv.px[(x, y)] = SHADOW
    for (x, y), s in list(st.items()):                # puntas de fronda que asoman hacia lo abierto (arriba y lados)
        if s != 'can': continue
        for dx, dy, u in ((0, -1, x), (-1, 0, y), (1, 0, y)):
            q = (x + dx, y + dy)
            if st.get(q) == 'open' and (u + (dx == 1) * 2) % 5 == 2 and 0 <= q[0] < 16 and 0 <= q[1] < 16:
                cv.px[q] = LEAF[2]
    return cv

def valid_edges():
    out = []
    for m in range(16):
        for c in range(16):
            ok = True
            for bit, (a, b) in ((1, (1, 2)), (2, (4, 2)), (4, (4, 8)), (8, (1, 8))):
                if c & bit and (m & a or m & b): ok = False
            if ok and not (m == 0 and c == 0): out.append((m, c))
    return out


# =================================================================================================
# Subtanda B: brechas (filo roto por máscara, restos en el suelo, surcos de las patas)
# =================================================================================================
PULP = [C(c) for c in ('#5a4a30', '#8a7650', '#b8a478', '#e0d0a8')]   # pulpa de los tallos partidos
SOIL = [C(c) for c in ('#161812', '#1e221a', '#4a4632', '#6a6448')]   # surco oscuro y tierra levantada

def splinter(cv, x, y, dx, dy, L, rnd, box=(0, 0, 16, 16)):
    """Tallo partido: astilla de corteza oscura con la pulpa clara en la punta rota."""
    px_, py_ = -dy, dx                                # lado de la astilla
    for st in range(L):
        X, Y = x + dx * st, y + dy * st
        if not (box[0] <= X < box[2] and box[1] <= Y < box[3]): break
        c = PULP[2] if st == L - 1 else PULP[1] if st >= L - 2 else T['bark'][2]
        cv.put(X, Y, c)
        if st < L - 1 and box[0] <= X + px_ < box[2] and box[1] <= Y + py_ < box[3]: cv.put(X + px_, Y + py_, T['bark'][1] if st % 2 else PULP[1])
    if rnd.random() < .5:                             # astilla suelta que se abre
        X, Y = x + dx * (L - 2) + px_ * 2, y + dy * (L - 2) + py_ * 2
        if box[0] <= X < box[2] and box[1] <= Y < box[3]: cv.put(X, Y, PULP[2])

def break_overlay(m, c, v=0):
    st = classify(m, c, v)
    cv = Canvas(16, 16); rnd = random.Random(m * 31 + c * 7 + 3)
    solid = lambda p: st.get(p) in ('can', 'cur')
    rim = []
    for (x, y), s in st.items():
        if not solid((x, y)): continue
        outs = [(dx, dy) for dx, dy in N4 if 0 <= x + dx < 16 and 0 <= y + dy < 16 and not solid((x + dx, y + dy))]
        if outs: rim.append((x, y, outs))
    for (x, y, outs) in rim:
        h = hsh(x, y, m * 3 + c)
        dx = sum(o[0] for o in outs); dy = sum(o[1] for o in outs)
        if h % 6 == 0:                                # tallo partido hacia la brecha
            splinter(cv, x, y, max(-1, min(1, dx)), max(-1, min(1, dy)), 2 + h // 4 % 3, rnd)
        elif h % 6 in (1, 4):                         # fronda desgarrada que cuelga hacia fuera
            frondlet(cv, x, y, math.atan2(dy, dx) + rnd.uniform(-.5, .5), rnd.uniform(3, 5), OCHRE if h % 3 else LEAF, .5, 1.2, (0, 0, 16, 16), 1)
        elif h % 6 == 2:                              # copa aplastada junto al filo
            for k in range(3):
                X, Y = x - dx * k, y - dy * k
                if solid((X, Y)): cv.put(X, Y, LEAF[0] if k else DEEP[2])
    return cv

def debris(k, dense):
    """Restos sobre el suelo de la brecha: frondas aplastadas, trozos de tallo y hojas."""
    cv = Canvas(16, 16); rnd = random.Random(2000 + k * 7 + dense * 100); box = (1, 1, 15, 15)
    if dense:
        for i in range(3):                            # frondas tumbadas y aplastadas
            frondlet(cv, rnd.uniform(2, 14), rnd.uniform(3, 13), rnd.uniform(0, 6.28), rnd.uniform(8, 12), OCHRE if i % 2 else LEAF, .05, 2.4, box, i % 2)
        for i in range(2):                            # trozos de tallo partido
            x, y = rnd.uniform(3, 12), rnd.uniform(3, 12); a = rnd.uniform(0, 6.28); L = rnd.randrange(4, 7)
            for st in range(L):
                X, Y = x + math.cos(a) * st, y + math.sin(a) * st
                if 1 <= X < 15 and 1 <= Y < 14: cv.put(X, Y, PULP[2] if st in (0, L - 1) else T['bark'][2]); cv.put(X, Y + 1, T['bark'][0])
    for i in range(3 if dense else rnd.randrange(2, 4)):          # hojas sueltas
        x, y = rnd.randrange(1, 13), rnd.randrange(1, 15)
        c1 = rnd.choice((OCHRE[1], OCHRE[2], LEAF[2], LEAF[3]))
        cv.put(x, y, c1); cv.put(x + 1, y, darker(c1, .75))
    if not dense and k % 2:                           # ramita
        x, y = rnd.uniform(3, 10), rnd.uniform(4, 12)
        for st in range(5): cv.put(x + st, y - st // 2, T['bark'][2] if st % 2 else PULP[1])
    return cv

def track(k):
    """Surcos de las patas, dibujados hacia la derecha (el código los gira a la dirección de la embestida)."""
    cv = Canvas(32, 16); rnd = random.Random(2500 + k)
    for row in (3, 12):                               # una fila de patas a cada lado del cuerpo
        for i in range(7):
            x = 3 + i * 4 + rnd.randrange(0, 2) + (2 if row == 12 else 0)
            y = row + rnd.randrange(-1, 2)
            sgn = 1 if row == 12 else -1
            for st in range(4):                       # arañazo hacia atrás y hacia fuera, con la tierra levantada al lado
                X, Y = x - st, y + sgn * st * .5
                cv.put(X, Y, SOIL[0] if st < 3 else SOIL[1])
                cv.put(X, Y + sgn, SOIL[3] if st == 0 else SOIL[2])
    for x in range(0, 32, 3):                         # tierra levantada entre las filas, a trazos
        if rnd.random() < .5: cv.put(x, 7 + rnd.randrange(0, 3), SOIL[2])
    return cv

# =================================================================================================
# Sala de muestra: claro de 28×16 con espesura alrededor y dos brechas (una en diagonal)
# =================================================================================================
SW, SH = 46, 30
CL = (9, 7, 37, 23)                                   # claro: x0, y0, x1, y1 (celdas)
BREACHES = [((23, 15), (1.0, -0.62), 6), ((23, 15), (-1.0, 0.0), 6)]   # (origen, dirección, ancho)

def thicket_cells():
    th = set()
    for y in range(SH):
        for x in range(SW):
            if CL[0] <= x < CL[2] and CL[1] <= y < CL[3]: continue
            cx, cy = x + .5, y + .5; cut = False
            for (ox, oy), (dx, dy), w in BREACHES:
                n = math.hypot(dx, dy); dx, dy = dx / n, dy / n
                qx, qy = cx - ox, cy - oy
                if qx * dx + qy * dy > 0 and abs(qx * dy - qy * dx) <= w / 2: cut = True
            if not cut: th.add((x, y))
    return th

def sample(base, inners, edges, crowns, trees, scale=2):
    floor = {n: Image.open(os.path.join(HERE, '..', '..', 'floor3-environment', 'Smoke', 'tiles', n + '.png')).convert('RGBA')
             for n in ('Floor_0', 'Floor_1', 'Floor_2', 'Floor_3', 'Floor_4', 'Floor_5')}
    th = thicket_cells()
    im = Image.new('RGBA', (SW * 16, SH * 16))
    for y in range(SH):
        for x in range(SW):
            h = hsh(x, y)
            im.alpha_composite(floor['Floor_0' if h % 3 else f'Floor_{1 + h // 3 % 5}'], (x * 16, y * 16))
    def open_(x, y): return (x, y) not in th and 0 <= x < SW and 0 <= y < SH
    for (x, y) in th:
        m = (1 if open_(x, y - 1) else 0) | (2 if open_(x + 1, y) else 0) | (4 if open_(x, y + 1) else 0) | (8 if open_(x - 1, y) else 0)
        c = 0
        for bit, (dx, dy), (a, b) in ((1, (1, -1), (1, 2)), (2, (1, 1), (4, 2)), (4, (-1, 1), (4, 8)), (8, (-1, -1), (1, 8))):
            if open_(x + dx, y + dy) and not (m & a) and not (m & b): c |= bit
        h = hsh(x, y, 2)
        v = hsh(x, y, 8) % 3
        tile = inners[0 if h % 3 else 1 + h // 3 % 5] if (m, c) == (0, 0) else edges.get((m, c, v), edges[(m, c, 0)])
        im.alpha_composite(tile.img(), (x * 16, y * 16))
    spill = {n: Image.open(os.path.join(HERE, '..', '..', 'floor3-environment', 'Smoke', 'deco', n + '.png')).convert('RGBA')
             for n in [f'Spill_Top_{k}' for k in range(4)] + [f'Spill_Left_{k}' for k in range(4)] + [f'Spill_Bottom_{k}' for k in range(3)]}
    for y in range(SH):                               # solapas del tramo sobre el suelo junto a la espesura
        for x in range(SW):
            if (x, y) in th: continue
            hv = hsh(x * 3, y * 5)
            if (x, y - 1) in th and hv % 2 == 0: im.alpha_composite(spill[f'Spill_Top_{hv // 2 % 4}'], (x * 16, y * 16))
            if (x - 1, y) in th and hv % 3 == 0: im.alpha_composite(spill[f'Spill_Left_{hv // 3 % 4}'], (x * 16, y * 16))
            if (x + 1, y) in th and hv % 3 == 1: im.alpha_composite(spill[f'Spill_Left_{hv // 3 % 4}'].transpose(Image.FLIP_LEFT_RIGHT), (x * 16, y * 16))
            if (x, y + 1) in th and hv % 2 == 1: im.alpha_composite(spill[f'Spill_Bottom_{hv // 2 % 3}'], (x * 16, y * 16))
    for by in range(0, SH - 1, 2):                    # coronas en bloques de 2×2 de interior
        for bx in range(0, SW - 1, 2):
            cells = [(bx + i, by + j) for i in range(-1, 3) for j in range(-1, 3)]
            if all(q in th for q in cells) and hsh(bx // 2, by // 2, 9) % 5 < 4:
                h = hsh(bx // 2, by // 2, 4)
                jx, jy = h % 9 - 4, (h // 9) % 9 - 4      # el código la desplaza hasta ±4 px dentro del bloque
                spr = trees[h // 81 % 3] if hsh(bx // 2, by // 2, 6) % 5 < 2 else crowns[h // 81 % 4]
                im.alpha_composite(spr.img(), (bx * 16 + 16 - spr.w // 2 + jx, by * 16 + 16 - spr.h // 2 + jy))
    return im.resize((im.width * scale, im.height * scale), Image.NEAREST), im


# =================================================================================================
# Subtanda C: suelo tras la caída de la espesura (F2)
# =================================================================================================
def mat_base(seed=3100):
    """Frondas tumbadas y aplastadas que dan la vuelta (encajan en mosaico); entre ellas se ve el suelo."""
    cv = WrapCanvas(16, 16, True, True)
    n8, n4, n2 = pnoise(seed, 16, 8), pnoise(seed + 1, 16, 4), pnoise(seed + 2, 16, 2)
    LIT = [DEEP[2], LEAF[0], OCHRE[0], LEAF[1], LEAF[2]]
    for y in range(16):
        for x in range(16):
            h = hsh(x, y, seed) % 100
            if h < 30: continue                       # huecos sueltos: se ve el suelo
            t = 1.2 + n2(x, y) * 1.6 + n8(x, y) * .6 + (1 if h > 93 else 0)
            cv.px[(x, y)] = LIT[max(0, min(4, dither(t, x, y)))]
    return cv

def mat(k, base):
    cv = copy_canvas(base); rnd = random.Random(3200 + k); box = (2, 2, 14, 14)
    if k in (1, 2, 3):                                # fronda tumbada, entera dentro del tile
        frondlet(cv, rnd.uniform(3, 6), rnd.uniform(4, 12), rnd.uniform(-.6, .6), rnd.uniform(7, 10), LEAF if k != 2 else OCHRE, .02, 2.2, box, 1)
    if k == 1:                                        # trozo de tallo
        x, y, a = rnd.uniform(4, 8), rnd.uniform(5, 10), rnd.uniform(-.4, .4)
        for st in range(7): cv.put(x + math.cos(a) * st, y + math.sin(a) * st, PULP[2] if st in (0, 6) else T['bark'][2]); cv.put(x + math.cos(a) * st, y + math.sin(a) * st + 1, T['bark'][0])
    elif k == 3:
        for i in range(5):
            x, y = rnd.randrange(3, 12), rnd.randrange(3, 13); c1 = rnd.choice((OCHRE[1], OCHRE[2], LEAF[3]))
            cv.put(x, y, c1); cv.put(x + 1, y, darker(c1, .75))
    return cv

def mat_edge(base, m, c, D=7):
    """La alfombra se aclara hacia los lados abiertos (claro o brecha): sin línea de corte."""
    cv = Canvas(16, 16)
    for (x, y), col in base.px.items():
        px, py = x + .5, y + .5; v = -99
        if m & 1: v = max(v, ed(px, 'N', D) - py)
        if m & 2: v = max(v, ed(py, 'E', D) - (16 - px))
        if m & 4: v = max(v, ed(px, 'S', D) - (16 - py))
        if m & 8: v = max(v, ed(py, 'W', D) - px)
        for bit, (kx, ky) in ((1, (16, 0)), (2, (16, 16)), (4, (0, 16)), (8, (0, 0))):
            if c & bit: v = max(v, D * .85 - math.hypot(px - kx, py - ky))
        h = hsh(x, y, 41) % 10
        keep = v < -2 or (v < 0 and h < 6) or (v < 2 and h < 3) or (v < 4 and h < 1)
        if keep: cv.px[(x, y)] = col
    return cv

def lying(S_w, S_h, base_pt, d, length, r, seed, kind):
    """Árbol (kind 'tree') o helecho (kind 'fern') tumbado en la dirección d desde base_pt."""
    cv = Canvas(S_w, S_h); part = Canvas(S_w, S_h); rnd = random.Random(seed)
    dx, dy = d; n = math.hypot(dx, dy); dx, dy = dx / n, dy / n
    px_, py_ = -dy, dx
    bx, by = base_pt; bark = [C(c) for c in ('#1e1a14', '#2e281e', '#463c2a', '#62543a', '#82704c')]
    lx, ly = -.6, -.8
    for y in range(S_h):                              # tronco: cilindro con los cojinetes en rombo
        for x in range(S_w):
            qx, qy = x + .5 - bx, y + .5 - by
            t = qx * dx + qy * dy; s_ = qx * px_ + qy * py_
            jag = 1.5 * abs(math.sin(s_ * 2.1)) if t < 2 else 0          # base partida, en dientes
            if jag <= t <= length and abs(s_) <= r:
                nn = s_ / r; lit = nn * (px_ * lx + py_ * ly)
                i = 4 if lit > .55 else 3 if lit > .2 else 2 if lit > -.2 else 1 if abs(nn) < .85 else 0
                u, w = t * 1.0, s_ * 2.0
                if (round(u + w) % 5 == 0 or round(u - w) % 5 == 0) and i > 0: i -= 1
                part.put(x, y, bark[i] if t > jag + 1.2 else PULP[2 if lit > 0 else 1])
    tip = (bx + dx * length, by + dy * length)
    if kind == 'tree':
        for j in (-1, 1):                             # ramas que se abren y penachos aplastados
            a = math.atan2(dy, dx) + j * rnd.uniform(.35, .6); L = rnd.uniform(7, 10)
            ex, ey = tip[0] + math.cos(a) * L, tip[1] + math.sin(a) * L
            for st in range(int(L * 2)):
                part.put(tip[0] + (ex - tip[0]) * st / (L * 2), tip[1] + (ey - tip[1]) * st / (L * 2), bark[2])
            for (tx, ty) in ((ex, ey), ((ex + tip[0]) / 2 + j * px_ * 3, (ey + tip[1]) / 2 + j * py_ * 3)):
                rr = rnd.uniform(3.5, 4.5)
                fill(part, lambda X, Y: ((X - tx) / rr) ** 2 + ((Y - ty) / (rr * .7)) ** 2 <= 1, tx, ty, rr, rr * .7, TUFT[:4])
                for q in range(18):
                    aa = q * 6.283 / 18; LL = rnd.uniform(2, 5)
                    for st in range(int(LL)):
                        X, Y = tx + math.cos(aa) * (rr * .8 + st), ty + math.sin(aa) * (rr * .55 + st * .7)
                        part.put(X, Y, TUFT[2] if math.cos(aa) * lx + math.sin(aa) * ly > 0 else TUFT[1])
    else:                                             # helecho: frondas en abanico, tumbadas
        a0 = math.atan2(dy, dx)
        for f in range(7):
            a = a0 + (f - 3) * .32 + rnd.uniform(-.08, .08)
            frondlet(part, tip[0], tip[1], a, rnd.uniform(10, 14), CROWN if f % 3 else OCHRE, .02, 2.6, (0, 0, S_w, S_h), 1 if f % 2 else 0)
    for (x, y) in list(part.px):                      # sombra pegada al suelo, abajo a la derecha
        q = (x + 1, y + 2)
        if q not in part.px and 0 <= q[0] < S_w and 0 <= q[1] < S_h: cv.px[q] = SHADOW[:3] + (110,)
    for p_, col in part.px.items(): cv.px[p_] = col
    return cv

def fallen_pieces():
    return {'Fallen_Tree_0': lying(60, 30, (4, 15), (1, .08), 30, 3.6, 3301, 'tree'),
            'Fallen_Tree_1': lying(60, 30, (4, 13), (1, -.12), 27, 3.2, 3302, 'tree'),
            'Fallen_Tree_V': lying(30, 60, (15, 4), (.06, 1), 30, 3.6, 3303, 'tree'),
            'Fallen_Fern_0': lying(40, 28, (5, 15), (1, .1), 9, 2.2, 3304, 'fern'),
            'Fallen_Fern_1': lying(40, 28, (5, 12), (1, -.15), 8, 2.0, 3305, 'fern')}

def sample_fallen(edges, dense, light, mats, medges, pieces, scale=2):
    th = thicket_cells()
    clear = lambda x, y: CL[0] <= x < CL[2] and CL[1] <= y < CL[3]
    floor = {n: Image.open(os.path.join(CAPDIR, n + '.png')).convert('RGBA') for n in ('Floor_0', 'Floor_1', 'Floor_2', 'Floor_3', 'Floor_4', 'Floor_5')}
    im = Image.new('RGBA', (SW * 16, SH * 16))
    for y in range(SH):
        for x in range(SW):
            h = hsh(x, y)
            im.alpha_composite(floor['Floor_0' if h % 3 else f'Floor_{1 + h // 3 % 5}'], (x * 16, y * 16))
    breach = {(x, y) for y in range(SH) for x in range(SW) if (x, y) not in th and not clear(x, y)}
    for (x, y) in breach:
        near = any((x + dx, y + dy) in th for dx in (-1, 0, 1) for dy in (-1, 0, 1))
        im.alpha_composite((dense if near else light)[hsh(x, y, 21) % 4].img(), (x * 16, y * 16))
    def gone(x, y): return (x, y) not in th and 0 <= x < SW and 0 <= y < SH
    for (x, y) in th:                                 # alfombra donde había espesura, aclarándose hacia lo abierto
        m = (1 if gone(x, y - 1) else 0) | (2 if gone(x + 1, y) else 0) | (4 if gone(x, y + 1) else 0) | (8 if gone(x - 1, y) else 0)
        c = 0
        for bit, (ddx, ddy), (a, b) in ((1, (1, -1), (1, 2)), (2, (1, 1), (4, 2)), (4, (-1, 1), (4, 8)), (8, (-1, -1), (1, 8))):
            if gone(x + ddx, y + ddy) and not (m & a) and not (m & b): c |= bit
        tile = mats[hsh(x, y, 31) % 4] if (m, c) == (0, 0) else medges[(m, c)]
        im.alpha_composite(tile.img(), (x * 16, y * 16))
    for by in range(0, SH - 1, 2):                    # lo que dejan las copas al caer (mismas celdas que en la F1)
        for bx in range(0, SW - 1, 2):
            cells = [(bx + i, by + j) for i in range(-1, 3) for j in range(-1, 3)]
            if all(q in th for q in cells) and hsh(bx // 2, by // 2, 9) % 5 < 4:
                h = hsh(bx // 2, by // 2, 4)
                if hsh(bx // 2, by // 2, 6) % 5 < 2 and h % 2: name = ('Fallen_Tree_0', 'Fallen_Tree_1', 'Fallen_Tree_V')[h // 81 % 3]
                elif h % 3 == 1: name = f'Fallen_Fern_{h // 81 % 2}'
                else: continue
                spr = pieces[name].img()
                if h % 3 == 0 and 'V' not in name: spr = spr.transpose(Image.FLIP_LEFT_RIGHT)
                jx, jy = h % 13 - 6, (h // 13) % 13 - 6       # desplazado hasta ±6 px
                im.alpha_composite(spr, (bx * 16 + 16 - spr.width // 2 + jx, by * 16 + 16 - spr.height // 2 + jy))
    return im.resize((im.width * scale, im.height * scale), Image.NEAREST)


def sample_breach(small, edges, breaks, dense, light, tracks, scale=2):
    th = thicket_cells(); im = small.copy()
    clear = lambda x, y: CL[0] <= x < CL[2] and CL[1] <= y < CL[3]
    breach = {(x, y) for y in range(SH) for x in range(SW) if (x, y) not in th and not clear(x, y)}
    for (x, y) in breach:                             # restos: densos junto a la espesura, ligeros en el centro
        near = any((x + dx, y + dy) in th for dx in (-1, 0, 1) for dy in (-1, 0, 1))
        im.alpha_composite((dense if near else light)[hsh(x, y, 21) % 4].img(), (x * 16, y * 16))
    for (ox, oy), (dx, dy), w in BREACHES:            # surcos girados por el eje de la brecha, cada 2 u
        n = math.hypot(dx, dy); ang = math.degrees(math.atan2(dy, dx))
        for i, t in enumerate(range(0, 60, 2)):
            px, py = ox + dx / n * t, oy + dy / n * t
            if (int(px), int(py)) not in breach: continue
            tr = tracks[i % 2].img().rotate(-ang, expand=True, resample=Image.NEAREST)
            X, Y = int(px * 16 - tr.width / 2), int(py * 16 - tr.height / 2)
            if X >= 0 and Y >= 0 and X + tr.width <= im.width and Y + tr.height <= im.height: im.alpha_composite(tr, (X, Y))
    def open_(x, y): return (x, y) not in th and 0 <= x < SW and 0 <= y < SH
    for (x, y) in th:                                 # filo roto en la espesura que da a la brecha
        if not any((x + dx, y + dy) in breach for dx in (-1, 0, 1) for dy in (-1, 0, 1)): continue
        m = (1 if open_(x, y - 1) else 0) | (2 if open_(x + 1, y) else 0) | (4 if open_(x, y + 1) else 0) | (8 if open_(x - 1, y) else 0)
        c = 0
        for bit, (ddx, ddy), (a, b) in ((1, (1, -1), (1, 2)), (2, (1, 1), (4, 2)), (4, (-1, 1), (4, 8)), (8, (-1, -1), (1, 8))):
            if open_(x + ddx, y + ddy) and not (m & a) and not (m & b): c |= bit
        v = hsh(x, y, 8) % 3
        key = (m, c, v) if (m, c, v) in breaks else (m, c, 0)
        if key in breaks: im.alpha_composite(breaks[key].img(), (x * 16, y * 16))
    return im.resize((im.width * scale, im.height * scale), Image.NEAREST)

if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    base = load16('Wall_Cap')
    inners = [inner(base, k) for k in range(6)]
    edges = {(m, c, v): edge_tile(base, m, c, v) for m, c in valid_edges() for v in range(3) if v == 0 or m & 4 or c & 6}
    crowns = [crown(k) for k in range(4)]
    for k, cv in enumerate(inners): cv.img().save(os.path.join(OUT, f'Thicket_Inner_{k}.png'))
    for (m, c, v), cv in edges.items(): cv.img().save(os.path.join(OUT, f'Thicket_Edge_{m}_{c}' + (f'_v{v}' if v else '') + '.png'))
    trees = [tree_crown(k) for k in range(3)]
    for k, cv in enumerate(trees): cv.img().save(os.path.join(OUT, f'Thicket_TreeCrown_{k}.png'))
    for k, cv in enumerate(crowns): cv.img().save(os.path.join(OUT, f'Thicket_Crown_{k}.png'))
    order = sorted(edges)
    sheet = Image.new('RGBA', (8 * 16, ((len(order) + 6 + 7) // 8) * 16))
    for i, cv in enumerate(inners + [edges[k] for k in order]): sheet.alpha_composite(cv.img(), ((i % 8) * 16, (i // 8) * 16))
    sheet.save(os.path.join(OUT, 'Thicket_Tileset.png'))
    with open(os.path.join(OUT, 'Thicket_Tileset.txt'), 'w', encoding='utf-8') as f:
        f.write('Orden de Thicket_Tileset.png (8 columnas, de izquierda a derecha y de arriba abajo):\n')
        for i, name in enumerate([f'Thicket_Inner_{k}' for k in range(6)] + [f'Thicket_Edge_{m}_{c}' + (f'_v{v}' if v else '') for m, c, v in order]):
            f.write(f'{i}: {name}\n')
    big, small = sample(base, inners, edges, crowns, trees)
    big.save(os.path.join(HERE, 'vista_previa_espesura.png'))
    breaks = {k: break_overlay(*k) for k in edges}
    dense = [debris(k, True) for k in range(4)]; light = [debris(k, False) for k in range(4)]
    tracks = [track(k) for k in range(2)]
    BD = os.path.join(HERE, 'Breach'); os.makedirs(BD, exist_ok=True)
    for (m, c, v), cv in breaks.items(): cv.img().save(os.path.join(BD, f'Thicket_Break_{m}_{c}' + (f'_v{v}' if v else '') + '.png'))
    for k in range(4):
        dense[k].img().save(os.path.join(BD, f'Breach_Debris_Dense_{k}.png')); light[k].img().save(os.path.join(BD, f'Breach_Debris_Light_{k}.png'))
    for k, cv in enumerate(tracks): cv.img().save(os.path.join(BD, f'Breach_Track_{k}.png'))
    sample_breach(small, edges, breaks, dense, light, tracks).save(os.path.join(HERE, 'vista_previa_brechas.png'))
    mb = mat_base(); mats = [mat(k, mb) if k else copy_canvas(mb, Canvas) for k in range(4)]
    medges = {(m, c): mat_edge(mb, m, c) for m, c in valid_edges()}
    pieces = fallen_pieces()
    FD = os.path.join(HERE, 'Fallen'); os.makedirs(FD, exist_ok=True)
    for k, cv in enumerate(mats): cv.img().save(os.path.join(FD, f'Fallen_Mat_{k}.png'))
    for (m, c), cv in medges.items(): cv.img().save(os.path.join(FD, f'Fallen_Edge_{m}_{c}.png'))
    for name, cv in pieces.items(): cv.img().save(os.path.join(FD, name + '.png'))
    sample_fallen(edges, dense, light, mats, medges, pieces).save(os.path.join(HERE, 'vista_previa_caida.png'))
    preview(os.path.join(HERE, 'vista_previa_caida_piezas.png'), {'Alfombra': mats, 'Bordes (muestra)': [medges[k] for k in sorted(medges)[:10]],
        'Troncos y helechos': list(pieces.values())}, BG, scale=3, cols=10)
    preview(os.path.join(HERE, 'vista_previa_brechas_piezas.png'), {
        'Restos densos': dense, 'Restos ligeros': light, 'Surcos': tracks,
        'Filo roto (muestra)': [breaks[k] for k in sorted(breaks)[:12]]}, BG, scale=4, cols=12)
    print('Hecho:', len(edges), 'bordes')
