"""Efectos e iconos de las Bocas del piso 3 de Primordia (pantano carbonífero).

- Escupitajo venenoso (rana): bola viscosa en vuelo, bolitas de la Camada (III), charcos de veneno pequeño (rastro) y
  grande (al reventar; son también los charcos de veneno de las reacciones) y el reventón de radio 1,5 u.
- Peonza (Diplocaulus): remolino del giro (radio dibujado 1,5 u; el juego lo escala) y destello al devolver (II).
- Chorro de vinagre (vinagrillo): tramo que se repite a lo largo del chorro, salida, impacto y Salpicadura (III).
- Iconos de las tres Bocas en 32 y 16 px, como los del piso 2.
Usa la bola viscosa y el modelo en volumen de generar_cuerpos_piso3.py. Skill primordia-sprites (pixel.py).

Uso: python generar_efectos_bocas_piso3.py [carpeta_salida]   (por defecto, la carpeta de este script)
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
_argv = sys.argv; sys.argv = sys.argv[:1]
import generar_cuerpos_piso3 as B                     # bola viscosa, modelo del Diplocaulus, paletas
sys.argv = _argv
from pixel import *

OUT = sys.argv[1] if len(sys.argv) > 1 else HERE
BG = B.BG
VENOM = B.VENOM
VIN = ['#7a5a30', '#b8904e', '#e2c878', '#f6ecb4', '#fffbe6']     # vinagre: paja clara (lejos del lima y del fuego)
WIND = [B.DIPLO[2], B.DIPLO[3], B.DIPLO[4], '#fff8ec']           # remolino: salmón a crema

def metaball(lobes):
    return lambda x, y: sum(r * r / max(.01, (x - cx) ** 2 + (y - cy) ** 2) for cx, cy, r in lobes) >= 1.0

def drop(cv, x, y, r, pal=VENOM, outline=True):
    """Gota suelta con contorno y brillo."""
    t = Canvas(cv.w, cv.h)
    if r < 1.1:
        t.put(x, y, C(pal[1])); t.put(x - 1, y, C(pal[2]))
    else:
        ellipse(t, x, y, r, r * .9, pal[:4])
        t.put_in(x - r * .45, y - r * .45, C(pal[3]))
    if outline: t.outline()
    for p, c in t.px.items():
        if p not in cv.px or c != OUTL: cv.px[p] = c

# =================================================================================================
# Escupitajo venenoso
# =================================================================================================
def proj_spit(i, n=6):
    """Bola viscosa volando a la derecha: tiembla, se estira hacia atrás y gotea."""
    cv = Canvas(24)
    w = i / n
    B.venom_blob(cv, 13.5, 11.5, 4.6 + .25 * math.sin(w * 2 * math.pi), wob=w,
                 tail=(5.5 - .8 * math.sin(w * 2 * math.pi), 11.8 + .6 * math.cos(w * 2 * math.pi)))
    for k in range(2):                                   # gotas que se quedan atrás
        ph = (w + k * .5) % 1
        drop(cv, 9 - ph * 7, 16 + ph * 4, 1.3 - ph * .6)
    return cv

def proj_spit_small(i, n=4):
    cv = Canvas(12)
    w = i / n
    B.venom_blob(cv, 6.5, 5.6, 2.6 + .15 * math.sin(w * 2 * math.pi), wob=w, tail=(2.6, 6))
    return cv

VMID = '#4a8c1c'                                    # tono intermedio del charco (fondo más hondo)

def puddle_mask(cx, cy, r, seed, grow=1.0):
    rnd = random.Random(seed)
    lobes = [(cx, cy, r * .55 * grow)]
    for k in range(5):
        a = k * 2 * math.pi / 5 + rnd.uniform(-.4, .4)
        d = r * rnd.uniform(.38, .5) * grow
        lobes.append((cx + math.cos(a) * d, cy + math.sin(a) * d, r * rnd.uniform(.26, .36) * grow))
    return metaball(lobes)

def puddle(cv, cx, cy, r, seed, grow=1.0, bubbles=(), alpha=235, dry=0.0):
    """Charco de veneno visto desde arriba: plano, sin contorno (como los charcos del escenario), borde oscuro abajo
    a la derecha y claro arriba a la izquierda, fondo con manchas más hondas, brillos y burbujas. dry: se seca."""
    inside = puddle_mask(cx, cy, r, seed, grow * (1 - .35 * dry))
    pts = {(x, y) for y in range(cv.h) for x in range(cv.w) if inside(x + .5, (y + .5 - cy) / .72 + cy)}
    pal = VENOM if dry < .5 else [VENOM[0], VENOM[0], VMID, VENOM[1]]
    rnd = random.Random(seed * 3)
    deep = [(cx + rnd.uniform(-.4, .4) * r, cy + rnd.uniform(-.3, .3) * r * .72, r * rnd.uniform(.18, .3)) for _ in range(3)]
    for (x, y) in pts:
        edge_dr = (x + 1, y) not in pts or (x, y + 1) not in pts
        edge_ul = (x - 1, y) not in pts or (x, y - 1) not in pts
        inner = all((x + dx, y + dy) in pts for dx in (-2, 2) for dy in (-2, 2))
        c = pal[0] if edge_dr else pal[2] if edge_ul else pal[1]
        if inner and r > 8 and any((x - a) ** 2 + ((y - b_) / .8) ** 2 < rr * rr for a, b_, rr in deep): c = VMID
        cv.put(x, y, C(c, alpha))
    if dry < .5:                                          # brillos: dos rayitas arriba a la izquierda
        for k in range(max(2, int(r * .35))):
            x, y = cx - r * .4 + k, cy - r * .32
            if (int(x), int(y)) in pts: cv.put(x, y, C(pal[3], alpha))
        if r > 8:
            for k in range(3):
                x, y = cx - r * .1 + k, cy - r * .2
                if (int(x), int(y)) in pts: cv.put(x, y, C(pal[2], alpha))
    for (bx, by, br) in bubbles:                          # burbujas: cúpula pequeña o anillo al reventar
        bx, by = int(bx), int(by)
        if (bx, by) not in pts: continue
        if br <= 0: cv.put(bx, by, C(VENOM[2]))
        elif br < 1.5:
            for (dx, dy, c) in ((0, 0, VENOM[2]), (1, 0, VENOM[2]), (0, 1, VENOM[1]), (1, 1, VENOM[0])): cv.put(bx + dx, by + dy, C(c))
            cv.put(bx, by, C(VENOM[3]))
        else:
            for (dx, dy) in ((-1, 0), (1, 0), (0, -1), (0, 1)): cv.put(bx + dx, by + dy, C(VENOM[2]))
    return pts

def puddle_sheets(name, n, r, seed):
    c = n / 2
    appear = []
    for k, g in enumerate((.45, 1.12, 1.0)):
        cv = Canvas(n); puddle(cv, c, c, r, seed, g)
        if k == 1:                                         # salpicón al caer
            for a in range(0, 360, 60):
                drop(cv, c + math.cos(math.radians(a + 20)) * r * .88, c + math.sin(math.radians(a + 20)) * r * .66, max(.8, r / 8))
        appear.append(cv)
    idle = []
    for i in range(4):
        cv = Canvas(n)
        bb = [(c + r * .35 * math.cos(k * 2.1 + .5), c + r * .22 * math.sin(k * 2.1 + .5), ((i + k) % 4) * .9)
              for k in range(3 if r > 8 else 1)]
        puddle(cv, c, c, r, seed, 1.0 + .03 * math.sin(i * math.pi / 2), bubbles=bb)
        idle.append(cv)
    fade = []
    for k, d in enumerate((.35, .7, 1.0)):
        cv = Canvas(n); puddle(cv, c, c, r, seed, 1.0, alpha=int(235 - 70 * d), dry=d); fade.append(cv)
    return {f'{name}_Appear': appear, f'{name}_Idle': idle, f'{name}_Fade': fade}

def spit_burst(i):
    """Reventón de radio 1,5 u (24 px): la bola se aplasta, estalla en un anillo de gotas que vuelan y caen."""
    cv = Canvas(56); c = 28
    rnd = random.Random(5)
    rays = [(k * 2 * math.pi / 11 + rnd.uniform(-.2, .2), rnd.uniform(.8, 1.05), rnd.uniform(.8, 1.2)) for k in range(11)]
    if i == 0:
        B.venom_blob(cv, c, c, 5.5, wob=.2)
        return cv
    if i == 1:
        ellipse(cv, c, c, 8, 6.5, VENOM); cv.outline()
        for (a, d, s) in rays: drop(cv, c + math.cos(a) * 10 * d, c + math.sin(a) * 8 * d, 2.2 * s)
        sparks(cv, c - 2, c - 2, 2)
        return cv
    rr = (0, 0, 13, 18, 21, 22, 22)[i]
    if i <= 4:
        ellipse(cv, c, c, max(1, 7 - 2.5 * (i - 1)), max(1, 5.5 - 2 * (i - 1)), VENOM)   # núcleo que se deshace
        if i < 4: cv.outline()
    for (a, d, s) in rays:
        x, y = c + math.cos(a) * rr * d, c + math.sin(a) * rr * d * .82
        if i <= 4:
            drop(cv, x, y, max(.8, (2.4 - .4 * (i - 2)) * s))
            for k in (1, 2):                                # estela de cada gota
                cv.put_out(c + math.cos(a) * (rr * d - 2.2 * k), c + math.sin(a) * (rr * d - 2.2 * k) * .82, C(VENOM[2 if k == 1 else 1]))
        else:                                               # gotas que caen y salpican el suelo
            t = Canvas(56); puddle(t, x, y, 2.2 * s * (1.2 if i == 5 else .9), int(a * 100), 1.0, alpha=235 if i == 5 else 170)
            for p, v in t.px.items(): cv.px.setdefault(p, v)
    return cv

# =================================================================================================
# Peonza
# =================================================================================================
def whirl(i, n=4, size=48, r_out=22):
    """Remolino de la Peonza, en bucle: 3 rachas en arco que giran en el sentido del Diplocaulus (horario) y motas
    de barro que salen despedidas. El radio dibujado es 1,5 u; el juego lo escala al radio del ataque."""
    cv = Canvas(size); c = size / 2
    base = i * (120 / n)
    for k in range(3):
        a0 = base + k * 120
        for s in range(0, 95, 3):                           # arco: grueso y claro en la cabeza, fino en la cola
            a = math.radians(a0 + s)
            head = 1 - s / 95
            for dr in range(3 if head > .55 else 2 if head > .25 else 1):
                rr = r_out - 1 - dr - (1 - head) * 3
                col = WIND[3] if head > .8 and dr == 0 else WIND[2] if head > .45 else WIND[1] if head > .2 else WIND[0]
                cv.put(c + math.cos(a) * rr, c + math.sin(a) * rr, C(col, 255 if head > .2 else 190))
        for s in range(0, 50, 4):                           # racha interior más corta
            a = math.radians(a0 + 60 + s)
            cv.put(c + math.cos(a) * (r_out - 8), c + math.sin(a) * (r_out - 8), C(WIND[1] if s < 30 else WIND[0], 220))
    rnd = random.Random(3)
    for k in range(6):                                      # barro y gotas despedidas
        a = math.radians(rnd.uniform(0, 360) + i * 30)
        d = r_out - 4 + ((i + k) % 4) * 1.2
        cv.put(c + math.cos(a) * d, c + math.sin(a) * d, C(B.MUD[2] if k % 2 else '#9cc8a0'))
    return cv

def deflect(i):
    """Destello al devolver un proyectil (Peonza II): chispa y media luna salmón."""
    cv = Canvas(16); c = 8
    if i < 3: sparks(cv, c, c, (1, 2, 1)[i])
    rr = (3, 5, 6.5, 7)[i]
    for s in range(-60, 61, 12):
        a = math.radians(180 + s)
        cv.put(c + math.cos(a) * rr, c + math.sin(a) * rr, C(WIND[3] if i < 2 else WIND[1], 255 if i < 3 else 160))
    return cv

# =================================================================================================
# Chorro de vinagre
# =================================================================================================
def stream(i, n=4):
    """Tramo de 16×8 que se repite a lo largo del chorro (hacia la derecha, en bucle): núcleo claro de 4 px, borde de
    paja, pulsos que avanzan 4 px por cuadro y gotitas sueltas. Encaja consigo mismo en x (periodo de 16 px)."""
    cv = Canvas(16, 8)
    for x in range(16):
        ph = ((x - 4 * i) % 16) / 16
        bulge = 1 if ph < .3 else 0
        for y in range(2 - bulge, 6 + bulge):
            core = 3 <= y <= 4
            col = VIN[4] if core and ph < .12 else VIN[3] if (core or y in (2, 5)) and ph < .55 else VIN[2] if core or y in (2, 5) else VIN[1]
            if y in (1, 6): col = VIN[1]
            cv.put(x, y, C(col, 255 if 2 <= y <= 5 else 215))
    for (x0, y0) in ((2, 0), (11, 7), (7, 0)):              # gotitas que se separan
        cv.put((x0 + 4 * i) % 16, y0, C(VIN[2], 220))
    return cv

def muzzle(i):
    cv = Canvas(12); c = 6
    for k in range(5):
        a = math.radians(-40 + k * 20)
        L = (2, 4, 5)[i] - abs(k - 2) * .6
        for t in range(int(L) + 1): cv.put(c - 2 + math.cos(a) * t, c + math.sin(a) * t, C(VIN[3 if t < L - 1 else 2]))
    if i == 0: cv.put(c - 2, c, C(VIN[4]))
    return cv

def hit(i):
    """Impacto en el final del chorro (en bucle): salpica hacia atrás y a los lados. Centrado en el punto de impacto."""
    cv = Canvas(16); c = 8
    ellipse(cv, c, c, 2.2 + .4 * (i % 2), 2.0, VIN[1:5])
    for k in range(6):
        a = math.radians(110 + k * 28 + i * 9)
        d = 3.5 + ((i + k) % 4) * 1.1
        cv.put(c + math.cos(a) * d, c + math.sin(a) * d, C(VIN[3] if (i + k) % 2 else VIN[2]))
        cv.put(c + math.cos(a) * (d - 1.2), c + math.sin(a) * (d - 1.2), C(VIN[2], 200))
    return cv

def splash(i):
    """Salpicadura (Chorro III): estallido de vinagre de radio 1 u (16 px) cada 0,5 s."""
    cv = Canvas(32); c = 16
    rr = (4, 9, 13, 15, 15)[i]
    if i < 2: ellipse(cv, c, c, 4 - i, 3.4 - i, VIN[1:5])
    rnd = random.Random(9)
    for k in range(10):
        a = k * 2 * math.pi / 10 + rnd.uniform(-.2, .2)
        d = rr * rnd.uniform(.8, 1.0)
        x, y = c + math.cos(a) * d, c + math.sin(a) * d * .85
        if i < 3:
            drop(cv, x, y, 1.4 - .3 * i, pal=VIN[1:5], outline=False)
            cv.put_out(c + math.cos(a) * (d - 2), c + math.sin(a) * (d - 2) * .85, C(VIN[2], 200))
        else:
            cv.put(x, y, C(VIN[1], 200 if i == 3 else 120)); cv.put(x + 1, y, C(VIN[2], 200 if i == 3 else 110))
    return cv

# =================================================================================================
# Iconos
# =================================================================================================
def icon_spit(n):
    cv = Canvas(n); s = n / 32
    B.venom_blob(cv, 17 * s, 13 * s, 8.4 * s, wob=.35, tail=(5 * s, 22 * s))
    if n >= 32:
        drop(cv, 25, 26, 1.6); drop(cv, 9, 28, 1.1)
    return cv

def scaled(parts, k):
    for p in parts: p.c = tuple(v * k for v in p.c); p.r = tuple(v * k for v in p.r)
    return parts

def fit(cv, n, dx=0, dy=0):
    """Centra en un lienzo de n×n lo dibujado en un lienzo grande."""
    xs = [x for x, y in cv.px]; ys = [y for x, y in cv.px]
    ox = (n - (max(xs) - min(xs) + 1)) // 2 - min(xs) + dx; oy = (n - (max(ys) - min(ys) + 1)) // 2 - min(ys) + dy
    out = Canvas(n)
    for (x, y), c in cv.px.items(): out.put(x + ox, y + oy, c)
    return out

def icon_top(n):
    """El Diplocaulus visto casi desde arriba, girando en diagonal, con dos rachas del giro alrededor."""
    k = n / 32 * (.92 if n >= 32 else .8)
    with B.camera(72):
        cv, info = B.trace(scaled(B.diplo_parts(tuck=1, crouch=.3), k), 64, -35, (32, 32), B.DIPLO_PAL)
    cv.outline(); cv.clean()
    out = fit(cv, n)
    c = n / 2
    for kk in (0, 1):
        for st in range(0, 75, 3 if n >= 32 else 6):
            a = math.radians(160 + kk * 180 + st)
            rr = n * .42
            out.put_out(c + math.cos(a) * rr, c + math.sin(a) * rr * .9, C(WIND[3] if st > 50 else WIND[2] if st > 25 else WIND[1]))
    return out

def icon_jet(n):
    """El vinagrillo con el abdomen alzado sobre el lomo, soltando el chorro hacia delante."""
    k = n / 32 * (1.12 if n >= 32 else 1.0)
    pose = dict(raise_=1, crouch=.4, claw=.8)
    with B.camera(B.WHIP_ELEV):
        parts = scaled(B.whip_parts(**pose), k)
        cv, info = B.trace(parts, 64, 0, (32, 32), B.WHIP_PAL)
        cv.outline(); cv.clean()
        lines = [([tuple(v * k for v in p) for p in pts], cols) for pts, cols in B.whip_lines(**pose)]
        if n < 32: lines = lines[6:]                       # en 16 px, sin patas: solo palpos y látigo
        B.draw_lines(cv, info, 0, (32, 32), lines)
        _, (tf, tu, phi) = B.whip_skeleton(1, 0, .4)
        tx, ty, _ = B.screen_of(0, (32, 32), tf * k, 0, tu * k)
    for ang in (-12, 4, 20):                               # chorro: abanico de gotitas hacia delante
        a = math.radians(ang)
        for j in range(1, int(10 * k) + 1, 2 if n >= 32 else 3):
            col = VIN[4] if j < 3 else VIN[3] if j < 7 else VIN[2]
            cv.put(tx + 1 + math.cos(a) * j, ty + math.sin(a) * j, C(col))
    return fit(cv, n)

# =================================================================================================
# Salida
# =================================================================================================
EFFECTS_MS = {'Proj_PoisonSpit': 70, 'Proj_PoisonSpitSmall': 80, 'Fx_PoisonPuddle': 90, 'Fx_PoisonSpit_Burst': 60,
              'Fx_SpinningTop_Whirl': 50, 'Fx_SpinningTop_Deflect': 50, 'Fx_VinegarJet_Stream': 60,
              'Fx_VinegarJet_Muzzle': 50, 'Fx_VinegarJet_Hit': 60, 'Fx_VinegarJet_Splash': 60}

if __name__ == '__main__':
    E = {}
    E['Proj_PoisonSpit'] = [proj_spit(i) for i in range(6)]
    E['Proj_PoisonSpitSmall'] = [proj_spit_small(i) for i in range(4)]
    E.update(puddle_sheets('Fx_PoisonPuddleSmall', 16, 6.2, 11))
    E.update(puddle_sheets('Fx_PoisonPuddleBig', 48, 19.5, 23))
    E['Fx_PoisonSpit_Burst'] = [spit_burst(i) for i in range(7)]
    E['Fx_SpinningTop_Whirl'] = [whirl(i) for i in range(4)]
    E['Fx_SpinningTop_Deflect'] = [deflect(i) for i in range(4)]
    E['Fx_VinegarJet_Stream'] = [stream(i) for i in range(4)]
    E['Fx_VinegarJet_Muzzle'] = [muzzle(i) for i in range(3)]
    E['Fx_VinegarJet_Hit'] = [hit(i) for i in range(4)]
    E['Fx_VinegarJet_Splash'] = [splash(i) for i in range(5)]
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    for name, frames in E.items():
        save_sheet(os.path.join(OUT, 'Effects', name + '.png'), frames)
        ms = next((v for k, v in EFFECTS_MS.items() if name.startswith(k)), 70)
        gif(os.path.join(OUT, 'gif', name + '.gif'), frames, ms, BG)
    preview(os.path.join(OUT, 'vista_previa_efectos_bocas.png'), E, BG, scale=3)
    I = {}
    for nm, fn in (('PoisonSpit', icon_spit), ('SpinningTop', icon_top), ('VinegarJet', icon_jet)):
        for n in (32, 16):
            I[f'Icon_{nm}_{n}'] = [fn(n)]
            save_sheet(os.path.join(OUT, 'Icons', f'Icon_{nm}_{n}.png'), [fn(n)])
    preview(os.path.join(OUT, 'vista_previa_iconos_bocas.png'), I, C('#232a3a'), scale=4, cols=1)
    print('Listo:', OUT)
