"""Aspecto nuevo de las especiales del ámbar (piso 3; ver README). Subtanda 1: el bloque de ámbar y la gota.

El bloque (Cápsula de ámbar sobre el jugador, Gota de ámbar sobre un enemigo) va en DOS capas para que lo encerrado
se vea dentro: `Back` (el ámbar del fondo, detrás de la criatura) y `Front` (translúcida: tiñe a la criatura y lleva
el borde, las facetas, el brillo y las grietas). El código dibuja la criatura entre las dos y escala el conjunto
según su tamaño. Sustituye a Fx_AmberBlock_* de floor3-mutations.
Hecho con la skill primordia-sprites (pixel.py): 16 px por unidad, luz arriba a la izquierda, contorno OUTL.

Uso: python generar_especiales_v2.py
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
TEMP = os.path.normpath(os.path.join(HERE, '..'))
sys.path.insert(0, os.path.join(TEMP, 'floor3-environment'))
from base_piso3 import *
from PIL import Image

AMB = [C(c) for c in AMBER]                       # #6a2e0e #b0581a #e89a30 #ffcc5a #fff0b0
W, H = 48, 56
CX, CY, RX, RY = 24, 32, 19.5, 19.5               # el bloque: óvalo algo más alto que ancho, apoyado abajo
FLOOR = int(CY + RY)                                 # y del suelo (51,5)

def inside(x, y, rx=RX, ry=RY, k=0.0):
    """Óvalo del bloque con dos facetas planas (arriba a la izquierda y abajo a la derecha) para que parezca tallado."""
    u, v = (x + .5 - CX) / rx, (y + .5 - CY) / ry
    if u * u + v * v > 1: return False
    if (-u - v) > 1.18 or (u + v) > 1.22: return False                  # facetas
    return True

def facet_lines(x, y):
    """Aristas de las facetas: rectas que cruzan el bloque."""
    u, v = (x + .5 - CX) / RX, (y + .5 - CY) / RY
    return abs((-u - v) - 1.05) < .045 or abs((u + v) - 1.08) < .045 or abs((u * .5 - v) + .35) < .035

def block_back(level=1.0):
    """El ámbar del fondo (detrás de la criatura): más oscuro y casi opaco, abajo a la derecha más hondo.
    level: altura que ha subido la resina (0..1; formación)."""
    cv = Canvas(W, H); top = FLOOR - (FLOOR - (CY - RY)) * level
    for y in range(H):
        for x in range(W):
            if not inside(x, y) or y < top: continue
            u, v = (x + .5 - CX) / RX, (y + .5 - CY) / RY
            c = AMB[0] if u + v > .55 else AMB[1]
            cv.put(x, y, c[:3] + (235,))
    return cv

def block_front(level=1.0, glint=-1.0, cracks=0, bubbles=True):
    """La cara de delante, translúcida: tinte ámbar, borde claro arriba a la izquierda y oscuro abajo a la derecha,
    aristas de las facetas, burbujitas, un brillo que la cruza (glint 0..1) y grietas (cracks 0-3)."""
    cv = Canvas(W, H); top = FLOOR - (FLOOR - (CY - RY)) * level
    for y in range(H):
        for x in range(W):
            if not inside(x, y) or y < top: continue
            u, v = (x + .5 - CX) / RX, (y + .5 - CY) / RY
            edge = not (inside(x + 1, y) and inside(x - 1, y) and inside(x, y + 1) and inside(x, y - 1))
            edge2 = not (inside(x + 2, y) and inside(x - 2, y) and inside(x, y + 2) and inside(x, y - 2))
            surf = y < top + 2 and level < 1
            if edge or surf: c = AMB[3] if (u + v < 0 or surf) else AMB[1]; a = 255
            elif edge2: c = AMB[4] if u + v < -.6 else AMB[2] if u + v < .4 else AMB[0]; a = 230 if u + v < -.6 else 150
            elif facet_lines(x, y): c = AMB[3]; a = 150
            else: c = AMB[2] if u + v < .2 else AMB[1]; a = 95
            cv.put(x, y, c[:3] + (a,))
    if glint >= 0 and level >= 1:                                          # brillo que cruza en diagonal
        for y in range(H):
            for x in range(W):
                if inside(x, y) and abs((x - CX) + (y - CY) * .7 - (glint * 70 - 35)) < 1.6:
                    cv.put(x, y, AMB[4][:3] + (210,))
    if bubbles and level >= .6:
        for (bx, by) in ((14, 42), (31, 21), (18, 25), (34, 40)):
            if inside(bx, by) and by >= top + 2: cv.put(bx, by, AMB[4][:3] + (200,)); cv.put(bx + 1, by + 1, AMB[3][:3] + (140,))
    if level >= 1:
        for (x, y) in ((11, 22), (12, 20), (13, 19), (15, 18)): cv.put(x, y, AMB[4])   # reflejo fuerte arriba a la izquierda
    CR = [((24, 14), [(22, 19), (25, 24), (21, 29)]), ((41, 32), [(36, 33), (31, 37), (28, 35)]),
          ((7, 36), [(12, 37), (16, 42), (21, 41)]), ((30, 50), [(28, 45), (31, 41)])]
    for i in range(min(cracks, 3) + (1 if cracks >= 3 else 0)):           # grietas que se extienden
        p0, pts = CR[i]
        prev = p0
        for q in pts:
            n = int(max(abs(q[0] - prev[0]), abs(q[1] - prev[1]))) + 1
            for t in range(n + 1):
                x = round(prev[0] + (q[0] - prev[0]) * t / n); y = round(prev[1] + (q[1] - prev[1]) * t / n)
                if inside(x, y): cv.put(x, y, AMB[4]); cv.put(x + 1, y, AMB[0][:3] + (200,)) if inside(x + 1, y) else None
            prev = q
    outline_in(cv)
    return cv

def outline_in(cv):
    """Contorno OUTL alrededor del bloque (solo donde hay píxeles opacos del borde)."""
    add = [(x, y) for y in range(H) for x in range(W) if (x, y) not in cv.px and any(
        (x + dx, y + dy) in cv.px and cv.px[(x + dx, y + dy)][3] >= 230 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for p_ in add: cv.px[p_] = OUTL

def shadow(cv, rx=17, ry=2.5):
    for y in range(int(FLOOR - ry + 1), int(FLOOR + ry + 1)):
        for x in range(int(CX - rx), int(CX + rx) + 1):
            if ((x + .5 - CX) / rx) ** 2 + ((y + .5 - FLOOR - .5) / ry) ** 2 <= 1: cv.put_out(x, y, (24, 20, 37, 90))
    return cv

# --- animaciones -----------------------------------------------------------------------------------
def form():
    """La resina sube del suelo envolviendo a la criatura y se endurece (6 cuadros): superficie con gotas que suben,
    al final un destello. Hojas Back y Front con los mismos cuadros."""
    levels = (.12, .3, .52, .74, .92, 1.0)
    back = [shadow(block_back(l)) for l in levels]
    front = []
    for i, l in enumerate(levels):
        cv = block_front(l, glint=.15 if i == 5 else -1)
        if l < 1:                                                           # gotas que salpican en la superficie
            top = int(FLOOR - (FLOOR - (CY - RY)) * l)
            for k, dx in enumerate((-10, -2, 7, 13)):
                x = CX + dx; y = top - 1 - (i + k) % 3
                if 0 <= y: cv.put(x, y, AMB[3]); cv.put(x, y + 1, AMB[2])
        front.append(cv)
    return back, front

def hold():
    """Encerrado (8 cuadros en bucle): un brillo cruza el bloque cada vuelta y las burbujas titilan."""
    return [shadow(block_back())] * 1, [block_front(glint=(f / 8 * 1.6) if f < 5 else -1) for f in range(8)]

def crack():
    """Grietas en 3 fases (el código elige la fase: por tiempo en la Cápsula, por golpes o tiempo en la Gota)."""
    return [block_front(cracks=k) for k in (1, 2, 3)]

def shatter():
    """Revienta (8 cuadros): destello con grietas por todo, el bloque se parte en esquirlas que salen despedidas,
    giran y caen, y polvo dorado. Sin capa Back: la criatura queda libre desde el cuadro 1."""
    SW, SH, OX, OY = 80, 72, 16, 10                                        # celda mayor: las esquirlas vuelan lejos
    frames = []
    cv0 = block_front(cracks=4)
    for (x, y), c in list(cv0.px.items()):
        if c != OUTL and c[3] < 230: cv0.px[(x, y)] = AMB[3][:3] + (200,)
    shadow(cv0); cv = Canvas(SW, SH); paste(cv, cv0, OX, OY); frames.append(cv)
    rnd = random.Random(7)
    shards = []
    for k in range(10):
        a = k / 10 * 2 * math.pi + rnd.uniform(-.2, .2)
        shards.append((a, rnd.uniform(1.6, 2.8), rnd.choice((2, 3, 3, 4)), rnd.uniform(-1, 1), rnd.random() < .5))
    for f in range(1, 8):
        cv = Canvas(SW, SH); t = f / 7
        for (a, sp, size, spin, light) in shards:
            r = 6 + sp * f * 1.6
            x = OX + CX + math.cos(a) * r; y = OY + CY + math.sin(a) * r * .8 + (f * f) * .45     # cae
            if y > OY + FLOOR + 2: y = OY + FLOOR + 2 - (f % 2)
            if t > .85 and (k := int(x + y)) % 2: continue
            ang = spin * f
            for i in range(-size, size + 1):
                for j in range(-1, 2):
                    px_ = x + math.cos(ang) * i - math.sin(ang) * j * .8
                    py_ = y + math.sin(ang) * i + math.cos(ang) * j * .8
                    cv.put(px_, py_, AMB[3] if (light and i < 0) else AMB[2] if j < 1 else AMB[1])
        cv.outline(); cv.clean()
        for k in range(8 - f):                                             # polvo dorado
            a = k * 1.3 + f * .4; r = 8 + f * 4
            cv.put(OX + CX + math.cos(a) * r, OY + CY + math.sin(a) * r * .7, AMB[4][:3] + (max(60, 220 - f * 25),))
        frames.append(cv)
    return frames

def amber_drop(f):
    """Gota de ámbar lanzada (16×16, 4 cuadros, mirando a la derecha): gota brillante estirada con el reflejo y
    gotitas detrás."""
    cv = Canvas(16, 16); wob = [0, .3, 0, -.3][f]
    rot_ellipse(cv, 9, 8 + wob, 4.2, 2.8, 0, AMB[1:])
    cv.put(10, 7, AMB[4]); cv.put(11, 7, AMB[4]); cv.put(10, 6, AMB[3])
    cv.outline(); cv.clean()
    for k, (dx, a) in enumerate(((3, 230), (1, 160))):                    # gotitas que siguen
        cv.put(dx + (f % 2), 8 + (k - f % 2) * .6, AMB[2][:3] + (a,))
    return cv

def drop_hit(f):
    """La gota estalla al dar (24×24, 5 cuadros): salpicadura de resina en corona y gotas; luego se forma el bloque."""
    cv = Canvas(24, 24); r = 2 + f * 2.2
    for k in range(10):
        a = k / 10 * 2 * math.pi
        x, y = 12 + math.cos(a) * r, 12 + math.sin(a) * r * .8
        if f < 4: cv.put(x, y, AMB[3] if k % 2 else AMB[2]); cv.put(x + math.cos(a), y + math.sin(a) * .8, AMB[1]) if f > 1 else None
        else: cv.put(x, y, AMB[2][:3] + (120,))
    if f < 2:
        ellipse(cv, 12, 12, 3 - f, 2.4 - f * .6, AMB[1:]); cv.put(11, 11, AMB[4])
    return cv

# =================================================================================================
# Subtanda 2: nivel III
# =================================================================================================
HEART = [C(c) for c in RED4]                      # #a33b3b #e05a5a #ff9a8a #fff0e0

def star(cv, x, y, L, c_core, c_arm, diag=True):
    """Destello en estrella: cruz larga y, si diag, aspas cortas."""
    cv.put(x, y, c_core)
    for k in range(1, L + 1):
        c = c_arm if k > 1 else c_core
        for dx, dy in ((k, 0), (-k, 0), (0, k), (0, -k)): cv.put(x + dx, y + dy, c)
    if diag:
        for k in range(1, max(2, L // 2)):
            for dx, dy in ((k, k), (-k, k), (k, -k), (-k, -k)): cv.put(x + dx, y + dy, c_arm)

def cerci_burst(f):
    """Cercos III: estallido de chispas en estrella al quebrar (56×56, 7 cuadros; centrado en el cuerpo). Destello
    blanco y dorado que se abre en cruz, 8 chispas que saltan y caen titilando."""
    cv = Canvas(56, 56); cx, cy = 28, 28
    if f < 3:
        star(cv, cx, cy, [5, 9, 6][f], WHITE4[3] if f < 2 else AMB[4], AMB[4] if f < 2 else AMB[3], diag=f > 0)
    rnd = random.Random(3)
    for k in range(8):
        a = k / 8 * 2 * math.pi + .2; sp = rnd.uniform(2.2, 3.2)
        if f < 1: continue
        r = 4 + sp * f * 1.0
        x, y = cx + math.cos(a) * r, cy + math.sin(a) * r + (f * f) * .12
        if f >= 6 and k % 2: continue
        c = AMB[4] if (f + k) % 2 else AMB[3]
        if f < 5: star(cv, round(x), round(y), 1 if f > 2 else 2, c, AMB[2], diag=False)
        else: cv.put(x, y, c)
    return cv

# --- Cápsula III: doble capa ---------------------------------------------------------------------
OUT_RX, OUT_RY, OUT_CY = RX + 3, RY + 2.5, CY - 2

def in_outer(x, y):
    u, v = (x + .5 - CX) / OUT_RX, (y + .5 - OUT_CY) / OUT_RY
    return u * u + v * v <= 1 and y <= FLOOR

def outer_shell(cracks=0, level=1.0):
    """La cáscara exterior de la Cápsula III (delante): banda gruesa y oscura con vetas doradas, borde claro arriba a
    la izquierda; deja ver el bloque de dentro. Grietas (0-3) solo en esta capa."""
    cv = Canvas(W, H); top = FLOOR - (FLOOR - (OUT_CY - OUT_RY)) * level
    n = noise(31)
    for y in range(H):
        for x in range(W):
            if not in_outer(x, y) or inside(x, y) or y < top: continue
            u, v = (x + .5 - CX) / OUT_RX, (y + .5 - OUT_CY) / OUT_RY
            edge = not (in_outer(x + 1, y) and in_outer(x - 1, y) and in_outer(x, y - 1))
            vein = abs(n(x, y, 5) - .5) < .06
            if edge: c = AMB[3] if u + v < 0 else AMB[1]
            elif vein: c = AMB[3] if u + v < .3 else AMB[2]
            else: c = AMB[1] if u + v < -.3 else AMB[0]
            cv.put(x, y, c[:3] + (245,))
    CR = [((9, 20), [(12, 24), (10, 28)]), ((40, 18), [(37, 22), (39, 27)]), ((24, 10), [(23, 13), (26, 15)])]
    for k in range(min(3, cracks)):
        p0, pts = CR[k]; prev = p0
        for q in pts:
            n_ = int(max(abs(q[0] - prev[0]), abs(q[1] - prev[1]))) + 1
            for t in range(n_ + 1):
                x = round(prev[0] + (q[0] - prev[0]) * t / n_); y = round(prev[1] + (q[1] - prev[1]) * t / n_)
                if (x, y) in cv.px: cv.put(x, y, AMB[4])
            prev = q
    outline_in(cv)
    return cv

def core_glow(f, n=8):
    """Núcleo cálido que late (capa Back de la Cápsula III, detrás de la criatura)."""
    cv = block_back()
    br = .5 + .5 * math.sin(f / n * 2 * math.pi)
    for y in range(H):
        for x in range(W):
            d = math.hypot((x + .5 - CX) / 11, (y + .5 - (FLOOR - 13)) / 9)
            if d < 1 and inside(x, y):
                k = (1 - d) * (.6 + .4 * br)
                if k > .55: cv.put(x, y, AMB[4][:3] + (245,))
                elif k > .3: cv.put(x, y, AMB[3][:3] + (240,))
                elif k > .12: cv.put(x, y, AMB[2][:3] + (238,))
    return shadow(cv)

def half_heart(cv, x, y, a=255):
    """Corazoncito de 7×6 (uno por cada medio corazón curado), rosado con brillo y contorno."""
    shape = [".##.##.", "#######", "#######", ".#####.", "..###..", "...#..."]
    for j, row in enumerate(shape):
        for i_, ch in enumerate(row):
            if ch == '#':
                c = HEART[3] if (i_, j) in ((1, 1), (1, 2)) else HEART[2] if j < 2 and i_ < 3 else HEART[1] if j < 4 else HEART[0]
                cv.put(x + i_, y + j, c[:3] + (a,))
    for j, row in enumerate(shape):
        for i_, ch in enumerate(row):
            if ch != '#': continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                xx, yy = i_ + dx, j + dy
                if not (0 <= yy < 6 and 0 <= xx < 7 and shape[yy][xx] == '#'): cv.put_out(x + xx, y + yy, OUTL[:3] + (a,))

def capsule_heal(f):
    """Cápsula III curando (12 cuadros en bucle mientras dura, 48×56, por encima de todo): pulsos cálidos que salen
    del núcleo y un medio corazón que sube por cada medio que cura (salen en los cuadros 1, 5 y 9)."""
    cv = Canvas(W, H); cy = FLOOR - 13
    r = (f % 4) * 4.5 + 3
    for k in range(28):
        a = k / 28 * 2 * math.pi
        x, y = CX + math.cos(a) * r, cy + math.sin(a) * r * .8
        if inside(int(x), int(y)) and (k + f) % 2 == 0: cv.put(x, y, (255, 154, 138, max(40, 200 - int(r * 9))))
    for start in (1, 5, 9):
        t = (f - start) % 12
        if t < 8:
            y = cy - 8 - t * 3.2; x = CX - 3 + math.sin(t * .9 + start) * 2
            half_heart(cv, round(x), round(y), 255 if t < 5 else 255 - (t - 4) * 55)
    return cv

def capsule3_form():
    """Cápsula III se forma: el bloque base (6 cuadros) y luego la cáscara exterior sube por fuera (2 cuadros)."""
    fb, ff = form()
    back = fb + [core_glow(0), core_glow(1)]
    front = ff + [_merge(block_front(), outer_shell(level=.55)), _merge(block_front(glint=.2), outer_shell())]
    return back, front

def _merge(a, b):
    cv = Canvas(W, H); cv.px = dict(a.px); cv.px.update(b.px); return cv

# --- Gota III: cristales y fósil ------------------------------------------------------------------
def crystal(cv, x, base, h, w, lean, light=True):
    """Cristal de ámbar puntiagudo que sale del suelo: prisma con la cara de la luz clara y la otra oscura."""
    for j in range(h):
        fr = j / max(1, h)
        hw = w * (1 - fr) ** .8
        cxj = x + lean * j
        for dx in range(-int(math.ceil(hw)), int(math.ceil(hw)) + 1):
            if abs(dx) > hw: continue
            c = (AMB[3] if dx < 0 else AMB[1]) if light else (AMB[2] if dx < 0 else AMB[0])
            if dx == 0: c = AMB[4] if fr > .2 else AMB[3]
            cv.put(cxj + dx, base - j, c)

CRYS = [(8, 12, 3, -.2), (14, 18, 3.2, -.1), (21, 24, 3.6, 0), (28, 20, 3.4, .08), (35, 15, 3, .15), (40, 10, 2.4, .3),
        (11, 8, 2, -.3), (31, 9, 2.2, .1)]

def crystals_erupt(f):
    """Gota III: al dar, cristales de ámbar brotan del suelo alrededor del enemigo y se cierran en el bloque
    (6 cuadros, 48×56, delante). El cuadro 5 ya es el bloque cristalino."""
    if f == 5: return fossil_front(0)
    cv = Canvas(W, H); g = [.25, .55, .85, 1.0, 1.0][f]
    for k, (x, h, w, lean) in enumerate(CRYS):
        crystal(cv, x, FLOOR, max(2, int(h * g * (1.15 if f == 3 else 1))), w, lean * (1 - g * .5), light=k % 3 != 2)
    if f == 4:                                                              # se funden en el bloque: destello
        b = block_front(); cv2 = _merge(b, cv)
        star(cv2, CX - 6, CY - 10, 5, WHITE4[3], AMB[4]); cv = cv2
    cv.outline(); cv.clean()
    return cv

def fossil_front(f):
    """El bloque cristalino de la Gota III con el cadáver dentro (delante; 8 cuadros en bucle): el bloque base con
    puntas de cristal que sobresalen arriba y a los lados y destellos que saltan de arista en arista."""
    cv = block_front(glint=(f / 8 * 1.6) if f < 5 else -1)
    sp = Canvas(W, H)
    for (x, base, h, w, lean) in ((10, 28, 7, 2, -.35), (17, 18, 8, 2.2, -.15), (30, 17, 9, 2.4, .12), (38, 27, 7, 2, .35)):
        crystal(sp, x, base, h, w, lean)
    sp.outline(); sp.clean()
    for p_, c in sp.px.items():
        if p_ not in cv.px or cv.px[p_][3] < 200: cv.px[p_] = c
    tw = [(14, 20), (33, 19), (24, 30), (18, 44)][f % 4]                   # destello que salta
    if f % 2 == 0: star(cv, tw[0], tw[1], 2, WHITE4[3], AMB[4], diag=False)
    return cv

def fossil_tint(img, k):
    """Referencia del oscurecido del enemigo (lo hará un shader): mezcla hacia pardo ámbar muy oscuro, con un borde
    de luz ámbar; k 0..1."""
    out = img.copy(); px = out.load(); w, h = out.size
    dark = (58, 26, 8)
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0: continue
            px[x, y] = (int(r + (dark[0] - r) * k), int(g + (dark[1] - g) * k), int(b + (dark[2] - b) * k), a)
    if k > .6:                                                              # borde de luz ámbar
        src = img.load()
        for y in range(1, h - 1):
            for x in range(1, w - 1):
                if src[x, y][3] and (not src[x - 1, y][3] or not src[x, y - 1][3]): px[x, y] = (232, 154, 48, 255)
    return out

def stun_stars(f):
    """Gota I-II: estrellas de aturdido que giran sobre el bloque (6 cuadros, 32×16)."""
    cv = Canvas(32, 16)
    for k in range(3):
        a = f / 6 * 2 * math.pi + k * 2.09
        x, y = 16 + math.cos(a) * 11, 8 + math.sin(a) * 4
        front = math.sin(a) > 0
        star(cv, round(x), round(y), 2 if front else 1, AMB[4] if front else AMB[3], AMB[3] if front else AMB[2], diag=False)
    return cv

# --- maqueta ---------------------------------------------------------------------------------------
def creature(path, cw=None):
    im = Image.open(path).convert('RGBA'); h = im.height
    return im.crop((0, 0, cw or h, h))

def encased(back, front, body):
    out = Image.new('RGBA', (W, H))
    out.alpha_composite(back.img())
    out.alpha_composite(body, ((W - body.width) // 2, FLOOR - body.height + 2))
    out.alpha_composite(front.img())
    return out

if __name__ == '__main__':
    fb, ff = form(); hb, hf = hold(); cr = crack(); sh = shatter()
    anims = {'Fx_AmberShell_Back_Form': fb, 'Fx_AmberShell_Front_Form': ff, 'Fx_AmberShell_Back_Hold': hb,
             'Fx_AmberShell_Front_Hold': hf, 'Fx_AmberShell_Front_Crack': cr, 'Fx_AmberShell_Shatter': sh,
             'Proj_AmberDrop': [amber_drop(f) for f in range(4)], 'Fx_AmberDrop_Hit': [drop_hit(f) for f in range(5)]}
    os.makedirs(os.path.join(HERE, 'shell'), exist_ok=True)
    for name, frames in anims.items(): save_sheet(os.path.join(HERE, 'shell', name + '.png'), frames)
    frog = creature(os.path.join(TEMP, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png'))
    beetle = creature(os.path.join(TEMP, 'floor3-enemies', 'Beetle', 'Beetle_Idle.png'))
    seq = []                                                                # la secuencia completa con la rana
    for i in range(6): seq.append(encased(fb[i], ff[i], frog))
    for f in range(8): seq.append(encased(hb[0], hf[f], frog))
    for k in range(3): seq += [encased(hb[0], cr[k], frog)] * 2
    for f in range(8):
        im = Image.new('RGBA', (W, H))
        if f > 0: im.alpha_composite(frog, ((W - frog.width) // 2, FLOOR - frog.height + 2))
        im.alpha_composite(sh[f].img().crop((16, 10, 16 + W, 10 + H))); seq.append(im)
    bg = C('#1c2e20')
    os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True)
    big = [Image.new('RGBA', (W * 6, H * 6), bg) for _ in seq]
    for b, s in zip(big, seq): b.alpha_composite(s.resize((W * 6, H * 6), Image.NEAREST))
    big[0].save(os.path.join(HERE, 'gif', 'escena_bloque_nuevo.gif'), save_all=True, append_images=big[1:],
                duration=[90] * 6 + [120] * 8 + [260] * 6 + [70] * 8, loop=0, disposal=2)
    sheet = Image.new('RGBA', (W * 5 + 40, H + 8), bg)
    old = Image.open(os.path.join(TEMP, 'floor3-mutations', 'Effects', 'Fx_AmberBlock_Hold.png')).convert('RGBA').crop((0, 0, 48, 48))
    o2 = Image.new('RGBA', (W, H)); o2.alpha_composite(frog, ((W - frog.width) // 2, 40 - frog.height)); o2.alpha_composite(old, (0, 0))
    for i, im in enumerate((o2, encased(hb[0], hf[2], frog), encased(hb[0], hf[6], beetle), encased(hb[0], cr[2], frog), seq[-5])):
        sheet.alpha_composite(im, (4 + i * (W + 8), 4))
    sheet.resize((sheet.width * 4, sheet.height * 4), Image.NEAREST).save(os.path.join(HERE, 'vista_previa_bloque.png'))
    print('Listo en', HERE)

    # ---- subtanda 2 -------------------------------------------------------------------------------
    L3 = {'Fx_Cerci_Burst_III': [cerci_burst(f) for f in range(7)],
          'Fx_AmberShell3_Back_Hold': [core_glow(f) for f in range(8)],
          'Fx_AmberShell3_Front_Hold': [_merge(block_front(glint=(f / 8 * 1.6) if f < 5 else -1), outer_shell()) for f in range(8)],
          'Fx_AmberShell3_Front_Crack': [_merge(block_front(), outer_shell(cracks=k)) for k in (1, 2, 3)],
          'Fx_CapsuleHeal': [capsule_heal(f) for f in range(12)],
          'Fx_AmberCrystals_Erupt': [crystals_erupt(f) for f in range(6)],
          'Fx_AmberFossil_Front': [fossil_front(f) for f in range(8)],
          'Fx_AmberStun_Stars': [stun_stars(f) for f in range(6)]}
    c3b, c3f = capsule3_form(); L3['Fx_AmberShell3_Back_Form'] = c3b; L3['Fx_AmberShell3_Front_Form'] = c3f
    os.makedirs(os.path.join(HERE, 'level3'), exist_ok=True)
    for name, frames in L3.items(): save_sheet(os.path.join(HERE, 'level3', name + '.png'), frames)
    def scale6(seq, name, ms):
        big = []
        for s_ in seq:
            b = Image.new('RGBA', (s_.width * 6, s_.height * 6), bg); b.alpha_composite(s_.resize((s_.width * 6, s_.height * 6), Image.NEAREST)); big.append(b)
        big[0].save(os.path.join(HERE, 'gif', name), save_all=True, append_images=big[1:], duration=ms, loop=0, disposal=2)
    # Cercos III
    seq = []
    for f in range(9):
        im = Image.new('RGBA', (56, 56)); im.alpha_composite(frog, (28 - frog.width // 2, 28 - frog.height // 2))
        if f < 7: im.alpha_composite(L3['Fx_Cerci_Burst_III'][f].img())
        seq.append(im)
    scale6(seq, 'escena_cercos_III.gif', 70)
    # Cápsula III: se forma, cura (12), se agrieta la capa de fuera, revienta
    seq = []
    for k in range(8): seq.append(encased(c3b[k], c3f[k], frog))
    for f in range(12):
        im = encased(L3['Fx_AmberShell3_Back_Hold'][f % 8], L3['Fx_AmberShell3_Front_Hold'][f % 8], frog)
        im.alpha_composite(L3['Fx_CapsuleHeal'][f].img()); seq.append(im)
    for k in range(3): seq += [encased(L3['Fx_AmberShell3_Back_Hold'][0], L3['Fx_AmberShell3_Front_Crack'][k], frog)] * 2
    for f in range(8):
        im = Image.new('RGBA', (W, H))
        if f > 0: im.alpha_composite(frog, ((W - frog.width) // 2, FLOOR - frog.height + 2))
        im.alpha_composite(sh[f].img().crop((16, 10, 16 + W, 10 + H))); seq.append(im)
    scale6(seq, 'escena_capsula_III.gif', [90] * 8 + [110] * 12 + [240] * 6 + [70] * 8)
    # Gota III: gota, estalla, cristales, se fosiliza, queda de escudo; y Gota I-II con estrellas
    seq = []
    for f in range(4):
        im = Image.new('RGBA', (W, H)); im.alpha_composite(beetle, ((W - beetle.width) // 2, FLOOR - beetle.height + 2))
        d = anims['Proj_AmberDrop'][f].img(); im.alpha_composite(d, (2 + f * 4, 30)); seq.append(im)
    for f in range(5):
        im = Image.new('RGBA', (W, H)); im.alpha_composite(beetle, ((W - beetle.width) // 2, FLOOR - beetle.height + 2))
        im.alpha_composite(anims['Fx_AmberDrop_Hit'][f].img(), (12, 28)); seq.append(im)
    for f in range(6):
        im = Image.new('RGBA', (W, H)); im.alpha_composite(hb[0].img()) if f >= 4 else None
        im.alpha_composite(beetle, ((W - beetle.width) // 2, FLOOR - beetle.height + 2))
        im.alpha_composite(L3['Fx_AmberCrystals_Erupt'][f].img()); seq.append(im)
    for k in range(6):
        b2 = fossil_tint(beetle, (k + 1) / 6)
        seq.append(encased(hb[0], L3['Fx_AmberFossil_Front'][k % 8], b2))
    for f in range(8): seq.append(encased(hb[0], L3['Fx_AmberFossil_Front'][f], fossil_tint(beetle, 1)))
    scale6(seq, 'escena_gota_III.gif', [60] * 4 + [60] * 5 + [80] * 6 + [140] * 6 + [130] * 8)
    seq = []
    for f in range(12):
        im = Image.new('RGBA', (W, H + 10)); e = encased(hb[0], hf[f % 8], beetle); im.alpha_composite(e, (0, 10))
        im.alpha_composite(L3['Fx_AmberStun_Stars'][f % 6].img(), (8, 2 + 10)); seq.append(im)
    scale6(seq, 'escena_gota_I_II.gif', 100)
    sheet = Image.new('RGBA', (W * 4 + 40, H + 8), bg)
    caps = encased(L3['Fx_AmberShell3_Back_Hold'][2], L3['Fx_AmberShell3_Front_Hold'][2], frog); caps.alpha_composite(L3['Fx_CapsuleHeal'][3].img())
    burst = Image.new('RGBA', (W, H)); burst.alpha_composite(frog, ((W - frog.width) // 2, 28)); burst.alpha_composite(L3['Fx_Cerci_Burst_III'][2].img().crop((8, 8, 48, 48)), (4, 18))
    for i_, im in enumerate((burst, caps, encased(hb[0], L3['Fx_AmberFossil_Front'][0], fossil_tint(beetle, 1)), encased(hb[0], hf[2], beetle))):
        sheet.alpha_composite(im, (4 + i_ * (W + 8), 4))
    st = L3['Fx_AmberStun_Stars'][1].img(); sheet.alpha_composite(st, (4 + 3 * (W + 8) + 8, 4))
    sheet.resize((sheet.width * 4, sheet.height * 4), Image.NEAREST).save(os.path.join(HERE, 'vista_previa_nivel_III.png'))
    print('Nivel III listo')
