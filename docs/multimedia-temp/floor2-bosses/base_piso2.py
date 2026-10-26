"""Utilidades comunes de los jefes del piso 2 (mismo estilo que ../floor2-enemies/generar_enemigos_piso2.py)."""
import math, os
from PIL import Image, ImageDraw

OUTL = (24, 20, 37, 255)

def C(h, a=255):
    if isinstance(h, tuple): return h[:3] + (a,)
    h = h.lstrip('#'); return tuple(int(h[i:i+2], 16) for i in (0, 2, 4)) + (a,)

EYE = C('#f4f4e8')
SPARK = [C('#ffffff'), C('#fff6c0'), C('#ffe080')]
WHITE4 = ['#ffffff'] * 4
RED4 = ['#a33b3b', '#e05a5a', '#ff9a8a', '#fff0e0']
PALE4 = ['#6a6450', '#9a9478', '#c4c0a4', '#ecead8']
STONE4 = ['#4a4640', '#6e6a62', '#948f84', '#b8b2a4']
SAND = ['#4a3c28', '#7a6646', '#ad9670', '#dcc9a0']
BUBBLE = (200, 236, 255)

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
            (x+dx, y+dy) in s.px and s.px[(x+dx, y+dy)] != OUTL and s.px[(x+dx, y+dy)][3] > 120
            for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)))]
        for p in add: s.px[p] = OUTL
    def img(s):
        im = Image.new('RGBA', (s.n, s.n), (0, 0, 0, 0))
        for p, c in s.px.items(): im.putpixel(p, c)
        return im

def shade(nx, ny):
    v = (-0.6*nx - 0.8*ny) * 0.8 + 0.35 - max(0, math.hypot(nx, ny) - 0.75) * 1.2
    return 0 if v < -0.15 else 1 if v < 0.35 else 2 if v < 0.75 else 3

def fill(cv, inside, cx, cy, rx, ry, ramp, alpha=255, shift=0, box=None):
    ramp = [C(c, alpha) for c in ramp]
    x0, y0, x1, y1 = box or (0, 0, cv.n, cv.n)
    for y in range(max(0, int(y0)), min(cv.n, int(y1) + 1)):
        for x in range(max(0, int(x0)), min(cv.n, int(x1) + 1)):
            px, py = x + .5, y + .5
            if inside(px, py):
                cv.px[(x, y)] = ramp[max(0, min(3, shade((px - cx) / rx, (py - cy) / ry) + shift))]

def ellipse(cv, cx, cy, rx, ry, ramp, alpha=255, shift=0):
    fill(cv, lambda x, y: ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1, cx, cy, rx, ry, ramp, alpha, shift,
         box=(cx - rx - 1, cy - ry - 1, cx + rx + 1, cy + ry + 1))

def rot_ellipse(cv, cx, cy, rx, ry, ang, ramp, alpha=255, shift=0):
    ca, sa = math.cos(ang), math.sin(ang)
    def inside(x, y):
        dx, dy = x - cx, y - cy
        u, v = dx * ca + dy * sa, -dx * sa + dy * ca
        return (u / rx) ** 2 + (v / ry) ** 2 <= 1
    r = max(rx, ry)
    fill(cv, inside, cx, cy, r, r, ramp, alpha, shift, box=(cx - r - 1, cy - r - 1, cx + r + 1, cy + r + 1))

def big_eye(cv, x, y, state='angry', red=False, look=1, size=3):
    """Ojo de jefe (size×size) mirando hacia look (1 derecha). Enfadado: ceja que tapa la esquina de arriba de dentro."""
    x, y = int(x), int(y)
    if state == 'none': return
    if state == 'x':
        for i in range(size):
            cv.put(x + i, y + i, OUTL); cv.put(x + size - 1 - i, y + i, OUTL)
        return
    if state == 'closed':
        for i in range(size): cv.put(x + i, y + size - 1, OUTL)
        return
    white = C('#ffd0c8') if red else EYE
    for dx in range(size):
        for dy in range(size): cv.put(x + dx, y + dy, white)
    if state == 'tired':
        for i in range(size): cv.put(x + i, y, OUTL)
    if state in ('angry', 'tired'):
        pc = C('#ff3020') if red else OUTL
        px = x + size - 2 if look > 0 else x
        for dx in range(2):
            for dy in range(2): cv.put(px + dx, y + size - 2 + dy, pc)
        if state == 'angry': cv.put(x if look > 0 else x + size - 1, y - 1, OUTL); cv.put(x + 1, y - 1, OUTL)

def sparks(cv, x, y, size=2):
    cv.put(x, y, SPARK[0])
    for k in range(1, size + 1):
        c = SPARK[min(k, 2)]
        for dx, dy in ((k, 0), (-k, 0), (0, k), (0, -k)): cv.put(x + dx, y + dy, c)
    if size >= 2:
        for dx, dy in ((1, 1), (-1, -1), (1, -1), (-1, 1)): cv.put(x + dx, y + dy, SPARK[1])

def bubble(cv, x, y, r, a=210):
    for yy in range(int(y - r) - 1, int(y + r) + 2):
        for xx in range(int(x - r) - 1, int(x + r) + 2):
            d = math.hypot(xx + .5 - x, yy + .5 - y)
            if r - 0.9 <= d <= r: cv.put_out(xx, yy, BUBBLE + (a,))
    cv.put_out(x - r * 0.4, y - r * 0.4, (255, 255, 255, a))

def as_img(f): return f if isinstance(f, Image.Image) else f.img()

def save_sheet(path, frames):
    w, h = frames[0].size
    sheet = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, im in enumerate(frames): sheet.paste(im, (i * w, 0))
    sheet.save(path)

def preview(path, anims, bg, scale=2, cols=None):
    rows = list(anims)
    cell = max(max(f.width for f in v) for v in anims.values()) * scale + 8
    cols = cols or max(len(v) for v in anims.values())
    W = 170 + cell * cols; rh = cell
    im = Image.new('RGBA', (W, 10 + len(rows) * rh), bg); d = ImageDraw.Draw(im)
    for r, name in enumerate(rows):
        y0 = 10 + r * rh
        d.text((8, y0 + rh // 2 - 6), name.replace('_', ' '), fill=C('#bfe6ff'))
        for i, f in enumerate(anims[name]):
            big = f.resize((f.width * scale, f.height * scale), Image.NEAREST)
            im.alpha_composite(big, (170 + i * cell + (cell - big.width) // 2, y0 + (rh - big.height) // 2))
    im.save(path)

def gif(path, frames, ms, bg, scale=3):
    n = max(f.width for f in frames)
    out = []
    for f in frames:
        b = Image.new('RGBA', (n, n), bg); b.alpha_composite(f, ((n - f.width) // 2, (n - f.height) // 2))
        out.append(b.resize((n * scale, n * scale), Image.NEAREST).convert('RGB'))
    out[0].save(path, save_all=True, append_images=out[1:], duration=ms, loop=0)
