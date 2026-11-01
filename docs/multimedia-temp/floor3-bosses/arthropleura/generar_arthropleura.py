"""Arthropleura, jefe del piso 3 de Primordia (tanda 6a: cuerpo base). Skill primordia-sprites (pixel.py).

Milpiés gigante del Carbonífero en picada (cámara a 55°, como el Diplocaulus), rediseñado el 2026-10-31 con la foto de
referencia del usuario: quitina de cobre oxidado, placas planas y anchas que se solapan como una armadura, patas
largas con anillos y una cabeza de escudo con ojos negros y colmillos.

Modelado: superficies de altura (placas con bisel, cresta, surco y bultos; paranotos lobulados), tubos (patas,
colmillos, antenas, espinas) y esferas (ojos), muestreados en puntos con normal y proyectados con un z-buffer. Misma
luz y mismos escalones de sombra que generar_cuerpos_piso3.trace(); contorno OUTL y líneas interiores donde una pieza
tapa a otra.

El cuerpo son piezas que el código encadena (pivote = centro de la celda = nudo de la cadena, a 2,6 u de paso):
- Head (160×160): escudo, ojos, colmillos, antenas y la primera placa (cuello) con 2 pares de patas.
- Segment (112×112): DOS placas por pieza (56 placas a la vista con 28 piezas), 4 pares de patas en ola.
- Tail (112×112): una placa, pigidio y dos espinas.
16 direcciones: 9 dibujadas (de +90° arriba a -90° abajo, mirando a la derecha) y el resto con Flip X.

Uso: python generar_arthropleura.py todo      (hojas de la 6a en Sheets/, vistas previas y GIF)
     python generar_arthropleura.py estados   (6b: capas de estado, placas abiertas, rueda y muerte)
     python generar_arthropleura.py muestra   (la muestra del aspecto)
"""
import math, os, random, sys
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
MT = os.path.normpath(os.path.join(HERE, '..', '..'))
sys.path.insert(0, os.path.join(MT, 'floor3-environment'))
from base_piso3 import *                               # noqa: F401,F403  (pixel.py, paletas del piso)
from PIL import Image

OUT = HERE
BG = C('#30332a')                                      # suelo del Bosque humeante
ELEV = math.radians(55)
STEP_PX = 2.6 * 16                                     # paso de la cadena (2,6 u)
SEG_N, HEAD_N, TAIL_N = 112, 160, 112

# --- paletas (rampa de 5, de sombra a luz) --------------------------------------------------------
PALS = {
    'plate':   ['#1f120e', '#43241a', '#6e3a22', '#9c5a34', '#c88a58'],   # cobre oxidado
    'band':    ['#170d0b', '#2e1a14', '#4a2a1c', '#6e3a22', '#9c5a34'],   # borde trasero de cada placa
    'under':   ['#3a2418', '#5e3a26', '#8a5a3a', '#b48058', '#d8aa80'],   # vientre (al alzarse)
    'edge':    ['#4a2a1a', '#7a4a2c', '#b07a4c', '#d8a874', '#f0d0a0'],   # cuero: filos, pinchos, puntas
    'leg':     ['#2a1510', '#5a2c18', '#9a4e26', '#c8743a', '#e8a868'],   # patas anaranjadas
    'legband': ['#170d0b', '#2e1a14', '#4a2a1c', '#6e3a22', '#9c5a34'],   # anillos de las patas (= band)
    'eye':     ['#08060a', '#140e14', '#241a22', '#4a3238', '#e8dcd4'],   # ojo compuesto negro brillante
    'fang':    ['#170d0b', '#2e1a14', '#4a2a1c', '#6e3a22', '#9c5a34'],   # colmillos: oscuros (= band), punta de cuero
    'mouth':   ['#22080f', '#3a1020', '#6a2034', '#9a3a4a', '#c8687a'],   # boca
    'spike':   ['#4a2a1a', '#7a4a2c', '#b07a4c', '#d8a874', '#f0d0a0'],   # pinchos y desgaste (= cuero; se carbonizan)
    'crack':   ['#0e0807', '#140b09', '#1c100c', '#2a1812', '#3a2218'],   # grieta (6b)
    'crackhi': ['#7a4a2c', '#9c6a40', '#c08a58', '#d8a874', '#e8c090'],   # borde roto que coge la luz (6b)
    'ember':   ['#5a1a14', '#a8341c', '#e8682a', '#ffb24a', '#fff0b0'],   # ascuas en las grietas del chamuscado (6b)
    'flesh':   ['#5a1e2e', '#9a3a4a', '#d06a74', '#f0a0a0', '#ffd8d0'],   # carne rosada bajo las placas (6b)
    'canopy':  ['#1a1c18', '#2c3127', '#433d1e', '#635726', '#8a7a40'],   # cresta de frondas sobre la espesura (6c)
    'canopydk': ['#141612', '#1a1c18', '#22261f', '#2c3127', '#373c2f'],  # huecos entre frondas
}
CHARP = ['#0e0b0c', '#1c1618', '#2c2426', '#423638', '#5c4c48']           # placas chamuscadas
ASHP = ['#1e1c1e', '#363234', '#524c4c', '#746c68', '#9a908a']            # muerto: gris ceniza
MATS = list(PALS)
MI = {m: i for i, m in enumerate(MATS)}
PAL_C = [[C(c) for c in PALS[m]] for m in MATS]

# =================================================================================================
# Geometría: nubes de puntos con normal, material y grupo
# =================================================================================================
class Geo:
    def __init__(s): s.P, s.N, s.M, s.G = [], [], [], []; s.groups = {}
    def gid(s, name):
        if name not in s.groups: s.groups[name] = len(s.groups)
        return s.groups[name]
    def add(s, P, N, mat, grp):
        P = np.asarray(P, float).reshape(-1, 3); N = np.asarray(N, float).reshape(-1, 3)
        N = N / np.maximum(1e-6, np.linalg.norm(N, axis=1, keepdims=True))
        s.P.append(P); s.N.append(N)
        s.M.append(np.full(len(P), MI[mat]) if isinstance(mat, str) else np.asarray(mat))
        s.G.append(np.full(len(P), s.gid(grp)))
    def arrays(s):
        return np.vstack(s.P), np.vstack(s.N), np.concatenate(s.M), np.concatenate(s.G)

def _place(F, L, Z, fc, lc, uc, yaw):
    ca, sa = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    return fc + F * ca - L * sa, lc + F * sa + L * ca, uc + Z

