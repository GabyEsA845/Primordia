"""
generar_mapa.py - Primordia: minimapa del HUD y mapa grande (TAB).

Reutiliza generar_menus.py y generar_ui.py (que cargan generar_mutaciones.py
y generar_jugador.py): Cv, contorno #181425, rampas, el 9-slice de membrana,
el vidrio de la lente de la pausa, UI_Pause_Title y la fuente de la maqueta.

El juego pinta el plano (1 píxel por casilla) con UI_Map_Palette.png; aquí
solo van los marcos, el fondo, la paleta y los iconos. La vista previa monta
un plano de ejemplo con esa misma regla.

Uso:
  python generar_mapa.py [carpeta_multimedia_temp] [carpeta_salida]
Por defecto: la carpeta de arriba (donde están player/, mutations/, ui/,
menus/, environment/ y enemies/) y la carpeta de este script.
"""
import importlib.util
import math
import os
import sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, '..')
OUT = sys.argv[2] if len(sys.argv) > 2 else HERE

# --------------------------------------------- cargar los otros generadores
_argv = sys.argv
sys.argv = ['generar_menus.py', ROOT, os.devnull]
_spec = importlib.util.spec_from_file_location('generar_menus', os.path.join(ROOT, 'menus', 'generar_menus.py'))
G = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(G)
sys.argv = _argv
U, M = G.U, G.M

C, CA, OUTL = U.C, U.CA, U.OUTL
Cv, ell, col, nine = U.Cv, U.ell, U.col, U.nine
shade_v, ramp_idx = U.shade_v, U.ramp_idx
MEM, GLASS = U.MEM, G.GLASS
W, H = 480, 270

# ============================================================ 3. PALETA
# Orden fijo: el juego lee el color de la casilla por su x en la tira de 8x1.
PALETTE = [
    ('Background', '#0d221d'),      # agua oscura, más honda que el suelo #16352e
    ('Wall', '#9ad8bc'),            # contorno claro de las salas visitadas
    ('Floor', '#2e6652'),           # suelo visitado y limpio
    ('FloorCurrent', '#7cc98e'),    # sala actual: verde claro, se ve a la primera
    ('FloorUncleared', '#a34a5e'),  # enemigos vivos: rosa apagado (el jefe es magenta vivo)
    ('Corridor', '#4a8a72'),        # pasillos y puertas: entre el suelo y el contorno
    ('Unexplored', '#1a3a32'),      # silueta de las vecinas sin visitar
    ('UnexploredWall', '#2b5446'),  # su contorno, un poco más claro
]
PAL = {k: C(v) for k, v in PALETTE}


def map_palette():
    im = Image.new('RGBA', (8, 1))
    for i, (_, c) in enumerate(PALETTE):
        im.putpixel((i, 0), C(c))
    return im


# ======================================================== 1. MINIMAPA (HUD)
def minimap_frame():
    """Membrana de 4 px (contorno, 2 px de membrana con luz arriba a la izquierda,
    contorno interior) con el interior transparente. 9-slice, borde 6."""
    n = 32
    cv = Cv(n)
    rad = 5.0
    for y in range(n):
        for x in range(n):
            ax, ay = min(x + .5, n - x - .5), min(y + .5, n - y - .5)
            if ax < rad and ay < rad:
                d = rad - math.hypot(rad - ax, rad - ay)
            else:
                d = min(ax, ay)
            if d < 0.5 or min(ax, ay) > 4:
                continue
            top, left = (y + .5) < n / 2, (x + .5) < n / 2
            lit = (top and ay <= ax) or (left and ax < ay)
            if d < 1.5:
                cv.put(x, y, OUTL)
            elif min(ax, ay) > 3:                         # contorno interior (borde del recuadro)
                cv.put(x, y, OUTL)
            elif d < 2.5:
                cv.put(x, y, MEM[3] if lit else MEM[1])
            else:
                cv.put(x, y, MEM[2] if lit else MEM[0])
    # detalles solo en las esquinas (no se estiran): burbujita y brillo
    for (bx, by) in ((2, 2), (n - 3, 2), (2, n - 3), (n - 3, n - 3)):
        cv.put(bx, by, MEM[3] if (bx, by) == (2, 2) else MEM[2])
    cv.put(1, 3, '#e4ffd8')
    cv.put(3, 1, '#e4ffd8')
    return cv


