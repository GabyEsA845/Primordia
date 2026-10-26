"""Presentación de los jefes (estilo Enter the Gungeon): retrato del rótulo y pose de entrada de cada jefe.

Uso: python generar_presentaciones.py   (escribe junto a este script)

Salida:
  Portraits/Portrait_<Jefe>.png     retrato del rótulo, 128x128, misma escala de píxel que el juego
  Poses/<Jefe>_Intro.png            pose de presentación (hoja en tira, celda del jefe)
  vista_previa_<jefe>.png           retrato + pose ampliados
  maqueta_rotulo_<jefe>.png         el rótulo sobre la pantalla de 480x270, ampliado x2
  gif/*.gif                         la pose en movimiento
"""
import math, os, sys
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
TEMP = os.path.normpath(os.path.join(HERE, '..'))
ROOT = os.path.normpath(os.path.join(TEMP, '..', '..'))
FONTS = os.path.join(ROOT, 'Assets', '_Primordia', 'Art', 'Fonts')
sys.path.insert(0, os.path.join(TEMP, 'floor2-bosses'))
from base_piso2 import *


def load_defs(path, stop):
    """Carga las funciones de dibujo de un generador sin ejecutar su parte de guardado."""
    src = open(path, encoding='utf-8').read()
    src = src[:src.index(stop)]
    ns = {'__file__': path}
    old = sys.argv; sys.argv = [path, os.path.join(HERE, '_tmp')]
    try:
        exec(compile(src, path, 'exec'), ns)
    finally:
        sys.argv = old
    return ns


N = 128
PN = 176   # retrato: ~1/4 de la pantalla de 480x270

# =====================================================================================
# Anomalocaris
# =====================================================================================
AN = load_defs(os.path.join(TEMP, 'floor2-bosses', 'generar_anomalocaris.py'), 'anims = {}')
A_BODY, A_FLAP, A_APP, A_EYEB = AN['BODY'], AN['FLAP'], AN['APP'], AN['EYEB']
IRIS = ['#6a8a28', '#b8e050', '#e8ff9a', '#ffffe0']
CONE = ['#5a2a22', '#a8624a', '#dca07a', '#fff0d0']
GLOW = AN['GLOW']


def chain(cv, x, y, a, turn, segs, r0, r1, step, ramp):
    """Cadena de segmentos que gira poco a poco (apéndices, pedúnculos). Devuelve (x, y, r, ángulo) de cada uno."""
    pts = []
    for k in range(segs):
        t = k / (segs - 1)
        r = r0 + (r1 - r0) * t
        pts.append((x, y, r, a))
        a += turn * (0.3 + 1.7 * t * t)
        x += math.cos(a) * step; y += math.sin(a) * step
    for i, (px, py, r, _) in enumerate(pts):
        ellipse(cv, px, py, r, r, ramp, shift=-1 if i % 2 else 0)
    return pts


