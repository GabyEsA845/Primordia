"""Salas especiales del piso 2: planos a tamaño real en el formato de RoomTemplate, comprobación y vistas previas.

Uso: python generar_salas_especiales.py

Símbolos (los de RoomTemplate más los nuevos de este catálogo):
  #  pared              .  suelo             ^ v < >  huecos de puerta (2 tiles)
  ~  fosa               o  roca pequeña (1×1, bloquea)     O  obstáculo grande del tema (2×2, bloquea)
  F  hueso del fósil (1×1, bloquea)          S  esponja Vauxia (enemigo fijo)
  C  cofre               H  coral con el corazón morado (2×2)      E  entrada de oleadas (suelo)

Comprobaciones: las de RoomTemplate.Validate (puertas de 2 tiles alineadas, piezas de pared que el arte puede
dibujar, contando fosas y objetos como suelo), fosas de al menos 2×2, obstáculos 2×2 completos y alcance: qué se
llega andando desde las puertas y qué solo con la esquiva (3 u: salta fosas de hasta 2 tiles en línea recta).

Salida en Layouts/ (un .txt por sala), vista_previa_<sala>.png (con el tileset de la franja), catalogo.png
(todas a escala 1) e informe.txt.
"""
import math, os, random
from collections import deque
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
TEMP = os.path.normpath(os.path.join(HERE, '..'))

# ======================================================================================
# Constructor: rejilla con la fila 0 arriba (como el texto)
# ======================================================================================
class Room:
    def __init__(s, w, h, fill='#'):
        s.w, s.h = w, h
        s.g = [[fill] * w for _ in range(h)]

    def rect(s, x0, y0, x1, y1, c):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                s.g[y][x] = c
        return s

    def put(s, x, y, c):
        s.g[y][x] = c
        return s

    def block(s, x, y, c):
        """Objeto de 2×2 con la esquina de arriba a la izquierda en (x, y)."""
        return s.rect(x, y, x + 1, y + 1, c)

    def doors(s, up=None, down=None, left=None, right=None):
        if up is not None: s.put(up, 0, '^').put(up + 1, 0, '^')
        if down is not None: s.put(down, s.h - 1, 'v').put(down + 1, s.h - 1, 'v')
        if left is not None: s.put(0, left, '<').put(0, left + 1, '<')
        if right is not None: s.put(s.w - 1, right, '>').put(s.w - 1, right + 1, '>')
        return s

    def text(s):
        return [''.join(r) for r in s.g]


def basic(w, h):
    """Sala con 3 filas de pared arriba, 1 en los demás lados y el interior de suelo."""
    return Room(w, h).rect(1, 3, w - 2, h - 2, '.')


# ======================================================================================
# Las 10 salas
# ======================================================================================
def parkour():
    """2 casillas en vertical. Se entra por arriba y se sale por abajo; dentro, una fosa con plataformas."""
    r = Room(18, 44).rect(1, 3, 16, 42, '~')
    plats = [(6, 3, 11, 4), (4, 7, 7, 8), (3, 11, 5, 12), (8, 11, 11, 12), (10, 15, 13, 16), (11, 19, 14, 20),
             (6, 19, 8, 20), (5, 23, 8, 24), (3, 27, 6, 28), (5, 31, 8, 32), (7, 35, 10, 36), (3, 39, 14, 42)]
    for x0, y0, x1, y1 in plats:
        r.rect(x0, y0, x1, y1, '.')
    r.put(12, 40, 'C')
    return r.doors(up=8, down=8)


