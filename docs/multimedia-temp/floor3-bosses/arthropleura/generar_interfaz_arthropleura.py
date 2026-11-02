"""Interfaz del Arthropleura (tanda 6c-2, piso 3): barra de vida de segmentos y flechas del borde de pantalla.

Barra (BossBar/UI_BossBar_Arthropleura_*): el ciempiés visto desde arriba, mirando a la izquierda. Cabeza enorme con
los ojos compuestos negros, dos colmillos curvos de cuero y las antenas; 20 placas de cobre con paranotos de punta
dorada por encima y por debajo de la ventana; la cola con los dos cercos dorados a la derecha. Más larga que las del
piso 2: 320×32 con la ventana de 260×8 en (46, 12), una placa del relleno = 13 px (20 placas, una por segmento).
Fases: F1 cobre, F2 ascuas (el fuego), F3 carne rosada que late (placas abiertas), último 10 % rueda (carne viva,
ojos en espiral). Muescas al 70 % y al 35 %; efectos de cambio de fase: la pared que revienta (70 %) y las placas que
se abren de la cola a la cabeza (35 %).

Flechas del borde (EdgeArrows/UI_EdgeArrow_*): insignia redonda de 24×24 con el icono (cabeza roja, cola dorada,
punto débil rosa que late) y un puntero de 12×12 aparte en 16 direcciones.

Hecho con la skill primordia-sprites (pixel.py): luz arriba a la izquierda, contorno OUTL; paletas de
generar_arthropleura.py (cobre oxidado, cuero, carne, ascuas).

Uso: python generar_interfaz_arthropleura.py [carpeta_salida]   (por defecto, la carpeta de este script)
"""
import math, os, random, sys

def _skill_scripts():
    d = os.path.dirname(os.path.abspath(__file__))
    while True:
        p = os.path.join(d, '.claude', 'skills', 'primordia-sprites', 'scripts')
        if os.path.isdir(p): return p
        if os.path.dirname(d) == d: raise SystemExit('No encuentro .claude/skills/primordia-sprites/scripts')
        d = os.path.dirname(d)
sys.path.insert(0, _skill_scripts())
from pixel import *
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = sys.argv[1] if len(sys.argv) > 1 else HERE
OUT = os.path.join(ROOT, 'BossBar')
OUT_ARROW = os.path.join(ROOT, 'EdgeArrows')
BG = C('#0b1220')

# --- paletas (las de generar_arthropleura.py, en rampas de 4) -------------------------------------------
PLATE4 = ['#43241a', '#6e3a22', '#9c5a34', '#c88a58']      # cobre oxidado
BAND4 = ['#170d0b', '#2e1a14', '#4a2a1c', '#6e3a22']       # borde trasero de cada placa
EDGE4 = ['#7a4a2c', '#b07a4c', '#d8a874', '#f0d0a0']       # cuero: colmillos
LEG4 = ['#5a2c18', '#9a4e26', '#c8743a', '#e8a868']        # patas y antenas
EYE4 = ['#08060a', '#140e14', '#241a22', '#4a3238']        # ojo compuesto
MOUTH4 = ['#22080f', '#3a1020', '#6a2034', '#9a3a4a']
GOLD4 = ['#7a4a14', '#c08a2c', '#f0c850', '#fff0a8']       # puntas doradas de los paranotos y los cercos
EMBER4 = ['#a8341c', '#e8682a', '#ffb24a', '#fff0b0']
CHAR4 = ['#1c1618', '#2c2426', '#423638', '#5c4c48']
FLESH5 = ['#5a1e2e', '#9a3a4a', '#d06a74', '#f0a0a0', '#ffd8d0']
WINDOW_BACK = ['#0e0a10', '#1a1218', '#241a20']
FILLS = {
    'Copper': ['#5a2a14', '#a8582a', '#e08a44', '#ffc884'],     # cobre bruñido, más vivo que el marco
    'Ember': ['#7a1e10', '#d4441c', '#f88c2a', '#ffd25a'],
    'Flesh': ['#8a2a40', '#c8506a', '#f08a9a', '#ffd0d4'],
    'Raw': ['#9a1438', '#e0305a', '#ff6a8a', '#ffd0dc'],        # la rueda: carne viva, más encendida
    'Trail': ['#b8a8a4', '#dcd0cc', '#f4ece6', '#fffaf4'],
}

W, H = 320, 32
WX0, WX1, WY0, WY1 = 46, 306, 12, 20     # ventana (x1, y1 exclusivos)
CY = 16.0
PLATE = 13                               # una placa del lomo = un segmento
NOTCHES = (0.70, 0.35)

def _mirror_y(y): return 2 * int(CY) - 1 - y

