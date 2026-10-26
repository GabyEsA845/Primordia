"""Iconos de las Bocas del piso 2 (16×16 y 32×32), con el estilo de los de las mutaciones del piso 1:
objeto sombreado con luz arriba a la izquierda, 3-4 tonos y contorno oscuro.

- Placas mandibulares: las dos placas de hueso de un pez acorazado, abiertas, con el filo aserrado claro.
- Cnidocitos: la cápsula urticante rosa disparando su arpón amarillo, con el hilo enrollado.

Uso: python generar_iconos_piso2.py   (salida: Icons/ y vista_previa_iconos.png junto a este script)
"""
import math, os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUTL = (24, 20, 37, 255)
def C(h): h = h.lstrip('#'); return tuple(int(h[i:i+2], 16) for i in (0, 2, 4)) + (255,)

PLATE = ['#353a4c', '#5f6880', '#9ea8c0', '#e2e8f4']
BONE = ['#b8ac90', '#e8dcc0', '#fff8e8']
CAPS = ['#7a2a7c', '#b84aa8', '#e880d0', '#ffd0f4']
DART = ['#c8a030', '#fff2a0', '#ffffff']
THREAD = '#f2a2e2'

def outline(im):
    w, h = im.size
    add = [(x, y) for y in range(h) for x in range(w) if im.getpixel((x, y))[3] == 0 and any(
        0 <= x+dx < w and 0 <= y+dy < h and im.getpixel((x+dx, y+dy))[3] and im.getpixel((x+dx, y+dy)) != OUTL
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for p in add: im.putpixel(p, OUTL)

def shade(nx, ny, ramp):
    v = (-0.6*nx - 0.8*ny) * 0.8 + 0.35 - max(0, math.hypot(nx, ny) - 0.75) * 1.2
    return C(ramp[0 if v < -0.15 else 1 if v < 0.35 else 2 if v < 0.75 else len(ramp) - 1])

def inside_poly(px, py, pts):
    r = False
    for i in range(len(pts)):
        (x1, y1), (x2, y2) = pts[i], pts[i-1]
        if (y1 > py) != (y2 > py) and px < (x2 - x1) * (py - y1) / (y2 - y1) + x1: r = not r
    return r

def jaw_plates(n):
    """Dos placas en cuña que se abren hacia la derecha, con la bisagra a la izquierda."""
    im = Image.new('RGBA', (n, n), (0, 0, 0, 0)); s = n / 32
    upper = [(3, 15), (8, 6), (17, 3), (27, 6), (30, 11), (20, 11), (10, 15)]
    lower = [(3, 17), (10, 18), (19, 21), (29, 22), (26, 27), (16, 29), (7, 25)]
    for pts, cy in ((upper, 8.5), (lower, 23.5)):
        pts = [(x * s, y * s) for x, y in pts]
        for y in range(n):
            for x in range(n):
                px, py = x + .5, y + .5
                if inside_poly(px, py, pts):
                    im.putpixel((x, y), shade((px - 15 * s) / (13 * s), (py - cy * s) / (8 * s), PLATE))
    # filos aserrados: la arista de cada placa que da al hueco, en hueso claro, con dientes que asoman
    snap = im.copy()
    for x in range(int(8 * s), n):
        for y in range(n):
            if not snap.getpixel((x, y))[3]:
                continue
            upper_plate = y < 16 * s
            gap = y + 1 if upper_plate else y - 1
            if 0 <= gap < n and snap.getpixel((x, gap))[3] == 0:
                im.putpixel((x, y), C(BONE[1]))
                if (x // max(1, int(2 * s))) % 2 == 0:
                    im.putpixel((x, gap), C(BONE[2]))
    # ojo dorado en la placa de arriba y remache
    ex, ey = int(12 * s), int(8 * s)
    im.putpixel((ex, ey), C('#ffd060'))
    if n >= 32:
        im.putpixel((ex + 1, ey), C('#ffd060')); im.putpixel((ex, ey + 1), C('#c89030')); im.putpixel((ex + 1, ey + 1), OUTL)
        im.putpixel((int(7 * s), int(22 * s)), C(PLATE[3]))
    outline(im)
    return im

def cnidocytes(n):
    """Cápsula abajo a la izquierda; el hilo sale enrollado hacia arriba a la derecha y acaba en el arpón."""
    im = Image.new('RGBA', (n, n), (0, 0, 0, 0)); s = n / 32
    cx, cy, rx, ry = 10 * s, 21 * s, 7.5 * s, 8.5 * s
    for y in range(n):
        for x in range(n):
            nx, ny = (x + .5 - cx) / rx, (y + .5 - cy) / ry
            if nx * nx + ny * ny <= 1:
                im.putpixel((x, y), shade(nx, ny, CAPS))
    # opérculo abierto arriba a la derecha de la cápsula
    for k in range(int(3 * s) + 1):
        x, y = int(cx + rx * 0.55) - k // 2, int(cy - ry * 0.75) + k // 2
        if 0 <= x < n and 0 <= y < n: im.putpixel((x, y), C('#4a1a4c'))
    # hilo en espiral que sale de la boca de la cápsula
    for i in range(int(60 * s)):
        t = i / (60 * s)
        ang = t * 4.5 * math.pi
        r = (3.2 - 1.8 * t) * s
        x = cx + rx * 0.5 + t * 14 * s + math.cos(ang) * r
        y = cy - ry * 0.7 - t * 11 * s + math.sin(ang) * r * 0.6
        if 0 <= x < n and 0 <= y < n and im.getpixel((int(x), int(y)))[3] == 0:
            im.putpixel((int(x), int(y)), C(THREAD))
    # arpón con púas
    tip = (int(29 * s), int(3 * s))
    for k in range(int(6 * s)):
        x, y = tip[0] - k, tip[1] + k
        if 0 <= x < n and 0 <= y < n: im.putpixel((x, y), C(DART[1] if k else DART[2]))
    for dx, dy in ((-3, -1), (1, 3), (-4, 0), (0, 4)):
        x, y = tip[0] + int(dx * s), tip[1] + int(dy * s)
        if 0 <= x < n and 0 <= y < n: im.putpixel((x, y), C(DART[0]))
    if n >= 32:
        im.putpixel((int(cx - rx * 0.45), int(cy - ry * 0.45)), C('#ffffff'))
    outline(im)
    return im


# ---------- iconos del resto de mutaciones del piso 2 ----------

def blob(im, cx, cy, rx, ry, ramp):
    n = im.width
    for y in range(n):
        for x in range(n):
            nx, ny = (x + .5 - cx) / rx, (y + .5 - cy) / ry
            if nx * nx + ny * ny <= 1:
                im.putpixel((x, y), shade(nx, ny, ramp))

def fill_poly(im, pts, ramp, cx, cy, rx, ry):
    n = im.width
    for y in range(n):
        for x in range(n):
            if inside_poly(x + .5, y + .5, pts):
                im.putpixel((x, y), shade((x + .5 - cx) / rx, (y + .5 - cy) / ry, ramp))

def dot(im, x, y, c):
    if 0 <= int(x) < im.width and 0 <= int(y) < im.height:
        im.putpixel((int(x), int(y)), c if isinstance(c, tuple) else C(c))

def arm_crown(n):
    """Corona de brazos: cáliz dorado y cinco brazos plumosos morados con las puntas enroscadas."""
    im = Image.new('RGBA', (n, n), (0, 0, 0, 0)); s = n / 32
    arm = ['#3a1a50', '#7a3aa0', '#b878e0']
    feathers = []
    for i, a0 in enumerate((-70, -35, 0, 35, 70)):
        base = math.radians(a0 * 0.8); sign = -1 if a0 < 0 else 1 if a0 > 0 else 1
        x, y = 16 * s + math.sin(base) * 4 * s, 21 * s
        steps = int(26 * s)
        for k in range(steps):
            t = k / max(1, steps - 1)
            ang = base + sign * (0.4 * t * t + 1.8 * t ** 4)
            x += math.sin(ang) * 0.6 * s; y -= math.cos(ang) * 0.6 * s
            nx, ny = math.cos(ang), math.sin(ang)
            dot(im, x, y, arm[1] if t < 0.7 else arm[2])
            if n >= 32 and t < 0.8: dot(im, x + nx, y + ny, arm[0])
            if k % 2 and 0.15 < t < 0.95:
                for side in (-1, 1): feathers.append((x + nx * 1.8 * s * side, y + ny * 1.8 * s * side))
    blob(im, 16 * s, 24 * s, 6.5 * s, 4.5 * s, ['#5a4a1a', '#8a7a2a', '#c8b040', '#f0e080'])
    outline(im)
    for fx, fy in feathers:
        if 0 <= int(fx) < n and 0 <= int(fy) < n and im.getpixel((int(fx), int(fy)))[3] == 0:
            dot(im, fx, fy, '#e8c0ff')
    return im

def tail_propulsor(n):
    """Cola propulsora: aleta caudal bifurcada naranja que empuja, con tres burbujas detrás."""
    im = Image.new('RGBA', (n, n), (0, 0, 0, 0)); s = n / 32
    fin = ['#8a4424', '#d0763a', '#f0a060', '#ffd0a0']
    # pedúnculo a la derecha y dos lóbulos que se abren hacia la izquierda
    fill_poly(im, [(p[0] * s, p[1] * s) for p in ((29, 14), (29, 19), (20, 18), (10, 29), (13, 17), (13, 15), (10, 3), (20, 14))],
              fin, 20 * s, 12 * s, 12 * s, 12 * s)
    for k in (-1, 0, 1):                                  # radios
        for t in range(int(8 * s)):
            dot(im, 19 * s - t, 16 * s + k * t * 1.1, fin[0])
    outline(im)
    for bx, by, r in ((6, 16, 2.6), (4, 24, 1.8), (6, 8, 1.6)):
        if n < 32 and r < 1.7: continue
        for y in range(n):
            for x in range(n):
                d = math.hypot(x + .5 - bx * s, y + .5 - by * s)
                if abs(d - r * s) < 0.55 and im.getpixel((x, y))[3] == 0:
                    im.putpixel((x, y), C('#bfe6ff'))
        dot(im, (bx - r * 0.45) * s, (by - r * 0.45) * s, '#ffffff')
    return im

def hallucigenia_spines(n):
    """Espinas de Hallucigenia: cuerpo de gusano con púas altas en el lomo y patitas debajo."""
    im = Image.new('RGBA', (n, n), (0, 0, 0, 0)); s = n / 32
    body = ['#8a4a5a', '#c87888', '#f0a8b0', '#ffe0e0']
    for k, sx in enumerate((7, 12, 17, 22)):          # púas, primero (detrás del cuerpo)
        fill_poly(im, [(p[0] * s, p[1] * s) for p in ((sx, 19), (sx + 3, 19), (sx + 3.6, 5 + k % 2 * 2))],
                  ['#a89a7a', '#d8ccb0', '#fff4dc', '#ffffff'], sx * s, 10 * s, 4 * s, 10 * s)
    blob(im, 16 * s, 21 * s, 13 * s, 3.6 * s, body)
    blob(im, 27.5 * s, 20 * s, 3 * s, 3 * s, body)       # cabeza
    for lx in range(6, 26, 4):                          # patitas
        for t in range(int(4 * s)):
            dot(im, lx * s + t * 0.3, 24 * s + t, body[1])
    outline(im)
    return im

def trilobite_plates(n):
    """Placas de trilobite: el caparazón visto desde arriba, con sus tres lóbulos, segmentos y espinas."""
    im = Image.new('RGBA', (n, n), (0, 0, 0, 0)); s = n / 32
    ramp = ['#4a3a30', '#7a6450', '#a8907a', '#d8c4a8']
    for sx in (-1, 1):                                    # espinas genales, detrás
        fill_poly(im, [(p[0] * s, p[1] * s) for p in ((16 + sx * 8, 9), (16 + sx * 11, 11), (16 + sx * 12.5, 22))],
                  ramp, 16 * s, 12 * s, 12 * s, 12 * s)
    for y in range(n):
        for x in range(n):
            px, py = x + .5, y + .5
            nx, ny = (px - 16 * s) / (10 * s), (py - 17 * s) / (13 * s)
            if nx * nx + ny * ny <= 1:
                axial = abs(px - 16 * s) < 2.5 * s
                c = shade(nx, ny, ramp)
                if py > 12 * s and int(py / s) % 3 == 0:
                    c = C(ramp[0])
                if axial and py > 9 * s:
                    c = shade(nx * 0.5, ny, ['#7a6450', '#a8907a', '#d8c4a8', '#f4e8d0'])
                im.putpixel((x, y), c)
    for ex in (11, 20):
        dot(im, ex * s, 9 * s, '#e8f4ff'); dot(im, ex * s + 1, 9 * s, '#8ab8d8')
    outline(im)
    return im

def wiwaxia_sclerites(n):
    """Escleritos de Wiwaxia: tres escamas doradas en abanico, como saliendo disparadas."""
    im = Image.new('RGBA', (n, n), (0, 0, 0, 0)); s = n / 32
    ramp = ['#7a5a1a', '#c08a30', '#ecc060', '#fff0b0']
    for cx, cy, ang in ((10, 20, -0.5), (17, 13, 0.0), (24, 20, 0.5)):
        ca, sa = math.cos(ang), math.sin(ang)
        for y in range(n):
            for x in range(n):
                dx, dy = (x + .5) / s - cx, (y + .5) / s - cy
                u, v = dx * ca + dy * sa, -dx * sa + dy * ca      # u a lo ancho, v a lo largo
                if (u / 3.2) ** 2 + (v / 8) ** 2 <= 1 and v < 6:
                    c = shade(u / 3.2, v / 8, ramp)
                    if abs(u) < 0.6: c = C(ramp[0])               # nervio central
                    im.putpixel((x, y), c)
    outline(im)
    return im

def brood_sac(n):
    """Saco de crías: bolsa rosa translúcida con tres larvas dentro."""
    im = Image.new('RGBA', (n, n), (0, 0, 0, 0)); s = n / 32
    blob(im, 16 * s, 17 * s, 11 * s, 12 * s, ['#8a3a6a', '#b0508e', '#d478b4', '#f8c0e4'])
    for lx, ly, flip in ((10, 16, 1), (18, 11, -1), (15, 22, 1)):
        pts = [(lx * s + k * flip * 0.9 * s, ly * s + math.sin(k * 0.8) * 1.2 * s) for k in range(6)]
        for i, (x, y) in enumerate(pts):
            dot(im, x, y, '#fff4d8')
            dot(im, x, y + 1, '#5a2048')                  # sombra bajo la larva: se despega del saco
            if n >= 32 and i < 4: dot(im, x, y - 1, '#fff4d8')
        dot(im, pts[0][0], pts[0][1], '#3a1030')            # ojo
    dot(im, 9 * s, 9 * s, '#ffffff'); dot(im, 10 * s, 8 * s, '#ffffff')
    outline(im)
    return im

def compound_eyes(n):
    """Ojos compuestos: cúpula ámbar cubierta de facetas hexagonales (celdas de Voronoi en rejilla hexagonal)."""
    im = Image.new('RGBA', (n, n), (0, 0, 0, 0)); s = n / 32
    ramp = ['#7a3a10', '#c86a20', '#f0a040', '#ffe0a0']
    blob(im, 16 * s, 17 * s, 12 * s, 11 * s, ramp)
    size = 5.2 * s
    centers = [(col * size + (size / 2 if row % 2 else 0), row * size * 0.87) for row in range(-2, 12) for col in range(-2, 12)]
    for y in range(n):
        for x in range(n):
            if im.getpixel((x, y))[3] == 0: continue
            d = sorted(math.hypot(x + .5 - cx, y + .5 - cy) for cx, cy in centers)[:2]
            if d[1] - d[0] < 0.9:
                im.putpixel((x, y), C('#5a2a0a'))
    dot(im, 10 * s, 10 * s, '#ffffff'); dot(im, 11 * s, 10 * s, '#fff4d8'); dot(im, 10 * s, 11 * s, '#fff4d8')
    outline(im)
    return im

NEW_ICONS = (('ArmCrown', arm_crown), ('TailPropulsor', tail_propulsor), ('HallucigeniaSpines', hallucigenia_spines),
             ('TrilobitePlates', trilobite_plates), ('WiwaxiaSclerites', wiwaxia_sclerites), ('BroodSac', brood_sac),
             ('CompoundEyes', compound_eyes))

os.makedirs(f'{HERE}/Icons', exist_ok=True)
icons = {}
ALL = (('JawPlates', jaw_plates), ('Cnidocytes', cnidocytes)) + NEW_ICONS
for name, fn in ALL:
    for n in (32, 16):
        im = fn(n); im.save(f'{HERE}/Icons/Icon_{name}_{n}.png'); icons[(name, n)] = im

cols = len(ALL)
pv = Image.new('RGBA', (cols * (32 * 5 + 16 * 5 + 30) + 10, 32 * 5 + 20), C('#283240'))
x = 10
for name, _ in ALL:
    for size in (32, 16):
        im = icons[(name, size)]; big = im.resize((im.width * 5, im.height * 5), Image.NEAREST)
        pv.alpha_composite(big, (x, 10)); x += big.width + 10
    x += 10
pv.save(f'{HERE}/vista_previa_iconos.png')
print('Listo')
