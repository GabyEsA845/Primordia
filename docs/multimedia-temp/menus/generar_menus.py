"""
generar_menus.py - Primordia: menú de pausa y menú principal.

Reutiliza generar_ui.py (que a su vez carga generar_mutaciones.py y
generar_jugador.py): Canvas/Cv, contorno #181425, rampas, colores de linaje,
el 9-slice de membrana de UI_Panel/UI_Card/UI_Button y UI_Slot_Frame.

Uso:
  python generar_menus.py [carpeta_multimedia_temp] [carpeta_salida]
Por defecto: la carpeta de arriba (donde están player/, mutations/, ui/,
environment/ y enemies/) y la carpeta de este script.
"""
import importlib.util
import math
import os
import random
import sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, '..')
OUT = sys.argv[2] if len(sys.argv) > 2 else HERE

# --------------------------------------------- cargar los otros generadores
_argv = sys.argv
sys.argv = ['generar_ui.py', ROOT, os.devnull]
_spec = importlib.util.spec_from_file_location('generar_ui', os.path.join(ROOT, 'ui', 'generar_ui.py'))
U = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(U)
sys.argv = _argv
M = U.M

C, CA, OUTL = U.C, U.CA, U.OUTL
Cv, ell, thick, col, nine = U.Cv, U.ell, U.thick, U.col, U.nine
shade_v, ramp_idx = U.shade_v, U.ramp_idx
MEM, MEM_HI, DARK_MEM, LINEAGES = U.MEM, U.MEM_HI, U.DARK_MEM, U.LINEAGES
BONE, FLESH, FIN, LIME, PURP, HEART = U.BONE, U.FLESH, U.FIN, U.LIME, U.PURP, U.HEART
BG = U.BG
W, H = 480, 270

# agua clara de la lente y vidrio
WATER = ['#16352e', '#1b4038', '#204a41', '#26554a']
GLASS = ['#1e4a46', '#3a7a74', '#7cc4bc', '#d8fff6']
AQUA = ['#1e6a5a', '#2f9a80', '#5ad0b0', '#b0f0e0']
LINE = '#b8f0dc'                                         # trazo de las líneas de las piezas


def dist(x, y, cx, cy):
    return math.hypot(x + .5 - cx, y + .5 - cy)


# ======================================================== A. MENÚ DE PAUSA
LENS = 112
LC = 56
R_IN = 47.5                     # interior: cabe 96x96 (una célula de 32x32 a x3)
R_OUT = 55.0


def pause_lens():
    cv = Cv(LENS)
    for y in range(LENS):
        for x in range(LENS):
            d = dist(x, y, LC, LC)
            if d > R_OUT:
                continue
            if d <= R_IN:
                # agua algo más clara que el fondo, con viñeta suave (escalones con tramado)
                t = d / R_IN
                lvl = 3 - t * 3.2
                base = int(lvl)
                frac = lvl - base
                bayer = ((x % 2) * 2 + (y % 2) * 3) % 4 / 4
                i = max(0, min(3, base + (1 if frac > bayer + 0.12 else 0)))
                cv.put(x, y, WATER[i])
            else:
                # aro de vidrio grueso, luz arriba a la izquierda
                a = math.atan2(y + .5 - LC, x + .5 - LC)
                light = math.cos(a + 3 * math.pi / 4)         # 1 arriba-izquierda, -1 abajo-derecha
                r = (d - R_IN) / (R_OUT - R_IN)              # 0 dentro, 1 fuera
                if r < 0.22:
                    i = 0                                     # canto interior, oscuro
                elif r > 0.82:
                    i = 1 if light < 0.3 else 2
                else:
                    i = 3 if light > 0.75 and 0.35 < r < 0.7 else 2 if light > -0.1 else 1
                cv.put(x, y, GLASS[i])
    # marcas de graduación del microscopio en el aro
    for k in range(12):
        a = k * math.pi / 6
        for rr in (R_OUT - 2.2, R_OUT - 3.2):
            cv.put(LC - .5 + math.cos(a) * rr, LC - .5 + math.sin(a) * rr, GLASS[0] if k % 3 else '#0e2a26')
    cv.outline()
    # contorno interior entre vidrio y agua
    for y in range(LENS):
        for x in range(LENS):
            d = dist(x, y, LC, LC)
            if R_IN - 0.5 < d <= R_IN + 0.5:
                cv.put(x, y, OUTL)
    return cv


def pause_glint():
    cv = Cv(LENS)
    for y in range(LC):
        for x in range(LC):
            d = dist(x, y, LC, LC)
            a = math.degrees(math.atan2(y + .5 - LC, x + .5 - LC))
            if -165 < a < -110 and abs(d - 41) < 1.3:
                cv.put(x, y, (232, 255, 246, 110))
            if -150 < a < -125 and abs(d - 37.5) < 0.6:
                cv.put(x, y, (232, 255, 246, 60))
    for (x, y, a) in ((21, 30, 150), (22, 29, 90), (27, 23, 70)):
        cv.put(x, y, (255, 255, 255, a))
    return cv


BUBBLES = [(30, 84, 0, 1.3), (44, 92, 3, 0.8), (70, 88, 5, 1.6), (82, 74, 2, 0.8), (62, 70, 6, 1.0),
           (38, 64, 4, 0.8), (76, 52, 1, 1.2), (26, 50, 7, 0.8)]


