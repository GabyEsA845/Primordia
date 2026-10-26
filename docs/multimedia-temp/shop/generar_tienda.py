"""
generar_tienda.py - Primordia: la tienda del piso (Darwin, mostrador,
pedestales, artículos en venta, tecla E e icono del mapa).

Reutiliza los otros generadores:
  generar_mapa.py -> generar_menus.py -> generar_ui.py -> generar_mutaciones.py -> generar_jugador.py
  (Cv, contorno #181425, rampas de color, colores de linaje, la burbuja de
   mutación, los corazones del HUD, la biomasa, la membrana de los botones,
   el vidrio, la fuente de la maqueta y el plano del minimapa)
  generar_escenario.py (paleta del piso 1 y la roca de los obstáculos)
y los sprites ya generados de environment/ para la vista previa.

Uso:
  python generar_tienda.py [carpeta_multimedia_temp] [carpeta_salida]
Por defecto: la carpeta de arriba y la carpeta de este script.
"""
import importlib.util
import math
import os
import random
import sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, '..')
OUT = sys.argv[2] if len(sys.argv) > 2 else HERE

# --------------------------------------------- cargar los otros generadores
_argv = sys.argv
sys.argv = ['generar_mapa.py', ROOT, os.devnull]
_spec = importlib.util.spec_from_file_location('generar_mapa', os.path.join(ROOT, 'map', 'generar_mapa.py'))
MP = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(MP)
sys.argv = _argv
G = MP.G           # generar_menus.py
U = MP.U           # generar_ui.py
M = MP.M           # generar_mutaciones.py


def _defs(path, cut):
    """Ejecuta solo las definiciones de un generador antiguo (sin la parte que guarda)."""
    src = open(path, encoding='utf-8').read().replace('\r', '')
    ns = {'__name__': os.path.basename(path)}
    argv = sys.argv
    sys.argv = [path, os.devnull]
    exec(src.split(cut, 1)[0], ns)
    sys.argv = argv
    return ns


E = _defs(os.path.join(ROOT, 'environment', 'generar_escenario.py'), '\n# ---------------- generar')
SOUP = E['THEMES']['Soup']

C, CA, OUTL = U.C, U.CA, U.OUTL
Cv, ell, col, thick, sheet = U.Cv, U.ell, U.col, U.thick, U.sheet
shade_v, ramp_idx = U.shade_v, U.ramp_idx
BIO, MEM, MEM_HI, HEART = U.BIO, U.MEM, U.MEM_HI, U.HEART
GLASS = G.GLASS
GOLD = MP.GOLD                                     # el dorado del nivel III y de la recompensa
BG = U.BG
ROCK = ['#2a3a3a', '#46605a', '#6a8a7a', '#9ab8a0']  # la roca de Obstacle_Rock

# ------------------------------------------------------------ paletas nuevas
SKIN = ['#8a4e3e', '#c8806a', '#eeb494', '#ffe2cc']
BEARD = ['#7e8498', '#b8bccc', '#e4e6ee', '#ffffff']
COAT = ['#15121f', '#262238', '#3c3656', '#5c547c']   # levita oscura
PANTS = ['#2a2836', '#423f52', '#5e5a70', '#807c92']
SHOE = ['#140e16', '#2a1e26', '#46323c', '#6a5058']
BOOK = ['#1e3a24', '#2f5a34', '#4a8248', '#7ab06a']   # libreta de tapas verdes
PAPER = ['#a89a7a', '#d8ccaa', '#f2ead0', '#fffaf0']
CHEEK = '#ec8c84'
WOOD = ['#2e1c16', '#4e3022', '#74482e', '#a06c40']   # madera de naufragio
WOOD_TOP = ['#5a3a26', '#7e5434', '#a6764a', '#cca070']
CORAL = ['#6a2440', '#a84060', '#e07484', '#ffbcc4']
SHELL = ['#7a6a5a', '#bfae94', '#e6dac0', '#fff8e8']
LIQ = ['#4a6a1a', '#7a9a2a', '#b4cc4a', '#e0f08a']   # líquido de los frascos (formol verdoso)
CORK = ['#5a3a1e', '#8a5e30', '#b88850', '#e0b878']
SPONGE = ['#2c3b3a', '#40564f', '#5a7468', '#7c968a']  # pedestal: apagado, como la roca
BUBBLE = '#b0f0e0'


def canvas_from(img):
    cv = Cv(img.width, img.height)
    px = img.load()
    for y in range(img.height):
        for x in range(img.width):
            if px[x, y][3]:
                cv.px[(x, y)] = px[x, y]
    return cv


def blit(dst, src, ox, oy):
    for (x, y), c in src.px.items():
        dst.put(x + ox, y + oy, c)


def rect(cv, x0, y0, x1, y1, c):
    for y in range(int(y0), int(y1) + 1):
        for x in range(int(x0), int(x1) + 1):
            cv.put(x, y, c)


def ring_bubble(cv, x, y, r, a=230):
    """Burbujita de agua: aro claro con brillo (como Deco_Bubbles)."""
    if r <= 0.6:
        cv.put(x, y, col(BUBBLE, a))
        return
    for yy in range(int(y - r - 1), int(y + r + 2)):
        for xx in range(int(x - r - 1), int(x + r + 2)):
            d = math.hypot(xx + .5 - x, yy + .5 - y)
            if r - 0.75 <= d <= r + 0.25:
                cv.put(xx, yy, col(BUBBLE, a))
    cv.put(x - r * 0.5 - .5, y - r * 0.5 - .5, col('#ffffff', a))


