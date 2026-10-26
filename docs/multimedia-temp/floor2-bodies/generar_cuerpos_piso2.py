"""Cuerpos del jugador del piso 2 (océano cámbrico) de Primordia.

Medusa (proyectil, Boca: Cnidocitos) y pez acorazado (cuerpo a cuerpo, Boca: Placas mandibulares).
Desde el piso 2 la Boca va dibujada en el cuerpo y cada cuerpo tiene su propia animación de ataque.
Mismo estilo que ../player/generar_jugador.py: 16 px por unidad, luz arriba a la izquierda, contorno oscuro.

Uso: python generar_cuerpos_piso2.py [carpeta_salida]   (por defecto, la carpeta de este script)
"""
import math, sys, os, random
from PIL import Image, ImageDraw

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__))
N = 24          # lienzo de los cuerpos (el cuerpo ocupa unos 20 px)
ND = 32         # lienzo de la muerte
OUTL = (24, 20, 37, 255)

def C(h, a=255):
    h = h.lstrip('#'); return tuple(int(h[i:i+2], 16) for i in (0, 2, 4)) + (a,)

EYE = C('#f4f4e8')
MOUTH = C('#4a1428')
BONE = C('#f4ecd8')
SPARK = [C('#ffffff'), C('#fff6c0'), C('#ffe080')]

# --- paletas -------------------------------------------------------------------------------
FISH = dict(ramp=['#7a5424', '#b8843a', '#e3b85e', '#fff0b4'],        # cuerpo dorado
            plate=['#353a4c', '#5f6880', '#9ea8c0', '#e2e8f4'],       # placas de hueso
            fin='#d0763a', find='#8a4424')
JELLY = dict(ramp=['#6a2a7c', '#a648aa', '#e07ad2', '#ffd2f4'],       # campana
             nuc='#7a2a8a', tent=['#f2a2e2', '#c862b8'], arm='#ffc8ee',
             cnido='#fff2a0', cnido_hot='#ffffff')
WHITE_F = dict(ramp=['#ffffff'] * 4, plate=['#ffffff'] * 4, fin='#ffffff', find='#ffffff')
RED_F = dict(ramp=['#a33b3b', '#e05a5a', '#ff9a8a', '#fff0e0'], plate=['#7a2a2a', '#b84a4a', '#e88a80', '#ffe0d8'],
             fin='#e05a5a', find='#a33b3b')
PALE_F = dict(ramp=['#6a6450', '#9a9478', '#c4c0a4', '#ecead8'], plate=['#4a4e58', '#767c8a', '#a8aeb8', '#dcdfe4'],
              fin='#9a9478', find='#6a6450')
WHITE_J = dict(ramp=['#ffffff'] * 4, nuc='#ffffff', tent=['#ffffff'] * 2, arm='#ffffff', cnido='#ffffff', cnido_hot='#ffffff')
RED_J = dict(ramp=['#a33b3b', '#e05a5a', '#ff9a8a', '#fff0e0'], nuc='#8a3030', tent=['#ff9a8a', '#e05a5a'],
             arm='#fff0e0', cnido='#fff0e0', cnido_hot='#ffffff')
PALE_J = dict(ramp=['#6a5a74', '#9a88a2', '#c4b4c8', '#ece0ee'], nuc='#7a6a84', tent=['#c4b4c8', '#9a88a2'],
              arm='#ece0ee', cnido='#d8d0c0', cnido_hot='#ece0ee')

# --- lienzo --------------------------------------------------------------------------------
class Canvas:
    def __init__(s, n=N): s.n = n; s.px = {}
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
    """Índice 0-3 de la rampa con luz arriba a la izquierda (como la célula)."""
    v = (-0.6*nx - 0.8*ny) * 0.8 + 0.35 - max(0, math.hypot(nx, ny) - 0.75) * 1.2
    return 0 if v < -0.15 else 1 if v < 0.35 else 2 if v < 0.75 else 3

def ellipse(cv, cx, cy, rx, ry, ramp, keep=lambda px, py: True, lean=0.0):
    ramp = [C(c) for c in ramp]
    for y in range(cv.n):
        for x in range(cv.n):
            px, py = x + .5, y + .5
            t = (cy - py) / ry
            nx, ny = (px - cx - lean*max(0, t)) / rx, (py - cy) / ry
            if nx*nx + ny*ny <= 1 and keep(px, py):
                cv.px[(x, y)] = ramp[shade(nx, ny)]

def poly(cv, pts, col, only_empty=False):
    col = C(col) if isinstance(col, str) else col
    xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
    for y in range(max(0, int(min(ys))), min(cv.n, int(max(ys)) + 1)):
        for x in range(max(0, int(min(xs))), min(cv.n, int(max(xs)) + 1)):
            px, py = x + .5, y + .5; inside = False
            for i in range(len(pts)):
                (x1, y1), (x2, y2) = pts[i], pts[i-1]
                if (y1 > py) != (y2 > py) and px < (x2 - x1) * (py - y1) / (y2 - y1) + x1: inside = not inside
            if inside and not (only_empty and (x, y) in cv.px): cv.px[(x, y)] = col

def eye(cv, ox, oy, look, state, h=2):
    """Ojo de la célula en 2×2 (los cuerpos nuevos conservan la cara del protagonista)."""
    if state == 'none': return
    if state == 'closed': cv.put(ox, oy+1, OUTL); cv.put(ox+1, oy+1, OUTL); return
    if state == 'x':
        for dx, dy in ((0,0),(1,1),(0,1),(1,0)): pass
        cv.put(ox, oy, OUTL); cv.put(ox+1, oy+1, OUTL); cv.put(ox+1, oy, OUTL); cv.put(ox, oy+1, OUTL); return
    for dx in range(2):
        for dy in range(h): cv.put(ox+dx, oy+dy, EYE)
    if state == 'wide': return
    if look == 'side': cv.put(ox+1, oy+h-2, OUTL); cv.put(ox+1, oy+h-1, OUTL)
    else: cv.put(ox, oy+h-1, OUTL); cv.put(ox+1, oy+h-1, OUTL)

