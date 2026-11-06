"""Cápsula de ámbar, propuesta D (sobre la B, con la foto de ámbar de la usuaria como referencia de textura).

Gota alargada hacia arriba, con dientes en los lados que apuntan hacia arriba y una cresta de picos en la punta.
Algo más grande que la rana (holgura alrededor). Textura: borde rojizo y hondo, centro amarillo translúcido,
vetas fibrosas curvas y grietas finas. Dos capas (Back/Front); la criatura va en medio. Celda 46x48, pies en y = 41.
Uso: python generar_capsula_d.py   (necesita generar_capsulas.py en la misma carpeta, solo para la comparación)
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
TEMP = os.path.normpath(os.path.join(HERE, '..'))
sys.path.insert(0, os.path.join(TEMP, 'floor3-environment'))
from base_piso3 import *
from PIL import Image

AMB = [C(c) for c in AMBER]
W, H = 46, 48
CX, TOP, BOT = 23, 2, 44                  # eje, punta y suelo
FROG_AT = (6, 13)                         # en la hoja guardada (recortada 2 filas arriba) los pies quedan en y = 39
SEED = 11

def rgba(c, a=255): return tuple(c[:3]) + (a,)

# --- forma: gota alargada con dientes hacia arriba ----------------------------------------------------
def half_width(y):
    """Gota de agua: bulbo redondo abajo + punta de lados cóncavos arriba."""
    e = 17.5 * math.sqrt(max(0.0, 1 - ((y - 30.5) / 14.5) ** 2)) if y < BOT + .5 else 0
    q = (y - TOP) / (36.0 - TOP)                                            # punta roma: 4 px de ancho y se abre
    t = (3.4 + 14.1 * q ** 1.05) * min(1.0, (y - TOP + 1) / 4.0) if TOP < y < 36 else 0
    return max(e, t)

def build_mask():
    rnd = random.Random(SEED); L, R = {}, {}
    for y in range(TOP, BOT + 1):
        hw = half_width(y + .5)
        if hw <= 0: continue
        p = (y - TOP) / (BOT - TOP)
        for side, d in ((-1, L), (1, R)):
            off = 0.0
            if .16 < p < .93:                                               # pinchos que suben desde el suelo: punta arriba,
                per = 7 + (1 if side < 0 else 0)                             # pendiente larga y suave por debajo, salto seco arriba
                yy = y + (3 if side < 0 else 0); ph = yy % per; n = yy // per
                shape = (.55, 1.0, .8, .6, .4, .2, .05, 0)[ph]
                off = 3.6 * shape * (1 if n % 3 else .6)
            d[y] = hw + off
    mask = set()
    for y in range(TOP, BOT + 1):
        if y in L:
            for x in range(W):
                if -L[y] <= x + .5 - CX <= R[y]: mask.add((x, y))
    mask |= {(CX - 1, TOP), (CX, TOP)}
    return mask

MASK = build_mask()
def ins(x, y): return (x, y) in MASK

def depth(x, y, n=4):
    for k in range(1, n):
        if not all(ins(x + dx * k, y + dy * k) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))): return k
    return n

def lit(x, y):
    """-1 luz (arriba-izq) .. +1 sombra (abajo-der)."""
    u = (x - (CX - 21)) / 42; v = (y - TOP) / (BOT - TOP)
    return (u + v) - 1.0

def put(cv, x, y, c, a=255):
    if ins(x, y): cv.put(x, y, rgba(c, a))

def line(cv, a, b, c, al):
    n = int(max(abs(b[0] - a[0]), abs(b[1] - a[1]))) + 1
    for t in range(n + 1): put(cv, round(a[0] + (b[0] - a[0]) * t / n), round(a[1] + (b[1] - a[1]) * t / n), c, al)

def outline_in(cv):
    add = [(x, y) for y in range(H) for x in range(W) if (x, y) not in cv.px and any(
        (x + dx, y + dy) in cv.px and cv.px[(x + dx, y + dy)][3] >= 230 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for p_ in add: cv.px[p_] = OUTL

def shadow(cv):
    for y in range(BOT - 1, BOT + 3):
        for x in range(CX - 20, CX + 21):
            if ((x + .5 - CX) / 19.5) ** 2 + ((y + .5 - BOT - 1) / 1.7) ** 2 <= 1: cv.put_out(x, y, (24, 20, 37, 90))

# --- capas ------------------------------------------------------------------------------------------
def back():
    """Fondo: rojizo en el borde y a la sombra; el centro, naranja cálido."""
    cv = Canvas(W, H)
    for (x, y) in MASK:
        d = depth(x, y); k = lit(x, y)
        c = AMB[0] if (d <= 2 or k > .45) else AMB[1] if (k > -.1 or d == 3) else AMB[2]
        cv.put(x, y, rgba(c, 235))
    return cv

def fibers(cv):
    """Vetas fibrosas curvas (como las del ámbar de la foto): del borde hacia dentro, ondulantes."""
    rnd = random.Random(SEED + 5)
    starts = [(6, 24), (9, 17), (38, 28), (36, 20), (13, 34), (31, 36), (16, 11), (27, 12), (22, 28), (8, 30)]
    for i, (sx, sy) in enumerate(starts):
        x, y = float(sx), float(sy); ang = rnd.uniform(-1.9, -1.0) if sx < CX else rnd.uniform(-2.2, -1.2)
        ang = (math.pi * (-.25 if sx < CX else -.75)) + rnd.uniform(-.5, .5)
        dark = i % 3 != 0
        for s in range(rnd.randint(9, 15)):
            put(cv, round(x), round(y), AMB[0] if dark else AMB[3], 215 if dark else 190)
            ang += math.sin(s * .9 + i) * .35; x += math.cos(ang); y += math.sin(ang) * .9

def cracks(cv):
    for pts in (((15, 14), (18, 20), (16, 26), (20, 31)), ((33, 15), (30, 22), (33, 28)), ((12, 36), (18, 38), (24, 36))):
        for a, b in zip(pts, pts[1:]):
            line(cv, a, b, AMB[4], 235)
            line(cv, (a[0] + 1, a[1]), (b[0] + 1, b[1]), AMB[0], 160)                  # sombra de la grieta

def front():
    cv = Canvas(W, H)
    for (x, y) in MASK:
        d = depth(x, y); k = lit(x, y)
        if d == 1: c = AMB[3] if k < -.15 else AMB[1] if k > .3 else AMB[2]; a = 255
        elif d == 2: c = AMB[4] if k < -.6 else AMB[2] if k < .35 else AMB[0]; a = 215 if k < -.6 else 150
        elif d == 3: c = AMB[1] if k > .1 else AMB[2]; a = 105
        else: c = AMB[3] if k < -.1 else AMB[2]; a = 50
        cv.put(x, y, rgba(c, a))
    fibers(cv); cracks(cv)
    for (x, y) in ((9, 22), (10, 19), (12, 16), (14, 13), (17, 10), (20, 8)): put(cv, x, y, AMB[4], 255)      # reflejo largo
    for (x, y) in ((8, 24), (9, 20), (11, 17), (13, 14), (16, 11)): put(cv, x, y, AMB[3], 200)
    for (x, y) in ((36, 25), (37, 27), (37, 30)): put(cv, x, y, AMB[3], 220)                                  # luz del borde derecho
    for (bx, by) in ((14, 38), (32, 34), (27, 17), (20, 30)):
        put(cv, bx, by, AMB[4], 200); put(cv, bx + 1, by + 1, AMB[3], 150)
    for x in range(12, 34): put(cv, x, 42, AMB[3], 110)                                                      # menisco en la base
    outline_in(cv)
    return cv

# --- maqueta ---------------------------------------------------------------------------------------
def frog():
    im = Image.open(os.path.join(TEMP, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')).convert('RGBA')
    return im.crop((0, 0, im.height, im.height))

def encased(body):
    out = Image.new('RGBA', (W, H)); sh = Canvas(W, H); shadow(sh); out.alpha_composite(sh.img())
    out.alpha_composite(back().img()); out.alpha_composite(body, FROG_AT); out.alpha_composite(front().img())
    return out

if __name__ == '__main__':
    os.makedirs(os.path.join(HERE, 'propuestas'), exist_ok=True)
    back().img().crop((0, 2, W, H)).save(os.path.join(HERE, 'propuestas', 'Capsule_D_Back.png'))
    front().img().crop((0, 2, W, H)).save(os.path.join(HERE, 'propuestas', 'Capsule_D_Front.png'))
    body = frog(); d = encased(body).crop((0, 2, W, H))
    sys.path.insert(0, HERE)
    import generar_capsulas as g
    b = g.with_shadow('B', g.encased('B', body))
    bg = C('#2b2318'); S = 8
    sheet = Image.new('RGBA', (46 * S * 2 + 38 * S + 24 * S, 50 * S), bg)
    sheet.alpha_composite(b.resize((38 * S, 36 * S), Image.NEAREST), (6 * S, (50 - 36) * S))
    sheet.alpha_composite(d.resize((46 * S, 46 * S), Image.NEAREST), (50 * S, 4 * S))
    sheet.convert('RGB').save(os.path.join(HERE, 'vista_previa_capsula_d.png'))
    only = Image.new('RGBA', (46 * 14, 46 * 14), bg); only.alpha_composite(d.resize((46 * 14, 46 * 14), Image.NEAREST))
    only.convert('RGB').save(os.path.join(HERE, 'vista_previa_capsula_d_grande.png'))
    print('ok')

def encased_at(body, pos):
    """Como encased(), pero con la criatura en `pos` (para cuerpos de otro tamaño)."""
    out = Image.new('RGBA', (W, H)); sh = Canvas(W, H); shadow(sh); out.alpha_composite(sh.img())
    out.alpha_composite(back().img()); out.alpha_composite(body, pos); out.alpha_composite(front().img())
    return out