# ============================================================ 1. DARWIN
def darwin(dy=0, legs_dy=0, eyes='open', hand='book', mouth='calm', squash=0.0, bubbles=(), brow=0):
    """Charles Darwin chibi, de frente, en un lienzo de 24x32 con los pies en la fila 31.
    dy sube o baja el cuerpo (respirar, saltar); legs_dy mueve las piernas (salto).
    hand: 'book' (libreta en la mano), 'beard' (se acaricia la barba), 'up' (alza la libreta)."""
    cv = Cv(24, 32)
    b = dy                                  # desplazamiento del cuerpo
    sq = squash                             # >0 se aplasta (más ancho, más bajo)
    # --- piernas y zapatos
    ly = legs_dy
    for lx in (9.5, 14.5):
        rect(cv, lx - 1.5, 26 + ly, lx + 0.5, 29 + ly, PANTS[1])
        cv.put(lx - 1.5, 26 + ly, PANTS[2])
        cv.put(lx - 1.5, 27 + ly, PANTS[2])
    for lx, sx in ((9.5, -0.6), (14.5, 0.6)):
        ell(cv, lx - 0.5 + sx, 30.6 + ly, 2.3, 1.2, SHOE)
    # --- levita: cuerpo y faldones
    ell(cv, 12, 22.5 + b + sq, 6.2 + sq, 5.2 - sq * 0.6, COAT)
    ell(cv, 12, 26.0 + b * 0.5 + sq, 6.0 + sq, 2.6, COAT, light=-0.15)
    for y in range(int(24 + b), int(28 + b * 0.5 + sq) + 1):     # abertura de la levita (chaleco)
        cv.put(12, y, COAT[0])
    cv.put(13, int(25 + b), '#c8b070')                            # botón dorado
    cv.put(13, int(27 + b * 0.5), '#c8b070')
    # --- brazo izquierdo (de la pantalla), caído junto al cuerpo
    def sleeve(xx, yy, x, y, r):
        return COAT[2] if xx + .5 < x else COAT[1]
    thick(cv, [(6.6, 19.8 + b), (5.6, 23.6 + b)], [3.0], sleeve)
    ell(cv, 5.6, 25.0 + b, 1.4, 1.2, SKIN)
    # --- brazo derecho según la pose
    if hand == 'book':
        thick(cv, [(17.4, 19.8 + b), (18.4, 23.2 + b)], [3.0], sleeve)
        book(cv, 16.5, 22.5 + b)
        ell(cv, 18.6, 24.6 + b, 1.3, 1.1, SKIN)
    elif hand == 'beard':
        thick(cv, [(17.4, 20.0 + b), (17.0, 22.4 + b), (15.4, 21.0 + b)], [3.0, 2.6], sleeve)
    elif hand == 'up':
        thick(cv, [(17.4, 19.8 + b), (19.6, 15.0 + b), (20.0, 10.5 + b)], [3.0, 2.6], sleeve)
    # --- cabeza (grande: chibi)
    hx, hy = 12, 10.2 + b + sq * 0.6
    ell(cv, hx, hy, 7.0 + sq * 0.5, 6.6 - sq * 0.4, SKIN, light=0.1)
    # orejas
    for ex in (4.9, 19.1):
        ell(cv, ex, hy + 1.6, 1.1, 1.6, SKIN, light=-0.2)
    # pelo blanco a los lados (calvo en la coronilla)
    for ex, s in ((5.6, -1), (18.4, 1)):
        ell(cv, ex, hy + 0.6, 1.7, 2.6, BEARD, light=-0.05)
        cv.put(ex + s * 0.8, hy - 1.8, BEARD[2])
    # brillo de la calva
    cv.put(hx - 3, hy - 4.6, '#ffffff')
    cv.put(hx - 2, hy - 5.1, '#ffffff')
    cv.put(hx - 4, hy - 3.6, SKIN[3])
    # --- barba grande (mejillas, mentón y pecho)
    by0 = hy + 3.4
    for y in range(32):
        for x in range(24):
            px, py = x + .5, y + .5
            a = ((px - 12) / 6.4) ** 2 + ((py - (by0 + 2.6)) / 4.4) ** 2 <= 1
            bot = ((px - 12) / 4.6) ** 2 + ((py - (by0 + 5.8)) / 4.0) ** 2 <= 1
            if (a or bot) and py >= by0 - 1.2:
                nx, ny = (px - 11) / 6, (py - (by0 + 3.5)) / 6
                i = ramp_idx(shade_v(nx, ny) + 0.25)
                if (x * 3 + y) % 7 == 0 and i > 1:      # mechones
                    i -= 1
                cv.put(x, y, BEARD[i])
    # bigote que cae a los lados de la boca
    for (x, y) in ((9, 0), (10, 0), (11, -0.4), (13, -0.4), (14, 0), (15, 0)):
        cv.put(x, by0 + 0.6 + y, BEARD[3])
    # boca
    my = by0 + 1.8
    if mouth == 'calm':
        cv.put(11.5, my, BEARD[0]); cv.put(12.5, my, BEARD[0])
    elif mouth == 'smile':
        for (x, y) in ((10.5, 0), (11.5, 0.8), (12.5, 0.8), (13.5, 0)):
            cv.put(x, my + y - 0.4, '#5a2434')
    elif mouth == 'open':
        rect(cv, 11, my - 0.4, 13, my + 0.8, '#5a2434')
        cv.put(11.5, my + 0.8, '#e05a6a'); cv.put(12.5, my + 0.8, '#e05a6a')
    # --- cara
    ey = hy + 0.2
    for ex in (8.6, 15.4):
        if eyes == 'open':
            cv.put(ex, ey, OUTL); cv.put(ex, ey + 1, OUTL)
        elif eyes == 'closed':
            cv.put(ex - 0.6, ey + 1, OUTL); cv.put(ex + 0.6, ey + 1, OUTL)
        elif eyes == 'happy':
            cv.put(ex - 1, ey + 1, OUTL); cv.put(ex, ey, OUTL); cv.put(ex + 1, ey + 1, OUTL)
    # cejas pobladas, grises, que cuelgan hacia fuera
    for sx in (-1, 1):
        x0 = 12 + sx * 3.4
        pts = ((-1.5, 0.6), (-0.5, -0.4), (0.5, -0.4), (1.5, 0.2), (2.4, 1.2))
        for i, (dx, dd) in enumerate(pts):
            x = x0 + dx * sx
            yb = ey - 2.0 - brow + dd
            cv.put(x, yb, BEARD[3] if i < 3 else BEARD[2])
            cv.put(x, yb + 1, BEARD[1] if i < 4 else BEARD[0])
    # nariz y mejillas
    ell(cv, 12, hy + 2.6, 1.3, 1.1, SKIN, light=0.2)
    cv.put(11.5, hy + 2.0, SKIN[3])
    cv.put(7.6, hy + 2.6, CHEEK); cv.put(16.4, hy + 2.6, CHEEK)
    # mano en la barba / libreta en alto (encima de la barba)
    if hand == 'beard':
        ell(cv, 15.0, 20.4 + b, 1.4, 1.2, SKIN)
    if hand == 'up':
        book(cv, 18.0, 6.6 + b, open_=True)
        ell(cv, 20.0, 10.6 + b, 1.3, 1.2, SKIN)
    cv.outline()
    for (x, y, r) in bubbles:
        ring_bubble(cv, x, y, r)
    return cv


