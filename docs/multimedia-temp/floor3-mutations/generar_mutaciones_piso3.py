"""Mutaciones del piso 3 de Primordia (pantano carbonífero), por subtandas.

Regla del piso 3 (mutaciones por niveles): las normales NO cambian la silueta del jugador; las Extremidades y los
Órganos se ven con efectos al usarlas, las Pieles retocan el cuerpo solo dentro de su silueta, las especiales del ámbar
sí cambian el modelo y los linajes lo transforman.

4a, Extremidades (solo efectos, en la esquiva):
- Patas de salto (JumpingLegs): despegue con polvo y muelle verde, aterrizaje que salpica, onda de daño (II) y
  estrellitas de aturdido (III, Rana toro).
- Patas hilanderas (SpinnerLegs): destello de seda al salir, tramo de hilo que se repite, nudo de los extremos, chispa
  al dañar y la telaraña del cruce (III).

Las capas de las Pieles (4b) se calculan desde los sprites reales de ../floor3-bodies cuadro a cuadro: lienzo de 56×56
con el cuerpo centrado, en Idle, Move y los ataques con dirección, en side/down/up.
Hecho con la skill primordia-sprites (pixel.py): 16 px por unidad, luz arriba a la izquierda, contorno OUTL.

4b, Pieles (capa ENCIMA del cuerpo, solo los píxeles del cuerpo que cambian; ni contorno ni ojos):
- Piel anfibia (AmphibianSkin): película húmeda verde agua, brillos que se mueven y gotitas abajo. Efectos: carga en
  el agua, moco cargado que absorbe un golpe, reventón y reflejo (III).
- Escamas carbonizadas (CharredScales): carbón con escamas al tresbolillo y grietas de ascuas que laten. Efectos:
  ascuas tras pisar fuego (tus ataques queman), enemigo ardiendo (genérico) y Brasa (III).

4c, Órganos (sin capa en el cuerpo; efectos detallados): Saco de metano (vejiga que revienta, nube a la deriva,
explosión, Incendio III), Feromonas en rosa coral (onda de 2 u, aura, marca y Frenesí III) y el parásito de la clase
Parásito (pegado, salto, gota de vida y curación).

4d, Especiales del ámbar (sí cambian el modelo; cada una en su zona): Cercos de cucaracha detrás (punta de ámbar
encendida = quiebro listo), Cápsula de ámbar = costra de resina en el lomo con un mosquito atrapado, Gota de ámbar =
glándula delante que se llena al cargar (Rest y Full). Efectos: quiebro, listo, bloque de ámbar (para la Cápsula y para
el enemigo encerrado por la Gota), estallido en esquirlas y la gota lanzada.

Uso: python generar_mutaciones_piso3.py [limbs] [skins] [jumpskins] [organs] [specials]
"""
import math, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', 'floor3-enemies'))
_argv = sys.argv; sys.argv = [sys.argv[0], 'none']
import generar_enemigos_piso3 as E                      # ell5, rell5, mix_ramp, blend, web... y la skill (pixel.py)
sys.argv = _argv
from generar_enemigos_piso3 import *                    # noqa: F401,F403  (C, Canvas, OUTL, save_sheet, preview, gif...)
from PIL import Image

OUT = HERE
BODIES = os.path.normpath(os.path.join(HERE, '..', 'floor3-bodies'))
BG = C('#1c2e20')
NL = 56
BODY_ANIMS = {'Frog': ('Idle', 'Move', 'Attack'),
              'Diplocaulus': ('Idle', 'Move', 'AttackStart'),
              'Whipscorpion': ('Idle', 'Move', 'AttackStart', 'AttackLoop', 'AttackEnd')}
FACINGS = ('side', 'down', 'up')

LEG = ramp('#5a9a3a', 5)                                 # patas de salto: verde rana
LEG_B = ['#2a4a1c', '#3e6a28']                           # bandas oscuras
CHITIN = ['#1e1614', '#3a2a22', '#5e4434', '#8a6a4e', '#c0a07a']   # patas hilanderas: quitina parda
SILKR = ['#7a8a84', '#aebcb4', '#d8e2dc', '#f2f8f4', '#ffffff']    # seda
DUST = ['#3a2e22', '#5a4a36', '#86735a', '#b4a080']      # polvo y barro del aterrizaje
WATER = ['#2a5e54', '#4e8a72', '#9cc8a0', '#e0f4e4']     # gotas de agua turbia

