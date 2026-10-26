"""Sprites del jugador (célula) de Primordia. Uso: python3 player.py <carpeta_salida>"""
import math, sys, os
from PIL import Image

OUT = sys.argv[1]
OUTL = (24, 20, 37, 255)
def C(h): h = h.lstrip('#'); return tuple(int(h[i:i+2], 16) for i in (0, 2, 4)) + (255,)

GREEN = dict(ramp=['#2f7d4f', '#4fb85f', '#8be36a', '#d6ff9a'], nuc='#3a9a58', nucd='#2a6e45', hi='#ffffff', hi2='#f0ffd8')
RED   = dict(ramp=['#a33b3b', '#e05a5a', '#ff9a8a', '#fff0e0'], nuc='#b94a4a', nucd='#8a3030', hi='#ffffff', hi2='#fff0e0')
WHITE = dict(ramp=['#ffffff'] * 4, nuc='#ffffff', nucd='#ffffff', hi='#ffffff', hi2='#ffffff')
PALE  = dict(ramp=['#5a8a5a', '#8ac08a', '#b8e0a8', '#e8ffd8'], nuc='#6a9a6a', nucd='#4a7a4a', hi='#ffffff', hi2='#f0ffd8')
EYE = C('#f4f4e8')

class Canvas:
    def __init__(s, n): s.n = n; s.px = {}
    def put(s, x, y, c):
        x, y = int(round(x)), int(round(y))
        if 0 <= x < s.n and 0 <= y < s.n: s.px[(x, y)] = C(c) if isinstance(c, str) else c
    def put_in(s, x, y, c):
        if (int(round(x)), int(round(y))) in s.px: s.put(x, y, c)
    def outline(s):
        add = [(x, y) for y in range(s.n) for x in range(s.n) if (x, y) not in s.px and any(
            s.px.get((x+dx, y+dy), OUTL) != OUTL for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)))]
        for p in add: s.px[p] = OUTL
    def img(s):
        im = Image.new('RGBA', (s.n, s.n), (0, 0, 0, 0))
        for p, c in s.px.items(): im.putpixel(p, c)
        return im

def body(cv, cx, cy, rx, ry, pal, lean=0.0):
    """Elipse sombreada con luz arriba a la izquierda. lean desplaza la parte alta (inercia)."""
    ramp = [C(c) for c in pal['ramp']]
    for y in range(cv.n):
        for x in range(cv.n):
            px, py = x + .5, y + .5
            t = (cy - py) / ry  # 1 arriba, -1 abajo
            sx = px - cx - lean * max(0, t)
            nx, ny = sx / rx, (py - cy) / ry
            if nx*nx + ny*ny <= 1:
                v = (-0.6*nx - 0.8*ny) * 0.8 + 0.35 - max(0, math.hypot(nx, ny) - 0.75) * 1.2
                cv.px[(x, y)] = ramp[0 if v < -0.15 else 1 if v < 0.35 else 2 if v < 0.75 else 3]

def rot(dx, dy, a): return dx*math.cos(a) - dy*math.sin(a), dx*math.sin(a) + dy*math.cos(a)

def features(cv, cx, cy, rx, ry, pal, facing, angle=0.0, eyes='open'):
    k = rx / 6.5
    # núcleo
    nuc = {'side': [(-3,1),(-2,1),(-3,2),(-2,2),(-1,2)], 'down': [(-3,2),(-2,2),(-3,3),(-2,3),(-1,3)],
           'up': [(-1,-1),(0,-1),(1,-1),(-1,0),(0,0),(1,0),(0,-2)]}[facing]
    for i, (dx, dy) in enumerate(nuc):
        x, y = rot(dx*k, dy*k, angle); cv.put_in(cx+x, cy+y, pal['nucd'] if i == 1 else pal['nuc'])
    # brillo
    for (dx, dy), c in (((-4,-4), pal['hi']), ((-3,-4), pal['hi2'])):
        x, y = rot(dx*k, dy*(ry/6), angle); cv.put_in(cx+x, cy+y, c)
    if facing == 'up' or eyes == 'none': return
    eyex = {'side': (0, 3), 'down': (-3, 1)}[facing]
    for ex in eyex:
        ox, oy = cx + ex*k, cy - 2.5*(ry/6)
        if eyes == 'closed':
            cv.put(ox, oy+1, OUTL); cv.put(ox+1, oy+1, OUTL); continue
        if eyes == 'x':
            for dx, dy in ((0,0),(2,0),(1,1),(0,2),(2,2)): cv.put(ox-0.5+dx, oy+dy, OUTL)
            continue
        for dx in range(2):
            for dy in range(3): cv.put(ox+dx, oy+dy, EYE)
        if eyes == 'wide': cv.put(ox+(1 if facing=='side' else 0), oy+1, OUTL); continue
        if facing == 'side': cv.put(ox+1, oy+1, OUTL); cv.put(ox+1, oy+2, OUTL)
        else: cv.put(ox, oy+2, OUTL); cv.put(ox+1, oy+2, OUTL)

