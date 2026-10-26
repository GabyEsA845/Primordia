"""Sprites de enemigos y jefe del piso 1 de Primordia. Uso: python3 enemies.py <carpeta_salida>"""
import math, sys, os
from PIL import Image, ImageDraw

OUT = sys.argv[1]
OUTL = (24, 20, 37, 255)
def C(h): h = h.lstrip('#'); return tuple(int(h[i:i+2], 16) for i in (0, 2, 4)) + (255,)
EYE = '#f4f4e8'

class Canvas:
    def __init__(s, n): s.n = n; s.px = {}
    def put(s, x, y, c):
        x, y = int(math.floor(x)), int(math.floor(y))
        if 0 <= x < s.n and 0 <= y < s.n: s.px[(x, y)] = C(c) if isinstance(c, str) else c
    def put_in(s, x, y, c):
        if (int(math.floor(x)), int(math.floor(y))) in s.px: s.put(x, y, c)
    def outline(s):
        add = [(x, y) for y in range(s.n) for x in range(s.n) if (x, y) not in s.px and any(
            s.px.get((x+dx, y+dy), OUTL) != OUTL for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)))]
        for p in add: s.px[p] = OUTL
    def img(s):
        im = Image.new('RGBA', (s.n, s.n), (0, 0, 0, 0))
        for p, c in s.px.items(): im.putpixel(p, c)
        return im

def shade(cv, fn, cx, cy, r, ramp):
    """Rellena la forma fn(x,y) con 4 tonos, luz arriba a la izquierda."""
    ramp = [C(c) for c in ramp]
    for y in range(cv.n):
        for x in range(cv.n):
            px, py = x + .5, y + .5
            if fn(px, py):
                nx, ny = (px - cx) / r, (py - cy) / r
                v = (-0.6*nx - 0.8*ny) * 0.8 + 0.35 - max(0, math.hypot(nx, ny) - 0.75) * 1.2
                cv.px[(x, y)] = ramp[0 if v < -0.15 else 1 if v < 0.35 else 2 if v < 0.75 else 3]

def ell(cx, cy, rx, ry): return lambda x, y: ((x-cx)/rx)**2 + ((y-cy)/ry)**2 <= 1
def union(*fs): return lambda x, y: any(f(x, y) for f in fs)
WHITE4 = ['#ffffff'] * 4

def droplets(n, cx, cy, r, sz, ramp, core=None, k=8, phase=0.3):
    cv = Canvas(n)
    if core: shade(cv, ell(cx, cy, core, core*0.85), cx, cy, core, ramp)
    for i in range(k):
        a = i * 2*math.pi/k + phase
        gx, gy = cx + math.cos(a)*r, cy + math.sin(a)*r*0.8
        shade(cv, ell(gx, gy, sz, sz), gx, gy, sz, ramp)
    cv.outline(); return cv

def puddle(n, cx, cy, w, ramp):
    cv = Canvas(n); shade(cv, ell(cx, cy, w, 1.6), cx, cy, w, ramp); cv.outline(); return cv

