"""
generar_evolucion_linajes.py - Primordia: la evolución al completar un linaje (rama feature/lineage-evolution).

  Limbs/      Limbs_<Linaje>_<Idle|Move>_<side|down|up>   extremidades que sustituyen a las de la mutación
              mientras el linaje está activo (32x32, centradas en la célula, 4 cuadros con el balanceo)
                Shark (aleta dorsal y pectorales), Squid (tentáculos), Turtle (patas escamosas),
                Jellyfish (tentáculos translúcidos), Scorpion (patas con pinzas), Mold (pelusa de moho)
  Mouth/      Mouth_<Linaje>_<Idle|Attack>                 bocas que giran con el apuntado (24x24, unión en 0,12)
                Turtle (pico córneo), Scorpion (cola curvada; el proyectil sale de su punta)
  Cap/        Cap_Puffball_<Idle|Move>_<side|down|up>      gorrito de cuesco del Cuesco de lobo (24x24)
  Evolution/  Evolution_<Linaje>                           la célula brilla, se transforma en la silueta del
              animal y vuelve (48x48, 12 cuadros, unos 2 s)
  Effects/    LineageLost                                  pof blanco al perder el linaje (el juego lo tiñe)

Reutiliza generar_linajes.py y, a través de él, la célula, las paletas y el contorno de los demás generadores.

Uso:
  python generar_evolucion_linajes.py [carpeta_multimedia_temp] [carpeta_salida]
"""
import importlib.util
import math
import os
import random
import sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, '..')
OUT = sys.argv[2] if len(sys.argv) > 2 else HERE

_argv = sys.argv
sys.argv = ['generar_linajes', ROOT, os.devnull]
_spec = importlib.util.spec_from_file_location('generar_linajes', os.path.join(ROOT, 'lineages', 'generar_linajes.py'))
L = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(L)
sys.argv = _argv
M, B, F1 = L.M, L.B, L.F1

C, OUTL = M.C, M.OUTL
Canvas, thick, blob = M.Canvas, M.thick, M.blob
shade_v, ramp_idx, geom = M.shade_v, M.ramp_idx, M.geom
FACINGS, FRAMES, LN, AY = M.FACINGS, M.FRAMES, M.LN, M.AY
BG = C('#16352e')

SHARK = ['#2c3e58', '#4a6688', '#7e9cbc', '#c8dcef']
SQUID = ['#6a2240', '#a8446a', '#e07a98', '#ffc0d0']
TURTLE = ['#2e4a1e', '#4e7a2c', '#7eae44', '#c0e07a']
HORN = ['#5a3a1a', '#9a6a2a', '#d0a050', '#f6dc98']
JELLY = ['#7a3a9a', '#b06ad0', '#e0a8f0', '#fbe6ff']
SCORP = ['#3a2410', '#6e4618', '#a8702a', '#e6b060']
MOLD = ['#5a6a4a', '#9aaa7a', '#d4e0b4', '#f4fbe6']
PUFF = ['#8a7a5a', '#cfc2a0', '#f2ead6', '#ffffff']

CLASS_RAMP = {
    'carn': ['#5a1a1a', '#a83232', '#e0605a', '#ffb0a0'],
    'herb': ['#1e4a1e', '#3c8d2f', '#7ed060', '#d0ffb0'],
    'ven': ['#3a1060', '#8a3fb8', '#c99af0', '#f4e0ff'],
    'hongo': ['#5a3a10', '#a8701c', '#e9b45c', '#fff0c0'],
}


# ================================================================ ayudantes
def CA(c, a=255):
    """Color con transparencia, desde hexadecimal o desde una tupla."""
    return (C(c) if isinstance(c, str) else tuple(c))[:3] + (a,)


def fill_poly(cv, pts, ramp, alpha=255):
    """Polígono relleno con la luz de arriba a la izquierda (según su caja)."""
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    w, h = max(1, (x1 - x0) / 2), max(1, (y1 - y0) / 2)
    for y in range(int(y0) - 1, int(y1) + 2):
        for x in range(int(x0) - 1, int(x1) + 2):
            px, py = x + .5, y + .5
            inside = False
            j = len(pts) - 1
            for i in range(len(pts)):
                (xi, yi), (xj, yj) = pts[i], pts[j]
                if (yi > py) != (yj > py) and px < (xj - xi) * (py - yi) / (yj - yi) + xi:
                    inside = not inside
                j = i
            if inside:
                cv.put(x, y, CA(ramp[ramp_idx(shade_v((px - cx) / w, (py - cy) / h))], alpha))