def slab(g, fc, lc, uc, a, b, h, grp, p=4.0, s0=.55, detail=None, yaw=0.0, mat='plate', band=None, serr=0.0,
         droop=0.0, under=True, step=.33, marks=None):
    """Placa: superficie de altura sobre una superelipse (a a lo largo, b a lo ancho) con la tapa plana y el borde
    biselado. detail(F, L) suma relieve; band = fracción trasera con el material 'band'; serr = dientes en el borde
    delantero; droop = cae hacia los lados (paranotos)."""
    fs = np.arange(-a, a + step, step); ls = np.arange(-b, b + step, step)
    F, L = np.meshgrid(fs, ls); F = F.ravel(); L = L.ravel()
    def sfun(F, L):
        s = (np.abs(F / a) ** p + np.abs(L / b) ** p) ** (1 / p)
        if serr: s = s + serr * (0.5 + 0.5 * np.sin(L * 1.25)) * np.clip((F / a - .25) / .75, 0, 1)
        return s
    def zfun(F, L):
        s = sfun(F, L)
        prof = np.where(s <= s0, 1.0, np.sqrt(np.clip(1 - ((s - s0) / (1 - s0)) ** 2, 0, 1)))
        z = h * prof - droop * np.abs(L) / b
        if detail is not None: z = z + detail(F, L) * prof
        return z
    s = sfun(F, L); keep = s <= 1
    F, L = F[keep], L[keep]
    Z = zfun(F, L); d = .2
    zx = (zfun(F + d, L) - zfun(F - d, L)) / (2 * d); zy = (zfun(F, L + d) - zfun(F, L - d)) / (2 * d)
    ca, sa = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    nf, nl = (-zx) * ca - (-zy) * sa, (-zx) * sa + (-zy) * ca          # normal girada con la pieza
    X, Y, U = _place(F, L, Z, fc, lc, uc, yaw)
    mats = np.full(len(F), MI[mat])
    if band: mats[F < -a * (1 - 2 * band)] = MI['band']
    if marks is not None:                                       # textura: {material: máscara(F, L)}
        for m_, fn in marks.items(): mats[fn(F, L)] = MI[m_]
    g.add(np.stack([X, Y, U], 1), np.stack([nf, nl, np.ones_like(nf)], 1), mats, grp)
    if under:                                                   # cara de abajo (se ve al alzarse)
        X2, Y2, U2 = _place(F, L, np.zeros_like(F) - .8 - droop * np.abs(L) / b, fc, lc, uc, yaw)
        g.add(np.stack([X2, Y2, U2], 1), np.tile([0, 0, -1.0], (len(F), 1)), 'under', grp)
    # canto del borde (1 px de grueso)
    th = np.linspace(0, 2 * math.pi, int(2 * math.pi * max(a, b) / .3))
    lo, hi = np.zeros_like(th), np.full_like(th, 1.5)
    for _ in range(18):
        m = (lo + hi) / 2
        inside = sfun(m * a * np.cos(th), m * b * np.sin(th)) <= 1
        lo, hi = np.where(inside, m, lo), np.where(inside, hi, m)
    cF, cL = lo * a * np.cos(th), lo * b * np.sin(th)
    nf3, nl3 = np.cos(th) / a, np.sin(th) / b
    nf3, nl3 = nf3 * ca - nl3 * sa, nf3 * sa + nl3 * ca
    for zz in (-.6, -.2):
        X3, Y3, U3 = _place(cF, cL, np.full(len(cF), zz) - droop * np.abs(cL) / b, fc, lc, uc, yaw)
        g.add(np.stack([X3, Y3, U3], 1), np.stack([nf3, nl3, np.zeros_like(nf3)], 1), 'band' if band else mat, grp)

def bez(pts, n=None):
    """Curva de Bézier por los puntos de control (muestreo denso)."""
    P = np.asarray(pts, float)
    L = sum(np.linalg.norm(P[i + 1] - P[i]) for i in range(len(P) - 1))
    n = n or max(8, int(L / .3))
    t = np.linspace(0, 1, n)[:, None]
    k = len(P) - 1
    out = np.zeros((n, 3))
    for i in range(k + 1):
        out += math.comb(k, i) * (t ** i) * ((1 - t) ** (k - i)) * P[i]
    return out

def tube(g, path, r0, r1, grp, mats, cap=True):
    """Tubo a lo largo de path (N×3) con radio de r0 a r1; mats(t) -> material por tramo (t de 0 a 1)."""
    path = np.asarray(path, float); n = len(path)
    T = np.gradient(path, axis=0); T /= np.maximum(1e-6, np.linalg.norm(T, axis=1, keepdims=True))
    ref = np.where(np.abs(T[:, 2:3]) < .9, np.array([[0, 0, 1.0]]), np.array([[1.0, 0, 0]]))
    N1 = np.cross(T, ref); N1 /= np.maximum(1e-6, np.linalg.norm(N1, axis=1, keepdims=True))
    N2 = np.cross(T, N1)
    ts = np.linspace(0, 1, n)
    r = r0 + (r1 - r0) * ts
    m_ = max(8, int(2 * math.pi * max(r0, r1) / .35))
    ph = np.linspace(0, 2 * math.pi, m_, endpoint=False)
    cos, sin = np.cos(ph)[None, :, None], np.sin(ph)[None, :, None]
    Nr = cos * N1[:, None, :] + sin * N2[:, None, :]
    P = path[:, None, :] + r[:, None, None] * Nr
    mat = np.array([MI[mats(t)] for t in ts])
    g.add(P.reshape(-1, 3), Nr.reshape(-1, 3), np.repeat(mat, m_), grp)
    if cap: sphere(g, path[-1], max(.5, r1), grp, mats(1.0))

def sphere(g, c, r, grp, mat, squash=1.0):
    n = max(10, int(math.pi * r / .3))
    th, ph = np.meshgrid(np.linspace(0, math.pi, n), np.linspace(0, 2 * math.pi, 2 * n))
    N = np.stack([np.sin(th) * np.cos(ph), np.sin(th) * np.sin(ph), np.cos(th)], -1).reshape(-1, 3)
    P = np.asarray(c) + N * np.array([r, r, r * squash])
    g.add(P, N * np.array([1, 1, 1 / squash]), mat, grp)

# =================================================================================================
# Render (z-buffer)
# =================================================================================================
def render_geo(g, yaw, n, origin=None, only=None, pal=None):
    """only: materiales que se pintan (capa encima, sin contorno); pal: {material: rampa} que sustituye colores."""
    P, N, M, G = g.arrays()
    y = math.radians(yaw); cy, sy = math.cos(y), math.sin(y)
    X = P[:, 0] * cy - P[:, 1] * sy; Y = P[:, 0] * sy + P[:, 1] * cy; Z = P[:, 2]
    NX = N[:, 0] * cy - N[:, 1] * sy; NY = N[:, 0] * sy + N[:, 1] * cy; NZ = N[:, 2]
    se, ce = math.sin(ELEV), math.cos(ELEV)
    ox, oy = origin or (n / 2, n / 2)                         # pivote: centro de la celda = nudo de la cadena
    sx = X + ox; syy = -(Y * se + Z * ce) + oy
    depth = Y * ce - Z * se
    vis = (NY * ce - NZ * se) < 0.08                          # cara hacia la cámara
    ix, iy = np.floor(sx).astype(int), np.floor(syy).astype(int)
    ok = vis & (ix >= 0) & (iy >= 0) & (ix < n) & (iy < n)
    ix, iy, depth, M, G = ix[ok], iy[ok], depth[ok], M[ok], G[ok]
    ns = np.stack([NX[ok], -(NY[ok] * se + NZ[ok] * ce), -(NY[ok] * ce - NZ[ok] * se)], 1)   # normal en pantalla
    idx = iy * n + ix
    order = np.lexsort((depth, idx))
    first = np.unique(idx[order], return_index=True)[1]
    sel = order[first]
    D = np.full((n, n), 1e9); GR = np.full((n, n), -1); MA = np.full((n, n), -1); NS = np.zeros((n, n, 3))
    D[iy[sel], ix[sel]] = depth[sel]; GR[iy[sel], ix[sel]] = G[sel]; MA[iy[sel], ix[sel]] = M[sel]
    NS[iy[sel], ix[sel]] = ns[sel]
    # normal suavizada (5×5 del mismo grupo y profundidad parecida)
    acc = np.zeros_like(NS); pad = 2
    Dp = np.pad(D, pad, constant_values=1e9); Gp = np.pad(GR, pad, constant_values=-2)
    Np = np.pad(NS, ((pad, pad), (pad, pad), (0, 0)))
    for dy in range(-2, 3):
        for dx in range(-2, 3):
            Gs = Gp[pad + dy:pad + dy + n, pad + dx:pad + dx + n]; Ds = Dp[pad + dy:pad + dy + n, pad + dx:pad + dx + n]
            m = (Gs == GR) & (np.abs(Ds - D) < 3)
            w = 2.0 if (dx, dy) == (0, 0) else 1.0
            acc += Np[pad + dy:pad + dy + n, pad + dx:pad + dx + n] * (m[..., None] * w)
    acc /= np.maximum(1e-6, np.linalg.norm(acc, axis=2, keepdims=True))
    lit = np.array(LIGHT, float); lit /= np.linalg.norm(lit)
    lam = acc @ lit
    I = np.where(lam < -.2, 0, np.where(lam < .5, 1, np.where(lam < .88, 2, np.where(lam < .97, 3, 4))))
    I = np.where((lam < 0) & (acc[..., 2] < .35), I + 1, I)   # luz rebotada en el borde en sombra
    I = np.clip(I, 0, 4)
    # líneas interiores: donde una pieza de otro grupo tapa a esta con un salto de profundidad
    line = np.zeros((n, n), bool)
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        Gs = Gp[pad + dy:pad + dy + n, pad + dx:pad + dx + n]; Ds = Dp[pad + dy:pad + dy + n, pad + dx:pad + dx + n]
        line |= (GR >= 0) & (Gs >= 0) & (Gs != GR) & (D - Ds > 1.6)
    cv = Canvas(n)
    keep = None if only is None else {MI[m] for m in only}
    PC = PAL_C if not pal else [[C(c) for c in pal[m]] if m in pal else PAL_C[MI[m]] for m in MATS]
    for yy, xx in zip(*np.nonzero(GR >= 0)):
        if keep is not None and MA[yy, xx] not in keep: continue
        i = 0 if line[yy, xx] else int(I[yy, xx])
        cv.px[(int(xx), int(yy))] = PC[MA[yy, xx]][i]
    cv.ma, cv.gr = MA, GR
    if keep is not None: return cv
    # limpieza: píxel distinto de sus vecinos del mismo grupo, que coinciden
    for _ in range(2):
        for (x, yv), c in list(cv.px.items()):
            nb = [cv.px[(x + a, yv + b)] for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1))
                  if (x + a, yv + b) in cv.px and GR[yv + b, x + a] == GR[yv, x]]
            if len(nb) >= 3 and c not in nb:
                best = max(set(nb), key=nb.count)
                if nb.count(best) >= 3: cv.px[(x, yv)] = best
    # brillo rojo en los ojos, junto al reflejo blanco
    eye_hi = PAL_C[MI['eye']][4]
    for (x, yv), c in list(cv.px.items()):
        if not pal and c == eye_hi and cv.px.get((x + 1, yv + 1)) in PAL_C[MI['eye']][:3]:
            cv.px[(x + 1, yv + 1)] = C('#c0302a')
    cv.outline(); cv.clean()
    return cv