def portrait_anomalocaris(n=PN, open_=0.0):
    """De frente, viniendo hacia ti: aletas abiertas como alas, ojos en pedúnculo, boca redonda de placas
    y los dos apéndices que bajan, se abren y se enroscan hacia la boca como unas tenazas.
    Dibujado sobre una rejilla de 128 que se escala a n (las medidas de abajo son de la rejilla de 128)."""
    k = n / 128
    def q(v): return v * k
    cv = Canvas(n)
    mx = n / 2
    # aletas: un abanico de lóbulos a cada lado, de abajo (detrás) hacia arriba (delante)
    for s in (-1, 1):
        for j in range(4, -1, -1):
            th = -0.45 + j * 0.32
            fx, fy = mx + s * q(27 + 13 * math.cos(th)), q(64 + 15 * math.sin(th))
            rot_ellipse(cv, fx, fy, q(11.5 - j * 0.6), q(4.6), th if s > 0 else math.pi - th, A_FLAP,
                        shift=-1 if j % 2 else 0)
    for s in (-1, 1):                                          # pedúnculos gruesos
        chain(cv, mx + s * q(12), q(50), math.atan2(-20, s * 23), 0, 10, q(4.4), q(3.4), q(3.0), A_BODY)
    ellipse(cv, mx, q(66), q(27), q(22), A_BODY)                # cabeza
    ellipse(cv, mx, q(55), q(19), q(11), A_BODY, shift=1)       # placa dorsal
    for y in range(int(q(40)), int(q(70))):
        for x in range(int(q(40)), int(q(90))):
            e = ((x + .5 - mx) / q(19)) ** 2 + ((y + .5 - q(55)) / q(11)) ** 2
            if 1 - 1.7 / q(11) < e <= 1: cv.put(x, y, C(A_BODY[1]))
    for j in range(-2, 3):                                      # surcos de la placa
        x = mx + q(j * 7)
        for y in range(int(q(47)), int(q(61))): cv.put_in(x, y + q(abs(j)), C(A_BODY[1]))
    # boca: anillo de placas con el centro oscuro y dientes hacia dentro
    for i in range(14):
        a = i * math.tau / 14
        ellipse(cv, mx + math.cos(a) * q(9), q(80) + math.sin(a) * q(7), q(3.4), q(3.0), CONE, shift=0 if i % 2 else -1)
    ellipse(cv, mx, q(80), q(5.6), q(4.4), ['#1a0c14', '#2a1420', '#3a1c28', '#4a2432'])
    for i in range(10):
        a = i * math.tau / 10
        cv.put(mx + math.cos(a) * q(4.3), q(80) + math.sin(a) * q(3.2), C(CONE[3]))
        cv.put(mx + math.cos(a) * q(3.4), q(80) + math.sin(a) * q(2.4), C(CONE[2]))
    # apéndices: bajan hacia fuera y se enroscan hacia el centro, por debajo de la boca
    apps = []
    for s in (-1, 1):
        a0 = math.pi / 2 - s * (0.75 + 0.4 * open_)
        apps.append((s, chain(cv, mx + s * q(17), q(84), a0, s * (0.36 - 0.15 * open_) / k, int(14 * k),
                              q(6.4), q(2.4), 3.4, A_APP)))
    # ojos: globo oscuro, iris luminoso con pupila de rendija, párpado enfadado que baja hacia el centro
    eyes = [(mx - q(35), q(30)), (mx + q(35), q(30))]
    for s, (ex, ey) in zip((-1, 1), eyes):
        ellipse(cv, ex, ey, q(12), q(11), A_EYEB)
        ellipse(cv, ex - s * q(1), ey + q(1.5), q(7), q(7), IRIS)
        h = int(q(5.5))
        for dy in range(-h, h + 1):
            cv.put(ex - s * q(1), ey + q(1.5) + dy, OUTL)
            if abs(dy) < h - 1: cv.put(ex - s * q(1) - 1, ey + q(1.5) + dy, OUTL)
            if abs(dy) < h - 3 and k > 1.2: cv.put(ex - s * q(1) + 1, ey + q(1.5) + dy, OUTL)
        for dx, dy in ((-4, -3), (-3, -3), (-4, -2), (-2.6, -3.6)): cv.put(ex + q(dx), ey + q(dy), C('#ffffff'))
        for yy in range(int(ey - q(12)), int(ey + q(3))):
            for xx in range(int(ex - q(13)), int(ex + q(14))):
                inner = (xx - ex) * -s
                lid = ey - q(5) + inner * 0.5
                if (xx, yy) in cv.px and yy < lid:
                    cv.px[(xx, yy)] = C(A_EYEB[1] if yy < lid - 1.5 else OUTL)
    cv.outline()
    for s, pts in apps:                                         # espinas de los apéndices, hacia dentro de la curva
        for px, py, r, a in pts[1:-1]:
            nx, ny = (math.sin(a), -math.cos(a)) if s < 0 else (-math.sin(a), math.cos(a))
            for d, col in ((1.2, C('#fff0d8')), (2.3, C('#fff0d8')), (3.3, C('#e8c8a8', 220)), (4.3, C('#e8c8a8', 140))):
                if d < 4 or k > 1.2: cv.put_out(px + nx * (r + d), py + ny * (r + d), col)
    for ex, ey in eyes:                                         # brillo de los ojos
        R0, R1 = q(12), q(16.5)
        for yy in range(int(ey - R1), int(ey + R1) + 1):
            for xx in range(int(ex - R1), int(ex + R1) + 1):
                d = math.hypot(xx + .5 - ex, yy + .5 - ey)
                if R0 < d < R1: cv.put_out(xx, yy, GLOW + (int(90 * (R1 - d) / (R1 - R0)),))
    return cv.img()


def pose_anomalocaris():
    """Se acerca nadando, abre los apéndices con los ojos encendidos y tiembla, amenazante (el último cuadro se queda)."""
    f = AN['anomalocaris']
    T = math.tau
    fr = [f(phase=i * T / 6, curl=5.6) for i in range(2)]
    for i, k in enumerate((0.3, 0.65, 1.0)):
        fr.append(f(phase=2 + i * 0.6, amp=0.6, dx=-k * 3, spread=0.55 + k * 0.75, curl=5.6 - k * 4, glow=k > 0.5, red=k > 0.9))
    for i in range(3):
        fr.append(f(phase=4 + i, amp=0.3, sway=0.2, dx=-3 + (i % 2), spread=1.35, curl=1.5, glow=True, red=True,
                    bubbles=1 + i))
    return [as_img(x) for x in fr]