def book(cv, x, y, open_=False):
    """Libreta de campo de tapas verdes (4x5) o abierta en alto (5x4)."""
    if not open_:
        rect(cv, x, y, x + 3, y + 4, BOOK[1])
        rect(cv, x, y, x + 3, y, BOOK[3])
        rect(cv, x, y, x, y + 4, BOOK[2])
        rect(cv, x + 3, y + 1, x + 3, y + 4, PAPER[2])   # canto de las hojas
    else:
        rect(cv, x, y, x + 4, y + 3, PAPER[2])
        rect(cv, x, y, x + 1, y, PAPER[3])
        rect(cv, x + 2, y, x + 2, y + 3, PAPER[0])        # lomo
        cv.put(x + 0.4, y + 1.6, PAPER[0]); cv.put(x + 3.6, y + 1.6, PAPER[0])   # renglones
        rect(cv, x, y + 3, x + 4, y + 3, BOOK[1])


def darwin_idle():
    return [
        darwin(0),
        darwin(-0.6),                                                  # inspira
        darwin(-0.6, eyes='closed'),                                   # parpadea
        darwin(0, hand='beard', bubbles=((17.5, 15.5, 0.5),)),        # se acaricia la barba
        darwin(0.4, hand='beard', bubbles=((18.5, 11.5, 1.2),)),
        darwin(0, bubbles=((19.5, 6.5, 1.6),)),                        # la burbuja sube
    ]


def darwin_buy():
    return [
        darwin(1, squash=0.6, eyes='happy', mouth='smile', hand='beard'),           # se agacha
        darwin(-2, legs_dy=-2, eyes='happy', mouth='open', hand='up', brow=1),       # salta y alza la libreta
        darwin(-3, legs_dy=-3, eyes='happy', mouth='open', hand='up', brow=1,
               bubbles=((4.5, 6.5, 1.2), (21.5, 3.5, 0.5))),
        darwin(-1, legs_dy=-1, eyes='happy', mouth='open', hand='up', bubbles=((4.5, 3.5, 1.4),)),
        darwin(1, squash=0.5, eyes='happy', mouth='smile', hand='book'),            # aterriza
        darwin(0),                                                                   # = Idle 0
    ]


# ============================================================ 2. MOSTRADOR
def jar(cv, x, y, w, h, thing, rng):
    """Frasco de especímenes de vidrio con tapón de corcho y algo flotando en formol."""
    # tapón
    rect(cv, x + 1, y, x + w - 2, y + 1, CORK[2])
    rect(cv, x + 1, y + 1, x + w - 2, y + 1, CORK[1])
    cv.put(x + 1, y, CORK[3])
    # cuello y cuerpo
    for yy in range(y + 2, y + h):
        for xx in range(x, x + w):
            t = (yy - (y + 2)) / max(1, h - 3)
            edge = xx in (x, x + w - 1)
            if yy == y + 2 and edge:
                continue
            liquid = yy >= y + 4
            if edge:
                c = col(GLASS[3], 255) if xx == x else col(GLASS[1], 255)
            elif liquid:
                c = LIQ[2] if xx < x + w / 2 else LIQ[1]
                if yy == y + 4:
                    c = LIQ[3]
            else:
                c = col(GLASS[2], 150)
            cv.put(xx, yy, c)
    rect(cv, x + 1, y + h - 1, x + w - 2, y + h - 1, LIQ[0])
    # brillo del vidrio
    cv.put(x + 1, y + 3, '#ffffff')
    cv.put(x + 1, y + 5, col(GLASS[3], 230))
    # espécimen
    cx, cy = x + w / 2, y + 4 + (h - 4) / 2
    if thing == 'seahorse':                         # caballito de mar
        for (dx, dy, c) in ((0, -2, '#e08a4a'), (1, -2, '#e08a4a'), (-0.4, -1, '#c0602a'), (0, 0, '#e08a4a'),
                            (0.6, 1, '#c0602a'), (0, 2, '#c0602a'), (-1, 2, '#a04a24'), (1.6, -2, '#ffd08a')):
            cv.put(cx + dx - .5, cy + dy, c)
    elif thing == 'fish':                            # pececillo pálido
        for (dx, dy, c) in ((-1.5, 0, '#f0e0d0'), (-0.5, 0, '#f0e0d0'), (0.5, 0, '#d8c0b0'), (1.5, -1, '#b89a8a'),
                            (1.5, 1, '#b89a8a'), (-1.5, -1, '#ffffff')):
            cv.put(cx + dx, cy + dy, c)
        cv.put(cx - 1.5, cy, OUTL)
    elif thing == 'worm':                            # gusano enroscado
        for (dx, dy) in ((-1, -1), (0, -1.5), (1, -1), (1, 0), (0, 0.5), (-1, 1), (0, 1.8)):
            cv.put(cx + dx - .5, cy + dy, CORAL[2])
        cv.put(cx - .5, cy - 1.5, CORAL[3])


