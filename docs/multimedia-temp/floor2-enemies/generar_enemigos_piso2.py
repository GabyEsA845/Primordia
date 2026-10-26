"""Enemigos del piso 2 (océano cámbrico) de Primordia.

Trilobite, medusa azul, Ottoia (gusano enterrado), esponja Vauxia y cardumen de Pikaia.
Mismo estilo que ../floor2-bodies/generar_cuerpos_piso2.py y ../enemies/generar_enemigos.py:
16 px por unidad, luz arriba a la izquierda, contorno oscuro, ojos enfadados. Todos miran a la derecha (Flip X).

Uso: python generar_enemigos_piso2.py [carpeta_salida]   (por defecto, la carpeta de este script)
"""
import math, sys, os, random
from PIL import Image, ImageDraw

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__))
OUTL = (24, 20, 37, 255)

def C(h, a=255):
    if isinstance(h, tuple): return h[:3] + (a,)
    h = h.lstrip('#'); return tuple(int(h[i:i+2], 16) for i in (0, 2, 4)) + (a,)

EYE = C('#f4f4e8')
MOUTH = C('#3a1020')
BONE = C('#f4ecd8')
SPARK = [C('#ffffff'), C('#fff6c0'), C('#ffe080')]
WHITE4 = ['#ffffff'] * 4
RED4 = ['#a33b3b', '#e05a5a', '#ff9a8a', '#fff0e0']
PALE4 = ['#6a6450', '#9a9478', '#c4c0a4', '#ecead8']
STONE4 = ['#4a4640', '#6e6a62', '#948f84', '#b8b2a4']
SAND = ['#4a3c28', '#7a6646', '#ad9670', '#dcc9a0']
HEAL = C('#7aff9a')

# --- lienzo --------------------------------------------------------------------------------
class Canvas:
    def __init__(s, n): s.n = n; s.px = {}
    def put(s, x, y, c):
        x, y = int(math.floor(x)), int(math.floor(y))
        if 0 <= x < s.n and 0 <= y < s.n: s.px[(x, y)] = C(c) if isinstance(c, str) else c
    def put_in(s, x, y, c):
        if (int(math.floor(x)), int(math.floor(y))) in s.px: s.put(x, y, c)
    def put_out(s, x, y, c):
        if (int(math.floor(x)), int(math.floor(y))) not in s.px: s.put(x, y, c)
    def outline(s):
        add = [(x, y) for y in range(s.n) for x in range(s.n) if (x, y) not in s.px and any(
            (x+dx, y+dy) in s.px and s.px[(x+dx, y+dy)] != OUTL for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)))]
        for p in add: s.px[p] = OUTL
    def img(s):
        im = Image.new('RGBA', (s.n, s.n), (0, 0, 0, 0))
        for p, c in s.px.items(): im.putpixel(p, c)
        return im

def shade(nx, ny):
    """Índice 0-3 de la rampa con luz arriba a la izquierda."""
    v = (-0.6*nx - 0.8*ny) * 0.8 + 0.35 - max(0, math.hypot(nx, ny) - 0.75) * 1.2
    return 0 if v < -0.15 else 1 if v < 0.35 else 2 if v < 0.75 else 3

def fill(cv, inside, cx, cy, rx, ry, ramp, alpha=255, shift=0):
    ramp = [C(c, alpha) for c in ramp]
    for y in range(cv.n):
        for x in range(cv.n):
            px, py = x + .5, y + .5
            if inside(px, py):
                cv.px[(x, y)] = ramp[max(0, min(3, shade((px - cx) / rx, (py - cy) / ry) + shift))]

def ellipse(cv, cx, cy, rx, ry, ramp, alpha=255, shift=0):
    fill(cv, lambda x, y: ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1, cx, cy, rx, ry, ramp, alpha, shift)

def eye_side(cv, x, y, state='angry', red=False):
    """Ojo de perfil (mira a la derecha), como los enemigos del piso 1."""
    x, y = int(x), int(y)
    if state == 'none': return
    if state == 'x':
        for dx, dy in ((0, 0), (2, 0), (1, 1), (0, 2), (2, 2)): cv.put(x - 1 + dx, y - 1 + dy, OUTL)
        return
    if state == 'closed': cv.put(x, y + 1, OUTL); cv.put(x + 1, y + 1, OUTL); return
    white = C('#ffd0c8') if red else EYE
    for dx in range(2):
        for dy in range(2): cv.put(x + dx, y + dy, white)
    if state == 'wide': return
    cv.put(x + 1, y + 1, C('#ff3020') if red else OUTL)
    cv.put(x - 1, y - 1, OUTL); cv.put(x, y - 1, OUTL)      # ceja

def eye_front(cv, x, y, side, state='angry', red=False):
    """Ojo de frente; side -1 = ojo izquierdo, 1 = derecho. Enfadado: la esquina de dentro se tapa."""
    x, y = int(x), int(y)
    if state in ('none', 'x', 'closed'): eye_side(cv, x, y, state); return
    white = C('#ffd0c8') if red else EYE
    for dx in range(2):
        for dy in range(2): cv.put(x + dx, y + dy, white)
    if state == 'wide': return
    inner = x + 1 if side < 0 else x
    cv.put(inner, y, OUTL)
    cv.put(inner, y + 1, C('#ff3020') if red else OUTL)

def sparks(cv, x, y, size=2):
    cv.put(x, y, SPARK[0])
    for k in range(1, size + 1):
        c = SPARK[min(k, 2)]
        for dx, dy in ((k, 0), (-k, 0), (0, k), (0, -k)): cv.put(x + dx, y + dy, c)

def puff(cv, x, y, r, alpha=200):
    """Nube de arena."""
    for yy in range(int(y - r) - 1, int(y + r) + 2):
        for xx in range(int(x - r) - 1, int(x + r) + 2):
            d = math.hypot(xx + .5 - x, yy + .5 - y)
            if d <= r: cv.put_out(xx, yy, C(SAND[3] if d < r * 0.55 else SAND[2], alpha))

def as_img(f): return f if isinstance(f, Image.Image) else f.img()