# =====================================================================================
# Euriptérido
# =====================================================================================
EU = load_defs(os.path.join(TEMP, 'floor2-bosses', 'generar_euripterido.py'), 'anims = {}')
E_BODY, E_LIMB = EU['ARMOR']['body'], EU['ARMOR']['limb']
POISON_RAMP = ['#4a8a20', '#7acc3a', '#a8ff5a', '#eaffc8']


def claw(cv, x, y, a, k, gap):
    """Pinza curva y puntiaguda al final del brazo: dos hojas que salen separadas, se curvan una hacia la otra
    y dejan una abertura entre las puntas."""
    ellipse(cv, x, y, 5.4 * k, 5.4 * k, E_LIMB)                 # mano
    tips = []
    for s in (-1, 1):
        px, py = -math.sin(a) * s, math.cos(a) * s
        bx, by = x + px * 3.4 * k + math.cos(a) * 2 * k, y + py * 3.4 * k + math.sin(a) * 2 * k
        pts = chain(cv, bx, by, a + s * gap, -s * 0.3, 9, 3.4 * k, 0.9 * k, 2.0 * k, E_LIMB)
        tips.append(pts[-1])
    return tips


def portrait_eurypterid(n=PN):
    """De frente: escudo acorazado con púas en el borde, ojos enfadados, las dos pinzas levantadas y abiertas
    y la cola arqueada por encima, con la punta venenosa encendida."""
    k = n / 128
    def q(v): return v * k
    cv = Canvas(n)
    mx = n / 2
    # cola: sale de detrás, sube por encima del escudo y se dobla hacia delante
    tail = chain(cv, mx + q(22), q(66), -math.pi / 2 + 0.25, -0.13 / k, int(17 * k), q(7), q(3.6), 4.2, E_BODY)
    tx, ty, tr, ta = tail[-1]
    rot_ellipse(cv, tx + math.cos(ta) * q(5), ty + math.sin(ta) * q(5), q(6), q(3.4), ta, E_LIMB)
    sting = (tx + math.cos(ta) * q(12), ty + math.sin(ta) * q(12))
    # patas y remos, detrás
    for s in (-1, 1):
        for j, (ang, L) in enumerate(((0.35, 26), (0.85, 22), (1.25, 18))):
            aa = ang if s > 0 else math.pi - ang
            chain(cv, mx + s * q(18), q(80 + j * 5), aa, 0.05 * s, int(6 * k), q(3.2), q(1.8), q(L / 6), E_LIMB)
        ang = 0.15 if s > 0 else math.pi - 0.15
        rot_ellipse(cv, mx + s * q(42), q(94), q(12), q(5), ang + s * 0.4, E_LIMB, shift=-1)
    # abdomen, que se aleja hacia abajo
    for j in range(5, -1, -1):
        ellipse(cv, mx, q(88 + j * 6), q(22 - j * 2.2), q(6), E_BODY, shift=-1 if j % 2 else 0)
    # escudo (prosoma)
    def shield(x, y):
        u, v = (x - mx) / q(35), (y - q(68)) / q(18)
        return abs(u) ** 2.8 + abs(v) ** 2.2 <= 1
    fill(cv, shield, mx, q(64), q(35), q(18), E_BODY)
    for y in range(int(q(46)), int(q(90))):                    # borde blindado claro arriba, oscuro abajo
        for x in range(int(mx - q(36)), int(mx + q(36))):
            if (x, y) in cv.px and shield(x + .5, y + .5):
                if not shield(x + .5, y + .5 - q(2.2)) and y < q(66): cv.put(x, y, C(E_BODY[3]))
                elif not shield(x + .5, y + .5 + q(1.6)) and y > q(66): cv.put(x, y, C(E_BODY[0]))
    for x in range(int(mx - q(14)), int(mx + q(15))):          # cresta central
        cv.put_in(x, q(58) + abs(x - mx) * 0.18, C(E_BODY[1]))
    # brazos con pinzas, levantados a los lados
    for s in (-1, 1):
        a0 = -math.pi / 2 + s * 0.8
        arm = chain(cv, mx + s * q(28), q(70), a0, 0, int(5 * k), q(5.6), q(4.6), 3.6, E_LIMB)
        ax, ay, ar, aa = arm[-1]
        claw(cv, ax + math.cos(aa) * q(4), ay + math.sin(aa) * q(4), aa, k, 0.75)
    # ojos: blancos con la pupila roja mirándote y el ceño hacia el centro
    eyes = [(mx - q(13), q(62)), (mx + q(13), q(62))]
    for s, (ex, ey) in zip((-1, 1), eyes):
        ellipse(cv, ex, ey, q(6.4), q(4.6), ['#1a1428', '#2e2438', '#56486a', '#9a88a8'])
        ellipse(cv, ex, ey + q(0.5), q(4.6), q(3.2), ['#e0d8c8', '#f4f4e8', '#ffffff', '#ffffff'])
        ellipse(cv, ex - s * q(1.2), ey + q(1), q(2), q(2), ['#c02020', '#ff3020', '#ff6040', '#ffa080'])
        cv.put(ex - s * q(1.2), ey + q(1), OUTL); cv.put(ex - s * q(1.2) - 1, ey + q(1), OUTL)
        for yy in range(int(ey - q(6)), int(ey + q(2))):
            for xx in range(int(ex - q(7)), int(ex + q(8))):
                inner = (xx - ex) * -s
                lid = ey - q(2.2) + inner * 0.45
                if (xx, yy) in cv.px and yy < lid and ((xx + .5 - ex) / q(6.4)) ** 2 + ((yy + .5 - ey) / q(4.6)) ** 2 <= 1:
                    cv.px[(xx, yy)] = C(E_BODY[1] if yy < lid - 1.5 else OUTL)
    cv.outline()
    # púas del borde del escudo, con la punta venenosa
    for j in range(-5, 6):
        a = -math.pi / 2 + j * 0.27
        bx, by = mx + math.cos(a) * q(35.5), q(66) + math.sin(a) * q(18.5)
        for d in range(int(q(4))):
            col = C('#fff0d8') if d < q(2.6) else C(POISON_RAMP[2])
            cv.put_out(bx + math.cos(a) * d, by + math.sin(a) * d, col)
    for t in range(int(q(5))):                                  # aguijón de la cola
        cv.put_out(sting[0] + math.cos(ta) * t, sting[1] + math.sin(ta) * t, C('#fff0d8') if t < q(3) else C(POISON_RAMP[2]))
    gx, gy = sting                                              # brillo venenoso
    R0, R1 = q(2), q(9)
    for yy in range(int(gy - R1), int(gy + R1) + 1):
        for xx in range(int(gx - R1), int(gx + R1) + 1):
            d = math.hypot(xx + .5 - gx, yy + .5 - gy)
            if R0 < d < R1: cv.put_out(xx, yy, (168, 255, 90, int(110 * (R1 - d) / (R1 - R0))))
    return cv.img()


