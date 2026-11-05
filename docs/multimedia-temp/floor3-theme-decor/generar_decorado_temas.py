"""Decorado propio de las salas con tema del piso 3 (paso 6, bloque 1; ver README).

Solo decorado (no cambia el combate). Cada tema tiene 1 pieza protagonista y 1-2 de relleno, con variantes que
salen de parámetros, en los tres tramos del incendio (<Swamp|Smoke|Burnt>/). Lo que arde lleva _Idle / _Burn (4,
bucle) / _Burnt, como los helechos; en la tierra quemada solo existe ya quemado (Idle = Burnt).
Pivote: el pie, en el centro de abajo (los sprites llevan margen y sombra en el suelo), como floor3-environment.
Hecho con la skill primordia-sprites (pixel.py): 16 px por unidad, luz arriba a la izquierda, contorno OUTL.

Temas hechos:
- Nido de bombarderos: Nest_{Big,Small,Long} (montículo de tierra y hojarasca con galerías, élitros sueltos y la
  boca chamuscada; arde la hojarasca), LeafPile_{A,B,C} (pilas de hojarasca; arden), Deco_BeetleScorch (4 manchas
  de chorro quemado en el suelo, variantes).

Uso: python generar_decorado_temas.py
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
TEMP = os.path.normpath(os.path.join(HERE, '..'))
sys.path.insert(0, os.path.join(TEMP, 'floor3-environment'))
from base_piso3 import *
from PIL import Image

SHADOW = (24, 20, 37, 90)                                  # sombra en el suelo (la de floor3-environment)
EARTH = {'Swamp': ['#22180f', '#3a2a1c', '#55402a', '#735a3a', '#94784e'],
         'Smoke': ['#221c16', '#38302a', '#524638', '#6e604a', '#8e7e60'],
         'Burnt': ['#141114', '#221c1e', '#352b2a', '#4c3e38', '#6a5848']}
LITTER = {'Swamp': ['#2e2a14', '#4e4a22', '#6e6a2e', '#94883e', '#b8a85a'],   # hojarasca: oliva y pardo
          'Smoke': ['#3a2e16', '#5e4a20', '#8a6e2a', '#b8963a', '#e0c870'],   # reseca (FERN_DRY)
          'Burnt': CHAR}
ASH = ['#4a4346', '#6d6466', '#958a88']
NAVY = ramp('#2a3a6a', 4)
SCORCH_RIM = C('#7a4420')

def pal(seq): return [C(c) if isinstance(c, str) else c for c in seq]

def ground_shadow(cv, cx, cy, rx, ry):
    for y in range(int(cy - ry), int(cy + ry) + 1):
        for x in range(int(cx - rx), int(cx + rx) + 1):
            if ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 <= 1: cv.put_out(x, y, SHADOW)

def char_blend(c, k):
    """Oscurece hacia el carbón (k 0..1)."""
    tgt = C(CHAR[1])
    return tuple(int(c[i] + (tgt[i] - c[i]) * k) for i in range(3)) + (255,)

# =================================================================================================
# Nido de bombarderos
# =================================================================================================
NEST = {            # lienzo, radio x, alto, bultos (dx, dy, rx, ry relativos), galerías (dx, dy, tamaño), élitros
    'Big':   ((48, 34), 18, 10, ((-8, 1, 9, 6.5), (3, -2, 10, 8), (11, 2, 7, 5), (-1, 4, 15, 5)),
              ((-5, 3, 1.25), (8, 0, .8), (1, -5, .6)), 2),
    'Small': ((36, 26), 12, 7, ((-3, 0, 8, 6), (5, 2, 6, 4.5), (0, 3, 11, 4)), ((-3, 2.5, 1.1), (6, 1, .7)), 1),
    'Long':  ((58, 30), 24, 8, ((-15, 1, 9, 5.5), (-4, -1, 10, 7), (8, 0, 9, 6), (17, 2, 6, 4), (0, 3, 22, 4.5)),
              ((-11, 2.5, 1.0), (4, 2.5, 1.2), (14, 1, .7), (-3, -4, .6)), 2),
}

def leaf(cv, x, y, a, tones, rnd, inside=None):
    """Hoja seca de 2-3 px en dirección a (radianes): punta clara, base oscura."""
    L = rnd.choice((2, 3))
    for k in range(L):
        px, py = round(x + math.cos(a) * k), round(y + math.sin(a) * k * .6)
        if inside and not inside(px, py): continue
        cv.put(px, py, tones[min(len(tones) - 1, 1 + k)] if rnd.random() < .8 else tones[0])

def hole(cv, x, y, earth, size=1.0, scorched=False):
    """Boca de galería: hueco oscuro, labio de abajo con luz, borde de arriba en sombra. Si está chamuscada, el
    hollín sale en abanico hacia arriba (por ahí salen los chorros)."""
    rx, ry = 2.6 * size, 1.8 * size
    if scorched:
        for yy in range(int(y - ry - 7), int(y)):
            for xx in range(int(x - rx - 3), int(x + rx + 4)):
                up = y - yy; spread = rx + up * .45
                if abs(xx + .5 - x) < spread and (xx * 7 + yy * 3) % 5 < (4 if up < 4 else 2) and (xx, yy) in cv.px:
                    cv.put(xx, yy, C(CHAR[1]) if up < 4 else C(CHAR[2]))
    for yy in range(int(y - ry) - 2, int(y + ry) + 3):
        for xx in range(int(x - rx) - 2, int(x + rx) + 3):
            d = math.hypot((xx + .5 - x) / rx, (yy + .5 - y) / ry)
            if d <= 1: cv.put(xx, yy, PASSAGE if yy >= y - ry * .3 else OUTL)
            elif d <= 1.4 and (xx, yy) in cv.px:
                if scorched: cv.put(xx, yy, SCORCH_RIM if yy > y else C(CHAR[1]))
                elif yy > y: cv.put(xx, yy, earth[3])                # labio de abajo, con luz
                else: cv.put(xx, yy, earth[1])

def nest(t, kind, seed, fire=-1, burnt=False):
    """Montículo bajo de tierra hecho de bultos, con el pie ancho, una capa de hojarasca arriba, bocas de galería
    de distinto tamaño (la mayor, chamuscada), élitros azules sueltos y terrones alrededor.
    fire >= 0: la hojarasca arde (cada valor, otras llamas). burnt: tierra ennegrecida, ceniza y ascuas."""
    (W, H), rx, hgt, lumps, holes, ely = NEST[kind]
    key = t['key']; burnt = burnt or t['burnt']
    earth = pal(EARTH[key]); lit = pal(LITTER[key])
    if burnt: earth = [char_blend(c, .55) for c in earth]
    cv = Canvas(W, H); n = noise(seed)
    cx, cy = W / 2, H - 4 - hgt * .55
    def inside(x, y):
        for (dx, dy, lx, ly) in lumps:
            if ((x + .5 - cx - dx) / lx) ** 2 + ((y + .5 - cy - dy) / ly) ** 2 <= 1 + (n(x, y, 3.0) - .5) * .5: return True
        return False
    fill(cv, inside, cx - 1, cy - 1, rx, hgt, earth)              # sombreado de cúpula sobre todo el montículo
    lr = random.Random(seed * 7 + 1)                              # hojarasca arriba de cada bulto
    for _ in range(int(rx * hgt * 1.1)):
        x = cx + lr.uniform(-rx, rx); y = cy + lr.uniform(-hgt, hgt * .3)
        if not inside(x, y) or not inside(x, y - 1): continue
        light = y < cy - hgt * .3 and x < cx + rx * .3
        tones = ([C(c) for c in ASH] if burnt else lit[1:] if light else lit[:4])
        if burnt and lr.random() < .5: tones = [C(CHAR[1]), C(CHAR[2]), C(CHAR[3])]
        leaf(cv, x, y, lr.uniform(0, 6.28), tones, lr, inside)
    for i, (dx, dy, sz) in enumerate(holes):                      # galerías (la primera, chamuscada)
        hole(cv, cx + dx, cy + dy, earth, sz, scorched=(i == 0) or burnt)
    if not burnt:                                                 # élitros sueltos (azul marino con brillo)
        er = random.Random(seed + 3)
        for k in range(ely):
            for _ in range(20):
                ex = int(cx + er.uniform(-rx * .6, rx * .6)); ey = int(cy + er.uniform(-hgt * .6, 0))
                if inside(ex, ey) and inside(ex + 1, ey + 1): break
            for (ddx, ddy, c) in ((0, 0, NAVY[2]), (1, 0, NAVY[1]), (0, 1, NAVY[1]), (1, 1, NAVY[0]), (0, -1, NAVY[3])):
                cv.put(ex + ddx, ey + ddy, c)
    cv.outline(); cv.clean()
    cr = random.Random(seed + 5)                                  # terrones al pie (sin contorno)
    for _ in range(5):
        a = cr.uniform(.1, math.pi - .1); x = int(cx + math.cos(a) * (rx + 2)); y = int(H - 4 + cr.uniform(-1, 1))
        cv.put_out(x, y, earth[2]); cv.put_out(x + 1, y, earth[1]); cv.put_out(x, y + 1, earth[0])
    if burnt:                                                     # ascuas que quedan
        br = random.Random(seed + 9)
        for _ in range(3):
            for _ in range(20):
                x, y = int(cx + br.uniform(-rx * .6, rx * .6)), int(cy + br.uniform(-hgt * .6, 0))
                if inside(x, y): break
            cv.put(x, y, C(EMBER[2])); cv.put(x + 1, y, C(EMBER[1]))
    if fire >= 0:                                                 # llamas sobre la hojarasca
        fr = random.Random(seed * 31 + fire)
        for _ in range(int(rx * .6)):
            for _ in range(20):
                x = int(cx + fr.uniform(-rx * .8, rx * .8)); y = int(cy + fr.uniform(-hgt * .8, 0))
                if inside(x, y) and not inside(x, y - 2): break
            flame(cv, x, y, fr, h=fr.randrange(3, 8))
        for _ in range(3):
            cv.put(int(cx + fr.uniform(-rx * .6, rx * .6)), int(cy - hgt - fr.randrange(2, 5)), C(FIRE[3]))
    ground_shadow(cv, cx, H - 3.5, rx + 2, 2)
    return cv

def nest_burn(t, kind, seed):
    """Ardiendo: la hojarasca se va oscureciendo mientras arde (4 cuadros en bucle, con el oscurecido a medias)."""
    out = []
    for f in range(4):
        cv = nest(t, kind, seed, fire=f, burnt=False)
        out.append(cv)
    return out

# --- pilas de hojarasca -------------------------------------------------------------------------
PILES = {'A': ((30, 22), ((-3, 0, 5, 3.2), (3, 1, 5, 3), (0, -1, 4, 2.6))),
         'B': ((24, 20), ((-1, 0, 4.5, 3), (3, 1, 3.5, 2.4))),
         'C': ((40, 22), ((-7, 1, 5, 2.8), (-1, -1, 5.5, 3.4), (6, 1, 5, 3), (10, 2, 3, 2)))}

def leaf_pile(t, kind, seed, fire=-1, burnt=False):
    """Pila baja de hojas secas amontonadas, hecha de 2-4 bultos, con alguna hoja suelta en el suelo."""
    (W, H), lumps = PILES[kind]
    key = t['key']; burnt = burnt or t['burnt']
    lit = pal(LITTER[key]); cv = Canvas(W, H); rnd = random.Random(seed); n = noise(seed)
    cx, cy = W / 2, H - 6
    def inside(x, y):
        return any(((x + .5 - cx - dx) / lx) ** 2 + ((y + .5 - cy - dy) / ly) ** 2 <= 1 + (n(x, y, 2.5) - .5) * .6
                   for (dx, dy, lx, ly) in lumps)
    rx = max(abs(dx) + lx for dx, _, lx, _ in lumps)
    base = [C(CHAR[0]), C(CHAR[1]), C(CHAR[2]), C(CHAR[3])] if burnt else lit[:4]
    fill(cv, inside, cx - 1, cy - 1, rx, 4, base)
    rust = ramp('#8a4a2a', 4)                                      # alguna hoja rojiza
    for _ in range(int(rx * 9)):
        x = cx + rnd.uniform(-rx, rx); y = cy + rnd.uniform(-4, 3)
        if not inside(x, y): continue
        light = y < cy - 1 and x < cx + 2
        tones = ([C(c) for c in ASH] if burnt and rnd.random() < .5 else [C(CHAR[1]), C(CHAR[2]), C(CHAR[3])] if burnt
                 else rust if rnd.random() < .12 else lit[1:] if light else lit[:4])
        leaf(cv, x, y, rnd.uniform(0, 6.28), tones, rnd, inside)
    cv.outline(); cv.clean()
    if not burnt:                                                  # hojas sueltas en el suelo (sin contorno)
        for k in range(2):
            x = int(cx + (rx + 2) * (1 if k else -1)); y = H - 4 - k
            cv.put_out(x, y, lit[1]); cv.put_out(x + 1, y, lit[2]); cv.put_out(x + (2 if k else -1), y - 1, lit[1])
    if burnt: cv.put(int(cx), int(cy - 1), C(EMBER[2]))
    if fire >= 0:
        fr = random.Random(seed * 17 + fire)
        for _ in range(int(rx * .6) + 1):
            for _ in range(20):
                x = int(cx + fr.uniform(-rx, rx)); y = int(cy + fr.uniform(-3, 2))
                if inside(x, y): break
            flame(cv, x, y, fr, h=fr.randrange(3, 7))
        cv.put(int(cx + fr.uniform(-rx * .5, rx * .5)), int(cy - 6 - fr.randrange(1, 4)), C(FIRE[3]))
    ground_shadow(cv, cx, H - 3.5, rx + 1, 1.6)
    return cv

# --- manchas de chorro quemado (decal del suelo, sin contorno) -----------------------------------
def scorch(t, seed):
    """Salpicadura del chorro caliente en el suelo: núcleo chamuscado alargado en la dirección del chorro, gotas
    que salen en abanico y un borde pardo anaranjado apagado. Va pegada al suelo: sin contorno ni sombra."""
    W, H = 34, 22
    cv = Canvas(W, H); rnd = random.Random(seed)
    ang = rnd.uniform(-.5, .5) + (math.pi if rnd.random() < .5 else 0)
    n = noise(seed + 2)
    ca, sa = math.cos(ang), math.sin(ang)
    cx, cy = W / 2, H / 2
    core = [C(CHAR[1]), C(CHAR[2]), C(CHAR[3])]
    if t['burnt']: core = [C(CHAR[0]), C(CHAR[1]), C(CHAR[2])]
    for y in range(H):
        for x in range(W):
            dx, dy = x + .5 - cx, (y + .5 - cy) * SQ
            u = dx * ca + dy * sa; v = -dx * sa + dy * ca            # u a lo largo del chorro
            wdt = 4.2 + max(0, u) * .5                               # se abre en abanico
            d = math.hypot(u / 12.5, v / wdt) + (n(x, y, 4.0) - .5) * .35
            if d < .55: cv.put(x, y, core[0] if d < .3 else core[1])
            elif d < .8: cv.put(x, y, core[1] if (x + y) % 2 else core[2])
            elif d < .95: cv.put(x, y, SCORCH_RIM if (x + y) % 2 else core[2])
            elif d < 1.1 and (x * 3 + y) % 3 == 0: cv.put(x, y, SCORCH_RIM)
    for _ in range(7):                                                # gotas en abanico delante
        a = ang + rnd.uniform(-.5, .5); r = rnd.uniform(11, 14.5)
        x, y = int(cx + math.cos(a) * r), int(cy + math.sin(a) * r / SQ)
        cv.put(x, y, core[1]);
        if rnd.random() < .5: cv.put(x + 1, y, SCORCH_RIM)
    return cv

# =================================================================================================
# Telar de la araña
# =================================================================================================
# Seda del decorado: mate, algo amarillenta y con volumen, siempre pegada a objetos (nunca una tela plana en el
# suelo: esas son las telarañas que frenan, blancas y brillantes).
SILK = {'Swamp': ['#44402e', '#726a50', '#a49a74', '#cec49a', '#ece4c0'],      # crema: no se confunde con las rocas
        'Smoke': ['#423a2c', '#6e624a', '#9c8e6c', '#c4b48e', '#e0d2ae']}
import generar_escenario_piso3 as ESC

def silk_wrap(cv, seed, pal_, cover=.72, ang=1.25, box=None):
    """Envuelve en seda lo ya dibujado (no el contorno ni la sombra): una capa casi continua con el borde
    deshilachado, luz suave arriba a la izquierda y las vueltas marcadas (hebras claras y surcos que cruzan el
    objeto). Devuelve la máscara de la seda."""
    n = noise(seed); n2 = noise(seed + 11)
    pts = [p for p, c in cv.px.items() if c[3] == 255 and c != OUTL]
    xs = [x for x, _ in pts]; ys = [y for _, y in pts]
    x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
    ca, sa = math.cos(ang), math.sin(ang)
    mask = {(x, y) for (x, y) in pts if (not box or box[0] <= x < box[2] and box[1] <= y < box[3])
            and n2(x, y, 9.0) < cover}
    for (x, y) in mask:
        edge = any((x + dx, y + dy) not in mask for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
        u = (x - x0) / max(1, x1 - x0); v = (y - y0) / max(1, y1 - y0)
        k = 3 if u * .6 + v < .45 else 2 if u * .6 + v < 1.0 else 1
        w = (x * ca + y * sa) + (n(x, y, 6.0) - .5) * 3                     # coordenada de las vueltas
        ph = w % 4
        if ph < .9: k += 1                                                  # hebra con luz
        elif 2 <= ph < 2.8: k -= 1                                          # surco entre vueltas
        if edge: k -= 1
        cv.put(x, y, pal_[max(0, min(4, k))])
    return mask

def thread(cv, a, b, c):
    """Hilo tenso de 1 px (sin contorno) de a a b, detrás de lo dibujado."""
    (x0, y0), (x1, y1) = a, b
    n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
    for k in range(n + 1):
        t_ = k / max(1, n)
        cv.put_out(round(x0 + (x1 - x0) * t_), round(y0 + (y1 - y0) * t_), c)

def cocoon_on(cv, x, y, pal_, L=5, ang=1.3):
    """Capullo pequeño pegado (presa envuelta): elipse girada con las vueltas de seda marcadas."""
    rot_ellipse(cv, x, y, L * .5, 1.8, ang, pal_[1:])
    ca, sa = math.cos(ang), math.sin(ang)
    for k in (-1, 1):
        cv.put(round(x + ca * k * 1.2), round(y + sa * k * 1.2), pal_[1])

SILK_BASES = {      # pieza de floor3-environment, margen alrededor, número de hilos
    'Fallen': ('fallen_log', 10, 3),
    'Trunk':  ('broken_trunk', 10, 4),
    'Stump':  ('stump', 8, 2),
}

def anchors(mask, k, seed):
    """k puntos de la seda repartidos de izquierda a derecha, en su mitad de arriba."""
    pts = sorted(mask); xs = [x for x, _ in pts]; x0, x1 = min(xs), max(xs)
    out = []
    for i in range(k):
        tx = x0 + (x1 - x0) * (i + .5) / k
        col = [p for p in pts if abs(p[0] - tx) < 2]
        if col: out.append(min(col, key=lambda p: p[1] + random.Random(seed + i).randrange(0, 4)))
    return out

def silk_log(t, kind, seed, fire=-1, burnt=False):
    """Tronco caído, tronco partido o tocón (los de floor3-environment) envueltos en seda, con hilos tensos al
    suelo y un capullo pegado. fire >= 0: la seda arde. burnt: sin seda, madera chamuscada donde estaba y
    algún hilo negro."""
    fn, m, nth = SILK_BASES[kind]
    burnt = burnt or t['burnt']
    base = getattr(ESC, fn)(t, seed)
    cv = Canvas(base.w + 2 * m, base.h + m)
    paste(cv, base, m, m)
    gy = base.h + m - 3                                                   # el suelo, junto al pie
    cx_mid = cv.w / 2
    def ground_end(a, k=1.0):
        return (round(a[0] + (a[0] - cx_mid) * .35 * k + (4 if a[0] > cx_mid else -4)), gy + random.Random(a[0]).randrange(-1, 2))
    if t['burnt']:
        pts = [p for p, c in cv.px.items() if c[3] == 255 and c != OUTL]
        for a in anchors(set(pts), 2, seed): thread(cv, a, ground_end(a, .5), C(CHAR[2]))
        return cv
    pal_ = pal(SILK[t['key']])
    mask = silk_wrap(cv, seed, pal_)
    if burnt:
        for p in mask:
            c = cv.px.get(p)
            if c: cv.px[p] = char_blend(c, .6)
        rr = random.Random(seed)
        for a in anchors(mask, 1, seed): thread(cv, a, ground_end(a, .5), C(CHAR[1]))
        for _ in range(3):
            p = rr.choice(sorted(mask)); cv.put(p[0], p[1], C(EMBER[2]))
        return cv
    for a in anchors(mask, nth, seed): thread(cv, a, ground_end(a), pal_[3])
    mx = sorted(mask)
    cr = random.Random(seed + 2); cx_, cy_ = cr.choice(mx[len(mx) // 4: len(mx) * 3 // 4])
    cocoon_on(cv, cx_, cy_, pal_, L=cr.choice((5, 6)), ang=cr.uniform(.9, 1.8))
    if fire >= 0:
        fr = random.Random(seed * 13 + fire)
        for _ in range(max(4, len(mask) // 40)):
            x, y = fr.choice(mx)
            flame(cv, x, y, fr, h=fr.randrange(3, 7))
        for _ in range(3):
            x, y = fr.choice(mx); cv.put(x, y - fr.randrange(7, 11), C(FIRE[3]))
    return cv

COCOONS = {         # lienzo, largo, grosor, giro, qué asoma
    'Small':  ((20, 16), 9, 3.0, .25, 'wings'),
    'Medium': ((26, 18), 12, 3.8, -.2, 'legs'),
    'Large':  ((32, 22), 16, 5.0, .12, 'leg'),
}

def cocoon(t, kind, seed, fire=-1, burnt=False):
    """Presa envuelta en seda tirada en el suelo: huso con las puntas afiladas y las vueltas en espiral marcadas,
    y algo que asoma por un extremo (alas de efímera, patas de escarabajo)."""
    (W, H), L, th, ang, poke = COCOONS[kind]
    burnt = burnt or t['burnt']
    pal_ = [C(CHAR[0]), C(CHAR[1]), C(CHAR[2]), C(CHAR[3]), C(CHAR[4])] if burnt else pal(SILK[t['key']])
    cv = Canvas(W, H); cx, cy = W / 2, H - 4 - th * .7
    ca, sa = math.cos(ang), math.sin(ang)
    hl = L * .5
    for y in range(H):
        for x in range(W):
            dx, dy = x + .5 - cx, y + .5 - cy
            u = dx * ca + dy * sa; v = -dx * sa + dy * ca
            if abs(u) >= hl: continue
            half = th * (1 - abs(u / hl) ** 1.6) ** .9
            if abs(v) > half: continue
            k = 3 if v < -half * .3 else 2 if v < half * .45 else 1        # luz arriba (el lomo)
            if u < -hl * .4 and v < 0: k = min(4, k + 1)
            if (u * 1.0 + v * 1.4) % 3.6 < 1.1: k -= 2                       # vuelta en espiral
            cv.put(x, y, pal_[max(0, k)])
    cv.outline(); cv.clean()
    if not burnt:
        ex, ey = cx + ca * hl, cy + sa * hl                               # extremo por el que asoma la presa
        if poke == 'wings':
            for (dx, dy) in ((0, -1), (1, -1), (1, -2), (2, -2), (1, 0), (2, 0)): cv.put_out(round(ex + dx), round(ey + dy), (200, 236, 255, 150))
        else:
            for (dx, dy) in (((0, 1), (1, 2), (0, -1), (1, -2), (2, -2)) if poke == 'legs' else ((0, -1), (1, -2), (2, -2), (3, -3))):
                cv.put_out(round(ex + dx), round(ey + dy), NAVY[0])
    else:
        cv.put(round(cx), round(cy), C(EMBER[2]))
    if fire >= 0:
        fr = random.Random(seed * 7 + fire)
        for _ in range(2 + L // 4):
            flame(cv, round(cx + fr.uniform(-L * .35, L * .35)), round(cy + fr.uniform(-1, 1)), fr, h=fr.randrange(3, 6))
    ground_shadow(cv, cx, H - 3.5, hl + 1, 1.4)
    return cv

EGGSACS = {'A': ((18, 20), ((0, 0, 4.4),)), 'B': ((24, 20), ((-3, 0, 4.2), (3.6, 1, 3.4))),
           'C': ((30, 22), ((-5, 1, 3.8), (1, -1, 4.6), (7, 1.5, 3.2)))}

def egg_sac(t, kind, seed, fire=-1, burnt=False):
    """Sacos de huevos: gotas de seda algodonosas con el pico arriba retorcido en un mechón, los huevos marcados
    bajo la seda y pelusa translúcida alrededor; de 1 a 3."""
    (W, H), balls = EGGSACS[kind]
    burnt = burnt or t['burnt']
    pal_ = [C(CHAR[0]), C(CHAR[1]), C(CHAR[2]), C(CHAR[3]), C(CHAR[4])] if burnt else pal(SILK[t['key']])
    cv = Canvas(W, H); n = noise(seed); cx, cy = W / 2, H - 7
    for (dx, dy, r) in sorted(balls, key=lambda b: b[1]):
        bx, by = cx + dx, cy + dy
        def inside(x, y, bx=bx, by=by, r=r):
            ux, uy = x + .5 - bx, y + .5 - by
            if uy < 0: ux *= 1 + (-uy / r) * .9                            # gota: se estrecha hacia arriba
            return math.hypot(ux, uy * (1.05 if uy > 0 else .8)) <= r + (n(x, y, 1.6) - .5) * 1.0
        fill(cv, inside, bx, by, r, r, pal_[1:])
        if not burnt:
            for (ox, oy) in ((-2, 0), (0, 1), (2, 0), (-1, 2), (1, 2), (0, -1)):   # huevos bajo la seda
                cv.put_in(round(bx + ox * r / 4.4), round(by + oy * r / 4.4), pal_[2])
            tip = round(by - r * 1.25)                                     # mechón retorcido
            cv.put(round(bx), tip, pal_[3]); cv.put(round(bx) + 1, tip - 1, pal_[3]); cv.put(round(bx) + 1, tip - 2, pal_[2])
    cv.outline(); cv.clean()
    if not burnt:                                                          # pelusa de seda alrededor (translúcida)
        fz = random.Random(seed + 4)
        for (x, y) in [p for p, c in cv.px.items() if c == OUTL]:
            for dx, dy in ((0, -1), (-1, 0), (1, 0)):
                if (x + dx, y + dy) not in cv.px and fz.random() < .45:
                    cv.put(x + dx, y + dy, pal_[4][:3] + (110,))
    else:
        cv.put(round(cx), round(cy), C(EMBER[2]))
    if fire >= 0:
        fr = random.Random(seed * 5 + fire)
        for (dx, dy, r) in balls:
            flame(cv, round(cx + dx + fr.uniform(-r * .5, r * .5)), round(cy + dy), fr, h=fr.randrange(3, 6))
    ground_shadow(cv, cx, H - 3.5, max(abs(d) + r for d, _, r in balls) + 1, 1.3)
    return cv

# =================================================================================================
# Charca de sapos
# =================================================================================================
import generar_tilesets_piso3 as TS

def water_ramp(t):
    r = [C(c) for c in WATER]
    if t['burnt']: r = [blend(c, C('#4a4346'), 140) for c in r]
    elif t['key'] == 'Smoke': r = [blend(c, C('#4a4a36'), 60) for c in r]
    return r

def shade_water(cv, bank, floor_tile, t, W):
    """Como la orilla del lodo profundo, pero el agua es poco profunda: junto a la orilla se aclara (se ve el fondo)
    salvo bajo la orilla de arriba, que hace sombra; la orilla de abajo brilla."""
    for (x, y) in bank:
        c = floor_tile.px[(x, y)]
        wet = any((x + dx, y + dy) not in bank and 0 <= x + dx < 16 and 0 <= y + dy < 16 for dx, dy in N4)
        near = any((x + dx, y + dy) not in bank and 0 <= x + dx < 16 and 0 <= y + dy < 16 for dx in (-2, 0, 2) for dy in (-2, 0, 2))
        if wet and (x, y + 1) not in bank and y + 1 < 16: c = t['floor'][3]
        elif wet: c = darker(t['floor'][0], .8)
        elif near: c = darker(c, .85)
        cv.px[(x, y)] = c
    for y in range(16):
        for x in range(16):
            if (x, y) in bank: continue
            up, left = (x, y - 1) in bank, (x - 1, y) in bank
            down, right = (x, y + 1) in bank, (x + 1, y) in bank
            near = any((x + dx, y + dy) in bank for dx in (-2, -1, 0, 1, 2) for dy in (-2, -1, 0, 1, 2))
            if up or left: cv.px[(x, y)] = W[0]
            elif (x, y - 2) in bank and (x + y) % 2 == 0: cv.px[(x, y)] = W[1]
            elif down: cv.px[(x, y)] = W[4] if x % 3 else W[3]
            elif right: cv.px[(x, y)] = W[3]
            elif near: cv.px[(x, y)] = W[3] if (x + y) % 2 else W[2]       # poco fondo: se aclara

def water_tiles(t):
    """16 tiles por máscara (vecinos que NO son agua: N=1, E=2, S=4, O=8) y 4 esquinas de dentro, como Mud_*."""
    sd = TS.SEED[t['key']]; base = TS.floor_base(t, sd); W = water_ramp(t)
    surf = WrapCanvas(16, 16, True, True)
    n8, n4 = pnoise(sd + 60, 16, 8), pnoise(sd + 61, 16, 4)
    for y in range(16):
        for x in range(16):
            v = .55 + n8(x, y) * .55 + n4(x, y) * .5
            surf.px[(x, y)] = W[1 + min(1, max(0, dither(v, x, y)))]
    if t['burnt']:
        for (x, y) in ((3, 4), (11, 9), (7, 13), (13, 2)): surf.px[(x, y)] = C(ASH[1])
    tiles = []
    for m in range(16):
        cv = copy_canvas(surf, Canvas); shade_water(cv, TS.bank_set(m), base, t, W); tiles.append(cv)
    corners = []
    for name in ('NE', 'NO', 'SE', 'SO'):
        bank = TS.bank_set(0, name.replace('O', 'W'))
        full = copy_canvas(surf, Canvas); shade_water(full, bank, base, t, W)
        cv = Canvas(16, 16)
        near = {(x + dx, y + dy) for (x, y) in bank for dx in range(-2, 3) for dy in range(-2, 3)}
        for q in near:
            if q in full.px and full.px[q] != surf.px.get(q): cv.px[q] = full.px[q]
        for q in bank: cv.px[q] = full.px[q]
        corners.append(cv)
    return tiles, corners

def water_shimmer(t):
    """6 cuadros, capa encima de algunas celdas de agua: dos reflejos que se deslizan y una onda que se abre."""
    W = water_ramp(t); frames = []
    for f in range(6):
        cv = Canvas(16, 16)
        for (x0, y0, off) in ((3, 4, 0), (8, 11, 3)):
            ph = (f + off) % 6
            if ph < 4:
                for j in range(3 if ph in (1, 2) else 2):
                    cv.put(x0 + ph + j, y0, W[4] if j == 1 or ph in (1, 2) and j == 0 else W[3])
        ph = f % 6                                                       # onda (un sapo se acaba de zambullir)
        if 2 <= ph <= 4:
            r = ph - 1
            for a in range(16):
                if a % 2 == 0 or ph == 2:
                    cv.put(11 + math.cos(a * .39) * r * 1.4, 5 + math.sin(a * .39) * r * .8, W[3])
        frames.append(cv)
    return frames

REEDS = {'A': ((24, 26), 11, (8, 16)), 'B': ((18, 22), 7, (6, 13)), 'C': ((32, 24), 15, (5, 14))}

def reeds(t, kind, seed, sway=0.0, fire=-1, burnt=False):
    """Mata densa de plántulas de orilla: una macolla oscura en el pie de la que salen hojas de junco que se
    arquean hacia fuera y tallitos de equiseto rectos (alguno con el estróbilo pardo); se mecen. Partes finas sin
    contorno; la macolla, con contorno. Arden: quedan tocones negros."""
    (W, H), nst, (hmin, hmax) = REEDS[kind]
    burnt = burnt or t['burnt']
    g = [C(c) for c in (FERN if t['key'] == 'Swamp' else FERN_DRY)]
    tan = ramp('#c8b070', 4)
    cv = Canvas(W, H); rnd = random.Random(seed); gy = H - 5; cx = W / 2
    bw = W * .22
    ellipse(cv, cx, gy, bw, 2.6, [C(CHAR[0]), C(CHAR[1]), C(CHAR[2]), C(CHAR[3])] if burnt else g[:4])   # macolla
    cv.outline(); cv.clean()
    blades = []
    for k in range(nst):
        u = (k + .5) / nst * 2 - 1                                        # -1 izquierda .. 1 derecha
        x0 = cx + u * bw * .8 + rnd.uniform(-.6, .6)
        h = rnd.randrange(hmin, hmax + 1) * (1 - abs(u) * .35)
        kind_ = 'horsetail' if rnd.random() < .3 else 'blade'
        blades.append((x0, h, u, kind_, rnd.random() < .5, rnd.uniform(.6, 1.2)))
    blades.sort(key=lambda b: -b[1])
    for (x0, h, u, kind_, cone, curl) in blades:
        h = max(2, int(h // 3)) if burnt else int(h)
        for j in range(h):
            fr = j / max(1, h)
            bend = (u * curl * fr * fr * 4.5) if kind_ == 'blade' else u * fr * 1.2
            x = x0 + bend + sway * fr * fr * 2.4
            y = gy - 1 - j + (fr * fr * h * .25 * abs(u) if kind_ == 'blade' else 0)   # la punta cae un poco
            if burnt: c = C(CHAR[2]) if j < h - 1 else C(CHAR[3])
            elif kind_ == 'horsetail' and j % 3 == 2: c = g[1]
            else: c = g[4] if fr > .7 and u < .3 else g[3] if fr > .35 else g[2]
            cv.put_out(x, y, c) if j < 2 else cv.put(x, y, c)
            if not burnt and kind_ == 'horsetail' and j % 3 == 2 and 2 < j < h - 2:
                cv.put(x - 1, y - 1, g[2]); cv.put(x + 1, y - 1, g[2])
        if kind_ == 'horsetail' and cone and not burnt:
            tx = x0 + u * 1.2 + sway * 2.4
            for dy, c in ((0, tan[1]), (1, tan[2]), (2, tan[3])): cv.put(tx, gy - h - dy, c)
    if burnt: cv.put(cx, gy - 1, C(EMBER[2]))
    if fire >= 0:
        fr_ = random.Random(seed * 3 + fire)
        for _ in range(3 + nst // 4):
            flame(cv, cx + fr_.uniform(-bw, bw), gy - fr_.randrange(1, 6), fr_, h=fr_.randrange(4, 8))
    ground_shadow(cv, cx, gy + 2, bw + 2, 1.3)
    return cv

def reeds_set(out, t, kind, seed):
    out[f'ShoreReeds_{kind}_Idle'] = [reeds(t, kind, seed, sway=math.sin(f * math.pi / 2) * .8) for f in range(4)]
    if not t['burnt']:
        out[f'ShoreReeds_{kind}_Burn'] = [reeds(t, kind, seed, fire=f) for f in range(4)]
        out[f'ShoreReeds_{kind}_Burnt'] = [reeds(t, kind, seed, burnt=True)]

def toad_spawn(t, seed):
    """Puesta de huevos de sapo sobre el barro de la orilla: cordones gelatinosos de huevos claros, cada uno con
    el punto negro del embrión, y la gelatina con un borde más oscuro. Decal pegado al suelo."""
    cv = Canvas(34, 18); rnd = random.Random(seed)
    jelly = [C('#5a7a62'), C('#9cc0a0'), C('#d8ecd0')]
    if t['burnt']: jelly = [C(ASH[0]), C(ASH[1]), C(ASH[2])]
    strands = (1, 2, 3)[seed % 3]
    eggs = []
    for k in range(strands):
        x, y = rnd.uniform(4, 8), 4 + k * 4 + rnd.uniform(0, 2); a = rnd.uniform(0, 6)
        for j in range(rnd.randrange(6, 9)):
            eggs.append((round(x), round(y))); x += 2.6; y += math.sin(a + j * .9) * 1.1
    for (x, y) in eggs:                                                   # gelatina
        for dx in (-1, 0, 1, 2):
            for dy in (-1, 0, 1, 2):
                if abs(dx - .5) + abs(dy - .5) <= 2: cv.put(x + dx, y + dy, jelly[0])
    for (x, y) in eggs:
        for (dx, dy) in ((0, 0), (1, 0), (0, 1), (1, 1)): cv.put(x + dx, y + dy, jelly[1])
        cv.put(x, y, jelly[2]); cv.put(x + 1, y + 1, C('#141820'))       # brillo y embrión
    return cv

def wet_mud(t, seed):
    """Barro encharcado de la orilla: mancha de lodo mojado (más claro y pardo que el suelo, con brillos de agua)
    con huellas de sapo bien marcadas. Decal, sin contorno."""
    W_, H_ = 38, 22
    cv = Canvas(W_, H_); rnd = random.Random(seed)
    mask = {(x + 3, y + 3) for (x, y) in blob(seed, rnd.choice(('redondo', 'alargado', 'lobulado')), 32, 16)}
    sl = [C(c) for c in SLUDGE]
    if t['burnt']: sl = [blend(c, C('#4a4346'), 120) for c in sl]
    for (x, y) in mask:
        d = sum((x + dx, y + dy) not in mask for dx in (-1, 0, 1) for dy in (-1, 0, 1))
        cv.put(x, y, sl[0] if d > 2 else sl[1] if (x * 3 + y) % 7 else sl[2])
    pts = sorted(mask)
    for _ in range(len(pts) // 25):                                       # brillos de agua
        x, y = rnd.choice(pts)
        if (x + 1, y) in mask: cv.put(x, y, sl[3]); cv.put(x + 1, y, sl[2])
    x, y = 9, 13
    for k in range(4):                                                   # huellas: dos patas por paso
        for side in (-1, 1):
            hx, hy = x + k * 6, y - k * 2 + side * 2
            if (hx, hy) not in mask: continue
            for (dx, dy) in ((0, 0), (1, 0), (0, 1), (1, 1)): cv.put(hx + dx, hy + dy, OUTL[:3] + (200,))
            for (dx, dy) in ((3, -1), (3, 1), (2, 2), (2, -2)): cv.put(hx + dx, hy + dy, OUTL[:3] + (170,))
    return cv

def tadpole_pool(t, seed):
    """Charquito de una casilla con renacuajos que dan vueltas (6 cuadros; el agua, la de los charcos)."""
    frames = []
    for f in range(6):
        base = ESC.puddle(t, 'Water', 'redondo', 20, 12, seed, frame=f % 4)
        cv = Canvas(base.w, base.h); paste(cv, base, 0, 0)
        cx, cy = base.w / 2, base.h / 2
        for k in range(3):
            a = f / 6 * 2 * math.pi + k * 2.1; a2 = a + .5
            x, y = cx + math.cos(a) * 5.5, cy + math.sin(a) * 2.6
            tx, ty = cx + math.cos(a - .55) * 5.5, cy + math.sin(a - .55) * 2.6   # la cola, detrás
            cv.put(round(x), round(y), C('#141418')); cv.put(round(x) + 1, round(y), C('#26261e'))
            cv.put(round(tx), round(ty) + (f + k) % 2, C('#3a3a2e'))
        frames.append(cv)
    return frames

ROCKS = {'A': ((18, 16), ((9, 10, 6, 4.5),)), 'B': ((26, 20), ((13, 12, 9, 6), (7, 14, 4, 3))),
         'C': ((28, 18), ((9, 11, 6, 4.5), (19, 12, 6.5, 4.2), (14, 14, 3.5, 2.5)))}

def moss_rock(t, kind, seed):
    """Piedras bajas con una capa gruesa de musgo encima (donde se posan los sapos)."""
    (W, H), stones = ROCKS[kind]
    cv = Canvas(W, H); rnd = random.Random(seed)
    stone = [C(c) for c in ESC.STONE]
    for (x, y, rx, ry) in stones: ellipse(cv, x, y, rx, ry, stone)
    moss = ([C(c) for c in ('#2e4a26', '#3d5e36', '#557a3e', '#7fa04a')] if t['key'] == 'Swamp' else
            [C(c) for c in FERN_DRY[:4]] if t['key'] == 'Smoke' else [C(c) for c in [CHAR[2]] + ASH])
    for (x, y) in list(cv.px):
        d = 0
        while (x, y - d - 1) in cv.px: d += 1
        if d < 3 + (x * 7 + seed) % 2:
            cv.px[(x, y)] = moss[3] if d == 0 and x < W * .55 else moss[2] if d < 2 else moss[1]
    cv.outline(); cv.clean()
    ground_shadow(cv, W / 2, H - 3, W * .42, 1.6)
    return cv

def build_pond(t):
    out = {}
    tiles, corners = water_tiles(t)
    for m, cv in enumerate(tiles): out[f'Water_{m}'] = [cv]
    for name, cv in zip(('NE', 'NO', 'SE', 'SO'), corners): out[f'Water_Corner_{name}'] = [cv]
    out['Water_Shimmer'] = water_shimmer(t)
    for i, kind in enumerate(REEDS): reeds_set(out, t, kind, 50 + i * 7)
    out['Deco_ToadSpawn'] = [toad_spawn(t, 60 + k * 5) for k in range(3)]
    out['Deco_WetMud'] = [wet_mud(t, 70 + k * 5) for k in range(3)]
    out['TadpolePool'] = tadpole_pool(t, 80)
    for i, kind in enumerate(ROCKS): out[f'MossRock_{kind}'] = [moss_rock(t, kind, 90 + i * 3)]
    return out

def scene_pond(key, out, f=0):
    """Suelo del tramo con una gran charca hecha con los tiles, su orilla decorada y unos sapos."""
    t = tram(key)
    floor = [Image.open(os.path.join(TEMP, 'floor3-environment', key, 'tiles', f'Floor_{k}.png')).convert('RGBA') for k in range(6)]
    bg = Image.new('RGBA', (480, 270)); rnd = random.Random(3)
    for y in range(0, 272, 16):
        for x in range(0, 480, 16): bg.alpha_composite(floor[0 if rnd.random() < .66 else rnd.randint(1, 5)], (x, y))
    nz = noise(5)
    water = {(i, j) for i in range(30) for j in range(17)
             if ((i - 15.5) / 7.5) ** 2 + ((j - 8.5) / 4.2) ** 2 + (nz(i * 16, j * 16, 40) - .5) * .5 < 1}
    for (i, j) in sorted(water):
        m = sum(b for (dx, dy, b) in ((0, -1, 1), (1, 0, 2), (0, 1, 4), (-1, 0, 8)) if (i + dx, j + dy) not in water)
        bg.alpha_composite(out[f'Water_{m}'][0].img(), (i * 16, j * 16))
        for (dx, dy, name) in ((1, -1, 'NE'), (-1, -1, 'NO'), (1, 1, 'SE'), (-1, 1, 'SO')):
            if (i + dx, j) in water and (i, j + dy) in water and (i + dx, j + dy) not in water:
                bg.alpha_composite(out[f'Water_Corner_{name}'][0].img(), (i * 16, j * 16))
    inner = [c for c in sorted(water) if all((c[0] + dx, c[1] + dy) in water for dx in (-1, 0, 1) for dy in (-1, 0, 1))]
    for k, (i, j) in enumerate(inner[::5]):
        bg.alpha_composite(out['Water_Shimmer'][(f * 1 + k) % 6].img(), (i * 16, j * 16))
    def decal(name, k, x, y):
        im = out[name][k].img(); bg.alpha_composite(im, (x - im.width // 2, y - im.height // 2))
    decal('Deco_WetMud', 0, 128, 150); decal('Deco_WetMud', 1, 380, 210); decal('Deco_WetMud', 2, 300, 70)
    decal('Deco_ToadSpawn', 0, 120, 160); decal('Deco_ToadSpawn', 1, 372, 214); decal('Deco_ToadSpawn', 2, 200, 228)
    put = putter(bg, out)
    burning = key != 'Burnt'
    for k, (x, y, kind) in enumerate(((150, 92, 'A'), (176, 86, 'B'), (330, 78, 'C'), (392, 112, 'A'), (420, 150, 'B'),
                                      (110, 186, 'C'), (250, 236, 'A'), (360, 226, 'B'), (80, 140, 'B'))):
        name = f'ShoreReeds_{kind}_Burn' if burning and k == 3 else f'ShoreReeds_{kind}_Idle'
        put(name, x, y, (f + k) % 4)
    put('MossRock_B', 96, 120); put('MossRock_A', 300, 240); put('MossRock_C', 440, 196)
    tp = out['TadpolePool']; im = tp[f % 6].img(); bg.alpha_composite(im, (54, 210)); bg.alpha_composite(tp[(f + 3) % 6].img(), (420, 60))
    enemies(bg, 'Toad', 'Toad_Idle.png', ((150, 110, False), (330, 190, True), (410, 100, True)))
    return bg

# =================================================================================================
# Pradera de Calamites
# =================================================================================================
# Matas de Calamites jóvenes: tallos finos (2 px) y de un verde más apagado que la Calamites madre enemiga, sin
# cara ni estróbilos crema grandes; los verticilos de hojas plumosos son lo que se lee.
CAL = {'Swamp': ramp('#4e7a34', 5), 'Smoke': [C(c) for c in FERN_DRY]}
CONE = ramp('#8a6a3a', 4)
SPORE = [C('#d8d090'), C('#f0e8a8')]
CLUMPS = {'A': ((34, 46), 5, (20, 32)), 'B': ((22, 32), 3, (14, 24)), 'C': ((38, 38), 7, (12, 30))}

def calamites_clump(t, kind, seed, sway=0.0, fire=-1, burnt=False):
    """Cañaveral de Calamites jóvenes: macolla oscura en el pie (con contorno) y tallos articulados finos sin
    contorno (2 px: lado con luz y lado en sombra, nudo oscuro cada 4 px) con verticilos de hojas largas que se
    abren hacia arriba como plumeros; alguno con un estróbilo pardo en la punta. Se mecen desde el pie."""
    (W, H), nst, (hmin, hmax) = CLUMPS[kind]
    burnt = burnt or t['burnt']
    g = [C(c) for c in CHAR] if burnt else CAL[t['key']]
    cv = Canvas(W, H); rnd = random.Random(seed); gy = H - 5; cx = W / 2
    ellipse(cv, cx, gy, W * .2, 2.4, g[:4]); cv.outline(); cv.clean()   # macolla
    stems = []
    for k in range(nst):
        u = (k + .5) / nst * 2 - 1
        stems.append((cx + u * W * .2 + rnd.uniform(-1, 1), rnd.randrange(hmin, hmax + 1) * (1 - abs(u) * .25),
                      u * .18 + rnd.uniform(-.05, .05), rnd.random() < .45, rnd.uniform(0, 3)))
    stems.sort(key=lambda s_: -s_[1])
    def at(x0, lean, j, h):
        fr = j / max(1, h)
        return x0 + lean * j + sway * fr * fr * 3.0
    for (x0, h, lean, cone, ph) in stems:
        h = max(3, int(h // 4)) if burnt else int(h)
        for j in range(1, h):
            x = at(x0, lean, j, h); y = gy - j
            node = (j + int(ph)) % 4 == 0
            cv.put(x, y, g[0] if node else g[3]); cv.put(x + 1, y, g[0] if node else g[1])
        if burnt: continue
        for j in range(5, h - 1):                                        # verticilos: plumeros abiertos hacia arriba
            if (j + int(ph)) % 4: continue
            x = at(x0, lean, j, h); y = gy - j
            L = 3 + round(3 * (1 - j / h))
            for side in (-1, 1):
                for k in range(1, L + 1):
                    px_ = x + (side * k * .9 if side < 0 else 1 + k * .9); py_ = y - k * .75
                    cv.put_out(px_, py_, g[3] if side < 0 and k < L else g[2] if k < L else g[4])
        x = at(x0, lean, h, h)
        if cone:
            for dy, c in ((0, CONE[1]), (1, CONE[2]), (2, CONE[2]), (3, CONE[3])):
                cv.put(x, gy - h - dy, c); cv.put(x + 1, gy - h - dy, CONE[0] if dy < 3 else CONE[2])
        else:
            cv.put(x, gy - h, g[4]); cv.put(x + 1, gy - h, g[3])
    if burnt: cv.put(cx, gy - 1, C(EMBER[2]))
    if fire >= 0:
        fr_ = random.Random(seed * 3 + fire)
        for (x0, h, lean, cone, ph) in stems[:5]:
            flame(cv, at(x0, lean, int(h * .4), h), gy - fr_.randrange(int(h * .2), max(int(h * .2) + 1, int(h * .7))), fr_, h=fr_.randrange(4, 8))
    ground_shadow(cv, cx, gy + 2, W * .3, 1.6)
    return cv

def clump_set(out, t, kind, seed):
    out[f'CalamitesClump_{kind}_Idle'] = [calamites_clump(t, kind, seed, sway=math.sin(f * math.pi / 2) * .8) for f in range(4)]
    if not t['burnt']:
        out[f'CalamitesClump_{kind}_Burn'] = [calamites_clump(t, kind, seed, fire=f) for f in range(4)]
        out[f'CalamitesClump_{kind}_Burnt'] = [calamites_clump(t, kind, seed, burnt=True)]

def horsetail_grass(t, seed):
    """Mancha de hierba de equisetos: brotes finos de 2-5 px con la punta clara, más densos en el centro. Decal sin
    contorno (da textura de pradera al suelo)."""
    W_, H_ = 36, 22
    cv = Canvas(W_, H_); rnd = random.Random(seed)
    g = [C(c) for c in (CHAR if t['burnt'] else FERN if t['key'] == 'Swamp' else FERN_DRY)]
    mask = {(x + 2, y + 5) for (x, y) in blob(seed, rnd.choice(('redondo', 'alargado', 'salpicado')), 32, 16)}
    pts = sorted(mask)
    for _ in range(len(pts) // 7):
        x, y = rnd.choice(pts); h = rnd.randrange(2, 6)
        for j in range(h):                                               # tonos apagados: es escenario
            if 0 <= y - j: cv.put(x, y - j, g[2] if j == h - 1 else g[1] if j > 0 else g[0])
        if h > 3 and rnd.random() < .5: cv.put(x - 1, y - h + 2, g[1]); cv.put(x + 1, y - h + 2, g[1])
    return cv

def fallen_cones(t, seed):
    """Dos o tres estróbilos pardos caídos (conitos con escamas), tumbados en el suelo."""
    cv = Canvas(20, 12); rnd = random.Random(seed)
    cn = [C(c) for c in CHAR] if t['burnt'] else CONE
    for k in range(rnd.choice((2, 3))):
        x, y, a = 4 + k * 6 + rnd.uniform(-1, 1), 6 + rnd.uniform(-1.5, 1.5), rnd.uniform(-.6, .6) + (math.pi * .5 if rnd.random() < .3 else 0)
        rot_ellipse(cv, x, y, 2.8, 1.5, a, cn[:4] if len(cn) >= 4 else cn)
        ca, sa = math.cos(a), math.sin(a)
        for j in (-1, 1): cv.put_in(round(x + ca * j), round(y + sa * j), cn[0])     # escamas
    cv.outline(); cv.clean(); ground_shadow(cv, 10, 9, 8, 1.2)
    return cv

def spore_drift(t, seed):
    """Esporas amarillas que flotan sobre la pradera (8 cuadros en bucle, 24×24, translúcidas, sin contorno)."""
    rnd = random.Random(seed)
    motes = [(rnd.uniform(3, 21), rnd.uniform(12, 21), rnd.uniform(0, 1), rnd.random() < .6) for _ in range(10)]
    frames = []
    for f in range(8):
        cv = Canvas(24, 24)
        for (x, y, ph, big) in motes:
            tt = (f / 8 + ph) % 1
            px_ = x + math.sin(tt * 2 * math.pi) * 1.5; py_ = y - tt * 9
            a = min(255, int(320 * math.sin(tt * math.pi)))
            if a < 40: continue
            cv.put(px_, py_, SPORE[1][:3] + (a,))
            if big: cv.put(px_ + 1, py_, SPORE[0][:3] + (a // 2,)); cv.put(px_, py_ + 1, SPORE[0][:3] + (a // 2,))
        frames.append(cv)
    return frames

def build_meadow(t):
    out = {}
    for i, kind in enumerate(CLUMPS): clump_set(out, t, kind, 100 + i * 7)
    out['Deco_HorsetailGrass'] = [horsetail_grass(t, 110 + k * 3) for k in range(4)]
    out['Deco_FallenCones'] = [fallen_cones(t, 120 + k * 3) for k in range(3)]
    if not t['burnt']: out['Fx_SporeDrift'] = spore_drift(t, 130)
    return out

def floor_bg(key, seed=3):
    floor = [Image.open(os.path.join(TEMP, 'floor3-environment', key, 'tiles', f'Floor_{k}.png')).convert('RGBA') for k in range(6)]
    bg = Image.new('RGBA', (480, 270)); rnd = random.Random(seed)
    for y in range(0, 272, 16):
        for x in range(0, 480, 16): bg.alpha_composite(floor[0 if rnd.random() < .66 else rnd.randint(1, 5)], (x, y))
    return bg

def scene_meadow(key, out, f=0):
    """Claro abierto: cañaverales, hierba, conos y esporas, con Calamites madre, retoños y una Medullosa."""
    bg = floor_bg(key)
    def decal(name, k, x, y):
        im = out[name][k].img(); bg.alpha_composite(im, (x - im.width // 2, y - im.height // 2))
    for k, (x, y) in enumerate(((90, 70), (200, 120), (330, 60), (420, 150), (130, 200), (260, 220), (380, 240), (60, 140))):
        decal('Deco_HorsetailGrass', k % 4, x, y)
    for k, (x, y) in enumerate(((150, 96), (300, 170), (420, 200))): decal('Deco_FallenCones', k % 3, x, y)
    put = putter(bg, out); burning = key != 'Burnt'
    for k, (x, y, kind) in enumerate(((70, 110, 'A'), (110, 96, 'B'), (240, 80, 'C'), (400, 100, 'A'), (440, 120, 'B'),
                                      (90, 250, 'C'), (330, 252, 'A'), (190, 260, 'B'))):
        name = f'CalamitesClump_{kind}_Burn' if burning and k == 3 else f'CalamitesClump_{kind}_Idle'
        put(name, x, y, (f + k) % 4)
    if 'Fx_SporeDrift' in out:
        for k, (x, y) in enumerate(((120, 80), (260, 60), (350, 200))):
            bg.alpha_composite(out['Fx_SporeDrift'][(f * 2 + k * 3) % 8].img(), (x, y))
    enemies(bg, 'Calamites', 'Calamites_Idle.png', ((200, 130, False),), cw=32)
    enemies(bg, 'CalamitesSprout', 'CalamitesSprout_Idle.png', ((250, 170, False), (180, 180, True)))
    enemies(bg, 'Medullosa', 'Medullosa_Idle.png', ((340, 120, False),))
    return bg

# =================================================================================================
# Cielo de libélulas
# =================================================================================================
LIGHT = {'Swamp': (255, 240, 176), 'Smoke': (255, 222, 150), 'Burnt': (255, 180, 120)}   # cálida; en humo y ceniza, más naranja
SHAFTS = {'A': (52, 112, 15), 'B': (72, 96, 22)}                     # lienzo, ancho del rayo

def light_shaft(t, kind, f, seed=0):
    """Rayo de luz que cae del dosel en diagonal (de arriba a la izquierda hacia abajo a la derecha, como la luz del
    juego): translúcido, más denso en el centro, con vetas y motas que brillan dentro; 6 cuadros, la luz respira.
    Sin contorno (en Unity puede ir con mezcla aditiva)."""
    W, H, wd = SHAFTS[kind]
    col = LIGHT[t['key']]; cv = Canvas(W, H); n = noise(seed + 3)
    breath = .8 + .2 * math.sin(f / 6 * 2 * math.pi)
    for y in range(H):
        fy = y / H
        cx = wd * .7 + (W - wd * 1.4) * fy                            # se desplaza a la derecha al bajar
        w = wd * (.8 + .4 * fy)
        fade = min(1, fy * 4) * min(1, (1 - fy) * 3)                  # nace y se apaga
        for x in range(W):
            d = abs(x + .5 - cx) / w
            if d > 1: continue
            streak = .75 + .5 * n(x * 2 - y * .6, f * 1.5, 6)
            a = (1 - d * d) * fade * streak * breath * 105
            if a > 8: cv.put(x, y, col + (int(min(150, a)),))
    rnd = random.Random(seed)
    for k in range(6):                                                # motas que brillan
        fy = (rnd.random() + f / 6 * .25) % 1
        y = int(fy * H); cx = wd * .7 + (W - wd * 1.4) * fy + rnd.uniform(-wd * .5, wd * .5)
        a = int(200 * math.sin(fy * math.pi) * (.5 + .5 * math.sin(f + k)))
        if a > 40: cv.put(cx, y, (255, 250, 220, a))
    return cv

def light_pool(t, kind, f):
    """Pozo de luz en el suelo donde acaba el rayo: elipse cálida translúcida que respira con él (6 cuadros)."""
    W, H = (48, 18) if kind == 'A' else (64, 24)
    col = LIGHT[t['key']]; cv = Canvas(W, H)
    breath = .8 + .2 * math.sin(f / 6 * 2 * math.pi)
    for y in range(H):
        for x in range(W):
            d = math.hypot((x + .5 - W / 2) / (W / 2), (y + .5 - H / 2) / (H / 2))
            if d < 1:
                a = (1 - d) ** 1.1 * 110 * breath
                if a > 6: cv.put(x, y, col + (int(a),))
    return cv

PERCHES = {'A': ((28, 44), ((11, 34, .08),), ((0, 0),)), 'B': ((30, 40), ((12, 30, -.08), (16, 24, .14)), ((0, 0), (1, 0))),
           'C': ((26, 30), ((12, 20, .1),), ((0, 0),))}

def small_dragonfly(cv, x, y, f, k, side=1):
    """Libélula pequeña posada en la punta (vista de lado): abdomen turquesa fino y segmentado, tórax, cabeza con el
    ojo grande y dos alas translúcidas alzadas con su vena; de vez en cuando las sacude."""
    T = ramp('#2aa8a8', 4); wing = (214, 240, 244, 170); vein = (150, 200, 210, 200)
    flick = (f + k) % 4 == 2
    up = 1 if flick else 0
    for (dx0, L) in ((0, 5), (-2, 5)):                                   # alas: dos lóbulos alzados hacia atrás
        for j in range(L):
            for r in (1, 2):
                cv.put(x + side * (dx0 - j), y - r - up - (j > 2), wing)
            cv.put(x + side * (dx0 - j), y - 2 - up - (j > 2), vein) if j in (1, 3) else None
    for j in range(7): cv.put(x - side * (j + 1), y, T[2] if j % 2 else T[1])   # abdomen
    cv.put(x, y, T[2]); cv.put(x + side, y, T[2])                         # tórax
    for (dx, dy) in ((2, 0), (2, -1), (3, 0), (3, -1)): cv.put(x + side * dx, y + dy, T[3])   # cabeza
    cv.put(x + side * 3, y - 1, (240, 250, 250, 255))                     # brillo del ojo

def perch(t, kind, seed, f=0, fire=-1, burnt=False):
    """Caña seca (o dos) alta donde se posan 1-2 libélulas pequeñas; las cañas arden y las libélulas se van."""
    (W, H), canes, flies = PERCHES[kind]
    burnt = burnt or t['burnt']
    cn = [C(c) for c in (CHAR if burnt else FERN_DRY)]
    cv = Canvas(W, H); gy = H - 4
    tips = []
    for (x0, h, lean) in canes:
        h = max(4, h // 3) if burnt else h
        for j in range(h):
            x = x0 + lean * j; node = j % 5 == 4
            cv.put(x, gy - j, cn[1] if node else cn[3]); cv.put(x + 1, gy - j, cn[1] if node else cn[2])
            if not burnt and j == h // 2: cv.put(x + 2, gy - j - 1, cn[2]); cv.put(x + 3, gy - j - 2, cn[3]); cv.put(x + 4, gy - j - 2, cn[2])   # hoja seca
        tips.append((round(x0 + lean * h), gy - h))
    ellipse(cv, W / 2, gy + .5, 4, 1.6, cn[:4])
    cv.outline(); cv.clean()
    if not burnt and fire < 0:
        for k, (ci, dy) in enumerate(flies):
            tx, ty = tips[ci]
            small_dragonfly(cv, tx, ty - 1 + dy, f, k, side=1 if k % 2 == 0 else -1)
    if burnt: cv.put(W / 2, gy - 1, C(EMBER[2]))
    if fire >= 0:
        fr = random.Random(seed * 3 + fire)
        for (tx, ty) in tips:
            for _ in range(2): flame(cv, tx + fr.uniform(-1, 1), ty + fr.randrange(3, max(4, (gy - ty) // 2)), fr, h=fr.randrange(4, 8))
    ground_shadow(cv, W / 2, gy + 2, 5, 1.2)
    return cv

def perch_set(out, t, kind, seed):
    out[f'Perch_{kind}_Idle'] = [perch(t, kind, seed, f=f) for f in range(4)]
    if not t['burnt']:
        out[f'Perch_{kind}_Burn'] = [perch(t, kind, seed, fire=f) for f in range(4)]
        out[f'Perch_{kind}_Burnt'] = [perch(t, kind, seed, burnt=True)]

def dragonfly_shadow(f):
    """Sombra de una libélula gigante que vuela por encima (vista desde arriba, mirando a la derecha): cuerpo largo
    y 4 alas que baten (4 cuadros). El código la cruza por el suelo. Translúcida, sin contorno."""
    cv = Canvas(56, 40); sh = (24, 20, 37, 70)
    cx, cy = 28, 20
    for x in range(10, 48):                                          # cuerpo
        w = 1.6 if x > 36 else 1.0 if x < 30 else 1.3
        for y in range(int(cy - w), int(cy + w) + 1): cv.put(x, y, sh)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1, 2): cv.put(48 + dx, cy + dy, sh)        # cabeza
    span = (1.0, .75, .45, .75)[f]
    for (wx, ln, wdt) in ((38, 17, 3.2), (31, 15, 3.0)):           # alas delanteras y traseras
        for side in (-1, 1):
            for k in range(int(ln * span) + 1):
                for j in range(-int(wdt), int(wdt) + 1):
                    if abs(j) <= wdt * (1 - (k / max(1, ln)) ** 2) ** .5:
                        cv.put(wx + j - k * .25, cy + side * (k + 2), sh)
    return cv

def build_sky(t):
    out = {}
    for kind in SHAFTS: out[f'Fx_LightShaft_{kind}'] = [light_shaft(t, kind, f, 7 + ord(kind)) for f in range(6)]
    for kind in ('A', 'B'): out[f'Deco_LightPool_{kind}'] = [light_pool(t, kind, f) for f in range(6)]
    for i, kind in enumerate(PERCHES): perch_set(out, t, kind, 140 + i * 5)
    out['Fx_DragonflyShadow'] = [dragonfly_shadow(f) for f in range(4)]
    return out

def scene_sky(key, out, f=0):
    """Sala alargada y despejada: rayos de luz con su pozo, posaderos con libélulas, una sombra que cruza y dos
    libélulas gigantes enemigas."""
    bg = floor_bg(key, seed=8)
    for (x, y, kind) in ((120, 150, 'A'), (330, 190, 'B')):           # pozos de luz (bajo todo)
        im = out[f'Deco_LightPool_{kind}'][f % 6].img(); bg.alpha_composite(im, (x - im.width // 2, y - im.height // 2))
    sh = out['Fx_DragonflyShadow'][f % 4].img(); bg.alpha_composite(sh, (200 + f * 14, 210))
    put = putter(bg, out); burning = key != 'Burnt'
    for k, (x, y, kind) in enumerate(((60, 120, 'A'), (250, 100, 'B'), (420, 140, 'C'), (180, 250, 'C'), (400, 250, 'A'))):
        name = f'Perch_{kind}_Burn' if burning and k == 2 else f'Perch_{kind}_Idle'
        put(name, x, y, (f + k) % 4)
    enemies(bg, 'Dragonfly', 'Dragonfly_Fly.png', ((150, 60, False), (300, 140, True)), cw=40)
    for (x, y, kind) in ((120, 150, 'A'), (330, 190, 'B')):           # rayos encima de todo
        im = out[f'Fx_LightShaft_{kind}'][f % 6].img(); bg.alpha_composite(im, (x - im.width + SHAFTS[kind][2], y - im.height + 6))
    return bg

# =================================================================================================
# Salida
# =================================================================================================
def with_burn(out, name, fn, t, *a):
    """Idle, y en pantano y humeante también Burn (4, bucle) y Burnt."""
    out[f'{name}_Idle'] = [fn(t, *a)]
    if not t['burnt']:
        out[f'{name}_Burn'] = [fn(t, *a, fire=f) for f in range(4)]
        out[f'{name}_Burnt'] = [fn(t, *a, burnt=True)]

def build_nest(t):
    out = {}
    for i, kind in enumerate(NEST): with_burn(out, f'Nest_{kind}', nest, t, kind, 40 + i * 11)
    for i, kind in enumerate(PILES): with_burn(out, f'LeafPile_{kind}', leaf_pile, t, kind, 70 + i * 5)
    out['Deco_BeetleScorch'] = [scorch(t, 90 + k * 7) for k in range(4)]
    return out

def build_loom(t):
    out = {}
    for i, kind in enumerate(SILK_BASES): with_burn(out, f'SilkWrap_{kind}', silk_log, t, kind, 4 + i * 3)
    for i, kind in enumerate(COCOONS): with_burn(out, f'Cocoon_{kind}', cocoon, t, kind, 20 + i * 3)
    for i, kind in enumerate(EGGSACS): with_burn(out, f'EggSac_{kind}', egg_sac, t, kind, 30 + i * 3)
    return out

def room_bg(key):
    bg = Image.new('RGBA', (480, 270), (14, 22, 16, 255))
    room = Image.open(os.path.join(TEMP, 'floor3-environment', key, 'Sala_muestra.png')).convert('RGBA')
    bg.alpha_composite(room, (32, 7))
    return bg

def putter(bg, out):
    def put(name, x, y, frame=0):                                  # x, y = pie (centro de abajo)
        fr = out[name][min(frame, len(out[name]) - 1)]; im = fr.img()
        bg.alpha_composite(im, (x - im.width // 2, y - im.height))
    return put

def enemies(bg, folder, sheet, spots, cw=None):
    im = Image.open(os.path.join(TEMP, 'floor3-enemies', folder, sheet)).convert('RGBA'); h = im.height
    im = im.crop((0, 0, cw or h, h))
    for (x, y, fl) in spots: bg.alpha_composite(im.transpose(Image.FLIP_LEFT_RIGHT) if fl else im, (x, y))

def scene_nest(key, out, f=0):
    """La sala de muestra del tramo con el decorado del Nido de bombarderos y unos bombarderos (480×270)."""
    bg = room_bg(key); put = putter(bg, out)
    for k, (x, y) in enumerate(((150, 150), (270, 200), (230, 240), (390, 160))):
        sc = out['Deco_BeetleScorch'][k].img(); bg.alpha_composite(sc, (x - 17, y - 11))
    burning = key != 'Burnt'
    put('Nest_Big_Burn' if burning else 'Nest_Big_Idle', 360, 112, f)
    put('Nest_Small_Idle', 110, 232); put('Nest_Long_Idle', 270, 236)
    put('LeafPile_A_Idle', 330, 128); put('LeafPile_B_Burn' if burning else 'LeafPile_B_Idle', 196, 112, (f + 2) % 4)
    put('LeafPile_C_Idle', 150, 200)
    enemies(bg, 'Beetle', 'Beetle_Idle.png', ((300, 150, True), (350, 190, True), (200, 196, False)))
    return bg

def scene_loom(key, out, f=0):
    """La sala de muestra con el decorado del Telar y unas arañas."""
    bg = room_bg(key); put = putter(bg, out)
    burning = key != 'Burnt'
    put('SilkWrap_Fallen_Idle', 340, 128); put('SilkWrap_Trunk_Burn' if burning else 'SilkWrap_Trunk_Idle', 140, 240, f)
    put('SilkWrap_Stump_Idle', 400, 250); put('SilkWrap_Stump_Idle', 200, 120)
    put('Cocoon_Large_Idle', 300, 140); put('Cocoon_Small_Idle', 250, 210); put('Cocoon_Medium_Idle', 170, 250)
    put('Cocoon_Medium_Burn' if burning else 'Cocoon_Medium_Idle', 380, 160, (f + 1) % 4)
    put('EggSac_C_Idle', 365, 136); put('EggSac_A_Idle', 120, 160); put('EggSac_B_Idle', 290, 250)
    enemies(bg, 'Spider', 'Spider_Idle.png', ((300, 160, True), (215, 190, False)))
    return bg

THEMES = {   # clave: (construir, escena, nombre de la maqueta, piezas para la vista previa)
    'sky': (build_sky, scene_sky, 'cielo', ('Fx_LightShaft_A', 'Fx_LightShaft_B', 'Deco_LightPool_A', 'Perch_A_Idle',
            'Perch_B_Idle', 'Perch_C_Idle', 'Perch_B_Burn', 'Perch_A_Burnt', 'Fx_DragonflyShadow')),
    'meadow': (build_meadow, scene_meadow, 'pradera', ('CalamitesClump_A_Idle', 'CalamitesClump_B_Idle',
               'CalamitesClump_C_Idle', 'CalamitesClump_A_Burn', 'CalamitesClump_C_Burnt', 'Deco_HorsetailGrass',
               'Deco_FallenCones', 'Fx_SporeDrift')),
    'pond': (build_pond, scene_pond, 'charca', ('Water_0', 'Water_5', 'Water_15', 'Water_Corner_NE', 'Water_Shimmer',
             'ShoreReeds_A_Idle', 'ShoreReeds_B_Idle', 'ShoreReeds_C_Idle', 'ShoreReeds_A_Burn', 'ShoreReeds_C_Burnt',
             'Deco_ToadSpawn', 'Deco_WetMud', 'TadpolePool', 'MossRock_A', 'MossRock_B', 'MossRock_C')),
    'nest': (build_nest, scene_nest, 'nido', ('Nest_Big_Idle', 'Nest_Small_Idle', 'Nest_Long_Idle', 'Nest_Big_Burnt',
             'LeafPile_A_Idle', 'LeafPile_B_Idle', 'LeafPile_C_Idle', 'LeafPile_A_Burnt', 'Deco_BeetleScorch', 'Nest_Big_Burn')),
    'loom': (build_loom, scene_loom, 'telar', ('SilkWrap_Fallen_Idle', 'SilkWrap_Trunk_Idle', 'SilkWrap_Stump_Idle',
             'SilkWrap_Fallen_Burnt', 'SilkWrap_Trunk_Burn', 'Cocoon_Small_Idle', 'Cocoon_Medium_Idle', 'Cocoon_Large_Idle',
             'Cocoon_Large_Burnt', 'EggSac_A_Idle', 'EggSac_B_Idle', 'EggSac_C_Idle', 'EggSac_C_Burnt')),
}

if __name__ == '__main__':
    todo = sys.argv[1:] or list(THEMES)
    for th in todo:
        build, scene, label, shown = THEMES[th]
        rows = {}
        for key in ('Swamp', 'Smoke', 'Burnt'):
            t = tram(key); out = build(t)
            d = os.path.join(HERE, key); os.makedirs(d, exist_ok=True)
            for name, frames in out.items(): save_sheet(os.path.join(d, name + '.png'), frames)
            for name in shown:
                if name in out: rows[f'{key} {name}'] = out[name]
            frames = [scene(key, out, f).resize((960, 540), Image.NEAREST) for f in range(4)]
            os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True)
            frames[0].save(os.path.join(HERE, f'maqueta_{label}_{key}.png'))
            if key != 'Burnt':
                frames[0].save(os.path.join(HERE, 'gif', f'escena_{label}_{key}.gif'), save_all=True,
                               append_images=frames[1:], duration=110, loop=0, disposal=2)
            print('  ', th, key, len(out), 'hojas')
        preview(os.path.join(HERE, f'vista_previa_{label}.png'), rows, BG, scale=4)
    print('Listo en', HERE)
