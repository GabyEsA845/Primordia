"""Salto alto de las Patas de salto (piso 3 de Primordia): poses de salto de los tres cuerpos del jugador.

La esquiva con las Patas de salto es un salto alto: el código sube el sprite en arco (≈1,5 u en el punto medio) y deja una
sombra en el suelo (Fx_FlyerShadow de la libélula, escalada). Estas hojas solo dibujan la pose de cada fase, siempre en el
sitio (sin elevar dentro del lienzo), y el código elige el cuadro por el avance del salto (README).
- Frog: se agacha, se impulsa con las patas traseras estiradas, vuela, cae con las manos por delante y amortigua.
- Jump_Charge (nivel III, salto cargado): agachado del todo, temblando, mientras se mantiene el botón (bucle).

Reutiliza las formas de ../floor3-bodies/generar_cuerpos_piso3.py (importado como B). Skill primordia-sprites.

Uso: python generar_salto_piso3.py [carpeta_salida] [frog] [diplo] [whip]
"""
import math, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', 'floor3-bodies'))
_argv = sys.argv[:]
sys.argv = sys.argv[:1]                       # el generador de cuerpos lee sys.argv al importarse
import generar_cuerpos_piso3 as B
sys.argv = _argv
from pixel import *
from PIL import Image

OUT = sys.argv[1] if len(sys.argv) > 1 and not sys.argv[1] in ('frog', 'diplo', 'whip', 'marker', 'web') else HERE
BG = B.BG
BODIES = os.path.join(HERE, '..', 'floor3-bodies')   # las hojas de salto se escriben junto a las de cada cuerpo

# Fase del salto de cada cuadro (avance de 0 a 1 en que empieza): el código elige el cuadro así.
PHASES = [0.0, 0.12, 0.35, 0.62, 0.88]
ARC_PX = 24                                   # altura del arco en el punto medio (1,5 u) para las vistas previas

# =================================================================================================
# Rana de perfil: copia de B.frog_side con las patas del salto (impulso, vuelo, caída con las manos delante)
# =================================================================================================

def _limb(cv, parts, ink, edge):
    """Pata del lado de la cámara, por delante del cuerpo: se dibuja aparte, con contorno propio, y su borde sobre el
    cuerpo se oscurece (ink) en lugar del contorno exterior."""
    leg = Canvas(cv.w if hasattr(cv, 'w') else 32)
    for (x, y, rx, ry, ang) in parts:
        rot_ellipse(leg, x, y, rx, ry, ang, ink, shift=-1)
    inside = set(leg.px)
    leg.outline(); leg.clean()
    for p, c in leg.px.items():
        if p not in inside and p in cv.px and cv.px[p] != OUTL:
            cv.px[p] = C(edge)                      # borde interior sobre el cuerpo
        elif p in inside or p not in cv.px:
            cv.px[p] = c