def bubble_px(cv, x, y, r, a):
    if r < 1:
        cv.put(x, y, (200, 255, 236, a))
        return
    for yy in range(int(y - r - 1), int(y + r + 2)):
        for xx in range(int(x - r - 1), int(x + r + 2)):
            d = math.hypot(xx + .5 - x, yy + .5 - y)
            if r - 0.6 < d <= r + 0.4:
                cv.put(xx, yy, (180, 240, 220, a))
    cv.put(x - r * 0.5, y - r * 0.5, (255, 255, 255, min(255, a + 40)))


def lens_bubbles(f):
    cv = Cv(LENS)
    for (x, y, ph, r) in BUBBLES:
        t = (f + ph) % 8
        yy = y - t * 2
        xx = x + math.sin((t + ph) * 0.8) * 0.8
        a = [60, 130, 170, 170, 170, 150, 110, 50][t]
        if dist(xx, yy, LC, LC) < R_IN - 3:
            bubble_px(cv, xx, yy, r, a)
    return cv


def callout_line():
    cv = Cv(3)
    cv.put(1, 1, LINE)
    cv.outline()
    return cv


def callout_dot(f):
    cv = Cv(5)
    r = [1.4, 1.7, 1.9, 1.7][f]
    for y in range(5):
        for x in range(5):
            d = dist(x, y, 2.5, 2.5)
            if d <= r:
                cv.put(x, y, '#ffffff' if d < 0.6 else LINE)
    cv.outline()
    return cv


def slot_frame_hi():
    """UI_Slot_Frame con el borde más claro y brillante."""
    cv = Cv(20)
    for y in range(20):
        for x in range(20):
            ax, ay = abs(x + .5 - 10), abs(y + .5 - 10)
            d = math.hypot(max(ax - 6.5, 0), max(ay - 6.5, 0))
            if d > 3.2:
                continue
            if ax < 8 and ay < 8:
                cv.put(x, y, (30, 70, 58, 175))
            else:
                v = (-0.6 * (x - 10) - 0.8 * (y - 10)) / 10
                cv.put(x, y, MEM_HI[3] if v > 0.3 else MEM_HI[2] if v > -0.35 else MEM_HI[1])
    cv.outline()
    return cv


TIP_RIM = ['#0e201b', '#1e4a3e', '#3a7a64', '#8fd0b0']
TIP_FILL = '#0b1d19'


def tooltip():
    return nine(24, 24, 6, TIP_RIM, TIP_FILL, 5, bubbles=False, alpha_fill=245)


def tooltip_pointer():
    """Piquito que apunta a la izquierda. Se solapa 2 px con el borde izquierdo del cuadro."""
    cv = Cv(6, 7)
    for y in range(7):
        for x in range(6):
            half = (x + .5) * 0.62                       # se abre hacia la derecha
            if abs(y + .5 - 3.5) > half:
                continue
            edge = abs(y + .5 - 3.5) > half - 1.0
            if x >= 4:
                cv.put(x, y, col(TIP_FILL, 245) if 1 <= y <= 5 else TIP_RIM[1])
            elif edge:
                cv.put(x, y, TIP_RIM[3] if y < 3.5 else TIP_RIM[1])
            else:
                cv.put(x, y, col(TIP_FILL, 245))
    cv.outline()
    for y in range(7):                                    # lado derecho abierto (se une al cuadro)
        if cv.px.get((5, y), (0, 0, 0, 0))[:3] == OUTL[:3]:
            del cv.px[(5, y)]
    return cv


def stats_panel():
    return nine(48, 48, 10, ['#0e201b', '#173a32', '#24524a', '#3f7a68'], '#0b1a16', 7, alpha_fill=225)


def stats_row():
    cv = Cv(16, 14)
    for y in range(1, 13):
        for x in range(16):
            ax = min(x + .5, 16 - x - .5)
            if ax < 3 and abs(y + .5 - 7) > 2 + ax * 1.7:       # extremos redondeados
                continue
            a = 70 if y == 2 else 40
            cv.put(x, y, col(MEM[2] if y == 2 else MEM[1], a))
    return cv


# ---------------------------------------------- iconos de estadística 12x12
# Reglas: silueta de ~10x10 centrada en 12x12, contorno #181425 de 1 px,
# rampa de 4 tonos con luz arriba a la izquierda, un píxel de brillo blanco
# arriba a la izquierda, y un color propio por estadística.
def stat_health():
    cv = Cv(12)
    for y in range(12):
        for x in range(12):
            px, py = x + .5, y + .5
            l = (px - 4.0) ** 2 + (py - 4.5) ** 2 <= 2.3 ** 2
            r = (px - 8.0) ** 2 + (py - 4.5) ** 2 <= 2.3 ** 2
            tri = 4.5 <= py <= 10 and abs(px - 6) <= (10.2 - py) * 0.75
            if l or r or tri:
                cv.put(x, y, HEART[ramp_idx(shade_v((px - 6) / 5, (py - 6) / 5) + 0.15)])
    cv.put(3, 4, '#ffffff')
    cv.put(4, 3, '#fff0e0')
    cv.outline()
    return cv


def stat_damage():
    cv = Cv(12)
    # púa de hueso en diagonal, base carnosa cálida abajo a la izquierda
    pts = [(3.2, 8.8), (5.6, 6.2), (8.0, 3.8), (9.8, 2.0)]
    thick(cv, pts, [3.4, 2.6, 1.4], lambda xx, yy, x, y, r: BONE[3] if (xx - x) + (yy - y) < -0.6 else BONE[2] if (xx - x) + (yy - y) < 0.5 else BONE[1])
    ell(cv, 3.0, 9.0, 2.4, 2.2, FLESH)
    cv.put(5, 5, '#ffffff')
    cv.outline()
    return cv