def minimap_back():
    """Agua oscura al ~80 % detrás del plano. 9-slice, borde 2."""
    bg = PAL['Background']
    im = Image.new('RGBA', (16, 16), bg[:3] + (204,))
    return im


# ======================================================== 2. MAPA GRANDE
def map_panel():
    """Portaobjetos / placa de Petri vista desde arriba: vidrio grueso con brillo
    y el interior liso del color Background. 9-slice, borde 12."""
    n = 48
    cv = Cv(n)
    rad = 9.0
    bg = PAL['Background']
    for y in range(n):
        for x in range(n):
            ax, ay = min(x + .5, n - x - .5), min(y + .5, n - y - .5)
            if ax < rad and ay < rad:
                d = rad - math.hypot(rad - ax, rad - ay)
            else:
                d = min(ax, ay)
            if d < 0.5:
                continue
            top, left = (y + .5) < n / 2, (x + .5) < n / 2
            lit = (top and ay <= ax) or (left and ax < ay)
            if d < 1.5:
                c = OUTL
            elif d < 2.5:                                 # canto del vidrio
                c = C(GLASS[3]) if lit else C(GLASS[1])
            elif d < 5.5:                                 # grosor del vidrio
                c = col(GLASS[2] if lit else GLASS[1], 235)
            elif d < 6.5:
                c = C(GLASS[1]) if lit else C(GLASS[0])
            elif d < 7.5:                                 # labio interior, en sombra arriba a la izquierda
                c = C(GLASS[0]) if lit else C(GLASS[1])
            elif d < 8.5:
                c = OUTL
            else:
                c = bg
            cv.put(x, y, c)
    # esquinas: burbujitas atrapadas en el vidrio y un destello
    for (bx, by, c) in ((4, 5, GLASS[3]), (5, 4, GLASS[3]), (n - 5, 4, GLASS[2]), (4, n - 5, GLASS[2]),
                        (n - 5, n - 5, GLASS[2]), (n - 4, n - 6, GLASS[3])):
        cv.put(bx, by, c)
    cv.put(3, 3, '#ffffff')
    return cv


def map_glint():
    """Reflejo del vidrio encima del mapa: franja curva en la esquina superior
    izquierda que sigue, muy fina, por el borde de arriba y el de la izquierda.
    9-slice, borde 12, centro transparente."""
    n = 48
    im = Image.new('RGBA', (n, n), (0, 0, 0, 0))
    px = im.load()
    cx = cy = 13.5
    for y in range(12):
        for x in range(12):
            d = math.hypot(x + .5 - cx, y + .5 - cy)
            if x + .5 < cx and y + .5 < cy:
                if 7.6 <= d < 9.4:                         # franja principal (2 px)
                    px[x, y] = (230, 255, 248, 150)
                elif 5.4 <= d < 6.4:                       # segunda franja, más corta
                    ang = math.degrees(math.atan2(cy - (y + .5), cx - (x + .5)))
                    if 22 < ang < 68:
                        px[x, y] = (230, 255, 248, 90)
    for x in range(12, n - 12):                            # continuación por arriba (se estira)
        px[x, 5] = (230, 255, 248, 70)
    for y in range(12, n - 12):                            # y por la izquierda
        px[5, y] = (230, 255, 248, 70)
    px[3, 9], px[9, 3] = (255, 255, 255, 150), (255, 255, 255, 150)
    return im