def frog_jump_side(pose, pal=B.FROG_PAL, eyes='open', shake=0, squash=None):
    """pose: 'crouch', 'push', 'fly', 'fall', 'land'. shake: desplazamiento de 1 px (temblor de la carga)."""
    P = {
        #          aplast. estir. cuerpo_dy cabeza_dy
        'crouch': (0.85, 0.0, 0.0, 0.0),
        'push':   (0.0, 0.5, -1.2, -1.4),
        'fly':    (0.0, 0.5, -0.4, -0.8),
        'fall':   (0.0, 0.2, 0.4, 1.0),
        'land':   (0.6, 0.0, 0.0, 0.0),
    }[pose]
    q, s, bdy, hdy = P
    if squash is not None: q = squash
    cv = Canvas(32)
    Bd = pal['body']; body = Bd[1:]
    oy = 2.2 * q
    bx, by = 16 - 1 * s + shake, 18.5 + oy - 0.8 * s + bdy
    hx, hy = 24.2 + 1 * s + shake, 14.5 + oy - 1.6 * s - 0.6 * q + hdy
    if pose in ('crouch', 'land'):
        rot_ellipse(cv, 11 + shake, 22.5 + oy - 0.3 * q, 6.2 + q, 4.4 - q, -0.3, Bd[:4], shift=-1)        # anca plegada
    else:
        # pata del otro lado, asomando detrás (más oscura)
        far = {'push': (6.0, 25.5, 4.2, 1.6, 0.75), 'fly': (6.0, 22.4, 4.2, 1.6, 0.12), 'fall': (10.6, 25.6, 3.2, 1.6, 0.5)}[pose]
        rot_ellipse(cv, far[0] + shake, far[1], far[2], far[3], far[4], [Bd[0]] + Bd[:3], shift=-1)
    rot_ellipse(cv, bx, by, 10.0 * (1 + .12 * q) + 1.2 * s, 7.4 * (1 - .18 * q) - 0.8 * s,
                -0.32 - 0.25 * s + (0.22 if pose == 'fall' else 0), body, shift=-1)
    ellipse(cv, hx, hy, 6.0, 5.0 - .3 * q, body, shift=-1)                                                # cabeza
    ellipse(cv, hx - .7, hy - 4.3 + .3 * q, 3.3, 3.1, body, shift=-1)                                     # ojo saltón
    if pose in ('crouch', 'land'):
        rot_ellipse(cv, 22.5 + shake, 23.5 + oy - 0.4 * q, 1.8, 3.6 - .6 * q, 0.25 + .5 * q, Bd[:4], shift=-1)
    elif pose in ('push', 'fly'):
        rot_ellipse(cv, 22.2 + shake, 21.4 + bdy, 1.5, 3.0, 1.0, Bd[:4], shift=-1)                         # brazo pegado
    else:
        rot_ellipse(cv, 26.4 + shake, 23.4 + bdy, 1.6, 3.8, -0.6, Bd[:4], shift=-1)                         # brazo delante
    cv.outline(); cv.clean()
    # pata trasera del lado de la cámara, por delante del cuerpo
    near = {
        'push': [(9.0, 23.2, 4.6, 2.6, 0.75), (5.4, 27.0, 3.6, 1.5, 0.55)],   # muslo y pantorrilla empujando abajo-atrás
        'fly':  [(9.0, 21.4, 5.0, 2.4, 0.18), (4.4, 22.6, 3.2, 1.4, 0.05)],   # estiradas hacia atrás
        'fall': [(11.2, 22.8, 4.4, 2.7, 0.85), (9.6, 26.0, 3.2, 1.5, -0.3)],  # se recogen bajo el cuerpo
    }.get(pose)
    if near:
        _limb(cv, [(x + shake, y, rx, ry, a) for (x, y, rx, ry, a) in near], Bd[:4], Bd[0])
    ddx, ddy = round(bx - 16), round(by - 18.5)
    if pal['details']:
        B.spots(cv, ((8, 15, 3, 2), (12, 12, 3, 2), (16, 15, 3, 3), (11, 19, 2, 2), (19, 12, 2, 2)), ddx, ddy, pal['ink'])
        B.spots(cv, ((26, 13, 2, 1),), round(hx - 24.2), round(hy - 14.5), pal['ink'])
        if near:
            B.spots(cv, ((round(near[0][0]) - 1, round(near[0][1]) - 1, 2, 1),), shake, 0, pal['ink'])
        if pose in ('crouch', 'land'):
            for x in range(6, 16):                                                                          # pliegue del anca
                y = round(20.5 + oy - 0.3 * q + (x - 7) * 0.35)
                if cv.px.get((x + shake, y)) not in (None, OUTL): cv.put(x + shake, y, C(Bd[0]))
        cv.put_in(12 + ddx, 12 + ddy, C(Bd[4])); cv.put_in(13 + ddx, 11 + ddy, C(Bd[4]))
        cv.put_in(14 + ddx, 11 + ddy, C(Bd[3]))
    mx, my = round(hx - 2.2), round(hy + 2.5)                                                               # boca cerrada
    for x in range(mx, round(hx + 6)): cv.put_in(x, my + (1 if x < mx + 2 else 0), C(Bd[0]))
    ex, ey = round(hx - 2.2), round(hy - 5.5 + .3 * q)
    B.eye(cv, ex, ey, (1, 1) if pose == 'fall' else (1, 0), eyes, pal['eye'], lid=C(Bd[1]))
    if pal['details']: cv.put_in(ex - 1, ey - 1, C(Bd[3]))
    # dedos finos (sin contorno)
    if pose in ('crouch', 'land'):
        fy = round(27 + oy - 2.2 * q + 0.4 * q)
        for dx in (-1, 1, 2): cv.put_out(22.5 + shake + dx, fy, C(Bd[1]))
        for dx in (-3, -2, 0): cv.put_out(11 + shake + dx, round(26.6 + oy), C(Bd[1]))
    elif pose == 'push':
        for k in range(3): cv.put_out(1 + shake + k, 30 - (k == 2), C(Bd[1]))                               # pie empujando
    elif pose == 'fly':
        for k in range(3): cv.put_out(1 + shake, 22 + k, C(Bd[1]))                                          # pie estirado
    else:
        for dx in (0, 1, 2): cv.put_out(27 + shake + dx, round(28.2 + bdy) - (1 if dx == 2 else 0), C(Bd[1]))  # mano abierta
        for dx in (-1, 0, 1): cv.put_out(8 + shake + dx, 28, C(Bd[1]))                                       # pie recogido
    return cv

