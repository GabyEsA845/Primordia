"""Cápsula de ámbar (piso 3): 3 propuestas de forma nueva, irregulares y ajustadas al cuerpo (ver README).

Sustituye al bloque octogonal de 48x56 de floor3-specials-look. Cada propuesta va en DOS capas, como antes:
`Back` (ámbar del fondo, detrás de la criatura) y `Front` (translúcida, con borde, brillos y acabado). El código
dibuja la criatura entre las dos. Celda 38x36; la criatura (rana, caja 27x22) queda con los pies en y = 31.
  A  Gota fundida   : lisa, pulida y muy translúcida; punta ladeada arriba a la derecha.
  B  Piedra tosca   : angulosa, aristas con astillas, costra clara en el borde y muchas burbujas.
  C  Nódulo con corteza: abultado, con placas de corteza oscura y una ventana clara arriba.
Uso: python generar_capsulas.py
"""
import os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
TEMP = os.path.normpath(os.path.join(HERE, '..'))
sys.path.insert(0, os.path.join(TEMP, 'floor3-environment'))
from base_piso3 import *
from PIL import Image

AMB = [C(c) for c in AMBER]            # #6a2e0e #b0581a #e89a30 #ffcc5a #fff0b0
W, H = 38, 36
FLOOR = 33                              # y del suelo bajo la cápsula
FROG_AT = (3, 3)                        # esquina de la celda de 32x32 de la rana (caja x 6..33, y 9..31)
CRUST = (74, 40, 20)                    # corteza oscura (C): ámbar sucio con tierra

def rgba(c, a=255): return tuple(c[:3]) + (a,)

# --- formas ---------------------------------------------------------------------------------------
def poly_in(pts):
    def f(x, y):
        x += .5; y += .5; ins = False; j = len(pts) - 1
        for i in range(len(pts)):
            xi, yi = pts[i]; xj, yj = pts[j]
            if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi) + xi: ins = not ins
            j = i
        return ins
    return f

def ell_in(cx, cy, rx, ry): return lambda x, y: ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 <= 1
def union(*fs): return lambda x, y: any(f(x, y) for f in fs)

SHAPES = {
    'A': union(ell_in(19, 21, 17, 12.5), poly_in([(21, 12), (27, 5), (30, 2), (31, 8), (34, 15), (29, 19)])),
    'B': poly_in([(3, 28), (2, 19), (6, 12), (11, 8), (15, 3), (21, 6), (27, 4), (33, 10), (36, 18), (35, 27),
                  (29, 33), (19, 34), (9, 33)]),
    'C': union(ell_in(13, 21, 9.5, 10.5), ell_in(25, 19, 10, 11), ell_in(19, 28, 13, 6.5), ell_in(6, 27, 4, 4)),
}

def pixels(inside): return [(x, y) for y in range(H) for x in range(W) if inside(x, y)]

def depth(inside, x, y, n=3):
    """Distancia al borde (1..n; n = interior hondo)."""
    for k in range(1, n):
        if not all(inside(x + dx * k, y + dy * k) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))): return k
    return n

def lit(inside, x, y):
    """-1 (luz, arriba a la izquierda) .. +1 (sombra, abajo a la derecha) según la posición en la forma."""
    pts = pixels(inside); xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
    u = (x - min(xs)) / max(1, max(xs) - min(xs)); v = (y - min(ys)) / max(1, max(ys) - min(ys))
    return (u + v) - 1.0