# ============================================================ 4. ICONOS
GREEN = ['#2f7d4f', '#4fb85f', '#8be36a', '#d6ff9a']       # la célula del jugador
VIRUS = ['#7a2a5a', '#c8487e', '#ff7aa8', '#ffd0e0']       # cuerpo del virus jefe
KNOB = ['#6a7a20', '#a8c040', '#d8f070', '#f8ffc0']        # bolitas de las púas
GOLD = ['#8a5e14', '#d09a2a', '#f8d460', '#fff6d0']        # burbuja de recompensa


def map_player(f):
    """Célula verde vista desde arriba que late (4 cuadros, 7x7, centro 3,3)."""
    cv = Cv(7)
    r = (2.45, 2.75, 2.95, 2.75)[f]
    ell(cv, 3.5, 3.5, r, r, GREEN, light=(0.0, 0.1, 0.2, 0.1)[f])
    cv.put(2, 2, '#ffffff')
    if f == 2:
        cv.put(3, 2, GREEN[3])
    cv.put(4, 4, GREEN[0])                                 # núcleo
    cv.outline()
    return cv


def map_boss():
    """El virus: esfera magenta con púas de bolita lima (9x9, centro 4,4)."""
    cv = Cv(9)
    for k in range(8):                                     # púas
        a = k * math.pi / 4 + math.pi / 8
        ex, ey = 4.5 + math.cos(a) * 3.4, 4.5 + math.sin(a) * 3.4
        cv.put(ex - .5, ey - .5, KNOB[2] if ey < 4.5 else KNOB[1])
    ell(cv, 4.5, 4.5, 2.6, 2.6, VIRUS)
    cv.put(3, 3, '#ffffff')
    cv.put(5, 5, VIRUS[0])
    cv.outline()
    return cv


def map_reward():
    """Burbuja de mutación: membrana dorada redonda, interior translúcido y núcleo
    brillante (7x7, centro 3,3)."""
    cv = Cv(7)
    inner = '#7a5418'
    rows = ['..mmm..',
            '.mhiim.',
            '.mincM.',
            '.miiiM.',
            '..MMM..']
    for j, row in enumerate(rows):
        for i, ch in enumerate(row):
            c = {'m': GOLD[2], 'M': GOLD[1], 'h': '#ffffff', 'i': inner, 'n': GOLD[3], 'c': GOLD[2]}.get(ch)
            if c:
                cv.put(i, j + 1, c)
    cv.put(3, 3, '#fffbe8')                                # núcleo brillante
    cv.outline()
    return cv


# ============================================================ GUARDAR
SHEETS = G.SHEETS
SHEETS.clear()


def save(name, frames, ms=None):
    G.OUT = OUT
    G.save('.', name, frames, ms)


def build():
    save('UI_Minimap_Frame', [minimap_frame()])
    save('UI_Minimap_Back', [minimap_back()])
    save('UI_Map_Panel', [map_panel()])
    save('UI_Map_Glint', [map_glint()])
    save('UI_Map_Palette', [map_palette()])
    save('UI_Map_Player', [map_player(f) for f in range(4)], 150)
    save('UI_Map_Boss', [map_boss()])
    save('UI_Map_Reward', [map_reward()])


def first(name):
    return SHEETS[name][5][0]


# ===================================================== PLANO DE EJEMPLO
# Cada sala ocupa una celda de la rejilla de 28x16 casillas; entre celdas hay
# 6 casillas de pasillo de 4 de ancho. Las formas incluyen la pared (anillo de 1).
CW, CH, GAP = 28, 16, 6


def shape(kind):
    """Conjunto de casillas (x, y) de la sala, pared incluida, en su celda de 28x16."""
    s = set()
    for y in range(CH):
        for x in range(CW):
            if kind == 'rect':
                ok = True
            elif kind == 'small':
                ok = 4 <= x < 24 and 2 <= y < 14
            elif kind == 'cross':
                ok = (4 <= y < 12) or (7 <= x < 21)
            elif kind == 'L':
                ok = not (x >= 16 and y >= 11)
            elif kind == 'columns':
                ok = True
            elif kind == 'boss':
                ok = (min(x, CW - 1 - x) + min(y, CH - 1 - y)) >= 3
            else:
                raise ValueError(kind)
            if ok:
                s.add((x, y))
    return s


