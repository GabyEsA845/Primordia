"""Barra de vida del Anomalocaris (interfaz, piso 2).

La barra es el propio animal visto desde arriba: la cabeza en punta a la izquierda, con las fauces de garras y los ojos con ceño, las placas del
caparazón con sus aletas a lo largo y el abanico de la cola a la derecha. La vida rellena la ventana del cuerpo (200×8)
desde la cabeza. Piezas que el juego superpone (todas a escala de interfaz: un píxel de interfaz = un píxel del juego):
- Frame / FrameDark: el animal (normal y en silueta para la escena de los ojos y la silueta del frenesí), 258×32, con la
  ventana vacía en (38, 12)-(237, 19) (igual desde arriba que desde abajo).
- Back: fondo de la ventana. Fill_<Fase>: relleno por fase; Fill_Trail: el trazo claro de la vida perdida (barra
  fantasma). Todos 200×8, se recortan de izquierda a derecha.
- Eyes_<Fase>: los ojos por fase, 258×32 sobre el marco.
- Notch / Notch_Broken: placa de hueso que marca un cambio de fase (8×32, centrada en su umbral) y su versión rota;
  Fx_NotchBreak: se rompe (5 cuadros de 16×34).

Hecho con la skill primordia-sprites (pixel.py): luz arriba a la izquierda, contorno OUTL; paleta del Anomalocaris.

Uso: python generar_barra_anomalocaris.py [carpeta_salida]   (por defecto, la carpeta de este script)
"""
import math, os, sys

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

# --- paleta (la del Anomalocaris, generar_anomalocaris.py) ---------------------------------------
BODY = ['#3a0e1a', '#7a2030', '#b8443e', '#ec7a5a']
FLAP = ['#4a1222', '#94323e', '#d0624e', '#ffa47a']
APP = ['#3e1216', '#8a2c26', '#c8603a', '#ffa860']
EYEB = ['#140c1a', '#32243a', '#5c4a66', '#9a88a8']
DARKB = ['#0c0812', '#181222', '#241c30', '#30283e']        # silueta
BONE = ['#7a5a4a', '#c8a888', '#ecd8c0', '#fff0d8']         # placa de la muesca
WINDOW_BACK = ['#0e0810', '#1a1018', '#24161e']

FILLS = {
    'Stalk': ['#5a1424', '#b02c38', '#ec5048', '#ff9a78'],    # acecho: rojo vivo (más claro que el caparazón)
    'Encircle': ['#4a1a14', '#a0381e', '#e0662e', '#ffa860'], # cerco: rojo anaranjado
    'Frenzy': ['#2a0612', '#6a0c22', '#b0142e', '#ff5060'],   # frenesí: carmesí
    'Immune': ['#140c1a', '#2a2236', '#433a52', '#5c4a66'],   # inmune: gris violáceo
    'Trail': ['#b8a8a4', '#dcd0cc', '#f4ece6', '#fffaf4'],    # barra fantasma
}

W, H = 258, 32
WX0, WX1, WY0, WY1 = 38, 238, 12, 20     # ventana (x1, y1 exclusivos)
CY = 16.0                                # eje del cuerpo
PLATE = 10                               # largo de cada placa del caparazón
EYE_TOP, EYE_BOTTOM = (30, 2), (30, 24)  # esquina de arriba a la izquierda de cada ojo (6×6)
TOOTH = ['#5a4636', '#a88c72', '#e4d0b4', '#fff4e0']        # dientes de la boca y púas de los apéndices

# --- cabeza --------------------------------------------------------------------------------------
# Mira a la izquierda: escudo de la cabeza en punta con un pincho hacia atrás en cada esquina y cejas en V; delante,
# los dos apéndices de garra, segmentados, se curvan hacia delante y hacia dentro como un cepo, con púas de hueso
# hacia dentro que casi se cruzan.
def _mirror(y): return 2 * CY - y

def _inside_poly(pts):
    def inside(x, y):
        c = False
        for (x0, y0), (x1, y1) in zip(pts, pts[1:] + pts[:1]):
            if (y0 > y) != (y1 > y) and x < x0 + (y - y0) * (x1 - x0) / (y1 - y0): c = not c
        return c
    return inside

HEAD_TOP = [(49, 10.0), (46.5, 2.2), (39, 5.4), (36, 5.0), (28, 6.6), (21, 9.4), (16, 12.8), (13, 16)]
CLAW_TOP = [(21.0, 11.0), (17.0, 7.2), (12.5, 5.0), (8.0, 4.4), (4.6, 5.5), (2.6, 8.0), (2.2, 11.2), (3.4, 13.8)]

