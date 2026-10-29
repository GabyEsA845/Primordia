"""Tilesets del piso 3 de Primordia: los tres tramos del incendio (Swamp, Smoke, Burnt).

Por tramo, en <Tema>/: tiles de suelo, tapa, caras (en pares de arriba y abajo), bordes, esquinas y los 6 de
formas; solapas de espesura que se salen al suelo; lodo profundo (16 máscaras como las fosas, 4 esquinas y
burbujas); arco y puertas de raíces (cerrar y abrir); maleza quemable (arriba, lado, abajo; quieta y ardiendo);
y una sala de muestra armada con las mismas reglas que WallRules.cs. Ver README.md.

Todo encaja en mosaico: lo que toca los bordes de un tile se dibuja igual en todas sus variantes (lienzo que se da
la vuelta, misma semilla) y lo propio de cada variante queda dentro.

Uso: python generar_tilesets_piso3.py
"""
import math, os, random
from base_piso3 import *
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
SEED = {'Swamp': 300, 'Smoke': 340, 'Burnt': 380}

# =================================================================================================
# Suelo
# =================================================================================================
def floor_base(t, seed):
    cv = WrapCanvas(16, 16, True, True)
    n8, n4 = pnoise(seed, 16, 8), pnoise(seed + 3, 16, 4)
    for y in range(16):
        for x in range(16):
            v = 1.15 + n8(x, y) * .6 + n4(x, y) * .9
            cv.px[(x, y)] = t['floor'][min(3, max(0, dither(v, x, y)))]
    return cv

