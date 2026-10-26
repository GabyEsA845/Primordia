"""Sprites de proyectiles de Primordia. Todos apuntan a la derecha (el código los rota). Uso: python3 projectiles.py <carpeta>"""
import math, sys, os
from PIL import Image, ImageDraw

OUT = sys.argv[1]
OUTL = (24, 20, 37, 255)
def C(h): h = h.lstrip('#'); return tuple(int(h[i:i+2], 16) for i in (0, 2, 4)) + (255,)

class Canvas:
    def __init__(s, w, h=None): s.w = w; s.h = h or w; s.px = {}
    def put(s, x, y, c):
        x, y = int(math.floor(x)), int(math.floor(y))
        if 0 <= x < s.w and 0 <= y < s.h: s.px[(x, y)] = C(c) if isinstance(c, str) else c
    def put_in(s, x, y, c):
        if (int(math.floor(x)), int(math.floor(y))) in s.px: s.put(x, y, c)
    def outline(s):
        add = [(x, y) for y in range(s.h) for x in range(s.w) if (x, y) not in s.px and any(
            s.px.get((x+dx, y+dy), OUTL) != OUTL for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)))]
        for p in add: s.px[p] = OUTL
    def img(s):
        im = Image.new('RGBA', (s.w, s.h), (0, 0, 0, 0))
        for p, c in s.px.items(): im.putpixel(p, c)
        return im

def shade(cv, fn, cx, cy, r, ramp):
    ramp = [C(c) for c in ramp]
    for y in range(cv.h):
        for x in range(cv.w):
            px, py = x + .5, y + .5
            if fn(px, py):
                nx, ny = (px - cx) / r, (py - cy) / r
                v = (-0.6*nx - 0.8*ny) * 0.8 + 0.35 - max(0, math.hypot(nx, ny) - 0.75) * 1.2
                cv.px[(x, y)] = ramp[0 if v < -0.15 else 1 if v < 0.35 else 2 if v < 0.75 else 3]
def ell(cx, cy, rx, ry): return lambda x, y: ((x-cx)/rx)**2 + ((y-cy)/ry)**2 <= 1

# ---- Proyectil base del jugador: gota de citoplasma (8x8, cuerpo ~6 px) ----
def player_shot(f):
    cv = Canvas(8); stretch = (0.3, -0.1)[f]
    shade(cv, ell(4.3, 4, 2.7 + stretch, 2.4 - stretch*0.5), 4.3, 4, 2.7, ['#7a8a20', '#c8d840', '#f0f878', '#ffffff'])
    cv.put_in(3.3, 3, '#ffffff'); cv.outline()
    cv.put(0, 4, '#c8d840') if f == 0 else cv.put(0, 3, '#c8d840')  # estela
    return cv

# ---- Mandíbulas: mordisco que se cierra (12x12) ----
def mandibles(f):
    cv = Canvas(12); gap = (3.2, 1.8, 0.6)[f]
    ramp = ['#a89a80', '#e0d8c0', '#fff8e8', '#ffffff']
    for sgn in (-1, 1):
        cy = 6 + sgn*gap
        fang = lambda x, y, cy=cy, sgn=sgn: ((x-5.5)/4.5)**2 + ((y-cy)/2.2)**2 <= 1 and sgn*(y-cy) >= -0.5 - (x-5)*0.25*0
        shade(cv, fang, 5.5, cy, 4, ramp)
        # dientes hacia el centro
        for tx in (4, 6, 8):
            cv.put(tx, cy - sgn*2.2, ramp[2])
        cv.put(9.5, cy - sgn*1.6, ramp[1]); cv.put(10, cy - sgn*1.0, ramp[1])
    cv.outline()
    if f == 2:  # chispa del golpe
        for p in ((11, 5), (11, 6)): cv.put(*p, '#ffffff')
    return cv

# ---- Aguijón: púa verde con gota de veneno (12x6) ----
def stinger(f):
    cv = Canvas(12, 6)
    spike = lambda x, y: 1 <= x <= 10.5 and abs(y - 3) <= 1.4 * (1 - (x - 1) / 10.5) + 0.35
    shade(cv, spike, 5, 3, 5, ['#3a6a20', '#6aa838', '#a8e060', '#e8ffc0'])
    cv.put(10, 3, '#2a3a20'); cv.put(9, 3, '#2a3a20')  # punta oscura
    cv.put(2 + f*3, 2, '#e8ffc0')                     # brillo que recorre la púa
    cv.outline()
    cv.put(4, 5 if f == 0 else 4, '#c8ff40') if False else None
    return cv

# ---- Espora: bola púrpura con pinchos que gira (10x10) ----
def spore(f):
    cv = Canvas(10); c = 5
    shade(cv, ell(c, c, 2.8, 2.8), c, c, 2.8, ['#4a2a7a', '#8a5ac8', '#c095f0', '#f0e0ff'])
    for k in range(4):
        a = k*math.pi/2 + f*math.pi/8
        cv.put(c + math.cos(a)*3.6, c + math.sin(a)*3.6, '#8a5ac8')
    cv.put_in(4, 4, '#ffffff'); cv.put_in(5.5, 5.5, '#4a2a7a')
    cv.outline(); return cv