def counter():
    """Mostrador de madera de naufragio con coral, conchas, frascos y un libro abierto.
    48x24: cara de arriba en y 9-13, frente en y 14-22."""
    cv = Cv(48, 24)
    rng = random.Random(7)
    top0, top1, bot = 9, 13, 22
    # frente: tablas verticales oscuras
    for y in range(top1 + 1, bot + 1):
        for x in range(1, 47):
            plank = (x - 1) % 8
            c = WOOD[1] if plank not in (0, 7) else WOOD[0]
            if plank == 1:
                c = WOOD[2]
            if y == top1 + 1:
                c = WOOD[0]                               # sombra bajo el canto
            cv.put(x, y, c)
    for x in range(1, 47, 8):                              # clavos
        cv.put(x + 3, top1 + 3, '#8a7a6a')
        cv.put(x + 3, bot - 2, '#8a7a6a')
    # cara de arriba: tabla clara con vetas
    for y in range(top0, top1 + 1):
        for x in range(1, 47):
            if y == top0 and x in (1, 46):
                continue
            c = WOOD_TOP[2]
            if y == top0:
                c = WOOD_TOP[3]
            elif y == top1:
                c = WOOD_TOP[1]
            elif (x * 5 + y * 11) % 13 == 0:
                c = WOOD_TOP[1]
            cv.put(x, y, c)
    # coral rosa creciendo en las esquinas del frente y algas
    for (cx, cy) in ((3, 19), (44, 18)):
        for k, (dx, dy, r) in enumerate(((0, 0, 2.2), (1.6, -1.8, 1.4), (-1.2, -2.4, 1.2), (0.8, 2.2, 1.5))):
            ell(cv, cx + dx, cy + dy, r, r, CORAL)
        cv.put(cx - 0.5, cy - 0.5, CORAL[3])
    for (cx, cy) in ((14, 20), (33, 21), (25, 17)):         # percebes (conchas pequeñas)
        ell(cv, cx, cy, 1.5, 1.2, SHELL)
        cv.put(cx - .5, cy - .5, SHELL[0])
    for x in (8, 9, 38, 39, 40):                            # alga colgando del canto
        for y in range(top1 + 1, top1 + 3 + (x % 3)):
            cv.put(x, y, SOUP['algae'][1] if y < top1 + 3 else SOUP['algae'][0])
    # libro abierto sobre el mostrador (a la izquierda de Darwin)
    for y in range(10, 14):
        for x in range(12, 21):
            c = PAPER[2] if x < 16 else PAPER[1]
            if x == 16:
                c = PAPER[0]
            if y == 13:
                c = BOOK[1]
            cv.put(x, y, c)
    for (x0, x1) in ((13, 15), (17, 19)):                     # renglones (sin texto)
        for x in range(x0, x1):
            cv.put(x, 11, PAPER[1])
    cv.put(13, 10, PAPER[3])
    # frascos de especímenes encima
    jar(cv, 3, 1, 6, 11, 'seahorse', rng)
    jar(cv, 31, 4, 5, 8, 'worm', rng)
    jar(cv, 37, 0, 5, 10, 'fish', rng)     # detrás de la concha
    # vieira junto al frasco del pez
    for y in range(24):
        for x in range(48):
            dx, dy = x + .5 - 44, y + .5 - 13.4
            if (dx / 3.4) ** 2 + (dy / 3.0) ** 2 <= 1 and dy < 0.8:      # vieira: abanico con costillas
                rib = int((math.atan2(dy, dx) + math.pi) / 0.5) % 2
                cv.put(x, y, SHELL[(3 if dy < -1.5 else 2) if rib else 1])
    for x in (43, 44, 45):
        cv.put(x, 14, SHELL[1])
    cv.outline()
    return cv


# ============================================================ 3. PEDESTALES
PED_TOP = (10.0, 4.0, 8.4, 3.0)                  # centro y radios de la cara de arriba


def pedestal_mask(x, y):
    cx, cy, rx, ry = PED_TOP
    px, py = x + .5, y + .5
    top = ((px - cx) / rx) ** 2 + ((py - cy) / ry) ** 2 <= 1
    # columna algo más estrecha que se ensancha en la base (coral/esponja)
    if cy <= py <= 14.0:
        t = (py - cy) / (14.0 - cy)
        hw = 7.4 - 1.2 * math.sin(t * math.pi) + (0.8 if t > 0.85 else 0)
        col_ = abs(px - cx) <= hw
    else:
        col_ = False
    base = ((px - cx) / 8.6) ** 2 + ((py - 13.2) / 1.6) ** 2 <= 1
    return top or col_ or base


def pedestal():
    """Columna corta de esponja/roca con la cara de arriba plana y más clara. 20x16."""
    cv = Cv(20, 16)
    cx, cy, rx, ry = PED_TOP
    rng = random.Random(3)
    for y in range(16):
        for x in range(20):
            if not pedestal_mask(x, y):
                continue
            px, py = x + .5, y + .5
            topf = ((px - cx) / rx) ** 2 + ((py - cy) / ry) ** 2
            if topf <= 1:
                d = topf
                c = ROCK[3] if (d > 0.6 and py < cy and px < cx + 3) else ROCK[2]
                if d > 0.7 and py > cy:
                    c = SPONGE[2]                       # canto delantero de la cara de arriba
                cv.put(x, y, c)
            else:
                nx = (px - cx) / 7.5
                i = 2 if nx < -0.45 else 1 if nx < 0.5 else 0
                if py > 13.2 or py < cy + ry + 1.2:
                    i = max(0, i - 1)                   # sombra bajo el canto y en la base
                if x % 4 == 1 and i > 0 and py < 13:
                    i -= 1                              # estrías verticales de la esponja
                cv.put(x, y, SPONGE[i])
    # musgo
    for (x, y) in ((3, 6), (4, 7), (16, 7)):
        cv.put(x, y, SOUP['algae'][1])
    cv.outline()
    return cv


