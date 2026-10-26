"""
generar_mutaciones.py - Primordia: sprites de las 9 mutaciones del jugador.

Reutiliza generar_jugador.py (paletas, Canvas, body, features, contorno y los
parámetros de cada cuadro de Idle, Move, Dodge y Hurt), así que todo encaja
píxel a píxel con la célula.

Uso:
  python generar_mutaciones.py [ruta/generar_jugador.py] [carpeta_salida]
Por defecto: ../player/generar_jugador.py y la carpeta de este script.

Ranuras (orden de dibujo, de atrás hacia delante):
  Extremidades -> Cuerpo (Piel) -> Órgano -> Boca
  (mirando hacia arriba la Boca va detrás del cuerpo)
"""
import math
import os
import sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
PLAYER_PY = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, '..', 'player', 'generar_jugador.py')
OUT = sys.argv[2] if len(sys.argv) > 2 else HERE

# ------------------------------------------------- cargar generar_jugador.py
# Se ejecuta el script del jugador sin la parte que guarda archivos. Mientras
# construye sus animaciones se anotan los parámetros de cada llamada a cell(),
# para que las mutaciones usen exactamente el mismo balanceo.
_src = open(PLAYER_PY, encoding='utf-8').read().replace('\r', '')
_defs, _rest = _src.split("\nFACINGS = ", 1)
_anims_src = "FACINGS = " + _rest.split("\nos.makedirs(f'{OUT}/frames'", 1)[0]
P = {'__name__': 'generar_jugador'}
_argv = sys.argv
sys.argv = ['generar_jugador.py', os.devnull]
exec(_defs, P)
sys.argv = _argv

_calls = {}
_real_cell = P['cell']


def _rec_cell(facing='side', squash=0.0, bob=0, lean=0.0, pal=P['GREEN'], angle=0.0, eyes='open', n=16, grow=0.0):
    cv = _real_cell(facing, squash, bob, lean, pal, angle, eyes, n, grow)
    _calls[id(cv)] = dict(facing=facing, squash=squash, bob=bob, lean=lean, pal=pal,
                          angle=angle, eyes=eyes, grow=grow)
    return cv


P['cell'] = _rec_cell
exec(_anims_src, P)
P['cell'] = _real_cell

Canvas, body, features, rot, C = P['Canvas'], P['body'], P['features'], P['rot'], P['C']
OUTL, GREEN, RED, WHITE, PALE = P['OUTL'], P['GREEN'], P['RED'], P['WHITE'], P['PALE']
PANIMS = P['anims']
FACINGS = P['FACINGS']
# Parámetros por cuadro de cada animación del jugador (Idle_side, Move_down, ...).
FRAMES = {name: [_calls[id(cv)] for cv in frs] for name, frs in PANIMS.items() if name != 'Death'}


def CA(h, a=255):
    return C(h)[:3] + (a,)


# ------------------------------------------------------------- paletas extra
# Depredador: naranjas y rojos cálidos, hueso claro
FLESH = ['#8a2e1a', '#d0562a', '#f08a4a', '#ffc080']
BONE = ['#7a6a5a', '#bfae94', '#e6dac0', '#fff8e8']
FIN = ['#a8321e', '#e0582a', '#ff9a4a', '#ffd08a']
INK = ['#0e1430', '#1c2a58', '#30488a', '#5a7cc0']
# Acorazado: grises azulados, brillo frío
ARM = ['#34405a', '#56688a', '#8aa0bc', '#cfe0f2']
ARM_RED = ['#6a2432', '#a8445a', '#d8848e', '#ffd0d0']
ARM_PALE = ['#646e7e', '#8c98a8', '#b4c0cc', '#dfe7ef']
SHOCK = ['#2a5a9a', '#4a9ae0', '#9ad8ff', '#ffffff']
# Tóxico: lima ácido y morados
LIME = ['#4a7a1a', '#86bc2c', '#c0f040', '#eaff9a']
PURP = ['#3e1660', '#6e2a98', '#a85ad0', '#dca8ff']
DARKCELL = dict(ramp=['#1e4a38', '#2c6a48', '#3e9654', '#74c45e'], nuc='#2f7d4f', nucd='#1e5a3a',
                hi='#ffffff', hi2='#f0ffd8')


def shade_v(nx, ny):
    """Misma luz que body(): arriba a la izquierda."""
    return (-0.6 * nx - 0.8 * ny) * 0.8 + 0.35 - max(0, math.hypot(nx, ny) - 0.75) * 1.2


def ramp_idx(v):
    return 0 if v < -0.15 else 1 if v < 0.35 else 2 if v < 0.75 else 3


def geom(n, r0x, r0y, squash, bob, grow, maxr=99):
    """Igual que cell(): el cuerpo se apoya abajo y se aplasta/estira."""
    rx = min(maxr, (r0x + grow) * (1 + 0.12 * squash))
    ry = (r0y + grow) * (1 - 0.12 * squash)
    cx = n / 2
    cy = n / 2 + 0.5 + r0y - ry + bob
    return cx, cy, rx, ry