CELL = 40                                     # lienzo de las hojas de salto: el de 32 con 4 px de margen (cuerpo centrado igual)

def pad(cv, n=CELL):
    """El cuadro de 32×32 centrado en un lienzo de n×n."""
    if cv.w == n:
        return cv
    o = (n - cv.w) // 2
    out = Canvas(n)
    out.px = {(x + o, y + o): c for (x, y), c in cv.px.items()}
    return out

def shifted(cv, dx):
    """El mismo cuadro movido dx px en horizontal (temblor de la carga)."""
    out = Canvas(cv.w, cv.h)
    out.px = {(x + dx, y): c for (x, y), c in cv.px.items() if 0 <= x + dx < cv.w}
    out.info = getattr(cv, 'info', None)
    return out

def frog_jump_front(pose, back=False, eyes='open', squash=None):
    """Rana de frente (down) o de espaldas (up, back=True) en cada fase del salto, con las formas de B.frog_front."""
    k = {'crouch': dict(squash=.85), 'push': dict(stretch=1.0), 'fly': dict(stretch=.6),
         'fall': dict(), 'land': dict(squash=.6)}[pose]
    if squash is not None: k['squash'] = squash
    cv = B.frog_front(back=back, eyes=eyes, **k)
    if pose == 'fall' and not back:
        # manos estiradas hacia la cámara para recibir el suelo
        Bd = B.FROG_PAL['body']
        _limb(cv, [(10.2, 26.2, 1.7, 3.4, 0.3), (21.8, 26.2, 1.7, 3.4, -0.3)], Bd[:4], Bd[0])
        for side, x0 in ((-1, 9), (1, 22)):
            for dx in (-1, 0, 1): cv.put_out(x0 + side * .6 + dx * 1.3, 30 - (1 if dx == 0 else 0), C(Bd[1]))
    return cv

def frog_jump_anims():
    A = {}
    poses = ('crouch', 'push', 'fly', 'fall', 'land')
    A['Player_Frog_Jump_side'] = [pad(frog_jump_side(p)) for p in poses]
    A['Player_Frog_Jump_down'] = [pad(frog_jump_front(p)) for p in poses]
    A['Player_Frog_Jump_up'] = [pad(frog_jump_front(p, back=True)) for p in poses]
    # Carga: más agachada que el despegue y temblando (1 px a cada lado, cuerpo que se aplasta un poco más y menos).
    A['Player_Frog_JumpCharge_side'] = [shifted(pad(frog_jump_side('crouch', eyes='squint', squash=q)), s)
                                        for q, s in ((1.0, 0), (0.92, 1), (1.0, 0), (0.92, -1))]
    for v, back in (('down', False), ('up', True)):
        A[f'Player_Frog_JumpCharge_{v}'] = [shifted(pad(frog_jump_front('crouch', back, 'squint', q)), s)
                                            for q, s in ((1.0, 0), (.92, 1), (1.0, 0), (.92, -1))]
    return A


# =================================================================================================
# Diplocaulus y vinagrillo: modelos 3D de B con todo el cuerpo inclinado (morro arriba al despegar, abajo al caer)
# =================================================================================================

def _pitch_point(f, u, deg, u0):
    a = math.radians(deg)
    du = u - u0
    return f * math.cos(a) - du * math.sin(a), u0 + f * math.sin(a) + du * math.cos(a)

