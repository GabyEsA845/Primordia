"""Megarachne, minijefe opcional del piso 3 de Primordia (tanda 6h: cuerpo). Skill primordia-sprites (pixel.py).

Euriptérido que se creyó araña gigante, con la referencia del usuario (referencia_megarachne.png: la maqueta antigua
de museo, cuando se reconstruía como araña). Forma de la foto con el guiño sutil del euriptérido:
- caparazón en cúpula con surcos radiales, montículo de los ojos (dos ojos oscuros) y «cara» con quelíceros gruesos;
- abdomen ancho y plano con surcos en abanico detrás y bandas de segmentos suaves (el euriptérido);
- telson corto que asoma por detrás, hileras pequeñas;
- 8 patas gruesas y nudosas, rodillas altas, punta negra en garra.

Dos paletas para elegir: A (tanda 0: gris violáceo con marcas rojas, patas arena de la foto) y B (colores de la
foto: pizarra oliva y patas arena, pocas marcas rojas).

Modelado con el motor de generar_arthropleura (slab, tube, sphere, render_geo: z-buffer, luz y contorno), picada 55°.
Celda 128×128, pivote = centro de la celda = centro del cuerpo en el suelo. 6 u (96 px) de punta a punta de las patas.

Uso: python generar_megarachne.py muestra        (la muestra del aspecto en una dirección, con las dos paletas)
     python generar_megarachne.py poses          (prueba rápida: un cuadro de cada animación)
     python generar_megarachne.py todo [Anim...] (6h: hojas en Sheets/ y Shadows/, vistas previas, GIF y escenas)
     python generar_megarachne.py escenas [salto curva aturdida]
     python generar_megarachne.py 6i1            (6i-1: variante del enfado de todo + momentos + vistas previas)
     python generar_megarachne.py momentos       (prueba rápida de la 6i-1: momentos y enfado de 3 animaciones)
     python generar_megarachne.py rage [Anim...] (solo la variante del enfado)   (escenas de prueba: PNG con cuadros clave + gif/escenas/)
"""
import math, os, random, sys
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
MT = os.path.normpath(os.path.join(HERE, '..', '..'))
sys.path.insert(0, os.path.join(MT, 'floor3-environment'))
sys.path.insert(0, os.path.join(HERE, '..', 'arthropleura'))
import generar_arthropleura as AR                      # Geo, slab, tube, sphere, bez, render_geo (motor 3D)
from generar_escala_arthropleura import font as F
from base_piso3 import *                               # noqa: F401,F403
from PIL import Image, ImageDraw

OUT = HERE
N = 128
BG = C('#30332a')                                      # suelo del Bosque humeante

# --- paletas (rampa de 5, de sombra a luz) ------------------------------------------------------
PAL_A = {                                              # tanda 0: gris violáceo, marcas rojas; patas de la foto
    'gbody':  ['#1c141e', '#30243a', '#4a3a4a', '#6e5a6a', '#9a8494'],
    'gdark':  ['#100a12', '#1c141e', '#2a1e2c', '#3a2c3c', '#4e3e50'],   # surcos y bandas de segmentos
    'gred':   ['#4a0e12', '#7a1a1e', '#a82a2a', '#c83a3a', '#f08070'],   # marcas rojas
    'gleg':   ['#3a2818', '#644628', '#9a7444', '#c4a06a', '#e6cc98'],   # patas color arena
    'gjoint': ['#2a1c12', '#4a3420', '#765834', '#9a7c50', '#bca070'],   # articulaciones nudosas
    'gclaw':  ['#0a0808', '#161012', '#241a1e', '#36282c', '#584448'],   # puntas negras en garra
    'gfang':  ['#120c10', '#22161c', '#3a2a30', '#5a444a', '#86707a'],   # quelíceros
    'eye':    ['#08060a', '#140e14', '#241a22', '#4a3238', '#e8dcd4'],   # ojos oscuros con reflejo rojo
    'under':  ['#241a24', '#3e2e3c', '#5e4a58', '#806a78', '#a08c98'],   # vientre: placas ventrales más claras
}
PAL_B = dict(PAL_A, **{                                # colores de la foto: pizarra oliva
    'gbody':  ['#16180f', '#2a2c20', '#45473a', '#686a58', '#94947c'],
    'gdark':  ['#0c0d08', '#16180f', '#22241a', '#2e3024', '#3c3e30'],
    'gred':   ['#3a0e0c', '#601a14', '#8a2a1e', '#a83a2a', '#d07058'],
    'under':  ['#0e100a', '#1a1c14', '#26281e', '#34362a', '#46483a'],
})
PALETTES = {'A': PAL_A, 'B': PAL_B}
NAMES = {'A': 'A · tanda 0: gris violáceo y rojo, patas de la foto', 'B': 'B · colores de la foto: pizarra oliva'}
AR.PALS.update(PAL_A)
AR.MATS = list(AR.PALS)
AR.MI = {m: i for i, m in enumerate(AR.MATS)}
AR.PAL_C = [[C(c) for c in AR.PALS[m]] for m in AR.MATS]
MI = AR.MI
PAL = {'k': 'A'}
RAGE_PAL = {                                           # enfado: rojo encendido y ojos que brillan
    'gred': ['#8a1414', '#d42420', '#ff4430', '#ff8a58', '#ffd8a8'],
    'eye':  ['#140406', '#2e080a', '#8a140e', '#e0301e', '#ffd0a0'],
}                                       # paleta activa (marcas: B lleva menos rojo)

# =================================================================================================
# Cuerpo (F hacia delante, L a un lado, Z arriba; px; el suelo es Z = 0)
# =================================================================================================
DF = 7.0                                               # desplaza todo hacia delante: el centro del cuerpo al pivote
CAR = dict(fc=13 + DF, a=14.0, b=13.0, h=7.0, uc=8.0)   # caparazón (prosoma)
ABD = dict(fc=-19 + DF, a=21.5, b=19.0, h=8.5, uc=8.5)  # abdomen (opistosoma)
SEG_F = (13.0, 6.5, 0.0, -6.5, -13.0)                  # bandas de segmentos del abdomen (F local)

def car_detail(F, L):
    r = np.hypot(F + 1, L); ang = np.arctan2(L, F + 1)
    fovea = -1.3 * np.exp(-((F + 1) ** 2 + L ** 2) / 3.5)                       # hoyo central
    striae = -.55 * np.clip(np.cos(ang * 8) * 3 - 2, 0, 1) * np.clip((r - 2.5) / 5, 0, 1)   # surcos radiales
    mound = 2.2 * np.exp(-((F - 7) ** 2 / 9 + L ** 2 / 20))                   # montículo de los ojos
    face = -.8 * np.exp(-((F - 11.5) ** 2 / 1.2)) * (np.abs(L) < 7)            # surco de la «cara»
    return fovea + striae + mound + face

def car_marks():
    m = {'gdark': lambda F, L: (np.cos(np.arctan2(L, F + 1) * 8) > .93) & (np.hypot(F + 1, L) > 3.5) & (np.hypot(F + 1, L) < 10)}
    if PAL['k'] == 'A':                                                          # línea roja por el centro
        w_ = 1.6 if PAL.get('rage') else .9
        m['gred'] = lambda F, L: (np.abs(L) < w_) & (F > -9) & (F < -3)
    return m

def abd_detail(F, L):
    segs = sum(-.5 * np.exp(-((F - f) / .7) ** 2) for f in SEG_F)              # bandas del euriptérido (suaves)
    fan = .38 * np.cos(np.arctan2(L, F - 9) * 18) * np.clip((-F - 1) / 12, 0, 1)   # abanico detrás
    keel = .6 * np.exp(-(L / 2.2) ** 2)
    return segs + fan + keel

def abd_marks():
    def bands(F, L): return np.min(np.abs(np.subtract.outer(F, np.array(SEG_F))), axis=1) < .45
    def fan(F, L): return (np.cos(np.arctan2(L, F - 9) * 18) < -.85) & (F < -6)
    m = {'gdark': lambda F, L: bands(F, L) | fan(F, L)}
    if PAL['k'] == 'A':                                                          # 3 pares de manchas rojas
        k_ = 1.45 if PAL.get('rage') else 1.0
        spots = [(8, 7.5, 2.0 * k_), (2, 9.5, 2.2 * k_), (-5, 9.0, 2.0 * k_)]
        m['gred'] = lambda F, L: np.any([((F - f) ** 2 + (np.abs(L) - l) ** 2) < r * r for f, l, r in spots], axis=0)
    else:                                                                        # B: una marca roja detrás
        m['gred'] = lambda F, L: ((F + 9) ** 2 / 5 + L ** 2 / 2.2) < 1
    return m

