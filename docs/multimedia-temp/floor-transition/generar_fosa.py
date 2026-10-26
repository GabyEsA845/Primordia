"""Fosa de la sala de paso de Primordia: el agujero por el que se salta al piso siguiente.

Vista en 3/4 como las paredes: se ve la pared interior del lado de arriba (con estratos), el borde de roca con su
labio claro y, al fondo, una luz azul que late (4 cuadros). También la burbuja que sube de la fosa (la mueve el
código) y el icono del mapa. Paleta del océano cámbrico (Theme_Cambrian), porque la fosa está en su lado de la sala.

Uso: python generar_fosa.py [carpeta_salida]   (por defecto, la carpeta de este script)
"""
import math, os, sys
from PIL import Image, ImageDraw

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__))
OUTL = (24, 20, 37, 255)
def C(h, a=255): h = h.lstrip('#'); return tuple(int(h[i:i+2], 16) for i in (0, 2, 4)) + (a,)

W, H = 112, 80            # 7 × 5 tiles
RIM = ['#1c2c50', '#365890', '#4e78b0', '#80a8d8']
LIP = '#b0e0ec'
WALL = ['#0a1428', '#0e1830', '#152240', '#1c2c50']
DEEP = '#040814'
GLOW = ['#08203e', '#0e3460', '#1a5088', '#3a7ac0', '#7ac0f0', '#c8f0ff']
VEIN = '#d0705a'

def inside(x, y, cx, cy, rx, ry, wob=0.0, seed=0.0):
    a = math.atan2(y - cy, x - cx)
    r = 1 + wob * (math.sin(a * 3 + seed) * 0.6 + math.sin(a * 7 + seed * 2) * 0.4)
    return ((x - cx) / (rx * r)) ** 2 + ((y - cy) / (ry * r)) ** 2 <= 1

def pit(frame):
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    pulse = (0.0, 0.5, 1.0, 0.5)[frame]
    cx, cy = W / 2, H / 2 + 1
    for y in range(H):
        for x in range(W):
            px, py = x + .5, y + .5
            if not inside(px, py, cx, cy, 53, 37, 0.04, 1.0):
                continue
            hole = inside(px, py, cx, cy - 1, 46, 30, 0.05, 2.0)
            if not hole:
                # borde de roca: más claro arriba a la izquierda (la luz), labio claro hacia dentro
                a = math.atan2(py - cy, px - cx)
                v = -math.sin(a) * 0.6 - math.cos(a) * 0.3
                near_hole = inside(px, py, cx, cy - 1, 48.5, 32.5, 0.05, 2.0)
                if near_hole:
                    c = LIP if py < cy + 6 else RIM[3]
                else:
                    c = RIM[3] if v > 0.45 else RIM[2] if v > 0 else RIM[1] if v > -0.5 else RIM[0]
                im.putpixel((x, y), C(c))
                continue
            # dentro: la pared interior de arriba se ve; el fondo, desplazado hacia abajo, se ve por debajo
            floor = inside(px, py, cx, cy + 9, 36, 21, 0.06, 3.0)
            if not floor:
                depth = (py - (cy - 31)) / 40           # 0 junto al borde, crece hacia abajo
                band = int(depth * 9)
                c = WALL[3] if band <= 0 else WALL[2] if band <= 1 else WALL[1] if band <= 3 else WALL[0]
                if band in (1, 3) and (x + band) % 5 == 0: c = WALL[0]     # estratos
                if 1 <= band <= 3 and (x * 13 + y * 7 + band * 5) % 37 == 0: c = VEIN  # vetas de coral sueltas, como las paredes
                im.putpixel((x, y), C(c))
                continue
            # fondo: luz azul que late desde el centro
            d = math.hypot((px - cx) / 36, (py - (cy + 11)) / 19)
            g = (1 - d) * (3.2 + pulse * 1.6) - 0.4
            k = max(-1, min(len(GLOW) - 1, int(g)))
            im.putpixel((x, y), C(DEEP if k < 0 else GLOW[k]))
    # partículas suspendidas en la luz
    for i, (bx, by) in enumerate(((44, 52), (63, 47), (55, 58), (70, 55), (49, 45))):
        if (i + frame) % 3 != 0:
            im.putpixel((bx, by - frame), C(GLOW[5], 220))
    # contorno exterior
    add = []
    for y in range(H):
        for x in range(W):
            if im.getpixel((x, y))[3] == 0 and any(0 <= x+dx < W and 0 <= y+dy < H and im.getpixel((x+dx, y+dy))[3] == 255
                                                   for dx, dy in ((1,0),(-1,0),(0,1),(0,-1))):
                add.append((x, y))
    for p in add: im.putpixel(p, OUTL)
    return im

