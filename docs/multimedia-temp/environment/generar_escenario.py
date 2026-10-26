"""Tileset del piso 1 (sopa primordial) y variante del jefe. Uso: python3 environment.py <carpeta>"""
import math, sys, os, random
from PIL import Image, ImageDraw
OUT = sys.argv[1]
T = 16
OUTL = (24, 20, 37, 255)
def C(h, a=255): h = h.lstrip('#'); return tuple(int(h[i:i+2], 16) for i in (0, 2, 4)) + (a,)

THEMES = {
  'Soup': dict(
    floor=['#173a30', '#1d4639', '#245244', '#2e6150'], fleck=['#3a7a5c', '#4f9a70', '#8fd0a0'], algae=['#2f6a3a', '#4a8a40', '#7ab050'],
    cap=['#1e1426', '#2a1c34', '#352442', '#45304f'], face=['#3a2440', '#57345c', '#7a4a78', '#a46a98', '#c890b8'],
    vein='#8a3a5a', lip='#d8a0c8', drip='#c890b8', shadow='#10281f'),
  'Boss': dict(
    floor=['#2a1a1c', '#361f22', '#44262a', '#553034'], fleck=['#6a3a3a', '#8a4848', '#c07070'], algae=['#5a2a2a', '#7a3a34', '#a05a40'],
    cap=['#1a0c12', '#26121a', '#321822', '#42202c'], face=['#3a1420', '#5a1e30', '#802c40', '#a84858', '#d07080'],
    vein='#c03050', lip='#e898a8', drip='#d07080', shadow='#1a0e10'),
}

def new(w=T, h=T): return Image.new('RGBA', (w, h), (0, 0, 0, 0))
def P(im, x, y, c):
    if 0 <= x < im.width and 0 <= y < im.height: im.putpixel((int(x), int(y)), C(c) if isinstance(c, str) else c)

# ---------------- suelo ----------------
def floor_tile(th, v):
    im = new(); rnd = random.Random(100 + v); f = th['floor']
    for y in range(T):
        for x in range(T):
            # ondulación suave que encaja en los bordes (periodo 16)
            n = math.sin(x * 2*math.pi/16 + v) * math.cos(y * 2*math.pi/16 + v*0.7)
            P(im, x, y, f[1] if n > 0.55 else f[0])
    for _ in range(1 + v % 2):  # partículas
        x, y = rnd.randrange(1, 15), rnd.randrange(1, 15); P(im, x, y, th['fleck'][0])
    if v == 1:   # burbuja
        for a in range(12):
            P(im, 8 + round(math.cos(a*math.pi/6)*2.5), 7 + round(math.sin(a*math.pi/6)*2.5), th['fleck'][1])
        P(im, 7, 6, th['fleck'][2])
    if v == 2:   # trocito de alga
        for i, (x, y) in enumerate(((4,11),(5,10),(5,9),(6,8),(7,8),(8,7))): P(im, x, y, th['algae'][1 if i % 2 else 0])
    if v == 3:   # grupo de motas claras
        for x, y in ((10,4),(11,5),(9,5),(12,9)): P(im, x, y, th['fleck'][1])
    if v == 4:   # mancha más clara
        for y in range(5, 11):
            for x in range(4, 12):
                if ((x-7.5)/4)**2 + ((y-7.5)/3)**2 <= 1: P(im, x, y, f[2])
        P(im, 6, 6, f[3])
    if v == 5:   # dos burbujitas
        for cx, cy in ((4, 4), (11, 11)):
            for dx, dy in ((0,-1),(1,0),(0,1),(-1,0)): P(im, cx+dx, cy+dy, th['fleck'][1])
            P(im, cx-1, cy-1, th['fleck'][2])
    return im

def floor_shadow(th, v):
    im = floor_tile(th, v*4)
    for y in range(6):
        for x in range(T):
            if y < 3 or (y < 5 and (x + y + v) % 3 == 0) or (y == 5 and (x + v) % 5 == 0):
                P(im, x, y, th['shadow'])
    return im

