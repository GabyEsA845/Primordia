"""
generar_ui.py - Primordia: objetos e interfaz.

Reutiliza generar_jugador.py y generar_mutaciones.py (paletas, contorno #181425,
rampas de color, colores de linaje y las piezas de las mutaciones), así que todo
encaja con la célula y sus mutaciones.

Uso:
  python generar_ui.py [carpeta_multimedia_temp] [carpeta_salida]
Por defecto: la carpeta de arriba (donde están player/, mutations/, environment/
y enemies/) y la carpeta de este script.
"""
import importlib.util
import math
import os
import sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, '..')
OUT = sys.argv[2] if len(sys.argv) > 2 else HERE

# ---------------------------------------------- cargar los otros generadores
_argv = sys.argv
sys.argv = ['generar_mutaciones.py', os.path.join(ROOT, 'player', 'generar_jugador.py'), os.devnull]
_spec = importlib.util.spec_from_file_location('generar_mutaciones', os.path.join(ROOT, 'mutations', 'generar_mutaciones.py'))
M = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(M)
sys.argv = _argv

C, CA, OUTL, Canvas, body, features = M.C, M.CA, M.OUTL, M.Canvas, M.body, M.features
shade_v, ramp_idx = M.shade_v, M.ramp_idx
FLESH, BONE, FIN, INK, ARM, SHOCK, LIME, PURP = M.FLESH, M.BONE, M.FIN, M.INK, M.ARM, M.SHOCK, M.LIME, M.PURP
GREEN, RED = M.GREEN, M.RED

BG = C('#16352e')
# biomasa: verde amarillento
BIO = ['#4e6e1a', '#8eb032', '#c8dc58', '#f4ffb0']
# membrana de la interfaz (verde azulado, como el agua del piso 1)
MEM = ['#173a32', '#2a5e50', '#4a9078', '#8fd0b0']
MEM_HI = ['#2a5e50', '#4a9078', '#9ae0b8', '#e4ffd8']
DIM = ['#1c332d', '#28463e', '#36594f', '#4a7062']   # siluetas apagadas
LINEAGES = {
    'Predator': dict(ramp=FIN, light=BONE[3], inner=FIN[0], color=(242, 128, 89)),
    'Armored': dict(ramp=ARM, light='#e8f4ff', inner=ARM[0], color=(140, 178, 230)),
    'Toxic': dict(ramp=[PURP[1], LIME[1], LIME[2], LIME[3]], light=LIME[3], inner=PURP[2], color=(140, 230, 89)),
}
# Colores por ranura (sustituyen a los linajes antiguos en burbujas, tarjetas e iconos):
# Boca naranja (colmillo), Extremidades morado (aleta), Piel azul (escudo), Órgano lima (gota).
SLOT_COLORS = {
    'Mouth': LINEAGES['Predator'],
    'Limbs': dict(ramp=PURP, light=PURP[3], inner=PURP[0], color=(168, 90, 208)),
    'Skin': LINEAGES['Armored'],
    'Organ': LINEAGES['Toxic'],
}
LINEAGES.update(SLOT_COLORS)