def pedestal_glow(f):
    """Brillo semitransparente que se pinta encima del pedestal: borde y cara de arriba."""
    base = pedestal()
    cv = Cv(20, 16)
    a = (90, 140, 190, 140)[f]
    c = (200, 255, 214)
    cx, cy, rx, ry = PED_TOP
    for (x, y), cc in base.px.items():
        if cc[:3] == OUTL[:3]:
            cv.put(x, y, c + (min(255, a + 60),))           # el contorno se ilumina
        else:
            px, py = x + .5, y + .5
            topf = ((px - cx) / rx) ** 2 + ((py - cy) / ry) ** 2
            if topf <= 1:
                cv.put(x, y, c + (int(a * (0.25 + 0.45 * topf)),))   # aro brillante en la cara de arriba
            else:
                cv.put(x, y, c + (int(a * 0.18),))
    return cv


# ============================================================ 4. ARTÍCULOS
U.LINEAGES['Upgrade'] = dict(ramp=GOLD, light='#ffffff', inner=GOLD[0], color=(248, 212, 96))
SPARKS = [(2.5, 3.5), (20.5, 2.5), (21.5, 19.5), (2.5, 20.5)]


def star(cv, x, y, size, c_core='#ffffff', c_arm=GOLD[2], a=255):
    cv.put(x, y, col(c_core, a))
    for k in range(1, size + 1):
        cc = col(c_arm if k == size else c_core, a)
        for dx, dy in ((k, 0), (-k, 0), (0, k), (0, -k)):
            cv.put(x + dx, y + dy, cc)


def upgrade(f):
    """Burbuja dorada de mejora (la de mutación, con la rampa del nivel III) y destellos."""
    cv = U.bubble('Upgrade', f)
    # destellos que se encienden por turnos en las esquinas (fuera del centro de 16x16)
    for k, (x, y) in enumerate(SPARKS):
        ph = (f - k * 1.5) % 6
        if ph < 1:
            star(cv, x, y, 1)
        elif ph < 2:
            star(cv, x, y, 2 if k % 2 == 0 else 1)
        elif ph < 3:
            cv.put(x, y, GOLD[3])
    return cv


# corazones a mano, de la familia de UI_Heart_Full (escalarlo a 12 px lo deforma)
H7 = ['.XX.XX.', 'XXXXXXX', 'XXXXXXX', '.XXXXX.', '..XXX..', '...X...']
H9 = ['.XXX.XXX.', 'XXXXXXXXX', 'XXXXXXXXX', 'XXXXXXXXX', '.XXXXXXX.', '..XXXXX..', '...XXX...', '....X....']
H11 = ['.XXX...XXX.', 'XXXXX.XXXXX', 'XXXXXXXXXXX', 'XXXXXXXXXXX', 'XXXXXXXXXXX', '.XXXXXXXXX.',
       '..XXXXXXX..', '...XXXXX...', '....XXX....', '.....X.....']
H13 = ['.XXXX...XXXX.', 'XXXXXX.XXXXXX', 'XXXXXXXXXXXXX', 'XXXXXXXXXXXXX', 'XXXXXXXXXXXXX', 'XXXXXXXXXXXXX',
       '.XXXXXXXXXXX.', '..XXXXXXXXX..', '...XXXXXXX...', '....XXXXX....', '.....XXX.....', '......X......']


def heart_px(rows, n, bottom):
    """Casillas del corazón centrado en x en un lienzo de n, con la punta en la fila bottom."""
    w, h = len(rows[0]), len(rows)
    ox, oy = (n - w) // 2, bottom - h + 1
    return {(ox + i, oy + j) for j, r in enumerate(rows) for i, ch in enumerate(r) if ch == 'X'}, (ox, oy, w, h)


def shade_heart(cv, pts, box, light=0.0):
    ox, oy, w, h = box
    for (x, y) in pts:
        nx, ny = (x + .5 - ox - w / 2) / (w / 2), (y + .5 - oy - h * 0.42) / (h / 2)
        cv.put(x, y, HEART[ramp_idx(shade_v(nx, ny) + light)])


def heal(f):
    """Glóbulo de líquido rojo con forma de corazón que late (medio corazón de vida). 12x12."""
    cv = Cv(12)
    rows = [H9, H7, H9, H9][f]
    pts, box = heart_px(rows, 12, 9 if rows is H9 else 9)
    shade_heart(cv, pts, box, -0.05)
    ox, oy, w, h = box
    cv.put_in(ox + 1, oy + 1, '#ffffff')                     # brillo de líquido
    cv.put_in(ox + 2, oy + 1, HEART[3])
    cv.put_in(ox + w - 3, oy + h - 4 + (f % 2), HEART[3])     # burbujita dentro
    cv.outline()
    return cv


def heart_container(f):
    """Corazón extra: corazón grande con membrana clara, núcleo que late y brillo dorado. 16x16."""
    cv = Cv(16)
    rows = [H13, H11, H13, H13][f]
    pts, box = heart_px(rows, 16, 13)
    ox, oy, w, h = box
    shade_heart(cv, pts, box, -0.1)
    for (x, y) in pts:                                     # membrana: canto claro arriba a la izquierda
        edge = [(dx, dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)) if (x + dx, y + dy) not in pts]
        if any(d in ((-1, 0), (0, -1)) for d in edge):
            cv.put(x, y, '#ffc4cc')
        elif edge:
            cv.put(x, y, '#9a2638')
    # núcleo de la célula-corazón
    nx0, ny0 = ox + w - 5, oy + h // 2 - 1
    for (x, y) in ((nx0, ny0), (nx0 + 1, ny0), (nx0, ny0 + 1), (nx0 + 1, ny0 + 1), (nx0 - 1, ny0 + 1)):
        cv.put_in(x, y, HEART[0])
    cv.put_in(nx0, ny0, '#a02a3a')
    # brillo grande
    for (x, y, c) in ((2, 1, '#ffffff'), (3, 1, '#ffffff'), (2, 2, '#ffffff'), (4, 1, HEART[3]), (2, 3, HEART[3])):
        cv.put_in(ox + x - 1, oy + y, c)
    cv.outline()
    # destello dorado que se enciende y apaga (lo hace más valioso que la curación)
    if f in (0, 1, 2):
        star(cv, 13, 2, 1 if f != 1 else 2)
    return cv