# ---------------- paredes de membrana ----------------
def cap_fill(im, th, x0=0, y0=0, w=T, h=T, seed=0):
    c = th['cap']; rnd = random.Random(seed)
    for y in range(y0, y0+h):
        for x in range(x0, x0+w): P(im, x, y, c[1])
    # células de la membrana vistas desde arriba (dentro del tile, sin tocar bordes)
    for cx, cy, r in ((4, 4, 2.6), (11, 5, 2.2), (7, 11, 2.8), (13, 12, 1.8)):
        for y in range(T):
            for x in range(T):
                d = math.hypot(x+.5-cx, y+.5-cy)
                if x0 <= x < x0+w and y0 <= y < y0+h:
                    if r-1 <= d <= r: P(im, x, y, c[0])
                    elif d < r-1: P(im, x, y, c[2])
        P(im, int(cx-0.8), int(cy-0.8), c[3])

def wall_cap(th): im = new(); cap_fill(im, th); return im

def face_tile(th, part, v):
    """part: 'upper' (pegada a la tapa) o 'lower' (toca el suelo)."""
    im = new(); f = th['face']
    for y in range(T):
        yy = y + (0 if part == 'upper' else T)          # 0..31 a lo largo de la cara
        for x in range(T):
            # pliegues verticales de la membrana
            fold = math.sin((x + v*5) * 2*math.pi/8)
            g = 3 - yy/9 + fold*0.6
            P(im, x, y, f[max(0, min(4, int(round(g))))])
    if part == 'upper':
        for x in range(T): P(im, x, 0, OUTL); P(im, x, 1, th['lip'])       # borde con la tapa
    else:
        for x in range(T): P(im, x, T-1, OUTL)
        for x in (3 + v*2, 11 - v):                                      # gotas que cuelgan
            for d in range(2 + (x % 3)): P(im, x, T-2-d, th['drip'])
            P(im, x, T-1, th['drip'])
    # venas
    rnd = random.Random(7 + v + (0 if part == 'upper' else 50))
    x = rnd.randrange(3, 13)
    for y in range(2 if part == 'upper' else 0, T-2):
        x += rnd.choice((-1, 0, 0, 1)); x = max(1, min(14, x)); P(im, x, y, th['vein'])
    return im

def edge_tile(th, side):
    """Pared vista desde arriba con el labio hacia el suelo. side: 'left' (pared izquierda), 'right', 'bottom'."""
    im = new(); cap_fill(im, th, seed=3)
    for i in range(T):
        if side == 'left':   P(im, T-1, i, OUTL); P(im, T-2, i, th['lip']); P(im, T-3, i, th['face'][3])
        if side == 'right':  P(im, 0, i, OUTL); P(im, 1, i, th['lip']); P(im, 2, i, th['face'][3])
        if side == 'bottom': P(im, i, 0, OUTL); P(im, i, 1, th['lip']); P(im, i, 2, th['face'][3])
    return im

def corner_tile(th, side):
    im = new(); cap_fill(im, th, seed=5)
    # esquina interior: solo un pico de labio en la esquina que da al suelo
    xs = (T-1, T-2, T-3) if side == 'BL' else (0, 1, 2)
    for k, col in enumerate((OUTL, C(th['lip']), C(th['face'][3]))):
        P(im, xs[k], 0, col); P(im, xs[k], 1, col if k else OUTL)
        P(im, xs[0], k, OUTL)
    return im

# ---------------- puertas ----------------
def doorway_top(th):
    """Arco abierto en la cara de la pared superior (2x2 tiles). Va en lugar de 4 tiles de cara."""
    im = new(32, 32)
    for y in range(32):
        for x in range(32):
            src = face_tile(th, 'upper' if y < 16 else 'lower', 0)
            im.putpixel((x, y), src.getpixel((x % 16, y % 16)))
    for y in range(32):
        for x in range(32):
            nx, ny = (x+.5-16)/12, (y+.5-32)/28
            d = nx*nx + ny*ny
            if d <= 1: P(im, x, y, '#0a0610' if d < 0.7 else '#1a0e20')
            elif d <= 1.18: P(im, x, y, th['lip'] if y < 20 else th['face'][3])
            elif d <= 1.3: P(im, x, y, OUTL)
    return im

