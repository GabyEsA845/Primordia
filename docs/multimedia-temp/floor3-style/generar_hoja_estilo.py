"""Hoja de estilo del piso 3 de Primordia: pantano carbonífero.

Fija las paletas del piso antes de dibujar nada definitivo y comprueba la escala de 32 px:
- hoja_estilo_piso3.png: rampas de los tres tramos del incendio (pantano verde, bosque humeante, tierra quemada),
  del terreno y las plantas, de las criaturas (jugador, enemigos, jefes), de la clase Parásito y de las reacciones.
- lamina_escala.png: un trozo de sala de cada tramo con suelo, pared, charcos, helecho alto, la rana venenosa
  (cuerpo de ~32 px) y el placodermo del piso 2 (24 px) al lado para comparar.
Hecho con la skill primordia-sprites (pixel.py): 16 px por unidad, luz arriba a la izquierda, contorno OUTL.
Nada de esto se importa: es una prueba de estilo. Las piezas buenas se pasan luego a sus generadores.

Uso: python generar_hoja_estilo.py [carpeta_salida]   (por defecto, la carpeta de este script)
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
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = sys.argv[1] if len(sys.argv) > 1 else HERE
BG = C('#1c2e20')                         # fondo de revisión del piso 3 (musgo oscuro)

# =================================================================================================
# Paletas
# =================================================================================================
# Regla del piso: el escenario va apagado y oscuro (oliva, pardo, ceniza); lo que importa para jugar va
# saturado y claro (veneno lima, fuego naranja, metano cian pálido, ámbar). Así las reacciones se leen
# sobre cualquier tramo.

# --- tres tramos del incendio: suelo (4), motas (3), pared: cara (5), tapa (4), vetas (3) ---------
TRAMOS = {
    'Swamp': dict(                                    # 1. pantano verde
        nombre='Pantano verde',
        floor=['#1c2e20', '#213626', '#273f2b', '#2f4a32'],
        fleck=['#3d5e36', '#557a3e', '#7fa04a'],      # musgo y brotes
        face=['#1e1a1c', '#2e2622', '#40332a', '#584634', '#76603f'],   # talud de tierra y raíces
        cap=['#14231a', '#1b3022', '#24402a', '#2f5232'],               # maleza vista desde arriba
        vein=['#3f6a2e', '#68973c', '#a2c85a'],       # musgo que cuelga del talud
        leaf=['#1a3420', '#264a2a', '#386634', '#548440'],              # follaje de pared (más oscuro que los helechos sueltos)
        bark=['#1e2420', '#2e3830', '#435040', '#5e6c52', '#8a9670'],   # troncos de Lepidodendron
        brush=(152, 184),                             # hueco de maleza quemable en la pared de arriba (x)
    ),
    'Smoke': dict(                                    # 2. bosque humeante
        nombre='Bosque humeante',
        floor=['#22261f', '#292d24', '#30342a', '#3a3d31'],
        fleck=['#55553f', '#77704f', '#a0946a'],      # hojarasca seca y ceniza
        face=['#1c1819', '#29221f', '#382e27', '#4a3d30', '#62513c'],
        cap=['#1a1c18', '#22261f', '#2c3127', '#373c2f'],
        vein=['#5a4a2a', '#8a6a34', '#c09a4a'],       # musgo amarillento, reseco
        leaf=['#2a2818', '#433d1e', '#635726', '#857232'],
        bark=['#1c1e1a', '#2a2e26', '#3c4234', '#525a44', '#707a5a'],
        brush=(40, 72), smoke=True,
    ),
    'Burnt': dict(                                    # 3. tierra quemada
        nombre='Tierra quemada',
        floor=['#1d1a1d', '#242024', '#2c272a', '#363033'],
        fleck=['#4a4346', '#6d6466', '#958a88'],      # ceniza
        face=['#141116', '#1e191d', '#2a2326', '#3a3032', '#4c3f3e'],   # troncos carbonizados
        cap=['#151316', '#1c191c', '#242024', '#2d282b'],
        vein=['#a8341c', '#e8682a', '#ffb24a'],       # ascuas en las grietas
        leaf=['#141114', '#221c1e', '#352b2a', '#4c3e38'],
        bark=['#141114', '#221c1e', '#352b2a', '#4c3e38', '#6a5848'],
    ),
}

# --- terreno y plantas -------------------------------------------------------------------------
BRUSH = ['#3a3018', '#5c4c24', '#86703a', '#b49a52', '#dcc47a']   # maleza quemable: reseca y clara
MUD = ['#22160f', '#33221a', '#4a3324', '#634830', '#86683f']      # lodo profundo (te hunde)
SLUDGE = ['#3a2a1c', '#55402a', '#735a3a', '#9a7e52']              # charco de lodo (ralentiza)
WATER = ['#16302e', '#1d4440', '#2a5e54', '#4e8a72', '#9cc8a0']    # agua de charca, turbia
FERN = ['#1c3a20', '#2a5a2a', '#3e8a34', '#6abf48', '#b8e878']     # helecho alto (vivo, más claro que el suelo)
FERN_DRY = ['#3a2e16', '#5e4a20', '#8a6e2a', '#b8963a', '#e0c870'] # helecho reseco (tramo 2)
CHAR = ['#141114', '#221c1e', '#352b2a', '#4c3e38', '#6a5848']     # carbón: plantas quemadas y muerte del piso
BARK = ['#1e2420', '#2e3830', '#435040', '#5e6c52', '#8a9670']     # corteza de Lepidodendron (escamas en rombo)
STROB = ['#2a1a3a', '#4a2a6a', '#7046a0', '#a07ad0', '#d8c0f0']    # estróbilos (conos-cofre): morado = secreto

# --- reacciones ---------------------------------------------------------------------------------
POISON = ['#2e5a14', '#5aa020', '#a8e040', '#e8ffc0']              # el verde lima del veneno de siempre
METHANE = ['#1e5a78', '#2a90b8', '#6ad0ec', '#d0f8ff']             # gas de pantano: cian pálido, como un fuego fatuo
FIRE = ['#8a1e14', '#d4441c', '#f88c2a', '#ffd25a', '#fff4c0']
TOXIC = ['#2a2a12', '#4e4e1a', '#7a7a28', '#b0b03a', '#e4e478']    # nube tóxica (fuego + veneno): mostaza sucia
AMBER = ['#6a2e0e', '#b0581a', '#e89a30', '#ffcc5a', '#fff0b0']    # ámbar de las especiales
EMBER = ['#5a1a14', '#a8341c', '#e8682a', '#ffb24a']               # ascuas sueltas, mecha del Arthropleura
SPARKLE = ['#fff6c0', '#ffffff']

# --- clases -------------------------------------------------------------------------------------
PARASITE = ['#3a0e1e', '#6a1630', '#9a2044', '#d04a62', '#f4a0a4']  # clase nueva Parásito, granate

# --- criaturas (rampas de 5: cuerpo de 32 px) --------------------------------------------------
CRIATURAS = {
    # jugador: un cuerpo por Boca
    'Rana venenosa': (ramp('#c8a028', 5), ['#0e1e50', '#173a8a', '#2d64c8', '#7aa8e8'], 'Jugador · Área (Escupitajo). Amarillo oscuro, manchas azules'),
    'Diplocaulus': (ramp('#d8806a', 5), ramp('#8a3a4a', 4), 'Jugador · Cuerpo a cuerpo (Peonza). Salmón de ajolote'),
    'Vinagrillo': (ramp('#8a4a2e', 5), ramp('#e0b070', 4), 'Jugador · Proyectil (Chorro). Caoba con pinzas claras'),
    # enemigos
    'Calamites': (ramp('#6a9a3a', 5), ramp('#d8d090', 4), 'Caña segmentada, bandas pálidas'),
    'Medullosa': (ramp('#3a6a3a', 5), ramp('#a8502a', 4), 'Frondas oscuras, semillas pardo rojizo'),
    'Libélula gigante': (ramp('#2aa8a8', 5), ramp('#c8ecf0', 4), 'Turquesa metálico, alas translúcidas'),
    'Escarabajo bombardero': (ramp('#2a3a6a', 5), ramp('#e8702a', 4), 'Élitros azul noche, cabeza naranja'),
    'Araña de las frondas': (ramp('#5a6a2a', 5), ramp('#e04040', 4), 'Camuflaje de helecho, ojos rojos'),
    'Sapo de fango': (ramp('#8a6a3a', 5), ramp('#c8b070', 4), 'Ocre de lodo, vientre claro'),
    # jefes
    'Arthropleura': (ramp('#5a3a5a', 5), ramp('#c89a48', 4), 'Jefe 1 · segmentos ciruela, placas ocre'),
    'Meganeura': (ramp('#1e7a8a', 5), ramp('#e8b050', 4), 'Jefe 2 · verde azulado iridiscente, alas ámbar'),
    'Megarachne': (ramp('#4a3a4a', 5), ramp('#c83a3a', 4), 'Minijefe · euriptérido gris violáceo, marcas rojas'),
    'Lepidodendron ancestral': (BARK, STROB[:4], 'Jefe secreto · corteza gris verdosa, estróbilos morados'),
}

# =================================================================================================
# Utilidades
# =================================================================================================
def noise(seed):
    rnd = random.Random(seed)
    g = [[rnd.random() for _ in range(9)] for _ in range(9)]
    def f(x, y, s=8.0):
        fx, fy = x / s, y / s
        x0, y0 = int(fx) % 8, int(fy) % 8
        tx, ty = fx - int(fx), fy - int(fy)
        tx, ty = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
        a = g[y0][x0] * (1 - tx) + g[y0][x0 + 1] * tx
        b = g[y0 + 1][x0] * (1 - tx) + g[y0 + 1][x0 + 1] * tx
        return a * (1 - ty) + b * ty
    return f

BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]
def dither(v, x, y):
    """v en 0..n-1 continuo -> índice entero con trama Bayer 4×4 entre tonos vecinos."""
    base = int(v); frac = v - base
    return base + (1 if frac > (BAYER[y % 4][x % 4] + 0.5) / 16 else 0)

# =================================================================================================
# Escenario (lienzo grande por trozo de sala)
# =================================================================================================
TW, TH = 14, 10                           # tiles por trozo de sala
W, H = TW * 16, TH * 16

def draw_floor(cv, t, seed, box=None, wall_shadow=True):
    x0, y0, x1, y1 = box or (16, 48, W - 16, H - 16)
    n1, n2 = noise(seed), noise(seed + 7)
    fl = [C(c) for c in t['floor']]
    for y in range(y0, y1):
        for x in range(x0, x1):
            v = n1(x, y, 14) * 2.2 + n2(x, y, 5) * 0.9
            cv.px[(x, y)] = fl[min(3, max(0, dither(v, x, y)))]
    rnd = random.Random(seed)
    for _ in range((x1 - x0) * (y1 - y0) // 450):      # motas en grupos de 1-3 px
        x, y = rnd.randrange(x0 + 2, x1 - 2), rnd.randrange(y0 + 2, y1 - 2)
        c = C(rnd.choice(t['fleck'][:2]))
        cv.put(x, y, c)
        if rnd.random() < 0.5: cv.put(x + 1, y, c)
        if rnd.random() < 0.25: cv.put(x, y - 1, C(t['fleck'][2]))
    if wall_shadow:
        for x in range(x0, x1):                      # sombra de la pared en la primera fila
            for y in range(y0, y0 + 4):
                if (x + y) % 2 == 0 or y < y0 + 2: cv.px[(x, y)] = C(t['floor'][0])

def draw_cap(cv, t, box, seed):
    x0, y0, x1, y1 = box
    n = noise(seed)
    cp = [C(c) for c in t['cap']]
    for y in range(y0, y1):
        for x in range(x0, x1):
            v = n(x, y, 5) * 3.6
            cv.px[(x, y)] = cp[min(3, max(0, dither(v * 0.9, x, y)))]

def draw_face(cv, t, seed, burnt=False):
    """Cara de la pared superior (2 filas de tile): talud de tierra con raíces o troncos carbonizados."""
    fc = [C(c) for c in t['face']]; vn = [C(c) for c in t['vein']]
    rnd = random.Random(seed)
    n = noise(seed + 3)
    for y in range(16, 48):
        for x in range(16, W - 16):
            k = (y - 16) / 32                         # más claro arriba, más oscuro abajo (sombra del pie)
            v = 3.3 - k * 2.4 + (n(x, y, 4) - 0.5) * 1.2
            cv.px[(x, y)] = fc[min(4, max(0, dither(v, x, y)))]
    if burnt:                                         # troncos tumbados uno sobre otro, con grietas de ascuas
        for row, yc in enumerate((22, 33, 43)):
            off = rnd.randrange(0, 20)
            for x in range(16, W - 16):
                for dy in range(-5, 6):
                    y = yc + dy
                    if not 16 <= y < 48: continue
                    ny = dy / 5.5
                    i = 0 if ny > 0.75 else 1 if ny > 0.2 else 2 if ny > -0.5 else 3
                    if (x + off) % 46 < 2: i = 0          # junta entre troncos
                    cv.px[(x, y)] = fc[i + (1 if row == 0 else 0)] if i < 4 else fc[4]
            for _ in range(5):                        # grietas con ascuas
                gx = rnd.randrange(20, W - 24)
                for j in range(rnd.randrange(3, 7)):
                    cv.put(gx + j, yc - 1 + (j % 3 == 1), vn[1] if j % 2 else vn[0])
                cv.put(gx + 1, yc - 1, vn[2])
    else:                                             # raíces verticales que bajan por el talud
        x = 18
        while x < W - 18:
            x += rnd.randrange(5, 11)
            top = 16 + rnd.randrange(0, 6); bot = 40 + rnd.randrange(0, 8)
            sway = rnd.random() * 6
            for y in range(top, bot):
                xx = x + round(math.sin(y / 5 + sway) * 1.2)
                cv.put(xx, y, fc[3]); cv.put(xx + 1, y, fc[1])
        for x in range(16, W - 16):                   # musgo que cuelga del borde de arriba
            d = int(2 + n(x, 0, 3) * 5)
            if rnd.random() < 0.2: d += rnd.randrange(2, 6)
            for y in range(16, 16 + d):
                cv.px[(x, y)] = vn[0] if y > 16 + d - 2 else vn[1] if y > 16 + d - 4 else vn[2] if y == 16 else vn[1]
    for x in range(16, W - 16):                       # pie de la pared contra el suelo
        cv.px[(x, 47)] = fc[0]

def frondlet(cv, x, y, ang, L, pal, droop=.3, lw=2.5, box=None, shade=0):
    """Fronda pequeña para paredes y copas: nervio con foliolos. ang en radianes (0 = derecha, pi/2 = abajo).
    box recorta lo que se sale; shade oscurece (las de detrás)."""
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

def draw_canopy(cv, t, box, seed, clip=None):
    """Tapa de la pared: espesura vista desde arriba. Fondo oscuro con huecos y tres capas de matas de frondas
    (detrás más oscuras). En la tierra quemada, maraña de ramas carbonizadas, ceniza y alguna ascua."""
    x0, y0, x1, y1 = box
    clip = clip or box
    rnd = random.Random(seed); n = noise(seed)
    cap = [C(c) for c in t['cap']]; leaf = [C(c) for c in t['leaf']]
    for y in range(y0, y1):
        for x in range(x0, x1):
            cv.px[(x, y)] = cap[0] if n(x, y, 4) < .45 else cap[1]
    area = (x1 - x0) * (y1 - y0)
    burnt = 'bark' in t and t['leaf'][0] == CHAR[0]
    for layer in range(3):
        for k in range(area // 70):
            cx, cy = rnd.uniform(x0 - 3, x1 + 3), rnd.uniform(y0 - 3, y1 + 3)
            if burnt:
                for f in range(3):
                    twig(cv, cx, cy, rnd.uniform(0, 6.28), rnd.uniform(5, 9), [C(c) for c in CHAR[max(0, 1 - layer):]], clip, rnd.random() < .08, rnd)
                continue
            nf = rnd.randrange(5, 8); a0 = rnd.uniform(0, 6.28)
            for f in range(nf):
                frondlet(cv, cx, cy, a0 + f * 6.283 / nf + rnd.uniform(-.2, .2), rnd.uniform(5, 9), leaf,
                         droop=.12, lw=2, box=clip, shade=3 - layer)
    if burnt:
        for k in range(area // 50):
            cv.put(rnd.randrange(x0, x1), rnd.randrange(y0, y1), C(rnd.choice(('#4a4346', '#6d6466', '#6d6466', '#958a88'))))
        for k in range(area // 500):
            cv.put(rnd.randrange(x0, x1), rnd.randrange(y0, y1), C(EMBER[2]))

def bark_trunk(cv, tx, w, y0, y1, bark, ember=None, seed=0):
    """Tronco de Lepidodendron: cojinetes foliares en rombo (filas alternas desplazadas) con la cicatriz oscura en
    medio, luz a la izquierda y pie ensanchado. ember: las juntas brillan (tronco carbonizado)."""
    rnd = random.Random(seed)
    cw, chh = 6, 7                                    # tamaño de cada rombo
    for y in range(y0, y1):
        flare = 1 if y > y1 - 4 else 0
        row = (y - y0) // chh
        for x in range(tx - flare, tx + w + flare):
            u = (x - tx + .5) / w
            lx = (x - tx + (cw // 2 if row % 2 else 0)) % cw - (cw - 1) / 2
            ly = (y - y0) % chh - (chh - 1) / 2
            dd = abs(lx) / (cw / 2) + abs(ly) / (chh / 2)
            light = 4 if u < .22 else 3 if u < .45 else 2 if u < .8 else 1
            if x == tx - flare or x == tx + w - 1 + flare: c = bark[0]
            elif dd > .95: c = C(ember) if ember and rnd.random() < .1 else bark[max(0, light - 2)]   # junta
            elif dd < .25 and u > .2: c = bark[max(0, light - 2)]                                     # cicatriz
            else: c = bark[light] if dd < .7 else bark[max(1, light - 1)]
            cv.put(x, y, c)

def draw_face_veg(cv, t, seed, x0, x1, y0=16, y1=48):
    """Cara de la pared superior: espesura vista de frente. Fondo en sombra, frondas lejanas colgando, troncos de
    Lepidodendron, cortina de frondas delante, musgo y raíces colgando, y helechos que suben desde el pie."""
    rnd = random.Random(seed)
    cap = [C(c) for c in t['cap']]; leaf = [C(c) for c in t['leaf']]; bark = [C(c) for c in t['bark']]
    vn = [C(c) for c in t['vein']]
    box = (x0, y0, x1, y1)
    burnt = t['leaf'][0] == CHAR[0]
    for y in range(y0, y1):
        for x in range(x0, x1):
            k = (y - y0) / (y1 - y0)
            cv.px[(x, y)] = cap[1] if k < .35 and (x + y) % 2 == 0 else cap[0]
    if burnt:                                         # bosque quemado con fondo: humo, troncos lejanos, troncos de pie
        glow = C('#3a1812'); haze = C('#5a4a48')
        for y in range(y0, y1):                       # humo arriba, resplandor de incendios lejanos abajo
            k = (y - y0) / (y1 - y0)
            for x in range(x0, x1):
                c = cap[0] if k < .5 or (k < .75 and (x + y) % 2) else blend(cap[0], glow, int(255 * (k - .4)))
                cv.px[(x, y)] = c
        for k in range((x1 - x0) // 9):               # troncos lejanos: siluetas finas, partidos a distinta altura
            tx = rnd.randrange(x0, x1 - 3); wd = rnd.randrange(2, 4); top = y0 + rnd.randrange(0, 14)
            for y in range(top, y1):
                for dx in range(wd):
                    cv.put(tx + dx, y, C(CHAR[1]) if dx else C(CHAR[2]))
            cv.put(tx + rnd.randrange(wd), top - 1, C(CHAR[1]))           # punta astillada
        for band in range(2):                         # velo de humo entre los planos
            yy = y0 + 8 + band * 11; ph = rnd.uniform(0, 6)
            for x in range(x0, x1):
                y = yy + round(math.sin(x / 7 + ph) * 1.5)
                for dy in (0, 1, 2):
                    if (x + dy) % 2 == 0: cv.px[(x, y + dy)] = blend(cv.px.get((x, y + dy)), haze, 60)
        x = x0 + rnd.randrange(4, 18)
        while x < x1 - 10:                            # troncos de pie, juntas encendidas, algunos partidos
            top = y0 if rnd.random() < .6 else y0 + rnd.randrange(4, 12)
            wd = rnd.randrange(8, 12)
            bark_trunk(cv, x, wd, top, y1, bark, EMBER[2], seed + x)
            if top > y0:
                for dx in range(wd):                  # corte astillado arriba
                    for j in range(rnd.randrange(0, 3)): cv.put(x + dx, top + j, cap[0])
                cv.put(x + wd // 2, top + 2, C(EMBER[3]))
            for k in range(2): twig(cv, x + wd // 2, top + rnd.randrange(3, 12), rnd.choice((-.4, 3.5)), rnd.uniform(6, 10), [C(c) for c in CHAR], box, True, rnd)
            x += rnd.randrange(30, 50)
        for k in range(2):                            # algún tronco caído suelto al pie, inclinado
            lx = rnd.randrange(x0, x1 - 40); L = rnd.randrange(26, 40); tilt = rnd.uniform(-.12, .12)
            for i in range(L):
                yc = y1 - 5 + round(i * tilt)
                for dy in range(-3, 3):
                    ny = dy / 3
                    cv.put(lx + i, yc + dy, C(CHAR[1] if ny > .5 else CHAR[2] if ny > -.3 else CHAR[3]))
                if i % 9 == 4: cv.put(lx + i, yc - 1, C(EMBER[1])); cv.put(lx + i + 1, yc - 1, C(EMBER[2]))
            for dy in range(-3, 3): cv.put(lx + L, y1 - 5 + round(L * tilt) + dy, C(CHAR[3]))   # corte del tronco
        for x in range(x0, x1): cv.px[(x, y1 - 1)] = cap[0]
        return
    for k in range((x1 - x0) // 5):                   # frondas lejanas, muy oscuras, colgando
        frondlet(cv, rnd.uniform(x0, x1), y0 + rnd.uniform(-2, 8), math.pi / 2 + rnd.uniform(-1.1, 1.1),
                 rnd.uniform(8, 14), leaf, droop=.4, lw=2.5, box=box, shade=2)
    x = x0 + rnd.randrange(2, 14)                     # troncos
    while x < x1 - 8:
        bark_trunk(cv, x, rnd.randrange(10, 14), y0, y1, bark, None, seed + x)
        x += rnd.randrange(30, 52)
    for k in range((x1 - x0) // 7):                   # cortina de frondas delante, desde la copa
        frondlet(cv, rnd.uniform(x0, x1), y0 + rnd.uniform(-3, 4), math.pi / 2 + rnd.uniform(-1.2, 1.2),
                 rnd.uniform(9, 15), leaf, droop=.45, lw=3, box=box, shade=rnd.choice((0, 1)))
    for k in range((x1 - x0) // 9):                   # musgo y raíces colgando
        mx = rnd.randrange(x0, x1); L = rnd.randrange(4, 14)
        for j in range(L):
            cv.put(mx + round(math.sin(j / 3 + mx) * .6), y0 + j, vn[2] if j == 0 else vn[1] if j < L - 2 else vn[0])
    for k in range((x1 - x0) // 8):                   # helechos que suben desde el pie
        frondlet(cv, rnd.uniform(x0, x1), y1 - 1, -math.pi / 2 + rnd.uniform(-1.0, 1.0),
                 rnd.uniform(7, 12), leaf, droop=.35, lw=2.5, box=box, shade=rnd.choice((0, 1)))
    for x in range(x0, x1):                           # pie en sombra contra el suelo
        cv.px[(x, y1 - 1)] = cap[0]
    if t.get('smoke'):                                # humo entre las hojas
        for k in range(3):
            yy = y0 + 6 + k * 9; ph = rnd.uniform(0, 6)
            for x in range(x0, x1):
                if math.sin(x / 9 + ph) > .1:
                    y = yy + round(math.sin(x / 6 + ph) * 1.5)
                    for dy in (0, 1):
                        if (x + dy) % 2 == 0: cv.px[(x, y + dy)] = blend(cv.px.get((x, y + dy)), C('#8a8a7a'), 70)

def draw_brush(cv, x0, x1, y0=16, y1=48, seed=0):
    """Maleza quemable: hueco de 2 tiles en la pared tapado por una maraña de tallos secos, más clara que la
    pared. Detrás se intuye un paso oscuro (hay algo al otro lado)."""
    rnd = random.Random(seed)
    br = [C(c) for c in BRUSH]
    for y in range(y0, y1):                           # paso oscuro detrás, con el suelo asomando abajo
        for x in range(x0, x1):
            cv.px[(x, y)] = C('#0e1210') if y < y1 - 6 or abs(x - (x0 + x1) / 2) > 9 else C('#1c2a1e')
    for k in range(34):                               # tallos que se arquean y se cruzan
        sx = rnd.uniform(x0 - 2, x1 + 2); ang = rnd.choice((-1, 1)) * rnd.uniform(.4, 1.2)
        L = rnd.uniform(10, 28); yb = y1 - rnd.uniform(0, 6)
        for st in range(int(L)):
            x, y = sx + math.sin(ang) * st + ang * (st / L) ** 2 * 8, yb - st * math.cos(ang * .6)
            if x0 <= x < x1 and y0 <= y < y1:
                cv.put(x, y, br[3] if (st + k) % 5 == 0 else br[2] if k % 2 else br[1])
                if st % 4 == 2: cv.put(x + (1 if ang > 0 else -1), y - 1, br[4] if st % 8 == 2 else br[2])
    for k in range(46):                               # hojitas secas
        x, y = rnd.randrange(x0, x1 - 1), rnd.randrange(y0, y1 - 1)
        cv.put(x, y, br[3]); cv.put(x + 1, y, br[2]); cv.put(x, y + 1, br[1])
    for x in range(x0, x1): cv.put(x, y1 - 1, br[0])

def soften_gap(cv, t, x0, x1, y0=16, y1=48, seed=0):
    """Las frondas de la pared de al lado tapan los bordes del hueco de maleza (que no parezca recortado)."""
    rnd = random.Random(seed); leaf = [C(c) for c in t['leaf']]
    box = (x0 - 6, y0, x1 + 6, y1)
    for k in range(6):
        frondlet(cv, x0 + rnd.uniform(-4, 2), y0 + rnd.uniform(0, 26), rnd.uniform(-.6, .9), rnd.uniform(6, 10), leaf, .35, 2.5, box, rnd.choice((0, 1)))
        frondlet(cv, x1 + rnd.uniform(-2, 4), y0 + rnd.uniform(0, 26), math.pi + rnd.uniform(-.9, .6), rnd.uniform(6, 10), leaf, .35, 2.5, box, rnd.choice((0, 1)))
    for k in range(7):
        frondlet(cv, rnd.uniform(x0, x1), y0 - 1, math.pi / 2 + rnd.uniform(-1, 1), rnd.uniform(6, 11), leaf, .45, 3, box, rnd.choice((0, 1)))

def spill(cv, t, seed):
    """La espesura se sale de las paredes hacia el suelo: el borde de la sala deja de ser una línea recta."""
    rnd = random.Random(seed)
    leaf = [C(c) for c in t['leaf']]
    burnt = t['leaf'][0] == CHAR[0]
    floor_box = (16, 48, W - 16, H - 16)
    def one(x, y, ang, L):
        b = (max(16, int(x) - 12), max(48, int(y) - 12), min(W - 16, int(x) + 13), min(H - 16, int(y) + 13))
        if burnt: twig(cv, x, y, ang, L * .8, [C(c) for c in CHAR], b, True, rnd)
        else: frondlet(cv, x, y, ang, L, leaf, droop=.3, lw=2.5, box=b, shade=rnd.choice((0, 0, 1)))
    for x in range(18, W - 18, 7):                    # desde el pie de la pared de arriba
        if rnd.random() < .6: one(x + rnd.uniform(-2, 2), 47, math.pi / 2 + rnd.uniform(-.9, .9), rnd.uniform(4, 9))
    for y in range(52, H - 18, 7):                    # desde los lados
        if rnd.random() < .6: one(15, y + rnd.uniform(-2, 2), rnd.uniform(-.5, .7), rnd.uniform(4, 8))
        if rnd.random() < .6: one(W - 16, y + rnd.uniform(-2, 2), math.pi + rnd.uniform(-.7, .5), rnd.uniform(4, 8))
    for x in range(18, W - 18, 8):                    # desde la pared de abajo (asoman por encima)
        if rnd.random() < .5: one(x + rnd.uniform(-2, 2), H - 16, -math.pi / 2 + rnd.uniform(-.8, .8), rnd.uniform(3, 6))

def draw_walls(cv, t, seed, burnt):
    br = t.get('brush')
    if br:
        draw_face_veg(cv, t, seed, 16, br[0]); draw_face_veg(cv, t, seed + 5, br[1], W - 16)
        draw_brush(cv, br[0], br[1], seed=seed + 9)
        soften_gap(cv, t, br[0], br[1], seed=seed + 11)
    else:
        draw_face_veg(cv, t, seed, 16, W - 16)
    draw_canopy(cv, t, (0, 0, W, 16), seed, clip=(0, 0, W, 19))     # la copa cuelga un poco sobre la cara
    draw_canopy(cv, t, (0, 16, 16, H), seed + 1)
    draw_canopy(cv, t, (W - 16, 16, W, H), seed + 2)
    draw_canopy(cv, t, (16, H - 16, W - 16, H), seed + 3)
    for y in range(17, H - 16): cv.px[(15, y)] = blend(cv.px[(15, y)], C(t['cap'][3]), 120)   # canto con luz
    spill(cv, t, seed + 4)

N4 = ((1, 0), (-1, 0), (0, 1), (0, -1))
SQ = 1.6                                   # aplastamiento vertical de lo que está en el suelo (vista inclinada)
SHAPES = ('redondo', 'alargado', 'lobulado', 'cadena', 'isla', 'salpicado', 'rinon')

def blob(seed, shape, w, h):
    """Máscara orgánica de w×h para charcos, lodo y niebla: metabolas aplastadas en vertical con el borde movido
    por ruido. Formas: redondo, alargado, lobulado, cadena, isla (con islote en medio), salpicado (gotas
    alrededor) y rinon (media luna)."""
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

LIQUIDS = {                      # rampa, reflejos de agua, brillo de barro, burbujas, orilla húmeda
    'agua': (WATER, True, False, 0, True),
    'veneno': (POISON, True, False, 2, False),
    'lodo': (SLUDGE, False, True, 0, True),
    'profundo': (MUD, False, True, 4, True),
    'ceniza': (CHAR[1:], False, False, 0, False),
}

def puddle(cv, ox, oy, mask, kind, seed):
    """Charco de la forma `mask` (de blob) con la esquina en ox, oy. Arriba, la sombra del borde; abajo, la orilla
    mojada que coge luz; dentro, reflejos (agua, veneno) o brillo pegajoso y burbujas (lodo)."""
    rmp, refl, gloss, nbub, wet = LIQUIDS[kind]
    r = [C(c) for c in rmp]; top = len(r) - 1; b = 2 if top >= 4 else 1
    rnd = random.Random(seed); n = noise(seed + 9)
    dist = {p: 1 for p in mask if any((p[0] + dx, p[1] + dy) not in mask for dx, dy in N4)}
    for k in (2, 3, 4):
        for p in [p for p in mask if p not in dist and any(dist.get((p[0] + dx, p[1] + dy)) == k - 1 for dx, dy in N4)]:
            dist[p] = k
    if wet:                                           # orilla húmeda: el suelo justo debajo y a los lados, más oscuro
        ring = {(x + dx, y + dy) for (x, y) in mask for dx, dy in ((0, 1), (1, 0), (-1, 0), (0, 2))} - mask
        for q in ring:
            if (q[0], q[1] - 1) in mask or (q[0], q[1] - 2) in mask or (q[0] - 1, q[1]) in mask or (q[0] + 1, q[1]) in mask:
                c = cv.px.get((ox + q[0], oy + q[1]))
                if c: cv.px[(ox + q[0], oy + q[1])] = tuple(int(v * .8) for v in c[:3]) + (255,)
    for (x, y) in mask:
        d = dist.get((x, y), 5)
        if (x, y - 1) not in mask: i = 0                          # sombra del borde de arriba
        elif (x, y + 1) not in mask: i = top - 1                  # orilla de abajo, iluminada
        elif d == 1: i = max(0, b - 1)
        elif (x, y - 2) not in mask: i = max(0, b - 1)            # franja en sombra bajo el borde
        else: i = (b - 1 if n(x, y, 5) < .38 else b + 1 if n(x, y, 3) > .78 else b) if gloss else b   # manchas, sin trama
        cv.px[(ox + x, oy + y)] = r[i]
    inner = sorted(p for p in mask if dist.get(p, 5) >= 3)
    if refl and inner:                                # reflejos: rayitas horizontales arriba a la izquierda
        mx = sum(p[0] for p in inner) / len(inner); my = sum(p[1] for p in inner) / len(inner)
        cand = [p for p in inner if p[0] < mx + 2 and p[1] < my + 1] or inner
        for k in range(max(1, len(inner) // 60) + 1):
            x, y = rnd.choice(cand)
            for j in range(rnd.randrange(2, 5)):
                if (x + j, y) in mask: cv.px[(ox + x + j, oy + y)] = r[top] if j else r[top - 1]
    if gloss and inner:                               # brillo pegajoso del barro
        for k in range(max(1, len(inner) // 70)):
            x, y = rnd.choice(inner)
            cv.px[(ox + x, oy + y)] = r[top - 1]; cv.put(ox + x + 1, oy + y, r[top])
    for k in range(nbub if inner else 0):            # burbujas con su onda
        x, y = rnd.choice(inner)
        cv.px[(ox + x, oy + y)] = r[top]; cv.px[(ox + x + 1, oy + y)] = r[top - 1]
        for j in range(-3, 5):
            q = (x + j, y + (1 if abs(j - .5) > 2.5 else 2))
            if q in mask and abs(j - .5) > 1.5: cv.px[(ox + q[0], oy + q[1])] = r[min(top - 1, b + 1)]

TRUNK = ['#241a18', '#36281f', '#4c3828', '#664d34']   # manto de raíces de Psaronius
FERN_KINDS = {          # frondas detrás, delante, ancho de foliolo, paso entre foliolos, largo, caída de la punta
    'frondoso':     (5, 6, 5.5, 2, 1.0, .34),
    'ralo':         (1, 3, 3.0, 3, 1.05, .30),
    'joven':        (0, 3, 3.5, 2, .55, .25),
    'inclinado':    (2, 4, 4.5, 2, .95, .55),
    'arborescente': (3, 5, 4.5, 2, .72, .75),
}

def fern(cv, bx, by, rmp, seed, kind='frondoso', burnt=0.0, fire=False, flip=None):
    """Helecho del escenario. kind: frondoso (mata densa), ralo (pocas frondas finas), joven (corto, con báculos
    enrollados), inclinado (vencido hacia un lado), arborescente (Psaronius: tronco de 1,5 tiles y copa).
    Primero las frondas de detrás (un tono más oscuras). burnt 0..1 = parte quemada desde las puntas."""
    rnd = random.Random(seed)
    nb, nf, lw, step, ls, droop = FERN_KINDS[kind]
    flip = rnd.random() < .5 if flip is None else flip
    rp = [C(c) for c in rmp]; ch = [C(c) for c in CHAR]
    lean = (.45 if kind == 'inclinado' else 0) * (-1 if flip else 1)
    top_y = by
    if kind == 'arborescente':
        tr = ch if burnt > .8 else [C(c) for c in TRUNK]
        top_y = by - 22
        for y in range(top_y, by + 1):
            for dx in range(-2, 3):
                k = (y + (dx + 2) * 2) % 5                         # escamas del manto de raíces
                i = 0 if dx == 2 else 3 if dx == -2 and k < 3 else 2 if k < 2 else 1
                cv.put(bx + dx, y, tr[i])
        for dx in (-3, -2, 2, 3):                                  # raíces que se abren al pie
            cv.put(bx + dx, by, tr[1]); cv.put(bx + dx * 1.3, by + 1, tr[0])
        lo, hi = -1.7, 1.7
    else:
        lo, hi = -1.0, 1.0
    def spread(n, a, b):
        if n == 0: return []
        if n == 1: return [rnd.uniform(-.2, .2)]
        return [a + (b - a) * i / (n - 1) + rnd.uniform(-.12, .12) for i in range(n)]
    back = [(a + lean, rnd.uniform(28, 34) * ls) for a in spread(nb, lo, hi)]
    front = [(a + lean, rnd.uniform(34, 42) * ls) for a in spread(nf, lo * .8, hi * .8)]
    tips = []
    for layer, fronds in enumerate((back, front)):
        for ang, L in fronds:
            side = -1 if ang < 0 else 1
            pts = []
            for st in range(int(L)):
                t = st / L
                pts.append((bx + math.sin(ang) * st + side * t * t * L * .22,
                            top_y - math.cos(ang) * st + t * t * L * droop, t))
            for j, (x, y, t) in enumerate(pts):
                scorched = t > 1 - burnt
                pal = ch if scorched else rp
                d = 1 - layer
                q = pts[min(j + 1, len(pts) - 1)]
                nx, ny = q[0] - x, q[1] - y
                nn = math.hypot(nx, ny) or 1; nx, ny = nx / nn, ny / nn
                if j % step == 0 and t < .94:
                    w = 1 + round((1 - t) * lw * (1 - .5 * t))
                    for sgn in (-1, 1):
                        px, py = -ny * sgn, nx * sgn
                        lit = (px * -.6 + py * -.8) > 0
                        for k in range(1, w + 1):
                            tone = (3 if lit else 1) if k < w else (2 if lit else 0)
                            cv.put(x + (px * .85 + nx * .5) * k, y + (py * .85 + ny * .5) * k, pal[max(0, tone - d)])
                cv.put(x, y, pal[max(0, 2 - d)] if not scorched else ch[2])
                if scorched and fire and rnd.random() < .08: tips.append((x, y, 2))
            tips.append(pts[-1])
    if kind == 'joven':
        for k in range(2): fiddle(cv, bx + (k * 2 - 1) * rnd.randrange(2, 5), by, rnd.randrange(8, 13), rp if burnt < .5 else ch, k == 0)
    elif kind == 'frondoso' and rnd.random() < .6:
        fiddle(cv, bx + rnd.randrange(-4, 5), by, rnd.randrange(10, 15), rp if burnt < .5 else ch, rnd.random() < .5)
    if kind != 'arborescente':
        for x in range(-2, 3):                                    # mata del pie
            for y in range(-2, 1): cv.put(bx + x, by + y, rp[0] if burnt < .9 else ch[1])
    if fire:
        for x, y, t in tips:
            if t == 2 or burnt > .3: flame(cv, x, y - 1, rnd)

def fiddle(cv, x, y, h, rp, left):
    """Báculo: fronda joven que sube y se enrolla en espiral en la punta."""
    sd = -1 if left else 1
    for k in range(h):
        cv.put(x + round(math.sin(k / h * 1.4) * 1.5) * sd, y - k, rp[2] if k % 3 else rp[1])
    tx, ty = x + round(math.sin(1.4) * 1.5) * sd, y - h
    for k in range(13):
        a = k * .7; rr = 2.0 * (1 - k / 14)
        cv.put(tx + sd * (rr - rr * math.cos(a)), ty - rr * math.sin(a), rp[3] if a < 3.3 else rp[2])
    cv.put(tx + sd * 2, ty - 1, rp[4] if len(rp) > 4 else rp[3])

DECO = {
    'Swamp': ('musgo', 'musgo', 'hojas', 'piedra', 'raiz', 'brote', 'musgo'),
    'Smoke': ('hojas', 'hojas', 'piedra', 'raiz', 'musgo', 'ceniza'),
    'Burnt': ('ramitas', 'piedra', 'ceniza', 'tocon', 'raiz', 'ramitas'),
}
STONE = ['#2e2c34', '#4a4650', '#6a6670', '#8e8a90']

def deco(cv, kind, x, y, tk, seed):
    """Detalles pequeños del suelo (no bloquean). Devuelven la caja que ocupan (para no amontonarlos)."""
    rnd = random.Random(seed); t = TRAMOS[tk]
    fl = [C(c) for c in t['fleck']]
    if kind == 'musgo':                               # cojín de musgo
        cols = fl if tk == 'Swamp' else [C(c) for c in t['vein']]
        w, h = rnd.randrange(7, 12), rnd.randrange(4, 6)
        m = blob(seed, rnd.choice(('redondo', 'lobulado')), w, h)
        for (i, j) in m:
            top = (i, j - 1) not in m
            cv.px[(x + i, y + j)] = cols[2] if top and (i + j) % 2 == 0 else cols[1] if top or (i * 7 + j * 3) % 5 == 0 else cols[0]
        return (x, y, w, h)
    if kind == 'hojas':                               # hojas de helecho caídas
        cols = [C(c) for c in (FERN[1:4] if tk == 'Swamp' else FERN_DRY[1:4] if tk == 'Smoke' else CHAR[1:4])]
        for k in range(rnd.randrange(2, 5)):
            lx, ly = x + rnd.randrange(0, 9), y + rnd.randrange(0, 5)
            fl_ = rnd.random() < .5
            for (i, j, c) in ((0, 0, 1), (1, 0, 2), (1, 1, 0), (2, 1, 1)):
                cv.put(lx + (2 - i if fl_ else i), ly + j, cols[c])
        return (x, y, 12, 7)
    if kind == 'piedra':
        rx, ry = rnd.uniform(2.5, 4), rnd.uniform(1.8, 2.6)
        cvs = Canvas(12, 8); ellipse(cvs, 6, 4.5, rx, ry, STONE); cvs.outline()
        if tk == 'Swamp':
            for i in range(12):
                if (i, 3) in cvs.px and cvs.px[(i, 3)] != OUTL and rnd.random() < .7: cvs.put(i, 3, fl[1])
        for (i, j), c in cvs.px.items(): cv.px[(x + i, y + j)] = c
        return (x, y, 12, 8)
    if kind == 'raiz':                                # raíz que asoma del suelo y se vuelve a hundir
        fc = [C(c) for c in t['face']]
        L = rnd.randrange(10, 18); ph = rnd.uniform(0, 6)
        for i in range(L):
            yy = y + 2 + round(math.sin(i / 4 + ph) * 1.2)
            end = i < 2 or i >= L - 2
            cv.put(x + i, yy, fc[1] if end else fc[3]); cv.put(x + i, yy + 1, fc[0] if end else fc[1])
        return (x, y, L, 6)
    if kind == 'brote':
        fiddle(cv, x + 3, y + 9, 5, [C(c) for c in FERN], rnd.random() < .5)
        return (x, y, 7, 10)
    if kind == 'ceniza':
        cols = [C(c) for c in CHAR[2:]] + [C('#8a8484')]
        for k in range(rnd.randrange(6, 12)):
            cv.put(x + rnd.randrange(0, 10), y + rnd.randrange(0, 5), cols[rnd.randrange(len(cols))])
        return (x, y, 10, 5)
    if kind == 'ramitas':
        ch = [C(c) for c in CHAR]
        for k in range(rnd.randrange(2, 4)):
            sx, sy = x + rnd.randrange(0, 6), y + rnd.randrange(0, 5)
            dx = rnd.choice((1, -1)); L = rnd.randrange(4, 8)
            for i in range(L): cv.put(sx + i * dx, sy + (i // 3), ch[3] if i % 2 else ch[2])
            if rnd.random() < .5: cv.put(sx + (L - 1) * dx, sy + (L - 1) // 3, C(EMBER[2]))
        return (x, y, 12, 7)
    if kind == 'tocon':                               # tocón quemado: anillos arriba, lado en sombra
        ch = [C(c) for c in CHAR]
        cvs = Canvas(12, 11)
        for j in range(4, 10):
            for i in range(2 - (j > 7), 10 + (j > 7)): cvs.put(i, j, ch[1] if i > 6 else ch[2] if i > 3 else ch[3])
        ellipse(cvs, 6, 4, 4, 2.2, CHAR[3:] + [C('#8a7a68')])
        for i in range(4, 9): cvs.put(i, 4 - (i in (5, 6, 7)), ch[2] if i % 2 else ch[3])
        cvs.put(5, 4, ch[1]); cvs.put(6, 4, ch[1]); cvs.put(7, 3, C(EMBER[2]))
        cvs.outline()
        for (i, j), c in cvs.px.items(): cv.px[(x + i, y + j)] = c
        return (x, y, 12, 11)
    return (x, y, 0, 0)

def scatter(cv, tk, n, seed, taken, box):
    """Reparte n detalles del tramo sin pisar lo ya colocado (taken: lista de cajas x, y, w, h)."""
    rnd = random.Random(seed)
    x0, y0, x1, y1 = box
    for k in range(n * 8):
        if n <= 0: break
        x, y = rnd.randrange(x0, x1 - 12), rnd.randrange(y0, y1 - 10)
        if any(x < bx + bw + 2 and bx < x + 14 and y < by + bh + 2 and by < y + 12 for bx, by, bw, bh in taken): continue
        taken.append(deco(cv, rnd.choice(DECO[tk]), x, y, tk, seed * 31 + k)); n -= 1

def flame(cv, x, y, rnd, h=None):
    h = h or rnd.randrange(3, 6)
    for k in range(h):
        c = C(FIRE[4]) if k == 0 else C(FIRE[3]) if k < 2 else C(FIRE[2]) if k < h - 1 else C(FIRE[1])
        cv.put(x, y - k, c)
        if k < h - 2: cv.put(x + (1 if k % 2 else -1), y - k, C(FIRE[2]))

def methane_mist(cv, ox, oy, w, h, seed, phase=0.0):
    """Metano a ras de suelo: niebla baja y ancha, sin borde duro (no debe leerse como un charco). Muy translúcida,
    se va deshaciendo en trama hacia fuera, más densa abajo (pesa). Lo que más se ve son las vetas claras que se
    arrastran, las volutas que suben y se apagan, y las burbujitas donde brota. phase 0..1 para el bucle."""
    rnd = random.Random(seed)
    mask = blob(seed, 'alargado', w, h)
    dist = {q: 1 for q in mask if any((q[0] + dx, q[1] + dy) not in mask for dx, dy in N4)}
    for k in (2, 3, 4):
        for q in [q for q in mask if q not in dist and any(dist.get((q[0] + dx, q[1] + dy)) == k - 1 for dx, dy in N4)]:
            dist[q] = k
    ys = [q[1] for q in mask]; ytop, ybot = min(ys), max(ys)
    drift = phase * 6.283
    def add(x, y, c, a): cv.px[(ox + x, oy + y)] = blend(cv.px.get((ox + x, oy + y)), C(c), a)
    for (x, y) in mask:
        d = dist.get((x, y), 5)
        dens = min(1, d / 4) * (.55 + .45 * (y - ytop) / max(1, ybot - ytop))   # se deshace hacia fuera, pesa abajo
        dens *= .8 + .2 * math.sin(x / 5 + drift + y * .7)                       # ondula al moverse
        if dens * 16 > BAYER[y % 4][(x + int(phase * 8)) % 4] + .5:
            add(x, y, METHANE[2], 70 if dens > .6 else 50)
    for i in range(3):                                  # vetas que se arrastran hacia la derecha
        yl = ytop + 2 + i * max(2, (ybot - ytop - 3) // 3)
        off = rnd.randrange(0, 16)
        for x in range(w):
            u = (x - phase * 32 + off) % 16
            if u < 7:
                y = yl + round(math.sin((x + phase * 32) / 4 + i) * .7)
                if dist.get((x, y), 0) >= 2: add(x, y, METHANE[3], 150 if 1 < u < 6 else 80)
    for i in range(4):                                  # volutas que suben y se apagan
        wx = rnd.randrange(w // 6, w * 5 // 6)
        ph = (phase + i / 4) % 1
        col = [q[1] for q in mask if abs(q[0] - wx) < 2]
        base_y = (min(col) + 2) if col else ytop
        for j in range(6):
            yy = base_y - j - round(ph * 5)
            xx = wx + round(math.sin(j * .9 + ph * 6 + i) * 1.3)
            a = int(150 * (1 - j / 6) * (1 - ph))
            if a > 20: add(xx, yy, METHANE[3], a)
    for i in range(3):                                  # burbujitas en el suelo, donde brota
        bx = rnd.randrange(w // 4, w * 3 // 4); by = ybot - 2 - rnd.randrange(0, 3)
        if (phase * 2 + i * .37) % 1 < .45:
            add(bx, by, METHANE[3], 210); add(bx + 1, by, METHANE[2], 150)

def blend(under, over, a):
    if under is None: return over[:3] + (a,)
    k = a / 255
    return tuple(round(under[j] * (1 - k) + over[j] * k) for j in range(3)) + (255,)

def ember_crack(cv, x0, y0, length, seed):
    rnd = random.Random(seed)
    x, y = x0, y0
    for k in range(length):
        cv.put(x, y, C(EMBER[1] if k % 4 else EMBER[2]))
        if k % 5 == 2: cv.put(x, y - 1, C(EMBER[3]))
        x += 1; y += rnd.choice((-1, 0, 0, 1))
        if rnd.random() < 0.12:
            cv.put(x, y + 1, C(EMBER[0])); cv.put(x, y + 2, C(EMBER[0]))

def drop_shadow(cv, cx, cy, rx, ry=None):
    ry = ry or max(1, rx // 3)
    for y in range(int(cy - ry), int(cy + ry) + 1):
        for x in range(int(cx - rx), int(cx + rx) + 1):
            if ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 <= 1 and (x, y) in cv.px:
                p = cv.px[(x, y)]
                cv.px[(x, y)] = tuple(int(v * 0.72) for v in p[:3]) + (255,)

# =================================================================================================
# Rana venenosa (cuerpo del jugador, ~32 px, mira a la derecha, sentada)
# =================================================================================================
FROG = ramp('#c8a028', 5)                # amarillo oscuro
INK = ['#173a8a', '#2d64c8', '#7aa8e8']   # manchas azules: sombra, base, brillo

def frog(breath=0.0):
    cv = Canvas(32)
    b = breath * 0.3
    body = FROG[1:]                                         # sombra, base, luz, brillo (shift=-1: domina la base)
    rot_ellipse(cv, 10, 22.5, 6.2, 4.4, -0.3, FROG[:4], shift=-1)          # anca de la pata trasera
    rot_ellipse(cv, 15, 18.5 - b, 10.5, 7.4 + breath * 0.4, -0.32, body, shift=-1)   # cuerpo en gota
    ellipse(cv, 23.2, 14.5 - b, 6.0, 5.0, body, shift=-1)                  # cabeza
    ellipse(cv, 22.5, 10.2 - b, 3.3, 3.1, body, shift=-1)                  # ojo saltón
    rot_ellipse(cv, 21.5, 23.5, 1.8, 3.6, 0.25, FROG[:4], shift=-1)        # pata delantera
    cv.outline(); cv.clean()
    for x in range(6, 15):                                   # pliegue del anca contra el cuerpo
        y = round(20.5 + (x - 6) * 0.35)
        if cv.px.get((x, y)) not in (None, OUTL): cv.put(x, y, C(FROG[0]))
    for (x, y, w, hh) in ((7, 15, 3, 2), (11, 12, 3, 2), (15, 15, 3, 3), (10, 19, 3, 2), (18, 12, 2, 2),
                          (5, 21, 2, 2), (14, 21, 3, 2), (25, 13, 2, 1)):          # manchas azules
        for dy in range(hh):
            last = dy == hh - 1 and w > 2
            for k in range(w - (1 if last else 0)):
                cv.put_in(x + k + (1 if last else 0), round(y - b) + dy, C(INK[0] if dy == hh - 1 else INK[1]))
        cv.put_in(x, round(y - b), C(INK[2]))
    for x in range(21, 29): cv.put_in(x, round(17 - b) + (1 if x < 23 else 0), C(FROG[0]))   # boca
    ey = round(9 - b)                                         # ojo: blanco roto, pupila mirando a la derecha
    for dx in range(3):
        for dy in range(3): cv.put(21 + dx, ey + dy, EYE)
    for dx, dy in ((2, 0), (2, 1), (1, 1), (2, 2), (1, 2)): cv.put(21 + dx, ey + dy, OUTL)
    cv.put_in(11, 12, C(FROG[4])); cv.put_in(12, 11, C(FROG[4])); cv.put_in(13, 11, C(FROG[3]))   # brillo del lomo
    cv.put_in(20, 11, C(FROG[3]))
    for dx in (-1, 1, 2): cv.put_out(21.5 + dx, 27, C(FROG[1]))  # dedos (finos, sin contorno)
    return cv

def frog_frames():
    return [frog(math.sin(i * math.pi / 2) * 0.8 + 0.8) for i in range(4)]

def placoderm():
    p = os.path.join(HERE, '..', 'floor2-bodies', 'Placoderm', 'Player_Placoderm_Idle_side.png')
    try:
        return Image.open(p).convert('RGBA').crop((0, 0, 24, 24))
    except OSError:
        return None

# =================================================================================================
# Trozos de sala
# =================================================================================================
def paste(cv, im, x, y):
    im = as_img(im)
    for (i, j) in ((i, j) for j in range(im.height) for i in range(im.width)):
        p = im.getpixel((i, j))
        if p[3] > 0: cv.px[(x + i, y + j)] = blend(cv.px.get((x + i, y + j)), p, p[3]) if p[3] < 255 else p

def scene(key, seed):
    t = TRAMOS[key]
    cv = Canvas(W, H)
    draw_floor(cv, t, seed)
    draw_walls(cv, t, seed, burnt=(key == 'Burnt'))
    fr = frog()
    taken = [(88, 68, 36, 38)]                         # sitio de la rana
    def pud(x, y, w, h, shape, kind, s_):
        puddle(cv, x, y, blob(s_, shape, w, h), kind, s_); taken.append((x, y, w, h))
    if key == 'Swamp':
        pud(20, 100, 64, 32, 'isla', 'profundo', seed + 1)
        pud(144, 54, 40, 16, 'lobulado', 'agua', seed + 2)
        pud(140, 124, 30, 12, 'salpicado', 'lodo', seed + 3)
        pud(60, 58, 18, 8, 'cadena', 'lodo', seed + 7)
        ferns = [(36, 82, FERN, 'frondoso', 0, False), (182, 104, FERN, 'arborescente', 0, False),
                 (134, 90, FERN, 'ralo', 0, False), (184, 142, FERN, 'joven', 0, False), (110, 140, FERN, 'inclinado', 0, False)]
    elif key == 'Smoke':
        pud(30, 110, 44, 16, 'cadena', 'veneno', seed + 1)
        pud(150, 126, 32, 14, 'rinon', 'lodo', seed + 3)
        pud(26, 60, 22, 10, 'redondo', 'lodo', seed + 8)
        ferns = [(178, 110, FERN_DRY, 'inclinado', .45, True), (36, 92, FERN_DRY, 'frondoso', .2, False),
                 (106, 148, FERN_DRY, 'ralo', .1, False), (196, 148, FERN_DRY, 'joven', 0, False)]
    else:
        ember_crack(cv, 24, 100, 46, seed + 1); taken.append((24, 96, 46, 8))
        ember_crack(cv, 130, 64, 34, seed + 2); taken.append((130, 60, 34, 8))
        pud(150, 118, 30, 14, 'lobulado', 'ceniza', seed + 3)
        ferns = [(40, 88, CHAR, 'arborescente', 1.0, True), (184, 112, CHAR, 'frondoso', 1.0, False),
                 (120, 142, CHAR, 'ralo', 1.0, False)]
    for f in ferns: taken.append((f[0] - 18, f[1] - 30, 36, 34))
    scatter(cv, key, 14, seed + 40, taken, (20, 54, W - 20, H - 18))
    order = sorted(ferns, key=lambda f: f[1])
    for (x, y, rmp, kind, burnt, fire) in order:       # los de arriba, detrás de la rana
        if y <= 100: fern(cv, x, y, rmp, seed + x, kind, burnt, fire)
    drop_shadow(cv, 104, 97, 9, 3)
    paste(cv, fr, 88, 68)
    if key == 'Swamp':
        pl = placoderm()
        if pl: drop_shadow(cv, 72, 84, 6, 2); paste(cv, pl, 60, 64)
        methane_mist(cv, 26, 96, 40, 12, seed + 5)
    elif key == 'Smoke':
        methane_mist(cv, 130, 52, 56, 16, seed + 5)
    else:
        rnd = random.Random(seed)
        for _ in range(12):                           # ascuas sueltas en el aire
            cv.put(rnd.randrange(20, W - 20), rnd.randrange(56, H - 20), C(rnd.choice(EMBER[2:])))
    for (x, y, rmp, kind, burnt, fire) in order:       # los de abajo, delante
        if y > 100: fern(cv, x, y, rmp, seed + x, kind, burnt, fire)
    return cv

def patch(tk, w, h, seed):
    cv = Canvas(w, h); draw_floor(cv, TRAMOS[tk], seed, (0, 0, w, h), wall_shadow=False); return cv

def muestrario(path):
    """Variedad: helechos por tramo, formas de charco, lodo profundo y detalles del suelo, a ×3."""
    rows = []
    kinds = list(FERN_KINDS)
    for tk, rmp, burnt in (('Swamp', FERN, 0), ('Smoke', FERN_DRY, .25), ('Burnt', CHAR, 1.0)):
        cells = []
        for i, k in enumerate(kinds):
            cv = patch(tk, 84, 60, 70 + i)
            fern(cv, 42, 52, rmp, 200 + i * 7, k, burnt if tk == 'Burnt' else burnt * (i % 2), False, flip=i % 2 == 1)
            cells.append(cv)
        rows.append(('Helechos · ' + TRAMOS[tk]['nombre'], cells, ', '.join(kinds)))
    for title, tk, kind, shapes, size in (('Charcos de lodo', 'Smoke', 'lodo', SHAPES, (44, 20)),
                                          ('Agua de charca', 'Swamp', 'agua', SHAPES[:5], (44, 20)),
                                          ('Charcos de veneno', 'Smoke', 'veneno', SHAPES[2:], (44, 20)),
                                          ('Lodo profundo', 'Swamp', 'profundo', ('isla', 'lobulado', 'rinon', 'alargado'), (72, 36))):
        cells = []
        for i, sh in enumerate(shapes):
            w, h = size
            cv = patch(tk, w + 8, h + 8, 90 + i)
            puddle(cv, 4, 4, blob(300 + i * 13 + len(title), sh, w, h), kind, 300 + i)
            cells.append(cv)
        rows.append((title, cells, ', '.join(shapes)))
    for tk in TRAMOS:
        cells = []
        for i, k in enumerate(dict.fromkeys(DECO[tk])):
            cv = patch(tk, 24, 18, 50 + i)
            deco(cv, k, 4, 3, tk, 400 + i)
            cells.append(cv)
        rows.append(('Suelo · ' + TRAMOS[tk]['nombre'], cells, ', '.join(dict.fromkeys(DECO[tk]))))
    sc, pad = 3, 10
    widths = [sum(c.w * sc + pad for c in cells) for _, cells, _ in rows]
    Wd = 20 + max(widths); Hd = 10 + sum(max(c.h for c in cells) * sc + 36 for _, cells, _ in rows)
    im = Image.new('RGBA', (Wd, Hd), C('#0c120e')); d = ImageDraw.Draw(im)
    y = 10
    for title, cells, note in rows:
        d.text((12, y), title + '  ·  ' + note, fill=C('#e8f0c8'), font=_font()); y += 20
        x = 12
        for c in cells:
            im.alpha_composite(c.img().resize((c.w * sc, c.h * sc), Image.NEAREST), (x, y)); x += c.w * sc + pad
        y += max(c.h for c in cells) * sc + 16
    im.save(path)

# =================================================================================================
# Hoja de paletas
# =================================================================================================
def sheet(path):
    S = 20                                             # lado de cada muestra
    F = _font()
    sections = []
    for k, t in TRAMOS.items():
        sections.append(('Tramo · ' + t['nombre'], [('suelo', t['floor'], ''), ('motas', t['fleck'], ''),
                         ('pared (cara)', t['face'], ''), ('pared (tapa)', t['cap'], ''), ('vetas', t['vein'], '')]))
    sections.append(('Terreno y plantas', [
        ('lodo profundo', MUD, 'te hunde; los que andan se hunden'), ('charco de lodo', SLUDGE, 'ralentiza'),
        ('agua de charca', WATER, 'Piel anfibia carga aquí'), ('helecho alto', FERN, 'tapa la vista y arde'),
        ('helecho reseco', FERN_DRY, 'tramo 2'), ('carbón', CHAR, 'quemado y muerte del piso'),
        ('corteza', BARK, 'Lepidodendron / Sigillaria'), ('estróbilo', STROB, 'conos-cofre; morado = secreto')]))
    sections.append(('Reacciones', [
        ('veneno', POISON, 'el lima de siempre'), ('metano', METHANE, 'niebla baja translúcida, fuego fatuo'),
        ('fuego', FIRE, ''), ('nube tóxica', TOXIC, 'fuego + charco de veneno'), ('ámbar', AMBER, 'especiales'),
        ('ascuas', EMBER, 'mecha, grietas')]))
    sections.append(('Clase nueva', [('Parásito', PARASITE, 'granate')]))
    sections.append(('Criaturas (cuerpo · detalle)', [(n, a + ['-'] + b, note) for n, (a, b, note) in CRIATURAS.items()]))
    rows = sum(len(r) + 1 for _, r in sections)
    Wd, Hd = 760, 16 + rows * (S + 4) + len(sections) * 6
    im = Image.new('RGBA', (Wd, Hd), C('#121a14')); d = ImageDraw.Draw(im)
    y = 10
    for title, items in sections:
        d.text((10, y + 4), title, fill=C('#e8f0c8'), font=F); y += S + 4
        for name, rmp, note in items:
            d.text((18, y + 4), name, fill=C('#a8c0a0'), font=F)
            x = 190
            for c in rmp:
                if c == '-': x += 10; continue
                d.rectangle((x, y, x + S - 2, y + S - 2), fill=C(c)); x += S
            if note: d.text((x + 12, y + 4), note, fill=C('#7a9478'), font=F)
            y += S + 4
        y += 6
    im.save(path)

# =================================================================================================
if __name__ == '__main__':
    sheet(os.path.join(OUT, 'hoja_estilo_piso3.png'))
    scenes = [scene(k, 30 + i * 11) for i, k in enumerate(TRAMOS)]
    sc = 3
    pad = 12
    lam = Image.new('RGBA', (pad + len(scenes) * (W * sc + pad), H * sc + pad * 2 + 18), C('#0c120e'))
    d = ImageDraw.Draw(lam)
    for i, (k, s) in enumerate(zip(TRAMOS, scenes)):
        x = pad + i * (W * sc + pad)
        d.text((x, 6), '%d. %s' % (i + 1, TRAMOS[k]['nombre']), fill=C('#e8f0c8'), font=_font())
        lam.alpha_composite(s.img().resize((W * sc, H * sc), Image.NEAREST), (x, 24))
    lam.save(os.path.join(OUT, 'lamina_escala.png'))
    for k, s in zip(TRAMOS, scenes):                   # cada trozo a 1:1, para mirar el tamaño real
        s.img().save(os.path.join(OUT, 'Escena_%s.png' % k))
    muestrario(os.path.join(OUT, 'muestrario_variedad.png'))
    mist = []
    for i in range(8):
        cv = patch('Smoke', 80, 40, 3); methane_mist(cv, 8, 12, 64, 18, 7, i / 8); mist.append(cv)
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    gif(os.path.join(OUT, 'gif', 'Prueba_Metano.gif'), mist, 140, BG, scale=5)
    preview(os.path.join(OUT, 'vista_previa_metano.png'), {'Metano a ras de suelo': mist[:4]}, BG, scale=4)
    anims = {'Player_Frog_Idle_side (prueba)': frog_frames()}
    save_sheet(os.path.join(OUT, 'Prueba_Frog_Idle_side.png'), anims['Player_Frog_Idle_side (prueba)'])
    preview(os.path.join(OUT, 'vista_previa_rana.png'), anims, BG, scale=6)
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    gif(os.path.join(OUT, 'gif', 'Prueba_Frog_Idle_side.gif'), anims['Player_Frog_Idle_side (prueba)'], 160, BG, scale=6)
    print('Listo en', OUT)