def pose_eurypterid():
    """Levanta y abre las pinzas, se le erizan las púas del caparazón y se le encienden los ojos."""
    f = EU['eurypterid']
    T = math.tau
    fr = [f(phase=i * T / 6, tail=0.12 * math.sin(i * T / 6)) for i in range(2)]
    for i, o in enumerate((0.7, 0.85, 1.0)):
        fr.append(f(open_=(o, o), spread=0.3 + i * 0.15, dx=-i * 0.7, legs='brace', red=i >= 1, spike_glow=1 if i >= 1 else 0))
    for i in range(3):
        fr.append(f(open_=(1.0, 1.0), spread=0.6, dx=-2 + (i % 2), legs='brace', red=True, spike_glow=1, tail=0.1 * (i % 2)))
    return [as_img(x) for x in fr]


def cave_room():
    """Fondo de la cueva del Euriptérido para la maqueta: azul oscuro con rocas."""
    im = Image.new('RGBA', (480, 270), C('#141a24'))
    d = ImageDraw.Draw(im)
    for x, y, r in ((70, 60, 22), (380, 50, 18), (160, 236, 20), (300, 110, 16), (440, 230, 24), (40, 200, 14)):
        d.ellipse([x - r, y - r * 0.7, x + r, y + r * 0.7], fill=C('#2a303c'), outline=C('#0c1018'))
        d.ellipse([x - r * 0.6, y - r * 0.55, x + r * 0.3, y - r * 0.1], fill=C('#3c4450'))
    return im


# =====================================================================================
# Piso 1: virus gigante y ameba gigante (sus generadores ya dibujan a cualquier tamaño)
# =====================================================================================
VI = load_defs(os.path.join(TEMP, 'enemies', 'generar_enemigos.py'), '# --------- guardar')
AM = load_defs(os.path.join(TEMP, 'enemies', 'BossAmoeba', 'generar_ameba_gigante.py'),
               '# ---------------- animaciones del cuerpo grande')
SOUP_RAMP = ['#f8ffd8', '#d8f070', '#a8c040', '#5a7020']