def band(cv, path, r0, r1, ramp4, mirror=False, put=None):
    """Banda redondeada que sigue el camino (de grosor r0 a r1), con luz por arriba; mirror la refleja en CY."""
    segs, L = [], 0.0
    for (x0, y0), (x1, y1) in zip(path, path[1:]):
        l = math.hypot(x1 - x0, y1 - y0); segs.append((x0, y0, x1, y1, l, L)); L += l
    xs = [p[0] for p in path]; ys = [p[1] for p in path]
    r = max(r0, r1) + 1
    for y in range(int(min(ys) - r), int(max(ys) + r) + 1):
        for x in range(int(min(xs) - r), int(max(xs) + r) + 1):
            px, py = x + .5, y + .5
            best = None
            for x0, y0, x1, y1, l, s0 in segs:
                dx, dy = (x1 - x0) / l, (y1 - y0) / l
                t = max(0.0, min(l, (px - x0) * dx + (py - y0) * dy))
                qx, qy = x0 + dx * t, y0 + dy * t
                d = math.hypot(px - qx, py - qy)
                if best is None or d < best[0]: best = (d, s0 + t, py - qy)
            d, sl, vy = best
            rr = r0 + (r1 - r0) * sl / L
            if d > rr: continue
            i = 3 if vy < -rr * 0.35 else 2 if vy < rr * 0.3 else 1
            if sl / L > 0.86 and ramp4 is not GOLD4: i = min(3, i + 1)          # punta gastada, más clara
            yy = _mirror_y(y) if mirror else y
            if mirror: i = max(0, i - 1)
            (put or cv.put)(x, yy, C(ramp4[i]))

def bez(p0, p1, p2, n=12):
    return [((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0],
             (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]) for t in (k / n for k in range(n + 1))]

# --- cuerpo --------------------------------------------------------------------------------------------
def lobe(s):
    """Cuánto sobresale el paranoto en el píxel s de la placa (0 delante, 3 detrás): ala echada hacia atrás."""
    return round(3 * (s / (PLATE - 1)) ** 1.3)