def iris(w, h, k, th, shape='ellipse'):
    """Membrana tipo esfínter. k=0 abierta (casi invisible) .. 4 cerrada."""
    im = new(w, h); f = th['face']; hole = (1.0, 0.72, 0.45, 0.2, 0.0)[k]
    cx, cy = w/2, h/2
    for y in range(h):
        for x in range(w):
            nx, ny = (x+.5-cx)/(w/2-1), (y+.5-cy)/(h/2-1)
            r = math.hypot(nx, ny) if shape == 'ellipse' else max(abs(nx), abs(ny)) * 0.75 + math.hypot(nx, ny) * 0.25
            if r > 1: continue
            if r < hole: continue
            a = math.atan2(ny, nx)
            fold = math.sin(a*8 + (1-r)*3)            # pliegues en espiral hacia el centro
            g = 1.6 + fold*0.9 + (r-0.5)*0.8
            P(im, x, y, f[max(0, min(4, int(round(g))))])
    # contorno del borde exterior y del agujero
    src = im.copy()
    for y in range(h):
        for x in range(w):
            if src.getpixel((x, y))[3] == 0: continue
            if any(not (0 <= x+dx < w and 0 <= y+dy < h) or src.getpixel((x+dx, y+dy))[3] == 0 for dx, dy in ((1,0),(-1,0),(0,1),(0,-1))):
                P(im, x, y, OUTL)
    if k == 4:
        for dx, dy in ((0,0),(1,0),(0,1),(1,1)): P(im, int(cx)-1+dx, int(cy)-1+dy, f[0])
        P(im, int(cx)-1, int(cy)-1, OUTL)
    return im

def door_top_frames(th):
    out = []
    for k in range(5):
        im = new(32, 32)
        ir = iris(24, 28, k, th)
        # recorta el iris a la forma del arco
        for y in range(28):
            for x in range(24):
                X, Y = x + 4, y + 4
                nx, ny = (X+.5-16)/12, (Y+.5-32)/28
                c = ir.getpixel((x, y))
                if c[3] and nx*nx + ny*ny <= 1.02: im.putpixel((X, Y), c)
        out.append(im)
    return out

# ---------------- decoración (no bloquea) ----------------
def deco_bubbles(th, k):
    im = new()
    for cx, cy, r in ((5, 10, 2.6), (10, 6, 1.8), (11, 12, 1.2)):
        rr = r + (0.4 if (k + int(cx)) % 2 else 0)
        for a in range(24):
            P(im, round(cx + math.cos(a*math.pi/12)*rr), round(cy + math.sin(a*math.pi/12)*rr), th['fleck'][2])
        P(im, round(cx - rr*0.5), round(cy - rr*0.5), '#ffffff')
    return im

def deco_diatom_old(th):
    im = new(); c = 7.5
    for y in range(T):
        for x in range(T):
            d = math.hypot(x+.5-8, y+.5-8)
            if d <= 6:
                a = math.atan2(y+.5-8, x+.5-8)
                spoke = abs(math.sin(a*6)) < 0.25
                P(im, x, y, '#d8c070' if d > 5 else ('#f0e0a0' if spoke or d < 1.5 else '#a89040'))
    for y in range(T):
        for x in range(T):
            if im.getpixel((x, y))[3] == 0 and any(0 <= x+dx < T and 0 <= y+dy < T and im.getpixel((x+dx, y+dy))[3] and im.getpixel((x+dx, y+dy)) != OUTL for dx, dy in ((1,0),(-1,0),(0,1),(0,-1))):
                im.putpixel((x, y), (1, 1, 1, 1))
    for y in range(T):
        for x in range(T):
            if im.getpixel((x, y)) == (1, 1, 1, 1): im.putpixel((x, y), OUTL)
    P(im, 5, 5, '#ffffff'); return im

def deco_diatom(th):
    im = new()
    for y in range(T):
        for x in range(T):
            d = math.hypot(x+.5-8, y+.5-8)
            if d <= 6:
                a = math.atan2(y+.5-8, x+.5-8)
                spoke = abs(math.sin(a*6)) < 0.25
                P(im, x, y, '#d8c070' if d > 5 else ('#f0e0a0' if spoke or d < 1.5 else '#a89040'))
    _outline(im); P(im, 5, 5, '#ffffff'); return im

def deco_diatom_long(th):
    im = new()
    for y in range(T):
        for x in range(T):
            if ((x+.5-8)/7)**2 + ((y+.5-8)/2.5)**2 <= 1:
                P(im, x, y, '#f0e0a0' if x % 3 == 0 else '#c8a850')
    _outline(im); P(im, 4, 7, '#ffffff'); return im