def thick_brow(cv, cx, top, half_w, drop, t):
    """Ceño en V de t píxeles de grosor (los generadores del piso 1 lo dibujan de 1 px, fino a este tamaño)."""
    for i in range(int(half_w) + 1):
        for w in range(t):
            for side in (-1, 1):
                cv.put(cx + side * (half_w - i), top + i * drop + w, OUTL)


def teeth_mouth(cv, cx, cy, w, h, k, dark='#3a0a20', tooth='#fbf6f0', tongue=None):
    """Boca abierta (elipse oscura) con una fila de dientes puntiagudos arriba y otra abajo."""
    ellipse(cv, cx, cy, w, h, [dark] * 4)
    if tongue: ellipse(cv, cx + w * 0.15, cy + h * 0.55, w * 0.5, h * 0.35, tongue)
    tw = max(3, int(3 * k))
    x = cx - w + tw
    while x < cx + w - tw:
        for row, sgn in ((-1, 1), (1, -1)):
            yb = cy + row * h * math.sqrt(max(0, 1 - ((x - cx) / w) ** 2))
            for i in range(tw):
                for j in range(tw - i):
                    if 0 <= j < tw: cv.put_in(x - (tw - i) / 2 + j, yb + sgn * (i + 1) - (1 if sgn < 0 else 0), C(tooth))
        x += tw + 1


def portrait_virus(n=PN):
    """El virus gigante de frente y furioso: cápside de caras marcadas, espículas gruesas con las bolas encendidas,
    un solo ojo enorme con el ceño en V y la boca abierta llena de dientes."""
    k = n / 128
    def q(v): return v * k
    V = VI['VIR1']
    cv = Canvas(n)
    c = n / 2
    R = q(40)
    for j in range(8):                                          # espículas
        a = j * math.pi / 4 + math.pi / 8
        L = q(58)
        for t in range(int(R * 0.8), int(L)):
            for w in (-1, 0, 1):
                cv.put(c + math.cos(a) * t - math.sin(a) * w, c + math.sin(a) * t + math.cos(a) * w,
                       C('#6a6a88') if w == 1 else C(V['stalk']) if w == 0 else C('#b4b4cc'))
        hx, hy = c + math.cos(a) * L, c + math.sin(a) * L
        ellipse(cv, hx, hy, q(6), q(6), V['knob'])
    def hexa(x, y):
        dx, dy = abs(x - c), abs(y - c)
        return dy <= R * 0.88 and dx * 0.88 + dy * 0.5 <= R * 0.88
    fill(cv, hexa, c, c, R, R, V['body'])
    # caras de la cápside: un hexágono interior y radios hasta los vértices
    verts = [(c + R * math.cos(math.pi / 3 * i) * 1.0, c + R * 0.88 * math.sin(math.pi / 3 * i) * 1.0) for i in range(6)]
    inner = [(c + (x - c) * 0.62, c + (y - c) * 0.62) for x, y in verts]
    def seg(p0, p1, col):
        L = int(math.hypot(p1[0] - p0[0], p1[1] - p0[1]))
        for t in range(L + 1):
            cv.put_in(p0[0] + (p1[0] - p0[0]) * t / L, p0[1] + (p1[1] - p0[1]) * t / L, C(col))
    for i in range(6):
        seg(inner[i], inner[(i + 1) % 6], V['facet']); seg(inner[i], verts[i], V['facet'])
    for (x, y) in ((c - R * 0.55, c - R * 0.6), (c - R * 0.62, c - R * 0.5)):   # brillo
        ellipse(cv, x, y, q(2.2), q(1.6), ['#ffffff'] * 4)
    # ojo
    ew, eh = q(17), q(13)
    ellipse(cv, c, c - q(5), ew, eh, ['#d8d0c8', '#f4f4e8', '#ffffff', '#ffffff'])
    ellipse(cv, c, c - q(3), q(5), q(9), ['#1b1b2b'] * 4)
    ellipse(cv, c - q(1.6), c - q(7), q(1.6), q(2), ['#ffffff'] * 4)
    for i in range(int(ew) + 4):                               # ceño en V, grueso
        for t in range(int(q(3))):
            for side in (-1, 1):
                cv.put(c + side * (ew + 2 - i), c - q(5) - eh - q(1) + i * 0.42 + t, OUTL)
    teeth_mouth(cv, c, c + q(20), q(15), q(6), k)
    cv.outline()
    for j in range(8):                                          # bolas que brillan
        a = j * math.pi / 4 + math.pi / 8
        hx, hy = c + math.cos(a) * q(58), c + math.sin(a) * q(58)
        R0, R1 = q(6), q(10)
        for yy in range(int(hy - R1), int(hy + R1) + 1):
            for xx in range(int(hx - R1), int(hx + R1) + 1):
                d = math.hypot(xx + .5 - hx, yy + .5 - hy)
                if R0 < d < R1: cv.put_out(xx, yy, (216, 240, 112, int(100 * (R1 - d) / (R1 - R0))))
    return cv.img()