def sparks(cv, x, y, size=2):
    """Destello del golpe: cruz blanca con puntas amarillas."""
    cv.put(x, y, SPARK[0])
    for k in range(1, size + 1):
        c = SPARK[min(k, 2)]
        for dx, dy in ((k,0),(-k,0),(0,k),(0,-k)): cv.put(x+dx, y+dy, c)
    if size >= 2:
        for dx, dy in ((1,1),(-1,-1),(1,-1),(-1,1)): cv.put(x+dx, y+dy, SPARK[1])

# =========================================================================================
# PEZ ACORAZADO (placodermo) con Placas mandibulares
# =========================================================================================
def open_jaw(cv, hx, my, open_px, front, lower_only_x=None, up_ratio=0.3, vertical=False, half=3.5, cx=0):
    """Abre la mandíbula: lo que queda bajo la línea de la boca baja y lo de arriba sube un poco.
    side: gira alrededor de la bisagra hx (más abierto delante). down: baja en bloque (vertical=True)."""
    if open_px <= 0: return
    moved = {}
    gap = {}
    for (x, y), c in cv.px.items():
        if vertical:
            inside = abs(x + .5 - cx) <= half
            f = 1.0 if inside else 0.0
        else:
            inside = x >= hx
            f = (x + .5 - hx) / max(1, front - hx) if inside else 0.0
        if inside and y > my:
            d = int(round(open_px * f)); moved[(x, y + d)] = c
            gap.setdefault(x, [None, None]); gap[x][1] = min(gap[x][1] or 99, y + d)
        elif inside and y < my:
            d = int(round(open_px * up_ratio * f)); moved[(x, y - d)] = c
            gap.setdefault(x, [None, None]); gap[x][0] = max(gap[x][0] or -1, y - d)
        elif inside and y == my:
            pass  # la línea de la boca desaparece al abrir
        else:
            moved.setdefault((x, y), c)
    cv.px = moved
    for x, (top, bot) in gap.items():
        if top is None or bot is None: continue
        for y in range(top + 1, bot): cv.px[(x, y)] = MOUTH
        # filo de hueso aserrado arriba y abajo (en un pez sin dientes, las placas cortan)
        if x % 2 == 0:
            if bot - top > 2: cv.px[(x, top + 1)] = BONE; cv.px[(x, bot - 1)] = BONE

def fish_side(cv, cx, cy, rx, ry, pal, tail=0.0, fin=0.0, open_px=0, eyes='open', squash=0.0):
    rx *= 1 + 0.1*squash; ry *= 1 - 0.1*squash
    back = cx - rx
    # cola heterocerca (lóbulo de arriba más largo), detrás del cuerpo
    poly(cv, [(back + 2, cy - 1.8), (back + 2, cy + 1.6), (back - 3.2, cy + 3.2 + tail*0.8),
              (back - 1.6, cy + 0.2 + tail), (back - 4.4, cy - 3.8 + tail*1.2)], pal['fin'])
    poly(cv, [(back + 0.5, cy - 0.6), (back + 0.5, cy + 0.4), (back - 3.6, cy - 3.2 + tail*1.2)], pal['find'])
    # aleta dorsal
    poly(cv, [(cx - 2.6, cy - ry + 0.9), (cx + 0.4, cy - ry + 0.9), (cx - 3.2, cy - ry - 2.0 - fin*0.4)], pal['fin'])
    # cuerpo
    ellipse(cv, cx, cy, rx, ry, pal['ramp'])
    # placas de la cabeza: la mitad delantera del cuerpo, con la misma luz
    hx = cx + 0.8
    pr = [C(c) for c in pal['plate']]
    for (x, y) in list(cv.px):
        px, py = x + .5, y + .5
        nx, ny = (px - cx) / rx, (py - cy) / ry
        if px >= hx and nx*nx + ny*ny <= 1: cv.px[(x, y)] = pr[shade(nx, ny)]
    for (x, y) in list(cv.px):  # costura de la placa (borde trasero) y remaches
        if int(math.floor(hx)) == x and cv.px[(x, y)] in pr: cv.px[(x, y)] = pr[0]
    cv.put_in(hx + 1.5, cy + ry - 1.6, pr[3])
    # aleta pectoral, bajo la costura
    poly(cv, [(cx - 1.4, cy + ry - 1.4), (cx + 1.2, cy + ry - 1.4), (cx - 2.4 - fin*0.6, cy + ry + 1.6 + fin*0.4)], pal['find'])
    # ojo en la placa
    eye(cv, int(cx + 2.6), int(cy - 2.2), 'side', eyes)
    # boca: línea de corte; al abrir, la mandíbula de abajo gira hacia abajo
    my = int(math.floor(cy + 1.2)); front = cx + rx
    if open_px <= 0:
        for x in range(int(hx + 1.5), int(front) + 1): cv.put_in(x, my, OUTL)
    else:
        open_jaw(cv, hx + 1.0, my, open_px, front)

