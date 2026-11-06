"""Cápsula de ámbar de nivel III (piso 3): 3 propuestas sobre la forma D (gota con pinchos), más grandes e imponentes.

  1 Corona de cristales : la misma silueta, mayor, con cristales dorados que brotan de los hombros y halo dorado.
  2 Doble cáscara       : cáscara exterior gruesa y oscura con vetas de oro + la cápsula clara dentro + núcleo cálido.
  3 Ámbar con vetas de oro: ámbar claro con vetas de oro puro como raíces, facetas pulidas y motas doradas que suben.
Capas por propuesta: Back (detrás de la criatura), Front (translúcida delante) y Glow (halo y motas, por detrás de todo).
Celda 58x60; la criatura (caja 27x22) queda centrada con los pies en y = 51. Uso: python generar_capsula_nivel3.py
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
TEMP = os.path.normpath(os.path.join(HERE, '..'))
sys.path.insert(0, os.path.join(TEMP, 'floor3-environment')); sys.path.insert(0, HERE)
from base_piso3 import *
from PIL import Image

AMB = [C(c) for c in AMBER]
W, H, CX, FEET = 58, 60, 29, 51
FROG_AT = (13, 23)

def rgba(c, a=255): return tuple(c[:3]) + (a,)

# --- formas --------------------------------------------------------------------------------------------
def make_mask(top, bot, cy, ry, rx, tip0, tip_end, amp, per=7, spikes=()):
    """Gota de agua (bulbo + punta roma) con pinchos laterales que suben desde el suelo y, si se piden, cristales."""
    mask = set(); crown = set()
    for y in range(top, bot + 1):
        e = rx * math.sqrt(max(0.0, 1 - ((y + .5 - cy) / ry) ** 2)) if y < bot + .5 else 0
        q = (y + .5 - top) / (tip_end - top)
        t = (tip0 + (rx - tip0) * q ** 1.05) * min(1.0, (y - top + 1.5) / 4.0) if top < y + .5 < tip_end else 0
        hw = max(e, t)
        if hw <= 0: continue
        p = (y - top) / (bot - top); L = R = hw
        if .16 < p < .93:
            for side in (-1, 1):
                yy = y + (3 if side < 0 else 0); pp = per + (1 if side < 0 else 0)
                off = amp * (.55, 1.0, .8, .6, .4, .2, .05, 0, 0)[yy % pp] * (1 if (yy // pp) % 3 else .6)
                if side < 0: L += off
                else: R += off
        for x in range(W):
            if -L <= x + .5 - CX <= R: mask.add((x, y))
    mask |= {(CX - 1, top), (CX, top)}
    for (xc, yb, h, w) in spikes:                                             # cristales: triángulos con la punta arriba
        for k in range(h):
            hw = max(.5, w / 2 * (1 - k / h))
            for x in range(int(xc - hw + .5), int(xc + hw + .5) + 1):
                if (x, yb - k) not in mask: crown.add((x, yb - k))
    return mask | crown, crown

class Cap:
    def __init__(self, mask, crown=frozenset()):
        self.mask = mask; self.crown = crown
        xs = [x for x, y in mask]; ys = [y for x, y in mask]
        self.x0, self.x1, self.y0, self.y1 = min(xs), max(xs), min(ys), max(ys)
    def ins(self, x, y): return (x, y) in self.mask
    def depth(self, x, y, n=4):
        for k in range(1, n):
            if not all(self.ins(x + dx * k, y + dy * k) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))): return k
        return n
    def lit(self, x, y):
        u = (x - self.x0) / max(1, self.x1 - self.x0); v = (y - self.y0) / max(1, self.y1 - self.y0)
        return u + v - 1.0
    def put(self, cv, x, y, c, a=255):
        if self.ins(x, y): cv.put(x, y, rgba(c, a))
    def line(self, cv, a, b, c, al):
        n = int(max(abs(b[0] - a[0]), abs(b[1] - a[1]))) + 1
        for t in range(n + 1):
            x = round(a[0] + (b[0] - a[0]) * t / n); y = round(a[1] + (b[1] - a[1]) * t / n)
            if not in_creature(x, y): self.put(cv, x, y, c, al)

def in_creature(x, y): return 14 <= x <= 44 and 27 <= y <= 52          # donde va la criatura: sin grietas ni vetas encima

def outline_in(cv):
    add = [(x, y) for y in range(H) for x in range(W) if (x, y) not in cv.px and any(
        (x + dx, y + dy) in cv.px and cv.px[(x + dx, y + dy)][3] >= 230 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for p_ in add: cv.px[p_] = OUTL

# --- capas base ----------------------------------------------------------------------------------------
def back_layer(cap, core=False):
    cv = Canvas(W, H)
    for (x, y) in cap.mask:
        d = cap.depth(x, y); k = cap.lit(x, y)
        c = AMB[0] if (d <= 2 or k > .45) else AMB[1] if (k > -.1 or d == 3) else AMB[2]
        cv.put(x, y, rgba(c, 235))
    if core:                                                                  # núcleo cálido detrás de la criatura
        for y in range(H):
            for x in range(W):
                r = math.hypot((x - CX) / 15.0, (y - 43) / 11.0)
                if r < 1 and cap.ins(x, y):
                    cv.put(x, y, rgba(AMB[4] if r < .35 else AMB[3] if r < .65 else AMB[2], 245 if r < .65 else 225))
    return cv

def front_layer(cap, crown_light=True, deep=False):
    cv = Canvas(W, H)
    for (x, y) in cap.mask:
        d = cap.depth(x, y); k = cap.lit(x, y)
        if (x, y) in cap.crown and crown_light:                               # cristales: claros y casi opacos
            c = AMB[4] if k < -.1 else AMB[3] if k < .5 else AMB[2]; a = 235
        elif d == 1: c = AMB[3] if k < -.15 else AMB[1] if k > .3 else AMB[2]; a = 255
        elif d == 2: c = AMB[4] if k < -.6 else AMB[2] if k < .35 else AMB[0]; a = 215 if k < -.6 else 150
        elif d == 3: c = AMB[0] if deep else (AMB[1] if k > .1 else AMB[2]); a = 130 if deep else 105
        else: c = AMB[1] if deep else (AMB[3] if k < -.1 else AMB[2]); a = 105 if deep else 50
        cv.put(x, y, rgba(c, a))
    return cv

def cracks(cap, cv, rnd, n=4):
    for _ in range(n):
        x, y = rnd.choice([p for p in cap.mask if cap.depth(*p) == 2 and not in_creature(*p)])
        ang = math.atan2(40 - y, CX - x) + rnd.uniform(-.5, .5)
        for s in range(rnd.randint(7, 12)):
            if not in_creature(round(x), round(y)):
                cap.put(cv, round(x), round(y), AMB[4], 235); cap.put(cv, round(x) + 1, round(y), AMB[0], 150)
            ang += rnd.uniform(-.7, .7); x += math.cos(ang); y += math.sin(ang) * .9

def veins(cap, cv, rnd, n, core=(CX, 40), c1=None, c2=None, length=(10, 17), branch=True):
    c1 = c1 or AMB[4]; c2 = c2 or AMB[3]
    pool = [p for p in cap.mask if cap.depth(*p) == 2 and not in_creature(*p)]
    def walk(x, y, ang, L, depth_):
        for s in range(L):
            xi, yi = round(x), round(y)
            if not in_creature(xi, yi):
                cap.put(cv, xi, yi, c1, 255); cap.put(cv, xi + 1, yi, c2, 170); cap.put(cv, xi, yi + 1, c1, 255); cap.put(cv, xi + 1, yi + 1, c2, 140)
            tgt = math.atan2(core[1] - y, core[0] - x)
            ang += (math.sin(s * 1.1 + L) * .4) + (tgt - ang) * .08; x += math.cos(ang); y += math.sin(ang) * .95
            if branch and depth_ == 0 and s == L // 2 and rnd.random() < .8: walk(x, y, ang + rnd.choice((-.9, .9)), L // 2, 1)
    for _ in range(n):
        x, y = rnd.choice(pool); walk(float(x), float(y), math.atan2(core[1] - y, core[0] - x) + rnd.uniform(-.4, .4), rnd.randint(*length), 0)

def reflections(cap, cv, k=1.0):
    pts = [(p) for p in cap.mask if cap.depth(*p) == 2 and cap.lit(*p) < -.55 and p[1] > cap.y0 + 6]
    pts.sort(key=lambda p: (p[0] + p[1]))
    for (x, y) in pts[:int(18 * k)]: cap.put(cv, x, y, AMB[4], 255)

def bubbles(cap, cv, rnd, n=4):
    for _ in range(n):
        x, y = rnd.choice([p for p in cap.mask if cap.depth(*p) >= 3 and not in_creature(*p)])
        cap.put(cv, x, y, AMB[4], 200); cap.put(cv, x + 1, y + 1, AMB[3], 150)

def glow(cap, strength, motes=0, seed=3, cy=36, rad=29):
    """Halo dorado tenue (y motas que suben) por detrás de todo. El código lo hace respirar."""
    cv = Canvas(W, H); rnd = random.Random(seed)
    for y in range(H):
        for x in range(W):
            r = math.hypot((x + .5 - CX) / rad, (y + .5 - cy) / (rad * 1.02))
            if r < 1:
                a = int(strength * (1 - r) ** 1.6)
                if a > 4: cv.put(x, y, rgba(AMB[3], a))
    for _ in range(motes):
        x = rnd.randint(2, W - 3); y = rnd.randint(2, H - 8)
        if not cap.ins(x, y) and not cap.ins(x, y + 1) and math.hypot(x - CX, y - 34) < 29:
            cv.put(x, y, rgba(AMB[4], 235)); cv.put(x, y + 1, rgba(AMB[3], 120))
    return cv

# --- las tres propuestas --------------------------------------------------------------------------------
def prop1():
    args = (2, 54, 39.5, 15.5, 21.5, 4.4, 42, 4.4)
    base, _ = make_mask(*args)
    def top_at(x): return min(y for (xx, y) in base if xx == x)
    sp = [(CX + dx, top_at(CX + dx) + 3, h, w) for dx, h, w in ((-11, 12, 5), (13, 13, 5), (-17, 10, 4), (18, 10, 4))]
    mask, crown = make_mask(*args, spikes=sp)
    cap = Cap(mask, crown); rnd = random.Random(21)
    fr = front_layer(cap); cracks(cap, fr, rnd, 3); reflections(cap, fr); bubbles(cap, fr, rnd, 4); outline_in(fr)
    return back_layer(cap), fr, glow(cap, 150), cap

def prop2():
    mo, _ = make_mask(2, 57, 40.5, 17.5, 25, 5, 46, 5.2)
    mi, _ = make_mask(12, 54, 40.5, 14.5, 17.5, 3.4, 46, 3.2)
    out = Cap(mo); inn = Cap(mi); rnd = random.Random(33)
    ring = mo - mi
    # fondo: el ámbar de la cápsula interior con núcleo cálido
    bk = back_layer(inn, core=True)
    # frente: cáscara exterior (anillo oscuro con vetas de oro) + la cápsula interior translúcida
    fr = front_layer(inn, crown_light=False)
    for (x, y) in ring:
        d = out.depth(x, y); k = out.lit(x, y)
        near_in = any((x + dx, y + dy) in mi for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
        c = AMB[0] if k > -.2 else AMB[1]
        if d == 1: c = AMB[2] if k < -.2 else AMB[0]
        if near_in: c = (74, 40, 20)                                          # juntura oscura entre las dos cáscaras
        fr.put(x, y, rgba(c, 255))
    for (x, y) in sorted(p for p in ring if out.depth(*p) == 2 and out.lit(*p) < -.5)[:16]: fr.put(x, y, rgba(AMB[3], 255))
    pool = [p for p in ring if not near_edge(p, mi) and out.depth(*p) >= 2]
    for _ in range(9):                                                        # vetas de oro por la cáscara
        x, y = rnd.choice(pool); ang = rnd.uniform(0, 6.28)
        for s in range(rnd.randint(5, 9)):
            if (round(x), round(y)) in ring and not in_creature(round(x), round(y)):
                fr.put(round(x), round(y), rgba(AMB[3] if s % 3 else AMB[4], 255))
            ang += rnd.uniform(-.8, .8); x += math.cos(ang); y += math.sin(ang)
    cracks(inn, fr, rnd, 2); reflections(inn, fr, .6); bubbles(inn, fr, rnd, 3)
    outline_in(fr)
    return bk, fr, glow(out, 110, cy=38), out

def near_edge(p, mask): return any((p[0] + dx, p[1] + dy) in mask for dx in (-1, 0, 1) for dy in (-1, 0, 1))

def prop3():
    mask, crown = make_mask(6, 54, 39.5, 15, 20, 3.8, 42, 3.8)
    cap = Cap(mask); rnd = random.Random(47)
    fr = front_layer(cap, crown_light=False, deep=True)
    for (x, y) in cap.mask:                                                   # facetas pulidas: arista clara y cara más lisa
        if cap.depth(x, y) == 2 and cap.lit(x, y) < -.3: fr.put(x, y, rgba(AMB[4], 235))
    cap.line(fr, (CX - 12, 20), (CX - 17, 36), AMB[4], 220); cap.line(fr, (CX + 3, 8), (CX + 12, 22), AMB[3], 200)
    veins(cap, fr, rnd, 7); cracks(cap, fr, rnd, 2); bubbles(cap, fr, rnd, 3)
    outline_in(fr)
    return back_layer(cap), fr, glow(cap, 105, motes=16), cap

# --- maqueta ---------------------------------------------------------------------------------------
def frog():
    im = Image.open(os.path.join(TEMP, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')).convert('RGBA')
    return im.crop((0, 0, im.height, im.height))

def compose(bk, fr, gl, cap, body, bot):
    out = Image.new('RGBA', (W, H)); out.alpha_composite(gl.img())
    sh = Canvas(W, H)
    for y in range(bot - 1, bot + 3):
        for x in range(CX - 24, CX + 25):
            if ((x + .5 - CX) / (cap.x1 - cap.x0) * 2) ** 2 + ((y + .5 - bot - 1) / 1.8) ** 2 <= 1: sh.put_out(x, y, (24, 20, 37, 90))
    out.alpha_composite(sh.img()); out.alpha_composite(bk.img()); out.alpha_composite(body, FROG_AT); out.alpha_composite(fr.img())
    return out

if __name__ == '__main__':
    body = frog(); os.makedirs(os.path.join(HERE, 'nivel3'), exist_ok=True)
    cells = []; names = ['1_Corona', '2_DobleCascara', '3_VetasDeOro']
    for name, fn, bot in zip(names, (prop1, prop2, prop3), (54, 57, 54)):
        bk, fr, gl, cap = fn()
        for lay, cv in (('Back', bk), ('Front', fr), ('Glow', gl)): cv.img().save(os.path.join(HERE, 'nivel3', f'Capsule3_{name}_{lay}.png'))
        cells.append(compose(bk, fr, gl, cap, body, bot))
    import generar_capsula_d as g                                              # la cápsula actual, a la misma escala, pies alineados
    base = g.encased(frog()).crop((0, 2, g.W, g.H)); b = Image.new('RGBA', (W, H)); b.alpha_composite(base, ((W - base.width) // 2, FEET + 1 - 39))
    S = 8; bg = C('#2b2318')
    sheet = Image.new('RGBA', (W * S * 4 + 50, H * S + 20), bg)
    for i, im in enumerate([b] + cells):
        sheet.alpha_composite(im.resize((W * S, H * S), Image.NEAREST), (10 + i * (W * S + 10), 10))
    sheet.convert('RGB').save(os.path.join(HERE, 'vista_previa_nivel3.png'))
    print('ok', sheet.size)
