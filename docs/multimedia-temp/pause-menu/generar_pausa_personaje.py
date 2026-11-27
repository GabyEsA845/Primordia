"""Menú de pausa nuevo, estilo membrana (subtanda 2): pestañas en yema, iconos de pestaña, teclas Q/E,
lente-vacuola de la criatura y maqueta de la pantalla Personaje.

Reutiliza la paleta y las piezas de la subtanda 1 (generar_pausa_membrana.py, en esta carpeta).
Hecho con la skill primordia-sprites (pixel.py): luz arriba a la izquierda, contorno OUTL.

Uso: python generar_pausa_personaje.py [carpeta_salida]
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from generar_pausa_membrana import *          # paleta, panel, botón, título, núcleo, slice9, text, game_backdrop
import generar_pausa_membrana as M

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__))
MT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))          # docs/multimedia-temp
UNDER = C('#0f221d')            # interior opaco de la pestaña activa (tapa la membrana del panel debajo)
HEART = ['#7a1e2a', '#c8384a', '#f0706e', '#ffd0c0']
AMBER = ['#7a3a10', '#c86a1c', '#f2a83a', '#ffe0a0']
GREEN = ['#2a6a2a', '#4ea040', '#8ad860', '#d4ffb0']


def smax(a, b, k):
    """Unión suave de dos formas (profundidad positiva dentro): deja un remate cóncavo de radio ~k."""
    h = max(k - abs(a - b), 0) / k
    return max(a, b) + h * h * k / 4


# --- pestañas (40×24; la línea de arriba del panel cae en la fila 18; bordes L12 R12, modo Tiled) -------
TAB_W, TAB_H, TAB_B, TAB_EDGE = 40, 24, 12, 18
IDLE_TOP = 3                    # la yema inactiva empieza 3 px más abajo; su interior (filas 6-17) cabe un icono de 12
BODY_X0, BODY_X1, TAB_R = 5, 35, 6

def _tab_depth(x, y, top):
    """Profundidad dentro de la yema (cuerpo con las esquinas de arriba redondeadas, abierto por abajo)."""
    d, _ = rr(x - BODY_X0, y - top, BODY_X1 - BODY_X0, 200, TAB_R)
    return d


def tab_active():
    """Yema unida al panel: su membrana baja y se funde con la del panel con un remate cóncavo."""
    cv = Canvas(TAB_W, TAB_H)
    def field(x, y):
        return smax(_tab_depth(x, y, 0), (y + 0.5) - TAB_EDGE, 3.5)
    for y in range(TAB_H):
        for x in range(TAB_W):
            d = field(x, y)
            if d <= 0: continue
            k = int(d)
            if y >= TAB_EDGE + 4 and k < 6: continue          # bajo la membrana del panel, fuera de la yema: lo pinta el panel
            gx, gy = field(x + 1, y) - field(x - 1, y), field(x, y + 1) - field(x, y - 1)
            g = math.hypot(gx, gy) or 1
            l = lit((-gx / g, -gy / g))
            if k == 0: c = OUTL
            elif k == 1: c = A(MEM_HI[3] if l > 0 else MEM_HI[2] if l == 0 else MEM[1])
            elif k == 2: c = A(MEM_HI[1])
            elif k == 3: c = A(MEM[2] if l >= 0 else MEM[1]) if (x + y) % 2 == 0 else A(MEM[1], 225)
            else: c = UNDER
            cv.put(x, y, c)
    return cv


def tab_bud(hover=False):
    """Yema cerrada sobre el panel (pestaña inactiva o con el ratón encima): más baja y con su propia base."""
    cv = Canvas(TAB_W, TAB_H)
    top, rim = IDLE_TOP, (MEM_HI if hover else MEM)
    for y in range(top, TAB_EDGE + 1):
        for x in range(TAB_W):
            d = _tab_depth(x, y, top)
            if d <= 0: continue
            k = int(d)
            if y == TAB_EDGE: c = OUTL                       # se apoya en la línea de arriba del panel
            elif k == 0: c = OUTL
            elif k == 1: c = A(rim[3] if (y - top) < 3 or x < BODY_X0 + 2 else rim[1])
            elif k == 2: c = A(rim[1])
            else: c = A(MEM_HI[0] if hover else MEM[0], 235)
            cv.put(x, y, c)
    for s in (-1, 1):                                       # pie: un remate en cada lado que la pega al panel
        bx = BODY_X0 - 1 if s < 0 else BODY_X1
        for j, w in ((TAB_EDGE - 1, 2), (TAB_EDGE - 2, 1)):
            for i in range(w):
                cv.put(bx + s * i, j, A(rim[1]) if (j, i) != (TAB_EDGE - 1, w - 1) else OUTL)
            cv.put(bx + s * w, j, OUTL)
    return cv


# --- iconos de pestaña (12×12) ----------------------------------------------------------------------
def _finish(cv):
    cv.outline(); cv.clean(); return cv

def icon_creature():
    cv = Canvas(12)
    ellipse(cv, 6, 6.5, 4.6, 4.2, GREEN)
    _finish(cv)
    for x, y in ((5, 5), (8, 5)):
        cv.put(x, y, EYE); cv.put(x, y + 1, EYE)
    cv.put(3, 4, A(GREEN[3]))
    return cv

HEART_SHAPE = ['.##...##.', '####.####', '#########', '#########', '.#######.', '..#####..', '...###...', '....#....']

def icon_state():
    cv = Canvas(12)
    for j, row in enumerate(HEART_SHAPE):
        for i, ch in enumerate(row):
            if ch == '#':
                f = 3 if i + j < 4 else 2 if i + j < 8 else 1 if i + j < 11 else 0   # luz arriba a la izquierda
                cv.put(1 + i, 2 + j, A(HEART[f]))
    _finish(cv)
    cv.put(2, 3, A(HEART[3])); cv.put(3, 3, A('#ffe8e0'))
    return cv


def icon_items():
    cv = Canvas(12)
    pts = [(6, 1), (10, 4), (10, 8), (6, 11), (2, 8), (2, 4)]
    def inside(x, y):
        px, py = x + 0.5, y + 0.5; s = 0
        for i in range(6):
            (x0, y0), (x1, y1) = pts[i], pts[(i + 1) % 6]
            if (x1 - x0) * (py - y0) - (y1 - y0) * (px - x0) < 0: return False
        return True
    for y in range(12):
        for x in range(12):
            if inside(x, y):
                f = 3 if x < 5 and y < 5 else 2 if x < 6 else 1 if y < 9 else 0   # facetas: arriba izq. clara
                cv.put(x, y, A(AMBER[f]))
    _finish(cv)
    cv.put(4, 3, A('#fff6c0'))
    return cv

def icon_run():
    cv = Canvas(12)
    for y in range(1, 11):
        half = 4 - min(y - 1, 10 - y) * 0.85                # reloj de arena: ancho arriba y abajo, cintura en medio
        for x in range(12):
            if abs(x + 0.5 - 6) <= max(half, 0.9):
                cv.put(x, y, A(GLASS[2], 230))
    for y in (1, 10):
        for x in range(2, 10): cv.put(x, y, A(SAND[1]))
    for x, y in ((5, 3), (6, 3), (4, 3), (7, 3), (5, 4), (6, 4), (6, 5), (6, 7), (5, 8), (6, 8), (4, 9), (5, 9), (6, 9), (7, 9)):
        cv.put(x, y, A(SAND[3] if y < 6 else AMBER[2]))
    _finish(cv)
    cv.put(4, 7, A(GLASS[3]))
    return cv

TAB_ICONS = {'Creature': icon_creature, 'State': icon_state, 'Items': icon_items, 'Run': icon_run}


# --- teclas Q y E (13×13, como UI_Key_E de la tienda; 0 normal, 1 pulsada) --------------------------
GLYPHS = {'Q': ['0110', '1001', '1001', '1010', '0101'], 'E': ['111', '100', '110', '100', '111']}

def key(letter, pressed):
    cv = Canvas(13)
    top = 1 if pressed else 0
    rim = MEM if not pressed else ['#0c1e1a', '#16382f', '#2a5e50', '#4a9078']
    fill = MEM[1] if not pressed else '#173a32'
    for y in range(top, 13):
        for x in range(13):
            ax, ay = min(x, 12 - x), min(y - top, 12 - y)
            if ax + ay < 2 and ax < 2 and ay < 2 and (ax + ay < 1 or (ax == 0 or ay == 0)): continue
            if x in (0, 12) or y in (top, 12): cv.put(x, y, OUTL); continue
            side_y = 10 if not pressed else 11
            if y >= side_y: cv.put(x, y, A(rim[0] if y == 11 else rim[1])); continue
            if y == top + 1 or x == 1: c = rim[3] if not (x == 11 or y == side_y - 1) else rim[2]
            elif x == 11 or y == side_y - 1: c = rim[1]
            else: c = fill
            cv.put(x, y, A(c))
    cv.put(2, top + 2, A('#e4ffd8' if not pressed else MEM[3]))
    g = GLYPHS[letter]; gx, gy = (6 - len(g[0]) // 2), top + 3
    lc = A('#ecffe0' if not pressed else '#a8d8bc')
    for j, row in enumerate(g):
        for i, bit in enumerate(row):
            if bit == '1' and g[min(j + 1, 4)][i] != '1' or (bit == '1' and j == 4): cv.put(gx + i, gy + j + 1, OUTL)
    for j, row in enumerate(g):
        for i, bit in enumerate(row):
            if bit == '1': cv.put(gx + i, gy + j, lc)
    return cv


# --- lente-vacuola de la criatura (112×112, centro (56,56), interior de radio 47,5 como la actual) ----
LR, LC = 54.0, 56.0

def lens():
    cv = Canvas(112)
    for y in range(112):
        for x in range(112):
            dx, dy = x + 0.5 - LC, y + 0.5 - LC
            r = math.hypot(dx, dy); d = LR - r
            if d <= 0: continue
            k = int(d); n = (dx / (r or 1), dy / (r or 1)); l = lit(n)
            if k == 0: c = OUTL
            elif k == 1: c = A(MEM[3] if l > 0 else MEM[2] if l == 0 else MEM[1])
            elif k == 2: c = A(MEM[1])
            elif k == 3: c = A(MEM[2] if l >= 0 else MEM[1]) if (x + y) % 2 == 0 else A(MEM[1], 230)
            elif k == 4: c = A(MEM[1])
            elif k == 5: c = A(MEM[0])
            elif k == 6: c = A(MEM[0], 215)
            elif k < 10: c = A(GLASS[0], 190)
            else:
                t = max(0.0, min(1.0, (-(dx * 0.55 + dy * 0.85) / 47 + 1) / 2))   # algo más claro arriba a la izquierda
                c = A(GLASS[0] if t < 0.55 else '#22524c', 170)
            cv.put(x, y, c)
    for ang, rr_, out in ((205, 3.6, 1.0), (222, 2.0, 1.4), (330, 3.0, 0.6), (95, 2.6, 0.8)):   # yemas en la membrana
        a = math.radians(ang)
        vesicle(cv, LC + math.cos(a) * (LR - 2 + out), LC + math.sin(a) * (LR - 2 + out), rr_)
    return cv


def lens_glint():
    cv = Canvas(112)
    for y in range(112):
        for x in range(112):
            dx, dy = x + 0.5 - LC, y + 0.5 - LC
            r = math.hypot(dx, dy); a = math.degrees(math.atan2(dy, dx))
            if 40.5 < r < 43 and -160 < a < -112: cv.put(x, y, A(GLASS[3], 150 if r < 42 else 90))
    for x, y in ((31, 28), (30, 29)): cv.put(x, y, A(GLASS[3], 170))
    return cv


ORGANELLES = [  # (ángulo, distancia, tipo, fase)
    (150, 34, 'mito', 0.0), (35, 37, 'mito', 0.4), (250, 36, 'dot', 0.2), (300, 30, 'dot', 0.7),
    (75, 33, 'dot', 0.5), (190, 40, 'dot', 0.9), (110, 40, 'ves', 0.3), (5, 30, 'ves', 0.8),
]

def lens_organelles(frame, n=8):
    """Orgánulos que flotan despacio detrás de la criatura (sustituyen a las burbujas). Bucle de 8 cuadros."""
    cv = Canvas(112)
    for ang, dist, kind, ph in ORGANELLES:
        t = (frame / n + ph) * 2 * math.pi
        a = math.radians(ang)
        x = LC + math.cos(a) * dist + math.cos(t) * 1.2
        y = LC + math.sin(a) * dist + math.sin(t) * 1.2
        if kind == 'mito':
            o = Canvas(112)
            rot_ellipse(o, x, y, 3.6, 1.9, a + math.pi / 2 + math.sin(t) * 0.15, AQUA, alpha=210)
            o.outline(OUTL, 100)
            for i in (-1, 0, 1):
                o.put_in(x + math.cos(a + math.pi / 2) * i * 1.5, y + math.sin(a + math.pi / 2) * i * 1.5, A(AQUA[0], 210))
            cv.px.update(o.px)
        elif kind == 'ves':
            bubble(cv, x, y, 2, a=150)
        else:
            cv.put(x, y, A(MEM_HI[2], 170)); cv.put(x + 1, y, A(MEM_HI[1], 150))
    return cv


# --- casillas de mutación y sus líneas -----------------------------------------------------------------
# Casilla 26×26 (la membrana va de (1,1) a (25,25); el icono de 16 va centrado). Línea: 3×3, 9-slice de borde 1,
# Sliced (CreaturePanel la estira en tramos de 3 px de grueso). Punto: vesícula de anclaje de 7×7 que late.
SLOT_W = 26

def slot(state):
    rim = {'Normal': MEM, 'Highlighted': MEM_HI}[state]
    cv = Canvas(SLOT_W)
    for y in range(SLOT_W):
        for x in range(SLOT_W):
            d, n = rr(x - 1, y - 1, SLOT_W - 2, SLOT_W - 2, 6)
            if d <= 0:
                if state == 'Highlighted' and d > -1: cv.put(x, y, A(AQUA[2], 90))
                continue
            k, l = int(d), lit(n)
            if k == 0: c = OUTL
            elif k == 1: c = A(rim[3] if l > 0 else rim[2] if l == 0 else rim[1])
            elif k == 2: c = A(rim[1])
            elif k == 3: c = A(rim[2] if l >= 0 else rim[1]) if (x + y) % 2 == 0 else A(rim[1], 225)
            else: c = A(MEM_HI[0] if state == 'Highlighted' else FILL, 200)
            cv.put(x, y, c)
    vesicle(cv, 3.6, 3.6, 2.0)
    return cv


def callout_line():
    cv = Canvas(3)
    for y in range(3):
        for x in range(3):
            cv.put(x, y, A(MEM_HI[2]) if (x, y) == (1, 1) else A(MEM[0], 230))
    return cv


def callout_dot(frame):
    cv = Canvas(7)
    r = (2.2, 2.5, 2.8, 2.5)[frame]
    ellipse(cv, 3.5, 3.5, r, r, LIT if frame == 2 else MEM_HI)
    cv.outline(); cv.clean()
    cv.put(2, 2, A(LIT[3] if frame == 2 else MEM_HI[3]))
    return cv


# --- maqueta de Personaje ---------------------------------------------------------------------------
def load(rel):
    return Image.open(os.path.join(MT, rel)).convert('RGBA')

TABS = [('Creature', 'CRIATURA'), ('State', 'ESTADO'), ('Items', 'OBJETOS'), ('Run', 'PARTIDA')]
PX, PY, PW, PH = 58, 54, 364, 196

def tab_width(label):
    d = ImageDraw.Draw(Image.new('RGBA', (1, 1))); d.fontmode = '1'
    x0, _, x1, _ = d.textbbox((0, 0), label, font=M.font())
    w = (x1 - x0) + 12 + 3 + 2 * (BODY_X0 + 5)
    return w + (w % 2)


def personaje_mockup(active, frame, hover=None, key_down=None):
    im = game_backdrop()
    im.alpha_composite(slice9(panel(), PW, PH, PANEL_B, PANEL_B, PANEL_B, PANEL_B), (PX, PY))
    tw = 64 + 2 * TITLE_B
    im.alpha_composite(slice9(title(), tw, TITLE_H, TITLE_B, 0, TITLE_B, 0), (240 - tw // 2, 6))
    text(im, 'PERSONAJE', 240, 17, TEXT_HI, bold=True)
    # pestañas: en x par respecto al panel para que el punteado case
    widths = [tab_width(l) for _, l in TABS]
    x = PX + (PW - sum(widths) + 2 * (len(TABS) - 1)) // 2
    x += (x - PX) % 2
    for i, (key_, label) in enumerate(TABS):
        w = widths[i]
        spr = tab_active() if i == active else tab_bud(hover == i)
        im.alpha_composite(slice9(spr, w, TAB_H, TAB_B, 0, TAB_B, 0), (x, PY - TAB_EDGE))
        top = 0 if i == active else IDLE_TOP
        cy = PY - TAB_EDGE + (top + 3 + TAB_EDGE - 1 + 1) // 2      # centro del interior de la yema
        ic = as_img(TAB_ICONS[key_]())
        if i != active:
            ic = Image.blend(Image.new('RGBA', ic.size, (0, 0, 0, 0)), ic, 1.0)
            px_ = ic.load()
            for yy in range(12):
                for xx in range(12):
                    r_, g_, b_, a_ = px_[xx, yy]
                    if a_: px_[xx, yy] = (int(r_ * .6 + 20), int(g_ * .6 + 30), int(b_ * .6 + 26), a_)
        im.alpha_composite(ic, (x + BODY_X0 + 4, cy - 6))
        text(im, label, x + BODY_X0 + 4 + 12 + 3 + (w - (BODY_X0 + 4 + 12 + 3) - BODY_X0 - 4) / 2, cy,
             TEXT_HI if i == active else C('#9ec8b4'))
        if i == 0: first_x = x
        x += w - 2
    last_x = x + 2
    im.alpha_composite(as_img(key('Q', key_down == 'Q')), (first_x - 15, PY - 15))
    im.alpha_composite(as_img(key('E', key_down == 'E')), (last_x + 2, PY - 15))
    TABS_DRAW[TABS[active][0]](im, frame)
    text(im, 'ESC VOLVER', PX + PW - 44, PY + PH - 10, C('#7fa898'))
    return im


def draw_creature_tab(im, frame):
    lx, ly = PX + 120, PY + 100                              # centro de la lente
    im.alpha_composite(as_img(lens()), (lx - 56, ly - 56))
    im.alpha_composite(as_img(lens_organelles(frame)), (lx - 56, ly - 56))
    pl = load('player/Player_Idle_down.png').crop((16 * (frame // 2 % 4), 0, 16 * (frame // 2 % 4) + 16, 16))
    im.alpha_composite(pl.resize((48, 48), Image.NEAREST), (lx - 24, ly - 26))
    im.alpha_composite(as_img(lens_glint()), (lx - 56, ly - 56))
    line = as_img(callout_line())
    def seg(x0, y0, x1, y1):
        x0, x1 = sorted((int(x0), int(x1))); y0, y1 = sorted((int(y0), int(y1)))
        im.alpha_composite(slice9(line, x1 - x0 + 3, y1 - y0 + 3, 1, 1, 1, 1, tiled=False), (x0 - 1, y0 - 1))
    slots = [(-75, -72, 'ui/Icons/Icon_Mandibles_16.png', 'Highlighted'), (75, -72, 'ui/HUD/UI_Slot_Skin_Empty.png', 'Normal'),
             (-75, 72, 'ui/Icons/Icon_Fins_16.png', 'Normal'), (75, 72, 'ui/HUD/UI_Slot_Organ_Empty.png', 'Normal')]
    for sx, sy, ic, st in slots:
        cx, cy = lx + sx, int(ly + sy * 0.92)
        ex, ey = int(lx + sx * 0.28), int(ly + sy * 0.22)
        seg(cx + (13 if sx < 0 else -13), cy, ex, cy); seg(ex, cy, ex, ey)
        im.alpha_composite(as_img(callout_dot(frame % 4)), (ex - 3, ey - 3))
        im.alpha_composite(as_img(slot(st)), (int(cx) - 13, int(cy) - 13))
        im.alpha_composite(load(ic), (int(cx) - 8, int(cy) - 8))
    frame_spr = as_img(slot('Normal'))
    # columna derecha: Boca equipada
    bx = PX + 238
    text(im, 'BOCA', bx + 50, PY + 30, C('#ffd27a'))
    im.alpha_composite(frame_spr, (bx + 37, PY + 37)); im.alpha_composite(load('ui/Icons/Icon_Mandibles_16.png'), (bx + 42, PY + 42))
    text(im, 'MANDIBULAS', bx + 50, PY + 72, TEXT)
    text(im, 'NIVEL II', bx + 50, PY + 84, C('#9ec8b4'))
    text(im, 'MORDISCO CORTO', bx + 50, PY + 104, C('#9ec8b4'))
    text(im, '3 DE DANO', bx + 50, PY + 114, C('#9ec8b4'))


def draw_state_tab(im, frame):
    heart = load('ui/HUD/UI_Heart_Full.png')
    x0, y0 = PX + 30, PY + 30
    text(im, 'VIDA', x0 + 14, y0, C('#ffd27a'))
    for i in range(7):
        h = heart if i < 5 else _tint(heart, (240, 200, 90))   # los ganados, en otro color (arte en la subtanda 3)
        im.alpha_composite(h, (x0 + 34 + i * 14, y0 - 8))
    text(im, '+2 EXTRA', x0 + 160, y0, C('#f2d07a'))
    for j, (k, v) in enumerate((('DANO', '2'), ('VELOCIDAD', '7'), ('INESTABLE', '+1/2'))):
        text(im, k, x0 + 30, y0 + 28 + j * 18, TEXT); text(im, v, x0 + 150, y0 + 28 + j * 18, TEXT_HI)
    text(im, 'LINAJE: DEPREDADOR', x0 + 100, y0 + 100, C('#ffd27a'))
    text(im, 'EVOLUCIONADO', x0 + 100, y0 + 112, C('#9ec8b4'))


def draw_items_tab(im, frame):
    x0, y0 = PX + 30, PY + 30
    text(im, 'RELIQUIAS', x0 + 30, y0, C('#ffd27a'))
    im.alpha_composite(as_img(icon_items()), (x0 + 4, y0 + 12)); text(im, 'RAIZ ANCESTRAL', x0 + 70, y0 + 18, TEXT)
    text(im, 'OBJETOS CLAVE', x0 + 40, y0 + 46, C('#ffd27a'))
    text(im, 'ESPORA MORADA', x0 + 66, y0 + 62, TEXT)
    im.alpha_composite(load('ui/HUD/UI_Biomass.png'), (x0 + 6, y0 + 92)); text(im, 'BIOMASA  245', x0 + 56, y0 + 98, TEXT_HI)


def draw_run_tab(im, frame):
    x0, y0 = PX + 30, PY + 30
    for j, (k, v) in enumerate((('PISO', '3 - PANTANO'), ('SEMILLA', '123456'), ('TIEMPO', '24:13'),
                                ('ENEMIGOS', '187'), ('JEFES', '4'))):
        text(im, k, x0 + 30, y0 + j * 18, TEXT); text(im, v, x0 + 170, y0 + j * 18, TEXT_HI)


TABS_DRAW = {'Creature': draw_creature_tab, 'State': draw_state_tab, 'Items': draw_items_tab, 'Run': draw_run_tab}


def _tint(im, rgb):
    im = im.copy(); p = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = p[x, y]
            if a and (r, g, b) != OUTL[:3]:
                lum = (r + g + b) / 765
                p[x, y] = (int(rgb[0] * (0.35 + lum)), int(rgb[1] * (0.35 + lum)), int(rgb[2] * (0.35 + lum)), a)
    return im


if __name__ == '__main__':
    sheets = {
        'UI_PauseMem_Tab_Active': [tab_active()],
        'UI_PauseMem_Tab_Idle': [tab_bud(False)],
        'UI_PauseMem_Tab_Hover': [tab_bud(True)],
        'UI_PauseMem_TabIcon_Creature': [icon_creature()],
        'UI_PauseMem_TabIcon_State': [icon_state()],
        'UI_PauseMem_TabIcon_Items': [icon_items()],
        'UI_PauseMem_TabIcon_Run': [icon_run()],
        'UI_PauseMem_Key_Q': [key('Q', False), key('Q', True)],
        'UI_PauseMem_Key_E': [key('E', False), key('E', True)],
        'UI_PauseMem_Slot_Normal': [slot('Normal')],
        'UI_PauseMem_Slot_Highlighted': [slot('Highlighted')],
        'UI_PauseMem_Callout_Line': [callout_line()],
        'UI_PauseMem_Callout_Dot': [callout_dot(i) for i in range(4)],
        'UI_PauseMem_Lens': [lens()],
        'UI_PauseMem_Lens_Glint': [lens_glint()],
        'UI_PauseMem_Lens_Organelles': [lens_organelles(i) for i in range(8)],
    }
    for name, frames in sheets.items():
        save_sheet(os.path.join(OUT, 'PauseMem', name + '.png'), frames)

    # vista previa: piezas ampliadas + Personaje (pestaña Criatura) a 2x + zoom de las pestañas
    small = {k: v for k, v in sheets.items() if 'Lens' not in k}
    preview(os.path.join(MT, '..', '..', 'tmp_ignore.png') if False else os.path.join(OUT, '_piezas_tmp.png'), small, BG, scale=4)
    with Image.open(os.path.join(OUT, '_piezas_tmp.png')) as _b: board = _b.convert('RGBA')
    os.remove(os.path.join(OUT, '_piezas_tmp.png'))
    lensb = Image.new('RGBA', (112, 112), C('#0b1512'))
    for f in (lens(), lens_organelles(0), lens_glint()): lensb.alpha_composite(as_img(f))
    lensb = lensb.resize((336, 336), Image.NEAREST)
    full = personaje_mockup(0, 0, hover=2)
    mock = full.resize((960, 540), Image.NEAREST)
    zoom = full.crop((PX - 24, PY - 24, PX + 236, PY + 28)).resize((520, 104), Image.NEAREST)
    W = max(board.width + 356, 1500)
    prev = Image.new('RGBA', (W, max(board.height, 346) + 560 + 114), BG)
    prev.alpha_composite(board, (0, 0)); prev.alpha_composite(lensb, (board.width + 10, 5))
    y = max(board.height, 346) + 10
    prev.alpha_composite(mock, (10, y)); prev.alpha_composite(zoom, (980, y))
    others = [personaje_mockup(i, 0).resize((480, 270), Image.NEAREST) for i in (1, 2, 3)]
    for i, o in enumerate(others[:1]): prev.alpha_composite(o.crop((0, 0, 480, 270)).resize((480, 270)), (980, y + 120))
    prev.save(os.path.join(OUT, 'vista_previa_pausa_personaje.png'))

    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    frames, ms = [], []
    for t in range(4):
        for f in range(8):
            frames.append(personaje_mockup(t, f)); ms.append(140)
        frames.append(personaje_mockup(t, 0, key_down='E')); ms.append(160)
    frames = [f.resize((960, 540), Image.NEAREST).convert('RGB') for f in frames]
    frames[0].save(os.path.join(OUT, 'gif', 'Pausa_Personaje.gif'), save_all=True, append_images=frames[1:], duration=ms, loop=0)
    print('Listo en', OUT)