def heart():
    """Abismo, lateral. Doble de enemigos; en el centro, el santuario del corazón rodeado por la fosa."""
    r = basic(34, 22)
    r.rect(1, 3, 3, 4, '#').rect(30, 3, 32, 4, '#')            # esquinas de roca
    r.rect(1, 19, 2, 20, '#').rect(31, 19, 32, 20, '#')
    r.rect(13, 7, 20, 16, '~').rect(15, 9, 18, 14, '.')        # santuario: isla de 4×6 con la fosa de 2 alrededor
    r.block(16, 11, 'H')
    for x0, y0, x1, y1 in [(5, 6, 7, 7), (26, 6, 28, 8), (5, 15, 6, 17), (25, 16, 28, 17), (9, 18, 11, 19), (22, 3, 23, 4)]:
        r.rect(x0, y0, x1, y1, '~')                            # grietas sueltas
    for x, y in [(10, 5), (23, 13), (9, 13)]:
        r.block(x, y, 'O')
    for x, y in [(4, 10), (29, 11), (12, 19), (21, 18), (27, 4)]:
        r.put(x, y, 'o')
    return r.doors(up=16, down=16, left=11, right=11)


def ring():
    """Anillo de la sima: una sima redonda; en el centro, una isla con un cofre a la que se llega esquivando."""
    r = basic(30, 20)
    cx, cy = 14.5, 11
    for y in range(3, 19):
        for x in range(1, 29):
            d = math.hypot((x - cx) / 1.15, y - cy)
            if 2.4 < d <= 4.9:
                r.put(x, y, '~')
    r.put(14, 11, 'C').put(15, 11, 'C')                    # almeja grande: candidata a la sala garantizada
    for x, y in [(4, 5), (24, 5), (4, 16), (24, 16)]:
        r.block(x, y, 'O')
    return r.doors(up=14, down=14, left=10, right=10)


def bridge():
    """Puente roto: una grieta de 3 tiles, demasiado ancha para la esquiva, con dos puentes."""
    r = basic(32, 22)
    r.rect(1, 12, 30, 14, '~').rect(7, 12, 8, 14, '.').rect(23, 12, 24, 14, '.')
    for x, y in [(12, 6), (19, 6), (5, 17), (14, 17), (25, 17)]:
        r.block(x, y, 'O')
    for x, y in [(2, 19), (29, 19), (15, 20), (2, 16), (29, 16)]:
        r.put(x, y, 'E')
    return r.doors(up=15, down=15, left=6, right=6)


def coral_forest():
    """Bosque de coral: columnas de roca de adorno y corales grandes que tapan la vista. Ottoias entre ellos."""
    r = basic(32, 20)
    for x in (6, 15, 24):                                      # columnas de pared de 2×3
        r.rect(x, 6, x + 1, 8, '#').rect(x, 13, x + 1, 15, '#')
    for x, y in [(3, 10), (10, 4), (10, 11), (19, 4), (19, 11), (27, 10), (10, 16), (20, 16), (3, 4), (27, 4)]:
        r.block(x, y, 'O')
    return r.doors(up=15, down=15, left=10, right=10)


def fossil():
    """Fósil gigante: el esqueleto de una criatura enorme; la columna y las costillas son cobertura."""
    r = basic(34, 20)
    for x in range(7, 26):                                     # columna vertebral
        r.put(x, 11, 'F')
    r.rect(25, 9, 28, 13, 'F')                                 # cráneo
    r.put(29, 11, 'F')
    for i, x in enumerate(range(9, 24, 4)):                    # costillas: hacia arriba y hacia abajo, curvadas
        for k in range(1, 4):
            r.put(x - (1 if k == 3 else 0), 11 - k, 'F')
            r.put(x - (1 if k == 3 else 0), 11 + k, 'F')
    for x, y in [(3, 5), (30, 16), (14, 4), (17, 17)]:
        r.put(x, y, 'o')
    return r.doors(up=16, down=16, left=6, right=6)


def sponge_garden():
    """Jardín de esponjas: sala en U con un estanque; 3 esponjas, una en un islote del estanque."""
    r = basic(32, 22)
    r.rect(12, 3, 19, 10, '#')                                 # roca que cuelga del techo y hace la U
    r.rect(11, 13, 20, 18, '~').rect(15, 15, 16, 16, '.')      # estanque con islote
    r.put(15, 15, 'S').put(5, 7, 'S').put(26, 7, 'S')
    for x, y in [(4, 15), (26, 15)]:
        r.block(x, y, 'O')
    r.put(9, 5, 'C').put(10, 5, 'C')                       # almeja grande: candidata a la sala garantizada
    return r.doors(up=5, down=5, left=16, right=16)


