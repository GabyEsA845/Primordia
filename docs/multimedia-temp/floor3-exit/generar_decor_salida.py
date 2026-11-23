"""Decoración de la sala de paso del piso 3 al 4 (2026-11-23, Opus): «del bosque muerto al desierto».

Franja quemada: piezas que ya existen (floor3-environment/Burnt/props, helechos quemados, ascuas).
Franja intermedia (BurntSand): restos del bosque, ya fríos, que la arena va enterrando (subtanda A).
Franja pérmica: rocas y lajas (B), plantas pérmicas secas (C), huellas fósiles hacia el sumidero (D) y ráfagas (E).

Subtanda A (`decor/`):
  Deco_BuriedLog_{0,1,2}   troncos de Lepidodendron carbonizados, tumbados y medio enterrados (se pisan)
  Obstacle_BuriedTrunk     tronco partido de pie con una duna apoyada (colisión en la base)
  Deco_BuriedStump_{0,1}   tocones enterrados hasta la mitad
  Deco_DeadFern_{0,1,2}    helechos carbonizados que asoman de la arena
  Deco_Charcoal_{0,1,2}    trozos de carbón medio enterrados

Uso: python generar_decor_salida.py
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import generar_salida_piso3 as S
from generar_salida_piso3 import *                    # noqa: F401,F403
from PIL import Image

DECOR = os.path.join(HERE, 'decor')
ENV = os.path.join(HERE, '..', 'floor3-environment')
CH = [C(c) for c in CHAR]                              # carbón, frío
ASH = [C(c) for c in ('#4a4346', '#6d6466', '#958a88')]
SND = [C(c) for c in ('#2a1814', '#35201a', '#3d241c', '#4a2c21', '#5c3726')]   # arena pérmica, apagada como el suelo
O = OUTL if 'OUTL' in dir() else C('#181425')

def rgba(c, a=255): return tuple(c[:3]) + (a,)

def outline_where(cv, pts):
    """Contorno solo alrededor de los píxeles dados (la madera), no de la arena."""
    add = set()
    for (x, y) in pts:
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            q = (x + dx, y + dy)
            if q not in cv.px and 0 <= q[0] < cv.w and 0 <= q[1] < cv.h: add.add(q)
    for q in add: cv.px[q] = rgba(O)

def bark(x, y, nx, ny, seed, burnt_top=True):
    """Corteza carbonizada con cojinetes en rombo; nx, ny = normal aproximada (-1..1) para la luz de arriba-izq."""
    l = -(nx * .55 + ny * .8)                          # >0 con luz
    i = 2 + (1 if l > .35 else 0) + (1 if l > .75 else 0) - (1 if l < -.3 else 0) - (1 if l < -.7 else 0)
    if ((x + y) % 6 == 0 or (x - y) % 6 == 0) and i > 0: i -= 1   # rejilla de rombos
    c = CH[max(0, min(4, i))]
    if burnt_top and l > .6 and hsh(x, y, seed) % 5 == 0: c = ASH[1]   # ceniza encima
    return c

def drift(cv, x0, x1, top, bot, seed):
    """Arena: rellena de top(x) a bot(x) entre x0 y x1 (tapa lo que haya). Luz en las laderas que miran a la izquierda,
    sombra en las de la derecha; bordes en trama para fundirse con el suelo."""
    for x in range(max(0, x0), min(cv.w, x1)):
        t = top(x); b = bot(x); sl = top(x + 1) - top(x - 1)
        edge = min(x - x0, x1 - 1 - x)
        for y in range(max(0, math.ceil(t)), min(cv.h, b + 1)):
            if (edge < 2 or y >= b) and (x + y) % 2: continue
            if edge < 1 and hsh(x, y, seed) % 3: continue
            d = y + .5 - t
            if d < 1.2: c = SND[4] if sl < -.35 else SND[3] if sl < .35 else SND[2]
            elif d < 3: c = SND[3] if sl < -.35 else SND[2]
            else: c = SND[2] if sl < .1 else SND[1]
            if hsh(x, y, seed) % 13 == 0: c = SND[1]
            cv.px[(x, y)] = rgba(c)

def bump(x, cx, rx, ry, skew=0.0):
    dx = (x + .5 - cx) / rx
    if abs(dx) >= 1: return 0.0
    return ry * (1 - dx * dx) ** .8 * (1 + skew * dx)

def mound(cv, cx, base, rx, ry, seed, skew=0.0):
    nz = noise(seed)
    drift(cv, int(cx - rx), int(cx + rx) + 1, lambda x: base - bump(x, cx, rx, ry, skew) + (nz(x, 0, 3) - .5) * 1.2,
          lambda x: base + (1 if abs(x + .5 - cx) < rx * .6 else 0), seed)

def buried_log(w, h, seed, humps=2, cut_left=True):
    cv = Canvas(w, h); rnd = random.Random(seed)
    r = h * .3; yc = h * .5; x0, x1 = 4, w - 5
    wood = set()
    for y in range(h):
        for x in range(x0, x1):
            t = (x - x0) / (x1 - x0); rr = r * (1 - .25 * t)
            dy = (y + .5 - yc) / rr
            if abs(dy) > 1: continue
            if x > x1 - 4 and hsh(x, y, seed) % 3 == 0: continue  # punta rota
            cv.px[(x, y)] = rgba(bark(x, y, 0, dy, seed)); wood.add((x, y))
            if cut_left and x < x0 + 2:
                cv.px[(x, y)] = rgba(CH[3] if int(abs(dy) * 3 + x) % 2 else CH[2])
    hx = sorted(rnd.uniform(.25, .75) for _ in range(humps))     # lenguas de duna que cruzan el tronco
    nz = noise(seed + 1)
    def cross(x):
        t = (x - x0) / (x1 - x0)
        return max(math.exp(-((t - u) / .12) ** 2) for u in hx) if 0 <= t <= 1 else 0
    def top(x):
        if x0 <= x < x1:                               # hundido hasta la mitad; donde cruza una duna, tapado
            return yc + .15 * r - (1.15 * r + 1.5) * min(1, cross(x) * 1.6) + (nz(x, 0, 2) - .5) * .8
        d = (x0 - x) if x < x0 else (x - x1 + 1)        # montoncitos en las puntas
        return yc + .15 * r + d * 1.1 + (nz(x, 0, 3) - .5)
    vis = lambda p: p[1] + .5 < top(p[0])
    outline_where(cv, [p for p in wood if vis(p)])
    for q in [q for q, c in cv.px.items() if c[:3] == tuple(O[:3]) and q[1] + .5 >= top(q[0])]: del cv.px[q]
    drift(cv, 0, w, top, lambda x: int(yc + r * .9) + (1 if x0 + 2 < x < x1 - 2 else 0), seed + 3)
    return cv

def buried_trunk():
    """Tronco partido de pie (obstáculo, colisión en la base), duna apoyada a la izquierda (el viento)."""
    w, h = 40, 48; cv = Canvas(w, h); cx = 21; base = 42
    wood = set()
    for y in range(h):
        for x in range(w):
            hy = base - y
            if hy < 0: continue
            rr = 7.5 + (3.5 * max(0, 1 - hy / 7) ** 2)         # se abre al pie (raíces)
            top = 34 + 4 * math.sin(x * 1.3) + 3 * math.sin(x * 2.9 + 1)   # punta rota, dentada
            dx = (x + .5 - cx) / rr
            if abs(dx) > 1 or hy > top: continue
            cv.px[(x, y)] = rgba(bark(x, y, dx, -.2, 7)); wood.add((x, y))
    for y in range(h):                                  # el hueco quemado de arriba
        for x in range(w):
            if (x, y) in wood and base - y > 30 and abs(x + .5 - cx) < 4 and (x, y - 2) not in wood:
                cv.px[(x, y)] = rgba(CH[0])
    for k, (x, y) in enumerate(((14, 18), (25, 24), (17, 30), (23, 12))):   # grietas con ceniza
        for d in range(4): cv.px[(x, y + d)] = rgba(CH[0] if d % 2 else CH[1])
    outline_where(cv, wood)
    mound(cv, 13, base + 3, 13, 12, 21, skew=-.3)           # duna contra el lado izquierdo (el viento)
    mound(cv, 27, base + 3, 11, 4, 22)
    return cv

def buried_stump(seed, w=24, h=22):
    cv = Canvas(w, h); cx = w / 2; base = h - 6; wood = set()
    for y in range(h):
        for x in range(w):
            hy = base - y; rr = 6 + 2.5 * max(0, 1 - hy / 4)
            top = 9 + 2 * math.sin(x * 1.7 + seed)
            dx = (x + .5 - cx) / rr
            if hy < -2 or abs(dx) > 1 or hy > top: continue
            cv.px[(x, y)] = rgba(bark(x, y, dx, -.1, seed)); wood.add((x, y))
    for x in range(int(cx - 5), int(cx + 5)):          # corte de arriba con anillos
        y = int(base - 9 - 2 * math.sin(x * 1.7 + seed)) + 1
        if (x, y) in wood: cv.px[(x, y)] = rgba(CH[3] if x % 2 else CH[2])
    outline_where(cv, wood)
    mound(cv, cx + 1, h - 2, w / 2 - 1, 7, seed + 5, skew=-.2)
    return cv

def dead_fern(seed, w=26, h=20):
    """Helecho carbonizado: 3-4 frondas curvas con pínnulas cortas, ceniza en las puntas, en un montoncito de arena."""
    cv = Canvas(w, h); rnd = random.Random(seed); bx, by = w / 2, h - 4
    for k in range(rnd.randint(3, 4)):
        ang = math.radians(-90 + rnd.uniform(-60, 60)); L = rnd.uniform(11, 16); bend = rnd.uniform(.05, .12) * (1 if ang > -math.pi / 2 else -1)
        x, y = bx + rnd.uniform(-2, 2), by
        for s in range(int(L)):
            ang += bend; x += math.cos(ang); y += math.sin(ang)
            tip = s > L - 3
            cv.put(x, y, rgba(ASH[2] if tip else ASH[0] if s % 3 == 0 else CH[3]))
            if 1 < s < L - 2 and s % 2 == 0:            # pínnulas que cuelgan
                for side in (-1, 1):
                    px_, py_ = x + math.cos(ang + side * 1.3) * 2, y + math.sin(ang + side * 1.3) * 2 + 1
                    cv.put(px_, py_, rgba(CH[2] if side > 0 else ASH[0]))
    mound(cv, bx, h - 2, 9, 3.5, seed + 9)
    return cv

def charcoal(seed, w=14, h=10):
    cv = Canvas(w, h); rnd = random.Random(seed); wood = set()
    for k in range(rnd.randint(2, 3)):
        cx, cy, rx, ry = rnd.uniform(4, 10), rnd.uniform(4, 6), rnd.uniform(2, 3.5), rnd.uniform(1.4, 2.2)
        for y in range(h):
            for x in range(w):
                if ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 <= 1:
                    cv.px[(x, y)] = rgba(CH[3] if (x - cx) + (y - cy) < -1.5 else CH[2] if y < cy else CH[1]); wood.add((x, y))
    outline_where(cv, wood)
    mound(cv, w / 2, h - 2, w / 2, 3, seed + 2)
    return cv

def pad(cv, m=2):
    """Margen transparente alrededor (nada toca el borde de la celda); el pivote abajo-centro no cambia de sitio."""
    out = Canvas(cv.w + 2 * m, cv.h + 2 * m)
    for (x, y), c in cv.px.items(): out.px[(x + m, y + m)] = c
    return out

def build_a():
    return {k: (v if k.startswith('Deco_BuriedLog') or k.startswith('Obstacle') else pad(v)) for k, v in _build_a().items()}

def _build_a():
    return {'Deco_BuriedLog_0': buried_log(56, 24, 3, humps=2),
            'Deco_BuriedLog_1': buried_log(44, 20, 8, humps=1, cut_left=False),
            'Deco_BuriedLog_2': buried_log(36, 18, 13, humps=2),
            'Obstacle_BuriedTrunk': buried_trunk(),
            'Deco_BuriedStump_0': buried_stump(2), 'Deco_BuriedStump_1': buried_stump(5, 26, 20),
            'Deco_DeadFern_0': dead_fern(1), 'Deco_DeadFern_1': dead_fern(4, 24, 18), 'Deco_DeadFern_2': dead_fern(9, 22, 16),
            'Deco_Charcoal_0': charcoal(1), 'Deco_Charcoal_1': charcoal(6), 'Deco_Charcoal_2': charcoal(11, 12, 9)}


# =================================================================================================
# Subtanda B: rocas y lajas de arenisca (mismos estratos que el acantilado del tileset)
# =================================================================================================
STRATA = [(ROCK, 4), (PALE, 2), (ROCK, 3), (ROCK, 4), (PALE, 1), (ROCK, 3), (ROCK, 2)]
STRATA_H = [4, 3, 4, 3, 3, 4, 5]

def strata_at(x, y, y0, seed):
    """Banda de estrato en la fila y (y0 = arriba de la cara), con una ondulación leve."""
    yy = y - y0 + .8 * math.sin(x * .35 + seed); acc = 0
    for i, hgt in enumerate(STRATA_H * 3):
        acc += hgt
        if yy < acc: return STRATA[i % len(STRATA)], acc - yy < 1.1, yy - (acc - hgt) < 1
    return STRATA[-1], False, False

def boulder(seed, w=40, h=40, cap=9, width=15, height=24):
    """Peñasco: tapa plana con luz arriba y cara con estratos; el viento lo gasta por abajo (más estrecho al pie)."""
    cv = Canvas(w, h); rnd = random.Random(seed); nz = noise(seed)
    cx = w / 2; base = h - 5; ytop = base - height
    rock = set()
    def half(y):
        t = (y - ytop) / height
        (r, k), _, _ = strata_at(cx, y, ytop + cap // 2, seed)
        hard = 1.2 if (r is ROCK and k >= 4) else -1.0 if r is PALE else 0      # los blandos se gastan más
        rnd_top = max(0, 3 - (y - ytop - cap // 2)) * .9                       # arriba, esquinas redondeadas
        return width * (1 - .3 * max(0, t - .5) / .5) + hard + 2.6 * (nz(0, y, 5) - .5) - rnd_top
    for y in range(ytop - cap // 2, base + 1):
        for x in range(w):
            dx = x + .5 - cx
            if y < ytop + cap // 2:                       # tapa (elipse vista desde arriba)
                if (dx / (width - 1 + 3.5 * (nz(x, 0, 4) - .5))) ** 2 + ((y + .5 - ytop) / (cap / 2)) ** 2 > 1: continue
                l = -(dx / width) - (y - ytop) / cap * .6
                c = ROCK[5] if l > .45 else ROCK[4] if l > -.2 else ROCK[3]
                if hsh(x, y, seed) % 9 == 0: c = PALE[3]
            else:
                if abs(dx) > half(y): continue
                (r, k), top, bot = strata_at(x, y, ytop + cap // 2, seed)
                i = k + (1 if top and r is PALE else 0) - (1 if bot else 0)
                u = dx / half(y)
                i += 1 if u < -.55 else -1 if u > .45 else 0
                i -= 1 if (y - ytop) / height > .8 else 0
                if hsh(x, y, seed) % 13 == 0: i -= 1
                c = r[cl(i, len(r) - 1)]
            cv.px[(x, y)] = rgba(c); rock.add((x, y))
    for k in range(3):                                  # agujeritos de la arenisca
        x, y = int(cx + rnd.uniform(-width * .6, width * .6)), rnd.randrange(ytop + cap // 2 + 3, base - 4)
        if (x, y) in rock and (x + 1, y + 1) in rock:
            cv.px[(x, y)] = rgba(ROCK[0]); cv.px[(x + 1, y)] = rgba(ROCK[1]); cv.px[(x, y + 1)] = rgba(ROCK[5])
    x = int(cx + rnd.uniform(-3, 4))                    # grieta de la tapa al pie
    for y in range(ytop - 1, base - 3):
        if (x, y) in rock: cv.px[(x, y)] = rgba(ROCK[0])
        if (x - 1, y) in rock and y % 3: cv.px[(x - 1, y)] = rgba(ROCK[4])
        if rnd.random() < .25: x += rnd.choice((-1, 1))
    outline_where(cv, rock)
    for x in range(w):                                  # sombra de contacto a la derecha y abajo
        for y in range(base - 1, base + 3):
            if (x, y) not in cv.px and ((x + .5 - cx - 3) / (width + 2)) ** 2 + ((y + .5 - base - .5) / 2.5) ** 2 <= 1:
                cv.px[(x, y)] = (24, 14, 12, 120)
    mound(cv, cx - width * .55, base + 2, width * .8, 6, seed + 4, skew=-.3)   # arena contra el lado del viento
    mound(cv, cx + width * .6, base + 2, width * .5, 3, seed + 5)
    return cv

def poly(cx, cy, rx, ry, n, rnd, jit=.25):
    pts = []
    for k in range(n):
        a = 2 * math.pi * (k + rnd.uniform(-.25, .25)) / n
        f = 1 + rnd.uniform(-jit, jit)
        pts.append((cx + math.cos(a) * rx * f, cy + math.sin(a) * ry * f))
    return pts

def inside(pts, x, y):
    c = False; n = len(pts)
    for k in range(n):
        (x1, y1), (x2, y2) = pts[k], pts[(k + 1) % n]
        if (y1 > y) != (y2 > y) and x < (x2 - x1) * (y - y1) / (y2 - y1) + x1: c = not c
    return c

def slab(seed, w=30, h=16, pieces=2):
    """Laja: placa de arenisca partida y tumbada a ras de suelo (cara de arriba, canto de 2 px, grietas)."""
    cv = Canvas(w, h); rnd = random.Random(seed); rock = set()
    pts = poly(w / 2, h / 2 - 1.5, w / 2 - 3, h / 2 - 4, 7, rnd, .18)
    cuts = [(rnd.uniform(.35, .65) * w, rnd.uniform(-.6, .6)) for _ in range(pieces - 1)]
    for y in range(h):
        for x in range(w):
            if not inside(pts, x + .5, y + .5): continue
            if any(abs((x + .5) - (cxx + sl * (y - h / 2))) < .9 for cxx, sl in cuts): continue
            l = -(x - w / 2) / w - (y - h / 2) / h
            c = ROCK[5] if l > .3 else ROCK[4] if l > -.15 else ROCK[3]
            if hsh(x, y, seed) % 9 == 0: c = PALE[2]
            cv.px[(x, y)] = rgba(c); rock.add((x, y))
    for (x, y) in rock:                                 # filo con luz arriba-izquierda
        if (x, y - 1) not in rock and (x - 1, y) not in rock: cv.px[(x, y)] = rgba(PALE[3])
    for (x, y) in list(rock):                           # canto: 2 filas por debajo
        for d in (1, 2):
            q = (x, y + d)
            if q not in rock and q not in cv.px: cv.px[q] = rgba(ROCK[2] if d == 1 else ROCK[1])
    outline_where(cv, list(cv.px))
    mound(cv, w * .7, h - 3, w * .25, 3, seed + 2)
    return cv

def stone(seed, w=16, h=14):
    """Piedra mediana (se pisa): facetas (arriba clara, derecha a la sombra) y sombra al pie."""
    cv = Canvas(w, h); rnd = random.Random(seed); rock = set(); cx, cy = w / 2, h / 2 - .5
    pts = poly(cx, cy, w / 2 - 2.5, h / 2 - 3, 6, rnd, .2)
    ridge = cy - rnd.uniform(0, 1.5)
    for y in range(h):
        for x in range(w):
            if not inside(pts, x + .5, y + .5): continue
            if y + .5 < ridge: c = ROCK[5] if x < cx else ROCK[4]               # cara de arriba
            else: c = ROCK[3] if x < cx - 1 else ROCK[2] if x < cx + 2 else ROCK[1]
            if abs(y + .5 - ridge - 2) < .6 and rnd.random() < .7 and seed % 2: c = PALE[1]   # algún estrato
            cv.px[(x, y)] = rgba(c); rock.add((x, y))
    outline_where(cv, rock)
    yb = max(y for _, y in rock) + 2
    for x in range(w):
        if (x, yb) not in cv.px and any((x - 1, yy) in rock for yy in range(yb - 3, yb)): cv.px[(x, yb)] = (24, 14, 12, 110)
    return cv

def gravel(seed, w=18, h=12):
    cv = Canvas(w, h); rnd = random.Random(seed)
    for k in range(rnd.randint(4, 7)):
        S.pebble(cv, rnd.randrange(2, w - 5), rnd.randrange(2, h - 4), big=(k == 0))
    return cv

def build_b():
    return {'Obstacle_SandRock_0': boulder(3), 'Obstacle_SandRock_1': boulder(11, 36, 36, 8, 12, 19),
            'Deco_Slab_0': slab(2), 'Deco_Slab_1': slab(7, 24, 14, 2), 'Deco_Slab_2': slab(12, 34, 18, 3),
            'Deco_Stone_0': stone(1), 'Deco_Stone_1': stone(4, 14, 12), 'Deco_Stone_2': stone(9, 18, 14),
            'Deco_Gravel_0': gravel(1), 'Deco_Gravel_1': gravel(5, 16, 10), 'Deco_Gravel_2': gravel(8, 20, 12)}


# =================================================================================================
# Subtanda C: plantas pérmicas secas (Sway 4 × 220 ms en bucle; Rustle 6 × 70 ms al pasar una ráfaga)
# =================================================================================================
OLIVE = [C(c) for c in ('#1a2014', '#28301c', '#3a4426', '#525a32', '#6e7242')]   # agujas de la Walchia, apagadas
STEM = [C(c) for c in ('#1e140e', '#2e2016', '#44301e')]
CALD = [C(c) for c in ('#222a18', '#343e22', '#4a542c', '#66683a', '#868250')]   # Calamites reseco (de ramp #6a9a3a, apagado)
SWAY = [0, .8, 0, -.8]
RUSTLE = [3.2, 2.0, -1.2, .9, -.4, .1]

def walchia(bend, seed, W=26, H=32, tiers=5, grains=0):
    """Walchia joven: conífera primitiva con ramas en pisos, agujas cortas; puntas secas."""
    cv = Canvas(W, H); rnd = random.Random(seed); base = H - 4; cx = W / 2; top = 3
    sx = lambda y: cx + bend * ((base - y) / (base - top)) ** 1.6
    for y in range(top + 2, base + 1):                 # tronco
        cv.put(sx(y), y, rgba(STEM[2]));
        if y > base - 8: cv.put(sx(y) + 1, y, rgba(STEM[0]))
    for k in range(tiers):
        t = k / (tiers - 1)
        y0 = base - 5 - t * (base - 5 - top - 3)
        for side in (-1, 1):
            if 0 < k < tiers - 1 and rnd.random() < .18: continue          # rama que falta
            L = (W / 2 - 2) * (1 - .6 * t) * rnd.uniform(.55, 1.05)
            dead = rnd.random() < .2                                         # rama seca entera
            yk = y0 + rnd.choice((-1, 0, 0, 1))
            for st in range(int(L) + 1):
                x = sx(yk) + side * st + bend * .25 * (st / max(L, 1)) * (1 - t)
                y = yk + st * st * .06 - st * .2
                tip = st > L - 2.5
                dry = dead or (tip and hsh(k, side + 3, seed) % 3 != 0)
                cv.put(x, y, rgba(STEM[1] if st < 2 or dead else OLIVE[2]))
                if st >= 1:                             # agujas: arriba con luz, abajo a la sombra
                    up = DRY[2] if dry else OLIVE[4] if side < 0 else OLIVE[3]
                    dn = DRY[1] if dry else OLIVE[1]
                    if (st + k) % 2 == 0 or st > 2: cv.put(x, y - 1, rgba(up))
                    cv.put(x, y + 1, rgba(dn))
    for d in range(3):                                  # brote de la punta
        cv.put(sx(top + d), top + d, rgba(OLIVE[4] if d < 2 else OLIVE[3]))
    for g in range(grains):                             # arena que se lleva la ráfaga
        cv.put(cx + 4 + g * 3 + bend, base - 10 + (g * 5) % 9, rgba(SND[4]))
    mound(cv, cx, H - 2, 6, 2.5, seed + 3)
    return cv

def tuft(bend, seed, W=20, H=16, n=9, grains=0):
    """Mata reseca de tallos duros en abanico, algunas con espiguitas."""
    cv = Canvas(W, H); rnd = random.Random(seed); bx, by = W / 2, H - 3
    for k in range(n):
        a = math.radians(-160 + 140 * (k + rnd.uniform(-.3, .3)) / (n - 1))
        L = rnd.uniform(H * .45, H * .8) * (1 - .35 * abs(math.cos(a)))
        for st in range(int(L)):
            t = st / L
            x = bx + math.cos(a) * st + bend * t ** 1.5
            y = by + math.sin(a) * st
            cv.put(x, y, rgba(DRY[3] if t > .75 else DRY[2] if (k % 3) else DRY[1]))
        if k % 4 == 1: cv.put(x, y - 1, rgba(DRY[3])); cv.put(x + 1, y - 1, rgba(SANDLIT[0]))
    for g in range(grains): cv.put(bx + 3 + g * 3 + bend, by - 5 + g * 2, rgba(SND[4]))
    mound(cv, bx, H - 2, W / 2 - 3, 2, seed + 2)
    return cv

def calamites_dry(bend, seed, W=20, H=34, grains=0):
    """Cola de caballo superviviente: 3 cañas con nudos y verticilos de hojitas caídas; una partida."""
    cv = Canvas(W, H); rnd = random.Random(seed); base = H - 4
    for k, (x0, hgt, broken) in enumerate(((W / 2 - 3, H - 10, False), (W / 2 + 2, H - 15, False), (W / 2 + 5, H - 22, True))):
        for st in range(int(hgt)):
            y = base - st; t = st / hgt
            x = x0 + bend * t ** 1.5 * (1 - .2 * k)
            node = (st + 2 * k + 1) % 5 == 4
            cv.put(x, y, rgba(CALD[1] if node else CALD[3] if k == 0 else CALD[2]))
            if k < 2: cv.put(x + 1, y, rgba(CALD[0] if node else CALD[1]))
            if node and st < hgt - 2 and hsh(k, st, seed) % 3:   # verticilo: hojitas cortas que cuelgan
                for side in (-1, 1):
                    for d in (1, 2):
                        cv.put(x + side * d + (1 if side > 0 and k < 2 else 0) + bend * .15, y + d, rgba(CALD[4] if d == 2 and side < 0 else CALD[3] if side < 0 else CALD[2]))
        if broken: cv.put(x - 1, y - 1, rgba(DRY[2])); cv.put(x, y - 1, rgba(DRY[3]))
        else:                                           # estróbilo seco en la punta
            for d in range(3): cv.put(x, y - 1 - d, rgba(DRY[2] if d else DRY[1])); cv.put(x + 1, y - 1 - d, rgba(DRY[1]))
    for g in range(grains): cv.put(W / 2 + 4 + g * 3 + bend, base - 12 + g * 4, rgba(SND[4]))
    mound(cv, W / 2 + 1, H - 2, 7, 3, seed + 4)
    return cv

def plant_sheets(fn, **kw):
    return {'Sway': [pad(fn(b, **kw)) for b in SWAY],
            'Rustle': [pad(fn(b, grains=max(0, 3 - i), **kw)) for i, b in enumerate(RUSTLE)]}

def build_c():
    return {'Plant_Walchia_0': plant_sheets(walchia, seed=3),
            'Plant_Walchia_1': plant_sheets(walchia, seed=8, W=22, H=26, tiers=4),
            'Plant_DryTuft_0': plant_sheets(tuft, seed=1),
            'Plant_DryTuft_1': plant_sheets(tuft, seed=5, W=24, H=18, n=11),
            'Plant_DryTuft_2': plant_sheets(tuft, seed=9, W=16, H=13, n=7),
            'Plant_DryCalamites': plant_sheets(calamites_dry, seed=2)}


# =================================================================================================
# Subtanda D: huellas fósiles de un sinápsido (tipo Dimetrodon), grietas de barro seco y costra de sal
# =================================================================================================
CRUST = [C(c) for c in ('#3a2620', '#4e3428', '#644434', '#7a5640')]          # arena endurecida (fósil)
DEEP = [C(c) for c in ('#160b09', '#21120e')]                                   # fondo de la pisada
SALT = [C(c) for c in ('#4a3a34', '#5c4c44', '#6e5e54', '#827266')]           # sal, apagada

def foot_pts(hind):
    """Huella mirando a la derecha (+x), pie IZQUIERDO (dedos hacia arriba-derecha): planta + 5 dedos con garra."""
    pts = []
    pw, ph = (3.2, 2.6) if hind else (2.4, 2.0)
    for y in range(-6, 7):
        for x in range(-6, 7):
            if (x / pw) ** 2 + (y / ph) ** 2 <= 1: pts.append((x, y, 'sole'))
    spread = [-62, -30, 0, 28, 55] if hind else [-58, -28, 0, 26, 50]
    lens = [2.5, 3.5, 4.2, 3.8, 2.6] if hind else [2, 3, 3.4, 3, 2]
    for a, L in zip(spread, lens):
        a = math.radians(a - 8)
        for st in range(1, int(L + pw) + 1):
            if st <= pw - .5: continue
            pts.append((round(math.cos(a) * st), round(math.sin(a) * st), 'toe'))
        pts.append((round(math.cos(a) * (L + pw + .8)), round(math.sin(a) * (L + pw + .8)), 'claw'))
    return pts

def stamp(pts, w, h, rot45=False):
    """Hoyuelo en arena endurecida: dedos y garras lisos y hondos; la planta, con su pared a la sombra arriba-izq y
    el reborde de abajo-der con luz."""
    cv = Canvas(w, h); cx, cy = w // 2, h // 2
    q = {}
    for (x, y, kind) in pts:
        if rot45: x, y = round((x - y) * .7071), round((x + y) * .7071)
        p_ = (cx + x, cy + y)
        if q.get(p_) != 'sole': q[p_] = kind
    if rot45:                                          # al girar quedan huecos de 1 px: se rellenan
        for y in range(h):
            for x in range(w):
                if (x, y) not in q and q.get((x - 1, y)) == 'sole' and q.get((x + 1, y)) == 'sole': q[(x, y)] = 'sole'
    for (x, y), kind in q.items():
        if kind != 'sole': cv.px[(x, y)] = rgba(DEEP[0]); continue
        ul = (x - 1, y) not in q or (x, y - 1) not in q
        dr = (x + 1, y) not in q or (x, y + 1) not in q
        cv.px[(x, y)] = rgba(DEEP[0] if ul else CRUST[1] if dr else DEEP[1])
    for (x, y), kind in list(q.items()):               # reborde con luz solo bajo la planta
        if kind != 'sole': continue
        for dx, dy in ((1, 0), (0, 1)):
            r_ = (x + dx, y + dy)
            if r_ not in q and r_ not in cv.px: cv.px[r_] = rgba(CRUST[3], 220)
    return cv

def tail_drag(rot45=False, n=16):
    """Surco de la cola: 16 px de largo, ondula; encaja consigo mismo de punta a punta."""
    cv = Canvas(20, 20); q = []
    for st in range(n):
        x, y = st - n / 2 + .5, 1.2 * math.sin(2 * math.pi * st / n)
        if rot45: x, y = (x - y) * .7071, (x + y) * .7071
        q.append((round(10 + x), round(10 + y)))
    for (x, y) in q: cv.px[(x, y)] = rgba(DEEP[1])
    for (x, y) in q:
        if (x, y + 1) not in cv.px: cv.px[(x, y + 1)] = rgba(CRUST[2], 220)
        if (x, y - 1) not in cv.px: cv.px[(x, y - 1)] = rgba(CRUST[3] if (x % 3) else CRUST[1], 200)
    return cv

def mud_cracks(seed, w=36, h=22):
    """Barro seco: placas que se abarquillan, grietas oscuras; borde que se deshace en el suelo."""
    cv = Canvas(w, h); rnd = random.Random(seed); nz = noise(seed)
    cells = [(rnd.uniform(0, w), rnd.uniform(0, h)) for _ in range(int(w * h / 70))]
    for y in range(h):
        for x in range(w):
            dx, dy = (x + .5 - w / 2) / (w / 2 - 1), (y + .5 - h / 2) / (h / 2 - 1)
            d = dx * dx + dy * dy + (nz(x, y, 4) - .5) * .7
            if d > 1 or (d > .8 and (x + y) % 2): continue
            ds = sorted((math.hypot(x - a, (y - b) * 1.4), i) for i, (a, b) in enumerate(cells))
            gap = ds[1][0] - ds[0][0]
            if gap < 1.1: c = SND[0]                                       # grieta
            elif gap < 2.2: c = CRUST[3] if (x - cells[ds[0][1]][0]) + (y - cells[ds[0][1]][1]) < 0 else CRUST[1]   # filo abarquillado
            else: c = CRUST[2] if hsh(x, y, seed) % 7 else CRUST[1]
            cv.px[(x, y)] = rgba(c)
    return cv

def salt_crust(seed, w=30, h=18):
    cv = Canvas(w, h); nz = noise(seed); nz2 = noise(seed + 1)
    for y in range(h):
        for x in range(w):
            dx, dy = (x + .5 - w / 2) / (w / 2 - 1), (y + .5 - h / 2) / (h / 2 - 1)
            d = dx * dx + dy * dy + (nz(x, y, 4) - .5) * .9
            if d > 1: continue
            v = nz2(x, y, 3)
            if d > .6 and (x + y) % 2: continue
            if v < .35 and d > .3: continue                                 # a manchas
            c = SALT[3] if v > .75 and d < .5 else SALT[2] if v > .55 else SALT[1] if d < .7 else SALT[0]
            cv.px[(x, y)] = rgba(c, 235)
    return cv

def build_d():
    hp, fp = foot_pts(True), foot_pts(False)
    return {'Deco_Track_Hind_E': stamp(hp, 18, 18), 'Deco_Track_Fore_E': stamp(fp, 14, 14),
            'Deco_Track_Hind_NE': stamp(hp, 18, 18, True), 'Deco_Track_Fore_NE': stamp(fp, 14, 14, True),
            'Deco_TailDrag_E': tail_drag(), 'Deco_TailDrag_NE': tail_drag(True),
            'Deco_MudCracks_0': mud_cracks(2), 'Deco_MudCracks_1': mud_cracks(6, 28, 18), 'Deco_MudCracks_2': mud_cracks(11, 44, 26),
            'Deco_SaltCrust_0': salt_crust(3), 'Deco_SaltCrust_1': salt_crust(8, 22, 14)}

def trail(im, D, p0, p1, step=26, side=6):
    """Rastro de muestra (lo que hará el código): pares trasera/delantera alternando lado, cola por el centro."""
    (x0, y0), (x1, y1) = p0, p1
    L = math.hypot(x1 - x0, y1 - y0); ux, uy = (x1 - x0) / L, (y1 - y0) / L; nx, ny = -uy, ux
    def put(spr, x, y): im.alpha_composite(spr, (int(x - spr.width / 2), int(y - spr.height / 2)))
    for k in range(int(L / 16)):                        # la cola primero (debajo)
        put(D['Deco_TailDrag_E'].img(), x0 + ux * (8 + k * 16) - nx * 1, y0 + uy * (8 + k * 16) - ny * 1)
    k = 0; d = 6
    while d < L - 4:
        left = k % 2 == 0; sg = -1 if left else 1       # izquierda = arriba en el sentido de la marcha
        for kind, off, sd in (('Hind', 0, side), ('Fore', 14, side - 2)):   # la delantera, por delante y más cerca del centro
            spr = D[f'Deco_Track_{kind}_E'].img()
            if not left: spr = spr.transpose(Image.FLIP_TOP_BOTTOM)
            put(spr, x0 + ux * (d + off) + nx * sg * sd, y0 + uy * (d + off) + ny * sg * sd)
        d += step; k += 1

# =================================================================================================
# Sala de muestra con la decoración
# =================================================================================================
def load(path):
    return Image.open(os.path.join(ENV, path)).convert('RGBA')

def frame0(im, w):
    return im.crop((0, 0, w, im.height))

def room_with(pieces):
    bb = gt.build('Burnt'); builds = {'Burnt': dict(tiles=bb['tiles'], spills=bb['spills'])}
    builds['BurntSand'] = build_burntsand(); builds['Permian'] = build_permian()
    S.PIT = [sumidero(k) for k in range(FRAMES)]
    blends = blend_sets(builds); faces_t = transition_faces(builds)
    decals = [sand_decal(48, 32, 5), sand_decal(32, 24, 9), sand_decal(40, 20, 14)]
    _, im = sample_room(builds, blends, faces_t, decals)
    def at(spr, x, y):                                 # pivote: abajo en el centro, en píxeles de la sala
        im.alpha_composite(spr, (int(x - spr.width / 2), int(y - spr.height)))
    P = {k: v.img() for k, v in pieces.items()}
    # franja quemada: lo que ya existe
    at(load('Burnt/props/Obstacle_BrokenTrunk.png'), 5 * 16, 7 * 16 + 4)
    at(load('Burnt/props/Obstacle_FallenLog.png'), 5 * 16, 12 * 16 + 6)
    at(load('Burnt/props/Obstacle_Stump.png'), 12 * 16, 11 * 16 + 4)
    at(frame0(load('Smoke/props/Fern_Leafy_Burnt.png'), 84), 11 * 16, 7 * 16 + 4)
    # franja intermedia: restos que la arena entierra
    at(P['Obstacle_BuriedTrunk'], 19 * 16, 7 * 16 + 8)
    at(P['Deco_BuriedLog_0'], 16 * 16, 12 * 16 + 8)
    at(P['Deco_BuriedLog_1'], 23 * 16, 10 * 16 + 4)
    at(P['Deco_BuriedStump_0'], 14 * 16, 9 * 16)
    at(P['Deco_DeadFern_0'], 21 * 16, 12 * 16 + 4)
    at(P['Deco_DeadFern_1'], 24 * 16, 6 * 16 + 8)
    at(P['Deco_Charcoal_0'], 17 * 16, 9 * 16 + 10)
    at(P['Deco_Charcoal_1'], 25 * 16, 12 * 16 + 10)
    # franja pérmica: los últimos restos, casi tapados
    at(P['Deco_BuriedLog_2'], 28 * 16, 5 * 16 + 14)
    at(P['Deco_BuriedStump_1'], 28 * 16, 12 * 16 + 8)
    at(P['Deco_DeadFern_2'], 33 * 16, 5 * 16 + 12)
    at(P['Deco_Charcoal_2'], 30 * 16, 12 * 16 + 12)
    if 'Obstacle_SandRock_0' in P:                     # subtanda B: rocas en la franja pérmica
        at(P['Obstacle_SandRock_0'], 35 * 16, 7 * 16 + 2)
        at(P['Obstacle_SandRock_1'], 27 * 16 + 8, 10 * 16 + 4)
        at(P['Deco_Slab_0'], 33 * 16, 13 * 16 + 2)
        at(P['Deco_Slab_2'], 31 * 16, 5 * 16 + 14)
        at(P['Deco_Slab_1'], 24 * 16, 8 * 16 + 8)
        at(P['Deco_Stone_0'], 29 * 16 + 4, 7 * 16 + 4)
        at(P['Deco_Stone_1'], 37 * 16, 12 * 16 + 6)
        at(P['Deco_Stone_2'], 26 * 16, 12 * 16 + 10)
        at(P['Deco_Gravel_0'], 33 * 16 + 4, 7 * 16 + 8)
        at(P['Deco_Gravel_1'], 29 * 16, 10 * 16 + 12)
        at(P['Deco_Gravel_2'], 36 * 16, 10 * 16 + 4)
    if 'Deco_Track_Hind_E' in P:                       # subtanda D (en el juego van al nivel del suelo, debajo de todo)
        trail(im, DD, (25 * 16, 6 * 16 + 8), (30 * 16 + 12, 8 * 16 + 4))
        at(P['Deco_MudCracks_0'], 24 * 16, 11 * 16 + 4)
        at(P['Deco_MudCracks_2'], 22 * 16, 9 * 16 + 6)
        at(P['Deco_SaltCrust_0'], 30 * 16, 5 * 16 + 4)
        at(P['Deco_SaltCrust_1'], 37 * 16, 9 * 16)
    if 'Plant_Walchia_0' in P:                         # subtanda C: plantas a la sombra de las rocas y del acantilado
        at(P['Plant_Walchia_0'], 37 * 16 + 4, 5 * 16 + 10)
        at(P['Plant_Walchia_1'], 26 * 16 + 4, 10 * 16 + 4)
        at(P['Plant_DryCalamites'], 33 * 16 + 2, 5 * 16 + 8)
        at(P['Plant_DryTuft_0'], 29 * 16 + 6, 10 * 16 + 6)
        at(P['Plant_DryTuft_1'], 36 * 16 + 12, 7 * 16 + 6)
        at(P['Plant_DryTuft_2'], 27 * 16 + 6, 5 * 16 + 10)
        at(P['Plant_DryTuft_0'], 37 * 16 + 4, 12 * 16 + 12)
        at(P['Plant_DryTuft_2'], 22 * 16, 12 * 16 + 10)
    return im



# =================================================================================================
# Subtanda E: ráfagas de arena (velo bajo y translúcido que el código mueve de izquierda a derecha)
# =================================================================================================
GUST = [C(c) for c in ('#6c402b', '#865236', '#a06a40', '#c08a58', '#e0b080')]   # polvo en el aire, con luz

def gust(k, n=6, w=64, h=24, seed=1):
    """Velo de arena: rachas horizontales onduladas, más densas abajo, cabeza a la derecha con granos sueltos y cola que
    se deshace a la izquierda; las rachas corren hacia la derecha dentro del velo (bucle de n cuadros)."""
    cv = Canvas(w, h); rnd = random.Random(seed)
    streaks = [(rnd.uniform(4, h - 3), rnd.uniform(0, w), rnd.uniform(10, 26), rnd.uniform(0, 6.28)) for _ in range(int(w / 5))]
    for y in range(h):                                   # calima: cuerpo tenue del velo, tramado, más denso abajo
        for x in range(w):
            u, v = x / w, (y + .5) / h
            env = min(1, u * 2.5) * min(1, (1 - u) * 4) * max(0, 1 - ((v - .7) / .45) ** 2)
            ph = (x - k * w / n) * .2
            if env * (.6 + .4 * math.sin(ph)) > (BAYER[y % 4][(x + k) % 4] + .5) / 16 + .25:
                cv.put(x, y, rgba(GUST[1], 60))
    for (y0, x0, L, ph) in streaks:
        v = (y0 / h)                                     # abajo, más densa y más clara
        for st in range(int(L)):
            x = (x0 + st + k * w / n) % (w + 20) - 10
            y = y0 + 1.2 * math.sin(x * .18 + ph)
            if not (0 <= x < w): continue
            u = x / w                                    # 0 = cola, 1 = cabeza
            env = min(1, u * 3) * min(1, (1 - u) * 5) * (.45 + .55 * v)   # se apaga en la cola y en la punta
            if env < .15 or (st % 3 == 0 and env < .5): continue
            c = GUST[3] if st > L - 3 else GUST[2] if v > .55 else GUST[1]
            cv.put(x, y, rgba(c, int(110 + 120 * env)))
    for g in range(9):                                   # granos sueltos que van delante
        gx = (w * .72 + g * 3.3 + k * 2.2) % w
        if gx < w * .6: gx += w * .3
        gy = h - 4 - (g * 7) % (h - 8)
        cv.put(gx, gy, rgba(GUST[4] if g % 3 == 0 else GUST[3], 200))
    return cv

def build_e():
    return {'Fx_SandGust': [gust(k) for k in range(6)],
            'Fx_SandGust_Small': [gust(k, w=44, h=16, seed=4) for k in range(6)]}

# =================================================================================================
# Sala amplia de muestra (60×26 casillas): las tres franjas a tamaño real, todo ordenado por altura
# =================================================================================================
BIG_ROOM = ["#" * 60] * 4 + [
    "##" + "." * 14 + "###" + "." * 39 + "##" if y < 7 else
    "##" + "." * 8 + "###" + "." * 45 + "##" if 14 <= y < 17 else
    "##" + "." * 28 + "###" + "." * 25 + "##" if 18 <= y < 21 else
    "##" + "." * 56 + "##" for y in range(4, 23)] + ["#" * 60] * 3
BIG_PIT = (47, 11)

def big_room(P, DD, Cs, plants=True):
    S.ROOM = BIG_ROOM; S.PIT_AT = BIG_PIT
    S.b1 = lambda y: 20 + round(1.8 * math.sin(y * 1.1 + .5) + .8 * math.sin(y * 2.3))
    S.b2 = lambda y: 39 + round(2.0 * math.sin(y * .9 + 2) + .9 * math.sin(y * 2.1 + 1))
    bb = gt.build('Burnt'); builds = {'Burnt': dict(tiles=bb['tiles'], spills=bb['spills'])}
    builds['BurntSand'] = build_burntsand(); builds['Permian'] = build_permian()
    S.PIT = [sumidero(k) for k in range(FRAMES)]
    blends = blend_sets(builds); faces_t = transition_faces(builds)
    decals = [sand_decal(48, 32, 5), sand_decal(32, 24, 9), sand_decal(40, 20, 14)]
    _, im = sample_room(builds, blends, faces_t, decals)
    T = lambda tx, ty, dx=0, dy=0: (int(tx * 16 + dx), int(ty * 16 + dy))
    def flat(spr, xy): im.alpha_composite(spr, (int(xy[0] - spr.width / 2), int(xy[1] - spr.height / 2)))
    # calcomanías del suelo (debajo de todo)
    flat(DD['Deco_MudCracks_2'].img(), T(30, 16)); flat(DD['Deco_MudCracks_0'].img(), T(41, 13, 8))
    flat(DD['Deco_MudCracks_1'].img(), T(51, 6, 8)); flat(DD['Deco_SaltCrust_0'].img(), T(55, 11, 8))
    flat(DD['Deco_SaltCrust_1'].img(), T(43, 21))
    trail(im, DD, T(39, 9, 8), T(47, 13, 4))
    # objetos con pivote abajo-centro, ordenados por altura (como en el juego)
    obj = []
    def at(spr, xy): obj.append((xy[1], spr, xy))
    burnt = lambda n: load(f'Burnt/props/{n}.png')
    at(burnt('Obstacle_BrokenTrunk'), T(6, 9)); at(burnt('Obstacle_FallenLog'), T(5, 20, 0, 6))
    at(burnt('Obstacle_Stump'), T(15, 11)); at(burnt('Obstacle_Stump'), T(4, 14, 8))
    at(frame0(load('Smoke/props/Fern_Leafy_Burnt.png'), 84), T(10, 7, 0, 4))
    at(frame0(load('Smoke/props/Fern_Bent_Burnt.png'), 88), T(15, 21, 0, 6))
    A = {k: v.img() for k, v in P.items()}
    for n, xy in (('Obstacle_BuriedTrunk', T(25, 10)), ('Deco_BuriedLog_0', T(23, 20, 0, 6)), ('Deco_BuriedLog_1', T(34, 13)),
                  ('Deco_BuriedStump_0', T(21, 14)), ('Deco_BuriedStump_1', T(36, 21, 0, 8)), ('Deco_DeadFern_0', T(28, 22)),
                  ('Deco_DeadFern_1', T(35, 7, 8)), ('Deco_DeadFern_2', T(22, 7, 0, 8)), ('Deco_Charcoal_0', T(27, 15)),
                  ('Deco_Charcoal_1', T(31, 9)), ('Deco_Charcoal_2', T(37, 17)), ('Deco_BuriedLog_2', T(41, 21, 0, 8)),
                  ('Obstacle_SandRock_0', T(55, 9)), ('Obstacle_SandRock_1', T(43, 18)), ('Deco_Slab_0', T(50, 21, 0, 8)),
                  ('Deco_Slab_1', T(42, 8, 0, 4)), ('Deco_Slab_2', T(56, 16, 0, 8)), ('Deco_Stone_0', T(46, 7, 8)),
                  ('Deco_Stone_1', T(54, 19, 8)), ('Deco_Stone_2', T(40, 18)), ('Deco_Gravel_0', T(53, 10, 8)),
                  ('Deco_Gravel_1', T(44, 14, 8)), ('Deco_Gravel_2', T(56, 19, 8))):
        at(A[n], xy)
    global PLANTS
    PLANTS = []
    for n, xy, k in (('Plant_Walchia_0', T(57, 6, 0, 2), 0), ('Plant_Walchia_1', T(41, 18, 8, 2), 2), ('Plant_DryCalamites', T(48, 5, 0, 8), 1),
                     ('Plant_DryTuft_0', T(54, 10, 8), 3), ('Plant_DryTuft_1', T(45, 19, 8), 1), ('Plant_DryTuft_2', T(40, 6, 8, 8), 2),
                     ('Plant_DryTuft_0', T(56, 22, 0, 4), 1), ('Plant_DryTuft_2', T(49, 22), 0), ('Plant_DryTuft_1', T(38, 10), 2)):
        PLANTS.append((n, xy, k))
        if plants: at(Cs[n]['Sway'][k].img(), xy)
    for _, spr, (x, y) in sorted(obj, key=lambda o: o[0]): im.alpha_composite(spr, (int(x - spr.width / 2), int(y - spr.height)))
    return im


def gust_scene(P, DD, Cs, G):
    """Franja pérmica: pasan dos ráfagas (una grande y otra pequeña) a 6 u/s; las plantas que tocan hacen Rustle."""
    base = big_room(P, DD, Cs, plants=False)
    X0, Y0, X1, Y1 = 37 * 16, 3 * 16, 60 * 16, 24 * 16
    gusts = [('Fx_SandGust', 0, 9 * 16), ('Fx_SandGust_Small', 26, 18 * 16 + 4), ('Fx_SandGust', 40, 14 * 16)]   # (hoja, cuadro de salida, y)
    speed = 6 * 16 * .08                                 # 6 u/s a 80 ms por cuadro
    rust = {}
    frames = []
    for t in range(84):
        im = base.copy()
        boxes = []
        for (sh, t0, gy) in gusts:
            if t < t0: continue
            spr = G[sh][(t - t0) % 6].img()
            gx = X0 - spr.width + (t - t0) * speed
            if gx > X1: continue
            boxes.append((gx, gy - spr.height, gx + spr.width, gy))
        for i, (n, (x, y), k) in enumerate(PLANTS):
            if x < X0 - 16: continue
            hit = any(bx0 + 8 < x < bx1 and by0 - 8 < y < by1 + 16 for (bx0, by0, bx1, by1) in boxes)
            if hit and i not in rust: rust[i] = t
            if i in rust and t - rust[i] >= 6 and not hit: del rust[i]
            spr = Cs[n]['Rustle'][min(5, t - rust[i])] if i in rust else Cs[n]['Sway'][(t // 3 + k) % 4]
            spr = spr.img(); im.alpha_composite(spr, (int(x - spr.width / 2), int(y - spr.height)))
        for (sh, t0, gy) in gusts:                       # el velo va por encima (es bajo, pero translúcido)
            if t < t0: continue
            spr = G[sh][(t - t0) % 6].img(); gx = X0 - spr.width + (t - t0) * speed
            if gx <= X1: im.alpha_composite(spr, (int(gx), int(gy - spr.height)))
        frames.append(im.crop((X0, Y0, X1, Y1)))
    big = [f.resize((f.width * 2, f.height * 2), Image.NEAREST).convert('RGB') for f in frames]
    os.makedirs(os.path.join(HERE, 'gif', 'escenas'), exist_ok=True)
    big[0].save(os.path.join(HERE, 'gif', 'escenas', 'rafagas_permica.gif'), save_all=True, append_images=big[1:], duration=80, loop=0)
    keys = [14, 22, 34, 50]
    strip = Image.new('RGB', (len(keys) * (frames[0].width * 2 + 8), frames[0].height * 2), (16, 18, 16))
    for j, kk in enumerate(keys): strip.paste(big[kk], (j * (frames[0].width * 2 + 8), 0))
    strip.save(os.path.join(HERE, 'vista_previa_rafagas.png'))

def main():
    os.makedirs(DECOR, exist_ok=True)
    A = build_a()
    for k, cv in A.items(): cv.img().save(os.path.join(DECOR, k + '.png'))
    preview(os.path.join(HERE, 'vista_previa_decor_A.png'), {k: [v] for k, v in A.items()}, '#3a2a22', scale=4, cols=1)
    Bp = {k: pad(v) for k, v in build_b().items()}
    for k, cv in Bp.items(): cv.img().save(os.path.join(DECOR, k + '.png'))
    preview(os.path.join(HERE, 'vista_previa_decor_B.png'), {k: [v] for k, v in Bp.items()}, '#3d241c', scale=4, cols=1)
    Cs = build_c()
    for k, sh in Cs.items():
        for anim, fr in sh.items(): save_sheet(os.path.join(DECOR, f'{k}_{anim}.png'), fr)
        gif(os.path.join(HERE, 'gif', f'{k}.gif'), sh['Sway'] * 2 + sh['Rustle'] + sh['Sway'], [220] * 8 + [70] * 6 + [220] * 4, '#3d241c', scale=4)
    preview(os.path.join(HERE, 'vista_previa_decor_C.png'), {f'{k} {a}': v for k, sh in Cs.items() for a, v in sh.items()}, '#3d241c', scale=4)
    global DD
    DD = {k: pad(v) for k, v in build_d().items()}
    for k, cv in DD.items(): cv.img().save(os.path.join(DECOR, k + '.png'))
    preview(os.path.join(HERE, 'vista_previa_decor_D.png'), {k: [v] for k, v in DD.items()}, '#3d241c', scale=5, cols=1)
    G = build_e()
    for k, fr in G.items():
        save_sheet(os.path.join(DECOR, k + '.png'), fr)
        gif(os.path.join(HERE, 'gif', k + '.gif'), fr, 80, '#3d241c', scale=4)
    preview(os.path.join(HERE, 'vista_previa_decor_E.png'), G, '#3d241c', scale=4)
    gust_scene({**A, **Bp}, DD, Cs, G)
    im = big_room({**A, **Bp}, DD, Cs)
    im.resize((im.width * 2, im.height * 2), Image.NEAREST).save(os.path.join(HERE, 'vista_previa_sala_decor.png'))
    for name, box in (('quemada', (0, 3, 21, 24)), ('intermedia', (18, 3, 40, 24)), ('permica', (37, 3, 60, 24))):
        c_ = im.crop((box[0] * 16, box[1] * 16, box[2] * 16, box[3] * 16))
        c_.resize((c_.width * 3, c_.height * 3), Image.NEAREST).save(os.path.join(HERE, f'vista_previa_sala_decor_{name}.png'))
    c_ = im.crop((38 * 16, 7 * 16, 49 * 16, 15 * 16))
    c_.resize((c_.width * 5, c_.height * 5), Image.NEAREST).save(os.path.join(HERE, 'vista_previa_sala_decor_rastro.png'))

if __name__ == '__main__':
    main()