# ================================================================== 1. PIEL
CARA_R = (9.0, 7.0)    # caparazón: ~20x16 con contorno
STING_R = (7.0, 7.0)   # piel urticante: ~16x16 más las púas


def carapace(facing='side', squash=0.0, bob=0, lean=0.0, pal=GREEN, angle=0.0, eyes='open', grow=0.0, n=24,
             plates=None):
    cv = Canvas(n)
    cx, cy, rx, ry = geom(n, *CARA_R, squash, bob, grow, maxr=n / 2 - 1.6)
    body(cv, cx, cy, rx, ry, pal, lean)
    if plates is None:
        plates = ARM_RED if pal is RED else (['#ffffff'] * 4 if pal is WHITE else ARM_PALE if pal is PALE else ARM)
    rolled = eyes == 'none'
    gap = C(pal['ramp'][0])            # la membrana asoma entre placas
    for (x, y) in list(cv.px):
        px, py = x + .5, y + .5
        t = (cy - py) / ry
        sx = px - cx - lean * max(0, t)
        nx, ny = sx / rx, (py - cy) / ry
        r = math.hypot(nx, ny)
        if r > 1 - 1.3 / min(rx, ry):          # anillo de membrana verde en el borde
            continue
        if not rolled:
            if facing == 'side' and ny > 0.45:   # de lado se ve la panza blanda
                continue
            if facing == 'down' and ny > 0.55:
                continue
        # coordenada a lo largo del cuerpo -> segmentos (placas)
        ux, uy = rot(sx, py - cy, -angle)
        u = ux if (facing == 'side' and not rolled) else uy
        seg = (u + 40.5) / 4.2
        f = seg - math.floor(seg)
        if f < 0.24:
            cv.px[(x, y)] = gap
            continue
        # cada placa: canto claro arriba, cuerpo medio y borde oscuro abajo;
        # el lado en sombra (abajo a la derecha) baja un tono
        v = shade_v(nx, ny)
        i = (3 if v > 0.3 else 2) if f < 0.45 else 2 if f < 0.8 else 1
        if v < -0.1:
            i -= 1
        cv.px[(x, y)] = C(plates[i])
    features(cv, cx + lean * 0.5, cy, rx, ry, pal, facing, angle, eyes)
    cv.outline()
    return cv


def stinging(facing='side', squash=0.0, bob=0, lean=0.0, pal=GREEN, angle=0.0, eyes='open', grow=0.0, n=24,
             bristle=0):
    cv = Canvas(n)
    cx, cy, rx, ry = geom(n, *STING_R, squash, bob, grow, maxr=7.9)
    bpal = DARKCELL if pal is GREEN else pal
    # púas (lima y morado) y pelillos alrededor del contorno
    N = 16
    for i in range(N):
        a = 2 * math.pi * i / N + angle + 0.1
        big = i % 2 == 0
        L = (2.8 if big else 1.5) + bristle * (0.7 if big else 0.4)
        ex, ey = cx + math.cos(a) * rx, cy + math.sin(a) * ry
        if pal is WHITE:
            cols = ['#ffffff'] * 3
        elif pal is RED:
            cols = [RED['ramp'][0], RED['ramp'][1], RED['ramp'][2]]
        elif pal is PALE:
            cols = ['#8a7a9a', '#b0a8b8', '#d8d0dc']
        elif big:
            cols = [PURP[1], PURP[2], LIME[2]]
        else:
            cols = [LIME[1], LIME[2], LIME[3]]
        steps = int(L * 2) + 1
        for s in range(steps):
            d = -0.5 + s * 0.5
            c = cols[min(2, int((d + 0.5) / max(L, 0.01) * 3))]
            px_, py_ = ex + math.cos(a) * d, ey + math.sin(a) * d
            if 1 <= round(px_) <= n - 2 and 1 <= round(py_) <= n - 2:   # deja sitio al contorno
                cv.put(px_, py_, c)
    body(cv, cx, cy, rx, ry, bpal, lean)
    # motas lima/moradas en la membrana
    if pal is GREEN:
        for (dx, dy, c) in ((-3, -1, LIME[1]), (3, 2, PURP[2]), (1, -4, LIME[1]), (-2, 4, PURP[2]), (5, -2, LIME[1])):
            x, y = rot(dx * rx / 7.5, dy * ry / 7.5, angle)
            cv.put_in(cx + x, cy + y, c)
    features(cv, cx + lean * 0.5, cy, rx, ry, bpal, facing, angle, eyes)
    cv.outline()
    return cv


def skin_anims(maker, extra_move=None):
    a = {}
    for name, params in FRAMES.items():
        frs = []
        for i, p in enumerate(params):
            kw = dict(p)
            if extra_move is not None:
                kw.update(extra_move(name, i))
            frs.append(maker(**kw))
        a[name] = frs
    return a