# ---- Proyectil enemigo (ameba): orbe rosa fuerte con núcleo (10x10, cuerpo 8 px) ----
def enemy_shot(f):
    cv = Canvas(10); r = 3.6 + (0.3 if f else 0)
    shade(cv, ell(5, 5, r, r), 5, 5, r, ['#a01858', '#ff3c8c', '#ff90c0', '#ffffff'])
    for p in ((5, 5), (5, 6), (6, 5), (6, 6)): cv.put_in(*p if f else (p[0]-0.0, p[1]), '#ffd0e4')
    cv.put_in(3, 3, '#ffffff'); cv.outline(); return cv

# ---- Espiral del jefe: mini virión rosa que gira (10x10, cuerpo 8 px) ----
def boss_spiral(f):
    cv = Canvas(10); c = 5
    shade(cv, ell(c, c, 2.9, 2.9), c, c, 2.9, ['#8a1a50', '#ff4f9a', '#ffa0c8', '#ffffff'])
    for k in range(4):
        a = k*math.pi/2 + f*math.pi/8 + math.pi/4
        cv.put(c + math.cos(a)*3.8, c + math.sin(a)*3.8, '#d8f070')
    cv.put_in(4, 4, '#ffffff'); cv.outline(); return cv

# ---- Ráfaga del jefe: orbe naranja grande que late (14x14, cuerpo 12 px) ----
def boss_burst(f):
    cv = Canvas(14); r = 5.4 + (0.5 if f else 0)
    shade(cv, ell(7, 7, r, r), 7, 7, r, ['#a83a10', '#ff8a20', '#ffd060', '#fff8d0'])
    # núcleo incandescente
    core = [(6,6),(7,6),(6,7),(7,7)] + ([(5,6),(8,7),(6,5),(7,8)] if f else [])
    for p in core: cv.put_in(*p, '#fff8d0')
    cv.put_in(5, 4, '#ffffff'); cv.put_in(4, 5, '#ffffff'); cv.outline(); return cv

anims = {
    'Projectile_Player':     ([player_shot(i) for i in range(2)], 100, 'Base del jugador (sin mutación de Boca)', 'Projectile_Player'),
    'Projectile_Mandibles':  ([mandibles(i) for i in range(3)], 110, 'Mandíbulas: el mordisco se cierra (no hace bucle, dura 0,35 s)', 'Projectile_Mandibles'),
    'Projectile_Stinger':    ([stinger(i) for i in range(3)], 90, 'Aguijón: púa venenosa', 'Projectile_Stinger'),
    'Projectile_Spore':      ([spore(i) for i in range(4)], 100, 'Espora: gira despacio', 'Projectile_Spore'),
    'Projectile_Enemy':      ([enemy_shot(i) for i in range(2)], 140, 'Enemigo (ameba): late', 'Projectile_Enemy'),
    'Projectile_BossSpiral': ([boss_spiral(i) for i in range(4)], 90, 'Espiral del jefe: mini virión que gira', 'Projectile_BossSpiral'),
    'Projectile_BossBurst':  ([boss_burst(i) for i in range(2)], 140, 'Ráfaga del jefe: orbe grande que late', 'Projectile_BossBurst'),
}
os.makedirs(f'{OUT}/frames', exist_ok=True); os.makedirs(f'{OUT}/gif', exist_ok=True)
BG = C('#16352e'); S = 10
prev = Image.new('RGBA', (240 + 4*(16*S+10), len(anims)*(16*S+10) + 10), BG); d = ImageDraw.Draw(prev)
for r, (name, (frames, ms, label, _)) in enumerate(anims.items()):
    w, h = frames[0].w, frames[0].h
    sheet = Image.new('RGBA', (w*len(frames), h), (0, 0, 0, 0)); gif = []
    y0 = 10 + r*(16*S+10)
    d.text((10, y0 + 16*S//2 - 6), name.replace('Projectile_', ''), fill=C('#d6ff9a'))
    for i, cv in enumerate(frames):
        im = cv.img(); im.save(f'{OUT}/frames/{name}_{i}.png'); sheet.paste(im, (i*w, 0))
        big = im.resize((w*S, h*S), Image.NEAREST)
        prev.alpha_composite(big, (240 + i*(16*S+10) + (16*S - w*S)//2, y0 + (16*S - h*S)//2))
        g = Image.new('RGBA', (16, 16), BG); g.alpha_composite(im, ((16-w)//2, (16-h)//2))
        gif.append(g.resize((192, 192), Image.NEAREST).convert('RGB'))
    sheet.save(f'{OUT}/{name}.png')
    gif[0].save(f'{OUT}/gif/{name}.gif', save_all=True, append_images=gif[1:], duration=ms, loop=0)
prev.save(f'{OUT}/vista_previa_proyectiles.png')

# Escena de prueba: cómo se ven al tamaño real junto al jugador y el jefe (x4)