def cell(facing='side', squash=0.0, bob=0, lean=0.0, pal=GREEN, angle=0.0, eyes='open', n=16, grow=0.0):
    cv = Canvas(n); off = (n - 16) / 2
    rx = (6.5 + grow) * (1 + 0.12*squash); ry = (6 + grow) * (1 - 0.12*squash)
    cx = 8 + off; cy = 15 + off - ry + bob - 0.5
    body(cv, cx, cy, rx, ry, pal, lean)
    features(cv, cx + lean*0.5, cy, rx, ry, pal, facing, angle, eyes)
    cv.outline(); return cv

FACINGS = ('side', 'down', 'up')
anims = {}
for f in FACINGS:
    anims[f'Idle_{f}']  = [cell(f, s, 0) for s in (0, 0.5, 1, 0.5)]
    anims[f'Move_{f}']  = [cell(f, 1, 0, -1), cell(f, 0, -1, -0.5), cell(f, -1, -1, 0.5), cell(f, 0, 0, 0)]
    if f == 'side':
        anims['Move_side'] = [cell(f, 1, 0, -1.2), cell(f, 0, -1, -0.6), cell(f, -1, -1, 0.6), cell(f, 0, 0, 0)]
    # esquiva: se encoge, rueda (gira el núcleo y el brillo) y se estira al salir
    anims[f'Dodge_{f}'] = [cell(f, 1.6, 0, eyes='closed'),
                           cell(f, 0, -1, angle=math.pi/2, eyes='none', grow=-1),
                           cell(f, 0, -1, angle=math.pi, eyes='none', grow=-1),
                           cell(f, 0, -1, angle=3*math.pi/2, eyes='none', grow=-1),
                           cell(f, -1, 0, eyes='open')]
anims['Hurt'] = [cell('side', -0.6, 0, pal=WHITE, eyes='wide'), cell('side', 0.6, 0, pal=RED, eyes='wide')]

def death():
    fr = [cell('side', -0.5, 0, pal=WHITE, eyes='wide', n=24),
          cell('down', 0, 0, pal=PALE, eyes='x', n=24, grow=1),
          cell('down', -0.3, -1, pal=PALE, eyes='x', n=24, grow=2)]
    # estallido: gotas en anillo
    for r, sz in ((7, 2), (9, 1.5), (10.5, 1)):
        cv = Canvas(24)
        if r == 7: body(cv, 12, 13, 3.5, 3, GREEN)
        for k in range(8):
            a = k*math.pi/4 + 0.3
            gx, gy = 12 + math.cos(a)*r, 13 + math.sin(a)*r*0.8
            body(cv, gx, gy, sz, sz, GREEN)
        cv.outline(); fr.append(cv)
    # charco final
    cv = Canvas(24); body(cv, 12, 20, 6, 1.6, PALE); cv.put(10, 19, '#e8ffd8'); cv.outline(); fr.append(cv)
    return fr
anims['Death'] = death()

os.makedirs(f'{OUT}/frames', exist_ok=True)
sheets = {}
for name, frames in anims.items():
    n = frames[0].n
    sheet = Image.new('RGBA', (n*len(frames), n), (0, 0, 0, 0))
    for i, cv in enumerate(frames):
        im = cv.img(); im.save(f'{OUT}/frames/Player_{name}_{i}.png'); sheet.paste(im, (i*n, 0))
    sheet.save(f'{OUT}/Player_{name}.png'); sheets[name] = sheet

# vista previa ampliada x6 sobre fondo de sopa, con etiquetas
from PIL import ImageDraw
S = 6; rows = list(anims); colw = 24*S + 8
W = 150 + colw*7; H = 24*S*len(rows) + 20
bg = Image.new('RGBA', (W, H), C('#16352e')); d = ImageDraw.Draw(bg)
for r, name in enumerate(rows):
    y0 = 10 + r*24*S
    d.text((10, y0 + 24*S//2 - 6), name.replace('_', ' '), fill=C('#d6ff9a'))
    for i, cv in enumerate(anims[name]):
        im = cv.img(); n = im.width; big = im.resize((n*S, n*S), Image.NEAREST)
        bg.alpha_composite(big, (150 + i*colw + (24*S - n*S)//2, y0 + (24*S - n*S)//2))
bg.save(f'{OUT}/_vista_previa.png')

# GIF animados ampliados para ver el movimiento
def gif(name, ms, flip=False):
    fr = []
    for cv in anims[name]:
        im = cv.img(); n = im.width
        bgi = Image.new('RGBA', (24, 24), C('#16352e')); bgi.alpha_composite(im, ((24-n)//2, (24-n)//2))
        if flip: bgi = bgi.transpose(Image.FLIP_LEFT_RIGHT)
        fr.append(bgi.resize((24*8, 24*8), Image.NEAREST).convert('RGB'))
    fr[0].save(f'{OUT}/_gif/{name}.gif', save_all=True, append_images=fr[1:], duration=ms, loop=0)
os.makedirs(f'{OUT}/_gif', exist_ok=True)
for name in anims:
    gif(name, {'Idle': 180, 'Move': 110, 'Dodge': 60, 'Hurt': 90, 'Death': 110}[name.split('_')[0]])