PILLARS = {(x, y) for px_ in (7, 19) for py_ in (5, 9) for x in (px_, px_ + 1) for y in (py_, py_ + 1)}


def paint_floor(rooms, links, current, cols, rows):
    """Devuelve una imagen (1 px por casilla) con la paleta, como la pintará el juego.
    rooms: {nombre: (col, fila, forma, estado)}; estado: 'visited', 'uncleared', 'unexplored'.
    links: [(a, b)] salas unidas por pasillo."""
    w, h = cols * (CW + GAP) - GAP, rows * (CH + GAP) - GAP
    grid = {}
    floor_of = {}
    for name, (c, r, kind, state) in rooms.items():
        ox, oy = c * (CW + GAP), r * (CH + GAP)
        sh = shape(kind)
        fl = set()
        for (x, y) in sh:
            wall = any((x + dx, y + dy) not in sh for dx in (-1, 0, 1) for dy in (-1, 0, 1))
            if kind == 'columns' and (x, y) in PILLARS:
                wall = True
            if state == 'unexplored':
                k = 'UnexploredWall' if wall else 'Unexplored'
            else:
                k = 'Wall' if wall else ('FloorCurrent' if name == current else
                                         'FloorUncleared' if state == 'uncleared' else 'Floor')
            grid[(ox + x, oy + y)] = k
            if not wall:
                fl.add((ox + x, oy + y))
        floor_of[name] = fl
    for a, b in links:
        ca, ra = rooms[a][:2]
        cb, rb = rooms[b][:2]
        both = rooms[a][3] != 'unexplored' and rooms[b][3] != 'unexplored'
        if ra == rb:                                       # pasillo horizontal
            r = ra
            y0 = r * (CH + GAP) + CH // 2 - 2
            x0 = min(ca, cb) * (CW + GAP) + CW // 2
            x1 = max(ca, cb) * (CW + GAP) + CW // 2
            for y in range(y0, y0 + 4):
                xs = range(x0, x1)
                for x in xs:
                    p = (x, y)
                    if p in floor_of[a] or p in floor_of[b]:
                        continue
                    k = grid.get(p)
                    if k is None:                          # tramo entre salas
                        grid[p] = 'Corridor' if both else 'Unexplored'
                    elif k in ('Wall', 'UnexploredWall'):  # hueco de puerta
                        grid[p] = 'Corridor' if k == 'Wall' else 'Unexplored'
        else:                                              # pasillo vertical
            c = ca
            x0 = c * (CW + GAP) + CW // 2 - 2
            y0 = min(ra, rb) * (CH + GAP) + CH // 2
            y1 = max(ra, rb) * (CH + GAP) + CH // 2
            for x in range(x0, x0 + 4):
                for y in range(y0, y1):
                    p = (x, y)
                    if p in floor_of[a] or p in floor_of[b]:
                        continue
                    k = grid.get(p)
                    if k is None:
                        grid[p] = 'Corridor' if both else 'Unexplored'
                    elif k in ('Wall', 'UnexploredWall'):
                        grid[p] = 'Corridor' if k == 'Wall' else 'Unexplored'
    im = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    for (x, y), k in grid.items():
        if 0 <= x < w and 0 <= y < h:
            im.putpixel((x, y), PAL[k])
    return im


def room_center(rooms, name):
    c, r = rooms[name][:2]
    return c * (CW + GAP) + CW // 2, r * (CH + GAP) + CH // 2