def stroke(cv, pts, w0, w1, ramp, alpha=255, light_up=True):
    """Trazo que se afina de w0 a w1, más claro por arriba."""
    ws = [w0 + (w1 - w0) * i / max(1, len(pts) - 2) for i in range(len(pts) - 1)]

    def col(xx, yy, x, y, r):
        top = (yy + .5) < y - r * 0.2
        return CA(ramp[2 if top and light_up else 1], alpha)
    thick(cv, pts, ws, col)


def wave_line(root, ang, length, phase, amp, steps=8, freq=4.0):
    """Puntos de una línea que ondula."""
    x, y = root
    pts = [(x, y)]
    for s in range(1, steps + 1):
        t = s / steps
        a = ang + amp * math.sin(phase + t * freq)
        x += math.cos(a) * length / steps
        y += math.sin(a) * length / steps
        pts.append((x, y))
    return pts


def sway_of(anim, f):
    return [1.2, 0.0, -1.2, 0.0][f] if anim == 'Move' else [0.0, 0.5, 1.0, 0.5][f]


# ============================================================ 1. EXTREMIDADES
def shark_fins(facing, p, anim, f):
    cv = Canvas(LN)
    cx, cy, rx, ry = M.cell_geom(p)
    top = cy - ry
    s = sway_of(anim, f)
    lean = p.get('lean', 0.0)
    if facing == 'side':
        bx = cx - 1 + lean * 0.8
        fill_poly(cv, [(bx - 4, top + 3), (bx + 3, top + 3), (bx - 3 - s, top - 6)], SHARK)
        fill_poly(cv, [(cx + 1, cy + ry - 2), (cx + 5, cy + ry - 2), (cx - 1 - s * 0.5, cy + ry + 3)], SHARK)
    else:
        fill_poly(cv, [(cx - 1.8, top + 3), (cx + 1.8, top + 3), (cx + s * 0.4, top - 6)], SHARK)
        for side in (-1, 1):
            ex = cx + side * (rx - 1)
            fill_poly(cv, [(ex, cy), (ex, cy + 3), (ex + side * (5 + s * 0.3), cy + 5)], SHARK)
    cv.outline()
    return cv


def squid_tentacles(facing, p, anim, f):
    cv = Canvas(LN)
    cx, cy, rx, ry = M.cell_geom(p)
    moving = anim == 'Move'
    ph = f * math.pi / 2 * (1.5 if moving else 1)
    amp = 0.45 if moving else 0.3
    roots = []
    if facing == 'side':
        for k in range(5):
            roots.append(((cx - rx * 0.5, cy - 2 + k * 1.3), math.pi - 0.35 + k * 0.17, 9 + (k % 2) * 2))
    else:
        for k in range(6):
            roots.append(((cx - 5 + k * 2, cy + ry - 2), math.pi / 2 + (k - 2.5) * 0.18, 6.5 + (k % 2) * 1.5))
    for k, (root, ang, length) in enumerate(roots):
        pts = wave_line(root, ang, length + (1.2 if moving else 0), ph + k * 0.9, amp)
        stroke(cv, pts, 2.2, 1.0, SQUID)
        for i in (3, 5):
            if i < len(pts):
                cv.put(pts[i][0], pts[i][1] + 0.6, SQUID[3])     # ventosas
    cv.outline()
    return cv


def turtle_legs(facing, p, anim, f):
    cv = Canvas(LN)
    cx, cy, rx, ry = M.cell_geom(p)
    st = [1.5, 0, -1.5, 0][f] if anim == 'Move' else 0

    def leg(hip, foot, far=False):
        ramp = TURTLE if not far else [TURTLE[0], TURTLE[0], TURTLE[1], TURTLE[2]]
        stroke(cv, [hip, ((hip[0] + foot[0]) / 2, (hip[1] + foot[1]) / 2), foot], 3.4, 2.6, ramp)
        cv.put(foot[0] + 0.5, foot[1] + 0.8, HORN[3])              # uña
        cv.put((hip[0] + foot[0]) / 2, (hip[1] + foot[1]) / 2, ramp[0])   # escama

    if facing == 'side':
        for far in (True, False):
            o = 1.5 if far else 0
            leg((cx + 3 + o, cy + ry - 3), (cx + 5 + o + (st if not far else -st), cy + ry + 3), far)
            leg((cx - 4 + o, cy + ry - 3), (cx - 6 + o + (-st if not far else st), cy + ry + 3), far)
    else:
        for side in (-1, 1):
            leg((cx + side * (rx - 2), cy - 1), (cx + side * (rx + 3), cy - 3 + side * st * 0.5))
            leg((cx + side * (rx - 3), cy + ry - 3), (cx + side * (rx + 1), cy + ry + 2 - side * st * 0.5))
    cv.outline()
    return cv