def sealed():
    """Sala sellada: amplia y con pocas coberturas; tres oleadas desde las esquinas."""
    r = basic(32, 20)
    r.rect(1, 3, 2, 4, '#').rect(29, 3, 30, 4, '#')
    for x, y in [(8, 7), (22, 7), (8, 13), (22, 13)]:
        r.block(x, y, 'O')
    for x, y in [(3, 5), (28, 5), (2, 17), (29, 17)]:
        r.put(x, y, 'E')
    return r.doors(up=15, down=15, left=10, right=10)


def islands():
    """Islas sobre la fosa unidas por puentes de un tile. Un enemigo que sale de su isla cae."""
    r = Room(34, 22).rect(1, 3, 32, 20, '~')
    r.rect(12, 3, 21, 5, '.').rect(12, 18, 21, 20, '.')        # islas de las puertas de arriba y abajo
    r.rect(1, 9, 6, 14, '.').rect(27, 9, 32, 14, '.')          # islas de los lados
    r.rect(12, 9, 21, 14, '.')                                 # isla central
    r.rect(16, 6, 16, 8, '.').rect(17, 15, 17, 17, '.')        # puentes
    r.rect(7, 11, 11, 11, '.').rect(22, 12, 26, 12, '.')
    r.block(14, 10, 'O').block(18, 12, 'O')
    return r.doors(up=16, down=16, left=11, right=11)


def grotto():
    """Gruta escondida: pequeña, con un laberinto de grietas y un cofre grande en el centro."""
    r = basic(24, 18)
    r.rect(7, 6, 16, 7, '~').rect(7, 12, 16, 13, '~')          # anillo de grietas con una sola entrada (abajo)
    r.rect(7, 8, 8, 11, '~').rect(15, 8, 16, 11, '~')
    r.rect(11, 12, 12, 13, '.')
    r.rect(3, 4, 4, 9, '~').rect(19, 10, 20, 15, '~')          # grietas sueltas que hacen dar la vuelta
    r.put(11, 9, 'C').put(12, 9, 'C')
    return r.doors(up=11, down=11, left=9, right=9)


# (nombre, plano, franja de la vista previa, entrantes máximos del contorno de cueva en tiles)
ROOMS = [
    ('Parkour', parkour, 'Cambrian', (2, 2)),        # de 2 en 2, para no dejar fosas de 1 tile junto a las plataformas
    ('CorazonMorado', heart, 'Abyss', 3),
    ('AnilloSima', ring, 'Shallows', 3),
    ('PuenteRoto', bridge, 'Cambrian', 2),
    ('BosqueCoral', coral_forest, 'Cambrian', 2),
    ('FosilGigante', fossil, 'Shallows', 3),
    ('JardinEsponjas', sponge_garden, 'Cambrian', 2),
    ('SalaSellada', sealed, 'Abyss', 2),
    ('Islas', islands, 'Shallows', 2),
    ('GrutaEscondida', grotto, 'Abyss', 2),
]


