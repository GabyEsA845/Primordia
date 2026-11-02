"""Meganeura, jefe 2 del piso 3 de Primordia (tanda 6d: cuerpo base). Skill primordia-sprites (pixel.py).

Libélula gigante del Carbonífero en picada alta (cámara a 55°, como el Arthropleura), con la referencia del usuario
(referencia_meganeura.png) y la paleta C aprobada en la 6d-0: cuerpo verde azulado con el dibujo de la referencia
(bandas oscuras en el abdomen, rayas ámbar en el tórax, cara ámbar, ojos enormes oscuros con reflejo rojo, patas verde
oliva con anillos) y alas PURAMENTE TRANSLÚCIDAS gris azulado con venación densa y pterostigma oscuro.

Modelado: el cuerpo con el motor de generar_arthropleura (elipsoides, tubos y esferas con z-buffer, misma luz y
contorno). Las alas son planos finos que giran en la raíz (aleteo): membrana semitransparente (el suelo se ve a
través), venas y borde más opacos; se mezclan con el cuerpo según la profundidad de cada píxel.

Celda 192×192, pivote = centro de la celda = centro del tórax. 10 u de envergadura (160 px).

Uso: python generar_meganeura.py todo      (6d: hojas en Sheets/ y Shadows/, vistas previas, GIF y escenas)
     python generar_meganeura.py escenas   (solo el vuelo en círculo y la escena del picado)
     python generar_meganeura.py muestra   (la muestra del aspecto en una dirección)
     python generar_meganeura.py poses     (prueba rápida: un cuadro de cada animación)
     python generar_meganeura.py variantes [Burnt Torn1 ...]   (6e-1: alas quemadas y rasgadas)
     python generar_meganeura.py solo <Animación>...          (regenera solo esas hojas)
"""
import math, os, random, sys
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
MT = os.path.normpath(os.path.join(HERE, '..', '..'))
sys.path.insert(0, os.path.join(MT, 'floor3-environment'))
sys.path.insert(0, os.path.join(HERE, '..', 'arthropleura'))
import generar_arthropleura as AR                      # Geo, tube, sphere, bez, render_geo (motor 3D)
from base_piso3 import *                               # noqa: F401,F403
from PIL import Image, ImageDraw

OUT = HERE
N = 192
ELEV = AR.ELEV
BG = C('#242024')                                      # suelo de la Tierra quemada

# --- paleta C (rampa de 5, de sombra a luz) ------------------------------------------------------
AR.PALS.update({
    'mbody':    ['#082830', '#0f4a56', '#1a7282', '#34a0aa', '#8edcd2'],   # verde azulado iridiscente
    'mjoint':   ['#05161a', '#0a2228', '#103440', '#184a56', '#24606a'],   # bandas oscuras de las uniones
    'mstripe':  ['#5a2a0e', '#9a5418', '#d08a2c', '#f0b850', '#ffe4a0'],   # rayas ámbar del tórax
    'mface':    ['#4a2a12', '#7a4a1e', '#b07a32', '#d8a850', '#f4d890'],   # cara ámbar apagado
    'mleg':     ['#1e2410', '#3a4418', '#64702a', '#94a040', '#c8d07a'],   # patas verde oliva
    'mlegband': ['#0e120a', '#181e10', '#262e18', '#38421f', '#4a5628'],   # anillos de las patas
    'mcerci':   ['#05161a', '#0a2228', '#103440', '#1e5662', '#34a0aa'],
    'mwood':    ['#2a1c14', '#4c3828', '#8e7048', '#b8955e', '#dcbc84'],   # madera astillada
    'mbark':    ['#141114', '#221c1e', '#352b2a', '#4c3e38', '#6a5848'],   # corteza carbonizada
})
AR.PALS['eye'] = ['#0a0608', '#160c10', '#28141a', '#4a2028', '#e8c8bc']   # ojo compuesto oscuro (reflejo rojo)
AR.MATS = list(AR.PALS)
AR.MI = {m: i for i, m in enumerate(AR.MATS)}
AR.PAL_C = [[C(c) for c in AR.PALS[m]] for m in AR.MATS]
MI = AR.MI

WING = dict(membrane=(170, 196, 214, 104), sheen=(226, 238, 246, 140), costal=(120, 144, 162, 140),
            vein=(34, 42, 52, 190), crossvein=(78, 92, 106, 118), edge=(26, 32, 40, 230), stig=(58, 40, 30, 255))
GLINT = (178, 232, 255, 215)                                    # reflejo azulado (presentación)

# =================================================================================================
# Cuerpo
# =================================================================================================
def ellipsoid(g, c, r, grp, matfn, pitch=0.0):
    """Elipsoide de radios r = (a lo largo, a lo ancho, alto); matfn(F, L, Z) -> índices de material."""
    rf, rl, rz = r
    n = max(14, int(math.pi * max(r) / .3))
    th, ph = np.meshgrid(np.linspace(0, math.pi, n), np.linspace(0, 2 * math.pi, 2 * n))
    u = np.stack([np.sin(th) * np.cos(ph), np.sin(th) * np.sin(ph), np.cos(th)], -1).reshape(-1, 3)
    P = u * np.array([rf, rl, rz]); Nn = u / np.array([rf, rl, rz])
    mats = matfn(P[:, 0], P[:, 1], P[:, 2])
    cp, sp = math.cos(math.radians(pitch)), math.sin(math.radians(pitch))
    rot = lambda A: np.stack([A[:, 0] * cp - A[:, 2] * sp, A[:, 1], A[:, 0] * sp + A[:, 2] * cp], 1)
    g.add(rot(P) + np.asarray(c, float), rot(Nn), mats, grp)

def const(m): return lambda F, L, Z: np.full(len(F), MI[m])

def thorax_mats(F, L, Z):
    m = np.full(len(F), MI['mbody'])
    q = F + 1.5 * np.abs(L)                                    # rayas oblicuas a los lados
    side = np.abs(L) > 3
    m[side & (np.abs(q - 6) < 1.8)] = MI['mstripe']
    m[side & (np.abs(q - 12.5) < 1.4)] = MI['mjoint']
    m[side & (np.abs(q + 2) < 1.5)] = MI['mstripe']
    m[(np.abs(L) < 1.3) & (Z > 5)] = MI['mjoint']             # quilla oscura del lomo
    return m

ABD_SEGS = 9
def abd_mat(t):
    if t < .06: return 'mbody'
    k = ((t - .06) / .94 * ABD_SEGS) % 1
    return 'mjoint' if k > .78 else 'mbody'

LEGS = ((11, 15, 9), (5, 7, 15), (-1, -2, 16))                  # (raíz en F, punta hacia delante, punta hacia fuera)

def leg(g, f0, side, grp, fwd=8, out=12, fold=0.0):
    """Pata colgando bajo el tórax (como en la referencia): cadera, rodilla hacia fuera, tibia y tarso. Cada par
    apunta a un sitio distinto para que se lean separadas."""
    k = 1 - .45 * fold
    pts = [(f0, side * 5, -9), (f0 + fwd * .25, side * (out * .8 + 3), -12),
           (f0 + fwd * .65 * k, side * (out + 2) * k, -19 + 4 * fold), (f0 + fwd * k, side * out * k, -23 + 6 * fold)]
    AR.tube(g, AR.bez(pts), 1.6, .7, grp, lambda t: 'mlegband' if (t * 6) % 1 > .7 else 'mleg')

def body_geo(fold=0.0, curl=0.0, bury=None, jaw=0.0):
    g = AR.Geo(); z = 0.0
    ellipsoid(g, (0, 0, z), (16, 12.5, 12), 'thorax', thorax_mats, pitch=-8)
    ellipsoid(g, (14, 0, z + 1.5), (4.5, 6.5, 5.5), 'neck', const('mbody'))
    for s_ in (-1, 1): AR.sphere(g, (20.5, s_ * 5.6, z + 3), 7.6, f'eye{s_}', 'eye')
    ellipsoid(g, (26.5, 0, z - 1.5), (4, 5.8, 4.8), 'face', const('mface'))
    AR.sphere(g, (29, 0, z - 5), 2.3, 'mouth', 'mjoint')
    if jaw:                                                     # mandíbulas que se abren de lado (chillido)
        for s_ in (-1, 1):
            AR.tube(g, AR.bez([(28, s_ * 2, z - 5), (32, s_ * (3 + 4 * jaw), z - 6), (36, s_ * (2 + 7 * jaw), z - 7)]),
                    1.9, .7, f'jaw{s_}', lambda t: 'mface' if t < .6 else 'mjoint')
    ellipsoid(g, (-15, 0, z - 1), (7, 6.2, 5.8), 'abd0', const('mbody'))
    c = curl                                                    # 1 = la punta baja hasta el agua (poner huevos)
    ctrl = [(-15, 0, z - 1), (-42 + 2 * c, 0, z - 3 * c), (-60 + 14 * c, 0, z - 1 - 26 * c), (-80 + 34 * c, 0, z - 4 - 44 * c)]
    path = AR.bez(ctrl)
    AR.tube(g, path, 5.0, 2.6, 'abd', abd_mat)
    tip, d = np.array(path[-1]), np.array(path[-1]) - np.array(path[-6])
    d = d / np.linalg.norm(d)
    side_v = np.array([0, 1.0, 0])
    for s_ in (-1, 1):
        AR.tube(g, AR.bez([tip, tip + d * 4 + side_v * s_ * 1.5, tip + d * 8 + side_v * s_ * 3.2]), 1.1, .6, f'cerc{s_}', lambda t: 'mcerci')
    for i, (f0, fwd, out) in enumerate(LEGS):
        for s_ in (-1, 1): leg(g, f0, s_, f'leg{i}{s_}', fwd, out, fold)
    if bury is not None:                                        # lo que pasa de F = bury está dentro del tronco
        for i in range(len(g.P)):
            keep = g.P[i][:, 0] <= bury
            g.P[i], g.N[i], g.M[i], g.G[i] = g.P[i][keep], g.N[i][keep], g.M[i][keep], g.G[i][keep]
        rnd = random.Random(7)
        for k in range(14):                                     # astillas que salen alrededor del cuello
            a = k / 14 * 2 * math.pi + rnd.uniform(-.15, .15)
            r0 = rnd.uniform(7, 11); ln = rnd.uniform(6, 12)
            base = np.array([bury, r0 * math.cos(a), r0 * math.sin(a) + 1])
            tip = base + np.array([-ln * .7, math.cos(a) * ln * .7, math.sin(a) * ln * .7])
            AR.tube(g, AR.bez([base, (base + tip) / 2 + np.array([0, 0, .5]), tip]), 2.2, .5, f'spl{k}',
                    (lambda t, m=('mwood' if k % 3 else 'mbark'): m), cap=False)
        AR.sphere(g, (bury + 1.5, 0, 1), 8.5, 'hole', 'mbark', squash=.9)   # boca del agujero en la madera
    g.P = [xf(P) for P in g.P]; g.N = [xf_n(n_) for n_ in g.N]
    return g

def body_depth(g, yaw, n=N, origin=None):
    """Profundidad del cuerpo por píxel con la misma proyección que render_geo (menor = más cerca)."""
    P, Nn, M, G = g.arrays()
    y = math.radians(yaw); cy, sy = math.cos(y), math.sin(y)
    X = P[:, 0] * cy - P[:, 1] * sy; Y = P[:, 0] * sy + P[:, 1] * cy; Z = P[:, 2]
    se, ce = math.sin(ELEV), math.cos(ELEV)
    ox, oy = origin or (n / 2, n / 2)
    ix = np.floor(X + ox).astype(int); iy = np.floor(-(Y * se + Z * ce) + oy).astype(int)
    d = Y * ce - Z * se
    ok = (ix >= 0) & (iy >= 0) & (ix < n) & (iy < n)
    D = np.full((n, n), 1e9)
    np.minimum.at(D, (iy[ok], ix[ok]), d[ok])
    return D

# =================================================================================================
# Alas: planos translúcidos que giran en la raíz
# =================================================================================================
SPAN = 160
XF = dict(pitch=0.0, bob=0.0, roll=0.0)                                   # pose de todo el cuerpo (la fija render/shadow)

def xf(P):
    """Cabeceo alrededor del centro del tórax (eje L) y subida/bajada."""
    P = _roll(np.asarray(P, float).reshape(-1, 3))
    p = math.radians(XF['pitch']); c, s_ = math.cos(p), math.sin(p)
    return np.stack([P[:, 0] * c - P[:, 2] * s_, P[:, 1], P[:, 0] * s_ + P[:, 2] * c + XF['bob']], 1)

def _roll(P):
    """Giro sobre el eje del cuerpo (F): 180 = boca arriba."""
    r = math.radians(XF['roll'])
    if not r: return P
    c, s_ = math.cos(r), math.sin(r)
    return np.stack([P[:, 0], P[:, 1] * c - P[:, 2] * s_, P[:, 1] * s_ + P[:, 2] * c], 1)

