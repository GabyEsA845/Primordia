"""Cápsula de ámbar (piso 3): animación de cristalización del jugador (el tiempo se detiene mientras dura; lo hace el código).

El cristal SUBE desde el suelo (12 cuadros): 8 de formación (la resina sube y endurece; su superficie brilla) y 4 de
remate (un destello cruza la cápsula, halo dorado tenue y motas que suben). Nivel III: la cápsula es la misma, algo mayor
(con más grietas), y por el suelo brotan vetas de ámbar a los lados mientras se forma.
Hojas (celda 58x60, pies de la criatura en y = 51, eje x = 29; la criatura va ENTRE Back y Front):
  Fx_AmberCapsule_Form_{Back,Front}  (nivel I-II)   Fx_AmberCapsule3_Form_{Back,Front}  (nivel III)
  Fx_AmberCapsule_Form_Glow (halo y motas, detrás de todo; igual en I-II y III)
  Fx_AmberFloorVeins_Form (nivel III; celda 96x16, eje x = 48, su fila 0 = y 44 de la celda de la cápsula)
Uso: python generar_cristalizacion.py
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import generar_capsula_nivel3 as n3
import generar_capsula_d as g
from generar_capsula_nivel3 import AMB, rgba, W, H, CX, Cap, make_mask, back_layer, front_layer, cracks, reflections, bubbles, outline_in
from base_piso3 import *
from PIL import Image

FROG_AT = (12, 23)
LEVELS = [.08, .2, .34, .48, .62, .76, .9, 1.0]                       # formación: cuánto ha subido la resina
GLINT = [.1, .4, .7, 1.0]                                              # remate: posición del destello
HALO = [70, 130, 100, 60]                                              # remate: fuerza del halo
NF = len(LEVELS) + len(GLINT)
MS = [90] * len(LEVELS) + [110] * len(GLINT)
VW, VH, VOY = 96, 16, 44                                               # lienzo de las vetas y su desfase vertical

def shift(cv, dx, dy):
    out = Canvas(W, H)
    for (x, y), c in cv.px.items(): out.px[(x + dx, y + dy)] = c
    return out

def copy(cv):
    out = Canvas(W, H); out.px = dict(cv.px); return out

def bounds(cv):
    ys = [y for (x, y) in cv.px]; return min(ys), max(ys)

def clip(cv, level, top_cap):
    """Deja solo lo que ya ha subido la resina. top_cap: pone la superficie brillante y su contorno."""
    y0, y1 = bounds(cv); ys = int(round(y1 - level * (y1 - y0))) if level < 1 else y0
    out = Canvas(W, H)
    for (x, y), c in cv.px.items():
        if y >= ys: out.px[(x, y)] = c
    if top_cap and level < 1:
        for x in range(W):
            c = cv.px.get((x, ys))
            if c is not None and c != OUTL and c[3] > 0:
                out.px[(x, ys)] = rgba(AMB[4] if (x + ys) % 3 == 0 else AMB[3], 255)
                if (x, ys - 1) not in out.px: out.px[(x, ys - 1)] = OUTL
                if level < .9 and (x, ys + 1) in out.px and out.px[(x, ys + 1)] != OUTL and x % 2 == 0:
                    out.px[(x, ys + 1)] = rgba(AMB[3], 235)
    return out

def glint(front, g_pos, cy):
    out = copy(front)
    for (x, y), c in list(front.px.items()):
        if c != OUTL and c[3] > 0 and abs((x - CX) + (y - cy) * .7 - (g_pos * 80 - 40)) < 1.8:
            out.px[(x, y)] = rgba(AMB[4], 230)
    return out

# --- capa del nivel III (la cápsula D, mayor y con más grietas) ---------------------------------------
def capsule3():
    mask, _ = make_mask(3, 54, 39.5, 15.5, 21, 4.2, 43, 4.2)
    cap = Cap(mask); rnd = random.Random(61)
    fr = front_layer(cap)
    pool = [p for p in cap.mask if cap.depth(*p) == 2 and not n3.in_creature(*p)]
    for i in range(10):                                                    # vetas fibrosas oscuras, como en la cápsula base
        x, y = rnd.choice(pool); ang = math.atan2(40 - y, CX - x) + rnd.uniform(-.5, .5)
        for s in range(rnd.randint(9, 14)):
            if not n3.in_creature(round(x), round(y)): cap.put(fr, round(x), round(y), AMB[0] if i % 3 else AMB[3], 215 if i % 3 else 190)
            ang += math.sin(s * .9 + i) * .35; x += math.cos(ang); y += math.sin(ang) * .9
    cracks(cap, fr, rnd, 7); reflections(cap, fr); bubbles(cap, fr, rnd, 5)
    for x in range(CX - 13, CX + 14): cap.put(fr, x, 52, AMB[3], 110)     # menisco en la base
    outline_in(fr)
    return back_layer(cap), fr, cap

def capsule1():
    return shift(g.back(), 6, 10), shift(g.front(), 6, 10)

# --- vetas de ámbar en el suelo (nivel III) ----------------------------------------------------------
def floor_veins():
    """Lista de (paso, x, y, color) de varias vetas que salen de la base hacia los lados, sobre el suelo."""
    rnd = random.Random(8); out = []; mx = 0
    for side in (-1, 1):
        for i in range(3):
            x, y = 48 + side * (13 + i * 2.5), 8.0 + i * 1.2
            a = (0.0, .25, .45)[i] + rnd.uniform(-.1, .1); L = (30, 22, 15)[i] + rnd.randint(-2, 2)
            for s in range(L):
                out.append((s, round(x), round(y), 0))
                if s % 4 == 2: out.append((s, round(x), round(y) - 1, 1))
                x += side * math.cos(a) * .95; y += math.sin(a) * .55 + math.sin(s * .7 + i) * .22
                y = min(13.0, max(6.0, y)); mx = max(mx, s)
                if s == L // 2 and i != 1:                                  # una ramita
                    bx, by, ba = x, y, a + rnd.choice((-.8, .8))
                    for t in range(7):
                        out.append((s + t, round(bx), round(by), 0)); bx += side * math.cos(ba) * .9; by += math.sin(ba) * .5
                        by = min(13.0, max(6.0, by)); mx = max(mx, s + t)
    return out, mx

def veins_frame(data, f):
    pts, mx = data; cv = Canvas(VW, VH)
    prog = min(1.0, max(0.0, (f - 1) / 8.0)); head = prog * (mx + 1)
    for (s, x, y, kind) in pts:
        if s > head: continue
        near = head - s < 2 and prog < 1
        cv.put(x, y + 1, (74, 40, 20, 150))                                 # sombra bajo la veta
        cv.put(x, y, rgba(AMB[4] if near else AMB[3] if kind else AMB[2], 255))
    return cv

# --- remate: halo y motas ------------------------------------------------------------------------------
def glow_frame(f):
    k = f - len(LEVELS)
    if k < 0: return Canvas(W, H)
    cv = n3.glow(None, HALO[k], motes=0)
    rnd = random.Random(5)
    for i in range(9):
        side = rnd.choice((-1, 1)); x = CX + side * rnd.randint(23, 28); y0 = rnd.randint(34, 52); sp = rnd.choice((2, 3))
        y = y0 - k * sp; a = 235 - k * 40
        if 1 <= x < W - 1 and y > 2: cv.put(x, y, rgba(AMB[4], a)); cv.put(x, y + 1, rgba(AMB[3], a // 2))
    return cv

# --- montaje --------------------------------------------------------------------------------------------
def frames_for(back, front, cy):
    bf, ff = [], []
    for f in range(NF):
        if f < len(LEVELS): bf.append(clip(back, LEVELS[f], False)); ff.append(clip(front, LEVELS[f], True))
        else: bf.append(back); ff.append(glint(front, GLINT[f - len(LEVELS)], cy))
    return bf, ff

def stage(f, back, front, glow, veins, body, with_veins):
    SW = VW; im = Image.new('RGBA', (SW, H), C('#2b2318')); px0 = (SW - W) // 2
    floor = Image.new('RGBA', (SW, H - 51), C('#33291c')); im.alpha_composite(floor, (0, 51))
    if with_veins: im.alpha_composite(veins.img(), (0, VOY))
    sh = Canvas(W, H)
    for y in range(53, 57):
        for x in range(CX - 22, CX + 23):
            if ((x + .5 - CX) / 21.0) ** 2 + ((y + .5 - 54.5) / 1.8) ** 2 <= 1: sh.put_out(x, y, (24, 20, 37, 90))
    cell = Image.new('RGBA', (W, H)); cell.alpha_composite(glow.img()); cell.alpha_composite(sh.img())
    cell.alpha_composite(back.img()); cell.alpha_composite(body, FROG_AT); cell.alpha_composite(front.img())
    im.alpha_composite(cell, (px0, 0)); return im

if __name__ == '__main__':
    out = os.path.join(HERE, 'cristalizacion'); os.makedirs(out, exist_ok=True)
    body = n3.frog(); b1, f1 = capsule1(); b3, f3, cap3 = capsule3()
    B1, F1 = frames_for(b1, f1, 40); B3, F3 = frames_for(b3, f3, 40)
    G = [glow_frame(f) for f in range(NF)]; data = floor_veins(); V = [veins_frame(data, f) for f in range(NF)]
    from pixel import save_sheet
    for name, fr in (('Fx_AmberCapsule_Form_Back', B1), ('Fx_AmberCapsule_Form_Front', F1), ('Fx_AmberCapsule3_Form_Back', B3),
                     ('Fx_AmberCapsule3_Form_Front', F3), ('Fx_AmberCapsule_Form_Glow', G)):
        save_sheet(os.path.join(out, name + '.png'), fr)
    sheet = Image.new('RGBA', (VW * NF, VH)); [sheet.alpha_composite(v.img(), (i * VW, 0)) for i, v in enumerate(V)]
    sheet.save(os.path.join(out, 'Fx_AmberFloorVeins_Form.png'))
    runs = {'nivel_I-II': [stage(f, B1[f], F1[f], G[f], V[f], body, False) for f in range(NF)],
            'nivel_III': [stage(f, B3[f], F3[f], G[f], V[f], body, True) for f in range(NF)]}
    os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True); S = 6; bg = C('#2b2318')
    for name, ims in runs.items():
        big = [i.resize((i.width * S, i.height * S), Image.NEAREST).convert('RGB') for i in ims]
        dur = MS + []; dur[-1] = 700
        big[0].save(os.path.join(HERE, 'gif', f'cristalizacion_{name}.gif'), save_all=True, append_images=big[1:], duration=dur, loop=0)
    s2 = 3; cw, ch = VW * s2, H * s2
    contact = Image.new('RGB', (cw * 6 + 14, ch * 4 + 14), (20, 16, 12))
    for r, (name, ims) in enumerate(runs.items()):
        for f, im in enumerate(ims):
            contact.paste(im.resize((cw, ch), Image.NEAREST).convert('RGB'), (2 + (f % 6) * (cw + 2), 2 + (r * 2 + f // 6) * (ch + 2)))
    contact.save(os.path.join(HERE, 'vista_previa_cristalizacion.png')); print('ok', contact.size)