def jelly_tentacles(facing, p, anim, f):
    cv = Canvas(LN)
    cx, cy, rx, ry = M.cell_geom(p)
    moving = anim == 'Move'
    ph = f * math.pi / 2
    tilt = -0.35 if facing == 'side' else 0.0
    for k in range(7):
        root = (cx - 5 + k * 1.7, cy + ry - 2)
        length = 8 + (k % 3) * 2 + (1.5 if moving else 0)
        pts = wave_line(root, math.pi / 2 + tilt + (k - 3) * 0.06, length, ph + k * 1.1, 0.4 if moving else 0.28, steps=10)
        for i, (x, y) in enumerate(pts):
            cv.put(x, y, CA(JELLY[1] if i < 5 else JELLY[2], 220))
            if i % 4 == 3:
                cv.put(x + 1, y, CA(JELLY[3], 230))
    M.translucent_outline(cv, 110)
    return cv


def scorpion_legs(facing, p, anim, f):
    cv = Canvas(LN)
    cx, cy, rx, ry = M.cell_geom(p)
    st = [1.5, 0, -1.5, 0][f] if anim == 'Move' else 0

    def leg(hip, foot, lift, far=False):
        ramp = SCORP if not far else [SCORP[0], SCORP[0], SCORP[1], SCORP[2]]
        knee = ((hip[0] + foot[0]) / 2, min(hip[1], foot[1]) - 2.5 - lift)
        stroke(cv, [hip, knee, (foot[0], foot[1] - lift)], 1.6, 1.0, ramp)
        cv.put(knee[0], knee[1], ramp[3])

    def claw(shoulder, wrist, tip_dir, far=False):
        ramp = SCORP if not far else [SCORP[0], SCORP[0], SCORP[1], SCORP[2]]
        elbow = ((shoulder[0] + wrist[0]) / 2, (shoulder[1] + wrist[1]) / 2 - 1.5)
        stroke(cv, [shoulder, elbow, wrist], 2.0, 1.6, ramp)
        blob(cv, wrist[0], wrist[1], 2.0, 1.6, ramp)
        ax, ay = tip_dir
        for side in (-1, 1):
            nx, ny = -ay * side, ax * side
            pts = [(wrist[0] + nx * 1.0, wrist[1] + ny * 1.0),
                   (wrist[0] + ax * 2.5 + nx * 1.6, wrist[1] + ay * 2.5 + ny * 1.6),
                   (wrist[0] + ax * 4.2 + nx * 0.4, wrist[1] + ay * 4.2 + ny * 0.4)]
            stroke(cv, pts, 1.4, 0.8, ramp)

    if facing == 'side':
        for far in (True, False):
            o = 1.2 if far else 0
            for k, hx in enumerate((-4.0, -1.0, 2.0)):
                s = st if (k % 2 == 0) != far else -st
                leg((cx + hx + o, cy + ry - 3), (cx + hx * 1.8 + o + s, cy + ry + 3), 0, far)
            claw((cx + rx - 2 + o * 0.5, cy + 1), (cx + rx + 3 + o, cy + 1 - o * 0.5), (1, 0), far)
    else:
        sgn = 1 if facing == 'down' else -1
        for side in (-1, 1):
            for k, a in enumerate((-0.4, 0.2, 0.8)):
                s = st if (k % 2 == 0) == (side > 0) else -st
                hip = (cx + side * (rx - 2) * math.cos(a), cy + (ry - 2) * math.sin(a))
                leg(hip, (cx + side * 12.5 * math.cos(a), cy + 10 * math.sin(a) + 2 + s), 0)
            claw((cx + side * (rx - 2), cy + sgn * 1), (cx + side * (rx + 3), cy + sgn * (ry + 1)), (0, sgn))
    cv.outline()
    return cv


