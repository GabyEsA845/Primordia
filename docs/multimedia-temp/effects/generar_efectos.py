"""Sprites de efectos visuales de Primordia. Uso: python3 effects.py <carpeta>"""
import math, sys, os, random
from PIL import Image, ImageDraw
OUT = sys.argv[1]
OUTL = (24, 20, 37, 255)
def C(h, a=255): h = h.lstrip('#'); return tuple(int(h[i:i+2], 16) for i in (0, 2, 4)) + (a,)

class Canvas:
    def __init__(s, n): s.n = n; s.px = {}
    def put(s, x, y, c):
        x, y = int(math.floor(x)), int(math.floor(y))
        if 0 <= x < s.n and 0 <= y < s.n: s.px[(x, y)] = C(c) if isinstance(c, str) else c
    def outline(s, col=OUTL):
        add = [(x, y) for y in range(s.n) for x in range(s.n) if (x, y) not in s.px and
               any((x+dx, y+dy) in s.px and s.px[(x+dx, y+dy)] != col for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)))]
        for p in add: s.px[p] = col
    def img(s):
        im = Image.new('RGBA', (s.n, s.n), (0, 0, 0, 0))
        for p, c in s.px.items(): im.putpixel(p, c)
        return im

def disc(cv, cx, cy, r, ramp, alpha=255):
    ramp = [C(c, alpha) for c in ramp]
    for y in range(int(cy-r-1), int(cy+r+2)):
        for x in range(int(cx-r-1), int(cx+r+2)):
            nx, ny = (x+.5-cx)/r, (y+.5-cy)/r
            if nx*nx + ny*ny <= 1:
                v = (-0.6*nx - 0.8*ny)*0.8 + 0.35 - max(0, math.hypot(nx, ny)-0.75)*1.2
                cv.put(x, y, ramp[0 if v < -0.15 else 1 if v < 0.35 else 2 if v < 0.75 else 3])

def ring(cv, cx, cy, r, w, cols):
    """Anillo de grosor w; cols de dentro a fuera."""
    for y in range(cv.n):
        for x in range(cv.n):
            d = math.hypot(x+.5-cx, y+.5-cy)
            if r - w <= d <= r:
                k = min(len(cols)-1, int((d - (r-w)) / w * len(cols)))
                cv.put(x, y, cols[k])

# ---------- Impacto (12x12): estrella que se abre y se apaga ----------
def impact(f, core, mid):
    cv = Canvas(12); c = 6
    L = (2, 4, 5, 5)[f]
    if f < 3:
        for d in range(L):
            for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)): cv.put(c-0.5+dx*d, c-0.5+dy*d, core if d < L-1 else mid)
        for d in range(max(0, L-2)):
            for dx, dy in ((1,1),(-1,-1),(1,-1),(-1,1)): cv.put(c-0.5+dx*d, c-0.5+dy*d, mid)
        if f == 0: cv.put(c-0.5, c-0.5, '#ffffff')
        else: cv.px.pop((5, 5), None) if f == 2 else None
    else:
        for dx, dy in ((4,0),(-4,0),(0,4),(0,-4),(3,3),(-3,-3),(3,-3),(-3,3)): cv.put(c-0.5+dx, c-0.5+dy, mid)
    return cv

# ---------- Muerte de enemigo (24x24): burbujas que suben y revientan ----------
def death_bubbles(f):
    cv = Canvas(24); random.seed(7)
    bubbles = [(12, 14, 4), (7, 16, 2.5), (17, 15, 3), (10, 19, 1.8), (15, 19, 2)]
    for i, (bx, by, r) in enumerate(bubbles):
        rise = f*2.2 + i*0.4
        rr = r * (1 + f*0.12)
        if f >= 3 + (i % 2):  # revienta: queda un anillo de gotitas
            if f == 3 + (i % 2):
                for k in range(6):
                    a = k*math.pi/3
                    cv.put(bx + math.cos(a)*(rr+1), by - rise + math.sin(a)*(rr+1), '#d0fff0')
            continue
        cy = by - rise
        for y in range(int(cy-rr-1), int(cy+rr+2)):
            for x in range(int(bx-rr-1), int(bx+rr+2)):
                d = math.hypot(x+.5-bx, y+.5-cy)
                if rr-1.1 <= d <= rr: cv.put(x, y, '#a0f0d8')
        cv.put(bx - rr*0.5, cy - rr*0.5, '#ffffff')
    if f == 0:
        for k in range(8):
            a = k*math.pi/4
            cv.put(12 + math.cos(a)*7, 13 + math.sin(a)*6, '#ffffff')
    return cv

# ---------- Estela de esquiva (16x16): silueta fantasma de la célula ----------
def dodge_trail(f):
    cv = Canvas(16); r = (6.2, 5.2, 4.0, 2.6)[f]
    cols = [C('#60d0ff', a) for a in (90, 70, 50, 30)]
    for y in range(16):
        for x in range(16):
            nx, ny = (x+.5-8)/r, (y+.5-9)/(r*0.93)
            if nx*nx + ny*ny <= 1: cv.px[(x, y)] = cols[f] if nx*nx+ny*ny > 0.45 else C('#b0f0ff', (120, 90, 60, 40)[f])
    return cv

# ---------- Veneno (12x12): burbujas verdes que suben sobre el enemigo ----------
def poison(f):
    cv = Canvas(12)
    for i, (bx, r) in enumerate(((3, 1.6), (7, 2.1), (10, 1.3))):
        y = 10 - ((f*3 + i*4) % 12)
        if y < 1: continue
        for yy in range(int(y-r-1), int(y+r+2)):
            for xx in range(int(bx-r-1), int(bx+r+2)):
                d = math.hypot(xx+.5-bx, yy+.5-y)
                if d <= r: cv.put(xx, yy, '#a8e040' if d > r-1 else '#5aa020')
        cv.put(bx-0.5, y-0.8, '#e8ffc0')
    cv.outline(); return cv