LEGS = [  # cadera (F, L), pie (F, L), altura de la rodilla
    ((18.5, 7.5), (38, 26), 24),
    ((15, 9.5), (17, 44), 27),
    ((11, 10.5), (-8, 44), 27),
    ((7, 9.5), (-30, 33), 24),
]
PIVOT = np.array([-1 + DF, 0.0, 9.5])                  # pedicelo: bisagra del abdomen
Z0 = 9.0                                               # altura del eje del cuerpo (giro sobre sí misma)

def leg(g, hip, foot, kz, side, grp, lift=0.0, fwd=0.0, curl=0.0, dz=0.0, out=0.0, spines=False, tuck=0.0):
    """Pata gruesa y nudosa: fémur que sube a la rodilla alta, tibia que baja, metatarso y garra negra.
    lift/fwd mueven el pie; curl recoge la pata hacia el cuerpo (colgada, caída, muerte); dz baja la cadera
    (agacharse); out abre el pie hacia fuera."""
    (hf, hl), (pf, pl) = hip, foot
    pf += fwd; pl += out
    hip3 = np.array([hf + DF, side * hl, 10.5 + dz])
    foot3 = np.array([pf + DF, side * pl, 0.0 + lift])
    if curl:                                            # pie hacia el cuerpo y hacia arriba
        foot3 = hip3 + (foot3 - hip3) * (1 - .55 * curl)
        foot3[2] = lift + (hip3[2] + 2) * curl
    knee = hip3 + (foot3 - hip3) * .42; knee[2] = max(hip3[2] + 4, kz + dz * .3 + lift * .4 + 6 * curl)
    ank = hip3 + (foot3 - hip3) * .8; ank[2] = 9 + lift * .8 + (foot3[2] - lift) * .6 * curl + dz * .2
    if tuck:                                             # muerte: patas recogidas hacia la panza (al girar, al cielo)
        t = tuck
        foot3 = hip3 + (np.array([pf + DF, side * pl, 0.0]) - hip3) * (1 - .62 * t); foot3[2] = (1 - t) * foot3[2] - 9 * t
        foot3[1] = hip3[1] + (foot3[1] - hip3[1]) * (1 - .35 * t)
        knee = hip3 + (foot3 - hip3) * .5; knee[1] = hip3[1] + (knee[1] - hip3[1]) * 1.25; knee[2] = knee[2] * (1 - t) + (hip3[2] - 4) * t
        ank = hip3 + (foot3 - hip3) * .82; ank[2] = ank[2] * (1 - t) + (foot3[2] + 3) * t
    ring = lambda t: 'gjoint' if t > .9 or t < .08 else 'gleg'                 # anillo oscuro junto a cada nudo
    AR.tube(g, AR.bez([hip3, (hip3 + knee) / 2 + np.array([0, 0, 4]), knee]), 2.5, 2.0, grp, ring, cap=False)
    AR.sphere(g, knee, 3.2, grp + 'k', 'gjoint')
    AR.tube(g, AR.bez([knee, (knee + ank) / 2 + np.array([0, 0, 2]), ank]), 2.0, 1.7, grp + 't', ring, cap=False)
    AR.sphere(g, ank, 2.5, grp + 'a', 'gjoint')
    AR.tube(g, AR.bez([ank, foot3]), 1.6, .6, grp + 'm', lambda t: 'gclaw' if t > .45 else ('gjoint' if t > .3 else 'gleg'))
    if spines:                                           # enfado: espinas erizadas en fémur y tibia
        out_v = np.array([0, side * 1.0, .9]); out_v /= np.linalg.norm(out_v)
        for k, (a_, b_, t) in enumerate(((hip3, knee, .35), (hip3, knee, .7), (knee, ank, .3), (knee, ank, .65))):
            base = a_ + (b_ - a_) * t
            AR.tube(g, AR.bez([base, base + out_v * 5.5 + (b_ - a_) / np.linalg.norm(b_ - a_) * 2.0]), 1.4, .35,
                    grp + f's{k}', lambda t: 'gred' if t > .45 else 'gclaw', cap=False)

def ellipsoid(g, c, r, grp, mat):
    """Elipsoide de radios r = (a lo largo, a lo ancho, alto); mat = material o función (F, L, Z) -> nombres."""
    rf, rl, rz = r
    n = max(14, int(math.pi * max(r) / .3))
    th, ph = np.meshgrid(np.linspace(0, math.pi, n), np.linspace(0, 2 * math.pi, 2 * n))
    u = np.stack([np.sin(th) * np.cos(ph), np.sin(th) * np.sin(ph), np.cos(th)], -1).reshape(-1, 3)
    P = u * np.array([rf, rl, rz])
    m = np.array([MI[x] for x in mat(P[:, 0], P[:, 1], P[:, 2])]) if callable(mat) else mat
    g.add(P + np.asarray(c, float), u / np.array([rf, rl, rz]), m, grp)

def belly_mats(F, L, Z):
    """Vientre de euriptérido (el guiño al quedar panza arriba): placas ventrales anchas con juntas oscuras y un
    surco en medio; arriba (lo que toca el lomo) el material normal."""
    out = np.full(len(F), 'under', dtype=object)
    joints = np.min(np.abs(np.subtract.outer(F, np.array([-13.0, -6.5, 0.0, 6.5, 13.0]))), axis=1) < .7
    out[joints & (Z < 1)] = 'gdark'
    out[(np.abs(L) < .8) & (Z < -2)] = 'gdark'
    return out

def rot_abd(P, a):
    """Alza el abdomen (a en grados, + = la punta sube; 110 = por encima del caparazón, como un escorpión)."""
    if not a: return P
    r = math.radians(a); c, s_ = math.cos(r), math.sin(r)
    v = P - PIVOT
    return np.stack([v[:, 0] * c + v[:, 2] * s_, v[:, 1], -v[:, 0] * s_ + v[:, 2] * c], 1) + PIVOT

def rot_roll(P, r_deg, z0=Z0):
    """Gira el cuerpo sobre su eje largo (+ = cae hacia su izquierda; 180 = panza arriba)."""
    if not r_deg: return P
    r = math.radians(r_deg); c, s_ = math.cos(r), math.sin(r)
    return np.stack([P[:, 0], P[:, 1] * c - (P[:, 2] - z0) * s_, P[:, 1] * s_ + (P[:, 2] - z0) * c + z0], 1)

def gait(i, side, t, stride=10.0, height=7.0):
    """Marcha en tetrápodo: (pata 0 izq., 1 der., 2 izq., 3 der.) y las contrarias van a contrafase.
    Devuelve (lift, fwd) del pie."""
    grp = (i + (side > 0)) % 2
    ph = (t + .5 * grp) % 1
    if ph < .5:                                          # en el aire: avanza
        u = ph / .5
        return height * math.sin(u * math.pi), -stride / 2 + stride * u
    u = (ph - .5) / .5                                   # apoyada: el cuerpo la deja atrás
    return 0.0, stride / 2 - stride * u

