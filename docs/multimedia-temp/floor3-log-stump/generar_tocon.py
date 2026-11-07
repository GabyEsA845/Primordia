"""Tocón hueco del piso 3 (paso 6, bloque 4: salas especiales; ver README). El tronco caído está en generar_tronco.py.

- Tocón hueco (`Stump/`): la sala ES el interior de un tocón gigante. Tileset propio (mismo motor y nombres que los
  tramos del piso: paredes de madera podrida por dentro, borde del tocón visto desde arriba, suelo de serrín y madera
  blanda) y piezas: rayo de luz que entra por arriba sobre el cono, hongos de repisa en las paredes, setas, trozos de
  madera y serrín. Igual en todos los tramos (refugio).
Hecho con la skill primordia-sprites (pixel.py): 16 px por unidad, luz arriba a la izquierda, contorno OUTL.

Uso: python generar_tocon.py
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
TEMP = os.path.normpath(os.path.join(HERE, '..'))
ENV = os.path.join(TEMP, 'floor3-environment')
sys.path.insert(0, ENV)
from base_piso3 import *
import base_piso3
from PIL import Image
import generar_tilesets_piso3 as TS

MUDP = [C(c) for c in MUD]                        # #22160f #33221a #4a3324 #634830 #86683f
SHADOW = (24, 20, 37, 110)

# Corteza de Lepidodendron tumbada (la usan los trozos de madera del tocón)
def bark_px(x, y, t0, b0, bark):
    """Color de la corteza en (x, y) para un tronco tumbado entre t0 y b0: cojinetes en rombo que se aprietan hacia
    los bordes (el tronco es un cilindro), luz arriba y sombra abajo, contra el lodo."""
    v = (y + .5 - t0) / (b0 + 1 - t0)
    s = math.asin(max(-1., min(1., 2 * v - 1))) / (math.pi / 2)
    yy = (s + 1) * 20
    row = int(yy // 6.5)
    lx = (x + (4 if row % 2 else 0)) % 8 - 3.5
    ly = yy % 6.5 - 3.25
    dd = abs(lx) / 4 + abs(ly) / 3.25
    light = 2 if v < .1 else 4 if v < .32 else 3 if v < .55 else 2 if v < .76 else 1 if v < .9 else 0
    if dd > .9: c = bark[max(0, light - 2)]
    elif dd < .24 and light >= 2: c = bark[light - 2]
    else: c = bark[light] if dd < .66 else bark[max(0, light - 1)]
    return c, v, dd

# =================================================================================================
# TOCÓN HUECO: el tramo «Stump» para el motor del tileset
# =================================================================================================
FUNG = [C(c) for c in ('#3e301c', '#6e5a34', '#a08a56', '#cfc08a', '#f0e8c4')]   # hongos (apagados: es escenario)

base_piso3.TRAMOS['Stump'] = dict(
    nombre='Tocón hueco',
    floor=['#2e2016', '#36261a', '#3e2c1e', '#473323'],          # madera blanda y serrín (lo más claro de la sala)
    fleck=['#5e4630', '#7e6040', '#a68458'],
    face=['#170f0a', '#24170f', '#342216', '#47301e', '#5e4028'],   # la pared del tocón por dentro
    cap=['#1a120c', '#22180f', '#2b1e13', '#352517'],             # el borde del tocón visto desde arriba
    vein=['#3d5a22', '#56772e', '#86a24a'],
    leaf=['#1c2c14', '#2a4019', '#3d5a22', '#56772e'],            # musgo que cuelga del borde
    bark=['#1e1610', '#2e2218', '#433222', '#5c4630', '#7a6044'],
    root=['#1e1612', '#33241a', '#4c3828', '#6a4e34', '#8e6e48'],
    seal=['#7a6a42', '#b0a064', '#dcd09a', '#f8f0d0'],            # nudo de las puertas: hongo pálido
)
TS.SEED['Stump'] = 610
_canopy, _face_pair = TS.canopy, TS.face_pair

def stump_cap(t, seed, extra=0):
    """Borde del tocón desde arriba: madera podrida con anillos que ondulan, grietas y motas de musgo."""
    cv = WrapCanvas(16, 16, True, True); cap = t['cap']
    n = pnoise(seed, 16, 4); rnd = random.Random(seed)
    for y in range(16):
        for x in range(16):
            ring = ((y + 1.6 * math.sin(2 * math.pi * x / 16 + .7) + .8 * math.sin(2 * math.pi * x / 8 + 2.1)) / 4) % 1
            m = n(x, y)
            c = cap[0] if ring < .18 else cap[3] if ring < .3 and m > .35 else cap[2] if ring < .55 else cap[1]
            cv.px[(x, y)] = c
    for k in range(2):                                   # grietas
        x, y = rnd.randrange(16), rnd.randrange(16)
        for j in range(rnd.randrange(3, 6)): cv.put(x + (j > 2), y + j, cap[0])
    cv.put(rnd.randrange(16), rnd.randrange(16), t['vein'][0])
    r2 = random.Random(seed * 13 + extra); cx, cy = r2.randrange(5, 11), r2.randrange(5, 11)
    if extra == 1:                                       # mata de musgo
        for (dx, dy, k) in ((0, 0, 1), (1, 0, 2), (-1, 0, 0), (0, -1, 2), (0, 1, 0), (1, 1, 0), (-1, -1, 1)):
            cv.put(cx + dx, cy + dy, t['vein'][k])
    elif extra == 2:                                     # setilla pálida
        cv.put(cx, cy, FUNG[2]); cv.put(cx + 1, cy, FUNG[1]); cv.put(cx, cy + 1, FUNG[0]); cv.put(cx - 1, cy, FUNG[3])
    elif extra == 3:                                     # agujeros de escarabajo
        for (dx, dy) in ((0, 0), (3, 2), (1, 4)):
            cv.put(cx + dx, cy + dy, PASSAGE); cv.put(cx + dx - 1, cy + dy, cap[3]); cv.put(cx + dx, cy + dy - 1, cap[3])
    return cv

def bracket(cv, x, y, w, h):
    """Hongo de repisa pegado a la pared: media elipse con la cara de arriba clara y la sombra debajo."""
    for yy in range(y, y + h):
        for xx in range(x, x + w):
            u = (xx + .5 - x) / w * 2 - 1; v = (yy + .5 - y) / h
            if u * u + (v - 0.05) ** 2 * 1.0 > 1 or v > 1: continue
            k = 4 if v < .3 and u < .2 else 3 if v < .45 else 2 if v < .8 else 1
            cv.put(xx, yy, FUNG[k])
    for xx in range(x + 1, x + w - 1): cv.put(xx, y + h, FUNG[0])
    for xx in range(x + 2, x + w - 2):
        if xx % 2: cv.put(xx, y + h + 1, darker(cv.px.get((xx, y + h + 1), FUNG[0]), .7))

def stump_face(t, seed, var):
    """Pared del tocón por dentro (16×32): fibras verticales con crestas iluminadas a la izquierda, más oscura al pie.
    Variantes: 0 grieta, 1 hongos de repisa, 2 raicillas que cuelgan, 3 hueco de un nudo con musgo."""
    cv = WrapCanvas(16, 32, True, False); f = t['face']
    n = pnoise(seed, 16, 4); rv = random.Random(seed * 31 + var)
    widths = (3, 5, 4, 4); edges = []
    x0 = 0
    for w in widths: edges.append((x0, w)); x0 += w
    for y in range(32):
        sway = round(math.sin(y / 5 + 1.3) * .8)
        for x in range(16):
            xx = (x - sway) % 16
            for (e, w) in edges:
                if e <= xx < e + w: i = xx - e; break
            k = 3 if i == 0 else 1 if i == w - 1 else 2
            if i == w - 1 and (y + e) % 7 < 2: k = 0
            m = n(x, y % 16 + (16 if y >= 16 else 0))
            if m > .72: k = max(0, k - 1)                        # madera podrida, más oscura
            if y < 4: k = max(0, k - 1)                          # bajo el borde
            if y > 25: k = max(0, k - (1 if y < 29 else 2))      # al pie
            cv.px[(x, y)] = f[k]
    cv.clip = (2, 0, 14, 32)
    if var == 0:                                         # grieta larga
        x = rv.randrange(5, 10)
        for y in range(5, 27):
            if y % 6 == 3: x += rv.choice((-1, 1))
            cv.put(x, y, PASSAGE); cv.put(x - 1, y, f[4] if y % 3 else f[3])
    elif var == 1:                                       # hongos de repisa
        bracket(cv, 3, 9, 9, 4); bracket(cv, 7, 17, 7, 3)
    elif var == 2:                                       # raicillas que cuelgan desde el borde
        for k in range(3):
            mx = rv.randrange(3, 13); L = rv.randrange(10, 20)
            for j in range(L): cv.put(mx + round(math.sin(j / 3 + mx) * .7), 3 + j, t['root'][3] if j % 5 else t['root'][2])
    elif var == 3:                                       # hueco de un nudo con musgo
        for y in range(11, 20):
            for x in range(5, 12):
                d = ((x + .5 - 8.5) / 3.4) ** 2 + ((y + .5 - 15.5) / 4.6) ** 2
                if d < 1: cv.put(x, y, PASSAGE if d < .62 else f[4] if (x < 8 or y < 13) else f[1])
        for (dx, k) in ((-2, 0), (-1, 1), (0, 2), (1, 1), (2, 0)): cv.put(8 + dx, 20, t['vein'][k])
    cv.clip = None
    for x in range(16):                                  # musgo al pie y sombra contra el suelo
        if math.sin(x * 1.7 + 1) > .4: cv.put(x, 30, t['vein'][0])
        cv.px[(x, 31)] = t['cap'][0]
    return cv

TS.canopy = lambda t, seed, extra=0: stump_cap(t, seed, extra) if t['key'] == 'Stump' else _canopy(t, seed, extra)
TS.face_pair = lambda t, seed, var: stump_face(t, seed, var) if t['key'] == 'Stump' else _face_pair(t, seed, var)

def floor_stump(base, k, seed):
    """Suelo: serrín, astillas, setillas, agujeros de escarabajo con su serrín, y una mancha de musgo."""
    t = TS.tram('Stump'); cv = copy_canvas(base, Canvas); rnd = random.Random(seed + k * 13)
    fl = t['fleck']; fo = t['floor']
    if k == 1:                                           # montoncito de serrín
        for (dx, dy, c) in ((0, 0, 1), (1, 0, 2), (2, 0, 1), (-1, 1, 0), (0, 1, 1), (1, 1, 2), (2, 1, 1), (3, 1, 0), (1, -1, 2)):
            cv.put(6 + dx, 8 + dy, fl[c])
        cv.put(11, 5, fl[1]); cv.put(4, 11, fl[0])
    elif k == 2:                                         # astillas
        for i in range(6): cv.put(3 + i, 6 + i // 2, fl[2] if i % 2 else fl[1])
        for i in range(4): cv.put(10 + i, 11 - i // 2, fl[1])
        cv.put(3, 7, fo[0]); cv.put(10, 12, fo[0])
    elif k == 3:                                         # setillas
        for (x, y, big) in ((6, 7, True), (9, 9, False), (5, 10, False)):
            cv.put(x, y + 1, FUNG[1]); cv.put(x, y, FUNG[3]); cv.put(x + 1, y, FUNG[2]); cv.put(x - 1, y, FUNG[3] if big else FUNG[2])
            if big: cv.put(x, y - 1, FUNG[4]); cv.put(x + 1, y + 1, fo[0])
    elif k == 4:                                         # agujeros de escarabajo y su serrín
        for (x, y) in ((5, 6), (9, 8), (6, 11)):
            cv.put(x, y, PASSAGE); cv.put(x - 1, y, fo[3]); cv.put(x, y - 1, fo[3]); cv.put(x + 1, y + 1, fl[1])
    else:                                                # mancha de musgo con una astilla
        for (dx, dy, c) in ((0, 0, 1), (1, 0, 2), (-1, 0, 0), (0, 1, 0), (1, -1, 1), (2, 0, 0), (0, -1, 1)):
            cv.put(7 + dx, 7 + dy, t['vein'][c])
        for i in range(4): cv.put(4 + i, 11, fl[2] if i == 0 else fl[1])
    return cv

def build_stump():
    b = TS.build('Stump')
    T = b['tiles']
    for k in range(1, 6): T[f'Floor_{k}'] = floor_stump(T['Floor_0'], k, 3)
    return b

# --- piezas del tocón --------------------------------------------------------------------------------
LIGHT = (255, 236, 184)

def light_shaft(f):
    """Rayo de luz que entra por la boca del tocón (64×112, translúcido, 8 cuadros): una franja inclinada que baja
    desde arriba a la izquierda hasta un charco de luz en el suelo, con motas de polvo que flotan. El charco
    (centro en (38, 100)) va bajo el cono. En Unity, mejor aditivo y con una luz 2D cálida."""
    W, H = 64, 112; cv = Canvas(W, H)
    br = .9 + .1 * math.sin(f / 8 * 2 * math.pi)
    px, py, rx, ry = 38, 100, 25, 9
    for y in range(H):
        for x in range(W):
            a = 0.
            u = y / (py - 2)                                        # franja: de x 6..34 arriba a x 22..54 abajo
            if u <= 1:
                l, r = 6 + 16 * u, 34 + 20 * u
                if l <= x + .5 <= r:
                    edge = min(x + .5 - l, r - x - .5) / 5
                    a = 46 * min(1, edge) * (.6 + .4 * u)
                    if (x + 2 * y + f) % 23 == 0: a += 8           # haces más claros que bajan
            d = math.hypot((x + .5 - px) / rx, (y + .5 - py) / ry)
            if d < 1: a = max(a, 70 * (1 - d) ** .8 + 18)
            if d < .45: a += 16
            a *= br
            if a > 5: cv.put(x, y, LIGHT + (int(min(150, a)),))
    rnd = random.Random(4)                                     # motas de polvo
    for k in range(9):
        x0, y0, sp = rnd.uniform(10, 46), rnd.uniform(0, 96), rnd.uniform(.6, 1.3)
        y = (y0 + f * sp * 1.5) % 96; x = x0 + 8 * y / 96 + math.sin((f + k) / 8 * 2 * math.pi) * .8
        cv.put(x, y, (255, 246, 214, 190 if k % 3 else 230))
    return cv

def shelf_fungus(kind):
    """Hongos de repisa sueltos, para pegar en las caras de la pared (pivote: centro)."""
    if kind == 'A': cv = Canvas(14, 14); bracket(cv, 1, 1, 10, 4); bracket(cv, 4, 7, 9, 4)
    elif kind == 'B': cv = Canvas(18, 9); bracket(cv, 1, 1, 15, 5)
    else: cv = Canvas(12, 17); bracket(cv, 1, 1, 7, 3); bracket(cv, 4, 6, 7, 3); bracket(cv, 2, 11, 6, 3)
    cv.outline(); cv.clean(); return cv

def mushrooms(kind):
    """Matas de setas del suelo (pivote: el pie). Sombrero pálido, pie claro y sombra."""
    spots = {'A': ((4, 8, 3), (8, 9, 2), (11, 7, 2)), 'B': ((5, 9, 4), (10, 10, 2)), 'C': ((3, 9, 2), (6, 7, 3), (10, 9, 3), (13, 10, 2))}[kind]
    cv = Canvas(18, 14)
    for (x, y, r) in sorted(spots, key=lambda s: s[1]):
        for j in range(1, r + 1): cv.put(x, y + j, FUNG[3] if j < r else FUNG[2])
        for dx in range(-r, r + 1):
            for dy in range(-(r + 1) // 2, 1):
                if (dx / (r + .5)) ** 2 + (dy / ((r + 1) / 2 + .3)) ** 2 < 1:
                    cv.put(x + dx, y + dy, FUNG[4] if dx == -1 and dy == -(r + 1) // 2 + 1 else FUNG[3] if dy < 0 and dx < 0 else FUNG[2])
        cv.put(x + r, y, FUNG[1]); cv.put(x - 1, y + 1, FUNG[1])
    cv.outline(); cv.clean()
    for x in range(1, 17):
        if cv.px.get((x, 13)) is None and any(cv.px.get((x, y)) not in (None, OUTL) for y in range(13)): cv.put(x, 13, SHADOW)
    return cv

def wood_chunk(kind):
    """Trozo de madera podrida caído de la pared (pivote: el pie): corteza en rombo por fuera, madera blanda dentro."""
    t = TS.tram('Stump'); bark = t['bark']; fl = t['fleck']
    w, h = (22, 13) if kind == 'A' else (16, 11)
    cv = Canvas(w, h)
    for y in range(1, h - 2):
        for x in range(1, w - 1):
            u = (x + .5) / w; v = (y - 1 + .5) / (h - 3)
            if (2 * u - 1) ** 2 + (2 * v - 1) ** 4 > 1: continue
            if v < .45:                                         # cara de arriba: madera blanda clara con vetas
                c = fl[2] if (x + y) % 5 and u < .5 else fl[1] if (x * 3 + y) % 7 else fl[0]
            else:
                c, _, _ = bark_px(x, y, int(h * .45), h - 3, bark)
            cv.put(x, y, c)
    cv.outline(); cv.clean()
    for x in range(2, w - 2): cv.put(x, h - 1, SHADOW)
    return cv

def sawdust(kind):
    """Mancha de serrín en el suelo (sin contorno: es del suelo)."""
    t = TS.tram('Stump'); fl = t['fleck']; w, h = (26, 12) if kind == 'A' else (18, 10)
    cv = Canvas(w, h); n = noise(17 + ord(kind))
    for y in range(h):
        for x in range(w):
            d = ((x + .5 - w / 2) / (w / 2)) ** 2 + ((y + .5 - h / 2) / (h / 2)) ** 2
            m = n(x * 2, y * 2, 3)
            if d < .45 or (d < 1 and m > .55 and (x + y) % 2 == 0):
                cv.put(x, y, fl[2] if d < .15 and m > .5 else fl[1] if m > .45 else fl[0])
    return cv

def stump_pieces():
    P = {'Fx_StumpLight': [light_shaft(f) for f in range(8)]}
    for k in 'ABC': P[f'Deco_ShelfFungus_{k}'] = [shelf_fungus(k)]
    for k in 'ABC': P[f'Deco_Mushrooms_{k}'] = [mushrooms(k)]
    for k in 'AB': P[f'Deco_WoodChunk_{k}'] = [wood_chunk(k)]
    for k in 'AB': P[f'Deco_Sawdust_{k}'] = [sawdust(k)]
    return P

# =================================================================================================
# Maquetas
# =================================================================================================
STUMP_ROOM = [  # 22×14 como Room_Special_HollowStump (contorno irregular); 'L' puerta (con maleza al otro lado)
    "######################",
    "######################",
    "######################",
    "#####...........######",
    "###...............####",
    "##.................###",
    "L..................###",
    "L...................##",
    "##..................##",
    "###................###",
    "####..............####",
    "######.........#######",
    "######################",
    "######################",
]

def stump_scene(b, P, f=0):
    TS.SAMPLE = STUMP_ROOM
    big, small = TS.sample_room(b)
    im = small.convert('RGBA'); W, H = im.size
    out = Image.new('RGBA', (480, 270), (10, 7, 5, 255)); ox, oy = (480 - W) // 2, (270 - H) // 2
    out.alpha_composite(im, (ox, oy))
    def at(img, x, y): out.alpha_composite(img, (ox + x, oy + y))
    for (k, x, y) in (('A', 70, 150), ('B', 230, 170)): at(P[f'Deco_Sawdust_{k}'][0].img(), x, y)
    for (k, x, y) in (('A', 60, 100), ('B', 250, 70), ('C', 205, 150), ('A', 300, 135)): at(P[f'Deco_Mushrooms_{k}'][0].img(), x, y)
    for (k, x, y) in (('A', 120, 160), ('B', 270, 108)): at(P[f'Deco_WoodChunk_{k}'][0].img(), x, y)
    for (k, x, y) in (('A', 100, 26), ('B', 196, 20), ('C', 152, 30)): at(P[f'Deco_ShelfFungus_{k}'][0].img(), x, y)
    cone = Image.open(os.path.join(ENV, 'pieces', 'Cone_Big_Idle.png')).convert('RGBA')
    cw = 32; cone = cone.crop((cw * (f // 2 % 4), 0, cw * (f // 2 % 4) + cw, cone.height))
    cx, cy = 11 * 16, 7 * 16 + 6                                   # centro de las casillas CC (fila 6), pie del cono
    at(P['Fx_StumpLight'][f].img(), cx - 38, cy - 100)
    at(cone, cx - 16, cy - cone.height + 2)
    return out

if __name__ == '__main__':

    b = build_stump()
    d = os.path.join(HERE, 'Stump'); os.makedirs(os.path.join(d, 'tiles'), exist_ok=True); os.makedirs(os.path.join(d, 'deco'), exist_ok=True)
    for name in TS.TILE_ORDER: b['tiles'][name].img().save(os.path.join(d, 'tiles', name + '.png'))
    TS.grid([b['tiles'][n] for n in TS.TILE_ORDER], 8).save(os.path.join(d, 'Tileset_Stump.png'))
    for name, cv in b['spills'].items(): cv.img().save(os.path.join(d, 'deco', name + '.png'))
    b['arch'].img().save(os.path.join(d, 'Doorway_Top.png'))
    for side, (close, opn) in b['doors'].items():
        save_sheet(os.path.join(d, f'Door_{side}_Close.png'), close); save_sheet(os.path.join(d, f'Door_{side}_Open.png'), opn)
    P = stump_pieces()
    pd = os.path.join(HERE, 'pieces'); os.makedirs(pd, exist_ok=True)
    for name, frames in P.items(): save_sheet(os.path.join(pd, name + '.png'), frames)
    frames = [stump_scene(b, P, f).resize((960, 540), Image.NEAREST) for f in range(8)]
    frames[0].save(os.path.join(HERE, 'maqueta_tocon.png'))
    os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True)
    frames[0].save(os.path.join(HERE, 'gif', 'escena_tocon.gif'), save_all=True, append_images=frames[1:], duration=140, loop=0, disposal=2)
    preview(os.path.join(HERE, 'vista_previa_tocon.png'), P, C('#2e2016'), scale=4)
    print('Listo en', HERE)