# =============== Bacteria (16x16) ===============
BAC = ['#8a3a2a', '#d0603a', '#f2934e', '#ffd08a']
def bacteria(n=16, squash=0.0, tail=0, pal=BAC, eyes='angry', off=(0, 0), grow=0.0):
    cv = Canvas(n); ox, oy = (n-16)/2 + off[0] + 0.6, (n-16)/2 + off[1]
    hl = 3.1 * (1 - 0.15*squash) + grow*0.5; ry = 3.2 * (1 + 0.12*squash) + grow*0.5
    x0, x1, cy = 6 + ox - squash*0.4, 11.5 + ox + squash*0.4, 8.5 + oy
    cap = lambda x, y: (x0 <= x <= x1 and abs(y-cy) <= ry) or ((x-x0)/hl)**2 + ((y-cy)/ry)**2 <= 1 or ((x-x1)/hl)**2 + ((y-cy)/ry)**2 <= 1
    shade(cv, cap, (x0+x1)/2, cy, 5, pal)
    if pal is BAC:
        for p in ((x0+1, cy+1), (x0+3, cy-2), (x0+4, cy+2)): cv.put_in(*p, '#b44e33')
        cv.put_in(x0-1, cy-2, '#fff2c8')
    # flagelo ondulado detrás
    waves = [[(0,0),(-1,1),(-1,2),(0,3),(0,4)], [(0,0),(-1,0),(-2,1),(-2,2),(-1,3)],
             [(0,0),(-1,-1),(-1,-2),(0,-3),(0,-4)], [(0,0),(-1,0),(-2,-1),(-2,-2),(-1,-3)]]
    fx = x0 - hl
    for dx, dy in waves[tail % 4]: cv.put(fx + dx, cy + dy, pal[1])
    ex = x1
    if eyes == 'angry':
        cv.put(ex, cy-1.5, EYE); cv.put(ex+1, cy-1.5, EYE); cv.put(ex+1, cy-0.5, EYE); cv.put(ex, cy-0.5, OUTL)
        cv.put(ex-1, cy-2.5, OUTL); cv.put(ex, cy-2.5, OUTL)
        cv.put(ex, cy+1.5, OUTL); cv.put(ex+1, cy+1.5, OUTL)
    elif eyes == 'x':
        for dx, dy in ((0,0),(2,0),(1,1),(0,2),(2,2)): cv.put(ex-1+dx, cy-2.5+dy, OUTL)
    cv.outline(); return cv

# =============== Ameba (16x16) y ameba pequeña (10x10) ===============
AME = ['#5a3a8c', '#9a5fd0', '#c995f0', '#f0d8ff']
def ameba(n=16, size=16, frame=0, pal=AME, eyes='open', squash=0.0, bulge=0.0, glow=False, off=(0, 0), grow=0.0, small=False):
    cv = Canvas(n); c = n/2; r = size/2 - 1.8 + grow
    cx, cy = c + off[0], c + off[1]
    rx, ry = r*(1+0.12*squash), r*(1-0.12*squash)
    lob = [[(-.7,.45,.45),(.75,.35,.4),(.2,-.8,.35)], [(-.68,-.2,.45),(.7,.6,.4),(-.1,.85,.35)],
           [(-.75,.2,.42),(.6,-.6,.38),(.35,.8,.38)], [(-.5,-.7,.4),(.8,.0,.4),(-.6,.75,.36)]][frame % 4]
    fs = [ell(cx, cy, rx, ry)] + [ell(cx+dx*r, cy+dy*r, rr*r, rr*r) for dx, dy, rr in lob]
    if bulge: fs.append(ell(cx + r*0.75 + bulge*1.1, cy + 1.5, 1.6 + bulge*0.5, 1.4 + bulge*0.35))
    shade(cv, union(*fs), cx, cy, r+1, pal)
    if pal is AME:
        cv.put_in(cx-2, cy+1, '#7a46b0'); cv.put_in(cx-1, cy+1, '#7a46b0'); cv.put_in(cx-2, cy+2, '#7a46b0')
        cv.put_in(cx-3, cy-3, '#ffffff')
    if glow:  # carga del disparo en el seudópodo
        gx = cx + r*0.75 + bulge*1.1 + 0.5
        for dx, dy in ((0,0),(1,0),(0,1),(1,1)): cv.put_in(gx-1+dx, cy+1+dy, '#ff4f9a')
        cv.put_in(gx-1, cy+1, '#ffd0e0')
    if small:
        if eyes == 'x': cv.put(cx, cy-1, OUTL); cv.put(cx+1, cy, OUTL); cv.put(cx+1, cy-1, OUTL); cv.put(cx, cy, OUTL)
        elif eyes != 'none': cv.put(cx, cy-1, EYE); cv.put(cx+1, cy-1, EYE); cv.put(cx+1, cy-1, OUTL) if False else cv.put(cx+1, cy, OUTL); cv.put(cx, cy, EYE)
    elif eyes == 'x':
        for ex in (cx-2, cx+2):
            for dx, dy in ((0,0),(2,0),(1,1),(0,2),(2,2)): cv.put(ex-1+dx, cy-3+dy, OUTL)
    elif eyes != 'none':
        for ex in (cx-1, cx+2):
            for dx in range(2):
                for dy in range(3): cv.put(ex+dx, cy-3+dy, EYE)
            if eyes == 'open': cv.put(ex+1, cy-2, OUTL); cv.put(ex+1, cy-1, OUTL)
            elif eyes == 'squint': cv.put(ex, cy-3, OUTL); cv.put(ex+1, cy-3, OUTL); cv.put(ex+1, cy-2, OUTL); cv.put(ex+1, cy-1, OUTL)
    cv.outline(); return cv

