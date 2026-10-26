"""Mordida de Mandíbulas (ataque cuerpo a cuerpo, sin proyectil). Apunta a la derecha. Uso: python3 bite.py <carpeta>"""
import math, sys, os
from PIL import Image
OUT = sys.argv[1]
OUTL = (24, 20, 37, 255)
def C(h): h = h.lstrip('#'); return tuple(int(h[i:i+2], 16) for i in (0, 2, 4)) + (255,)
BONE = ['#a89a80', '#e0d8c0', '#fff8e8', '#ffffff']
DIM = ['#7a7060', '#a89e88', '#ccc4b0', '#e4ded0']
GUM = '#c04060'
N = 24; CX = 12.5

def bite(gap, sparks=0, dim=False, w=9.0):
    ramp = [C(c) for c in (DIM if dim else BONE)]
    px = {}
    for sgn in (-1, 1):                 # -1 superior, 1 inferior
        edge = 12 + sgn*gap/2            # borde interior de la mandíbula
        for y in range(N):
            for x in range(N):
                fx, fy = x + .5, y + .5
                d = (fy - edge) * sgn    # >0 hacia fuera
                if d < 0: continue
                if ((fx-CX)/w)**2 + (d/5.2)**2 <= 1:
                    # sombreado: más claro arriba-izquierda
                    v = (0.6 if sgn < 0 else 0.1) + (0.5 - (fx - CX)/w*0.4) - d/10
                    if d < 1.2 and abs(fx-CX) < w-1.5: px[(x, y)] = C(GUM)       # encía
                    else: px[(x, y)] = ramp[0 if v < 0.2 else 1 if v < 0.55 else 2 if v < 0.9 else 3]
        # dientes hacia el hueco (colmillos más largos en los extremos)
        for tx, ln in ((CX-6, 3), (CX-3, 2), (CX, 2), (CX+3, 2), (CX+6, 3)):
            if gap < 2: break
            for k in range(ln):
                for dx in range(max(1, 2-k)):
                    x, y = int(tx + dx), int(edge - sgn*(k + 0.5))
                    if 0 <= x < N and 0 <= y < N: px[(x, y)] = ramp[3] if k < ln-1 else ramp[2]
    if gap < 2:  # boca cerrada: costura oscura con dientes entrelazados
        for x in range(int(CX-w+2), int(CX+w-1)):
            px[(x, 11)] = OUTL if x % 3 else ramp[3]
            px[(x, 12)] = OUTL if (x+1) % 3 else ramp[3]
        for x in range(int(CX-w+2), int(CX+w-1)):
            for y in (10, 13):
                if px.get((x, y)) == C(GUM): px[(x, y)] = ramp[1]
    # contorno
    add = [(x, y) for y in range(N) for x in range(N) if (x, y) not in px and
           any((x+dx, y+dy) in px for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)))]
    for p in add: px[p] = OUTL
    # líneas de movimiento al abrir
    if gap >= 6 and not dim:
        for sgn in (-1, 1):
            for x in range(3, 8): px.setdefault((x, int(12 + sgn*(gap/2 + 6.5))), C('#ffffff'))
    # chispas del impacto
    star = {2: [(0,0),(1,0),(-1,0),(0,1),(0,-1),(2,0),(-2,0),(0,2),(0,-2),(2,2),(-2,-2),(2,-2),(-2,2)],
            1: [(0,0),(1,0),(-1,0),(0,1),(0,-1)]}
    for dx, dy in star.get(sparks, []):
        x, y = int(CX + w + 1.5 + dx), 12 + dy
        if 0 <= x < N and 0 <= y < N: px[(x, y)] = C('#ffffff' if abs(dx)+abs(dy) < 2 else '#fff0a0')
    im = Image.new('RGBA', (N, N), (0, 0, 0, 0))
    for p, c in px.items(): im.putpixel(p, c)
    return im

frames = [bite(7, w=7.5), bite(9), bite(3), bite(0, sparks=2), bite(0, sparks=1, dim=True, w=8.5)]
os.makedirs(f'{OUT}/frames', exist_ok=True); os.makedirs(f'{OUT}/gif', exist_ok=True)
BG = C('#16352e'); S = 8
sheet = Image.new('RGBA', (N*len(frames), N), (0, 0, 0, 0))
prev = Image.new('RGBA', (len(frames)*(N*S+10)+10, N*S+20), BG); gif = []
for i, im in enumerate(frames):
    im.save(f'{OUT}/frames/Mandibles_Bite_{i}.png'); sheet.paste(im, (i*N, 0))
    prev.alpha_composite(im.resize((N*S, N*S), Image.NEAREST), (10 + i*(N*S+10), 10))
    g = Image.new('RGBA', (N, N), BG); g.alpha_composite(im); gif.append(g.resize((N*8, N*8), Image.NEAREST).convert('RGB'))
sheet.save(f'{OUT}/Mandibles_Bite.png'); prev.save(f'{OUT}/vista_previa_mordida.png')
gif[0].save(f'{OUT}/gif/Mandibles_Bite.gif', save_all=True, append_images=gif[1:], duration=[60, 60, 50, 90, 90], loop=0)