def plates(cv):
    rnd = random.Random(11)
    for p in range((WX1 - WX0) // PLATE):
        for s in range(PLATE):
            x = WX0 + p * PLATE + s
            rear = s >= PLATE - 2
            r4 = BAND4 if rear else PLATE4
            top = WY0 - 5 - lobe(s)
            for y in range(top, WY0):
                d = y - top
                i = 3 if d == 0 else 2 if d <= 2 else 1
                if s == 0 and i < 3: i += 1                                   # el filo de delante coge la luz
                cv.put(x, y, C(r4[i]))
            bot = WY1 + 4 + lobe(s)
            for y in range(WY1, bot + 1):
                d = y - WY1
                i = 2 if d == 0 else 1 if d <= 3 else 0
                cv.put(x, y, C(r4[i]))
        # punta dorada del paranoto, echada hacia atrás y afuera
        xe = WX0 + p * PLATE + PLATE - 1
        ty, by = WY0 - 5 - 3, WY1 + 4 + 3
        for k, (dx, dy) in enumerate(((0, -1), (1, -2), (2, -2))):
            cv.put(xe + dx, ty + dy, C(GOLD4[1 + k]))
            cv.put(xe + dx, by - dy, C(GOLD4[k]))
    return rnd

def plate_specks(cv):
    """Picaduras claras sobre las placas (como en el sprite), después del contorno."""
    rnd = random.Random(5)
    for p in range((WX1 - WX0) // PLATE):
        for _ in range(3):
            s = rnd.randint(2, PLATE - 4)
            x = WX0 + p * PLATE + s
            y = rnd.choice((WY0 - 3, WY0 - 4, WY1 + 2, WY1 + 3))
            cv.put_in(x, y, C(PLATE4[3] if y < WY0 else PLATE4[2]))

def legs(cv):
    """Dos patas finas por placa y lado asomando bajo el paranoto (sin contorno)."""
    for p in range((WX1 - WX0) // PLATE):
        s = 6
        x = WX0 + p * PLATE + s
        ty = WY0 - 5 - lobe(s) - 1
        for k in range(4):
            cv.put_out(x + k * 0.7, ty - k, C(LEG4[1 + min(2, k)]))
            cv.put_out(x + k * 0.7, _mirror_y(ty - k), C(LEG4[min(2, k)]))

def tail(cv):
    for x in range(WX1, WX1 + 7):
        half = 7.0 - (x - WX1) * 0.45
        for y in range(H):
            v = y + .5 - CY
            if abs(v) <= half:
                i = 3 if v < -half + 1.2 else 2 if v < 0 else 1 if v < half - 1 else 0
                if x == WX1 + 6: i = max(0, i - 1)
                cv.put(x, y, C(PLATE4[i]))
    for mirror in (0, 1):
        band(cv, bez((WX1 + 5, 13.5), (WX1 + 10, 12.5), (W - 2, 9.5), 10), 1.6, 0.6, GOLD4, mirror=bool(mirror))

def head(cv):
    # colmillos (detrás de la cabeza), de la base oscura a la punta de cuero
    for mirror in (False, True):
        band(cv, bez((18, 11.5), (8, 6.5), (3.5, 13.5), 14), 2.3, 0.7, EDGE4, mirror=mirror)
    # boca entre los colmillos
    for y in range(13, 19):
        for x in range(12, 18):
            if (x - 15.5) ** 2 / 9 + (y + .5 - CY) ** 2 / 7 <= 1:
                cv.put(x, y, C(MOUTH4[1 if y < 16 else 0]))
    # cápsula de la cabeza
    ellipse(cv, 31, CY, 15.5, 11.5, PLATE4)
    # cuello: un collar oscuro bajo la primera placa
    for y in range(6, 26):
        if abs(y + .5 - CY) < 9.5: cv.put(45, y, C(BAND4[1]))

def head_details(cv):
    for x in range(20, 44):                                                   # sutura central
        cv.put_in(x, 15, C(PLATE4[0] if x % 6 else BAND4[0])); cv.put_in(x, 16, C(PLATE4[1]))
    for x, y in ((24, 6), (27, 5), (31, 5), (35, 5), (39, 6), (19, 8)):     # filo iluminado
        cv.put_in(x, y, C(PLATE4[3]))
    for x, y in ((34, 9), (38, 11), (36, 22), (40, 20), (29, 23)):          # picaduras
        cv.put_in(x, y, C(PLATE4[3] if y < 16 else PLATE4[2]))

def antennae(cv):
    for mirror in (0, 1):
        for k, (x, y) in enumerate(bez((17, 7), (10, 1.5), (2, 1), 10)):
            yy = _mirror_y(int(y)) if mirror else y
            cv.put_out(x, yy, C(LEG4[2 if k < 7 else 3]))

def window_hole(cv):
    for x in range(WX0, WX1):
        for y in range(WY0, WY1):
            cv.px.pop((x, y), None)
    for x in range(WX0 - 1, WX1 + 1):
        cv.put(x, WY0 - 1, OUTL); cv.put(x, WY1, OUTL)
    for y in range(WY0, WY1):
        cv.put(WX0 - 1, y, OUTL); cv.put(WX1, y, OUTL)

def frame(state='Copper', k=0):
    cv = Canvas(W, H)
    plates(cv)
    tail(cv)
    head(cv)
    if state in ('Open', 'Raw'):
        open_plates(cv, k, raw=state == 'Raw')
    cv.outline(); cv.clean()
    legs(cv); antennae(cv)
    window_hole(cv)
    head_details(cv)
    plate_specks(cv)
    if state == 'Ember':
        embers(cv, k)
    return cv

def embers(cv, k):
    """F2: placas chamuscadas con grietas de ascuas que parpadean (k = 0..2) y alguna chispa."""
    rnd = random.Random(21)
    for p in range((WX1 - WX0) // PLATE):
        x0 = WX0 + p * PLATE
        for top in (True, False):
            # mancha chamuscada en el centro de la placa
            w0 = rnd.randint(2, 4); w1 = PLATE - rnd.randint(3, 5)
            for s in range(w0, w1):
                for d in range(1, 4):
                    if d == 3 and s in (w0, w1 - 1): continue
                    y = WY0 - 1 - d if top else WY1 + d
                    cv.put_in(x0 + s, y, C(CHAR4[2 if d == 1 else 1] if top else CHAR4[1 if d == 1 else 0]))
            # grieta encendida
            sx = x0 + rnd.randint(3, PLATE - 5)
            y = WY0 - 2 if top else WY1 + 1
            for i in range(rnd.randint(3, 5)):
                c = EMBER4[(i + k + p) % 3 + (1 if (i + k) % 3 == 0 else 0)]
                cv.put_in(sx + i, y - (i % 2) if top else y + (i % 2), C(c))
    for i in range(5):
        x = WX0 + ((i * 53 + k * 29) % (WX1 - WX0))
        y = WY0 - 9 - (k + i) % 3 if i % 2 else WY1 + 8 + (k + i) % 3
        cv.put_out(x, y, C(EMBER4[2 + (i + k) % 2], 220))
    for x, y in ((24, 7), (37, 8), (30, 24)):                                # brasas en la cabeza
        cv.put_in(x + k % 2, y, C(EMBER4[1 + k % 2]))

def open_plates(cv, k, raw=False):
    """F3 y la rueda: el borde trasero de cada placa se levanta y deja ver la carne rosada que late."""
    flesh = FLESH5[1:] if not raw else ['#b02a4a', '#e85a7a', '#ff9ab0', '#fff0f0']
    for p in range((WX1 - WX0) // PLATE):
        x0 = WX0 + p * PLATE
        for s in range(PLATE - 4, PLATE):
            x = x0 + s
            top = WY0 - 5 - lobe(s)
            bot = WY1 + 4 + lobe(s)
            for y in range(top + 1, WY0):
                d = y - top
                if s == PLATE - 4:                                            # filo levantado de la placa
                    cv.put(x, y, C(PLATE4[3] if d < 3 else PLATE4[2]))
                else:
                    i = 3 if d == 1 + k else 2 if d <= 3 else 1
                    cv.put(x, y, C(flesh[min(3, i + (k if s == PLATE - 2 else 0))]))
            for y in range(WY1, bot):
                d = y - WY1
                if s == PLATE - 4:
                    cv.put(x, y, C(PLATE4[2] if d > 1 else PLATE4[1]))
                else:
                    i = 2 if d == 2 + k else 1 if d < 4 else 0
                    cv.put(x, y, C(flesh[i + (1 if k and s == PLATE - 2 else 0)]))

# --- ojos ----------------------------------------------------------------------------------------------
EYE_C = (24.5, 10.0)          # centro del ojo de arriba; el de abajo, reflejado
BROW = [(30, 6), (29, 6), (28, 6), (27, 6), (26, 6), (25, 6), (24, 6), (23, 7), (22, 7), (21, 8)]

def eyes(state, k=0):
    cv = Canvas(W, H)
    ex, ey = EYE_C
    for mirror in (False, True):
        sub = Canvas(W, H)
        if state == 'Roll':
            ellipse(sub, ex, ey, 4.2, 3.0, ['#c8b8b0', '#e8dcd4', '#f4f0ec', '#ffffff'])
            # espiral que gira: 4 cuadros
            for j in range(11):
                a = j * 0.9 + k * math.pi / 2
                rr = 0.3 + j * 0.3
                sub.put_in(ex + math.cos(a) * rr * 1.4, ey + math.sin(a) * rr, C('#3a1020'))
        else:
            ramp4 = {'Hunt': EYE4, 'Burn': ['#3a0e08', '#8a2a10', '#e8682a', '#ffb24a'],
                     'Flesh': ['#3a0614', '#8a1430', '#e03a5a', '#ff8aa0']}[state]
            ellipse(sub, ex, ey, 4.2, 3.0, ramp4)
            hi = {'Hunt': EYE, 'Burn': C('#fff0b0'), 'Flesh': C('#ffe0e8')}[state]
            sub.put(ex - 2, ey - 1.2, hi); sub.put(ex - 1, ey - 2, hi); sub.put(ex - 2, ey - 2, hi)
            if state == 'Hunt': sub.put(ex - 1, ey - 1.6, C(EYE4[3]))
            if state == 'Burn' and k: sub.put(ex - 1, ey - 0.6, hi)
        sub.outline()
        for x, y in BROW:                                                     # ceño hacia delante
            if state != 'Roll': sub.put(x, y, OUTL)
        if state in ('Burn', 'Flesh') and k:                                  # halo que late
            glow = C('#ff8a3a', 140) if state == 'Burn' else C('#ff5a7a', 140)
            for a in range(16):
                t = a / 16 * 2 * math.pi
                sub.put_out(ex + math.cos(t) * 5.4, ey + math.sin(t) * 4.1, glow)
        for (x, y), c in sub.px.items():
            cv.put(x, _mirror_y(y) if mirror else y, c)
    return cv

# --- ventana -------------------------------------------------------------------------------------------
def window_fill(ramp, k=0, mode=''):
    w, h = WX1 - WX0, WY1 - WY0
    cv = Canvas(w, h)
    rnd = random.Random(100 + k)
    for x in range(w):
        s = x % PLATE
        for y in range(h):
            i = 3 if y == 0 else 2 if y < 3 else 1 if y < 6 else 0
            if s == PLATE - 1 or s == PLATE - 2 and y > 4: i = max(0, i - 1)  # junta entre placas
            if s == 0 and 0 < y < 6: i = min(3, i + 1)                          # filo de delante
            if mode == 'pulse' and k and 0 < y < 4 and 1 <= s <= PLATE - 4: i = min(3, i + 1)
            cv.put(x, y, C(ramp[i]))
        if mode == 'ember':                                                     # brasas que se mueven
            if rnd.random() < 0.18: cv.put(x, rnd.randint(2, 6), C(EMBER4[3]))
            if rnd.random() < 0.10: cv.put(x, rnd.randint(4, 7), C(ramp[0]))
        if mode == 'raw' and s == (3 + k * 5) % PLATE: cv.put(x, 1, C('#ffffff'))   # brillo húmedo
    return cv

def back():
    w, h = WX1 - WX0, WY1 - WY0
    cv = Canvas(w, h)
    for x in range(w):
        for y in range(h):
            cv.put(x, y, C(WINDOW_BACK[0 if y < 2 else 1 if y < 6 else 2]))
    return cv

# --- muescas -------------------------------------------------------------------------------------------
def notch(broken=False):
    """Espina de quitina que cruza la barra con las puntas doradas arriba y abajo."""
    cv = Canvas(8, H)
    for y in range(1, H - 1):
        e = min(y - 1, H - 2 - y)
        half = min(0.6 + e * 0.45, 2.4)
        if broken and 8 <= y <= 22: continue
        for x in range(8):
            if abs(x + .5 - 4) <= half:
                gold = e < 4
                r4 = GOLD4 if gold else PLATE4
                i = 3 if x < 4 and y < 16 else 2 if x < 4 else 1
                if abs(x + .5 - 4) > half - 1: i = max(0, i - 1)
                if not gold and y % 5 == 0: i = max(0, i - 1)
                cv.put(x, y, C(r4[i]))
    if broken:
        for x, y in ((3, 7), (4, 6), (5, 7), (3, 23), (4, 24), (5, 23)):
            cv.put(x, y, C(BAND4[1]))
    cv.outline(); cv.clean()
    return cv

def notch_break(k):
    out = Canvas(16, H + 2)
    base = notch(broken=k >= 2).img()
    for y in range(H):
        for x in range(8):
            p = base.getpixel((x, y))
            if p[3]: out.put(x + 4, y + 1, p)
    if k == 0:
        for y in range(9, 22): out.put_in(8 + (y % 2), y, C(GOLD4[3]))
    if k == 1:
        sparks(out, 8, 16, 2)
    if k >= 2:
        d = (k - 1) * 1.8
        for dx, dy in ((-1, -1), (1, -0.6), (-0.8, 0.9), (1.1, 1), (0, -1.3)):
            col = C(PLATE4[2] if k < 4 else PLATE4[1], 255 - 60 * (k - 2))
            out.put(8 + dx * d, 16 + dy * d, col); out.put(9 + dx * d, 16 + dy * d, C(GOLD4[2], 255 - 60 * (k - 2)))
    return out

def notch_x(t): return WX0 + int((WX1 - WX0) * t)

# --- efectos de cambio de fase ---------------------------------------------------------------------------
WOOD = ['#2e1c12', '#5a3a22', '#8a6038', '#b88a58']
LEAF = ['#1e3016', '#3a5a22', '#6a8a34', '#a0b858']
ROCK = ['#3a3630', '#5e5850', '#8a8478']
DUST = (184, 160, 120)

def wall_break(k, n=6):
    """70 %: la pared del claro revienta. Astillas, piedras, hojas y polvo salen de la muesca del 70 %."""
    cv = Canvas(W, H)
    rnd = random.Random(7)
    t = (k + 1) / n
    cx = notch_x(NOTCHES[0])
    if k == 0:
        sparks(cv, cx, 6, 3); sparks(cv, cx, 26, 3); sparks(cv, cx - 12, 16, 2); sparks(cv, cx + 12, 16, 2)
    for i in range(110):
        kind = i % 4
        ang = rnd.uniform(0, 2 * math.pi)
        sp = rnd.uniform(25, 190)
        x = cx + math.cos(ang) * sp * t
        y = CY + math.sin(ang) * (6 + sp * 0.1) * min(1, t * 2) + 10 * t * t * (1 if kind < 2 else 0.4)   # caen
        if not (1 <= y < H - 2): continue
        a = int(255 * min(1, 1.35 - t) * min(1, min(y - 1, H - 3 - y) / 4))       # se desvanece hacia los bordes
        if kind == 0:                                        # astilla
            for d in range(4): cv.put(x + d * math.cos(ang + i), y + d * 0.6 * math.sin(ang + i), C(WOOD[3 - d % 3], a))
        elif kind == 1:                                      # piedra
            cv.put(x, y, C(ROCK[2], a)); cv.put(x + 1, y, C(ROCK[1], a)); cv.put(x, y + 1, C(ROCK[1], a)); cv.put(x + 1, y + 1, C(ROCK[0], a))
        elif kind == 2:                                      # hoja
            cv.put(x, y, C(LEAF[2 + (k + i) % 2], a)); cv.put(x + 1, y + (k % 2), C(LEAF[1], a)); cv.put(x - 1, y - (k % 2), C(LEAF[3], a))
        elif k < 4:                                          # polvo
            r = 1.5 + t * 3
            for yy in range(int(y - r), int(y + r) + 1):
                for xx in range(int(x - r), int(x + r) + 1):
                    if (xx - x) ** 2 + (yy - y) ** 2 <= r * r:
                        cv.put_out(xx, yy, DUST + (int(170 * (1 - t)),))
    return cv

def plates_open(k, n=6):
    """35 %: las placas se abren en una ola de la cola a la cabeza; destello rosa y esquirlas de quitina."""
    cv = Canvas(W, H)
    rnd = random.Random(9)
    front = WX1 - (WX1 - WX0 + 30) * (k + 1) / n
    for p in range((WX1 - WX0) // PLATE):
        xr = WX0 + p * PLATE + PLATE - 2
        dist = xr - front
        if dist < 0: continue
        if dist < 40:                                                         # el frente de la ola
            a = int(255 * min(1, 1.3 - dist / 40))
            for y in list(range(3, WY0 - 1)) + list(range(WY1 + 1, H - 3)):
                cv.put(xr, y, C(FLESH5[4], a)); cv.put(xr + 1, y, C(FLESH5[3], a))
                cv.put(xr - 1, y, C(FLESH5[3], a // 2)); cv.put(xr - 2, y, C(FLESH5[2], a // 3))
            for j in range(4):                                                # esquirlas de quitina que saltan
                fx = xr + rnd.uniform(-4, 4) + dist * 0.15
                fy = (rnd.uniform(2, 6) - dist * 0.08) if j % 2 == 0 else (rnd.uniform(25, 29) + dist * 0.08)
                if not (1 <= fy < H - 2): continue
                cv.put(fx, fy, C(PLATE4[3], a)); cv.put(fx + 1, fy, C(PLATE4[2], a)); cv.put(fx, fy + 1, C(PLATE4[1], a))
            if dist < 8: sparks(cv, xr, 4 if p % 2 else 27, 1)
        elif rnd.random() < 0.25:
            sparks(cv, xr, rnd.choice((5, 26)), 1)
    return cv

# --- flechas del borde -----------------------------------------------------------------------------------
RED4B = ['#5a1414', '#a82a22', '#e8503a', '#ff9a7a']
RINGS = {'Head': RED4B, 'Tail': GOLD4, 'Weak': FLESH5[1:]}
BADGE = 24

def badge_base(ring):
    cv = Canvas(BADGE)
    c = BADGE / 2
    for y in range(BADGE):
        for x in range(BADGE):
            dx, dy = x + .5 - c, y + .5 - c
            r = math.hypot(dx, dy)
            if r <= 11:
                if r > 9.2:
                    cv.put(x, y, C(ring[shade(dx / 11, dy / 11)]))
                elif r > 8.3:
                    cv.put(x, y, OUTL)                                          # filo de dentro del aro
                else:
                    cv.put(x, y, C('#1c1218' if dy < 2 else '#140c10'))
    cv.outline()
    return cv

def icon_head(cv):
    """Cabeza vista desde arriba con los colmillos hacia delante (arriba): ojos negros en las esquinas, antenas y
    el primer segmento debajo."""
    for x in range(7, 18):                                                          # primer segmento
        for y in (19, 20):
            cv.put(x, y, C(PLATE4[2 if y == 19 else 1]))
    ellipse(cv, 12, 15, 6, 4.2, PLATE4)
    cv.outline()
    for mirror in (0, 1):
        sx = (lambda x: 24 - x) if mirror else (lambda x: x)
        for i, (x, y) in enumerate(bez((7.5, 13), (3.5, 10), (4, 5), 7)):           # antenas
            cv.put(sx(x), y, C(LEG4[2 if i < 5 else 3]))
        for i, (x, y) in enumerate(bez((9.5, 12), (5.6, 8.5), (9.6, 4.6), 10)):        # colmillos
            cv.put(sx(x), y, C(EDGE4[3 if i > 6 or not mirror else 2]))
            if i < 7: cv.put(sx(x) + (-1 if not mirror else 1), y, C(EDGE4[1 if not mirror else 0]))
    for ex in (8.5, 15.5):                                                          # ojos en las esquinas de delante
        for dx in (0, 1):
            for dy in (0, 1):
                cv.put(ex - 1 + dx, 12 + dy, C(EYE4[0 if dy else 2]))
    cv.put(7.5, 12, C(EYE4[3])); cv.put(14.5, 12, C(EYE4[3]))
    for x in range(10, 15): cv.put(x, 11, C(PLATE4[3]))
    for y in (14, 15, 16): cv.put(12, y, C(PLATE4[0]))

def icon_tail(cv):
    """Los dos últimos segmentos, cada vez más estrechos, y los dos cercos dorados abiertos hacia atrás (abajo)."""
    for y0, y1, half in ((4, 8, 6.5), (9, 12, 4.5)):
        for y in range(y0, y1 + 1):
            for x in range(24):
                dx = x + .5 - 12
                if abs(dx) <= half - (y == y1) * 0.8:
                    i = 3 if y == y0 else 2 if y < y1 - 1 else 1 if y < y1 else 0
                    if abs(dx) > half - 1.5: i = max(0, i - 1)
                    cv.put(x, y, C(PLATE4[i]))
        cv.put(12 - half, y0 - 1, C(GOLD4[2])); cv.put(11 + half, y0 - 1, C(GOLD4[2]))     # pinchos del paranoto
    cv.outline()
    for mirror in (0, 1):
        sx = (lambda x: 24 - x) if mirror else (lambda x: x)
        for i, (x, y) in enumerate(bez((10.5, 13), (9, 16), (6, 19), 8)):
            cv.put(sx(x), y, C(GOLD4[2 if i < 6 else 3]))
            if i < 5: cv.put(sx(x) + (-1 if not mirror else 1), y, C(GOLD4[1]))

def icon_weak(cv, k):
    """Carne rosada que late entre dos placas abiertas."""
    for y in range(6, 18):
        for x in range(24):
            dx = x + .5 - 12
            if y < 9 and abs(dx) <= 6: cv.put(x, y, C(PLATE4[3 if y == 6 else 2]))
            if y > 15 and abs(dx) <= 6: cv.put(x, y, C(PLATE4[1 if y == 16 else 0]))
    r = 3.2 + k * 0.5
    ellipse(cv, 12, 12.5, r + 1.2, r * 0.75, FLESH5[1:])
    cv.put(10, 11, C(FLESH5[4])); cv.put(11, 11, C(FLESH5[4]) if k else C(FLESH5[3]))
    if k == 2: sparks(cv, 16, 10, 1)

def badge(kind, k=0, warn=False):
    ring = RINGS[kind]
    if kind == 'Weak' and k: ring = FLESH5[2:] + ['#fff0ec']                       # el aro late
    if warn: ring = ['#a82a22', '#ff5a3a', '#ffb09a', '#fff0e0'] if k == 0 else ['#e8503a', '#ff9a7a', '#fff0e0', '#ffffff']
    cv = badge_base(ring)
    ic = Canvas(BADGE)
    {'Head': icon_head, 'Tail': icon_tail}.get(kind, lambda c: icon_weak(c, k))(ic)
    if kind == 'Weak': ic.outline()
    for (x, y), c in ic.px.items():
        if math.hypot(x + .5 - 12, y + .5 - 12) <= 8.3: cv.put(x, y, c)
    if warn and k == 1:
        for a in range(4):
            t = a / 4 * 2 * math.pi + math.pi / 4
            cv.put_out(12 + math.cos(t) * 12, 12 + math.sin(t) * 12, SPARK[1])
    return cv

PTR = 12
def pointer(kind, d):
    """Puntero triangular en la dirección d (0 = derecha, en sentido antihorario de 22,5° en 22,5°)."""
    cv = Canvas(PTR)
    a = d * math.pi / 8
    ux, uy = math.cos(a), -math.sin(a)
    px_, py_ = -uy, ux
    c = PTR / 2
    tip = (c + ux * 4.6, c + uy * 4.6)
    b1 = (c - ux * 2.4 + px_ * 3.6, c - uy * 2.4 + py_ * 3.6)
    b2 = (c - ux * 2.4 - px_ * 3.6, c - uy * 2.4 - py_ * 3.6)
    def inside(x, y):
        def side(p, q): return (q[0] - p[0]) * (y - p[1]) - (q[1] - p[1]) * (x - p[0])
        s = (side(tip, b1), side(b1, b2), side(b2, tip))
        return all(v >= 0 for v in s) or all(v <= 0 for v in s)
    fill(cv, inside, c - 1, c - 1, 5, 5, RINGS[kind])
    cv.outline(); cv.clean()
    return cv

# --- vista previa ----------------------------------------------------------------------------------------
def mock(frame_cv, fill_cv, eye_cv, ratio, trail, broken=(False, False), fx=None):
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    img.alpha_composite(back().img(), (WX0, WY0))
    ww, wh = WX1 - WX0, WY1 - WY0
    img.alpha_composite(window_fill(FILLS['Trail']).img().crop((0, 0, max(1, int(ww * trail)), wh)), (WX0, WY0))
    img.alpha_composite(as_img(fill_cv).crop((0, 0, max(1, int(ww * ratio)), wh)), (WX0, WY0))
    img.alpha_composite(as_img(frame_cv), (0, 0))
    for t, b in zip(NOTCHES, broken):
        img.alpha_composite(notch(broken=b).img(), (notch_x(t) - 4, 0))
    img.alpha_composite(as_img(eye_cv), (0, 0))
    if fx: img.alpha_composite(as_img(fx), (0, 0))
    return img

def arrows_panel(sheets, A):
    """Las tres insignias, cada una con sus 16 punteros alrededor."""
    cell = 64
    img = Image.new('RGBA', (cell * 4 + 8, cell + 8), BG)
    for j, (kind, bname) in enumerate((('Head', 'Badge_Head'), ('Head', 'Badge_HeadWarn'), ('Tail', 'Badge_Tail'),
                                       ('Weak', 'Badge_Weak'))):
        cx, cy = 4 + j * cell + cell // 2, 4 + cell // 2
        b = as_img(sheets[A + bname][-1])
        img.alpha_composite(b, (cx - 12, cy - 12))
        for d in range(16):
            a = d * math.pi / 8
            x, y = cx + math.cos(a) * 21, cy - math.sin(a) * 21
            img.alpha_composite(as_img(sheets[A + 'Pointer_' + kind][d]), (int(x - PTR / 2), int(y - PTR / 2)))
    return img

if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True); os.makedirs(OUT_ARROW, exist_ok=True)
    P = 'UI_BossBar_Arthropleura_'
    sheets = {
        P + 'Frame_Copper': [frame('Copper')],
        P + 'Frame_Ember': [frame('Ember', k) for k in range(3)],
        P + 'Frame_Open': [frame('Open', k) for k in range(2)],
        P + 'Frame_Raw': [frame('Raw', k) for k in range(2)],
        P + 'Back': [back()],
        P + 'Fill_Copper': [window_fill(FILLS['Copper'])],
        P + 'Fill_Ember': [window_fill(FILLS['Ember'], k, 'ember') for k in range(3)],
        P + 'Fill_Flesh': [window_fill(FILLS['Flesh'], k, 'pulse') for k in range(2)],
        P + 'Fill_Raw': [window_fill(FILLS['Raw'], k, 'raw') if k == 0 else window_fill(FILLS['Raw'], k, 'pulse')
                         for k in range(2)],
        P + 'Fill_Trail': [window_fill(FILLS['Trail'])],
        P + 'Eyes_Hunt': [eyes('Hunt')],
        P + 'Eyes_Burn': [eyes('Burn', k) for k in range(2)],
        P + 'Eyes_Flesh': [eyes('Flesh', k) for k in range(2)],
        P + 'Eyes_Roll': [eyes('Roll', k) for k in range(4)],
        P + 'Notch': [notch()],
        P + 'Notch_Broken': [notch(broken=True)],
        P + 'Fx_NotchBreak': [notch_break(k) for k in range(5)],
        P + 'Fx_WallBreak': [wall_break(k) for k in range(6)],
        P + 'Fx_PlatesOpen': [plates_open(k) for k in range(6)],
    }
    A = 'UI_EdgeArrow_Arthropleura_'
    arrows = {
        A + 'Badge_Head': [badge('Head')],
        A + 'Badge_HeadWarn': [badge('Head', k, warn=True) for k in range(2)],
        A + 'Badge_Tail': [badge('Tail')],
        A + 'Badge_Weak': [badge('Weak', k) for k in range(3)],
        A + 'Pointer_Head': [pointer('Head', d) for d in range(16)],
        A + 'Pointer_Tail': [pointer('Tail', d) for d in range(16)],
        A + 'Pointer_Weak': [pointer('Weak', d) for d in range(16)],
    }
    for name, frames in sheets.items():
        save_sheet(os.path.join(OUT, name + '.png'), frames)
    for name, frames in arrows.items():
        save_sheet(os.path.join(OUT_ARROW, name + '.png'), frames)

    S = lambda n, i=0: sheets[P + n][i]
    rows = [
        mock(S('Frame_Copper'), S('Fill_Copper'), S('Eyes_Hunt'), 0.88, 0.94),
        mock(S('Frame_Copper'), S('Fill_Copper'), S('Eyes_Hunt'), 0.70, 0.70, fx=S('Fx_WallBreak', 1)),
        mock(S('Frame_Ember', 1), S('Fill_Ember', 1), S('Eyes_Burn', 1), 0.52, 0.58, (True, False)),
        mock(S('Frame_Ember', 0), S('Fill_Ember', 0), S('Eyes_Burn', 0), 0.35, 0.35, (True, False), fx=S('Fx_PlatesOpen', 2)),
        mock(S('Frame_Open', 1), S('Fill_Flesh', 1), S('Eyes_Flesh', 1), 0.24, 0.29, (True, True)),
        mock(S('Frame_Raw', 0), S('Fill_Raw', 0), S('Eyes_Roll', 1), 0.07, 0.10, (True, True)),
    ]
    sheet = Image.new('RGBA', (W + 8, (H + 6) * len(rows) + 2), BG)
    for i, row in enumerate(rows):
        sheet.alpha_composite(row, (4, 4 + i * (H + 6)))
    sheet.resize((sheet.width * 4, sheet.height * 4), Image.NEAREST).save(os.path.join(HERE, 'vista_previa_6c2_barra.png'))
    panel = arrows_panel(arrows, A)
    panel.resize((panel.width * 5, panel.height * 5), Image.NEAREST).save(os.path.join(HERE, 'vista_previa_6c2_flechas.png'))

    gd = os.path.join(HERE, 'gif'); os.makedirs(gd, exist_ok=True)
    gif(os.path.join(gd, 'BossBar_WallBreak.gif'),
        [mock(S('Frame_Copper'), S('Fill_Copper'), S('Eyes_Hunt'), 0.70, 0.70, fx=f) for f in sheets[P + 'Fx_WallBreak']], 80, BG, scale=3)
    gif(os.path.join(gd, 'BossBar_PlatesOpen.gif'),
        [mock(S('Frame_Ember', i % 3), S('Fill_Ember', i % 3), S('Eyes_Burn', i % 2), 0.35, 0.35, (True, False), fx=f)
         for i, f in enumerate(sheets[P + 'Fx_PlatesOpen'])], 80, BG, scale=3)
    gif(os.path.join(gd, 'BossBar_Ember.gif'),
        [mock(S('Frame_Ember', i), S('Fill_Ember', i), S('Eyes_Burn', i % 2), 0.52, 0.58, (True, False)) for i in (0, 1, 2)], 120, BG, scale=3)
    gif(os.path.join(gd, 'BossBar_Flesh.gif'),
        [mock(S('Frame_Open', i), S('Fill_Flesh', i), S('Eyes_Flesh', i), 0.24, 0.29, (True, True)) for i in (0, 1)], 260, BG, scale=3)
    gif(os.path.join(gd, 'BossBar_Roll.gif'),
        [mock(S('Frame_Raw', i % 2), S('Fill_Raw', i % 2), S('Eyes_Roll', i), 0.07, 0.10, (True, True)) for i in range(4)], 110, BG, scale=3)
    badges = []
    for i in range(6):
        im = Image.new('RGBA', (4 * 28, 28), BG)
        for j, (n, f) in enumerate((('Badge_Head', 0), ('Badge_HeadWarn', i % 2), ('Badge_Tail', 0), ('Badge_Weak', i % 3))):
            im.alpha_composite(as_img(arrows[A + n][f]), (2 + j * 28, 2))
        badges.append(im)
    gif(os.path.join(gd, 'EdgeArrow_Badges.gif'), badges, 160, BG, scale=5)
    print('Listo en', OUT, 'y', OUT_ARROW)