def body_geo(pose=None):
    """pose: walk (fase 0-1 o None), stride, feel (tantea con las delanteras), dz (agacharse, px), abd (alza el
    abdomen, grados), curl (patas recogidas 0-1), flail (fase de pataleo), roll (grados), legs_up (alza las
    delanteras: amenaza), twitch (fase de temblor, aturdida), out (patas abiertas, px), climb (fase al trepar)."""
    q = pose or {}
    g = AR.Geo()
    PAL['rage'] = bool(q.get('rage'))
    dz = q.get('dz', 0.0); curl = q.get('curl', 0.0)
    rnd = random.Random(int((q.get('flail') or 0) * 97) + 5)
    for i, (hip, foot, kz) in enumerate(LEGS):                  # patas
        for s in (-1, 1):
            lift = fwd = 0.0
            if q.get('walk') is not None:
                lift, fwd = gait(i, s, q['walk'], q.get('stride', 10.0))
            if q.get('feel') is not None and i == 0:            # tantea: las delanteras suben y bajan desfasadas
                ph = q['feel'] + (.5 if s > 0 else 0)
                lift = 3 + 3 * math.sin(ph * 2 * math.pi); fwd = 2 * math.cos(ph * 2 * math.pi)
            if q.get('legs_up') and i == 0:                     # amenaza: delanteras al aire (wave: se agitan)
                lift = 22 * q['legs_up']; fwd = -4 * q['legs_up']
                if q.get('wave') is not None:
                    ph = q['wave'] + (.25 if s > 0 else 0)
                    lift += 4 * math.sin(ph * 2 * math.pi); fwd += 3 * math.cos(ph * 2 * math.pi)
            c = curl
            if q.get('flail') is not None:                      # pataleo al caer
                lift += rnd.uniform(0, 10); fwd += rnd.uniform(-6, 6); c = max(0, curl + rnd.uniform(-.2, .2))
            if q.get('twitch') is not None:                     # temblor aturdida
                ph = q['twitch'] + i * .37 + (s > 0) * .21
                lift += 2.5 * max(0, math.sin(ph * 2 * math.pi * 2))
            if q.get('climb') is not None and i < 2:            # trepar: delanteras tiran del hilo alternando
                ph = q['climb'] + i * .5 + (s > 0) * .25
                lift += 6 * math.sin(ph * 2 * math.pi); fwd += 4 * math.cos(ph * 2 * math.pi)
            leg(g, hip, foot, kz, s, f'leg{i}{s}', lift=lift, fwd=fwd, curl=c, dz=dz, out=q.get('out', 0.0),
                spines=bool(q.get('rage')), tuck=q.get('tuck', 0.0))
    n_abd = len(g.P)
    ellipsoid(g, (ABD['fc'] - .5, 0, ABD['uc'] + 1.5 + dz), (ABD['a'] * .9, ABD['b'] * .86, 6.5), 'belly', belly_mats)   # vientre con volumen
    AR.slab(g, ABD['fc'], 0, ABD['uc'] + dz, ABD['a'], ABD['b'], ABD['h'], 'abd', p=2.3, s0=.3, detail=abd_detail,
            mat='gbody', marks=abd_marks())
    for s in (-1, 1):
        AR.tube(g, AR.bez([(ABD['fc'] - 17, s * 2.2, 8.5 + dz), (ABD['fc'] - 20, s * 3.2, 7.5 + dz)]), 1.2, .8, f'spin{s}',
                lambda t: 'gjoint')                             # hileras
    tail = AR.bez([(ABD['fc'] - 16, 0, 9.5 + dz), (ABD['fc'] - 22, 0, 8.5 + dz), (ABD['fc'] - 27, 0, 6.5 + dz)])
    AR.tube(g, tail, 2.4, .5, 'telson', lambda t: 'gred' if (t > .8 and PAL['k'] == 'A') else 'gdark')
    a = q.get('abd', 0.0)
    for k in range(n_abd, len(g.P)):                           # el abdomen gira en el pedicelo
        g.P[k] = rot_abd(g.P[k] - np.array([0, 0, dz]), a) + np.array([0, 0, dz])
        g.N[k] = rot_abd(g.N[k] + PIVOT, a) - PIVOT
    AR.sphere(g, (-1 + DF, 0, 9.5 + dz), 4.5, 'pedicel', 'gdark')
    AR.slab(g, CAR['fc'], 0, CAR['uc'] + dz, CAR['a'], CAR['b'], CAR['h'], 'car', p=2.2, s0=.25, detail=car_detail,
            mat='gbody', marks=car_marks())
    for s in (-1, 1):
        AR.sphere(g, (CAR['fc'] + 7, s * 4.0, CAR['uc'] + CAR['h'] + 1.2 + dz), 2.8, f'eye{s}', 'eye')
        AR.sphere(g, (CAR['fc'] + 14, s * 3.8, 7.5 + dz), 4.0, f'chel{s}', 'gfang', squash=.9)   # quelíceros gruesos
        AR.tube(g, AR.bez([(CAR['fc'] + 16.5, s * 3.6, 5.5 + dz), (CAR['fc'] + 19, s * 2.8, 3.5 + dz), (CAR['fc'] + 18.5, s * 1.2, 2 + dz)]),
                1.2, .4, f'fang{s}', lambda t: 'gclaw')
    pt = q.get('pitch', 0.0)
    if pt:                                               # cabeceo: + = la cabeza sube (amenaza, salir del embudo)
        g.P = [rot_pitch(P_, pt) for P_ in g.P]
        g.N = [rot_pitch(N_, pt, z0=0, f0=0) for N_ in g.N]
    r = q.get('roll', 0.0)
    if r:
        g.P = [rot_roll(P_, r) for P_ in g.P]
        g.N = [rot_roll(N_, r, z0=0) for N_ in g.N]
    if q.get('emerge') is not None:                      # sale del embudo: hunde el cuerpo y recorta
        emerge(g, q['emerge'])
    return g

HOLE_W = (18.0, 24.0)                                    # semiejes del embudo en el mundo (F, L) = 48×30 px en pantalla

def rot_pitch(P, deg, z0=Z0, f0=0.0):
    r = math.radians(deg); c, s_ = math.cos(r), math.sin(r)
    vF, vZ = P[:, 0] - f0, P[:, 2] - z0
    return np.stack([vF * c - vZ * s_ + f0, P[:, 1], vF * s_ + vZ * c + z0], 1)

def emerge(g, k):
    """k = 0 dentro del embudo … 1 fuera. Hunde el cuerpo (y lo adelanta: sale la cabeza primero) bajo la superficie
    de la copa (Z = 0). Lo que queda por debajo se borra salvo dentro del agujero, donde se ve oscurecido."""
    depth = -34 * (1 - k); shiftF = -16 * (1 - k)
    dark = MI['gdark']
    for i in range(len(g.P)):
        P = g.P[i].copy(); P[:, 0] += shiftF; P[:, 2] += depth
        inside = (P[:, 0] / HOLE_W[0]) ** 2 + (P[:, 1] / HOLE_W[1]) ** 2 < 1
        keep = (P[:, 2] >= -.5) | inside
        M = g.M[i].copy(); M[(P[:, 2] < -5) & inside] = dark
        g.P[i], g.N[i], g.M[i], g.G[i] = P[keep], g.N[i][keep], M[keep], g.G[i][keep]

def spinneret(pose=None):
    """Punto de las hileras (origen de la seda) en coordenadas del cuerpo, con la pose aplicada."""
    q = pose or {}; dz = q.get('dz', 0.0)
    P = np.array([[ABD['fc'] - 20, 0, 7.5 + dz]])
    P = rot_abd(P - np.array([0, 0, dz]), q.get('abd', 0.0)) + np.array([0, 0, dz])
    return rot_roll(P, q.get('roll', 0.0))[0]

def project(P, yaw):
    """Punto del cuerpo → píxel de la celda (misma proyección que render_geo)."""
    y = math.radians(yaw); cy, sy = math.cos(y), math.sin(y)
    X = P[0] * cy - P[1] * sy; Y = P[0] * sy + P[1] * cy
    se, ce = math.sin(AR.ELEV), math.cos(AR.ELEV)
    return X + N / 2, -(Y * se + P[2] * ce) + N / 2

def shadow_of(g, yaw):
    P, _, _, _ = g.arrays()
    P = P.copy(); P[:, 2] = 0
    y = math.radians(yaw); cy, sy = math.cos(y), math.sin(y)
    X = P[:, 0] * cy - P[:, 1] * sy; Y = P[:, 0] * sy + P[:, 1] * cy
    xs, ys = X + N / 2, -(Y * math.sin(AR.ELEV)) + N / 2
    im = Image.new('RGBA', (N, N), (0, 0, 0, 0)); p = im.load()
    for x, y_ in zip(np.floor(xs).astype(int), np.floor(ys).astype(int)):
        if 0 <= x < N and 0 <= y_ < N: p[x, y_] = (10, 6, 12, 255)
    return im

SILK_C = (226, 222, 236, 255); SILK_D = (150, 144, 168, 255)

def weave_ground(yaw):
    """Punto del suelo (px de la celda) donde el tramo de hilo de Weave toca el suelo: el código sigue el hilo fino
    desde aquí hasta el ancla."""
    return project(np.array([ABD['fc'] - 34, 0, 0]), yaw)

def hang_origin(pose):
    """Píxel de la celda (1 dirección, Dn90) donde nace el hilo del que cuelga: las hileras."""
    return tuple(round(v) for v in project(spinneret(pose), HANG_YAW))