def deco_algae(th, k):
    im = new(); a = th['algae']
    for s, (bx, hgt) in enumerate(((5, 13), (9, 10), (12, 7))):
        for i in range(hgt):
            sway = round(math.sin(i/3 + k*0.9 + s) * (i/hgt) * 1.8)
            P(im, bx + sway, 15 - i, a[1 + (1 if i > hgt - 4 else 0)])
            if i % 4 == 2: P(im, bx + sway + (1 if s % 2 else -1), 15 - i, a[2])
    _outline(im); return im

def deco_specks(th, k):
    im = new(); rnd = random.Random(40 + k)
    for _ in range(6):
        x, y = rnd.randrange(0, 16), rnd.randrange(0, 16); P(im, x, y, th['fleck'][2 if rnd.random() < 0.3 else 1])
    return im

def deco_remains(th, k):
    """Restos de células infectadas (sala del jefe)."""
    im = new(); rnd = random.Random(70 + k)
    for cx, cy, r in ((7, 9, 4.5), (11, 5, 2.5)) if k == 0 else ((8, 8, 5),):
        for y in range(T):
            for x in range(T):
                d = math.hypot(x+.5-cx, y+.5-cy) + math.sin(math.atan2(y-cy, x-cx)*5)*0.6
                if d <= r: P(im, x, y, '#6a5a50' if d > r-1.2 else '#8a7868')
        for _ in range(3): P(im, cx + rnd.randint(-2, 2), cy + rnd.randint(-2, 2), '#ff7aa8')
    _outline(im); return im

def deco_goo(th):
    im = new(32, 16)
    for y in range(16):
        for x in range(32):
            if ((x+.5-16)/14)**2 + ((y+.5-8)/5.5)**2 + math.sin(x*0.7)*0.08 <= 1:
                P(im, x, y, '#a8203c' if ((x+.5-13)/8)**2 + ((y+.5-6)/2.5)**2 > 1 else '#ff4060')
    P(im, 10, 5, '#ffb0b8'); P(im, 11, 5, '#ffb0b8'); return im

def _outline(im):
    w, h = im.size; add = []
    for y in range(h):
        for x in range(w):
            if im.getpixel((x, y))[3] == 0 and any(0 <= x+dx < w and 0 <= y+dy < h and im.getpixel((x+dx, y+dy))[3] for dx, dy in ((1,0),(-1,0),(0,1),(0,-1))):
                add.append((x, y))
    for p in add: im.putpixel(p, OUTL)

# ---------------- obstáculos (bloquean) ----------------
def shade_blob(im, fn, cx, cy, r, ramp):
    for y in range(im.height):
        for x in range(im.width):
            if fn(x+.5, y+.5):
                nx, ny = (x+.5-cx)/r, (y+.5-cy)/r
                v = (-0.6*nx - 0.8*ny)*0.8 + 0.35 - max(0, math.hypot(nx, ny)-0.75)*1.2
                P(im, x, y, ramp[0 if v < -0.15 else 1 if v < 0.35 else 2 if v < 0.75 else 3])

def obstacle_rock(th, big=False):
    n = 32 if big else 16; im = new(n, n); rnd = random.Random(9 + big)
    ramp = ['#2a3a3a', '#46605a', '#6a8a7a', '#9ab8a0']
    parts = [(n*0.5, n*0.58, n*0.38)] + ([(n*0.3, n*0.65, n*0.22), (n*0.7, n*0.45, n*0.25)] if big else [(n*0.32, n*0.7, n*0.22)])
    fn = lambda x, y: any(math.hypot(x-cx, (y-cy)*1.15) <= r for cx, cy, r in parts)
    shade_blob(im, fn, n*0.5, n*0.55, n*0.4, ramp)
    for _ in range(4 if big else 2):  # musgo
        x, y = rnd.randrange(int(n*0.25), int(n*0.75)), rnd.randrange(int(n*0.25), int(n*0.5))
        if im.getpixel((x, y))[3]: P(im, x, y, th['algae'][2])
    _outline(im); return im