def burst_frame(n, r, sz, cx, cy, core, shards):
    cv = Canvas(n)
    if core:
        body(cv, cx, cy, 4, 3.4, GREEN)
    for k in range(8):
        a = k * math.pi / 4 + 0.3
        body(cv, cx + math.cos(a) * r, cy + math.sin(a) * r * 0.8, sz, sz, GREEN)
    for k, (kind, a0) in enumerate(shards):
        a = a0
        gx, gy = cx + math.cos(a) * (r + 2), cy + math.sin(a) * (r + 2) * 0.8
        if kind == 'plate':
            for dx in (-1, 0, 1):
                for dy in (0, 1):
                    x, y = rot(dx, dy, a + k)
                    cv.put(gx + x, gy + y, ARM[2] if dy == 0 else ARM[1])
            cv.put(gx + rot(-1, 0, a + k)[0], gy + rot(-1, 0, a + k)[1], ARM[3])
        else:
            for s in range(3):
                cv.put(gx + math.cos(a) * s, gy + math.sin(a) * s, [PURP[1], PURP[2], LIME[2]][s])
    cv.outline()
    return cv


def skin_death(maker, shard_kind, n=32):
    c = n / 2
    fr = [maker('side', -0.5, 0, pal=WHITE, eyes='wide', n=n),
          maker('down', 0, 0, pal=PALE, eyes='x', n=n, grow=1),
          maker('down', -0.3, -1, pal=PALE, eyes='x', n=n, grow=2)]
    angs = [0.1 + k * 2 * math.pi / 7 for k in range(7)]
    shards = [(shard_kind, a) for a in angs]
    fr.append(burst_frame(n, 9, 2.4, c, c + 1, True, shards))
    fr.append(burst_frame(n, 10, 1.8, c, c + 1, False, shards))
    fr.append(burst_frame(n, 10.5, 1.2, c, c, False, shards[::2]))
    cv = Canvas(n)
    body(cv, c, c + 8, 8, 2, PALE)
    cv.put(c - 3, c + 7, '#e8ffd8')
    if shard_kind == 'plate':
        for gx in (c - 5, c + 4):
            for dx in (-1, 0, 1):
                cv.put(gx + dx, c + 6, ARM_PALE[2]); cv.put(gx + dx, c + 7, ARM_PALE[1])
    else:
        for gx in (c - 6, c + 2, c + 6):
            cv.put(gx, c + 6, PURP[2]); cv.put(gx + 1, c + 6, LIME[2])
    cv.outline()
    fr.append(cv)
    return fr


def build_skins():
    skins = {}
    skins['Carapace'] = skin_anims(lambda **k: carapace(**k))
    # las púas se erizan al moverse (y un poco al aplastarse en reposo)
    skins['StingingSkin'] = skin_anims(
        lambda **k: stinging(**k),
        lambda name, i: {'bristle': (1 if name.startswith('Move') else (0.5 if name.startswith('Idle') and i == 2 else 0))
                         + (1 if name == 'Hurt' else 0)})
    skins['Carapace']['Death'] = skin_death(lambda *a, **k: carapace(*a, **k), 'plate')
    skins['StingingSkin']['Death'] = skin_death(lambda *a, **k: stinging(*a, **k), 'spike')
    return skins


# ================================================================== 2. BOCA
# Lienzo 24x24 mirando a la derecha. Punto de unión: (0, 12).
AY = 12


def thick(cv, pts, widths, colfn):
    """Trazo grueso por una lista de puntos con anchura variable."""
    for (x0, y0), (x1, y1), w in zip(pts, pts[1:], widths):
        steps = max(2, int(math.hypot(x1 - x0, y1 - y0) * 3))
        for s in range(steps + 1):
            t = s / steps
            x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
            r = w / 2
            for yy in range(int(y - r - 1), int(y + r + 2)):
                for xx in range(int(x - r - 1), int(x + r + 2)):
                    if (xx + .5 - x) ** 2 + (yy + .5 - y) ** 2 <= r * r:
                        c = colfn(xx, yy, x, y, r)
                        if c is not None:
                            cv.put(xx, yy, c)


def blob(cv, cx, cy, rx, ry, ramp, alpha=255):
    for y in range(cv.n):
        for x in range(cv.n):
            nx, ny = (x + .5 - cx) / rx, (y + .5 - cy) / ry
            if nx * nx + ny * ny <= 1:
                cv.put(x, y, CA(ramp[ramp_idx(shade_v(nx, ny))], alpha))