def fish_down(cv, cx, cy, rx, ry, pal, tail=0.0, fin=0.0, open_px=0, eyes='open', squash=0.0):
    rx *= 1 + 0.1*squash; ry *= 1 - 0.1*squash
    # cola asomando por detrás (arriba)
    poly(cv, [(cx - 1, cy - ry + 1.5), (cx + 1, cy - ry + 1.5), (cx + 2.4 + tail, cy - ry - 2.4), (cx + tail*0.5, cy - ry - 1.2),
              (cx - 2.4 + tail, cy - ry - 2.6)], pal['fin'])
    # aletas pectorales a los lados
    for s in (-1, 1):
        poly(cv, [(cx + s*(rx - 1.2), cy + 0.4), (cx + s*(rx - 1.2), cy + 2.6), (cx + s*(rx + 2.0), cy + 3.0 + fin)], pal['find'])
    ellipse(cv, cx, cy, rx, ry, pal['ramp'])
    pr = [C(c) for c in pal['plate']]
    ptop = cy + 0.9  # las placas cubren la mitad de arriba (el casco visto de frente)
    for (x, y) in list(cv.px):
        px, py = x + .5, y + .5
        nx, ny = (px - cx) / rx, (py - cy) / ry
        if py < ptop and nx*nx + ny*ny <= 1: cv.px[(x, y)] = pr[shade(nx, ny)]
    for y in range(0, int(ptop)):  # costura central de las dos placas
        cv.put_in(cx - 0.5, y, pr[0])
    for x in range(cv.n):  # borde inferior del casco
        if (x, int(ptop) - 1) in cv.px and cv.px[(x, int(ptop) - 1)] in pr: cv.px[(x, int(ptop) - 1)] = pr[0]
    eye(cv, int(cx - 3.5), int(cy - 2.6), 'down', eyes)
    eye(cv, int(cx + 1.5), int(cy - 2.6), 'down', eyes)
    my = int(math.floor(cy + 2.6))
    if open_px <= 0:
        for x in range(int(cx - 2.5), int(cx + 2.5)): cv.put_in(x, my, OUTL)
    else:
        open_jaw(cv, 0, my, open_px, 0, vertical=True, half=3.2, cx=cx, up_ratio=0.0)

def fish_up(cv, cx, cy, rx, ry, pal, tail=0.0, fin=0.0, open_px=0, eyes='none', squash=0.0):
    rx *= 1 + 0.1*squash; ry *= 1 - 0.1*squash
    # cola abajo, hacia la cámara
    poly(cv, [(cx - 1.2, cy + ry - 1.5), (cx + 1.2, cy + ry - 1.5), (cx + 2.8 + tail, cy + ry + 3.4), (cx + tail*0.5, cy + ry + 2.0),
              (cx - 2.4 + tail, cy + ry + 3.0)], pal['fin'])
    for s in (-1, 1):
        poly(cv, [(cx + s*(rx - 1.2), cy + 0.2), (cx + s*(rx - 1.2), cy + 2.4), (cx + s*(rx + 2.0), cy + 2.8 + fin)], pal['find'])
    ellipse(cv, cx, cy, rx, ry, pal['ramp'])
    pr = [C(c) for c in pal['plate']]
    for (x, y) in list(cv.px):  # la nuca acorazada, arriba
        px, py = x + .5, y + .5
        nx, ny = (px - cx) / rx, (py - cy) / ry
        if py < cy - 1.2 and nx*nx + ny*ny <= 1: cv.px[(x, y)] = pr[shade(nx, ny)]
    for y in range(int(cy - 1.2), int(cy + ry - 1)):  # aleta dorsal vista desde atrás
        cv.put_in(cx - 0.5, y, pal['fin'])
    cv.put_in(cx - 0.5, cy - 1.2, pr[0])

FISH_DRAW = {'side': fish_side, 'down': fish_down, 'up': fish_up}
FISH_POS = {'side': (12.5, 12.5, 7.0, 4.4), 'down': (12, 12.5, 5.6, 5.8), 'up': (12, 10.5, 5.6, 5.8)}

def fish(facing, pal=FISH, tail=0.0, fin=0.0, open_px=0, eyes=None, squash=0.0, dx=0, dy=0, grow=0.0, flash=None, n=N):
    cv = Canvas(n); off = (n - N) / 2
    cx, cy, rx, ry = FISH_POS[facing]
    if eyes is None: eyes = 'none' if facing == 'up' else 'open'
    FISH_DRAW[facing](cv, cx + dx + off, cy + dy + off, rx + grow, ry + grow, pal, tail, fin, open_px, eyes, squash)
    cv.outline()
    if flash: sparks(cv, flash[0] + dx + off, flash[1] + dy + off, flash[2])
    return cv

