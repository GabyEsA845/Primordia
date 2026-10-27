"""Barra de vida del Euriptérido (interfaz, piso 2).

La barra es el propio escorpión marino visto desde arriba, mirando a la izquierda: dos pinzas abiertas con los dedos
dentados, el escudo de la cabeza con los ojos enfadados, las patas y los remos, los segmentos del abdomen a lo largo
de la barra y la cola acabada en púa a la derecha. Mismas medidas que la del Anomalocaris: 258×32 con la ventana de
200×8 en (38, 12). Piezas (un píxel de interfaz = un píxel del juego):
- Frame_Armor / Frame_Soft: el animal acorazado (verde bronce, con púas en los segmentos) y blando (rosa, liso).
- Frame_Crack: la muda (3 cuadros en bucle): el caparazón agrietado con las grietas que brillan.
- Frame_Fury: el último aliento (2 cuadros): cuerpo blando con la púa de la cola encendida en verde.
- Back, Fill_Armor / Fill_Soft / Fill_Molt / Fill_Venom (2 cuadros que laten) / Fill_Trail: 200×8.
- Eyes_Armor / Eyes_Soft / Eyes_Molt (cerrados) / Eyes_Fury (rojos, 2 cuadros): 258×32 sobre el marco.
- Notch / Notch_Broken (8×32) y Fx_NotchBreak (5 cuadros de 16×34): la placa de quitina del 50 %.
- Fx_ShellBurst (6 cuadros de 258×32): al acabar la muda el caparazón salta en pedazos.

Hecho con la skill primordia-sprites (pixel.py): luz arriba a la izquierda, contorno OUTL; paletas del Euriptérido.

Uso: python generar_barra_euripterido.py [carpeta_salida]   (por defecto, la carpeta de este script)
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
OUT = os.path.join(sys.argv[1] if len(sys.argv) > 1 else HERE, 'BossBar')
BG = C('#0b1220')

# --- paletas (las de generar_euripterido.py) -------------------------------------------------------
ARMOR = dict(body=['#16241e', '#2c4a38', '#527a52', '#9cc086'], limb=['#1e3028', '#3a5c44', '#6a9466', '#b4d498'], spikes=True)
SOFT = dict(body=['#5a2c3c', '#9a5466', '#d8949a', '#ffdcd2'], limb=['#6a3446', '#aa6070', '#e4a4a4', '#fff0e4'], spikes=False)
TOOTH = ['#5a4636', '#a88c72', '#e4d0b4', '#fff4e0']          # dientes de las pinzas
CHITIN = ['#5a5040', '#8a7c60', '#b8a888', '#e0d4b8']         # placa de la muesca (la muda vacía)
GLOW = ['#c8a040', '#ffe08a', '#fff8d8']                      # grietas de la muda
VENOM = ['#1a4a10', '#3a9a20', '#7ae040', '#d0ff90']
WINDOW_BACK = ['#0e0a10', '#1a1218', '#241a20']

FILLS = {
    'Armor': ['#2a3a14', '#5a7a2a', '#9ab84a', '#dcf08a'],    # verde bronce, más claro que el caparazón
    'Soft': ['#6a2438', '#c04a66', '#f08aa0', '#ffd4dc'],     # rosa carne
    'Molt': ['#3a3624', '#6a6440', '#a09a64', '#e0dca4'],     # pálido, como la muda vacía
    'Venom': VENOM,
    'Trail': ['#b8a8a4', '#dcd0cc', '#f4ece6', '#fffaf4'],    # barra fantasma
}

W, H = 258, 32
WX0, WX1, WY0, WY1 = 38, 238, 12, 20     # ventana (x1, y1 exclusivos)
CY = 16.0
PLATE = 10                               # largo de cada segmento del abdomen
EYE_TOP = (27, 6)                        # esquina de arriba a la izquierda del ojo de arriba (6×5)

def _mirror_y(y): return 2 * int(CY) - 1 - y

def _poly(pts):
    def inside(x, y):
        c = False
        for (x0, y0), (x1, y1) in zip(pts, pts[1:] + pts[:1]):
            if (y0 > y) != (y1 > y) and x < x0 + (y - y0) * (x1 - x0) / (y1 - y0): c = not c
        return c
    return inside

# Escudo de la cabeza (prosoma): trapecio redondeado, más estrecho delante (izquierda).
SHIELD_TOP = [(44, 9.0), (42, 5.0), (36, 3.6), (28, 3.8), (21, 5.6), (16, 9.0), (14, 13.0), (13.5, 16.0)]
# Pinza de arriba: brazo que sale del escudo hacia delante y afuera, y la mano con los dos dedos abiertos.
ARM_TOP = [(18.0, 12.0), (14.0, 9.4), (10.5, 7.2)]
HAND_TOP = (9.6, 6.6, 2.8, 2.4)                                   # mano: centro y radios
FINGER_OUT = [(8.6, 4.6), (6.4, 3.0), (4.0, 2.4), (2.8, 3.0), (2.5, 4.4)]       # dedo de fuera: se curva hacia dentro
FINGER_IN = [(8.6, 8.6), (6.4, 11.0), (4.0, 12.0), (2.9, 11.4), (2.6, 9.8)]     # dedo de dentro: se curva hacia fuera

def _band(path, r0, r1):
    """Píxeles de una banda que sigue el camino, del grosor r0 al principio a r1 al final: {(x, y): índice}."""
    segs, L = [], 0.0
    for (x0, y0), (x1, y1) in zip(path, path[1:]):
        l = math.hypot(x1 - x0, y1 - y0); segs.append((x0, y0, x1, y1, l, L)); L += l
    out = {}
    for y in range(int(CY)):
        for x in range(26):
            px, py = x + .5, y + .5
            best = None
            for x0, y0, x1, y1, l, s0 in segs:
                dx, dy = (x1 - x0) / l, (y1 - y0) / l
                t = max(0.0, min(l, (px - x0) * dx + (py - y0) * dy))
                qx, qy = x0 + dx * t, y0 + dy * t
                d = math.hypot(px - qx, py - qy)
                if best is None or d < best[0]:
                    best = (d, s0 + t, dx * (py - qy) - dy * (px - qx))
            d, sl, v = best
            r = r0 + (r1 - r0) * sl / L
            if d > r: continue
            out[(x, y)] = 3 if v < -r * 0.3 else 2 if v < r * 0.35 else 1
    return out, segs, L

def _samples(path, count, t0=0.25, t1=0.85):
    """Puntos repartidos a lo largo del camino, del tramo t0 al t1 de su largo."""
    segs, L = [], 0.0
    for (x0, y0), (x1, y1) in zip(path, path[1:]):
        l = math.hypot(x1 - x0, y1 - y0); segs.append((x0, y0, x1, y1, l, L)); L += l
    out = []
    for k in range(count):
        sl = L * (t0 + (t1 - t0) * k / max(1, count - 1))
        for x0, y0, x1, y1, l, s0 in segs:
            if s0 <= sl <= s0 + l:
                t = (sl - s0) / l
                out.append((x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, sl / L))
                break
    return out

def pincers(cv, pal):
    """Las dos pinzas (la de abajo es la de arriba en espejo), detrás del escudo: brazo, mano gruesa y dos dedos largos."""
    limb = pal['limb']
    pieces = [(_band(ARM_TOP, 2.0, 1.6)[0], 0), (_band(FINGER_OUT, 1.5, 0.5)[0], 0), (_band(FINGER_IN, 1.4, 0.5)[0], -1)]
    for side in (0, 1):
        for band, shift in pieces:
            for (x, y), i in band.items():
                yy = _mirror_y(y) if side else y
                cv.put(x, yy, C(limb[max(0, min(3, i + shift - side))]))
        hx, hy, rx, ry = HAND_TOP
        ellipse(cv, hx, (2 * CY - hy) if side else hy, rx, ry, limb, shift=-side)

def pincer_teeth(cv, dark=False):
    """Fila de dientes claros en el borde de dentro de cada dedo, hacia el hueco de la pinza (sin contorno: son finos)."""
    for side in (0, 1):
        for path, r0, r1, down in ((FINGER_OUT, 1.5, 0.5, 1), (FINGER_IN, 1.4, 0.5, -1)):
            for qx, qy, t in _samples(path, 4, 0.2, 0.75):
                r = r0 + (r1 - r0) * t
                for k, col in ((1, TOOTH[2]), (2, TOOTH[3])):
                    y = qy + down * (r + k * 0.9)
                    if side:
                        y = 2 * CY - y
                    cv.put_out(qx, y, C(col))

def legs(cv, pal):
    """Tres pares de patas finas que asoman por los lados del escudo (sin contorno) y los dos remos grandes detrás."""
    limb = pal['limb']
    for side in (0, 1):
        for k, x in enumerate((20, 25, 30)):
            for t in range(4):
                xx = x - t * 0.6 - k * 0.2
                y = 4.2 - t
                cv.put_out(xx, _mirror_y(int(y)) if side else y, C(limb[1 if t < 3 else 2]))

def paddles(cv, pal):
    for side in (0, 1):
        y = 4.6 if side == 0 else _mirror_y(4) + 0.4
        rot_ellipse(cv, 46.5, y, 5.2, 2.3, 0.45 if side == 0 else -0.45, pal['limb'], shift=-side)

def shield(cv, pal):
    pts = SHIELD_TOP + [(x, 2 * CY - y) for x, y in reversed(SHIELD_TOP[:-1])]
    fill(cv, _poly(pts), 30, CY - 3, 16, 12, pal['body'], box=(10, 1, 48, 31))

def shield_details(cv, pal):
    body = pal['body']
    # cresta central y surco del borde: el borde claro es el frente blindado
    for x in range(16, 43):
        cv.put_in(x, 15, C(body[0] if x % 5 else OUTL)); cv.put_in(x, 14, C(body[2]))
    for x, y in ((22, 6), (24, 5), (27, 4), (31, 4), (35, 4), (38, 5)):
        cv.put_in(x, y, C(body[3]))
    # ceño: arista oscura en V sobre cada ojo, hacia delante y hacia el centro
    for side in (0, 1):
        for k, (x, y) in enumerate(((33, 7), (32, 8), (31, 9), (30, 10), (29, 10), (28, 11), (27, 11))):
            cv.put_in(x, _mirror_y(y) if side else y, OUTL if k < 5 else C(body[0]))

def abdomen(cv, pal):
    """Segmentos por encima y por debajo de la ventana; acorazado, con una púa en el borde de cada segmento."""
    body = pal['body']
    for x in range(WX0 - 1, WX1 + 2):
        seg = (x - WX0) % PLATE
        bulge = 1 if 2 <= seg <= PLATE - 3 else 0
        for y in range(WY0 - 3 - bulge, WY0):
            cv.put(x, y, C(body[3] if y == WY0 - 3 - bulge else body[2] if y < WY0 - 1 else body[1]))
        for y in range(WY1, WY1 + 3 + bulge):
            cv.put(x, y, C(body[1] if y == WY1 else body[0]))
        if seg == PLATE - 1:
            for y in list(range(WY0 - 4, WY0)) + list(range(WY1, WY1 + 4)):
                cv.put(x, y, C(body[0]))
    if pal['spikes']:
        for x in range(WX0 + PLATE - 2, WX1 - 2, PLATE):
            for k in range(3):
                cv.put(x - k * 0.5, WY0 - 5 - k, C(body[2 if k < 2 else 3]))
                cv.put(x - k * 0.5, WY1 + 4 + k, C(body[1 if k < 2 else 2]))

def tail(cv, pal, glow=0):
    """Cola: segmentos que se estrechan tras la ventana y la púa (telson) hacia la derecha."""
    body = pal['body']
    for x in range(WX1, 248):
        half = 6.0 - (x - WX1) * 0.32
        for y in range(H):
            if abs(y + .5 - CY) <= half:
                i = 3 if y + .5 < CY - half + 1.2 else 2 if y + .5 < CY else 1 if y + .5 < CY + half - 1 else 0
                if (x - WX1) % 4 == 3: i = max(0, i - 1)
                cv.put(x, y, C(body[i]))
    spike = VENOM if glow else body
    for x in range(247, W - 2):
        half = 2.6 * (W - 2 - x) / (W - 249)
        for y in range(H):
            if abs(y + .5 - CY) <= half + 0.3:
                i = 3 if y + .5 < CY - 0.5 else 2 if y + .5 < CY + 0.5 else 1
                cv.put(x, y, C(spike[i]))

def tail_glow(cv, glow):
    """Halo y chispas de la púa encendida (por encima del contorno)."""
    if not glow: return
    rnd = random.Random(glow)
    for _ in range(5 + glow * 3):
        x, y = rnd.uniform(244, W - 2), CY + rnd.uniform(-6, 6)
        cv.put_out(x, y, VENOM[3][:7] and C(VENOM[3 if glow == 2 else 2], 200))
    cv.put(W - 3, int(CY), C('#ffffff'))

def frame(pal, crack=0, glow=0):
    cv = Canvas(W, H)
    paddles(cv, pal)
    pincers(cv, pal)
    abdomen(cv, pal)
    tail(cv, pal, glow)
    shield(cv, pal)
    cv.outline(); cv.clean()
    legs(cv, pal)
    for x in range(WX0, WX1):
        for y in range(WY0, WY1):
            cv.px.pop((x, y), None)
    for x in range(WX0 - 1, WX1 + 1):
        cv.put(x, WY0 - 1, OUTL); cv.put(x, WY1, OUTL)
    for y in range(WY0, WY1):
        cv.put(WX0 - 1, y, OUTL); cv.put(WX1, y, OUTL)
    shield_details(cv, pal)
    pincer_teeth(cv)
    if crack:
        cracks(cv, crack)
    tail_glow(cv, glow)
    return cv

def cracks(cv, k):
    """Grietas de la muda que brillan (k = 1..3, la luz va y viene)."""
    rnd = random.Random(7)
    lines = []
    # en el escudo, desde la cresta hacia los bordes
    for sx, sy, dx, dy, n in ((24, 15, 0.6, -1, 9), (34, 15, -0.3, -1, 10), (28, 16, 0.5, 1, 9), (38, 16, 0.2, 1, 8)):
        x, y = sx, sy
        for i in range(n):
            lines.append((x, y)); x += dx + rnd.choice((-0.5, 0, 0.5)); y += dy
    # en el abdomen, una grieta corta en cada tres segmentos
    for x0 in range(WX0 + 4, WX1 - 4, PLATE * 3):
        for i in range(3):
            lines.append((x0 + i, WY0 - 2 - i % 2)); lines.append((x0 + 3 + i, WY1 + 1 + i % 2))
    for x, y in lines:
        if (int(x), int(y)) in cv.px and cv.px[(int(x), int(y))] != OUTL:
            cv.put(x, y, C(GLOW[min(2, k - 1)]))
            if k == 3: cv.put_in(x + 1, y, C(GLOW[0]))

# --- ojos ----------------------------------------------------------------------------------------
SHAPE = [(dx, dy) for dx in range(6) for dy in range(5) if (dx, dy) not in ((0, 0), (5, 0), (0, 4), (5, 4))]
LID = {(0, 0), (1, 0), (2, 0), (3, 0), (0, 1), (1, 1), (0, 2)}          # párpado en cuña: ceño hacia delante

def eyes(state, frame_index=0, pal=ARMOR):
    cv = Canvas(W, H)
    ex, ey = EYE_TOP
    for bottom in (0, 1):
        put = lambda dx, dy, c: cv.put(ex + dx, (_mirror_y(ey + dy) if bottom else ey + dy), c)
        if state == 'Molt':
            for dx, dy in SHAPE:
                put(dx, dy, C(pal['body'][2 if dy < 2 else 1]))
            for dx in range(6): put(dx, 2, OUTL)
            continue
        red = state == 'Fury'
        for dx, dy in SHAPE: put(dx, dy, C('#ff5a4a') if red else C(EYE))
        for dx, dy in LID: put(dx, dy, C(pal['body'][3 - bottom] if dy == 0 else pal['body'][2 - bottom]))
        for dx in range(6):
            edge = max([dy for (x, dy) in LID if x == dx], default=-1)
            if (dx, edge + 1) in SHAPE: put(dx, edge + 1, OUTL)
        for dx, dy in ((1, 3), (2, 3), (1, 2), (2, 2)):
            if (dx, dy) in SHAPE and (dx, dy) not in LID: put(dx, dy, C('#5a0614') if red else OUTL)
        if red and frame_index == 1:
            for dx, dy in ((-1, 2), (6, 2), (2, -1), (3, -1), (2, 5), (3, 5)):
                put(dx, dy, C('#ff4a3a', 150))
            put(4, 2, C('#fff0e0'))
    return cv

# --- ventana -------------------------------------------------------------------------------------
def window_fill(ramp, bright=False):
    w, h = WX1 - WX0, WY1 - WY0
    cv = Canvas(w, h)
    for x in range(w):
        seam = x % PLATE == PLATE - 1
        for y in range(h):
            i = 3 if y == 0 else 2 if y < 3 else 1 if y < 6 else 0
            if seam: i = max(0, i - 1)
            if bright and y in (1, 2, 3) and x % PLATE in (1, 2, 3, 4, 5): i = 3
            cv.put(x, y, C(ramp[i]))
    return cv

def back():
    w, h = WX1 - WX0, WY1 - WY0
    cv = Canvas(w, h)
    for x in range(w):
        for y in range(h):
            cv.put(x, y, C(WINDOW_BACK[0 if y < 2 else 1 if y < 6 else 2]))
    return cv

# --- muesca y salto del caparazón ------------------------------------------------------------------
def notch(broken=False):
    cv = Canvas(8, H)
    ramp = CHITIN if not broken else ['#3a3428', '#5a5040', '#7a6e58', '#9a8c70']
    for y in range(2, H - 2):
        half = min(1.2 + min(y - 2, H - 3 - y) * 0.35, 2.6)
        for x in range(8):
            if abs(x + 0.5 - 4) <= half:
                if broken and 7 <= y <= 18: continue
                i = 3 if x < 3 and y < 11 else 2 if x < 4 else 1
                if abs(x + 0.5 - 4) > half - 1: i = max(0, i - 1)
                if y % 6 == 5: i = max(0, i - 1)                   # anillos de la quitina
                cv.put(x, y, C(ramp[i]))
    if broken:
        for x, y in ((3, 6), (4, 7), (5, 6), (3, 19), (4, 18), (5, 19)):
            cv.put(x, y, C(ramp[0]))
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
        for y in range(9, 19): out.put_in(8 + (y % 2), y, C(CHITIN[0]))
    if k == 1:
        sparks(out, 8, 13, 2)
    if k >= 2:
        d = (k - 1) * 1.7
        for dx, dy in ((-1, -1), (1, -0.6), (-0.8, 0.9), (1.1, 1)):
            col = C(CHITIN[2] if k < 4 else CHITIN[1], 255 - 60 * (k - 2))
            out.put(8 + dx * d, 14 + dy * d, col); out.put(9 + dx * d, 14 + dy * d, col)
    return out

def shell_burst(k, n=6):
    """El caparazón verde salta en pedazos al acabar la muda: esquirlas que salen de las bandas y del escudo."""
    cv = Canvas(W, H)
    rnd = random.Random(3)
    body = ARMOR['body']
    t = (k + 1) / n
    for i in range(46):
        x0 = rnd.uniform(14, WX1 + 8)
        up = rnd.random() < 0.5
        y0 = rnd.uniform(4, 10) if up else rnd.uniform(21, 27)
        vx, vy = rnd.uniform(-3, 3), (-1 if up else 1) * rnd.uniform(4, 9)
        x, y = x0 + vx * t * 2, y0 + vy * t * 1.4
        if not (0 <= y < H): continue
        alpha = int(255 * (1 - t * 0.8))
        size = 2 if i % 3 else 1
        for dx in range(size):
            cv.put(x + dx, y, C(body[2 if dx == 0 else 1], alpha))
        cv.put(x, y + 1, C(body[0], alpha))
    if k == 0:
        for x, y in ((24, 8), (60, 11), (120, 11), (180, 20), (30, 24)):
            sparks(cv, x, y, 2)
    elif k == 1:
        for x, y in ((40, 6), (150, 22), (210, 9)):
            sparks(cv, x, y, 1)
    return cv

# --- salida --------------------------------------------------------------------------------------
def mock(frame_cv, fill_ramp, eye_cv, ratio, trail, broken, bright=False):
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    img.alpha_composite(back().img(), (WX0, WY0))
    ww = WX1 - WX0
    img.alpha_composite(window_fill(FILLS['Trail']).img().crop((0, 0, max(1, int(ww * trail)), WY1 - WY0)), (WX0, WY0))
    img.alpha_composite(window_fill(fill_ramp, bright).img().crop((0, 0, max(1, int(ww * ratio)), WY1 - WY0)), (WX0, WY0))
    img.alpha_composite(as_img(frame_cv), (0, 0))
    img.alpha_composite(notch(broken=broken).img(), (WX0 + int(ww * 0.5) - 4, 0))
    img.alpha_composite(as_img(eye_cv), (0, 0))
    return img

if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    P = 'UI_BossBar_Eurypterid_'
    sheets = {
        P + 'Frame_Armor': [frame(ARMOR)],
        P + 'Frame_Soft': [frame(SOFT)],
        P + 'Frame_Crack': [frame(ARMOR, crack=k) for k in (1, 2, 3)],
        P + 'Frame_Fury': [frame(SOFT, glow=g) for g in (1, 2)],
        P + 'Back': [back()],
        P + 'Fill_Armor': [window_fill(FILLS['Armor'])],
        P + 'Fill_Soft': [window_fill(FILLS['Soft'])],
        P + 'Fill_Molt': [window_fill(FILLS['Molt'])],
        P + 'Fill_Venom': [window_fill(FILLS['Venom']), window_fill(FILLS['Venom'], bright=True)],
        P + 'Fill_Trail': [window_fill(FILLS['Trail'])],
        P + 'Eyes_Armor': [eyes('Armor', pal=ARMOR)],
        P + 'Eyes_Soft': [eyes('Soft', pal=SOFT)],
        P + 'Eyes_Molt': [eyes('Molt', pal=ARMOR)],
        P + 'Eyes_Fury': [eyes('Fury', i, pal=SOFT) for i in range(2)],
        P + 'Notch': [notch()],
        P + 'Notch_Broken': [notch(broken=True)],
        P + 'Fx_NotchBreak': [notch_break(k) for k in range(5)],
        P + 'Fx_ShellBurst': [shell_burst(k) for k in range(6)],
    }
    for name, frames in sheets.items():
        save_sheet(os.path.join(OUT, name + '.png'), frames)

    # Vista previa ×5: acorazado al 85 %, muda al 50 %, blando al 30 %, último aliento al 60 % y el salto del caparazón.
    rows = [
        mock(sheets[P + 'Frame_Armor'][0], FILLS['Armor'], sheets[P + 'Eyes_Armor'][0], 0.85, 0.93, False),
        mock(sheets[P + 'Frame_Crack'][2], FILLS['Molt'], sheets[P + 'Eyes_Molt'][0], 0.5, 0.5, False),
        mock(sheets[P + 'Frame_Soft'][0], FILLS['Soft'], sheets[P + 'Eyes_Soft'][0], 0.3, 0.36, True),
        mock(sheets[P + 'Frame_Fury'][1], FILLS['Venom'], sheets[P + 'Eyes_Fury'][1], 0.6, 0.62, True, bright=True),
    ]
    burst = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    burst.alpha_composite(rows[2])
    burst.alpha_composite(as_img(sheets[P + 'Fx_ShellBurst'][1]))
    rows.append(burst)
    sheet = Image.new('RGBA', (W + 8, (H + 6) * len(rows) + 2), BG)
    for i, row in enumerate(rows):
        sheet.alpha_composite(row, (4, 4 + i * (H + 6)))
    sheet.resize((sheet.width * 5, sheet.height * 5), Image.NEAREST).save(os.path.join(HERE, 'vista_previa_barra_euripterido.png'))
    os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True)
    gif(os.path.join(HERE, 'gif', 'Eurypterid_ShellBurst.gif'), [Image.alpha_composite(rows[2], as_img(f)) for f in sheets[P + 'Fx_ShellBurst']],
        80, BG, scale=4)
    print('Listo en', OUT)
