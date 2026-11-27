"""Menú de pausa nuevo, estilo membrana (subtanda 3): corazones extra en ámbar, iconos fósiles de la pestaña
Partida, cuadro de información con pseudópodo y franja de fila; maquetas de Estado, Objetos y Partida.

Reutiliza las subtandas 1 y 2 (generar_pausa_membrana.py y generar_pausa_personaje.py, en esta carpeta).
Hecho con la skill primordia-sprites (pixel.py): luz arriba a la izquierda, contorno OUTL.

Uso: python generar_pausa_detalles.py [carpeta_salida]
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import generar_pausa_personaje as P
from generar_pausa_personaje import *

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__))
BONE = ['#6a5a40', '#a8946c', '#d8c8a0', '#f4ecd4']
MUD = ['#3a2a1c', '#5a4430', '#7a6046', '#9a7c5a']


# --- corazones extra: los del HUD teñidos de ámbar (misma forma; 16×16) --------------------------------
AMBER_MAP = {(200, 56, 74): '#c86a1c', (240, 112, 110): '#f2a83a', (255, 208, 192): '#ffe0a0',
             (255, 240, 224): '#fff6d8', (110, 40, 56): '#5a2e10'}

def amber_heart(kind, sparkle=0):
    src = load('ui/HUD/UI_Heart_%s.png' % kind)
    cv = Canvas(16)
    for y in range(16):
        for x in range(16):
            r, g, b, a = src.getpixel((x, y))
            if not a: continue
            c = AMBER_MAP.get((r, g, b))
            cv.put(x, y, A(c, a) if c else (r, g, b, a))
    if sparkle:                                                    # destello en estrella arriba a la derecha
        sx, sy = 11, 4
        arm = 1 if sparkle == 1 else 2
        cv.put(sx, sy, SPARK[0])
        for i in range(1, arm + 1):
            for dx, dy in ((i, 0), (-i, 0), (0, i), (0, -i)):
                cv.put(sx + dx, sy + dy, SPARK[1] if i == 1 else A(SPARK[2], 200))
    return cv

HEART_EXTRA_FULL = [amber_heart('Full', s) for s in (0, 0, 0, 0, 1, 2, 1, 0)]   # bucle de 8; el destello sale cada tanto


# --- iconos fósiles de la pestaña Partida (12×12; el tiempo usa el reloj de la pestaña) ---------------
def _out(cv):
    cv.outline(); cv.clean(); return cv

def icon_floor():
    """Piso: corte de estratos con una concha fósil."""
    cv = Canvas(12)
    bands = [(1, 3, SAND), (4, 6, MUD), (7, 10, STONE4)]
    for y0, y1, rmp in bands:
        for y in range(y0, y1 + 1):
            for x in range(1, 11):
                wave = int(round(math.sin(x * 0.9 + y0) * 0.6))
                if y == y0 and wave > 0 and x % 3 == 0: continue
                f = 2 if y == y0 else 1 if y < y1 else 0
                if x == 1: f = min(3, f + 1)
                cv.put(x, y, A(rmp[f]))
    _out(cv)
    for x, y in ((6, 5), (7, 5), (7, 4)): cv.put(x, y, A(BONE[3]))   # concha
    cv.put(3, 8, A(STONE4[3])); cv.put(9, 2, A(SAND[3]))
    return cv

def icon_seed():
    """Semilla: semilla parda con un brote verde."""
    cv = Canvas(12)
    rot_ellipse(cv, 6, 7.5, 3.6, 2.6, math.radians(-35), SAND)
    _out(cv)
    cv.put_in(5, 6, A(SAND[3])); cv.put_in(7, 9, A(SAND[0]))
    for x, y in ((8, 4), (8, 3), (9, 2), (10, 2), (7, 2)):          # brote
        cv.put(x, y, A(GREEN[2] if y > 2 or x == 9 else GREEN[3]))
    return cv

FOE = ['#3a1e4a', '#6a3a8a', '#9a62b8', '#d0a8e8']     # morado: lejos del verde del jugador y del rojo de la vida

def _angry_eyes(cv, cx, cy):
    """Ojos enfadados de 3×2: el párpado de dentro baja en diagonal y la pupila mira al centro."""
    for s_ in (-1, 1):
        cols = [cx + s_ * k for k in (3, 2, 1)]                      # de fuera a dentro
        cv.put(cols[0], cy, EYE); cv.put(cols[1], cy, EYE); cv.put(cols[2], cy, OUTL)
        cv.put(cols[0], cy + 1, EYE); cv.put(cols[1], cy + 1, EYE); cv.put(cols[2], cy + 1, OUTL)
        cv.put(cols[2], cy - 1, OUTL)                                # ceja

def icon_enemies():
    """Enemigos: un bicho con los ojos enfadados y dos pinchos."""
    cv = Canvas(12)
    ellipse(cv, 6.5, 7, 5.0, 3.9, FOE)
    for x, y in ((3, 3), (9, 3)): cv.put(x, y, A(FOE[1]))               # pinchos
    _out(cv)
    _angry_eyes(cv, 6.5, 6)
    return cv

def icon_bosses():
    """Jefes: el mismo bicho con una corona de ámbar."""
    cv = Canvas(12)
    ellipse(cv, 6.5, 8, 5.0, 3.2, FOE)
    for x in range(3, 10): cv.put(x, 4, A(AMBER[2]))                    # base de la corona
    for x, y in ((3, 3), (3, 2), (6, 3), (6, 2), (6, 1), (9, 3), (9, 2)): cv.put(x, y, A(AMBER[2] if y > 1 else AMBER[3]))
    _out(cv)
    cv.put(4, 4, A(AMBER[3])); cv.put(6, 4, A('#e05a5a'))               # brillo y gema
    _angry_eyes(cv, 6.5, 7)
    return cv

def icon_unstable():
    """Inestable: corazón con una grieta que brilla en ámbar."""
    cv = Canvas(12)
    for j, row in enumerate(P.HEART_SHAPE):
        for i, ch in enumerate(row):
            if ch == '#':
                f = 3 if i + j < 4 else 2 if i + j < 8 else 1 if i + j < 11 else 0
                cv.put(1 + i, 2 + j, A(P.HEART[f]))
    _out(cv)
    for x, y in ((5, 3), (5, 4), (4, 5), (5, 6), (6, 7), (5, 8)): cv.put(x, y, A(AMBER[3] if y in (5, 6) else AMBER[2]))
    cv.put(2, 3, A(P.HEART[3]))
    return cv

RUN_ICONS = {'Floor': icon_floor, 'Seed': icon_seed, 'Enemies': icon_enemies, 'Bosses': icon_bosses}


# --- cuadro de información (9-slice 32×32, borde 10, Tiled) y pseudópodo (12×14) --------------------------
TIP_W, TIP_R, TIP_B = 32, 7, 10
TIP_FILL = A(FILL, 235)

def _layers(k, l, x, y):
    if k == 0: return OUTL
    if k == 1: return A(MEM[3] if l > 0 else MEM[2] if l == 0 else MEM[1])
    if k == 2: return A(MEM[1])
    if k == 3: return A(MEM[2] if l >= 0 else MEM[1]) if (x + y) % 2 == 0 else A(MEM[1], 225)
    return TIP_FILL

def tooltip():
    cv = Canvas(TIP_W)
    for y in range(TIP_W):
        for x in range(TIP_W):
            d, n = rr(x, y, TIP_W, TIP_W, TIP_R)
            if d > 0: cv.put(x, y, _layers(int(d), lit(n), x, y))
    vesicle(cv, 28.0, 3.8, 2.0)
    return cv

POD_W, POD_H, POD_EDGE = 12, 14, 8      # el borde izquierdo del cuadro cae en la columna 8 (el pseudópodo lo pisa 4 px)

def pod():
    """Saliente blando que apunta a la izquierda y se funde con el borde del cuadro (como la yema de la pestaña)."""
    cv = Canvas(POD_W, POD_H)
    def lobe(x, y):
        return (1 - math.hypot((x + 0.5 - 4.2) / 3.8, (y + 0.5 - 7.0) / 3.6)) * 3.6   # bulbo de la punta
    def edge(x, y):
        return (x + 0.5) - POD_EDGE
    def field(x, y):
        return smax(lobe(x, y), edge(x, y), 4.5)                     # cuello ancho con remate cóncavo
    for y in range(POD_H):
        for x in range(POD_W):
            d = field(x, y)
            if d <= 0: continue
            if x >= POD_EDGE and d - edge(x, y) < 0.05: continue        # ahí manda el cuadro
            gx, gy = field(x + 1, y) - field(x - 1, y), field(x, y + 1) - field(x, y - 1)
            g = math.hypot(gx, gy) or 1
            cv.put(x, y, _layers(int(d), lit((-gx / g, -gy / g)), x, y))
    return cv


# --- franja de fila (9-slice 24×14, bordes L6 R6, Sliced) ------------------------------------------------
def row_strip():
    cv = Canvas(24, 14)
    for y in range(14):
        for x in range(24):
            d, n = rr(x, y, 24, 14, 6)
            if d <= 0: continue
            k = int(d)
            if k == 0: c = A(MEM[2], 120 if n[1] < 0.3 else 90)
            elif y == 1: c = A(MEM[2], 95)
            else: c = A(MEM[1], 80)
            cv.put(x, y, c)
    return cv


# --- maquetas -----------------------------------------------------------------------------------------
def rows(im, x0, y0, w, items, value_x=None):
    """items = [(icono o None, etiqueta, valor, color del valor)]."""
    strip = as_img(row_strip())
    for j, (ic, label, value, vc) in enumerate(items):
        y = y0 + j * 17
        im.alpha_composite(slice9(strip, w, 14, 6, 0, 6, 0, tiled=False), (x0, y))
        if ic is not None: im.alpha_composite(as_img(ic) if not isinstance(ic, Image.Image) else ic, (x0 + 4, y + 1))
        _left(im, label, x0 + 20, y + 7, TEXT)
        _right(im, value, x0 + w - 6, y + 7, vc or TEXT_HI)


def _measure(s, bold=False):
    d = ImageDraw.Draw(Image.new('RGBA', (1, 1))); d.fontmode = '1'
    x0, _, x1, _ = d.textbbox((0, 0), s, font=M.font(bold)); return x1 - x0

def _left(im, s, x, cy, color, bold=False):
    text(im, s, x + _measure(s, bold) / 2, cy, color, bold)

def _right(im, s, x, cy, color, bold=False):
    text(im, s, x - _measure(s, bold) / 2, cy, color, bold)


def draw_state_tab(im, frame):
    x0, y0 = PX + 22, PY + 24
    _left(im, 'VIDA', x0, y0 + 7, C('#ffd27a'))
    full, half = load('ui/HUD/UI_Heart_Full.png'), load('ui/HUD/UI_Heart_Half.png')
    for i in range(5): im.alpha_composite(full, (x0 + 34 + i * 15, y0))
    im.alpha_composite(as_img(HEART_EXTRA_FULL[frame % 8]), (x0 + 34 + 5 * 15, y0))
    im.alpha_composite(as_img(amber_heart('Half')), (x0 + 34 + 6 * 15, y0))
    _left(im, '+2 EXTRA', x0 + 34 + 7 * 15 + 6, y0 + 7, C('#f2c24e'))
    stat = lambda n: load('menus/Pause/UI_Stat_%s.png' % n)
    rows(im, x0, y0 + 26, 150, [(stat('Damage'), 'DANO', '2', None), (stat('Speed'), 'VELOCIDAD', '7', None),
                                (icon_unstable(), 'INESTABLE', '+1/2', C('#f2c24e'))])
    lx = x0 + 176
    _left(im, 'LINAJE', lx, y0 + 33, C('#ffd27a'))
    _left(im, 'DEPREDADOR', lx, y0 + 50, TEXT_HI)
    _left(im, 'EVOLUCIONADO', lx, y0 + 62, C('#9ec8b4'))
    _left(im, 'MORDISCO +1', lx, y0 + 80, C('#9ec8b4'))


def draw_items_tab(im, frame):
    x0, y0 = PX + 22, PY + 24
    _left(im, 'RELIQUIAS', x0, y0 + 4, C('#ffd27a'))
    rows(im, x0, y0 + 12, 200, [(icon_items(), 'RAIZ ANCESTRAL', '', None)])
    _left(im, 'OBJETOS CLAVE', x0, y0 + 46, C('#ffd27a'))
    rows(im, x0, y0 + 54, 200, [(icon_seed(), 'ESPORA MORADA', '', None)])
    _left(im, 'BIOMASA', x0, y0 + 88, C('#ffd27a'))
    rows(im, x0, y0 + 96, 200, [(load('ui/HUD/UI_Biomass.png'), 'BIOMASA', '245', None)])
    # cuadro de información al pasar por la reliquia
    _tooltip_at(im, x0 + 206, y0 + 19, 'RAIZ ANCESTRAL', 'RELIQUIA', '#e0a040', ['LAS RAICES TE CURAN', '1/2 AL LIMPIAR SALA'])


def draw_run_tab(im, frame):
    x0, y0 = PX + 22, PY + 24
    rows(im, x0, y0, 220, [(icon_floor(), 'PISO', '3  PANTANO', None), (icon_seed(), 'SEMILLA', '123456', None),
                           (icon_run(), 'TIEMPO', '24:13', None), (icon_enemies(), 'ENEMIGOS', '187', None),
                           (icon_bosses(), 'JEFES', '4', None)])


def _tooltip_at(im, px, cy, title_, kind, stripe_color, lines):
    """Cuadro con el pseudópodo apuntando a (px, cy)."""
    w, h = 118, 44 + 10 * len(lines)
    bx, by = px + POD_W - POD_EDGE - 4 + 4, cy - 12
    im.alpha_composite(slice9(tooltip(), w, h, TIP_B, TIP_B, TIP_B, TIP_B), (bx, by))
    im.alpha_composite(as_img(pod()), (bx - POD_EDGE, cy - POD_H // 2))
    stripe = load('ui/Panels/UI_Card_Stripe.png')
    sc = C(stripe_color)
    st = slice9(stripe, w - 14, stripe.height, 4, 0, 4, 0, tiled=False)
    p = st.load()
    for yy in range(st.height):
        for xx in range(st.width):
            r, g, b, a = p[xx, yy]
            if a: p[xx, yy] = (r * sc[0] // 255, g * sc[1] // 255, b * sc[2] // 255, a)
    im.alpha_composite(st, (bx + 7, by + 6))
    _left(im, title_, bx + 8, by + 16, TEXT_HI)
    _left(im, kind, bx + 8, by + 26, C(stripe_color))
    for j, ln in enumerate(lines): _left(im, ln, bx + 8, by + 38 + j * 10, C('#9ec8b4'))


_orig_creature = P.draw_creature_tab
def draw_creature_tab(im, frame):
    _orig_creature(im, frame)
    lx, ly = PX + 120, PY + 100
    _tooltip_at(im, lx - 75 + 13, int(ly - 72 * 0.92), 'MANDIBULAS', 'BOCA', '#e05a5a', ['MORDISCO CORTO', '3 DE DANO'])

P.TABS_DRAW.update({'Creature': draw_creature_tab, 'State': draw_state_tab, 'Items': draw_items_tab, 'Run': draw_run_tab})


if __name__ == '__main__':
    sheets = {
        'UI_PauseMem_Heart_Extra_Full': HEART_EXTRA_FULL,
        'UI_PauseMem_Heart_Extra_Half': [amber_heart('Half')],
        'UI_PauseMem_Heart_Extra_Empty': [amber_heart('Empty')],
        'UI_PauseMem_RunIcon_Floor': [icon_floor()],
        'UI_PauseMem_RunIcon_Seed': [icon_seed()],
        'UI_PauseMem_RunIcon_Enemies': [icon_enemies()],
        'UI_PauseMem_RunIcon_Bosses': [icon_bosses()],
        'UI_PauseMem_StatIcon_Unstable': [icon_unstable()],
        'UI_PauseMem_Tooltip': [tooltip()],
        'UI_PauseMem_Tooltip_Pod': [pod()],
        'UI_PauseMem_Row': [row_strip()],
    }
    for name, frames in sheets.items():
        save_sheet(os.path.join(OUT, 'PauseMem', name + '.png'), frames)

    preview(os.path.join(OUT, '_piezas3_tmp.png'), sheets, BG, scale=5)
    with Image.open(os.path.join(OUT, '_piezas3_tmp.png')) as _b: board = _b.convert('RGBA')
    os.remove(os.path.join(OUT, '_piezas3_tmp.png'))
    shots = [personaje_mockup(t, 4).resize((720, 405), Image.NEAREST) for t in range(4)]
    prev = Image.new('RGBA', (max(board.width, 1460), board.height + 830), BG)
    prev.alpha_composite(board, (0, 0))
    for i, s in enumerate(shots): prev.alpha_composite(s, (10 + (i % 2) * 730, board.height + 10 + (i // 2) * 415))
    prev.save(os.path.join(OUT, 'vista_previa_pausa_detalles.png'))

    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    frames = [personaje_mockup(1, f).resize((960, 540), Image.NEAREST).convert('RGB') for f in range(8)]
    frames[0].save(os.path.join(OUT, 'gif', 'Pausa_Estado.gif'), save_all=True, append_images=frames[1:], duration=150, loop=0)
    print('Listo en', OUT)