def jaw(cv, open_a, sign, reach=1.0):
    """Pinza de hueso curva. sign=-1 la de arriba, +1 la de abajo."""
    base = (5.5, AY + sign * 2.5)
    pts, ws = [], []
    L = 16 * reach
    for i in range(9):
        t = i / 8
        u = L * t
        v = sign * (2.5 * math.sin(math.pi * t * 0.9) - 4.5 * t ** 3)   # se abomba y la punta se curva hacia dentro
        a = sign * open_a
        x, y = u * math.cos(a) - v * math.sin(a), u * math.sin(a) + v * math.cos(a)
        pts.append((base[0] + x, base[1] + y))
        ws.append(4.4 - 3.2 * t)
    def col(xx, yy, x, y, r):
        dy = (yy + .5 - y) * -sign      # >0 hacia el lado exterior
        return BONE[3] if (dy < -r * 0.25 and sign < 0) else BONE[2] if dy < r * 0.2 else BONE[1]
    thick(cv, pts, ws, col)
    # dientes serrados en el borde interior, apuntando hacia el hueco
    for i in (2, 3, 4, 5, 6):
        (x0, y0), (x1, y1) = pts[i], pts[i + 1]
        dx, dy = x1 - x0, y1 - y0
        ln = math.hypot(dx, dy) or 1
        nx, ny = -dy / ln, dx / ln
        if abs(y0 + ny - AY) > abs(y0 - AY):     # normal hacia el hueco entre las pinzas
            nx, ny = -nx, -ny
        w = ws[i] / 2
        tl = 2 if i in (3, 5) else 1                   # dientes alternos largos y cortos
        for k in range(tl):
            tx, ty = x0 + nx * (w + 0.7 + k), y0 + ny * (w + 0.7 + k)
            cv.put(tx, ty, BONE[3] if k == tl - 1 else BONE[1])
    return pts[-1]


def mandibles(open_a, reach=1.0, flesh_s=1.0):
    cv = Canvas(24)
    jaw(cv, open_a, -1, reach)
    jaw(cv, open_a, +1, reach)
    # base carnosa naranja
    blob(cv, 4.6, AY, 3.6 * flesh_s, 4.6 * flesh_s, FLESH)
    for (x, y) in ((3, AY - 2), (4, AY - 3)):
        cv.put_in(x, y, FLESH[3])
    cv.put_in(6, AY + 2, FLESH[0])
    cv.outline()
    return cv


def stinger(shift=0.0, drop=1.0, wig=0.0, flash=False):
    cv = Canvas(24)
    # tallo carnoso morado en la base
    blob(cv, 3.6 + shift, AY, 2.6, 4.2, PURP)
    # segmentos lima, cada uno más estrecho
    segs = [(5.0, 4.0), (9.2, 3.4), (12.9, 2.8), (16.1, 2.2)]
    for k, (sx, w) in reversed(list(enumerate(segs))):
        x0 = sx + shift
        oy = AY + wig * (k % 2 * 2 - 1) * 0.5 * (k / 3)
        blob(cv, x0 + 2.2, oy, 2.6, w, LIME)
        for yy in range(-int(w) + 1, int(w)):          # junta oscura entre segmentos
            cv.put_in(x0 + 0.2, oy + yy, LIME[0])
    # punta
    for i in range(5):
        x = 18 + shift + i
        cv.put(x, AY - 1, LIME[2] if i < 3 else LIME[3])
        if i < 3:
            cv.put(x, AY, LIME[1])
    if drop > 0:
        blob(cv, 21.6 + shift, AY + 1.2, 1.8 * drop + 0.2, 2.1 * drop + 0.2, PURP)
        cv.put_in(21 + shift, AY + 0.4, PURP[3])
    if flash:
        for (dx, dy) in ((0, 0), (1, 0), (0, -1), (0, 1), (-1, 0)):
            cv.put(22 + shift + dx, AY - 0.5 + dy, '#ffffff')
        cv.put(23, AY - 3, LIME[3]); cv.put(23, AY + 2, LIME[3])
    cv.outline()
    return cv


def spores(spread=1.0, sizes=(1, 1, 1), open_=False, squeeze=0.0):
    cv = Canvas(24)
    # sacos: delante, arriba y abajo (en abanico)
    sacs = [(14.5, AY, 0.0), (11.0, AY - 5.2, -1), (11.0, AY + 5.2, 1)]
    for k, (x, y, side) in enumerate(sacs):
        x = 4 + (x - 4) * spread - squeeze
        y = AY + (y - AY) * spread * (1 - 0.25 * squeeze)
        sacs[k] = (x, y, side)
    # tallo corto
    for (x, y, side) in sacs:
        thick(cv, [(1, AY), (5, AY + side * 1.2), (x - 2, y)], [2.6, 2.0],
              lambda xx, yy, x_, y_, r: LIME[1] if yy + .5 < y_ else LIME[0])
    for k, (x, y, side) in enumerate(sacs):
        r = 3.6 * sizes[k]
        blob(cv, x, y, r, r * 0.95, PURP, alpha=225)
        for (dx, dy) in ((-1, -1), (1, 1), (1.5, -1.5)):
            cv.put_in(x + dx * r / 3.6, y + dy * r / 3.6, LIME[2])
        cv.put_in(x - r * 0.45, y - r * 0.55, PURP[3])
        if open_:   # boca abierta del saco, hacia fuera
            ox, oy = x + r * 0.6, y + side * r * 0.6
            cv.put_in(ox, oy, '#1a0a28')
            cv.put_in(ox - 0.8 + (0 if side else 0), oy, '#1a0a28')
    # base carnosa
    blob(cv, 3.0, AY, 2.4, 3.2, PURP)
    cv.outline()
    return cv