def cave_edges(rows, seed, depth):
    step = 1
    if isinstance(depth, tuple): depth, step = depth[0] // depth[1], depth[1]
    """Contorno de cueva: la roca entra en la sala de 0 a `depth` tiles desde cada lado, con un perfil que sube y baja
    de uno en uno, sin picos de 1 tile, y a cero delante de las puertas (quedan cuellos de 6 tiles). Solo se convierte
    en roca el suelo y la fosa; los objetos, las islas y lo que hay dentro no se tocan."""
    g = [list(r) for r in rows]
    h, w = len(g), len(g[0])
    rnd = random.Random(seed)
    doors_x = [x for x in range(w) if rows[0][x] == '^' or rows[h - 1][x] == 'v']
    doors_y = [y for y in range(h) if rows[y][0] == '<' or rows[y][w - 1] == '>']

    def profile(n, door_pos, dmax):
        v, cur, run = [], rnd.randint(0, dmax), 0
        for i in range(n):
            if run >= 2 and rnd.random() < 0.45:
                cur = max(0, min(dmax, cur + rnd.choice((-1, 1)))); run = 0
            v.append(cur); run += 1
        for i in range(n):                               # cuellos delante de las puertas, bajando poco a poco
            dist = min([abs(i - p) for p in door_pos] + [99]) - 2
            v[i] = min(v[i], max(0, dist))
        for i in range(1, n - 1):                        # sin picos de 1 tile
            if v[i] > v[i - 1] and v[i] > v[i + 1]: v[i] = max(v[i - 1], v[i + 1])
        return v

    def carve(x, y):
        if 0 <= x < w and 0 <= y < h and g[y][x] in '.~': g[y][x] = '#'

    top = profile(w, doors_x, depth); bottom = profile(w, doors_x, max(1, depth - 1))
    left = profile(h, doors_y, depth); right = profile(h, doors_y, depth)
    for x in range(1, w - 1):
        for d in range(top[x] * step): carve(x, 3 + d)
        for d in range(bottom[x] * step): carve(x, h - 2 - d)
    for y in range(3, h - 1):
        for d in range(left[y] * step): carve(1 + d, y)
        for d in range(right[y] * step): carve(w - 2 - d, y)
    return [''.join(r) for r in g]


def dash_only(notes):
    return next((int(n.split()[0]) for n in notes if n.endswith('solo se alcanzan esquivando')), 0)


def key_notes(notes):
    """Lo que no puede cambiar al deformar el contorno: qué puertas y objetos se alcanzan y cómo."""
    return sorted(n for n in notes if n.startswith(('la puerta', 'el cofre', 'el corazón', 'una esponja')))

# ======================================================================================
# Comprobación (port de RoomTemplate.Validate y WallRules.Classify)
# ======================================================================================
OPEN_MARKS = set('.~oOFSCHE')        # para las paredes, todo lo que está a ras de suelo cuenta como suelo
WALK = set('.E')
BLOCK = set('oOFSCH')


def kinds(rows):
    """Tipo de cada celda como en FloorMap con las puertas abiertas: wall, floor, passage, gap, arch."""
    h, w = len(rows), len(rows[0])
    k = [['wall'] * w for _ in range(h)]
    for y in range(h):
        for x in range(w):
            c = rows[y][x]
            k[y][x] = 'floor' if c in OPEN_MARKS else 'passage' if c == '^' else 'gap' if c in 'v<>' else 'wall'
    for x in range(w):
        if rows[0][x] == '^':
            y = 1
            while y < h and rows[y][x] == '#':
                k[y][x] = 'arch'; y += 1
    return k


def classify(k, x, y):
    """WallRules.Classify con y hacia abajo (la fila de encima es y - 1)."""
    h, w = len(k), len(k[0])
    get = lambda xx, yy: k[yy][xx] if 0 <= xx < w and 0 <= yy < h else 'wall'
    is_open = lambda xx, yy: get(xx, yy) in ('floor', 'passage')
    kind = get(x, y)
    if kind == 'arch': return 'None'
    if kind == 'gap': return 'Floor'
    if kind in ('floor', 'passage'):
        return 'FloorShadow' if get(x, y - 1) in ('wall', 'arch') else 'Floor'
    up, left, right = is_open(x, y - 1), is_open(x - 1, y), is_open(x + 1, y)
    face_lower = is_open(x, y + 1)
    face_upper = not face_lower and is_open(x, y + 2)
    if face_lower or face_upper:
        if up or (left and right): return 'Missing'
        if face_lower: return 'FaceLowerEndLeft' if left else 'FaceLowerEndRight' if right else 'FaceLower'
        return 'FaceUpperEndLeft' if left else 'FaceUpperEndRight' if right else 'FaceUpper'
    if left and right: return 'Missing'
    if up and left: return 'OuterTopLeft'
    if up and right: return 'OuterTopRight'
    if up: return 'EdgeBottom'
    if left: return 'EdgeRight'
    if right: return 'EdgeLeft'
    is_face = lambda xx, yy: get(xx, yy) == 'wall' and (is_open(xx, yy + 1) or is_open(xx, yy + 2))
    if is_face(x - 1, y): return 'EdgeRight'
    if is_face(x + 1, y): return 'EdgeLeft'
    if is_open(x + 1, y - 1): return 'CornerBottomLeft'
    if is_open(x - 1, y - 1): return 'CornerBottomRight'
    return 'Cap'


