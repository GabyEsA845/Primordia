"""Cables de la Megarachne, versión 2 (tanda 6l). Skill primordia-sprites.

Encargo (2026-11-18): (1) ARRANQUE del cable: abanico de seda que sale del borde de la copa del Nido y se junta en el
cable, para tapar el corte recto; (2) TEXTURA del cable menos plana: haz de hebras de seda en vez de una cinta con
rombos (parecía una pasarela).

Medidas del código (MegarachneBoss.Cables.cs): cable de 1,8 u de ancho (= 29 px), LineRenderer en mosaico. La textura
antigua (32×14) se estiraba al doble a lo ancho; la nueva mide 64×29 y va a su tamaño (material mainTextureScale.x =
16/64 = 0.25: una vuelta de la textura cada 4 u).

Uso: python generar_cables_megarachne.py piezas       (exporta el estilo B elegido: Effects/Cable_*, Nido/Nido_CableRoot)
     python generar_cables_megarachne.py propuestas   (hoja de prueba: estilos A, B, C)
     python generar_cables_megarachne.py escena        (la sala de prueba simulada: actual y A, B, C)
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
MT = os.path.normpath(os.path.join(HERE, '..', '..'))
sys.path.insert(0, os.path.join(MT, 'floor3-environment'))
sys.path.insert(0, HERE)
from base_piso3 import *                               # noqa: F401,F403
from PIL import Image, ImageDraw
import generar_nido_megarachne as NI

SILK = [(118, 110, 138), (160, 154, 180), (204, 199, 220), (234, 231, 244), (252, 251, 255)]
TL, TH = 64, 29                                        # tramo del mosaico: 64 px de largo, 29 de ancho (1,8 u)
MID = (TH - 1) / 2

# --- geometría del código (MegarachneBoss.Cables.cs, SetArena) -------------------------------------------------
NAMES = ['NO', 'N', 'NE', 'SO', 'SE']
ANG = {'NO': 205, 'N': 268, 'NE': 330, 'SO': 140, 'SE': 35}
OUTER = {'NO': (-12, 2), 'N': (0, 4), 'NE': (12, 2), 'SO': (-13, -7), 'SE': (13, -7)}
STUMP_LIFT, SIG_H, SIG_PIV, ANCHOR_FR = 1.5, 66, 18, .8      # Obstacle_Sigillaria 48×66, pivote a 18 px del pie

def cable_world(name):
    """(inicio en la rama, fin en la Sigillaria) en unidades, con el pie del árbol en (0, 0)."""
    a = math.radians(ANG[name])
    s = (60 * math.cos(a) / 16, (64 - 40 * math.sin(a)) / 16)
    ox, oy = OUTER[name]
    py = oy + STUMP_LIFT
    e = (ox, py - SIG_PIV / 16 + ANCHOR_FR * SIG_H / 16)
    return s, e

def cable_dir_px(name):
    s, e = cable_world(name)
    dx, dy = e[0] - s[0], -(e[1] - s[1])
    L = math.hypot(dx, dy)
    return dx / L, dy / L

# =================================================================================================
# (2) Textura del cable
# =================================================================================================
STYLES = {
    # (desplazamiento, amplitud, vueltas por tramo, fase, grosor) de cada hebra
    # A: cordón de 3 hebras gruesas con una vuelta larga
    'A': dict(strands=[(0, 5, 1, 0, 2), (0, 5, 1, 2.09, 2), (0, 5, 1, 4.19, 2)], veil=22, fibers=4, env=7),
    # B: haz de hebras casi paralelas que se cruzan poco (recomendado)
    'B': dict(strands=[(-5, 1.5, 1, 0, 1), (-2, 2, 1, 1.4, 2), (1, 2, 1, 2.8, 1), (4, 1.5, 1, 4.1, 2),
                       (6, 1, 2, 5.0, 1), (0, 6, 1, .6, 1), (-1, 5, 1, 3.7, 1)], veil=30, fibers=9, env=8),
    # C: el haz de B con un velo lechoso de seda alrededor
    'C': dict(strands=[(-5, 1.5, 1, 0, 1), (-2, 2, 1, 1.4, 2), (1, 2, 1, 2.8, 1), (4, 1.5, 1, 4.1, 2),
                       (6, 1, 2, 5.0, 1), (0, 6, 1, .6, 1), (-1, 5, 1, 3.7, 1)], veil=80, fibers=6, env=10),
}

GROW_ORDER = [2, 1, 3, 0, 4, 5, 6]                   # en Cable_Grow las hebras de B salen en este orden

def cable_tile(style='B', k=0, grow=1.0, state='normal'):
    """Tramo del cable (TL×TH, en mosaico). grow (0-1): del hilo fino al haz (Cable_Grow). state: normal | burning."""
    S = STYLES[style]
    im = Image.new('RGBA', (TL, TH), (0, 0, 0, 0)); p = im.load()
    env = max(1.5, S['env'] * grow)
    veil = S['veil'] * min(1, grow * 1.4)
    for x in range(TL):                                   # velo: más denso en el centro, sin bordes rectos
        e = env + .8 * grow * math.sin(x / TL * 2 * math.pi * 3 + 1)
        for y in range(TH):
            d = abs(y - MID) / e
            if d <= 1:
                a = int(veil * (1 - d * d * .6))
                if a > 4: p[x, y] = SILK[3] + (a,)
    rf = random.Random(5)                                 # pelusas sueltas que salen del haz
    for j in range(S['fibers'] if grow > .6 else 0):
        x0 = rf.randrange(TL); side = rf.choice((-1, 1)); L = rf.randint(2, 4); sl = rf.choice((-1, 1))
        y0 = MID + side * (env - 1)
        for i in range(L):
            x, y = (x0 + sl * i) % TL, round(y0 + side * (i + 1))
            if 0 <= y < TH: p[x, y] = SILK[2] + (150 - i * 25,)
    segs = []                                             # hebras: de atrás (oscuras) a delante (claras)
    n = len(S['strands'])
    for i, (off, amp, f, ph, th) in enumerate(S['strands']):
        if style == 'B' and grow < 1:
            g = min(1, max(0, (grow - GROW_ORDER.index(i) / n * .8) / .35))
            if g <= 0: continue
            off, amp, th = off * g, amp * g, th if g > .6 else 1
        prev = MID + off + amp * math.sin(-2 * math.pi * f / TL + ph)
        for x in range(TL):
            t = 2 * math.pi * f * x / TL + ph
            y = MID + off + amp * math.sin(t)
            z = .6 * math.cos(t) - (y - MID) / 9           # delante y arriba (hacia la luz) = más claro
            segs.append((z, x, prev, y, th))
            prev = y
    segs.sort(key=lambda s: s[0])
    for z, x, yp, y, th in segs:
        col = SILK[4] if z > .55 else SILK[3] if z > .05 else SILK[2] if z > -.45 else SILK[1]
        lo = SILK[2] if z > .05 else SILK[1] if z > -.45 else SILK[0]
        a = 255 if z > -.45 else 200
        y0, y1 = sorted((round(yp), round(y)))
        for yy in range(y0, y1 + 1):
            p[x, yy] = col + (a,)
            if th == 2 and yy == y1 and yy + 1 < TH: p[x, yy + 1] = lo + (a,)
    if state == 'burning':                                # al rojo bajo las llamas: el velo y las pelusas se van
        rnd = random.Random(40 + k)
        for x in range(TL):
            for y in range(TH):
                c = p[x, y]
                if not c[3]: continue
                if c[3] < 180: p[x, y] = (0, 0, 0, 0); continue
                t = rnd.random()                          # cada hebra conserva su luz; pocas chispas y huecos
                lvl = 3 if c[:3] == SILK[4] else 2 if c[:3] in (SILK[3], SILK[2]) else 1
                if t < .12: lvl = min(3, lvl + 1)
                elif t > .9: lvl = 0
                p[x, y] = (C('#3a2224') if lvl == 0 else EMB[lvl])[:3] + (255,)
        return im
    if grow >= 1:
        for x in ((11 + k * 9) % TL, (43 + k * 5) % TL):  # gotas de rocío sobre las hebras de delante
            ys = [yy for yy in range(TH) if p[x, yy][3] == 255 and p[x, yy][:3] == SILK[4]]
            if ys: p[x, ys[0]] = (255, 255, 255, 255)
    return im

EMB = [C(c) for c in EMBER]
CHARC = [(20, 14, 18), (34, 26, 30), (58, 46, 50), (90, 76, 78)]

def charred_tile(k=0):
    """Cable quemado (negro y fino, TL×TH para el mismo LineRenderer): 2 hebras retorcidas y ascuas que laten."""
    im = Image.new('RGBA', (TL, TH), (0, 0, 0, 0)); p = im.load()
    for off, amp, ph in ((-1, 1.5, 0), (1, 1.5, 2.6)):
        prev = None
        for x in range(TL):
            y = round(MID + off + amp * math.sin(2 * math.pi * x / TL + ph))
            for yy in (range(min(prev, y), max(prev, y) + 1) if prev is not None else (y,)):
                p[x, yy] = CHARC[2] + (255,)
            p[x, y + 1] = CHARC[0] + (255,)
            prev = y
    rnd = random.Random(9)
    for j in range(7):
        x = rnd.randrange(TL); ys = [y for y in range(TH) if p[x, y][3]]
        if not ys: continue
        on = (j + k) % 3
        if on: p[x, ys[0]] = (EMB[3] if on == 2 else EMB[2])[:3] + (255,)
    for j in range(3):                                    # hilachas sueltas
        x = rnd.randrange(TL)
        for i in range(2): p[(x + i) % TL, round(MID + 3 + i)] = CHARC[1] + (200,)
    return im

def thin_tile(k=0):
    """Hilo fino que tiende al tejer (TL×4): una hebra con su sombra y un brillo que recorre el hilo."""
    im = Image.new('RGBA', (TL, 4), (0, 0, 0, 0)); p = im.load()
    for x in range(TL):
        y = 1 + (1 if (x // 16) % 4 == 2 else 0)          # leve vuelta: no es una regla recta
        p[x, y] = SILK[3] + (245,)
        p[x, y + 1 if y == 1 else y - 1] = SILK[1] + (150,)
    for i in range(3):
        x = (k * 21 + i) % TL
        ys = [y for y in range(4) if p[x, y][3] > 200]
        if ys: p[x, ys[0]] = SILK[4] + (255,)
    return im

def old_tile():
    return Image.open(os.path.join(HERE, 'Effects', 'Cable_Normal.png')).convert('RGBA').crop((0, 0, 32, 14))

# =================================================================================================
# (1) Arranque: abanico de seda (capa de la celda grande, pivote = pie del tronco)
# =================================================================================================
LW, LH = 256, 224                                      # celda de la capa de arranques
LFX, LFY = 128, 188                                    # pie del tronco en esa celda
OX, OY = LFX - NI.FX, LFY - NI.FY                      # desplazamiento celda del árbol -> celda de la capa

FANS = {
    'A': dict(n=6, spread=60, r=(8, 16), peel=(4, 18), veil=0, cross=False, core=2),
    'B': dict(n=9, spread=88, r=(12, 28), peel=(6, 38), veil=0, cross=False, core=2),
    'C': dict(n=9, spread=88, r=(12, 28), peel=(6, 38), veil=55, cross=True, core=2),
}

def _line(p, pts, col, w=1):
    last = None
    for (x, y) in pts:
        q = (round(x), round(y))
        if q == last: continue
        last = q
        if 0 <= q[0] < LW and 0 <= q[1] < LH:
            p[q] = col
            if w == 2 and q[1] + 1 < LH: p[q[0], q[1] + 1] = SILK[1] + (col[3],)

def _curve(a, b, sag=1.5, n=None):
    n = n or int(math.hypot(b[0] - a[0], b[1] - a[1]) * 3) + 2
    mx, my = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2 + sag
    return [((1 - t) ** 2 * a[0] + 2 * (1 - t) * t * mx + t * t * b[0],
             (1 - t) ** 2 * a[1] + 2 * (1 - t) * t * my + t * t * b[1]) for t in [i / (n - 1) for i in range(n)]]

def in_crown(x, y):
    return NI.crown_mask(x - OX, y - OY, 5)

def fan(name, style='B', seed=0):
    """Abanico de un cable en la capa LW×LH. Las hebras se despegan del cable a distintas distancias y se pegan a
    las hojas del borde de la copa con una gotita de seda; un par de hebras del núcleo siguen el cable por encima
    de las hojas hasta la punta de la rama."""
    F = FANS[style]
    im = Image.new('RGBA', (LW, LH), (0, 0, 0, 0)); p = im.load()
    rnd = random.Random(sum(map(ord, name)) * 7 + seed * 31)
    ax, ay = NI.anchor(name); ax += OX; ay += OY
    dx, dy = cable_dir_px(name); nx, ny = -dy, dx
    s_rim = 0
    while in_crown(ax + dx * s_rim, ay + dy * s_rim) and s_rim < 30: s_rim += 1
    bx, by = -dx, -dy                                      # hacia dentro de la copa
    n = F['n']
    threads = []
    for j in range(n):
        f = j / (n - 1) * 2 - 1                            # -1 .. 1, de un lado al otro
        psi = math.radians(F['spread'] * f + rnd.uniform(-6, 6))
        cx, cy = bx * math.cos(psi) - by * math.sin(psi), bx * math.sin(psi) + by * math.cos(psi)
        r = rnd.uniform(*F['r'])
        A = (ax + dx * s_rim * .5 + cx * r, ay + dy * s_rim * .5 + cy * r)
        for _ in range(20):                                # si cae fuera de la copa, se mete hacia dentro
            if in_crown(*A): break
            A = (A[0] + bx * 1.5, A[1] + by * 1.5)
        s = s_rim + F['peel'][0] + (F['peel'][1] - F['peel'][0]) * (1 - abs(f)) ** .7 * rnd.uniform(.7, 1)
        o = f * 7 + rnd.uniform(-1, 1)                     # sale del lado del cable que le toca
        Q = (ax + dx * s + nx * o, ay + dy * s + ny * o)
        threads.append((A, Q, abs(f)))
    if F['veil']:                                          # lámina de seda entre las hebras
        lay = Image.new('RGBA', (LW, LH), (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
        poly = [t[0] for t in threads] + [t[1] for t in reversed(threads)]
        d.polygon(poly, fill=SILK[3] + (F['veil'],))
        im.alpha_composite(lay)
    for A, Q, e in sorted(threads, key=lambda t: -t[2]):  # las de fuera primero (más tenues)
        col = (SILK[3] if e < .5 else SILK[2]) + (240 if e < .7 else 205,)
        _line(p, _curve(Q, A, sag=1 + e * 1.5), col)
    if F['cross']:                                         # hebra de amarre que cruza el abanico
        mids = [(A[0] * .55 + Q[0] * .45, A[1] * .55 + Q[1] * .45 + 1) for A, Q, e in threads]
        for a, b in zip(mids, mids[1:]): _line(p, _curve(a, b, sag=.8), SILK[2] + (200,))
    for k in range(F['core']):                             # núcleo del cable que se mete entre las hojas
        o = (k - (F['core'] - 1) / 2) * 3.5
        a = (ax + dx * (s_rim + 8) + nx * o, ay + dy * (s_rim + 8) + ny * o)
        b = (ax + nx * o * .3 - dx * 2, ay + ny * o * .3 - dy * 2)
        _line(p, _curve(a, b, sag=.5), (SILK[3] if k == 0 else SILK[2]) + (235,))
    for A, Q, e in threads:                                # gotitas de seda donde se pegan a las hojas
        x, y = round(A[0]), round(A[1])
        for qx, qy, c in ((0, 0, SILK[4]), (1, 0, SILK[3]), (0, 1, SILK[2])):
            if 0 <= x + qx < LW and 0 <= y + qy < LH: p[x + qx, y + qy] = c + (255,)
    x, y = round(ax - dx * 2), round(ay - dy * 2)          # nudo en la punta de la rama
    for qx, qy, c in ((0, 0, SILK[4]), (1, 0, SILK[2]), (0, 1, SILK[2])): p[x + qx, y + qy] = c + (255,)
    return im

def fan_layer(style):
    im = Image.new('RGBA', (LW, LH), (0, 0, 0, 0))
    for nm in NAMES: im.alpha_composite(fan(nm, style))
    return im

# =================================================================================================
# Simulación de la sala de prueba (como la dibuja Unity: LineRenderer en mosaico + sprites por orden)
# =================================================================================================
X0, X1, Y0, Y1 = -15.5, 15.5, -5.5, 9.0                # ventana del mundo (u)
PPU = 16

def w2p(x, y): return ((x - X0) * PPU, (Y1 - y) * PPU)

def draw_line(img, s, e, tex, width, scale_x, curve):
    """LineRenderer en modo Tile: u = distancia * scale_x (mundo), v a lo ancho."""
    p = img.load(); tp = tex.load(); tw, th = tex.size
    sx, sy = w2p(*s); ex, ey = w2p(*e)
    L = math.hypot(ex - sx, ey - sy); ux, uy = (ex - sx) / L, (ey - sy) / L
    xmin, xmax = int(min(sx, ex)) - 20, int(max(sx, ex)) + 20
    ymin, ymax = int(min(sy, ey)) - 20, int(max(sy, ey)) + 20
    for Y in range(max(0, ymin), min(img.height, ymax)):
        for X in range(max(0, xmin), min(img.width, xmax)):
            qx, qy = X + .5 - sx, Y + .5 - sy
            along = qx * ux + qy * uy
            if along < 0 or along > L: continue
            perp = -qx * uy + qy * ux
            t = along / L
            hw = width * curve(t) * PPU / 2
            if abs(perp) > hw: continue
            u = (along / PPU * scale_x) % 1
            v = (perp / hw + 1) / 2
            c = tp[min(tw - 1, int(u * tw)), min(th - 1, int(v * th))]
            if c[3]:
                b = p[X, Y]; a = c[3] / 255
                p[X, Y] = tuple(int(b[i] * (1 - a) + c[i] * a) for i in range(3)) + (255,)

def lerp_curve(keys):
    def f(t):
        for (t0, v0), (t1, v1) in zip(keys, keys[1:]):
            if t <= t1: return v0 + (v1 - v0) * (t - t0) / (t1 - t0)
        return keys[-1][1]
    return f

OLD_CURVE = lerp_curve([(0, .45), (.12, 1), (.9, 1), (1, .7)])
NEW_CURVE = lerp_curve([(0, 1), (.9, 1), (1, .7)])

def scene(style=None):
    """style None = como está ahora en Test_BossMegarachne; 'A'/'B'/'C' = propuesta."""
    import generar_megarachne as MG
    W, H = int((X1 - X0) * PPU), int((Y1 - Y0) * PPU)
    img = MG.smoke_floor(W, H, seed=6)
    tree = Image.open(os.path.join(HERE, 'Nido', 'Nido_Tree_Wet.png')).convert('RGBA')
    web = Image.open(os.path.join(HERE, 'Nido', 'Nido_Web.png')).convert('RGBA')
    sig = Image.open(os.path.join(MT, '..', '..', 'Assets', '_Primordia', 'Art', 'Floor3', 'floor3-environment', 'Smoke',
                                  'props', 'Obstacle_Sigillaria.png')).convert('RGBA')
    fx, fy = w2p(0, 0); fx, fy = int(fx), int(fy)
    trunk = tree.crop((0, 144, 160, 176)); crown = tree.crop((0, 0, 160, 144))
    for nm in sorted(NAMES, key=lambda n: -OUTER[n][1]):   # orden 4: Sigillarias y pie del tronco
        X, Y = w2p(OUTER[nm][0], OUTER[nm][1] + STUMP_LIFT)
        img.alpha_composite(sig, (int(X) - 24, int(Y) - (SIG_H - SIG_PIV)))
    img.alpha_composite(trunk, (fx - 80, fy - 164 + 144))
    tex = old_tile() if style is None else cable_tile(style)
    for nm in NAMES:                                       # orden 19: cables
        s, e = cable_world(nm)
        if style is None: draw_line(img, s, e, tex, 1.8, .5, OLD_CURVE)
        else: draw_line(img, s, e, tex, 1.8, PPU / TL, NEW_CURVE)
    img.alpha_composite(crown, (fx - 80, fy - 164))       # orden 20: copa
    img.alpha_composite(web, (fx - 80, fy - 164))         # orden 21: telaraña del Nido
    if style: img.alpha_composite(fan_layer(style), (fx - LFX, fy - LFY))   # orden 22: arranques
    sheet = Image.open(os.path.join(HERE, 'Sheets', 'Megarachne_Idle.png')).convert('RGBA')
    spr = sheet.crop((0, 4 * 128, 128, 5 * 128)).resize((96, 96), Image.NEAREST)   # Side, BodyScale 0.75
    s, e = cable_world('NE'); mx, my = w2p(s[0] + (e[0] - s[0]) * .62, s[1] + (e[1] - s[1]) * .62)
    img.alpha_composite(spr, (int(mx) - 48, int(my) - 48))
    return img

# =================================================================================================
# Hojas para la usuaria
# =================================================================================================
def _font(sz):
    sys.path.insert(0, os.path.join(HERE, '..', 'arthropleura'))
    from generar_escala_arthropleura import font as F
    return F(sz)

def label(d, xy, text, sz=18, col=(255, 240, 200)):
    d.text(xy, text, fill=col, font=_font(sz), stroke_width=3, stroke_fill=(10, 10, 14))

def closeup(style, name, box):
    """Recorte de la escena alrededor de un arranque."""
    img = scene(style)
    s, e = cable_world(name); X, Y = w2p(*s)
    return img.crop((int(X) + box[0], int(Y) + box[1], int(X) + box[2], int(Y) + box[3]))

def propuestas():
    import generar_megarachne as MG
    BGC = (34, 36, 30, 255)
    z = 3
    rows = [(None, 'Actual'), ('A', 'A · cordón de 3 hebras + abanico corto'),
            ('B', 'B · haz de hebras finas + abanico ancho (recomendado)'),
            ('C', 'C · haz con velo lechoso + abanico con lámina de seda')]
    boxes = {'SE': (-40, -45, 80, 45), 'NO': (-84, -45, 36, 45)}
    tile_w = TL * 3
    col_w = [tile_w * z + 30, 120 * z + 20, 120 * z + 20]
    row_h = 90 * z + 50
    W = sum(col_w) + 20; H = row_h * len(rows) + 60
    out = Image.new('RGBA', (W, H), BGC); d = ImageDraw.Draw(out)
    label(d, (14, 10), 'Megarachne · cables v2 (6l): textura del cable y arranque en abanico · hoja de prueba ×3', 22)
    label(d, (col_w[0] + 20, 38), 'arranque SE (abajo dcha.)', 16)
    label(d, (col_w[0] + col_w[1] + 20, 38), 'arranque NO (izquierda)', 16)
    label(d, (20, 38), 'tramo del cable ×3 repeticiones (sobre el suelo)', 16)
    for r, (st, txt) in enumerate(rows):
        y = 70 + r * row_h
        label(d, (20, y), txt, 18, (150, 230, 255))
        floor = MG.smoke_floor(tile_w, 48, seed=2)
        tex = old_tile().resize((32, 28), Image.NEAREST) if st is None else cable_tile(st)
        for i in range(tile_w // tex.width + 1): floor.alpha_composite(tex, (i * tex.width, (48 - tex.height) // 2))
        out.alpha_composite(floor.resize((tile_w * z, 48 * z), Image.NEAREST), (20, y + 34))
        x = col_w[0] + 10
        for nm in ('SE', 'NO'):
            c = closeup(st, nm, boxes[nm])
            out.alpha_composite(c.resize((c.width * z, c.height * z), Image.NEAREST), (x + 10, y + 30))
            x += 120 * z + 20
    path = os.path.join(HERE, 'vista_previa_6l_propuestas.png')
    out.convert('RGB').save(path); print(path, out.size)

def escena():
    z = 2
    imgs = [(None, 'Actual (Test_BossMegarachne)'), ('A', 'A'), ('B', 'B (recomendado)'), ('C', 'C')]
    w, h = int((X1 - X0) * PPU) * z, int((Y1 - Y0) * PPU) * z
    out = Image.new('RGBA', (w * 2 + 30, h * 2 + 110), (20, 20, 22, 255)); d = ImageDraw.Draw(out)
    label(d, (12, 8), 'Megarachne · la sala de prueba simulada (cables, copa, telaraña y arranques en su orden) ×2', 22)
    for i, (st, txt) in enumerate(imgs):
        x, y = 10 + (i % 2) * (w + 10), 50 + (i // 2) * (h + 30)
        out.alpha_composite(scene(st).resize((w, h), Image.NEAREST), (x, y + 22))
        label(d, (x + 4, y), txt, 18, (150, 230, 255))
    path = os.path.join(HERE, 'vista_previa_6l_escena.png')
    out.convert('RGB').save(path); print(path, out.size)

def vstack(frames):
    w, h = frames[0].size
    im = Image.new('RGBA', (w, h * len(frames)), (0, 0, 0, 0))
    for i, f in enumerate(frames): im.paste(f, (0, i * h))
    return im

def hstack(frames):
    w, h = frames[0].size
    im = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames): im.paste(f, (i * w, 0))
    return im

def piezas():
    """Exporta las piezas definitivas (estilo B) y la vista previa vista_previa_6l_piezas.png."""
    import generar_megarachne as MG
    E, N_ = os.path.join(HERE, 'Effects'), os.path.join(HERE, 'Nido')
    sets = {
        'Cable_Normal': [cable_tile('B')],
        'Cable_Thin': [thin_tile(k) for k in range(3)],
        'Cable_Grow': [cable_tile('B', grow=max(.05, i / 5)) for i in range(6)],
        'Cable_Burning': [cable_tile('B', k, state='burning') for k in range(3)],
        'Cable_Charred': [charred_tile(k) for k in range(3)],
    }
    for name, fr in sets.items(): vstack(fr).save(os.path.join(E, name + '.png'))   # cuadros en VERTICAL
    roots = [fan(nm, 'B') for nm in NAMES]
    hstack(roots).save(os.path.join(N_, 'Nido_CableRoot.png'))
    BGC = (34, 36, 30, 255); z = 3
    rows = []
    for name, fr in sets.items():
        tiles = []
        for f in fr:
            fl = MG.smoke_floor(TL * 2, 40, seed=2)
            for i in range(2): fl.alpha_composite(f, (i * TL, (40 - f.height) // 2))
            tiles.append(fl)
        rows.append((name + ' (%d)' % len(fr), tiles))
    tree = Image.open(os.path.join(N_, 'Nido_Tree_Wet.png')).convert('RGBA')
    web = Image.open(os.path.join(N_, 'Nido_Web.png')).convert('RGBA')
    views = []
    for r in roots + [fan_layer('B')]:
        b = MG.smoke_floor(LW, LH, seed=4); b.alpha_composite(tree, (OX, OY)); b.alpha_composite(web, (OX, OY))
        b.alpha_composite(r); views.append(b)
    W = max(20 + max(len(t) for _, t in rows) * (TL * 2 * z + 12), 20 + 3 * (LW * 2 + 12))
    H = 50 + len(rows) * (40 * z + 40) + 2 * (LH * 2 + 40) + 20
    out = Image.new('RGBA', (W, H), BGC); d = ImageDraw.Draw(out)
    label(d, (14, 10), 'Megarachne · cables v2 (6l, estilo B): piezas exportadas', 22)
    y = 50
    for name, tiles in rows:
        label(d, (20, y), name + ' · 2 repeticiones ×3', 16, (150, 230, 255))
        for i, t in enumerate(tiles):
            out.alpha_composite(t.resize((t.width * z, t.height * z), Image.NEAREST), (20 + i * (TL * 2 * z + 12), y + 22))
        y += 40 * z + 40
    titles = ['Nido_CableRoot ' + n for n in NAMES] + ['los 5 juntos']
    for i, v in enumerate(views):
        x, yy = 20 + (i % 3) * (LW * 2 + 12), y + (i // 3) * (LH * 2 + 40)
        label(d, (x, yy), titles[i] + ' ×2', 16, (150, 230, 255))
        out.alpha_composite(v.resize((LW * 2, LH * 2), Image.NEAREST), (x, yy + 22))
    path = os.path.join(HERE, 'vista_previa_6l_piezas.png'); out.convert('RGB').save(path); print(path, out.size)
    os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True)
    MG.gif(os.path.join(HERE, 'gif', 'Cable_Grow.gif'), rows[2][1], 160, MG.BG, scale=3)
    MG.gif(os.path.join(HERE, 'gif', 'Cable_Burning.gif'), rows[3][1], 120, MG.BG, scale=3)
    MG.gif(os.path.join(HERE, 'gif', 'Cable_Charred.gif'), rows[4][1], 140, MG.BG, scale=3)

if __name__ == '__main__':
    cmds = sys.argv[1:] or ['propuestas', 'escena']
    if 'piezas' in cmds: piezas()
    if 'propuestas' in cmds: propuestas()
    if 'escena' in cmds: escena()