# =========================================================================================
# TRILOBITE (24×24, visto desde arriba, cabeza a la derecha)
# =========================================================================================
TRI = ['#4a2418', '#8a4a2a', '#c47a42', '#f2b674']          # tórax y pigidio, óxido
TRI_ARM = ['#22243a', '#40445c', '#6a7090', '#a4aac4']      # céfalo blindado, acero
TRI_LEG = ['#d89060', '#f4c890']
BELLY = ['#7a4a40', '#c08070', '#e8b4a0', '#fff0e4']

def trilobite(pal=TRI, arm=TRI_ARM, leg_c=TRI_LEG, n=24, dx=0, dy=0, sx=1.0, sy=1.0, leg=0, legs='walk',
              eyes='angry', red=False, rim=0, belly=False, crack=0, stars=None, dust=0, streak=0, alpha=255):
    cv = Canvas(n); o = (n - 24) / 2
    cx, cy = 11.5 + dx + o, 12 + dy + o
    L, W = 9 * sx, 6.2 * sy
    hb, tb = 2.5 * sx, -5.5 * sx

    def inside(px, py):
        u, v = px - cx, py - cy
        if u >= hb: return ((u - hb) / (L - hb)) ** 2 + (v / W) ** 2 <= 1
        if u >= tb: return abs(v) <= W * (0.78 + 0.2 * (u - tb) / (hb - tb))
        return ((u - tb) / (3.5 * sx)) ** 2 + (v / (W * 0.78)) ** 2 <= 1

    if belly:
        fill(cv, inside, cx, cy, L, W, pal if pal is not TRI else BELLY, alpha)
        bp = pal if pal is not TRI else BELLY
        ellipse(cv, cx + 5 * sx, cy, 2 * sx, 1.6 * sy, bp, alpha, shift=-1)          # hipostoma
        for u in range(-6, 4, 2):                                                     # bases de las patas
            for s in (-1, 1): cv.put_in(cx + u * sx, cy + s * 2 * sy, C(bp[0], alpha))
    else:
        fill(cv, inside, cx, cy, L, W, pal, alpha)
        # céfalo blindado (el frente que recibe la mitad del daño)
        fill(cv, lambda px, py: inside(px, py) and px - cx >= hb, cx, cy, L, W, arm, alpha)
        # espinas genales hacia atrás
        for s in (-1, 1):
            for k in range(5):
                cv.put(cx + hb - 1 - k, cy + s * (W - 0.6 + k * 0.25), C(arm[1 if k < 3 else 0], alpha))
        ellipse(cv, cx + 5.4 * sx, cy, 2.3 * sx, 2 * sy, arm, alpha, shift=1)      # glabela
        # surcos (los tres lóbulos) y segmentos del tórax
        for (x, y) in list(cv.px):
            u, v = x + .5 - cx, y + .5 - cy
            if u >= hb - 0.5 or cv.px[(x, y)][:3] in [C(a)[:3] for a in arm]: continue
            if abs(abs(v) - W * 0.32) < 0.5: cv.px[(x, y)] = C(pal[0], alpha)
            elif u - tb >= 0.5 and int(u - tb) % 2 == 1: cv.px[(x, y)] = C(pal[1], alpha)
            elif u < tb and int(tb - u) % 2 == 1 and abs(v) < W * 0.5: cv.px[(x, y)] = C(pal[1], alpha)
        if rim:
            col = C('#ff9a5a') if rim == 1 else C('#ffffff')
            for (x, y) in list(cv.px):
                u, v = x + .5 - cx, y + .5 - cy
                if u > L - 1.6 - abs(v) * 0.25 and u >= hb: cv.px[(x, y)] = col
        ex = cx + 3.5 * sx
        eye_side(cv, ex, cy - 2 - 2 * sy, eyes, red)
        eye_side(cv, ex, cy + 2 * sy, eyes, red)

    if crack:   # muerte: el caparazón se parte en céfalo, tórax y pigidio
        moved = {}
        for (x, y), c in cv.px.items():
            u = x + .5 - cx
            moved[(x + (crack if u >= hb else -crack if u < tb else 0), y + (crack if u < tb else 0))] = c
        cv.px = moved
    cv.outline()

    # patas: finas, sin contorno, asoman por los lados
    if legs != 'none':
        for k, u0 in enumerate((-6, -4, -2, 0, 2, 4)):
            u0 *= sx
            for s in (-1, 1):
                if legs == 'walk': a = 0.55 * math.sin(leg * math.pi / 2 + k * 1.4 + (math.pi if s > 0 else 0)); ln = 3
                elif legs == 'brace': a = 0.55 - k * 0.05; ln = 3.2
                elif legs == 'swept': a = -1.15 + 0.15 * math.sin(leg * math.pi + k); ln = 3.4
                elif legs == 'flail': a = 1.1 * math.sin(leg * math.pi / 2 + k * 1.9 + s); ln = 3.6
                elif legs == 'limp': a = -0.5; ln = 2
                else: a = 0.9; ln = 1.5     # curl
                # borde del cuerpo en u0
                e = 0
                while e < 12 and inside(cx + u0 + .5, cy + s * (e + .5)): e += 1
                px, py = cx + u0, cy + s * (e + 1.2)
                for st in range(int(ln * 2)):
                    px += math.sin(a) * 0.5; py += s * math.cos(a) * 0.5
                    cv.put_out(px, py, C(leg_c[1 if st >= ln else 0], alpha))
        # antenas
        for s in (-1, 1):
            px, py = cx + L - 0.6, cy + s * 1.2
            b0 = {'swept': 1.4, 'flail': 0.6 + 0.6 * math.sin(leg * 1.6 + s), 'brace': 0.6}.get(legs, 0.25 + 0.15 * math.sin(leg * math.pi / 2 + s))
            for st in range(9):
                b = b0 + st * 0.09
                if legs == 'swept': px -= 0.15; py += s * 0.5
                else: px += math.cos(b) * 0.5; py += s * math.sin(b) * 0.5
                cv.put_out(px, py, C(leg_c[0], alpha))
    for k in range(dust):     # arena que levanta al rascar antes de embestir
        puff(cv, cx - L - 1 - k * 1.5, cy + (-4 if k % 2 else 4) * sy + (k - 1) * 0.5, 1.2 + k * 0.3, 210 - k * 30)
    for k in range(streak):   # estela de velocidad
        for v in (-4, 0, 4):
            for t in range(3 + (k + v // 4) % 3):
                cv.put_out(cx - L - 2 - t - k * 2, cy + v + (1 if v == 0 else 0), C('#bfe6ff', 170 - t * 35))
    if stars is not None:     # aturdido: estrellitas girando
        for i in range(3):
            a = stars + i * 2.09
            sx_, sy_ = cx + math.cos(a) * 6, cy - W - 2.5 + math.sin(a) * 1.5
            cv.put(sx_, sy_, SPARK[1])
            for ddx, ddy in ((1, 0), (-1, 0), (0, 1), (0, -1)): cv.put(sx_ + ddx, sy_ + ddy, SPARK[2])
    return cv

anims = {}
anims['Trilobite_Move'] = [trilobite(leg=i) for i in range(4)]
# aviso (0,6 s): se echa atrás, ojos rojos, rasca la arena y el filo del céfalo brilla
anims['Trilobite_Warn'] = [
    trilobite(dx=-1, sx=0.95, sy=1.05, legs='brace', red=True),
    trilobite(dx=-2, sx=0.9, sy=1.08, legs='brace', red=True, rim=1, dust=1),
    trilobite(dx=-2, dy=1, sx=0.9, sy=1.08, legs='brace', red=True, rim=2, dust=2),
    trilobite(dx=-2, sx=0.9, sy=1.08, legs='brace', red=True, rim=1, dust=3)]
# embestida (bucle): estirado, patas hacia atrás, estela
anims['Trilobite_Charge'] = [trilobite(n=32, dx=2, sx=1.08, sy=0.94, legs='swept', leg=i, red=True,
                                       rim=1 if i == 0 else 0, streak=1 + i % 2) for i in range(3)]
# al chocar se da la vuelta y queda panza arriba
FLIP = [trilobite(sy=0.55, eyes='wide', legs='swept'),
        trilobite(belly=True, sy=0.3, legs='none'),
        trilobite(belly=True, sy=0.75, legs='flail', leg=0)]
anims['Trilobite_Flip'] = FLIP
anims['Trilobite_Stunned'] = [trilobite(belly=True, legs='flail', leg=i, stars=i * math.pi / 6) for i in range(4)]
anims['Trilobite_Recover'] = [trilobite(belly=True, sy=0.6, legs='flail', leg=2), trilobite(belly=True, sy=0.3, legs='none'),
                              trilobite(sy=0.6, legs='swept'), trilobite(legs='walk', leg=1)]
anims['Trilobite_Hurt'] = [trilobite(pal=WHITE4, arm=WHITE4, leg_c=['#ffffff'] * 2, eyes='wide', sx=0.94, sy=1.06),
                           trilobite(pal=RED4, arm=RED4, leg_c=RED4[2:], eyes='wide', sx=1.04, sy=0.96)]
anims['Trilobite_Death'] = [
    trilobite(n=32, pal=WHITE4, arm=WHITE4, leg_c=['#ffffff'] * 2, eyes='wide'),
    trilobite(n=32, pal=PALE4, arm=PALE4, leg_c=PALE4[2:], eyes='x', legs='limp'),
    trilobite(n=32, pal=PALE4, leg_c=PALE4[2:], belly=True, legs='curl'),
    trilobite(n=32, pal=PALE4, leg_c=PALE4[2:], belly=True, legs='none', crack=1, dust=2),
    trilobite(n=32, pal=STONE4, arm=STONE4, eyes='none', legs='none', crack=1, dust=3, alpha=200),
    trilobite(n=32, pal=STONE4, arm=STONE4, eyes='none', legs='none', crack=1),
    trilobite(n=32, pal=STONE4, arm=STONE4, eyes='none', legs='none', crack=1)]

# =========================================================================================
# MEDUSA AZUL (24×24, de frente, translúcida)
# =========================================================================================
BJ = dict(ramp=['#1a3478', '#2f62c4', '#6aa8ee', '#c8ecff'], alpha=205, gon='#9ef4ff',
          tent=['#7ad8ff', '#3a8ae0'], arm='#b0e8ff', cn='#a8e8ff', hot='#ffffff')
WHITE_J = dict(ramp=WHITE4, alpha=255, gon='#ffffff', tent=['#ffffff'] * 2, arm='#ffffff', cn='#ffffff', hot='#ffffff')
RED_J = dict(ramp=RED4, alpha=230, gon='#fff0e0', tent=['#ff9a8a', '#e05a5a'], arm='#fff0e0', cn='#fff0e0', hot='#ffffff')
PALE_J = dict(ramp=['#4a5a74', '#7a8aa2', '#a4b4c8', '#dce4ee'], alpha=190, gon='#c4d0dc', tent=['#a4b4c8', '#7a8aa2'],
              arm='#dce4ee', cn='#c4d0dc', hot='#dce4ee')

def blue_jelly(pal=BJ, pulse=0.0, phase=0, glow=0, eyes='angry', red=False, dx=0, dy=0, tent=6.0, lift=0.0,
               droop=0.0, n=24, grow=0.0, ring=None):
    """pulse > 0: campana ancha y baja; pulse < 0: estrecha y alta. ring = (radio, intensidad): las 8 cargas."""
    cv = Canvas(n); o = (n - 24) / 2
    cx = 12 + dx + o; rim = 12.5 + dy + o - pulse * 0.6
    rx = (6.8 + grow) * (1 + 0.16 * pulse); ry = (6.6 + grow) * (1 - 0.18 * pulse)
    a = pal['alpha']
    fill(cv, lambda px, py: py <= rim + 0.5 and ((px - cx) / rx) ** 2 + ((py - rim) / ry) ** 2 <= 1, cx, rim, rx, ry, pal['ramp'], a)
    ramp = [C(c, a) for c in pal['ramp']]
    for x in range(int(cx - rx + 1), int(cx + rx)):           # festón
        if (x + phase // 2) % 3 != 2: cv.put(x, rim + 0.5, ramp[1])
    for gx in (-3, -2, 1, 2):                                  # gónadas, arriba
        cv.put_in(cx + gx, rim - ry * 0.8, C(pal['gon'], 235))
    ey = rim - ry * 0.48
    eye_front(cv, cx - 3.5, ey, -1, eyes, red); eye_front(cv, cx + 1.5, ey, 1, eyes, red)
    hot, cn = C(pal['hot']), C(pal['cn'])
    for i, x in enumerate(range(int(cx - rx + 1), int(cx + rx), 2)):   # cnidocitos del borde
        cv.put_in(x, rim + 0.5, hot if glow >= 2 or (glow == 1 and i % 2 == 0) else cn)
    cv.outline()
    tl = [C(c, 170) for c in pal['tent']]
    strands = [cx - rx + 1.6, cx - rx * 0.45, cx + rx * 0.45, cx + rx - 1.6]
    length = max(1.0, tent - lift)
    for i, x0 in enumerate(strands):
        for k in range(int(length + droop)):
            wav = math.sin(phase * math.pi / 2 + k * 0.55 + i * 1.6) * (0.75 if droop == 0 else 0.25) * min(1, k / 2)
            cv.put_out(x0 + wav + lift * 0.25 * (1 if x0 > cx else -1), rim + 1.5 + k, tl[k % 2])
    al = C(pal['arm'], 200)
    for k in range(int(length * 0.7 + droop)):
        wav = math.sin(phase * math.pi / 2 + k * 0.5 + 2) * 0.5 * min(1, k / 2)
        cv.put_out(cx - 0.5 + wav, rim + 1.5 + k, al)
    if ring:   # las 8 cargas que va a soltar, alrededor de la campana
        r, lvl = ring
        for i in range(8):
            ang = i * math.pi / 4
            px, py = cx + math.cos(ang) * r, rim - ry * 0.4 + math.sin(ang) * r
            if lvl >= 3: sparks(cv, px, py, 1)
            else:
                cv.put(px, py, C('#ffffff') if lvl >= 2 else C(pal['cn']))
                if lvl >= 2:
                    for ddx, ddy in ((1, 0), (-1, 0), (0, 1), (0, -1)): cv.put_out(px + ddx, py + ddy, C(pal['tent'][0], 160))
    return cv

for i, (p, b) in enumerate(((0, 0), (0.6, 0), (1.1, 1), (0.6, 0))):
    anims.setdefault('BlueJelly_Move', []).append(blue_jelly(pulse=p, phase=i, dy=b))
# aviso: se llena, se encienden el borde y las 8 cargas a su alrededor
anims['BlueJelly_Warn'] = [blue_jelly(n=32, pulse=0.8, glow=1, red=True, ring=(8, 1)),
                           blue_jelly(n=32, pulse=1.2, glow=1, red=True, phase=1, lift=2, ring=(9, 1)),
                           blue_jelly(n=32, pulse=1.5, glow=2, red=True, phase=2, lift=3, ring=(9.5, 2)),
                           blue_jelly(n=32, pulse=1.7, glow=2, red=True, phase=3, lift=3, dy=1, ring=(10, 2))]
# disparo: se contrae de golpe y suelta el anillo (los proyectiles salen en el cuadro 0)
anims['BlueJelly_Shoot'] = [blue_jelly(n=32, pulse=-1.6, glow=3, tent=8, dy=-1, ring=(11, 3)),
                            blue_jelly(n=32, pulse=-1.0, glow=1, tent=8, dy=-2, phase=1),
                            blue_jelly(n=32, pulse=0.0, phase=2, dy=-1),
                            blue_jelly(n=32, pulse=0.5, phase=3)]
anims['BlueJelly_Hurt'] = [blue_jelly(pal=WHITE_J, eyes='wide', pulse=-0.6), blue_jelly(pal=RED_J, eyes='wide', pulse=0.6)]

def jelly_death():
    fr = [blue_jelly(pal=WHITE_J, eyes='wide', n=32),
          blue_jelly(pal=PALE_J, eyes='x', n=32, pulse=0.6),
          blue_jelly(pal=PALE_J, eyes='x', n=32, pulse=1.6, droop=3, dy=1),
          blue_jelly(pal=PALE_J, eyes='x', n=32, pulse=2.4, droop=5, dy=3, tent=4)]
    for step in (1, 2):
        cv = Canvas(32)
        if step == 1: ellipse(cv, 16, 22, 6, 2.4, PALE_J['ramp'], 190)
        for k in range(7):
            ang = k * 0.9 + step
            gx, gy = 16 + math.cos(ang) * (4 + step * 3), 19 - step * 3 - (k % 3) * 2
            ellipse(cv, gx, gy, 1.1, 1.1, PALE_J['ramp'], 200 - step * 50)
        cv.outline(); fr.append(cv)
    return fr
anims['BlueJelly_Death'] = jelly_death()

# =========================================================================================
# OTTOIA (32×32): gusano enterrado. El agujero está en (16, 24): pivote (0,5; 0,25)
# =========================================================================================
WORM = ['#5a1e2e', '#a43e52', '#e07a84', '#ffc8c4']
HX, HY = 16, 24

def sand_mound(cv, rx=5.0, ry=2.4, bump=0.0, dx=0, cracks=0):
    ellipse(cv, HX + dx, HY - bump * 0.5, rx, ry + bump * 0.4, SAND)
    if bump > 0: ellipse(cv, HX + dx + 0.5, HY - 1 - bump, 2.6, 1.2 + bump * 0.3, SAND, shift=1)
    for k in range(cracks):   # grietas que se abren antes de salir
        a = k * 2.4 + 0.4
        for t in range(1, 4 + k % 2):
            cv.put_in(HX + dx + math.cos(a) * t * 1.1, HY - 1 + math.sin(a) * t * 0.5, C(SAND[0]))

def hole(cv, rx=4.2, ry=1.8):
    ellipse(cv, HX, HY + 0.4, rx + 1.8, ry + 1.0, SAND)
    for y in range(32):
        for x in range(32):
            if ((x + .5 - HX) / rx) ** 2 + ((y + .5 - HY) / ry) ** 2 <= 1: cv.px[(x, y)] = C('#21180f')

def worm_path(cv, pts, pal, hooks=True, open_=0.0, eyes='angry', red=False, mask_y=None):
    """pts: [(x, y, r)] de la base a la punta. Discos sombreados uno por uno: así se ven los anillos."""
    for i, (x, y, r) in enumerate(pts):
        band = -1 if i % 3 == 0 else 0
        def inside(px, py, x=x, y=y, r=r): return (px - x) ** 2 + (py - y) ** 2 <= r * r and (mask_y is None or py <= mask_y)
        fill(cv, inside, x, y, r, r, pal, shift=band)
    tx, ty, tr = pts[-1]
    if open_ > 0:
        mry = 0.8 + open_ * 1.4
        for yy in range(32):
            for xx in range(32):
                d = ((xx + .5 - tx) / (tr * 0.75)) ** 2 + ((yy + .5 - (ty - 0.5)) / mry) ** 2
                if d <= 1: cv.px[(xx, yy)] = MOUTH
                elif d <= 1.7 and (xx + yy) % 2 == 0 and (xx, yy) in cv.px: cv.px[(xx, yy)] = BONE
    else:
        cv.put_in(tx - 1, ty - 1, OUTL); cv.put_in(tx, ty - 1, OUTL)
    if eyes != 'none' and len(pts) > 4:
        ex, ey, _ = pts[-3]
        eye_front(cv, ex - 2.5, ey, -1, eyes, red); eye_front(cv, ex + 0.5, ey, 1, eyes, red)
    cv.outline()
    if hooks:   # ganchos de la probóscide, pálidos y hacia atrás
        for i in range(max(0, len(pts) - 7), len(pts) - 1, 2):
            x, y, r = pts[i]
            for s in (-1, 1):
                cv.put_out(x + s * (r + 1.2), y + 0.8, C('#fff0d8'))
                cv.put_out(x + s * (r + 1.6), y + 1.6, C('#e0c8b0', 200))

def standing(h, bend=0.0, r=2.8):
    pts = []
    for t in range(int(h) + 1):
        f = t / max(1, h)
        pts.append((HX + bend * f * f, HY - t, r + (0.5 if t > h - 6 else 0)))
    return pts

def ottoia(h, bend=0.0, open_=0.0, pal=WORM, eyes='angry', red=False, spray=0, flash=None, hole_r=4.2):
    cv = Canvas(32)
    hole(cv, hole_r, hole_r * 0.43)
    if h > 0: worm_path(cv, standing(h, bend), pal, open_=open_, eyes=eyes, red=red, mask_y=HY + 0.6)
    else: cv.outline()
    rnd = random.Random(spray)
    for k in range(spray * 3):   # arena que salta
        a = rnd.uniform(math.pi * 1.05, math.pi * 1.95); d = rnd.uniform(4, 6 + spray * 2)
        cv.put_out(HX + math.cos(a) * d, HY + math.sin(a) * d * 0.8, C(SAND[3 if k % 2 else 2]))
    if flash: sparks(cv, *flash)
    return cv

def burrowed(i, cracks=0, shake=0, ring=None, grains=0):
    cv = Canvas(32)
    if ring:   # el círculo de aviso: radio 0,8 u (13 px)
        r, lvl = ring
        col = C('#ff6a4a', 140 + lvl * 35)
        for k in range(72):
            a = k * math.pi / 36 + i * 0.12
            if (k // 3 + i) % 2 and lvl < 3: continue
            cv.put(HX + math.cos(a) * r, HY - 2 + math.sin(a) * r * 0.75, col)
    sand_mound(cv, bump=[0.6, 1.2, 0.8, 0.2][i % 4] if not shake else 1.2, dx=shake, cracks=cracks)
    cv.outline()
    for k in range(2):   # ondas en la arena
        rr = 5.5 + ((i + k * 2) % 4) * 1.3
        for a10 in range(-14, 15):
            a = math.pi + a10 * 0.1
            for s in (1, -1):
                cv.put_out(HX + math.cos(a) * rr * s, HY + math.sin(a10 * 0.1) * rr * 0.45 + 0.5, C(SAND[2], 190 - ((i + k * 2) % 4) * 40))
    rnd = random.Random(i + 10 * grains)
    for k in range(grains):
        cv.put_out(HX + rnd.uniform(-5, 5), HY - 3 - rnd.uniform(0, 2 + grains), C(SAND[3]))
    return cv

anims['Ottoia_Burrowed'] = [burrowed(i) for i in range(4)]
# aviso 0,8 s: la arena se agrieta y tiembla dentro del círculo
anims['Ottoia_Warn'] = [burrowed(0, cracks=1, ring=(13, 1), grains=1),
                        burrowed(1, cracks=2, shake=1, ring=(12, 2), grains=2),
                        burrowed(2, cracks=3, shake=-1, ring=(11.5, 2), grains=3),
                        burrowed(3, cracks=4, shake=1, ring=(11, 3), grains=4)]
# sale, abre la boca, muerde (cuadro 3) y se queda un momento
anims['Ottoia_Attack'] = [ottoia(5, spray=3), ottoia(12, open_=0.4, spray=2), ottoia(17, bend=-1, open_=1.0, red=True),
                          ottoia(16, bend=3, open_=0.0, flash=(22, 6, 2)), ottoia(15, bend=2, flash=(21, 7, 1)), ottoia(14, bend=1)]
anims['Ottoia_Sink'] = [ottoia(9), ottoia(4), ottoia(1, eyes='none'), ottoia(0, hole_r=2.4, spray=1)]
anims['Ottoia_Hurt'] = [ottoia(14, pal=WHITE4, eyes='wide'), ottoia(13, pal=RED4, eyes='wide', bend=-1)]

def ottoia_death():
    fr = [ottoia(14, pal=WHITE4, eyes='wide'), ottoia(14, bend=4, pal=PALE4, eyes='x')]
    for k, alpha_puff in ((0, 0), (1, 2), (2, 4)):
        cv = Canvas(32); hole(cv)
        if k < 2:   # cae tumbado sobre la arena
            pts = [(HX + t * 0.75, HY - 1 - math.sin(t * 0.35) * (2 - k) - t * 0.15, 2.6 - k * 0.3) for t in range(15)]
            worm_path(cv, pts, PALE4, hooks=k == 0, eyes='x' if k == 0 else 'none')
        else: cv.outline()
        for j in range(alpha_puff): puff(cv, HX + 3 + j * 2.5, HY - 2 - (j % 2) * 2, 1.6 + k * 0.3)
        fr.append(cv)
    cv = Canvas(32); sand_mound(cv, rx=4, ry=1.8); cv.outline(); fr.append(cv)
    return fr
anims['Ottoia_Death'] = ottoia_death()

# =========================================================================================
# ESPONJA VAUXIA (32×32, inmóvil): tubos ramificados con ósculos arriba
# =========================================================================================
SP = ['#5a4012', '#9a7424', '#d4aa44', '#fbe58c']
TUBES = [((12.5, 25), (7.5, 12), 2.3, 3.2, 1.0),     # (base, punta, radio base, radio punta, cuánto se mece)
         ((19.5, 23), (24.5, 10), 2.5, 3.3, 1.0),
         ((16, 28), (16, 7), 3.6, 4.6, 0.5)]

def vauxia(pal=SP, sway=0.0, grow=0.0, glow=0, eyes='angry', red=False, droop=0.0, motes=None, pores=0, n=32):
    cv = Canvas(n)
    ellipse(cv, 16, 28.5, 6.5, 2, STONE4)          # roca donde se agarra
    tops = []
    for (bx, by), (tx, ty), r0, r1, sw in TUBES:
        tx += math.sin(sway) * sw + droop * (tx - bx) * 0.25
        ty += droop * 3 * sw
        r0 += grow; r1 += grow
        def inside(px, py, bx=bx, by=by, tx=tx, ty=ty, r0=r0, r1=r1):
            vx, vy = tx - bx, ty - by; l2 = vx * vx + vy * vy
            t = max(0, min(1, ((px - bx) * vx + (py - by) * vy) / l2))
            return math.hypot(px - bx - vx * t, py - by - vy * t) <= r0 + (r1 - r0) * t
        fill(cv, inside, (bx + tx) / 2 - 1, (by + ty) / 2 - 2, r1 * 1.2, abs(by - ty) * 0.6, pal)
        # poros: la red de la esponja
        for y in range(n):
            for x in range(n):
                if inside(x + .5, y + .5) and (x * 3 + y * 5 + pores) % 7 == 0 and y > ty + 2:
                    cv.px[(x, y)] = HEAL if glow >= 2 else C(pal[0])
        tops.append((tx, ty, r1))
    for tx, ty, r1 in tops:   # ósculos
        for y in range(n):
            for x in range(n):
                d = ((x + .5 - tx) / (r1 * 0.7)) ** 2 + ((y + .5 - ty) / 1.3) ** 2
                if d <= 1: cv.px[(x, y)] = HEAL if glow >= 3 else C('#a6ffb8') if glow == 2 else C('#2a1a08')
                elif d <= 2.0 and (x, y) in cv.px and y < ty: cv.px[(x, y)] = C(pal[3])
    eye_front(cv, 13.5, 17, -1, eyes, red); eye_front(cv, 16.5, 17, 1, eyes, red)
    cv.outline()
    for tx, ty, r1 in tops:   # espículas, pegadas al borde de cada tubo
        for k in range(1, 10, 3):
            y = int(ty + 2 + k)
            for s in (-1, 1):
                x = int(tx)
                while 0 <= x + s < n and (x + s, y) in cv.px: x += s
                if (x, y) in cv.px and cv.px[(x, y)] == OUTL: cv.put_out(x + s, y - 1, C('#fff4c0', 220))
    if glow:
        for tx, ty, r1 in tops: cv.put_out(tx, ty - 2, C(HEAL, 120 + glow * 40))
    if motes:   # chispas verdes que suben de los ósculos
        h, a = motes
        for i, (tx, ty, r1) in enumerate(tops):
            mx, my = tx + (i - 1) * 0.5, ty - 3 - h - i % 2
            cv.put(mx, my, C('#ffffff', a))
            for ddx, ddy in ((1, 0), (-1, 0), (0, 1), (0, -1)): cv.put_out(mx + ddx, my + ddy, C(HEAL, a))
    return cv

anims['Vauxia_Idle'] = [vauxia(sway=i * math.pi / 2 * 0.6 - 0.45, pores=i) for i in range(4)]
# cura (cada 3 s): se hincha, se ilumina en verde y suelta el pulso (cuadro 2)
anims['Vauxia_Heal'] = [vauxia(grow=0.3, glow=1), vauxia(grow=0.6, glow=2, motes=(0, 200)),
                        vauxia(grow=-0.2, glow=3, motes=(2, 255), red=False),
                        vauxia(glow=1, motes=(5, 180)), vauxia(motes=(8, 90))]
anims['Vauxia_Hurt'] = [vauxia(pal=WHITE4, eyes='wide', grow=-0.3), vauxia(pal=RED4, eyes='wide', grow=0.3)]

def vauxia_death():
    fr = [vauxia(pal=WHITE4, eyes='wide'), vauxia(pal=PALE4, eyes='x', droop=0.4), vauxia(pal=PALE4, eyes='x', droop=1.0)]
    base = vauxia(pal=PALE4, eyes='none', droop=1.0)
    for step in (1, 2):   # se desmorona en trozos que caen
        cv = Canvas(32); rnd = random.Random(step)
        for (x, y), c in base.px.items():
            if c == OUTL: continue
            if rnd.random() < 0.25 * step: continue
            fall = int(max(0, (26 - y)) * 0.18 * step + rnd.random() * step)
            cv.put(x + rnd.choice((-1, 0, 1)) * (step - 1), min(29, y + fall), c)
        cv.outline(); fr.append(cv)
    cv = Canvas(32); ellipse(cv, 16, 28.5, 6.5, 2, STONE4)
    rnd = random.Random(7)
    for k in range(14): cv.put(16 + rnd.uniform(-7, 7), 26 + rnd.uniform(-1.5, 1), C(PALE4[2 + k % 2]))
    cv.outline(); fr.append(cv)
    return fr
anims['Vauxia_Death'] = vauxia_death()

# =========================================================================================
# PIKAIA (16×16, ~12 px): pez primitivo del cardumen
# =========================================================================================
PIK = ['#2e4a44', '#5e8a7a', '#9ccaa8', '#e4fff0']

def pikaia(pal=PIK, phase=0, amp=1.0, n=16, eyes='angry', flip=False, curl=0.0, streak=0, alpha=255, dx=0):
    cv = Canvas(n); o = (n - 16) / 2 + dx
    def yc(px): return 8 + o * 0 + (n - 16) / 2 + amp * math.sin(phase * math.pi / 2 - px * 0.6) * max(0, (14.5 + o - px) / 12) ** 1.4 \
        + curl * max(0, (14 + o - px) / 6) ** 2
    def hh(px):
        t = (px - 2 - o) / 12
        return 0 if t < 0 or t > 1 else 0.6 + 1.8 * math.sin(min(1, t * 1.15) * math.pi) ** 0.8
    fill(cv, lambda px, py: abs(py - yc(px)) <= hh(px), 9 + o, 8 + (n - 16) / 2, 6, 2.5, pal, alpha)
    for k, mx in enumerate((5, 7, 9, 11)):   # miómeros en V
        for d in (-1, 0, 1):
            cv.put_in(mx + o + abs(d), yc(mx + o + abs(d)) + d, C(pal[1], alpha))
    ey = yc(12.5 + o)
    if eyes == 'angry': cv.put_in(12 + o, ey - 1, EYE); cv.put_in(13 + o, ey - 1, OUTL)
    elif eyes == 'x': cv.put_in(12 + o, ey - 1, OUTL); cv.put_in(13 + o, ey, OUTL)
    elif eyes == 'wide': cv.put_in(12 + o, ey - 1, EYE); cv.put_in(13 + o, ey - 1, EYE)
    cv.outline()
    for x in range(int(2 + o), int(7 + o)):   # aleta de la cola, translúcida
        cv.put_out(x, yc(x + .5) - hh(x + .5) - 1.2, C(pal[3], 150))
        cv.put_out(x, yc(x + .5) + hh(x + .5) + 1.2, C(pal[3], 150))
    cv.put_out(15 + o, yc(15 + o) - 1, C(pal[2], alpha)); cv.put_out(15 + o, yc(15 + o) + 1, C(pal[2], alpha))   # tentáculos
    for k in range(streak):
        for v in (-1, 1):
            for t in range(2 + k):
                cv.put_out(o - t - k, 8 + (n - 16) / 2 + v * 2 + t % 2 * 0, C('#bfe6ff', 170 - t * 40))
    im = cv.img()
    return im.transpose(Image.FLIP_TOP_BOTTOM) if flip else im

anims['Pikaia_Swim'] = [pikaia(phase=i) for i in range(4)]
anims['Pikaia_Dash'] = [pikaia(n=24, phase=i, amp=0.35, dx=2, streak=2 + i % 2) for i in range(3)]
anims['Pikaia_Hurt'] = [pikaia(pal=WHITE4, eyes='wide', amp=0.4), pikaia(pal=RED4, eyes='wide', phase=2)]

def pikaia_death():
    fr = [pikaia(pal=WHITE4, eyes='wide'), pikaia(pal=PALE4, eyes='x', flip=True, amp=0.3),
          pikaia(pal=PALE4, eyes='x', flip=True, curl=1.5, amp=0)]
    for step in (1, 2):
        cv = Canvas(16); rnd = random.Random(step)
        for k in range(10 - step * 3):
            cv.put(4 + rnd.uniform(0, 9), 8 - step * 2 + rnd.uniform(-2, 2), C(PALE4[2 + k % 2], 230 - step * 70))
        fr.append(cv)
    return fr
anims['Pikaia_Death'] = pikaia_death()

anims = {k: [as_img(x) for x in v] for k, v in anims.items()}

# =========================================================================================
# Efectos y proyectiles
# =========================================================================================
def jelly_orb():
    """8×8: carga urticante de la medusa (sale en anillo, sin dirección)."""
    fr = []
    for k in range(2):
        cv = Canvas(8)
        ellipse(cv, 4, 4, 2.6, 2.6, ['#2f62c4', '#6aa8ee', '#9ef4ff', '#ffffff'])
        cv.put(3 + k, 3, C('#ffffff')); cv.outline()
        if k: cv.put(1, 1, C('#9ef4ff', 160))
        else: cv.put(6, 6, C('#9ef4ff', 160))
        fr.append(cv.img())
    return fr

def jelly_impact():
    fr = []
    for r, a in ((2, 255), (4, 230), (5.5, 160), (6.5, 80)):
        im = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
        for k in range(16):
            ang = k * math.pi / 8
            x, y = round(7.5 + math.cos(ang) * r), round(7.5 + math.sin(ang) * r)
            if 0 <= x < 16 and 0 <= y < 16: im.putpixel((x, y), (158, 244, 255, a) if k % 2 else (255, 255, 255, a))
        if r < 3: im.putpixel((8, 8), (255, 255, 255, 255))
        fr.append(im)
    return fr

def heal_pulse():
    """168×168: onda de curación de la esponja hasta 5,2 u (83 px de radio)."""
    fr = []
    N = 168; M = N // 2
    for r, a in ((10, 255), (29, 230), (47, 200), (62, 150), (74, 100), (82, 50)):
        im = Image.new('RGBA', (N, N), (0, 0, 0, 0))
        steps = int(2 * math.pi * r * 1.5)
        for k in range(steps):
            ang = k * 2 * math.pi / steps
            for dr in (0, -1):
                x, y = int(M + math.cos(ang) * (r + dr)), int(M + math.sin(ang) * (r + dr))
                im.putpixel((x, y), (122, 255, 154, a if dr == 0 else a // 2))
        for k in range(8):   # cruces a lo largo de la onda
            ang = k * math.pi / 4 + r * 0.02
            x, y = int(M + math.cos(ang) * (r - 3)), int(M + math.sin(ang) * (r - 3))
            for ddx, ddy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
                if 0 <= x + ddx < N and 0 <= y + ddy < N:
                    im.putpixel((x + ddx, y + ddy), (255, 255, 255, a) if (ddx, ddy) == (0, 0) else (122, 255, 154, a))
        fr.append(im)
    return fr

def shield():
    """32×32 en bucle: escudo de la esponja sobre un enemigo curado."""
    fr = []
    for k in range(4):
        im = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
        for y in range(32):
            for x in range(32):
                d = math.hypot(x + .5 - 16, y + .5 - 16)
                if 12.4 <= d <= 13.4:
                    ang = math.atan2(y + .5 - 16, x + .5 - 16)
                    shine = math.cos(ang - k * math.pi / 2 + 2.3)
                    im.putpixel((x, y), (255, 255, 255, 230) if shine > 0.85 else (138, 255, 192, 190))
                elif d < 12.4 and (x + y + k) % 4 == 0 and d > 10.5:
                    im.putpixel((x, y), (138, 255, 192, 60))
        fr.append(im)
    return fr

def heal_motes():
    """16×16: cruces verdes que suben sobre el enemigo curado."""
    fr = []
    for k in range(5):
        im = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
        for i, (x0, y0) in enumerate(((4, 12), (10, 13), (7, 10))):
            y = y0 - k * 2 - i; a = max(0, 255 - k * 45 - i * 20)
            if 1 <= y < 15:
                for ddx, ddy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
                    im.putpixel((x0 + ddx, y + ddy), (255, 255, 255, a) if (ddx, ddy) == (0, 0) else (122, 255, 154, a))
        fr.append(im)
    return fr

def heal_thread():
    """8×8: cuenta de luz del hilo que une la esponja con el enemigo que cura. Se ponen en hilera, a 1:1, de la
    esponja al curado; cada una se enciende (cuadro 0) y se apaga (cuadros 1-3)."""
    fr = []
    for k, a in enumerate((255, 210, 140, 70)):
        im = Image.new('RGBA', (8, 8), (0, 0, 0, 0))
        r = 2.6 - k * 0.45
        for y in range(8):
            for x in range(8):
                d = math.hypot(x + .5 - 4, y + .5 - 4)
                if d <= r * 0.45 and k < 2:
                    im.putpixel((x, y), (255, 255, 255, a))
                elif d <= r:
                    im.putpixel((x, y), (122, 255, 154, a))
                elif d <= r + 1.1 and k < 3:
                    im.putpixel((x, y), (60, 170, 110, a // 2))
        fr.append(im)
    return fr

fx = {'Proj_BlueJelly': jelly_orb(), 'Fx_BlueJelly_Impact': jelly_impact(), 'Fx_Vauxia_HealPulse': heal_pulse(),
      'Fx_Vauxia_Shield': shield(), 'Fx_Vauxia_HealMotes': heal_motes(), 'Fx_Vauxia_Thread': heal_thread()}

# =========================================================================================
# Guardado, vistas previas y GIF
# =========================================================================================
ENEMIES = ('Trilobite', 'BlueJelly', 'Ottoia', 'Vauxia', 'Pikaia')

def save_sheet(path, frames):
    w, h = frames[0].size
    sheet = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, im in enumerate(frames): sheet.paste(im, (i * w, 0))
    sheet.save(path)

for e in ENEMIES:
    os.makedirs(f'{OUT}/{e}', exist_ok=True)
    for name, frames in anims.items():
        if name.startswith(e + '_'): save_sheet(f'{OUT}/{e}/{name}.png', frames)
os.makedirs(f'{OUT}/Effects', exist_ok=True)
for name, frames in fx.items(): save_sheet(f'{OUT}/Effects/{name}.png', frames)

BG = C('#0f2a4a')                                               # arrecife
BANDS = [('Someras', C('#1f6a7a')), ('Arrecife', BG), ('Abismo', C('#070b16'))]
S = 4

def preview():
    rows = list(anims)
    cell = 32 * S + 10; rh = 32 * S + 10
    W = 190 + cell * 7; H = 10 + len(rows) * rh
    bg = Image.new('RGBA', (W, H), BG); d = ImageDraw.Draw(bg)
    for r, name in enumerate(rows):
        y0 = 10 + r * rh
        d.text((10, y0 + rh // 2 - 6), name.replace('_', ' '), fill=C('#bfe6ff'))
        for i, im in enumerate(anims[name]):
            big = im.resize((im.width * S, im.height * S), Image.NEAREST)
            bg.alpha_composite(big, (190 + i * cell + (cell - big.width) // 2, y0 + (rh - big.height) // 2))
    bg.save(f'{OUT}/vista_previa_enemigos_piso2.png')

def preview_bands():
    """Primer cuadro de cada enemigo sobre las tres franjas de profundidad, a ×4."""
    keys = ['Trilobite_Move', 'Trilobite_Warn', 'BlueJelly_Move', 'Ottoia_Burrowed', 'Ottoia_Attack', 'Vauxia_Idle', 'Pikaia_Swim']
    idx = {'Trilobite_Warn': 2, 'Ottoia_Attack': 2}
    cell = 36 * S
    im = Image.new('RGBA', (cell * len(keys), cell * 3), BG)
    for b, (_, col) in enumerate(BANDS):
        im.paste(Image.new('RGBA', (cell * len(keys), cell), col), (0, b * cell))
        for k, key in enumerate(keys):
            f = anims[key][idx.get(key, 0)]
            big = f.resize((f.width * S, f.height * S), Image.NEAREST)
            im.alpha_composite(big, (k * cell + (cell - big.width) // 2, b * cell + (cell - big.height) // 2))
    im.save(f'{OUT}/vista_previa_franjas.png')

def preview_fx():
    W = 20 + sum(max(f[0].width, 32) * 2 + 10 for f in fx.values()) * 1
    im = Image.new('RGBA', (1400, 1200), BG); y = 10
    for name, frames in fx.items():
        sc = 2 if frames[0].width > 32 else 6
        x = 10
        for f in frames:
            big = f.resize((f.width * sc, f.height * sc), Image.NEAREST)
            im.alpha_composite(big, (x, y)); x += big.width + 10
        y += frames[0].height * sc + 14
    im.crop((0, 0, 1400, y)).save(f'{OUT}/vista_previa_efectos.png')

preview(); preview_bands(); preview_fx()

SPEED = {'Move': 120, 'Swim': 110, 'Warn': 150, 'Charge': 70, 'Flip': 60, 'Stunned': 120, 'Recover': 70, 'Hurt': 90,
         'Death': 120, 'Shoot': 80, 'Burrowed': 160, 'Attack': 70, 'Sink': 90, 'Idle': 180, 'Heal': 110, 'Dash': 60}
os.makedirs(f'{OUT}/gif', exist_ok=True)
def gif(name, frames, ms):
    n = max(32, frames[0].width)
    out = []
    for im in frames:
        b = Image.new('RGBA', (n, n), BG); b.alpha_composite(im, ((n - im.width) // 2, (n - im.height) // 2))
        out.append(b.resize((n * 6 if n <= 32 else n * 2, n * 6 if n <= 32 else n * 2), Image.NEAREST).convert('RGB'))
    out[0].save(f'{OUT}/gif/{name}.gif', save_all=True, append_images=out[1:], duration=ms, loop=0)
for name, frames in anims.items(): gif(name, frames, SPEED[name.split('_')[1]])
for name, frames in fx.items(): gif(name, frames, 80)
print('Listo:', OUT)