def mold_fuzz(facing, p, anim, f):
    cv = Canvas(LN)
    cx, cy, rx, ry = M.cell_geom(p)
    rnd = random.Random(11)
    for k in range(26):
        a = k * 2 * math.pi / 26 + rnd.uniform(-0.1, 0.1)
        root = (cx + math.cos(a) * (rx - 0.5), cy + math.sin(a) * (ry - 0.5))
        L_ = 2.2 + rnd.uniform(0, 1.6) + (0.6 if anim == 'Move' else 0)
        pts = wave_line(root, a, L_, f * math.pi / 2 + k, 0.4, steps=4)
        for i, (x, y) in enumerate(pts):
            cv.put(x, y, CA(MOLD[2] if i < 3 else MOLD[3], 210))
        if k % 5 == 0:
            cv.put(pts[-1][0], pts[-1][1], CA(MOLD[1], 230))      # esporangio
    M.translucent_outline(cv, 120)
    return cv


LIMBS = {'Shark': shark_fins, 'Squid': squid_tentacles, 'Turtle': turtle_legs,
         'Jellyfish': jelly_tentacles, 'Scorpion': scorpion_legs, 'Mold': mold_fuzz}


# ================================================================ 2. BOCAS
def rotate_about(pts, origin, ang):
    ox, oy = origin
    c, s = math.cos(ang), math.sin(ang)
    return [(ox + (x - ox) * c - (y - oy) * s, oy + (x - ox) * s + (y - oy) * c) for x, y in pts]


def turtle_beak(open_a=0.0, snap=False, reach=1.0):
    cv = Canvas(24)
    hinge = (1.5, AY)
    upper = rotate_about([(1, AY - 2.5), (4.5 * reach, AY - 3.2), (7.5 * reach, AY - 2), (9.5 * reach, AY + 0.6)], hinge, -open_a)
    lower = rotate_about([(1, AY + 2), (4.5 * reach, AY + 2.4), (7 * reach, AY + 1.2)], hinge, open_a)
    stroke(cv, upper, 4.4, 1.4, HORN)
    stroke(cv, lower, 3.4, 1.4, HORN, light_up=False)
    blob(cv, 2.2, AY, 2.6, 4.0, TURTLE)
    if snap:
        tip = upper[-1]
        for d in ((1, 0), (0, -1), (0, 1)):
            cv.put(tip[0] + d[0] + 1, tip[1] + d[1], '#ffffff')
    cv.outline()
    return cv


def scorpion_tail(curl=0.0, reach=1.0, flash=False):
    """Cola de segmentos que se arquea hacia arriba y acaba en el aguijón, apuntando hacia delante."""
    cv = Canvas(24)
    n = 6
    pts = []
    for i in range(n):
        t = i / (n - 1)
        th = t * (math.pi * (0.75 + curl))
        R = 7.5 * reach
        x = 1 + R * math.sin(th) * 1.25 + t * 4 * reach
        y = AY - R * (1 - math.cos(th)) * 0.55 + t * 1.5
        pts.append((x, y))
    for i, (x, y) in enumerate(pts):
        r = 2.4 - i * 0.22
        blob(cv, x, y, r, r * 0.9, SCORP)
        cv.put_in(x - r * 0.5, y - r * 0.5, SCORP[3])
    tx, ty = pts[-1]
    blob(cv, tx + 1.2, ty + 1.0, 2.0, 1.8, SCORP)                  # glándula
    tip = (tx + 4.0, ty + 3.2)
    stroke(cv, [(tx + 2, ty + 1.8), (tx + 3.2, ty + 2.7), tip], 1.3, 0.6, ['#2a1a0a', '#ffe0a0', '#ffffff', '#ffffff'])
    if flash:
        for d in ((1, 0), (0, 1), (1, 1)):
            cv.put(tip[0] + d[0], tip[1] + d[1], '#ffffff')
    cv.outline()
    cv.tip = tip
    return cv


def mouths():
    beak = {
        'Idle': [turtle_beak(0.0), turtle_beak(0.05), turtle_beak(0.1), turtle_beak(0.05)],
        'Attack': [turtle_beak(0.55, reach=0.95), turtle_beak(0.0, snap=True, reach=1.1), turtle_beak(0.08)],
    }
    tail = {
        'Idle': [scorpion_tail(0.0), scorpion_tail(0.04), scorpion_tail(0.08), scorpion_tail(0.04)],
        'Attack': [scorpion_tail(0.2, 0.9), scorpion_tail(-0.25, 1.15, flash=True), scorpion_tail(0.05)],
    }
    return {'Turtle': beak, 'Scorpion': tail}