def outline_in(cv):
    add = [(x, y) for y in range(H) for x in range(W) if (x, y) not in cv.px and any(
        (x + dx, y + dy) in cv.px and cv.px[(x + dx, y + dy)][3] >= 230 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for p_ in add: cv.px[p_] = OUTL

def shadow(cv, inside):
    xs = [x for x, y in pixels(inside)]; cx = (min(xs) + max(xs)) / 2; rx = (max(xs) - min(xs)) / 2 + 1
    for y in range(FLOOR - 1, FLOOR + 3):
        for x in range(int(cx - rx), int(cx + rx) + 1):
            if ((x + .5 - cx) / rx) ** 2 + ((y + .5 - FLOOR - 1) / 1.6) ** 2 <= 1: cv.put_out(x, y, (24, 20, 37, 90))

# --- capas -----------------------------------------------------------------------------------------
def back(kind):
    """El ámbar del fondo: más oscuro y casi opaco; abajo a la derecha, más hondo."""
    ins = SHAPES[kind]; cv = Canvas(W, H)
    for (x, y) in pixels(ins):
        k = lit(ins, x, y)
        c = AMB[0] if k > .35 else AMB[1] if k > -.35 else AMB[2]
        if kind == 'C': c = AMB[0] if k > -.1 else AMB[1]
        cv.put(x, y, rgba(c, 235))
    return cv

def front(kind, seed=7):
    ins = SHAPES[kind]; cv = Canvas(W, H); rnd = random.Random(seed + ord(kind))
    pts = pixels(ins)
    for (x, y) in pts:
        d = depth(ins, x, y); k = lit(ins, x, y)
        if d == 1:                                                         # borde: claro arriba-izq, oscuro abajo-der
            c = AMB[3] if k < -.1 else AMB[1] if k > .35 else AMB[2]; a = 255
        elif d == 2:
            c = AMB[4] if k < -.55 else AMB[2] if k < .45 else AMB[0]; a = 220 if k < -.55 else 140
        else:
            c = AMB[2] if k < .15 else AMB[1]; a = 70 if kind == 'A' else 95
        cv.put(x, y, rgba(c, a))
    {'A': finish_a, 'B': finish_b, 'C': finish_c}[kind](cv, ins, pts, rnd)
    outline_in(cv)
    return cv

def spark(cv, ins, x, y, c=None, a=210):
    if ins(x, y): cv.put(x, y, rgba(c or AMB[4], a))

def finish_a(cv, ins, pts, rnd):
    """Gota fundida: reflejo largo arriba a la izquierda, destello en la punta y 3 burbujas lisas."""
    for (x, y) in ((8, 14), (9, 12), (11, 10), (13, 9), (15, 9)): spark(cv, ins, x, y, AMB[4], 255)
    for (x, y) in ((7, 16), (8, 15), (10, 11), (12, 10)): spark(cv, ins, x, y, AMB[3], 200)
    for (x, y) in ((29, 5), (30, 4), (30, 6)): spark(cv, ins, x, y, AMB[4], 255)       # la punta brilla
    for (bx, by) in ((12, 27), (26, 26), (22, 15)):
        spark(cv, ins, bx, by, AMB[4], 200); spark(cv, ins, bx + 1, by + 1, AMB[3], 150)
    for x in range(10, 28):                                                 # menisco: línea clara en la base
        spark(cv, ins, x, 31, AMB[3], 120)

def finish_b(cv, ins, pts, rnd):
    """Piedra tosca: aristas rectas claras, costra moteada en el borde y muchas burbujas pequeñas."""
    for (a, b) in (((11, 8), (8, 20)), ((21, 6), (22, 16)), ((22, 16), (33, 22)), ((8, 20), (16, 30))):
        n = int(max(abs(b[0] - a[0]), abs(b[1] - a[1]))) + 1
        for t in range(n + 1):
            x = round(a[0] + (b[0] - a[0]) * t / n); y = round(a[1] + (b[1] - a[1]) * t / n)
            spark(cv, ins, x, y, AMB[3], 170)
    for (x, y) in pts:                                                      # costra: borde de 2 px picado de oscuro
        if depth(ins, x, y) <= 2 and rnd.random() < .45: cv.put(x, y, rgba(AMB[0] if rnd.random() < .5 else AMB[1], 255))
    for _ in range(9):
        x, y = rnd.choice(pts)
        if depth(ins, x, y) == 3: spark(cv, ins, x, y, AMB[4], 200)
    for (x, y) in ((6, 14), (7, 14), (7, 15), (8, 13)): spark(cv, ins, x, y, AMB[4], 255)

def finish_c(cv, ins, pts, rnd):
    """Nódulo con corteza: placas oscuras (abajo y a la derecha), vetas claras entre ellas y una ventana clara."""
    for (x, y) in pts:
        k = lit(ins, x, y)
        d = depth(ins, x, y)
        n = ((x // 3) * 7 + (y // 3) * 5) % 7                               # placas gruesas de 3x3, no ruido
        plate = (d <= 2 and k > -.2 and n < 5) or (d <= 3 and k > .5 and n < 6)
        if plate:
            cv.put(x, y, rgba(CRUST if d > 1 else blend(CRUST, AMB[0], 60), 250))
            if (x % 3 == 0 or y % 3 == 0) and d > 1: cv.put(x, y, rgba(AMB[1], 255))   # junta de resina entre placas
    for (x, y) in pts:                                                      # reborde claro de la ventana
        if lit(ins, x, y) < -.45 and depth(ins, x, y) <= 2: cv.put(x, y, rgba(AMB[3], 255))
    for (x, y) in ((10, 12), (11, 11), (12, 10), (13, 9), (9, 14)): spark(cv, ins, x, y, AMB[4], 255)
    for (x, y) in ((22, 12), (23, 11), (24, 12)): spark(cv, ins, x, y, AMB[3], 200)

# --- maqueta ---------------------------------------------------------------------------------------
def frog():
    im = Image.open(os.path.join(TEMP, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')).convert('RGBA')
    return im.crop((0, 0, im.height, im.height))

def encased(kind, body):
    out = Image.new('RGBA', (W, H))
    out.alpha_composite(back(kind).img())
    out.alpha_composite(body, FROG_AT)
    out.alpha_composite(front(kind).img())
    return out

def with_shadow(kind, im):
    cv = Canvas(W, H); shadow(cv, SHAPES[kind]); base = cv.img(); base.alpha_composite(im); return base

if __name__ == '__main__':
    body = frog()
    os.makedirs(os.path.join(HERE, 'propuestas'), exist_ok=True)
    cells = {}
    for k in 'ABC':
        save_sheet(os.path.join(HERE, 'propuestas', f'Capsule_{k}_Back.png'), [back(k)])
        save_sheet(os.path.join(HERE, 'propuestas', f'Capsule_{k}_Front.png'), [front(k)])
        cells[k] = with_shadow(k, encased(k, body))
    # el bloque octogonal de antes (48x56) para comparar el tamaño
    old = Image.new('RGBA', (48, 56))
    sh = os.path.join(TEMP, 'floor3-specials-look', 'shell')
    ob = Image.open(os.path.join(sh, 'Fx_AmberShell_Back_Hold.png')).convert('RGBA').crop((0, 0, 48, 56))
    of = Image.open(os.path.join(sh, 'Fx_AmberShell_Front_Hold.png')).convert('RGBA').crop((0, 0, 48, 56))
    old.alpha_composite(ob); old.alpha_composite(body, ((48 - 32) // 2, 51 - 32 + 2 - 0)); old.alpha_composite(of)
    bg = C('#2b2318')
    S = 8; pad = 6
    row = [('antes', old)] + [(k, cells[k]) for k in 'ABC']
    sheet = Image.new('RGBA', (sum(im.width * S + pad * S for _, im in row) + pad * S, 56 * S + 2 * pad * S + 40), bg)
    x = pad * S
    for name, im in row:
        sheet.alpha_composite(im.resize((im.width * S, im.height * S), Image.NEAREST), (x, pad * S + (56 - im.height) * S))
        x += im.width * S + pad * S
    # fila a escala real (x3) al pie
    small = Image.new('RGBA', (sheet.width // 2, 56 * 3 + 10), bg)
    xs = 20
    for name, im in row:
        small.alpha_composite(im.resize((im.width * 3, im.height * 3), Image.NEAREST), (xs, 5 + (56 - im.height) * 3)); xs += im.width * 3 + 24
    sheet.convert('RGB').save(os.path.join(HERE, 'vista_previa_capsulas.png'))
    small.convert('RGB').save(os.path.join(HERE, 'vista_previa_capsulas_escala.png'))
    print('ok', sheet.size)