def stat_speed():
    cv = Cv(12)
    # aleta lanzada hacia la derecha con tres líneas de movimiento detrás
    M.fin(cv, (5.0, 6.0), 0.0, 0.85, 6.6, 0.4, alpha=255)
    for (x0, y) in ((0, 3), (1, 6), (0, 9)):
        for x in range(x0, x0 + 3):
            cv.put(x, y, AQUA[3] if x > x0 else AQUA[2])
    cv.outline()
    cv.put(7, 4, '#ffffff')
    return cv


def stat_arrow(up):
    cv = Cv(7)
    ramp = LIME if up else ['#7a1e2a', '#c8384a', '#f0706e', '#ffd0c0']
    rows = ['...#...', '..###..', '.#####.', '..###..', '..###..', '.......', '.......']
    rows = rows[:5]
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch == '#':
                yy = y + 1 if up else 5 - y
                cv.put(x, yy, ramp[3] if (x <= 2 and y < 3) else ramp[2] if y < 3 else ramp[1])
    cv.outline()
    return cv


def pause_title():
    """Cinta de membrana con las puntas en cola de golondrina (9-slice izquierda/derecha 8 px)."""
    cv = Cv(32, 16)
    for y in range(16):
        for x in range(32):
            ax = min(x + .5, 32 - x - .5)
            yy = y + .5
            if yy < 2.5 or yy > 13.5:
                continue
            if ax < 4 and abs(yy - 8) < 4 - ax:              # muesca de la cola
                continue
            if yy < 4.5:
                c = AQUA[3] if yy < 3.5 else AQUA[2]
            elif yy > 11.5:
                c = AQUA[0]
            else:
                c = AQUA[1]
            if 4 <= ax < 7 and 3.5 < yy < 12.5:              # pliegue de la cinta
                c = AQUA[0] if yy > 8 else AQUA[1]
            cv.put(x, y, c)
    cv.outline()
    return cv


# ========================================================= B. MENÚ PRINCIPAL
def bayer4(x, y):
    m = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]
    return m[y % 4][x % 4] / 16


FAR = ['#16352e', '#18392f', '#1a3e34', '#1c4339', '#1f483e']


def bg_far():
    im = Image.new('RGBA', (W, H))
    px = im.load()
    rnd = random.Random(1)
    for y in range(H):
        t = 1 - y / (H - 1)                                  # 1 arriba, 0 abajo
        lvl = t * (len(FAR) - 1)
        for x in range(W):
            i = int(lvl) + (1 if (lvl - int(lvl)) > bayer4(x, y) else 0)
            px[x, y] = C(FAR[min(i, len(FAR) - 1)])
    for _ in range(260):                                     # textura muy sutil
        x, y = rnd.randrange(W), rnd.randrange(H)
        r, g, b, a = px[x, y]
        px[x, y] = (r + 4, g + 6, b + 5, 255)
    return im


RAYS = [(70, 26, 0.0), (150, 16, 1.3), (215, 34, 2.1), (300, 20, 3.4), (380, 28, 4.6), (445, 14, 5.5)]


def bg_rays(f):
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = im.load()
    ph = f * 2 * math.pi / 8
    slope = 0.42
    for y in range(H):
        fall = max(0.0, 1 - y / 210)                        # se apagan hacia abajo
        if fall <= 0:
            continue
        for (x0, w, p) in RAYS:
            cx = x0 + y * slope + math.sin(y * 0.035 + ph + p) * 4
            ww = w * (1 + 0.15 * math.sin(ph * 1 + p))
            for x in range(int(cx - ww / 2 - 1), int(cx + ww / 2 + 2)):
                if not 0 <= x < W:
                    continue
                e = 1 - abs(x + .5 - cx) / (ww / 2)
                if e <= 0:
                    continue
                v = e * fall * (0.75 + 0.25 * math.sin(ph + p))
                lvl = 3 if v > 0.55 else 2 if v > 0.3 else 1 if v > 0.1 else 0
                if lvl:
                    a = (0, 9, 16, 24)[lvl]
                    old = px[x, y][3]
                    px[x, y] = (200, 255, 230, max(old, a))
    return im


def put_wrap(px, x, y, c, w=W, h=H):
    px[int(round(x)) % w, int(round(y)) % h] = c