def build_mouths():
    m = {}
    m['Mandibles'] = {
        'Idle': [mandibles(0.42), mandibles(0.47, flesh_s=1.03), mandibles(0.50, flesh_s=1.05), mandibles(0.46)],
        # se abre del todo, se cierra de golpe hacia delante, vuelve
        'Attack': [mandibles(0.62, 0.9), mandibles(0.0, 1.12), mandibles(0.12, 1.05)],
    }
    m['Stinger'] = {
        'Idle': [stinger(0, 1.0, 0), stinger(0, 1.1, 1), stinger(0, 1.2, 0), stinger(0, 1.1, -1)],
        # dispara (destello), retrocede 2 px y vuelve con la gota creciendo
        'Attack': [stinger(0, 0, flash=True), stinger(-2, 0), stinger(-1, 0.5)],
    }
    m['Spores'] = {
        'Idle': [spores(sizes=(1, 0.95, 1)), spores(sizes=(1.05, 1, 0.95)), spores(sizes=(1, 1.05, 1)),
                 spores(sizes=(0.95, 1, 1.05))],
        # se aprietan, se abren en abanico, quedan vacíos
        'Attack': [spores(0.8, (0.9, 0.9, 0.9), squeeze=1.0), spores(1.25, (1.1, 1.1, 1.1), open_=True),
                   spores(1.1, (0.8, 0.8, 0.8), open_=True)],
    }
    return m


# ========================================================== 3. EXTREMIDADES
# Lienzo 32x32, centro de la célula en (16, 16); siguen el balanceo de la célula.
LN = 32


def cell_geom(p):
    return geom(LN, 6.5, 6.0, p['squash'], p['bob'], p.get('grow', 0))


def fin(cv, root, direction, spread, length, phase, alpha=200):
    """Aleta en abanico con radios y borde festoneado entre radio y radio."""
    rx0, ry0 = root
    nrib = 5
    for y in range(cv.n):
        for x in range(cv.n):
            dx, dy = x + .5 - rx0, y + .5 - ry0
            r = math.hypot(dx, dy)
            if r < 0.5:
                continue
            a = math.atan2(dy, dx)
            rel = (a - direction + math.pi) % (2 * math.pi) - math.pi
            if abs(rel) > spread:
                continue
            k = (rel / spread + 1) / 2 * (nrib - 1)       # 0..4 entre radios
            between = abs(k - round(k))                   # 0 en un radio, 0.5 en medio
            wave = 0.6 * math.sin(phase + k * 1.4)        # ondula a lo largo del borde
            L = length - 2.4 * between + wave
            if r > L:
                continue
            rib = between < 0.16 and r > 3
            down = rel * (1 if math.cos(direction) >= 0 else -1) > 0.15
            if rib:
                i = 1
            elif r > L - 1.4:
                i = 3 if not down else 2
            else:
                i = 2 if not down else 1
            cv.put(x, y, CA(FIN[i], 235 if rib else alpha))


def fins_frame(facing, p, anim, f):
    cv = Canvas(LN)
    cx, cy, rx, ry = cell_geom(p)
    if anim == 'Move':
        flap = [0.5, 0.15, -0.4, -0.05][f]; ln = [12, 12.5, 11.5, 12.5][f]
    else:
        flap = [0.1, 0.0, -0.1, 0.0][f]; ln = 12
    ph = f * math.pi / 2
    if facing == 'side':
        # las dos aletas laterales, barridas hacia atrás (detrás del cuerpo)
        fin(cv, (cx - 1.5, cy - 1), math.pi + 0.5 + flap, 0.42, ln - 0.5, ph)
        fin(cv, (cx - 1.5, cy + 1.5), math.pi - 0.45 - flap * 0.8, 0.42, ln - 1, ph + 1)
    else:
        for s in (-1, 1):
            tilt = (0.35 if facing == 'down' else -0.35) + flap * (1 if facing == 'down' else -1)
            base = 0 if s > 0 else math.pi
            fin(cv, (cx + s * 2.5, cy + 0.5), base + s * tilt, 0.55, ln, ph + (s > 0))
    cv.outline()
    return cv


def leg(cv, hip, foot, lift, far=False, bend=(0, -3)):
    hx, hy = hip
    fx, fy = foot[0], foot[1] - lift
    knee = (hx + (fx - hx) * 0.5 + bend[0], hy + (fy - hy) * 0.5 + bend[1] - lift)
    ramp = ARM if not far else [ARM[0], ARM[0], ARM[1], ARM[2]]
    def col_f(xx, yy, x, y, r):
        return ramp[3] if (yy + .5 < y - 0.2) else ramp[2]
    def col_t(xx, yy, x, y, r):
        return ramp[2] if (yy + .5 < y) else ramp[1]
    thick(cv, [hip, knee], [2.0], col_f)
    thick(cv, [knee, (fx, fy)], [1.5], col_t)
    cv.put(knee[0], knee[1], ramp[3])           # articulación
    cv.put(fx, fy, ramp[0])                     # uña