def floor_detail(cv, t, k, seed):
    """Lo propio de cada variante de suelo, siempre dentro (de 2 a 13): mancha de tono y detalles del tramo."""
    rnd = random.Random(seed * 7 + k)
    cx, cy, rx, ry = rnd.uniform(5, 11), rnd.uniform(5, 11), rnd.uniform(3, 5), rnd.uniform(2.5, 4)
    sh = rnd.choice((-1, 1))
    for y in range(2, 14):
        for x in range(2, 14):
            d = ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2
            if d < 1 and (d < .55 or (x + y) % 2 == 0):
                if cv.px[(x, y)] not in t['floor']: continue
                i = t['floor'].index(cv.px[(x, y)])
                cv.px[(x, y)] = t['floor'][max(0, min(3, i + sh))]
    fl, lf = t['fleck'], t['leaf']
    def clump(x, y):                                  # mata de musgo / hojarasca / ceniza de 3-4 px
        cv.put(x, y, fl[1]); cv.put(x + 1, y, fl[0]); cv.put(x, y - 1, fl[2]); cv.put(x - 1, y, fl[0])
    if t['key'] == 'Swamp':
        kinds = [('musgo', 2), ('brote', 1), ('piedra', 1), ('charquito', 1), ('musgo', 1), ('brote', 2)]
    elif t['key'] == 'Smoke':
        kinds = [('hoja', 3), ('ceniza', 2), ('ramita', 1), ('hoja', 2), ('piedra', 1), ('ceniza', 3)]
    else:
        kinds = [('grieta', 1), ('ceniza', 3), ('carbon', 2), ('ceniza', 2), ('grieta', 1), ('carbon', 1)]
    kind, n = kinds[k % len(kinds)]
    for _ in range(n):
        x, y = rnd.randrange(3, 12), rnd.randrange(4, 12)
        if kind == 'musgo': clump(x, y); clump(x + 3, y + 1) if rnd.random() < .5 else None
        elif kind == 'brote':                         # brote de helecho enrollado
            cv.put(x, y, FERN_C[1]); cv.put(x, y - 1, FERN_C[2]); cv.put(x + 1, y - 2, FERN_C[3]); cv.put(x + 1, y - 1, FERN_C[1])
        elif kind == 'piedra':
            for dx, dy, i in ((0, 0, 2), (1, 0, 1), (0, -1, 3), (1, -1, 2), (2, 0, 1)): cv.put(x + dx, y + dy, STONE_C[i])
            cv.put(x, y + 1, t['floor'][0]); cv.put(x + 1, y + 1, t['floor'][0])
        elif kind == 'charquito':                     # gotas de agua turbia que brillan
            for dx in range(3): cv.put(x + dx, y, WATER_C[1])
            cv.put(x + 1, y - 1, WATER_C[1]); cv.put(x, y, WATER_C[3])
        elif kind == 'hoja':                          # hoja seca caída
            cv.put(x, y, fl[1]); cv.put(x + 1, y - 1, fl[2]); cv.put(x + 1, y, fl[0])
        elif kind == 'ceniza':
            for j in range(rnd.randrange(3, 6)):
                cv.put(x + rnd.randrange(-2, 3), y + rnd.randrange(-1, 2), fl[rnd.randrange(3)])
        elif kind == 'ramita':
            for j in range(5): cv.put(x + j, y - j // 2, fl[0] if j % 2 else fl[1])
        elif kind == 'carbon':
            cv.put(x, y, C(CHAR[2])); cv.put(x + 1, y, C(CHAR[3])); cv.put(x, y + 1, C(CHAR[0])); cv.put(x + 1, y + 1, C(CHAR[1]))
        elif kind == 'grieta':                        # grieta con ascuas
            xx, yy = 3, rnd.randrange(5, 11)
            for j in range(9):
                cv.put(xx + j, yy, C(EMBER[1] if j % 3 else EMBER[2]))
                if j % 4 == 1: cv.put(xx + j, yy - 1, C(EMBER[0]))
                yy += rnd.choice((-1, 0, 0, 1)); yy = max(3, min(12, yy))
            cv.put(xx + 4, yy - 1, C(EMBER[3]))
    return cv

FERN_C = [C(c) for c in FERN]
WATER_C = [C(c) for c in WATER]
STONE_C = [C(c) for c in ('#2e2c34', '#4a4650', '#6a6670', '#8e8a90')]

def floor_shadow(base, t, k):
    cv = copy_canvas(base)
    for y in range(4):
        for x in range(16):
            if y < 2 or (x + y + k) % 2 == 0: cv.px[(x, y)] = t['floor'][0]
    return cv

# =================================================================================================
# Tapa (espesura vista desde arriba)
# =================================================================================================
def canopy(t, seed, extra=0):
    cv = WrapCanvas(16, 16, True, True)
    n = pnoise(seed, 16, 4)
    for y in range(16):
        for x in range(16):
            cv.px[(x, y)] = t['cap'][0] if n(x, y) < .45 else t['cap'][1]
    rnd = random.Random(seed)
    if t['burnt']:
        ch = [C(c) for c in CHAR]
        for layer in range(3):
            for k in range(2):
                cx, cy = rnd.uniform(0, 16), rnd.uniform(0, 16)
                for f in range(3): twig(cv, cx, cy, rnd.uniform(0, 6.28), rnd.uniform(4, 8), ch[:4] if layer == 2 else [ch[0], ch[1], ch[1], ch[2]], None, rnd.random() < .04, rnd)
        for k in range(5): cv.put(rnd.randrange(16), rnd.randrange(16), C(rnd.choice(('#4a4346', '#6d6466', '#958a88'))))
    else:
        for layer in range(3):
            for k in range(3 if layer < 2 else 2):
                cx, cy = rnd.uniform(0, 16), rnd.uniform(0, 16)
                nf = rnd.randrange(5, 8); a0 = rnd.uniform(0, 6.28)
                for f in range(nf):
                    frondlet(cv, cx, cy, a0 + f * 6.283 / nf + rnd.uniform(-.2, .2), rnd.uniform(5, 8), t['leaf'], .12, 2, None, 3 - layer)
    if extra:                                         # variante: una mata más, dentro del tile
        r2 = random.Random(seed * 13 + extra)
        cv.clip = (2, 2, 14, 14)
        cx, cy = r2.uniform(6, 10), r2.uniform(6, 10)
        if t['burnt']:
            for f in range(3): twig(cv, cx, cy, r2.uniform(0, 6.28), r2.uniform(4, 6), [C(c) for c in CHAR], None, True, r2)
            cv.put(cx, cy, C(EMBER[2]) if extra == 2 else C(CHAR[3]))
        else:
            nf = r2.randrange(6, 8); a0 = r2.uniform(0, 6.28)
            for f in range(nf): frondlet(cv, cx, cy, a0 + f * 6.283 / nf, r2.uniform(4, 6), t['leaf'], .1, 2, None, 0)
        cv.clip = None
    return cv

# =================================================================================================
# Cara de la pared de arriba (2 tiles de alto: arriba y abajo en pares)
# =================================================================================================
def face_pair(t, seed, var):
    """Cara 16×32: espesura vista de frente. Lo compartido (fondo, frondas lejanas, cortina, musgo, helechos del
    pie) da la vuelta en x y es igual en todas las variantes; troncos, raíces y extras de cada variante van dentro."""
    cv = WrapCanvas(16, 32, True, False)
    rnd = random.Random(seed)
    rv = random.Random(seed * 31 + var)
    cap, leaf, bark, vn = t['cap'], t['leaf'], t['bark'], t['vein']
    inner = (2, 0, 14, 32)
    if t['burnt']:
        glow = C('#3a1812'); haze = C('#5a4a48')
        for y in range(32):
            k = y / 32
            for x in range(16):
                cv.px[(x, y)] = cap[0] if k < .5 or (k < .75 and (x + y) % 2) else blend(cap[0], glow, int(255 * (k - .4)))
        for k in range(1 if var else 2):              # troncos lejanos partidos (de cada variante, dentro)
            tx = rv.randrange(3, 12); wd = rv.randrange(2, 4); top = rv.randrange(2, 16)
            for y in range(top, 32):
                for dx in range(wd): cv.put(tx + dx, y, C(CHAR[1]) if dx else C(CHAR[2]))
            cv.put(tx, top - 1, C(CHAR[1]))
        for band in range(2):                         # velo de humo
            yy = 8 + band * 11; ph = rnd.uniform(0, 6)
            for x in range(16):
                y = yy + round(math.sin(x * math.pi / 8 + ph) * 1.5)
                for dy in (0, 1, 2):
                    if (x + dy) % 2 == 0: cv.px[(x, y + dy)] = blend(cv.px.get((x, y + dy)), haze, 60)
        cv.clip = inner
        if var == 1:                                  # tronco carbonizado de pie
            bark_trunk(cv, 3, 9, 0, 32, bark, EMBER[2], seed + 1)
            twig(cv, 7, rv.randrange(6, 14), rv.choice((-.4, 3.5)), rv.uniform(5, 7), [C(c) for c in CHAR], None, True, rv)
        elif var == 2:                                # tronco partido con la punta encendida
            top = rv.randrange(8, 13)
            bark_trunk(cv, 4, 8, top, 32, bark, EMBER[2], seed + 2)
            for dx in range(8):
                for j in range(rv.randrange(0, 3)): cv.put(4 + dx, top + j, cap[0])
            cv.put(8, top + 2, C(EMBER[3])); cv.put(7, top + 2, C(EMBER[2])); cv.put(8, top + 1, C(FIRE[3]))
        elif var == 3:                                # trozo de tronco caído al pie, con ascuas
            for i in range(11):
                yc = 27 + (i > 6)
                for dy in range(-3, 3):
                    ny = dy / 3
                    cv.put(3 + i, yc + dy, C(CHAR[1] if ny > .5 else CHAR[2] if ny > -.3 else CHAR[3]))
                if i % 5 == 2: cv.put(3 + i, yc - 1, C(EMBER[1])); cv.put(4 + i, yc - 1, C(EMBER[2]))
            for dy in range(-3, 3): cv.put(13, 28 + dy, C(CHAR[4]))
            cv.put(13, 27, C(EMBER[0]))
        else:
            for k in range(2): twig(cv, rv.uniform(4, 12), rv.uniform(8, 26), rv.uniform(0, 6.28), rv.uniform(4, 6), [C(c) for c in CHAR], None, True, rv)
        cv.clip = None
        for k in range(3):                            # ramas que cuelgan de la copa (compartidas)
            twig(cv, rnd.uniform(0, 16), rnd.uniform(-2, 1), math.pi / 2 + rnd.uniform(-.8, .8), rnd.uniform(4, 8), [C(c) for c in CHAR], None, True, rnd)
        for x in range(16): cv.px[(x, 31)] = cap[0]
        return cv
    for y in range(32):                               # fondo en sombra
        for x in range(16):
            cv.px[(x, y)] = cap[1] if y < 11 and (x + y) % 2 == 0 else cap[0]
    for k in range(3):                                # frondas lejanas (compartidas)
        frondlet(cv, rnd.uniform(0, 16), rnd.uniform(-2, 8), math.pi / 2 + rnd.uniform(-1.1, 1.1), rnd.uniform(8, 14), leaf, .4, 2.5, None, 2)
    cv.clip = inner                                   # troncos de cada variante (detrás de la cortina)
    if var == 1: bark_trunk(cv, 3, 10, 0, 32, bark, None, seed + 1)
    elif var == 2: bark_trunk(cv, 5, 6, 0, 32, bark, None, seed + 2)
    cv.clip = None
    for k in range(3):                                # cortina de frondas desde la copa (compartida)
        frondlet(cv, rnd.uniform(0, 16), rnd.uniform(-3, 3), math.pi / 2 + rnd.uniform(-1.2, 1.2), rnd.uniform(9, 14), leaf, .45, 3, None, 1)
    cv.clip = inner
    if var == 2:                                      # raíces que cuelgan por delante
        for k in range(3):
            mx = rv.randrange(3, 13); L = rv.randrange(10, 22)
            for j in range(L): cv.put(mx + round(math.sin(j / 3 + mx) * .7), 4 + j, t['root'][3] if j % 5 else t['root'][2])
    elif var == 3:                                    # mucho musgo colgando y un helecho delante
        for k in range(4):
            mx = rv.randrange(3, 13); L = rv.randrange(6, 16)
            for j in range(L): cv.put(mx + round(math.sin(j / 3 + mx) * .6), j, vn[2] if j == 0 else vn[1] if j < L - 2 else vn[0])
        for f in range(3): frondlet(cv, rv.uniform(6, 10), 31, -math.pi / 2 + rv.uniform(-.9, .9), rv.uniform(8, 12), leaf, .35, 2.5, None, 0)
    elif var == 0:
        frondlet(cv, rv.uniform(5, 11), rv.uniform(10, 16), math.pi / 2 + rv.uniform(-.6, .6), rv.uniform(7, 10), leaf, .4, 3, None, 0)
    if var in (0, 1):                                 # un mechón de musgo en algunas variantes
        mx = rv.randrange(4, 12); L = rv.randrange(5, 11)
        for j in range(L): cv.put(mx + round(math.sin(j / 3 + mx) * .6), j, vn[2] if j == 0 else vn[1] if j < L - 2 else vn[0])
    cv.clip = None
    for k in range(2):                                # helechos que suben desde el pie (compartidos)
        frondlet(cv, rnd.uniform(0, 16), 31, -math.pi / 2 + rnd.uniform(-1.0, 1.0), rnd.uniform(7, 11), leaf, .35, 2.5, None, rnd.choice((0, 1)))
    if t['smoke']:                                    # humo entre las hojas (compartido)
        for k in range(3):
            yy = 6 + k * 9; ph = rnd.uniform(0, 6)
            for x in range(16):
                if math.sin(x * math.pi / 8 + ph) > .1:
                    y = yy + round(math.sin(x * math.pi / 4 + ph) * 1.5)
                    for dy in (0, 1):
                        if (x + dy) % 2 == 0: cv.px[(x, y + dy)] = blend(cv.px.get((x, y + dy)), C('#8a8a7a'), 70)
    for x in range(16): cv.px[(x, 31)] = cap[0]       # pie en sombra contra el suelo
    return cv

# =================================================================================================
# La copa cuelga sobre la cara (une la tapa con la cara)
# =================================================================================================
def oh_depth(x):  # dos lóbulos de copa por tile, de 1,5 a 6 px
    return 1.5 + 4.2 * abs(math.sin(2 * math.pi * x / 16 + .9)) ** .7 + .7 * math.sin(2 * math.pi * x / 16 + 2.4)

def overhang(cv, t, cap):
    """Textura de la tapa hasta un borde que ondula, filo de hojas, sombra debajo y dos puntas colgando cada 16 px.
    Igual en todas las caras (periodo 16): encaja con la tapa de arriba y entre vecinas."""
    lf = [C(c) for c in CHAR[:4]] if t['burnt'] else t['leaf']
    for x in range(cv.w):
        d = max(1, round(oh_depth(x)))
        for y in range(d): cv.px[(x, y)] = cap.px[(x % 16, y % 16)]
        cv.px[(x, d)] = lf[3] if x % 4 == 1 else lf[1] if x % 5 == 3 else lf[2]
        if d > 0 and x % 3 == 0: cv.px[(x, d - 1)] = lf[2]      # hojas en el filo del lóbulo
        if (x, d + 1) in cv.px: cv.px[(x, d + 1)] = darker(cv.px[(x, d + 1)], .55)
        if (x, d + 2) in cv.px and x % 2 == 0: cv.px[(x, d + 2)] = darker(cv.px[(x, d + 2)], .75)
    for X in range(0, cv.w, 16):
        for dx, ang, L in ((2, math.pi / 2 + .45, 6), (7, math.pi / 2 - .1, 5), (12, math.pi / 2 - .4, 7)):
            x = X + dx; y = round(oh_depth(x))
            if t['burnt']: twig(cv, x, y, ang, L - 1, [C(c) for c in CHAR], (0, 0, cv.w, cv.h), x % 3 == 0, random.Random(x))
            else: frondlet(cv, x, y, ang, L, lf, .3, 2, (0, 0, cv.w, cv.h), 0)
    return cv

def edge_beside_face(t, cap, face0, side):
    """Columna de pared junto a la cara (16x32, arriba y abajo): la espesura acaba en un borde que ondula y deja ver
    la cara que sigue detrás, no el suelo. side 'E': pared a la izquierda de la cara (EdgeLeft); 'W': a la derecha."""
    cv = tiled(cap, 16, 32)
    op = set()
    for y in range(32):
        for x in range(16):
            extra = 3 * min(1, (31 - y) / 8)            # entra más junto a la cara y se afina hacia el suelo
            dd = (15 - x) - depth_v(y, PH['E']) - extra if side == 'E' else x - depth_v(y, PH['W']) - extra
            if dd < 0 and y > oh_depth(x) + 1: op.add((x, y))
    for p in op: cv.px[p] = face0.px[p]
    lf = [C(c) for c in CHAR[:4]] if t['burnt'] else t['leaf']
    for (x, y) in list(cv.px):
        if (x, y) in op: continue
        if any((x + dx, y + dy) in op for dx, dy in N4): cv.px[(x, y)] = lf[2] if (x + y) % 3 else lf[1]
    for (x, y) in op:
        if any((x + dx, y + dy) not in op and 0 <= x + dx < 16 and 0 <= y + dy < 32 for dx, dy in N4):
            cv.px[(x, y)] = darker(cv.px[(x, y)], .6)
    return crop(cv, 0, 0, 16, 16), crop(cv, 0, 16, 16, 16)

# =================================================================================================
# Bordes: donde la espesura da al suelo, el borde ondula (mismo dibujo en todos los tiles de ese lado)
# =================================================================================================
def depth_v(y, ph):  # lados izquierdo/derecho: cuánto suelo se ve, según la fila (periodo 16)
    return 1.6 + 1.1 * math.sin(2 * math.pi * y / 16 + ph) + .5 * math.sin(4 * math.pi * y / 16 + ph * 2.3)
def depth_h(x, ph):  # lado de arriba: según la columna
    return 1.4 + 1.0 * math.sin(2 * math.pi * x / 16 + ph) + .5 * math.sin(4 * math.pi * x / 16 + ph * 1.7)
PH = {'N': .7, 'E': 2.1, 'W': 4.0}

def open_mask(w, h, sides=(), diags=()):
    """Píxeles del tile que dejan ver el suelo. sides: lados con suelo ('N', 'E', 'W'); diags: esquinas con suelo
    solo en diagonal ('NE', 'NW'). Las esquinas de fuera (dos lados) se redondean."""
    R = 4
    out = set()
    for y in range(h):
        for x in range(w):
            dN = y - depth_h(x, PH['N']) if 'N' in sides or any('N' in d for d in diags) else 99
            dE = (w - 1 - x) - depth_v(y, PH['E']) if 'E' in sides or 'NE' in diags else 99
            dW = x - depth_v(y, PH['W']) if 'W' in sides or 'NW' in diags else 99
            o = False
            for s, d in (('N', dN), ('E', dE), ('W', dW)):
                if s in sides and d < 0: o = True
            if 'NE' in diags and dN < 0 and dE < 0: o = True
            if 'NW' in diags and dN < 0 and dW < 0: o = True
            for a, b, da, db in (('N', 'W', dN, dW), ('N', 'E', dN, dE)):
                if a in sides and b in sides and 0 <= da < R and 0 <= db < R and math.hypot(R - da, R - db) > R: o = True
            if o: out.add((x, y))
    return out

def open_edges(cv, t, floor_tile, sides=(), diags=()):
    """Deja ver el suelo por los lados abiertos y pone el borde de hojas: puntas claras donde da la luz (lados de
    arriba e izquierda del hueco) y en sombra en los otros; el suelo de al lado, con la sombra de la espesura."""
    op = open_mask(cv.w, cv.h, sides, diags)
    if not op: return cv
    out = copy_canvas(cv, Canvas)
    for (x, y) in op: out.px[(x, y)] = floor_tile.px[(x % 16, y % 16)]
    for (x, y) in op:                                 # sombra de la espesura sobre el suelo
        if any((x + dx, y + dy) not in op and 0 <= x + dx < cv.w and 0 <= y + dy < cv.h for dx, dy in N4):
            out.px[(x, y)] = t['floor'][0]
    lf = [C(c) for c in CHAR[1:]] if t['burnt'] else t['leaf']
    rim = {}
    for (x, y) in list(cv.px):
        if (x, y) in op: continue
        nb = [(dx, dy) for dx, dy in N4 if (x + dx, y + dy) in op]
        if nb:
            lit = any(d in ((0, 1), (1, 0)) for d in nb) is False or (0, 1) in nb and (x % 3 != 0)
            rim[(x, y)] = lf[3] if (0, 1) in nb or (1, 0) in nb and (x + y) % 2 else lf[2] if lit else lf[1]
    for p, c in rim.items(): out.px[p] = c
    for (x, y), c in list(rim.items()):               # segunda fila de hojas
        for dx, dy in N4:
            q = (x + dx, y + dy)
            if q in out.px and q not in op and q not in rim and (x + y) % 2 == 0: out.px[q] = lf[2] if t['burnt'] else lf[1]
    if t['burnt']:
        for (x, y) in list(rim)[::5]: out.px[(x, y)] = C('#6d6466')    # ceniza en el filo
    return out

# =================================================================================================
# Solapas: la espesura se sale de las paredes hacia el suelo (capa encima del suelo, sin colisión)
# =================================================================================================
def spill(t, side, k, seed):
    cv = Canvas(16, 16)
    rnd = random.Random(seed * 17 + k * 5 + {'Top': 0, 'Left': 100, 'Bottom': 200}[side])
    box = {'Top': (1, 0, 15, 15), 'Left': (0, 1, 15, 15), 'Bottom': (1, 1, 15, 16)}[side]   # solo toca el lado de la pared
    ch = [C(c) for c in CHAR]
    n = 2 + (k % 2)
    for i in range(n):
        u = 4 + i * 8 / max(1, n - 1) + rnd.uniform(-1, 1)
        if side == 'Top': x, y, ang, L = u, -1, math.pi / 2 + rnd.uniform(-.6, .6), rnd.uniform(5, 9)
        elif side == 'Left': x, y, ang, L = -1, u, rnd.uniform(-.35, .5), rnd.uniform(5, 9)
        else: x, y, ang, L = u, 16, -math.pi / 2 + rnd.uniform(-.6, .6), rnd.uniform(3, 6)
        if t['burnt']: twig(cv, x, y, ang, L * .8, ch, box, True, rnd)
        else: frondlet(cv, x, y, ang, L, t['leaf'], .3, 2.5, box, rnd.choice((0, 0, 1)))
    return cv

# =================================================================================================
# Lodo profundo: 16 tiles por máscara (vecinos que NO son lodo: N=1, E=2, S=4, O=8), como las fosas
# =================================================================================================
MUD_C = [C(c) for c in MUD]
MPH = {'N': 1.3, 'E': 3.3, 'S': 5.1, 'W': .2}
def mud_depth(pos, ph): return 2.4 + 1.0 * math.sin(2 * math.pi * pos / 16 + ph) + .5 * math.sin(4 * math.pi * pos / 16 + ph * 1.9)

def mud_surface(seed):
    cv = WrapCanvas(16, 16, True, True)
    n8, n4 = pnoise(seed, 16, 8), pnoise(seed + 1, 16, 4)
    for y in range(16):
        for x in range(16):
            v = .45 + n8(x, y) * .6 + n4(x, y) * .7
            cv.px[(x, y)] = MUD_C[min(2, max(0, dither(v, x, y)))]
    return cv

def bank_set(mask, corner=None):
    """Píxeles de orilla (tierra) del tile: por los lados que no son lodo y, en las esquinas de dentro, un cuarto
    de círculo. Las esquinas de fuera se redondean."""
    R = 5
    out = set()
    for y in range(16):
        for x in range(16):
            dN = y - mud_depth(x, MPH['N']) if mask & 1 else 99
            dE = (15 - x) - mud_depth(y, MPH['E']) if mask & 2 else 99
            dS = (15 - y) - mud_depth(x, MPH['S']) if mask & 4 else 99
            dW = x - mud_depth(y, MPH['W']) if mask & 8 else 99
            o = min(dN, dE, dS, dW) < 0
            for da, db in ((dN, dW), (dN, dE), (dS, dW), (dS, dE)):
                if 0 <= da < R and 0 <= db < R and math.hypot(R - da, R - db) > R: o = True
            if corner:
                cx = 16 if 'E' in corner else 0; cy = 0 if 'N' in corner else 16
                r = 3.2 + .6 * math.sin(x * 1.3 + y)
                if math.hypot(x + .5 - cx, y + .5 - cy) < r: o = True
            if o: out.add((x, y))
    return out

def shade_mud(cv, bank, floor_tile, t, only=None):
    """Orilla y lodo junto a ella: la tierra de arriba proyecta sombra (luz arriba a la izquierda) y la orilla de
    abajo y de la derecha coge luz; la tierra que toca el lodo, mojada y más oscura."""
    for (x, y) in bank:
        c = floor_tile.px[(x, y)]
        wet = any((x + dx, y + dy) not in bank and 0 <= x + dx < 16 and 0 <= y + dy < 16 for dx, dy in N4)
        near = any((x + dx, y + dy) not in bank and 0 <= x + dx < 16 and 0 <= y + dy < 16 for dx in (-2, 0, 2) for dy in (-2, 0, 2))
        if wet and (x, y + 1) not in bank and y + 1 < 16: c = t['floor'][3]              # labio de arriba, con luz
        elif wet: c = darker(t['floor'][0], .85)
        elif near: c = darker(c, .8)
        cv.px[(x, y)] = c
    for y in range(16):
        for x in range(16):
            if (x, y) in bank or (only is not None and (x, y) not in only): continue
            up, left = (x, y - 1) in bank, (x - 1, y) in bank
            down, right = (x, y + 1) in bank, (x + 1, y) in bank
            if up or left: cv.px[(x, y)] = MUD_C[0]
            elif (x, y - 2) in bank and (x + y) % 2 == 0: cv.px[(x, y)] = MUD_C[0]
            elif down: cv.px[(x, y)] = MUD_C[4] if x % 3 else MUD_C[3]
            elif right: cv.px[(x, y)] = MUD_C[3]

def mud_tiles(t, seed, floor_tile):
    surf = mud_surface(seed)
    tiles = []
    for m in range(16):
        cv = copy_canvas(surf, Canvas)
        shade_mud(cv, bank_set(m), floor_tile, t)
        tiles.append(cv)
    corners = []
    for name in ('NE', 'NO', 'SE', 'SO'):
        corner = name.replace('O', 'W')
        bank = bank_set(0, corner)
        full = copy_canvas(surf, Canvas); shade_mud(full, bank, floor_tile, t)
        cv = Canvas(16, 16)                           # solo la esquina (capa encima)
        near = {(x + dx, y + dy) for (x, y) in bank for dx in (-2, -1, 0, 1, 2) for dy in (-2, -1, 0, 1, 2)}
        for p in near:
            if p in full.px and full.px[p] != surf.px.get(p): cv.px[p] = full.px[p]
        for p in bank: cv.px[p] = full.px[p]
        corners.append(cv)
    return tiles, corners

def mud_bubbles():
    """6 cuadros: dos burbujas que suben, revientan y dejan una onda (capa encima, en algunas celdas)."""
    frames = []
    for f in range(6):
        cv = Canvas(16, 16)
        for (bx, by, off, big) in ((6, 9, 0, True), (11, 4, 3, False)):
            p = (f + off) % 6
            if p == 0: cv.put(bx, by, MUD_C[3])
            elif p == 1:
                cv.put(bx, by, MUD_C[4]); cv.put(bx + 1, by, MUD_C[3]); cv.put(bx, by + 1, MUD_C[2])
            elif p == 2 and big:
                for dx, dy, c in ((0, -1, 4), (1, -1, 3), (-1, 0, 3), (0, 0, 4), (1, 0, 3), (2, 0, 2), (0, 1, 2), (1, 1, 2)):
                    cv.put(bx + dx, by + dy, MUD_C[c])
                cv.put(bx, by - 1, C('#b89a6a'))
            elif p == 2 or p == 3:                    # revienta: anillo
                r = 1.5 if p == 2 else 2.5
                for a in range(12):
                    x, y = bx + .5 + math.cos(a * .52) * r, by + .5 + math.sin(a * .52) * r / SQ
                    cv.put(x, y, MUD_C[3])
            elif p == 4:
                for a in range(14):
                    x, y = bx + .5 + math.cos(a * .45) * 3.5, by + .5 + math.sin(a * .45) * 3.5 / SQ
                    if a % 2 == 0: cv.put(x, y, MUD_C[2])
        frames.append(cv)
    return frames

# =================================================================================================
# Arco y puertas de raíces
# =================================================================================================
def arch_shape(x, y):
    """Hueco del arco en los 32×32 de la cara: de x 6 a 26, techo redondeado."""
    if not 6 <= x < 26: return False
    cx = 15.5; top = 5 + 6 * ((x + .5 - cx) / 10) ** 2
    return y >= top

def doorway(t, seed, faces):
    cv = Canvas(32, 32)
    for i in range(2): paste(cv, faces[i % len(faces)], i * 16, 0)
    ft = floor_base(t, seed)
    for y in range(32):
        for x in range(32):
            if not arch_shape(x, y): continue
            if y >= 27: c = ft.px[(x % 16, y % 16)] if y >= 29 else darker(ft.px[(x % 16, y % 16)], .6)
            else: c = PASSAGE if y < 24 or (x + y) % 2 else darker(t['floor'][0], .7)
            cv.px[(x, y)] = c
    rp = t['root']
    ch = [C(c) for c in CHAR]
    pal = rp if not t['burnt'] else ch
    # dos raíces gruesas que enmarcan el hueco y se cruzan arriba
    root(cv, [(4, 32), (2, 18), (6, 4), (20, 3)], 5, 3, pal)
    root(cv, [(28, 32), (30, 18), (26, 4), (11, 3)], 5, 3, pal)
    root(cv, [(9, 4), (13, 9), (19, 9), (24, 5)], 2, 1.5, pal)          # raíz fina que cuelga
    rnd = random.Random(seed)
    for k in range(4):                                # raicillas que cuelgan dentro del hueco
        x = rnd.randrange(9, 23); L = rnd.randrange(3, 8); top = int(5 + 6 * ((x + .5 - 15.5) / 10) ** 2) + 1
        for j in range(L): cv.put(x + (j // 3) % 2, top + j, pal[2] if j % 3 else pal[1])
    if t['burnt']:
        for (x, y) in ((5, 20), (27, 14), (14, 4)): cv.put(x, y, C(EMBER[2])); cv.put(x, y + 1, C(EMBER[1]))
    else:
        for k in range(3): frondlet(cv, rnd.choice((3, 28)), rnd.uniform(4, 22), math.pi / 2 + rnd.uniform(-.6, .6), rnd.uniform(5, 8), t['leaf'], .4, 2.5, (0, 0, 32, 32), 0)
    return cv

def _weave(w, h, across, n, x0, x1, y0, y1, diag=2):
    """Raíces que cruzan el hueco entero, alternando el lado del que crecen, y unas cuantas en diagonal."""
    out = []
    for i in range(n):
        if across == 'x':                             # de un lado al otro (crecen desde los lados)
            y = y0 + (y1 - y0) * (i + .5) / n
            a, b = (x0 - 1, x1 + 1) if i % 2 == 0 else (x1 + 1, x0 - 1)
            wv = 2.5 if i % 2 else -2.5
            out.append(([(a, y + wv * .4), (a + (b - a) * .33, y - wv), (a + (b - a) * .66, y + wv), (b, y - wv * .4)], 3.6, 2.4))
        else:                                         # de un extremo al otro (crecen desde arriba y abajo)
            x = x0 + (x1 - x0) * (i + .5) / n
            a, b = (y0 - 1, y1 + 1) if i % 2 == 0 else (y1 + 1, y0 - 1)
            wv = 2.5 if i % 2 else -2.5
            out.append(([(x + wv * .4, a), (x - wv, a + (b - a) * .33), (x + wv, a + (b - a) * .66), (x - wv * .4, b)], 3.6, 2.4))
    for k in range(diag):                             # diagonales que traban el tejido
        if k % 2 == 0: out.append(([(x0, y1), (x0 + (x1 - x0) * .3, y0 + (y1 - y0) * .6), (x0 + (x1 - x0) * .6, y0 + (y1 - y0) * .5), (x1, y0)], 3, 2))
        else: out.append(([(x1, y1), (x0 + (x1 - x0) * .7, y0 + (y1 - y0) * .6), (x0 + (x1 - x0) * .4, y0 + (y1 - y0) * .5), (x0, y0)], 3, 2))
    return out

ROOTS_TOP = _weave(32, 32, 'x', 7, 6, 25, 5, 31) + [([(15, 32), (17, 24), (14, 16), (16, 6)], 3.5, 2)]
ROOTS_SIDE = _weave(16, 32, 'y', 4, 0, 15, 0, 31)
ROOTS_BOTTOM = _weave(32, 16, 'x', 4, 0, 31, 0, 15)
CLOSE_G = (0, .18, .38, .58, .78, 1.0, 1.0)
CLOSE_MS = [70, 60, 60, 60, 60, 110, 140]
OPEN_MS = [130, 90, 60, 60, 60, 60]

def root_door(t, w, h, roots, grow, seal=0, shake=0, inside=None):
    """Puerta de raíces. grow 0..1 (cada raíz empieza un poco después que la anterior); seal 0 nada, 1 nudo que
    brilla, 2 brillo blanco (al abrirse); shake mueve 1 px (tiemblan)."""
    cv = Canvas(w, h)
    pal = [C(c) for c in CHAR] if t['burnt'] else t['root']
    back = [darker(c, .7) for c in pal]
    if grow <= 0: return cv
    n = len(roots)
    for layer, plist in ((0, back), (1, pal)):
        for i, (ctrl, w0, w1) in enumerate(roots):
            if (i % 2 == 0) != (layer == 0) and grow < 1: pass
            g = max(0, min(1, (grow * 1.25 - i * .25 / n)))
            if layer == 0:                            # copia de cada raíz detrás, desplazada (trenzado espeso)
                if grow < .5: continue
                ctrl = [(x + 2, y + 1) for x, y in ctrl]; w0, w1 = w0 * .8, w1 * .8
            ctrl = [(x + shake, y) for x, y in ctrl]
            root(cv, ctrl, w0, w1, plist, g, inside)
    cv.outline(); cv.clean()
    if w == 32 and h == 32:                           # la copa que cuelga sobre la cara queda por delante
        for p in [p for p in cv.px if p[1] <= round(oh_depth(p[0])) + 1]: del cv.px[p]
    if inside:                                        # el contorno no sale del hueco
        for p in [p for p in cv.px if not (inside[0] <= p[0] < inside[2] and inside[1] <= p[1] < inside[3])]: del cv.px[p]
    if seal:
        sl = t['seal']; cx, cy = w // 2 + shake, h // 2 + (1 if h > w else 0)
        if h == 32 and w == 32: cy = 17
        for y in range(cy - 2, cy + 3):
            for x in range(cx - 2, cx + 3):
                d = abs(x - cx) + abs(y - cy)
                if d <= 2: cv.px[(x, y)] = (sl[3] if seal == 2 else sl[2]) if d == 0 else (sl[2] if seal == 2 else sl[1]) if d == 1 else sl[0]
        if seal == 2:
            for x, y in ((cx - 3, cy), (cx + 3, cy), (cx, cy - 3), (cx, cy + 3)): cv.put(x, y, SPARK[1])
            cv.put(cx, cy, SPARK[0])
        if t['burnt']: cv.put(cx + 1, cy - 1, C(FIRE[3]))
    return cv

def root_door_anims(t, w, h, roots, inside):
    close = [root_door(t, w, h, roots, g, seal=1 if i >= 5 else 0, shake=(1 if i == 5 else 0), inside=inside)
             for i, g in enumerate(CLOSE_G)]
    if True:                                          # golpe al cerrarse: hojas y tierra que saltan (cuadro 5)
        rnd = random.Random(w * h)
        bits = t['fleck'] if not t['burnt'] else [C('#6d6466'), C(EMBER[2]), C('#958a88')]
        for k in range(8):
            x, y = rnd.randrange(inside[0], inside[2]), rnd.randrange(inside[1], inside[3])
            if (x, y) not in close[5].px: close[5].put(x, y, bits[k % len(bits)])
    opn = [root_door(t, w, h, roots, 1, seal=1), root_door(t, w, h, roots, 1, seal=2, shake=1),
           root_door(t, w, h, roots, .7), root_door(t, w, h, roots, .45), root_door(t, w, h, roots, .2), Canvas(w, h)]
    for f in opn[2:5]:                                # al retirarse, las raíces se secan (más oscuras)
        for p, c in list(f.px.items()):
            if c != OUTL: f.px[p] = darker(c, .85)
    return close, opn

# =================================================================================================
# Maleza quemable: maraña de tallos secos en un hueco de la pared; al arder se carboniza y se deshace
# =================================================================================================
BR = [C(c) for c in BRUSH]

def dilate(s, r):
    return {(x + dx, y + dy) for (x, y) in s for dx in range(-r, r + 1) for dy in range(-r, r + 1) if abs(dx) + abs(dy) <= r}

def tiled(tile, w, h):
    cv = Canvas(w, h)
    for y in range(h):
        for x in range(w): cv.px[(x, y)] = tile.px[(x % 16, y % 16)]
    return cv

def brush_hole(w, h, side, seed):
    """Hueco de borde irregular que la maraña tapa. Por el lado del suelo llega hasta el borde (se sale a la sala);
    por los demás deja un margen con la textura de la pared de al lado."""
    n = noise(seed)
    hole = set()
    for y in range(h):
        for x in range(w):
            j = (n(x, y, 3) - .5) * 3.2
            if side == 'S': ok = abs(x + .5 - w / 2) < 11 + j * .7 and y >= 7 + 5 * ((x + .5 - w / 2) / 12) ** 2 + j
            elif side == 'E': ok = abs(y + .5 - h / 2) < h / 2 - 4.5 - ((w - 1 - x) / w) ** 2 * 5 + j * .8   # en U: más estrecho lejos del suelo
            else: ok = abs(x + .5 - w / 2) < w / 2 - 4.5 - (y / h) ** 2 * 5 + j * .8
            if ok: hole.add((x, y))
    return hole

def brush_stems(seed, w, h, floor_side, near):
    """Tallos como listas de puntos, solo dentro del hueco y un poco por fuera."""
    rnd = random.Random(seed)
    stems = []
    for k in range(int(len(near) / 22)):
        if floor_side == 'S':                         # vista de frente (cara de arriba): suben desde el pie
            sx = rnd.uniform(4, w - 4); ang = rnd.choice((-1, 1)) * rnd.uniform(.4, 1.2)
            L = rnd.uniform(10, h - 6); yb = h - rnd.uniform(0, 6)
            pts = [(sx + math.sin(ang) * st + ang * (st / L) ** 2 * 8, yb - st * math.cos(ang * .6)) for st in range(int(L))]
        else:                                         # vista desde arriba: maraña en todas direcciones
            sx, sy = rnd.choice(sorted(near)); ang = rnd.uniform(0, 6.28); L = rnd.uniform(6, 13)
            cur = rnd.uniform(-.15, .15)
            pts = [(sx + math.cos(ang + cur * st) * st, sy + math.sin(ang + cur * st) * st / 1.2) for st in range(int(L))]
        stems.append((k, [p for p in pts if (int(p[0]), int(p[1])) in near]))
    return stems

def brush_u(x, y, w, h, side):
    return {'S': (h - 1 - y) / h, 'E': (w - 1 - x) / w, 'N': y / h}[side]

def brush(t, base, side, seed, burn=-1.0, gone=False):
    """base: lo que hay alrededor (cara o borde de pared, del mismo tamaño). burn -1 quieta; 0..1 frente del fuego
    avanzando desde el lado del suelo; >1 deshecha (gone: solo el paso con ceniza)."""
    w, h = base.w, base.h
    cv = copy_canvas(base, Canvas)
    rnd = random.Random(seed)
    hole = brush_hole(w, h, side, seed); near = dilate(hole, 2)
    ft = floor_base(t, seed)
    for (x, y) in dilate(hole, 1) - hole:             # el hueco hace sombra en la pared
        if (x, y) in cv.px: cv.px[(x, y)] = darker(cv.px[(x, y)], .6)
    for (x, y) in hole:                               # paso oscuro detrás con el suelo asomando
        if side == 'S': c = PASSAGE if y < h - 6 else ft.px[(x % 16, y % 16)] if y >= h - 3 else darker(t['floor'][0], .7)
        else: c = darker(ft.px[(x % 16, y % 16)], .45 if (x + y) % 2 else .55)
        cv.px[(x, y)] = c
    collapse = max(0, burn - 1)
    if not gone:
        for k, pts in brush_stems(seed, w, h, side, near):
            if collapse > 0 and rnd.random() < collapse * 1.3: continue
            for st, (x, y) in enumerate(pts):
                if collapse > 0 and side == 'S': y += collapse * 10 * (1 - y / h) * 1.5   # se vienen abajo
                if not (0 <= x < w and 0 <= y < h) or (collapse > 0 and (int(x), int(y)) not in hole): continue
                u = brush_u(int(x), int(y), w, h, side)
                charred = burn >= 0 and u < burn * 1.15
                if charred:
                    c = C(CHAR[3]) if (st + k) % 5 == 0 else C(CHAR[2]) if k % 2 else C(CHAR[1])
                    if burn < 1.2 and (st * 7 + k) % 11 == 0: c = C(EMBER[2])
                else:
                    c = BR[3] if (st + k) % 5 == 0 else BR[2] if k % 2 else BR[1]
                cv.put(x, y, c)
                if st % 4 == 2 and not charred and (int(x) + 1, int(y) - 1) in near:
                    cv.put(x + 1, y - 1, BR[4] if st % 8 == 2 else BR[2])
        if burn < 1:
            inner = sorted(hole)
            for k in range(len(hole) // 22):          # hojitas secas
                x, y = rnd.choice(inner)
                if burn >= 0 and brush_u(x, y, w, h, side) < burn * 1.15: continue
                cv.put(x, y, BR[3]); cv.put(x + 1, y, BR[2]); cv.put(x, y + 1, BR[1])
        if 0 <= burn <= 1.3:                          # llamas en el frente del fuego
            fr = random.Random(seed + int(burn * 100))
            for k in range(len(hole) // 50):
                if side == 'S': y = int(h - 1 - min(burn, 1) * h * 1.1 + fr.uniform(-3, 4)); x = fr.randrange(1, w - 1)
                elif side == 'E': x = int(w - 1 - min(burn, 1) * w * 1.1 + fr.uniform(-2, 3)); y = fr.randrange(3, h)
                else: y = int(min(burn, 1) * h * 1.1 + fr.uniform(-2, 3)); x = fr.randrange(1, w - 1)
                if burn > 1: y = fr.randrange(h // 2, h)
                y = min(h - 1, y)
                if (x, y) in near: flame(cv, x, y, fr, fr.randrange(3, 7) if burn < 1 else 2)
    if burn >= 1 or gone:                             # ceniza en el suelo del paso y tocones en el borde
        ar = random.Random(seed + 9)
        inner = sorted(hole)
        for k in range(len(hole) // (8 if not gone else 12)):
            x, y = ar.choice(inner)
            if side == 'S': y = max(y, h - 1 - int(abs(ar.gauss(0, 2.5))))
            if (x, y) in hole: cv.put(x, y, C(ar.choice(('#4a4346', '#6d6466', '#958a88', CHAR[2]))))
        rim = sorted(p for p in dilate(hole, 1) - hole if p in cv.px)
        for k in range(len(rim) // 6):                # tocones quemados de los tallos en el filo
            x, y = ar.choice(rim); cv.put(x, y, C(CHAR[2])); cv.put(x, y + 1, C(CHAR[1]))
        if not gone:
            for k in range(3): cv.put(*ar.choice(inner), C(EMBER[2]))
    return cv

def soften_edges(cv, t, seed, side):
    """Las frondas de la pared de al lado tapan el filo del hueco (que no parezca recortado)."""
    rnd = random.Random(seed); box = (0, 0, cv.w, cv.h)
    hole = brush_hole(cv.w, cv.h, side, seed - 5)
    edge = sorted(p for p in dilate(hole, 1) - hole if 0 <= p[0] < cv.w and 0 <= p[1] < cv.h)
    if not edge: return cv
    cx, cy = cv.w / 2, cv.h / 2
    for k in range(7 if side == 'S' else 5):
        x, y = rnd.choice(edge)
        ang = math.atan2(cy - y, cx - x) + rnd.uniform(-.5, .5)      # hacia dentro del hueco
        if side == 'S' and y < 12: ang = math.pi / 2 + rnd.uniform(-.7, .7)
        if t['burnt']: twig(cv, x, y, ang, rnd.uniform(4, 7), [C(c) for c in CHAR], box, True, rnd)
        else: frondlet(cv, x, y, ang, rnd.uniform(5, 8), t['leaf'], .35, 2.5, box, rnd.choice((0, 1)))
    return cv

BURN_T = (0, .18, .4, .65, .9, 1.15, 1.5, 1.85)
BURN_MS = [90, 90, 90, 90, 100, 110, 120, 140]

def brush_set(t, seed, faces, tiles, cap):
    face32 = Canvas(32, 32); paste(face32, faces[0], 0, 0); paste(face32, faces[3], 16, 0)
    bases = {'Top': (face32, 'S'), 'Side': (tiled(tiles['Wall_EdgeLeft'], 16, 32), 'E'),
             'Bottom': (tiled(tiles['Wall_EdgeBottom'], 32, 16), 'N')}
    out = {}
    for name, (base, side) in bases.items():
        s = seed + {'Top': 1, 'Side': 2, 'Bottom': 3}[name]
        def make(**kw):
            cv = soften_edges(brush(t, base, side, s, **kw), t, s + 5, side)
            if side == 'S': overhang(cv, t, cap)      # la copa cuelga también sobre la maleza
            return cv
        out[name] = (make(), [make(burn=b) for b in BURN_T], make(burn=2, gone=True))
    return out

# =================================================================================================
# Tramo completo
# =================================================================================================
def build(key):
    t = tram(key); s = SEED[key]
    out = {}
    base = floor_base(t, s)
    out['Floor_0'] = base
    for k in range(1, 6): out[f'Floor_{k}'] = floor_detail(copy_canvas(base), t, k, s)
    for k in range(2): out[f'Floor_ShadowTop_{k}'] = floor_shadow(base, t, k)
    cap = canopy(t, s + 10)
    out['Wall_Cap'] = cap
    for k in range(1, 4): out[f'Wall_Cap_{k}'] = canopy(t, s + 10, k)
    faces = [overhang(face_pair(t, s + 20, v), t, cap) for v in range(4)]
    for v, f in enumerate(faces):
        out[f'Wall_FaceUpper_{v}'] = crop(f, 0, 0, 16, 16); out[f'Wall_FaceLower_{v}'] = crop(f, 0, 16, 16, 16)
    out['Wall_EdgeLeft'] = open_edges(cap, t, base, ('E',))
    out['Wall_EdgeRight'] = open_edges(cap, t, base, ('W',))
    out['Wall_EdgeBottom'] = open_edges(cap, t, base, ('N',))
    out['Wall_CornerBL'] = open_edges(cap, t, base, (), ('NE',))
    out['Wall_CornerBR'] = open_edges(cap, t, base, (), ('NW',))
    out['Wall_OuterTopLeft'] = open_edges(cap, t, base, ('N', 'W'))
    out['Wall_OuterTopRight'] = open_edges(cap, t, base, ('N', 'E'))
    for side, name in (('E', 'Wall_EdgeLeft'), ('W', 'Wall_EdgeRight')):
        out[name + '_FaceUpper'], out[name + '_FaceLower'] = edge_beside_face(t, cap, faces[0], side)
    fl = open_edges(faces[0], t, base, ('W',)); fr = open_edges(faces[0], t, base, ('E',))
    out['Wall_FaceUpper_EndLeft'] = crop(fl, 0, 0, 16, 16); out['Wall_FaceLower_EndLeft'] = crop(fl, 0, 16, 16, 16)
    out['Wall_FaceUpper_EndRight'] = crop(fr, 0, 0, 16, 16); out['Wall_FaceLower_EndRight'] = crop(fr, 0, 16, 16, 16)
    spills = {f'Spill_{side}_{k}': spill(t, side, k, s) for side, n in (('Top', 4), ('Left', 4), ('Bottom', 3)) for k in range(n)}
    mud, mud_c = mud_tiles(t, s + 40, base)
    bub = mud_bubbles()
    arch = overhang(doorway(t, s + 50, [faces[0], faces[3]]), t, cap)
    doors = {'Top': root_door_anims(t, 32, 32, ROOTS_TOP, (6, 4, 26, 32)),
             'Side': root_door_anims(t, 16, 32, ROOTS_SIDE, (0, 0, 16, 32)),
             'Bottom': root_door_anims(t, 32, 16, ROOTS_BOTTOM, (0, 0, 32, 16))}
    br = brush_set(t, s + 60, faces, out, cap)
    return dict(t=t, tiles=out, faces=faces, spills=spills, mud=mud, mud_c=mud_c, bub=bub, arch=arch, doors=doors, brush=br)

# =================================================================================================
# Sala de muestra (mismas reglas que WallRules.cs y RoomPainter.cs)
# =================================================================================================
SAMPLE = [  # '#' pared, '.' suelo, '~' lodo profundo, 'A' arco (2×2 en la cara), 'M' maleza arriba (2×2), 'L' puerta izquierda, 'B' maleza abajo
    "##########################",
    "##########################",
    "####A#############M#######",
    "##########################",
    "##..............##......##",
    "##..............##......##",
    "##.....~~~~.....##......##",
    "L......~~~~~~...........##",
    "L.....~~~~~~~~..........##",
    "##.....~~~~~~.....##....##",
    "##......~~~......####...##",
    "##...............####~~.##",
    "##.......~~......####~~~##",
    "##.......~~...........~~##",
    "############BB############",
    "##########################",
]

def sample_room(b, scale=3):
    t = b['t']; T = b['tiles']
    rows = [r.ljust(26, '#') for r in SAMPLE]
    H, W = len(rows), len(rows[0])
    def kind(x, y):
        if not (0 <= x < W and 0 <= y < H): return '#'
        return rows[y][x]
    arch_cells = {(x + dx, y + dy) for y, r in enumerate(rows) for x, c in enumerate(r) if c == 'A' for dx in (0, 1) for dy in (0, 1)}
    brush_cells = {(x + dx, y + dy) for y, r in enumerate(rows) for x, c in enumerate(r) if c == 'M' for dx in (0, 1) for dy in (0, 1)}
    def is_open(x, y):
        k = kind(x, y)
        return k in '.~LB'
    def wall(x, y): return not is_open(x, y)
    def is_face(x, y): return wall(x, y) and (is_open(x, y + 1) or is_open(x, y + 2))
    im = Image.new('RGBA', (W * 16, H * 16), BG)
    def h(x, y): return (x * 73856093 ^ y * 19349663) & 0x7fffffff
    mud = lambda x, y: kind(x, y) == '~'
    for y in range(H):
        for x in range(W):
            if (x, y) in arch_cells or (x, y) in brush_cells: continue
            k = kind(x, y)
            if k in '.LB':
                shadow = wall(x, y - 1)
                name = (f'Floor_ShadowTop_{h(x, y) % 2}' if shadow else f'Floor_{1 + (h(x, y) // 3) % 5}' if h(x, y) % 3 == 0 else 'Floor_0')
                im.alpha_composite(T[name].img(), (x * 16, y * 16))
            elif k == '~':
                m = (0 if mud(x, y - 1) else 1) | (0 if mud(x + 1, y) else 2) | (0 if mud(x, y + 1) else 4) | (0 if mud(x - 1, y) else 8)
                im.alpha_composite(b['mud'][m].img(), (x * 16, y * 16))
                for i, (dx, dy) in enumerate(((1, -1), (-1, -1), (1, 1), (-1, 1))):
                    if mud(x + dx, y) and mud(x, y + dy) and not mud(x + dx, y + dy):
                        im.alpha_composite(b['mud_c'][i].img(), (x * 16, y * 16)); break
                if h(x, y) % 9 == 0: im.alpha_composite(b['bub'][h(x, y) // 9 % 6].img(), (x * 16, y * 16))
            else:
                up, left, right = is_open(x, y - 1), is_open(x - 1, y), is_open(x + 1, y)
                fl, fu = is_open(x, y + 1), (not is_open(x, y + 1)) and is_open(x, y + 2)
                if fl or fu:
                    if up or (left and right): name = None
                    else:
                        v = h(x, y + 1 if fl else y) % 4             # pares: la de abajo usa el hash de la de arriba
                        hv = h(x, y - 1 if fl else y) % 4
                        part = 'Lower' if fl else 'Upper'
                        name = f'Wall_Face{part}_EndLeft' if left else f'Wall_Face{part}_EndRight' if right else f'Wall_Face{part}_{hv}'
                elif left and right: name = None
                elif up and left: name = 'Wall_OuterTopLeft'
                elif up and right: name = 'Wall_OuterTopRight'
                elif up: name = 'Wall_EdgeBottom'
                elif left: name = 'Wall_EdgeRight'
                elif right: name = 'Wall_EdgeLeft'
                elif is_face(x - 1, y): name = 'Wall_EdgeRight_Face' + ('Lower' if is_open(x - 1, y + 1) else 'Upper')
                elif is_face(x + 1, y): name = 'Wall_EdgeLeft_Face' + ('Lower' if is_open(x + 1, y + 1) else 'Upper')
                elif is_open(x + 1, y - 1): name = 'Wall_CornerBL'
                elif is_open(x - 1, y - 1): name = 'Wall_CornerBR'
                else: name = 'Wall_Cap' if h(x, y) % 3 else f'Wall_Cap_{1 + h(x, y) // 3 % 3}'
                if name: im.alpha_composite(T[name].img(), (x * 16, y * 16))
                else: im.paste(C('#ff00ff'), (x * 16, y * 16, x * 16 + 16, y * 16 + 16))
    for y, r in enumerate(rows):                      # solapas de espesura sobre el suelo
        for x, c in enumerate(r):
            if c not in '.LB' or (x, y) in arch_cells: continue
            hv = h(x * 3, y * 5)
            if wall(x, y - 1) and hv % 2 == 0: im.alpha_composite(b['spills'][f'Spill_Top_{hv // 2 % 4}'].img(), (x * 16, y * 16))
            if wall(x - 1, y) and hv % 3 == 0: im.alpha_composite(b['spills'][f'Spill_Left_{hv // 3 % 4}'].img(), (x * 16, y * 16))
            if wall(x + 1, y) and hv % 3 == 1: im.alpha_composite(b['spills'][f'Spill_Left_{hv // 3 % 4}'].img().transpose(Image.FLIP_LEFT_RIGHT), (x * 16, y * 16))
            if wall(x, y + 1) and hv % 2 == 1: im.alpha_composite(b['spills'][f'Spill_Bottom_{hv // 2 % 3}'].img(), (x * 16, y * 16))
    for y, r in enumerate(rows):
        for x, c in enumerate(r):
            if c == 'A':
                im.alpha_composite(b['arch'].img(), (x * 16, y * 16))
                im.alpha_composite(b['doors']['Top'][0][-1].img(), (x * 16, y * 16))      # cerrada
            elif c == 'M': im.alpha_composite(b['brush']['Top'][0].img(), (x * 16, y * 16))
            elif c == 'L' and kind(x, y - 1) != 'L': im.alpha_composite(b['doors']['Side'][0][-1].img(), (x * 16, y * 16))
            elif c == 'B' and kind(x - 1, y) != 'B': im.alpha_composite(b['brush']['Bottom'][0].img(), (x * 16, y * 16))
    return im.resize((im.width * scale, im.height * scale), Image.NEAREST), im

# =================================================================================================
# Salida
# =================================================================================================
TILE_ORDER = (['Floor_0', 'Floor_1', 'Floor_2', 'Floor_3', 'Floor_4', 'Floor_5', 'Floor_ShadowTop_0', 'Floor_ShadowTop_1',
               'Wall_Cap', 'Wall_Cap_1', 'Wall_Cap_2', 'Wall_Cap_3']
              + [f'Wall_Face{p}_{v}' for v in range(4) for p in ('Upper', 'Lower')]
              + ['Wall_EdgeLeft', 'Wall_EdgeRight', 'Wall_EdgeBottom', 'Wall_CornerBL', 'Wall_CornerBR',
                 'Wall_OuterTopLeft', 'Wall_OuterTopRight', 'Wall_FaceUpper_EndLeft', 'Wall_FaceUpper_EndRight',
                 'Wall_FaceLower_EndLeft', 'Wall_FaceLower_EndRight', 'Wall_EdgeLeft_FaceUpper', 'Wall_EdgeLeft_FaceLower',
                 'Wall_EdgeRight_FaceUpper', 'Wall_EdgeRight_FaceLower'])

def grid(frames, cols):
    rows = (len(frames) + cols - 1) // cols
    w, h = frames[0].w, frames[0].h
    im = Image.new('RGBA', (cols * w, rows * h), (0, 0, 0, 0))
    for i, f in enumerate(frames): im.alpha_composite(f.img(), ((i % cols) * w, (i // cols) * h))
    return im

def write(key, b):
    d = os.path.join(HERE, key)
    for sub in ('tiles', 'mud', 'deco', 'gif'): os.makedirs(os.path.join(d, sub), exist_ok=True)
    for name in TILE_ORDER: b['tiles'][name].img().save(os.path.join(d, 'tiles', name + '.png'))
    grid([b['tiles'][n] for n in TILE_ORDER], 8).save(os.path.join(d, f'Tileset_{key}.png'))
    for name, cv in b['spills'].items(): cv.img().save(os.path.join(d, 'deco', name + '.png'))
    for m, cv in enumerate(b['mud']): cv.img().save(os.path.join(d, 'mud', f'Mud_{m}.png'))
    for name, cv in zip(('NE', 'NO', 'SE', 'SO'), b['mud_c']): cv.img().save(os.path.join(d, 'mud', f'Mud_Corner_{name}.png'))
    save_sheet(os.path.join(d, 'mud', 'Mud_Bubbles.png'), b['bub'])
    grid(b['mud'], 4).save(os.path.join(d, 'mud', 'Mud_Tileset.png'))
    b['arch'].img().save(os.path.join(d, 'Doorway_Top.png'))
    for side, (close, opn) in b['doors'].items():
        save_sheet(os.path.join(d, f'Door_{side}_Close.png'), close)
        save_sheet(os.path.join(d, f'Door_{side}_Open.png'), opn)
        gif(os.path.join(d, 'gif', f'Door_{side}.gif'), close + [close[-1]] * 3 + opn + [opn[-1]] * 3,
            CLOSE_MS + [300] * 3 + OPEN_MS + [300] * 3, t_bg(b))
    for side, (idle, burn, gone) in b['brush'].items():
        idle.img().save(os.path.join(d, f'Brush_{side}_Idle.png'))
        save_sheet(os.path.join(d, f'Brush_{side}_Burn.png'), burn)
        gone.img().save(os.path.join(d, f'Brush_{side}_Gone.png'))
        gif(os.path.join(d, 'gif', f'Brush_{side}.gif'), [idle] * 3 + burn + [gone] * 4, [200] * 3 + BURN_MS + [300] * 4, t_bg(b))
    gif(os.path.join(d, 'gif', 'Mud_Bubbles.gif'), [paste_on(b['mud'][0], f) for f in b['bub']], 140, t_bg(b), 6)
    big, small = sample_room(b)
    big.save(os.path.join(d, 'Sala_muestra_x3.png')); small.save(os.path.join(d, 'Sala_muestra.png'))
    return small

def t_bg(b): return b['t']['floor'][1]

def paste_on(base, over):
    cv = copy_canvas(base, Canvas); paste(cv, over, 0, 0); return cv

def piezas_preview(builds, path):
    """Vista previa de piezas: tiles, lodo, puertas y maleza de los tres tramos, a ×3."""
    rows = []
    for key, b in builds.items():
        anims = {f'{key} tiles': [b['tiles'][n] for n in TILE_ORDER[:12]],
                 f'{key} caras+bordes': [b['tiles'][n] for n in TILE_ORDER[12:]],
                 f'{key} lodo': b['mud'][:8], f'{key} lodo 2': b['mud'][8:] + b['mud_c'],
                 f'{key} solapas': list(b['spills'].values()),
                 f'{key} puerta arriba': [b['arch']] + b['doors']['Top'][0] + b['doors']['Top'][1],
                 f'{key} puertas lado/abajo': b['doors']['Side'][0][-1:] + b['doors']['Bottom'][0][-1:] + b['doors']['Side'][1][:2] + b['doors']['Bottom'][1][:2],
                 f'{key} maleza arriba': [b['brush']['Top'][0]] + b['brush']['Top'][1] + [b['brush']['Top'][2]],
                 f'{key} maleza lado/abajo': [b['brush']['Side'][0], b['brush']['Side'][1][3], b['brush']['Side'][2], b['brush']['Bottom'][0], b['brush']['Bottom'][1][3], b['brush']['Bottom'][2]]}
        rows.append(anims)
    merged = {}
    for a in rows: merged.update(a)
    preview(path, merged, BG, scale=3, cols=13)

if __name__ == '__main__':
    builds = {k: build(k) for k in ('Swamp', 'Smoke', 'Burnt')}
    smalls = [write(k, b) for k, b in builds.items()]
    W = max(s.width for s in smalls); H = sum(s.height for s in smalls) + 8 * (len(smalls) - 1)
    im = Image.new('RGBA', (W, H), BG); y = 0
    for s in smalls: im.alpha_composite(s, (0, y)); y += s.height + 8
    im.resize((W * 2, H * 2), Image.NEAREST).save(os.path.join(HERE, 'vista_previa_salas.png'))
    piezas_preview(builds, os.path.join(HERE, 'vista_previa_piezas.png'))
    print('Hecho:', ', '.join(builds))