def head_top(pose, yaw, img=None):
    """Píxel de la celda para el efecto de aturdida: en x, sobre la cabeza; en y, por encima del píxel más alto de
    la silueta (las rodillas quedan más altas que la cabeza)."""
    q = pose or {}; dz = q.get('dz', 0.0)
    P = np.array([[CAR['fc'] + 4, 0, CAR['uc'] + CAR['h'] + dz]])
    x, y = project(rot_roll(P, q.get('roll', 0.0))[0], yaw)
    if img is None: img = render(yaw, **q)[0]
    top = img.getchannel('A').getbbox()[1]
    return round(x), max(14, top - 4)

def draw_thread(im, x0, y0, x1, y1):
    """Hilo de 1 px (con un píxel de sombra al lado) en la celda."""
    d = ImageDraw.Draw(im)
    d.line([(x0 + 1, y0), (x1 + 1, y1)], fill=SILK_D, width=1)
    d.line([(x0, y0), (x1, y1)], fill=SILK_C, width=1)

def render(yaw=0.0, pal='A', **pose):
    """Devuelve (sprite, sombra). pose['thread'] = 'back' (teje: tramo corto de hilo fino desde las hileras al suelo,
    hacia atrás; el código sigue el hilo desde ese punto del suelo hasta el ancla). El hilo del que cuelga (Drop,
    Climb, entrada) NO va en el sprite: lo dibuja el código desde el anclaje hasta las hileras (hang_origin)."""
    PAL['k'] = pal
    g = body_geo(pose)
    over = {} if pal == 'A' else dict(PALETTES[pal])
    if pose.get('rage'): over.update(RAGE_PAL)
    cv = AR.render_geo(g, yaw, N, pal=over or None)
    im = cv.img().convert('RGBA')
    th = pose.get('thread')
    if th:
        sx, sy = project(spinneret(pose), yaw)
        lay = Image.new('RGBA', (N, N), (0, 0, 0, 0))
        if th == 'back':                                 # al suelo, detrás
            bx, by = weave_ground(yaw)
            draw_thread(lay, sx, sy, bx, by)
        lay.alpha_composite(im); im = lay
    return im, shadow_of(g, yaw)

# =================================================================================================
# 6h: animaciones del cuerpo (16 direcciones = 9 dibujadas + Flip X; lo que cuelga, 1 dirección)
# =================================================================================================
DIRS = AR.DIRS
def sw(i, n, k=1.0): return math.sin(i / n * 2 * math.pi * k)

ANIMS = {
    # quieta: tantea con las delanteras, respira
    'Idle':     [dict(feel=i / 4, dz=-.6 * abs(sw(i, 4))) for i in range(4)],
    # anda (en cable y en suelo; en el enfado el código la acelera): tetrápodo de 8 cuadros
    'Walk':     [dict(walk=i / 8, dz=-.8 * abs(sw(i, 8, 2))) for i in range(8)],
    # teje: anda más despacio con el abdomen bajo, soltando el hilo fino desde las hileras
    'Weave':    [dict(walk=i / 8, stride=7, abd=-9, dz=-1.2, thread='back') for i in range(8)],
    # ráfaga de seda: alza el abdomen sobre el caparazón (escorpión) y dispara; LA SEDA SALE EN EL CUADRO 4
    'SilkShoot': [dict(abd=a, dz=d, legs_up=u) for a, d, u in
                  ((0, 0, 0), (40, -1, 0), (75, -2, 0), (96, -3, .15), (102, -4, .1), (97, -3, .1), (60, -1.5, 0), (22, 0, 0))],
    # aviso del salto: se agacha en el cable con las delanteras abiertas
    'DropWarn': [dict(dz=-2 * k, out=2 * k, legs_up=.15 * k) for k in (1, 2, 3)],
    # aterriza en el suelo tras dejarse caer: golpe, se aplasta y se recompone
    'Land':     [dict(dz=-5, out=5), dict(dz=-3, out=3), dict(dz=-1, out=1)],
    # daño: los dos cuadros de la regla (blanco y rojo) sobre Idle 0 (se generan aparte)
    # cae del cable que arde: gira de lado pataleando
    'Fall':     [dict(roll=r, curl=.35, flail=i + 1, abd=-6) for i, r in enumerate((25, 55, 85, 110))],
    # aturdida en el suelo: tumbada, patas abiertas que tiemblan
    'Stunned':  [dict(dz=-7, out=9, curl=.2, twitch=i / 4, roll=12 + 5 * sw(i, 4), abd=-10) for i in range(4)],
    # se recupera: se levanta
    'Recover':  [dict(dz=-7 + 2.3 * k, out=9 - 3 * k, roll=12 - 4 * k, abd=-10 + 3.3 * k) for k in (1, 2, 3)],
}
HANG = {                                                 # 1 dirección (Dn90: mirando abajo), cuelga de su hilo
    # baja colgada (salto y entrada): patas recogidas que se mecen
    'Drop':     [dict(curl=.65 + .1 * sw(i, 4), abd=-14) for i in range(4)],
    # trepa por el hilo: las delanteras tiran alternando
    'Climb':    [dict(curl=.55, climb=i / 4, abd=-14) for i in range(4)],
}
MOMENTS = {                                              # 6i-1, 16 direcciones
    # amenaza (entrada y enfado): se alza con las delanteras arriba y las agita
    'Threat':   [dict(legs_up=u, pitch=p_, abd=a, dz=d, wave=i / 4 if i >= 2 else None) for i, (u, p_, a, d) in
                 enumerate(((.35, 6, 4, 0), (.75, 12, 8, 1), (1, 18, 12, 1.5), (1, 18, 12, 1.5), (1, 18, 12, 1.5), (1, 18, 12, 1.5)))],
    # muere: rueda panza arriba pataleando y encoge las patas (el rojo se apaga: sin paleta de enfado)
    'Death':    [dict(roll=r, tuck=c, flail=(i + 1) if 0 < i < 5 else None) for i, (r, c) in
                 enumerate(((0, .1), (30, .3), (70, .45), (110, .6), (150, .75), (172, .88), (180, .95), (180, 1.0)))],
    'Dead':     [dict(roll=180, tuck=1.0)],
}
INTRO = {                                                # 6i-1, 1 dirección (Dn90), sobre el embudo del Nido
    # sale del embudo: primero la cabeza, las delanteras se agarran al borde
    'IntroEmerge': [dict(emerge=k, pitch=p_, legs_up=u) for k, p_, u in
                    ((.05, 55, .9), (.2, 50, .9), (.35, 42, .8), (.5, 32, .6), (.65, 22, .4), (.8, 12, .2), (.92, 5, .1), (1, 0, 0))],
    # chilla desde dentro del embudo (enfado): medio fuera, delanteras agitándose; se enciende en el cuadro 3
    'Screech':     [dict(emerge=.5, pitch=32, legs_up=1, wave=i / 3, rage=i >= 3) for i in range(6)],
}
MS = {'Threat': 110, 'Death': 100, 'Dead': 100, 'IntroEmerge': 110, 'Screech': 90, 'Idle': 180, 'Walk': 80, 'Weave': 110, 'SilkShoot': 90, 'DropWarn': 120, 'Land': 90, 'Fall': 80, 'Stunned': 140,
      'Recover': 110, 'Drop': 120, 'Climb': 100, 'Hurt': 80}
HANG_YAW = -90.0

STAR_HI, STAR_LO, STAR_CORE = C('#ffe680'), C('#c8a040'), C('#fff8d8')