def frames_of(path):
    sheet = Image.open(path).convert('RGBA'); h = sheet.height
    return [sheet.crop((i * h, 0, (i + 1) * h, h)) for i in range(sheet.width // h)]

def cells_of(im, off):
    px = im.load()
    return {(x + off, y + off) for y in range(im.height) for x in range(im.width) if px[(x, y)][3] > 0}

def bbox(cells):
    xs = [c[0] for c in cells]; ys = [c[1] for c in cells]
    return min(xs), min(ys), max(xs), max(ys)

def core_box(cells, frac=0.55):
    """Caja del tronco: columnas y filas con al menos frac del grosor máximo (sin colas finas, antenas ni patas)."""
    cols, rows = {}, {}
    for (x, y) in cells: cols[x] = cols.get(x, 0) + 1; rows[y] = rows.get(y, 0) + 1
    mh, mw = max(cols.values()), max(rows.values())
    xs = [x for x, v in cols.items() if v >= mh * frac]; ys = [y for y, v in rows.items() if v >= mw * frac]
    return min(xs), min(ys), max(xs), max(ys)

def behind(cv, cells):
    for p in cells: cv.px.pop(p, None)

def thick(cv, a, b, cols, w=2):
    """Tramo de pata: w = 2 px de grosor (el de abajo, en sombra)."""
    for (x, y) in E.line_pts(*a, *b):
        cv.put(x, y, C(cols[0]))
        if w > 1: cv.put(x, y + 1, C(cols[1]))

def amp_of(anim):
    return {'Idle': 0.4, 'Move': 1.0}.get(anim, 0.25)

# =================================================================================================
# Patas de salto
# =================================================================================================
def toes(cv, x, y, s):
    """Tres dedos separados en la punta del pie (s = hacia dónde apunta: -1 izquierda, 1 derecha)."""
    for dy, dx in ((-1, 1), (0, 2), (1, 1)):
        cv.put(x + s * dx, y + dy, C(LEG[1])); cv.put(x + s * (dx + 1), y + dy, C(LEG[3]))

def jump_takeoff(i, n=5):
    """Despegue del salto (32×24): polvo que se abre a los lados y un muelle verde que se estira hacia arriba y se
    desvanece (el cuerpo ya va en el aire; el código lo pone en el punto de salida)."""
    cv = Canvas(32, 24)
    t = (i + 1) / n
    for s in (-1, 1):                                          # polvo a ras de suelo
        for k in range(4):
            x = 16 + s * (3 + t * 9 + k * 1.6); y = 20 - k * 0.6 - math.sin(t * math.pi) * 2
            if i < n - 1 or k < 2: cv.put(x, y, C(DUST[3 - min(3, i)], 230 - i * 35))
    if i < 4:                                                  # muelle: zigzag que sube y se estira
        top = 20 - (4 + t * 12)
        steps = 6
        for k in range(steps * 2 + 1):
            u = k / (steps * 2)
            x = 16 + (2.2 if (k // 2) % 2 else -2.2) * (1 - t * 0.5) * (1 if k % 2 else 0.4)
            y = 20 - u * (20 - top)
            cv.put(x, y, C(LEG[3] if u > 0.5 else LEG[2], 255 - i * 45))
        for j in range(2): cv.put(15 + j, top - 1, C('#ffe680', 255 - i * 45))
    return cv

def jump_land(i, n=5):
    """Aterrizaje (32×16): anillo de polvo y gotas que salpican (si cae en charco, el código lo tiñe o pone el del charco)."""
    cv = Canvas(32, 16)
    t = (i + 1) / n
    rx, ry = 4 + t * 11, 2 + t * 3.5
    for a in range(0, 360, 6):
        x, y = 16 + math.cos(math.radians(a)) * rx, 11 + math.sin(math.radians(a)) * ry
        if math.sin(math.radians(a)) < 0.2 or i < 2:
            cv.put(x, y, C(DUST[3 - min(3, i)] if i < 4 else DUST[1], 230 - i * 30))
    for k in range(6):
        a = math.radians(200 + k * 28)
        d = 3 + t * 10
        x, y = 16 + math.cos(a) * d, 10 + math.sin(a) * d * 0.5 - math.sin(t * math.pi) * 5
        if i < n - 1: cv.put(x, y, C(WATER[2])); cv.put(x, y + 1, C(WATER[1]))
    return cv

def jump_quake(i, n=5):
    """Nivel II (3 de daño al caer): onda de golpe en el suelo con grietas (48×32)."""
    cv = Canvas(48, 32)
    t = (i + 1) / n
    rx, ry = 6 + t * 16, 3 + t * 8
    col = ['#ffe6a0', '#e8b060', '#b47a3a', '#7a4e2a', '#4a3020'][i]
    for a in range(0, 360, 3):
        x, y = 24 + math.cos(math.radians(a)) * rx, 18 + math.sin(math.radians(a)) * ry
        cv.put(x, y, C(col, 240 - i * 25))
        if i < 3: cv.put(x, y + 1, C(DUST[1], 200))
    for k in range(6):                                            # grietas
        a = math.radians(k * 60 + 15)
        L = min(1, t * 1.6) * 7
        for j in range(int(L)):
            cv.put(24 + math.cos(a) * (2 + j), 18 + math.sin(a) * (2 + j) * 0.5 + (j % 2), C(DUST[0], 230 - i * 30))
    return cv

def stunned(i):
    """Aturdido (III, Rana toro), 16×10: tres estrellitas que giran sobre la cabeza del enemigo (bucle)."""
    cv = Canvas(16, 10)
    for k in range(3):
        a = i * math.pi / 2 / 1.5 + k * 2 * math.pi / 3
        x, y = 8 + math.cos(a) * 5.5, 5 + math.sin(a) * 2.5
        c = C('#ffe680') if math.sin(a) > -0.3 else C('#c8a040')
        cv.put(x, y, c); cv.put(x - 1, y, c); cv.put(x + 1, y, c); cv.put(x, y - 1, c); cv.put(x, y + 1, c)
        if math.sin(a) > 0.3: cv.put(x, y, C(SPARK[0]))
    return cv

# =================================================================================================
# Patas hilanderas
# =================================================================================================
def silk_thread(i):
    """Tramo del hilo (8×4) que se repite entre la salida y la llegada de la esquiva; un brillo lo recorre."""
    cv = Canvas(8, 4)
    for x in range(8):
        cv.put(x, 1, C(SILKR[2], 230)); cv.put(x, 2, C(SILKR[0], 150))
    cv.put((i * 4) % 8, 1, C(SILKR[4])); cv.put((i * 4 + 1) % 8, 1, C(SILKR[3]))
    return cv

def silk_anchor(i):
    """Nudo de un extremo del hilo (8×8)."""
    cv = Canvas(8)
    ell5(cv, 3.5, 3.5, 2.2, 2.0, SILKR); cv.outline(C('#5a6460'))
    if i: cv.put(3, 2, C(SILKR[4]))
    return cv

def silk_start(i):
    """Salida del hilo al empezar la esquiva (16×16): estallido de fibras de seda alrededor del nudo."""
    cv = Canvas(16)
    r = 2 + i * 2
    for a in range(10):
        ang = a * math.pi / 5 + 0.3
        for j in range(2):
            x, y = 8 + math.cos(ang) * (r - j), 8 + math.sin(ang) * (r - j)
            cv.put(x, y, C(SILKR[4 - min(4, i + j)], 255 - i * 50))
    if i < 2: ell5(cv, 8, 8, 2.2 - i * 0.6, 2.0 - i * 0.6, SILKR)
    return cv

def silk_hit(i):
    """Un enemigo toca el hilo y recibe daño: chispa de seda (12×12)."""
    cv = Canvas(12)
    r = 1 + i * 1.6
    for a in range(8):
        ang = a * math.pi / 4
        cv.put(6 + math.cos(ang) * r, 6 + math.sin(ang) * r, C(SILKR[4 - min(4, i)] if i < 3 else SILKR[0]))
    if i == 0: cv.put(6, 6, C(SPARK[0])); cv.put(5, 6, C(SILKR[3])); cv.put(7, 6, C(SILKR[3]))
    return cv

def silk_web(appear=1.0, gone=0.0):
    """Telaraña del cruce de hilos (III): como la de la araña, con gotas de rocío doradas para que se lea como tuya."""
    cv = E.web(appear=appear, gone=gone)
    if appear >= 1 and gone < 0.5:
        for (x, y) in ((9, 12), (22, 10), (14, 22), (24, 19), (7, 18)):
            cv.put(x, y, C('#ffd25a')); cv.put(x, y - 1, C('#fff4c0'))
    return cv

# =================================================================================================
# Iconos (32 y 16)
# =================================================================================================
def icon_jump(n):
    """Pata de rana en Z (muslo, pierna, pie largo con dedos) y un trazo de salto."""
    cv = Canvas(n); k = n / 32
    rell5(cv, 18 * k, 11 * k, 8.5 * k, 5 * k, -0.5, LEG)                    # muslo
    rell5(cv, 10 * k, 18.5 * k, 6.2 * k, 2.4 * k, 0.95, LEG, shift=-1)      # pierna
    rell5(cv, 15 * k, 25 * k, 8.5 * k, 2.0 * k, 0.0, LEG, shift=-1)         # pie
    cv.outline(); cv.clean()
    toes(cv, 23 * k, 25 * k, 1)
    if n >= 32:
        for (bx, by) in ((14, 9), (18, 7), (22, 9)): cv.put_in(bx, by, C(LEG_B[0])); cv.put_in(bx + 1, by + 1, C(LEG_B[1]))
    for j in range(int(4 * k) + 1): cv.put(3 * k + j, 6 * k - j * 0.5, C('#ffe680'))   # trazo de salto
    return cv

def icon_spinner(n):
    cv = Canvas(n); k = n / 32
    ell5(cv, 10 * k, 22 * k, 5 * k, 4.4 * k, SILKR); cv.outline(); cv.clean()
    for s, (kx, ky, tx, ty) in enumerate(((16, 4, 27, 14), (21, 6, 28, 24))):
        thick(cv, (12 * k, 18 * k), (kx * k, ky * k), (CHITIN[3], CHITIN[1]), 2 if n >= 32 else 1)
        for (x, y) in E.line_pts(kx * k, ky * k, tx * k, ty * k): cv.put(x, y, C(CHITIN[2]))
        cv.put(kx * k, ky * k, C(CHITIN[4]))
    for (x, y) in E.line_pts(6 * k, 26 * k, 3 * k, 29 * k): cv.put(x, y, C(SILKR[3]))            # hilo
    return cv

# =================================================================================================
# 4b, Pieles: retocan el cuerpo SOLO dentro de su silueta (ni contorno ni ojos); nada sobresale
# =================================================================================================
SKIN_ANIMS = {'Frog': ['Idle', 'Move', 'Dodge', 'Attack'],
              'Diplocaulus': ['Idle', 'Move', 'Dodge', 'AttackStart'],
              'Whipscorpion': ['Idle', 'Move', 'Dodge', 'AttackStart', 'AttackLoop', 'AttackEnd']}
SKIN_NOFACE = {'Diplocaulus': ['Spin', 'SpinEnd', 'Dizzy']}           # hojas sin dirección
MUCUS = ['#1e5a50', '#3a8a78', '#7ad8c8', '#c8f4ea', '#f0fffa']        # moco: verde agua
CARBON = ['#141114', '#221c1e', '#352b2a', '#4c3e38', '#6a5848']       # carbón (paleta del piso)
EMBER = ['#5a1a14', '#a8341c', '#e8682a', '#ffb24a', '#fff0b0']        # ascuas

def lum(c): return 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2]
def mixc(a, b, t): return tuple(int(round(a[k] * (1 - t) + b[k] * t)) for k in range(3)) + (255,)

def body_pixels(im):
    """Píxeles del cuerpo que se pueden retocar: opacos, ni contorno ni ojos (blanco roto y pupila)."""
    px = im.load(); out = {}
    for y in range(im.height):
        for x in range(im.width):
            c = px[(x, y)]
            if c[3] < 255 or c[:3] == OUTL[:3] or c[:3] == EYE[:3] or lum(c) > 235: continue
            out[(x, y)] = c
    return out

def skin_sheet(frames, fn):
    """Capa de piel (mismo tamaño que el cuerpo, va ENCIMA): solo los píxeles que cambian."""
    out = []
    for i, im in enumerate(frames):
        bp = body_pixels(im)
        if not bp: out.append(Image.new('RGBA', im.size, (0, 0, 0, 0))); continue
        xs = [p[0] for p in bp]; ys = [p[1] for p in bp]
        box = (min(xs), min(ys), max(xs), max(ys))
        ls = sorted(lum(c) for c in bp.values())
        q = lambda f: ls[min(len(ls) - 1, int(len(ls) * f))]
        layer = Image.new('RGBA', im.size, (0, 0, 0, 0)); lp = layer.load()
        for (x, y), c in fn(bp, box, (q(0.25), q(0.8), q(0.93)), i).items():
            if (x, y) in bp and c != bp[(x, y)]: lp[(x, y)] = c
        out.append(layer)
    return out

def amphibian(bp, box, qs, i):
    """Piel anfibia: tono húmedo muy ligero, brillos que se mueven por la parte iluminada y gotitas colgando abajo."""
    x0, y0, x1, y1 = box; lo, hi, top = qs
    out = {}
    for (x, y), c in bp.items():
        nc = mixc(c, C(MUCUS[2]), 0.14)                                   # película húmeda
        L = lum(c)
        if L >= top and ((x - x0) * 2 + 3 * (y - y0) + i) % 11 == 0: nc = C(MUCUS[4])    # brillos sueltos
        elif L >= hi: nc = mixc(nc, C(MUCUS[3]), 0.25)                    # la parte iluminada, más brillante
        out[(x, y)] = nc
    # gotitas: en el píxel más bajo de algunas columnas (se van turnando cuadro a cuadro)
    bottom = {}
    for (x, y) in bp: bottom[x] = max(bottom.get(x, -1), y)
    for x, y in bottom.items():
        if ((x - x0) * 3 + i * 2) % 7 == 0 and (x, y) in bp:
            out[(x, y)] = C(MUCUS[2])
            if (x, y - 1) in bp: out[(x, y - 1)] = C(MUCUS[3])
    return out

CRACKS = [((0.28, 0.42), (0.4, 0.52), (0.52, 0.46)),                   # grietas de ascuas, en coordenadas del cuerpo
          ((0.58, 0.68), (0.7, 0.6))]

def charred(bp, box, qs, i):
    """Escamas carbonizadas: el cuerpo se oscurece hacia el carbón conservando su forma, escamas al tresbolillo y
    grietas de ascuas que laten (i = cuadro)."""
    x0, y0, x1, y1 = box; lo, hi, top = qs
    w, h = max(1, x1 - x0), max(1, y1 - y0)
    out = {}
    for (x, y), c in bp.items():
        L = lum(c)
        k = 0 if L < lo else 2 if L < hi else 3                          # tono de carbón según la luz original
        nc = mixc(c, C(CARBON[k]), 0.62)
        lx, ly = x - x0, y - y0
        row = ly // 3
        if ly % 3 == 2 and (lx + (row % 2) * 2) % 4 != 3: nc = mixc(nc, C(CARBON[0]), 0.3)   # borde de escama
        elif ly % 3 == 0 and (lx + (row % 2) * 2) % 4 == 1 and L >= lo: nc = C(CARBON[4])     # brillo de ceniza
        out[(x, y)] = nc
    glow = (1, 2, 3, 2)[i % 4]                                            # las ascuas laten
    for crack in CRACKS:
        for a, b in zip(crack, crack[1:]):
            for (u, v) in E.line_pts(x0 + a[0] * w, y0 + a[1] * h, x0 + b[0] * w, y0 + b[1] * h):
                p = (int(u), int(v))
                if p in bp: out[p] = C(EMBER[glow])
    return out

# --- efectos de la Piel anfibia -------------------------------------------------------------------
def mucus_charge(i):
    """En agua o charco: el moco se carga (32×32, bucle): burbujitas verde agua que suben alrededor de los pies."""
    cv = Canvas(32)
    for k in range(6):
        ph = (i / 4 + k / 6) % 1
        x = 16 + math.sin(k * 2.1) * 9; y = 28 - ph * 14
        r = 0.8 + (k % 2) * 0.6
        if r > 1: bubble(cv, x, y, r + 0.4, a=int(240 * (1 - ph * 0.6)))
        else: cv.put(x, y, C(MUCUS[4], int(255 * (1 - ph * 0.6)))); cv.put(x, y + 1, C(MUCUS[2], int(200 * (1 - ph))))
    return cv

def mucus_shield(i, absorb=None, reflect=False):
    """Moco cargado (absorbe un golpe): película translúcida verde agua alrededor del cuerpo (48×48). absorb 0..1
    la revienta en gotas; reflect (III) la hace destellar."""
    cv = Canvas(48)
    if absorb is None:
        for a in range(0, 360, 4):
            r = 17 + math.sin(math.radians(a) * 3 + i * 1.5) * 0.8
            x, y = 24 + math.cos(math.radians(a)) * r, 25 + math.sin(math.radians(a)) * r * 0.85
            hi_ = math.cos(math.radians(a + 135)) > 0.6
            col = MUCUS[4] if reflect and (a // 4 + i) % 5 == 0 else MUCUS[3] if hi_ else MUCUS[2]
            cv.put(x, y, C(col, 230 if hi_ or reflect else 150))
        for k in range(3):                                                # brillo en arco arriba a la izquierda
            cv.put(14 + k, 12 - k // 2, C(MUCUS[4], 220))
        if reflect:
            cv.put(24 + 17 * math.cos(i), 25 + 14 * math.sin(i), C(SPARK[0]))
    else:
        for k in range(12):
            a = k * math.pi / 6
            d = 17 + absorb * 8
            x, y = 24 + math.cos(a) * d, 25 + math.sin(a) * d * 0.85 + absorb * absorb * 6
            cv.put(x, y, C(MUCUS[3], int(255 * (1 - absorb * 0.7)))); cv.put(x, y + 1, C(MUCUS[1], int(200 * (1 - absorb))))
    return cv

# --- efectos de las Escamas carbonizadas ----------------------------------------------------------
def flame(cv, x, y, h, ph):
    """Llamita de h px (para cuerpos que arden o ataques que queman)."""
    for k in range(int(h)):
        t = k / h
        w = (1 - t) * 1.6
        sway = math.sin(ph + t * 3) * t * 1.2
        for d in range(-int(w + .5), int(w + .5) + 1):
            col = FIRE[4] if t < 0.25 and d == 0 else FIRE[3] if t < 0.45 else FIRE[2] if t < 0.75 else FIRE[1]
            cv.put(x + d + sway, y - k, C(col))

def embers_primed(i):
    """Tras pisar fuego tus ataques queman 4 s: ascuas que suben del cuerpo (32×32, bucle)."""
    cv = Canvas(32)
    for k in range(10):
        ph = (i / 4 + k / 10) % 1
        x = 16 + math.sin(k * 2.7 + ph * 3) * 9; y = 27 - ph * 22
        col = C(EMBER[4 - int(ph * 3)], int(255 * (1 - ph * 0.5)))
        cv.put(x, y, col)
        if ph < 0.5: cv.put(x, y + 1, C(EMBER[2], int(220 * (1 - ph))))
    return cv

def burning(i):
    """Enemigo ardiendo (16×16, bucle): sirve también para las reacciones de la tanda 5."""
    cv = Canvas(16)
    flame(cv, 8, 14, 9 + (i % 2), i * 1.7)
    flame(cv, 4, 14, 5 + ((i + 1) % 2), i * 1.7 + 2)
    flame(cv, 12, 14, 6, i * 1.7 + 4)
    return cv

def blaze(i, n=6):
    """Brasa (III): al recibir un golpe estallas en llamas (48×48)."""
    cv = Canvas(48)
    t = (i + 1) / n
    r = 6 + t * 14
    for k in range(10):
        a = k * math.pi / 5 + 0.2
        x, y = 24 + math.cos(a) * r, 30 + math.sin(a) * r * 0.55
        if i < n - 1: flame(cv, x, y, 9 * (1 - t * 0.6) + (k % 3), i + k)
        else: cv.put(x, y - 2, C('#6a6466', 140))
    return cv

# --- iconos ---------------------------------------------------------------------------------------
def icon_amphibian(n):
    cv = Canvas(n); k = n / 32
    ell5(cv, 16 * k, 17 * k, 11 * k, 9 * k, five([MUCUS[1], MUCUS[2], MUCUS[3], MUCUS[4]]))
    cv.outline(); cv.clean()
    for (x, y) in ((10, 12), (12, 11), (11, 14)): cv.put_in(x * k, y * k, C(MUCUS[4]))
    bubble(cv, 22 * k, 20 * k, 2.4 * k)
    cv.put(16 * k, 27 * k, C(MUCUS[2])); cv.put(16 * k, min(n - 2, 28 * k + 1), C(MUCUS[3]))      # gota
    return cv

def icon_charred(n):
    cv = Canvas(n); k = n / 32
    ell5(cv, 16 * k, 16 * k, 11 * k, 10 * k, CARBON)
    cv.outline(); cv.clean()
    for row in range(int(8 * k), int(26 * k), max(2, int(3 * k))):
        for x in range(int(6 * k), int(27 * k)):
            if (x + (row // max(2, int(3 * k))) % 2 * 2) % 4 != 3: cv.put_in(x, row, C(CARBON[0]))
    for (a, b) in (((9, 12), (15, 16)), ((15, 16), (19, 14)), ((19, 14), (24, 20))):
        for (x, y) in E.line_pts(a[0] * k, a[1] * k, b[0] * k, b[1] * k): cv.put_in(x, y, C(EMBER[3]))
    return cv

def skins_jump():
    """Capas de las dos Pieles para las poses del salto de las Patas de salto (Jump y JumpCharge, 40×40), calculadas de las hojas de
    ../floor3-bodies como las demás. Solo estas hojas: las de las otras animaciones no se tocan."""
    comp = {}
    for mut, fn in (('AmphibianSkin', amphibian), ('CharredScales', charred)):
        for body in SKIN_ANIMS:
            for a in ('Jump', 'JumpCharge'):
                for f in FACINGS:
                    frames = frames_of(os.path.join(BODIES, body, f'Player_{body}_{a}_{f}.png'))
                    layer = skin_sheet(frames, fn)
                    save(os.path.join(OUT, 'Skin', f'Skin_{mut}_{body}_{a}_{f}.png'), layer)
                    if f == 'side':
                        rows = []
                        for fr, ly in zip(frames, layer):
                            c = fr.copy(); c.alpha_composite(ly); rows.append(c)
                        comp[f'{mut} {body} {a}_{f}'] = rows
        preview(os.path.join(OUT, f'vista_previa_piel_salto_{mut}.png'), {k: v for k, v in comp.items() if k.startswith(mut)}, BG, scale=3)

def skins():
    B = BODIES
    comp = {}
    for mut, fn in (('AmphibianSkin', amphibian), ('CharredScales', charred)):
        for body, anims in SKIN_ANIMS.items():
            sheets = [(f'{a}_{f}', f'Player_{body}_{a}_{f}') for a in anims for f in FACINGS]
            sheets += [(a, f'Player_{body}_{a}') for a in SKIN_NOFACE.get(body, [])]
            for tag, src in sheets:
                frames = frames_of(os.path.join(B, body, src + '.png'))
                layer = skin_sheet(frames, fn)
                save(os.path.join(OUT, 'Skin', f'Skin_{mut}_{body}_{tag}.png'), layer)
                if tag in ('Idle_side', 'Idle_down', 'Idle_up', 'Move_side') or tag.startswith(('Attack_side', 'AttackLoop_side', 'Spin')):
                    rows = []
                    for fr, ly in zip(frames, layer):
                        c = fr.copy(); c.alpha_composite(ly); rows.append(c)
                    comp[f'{mut} {body} {tag}'] = rows
        preview(os.path.join(OUT, f'vista_previa_piel_{mut}.png'), {k: v for k, v in comp.items() if k.startswith(mut)}, BG, scale=3)
        os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
        for body in SKIN_ANIMS:
            gif(os.path.join(OUT, 'gif', f'{mut}_{body}_Move_side.gif'), comp[f'{mut} {body} Move_side'], 110, BG, scale=4)
    fx = {'Fx_AmphibianSkin_Charge': [mucus_charge(i) for i in range(4)],
          'Fx_AmphibianSkin_Shield': [mucus_shield(i) for i in range(4)],
          'Fx_AmphibianSkin_Absorb': [mucus_shield(0, absorb=v) for v in (0.1, 0.35, 0.6, 0.85)],
          'Fx_AmphibianSkin_Reflect': [mucus_shield(i, reflect=True) for i in range(4)],
          'Fx_CharredScales_Primed': [embers_primed(i) for i in range(4)],
          'Fx_Burning': [burning(i) for i in range(4)],
          'Fx_CharredScales_Blaze': [blaze(i) for i in range(6)]}
    for name, frames in fx.items(): save(os.path.join(OUT, 'Effects', name + '.png'), frames)
    preview(os.path.join(OUT, 'vista_previa_efectos_4b.png'), fx, BG, scale=4)
    icons = {}
    for nm, fn in (('AmphibianSkin', icon_amphibian), ('CharredScales', icon_charred)):
        for n in (32, 16):
            save(os.path.join(OUT, 'Icons', f'Icon_{nm}_{n}.png'), [fn(n)]); icons[f'Icon_{nm}_{n}'] = [fn(n)]
    preview(os.path.join(OUT, 'vista_previa_iconos_4b.png'), icons, BG, scale=6)

# =================================================================================================
# 4c, Órganos: no se ven en el cuerpo, así que sus efectos son detallados (forma, brillo y partículas)
# =================================================================================================
METH = ['#1e5a78', '#2a90b8', '#6ad0ec', '#d0f8ff']          # metano (paleta de las reacciones)
PHERO = ['#7a2a3a', '#c04a5a', '#f47a8a', '#ffb4b8', '#ffe4e0']   # feromonas: rosa coral
PARA = ['#3a0e1e', '#6a1630', '#9a2044', '#d04a62', '#f4a0a4']    # clase Parásito: granate
SMOKE = ['#3a3436', '#5a5254', '#7a7274', '#9a9294']
HEART = ['#a33b3b', '#e05a5a', '#ff9a8a']

def soft(cv, cx, cy, blobs, pal, alpha=1.0, swirl=0.0, seed=0):
    """Nube blanda: suma de bolas (dx, dy, r); 3 escalones de opacidad (sin degradado suave) y luz arriba a la
    izquierda. swirl dibuja volutas claras dentro."""
    import random
    rnd = random.Random(seed)
    xs = [cx + b[0] for b in blobs]; ys = [cy + b[1] for b in blobs]
    R = max(b[2] for b in blobs)
    for y in range(int(min(ys) - R - 1), int(max(ys) + R + 2)):
        for x in range(int(min(xs) - R - 1), int(max(xs) + R + 2)):
            d = 0.0
            for (bx, by, r) in blobs:
                q = ((x + .5 - cx - bx) ** 2 + (y + .5 - cy - by) ** 2) / (r * r)
                if q < 1: d += (1 - q) ** 2
            if d < 0.08: continue
            lit = -(x - cx) * 0.4 - (y - cy) * 0.6
            k = 3 if d > 0.9 and lit > 2 else 2 if d > 0.45 else 1 if d > 0.2 else 0
            a = (95, 150, 190, 215)[k] * alpha
            if a > 8: cv.put(x, y, C(pal[k], int(a)))
    if swirl > 0:                                                        # volutas claras que giran
        for j in range(3):
            a0 = swirl * 2 * math.pi + j * 2.1
            for t in range(10):
                ang = a0 + t * 0.35; rr = 1.5 + t * 0.55
                x, y = cx + blobs[j % len(blobs)][0] * 0.5 + math.cos(ang) * rr, cy + blobs[j % len(blobs)][1] * 0.5 + math.sin(ang) * rr * 0.7
                if (int(x), int(y)) in cv.px: cv.put(x, y, C(pal[3], int(225 * alpha)))

# --- Saco de metano -------------------------------------------------------------------------------
def meth_release(i, n=7):
    """Al activarlo (48×48): una vejiga translúcida brota sobre el jugador, se hincha con venas, tiembla y revienta
    soltando la nube. El cuadro que cuenta (la nube sale) es el 5."""
    cv = Canvas(48)
    cx, base = 24, 30
    if i <= 4:
        g = (0.35, 0.6, 0.85, 1.0, 1.08)[i]
        wob = (0, 0.3, -0.3, 0.5, -0.6)[i]
        rx, ry = 9 * g + wob, 8 * g - wob
        cy = base - ry * 0.9
        for y in range(int(cy - ry - 1), int(cy + ry + 2)):
            for x in range(int(cx - rx - 1), int(cx + rx + 2)):
                q = ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2
                if q > 1: continue
                edge = q > 0.72
                lit = (x - cx) / rx * -0.6 + (y - cy) / ry * -0.8
                col = METH[3] if lit > 0.55 and not edge else METH[2] if edge or lit > 0.1 else METH[1]
                cv.put(x, y, C(col, 235 if edge else 150))
        for (a, l) in ((-2.4, 0.7), (-1.0, 0.8), (-1.8, 0.5)):              # venas
            for t in range(int(rx * l)):
                x = cx + math.cos(a) * t; y = cy + ry * 0.6 + math.sin(a) * t * 0.9
                if (int(x), int(y)) in cv.px: cv.put(x, y, C(METH[0], 200))
        cv.put(cx - rx * 0.45, cy - ry * 0.5, C(SPARK[0])); cv.put(cx - rx * 0.45 + 1, cy - ry * 0.5, C(METH[3]))
        for k in range(i):                                                  # burbujitas dentro
            bubble(cv, cx + math.sin(k * 2.3) * rx * 0.5, cy + ry * 0.4 - k * 2, 1.2, a=170)
        if i == 4:                                                          # a punto de reventar: tensión
            for a in range(0, 360, 45):
                cv.put(cx + math.cos(math.radians(a)) * (rx + 2), cy + math.sin(math.radians(a)) * (ry + 2), C(METH[3], 200))
    else:
        t = (i - 4) / (n - 4)
        soft(cv, cx, base - 8, [(-6 * t - 2, 0, 6 + 4 * t), (6 * t + 2, 1, 6 + 4 * t), (0, -4 * t, 7 + 3 * t)], METH,
             alpha=1.0, swirl=t, seed=i)
        for k in range(8):                                                  # jirones de la vejiga
            a = k * math.pi / 4 + 0.3
            d = 10 + t * 10
            x, y = cx + math.cos(a) * d, base - 8 + math.sin(a) * d * 0.8
            cv.put(x, y, C(METH[3], int(255 * (1 - t * 0.6)))); cv.put(x + math.cos(a), y + math.sin(a), C(METH[2], int(200 * (1 - t))))
    return cv

def meth_cloud(i, n=6, fade=0.0):
    """Nube a la deriva (40×32, bucle de 6): bolas que respiran, volutas que giran y burbujas que suben; más
    redonda y clara que la niebla del escenario para que se lea como tuya. fade 0..1 la deshace."""
    cv = Canvas(40, 32)
    ph = i / n * 2 * math.pi
    blobs = [(-8 + math.sin(ph) * 0.8, 2, 7.5 + math.sin(ph + 1) * 0.5), (7 + math.cos(ph) * 0.8, 2, 7.5 + math.cos(ph) * 0.5),
             (0, -3 + math.sin(ph + 2) * 0.6, 8.5), (-2, 5, 6.5)]
    if fade > 0: blobs = [(b[0] * (1 + fade * 0.6), b[1] - fade * 3, b[2] * (1 - fade * 0.25)) for b in blobs]
    soft(cv, 20, 17, blobs, METH, alpha=1 - fade * 0.8, swirl=i / n, seed=i)
    for k in range(4):                                                      # burbujas que suben y brillan
        t = (i / n + k / 4) % 1
        x, y = 20 + math.sin(k * 2.4) * 10, 24 - t * 18
        if fade < 0.7: bubble(cv, x, y, 1.1 + (k % 2) * 0.5, a=int(220 * (1 - t) * (1 - fade)))
    if fade == 0 and i % 3 == 0: cv.put(12, 10, C(SPARK[0]))                # destello que avisa de que es inflamable
    return cv

def meth_explode(i, n=8):
    """Explosión de la nube (64×64): destello azul del metano, bola de fuego, anillo de onda y humo."""
    cv = Canvas(64)
    c = 32
    if i == 0:
        soft(cv, c, c, [(0, 0, 10)], [METH[1], METH[2], METH[3], '#ffffff'], alpha=1.2)
        sparks(cv, c, c, 3)
        return cv
    t = i / (n - 1)
    R = 8 + t * 20
    if i <= 4:                                                              # bola de fuego con borde azul
        for y in range(int(c - R - 1), int(c + R + 2)):
            for x in range(int(c - R - 1), int(c + R + 2)):
                d = math.hypot(x + .5 - c, (y + .5 - c) * 1.1) / R
                if d > 1: continue
                n_ = math.sin(x * 1.3 + i) * math.cos(y * 1.1 - i) * 0.12
                v = d + n_
                col = FIRE[4] if v < 0.3 - t * 0.2 else FIRE[3] if v < 0.55 else FIRE[2] if v < 0.78 else FIRE[1] if v < 0.92 else METH[1]
                if i >= 4 and v < 0.3: col = SMOKE[3]
                cv.put(x, y, C(col))
    for a in range(0, 360, 3):                                              # anillo de onda
        rr = R + 4
        x, y = c + math.cos(math.radians(a)) * rr, c + math.sin(math.radians(a)) * rr * 0.9
        if i < 6: cv.put(x, y, C(FIRE[3] if i < 3 else METH[2], 230 - i * 30))
    if i >= 5:                                                              # humo que sube
        s_ = (i - 4) / (n - 4)
        soft(cv, c, c - s_ * 10, [(-8, 0, 7 + s_ * 4), (8, 2, 7 + s_ * 4), (0, -6, 8 + s_ * 3)], SMOKE,
             alpha=1 - s_ * 0.75, seed=i)
    for k in range(10):                                                     # chispas que salen
        a = k * 0.63 + 0.2
        d = R + 2 + t * 10
        if i < 6: cv.put(c + math.cos(a) * d, c + math.sin(a) * d, C(FIRE[4] if i < 3 else FIRE[2]))
    return cv

def ground_fire(i, n=6):
    """Incendio (III): fuego en el suelo 3 s (32×24, bucle). Genérico: servirá también en la tanda 5."""
    cv = Canvas(32, 24)
    for k, (x, h) in enumerate(((7, 8), (12, 12), (17, 14), (22, 11), (26, 7), (15, 9))):
        flame(cv, x, 21, h + ((i + k) % 3) - 1, i * 1.3 + k * 1.7)
    for x in range(4, 29):                                                  # brasas en la base
        if (x + i) % 3: cv.put(x, 22, C(EMBER[1 + (x + i) % 3]))
    return cv

# --- Feromonas ------------------------------------------------------------------------------------
def wisp(cv, x, y, ang, L, col, a=255, ph=0.0, curl=True):
    """Voluta de olor: trazo que ondula y acaba en un rizo; 2 px de grosor al principio (claro encima, oscuro debajo)."""
    pts = []
    for t in range(int(L)):
        w = math.sin(t * 0.6 + ph) * 1.4
        pts.append((x + math.cos(ang) * t - math.sin(ang) * w, y + math.sin(ang) * t + math.cos(ang) * w, t / L))
    if curl and pts:
        ex, ey, _ = pts[-1]
        for t in range(7):
            a2 = ang + math.pi / 2 + t * 0.8
            pts.append((ex + math.cos(a2) * 1.6 - math.cos(ang) * 1.6, ey + math.sin(a2) * 1.6 - math.sin(ang) * 1.6, 1.0))
    dark = PHERO[1]
    for (px, py, u) in pts:
        al = int(a * (1 - u * 0.45))
        if u < 0.5: cv.put(px, py + 1, C(dark, al))
        cv.put(px, py, C(col, al))

def phero_release(i, n=7):
    """Al activarlas (72×72): onda rosa coral hasta 2 u (32 px) con volutas de olor que giran hacia fuera; el radio
    del efecto se lee en el cuadro 4 (onda al máximo)."""
    cv = Canvas(72)
    c = 36
    t = min(1, (i + 1) / 5)
    R = 4 + t * 28
    fade = max(0, (i - 4) / (n - 4))
    if i == 0: ell5(cv, c, c, 4, 4, PHERO); cv.outline(C(PHERO[0]))
    for y in range(72):                                                    # velo rosa: la zona del efecto
        for x in range(72):
            if ((x + .5 - c) / R) ** 2 + ((y + .5 - c) / (R * 0.85)) ** 2 <= 1 and (x + y) % 2 == 0:
                cv.put(x, y, C(PHERO[2], int(45 * (1 - fade))))
    for a in range(0, 360, 2):                                             # anillo doble
        for (rr, col, al) in ((R, PHERO[3], 230), (R - 2, PHERO[2], 160)):
            x, y = c + math.cos(math.radians(a)) * rr, c + math.sin(math.radians(a)) * rr * 0.85
            if (a // 2 + i) % 6 or rr == R: cv.put(x, y, C(col, int(al * (1 - fade))))
    for k in range(6):                                                     # volutas que giran hacia fuera
        ang = k * math.pi / 3 + t * 1.2
        r0 = R * 0.4
        wisp(cv, c + math.cos(ang) * r0, c + math.sin(ang) * r0 * 0.85, ang + 0.5, 5 + t * 6,
             PHERO[3] if k % 2 else PHERO[4], a=int(240 * (1 - fade)), ph=i)
    for k in range(6):                                                     # motas brillantes
        ang = k * 1.05 + i * 0.4
        rr = R * (0.3 + (k % 3) * 0.2)
        cv.put(c + math.cos(ang) * rr, c + math.sin(ang) * rr * 0.85, C(PHERO[4], int(255 * (1 - fade))))
    return cv

def phero_aura(i, n=4):
    """Sobre un enemigo confundido (32×32, bucle): volutas rosas que suben del cuerpo."""
    cv = Canvas(32)
    for k in range(5):
        ph = (i / n + k / 5) % 1
        x = 16 + math.sin(k * 2.5) * 9; y = 28 - ph * 20
        wisp(cv, x, y, -math.pi / 2, 6, PHERO[4] if k % 2 else PHERO[3], a=int(255 * (1 - ph * 0.7)), ph=i + k)
    return cv

def phero_mark(i, frenzy=False):
    """Marca sobre la cabeza del confundido (14×12, bucle): espiral rosa que gira; frenzy (III, doble daño entre
    ellos) añade el signo de furia rojo."""
    cv = Canvas(14, 12)
    c = (7, 6)
    for t in range(16):
        ang = t * 0.55 + i * math.pi / 2
        rr = 0.6 + t * 0.28
        cv.put(c[0] + math.cos(ang) * rr, c[1] + math.sin(ang) * rr * 0.8, C(PHERO[4] if t < 5 else PHERO[3] if t < 11 else PHERO[2]))
    if frenzy:                                                            # signo de furia: cuatro trazos en cruz
        x, y = 11, 2
        col = C('#ff4a3a') if i % 2 == 0 else C('#c82a24')
        for (dx, dy) in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            cv.put(x + dx * 2, y + dy, col); cv.put(x + dx, y + dy * 2, col)
    return cv

# --- Parásito (clase nueva) -----------------------------------------------------------------------
def parasite(cv, x, y, ph=0.0, curl=0.0, ang=0.0, k=1.0):
    """Gusano parásito granate de ~9 px: 4 anillos que laten y una ventosa clara delante (ang = hacia dónde)."""
    ca, sa = math.cos(ang), math.sin(ang)
    for j in range(4):
        t = j - 1.5
        bend = curl * t * t * 0.6
        px, py = x - ca * t * 2.0 * k - sa * bend, y - sa * t * 2.0 * k + ca * bend
        r = (1.9 - abs(t) * 0.25 + math.sin(ph + j) * 0.25) * k
        ell5(cv, px, py, r, r * 0.9, five(PARA[1:]) if j else PARA)
    cv.outline(); cv.clean()
    hx, hy = x + ca * 3.6 * k, y + sa * 3.6 * k                           # ventosa
    cv.put(hx, hy, C(PARA[4])); cv.put(hx + ca, hy + sa, C(PARA[3]))
    for j in range(3):                                                      # anillos marcados
        t = j - 1
        cv.put_in(x - ca * t * 2 * k, y - sa * t * 2 * k - 1, C(PARA[0]))

def parasite_attached(i):
    """Pegado al enemigo (12×12, bucle): la ventosa hacia abajo (clavada) y el cuerpo que bombea."""
    cv = Canvas(12)
    parasite(cv, 6, 5, ph=i * math.pi / 2, curl=0.2 * math.sin(i * math.pi / 2), ang=math.pi / 2 + 0.3)
    if i % 2 == 0: cv.put(5, 10, C(PARA[3]))                               # gota que sorbe
    return cv

def parasite_leap(i, n=5):
    """Al morir el huésped salta al siguiente (16×16): se enrosca y gira (el código lo mueve en arco)."""
    cv = Canvas(16)
    parasite(cv, 8, 8, ph=i, curl=0.9, ang=i * 2 * math.pi / n)
    return cv

def drain_mote(i):
    """Gota de vida que viaja del enemigo al jugador (8×8, bucle de 2)."""
    cv = Canvas(8)
    ell5(cv, 4, 4, 1.8 + i * 0.3, 1.8 + i * 0.3, five(PARA[1:]))
    cv.put(3, 3, C(PARA[4]))
    blend(cv, 1, 5, PARA[3], 0.5); blend(cv, 0, 6, PARA[2], 0.3)          # estela
    return cv

def para_heal(i, n=6):
    """Cada 15 chupados, ½ corazón (32×32): anillos granates que se cierran sobre el jugador y medio corazón que sube."""
    cv = Canvas(32)
    t = i / (n - 1)
    R = 14 * (1 - t) + 2
    for a in range(0, 360, 10):
        x, y = 16 + math.cos(math.radians(a)) * R, 18 + math.sin(math.radians(a)) * R * 0.7
        if (a // 10 + i) % 3: cv.put(x, y, C(PARA[3] if a % 20 else PARA[4], 230))
    if i >= 2:                                                              # medio corazón (mitad izquierda)
        hy = 14 - (i - 2) * 2
        heart = ['.##.', '####', '####', '.###', '..##', '...#']
        for r, row in enumerate(heart):
            for c_, ch in enumerate(row):
                if ch == '#':
                    col = HEART[2] if (r == 1 and c_ == 1) else HEART[0] if r >= 3 and c_ == 3 - (5 - r) + 2 else HEART[1]
                    cv.put(12 + c_, hy + r, C(col))
        hc = Canvas(32); hc.px = {p: q for p, q in cv.px.items() if 12 <= p[0] <= 15 and hy <= p[1] <= hy + 5}
        hc.outline()
        for p_, q in hc.px.items():
            if p_ not in cv.px: cv.px[p_] = q
    return cv

# --- iconos ---------------------------------------------------------------------------------------
def icon_meth(n):
    cv = Canvas(n); k = n / 32
    soft(cv, 16 * k, 17 * k, [(-4 * k, 1 * k, 7 * k), (4 * k, 1 * k, 7 * k), (0, -4 * k, 7.5 * k)], METH, alpha=1.15, swirl=0.3)
    if n >= 32: bubble(cv, 9, 22, 1.6); bubble(cv, 23, 9, 1.2)
    for j in range(int(3 * k) + 1): cv.put(24 * k + j, 24 * k - j, C(FIRE[3]))      # chispa: es inflamable
    cv.put(24 * k + 1, 24 * k - 2, C(SPARK[0]))
    return cv

def icon_phero(n):
    """Glándula rosa coral de la que salen tres volutas con rizo."""
    cv = Canvas(n); k = n / 32
    ell5(cv, 16 * k, 23 * k, 7 * k, 5.5 * k, PHERO)
    cv.outline(); cv.clean()
    cv.put(13 * k, 21 * k, C(PHERO[4]))
    for j in range(3):
        wisp(cv, (11 + j * 5) * k, 16 * k, -math.pi / 2 + (j - 1) * 0.3, (9 if j == 1 else 7) * k,
             PHERO[3] if j != 1 else PHERO[4], ph=j * 2, curl=n >= 32)
    return cv

def icon_parasite(n):
    """Gusano parásito granate enroscado en C, con anillos y la ventosa clara."""
    cv = Canvas(n); k = n / 32
    for j in range(7):                                                     # cuerpo en arco
        a = math.pi * 0.15 + j * math.pi * 1.2 / 6
        r = (3.2 - abs(j - 3) * 0.25) * k
        ell5(cv, 16 * k + math.cos(a) * 8 * k, 16 * k + math.sin(a) * 8 * k, r, r, five(PARA[1:]) if j % 2 else PARA)
    cv.outline(); cv.clean()
    a = math.pi * 0.15 + math.pi * 1.2
    hx, hy = 16 * k + math.cos(a) * 8 * k, 16 * k + math.sin(a) * 8 * k            # ventosa en la punta
    cv.put(hx + 1, hy - 1, C(PARA[4])); cv.put(hx + 2, hy - 1, C(PARA[3]))
    return cv

def organs():
    fx = {'Fx_MethaneSac_Release': [meth_release(i) for i in range(7)],
          'Fx_MethaneSac_Cloud': [meth_cloud(i) for i in range(6)],
          'Fx_MethaneSac_CloudFade': [meth_cloud(i, fade=f) for i, f in enumerate((0.25, 0.5, 0.75, 1.0))],
          'Fx_MethaneSac_Explode': [meth_explode(i) for i in range(8)],
          'Fx_GroundFire': [ground_fire(i) for i in range(6)],
          'Fx_Pheromones_Release': [phero_release(i) for i in range(7)],
          'Fx_Pheromones_Aura': [phero_aura(i) for i in range(4)],
          'Fx_Pheromones_Mark': [phero_mark(i) for i in range(4)],
          'Fx_Pheromones_Frenzy': [phero_mark(i, frenzy=True) for i in range(4)],
          'Parasite_Attached': [parasite_attached(i) for i in range(4)],
          'Parasite_Leap': [parasite_leap(i) for i in range(5)],
          'Fx_Parasite_Drain': [drain_mote(i) for i in range(2)],
          'Fx_Parasite_Heal': [para_heal(i) for i in range(6)]}
    for name, frames in fx.items():
        save(os.path.join(OUT, 'Parasite' if name.startswith('Parasite_') else 'Effects', name + '.png'), frames)
    preview(os.path.join(OUT, 'vista_previa_efectos_4c.png'), fx, BG, scale=3)
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    for name in ('Fx_MethaneSac_Release', 'Fx_MethaneSac_Cloud', 'Fx_MethaneSac_Explode', 'Fx_Pheromones_Release',
                 'Parasite_Attached', 'Fx_Parasite_Heal'):
        gif(os.path.join(OUT, 'gif', name + '.gif'), fx[name], 90, BG, scale=4)
    icons = {}
    for nm, fn in (('MethaneSac', icon_meth), ('Pheromones', icon_phero), ('ClassParasite', icon_parasite)):
        for n in (32, 16):
            save(os.path.join(OUT, 'Icons', f'Icon_{nm}_{n}.png'), [fn(n)]); icons[f'Icon_{nm}_{n}'] = [fn(n)]
    preview(os.path.join(OUT, 'vista_previa_iconos_4c.png'), icons, BG, scale=6)

# =================================================================================================
# 4d, Especiales del ámbar: SÍ cambian el modelo. Las tres en ámbar y cada una en su zona (Cercos detrás,
# Cápsula en el lomo, Gota delante), para que juntas no se pisen.
# =================================================================================================
AMB = ['#6a2e0e', '#b0581a', '#e89a30', '#ffcc5a', '#fff0b0']
CARAMEL = ['#2e1c10', '#5a3a20', '#8a5e34', '#b88a52']         # cercos de cucaracha
SPEC_ANIMS = SKIN_ANIMS

def anchors(body, facing, cells):
    """Puntos de anclaje de cada cuerpo en un cuadro: rear (de donde salen los cercos), back (lomo) y front
    (glándula: lista de puntos). Coordenadas del lienzo de 56×56."""
    X0, Y0, X1, Y1 = bbox(cells); x0, y0, x1, y1 = core_box(cells)
    xc = (x0 + x1) / 2
    def top_at(x):
        ys = [c[1] for c in cells if c[0] == int(x)]; return min(ys) if ys else y0
    def extreme(key, pick):
        return pick(cells, key=key)
    a = {}
    if facing == 'side':
        if body == 'Diplocaulus':
            t = extreme(lambda c: c[0], min); a['rear'] = (t[0] + 1, t[1])
        else: a['rear'] = (x0 + 2, (y0 + y1) / 2 - 2)
        bx = x0 + (x1 - x0) * 0.42; a['back'] = (bx, top_at(bx))
        if body == 'Frog': a['front'] = [(x1 - 5, y1 - 2)]
        elif body == 'Diplocaulus':
            front = [c for c in cells if c[0] >= X1 - (X1 - X0) * 0.3]
            a['front'] = [min(front, key=lambda c: c[1]), max(front, key=lambda c: c[1])]
        else:
            r = max(cells, key=lambda c: c[0]); a['front'] = [(r[0] - 1, r[1])]
    elif facing == 'down':
        if body == 'Diplocaulus':
            t = extreme(lambda c: c[1], min); a['rear'] = (t[0], t[1] + 1)
        else: a['rear'] = (xc, y0 + 2)
        a['back'] = (xc + 2, top_at(xc + 2))
        if body == 'Frog': a['front'] = [(xc, y1 - 3)]
        elif body == 'Diplocaulus':
            a['front'] = [min(cells, key=lambda c: c[0]), max(cells, key=lambda c: c[0])]
        else: a['front'] = [(xc, Y1 - 3)]
    else:
        if body == 'Diplocaulus':
            t = extreme(lambda c: c[1], max); a['rear'] = (t[0], t[1] - 1)
        else: a['rear'] = (xc, y1 - 1)
        a['back'] = (xc, (y0 + y1) / 2 - 1)
        a['front'] = ([min(cells, key=lambda c: c[0]), max(cells, key=lambda c: c[0])] if body == 'Diplocaulus' else [])
    return a

# --- Cercos ---------------------------------------------------------------------------------------
def cercus(cv, x, y, ang, L, ph, ready=True):
    """Cerco segmentado: 2 px en la base, 1 px al final, marcas de segmento y punta de ámbar (encendida si listo)."""
    pts = []
    for t in range(int(L)):
        a = ang + math.sin(ph + t * 0.35) * 0.08 * t / L
        pts.append((x + math.cos(a) * t, y + math.sin(a) * t))
    for j, (px, py) in enumerate(pts[:-2]):
        col = CARAMEL[1] if j % 3 == 2 else CARAMEL[3] if j < L * 0.3 else CARAMEL[2]
        cv.put(px, py, C(col))
        if j < L * 0.45: cv.put(px, py + 1, C(CARAMEL[0] if j % 3 == 2 else CARAMEL[1]))
    tipcol = (AMB[4], AMB[3]) if ready else (AMB[1], AMB[0])
    for j, (px, py) in enumerate(pts[-2:]): cv.put(px, py, C(tipcol[j == 0]))

def cerci_layer(body, im, facing, anim, i):
    off = (NL - im.height) // 2
    cells = cells_of(im, off)
    a = anchors(body, facing, cells)
    ph = i * math.pi / 2 * (1.0 if anim == 'Move' else 0.5)
    cv = Canvas(NL)
    rx, ry = a['rear']
    L = 13 if body == 'Frog' else 11 if body == 'Whipscorpion' else 8
    if facing == 'side':
        for k, ang in enumerate((math.pi + 0.6, math.pi + 0.2)):          # hacia atrás, abiertos en V
            cercus(cv, rx - k, ry + k, ang + math.sin(ph + k) * 0.06, L - k, ph + k)
    elif facing == 'down':
        for s in (-1, 1): cercus(cv, rx + s * 1.5, ry, -math.pi / 2 + s * 0.5, L - 1, ph + s)
    else:
        for s in (-1, 1): cercus(cv, rx + s * 1.5, ry, math.pi / 2 + s * 0.55, L - 1, ph + s)
    if facing != 'up':                                                      # detrás del cuerpo (de espaldas, delante)
        behind(cv, cells)
    return cv.img()

# --- Cápsula de ámbar: costra de resina en el lomo -------------------------------------------------
def bead(cv, x, y, r, bug=False):
    ell5(cv, x, y, r, r * 0.9, AMB)
    if bug:                                                                  # mosquito atrapado
        cv.put(x, y, C(AMB[0])); cv.put(x + 1, y - 1, C(AMB[0])); cv.put(x - 1, y - 1, C(AMB[1]))

def capsule_layer(body, im, facing, anim, i):
    off = (NL - im.height) // 2
    cells = cells_of(im, off)
    a = anchors(body, facing, cells)
    bx, by = a['back']
    cv = Canvas(NL)
    bob = 0
    if facing == 'side': beads = [(bx, by + 1, 2.8, True), (bx - 4, by + 2, 2.0, False), (bx + 3.5, by + 1.5, 1.6, False)]
    elif facing == 'down': beads = [(bx + 4, by + 2.5, 1.7, False)]
    else: beads = [(bx, by, 3.0, True), (bx - 4.5, by + 2, 2.0, False), (bx + 4, by + 2.5, 2.2, False), (bx + 0.5, by + 5, 1.5, False)]
    for (x, y, r, bug) in beads: bead(cv, x, y + bob, r)
    cv.outline(); cv.clean()
    for (x, y, r, bug) in beads:
        if bug: cv.put(x, y, C(AMB[0])); cv.put(x + 1, y - 1, C(AMB[0])); cv.put(x - 1, y - 1, C(AMB[1]))
        g = (i + int(x)) % 4                                                 # brillo que se mueve
        cv.put(x - r * 0.4 + (g == 1), y - r * 0.45, C(AMB[4] if g != 3 else AMB[3]))
    return cv.img()

# --- Gota de ámbar: glándula que se llena ---------------------------------------------------------
def gland_layer(body, im, facing, anim, i, full=False):
    off = (NL - im.height) // 2
    cells = cells_of(im, off)
    a = anchors(body, facing, cells)
    cv = Canvas(NL)
    glow = (0, 1, 1, 0)[i % 4] if full else 0
    pal = AMB if full else [AMB[0], AMB[0], AMB[1], AMB[2], AMB[3]]
    pts = a['front']
    if body == 'Frog':
        for (x, y) in pts:                                                   # saco de la garganta de ámbar
            ell5(cv, x, y, 3.4 + full * 0.6, 2.4 + full * 0.5, pal)
    else:
        for (x, y) in pts: ell5(cv, x, y + (1 if body == 'Diplocaulus' else 0), 1.8 + full * 0.5, 1.8 + full * 0.5, pal)
    if pts: cv.outline(); cv.clean()
    for (x, y) in pts:
        if body == 'Frog':
            if not full:                                                     # medio lleno: arriba translúcido
                for dx in range(-2, 3): cv.put_in(x + dx, y - 1, C(AMB[1], 255))
            cv.put(x - 1, y - 1 + (body == 'Diplocaulus'), C(AMB[4] if full else AMB[3]))
            if full and glow: cv.put(x + 1, y, C(SPARK[0]))
        else:
            cv.put(x - .5, y - .5 + (body == 'Diplocaulus'), C(AMB[4] if full else AMB[2]))
            if body == 'Diplocaulus' and (i % 2 == 0 or full):              # gotea
                cv.put_out(x, y + 3.2, C(AMB[2] if full else AMB[1]))
    return cv.img()

# --- efectos --------------------------------------------------------------------------------------
def cerci_flash(i):
    """Quiebro (32×32): dos arcos ámbar a los lados (el cuerpo se aparta sin moverse) y un destello. La silueta
    fantasma la hace el código con el sprite del cuerpo teñido de ámbar y transparente."""
    cv = Canvas(32)
    t = (i + 1) / 4
    for s in (-1, 1):
        for a in range(-50, 51, 6):
            r = 8 + t * 6
            x = 16 + s * math.cos(math.radians(a)) * r; y = 16 + math.sin(math.radians(a)) * r
            cv.put(x, y, C(AMB[4 - i] if i < 3 else AMB[1], 255 - i * 40))
    if i < 2: sparks(cv, 16, 16, 2 - i)
    return cv

def cerci_ready(i):
    """El quiebro vuelve a estar listo (16×16): chispa ámbar."""
    cv = Canvas(16)
    r = 1 + i * 1.5
    for a in range(4):
        ang = a * math.pi / 2 + math.pi / 4 * (i % 2)
        for j in range(int(r) + 1): cv.put(8 + math.cos(ang) * j, 8 + math.sin(ang) * j, C(AMB[4] if j < 2 else AMB[3]))
    if i < 3: cv.put(8, 8, C(SPARK[0]))
    return cv

def amber_block(size, grow=1.0, crack=0.0, glint=0):
    """Bloque de resina (size × size): esquinas redondeadas, interior claro y transparente (lo de dentro se ve),
    borde iluminado arriba a la izquierda, dos reflejos en diagonal y burbujas atrapadas. grow 0..1 sube desde el
    suelo; crack 0..1 lo agrieta (a punto de romperse)."""
    cv = Canvas(size)
    m, rc = 3, 4
    x0, x1, y1 = m, size - m - 1, size - m - 1
    y0 = int(y1 - (y1 - m) * grow)
    def inside(x, y):
        if not (x0 <= x <= x1 and y0 <= y <= y1): return False
        for cx_, cy_ in ((x0 + rc, y0 + rc), (x1 - rc, y0 + rc), (x0 + rc, y1 - rc), (x1 - rc, y1 - rc)):
            if (x < x0 + rc and cx_ == x0 + rc or x > x1 - rc and cx_ == x1 - rc) and                (y < y0 + rc and cy_ == y0 + rc or y > y1 - rc and cy_ == y1 - rc):
                return (x - cx_) ** 2 + (y - cy_) ** 2 <= rc * rc
        return True
    for y in range(size):
        for x in range(size):
            if not inside(x, y): continue
            edge = not all(inside(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            if edge:
                lit = x - x0 < (y - y0) * 0.0 + 2 or y - y0 < 2
                cv.put(x, y, C(AMB[4] if lit and (x + y) < x0 + y0 + size * 0.9 else AMB[1] if x > x1 - 1 or y > y1 - 1 else AMB[2]))
                continue
            t = (y - y0) / max(1, y1 - y0)
            col, al = (AMB[3], 95) if t < 0.5 else (AMB[2], 120)
            d = (x - x0) - (y - y0)
            if grow >= 1 and (abs(d - 4 - glint) < 1 or abs(d - 9 - glint) < 0.6) and t < 0.7: col, al = AMB[4], 190
            cv.put(x, y, C(col, al))
    if grow >= 1:
        for (bx, by) in ((x1 - 6, y1 - 8), (x1 - 10, y1 - 4), (x0 + 7, y1 - 6)):
            cv.put(bx, by, C(AMB[4], 220)); cv.put(bx + 1, by, C(AMB[3], 200))
    cv.outline(C(AMB[0]), min_alpha=60)
    if crack > 0:
        import random
        rnd = random.Random(5)
        for k in range(int(3 + crack * 5)):
            x, y = rnd.randint(x0 + 3, x1 - 3), rnd.randint(y0 + 3, y1 - 3)
            for j in range(int(2 + crack * 4)):
                x += rnd.choice((-1, 0, 1)); y += rnd.choice((-1, 1))
                if inside(x, y): cv.put(x, y, C(AMB[4]))
    return cv

def amber_shatter(i, size=64, n=6):
    """El bloque revienta: esquirlas de ámbar que salen (y dañan) y un anillo de empujón."""
    import random
    rnd = random.Random(11)
    cv = Canvas(size)
    c = size / 2
    t = (i + 1) / n
    for k in range(14):
        a = k * 2 * math.pi / 14 + rnd.uniform(-0.2, 0.2)
        d = 6 + t * (size * 0.42) * rnd.uniform(0.7, 1.0)
        x, y = c + math.cos(a) * d, c + math.sin(a) * d * 0.85
        L = 2 if k % 3 else 3
        for j in range(L): cv.put(x + math.cos(a) * j, y + math.sin(a) * j, C(AMB[4 - j] if i < 4 else AMB[1]))
    for a in range(0, 360, 4):
        r = 8 + t * (size * 0.4)
        if i < 4: cv.put(c + math.cos(math.radians(a)) * r, c + math.sin(math.radians(a)) * r * 0.85, C(AMB[3], 220 - i * 50))
    return cv

def amber_drop(i):
    """Gota de ámbar lanzada (10×10, bucle de 2): tiembla y brilla."""
    cv = Canvas(10)
    ell5(cv, 5, 5, 2.8 + (i % 2) * 0.3, 2.8 - (i % 2) * 0.3, AMB)
    cv.outline(C(AMB[0])); cv.put(4, 3, C(AMB[4]))
    return cv

# --- iconos ---------------------------------------------------------------------------------------
def icon_cerci(n):
    cv = Canvas(n); k = n / 32
    for s, ang in ((0, math.pi * 1.25), (1, math.pi * 1.45)):
        cercus(cv, 24 * k, 24 * k + s * 2 * k, ang, 20 * k, s)
    for j in range(int(3 * k) + 1): cv.put(24 * k + j * 0.7, 26 * k + j, C(CARAMEL[2]))
    return cv

def icon_capsule(n):
    cv = Canvas(n); k = n / 32
    bead(cv, 16 * k, 16 * k, 10 * k); cv.outline(); cv.clean()
    if n >= 32:
        for (x, y, c_) in ((15, 15, 0), (16, 14, 0), (14, 14, 1), (18, 14, 1), (17, 16, 0), (13, 16, 1)):
            cv.put(x, y, C(AMB[c_]))                                         # mosquito
    cv.put(11 * k, 10 * k, C(AMB[4])); cv.put(12 * k, 10 * k, C(SPARK[0]))
    return cv

def icon_drop(n):
    cv = Canvas(n); k = n / 32
    fill5(cv, lambda x, y: ((x - 16 * k) / (8 * k)) ** 2 + ((y - 20 * k) / (8 * k)) ** 2 <= 1 or
          (abs(x - 16 * k) < (y - 4 * k) * 0.5 and 4 * k < y < 20 * k), 16 * k, 18 * k, 9 * k, 10 * k, AMB)
    cv.outline(); cv.clean()
    cv.put(12 * k, 16 * k, C(AMB[4])); cv.put(12 * k, 17 * k, C(SPARK[0]))
    return cv

def specials():
    B = BODIES
    comp_all = {}
    for body, anims in SPEC_ANIMS.items():
        for anim in anims:
            for f in FACINGS:
                frames = frames_of(os.path.join(B, body, f'Player_{body}_{anim}_{f}.png'))
                L = {'Cerci': [cerci_layer(body, fr, f, anim, i) for i, fr in enumerate(frames)],
                     'AmberCapsule': [capsule_layer(body, fr, f, anim, i) for i, fr in enumerate(frames)],
                     'AmberGland_Rest': [gland_layer(body, fr, f, anim, i) for i, fr in enumerate(frames)],
                     'AmberGland_Full': [gland_layer(body, fr, f, anim, i, full=True) for i, fr in enumerate(frames)]}
                for mut, lay in L.items():
                    save(os.path.join(OUT, 'Special', f'Special_{mut}_{body}_{anim}_{f}.png'), lay)
                if anim in ('Idle', 'Move') or (anim.startswith('Attack') and f == 'side'):
                    for label, keys in (('Cerci', ['Cerci']), ('AmberCapsule', ['AmberCapsule']),
                                        ('AmberGland', ['AmberGland_Full']),
                                        ('las tres', ['Cerci', 'AmberCapsule', 'AmberGland_Rest'])):
                        rows = []
                        for j, fr in enumerate(frames):
                            c = Image.new('RGBA', (NL, NL), (0, 0, 0, 0))
                            o = (NL - fr.height) // 2
                            if 'Cerci' in keys and f != 'up': c.alpha_composite(L['Cerci'][j])
                            c.alpha_composite(fr, (o, o))
                            for k_ in keys:
                                if k_ != 'Cerci' or f == 'up': c.alpha_composite(L[k_][j])
                            rows.append(c)
                        comp_all[f'{label} | {body} {anim} {f}'] = rows
    for label in ('Cerci', 'AmberCapsule', 'AmberGland', 'las tres'):
        sel = {k: v for k, v in comp_all.items() if k.startswith(label + ' |') and ('Idle' in k or 'Move_side' in k.replace(' ', '_'))}
        preview(os.path.join(OUT, f'vista_previa_especial_{label.replace(" ", "_")}.png'), sel, BG, scale=2)
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    for body in SPEC_ANIMS:
        gif(os.path.join(OUT, 'gif', f'Especiales_las_tres_{body}_Move_side.gif'), comp_all[f'las tres | {body} Move side'], 110, BG, scale=4)
    fx = {'Fx_Cerci_Flash': [cerci_flash(i) for i in range(4)],
          'Fx_Cerci_Ready': [cerci_ready(i) for i in range(4)],
          'Fx_AmberBlock_Form': [amber_block(48, grow=g) for g in (0.25, 0.5, 0.75, 1.0)],
          'Fx_AmberBlock_Hold': [amber_block(48, glint=g) for g in (0, 2)],
          'Fx_AmberBlock_Crack': [amber_block(48, crack=c) for c in (0.3, 0.7)],
          'Fx_AmberBlock_Shatter': [amber_shatter(i) for i in range(6)],
          'Proj_AmberDrop': [amber_drop(i) for i in range(2)]}
    for name, frames in fx.items(): save(os.path.join(OUT, 'Effects', name + '.png'), frames)
    preview(os.path.join(OUT, 'vista_previa_efectos_4d.png'), fx, BG, scale=3)
    icons = {}
    for nm, fn in (('Cerci', icon_cerci), ('AmberCapsule', icon_capsule), ('AmberDrop', icon_drop)):
        for n in (32, 16):
            save(os.path.join(OUT, 'Icons', f'Icon_{nm}_{n}.png'), [fn(n)]); icons[f'Icon_{nm}_{n}'] = [fn(n)]
    preview(os.path.join(OUT, 'vista_previa_iconos_4d.png'), icons, BG, scale=6)

# =================================================================================================
# Salida
# =================================================================================================
def save(path, frames):
    os.makedirs(os.path.dirname(path), exist_ok=True); save_sheet(path, frames)

def layer_sheets(mut, folder, fn):
    rows = {}
    for body, anims in BODY_ANIMS.items():
        for anim in anims:
            for f in FACINGS:
                frames = frames_of(os.path.join(BODIES, body, f'Player_{body}_{anim}_{f}.png'))
                layer = [fn(fr, f, anim, i) for i, fr in enumerate(frames)]
                save(os.path.join(OUT, folder, f'{folder}_{mut}_{body}_{anim}_{f}.png'), layer)
                # composición para revisar: capa + cuerpo encima (las Extremidades van detrás)
                comp = []
                for fr, ly in zip(frames, layer):
                    c = Image.new('RGBA', (NL, NL), (0, 0, 0, 0)); c.alpha_composite(ly)
                    o = (NL - fr.height) // 2; c.alpha_composite(fr, (o, o)); comp.append(c)
                if anim in ('Idle', 'Move') or anim.startswith('Attack'):
                    rows[f'{body} {anim} {f}'] = comp
    preview(os.path.join(OUT, f'vista_previa_capas_{mut}.png'), rows, BG, scale=2)
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    for body in BODY_ANIMS:
        gif(os.path.join(OUT, 'gif', f'{mut}_{body}_Move_side.gif'), rows[f'{body} Move side'], 110, BG, scale=4)

def limbs():
    fx = {'Fx_JumpingLegs_Takeoff': [jump_takeoff(i) for i in range(5)],
          'Fx_JumpingLegs_Land': [jump_land(i) for i in range(5)],
          'Fx_JumpingLegs_Quake': [jump_quake(i) for i in range(5)],
          'Fx_Stunned': [stunned(i) for i in range(4)],
          'Fx_SilkThread_Start': [silk_start(i) for i in range(4)],
          'Fx_SilkThread': [silk_thread(i) for i in range(2)],
          'Fx_SilkThread_Anchor': [silk_anchor(i) for i in range(2)],
          'Fx_SilkThread_Hit': [silk_hit(i) for i in range(4)],
          'Fx_SilkWeb_Appear': [silk_web(appear=v) for v in (0.3, 0.6, 1.0)],
          'Fx_SilkWeb_Idle': [silk_web()],
          'Fx_SilkWeb_Fade': [silk_web(gone=g) for g in (0.3, 0.6, 0.85)]}
    for name, frames in fx.items(): save(os.path.join(OUT, 'Effects', name + '.png'), frames)
    preview(os.path.join(OUT, 'vista_previa_efectos_4a.png'), fx, BG, scale=4)
    icons = {}
    for nm, fn in (('JumpingLegs', icon_jump), ('SpinnerLegs', icon_spinner)):
        for n in (32, 16):
            save(os.path.join(OUT, 'Icons', f'Icon_{nm}_{n}.png'), [fn(n)]); icons[f'Icon_{nm}_{n}'] = [fn(n)]
    preview(os.path.join(OUT, 'vista_previa_iconos_4a.png'), icons, BG, scale=6)

if __name__ == '__main__':
    which = sys.argv[1:] or ['limbs']
    if 'limbs' in which: limbs()
    if 'skins' in which: skins()
    if 'jumpskins' in which: skins_jump()
    if 'organs' in which: organs()
    if 'specials' in which: specials()
    print('Listo:', OUT)
