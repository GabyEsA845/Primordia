"""Sala de paso del piso 3 al 4 de Primordia: de la tierra quemada a la arena roja del Pérmico.

- Tileset `Permian` (arena roja pérmica): suelo de arena rojo óxido con ondas del viento, tapa = meseta de roca vista
  desde arriba (grietas, arena en los huecos, matas secas), cara = acantilado de arenisca con estratos y arena al pie.
  Mismas piezas y nombres que los tramos del piso 3 (`../floor3-environment/`), solo tiles y solapas.
- Tileset `BurntSand` (franja intermedia): paredes de tierra quemada y la arena que se cuela por el suelo.
- `Transition_Pit_Sand`: sumidero de arena (7×5 tiles, 8 cuadros) con la arena que cae por la pared y gira hacia
  el centro, donde late una luz cálida. `Transition_Dust_Sand`: grano de arena que sube y se deshace en polvo.
- Sala de muestra con las tres franjas (quemada → intermedia → pérmica) y el sumidero.

Uso: python generar_salida_piso3.py
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', 'floor3-environment'))
from base_piso3 import *
import generar_tilesets_piso3 as gt
from PIL import Image

# =================================================================================================
# Paletas: rojo óxido apagado (regla del piso: el escenario oscuro, lo que importa para jugar, claro)
# =================================================================================================
PERMIAN = dict(
    key='Permian', nombre='Arena roja pérmica',
    floor=['#2e1a16', '#35201a', '#3d241c', '#462a20'],
    fleck=['#56321f', '#6c402b', '#865236'],
    cap=['#1f1210', '#261613', '#2e1b16', '#37211a'],
    leaf=['#1a0e0c', '#4a2a1e', '#623826', '#7c4a32'],      # filo de la meseta: roca con luz
)
ROCK = [C(c) for c in ('#1a0e0c', '#261411', '#341b15', '#45231a', '#5a2e21', '#72402b')]   # arenisca
PALE = [C(c) for c in ('#3e2c22', '#56402e', '#6e543c', '#86684a')]                        # estrato claro
DRY = [C(c) for c in ('#2e2414', '#4a3a1e', '#6e5628', '#94783a')]                         # mata seca
SANDLIT = [C(c) for c in ('#a06a40', '#c08a58', '#e0b080')]
SEED = {'Permian': 420, 'BurntSand': 460}

def pal(d):
    t = {k: ([C(c) for c in v] if isinstance(v, list) else v) for k, v in d.items()}
    t.setdefault('burnt', False); t.setdefault('smoke', False)
    return t

def cl(i, n=3): return max(0, min(n, i))
def hsh(x, y, s=0): return ((x * 73856093) ^ (y * 19349663) ^ (s * 83492791)) & 0x7fffffff

# =================================================================================================
# Suelo de arena
# =================================================================================================
def sand_floor(t, seed):
    cv = WrapCanvas(16, 16, True, True)
    n8, n4 = pnoise(seed, 16, 8), pnoise(seed + 3, 16, 4)
    for y in range(16):
        for x in range(16):
            v = 1.25 + n8(x, y) * .7 + n4(x, y) * .5
            cv.px[(x, y)] = t['floor'][cl(dither(v, x, y))]
    for k in range(2):                                # ondas del viento: crestas a tramos (cada 8 filas)
        for x in range(16):
            y = (k * 8 + 3 + round(1.5 * math.sin(2 * math.pi * x / 16 + k * 2.1) + .6 * math.sin(4 * math.pi * x / 16 + 1))) % 16
            if (x + 3 * k) % 7 >= 5: continue
            cv.px[(x, y)] = t['floor'][3]
            if (x + k) % 2: cv.px[(x, (y + 1) % 16)] = t['floor'][0]
    return cv

def pebble(cv, x, y, big=False):
    pts = ((0, 0, 4), (1, 0, 3), (0, 1, 3), (1, 1, 2)) if not big else \
          ((0, 0, 4), (1, 0, 4), (2, 0, 3), (0, 1, 3), (1, 1, 3), (2, 1, 2), (1, -1, 5))
    for dx, dy, i in pts: cv.put(x + dx, y + dy, ROCK[i])
    for dx in range(3 if big else 2): cv.put(x + dx, y + 2, C(PERMIAN['floor'][0]))     # sombra

def dry_tuft(cv, x, y, rnd, n=4):
    for j in range(n):
        ang = -math.pi / 2 + (j - (n - 1) / 2) * .45 + rnd.uniform(-.15, .15)
        L = rnd.randrange(2, 5)
        for st in range(L):
            cv.put(x + math.cos(ang) * st, y + math.sin(ang) * st, DRY[3] if st == L - 1 else DRY[2] if st else DRY[1])
    cv.put(x, y + 1, C(PERMIAN['floor'][0])); cv.put(x + 1, y + 1, C(PERMIAN['floor'][0]))

def sand_detail(cv, t, k, seed):
    rnd = random.Random(seed * 7 + k)
    kinds = [('guijarro', 2), ('grieta', 1), ('mata', 1), ('montoncito', 1), ('guijarro', 1), ('mata', 2)]
    kind, n = kinds[k % len(kinds)]
    for _ in range(n):
        x, y = rnd.randrange(3, 11), rnd.randrange(4, 11)
        if kind == 'guijarro': pebble(cv, x, y, rnd.random() < .5)
        elif kind == 'mata': dry_tuft(cv, x, y + 2, rnd)
        elif kind == 'grieta':                        # costra seca agrietada
            xx, yy = 3, rnd.randrange(5, 11)
            for j in range(10):
                cv.put(xx + j, yy, t['floor'][0]); cv.put(xx + j, yy + 1, t['fleck'][0] if j % 3 else t['floor'][3])
                if j == 4: cv.put(xx + j, yy - 1, t['floor'][0]); cv.put(xx + j, yy - 2, t['floor'][0])
                yy += rnd.choice((-1, 0, 0, 1)); yy = max(3, min(11, yy))
        elif kind == 'montoncito':                    # arena más clara amontonada
            cx, cy = rnd.uniform(6, 10), rnd.uniform(6, 10)
            for yy in range(2, 14):
                for xx in range(2, 14):
                    d = ((xx + .5 - cx) / 4.2) ** 2 + ((yy + .5 - cy) / 2.6) ** 2
                    if d < 1:
                        lit = (xx - cx) + (yy - cy) * 1.5 < 0
                        cv.put(xx, yy, t['fleck'][1] if lit and d < .5 else t['fleck'][0] if lit or d < .4 else t['floor'][3])
            cv.put(cx - 1, cy - 1, SANDLIT[0])
    return cv

# =================================================================================================
# Tapa: meseta de roca vista desde arriba
# =================================================================================================
def mesa_cap(t, seed, extra=0):
    cv = WrapCanvas(16, 16, True, True)
    n4, n8 = pnoise(seed, 16, 4), pnoise(seed + 1, 16, 8)
    cap = t['cap']
    val = {(x, y): n8(x, y) * .9 + n4(x, y) * .6 for y in range(16) for x in range(16)}
    for (x, y), v in val.items():
        c = cap[cl(dither(v * 2.4, x, y))]
        g = v - val[((x + 1) % 16, (y + 1) % 16)]     # relieve: la luz llega de arriba a la izquierda
        if g > .13 and (x + y) % 2: c = cap[3]
        elif g < -.13 and (x + y) % 2 == 0: c = cap[0]
        if n8(x, y) < .18 and (x + y) % 2: c = t['floor'][1]           # arena en los huecos
        cv.px[(x, y)] = c
    rnd = random.Random(seed)
    for k in range(0):                                # (sin grieta en la base: se repetía en cada tile)
        x, y = rnd.uniform(0, 16), rnd.uniform(0, 16); a = rnd.uniform(0, 6.28)
        for st in range(rnd.randrange(5, 8)):
            cv.put(x, y, ROCK[0]); cv.put(x + 1, y + 1, cap[3])
            a += rnd.uniform(-.6, .6); x += math.cos(a); y += math.sin(a)
    if extra:
        r2 = random.Random(seed * 13 + extra)
        cv.clip = (2, 2, 14, 14)
        cx, cy = r2.randrange(5, 10), r2.randrange(6, 11)
        if extra == 1: dry_tuft(cv, cx, cy, r2, 5)
        elif extra == 2: pebble(cv, cx, cy, True); pebble(cv, cx + 4, cy + 2)
        else:
            for st in range(9): cv.put(cx - 3 + st, cy + round(math.sin(st)), ROCK[0]); cv.put(cx - 3 + st, cy + 1 + round(math.sin(st)), cap[3])
        cv.clip = None
    return cv

# =================================================================================================
# Cara: acantilado de arenisca con estratos (16×32, en pares)
# =================================================================================================
BANDS = [  # (alto, rampa, tono): de arriba abajo
    (4, ROCK, 4), (3, PALE, 1), (5, ROCK, 3), (3, ROCK, 4), (4, PALE, 0), (5, ROCK, 3), (4, ROCK, 2), (4, ROCK, 2)]

def cliff_face(t, seed, var):
    cv = WrapCanvas(16, 32, True, False)
    rv = random.Random(seed * 31 + var)
    tops, y = [], 0
    for i, (hgt, r, k) in enumerate(BANDS):
        tops.append(y); y += hgt
    def band_at(x, y):
        j = 0
        for i, y0 in enumerate(tops):
            if y >= y0 + round(.8 * math.sin(2 * math.pi * x / 16 + i * 1.7)) * (i > 0): j = i
        return j
    for y in range(32):
        for x in range(16):
            j = band_at(x, y)
            hgt, r, k = BANDS[j]
            top = y == 0 or band_at(x, y - 1) != j
            bot = y == 31 or band_at(x, y + 1) != j
            i = k + (1 if top and r is PALE else -1 if bot else 0) - (1 if y > 22 else 0)
            if hsh(x, y, seed) % 13 == 0: i -= 1
            cv.px[(x, y)] = r[cl(i, len(r) - 1)]
    cv.clip = (2, 0, 14, 32)
    if var == 1:                                      # grieta vertical
        x = rv.randrange(6, 10)
        for y in range(3, 27):
            cv.put(x, y, ROCK[1]); cv.put(x - 1, y, ROCK[4] if y % 3 else ROCK[3])
            if rv.random() < .3: x += rv.choice((-1, 1)); x = max(4, min(11, x))
    elif var == 2:                                    # bloque caído al pie
        fill(cv, lambda px, py: ((px - 8) / 4.5) ** 2 + ((py - 26) / 3.2) ** 2 <= 1, 8, 26, 4.5, 3.2, ROCK[1:])
        for dx in range(-4, 5): cv.put(8 + dx, 29, ROCK[0])
    elif var == 3:                                    # chorrito de arena desde una repisa
        x = rv.randrange(5, 10)
        for dx in range(-1, 3): cv.put(x + dx, 9, ROCK[0])
        for y in range(10, 27):
            if y % 4 != 3: cv.put(x + (y % 5 == 0), y, t['fleck'][1] if y % 3 else t['fleck'][0])
    else:                                             # agujeritos de la roca
        for k in range(4):
            x, y = rv.randrange(3, 13), rv.randrange(3, 24)
            cv.put(x, y, ROCK[0]); cv.put(x + 1, y, ROCK[1]); cv.put(x, y + 1, ROCK[5])
    cv.clip = None
    for x in range(16):                               # arena amontonada al pie (compartida)
        h = 3 + round(1.2 * math.sin(2 * math.pi * x / 16 + 1.1) + .6 * math.sin(4 * math.pi * x / 16))
        h += 3 if var == 3 and 4 <= x <= 11 else 0
        for d in range(h):
            y = 31 - d
            cv.px[(x, y)] = t['fleck'][0] if d == h - 1 else t['floor'][2] if d > 1 else t['floor'][0]
    return cv

def rim_over(cv, t, cap):
    """La meseta asoma por arriba de la cara: textura de la tapa hasta un borde que ondula y filo con luz."""
    lf = t['leaf']
    for x in range(cv.w):
        d = max(1, round(1.4 + 1.8 * abs(math.sin(2 * math.pi * x / 16 + .9)) ** .8 + .6 * math.sin(2 * math.pi * x / 16 + 2.4)))
        for y in range(d): cv.px[(x, y)] = cap.px[(x % 16, y % 16)]
        cv.px[(x, d)] = lf[3] if x % 3 else lf[2]
        if (x, d + 1) in cv.px: cv.px[(x, d + 1)] = darker(cv.px[(x, d + 1)], .7)
    return cv

# =================================================================================================
# Solapas: arena amontonada contra la pared (capa encima del suelo)
# =================================================================================================
def sand_spill(t, side, k, seed):
    cv = Canvas(16, 16)
    rnd = random.Random(seed * 17 + k * 5 + {'Top': 0, 'Left': 100, 'Bottom': 200}[side])
    u0, u1 = rnd.uniform(.5, 3), rnd.uniform(12.5, 15.5)
    A = rnd.uniform(2, 3.5) if side != 'Bottom' else rnd.uniform(1.5, 2.5)
    for a in range(16):           # a: a lo largo de la pared; b: distancia a la pared
        if not (u0 < a + .5 < u1): continue
        dep = A * math.sin(math.pi * (a + .5 - u0) / (u1 - u0)) ** .3 * (1 + .25 * math.sin(a * .9 + k))
        for b in range(int(dep) + 1):
            if b > dep: continue
            x, y = {'Top': (a, b), 'Left': (b, a), 'Bottom': (a, 15 - b)}[side]
            edge = b >= dep - 1
            lit = (a + .5 - (u0 + u1) / 2) < 0 if side != 'Left' else b < dep * .6
            if edge and (a + b) % 2: continue
            c = t['floor'][3] if edge else t['fleck'][1] if lit and b < dep * .4 and (a + b) % 2 else t['fleck'][0]
            cv.put(x, y, c)
    if k % 2:                                         # piedrecita caída
        a = round((u0 + u1) / 2 + rnd.uniform(-2, 2)); b = 2
        x, y = {'Top': (a, b), 'Left': (b, a), 'Bottom': (a, 12)}[side]
        pebble(cv, x, y)
    return cv

# =================================================================================================
# Tramos completos
# =================================================================================================
def tiles_from(t, base, floors, cap_fn, faces, extra_spills):
    out = {'Floor_0': base}
    for k, f in enumerate(floors, 1): out[f'Floor_{k}'] = f
    for k in range(2): out[f'Floor_ShadowTop_{k}'] = gt.floor_shadow(base, t, k)
    cap = cap_fn(0); out['Wall_Cap'] = cap
    for k in range(1, 4): out[f'Wall_Cap_{k}'] = cap_fn(k)
    for v, f in enumerate(faces):
        out[f'Wall_FaceUpper_{v}'] = crop(f, 0, 0, 16, 16); out[f'Wall_FaceLower_{v}'] = crop(f, 0, 16, 16, 16)
    for name, sides, diags in (('Wall_EdgeLeft', ('E',), ()), ('Wall_EdgeRight', ('W',), ()), ('Wall_EdgeBottom', ('N',), ()),
                               ('Wall_CornerBL', (), ('NE',)), ('Wall_CornerBR', (), ('NW',)),
                               ('Wall_OuterTopLeft', ('N', 'W'), ()), ('Wall_OuterTopRight', ('N', 'E'), ())):
        out[name] = gt.open_edges(cap, t, base, sides, diags)
    for side, name in (('E', 'Wall_EdgeLeft'), ('W', 'Wall_EdgeRight')):
        out[name + '_FaceUpper'], out[name + '_FaceLower'] = gt.edge_beside_face(t, cap, faces[0], side)
    fl = gt.open_edges(faces[0], t, base, ('W',)); fr = gt.open_edges(faces[0], t, base, ('E',))
    out['Wall_FaceUpper_EndLeft'] = crop(fl, 0, 0, 16, 16); out['Wall_FaceLower_EndLeft'] = crop(fl, 0, 16, 16, 16)
    out['Wall_FaceUpper_EndRight'] = crop(fr, 0, 0, 16, 16); out['Wall_FaceLower_EndRight'] = crop(fr, 0, 16, 16, 16)
    return dict(t=t, tiles=out, spills=extra_spills)

def build_permian():
    t = pal(PERMIAN); s = SEED['Permian']
    base = sand_floor(t, s)
    floors = [sand_detail(copy_canvas(base), t, k, s) for k in range(1, 6)]
    cap0 = mesa_cap(t, s + 10)
    faces = [rim_over(cliff_face(t, s + 20, v), t, cap0) for v in range(4)]
    spills = {f'Spill_{side}_{k}': sand_spill(t, side, k, s) for side, n in (('Top', 4), ('Left', 4), ('Bottom', 3)) for k in range(n)}
    return tiles_from(t, base, floors, lambda k: mesa_cap(t, s + 10, k), faces, spills)

def sand_patch(cv, sand, ts, seed, n=1, r=(3.5, 5.5)):
    """Lengua de arena encima del suelo quemado (dentro del tile), con su borde en trama."""
    rnd = random.Random(seed)
    for _ in range(n):
        cx, cy = rnd.uniform(5, 11), rnd.uniform(5, 11)
        rx, ry = rnd.uniform(*r), rnd.uniform(r[0] * .6, r[1] * .7); ang = rnd.uniform(-.6, .6)
        nz = noise(seed + _)
        for y in range(1, 15):
            for x in range(1, 15):
                dx, dy = x + .5 - cx, y + .5 - cy
                u = (dx * math.cos(ang) + dy * math.sin(ang)) / rx; w = (-dx * math.sin(ang) + dy * math.cos(ang)) / ry
                d = u * u + w * w + (nz(x, y, 3) - .5) * .5
                if d < .75: cv.px[(x, y)] = sand.px[(x, y)]
                elif d < 1 and (x + y) % 2 == 0: cv.px[(x, y)] = sand.px[(x, y)]
    return cv

def build_burntsand():
    tb = tram('Burnt'); s = SEED['BurntSand']; ts = pal(PERMIAN)
    base = gt.floor_base(tb, s)
    sand = sand_floor(ts, s + 1)
    for p_ in sand.px: sand.px[p_] = blend(sand.px[p_], tb['floor'][2], 70)       # arena apagada, entre ceniza
    nd = pnoise(s + 7, 16, 8); n4 = pnoise(s + 8, 16, 4)
    for (x, y) in list(base.px):                      # velo de arena que da la vuelta (igual en todas las variantes)
        if hsh(x, y, s) % 9 == 0 or (nd(x, y) > .7 and hsh(x, y, s) % 3 == 0): base.px[(x, y)] = sand.px[(x, y)]
    floors = [sand_patch(copy_canvas(base), sand, ts, s + 1, 1, (6, 7.5)),
              sand_patch(copy_canvas(base), sand, ts, s + 2, 2, (3.5, 5)),
              sand_patch(gt.floor_detail(copy_canvas(base), tb, 3, s), sand, ts, s + 3, 1, (4.5, 6)),
              gt.floor_detail(copy_canvas(base), tb, 1, s),
              sand_patch(gt.floor_detail(copy_canvas(base), tb, 2, s), sand, ts, s + 5, 1, (5, 6.5))]
    cap0 = gt.canopy(tb, s + 10)
    faces = [gt.overhang(gt.face_pair(tb, s + 20, v), tb, cap0) for v in range(4)]
    for v in (1, 3):                                  # algo de arena al pie de algunas caras
        f = faces[v]
        for x in range(2, 14):
            h = round(1.5 + 1.2 * math.sin(x * .7 + v))
            for d in range(h): f.px[(x, 31 - d)] = ts['fleck'][0] if d == h - 1 else ts['floor'][1]
    spills = {}
    for side, n in (('Top', 4), ('Left', 4), ('Bottom', 3)):
        for k in range(n):
            spills[f'Spill_{side}_{k}'] = sand_spill(ts, side, k, s) if k % 2 == 0 else gt.spill(tb, side, k, s)
    return tiles_from(tb, base, floors, lambda k: gt.canopy(tb, s + 10, k), faces, spills)

# =================================================================================================
# Sumidero de arena (7×5 tiles, 8 cuadros) y polvo
# =================================================================================================
W, H = 112, 80
FRAMES = 8
CX, CY = W / 2, H / 2 + 1
FY = CY + 10
WALLR = [C(c) for c in ('#120a08', '#1c0f0c', '#281510', '#361c14', '#46241a')]
FUN = [C(c) for c in ('#120805', '#1a0c08', '#24110b', '#2e1610', '#381c14', '#432319', '#4f2b1f', '#5e3526', '#6e412e', '#7e4e36')]
GLOW = [C(c) for c in ('#3a1a10', '#5a2814', '#86401c', '#b8642a', '#e8963e', '#ffc870', '#fff0c0')]
STREAMS = [(31, 0, 9), (60, 2, 13), (83, 4, 7)]   # (x, desfase, primera fila)

def wobbly(x, y, cx, cy, rx, ry, wob, seed):
    a = math.atan2(y - cy, x - cx)
    r = 1 + wob * (math.sin(a * 3 + seed) * .6 + math.sin(a * 7 + seed * 2) * .4)
    return ((x - cx) / (rx * r)) ** 2 + ((y - cy) / (ry * r)) ** 2
def outer(x, y): return wobbly(x, y, CX, CY, 53, 37, .05, 1.0) <= 1
def hole(x, y): return wobbly(x, y, CX, CY - 1, 45, 29, .06, 2.0) <= 1
def near_hole(x, y): return wobbly(x, y, CX, CY - 1, 47.5, 31.5, .06, 2.0) <= 1
def funnel(x, y): return wobbly(x, y, CX, FY, 36, 18, .05, 3.0) <= 1

def sumidero(frame):
    cv = Canvas(W, H)
    t = frame / FRAMES
    pulse = .5 - .5 * math.cos(t * 2 * math.pi)
    tp = pal(PERMIAN)
    for y in range(H):
        for x in range(W):
            px, py = x + .5, y + .5
            if not outer(px, py): continue
            n = hsh(x, y) % 11
            if not hole(px, py):                      # borde de arenisca con arena encima
                a = math.atan2(py - CY, px - CX)
                v = -math.sin(a) * .6 - math.cos(a) * .3
                tone = 5 if v > .5 else 4 if v > .05 else 3 if v > -.45 else 2
                if near_hole(px, py):
                    c = PALE[3] if py < CY + 4 else ROCK[3]
                elif py < CY - 10 or (v > .1 and n < 4):
                    c = tp['fleck'][cl(tone - 3, 2)] if n > 1 else tp['floor'][3]
                else:
                    c = ROCK[tone] if n > 1 else ROCK[tone - 1]
                    sy = (py - CY) + 2 * math.sin(px * .2)  # estratos que se ven en el canto
                    if int(sy) % 5 == 0 and py > CY: c = PALE[cl(tone - 2, 3)]
                cv.put(x, y, c); continue
            if not funnel(px, py):                    # pared interior: estratos que oscurecen al bajar
                depth = (py - (CY - 30)) / 38
                wave = math.sin(x * .19 + 1.3) * 1.4 + math.sin(x * .07) * 2
                band = int((depth * 38 + wave) / 4)
                c = WALLR[cl(4 - band // 2, 4)] if band % 3 else PALE[cl(1 - band // 4, 3)] if band < 6 else WALLR[0]
                if n == 0: c = WALLR[cl(WALLR.index(c) - 1, 4)] if c in WALLR else c
                for sx, off, y0 in STREAMS:           # chorros finos de arena que caen a trazos
                    if x == sx + round(math.sin(y * .25 + sx) * .6) and y >= y0 and (y - frame * 3 + off) % 6 < 4:
                        c = tp['fleck'][1] if (y - frame * 3 + off) % 6 == 0 else tp['fleck'][0]
                        if depth > .75: c = darker(c, .8)
                if funnel(px, py + 2): c = FUN[5] if (x + frame) % 3 else FUN[6]    # la arena entra en el embudo
                cv.put(x, y, c); continue
            # Embudo de arena que gira hacia el centro, donde late una luz cálida
            dx, dy = (px - CX) / 36, (py - FY) / 18
            r = math.hypot(dx, dy)
            th = math.atan2(dy, dx) / (2 * math.pi)
            arm = (3 * th + 3.2 * r + t) % 1.0
            v = 1.2 + r * 6.0 - dy * 2.0 - dx * .8
            v += .8 if arm < .28 else -.5 if arm > .7 else 0
            if not funnel(px, py - 3): v -= 2.0          # sombra de la pared sobre el filo de arriba
            if r < .17:
                g = (1 - r / .17) * (3.0 + pulse * 2.4) - .3
                cv.put(x, y, GLOW[cl(int(g), len(GLOW) - 2)])
            else:
                cv.put(x, y, FUN[cl(dither(v, x, y), len(FUN) - 1)])
    rnd = random.Random(5)
    for i, (mx, my) in enumerate(((34, 34), (58, 30), (76, 38), (46, 46), (68, 50), (30, 52))):   # polvo que sube
        y = my - ((frame * 1.5 + i * 3) % 10)
        if (i + frame) % 4 != 0: cv.put(mx + math.sin(frame + i) * 1.2, y, C(hexc(SANDLIT[i % 2]), 220))
    cv.outline()
    return cv

def dust(frame):
    """0-1: grano que sube; 2: se deshace en polvo; 3: motas."""
    cv = Canvas(8, 8)
    if frame < 2:
        x, y = (3, 3) if frame == 0 else (3, 2)
        cv.put(x, y, SANDLIT[2]); cv.put(x + 1, y, SANDLIT[1]); cv.put(x, y + 1, SANDLIT[1]); cv.put(x + 1, y + 1, SANDLIT[0])
        if frame == 1: cv.put(x, y + 3, C(hexc(SANDLIT[0]), 150)); cv.put(x + 1, y + 4, C(hexc(SANDLIT[0]), 110))
    elif frame == 2:
        for a in range(8):
            cv.put(round(3.5 + math.cos(a * math.pi / 4) * 2.4), round(3.5 + math.sin(a * math.pi / 4) * 2.0), C(hexc(SANDLIT[1 if a % 2 else 0]), 210))
        cv.put(3, 3, SANDLIT[2])
    else:
        for (x, y) in ((0, 3), (7, 2), (2, 7), (6, 6), (4, 0)): cv.put(x, y, C(hexc(SANDLIT[0]), 170))
    return cv

# =================================================================================================
# Sala de muestra: quemada → intermedia → pérmica, con el sumidero
# =================================================================================================
ROOM = ["#" * 40] * 4 + [
    "##" + "." * 11 + "####" + "." * 21 + "##",
    "##" + "." * 11 + "####" + "." * 21 + "##",
    "##" + "." * 36 + "##",
    "##" + "." * 36 + "##",
    "##" + "." * 6 + "###" + "." * 27 + "##",
    "##" + "." * 6 + "###" + "." * 27 + "##",
    "##" + "." * 6 + "###" + "." * 27 + "##",
    "##" + "." * 36 + "##",
    "##" + "." * 36 + "##",
] + ["#" * 40] * 3
PIT_AT = (30, 7)

# Frontera irregular entre franjas (la decide el código con ruido; aquí, la misma idea)
RANK = {'Burnt': 0, 'BurntSand': 1, 'Permian': 2}
def b1(y): return 13 + round(1.6 * math.sin(y * 1.1 + .5) + .8 * math.sin(y * 2.3))
def b2(y): return 26 + round(1.8 * math.sin(y * .9 + 2) + .9 * math.sin(y * 2.1 + 1))
def theme_at(x, y=0): return 'Burnt' if x < b1(y) else 'BurntSand' if x < b2(y) else 'Permian'

# =================================================================================================
# Mezcla entre franjas: capas por máscara (vecinos de la otra franja: N=1, E=2, S=4, O=8), como el lodo
# =================================================================================================
EPH = {'N': .4, 'E': 1.9, 'S': 3.3, 'W': 5.0}
def edge_d(u, side, D): return D + 1.6 * math.sin(2 * math.pi * u / 16 + EPH[side]) + .7 * math.sin(4 * math.pi * u / 16 + EPH[side] * 1.7)
CORNERS = {'NE': (16, 0), 'NO': (0, 0), 'SE': (16, 16), 'SO': (0, 16)}

def reach(x, y, mask, corner, D):
    """Cuánto entra la otra franja en el píxel: >0 dentro, baja hacia el centro del tile."""
    px, py = x + .5, y + .5; v = -99
    if mask & 1: v = max(v, edge_d(px, 'N', D) - py)
    if mask & 2: v = max(v, edge_d(py, 'E', D) - (16 - px))
    if mask & 4: v = max(v, edge_d(px, 'S', D) - (16 - py))
    if mask & 8: v = max(v, edge_d(py, 'W', D) - px)
    if corner:
        cx, cy = CORNERS[corner]
        a = math.atan2(py - cy, px - cx)
        v = max(v, D * .85 + .9 * math.sin(a * 5 + 1) - math.hypot(px - cx, py - cy))
    return v

def overlay(mask, corner, D, paint):
    cv = Canvas(16, 16)
    for y in range(16):
        for x in range(16):
            v = reach(x, y, mask, corner, D)
            if v > -6: paint(cv, x, y, v)
    return cv

def blend_sets(builds):
    tp = pal(PERMIAN); tb = tram('Burnt')
    sand = builds['Permian']['tiles']['Floor_0']; mesa = builds['Permian']['tiles']['Wall_Cap']
    dsand = copy_canvas(sand)
    for q in dsand.px: dsand.px[q] = blend(dsand.px[q], tb['floor'][2], 70)
    def p_sand(cv, x, y, v):                          # arena pérmica que entra en la franja intermedia
        h = hsh(x, y, 7)
        if v > 1.5 or (v > 0 and (x + y) % 2 == 0) or (v > -2.5 and h % (3 + int(-v * 2)) == 0):
            c = sand.px[(x, y)]
            if -.5 < v <= 1.5 and (x + y) % 2: c = tp['fleck'][0]          # filo un poco más claro
            cv.put(x, y, c)
    def p_dust(cv, x, y, v):                          # granos de arena sobre la ceniza
        h = hsh(x, y, 11)
        dens = 2 if v > 1 else 3 if v > -1 else 5 if v > -3 else 8
        if h % dens == 0: cv.put(x, y, dsand.px[(x, y)])
    def p_ash(cv, x, y, v):                           # ceniza y carbón que se cuelan en la arena
        h = hsh(x, y, 13)
        dens = 3 if v > 1 else 5 if v > -1 else 9
        if h % dens == 0: cv.put(x, y, C(('#4a4346', CHAR[1], CHAR[2], TRAMOS['Burnt']['floor'][2])[h // dens % 4]))
    def p_cap(cv, x, y, v):                           # duna que entierra la espesura quemada
        if v <= -2: return
        if v <= 0 and (x + y) % 2: return
        c = mesa.px[(x, y)]                           # la meseta de arenisca sigue por encima de la espesura
        if v <= 0: c = sand.px[(x, y)]                # y la arena suelta va delante
        elif v <= 1: c = tp['leaf'][3] if (x + y) % 3 else tp['leaf'][2]           # filo con luz
        cv.put(x, y, c)
    out = {}
    for name, D, paint in (('SandEdge', 6, p_sand), ('DustEdge', 7, p_dust), ('AshEdge', 5, p_ash), ('SandCap', 7, p_cap)):
        out[name] = {m: overlay(m, None, D, paint) for m in range(1, 16)}
        out[name].update({f'Corner_{c}': overlay(0, c, D, paint) for c in CORNERS})
    return out

def face_canvas(T, v):
    cv = Canvas(16, 32); paste(cv, T[f'Wall_FaceUpper_{v}'], 0, 0); paste(cv, T[f'Wall_FaceLower_{v}'], 0, 16); return cv

def border_face(fb, fp, v, sand_right):
    """Cara de frontera (16×32): tierra quemada a un lado, arenisca al otro; la arena entierra un tronco quemado."""
    tp = pal(PERMIAN)
    cv = Canvas(16, 32)
    for y in range(32):
        for x in range(16):
            wob = 3 * math.sin(y * .35 + v * 2) + (1.5 * math.sin(y * .9) if y >= 8 else 0)
            b = 7 + wob if sand_right else 8 - wob
            sandside = x > b if sand_right else x < b
            cv.px[(x, y)] = fp.px[(x, y)] if sandside else fb.px[(x, y)]
            if abs(x - b) < 1 and y > 6: cv.px[(x, y)] = ROCK[1]
    for i in range(9 if v == 0 else 6):               # tronco carbonizado medio enterrado
        xx = 2 + i if sand_right else 13 - i
        yc = 25 - (i > 4)
        for dy in range(-2, 2): cv.put(xx, yc + dy, C(CHAR[1] if dy > 0 else CHAR[2] if dy > -2 else CHAR[3]))
    cv.put(6 if sand_right else 9, 23, C(EMBER[1]))
    for x in range(16):                               # arena al pie, más alta del lado de la arenisca
        hgt = 4 + round((x if sand_right else 15 - x) / 4 + 1.2 * math.sin(x * .8 + v))
        for d in range(hgt):
            y = 31 - d
            cv.px[(x, y)] = tp['fleck'][1] if d == hgt - 1 and x % 3 else tp['fleck'][0] if d == hgt - 1 else tp['floor'][2] if d > 1 else tp['floor'][0]
    return cv

def transition_faces(builds):
    out = {}
    for v in range(2):
        fb = face_canvas(builds['BurntSand']['tiles'], 1 if v == 0 else 2)
        fp = face_canvas(builds['Permian']['tiles'], v * 2)
        out[f'BurntToSand_{v}'] = border_face(fb, fp, v, True)
        out[f'SandToBurnt_{v}'] = border_face(fb, fp, v, False)
    return out

def sand_decal(w, h, seed):
    """Lengua grande de arena suelta (calcomanía sobre el suelo, cerca de la frontera)."""
    tp = pal(PERMIAN); nz = noise(seed); rnd = random.Random(seed)
    sand = sand_floor(tp, SEED['Permian'])
    cv = Canvas(w, h); ang = rnd.uniform(-.35, .35)
    for y in range(h):
        for x in range(w):
            dx, dy = (x + .5 - w / 2) / (w / 2), (y + .5 - h / 2) / (h / 2)
            u = dx * math.cos(ang) + dy * math.sin(ang); vv = -dx * math.sin(ang) + dy * math.cos(ang)
            d = u * u * (1 + .4 * u) + vv * vv * 1.3 + (nz(x, y, 4) - .5) * .9
            if d < .55: c = sand.px[(x % 16, y % 16)]
            elif d < .8 and (x + y) % 2 == 0: c = sand.px[(x % 16, y % 16)]
            elif d < 1 and hsh(x, y, seed) % 4 == 0: c = tp['fleck'][0]
            else: continue
            if d < .4 and (y - round(3 * math.sin(x * .25 + seed))) % 9 == 0 and x % 4 == 1: c = tp['floor'][3]   # ondas sueltas
            cv.put(x, y, c)
    return cv

def sample_room(builds, blends, faces_t, decals, frame=2, scale=2):
    rows = ROOM; Hh, Ww = len(rows), len(rows[0])
    def is_open(x, y): return 0 <= x < Ww and 0 <= y < Hh and rows[y][x] == '.'
    def wall(x, y): return not is_open(x, y)
    def is_face(x, y): return wall(x, y) and (is_open(x, y + 1) or is_open(x, y + 2))
    def th(x, y):                                     # la cara de abajo usa el tema de la de arriba (par entero)
        if wall(x, y) and is_open(x, y + 1) and wall(x, y - 1): y -= 1
        return theme_at(x, y)
    def rk(x, y): return RANK[th(max(0, min(Ww - 1, x)), max(0, min(Hh - 1, y)))]
    h = lambda x, y: (x * 73856093 ^ y * 19349663) & 0x7fffffff
    im = Image.new('RGBA', (Ww * 16, Hh * 16), BG)
    over = []
    for y in range(Hh):
        for x in range(Ww):
            key = th(x, y); T = builds[key]['tiles']; r = RANK[key]
            if is_open(x, y):
                name = f'Floor_ShadowTop_{h(x, y) % 2}' if wall(x, y - 1) else (f'Floor_{1 + (h(x, y) // 3) % 5}' if h(x, y) % 3 == 0 else 'Floor_0')
                if key == 'BurntSand' and h(x, y) % 2 == 0 and not wall(x, y - 1): name = f'Floor_{1 + (h(x, y) // 7) % 5}'
                tile = T[name].img()
            else:
                up, left, right = is_open(x, y - 1), is_open(x - 1, y), is_open(x + 1, y)
                fl, fu = is_open(x, y + 1), (not is_open(x, y + 1)) and is_open(x, y + 2)
                tile = None
                if fl or fu:
                    hv = h(x, y - 1 if fl else y) % 4
                    part = 'Lower' if fl else 'Upper'
                    name = f'Wall_Face{part}_EndLeft' if left else f'Wall_Face{part}_EndRight' if right else f'Wall_Face{part}_{hv}'
                    if not (left or right) and r < 2:  # cara de frontera en la celda quemada
                        nr, nl = rk(x + 1, y), rk(x - 1, y)
                        tf = faces_t[f'BurntToSand_{hv % 2}'] if nr == 2 else faces_t[f'SandToBurnt_{hv % 2}'] if nl == 2 else None
                        if tf: tile = crop(tf, 0, 0 if fu else 16, 16, 16).img()
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
                if tile is None: tile = T[name].img()
            im.alpha_composite(tile, (x * 16, y * 16))
            if is_face(x, y): continue                # capas de mezcla
            nb = {1: rk(x, y - 1), 2: rk(x + 1, y), 4: rk(x, y + 1), 8: rk(x - 1, y)}
            dg = {'NE': rk(x + 1, y - 1), 'NO': rk(x - 1, y - 1), 'SE': rk(x + 1, y + 1), 'SO': rk(x - 1, y + 1)}
            adj = {'NE': (1, 2), 'NO': (1, 8), 'SE': (4, 2), 'SO': (4, 8)}
            def place(setname, test):
                m = sum(k for k, v in nb.items() if test(v))
                if m: over.append((blends[setname][m], x, y))
                for c, (a, b) in adj.items():
                    if test(dg[c]) and not (m & a) and not (m & b): over.append((blends[setname][f'Corner_{c}'], x, y))
            if is_open(x, y):
                if r == 1: place('SandEdge', lambda v: v == 2)
                if r == 0: place('DustEdge', lambda v: v >= 1)
                if r == 2: place('AshEdge', lambda v: v <= 1)
            elif r < 2: place('SandCap', lambda v: v == 2)
    for cv, x, y in over: im.alpha_composite(cv.img(), (x * 16, y * 16))
    for y in range(Hh):                               # solapas
        for x in range(Ww):
            if not is_open(x, y): continue
            S = builds[th(x, y)]['spills']; hv = h(x * 3, y * 5)
            if wall(x, y - 1) and hv % 2 == 0: im.alpha_composite(S[f'Spill_Top_{hv // 2 % 4}'].img(), (x * 16, y * 16))
            if wall(x - 1, y) and hv % 3 == 0: im.alpha_composite(S[f'Spill_Left_{hv // 3 % 4}'].img(), (x * 16, y * 16))
            if wall(x + 1, y) and hv % 3 == 1: im.alpha_composite(S[f'Spill_Left_{hv // 3 % 4}'].img().transpose(Image.FLIP_LEFT_RIGHT), (x * 16, y * 16))
            if wall(x, y + 1) and hv % 2 == 1: im.alpha_composite(S[f'Spill_Bottom_{hv // 2 % 3}'].img(), (x * 16, y * 16))
    for i, (dx, dy) in enumerate(((21 * 16 + 4, 11 * 16 + 2), (27 * 16 + 6, 5 * 16 + 4), (16 * 16, 7 * 16 + 8))):   # calcomanías
        im.alpha_composite(decals[i].img(), (dx, dy))
    im.alpha_composite(PIT[frame].img(), (PIT_AT[0] * 16, PIT_AT[1] * 16))
    return im.resize((im.width * scale, im.height * scale), Image.NEAREST), im

# =================================================================================================
# Salida
# =================================================================================================
def write_set(key, b):
    d = os.path.join(HERE, key)
    for sub in ('tiles', 'deco'): os.makedirs(os.path.join(d, sub), exist_ok=True)
    for name in gt.TILE_ORDER: b['tiles'][name].img().save(os.path.join(d, 'tiles', name + '.png'))
    gt.grid([b['tiles'][n] for n in gt.TILE_ORDER], 8).save(os.path.join(d, f'Tileset_{key}.png'))
    for name, cv in b['spills'].items(): cv.img().save(os.path.join(d, 'deco', name + '.png'))

if __name__ == '__main__':
    bb = gt.build('Burnt'); builds = {'Burnt': dict(tiles=bb['tiles'], spills=bb['spills'])}
    builds['BurntSand'] = build_burntsand(); builds['Permian'] = build_permian()
    for k in ('Permian', 'BurntSand'): write_set(k, builds[k])
    PIT = [sumidero(k) for k in range(FRAMES)]
    DUST = [dust(k) for k in range(4)]
    save_sheet(os.path.join(HERE, 'Transition_Pit_Sand.png'), PIT)
    save_sheet(os.path.join(HERE, 'Transition_Dust_Sand.png'), DUST)
    os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True)
    blends = blend_sets(builds); faces_t = transition_faces(builds)
    decals = [sand_decal(48, 32, 5), sand_decal(32, 24, 9), sand_decal(40, 20, 14)]
    d = os.path.join(HERE, 'blend'); os.makedirs(d, exist_ok=True)
    for name, st in blends.items():
        for m, cv in st.items(): cv.img().save(os.path.join(d, f'{name}_{m}.png'))
        gt.grid([st[m] for m in range(1, 16)] + [st[f'Corner_{c}'] for c in CORNERS], 8).save(os.path.join(d, f'{name}_Tileset.png'))
    for name, cv in faces_t.items():
        crop(cv, 0, 0, 16, 16).img().save(os.path.join(d, f'Wall_FaceUpper_{name}.png'))
        crop(cv, 0, 16, 16, 16).img().save(os.path.join(d, f'Wall_FaceLower_{name}.png'))
    for i, cv in enumerate(decals): cv.img().save(os.path.join(d, f'Sand_Drift_{i}.png'))
    big, small = sample_room(builds, blends, faces_t, decals)
    big.save(os.path.join(HERE, 'vista_previa_sala.png'))
    # sumidero sobre la arena, a ×4, con un grano de polvo que sube
    sand = builds['Permian']['tiles']['Floor_0']
    pad = 16; frames = []
    for k, f in enumerate(PIT):
        im = Image.new('RGBA', (W + pad * 2, H + pad * 2))
        for yy in range(0, im.height, 16):
            for xx in range(0, im.width, 16): im.alpha_composite(sand.img(), (xx, yy))
        im.alpha_composite(f.img(), (pad, pad))
        im.alpha_composite(DUST[min(3, k // 2)].img(), (pad + 60, pad + 40 - k * 3))
        frames.append(im)
    frames[2].resize((frames[2].width * 4, frames[2].height * 4), Image.NEAREST).save(os.path.join(HERE, 'vista_previa_sumidero.png'))
    g = [f.resize((f.width * 4, f.height * 4), Image.NEAREST).convert('RGB') for f in frames]
    g[0].save(os.path.join(HERE, 'gif', 'Transition_Pit_Sand.gif'), save_all=True, append_images=g[1:], duration=167, loop=0)
    gt.preview(os.path.join(HERE, 'vista_previa_piezas.png'), {
        'Permian tiles': [builds['Permian']['tiles'][n] for n in gt.TILE_ORDER[:12]],
        'Permian caras+bordes': [builds['Permian']['tiles'][n] for n in gt.TILE_ORDER[12:]],
        'Permian solapas': list(builds['Permian']['spills'].values()),
        'BurntSand suelos': [builds['BurntSand']['tiles'][f'Floor_{k}'] for k in range(6)] + [builds['BurntSand']['tiles'][f'Wall_FaceLower_{v}'] for v in (1, 3)],
        'BurntSand solapas': list(builds['BurntSand']['spills'].values()),
        'Polvo': DUST,
        **{f'{n} (mascaras)': [blends[n][m] for m in range(1, 16)] for n in blends},
        **{f'{n} (esquinas)': [blends[n][f'Corner_{c}'] for c in CORNERS] for n in blends},
        'Caras de frontera': list(faces_t.values()), 'Calcomanias': decals}, BG, scale=3, cols=15)
    print('Hecho')
