"""Lepidodendron ancestral · subtanda 6m: las 3 paredes de raíces del pasillo (25 % de la vida del jefe cada una).

Intento 2 (aprobado el 2026-11-01): cada pared es UNA MASA SÓLIDA de raíces apretadas y trenzadas, un modelo completo
(no capas): se calcula un mapa de alturas (un terraplén + cordones de raíz trenzados que se pisan entre sí + nudos
leñosos) y se proyecta en picada con su volumen (luz arriba a la izquierda, pliegues oscuros entre raíces, cara sur
visible). Por los bordes la masa baja hasta el suelo y su color se funde con él (tierra removida, raicillas); por el
sur los cordones se hunden en la tierra; por el norte se apoya contra la pared de arriba con una sombra suave.
Los puntos débiles son nudos leñosos que abultan la propia masa; la luz morada sale solo por sus grietas (capa aparte).

Estados: 4 de daño (roturas con astillas y grietas, que esquivan los nudos), carbonizada (el fuego avanza por la
pared), nudos (latido, golpe, revientan y quedan como herida), retirada bajo tierra (o desmoronamiento en ceniza si
estaba carbonizada) y la cicatriz que queda.

Uso: python generar_paredes_lepidodendron.py [muestra] [piezas] [escenas]
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import generar_lepidodendron as G
from generar_lepidodendron import *                    # noqa: F401,F403
from generar_lepidodendron import _h
from PIL import Image, ImageDraw

WALLS_DIR = os.path.join(HERE, 'Walls')
CORR_H = 18 * U                                        # alto del pasillo (px)
TOPO = 4                                               # la masa se apoya contra la pared de arriba
BOTO = 18                                              # lo que se hunde por el sur
K = .72                                                # altura (px) → desplazamiento hacia arriba en pantalla
FRINGE = 26                                            # franja de suelo removido a cada lado
SM = tram('Smoke')
FLOOR = SM['floor']
# raíces del mismo árbol: la paleta del tronco y del pie (oliva grisáceo)
ROOTW = [C(c) for c in ('#0e100e', '#161a16', '#252b24', '#38402f', '#525a40', '#767c58', '#959a72')]
GREENW = [C(c) for c in ('#0e120d', '#182017', '#263222', '#36462c', '#4e6238', '#6c8048', '#8ea062')]
DARKW = [C(c) for c in ('#0b0c0a', '#121410', '#1d221b', '#2c3326', '#3e4632', '#58603f', '#747a56')]
CHARW = [C(c) for c in ('#0b090a', '#141114', '#1e191b', '#2a2324', '#382e2c', '#4c3e38', '#6a5848')]
WOOD = [C(c) for c in ('#4a3c2a', '#6e5a3e', '#968060', '#bca684')]                  # madera de dentro (astillas)
ASH = [C(c) for c in ('#242022', '#322e30', '#433d3f', '#575052')]               # ceniza (oscura: se pisa, no es un muro)
EMB = [C(c) for c in EMBER]
CRACK = C('#140c1c')
HOLE = C('#100a12')
SAP = [C(c) for c in STROB]
GLOOMC = C('#0e1210')

SPEC = {
    1: dict(th=3.0, hmax=24, cords=9, diag=2, R=(6, 8), knots=2, kr=11, pal=GREENW, extra='wet', seed=11),
    2: dict(th=3.5, hmax=30, cords=11, diag=3, R=(7, 10), knots=3, kr=12, pal=ROOTW, extra=None, seed=12),
    3: dict(th=4.0, hmax=38, cords=13, diag=4, R=(8, 12), knots=4, kr=14, pal=DARKW, extra='old', seed=13),
}
BREAKS = (0, 3, 6, 10)                                 # roturas por estado de daño
CRACKS = (0, 4, 8, 13)                                 # grietas por estado de daño

def cell(n):
    sp = SPEC[n]
    W = int(sp['th'] * U) + 2 * FRINGE + 24
    lift = int(sp['hmax'] * 1.6 * K) + 6
    H = TOPO + CORR_H + BOTO + lift + 6                # 6 px de margen abajo
    return W, H, W // 2, H - BOTO - 6, lift               # ancho, alto, pivote x, pivote y (borde sur del pasillo), margen

class Mass:
    """Mapa de alturas de la pared n en coordenadas de suelo (gx, gy); gy = 0 arriba de la celda.
    dmg 0..3: estado de daño; hfac 0..1: cuánto queda fuera del suelo (retirada / desmoronamiento);
    dead: nudos reventados (quedan como herida)."""
    def __init__(s, n, dmg=0, hfac=1.0, dead=(), shrink=True):
        s.n, s.sp = n, SPEC[n]
        sp = s.sp
        s.dmg, s.hfac, s.dead = dmg, hfac, set(dead)
        s.nar = (.35 + .65 * hfac) if shrink else 1.0     # al retirarse también se estrecha
        s.W, s.H, s.px, s.py, s.lift = cell(n)
        s.hw = sp['th'] * U / 2
        s.y0, s.y1 = s.lift, s.py + BOTO
        rnd = random.Random(sp['seed'])
        s.cords = []
        nc = sp['cords']
        for k in range(nc):                            # cordones trenzados de norte a sur (se pisan entre sí)
            off = (k / (nc - 1) - .5) * 2 * s.hw * .78
            s.cords.append(dict(kind='braid', off=off, A=rnd.uniform(.55, 1.0) * s.hw * .42, P=rnd.uniform(60, 110),
                                ph=rnd.uniform(0, 6.28), R=rnd.uniform(*sp['R']), lift=rnd.uniform(0, 4)))
        for k in range(sp['diag']):                    # cordones en diagonal por encima
            ya = rnd.uniform(s.y0 + 20, s.y1 - 120)
            s.cords.append(dict(kind='diag', ya=ya, yb=ya + rnd.uniform(70, 130), xa=rnd.uniform(-.9, .9) * s.hw,
                                xb=rnd.uniform(-.9, .9) * s.hw, R=rnd.uniform(*sp['R']) * 1.1, lift=rnd.uniform(4, 8)))
        kn = sp['knots']                               # nudos leñosos (puntos débiles)
        s.knots = [(s.px + rnd.uniform(-.3, .3) * s.hw, s.y0 + TOPO + (CORR_H - 20) * (i + .5) / kn + rnd.uniform(-10, 10), sp['kr'])
                   for i in range(kn)]
        # daño: roturas y grietas fijas (siempre las mismas, se van sumando), lejos de los nudos
        dr = random.Random(sp['seed'] + 100)
        far = lambda x, y, m_: all(math.hypot(x - kx, y - ky) > kr * m_ for (kx, ky, kr) in s.knots)
        s.breaks = []
        while len(s.breaks) < BREAKS[-1]:
            x, y = s.px + dr.uniform(-.75, .75) * s.hw, dr.uniform(s.y0 + 20, s.py - 40)
            if far(x, y, 2.2): s.breaks.append((x, y, dr.uniform(7, 12)))
        s.cracks = []
        while len(s.cracks) < CRACKS[-1]:
            x, y = s.px + dr.uniform(-.7, .7) * s.hw, dr.uniform(s.y0 + 16, s.py - 30)
            if not far(x, y, 2.0): continue
            a = dr.uniform(0, 6.28); pts = [(x, y)]
            for j in range(3):
                a += dr.uniform(-.7, .7); L = dr.uniform(5, 9)
                pts.append((pts[-1][0] + math.cos(a) * L, pts[-1][1] + math.sin(a) * L * .8))
            s.cracks.append(pts)
        s.cache = {}

    def taper(s, gy):
        t0 = s.py - 52
        if gy > t0:
            t = max(0., (s.y1 - gy) / (s.y1 - t0))
            return t * t * (3 - 2 * t)
        return 1.

    def base(s, gx, gy):
        u = (gx - s.px) / (s.hw * s.nar)
        if abs(u) >= 1: return 0.
        return s.sp['hmax'] * .5 * (1 - u * u) ** .7 * s.taper(gy)

    def cord_xc(s, c, gy):
        if c['kind'] == 'braid':
            return s.px + c['off'] + c['A'] * math.sin(2 * math.pi * gy / c['P'] + c['ph'])
        t = (gy - c['ya']) / (c['yb'] - c['ya'])
        if not 0 <= t <= 1: return None
        return s.px + c['xa'] + (c['xb'] - c['xa']) * t

    def h(s, gx, gy):
        """(altura, id del cordón de arriba, marca) — marca: None, 'crease', 'crack', 'splinter', ('knot', d, grieta),
        ('hole', d)."""
        key = (gx, gy)
        if key in s.cache: return s.cache[key]
        tp = s.taper(gy)
        b = s.base(gx, gy)
        best, second, bid = b, 0., -1
        cut = 0.
        for (bx, by, br) in s.breaks[:BREAKS[s.dmg]]:  # roturas: los cordones se rompen y se ve la madera
            d = math.hypot(gx - bx, (gy - by) * 1.2) / br
            if d < 1: cut = max(cut, 1 - d)
        for i, c in enumerate(s.cords):
            xc = s.cord_xc(c, gy)
            if xc is None: continue
            R = c['R'] * (.6 + .4 * tp) * ((.3 + .7 * s.hfac) if s.nar < 1 else 1)
            xc = s.px + (xc - s.px) * s.nar
            d = abs(gx - xc)
            if d >= R: continue
            uc = (xc - s.px) / s.hw
            under = s.sp['hmax'] * .45 * max(0, 1 - uc * uc) ** .7 * tp
            prof = math.sqrt(1 - (d / R) ** 2)
            along = gy + (c.get('ph', 0) * 7)
            wr = 1 - .045 * (math.sin(along * 2 * math.pi / 9) ** 8)  # arrugas de la corteza (suaves)
            v = under + (c['lift'] * tp + R * .85 * prof * wr * (.5 + .5 * tp)) * (1 - .75 * cut ** .5)
            if v > best: second, best, bid = best, v, i
            elif v > second: second = v
        mark = 'crease' if (bid >= 0 and best - second < 1.6 and second > b + 1) else None
        if cut >= .3: mark = 'gap'                       # dentro de la rotura: hueco oscuro
        elif cut > .08 and bid >= 0 and _h(gx, gy, 31) < .45: mark = 'splinter'   # astillas irregulares en el borde
        for pts in s.cracks[:CRACKS[s.dmg]]:           # grietas
            for (a, b_) in zip(pts, pts[1:]):
                vx, vy = b_[0] - a[0], b_[1] - a[1]
                t = max(0, min(1, ((gx - a[0]) * vx + (gy - a[1]) * vy) / (vx * vx + vy * vy)))
                dd_ = math.hypot(gx - a[0] - vx * t, gy - a[1] - vy * t)
                if dd_ < 1.0: mark = 'crack'
                elif dd_ < 1.9 and mark is None and gx - a[0] - vx * t < 0: mark = 'crackhi'   # canto con luz
        for ki, (kx, ky, kr) in enumerate(s.knots):
            d = math.hypot(gx - kx, (gy - ky) * .8) / (kr * 1.25)        # más largo que ancho: sigue a los cordones
            if d < 1:
                v = s.base(kx, ky) + s.sp['hmax'] * .42 + kr * .42 * (1 - d * d) ** .8
                if v > best - (2.5 if d < .55 else -1):  # por los bordes, los cordones pasan por encima del bulto
                    best = max(best, v)
                    if ki in s.dead and d < .62:        # reventado: un hueco
                        best -= kr * .35 * (1 - d / .62)
                        mark = ('hole', d / .62)
                        continue
                    ang = math.atan2(gy - ky, gx - kx)
                    cr = False
                    if .12 < d < .85:                   # 5 grietas finas que se abren desde el centro
                        for j in range(5):
                            a0 = j * 1.2566 + ki + .3 * math.sin(d * 9 + j)
                            da = abs((ang - a0 + math.pi) % (2 * math.pi) - math.pi)
                            if da * d * kr * 1.25 < .75 * (1 - d * .6): cr = True
                    if d < .14: cr = True               # el corazón, rajado
                    mark = ('knot', d, cr)
        out = (best * s.hfac, bid, mark)
        s.cache[key] = out
        return out

def turned_earth(m, progress, burnt=False, seed=0):
    """Tierra revuelta sobre el suelo de la sala (que se sigue viendo entre medias): terrones sombreados (luz arriba,
    sombra debajo), hoyos alargados donde estaban las raíces, algún muñón de raíz y poca savia. progress 0..1: a 0
    solo hay terrones a los lados de la pared; al retirarse van apareciendo también donde estaba la masa (a 1, la
    cicatriz entera). burnt: con algo de ceniza."""
    rnd = random.Random(m.sp['seed'] * 13 + seed)
    cv = Canvas(m.W, m.H)
    hw = m.hw
    ya, yb = m.y0 + 4, m.y1 - 6
    def edge(y):                                        # ancho de la cicatriz (irregular, más estrecha al sur)
        t = (y - ya) / (yb - ya)
        return hw * (1.0 + .1 * math.sin(y / 13 + m.n) + .05 * math.sin(y / 5)) * (1 - max(0, t - .8) * 2.2)
    def inside(x, y, full):
        e = edge(y)
        u = abs(x - m.px) / max(e, 1)
        return u < 1 + (FRINGE / hw) * .9 if full else 1 <= u < 1 + (FRINGE / hw) * .9
    items = []
    for k in range(int((yb - ya) * hw * .032)):         # terrones en la cicatriz (aparecen al retirarse); entre ellos, el suelo
        x, y = m.px + rnd.uniform(-1, 1) * hw, rnd.uniform(ya, yb)
        if inside(x, y, True) and abs(x - m.px) < edge(y): items.append(('clod', x, y, rnd.random(), rnd.uniform(2.6, 5.4)))
    for k in range(int((yb - ya) * .5)):                # terrones a los lados (siempre)
        sg = rnd.choice((-1, 1))
        x, y = m.px + sg * (hw + rnd.uniform(0, FRINGE * .85)), rnd.uniform(ya, yb)
        items.append(('clod', x, y, -1, rnd.uniform(1.6, 3.6) * (1 - abs(x - m.px - sg * hw) / (FRINGE * 1.2))))
    for k in range(int((yb - ya) / 22)):                # hoyos donde estaban las raíces
        x, y = m.px + rnd.uniform(-.7, .7) * hw, rnd.uniform(ya + 8, yb - 16)
        items.append(('hole', x, y, rnd.random() * .8, rnd.uniform(2, 3.2)))
    for k in range(6):                                  # muñones de raíz
        x, y = m.px + rnd.uniform(-.8, .8) * hw, rnd.uniform(ya + 10, yb - 10)
        items.append(('stub', x, y, rnd.random() * .9, 0))
    for k in range(4):                                  # gotas de savia
        x, y = m.px + rnd.uniform(-.6, .6) * hw, rnd.uniform(ya + 10, yb - 10)
        items.append(('sap', x, y, .3 + rnd.random() * .6, 0))
    soilp = [SOIL[0], SOIL[1], SOIL[2], SOIL[3]]
    for (kind, x, y, rank, r) in sorted(items, key=lambda it: it[2]):
        if rank > progress: continue                    # rank -1: los de los lados (siempre)
        if kind == 'clod':
            ry = r * .62
            for yy in range(int(y - ry - 1), int(y + ry + 2)):
                for xx in range(int(x - r - 1), int(x + r + 2)):
                    d = ((xx - x) / r) ** 2 + ((yy - y) / ry) ** 2
                    if d > 1: continue
                    lit = (xx - x) * -.5 + (yy - y) * -1.0
                    c = soilp[3] if lit > ry * .35 else soilp[2] if lit > -ry * .4 else soilp[1]
                    if burnt and _h(int(xx), int(yy), 5) < .3: c = ASH[2] if lit > 0 else ASH[1]
                    cv.put(xx, yy, c)
            for xx in range(int(x - r * .7), int(x + r * .7) + 1):     # sombra debajo del terrón
                if (xx, int(y + ry + 1)) not in cv.px: cv.put(xx, y + ry + 1, (10, 12, 10, 150))
        elif kind == 'hole':
            ry = r * 2.6
            for yy in range(int(y - ry), int(y + ry) + 1):
                for xx in range(int(x - r), int(x + r) + 1):
                    d = ((xx - x) / r) ** 2 + ((yy - y) / ry) ** 2
                    if d > 1: continue
                    c = (14, 13, 10, 255) if d < .5 else SOIL[0] if (yy - y) < ry * .3 else SOIL[2]   # el borde de abajo, con luz
                    cv.put(xx, yy, c)
        elif kind == 'stub':
            for j in range(4):
                cv.put(x, y - j, ROOTW[4] if j == 3 else ROOTW[3]); cv.put(x + 1, y - j, ROOTW[2])
            cv.put(x, y - 4, WOOD[2])
        elif kind == 'sap':
            cv.put(x, y, SAP[1]); cv.put(x + 1, y, SAP[2]); cv.put(x, y + 1, SAP[0])
    return cv


def render(n, glow_frames=(), dmg=0, burnt=False, hfac=1.0, dead=(), seed_fx=0, ashpile=False):
    """Devuelve (pared, [capas de brillo de las grietas de los nudos por intensidad], masa)."""
    m = Mass(n, dmg, hfac, dead, shrink=not burnt)
    sp = m.sp
    pal = CHARW if burnt else sp['pal']
    W, H = m.W, m.H
    img = Canvas(W, H)
    owner = {}
    shadow = Canvas(W, H)
    rnd = random.Random(sp['seed'] + 5 + seed_fx)
    Lx, Ly, Lz = -.5, -.55, .67
    sink = 1 - hfac                                     # 0 → 1 mientras se retira
    if ashpile: sink = 1.0                              # el montón final: todo ceniza
    for gx in range(W):
        prev = None
        for gy in range(int(m.y0), int(m.y1) + 2):
            hh, bid, mark = m.h(gx, gy)
            u = abs(gx - m.px) / m.hw
            if hh <= .2:
                prev = None
                continue
            sy = int(round(gy - hh * K))
            hx = m.h(gx + 1, gy)[0] - m.h(gx - 1, gy)[0]
            hy = m.h(gx, gy + 1)[0] - m.h(gx, gy - 1)[0]
            nx, ny, nz = -hx / 2, -hy / 2, 1.
            ln = math.sqrt(nx * nx + ny * ny + nz * nz)
            lam = (nx * Lx + ny * Ly + nz * Lz) / ln
            t = 1 + lam * 4.2
            crack_px = False
            if mark == 'crease': t -= 2.2
            elif mark == 'crack': t = -5
            elif mark == 'gap': t = .3
            elif mark == 'crackhi': t += 1
            elif isinstance(mark, tuple) and mark[0] == 'knot':
                _, dk, cr = mark
                t += 1.1 - dk * .7 + (.8 if math.sin(dk * 13) > .55 else 0)      # anillos de la madera
                if cr: t = -9; crack_px = True
            ti = max(0, min(6, int(t)))
            c = pal[ti]
            if mark == 'crack': c = EMB[1] if burnt and _h(gx, gy, 2) < .35 else pal[0]
            if mark == 'gap': c = (EMB[1] if _h(gx, gy, 12) < .2 else pal[0]) if burnt else pal[0]
            if mark == 'splinter' and not burnt: c = WOOD[max(0, min(3, int(1 + lam * 2.5)))]
            if mark == 'splinter' and burnt: c = EMB[2] if _h(gx, gy, 8) < .5 else pal[2]
            if mark == 'crease' and burnt and _h(gx, gy, 5) < .04: c = EMB[1]       # alguna ascua en los pliegues
            if isinstance(mark, tuple) and mark[0] == 'hole':
                c = HOLE if mark[1] < .8 else (SAP[1] if _h(gx, gy, 7) < .35 else pal[1])   # hueco con restos de savia
            if t == -9: c = CRACK
            if hh < 3.5 and dither(1 - hh / 3.5, gx, gy):
                c = (ASH[1] if burnt else FLOOR[1]) if ti < 3 else (ASH[2] if burnt else FLOOR[3])
            if gy < m.y0 + 22:
                c = blend(c, GLOOMC, .45 * (m.y0 + 22 - gy) / 22)
            if not burnt and sp['extra'] == 'wet' and ti >= 5 and _h(gx, gy, 9) < .18: c = C('#a8c8bc')
            if not burnt and sp['extra'] == 'old' and lam > .5 and math.sin(gx / 7 + math.sin(gy / 11) * 2) + math.sin(gy / 9) > .9 and mark is None:
                c = MOSS[2 if lam > .7 else 1]
            if burnt and sink > 0:                      # se deshace en ceniza, por manchas que crecen
                pn = .5 + .25 * math.sin(gx / 6.3 + math.sin(gy / 9) * 2) + .25 * math.sin(gy / 7.1 + math.cos(gx / 11) * 2)
                if pn < sink * 1.15: c = ASH[max(0, min(3, int(1 + lam * 2.4 + (_h(gx, gy, 17) - .5) * 1.6)))]
                if pn < sink * 1.15 and _h(gx, gy, 13) < .008: c = EMB[2]
            if prev is not None and sy > prev + 1:      # cara sur visible
                for yy in range(prev + 1, sy):
                    img.put(gx, yy, pal[max(0, ti - 2)]); owner[(gx, yy)] = None
            img.put(gx, sy, c)
            owner[(gx, sy)] = (mark[1],) if crack_px else None
            prev = sy
            if hh > 4 and _h(gx, gy, 3) < .5:
                shadow.put(gx + int(hh * .45), gy + int(hh * .2), C('#0a0e0c', 115))
    if 0 < sink < .99 and not ashpile:                  # tierra (o ceniza) que salta a los lados al moverse
        for k in range(int(70 * math.sin(math.pi * min(1, sink * 1.1)))):
            y = rnd.uniform(m.y0 + 10, m.py - 10)
            x = m.px + rnd.choice((-1, 1)) * (m.hw + rnd.uniform(0, 18))
            yy = y - rnd.uniform(0, 26) * (1 - sink)
            img.put(x, yy, (ASH[rnd.randrange(1, 4)] if burnt else SOIL[rnd.randrange(1, 4)]))
            if burnt and rnd.random() < .25: img.put(x + 1, yy - 2, EMB[3])
    for (x, y) in list(shadow.px):
        if (x, y) in img.px: del shadow.px[(x, y)]
    img.outline(min_alpha=250)
    soft = (SOIL[1], SOIL[2], FLOOR[1], FLOOR[3], ASH[1], ASH[2], SOIL[0], SAP[1], ROOTW[2])
    for (x, y), col in list(img.px.items()):            # sin contorno donde la masa toca el suelo (se funde)
        if col == OUTL:
            ny_ = [img.px.get((x + dx, y + dy)) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))]
            if any(c_ in soft for c_ in ny_ if c_): del img.px[(x, y)]
    base = shadow.img().convert('RGBA')
    base.alpha_composite(turned_earth(m, 0.0 if burnt else min(1., sink * 1.05), burnt).img().convert('RGBA'))
    base.alpha_composite(img.img().convert('RGBA'))
    glows = []
    for g in glow_frames:                               # la luz morada sale por las grietas de los nudos
        gl = Canvas(W, H)
        for (x, y), o in owner.items():
            if not o or (x, y) not in img.px or img.px[(x, y)] != CRACK: continue
            core = o[0] < .35
            c = SAP[4] if (g > .75 and core) else SAP[3] if (g > .45 or core) else SAP[2]
            if g >= 1.0: c = C('#f4e8ff') if core else SAP[4]
            gl.put(x, y, c)
            if g > .6:
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    q = (x + dx, y + dy)
                    if q not in gl.px and q in img.px and img.px[q] != CRACK: gl.put(q[0], q[1], (112, 70, 160, int(120 * min(1, g))))
        glows.append(gl.img().convert('RGBA'))
    return base, glows, m

def knot_box(m, i):
    """Recorte (en la celda de la pared) alrededor del nudo i: (x0, y0, lado)."""
    kx, ky, kr = m.knots[i]
    hk = m.h(int(kx), int(ky))[0]
    S = int(kr * 2.8) + 8
    return int(kx - S / 2), int(ky - hk * K - S / 2), S

# =================================================================================================
# Efectos sueltos
# =================================================================================================
def fx_knot_burst(k, n=6, S=64):
    """El nudo revienta: destello morado, salpicadura de savia y astillas que caen (64×64, centro = el nudo)."""
    cv = Canvas(S, S)
    c = S / 2
    rnd = random.Random(3)
    if k == 0:
        for y in range(S):
            for x in range(S):
                d = math.hypot(x - c, (y - c) * 1.2)
                if d < 9: cv.put(x, y, C('#f4e8ff') if d < 4 else SAP[4])
                elif d < 13 and dither(.5, x, y): cv.put(x, y, SAP[3])
        return cv
    t = k / (n - 1)
    for j in range(22):                                 # gotas de savia
        a = rnd.uniform(0, 6.28); v = rnd.uniform(8, 26)
        x = c + math.cos(a) * v * t
        y = c + math.sin(a) * v * t * .8 + 30 * t * t   # caen
        if t > .8 and j % 2: continue
        col = SAP[3] if j % 3 else SAP[4]
        cv.put(x, y, col); cv.put(x + 1, y, SAP[2])
        if j % 4 == 0 and t < .6: cv.put(x - 1, y + 1, SAP[2])
    for j in range(8):                                  # astillas de madera
        a = rnd.uniform(0, 6.28); v = rnd.uniform(10, 22)
        x = c + math.cos(a) * v * t; y = c + math.sin(a) * v * t * .8 + 26 * t * t
        cv.put(x, y, WOOD[2]); cv.put(x + math.cos(a), y + math.sin(a), WOOD[1])
    if k == 1:
        for y in range(S):
            for x in range(S):
                if 9 < math.hypot(x - c, (y - c) * 1.2) < 12 and dither(.6, x, y): cv.put(x, y, SAP[2])
    return cv

def A_fire():
    """Las llamas aprobadas del piso 3 (floor3-mutations/Effects/Fx_GroundFire.png, 6 cuadros de 32×24)."""
    path = os.path.join(MT, 'floor3-mutations', 'Effects', 'Fx_GroundFire.png')
    return [A.frame(path, i, 32) for i in range(6)]

def fx_root_smolder(k, n=6):
    """Rescoldo sobre lo carbonizado (24×24): chispas que suben y un hilo de humo, en bucle."""
    cv = Canvas(24, 24)
    rnd = random.Random(5)
    for j in range(5):
        x0 = rnd.uniform(4, 20); ph = rnd.uniform(0, 1)
        t = (k / n + ph) % 1
        cv.put(x0 + math.sin(t * 6 + j) * 1.5, 22 - t * 18, EMB[3] if t < .5 else EMB[2])
    for j in range(8):                                  # humo
        t = (k / n + j / 8) % 1
        x = 12 + math.sin(t * 5) * 3; y = 20 - t * 20
        cv.put(x, y, (120, 112, 110, int(110 * (1 - t))))
        cv.put(x + 1, y, (100, 94, 92, int(70 * (1 - t))))
    return cv

# =================================================================================================
# Hojas
# =================================================================================================
def build_wall(n):
    """Todas las hojas de la pared n: {nombre: [cuadros]} + datos de los nudos."""
    out = {}
    glows = None
    for d in range(4):
        b, gl, m = render(n, (.3, .5, .75, .5, 1.0, .7) if d == 0 else (), dmg=d)
        out[f'Lepido_Wall{n}_D{d}'] = [b]
        if d == 0: glows, m0 = gl, m
        out[f'Lepido_Wall{n}_Charred_D{d}'] = [render(n, (), dmg=d, burnt=True)[0]]
        a = b.split()[3]                                # golpe: silueta blanca y luego roja (solo lo opaco)
        a = a.point(lambda v: 255 if v == 255 else 0)
        white = Image.new('RGBA', b.size, (255, 255, 255, 255)); white.putalpha(a)
        red = Image.new('RGBA', b.size, C(RED4[1])); red.putalpha(a)
        out[f'Lepido_Wall{n}_Hurt_D{d}'] = [white, red]
    knots = []
    for i in range(SPEC[n]['knots']):
        x0, y0, S = knot_box(m0, i)
        box = (x0, y0, x0 + S, y0 + S)
        out[f'Lepido_Wall{n}_Knot{i}_Idle'] = [g.crop(box) for g in glows[:4]]
        out[f'Lepido_Wall{n}_Knot{i}_Hit'] = [g.crop(box) for g in glows[4:6]]
        out[f'Lepido_Wall{n}_Knot{i}_Wound'] = [render(n, (), dmg=0, dead=(i,))[0].crop(box)]
        out[f'Lepido_Wall{n}_Knot{i}_WoundCharred'] = [render(n, (), dmg=0, burnt=True, dead=(i,))[0].crop(box)]
        knots.append((x0, y0, S))
    allk = tuple(range(SPEC[n]['knots']))
    fr = [1.0, .86, .7, .54, .38, .24, .12, .04]
    frc = [1.0, .82, .64, .48, .34, .24, .16, .12]   # la ceniza no se hunde del todo: queda un montón
    out[f'Lepido_Wall{n}_Retract'] = [render(n, (), dmg=3, hfac=f, dead=allk, seed_fx=j)[0] for j, f in enumerate(fr)]
    out[f'Lepido_Wall{n}_Scar'] = [render(n, (), dmg=3, hfac=0.0, dead=allk)[0]]
    out[f'Lepido_Wall{n}_Crumble'] = [render(n, (), dmg=3, burnt=True, hfac=f, dead=allk, seed_fx=j)[0] for j, f in enumerate(frc)]
    out[f'Lepido_Wall{n}_AshScar'] = [render(n, (), dmg=3, burnt=True, hfac=0.1, dead=allk, seed_fx=9, ashpile=True)[0]]
    return out, knots, m0

def piezas():
    os.makedirs(WALLS_DIR, exist_ok=True)
    info = {}
    for n in (1, 2, 3):
        out, knots, m = build_wall(n)
        for name, frames in out.items(): save_sheet(os.path.join(WALLS_DIR, name + '.png'), frames)
        info[n] = (knots, (m.W, m.H, m.px, m.py))
        print(f'   pared {n}: {len(out)} hojas · celda {m.W}×{m.H} · pivote ({m.px}, {m.py}) · nudos {knots}')
    save_sheet(os.path.join(WALLS_DIR, 'Fx_KnotBurst.png'), [fx_knot_burst(k) for k in range(6)])
    save_sheet(os.path.join(WALLS_DIR, 'Fx_RootSmolder.png'), [fx_root_smolder(k) for k in range(6)])
    return info

# =================================================================================================
# Láminas y escenas
# =================================================================================================
def _corridor(Wt=22):
    im = A.room(A.rect_room(Wt, 24, (0, 3, Wt - 1, 20)))
    props = os.path.join(MT, 'floor3-environment', 'Smoke', 'props')
    sac = A.frame(os.path.join(props, 'Methane_Sac_Idle.png'), 0, 24)
    fern = A.frame(os.path.join(props, 'Fern_Leafy_Sway.png'), 0, 84)
    for (x, y, spr) in ((6.8, 7.5, sac), (5.8, 18.5, fern), (7.0, 14, sac)):
        im.alpha_composite(spr, (int(x * U - spr.width / 2), int(y * U - spr.height + 4)))
    return im

def _place(im, spr, m, dx=0, dy=0):
    ox, oy = int(13 * U - m.px) + dx, int(21 * U - m.py) + dy
    im.alpha_composite(spr, (ox, oy))
    return ox, oy

def lamina_muestra():
    segs, dets = [], []
    for n in (1, 2, 3):
        im = _corridor()
        base, glows, m = render(n, (.85,))
        ox, oy = _place(im, base, m); im.alpha_composite(glows[0], (ox, oy))
        im.alpha_composite(A.frame(FROG, 0, 32), (int(3 * U), int(11 * U)))
        segs.append(im)
        x0, y0, S = knot_box(m, 0)
        dets.append(im.crop((ox + x0 - 30, oy + y0 - 16, ox + x0 + S + 30, oy + y0 + S + 16)))
    s, z = 2, 4
    Wd = sum(sg.width for sg in segs) + 32
    out = Image.new('RGBA', (Wd * s, segs[0].height * s + 30 + dets[0].height * z + 40), (16, 18, 16, 255))
    d = ImageDraw.Draw(out)
    x = 0
    for i, sg in enumerate(segs):
        out.alpha_composite(sg.resize((sg.width * s, sg.height * s), Image.NEAREST), (x, 30))
        d.text((x + 8, 6), ['pared 1 (3 u): joven y húmeda · 2 nudos', 'pared 2 (3,5 u) · 3 nudos', 'pared 3 (4 u): vieja, con musgo · 4 nudos'][i],
               fill=(120, 230, 230), font=A.font(16))
        out.alpha_composite(dets[i].resize((dets[i].width * z, dets[i].height * z), Image.NEAREST), (x + 20, segs[0].height * s + 60))
        d.text((x + 20, segs[0].height * s + 38), 'nudo ×4', fill=(120, 230, 230), font=A.font(15))
        x += (sg.width + 16) * s
    G._save(out, 'Lepidodendron ancestral · 6m muestra (intento 2): cada pared, una masa sólida de raíces trenzadas (×2; nudos ×4)',
            ['Modelo completo por mapa de alturas: terraplén + cordones trenzados que se pisan, pliegues oscuros, cara sur visible; por los lados baja al suelo',
             'y se funde con él (tierra removida, raicillas); por el sur se hunde; por el norte se apoya en la pared. Nudos leñosos con la luz morada en las grietas.'],
            'vista_previa_6m_muestra.png')

def _sheet(name):
    return Image.open(os.path.join(WALLS_DIR, name + '.png')).convert('RGBA')

def _frames(name, w):
    im = _sheet(name)
    return [im.crop((i * w, 0, i * w + w, im.height)) for i in range(im.width // w)]

def lamina_estados():
    """Pared 2: los 4 estados de daño, normal y carbonizada, y la retirada / el desmoronamiento (×1)."""
    n = 2
    W, H, px, py, lift = cell(n)
    rows = [('daño 0-3', [_sheet(f'Lepido_Wall{n}_D{d}') for d in range(4)]),
            ('carbonizada 0-3', [_sheet(f'Lepido_Wall{n}_Charred_D{d}') for d in range(4)]),
            ('se retira bajo tierra', _frames(f'Lepido_Wall{n}_Retract', W)[::2] + [_sheet(f'Lepido_Wall{n}_Scar')]),
            ('carbonizada: se desmorona', _frames(f'Lepido_Wall{n}_Crumble', W)[::2] + [_sheet(f'Lepido_Wall{n}_AshScar')])]
    cols = max(len(r[1]) for r in rows)
    out = Image.new('RGBA', (180 + cols * (W + 10), len(rows) * (H + 10)), C('#2a2d25'))
    d = ImageDraw.Draw(out)
    for r, (lab, fs) in enumerate(rows):
        d.text((8, r * (H + 10) + H // 2), lab, fill=(120, 230, 230), font=A.font(15))
        for i, f in enumerate(fs): out.alpha_composite(f, (180 + i * (W + 10), r * (H + 10)))
    s = 2
    big = out.resize((out.width * s, out.height * s), Image.NEAREST)
    G._save(big, 'Lepidodendron ancestral · 6m estados de la pared 2 (×2): daño, carbonizada, retirada y desmoronamiento',
            ['Las roturas y grietas esquivan los nudos (así sus parches valen en cualquier estado). Retirada y desmoronamiento: 8 cuadros (aquí 1 de cada 2) + la cicatriz.'],
            'vista_previa_6m_estados.png')

def escenas():
    n = 2
    W, H, px, py, lift = cell(n)
    m = Mass(n)
    os.makedirs(os.path.join(GIF, 'escenas'), exist_ok=True)
    frog = A.frame(FROG, 0, 32)
    def save(frames, ms, name):
        out = [f.resize((f.width * 2, f.height * 2), Image.NEAREST).convert('RGB') for f in frames]
        out[0].save(os.path.join(GIF, 'escenas', name), save_all=True, append_images=out[1:], duration=ms, loop=0)
    room = _corridor()
    room.alpha_composite(frog, (int(5 * U), int(11 * U)))
    crop = lambda im: im.crop((4 * U, 0, 22 * U, im.height))
    # 1) nudo: late, recibe golpes, revienta y queda la herida
    D0 = _sheet(f'Lepido_Wall{n}_D0')
    x0, y0, S = knot_box(m, 1)
    idle = _frames(f'Lepido_Wall{n}_Knot1_Idle', S); hit = _frames(f'Lepido_Wall{n}_Knot1_Hit', S)
    wound = _sheet(f'Lepido_Wall{n}_Knot1_Wound'); burst = _frames('Fx_KnotBurst', 64)
    seq = [('i', k % 4) for k in range(6)] + [('h', 0), ('h', 1), ('i', 0), ('i', 1), ('h', 0), ('h', 1), ('i', 2)] + [('b', k) for k in range(6)] + [('w', 0)] * 6
    frames = []
    for (st, k) in seq:
        im = room.copy(); ox, oy = _place(im, D0, m)
        if st == 'i': im.alpha_composite(idle[k], (ox + x0, oy + y0))
        elif st == 'h': im.alpha_composite(hit[k], (ox + x0, oy + y0))
        else: im.alpha_composite(wound, (ox + x0, oy + y0))
        if st == 'b': im.alpha_composite(burst[k], (ox + x0 + S // 2 - 32, oy + y0 + S // 2 - 32))
        frames.append(crop(im))
    save(frames, [160] * 6 + [60, 60, 120, 120, 60, 60, 120] + [70] * 6 + [200] * 6, 'Lepido_pared_nudo.gif')
    keys = [frames[i] for i in (2, 6, 13, 16, 24)]
    # 2) el fuego avanza (prende en el centro, ~3 s), deja la pared carbonizada con rescoldos
    D1, C1 = _sheet(f'Lepido_Wall{n}_D1'), _sheet(f'Lepido_Wall{n}_Charred_D1')
    fire = A_fire(); smol = _frames('Fx_RootSmolder', 24)        # llamas: Fx_GroundFire del piso 3 (aprobado)
    yi = py - CORR_H * .5
    frames2 = []
    for t in range(34):
        im = room.copy()
        r = t * 9                                       # px de pantalla que avanza el frente por cuadro (~3 u/s)
        mix = D1.copy()
        mask = Image.new('L', D1.size, 0); md = ImageDraw.Draw(mask)
        md.rectangle([0, yi - r, D1.width, yi + r], fill=255)
        mix.paste(C1, (0, 0), mask)
        ox, oy = _place(im, mix, m)
        rr = random.Random(t)
        for yy in (yi - r, yi + r):                     # llamas en el frente
            if lift < yy < py:
                for j in range(3): im.alpha_composite(fire[(t + j) % 6], (int(ox + px - 40 + j * 20 + rr.randint(-3, 3)), int(oy + yy - 24 + 6 + rr.randint(-2, 2))))
        for j in range(int(min(r, 150) / 20)):          # rescoldos detrás
            for sgn in (-1, 1):
                yy = yi + sgn * j * 20
                im.alpha_composite(smol[(t + j) % 6], (int(ox + px - 12 + (j % 3 - 1) * 14), int(oy + yy - 34)))
        frames2.append(crop(im))
    save(frames2, 100, 'Lepido_pared_fuego.gif')
    keys += [frames2[i] for i in (3, 10, 20, 33)]
    # 3) se retira bajo tierra (sacudida + tierra) y queda la cicatriz
    ret = _frames(f'Lepido_Wall{n}_Retract', W); scar = _sheet(f'Lepido_Wall{n}_Scar')
    D3 = _sheet(f'Lepido_Wall{n}_D3')
    frames3 = []
    for k in range(6):
        im = room.copy(); _place(im, D3, m, dx=(2 if k % 2 else -2)); frames3.append(crop(im))
    for f in ret + [scar] * 6:
        im = room.copy(); _place(im, f, m); frames3.append(crop(im))
    save(frames3, [50] * 6 + [90] * 8 + [200] * 6, 'Lepido_pared_retirada.gif')
    keys += [frames3[i] for i in (2, 8, 11, 15)]
    # 4) carbonizada: se desmorona en ceniza
    cr = _frames(f'Lepido_Wall{n}_Crumble', W); ash = _sheet(f'Lepido_Wall{n}_AshScar')
    frames4 = []
    for f in cr + [ash] * 6:
        im = room.copy(); _place(im, f, m); frames4.append(crop(im))
    save(frames4, [90] * 8 + [200] * 6, 'Lepido_pared_ceniza.gif')
    keys += [frames4[i] for i in (2, 5, 9)]
    names = ['nudo: late', 'golpe', 'revienta', 'savia y astillas', 'herida', 'prende', 'el fuego avanza', 'casi toda', 'carbonizada',
             'sacudida', 'se retira', 'se hunde', 'cicatriz', 'se desmorona', 'ceniza', 'montón de ceniza']
    w_, h_ = keys[0].size
    cols = 8
    sheet = Image.new('RGBA', (cols * (w_ + 6), 2 * (h_ + 24)), (16, 18, 16, 255))
    dd = ImageDraw.Draw(sheet)
    for i, (f, nm) in enumerate(zip(keys, names)):
        x, y = (i % cols) * (w_ + 6), (i // cols) * (h_ + 24)
        sheet.alpha_composite(f, (x, y + 22)); dd.text((x + 4, y + 4), nm, fill=(120, 230, 230), font=A.font(14))
    G._save(sheet, 'Lepidodendron ancestral · 6m escenas de la pared 2 (×1): nudo que revienta, fuego que avanza, retirada y ceniza',
            ['GIF (×2): gif/escenas/Lepido_pared_nudo.gif, _fuego.gif, _retirada.gif, _ceniza.gif'], 'vista_previa_6m_escenas.png')

if __name__ == '__main__':
    which = sys.argv[1:] or ['muestra']
    if 'muestra' in which: lamina_muestra()
    if 'piezas' in which: piezas(); lamina_estados()
    if 'escenas' in which: escenas()