def ameba_split():
    n = 24; fr = []
    fr.append(ameba(n, 16, 0, pal=WHITE4, eyes='squint'))
    fr.append(ameba(n, 16, 1, eyes='squint', squash=1.2))
    # se alarga y se estrangula por la cintura
    for sep, rr in ((2.5, 5.2), (4, 4.6), (5.5, 4.2)):
        cv = Canvas(n); c = 12
        f = union(ell(c-sep, c, rr, rr*0.95), ell(c+sep, c, rr, rr*0.95))
        shade(cv, f, c, c, 7, AME)
        for sx in (c-sep, c+sep):
            cv.put(sx-1, c-2, EYE); cv.put(sx, c-2, EYE); cv.put(sx, c-1, OUTL); cv.put(sx-1, c-1, EYE)
        if sep < 5:  # cuello que se rompe
            for dy in (-1, 0): cv.put(c, c+dy, AME[0])
        cv.outline(); fr.append(cv)
    return fr

# =============== Virus gigante (48x48; muerte 64x64) ===============
VIR1 = dict(body=['#7a2a5a', '#c8487e', '#ff7aa8', '#ffd0e0'], stalk='#8a8aa8', knob=['#6a7a20', '#a8c040', '#d8f070', '#f8ffc0'], facet='#a83a6a', iris=None)
VIR2 = dict(body=['#5a1030', '#a8203c', '#ff4060', '#ffb0b8'], stalk='#a07080', knob=['#7a2010', '#d05020', '#ff9040', '#ffe0a0'], facet='#801830', iris='#ff3030')
VIRW = dict(body=WHITE4, stalk='#ffffff', knob=WHITE4, facet='#ffffff', iris=None)

