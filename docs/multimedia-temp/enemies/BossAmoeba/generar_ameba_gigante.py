"""Ameba gigante, segundo jefe del piso 1 de Primordia: hojas de animación, proyectiles e icono del candado.

Uso: python generar_ameba_gigante.py            (escribe junto a este script)

Salida (rutas relativas a docs/multimedia-temp):
  enemies/BossAmoeba/BossAmoeba_<Clip>.png          cuerpo grande, cuadros de 112x112
  enemies/BossAmoebaHalf/BossAmoebaHalf_<Clip>.png  ameba mediana (60 %), cuadros de 64x64
  enemies/BossAmoeba/projectiles/Projectile_Amoeba*.png  proyectiles de la lluvia final
  ui/BossBar/Icon_Lock.png                          candado de la barra del jefe (inmune)
  enemies/BossAmoeba/vista_previa_BossAmoeba.png    todas las animaciones, ampliadas
  enemies/BossAmoeba/gif/*.gif                      cada animación en movimiento
"""
import math, os, random
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
TEMP = os.path.normpath(os.path.join(HERE, '..', '..'))
OUT_BIG = HERE
OUT_HALF = os.path.join(TEMP, 'enemies', 'BossAmoebaHalf')
OUT_PROJ = os.path.join(HERE, 'projectiles')
OUT_UI = os.path.join(TEMP, 'ui', 'BossBar')

OUTL = (24, 20, 37, 255)
def C(h, a=255):
    h = h.lstrip('#'); return tuple(int(h[i:i+2], 16) for i in (0, 2, 4)) + (a,)
EYE = '#fbf6f0'
PUPIL = '#2a1830'

# Rampa de 4 tonos, de oscuro a claro. Rosa pálido en calma; más encendido al enfadarse; casi rojo en la furia.
CALM = ['#b0627f', '#dc93ae', '#f4bfd0', '#fde8ef']
ANGRY = ['#a83f62', '#d96a8c', '#f19bb3', '#fdd2de']
RAGE = ['#8c2440', '#c84060', '#ec7088', '#fbb4c0']
TIRED = ['#95808a', '#c2aab4', '#e0cfd6', '#f4ecef']
WHITE = ['#ffffff'] * 4
NUCLEUS = {'calm': '#c9759a', 'angry': '#b8506f', 'rage': '#a0304a', 'tired': '#a8929c'}

