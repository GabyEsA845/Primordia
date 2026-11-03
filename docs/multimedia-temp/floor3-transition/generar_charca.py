"""Charca de la sala de paso del piso 2 al 3 de Primordia (pantano carbonífero).

Sustituye a la fosa azul del piso 1 en la sala de paso del piso 2: un pozo en 3/4 del mismo tamaño (7×5 tiles) con
borde de tierra y musgo, raíces de Lepidodendron que lo abrazan y bajan al agua, pared interior de tierra húmeda y,
abajo, agua turbia con una luz verdosa que late, ondas que se abren y motas de esporas que flotan. También la
burbuja verdosa que sube y revienta.

Uso: python generar_charca.py    (escribe aquí las hojas, la vista previa y gif/)
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', 'floor3-environment'))
from base_piso3 import *                       # Canvas, C, paletas del piso 3, root(), frondlet()...

W, H = 112, 80                                  # 7 × 5 tiles, como Transition_Pit
FRAMES = 8
CX, CY = W / 2, H / 2 + 1
EARTH = ['#1a120c', '#2a1d14', '#3e2b1d', '#5a4029', '#7a5a38']          # borde de tierra (el lodo, algo más cálido)
MOSS = ['#2f4a2a', '#3d5e36', '#557a3e', '#7fa04a', '#a8c868']
WALL = ['#0c0a08', '#140f0b', '#1e1610', '#2a1f16']                       # pared interior húmeda, oscura
GLOW = ['#0a1a18', '#10262a', '#16302e', '#1d4440', '#2a5e54', '#4e8a72', '#9cc8a0', '#d8f0c0']
LIP = '#8aa868'                                                           # labio con luz, musgo claro
SPORE = ['#e8f0a0', '#c8d880']


def wobbly(x, y, cx, cy, rx, ry, wob, seed):
    a = math.atan2(y - cy, x - cx)
    r = 1 + wob * (math.sin(a * 3 + seed) * 0.6 + math.sin(a * 7 + seed * 2) * 0.4)
    return ((x - cx) / (rx * r)) ** 2 + ((y - cy) / (ry * r)) ** 2


def outer(x, y): return wobbly(x, y, CX, CY, 53, 37, 0.05, 1.0) <= 1
def hole(x, y): return wobbly(x, y, CX, CY - 1, 45, 29, 0.06, 2.0) <= 1
def near_hole(x, y): return wobbly(x, y, CX, CY - 1, 47.5, 31.5, 0.06, 2.0) <= 1
def water(x, y): return wobbly(x, y, CX, CY + 9, 37, 21, 0.05, 3.0) <= 1


# Raíces que abrazan el borde (encima de la tierra) y raíces que cuelgan por la pared hasta el agua.
def arc(a0, a1, rx=50, ry=34):
    """Puntos de control de una Bézier que sigue el borde entre dos ángulos (grados; 90 = delante)."""
    pts = []
    for f in (0, 1 / 3, 2 / 3, 1):
        a = math.radians(a0 + (a1 - a0) * f)
        pts.append((CX + math.cos(a) * rx, CY + math.sin(a) * ry))
    return tuple(pts)


# Raíces que abrazan el borde siguiendo su curva (medio enterradas: se ven a tramos) y raíces que cuelgan.
RIM_ROOTS = [arc(165, 112), arc(28, 70), arc(205, 238, 49, 33)]
# En la sombra del pozo, la corteza va más oscura.
# En el borde, la corteza algo más apagada que la de los troncos, para que se funda con la tierra.
BARK_RIM = ['#141a16', '#232c26', '#36423a', '#4c5a48', '#66745a']
BARK_DARK = ['#0e120e', '#161c18', '#222a24', '#323c32', '#4a5642']
HANGING = [  # (control, grosor arriba, grosor abajo)
    (((24, 20), (18, 27), (30, 32), (25, 44)), 3.2, 1.2),
    (((61, 11), (66, 18), (55, 22), (60, 30)), 2.6, 1.0),
    (((86, 17), (92, 24), (82, 30), (88, 41)), 3.0, 1.2),
]
ROOTLETS = [(36, 14, 9), (46, 12, 6), (72, 12, 11), (78, 14, 7), (96, 24, 6)]  # (x, y, largo): raicillas de 1 px


def charca(frame):
    cv = Canvas(W, H)
    t = frame / FRAMES
    pulse = 0.5 - 0.5 * math.cos(t * 2 * math.pi)
    rnd = random.Random(7)
    for y in range(H):
        for x in range(W):
            px, py = x + .5, y + .5
            if not outer(px, py):
                continue
            if not hole(px, py):
                # Borde de tierra: luz arriba a la izquierda; musgo en la parte de arriba y labio claro hacia dentro.
                a = math.atan2(py - CY, px - CX)
                v = -math.sin(a) * 0.6 - math.cos(a) * 0.3
                tone = 4 if v > .5 else 3 if v > .05 else 2 if v > -.45 else 1
                n = (x * 37 + y * 17) % 11
                if near_hole(px, py):
                    c = C(LIP) if py < CY + 4 else C(EARTH[3])
                elif py < CY - 8 or (v > .1 and n < 5):
                    # Musgo apagado: el tono más claro solo en algunos puntos de la parte con luz.
                    m = max(0, min(3, tone - 1)) if n > 1 else max(0, tone - 2)
                    c = C(MOSS[m]) if (x * 5 + y * 3) % 7 else C(EARTH[max(1, tone - 1)])
                else:
                    c = C(EARTH[tone]) if n > 1 else C(EARTH[max(0, tone - 1)])
                cv.put(x, y, c)
                continue
            if not water(px, py):
                # Pared interior de arriba: tierra húmeda que oscurece al bajar, con vetas.
                depth = (py - (CY - 30)) / 38
                # Estratos de tierra irregulares (ondulan en x) y grumos sueltos: nada de columnas regulares.
                wave = math.sin(x * .19 + 1.3) * 1.6 + math.sin(x * .07) * 2.2
                band = int((depth * 38 + wave) / 4.5)
                c = WALL[3] if band <= 0 else WALL[2] if band <= 2 else WALL[1] if band <= 4 else WALL[0]
                h = (x * 73856093 ^ y * 19349663) & 0xff
                if h < 22 and band <= 5:
                    c = WALL[max(0, WALL.index(c) - 1)]
                elif h > 244 and band <= 3:
                    c = WALL[min(3, WALL.index(c) + 1)]
                # Brillo húmedo de la pared junto al agua, del color de la luz.
                if water(px, py + 2):
                    c = GLOW[3] if (x + frame) % 4 else GLOW[4]
                cv.put(x, y, C(c))
                continue
            # Agua turbia: luz verdosa que sube de lo hondo y late.
            d = math.hypot((px - CX) / 37, (py - (CY + 11)) / 19)
            g = (1 - d) * (4.2 + pulse * 1.8) - 0.3
            k = max(0, min(len(GLOW) - 2, int(g)))
            # Ondas: dos anillos que se abren desde el centro en todo el ciclo.
            for phase in (0.0, 0.5):
                r = ((t + phase) % 1.0) * 30 + 3
                ring = abs(math.hypot((px - CX) / 1.0, (py - (CY + 11)) * 1.8) - r)
                if ring < 0.7 and k < len(GLOW) - 2:
                    k = min(len(GLOW) - 2, k + 1 + (1 if r < 18 else 0))
            cv.put(x, y, C(GLOW[k]))
    # Raíces colgando sobre la pared (por detrás del borde de abajo) hasta el agua.
    for ctrl, w0, w1 in HANGING:
        root(cv, ctrl, w0, w1, BARK_DARK, box=(0, 0, W, int(CY + 14)))
    for (rx_, ry_, length) in ROOTLETS:
        for k in range(length):
            x = rx_ + round(math.sin(k * .7 + rx_) * .8)
            if hole(x + .5, ry_ + k + .5):
                cv.put(x, ry_ + k, C(BARK_DARK[2] if k < length - 2 else BARK_DARK[1]))
    # Las raíces del borde, encima de todo: abrazan la tierra y una baja al agua.
    for i, ctrl in enumerate(RIM_ROOTS):
        part = Canvas(W, H)
        root(part, ctrl, 5, 3, BARK_RIM)
        pts = bezier(*ctrl, n=60)
        for (x, y), c in part.px.items():
            # Medio enterrada: un tramo de cada tres se mete en la tierra.
            j = min(range(len(pts)), key=lambda q: (pts[q][0] - x) ** 2 + (pts[q][1] - y) ** 2)
            if (j // 20 + i) % 4 != 3 and not hole(x + .5, y + .5):
                cv.put(x, y, c)
        # Musgo sobre el lomo de la raíz, a tramos.
        for j, (x, y) in enumerate(pts[4:-4:5]):
            if (j + i) % 3 == 0:
                cv.put(x, y - 2, C(MOSS[2])); cv.put(x + 1, y - 2, C(MOSS[1]))
    # Motas de esporas que flotan sobre la charca (giran despacio en el ciclo).
    for i, (mx, my) in enumerate(((36, 30), (60, 26), (78, 34), (48, 44), (70, 48), (30, 50))):
        ang = t * 2 * math.pi + i * 1.7
        x, y = mx + math.cos(ang) * 2.5, my + math.sin(ang) * 1.5 - 1
        if (i + frame) % 4 != 0:
            cv.put(x, y, C(SPORE[i % 2], 230))
    cv.outline()
    return cv


def bubble(frame):
    """0-1: sube temblando; 2: revienta en anillo; 3: chispitas."""
    cv = Canvas(8, 8)
    if frame < 2:
        rx, ry = (2.6, 2.6) if frame == 0 else (2.9, 2.4)
        for a in range(32):
            cv.put(round(4 + math.cos(a * math.pi / 16) * rx - .5), round(4 + math.sin(a * math.pi / 16) * ry - .5), C(GLOW[6]))
        cv.put(3, 2, C('#ffffff')); cv.put(2, 3, C('#ffffff', 180))
    elif frame == 2:
        for a in range(16):
            if a % 2 == 0:
                cv.put(round(4 + math.cos(a * math.pi / 8) * 3.4 - .5), round(4 + math.sin(a * math.pi / 8) * 3.4 - .5), C(GLOW[7]))
    else:
        for (x, y) in ((0, 3), (7, 2), (2, 7), (6, 6), (4, 0)):
            cv.put(x, y, C(GLOW[7], 200))
    return cv


frames = [charca(k) for k in range(FRAMES)]
bubbles = [bubble(k) for k in range(4)]
save_sheet(f'{HERE}/Transition_Pit_Swamp.png', frames)
save_sheet(f'{HERE}/Transition_Bubble_Swamp.png', bubbles)


# --- vistas previas: sobre el suelo del pantano ---
def floor_bg(w, h):
    bg = Image.new('RGBA', (w, h), C(TRAMOS['Swamp']['floor'][0]))
    pal = TRAMOS['Swamp']['floor']
    r = random.Random(3)
    for y in range(h):
        for x in range(w):
            v = (math.sin(x * .21 + y * .07) + math.sin(y * .17 - x * .05)) * .5 + r.random() * .4
            bg.putpixel((x, y), C(pal[min(3, max(0, int((v + 1) * 1.7)))]))
    return bg


pad = 16
base = floor_bg(W + pad * 2, H + pad * 2)
os.makedirs(f'{HERE}/gif', exist_ok=True)
gframes = []
for k, f in enumerate(frames):
    im = base.copy(); im.alpha_composite(f.img(), (pad, pad))
    # una burbuja de muestra que sube y revienta
    b = bubbles[min(3, k // 2)]
    im.alpha_composite(b.img(), (pad + 60, pad + 46 - k * 3))
    gframes.append(im.resize((im.width * 4, im.height * 4), Image.NEAREST).convert('RGB'))
gframes[0].save(f'{HERE}/gif/Transition_Pit_Swamp.gif', save_all=True, append_images=gframes[1:], duration=167, loop=0)

pv = Image.new('RGBA', ((W + pad * 2) * 4 + 150, (H + pad * 2) * 4 + 20), C('#101a12'))
im = base.copy(); im.alpha_composite(frames[2].img(), (pad, pad))
pv.alpha_composite(im.resize((im.width * 4, im.height * 4), Image.NEAREST), (0, 0))
for i, b in enumerate(bubbles):
    pv.alpha_composite(b.img().resize((48, 48), Image.NEAREST), ((W + pad * 2) * 4 + 20 + (i % 2) * 60, 20 + (i // 2) * 60))
pv.save(f'{HERE}/vista_previa_charca.png')

# Comparación con la fosa del piso 1
old = os.path.join(HERE, '..', 'floor-transition', 'Transition_Pit.png')
if os.path.exists(old):
    o = Image.open(old).convert('RGBA').crop((2 * W, 0, 3 * W, H))
    cmp_ = Image.new('RGBA', (W * 2 + 24, H + 16), C('#101a12'))
    cmp_.alpha_composite(o, (8, 8)); cmp_.alpha_composite(frames[2].img(), (W + 16, 8))
    cmp_.resize((cmp_.width * 3, cmp_.height * 3), Image.NEAREST).save(f'{HERE}/vista_previa_comparacion.png')
print('Listo:', HERE)