def bg_particles():
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = im.load()
    rnd = random.Random(7)
    for _ in range(170):                                     # motas de 1 px
        put_wrap(px, rnd.randrange(W), rnd.randrange(H), (170, 230, 200, rnd.choice((50, 80, 110))))
    for _ in range(26):                                      # burbujitas
        x, y, r = rnd.randrange(W), rnd.randrange(H), rnd.choice((1, 1.5, 2))
        for yy in range(int(y - r - 1), int(y + r + 2)):
            for xx in range(int(x - r - 1), int(x + r + 2)):
                d = math.hypot(xx + .5 - x, yy + .5 - y)
                if r - 0.6 < d <= r + 0.4:
                    put_wrap(px, xx, yy, (180, 240, 220, 90))
        put_wrap(px, x - 1, y - 1, (255, 255, 255, 120))
    for _ in range(22):                                      # plancton: diatomeas y bichitos
        x, y = rnd.randrange(W), rnd.randrange(H)
        kind = rnd.randrange(3)
        c = rnd.choice(((160, 220, 90, 110), (120, 200, 170, 110), (170, 120, 200, 90)))
        if kind == 0:
            for t in range(4):
                put_wrap(px, x + t, y + (t // 2), c)
        elif kind == 1:
            for dx, dy in ((0, 0), (1, 0), (0, 1), (1, 1)):
                put_wrap(px, x + dx, y + dy, c)
            put_wrap(px, x + 2, y + 2, c[:3] + (60,))
        else:
            for dx, dy in ((0, 0), (1, 1), (2, 0), (1, -1)):
                put_wrap(px, x + dx, y + dy, c)
    return im


NEAR = [  # (cx, cy, r, forma) ; el centro de la pantalla queda libre
    (20, 250, 62, 'cell'), (175, 8, 30, 'rod'), (330, 262, 46, 'amoeba'), (455, 30, 34, 'cell')]


def bg_near():
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = im.load()
    for (cx, cy, r, kind) in NEAR:
        for y in range(max(0, int(cy - r * 1.6)), min(H, int(cy + r * 1.6))):
            for x in range(int(cx - r * 2.2), int(cx + r * 2.2)):
                dx = x + .5 - cx
                dy = y + .5 - cy
                if kind == 'rod':                          # bacteria en bastón, tumbada
                    qx = max(abs(dx) - r * 1.3, 0)
                    d = math.hypot(qx, dy) / (r * 0.55)
                elif kind == 'amoeba':
                    a = math.atan2(dy, dx)
                    rr = r * (1 + 0.22 * math.sin(3 * a + 1) + 0.12 * math.sin(5 * a))
                    d = math.hypot(dx, dy) / rr
                else:
                    d = math.hypot(dx, dy) / r
                if d >= 1:
                    continue
                # desenfoque: el borde se desvanece; núcleo algo más oscuro
                v = min(1.0, (1 - d) / 0.35)
                lvl = 3 if v > 0.75 else 2 if v > 0.45 else 1 if v > 0.18 else 0
                if kind != 'rod' and math.hypot(dx + r * 0.25, dy + r * 0.1) < r * 0.28:
                    lvl = min(4, lvl + 1)
                if lvl and (lvl > 1 or bayer4(x, y) < 0.5):
                    a = (0, 40, 70, 95, 120)[lvl]
                    q = (x % W, y)
                    old = px[q][3]
                    px[q] = (8, 22, 20, max(old, a))
    return im


# ------------------------------------------------------------------- logo
GLYPHS = {
    'P': ['11110', '10001', '10001', '11110', '10000', '10000', '10000'],
    'R': ['11110', '10001', '10001', '11110', '10100', '10010', '10001'],
    'I': ['111', '010', '010', '010', '010', '010', '111'],
    'M': ['10001', '11011', '10101', '10101', '10001', '10001', '10001'],
    'O': ['01110', '10001', '10001', '10001', '10001', '10001', '01110'],
    'D': ['11110', '10001', '10001', '10001', '10001', '10001', '11110'],
    'A': ['01110', '10001', '10001', '11111', '10001', '10001', '10001'],
}
LOGO_W, LOGO_H = 200, 48
STEP = 3.7          # separación entre células de cada letra
CR = 2.35           # radio de cada célula


def logo_cells():
    word = 'PRIMORDIA'
    cols = sum(len(GLYPHS[c][0]) for c in word) + len(word) - 1
    x0 = (LOGO_W - cols * STEP) / 2
    y0 = (LOGO_H - 7 * STEP) / 2 - 1
    cells = []
    cx = x0
    for ch in word:
        g = GLYPHS[ch]
        for r, row in enumerate(g):
            for c, bit in enumerate(row):
                if bit == '1':
                    cells.append((cx + (c + .5) * STEP, y0 + (r + .5) * STEP, r))
        cx += (len(g[0]) + 1) * STEP
    return cells


def menu_logo():
    cv = Cv(LOGO_W, LOGO_H)
    cells = logo_cells()
    # sombra inferior (contorno más grueso abajo) para leerse sobre el fondo
    for (x, y, r) in cells:
        for yy in range(int(y - CR), int(y + CR + 3)):
            for xx in range(int(x - CR - 1), int(x + CR + 2)):
                if math.hypot(xx + .5 - x, yy + .5 - (y + 1.6)) <= CR + 0.4:
                    cv.put(xx, yy, OUTL)
    # células: lima arriba, verde agua abajo
    for (x, y, r) in cells:
        ramp = LIME if r < 3 else (AQUA if r > 4 else ['#2f6a3a', '#4fa060', '#8ad07a', '#d6ff9a'])
        for yy in range(int(y - CR - 1), int(y + CR + 2)):
            for xx in range(int(x - CR - 1), int(x + CR + 2)):
                nx, ny = (xx + .5 - x) / (CR + 0.6), (yy + .5 - y) / (CR + 0.6)
                if nx * nx + ny * ny <= 1:
                    cv.put(xx, yy, ramp[ramp_idx(shade_v(nx, ny) + 0.2)])
    for (x, y, r) in cells:                                 # núcleo y brillo de cada célula
        cv.put_in(x - 1, y - 1, '#ffffff' if r < 3 else AQUA[3])
    cv.outline()
    # unas burbujas alrededor
    for (x, y, r) in ((8, 12, 1.5), (13, 6, 1), (190, 36, 2), (184, 41, 1), (104, 4, 1)):
        bubble_px(cv, x, y, r, 200)
    return cv


def logo_shine(f):
    base = menu_logo()
    cv = Cv(LOGO_W, LOGO_H)
    pos = -30 + f * (LOGO_W + 60) / 7
    for (x, y), c in base.px.items():
        if c[:3] == OUTL[:3] or c[3] < 200:
            continue
        d = (x + 0.45 * y) - pos
        if abs(d) < 5:
            cv.put(x, y, (255, 255, 255, 170 if abs(d) < 2 else 80))
    return cv


def menu_selector(f):
    cv = Cv(9)
    s = [0, 0.3, 0.5, 0.3][f]
    ell(cv, 5.2, 4.5, 2.6 + s, 2.3 + s * 0.6, M.GREEN['ramp'])
    cv.put(4.6, 3.4, '#ffffff')
    cv.put(5.7, 4.4, M.GREEN['nucd'])
    # flagelo detrás que ondula
    wig = [0, 1, 0, -1][f]
    for x, dy in ((2, 0), (1, wig), (0, -wig)):
        cv.put(x, 4 + dy, M.GREEN['ramp'][2])
    cv.outline()
    return cv


def menu_bubble(f):
    cv = Cv(8)
    if f < 3:
        r = [1.2, 1.7, 2.3][f]
        y = [5.5, 4.5, 3.6][f]
        bubble_px(cv, 4, y, r, 220)
        cv.outline(alpha=120)
    elif f == 3:
        for dx, dy in ((-2, 0), (2, 0), (0, -2), (0, 2), (-1.5, -1.5), (1.5, -1.5)):
            cv.put(4 + dx, 3.5 + dy, (200, 255, 236, 200))
    else:
        for dx, dy in ((-3, -1), (3, -1), (0, -3)):
            cv.put(4 + dx, 3.5 + dy, (200, 255, 236, 110))
    return cv


# ================================================================ GUARDAR
SHEETS = {}


def as_img(x):
    return x if isinstance(x, Image.Image) else x.img()


def save(group, name, frames, ms=None):
    frames = [as_img(f) for f in frames]
    w, h = frames[0].size
    os.makedirs(os.path.join(OUT, group), exist_ok=True)
    sh = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, im in enumerate(frames):
        sh.paste(im, (i * w, 0))
    sh.save(os.path.join(OUT, group, name + '.png'))
    if len(frames) > 1:
        os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
        sc = max(1, min(8, 240 // max(w, h)))
        out = []
        for im in frames:
            b = Image.new('RGBA', (w, h), BG)
            b.alpha_composite(im)
            out.append(b.resize((w * sc, h * sc), Image.NEAREST).convert('RGB'))
        out[0].save(os.path.join(OUT, 'gif', name + '.gif'), save_all=True, append_images=out[1:], duration=ms, loop=0)
    SHEETS[name] = (group, len(frames), w, h, ms, frames)


def build():
    save('Pause', 'UI_Pause_Lens', [pause_lens()])
    save('Pause', 'UI_Pause_Lens_Glint', [pause_glint()])
    save('Pause', 'UI_Pause_LensBubbles', [lens_bubbles(f) for f in range(8)], 160)
    save('Pause', 'UI_Callout_Line', [callout_line()])
    save('Pause', 'UI_Callout_Dot', [callout_dot(f) for f in range(4)], 180)
    save('Pause', 'UI_Slot_Frame_Highlighted', [slot_frame_hi()])
    save('Pause', 'UI_Tooltip', [tooltip()])
    save('Pause', 'UI_Tooltip_Pointer', [tooltip_pointer()])
    save('Pause', 'UI_Stats_Panel', [stats_panel()])
    save('Pause', 'UI_Stats_Row', [stats_row()])
    save('Pause', 'UI_Stat_Health', [stat_health()])
    save('Pause', 'UI_Stat_Damage', [stat_damage()])
    save('Pause', 'UI_Stat_Speed', [stat_speed()])
    save('Pause', 'UI_Stat_Up', [stat_arrow(True)])
    save('Pause', 'UI_Stat_Down', [stat_arrow(False)])
    save('Pause', 'UI_Pause_Title', [pause_title()])
    save('Main', 'Menu_Bg_Far', [bg_far()])
    save('Main', 'Menu_Bg_Rays', [bg_rays(f) for f in range(8)], 180)
    save('Main', 'Menu_Bg_Particles', [bg_particles()])
    save('Main', 'Menu_Bg_Near', [bg_near()])
    save('Main', 'Menu_Logo', [menu_logo()])
    save('Main', 'Menu_Logo_Shine', [logo_shine(f) for f in range(8)], 50)
    save('Main', 'Menu_Selector', [menu_selector(f) for f in range(4)], 150)
    save('Main', 'Menu_Bubble', [menu_bubble(f) for f in range(5)], 90)


# ============================================================ VISTA PREVIA
FONT = {
    'A': '010101111101101', 'B': '110101110101110', 'C': '011100100100011', 'D': '110101101101110',
    'E': '111100110100111', 'F': '111100110100100', 'G': '011100101101011', 'H': '101101111101101',
    'I': '111010010010111', 'J': '001001001101010', 'K': '101101110101101', 'L': '100100100100111',
    'M': '101111111101101', 'N': '110101101101101', 'O': '010101101101010', 'P': '110101110100100',
    'Q': '010101101110011', 'R': '110101110101101', 'S': '011100010001110', 'T': '111010010010010',
    'U': '101101101101111', 'V': '101101101101010', 'W': '101101111111101', 'X': '101101010101101',
    'Y': '101101010010010', 'Z': '111001010100111', '0': '111101101101111', '1': '010110010010111',
    '2': '110001010100111', '3': '110001010001110', '4': '101101111001001', '5': '111100110001110',
    '6': '011100111101111', '7': '111001010010010', '8': '111101111101111', '9': '111101111001110',
    '/': '001001010100100', ':': '000010000010000', '.': '000000000000010', '-': '000000111000000',
    ',': '000000000010100', '%': '101001010100101', '+': '000010111010000', ' ': '000000000000000',
    '(': '010100100100010', ')': '010001001001010', "'": '010010000000000',
}


def ptext(im, xy, s, fill=(230, 245, 230), scale=1, shadow=True):
    """Fuente pixel de 3x5 para la maqueta (el juego usa la suya)."""
    x0, y0 = xy
    px = im.load()
    for k, ch in enumerate(s.upper()):
        g = FONT.get(ch, FONT[' '])
        for i, bit in enumerate(g):
            if bit == '1':
                gx, gy = x0 + (k * 4 + i % 3) * scale, y0 + (i // 3) * scale
                for sy in range(scale):
                    for sx in range(scale):
                        if shadow and 0 <= gx + sx < im.width and 0 <= gy + sy + scale < im.height:
                            if px[gx + sx, gy + sy + scale][:3] != fill[:3]:
                                px[gx + sx, gy + sy + scale] = OUTL
                        if 0 <= gx + sx < im.width and 0 <= gy + sy < im.height:
                            px[gx + sx, gy + sy] = fill[:3] + (255,)


def tw(s, scale=1):
    return (len(s) * 4 - 1) * scale


def first(name):
    return SHEETS[name][5][0]


def game_shot():
    g = U.floor_bg(W, H, walls=True)
    sk = M.carapace('down').img()
    g.alpha_composite(sk, (228, 140))
    bac = os.path.join(ROOT, 'enemies', 'Bacteria', 'Bacteria_Move.png')
    if os.path.exists(bac):
        b = Image.open(bac).convert('RGBA').crop((0, 0, 16, 16))
        for p in ((300, 120), (150, 190), (380, 200)):
            g.alpha_composite(b, p)
    g.alpha_composite(Image.new('RGBA', (W, H), (0, 0, 0, 191)))      # negro al 75 %
    return g


PARTS = None


def build_parts():
    global PARTS
    if PARTS is None:
        PARTS = (M.build_skins(), M.build_mouths(), M.build_limbs(), M.build_organs())
    return PARTS


def creature(mut):
    """Cuadro Idle_down de 64x64 (centro 32, 32), con o sin mutaciones."""
    sk, mo, li, org = build_parts()
    kw = dict(mouth='Mandibles', limbs_='Fins', skin='Carapace') if mut else {}
    return M.compose('down', 0, sk, mo, li, org, anim='Idle', **kw)


def fit_scale(im):
    """Mayor escala entera (máx. x3) a la que la criatura cabe en el interior de la lente,
    centrada en su caja. Es la regla que proponemos para el juego."""
    x0, y0, x1, y1 = im.getbbox()
    mx, my = (x0 + x1) / 2, (y0 + y1) / 2
    a = im.getchannel('A').load()
    for s in (3, 2, 1):
        if all(math.hypot((x + .5 - mx) * s, (y + .5 - my) * s) <= R_IN - 1
               for y in range(y0, y1) for x in range(x0, x1) if a[x, y]):
            return s, mx, my
    return 1, mx, my


def part_anchors(mut, im):
    """Puntos de cada pieza (en el lienzo de 64) donde nace su línea."""
    a = im.getchannel('A').load()
    base = creature(False).getchannel('A').load()
    def lefmost(y):
        return min(x for x in range(64) if a[x, y] > 128)
    def rightmost(y):
        return max(x for x in range(64) if a[x, y] > 128)
    def topmost(x):
        return min(y for y in range(64) if a[x, y] > 128)
    anc = {'Organ': (32, 34)}
    anc['Skin'] = (36, topmost(36) + 1)
    if mut:
        ys = [y for y in range(64) if any(a[x, y] > 128 and not base[x, y] for x in range(42, 64))]
        fy = max(ys)                                   # punta baja de la aleta derecha
        fx = round(sum(x for x in range(42, 64) if a[x, fy] > 128) /
                   max(1, sum(1 for x in range(42, 64) if a[x, fy] > 128)))
        anc['Limbs'] = (fx, fy - 1)
        anc['Mouth'] = (lefmost(50) + 1, 50)          # costado izquierdo de las mandíbulas
    else:
        anc['Limbs'] = (rightmost(37) - 1, 37)        # donde se engancharían las extremidades
        anc['Mouth'] = (lefmost(38) + 1, 38)          # abajo a la izquierda, donde va la boca
    return anc


# zona derecha de la pausa (unos 170x190): centro de la lente y recuadros en las esquinas
ZC = (385, 130)
BOXES = {'Mouth': (-85, -82), 'Skin': (65, -82), 'Limbs': (-85, 62), 'Organ': (65, 62)}


def lens_block(scr, mut, slots, tip):
    """Monta la lente, el retrato, las líneas en codo y los recuadros.
    Orden de capas: lente, burbujas, líneas, célula, brillo, puntos, recuadros, tooltip."""
    cx, cy = ZC
    lx, ly = cx - LC, cy - LC
    scr.alpha_composite(first('UI_Pause_Lens'), (lx, ly))
    scr.alpha_composite(SHEETS['UI_Pause_LensBubbles'][5][2], (lx, ly))
    im = creature(mut)
    s, mx, my = fit_scale(im)
    def to_scr(p):
        return (cx + int(math.floor((p[0] + .5 - mx) * s)), cy + int(math.floor((p[1] + .5 - my) * s)))
    anc = {k: to_scr(v) for k, v in part_anchors(mut, im).items()}
    box = {k: (cx + dx, cy + dy) for k, (dx, dy) in BOXES.items()}
    tgt = {k: ((x + 19, y + 10) if x < cx else (x, y + 10)) for k, (x, y) in box.items()}
    xl, xr = cx - 61, cx + 61                  # pasillos verticales entre la lente y los recuadros
    routes = {
        'Mouth': lambda a, b: [a, (xl, a[1]), (xl, b[1]), b],
        'Skin': lambda a, b: [a, (a[0], b[1]), b],
        'Organ': lambda a, b: [a, (xr, a[1]), (xr, b[1]), b],
        'Limbs': lambda a, b: [a, (a[0], b[1]), b],
    }
    line = first('UI_Callout_Line')
    for k in ('Mouth', 'Skin', 'Limbs', 'Organ'):
        pts = routes[k](anc[k], tgt[k])
        for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
            if x0 == x1:
                ya, yb = sorted((y0, y1))
                scr.alpha_composite(U.nine_render(line, 3, yb - ya + 3, 1), (x0 - 1, ya - 1))
            else:
                xa, xb = sorted((x0, x1))
                scr.alpha_composite(U.nine_render(line, xb - xa + 3, 3, 1), (xa - 1, y0 - 1))
    big = im.resize((64 * s, 64 * s), Image.NEAREST)
    ox, oy = to_scr((0, 0))
    ox, oy = ox - int(math.floor(.5 * s)), oy - int(math.floor(.5 * s))
    scr.alpha_composite(big.crop((max(0, -ox), max(0, -oy), big.width, big.height)), (max(0, ox), max(0, oy)))
    scr.alpha_composite(first('UI_Pause_Lens_Glint'), (lx, ly))
    for k in ('Mouth', 'Skin', 'Limbs', 'Organ'):
        scr.alpha_composite(SHEETS['UI_Callout_Dot'][5][1], (anc[k][0] - 2, anc[k][1] - 2))
    for k in ('Mouth', 'Skin', 'Limbs', 'Organ'):
        x, y = box[k]
        fr = first('UI_Slot_Frame_Highlighted') if (tip and k == 'Mouth') else U.slot_frame().img()
        scr.alpha_composite(fr, (x, y))
        m = slots.get(k)
        icon = U.ICONS[m](16).img() if m else U.slot_empty(k).img()
        scr.alpha_composite(icon, (x + 2, y + 2))
    if tip:
        # cuadro de información a la derecha del recuadro de la Boca (el piquito lo une)
        bx, by = box['Mouth']
        tx, ty, tw_, th = bx + 24, by - 4, 112, 52
        scr.alpha_composite(U.nine_render(first('UI_Tooltip'), tw_, th, 6), (tx, ty))
        scr.alpha_composite(first('UI_Tooltip_Pointer'), (tx - 4, by + 7))
        stripe = U.tint(U.nine_render(U.card_stripe().img(), tw_ - 12, 6, (4, 0, 4, 0)), LINEAGES['Predator']['color'])
        scr.alpha_composite(stripe, (tx + 6, ty + 5))
        ptext(scr, (tx + 8, ty + 15), 'MANDIBULAS', (255, 250, 235))
        scr.alpha_composite(U.lin_predator().img(), (tx + 7, ty + 22))
        ptext(scr, (tx + 22, ty + 26), 'BOCA - DEPREDADOR', (200, 225, 210))
        ptext(scr, (tx + 8, ty + 36), 'MORDISCO CORTO (3 DANO).', (190, 210, 200))
        ptext(scr, (tx + 8, ty + 43), 'CADA 3 MUERTES CURA 1/2.', (190, 210, 200))
    return s


def pause_screen(mut):
    scr = game_shot()
    # izquierda: estadísticas
    px_, py_, pw, ph = 14, 64, 112, 100
    scr.alpha_composite(U.nine_render(first('UI_Stats_Panel'), pw, ph, 10), (px_, py_))
    ptext(scr, (px_ + 12, py_ + 10), 'ESTADISTICAS', (214, 255, 154))
    rows = [('UI_Stat_Health', 'VIDA', '6/8' if mut else '6/6', None),
            ('UI_Stat_Damage', 'DANO', '2' if mut else '1', None),
            ('UI_Stat_Speed', 'VEL.', '7' if mut else '6', 'UI_Stat_Up' if mut else None)]
    for i, (icon, name, val, arrow) in enumerate(rows):
        y = py_ + 22 + i * 15
        if i % 2 == 0:
            scr.alpha_composite(U.nine_render(first('UI_Stats_Row'), pw - 16, 14, (4, 0, 4, 0)), (px_ + 8, y))
        scr.alpha_composite(first(icon), (px_ + 11, y + 1))
        ptext(scr, (px_ + 27, y + 5), name)
        vx = px_ + pw - 22 - tw(val)
        ptext(scr, (vx, y + 5), val, (255, 255, 240))
        if arrow:
            scr.alpha_composite(first(arrow), (px_ + pw - 19, y + 4))
    ptext(scr, (14, 252), 'SEMILLA: 123456', (150, 180, 170))
    # centro: título y botones
    t = U.nine_render(first('UI_Pause_Title'), 84, 16, (8, 0, 8, 0))
    scr.alpha_composite(t, (240 - 42 - 50, 18))
    ptext(scr, (240 - 50 - tw('PAUSA', 2) // 2, 21), 'PAUSA', (240, 255, 235), scale=2)
    for i, label in enumerate(('CONTINUAR', 'REINICIAR', 'MENU PRINCIPAL')):
        st = 'Highlighted' if (i == 0 and not mut) else 'Normal'
        b = U.nine_render(U.button(st).img(), 84, 16, 7)
        x, y = 148, 92 + i * 22
        scr.alpha_composite(b, (x, y))
        ptext(scr, (x + 42 - tw(label) // 2, y + 6), label)
    # derecha: lente y piezas
    slots = {'Mouth': 'Mandibles', 'Skin': 'Carapace', 'Limbs': 'Fins'} if mut else {}
    lens_block(scr, mut, slots, tip=mut)
    return scr


def main_menu(t=0.0, frame=0):
    scr = Image.new('RGBA', (W, H))
    scr.alpha_composite(first('Menu_Bg_Far'))
    scr.alpha_composite(SHEETS['Menu_Bg_Rays'][5][frame % 8])
    par = first('Menu_Bg_Particles')
    oy = int(round(t * SPEEDS['Menu_Bg_Particles'])) % H
    scr.alpha_composite(par.crop((0, oy, W, H)), (0, 0))
    if oy:
        scr.alpha_composite(par.crop((0, 0, W, oy)), (0, H - oy))
    near = first('Menu_Bg_Near')
    ox = int(round(t * SPEEDS['Menu_Bg_Near'])) % W
    scr.alpha_composite(near.crop((ox, 0, W, H)), (0, 0))
    if ox:
        scr.alpha_composite(near.crop((0, 0, ox, H)), (W - ox, 0))
    logo = first('Menu_Logo')
    scr.alpha_composite(logo, (W // 2 - LOGO_W // 2, 14))
    if frame % 16 < 8:
        scr.alpha_composite(SHEETS['Menu_Logo_Shine'][5][frame % 8], (W // 2 - LOGO_W // 2, 14))
    cell = M.PANIMS['Idle_down'][frame % 4].img()
    big = cell.resize((48, 48), Image.NEAREST)
    scr.alpha_composite(big, (W // 2 - 24, 78))
    for i, label in enumerate(('JUGAR', 'SALIR')):
        st = 'Highlighted' if i == 0 else 'Normal'
        b = U.nine_render(U.button(st).img(), 72, 16, 7)
        x, y = W // 2 - 36, 150 + i * 22
        scr.alpha_composite(b, (x, y))
        ptext(scr, (x + 36 - tw(label) // 2, y + 6), label)
    scr.alpha_composite(SHEETS['Menu_Selector'][5][frame % 4], (W // 2 - 36 - 12, 150 + 4))
    ptext(scr, (W - 4 - tw('V0.3'), H - 9), 'V0.3', (120, 160, 150))
    return scr


SPEEDS = {'Menu_Bg_Far': 0, 'Menu_Bg_Rays': 0, 'Menu_Bg_Particles': 6, 'Menu_Bg_Near': 10}


def preview(path):
    S = 3
    shots = [('1. PAUSA', pause_screen(True)), ('2. PAUSA SIN MUTACIONES', pause_screen(False)),
             ('3. MENU PRINCIPAL', main_menu())]
    lab = 24
    out = Image.new('RGBA', (W * S + 20, len(shots) * (H * S + lab + 10) + 10), BG)
    y = 10
    for name, im in shots:
        ptext(out, (12, y + 6), name, (214, 255, 154), scale=2)
        y += lab
        out.alpha_composite(im.resize((W * S, H * S), Image.NEAREST), (10, y))
        y += H * S + 10
    out.convert('RGB').save(path)
    # GIF del menú principal con las capas desplazándose
    frames = []
    for k in range(48):
        frames.append(main_menu(t=k * 0.1, frame=k).resize((W * 2, H * 2), Image.NEAREST).convert('RGB'))
    frames[0].save(os.path.join(OUT, 'gif', 'Menu_Principal_capas.gif'), save_all=True, append_images=frames[1:],
                   duration=100, loop=0)
    # ampliación x4 de los iconos de estadística
    ic = Image.new('RGBA', (4 * (5 * 16), 4 * 16), BG)
    for i, n in enumerate(('UI_Stat_Health', 'UI_Stat_Damage', 'UI_Stat_Speed', 'UI_Stat_Up', 'UI_Stat_Down')):
        im = first(n)
        ic.alpha_composite(im.resize((im.width * 4, im.height * 4), Image.NEAREST), (i * 64 + 4, 8))
    ic.convert('RGB').save(os.path.join(OUT, '_iconos_estadistica_x4.png'))


def main():
    build()
    preview(os.path.join(OUT, 'vista_previa_menus.png'))
    for k, v in SHEETS.items():
        print(k, *v[:5])


if __name__ == '__main__':
    main()