# =========================================================================================
# MEDUSA con Cnidocitos
# =========================================================================================
def jelly(facing, pal=JELLY, pulse=0.0, phase=0, lean=0.0, trail=0.0, glow=0, eyes=None, dx=0, dy=0,
          tent=6.0, lift=0.0, flash=None, n=N, grow=0.0, droop=0.0):
    """pulse > 0: campana ancha y baja (se llena); pulse < 0: estrecha y alta (empuja el agua)."""
    cv = Canvas(n); off = (n - N) / 2
    if eyes is None: eyes = 'none' if facing == 'up' else 'open'
    cx = 12 + dx + off; rim = 13.0 + dy + off - pulse*0.6
    rx = (7.0 + grow) * (1 + 0.16*pulse); ry = (6.8 + grow) * (1 - 0.18*pulse)
    if facing == 'side':
        lean = lean or 2.2; trail = trail or 0.6
    # campana: media elipse con el borde festoneado
    ellipse(cv, cx, rim, rx, ry, pal['ramp'], keep=lambda px, py: py <= rim + 0.5, lean=lean)
    ramp = [C(c) for c in pal['ramp']]
    for x in range(int(cx - rx + 1), int(cx + rx)):   # festón del borde: lóbulos de 2 px
        if (x + phase//2) % 3 != 2: cv.put(x, rim + 0.5, ramp[1])
    # "núcleo" en trébol, recuerdo de la célula que fue
    ny0 = rim - ry*0.62
    for ox, oy in ((-1, 0), (0, 0), (-1, 1), (0, 1), (1, 1)):   # como el núcleo de la célula
        cv.put_in(cx - 2.5 + ox + lean*0.5, ny0 + oy, pal['nuc'])
    # cara
    ey = int(rim - ry*0.42)
    if facing == 'down':
        eye(cv, int(cx - 3.5), ey, 'down', eyes, 3); eye(cv, int(cx + 1.5), ey, 'down', eyes, 3)
    elif facing == 'side':
        eye(cv, int(cx + 1 + lean*0.3), ey, 'side', eyes, 3); eye(cv, int(cx + 4 + lean*0.3), ey, 'side', eyes, 3)
    # cnidocitos: puntos que brillan en el borde (la Boca de la medusa)
    hot = C(pal['cnido_hot']); cn = C(pal['cnido'])
    for i, x in enumerate(range(int(cx - rx + 1), int(cx + rx), 3)):
        cv.put_in(x, rim + 0.5, hot if glow >= 2 or (glow == 1 and i % 2 == 0) else cn)
    cv.outline()
    # tentáculos detrás de todo, sin contorno y algo translúcidos
    tl = [C(c, 235) for c in pal['tent']]
    strands = [cx - rx + 1.8, cx - rx*0.4, cx + rx*0.4, cx + rx - 1.8]
    length = max(1.0, tent - lift)
    for i, x0 in enumerate(strands):
        for k in range(int(length + droop)):
            wav = math.sin(phase*math.pi/2 + k*0.55 + i*1.6) * (0.75 if droop == 0 else 0.25) * min(1, k/2)
            x = x0 + wav - trail*k*0.32 + lift*0.2*(1 if x0 > cx else -1)
            cv.put_out(x, rim + 1.5 + k, tl[k % 2])
    # brazos orales en el centro, más gruesos y claros
    al = C(pal['arm'], 240)
    for k in range(int(length*0.75 + droop)):
        wav = math.sin(phase*math.pi/2 + k*0.5 + 2) * 0.5 * min(1, k/2)
        for s in (-0.6, 0.6):
            cv.put_out(cx + s + wav - trail*k*0.3, rim + 1.5 + k, al)
    if flash: sparks(cv, flash[0] + dx + off, flash[1] + dy + off, flash[2])
    return cv

# =========================================================================================
# Animaciones
# =========================================================================================
FACINGS = ('side', 'down', 'up')
def rotated(cv, deg): return cv.img().rotate(deg)
def as_img(f): return f if isinstance(f, Image.Image) else f.img()

anims = {}   # nombre -> lista de imágenes
# --- pez ---
for f in FACINGS:
    anims[f'Placoderm_Idle_{f}'] = [fish(f, tail=t, fin=fn, dy=b) for t, fn, b in ((0, 0, 0), (1, 0.5, 0), (0, 1, 1), (-1, 0.5, 0))]
    anims[f'Placoderm_Move_{f}'] = [fish(f, tail=t, fin=fn, dy=b, squash=s) for t, fn, b, s in
                                    ((-2, 1, 0, 0.3), (0, 0, -1, 0), (2, -0.5, -1, -0.3), (0, 0.5, 0, 0))]
    # esquiva: se enrosca y da una vuelta (como la rueda de la célula)
    curl = fish(f, tail=2.5, squash=1.2, eyes='closed', grow=-1)
    anims[f'Placoderm_Dodge_{f}'] = [as_img(curl), rotated(curl, -90), rotated(curl, -180), rotated(curl, -270),
                                     as_img(fish(f, squash=-1, tail=-1))]
# ataque: preparación (se echa atrás y abre), golpe (se lanza y cierra de golpe con destello), recuperación
anims['Placoderm_Attack_side'] = [
    fish('side', dx=-1, open_px=1, squash=0.3, tail=1),
    fish('side', dx=-2, open_px=3, squash=0.6, tail=2, fin=1),
    fish('side', dx=1, open_px=4, squash=-0.4, tail=-2),
    fish('side', dx=3, open_px=0, squash=-0.6, tail=-1, flash=(21, 13, 2)),
    fish('side', dx=2, open_px=0, squash=-0.2, tail=1, dy=-1 + 1, flash=(21, 13, 1)),
    fish('side', dx=0, tail=0)]
anims['Placoderm_Attack_down'] = [
    fish('down', dy=-1, open_px=1, squash=0.3),
    fish('down', dy=-2, open_px=2, squash=0.5, fin=1),
    fish('down', dy=1, open_px=3, grow=0.5, squash=-0.3),
    fish('down', dy=2, open_px=0, grow=0.8, squash=-0.4, flash=(12, 18, 2)),
    fish('down', dy=1, open_px=0, grow=0.4, flash=(12, 18, 1)),
    fish('down')]
anims['Placoderm_Attack_up'] = [
    fish('up', dy=1, squash=0.3, tail=1),
    fish('up', dy=2, squash=0.6, tail=2, fin=1),
    fish('up', dy=-1, squash=-0.4, tail=-2),
    fish('up', dy=-3, squash=-0.6, tail=-1, flash=(12, 4, 2)),
    fish('up', dy=-2, squash=-0.2, flash=(12, 4, 1)),
    fish('up')]
anims['Placoderm_Hurt'] = [as_img(fish('side', pal=WHITE_F, eyes='wide', squash=-0.6)),
                           as_img(fish('side', pal=RED_F, eyes='wide', squash=0.6))]

def fish_death():
    fr = [fish('side', pal=WHITE_F, eyes='wide', n=ND),
          fish('side', pal=PALE_F, eyes='x', n=ND, tail=1)]
    # panza arriba
    fr.append(fish('side', pal=PALE_F, eyes='x', n=ND).img().transpose(Image.FLIP_TOP_BOTTOM))
    # las placas se sueltan y el cuerpo se hunde entre burbujas
    for step in (1, 2, 3):
        cv = Canvas(ND)
        if step < 3:
            ellipse(cv, 16, 18 + step, 6.5, 3.6 - step, PALE_F['ramp'])
        poly(cv, [(18 - step, 12 + step*1.5), (23 + step, 12 + step*1.5), (23 + step, 15 + step*1.5), (19 - step, 15 + step*1.5)], PALE_F['plate'][2])
        poly(cv, [(18, 20 + step), (23 + step*1.5, 20 + step), (22 + step*1.5, 22 + step)], PALE_F['plate'][1])
        cv.outline()
        for k in range(3):
            bx, by = 10 + k*5 + step, 14 - step*3 - k*2
            for p in ((0, 0), (1, 0), (0, 1), (1, 1)): cv.put(bx + p[0], by + p[1], C('#d8f0ff', 200))
        fr.append(cv)
    cv = Canvas(ND)
    poly(cv, [(10, 25), (16, 25), (15, 27), (11, 27)], PALE_F['plate'][2])
    poly(cv, [(18, 26), (23, 26), (22, 27)], PALE_F['plate'][1])
    cv.outline(); fr.append(cv)
    return [as_img(x) for x in fr]
anims['Placoderm_Death'] = fish_death()

# --- medusa ---
for f in FACINGS:
    anims[f'Jellyfish_Idle_{f}'] = [jelly(f, pulse=p, phase=i, dy=b) for i, (p, b) in enumerate(((0, 0), (0.5, 0), (1, 1), (0.5, 0)))]
    tr = 1.0 if f == 'side' else 0.0
    anims[f'Jellyfish_Move_{f}'] = [jelly(f, pulse=p, phase=i, dy=b, trail=tr, tent=t) for i, (p, b, t) in
                                    enumerate(((1.2, 1, 5), (-1.2, -1, 7), (-0.6, -1, 7), (0.4, 0, 6)))]
    ball = jelly(f, pulse=-1.6, eyes='closed', tent=3, grow=-1)
    anims[f'Jellyfish_Dodge_{f}'] = [as_img(ball), rotated(ball, -90), rotated(ball, -180), rotated(ball, -270),
                                     as_img(jelly(f, pulse=1.2, tent=7))]
# ataque: se llena, levanta los tentáculos y suelta los cnidocitos de golpe con un destello en el borde
FLASH_J = {'side': (20, 10), 'down': (12, 19), 'up': (12, 4)}
for f in FACINGS:
    fx, fy = FLASH_J[f]; tr = 1.0 if f == 'side' else 0.0
    rec = {'side': (-1, 0), 'down': (0, -1), 'up': (0, 1)}[f]
    anims[f'Jellyfish_Attack_{f}'] = [
        as_img(jelly(f, pulse=0.8, glow=1, phase=0, trail=tr)),
        as_img(jelly(f, pulse=1.4, glow=2, phase=1, lift=3, trail=tr)),
        as_img(jelly(f, pulse=-1.4, glow=3, phase=2, tent=7, flash=(fx, fy, 2), trail=tr)),
        as_img(jelly(f, pulse=-0.8, glow=1, phase=3, dx=rec[0], dy=rec[1], tent=8, trail=-tr, flash=(fx, fy, 1))),
        as_img(jelly(f, pulse=0.3, phase=0, dx=rec[0], dy=rec[1], trail=tr)),
        as_img(jelly(f, pulse=0, phase=1, trail=tr))]
anims['Jellyfish_Hurt'] = [as_img(jelly('side', pal=WHITE_J, eyes='wide', pulse=-0.6)),
                           as_img(jelly('side', pal=RED_J, eyes='wide', pulse=0.6))]

def jelly_death():
    fr = [jelly('side', pal=WHITE_J, eyes='wide', n=ND),
          jelly('down', pal=PALE_J, eyes='x', n=ND, pulse=0.6),
          jelly('down', pal=PALE_J, eyes='x', n=ND, pulse=1.6, droop=3, dy=1),     # se desinfla
          jelly('down', pal=PALE_J, eyes='x', n=ND, pulse=2.4, droop=5, dy=3, tent=4)]
    for step in (1, 2):  # se deshace en gotas que suben
        cv = Canvas(ND)
        if step == 1: ellipse(cv, 16, 22, 6, 2.4, PALE_J['ramp'])
        for k in range(7):
            a = k * 0.9 + step
            gx, gy = 16 + math.cos(a)*(4 + step*3), 20 - step*3 - (k % 3)*2
            ellipse(cv, gx, gy, 1.1, 1.1, PALE_J['ramp'])
        cv.outline(); fr.append(cv)
    cv = Canvas(ND); ellipse(cv, 16, 26, 6.5, 1.5, PALE_J['ramp']); cv.put(14, 25, C('#ece0ee')); cv.outline(); fr.append(cv)
    return [as_img(x) for x in fr]
anims['Jellyfish_Death'] = jelly_death()


# =========================================================================================
# LIRIO DE MAR (crinoideo) con Corona de brazos
# =========================================================================================
CRINOID = dict(cup=['#5a4a1a', '#8a7a2a', '#c8b040', '#f0e080'],      # cáliz dorado
               arm=['#3a1a50', '#7a3aa0', '#b878e0'], pinnule='#e8c0ff',
               stalk=['#6a5a22', '#a89040'])
WHITE_C = dict(cup=['#ffffff'] * 4, arm=['#ffffff'] * 3, pinnule='#ffffff', stalk=['#ffffff'] * 2)
RED_C = dict(cup=['#a33b3b', '#e05a5a', '#ff9a8a', '#fff0e0'], arm=['#a33b3b', '#e05a5a', '#ff9a8a'],
             pinnule='#fff0e0', stalk=['#a33b3b', '#e05a5a'])
PALE_C = dict(cup=['#6a6450', '#9a9478', '#c4c0a4', '#ecead8'], arm=['#5a6a5a', '#8a9a88', '#b8c4b0'],
              pinnule='#e0e8d8', stalk=['#6a6450', '#9a9478'])
STONE_C = dict(cup=['#4a4640', '#6e6a62', '#948f84', '#b8b2a4'], arm=['#4a4640', '#6e6a62', '#948f84'],
               pinnule='#b8b2a4', stalk=['#4a4640', '#6e6a62'])
CURRENT = (191, 230, 255)

ARM_ANGLES = (-80, -60, -40, -20, 0, 20, 40, 60, 80)   # grados desde la vertical: diez brazos finos

def crinoid(facing, pal=CRINOID, open_=0.8, sway=0.0, sweep=0.0, spin=0.0, dx=0, dy=0, eyes=None, bob=0,
            droop=0.0, swirl=None, n=N, stalk=True):
    """open_: 0 = brazos enrollados en bola, 1 = abiertos, >1 = muy abiertos. sweep: los brazos se echan hacia atrás
    (al nadar de lado). spin: giro de toda la corona (el remolino del ataque). droop: brazos caídos (muerte)."""
    cv = Canvas(n); off = (n - N) / 2
    if eyes is None: eyes = 'none' if facing == 'up' else 'open'
    cx, cy = 12 + dx + off, 15.5 + dy + off + bob
    arm_ramp = [C(c) for c in pal['arm']]
    # tallo corto con raíces (cirros), detrás del cáliz
    if stalk:
        for k in range(4):
            col = pal['stalk'][k % 2]
            cv.put(cx - 1 + round(math.sin(sway + k) * 0.4), cy + 2 + k, col)
            cv.put(cx + round(math.sin(sway + k) * 0.4), cy + 2 + k, col)
        for sx in (-2, -1, 1, 2):
            cv.put(cx + sx - (1 if sx < 0 else 0) * 0 + (0.5 if sx > 0 else -0.5) * 2 * 0, cy + 6, pal['stalk'][0])
    # brazos plumosos
    pinnules = []
    arm_pixels = []
    length = 5 + 5.5 * min(open_, 1.2)
    curl = (1 - min(open_, 1.0)) * 4.2 + 0.4
    for i, a0 in enumerate(ARM_ANGLES):
        base = math.radians(a0 * (0.6 + 0.55 * min(open_, 1.3)) + spin - sweep * 25)
        side_sign = -1 if a0 < 0 else 1 if a0 > 0 else (1 if spin >= 0 else -1)
        x = cx + math.sin(base) * 2.6
        y = cy - 2.2 + abs(math.sin(base)) * 0.6
        steps = int(length * 2)
        for s in range(steps):
            t = s / max(1, steps - 1)
            # las puntas se enroscan hacia dentro, como en los crinoideos de verdad
            ang = (base + side_sign * (curl * t * t + 0.9 * t ** 4) + math.sin(sway + i * 1.3 + t * 3) * 0.12
                   + droop * t * side_sign * 1.6)
            x += math.sin(ang) * 0.5
            y -= math.cos(ang) * 0.5 - droop * 0.35
            nx, ny = math.cos(ang), math.sin(ang)          # perpendicular al brazo
            # brazo de 1 px, sin contorno (se pinta después): más finos y numerosos, como una flor de plumas
            arm_pixels.append((x, y, arm_ramp[0] if t < 0.3 else arm_ramp[1] if t < 0.75 else arm_ramp[2]))
            if s % 2 == 1 and 0.15 < t < 0.95:
                k = 1 if (s // 2 + i) % 2 else -1           # plumas alternas, a un lado y a otro
                pinnules.append((x + nx * 1.1 * k + math.sin(ang) * 0.5, y + ny * 1.1 * k - math.cos(ang) * 0.5))
    # cáliz con la cara de la célula
    ellipse(cv, cx, cy, 3.8, 3.1, pal['cup'])
    for x0 in range(int(cx - 3), int(cx + 4), 2):          # placas del cáliz
        cv.put_in(x0, cy + 1, pal['cup'][0])
    if facing == 'side':
        eye(cv, int(cx), int(cy - 1.5), 'side', eyes); eye(cv, int(cx + 2), int(cy - 1.5), 'side', eyes)
    elif facing == 'down':
        eye(cv, int(cx - 2.5), int(cy - 1.5), 'down', eyes); eye(cv, int(cx + 0.5), int(cy - 1.5), 'down', eyes)
    cv.outline()
    # brazos y pínnulas (las plumas), sin contorno, por detrás del cáliz
    for px, py, col in arm_pixels:
        cv.put_out(px, py, col)
    for px, py in pinnules:
        cv.put_out(px, py, pal['pinnule'])
    # corriente del ataque: trazos curvos alrededor de la corona
    if swirl is not None:
        sx, sy, r, ph = swirl
        for k in range(3):
            for s in range(6):
                ang = ph + k * 2.1 + s * 0.22
                cv.put_out(sx + off + dx + math.cos(ang) * (r + s * 0.25), sy + off + dy + math.sin(ang) * (r + s * 0.25) * 0.8,
                           CURRENT + (255 - s * 25,))
    return cv

for f in FACINGS:
    anims[f'Crinoid_Idle_{f}'] = [crinoid(f, open_=o, sway=i * 1.5, bob=b)
                                  for i, (o, b) in enumerate(((0.75, 0), (0.85, 0), (0.95, -1), (0.85, 0)))]
    sw = 1.0 if f == 'side' else 0.0
    anims[f'Crinoid_Move_{f}'] = [crinoid(f, open_=o, sway=i * 1.5, sweep=sw * s, bob=b)
                                  for i, (o, s, b) in enumerate(((1.0, 0.2, 0), (0.55, 0.9, -1), (0.85, 0.6, -1), (0.7, 0.4, 0)))]
    ball = crinoid(f, open_=0.0, eyes='closed', stalk=False)
    anims[f'Crinoid_Dodge_{f}'] = [as_img(ball), rotated(ball, -90), rotated(ball, -180), rotated(ball, -270),
                                   as_img(crinoid(f, open_=1.1))]
    # ataque: recoge los brazos, los abre de golpe girando la corona (la corriente) y vuelve
    cx0 = {'side': 18, 'down': 12, 'up': 12}[f]; cy0 = {'side': 11, 'down': 17, 'up': 6}[f]
    anims[f'Crinoid_Attack_{f}'] = [
        crinoid(f, open_=0.45, dy=1),
        crinoid(f, open_=0.15, dy=1, sway=1),
        crinoid(f, open_=1.35, spin=-14, dy=-1, swirl=(cx0, cy0, 3.5, 0.0)),
        crinoid(f, open_=1.25, spin=18, dy=-1, swirl=(cx0, cy0, 4.5, 1.2)),
        crinoid(f, open_=1.05, spin=6, swirl=(cx0, cy0, 5.5, 2.4)),
        crinoid(f, open_=0.85)]
anims['Crinoid_Hurt'] = [as_img(crinoid('side', pal=WHITE_C, eyes='wide', open_=1.1)),
                         as_img(crinoid('side', pal=RED_C, eyes='wide', open_=0.5))]

def crinoid_death():
    fr = [crinoid('side', pal=WHITE_C, eyes='wide', n=ND, open_=1.1),
          crinoid('down', pal=PALE_C, eyes='x', n=ND, open_=0.9, droop=0.4),
          crinoid('down', pal=PALE_C, eyes='x', n=ND, open_=0.9, droop=1.0, dy=1)]
    # los brazos se rompen en osículos (las piezas que fosilizan) que caen
    for step in (1, 2):
        cv = Canvas(ND)
        ellipse(cv, 16, 19 + step, 3.8, 3.1 - step * 0.6, PALE_C['cup'])
        cv.outline()
        rnd = random.Random(step)
        for k in range(14):
            x, y = 16 + rnd.uniform(-9, 9), 12 + rnd.uniform(-6, 6) + step * 3
            cv.put(x, y, PALE_C['arm'][1 + k % 2])
        fr.append(cv)
    # se queda fosilizado en el fondo
    fos = crinoid('down', pal=STONE_C, eyes='none', n=ND, open_=1.0, droop=0.2, dy=2)
    fr += [fos, fos]
    return [as_img(x) for x in fr]
anims['Crinoid_Death'] = crinoid_death()

anims = {k: [as_img(x) for x in v] for k, v in anims.items()}

# =========================================================================================
# Efectos: mordisco de las placas, arpón de cnidocito y su destello
# =========================================================================================
def bite_fx():
    """48×48 dibujado hacia la derecha, unión en (0, 24): dos placas de hueso en arco (110° en total)
    que se cierran de golpe; destello al chocar y astillas al final."""
    fr = []
    ox, oy = 1, 24
    for a, alpha, snap in ((21, 170, 0), (13, 230, 0), (5, 255, 0), (0, 255, 1), (0, 130, 2)):
        im = Image.new('RGBA', (48, 48), (0, 0, 0, 0))
        if snap < 2:
            for y in range(48):
                for x in range(48):
                    r = math.hypot(x + .5 - ox, y + .5 - oy); th = math.degrees(math.atan2(y + .5 - oy, x + .5 - ox))
                    if not 7 <= r <= 22: continue
                    for s_ in (-1, 1):
                        lo, hi = a, a + 34          # la placa va de su filo (a) hacia fuera (a + 22)
                        t = th * s_
                        if lo <= t <= hi:
                            edge_r = r < 8.2 or r > 20.8
                            lead = t - lo < 2.2
                            col = C('#9ea8c0') if edge_r else BONE
                            if lead: col = SPARK[0] if snap else C('#ffffff')
                            if lead and int(r) % 3 == 0 and not snap: col = C('#5f6880')   # filo aserrado
                            im.putpixel((x, y), col[:3] + (int(alpha * (1 - (t - lo) / 44)),))
        if snap:
            for r in range(10, 27):
                im.putpixel((ox + r, oy), (255, 255, 255, 255 if snap == 1 else 120))
                if snap == 1 and r % 3 == 0:
                    im.putpixel((ox + r, oy - 1), SPARK[1]); im.putpixel((ox + r, oy + 1), SPARK[1])
            if snap == 1:
                for dx, dy in ((24, -3), (25, 3), (19, -4), (20, 4), (28, 0)): im.putpixel((ox + dx, oy + dy), SPARK[2])
            if snap == 2:
                for k in range(7):
                    x = 12 + k*2; y = oy + (1 if k % 2 else -1) * (3 + k % 3 + k//2)
                    im.putpixel((x, y), BONE[:3] + (170,)); im.putpixel((x + 1, y), C('#9ea8c0')[:3] + (120,))
        fr.append(im)
    return fr

def harpoon():
    """16×8 hacia la derecha: arpón urticante con su hilo ondulante."""
    fr = []
    for ph in range(2):
        cv = Canvas(16)
        for x in range(3, 12): cv.put(x, 4, '#fff2a0')
        for (x, y) in ((12, 4), (13, 4), (14, 4), (11, 3), (11, 5), (12, 3), (12, 5), (10, 2), (10, 6)): cv.put(x, y, '#ffffff')
        cv.outline()
        for x in range(0, 4):
            cv.put_out(x, 4 + (1 if (x + ph) % 2 else 0) - (0 if x > 1 else 0), C('#f2a2e2', 220))
        fr.append(cv.img().crop((0, 0, 16, 8)) if False else cv.img().crop((0, 0, 16, 16)).crop((0, 0, 16, 8)))
    return fr

def muzzle():
    fr = []
    for sz, a in ((1, 255), (2, 255), (3, 200), (2, 110)):
        im = Image.new('RGBA', (16, 16), (0, 0, 0, 0))
        for k in range(-sz, sz + 1):
            for x, y in ((8 + k, 8), (8, 8 + k)):
                c = C('#ffffff') if abs(k) < 2 else C('#fff2a0')
                im.putpixel((x, y), c[:3] + (a,))
        if sz >= 2:
            for dx, dy in ((1, 1), (-1, -1), (1, -1), (-1, 1)):
                im.putpixel((8 + dx*(sz - 1), 8 + dy*(sz - 1)), C('#f2a2e2')[:3] + (a,))
        fr.append(im)
    return fr


def whirl():
    """48×48 en bucle: el remolino de la Corona de brazos, tres brazos de corriente que giran hacia el centro."""
    fr = []
    for k in range(4):
        im = Image.new('RGBA', (48, 48), (0, 0, 0, 0))
        for arm in range(3):
            for r10 in range(30, 220):
                r = r10 / 10
                ang = arm * 2 * math.pi / 3 + r * 0.38 - k * math.pi / 6
                x, y = int(24 + math.cos(ang) * r), int(24 + math.sin(ang) * r)
                a = int(230 * (1 - r / 22) + 25)
                col = (220, 245, 255, a) if r < 8 else (150, 215, 240, a)
                if 0 <= x < 48 and 0 <= y < 48: im.putpixel((x, y), col)
        rnd = random.Random(k)
        for _ in range(10):
            ang = rnd.uniform(0, 2 * math.pi); r = rnd.uniform(6, 20)
            x, y = int(24 + math.cos(ang) * r), int(24 + math.sin(ang) * r)
            im.putpixel((x, y), (200, 255, 200, 200))
        fr.append(im)
    return fr

fx = {'Fx_JawPlates_Bite': bite_fx(), 'Proj_Cnidocyte': harpoon(), 'Fx_Cnidocyte_Muzzle': muzzle(), 'Fx_ArmCrown_Whirl': whirl()}

# =========================================================================================
# Guardado, vista previa y GIF
# =========================================================================================
def save_sheets(group, folder, prefix):
    os.makedirs(f'{OUT}/{folder}', exist_ok=True)
    for name, frames in group.items():
        w, h = frames[0].size
        sheet = Image.new('RGBA', (w*len(frames), h), (0, 0, 0, 0))
        for i, im in enumerate(frames): sheet.paste(im, (i*w, 0))
        sheet.save(f'{OUT}/{folder}/{prefix}{name}.png')

save_sheets({k[len('Placoderm_'):]: v for k, v in anims.items() if k.startswith('Placoderm_')}, 'Placoderm', 'Player_Placoderm_')
save_sheets({k[len('Jellyfish_'):]: v for k, v in anims.items() if k.startswith('Jellyfish_')}, 'Jellyfish', 'Player_Jellyfish_')
save_sheets({k[len('Crinoid_'):]: v for k, v in anims.items() if k.startswith('Crinoid_')}, 'Crinoid', 'Player_Crinoid_')
save_sheets(fx, 'Effects', '')

BG = C('#0f2a4a')     # fondo del océano cámbrico para revisar el contraste
S = 5
def preview():
    rows = list(anims) + list(fx)
    cell = 48*S//2 + 8
    W = 230 + cell*7; H = 10 + len(rows)*(32*S//1 // 1 if False else 34*S//2 + 6)
    rh = 34*S//2 + 6
    H = 10 + len(rows)*rh
    bg = Image.new('RGBA', (W, H), BG); d = ImageDraw.Draw(bg)
    for r, name in enumerate(rows):
        frames = anims.get(name) or fx[name]
        y0 = 10 + r*rh
        d.text((10, y0 + rh//2 - 8), name.replace('_', ' '), fill=C('#bfe6ff'))
        for i, im in enumerate(frames):
            sc = S if im.width <= 24 else S*24//im.width if im.width > 32 else S*3//4
            if im.width == 48: sc = 2
            if im.width == 16 and name.startswith(('Proj', 'Fx')): sc = 5
            big = im.resize((im.width*sc, im.height*sc), Image.NEAREST)
            bg.alpha_composite(big, (230 + i*cell + (cell - big.width)//2, y0 + (rh - big.height)//2))
    bg.save(f'{OUT}/vista_previa_cuerpos_piso2.png')
preview()

def gif(name, ms, frames=None):
    frames = frames or anims[name]
    n = max(32, frames[0].width)
    out = []
    for im in frames:
        b = Image.new('RGBA', (n, n), BG); b.alpha_composite(im, ((n - im.width)//2, (n - im.height)//2))
        out.append(b.resize((n*6, n*6), Image.NEAREST).convert('RGB'))
    out[0].save(f'{OUT}/gif/{name}.gif', save_all=True, append_images=out[1:], duration=ms, loop=0)
os.makedirs(f'{OUT}/gif', exist_ok=True)
SPEED = {'Idle': 180, 'Move': 110, 'Dodge': 60, 'Attack': 70, 'Hurt': 90, 'Death': 120}
for name in anims: gif(name, SPEED[name.split('_')[1]])
for name in fx: gif(name, 70, fx[name])
print('Listo:', OUT)