def portrait_amoeba(n=PN):
    """La ameba gigante furiosa: citoplasma gelatinoso con membrana, gránulos, vacuolas y núcleo, rugiendo con dientes."""
    import random as _r
    k = n / 128
    def q(v): return v * k
    A = AM
    pal = A['ANGRY']
    cv = Canvas(n)
    cx, cy = n / 2, n / 2 + q(4)
    r = q(42)
    shape = A['blob_shape'](cx, cy, r, 0.3, 0.0, 6, 0.3, 0.5)
    fill(cv, shape, cx, cy, r + q(6), r + q(6), pal)
    rnd = _r.Random(4)
    for (x, y) in list(cv.px):                                  # membrana: borde claro por dentro
        edge = any(not shape(x + .5 + dx * q(2.2), y + .5 + dy * q(2.2)) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
        if edge and (x + y) < cx + cy: cv.px[(x, y)] = C(pal[3])
    for _ in range(int(40 * k * k)):                             # gránulos del citoplasma, en grupitos
        gx, gy = rnd.uniform(cx - r, cx + r), rnd.uniform(cy - r, cy + r)
        if (int(gx), int(gy)) in cv.px and cv.px[(int(gx), int(gy))] != C(pal[3]):
            for dx, dy in ((0, 0), (1, 0), (0, 1)):
                cv.put_in(gx + dx, gy + dy, C(pal[1]))
            cv.put_in(gx + 1, gy + 1, C(pal[2]))
    ellipse(cv, cx - r * 0.55, cy + r * 0.5, r * 0.2, r * 0.16, ['#8a2848', '#b8506f', '#d0708a', '#f0a0b8'])   # núcleo
    ellipse(cv, cx - r * 0.59, cy + r * 0.46, r * 0.05, r * 0.05, ['#fdd2de'] * 4)
    for _ in range(7):                                          # vacuolas
        a, d = rnd.uniform(0, math.tau), rnd.uniform(0.68, 0.85) * r
        vx, vy = cx + math.cos(a) * d, cy + math.sin(a) * d * 0.9
        if not shape(vx, vy) or math.hypot(vx - (cx - r * 0.55), vy - (cy + r * 0.5)) < r * 0.3: continue
        vr = q(rnd.uniform(2.2, 4.2))
        ellipse(cv, vx, vy, vr, vr, ['#f19bb3', '#f8c0d0', '#fde0e8', '#ffffff'])
        cv.put(vx - vr * 0.4, vy - vr * 0.4, C('#ffffff'))
    # ojos grandes con el ceño
    for side in (-1, 1):
        ex, ey = cx + side * q(13), cy - q(14)
        ellipse(cv, ex, ey, q(6), q(7), ['#e8dcd8', '#fbf6f0', '#ffffff', '#ffffff'])
        ellipse(cv, ex - side * q(1.2), ey + q(1.5), q(2.4), q(3.2), ['#2a1830'] * 4)
        cv.put(ex - side * q(1.2) - q(1), ey, C('#ffffff'))
        for i in range(int(q(14))):
            for t in range(int(q(2.4))):
                x = ex - q(7) + i
                yy = ey - q(8) + ((i if side < 0 else q(14) - i) * 0.4) + t
                cv.put(x, yy, OUTL)
        for yy in range(int(ey - q(8)), int(ey + q(1))):         # el ceño tapa lo de arriba del ojo
            for xx in range(int(ex - q(7)), int(ex + q(7))):
                i = xx - (ex - q(7))
                if yy < ey - q(8) + ((i if side < 0 else q(14) - i) * 0.4) and ((xx + .5 - ex) / q(6)) ** 2 + ((yy + .5 - ey) / q(7)) ** 2 <= 1:
                    cv.put(xx, yy, C(pal[1]))
    teeth_mouth(cv, cx, cy + q(10), q(13), q(9), k, dark='#4a0c26', tongue=['#a03050', '#d0607a', '#e88098', '#f8b0c0'])
    cv.outline()
    return cv.img()


def pose_virus():
    """Las espículas se cargan girando, abre el ojo de golpe y ruge con la boca abierta."""
    f = VI['virus']
    fr = [f(48, spin=i * 0.12) for i in range(2)]
    fr.append(f(48, spin=0.4, eye='wide', mouth='open', glow=True))
    fr.append(f(48, spin=0.7, eye='wide', mouth='wide', glow=True, scale=1.05))
    for i in range(4):
        fr.append(f(48, spin=1.0 + i * 0.35, eye='open', angry=True, mouth='wide', glow=i % 2 == 0, shake=((1 if i % 2 else -1), 0)))
    return [as_img(x) for x in fr]


def pose_amoeba():
    """Se encoge, crece de golpe y ruge temblando con los seudópodos desatados."""
    f = AM['draw_amoeba']
    A = AM
    fr = [f(t=i / 6) for i in range(2)]
    fr.append(f(t=0.3, eye='wide', mouth_kind='open', squash=-0.6, r=28))
    fr.append(f(t=0.4, pal=A['lerp_pal'](A['CALM'], A['ANGRY'], 0.6), mood='angry', eye='angry', mouth_kind='open', r=32, squash=0.5))
    for i in range(4):
        fr.append(f(t=0.5 + i / 8, pal=A['ANGRY'], mood='angry', eye='angry', mouth_kind='roar', r=31 + (i % 2), amp=0.26,
                    off=((1 if i % 2 else -1), 0)))
    return [as_img(x) for x in fr]


def soup_room(tint):
    """Fondo de sala del piso 1 para la maqueta: sopa con burbujas."""
    im = Image.new('RGBA', (480, 270), C(tint))
    d = ImageDraw.Draw(im)
    for x, y, r in ((60, 50, 6), (400, 40, 4), (150, 235, 5), (310, 120, 3), (450, 230, 7), (30, 190, 4), (220, 30, 3)):
        d.ellipse([x - r, y - r, x + r, y + r], outline=(150, 200, 120, 120))
    return im


# =====================================================================================
# Rótulo de prueba
# =====================================================================================
def font(name, size): return ImageFont.truetype(os.path.join(FONTS, name), size)


def title_text(text, path, size, ramp, out=(8, 20, 34)):
    """Nombre del jefe en píxel puro: relleno en bandas de 4 tonos de arriba abajo, contorno y sombra de 1 px."""
    f = ImageFont.truetype(path, size)
    l, t, r, b = f.getbbox(text)
    w, h = r - l + 8, b - t + 8
    mask = Image.new('L', (w, h), 0)
    d = ImageDraw.Draw(mask); d.fontmode = '1'
    d.text((4 - l, 4 - t), text, font=f, fill=255)
    m = mask.load()
    ys = [y for y in range(h) for x in range(w) if m[x, y]]
    y0, y1 = min(ys), max(ys)
    im = Image.new('RGBA', (w, h + 2), (0, 0, 0, 0)); px = im.load()
    for y in range(h):
        for x in range(w):
            if m[x, y]:
                px[x, y] = C(ramp[min(3, int((y - y0) / max(1, y1 - y0) * 4))])
            elif any(0 <= x + dx < w and 0 <= y + dy < h and m[x + dx, y + dy]
                     for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, 1), (1, -1), (-1, -1))):
                px[x, y] = out + (255,)
    sh = Image.new('RGBA', im.size, (0, 0, 0, 0))
    sh.paste((4, 10, 20, 255), (0, 2), im.crop((0, 0, w, h + 2)).split()[3].crop((0, 0, w, h + 2)))
    sh.alpha_composite(im)
    return sh