def pitched_parts(parts, deg, u0=2.5):
    """Inclina el animal entero alrededor de su centro (f=0, u=u0): mueve los centros y cabecea las piezas alargadas
    hacia delante (cuerpo, cola, placa de la cabeza); los cuernos y las patas solo se mueven."""
    if not deg:
        return parts
    out = []
    for p in parts:
        f, l, u = p.c
        f2, u2 = _pitch_point(f, u, deg, u0)
        along = abs(((p.alpha + 180) % 360) - 180) < 60 and p.group != 'leg'
        out.append(B.Part(f2, l, u2, *p.r, alpha=p.alpha, group=p.group, mat=p.mat, pitch=p.pitch + (deg if along else 0)))
        if hasattr(p, 'stripe'): out[-1].stripe = p.stripe
    return out

def pitched_lines(lines, deg, u0=2.5, spread=0.0):
    """Las partes finas con la misma inclinación. spread (vinagrillo): abre las 6 patas en el aire."""
    out = []
    for i, (pts, cols) in enumerate(lines):
        new = []
        for j, (f, l, u) in enumerate(pts):
            if spread and i < 6 and j > 0:                       # rodilla y pie: más arriba y más abiertos
                l *= 1 + (.15 if j == 1 else .28) * spread
                u += (1.4 if j == 1 else 2.6) * spread
            f2, u2 = _pitch_point(f, u, deg, u0)
            new.append((f2, l, u2))
        out.append((new, cols))
    return out

def diplo_jump(view, pitch=0.0, shake=0, **pose):
    orig = B.diplo_parts
    B.diplo_parts = lambda **k: pitched_parts(orig(**k), pitch)
    try:
        o = B.DIPLO_ORIGIN[view]
        cv = B.diplo_render(view, n=CELL, origin=(o[0] + (CELL - 32) / 2, o[1] + (CELL - 32) / 2), **pose)
    finally:
        B.diplo_parts = orig
    return shifted(cv, shake) if shake else cv

DIPLO_JUMP = [
    dict(crouch=1.0, stretch=-.3, A=.3, pitch=-6),                    # se encoge como un muelle
    dict(stretch=.8, tuck=.6, A=.4, phase=.2, pitch=20),              # sale disparado, la cabeza por delante
    dict(stretch=.6, tuck=1.0, A=1.2, phase=.5, pitch=6),             # en el aire, la cola ondea
    dict(stretch=.4, A=.6, phase=.8, pitch=-14),                      # cae de panza, morro abajo
    dict(crouch=1.0, stretch=.2, A=.4, phase=1.0),                    # panzazo
]

def diplo_jump_anims():
    A = {}
    for v in ('side', 'down', 'up'):
        A[f'Player_Diplocaulus_Jump_{v}'] = [diplo_jump(v, **f) for f in DIPLO_JUMP]
        A[f'Player_Diplocaulus_JumpCharge_{v}'] = [
            diplo_jump(v, crouch=1.0, stretch=-.35, A=.3, curl=c, pitch=-6, shake=s)
            for c, s in ((4, 0), (-2, 1), (4, 0), (-2, -1))]
    return A

def whip_jump(view, pitch=0.0, spread=0.0, shake=0, **pose):
    op, ol = B.whip_parts, B.whip_lines
    u0 = 2.4 - .5 * pose.get('crouch', 0)
    B.whip_parts = lambda **k: pitched_parts(op(**k), pitch, u0)
    B.whip_lines = lambda **k: pitched_lines(ol(**k), pitch, u0, spread)
    try:
        cv = B.whip_render(view, n=CELL, **pose)
    finally:
        B.whip_parts, B.whip_lines = op, ol
    return shifted(cv, shake) if shake else cv

WHIP_JUMP = [
    dict(crouch=1.0, claw=.6),                                        # se recoge
    dict(raise_=.45, claw=.3, pitch=14),                              # se impulsa, la cola sube
    dict(raise_=.65, claw=.2, pitch=4, spread=1.0),                   # en el aire, patas abiertas y cola en alto
    dict(raise_=.35, claw=.9, pitch=-14, spread=.5),                  # cae sobre las pinzas
    dict(crouch=.7, claw=.5, raise_=.15),                             # apoya
]

def whip_jump_anims():
    A = {}
    for v in ('side', 'down', 'up'):
        A[f'Player_Whipscorpion_Jump_{v}'] = [whip_jump(v, **f) for f in WHIP_JUMP]
        A[f'Player_Whipscorpion_JumpCharge_{v}'] = [
            whip_jump(v, crouch=1.0, claw=.6, raise_=.25, tremble=tr, shake=s)
            for tr, s in ((1, 0), (-1, 1), (1, 0), (-1, -1))]
    return A