def fx_stunned(i, n=8):
    """Aturdida (64×28, 8 cuadros en bucle): 4 estrellas que giran en una órbita por encima de la cabeza, con estela;
    las de detrás más pequeñas y apagadas. Mismo estilo que Fx_Stunned (tanda 4), a la escala del minijefe.
    Pivote = centro de la celda (va en el ancla head_top de cada dirección)."""
    W_, H_, RX, RY = 64, 28, 24, 7
    im = Image.new('RGBA', (W_, H_), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    cx, cy = W_ // 2, H_ // 2
    for k in range(48):                                   # órbita punteada tenue
        a = k / 48 * 2 * math.pi
        if k % 3 == 0: d.point((cx + RX * math.cos(a), cy + RY * math.sin(a)), fill=STAR_HI[:3] + (80,))
    stars = sorted((math.sin((i / n + k / 4) * 2 * math.pi), (i / n + k / 4) * 2 * math.pi) for k in range(4))
    BIG = [(0, -3), (0, -2), (-1, -1), (0, -1), (1, -1), (-3, 0), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (3, 0),
           (-1, 1), (0, 1), (1, 1), (-2, 2), (-1, 2), (1, 2), (2, 2)]          # estrella de 5 puntas 7×6
    SMALL = [(0, -2), (-1, -1), (0, -1), (1, -1), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (-1, 1), (1, 1)]
    for depth, a in stars:                                # de detrás a delante
        x, y = round(cx + RX * math.cos(a)), round(cy + RY * math.sin(a))
        front = depth > -.2
        col = STAR_HI if front else STAR_LO
        for t in (1, 2, 3):                               # estela
            at = a - t * .22
            d.point((round(cx + RX * math.cos(at)), round(cy + RY * math.sin(at))), fill=col[:3] + (170 - 45 * t,))
        shape = BIG if front else SMALL
        for dx, dy in shape:                              # contorno oscuro para que se lea sobre el cuerpo
            for ox, oy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                if (dx + ox, dy + oy) not in shape: d.point((x + dx + ox, y + dy + oy), fill=OUTL[:3] + (200,))
        for dx, dy in shape: d.point((x + dx, y + dy), fill=col)
        if front: d.point((x, y), fill=STAR_CORE); d.point((x - 1, y - 1), fill=STAR_CORE)
    return im

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

def poses():
    """Prueba rápida: un cuadro de cada animación en Side y Dn45."""
    rows = {}
    for yaw in (0, -45):
        rows[f'yaw {yaw}'] = [render(yaw, **ANIMS[n][min(3, len(ANIMS[n]) - 1)])[0] for n in ANIMS]
    rows['colgada'] = [render(HANG_YAW, **HANG[n][1])[0] for n in HANG]
    preview(os.path.join(OUT, 'vista_previa_6h_poses.png'), rows, BG, scale=2)
    print('poses:', ' · '.join(list(ANIMS) + list(HANG)))

def todo(names=None):
    for d_ in ('Sheets', 'Shadows', 'gif'): os.makedirs(os.path.join(OUT, d_), exist_ok=True)
    side, origins = {}, {}
    for name, poses_ in ANIMS.items():
        if names and name not in names: continue
        rows, srows = [], []
        for dname, yaw in DIRS:
            fr = [render(yaw, **pz) for pz in poses_]
            rows.append([f for f, _ in fr]); srows.append([s_ for _, s_ in fr])
            if dname == 'Side': side[name] = rows[-1]
            if name == 'SilkShoot': origins[dname] = tuple(round(v) for v in project(spinneret(poses_[4]), yaw))
        grid(rows).save(os.path.join(OUT, 'Sheets', f'Megarachne_{name}.png'))
        grid(srows).save(os.path.join(OUT, 'Shadows', f'Megarachne_Shadow_{name}.png'))
        if name == 'Idle':                                  # Hurt sobre Idle 0 en las 9 direcciones
            grid([hurt(r[0]) for r in rows]).save(os.path.join(OUT, 'Sheets', 'Megarachne_Hurt.png'))
            grid([[r[0]] * 2 for r in srows]).save(os.path.join(OUT, 'Shadows', 'Megarachne_Shadow_Hurt.png'))
            side['Hurt'] = hurt(rows[4][0])
        print('  ', name, flush=True)
    for name, poses_ in HANG.items():
        if names and name not in names: continue
        fr = [render(HANG_YAW, **pz) for pz in poses_]
        grid([[f for f, _ in fr]]).save(os.path.join(OUT, 'Sheets', f'Megarachne_{name}.png'))
        grid([[s_ for _, s_ in fr]]).save(os.path.join(OUT, 'Shadows', f'Megarachne_Shadow_{name}.png'))
        prev = []
        for (f, _), pz in zip(fr, poses_):                  # vista previa: hilo hasta el borde de la celda
            ox, oy = hang_origin(pz)
            lay = Image.new('RGBA', (N, N), (0, 0, 0, 0)); draw_thread(lay, ox, 0, ox, oy); lay.alpha_composite(f); prev.append(lay)
        side[name] = prev
        print('  ', name, 'hilo desde las hileras (px):', [hang_origin(pz) for pz in poses_], flush=True)
    if not names or 'Stunned' in names:
        os.makedirs(os.path.join(OUT, 'Effects'), exist_ok=True)
        fx = [fx_stunned(i) for i in range(8)]
        strip = Image.new('RGBA', (64 * 8, 28), (0, 0, 0, 0))
        for i, f in enumerate(fx): strip.paste(f, (i * 64, 0))
        strip.save(os.path.join(OUT, 'Effects', 'Fx_Megarachne_Stunned.png'))
        gif(os.path.join(OUT, 'gif', 'Fx_Megarachne_Stunned.gif'), fx, 70, BG, scale=4)
        print('   ancla de Fx_Megarachne_Stunned (Stunned, px de la celda):',
              {dn: [head_top(pz, yaw) for pz in ANIMS['Stunned'][:1]] for dn, yaw in DIRS}, '(fijo durante la animación)')
    if not names or 'Weave' in names:
        print('   Weave: el hilo toca el suelo en (px de la celda):', {dn: tuple(round(v) for v in weave_ground(yaw)) for dn, yaw in DIRS})
    if origins: print('Origen de la seda (SilkShoot cuadro 4, px de la celda):', origins)
    if not names:
        preview(os.path.join(OUT, 'vista_previa_6h_animaciones.png'), side, BG, scale=2)
        dirs = {f'{n} (9 direcciones)': [render(yaw, **ANIMS[n][k])[0] for _, yaw in DIRS]
                for n, k in (('Idle', 0), ('Walk', 2), ('SilkShoot', 4), ('Stunned', 0))}
        preview(os.path.join(OUT, 'vista_previa_6h_direcciones.png'), dirs, BG, scale=1)
    for name, frames in side.items():
        gif(os.path.join(OUT, 'gif', f'Megarachne_{name}.gif'), frames, MS[name], BG, scale=2)
    print('Listo:', OUT)

# =================================================================================================
# Muestra
# =================================================================================================
def rage(names=None):
    """6i-1: variante del enfado de todas las animaciones de cuerpo (Sheets/Rage/, mismas filas y cuadros)."""
    os.makedirs(os.path.join(OUT, 'Sheets', 'Rage'), exist_ok=True)
    side = {}
    for name, poses_ in list(ANIMS.items()) + [('Threat', MOMENTS['Threat'])]:
        if names and name not in names: continue
        rows = [[render(yaw, **dict(pz, rage=True))[0] for pz in poses_] for _, yaw in DIRS]
        grid(rows).save(os.path.join(OUT, 'Sheets', 'Rage', f'Megarachne_{name}_Rage.png'))
        side[name] = rows[4]
        if name == 'Idle':
            grid([hurt(r[0]) for r in rows]).save(os.path.join(OUT, 'Sheets', 'Rage', 'Megarachne_Hurt_Rage.png'))
        print('  ', name, 'Rage', flush=True)
    for name, poses_ in HANG.items():
        if names and name not in names: continue
        fr = [render(HANG_YAW, **dict(pz, rage=True))[0] for pz in poses_]
        grid([fr]).save(os.path.join(OUT, 'Sheets', 'Rage', f'Megarachne_{name}_Rage.png'))
        print('  ', name, 'Rage', flush=True)
    return side

def momentos():
    """6i-1: Threat, Death y Dead (16 direcciones) e IntroEmerge y Screech (1 dirección, sobre el embudo)."""
    os.makedirs(os.path.join(OUT, 'Sheets'), exist_ok=True); os.makedirs(os.path.join(OUT, 'Shadows'), exist_ok=True)
    side = {}
    for name, poses_ in MOMENTS.items():
        rows, srows = [], []
        for dname, yaw in DIRS:
            fr = [render(yaw, **pz) for pz in poses_]
            rows.append([f for f, _ in fr]); srows.append([s_ for _, s_ in fr])
            if dname == 'Side': side[name] = rows[-1]
        grid(rows).save(os.path.join(OUT, 'Sheets', f'Megarachne_{name}.png'))
        grid(srows).save(os.path.join(OUT, 'Shadows', f'Megarachne_Shadow_{name}.png'))
        print('  ', name, flush=True)
    for name, poses_ in INTRO.items():
        fr = [render(HANG_YAW, **pz)[0] for pz in poses_]
        grid([fr]).save(os.path.join(OUT, 'Sheets', f'Megarachne_{name}.png'))
        side[name] = fr
        print('  ', name, flush=True)
    return side

def vista_6i(side_m, side_r):
    """Lámina 6i-1: normal y enfado lado a lado, momentos, Dead en 9 direcciones, y la salida del embudo y el
    chillido montados sobre el árbol del Nido."""
    import generar_nido_megarachne as NG
    preview(os.path.join(OUT, 'vista_previa_6i_momentos.png'), {k: v for k, v in side_m.items() if k not in INTRO}, BG, scale=2)
    comp = {}
    for name in ('Idle', 'Walk', 'SilkShoot', 'Threat'):
        normal = side_m['Threat'] if name == 'Threat' else [render(0, **pz)[0] for pz in ANIMS[name]]
        comp[f'{name} normal'] = normal; comp[f'{name} enfado'] = side_r[name]
    preview(os.path.join(OUT, 'vista_previa_6i_enfado.png'), comp, BG, scale=2)
    dead = {'Dead (9 direcciones)': [render(yaw, **MOMENTS['Dead'][0])[0] for _, yaw in DIRS]}
    preview(os.path.join(OUT, 'vista_previa_6i_dead.png'), dead, BG, scale=1)
    rows = []
    for name, dry in (('IntroEmerge', False), ('Screech', False)):
        tree = NG.tree(dry); tree.alpha_composite(NG.nest_layer(fresh=dry))
        fr = []
        for f in side_m[name]:
            t = tree.copy(); t.alpha_composite(f, (NG.CX - N // 2, NG.CY - N // 2)); fr.append(t)
        rows.append((name, fr))
        gif(os.path.join(OUT, 'gif', f'Megarachne_{name}_Nido.gif'), fr, MS[name], BG, scale=2)
    preview(os.path.join(OUT, 'vista_previa_6i_embudo.png'), dict(rows), BG, scale=2)
    for name, frames in side_m.items():
        if name not in INTRO: gif(os.path.join(OUT, 'gif', f'Megarachne_{name}.gif'), frames, MS[name], BG, scale=2)
    for name, frames in side_r.items():
        gif(os.path.join(OUT, 'gif', f'Megarachne_{name}_Rage.gif'), frames, MS.get(name, 100), BG, scale=2)
    print('Listo 6i-1')

def smoke_floor(w, h, seed=3):
    tiles = os.path.join(MT, 'floor3-environment', 'Smoke', 'tiles')
    fl = [Image.open(os.path.join(tiles, f'Floor_{k}.png')).convert('RGBA') for k in range(6)]
    rnd = random.Random(seed); im = Image.new('RGBA', (w, h))
    for y in range(0, h, 16):
        for x in range(0, w, 16): im.alpha_composite(fl[0 if rnd.random() < .66 else rnd.randint(1, 5)], (x, y))
    return im

def with_shadow(bg, spr, sh, x, y, a=150):
    s = sh.copy(); s.putalpha(s.getchannel('A').point(lambda v: a if v else 0))
    bg.alpha_composite(s, (x - N // 2, y - N // 2))
    bg.alpha_composite(spr, (x - N // 2, y - N // 2))

def muestra():
    s = 3
    ref = Image.open(os.path.join(HERE, 'referencia_megarachne.png')).convert('RGBA')
    ref = ref.resize((int(ref.width * 330 / ref.height), 330))
    frog = Image.open(os.path.join(MT, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')).convert('RGBA').crop((0, 0, 32, 32))
    rows = []
    for k in 'AB':
        side, sh = render(0, k)
        diag, _ = render(-45, k)
        up, _ = render(90, k)
        scene = smoke_floor(240, 150)
        with_shadow(scene, side, sh, 150, 78)
        scene.alpha_composite(frog, (24, 70))
        rows.append((k, side, diag, up, scene))
    W = 20 + ref.width + 20 + 3 * (N * s + 16) + 20
    H = 50 + 2 * (N * s + 40) + 40 + 150 * 2 + 40
    full = Image.new('RGBA', (W, H), tuple(BG[:3]) + (255,))
    d = ImageDraw.Draw(full)
    d.text((20, 10), 'Megarachne · 6h muestra: picada 55°, 6 u de patas (celda 128), dos paletas · Side, Dn45 y Up90 a ×3',
           fill=(255, 240, 200), font=F(20))
    y = 50
    full.alpha_composite(ref, (20, y + 20))
    d.text((20, y + 20 + ref.height + 6), 'referencia del usuario', fill=(200, 210, 200), font=F(15))
    x0 = 20 + ref.width + 20
    for k, side, diag, up, scene in rows:
        d.text((x0, y), NAMES[k], fill=(255, 240, 200), font=F(17))
        for i, im in enumerate((side, diag, up)):
            full.alpha_composite(im.resize((N * s, N * s), Image.NEAREST), (x0 + i * (N * s + 16), y + 26))
        y += N * s + 40
    d.text((20, y), 'En el juego (×2), junto a la rana de 32 px, con su sombra:', fill=(255, 240, 200), font=F(17))
    y += 28
    for i, (k, side, diag, up, scene) in enumerate(rows):
        full.alpha_composite(scene.resize((scene.width * 2, scene.height * 2), Image.NEAREST), (20 + i * (scene.width * 2 + 20), y))
        d.text((28 + i * (scene.width * 2 + 20), y + 6), k, fill=(255, 240, 200), font=F(18), stroke_width=3, stroke_fill=(10, 10, 14))
    out = os.path.join(OUT, 'vista_previa_6h_muestra.png')
    full.convert('RGB').save(out); print('  ', os.path.basename(out), full.size)


# =================================================================================================
# Escenas de prueba (6h): salto desde el cable y curva andando (16 direcciones con Flip X)
# =================================================================================================
_CACHE = {}
def sprite_for(name, k, ang, shadow_=False):
    """Cuadro k de la animación name mirando a ang (0 = derecha, 90 = arriba): cuantiza a 16 y voltea.
    Lo que cuelga (HANG) tiene una sola dirección."""
    if name in HANG:
        key = (name, k, HANG_YAW)
        if key not in _CACHE: _CACHE[key] = render(HANG_YAW, **HANG[name][k])
        return _CACHE[key][1 if shadow_ else 0]
    q = round(ang / 22.5) * 22.5
    q = (q + 180) % 360 - 180
    flip = abs(q) > 90
    base = (180 - q if q > 0 else -180 - q) if flip else q
    key = (name, k, base)
    if key not in _CACHE: _CACHE[key] = render(base, **ANIMS[name][k])
    im = _CACHE[key][1 if shadow_ else 0]
    return im.transpose(Image.FLIP_LEFT_RIGHT) if flip else im

def place(bg, spr, x, y, sh=None, gx=None, gy=None, a=120):
    """Sprite con el pivote (centro de la celda) en (x, y); sombra con su pivote en el suelo (gx, gy)."""
    if sh is not None:
        s_ = sh.copy(); s_.putalpha(s_.getchannel('A').point(lambda v: a if v else 0))
        bg.alpha_composite(s_, (int((x if gx is None else gx) - N // 2), int((y if gy is None else gy) - N // 2)))
    bg.alpha_composite(spr, (int(x - N // 2), int(y - N // 2)))

def caption(im, text):
    d = ImageDraw.Draw(im)
    d.text((6, im.height - 16), text, fill=(255, 240, 200), font=F(11), stroke_width=2, stroke_fill=(10, 10, 14))

def escena_salto():
    """En su cable: anda, aviso (se agacha), se deja caer colgada de su hilo sobre la marca, aterriza, queda en el
    suelo (ventana de daño ~1,5 s) y trepa de vuelta por el hilo."""
    import generar_escala_megarachne as E
    W, H = 360, 260
    base = smoke_floor(W, H, seed=11)
    CY, ALT = 70, 120                                    # cable en y = 70 (pantalla); el suelo bajo él, 120 px más abajo
    frog = Image.open(os.path.join(MT, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')).convert('RGBA').crop((0, 0, 32, 32))
    DX = 210                                             # x del salto
    steps = []
    for i in range(8): steps.append(('Walk', i, 120 + i * 11, CY, 'en su cable (invulnerable)'))
    for i in range(3): steps.append(('DropWarn', i, DX, CY, 'aviso: se agacha; la marca se fija'))
    for i in range(3): steps.append(('DropWarn', 2, DX, CY, 'aviso: se agacha; la marca se fija'))
    d0 = N // 2 - hang_origin(HANG['Drop'][0])[1]       # del pivote a las hileras: empieza con las hileras en el cable
    for i in range(6): steps.append(('Drop', i % 4, DX, CY + d0 + (ALT - d0) * (i / 5) ** 1.6, 'se deja caer colgada de su hilo'))
    for i in range(3): steps.append(('Land', i, DX, CY + ALT, 'aterriza'))
    for i in range(8): steps.append(('Idle', i % 4, DX, CY + ALT, 'en el suelo: ventana de daño (~1,5 s)'))
    for i in range(8): steps.append(('Climb', i % 4, DX, CY + d0 + (ALT - d0) * (1 - (i + 1) / 8), 'trepa de vuelta por el hilo'))
    frames = []
    for (name, k, x, y, txt) in steps:
        im = base.copy()
        gy = CY + ALT
        if name in ('Walk', 'DropWarn', 'Drop', 'Land'):          # marca en el suelo (provisional; la de verdad, 6j)
            d = ImageDraw.Draw(im)
            col = (255, 200, 80, 255) if name == 'Walk' else (220, 50, 50, 255)
            d.ellipse([DX - 18, gy - 9, DX + 18, gy + 9], outline=col, width=1)
        im.alpha_composite(frog, (DX - 120, gy - 22))
        E.cable_shadow(im, (0, gy - 6), (W, gy - 6))
        E.cable(im, (-10, CY), (W + 10, CY), sag=4, seed=2)
        hang = name in HANG
        if hang:                                            # hilo: del cable a las hileras (lo hará el código)
            ox, oy = hang_origin((HANG[name])[k])
            draw_thread(im, x, CY, x - N // 2 + ox, y - N // 2 + oy)
        ang = 0 if name == 'Walk' else -90 if hang else 0
        spr = sprite_for(name, k, ang); sh = sprite_for(name, k, ang, True)
        a = int(60 + 70 * min(1, (y - CY) / ALT)) if (hang or y < gy) else 130
        place(im, spr, x, y, sh, x, gy, a=a)
        caption(im, txt)
        frames.append(im)
    return frames

def escena_curva():
    """Anda en círculo por el suelo: pasa por las 16 direcciones (9 dibujadas + Flip X)."""
    W, H = 300, 240
    base = smoke_floor(W, H, seed=5)
    frames = []
    n = 32
    for i in range(n):
        t = i / n * 2 * math.pi
        x, y = W / 2 + 80 * math.cos(t), H / 2 + 62 * math.sin(t) * .82
        vx, vy = -math.sin(t), math.cos(t) * .82                 # tangente en pantalla
        ang = math.degrees(math.atan2(-vy / .82, vx))
        im = base.copy()
        place(im, sprite_for('Walk', i % 8, ang), x, y, sprite_for('Walk', i % 8, ang, True))
        caption(im, f'Walk en curva · {round(ang / 22.5) * 22.5 % 360:.1f}°')
        frames.append(im)
    return frames

FIRE_PX = 6 * 16 * .09                                   # 6 u/s a 90 ms por cuadro ≈ 8,6 px por cuadro

def _flames():
    path = os.path.join(MT, 'floor3-mutations', 'Effects', 'Fx_GroundFire.png')
    im = Image.open(path).convert('RGBA')
    return [im.crop((i * 32, 0, i * 32 + 32, 24)) for i in range(im.width // 32)]

def burning_cable(im, C_, ign, front, t_burnt, j, rnd, fallen=0.0):
    """Cable (línea central C_ de puntos en pantalla) que arde como una mecha desde ign (índice) con el frente a
    front puntos de distancia en cada sentido; t_burnt[i] = cuadro en que ardió el punto i (o None).
    Estados por punto: normal (seda), ardiendo (llamas, ~6 puntos detrás del frente), carbonizado (negro, fino,
    se descuelga más cuanto más tiempo lleva quemado). fallen > 0: el tramo carbonizado ya cayó (solo cabos)."""
    import generar_escala_megarachne as E
    n = len(C_) - 1
    state = []
    for i in range(n + 1):
        dist = abs(i - ign)
        if dist > front: state.append('normal')
        elif dist > front - 6: state.append('burn')
        else: state.append('char')
    # tramos normales: seda como E.cable, por trozos contiguos
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    half = 7
    def edge(pts, k):
        """Borde del cable a ±half px de la línea central (normal por diferencia con los vecinos)."""
        out = []
        for i, (x, y) in enumerate(pts):
            a, b = pts[max(0, i - 1)], pts[min(len(pts) - 1, i + 1)]
            dx, dy = b[0] - a[0], b[1] - a[1]; L = math.hypot(dx, dy) or 1
            out.append((x - dy / L * half * k, y + dx / L * half * k * .82))
        return out
    runs, cur = [], []
    for i, st in enumerate(state):
        if st == 'normal': cur.append(i)
        elif cur: runs.append(cur); cur = []
    if cur: runs.append(cur)
    for r in runs:
        if len(r) < 2: continue
        pts = [C_[i] for i in r]
        e1, e2 = edge(pts, 1), edge(pts, -1)
        d.polygon(e1 + e2[::-1], fill=(226, 222, 236, 70))
        for k_, ph in ((-.25, 0), (.25, 2.1), (0, 4.0)):
            d.line([(x + (a[0] - x) * (k_ + .3 * math.sin(i * .5 + ph)), y + (a[1] - y) * (k_ + .3 * math.sin(i * .5 + ph)))
                    for i, ((x, y), a) in enumerate(zip(pts, e1))], fill=(226, 222, 236, 210), width=1)
        d.line(e1, fill=(196, 190, 212, 235), width=1); d.line(e2, fill=(150, 144, 168, 235), width=1)
    # carbonizado: hilo negro fino que se descuelga
    char = [i for i, st in enumerate(state) if st != 'normal']
    if char and fallen <= 0:
        pts = []
        for i in range(min(char), max(char) + 1):
            x, y = C_[i]
            age = (j - t_burnt[i]) if t_burnt[i] is not None else 0
            sag = min(18, age * 1.6) * math.sin(math.pi * (i - min(char)) / max(1, max(char) - min(char)))
            pts.append((x, y + 3 + sag))
        d.line(pts, fill=(34, 26, 30, 255), width=2)
        for k in range(0, len(pts), 3):
            if rnd.random() < .35: d.point(pts[k], fill=(255, 150, 60, 255))     # ascuas en el hilo
            if rnd.random() < .25: d.point((pts[k][0] + rnd.uniform(-2, 2), pts[k][1] + rnd.uniform(4, 16)), fill=(120, 110, 110, 200))  # ceniza
    im.alpha_composite(lay)
    # llamas a lo largo del tramo que arde (por encima)
    fl = _flames()
    for i, st in enumerate(state):
        if st == 'burn' and i % 2 == 0:
            x, y = C_[i]
            f = fl[(j + i) % len(fl)]
            im.alpha_composite(f, (int(x - 16), int(y - 20)))
    return state

def stub(im, x, y, side, k_fade=1.0):
    """Cabo chamuscado que cuelga de un ancla (side = hacia dónde iba el cable) con la punta en ascua."""
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    a = int(255 * k_fade)
    pts = [(x + side * (t * 9 + 2 * math.sin(t * 5)), y + 34 * t ** 1.5) for t in [i / 10 for i in range(11)]]
    d.line(pts, fill=(20, 14, 18, a), width=4)                                                      # contorno
    d.line(pts, fill=(78, 62, 64, a), width=2)                                                      # seda chamuscada
    d.line([(px - side, py) for px, py in pts], fill=(140, 118, 112, a), width=1)                   # borde que coge luz
    d.line([(px, py) for px, py in pts[:3]], fill=(226, 222, 236, int(a * .8)), width=2)            # seda sana junto al ancla
    d.point(pts[-1], fill=(255, 150, 60, a)); d.point((pts[-1][0], pts[-1][1] - 1), fill=(255, 210, 120, a))
    im.alpha_composite(lay)

def escena_aturdida():
    """Concepto del cable que arde (las piezas definitivas, en la 6j): la rana prende el cable cerca de ella; el
    fuego corre como una mecha a ~6 u/s en los dos sentidos; ella huye hacia el Nido pero el frente la alcanza y
    cae; aturdida con las estrellas (ventana de daño); el fuego se apaga con humo junto al Nido (seda húmeda antes
    del enfado); el cable carbonizado se descuelga y cae; quedan los cabos; una ascua prende una telaraña del suelo."""
    import generar_escala_megarachne as E
    W, H = 380, 250
    base = smoke_floor(W, H, seed=8)
    CY, ALT = 70, 120
    gy = CY + ALT
    NX = W + 6                                              # el Nido, a la derecha (centro fuera de cuadro)
    E.cable_shadow(base, (0, gy - 6), (NX - 40, gy - 6))
    E.web(base, 120, gy + 8, 1.2)                           # telaraña del suelo (la prenderá una ascua)
    TX = 30                                                 # Sigillaria del borde: ancla izquierda del cable
    sig = Image.open(os.path.join(MT, 'floor3-environment', 'Smoke', 'props', 'Obstacle_Sigillaria.png')).convert('RGBA')
    base.alpha_composite(sig, (TX - sig.width // 2, CY + 44 - sig.height + 4))
    frog = Image.open(os.path.join(MT, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')).convert('RGBA').crop((0, 0, 32, 32))
    n = 60
    C_ = [(TX + (NX - 40 - TX) * t, CY + 6 * 4 * t * (1 - t)) for t in [i / n for i in range(n + 1)]]
    step = (NX - 40 - TX) / n                               # px por punto
    ign = int((150 - TX) / step)                            # prende en x ≈ 150
    fx = [fx_stunned(i) for i in range(8)]
    fl = _flames()
    rnd = random.Random(4)
    t_burnt = [None] * (n + 1)
    sx, mode, k_anim, fall_y = 205.0, 'walk', 0, 0.0
    embers, ground_fire = [], None
    frames = []
    total = 76
    for j in range(total):
        im = base.copy()
        front = -1 if j < 3 else (j - 3) * FIRE_PX / step
        for i in range(n + 1):
            if t_burnt[i] is None and abs(i - ign) <= front - 6: t_burnt[i] = j
        reached_end = front > max(ign, n - ign) + 6
        if reached_end and 'drop_at' not in locals(): drop_at = j + 6
        fallen = 1.0 if ('drop_at' in locals() and j >= drop_at) else 0.0
        im.alpha_composite(frog, (60, gy - 22))
        if j < 3:                                           # la rana escupe fuego al cable (chispa provisional)
            d = ImageDraw.Draw(im); d.line([(80, gy - 12), (150, CY)], fill=(255, 170, 60, 200), width=1)
        if fallen <= 0:
            state = burning_cable(im, C_, ign, front, t_burnt, j, rnd)
            for i, st in enumerate(state):                  # ascuas que caen de lo que arde
                if st == 'burn' and rnd.random() < .08: embers.append([C_[i][0], C_[i][1], rnd.uniform(4, 7)])
            if j == 6: embers.append([121, CY + 4, 6.0])        # una cae seguro sobre la telaraña
        else:
            k_f = max(0.0, min(1.0, 1.6 - (j - drop_at) / 22))
            stub(im, TX, CY, 1, k_f); stub(im, NX - 40, CY, -1, k_f)
            if j - drop_at < 4:                             # el cable carbonizado cae: ceniza que se dispersa
                d = ImageDraw.Draw(im)
                for k in range(30):
                    d.point((rnd.uniform(TX, NX - 40), CY + 10 + (j - drop_at) * 18 + rnd.uniform(-6, 12)), fill=(70, 62, 64, 200))
        if front > (n - ign) - 2:                           # llega al Nido: se apaga con bocanadas de humo que suben
            if 'smoke_at' not in locals(): smoke_at = j
            lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
            for k in range(6):
                age = j - smoke_at - k * 2
                if age < 0 or age > 14: continue
                r = 2 + age * .5
                px_, py_ = NX - 44 + 3 * math.sin(age * .6 + k), CY - 4 - age * 3
                d.ellipse([px_ - r, py_ - r, px_ + r, py_ + r], fill=(170, 166, 170, int(150 * (1 - age / 14))))
            im.alpha_composite(lay)
        E.nest(im, NX, gy, ALT, shadow=False)
        # ascuas que caen; la primera que toca la telaraña la prende
        d = ImageDraw.Draw(im)
        for e in embers:
            e[1] += e[2]
            if e[1] < gy + 8: d.point((e[0], e[1]), fill=(255, 170, 60, 255)); d.point((e[0], e[1] - 1), fill=(255, 220, 120, 160))
            elif ground_fire is None and abs(e[0] - 120) < 20: ground_fire = j
        embers = [e for e in embers if e[1] < gy + 8]
        if ground_fire is not None:
            for k, ox in enumerate((-12, 0, 12)):
                if j - ground_fire >= k * 2: im.alpha_composite(fl[(j + k) % len(fl)], (120 + ox - 16, gy - 10))
        # la Megarachne
        right_front = C_[min(n, int(ign + front))][0] if front >= 0 else -1e9
        if mode == 'walk':
            if front >= 0: sx += 4.5                         # huye hacia el Nido
            if right_front >= sx - 10 and front >= 0: mode, fall_y, k_anim = 'fall', 0.0, 0
            spr, sh = sprite_for('Walk', j % 8, 0), sprite_for('Walk', j % 8, 0, True)
            place(im, spr, sx, CY + 4, sh, sx, gy, a=70)
            txt = 'la rana prende el cable' if j < 3 else 'el fuego corre por el cable (~6 u/s): huye'
        elif mode == 'fall':
            fall_y = min(ALT, fall_y + 14 + k_anim * 6); k_anim += 1
            spr, sh = sprite_for('Fall', min(3, k_anim), 0), sprite_for('Fall', min(3, k_anim), 0, True)
            place(im, spr, sx, CY + fall_y, sh, sx, gy, a=int(70 + 60 * fall_y / ALT))
            if fall_y >= ALT: mode, k_anim = 'stun', 0
            txt = 'el frente la alcanza: cae'
        else:
            spr, sh = sprite_for('Stunned', k_anim % 4, 0), sprite_for('Stunned', k_anim % 4, 0, True)
            place(im, spr, sx, gy, sh, sx, gy, a=130)
            hx, hy = head_top(ANIMS['Stunned'][0], 0)
            im.alpha_composite(fx[k_anim % 8], (int(sx - N // 2 + hx - 32), int(gy - N // 2 + hy - 14)))
            k_anim += 1
            txt = 'aturdida (ventana de daño)' + (' · quedan los cabos' if fallen else '')
        if ground_fire is not None and j - ground_fire < 12: txt = txt.split(' · ')[0] + ' · una ascua prende el suelo'
        caption(im, txt)
        frames.append(im)
    return frames

def escenas(names=None):
    os.makedirs(os.path.join(OUT, 'gif', 'escenas'), exist_ok=True)
    todo_ = {'salto': (escena_salto, 90), 'curva': (escena_curva, 80), 'aturdida': (escena_aturdida, 90)}
    for name, (fn, ms) in todo_.items():
        if names and name not in names: continue
        frames = fn()
        gif(os.path.join(OUT, 'gif', 'escenas', f'Megarachne_{name}.gif'), frames, ms, BG, scale=2)
        pick = frames[::max(1, len(frames) // 8)][:8]                # lámina con cuadros clave
        w, h = frames[0].size
        sheet = Image.new('RGBA', (w * 4 + 30, h * 2 + 30), tuple(BG[:3]) + (255,))
        for j, f in enumerate(pick): sheet.alpha_composite(f, (6 + (j % 4) * (w + 6), 6 + (j // 4) * (h + 6)))
        sheet = sheet.resize((sheet.width * 2, sheet.height * 2), Image.NEAREST)
        sheet.convert('RGB').save(os.path.join(OUT, f'vista_previa_6h_escena_{name}.png'))
        print('  escena', name, len(frames), 'cuadros')

if __name__ == '__main__':
    cmd = sys.argv[1] if len(sys.argv) > 1 else 'muestra'
    if cmd == 'muestra': muestra()
    elif cmd == 'poses': poses()
    elif cmd == 'momentos': vista_6i(momentos(), rage(['Idle', 'Walk', 'SilkShoot', 'Threat']))
    elif cmd == 'rage': rage(sys.argv[2:] or None)
    elif cmd == '6i1': sr = rage(); vista_6i(momentos(), sr)
    elif cmd == 'todo': todo(sys.argv[2:] or None); escenas()
    elif cmd == 'escenas': escenas(sys.argv[2:] or None)