def virus(n=48, pal=VIR1, spin=0.0, spike=18.5, knob=2.2, scale=1.0, eye='open', look=(0, 0), mouth='closed',
          cracks=0, glow=False, shake=(0, 0), angry=False, lose=()):
    cv = Canvas(n); c = n/2 + shake[0]; cyy = n/2 + shake[1]
    R = 13.5 * scale
    # espículas
    for k in range(8):
        if k in lose: continue
        a = k*math.pi/4 + spin
        L = spike * scale
        for t in range(int(R*0.9), int(L)):
            cv.put(c + math.cos(a)*t, cyy + math.sin(a)*t, pal['stalk'])
        hx, hy = c + math.cos(a)*L, cyy + math.sin(a)*L
        shade(cv, ell(hx, hy, knob, knob), hx, hy, knob, pal['knob'] if not glow else ['#ff9040', '#ffd060', '#fff0a0', '#ffffff'])
    # cápside hexagonal
    def hexa(x, y):
        dx, dy = abs(x-c), abs(y-cyy)
        return dy <= R*0.88 and dx*0.88 + dy*0.5 <= R*0.88
    shade(cv, hexa, c, cyy, R, pal['body'])
    # facetas
    for t in range(-int(R), int(R)+1):
        if abs(t) > R*0.62: cv.put_in(c+t, cyy, pal['facet'])
    for t in range(3, int(R*0.8)):
        cv.put_in(c - t*0.5, cyy - R*0.88 + t*0.0 + 1, None or pal['facet']) if False else None
    # brillo
    for dx, dy, col in ((-9, -8, '#ffffff'), (-8, -8, pal['body'][3]), (-9, -7, pal['body'][3])):
        cv.put_in(c + dx*scale, cyy + dy*scale, col)
    # grietas (fase 2 / muerte)
    crack_paths = [[(-8,-3),(-7,-2),(-6,-2),(-5,-1)], [(6,-6),(5,-5),(5,-4),(4,-3)], [(7,4),(6,5),(5,5),(4,6)], [(-6,6),(-5,5),(-5,4)]]
    for p in crack_paths[:cracks]:
        for dx, dy in p: cv.put_in(c + dx*scale, cyy + dy*scale, OUTL)
    # ojo
    ew, eh = 6*scale, 4.6*scale
    if eye in ('open', 'wide'):
        eh2 = eh * (1.15 if eye == 'wide' else 1)
        for yy in range(int(cyy-eh2-1), int(cyy+eh2+1)):
            for xx in range(int(c-ew-1), int(c+ew+1)):
                if ((xx+.5-c)/ew)**2 + ((yy+.5-cyy+1)/eh2)**2 <= 1: cv.put(xx, yy, EYE)
        pr = 2*scale*(0.7 if eye == 'wide' else 1)
        px, py = c + look[0]*2, cyy - 1 + look[1]
        for yy in range(int(py-3*scale-1), int(py+3*scale+1)):
            for xx in range(int(px-pr-1), int(px+pr+1)):
                if ((xx+.5-px)/pr)**2 + ((yy+.5-py)/(3*scale*(0.7 if eye=='wide' else 1)))**2 <= 1:
                    cv.put(xx, yy, pal['iris'] if pal['iris'] and abs(xx+.5-px) > pr*0.45 else '#1b1b2b')
        cv.put(c-3*scale, cyy-3*scale, '#ffffff')
        if angry:  # ceja en V
            for i in range(int(ew)+1):
                cv.put(c-ew+i, cyy-eh-2+i*0.35, OUTL); cv.put(c+ew-i, cyy-eh-2+i*0.35, OUTL)
    elif eye == 'closed':
        for xx in range(int(c-ew), int(c+ew)): cv.put(xx, cyy-1 + (1 if abs(xx-c) < ew*0.6 else 0), OUTL)
    elif eye == 'x':
        for i in range(-3, 4): cv.put(c+i, cyy-1+i, OUTL); cv.put(c+i, cyy-1-i, OUTL)
    # boca
    my = cyy + 7*scale
    if mouth == 'closed':
        for xx in range(int(c-5*scale), int(c+5*scale)+1): cv.put(xx, my, OUTL)
        for xx in range(int(c-4*scale), int(c+5*scale), 2): cv.put(xx, my+1, EYE)
    else:
        h = {'open': 2, 'wide': 4}[mouth]
        for yy in range(int(my-1), int(my+h)):
            for xx in range(int(c-5*scale), int(c+5*scale)+1): cv.put(xx, yy, '#3a0a20')
        for xx in range(int(c-4*scale), int(c+5*scale), 2): cv.put(xx, my-1, EYE); cv.put(xx, my+h-1, EYE)
    cv.outline(); return cv

def boss_death():
    n = 64; fr = []
    fr.append(virus(n, VIRW, eye='wide', mouth='wide'))
    fr.append(virus(n, VIR2, eye='x', mouth='wide', cracks=4, shake=(1, 0), scale=1.05))
    fr.append(virus(n, VIRW, eye='x', mouth='wide', cracks=4, shake=(-1, 0), scale=1.1))
    fr.append(virus(n, VIR2, eye='x', mouth='wide', cracks=4, scale=1.18, spike=22, lose=(1, 4, 6)))
    fr.append(virus(n, VIRW, eye='x', mouth='wide', cracks=4, scale=1.25, spike=22, lose=(0, 1, 3, 4, 6, 7)))
    for r, sz in ((14, 4), (20, 3), (25, 2), (28, 1.2)):
        fr.append(droplets(n, 32, 32, r, sz, VIR2['body'], core=6 if r == 14 else None, k=10))
    fr.append(puddle(n, 32, 44, 12, ['#5a1030', '#a8203c', '#ff7aa8', '#ffd0e0']))
    return fr