# =================================================================================================
# Marcador del salto cargado (nivel III): diana de huellas sobre el suelo
# =================================================================================================
LEG = ramp('#5a9a3a', 5)                      # verde rana de las Patas de salto (el del muelle del despegue)
GLOW = '#ffe680'                              # amarillo del muelle: carga completa

def footprint(cv, cx, cy, col, hi):
    """Huella de rana: almohadilla y 4 dedos largos y abiertos, con la punta redonda."""
    for x in (-1, 0, 1):
        cv.put(cx + x, cy, C(col)); cv.put(cx + x, cy + 1, C(col))
    cv.put(cx - 1, cy, C(hi))
    for pts in (((-2, -1), (-3, -2)), ((-1, -2), (-1, -3)), ((1, -2), (1, -3)), ((2, -1), (3, -2))):
        for k, (dx, dy) in enumerate(pts):
            cv.put(cx + dx, cy + dy, C(hi if k == len(pts) - 1 else col))

def marker_spot(i, n=6, full=False):
    """Anillo punteado de 22×12 que gira (8 trazos), con la huella en el centro. full: carga al máximo (late en amarillo)."""
    cv = Canvas(24, 16)
    cx, cy, rx, ry = 12, 8, 10.5, 5.5
    pulse = (math.sin(i / n * 2 * math.pi) + 1) / 2
    for a10 in range(0, 3600, 25):
        a = a10 / 10
        seg = (a - i * 45 / n) % 45                                            # un trazo avanza en todo el bucle
        if seg > 26:
            continue                                                           # hueco entre trazos
        x, y = cx + rx * math.cos(math.radians(a)), cy + ry * math.sin(math.radians(a))
        front = math.sin(math.radians(a)) > 0                                   # mitad de delante, más clara
        if full:
            col = GLOW if (front and pulse > .5) else LEG[4] if front else LEG[3]
        else:
            col = LEG[4] if front else LEG[3]
        cv.put(x, y, C(col))
    footprint(cv, cx, cy + 1, LEG[3] if not full else LEG[4], LEG[4] if not full else GLOW)
    return cv

def marker_corner(k):
    """Esquina tenue del radio del impacto (8×8): k = 0 arriba-izq, 1 arriba-der, 2 abajo-der, 3 abajo-izq; apunta hacia fuera."""
    cv = Canvas(8, 8)
    sx = -1 if k in (0, 3) else 1
    sy = -1 if k in (0, 1) else 1
    x0 = 1 if sx < 0 else 6
    y0 = 1 if sy < 0 else 6
    for d in range(4):
        cv.put(x0 - sx * d, y0, C(LEG[3], 200 - d * 30))
        cv.put(x0, y0 - sy * d, C(LEG[3], 200 - d * 30))
    return cv

def marker_anims():
    return {
        'Fx_JumpMarker_Spot': [marker_spot(i) for i in range(6)],
        'Fx_JumpMarker_Full': [marker_spot(i, full=True) for i in range(6)],
        'Fx_JumpMarker_Corner': [marker_corner(k) for k in range(4)],
    }

# =================================================================================================
# Red de las Patas hilanderas (nivel III): prototipo del tejido en tiempo real (el código C# hará lo mismo)
# =================================================================================================
SILK = ['#7a8a84', '#aebcb4', '#d8e2dc', '#f2f8f4']        # seda (la de Fx_SilkThread)
DEW = ['#c8a040', '#ffe680']                               # rocío dorado: la telaraña es tuya
RING_PX = 7                                                # separación de los anillos (px)

def _line(x0, y0, x1, y1):
    n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
    return [(round(x0 + (x1 - x0) * k / n), round(y0 + (y1 - y0) * k / n)) for k in range(n + 1)]

