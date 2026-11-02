"""Lepidodendron ancestral · subtanda 6l-3: los decorados del pie del árbol (solo adorno, sin colisión).

- Hongos en repisa (en los lóbulos del tronco) y setas con brillo morado tenue (laten en reposo, se encienden con
  el aviso de las vetas y destellan con el golpe).
- Florecillas moradas: racimos bajos entre el musgo (distintas de Flower_Purple, que se recoge).
- Insectos de adorno: cucaracha primitiva (huye), milpiés pequeño (por las raíces), libelulita, colémbolos (saltan).
- Ambiente: charco oscuro entre lóbulos con el reflejo de un rayo, lianas que bajan de la copa por el tronco.
- Restos (de quienes llegaron antes), casi enterrados al pie del árbol (solo asoma lo más reconocible):
  exoesqueleto de Pulmonoscorpius, alas de paleodictíptero (Mazothairos), trigonotárbido panza arriba, cráneo de
  anfibio primitivo (Proterogyrinus).

Uso: python generar_pie_lepidodendron.py [piezas] [escena]
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import generar_lepidodendron as G                      # tronco, montículo, vetas, copa, luz (y base_piso3 + pixel)
from generar_lepidodendron import *                    # noqa: F401,F403
from PIL import Image, ImageDraw

DECOR = os.path.join(HERE, 'Decor')
FUNG = [C(c) for c in ('#3a3024', '#56472f', '#77653f', '#9a8658', '#bfae80')]       # hongo en repisa (ocre apagado)
STEM = [C(c) for c in ('#6e6858', '#948c76', '#bab29a', '#d8d0b8')]                   # pie de las setas
CAP = [C(c) for c in ('#24182e', '#3e2858', '#5e3c88', '#8a64ba', '#c4a4e8')]         # sombrero morado
GLOW = (160, 122, 208)
PETAL = [C(c) for c in ('#4a2a6a', '#7046a0', '#a07ad0', '#d0b4f0')]
POLLEN = C('#e4d890')
CHITIN = [C(c) for c in ('#2e2620', '#4a3e32', '#6a5a46', '#8e7c60', '#b2a080')]      # quitina vieja, descolorida
BONE = [C(c) for c in ('#4a4638', '#6e6852', '#958e74', '#bab298', '#d6cfb6')]
ROACH = [C(c) for c in ('#20180f', '#3a2c1c', '#5a4428', '#7c6038', '#a08050')]
MILLI = [C(c) for c in ('#1c1614', '#33261e', '#4e3a2a', '#6e5236')]
DRAGON = [C(c) for c in ('#1c3634', '#2e5650', '#4a8074', '#7cb0a0')]
WING = (200, 214, 210)
COLL = [C(c) for c in ('#4a4458', '#726a84', '#9a92ac')]
VINE = [C(c) for c in ('#1a2414', '#2a3a20', '#3e522c', '#587040')]
WATER_D = [C(c) for c in ('#0c1412', '#121c1a', '#1a2824', '#26382f')]

def _sheet(name, frames):
    save_sheet(os.path.join(DECOR, name + '.png'), frames)

# =================================================================================================
# Hongos en repisa y setas
# =================================================================================================
def bracket(n_shelves, w, seed):
    """Hongo en repisa: repisas semicirculares apiladas y desplazadas (vistas un poco desde arriba), con anillos
    de crecimiento, el borde claro y el canto oscuro de abajo."""
    rnd = random.Random(seed)
    W, H = w + 12, n_shelves * 7 + 10
    cv = Canvas(W, H)
    for i in range(n_shelves):
        rx = w / 2 * (1 - i * .14) * rnd.uniform(.82, 1.0)
        ry = rx * .38
        cx, cy = W / 2 + (3 if i % 2 else -3) + rnd.uniform(-1, 1), H - 6 - i * 7
        for y in range(int(cy - ry), int(cy + 3)):
            for x in range(int(cx - rx), int(cx + rx) + 1):
                d = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
                if y <= cy and d > 1: continue
                if y > cy and abs(x - cx) > rx * (1 - (y - cy) / 4): continue    # canto de abajo
                if y > cy: c = FUNG[0] if y > cy + 1 else FUNG[1]
                else:
                    ring = int(math.sqrt(d) * 5)
                    lit = (x - cx) < rx * .25
                    c = FUNG[4] if d > .84 else (FUNG[3] if lit else FUNG[2]) if ring % 2 == 0 else (FUNG[2] if lit else FUNG[1])
                cv.put(x, y, c)
    cv.outline(); cv.clean()
    return cv

def shroom(cv, x, y, h, r, g):
    """Seta: pie de h px y sombrero de radio r; g 0..1 = cuánto brilla el sombrero."""
    for j in range(h):
        cv.put(x, y - j, STEM[2] if j < h - 1 else STEM[1]); cv.put(x + 1, y - j, STEM[1])
    cy = y - h
    for yy in range(int(cy - r * .7), int(cy + 2)):
        for xx in range(int(x - r), int(x + r + 2)):
            d = ((xx - x - .5) / (r + .5)) ** 2 + ((yy - cy) / (r * .7 + .5)) ** 2
            if d > 1 or yy > cy + 1: continue
            lit = (xx - x) < 0 and yy < cy - r * .2
            tone = 2 + (1 if lit else 0) + (1 if g > .6 else 0)
            if yy >= cy: tone = 1 + (1 if g > .6 else 0)
            cv.put(xx, yy, CAP[min(4, tone)])
    if g > .3: cv.put(x - 1, cy - r * .4, CAP[4] if g > .55 else CAP[3])   # punto de brillo
    if g > .85:                                         # encendida: el sombrero entero se aclara y brilla
        for xx in range(int(x - r + 1), int(x + r + 1)): cv.put(xx, cy - 1, CAP[4] if (xx + int(cy)) % 2 else CAP[3])
        cv.put(x, cy - r * .5, C('#f4e8ff'))

def shroom_cluster(kind, g):
    """Racimo de setas (24×20) con halo translúcido según g (0..1)."""
    specs = {'A': [(8, 15, 7, 3), (13, 16, 10, 4), (17, 15, 5, 2)],
             'B': [(5, 16, 4, 2), (9, 17, 6, 2), (12, 15, 3, 2), (15, 16, 5, 2), (19, 17, 4, 2)],
             'C': [(9, 16, 11, 5), (16, 16, 7, 3)]}[kind]
    cv = Canvas(24, 20)
    for (x, y, h, r) in sorted(specs, key=lambda s: s[1]): shroom(cv, x, y, h, r, g)
    for (x, y, h, r) in specs:                          # musgo al pie
        for k in range(-3, 4):
            if (k + x) % 2: cv.put(x + k, y + 1, MOSS[2] if k < 0 else MOSS[1])
    cv.outline(); cv.clean()
    halo = Canvas(24, 20)                               # halo translúcido (debajo)
    if g > .05:
        for (x, y, h, r) in specs:
            cy = y - h
            for yy in range(20):
                for xx in range(24):
                    gr = 3 + 3 * max(0, g - .5) * 2                # el halo crece al encenderse
                    d = math.hypot((xx - x) / (r + gr), (yy - cy) / (r + gr * .7))
                    if d < 1: halo.put(xx, yy, GLOW + (int((60 + 90 * max(0, g - .5) * 2) * g * (1 - d)),))
    out = halo.img().convert('RGBA'); out.alpha_composite(cv.img().convert('RGBA'))
    return out

# =================================================================================================
# Florecillas moradas
# =================================================================================================
def flowerlets(kind, k, n=4):
    """Racimo bajo de florecillas (20×14): 4 pétalos de 1 px y el centro; se mecen 1 px."""
    rnd = random.Random({'A': 1, 'B': 2, 'C': 3}[kind])
    cv = Canvas(20, 14)
    for x in range(3, 17):                              # mata de musgo
        for y in range(10, 13):
            if ((x - 10) / 7) ** 2 + ((y - 11.5) / 1.8) ** 2 <= 1: cv.put(x, y, MOSS[2] if y == 10 else MOSS[1])
    pts = [(rnd.randint(4, 15), rnd.randint(3, 8)) for _ in range({'A': 5, 'B': 7, 'C': 4}[kind])]
    sw = round(math.sin(2 * math.pi * k / n))
    for i, (x, y) in enumerate(sorted(pts, key=lambda p: p[1])):
        ox = sw if i % 2 else 0
        for j in range(y + 1, 11): cv.put(x + (ox if j < y + 3 else 0), j, MOSS[3] if j < 10 else MOSS[2])
        fx = x + ox
        cv.put(fx - 1, y, PETAL[2]); cv.put(fx + 1, y, PETAL[1]); cv.put(fx, y - 1, PETAL[3]); cv.put(fx, y + 1, PETAL[1])
        cv.put(fx, y, POLLEN)
    cv.outline(); cv.clean()
    return cv

# =================================================================================================
# Insectos de adorno (miran a la derecha; el código voltea)
# =================================================================================================
def roach(k, run=True):
    """Cucaracha primitiva vista desde arriba (16×12): Run 4 cuadros (patas alternas), Idle 2 (antenas)."""
    cv = Canvas(16, 12)
    cx, cy = 7, 6
    for y in range(cy - 3, cy + 4):
        for x in range(cx - 5, cx + 5):
            d = ((x - cx + .5) / 5) ** 2 + ((y - cy + .5) / 3) ** 2
            if d > 1: continue
            c = ROACH[3] if (x < cx and y < cy) else ROACH[2]
            if x >= cx + 2: c = ROACH[4] if y < cy else ROACH[3]          # escudo del pronoto (delante)
            if y == cy and x < cx + 2: c = ROACH[1]                        # línea entre las alas
            cv.put(x, y, c)
    cv.outline()
    ph = k % 2
    for i, lx in enumerate((cx - 3, cx, cx + 3)):        # patas finas (sin contorno)
        a = (1 if (i + ph) % 2 else -1) if run else 0
        cv.put(lx + a, cy - 4, ROACH[1]); cv.put(lx + a - (1 if i == 0 else 0), cy - 5, ROACH[1])
        cv.put(lx - a, cy + 4, ROACH[1]); cv.put(lx - a - (1 if i == 0 else 0), cy + 5, ROACH[1])
    tw = (k % 2) if not run else 0
    for j in range(1, 5):                                # antenas largas
        cv.put(cx + 5 + j, cy - 1 - j // 2 - (tw if j > 2 else 0), ROACH[3])
        cv.put(cx + 5 + j, cy + 1 + j // 2, ROACH[3])
    return cv

def millipede(k, n=4):
    """Milpiés pequeño (24×8): 9 anillos con patas que ondulan."""
    cv = Canvas(24, 8)
    for i in range(9):
        x = 3 + i * 2
        y = 4 + round(math.sin(i * .7 + k * .5) * .6)
        for dy in (-1, 0, 1): cv.put(x, y + dy, MILLI[3] if dy < 0 else MILLI[2]); cv.put(x + 1, y + dy, MILLI[2] if dy < 0 else MILLI[1])
    cv.put(21, 4, MILLI[3]); cv.put(22, 4, MILLI[2])
    cv.outline()
    for i in range(9):
        x = 3 + i * 2 + (1 if (i + k) % 2 else 0)
        cv.put(x, 1, MILLI[1]); cv.put(x, 7, MILLI[1])
    return cv

def dragonfly(k, fly=True):
    """Libelulita (16×16): cuerpo fino y 4 alas finas y translúcidas; Fly 2 cuadros (alas largas / cortas =
    borrosas), Perch 1 (alas abiertas, quietas)."""
    cv = Canvas(16, 16)
    for x in range(3, 13): cv.put(x, 8, DRAGON[2] if x > 9 else DRAGON[1])
    cv.put(13, 8, DRAGON[3]); cv.put(12, 7, DRAGON[3]); cv.put(12, 9, DRAGON[3])     # cabeza y ojos
    L = 6 if (not fly or k == 0) else 4
    for bx in (10, 8):                                  # par de delante y par de detrás
        for s in (-1, 1):
            for j in range(1, L + 1):
                x = bx - (1 if j > L * .6 else 0)
                a = 150 - j * 10
                cv.put(x, 8 + s * j, WING + (a,))
                if 1 < j < L: cv.put(x - 1, 8 + s * j, WING + (a - 50,))
    return cv

def springtail(k):
    """Colémbolos (8×8): una mota que salta (4 cuadros) con su sombrita."""
    cv = Canvas(8, 8)
    hop = (0, 2, 3, 1)[k % 4]
    cv.put(4, 7, (10, 14, 12, 110))
    cv.put(3, 6 - hop, COLL[2]); cv.put(4, 6 - hop, COLL[1]); cv.put(3, 5 - hop, COLL[1])
    return cv

# =================================================================================================
# Ambiente
# =================================================================================================
def dark_puddle(k, n=4):
    """Charco oscuro entre lóbulos (48×18) con el reflejo del rayo de luz, que tiembla."""
    cv = Canvas(48, 18)
    for y in range(18):
        for x in range(48):
            d = ((x - 24) / 22) ** 2 + ((y - 9) / 7) ** 2
            d /= (1 + .12 * math.sin(math.atan2(y - 9, x - 24) * 3 + 1)) ** 2
            if d > 1: continue
            c = WATER_D[0] if d < .35 else WATER_D[1] if d < .7 else WATER_D[2]
            if d > .86: c = WATER_D[3] if y < 9 else MOSS[1]
            cv.put(x, y, c)
    sh = math.sin(2 * math.pi * k / n)
    for j in range(9):                                  # reflejo de la luz: una raya rota que tiembla
        x = 18 + j * 1.3 + (sh if j % 3 == 0 else 0)
        y = 6 + j * .55
        if j % 4 != 3: cv.put(x, y, (220, 214, 160, 200 if j % 2 else 140))
    cv.outline(min_alpha=250); cv.clean()
    return cv

def liana(kind, k, n=8):
    """Liana que baja de la copa pegada al tronco (24×144): tallo de 2 px con pares de hojas; se mece poco."""
    rnd = random.Random({'A': 4, 'B': 9}[kind])
    cv = Canvas(24, 144)
    L = {'A': 136, 'B': 104}[kind]
    sw = .9 * math.sin(2 * math.pi * k / n)
    ph = rnd.uniform(0, 6)
    pts = [(12 + 3 * math.sin(j / 19 + ph) + sw * (j / L) ** 1.5 * 3, j) for j in range(L)]
    for j, (x, y) in enumerate(pts):
        cv.put(x, y, VINE[2]); cv.put(x + 1, y, VINE[1])
        if j % 11 == 5 and j > 6:
            s = 1 if (j // 11) % 2 else -1
            for t in range(4):
                cv.put(x + s * (1 + t), y + t * .6, VINE[3] if t < 2 else VINE[2])
                cv.put(x + s * (1 + t), y + t * .6 + 1, VINE[1])
    ex, ey = pts[-1]                                    # punta enroscada
    for t in range(8):
        a = t * .8
        cv.put(ex + math.cos(a) * 2.2, ey + math.sin(a) * 2.2, VINE[2])
    return cv

# =================================================================================================
# Restos de quienes llegaron antes
# =================================================================================================
def _seg(cv, cx, cy, rx, ry, pal, hole=False):
    for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
        for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
            d = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
            if d > 1: continue
            lit = (x - cx) * -.6 + (y - cy) * -.8
            c = pal[3] if lit > rx * .3 else pal[2] if lit > -rx * .3 else pal[1]
            if hole and d < .3: c = pal[0]              # vacío por dentro (roto)
            cv.put(x, y, c)

def bury(spr, visible, seed=0):
    """Casi enterrado: lo que no es `visible(x, y)` se vuelve un bulto de tierra (del color del montículo) con la
    forma vaga del cuerpo y el borde en trama (se funde con el suelo, sin contorno); lo que asoma se mancha de tierra,
    tiene un labio de tierra donde sale del suelo y algo de musgo."""
    rnd = random.Random(seed)
    im = spr.img().convert('RGBA') if hasattr(spr, 'img') else spr.convert('RGBA')
    W, H = im.size
    px = im.load()
    src = [[px[x, y] for y in range(H)] for x in range(W)]
    op = lambda x, y: 0 <= x < W and 0 <= y < H and src[x][y][3] > 0
    soil = set((x, y) for x in range(W) for y in range(H) if op(x, y) and not visible(x, y))
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    o = out.load()
    for x in range(W):
        for y in range(H):
            if not op(x, y): continue
            if (x, y) in soil:
                edge = any(not op(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
                if edge and not dither(.5, x, y): continue                         # borde en trama
                top = not op(x, y - 1) or (x, y - 1) not in soil
                c = SOIL[3] if top else SOIL[1] if not op(x, y + 1) else SOIL[2]
                if rnd.random() < .07: c = MOSS[rnd.choice((1, 2))]
                o[x, y] = c
            else:
                c = src[x][y]
                if c[:3] != OUTL[:3] and c[3] == 255:                              # manchado de tierra, viejo
                    c = blend(c, SOIL[1], .28)
                    if rnd.random() < .05: c = MOSS[2]
                o[x, y] = c
    for (x, y) in soil:                                                             # labio de tierra al salir del suelo
        if op(x, y - 1) and (x, y - 1) not in soil and o[x, y][3]: o[x, y] = SOIL[3]
    return out

def scorpion_remains():
    """Exoesqueleto vacío de Pulmonoscorpius (64×40), visto desde arriba y mirando a la derecha: prosoma, 7 placas
    del mesosoma (dos rotas, vacías), la cola de 5 anillos curvada por encima del cuerpo con el aguijón, un pedipalpo
    con su pinza y el otro roto (la pinza, suelta al lado). Quitina vieja y descolorida."""
    cv = Canvas(64, 40)
    cy = 20
    for i in range(7):                                  # mesosoma (de atrás hacia delante)
        _seg(cv, 14 + i * 3.8, cy, 2.8, 7 - abs(i - 4) * .45, CHITIN, hole=(i in (2, 5)))
    _seg(cv, 42, cy, 5.5, 6, CHITIN)                    # prosoma
    for (x, y) in ((45, cy - 1), (45, cy + 1)): cv.put(x, y, CHITIN[0])               # ojos vacíos
    _seg(cv, 48.5, cy - 1.5, 1.4, 1.1, CHITIN); _seg(cv, 48.5, cy + 1.5, 1.4, 1.1, CHITIN)   # quelíceros
    for (x, y) in ((46, cy - 6), (50, cy - 10), (54, cy - 11)): _seg(cv, x, y, 2.3, 1.7, CHITIN)   # pedipalpo
    _seg(cv, 58.5, cy - 11, 3.6, 2.6, CHITIN); _seg(cv, 62, cy - 12.5, 1.8, 1, CHITIN); _seg(cv, 62, cy - 9.8, 1.6, .9, CHITIN)
    _seg(cv, 46, cy + 6, 2.2, 1.6, CHITIN)              # el otro pedipalpo: roto
    _seg(cv, 55, cy + 14, 3.6, 2.6, CHITIN); _seg(cv, 58.8, cy + 13, 1.8, 1, CHITIN); _seg(cv, 58.6, cy + 15.6, 1.6, .9, CHITIN)
    tail = [(10, cy - 1), (7, cy - 6), (8, cy - 11), (12, cy - 14.5), (17, cy - 15.5)]   # cola curvada sobre el cuerpo
    for j, (x, y) in enumerate(tail): _seg(cv, x, y, 2.6 - j * .15, 2.3 - j * .1, CHITIN)
    _seg(cv, 21.5, cy - 15, 2.4, 1.8, CHITIN)           # telson
    cv.put(24.5, cy - 14, CHITIN[4]); cv.put(25.5, cy - 13, CHITIN[3])               # aguijón
    for (x, y) in ((7, cy + 3), (11, cy + 5)):          # tierra que tapa la parte de atrás
        for yy in range(y - 3, y + 3):
            for xx in range(x - 4, x + 5):
                if ((xx - x) / 4) ** 2 + ((yy - y) / 2.4) ** 2 <= 1 and dither(.75, xx, yy): cv.put(xx, yy, SOIL[2])
    cv.outline(); cv.clean()
    for i, x in enumerate((24, 29, 34, 38)):            # patas (finas, alguna rota)
        for s_ in (-1, 1):
            if (i, s_) == (1, 1): continue
            for t in range(4 + (i % 2)): cv.put(x - t * .4, cy + s_ * (7.5 + t), CHITIN[2] if t < 3 else CHITIN[1])
    return cv

def wings_remains():
    """Dos alas sueltas de paleodictíptero (Mazothairos), cruzadas en el musgo (48×24): largas y estrechas, punta
    redonda, membrana pálida translúcida con nervios y manchas oscuras."""
    cv = Canvas(48, 24)
    for (x0, y0, ang, L, wd) in ((4, 13, -.34, 40, 4.0), (5, 14, .3, 36, 3.6)):     # en V, aún unidas por la base
        ca, sa = math.cos(ang), math.sin(ang)
        for t in range(L):
            u = t / L
            wv = wd * (min(1, u * 4) ** .5) * (1 - max(0, u - .8) / .2 * .55)       # base estrecha, punta redonda
            for s_ in range(-int(wv + .5), int(wv + .5) + 1):
                x, y = x0 + t * ca - s_ * sa, y0 + t * sa + s_ * ca
                rim = abs(s_) >= wv - .3
                spot = (int(u * 7) in (2, 4, 6)) and abs(s_) < wv * .75 and not (int(u * 7) == 2 and s_ > 0)
                vein = s_ == 0 or (t % 6 == 0 and abs(s_) < wv)
                if rim: c = (96, 86, 66, 230)
                elif spot: c = (58, 42, 32, 235)
                elif vein: c = (120, 110, 86, 220)
                else: c = (186, 178, 150, 150)
                cv.put(x, y, c)
    return cv

def trigonotarbid_remains():
    """Trigonotárbido panza arriba (20×16): abdomen redondo de placas, patas encogidas al cielo."""
    cv = Canvas(20, 16)
    _seg(cv, 11, 9, 5.5, 4.6, CHITIN)
    for i in range(3):
        for x in range(7, 16): cv.put(x, 6 + i * 2.2, CHITIN[1])                           # placas del vientre
    _seg(cv, 5, 9, 2.6, 2.4, CHITIN)
    cv.outline(); cv.clean()
    for i, x in enumerate((6, 8, 10, 12)):              # 8 patas encogidas (finas)
        for s in (-1, 1):
            for t in range(3): cv.put(x - t * .4, 9 + s * (4 + t), CHITIN[2] if t < 2 else CHITIN[1])
            cv.put(x - 1.4, 9 + s * 7 - s, CHITIN[1])
    return cv

def skull_remains():
    """Cráneo de anfibio primitivo (Proterogyrinus) visto desde arriba y mirando a la derecha (38×28), medio
    enterrado por detrás: hocico ancho y redondeado, tabla craneal recta atrás con los cuernos en las esquinas,
    órbitas a media cabeza, narinas, dientes en el borde y la superficie llena de hoyitos."""
    cv = Canvas(38, 28)
    cy = 14
    def half(x):                                        # medio ancho del cráneo en la columna x
        u = (x - 4) / 30
        if u < 0 or u > 1: return -1
        if u > .45: return 11.5 * max(0, 1 - ((u - .45) / .55)) ** .62 + (1.2 if u < .98 else 0)   # hocico ancho que se afila
        return 11.5 + (1.6 if u < .1 else 0)            # tabla craneal (y los cuernos atrás)
    for y in range(28):
        for x in range(38):
            h = half(x)
            if h < 0 or abs(y - cy) > h: continue
            if x < 6 and abs(y - cy) < 8: continue      # muesca entre los cuernos
            lit = (x - 19) * -.35 + (y - cy) * -.9
            t_ = 3 if lit > 2.5 else 2 if lit > -3 else 1
            if (x * 5 + y * 7) % 9 == 0 or (x * 3 + y * 11) % 13 == 0: t_ -= 1                 # hoyitos
            c = BONE[t_]
            if abs(y - cy) > h - 1 and x > 16 and x % 2: c = BONE[4]                         # dientes en el borde
            cv.put(x, y, c)
    for (ox, oy) in ((19, cy - 4.5), (19, cy + 4.5)):   # órbitas
        for y in range(int(oy) - 3, int(oy) + 4):
            for x in range(ox - 4, ox + 4):
                d = ((x - ox + .5) / 3.2) ** 2 + ((y - oy) / 2.6) ** 2
                if d <= 1: cv.put(x, y, (26, 24, 20, 255) if d < .6 else BONE[0])
    for y in (cy - 2, cy + 2): cv.put(32, y, BONE[0]); cv.put(31, y, BONE[0])     # narinas
    for y in range(28):                                 # la parte de atrás, enterrada
        for x in range(0, 13):
            if (x, y) in cv.px and dither(max(0, 1 - x / 12), x, y): cv.put(x, y, SOIL[2] if y < cy else SOIL[1])
    cv.outline(); cv.clean()
    return cv

def build_all():
    out = {}
    for nm, (n, w, sd) in {'S': (2, 18, 1), 'M': (3, 26, 2), 'L': (4, 34, 3)}.items():
        out[f'Lepido_Fungus_{nm}'] = [bracket(n, w, sd)]
    for kind in 'ABC':
        out[f'Lepido_Shroom{kind}_Idle'] = [shroom_cluster(kind, .25 + .2 * math.sin(2 * math.pi * k / 6)) for k in range(6)]
        out[f'Lepido_Shroom{kind}_Warn'] = [shroom_cluster(kind, g) for g in (.8, 1.0, .9, 1.0)]
        out[f'Lepido_Shroom{kind}_Hit'] = [shroom_cluster(kind, g) for g in (1.0, .6)]
        out[f'Lepido_Flowerlets{kind}'] = [flowerlets(kind, k) for k in range(4)]
    out['Critter_Roach_Run'] = [roach(k, True) for k in range(4)]
    out['Critter_Roach_Idle'] = [roach(k, False) for k in range(2)]
    out['Critter_Millipede_Crawl'] = [millipede(k) for k in range(4)]
    out['Critter_Dragonfly_Fly'] = [dragonfly(k, True) for k in range(2)]
    out['Critter_Dragonfly_Perch'] = [dragonfly(0, False)]
    out['Critter_Springtail_Hop'] = [springtail(k) for k in range(4)]
    out['Lepido_DarkPuddle'] = [dark_puddle(k) for k in range(4)]
    for kind in 'AB': out[f'Lepido_Liana{kind}_Sway'] = [liana(kind, k) for k in range(8)]
    # restos casi enterrados al pie del árbol: solo asoma lo más reconocible
    out['Remains_Pulmonoscorpius'] = [bury(scorpion_remains(), lambda x, y: (x < 28 and y < 13) or (x > 49 and y < 11), 1)]
    out['Remains_PaleoWings'] = [bury(wings_remains(), lambda x, y: x > 24 and y < 12, 2)]
    out['Remains_Trigonotarbid'] = [bury(trigonotarbid_remains(), lambda x, y: y < 8 or y > 12 and (x % 3), 3)]
    out['Remains_AmphibianSkull'] = [bury(skull_remains(), lambda x, y: x > 14 and 6 < y < 22, 4)]
    PAD = 2                                             # margen transparente: nada toca el borde de la celda
    for name, frames in out.items():                    # (las lianas no: nacen del borde de arriba, de la copa)
        if name.startswith('Lepido_Liana'): continue
        padded = []
        for f in frames:
            im = f.img().convert('RGBA') if hasattr(f, 'img') else f
            big = Image.new('RGBA', (im.width + 2 * PAD, im.height + 2 * PAD), (0, 0, 0, 0))
            big.alpha_composite(im, (PAD, PAD)); padded.append(big)
        out[name] = padded
    return out

def piezas():
    os.makedirs(DECOR, exist_ok=True)
    allp = build_all()
    for name, frames in allp.items(): _sheet(name, frames)
    small = {k: v for k, v in allp.items() if not k.startswith('Lepido_Liana')}
    preview(os.path.join(HERE, 'vista_previa_6l3_piezas.png'), small, G.BG, 4, cols=8)
    gif(os.path.join(GIF, 'Lepido_Shroom_Warn.gif'), allp['Lepido_ShroomA_Idle'] * 2 + allp['Lepido_ShroomA_Warn'] * 2 + allp['Lepido_ShroomA_Hit'] + allp['Lepido_ShroomA_Idle'], 140, G.BG, 6)
    gif(os.path.join(GIF, 'Critter_Roach_Run.gif'), allp['Critter_Roach_Run'], 70, '#2a2d25', 8)
    print('   Decor/ escrito:', len(allp), 'hojas')
    return allp

DECOR_LAYOUT = [   # (pieza, x, y en px respecto al pie del tronco, cuadro/fase, volteo) — la colocación de la escena
    ('Lepido_Fungus_L', -96, -40, 0, False), ('Lepido_Fungus_M', -70, -18, 0, False), ('Lepido_Fungus_S', 84, -30, 0, True),
    ('Lepido_Fungus_M', 104, -10, 0, True), ('Lepido_Fungus_S', 30, -52, 0, False),
    ('Lepido_ShroomA', -128, 14, 0, False), ('Lepido_ShroomB', -40, 30, 2, False), ('Lepido_ShroomC', 120, 22, 4, True),
    ('Lepido_ShroomB', 60, 36, 1, True), ('Lepido_ShroomA', 158, 4, 3, True),
    ('Lepido_FlowerletsA', -150, 30, 0, False), ('Lepido_FlowerletsB', -104, 44, 1, False), ('Lepido_FlowerletsC', -16, 48, 2, False),
    ('Lepido_FlowerletsA', 30, 52, 3, True), ('Lepido_FlowerletsB', 88, 44, 0, True), ('Lepido_FlowerletsC', 140, 34, 1, False),
    ('Lepido_FlowerletsB', -182, 10, 2, True), ('Lepido_FlowerletsA', 186, 16, 1, False),
    ('Lepido_DarkPuddle', -64, 46, 0, False),
    ('Remains_Pulmonoscorpius', 118, 58, 0, False), ('Remains_AmphibianSkull', -118, 52, 0, False),
    ('Remains_PaleoWings', 40, 74, 0, False), ('Remains_Trigonotarbid', -96, 72, 0, True),
]

def escena():
    """El pie del árbol con todos los decorados, los insectos moviéndose y las setas reaccionando al aviso y al golpe."""
    import generar_escala_lepidodendron as E
    allp = build_all()
    def fr(name, k):
        f = allp[name][k % len(allp[name])]
        return f.img().convert('RGBA') if hasattr(f, 'img') else f
    W, H = 50, 30
    fx, fy = 25, 15.5
    FX, FY = int(fx * U), int(fy * U)
    room = A.room(A.rect_room(W, H, (2, int(fy - 10.5), W - 3, H - 2)))
    def put(im, c, px, py):
        im.alpha_composite(c.img().convert('RGBA') if hasattr(c, 'img') else c, (FX - px, FY - py))
    put(room, G.back_roots(), G.BFX, G.BFY)
    m, sh = G.mound()
    put(room, sh, G.MFX, G.MFY); put(room, m, G.MFX, G.MFY)
    for (nm, x, y, k, flip) in DECOR_LAYOUT:            # suelo: charco y restos (estáticos) y flores (se mecen aparte)
        if nm.startswith(('Remains', 'Lepido_DarkPuddle')): continue
    trunk_im = G.trunk().img().convert('RGBA')
    tiles = [G.dapple_tile(k).img().convert('RGBA') for k in range(8)]
    vein = {('rest', 0): G.veins('rest').img().convert('RGBA')}
    canopy = [G.canopy(G.ARENA_W, G.ARENA_H, holes=G.HOLES, sway=.16 * math.sin(2 * math.pi * k / 8)).img().convert('RGBA') for k in range(8)]
    shafts = [G.light_shaft(k).img().convert('RGBA') for k in range(8)]
    frog = A.frame(FROG, 0, 32)
    cx0, cy0 = fx - 20, fy - 8.75 - 1.5
    X0, Y0 = int(cx0 * U), int(cy0 * U)
    seq = [('rest', 0)] * 8 + [('warn', k) for k in range(8)] + [('rest', 0)] * 4 + [('hit', 0), ('hit', 1)] + [('rest', 0)] * 8
    frames = []
    for t, (vm, vk) in enumerate(seq):
        im = room.copy()
        dl = Image.new('RGBA', im.size, (0, 0, 0, 0))
        for ty in range(0, im.height, 256):
            for tx in range(0, im.width, 256): dl.alpha_composite(tiles[(t // 2) % 8], (tx, ty))
        im.alpha_composite(dl)
        for (nm, x, y, k, flip) in sorted(DECOR_LAYOUT, key=lambda d: d[2]):
            if nm.startswith('Lepido_Fungus'): continue
            if nm.startswith('Lepido_Shroom'):
                st = 'Warn' if vm == 'warn' else 'Hit' if vm == 'hit' else 'Idle'
                f = fr(f'{nm}_{st}', (t + k) if st != 'Hit' else vk)
            elif nm.startswith('Lepido_Flowerlets'): f = fr(nm, (t // 2 + k))
            else: f = fr(nm, 0)
            if flip: f = f.transpose(Image.FLIP_LEFT_RIGHT)
            im.alpha_composite(f, (FX + x - f.width // 2, FY + y - f.height + 4))
        put(im, trunk_im, G.OX, G.FOOT)
        key = (vm, vk)
        if key not in vein: vein[key] = G.veins(vm, vk).img().convert('RGBA')
        put(im, vein[key], G.OX, G.FOOT)
        for (nm, x, y, k, flip) in DECOR_LAYOUT:          # hongos en repisa: pegados al tronco
            if nm.startswith('Lepido_Fungus'):
                f = fr(nm, 0)
                if flip: f = f.transpose(Image.FLIP_LEFT_RIGHT)
                im.alpha_composite(f, (FX + x - f.width // 2, FY + y - f.height // 2))
        for (nm, x, kk) in (('Lepido_LianaA', -40, 0), ('Lepido_LianaB', 28, 3)):     # lianas por el tronco
            f = fr(f'{nm}_Sway', t + kk)
            im.alpha_composite(f, (FX + x - 12, Y0 + 26))
        # insectos: cucaracha que cruza, milpiés por una raíz, libelulita posada / volando, colémbolos
        rx = -170 + t * 7
        im.alpha_composite(fr('Critter_Roach_Run', t), (FX + rx, FY + 62))
        im.alpha_composite(fr('Critter_Millipede_Crawl', t), (FX + 70 + t, FY + 2))
        if t < 14: im.alpha_composite(fr('Critter_Dragonfly_Perch', 0), (FX - 132, FY + 26))
        else:
            dfx, dfy = FX - 132 + (t - 14) * 9, FY + 26 - (t - 14) * 5 + math.sin(t) * 3
            im.alpha_composite(fr('Critter_Dragonfly_Fly', t), (int(dfx), int(dfy)))
        for j, (sx, sy) in enumerate(((-20, 64), (-12, 70), (-4, 62))):
            im.alpha_composite(fr('Critter_Springtail_Hop', t + j), (FX + sx, FY + sy))
        im.alpha_composite(frog, (FX - 7 * U, FY + 5 * U))
        for j, (hx, hr) in enumerate(G.HOLES):
            gx = X0 - 32 + hx; gy = Y0 + G.CANOPY_Y + int(G.ARENA_H * .58 * .62)
            im.alpha_composite(shafts[(t + j * 3) % 8], (int(gx - (1.4 * U / 2 + 8)), gy))
        im.alpha_composite(canopy[t % 8], (X0 - 32, Y0 + G.CANOPY_Y))
        frames.append(im.crop((X0, Y0, X0 + 640, Y0 + 360)))
    os.makedirs(os.path.join(GIF, 'escenas'), exist_ok=True)
    out = [f.convert('RGB') for f in frames]
    ms = [150] * 8 + [70] * 8 + [110] * 4 + [120] * 2 + [150] * 8
    out[0].save(os.path.join(GIF, 'escenas', 'Lepido_pie.gif'), save_all=True, append_images=out[1:], duration=ms, loop=0)
    # lámina: un cuadro en reposo a ×2 (recorte del pie) y el aviso a ×2
    def crop_base(f):
        x0, y0 = FX - X0 - 230, FY - Y0 - 110
        return f.crop((x0, y0, x0 + 460, y0 + 210)).resize((920, 420), Image.NEAREST)
    a, b = crop_base(frames[4]), crop_base(frames[14])
    sheet = Image.new('RGBA', (920, 420 * 2 + 52), (16, 18, 16, 255))
    d = ImageDraw.Draw(sheet)
    sheet.alpha_composite(a, (0, 24)); sheet.alpha_composite(b, (0, 24 + 420 + 28))
    d.text((8, 4), 'reposo: las setas laten despacio', fill=(120, 230, 230), font=A.font(16))
    d.text((8, 420 + 28 + 4), 'aviso: las setas se encienden a la vez que las vetas', fill=(120, 230, 230), font=A.font(16))
    G._save(sheet, 'Lepidodendron ancestral · 6l-3 el pie del árbol con sus decorados (×2)',
            ['Hongos en repisa, setas moradas, florecillas, charco con reflejo, lianas, restos (Pulmonoscorpius, alas de',
             'paleodictíptero, trigonotárbido, cráneo de anfibio) e insectos. GIF: gif/escenas/Lepido_pie.gif'],
            'vista_previa_6l3_pie.png')

if __name__ == '__main__':
    which = sys.argv[1:] or ['piezas', 'escena']
    if 'piezas' in which: piezas()
    if 'escena' in which: escena()
