"""Cápsula de ámbar (piso 3): animación de rotura (12 cuadros; la criatura queda libre desde el cuadro del estallido).

1) AGRIETADO (4 cuadros, 110 ms): grietas que salen del centro-abajo y se ramifican en todas direcciones; la cápsula
   tiembla y la luz dorada se cuela por las grietas.  2) QUIEBRE (1 cuadro, 90 ms): queda partida en 8 trozos grandes
   separados por rendijas de luz.  3) ESTALLIDO (7 cuadros, 70 ms): los trozos (con borde propio) salen despedidos,
   giran y caen; esquirlas pequeñas, polvo y destello dorado; Back vacío y la criatura libre.  Nivel III: las vetas del
   suelo vibran, brillan y se disuelven en motas que suben.
Hojas (celda 96x60: la cápsula ocupa el centro, eje x = 48, pies de la criatura en y = 51; la criatura va ENTRE Back y Front):
  Fx_AmberCapsule_Break_{Back,Front}  (nivel I-II)    Fx_AmberCapsule3_Break_{Back,Front}  (nivel III)
  Fx_AmberCapsule_Break_Glow (destello, polvo; detrás de todo)    Fx_AmberFloorVeins_Break (nivel III; celda 96x16, fila 0 = y 44)
Cuadro que cuenta: el 5 (índice 4, el quiebre) es cuando el código libera al jugador y reparte las esquirlas.
Uso: python generar_rotura.py
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import generar_cristalizacion as cz
import generar_capsula_nivel3 as n3
from generar_capsula_nivel3 import AMB, rgba, H
from base_piso3 import *
from PIL import Image

BW, OX = 96, 19                                  # ancho de la celda y desfase de la cápsula (58 -> 96)
CX, FLOOR = 29 + OX, 56
NCR, NBU = 4, 7
NF = NCR + 1 + NBU
MS = [110] * NCR + [90] + [70] * NBU
FROG_AT = (cz.FROG_AT[0] + OX, cz.FROG_AT[1])
CEN = (CX, 42)                                    # de dónde salen las grietas y el estallido

def wide(cv):
    out = Canvas(BW, H)
    for (x, y), c in cv.px.items(): out.px[(x + OX, y)] = c
    return out

def cp(cv):
    out = Canvas(BW, H); out.px = dict(cv.px); return out

def shiftx(cv, dx):
    if dx == 0: return cv
    out = Canvas(BW, H)
    for (x, y), c in cv.px.items(): out.px[(x + dx, y)] = c
    return out

def in_creature(x, y): return 14 + OX <= x <= 44 + OX and 27 <= y <= 52

# --- 1) grietas ------------------------------------------------------------------------------------------------
def crack_data(mask, seed):
    rnd = random.Random(seed); pts = []; n = 13
    def walk(x, y, ang, L, s0, branch):
        for s in range(L):
            xi, yi = round(x), round(y)
            if (xi, yi) not in mask: return
            pts.append((s0 + s, xi, yi))
            ang += rnd.uniform(-.55, .55); x += math.cos(ang); y += math.sin(ang) * .95
            if branch and s in (7, 14) and rnd.random() < .7: walk(x, y, ang + rnd.choice((-.9, .9)), 9, s0 + s, False)
    for i in range(n):
        ang = 2 * math.pi * i / n + rnd.uniform(-.2, .2)
        walk(CEN[0] + math.cos(ang) * 13, CEN[1] + math.sin(ang) * 11, ang, 26, 0, True)
    return pts

def cracked(front, pts, k, mask):
    out = cp(front); lim = 6 + k * 7
    for (s, x, y) in pts:
        if s > lim or in_creature(x, y): continue
        out.px[(x, y)] = rgba(AMB[4], 245)
        if (x + 1, y) in mask and (x + 1, y) not in [(a, b) for (_, a, b) in pts] and (x + 1, y) in out.px and out.px[(x + 1, y)] != OUTL:
            out.px[(x + 1, y)] = rgba(AMB[0], 170)
    if k >= 1:                                                           # la luz dorada se cuela por las grietas
        glowpx = {}
        for (s, x, y) in pts:
            if s > lim or in_creature(x, y): continue
            for dx, dy in ((0, -1), (-1, 0), (0, 1)):
                q = (x + dx, y + dy)
                if q in mask and q in out.px and out.px[q] != OUTL and out.px[q][3] < 250: glowpx[q] = 1
        for q in glowpx: out.px[q] = rgba(AMB[3], 215 if k < 3 else 245)
    return out

# --- 2) quiebre en trozos ---------------------------------------------------------------------------------------
def pieces_of(mask, n=8, seed=4):
    rnd = random.Random(seed); pts = sorted(mask); seeds = []
    cand = pts[:]; rnd.shuffle(cand)
    for p in cand:
        if all(math.hypot(p[0] - q[0], p[1] - q[1]) > 11 for q in seeds): seeds.append(p)
        if len(seeds) == n: break
    asg = {}
    for (x, y) in pts:
        noise = ((x * 7 + y * 13) % 5) * .7
        asg[(x, y)] = min(range(len(seeds)), key=lambda i: math.hypot(x - seeds[i][0], (y - seeds[i][1]) * 1.1) + (noise if i % 2 else 0))
    return asg, len(seeds)

def fractured(front, asg):
    out = cp(front); seam = set()
    for (x, y), a in asg.items():
        if any(asg.get((x + dx, y + dy), a) != a for dx, dy in ((1, 0), (0, 1))): seam.add((x, y))
    for q in seam: out.px[q] = rgba(AMB[4], 255)
    return out, seam

# --- 3) estallido -------------------------------------------------------------------------------------------------
def flatten(back, front):
    im = Image.new('RGBA', (BW, H)); im.alpha_composite(back.img()); im.alpha_composite(front.img()); return im

def outlined(img):
    w, h = img.size; out = Image.new('RGBA', (w + 2, h + 2)); out.paste(img, (1, 1)); px = out.load(); src = img.load()
    for y in range(h + 2):
        for x in range(w + 2):
            if px[x, y][3] == 0:
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    xx, yy = x - 1 + dx, y - 1 + dy
                    if 0 <= xx < w and 0 <= yy < h and src[xx, yy][3] >= 200: px[x, y] = OUTL + (255,) if len(OUTL) == 3 else OUTL; break
    return out

def burst(back, front, asg, n, rnd):
    flat = flatten(back, front); parts = []
    for i in range(n):
        cells = [p for p, a in asg.items() if a == i]
        xs = [x for x, y in cells]; ys = [y for x, y in cells]; x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
        im = Image.new('RGBA', (x1 - x0 + 1, y1 - y0 + 1)); px = im.load(); fp = flat.load()
        for (x, y) in cells: px[x - x0, y - y0] = fp[x, y]
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2; d = math.hypot(cx - CEN[0], cy - CEN[1])
        ux, uy = ((cx - CEN[0]) / d, (cy - CEN[1]) / d) if d > 3 else (rnd.uniform(-1, 1), -1)
        parts.append(dict(img=outlined(im), c=(cx, cy), u=(ux, uy), sp=rnd.uniform(3.4, 5.2), spin=rnd.uniform(-26, 26)))
    shards = []
    cells = sorted(asg)
    for _ in range(16):
        x, y = rnd.choice(cells); col = [fp for fp in [flat.getpixel((x, y))] if fp[3] > 0]
        ang = rnd.uniform(0, 2 * math.pi)
        shards.append(dict(p=(x, y), v=(math.cos(ang) * rnd.uniform(3, 7), math.sin(ang) * rnd.uniform(2, 6) - 3), col=col[0] if col else AMB[3] + (255,), big=rnd.random() < .4))
    return parts, shards

def burst_frame(parts, shards, t):
    cv = Canvas(BW, H); im = Image.new('RGBA', (BW, H)); alpha = 1.0 if t <= 4 else .55 if t == 5 else .3 if t == 6 else .12
    for p in parts:
        ux, uy = p['u']; cx, cy = p['c']
        x = cx + ux * p['sp'] * t * .8; y = cy + (uy * p['sp'] - 2.2) * t * .8 + .55 * t * t
        h = p['img'].height; y = min(y, FLOOR - 1 - h / 2 + (cy % 3))         # se posan en el suelo
        r = p['img'].rotate(p['spin'] * min(t, 4), expand=True, resample=Image.NEAREST)
        if alpha < 1: r.putalpha(r.getchannel('A').point(lambda v: int(v * alpha)))
        im.alpha_composite(r, (int(round(x - r.width / 2)), int(round(y - r.height / 2)))) if 0 <= round(x - r.width / 2) < BW - r.width and 0 <= round(y - r.height / 2) < H - r.height else None
    for s in shards:
        x = s['p'][0] + s['v'][0] * t * .8; y = s['p'][1] + s['v'][1] * t * .8 + .6 * t * t; y = min(y, FLOOR)
        xi, yi = int(round(x)), int(round(y))
        c = s['col']; a = int(c[3] * alpha)
        for dx, dy in ((0, 0), (1, 0), (0, 1), (1, 1)) if s['big'] else ((0, 0),):
            if 0 <= xi + dx < BW and 0 <= yi + dy < H: im.putpixel((xi + dx, yi + dy), c[:3] + (a,))
    return im

# --- halo, polvo y vetas -----------------------------------------------------------------------------------
HALO = [0, 0, 0, 40, 160, 150, 115, 80, 50, 30, 15, 0]
def glow_frame(f, rnd_seed=9):
    cv = Canvas(BW, H)
    if HALO[f] > 0:
        for y in range(H):
            for x in range(BW):
                r = math.hypot((x + .5 - CX) / 34.0, (y + .5 - 38) / 31.0)
                if r < 1:
                    a = int(HALO[f] * (1 - r) ** 1.5)
                    if a > 4: cv.put(x, y, rgba(AMB[3], a))
    t = f - NCR - 1
    if t >= 0:                                                              # polvo dorado que se abre desde el centro
        rnd = random.Random(rnd_seed)
        for i in range(22):
            ang = rnd.uniform(0, 2 * math.pi); r0 = rnd.uniform(4, 12); sp = rnd.uniform(2.2, 4.2)
            x = CX + math.cos(ang) * (r0 + sp * (t + 1)); y = 42 + math.sin(ang) * (r0 + sp * (t + 1)) * .8 - t * .8
            a = max(0, 235 - t * 36)
            if 1 <= x < BW - 1 and 1 <= y < H - 2 and a > 0:
                cv.put(round(x), round(y), rgba(AMB[4] if i % 3 == 0 else AMB[3], a))
    return cv

def veins_frame(data, f):
    pts, mx = data; cv = Canvas(cz.VW, cz.VH)
    t = f - NCR - 1
    for (s, x, y, kind) in pts:
        if f < NCR:
            col = AMB[4] if (f + s // 3) % 2 == 0 and f >= 1 else AMB[3] if kind else AMB[2]; a = 255
            xx, yy = x, y
        elif t < 0: col = AMB[4]; a = 255; xx, yy = x, y
        else:
            u = ((x * 37 + y * 91 + s * 13) % 100) / 100.0
            if u < (t + 1) / 8.5: continue
            col = AMB[4] if t < 3 else AMB[3]; a = 255 - t * 25; xx, yy = x, y - t // 2
        if f < NCR or t < 0: cv.put(xx, yy + 1, (74, 40, 20, 150))
        cv.put(xx, yy, rgba(col, a))
    return cv

def stage(back, front, glow, veins, body, with_veins):
    im = Image.new('RGBA', (BW, H), C('#2b2318')); im.alpha_composite(Image.new('RGBA', (BW, H - 51), C('#33291c')), (0, 51))
    if with_veins: im.alpha_composite(veins.img(), (0, cz.VOY))
    im.alpha_composite(glow.img())
    sh = Canvas(BW, H)
    for y in range(53, 57):
        for x in range(CX - 22, CX + 23):
            if ((x + .5 - CX) / 21.0) ** 2 + ((y + .5 - 54.5) / 1.8) ** 2 <= 1: sh.put_out(x, y, (24, 20, 37, 90))
    im.alpha_composite(sh.img()); im.alpha_composite(as_img(back)); im.alpha_composite(body, FROG_AT); im.alpha_composite(as_img(front)); return im

def build(back, front, seed):
    back, front = wide(back), wide(front); mask = set(back.px)
    pts = crack_data(mask, seed); asg, n = pieces_of(mask, 8, seed)
    B, F = [], []
    for k in range(NCR):
        dx = (0, 1, -1, 1)[k]; B.append(shiftx(back, dx)); F.append(shiftx(cracked(front, pts, k, mask), dx))
    fr, seam = fractured(cracked(front, pts, NCR - 1, mask), asg); B.append(back); F.append(fr)
    parts, shards = burst(back, front, asg, n, random.Random(seed))
    for t in range(1, NBU + 1):
        B.append(Canvas(BW, H))
        im = burst_frame(parts, shards, t); c = Canvas(BW, H); c.px = {}
        F.append(im)                                                         # imagen ya compuesta (PIL)
    return B, F

def as_img(x): return x if isinstance(x, Image.Image) else x.img()

if __name__ == '__main__':
    out = os.path.join(HERE, 'rotura'); os.makedirs(out, exist_ok=True)
    body = n3.frog(); b1, f1 = cz.capsule1(); b3, f3, _ = cz.capsule3()
    B1, F1 = build(b1, f1, 3); B3, F3 = build(b3, f3, 5)
    G = [glow_frame(f) for f in range(NF)]; data = cz.floor_veins(); V = [veins_frame(data, f) for f in range(NF)]
    def sheet(path, frames, w, h):
        s = Image.new('RGBA', (w * len(frames), h))
        for i, fr in enumerate(frames): s.alpha_composite(as_img(fr), (i * w, 0))
        s.save(path)
    for name, fr in (('Fx_AmberCapsule_Break_Back', B1), ('Fx_AmberCapsule_Break_Front', F1), ('Fx_AmberCapsule3_Break_Back', B3),
                     ('Fx_AmberCapsule3_Break_Front', F3), ('Fx_AmberCapsule_Break_Glow', G)): sheet(os.path.join(out, name + '.png'), fr, BW, H)
    sheet(os.path.join(out, 'Fx_AmberFloorVeins_Break.png'), V, cz.VW, cz.VH)
    def frog_for(f): return body if f <= NCR else body
    runs = {'nivel_I-II': [stage(B1[f], F1[f], G[f], V[f], body, False) for f in range(NF)],
            'nivel_III': [stage(B3[f], F3[f], G[f], V[f], body, True) for f in range(NF)]}
    os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True); S = 6
    for name, ims in runs.items():
        big = [i.resize((i.width * S, i.height * S), Image.NEAREST).convert('RGB') for i in ims]; dur = MS[:]; dur[-1] = 700
        big[0].save(os.path.join(HERE, 'gif', f'rotura_{name}.gif'), save_all=True, append_images=big[1:], duration=dur, loop=0)
    s2 = 3; cw, ch = BW * s2, H * s2
    contact = Image.new('RGB', (cw * 4 + 10, ch * 6 + 14), (20, 16, 12))
    for r, (name, ims) in enumerate(runs.items()):
        for f, im in enumerate(ims):
            contact.paste(im.resize((cw, ch), Image.NEAREST).convert('RGB'), (2 + (f % 4) * (cw + 2), 2 + (r * 3 + f // 4) * (ch + 2)))
    contact.save(os.path.join(HERE, 'vista_previa_rotura.png')); print('ok', contact.size)