def xf_n(Nn):
    Nn = _roll(Nn)
    p = math.radians(XF['pitch']); c, s_ = math.cos(p), math.sin(p)
    return np.stack([Nn[:, 0] * c - Nn[:, 2] * s_, Nn[:, 1], Nn[:, 0] * s_ + Nn[:, 2] * c], 1)
WINGS = {                                                       # raíz (F, L, Z), largo, barrido, cuerda base/medio
    'fore': dict(root=(7, 3, 11), L=76.5, sweep=12, c0=7, cm=14),
    'hind': dict(root=(-5, 3, 10), L=72.5, sweep=-15, c0=16, cm=16),
}

def chord(w, t):
    c0, cm = w['c0'], w['cm']
    ch = c0 + (cm - c0) * math.sin(min(1, t / .45) * math.pi / 2) - cm * .25 * max(0, t - .45) / .55
    if t > .82: ch *= math.sqrt(max(0, 1 - ((t - .82) / .18) ** 2)) * .75 + .25
    return ch

def lead(w, t): return w['sweep'] * t + 2 * math.sin(t * math.pi)

def wing_point(w, side, t, v, flap):
    """t = 0 raíz … 1 punta; v = 0 borde de ataque … 1 borde de salida; flap en grados (+ = arriba);
    w['sw'] = barrido hacia atrás en grados (picado: alas pegadas hacia atrás)."""
    rf, rl, rz = w['root']
    a_ = lead(w, t) - v * chord(w, t)                           # a lo largo del cuerpo, desde la raíz
    s = t * w['L']
    a = math.radians(flap); ph = math.radians(w.get('sw', 0.0))
    h = s * math.cos(a)
    F = rf + a_ * math.cos(ph) - h * math.sin(ph)
    L = rl + a_ * math.sin(ph) + h * math.cos(ph)
    return tuple(xf([(F, side * L, rz + s * math.sin(a))])[0])

def project(pts, yaw, n=N, origin=None):
    P = np.asarray(pts, float)
    y = math.radians(yaw); cy, sy = math.cos(y), math.sin(y)
    X = P[:, 0] * cy - P[:, 1] * sy; Y = P[:, 0] * sy + P[:, 1] * cy; Z = P[:, 2]
    se, ce = math.sin(ELEV), math.cos(ELEV)
    ox, oy = origin or (n / 2, n / 2)
    return X + ox, -(Y * se + Z * ce) + oy, Y * ce - Z * se

# --- daño de las alas (6e-1): rasgones por fase y agujeros quemados (permanentes) -------------------
# Rasgón: muesca desde el borde de salida (t0 = dónde, prof = fracción de la cuerda). Punta cortada: falta la punta.
TEARS = {
    1: {('fore', 1): [(.62, .45)], ('hind', -1): [(.48, .5)]},
    2: {('fore', 1): [(.62, .55), (.84, .6)], ('fore', -1): [(.42, .4)], ('hind', -1): [(.48, .6), (.74, .65)],
        ('hind', 1): [(.3, .45), (.66, .4)]},
}
TIPCUT = {2: {('fore', -1): .86, ('hind', 1): .9}}
BURNS = {('fore', -1): [(.38, .5, 4.5), (.7, .35, 3.0)], ('hind', 1): [(.56, .45, 5.0)], ('fore', 1): [(.24, .62, 3.0)],
         ('hind', -1): [(.82, .4, 3.5)]}                           # (t, v, radio en px)
CHARC = (44, 28, 20, 235)                                         # borde quemado
SINGE = (150, 118, 80, 120)                                       # membrana chamuscada alrededor

def chord_np(w, T):
    c0, cm = w['c0'], w['cm']
    ch = c0 + (cm - c0) * np.sin(np.minimum(1, T / .45) * math.pi / 2) - cm * .25 * np.maximum(0, T - .45) / .55
    k = np.sqrt(np.clip(1 - ((T - .82) / .18) ** 2, 0, 1)) * .75 + .25
    return np.where(T > .82, ch * k, ch)

def wing_damage(w, side, T, V, dmg):
    """Código por punto: 0 sano, 1 agujero, 2 borde de rasgón, 3 borde quemado, 4 chamuscado."""
    code = np.zeros(len(T), int)
    if not dmg: return code
    key = (w['name'], side)
    L = w['L']; ch = chord_np(w, T)
    torn = dmg.get('torn', 0)
    for t0, dep in TEARS.get(torn, {}).get(key, []):
        e = (1 - V) * ch                                          # px desde el borde de salida
        u = np.abs(T - t0) * L
        W = 3.5 + 1.2 * np.sin(e * 1.7 + t0 * 9)                  # dientes
        lim = dep * w['cm'] * (1 - u / W)
        code[(e < lim)] = 1
        code[(code == 0) & (e < lim + 1.1) & (u < W + 1)] = 2
    cut = TIPCUT.get(torn, {}).get(key)
    if cut:
        edge_t = cut - .06 * V + .015 * np.sin(V * 17)
        code[T > edge_t] = 1
        code[(code == 0) & (T > edge_t - 1.2 / L)] = 2
    if dmg.get('burnt'):
        for tc, vc, r in BURNS.get(key, []):
            dx = (T - tc) * L; dy = (V - vc) * ch
            d = np.hypot(dx, dy)
            rr = r * (1 + .22 * np.sin(np.arctan2(dy, dx) * 5 + tc * 7))
            code[d < rr] = 1
            code[(code == 0) & (d < rr + 1.3)] = 3
            code[(code == 0) & (d < rr + 3.8)] = 4
    return code

def wing_layer(w, side, flap, yaw, seed=0, dmg=None, glint=None, elec=None):
    """Píxeles del ala: {(x, y): (profundidad, rgba)} con membrana, brillo, venas, borde y pterostigma.
    dmg = {'torn': 0/1/2, 'burnt': bool}; glint = posición (0-1) del reflejo azulado que recorre el ala (presentación)."""
    px = {}
    def put(xs, ys, ds, cols, prio):
        for x, y, d, col, pr in zip(np.floor(xs).astype(int), np.floor(ys).astype(int), ds, cols, prio):
            if not (0 <= x < N and 0 <= y < N): continue
            cur = px.get((x, y))
            if cur is None or pr > cur[2] or (pr == cur[2] and d < cur[0]): px[(x, y)] = (d, col, pr)
    a = math.radians(flap)
    Ts, Vs, cols, prios, kinds = [], [], [], [], []               # kind: 0 membrana, 1 vena
    def add(T, V, col, prio, kind):
        T = np.asarray(T, float).ravel(); V = np.asarray(V, float).ravel()
        Ts.append(T); Vs.append(V); cols.extend([col] * len(T)); prios.append(np.full(len(T), prio)); kinds.append(np.full(len(T), kind))
    # membrana (con una franja de brillo oblicua que se mueve con el aleteo)
    T, V = np.meshgrid(np.linspace(0, 1, 260), np.linspace(0, 1, 40)); T = T.ravel(); V = V.ravel()
    band = np.abs((T - V * .35) - (.45 + .18 * side * math.sin(a))) < .07
    costal = V < .12
    for m, col in ((~band & ~costal, WING['membrane']), (band & ~costal, WING['sheen']), (costal, WING['costal'])):
        add(T[m], V[m], col, 0, 0)
    if glint is not None:                                         # reflejo azulado que recorre el ala
        m = np.abs((T - V * .3) - glint) < .13
        add(T[m], V[m], GLINT, 0.5, 0)
        m2 = np.abs((T - V * .3) - glint) < .05
        add(T[m2], V[m2], (235, 250, 255, 240), 3, 0)   # filo del reflejo, encima de las venas
    # venas longitudinales y borde
    ts = np.linspace(0, 1, 520)
    add(ts, np.zeros_like(ts), WING['edge'], 4, 1)                # costa (borde de ataque)
    add(ts, np.ones_like(ts), WING['edge'], 4, 1)                 # borde de salida
    for v in (.12, .32, .55, .78):
        tt = ts[ts <= .96]; add(tt, np.full_like(tt, v), WING['vein'], 2, 1)
    # venas cruzadas en ladrillo (red densa como en la referencia)
    rnd = random.Random(seed)
    vs = (.12, .32, .55, .78, 1.0)
    step = 5.5 / w['L']
    for k in range(4):
        t = .03 + rnd.random() * step + (step / 2) * (k % 2)
        while t < .97:
            vv = np.linspace(vs[k], vs[k + 1], 14)
            add(t + .004 * np.arange(14), vv, WING['crossvein'], 1, 1)
            t += step * (1.0 + .25 * rnd.random())
    vv = np.linspace(0, 1, 30); add(np.ones_like(vv), vv, WING['edge'], 4, 1)   # punta
    TT, VV = np.meshgrid(np.linspace(.82, .89, 14), np.linspace(.02, .2, 8))   # pterostigma
    add(TT, VV, WING['stig'], 5, 1)
    T = np.concatenate(Ts); V = np.concatenate(Vs); P = np.concatenate(prios); K = np.concatenate(kinds)
    code = wing_damage(w, side, T, V, dmg)
    cols = list(cols)
    if elec:                                                    # electrocutada: membrana azul y venas blancas
        mem, vein = ((120, 190, 255, 150), (235, 250, 255, 235)) if elec == 1 else ((40, 80, 160, 120), (150, 210, 255, 220))
        cols = [mem if k == 0 else vein for k in K]
    for i in np.nonzero(code)[0]:
        c = code[i]
        if c == 2: cols[i] = WING['edge']; P[i] = 4
        elif c == 3: cols[i] = CHARC; P[i] = 4
        elif c == 4 and K[i] == 0: cols[i] = SINGE
    keep = code != 1
    idx = np.nonzero(keep)[0]
    xs, ys, ds = project([wing_point(w, side, T[i], V[i], flap) for i in idx], yaw)
    put(xs, ys, ds, [cols[i] for i in idx], P[idx])
    return {k: (d, c) for k, (d, c, p) in px.items()}

def blend_over(dst, src):
    sa = src[3] / 255; da = dst[3] / 255
    oa = sa + da * (1 - sa)
    if oa <= 0: return (0, 0, 0, 0)
    return tuple(int(round((src[i] * sa + dst[i] * da * (1 - sa)) / oa)) for i in range(3)) + (int(round(oa * 255)),)

EYE_GLOW = ['#140406', '#2e080a', '#6a120e', '#c0301e', '#ffc8a0']   # ojos encendidos (aviso del picado)

def set_pose(pose):
    XF['pitch'] = pose.get('pitch', 0.0); XF['bob'] = pose.get('bob', 0.0); XF['roll'] = pose.get('roll', 0.0)

def wing_set(pose):
    return (('fore', pose.get('fore', 5.0), pose.get('fsw', 0.0)), ('hind', pose.get('hind', 5.0), pose.get('hsw', 0.0)))

def render(yaw=0.0, **pose):
    """pose: fore/hind (aleteo en grados), fsw/hsw (barrido atrás), pitch (morro arriba +), bob, fold (patas
    recogidas 1 / estiradas -1), curl (abdomen hacia abajo), eye (ojos encendidos), seed."""
    set_pose(pose)
    g = body_geo(fold=pose.get('fold', 0.0), curl=pose.get('curl', 0.0), bury=pose.get('bury'), jaw=pose.get('jaw', 0.0))
    pal = {'eye': EYE_GLOW} if pose.get('eye') else None
    if pose.get('elec'): pal = {m: (ELEC if pose['elec'] == 1 else ELEC_DK) for m in AR.MATS}
    if pose.get('dead'): pal = DEAD_PAL
    cv = AR.render_geo(g, yaw, N, pal=pal)
    body = cv.img().convert('RGBA')
    D = body_depth(g, yaw)
    bp = body.load()
    for (x, y) in list(cv.px):                                  # el contorno no tiene profundidad: la del vecino
        if D[y, x] >= 1e9:
            nb = [D[y + b, x + a] for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)) if 0 <= x + a < N and 0 <= y + b < N]
            D[y, x] = min(nb) if nb else 0
    layers = []
    seed = pose.get('seed', 1)
    for name, flap, sw_ in wing_set(pose):
        w = dict(WINGS[name], sw=sw_, name=name)
        for side in (-1, 1):
            if pose.get('nowing') == (name, side): continue
            layers.append(wing_layer(w, side, flap, yaw, seed=seed + (side > 0) + 2 * (name == 'hind'),
                                     dmg=pose.get('dmg'), glint=pose.get('glint'), elec=pose.get('elec')))
    out = Image.new('RGBA', (N, N), (0, 0, 0, 0)); op = out.load()
    keys = set(cv.px) | set().union(*[set(l) for l in layers])
    for (x, y) in keys:
        stack = []
        if (x, y) in cv.px: stack.append((D[y, x], bp[x, y], True))
        for l in layers:
            if (x, y) in l: stack.append((l[(x, y)][0], l[(x, y)][1], False))
        stack.sort(key=lambda s: -s[0])                         # de lejos a cerca
        col = (0, 0, 0, 0)
        for d, c, opaque in stack: col = c if opaque else blend_over(col, c)
        op[x, y] = col
    if pose.get('elec'): bolts(out, seed=pose.get('seed', 1) * 31 + int(yaw), bright=pose['elec'] == 1)
    return out

