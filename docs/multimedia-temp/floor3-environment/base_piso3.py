"""Base común del arte del piso 3 de Primordia (pantano carbonífero).

Paletas aprobadas en la hoja de estilo (floor3-style/, tanda 0) y utilidades de dibujo que comparten los
generadores del piso: ruido que encaja en mosaico, trama, frondas, ramas quemadas, troncos de Lepidodendron,
raíces, charcos con forma y llamas. Importa la biblioteca de la skill (pixel.py).
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
from pixel import _font

BG = C('#1c2e20')                         # fondo de revisión del piso 3 (musgo oscuro)

# =================================================================================================
# Paletas (las de floor3-style/generar_hoja_estilo.py, aprobadas el 2026-10-28)
# =================================================================================================
# Regla del piso: el escenario va apagado y oscuro; lo que importa para jugar va saturado y claro.
TRAMOS = {
    'Swamp': dict(                                    # 1. pantano verde
        nombre='Pantano verde',
        floor=['#1c2e20', '#213626', '#273f2b', '#2f4a32'],
        fleck=['#3d5e36', '#557a3e', '#7fa04a'],
        face=['#1e1a1c', '#2e2622', '#40332a', '#584634', '#76603f'],
        cap=['#14231a', '#1b3022', '#24402a', '#2f5232'],
        vein=['#3f6a2e', '#68973c', '#a2c85a'],
        leaf=['#1a3420', '#264a2a', '#386634', '#548440'],
        bark=['#1e2420', '#2e3830', '#435040', '#5e6c52', '#8a9670'],
        root=['#1e1612', '#33241a', '#4c3828', '#6a4e34', '#8e6e48'],
        seal=['#3f6a2e', '#68973c', '#a2c85a', '#e0f8a0'],    # nudo de las puertas: musgo que brilla
    ),
    'Smoke': dict(                                    # 2. bosque humeante
        nombre='Bosque humeante',
        floor=['#22261f', '#292d24', '#30342a', '#3a3d31'],
        fleck=['#55553f', '#77704f', '#a0946a'],
        face=['#1c1819', '#29221f', '#382e27', '#4a3d30', '#62513c'],
        cap=['#1a1c18', '#22261f', '#2c3127', '#373c2f'],
        vein=['#5a4a2a', '#8a6a34', '#c09a4a'],
        leaf=['#2a2818', '#433d1e', '#635726', '#857232'],
        bark=['#1c1e1a', '#2a2e26', '#3c4234', '#525a44', '#707a5a'],
        root=['#1c1614', '#2e241c', '#463628', '#5e4a36', '#7c6448'],
        seal=['#8a6a34', '#c09a4a', '#e8c870', '#fff0b8'],    # resina
        smoke=True,
    ),
    'Burnt': dict(                                    # 3. tierra quemada
        nombre='Tierra quemada',
        floor=['#1d1a1d', '#242024', '#2c272a', '#363033'],
        fleck=['#4a4346', '#6d6466', '#958a88'],
        face=['#141116', '#1e191d', '#2a2326', '#3a3032', '#4c3f3e'],
        cap=['#151316', '#1c191c', '#242024', '#2d282b'],
        vein=['#a8341c', '#e8682a', '#ffb24a'],
        leaf=['#141114', '#221c1e', '#352b2a', '#4c3e38'],
        bark=['#141114', '#221c1e', '#352b2a', '#4c3e38', '#6a5848'],
        root=['#141114', '#221c1e', '#352b2a', '#4c3e38', '#6a5848'],
        seal=['#a8341c', '#e8682a', '#ffb24a', '#ffe8a0'],    # ascuas
        burnt=True,
    ),
}

BRUSH = ['#3a3018', '#5c4c24', '#86703a', '#b49a52', '#dcc47a']   # maleza quemable: reseca y clara
MUD = ['#22160f', '#33221a', '#4a3324', '#634830', '#86683f']      # lodo profundo (te hunde)
SLUDGE = ['#3a2a1c', '#55402a', '#735a3a', '#9a7e52']              # charco de lodo (ralentiza)
WATER = ['#16302e', '#1d4440', '#2a5e54', '#4e8a72', '#9cc8a0']    # agua de charca, turbia
FERN = ['#1c3a20', '#2a5a2a', '#3e8a34', '#6abf48', '#b8e878']     # helecho alto
FERN_DRY = ['#3a2e16', '#5e4a20', '#8a6e2a', '#b8963a', '#e0c870'] # helecho reseco
CHAR = ['#141114', '#221c1e', '#352b2a', '#4c3e38', '#6a5848']     # carbón
BARK = ['#1e2420', '#2e3830', '#435040', '#5e6c52', '#8a9670']     # corteza de Lepidodendron
STROB = ['#2a1a3a', '#4a2a6a', '#7046a0', '#a07ad0', '#d8c0f0']    # estróbilos (conos-cofre)
POISON = ['#2e5a14', '#5aa020', '#a8e040', '#e8ffc0']
METHANE = ['#1e5a78', '#2a90b8', '#6ad0ec', '#d0f8ff']
FIRE = ['#8a1e14', '#d4441c', '#f88c2a', '#ffd25a', '#fff4c0']
TOXIC = ['#2a2a12', '#4e4e1a', '#7a7a28', '#b0b03a', '#e4e478']
AMBER = ['#6a2e0e', '#b0581a', '#e89a30', '#ffcc5a', '#fff0b0']
EMBER = ['#5a1a14', '#a8341c', '#e8682a', '#ffb24a']
PARASITE = ['#3a0e1e', '#6a1630', '#9a2044', '#d04a62', '#f4a0a4']
PASSAGE = C('#0e1210')                    # paso oscuro detrás de la maleza y de los arcos

def tram(key):
    """Tramo con sus rampas ya convertidas a color."""
    t = {k: ([C(c) for c in v] if isinstance(v, list) else v) for k, v in TRAMOS[key].items()}
    t['key'] = key
    t.setdefault('burnt', False); t.setdefault('smoke', False)
    return t

# =================================================================================================
# Ruido, trama y mezcla
# =================================================================================================
def pnoise(seed, period=16, cell=8):
    """Ruido de valor que se repite cada `period` píxeles en x y en y (encaja en mosaico). cell divide a period."""
    n = period // cell
    rnd = random.Random(seed)
    g = [[rnd.random() for _ in range(n)] for _ in range(n)]
    def f(x, y):
        fx, fy = x / cell, y / cell
        x0, y0 = math.floor(fx), math.floor(fy)
        tx, ty = fx - x0, fy - y0
        tx, ty = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
        a = g[y0 % n][x0 % n] * (1 - tx) + g[y0 % n][(x0 + 1) % n] * tx
        b = g[(y0 + 1) % n][x0 % n] * (1 - tx) + g[(y0 + 1) % n][(x0 + 1) % n] * tx
        return a * (1 - ty) + b * ty
    return f

def noise(seed):
    """Ruido de valor sin repetición (piezas sueltas)."""
    rnd = random.Random(seed)
    g = [[rnd.random() for _ in range(33)] for _ in range(33)]
    def f(x, y, s=8.0):
        fx, fy = x / s, y / s
        x0, y0 = int(fx) % 32, int(fy) % 32
        tx, ty = fx - int(fx), fy - int(fy)
        tx, ty = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
        a = g[y0][x0] * (1 - tx) + g[y0][x0 + 1] * tx
        b = g[y0 + 1][x0] * (1 - tx) + g[y0 + 1][x0 + 1] * tx
        return a * (1 - ty) + b * ty
    return f

BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]
def dither(v, x, y):
    """v continuo -> índice entero con trama Bayer 4×4 entre tonos vecinos."""
    base = math.floor(v); frac = v - base
    return base + (1 if frac > (BAYER[y % 4][x % 4] + 0.5) / 16 else 0)

def blend(under, over, a):
    if under is None: return over[:3] + (a,)
    k = a / 255
    return tuple(round(under[j] * (1 - k) + over[j] * k) for j in range(3)) + (255,)

def darker(c, k=.8): return tuple(int(v * k) for v in c[:3]) + (c[3] if len(c) > 3 else 255,)

N4 = ((1, 0), (-1, 0), (0, 1), (0, -1))

class WrapCanvas(Canvas):
    """Lienzo que se da la vuelta en x y/o en y: lo que sale por un lado entra por el otro, así el tile encaja
    consigo mismo. `clip` (x0, y0, x1, y1) recorta lo que se dibuja (piezas que no deben tocar los bordes)."""
    def __init__(s, w, h=None, wrapx=True, wrapy=False):
        super().__init__(w, h); s.wx, s.wy = wrapx, wrapy; s.clip = None
    def put(s, x, y, c):
        x, y = int(math.floor(x)), int(math.floor(y))
        if s.clip and not (s.clip[0] <= x < s.clip[2] and s.clip[1] <= y < s.clip[3]): return
        if s.wx: x %= s.w
        if s.wy: y %= s.h
        if s._in(x, y): s.px[(x, y)] = C(c) if isinstance(c, str) else c

def copy_canvas(cv, cls=None):
    out = (cls or Canvas)(cv.w, cv.h)
    if isinstance(cv, WrapCanvas) and isinstance(out, WrapCanvas): out.wx, out.wy = cv.wx, cv.wy
    out.px = dict(cv.px); return out

def crop(cv, x0, y0, w, h):
    out = Canvas(w, h)
    for (x, y), c in cv.px.items():
        if x0 <= x < x0 + w and y0 <= y < y0 + h: out.px[(x - x0, y - y0)] = c
    return out

def paste(cv, src, ox, oy):
    """Pega un Canvas o una imagen encima (respeta la transparencia, sin mezclar)."""
    if isinstance(src, Canvas): items = src.px.items()
    else: items = (((x, y), src.getpixel((x, y))) for y in range(src.height) for x in range(src.width))
    for (x, y), c in items:
        if c[3] > 0: cv.put(ox + x, oy + y, c)

# =================================================================================================
# Plantas
# =================================================================================================
def frondlet(cv, x, y, ang, L, pal, droop=.3, lw=2.5, box=None, shade=0):
    """Fronda pequeña: nervio con foliolos. ang en radianes (0 = derecha, pi/2 = abajo). box recorta lo que se
    sale; shade oscurece (las de detrás). pal: 4 tonos."""
    def put(px, py, c):
        if box and not (box[0] <= px < box[2] and box[1] <= py < box[3]): return
        cv.put(px, py, c)
    pts = [(x + math.cos(ang) * st, y + math.sin(ang) * st + (st / L) ** 2 * L * droop, st / L) for st in range(int(L))]
    for j, (px, py, t) in enumerate(pts):
        q = pts[min(j + 1, len(pts) - 1)]
        nx, ny = q[0] - px, q[1] - py
        nn = math.hypot(nx, ny) or 1; nx, ny = nx / nn, ny / nn
        if j % 2 == 0 and t < .92:
            w = 1 + round((1 - t) * lw)
            for sgn in (-1, 1):
                ox_, oy_ = -ny * sgn, nx * sgn
                lit = (ox_ * -.6 + oy_ * -.8) > 0
                for k in range(1, w + 1):
                    tone = (3 if lit else 1) if k < w else (2 if lit else 0)
                    put(px + (ox_ * .85 + nx * .5) * k, py + (oy_ * .85 + ny * .5) * k, pal[max(0, min(3, tone - shade))])
        put(px, py, pal[max(0, 2 - shade)])

def twig(cv, x, y, ang, L, pal, box=None, ember=False, rnd=None):
    """Rama carbonizada con una horquilla (tierra quemada)."""
    def put(px, py, c):
        if box and not (box[0] <= px < box[2] and box[1] <= py < box[3]): return
        cv.put(px, py, c)
    for st in range(int(L)):
        put(x + math.cos(ang) * st, y + math.sin(ang) * st, pal[2] if st % 3 else pal[3])
        put(x + math.cos(ang) * st, y + math.sin(ang) * st + 1, pal[0])
    fx, fy = x + math.cos(ang) * L * .5, y + math.sin(ang) * L * .5
    for st in range(int(L * .45)):
        put(fx + math.cos(ang - .7) * st, fy + math.sin(ang - .7) * st, pal[2])
    if ember and rnd and rnd.random() < .5:
        put(x + math.cos(ang) * (L - 1), y + math.sin(ang) * (L - 1), C(EMBER[2]))

def bark_trunk(cv, tx, w, y0, y1, bark, ember=None, seed=0, flare=True, ember_p=.1):
    """Tronco de Lepidodendron: cojinetes en rombo (filas alternas desplazadas) con la cicatriz en medio, luz a la
    izquierda y pie ensanchado. ember: las juntas brillan (tronco carbonizado)."""
    rnd = random.Random(seed)
    cw, chh = 6, 7
    for y in range(y0, y1):
        fl = 1 if flare and y > y1 - 4 else 0
        row = (y - y0) // chh
        for x in range(tx - fl, tx + w + fl):
            u = (x - tx + .5) / w
            lx = (x - tx + (cw // 2 if row % 2 else 0)) % cw - (cw - 1) / 2
            ly = (y - y0) % chh - (chh - 1) / 2
            dd = abs(lx) / (cw / 2) + abs(ly) / (chh / 2)
            light = 4 if u < .22 else 3 if u < .45 else 2 if u < .8 else 1
            if x == tx - fl or x == tx + w - 1 + fl: c = bark[0]
            elif dd > .95: c = C(ember) if ember and rnd.random() < ember_p else bark[max(0, light - 2)]
            elif dd < .25 and u > .2: c = bark[max(0, light - 2)]
            else: c = bark[light] if dd < .7 else bark[max(1, light - 1)]
            cv.put(x, y, c)

def bezier(p0, p1, p2, p3, n):
    out = []
    for i in range(n + 1):
        t = i / n; a = 1 - t
        out.append((a ** 3 * p0[0] + 3 * a * a * t * p1[0] + 3 * a * t * t * p2[0] + t ** 3 * p3[0],
                    a ** 3 * p0[1] + 3 * a * a * t * p1[1] + 3 * a * t * t * p2[1] + t ** 3 * p3[1]))
    return out

def root(cv, ctrl, w0, w1, pal, grow=1.0, box=None, knots=True):
    """Raíz retorcida por una curva de Bézier (4 puntos), de grosor w0 a w1, sombreada como un cilindro con la luz
    arriba a la izquierda. grow 0..1 dibuja solo el principio (la raíz crece). pal: 5 tonos."""
    pts = bezier(*ctrl, n=60)
    last = max(0, int(len(pts) * grow))
    for i in range(last):
        x, y = pts[i]; q = pts[min(i + 1, len(pts) - 1)]
        dx, dy = q[0] - x, q[1] - y; dn = math.hypot(dx, dy) or 1
        nx, ny = -dy / dn, dx / dn                    # normal
        if nx * -.6 + ny * -.8 < 0: nx, ny = -nx, -ny  # que apunte hacia la luz
        r = (w0 + (w1 - w0) * i / len(pts)) / 2
        if i > last - 4: r *= (last - i) / 4 + .2     # punta que se afila al crecer
        for yy in range(int(y - r - 1), int(y + r + 2)):
            for xx in range(int(x - r - 1), int(x + r + 2)):
                ox_, oy_ = xx + .5 - x, yy + .5 - y
                d = math.hypot(ox_, oy_)
                if d > r + .25: continue
                if box and not (box[0] <= xx < box[2] and box[1] <= yy < box[3]): continue
                s = (ox_ * nx + oy_ * ny) / max(r, .5)   # 1 = lado iluminado, -1 = en sombra
                tone = 4 if s > .55 else 3 if s > .05 else 2 if s > -.5 else 1
                if r < 1.1: tone = 2 if s >= 0 else 1
                cv.put(xx, yy, pal[tone])
        if knots and i % 17 == 9 and r > 1.2:         # anillos de la corteza
            cv.put(x - nx * r * .3, y - ny * r * .3, pal[1])

def flame(cv, x, y, rnd, h=None, small=False):
    h = h or rnd.randrange(3, 6)
    for k in range(h):
        c = C(FIRE[4]) if k == 0 else C(FIRE[3]) if k < 2 else C(FIRE[2]) if k < h - 1 else C(FIRE[1])
        cv.put(x, y - k, c)
        if not small and k < h - 2: cv.put(x + (1 if k % 2 else -1), y - k, C(FIRE[2]))

# =================================================================================================
# Charcos con forma (lodo, agua, veneno)
# =================================================================================================
SQ = 1.6                                   # aplastamiento vertical de lo que está en el suelo
SHAPES = ('redondo', 'alargado', 'lobulado', 'cadena', 'isla', 'salpicado', 'rinon')

def blob(seed, shape, w, h):
    """Máscara orgánica de w×h: metabolas aplastadas en vertical con el borde movido por ruido."""
    rnd = random.Random(seed); n = noise(seed + 5)
    cx, cy = w / 2, h / 2
    R = min(w, h * SQ) / 2
    balls, holes = [], []
    if shape == 'redondo':
        balls = [(cx, cy, R * .74), (cx + rnd.uniform(-.25, .25) * R, cy + rnd.uniform(-.1, .1) * h, R * .55)]
    elif shape == 'alargado':
        balls = [(w * (.2 + .6 * i / 3), cy + rnd.uniform(-.1, .1) * h, R * rnd.uniform(.5, .62)) for i in range(4)]
    elif shape == 'lobulado':
        a0 = rnd.uniform(0, 6.28)
        balls = [(cx, cy, R * .5)] + [(cx + math.cos(a0 + k * 1.26) * w * .26, cy + math.sin(a0 + k * 1.26) * h * .24,
                                       R * rnd.uniform(.3, .4)) for k in range(5)]
    elif shape == 'cadena':
        ph = rnd.uniform(0, 6)
        balls = [(w * (.14 + .72 * i / 4), cy + math.sin(i * 1.9 + ph) * h * .2, R * rnd.uniform(.3, .4)) for i in range(5)]
    elif shape == 'isla':
        balls = [(cx, cy, R * .78), (cx + R * .35, cy, R * .58), (cx - R * .35, cy, R * .58)]
        holes = [(cx + rnd.uniform(-2, 2), cy - 1, R * .3)]
    elif shape == 'salpicado':
        balls = [(cx - w * .08, cy, R * .55), (cx + w * .1, cy + h * .06, R * .42)]
        for k in range(5):
            a = rnd.uniform(0, 6.28)
            balls.append((cx + math.cos(a) * w * .4, cy + math.sin(a) * h * .38, rnd.uniform(1.4, 2.2)))
    elif shape == 'rinon':
        for i in range(5):
            a = math.pi * (.1 + .8 * i / 4)
            balls.append((cx - math.cos(a) * w * .33, cy + h * .18 - math.sin(a) * h * .34, R * .4))
    mask = set()
    for y in range(h):
        for x in range(w):
            px, py = x + .5, y + .5
            f = sum(r * r / max(.01, (px - bx) ** 2 + ((py - by) * SQ) ** 2) for bx, by, r in balls)
            f *= 1 + (n(x, y, 3) - .5) * .5
            if f > 1 and not any(math.hypot(px - hx, (py - hy) * SQ) < hr for hx, hy, hr in holes):
                mask.add((x, y))
    return mask