class Canvas:
    def __init__(s, n): s.n = n; s.px = {}
    def put(s, x, y, c):
        x, y = int(math.floor(x)), int(math.floor(y))
        if 0 <= x < s.n and 0 <= y < s.n: s.px[(x, y)] = C(c) if isinstance(c, str) else c
    def put_in(s, x, y, c):
        if (int(math.floor(x)), int(math.floor(y))) in s.px: s.put(x, y, c)
    def outline(s):
        add = [(x, y) for y in range(s.n) for x in range(s.n) if (x, y) not in s.px and any(
            s.px.get((x+dx, y+dy), OUTL) != OUTL for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
        for p in add: s.px[p] = OUTL
    def img(s):
        im = Image.new('RGBA', (s.n, s.n), (0, 0, 0, 0))
        for p, c in s.px.items(): im.putpixel(p, c)
        return im

def shade(cv, fn, cx, cy, r, ramp):
    """Rellena la forma con 4 tonos, luz arriba a la izquierda (como el resto de enemigos)."""
    ramp = [C(c) if isinstance(c, str) else c for c in ramp]
    for y in range(cv.n):
        for x in range(cv.n):
            px, py = x + .5, y + .5
            if fn(px, py):
                nx, ny = (px - cx) / r, (py - cy) / r
                v = (-0.6*nx - 0.8*ny) * 0.8 + 0.35 - max(0, math.hypot(nx, ny) - 0.75) * 1.2
                cv.px[(x, y)] = ramp[0 if v < -0.15 else 1 if v < 0.35 else 2 if v < 0.75 else 3]

def ell(cx, cy, rx, ry): return lambda x, y: ((x-cx)/rx)**2 + ((y-cy)/ry)**2 <= 1
def union(*fs): return lambda x, y: any(f(x, y) for f in fs)

def blob_shape(cx, cy, r, t, squash=0.0, lobes=6, amp=0.16, seed=0.0):
    """Cuerpo de ameba: elipse con seudópodos que crecen y menguan con t (0..1, en bucle)."""
    rx, ry = r * (1 + 0.1*squash), r * (1 - 0.1*squash)
    fs = [ell(cx, cy, rx, ry)]
    for i in range(lobes):
        a = i * 2*math.pi/lobes + seed + 0.35*math.sin(2*math.pi*t + i)
        ext = 0.72 + amp * math.sin(2*math.pi*t + i*1.7 + seed)
        lr = r * (0.36 + 0.05*math.sin(2*math.pi*t*2 + i))
        fs.append(ell(cx + math.cos(a)*rx*ext, cy + math.sin(a)*ry*ext, lr, lr*0.9))
    return union(*fs)

def details(cv, cx, cy, r, mood, t):
    """Núcleo y vacuolas dentro del citoplasma."""
    nuc = NUCLEUS.get(mood, NUCLEUS['calm'])
    nx, ny = cx + r*0.28, cy + r*0.25
    for y in range(cv.n):
        for x in range(cv.n):
            if ((x+.5-nx)/(r*0.24))**2 + ((y+.5-ny)/(r*0.2))**2 <= 1: cv.put_in(x, y, nuc)
    cv.put_in(nx - r*0.08, ny - r*0.06, '#ffffff' if mood != 'tired' else '#f0e6ea')
    rnd = random.Random(7)
    for i in range(5):
        a = rnd.uniform(0, 2*math.pi) + 0.2*math.sin(2*math.pi*t + i)
        d = rnd.uniform(0.35, 0.62) * r
        vx, vy = cx + math.cos(a)*d, cy + math.sin(a)*d*0.85
        if abs(vx - nx) < r*0.3 and abs(vy - ny) < r*0.28: continue
        for dx, dy in ((0, 0), (1, 0), (0, 1), (1, 1)):
            cv.put_in(vx+dx, vy+dy, CALM[3] if mood != 'tired' else TIRED[3])
        cv.put_in(vx, vy, '#ffffff')
    # brillo
    for dx in range(3): cv.put_in(cx - r*0.55 + dx, cy - r*0.62, '#ffffff')
    cv.put_in(cx - r*0.6, cy - r*0.55, '#ffffff')

def eyes(cv, cx, cy, s, kind, look=(0, 1)):
    """Ojos de 6x7 px (escalados por s): calm, angry, rage, tired, shut, x, wide."""
    w, h = max(2, round(6*s)), max(3, round(7*s))
    for side in (-1, 1):
        ex = cx + side*round(8*s) - w/2
        ey = cy - round(9*s)
        if kind == 'x':
            for i in range(w):
                cv.put(ex + i, ey + i*h/w, OUTL); cv.put(ex + w-1-i, ey + i*h/w, OUTL)
            continue
        if kind == 'shut':
            for i in range(w): cv.put(ex + i, ey + h/2 + (1 if i in (0, w-1) else 0), OUTL)
            continue
        if kind == 'tired':
            for i in range(w): cv.put(ex + i, ey + h*0.55 + abs(i - (w-1)/2)*0.35, OUTL)
            for i in range(1, w-1): cv.put(ex + i, ey + h*0.55 + 1, EYE)
            cv.put(ex + w/2 + look[0], ey + h*0.55 + 1, PUPIL)
            continue
        for y in range(h):
            for x in range(w):
                if ((x+.5-w/2)/(w/2))**2 + ((y+.5-h/2)/(h/2))**2 <= 1.05: cv.put(ex+x, ey+y, EYE)
        pw = max(1, round(2*s)); ph = max(2, round(3*s))
        px = ex + w/2 - pw/2 + look[0]*max(1, round(s)); py = ey + h/2 - ph/2 + look[1]*max(1, round(s))
        pupil = '#c02040' if kind == 'rage' else PUPIL
        for y in range(ph):
            for x in range(pw): cv.put(px+x, py+y, pupil)
        if kind == 'calm':
            # párpado superior caído: mirada tranquila
            for x in range(w): cv.put(ex+x, ey, OUTL); cv.put(ex+x, ey+1, OUTL if 0 < x < w-1 else EYE)
        elif kind in ('angry', 'rage'):
            # ceño: diagonal hacia el centro
            thick = 2 if kind == 'rage' else 1
            for x in range(w + 1):
                yy = ey - 1 + (x if side < 0 else (w - x)) * 0.55
                for k in range(thick): cv.put(ex + x - (1 if side < 0 else 0), yy + k, OUTL)

def mouth(cv, cx, cy, s, kind, t=0.0):
    my = cy + round(3*s)
    if kind == 'smile':
        for i in range(-2, 3): cv.put(cx + i, my + (1 if abs(i) < 2 else 0), OUTL)
    elif kind == 'frown':
        for i in range(-3, 4): cv.put(cx + i, my + (0 if abs(i) < 2 else 1), OUTL)
    elif kind in ('open', 'roar', 'pant'):
        w = {'open': 4, 'roar': 7, 'pant': 3}[kind] * s
        h = {'open': 3, 'roar': 5, 'pant': 3}[kind] * s * (1 + (0.25*math.sin(2*math.pi*t) if kind != 'open' else 0))
        for y in range(int(-h), int(h) + 1):
            for x in range(int(-w), int(w) + 1):
                if (x/max(w, 1))**2 + (y/max(h, 1))**2 <= 1: cv.put(cx + x, my + 2*s + y, '#5a1430')
        if kind == 'roar':
            for x in range(int(-w)+1, int(w), 2): cv.put(cx + x, my + 2*s - h + 1, EYE)  # dientecillos
        if kind == 'pant':
            for x in range(-1, 2): cv.put(cx + x, my + 2*s + h - 1, '#e0708e')     # lengua

def draw_amoeba(n=112, r=30, t=0.0, pal=CALM, mood='calm', eye='calm', mouth_kind='smile', squash=0.0, off=(0, 0),
                look=(0, 1), veins=0, glow=0.0, lobes=6, amp=0.16, nucleus=True, sweat=False, sc=1.0, face=(0, 0)):
    cv = Canvas(n)
    cx, cy = n/2 + off[0], n/2 + off[1] + 2
    shade(cv, blob_shape(cx, cy, r, t, squash, lobes, amp), cx, cy, r + 2, pal)
    if nucleus and pal is not WHITE: details(cv, cx, cy, r, mood, t)
    if glow > 0:   # núcleo que brilla al disparar
        gr = r * 0.3 * glow
        for y in range(n):
            for x in range(n):
                d = math.hypot(x+.5-cx, y+.5-(cy+r*0.3))
                if d <= gr: cv.put_in(x, y, '#fff4d0' if d < gr*0.45 else '#ff7aa0')
    for k in range(veins):   # venas de furia
        a = k * 2.399 + 0.6
        x0, y0 = cx + math.cos(a)*r*0.55, cy + math.sin(a)*r*0.5
        for i in range(6):
            cv.put_in(x0 + math.cos(a + 0.6*math.sin(i))*i, y0 + math.sin(a)*i*0.6, '#7a1430')
    s = sc * r / 30 * 1.25
    # La cara se desplaza hacia donde mira (aviso antes de embestir).
    eyes(cv, cx + face[0], cy + face[1], s, eye, look)
    mouth(cv, cx + face[0], cy + face[1], s, mouth_kind, t)
    if sweat:
        sx, sy = cx + r*0.75, cy - r*0.55 + (t*4) % 4
        for dx, dy in ((0, 0), (0, 1), (-1, 1), (1, 1), (0, 2)): cv.put(sx+dx, sy+dy, '#bfe6ff')
    cv.outline()
    return cv

def lerp_pal(a, b, k):
    out = []
    for ca, cb in zip(a, b):
        ca, cb = C(ca), C(cb)
        out.append('#%02x%02x%02x' % tuple(round(ca[i] + (cb[i]-ca[i])*k) for i in range(3)))
    return out

def two_blobs(n, sep, rr, t, pal, eye, mouth_kind, waist=True, mood='angry', sc=0.6, squash=0.0, glue=0.0):
    """Dos amebas a ±sep del centro (división y fusión). glue>0 las une con un puente de citoplasma."""
    cv = Canvas(n); c = n/2
    fs = [blob_shape(c - sep, c + 2, rr, t, squash, 5, 0.14, 0.4), blob_shape(c + sep, c + 2, rr, t, squash, 5, 0.14, 2.1)]
    if glue > 0: fs.append(ell(c, c + 2, sep, rr * glue))
    shade(cv, union(*fs), c, c + 2, sep + rr, pal)
    for sx in (c - sep, c + sep):
        if pal is not WHITE: details(cv, sx, c + 2, rr, mood, t)
        eyes(cv, sx, c + 2, sc * rr / 30 * 1.25 / sc, eye)
        mouth(cv, sx, c + 2, rr / 30 * 1.25, mouth_kind, t)
    cv.outline(); return cv

def droplets(n, cx, cy, dist, size, pal, k=12, phase=0.2, fade=False):
    cv = Canvas(n)
    for i in range(k):
        a = i * 2*math.pi/k + phase + (0.2 if i % 2 else 0)
        d = dist * (0.85 + 0.25*((i*37) % 7)/7)
        gx, gy = cx + math.cos(a)*d, cy + math.sin(a)*d*0.85
        sz = size * (0.7 + 0.5*((i*13) % 5)/5)
        if fade and i % 3 == 0: continue
        shade(cv, ell(gx, gy, sz, sz), gx, gy, sz, pal)
    cv.outline(); return cv

# ---------------- animaciones del cuerpo grande ----------------
N = 112
def aim_face(k, mag):
    a = k * math.pi / 4
    return (round(math.cos(a) * mag), round(-math.sin(a) * mag))

def aim_look(k, mag):
    """Desplazamiento de las pupilas hacia el sector k (0 = derecha, 2 = arriba; en la imagen y crece hacia abajo)."""
    a = k * math.pi / 4
    return (round(math.cos(a) * mag), round(-math.sin(a) * mag))

def big():
    A = {}
    # Aviso antes de embestir: mira hacia donde va a salir (8 direcciones), temblando un poco.
    for k in range(8):
        A[f'AimCalm_{k}'] = [draw_amoeba(t=i/2, eye='wide', mouth_kind='open', look=aim_look(k, 2), face=aim_face(k, 6), off=((1 if i else 0), 0))
                             for i in range(2)]
        A[f'Aim_{k}'] = [draw_amoeba(t=i/2, pal=ANGRY, mood='angry', eye='angry', mouth_kind='frown', amp=0.2,
                                     look=aim_look(k, 2), face=aim_face(k, 6), off=((1 if i else 0), 0)) for i in range(2)]
    A['Idle'] = [draw_amoeba(t=i/6, look=((i//2) % 3 - 1, 1)) for i in range(6)]
    A['Angry'] = [draw_amoeba(t=i/6, pal=ANGRY, mood='angry', eye='angry', mouth_kind='frown', amp=0.2, look=(0, 1)) for i in range(6)]
    tr = []
    for i in range(8):
        k = min(1, i/5)
        shake = (1 if i % 2 else -1) if 1 <= i <= 5 else 0
        pal = lerp_pal(CALM, ANGRY, k)
        eye = 'calm' if i < 2 else 'wide' if i < 4 else 'angry'
        tr.append(draw_amoeba(t=i/8, pal=pal, mood='angry', eye=eye, mouth_kind='open' if 2 <= i < 4 else 'frown',
                              r=30 + (2 if 3 <= i <= 5 else 0), squash=0.6 if i == 5 else 0, off=(shake, 0)))
    A['Angry_Transition'] = tr
    A['Hurt_Calm'] = [draw_amoeba(pal=WHITE, eye='shut', mouth_kind='frown', nucleus=False),
                      draw_amoeba(pal=lerp_pal(CALM, ['#e05060'] * 4, 0.45), eye='shut', mouth_kind='frown')]
    A['Hurt_Angry'] = [draw_amoeba(pal=WHITE, eye='shut', mouth_kind='frown', nucleus=False),
                       draw_amoeba(pal=lerp_pal(ANGRY, ['#e03040'] * 4, 0.45), mood='angry', eye='shut', mouth_kind='frown')]
    # División: se estira, se estrangula por la cintura y quedan dos medianas a ±22 px (1,4 unidades).
    sp = []
    sp.append(draw_amoeba(pal=WHITE, eye='shut', mouth_kind='frown', nucleus=False))
    sp.append(draw_amoeba(pal=ANGRY, mood='angry', eye='angry', mouth_kind='roar', squash=1.0))
    for sep, rr, glue in ((6, 27, 0.9), (10, 25, 0.75), (14, 23, 0.55), (17, 22, 0.35), (19, 21, 0.18)):
        sp.append(two_blobs(N, sep, rr, 0.2, ANGRY, 'angry', 'frown', glue=glue))
    for sep in (21, 22, 22):
        sp.append(two_blobs(N, sep, 18, 0.1*sep, ANGRY, 'angry', 'frown'))
    A['Split'] = sp
    # Fusión: las medianas llegan tocándose (a ±17 px) y se funden en la grande, que vibra.
    mg = []
    for sep, rr, glue in ((17, 18, 0.0), (15, 19, 0.3), (12, 22, 0.6), (8, 25, 0.85), (4, 28, 1.0)):
        mg.append(two_blobs(N, sep, rr, 0.3, ANGRY, 'angry', 'frown', glue=glue))
    mg.append(draw_amoeba(pal=WHITE, eye='shut', mouth_kind='frown', nucleus=False, r=32))
    mg.append(draw_amoeba(pal=ANGRY, mood='angry', eye='angry', mouth_kind='frown', r=31, squash=-0.6))
    mg.append(draw_amoeba(pal=ANGRY, mood='angry', eye='angry', mouth_kind='frown', r=30, squash=0.4))
    A['Merge'] = mg
    A['Rage'] = [draw_amoeba(t=i/8, pal=RAGE, mood='rage', eye='rage', mouth_kind='roar', r=30 + (2 if i % 2 else 0),
                             veins=4 + i % 3, amp=0.24, off=((1 if i % 2 else -1), 0)) for i in range(8)]
    A['Cast_1'] = [draw_amoeba(t=i/6, pal=ANGRY, mood='angry', eye='angry', mouth_kind='open', glow=0.6 + 0.3*math.sin(i)) for i in range(6)]
    A['Cast_2'] = [draw_amoeba(t=i/6, pal=lerp_pal(ANGRY, RAGE, 0.5), mood='rage', eye='rage', mouth_kind='roar',
                               glow=0.8 + 0.3*math.sin(i*1.3), veins=3, amp=0.22) for i in range(6)]
    A['Cast_3'] = [draw_amoeba(t=i/6, pal=RAGE, mood='rage', eye='rage', mouth_kind='roar', glow=1.0 + 0.25*math.sin(i*2),
                               veins=6, amp=0.28, r=31 + (i % 2), off=((1 if i % 2 else -1), (1 if i % 3 == 0 else 0))) for i in range(6)]
    ex = []
    for i in range(8):
        k = i/7
        ex.append(draw_amoeba(t=i/8, pal=lerp_pal(RAGE, TIRED, k), mood='tired', eye='rage' if i < 2 else 'tired',
                              mouth_kind='roar' if i < 2 else 'pant', r=round(30 - 3*k), squash=-0.8*k, amp=0.16*(1-k)+0.05))
    A['Exhaust'] = ex
    A['Tired'] = [draw_amoeba(t=i/6, pal=TIRED, mood='tired', eye='tired', mouth_kind='pant', r=27,
                              squash=-0.8 + 0.25*math.sin(2*math.pi*i/6), amp=0.05, sweat=True) for i in range(6)]
    A['Hurt_Tired'] = [draw_amoeba(pal=WHITE, eye='shut', mouth_kind='pant', nucleus=False, r=27, squash=-0.8),
                       draw_amoeba(pal=lerp_pal(TIRED, ['#e05060'] * 4, 0.45), mood='tired', eye='shut', mouth_kind='pant', r=27, squash=-0.8)]
    de = []
    de.append(draw_amoeba(pal=WHITE, eye='x', mouth_kind='open', nucleus=False, r=27, squash=-0.8))
    de.append(draw_amoeba(pal=TIRED, mood='tired', eye='x', mouth_kind='open', r=29, squash=0.4))
    de.append(draw_amoeba(pal=WHITE, eye='x', mouth_kind='open', nucleus=False, r=32, squash=-0.3, amp=0.3))
    de.append(draw_amoeba(pal=CALM, mood='calm', eye='x', mouth_kind='open', r=34, amp=0.35))
    for i, d in enumerate((14, 22, 30, 38, 44, 48)):
        de.append(droplets(N, N/2, N/2 + 2, d, 6 - i*0.7, CALM, k=12, phase=0.1*i, fade=i >= 4))
    # charco final
    cv = Canvas(N); shade(cv, ell(N/2, N/2 + 14, 16, 3), N/2, N/2 + 14, 16, CALM); cv.outline(); de.append(cv)
    A['Death'] = de
    return A

def half():
    H = 64; A = {}
    A['Move'] = [draw_amoeba(n=H, r=18, t=i/6, pal=ANGRY, mood='angry', eye='angry', mouth_kind='frown', amp=0.22, lobes=5, sc=1.0)
                 for i in range(6)]
    for k in range(8):
        A[f'Aim_{k}'] = [draw_amoeba(n=H, r=18, t=i/2, pal=ANGRY, mood='angry', eye='angry', mouth_kind='frown', lobes=5,
                                     look=aim_look(k, 1), face=aim_face(k, 4), off=((1 if i else 0), 0)) for i in range(2)]
    A['Hurt'] = [draw_amoeba(n=H, r=18, pal=WHITE, eye='shut', mouth_kind='frown', nucleus=False, lobes=5),
                 draw_amoeba(n=H, r=18, pal=lerp_pal(ANGRY, ['#e03040'] * 4, 0.45), mood='angry', eye='shut', mouth_kind='frown', lobes=5)]
    return A

# ---------------- proyectiles ----------------
def projectile(n, r, pal, core, frames=4):
    out = []
    for i in range(frames):
        cv = Canvas(n); c = n/2
        sq = 0.12 * math.sin(2*math.pi*i/frames)
        shade(cv, ell(c, c, r*(1+sq), r*(1-sq)), c, c, r, pal)
        cv.put_in(c - r*0.4, c - r*0.4, '#ffffff')
        if core: cv.put_in(c, c, core); cv.put_in(c-1, c, core)
        cv.outline(); out.append(cv)
    return out

PROJ = {
    'AmoebaRing': projectile(10, 3.4, ['#c0306a', '#f05a92', '#ff9cc0', '#ffe0ec'], None),
    'AmoebaSpiral': projectile(10, 3.0, ['#7a3aa0', '#b064d8', '#e09cf8', '#f8e4ff'], None),
    'AmoebaFan': projectile(12, 4.2, ['#b01838', '#f03050', '#ff8a8a', '#fff0e0'], '#fff4d0'),
}

# ---------------- icono del candado (8x10) ----------------
LOCK = [
    "..####..",
    ".#....#.",
    ".#....#.",
    ".#....#.",
    "########",
    "#oooooo#",
    "#ooxxoo#",
    "#oooxoo#",
    "#oooooo#",
    "########",
]
def lock_icon():
    im = Image.new('RGBA', (8, 10), (0, 0, 0, 0))
    for y, row in enumerate(LOCK):
        for x, ch in enumerate(row):
            if ch == '#': im.putpixel((x, y), OUTL)
            elif ch == 'o': im.putpixel((x, y), C('#c8c8d4'))
            elif ch == 'x': im.putpixel((x, y), C('#3a3a48'))
    # el arco del candado en gris claro, con su contorno
    for x, y in ((2, 1), (5, 1), (2, 2), (5, 2), (2, 3), (5, 3)): im.putpixel((x, y), C('#9898a8'))
    return im

# ---------------- guardar ----------------
def save_sheets(anims, folder, prefix):
    os.makedirs(folder, exist_ok=True)
    for name, frames in anims.items():
        n = frames[0].n
        sheet = Image.new('RGBA', (n*len(frames), n), (0, 0, 0, 0))
        for i, cv in enumerate(frames): sheet.paste(cv.img(), (i*n, 0))
        sheet.save(os.path.join(folder, f'{prefix}_{name}.png'))

def save_gifs(anims, folder, fps):
    os.makedirs(folder, exist_ok=True)
    for name, frames in anims.items():
        n = frames[0].n; S = 4 if n <= 64 else 3
        imgs = []
        for cv in frames:
            g = Image.new('RGBA', (n, n), C('#141c33')); g.alpha_composite(cv.img())
            imgs.append(g.resize((n*S, n*S), Image.NEAREST).convert('RGB'))
        imgs[0].save(os.path.join(folder, f'{name}.gif'), save_all=True, append_images=imgs[1:],
                     duration=int(1000 / fps.get(name, 8)), loop=0)

def preview(groups, path):
    S = 2; pad = 8; label_w = 120
    rows = [(f'{g} {n}', fr) for g, anims in groups for n, fr in anims.items()]
    width = label_w + max(sum(cv.n*S + pad for cv in fr) for _, fr in rows) + pad
    height = sum(max(cv.n for cv in fr)*S + pad for _, fr in rows) + pad
    pv = Image.new('RGBA', (width, height), C('#141c33'))
    draw = ImageDraw.Draw(pv); y = pad
    for label, fr in rows:
        draw.text((6, y + 4), label, fill=C('#f4bfd0'))
        x = label_w
        for cv in fr:
            pv.alpha_composite(cv.img().resize((cv.n*S, cv.n*S), Image.NEAREST), (x, y)); x += cv.n*S + pad
        y += max(cv.n for cv in fr)*S + pad
    pv.save(path)

if __name__ == '__main__':
    B = big(); Hh = half()
    save_sheets(B, OUT_BIG, 'BossAmoeba')
    save_sheets(Hh, OUT_HALF, 'BossAmoebaHalf')
    save_sheets(PROJ, OUT_PROJ, 'Projectile')
    os.makedirs(OUT_UI, exist_ok=True)
    lock_icon().save(os.path.join(OUT_UI, 'Icon_Lock.png'))
    fps = {'Idle': 6, 'Angry': 8, 'Angry_Transition': 8, 'Split': 8, 'Merge': 8, 'Rage': 4, 'Cast_1': 8, 'Cast_2': 10,
           'Cast_3': 12, 'Exhaust': 8, 'Tired': 4, 'Death': 10, 'Move': 10,
           **{f'Aim_{k}': 8 for k in range(8)}, **{f'AimCalm_{k}': 8 for k in range(8)}}
    save_gifs(B, os.path.join(OUT_BIG, 'gif'), fps)
    save_gifs(Hh, os.path.join(OUT_HALF, 'gif'), fps)
    preview([('Grande', B), ('Mediana', Hh), ('Proyectil', PROJ)], os.path.join(OUT_BIG, 'vista_previa_BossAmoeba.png'))
    print('Listo:', len(B), 'animaciones grandes,', len(Hh), 'medianas,', len(PROJ), 'proyectiles.')