def shadow(yaw=0.0, **pose):
    """Sombra en el suelo: cuerpo y alas aplastados (sin altura), negro opaco; el código le da la transparencia y la
    coloca bajo el jefe (el pivote es el mismo punto: el centro del tórax proyectado al suelo)."""
    set_pose(dict(pose, bob=0.0))
    g = body_geo(fold=pose.get('fold', 0.0), curl=pose.get('curl', 0.0), bury=pose.get('bury'), jaw=pose.get('jaw', 0.0))
    P, _, _, _ = g.arrays()
    pts = [P]
    for name, flap, sw_ in wing_set(pose):
        w = dict(WINGS[name], sw=sw_)
        for side in (-1, 1):
            if pose.get('nowing') == (name, side): continue
            pts.append(np.array([wing_point(w, side, t, v, flap) for t in np.linspace(0, 1, 160) for v in np.linspace(0, 1, 16)]))
    P = np.vstack(pts); P[:, 2] = 0
    xs, ys, _ = project(P, yaw)
    im = Image.new('RGBA', (N, N), (0, 0, 0, 0)); p = im.load()
    for x, y in zip(np.floor(xs).astype(int), np.floor(ys).astype(int)):
        if 0 <= x < N and 0 <= y < N: p[x, y] = (10, 6, 12, 255)
    return im

# =================================================================================================
# Muestra
# =================================================================================================
FLY = [(28, 4), (8, 28), (-20, 8), (2, -20)]                    # aleteo (anterior, posterior): a contrafase

def burnt_floor(w, h, seed=3):
    tiles = os.path.join(MT, 'floor3-environment', 'Burnt', 'tiles')
    fl = [Image.open(os.path.join(tiles, f'Floor_{k}.png')).convert('RGBA') for k in range(6)]
    rnd = random.Random(seed); im = Image.new('RGBA', (w, h))
    for y in range(0, h, 16):
        for x in range(0, w, 16): im.alpha_composite(fl[0 if rnd.random() < .66 else rnd.randint(1, 5)], (x, y))
    return im