TITLE_FONT = os.path.join(HERE, 'fuentes', 'Jersey10-Regular.ttf')
SEA_RAMP = ['#e8fff4', '#9ff0e0', '#4cc4c8', '#2a7a9a']


def mockup(path, portrait, name, title, scene, accent, band=(14, 10, 22), name_img=None):
    W, H = 480, 270
    im = scene.copy()
    d = ImageDraw.Draw(im)
    # banda diagonal que entra desde la izquierda
    y0, y1 = 150, 214
    poly = [(0, y0 + 14), (W, y0 - 14), (W, y1 - 14), (0, y1 + 14)]
    d.polygon(poly, fill=band + (235,))
    d.line([(0, y0 + 14), (W, y0 - 14)], fill=accent, width=2)
    d.line([(0, y1 + 14), (W, y1 - 14)], fill=accent, width=2)
    # retrato a la derecha, por encima de la banda
    im.alpha_composite(portrait, (W - portrait.width - 26, y1 - portrait.height + 10))
    if name_img is not None:
        im.alpha_composite(name_img, (36, y0 + 18))
    else:
        d.text((40, y0 + 14), name.upper(), font=font('Silkscreen-Bold.ttf', 24), fill=(255, 244, 230))
    d.text((42, y0 + 50), title, font=font('Silkscreen-Regular.ttf', 8), fill=accent)
    d.text((W - 78, H - 14), '[E] Saltar', font=font('Silkscreen-Regular.ttf', 8), fill=(200, 200, 210, 200))
    im.resize((W * 2, H * 2), Image.NEAREST).save(path)