def validate(name, rows):
    problems, notes = [], []
    h, w = len(rows), len(rows[0])
    if any(len(r) != w for r in rows): problems.append('filas de distinto ancho')
    cells = lambda m: [(x, y) for y in range(h) for x in range(w) if rows[y][x] == m]
    doors = {m: cells(m) for m in '^v<>'}
    for m, cs in doors.items():
        if cs and len(cs) != 2: problems.append(f'la puerta {m} tiene {len(cs)} celdas')
    if doors['^'] and doors['v'] and min(c[0] for c in doors['^']) != min(c[0] for c in doors['v']):
        problems.append('puertas de arriba y abajo en columnas distintas')
    if doors['<'] and doors['>'] and min(c[1] for c in doors['<']) != min(c[1] for c in doors['>']):
        problems.append('puertas de los lados en filas distintas')
    k = kinds(rows)
    for y in range(h):
        for x in range(w):
            if classify(k, x, y) == 'Missing': problems.append(f'la pared en ({x}, {y}) no se puede dibujar')
    # fosas de al menos 2×2 y objetos grandes completos
    sq = lambda x, y, c: all(0 <= x + dx < w and 0 <= y + dy < h and rows[y + dy][x + dx] == c for dx in (0, 1) for dy in (0, 1))
    for c, what in (('~', 'fosa'), ('O', 'obstáculo grande'), ('H', 'coral del corazón')):
        for x, y in cells(c):
            if not any(sq(x - dx, y - dy, c) for dx in (0, 1) for dy in (0, 1)):
                problems.append(f'{what} en ({x}, {y}) no forma parte de un bloque de 2×2')
    # alcance: andando desde la primera puerta, y añadiendo la esquiva
    door_floor = []
    for m, cs in doors.items():
        for x, y in cs:
            dx, dy = {'^': (0, 1), 'v': (0, -1), '<': (1, 0), '>': (-1, 0)}[m]
            while 0 <= x < w and 0 <= y < h and rows[y][x] not in WALK:
                x += dx; y += dy
            door_floor.append((m, (x, y)))
    walk = lambda c: rows[c[1]][c[0]] in WALK

    def reach(start, dash):
        seen, q = {start}, deque([start])
        while q:
            x, y = q.popleft()
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                n = (x + dx, y + dy)
                if 0 <= n[0] < w and 0 <= n[1] < h and n not in seen and walk(n):
                    seen.add(n); q.append(n)
            if dash:
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)):
                    reach_max = 3 if dx == 0 or dy == 0 else 2      # 3 u en recto, 2 tiles por eje en diagonal
                    for d in range(2, reach_max + 1):
                        n = (x + dx * d, y + dy * d)
                        if not (0 <= n[0] < w and 0 <= n[1] < h): break
                        mid = [rows[y + dy * i][x + dx * i] for i in range(1, d)]
                        if any(m_ not in '~.E' for m_ in mid): break
                        if walk(n) and n not in seen:
                            seen.add(n); q.append(n)
        return seen

    if door_floor:
        start = door_floor[0][1]
        by_walk, by_dash = reach(start, False), reach(start, True)
        all_walk = {(x, y) for y in range(h) for x in range(w) if rows[y][x] in WALK}
        for m, c in door_floor[1:]:
            if c not in by_walk:
                notes.append(f'la puerta {m} solo se alcanza esquivando' if c in by_dash else f'la puerta {m} NO se alcanza')
                if c not in by_dash: problems.append(f'la puerta {m} no se alcanza')
        dash_only = all_walk & by_dash - by_walk
        lost = all_walk - by_dash
        if dash_only: notes.append(f'{len(dash_only)} tiles de suelo solo se alcanzan esquivando')
        if lost: problems.append(f'{len(lost)} tiles de suelo no se alcanzan nunca: {sorted(lost)[:4]}…')
        for c, what in (('C', 'el cofre'), ('H', 'el corazón'), ('S', 'una esponja')):
            for x, y in cells(c):
                near = [(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))]
                if any(n in by_walk for n in near): continue
                if any(n in by_dash for n in near): notes.append(f'{what} en ({x}, {y}) solo se alcanza esquivando')
                else: problems.append(f'{what} en ({x}, {y}) no se alcanza')
    return problems, notes