def bubble(frame):
    im = Image.new('RGBA', (8, 8), (0, 0, 0, 0))
    r = 2.6 if frame == 0 else 2.9
    for a in range(24):
        x = round(4 + math.cos(a * math.pi / 12) * r - 0.5); y = round(4 + math.sin(a * math.pi / 12) * r - 0.5)
        im.putpixel((x, y), C('#a8dcec'))
    im.putpixel((3, 2), C('#ffffff')); im.putpixel((2, 3), C('#ffffff', 180))
    return im

def map_icon():
    im = Image.new('RGBA', (9, 9), (0, 0, 0, 0))
    for y in range(9):
        for x in range(9):
            d = math.hypot((x + .5 - 4.5) / 4.4, (y + .5 - 4.5) / 3.6)
            if d <= 1:
                im.putpixel((x, y), C(LIP if d > 0.75 and y < 5 else RIM[2] if d > 0.75 else GLOW[4] if d < 0.3 else GLOW[2]))
    add = [(x, y) for y in range(9) for x in range(9) if im.getpixel((x, y))[3] == 0 and any(
        0 <= x+dx < 9 and 0 <= y+dy < 9 and im.getpixel((x+dx, y+dy))[3] for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)))]
    for p in add: im.putpixel(p, OUTL)
    return im

def sheet(frames):
    w, h = frames[0].size; s = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames): s.paste(f, (i * w, 0))
    return s

pits = [pit(k) for k in range(4)]
bubbles = [bubble(k) for k in range(2)]
sheet(pits).save(f'{OUT}/Transition_Pit.png')
sheet(bubbles).save(f'{OUT}/Transition_Bubble.png')
map_icon().save(f'{OUT}/UI_Map_Transition.png')

# vista previa sobre el suelo del océano y GIF del latido
floor = Image.open(os.path.join(OUT, '..', 'environment', 'Cambrian', 'tiles', 'Floor_0.png')).convert('RGBA') \
    if os.path.exists(os.path.join(OUT, '..', 'environment', 'Cambrian', 'tiles', 'Floor_0.png')) else None
def on_floor(im, pad=16):
    bg = Image.new('RGBA', (im.width + pad * 2, im.height + pad * 2), C('#16344a'))
    if floor:
        for y in range(0, bg.height, 16):
            for x in range(0, bg.width, 16): bg.paste(floor, (x, y))
    bg.alpha_composite(im, (pad, pad)); return bg
os.makedirs(f'{OUT}/gif', exist_ok=True)
fr = [on_floor(p).resize(((W + 32) * 4, (H + 32) * 4), Image.NEAREST).convert('RGB') for p in pits]
fr[0].save(f'{OUT}/gif/Transition_Pit.gif', save_all=True, append_images=fr[1:], duration=160, loop=0)
pv = Image.new('RGBA', ((W + 32) * 4 + 200, (H + 32) * 4 + 20), C('#0a1a2a'))
pv.alpha_composite(on_floor(pits[2]).resize(((W + 32) * 4, (H + 32) * 4), Image.NEAREST), (0, 0))
pv.alpha_composite(bubbles[0].resize((64, 64), Image.NEAREST), ((W + 32) * 4 + 20, 20))
pv.alpha_composite(map_icon().resize((72, 72), Image.NEAREST), ((W + 32) * 4 + 20, 120))
pv.save(f'{OUT}/vista_previa_fosa.png')
print('Listo:', OUT)