class Cv:
    """Como Canvas de generar_jugador.py, pero rectangular."""

    def __init__(s, w, h=None):
        s.w, s.h = w, h or w
        s.n = max(s.w, s.h)
        s.px = {}

    def inside(s, x, y):
        return 0 <= x < s.w and 0 <= y < s.h

    def put(s, x, y, c):
        x, y = int(round(x)), int(round(y))
        if s.inside(x, y):
            s.px[(x, y)] = C(c) if isinstance(c, str) else c

    def put_in(s, x, y, c):
        if (int(round(x)), int(round(y))) in s.px:
            s.put(x, y, c)

    def outline(s, alpha=255):
        o = OUTL[:3] + (alpha,)
        add = [(x, y) for y in range(s.h) for x in range(s.w) if (x, y) not in s.px and any(
            s.px.get((x + dx, y + dy), OUTL)[:3] != OUTL[:3] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
        for p in add:
            s.px[p] = o

    def img(s):
        im = Image.new('RGBA', (s.w, s.h), (0, 0, 0, 0))
        for p, c in s.px.items():
            im.putpixel(p, c)
        return im


def col(c, a=255):
    return CA(c, a) if isinstance(c, str) else c[:3] + (a,)


def ell(cv, cx, cy, rx, ry, ramp, alpha=255, light=0.0):
    """Elipse sombreada con la misma luz que la célula (arriba a la izquierda)."""
    for y in range(cv.h):
        for x in range(cv.w):
            nx, ny = (x + .5 - cx) / rx, (y + .5 - cy) / ry
            if nx * nx + ny * ny <= 1:
                cv.put(x, y, col(ramp[ramp_idx(shade_v(nx, ny) + light)], alpha))


def thick(cv, pts, widths, colfn):
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


def sheet(frames):
    w, h = frames[0].w, frames[0].h
    im = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, cv in enumerate(frames):
        im.paste(cv.img(), (i * w, 0))
    return im


# ================================================================ 1. BIOMASA
def globule(cv, cx, cy, r, squash=0.0):
    rx, ry = r * (1 + 0.12 * squash), r * (1 - 0.12 * squash)
    ell(cv, cx, cy, rx, ry, BIO)
    cv.put_in(cx - rx * 0.45, cy - ry * 0.45, '#ffffff')          # brillo
    cv.put_in(cx + rx * 0.3, cy + ry * 0.35, BIO[0])               # motita interior


def biomass(f):
    cv = Cv(10)
    bob = [0, -0.5, -1, -0.5][f]
    globule(cv, 5, 5.5 + bob, 3.4, [0, 0.6, 0, -0.6][f])
    cv.outline()
    return cv


def biomass_big(f):
    cv = Cv(14)
    bob = [0, -0.5, -1, -0.5][f]
    p = [0, 0.5, 0, -0.5][f]
    globule(cv, 5.2, 8 + bob, 3.6, p)
    globule(cv, 9.3, 8.5 + bob, 3.0, -p)
    globule(cv, 7.4, 4.6 + bob, 2.7, p)
    cv.outline()
    return cv


def biomass_collect():
    fr = []
    cv = Cv(16); globule(cv, 8, 9.5, 3.6, 1.6); cv.outline(); fr.append(cv)          # se aplasta
    cv = Cv(16); globule(cv, 8, 8, 3.0, -1.4)                                          # se estira
    cv.outline(); fr.append(cv)
    for k, (r, rise, a) in enumerate(((3.5, 2, 255), (5.0, 5, 200))):                # chispitas que suben
        cv = Cv(16)
        for j in range(6):
            ang = -math.pi / 2 + (j - 2.5) * 0.45
            x, y = 8 + math.cos(ang) * r, 9 + math.sin(ang) * r * 0.9 - rise * 0.5
            c = BIO[3] if (j + k) % 2 else BIO[2]
            cv.put(x, y, col(c, a))
            if k == 0:
                cv.put(x, y + 1, col(BIO[1], a))
        if k == 0:
            globule(cv, 8, 10, 1.6)
        fr.append(cv)
    return fr


def ui_biomass():
    cv = Cv(12)
    globule(cv, 5, 7, 3.6)
    globule(cv, 8.4, 4.2, 2.2)
    cv.outline()
    return cv


# ===================================================== 2. MUTACIÓN EN EL SUELO
def bubble(lin, f, n=24, burst=None):
    """Burbuja de membrana con el centro despejado para el icono de 16x16."""
    L = LINEAGES[lin]
    ramp = L['ramp']
    cv = Cv(n)
    c = n / 2
    ph = f * 2 * math.pi / 6
    beat = 0.5 + 0.5 * math.sin(ph)
    for y in range(n):
        for x in range(n):
            dx, dy = x + .5 - c, y + .5 - c
            r = math.hypot(dx, dy)
            a = math.atan2(dy, dx)
            R = 10.6 + 0.55 * math.sin(3 * a + ph) + 0.35 * math.sin(5 * a - ph)   # la membrana ondula
            if r > R:
                continue
            if r > R - 2.2:                                   # membrana
                v = (-0.6 * dx - 0.8 * dy) / R
                i = 3 if v > 0.55 else 2 if v > 0.0 else 1
                cv.put(x, y, col(ramp[i], 230))
            elif r > R - 3.2:                                 # cara interna, más tenue
                cv.put(x, y, col(L['inner'], 150))
            else:                                             # interior: núcleo que late, muy tenue
                glow = max(0.0, 1 - r / 8.5)
                aa = int(25 + 45 * glow * (0.6 + 0.4 * beat))
                cv.put(x, y, col(ramp[2], aa))
    # brillo arriba a la izquierda, sobre la membrana (fuera del centro de 16x16)
    for (dx, dy) in ((-7, -6), (-6, -7), (-5, -8)):
        cv.put(c + dx - .5, c + dy - .5, col(L['light'], 255))
    cv.put(c + 6.5, c + 6.5, col(ramp[3], 200))
    cv.outline()
    return cv


def bubble_collect(lin):
    L = LINEAGES[lin]
    ramp = L['ramp']
    fr = []
    big = Cv(32)
    b = bubble(lin, 0)                                       # 0: se hincha
    for (x, y), cc in b.px.items():
        big.px[(x + 4, y + 4)] = cc
    fr.append(big)
    for k, (r, w, a) in enumerate(((12.5, 2.4, 230), (14.0, 1.6, 200))):   # 1-2: se abre en jirones
        cv = Cv(32)
        for y in range(32):
            for x in range(32):
                dx, dy = x + .5 - 16, y + .5 - 16
                d = math.hypot(dx, dy)
                ang = math.atan2(dy, dx)
                if abs(d - r) <= w / 2 and math.sin(ang * 7 + k) > -0.2 + 0.4 * k:
                    cv.put(x, y, col(ramp[2] if dy < 0 else ramp[1], a))
        cv.outline()
        fr.append(cv)
    for k, (r, sz) in enumerate(((11.5, 1.6), (13.5, 1.0))):              # 3-4: gotitas que se alejan
        cv = Cv(32)
        for j in range(8):
            ang = j * math.pi / 4 + 0.4
            x, y = 16 + math.cos(ang) * r, 16 + math.sin(ang) * r
            ell(cv, x, y, sz + 0.5, sz + 0.5, ramp)
        if k == 0:
            cv.outline()
        fr.append(cv)
    return fr


def bubble_shadow():
    cv = Cv(16, 6)
    for y in range(6):
        for x in range(16):
            nx, ny = (x + .5 - 8) / 7.5, (y + .5 - 3) / 2.6
            d = nx * nx + ny * ny
            if d <= 1:
                cv.put(x, y, (8, 20, 18, 150 if d < 0.5 else 95))
    return cv


# ================================================ 3. ICONOS DE MUTACIÓN (16/32)
def ic_mandibles(n):
    cv = Cv(n)
    k = n / 16
    cy = n / 2
    base = (3.2 * k, cy)
    for sign in (-1, 1):
        L = 11.5 * k
        pts, ws = [], []
        for i in range(9):
            t = i / 8
            u = L * t
            v = sign * (2.8 * k * math.sin(math.pi * t * 0.9) - 4.2 * k * t ** 3)
            a = sign * 0.55
            x, y = u * math.cos(a) - v * math.sin(a), u * math.sin(a) + v * math.cos(a)
            pts.append((base[0] + 1.2 * k + x, cy + sign * 1.4 * k + y))
            ws.append((2.9 - 1.9 * t) * k)

        def cf(xx, yy, x, y, r, sign=sign):
            dy = (yy + .5 - y) * -sign
            return BONE[3] if (dy < -r * 0.2 and sign < 0) else BONE[2] if dy < r * 0.2 else BONE[1]
        thick(cv, pts, ws, cf)
        # dientes hacia el hueco
        for i in ((3, 5) if n == 16 else (2, 3, 4, 5, 6)):
            (x0, y0), (x1, y1) = pts[i], pts[i + 1]
            dx, dy = x1 - x0, y1 - y0
            ln = math.hypot(dx, dy) or 1
            nx, ny = -dy / ln, dx / ln
            if abs(y0 + ny - cy) > abs(y0 - cy):
                nx, ny = -nx, -ny
            w = ws[i] / 2
            for t in range(1 if n == 16 else (2 if i in (3, 5) else 1)):
                cv.put(x0 + nx * (w + 0.6 + t), y0 + ny * (w + 0.6 + t), BONE[3] if t else BONE[2])
    ell(cv, base[0], cy, 2.6 * k, 3.4 * k, FLESH)
    cv.outline()
    return cv


def ic_stinger(n):
    cv = Cv(n)
    k = n / 16
    # en diagonal: base abajo a la izquierda, punta arriba a la derecha
    d = (math.cos(-math.pi / 4), math.sin(-math.pi / 4))
    ox, oy = 3.0 * k, n - 3.0 * k
    ell(cv, ox, oy, 2.2 * k, 2.2 * k, PURP)
    segs = [(2.6, 2.4), (5.4, 2.0), (8.0, 1.6), (10.2, 1.25)]
    for s, w in reversed(segs):
        x, y = ox + d[0] * s * k, oy + d[1] * s * k
        ell(cv, x, y, w * k, w * k, LIME)
    for s, w in segs[1:]:                                      # juntas oscuras entre segmentos
        x, y = ox + d[0] * (s - 1.3) * k, oy + d[1] * (s - 1.3) * k
        for t in range(-int(w * k) + 1, int(w * k)):
            cv.put_in(x + t * 0.707, y + t * 0.707, LIME[0])
    for t in range(int(3 * k)):                               # punta
        cv.put(ox + d[0] * (11.8 * k + t), oy + d[1] * (11.8 * k + t), LIME[3] if t > k else LIME[2])
    tx, ty = ox + d[0] * (11.8 * k + 3 * k), oy + d[1] * (11.8 * k + 3 * k)
    ell(cv, tx + 0.3 * k, ty + 1.2 * k, 1.4 * k + 0.2, 1.7 * k + 0.2, PURP)   # gota de veneno
    cv.put_in(tx - 0.2 * k, ty + 0.6 * k, PURP[3])
    cv.outline()
    return cv


def ic_spores(n):
    cv = Cv(n)
    k = n / 16
    sacs = [(8.0, 5.0, 3.6), (4.6, 8.6, 3.0), (11.4, 8.6, 3.0)]
    base = (8, n - 1.5 * k)

    def stalk(xx, yy, x, y, r):
        return LIME[1] if xx + .5 < x else LIME[0]
    for (x, y, r) in sacs:
        thick(cv, [base, (8, 12 * k), (x * k, y * k + 1.5 * k)], [1.8 * k, 1.4 * k], stalk)
    for (x, y, r) in sorted(sacs, key=lambda s: s[1]):
        ell(cv, x * k, y * k, r * k, r * k * 0.95, PURP)
        for (dx, dy) in ((-0.9, -0.5), (0.8, 0.9)) if n == 16 else ((-1, -1), (1, 1), (1.5, -1.5), (-1.6, 0.8)):
            cv.put_in(x * k + dx * r * k / 3.6 * 1.6, y * k + dy * r * k / 3.6 * 1.6, LIME[2])
        cv.put_in(x * k - r * k * 0.45, y * k - r * k * 0.55, PURP[3])
    cv.outline()
    return cv


def ic_fins(n):
    cv = Cv(n)
    k = n / 16
    M.fin(cv, (2.5 * k, n - 2.5 * k), -math.pi / 4, 0.78, 12.4 * k, 0.6, alpha=255)
    cv.outline()
    return cv


def ic_legs(n):
    cv = Cv(n)
    k = n / 16
    w1, w2 = (2.0, 1.5) if n == 16 else (3.4, 2.6)
    # trozo de cuerpo acorazado arriba a la izquierda y tres patas que bajan
    for i, (hx, hy, kx, ky, fx, fy) in enumerate(((3, 5, 7, 2.5, 11, 7.5), (3, 8, 8.5, 6.5, 13, 11.5), (3, 11, 7.5, 10.5, 10, 14.5))):
        pts = [(hx * k, hy * k), (kx * k, ky * k), (fx * k, fy * k)]

        def cf(xx, yy, x, y, r):
            return ARM[3] if yy + .5 < y - r * 0.3 else ARM[2]

        def ct(xx, yy, x, y, r):
            return ARM[2] if yy + .5 < y else ARM[1]
        thick(cv, pts[:2], [w1 * (1 if n == 16 else 1)], cf)
        thick(cv, pts[1:], [w2], ct)
        cv.put(kx * k, ky * k, ARM[3])
        cv.put(fx * k, fy * k, ARM[0])
        if n == 32:
            cv.put(kx * k + 1, ky * k, ARM[3])
    ell(cv, 1.5 * k, 8 * k, 2.6 * k, 6.2 * k, ARM, light=-0.1)
    cv.outline()
    return cv


def ic_carapace(n):
    cv = Cv(n)
    k = n / 16
    cx, cy, rx, ry = n / 2, n / 2 + 1.2 * k, 7.0 * k, 6.0 * k
    for y in range(n):
        for x in range(n):
            nx, ny = (x + .5 - cx) / rx, (y + .5 - cy) / ry
            if nx * nx + ny * ny > 1 or ny > 0.62:
                continue
            seg = ((y + .5 - (cy - ry)) / (3.2 * k))
            f = seg - math.floor(seg)
            if f < (0.3 if n == 16 else 0.18):
                cv.put(x, y, GREEN['ramp'][0])               # la membrana asoma entre placas
                continue
            v = shade_v(nx, ny)
            i = (3 if v > 0.3 else 2) if f < 0.5 else 2 if f < 0.82 else 1
            if v < -0.1:
                i -= 1
            cv.put(x, y, ARM[max(0, i)])
    # borde blando de abajo (membrana verde)
    for x in range(n):
        for y in range(n):
            nx, ny = (x + .5 - cx) / rx, (y + .5 - cy) / ry
            if nx * nx + ny * ny <= 1 and 0.62 < ny <= 0.62 + 1.6 / ry:
                cv.put(x, y, GREEN['ramp'][1])
    cv.put_in(cx - rx * 0.5, cy - ry * 0.62, '#ffffff')
    cv.outline()
    return cv


def ic_stinging(n):
    cv = Cv(n)
    k = n / 16
    c = n / 2
    r = 4.6 * k
    N = 10 if n == 16 else 14
    for i in range(N):
        a = 2 * math.pi * i / N + 0.2
        big = i % 2 == 0
        L = (2.6 if big else 1.4) * k
        cols = [PURP[1], PURP[2], LIME[2]] if big else [LIME[1], LIME[2], LIME[3]]
        steps = int(L * 2) + 1
        for s in range(steps):
            d = s * 0.5
            px, py = c + math.cos(a) * (r + d), c + math.sin(a) * (r + d)
            if 1 <= round(px) <= n - 2 and 1 <= round(py) <= n - 2:
                cv.put(px, py, cols[min(2, int(d / max(L, .01) * 3))])
    ell(cv, c, c, r + 0.3, r + 0.3, M.DARKCELL['ramp'])
    for (dx, dy, cc) in ((-1.5, 1, LIME[1]), (1.5, -1.5, PURP[2]), (1.5, 2, LIME[1])):
        cv.put_in(c + dx * k, c + dy * k, cc)
    cv.put_in(c - r * 0.5, c - r * 0.55, '#ffffff')
    cv.outline()
    return cv


def ic_inksac(n):
    cv = Cv(n)
    k = n / 16
    ell(cv, 7 * k, 6.6 * k, 5.4 * k, 4.6 * k, INK)
    cv.put_in(4.6 * k, 4.2 * k, '#a8c0f0')
    cv.put_in(5.6 * k, 3.7 * k, INK[3])
    # gota de tinta saliendo abajo a la derecha
    thick(cv, [(10 * k, 10 * k), (11.6 * k, 12.2 * k)], [1.6 * k], lambda *a: INK[1])
    ell(cv, 12.6 * k, 13.3 * k, 1.7 * k + 0.2, 2.0 * k + 0.2, INK)
    cv.put_in(12.2 * k, 12.6 * k, INK[3])
    cv.outline()
    return cv


def ic_shock(n):
    cv = Cv(n)
    k = n / 16
    c = n / 2
    ell(cv, c, c, 5.6 * k, 5.6 * k, ['#2a5a9a', '#3a7ac0', '#4a9ae0', '#6ab4f0'], alpha=150)   # halo
    ell(cv, c, c, 3.8 * k, 3.8 * k, SHOCK)
    cv.put_in(c - 1.4 * k, c - 1.4 * k, '#ffffff')
    cv.outline(alpha=200)
    # chispas en zigzag hacia fuera
    for j, a in enumerate((-2.4, -0.5, 1.2, 2.6)):
        x, y = c + math.cos(a) * 5.0 * k, c + math.sin(a) * 5.0 * k
        pts = [(x, y)]
        for s in range(1, 3 if n == 16 else 4):
            off = (1 if s % 2 else -1) * 0.8
            pts.append((c + math.cos(a) * (5 + s * 1.2) * k - math.sin(a) * off, c + math.sin(a) * (5 + s * 1.2) * k + math.cos(a) * off))
        for p0, p1 in zip(pts, pts[1:]):
            for t in range(4):
                tt = t / 3
                px, py = p0[0] + (p1[0] - p0[0]) * tt, p0[1] + (p1[1] - p0[1]) * tt
                if 0 <= round(px) < n and 0 <= round(py) < n:
                    cv.put(px, py, '#ffffff' if t % 2 else SHOCK[2])
    return cv


ICONS = {'Mandibles': ic_mandibles, 'Stinger': ic_stinger, 'Spores': ic_spores, 'Fins': ic_fins,
         'JointedLegs': ic_legs, 'Carapace': ic_carapace, 'StingingSkin': ic_stinging,
         'InkSac': ic_inksac, 'ShockGland': ic_shock}
LINEAGE_OF = {'Mandibles': 'Predator', 'Fins': 'Predator', 'InkSac': 'Predator',
              'JointedLegs': 'Armored', 'Carapace': 'Armored', 'ShockGland': 'Armored',
              'Stinger': 'Toxic', 'Spores': 'Toxic', 'StingingSkin': 'Toxic'}


# ===================================================== 4. ICONOS DE LINAJE
def lin_predator():
    cv = Cv(12)
    # colmillo curvo de hueso sobre encía naranja
    pts = [(5.0, 3.5), (5.4, 6.4), (6.4, 8.9), (7.8, 10.6)]
    thick(cv, pts, [3.6, 2.8, 1.6], lambda xx, yy, x, y, r: BONE[3] if xx + .5 < x - r * 0.2 else BONE[2] if xx + .5 < x + r * 0.4 else BONE[1])
    ell(cv, 6.0, 2.6, 4.6, 2.0, FLESH)
    cv.outline()
    return cv


def lin_armored():
    cv = Cv(12)
    for y in range(12):
        for x in range(12):
            dx = abs(x + .5 - 6)
            top = 1.0
            half = 4.6 if y < 6 else 4.6 - (y - 5.5) * 0.85      # escudo que se estrecha abajo
            if y + .5 < top or y > 10 or dx > half:
                continue
            band = (y - 1) % 3
            if band == 0 and y > 1:
                cv.put(x, y, ARM[0])
                continue
            v = (-0.6 * (x + .5 - 6) / 5 - 0.8 * (y + .5 - 6) / 5) * 0.8 + 0.35
            i = 3 if (band == 1 and v > 0.3) else 2 if v > 0.05 else 1
            cv.put(x, y, ARM[i])
    cv.outline()
    return cv


def lin_toxic():
    cv = Cv(12)
    # gota lima con una espora morada dentro
    for y in range(12):
        for x in range(12):
            dx, dy = x + .5 - 6, y + .5 - 7.2
            r = math.hypot(dx, dy)
            inside = r <= 3.9 or (dy < 0 and abs(dx) <= 3.9 * (1 + dy / 6.4) and dy > -6.2)
            if inside:
                v = (-0.6 * dx - 0.8 * dy) / 4.5 * 0.8 + 0.35
                cv.put(x, y, LIME[ramp_idx(v)])
    ell(cv, 6.6, 7.8, 1.7, 1.7, PURP)
    cv.put_in(4.4, 5.6, '#ffffff')
    cv.outline()
    return cv


def slot_limbs():
    """Aleta morada con sus radios: icono de la ranura de Extremidades."""
    cv = Cv(12)
    for y in range(12):
        for x in range(12):
            px, py = x + .5, y + .5
            # abanico que sale de abajo a la izquierda
            dx, dy = px - 2.5, py - 10.0
            r = math.hypot(dx, dy)
            a = math.atan2(-dy, dx)
            if r > 9.2 - 0.9 * math.sin(a * 6) or a < 0.15 or a > 1.45:
                continue
            ray = math.sin(a * 11) > 0.55
            v = (-0.6 * (px - 6) - 0.8 * (py - 6)) / 6 * 0.8 + 0.35
            i = 1 if ray else (3 if v > 0.55 else 2)
            cv.put(x, y, PURP[i])
    ell(cv, 2.6, 9.6, 1.6, 1.6, PURP)
    cv.outline()
    return cv


SLOT_ICONS = {'Mouth': lin_predator, 'Limbs': slot_limbs, 'Skin': lin_armored, 'Organ': lin_toxic}


# =========================================================== 5. CORAZONES
HEART = ['#7a1e2a', '#c8384a', '#f0706e', '#ffd0c0']


def heart_mask(x, y):
    px, py = x + .5, y + .5
    l = (px - 5.0) ** 2 + (py - 5.6) ** 2 <= 3.9 ** 2
    r = (px - 11.0) ** 2 + (py - 5.6) ** 2 <= 3.9 ** 2
    tri = py >= 5.6 and abs(px - 8) <= (14.2 - py) * 0.95
    return l or r or tri


def heart(kind):
    cv = Cv(16)
    for y in range(16):
        for x in range(16):
            if not heart_mask(x, y):
                continue
            filled = kind == 'full' or (kind == 'half' and x < 8)
            if filled:
                nx, ny = (x + .5 - 8) / 7, (y + .5 - 8) / 7
                cv.put(x, y, HEART[ramp_idx(shade_v(nx, ny) + 0.15)])
            elif any(not heart_mask(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                cv.put(x, y, (110, 40, 56, 230))                  # borde interior apagado, para que se vea vacío
            else:
                cv.put(x, y, (24, 20, 37, 150))
    if kind != 'empty':
        for (x, y, c) in ((4, 4, '#ffffff'), (5, 3, '#fff0e0'), (4, 5, HEART[3])):
            cv.put(x, y, c)
        # núcleo de la célula-corazón
        for (x, y) in ((6, 8), (7, 8), (6, 9)) if kind == 'half' else ((9, 8), (10, 8), (9, 9)):
            cv.put(x, y, HEART[1])
    if kind == 'half':
        for y in range(16):                                   # costura en el corte
            if heart_mask(7, y) and heart_mask(8, y):
                cv.put(8, y, (24, 20, 37, 200))
    cv.outline()
    return cv


# ============================================================ 6. MIRA
CROSS = ['#5a7a2a', '#b8e070', '#ecffc0']


def crosshair(r=4.6, tick=(1, 3)):
    cv = Cv(15)
    c = 7.5
    for y in range(15):
        for x in range(15):
            d = math.hypot(x + .5 - c, y + .5 - c)
            if abs(d - r) <= 0.55:
                cv.put(x, y, CROSS[2] if (x + y) < 15 else CROSS[1])
    # cuatro marcas hacia fuera
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        for t in range(tick[0], tick[1] + 1):
            cv.put(7 + dx * (round(r) + t), 7 + dy * (round(r) + t), CROSS[2])
    cv.put(7, 7, CROSS[2])                                     # centro exacto
    cv.outline()
    return cv


# ================================================= 7. RANURAS DEL CUERPO
def slot_frame():
    cv = Cv(20)
    for y in range(20):
        for x in range(20):
            # cuadrado redondeado con borde de 2 px (deja 16x16 dentro)
            ax, ay = abs(x + .5 - 10), abs(y + .5 - 10)
            qx, qy = max(ax - 6.5, 0), max(ay - 6.5, 0)
            d = math.hypot(qx, qy)
            if d > 3.2:
                continue
            if ax < 8 and ay < 8:
                cv.put(x, y, (16, 36, 31, 150))               # fondo translúcido
            else:
                v = (-0.6 * (x - 10) - 0.8 * (y - 10)) / 10
                cv.put(x, y, MEM[3] if v > 0.45 else MEM[2] if v > -0.2 else MEM[1])
    cv.outline()
    return cv


def dimmed(cv_src, keep_outline=True):
    """Convierte una pieza en silueta apagada."""
    cv = Cv(cv_src.w, cv_src.h)
    for p, c in cv_src.px.items():
        if c[:3] == OUTL[:3]:
            cv.px[p] = OUTL[:3] + (150,)
        else:
            lum = (c[0] * 3 + c[1] * 6 + c[2]) / 10
            cv.px[p] = C(DIM[min(3, int(lum / 64))])[:3] + (200,)
    return cv


def slot_empty(kind):
    src = {'Mouth': ic_mandibles, 'Limbs': ic_legs, 'Skin': ic_carapace, 'Organ': ic_inksac}[kind](16)
    return dimmed(src)


# ================================================== 8. BARRA DE RECARGA
def cooldown_back():
    cv = Cv(18, 4)
    for y in range(1, 3):
        for x in range(1, 17):
            cv.put(x, y, (12, 22, 30, 200))
    for x in range(1, 17):
        cv.put(x, 0, OUTL); cv.put(x, 3, OUTL)
    for y in range(1, 3):
        cv.put(0, y, OUTL); cv.put(17, y, OUTL)
    return cv


def cooldown_fill():
    cv = Cv(16, 2)
    for x in range(16):
        cv.put(x, 0, SHOCK[2]); cv.put(x, 1, SHOCK[1])
    cv.put(0, 0, '#ffffff')
    return cv


def cooldown_ready():
    fr = []
    for k, (top, bot, edge) in enumerate((('#ffffff', '#ffffff', '#ffffff'), ('#e8f8ff', SHOCK[2], SHOCK[2]),
                                          (SHOCK[2], SHOCK[1], SHOCK[1]))):
        cv = Cv(18, 4)
        for x in range(1, 17):
            cv.put(x, 1, top); cv.put(x, 2, bot)
        for x in range(1, 17):
            cv.put(x, 0, edge if k < 2 else OUTL); cv.put(x, 3, edge if k < 2 else OUTL)
        for y in range(1, 3):
            cv.put(0, y, edge if k < 2 else OUTL); cv.put(17, y, edge if k < 2 else OUTL)
        fr.append(cv)
    return fr


# ============================================ 9. TARJETAS, PANEL Y BOTONES
def nine(w, h, border, rim, fill, radius, bubbles=True, sunken=False, alpha_fill=225, glow=None):
    """Membrana 9-slice: las esquinas llevan los detalles; los bordes son
    constantes a lo largo para que se estiren sin deformarse."""
    cv = Cv(w, h)
    rad = radius
    for y in range(h):
        for x in range(w):
            ax = min(x + .5, w - x - .5)                    # distancia al borde izquierdo/derecho
            ay = min(y + .5, h - y - .5)
            # esquina redondeada
            if ax < rad and ay < rad:
                d = rad - math.hypot(rad - ax, rad - ay)
            else:
                d = min(ax, ay)
            if d < 0.5:
                continue
            top = (y + .5) < h / 2
            left = (x + .5) < w / 2
            if d < 1.5:
                cv.put(x, y, OUTL)
            elif d < 3.0:                                    # membrana del borde, luz arriba a la izquierda
                lit = (top and ay <= ax) or (left and ax < ay)
                if sunken:
                    lit = not lit
                cv.put(x, y, rim[3] if lit else rim[1])
            elif d < 4.0:
                cv.put(x, y, col(rim[0], 235))
            else:
                cv.put(x, y, col(fill, alpha_fill))
    if bubbles:                                            # burbujitas solo en las esquinas
        for (bx, by, br) in ((5.5, 5.5, 1.3), (8.5, 4.6, 0.8)):
            for (sx, sy) in ((bx, by), (w - bx, by), (bx, h - by), (w - bx, h - by)):
                for y in range(h):
                    for x in range(w):
                        if math.hypot(x + .5 - sx, y + .5 - sy) <= br:
                            cv.put(x, y, col(rim[2], 200))
                cv.put(sx - 0.8, sy - 0.8, col(rim[3], 230))
    if glow:                                               # halo exterior al resaltar
        for (x, y), c in list(cv.px.items()):
            if c[:3] == OUTL[:3]:
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    q = (x + dx, y + dy)
                    if cv.inside(*q) and q not in cv.px:
                        cv.px[q] = col(glow, 150)
    return cv


CARD_FILL = '#0e221d'
DARK_MEM = ['#122a24', '#1e4a3e', '#3a7a64', '#7fbf9e']


def card(state):
    if state == 'Normal':
        return nine(48, 48, 12, DARK_MEM, CARD_FILL, 9)
    if state == 'Highlighted':
        return nine(48, 48, 12, MEM_HI, '#12302a', 9, glow=MEM_HI[2])
    if state == 'Pressed':
        return nine(48, 48, 12, ['#0c1e1a', '#16382f', '#2a5e50', '#4a9078'], '#0a1a16', 9, sunken=True, alpha_fill=235)
    if state == 'Disabled':
        return nine(48, 48, 12, ['#1a2422', '#28332f', '#3a4743', '#56625e'], '#141c1a', 9, alpha_fill=170)


def panel():
    return nine(48, 48, 12, ['#0e201b', '#173a32', '#2a5e50', '#4a9078'], '#0a1814', 10, alpha_fill=235)


def button(state):
    if state == 'Normal':
        return nine(32, 16, 7, MEM, '#1e4a3e', 6, bubbles=False, alpha_fill=255)
    if state == 'Highlighted':
        return nine(32, 16, 7, MEM_HI, '#2a5e50', 6, bubbles=False, alpha_fill=255, glow=MEM_HI[2])
    return nine(32, 16, 7, ['#0c1e1a', '#16382f', '#2a5e50', '#4a9078'], '#173a32', 6, bubbles=False,
                alpha_fill=255, sunken=True)


def card_stripe():
    """Franja blanca para teñir con el color del linaje (9-slice horizontal)."""
    cv = Cv(16, 6)
    G = ['#9a9a9a', '#c8c8c8', '#ececec', '#ffffff']
    for y in range(6):
        for x in range(16):
            ax = min(x + .5, 16 - x - .5)
            if ax < 2.5 and (y + .5 < 2.5 - ax or y + .5 > 3.5 + ax):     # puntas redondeadas
                continue
            if y == 0 or y == 5:
                cv.put(x, y, G[0])
            else:
                cv.put(x, y, G[3] if y == 1 else G[2] if y < 4 else G[1])
    return cv


# ================================================================ GUARDAR
MS = {}


def save(group, name, frames, ms=None):
    os.makedirs(os.path.join(OUT, group), exist_ok=True)
    sheet(frames).save(os.path.join(OUT, group, name + '.png'))
    if len(frames) > 1:
        os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
        w, h = frames[0].w, frames[0].h
        sc = max(4, 192 // max(w, h))
        out = []
        for cv in frames:
            b = Image.new('RGBA', (w, h), BG)
            b.alpha_composite(cv.img())
            out.append(b.resize((w * sc, h * sc), Image.NEAREST).convert('RGB'))
        out[0].save(os.path.join(OUT, 'gif', name + '.gif'), save_all=True, append_images=out[1:], duration=ms, loop=0)
    MS[name] = (group, len(frames), frames[0].w, frames[0].h, ms)


def build():
    A = {}
    A['Item_Biomass_Idle'] = ('Items', [biomass(f) for f in range(4)], 150)
    A['Item_Biomass_Big_Idle'] = ('Items', [biomass_big(f) for f in range(4)], 150)
    A['Item_Biomass_Collect'] = ('Items', biomass_collect(), 60)
    A['UI_Biomass'] = ('HUD', [ui_biomass()], None)
    for lin in LINEAGES:
        A[f'Item_Mutation_{lin}_Idle'] = ('Items', [bubble(lin, f) for f in range(6)], 120)
        A[f'Item_Mutation_{lin}_Collect'] = ('Items', bubble_collect(lin), 60)
    A['Item_Mutation_Shadow'] = ('Items', [bubble_shadow()], None)
    for m, fn in ICONS.items():
        A[f'Icon_{m}_16'] = ('Icons', [fn(16)], None)
        A[f'Icon_{m}_32'] = ('Icons', [fn(32)], None)
    A['Icon_Lineage_Predator'] = ('Icons', [lin_predator()], None)
    A['Icon_Lineage_Armored'] = ('Icons', [lin_armored()], None)
    A['Icon_Lineage_Toxic'] = ('Icons', [lin_toxic()], None)
    for s, fn in SLOT_ICONS.items():
        A[f'Icon_Slot_{s}'] = ('Icons', [fn()], None)
    A['UI_Heart_Full'] = ('HUD', [heart('full')], None)
    A['UI_Heart_Half'] = ('HUD', [heart('half')], None)
    A['UI_Heart_Empty'] = ('HUD', [heart('empty')], None)
    A['UI_Crosshair'] = ('HUD', [crosshair()], None)
    A['UI_Crosshair_Fire'] = ('HUD', [crosshair(3.4, (1, 2)), crosshair(4.0, (1, 3))], 40)
    A['UI_Slot_Frame'] = ('HUD', [slot_frame()], None)
    for s in ('Mouth', 'Limbs', 'Skin', 'Organ'):
        A[f'UI_Slot_{s}_Empty'] = ('HUD', [slot_empty(s)], None)
    A['UI_Cooldown_Back'] = ('HUD', [cooldown_back()], None)
    A['UI_Cooldown_Fill'] = ('HUD', [cooldown_fill()], None)
    A['UI_Cooldown_Ready'] = ('HUD', cooldown_ready(), 50)
    for st in ('Normal', 'Highlighted', 'Pressed', 'Disabled'):
        A[f'UI_Card_{st}'] = ('Panels', [card(st)], None)
    A['UI_Panel'] = ('Panels', [panel()], None)
    for st in ('Normal', 'Highlighted', 'Pressed'):
        A[f'UI_Button_{st}'] = ('Panels', [button(st)], None)
    A['UI_Card_Stripe'] = ('Panels', [card_stripe()], None)
    return A


# ============================================================ VISTA PREVIA
def nine_render(im, w, h, b):
    """Estira una imagen 9-slice (borde b, o (izq, arriba, der, abajo)) a w x h."""
    l, t, r, bt = (b, b, b, b) if isinstance(b, int) else b
    W, H = im.size
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    xs = [(0, l, 0, l), (l, W - r, l, w - r), (W - r, W, w - r, w)]
    ys = [(0, t, 0, t), (t, H - bt, t, h - bt), (H - bt, H, h - bt, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if sx1 <= sx0 or sy1 <= sy0 or dx1 <= dx0 or dy1 <= dy0:
                continue
            piece = im.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.NEAREST)
            out.alpha_composite(piece, (dx0, dy0))
    return out


def tint(im, rgb):
    r, g, b, a = im.split()
    r = r.point(lambda v: v * rgb[0] // 255)
    g = g.point(lambda v: v * rgb[1] // 255)
    b = b.point(lambda v: v * rgb[2] // 255)
    return Image.merge('RGBA', (r, g, b, a))


def floor_bg(w, h, walls=False):
    tiles = os.path.join(ROOT, 'environment', 'Soup', 'tiles')
    bg = Image.new('RGBA', (w, h), BG)
    if not os.path.isdir(tiles):
        return bg
    fl = [Image.open(os.path.join(tiles, f'Floor_{i}.png')).convert('RGBA') for i in range(6)]
    for ty in range(0, h, 16):
        for tx in range(0, w, 16):
            bg.paste(fl[(tx * 7 + ty * 13) // 16 % 6 if (tx + ty) % 48 else 0], (tx, ty))
    if walls:
        cap = Image.open(os.path.join(tiles, 'Wall_Cap.png')).convert('RGBA')
        fu = Image.open(os.path.join(tiles, 'Wall_FaceUpper_0.png')).convert('RGBA')
        flw = Image.open(os.path.join(tiles, 'Wall_FaceLower_0.png')).convert('RGBA')
        sh = Image.open(os.path.join(tiles, 'Floor_ShadowTop_0.png')).convert('RGBA')
        for tx in range(0, w, 16):
            for ty in range(0, 48, 16):
                bg.paste(cap, (tx, ty))
            bg.paste(fu, (tx, 48)); bg.paste(flw, (tx, 64)); bg.paste(sh, (tx, 80))
    return bg


def text(d, xy, s, fill=(230, 240, 220)):
    d.text(xy, s, fill=fill)


def preview(A, path):
    img = {k: sheet(v[1]) for k, v in A.items()}
    first = {k: v[1][0].img() for k, v in A.items()}
    S = 3

    # 1. HUD sobre la sala
    hud = floor_bg(480, 270, walls=True)
    bac = os.path.join(ROOT, 'enemies', 'Bacteria', 'Bacteria_Move.png')
    player = M.carapace('down').img()
    hud.alpha_composite(M.mandibles(0.45).img().rotate(-90, expand=True), (240 - 12, 135 + 6))
    hud.alpha_composite(player, (240 - 12, 135 - 12))
    if os.path.exists(bac):
        b = Image.open(bac).convert('RGBA').crop((0, 0, 16, 16))
        hud.alpha_composite(b, (330, 150))
        hud.alpha_composite(first['UI_Crosshair'], (330, 150))                 # mira sobre enemigo
    hud.alpha_composite(first['UI_Crosshair'], (120, 30))                      # sobre la pared
    hud.alpha_composite(first['UI_Crosshair'], (300, 210))                     # sobre el suelo
    hud.alpha_composite(img['UI_Crosshair_Fire'].crop((0, 0, 15, 15)), (360, 210))
    for i, k in enumerate(('Full', 'Full', 'Half', 'Empty')):
        hud.alpha_composite(first[f'UI_Heart_{k}'], (8 + i * 17, 8))
    slots = [('Mouth', 'Mandibles'), ('Limbs', None), ('Skin', 'Carapace'), ('Organ', None)]
    for i, (s, m) in enumerate(slots):
        x, y = 8 + i * 22, 28
        hud.alpha_composite(first['UI_Slot_Frame'], (x, y))
        hud.alpha_composite(first[f'Icon_{m}_16'] if m else first[f'UI_Slot_{s}_Empty'], (x + 2, y + 2))
    hud.alpha_composite(first['UI_Biomass'], (420, 9))
    d = ImageDraw.Draw(hud)
    text(d, (435, 9), 'x 42')

    # 2. Pantalla de elección
    ch = floor_bg(480, 270, walls=True)
    ch.alpha_composite(Image.new('RGBA', (480, 270), (6, 14, 12, 150)))
    ch.alpha_composite(nine_render(first['UI_Panel'], 440, 236, 12), (20, 14))
    d = ImageDraw.Draw(ch)
    text(d, (196, 22), 'ELIGE UNA MUTACION', (228, 255, 216))
    picks = [('Mandibles', 'Predator', 'Mandibulas', 'Boca'), ('Carapace', 'Armored', 'Caparazon', 'Piel'),
             ('Spores', 'Toxic', 'Esporas', 'Boca')]
    for i, (m, lin, name, slot) in enumerate(picks):
        x, y = 30 + i * 145, 40
        st = 'Highlighted' if i == 1 else 'Normal'
        ch.alpha_composite(nine_render(first[f'UI_Card_{st}'], 130, 150, 12), (x, y))
        stripe = tint(nine_render(first['UI_Card_Stripe'], 114, 6, (4, 0, 4, 0)), LINEAGES[lin]['color'])
        ch.alpha_composite(stripe, (x + 8, y + 8))
        ch.alpha_composite(first[f'Icon_{m}_32'], (x + 49, y + 20))
        d = ImageDraw.Draw(ch)
        text(d, (x + 65 - len(name) * 3, y + 58), name, (255, 255, 240))
        ch.alpha_composite(first[f'Icon_Lineage_{lin}'], (x + 30, y + 74))
        text(d, (x + 46, y + 75), slot + ' / ' + {'Predator': 'Depred.', 'Armored': 'Acoraz.', 'Toxic': 'Toxico'}[lin], (200, 220, 210))
        for k, line in enumerate(('Descripcion de la', 'mutacion en dos', 'o tres lineas.')):
            text(d, (x + 14, y + 94 + k * 11), line, (190, 210, 200))
        text(d, (x + 14, y + 130), 'Ranura libre', (255, 204, 128))
    ch.alpha_composite(nine_render(first['UI_Button_Normal'], 64, 16, 7), (208, 204))
    text(ImageDraw.Draw(ch), (224, 206), 'Saltar')

    # 3. En el suelo
    gr = floor_bg(480, 270)
    cell = M.PANIMS['Idle_down'][0].img()
    cx, cy = 120, 120
    gr.alpha_composite(cell, (cx - 8, cy - 8))
    bar_y = cy + 14                                            # 0,85 unidades bajo el centro
    gr.alpha_composite(first['UI_Cooldown_Back'], (cx - 9, bar_y - 2))
    fill = first['UI_Cooldown_Fill'].crop((0, 0, 10, 2))
    gr.alpha_composite(fill, (cx - 8, bar_y - 1))
    gr.alpha_composite(first['Item_Biomass_Idle'], (cx + 24, cy - 2))
    gr.alpha_composite(first['Item_Biomass_Big_Idle'], (cx + 44, cy - 4))
    for i, (lin, m) in enumerate((('Predator', 'Fins'), ('Armored', 'ShockGland'), ('Toxic', 'Stinger'))):
        x, y = 230 + i * 50, 112
        gr.alpha_composite(first['Item_Mutation_Shadow'], (x + 4, y + 22))
        gr.alpha_composite(first[f'Item_Mutation_{lin}_Idle'], (x, y))
        gr.alpha_composite(first[f'Icon_{m}_16'], (x + 4, y + 4))
    # iconos de 16 y de 32 sobre el suelo para comparar
    for i, m in enumerate(ICONS):
        gr.alpha_composite(first[f'Icon_{m}_16'], (60 + i * 40, 190))
        gr.alpha_composite(first[f'Icon_{m}_32'], (52 + i * 40, 216))

    blocks = [('1. HUD', hud), ('2. Pantalla de eleccion', ch), ('3. En el suelo', gr)]
    lab = 22
    out = Image.new('RGBA', (480 * S + 20, sum(480 * 0 + 270 * S + lab + 10 for _ in blocks) + 10), BG)
    d = ImageDraw.Draw(out)
    y = 10
    for name, im in blocks:
        text(d, (12, y + 4), name, (214, 255, 154))
        y += lab
        out.alpha_composite(im.resize((480 * S, 270 * S), Image.NEAREST), (10, y))
        y += 270 * S + 10
    out.convert('RGB').save(path)

    # hoja de iconos ampliada para revisar que se distinguen
    sc = 4
    ic = Image.new('RGBA', ((9 * 40 + 10) * sc, 64 * sc), BG)
    for i, m in enumerate(ICONS):
        ic.alpha_composite(first[f'Icon_{m}_32'].resize((32 * sc, 32 * sc), Image.NEAREST), ((10 + i * 40) * sc, 4 * sc))
        ic.alpha_composite(first[f'Icon_{m}_16'].resize((16 * sc, 16 * sc), Image.NEAREST), ((18 + i * 40) * sc, 42 * sc))
    ic.convert('RGB').save(os.path.join(OUT, '_iconos_x4.png'))


def main():
    A = build()
    for name, (group, frames, ms) in A.items():
        save(group, name, frames, ms)
    preview(A, os.path.join(OUT, 'vista_previa_ui.png'))
    for k, v in MS.items():
        print(k, *v)


if __name__ == '__main__':
    main()