# ======================================================================================
# Vista previa con el tileset de la franja
# ======================================================================================
T = 16


def load_theme(theme):
    t = {}
    for folder in (os.path.join(TEMP, 'environment', theme, 'tiles'), os.path.join(TEMP, 'environment-shapes', theme, 'tiles')):
        if os.path.isdir(folder):
            for f in os.listdir(folder):
                if f.endswith('.png'): t[f[:-4]] = Image.open(os.path.join(folder, f)).convert('RGBA')
    deco = os.path.join(TEMP, 'environment', theme, 'deco')
    big = {'Shallows': 'Obstacle_Archaeocyath', 'Cambrian': 'Obstacle_CoralBig', 'Abyss': 'Obstacle_Spire'}[theme]
    t['O'] = Image.open(os.path.join(deco, big + '.png')).convert('RGBA')
    t['o'] = Image.open(os.path.join(deco, 'Obstacle_Rock.png')).convert('RGBA')
    pits = os.path.join(TEMP, 'environment', theme, 'pits')
    t['pits'] = {f[:-4]: Image.open(os.path.join(pits, f)).convert('RGBA') for f in os.listdir(pits) if f.endswith('.png')}
    pieces = os.path.join(HERE, 'Pieces')
    first = lambda f, w, h: Image.open(os.path.join(pieces, f)).convert('RGBA').crop((0, 0, w, h))
    t['C'], t['CC'] = first('Clam_Small_Idle.png', 16, 16), first('Clam_Big_Idle.png', 32, 24)
    t['H'], t['E'] = first('HeartCoral_Idle.png', 32, 32), first('WaveVent_Idle.png', 16, 16)
    t['fossil'] = Image.open(os.path.join(pieces, 'Fossil_Skeleton.png')).convert('RGBA')
    t['motes'] = first(os.path.join('..', '..', 'environment', theme, 'pits', 'Pit_Motes.png'), 16, 16)
    t['deco'] = [Image.open(os.path.join(deco, f)).convert('RGBA').crop((0, 0, 16, 16)) for f in sorted(os.listdir(deco))
                 if f.startswith('Deco_') and Image.open(os.path.join(deco, f)).height == 16]
    sp = Image.open(os.path.join(TEMP, 'floor2-enemies', 'Vauxia', 'Vauxia_Idle.png')).convert('RGBA')
    t['S'] = sp.crop((0, 0, sp.height, sp.height))
    return t


TILE_NAME = {'FloorShadow': 'Floor_ShadowTop_{v2}', 'Cap': 'Wall_Cap', 'FaceUpper': 'Wall_FaceUpper_{v2}', 'FaceLower': 'Wall_FaceLower_{v2}',
             'FaceUpperEndLeft': 'Wall_FaceUpper_EndLeft', 'FaceUpperEndRight': 'Wall_FaceUpper_EndRight',
             'FaceLowerEndLeft': 'Wall_FaceLower_EndLeft', 'FaceLowerEndRight': 'Wall_FaceLower_EndRight',
             'EdgeLeft': 'Wall_EdgeLeft', 'EdgeRight': 'Wall_EdgeRight', 'EdgeBottom': 'Wall_EdgeBottom',
             'OuterTopLeft': 'Wall_OuterTopLeft', 'OuterTopRight': 'Wall_OuterTopRight',
             'CornerBottomLeft': 'Wall_CornerBL', 'CornerBottomRight': 'Wall_CornerBR'}