def dark_room():
    """Fondo de la sala del Anomalocaris para la maqueta: casi negro con plantas luminosas."""
    im = Image.new('RGBA', (480, 270), C('#070b16'))
    for x, y in ((60, 60), (400, 40), (140, 230), (300, 120), (450, 240), (30, 180)):
        for r in range(26, 0, -2):
            halo = Image.new('RGBA', im.size, (0, 0, 0, 0))
            ImageDraw.Draw(halo).ellipse([x - r, y - r, x + r, y + r], fill=(60, 140, 100, 14))
            im.alpha_composite(halo)
        d = ImageDraw.Draw(im)
        d.rectangle([x - 1, y - 6, x + 1, y + 4], fill=C('#4a8a5a'))
        d.ellipse([x - 3, y - 9, x + 3, y - 3], fill=C('#c8ff9a'))
    return im


def save_all(key, portrait, pose, ms, bg):
    for sub in ('Portraits', 'Poses', 'gif'): os.makedirs(os.path.join(HERE, sub), exist_ok=True)
    portrait.save(os.path.join(HERE, 'Portraits', f'Portrait_{key}.png'))
    save_sheet(os.path.join(HERE, 'Poses', f'{key}_Intro.png'), pose)
    # copia junto a las demás hojas del jefe, para que su menú de importación la recoja como el clip "Intro"
    boss_sheet = {'Virus': ('enemies', 'BossVirus', 'BossVirus'), 'Amoeba': ('enemies', 'BossAmoeba', 'BossAmoeba'),
                  'Anomalocaris': ('floor2-bosses', 'Anomalocaris', 'Anomalocaris'),
                  'Eurypterid': ('floor2-bosses', 'Eurypterid', 'Eurypterid')}[key]
    save_sheet(os.path.join(TEMP, boss_sheet[0], boss_sheet[1], f'{boss_sheet[2]}_Intro.png'), pose)
    preview(os.path.join(HERE, f'vista_previa_{key.lower()}.png'), {'Retrato': [portrait], 'Pose': pose}, bg, scale=2)
    gif(os.path.join(HERE, 'gif', f'{key}_Intro.gif'), pose + [pose[-1]] * 4, ms, bg)


if __name__ == '__main__':
    p = portrait_anomalocaris()
    save_all('Anomalocaris', p, pose_anomalocaris(), 130, C('#0a0f1c'))
    mockup(os.path.join(HERE, 'maqueta_rotulo_anomalocaris.png'), p, 'Anomalocaris', 'Terror del Cámbrico',
           dark_room(), (120, 220, 210), band=(8, 16, 30), name_img=title_text('ANOMALOCARIS', TITLE_FONT, 40, SEA_RAMP))
    e = portrait_eurypterid()
    save_all('Eurypterid', e, pose_eurypterid(), 130, C('#141a24'))
    mockup(os.path.join(HERE, 'maqueta_rotulo_euripterido.png'), e, 'Euriptérido', 'Escorpión de los mares',
           cave_room(), (120, 220, 210), band=(8, 16, 30), name_img=title_text('EURIPTÉRIDO', TITLE_FONT, 40, SEA_RAMP))
    v = portrait_virus()
    save_all('Virus', v, pose_virus(), 130, C('#1c2a22'))
    mockup(os.path.join(HERE, 'maqueta_rotulo_virus.png'), v, 'Virus gigante', 'Plaga primigenia',
           soup_room('#1c2a22'), (255, 122, 168), band=(30, 14, 26), name_img=title_text('VIRUS GIGANTE', TITLE_FONT, 40, SOUP_RAMP))
    am = portrait_amoeba()
    save_all('Amoeba', am, pose_amoeba(), 130, C('#1c2a22'))
    mockup(os.path.join(HERE, 'maqueta_rotulo_ameba.png'), am, 'Ameba gigante', 'La que todo lo engulle',
           soup_room('#22281e'), (255, 122, 168), band=(30, 14, 26), name_img=title_text('AMEBA GIGANTE', TITLE_FONT, 40, SOUP_RAMP))
    os.makedirs(os.path.join(HERE, 'Titles'), exist_ok=True)   # el nombre ya rotulado, para los documentos
    for key, text, ramp in (('Virus', 'VIRUS GIGANTE', SOUP_RAMP), ('Amoeba', 'AMEBA GIGANTE', SOUP_RAMP),
                            ('Anomalocaris', 'ANOMALOCARIS', SEA_RAMP), ('Eurypterid', 'EURIPTÉRIDO', SEA_RAMP)):
        title_text(text, TITLE_FONT, 40, ramp).save(os.path.join(HERE, 'Titles', f'Title_{key}.png'))
    print('Listo')