def legs_frame(facing, p, anim, f):
    cv = Canvas(LN)
    cx, cy, rx, ry = cell_geom(p)
    # paso alterno en trípode: grupo A y grupo B se turnan
    if anim == 'Move':
        stepA = [2, 0, -2, 0][f]; liftA = [0, 0, 0, 1.5][f]
        stepB = -stepA; liftB = [0, 1.5, 0, 0][f]
    else:
        stepA = stepB = 0; liftA = liftB = 0
    if facing == 'side':
        # de lado: patas bajo el cuerpo, las del lado lejano más oscuras y detrás
        for far in (True, False):
            for k, hx in enumerate((-4.0, 0.0, 4.0)):
                grpA = (k % 2 == 0) != far
                st, lf = (stepA, liftA) if grpA else (stepB, liftB)
                ox = 1.5 if far else 0
                hip = (cx + hx + ox, cy + ry - 3)
                foot = (16 + hx * 2.0 + ox + st, 16 + 12.5)
                leg(cv, hip, foot, lf, far, bend=(2.2 if hx >= 0 else -2.2, -0.5))
    else:
        for s in (-1, 1):
            for k, a in enumerate((-0.75, 0.0, 0.75)):
                grpA = (k % 2 == 0) == (s > 0)
                st, lf = (stepA, liftA) if grpA else (stepB, liftB)
                if facing == 'up':
                    st = -st
                hip = (cx + s * (rx - 2) * math.cos(a), cy + (ry - 2) * math.sin(a))
                foot = (16 + s * 13.2 * math.cos(a), 16.5 + 11.5 * math.sin(a) + 1 + st)
                leg(cv, hip, foot, lf, False, bend=(s * 0.5, -3))
    cv.outline()
    return cv


def build_limbs():
    out = {}
    for name, fn in (('Fins', fins_frame), ('JointedLegs', legs_frame)):
        d = {}
        for anim in ('Idle', 'Move'):
            for facing in FACINGS:
                d[f'{anim}_{facing}'] = [fn(facing, p, anim, i) for i, p in enumerate(FRAMES[f'{anim}_{facing}'])]
        out[name] = d
    return out


# ================================================================ 4. ÓRGANO
# 16x16, mismo encuadre que la célula. En la mitad de abajo, bajo los ojos.
OCX, OCY = 8.0, 11.0


def translucent_outline(cv, alpha=170):
    cv.outline()
    for p, c in list(cv.px.items()):
        if c == OUTL:
            cv.px[p] = OUTL[:3] + (alpha,)


def inksac(scale=1.0, n=16, squirt=0):
    cv = Canvas(n)
    o = (n - 16) / 2
    cx, cy = OCX + o, OCY + o
    blob(cv, cx, cy, 4.3 * scale, 2.9 * scale, INK, alpha=225)
    # brillo húmedo y una gota de tinta más oscura dentro
    cv.put_in(cx - 2 * scale, cy - 1.4 * scale, CA('#a8c0f0', 235))
    cv.put_in(cx - 1 * scale, cy - 1.6 * scale, CA(INK[3], 235))
    for (dx, dy) in ((1, 0.5), (2, 0.5), (1, 1.5)):
        cv.put_in(cx + dx * scale, cy + dy * scale, CA(INK[0], 240))
    translucent_outline(cv)
    return cv


def shockgland(f, n=16, power=0.0):
    cv = Canvas(n)
    o = (n - 16) / 2
    cx, cy = OCX + o, OCY + o
    r = 2.6 + 0.4 * (f % 2) + power
    blob(cv, cx, cy, r + 1.3, r + 0.6, ['#2a5a9a', '#3a7ac0', '#4a9ae0', '#6ab4f0'], alpha=120)   # halo
    blob(cv, cx, cy, r + 0.4, r, SHOCK, alpha=235)
    cv.put_in(cx - 0.8, cy - 0.8, '#ffffff')
    translucent_outline(cv)
    # chispas en zigzag que giran alrededor
    for k in range(3):
        a = f * math.pi / 2 * 0.5 + k * 2 * math.pi / 3
        sx, sy = cx + math.cos(a) * (r + 1.6), cy + math.sin(a) * (r + 0.8)
        for (px_, py_, c) in ((sx, sy, SHOCK[3]), (sx + math.cos(a + 1.2), sy + math.sin(a + 1.2) * 0.8, SHOCK[2])):
            if 1 <= round(px_) <= n - 2 and 1 <= round(py_) <= n - 2:
                cv.put(px_, py_, c)
    return cv


def ring(cv, cx, cy, r, w, cols, gaps=0, alpha=255):
    for y in range(cv.n):
        for x in range(cv.n):
            d = math.hypot(x + .5 - cx, (y + .5 - cy))
            if abs(d - r) <= w / 2:
                a = math.atan2(y + .5 - cy, x + .5 - cx)
                if gaps and math.sin(a * gaps) > 0.6:
                    continue
                t = (d - (r - w / 2)) / max(w, 0.01)
                cv.put(x, y, CA(cols[min(len(cols) - 1, int(t * len(cols)))], alpha))