def obstacle_colony(th):
    im = new(32, 32); rnd = random.Random(21)
    ramp = ['#8a3a2a', '#d0603a', '#f2934e', '#ffd08a']
    dome = lambda x, y: ((x-16)/13)**2 + ((y-18)/11)**2 <= 1
    shade_blob(im, dome, 16, 18, 12, ['#5a3020', '#8a4a30', '#b06a40', '#d08a58'])
    for _ in range(9):   # bacterias pegadas a la colonia
        cx, cy = rnd.uniform(8, 24), rnd.uniform(11, 25)
        for y in range(int(cy)-2, int(cy)+3):
            for x in range(int(cx)-3, int(cx)+4):
                if ((x+.5-cx)/2.6)**2 + ((y+.5-cy)/1.5)**2 <= 1: P(im, x, y, ramp[1 if y > cy else 2])
        P(im, int(cx)-1, int(cy)-1, ramp[3])
    _outline(im); return im

# ---------------- generar ----------------
def build(theme):
    th = THEMES[theme]; d = f'{OUT}/{theme}'; os.makedirs(f'{d}/tiles', exist_ok=True)
    tiles = {}
    for v in range(6): tiles[f'Floor_{v}'] = floor_tile(th, v)
    for v in range(2): tiles[f'Floor_ShadowTop_{v}'] = floor_shadow(th, v)
    tiles['Wall_Cap'] = wall_cap(th)
    for v in range(2):
        tiles[f'Wall_FaceUpper_{v}'] = face_tile(th, 'upper', v); tiles[f'Wall_FaceLower_{v}'] = face_tile(th, 'lower', v)
    tiles['Wall_EdgeLeft'] = edge_tile(th, 'left'); tiles['Wall_EdgeRight'] = edge_tile(th, 'right'); tiles['Wall_EdgeBottom'] = edge_tile(th, 'bottom')
    tiles['Wall_CornerBL'] = corner_tile(th, 'BL'); tiles['Wall_CornerBR'] = corner_tile(th, 'BR')
    for n, im in tiles.items(): im.save(f'{d}/tiles/{n}.png')
    # atlas 8 columnas
    names = list(tiles); cols = 8; rows = math.ceil(len(names)/cols)
    atlas = new(cols*T, rows*T)
    for i, n in enumerate(names): atlas.paste(tiles[n], ((i % cols)*T, (i // cols)*T))
    atlas.save(f'{d}/Tileset_{theme}.png')
    # puertas
    dt = doorway_top(th); dt.save(f'{d}/Doorway_Top.png')
    frames = {'Door_Top': door_top_frames(th), 'Door_Bottom': [iris(32, 16, k, th) for k in range(5)],
              'Door_Side': [iris(16, 32, k, th) for k in range(5)]}
    for n, fr in frames.items():
        w, h = fr[0].size; sh = new(w*len(fr), h)
        for i, im in enumerate(fr): sh.paste(im, (i*w, 0))
        sh.save(f'{d}/{n}.png')
    # decoración y obstáculos
    deco = {}
    if theme == 'Soup':
        deco['Deco_Bubbles'] = [deco_bubbles(th, k) for k in range(2)]
        deco['Deco_Diatom'] = [deco_diatom(th)]
        deco['Deco_DiatomLong'] = [deco_diatom_long(th)]
        deco['Deco_Algae'] = [deco_algae(th, k) for k in range(4)]
        deco['Deco_Specks'] = [deco_specks(th, k) for k in range(3)]
        deco['Obstacle_Rock'] = [obstacle_rock(th)]
        deco['Obstacle_RockBig'] = [obstacle_rock(th, True)]
        deco['Obstacle_Colony'] = [obstacle_colony(th)]
    else:
        deco['Deco_Bubbles'] = [deco_bubbles(th, k) for k in range(2)]
        deco['Deco_Algae'] = [deco_algae(th, k) for k in range(4)]
        deco['Deco_InfectedRemains'] = [deco_remains(th, k) for k in range(2)]
        deco['Deco_Goo'] = [deco_goo(th)]
    os.makedirs(f'{d}/deco', exist_ok=True)
    for n, fr in deco.items():
        w, h = fr[0].size; sh = new(w*len(fr), h)
        for i, im in enumerate(fr): sh.paste(im, (i*w, 0))
        sh.save(f'{d}/deco/{n}.png')
    return tiles, dt, frames, deco

def sample_room(theme, tiles, dt, frames, deco, door_frame):
    W, H = 22, 15; im = new(W*T, H*T); rnd = random.Random(5)
    for y in range(H):
        for x in range(W):
            if y == 0: t = 'Wall_Cap'
            elif y == 1 and 0 < x < W-1: t = f'Wall_FaceUpper_{x % 2}'
            elif y == 2 and 0 < x < W-1: t = f'Wall_FaceLower_{(x // 3) % 2}'
            elif y == H-1: t = 'Wall_CornerBL' if x == 0 else 'Wall_CornerBR' if x == W-1 else 'Wall_EdgeBottom'
            elif x == 0: t = 'Wall_EdgeLeft'
            elif x == W-1: t = 'Wall_EdgeRight'
            elif y == 3: t = f'Floor_ShadowTop_{x % 2}'
            else: t = f'Floor_{rnd.choice([0]*10 + [2,3,4,5] + [1])}'
            im.paste(tiles[t], (x*T, y*T))
    # puerta superior (arco) y laterales/inferior (hueco con suelo)
    im.paste(dt, (10*T, 1*T))
    for (x, y) in ((10, H-1), (11, H-1), (0, 7), (0, 8), (W-1, 7), (W-1, 8)): im.paste(tiles['Floor_0'], (x*T, y*T))
    im.alpha_composite(frames['Door_Top'][door_frame], (10*T, 1*T))
    im.alpha_composite(frames['Door_Bottom'][door_frame], (10*T, (H-1)*T))
    im.alpha_composite(frames['Door_Side'][door_frame], (0, 7*T))
    im.alpha_composite(frames['Door_Side'][door_frame].transpose(Image.FLIP_LEFT_RIGHT), ((W-1)*T, 7*T))
    # decoración
    spots = [(3, 5), (17, 4), (6, 11), (15, 12), (19, 9), (8, 6), (13, 9), (4, 9)]
    keys = [k for k in deco if k.startswith('Deco_')]
    for i, (x, y) in enumerate(spots):
        k = keys[i % len(keys)]; im.alpha_composite(deco[k][0], (x*T, y*T))
    return im

for theme in ('Soup', 'Boss'):
    tiles, dt, frames, deco = build(theme)
    for state, f in (('cerrada', 4), ('abierta', 0)):
        room = sample_room(theme, tiles, dt, frames, deco, f)
        room.save(f'{OUT}/{theme}/Sala_muestra_{state}.png')
        room.resize((room.width*3, room.height*3), Image.NEAREST).save(f'{OUT}/{theme}/Sala_muestra_{state}_x3.png')

# ---------- vista previa de piezas y GIF de las puertas ----------
BG = C('#0c1a16')
for theme in ('Soup', 'Boss'):
    d = f'{OUT}/{theme}'; S = 4
    parts = [Image.open(f'{d}/Tileset_{theme}.png'), Image.open(f'{d}/Doorway_Top.png'), Image.open(f'{d}/Door_Top.png'),
             Image.open(f'{d}/Door_Bottom.png'), Image.open(f'{d}/Door_Side.png')] + \
            [Image.open(f'{d}/deco/{n}') for n in sorted(os.listdir(f'{d}/deco'))]
    W = max(p.width for p in parts)*S + 20; H = sum(p.height*S + 10 for p in parts) + 10
    pv = Image.new('RGBA', (W, H), BG); y = 10
    for p in parts:
        pv.alpha_composite(p.resize((p.width*S, p.height*S), Image.NEAREST), (10, y)); y += p.height*S + 10
    pv.save(f'{d}/vista_previa_piezas.png')
    for n in ('Door_Top', 'Door_Bottom', 'Door_Side'):
        sh = Image.open(f'{d}/{n}.png'); w = {'Door_Top': 32, 'Door_Bottom': 32, 'Door_Side': 16}[n]; h = sh.height
        fr = []
        for k in list(range(5)) + [4, 4, 3, 2, 1, 0, 0]:
            g = Image.new('RGBA', (w, h), C('#1d4639')); g.alpha_composite(sh.crop((k*w, 0, k*w+w, h)))
            fr.append(g.resize((w*6, h*6), Image.NEAREST).convert('RGB'))
        os.makedirs(f'{d}/gif', exist_ok=True)
        fr[0].save(f'{d}/gif/{n}.gif', save_all=True, append_images=fr[1:], duration=90, loop=0)