def enemy_death(n, body_fn, ramp):
    return [body_fn(pal=WHITE4), body_fn(pal=ramp, grow=1, eyes='x'),
            droplets(n, n/2, n/2+1, n*0.28, 1.8, ramp, core=2.5, k=7),
            droplets(n, n/2, n/2+1, n*0.4, 1.2, ramp, k=7),
            puddle(n, n/2, n/2+5, n*0.25, ramp)]

anims = {
 'Bacteria': {
   'Move': [bacteria(tail=i, squash=s) for i, s in zip(range(4), (0.6, 0, -0.6, 0))],
   'Hurt': [bacteria(pal=WHITE4), bacteria(pal=['#a33b3b', '#e05a5a', '#ff9a8a', '#fff0e0'])],
   'Death': enemy_death(24, lambda pal, grow=0, eyes='angry': bacteria(24, pal=pal, grow=grow, eyes=eyes), BAC),
 },
 'Ameba': {
   'Move': [ameba(frame=i, squash=s) for i, s in zip(range(4), (0.5, 0, -0.5, 0))],
   'Shoot': [ameba(n=24, frame=0, squash=0.8, eyes='squint', bulge=0.8, glow=True), ameba(n=24, frame=0, squash=1.2, eyes='squint', bulge=1.8, glow=True),
             ameba(n=24, frame=1, squash=-0.8, bulge=2.6), ameba(n=24, frame=1, squash=-0.3, bulge=0.8)],
   'Hurt': [ameba(pal=WHITE4), ameba(pal=['#a33b3b', '#e05a5a', '#ff9a8a', '#fff0e0'])],
   'Split': ameba_split(),
   'Death': enemy_death(24, lambda pal, grow=0, eyes='open': ameba(24, pal=pal, grow=grow, eyes=eyes), AME),
 },
 'AmebaSmall': {
   'Move': [ameba(10, 10, i, small=True, squash=s) for i, s in zip(range(4), (0.5, 0, -0.5, 0))],
   'Hurt': [ameba(10, 10, small=True, pal=WHITE4), ameba(10, 10, small=True, pal=['#a33b3b', '#e05a5a', '#ff9a8a', '#fff0e0'])],
   'Death': [ameba(16, 10, small=True, pal=WHITE4), ameba(16, 10, small=True, eyes='x', grow=0.8),
             droplets(16, 8, 9, 4, 1.2, AME, core=1.6, k=6), droplets(16, 8, 9, 6, 0.9, AME, k=6), puddle(16, 8, 12, 3.5, AME)],
 },
 'BossVirus': {
   # Fase 1: gira despacio mientras dispara la espiral (bucle de 4 cuadros, giro de 11,25° por cuadro)
   'Idle_P1': [virus(spin=i*math.pi/16, spike=18.5 + (i % 2)) for i in range(4)],
   # Ráfaga circular: se contrae, las espículas brillan y se expande al disparar
   'Burst_P1': [virus(scale=0.94, spike=18, glow=True, eye='closed'), virus(scale=0.9, spike=17, glow=True, eye='closed', shake=(1, 0)),
                virus(scale=1.03, spike=19, knob=2.6, glow=True, eye='wide', mouth='wide'), virus(scale=1.02, spike=19.5, mouth='open'), virus()],
   # Cambio a fase 2 al 50 % de vida: tiembla, se agrieta y se pone rojo
   'Phase2_Transition': [virus(pal=VIRW, eye='wide', shake=(1, 0)), virus(eye='wide', cracks=1, shake=(-1, 0)),
                         virus(pal=VIR2, eye='wide', cracks=2, shake=(1, 0), mouth='open'), virus(pal=VIR2, eye='open', angry=True, cracks=3, mouth='wide', scale=1.05)],
   'Idle_P2': [virus(pal=VIR2, spin=i*math.pi/12, angry=True, cracks=3, spike=18.5 + (i % 2)) for i in range(4)],
   'Burst_P2': [virus(pal=VIR2, angry=True, cracks=3, scale=0.92, spike=18, glow=True, eye='closed'),
                virus(pal=VIR2, angry=True, cracks=3, scale=0.9, spike=17, glow=True, eye='closed', shake=(-1, 0)),
                virus(pal=VIR2, angry=True, cracks=3, scale=1.03, spike=19, knob=2.6, glow=True, eye='wide', mouth='wide'),
                virus(pal=VIR2, angry=True, cracks=3, spike=19.5, mouth='open'), virus(pal=VIR2, angry=True, cracks=3)],
   # Invocación de bacterias (solo fase 2): abre la boca y las escupe
   'Summon_P2': [virus(pal=VIR2, angry=True, cracks=3, mouth='open', eye='open', look=(0, 1)), virus(pal=VIR2, angry=True, cracks=3, mouth='wide', scale=1.04, look=(0, 1)),
                 virus(pal=VIR2, angry=True, cracks=3, mouth='wide', scale=0.98, shake=(0, 1)), virus(pal=VIR2, angry=True, cracks=3, mouth='open')],
   'Hurt_P1': [virus(pal=VIRW, eye='wide'), virus(pal=dict(VIR1, body=['#a33b3b', '#e05a5a', '#ff9a8a', '#fff0e0']), eye='wide')],
   'Hurt_P2': [virus(pal=VIRW, eye='wide', cracks=3), virus(pal=dict(VIR2, body=['#a33b3b', '#e05a5a', '#ff9a8a', '#fff0e0']), eye='wide', cracks=3)],
   'Death': boss_death(),
 },
}

