"""Tronco caído del piso 3 (paso 6, bloque 4: sala especial Tronco caído; ver README). El tocón está en generar_tocon.py.

Lepidodendron derribado y HUNDIDO A MEDIAS que cruza el lago de lodo profundo de lado a lado (2 casillas de alto: es el
paso seguro). Una versión por tramo del incendio (`<Swamp|Smoke|Burnt>/`).
- Tramos del lago (16×48, 6 variantes), cada uno con 3 hojas que comparten el dibujo del tronco:
  `_Idle` (8 cuadros, flota: el lodo de los costados sube y baja 1 px y las ondas recorren el tronco y se alejan),
  `_Step` (6 cuadros, lo pisa el jugador: el lodo le sube por los lados, salpica y vuelve) y `_Dip` (4 cuadros, el
  vecino de lo pisado: sube menos y sin salpicar).
- Tramos de la orilla (16×48, 2 variantes, quietos), extremo izquierdo (cepa con las raíces arrancadas, 40×64) y
  extremo derecho (copa partida con la horquilla y sus hojas, 48×64).
- Burbujas de lodo sueltas (`bubbles/`, las mismas en los 3 tramos): el código las suelta junto a los costados.
Hecho con la skill primordia-sprites (pixel.py): 16 px por unidad, luz arriba a la izquierda, contorno OUTL.

Uso: python generar_tronco.py
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
TEMP = os.path.normpath(os.path.join(HERE, '..'))
ENV = os.path.join(TEMP, 'floor3-environment')
sys.path.insert(0, ENV)
from base_piso3 import *
from PIL import Image
import generar_tilesets_piso3 as TS

MUDP = [C(c) for c in MUD]                        # #22160f #33221a #4a3324 #634830 #86683f
SHADOW = (24, 20, 37, 110)

LOG_H, BAND = 48, 8      # tramos: filas 8..39 = las dos casillas del tronco (pivote abajo a la izquierda, 8 px arriba)
END_H, BAND_E = 64, 16   # extremos: filas 16..47 (pivote abajo a la izquierda, 16 px arriba)
PH = .8                  # fase del perfil: la misma en todos los tramos y piezas, así encajan
IDLE_L = (0, 0, 1, 1, 1, 1, 0, 0)        # cuánto sube el lodo en cada cuadro (flotar)
STEP_L = (2, 3, 4, 4, 3, 1)              # al pisar
DIP_L = (1, 2, 2, 1)                     # el vecino de lo pisado
IDLE_MS, STEP_MS, DIP_MS = 160, 70, 90

LOG = {
    'Swamp': dict(bark=TRAMOS['Swamp']['bark'], moss=['#2c4424', '#3f6030', '#5c803c'], leaf=['#1e3a22', '#2e5a2c', '#4a7a38'],
                  wood=['#3a2c1c', '#5e4a30', '#8a7048', '#b89a66', '#dcc890'], root=TRAMOS['Swamp']['root'],
                  fung=['#4a3a22', '#7a6640', '#a89064', '#cbb88a'], kind='moss'),
    'Smoke': dict(bark=TRAMOS['Smoke']['bark'], moss=['#4a3e26', '#6a5530', '#8e7440'], leaf=['#3a3018', '#5c4c24', '#86703a'],
                  wood=['#3a2e1e', '#5e4c32', '#86704a', '#ae9466', '#d4bc88'], root=TRAMOS['Smoke']['root'],
                  fung=['#4a3a22', '#7a6640', '#a89064', '#cbb88a'], kind='dry'),
    'Burnt': dict(bark=CHAR, moss=['#4a4346', '#6d6466', '#958a88'], leaf=CHAR[1:4],
                  wood=CHAR, root=TRAMOS['Burnt']['root'], fung=['#2a2326', '#4a4346', '#6d6466', '#958a88'], kind='burnt'),
}

def lp(key):
    return {k: ([C(c) for c in v] if isinstance(v, list) else v) for k, v in LOG[key].items()}

def top_edge(x, off=0):  # periodo 16: 7..10
    return off + BAND - 1 + round(.9 + .9 * math.sin(2 * math.pi * x / 16 + PH) + .4 * math.sin(4 * math.pi * x / 16 + 2 * PH))

def bot_edge(x, off=0):  # periodo 16: 37..40
    return off + BAND + 32 - round(1.6 + 1.1 * math.sin(2 * math.pi * x / 16 + PH + 1.7) + .5 * math.sin(4 * math.pi * x / 16 + PH))

def wave(x, f, amp):     # onda que recorre el tronco hacia la derecha (periodo 16 px, 8 cuadros)
    return amp * math.sin(2 * math.pi * (x / 16 - f / 8))

# =================================================================================================
# El cuerpo del tronco
# =================================================================================================
def bark_px(x, y, t0, b0, P, seed=0):
    """Corteza en (x, y) de un tronco tumbado entre t0 y b0: cojinetes en rombo que se aprietan hacia los bordes
    (cilindro) y se desordenan un poco, luz arriba, franja de brillo mojado, sombra abajo y luz rebotada del lodo."""
    bark = P['bark']
    v = (y + .5 - t0) / (b0 + 1 - t0)
    s = math.asin(max(-1., min(1., 2 * v - 1))) / (math.pi / 2)
    yy = (s + 1) * 21 + .9 * math.sin(x * .83 + seed)
    row = int(yy // 6.5)
    lx = (x + (4 if row % 2 else 0) + (1 if (row * 7 + seed) % 5 == 0 else 0)) % 8 - 3.5
    ly = yy % 6.5 - 3.25
    dd = abs(lx) / 4 + abs(ly) / 3.25
    light = 2 if v < .07 else 3 if v < .16 else 4 if v < .3 else 3 if v < .5 else 2 if v < .68 else 1 if v < .84 else 0
    if light <= 1: c = bark[light] if dd < .97 else bark[max(0, light - 1)]
    elif dd > .9: c = bark[max(0, light - 1 - (light <= 2))]
    elif dd < .24 and light >= 3: c = bark[light - 2]
    else: c = bark[light] if dd < .66 else bark[max(0, light - 1)]
    if .18 < v < .25 and dd < .62 and (x * 3 + y) % 5:                       # brillo mojado
        c = blend(bark[4], (232, 240, 222) if P['kind'] != 'burnt' else (170, 160, 160), 70)
    elif .86 < v < .93 and dd < .9:                                          # luz rebotada del lodo
        c = blend(c, MUDP[3], 70)
    return c, v, dd

def draw_body(cv, xs, top, bot, P, seed, mudcoat=True):
    """Cuerpo con musgo/ceniza por arriba y, si está en el lodo, el costado de abajo manchado."""
    moss, kind = P['moss'], P['kind']
    n = pnoise(seed, 16, 4); rnd = random.Random(seed)
    for x in xs:
        t0, b0 = top(x), bot(x)
        for y in range(t0, b0 + 1):
            c, v, dd = bark_px(x, y, t0, b0, P, seed)
            m = n(x % 16, y)
            if kind == 'moss' and .05 < v < .45 and m > .58:
                c = moss[2] if v < .16 and m > .68 else moss[1] if m > .63 else moss[0]
            elif kind == 'dry' and .08 < v < .4 and m > .67:
                c = moss[2] if m > .75 and v < .18 else moss[1] if m > .71 else moss[0]
            elif kind == 'burnt':
                if dd > .9 and .3 < v < .6 and rnd.random() < .012: c = C(EMBER[0]) if rnd.random() < .5 else C(EMBER[1])
                elif v < .3 and m > .76: c = moss[0] if m < .82 else moss[1]
            if mudcoat:                                                          # manchas de lodo seco en el costado
                lim = .72 + .06 * math.sin(x * 1.9 + seed) + .04 * math.sin(x * 4.1)
                if v > lim: c = blend(c, MUDP[2], 90 if m > .5 else 60)
            cv.put(x, y, c)

def tuft(cv, x, y, ang, n, L, pal, spread=1.1):
    """Mechón de hojas de Lepidodendron (hojas largas como briznas) que sale de (x, y)."""
    for k in range(n):
        a = ang + (k / max(1, n - 1) - .5) * spread
        for j in range(1, L + 1):
            cv.put(x + math.cos(a) * j, y + math.sin(a) * j + .015 * j * j, pal[2] if j < 3 else pal[1] if j < L - 1 else pal[0])

def mud_ring(cv, x, y, w, f=0):
    """Anillo de lodo donde algo entra en el lodo (con un brillo que lo recorre)."""
    for dx in range(-w, w + 1): cv.put(x + dx, y, MUDP[3] if abs(dx) < w else MUDP[2])
    cv.put(x - w + 1 + (f // 2) % (2 * w - 1), y - 1, MUDP[4]); cv.put(x + w, y + 1, MUDP[1]); cv.put(x - w, y + 1, MUDP[1])

def stub(cv, p0, p1, w0, w1, P, tip=True):
    """Rama gruesa partida: cilindro de corteza con la punta de madera clara."""
    root(cv, [p0, ((p0[0] * 2 + p1[0]) / 3, (p0[1] * 2 + p1[1]) / 3), ((p0[0] + p1[0] * 2) / 3, (p0[1] + p1[1] * 2) / 3), p1],
         w0, w1, P['bark'], knots=True)
    if tip:
        wd = P['wood']; x, y = p1
        for (dx, dy, k) in ((0, 0, 3), (-1, 0, 2), (0, 1, 1), (1, 0, 2), (0, -1, 3)): cv.put(x + dx, y + dy, wd[k])
        cv.put(x, y, wd[0] if P['kind'] != 'burnt' else C(EMBER[0]))

def small_bracket(cv, x, y, w, P):
    """Hongo de repisa pequeño sobre el tronco."""
    fg = P['fung']
    for dx in range(w):
        u = (dx + .5) / w * 2 - 1
        cv.put(x + dx, y, fg[3] if u < 0 else fg[2])
        if abs(u) < .8: cv.put(x + dx, y - 1, fg[2] if u < 0 else fg[1])
        cv.put(x + dx, y + 1, fg[0])

def features_behind(cv, var, P, f):
    if var == 1:                                  # rama que sale hacia atrás y se mete en el lodo de arriba
        stub(cv, (9, top_edge(9) + 4), (5, 2), 5.6, 4.4, P, tip=False)
        mud_ring(cv, 5, 2, 3, f)

def features_front(cv, var, P, f, top, bot):
    kind = P['kind']; bark, wd, leaf, moss = P['bark'], P['wood'], P['leaf'], P['moss']
    if var == 0:                                  # musgo que cuelga del costado hasta el lodo
        for (x, L) in ((3, 4), (7, 6), (12, 3)):
            for j in range(L):
                y = bot(x) - 6 + j
                cv.put(x, y, (moss[1] if j < L - 1 else moss[0]) if kind != 'burnt' else (bark[3] if j % 2 else bark[2]))
    elif var == 1:                                # la base de la rama, con su cuenco, y un mechón de hojas
        stub(cv, (9, top_edge(9) + 4), (8, top_edge(9) - 1), 5.6, 5.4, P, tip=False)
        cv.put(9, top_edge(9) + 5, bark[0]); cv.put(10, top_edge(9) + 5, bark[0])
        if kind != 'burnt': tuft(cv, 12, top(12) + 6, -.2, 4, 5, leaf, .9)
    elif var == 2:                                # desconchón: la corteza se ha caído y se ve la madera clara
        cx, cy = 7.5, top(7) + 12
        for y in range(int(cy) - 4, int(cy) + 5):
            for x in range(2, 14):
                d = ((x + .5 - cx) / 5.4) ** 2 + ((y + .5 - cy) / 3.6) ** 2 + .15 * math.sin(x * 2.1 + y)
                if d < 1:
                    if kind == 'burnt': c = bark[3] if (x + 2 * y) % 5 == 0 else C(EMBER[0]) if (x * 7 + y) % 23 == 0 else bark[2]
                    else: c = wd[3] if y < cy - 1 and (x + y) % 4 else wd[2] if (y + x // 3) % 3 else wd[1]
                    cv.put(x, y, c)
                elif d < 1.35: cv.put(x, y, bark[0] if y > cy else bark[4] if kind != 'burnt' else bark[3])
        root(cv, [(5, bot(5) - 3), (4, bot(5) + 1), (6, bot(5) + 3), (8, bot(5) + 5)], 4.6, 3.2, P['root'], knots=True)
    elif var == 3:                                # más hundido: charco de lodo encima y una setilla
        for x in range(4, 12):
            for y in range(bot(x) - 4, bot(x) - 1):
                if (x - 7.5) ** 2 / 16 + (y - bot(x) + 2.5) ** 2 / 2.3 < 1: cv.put(x, y, MUDP[2] if y > bot(x) - 3 else MUDP[3])
        cv.put(5 + (f // 2) % 5, bot(5) - 4, MUDP[4])
        small_bracket(cv, 9, top(9) + 5, 4, P)
    elif var == 4:                                # hojas caídas de la copa sobre el tronco
        if kind == 'burnt':
            for (x, y, a) in ((2, 6, .3), (8, 9, -.25)):
                for j in range(7): cv.put(x + j * math.cos(a), top(x) + y + j * math.sin(a), bark[3] if j % 3 else bark[4])
        else:
            for (x, y, a, L) in ((1, 5, .25, 9), (4, 8, -.15, 10), (9, 6, .4, 6), (3, 11, .05, 8)):
                for j in range(L): cv.put(x + j * math.cos(a), top(x) + y + j * math.sin(a), leaf[2] if j < 3 else leaf[1] if j < L - 2 else leaf[0])
        small_bracket(cv, 11, top(11) + 13, 3, P)
    elif var == 5:                                # nudo hueco y dos hongos de repisa
        cx, cy = 6.5, top(6) + 13
        for y in range(int(cy) - 3, int(cy) + 4):
            for x in range(3, 11):
                d = ((x + .5 - cx) / 3.2) ** 2 + ((y + .5 - cy) / 2.6) ** 2
                if d < 1: cv.put(x, y, PASSAGE if d < .55 else bark[4] if (x < cx or y < cy) else bark[0])
        small_bracket(cv, 10, top(10) + 6, 4, P); small_bracket(cv, 12, top(12) + 9, 3, P)

def lake_tile(key, var, L, f, splash=-1):
    """Un cuadro de un tramo del lago (16×48). L: cuánto sube el lodo (0..3); f: fase de las ondas (0..7);
    splash: cuadro de la salpicadura al pisar (-1 = sin salpicadura)."""
    P = lp(key); cv = Canvas(16, LOG_H)
    bot = bot_edge
    if var == 3: bot = lambda x: bot_edge(x) - round(4 * math.sin(math.pi * (x + .5) / 16) ** 2)
    features_behind(cv, var, P, f)
    draw_body(cv, range(16), top_edge, bot, P, 11 + var)
    features_front(cv, var, P, f, top_edge, bot)
    n = pnoise(77, 16, 4)
    mud = lambda x, y: MUDP[1] if n(x, y % 16) > .6 else MUDP[2]
    for x in range(16):
        t0, b0 = top_edge(x), bot(x)
        # Arriba: el lodo de detrás tapa el canto del tronco al hundirse.
        ts = t0 - 1 + (L + 1) // 2 + round(wave(x, f, .5))
        for y in range(t0, ts + 1): cv.put(x, y, mud(x, y))
        cv.put(x, ts, MUDP[4] if (x - 2 * f) % 8 == 0 else MUDP[3])           # orilla del lodo, con un brillo que corre
        if ts >= t0: cv.put(x, ts + 1, blend(cv.px.get((x, ts + 1), MUDP[2]), MUDP[1], 120))
        # Abajo: el lodo sube por el costado de delante.
        bs = b0 - 2 - L + round(wave(x + 5, f, .6))
        if bs - 1 >= t0: cv.put(x, bs - 1, blend(cv.px.get((x, bs - 1), MUDP[2]), MUDP[1], 110))   # corteza mojada
        cv.put(x, bs, MUDP[3] if (x + 2 * f) % 7 else MUDP[4])
        for y in range(bs + 1, b0 + 1): cv.put(x, y, mud(x, y))
        cv.put(x, b0 + 1, MUDP[1])
        # Ondas que se alejan del tronco (dos a la vez, desfasadas).
        for k0 in (0, 4):
            k = (f + k0) % 8
            if k > 5: continue
            arc = math.sin(2 * math.pi * x / 16 + PH * 3 + k0) > -.2
            col = MUDP[4] if k < 2 and x % 5 == 0 else MUDP[3] if k < 4 else (MUDP[3] if x % 2 else None)
            if arc and col:
                if t0 - 4 - k >= 0: cv.put(x, t0 - 4 - k, col)
                if b0 + 5 + k < LOG_H: cv.put(x, b0 + 5 + k, col)
    if splash >= 0:                                                            # salpicadura al pisar
        s = splash
        for (x, y0, vx, vy) in ((3, top_edge(3), -.4, -3.2), (8, top_edge(8), .3, -3.8), (12, top_edge(12), .6, -2.8),
                                (4, bot(4), -.3, 2.6), (10, bot(10), .5, 3.0)):
            if s > 3: continue
            up = vy < 0
            px_, py_ = x + vx * s * 2, y0 + (vy * s + .9 * s * s * (1 if up else -1) * .5)
            if not up: py_ += 3
            cv.put(px_, py_, MUDP[4] if s < 2 else MUDP[3]); cv.put(px_ + 1, py_, MUDP[3])
            if s < 2: cv.put(px_, py_ + 1, MUDP[2]); cv.put(px_ + 1, py_ + 1, MUDP[1])
        r = 1 + s
        for x in range(16):
            if (x + s) % 3:
                if top_edge(x) - 2 - r >= 0: cv.put(x, top_edge(x) - 2 - r, MUDP[4] if s < 3 else MUDP[3])
                if bot(x) + 4 + r < LOG_H: cv.put(x, bot(x) + 4 + r, MUDP[4] if s < 3 else MUDP[3])
    return cv

def lake_sheets(key, var):
    return {f'Log_Mid_{var}_Idle': [lake_tile(key, var, IDLE_L[f], f) for f in range(8)],
            f'Log_Mid_{var}_Step': [lake_tile(key, var, STEP_L[s], (s * 2) % 8, s) for s in range(6)],
            f'Log_Mid_{var}_Dip': [lake_tile(key, var, DIP_L[s], (s * 2 + 1) % 8) for s in range(4)]}

def ground_tile(key, var):
    """Tramo de la orilla (16×48, quieto): el tronco apoyado en el suelo, con contorno y sombra."""
    P = lp(key); cv = Canvas(16, LOG_H)
    draw_body(cv, range(16), top_edge, bot_edge, P, 31 + var, mudcoat=False)
    if var == 0: features_front(cv, 0, P, 0, top_edge, bot_edge)
    else: small_bracket(cv, 6, top_edge(6) + 7, 4, P)
    for x in range(16):
        t0, b0 = top_edge(x), bot_edge(x)
        cv.put(x, t0 - 1, OUTL); cv.put(x, b0 + 1, OUTL)
        cv.put(x, b0 + 2, SHADOW)
        if x % 2: cv.put(x, b0 + 3, SHADOW)
    return cv

# =================================================================================================
# Extremos (64 de alto, la banda del tronco en las filas 16..47)
# =================================================================================================
def end_left(key):
    """Cepa con las raíces arrancadas (40×64): un disco alto de tierra y raíces enredadas, de pie, del que sale el
    tronco ensanchándose; raíces gruesas que se abren hacia fuera, raicillas, terrones y una piedra atrapada."""
    P = lp(key); cv = Canvas(40, END_H); rnd = random.Random(7)
    rt, wd, kind = P['root'], P['wood'], P['kind']
    flare = lambda x: max(0, round((20 - x) / 2.2))
    top = lambda x: top_edge(x, 8) - flare(x); bot = lambda x: bot_edge(x, 8) + flare(x)
    for (pts, w0) in ((((8, 10), (5, 6), (3, 4), (2, 2)), 4.4), (((6, 20), (3, 18), (2, 16), (1, 14)), 3.6),
                      (((7, 46), (4, 50), (3, 54), (2, 57)), 4.0), (((9, 55), (7, 58), (5, 60), (4, 62)), 3.2),
                      (((13, 6), (15, 3), (18, 2), (21, 1)), 3.2), (((14, 58), (16, 60), (19, 61), (23, 62)), 3.0),
                      (((5, 32), (2, 31), (1, 33), (1, 36)), 3.0)):
        root(cv, list(pts), w0, 1.4, rt, knots=True)
    draw_body(cv, range(14, 40), top, bot, P, 41, mudcoat=False)
    soil = [MUDP[0], MUDP[1], MUDP[2], rt[2], rt[3]]
    n = noise(5)
    for y in range(3, 61):                                 # el disco
        for x in range(3, 19):
            d = ((x + .5 - 10.5) / 7.6) ** 2 + ((y + .5 - 32) / 29) ** 2
            if d > 1 + .1 * math.sin(y * 1.3): continue
            v = n(x * 2, y * 2, 4)
            k = 3 if x < 8 and v > .45 else 2 if v > .5 else 1 if v > .3 else 0
            if d > .75 and x < 10: k = min(4, k + 1)       # canto iluminado
            cv.put(x, y, soil[k])
    for k in range(11):                                    # raíces enredadas en el disco
        x0, y0 = rnd.uniform(5, 16), rnd.uniform(6, 58); a = rnd.uniform(0, 6.28); Lr = rnd.uniform(5, 10)
        root(cv, [(x0, y0), (x0 + math.cos(a) * Lr * .4, y0 + math.sin(a) * Lr * .4 + 1), (x0 + math.cos(a) * Lr * .7, y0 + math.sin(a) * Lr * .7 - 1),
                  (x0 + math.cos(a) * Lr, y0 + math.sin(a) * Lr)], rnd.uniform(1.6, 2.6), 1.0, rt, box=(3, 3, 19, 61), knots=False)
    for (x, y) in ((6, 12), (12, 18), (7, 27), (14, 36), (8, 44), (12, 52), (10, 7)):   # raíces cortadas
        cv.put(x, y, wd[3] if kind != 'burnt' else C(EMBER[0])); cv.put(x + 1, y, wd[1]); cv.put(x, y + 1, wd[1]); cv.put(x - 1, y, wd[2])
    st = [C(c) for c in ('#2e2a30', '#45404a', '#615c66', '#827c88')]                   # piedra atrapada
    for y in range(38, 43):
        for x in range(13, 19):
            d = ((x + .5 - 15.8) / 2.9) ** 2 + ((y + .5 - 40.3) / 2.4) ** 2
            if d < 1: cv.put(x, y, st[3] if d < .3 and x < 16 else st[2] if y < 40 else st[1])
    if kind == 'moss':                                     # musgo en lo alto del disco
        for (x, y) in ((6, 6), (7, 5), (8, 5), (5, 8), (9, 6), (11, 4), (12, 5), (4, 13)): cv.put(x, y, P['moss'][1 + (x + y) % 2])
    for k in range(7):                                     # raicillas que cuelgan
        x, y = rnd.randrange(4, 17), rnd.randrange(8, 58)
        for j in range(rnd.randrange(3, 6)): cv.put(x - j // 2, y + j, rt[1] if j % 2 else rt[2])
    cv.outline(); cv.clean()
    for (x, y) in ((22, 7), (20, 59), (25, 60), (1, 46)):  # terrones caídos
        cv.put(x, y, soil[3]); cv.put(x + 1, y, soil[1]); cv.put(x, y + 1, OUTL); cv.put(x + 1, y + 1, OUTL)
    for x in range(16, 40):
        b0 = bot(x)
        for k in (1, 2):
            if cv.px.get((x, b0 + k)) is None: cv.put(x, b0 + k, SHADOW)
    return cv

def end_right(key):
    """Copa partida (48×64): el tronco se estrecha y acaba en astillas largas y claras con el corazón oscuro; arriba
    sale una horquilla gruesa (las ramas del Lepidodendron se parten en dos) con mechones de hojas, y otra rama
    rota cae hacia abajo."""
    P = lp(key); cv = Canvas(48, END_H); rnd = random.Random(9)
    wd, leaf, kind = P['wood'], P['leaf'], P['kind']
    taper = lambda x: max(0, round((x - 12) / 4.5))
    top = lambda x: top_edge(x, 8) + taper(x); bot = lambda x: bot_edge(x, 8) - taper(x)
    stub(cv, (16, top(16) + 3), (25, 9), 5.8, 4.4, P, tip=False)           # la horquilla de arriba
    stub(cv, (25, 9), (36, 4), 4.4, 3.0, P, tip=False)
    stub(cv, (25, 9), (31, 14), 3.6, 2.6, P, tip=True)
    stub(cv, (20, bot(20) - 3), (30, 56), 4.6, 3.4, P, tip=True)            # la rama rota de abajo
    if kind != 'burnt':
        tuft(cv, 36, 4, -.1, 6, 7, leaf, 1.4); tuft(cv, 36, 4, -1.2, 4, 5, leaf, .8)
        tuft(cv, 30, 56, .5, 5, 6, leaf, 1.2)
    else:
        for (x, y, a) in ((36, 4, .1), (36, 4, -.8), (30, 56, .6)):
            for j in range(5): cv.put(x + j * math.cos(a), y + j * math.sin(a), P['bark'][3] if j % 2 else P['bark'][2])
    jag = {}
    for y in range(BAND_E, BAND_E + 34):
        r = rnd.random()
        jag[y] = 24 + (rnd.randrange(6, 13) if r < .25 else rnd.randrange(1, 5) if r < .7 else rnd.randrange(-3, 1))
    for x in range(0, 40):
        t0, b0 = top(x), bot(x)
        for y in range(t0, b0 + 1):
            e = jag.get(y, 24)
            if x > e: continue
            c, v, dd = bark_px(x, y, t0, b0, P, 41)
            if x >= e - 3 or (x > 21 and (y + x) % 5 == 0):                  # la rotura: madera clara, corazón oscuro
                core = abs(v - .5) < .2
                c = wd[0] if core and x < e - 1 else wd[4] if v < .4 and x >= e - 1 else wd[3] if v < .55 else wd[2] if v < .75 else wd[1]
                if kind == 'burnt' and rnd.random() < .08: c = C(EMBER[0]) if rnd.random() < .6 else C(EMBER[1])
            elif kind == 'moss' and .05 < v < .4 and pnoise(51, 16, 4)(x % 16, y) > .6: c = P['moss'][1]
            cv.put(x, y, c)
    cv.outline(); cv.clean()
    for x in range(0, 30):
        b0 = bot(x)
        for k in (1, 2):
            if cv.px.get((x, b0 + k)) is None: cv.put(x, b0 + k, SHADOW)
    return cv

# =================================================================================================
# Burbujas de lodo (sueltas; el código las pone junto a los costados)
# =================================================================================================
def mud_bubble(f, big=True):
    """Burbuja que asoma, crece, revienta salpicando y deja un anillo que se abre (12×12 o 8×8)."""
    S = 12 if big else 8; cv = Canvas(S); c = S / 2 - .5
    R = (0, 1.4, 2.4, 3.2, 0, 0, 0) if big else (0, 1.2, 2.0, 0, 0, 0)
    if f == 0:                                        # hoyuelo
        cv.put(c, c, MUDP[0]); cv.put(c + 1, c, MUDP[1]); cv.put(c, c - 1, MUDP[3])
    elif R[f] > 0:                                    # cúpula con brillo
        r = R[f]
        for y in range(S):
            for x in range(S):
                d = math.hypot(x + .5 - S / 2, (y + .5 - S / 2) * 1.25)
                if d < r:
                    k = 4 if (x + .5 - S / 2) < -r * .3 and (y + .5 - S / 2) < -r * .2 else 3 if (y + .5 - S / 2) < 0 else 2
                    cv.put(x, y, MUDP[k])
                elif d < r + 1 and y + .5 > S / 2: cv.put(x, y, MUDP[0])
        cv.put(c - r * .45, c - r * .5, (255, 240, 210, 255) if r > 2 else MUDP[4])
    else:
        k = f - (4 if big else 3)                     # revienta: gotas y anillo que se abre
        rr = 2 + k * (1.5 if big else 1.2)
        for a in range(0, 360, 30):
            x = S / 2 + math.cos(math.radians(a)) * rr; y = S / 2 + math.sin(math.radians(a)) * rr / 1.4
            if k < 2 or a % 60 == 0: cv.put(x, y, MUDP[4] if k == 0 else MUDP[3])
        if k == 0:
            for (dx, dy) in ((-3, -4), (2, -5), (4, -2)): cv.put(c + dx, c + dy, MUDP[4])
        elif k == 1:
            for (dx, dy) in ((-4, -3), (3, -4)): cv.put(c + dx, c + dy, MUDP[3])
    return cv

# =================================================================================================
# Maqueta animada: el lago con el tronco, burbujas y la rana que lo cruza
# =================================================================================================
LAKE_W, LAKE_H, DOOR = 36, 24, 11

def lake_map():
    rows = [['#'] * LAKE_W for _ in range(LAKE_H)]
    for r in range(4, LAKE_H - 2):
        for x in range(2, LAKE_W - 2): rows[r][x] = '.'
    cx, cy = (LAKE_W - 1) / 2, DOOR + .5; rx, ry = LAKE_W * .3, (LAKE_H - 5) * .32
    for r in range(3, LAKE_H - 1):
        for x in range(1, LAKE_W - 1):
            if rows[r][x] != '.': continue
            nx, ny = (x - cx) / rx, (r - cy) / ry; a = math.atan2(ny, nx)
            e = 1 + .14 * math.sin(3 * a + .6) + .09 * math.sin(5 * a + 2)
            if nx * nx + ny * ny <= e * e: rows[r][x] = '~'
    rows[DOOR][0] = rows[DOOR + 1][0] = rows[DOOR][1] = rows[DOOR + 1][1] = 'L'
    return [''.join(r) for r in rows]

def mid_var(x): return (0, 4, 0, 3, 2, 0, 5, 1, 0, 3)[(x * 7919) % 97 % 10]

def scene_frames(key, S, B, walk=True, n=40):
    rows = lake_map(); TS.SAMPLE = rows
    b = TS.build(key); _, base = TS.sample_room(b); base = base.convert('RGBA')
    row = rows[DOOR]; x0, x1 = 2, LAKE_W - 3
    lake = [x for x in range(x0 + 2, x1 - 1) if row[x] == '~']
    y = DOOR * 16 - BAND
    frog = Image.open(os.path.join(TEMP, 'floor3-bodies', 'Frog', 'Player_Frog_Move_side.png')).convert('RGBA')
    rnd = random.Random(3)
    bubbles = [(rnd.choice(lake) * 16 + rnd.randrange(0, 12), rnd.choice((y - 4 + rnd.randrange(0, 4), y + 42 + rnd.randrange(0, 6))),
                rnd.randrange(0, n), rnd.random() < .6) for _ in range(9)]
    start = (lake[0] - 2) * 16; speed = (len(lake) + 4) * 16 / n
    frames = []
    for f in range(n):
        im = base.copy()
        im.alpha_composite(S['Log_EndLeft'][0].img(), (x0 * 16, DOOR * 16 - BAND_E))
        im.alpha_composite(S['Log_EndRight'][0].img(), ((x1 - 2) * 16, DOOR * 16 - BAND_E))
        px = start + f * speed
        cell = int((px + 16) // 16)                        # casilla bajo los pies de la rana
        for x in range(x0 + 2, x1 - 2):
            v = mid_var(x)
            if row[x] != '~':
                im.alpha_composite(S[f'Log_Ground_{x % 2}'][0].img(), (x * 16, y)); continue
            fr = None
            if walk:
                t_in = (x * 16 - 16 - start) / speed       # cuadro en que la rana pisa esta casilla
                if 0 <= f - t_in < 6: fr = S[f'Log_Mid_{v}_Step'][int(f - t_in)]
                else:
                    for nb in (x - 1, x + 1):
                        tn = (nb * 16 - 16 - start) / speed
                        if 0 <= f - tn < 4 and fr is None: fr = S[f'Log_Mid_{v}_Dip'][int(f - tn)]
            if fr is None: fr = S[f'Log_Mid_{v}_Idle'][(f // 2 + x) % 8 if False else (f // 2) % 8]
            im.alpha_composite(fr.img(), (x * 16, y))
        for (bx, by, t0, big) in bubbles:
            k = (f - t0) % n
            seq = B['Fx_MudBubble' if big else 'Fx_MudBubble_Small']
            if k // 2 < len(seq):
                fr = seq[k // 2].img(); im.alpha_composite(fr, (bx, by))
        if walk:
            fi = frog.crop(((f % 4) * 32, 0, (f % 4) * 32 + 32, 32))
            im.alpha_composite(fi, (int(px), DOOR * 16 - 6))
        frames.append(im.crop((0, (DOOR - 5) * 16, LAKE_W * 16, (DOOR + 7) * 16)))
    return frames

# =================================================================================================
if __name__ == '__main__':
    B = {'Fx_MudBubble': [mud_bubble(f, True) for f in range(7)], 'Fx_MudBubble_Small': [mud_bubble(f, False) for f in range(6)]}
    bd = os.path.join(HERE, 'bubbles'); os.makedirs(bd, exist_ok=True)
    for name, fr in B.items(): save_sheet(os.path.join(bd, name + '.png'), fr)
    os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True)
    stills, sets = [], {}
    for key in ('Swamp', 'Smoke', 'Burnt'):
        S = {'Log_EndLeft': [end_left(key)], 'Log_EndRight': [end_right(key)]}
        for v in range(6): S.update(lake_sheets(key, v))
        for v in range(2): S[f'Log_Ground_{v}'] = [ground_tile(key, v)]
        sets[key] = S
        d = os.path.join(HERE, key)
        for old in os.listdir(d) if os.path.isdir(d) else []:
            if old.startswith('Log_'): os.remove(os.path.join(d, old))
        os.makedirs(d, exist_ok=True)
        for name, fr in S.items(): save_sheet(os.path.join(d, name + '.png'), fr)
        frames = scene_frames(key, S, B)
        stills.append(frames[14])
        big = [fr.resize((fr.width * 2, fr.height * 2), Image.NEAREST) for fr in frames]
        big[0].save(os.path.join(HERE, 'gif', f'escena_tronco_{key}.gif'), save_all=True, append_images=big[1:], duration=80, loop=0, disposal=2)
    M = Image.new('RGBA', (stills[0].width, sum(s.height for s in stills) + 8), (10, 7, 5, 255))
    y = 0
    for s in stills: M.alpha_composite(s, (0, y)); y += s.height + 4
    M.resize((M.width * 2, M.height * 2), Image.NEAREST).save(os.path.join(HERE, 'maqueta_tronco.png'))
    for key, S in sets.items():
        gif(os.path.join(HERE, 'gif', f'tramo_{key}.gif'),
            S['Log_Mid_2_Idle'] * 2 + S['Log_Mid_2_Step'] + S['Log_Mid_2_Idle'],
            [IDLE_MS] * 16 + [STEP_MS] * 6 + [IDLE_MS] * 8, C('#4a3324'), 6)
    gif(os.path.join(HERE, 'gif', 'burbujas.gif'), B['Fx_MudBubble'] + [Canvas(12)] * 3, 110, C('#4a3324'), 8)
    preview(os.path.join(HERE, 'vista_previa_tronco.png'), {f'{k} {n}': fr for k in ('Swamp',) for n, fr in sets[k].items()}
            | {f'{k} {n}': sets[k][n] for k in ('Smoke', 'Burnt') for n in ('Log_EndLeft', 'Log_EndRight', 'Log_Mid_2_Step', 'Log_Mid_4_Idle')}
            | B, MUDP[2], scale=4)
    print('Listo en', HERE)
