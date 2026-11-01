"""Efectos y escenario del combate contra el Arthropleura (tanda 6c-1). Skill primordia-sprites (pixel.py).

Bosque humeante (Smoke). Reutiliza las frondas, ramitas y la Sigillaria del escenario (tanda 1b) y las nubes de la
tanda 5 (generar_reacciones_piso3.cloud).

- Ola de tierra (F2): montículo de tierra y hojarasca que corre por el suelo en línea recta (el código lo mueve), su
  final, y el pisotón de las patas que la avisa.
- Presentación: temblor (piedrecitas que saltan) y la espesura que suelta hojas al paso del cuerpo (F1).
- Embestida: tramo de aviso que se repite a lo largo de la trayectoria (como Fx_DragonflyAim) y polvo de arranque.
- 70 %: la pared de vegetación revienta (efecto grande) y deja restos en el suelo.
- F2: la Sigillaria derribada cae (se parte por la base) y queda como tronco tumbado.

Uso: python generar_efectos_arthropleura.py
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
MT = os.path.normpath(os.path.join(HERE, '..', '..'))
for d in ('floor3-environment', 'floor3-reactions'):
    sys.path.insert(0, os.path.join(MT, d))
_argv = sys.argv; sys.argv = sys.argv[:1]
import generar_escenario_piso3 as S                    # sigillaria(), ground_shadow(), plant_ramp()
import generar_reacciones_piso3 as RX                  # cloud(), SMK
sys.argv = _argv
from base_piso3 import *                               # noqa: F401,F403
from PIL import Image

OUT = HERE
T = tram('Smoke')
BGC = C('#30332a')
DIRT = ['#2a1e14', '#4a3420', '#6e5030', '#967040', '#c09a5c']        # tierra removida (más cálida y clara que el suelo)
LEAF = ['#433d1e', '#635726', '#8a6a34', '#c09a4a']                   # hojarasca reseca
DUST = ['#5a5040', '#7e725a', '#a69878', '#cdc09c']                   # polvo (claro: se lee sobre el suelo oscuro)
PEB = ['#3a3630', '#6e665a', '#a49a88']                               # piedrecitas
WARN = ['#5a1a14', '#a8341c', '#e8682a', '#ffb24a']                   # aviso de embestida (naranja de ascua)

def Ca(c, a): return C(c)[:3] + (max(0, min(255, int(a))),)

def leaf_bit(cv, x, y, ang, k=0):
    """Hoja seca de 2-3 px que gira."""
    c0, c1 = C(LEAF[(k + 2) % 4]), C(LEAF[k % 4])
    cv.put(x, y, c0); cv.put(x + math.cos(ang), y + math.sin(ang), c1)
    if k % 2: cv.put(x - math.cos(ang), y - math.sin(ang), C(LEAF[1]))

def clod(cv, x, y, r=1.0):
    if r < 1.2:
        cv.put(x, y, C(DIRT[3])); cv.put(x + 1, y, C(DIRT[2])); cv.put(x, y + 1, C(DIRT[1]))
    else:
        ellipse(cv, x, y, r, r * .85, DIRT[1:])

# =================================================================================================
# Ola de tierra
# =================================================================================================
def earth_wave(i, n=6, end=0.0):
    """Montículo que avanza (64×48, pie en y = 34 desde arriba; bucle de 6). end 0..1: se deshace en polvo."""
    cv = Canvas(64, 48)
    ph = i / n
    ry = 13 * (1 - end) + 1
    rx = 23 + 2 * math.sin(ph * 2 * math.pi) + 5 * end
    body = Canvas(64, 48)
    if ry > 1.5:
        def inside(x, y):
            dx = (x - 32) / rx; dy = (y - 40) / ry
            bump = .12 * math.sin(x * .7 + ph * 2 * math.pi * 2)              # cresta que se revuelve
            return dy <= 0 and dx * dx + dy * dy <= 1 + bump
        fill(body, inside, 32, 35, rx, ry * 1.2, DIRT[1:])
        for k in range(7):                                                 # terrones de la cresta
            a = math.pi + (k + .5) / 7 * math.pi
            x = 32 + math.cos(a) * rx * .8; y = 40 + math.sin(a) * ry * 1.05 + math.sin(ph * 6.28 + k) * 1.2
            clod(body, x, y, 2.2 if k % 2 else 1.4)
        for k in range(6):                                                 # hojarasca encima
            x = 32 + (k - 2.5) * rx * .3; y = 40 - ry * (.75 - .1 * (k % 2)) + math.sin(ph * 6.28 + k * 1.3)
            body.put_in(x, y, C(LEAF[2 + k % 2]))
        body.outline(); body.clean()
    cv.px.update(body.px)
    # polvo a los lados y lo que vuela
    dx_ = min(rx + 1, 22)
    dust = [(32 - dx_, 39, 6 + 3 * end, .85 * (1 - end * .6)), (32 + dx_, 39, 6 + 3 * end, .85 * (1 - end * .6))]
    if end > 0: dust += [(32, 34 - 5 * end, 11 * end + 4, .9 - .5 * end)]
    RX.cloud(cv, dust, DUST)
    for k in range(8):                                                     # terrones y hojas que saltan
        p = (ph + k / 8) % 1
        a = math.pi * (1.15 + .7 * (k / 7))
        x = 32 + math.cos(a) * (8 + 22 * p); y = 38 - ry - 18 * p + 24 * p * p
        if end and p > 1 - end: continue
        if k % 3 == 0: clod(cv, x, y)
        else: leaf_bit(cv, x, y, p * 9 + k, k)
    return cv

def stomp(i, n=5):
    """Pisotón de las patas que avisa de la ola (32×24): polvo, grieta y piedrecitas."""
    cv = Canvas(32, 24)
    t = i / (n - 1)
    if t > .1:                                                             # grieta en estrella
        for a in (0.2, 1.4, 2.6, 3.6, 4.9):
            for st in range(int(3 + 6 * min(1, t * 1.5))):
                x = 16 + math.cos(a) * st + (st % 2) * .5; y = 18 + math.sin(a) * st * .5
                cv.put(x, y, OUTL); cv.put(x, y - 1, C(DIRT[3]))
    RX.cloud(cv, [(9, 16 - 4 * t, 4 + 3 * t, 1 - t * .7), (23, 16 - 4 * t, 4 + 3 * t, 1 - t * .7), (16, 13 - 5 * t, 3 + 3 * t, .9 - t * .7)], DUST)
    for k in range(4):
        x = 16 + (k - 1.5) * 4.5 * (1 + .6 * t); y = 17 - 9 * math.sin(min(1, t * 1.2) * math.pi) * (.6 + .2 * k % 2)
        cv.put(x, y, C(PEB[1 + k % 2])); cv.put(x + 1, y, C(PEB[0]))
    return cv

# =================================================================================================
# Presentación
# =================================================================================================
def tremor(i, n=4):
    """Temblor (48×24, bucle): piedrecitas que saltan y polvillo; se reparte por la sala mientras se acerca."""
    cv = Canvas(48, 24)
    rnd = random.Random(3)
    for k in range(9):
        x = rnd.randint(4, 43); base = rnd.randint(16, 21); ph = (i / n + k * .37) % 1
        y = base - 4 * math.sin(ph * math.pi) * (1 if k % 3 else 1.6)
        for (dx, dy, c) in ((0, 0, PEB[2]), (1, 0, PEB[1]), (0, 1, PEB[1]), (1, 1, PEB[0])): cv.put(x + dx, y + dy, C(c))
        if ph < .2: RX.cloud(cv, [(x + .5, base + 1, 2.5, .8)], DUST)
    return cv

def canopy_shake(i, n=6):
    """La espesura suelta hojas y ramitas cuando el cuerpo pasa por debajo (32×40, bucle)."""
    cv = Canvas(32, 40)
    rnd = random.Random(8)
    for k in range(16):
        x0 = rnd.randint(5, 26); p = (i / n + k / 16) % 1
        x = x0 + math.sin(p * 9 + k) * 2.5; y = 4 + p * 30
        if k % 4 == 0: cv.put(x, y, C(T['root'][2])); cv.put(x + 1, y + 1, C(T['root'][1]))   # ramita
        else: leaf_bit(cv, x, y, p * 12 + k, k)
    RX.cloud(cv, [(16, 4 + (i % 2), 6, .45)], DUST)
    return cv

# =================================================================================================
# Embestida
# =================================================================================================
def charge_aim(i, n=4):
    """Tramo de aviso que se repite a lo largo de la embestida (16×8; el código lo estira como una línea y lo
    desplaza): marcas de garra en el suelo en naranja de ascua que laten hacia delante."""
    cv = Canvas(16, 8)
    for k in range(2):
        x0 = (k * 8 + i * 2) % 16
        b = 1 if (i + k) % 2 else 0
        for d in range(4):                                                # chevrón ">"
            cv.put((x0 + d) % 16, 1 + d, C(WARN[1 + b + (d > 1)])); cv.put((x0 + d) % 16, 6 - d, C(WARN[1 + b + (d > 1)]))
            cv.put((x0 + d - 1) % 16, 1 + d, C(WARN[0])); cv.put((x0 + d - 1) % 16, 6 - d, C(WARN[0]))
    return cv

def charge_dust(i, n=6):
    """Polvo y hojarasca al arrancar la embestida (48×32, detrás de la cabeza; mira a la derecha: sale hacia atrás)."""
    cv = Canvas(48, 32)
    t = i / (n - 1)
    RX.cloud(cv, [(30 - 14 * t, 24 - 4 * t, 5 + 5 * t, 1 - .7 * t), (36 - 20 * t, 26 - 2 * t, 4 + 4 * t, .9 - .7 * t),
                  (24 - 10 * t, 22 - 7 * t, 3 + 5 * t, .8 - .6 * t)], DUST)
    for k in range(6):
        x = 34 - (6 + 22 * t) * (.55 + .08 * k); y = 24 - 8 * math.sin(t * math.pi) * (.5 + .1 * k)
        if k % 2: leaf_bit(cv, x, y, t * 8 + k, k)
        else: clod(cv, x, y)
    return cv

# =================================================================================================
# Pared de vegetación que revienta (70 %) y restos
# =================================================================================================
def wall_break(i, n=9):
    """96×96, centro = el tramo de pared derribado. 0 se abomba; 1-2 revienta (frondas, ramas, terrones, polvo);
    3-8 cae lo que voló y el polvo se va. Cuadro que cuenta: 1 (la pared ya no está)."""
    cv = Canvas(96, 96)
    rnd = random.Random(21)
    lp = [C(c) for c in S.plant_ramp(T)]
    if i == 0:                                                            # revienta: chispazo de polvo y frondas
        RX.cloud(cv, [(48, 50, 10, 1.0), (38, 54, 6, .9), (58, 54, 6, .9)], DUST)
        pieces = Canvas(96, 96)
        for k in range(10):
            a = rnd.uniform(0, 2 * math.pi)
            frondlet(pieces, 48 + math.cos(a) * 8, 50 + math.sin(a) * 6, a, rnd.uniform(6, 10), lp, .3, 2.0)
        pieces.outline(); pieces.clean(); cv.px.update(pieces.px)
        return cv
    t = (i - 1) / (n - 2)
    RX.cloud(cv, [(48 + dx * (1 + t), 52 + dy * (1 + t * .5) - 6 * t, r * (1 + 1.2 * t), max(0, 1 - t * 1.1))
                  for dx, dy, r in ((-14, 2, 12), (14, 2, 12), (0, -8, 14), (0, 10, 11), (-24, 10, 8), (24, 10, 8))], DUST)
    pieces = Canvas(96, 96)
    for k in range(18):                                                    # lo que sale volando
        a = rnd.uniform(-math.pi, 0) if k % 3 else rnd.uniform(0, math.pi)
        sp = rnd.uniform(20, 42)
        d = sp * min(1, t * 1.6)
        x = 48 + math.cos(a) * d; y = 50 + math.sin(a) * d * .7 + 60 * max(0, t - .35) ** 2
        if y > 92: continue
        if k % 3 == 0: twig(pieces, x, y, a + t * 4, rnd.uniform(5, 9), [C(c) for c in T['root']], None, False, rnd)
        elif k % 3 == 1: frondlet(pieces, x, y, a + t * 5, rnd.uniform(5, 8), lp, .4, 1.8)
        else: clod(pieces, x, y, 1.8)
    pieces.outline(); pieces.clean()
    cv.px.update(pieces.px)
    return cv

def rubble(k):
    """Restos de la pared en el suelo (32×24, 3 variantes): montón de frondas rotas, ramas y tierra."""
    cv = Canvas(32, 24); rnd = random.Random(40 + k)
    lp = [C(c) for c in S.plant_ramp(T)]
    ellipse(cv, 16, 17, 11, 4.5, DIRT[1:])
    for j in range(5): twig(cv, rnd.uniform(7, 25), rnd.uniform(13, 18), rnd.uniform(-math.pi, 0), rnd.uniform(5, 9),
                            [C(c) for c in T['root']], None, False, rnd)
    for j in range(7): frondlet(cv, rnd.uniform(6, 26), rnd.uniform(12, 18), rnd.uniform(-math.pi, 0), rnd.uniform(4, 8), lp, .5, 1.8)
    cv.outline(); cv.clean(); S.ground_shadow(cv, 16, 20.5, 12, 2)
    return cv

# =================================================================================================
# Sigillaria derribada
# =================================================================================================
FALL_W, FALL_H, PIV = 112, 80, (28, 70)                                     # pivote = pie del tronco

def sigillaria_fall(n=9, dust=True):
    """Cae hacia la derecha (Flip X para la izquierda): se parte por la base, gira, golpea el suelo con polvo y
    rebota un poco. Cuadro que cuenta: 6 (golpe: daña y deja el tronco). El último = Sigillaria_Fallen."""
    tree = S.sigillaria(T, 7).img()                                       # 40×64, pie en (20, 61)
    split = 55
    top = tree.crop((0, 0, 40, split)); bottom = tree.crop((0, split, 40, 64))
    angles = (0, 6, 16, 32, 52, 74, 92, 86, 90)
    frames = []
    for k, ang in enumerate(angles[:n]):
        im = Image.new('RGBA', (FALL_W, FALL_H), (0, 0, 0, 0))
        im.alpha_composite(bottom, (PIV[0] - 20, PIV[1] - (61 - split)))
        big = Image.new('RGBA', (160, 160), (0, 0, 0, 0)); big.alpha_composite(top, (80 - 20, 80 - split))
        rot = big.rotate(-ang, resample=Image.NEAREST, center=(80, 80))
        if ang >= 74: rot = rot.resize((160, 160), Image.NEAREST)
        im.alpha_composite(rot, (PIV[0] - 80, PIV[1] - (61 - split) - 80 + 0))
        cv = Canvas(FALL_W, FALL_H)
        for y in range(FALL_H):
            for x in range(FALL_W):
                p = im.getpixel((x, y))
                if p[3]: cv.px[(x, y)] = p
        if k >= 6 and dust:                                                 # polvo del golpe a lo largo del tronco
            tt = (k - 6) / 2
            RX.cloud(cv, [(PIV[0] + 16 + 14 * j, PIV[1] - 3 - 3 * tt, 5 + 3 * tt, .9 - .35 * tt) for j in range(4)], DUST)
        if ang >= 86:                                                       # sombra del tronco tumbado
            for x in range(PIV[0], PIV[0] + 60):
                for y in (PIV[1] + 1, PIV[1] + 2):
                    if (x, y) not in cv.px: cv.px[(x, y)] = (24, 20, 37, 80)
        frames.append(cv)
    return frames

def fallen():
    return sigillaria_fall(dust=False)[-1]                                 # el último cuadro sin polvo

# =================================================================================================
# Revisión del usuario (2026-11-01): bulto bajo la maleza, borde que revienta, brecha y ondas expansivas
# =================================================================================================
CAP = [C(c) for c in T['cap']] + [C(T['leaf'][3])]                         # copa de la espesura (5 tonos)
BULGE = [C(c) for c in ('#2c3127', '#433d1e', '#635726', '#857232', '#a0946a')]   # frondas levantadas: cogen luz
LEAFC = [C(c) for c in T['leaf']]

def canopy_bulge(i, n=6):
    """Bulto de frondas que se levanta donde pasa una pieza escondida (64×48, encima de la copa; bucle). El código
    pone uno por pieza que está bajo la espesura, en su posición."""
    cv = Canvas(64, 48); rnd = random.Random(12)
    ph = i / n
    rise = 1 + .08 * math.sin(ph * 2 * math.pi)
    for y in range(48):                                                  # sombra suave debajo (sin contorno)
        for x in range(64):
            q = ((x + .5 - 33) / 25) ** 2 + ((y + .5 - 31) / (11 * rise)) ** 2
            if q <= 1 and y > 27 and (x + y) % 2 == 0: cv.px[(x, y)] = (24, 20, 37, 110)
    for y in range(48):                                                  # joroba con la trama de la copa
        for x in range(64):
            q = ((x + .5 - 32) / 23) ** 2 + ((y + .5 - 27) / (12 * rise)) ** 2
            if q > 1: continue
            lit = -(x - 32) * .03 - (y - 27) * .09 + (1 - q) * .8
            k_ = 3 if lit > .9 else 2 if lit > .45 else 1
            if q > .75 and (x * 3 + y) % 4 == 0: continue                  # borde deshilachado
            cv.px[(x, y)] = BULGE[k_ + 1] if (x + y * 2) % 5 else BULGE[k_]
    fr = Canvas(64, 48)
    for k in range(26):                                                  # frondas que se levantan y se agitan
        a = rnd.uniform(-math.pi, 0) if k % 3 else rnd.uniform(0, math.pi)
        r = rnd.uniform(.2, .95)
        x = 32 + math.cos(a) * 22 * r; y = 27 + math.sin(a) * 11 * r * rise
        sway = math.sin(ph * 2 * math.pi + k) * .5
        frondlet(fr, x, y, -math.pi / 2 + rnd.uniform(-1.3, 1.3) + sway, rnd.uniform(5, 9) * (1.2 - r * .4),
                 BULGE[1:] if y < 26 else BULGE[:4], .35, 2.2)
    fr.outline(); fr.clean()
    cv.px.update(fr.px)
    for k in range(5):                                                   # hojas sueltas que saltan
        p = (ph + k / 5) % 1
        leaf_bit(cv, 32 + (k - 2) * 9 + math.sin(p * 7) * 2, 14 - 12 * p + 10 * p * p, p * 9 + k, k)
    return cv

def edge_break(i, n=8):
    """El borde de la espesura revienta hacia dentro del claro (96×80; dibujado para el borde de ARRIBA: estalla
    hacia abajo; el código lo gira 90/180/270° exactos para los otros bordes). Cuadro que cuenta: 1 (el borde ya no
    está: se cambian los tiles por la brecha)."""
    cv = Canvas(96, 80); rnd = random.Random(33)
    lp = [C(c) for c in S.plant_ramp(T)]
    if i == 0:                                                           # el borde se abomba
        for k in range(14):
            frondlet(cv, rnd.uniform(28, 68), rnd.uniform(14, 26), math.pi / 2 + rnd.uniform(-.8, .8), rnd.uniform(6, 10), LEAFC, .3, 2.2)
        cv.outline(); cv.clean()
        RX.cloud(cv, [(48, 26, 8, .6)], DUST)
        return cv
    t = (i - 1) / (n - 2)
    RX.cloud(cv, [(48 + dx * (1 + t), 26 + 30 * t + dy, r * (1 + t), max(0, 1 - t * 1.05))
                  for dx, dy, r in ((-14, 0, 9), (14, 0, 9), (0, 6, 11), (-6, 14, 7), (8, 14, 7))], DUST)
    pieces = Canvas(96, 80)
    for k in range(16):
        a = math.pi / 2 + rnd.uniform(-1.2, 1.2)                         # hacia abajo, en abanico
        d = rnd.uniform(14, 40) * min(1, t * 1.7)
        x = 48 + rnd.uniform(-14, 14) + math.cos(a) * d; y = 20 + math.sin(a) * d + 18 * max(0, t - .4)
        if y > 78: continue
        if k % 3 == 0: twig(pieces, x, y, a + t * 3, rnd.uniform(5, 9), [C(c) for c in T['root']], None, False, rnd)
        elif k % 3 == 1: frondlet(pieces, x, y, a + t * 4, rnd.uniform(5, 9), lp, .4, 1.9)
        else: clod(pieces, x, y, 1.6)
    pieces.outline(); pieces.clean(); cv.px.update(pieces.px)
    return cv

def breach(side):
    """Restos de la brecha (encima de los tiles ya abiertos): flecos de frondas rotas a los dos lados de la abertura,
    tocones partidos y frondas aplastadas en el suelo. Top 96×64 (abertura de 5 u en el centro, x 8..88),
    Side 64×96 (borde izquierdo; Flip X para el derecho), Bottom 96×48."""
    rnd = random.Random({'Top': 51, 'Side': 52, 'Bottom': 53}[side])
    w, h = {'Top': (96, 64), 'Side': (64, 96), 'Bottom': (96, 48)}[side]
    cv = Canvas(w, h)
    lp = [C(c) for c in S.plant_ramp(T)]
    rp = [C(c) for c in T['root']]
    along = (lambda u, v: (u, v)) if side != 'Side' else (lambda u, v: (v, u))   # u = a lo largo de la abertura
    L = w if side != 'Side' else h
    D = h if side != 'Side' else w
    for k in range(8):                                                   # frondas aplastadas en el suelo de la abertura
        u = rnd.choice((rnd.uniform(12, 30), rnd.uniform(L - 30, L - 12))); v = rnd.uniform(D * .3, D * .9)
        x, y = along(u, v)
        frondlet(cv, x, y, rnd.uniform(0, 2 * math.pi), rnd.uniform(5, 8), CAP[:4], .5, 1.8)
    for s_ in (0, 1):                                                    # flecos rotos y tocones a los lados (apiñados)
        for k in range(12):
            u = (7 if s_ == 0 else L - 7) + rnd.uniform(-5, 5); v = rnd.uniform(2, D - 4)
            x, y = along(u, v)
            frondlet(cv, x, y, rnd.uniform(0, 2 * math.pi), rnd.uniform(6, 10), CAP[1:] if k % 3 else LEAFC, .4, 2.2)
        u = 10 if s_ == 0 else L - 10; v = D * .35
        x, y = along(u, v)
        ellipse(cv, x, y, 3.2, 2.6, [C(c) for c in T['bark'][1:]])           # tocón partido
        cv.put(x - 1, y - 2, C(T['bark'][4])); cv.put(x, y - 2, C(LEAF[3]))
    for k in range(5): twig(cv, rnd.uniform(8, w - 8), rnd.uniform(8, h - 8), rnd.uniform(0, 6.28), rnd.uniform(5, 9), rp, None, False, rnd)
    cv.outline(); cv.clean()
    return cv

SHOCK = [C('#2a1e14'), C('#5a4a36'), C('#a69878'), C('#e0d4b4')]          # grieta, sombra, cresta, brillo

def shock_tile(i, n=3):
    """Tramo de la onda expansiva (16×8) que el código repite a lo largo del arco (LineRenderer, textura en
    mosaico): cresta de polvo claro arriba (hacia fuera del arco) y grieta oscura debajo; parpadea en 3 cuadros."""
    cv = Canvas(16, 8)
    for x in range(16):
        bump = 1 if (x + i * 2) % 5 < 2 else 0
        cv.put(x, 2 - bump, SHOCK[2]); cv.put(x, 3, SHOCK[1]); cv.put(x, 4, SHOCK[0] if x % 3 else SHOCK[1])
        if (x + i) % 6 == 0: cv.put(x, 1 - bump, SHOCK[3])
    for k in range(2):                                                   # piedrecitas que saltan con la cresta
        x = (k * 8 + i * 3) % 16; cv.put(x, 0 if i % 2 else 1, C(PEB[2]))
    return cv

def shock_arc(cv, cx, cy, r, aim, spread=math.radians(70), life=1.0, k=0):
    """Dibuja un arco de la onda (para las escenas): elipse achatada (picada) de radio r abierta hacia aim."""
    steps = int(r * 2 * spread / 1.2) + 2
    for j in range(steps):
        a = aim - spread + 2 * spread * j / max(1, steps - 1)
        fade = 1 - abs(a - aim) / spread                                 # se apaga hacia las puntas
        if (j * 7 + k) % 13 > 11 * fade + 3: continue
        x = cx + math.cos(a) * r; y = cy + math.sin(a) * r * SE_
        ox, oy = math.cos(a), math.sin(a) * SE_
        al = 255 * min(1, life * 1.4)
        cv.put(x + ox, y + oy - 2, Ca('#e0d4b4', al)) if (j + k) % 4 == 0 else None
        cv.put(x + ox * .5, y + oy * .5 - 1, Ca('#cdc09c', al)); cv.put(x, y - 1, Ca('#a69878', al))
        cv.put(x, y, Ca('#5a4a36', al)); cv.put(x - ox, y - oy + 1, Ca('#2a1e14', al * .85))
        if (j + k * 3) % 13 == 0: cv.put(x + ox * 2, y - 3 - (k % 2), C(PEB[2]))
SE_ = math.sin(math.radians(55))

# =================================================================================================
def build():
    fx = {
        'Fx_Arthropleura_Stomp': [stomp(i) for i in range(5)],
        'Fx_Arthropleura_Shockwave': [shock_tile(i) for i in range(3)],
        'Fx_Arthropleura_EdgeBreak': [edge_break(i) for i in range(8)],
        'Fx_Arthropleura_Tremor': [tremor(i) for i in range(4)],
        'Fx_Arthropleura_CanopyShake': [canopy_shake(i) for i in range(6)],
        'Fx_Arthropleura_ChargeAim': [charge_aim(i) for i in range(4)],
        'Fx_Arthropleura_ChargeDust': [charge_dust(i) for i in range(6)],
        'Fx_Arthropleura_WallBreak': [wall_break(i) for i in range(9)],
    }
    scene = {'Rubble_Brush': [rubble(k) for k in range(3)], 'Sigillaria_Fall': sigillaria_fall(), 'Sigillaria_Fallen': [fallen()],
             'Breach_Top': [breach('Top')], 'Breach_Side': [breach('Side')], 'Breach_Bottom': [breach('Bottom')]}
    for name, fr in fx.items(): save_sheet(os.path.join(OUT, 'Effects', name + '.png'), fr)
    for name, fr in scene.items():
        if name == 'Rubble_Brush':
            for k, f in enumerate(fr): save_sheet(os.path.join(OUT, 'Smoke', f'{name}_{k}.png'), [f])
        else: save_sheet(os.path.join(OUT, 'Smoke', name + '.png'), fr)
    rows = dict(fx); rows.update(scene)
    preview(os.path.join(OUT, 'vista_previa_6c1.png'), rows, BGC, scale=3)
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    ms = {'Shockwave': 80, 'CanopyBulge': 110, 'EdgeBreak': 80, 'Stomp': 70, 'Tremor': 90, 'CanopyShake': 110, 'ChargeAim': 80, 'ChargeDust': 70,
          'WallBreak': 80, 'Sigillaria_Fall': 80}
    for name, fr in rows.items():
        if len(fr) > 1:
            m = next((v for k, v in ms.items() if k in name), 90)
            gif(os.path.join(OUT, 'gif', name + '.gif'), fr, m, BGC, scale=4)
    print('Listo:', OUT)

if __name__ == '__main__':
    build()