# ============================================================ 3. GORRITO DE CUESCO
SPECKS = [(-0.45, -0.4), (0.3, -0.6), (0.55, -0.1), (-0.15, -0.15), (-0.7, 0.05), (0.15, 0.2)]


def puffball(facing, p, n=24):
    cv = Canvas(n)
    cx, cy, rx, ry = geom(n, 6.5, 6.0, p['squash'], p['bob'], p.get('grow', 0))
    s = p['squash']
    w, h = 5.4 * (1 + 0.05 * s), 4.6 * (1 - 0.07 * s)
    bx = cx + p.get('lean', 0.0) * 0.8 + (0.5 if facing == 'side' else 0)
    by = cy - ry + 1.6 - h * 0.7                                   # centro de la bola
    for y in range(n):
        for x in range(n):
            nx, ny = (x + .5 - bx) / w, (y + .5 - by) / h
            if nx * nx + ny * ny <= 1:
                cv.px[(x, y)] = C(PUFF[ramp_idx(shade_v(nx, ny))])
    for (dx, dy) in SPECKS:
        cv.put_in(bx + dx * w - .5, by + dy * h - .5, PUFF[0])
    if facing != 'up':
        cv.put_in(bx - .5 + (1 if facing == 'side' else 0), by - h * 0.85, '#5a4a30')   # poro de arriba
    cv.outline()
    return cv


def cap_anims():
    out = {}
    for anim in ('Idle', 'Move'):
        for facing in FACINGS:
            out[f'{anim}_{facing}'] = [puffball(facing, p) for p in FRAMES[f'{anim}_{facing}']]
    return out


# ======================================================= 4. EVOLUCIÓN
E = 48
CX, CY = 24, 25


def ell(cx, cy, rx, ry):
    return ('ell', cx, cy, rx, ry)


def poly(*pts):
    return ('poly', pts)


def line(pts, w0, w1):
    return ('line', pts, w0, w1)


def silhouette_shapes(name):
    if name == 'Shark':
        return [ell(25, 27, 15, 6), poly((37, 24), (44, 27), (37, 30)), poly((19, 22), (28, 22), (20, 11)),
                poly((11, 25), (4, 15), (8, 27)), poly((11, 28), (5, 35), (8, 27)), poly((24, 31), (30, 31), (21, 38))]
    if name == 'Squid':
        shapes = [ell(24, 15, 6, 10), poly((18, 8), (24, 2), (30, 8), (24, 12)), ell(24, 27, 5.5, 3.5)]
        for k in range(6):
            x = 19.5 + k * 1.8
            shapes.append(line(wave_line((x, 29), math.pi / 2 + (k - 2.5) * 0.12, 13, k, 0.25), 2.2, 1.0))
        shapes.append(line(wave_line((22, 29), math.pi / 2 - 0.35, 17, 1.5, 0.3), 1.6, 1.0))
        shapes.append(line(wave_line((26, 29), math.pi / 2 + 0.35, 17, 3.0, 0.3), 1.6, 1.0))
        return shapes
    if name == 'Turtle':
        return [('half', 23, 28, 14, 11), ell(23, 29, 14, 3), ell(39, 26, 4.5, 3.2), line([(34, 28), (37, 27)], 3.2, 3.0),
                ell(14, 33, 3.2, 3.6), ell(31, 33, 3.2, 3.6), poly((9, 28), (5, 30), (9, 30))]
    if name == 'Jellyfish':
        shapes = [('half', 24, 21, 12, 12), ell(24, 21, 12, 2)]
        for k in range(7):
            shapes.append(line(wave_line((14.5 + k * 3.2, 22), math.pi / 2, 20 - abs(k - 3) * 1.5, k * 0.9, 0.3, steps=10), 1.4, 0.8))
        return shapes
    if name == 'Scorpion':
        tail = [(13, 30), (9, 24), (9, 17), (13, 12), (19, 10), (23, 12)]
        shapes = [ell(23, 31, 10, 4.2), line(tail, 4.0, 2.6), ell(24, 13.5, 2.4, 2.2), poly((25, 14), (28, 17), (25, 16)),
                  line([(31, 30), (35, 27), (39, 27)], 2.4, 2.2), ell(40.5, 27, 2.6, 2.2),
                  line([(41, 26), (44, 24), (45, 25)], 1.6, 1.0), line([(41, 28), (44, 29), (45, 28)], 1.6, 1.0)]
        for k, hx in enumerate((17, 22, 27)):
            shapes.append(line([(hx, 33), (hx - 2 + k, 36), (hx - 4 + k * 2, 40)], 1.6, 1.0))
        return shapes
    if name == 'Mold':
        shapes = [('half', 24, 38, 15, 10)]
        for k, (x, top) in enumerate(((14, 18), (19, 13), (24, 10), (29, 14), (34, 19))):
            shapes.append(line([(x, 32), (x + (k - 2) * 0.6, top + 3)], 1.4, 1.0))
            shapes.append(ell(x + (k - 2) * 0.6, top, 2.8, 2.6))
        return shapes
    if name == 'Puffball':
        return [ell(24, 27, 13, 11), ell(24, 37, 8, 3.2), ell(24, 9, 2.5, 2), ell(19, 6, 2, 1.6), ell(29, 7, 2.2, 1.8)]
    return []