def _claw_band():
    """Píxeles del apéndice de arriba: banda que sigue CLAW_TOP y se estrecha hacia la punta.
    Devuelve {(x, y): índice de la rampa} con la cara de fuera clara, la de dentro oscura y una junta cada 3 px."""
    segs, L = [], 0.0
    for (x0, y0), (x1, y1) in zip(CLAW_TOP, CLAW_TOP[1:]):
        l = math.hypot(x1 - x0, y1 - y0); segs.append((x0, y0, x1, y1, l, L)); L += l
    out = {}
    for y in range(H // 2):
        for x in range(24):
            px, py = x + 0.5, y + 0.5
            best = None
            for x0, y0, x1, y1, l, s0 in segs:
                dx, dy = (x1 - x0) / l, (y1 - y0) / l
                t = max(0.0, min(l, (px - x0) * dx + (py - y0) * dy))
                qx, qy = x0 + dx * t, y0 + dy * t
                d = math.hypot(px - qx, py - qy)
                if best is None or d < best[0]:
                    best = (d, s0 + t, dx * (py - qy) - dy * (px - qx))
            d, sl, v = best
            r = 2.1 - 1.3 * sl / L
            if d > r or sl > L - 0.2 and d > 0.6: continue
            i = 3 if v < -r * 0.35 else 2 if v < r * 0.3 else 1     # v < 0: cara de fuera (arriba a la izquierda)
            if sl % 3.2 < 0.8 and sl > 2: i -= 1                     # junta entre segmentos
            out[(x, y)] = max(0, i)
    return out, segs, L

def claw_teeth():
    """Púas del apéndice de arriba: [(x, y, es_punta)], hacia dentro de la curva y en una de 8 direcciones."""
    _, segs, L = _claw_band()
    teeth = []
    sl = 4.8
    while sl < L - 1.5:
        for x0, y0, x1, y1, l, s0 in segs:
            if s0 <= sl <= s0 + l:
                dx, dy = (x1 - x0) / l, (y1 - y0) / l
                t = sl - s0
                qx, qy = x0 + dx * t, y0 + dy * t
                break
        nx, ny = dy, -dx                                  # hacia dentro de la curva
        ux, uy = nx, ny
        nx, ny = round(nx * 1.4), round(ny * 1.4)
        nx, ny = max(-1, min(1, nx)), max(-1, min(1, ny))
        r = 2.1 - 1.3 * sl / L
        n = 3 if 7 < sl < L - 5 else 2
        bx, by = math.floor(qx + ux * (r + 0.4)), math.floor(qy + uy * (r + 0.4))
        for k in range(n): teeth.append((bx + nx * k, by + ny * k, k == n - 1))
        sl += 3.2
    return teeth

def _sym(side):
    return (lambda y: y) if side == 0 else _mirror

def head(cv, body, app, stalk, dark):
    # apéndices de garra (detrás del escudo): cuentas alternas, la última casi en punta
    for side in (0, 1):
        f = _sym(side)
        for (x, y), i in _claw_band()[0].items():
            cv.put(x, 2 * int(CY) - 1 - y if side else y, C(app[max(0, i - side)]))
    # escudo de la cabeza
    pts = HEAD_TOP + [(x, _mirror(y)) for x, y in reversed(HEAD_TOP[:-1])]
    fill(cv, _inside_poly(pts), 30, CY - 3, 17, 12, body, box=(10, 1, 50, 31))
    # ojos: abultan por encima y por debajo del borde del escudo, sobre un pedúnculo corto
    for side in (0, 1):
        ellipse(cv, EYE_TOP[0] + 3, _sym(side)(EYE_TOP[1] + 3), 3.3, 3.3, body if not dark else stalk, shift=-side)

def head_details(cv, body, dark):
    """Lo que va encima del contorno: púas de las garras, cejas en V y cresta."""
    tooth = DARKB if dark else TOOTH
    for side in (0, 1):
        f = _sym(side)
        # púas de hueso hacia dentro; sin contorno (son finas)
        for x, y, tip in claw_teeth():
            if (x, y) in _claw_band()[0]: continue
            cv.put(x, 2 * int(CY) - 1 - y if side else y, C(tooth[3] if tip else tooth[2]))
    if dark: return
    for side in (0, 1):
        f = _sym(side)
        # ceja: arista oscura del pedúnculo hacia delante y hacia el centro
        for k, (x, y) in enumerate(((31, 9), (30, 9), (29, 10), (28, 10), (27, 11), (26, 11), (25, 12), (24, 12), (23, 13))):
            yy = 31 - y if side else y
            cv.put_in(x, yy, OUTL if k < 6 else C(body[0]))
            if side == 0: cv.put_in(x, y - 1, C(body[3] if k < 5 else body[2]))
    # cresta central, con nudos oscuros
    for x in range(16, 44):
        cv.put_in(x, 15, C(OUTL if x % 4 == 1 else body[0])); cv.put_in(x, 14, C(body[2]))
    for x, y in ((17, 13), (18, 13), (20, 12)): cv.put_in(x, y, C(body[3]))

# --- marco ---------------------------------------------------------------------------------------
def frame(dark=False):
    body = DARKB if dark else BODY
    flap = DARKB if dark else FLAP
    app = DARKB if dark else APP
    stalk = DARKB if dark else EYEB
    cv = Canvas(W, H)
    # aletas: una por placa, arriba y abajo, alternando un poco su tamaño
    for i, x in enumerate(range(WX0 + 2, WX1 - 4, PLATE)):
        r = 3.6 if i % 2 else 3.0
        ellipse(cv, x + PLATE / 2, WY0 - 3.2, PLATE / 2 - 0.2, r, flap)
        ellipse(cv, x + PLATE / 2, WY1 + 2.6, PLATE / 2 - 0.2, r, flap, shift=-1)
    # bandas del caparazón por encima y por debajo de la ventana
    for x in range(WX0 - 1, WX1 + 1):
        for y in range(WY0 - 3, WY0):
            cv.put(x, y, C(body[2] if y == WY0 - 3 else body[1]))
        for y in range(WY1, WY1 + 3):
            cv.put(x, y, C(body[1] if y == WY1 else body[0]))
        # juntas entre placas
        if (x - WX0) % PLATE == PLATE - 1:
            for y in list(range(WY0 - 3, WY0)) + list(range(WY1, WY1 + 3)):
                cv.put(x, y, C(body[0]))
    for y in range(WY0, WY1):
        cv.put(WX1, y, C(body[1])); cv.put(WX1 + 1, y, C(body[0]))
    # cola: abanico de tres lóbulos hacia la derecha
    for ang, rx in ((-0.55, 7.5), (0.0, 8.5), (0.55, 7.5)):
        rot_ellipse(cv, WX1 + 6 + math.cos(ang) * 4, CY + math.sin(ang) * 6, rx - 0.5, 3.2, ang, flap)
    ellipse(cv, WX1 + 3, CY, 4.5, 6, body)
    head(cv, body, app, stalk, dark)
    cv.outline(); cv.clean()
    # la ventana queda vacía (el juego pone el fondo y el relleno por detrás)
    for x in range(WX0, WX1):
        for y in range(WY0, WY1):
            cv.px.pop((x, y), None)
    # filo oscuro alrededor de la ventana: separa el relleno del caparazón
    for x in range(WX0 - 1, WX1 + 1):
        cv.put(x, WY0 - 1, OUTL); cv.put(x, WY1, OUTL)
    for y in range(WY0, WY1):
        cv.put(WX0 - 1, y, OUTL); cv.put(WX1, y, OUTL)
    head_details(cv, body, dark)
    return cv

# --- ojos ----------------------------------------------------------------------------------------
# Cada ojo es de 6×6 (sin esquinas) en lo alto de su pedúnculo; el de abajo es el de arriba en espejo. Pupila en
# pequeña mirando hacia delante y párpado en cuña que baja hacia delante: miran con mala cara en todas las fases.
N = 6
SHAPE = [(dx, dy) for dx in range(N) for dy in range(N) if (dx, dy) not in ((0, 0), (N - 1, 0), (0, N - 1), (N - 1, N - 1))]
LID = {'Stalk': {(dx, dy) for dx in range(N) for dy in range(3 if dx < 3 else 2)},   # medio cerrados
       'Encircle': {(1, 0), (2, 0), (3, 0), (0, 1), (1, 1), (0, 2)},                # abiertos, ceño
       'Frenzy': {(1, 0), (2, 0), (3, 0), (4, 0), (0, 1), (1, 1), (2, 1), (0, 2)}}   # más ceño

def eyes(state, frame_index=0):
    cv = Canvas(W, H)
    for bottom, (ex, ey) in enumerate((EYE_TOP, EYE_BOTTOM)):
        put = lambda dx, dy, c: cv.put(ex + dx, ey + (N - 1 - dy if bottom else dy), c)
        if state == 'Dark':
            # en la oscuridad solo se ve el ojo, amarillento, con la rendija; el cuadro 1 es un parpadeo
            lid = LID['Encircle']
            for dx, dy in SHAPE:
                if (dx, dy) in lid: continue
                if frame_index == 1:
                    if dy == 3: put(dx, dy, C('#c8e070'))
                    continue
                put(dx, dy, C('#e8ff9a' if 1 <= dx <= 4 and 1 <= dy <= 4 else '#a8c860'))
            if frame_index == 0:
                for dy in (2, 3, 4): put(3, dy, C('#2a3010'))
                put(2, 2, C('#ffffe0'))
            continue
        red = state == 'Frenzy'
        for dx, dy in SHAPE: put(dx, dy, C('#ff6a5a') if red else C(EYE))
        if red:
            for dx, dy in ((3, 3), (4, 3), (3, 4), (4, 2)): put(dx, dy, C('#ffc0a8'))
        lid = LID[state]
        for dx, dy in lid: put(dx, dy, C(BODY[3 - bottom] if dy == 0 else BODY[2 - bottom]))
        # filo del párpado
        for dx in range(N):
            edge = max([dy for (x, dy) in lid if x == dx], default=-1)
            if 0 <= edge and (dx, edge + 1) in SHAPE: put(dx, edge + 1, OUTL)
        # pupila de 2×2 baja y hacia delante, como si mirara al jugador; en el frenesí, un punto que arde
        pupil = ((2, 4), (2, 3), (1, 4), (1, 3)) if state == 'Stalk' else ((1, 3), (2, 3), (1, 4), (2, 4))
        for dx, dy in pupil:
            if (dx, dy) in SHAPE and (dx, dy) not in lid: put(dx, dy, C('#5a0614') if red else OUTL)
        if red and frame_index == 1:
            for dx, dy in ((-1, 2), (-1, 3), (6, 2), (6, 3), (2, -1), (3, -1), (2, 6), (3, 6)):
                put(dx, dy, C('#ff4a5a', 150))
            put(4, 3, C('#fff0e0'))
    return cv

# --- ventana -------------------------------------------------------------------------------------
def window_fill(ramp, bright=False):
    """Relleno de 200×8: placas con nervadura (luz arriba), juntas cada PLATE px."""
    w, h = WX1 - WX0, WY1 - WY0
    cv = Canvas(w, h)
    for x in range(w):
        seam = x % PLATE == PLATE - 1
        for y in range(h):
            i = 3 if y == 0 else 2 if y < 3 else 1 if y < 6 else 0
            if seam: i = max(0, i - 1)
            if bright and y in (1, 2) and x % PLATE in (2, 3, 4): i = 3
            cv.put(x, y, C(ramp[i]))
    return cv

def back():
    w, h = WX1 - WX0, WY1 - WY0
    cv = Canvas(w, h)
    for x in range(w):
        for y in range(h):
            cv.put(x, y, C(WINDOW_BACK[0 if y < 2 else 1 if y < 6 else 2]))
    return cv

# --- muesca --------------------------------------------------------------------------------------
def notch(broken=False):
    cv = Canvas(8, H)
    ramp = BONE if not broken else ['#4a3a34', '#7a6658', '#a8907c', '#c8b098']
    for y in range(2, H - 2):
        # placa en punta arriba y abajo, más ancha en el centro
        half = 1.2 + min(y - 2, H - 3 - y) * 0.35
        half = min(half, 2.6)
        for x in range(8):
            if abs(x + 0.5 - 4) <= half:
                if broken and 7 <= y <= 18: continue
                i = 3 if x < 3 and y < 11 else 2 if x < 4 else 1
                if abs(x + 0.5 - 4) > half - 1: i = max(0, i - 1)
                cv.put(x, y, C(ramp[i]))
    if broken:
        # bordes partidos en zigzag
        for x, y in ((3, 6), (4, 7), (5, 6), (3, 19), (4, 18), (5, 19)):
            cv.put(x, y, C(ramp[0]))
    cv.outline(); cv.clean()
    return cv

def notch_break(k):
    """Se agrieta, destella y suelta esquirlas (16×24)."""
    cv = Canvas(16, H + 2)
    base = notch(broken=k >= 2).img()
    img = cv.img(); img.alpha_composite(base, (4, 1))
    out = Canvas(16, H + 2)
    for y in range(H + 2):
        for x in range(16):
            p = img.getpixel((x, y))
            if p[3]: out.put(x, y, p)
    if k == 0:
        for y in range(9, 19): out.put_in(8 + (y % 2), y, C(BONE[0]))   # grieta
    if k == 1:
        sparks(out, 8, 13, 2)
    if k >= 2:
        d = (k - 1) * 1.7
        for i, (dx, dy) in enumerate(((-1, -1), (1, -0.6), (-0.8, 0.9), (1.1, 1))):
            x, y = 8 + dx * d, 14 + dy * d
            col = C(BONE[2] if k < 4 else BONE[1], 255 - 60 * (k - 2))
            out.put(x, y, col); out.put(x + 1, y, col)
    return out

# --- salida --------------------------------------------------------------------------------------
def mock(state, ratio, trail, broken):
    """Barra montada como en el juego, para la vista previa."""
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    img.alpha_composite(back().img(), (WX0, WY0))
    ww = WX1 - WX0
    tr = window_fill(FILLS['Trail']).img().crop((0, 0, int(ww * trail), WY1 - WY0))
    img.alpha_composite(tr, (WX0, WY0))
    fill_state = 'Immune' if state == 'Dark' else state
    f = window_fill(FILLS[fill_state]).img().crop((0, 0, int(ww * ratio), WY1 - WY0))
    img.alpha_composite(f, (WX0, WY0))
    img.alpha_composite(frame(dark=state == 'Dark').img(), (0, 0))
    for threshold, gone in ((0.25, broken[0]), (0.6, broken[1])):
        x = WX0 + int(ww * threshold) - 4
        img.alpha_composite(notch(broken=gone).img(), (x, 0))
    img.alpha_composite(eyes(state).img(), (0, 0))
    return img

if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    P = 'UI_BossBar_Anomalocaris_'
    sheets = {
        P + 'Frame': [frame()],
        P + 'FrameDark': [frame(dark=True)],
        P + 'Back': [back()],
        P + 'Fill_Stalk': [window_fill(FILLS['Stalk'])],
        P + 'Fill_Encircle': [window_fill(FILLS['Encircle'])],
        P + 'Fill_Frenzy': [window_fill(FILLS['Frenzy']), window_fill(FILLS['Frenzy'], bright=True)],
        P + 'Fill_Immune': [window_fill(FILLS['Immune'])],
        P + 'Fill_Trail': [window_fill(FILLS['Trail'])],
        P + 'Eyes_Stalk': [eyes('Stalk')],
        P + 'Eyes_Encircle': [eyes('Encircle')],
        P + 'Eyes_Frenzy': [eyes('Frenzy', i) for i in range(2)],
        P + 'Eyes_Dark': [eyes('Dark', i) for i in range(2)],
        P + 'Notch': [notch()],
        P + 'Notch_Broken': [notch(broken=True)],
        P + 'Fx_NotchBreak': [notch_break(k) for k in range(5)],
    }
    for name, frames in sheets.items():
        save_sheet(os.path.join(OUT, name + '.png'), frames)

    # Vista previa: la barra montada como en el juego en cada fase (acecho al 85 % perdiendo un 10 %, cerco al 45 %,
    # escena o silueta al 25 % y frenesí al 12 %), a ×5.
    states = [('Stalk', 0.85, 0.95, (False, False)), ('Encircle', 0.45, 0.52, (False, True)),
              ('Dark', 0.25, 0.25, (False, True)), ('Frenzy', 0.12, 0.18, (True, True))]
    sheet = Image.new('RGBA', (W + 8, (H + 6) * len(states) + 2), BG)
    for i, state in enumerate(states):
        sheet.alpha_composite(mock(*state), (4, 4 + i * (H + 6)))
    sheet.resize((sheet.width * 5, sheet.height * 5), Image.NEAREST).save(
        os.path.join(HERE, 'vista_previa_barra_anomalocaris.png'))
    os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True)
    gif(os.path.join(HERE, 'gif', 'Fx_NotchBreak.gif'), sheets[P + 'Fx_NotchBreak'], 70, BG, scale=8)
    print('Listo en', OUT)