def purchase(f):
    """Efecto de compra: destello y chispas doradas y de biomasa que suben y se desvanecen. 32x32."""
    cv = Cv(32)
    c = 16
    if f == 0:
        ell(cv, c, c, 5.5, 5.5, ['#fff6d0', '#fffbe8', '#ffffff', '#ffffff'])
        star(cv, c - .5, c - .5, 9, '#ffffff', GOLD[2], 230)
        return cv
    if f == 1:
        for y in range(32):
            for x in range(32):
                d = math.hypot(x + .5 - c, y + .5 - c)
                if abs(d - 8.5) < 1.1:
                    cv.put(x, y, col(GOLD[3] if y < c else GOLD[2], 230))
                elif d < 3.5:
                    cv.put(x, y, col('#ffffff', 200))
        star(cv, c - .5, c - .5, 5, '#ffffff', GOLD[3], 200)
    rng = random.Random(11)
    a = (255, 255, 235, 190, 120, 60)[f]
    for j in range(20):
        ang = -math.pi / 2 + rng.uniform(-1.25, 1.25)
        sp = rng.uniform(0.75, 1.25)
        r = (0, 6, 9, 11.5, 13.5, 15)[f] * sp
        rise = (0, 0, 1.5, 3.5, 6, 8.5)[f] * sp
        x = c + math.cos(ang) * r
        y = c + math.sin(ang) * r * 0.8 - rise
        cc = (GOLD if j % 2 else BIO)
        if not (0 <= x < 32 and 0 <= y < 32):
            continue
        if f <= 3 and j % 3 == 0:
            star(cv, x, y, 1, cc[3], cc[2], a)
        else:
            cv.put(x, y, col(cc[3] if f < 4 else cc[2], a))
            if f < 5:
                cv.put(x, y + 1, col(cc[1], int(a * 0.7)))         # estela hacia abajo
    return cv


# ============================================================ 5. TECLA E
GLYPH_E = ['111', '100', '110', '100', '111']


def key_e(pressed):
    """Tecla de membrana/vidrio (como los botones del HUD) con la letra E. 13x13.
    0: normal; 1: pulsada (la cara baja 1 px y se oscurece)."""
    cv = Cv(13)
    top = 1 if pressed else 0
    rim = MEM if not pressed else ['#0c1e1a', '#16382f', '#2a5e50', '#4a9078']
    fill = MEM[1] if not pressed else '#173a32'
    for y in range(top, 13):
        for x in range(13):
            ax, ay = min(x, 12 - x), min(y - top, 12 - y)
            if ax + ay < 2 and ax < 2 and ay < 2:           # esquinas redondeadas
                if ax + ay < 1 or (ax == 0 or ay == 0) and ax + ay < 2:
                    continue
            if x in (0, 12) or y in (top, 12):
                cv.put(x, y, OUTL)
                continue
            side_y = 10 if not pressed else 11              # grosor de la tecla (lado de abajo)
            if y >= side_y:
                cv.put(x, y, rim[0] if y == 11 else rim[1])
                continue
            # cara de arriba: borde iluminado arriba a la izquierda
            if y == top + 1 or x == 1:
                c = rim[3] if not (x == 11 or y == side_y - 1) else rim[2]
            elif x == 11 or y == side_y - 1:
                c = rim[1]
            else:
                c = fill
            cv.put(x, y, c)
    cv.put(2, top + 2, '#e4ffd8' if not pressed else MEM[3])      # brillo del vidrio
    # letra E (3x5), con sombra oscura debajo
    gx, gy = 5, top + 3
    lc = '#ecffe0' if not pressed else '#a8d8bc'
    for j, row in enumerate(GLYPH_E):
        for i, bit in enumerate(row):
            if bit == '1':
                if (gx + i, gy + j + 1) not in cv.px or cv.px[(gx + i, gy + j + 1)][:3] != C(lc)[:3]:
                    cv.put(gx + i, gy + j + 1, OUTL)
    for j, row in enumerate(GLYPH_E):
        for i, bit in enumerate(row):
            if bit == '1':
                cv.put(gx + i, gy + j, lc)
    return cv


# ============================================================ 6. ICONO DEL MAPA
def map_shop():
    """Frasco de espécimen con biomasa (verde amarillento) y tapón de corcho. 7x7, centro (3, 3)."""
    cv = Cv(7)
    rows = ['.kkk.',
            'gwhwg',
            'gbBbg',
            'gBbbg',
            '.ggg.']
    pal = {'k': CORK[2], 'g': GLASS[3], 'w': GLASS[2], 'h': '#ffffff', 'b': BIO[2], 'B': BIO[3]}
    for j, row in enumerate(rows):
        for i, ch in enumerate(row):
            if ch in pal:
                cv.put(i + 1, j + 1, pal[ch])
    cv.put(2, 1, CORK[3])
    cv.outline()
    return cv


# ============================================================ GUARDAR
MS = {}