# =================================================================================================
# Piezas
# =================================================================================================
def _pits(a, b, seed, n=9):
    rnd = random.Random(seed)
    return [(rnd.uniform(-a * .75, a * .55), rnd.uniform(-b * .85, b * .85), rnd.uniform(.9, 1.4)) for _ in range(n)]

def plate_detail(a, b, seed=0):
    pits = _pits(a, b, seed)
    def det(F, L):
        keel = 1.0 * np.exp(-(L / 2.4) ** 2)                                     # cresta central
        groove = -0.9 * np.exp(-((F - a * .30) / .8) ** 2)                       # surco transversal
        nod = sum(.45 * np.exp(-((F + 2) ** 2 + (L - l0) ** 2) / 6.0) for l0 in (-b * .55, b * .55))
        stri = .28 * np.sin(F * 2.1 + .4 * np.sin(L * .35)) * np.clip(-F / a + .1, 0, 1)   # estrías de la mitad trasera
        pit = sum(-.7 * np.exp(-((F - f0) ** 2 + (L - l0) ** 2) / (r * r)) for f0, l0, r in pits)
        return keel + groove + nod + stri + pit
    return det

def plate_marks(a, b, seed=0):
    """Textura de quitina en color: poros oscuros y desgaste claro en los bultos."""
    pits = _pits(a, b, seed)
    def pores(F, L):
        m = np.zeros(len(F), bool)
        for f0, l0, r in pits: m |= (F - f0) ** 2 + (L - l0) ** 2 < (r * .75) ** 2
        return m
    def worn(F, L):
        h = np.zeros(len(F), bool)
        for l0 in (-b * .55, b * .55): h |= (F + 2) ** 2 + (L - l0) ** 2 < 1.6
        return h
    return {'band': pores, 'spike': worn}

def crack_lines(a, b, seed, n, steps, step_len=2.4, branch=0.0):
    """Grietas: polilíneas en zigzag que entran desde el borde hacia el centro de la placa."""
    rnd = random.Random(seed); lines = []
    for k in range(n):
        side = rnd.choice((-1, 1))
        f, l = rnd.uniform(-a * .6, a * .5), side * b * rnd.uniform(.55, .95)
        ang = math.atan2(-l, -f * .4 + rnd.uniform(-2, 2))
        pts = [(f, l)]
        for _ in range(steps):
            ang += rnd.uniform(-.75, .75)
            f, l = f + math.cos(ang) * step_len, l + math.sin(ang) * step_len
            pts.append((f, l))
            if branch and rnd.random() < branch:                                 # rama corta
                ba = ang + rnd.choice((-1, 1)) * rnd.uniform(.7, 1.2)
                lines.append([(f, l), (f + math.cos(ba) * step_len * 1.3, l + math.sin(ba) * step_len * 1.3)])
        lines.append(pts)
    return lines

def line_dist(F, L, lines):
    """Distancia de cada punto a la grieta más cercana y lado (arriba a la izquierda = +1)."""
    d = np.full(len(F), 1e9); side = np.zeros(len(F))
    for pts in lines:
        for (f0, l0), (f1, l1) in zip(pts, pts[1:]):
            df, dl = f1 - f0, l1 - l0; L2 = df * df + dl * dl or 1e-6
            t = np.clip(((F - f0) * df + (L - l0) * dl) / L2, 0, 1)
            dd = np.hypot(F - (f0 + t * df), L - (l0 + t * dl))
            cr = np.sign(df * (L - l0) - dl * (F - f0))
            upd = dd < d; d = np.where(upd, dd, d); side = np.where(upd, cr, side)
    return d, side

def state_marks(a, b, seed, crack=0, char=False):
    """Marcas de estado: grietas (1 = 70 %, 2 = 35 %) y ascuas del chamuscado."""
    out = {}
    if crack:
        lines = crack_lines(a, b, seed * 7 + 1, 2, 4) if crack == 1 else crack_lines(a, b, seed * 7 + 1, 3, 6, branch=.3)
        out['crack'] = lambda F, L, ls=lines: line_dist(F, L, ls)[0] < .8
        out['crackhi'] = lambda F, L, ls=lines: ((lambda d, s_: (d >= .8) & (d < 1.5) & (s_ > 0))(*line_dist(F, L, ls)))
    if char:
        el = crack_lines(a, b, seed * 13 + 5, 2, 5, step_len=2.0, branch=.25)
        out['ember'] = lambda F, L, ls=el: line_dist(F, L, ls)[0] < .5
    return out

def hinge(g, i0, hf, hu, th):
    """Gira los bloques añadidos desde i0 alrededor de la bisagra (f = hf, u = hu): la parte de detrás sube."""
    c, s_ = math.cos(th), math.sin(th)
    for i in range(i0, len(g.P)):
        P, N = g.P[i], g.N[i]
        f, u = P[:, 0] - hf, P[:, 2] - hu
        P[:, 0], P[:, 2] = hf + f * c - u * s_, hu + f * s_ + u * c
        nf, nz = N[:, 0].copy(), N[:, 2].copy()
        N[:, 0], N[:, 2] = nf * c - nz * s_, nf * s_ + nz * c