def with_shadow(bg, spr, sh, x, y, alt, a=110):
    s = sh.copy(); s.putalpha(s.getchannel('A').point(lambda v: a if v else 0))
    bg.alpha_composite(s, (x - N // 2, y - N // 2))
    bg.alpha_composite(spr, (x - N // 2, y - N // 2 - alt))

def muestra():
    font = AR.__dict__.get('_font', None)
    glide = render(0)
    flaps = [render(0, fore=f, hind=h, bob=.6 * math.sin(i / 4 * 2 * math.pi)) for i, (f, h) in enumerate(FLY)]
    sh = shadow(0)
    # lámina
    s = 3
    ref = Image.open(os.path.join(HERE, 'referencia_meganeura.png')).convert('RGBA')
    ref = ref.resize((int(ref.width * 300 / ref.height), 300))
    big = glide.resize((N * s, N * s), Image.NEAREST)
    strip = Image.new('RGBA', (N * 2 * 4, N * 2), (0, 0, 0, 0))
    for i, f in enumerate(flaps): strip.alpha_composite(f.resize((N * 2, N * 2), Image.NEAREST), (i * N * 2, 0))
    scene = burnt_floor(320, 200)
    frog = Image.open(os.path.join(MT, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')).convert('RGBA').crop((0, 0, 32, 32))
    with_shadow(scene, glide, sh, 210, 120, 22)
    scene.alpha_composite(frog, (60, 120))
    scene2 = scene.resize((scene.width * 2, scene.height * 2), Image.NEAREST)
    W = max(ref.width + big.width + 60, strip.width + 40, scene2.width + 40)
    H = 40 + max(ref.height, big.height) + 50 + strip.height + 50 + scene2.height + 30
    full = Image.new('RGBA', (W, H), BG + (255,) if len(BG) == 3 else BG)
    d = ImageDraw.Draw(full)
    fnt = AR.__dict__.get('font', None)
    from generar_escala_arthropleura import font as F
    y = 10
    d.text((20, y), 'Meganeura · 6d muestra: dirección Side (mirando a la derecha), picada 55°, paleta C con alas translúcidas', fill=(255, 240, 200), font=F(20))
    y += 34
    full.alpha_composite(ref, (20, y + (big.height - ref.height) // 2))
    full.alpha_composite(big, (ref.width + 40, y))
    d.text((20, y + (big.height + ref.height) // 2 + 6), 'referencia del usuario', fill=(200, 210, 200), font=F(15))
    d.text((ref.width + 40, y + big.height - 22), f'planeo ×{s} (celda {N}×{N})', fill=(200, 210, 200), font=F(15))
    y += big.height + 20
    d.text((20, y), 'Fly: aleteo de 4 cuadros (×2), alas anteriores y posteriores a contrafase', fill=(255, 240, 200), font=F(17))
    y += 28
    full.alpha_composite(strip, (20, y)); y += strip.height + 20
    d.text((20, y), 'En el juego (×2): a ras de suelo con su sombra, junto a la rana de 32 px', fill=(255, 240, 200), font=F(17))
    y += 28
    full.alpha_composite(scene2, (20, y))
    full.convert('RGB').save(os.path.join(OUT, 'vista_previa_6d_muestra.png'))
    os.makedirs(os.path.join(OUT, 'gif', 'escenas'), exist_ok=True)
    gif(os.path.join(OUT, 'gif', 'muestra_Fly.gif'), flaps, 50, BG, scale=2)
    print('Listo:', OUT)

# =================================================================================================
# 6d completa: animaciones en 16 direcciones (9 dibujadas + Flip X)
# =================================================================================================
DIRS = AR.DIRS
def sw(i, n, k=1.0): return math.sin(i / n * 2 * math.pi * k)

ANIMS = {
    # vuelo normal (subir, bajar, cruzar la sala a media altura, presentación)
    'Fly':      [dict(fore=f, hind=h, bob=.6 * sw(i, 4)) for i, (f, h) in enumerate(FLY)],
    # cernido a ras de suelo: aleteo corto y rápido, morro algo arriba, patas colgando
    'Hover':    [dict(fore=f, hind=h, pitch=6, bob=1.0 * sw(i, 4), fold=-.3) for i, (f, h) in
                 enumerate(((16, -4), (-4, 16), (-14, 4), (4, -14)))],
    # prepara el picado: se encoge, alas arriba y atrás, morro hacia el blanco, ojos encendidos
    'DiveWarn': [dict(fore=f, hind=f - 4, fsw=b, hsw=b + 4, pitch=p_, fold=fo, eye=i >= 2, bob=-.5 * i) for i, (f, b, p_, fo) in
                 enumerate(((24, 8, -4, .2), (34, 18, -10, .5), (40, 26, -16, .8), (42, 30, -20, 1.0)))],
    # picado: alas pegadas hacia atrás que vibran, morro abajo, patas por delante para agarrar
    'Dive':     [dict(fore=f, hind=f - 3, fsw=52, hsw=58, pitch=-26, fold=-.4, eye=True) for f in (10, 14, 7)],
    # remonta: aletazos amplios, morro arriba, patas recogidas
    'Climb':    [dict(fore=f, hind=h, pitch=16, fold=1.0, bob=.8 * sw(i, 4)) for i, (f, h) in
                 enumerate(((44, -10), (8, 44), (-36, 10), (0, -36)))],
    # aleteo fuerte que empuja (gas, veneno, proyectiles): se alza y bate; el empujón sale en el cuadro 3
    'Gust':     [dict(fore=f, hind=h, pitch=22, fold=.3, bob=b) for f, h, b in
                 ((30, 20, 0), (52, 46, 1), (40, 52, 1.5), (-28, -20, -1), (-36, -32, -1.5), (-8, -16, 0))],
    # se posa: frena, alas que se aplanan, patas que se estiran hacia el suelo
    'Land':     [dict(fore=f, hind=h, pitch=p_, fold=fo) for f, h, p_, fo in ((28, 10, 10, -.3), (12, 20, 5, -.7), (-2, 0, 0, -1.0))],
    # posada (presentación en el tronco, F2 junto a la charca): alas planas y algo caídas que tiemblan
    'Perch':    [dict(fore=-6 + d, hind=-8 - d, fold=-1.0, bob=-.4 * abs(sw(i, 4))) for i, d in enumerate((0, 2, 0, -2))],
    # pone huevos: posada, el abdomen baja hasta el agua y suelta el huevo (cuadro 3)
    'Lay':      [dict(fore=-6, hind=-8, fold=-1.0, curl=c) for c in (.2, .5, .85, 1.0, .9, .45)],
}
MS = {'Fly': 50, 'Hover': 40, 'DiveWarn': 120, 'Dive': 50, 'Climb': 60, 'Gust': 80, 'Land': 90, 'Perch': 200,
      'Lay': 120, 'Hurt': 80}

def hurt(im):
    """Hurt de 2 cuadros (regla de la skill): silueta blanca y luego roja con RED4; contorno y transparencia intactos."""
    out = []
    for kind in ('white', 'red'):
        o = im.copy(); p = o.load()
        for y in range(o.height):
            for x in range(o.width):
                c = p[x, y]
                if not c[3] or c[:3] == OUTL[:3]: continue
                if kind == 'white': p[x, y] = (255, 255, 255, c[3])
                else:
                    lum = (c[0] * .3 + c[1] * .59 + c[2] * .11) / 255
                    r = C(RED4[0 if lum < .14 else 1 if lum < .26 else 2 if lum < .45 else 3])
                    p[x, y] = r[:3] + (c[3],)
        out.append(o)
    return out

def grid(rows):
    im = Image.new('RGBA', (N * max(len(r) for r in rows), N * len(rows)), (0, 0, 0, 0))
    for y, r in enumerate(rows):
        for x, f in enumerate(r): im.paste(f, (x * N, y * N))
    return im

def sprite_for(name, k, ang, cache, shadow_=False):
    """Cuadro k de la animación name mirando a ang (0 = derecha, 90 = arriba): cuantiza a 16 y voltea."""
    q = round(ang / 22.5) * 22.5
    q = (q + 180) % 360 - 180
    flip = abs(q) > 90
    base = (180 - q if q > 0 else -180 - q) if flip else q
    key = (name, k, base, shadow_)
    if key not in cache:
        pose = ANIMS[name][k]
        cache[key] = shadow(base, **pose) if shadow_ else render(base, **pose)
    im = cache[key]
    return im.transpose(Image.FLIP_LEFT_RIGHT) if flip else im

def poses():
    """Prueba rápida: un cuadro de cada animación en Side y Dn45."""
    rows = {}
    for yaw in (0, -45):
        rows[f'yaw {yaw}'] = [render(yaw, **ANIMS[n][min(2, len(ANIMS[n]) - 1)]) for n in ANIMS]
    preview(os.path.join(OUT, 'vista_previa_6d_poses.png'), rows, BG, scale=2)
    print('poses:', ' · '.join(ANIMS))

def todo():
    os.makedirs(os.path.join(OUT, 'Sheets'), exist_ok=True); os.makedirs(os.path.join(OUT, 'Shadows'), exist_ok=True)
    os.makedirs(os.path.join(OUT, 'gif', 'escenas'), exist_ok=True)
    side, cache = {}, {}
    hurt_rows, hurt_sh = [], []
    for name, poses_ in ANIMS.items():
        rows, srows = [], []
        for dname, yaw in DIRS:
            frames = [render(yaw, **pz) for pz in poses_]
            rows.append(frames); srows.append([shadow(yaw, **pz) for pz in poses_])
            for k, f in enumerate(frames): cache[(name, k, yaw, False)] = f
            for k, f in enumerate(srows[-1]): cache[(name, k, yaw, True)] = f
            if dname == 'Side': side[name] = frames
            if name == 'Fly': hurt_rows.append(hurt(frames[0])); hurt_sh.append([srows[-1][0]] * 2)
        grid(rows).save(os.path.join(OUT, 'Sheets', f'Meganeura_{name}.png'))
        grid(srows).save(os.path.join(OUT, 'Shadows', f'Meganeura_Shadow_{name}.png'))
        print('  ', name, flush=True)
    grid(hurt_rows).save(os.path.join(OUT, 'Sheets', 'Meganeura_Hurt.png'))
    grid(hurt_sh).save(os.path.join(OUT, 'Shadows', 'Meganeura_Shadow_Hurt.png'))
    side['Hurt'] = hurt_rows[4]
    preview(os.path.join(OUT, 'vista_previa_6d_animaciones.png'), side, BG, scale=2)
    dirs = {f'{n} (9 direcciones)': [cache[(n, k, yaw, False)] for _, yaw in DIRS] for n, k in (('Fly', 0), ('Dive', 0), ('Perch', 0))}
    preview(os.path.join(OUT, 'vista_previa_6d_direcciones.png'), dirs, BG, scale=1)
    for name, frames in side.items(): gif(os.path.join(OUT, 'gif', f'Meganeura_{name}.gif'), frames, MS[name], BG, scale=2)
    escenas(cache)
    print('Listo:', OUT)

def solo(names):
    """Regenera solo las hojas, sombras y GIF de las animaciones indicadas."""
    for name in names:
        rows, srows = [], []
        for dname, yaw in DIRS:
            rows.append([render(yaw, **pz) for pz in ANIMS[name]]); srows.append([shadow(yaw, **pz) for pz in ANIMS[name]])
            if dname == 'Side': gif(os.path.join(OUT, 'gif', f'Meganeura_{name}.gif'), rows[-1], MS[name], BG, scale=2)
        grid(rows).save(os.path.join(OUT, 'Sheets', f'Meganeura_{name}.png'))
        grid(srows).save(os.path.join(OUT, 'Shadows', f'Meganeura_Shadow_{name}.png'))
        print('  ', name, flush=True)

def escenas(cache=None):
    cache = {} if cache is None else cache
    curva(cache); picado(cache)

FROG_IM = None
def frog():
    global FROG_IM
    if FROG_IM is None:
        FROG_IM = Image.open(os.path.join(MT, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')).convert('RGBA').crop((0, 0, 32, 32))
    return FROG_IM

def place(im, spr, x, y, alt=0, sh=None, sh_a=110, dither=False):
    """Pone la sombra en (x, y) del suelo y el jefe encima, subido alt px de altura (en pantalla, alt x cos 55)."""
    if sh is not None:
        s_ = sh.copy(); a = s_.getchannel('A')
        if dither:                                              # vuela alto: sombra tramada (más suave)
            px = a.load()
            for j in range(a.height):
                for i in range(a.width):
                    if px[i, j] and BAYER[j % 4][i % 4] >= 11: px[i, j] = 0
        s_.putalpha(a.point(lambda v: sh_a if v else 0))
        im.alpha_composite(s_, (int(x - N / 2), int(y - N / 2)))
    if spr is not None:
        im.alpha_composite(spr, (int(x - N / 2), int(y - N / 2 - alt * math.cos(ELEV))))

def caption(im, text):
    from generar_escala_arthropleura import font as F
    big = im.resize((im.width * 2, im.height * 2), Image.NEAREST)
    out = Image.new('RGBA', (big.width, big.height + 34), (16, 18, 16, 255))
    out.alpha_composite(big, (0, 34))
    ImageDraw.Draw(out).text((12, 7), text, fill=(255, 240, 200), font=F(17))
    return out

def curva(cache, n=24, w=420, h=320):
    """Vuela en círculo alrededor de la rana: pasa por las 16 direcciones (comprueba los volteos)."""
    se = math.sin(ELEV)
    cx, cy, R, alt = w / 2, h / 2 + 10, 120.0, 40
    out = []
    for f in range(n):
        im = burnt_floor(w, h, 5)
        a = f / n * 2 * math.pi
        x, y = cx + R * math.cos(a), cy + R * se * math.sin(a)
        ang = math.degrees(math.atan2(-math.cos(a), -math.sin(a)))     # tangente en el suelo (y hacia arriba)
        im.alpha_composite(frog(), (int(cx - 16), int(cy - 16)))
        place(im, sprite_for('Fly', f % 4, ang, cache), x, y, alt, sprite_for('Fly', f % 4, ang, cache, True))
        out.append(caption(im, f'Vuelo en círculo alrededor de la rana (pasa por las 16 direcciones) · {f + 1}/{n}'))
    out[0].convert('RGB').save(os.path.join(OUT, 'vista_previa_6d_curva.png'))
    out[0].save(os.path.join(OUT, 'gif', 'escenas', 'Meganeura_curva.gif'), save_all=True, append_images=out[1:], duration=70, loop=0)

def picado(cache, w=420, h=300):
    """Escena del picado (lo de la 6f es provisional: la marca es un círculo de prueba)."""
    fx, fy = 210, 190
    frames = []
    def frame(text, frog_xy, mark=None, mark_fixed=False, boss=None):
        im = burnt_floor(w, h, 9)
        d = ImageDraw.Draw(im)
        if mark:
            mx, my = mark
            col = (240, 70, 50, 255) if mark_fixed else (240, 160, 60, 255)
            d.ellipse([mx - 22, my - 22 * .82, mx + 22, my + 22 * .82], outline=col, width=2)
            d.line([mx - 6, my, mx + 6, my], fill=col); d.line([mx, my - 5, mx, my + 5], fill=col)
        later = None
        if boss:
            name, k, ang, x, y, alt, high = boss
            sh = sprite_for(name, k, ang, cache, True)
            if high: place(im, None, x, y, 0, sh, 80, dither=True)
            else: later = (name, k, ang, x, y, alt, sh)
        im.alpha_composite(frog(), (int(frog_xy[0] - 16), int(frog_xy[1] - 20)))
        if later:
            name, k, ang, x, y, alt, sh = later
            place(im, sprite_for(name, k, ang, cache), x, y, alt, sh, 110)
        frames.append(caption(im, text))
    ang_in = -35.0                                              # entra desde arriba a la izquierda
    ca, sa = math.cos(math.radians(ang_in)), math.sin(math.radians(ang_in))
    for i in range(6):
        frame('1 · Vuela alto: solo se ve su sombra (invulnerable)', (fx, fy), boss=('Fly', i % 4, -10, 60 + i * 22, 70 + i * 6, 0, True))
    path = [(fx - 30 + i * 6, fy + 4 - i * 1) for i in range(5)]
    for i, p_ in enumerate(path):
        frame('2 · Marca (provisional, 6f): sigue a la rana...', p_, mark=p_, boss=('Fly', i % 4, 0, 200 + i * 8, 60, 0, True))
    mark = path[-1]
    for i in range(5):
        frame('3 · ...y se queda fija: hay que apartarse (o ponerse delante de un tronco)', (mark[0] - 8 - i * 22, mark[1] + 6 + i * 6), mark=mark, mark_fixed=True)
    escape = (mark[0] - 100, mark[1] + 30)
    for i in range(6):
        t = (i + 1) / 6
        gx, gy = mark[0] - ca * 90 * (1 - t), mark[1] + sa * 90 * .82 * (1 - t)
        frame('4 · Picado: cae desde lo alto sobre la marca (Dive)', escape, mark=mark if i < 5 else None, mark_fixed=True,
              boss=('Dive', i % 3, ang_in, gx, gy, 260 * (1 - t) ** 1.3 + 4, False))
    for i in range(8):
        frame('5 · A ras de suelo tras el picado: vulnerable (Hover)', escape, boss=('Hover', i % 4, ang_in, mark[0] + i * 2, mark[1] - i, 16 + sw(i, 8), False))
    for i in range(7):
        t = (i + 1) / 7
        frame('6 · Remonta y vuelve a ser solo una sombra (Climb)', escape,
              boss=('Climb', i % 4, 20, mark[0] + 16 + 70 * t, mark[1] - 8 - 30 * t, 16 + 280 * t ** 1.6, i == 6))
    frames[0].save(os.path.join(OUT, 'gif', 'escenas', 'Meganeura_escena_picado.gif'), save_all=True, append_images=frames[1:], duration=110, loop=0)
    keys = [3, 8, 13, 17, 19, 22, 26, 32]
    W, H = frames[0].width, frames[0].height
    sheet = Image.new('RGBA', (W * 2, H * 4), (16, 18, 16, 255))
    for j, k in enumerate(keys): sheet.alpha_composite(frames[k], ((j % 2) * W, (j // 2) * H))
    sheet.convert('RGB').save(os.path.join(OUT, 'vista_previa_6d_picado.png'))

# =================================================================================================
# 6e-1: variantes de daño de las alas (mismas hojas, mismas sombras)
# =================================================================================================
VARIANTS = {'Burnt': dict(burnt=True), 'Torn1': dict(torn=1), 'Torn1Burnt': dict(torn=1, burnt=True),
            'Torn2': dict(torn=2), 'Torn2Burnt': dict(torn=2, burnt=True)}

def variantes(names=None):
    names = names or list(VARIANTS)
    for var in names:
        dmg = VARIANTS[var]
        hurt_rows = []
        for name, poses_ in ANIMS.items():
            rows = []
            for dname, yaw in DIRS:
                rows.append([render(yaw, dmg=dmg, **pz) for pz in poses_])
                if name == 'Fly': hurt_rows.append(hurt(rows[-1][0]))
            grid(rows).save(os.path.join(OUT, 'Sheets', f'Meganeura_{name}_{var}.png'))
        grid(hurt_rows).save(os.path.join(OUT, 'Sheets', f'Meganeura_Hurt_{var}.png'))
        print('  ', var, flush=True)
    vista_alas()

def vista_alas():
    """Fly y Perch (Side y Dn45) con cada estado de las alas, sacados de las hojas."""
    rows = {}
    for var in [None] + list(VARIANTS):
        suf = f'_{var}' if var else ''
        cells = []
        for name in ('Fly', 'Perch'):
            im = Image.open(os.path.join(OUT, 'Sheets', f'Meganeura_{name}{suf}.png'))
            for r in (4, 6): cells.append(im.crop((0, r * N, N, (r + 1) * N)))
        rows[var or 'sana'] = cells
    preview(os.path.join(OUT, 'vista_previa_6e1_alas.png'), rows, BG, scale=2)
    fr = []                                                       # en el juego, sobre el suelo quemado y junto a la rana
    for var in [None] + list(VARIANTS):
        im = burnt_floor(300, 200, 4)
        spr = Image.open(os.path.join(OUT, 'Sheets', f'Meganeura_Hover{"_" + var if var else ""}.png')).crop((0, 4 * N, N, 5 * N))
        sh = Image.open(os.path.join(OUT, 'Shadows', 'Meganeura_Shadow_Hover.png')).crop((0, 4 * N, N, 5 * N))
        im.alpha_composite(frog(), (40, 120))
        place(im, spr, 190, 110, 16, sh, 110)
        fr.append(caption(im, f'Hover · alas: {var or "sanas"}'))
    W, H = fr[0].width, fr[0].height
    sheet = Image.new('RGBA', (W * 3, H * 2), (16, 18, 16, 255))
    for j, f in enumerate(fr): sheet.alpha_composite(f, ((j % 3) * W, (j // 3) * H))
    sheet.convert('RGB').save(os.path.join(OUT, 'vista_previa_6e1_juego.png'))

# =================================================================================================
# 6e-2: golpes (clavada, se arranca, electrocutada, cae, boca arriba, se recupera) y capa de llamas
# =================================================================================================
ELEC = ['#123a7a', '#2a6ad0', '#6ab8ff', '#c0ecff', '#ffffff']    # destello: se le ve "por dentro"
ELEC_DK = ['#020610', '#06122a', '#0a1a3a', '#123a7a', '#2a6ad0']  # entre destellos: casi negra

def bolts(im, seed=0, bright=True, n=5):
    """Arcos eléctricos que saltan sobre la silueta (2 px: núcleo blanco y halo azul)."""
    a = np.array(im.getchannel('A')); ys, xs = np.nonzero(a > 200)
    if not len(xs): return
    rnd = random.Random(seed); d = ImageDraw.Draw(im)
    for _ in range(n if bright else n // 2):
        i = rnd.randrange(len(xs)); x, y = xs[i], ys[i]
        pts = [(x, y)]
        for _ in range(rnd.randint(3, 6)):
            x += rnd.randint(-7, 7); y += rnd.randint(-7, 7); pts.append((x, y))
        d.line(pts, fill=(106, 184, 255, 230), width=3)
        d.line(pts, fill=(255, 255, 255, 255) if bright else (192, 236, 255, 255), width=1)

STUCK_WINGS = ((36, -24), (-30, 34), (42, -20), (-24, 38), (32, -28), (-28, 30))
ANIMS2 = {
    # clavada en el tronco 1,5 s: cabeza y patas en la madera, alas desesperadas, abdomen que se retuerce
    'Stuck':    [dict(fore=f, hind=h, pitch=-26, bury=12, curl=c, fold=1.0, bob=.6 * sw(i, 6, 2)) for i, ((f, h), c) in
                 enumerate(zip(STUCK_WINGS, (-.25, .2, -.15, .25, -.2, .15)))],
    # se arranca de un tirón: la cabeza sale de la madera (las astillas salen en el cuadro 2: Fx_Meganeura_Splinters)
    'TearFree': [dict(fore=f, hind=h, pitch=p_, bury=b, fold=fo) for f, h, p_, b, fo in
                 ((40, -10, -24, 12, 1), (-20, 44, -14, 15, 1), (46, -14, -2, 20, .6), (-26, 40, 8, None, .3), (30, 6, 14, None, .2))],
    # alcanzada por el rayo: destellos azules que dejan ver la silueta, alas tiesas, patas abiertas
    'Shocked':  [dict(fore=20 + j, hind=18 - j, pitch=5, fold=-1.0, elec=e, seed=i + 1) for i, (e, j) in
                 enumerate(((1, 3), (2, -3), (1, 4), (2, -2)))],
    # cae dando vueltas (por el rayo o con las alas ardiendo)
    'Fall':     [dict(fore=f, hind=h, roll=r, pitch=-20, fold=.3) for f, h, r in
                 ((-10, 6, 30), (16, -14, 80), (-18, 10, 130), (6, -20, 170))],
    # boca arriba en el suelo: patas que pataleán y alas que tiemblan (vulnerable)
    'Grounded': [dict(fore=2 * d, hind=-2 * d, roll=180, fold=fo, curl=c, bob=-6) for d, fo, c in
                 ((0, -1.0, 0), (1, -.5, .12), (0, -1.0, .04), (-1, -.7, .16))],
    # se endereza y despega
    'Recover':  [dict(fore=f, hind=h, roll=r, pitch=10, fold=fo, bob=b) for f, h, r, fo, b in
                 ((30, -10, 165, -.8, -6), (-20, 36, 130, -.5, -5), (40, -14, 90, 0, -3), (-24, 40, 45, .4, -1),
                  (38, -10, 12, .8, 0), (-16, 34, 0, 1.0, 1))],
}
MS.update({'Stuck': 70, 'TearFree': 80, 'Shocked': 60, 'Fall': 70, 'Grounded': 150, 'Recover': 80})
BURN_ANIMS = ('Fall', 'Grounded')                                # capa Burning (cae con las alas ardiendo)

def layer_burning(base, k, seed=0):
    """Llamas sobre las alas y el cuerpo (sitios fijos por dirección; parpadean con el cuadro). Va ENCIMA del
    cuadro de la animación, en la misma celda."""
    a = np.array(base.getchannel('A'))
    cv = Canvas(N); sites = []
    for gy in range(4, N, 14):
        for gx in range(4, N, 14):
            ys, xs = np.nonzero(a[gy:gy + 14, gx:gx + 14] > 60)
            if len(xs) > 12: j = len(xs) // 2; sites.append((gx + xs[j], gy + ys[j]))
    rnd = random.Random(seed); rnd.shuffle(sites); sites = sorted(sites[:9], key=lambda p: p[1])
    for j, (x, y) in enumerate(sites):
        h = rnd.choice((7, 10, 14, 18)) + ((k + j) % 3) * 2 - 2
        AR.RX.vflame(cv, x, y + 2, min(h, y), k * 1.3 + j * 1.7, w=2.6)
        AR.RX.vflame(cv, x - 3, y + 3, min(h * .5, y), k * 1.3 + j * 2.3 + 1, w=1.4)
    for j in range(5):                                           # pavesas que suben
        x, y = sites[(j * 3 + k) % len(sites)] if sites else (N / 2, N / 2)
        cv.put(x + (j % 3) - 1, y - 12 - ((k * 3 + j * 5) % 9), C(AR.FIR[3] if j % 2 else AR.FIR[4]))
    return cv.img()

def golpes(names=None, var=None):
    """Hojas de la 6e-2 (y sus variantes de alas con var), sombras, capas Burning, vistas previas y GIF."""
    names = names or list(ANIMS2)
    dmg = VARIANTS.get(var) if var else None
    suf = f'_{var}' if var else ''
    side = {}
    for name in names:
        rows, srows, brows = [], [], []
        for dname, yaw in DIRS:
            frames = [render(yaw, dmg=dmg, **pz) for pz in ANIMS2[name]]
            rows.append(frames)
            if not var:
                srows.append([shadow(yaw, **pz) for pz in ANIMS2[name]])
                if name in BURN_ANIMS: brows.append([layer_burning(f, k, seed=int(yaw) + 90) for k, f in enumerate(frames)])
            if dname == 'Side': side[name] = frames + ([] if var or name not in BURN_ANIMS else [])
        grid(rows).save(os.path.join(OUT, 'Sheets', f'Meganeura_{name}{suf}.png'))
        if not var:
            grid(srows).save(os.path.join(OUT, 'Shadows', f'Meganeura_Shadow_{name}.png'))
            if brows: grid(brows).save(os.path.join(OUT, 'Sheets', f'Meganeura_{name}_Burning.png'))
            gif(os.path.join(OUT, 'gif', f'Meganeura_{name}.gif'), side[name], MS[name], BG, scale=2)
        print('  ', name + suf, flush=True)
    if not var: vista_golpes()

def vista_golpes():
    rows = {}
    for name in ANIMS2:
        im = Image.open(os.path.join(OUT, 'Sheets', f'Meganeura_{name}.png'))
        rows[name] = [im.crop((i * N, 4 * N, (i + 1) * N, 5 * N)) for i in range(im.width // N)]
        if name in BURN_ANIMS:
            lay = Image.open(os.path.join(OUT, 'Sheets', f'Meganeura_{name}_Burning.png'))
            fr = []
            for i in range(im.width // N):
                f = rows[name][i].copy(); f.alpha_composite(lay.crop((i * N, 4 * N, (i + 1) * N, 5 * N))); fr.append(f)
            rows[name + ' + Burning'] = fr
            gif(os.path.join(OUT, 'gif', f'Meganeura_{name}_Burning.gif'), fr, MS[name], BG, scale=2)
    preview(os.path.join(OUT, 'vista_previa_6e2_animaciones.png'), rows, BG, scale=2)
    dirs = {}
    for name in ('Stuck', 'Shocked', 'Grounded'):
        im = Image.open(os.path.join(OUT, 'Sheets', f'Meganeura_{name}.png'))
        dirs[f'{name} (9 direcciones)'] = [im.crop((0, r * N, N, (r + 1) * N)) for r in range(9)]
    preview(os.path.join(OUT, 'vista_previa_6e2_direcciones.png'), dirs, BG, scale=1)

def prueba_golpes():
    rows = {n: [render(y, **ANIMS2[n][min(2, len(ANIMS2[n]) - 1)]) for y in (0, -45, 90, -90)] for n in ANIMS2}
    preview(os.path.join(OUT, 'vista_previa_6e2_prueba.png'), rows, BG, scale=2)

# =================================================================================================
# 6e-3: momentos (presentación, chillido, posada furiosa, muerte)
# =================================================================================================
DEAD_PAL = {'mbody': ['#0c1c20', '#1a3036', '#2e4c52', '#4a6a6e', '#7a9494'],      # sin brillo: verde gris apagado
            'eye': ['#0a0808', '#141010', '#221a1a', '#3a2c2c', '#6a5a58'],
            'mstripe': ['#3a2a1a', '#5a4026', '#7a5a36', '#9a7a52', '#b8a078']}
LOST = ('hind', -1)                                              # el ala que se desprende al morir
DMG_F3 = dict(torn=2)                                            # al morir está en la F3

ANIMS3 = {
    # presentación: posada en el Lepidodendron, alas recogidas hacia atrás que se abren con un reflejo azulado
    'Intro':     [dict(fore=f, hind=f - 4, fsw=sw_, hsw=sw_ + 4, fold=-1.0, pitch=p_, glint=g) for f, sw_, p_, g in
                  ((14, 40, 4, None), (10, 30, 6, None), (4, 16, 8, -.1), (-2, 4, 8, .15), (-4, 0, 6, .4),
                   (-4, 0, 4, .65), (-5, 0, 2, .9), (-6, 0, 0, None))],
    # furia de la F2 (rompiste todos los huevos): se alza, abre las mandíbulas y chilla (ondas: Fx_Meganeura_Screech)
    'Screech':   [dict(fore=f, hind=f - 6, pitch=p_, jaw=j, fold=-.6, eye=True, bob=b) for f, p_, j, b in
                  ((10, 8, .2, 0), (26, 18, .7, 1), (34, 24, 1, 2), (28, 24, 1, 2), (36, 24, 1, 2), (20, 14, .5, 1))],
    # posada furiosa junto a la charca (ventana de daño de la F2): ojos encendidos, alas que tiemblan deprisa
    'PerchRage': [dict(fore=-6 + d, hind=-8 - d, fold=-1.0, eye=True, jaw=.3 * abs(d) / 4, bob=-.4 * abs(sw(i, 4)))
                  for i, d in enumerate((0, 4, 0, -4))],
}
MS.update({'Intro': 110, 'Screech': 90, 'PerchRage': 120, 'DeathFall': 90, 'Dead': 400, 'WingDetached': 70})
SCREECH_CALL = 2                                                 # cuadro del chillido (sale la primera onda)

DEATH_YAWS = (0, -50, -105, -160, 150, 95, 40, -15)               # cae en espiral (una fila: gira ella sola)
def death_fall(dmg=DMG_F3):
    out = []
    for i, yw in enumerate(DEATH_YAWS):
        t = i / (len(DEATH_YAWS) - 1)
        out.append(render(yw, dmg=dmg, nowing=LOST, fore=(-6, 22, -14, 18, -20, 10, -8, 4)[i], hind=(10, -10, 16, -16, 8, -6, 4, 0)[i],
                          roll=40 * math.sin(t * 7), pitch=-30 + 10 * t, fold=.5, eye=False))
    return out

def dead(yaw, dmg=DMG_F3):
    """Flota en la charca: alas planas sobre el agua, patas recogidas, color apagado, sin el ala trasera."""
    return render(yaw, dmg=dmg, nowing=LOST, fore=-3, hind=-3, fold=1.0, dead=True, bob=-2)

def wing_detached(n=8, dmg=DMG_F3, cell=128):
    """El ala trasera suelta que cae girando sola (celda 128, centrada)."""
    out = []
    for i in range(n):
        set_pose(dict(pitch=0))
        w = dict(WINGS['hind'], sw=0.0, name='hind')
        lay = wing_layer(w, -1, (0, 14, 28, 14, 0, -14, -28, -14)[i], i * 45.0, seed=3, dmg=dmg)
        im = Image.new('RGBA', (N, N), (0, 0, 0, 0)); p = im.load()
        for (x, y), (d, c) in lay.items(): p[x, y] = c
        bb = im.getbbox()
        o = Image.new('RGBA', (cell, cell), (0, 0, 0, 0))
        if bb:
            cr = im.crop(bb); o.alpha_composite(cr, ((cell - cr.width) // 2, (cell - cr.height) // 2))
        out.append(o)
    return out

def momentos(var=None):
    """Hojas de la 6e-3. var: variante de alas para Intro/Screech/PerchRage (la muerte siempre lleva Torn2;
    var con Burnt → también la versión quemada de la muerte)."""
    dmg = VARIANTS.get(var) if var else None
    suf = f'_{var}' if var else ''
    side = {}
    for name, poses_ in ANIMS3.items():
        if var and name == 'Intro': continue                      # la presentación siempre es con las alas sanas
        rows, srows = [], []
        for dname, yaw in DIRS:
            rows.append([render(yaw, dmg=dmg, **pz) for pz in poses_])
            if not var: srows.append([shadow(yaw, **pz) for pz in poses_])
            if dname == 'Side': side[name] = rows[-1]
        grid(rows).save(os.path.join(OUT, 'Sheets', f'Meganeura_{name}{suf}.png'))
        if not var: grid(srows).save(os.path.join(OUT, 'Shadows', f'Meganeura_Shadow_{name}.png'))
        print('  ', name + suf, flush=True)
    if var and 'Burnt' not in var: return
    dsuf = '_Burnt' if var else ''
    dmg_d = dict(DMG_F3, burnt=True) if var else DMG_F3
    df = death_fall(dmg_d); grid([df]).save(os.path.join(OUT, 'Sheets', f'Meganeura_DeathFall{dsuf}.png'))
    dd = [[dead(yaw, dmg_d)] for _, yaw in DIRS]; grid(dd).save(os.path.join(OUT, 'Sheets', f'Meganeura_Dead{dsuf}.png'))
    wd = wing_detached(dmg=dmg_d)
    sheet = Image.new('RGBA', (128 * len(wd), 128), (0, 0, 0, 0))
    for i, f in enumerate(wd): sheet.paste(f, (i * 128, 0))
    sheet.save(os.path.join(OUT, 'Sheets', f'Meganeura_WingDetached{dsuf}.png'))
    print('   muerte' + dsuf, flush=True)
    if var: return
    side['DeathFall'] = df; side['Dead (9 direcciones)'] = [f[0] for f in dd]
    side['WingDetached'] = [Image.new('RGBA', (N, N)) for _ in wd]
    for i, f in enumerate(wd): side['WingDetached'][i].alpha_composite(f, (32, 32))
    preview(os.path.join(OUT, 'vista_previa_6e3_animaciones.png'), side, BG, scale=2)
    for name in ('Intro', 'Screech', 'PerchRage', 'DeathFall', 'WingDetached'):
        gif(os.path.join(OUT, 'gif', f'Meganeura_{name}.gif'), side[name], MS[name], BG, scale=2)

def prueba_momentos():
    rows = {n: [render(0, **ANIMS3[n][k]) for k in range(0, len(ANIMS3[n]), 2)] for n in ANIMS3}
    rows['Muerte'] = death_fall()[::2] + [dead(0)]
    rows['Ala suelta'] = wing_detached()[::2]
    preview(os.path.join(OUT, 'vista_previa_6e3_prueba.png'), rows, BG, scale=2)

# =================================================================================================
# Escenas de la 6e (leen las hojas ya generadas; lo de la 6f —marca, rayo, huevos— es provisional)
# =================================================================================================
_SH = {}
def sheet(name, folder='Sheets', cell=N):
    key = (folder, name)
    if key not in _SH: _SH[key] = Image.open(os.path.join(OUT, folder, f'{name}.png')).convert('RGBA')
    return _SH[key]

def SF(name, k, ang, var=None, folder='Sheets'):
    """Cuadro k de la hoja name (con variante) mirando a ang: elige la fila y voltea como el código."""
    q = round(ang / 22.5) * 22.5; q = (q + 180) % 360 - 180
    flip = abs(q) > 90
    base = (180 - q if q > 0 else -180 - q) if flip else q
    row = [y for _, y in DIRS].index(base)
    im = sheet(f'{name}_{var}' if var else name, folder)
    k = k % (im.width // N)
    f = im.crop((k * N, row * N, (k + 1) * N, (row + 1) * N))
    return f.transpose(Image.FLIP_LEFT_RIGHT) if flip else f

def SH(name, k, ang):
    base = name.split('_')[0]
    return SF(base if base not in ('DeathFall', 'Dead') else 'Fly', k, ang, folder='Shadows') if os.path.exists(
        os.path.join(OUT, 'Shadows', f'Meganeura_Shadow_{base}.png')) else None

def boss(im, name, k, ang, x, y, alt, var=None, burning=False, sh=True):
    spr = SF(f'Meganeura_{name}', k, ang, var)
    if burning:
        spr = spr.copy(); spr.alpha_composite(SF(f'Meganeura_{name}_Burning', k, ang))
    shd = None
    if sh:
        try: shd = SF(f'Meganeura_Shadow_{name}', k, ang, folder='Shadows')
        except Exception: shd = None
    place(im, spr, x, y, alt, shd, 110)

def prop(name, k=0, w=None):
    im = Image.open(os.path.join(OUT, 'Burnt', f'{name}.png')).convert('RGBA')
    w = w or 64
    return im.crop((k * w, 0, (k + 1) * w, im.height))

FX_DIR = os.path.join(OUT, 'Effects')
def fx(name, k, w):
    im = Image.open(os.path.join(FX_DIR, f'{name}.png')).convert('RGBA')
    k = min(k, im.width // w - 1)
    return im.crop((k * w, 0, (k + 1) * w, im.height))

def env_frame(name, k, w):
    im = Image.open(os.path.join(MT, 'floor3-environment', 'Burnt', 'props', f'{name}.png')).convert('RGBA')
    return im.crop((k * w, 0, (k + 1) * w, im.height))

def pool_img():
    p_ = env_frame('Puddle_Water_Island', 0, 50)
    return p_.resize((p_.width * 3, p_.height * 3), Image.NEAREST)

def bolt_line(im, x, y_top, y_bot, seed):
    """Rayo PROVISIONAL (el de verdad es de la 6f-2)."""
    rnd = random.Random(seed); d = ImageDraw.Draw(im)
    pts = [(x + rnd.randint(-10, 10), y_top)]
    yy = y_top
    while yy < y_bot:
        yy += rnd.randint(8, 16); pts.append((x + rnd.randint(-9, 9), min(yy, y_bot)))
    d.line(pts, fill=(106, 184, 255, 255), width=4); d.line(pts, fill=(255, 255, 255, 255), width=2)

def save_scene(frames, name, keys, ms=110):
    os.makedirs(os.path.join(OUT, 'gif', 'escenas'), exist_ok=True)
    frames[0].save(os.path.join(OUT, 'gif', 'escenas', f'Meganeura_escena_{name}.gif'), save_all=True, append_images=frames[1:], duration=ms, loop=0)
    W, H = frames[0].width, frames[0].height
    rows = (len(keys) + 1) // 2
    sh_ = Image.new('RGBA', (W * 2, H * rows), (16, 18, 16, 255))
    for j, k in enumerate(keys): sh_.alpha_composite(frames[k], ((j % 2) * W, (j // 2) * H))
    sh_.convert('RGB').save(os.path.join(OUT, f'vista_previa_6e_escena_{name}.png'))
    print('  ', name, flush=True)

def escena_clavada(w=420, h=300):
    """Picado contra el Lepidodendron: la rana se pone delante, la Meganeura se clava, se arranca y deja la herida."""
    fr = []
    tx, ty = 250, 230                                        # pie del tronco
    def frame(text, tree, frog_xy, b=None, fxs=()):
        im = burnt_floor(w, h, 21)
        t_ = tree.copy(); im.alpha_composite(t_, (tx - 32, ty - 90))
        if frog_xy: im.alpha_composite(frog(), (int(frog_xy[0] - 16), int(frog_xy[1] - 20)))
        if b: boss(im, *b)
        for (img, x, y) in fxs: im.alpha_composite(img, (int(x - img.width / 2), int(y - img.height / 2)))
        fr.append(caption(im, text))
    gx_, gy_ = tx, ty - 40                                   # herida (y = 50 del tronco)
    alt_stuck = 40 / math.cos(ELEV)
    for i in range(4):
        frame('1 · La marca se fija y la rana se pone delante del Lepidodendron', prop('Lepido_Split_Idle', i), (tx - 30 - i * 2, ty + 8))
    for i in range(5):
        t = (i + 1) / 5
        frame('2 · Picado: la rana se aparta y la Meganeura va directa al tronco', prop('Lepido_Split_Idle', i % 4), (tx - 40 - i * 12, ty + 14 + i * 10),
              ('Dive', i % 3, 0, tx - 13 - 120 * (1 - t), ty, alt_stuck + 160 * (1 - t) ** 1.2))
    for i in range(4):
        frame('3 · ¡Se clava! (Lepido_Split_Hit)', prop('Lepido_Split_Hit', i), (tx - 100, ty + 62), ('Stuck', i, 0, tx - 13, ty, alt_stuck))
    for i in range(10):
        frame('4 · Clavada 1,5 s: cabeza y patas en la madera (Stuck) · vulnerable', prop('Lepido_Split_Gash', i % 4), (tx - 100 + i * 4, ty + 62 - i * 2),
              ('Stuck', i % 6, 0, tx - 13, ty, alt_stuck))
    for i in range(5):
        fxs = [(fx('Fx_Meganeura_Splinters', i - 2, 64), gx_ - 4, gy_)] if i >= 2 else []
        frame('5 · Se arranca de un tirón: astillas (TearFree)', prop('Lepido_Split_Gash', i % 4), (tx - 60, ty + 42),
              ('TearFree', i, 0, tx - 13 - i * 4, ty, alt_stuck + i * 3), fxs)
    for i in range(5):
        frame('6 · Remonta; el tronco se queda herido (Lepido_Split_Gash)', prop('Lepido_Split_Gash', i % 4), (tx - 60, ty + 42),
              ('Climb', i % 4, 20, tx + 20 + i * 18, ty - i * 6, alt_stuck + 30 * i))
    save_scene(fr, 'clavada', [2, 7, 9, 14, 22, 29])

def escena_rayo(w=420, h=300):
    """F3: a ras de suelo tras el picado, el rayo que sigue a la rana le cae encima: electrocutada, cae, boca arriba
    (vulnerable) y se recupera. Lluvia y señal del rayo: 6f-2."""
    fr = []
    bx, by = 230, 200
    def frame(text, frog_xy, b=None, fxs=(), bolt=None, under=()):
        im = burnt_floor(w, h, 33)
        for (img, x, y) in under: im.alpha_composite(img, (int(x - img.width / 2), int(y - img.height / 2)))
        im.alpha_composite(frog(), (int(frog_xy[0] - 16), int(frog_xy[1] - 20)))
        if b: boss(im, *b)
        if bolt: bolt_line(im, bolt[0], 0, bolt[1], bolt[2])
        for (img, x, y) in fxs: im.alpha_composite(img, (int(x - img.width / 2), int(y - img.height / 2)))
        fr.append(caption(im, text))
    for i in range(6):
        frame('1 · F3: tras el picado se queda a ras de suelo; la señal del rayo sigue a la rana (6f-2)', (bx - 150 + i * 8, by + 50),
              ('Hover', i % 4, 180, bx, by, 16, 'Torn2'))
    for i in range(2):
        frame('2 · La rana pasa junto a ella... ¡y el rayo cae! (provisional)', (bx - 100, by + 40), ('Hover', i, 180, bx, by, 16, 'Torn2'), bolt=(bx, by - 30, i))
    for i in range(6):
        frame('3 · Electrocutada (Shocked)', (bx - 100, by + 40), ('Shocked', i, 180, bx, by, 16, 'Torn2'), bolt=(bx, by - 30, 5) if i < 2 else None)
    for i in range(4):
        frame('4 · Cae dando vueltas (Fall)', (bx - 100, by + 40), ('Fall', i, 180, bx, by, 16 * (1 - i / 4), 'Torn2'))
    for i in range(6):
        frame('5 · Golpe contra el suelo (Fx_Meganeura_Crash)', (bx - 100, by + 40), ('Grounded', i % 4, 180, bx, by, 0, 'Torn2'),
              under=[(fx('Fx_Meganeura_Crash', i, 96), bx, by + 6)])
    for i in range(10):
        frame('6 · Boca arriba (Grounded): ¡a por ella! · vulnerable', (bx - 120 + i * 3, by + 50 - i), ('Grounded', i % 4, 180, bx, by, 0, 'Torn2'))
    for i in range(6):
        frame('7 · Se endereza y despega (Recover)', (bx - 130, by + 50), ('Recover', i, 180, bx, by, i * 3, 'Torn2'))
    save_scene(fr, 'rayo', [3, 7, 10, 15, 20, 30, 37])

def escena_presentacion(w=420, h=300):
    fr = []
    tx, ty = 240, 240
    top_alt = 78 / math.cos(ELEV)                            # posada en la rotura del tronco (unos 78 px sobre el pie)
    def frame(text, b=None, sh=True):
        im = burnt_floor(w, h, 44)
        im.alpha_composite(prop('Lepido_Split_Idle', len(fr) % 4), (tx - 32, ty - 90))
        im.alpha_composite(frog(), (110, 240))
        if b: boss(im, *b)
        fr.append(caption(im, text))
    for i in range(6):
        frame('1 · Silencio... una sombra enorme cruza la sala (vuela alto)')
        place(fr[-1], None, 0, 0)                            # (la sombra se pinta abajo)
    fr.clear()
    for i in range(6):
        im = burnt_floor(w, h, 44)
        im.alpha_composite(prop('Lepido_Split_Idle', i % 4), (tx - 32, ty - 90))
        im.alpha_composite(frog(), (110, 240))
        place(im, None, 20 + i * 70, 120 + i * 10, 0, SF('Meganeura_Shadow_Fly', i, -10, folder='Shadows'), 80, dither=True)
        fr.append(caption(im, '1 · Silencio... una sombra enorme cruza la sala (vuela alto)'))
    for i in range(5):
        t = (i + 1) / 5
        frame('2 · Baja (Fly) hacia el Lepidodendron partido', ('Fly', i % 4, 200, tx + 160 * (1 - t), ty - 40 * (1 - t), top_alt + 120 * (1 - t)))
    for i in range(3):
        frame('3 · Se posa (Land)', ('Land', i, 180, tx, ty, top_alt))
    for i in range(8):
        frame('4 · Abre las alas: reflejo azulado (Intro) · nombre en pantalla', ('Intro', i, 180, tx, ty, top_alt))
    for i in range(4):
        frame('MEGANEURA · la reina del cielo carbonífero (texto provisional)', ('Perch', i, 180, tx, ty, top_alt))
    save_scene(fr, 'presentacion', [2, 8, 12, 16, 19, 23])

def escena_furia(w=420, h=300):
    fr = []
    cx, cy = 230, 160
    pool = pool_img()
    def frame(text, b, fxs=()):
        im = burnt_floor(w, h, 55)
        im.alpha_composite(pool, (cx - pool.width // 2 + 40, cy + 30))
        im.alpha_composite(frog(), (70, 230))
        boss(im, *b)
        for (img, x, y) in fxs: im.alpha_composite(img, (int(x - img.width / 2), int(y - img.height / 2)))
        fr.append(caption(im, text))
    for i in range(3):
        frame('1 · Rompiste el último huevo: baja furiosa (Land)', ('Land', i, 180, cx, cy + 40, 30 - i * 8, 'Torn1'))
    for i in range(10):
        fxs = [(fx('Fx_Meganeura_Screech', i - 2, 160), cx - 28, cy + 40 - 18 - 8)] if i >= 2 else []
        frame('2 · ¡Chillido! (Screech): ondas desde la cabeza', ('Screech', min(i, 5) if i < 6 else 2 + i % 3, 180, cx, cy + 40, 8, 'Torn1'), fxs)
    for i in range(8):
        frame('3 · Posada furiosa junto a la charca (PerchRage): ventana de daño', ('PerchRage', i % 4, 180, cx, cy + 40, 8, 'Torn1'))
    save_scene(fr, 'furia', [1, 5, 8, 16])

def escena_muerte(w=420, h=300):
    fr = []
    px, py = 210, 190
    pool = pool_img()
    def frame(text, b=None, fxs=(), under=(), dead_=None):
        im = burnt_floor(w, h, 66)
        im.alpha_composite(pool, (px - pool.width // 2, py - pool.height // 2))
        for (img, x, y) in under: im.alpha_composite(img, (int(x - img.width / 2), int(y - img.height / 2)))
        im.alpha_composite(frog(), (60, 250))
        if dead_ is not None: im.alpha_composite(dead_, (px - N // 2, py - N // 2 - 2))
        if b:
            name, k, x, y, alt = b
            spr = sheet('Meganeura_DeathFall').crop((k * N, 0, (k + 1) * N, N))
            place(im, spr, x, y, alt, None)
        for (img, x, y) in fxs: im.alpha_composite(img, (int(x - img.width / 2), int(y - img.height / 2)))
        fr.append(caption(im, text))
    wing = sheet('Meganeura_WingDetached')
    for i in range(8):
        t = i / 7
        wf = wing.crop((i * 128, 0, (i + 1) * 128, 128))
        frame('1 · Último golpe: se le desprende un ala y cae en espiral (DeathFall + WingDetached)',
              ('DeathFall', i, px + 40 * math.cos(t * 5), py - 10 * math.sin(t * 5), 200 * (1 - t) ** 1.3 + 4),
              fxs=[(wf, px - 120 + 20 * t, 40 + 150 * t)])
    for i in range(7):
        frame('2 · Se estrella en la charca (Fx_Meganeura_Splash)', fxs=[(fx('Fx_Meganeura_Splash', i, 128), px, py - 22)],
              dead_=sheet('Meganeura_Dead').crop((0, 4 * N, N, 5 * N)) if i >= 2 else None)
    for i in range(8):
        frame('3 · La tormenta amaina; flota con las alas abiertas (Dead + DeadRipple)',
              under=[(fx('Fx_Meganeura_DeadRipple', i % 4, 160), px, py)], dead_=sheet('Meganeura_Dead').crop((0, 4 * N, N, 5 * N)))
    save_scene(fr, 'muerte', [2, 6, 10, 18])

# =================================================================================================
# Escenas de la 6f-1 (marca, aleteo, huevos y ninfas)
# =================================================================================================
def fxw(name, k, w, folder='Effects'):
    im = Image.open(os.path.join(OUT, folder, f'{name}.png')).convert('RGBA')
    k = k % (im.width // w)
    return im.crop((k * w, 0, (k + 1) * w, im.height))

def nymph(name, k, ang, cell=64):
    q = round(ang / 22.5) * 22.5; q = (q + 180) % 360 - 180
    flip = abs(q) > 90; base = (180 - q if q > 0 else -180 - q) if flip else q
    row = [y for _, y in DIRS].index(base)
    def get(folder, nm):
        im = sheet(nm, folder); c = im.height // 9; kk = k % (im.width // c)
        f = im.crop((kk * c, row * c, (kk + 1) * c, (row + 1) * c))
        return f.transpose(Image.FLIP_LEFT_RIGHT) if flip else f
    return get('Sheets', f'Meganeura_Nymph_{name}'), get('Shadows', f'Meganeura_Nymph_Shadow_{name}')

def put_c(im, img, x, y):
    im.alpha_composite(img, (int(x - img.width / 2), int(y - img.height / 2)))

def place_n(im, name, k, ang, x, y):
    spr, sh = nymph(name, k, ang)
    s_ = sh.copy(); s_.putalpha(s_.getchannel('A').point(lambda v: 90 if v else 0))
    put_c(im, s_, x, y); put_c(im, spr, x, y)

def escena_marca(w=420, h=300):
    """El picado completo con la marca de verdad: sigue (ámbar) → se fija (rojo) → picado → impacto y grieta."""
    fr = []
    fx_, fy_ = 210, 190
    def frame(text, frog_xy, mark=None, locked=False, b=None, under=(), high=None):
        im = burnt_floor(w, h, 9)
        for (img, x, y) in under: put_c(im, img, x, y)
        if mark: put_c(im, fxw('Fx_Meganeura_DiveMark_Locked' if locked else 'Fx_Meganeura_DiveMark_Follow', len(fr), 64), *mark)
        if high: place(im, None, high[0], high[1], 0, SF('Meganeura_Shadow_Fly', len(fr), high[2], folder='Shadows'), 80, dither=True)
        im.alpha_composite(frog(), (int(frog_xy[0] - 16), int(frog_xy[1] - 20)))
        if b: boss(im, *b)
        fr.append(caption(im, text))
    path = [(fx_ - 40 + i * 7, fy_ + 6 - i * 2) for i in range(8)]
    for i, p_ in enumerate(path):
        frame('1 · La marca (ámbar) sigue a la rana mientras su sombra ronda', p_, mark=p_, high=(140 + i * 10, 80, 0))
    mark = path[-1]
    for i in range(6):
        frame('2 · Se fija (roja, late más rápido): ¡apártate!', (mark[0] - 10 - i * 16, mark[1] + 8 + i * 5), mark=mark, locked=True)
    esc = (mark[0] - 106, mark[1] + 38)
    ca, sa = math.cos(math.radians(-35)), math.sin(math.radians(-35))
    for i in range(5):
        t = (i + 1) / 5
        frame('3 · Picado sobre la marca', esc, mark=mark if i < 4 else None, locked=True,
              b=('Dive', i % 3, -35, mark[0] - ca * 90 * (1 - t), mark[1] + sa * 74 * (1 - t), 240 * (1 - t) ** 1.3 + 6))
    for i in range(7):
        frame('4 · Impacto: onda de ceniza, ascuas y grieta (Fx_Meganeura_DiveImpact + DiveCrack)', esc,
              b=('Hover', i % 4, -35, mark[0], mark[1], 14), under=[(fxw('Fx_Meganeura_DiveCrack', min(i // 2, 4), 64), *mark),
                                                                       (fxw('Fx_Meganeura_DiveImpact', i, 112), mark[0], mark[1] - 4)])
    save_scene(fr, 'marca', [3, 10, 16, 19, 23])

def escena_aleteo(w=420, h=300):
    """Aleteo fuerte: la ráfaga empuja la niebla de metano hacia la rana (con rayas de arrastre)."""
    fr = []
    bx, by = 120, 160
    mist = env_frame('Methane_Mist', 0, 32)
    blobs = [(200 + (j % 3) * 22, 130 + (j // 3) * 22) for j in range(9)]
    for i in range(14):
        im = burnt_floor(w, h, 14)
        push = max(0, i - 4) * 9
        for (mx, my) in blobs:
            put_c(im, mist, mx + push, my + 10)
            if 4 <= i <= 11: put_c(im, fxw('Fx_Meganeura_WindDrag', i, 16), mx + push + 6, my + 6)
        im.alpha_composite(frog(), (360, 200))
        if 3 <= i <= 11: put_c(im, fxw('Fx_Meganeura_Gust', i, 112), bx + 90, by - 10)
        boss(im, 'Gust', min(i, 5) if i < 6 else 3 + (i % 3), 0, bx, by, 18)
        fr.append(caption(im, 'Aleteo fuerte (Gust): la ráfaga empuja la niebla de metano (WindDrag)' if i >= 3 else 'Se alza para el aleteo fuerte'))
    save_scene(fr, 'aleteo', [2, 6, 9, 12])

def escena_huevos(w=420, h=300):
    """F2: pone los huevos en la orilla, eclosionan, la ninfa persigue a la rana y le lanza el labio; otro racimo se
    rompe a golpes."""
    fr = []
    pool = pool_img()
    px, py = 210, 150
    eggs_xy = [(px - 70, py + 30), (px + 66, py + 28)]
    def frame(text, eg, b=None, nyms=(), frog_xy=(330, 250), extra=()):
        im = burnt_floor(w, h, 77)
        im.alpha_composite(pool, (px - pool.width // 2, py - pool.height // 2))
        for (st, k, x, y) in eg: put_c(im, fxw(f'Meganeura_Eggs_{st}', k, 40, 'Eggs'), x, y)
        for (nm, k, a, x, y) in nyms: place_n(im, nm, k, a, x, y)
        for (img, x, y) in extra: put_c(im, img, x, y)
        im.alpha_composite(frog(), (int(frog_xy[0] - 16), int(frog_xy[1] - 20)))
        if b: boss(im, *b)
        fr.append(caption(im, text))
    for i in range(6):
        eg = [('Appear', min(3, max(0, i - 2)), *eggs_xy[0])] if i >= 2 else []
        frame('1 · Se posa en la orilla y pone un racimo (Lay)', eg, b=('Lay', i, 180, eggs_xy[0][0] + 18, eggs_xy[0][1] - 4, 6, 'Torn1'))
    for i in range(4):
        frame('2 · Remonta: los racimos laten en la orilla', [('Idle', i, *eggs_xy[0]), ('Idle', i + 2, *eggs_xy[1])],
              b=('Climb', i, 160, eggs_xy[0][0] + 10 - i * 20, eggs_xy[0][1] - 20 - i * 10, 20 + i * 40))
    for i in range(6):
        nyms = [('Idle', 0, 0, eggs_xy[1][0] + 6, eggs_xy[1][1] - 2)] if i >= 3 else []
        frame('3 · Uno eclosiona: sale una ninfa (Eggs_Hatch)', [('Idle', i, *eggs_xy[0]), ('Hatch', i, *eggs_xy[1])], nyms=nyms)
    for i in range(8):
        x = eggs_xy[1][0] + 6 + i * 5; y = eggs_xy[1][1] + i * 6
        frame('4 · La ninfa persigue a la rana (Move)', [('Idle', i, *eggs_xy[0]), ('Break', 5, *eggs_xy[1])],
              nyms=[('Move', i, -50, x, y)])
    nx, ny = eggs_xy[1][0] + 46, eggs_xy[1][1] + 48
    for i in range(3):
        frame('5 · Se agacha y el labio brilla (Warn, 0,4 s)', [('Idle', i, *eggs_xy[0])], nyms=[('Warn', i, -30, nx, ny)])
    for i in range(4):
        spr, sh = nymph('Strike', i, -30)
        im_x = nx
        frame('6 · ¡Labio! Si te pega, te tira hacia ella (Strike)', [('Idle', i, *eggs_xy[0])], extra=[(spr, nx, ny)],
              frog_xy=(330 - (8 * i if i in (1, 2) else 0), 250 - (5 * i if i in (1, 2) else 0)))
    for i in range(6):
        frame('7 · Mientras, otro racimo se rompe a golpes (Eggs_Break)', [('Break', i, *eggs_xy[0])], nyms=[('Idle', i, -30, nx, ny)],
              frog_xy=(eggs_xy[0][0] + 26, eggs_xy[0][1] + 24))
    save_scene(fr, 'huevos', [3, 8, 13, 18, 25, 28, 33, 35])

# =================================================================================================
# Escenas de la 6f-2 (tormenta). La oscuridad y el destello los hace el código con capas de color.
# =================================================================================================
DARK_C, FLASH_C = (12, 20, 40), (232, 244, 255)

def overlay(im, col, a):
    if a > 0: im.alpha_composite(Image.new('RGBA', im.size, col + (int(255 * a),)))

def rain_on(im, k, light=False, a=1.0):
    t = fxw('Fx_Storm_Drizzle' if light else 'Fx_Storm_Rain', k, 128)
    if a < 1: t.putalpha(t.getchannel('A').point(lambda v: int(v * a)))
    for y in range(0, im.height, 128):
        for x in range(0, im.width, 128): im.alpha_composite(t, (x, y))

def tile_on(im, name, k, a=1.0, T=128):
    t = fxw(name, k, T)
    if a < 1: t.putalpha(t.getchannel('A').point(lambda v: int(v * a)))
    for y in range(0, im.height, T):
        for x in range(0, im.width, T): im.alpha_composite(t, (x, y))

def bolt_at(im, name, k, x, y, w, h):
    b = fxw(name, k, w); im.alpha_composite(b, (int(x - w / 2), int(y - h + 8)))

def burnt_prop(name, k=0, w=None):
    im = Image.open(os.path.join(OUT, 'Burnt', f'{name}.png')).convert('RGBA'); w = w or im.height
    return im.crop((k * w, 0, (k + 1) * w, im.height))

def escena_entrada(w=420, h=300):
    """Al 30 %: chilla y remonta, se oscurece con viento y ceniza, y el primer trueno cae junto a la rana e ilumina
    toda la pantalla; empieza la lluvia."""
    fr = []; fx_, fy_ = 170, 200
    for i in range(18):
        im = burnt_floor(w, h, 91); gl = Image.new('RGBA', (w, h))
        dark = min(.5, i * .05); flash = 0
        under, over = [], []
        if i < 6:
            boss(im, 'Screech', min(i, 5), 180, 280, 170, 24 + i * 4, 'Torn1')
            if i >= 2: put_c(im, fxw('Fx_Meganeura_Screech', i - 2, 160), 252, 170 - 40)
        elif i < 9:
            boss(im, 'Climb', i, 160, 280 - (i - 5) * 30, 170 - (i - 5) * 20, 50 + (i - 5) * 80, 'Torn2')
        if 9 <= i <= 10:
            bolt_at(gl, 'Fx_Bolt_Huge', i - 9, fx_ + 60, fy_ + 10, 192, 420); flash = .85 if i == 9 else .55
        if 10 <= i <= 15: put_c(gl, fxw('Fx_Bolt_Big_Impact', i - 10, 128), fx_ + 60, fy_ - 18)
        if i >= 11: put_c(gl, fxw('Fx_Bolt_Ring', (i - 11) * 2, 96), fx_ + 60, fy_ + 2)
        if i >= 12: put_c(im, fxw('Fx_Bolt_Scorch', 0, 64), fx_ + 60, fy_ + 2)
        im.alpha_composite(frog(), (fx_ - 16, fy_ - 20))
        if 3 <= i <= 12: tile_on(im, 'Fx_Storm_WindAsh', i, .9)
        overlay(im, DARK_C, dark)
        if i >= 11: rain_on(im, i, a=min(1, (i - 10) / 3))
        overlay(im, FLASH_C, flash)
        im.alpha_composite(gl)
        txt = ('1 · Al 30 %: chilla y remonta' if i < 6 else '2 · El cielo se oscurece; viento con ceniza' if i < 9
               else '3 · ¡PRIMER TRUENO junto a la rana! Destello en toda la pantalla' if i < 11 else '4 · Corriente circular y empieza a llover')
        fr.append(caption(im, txt))
    save_scene(fr, 'tormenta_entrada', [2, 7, 9, 10, 12, 16])

def escena_tormenta_rayos(w=440, h=320):
    """La señal grande sigue a la rana; mientras, rayos sueltos marcados en el terreno caen en una charca (mata a las
    ninfas), en una Sigillaria (se parte y arde; la lluvia la apaga) y en el suelo (corriente circular). La rana lleva
    la señal grande hasta la Meganeura a ras de suelo, se aparta y el rayo le cae encima."""
    fr = []
    pool = pool_img(); ppx, ppy = 110, 110
    el = Image.open(os.path.join(OUT, 'Burnt', 'Puddle_Water_Island_Electric.png')).convert('RGBA')
    sx, sy = 360, 120                                           # Sigillaria (pie)
    gx, gy = 330, 270                                           # rayo suelto en el suelo
    bx, by = 250, 200                                           # la Meganeura a ras de suelo
    nyms = [(ppx - 20, ppy - 6), (ppx + 26, ppy + 4)]
    for i in range(30):
        im = burnt_floor(w, h, 92); gl = Image.new('RGBA', (w, h))
        im.alpha_composite(pool, (ppx - pool.width // 2, ppy - pool.height // 2))
        if 9 <= i <= 22:
            e = el.crop((((i - 9) % 6) * 50, 0, ((i - 9) % 6 + 1) * 50, el.height)); e = e.resize((e.width * 3, e.height * 3), Image.NEAREST)
            gl.alpha_composite(e, (ppx - e.width // 2, ppy - e.height // 2))
        for j, (nx, ny) in enumerate(nyms):                     # ninfas en la charca
            if i < 9: place_n(im, 'Move', i + j, 30 + j * 120, nx, ny)
            elif i < 14:
                spr = sheet('Meganeura_Nymph_DeathShock').crop(((i - 9) * 64, 4 * 64, (i - 8) * 64, 5 * 64)); put_c(gl if i < 13 else im, spr, nx, ny)
            else:
                spr = sheet('Meganeura_Nymph_DeathShock').crop((4 * 64, 4 * 64, 5 * 64, 5 * 64)); put_c(im, spr, nx, ny)
        # Sigillaria: intacta → alcanzada → arde → humea con la lluvia
        if i < 10: sp = Image.open(os.path.join(MT, 'floor3-environment', 'Burnt', 'props', 'Obstacle_Sigillaria.png')).convert('RGBA'); im.alpha_composite(sp, (sx - 24, sy - 64))
        elif i < 18: gl.alpha_composite(burnt_prop('Sigillaria_Struck', i - 10, 72), (sx - 36, sy - 70))
        elif i < 24: gl.alpha_composite(burnt_prop('Sigillaria_Burning', i, 72), (sx - 36, sy - 70))
        else: im.alpha_composite(burnt_prop('Sigillaria_Smolder', i, 72), (sx - 36, sy - 70))
        # señales de los rayos sueltos (cargan) y sus rayos
        for (mx, my, t0) in ((ppx, ppy, 8), (sx, sy - 4, 9), (gx, gy, 12)):
            if i < t0: put_c(gl, fxw('Fx_Bolt_Small_Mark', min(7, i * 8 // t0), 48), mx, my)
            elif i < t0 + 2: bolt_at(gl, 'Fx_Bolt_Small', i - t0, mx, my + 6, 48, 360)
            if t0 <= i < t0 + 5: put_c(gl, fxw('Fx_Bolt_Small_Impact', i - t0, 64), mx, my - 8)
        if i >= 13: put_c(gl, fxw('Fx_Bolt_Ring', min(15, (i - 13)), 96), gx, gy)
        # la rana lleva la señal grande hasta la Meganeura y se aparta
        frx, fry = (60 + i * 7, 250 - i * 2) if i < 20 else (200 - (i - 20) * 12, 210 + (i - 20) * 6)
        mark_xy = (frx, fry - 4) if i < 20 else (bx - 10, by + 6)
        if i < 26: put_c(gl, fxw('Fx_Bolt_Big_Mark', min(9, i * 8 // 20) if i < 20 else 8 + i % 2, 96), *mark_xy)
        im.alpha_composite(frog(), (int(frx - 16), int(fry - 20)))
        if i < 26: boss(im, 'Hover', i % 4, 180, bx, by, 16, 'Torn2')
        else: boss(gl, 'Shocked', i, 180, bx, by, 16, 'Torn2', sh=False)
        if 26 <= i <= 27: bolt_at(gl, 'Fx_Bolt_Big', i - 26, bx, by + 6, 96, 360)
        if i >= 26: put_c(gl, fxw('Fx_Bolt_Big_Impact', i - 26, 128), bx, by - 12)
        overlay(im, DARK_C, .45)
        rain_on(im, i)
        if i in (8, 9, 12, 26): overlay(im, FLASH_C, .3 if i != 26 else .6)
        im.alpha_composite(gl)
        txt = ('1 · Señal grande (sigue a la rana) y señales sueltas en el terreno' if i < 8 else
               '2 · Rayos sueltos: charca electrificada (ninfas fritas) y Sigillaria partida' if i < 14 else
               '3 · Corriente circular; la Sigillaria arde (y la lluvia la va apagando)' if i < 20 else
               '4 · La señal grande se fija junto a la Meganeura: ¡la rana se aparta!' if i < 26 else
               '5 · ¡Rayo grande sobre la Meganeura! (Shocked)')
        fr.append(caption(im, txt))
    save_scene(fr, 'tormenta_rayos', [4, 9, 12, 17, 22, 26, 28])

def escena_silueta(w=420, h=300):
    """Vuela alto (solo sombra); cada relámpago enseña su silueta a contraluz en alto un instante."""
    fr = []
    sil = sheet('Meganeura_Silhouette_Fly')
    for i in range(12):
        im = burnt_floor(w, h, 93)
        x, y = 120 + i * 16, 190 - i * 3
        flash = i in (4, 5, 9)
        place(im, None, x, y, 0, SF('Meganeura_Shadow_Fly', i, -10, folder='Shadows'), 150 if flash else 80, dither=not flash)
        im.alpha_composite(frog(), (300, 230))
        overlay(im, DARK_C, .45)
        rain_on(im, i)
        if flash:
            overlay(im, FLASH_C, .3 if i != 5 else .15)
            row = [y_ for _, y_ in DIRS].index(-22.5)
            spr = sil.crop(((i % 4) * N, row * N, (i % 4 + 1) * N, (row + 1) * N))
            im.alpha_composite(spr, (int(x - N / 2), int(y - N / 2 - 150)))
        fr.append(caption(im, 'Relámpago: su silueta a contraluz en alto (2 cuadros) y la sombra nítida' if flash else 'F3: vuela alto entre las nubes (solo su sombra)'))
    save_scene(fr, 'tormenta_silueta', [2, 4, 5, 9])

def escena_final(w=420, h=300):
    """Muere: la lluvia se calma hasta llovizna, vuelve la luz y el suelo humea."""
    fr = []; px, py = 210, 160
    pool = pool_img()
    for i in range(16):
        im = burnt_floor(w, h, 94)
        im.alpha_composite(pool, (px - pool.width // 2, py - pool.height // 2))
        put_c(im, fxw('Fx_Meganeura_DeadRipple', i, 160), px, py)
        im.alpha_composite(sheet('Meganeura_Dead').crop((0, 4 * N, N, 5 * N)), (px - N // 2, py - N // 2 - 2))
        t = i / 15
        for j, (sx_, sy_) in enumerate(((80, 240), (330, 230), (60, 90), (360, 80), (250, 260))):
            if t > .3 + j * .08: put_c(im, fxw('Fx_Storm_Steam', i + j, 48), sx_, sy_)
        im.alpha_composite(frog(), (300, 240))
        overlay(im, DARK_C, .45 * (1 - t))
        if t < .5: rain_on(im, i, a=1 - t * 1.6)
        else: rain_on(im, i, light=True, a=1 - (t - .5))
        fr.append(caption(im, 'La tormenta amaina: llovizna, vuelve la luz y el suelo humea'))
    save_scene(fr, 'tormenta_final', [0, 6, 11, 15])

def escenas_6e(which=None):
    which = which or ['clavada', 'rayo', 'presentacion', 'furia', 'muerte']
    for w_ in which: globals()[f'escena_{w_}']()

# =================================================================================================
# 6f-2: silueta a contraluz (un relámpago la descubre cuando vuela alto)
# =================================================================================================
def silueta():
    """Meganeura_Silhouette_Fly: los mismos cuadros y filas que Meganeura_Fly, negra con el borde azul del
    relámpago; las alas, azul muy oscuro y translúcidas. El código la pinta en alto 2 cuadros con cada relámpago."""
    src = sheet('Meganeura_Fly'); W, H = src.size
    a = np.array(src.getchannel('A'))
    out = np.zeros((H, W, 4), np.uint8)
    body = a > 230; wing = (a > 0) & ~body
    out[wing] = (14, 26, 52, 150); out[body] = (6, 8, 14, 255)
    pad = np.pad(a > 0, 1)
    rim = (a > 0) & ~(pad[:-2, 1:-1] & pad[2:, 1:-1] & pad[1:-1, :-2] & pad[1:-1, 2:])
    top = (a > 0) & ~pad[:-2, 1:-1]                              # el borde de arriba, más claro (luz desde arriba)
    out[rim] = (74, 160, 240, 255); out[top] = (168, 224, 255, 255)
    Image.fromarray(out, 'RGBA').save(os.path.join(OUT, 'Sheets', 'Meganeura_Silhouette_Fly.png'))
    im = Image.fromarray(out, 'RGBA')
    rows = {'Silhouette_Fly (Side)': [im.crop((k * N, 4 * N, (k + 1) * N, 5 * N)) for k in range(4)],
            'Silhouette_Fly (Dn45, Up45)': [im.crop((0, 6 * N, N, 7 * N)), im.crop((0, 2 * N, N, 3 * N))]}
    preview(os.path.join(OUT, 'vista_previa_6f2a_silueta.png'), rows, C('#3a4a6a'), scale=2)
    print('   silueta')

if __name__ == '__main__':
    if 'muestra' in sys.argv[1:]: muestra()
    elif 'poses' in sys.argv[1:]: poses()
    elif 'todo' in sys.argv[1:]: todo()
    elif 'escenas' in sys.argv[1:]: escenas()
    elif 'variantes' in sys.argv[1:]: variantes(sys.argv[sys.argv.index('variantes') + 1:])
    elif 'golpes' in sys.argv[1:]:
        args = sys.argv[sys.argv.index('golpes') + 1:]
        golpes([x for x in args if x in ANIMS2] or None, next((x for x in args if x in VARIANTS), None))
    elif 'prueba_golpes' in sys.argv[1:]: prueba_golpes()
    elif 'momentos' in sys.argv[1:]:
        args = sys.argv[sys.argv.index('momentos') + 1:]
        momentos(next((x for x in args if x in VARIANTS), None))
    elif 'prueba_momentos' in sys.argv[1:]: prueba_momentos()
    elif 'escenas6e' in sys.argv[1:]: escenas_6e(sys.argv[sys.argv.index('escenas6e') + 1:] or None)
    elif 'silueta' in sys.argv[1:]: silueta()
    elif 'solo' in sys.argv[1:]: solo(sys.argv[sys.argv.index('solo') + 1:])
    else: print(__doc__)
