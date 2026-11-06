"""Gota de ámbar (piso 3): propuestas de forma del bloque que encierra al enemigo.

Rectángulo de bordes irregulares, como si el ámbar hubiera crecido desde el suelo, con la textura de la Cápsula D
(borde rojizo, centro translúcido, vetas fibrosas, grietas, reflejo, burbujas). Dos capas: Back detrás del enemigo y
Front translúcida delante. Medido para el escarabajo (26x13); se prueba también con el sapo y la araña.
  A = escalonado (columnas de ámbar de distinta altura, como cristal que ha subido a tirones)
  B = estratos (capas de resina apiladas desde el suelo, cada una más estrecha; la de arriba, irregular)
  C = agujas (bloque con puntas de cristal arriba y pinchos laterales que suben desde el suelo, familia de la Cápsula)
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__)); TEMP = os.path.normpath(os.path.join(HERE, '..'))
sys.path.insert(0, os.path.join(TEMP, 'floor3-environment'))
from base_piso3 import *
from PIL import Image

AMB = [C(c) for c in AMBER]
W, H, CX, BOT = 48, 40, 24, 35                     # celda, eje y última fila del bloque (pies del enemigo en y = 33)
ENEMY_BOX = (CX - 13, BOT - 15, CX + 13, BOT - 1)   # donde va el enemigo: sin grietas ni vetas encima

STRATA = []                                        # filas donde se junta una capa con la de abajo (propuesta B)

def rgba(c, a=255): return tuple(c[:3]) + (a,)
def in_enemy(x, y): return ENEMY_BOX[0] <= x <= ENEMY_BOX[2] and ENEMY_BOX[1] <= y <= ENEMY_BOX[3]

class Block:
    def __init__(self, mask):
        self.mask = mask; xs = [x for x, y in mask]; ys = [y for x, y in mask]
        self.x0, self.x1, self.y0, self.y1 = min(xs), max(xs), min(ys), max(ys)
    def ins(self, x, y): return (x, y) in self.mask
    def depth(self, x, y, n=4):
        for k in range(1, n):
            if not all(self.ins(x + dx * k, y + dy * k) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))): return k
        return n
    def lit(self, x, y):                             # luz arriba a la izquierda: -1 muy iluminado, +1 en sombra
        u = (x - self.x0) / max(1, self.x1 - self.x0); v = (y - self.y0) / max(1, self.y1 - self.y0)
        return u + v - 1.0
    def put(self, cv, x, y, c, a=255):
        if self.ins(x, y): cv.put(x, y, rgba(c, a))

# --- formas --------------------------------------------------------------------------------------------
def mask_from_tops(tops, flare):
    """tops: {x: y de arriba}; flare: {fila desde abajo: px de más a cada lado} (el pie que se ensancha)."""
    m = set()
    for x, t in tops.items():
        for y in range(t, BOT + 1): m.add((x, y))
    xs = sorted(tops); l, r = xs[0], xs[-1]
    for k, ext in flare.items():
        for e in range(1, ext + 1): m.add((l - e, BOT - k)); m.add((r + e, BOT - k))
    return m

def shape_a():
    rnd = random.Random(4); tops = {}; x = CX - 17; h = 19
    while x <= CX + 17:
        w = rnd.randint(3, 5); edge = x - (CX - 17) < 4 or (CX + 17) - x < 4
        h = max(16, min(24, h + rnd.choice((-3, -2, 2, 3)))) - (3 if edge else 0)
        for i in range(w):
            if x + i <= CX + 17: tops[x + i] = BOT - h
        x += w
    m = mask_from_tops(tops, {0: 2, 1: 1})
    for y in (BOT - 9, BOT - 8, BOT - 13): m.discard((CX - 17, y))
    for y in (BOT - 6, BOT - 11, BOT - 12): m.discard((CX + 17, y))
    return m

def shape_b():
    """Estratos: capas de resina apiladas desde el suelo, cada una algo más estrecha y desplazada; la última, irregular."""
    rnd = random.Random(6); m = set(); STRATA.clear()
    bands = [(BOT, 6, 18), (BOT - 6, 5, 16), (BOT - 11, 5, 14), (BOT - 16, 4, 11)]   # (fila de abajo, alto, medio ancho)
    for i, (yb, hgt, hw) in enumerate(bands):
        off = (0, 1, -1, 0)[i]
        for y in range(yb - hgt + 1, yb + 1):
            l, r = CX - hw + off, CX + hw + off
            if y == yb - hgt + 1 and i < 3: l += 1; r -= 1                      # canto redondeado de cada capa
            for x in range(l, r + 1): m.add((x, y))
        if i: STRATA.append(yb)
    top = bands[-1][0] - bands[-1][1]
    x = CX - 10                                                                  # remate irregular: lomos de 2-4 px
    while x <= CX + 10:
        w = rnd.randint(2, 4); h = rnd.choice((0, 1, 2, 2, 3))
        for i in range(w):
            for k in range(h - (1 if i in (0, w - 1) and h > 1 else 0)): m.add((x + i, top - k))
        x += w
    for e in (1, 2): m.add((CX - 17 - e, BOT)); m.add((CX + 17 + e, BOT))
    m.add((CX - 18, BOT - 1)); m.add((CX + 18, BOT - 1))
    return m

def shape_c():
    rnd = random.Random(9); tops = {x: BOT - 18 - rnd.choice((0, 0, 1)) for x in range(CX - 16, CX + 17)}
    for cx_, h, w in ((CX - 10, 6, 5), (CX - 3, 9, 6), (CX + 5, 7, 5), (CX + 12, 5, 4)):
        for x in range(cx_ - w // 2 - 1, cx_ + w // 2 + 2):
            peak = BOT - 18 - round(h * max(0.0, 1 - abs(x - cx_) / (w / 2 + .6)))
            if x in tops: tops[x] = min(tops[x], peak)
    m = mask_from_tops(tops, {0: 1})
    for side in (-1, 1):                             # pinchos que suben desde el suelo, pegados a los lados
        base = CX + side * 16
        for k in range(14):
            yy = BOT - k; wdt = max(0, round((14 - k) / 4.0))
            for e in range(1, wdt + 1): m.add((base + side * e, yy))
    return m

# --- capas ---------------------------------------------------------------------------------------------
def back_layer(b):
    cv = Canvas(W, H)
    for (x, y) in b.mask:
        d = b.depth(x, y); k = b.lit(x, y)
        cv.put(x, y, rgba(AMB[0] if (d <= 2 or k > .45) else AMB[1] if (k > -.1 or d == 3) else AMB[2], 235))
    return cv

def front_layer(b, rnd, n_fib=7, n_crack=4, n_bub=4, scale=1.0):
    cv = Canvas(W, H)
    for (x, y) in b.mask:
        d = b.depth(x, y); k = b.lit(x, y)
        if d == 1: c = AMB[3] if k < -.15 else AMB[1] if k > .3 else AMB[2]; a = 255
        elif d == 2: c = AMB[4] if k < -.6 else AMB[2] if k < .35 else AMB[0]; a = 215 if k < -.6 else 150
        elif d == 3: c = AMB[1] if k > .1 else AMB[2]; a = 105
        else: c = AMB[3] if k < -.1 else AMB[2]; a = 50
        cv.put(x, y, rgba(c, a))
    pool = [p for p in b.mask if b.depth(*p) == 2 and not in_enemy(*p)]
    allp = [p for p in b.mask if b.depth(*p) >= 2]
    for i in range(n_fib):                           # vetas fibrosas que suben desde abajo
        x, y = rnd.choice(pool if i % 2 == 0 else allp); ang = -math.pi / 2 + rnd.uniform(-.7, .7)
        for s in range(int(rnd.randint(6, 11) * scale)):
            xi, yi = round(x), round(y); inside = in_enemy(xi, yi)          # por delante del enemigo, tenues
            b.put(cv, xi, yi, AMB[0] if i % 3 else AMB[3], (85 if inside else 215) if i % 3 else (75 if inside else 190))
            ang += math.sin(s * .9 + i) * .35; x += math.cos(ang) * .8; y += math.sin(ang)
    for _ in range(n_crack):                         # grietas finas
        x, y = rnd.choice(pool); ang = rnd.uniform(0, 6.28)
        for s in range(int(rnd.randint(4, 8) * scale)):
            if not in_enemy(round(x), round(y)):
                b.put(cv, round(x), round(y), AMB[4], 235); b.put(cv, round(x) + 1, round(y), AMB[0], 150)
            ang += rnd.uniform(-.7, .7); x += math.cos(ang); y += math.sin(ang) * .9
    pts = sorted((p for p in b.mask if b.depth(*p) == 2 and b.lit(*p) < -.5), key=lambda p: p[0] + p[1])
    for (x, y) in pts[:int(14 * scale)]: b.put(cv, x, y, AMB[4], 255)   # reflejo largo arriba a la izquierda
    deep = [p for p in b.mask if b.depth(*p) >= 3 and not in_enemy(*p)]
    for _ in range(n_bub):
        x, y = rnd.choice(deep); b.put(cv, x, y, AMB[4], 200); b.put(cv, x + 1, y + 1, AMB[0], 140)
    for x in range(b.x0 + 2, b.x1 - 1): b.put(cv, x, BOT - 1, AMB[3], 110)   # menisco en la base
    for ys in STRATA:                                # juntura entre capas: línea clara encima, oscura debajo
        for x in range(b.x0, b.x1 + 1):
            if b.ins(x, ys - 1) and b.depth(x, ys - 1) >= 2: b.put(cv, x, ys - 1, AMB[3], 120 if in_enemy(x, ys - 1) else 180)
            if b.ins(x, ys) and b.depth(x, ys) >= 2: b.put(cv, x, ys, AMB[0], 90 if in_enemy(x, ys) else 160)
    add = [(x, y) for y in range(H) for x in range(W) if (x, y) not in cv.px and any(
        (x + dx, y + dy) in cv.px and cv.px[(x + dx, y + dy)][3] >= 230 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for p in add: cv.px[p] = OUTL
    return cv

# --- maqueta -------------------------------------------------------------------------------------------
def enemy(name):
    im = Image.open(os.path.join(TEMP, 'floor3-enemies', name, f'{name}_Idle.png')).convert('RGBA')
    cw = im.width // (6 if name == 'Mayflies' else 4)                    # las celdas no siempre son cuadradas (README)
    fr = im.crop((0, 0, cw, im.height)); bb = fr.getbbox(); return fr.crop(bb)

def compose(bk, fr, body, bg=C('#2b2318')):
    im = Image.new('RGBA', (W, H + 2), bg); im.alpha_composite(Image.new('RGBA', (W, H + 2 - BOT - 1), C('#33291c')), (0, BOT + 1))
    sh = Canvas(W, H + 2)
    for y in range(BOT, BOT + 3):
        for x in range(W):
            if ((x + .5 - CX) / (W / 2 - 4)) ** 2 + ((y + .5 - BOT - 1) / 1.6) ** 2 <= 1: sh.put(x, y, (24, 20, 37, 90))
    im.alpha_composite(sh.img()); im.alpha_composite(bk.img())
    im.alpha_composite(body, (CX - body.width // 2, BOT - 1 - body.height)); im.alpha_composite(fr.img())
    return im

if __name__ == '__main__':
    out = os.path.join(HERE, 'propuestas'); os.makedirs(out, exist_ok=True)
    bodies = [enemy(n) for n in ('Beetle', 'Toad', 'Spider')]
    rows = []
    for name, fn, seed in (('A', shape_a, 1), ('B', shape_b, 2), ('C', shape_c, 3)):
        STRATA.clear(); b = Block(fn()); bk = back_layer(b); fr = front_layer(b, random.Random(seed))
        bk.img().save(os.path.join(out, f'DropBlock_{name}_Back.png')); fr.img().save(os.path.join(out, f'DropBlock_{name}_Front.png'))
        empty = Image.new('RGBA', (W, H + 2), C('#2b2318')); empty.alpha_composite(bk.img()); empty.alpha_composite(fr.img())
        rows.append([empty] + [compose(bk, fr, body) for body in bodies])
    S = 5; cw, ch = W * S, (H + 2) * S
    sheet = Image.new('RGB', (cw * 4 + 10, ch * 3 + 8), (20, 16, 12))
    for r, row in enumerate(rows):
        for c, im in enumerate(row): sheet.paste(im.resize((cw, ch), Image.NEAREST).convert('RGB'), (2 + c * (cw + 2), 2 + r * (ch + 2)))
    sheet.save(os.path.join(HERE, 'vista_previa_propuestas.png')); print('ok', sheet.size)