def placeholder(kind):
    """Piezas que aún no tienen arte: hueso, cofre, coral del corazón, entrada de oleadas."""
    im = Image.new('RGBA', (32 if kind == 'H' else 16,) * 2, (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    if kind == 'F':
        d.rounded_rectangle([2, 4, 13, 12], 4, fill=(232, 220, 192), outline=(90, 80, 60))
        d.line([5, 8, 10, 8], fill=(200, 186, 150))
    elif kind == 'C':
        d.rectangle([2, 5, 13, 13], fill=(170, 110, 50), outline=(60, 36, 20))
        d.rectangle([2, 5, 13, 7], fill=(242, 193, 78), outline=(60, 36, 20))
        d.rectangle([7, 8, 8, 10], fill=(255, 240, 160))
    elif kind == 'H':
        for i, (x, y) in enumerate([(6, 22), (12, 14), (19, 20), (25, 12)]):
            d.ellipse([x - 4, y - 8, x + 4, y + 8], fill=(200, 90, 150), outline=(70, 20, 60))
        d.polygon([(16, 13), (11, 7), (11, 4), (14, 2), (16, 4), (18, 2), (21, 4), (21, 7)], fill=(190, 110, 255), outline=(60, 20, 90))
    elif kind == 'E':
        d.ellipse([2, 2, 13, 13], outline=(255, 111, 128), width=2)
        d.line([8, 4, 8, 11], fill=(255, 111, 128), width=2)
    return im


def render(rows, theme, seed=1):
    h, w = len(rows), len(rows[0])
    t = load_theme(theme)
    k = kinds(rows)
    rnd = random.Random(seed)
    floors = [n for n in t if n.startswith('Floor_') and n[6:].isdigit()]
    im = Image.new('RGBA', (w * T, h * T), (0, 0, 0, 255))
    face = t['Wall_FaceLower_0']
    for y in range(h):
        for x in range(w):
            c = rows[y][x]
            piece = classify(k, x, y)
            if c == '~':                                   # fosa: tile según qué vecinos son suelo, y esquinas de dentro
                pit_at = lambda xx, yy: 0 <= xx < w and 0 <= yy < h and rows[yy][xx] == '~'
                mask = (0 if pit_at(x, y - 1) else 1) | (0 if pit_at(x + 1, y) else 2) | (0 if pit_at(x, y + 1) else 4) | (0 if pit_at(x - 1, y) else 8)
                im.alpha_composite(t['pits'][f'Pit_{mask}'], (x * T, y * T))
                for name_, (dx, dy) in (('NE', (1, -1)), ('NO', (-1, -1)), ('SE', (1, 1)), ('SO', (-1, 1))):
                    if pit_at(x + dx, y) and pit_at(x, y + dy) and not pit_at(x + dx, y + dy):
                        im.alpha_composite(t['pits'][f'Pit_Corner_{name_}'], (x * T, y * T))
                continue
            if piece == 'None':
                name = 'Floor_0'
            elif piece == 'Floor':
                name = rnd.choice(floors) if rnd.random() < 0.3 else 'Floor_0'
            else:
                name = TILE_NAME[piece].format(v2=(x * 7 + y * 3) % 2)
            im.alpha_composite(t.get(name, t['Floor_0']), (x * T, y * T))
    # decoración del tema en el suelo libre (como hace el juego), lejos de las puertas y de los objetos
    for y in range(h):
        for x in range(w):
            near_door = any(rows[yy][xx] in '^v<>' for yy in range(max(0, y - 2), min(h, y + 3)) for xx in range(max(0, x - 2), min(w, x + 3)))
            if rows[y][x] == '.' and k[y][x] == 'floor' and classify(k, x, y) == 'Floor' and not near_door and rnd.random() < 0.07:
                im.alpha_composite(rnd.choice(t['deco']), (x * T, y * T))
            if rows[y][x] == '~' and rnd.random() < 0.12:
                im.alpha_composite(t['motes'], (x * T, y * T))
    # objetos encima del suelo
    done = set()
    if any('F' in r for r in rows):
        im.alpha_composite(t['fossil'], (0, 0))
    for y in range(h):
        for x in range(w):
            c = rows[y][x]
            if (x, y) in done: continue
            if c == 'O':
                for dx in (0, 1):
                    for dy in (0, 1): done.add((x + dx, y + dy))
                im.alpha_composite(t['O'], (x * T, y * T))
            elif c == 'H':
                for dx in (0, 1):
                    for dy in (0, 1): done.add((x + dx, y + dy))
                im.alpha_composite(t['H'], (x * T, y * T))
            elif c == 'C' and x + 1 < w and rows[y][x + 1] == 'C':
                done.add((x + 1, y))
                im.alpha_composite(t['CC'], (x * T, y * T - 8))
            elif c == 'C':
                im.alpha_composite(t['C'], (x * T, y * T))
            elif c == 'E':
                im.alpha_composite(t['E'], (x * T, y * T))
            elif c == 'o':
                im.alpha_composite(t['o'], (x * T, y * T))
            elif c == 'S':
                im.alpha_composite(t['S'], (x * T - 8, y * T - 16))
    return im


# ======================================================================================
if __name__ == '__main__':
    os.makedirs(os.path.join(HERE, 'Layouts'), exist_ok=True)
    report, images = [], []
    for name, make, theme, depth in ROOMS:
        base = make().text()
        base_raw = validate(name, base)[1]
        base_notes = key_notes(base_raw)
        rows = base
        for seed in range(1, 400):                       # primera semilla que respeta las reglas y el alcance
            cand = cave_edges(base, seed * 101 + len(name), depth)
            pr, nt = validate(name, cand)
            if not pr and key_notes(nt) == base_notes and dash_only(nt) <= dash_only(base_raw):
                rows = cand; break
        else:
            print(f'{name}: ninguna semilla valió, se queda sin contorno de cueva')
        with open(os.path.join(HERE, 'Layouts', f'Room_Special_{name}.txt'), 'w', encoding='utf-8') as f:
            f.write('\n'.join(rows) + '\n')
        problems, notes = validate(name, rows)
        report.append(f'{name} ({len(rows[0])}×{len(rows)}, {theme})')
        report += [f'  ERROR: {p}' for p in problems] + [f'  nota: {n}' for n in notes]
        if not problems: report.append('  correcta')
        im = render(rows, theme)
        im.resize((im.width * 2, im.height * 2), Image.NEAREST).save(os.path.join(HERE, f'vista_previa_{name}.png'))
        images.append((name, im))
    with open(os.path.join(HERE, 'informe.txt'), 'w', encoding='utf-8') as f:
        f.write('\n'.join(report) + '\n')
    # catálogo: todas a escala 1, en dos columnas
    pad = 24
    col_w = max(im.width for _, im in images)
    left = [im for i, (_, im) in enumerate(images) if i % 2 == 0]
    right = [im for i, (_, im) in enumerate(images) if i % 2 == 1]
    H = max(sum(im.height + pad for im in left), sum(im.height + pad for im in right)) + pad
    cat = Image.new('RGBA', (col_w * 2 + pad * 3, H), (10, 15, 28, 255))
    d = ImageDraw.Draw(cat)
    for col, lst in ((0, list(enumerate(images))[0::2]), (1, list(enumerate(images))[1::2])):
        y = pad
        for _, (name, im) in lst:
            x = pad + col * (col_w + pad)
            d.text((x, y - 12), name, fill=(190, 230, 255))
            cat.alpha_composite(im, (x, y))
            y += im.height + pad
    cat.save(os.path.join(HERE, 'catalogo.png'))
    print('\n'.join(report))
