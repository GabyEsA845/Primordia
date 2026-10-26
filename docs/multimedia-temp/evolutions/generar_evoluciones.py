"""
generar_evoluciones.py - Primordia: niveles y evoluciones de las mutaciones.

Rehace el Aguijón (cola de avispa) y el Caparazón (concha de tortuga), y dibuja
los efectos de las evoluciones, la tinta nueva, la onda grande, las marcas de
estado de los enemigos y las insignias de nivel.

Reutiliza los otros generadores:
  generar_menus.py -> generar_ui.py -> generar_mutaciones.py -> generar_jugador.py
  (Canvas/Cv, body, features, contorno #181425, rampas, colores de linaje,
   los parámetros de cada cuadro de la célula, iconos, UI_Slot_Frame, flecha)
  generar_efectos.py y generar_proyectiles.py (disc, ring, la onda de choque)
y los sprites ya generados de enemies/, effects/ y projectiles/ para la vista previa.

Uso:
  python generar_evoluciones.py [carpeta_multimedia_temp] [carpeta_salida]
Por defecto: la carpeta de arriba y la carpeta de este script.
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

# --------------------------------------------- cargar los otros generadores
_argv = sys.argv
sys.argv = ['generar_menus.py', ROOT, os.devnull]
_spec = importlib.util.spec_from_file_location('generar_menus', os.path.join(ROOT, 'menus', 'generar_menus.py'))
G = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(G)
sys.argv = _argv
U = G.U            # generar_ui.py
M = U.M            # generar_mutaciones.py


def _defs(path, cut):
    """Ejecuta solo las definiciones de un generador antiguo (sin la parte que guarda)."""
    src = open(path, encoding='utf-8').read().replace('\r', '')
    ns = {'__name__': os.path.basename(path)}
    argv = sys.argv
    sys.argv = [path, os.devnull]
    exec(src.split(cut, 1)[0], ns)
    sys.argv = argv
    return ns


FX = _defs(os.path.join(ROOT, 'effects', 'generar_efectos.py'), '\nanims = {')

C, CA, OUTL = M.C, M.CA, M.OUTL
Canvas, body, features, rot = M.Canvas, M.body, M.features, M.rot
shade_v, ramp_idx, geom, blob, thick = M.shade_v, M.ramp_idx, M.geom, M.blob, M.thick
Cv, ell = U.Cv, U.ell
GREEN, RED, WHITE, PALE = M.GREEN, M.RED, M.WHITE, M.PALE
LIME, PURP, ARM, ARM_PALE, FLESH, BONE, FIN = M.LIME, M.PURP, M.ARM, M.ARM_PALE, M.FLESH, M.BONE, M.FIN
SHOCK = M.SHOCK
EYE = M.P['EYE']
BG = C('#16352e')
AY = M.AY

PURP_D = ['#1c0630', PURP[0], PURP[1], PURP[2]]       # franjas oscuras del abdomen
GOLD = ['#8a5e14', '#d09a2a', '#f8d460', '#fff6d0']


def frac(v):
    return v - math.floor(v)


# ======================================================== 1. AGUIJÓN NUEVO
def abdomen(cv, ox, oy, ang, waist, L, wmax, curl=0.0, band=None, needle=4.0, drop=1.0, drop_dy=0.0,
            flash=False, hi=True):
    """Abdomen de avispa en cualquier dirección: cintura fina, abdomen con franjas
    lima y morado oscuro, púa brillante y gota de veneno en la punta.
    (ox, oy) es la unión; ang la dirección; curl curva la cola hacia +normal."""
    d = (math.cos(ang), math.sin(ang))
    nrm = (-d[1], d[0])
    a0 = waist
    band = band or L / 3.5
    end = a0 + L

    def vc(u):                                    # desvío de la línea central (curva de la cola)
        t = max(0.0, (u - a0) / L)
        return curl * t * t * L * 0.22

    def half(u):
        if u < a0:
            return 1.05                           # cintura (pecíolo)
        t = (u - a0) / L
        if t > 1:
            return -1
        p = 0.4
        if t < p:
            w = wmax * math.sqrt(max(0.0, 1 - ((p - t) / p) ** 2))
        else:
            w = wmax * (1 - ((t - p) / (1 - p)) ** 2.0)
        return max(w, 1.05 if t < 0.5 else 0.0)

    xs = range(int(min(ox, ox + d[0] * end) - wmax - 3), int(max(ox, ox + d[0] * end) + wmax + 3))
    ys = range(int(min(oy, oy + d[1] * end) - wmax - 3), int(max(oy, oy + d[1] * end) + wmax + 3))
    for y in ys:
        for x in xs:
            px, py = x + .5 - ox, y + .5 - oy
            u = px * d[0] + py * d[1]
            v = px * nrm[0] + py * nrm[1] - vc(u)
            if u < 0 or u > end:
                continue
            w = half(u)
            if w <= 0 or abs(v) > w:
                continue
            # normal de la superficie en pantalla, para la luz de arriba a la izquierda
            along = 0.0 if u < a0 else ((u - a0) / L - 0.38) * 0.9
            across = v / max(w, 0.8)
            sx = along * d[0] + across * nrm[0]
            sy = along * d[1] + across * nrm[1]
            lv = shade_v(sx, sy)
            if u < a0:
                c = PURP[ramp_idx(lv)]
            else:
                t = (u - a0) / L
                s = (u - a0 - 0.12 * L - 0.6 * across * across) / band
                if t < 0.12 or t > 0.88 or frac(s) < 0.34:
                    ramp = PURP_D                  # franjas oscuras, primer segmento y punta
                else:
                    ramp = LIME
                c = ramp[ramp_idx(lv)]
            cv.put(x, y, c)
    # brillo frío sobre el lomo
    if hi:
        for u in (a0 + L * 0.22, a0 + L * 0.3):
            w = half(u)
            p = (ox + d[0] * u + nrm[0] * (vc(u) - w * 0.55), oy + d[1] * u + nrm[1] * (vc(u) - w * 0.55))
            q = (ox + d[0] * u + nrm[0] * (vc(u) + w * 0.55), oy + d[1] * u + nrm[1] * (vc(u) + w * 0.55))
            top = p if (p[1] < q[1] or (p[1] == q[1] and p[0] < q[0])) else q
            cv.put_in(top[0] - .5, top[1] - .5, LIME[3])
    # púa: aguja clara y brillante
    tip = None
    s = 0.0
    while s <= needle:
        u = end - 0.6 + s
        x, y = ox + d[0] * u + nrm[0] * vc(end), oy + d[1] * u + nrm[1] * vc(end)
        cv.put(x - .5, y - .5, '#ffffff' if s > needle - 1.2 else PURP[3] if s > 1 else PURP[2])
        tip = (x, y)
        s += 0.5
    if flash:
        tx, ty = tip
        for (dx, dy) in ((1, 0), (0, -1), (0, 1), (2, 0)):
            cv.put(tx - .5 + dx, ty - .5 + dy, '#ffffff')
    if drop > 0:
        # gota de veneno en forma de lágrima, colgando de la punta
        tx, ty = tip
        gx, gy = int(round(tx + d[0] * 1.2 - 0.5)), int(round(ty + d[1] * 1.2 + 0.3 + drop_dy))
        if drop >= 0.9:
            shape = ['.a.', 'abc', 'bcc', '.c.'] if drop < 1.08 else ['.a.', 'abc', 'bbc', 'bcc', '.c.']
        else:
            shape = ['a.', 'bc']
        cols = {'a': PURP[3], 'b': PURP[2], 'c': PURP[1]}
        for j, row in enumerate(shape):
            for i, ch in enumerate(row):
                if ch in cols:
                    cv.put(gx - 1 + i, gy + j, cols[ch])
        if drop >= 0.9:
            cv.put(gx - 1, gy + 1, '#ffffff')
    return tip


# Tamaño del Aguijón a 2/3 del primero (decisión del 15 de octubre): unos 14 px de la unión a la punta.
def stinger_mouth(curl=0.0, drop=0.8, drop_dy=0.0, L=9.4, wmax=3.3, needle=2.0, flash=False, waist=2.0):
    cv = Canvas(24)
    tip = abdomen(cv, 0.0, AY, 0.0, waist, L, wmax, curl, needle=needle, drop=drop, drop_dy=drop_dy, flash=flash)
    cv.outline()
    cv.tip = tip
    return cv


def stinger_anims():
    return {
        # el abdomen respira y se curva un poco; la gota tiembla
        'Idle': [stinger_mouth(0.0, 0.8, 0.0), stinger_mouth(0.35, 0.8, 0.5, wmax=3.4),
                 stinger_mouth(0.6, 0.85, 0.0, wmax=3.5), stinger_mouth(0.35, 0.8, -0.5, wmax=3.4)],
        # se contrae, estocada hacia delante (sale el proyectil) y vuelve
        'Attack': [stinger_mouth(-0.3, 0.5, 0.0, L=7.9, wmax=3.7, needle=1.7, waist=1.6),
                   stinger_mouth(0.0, 0.0, 0.0, L=10.1, wmax=3.0, needle=3.0, flash=True, waist=2.2),
                   stinger_mouth(0.2, 0.5, 0.0)],
    }


def projectile_stinger(f):
    """Púa de avispa suelta (12x6, centro 6,3): aguja morada, punta lima, estela de veneno."""
    cv = Cv(12, 6)
    for x in range(3, 9):
        cv.put(x, 2, PURP[2] if x > 3 else PURP[1])
    for x in range(3, 8):
        cv.put(x, 3, PURP[1] if x > 3 else PURP[0])
    cv.put(9, 2, LIME[2]); cv.put(10, 2, LIME[3]); cv.put(8, 3, LIME[1])
    cv.put(2 + f * 2 + 2, 2, '#ffffff')                   # brillo que recorre la púa
    cv.outline()
    # estela de veneno, translúcida y sin contorno
    for (x, y, a) in (((1, 3, 150), (0, 3, 80), (0, 2, 60)), ((1, 3, 120), (0, 2, 90), (1, 2, 50)),
                      ((1, 3, 140), (0, 3, 110), (1, 2, 70)))[f]:
        cv.put(x, y, CA(PURP[2], a))
    return cv


def icon_stinger(n):
    cv = Cv(n)
    k = n / 16
    ang = -math.pi / 4
    if n == 16:
        abdomen(cv, 1.6, 14.4, ang, 1.4, 9.0, 3.1, curl=0.2, needle=2.0, drop=0.7)
    else:
        abdomen(cv, 3.2, 28.8, ang, 2.8, 18.5, 5.8, curl=0.2, needle=3.5, drop=1.2)
    cv.outline()
    return cv


# ======================================================= 2. CAPARAZÓN NUEVO
CARA_R = M.CARA_R                     # (9, 7): mismo cuerpo, misma colisión y misma Boca a 8,5 px
SHELL_UP = [(0, -0.5), (0, 0), (0, 0.5), (-0.56, -0.26), (0.56, -0.26), (-0.56, 0.27), (0.56, 0.27)]
SHELL_SIDE = [(-0.5, -0.42), (0.1, -0.5), (-0.75, 0.25), (-0.2, 0.2), (0.45, 0.2)]
SHELL_DOWN = [(0, -0.62), (-0.62, -0.38), (0.62, -0.38), (-0.95, 0.12), (0.95, 0.12)]
CRACKS = [[(-0.1, -1.0), (0.05, -0.6), (-0.15, -0.3), (0.1, 0.05), (0.0, 0.35)],
          [(0.7, -0.6), (0.45, -0.3), (0.6, 0.0), (0.4, 0.3)]]


def shell_cols(pal):
    if pal is WHITE:
        return ARM[:3] + ['#ffffff'], '#ffffff'
    if pal is PALE:
        return ARM_PALE, ARM_PALE[3]
    return ARM, ARM[3]


def draw_shell(cv, scx, scy, srx, sry, seeds, pal, rot_a=0.0, lean=0.0, clip_y=None, rim=1.6, cracks=0,
               ticks=14):
    """Concha abombada: escudos (Voronoi de las semillas) con juntas oscuras y un
    anillo de escudos marginales claros en el borde. Luz arriba a la izquierda."""
    cols, rimc = shell_cols(pal)
    sd = []
    for (a, b) in seeds:
        x, y = rot(a, b, rot_a)
        sd.append((scx + x * srx, scy + y * sry))
    pix = []
    for y in range(cv.n):
        for x in range(cv.n):
            px, py = x + .5, y + .5
            t = (scy - py) / sry
            sx = px - scx - lean * max(0, t)
            nx, ny = sx / srx, (py - scy) / sry
            if nx * nx + ny * ny > 1:
                continue
            if clip_y is not None and py > clip_y:
                continue
            pix.append((x, y, sx, nx, ny))
    for (x, y, sx, nx, ny) in pix:
        r = math.hypot(nx, ny)
        edge = r > 1 - rim / min(srx, sry)
        if clip_y is not None and (y + .5) > clip_y - rim:
            edge = True
        v = shade_v(nx, ny)
        if edge:
            a = math.atan2(ny, nx) - rot_a
            seg = frac(a / (2 * math.pi) * ticks) if clip_y is None or (y + .5) <= clip_y - rim else frac((x + .5) / 3.2)
            if seg < 0.16:
                c = cols[0] if v < 0.4 else cols[1]
            else:
                c = rimc if v > 0.7 else cols[2] if v > -0.1 else cols[1]
            cv.px[(x, y)] = C(c) if isinstance(c, str) else c
            continue
        px, py = scx + sx, y + .5
        ds = sorted(math.hypot(px - a, py - b) for (a, b) in sd)
        gap = ds[1] - ds[0]
        if gap < 1.0:
            cv.px[(x, y)] = C(cols[0])                 # junta oscura entre escudos
            continue
        # cada escudo un poco abombado: más claro hacia su esquina de arriba a la izquierda
        near = min(sd, key=lambda s_: math.hypot(px - s_[0], py - s_[1]))
        lx, ly = (px - near[0]) / 2.6, (py - near[1]) / 2.6
        vv = v - 0.35 * (0.6 * lx + 0.8 * ly)
        i = 3 if vv > 0.85 else 2 if vv > 0.2 else 1
        if gap < 1.9 and i == 1 and v > 0.2:
            i = 2                                      # canto claro junto a la junta, del lado de la luz
        cv.px[(x, y)] = C(cols[i])
    for kk in range(cracks):
        pts = CRACKS[kk % len(CRACKS)]
        for (a0, b0), (a1, b1) in zip(pts, pts[1:]):
            for s in range(8):
                t = s / 7
                a, b = a0 + (a1 - a0) * t, b0 + (b1 - b0) * t
                xx, yy = scx + a * srx, scy + b * sry
                cv.put_in(xx - .5, yy - .5, '#1e2436')
                cv.put_in(xx + .5, yy - .5, cols[3] if s % 2 else cols[2])


def side_eye(cv, x, y, eyes):
    if eyes == 'closed':
        cv.put(x, y + 1, OUTL); cv.put(x + 1, y + 1, OUTL); return
    if eyes == 'x':
        for dx, dy in ((0, 0), (2, 0), (1, 1), (0, 2), (2, 2)):
            cv.put(x - 0.5 + dx, y + dy, OUTL)
        return
    for dx in range(2):
        for dy in range(3):
            cv.put(x + dx, y + dy, EYE)
    if eyes == 'wide':
        cv.put(x + 1, y + 1, OUTL); return
    cv.put(x + 1, y + 1, OUTL); cv.put(x + 1, y + 2, OUTL)


def turtle(facing='side', squash=0.0, bob=0, lean=0.0, pal=GREEN, angle=0.0, eyes='open', grow=0.0, n=24,
           cracks=0):
    """La célula con la concha de tortuga a la espalda. Mismo cuerpo (9x7 de radio)
    que el Caparazón anterior: colisión 0,55 y Boca a 8,5 px."""
    cv = Canvas(n)
    cx, cy, rx, ry = geom(n, *CARA_R, squash, bob, grow, maxr=n / 2 - 1.6)
    mem = pal
    if eyes == 'none':
        # metida en la concha: solo se ve la concha (vista desde arriba) girando
        draw_shell(cv, cx, cy, rx - 0.2, ry + 0.6, SHELL_UP, pal, rot_a=angle, cracks=cracks)
        for (a, b) in ((0, -1.05), (0.98, 0.3), (-0.98, 0.3)):     # la membrana asoma por los huecos
            x, y = rot(a * (rx - 0.2), b * (ry + 0.6), angle)
            cv.put(cx + x - .5, cy + y - .5, mem['ramp'][2])
        cv.outline()
        return cv
    if facing == 'down':
        draw_shell(cv, cx, cy - 1.7, min(rx + 1.0, n / 2 - 1.2), ry * 0.95, SHELL_DOWN, pal, lean=lean, cracks=cracks)
        fcx, fcy, frx, fry = cx, cy + 1.0, rx - 1.6, ry - 1.0
        body(cv, fcx, fcy, frx, fry, mem, lean)
        features(cv, fcx + lean * 0.5, fcy, frx * 1.08, fry, mem, 'down', 0.0, eyes)
    elif facing == 'up':
        body(cv, cx, cy + 0.9, rx - 0.9, ry, mem, lean)
        draw_shell(cv, cx, cy - 0.5, rx + 0.3, ry + 0.1, SHELL_UP, pal, lean=lean, cracks=cracks)
    else:
        body(cv, cx, cy + 0.4, rx, ry * 0.92, mem, lean)
        # cabeza por delante: brillo y ojo
        hx = cx + rx - 3.6 + lean * 0.3
        cv.put_in(hx + 1.5, cy - 2.2, mem['hi2'])
        draw_shell(cv, cx - 1.6 + lean * 0.4, cy - 0.7, rx - 2.2, ry - 0.2, SHELL_SIDE, pal,
                   lean=lean * 0.6, clip_y=cy + 0.3 * ry, rim=1.7, cracks=cracks)
        side_eye(cv, round(hx), round(cy - 2.2), eyes)
    cv.outline()
    return cv


def turtle_anims():
    a = M.skin_anims(lambda **k: turtle(**k))
    # muerte: la concha se agrieta y la célula se deshace (estallido de placas de generar_mutaciones)
    a['Death'] = M.skin_death(lambda *ar, **k: turtle(*ar, cracks=int(k.get('grow', 0)), **k), 'plate')
    return a


def icon_carapace(n):
    """La concha de tortuga vista desde arriba, con cabeza y aletas asomando."""
    cv = Cv(n)
    k = n / 16
    cx, cy = n / 2, n / 2 + 0.6 * k
    rx, ry = 5.9 * k, 5.0 * k
    for (a, b, r) in ((0, -1.12, 1.7), (-0.95, -0.72, 1.25), (0.95, -0.72, 1.25), (-0.92, 0.8, 1.1), (0.92, 0.8, 1.1)):
        ell(cv, cx + a * rx, cy + b * ry, r * k, r * k, GREEN['ramp'])
    sc = Canvas(n)
    draw_shell(sc, cx, cy, rx, ry, SHELL_UP, GREEN, rim=1.3 * k, ticks=12 if n == 16 else 18)
    for p, c in sc.px.items():
        cv.px[p] = c
    if n == 32:
        cv.put(cx - 1, cy - 1.12 * ry - 1, OUTL)               # ojitos de la cabeza
        cv.put(cx + 1, cy - 1.12 * ry - 1, OUTL)
    cv.put(cx - rx * 0.45, cy - ry * 0.55, '#ffffff')
    cv.outline()
    return cv


# ================================================ 3. EFECTOS DE EVOLUCIÓN
def layer(n, h=None):
    return Image.new('RGBA', (n, h or n), (0, 0, 0, 0))


def put_a(im, x, y, c, a=255):
    """Pone un píxel con alfa (sin contorno), sin salirse del lienzo."""
    x, y = int(math.floor(x)), int(math.floor(y))
    if 0 <= x < im.width and 0 <= y < im.height:
        rgb = C(c)[:3] if isinstance(c, str) else c[:3]
        old = im.getpixel((x, y))
        if old[3] <= a:
            im.putpixel((x, y), rgb + (int(a),))


def ellipse_ring(im, cx, cy, rx, ry, w, c, a, a_back=None):
    """Anillo elíptico de grosor w. a_back: alfa de la mitad de atrás (arriba)."""
    for y in range(im.height):
        for x in range(im.width):
            nx, ny = (x + .5 - cx) / rx, (y + .5 - cy) / ry
            d = math.hypot(nx, ny)
            if abs(d - 1) * min(rx, ry) <= w / 2:
                put_a(im, x, y, c, a if (a_back is None or y + .5 >= cy) else a_back)


def sprite_cv(n, h=None):
    return Cv(n, h)


def paste(im, cv, x=0, y=0):
    im.alpha_composite(cv.img() if not isinstance(cv, Image.Image) else cv, (x, y))


def cross(cv, x, y, big=True):
    """Crucecita de curación (roja con centro blanco)."""
    pts = [(0, -1), (-1, 0), (1, 0), (0, 1)]
    for dx, dy in pts:
        cv.put(x + dx, y + dy, '#e8404e')
    cv.put(x, y - 1, '#ff8a8a')
    cv.put(x, y, '#ffffff')


def bubble_ring(cv, x, y, r, c, hi='#ffffff'):
    for yy in range(int(y - r - 1), int(y + r + 2)):
        for xx in range(int(x - r - 1), int(x + r + 2)):
            d = math.hypot(xx + .5 - x, yy + .5 - y)
            if r - 1.0 <= d <= r:
                cv.put(xx, yy, c)
    cv.put(x - r * 0.45 - .5, y - r * 0.45 - .5, hi)


HEAL_GLOW = '#ffc070'


def heal(f):
    """Mandíbulas: curación al matar. Brillo cálido que envuelve la célula y
    crucecitas y burbujas rojas y blancas que suben. 24x24, centro (12, 12)."""
    im = layer(24)
    ring_a = (110, 190, 150, 100, 55, 0)[f]
    rr = (8.0, 9.0, 9.6, 10.0, 10.3, 10.5)[f]
    if ring_a:
        ellipse_ring(im, 12, 12.5, rr, rr * 0.86, 1.2, HEAL_GLOW, ring_a, ring_a * 0.6)
        if f in (1, 2):
            ellipse_ring(im, 12, 12.5, rr - 1.2, (rr - 1.2) * 0.86, 1.0, '#fff0c8', ring_a * 0.45)
    cv = sprite_cv(24)
    rise = [(-1, 0), (0, 0), (1, 2), (2, 4), (3, 6), (4, 8)][f]
    items = [('x', 5, 18, 1), ('x', 18, 16, 2), ('b', 9, 20, 1), ('x', 13, 21, 3), ('b', 16, 20, 3)]
    for kind, x, y, start in items:
        if f < start or f - start > 3:
            continue
        yy = y - (f - start) * 3.2
        if kind == 'x':
            cross(cv, x, yy)
        else:
            bubble_ring(cv, x, yy, 1.6, '#ff7a80')
    cv.outline()
    im.alpha_composite(cv.img())
    return im


def poison_burst(f):
    """Aguijón III (Contagio): nube de veneno de radio 2 (64x64, centro 32,32)."""
    n, c = 64, 32
    im = layer(n)
    cloud_r = (8, 16, 24, 29, 31, 31)[f]
    cloud_a = (220, 170, 120, 70, 0, 0)[f]
    random.seed(5)
    if cloud_a:
        for y in range(n):
            for x in range(n):
                dx, dy = x + .5 - c, y + .5 - c
                d = math.hypot(dx, dy)
                a = math.atan2(dy, dx)
                edge = cloud_r * (0.9 + 0.08 * math.sin(7 * a + f) + 0.05 * math.sin(3 * a - 1))
                if d <= edge:
                    w = math.sin(4 * a + d * 0.35 - f * 0.8)
                    if edge - d < 2.2:
                        put_a(im, x, y, LIME[1], cloud_a)
                    elif w > 0.45:
                        put_a(im, x, y, PURP[2], cloud_a * 0.8)
                    else:
                        put_a(im, x, y, PURP[1], cloud_a * 0.62)
    cv = sprite_cv(n)
    # salpicaduras: gotas lima y moradas que salen disparadas
    drops = [(k * math.pi / 5 + 0.2, 2.2 if k % 2 else 1.6, LIME if k % 3 else PURP) for k in range(10)]
    if f <= 3:
        rr = (11, 19, 26, 29)[f]
        for (a, r, ramp) in drops:
            sz = r * (1.0, 1.0, 0.8, 0.55)[f]
            x, y = c + math.cos(a) * rr, c + math.sin(a) * rr
            ell(cv, x, y, sz, sz, ramp)
            if f < 2:
                cv.put(x - math.cos(a) * (sz + 1.5), y - math.sin(a) * (sz + 1.5), ramp[2])
    if f == 0:
        ell(cv, c, c, 6.5, 6.0, LIME)
        ell(cv, c + 1.5, c + 1, 3.5, 3.2, PURP)
        cv.put(c - 3, c - 3, '#ffffff')
    if f >= 3:
        # se deshace en burbujas que suben
        for k in range(9):
            a = k * 2 * math.pi / 9 + 0.4
            rr = 12 + (k % 3) * 6
            up = (f - 3) * 4
            r = (2.2, 1.8, 1.2)[f - 3]
            if f == 5 and k % 2:
                continue
            bubble_ring(cv, c + math.cos(a) * rr, c + math.sin(a) * rr * 0.8 - up, r + (k % 2) * 0.6,
                        LIME[2] if k % 2 else PURP[2])
    cv.outline()
    im.alpha_composite(cv.img())
    return im


def ambush(f):
    """Aletas III (Emboscada): aura de depredador. 32x32, centro (16,16), centro libre ~16x16."""
    n, c = 32, 16
    im = layer(n)
    pulse = (0, 1, 0, -1)[f]
    ellipse_ring(im, c, c + 0.5, 9.2 + pulse * 0.3, 8.4 + pulse * 0.3, 1.3, '#ff5a3a', 150 + pulse * 30)
    ellipse_ring(im, c, c + 0.5, 10.6, 9.8, 1.0, FIN[1], 70)
    cv = sprite_cv(n)
    # estelas de velocidad que giran alrededor
    for k in range(4):
        a0 = k * math.pi / 2 + f * math.pi / 8 + 0.3
        for s in range(6):
            a = a0 - s * 0.11
            r = 12.6 - (k % 2) * 1.2
            x, y = c + math.cos(a) * r, c + 0.5 + math.sin(a) * r * 0.92
            if s == 0:
                cv.put(x - .5, y - .5, FIN[3])
            elif s < 3:
                cv.put(x - .5, y - .5, FIN[2])
            elif s < 5:
                cv.put(x - .5, y - .5, FIN[1])
    cv.outline()
    im.alpha_composite(cv.img())
    # chispas rojizas sueltas, sin contorno
    for k in range(3):
        a = k * 2.1 + f * 0.9
        put_a(im, c + math.cos(a) * 14.2, c + math.sin(a) * 13.5, '#ffd08a', 220)
    return im


SAND = ['#4a4438', '#7a7260', '#a89e86', '#d6ccb4']


def stomp(f):
    """Patas III (Pisotón): anillo de arena y polvo hasta el radio 2 (64x64, centro 32,32)."""
    n, c = 64, 32
    im = layer(n)
    r = (7, 14, 21, 26, 29.5, 31)[f]
    w = (7, 8, 7, 6, 4, 3)[f]
    a_max = (240, 230, 210, 170, 120, 70)[f]
    random.seed(9 + f)
    for y in range(n):
        for x in range(n):
            dx, dy = x + .5 - c, (y + .5 - c) * 1.0
            d = math.hypot(dx, dy)
            a = math.atan2(dy, dx)
            rr = r + 1.2 * math.sin(6 * a + f) + 0.8 * math.sin(11 * a)
            if rr - w <= d <= rr:
                t = (d - (rr - w)) / w                      # 0 dentro, 1 fuera
                noise = random.random()
                if noise < 0.08 + 0.4 * t:
                    continue                                # polvo deshilachado
                col_ = SAND[3] if (dy < 0 and t > 0.5) else SAND[2] if t > 0.4 else SAND[1]
                put_a(im, x, y, col_, a_max * (0.65 + 0.35 * t))
    if f == 0:
        ellipse_ring(im, c, c, 4, 4, 2, SAND[0], 200)
    cv = sprite_cv(n)
    # piedrecitas que salen despedidas
    for k in range(8):
        a = k * math.pi / 4 + 0.4
        rr = r + 2 + (k % 2) * 2
        if f >= 5 and k % 2:
            continue
        x, y = c + math.cos(a) * rr, c + math.sin(a) * rr - (2 if f in (1, 2) else 0)
        if not (2 <= x <= n - 3 and 2 <= y <= n - 3):
            continue
        cv.put(x, y, SAND[2]); cv.put(x + 1, y, SAND[1]); cv.put(x, y + 1, SAND[1])
        cv.put(x, y, SAND[3]) if k % 3 == 0 else None
    cv.outline()
    im.alpha_composite(cv.img())
    return im


PLATE_ICE = ['#5a7aa8', '#8ab0d8', '#c4e0ff', '#ffffff']


def plates(f):
    """Caparazón III (Placas): brillo frío que recorre el borde de la concha
    mientras el escudo está cargado. Muy sutil. 24x24, centro (12, 12)."""
    im = layer(24)
    ellipse_ring(im, 12, 12, 10.8, 8.8, 1.0, PLATE_ICE[2], 45)
    a0 = f * math.pi / 2 - 2.2
    for s in range(9):
        a = a0 + s * 0.09
        x, y = 12 + math.cos(a) * 10.8, 12 + math.sin(a) * 8.8
        put_a(im, x, y, PLATE_ICE[3] if s == 4 else PLATE_ICE[2], 210 - abs(s - 4) * 35)
    if f == 0:
        put_a(im, 4, 5, '#ffffff', 220)
    return im


def hexagon(cv, x, y, r, rot_a, cols, a=210):
    for yy in range(int(y - r - 1), int(y + r + 2)):
        for xx in range(int(x - r - 1), int(x + r + 2)):
            dx, dy = rot(xx + .5 - x, yy + .5 - y, -rot_a)
            # hexágono regular
            q = max(abs(dx) * 0.866 + abs(dy) * 0.5, abs(dy))
            if q <= r:
                edge = q > r - 1
                lit = (dx + dy) < 0
                c = cols[3] if (edge and lit) else cols[2] if edge else cols[1]
                cv.put(xx, yy, CA(c, a))


def plates_break(f):
    """Caparazón III: el escudo para un golpe y se rompe en placas translúcidas (32x32)."""
    n, c = 32, 16
    im = layer(n)
    cv = sprite_cv(n)
    alpha = (230, 220, 200, 160, 100)[f]
    r = (11.0, 11.5, 13.0, 14.0, 14.5)[f]
    for k in range(8):
        a = k * math.pi / 4 + 0.39
        if f >= 3 and k % 2 == 1 and f == 4:
            continue
        x, y = c + math.cos(a) * r, c + math.sin(a) * r * 0.85
        size = (2.6, 2.6, 2.2, 1.8, 1.4)[f]
        hexagon(cv, x, y, size, a + f * 0.6, PLATE_ICE, alpha)
    if f == 0:
        ellipse_ring(im, c, c, 11.5, 9.8, 1.0, PLATE_ICE[3], 200)
    if f == 1:                                         # grietas brillantes
        for k in range(4):
            a = k * math.pi / 2 + 0.8
            for s in range(3):
                put_a(im, c + math.cos(a) * (8 + s), c + math.sin(a) * (7 + s), '#ffffff', 220)
    cv.outline(170)
    im.alpha_composite(cv.img())
    return im


def quill(f):
    """Piel urticante III (Erizo): púa disparada (10x4, centro 5,2), morada con la punta lima."""
    cv = Cv(10, 4)
    for x in range(1, 7):
        cv.put(x, 1, PURP[2]); cv.put(x, 2, PURP[1])
    cv.put(7, 1, LIME[2]); cv.put(8, 1, LIME[3]); cv.put(7, 2, LIME[1])
    cv.put(1, 2, PURP[0])
    cv.put((3, 5)[f], 1, '#f0d8ff')
    cv.outline()
    return cv


# ---------------------------------------------------- tinta rehecha
INK = ['#1a0f30', '#2c1a50', '#45307a', '#6a52a8']
INK_A = (0.24, 0.40, 0.50, 0.58)                       # alfas cuantizados: nunca más del 60 %


def ink_cloud(R, phase, grow=1.0, fade=1.0, breakup=0.0, seed=3):
    """Tinta diluida en agua: volutas moradas translúcidas (35-45 % de media),
    un borde algo más denso para que se vea dónde acaba, y huecos casi transparentes."""
    n = 2 * R
    im = layer(n)
    c = R
    rnd = random.Random(seed)
    holes = [(rnd.uniform(-0.6, 0.6) * R, rnd.uniform(-0.6, 0.6) * R, rnd.uniform(0.12, 0.2) * R) for _ in range(5)]
    for y in range(n):
        for x in range(n):
            dx, dy = x + .5 - c, y + .5 - c
            d = math.hypot(dx, dy)
            a = math.atan2(dy, dx)
            edge = (R - 1.5) * grow * (0.88 + 0.06 * math.sin(5 * a + phase) + 0.04 * math.sin(9 * a - 2 * phase)
                                       + 0.02 * math.sin(13 * a + 3 * phase))
            if d > edge:
                continue
            u = d / max(edge, 1)
            # brazos de remolino que giran despacio
            w = math.sin(3 * a - phase + 9.0 * u + 1.6 * math.sin(2 * a + phase)) * 0.7 + \
                math.sin(5 * a + 2 * phase - 5.0 * u) * 0.3
            if edge - d < 1.2:
                lvl, col_ = 3, INK[3]                  # el filo, más claro: marca dónde acaba
            elif edge - d < 3.2:
                lvl, col_ = 3, INK[1]                  # banda densa junto al borde
            elif w > 0.55:
                lvl, col_ = 2, INK[2]                  # volutas
            elif w > 0.0:
                lvl, col_ = 1, INK[1]
            else:
                lvl, col_ = 0, INK[0]                  # zonas casi transparentes
            for (hx, hy, hr) in holes:
                if math.hypot(dx - hx * grow, dy - hy * grow) < hr * grow and edge - d > 3.2:
                    lvl = 0
            if breakup:
                v = (math.sin(dx * 0.37 + dy * 0.21 + phase) + math.sin(dy * 0.43 - dx * 0.17 + 2 * phase)) * 0.5
                if v + 1 < breakup * 2.0 * (0.6 + 0.4 * u):
                    continue
            put_a(im, x, y, col_, 255 * INK_A[lvl] * fade)
    return im


def ink_sets(R):
    tau = 2 * math.pi
    start = [ink_cloud(R, 0.0, g, 1.0) for g in (0.35, 0.62, 0.86, 1.0)]
    loop = [ink_cloud(R, tau * k / 6) for k in range(6)]
    end = [ink_cloud(R, tau * (k + 1) / 8, 1.0 + 0.03 * (k + 1), (0.9, 0.75, 0.55, 0.32)[k], (0.15, 0.35, 0.55, 0.75)[k])
           for k in range(4)]
    return start, loop, end


def shockwave_large(f):
    """Glándula de choque II y III: la misma onda que Shockwave.png a radio 6 (192x192)."""
    cv = FX['Canvas'](192)
    c = 96
    r, w = ((21, 8), (45, 8), (66, 7), (81, 6), (91, 4), (95, 3))[f]
    cols = ['#2a90c0', '#60d0ff', '#c0f4ff', '#ffffff'][: max(2, w - 2)] if f < 5 else ['#60d0ff', '#2a90c0']
    FX['ring'](cv, c, c, r, w, cols)
    random.seed(f)
    for k in range(16 + f * 3):
        a = random.random() * 2 * math.pi
        for t in range(4):
            cv.put(c + math.cos(a) * (r - w / 2 + t - 1.5), c + math.sin(a) * (r - w / 2 + (t % 2)), '#ffffff')
    if f == 0:
        FX['disc'](cv, c, c, 12, ['#2a90c0', '#60d0ff', '#c0f4ff', '#ffffff'])
    return cv.img()


# ======================================================= 4. MARCAS DE ESTADO
BLOOD = ['#6a1420', '#b82838', '#f05a64', '#ffc0c0']


def drop_shape(cv, x, y, ramp, big=True):
    rows = ['.a.', 'abc', 'bcc', '.c.'] if big else ['a', 'c']
    if not big:
        cv.put(x, y, ramp[2]); cv.put(x, y + 1, ramp[1]); return
    cols = {'a': ramp[2], 'b': ramp[1], 'c': ramp[0]}
    for j, row_ in enumerate(rows):
        for i, ch in enumerate(row_):
            if ch in cols:
                cv.put(x - 1 + i, y + j, cols[ch])
    cv.put(x - 1, y + 1, ramp[3])


def bleed(f):
    """Mandíbulas III (Desgarro): gotas rojas que caen y salpican (12x12)."""
    cv = Cv(12)
    # gota grande que cae y salpica abajo
    ya = (1, 3, 5, None)[f]
    if ya is not None:
        drop_shape(cv, 4, ya, BLOOD)
    else:
        for (x, y) in ((2, 9), (6, 9), (3, 8), (5, 8)):
            cv.put(x, y, BLOOD[2] if y == 8 else BLOOD[1])
        cv.put(4, 10, BLOOD[1]); cv.put(3, 10, BLOOD[0]); cv.put(5, 10, BLOOD[0])
    # gota pequeña desfasada
    yb = (6, None, 1, 3)[f]
    if yb is not None:
        drop_shape(cv, 8, yb, BLOOD)
    else:
        for (x, y) in ((7, 9), (10, 9), (8, 10), (9, 10)):
            cv.put(x, y, BLOOD[1])
        cv.put(8, 8, BLOOD[2])
    cv.outline()
    return cv


INKM = ['#1a0f30', '#3a2468', '#6a4aa8', '#b8a0e8']


def blind(f):
    """Vejiga de tinta: cegado. Un ojo tachado por un manchón de tinta que gotea (12x12)."""
    cv = Cv(12)
    # ojo (almendra blanca con pupila)
    for y in range(12):
        for x in range(12):
            nx, ny = (x + .5 - 6) / 4.6, (y + .5 - 4.5) / 2.4
            if nx * nx + ny * ny <= 1:
                cv.put(x, y, '#f4f4e8')
    cv.put(6, 4, OUTL); cv.put(5, 4, OUTL); cv.put(6, 5, OUTL)
    # manchón de tinta en diagonal que tacha el ojo
    for k in range(-5, 6):
        x, y = 6 + k * 0.9, 4.5 + k * 0.55
        for t in (-0.6, 0.6):
            cv.put(x, y + t, INKM[1] if t > 0 else INKM[2])
    cv.put(2, 2, INKM[2]); cv.put(10, 7, INKM[1])
    # goterones que bajan y caen
    L = (1, 2, 3, 1)[f]
    for s in range(L):
        cv.put(8, 7 + s, INKM[1])
    cv.put(8, 7 + L, INKM[2])
    if f == 3:
        cv.put(3, 9, INKM[1]); cv.put(3, 10, INKM[0])
    elif f in (0, 1):
        cv.put(3, 7 + f * 1, INKM[1]); cv.put(3, 8 + f * 1, INKM[0])
    cv.outline()
    return cv


STAR = ['#b8a040', '#f0e080', '#fff6c0', '#ffffff']


def stun(f):
    """Patas articuladas / Pisotón: aturdido. Estrellitas amarillo pálido que giran (12x12)."""
    cv = Cv(12)
    for k in range(3):
        a = k * 2 * math.pi / 3 + f * math.pi / 6
        x, y = 6 + math.cos(a) * 4.0, 6.5 + math.sin(a) * 2.6
        front = math.sin(a) > -0.2
        xi, yi = int(round(x - .5)), int(round(y - .5))
        if front:
            for dx, dy in ((0, -1), (-1, 0), (1, 0), (0, 1)):
                cv.put(xi + dx, yi + dy, STAR[1])
            cv.put(xi, yi, STAR[3])
        else:
            cv.put(xi, yi, STAR[2])
    cv.outline()
    return cv


def paralyzed(f):
    """Glándula de choque III: paralizado. Chispas eléctricas azul claro en zigzag (12x12)."""
    cv = Cv(12)
    bolts = ([(3, 1), (7, 4), (4, 6), (8, 10)], [(8, 1), (4, 4), (8, 6), (3, 10)],
             [(4, 1), (8, 3), (4, 7), (7, 10)], [(7, 1), (3, 5), (7, 6), (4, 10)])[f]
    for (x0, y0), (x1, y1) in zip(bolts, bolts[1:]):
        steps = max(abs(x1 - x0), abs(y1 - y0)) * 2
        for s in range(steps + 1):
            t = s / steps
            cv.put(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, SHOCK[2])
    x, y = bolts[1]
    cv.put(x, y, '#ffffff')
    for (x, y) in (((1, 6), (10, 3)), ((10, 7), (1, 2)), ((1, 9), (10, 5)), ((10, 9), (2, 3)))[f]:
        cv.put(x, y, SHOCK[3])
    cv.outline()
    return cv


def paralyzed_body(f):
    """Rayitos azules que recorren el contorno del enemigo paralizado (32x32, hueco ~20x20)."""
    cv = Cv(32)
    c = 16
    rnd = random.Random(40 + f)
    for k in range(5):
        a0 = k * 2 * math.pi / 5 + f * 0.7 + rnd.uniform(-0.3, 0.3)
        pts = []
        for s in range(5):
            a = a0 + s * 0.16
            r = 11.6 + (1.6 if s % 2 else 0) - (0.6 if k % 2 else 0)
            pts.append((c + math.cos(a) * r, c + math.sin(a) * r))
        for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
            for s in range(5):
                t = s / 4
                cv.put(x0 + (x1 - x0) * t - .5, y0 + (y1 - y0) * t - .5, SHOCK[2])
        cv.put(pts[2][0] - .5, pts[2][1] - .5, '#ffffff')
    cv.outline()
    return cv


# ===================================================== 5. INSIGNIAS DE NIVEL
PILL = '#22303a'
DOT = ['#4a9078', '#9ae0b8', '#e4ffd8']                 # puntos de nivel I y II (membrana clara)
GEM = [GOLD[1], GOLD[2], '#fffbe8']                     # nivel III: gemas doradas


def pill(w, h):
    cv = Cv(w, h)
    for y in range(h):
        for x in range(w):
            corner = (x in (0, w - 1)) and (y in (0, h - 1))
            if corner:
                continue
            edge = x in (0, w - 1) or y in (0, h - 1)
            cv.put(x, y, OUTL if edge else PILL)
    return cv


def level_badge(level, large=False):
    cols = GEM if level == 3 else DOT
    if not large:
        cv = pill(9, 5)
        xs = {1: [4], 2: [2, 5], 3: [1, 3, 5]}[level]
        for x in xs:
            cv.put(x, 1, cols[2]); cv.put(x + 1, 1, cols[1])
            cv.put(x, 2, cols[1]); cv.put(x + 1, 2, cols[0])
        if level == 3:
            cv.put(7, 1, '#ffffff')                    # destello
            for x in range(1, 8):
                cv.put(x, 3, '#3a3020')                # base cálida bajo las gemas
        return cv
    cv = pill(15, 7)
    xs = {1: [6], 2: [4, 8], 3: [2, 6, 10]}[level]
    for x in xs:
        if level == 3:                                 # gema tallada (rombo)
            cv.put(x + 1, 2, cols[2]); cv.put(x, 3, cols[1]); cv.put(x + 1, 3, '#ffffff')
            cv.put(x + 2, 3, cols[1]); cv.put(x + 1, 4, cols[0])
            cv.put(x, 2, GOLD[0]); cv.put(x + 2, 2, GOLD[1]); cv.put(x, 4, GOLD[0]); cv.put(x + 2, 4, GOLD[0])
        else:
            for dy in range(3):
                for dx in range(3):
                    v = dx + dy
                    cv.put(x + dx, 2 + dy, cols[2] if v == 0 else cols[1] if v < 3 else cols[0])
    if level == 3:
        cv.put(13, 1, GOLD[3])
        for x in range(1, 14):
            cv.put(x, 5, '#3a3020')
    return cv


LEVELUP = ['#86bc2c', '#c0f040', '#eaff9a', '#ffffff']


def level_up(f, ramp=LEVELUP):
    """Subida de nivel: anillo de luz que sube por la célula y se cierra con chispas (32x32)."""
    n, c = 32, 16
    im = layer(n)
    ring = [(25, 11, 3.6), (20, 11, 3.4), (14, 10, 3.0), (9, 6.5, 2.2), None, None][f]
    if ring:
        y, rx, ry = ring
        ellipse_ring(im, c, y, rx, ry, 1.4, ramp[2], 255, 120)
        ellipse_ring(im, c, y, rx + 1.2, ry + 0.8, 1.0, ramp[1], 120, 50)
        # columnas de luz que suben desde el anillo
        for k in (-7, -3, 3, 7):
            for s in range(6 if f < 3 else 3):
                put_a(im, c + k * rx / 11, y - 1 - s, ramp[3], 140 - s * 22)
    cv = sprite_cv(n)
    if f == 4:                                          # se cierra en un destello
        x, y = 16, 6
        for d in range(1, 4):
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                cv.put(x + dx * d - .5, y + dy * d - .5, ramp[2] if d < 3 else ramp[1])
        cv.put(x - .5, y - .5, '#ffffff')
    if f >= 3:
        rnd = random.Random(7)
        for k in range(8):
            a = k * math.pi / 4 + 0.3
            rr = (0, 0, 0, 5, 8, 11)[f]
            x, y = 16 + math.cos(a) * rr, 6 + math.sin(a) * rr * 0.7 + (f - 4) * 3
            if f == 5 and k % 2:
                continue
            cv.put(x - .5, y - .5, ramp[3] if k % 2 else ramp[2])
    cv.outline()
    im.alpha_composite(cv.img())
    return im


# ================================================================ GUARDAR
SHEETS = {}


def as_img(x):
    return x if isinstance(x, Image.Image) else x.img()


def soft_edge(im, rows=3):
    """Nada toca el borde del lienzo: el borde se vacía y las dos filas siguientes se desvanecen."""
    im = im.copy()
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            d = min(x, y, w - 1 - x, h - 1 - y)
            if d < rows and px[x, y][3]:
                r, g, b, a = px[x, y]
                px[x, y] = (r, g, b, (0, a // 2, a * 4 // 5)[d])
    return im


def save(group, name, frames, ms=None, loop=False, center=None, note=''):
    frames = [as_img(f) for f in frames]
    if name.startswith(('InkCloud', 'Stomp')):           # volutas y polvo que llegan al borde
        frames = [soft_edge(f) for f in frames]
    elif name.startswith(('LevelUp', 'Stun')):           # una chispa suelta en el borde
        frames = [soft_edge(f, 1) for f in frames]
    w, h = frames[0].size
    os.makedirs(os.path.join(OUT, group), exist_ok=True)
    sh = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, im in enumerate(frames):
        sh.paste(im, (i * w, 0))
    sh.save(os.path.join(OUT, group, name + '.png'))
    if len(frames) > 1:
        os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
        sc = max(1, min(10, 240 // max(w, h)))
        out = []
        for im in frames:
            b = Image.new('RGBA', (w, h), BG)
            b.alpha_composite(im)
            out.append(b.resize((w * sc, h * sc), Image.NEAREST).convert('RGB'))
        out[0].save(os.path.join(OUT, 'gif', name + '.gif'), save_all=True, append_images=out[1:],
                    duration=ms or 100, loop=0)
    SHEETS[name] = dict(group=group, frames=frames, w=w, h=h, ms=ms, loop=loop,
                        center=center or (w // 2, h // 2), note=note)


def build():
    st = stinger_anims()
    save('Mouth', 'Mouth_Stinger_Idle', st['Idle'], 180, True, (0, 12), 'unión en el borde izquierdo a media altura')
    save('Mouth', 'Mouth_Stinger_Attack', st['Attack'], 70, False, (0, 12), 'el proyectil sale en el cuadro 1')
    ta = turtle_anims()
    for name in ('Idle_down', 'Idle_side', 'Idle_up', 'Move_down', 'Move_side', 'Move_up',
                 'Dodge_down', 'Dodge_side', 'Dodge_up', 'Hurt', 'Death'):
        anim = name.split('_')[0]
        ms = M.MS[anim]
        save('Skin', 'Skin_Carapace_' + name, ta[name], ms, anim in ('Idle', 'Move'),
             (16, 16) if name == 'Death' else (12, 12))
    save('Icons', 'Icon_Stinger_16', [icon_stinger(16)])
    save('Icons', 'Icon_Stinger_32', [icon_stinger(32)])
    save('Icons', 'Icon_Carapace_16', [icon_carapace(16)])
    save('Icons', 'Icon_Carapace_32', [icon_carapace(32)])
    save('Projectiles', 'Projectile_Stinger', [projectile_stinger(f) for f in range(3)], 90, True, (6, 3))
    save('Projectiles', 'Projectile_Quill', [quill(f) for f in range(2)], 80, True, (5, 2))
    save('Effects', 'Heal', [heal(f) for f in range(6)], 70, False, (12, 12))
    save('Effects', 'PoisonBurst', [poison_burst(f) for f in range(6)], 70, False, (32, 32), 'radio 2')
    save('Effects', 'Ambush', [ambush(f) for f in range(4)], 90, True, (16, 16))
    save('Effects', 'Stomp', [stomp(f) for f in range(6)], 60, False, (32, 32), 'radio 2')
    save('Effects', 'Plates', [plates(f) for f in range(4)], 140, True, (12, 12))
    save('Effects', 'PlatesBreak', [plates_break(f) for f in range(5)], 70, False, (16, 16))
    for R, suffix in ((48, ''), (64, '_Large')):
        s, l, e = ink_sets(R)
        save('Effects', f'InkCloud{suffix}_Start', s, 90, False, (R, R), f'radio {R // 16}')
        save('Effects', f'InkCloud{suffix}_Loop', l, 150, True, (R, R), f'radio {R // 16}')
        save('Effects', f'InkCloud{suffix}_End', e, 110, False, (R, R), f'radio {R // 16}')
    save('Effects', 'Shockwave_Large', [shockwave_large(f) for f in range(6)], 60, False, (96, 96), 'radio 6')
    save('Effects', 'LevelUp', [level_up(f) for f in range(6)], 70, False, (16, 16), 'niveles I y II')
    save('Effects', 'LevelUp_Gold', [level_up(f, GOLD) for f in range(6)], 70, False, (16, 16),
         'extra: la misma, dorada, para el nivel III')
    save('Status', 'Bleed', [bleed(f) for f in range(4)], 120, True, (6, 6))
    save('Status', 'Blind', [blind(f) for f in range(4)], 140, True, (6, 6))
    save('Status', 'Stun', [stun(f) for f in range(4)], 110, True, (6, 6))
    save('Status', 'Paralyzed', [paralyzed(f) for f in range(4)], 70, True, (6, 6))
    save('Status', 'Paralyzed_Body', [paralyzed_body(f) for f in range(4)], 70, True, (16, 16))
    for lv in (1, 2, 3):
        save('UI', f'UI_Level_{lv}', [level_badge(lv)])
    for lv in (1, 2, 3):
        save('UI', f'UI_Level_Large_{lv}', [level_badge(lv, True)])
    save('UI', 'UI_Level_Up', [G.stat_arrow(True)], center=(3, 3))


def fr(name, i=0):
    return SHEETS[name]['frames'][i]


# ============================================================ VISTA PREVIA
W, H = 480, 270


def load(rel, w=None, i=0):
    p = os.path.join(ROOT, rel)
    im = Image.open(p).convert('RGBA')
    h = im.height
    w = w or h
    return im.crop((i * w, 0, i * w + w, h))


def at(scr, im, cx, cy):
    """Pega im con su centro en (cx, cy)."""
    scr.alpha_composite(im, (int(round(cx - im.width / 2)), int(round(cy - im.height / 2))))


def with_mouth(cell, mouth, direction, edge, cell_c=None):
    """Monta la célula (centro en su lienzo) con una Boca unida en (0, 12), en 64x64."""
    out = layer(64)
    c = 32
    cc = cell_c or (cell.width // 2, cell.height // 2)
    if direction in ('right', 'left'):
        m = mouth
        pos = (c + int(edge), c - 12)
    elif direction == 'down':
        m = mouth.rotate(-90, expand=True)
        pos = (c - 12, c + int(edge))
    else:
        m = mouth.rotate(90, expand=True)
        pos = (c - 12, c - int(edge) - 24)
    if direction == 'up':
        out.alpha_composite(m, pos)
    out.alpha_composite(cell, (c - cc[0], c - cc[1]))
    if direction != 'up':
        out.alpha_composite(m, pos)
    if direction == 'left':
        out = out.transpose(Image.FLIP_LEFT_RIGHT)
    return out


def sec_stinger():
    scr = Image.new('RGBA', (W, 118), BG)
    cells = {'right': 'Idle_side', 'down': 'Idle_down', 'left': 'Idle_side', 'up': 'Idle_up'}
    for i, d in enumerate(('right', 'down', 'left', 'up')):
        cell = M.PANIMS[cells[d]][0].img()
        scr.alpha_composite(with_mouth(cell, fr('Mouth_Stinger_Idle', i % 4), d, M.SKIN_EDGE[None]), (i * 60, 0))
    for i in range(3):
        cell = M.PANIMS['Idle_side'][0].img()
        scr.alpha_composite(with_mouth(cell, fr('Mouth_Stinger_Attack', i), 'right', M.SKIN_EDGE[None]),
                            (250 + i * 58, 0))
    # proyectil, iconos nuevos y antiguos
    for i in range(3):
        at(scr, fr('Projectile_Stinger', i), 20 + i * 18, 82)
    G.ptext(scr, (8, 104), 'PROYECTIL', (190, 220, 205))
    x = 84
    for im in (fr('Icon_Stinger_16'), U.ICONS['Stinger'](16).img(), U.ICONS['Mandibles'](16).img(),
               U.ICONS['Spores'](16).img()):
        scr.alpha_composite(im, (x, 74)); x += 22
    x += 8
    for im in (fr('Icon_Stinger_32'), U.ICONS['Stinger'](32).img(), U.ICONS['Mandibles'](32).img(),
               U.ICONS['Spores'](32).img()):
        scr.alpha_composite(im, (x, 66)); x += 38
    G.ptext(scr, (84, 104), 'NUEVO  ANTES  MANDIBULAS  ESPORAS', (190, 220, 205))
    return scr


def sec_carapace():
    scr = Image.new('RGBA', (W, 150), BG)
    x = 4
    for d in ('down', 'side', 'up'):
        for i in range(4):
            scr.alpha_composite(fr(f'Skin_Carapace_Idle_{d}', i), (x, 4)); x += 26
        x += 6
    scr.alpha_composite(fr('Icon_Carapace_16'), (x + 4, 8))
    scr.alpha_composite(fr('Icon_Carapace_32'), (x + 26, 0))
    scr.alpha_composite(U.ICONS['Carapace'](32).img(), (x + 62, 0))
    G.ptext(scr, (x + 26, 34), 'NUEVO  ANTES', (190, 220, 205))
    mouths = M.build_mouths()
    e = M.SKIN_EDGE['Carapace']
    x = 0
    for i in range(3):
        scr.alpha_composite(with_mouth(fr('Skin_Carapace_Idle_side', 0), mouths['Mandibles']['Attack'][i].img(),
                                       'right', e), (x, 40)); x += 60
    scr.alpha_composite(with_mouth(fr('Skin_Carapace_Idle_down', 0), mouths['Mandibles']['Idle'][0].img(), 'down', e),
                        (x, 40)); x += 60
    scr.alpha_composite(with_mouth(fr('Skin_Carapace_Idle_up', 0), mouths['Mandibles']['Idle'][0].img(), 'up', e),
                        (x, 40)); x += 64
    for i in range(5):
        scr.alpha_composite(fr('Skin_Carapace_Dodge_side', i), (x + i * 26, 60))
    G.ptext(scr, (x, 88), 'ESQUIVA', (190, 220, 205))
    G.ptext(scr, (4, 108), 'MANDIBULAS A 8,5 PX: ATAQUE DE LADO, ABAJO Y ARRIBA', (190, 220, 205))
    x = 4
    for i in range(7):
        scr.alpha_composite(fr('Skin_Carapace_Death', i), (x + i * 34, 116))
    for i in range(2):
        scr.alpha_composite(fr('Skin_Carapace_Hurt', i), (x + 244 + i * 26, 120))
    for i in range(4):
        scr.alpha_composite(fr('Skin_Carapace_Move_side', i), (x + 310 + i * 26, 120))
    return scr


def cell_img(name='Idle_down', i=0):
    return M.PANIMS[name][i].img()


def sec_effects():
    scr = Image.new('RGBA', (W, 470), BG)
    bac = load('enemies/Bacteria/Bacteria_Move.png', 16)
    ame = load('enemies/Ameba/Ameba_Move.png', 16)
    y = 2
    for i in range(6):                                   # curación
        at(scr, cell_img(), 16 + i * 28, y + 14); at(scr, fr('Heal', i), 16 + i * 28, y + 14)
    for i in range(4):                                   # emboscada
        at(scr, cell_img('Idle_side'), 190 + i * 36, y + 16); at(scr, fr('Ambush', i), 190 + i * 36, y + 16)
    for i in range(4):                                   # placas
        at(scr, fr('Skin_Carapace_Idle_up', i), 346 + i * 32, y + 14); at(scr, fr('Plates', i), 346 + i * 32, y + 14)
    y = 40
    for i in range(5):                                   # rotura de placas
        at(scr, fr('Skin_Carapace_Idle_side', 0), 20 + i * 36, y + 18)
        at(scr, fr('PlatesBreak', i), 20 + i * 36, y + 18)
    # erizo: 8 púas saliendo en círculo
    for k, rr in enumerate((10, 18)):
        cx = 230 + k * 70
        at(scr, cell_img(), cx, y + 18)
        for j in range(8):
            a = j * math.pi / 4
            q = fr('Projectile_Quill', k).rotate(-math.degrees(a), expand=True)
            at(scr, q, cx + math.cos(a) * rr, y + 18 + math.sin(a) * rr)
    G.ptext(scr, (370, y + 14), 'ERIZO', (190, 220, 205))
    y = 82                                               # estallido de veneno junto a 3 amebas
    for i in range(6):
        cx = 34 + i * 70
        for (dx, dy) in ((-18, -10), (16, -14), (4, 18)):
            at(scr, ame, cx + dx, y + 32 + dy)
        at(scr, fr('PoisonBurst', i), cx, y + 32)
    y = 152                                              # pisotón
    for i in range(6):
        cx = 34 + i * 70
        for (dx, dy) in ((-22, 8), (20, -12)):
            at(scr, bac, cx + dx, y + 32 + dy)
        at(scr, cell_img(), cx, y + 32)
        at(scr, fr('Stomp', i), cx, y + 32)
    y = 222                                              # onda normal y grande
    old = load('effects/Shockwave.png', 128, 3)
    at(scr, cell_img(), 70, y + 120); at(scr, old, 70, y + 120)
    at(scr, cell_img(), 300, y + 120); at(scr, fr('Shockwave_Large', 3), 300, y + 120)
    G.ptext(scr, (40, y + 196), 'ONDA (RADIO 4)', (190, 220, 205))
    G.ptext(scr, (250, y + 220), 'ONDA GRANDE (RADIO 6)', (190, 220, 205))
    return scr


def ink_panel(w, h, cloud, seed):
    p = U.floor_bg(w, h)
    bac = load('enemies/Bacteria/Bacteria_Move.png', 16)
    shot = load('projectiles/Projectile_Enemy.png', 10)
    c = (w // 2, h // 2)
    at(p, bac, c[0] - 26, c[1] - 18); at(p, load('enemies/Bacteria/Bacteria_Move.png', 16, 2), c[0] + 24, c[1] + 16)
    at(p, shot, c[0] + 14, c[1] - 22); at(p, shot, c[0] - 18, c[1] + 22)
    at(p, cell_img('Idle_side'), c[0], c[1])
    at(p, cloud, c[0], c[1])
    return p


def sec_ink():
    scr = Image.new('RGBA', (W, 170), BG)
    old = load('effects/InkCloud.png', 96, 4)
    scr.alpha_composite(ink_panel(140, 140, old, 1), (4, 4))
    scr.alpha_composite(ink_panel(140, 140, fr('InkCloud_Loop', 0), 2), (150, 4))
    scr.alpha_composite(ink_panel(180, 150, fr('InkCloud_Large_Loop', 2), 3), (296, 4))
    G.ptext(scr, (8, 150), 'ANTES', (190, 220, 205))
    G.ptext(scr, (154, 150), 'NUEVA (RADIO 3)', (190, 220, 205))
    G.ptext(scr, (300, 158), 'NUEVA GRANDE (RADIO 4)', (190, 220, 205))
    return scr


def sec_status():
    scr = Image.new('RGBA', (W, 170), BG)
    poison = load('effects/Poison.png', 12)
    marks = [('Bleed', fr('Bleed', 1)), ('Blind', fr('Blind', 1)), ('Stun', fr('Stun', 1)),
             ('Paralyzed', fr('Paralyzed', 1)), ('Poison', poison)]
    enemies = [(load('enemies/Bacteria/Bacteria_Move.png', 16), 30),
               (load('enemies/Ameba/Ameba_Move.png', 16), 30),
               (load('enemies/BossVirus/BossVirus_Idle_P1.png', 48), 70)]
    y = 4
    for im, rowh in enemies:
        for j, (name, mk) in enumerate(marks):
            cx = 30 + j * 64
            cy = y + rowh - im.height // 2 - 2
            at(scr, im, cx, cy)
            if name == 'Paralyzed':
                at(scr, fr('Paralyzed_Body', 1), cx, cy)
            at(scr, mk, cx, cy - im.height // 2 - 7)
        y += rowh + 8
    # bacteria con sangrado y aturdimiento a la vez
    bac = enemies[0][0]
    cx, cy = 360, 30
    at(scr, bac, cx, cy)
    at(scr, fr('Bleed', 2), cx - 7, cy - 15)
    at(scr, fr('Stun', 2), cx + 7, cy - 15)
    G.ptext(scr, (330, 46), 'DOS A LA VEZ', (190, 220, 205))
    for j, lab in enumerate(('SANGRADO', 'CEGADO', 'ATURDIDO', 'PARALIZADO', 'VENENO')):
        G.ptext(scr, (30 + j * 64 - G.tw(lab) // 2, 160), lab, (190, 220, 205))
    return scr


def sec_badges():
    scr = Image.new('RGBA', (W, 140), BG)
    order = ['Mandibles', 'Fins', 'InkSac', 'Stinger', 'Spores', 'StingingSkin', 'JointedLegs', 'Carapace',
             'ShockGland']

    def icon(m, n):
        if m == 'Stinger':
            return fr(f'Icon_Stinger_{n}')
        if m == 'Carapace':
            return fr(f'Icon_Carapace_{n}')
        return U.ICONS[m](n).img()
    for lv in (1, 2, 3):
        for i, m in enumerate(order):
            x, y = 4 + i * 23, 4 + (lv - 1) * 24
            scr.alpha_composite(U.slot_frame().img(), (x, y))
            scr.alpha_composite(icon(m, 16), (x + 2, y + 2))
            scr.alpha_composite(fr(f'UI_Level_{lv}'), (x + 2 + 8, y + 2 + 12))
    card = U.card('Normal').img()
    for lv in (1, 2, 3):
        x = 220 + (lv - 1) * 54
        scr.alpha_composite(U.nine_render(card, 46, 46, 12), (x, 4))
        m = ('Carapace', 'Stinger', 'Fins')[lv - 1]
        scr.alpha_composite(icon(m, 32), (x + 7, 8))
        scr.alpha_composite(fr(f'UI_Level_Large_{lv}'), (x + 7 + 18, 8 + 26))
    # flecha de mejora junto a "NIVEL I > II"
    scr.alpha_composite(fr('UI_Level_Up'), (384, 14))
    G.ptext(scr, (394, 15), 'NIVEL I - II', (230, 245, 230))
    scr.alpha_composite(fr('UI_Level_Up'), (384, 30))
    G.ptext(scr, (394, 31), 'NIVEL II - III', (255, 220, 120))
    for i in range(6):
        at(scr, cell_img(), 20 + i * 36, 100); at(scr, fr('LevelUp', i), 20 + i * 36, 100)
        at(scr, cell_img(), 244 + i * 36, 100); at(scr, fr('LevelUp_Gold', i), 244 + i * 36, 100)
    return scr


def preview(path):
    S = 3
    secs = [('1. AGUIJON NUEVO: 4 DIRECCIONES, ATAQUE, PROYECTIL E ICONOS', sec_stinger()),
            ('2. CAPARAZON NUEVO: REPOSO, BOCA A 8,5 PX, ESQUIVA, GOLPE, MUERTE E ICONOS', sec_carapace()),
            ('3. EFECTOS: CURACION, EMBOSCADA, PLACAS, ERIZO, VENENO, PISOTON Y ONDAS', sec_effects()),
            ('4. TINTA: ANTES Y AHORA', sec_ink()),
            ('5. MARCAS DE ESTADO SOBRE BACTERIA, AMEBA Y VIRUS', sec_status()),
            ('6. INSIGNIAS DE NIVEL, FLECHA DE MEJORA Y SUBIDA DE NIVEL', sec_badges())]
    lab = 26
    out = Image.new('RGBA', (W * S + 20, sum(im.height * S + lab + 10 for _, im in secs) + 10), BG)
    y = 10
    for name, im in secs:
        G.ptext(out, (12, y + 6), name, (214, 255, 154), scale=2)
        y += lab
        out.alpha_composite(im.resize((im.width * S, im.height * S), Image.NEAREST), (10, y))
        y += im.height * S + 10
    out.convert('RGB').save(path)


def main():
    build()
    preview(os.path.join(OUT, 'vista_previa_evoluciones.png'))
    for k, v in SHEETS.items():
        print(v['group'], k, len(v['frames']), f"{v['w']}x{v['h']}", v['center'], v['ms'], v['loop'])


if __name__ == '__main__':
    main()