def web_pixels(poly, grow=1.0, seed=3):
    """Píxeles de la red tejida sobre el polígono convexo poly [(x, y) en px]. grow 0..1: radios que crecen del centro y
    anillos que aparecen hacia fuera. Devuelve {(x, y): (color, alfa)}."""
    import random
    rnd = random.Random(seed)
    cx = sum(p[0] for p in poly) / len(poly); cy = sum(p[1] for p in poly) / len(poly)
    # anclajes: las esquinas y puntos intermedios en los lados largos (un radio cada ~14 px de contorno)
    anchors = []
    for i, a in enumerate(poly):
        b = poly[(i + 1) % len(poly)]
        m = max(1, round(math.dist(a, b) / 14))
        for k in range(m): anchors.append((a[0] + (b[0] - a[0]) * k / m, a[1] + (b[1] - a[1]) * k / m))
    R = max(math.dist((cx, cy), p) for p in anchors)
    rings = max(2, int(R / RING_PX))
    px = {}
    def put(x, y, col, al=255):
        if (x, y) not in px or px[(x, y)][1] < al: px[(x, y)] = (col, al)
    # radios
    for a in anchors:
        for (x, y) in _line(cx, cy, cx + (a[0] - cx) * grow, cy + (a[1] - cy) * grow):
            put(x, y, SILK[2], 235)
    # anillos: entre cada par de radios, un tramo que se comba un poco hacia el centro
    for k in range(1, rings + 1):
        f = k / (rings + .35)
        if grow < f: continue
        for i, a in enumerate(anchors):
            b = anchors[(i + 1) % len(anchors)]
            p0 = (cx + (a[0] - cx) * f, cy + (a[1] - cy) * f); p1 = (cx + (b[0] - cx) * f, cy + (b[1] - cy) * f)
            mid = ((p0[0] + p1[0]) / 2, (p0[1] + p1[1]) / 2)
            sag = .1 * min(1.0, math.dist(p0, p1) / 10)
            mid = (mid[0] + (cx - mid[0]) * sag, mid[1] + (cy - mid[1]) * sag)
            for (x, y) in _line(*p0, *mid) + _line(*mid, *p1):
                put(x, y, SILK[1], 190)
            if rnd.random() < .35:                                             # gota de rocío en el cruce
                put(round(p0[0]), round(p0[1]), DEW[1] if rnd.random() < .5 else DEW[0], 255)
    # contorno: los hilos que unen los extremos, más gruesos y claros
    if grow >= 1:
        for i, a in enumerate(poly):
            b = poly[(i + 1) % len(poly)]
            for (x, y) in _line(*a, *b): put(x, y, SILK[3], 255)
    put(round(cx), round(cy), SILK[3], 255)
    return px

def web_frame(poly, size, grow=1.0, fray=0.0, seed=3, twinkle=0):
    """Un cuadro de la red: fray 0..1 la deshilacha (se pierden tramos y se apaga)."""
    import random
    rnd = random.Random(seed + 17)
    cv = Canvas(*size)
    for (x, y), (col, al) in web_pixels(poly, grow, seed).items():
        if fray and rnd.random() < fray: continue
        if col in DEW and twinkle and (x + y + twinkle) % 3 == 0: col = '#ffffff'
        cv.put(x, y, C(col, int(al * (1 - .55 * fray))))
    return cv

def web_preview():
    """Tres formas de red (triángulo, cuadrilátero y hexágono) que se tejen, brillan y se deshilachan."""
    shapes = [[(10, 52), (48, 6), (82, 46)],
              [(8, 18), (70, 8), (86, 50), (20, 58)],
              [(20, 10), (62, 6), (86, 30), (74, 58), (26, 60), (6, 34)]]
    seq = [dict(grow=.25), dict(grow=.55), dict(grow=.85), dict(grow=1), dict(twinkle=1), dict(twinkle=2),
           dict(fray=.3), dict(fray=.6), dict(fray=.85)]
    anims = {}
    for i, poly in enumerate(shapes):
        anims[f'Red_{i + 1}'] = [web_frame(poly, (92, 66), **f) for f in seq]
    return anims

# =================================================================================================
# Vistas previas: el salto en escena (arco + sombra) sobre el suelo del pantano
# =================================================================================================

def _shadow_frames():
    path = os.path.join(HERE, '..', 'floor3-enemies', 'Effects', 'Fx_FlyerShadow.png')
    if not os.path.exists(path):
        return None
    return Image.open(path).convert('RGBA')