# ============================================================ VISTA PREVIA
def hud_screen():
    scr = U.floor_bg(W, H, walls=True)
    player = M.carapace('down').img()
    scr.alpha_composite(player, (240 - 12, 150 - 12))
    for i, k in enumerate(('full', 'full', 'half', 'empty')):
        scr.alpha_composite(U.heart(k).img(), (8 + i * 17, 8))
    for i, (s, m) in enumerate((('Mouth', 'Mandibles'), ('Limbs', None), ('Skin', 'Carapace'), ('Organ', None))):
        x, y = 8 + i * 22, 28
        scr.alpha_composite(U.slot_frame().img(), (x, y))
        scr.alpha_composite(U.ICONS[m](16).img() if m else U.slot_empty(s).img(), (x + 2, y + 2))
    # minimapa: 88x68 arriba a la derecha (80x60 de interior), centrado en el jugador
    fx, fy = W - 8 - 88, 8
    ix, iy = fx + 4, fy + 4
    rooms = {
        'Cruz': (0, 1, 'cross', 'visited'),
        'Actual': (1, 1, 'rect', 'visited'),
        'Vecina': (2, 1, 'small', 'unexplored'),
        'Abajo': (1, 2, 'rect', 'uncleared'),
        'Arriba': (1, 0, 'rect', 'visited'),
    }
    links = [('Cruz', 'Actual'), ('Actual', 'Vecina'), ('Actual', 'Abajo')]
    # 'Arriba' no está unida ni se ve: queda fuera de la niebla
    del rooms['Arriba']
    plan = paint_floor(rooms, links, 'Actual', 3, 3)
    px_, py_ = room_center(rooms, 'Actual')
    px_, py_ = px_ + 5, py_ + 2                            # el jugador, algo a la derecha del centro
    scr.alpha_composite(U.nine_render(first('UI_Minimap_Back'), 80, 60, 2), (ix, iy))
    big = Image.new('RGBA', (plan.width + 160, plan.height + 120), (0, 0, 0, 0))
    big.alpha_composite(plan, (80, 60))
    view = big.crop((80 + px_ - 40, 60 + py_ - 30, 80 + px_ + 40, 60 + py_ + 30))
    scr.alpha_composite(view, (ix, iy))
    scr.alpha_composite(SHEETS['UI_Map_Player'][5][1], (ix + 40 - 3, iy + 30 - 3))
    scr.alpha_composite(U.nine_render(first('UI_Minimap_Frame'), 88, 68, 6), (fx, fy))
    # el contador de biomasa baja bajo el minimapa
    scr.alpha_composite(U.ui_biomass().img(), (fx + 56, fy + 72))
    G.ptext(scr, (fx + 70, fy + 75), 'X 42')
    return scr