def save(name, frames, ms=None, gif_scale=None):
    os.makedirs(OUT, exist_ok=True)
    sheet(frames).save(os.path.join(OUT, name + '.png'))
    if len(frames) > 1:
        os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
        w, h = frames[0].w, frames[0].h
        sc = gif_scale or max(4, 192 // max(w, h))
        out = []
        for cv in frames:
            b = Image.new('RGBA', (w, h), BG)
            b.alpha_composite(cv.img())
            out.append(b.resize((w * sc, h * sc), Image.NEAREST).convert('RGB'))
        durs = ms if isinstance(ms, list) else [ms] * len(frames)
        out[0].save(os.path.join(OUT, 'gif', name + '.gif'), save_all=True, append_images=out[1:],
                    duration=durs, loop=0)
    MS[name] = (len(frames), frames[0].w, frames[0].h, ms)


IDLE_MS = [220, 160, 90, 180, 180, 220]
BUY_MS = [70, 80, 110, 80, 90, 120]


def build():
    A = {}
    A['Shop_Darwin_Idle'] = (darwin_idle(), IDLE_MS)
    A['Shop_Darwin_Buy'] = (darwin_buy(), BUY_MS)
    A['Shop_Counter'] = ([counter()], None)
    A['Shop_Pedestal'] = ([pedestal()], None)
    A['Shop_Pedestal_Glow'] = ([pedestal_glow(f) for f in range(4)], 150)
    A['Item_Upgrade_Idle'] = ([upgrade(f) for f in range(6)], 120)
    A['Item_Heal_Idle'] = ([heal(f) for f in range(4)], 150)
    A['Item_HeartContainer_Idle'] = ([heart_container(f) for f in range(4)], 130)
    A['Item_Purchase'] = ([purchase(f) for f in range(6)], 60)
    A['UI_Key_E'] = ([key_e(False), key_e(True)], 200)
    A['UI_Map_Shop'] = ([map_shop()], None)
    return A


# ============================================================ VISTA PREVIA
W, H = 480, 270
ptext, tw = G.ptext, G.tw


def arrow_text(im, x, y, fill=(255, 240, 200)):
    """'I -> II' con la fuente de la maqueta y una flecha de 5 px (el juego usa Silkscreen)."""
    ptext(im, (x, y), 'I', fill)
    px = im.load()
    ax = x + 5
    for i in range(5):                                     # flecha
        px[ax + i, y + 2] = fill[:3] + (255,)
        px[ax + i, y + 3] = OUTL
    for (dx, dy) in ((3, 1), (3, 3), (2, 0), (2, 4)):
        px[ax + dx, y + dy] = fill[:3] + (255,)
    ptext(im, (ax + 7, y), 'II', fill)
    return ax + 7 + tw('II') - x


def price(im, cx, y, n, fill=(236, 250, 214)):
    s = str(n)
    w = 12 + 2 + tw(s)
    x = cx - w // 2
    im.alpha_composite(U.ui_biomass().img(), (x, y))
    ptext(im, (x + 14, y + 4), s, fill)


def item_on_pedestal(scr, A, pcx, ptop, item, frame=0, icon=None, glow=False):
    """Pedestal con su cara de arriba en ptop; el artículo flota encima, centrado en x,
    con la sombra Item_Mutation_Shadow en la cara de arriba."""
    scr.alpha_composite(A['Shop_Pedestal'][0][0].img(), (pcx - 10, ptop))
    if glow:
        scr.alpha_composite(A['Shop_Pedestal_Glow'][0][2].img(), (pcx - 10, ptop))
    if item is None:
        return
    scr.alpha_composite(U.bubble_shadow().img(), (pcx - 8, ptop + 1))
    im = item.img() if hasattr(item, 'img') else item
    scr.alpha_composite(im, (pcx - im.width // 2, ptop + 3 - im.height - 1))
    if icon:
        ic = U.ICONS[icon](16).img()
        scr.alpha_composite(ic, (pcx - 8, ptop + 3 - im.height - 1 + (im.height - 16) // 2))
    return ptop + 3 - im.height - 1


DARWIN_IN_COUNTER = (12, -13)   # esquina de Darwin respecto a la del mostrador


def shop_room(A):
    scr = U.floor_bg(W, H, walls=True)
    # mostrador y Darwin, arriba en el centro
    cx0, cy0 = 240 - 24, 86
    dx, dy = DARWIN_IN_COUNTER
    scr.alpha_composite(A['Shop_Darwin_Idle'][0][0].img(), (cx0 + dx, cy0 + dy))
    scr.alpha_composite(A['Shop_Counter'][0][0].img(), (cx0, cy0))
    # 4 pedestales en fila, 2 casillas por debajo del mostrador
    ptop = cy0 + 24 + 32
    centers = [240 - 72, 240 - 24, 240 + 24, 240 + 72]
    items = [(U.bubble('Predator', 0), 'Mandibles', 15), (U.bubble('Toxic', 0), 'Spores', 15),
             (A['Item_Upgrade_Idle'][0][0], 'Fins', 20), (A['Item_Heal_Idle'][0][0], None, 5)]
    for i, (pcx, (it, ic, cost)) in enumerate(zip(centers, items)):
        top = item_on_pedestal(scr, A, pcx, ptop, it, icon=ic, glow=(i == 1))
        yb = ptop + 18
        if i == 2:
            w = arrow_text(scr, pcx - 9, yb)
            yb += 8
        price(scr, pcx, yb + (0 if i == 2 else 4), cost)
        if i == 1:
            scr.alpha_composite(A['UI_Key_E'][0][0].img(), (pcx - 6, top - 15))
    # la célula del jugador junto al segundo pedestal, mirándolo
    cell = M.PANIMS['Idle_side'][0].img()
    scr.alpha_composite(cell, (centers[1] - 34, ptop + 6))
    return scr


def anim_strip(A):
    scr = U.floor_bg(W, 150)
    lab = (214, 255, 154)
    ptext(scr, (8, 4), 'SHOP_DARWIN_IDLE', lab)
    for i, cv in enumerate(A['Shop_Darwin_Idle'][0]):
        scr.alpha_composite(cv.img(), (8 + i * 28, 12))
    ptext(scr, (184, 4), 'SHOP_DARWIN_BUY', lab)
    for i, cv in enumerate(A['Shop_Darwin_Buy'][0]):
        scr.alpha_composite(cv.img(), (184 + i * 28, 12))
    # Darwin tras el mostrador, cuadro a cuadro de la compra (para ver cuánto asoma)
    ptext(scr, (360, 4), 'TRAS EL MOSTRADOR', lab)
    scr.alpha_composite(A['Shop_Darwin_Buy'][0][2].img(), (372 + 12, 22 - 13 + 2))
    scr.alpha_composite(A['Shop_Counter'][0][0].img(), (372, 24))
    ptext(scr, (8, 52), 'CORAZON EXTRA', lab)
    for i, cv in enumerate(A['Item_HeartContainer_Idle'][0]):
        item_on_pedestal(scr, A, 20 + i * 26, 80, cv)
    ptext(scr, (118, 52), 'CURACION', lab)
    for i, cv in enumerate(A['Item_Heal_Idle'][0]):
        item_on_pedestal(scr, A, 130 + i * 24, 80, cv)
    ptext(scr, (222, 52), 'MEJORA (6)', lab)
    for i, cv in enumerate(A['Item_Upgrade_Idle'][0]):
        scr.alpha_composite(cv.img(), (222 + i * 26, 62))
    ptext(scr, (8, 104), 'ITEM_PURCHASE', lab)
    for i, cv in enumerate(A['Item_Purchase'][0]):
        scr.alpha_composite(cv.img(), (8 + i * 34, 112))
    ptext(scr, (218, 104), 'VACIO', lab)
    item_on_pedestal(scr, A, 232, 124, None)
    ptext(scr, (262, 104), 'GLOW', lab)
    for i in range(4):
        scr.alpha_composite(A['Shop_Pedestal'][0][0].img(), (262 + i * 24, 124))
        scr.alpha_composite(A['Shop_Pedestal_Glow'][0][i].img(), (262 + i * 24, 124))
    ptext(scr, (366, 104), 'TECLA E', lab)
    for i, cv in enumerate(A['UI_Key_E'][0]):
        scr.alpha_composite(cv.img(), (366 + i * 18, 114))
        scr.alpha_composite(cv.img(), (366 + i * 18, 132))
    # tecla E sobre artículos (lectura)
    scr.alpha_composite(U.bubble('Armored', 0).img(), (408, 112))
    scr.alpha_composite(A['UI_Key_E'][0][0].img(), (414, 104))
    scr.alpha_composite(A['Item_Upgrade_Idle'][0][0].img(), (436, 112))
    scr.alpha_composite(A['UI_Key_E'][0][0].img(), (442, 104))
    return scr


def minimap_block(A):
    scr = U.floor_bg(W, 90)
    shop = A['UI_Map_Shop'][0][0].img()
    # minimapa del HUD con la tienda a la izquierda de la sala actual
    fx, fy = 8, 8
    ix, iy = fx + 4, fy + 4
    rooms = {
        'Tienda': (0, 1, 'small', 'visited'),
        'Actual': (1, 1, 'rect', 'visited'),
        'Vecina': (2, 1, 'small', 'unexplored'),
        'Abajo': (1, 2, 'rect', 'uncleared'),
    }
    links = [('Tienda', 'Actual'), ('Actual', 'Vecina'), ('Actual', 'Abajo')]
    plan = MP.paint_floor(rooms, links, 'Actual', 3, 3)
    px_, py_ = MP.room_center(rooms, 'Actual')
    px_ -= 8
    back = MP.minimap_back()
    scr.alpha_composite(U.nine_render(back, 80, 60, 2), (ix, iy))
    big = Image.new('RGBA', (plan.width + 160, plan.height + 120), (0, 0, 0, 0))
    big.alpha_composite(plan, (80, 60))
    tx, ty = MP.room_center(rooms, 'Tienda')
    big.alpha_composite(shop, (80 + tx - 3, 60 + ty - 3))
    view = big.crop((80 + px_ - 40, 60 + py_ - 30, 80 + px_ + 40, 60 + py_ + 30))
    scr.alpha_composite(view, (ix, iy))
    scr.alpha_composite(MP.map_player(1).img(), (ix + 40 - 3 + 8, iy + 30 - 3))
    scr.alpha_composite(U.nine_render(MP.minimap_frame().img(), 88, 68, 6), (fx, fy))
    # lectura: el icono sobre cada color de la paleta, junto a la recompensa y el jugador
    for i, (name, c) in enumerate(MP.PALETTE):
        x = 110 + i * 46
        scr.alpha_composite(Image.new('RGBA', (40, 18), C(c)), (x, 12))
        scr.alpha_composite(shop, (x + 3, 17))
        scr.alpha_composite(MP.map_reward().img(), (x + 16, 17))
        scr.alpha_composite(MP.map_player(2).img(), (x + 29, 17))
        ptext(scr, (x, 34), str(i), (200, 230, 215))
    # y a x6 para revisarlo
    scr.alpha_composite(shop.resize((42, 42), Image.NEAREST), (120, 44))
    scr.alpha_composite(MP.map_reward().img().resize((42, 42), Image.NEAREST), (170, 44))
    scr.alpha_composite(MP.map_player(2).img().resize((42, 42), Image.NEAREST), (220, 44))
    return scr


def preview(A, path):
    S = 3
    shots = [('1. LA TIENDA', shop_room(A)), ('2. ANIMACIONES Y ARTICULOS', anim_strip(A)),
             ('3. MINIMAPA: TIENDA EN UNA SALA LATERAL Y LECTURA SOBRE LA PALETA', minimap_block(A))]
    lab = 24
    out = Image.new('RGBA', (W * S + 20, sum(im.height * S + lab + 10 for _, im in shots) + 10), BG)
    y = 10
    for name, im in shots:
        ptext(out, (12, y + 6), name, (214, 255, 154), scale=2)
        y += lab
        out.alpha_composite(im.resize((im.width * S, im.height * S), Image.NEAREST), (10, y))
        y += im.height * S + 10
    out.convert('RGB').save(path)
    # Darwin a tamaño real y a x8, para comprobar que se reconoce
    real = Image.new('RGBA', (24 * 6 + 40 + 24 * 8 * 2 + 30, 32 * 8 + 20), BG)
    for i, cv in enumerate(A['Shop_Darwin_Idle'][0][:1] + A['Shop_Darwin_Buy'][0][2:3]):
        real.alpha_composite(cv.img(), (10 + i * 30, 10))
        real.alpha_composite(cv.img().resize((24 * 8, 32 * 8), Image.NEAREST), (80 + i * (24 * 8 + 10), 10))
    real.convert('RGB').save(os.path.join(OUT, '_darwin_x8.png'))


def main():
    A = build()
    for name, (frames, ms) in A.items():
        save(name, frames, ms)
    preview(A, os.path.join(OUT, 'vista_previa_tienda.png'))
    for k, v in MS.items():
        print(k, *v)


if __name__ == '__main__':
    main()