def ink_activate():
    fr = []
    # 0: se contrae de golpe
    fr.append(inksac(0.6, n=32))
    # 1-3: suelta tinta en chorros que se abren
    for k, (r, sz, al) in enumerate(((5.5, 2.4, 235), (8.5, 2.8, 215), (11, 2.2, 170))):
        cv = Canvas(32)
        cx, cy = 16, 16 + (OCY - 8)
        if k == 0:
            blob(cv, cx, cy, 2.2, 1.5, INK, alpha=230)
        for j in range(7):
            a = j * 2 * math.pi / 7 + 0.4 + k * 0.15
            blob(cv, cx + math.cos(a) * r, cy + math.sin(a) * r * 0.85, sz, sz * 0.85, INK, alpha=al)
        translucent_outline(cv, 140 if k == 2 else 170)
        fr.append(cv)
    return fr


def shock_activate():
    fr = []
    cx, cy = 16, 16 + (OCY - 8)
    fr.append(shockgland(0, n=32, power=1.2))                     # se carga
    cv = Canvas(32)                                               # destello
    blob(cv, cx, cy, 5, 5, ['#9ad8ff', '#ffffff', '#ffffff', '#ffffff'])
    ring(cv, cx, cy, 6.5, 1.5, [SHOCK[2], SHOCK[3]])
    fr.append(cv)
    cv = Canvas(32)                                               # la onda se expande
    blob(cv, cx, cy, 2.4, 2.4, SHOCK, alpha=230)
    ring(cv, cx, cy, 10, 2, [SHOCK[1], SHOCK[2], SHOCK[3]])
    for k in range(6):
        a = k * math.pi / 3 + 0.3
        for s in range(3):
            cv.put(cx + math.cos(a) * (6 + s * 1.3) + (s % 2), cy + math.sin(a) * (6 + s * 1.3), SHOCK[3])
    fr.append(cv)
    cv = Canvas(32)                                               # se desvanece
    ring(cv, cx, cy, 11.5, 1.2, [SHOCK[1], SHOCK[2]], gaps=5, alpha=170)
    blob(cv, cx, cy, 2, 2, SHOCK, alpha=200)
    fr.append(cv)
    return fr


def build_organs():
    return {
        'InkSac': {'Idle': [inksac(s) for s in (0.92, 1.0, 1.06, 1.0)], 'Activate': ink_activate()},
        'ShockGland': {'Idle': [shockgland(f) for f in range(4)], 'Activate': shock_activate()},
    }


# ================================================================== GUARDAR
def sheet(frames):
    n = frames[0].n
    im = Image.new('RGBA', (n * len(frames), n), (0, 0, 0, 0))
    for i, cv in enumerate(frames):
        im.paste(cv.img(), (i * n, 0))
    return im


BG = C('#16352e')