def big_map_screen():
    scr = U.floor_bg(W, H, walls=True)
    scr.alpha_composite(M.carapace('down').img(), (240 - 12, 150 - 12))
    scr.alpha_composite(Image.new('RGBA', (W, H), (0, 0, 0, 191)))       # partida al 75 %
    pw, ph = 400, 220
    px0, py0 = (W - pw) // 2, (H - ph) // 2 + 6
    scr.alpha_composite(U.nine_render(first('UI_Map_Panel'), pw, ph, 12), (px0, py0))
    rooms = {
        'Inicio': (0, 1, 'rect', 'visited'),
        'Cruce': (1, 1, 'cross', 'visited'),
        'Lateral1': (1, 0, 'columns', 'visited'),
        'Premio': (1, 2, 'L', 'visited'),
        'Lateral2': (0, 2, 'small', 'uncleared'),
        'Actual': (2, 2, 'rect', 'visited'),
        'Jefe': (3, 2, 'boss', 'unexplored'),
        'Vecina1': (2, 1, 'small', 'unexplored'),
        'Vecina2': (2, 3, 'rect', 'unexplored'),
    }
    links = [('Inicio', 'Cruce'), ('Cruce', 'Lateral1'), ('Cruce', 'Premio'), ('Premio', 'Lateral2'),
             ('Premio', 'Actual'), ('Actual', 'Jefe'), ('Actual', 'Vecina1'), ('Actual', 'Vecina2')]
    plan = paint_floor(rooms, links, 'Actual', 4, 4)
    s = 2 if plan.width * 2 <= pw - 24 and plan.height * 2 <= ph - 24 else 1
    big = plan.resize((plan.width * s, plan.height * s), Image.NEAREST)
    mx, my = px0 + (pw - big.width) // 2, py0 + (ph - big.height) // 2 + 2
    scr.alpha_composite(big, (mx, my))

    def at(name, dx=0, dy=0):
        x, y = room_center(rooms, name)
        return mx + (x + dx) * s, my + (y + dy) * s

    x, y = at('Jefe')
    scr.alpha_composite(first('UI_Map_Boss'), (x - 4, y - 4))
    x, y = at('Premio', -2, -1)
    scr.alpha_composite(first('UI_Map_Reward'), (x - 3, y - 3))
    x, y = at('Actual', -6, 3)
    scr.alpha_composite(SHEETS['UI_Map_Player'][5][2], (x - 3, y - 3))
    scr.alpha_composite(U.nine_render(first('UI_Map_Glint'), pw, ph, 12), (px0, py0))
    t = U.nine_render(G.pause_title().img(), 84, 16, (8, 0, 8, 0))
    scr.alpha_composite(t, (W // 2 - 42, py0 - 9))
    G.ptext(scr, (W // 2 - G.tw('MAPA', 2) // 2, py0 - 6), 'MAPA', (240, 255, 235), scale=2)
    return scr, s


def legibility_strip():
    """Comprobación: cada icono sobre cada color de la paleta, y el marco sobre el suelo."""
    scr = U.floor_bg(W, 90)
    for i, (name, c) in enumerate(PALETTE):
        x = 8 + i * 40
        sw = Image.new('RGBA', (36, 30), C(c))
        scr.alpha_composite(sw, (x, 8))
        scr.alpha_composite(SHEETS['UI_Map_Player'][5][2], (x + 2, 12))
        scr.alpha_composite(first('UI_Map_Boss'), (x + 13, 11))
        scr.alpha_composite(first('UI_Map_Reward'), (x + 26, 12))
        # a 1 px por casilla: un trozo de sala de ese color
        G.ptext(scr, (x, 42), str(i), (200, 230, 215))
    # marco del minimapa sobre el suelo oscuro (con el fondo semitransparente)
    fx, fy = 340, 6
    scr.alpha_composite(U.nine_render(first('UI_Minimap_Back'), 80, 60, 2), (fx + 4, fy + 4))
    scr.alpha_composite(U.nine_render(first('UI_Minimap_Frame'), 88, 68, 6), (fx, fy))
    return scr


def preview(path):
    S = 3
    big, s = big_map_screen()
    shots = [('1. HUD EN PARTIDA', hud_screen()), ('2. MAPA GRANDE (TAB), PLANO X%d' % s, big),
             ('3. LECTURA: ICONOS SOBRE CADA COLOR DE LA PALETA (0-7) Y MARCO SOBRE EL SUELO', legibility_strip())]
    lab = 24
    out = Image.new('RGBA', (W * S + 20, sum(im.height * S + lab + 10 for _, im in shots) + 10), U.BG)
    y = 10
    for name, im in shots:
        G.ptext(out, (12, y + 6), name, (214, 255, 154), scale=2)
        y += lab
        out.alpha_composite(im.resize((im.width * S, im.height * S), Image.NEAREST), (10, y))
        y += im.height * S + 10
    out.convert('RGB').save(path)


def main():
    build()
    preview(os.path.join(OUT, 'vista_previa_mapa.png'))
    for k, v in SHEETS.items():
        print(k, *v[1:5])


if __name__ == '__main__':
    main()