def mask_of(shapes, scale=1.0):
    """Píxeles que ocupa la silueta, escalada desde el centro del lienzo."""
    cv = Canvas(E)

    def tr(x, y):
        return CX + (x - CX) * scale, CY + (y - CY) * scale

    for sh in shapes:
        kind = sh[0]
        if kind in ('ell', 'half'):
            _, x, y, rx, ry = sh
            x, y = tr(x, y)
            rx, ry = rx * scale, ry * scale
            for yy in range(E):
                for xx in range(E):
                    nx, ny = (xx + .5 - x) / rx, (yy + .5 - y) / ry
                    if nx * nx + ny * ny <= 1 and (kind == 'ell' or ny <= 0):
                        cv.px[(xx, yy)] = OUTL
        elif kind == 'poly':
            fill_poly(cv, [tr(*q) for q in sh[1]], [OUTL] * 4)
        elif kind == 'line':
            pts = [tr(*q) for q in sh[1]]
            ws = [max(1.0, (sh[2] + (sh[3] - sh[2]) * i / max(1, len(pts) - 2)) * scale) for i in range(len(pts) - 1)]
            thick(cv, pts, ws, lambda *a: OUTL)
    return set(cv.px)


def paint(mask, ramp, white=0.0, glow=None, alpha=255):
    """Rellena la silueta con la rampa de su clase (o blanca), un borde claro y un halo."""
    cv = Canvas(E)
    if not mask:
        return cv
    xs = [p[0] for p in mask]
    ys = [p[1] for p in mask]
    cx, cy = (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2
    w, h = max(1, (max(xs) - min(xs)) / 2), max(1, (max(ys) - min(ys)) / 2)
    for (x, y) in mask:
        edge = any((x + dx, y + dy) not in mask for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
        col = C(ramp[3]) if edge else C(ramp[ramp_idx(shade_v((x + .5 - cx) / w, (y + .5 - cy) / h))])
        if white > 0:
            col = tuple(int(c + (255 - c) * white) for c in col[:3]) + (255,)
        cv.px[(x, y)] = col[:3] + (alpha,)
    cv.outline()
    if glow:
        halo = set()
        for (x, y) in list(cv.px):
            for dx in (-2, -1, 0, 1, 2):
                for dy in (-2, -1, 0, 1, 2):
                    q = (x + dx, y + dy)
                    if q not in cv.px and 0 <= q[0] < E and 0 <= q[1] < E and abs(dx) + abs(dy) <= 2:
                        halo.add((q, abs(dx) + abs(dy)))
        for (q, d) in halo:
            if q not in cv.px:
                cv.px[q] = CA(glow, 150 if d == 1 else 70)
    return cv


def sparkles(cv, t, color, count=8, radius=20):
    for k in range(count):
        a = k * 2 * math.pi / count + t * 1.4
        x, y = CX + math.cos(a) * radius, CY + math.sin(a) * radius * 0.9
        for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
            if (dx == 0 and dy == 0) or (k + int(t * 3)) % 2 == 0:
                cv.put(x + dx, y + dy, CA(color, 255 if dx == dy == 0 else 170))


def cell_image():
    im = Image.open(os.path.join(ROOT, 'player', 'Player_Idle_down.png')).convert('RGBA').crop((0, 0, 16, 16))
    out = Image.new('RGBA', (E, E), (0, 0, 0, 0))
    # Centrada en el lienzo, como el pivote del sprite del jugador.
    out.alpha_composite(im, (E // 2 - 8, E // 2 - 8))
    return out


def whiten(im, amount):
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a:
                px[x, y] = (int(r + (255 - r) * amount), int(g + (255 - g) * amount), int(b + (255 - b) * amount), a)
    return im


def evolution(name, cls):
    ramp = CLASS_RAMP[cls]
    glow = ramp[2]
    shapes = silhouette_shapes(name)
    cell_mask = mask_of([ell(CX, E / 2 + 0.5, 6.5, 6)])          # donde está la célula del jugador
    blob_big = mask_of([ell(CX, CY, 9, 8.5)])
    frames = []
    frames.append(cell_image())                                            # 0: la célula
    frames.append(whiten(cell_image(), 0.5))                               # 1: empieza a brillar
    f = paint(cell_mask, ramp, white=1.0, glow=glow).img()                 # 2: blanca, con halo
    frames.append(f)
    frames.append(paint(blob_big, ramp, white=1.0, glow=glow).img())       # 3: se hincha
    frames.append(paint(mask_of(shapes, 0.55) | mask_of([ell(CX, CY, 7, 6.5)]), ramp, white=1.0, glow=glow).img())  # 4: toma forma
    frames.append(paint(mask_of(shapes, 0.85), ramp, white=0.8, glow=glow).img())   # 5: casi el animal
    for k in range(3):                                                     # 6-8: el animal, con destellos
        cv = paint(mask_of(shapes, 1.0), ramp, glow=glow)
        sparkles(cv, k, ramp[3])
        frames.append(cv.img())
    frames.append(paint(mask_of(shapes, 1.0), ramp, white=0.9, glow=glow).img())     # 9: destello
    frames.append(paint(mask_of([ell(CX, CY, 8, 7.5)]), ramp, white=1.0, glow=glow).img())  # 10: vuelve a la célula
    last = cell_image()
    cv = Canvas(E)
    sparkles(cv, 3, ramp[3], count=10, radius=13)
    last.alpha_composite(cv.img())
    frames.append(last)                                                    # 11: la célula, con chispas
    return frames


# Milisegundos de cada cuadro: el animal se queda más tiempo (unos 2 s en total).
EVOLUTION_MS = [100, 110, 120, 120, 130, 150, 260, 260, 260, 150, 120, 200]
LINEAGE_CLASS = {'Shark': 'carn', 'Squid': 'carn', 'Turtle': 'herb', 'Jellyfish': 'ven', 'Scorpion': 'ven',
                 'Mold': 'hongo', 'Puffball': 'hongo'}


def lost_puff(f):
    """Pof blanco (el juego lo tiñe del color de la clase): bolitas que se abren y se apagan (24x24)."""
    cv = M.Canvas(24)
    r = [3, 6, 8.5, 10][f]
    al = [255, 220, 150, 80][f]
    for k in range(8):
        a = k * math.pi / 4 + 0.3
        x, y = 12 + math.cos(a) * r, 12 + math.sin(a) * r * 0.85
        size = [2.2, 1.8, 1.4, 1.0][f]
        for yy in range(int(y - size) - 1, int(y + size) + 2):
            for xx in range(int(x - size) - 1, int(x + size) + 2):
                if (xx + .5 - x) ** 2 + (yy + .5 - y) ** 2 <= size * size:
                    cv.put(xx, yy, CA('#ffffff', al))
    if f == 0:
        blob(cv, 12, 12, 3.5, 3.2, ['#ffffff'] * 4)
    return cv


# ================================================================ guardar
SHEETS = {}


def save(group, name, frames, ms=None):
    frames = [f if isinstance(f, Image.Image) else f.img() for f in frames]
    w, h = frames[0].size
    os.makedirs(os.path.join(OUT, group), exist_ok=True)
    sh = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, im in enumerate(frames):
        sh.paste(im, (i * w, 0))
    sh.save(os.path.join(OUT, group, name + '.png'))
    if len(frames) > 1:
        os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
        sc = max(1, min(8, 240 // max(w, h)))
        out = []
        durations = ms if isinstance(ms, list) else [ms or 100] * len(frames)
        for im in frames:
            b = Image.new('RGBA', (w, h), BG)
            b.alpha_composite(im)
            out.append(b.resize((w * sc, h * sc), Image.NEAREST).convert('RGB'))
        out[0].save(os.path.join(OUT, 'gif', name + '.gif'), save_all=True, append_images=out[1:],
                    duration=durations, loop=0)
    SHEETS[name] = frames


def build():
    for lineage, fn in LIMBS.items():
        for anim in ('Idle', 'Move'):
            for facing in FACINGS:
                frames = [fn(facing, p, anim, i) for i, p in enumerate(FRAMES[f'{anim}_{facing}'])]
                save('Limbs', f'Limbs_{lineage}_{anim}_{facing}', frames, M.MS[anim])
    for lineage, anims in mouths().items():
        for anim, frames in anims.items():
            save('Mouth', f'Mouth_{lineage}_{anim}', frames, M.MS[anim])
    tip = mouths()['Scorpion']['Idle'][0].tip
    print(f'Punta de la cola del Escorpión: {tip[0]:.1f} px desde la unión')
    for name, frames in cap_anims().items():
        save('Cap', f'Cap_Puffball_{name}', frames, M.MS[name.split('_')[0]])
    for lineage, cls in LINEAGE_CLASS.items():
        save('Evolution', f'Evolution_{lineage}', evolution(lineage, cls), EVOLUTION_MS)
    save('Effects', 'LineageLost', [lost_puff(f) for f in range(4)], 70)


# ============================================================ vista previa
def player_frame(facing, anim='Idle', i=0):
    im = Image.open(os.path.join(ROOT, 'player', f'Player_{anim}_{facing}.png')).convert('RGBA')
    return im.crop((i * 16, 0, i * 16 + 16, 16))


def preview(path):
    S = 4
    cw = 36
    rows = []
    # 1. extremidades: célula + pieza, reposo en 3 direcciones y moverse de lado
    for lineage in LIMBS:
        row = Image.new('RGBA', (cw * 10, cw), BG)
        k = 0
        for facing in FACINGS:
            limbs = SHEETS[f'Limbs_{lineage}_Idle_{facing}'][0]
            row.alpha_composite(limbs, (k * cw + 2, 2))
            row.alpha_composite(player_frame(facing), (k * cw + 10, 10))
            k += 1
        for i in range(4):
            limbs = SHEETS[f'Limbs_{lineage}_Move_side'][i]
            row.alpha_composite(limbs, (k * cw + 2, 2))
            row.alpha_composite(player_frame('side', 'Move', i), (k * cw + 10, 10))
            k += 1
        rows.append((f'{lineage}: extremidades', row))
    # 2. bocas sobre la célula (reposo y ataque)
    for lineage in ('Turtle', 'Scorpion'):
        row = Image.new('RGBA', (cw * 10, cw), BG)
        frames = SHEETS[f'Mouth_{lineage}_Idle'][:2] + SHEETS[f'Mouth_{lineage}_Attack']
        for k, m in enumerate(frames):
            row.alpha_composite(player_frame('side'), (k * cw + 4, 10))
            row.alpha_composite(m, (k * cw + 4 + 14, 6))
        rows.append((f'{lineage}: boca (reposo y ataque)', row))
    # 3. gorrito de cuesco
    row = Image.new('RGBA', (cw * 10, cw), BG)
    for k, facing in enumerate(FACINGS):
        row.alpha_composite(player_frame(facing), (k * cw + 10, 10))
        row.alpha_composite(SHEETS[f'Cap_Puffball_Idle_{facing}'][0], (k * cw + 6, 6))
    for i in range(4):
        row.alpha_composite(player_frame('side', 'Move', i), ((i + 3) * cw + 10, 10))
        row.alpha_composite(SHEETS['Cap_Puffball_Move_side'][i], ((i + 3) * cw + 6, 6))
    rows.append(('Cuesco de lobo: gorrito', row))
    # 4. evoluciones: 12 cuadros de cada una
    for lineage in LINEAGE_CLASS:
        row = Image.new('RGBA', (E * 12, E), BG)
        for k, fr in enumerate(SHEETS[f'Evolution_{lineage}']):
            row.alpha_composite(fr, (k * E, 0))
        rows.append((f'{lineage}: evolución', row))
    lab = 22
    W = max(r.width for _, r in rows) * S + 20
    H = sum(r.height * S + lab + 8 for _, r in rows) + 10
    out = Image.new('RGBA', (W, H), BG)
    y = 8
    for name, r in rows:
        B.E.G.ptext(out, (10, y + 4), name.upper(), (214, 255, 154), scale=2)
        y += lab
        out.alpha_composite(r.resize((r.width * S, r.height * S), Image.NEAREST), (10, y))
        y += r.height * S + 8
    out.convert('RGB').save(path)


def main():
    build()
    preview(os.path.join(OUT, 'vista_previa_evolucion.png'))
    for k, v in SHEETS.items():
        print(k, len(v), f'{v[0].width}x{v[0].height}')


if __name__ == '__main__':
    main()