# --------- guardar hojas, cuadros sueltos, vistas previas y GIF ---------
BG = C('#16352e')
MS = {'Move': 120, 'Shoot': 110, 'Hurt': 90, 'Death': 110, 'Split': 120, 'Idle_P1': 120, 'Idle_P2': 90, 'Burst_P1': 100, 'Burst_P2': 90,
      'Phase2_Transition': 150, 'Summon_P2': 130, 'Hurt_P1': 90, 'Hurt_P2': 90}
for enemy, an in anims.items():
    base = f'{OUT}/{enemy}'; os.makedirs(f'{base}/frames', exist_ok=True); os.makedirs(f'{base}/gif', exist_ok=True)
    cell = max(cv.n for fr in an.values() for cv in fr)
    S = 6 if cell <= 24 else 3
    cols = max(len(fr) for fr in an.values())
    W, H = 170 + cols*(cell*S+8), len(an)*(cell*S+8) + 12
    prev = Image.new('RGBA', (W, H), BG); d = ImageDraw.Draw(prev)
    for r, (name, frames) in enumerate(an.items()):
        n = frames[0].n
        sheet = Image.new('RGBA', (n*len(frames), n), (0, 0, 0, 0))
        gif = []
        for i, cv in enumerate(frames):
            im = cv.img(); im.save(f'{base}/frames/{enemy}_{name}_{i}.png'); sheet.paste(im, (i*n, 0))
            big = im.resize((im.width*S, im.height*S), Image.NEAREST)
            y0 = 8 + r*(cell*S+8)
            prev.alpha_composite(big, (170 + i*(cell*S+8) + (cell*S - big.width)//2, y0 + (cell*S - big.height)//2))
            g = Image.new('RGBA', (cell, cell), BG); g.alpha_composite(im, ((cell-n)//2, (cell-n)//2))
            gif.append(g.resize((cell*(192//cell), cell*(192//cell)), Image.NEAREST).convert('RGB'))
        d.text((10, 8 + r*(cell*S+8) + cell*S//2 - 6), name.replace('_', ' '), fill=C('#d6ff9a'))
        sheet.save(f'{base}/{enemy}_{name}.png')
        gif[0].save(f'{base}/gif/{name}.gif', save_all=True, append_images=gif[1:], duration=MS[name], loop=0)
    prev.save(f'{base}/vista_previa_{enemy}.png')