# ---------- Nube de tinta (96x96 = radio 3): se expande, se mantiene y se disipa ----------
INK = ['#0e0e24', '#1c1c40', '#2e2e60', '#4a4a88']
def ink_cloud(f):
    cv = Canvas(96); c = 48; random.seed(11)
    grow = (0.35, 0.65, 0.9, 1.0, 1.0, 1.0, 0.85, 0.6, 0.35)[f]
    puffs = [(0, 0, 26)] + [(math.cos(k*math.pi/4 + 0.3)*26, math.sin(k*math.pi/4 + 0.3)*26, 15 + (k % 3)*2) for k in range(8)] + \
            [(math.cos(k*math.pi/3)*13, math.sin(k*math.pi/3)*13, 16) for k in range(6)]
    wob = (0, 0, 0, 0, 1, -1, 0, 0, 0)[f]
    for i, (dx, dy, r) in enumerate(puffs):
        if f >= 6 and i % (10 - f) == 0 and i: continue      # se rompe al disiparse
        rr = r * grow + (wob if i % 2 else -wob)
        disc(cv, c + dx*grow, c + dy*grow, max(1, rr), INK)
    # ojos de tinta: remolinos claros
    if 2 <= f <= 6:
        for k in range(5):
            a = k*1.3 + f*0.5
            for t in range(6):
                cv.put(c + math.cos(a + t*0.35)*(8 + k*4 + t), c + math.sin(a + t*0.35)*(8 + k*4 + t), INK[3])
    cv.outline(); return cv

# ---------- Onda de choque (128x128 = radio 4): anillo eléctrico que se expande ----------
def shockwave(f):
    cv = Canvas(128); c = 64
    r, w = ((14, 6), (30, 6), (44, 5), (54, 4), (61, 3), (63, 2))[f]
    ring(cv, c, c, r, w, ['#2a90c0', '#60d0ff', '#c0f4ff', '#ffffff'][: max(2, w-1)] if f < 5 else ['#60d0ff', '#2a90c0'])
    # chispas sobre el anillo
    random.seed(f)
    for k in range(10 + f*2):
        a = random.random()*2*math.pi
        for t in range(3):
            cv.put(c + math.cos(a)*(r - w/2 + t - 1), c + math.sin(a)*(r - w/2 + (t % 2)), '#ffffff')
    if f == 0: disc(cv, c, c, 8, ['#2a90c0', '#60d0ff', '#c0f4ff', '#ffffff'])
    return cv

anims = {
    'Impact_Player': ([impact(i, '#ffffff', '#c8f060') for i in range(4)], 50, 'Golpe del jugador a un enemigo'),
    'Impact_Enemy':  ([impact(i, '#ffffff', '#ff4f9a') for i in range(4)], 50, 'Golpe enemigo o choque con pared'),
    'EnemyDeath_Bubbles': ([death_bubbles(i) for i in range(6)], 80, 'Burbujas genéricas al morir (encima de la muerte de cada enemigo)'),
    'DodgeTrail': ([dodge_trail(i) for i in range(4)], 50, 'Silueta que deja la célula al esquivar'),
    'Poison': ([poison(i) for i in range(4)], 120, 'Indicador de veneno sobre el enemigo (bucle)'),
    'InkCloud': ([ink_cloud(i) for i in range(9)], 110, 'Vejiga de tinta: aparece, se mantiene y se disipa (1 s)'),
    'Shockwave': ([shockwave(i) for i in range(6)], 60, 'Glándula de choque: anillo que se expande (0,35 s)'),
}
os.makedirs(f'{OUT}/frames', exist_ok=True); os.makedirs(f'{OUT}/gif', exist_ok=True)
BG = C('#16352e')
for name, (frames, ms, _) in anims.items():
    n = frames[0].n
    sheet = Image.new('RGBA', (n*len(frames), n), (0, 0, 0, 0)); gif = []
    S = max(1, 160 // n)
    prev = Image.new('RGBA', (len(frames)*(n*S+6)+6, n*S+12), BG)
    for i, cv in enumerate(frames):
        im = cv.img(); im.save(f'{OUT}/frames/{name}_{i}.png'); sheet.paste(im, (i*n, 0))
        big = im.resize((n*S, n*S), Image.NEAREST); prev.alpha_composite(big, (6 + i*(n*S+6), 6))
        g = Image.new('RGBA', (n, n), BG); g.alpha_composite(im); gif.append(g.resize((n*S, n*S), Image.NEAREST).convert('RGB'))
    sheet.save(f'{OUT}/{name}.png'); prev.save(f'{OUT}/_prev_{name}.png')
    gif[0].save(f'{OUT}/gif/{name}.gif', save_all=True, append_images=gif[1:], duration=ms, loop=0)
# vista previa conjunta
prevs = [Image.open(f'{OUT}/_prev_{n}.png') for n in anims]
W = max(p.width for p in prevs) + 160; H = sum(p.height + 8 for p in prevs) + 8
allp = Image.new('RGBA', (W, H), BG); d = ImageDraw.Draw(allp); y = 8
for n, p in zip(anims, prevs):
    d.text((8, y + p.height//2 - 6), n, fill=C('#d6ff9a')); allp.alpha_composite(p, (160, y)); y += p.height + 8
    os.remove(f'{OUT}/_prev_{n}.png')
allp.save(f'{OUT}/vista_previa_efectos.png')