def body_plate(g, fc, uc, grp, a=12.6, b=21.0, wing=True, crack=0, char=False, open_p=0.0, pulse=0.0):
    sd_ = sum(map(ord, grp)) % 97 + int(fc * 3)
    if open_p > 0:                                                               # carne rosada bajo la placa
        def folds(F, L): return .5 * np.sin(F * 1.1 + L * .25) + .7 * np.exp(-(L / 4) ** 2)
        slab(g, fc - 1, 0, uc - 1.6, a * .95, b * .9, 3.4 + 1.2 * pulse, grp + 'f', p=3, s0=.35, mat='flesh',
             detail=folds, under=False)
    i0 = len(g.P)
    marks = plate_marks(a, b, sd_); marks.update(state_marks(a, b, sd_, crack, char))
    slab(g, fc, 0, uc, a, b, 3.0, grp, p=5, s0=.62, detail=plate_detail(a, b, sd_), band=.16, marks=marks)
    if wing:
        for sd in (-1, 1):                                                       # paranotos lobulados
            slab(g, fc - 1.5, sd * (b + 4.0), uc + .2, a * .8, 7.5, 1.6, f'{grp}w{sd}', p=2.3, s0=.4, yaw=sd * -12,
                 droop=.8, band=.2, detail=lambda F, L: .4 * np.exp(-(L / 2) ** 2))
            tube(g, bez([(fc - 4, sd * (b + 8.5), uc + .8), (fc - 9, sd * (b + 12.5), uc + .4), (fc - 14, sd * (b + 14.5), uc - .2)]),
                 2.0, .5, f'{grp}s{sd}', lambda t: 'spike')                      # pincho color cuero
    if open_p > 0: hinge(g, i0, fc + a * .85, uc, -math.radians(72 * open_p))   # se levanta por detrás

def leg(g, fh, sd, walk, k, grp, reach=47, lift_up=0.0, paw=0.0, curl=0.0):
    if curl: reach, lift_up = reach - 16 * curl, lift_up + 4 * curl                      # muerto: patas encogidas
    ph = (walk + k * .25 + (.5 if sd > 0 else 0)) if walk is not None else .2 + k * .13
    sw = math.sin(ph * 2 * math.pi); lift = max(0, math.cos(ph * 2 * math.pi)) * 3.0
    hip = (fh, sd * 15, 3.5)
    knee = (fh + 2.5 * sw + paw * 8, sd * (reach - 13), 11.5 + lift + lift_up)
    foot = (fh + 6.0 * sw + paw * 16, sd * reach, 0.4 + lift * .4 + lift_up * 1.3)
    tube(g, bez([hip, knee], 14), 2.1, 1.7, grp, lambda t: 'legband' if t < .25 or t > .88 else 'leg', cap=False)
    tube(g, bez([knee, foot], 16), 1.7, .8, grp, lambda t: 'legband' if t < .1 else 'edge' if t > .82 else 'leg')

def segment_geo(walk=None, bob=0.0, crack=0, char=False, open_p=0.0, pulse=0.0, curl=0.0):
    g = Geo()
    st = dict(crack=crack, char=char, open_p=open_p, pulse=pulse)
    body_plate(g, 10.4, 6.4 + bob + .9, 'p0', **st)                           # la de delante tapa a la de detrás
    body_plate(g, -10.4, 6.4 + bob, 'p1', **st)
    for k, fh in enumerate((15.5, 5.5, -5.5, -15.5)):
        for sd in (-1, 1): leg(g, fh, sd, walk, k, f'l{k}{sd}', curl=curl)
    return g

def tail_geo(walk=None, bob=0.0, crack=0, char=False, curl=0.0):
    g = Geo()
    body_plate(g, 9, 6.2 + bob, 'p0', a=12.0, b=19.0, crack=crack, char=char)
    marks = state_marks(8.5, 13, 41, crack, char)
    slab(g, -7, 0, 5.6 + bob, 8.5, 13.0, 2.6, 'pyg', p=2.6, s0=.5, detail=plate_detail(8.5, 13), band=.25, marks=marks)
    for sd in (-1, 1):
        tube(g, bez([(-9, sd * 6, 6 + bob), (-20, sd * 9, 6.5 + bob), (-32, sd * 10, 5 + bob)]), 2.6, .7,
             f'sp{sd}', lambda t: 'plate' if t < .3 else 'spike')
    for k, fh in enumerate((14.0, 4.0)):
        for sd in (-1, 1): leg(g, fh, sd, walk, k, f'l{k}{sd}', reach=43, curl=curl)
    return g

def head_geo(open_=0.0, feel=0.0, rear=0.0, lunge=0.0, bob=0.0, walk=None, rear_up=0.0, crack=0, char=False, curl=0.0):
    """open_: colmillos abiertos (0..1); rear: se alza (0..1, ~48°); lunge: adelanta la cabeza."""
    g = Geo()
    u = 7.0 + bob; f0 = 7.0 + 4 * lunge
    body_plate(g, -15, u - .4, 'collar', a=12.0, b=21.0, crack=crack, char=char)
    for k, fh in enumerate((-10.0, -20.0)):
        for sd in (-1, 1): leg(g, fh, sd, walk, k, f'l{k}{sd}', reach=44, lift_up=8 * rear, paw=rear * (1 - k), curl=curl)
    # escudo: pala ancha con el borde delantero dentado, surco central y cuencas de los ojos
    def sh_det(F, L):
        return (-0.9 * np.exp(-(L / 1.6) ** 2) * (F > -2) + 1.4 * np.exp(-((F - 3) ** 2 + (np.abs(L) - 13) ** 2) / 10)
                + 0.6 * np.exp(-((F + 6) / 2.5) ** 2))
    slab(g, f0, 0, u + .6, 14.0, 25.0, 4.6, 'head', p=2.5, s0=.45, detail=sh_det, serr=.11,
         marks=state_marks(14.0, 25.0, 77, crack, char))
    for sd in (-1, 1):                                                           # ojos compuestos
        slab(g, f0 - 1.5, sd * 14.0, u + 5.4, 2.6, 6.5, 1.8, f'brow{sd}', p=2, s0=.3, yaw=sd * -20, under=False)  # ceja detrás del ojo
        sphere(g, (f0 + 3.0, sd * 15.0, u + 6.8), 3.3, f'eye{sd}', 'eye', squash=.85)
    if open_ > .05:                                                              # boca entre los colmillos
        slab(g, f0 + 13, 0, u - 1.5, 4.5, 3 + 6 * open_, 1.0, 'mouth', p=2, s0=.3, mat='mouth', under=False)
    for sd in (-1, 1):                                                           # colmillos curvos
        o = open_
        pts = [(f0 + 10, sd * 11, u - .5), (f0 + 25 + 2 * o, sd * (19 + 10 * o), u + .5),
               (f0 + 35 + 2 * o, sd * (15 + 15 * o), u - .5), (f0 + 39 + 3 * o, sd * (7 + 17 * o), u - 2.0)]
        tube(g, bez(pts), 4.8, .8, f'fang{sd}', lambda t: 'fang' if t < .5 else 'edge')
    for sd in (-1, 1):                                                           # antenas con anillos
        w = math.sin(feel * 2 * math.pi + (0 if sd < 0 else 1.9))
        pts = [(f0 + 11, sd * 7, u + 3), (f0 + 24 + 2 * w, sd * (14 + 3 * w), u + 12),
               (f0 + 38 + 2 * w, sd * (22 + 3 * w), u + 8), (f0 + 46 + 2 * w, sd * (24 + 3 * w), u + 2)]
        tube(g, bez(pts), 1.3, .7, f'ant{sd}', lambda t: 'legband' if int(t * 14) % 2 else 'plate')
    if rear > 0:                                                                 # se alza: gira todo hacia arriba
        th = math.radians(38 * rear * (1 - .75 * rear_up)); pf = -27.0; up = (15.0 + 24.0 * rear_up) * rear
        for i in range(len(g.P)):
            P, N = g.P[i], g.N[i]
            f, z = P[:, 0] - pf, P[:, 2].copy()
            P[:, 0], P[:, 2] = pf + f * math.cos(th) - z * math.sin(th), f * math.sin(th) + z * math.cos(th) + up * np.clip((P[:, 0] + 20) / 20, 0, 1)
            nf, nz = N[:, 0].copy(), N[:, 2].copy()
            N[:, 0], N[:, 2] = nf * math.cos(th) - nz * math.sin(th), nf * math.sin(th) + nz * math.cos(th)
    return g

def render(kind, yaw, only=None, pal=None, **pose):
    if kind == 'seg': return render_geo(segment_geo(**pose), yaw, SEG_N, only=only, pal=pal)
    if kind == 'tail': return render_geo(tail_geo(**pose), yaw, TAIL_N, only=only, pal=pal)
    if kind == 'roll': return render_geo(roll_geo(**pose), yaw, ROLL_N, origin=(ROLL_N / 2, ROLL_N / 2 + 26), only=only, pal=pal)
    if pose.get('rear') and yaw < 0: pose = dict(pose, rear_up=-yaw / 90)   # hacia la cámara: gira menos y sube más
    return render_geo(head_geo(**pose), yaw, HEAD_N, only=only, pal=pal)