def gif(path, frames, ms):
    n = max(cv.n for cv in frames)
    sc = 8 if n <= 24 else 6
    out = []
    for cv in frames:
        b = Image.new('RGBA', (n, n), BG)
        im = cv.img()
        b.alpha_composite(im, ((n - im.width) // 2, (n - im.height) // 2))
        out.append(b.resize((n * sc, n * sc), Image.NEAREST).convert('RGB'))
    out[0].save(path, save_all=True, append_images=out[1:], duration=ms, loop=0)


MS = {'Idle': 180, 'Move': 110, 'Dodge': 60, 'Hurt': 90, 'Death': 110, 'Attack': 70, 'Activate': 80}


def save_all(skins, mouths, limbs, organs):
    groups = {'Skin': [], 'Mouth': [], 'Limbs': [], 'Organ': []}
    for m, anims in skins.items():
        for a, frs in anims.items():
            groups['Skin'].append((f'Skin_{m}_{a}', frs))
    for m, anims in mouths.items():
        for a, frs in anims.items():
            groups['Mouth'].append((f'Mouth_{m}_{a}', frs))
    for m, anims in limbs.items():
        for a, frs in anims.items():
            groups['Limbs'].append((f'Limbs_{m}_{a}', frs))
    for m, anims in organs.items():
        for a, frs in anims.items():
            groups['Organ'].append((f'Organ_{m}_{a}', frs))
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    rows = []
    for g, items in groups.items():
        os.makedirs(os.path.join(OUT, g), exist_ok=True)
        for name, frs in items:
            sheet(frs).save(os.path.join(OUT, g, name + '.png'))
            anim = [k for k in MS if k in name.split('_')][0]
            gif(os.path.join(OUT, 'gif', name + '.gif'), frs, MS[anim])
            rows.append((g, name, len(frs), frs[0].n))
    return rows


# ============================================================ VISTA PREVIA
COMBOS = [
    ('Mandíbulas + Aletas + Caparazón + Vejiga de tinta', dict(mouth='Mandibles', limbs='Fins', skin='Carapace', organ='InkSac')),
    ('Aguijón + Patas articuladas + Piel urticante + Glándula de choque',
     dict(mouth='Stinger', limbs='JointedLegs', skin='StingingSkin', organ='ShockGland')),
    ('Esporas + Aletas + Piel urticante', dict(mouth='Spores', limbs='Fins', skin='StingingSkin')),
]
SKIN_EDGE = {None: 6.0, 'Carapace': 8.5, 'StingingSkin': 7.0}
W = 64   # lienzo de montaje, centro (32, 32)


def compose(facing, f, skins, mouths, limbs, organs, mouth=None, limbs_=None, skin=None, organ=None, anim='Move'):
    out = Image.new('RGBA', (W, W), (0, 0, 0, 0))
    c = W // 2

    def put(im, cxy):
        out.alpha_composite(im, (int(c - cxy[0]), int(c - cxy[1])))

    def mouth_layer():
        if not mouth:
            return
        im = mouths[mouth]['Idle'][f].img()
        e = SKIN_EDGE[skin]
        if facing == 'side':
            put(im, (-e, 12))
        elif facing == 'down':
            im = im.rotate(-90, expand=True)      # apunta hacia abajo; unión arriba al centro
            put(im, (12, -e))
        else:
            im = im.rotate(90, expand=True)       # apunta hacia arriba; unión abajo al centro
            put(im, (12, 24 + e))

    if limbs_:
        put(limbs[limbs_][f'{anim}_{facing}'][f].img(), (16, 16))
    if facing == 'up':
        mouth_layer()
    if skin:
        put(skins[skin][f'{anim}_{facing}'][f].img(), (12, 12))
    else:
        put(PANIMS[f'{anim}_{facing}'][f].img(), (8, 8))
    if organ:
        put(organs[organ]['Idle'][f].img(), (8, 8))
    if facing != 'up':
        mouth_layer()
    return out


def preview(skins, mouths, limbs, organs, path):
    S = 4
    rows = [('Célula sola', {})]
    names = {'Mandibles': 'Mandíbulas', 'Stinger': 'Aguijón', 'Spores': 'Esporas', 'Fins': 'Aletas',
             'JointedLegs': 'Patas articuladas', 'Carapace': 'Caparazón', 'StingingSkin': 'Piel urticante',
             'InkSac': 'Vejiga de tinta', 'ShockGland': 'Glándula de choque'}
    for m in mouths:
        rows.append((names[m], dict(mouth=m)))
    for m in limbs:
        rows.append((names[m], dict(limbs_=m)))
    for m in ('Carapace', 'StingingSkin'):
        rows.append((names[m], dict(skin=m)))
    for m in organs:
        rows.append((names[m], dict(organ=m)))
    for label, combo in COMBOS:
        rows.append((label, {('limbs_' if k == 'limbs' else k): v for k, v in combo.items()}))
    cell = W * S
    gap = 6
    labw = 230
    width = labw + 3 * (4 * (cell + gap) + 16)
    height = 30 + len(rows) * (cell + gap)
    out = Image.new('RGBA', (width, height), BG)
    d = ImageDraw.Draw(out)
    for j, facing in enumerate(FACINGS):
        d.text((labw + j * (4 * (cell + gap) + 16) + 4, 8), f'Move {facing} (4 cuadros)', fill=C('#d6ff9a'))
    for i, (label, kw) in enumerate(rows):
        y = 30 + i * (cell + gap)
        words, line, lines = label.split(' '), '', []
        for w_ in words:
            if len(line) + len(w_) > 30:
                lines.append(line); line = ''
            line += w_ + ' '
        lines.append(line)
        for k, ln in enumerate(lines):
            d.text((8, y + cell // 2 - 8 * len(lines) + k * 14), ln.strip(), fill=C('#d6ff9a'))
        for j, facing in enumerate(FACINGS):
            for f in range(4):
                im = compose(facing, f, skins, mouths, limbs, organs, **kw)
                im = im.resize((cell, cell), Image.NEAREST)
                out.alpha_composite(im, (labw + j * (4 * (cell + gap) + 16) + f * (cell + gap), y))
    out.convert('RGB').save(path)


def preview_gifs(skins, mouths, limbs, organs):
    """GIF de cada criatura combinada, andando en las 3 direcciones."""
    for idx, (label, combo) in enumerate(COMBOS):
        kw = {('limbs_' if k == 'limbs' else k): v for k, v in combo.items()}
        frs = []
        for f in range(4):
            b = Image.new('RGBA', (56 * 3, 56), BG)
            for j, facing in enumerate(FACINGS):
                b.alpha_composite(compose(facing, f, skins, mouths, limbs, organs, **kw).crop((4, 4, 60, 60)), (j * 56, 0))
            frs.append(b.resize((56 * 3 * 5, 56 * 5), Image.NEAREST).convert('RGB'))
        frs[0].save(os.path.join(OUT, 'gif', f'Criatura_{idx + 1}.gif'), save_all=True, append_images=frs[1:],
                    duration=120, loop=0)


def main():
    skins, mouths, limbs, organs = build_skins(), build_mouths(), build_limbs(), build_organs()
    rows = save_all(skins, mouths, limbs, organs)
    preview(skins, mouths, limbs, organs, os.path.join(OUT, 'vista_previa_mutaciones.png'))
    preview_gifs(skins, mouths, limbs, organs)
    for r in rows:
        print(*r)


if __name__ == '__main__':
    main()