def scene_gif(path, frames, steps=14, scale=4, distance=64):
    """El salto entero: la sombra recorre el suelo y el cuerpo sube en arco. distance en px (64 = 4 u)."""
    shadow = _shadow_frames()
    W, H = distance + 48, 72
    ground = 58
    out = []
    for i in range(steps + 1):
        t = i / steps
        k = max(j for j, p in enumerate(PHASES) if p <= t + 1e-6)
        sprite = as_img(frames[k])
        x = 8 + distance * t
        lift = ARC_PX * math.sin(math.pi * t)
        im = Image.new('RGBA', (W, H), C(B.TRAMOS['Swamp']))
        if shadow is not None:
            sc = 1.0 - 0.45 * (lift / ARC_PX)
            sw = max(4, round(shadow.width * sc)); sh = max(2, round(shadow.height * sc))
            s = shadow.resize((sw, sh), Image.NEAREST)
            im.alpha_composite(s, (round(x + 16 - sw / 2), round(ground - sh / 2)))
        im.alpha_composite(sprite, (round(x) - 4, round(ground - 32 - lift)))
        out.append(im.resize((W * scale, H * scale), Image.NEAREST))
    out[0].save(path, save_all=True, append_images=out[1:], duration=[90] * len(out), loop=0, disposal=2)
    return out

def marker_scene(path, M, scale=4):
    """La rana cargando el salto con el marcador alejándose y las 4 esquinas del radio del impacto (2,2 u)."""
    frog = [as_img(f) for f in frog_jump_anims()['Player_Frog_JumpCharge_side']]
    W, H = 200, 90
    out = []
    for i in range(24):
        t = i / 23
        full = t > .8
        im = Image.new('RGBA', (W, H), C(B.TRAMOS['Swamp']))
        im.alpha_composite(frog[i % 4], (4, 30))
        tx = 24 + 48 + 80 * min(1, t / .8)                     # de 3 u a 8 u
        ty = 62
        spot = as_img(M['Fx_JumpMarker_Full' if full else 'Fx_JumpMarker_Spot'][i % 6])
        im.alpha_composite(spot, (round(tx - 12), ty - 8))
        rx, ry = 2.2 * 16 * .72, 2.2 * 16 * .72 * .55
        for k, (sx, sy) in enumerate(((-1, -1), (1, -1), (1, 1), (-1, 1))):
            c = as_img(M['Fx_JumpMarker_Corner'][k])
            im.alpha_composite(c, (round(tx + sx * rx - 4), round(ty + sy * ry - 4)))
        out.append(im.resize((W * scale, H * scale), Image.NEAREST))
    out[0].save(path, save_all=True, append_images=out[1:], duration=[120] * len(out), loop=0, disposal=2)

def strip(path, anims, scale=4):
    preview(path, anims, BG, scale=scale)

if __name__ == '__main__':
    which = [a for a in sys.argv[1:] if a in ('frog', 'diplo', 'whip')]
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    A = {}
    if 'frog' in which:
        A.update(frog_jump_anims())
    if 'diplo' in which:
        A.update(diplo_jump_anims())
    if 'whip' in which:
        A.update(whip_jump_anims())
    if 'marker' in sys.argv[1:]:
        M = marker_anims()
        for name, frames in M.items():
            save_sheet(os.path.join(OUT, 'Effects', name + '.png'), frames)
            gif(os.path.join(OUT, 'gif', name + '.gif'), frames, 110, BG)
        preview(os.path.join(OUT, 'vista_previa_marcador.png'), M, BG, scale=6)
        marker_scene(os.path.join(OUT, 'gif', 'escena_marcador.gif'), M)
    if 'web' in sys.argv[1:]:
        W = web_preview()
        preview(os.path.join(OUT, 'vista_previa_red.png'), W, B.TRAMOS['Swamp'], scale=3)
        for name, frames in W.items():
            gif(os.path.join(OUT, 'gif', name + '.gif'), frames, 120, B.TRAMOS['Swamp'])
    for name, frames in A.items():
        body = name.split('_')[1]
        save_sheet(os.path.join(BODIES, body, name + '.png'), frames)
        gif(os.path.join(OUT, 'gif', name + '.gif'), frames, 200 if 'Charge' in name else 90, BG)
    if A:
        strip(os.path.join(OUT, 'vista_previa_salto.png'), A)
    for name, frames in A.items():
        if 'Charge' not in name:
            scene_gif(os.path.join(OUT, 'gif', f'escena_{name}.gif'), frames)
    print('Listo:', OUT)