# =================================================================================================
# Muestra para aprobar el aspecto
# =================================================================================================
def sprite_for(kind, ang, cache, **pose):
    """Sprite de una pieza mirando a ang (0 = derecha, 90 = arriba): cuantiza a 16 y voltea si mira a la izquierda."""
    q = round(ang / 22.5) * 22.5
    q = (q + 180) % 360 - 180
    flip = abs(q) > 90
    base = (180 - q if q > 0 else -180 - q) if flip else q
    key = (kind, base, tuple(sorted(pose.items())))
    if key not in cache: cache[key] = render(kind, base, **pose).img()
    im = cache[key]
    return im.transpose(Image.FLIP_LEFT_RIGHT) if flip else im

def floor_bg(w, h, seed=3):
    tiles = os.path.join(MT, 'floor3-environment', 'Smoke', 'tiles')
    fl = [Image.open(os.path.join(tiles, f'Floor_{k}.png')).convert('RGBA') for k in range(6)]
    rnd = random.Random(seed); im = Image.new('RGBA', (w, h))
    for y in range(0, h, 16):
        for x in range(0, w, 16): im.alpha_composite(fl[0 if rnd.random() < .66 else rnd.randint(1, 5)], (x, y))
    return im

def chain_scene(n_seg=7, w=440, h=260):
    """Tramo de la cadena como lo pondría el código: nudos cada 2,6 u, cada pieza con su dirección; cada placa
    tapa a la de detrás (se dibuja de la cola a la cabeza)."""
    im = floor_bg(w, h); cache = {}
    se = math.sin(ELEV)
    pts = [(70 + t * 320, 150 - 75 * math.sin(t * 3.4 + .3)) for t in [k / 399 for k in range(400)]][::-1]
    nodes, acc, want = [], 0.0, 0.0
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        L = math.hypot(bx - ax, (by - ay) / se)              # distancia en el suelo (la vertical va encogida)
        while want <= acc + L and len(nodes) < n_seg + 2:
            t = (want - acc) / L
            nodes.append((ax + (bx - ax) * t, ay + (by - ay) * t, math.degrees(math.atan2(-(ay - by) / se, ax - bx))))
            want += STEP_PX
        acc += L
    for i in range(len(nodes) - 1, -1, -1):
        x, y, a = nodes[i]
        kind = 'head' if i == 0 else 'tail' if i == len(nodes) - 1 else 'seg'
        pose = {'feel': .2} if kind == 'head' else {'walk': (i * .17) % 1}
        spr = sprite_for(kind, a, cache, **pose)
        im.alpha_composite(spr, (int(x - spr.width / 2), int(y - spr.height / 2)))
    frog = Image.open(os.path.join(MT, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')).convert('RGBA').crop((0, 0, 32, 32))
    im.alpha_composite(frog, (w - 64, h - 44))
    return im

def muestra():
    rows = {}
    rows['Head_Move'] = [render('head', 0, feel=i / 6, bob=.5 * math.sin(i / 6 * 2 * math.pi), walk=i / 6) for i in range(6)]
    rows['Head_Open (colmillos)'] = [render('head', 0, open_=v, feel=.3) for v in (0, .35, .7, 1.0, 1.0, .5)]
    rows['Head_Rear (se alza)'] = [render('head', 0, rear=v, open_=min(1, v * 1.2), feel=.3 + v) for v in (0, .3, .6, .9, 1.0, 1.0)]
    rows['Segment_Walk'] = [render('seg', 0, walk=i / 6, bob=.4 * math.sin(i / 6 * 4 * math.pi)) for i in range(6)]
    rows['Tail_Walk'] = [render('tail', 0, walk=i / 6) for i in range(6)]
    rows['Head 5 direcciones'] = [render('head', a, feel=.2) for a in (90, 45, 0, -45, -90)]
    rows['Head_Rear arriba, diagonal, abajo'] = [render('head', a, rear=1, open_=1, feel=.5) for a in (90, -45, -90)]
    rows['Segment 5 direcciones'] = [render('seg', a, walk=.1) for a in (90, 45, 0, -45, -90)]
    rows['Tail 5 direcciones'] = [render('tail', a, walk=.1) for a in (90, 45, 0, -45, -90)]
    preview(os.path.join(OUT, 'vista_previa_6a_muestra.png'), rows, BG, scale=3)
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    for name, ms in (('Head_Move', 110), ('Head_Open (colmillos)', 100), ('Head_Rear (se alza)', 110), ('Segment_Walk', 90)):
        gif(os.path.join(OUT, 'gif', 'muestra_' + name.split(' ')[0] + '.gif'), rows[name], ms, BG, scale=3)
    sc = chain_scene()
    sc.resize((sc.width * 3, sc.height * 3), Image.NEAREST).convert('RGB').save(os.path.join(OUT, 'vista_previa_6a_cadena.png'))
    print('Listo:', OUT)

# =================================================================================================
# 6a completa: todas las direcciones y animaciones
# =================================================================================================
DIRS = [('Up90', 90.0), ('Up68', 67.5), ('Up45', 45.0), ('Up23', 22.5), ('Side', 0.0),
        ('Dn23', -22.5), ('Dn45', -45.0), ('Dn68', -67.5), ('Dn90', -90.0)]

def sw(i, n, k=1.0): return math.sin(i / n * 2 * math.pi * k)

HEAD_ANIMS = {
    'Move':       [dict(feel=i / 6, bob=.5 * sw(i, 6), walk=i / 6) for i in range(6)],
    'Open':       [dict(open_=v, feel=.3) for v in (0, .4, .8, 1.0, .6, .2)],
    'Rear':       [dict(rear=r, open_=o, feel=.3 + i * .15, walk=i / 6) for i, (r, o) in
                   enumerate(((.3, 0), (.7, .3), (1, .8), (1, 1), (1, 1), (.5, .4)))],
    'ChargeWarn': [dict(lunge=l, open_=o, bob=-.8, feel=.1) for l, o in ((-.3, .3), (-.6, .6), (-.8, .85), (-.85, .95))],
    'Charge':     [dict(lunge=l, open_=o, walk=i / 4, feel=.5) for i, (l, o) in enumerate(((.6, 1), (1, .8), (1, .4), (.6, .2)))],
    'Eat':        [dict(open_=o, lunge=l, bob=b, feel=.2 + i * .1) for i, (o, l, b) in
                   enumerate(((.9, .5, -1), (.1, .8, -2), (0, .6, -1.5), (.35, .5, -1), (0, .5, -1.5), (.35, .5, -1),
                              (0, .4, -1), (.2, .2, -.5)))],
}
SEG_ANIMS = {'Idle': [dict(bob=.35 * sw(i, 4)) for i in range(4)],
             'Walk': [dict(walk=i / 6, bob=.4 * sw(i, 6, 2)) for i in range(6)]}
TAIL_ANIMS = SEG_ANIMS
MS = {'Move': 110, 'Open': 90, 'Rear': 110, 'ChargeWarn': 120, 'Charge': 60, 'Eat': 100, 'Idle': 200, 'Walk': 90,
      'Hurt': 80}

def hurt(cv):
    """Hurt de 2 cuadros (regla de la skill): silueta blanca y luego roja con RED4, contorno intacto."""
    out = []
    for kind in ('white', 'red'):
        c2 = Canvas(cv.w, cv.h)
        for p, c in cv.px.items():
            if c == OUTL: c2.px[p] = c; continue
            if kind == 'white': c2.px[p] = C('#ffffff')
            else:
                lum = (c[0] * .3 + c[1] * .59 + c[2] * .11) / 255
                c2.px[p] = C(RED4[0 if lum < .14 else 1 if lum < .26 else 2 if lum < .45 else 3])
        out.append(c2)
    return out

def grid_sheet(rows):
    w, h = rows[0][0].w, rows[0][0].h
    im = Image.new('RGBA', (w * max(len(r) for r in rows), h * len(rows)), (0, 0, 0, 0))
    for y, r in enumerate(rows):
        for x, f in enumerate(r): im.paste(f.img(), (x * w, y * h))
    return im

def todo():
    pieces = (('Head', 'head', HEAD_ANIMS), ('Segment', 'seg', SEG_ANIMS), ('Tail', 'tail', TAIL_ANIMS))
    os.makedirs(os.path.join(OUT, 'Sheets'), exist_ok=True); os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    side = {}
    for pname, kind, anims in pieces:
        hurt_rows = []
        for aname, poses in anims.items():
            rows = []
            for dname, yaw in DIRS:
                frames = [render(kind, yaw, **pz) for pz in poses]
                rows.append(frames)
                if dname == 'Side': side[f'{pname}_{aname}'] = frames
                if aname == list(anims)[0]: hurt_rows.append(hurt(frames[0]))
            grid_sheet(rows).save(os.path.join(OUT, 'Sheets', f'Arthropleura_{pname}_{aname}.png'))
            print('  ', pname, aname)
        grid_sheet(hurt_rows).save(os.path.join(OUT, 'Sheets', f'Arthropleura_{pname}_Hurt.png'))
        side[f'{pname}_Hurt'] = hurt_rows[4]
    preview(os.path.join(OUT, 'vista_previa_6a_animaciones.png'), side, BG, scale=2)
    dirs = {}
    for pname, kind, anims in pieces:
        first = list(anims)[0]
        dirs[f'{pname} (9 direcciones)'] = [render(kind, yaw, **anims[first][0]) for _, yaw in DIRS]
    dirs['Head_Rear (9 direcciones)'] = [render('head', yaw, **HEAD_ANIMS['Rear'][3]) for _, yaw in DIRS]
    preview(os.path.join(OUT, 'vista_previa_6a_direcciones.png'), dirs, BG, scale=2)
    for name, frames in side.items():
        aname = name.split('_', 1)[1]
        if aname != 'Hurt': gif(os.path.join(OUT, 'gif', f'Arthropleura_{name}.gif'), frames, MS[aname], BG, scale=3)
    curve = curve_frames()
    curve[0].resize((curve[0].width * 2, curve[0].height * 2), Image.NEAREST).convert('RGB').save(
        os.path.join(OUT, 'vista_previa_6a_curva.png'))
    big = [f.resize((f.width * 2, f.height * 2), Image.NEAREST).convert('RGB') for f in curve]
    big[0].save(os.path.join(OUT, 'gif', 'Arthropleura_curva.gif'), save_all=True, append_images=big[1:], duration=90, loop=0)
    print('Listo:', OUT)

def curve_frames(n_seg=14, w=560, h=400, n=6):
    """El cuerpo en una curva cerrada casi entera: pasa por las 16 direcciones (comprueba volteos y solapes). Cada
    segmento usa el cuadro de Walk desfasado uno por pieza (la ola que hará el código)."""
    se = math.sin(ELEV); cache = {}
    cx, cy, R = w / 2, h / 2 + 10, 140.0
    pts = [(cx + R * math.cos(a), cy + R * se * math.sin(a)) for a in [-.4 - k * 2 * math.pi / 600 * .93 for k in range(600)]]
    nodes, acc, want = [], 0.0, 0.0
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        L = math.hypot(bx - ax, (by - ay) / se)
        while want <= acc + L and len(nodes) < n_seg + 2:
            t = (want - acc) / L
            nodes.append((ax + (bx - ax) * t, ay + (by - ay) * t, math.degrees(math.atan2(-(ay - by) / se, ax - bx))))
            want += STEP_PX
        acc += L
    frog = Image.open(os.path.join(MT, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')).convert('RGBA').crop((0, 0, 32, 32))
    out = []
    for f in range(n):
        im = floor_bg(w, h, 5)
        for i in range(len(nodes) - 1, -1, -1):
            x, y, a = nodes[i]
            kind = 'head' if i == 0 else 'tail' if i == len(nodes) - 1 else 'seg'
            pose = HEAD_ANIMS['Move'][f] if kind == 'head' else SEG_ANIMS['Walk'][(f + i) % 6]
            spr = sprite_for(kind, a, cache, **pose)
            im.alpha_composite(spr, (int(x - spr.width / 2), int(y - spr.height / 2)))
        im.alpha_composite(frog, (int(cx - 16), int(cy - 16)))
        out.append(im)
    return out

# =================================================================================================
# 6b: capas de estado (encima de la pieza, mismos cuadros que su animación), placas abiertas, rueda y muerte
# =================================================================================================
sys.path.insert(0, os.path.join(MT, 'floor3-reactions'))
_argv = sys.argv; sys.argv = sys.argv[:1]
import generar_reacciones_piso3 as RX                  # vflame() y cloud() de la tanda 5
sys.argv = _argv
ROLL_N = 144
PLATE_MATS = ('plate', 'band', 'spike', 'under')
EMB = PALS['ember']

def layer_charred(kind, yaw, k, **pose):
    """Chamuscado: placas negras de carbón con grietas de ascuas que laten (k = cuadro: el latido va en 4)."""
    sh = (0, 1, 2, 1)[k % 4]
    pal = {m: CHARP for m in PLATE_MATS}
    pal['ember'] = [EMB[min(4, j + sh)] for j in range(5)]
    return render(kind, yaw, only=PLATE_MATS + ('ember',), pal=pal, char=True, **pose)

def layer_cracks(kind, yaw, level, **pose):
    return render(kind, yaw, only=('crack', 'crackhi'), crack=level, **pose)

def layer_burning(kind, yaw, k, **pose):
    """Ardiendo: llamas que salen de las placas (sitios fijos por dirección; parpadean con el cuadro)."""
    base = render(kind, yaw, **pose)
    n = base.w; ma = base.ma
    plate = np.isin(ma, [MI['plate'], MI['band']])
    cv = Canvas(n)
    sites = []
    for gy in range(4, n, 11):
        for gx in range(4, n, 11):
            ys, xs = np.nonzero(plate[gy:gy + 11, gx:gx + 11])
            if len(xs): j = len(xs) // 2; sites.append((gx + xs[j], gy + ys[j]))
    rnd = random.Random(int(yaw * 10) + n)
    rnd.shuffle(sites); sites = sorted(sites[:6], key=lambda p: p[1])
    hs = [rnd.choice((9, 14, 19, 24)) for _ in sites]
    for j, (x, y) in enumerate(sites):                                       # focos de alturas muy distintas, con lenguas al lado
        h = hs[j] + ((k + j) % 3) * 2 - 2
        RX.vflame(cv, x, y + 2, min(h, y), k * 1.3 + j * 1.7, w=3.4)
        RX.vflame(cv, x - 3, y + 3, min(h * .55, y), k * 1.3 + j * 2.3 + 1, w=1.8)
        RX.vflame(cv, x + 3, y + 3, min(h * .4, y), k * 1.7 + j, w=1.6)
    for j in range(4):                                                       # chispas que suben
        x, y = sites[(j * 3 + k) % len(sites)] if sites else (n / 2, n / 2)
        cv.put(x + (j % 3) - 1, y - 12 - ((k * 3 + j * 5) % 9), C(FIR[3] if j % 2 else FIR[4]))
    return cv

FIR = ['#8a1e14', '#d4441c', '#f88c2a', '#ffd25a', '#fff4c0']
LAYERS = ('Burning', 'Charred', 'Cracks1', 'Cracks2')

def layer(name, kind, yaw, k, **pose):
    if name == 'Burning': return layer_burning(kind, yaw, k, **pose)
    if name == 'Charred': return layer_charred(kind, yaw, k, **pose)
    return layer_cracks(kind, yaw, 1 if name == 'Cracks1' else 2, **pose)

OPEN_ANIMS = {'PlatesOpen': [dict(open_p=v, walk=i / 6, pulse=0) for i, v in enumerate((.25, .5, .8, 1.0))],
              'OpenWalk':   [dict(open_p=1.0, walk=i / 6, pulse=.5 + .5 * sw(i, 6)) for i in range(6)],
              'PlatesClose': [dict(open_p=v, walk=i / 6, pulse=0) for i, v in enumerate((.8, .5, .25, 0.0))]}

def roll_geo(phase=0.0):
    """Rueda (~8 u): el cuerpo enroscado de pie, en dos vueltas de placas, que rueda hacia delante (f).
    phase 0..1 = un paso de 2 placas de fuera (bucle de 8 cuadros)."""
    g = Geo()
    R0 = 46.0
    rings = ((44.0, 18, 8.0, 17.0), (27.0, 9, 7.0, 13.0))
    rot = phase * 2 * math.pi / 9
    for r, nplates, a, b in rings:
        for k in range(nplates):
            th = 2 * math.pi * k / nplates + rot
            T = np.array([math.cos(th), 0, -math.sin(th)]); Rd = np.array([math.sin(th), 0, math.cos(th)])
            Lx = np.array([0, 1.0, 0]); Cc = np.array([0, 0, R0]) + Rd * r
            sub = Geo()
            body_plate(sub, 0, 0, f'r{int(r)}_{k}', a=a, b=b, wing=(r > 30))
            for P, N, M, G in zip(sub.P, sub.N, sub.M, sub.G):
                Pw = Cc + P[:, 0:1] * T + P[:, 1:2] * Lx + P[:, 2:3] * Rd
                Nw = N[:, 0:1] * T + N[:, 1:2] * Lx + N[:, 2:3] * Rd
                name = [nm for nm, gi in sub.groups.items() if gi == G[0]][0]
                g.P.append(Pw); g.N.append(Nw); g.M.append(M); g.G.append(np.full(len(Pw), g.gid(name)))
    for sd in (-1, 1):                                                       # cubos: vientre y patas recogidas
        sphere(g, (0, sd * 9, R0), 20, f'hub{sd}', 'under', squash=1.0)
        for k in range(10):
            th = 2 * math.pi * k / 10 + rot * 2
            a0 = np.array([math.sin(th) * 14, sd * 16, R0 + math.cos(th) * 14])
            a1 = np.array([math.sin(th + .5) * 22, sd * 20, R0 + math.cos(th + .5) * 22])
            tube(g, bez([a0, a1], 10), 1.6, .8, f'rl{sd}{k}', lambda t: 'legband' if t < .3 else 'leg')
    return g

def roll_shadow(cv):
    """Sombra en el suelo bajo la rueda (OUTL translúcido), solo donde no hay rueda."""
    n = cv.w; cx, cy = n / 2, n / 2 + 26
    for y in range(int(cy - 5), int(cy + 6)):
        for x in range(int(cx - 30), int(cx + 31)):
            if ((x + .5 - cx) / 30) ** 2 + ((y + .5 - cy) / 5) ** 2 <= 1 and (x, y) not in cv.px: cv.px[(x, y)] = (24, 20, 37, 90)
    return cv

DEATH_HEAD = [dict(rear=.4, open_=1.0, feel=.9, walk=.1), dict(rear=.75, open_=1.0, feel=.2, walk=.4),
              dict(rear=.5, open_=.8, feel=.6, walk=.7), dict(rear=.2, open_=.6, bob=-1, feel=.3),
              dict(rear=0, open_=.3, bob=-2, curl=.4), dict(rear=0, open_=.2, bob=-2.5, curl=.8),
              dict(rear=0, open_=.2, bob=-2.5, curl=1.0), dict(rear=0, open_=.2, bob=-2.5, curl=1.0)]

def death_frame(yaw, k):
    pose = DEATH_HEAD[k]
    if k < 6: return render('head', yaw, **pose)
    return render('head', yaw, pal={m: ASHP for m in MATS}, **pose)              # se apaga en gris

def dead(kind, yaw):
    pose = {'curl': 1.0} if kind != 'head' else {'curl': 1.0, 'open_': .2, 'bob': -2.5}
    return render(kind, yaw, pal={m: ASHP for m in MATS}, **pose)

def death_smoke(k, n=6):
    """Humo gris que sube de una pieza muerta (24×40, bucle)."""
    cv = Canvas(24, 40)
    ph = k / n
    blobs = []
    for j in range(3):
        p = (ph + j / 3) % 1
        blobs.append((12 + math.sin(p * 5 + j) * 3, 33 - p * 24, 5 + p * 5, 1.0 - p * .8))
    RX.cloud(cv, blobs, RX.SMK)
    return cv

def comp(*ims):
    out = ims[0].copy()
    for im in ims[1:]: out.alpha_composite(im)
    return out

def piece_states(kind, ang, cache, pose, layers):
    """Pieza mirando a ang con sus capas encima (cuantiza a 16 direcciones y voltea si mira a la izquierda)."""
    q = round(ang / 22.5) * 22.5; q = (q + 180) % 360 - 180
    flip = abs(q) > 90
    base = (180 - q if q > 0 else -180 - q) if flip else q
    key = (kind, base, tuple(sorted(pose.items())), layers)
    if key not in cache:
        k = pose.get('_k', 0); pz = {a: b for a, b in pose.items() if a != '_k'}
        ims = [render(kind, base, **pz).img()] + [layer(L, kind, base, k, **pz).img() for L in layers]
        cache[key] = comp(*ims)
    im = cache[key]
    return im.transpose(Image.FLIP_LEFT_RIGHT) if flip else im

def states_curve(n=6):
    """La curva de la 6a con estados: un tramo ardiendo, otro chamuscado, un tramo abierto que late, grietas."""
    se = math.sin(ELEV); cache = {}
    w, h = 560, 400; cx, cy, R = w / 2, h / 2 + 10, 140.0
    pts = [(cx + R * math.cos(a), cy + R * se * math.sin(a)) for a in [-.4 - k * 2 * math.pi / 600 * .93 for k in range(600)]]
    nodes, acc, want = [], 0.0, 0.0
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        L = math.hypot(bx - ax, (by - ay) / se)
        while want <= acc + L and len(nodes) < 16:
            t = (want - acc) / L
            nodes.append((ax + (bx - ax) * t, ay + (by - ay) * t, math.degrees(math.atan2(-(ay - by) / se, ax - bx))))
            want += STEP_PX
        acc += L
    frog = Image.open(os.path.join(MT, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')).convert('RGBA').crop((0, 0, 32, 32))
    out = []
    for f in range(n):
        im = floor_bg(w, h, 5)
        for i in range(len(nodes) - 1, -1, -1):
            x, y, a = nodes[i]
            kind = 'head' if i == 0 else 'tail' if i == len(nodes) - 1 else 'seg'
            if kind == 'head': pose, layers = dict(HEAD_ANIMS['Move'][f], _k=f), ('Cracks2',)
            elif 2 <= i <= 4: pose, layers = dict(SEG_ANIMS['Walk'][(f + i) % 6], _k=f), ('Burning',)
            elif 5 <= i <= 7: pose, layers = dict(SEG_ANIMS['Walk'][(f + i) % 6], _k=f), ('Charred', 'Cracks1')
            elif 9 <= i <= 10: pose, layers = dict(OPEN_ANIMS['OpenWalk'][(f + i) % 6], _k=f), ()
            elif kind == 'seg': pose, layers = dict(SEG_ANIMS['Walk'][(f + i) % 6], _k=f), ('Cracks2',)
            else: pose, layers = dict(SEG_ANIMS['Walk'][(f + i) % 6], _k=f), ()
            spr = piece_states(kind, a, cache, pose, layers)
            im.alpha_composite(spr, (int(x - spr.width / 2), int(y - spr.height / 2)))
        im.alpha_composite(frog, (int(cx - 16), int(cy - 16)))
        out.append(im)
    return out

def estados():
    os.makedirs(os.path.join(OUT, 'Sheets'), exist_ok=True); os.makedirs(os.path.join(OUT, 'Effects'), exist_ok=True)
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    rows = {}
    # capas de estado sobre cada animación (mismos cuadros)
    for pname, kind, anims in (('Head', 'head', HEAD_ANIMS), ('Segment', 'seg', SEG_ANIMS), ('Tail', 'tail', TAIL_ANIMS)):
        for aname, poses in anims.items():
            for L in LAYERS:
                grid = [[layer(L, kind, yaw, k, **pz) for k, pz in enumerate(poses)] for _, yaw in DIRS]
                grid_sheet(grid).save(os.path.join(OUT, 'Sheets', f'Arthropleura_{pname}_{aname}_{L}.png'))
            print('  ', pname, aname, 'capas')
    # placas abiertas (solo segmentos)
    side = {}
    for aname, poses in OPEN_ANIMS.items():
        grid = [[render('seg', yaw, **pz) for pz in poses] for _, yaw in DIRS]
        grid_sheet(grid).save(os.path.join(OUT, 'Sheets', f'Arthropleura_Segment_{aname}.png'))
        side[f'Segment_{aname}'] = grid[4]
    # rueda
    grid = [[roll_shadow(render('roll', yaw, phase=k / 8)) for k in range(8)] for _, yaw in DIRS]
    grid_sheet(grid).save(os.path.join(OUT, 'Sheets', 'Arthropleura_Roll.png'))
    side['Roll'] = grid[4]; side['Roll (de frente)'] = grid[8]
    # muerte
    grid = [[death_frame(yaw, k) for k in range(8)] for _, yaw in DIRS]
    grid_sheet(grid).save(os.path.join(OUT, 'Sheets', 'Arthropleura_Head_Death.png'))
    side['Head_Death'] = grid[4]
    for pname, kind in (('Head', 'head'), ('Segment', 'seg'), ('Tail', 'tail')):
        g_ = [[dead(kind, yaw)] for _, yaw in DIRS]
        grid_sheet(g_).save(os.path.join(OUT, 'Sheets', f'Arthropleura_{pname}_Dead.png'))
        side[f'{pname}_Dead (3 direcciones)'] = [g_[j][0] for j in (0, 4, 8)]
    smoke = [death_smoke(k) for k in range(6)]
    save_sheet(os.path.join(OUT, 'Effects', 'Fx_Arthropleura_DeathSmoke.png'), smoke)
    side['Fx_Arthropleura_DeathSmoke'] = smoke
    # vista previa de las capas (de lado): pieza sola y con cada capa encima
    caps = {}
    for pname, kind, pz in (('Segment', 'seg', SEG_ANIMS['Walk'][0]), ('Head', 'head', HEAD_ANIMS['Move'][0])):
        base = render(kind, 0, **pz).img()
        row = [base]
        for L in LAYERS: row.append(comp(base, layer(L, kind, 0, 0, **pz).img()))
        row.append(comp(base, layer('Charred', kind, 0, 0, **pz).img(), layer('Cracks2', kind, 0, 0, **pz).img()))
        row.append(comp(base, layer('Charred', kind, 0, 0, **pz).img(), layer('Burning', kind, 0, 0, **pz).img()))
        caps[f'{pname}: sola, Burning, Charred, Cracks1, Cracks2, Charred+Cracks2, Charred+Burning'] = row
    preview(os.path.join(OUT, 'vista_previa_6b_capas.png'), caps, BG, scale=2)
    preview(os.path.join(OUT, 'vista_previa_6b_animaciones.png'), side, BG, scale=2)
    for name, fr in side.items():
        if len(fr) > 1 and '(' not in name: gif(os.path.join(OUT, 'gif', f'Arthropleura_{name}.gif'), fr, 90, BG, scale=3)
    curve = states_curve()
    curve[0].resize((curve[0].width * 2, curve[0].height * 2), Image.NEAREST).convert('RGB').save(
        os.path.join(OUT, 'vista_previa_6b_curva.png'))
    big = [f.resize((f.width * 2, f.height * 2), Image.NEAREST).convert('RGB') for f in curve]
    big[0].save(os.path.join(OUT, 'gif', 'Arthropleura_curva_estados.gif'), save_all=True, append_images=big[1:], duration=90, loop=0)
    print('Listo:', OUT)

# =================================================================================================
# 6c: cresta continua de frondas sobre la espesura (el cuerpo escondido)
# =================================================================================================
RIDGE_N = 112
RIDGE_PERIOD = STEP_PX / 3                                               # la textura casa entre tramos vecinos

def ridge_geo(phase=0.0, cap=False):
    """Tramo de cresta: lomo de frondas a lo largo de f (largo 2·a > paso de la cadena: se solapa con los vecinos),
    redondeado de lado a lado y con los extremos rectos; cap = remate redondeado por delante (cabeza o cola)."""
    g = Geo()
    a, b, h, capR = 25.0, 31.0, 10.0, 20.0
    step = .33
    fs = np.arange(-a, a + (capR if cap else 0) + step, step); ls = np.arange(-b, b + step, step)
    F, L = np.meshgrid(fs, ls); F = F.ravel(); L = L.ravel()
    k = 2 * math.pi / RIDGE_PERIOD; kb = 2 * math.pi / STEP_PX
    def width(F):                                                        # ancho deshilachado (se repite cada paso)
        w = b * (.84 + .1 * np.sin(F * k * 2 + .7) + .06 * np.sin(F * k * 3 + 2.1))
        if cap: w = w * np.sqrt(np.clip(1 - (np.clip(F - a, 0, None) / capR) ** 2, 0, 1))
        return w
    def fronds(F, L):
        return (1.6 * np.sin(F * k + 1.6 * np.sin(L * .33) + phase * 2 * math.pi)             # frondas en costillas
                + 1.1 * np.sin(F * k * 2.5 + L * .8) * np.sin(L * .55 - phase * 2 * math.pi))
    def zf(F, L):
        w = np.maximum(width(F), 1e-3)
        q = np.clip(1 - (L / w) ** 2, 0, 1)
        bumps = 1 + .2 * np.cos(F * kb)                                  # las placas de debajo, cada 2,6 u
        return h * q ** .55 * bumps * (1 + .05 * np.sin(phase * 2 * math.pi)) + fronds(F, L) * q ** .3
    w0 = width(F)
    rnd_ = (np.sin(F * 12.9898 + L * 78.233) * 43758.5453) % 1               # ruido fijo: el borde se deshace
    keep = (np.abs(L) <= w0) & ~((np.abs(L) > w0 * .78) & (rnd_ < (np.abs(L) / w0 - .78) * 3.5))
    F, L = F[keep], L[keep]
    Z = zf(F, L); d = .2
    zx = (zf(F + d, L) - zf(F - d, L)) / (2 * d); zy = (zf(F, L + d) - zf(F, L - d)) / (2 * d)
    mats = np.where(fronds(F, L) < -1.7, MI['canopydk'], MI['canopy'])   # huecos oscuros entre frondas
    g.add(np.stack([F, L, Z], 1), np.stack([-zx, -zy, np.ones_like(zx)], 1), mats, 'ridge')
    return g

def ridge(yaw, k, cap=False):
    return render_geo(ridge_geo(k / 4, cap), yaw, RIDGE_N, only=('canopy', 'canopydk'))

def maleza():
    os.makedirs(os.path.join(OUT, 'Sheets'), exist_ok=True)
    for name, cap in (('Arthropleura_CanopyRidge', False), ('Arthropleura_CanopyRidge_Cap', True)):
        grid = [[ridge(yaw, k, cap) for k in range(4)] for _, yaw in DIRS]
        grid_sheet(grid).save(os.path.join(OUT, 'Sheets', name + '.png'))
        print('  ', name)

if __name__ == '__main__':
    if 'maleza' in sys.argv[1:]: maleza()
    elif 'estados' in sys.argv[1:]: estados()
    elif 'todo' in sys.argv[1:]: todo()
    elif 'muestra' in sys.argv[1:] or len(sys.argv) == 1: muestra()
