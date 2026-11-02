"""Pulido del piso 3 de Primordia (tanda de arte de noviembre de 2026): MUESTRA en una dirección (rana de lado).

1. Lodo en el cuerpo: textura de lodo dentro de la silueta hasta el nivel de hundimiento, borde grumoso, burbujas; al
   salir, manchas que gotean y se limpian al andar (el código lo hace con un shader: textura + ruido, ver README).
2. Puertas: capa delantera del arco (`Doorway_Top_Front`, sacada del arco de cada tema) y sombra del hueco
   (`Doorway_Top_Shade`) para que el jugador pase por detrás del arco y se oscurezca al entrar.
3. Llamas genéricas sobre el cuerpo (S/M/L) y lenguas sueltas para el contorno.
4. Iconos de las barras de estado: quemado (llama) y nube tóxica (nube con calavera), lleno y vacío.
5. Agua de charca: salpicadura al entrar, onda por paso, línea de agua en las patas, gotas al salir y vapor si ardes.
6. Fumarola: ráfaga de vapor detrás de lo que empuja.
Hecho con la skill primordia-sprites (pixel.py): 16 px por unidad, luz arriba a la izquierda.

Uso: python generar_pulido_piso3.py [carpeta_salida]
"""
import math, os, random, sys
from collections import deque

def _skill_scripts():
    d = os.path.dirname(os.path.abspath(__file__))
    while True:
        p = os.path.join(d, '.claude', 'skills', 'primordia-sprites', 'scripts')
        if os.path.isdir(p): return p
        if os.path.dirname(d) == d: raise SystemExit('No encuentro .claude/skills/primordia-sprites/scripts')
        d = os.path.dirname(d)
sys.path.insert(0, _skill_scripts())
from pixel import *
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
MT = os.path.dirname(HERE)                                   # docs/multimedia-temp
OUT = sys.argv[1] if len(sys.argv) > 1 else HERE
GIFS = os.path.join(OUT, 'gif')
TIRAS = os.path.join(__import__('tempfile').gettempdir(), 'primordia_tiras')   # tiras de revisión (fuera del repo)
BG = C('#1c2e20')

MUD = [C(c) for c in ('#22160f', '#33221a', '#4a3324', '#634830', '#86683f')]   # lodo profundo
MUD_WET = C('#b09468')                                                          # brillo húmedo
WATER = [C(c) for c in ('#16302e', '#1d4440', '#2a5e54', '#4e8a72', '#9cc8a0')]
FIRE = [C(c) for c in ('#8a1e14', '#d4441c', '#f88c2a', '#ffd25a', '#fff4c0')]
TOXIC = [C(c) for c in ('#2a2a12', '#4e4e1a', '#7a7a28', '#b0b03a', '#e4e478')]
CHAR = [C(c) for c in ('#141114', '#221c1e', '#352b2a', '#4c3e38', '#6a5848')]
STEAM = [C(c) for c in ('#6a7a78', '#9aaaa6', '#c8d4d0', '#eef4f0')]

def lum(c): return 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2]
def with_a(c, a): return (c[0], c[1], c[2], max(0, min(255, int(a))))

def load_frames(path, cell):
    im = Image.open(path).convert('RGBA')
    return [im.crop((i * cell[0], 0, (i + 1) * cell[0], cell[1])) for i in range(im.width // cell[0])]

def frog(anim, view):
    return load_frames(os.path.join(MT, 'floor3-bodies', 'Frog', f'Player_Frog_{anim}_{view}.png'), (32, 32))

def to_cv(im):
    cv = Canvas(im.width, im.height)
    for y in range(im.height):
        for x in range(im.width):
            c = im.getpixel((x, y))
            if c[3]: cv.px[(x, y)] = c
    return cv

# =================================================================================================
# Ruido periódico (tileable) para la textura y la máscara del lodo
# =================================================================================================
def value_noise(n, period, seed):
    rnd = random.Random(seed)
    g = [[rnd.random() for _ in range(period)] for _ in range(period)]
    def at(x, y):
        fx, fy = x * period / n, y * period / n
        x0, y0 = int(fx) % period, int(fy) % period
        x1, y1 = (x0 + 1) % period, (y0 + 1) % period
        tx, ty = fx - int(fx), fy - int(fy)
        tx, ty = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
        a = g[y0][x0] + (g[y0][x1] - g[y0][x0]) * tx
        b = g[y1][x0] + (g[y1][x1] - g[y1][x0]) * tx
        return a + (b - a) * ty
    return at

def mud_noise(n=32):
    """Máscara de manchas (0..1, en 8 escalones): dónde se queda pegado el lodo al salir y el perfil del borde."""
    a, b = value_noise(n, 4, 7), value_noise(n, 8, 11)
    return [[round((0.65 * a(x, y) + 0.35 * b(x, y)) * 7) / 7 for x in range(n)] for y in range(n)]

def mud_texture(n=16, seed=3):
    """Textura de lodo tileable: grumos redondeados (cúpula con luz arriba a la izquierda) sobre un fondo húmedo.
    Devuelve índices de rampa 0..4 (5 = brillo húmedo)."""
    rnd = random.Random(seed)
    sp = value_noise(n, 4, seed + 1)
    idx = [[2 if sp(x, y) > .62 else 1 for x in range(n)] for y in range(n)]
    lumps = [(rnd.uniform(0, n), rnd.uniform(0, n), rnd.uniform(1.2, 2.3)) for _ in range(12)]
    for y in range(n):
        for x in range(n):
            for (cx, cy, r) in lumps:
                dx = (x + .5 - cx + n / 2) % n - n / 2
                dy = (y + .5 - cy + n / 2) % n - n / 2
                d = math.hypot(dx, dy)
                if d <= r:
                    lam = (-dx - dy) / (r * 1.4)
                    idx[y][x] = 4 if lam > .45 else 3 if lam > -.1 else 2
                    if lam > .7 and d > r * .4: idx[y][x] = 5
    return idx

NOISE = mud_noise()
MUD_MAX = 18                                         # 85 % del alto de la rana: los ojos siempre asoman
SINK_SHARE = .5                                      # parte del nivel que el cuerpo se hunde de verdad
TEX = mud_texture()

def mud_color(i): return MUD_WET if i == 5 else MUD[max(0, min(4, i))]

def mud_cover(sprite, level, stain=0.0, clean=1.0, t=0, feet=None, sunk=0, simple=False):
    """Simula el shader del lodo sobre un cuadro del jugador.
    level: alto cubierto en píxeles desde los pies (con borde grumoso). stain: alto de las manchas que quedan (al salir);
    clean: 0..1, cuánto se ha limpiado (con 1 no queda nada). t: tiempo (mueve el borde y el brillo).
    sunk: píxeles hundidos bajo la superficie (no se ven; el cuadro se dibuja tantos píxeles más abajo)."""
    cv = to_cv(sprite)
    bbox = sprite.getbbox()
    if not bbox: return cv
    feet = feet if feet is not None else bbox[3] - 1
    if sunk > 0:
        for (x, y) in list(cv.px):
            if feet - y < sunk: del cv.px[(x, y)]
    lums = sorted(lum(c) for c in cv.px.values() if c[:3] != OUTL[:3])
    lo, hi = lums[len(lums) // 4], lums[3 * len(lums) // 4]
    for (x, y), c in list(cv.px.items()):
        h = feet - y                                   # altura sobre los pies
        edge = level + (NOISE[(x + t) % 32][x % 32] - .5) * 3.2 + math.sin(x * 1.3 + t * .9) * .6
        covered = level > 0 and h <= edge
        rim = covered and h > edge - 1
        stained = (not covered and stain > 0 and h <= stain + NOISE[x % 32][(x * 2) % 32] * 2
                   and NOISE[(y * 3 + 5) % 32][(x + 9) % 32] > clean)
        if not (covered or stained): continue
        if c[:3] == OUTL[:3]: continue                   # el contorno se queda: la silueta se sigue leyendo
        if c[:3] == EYE[:3]: continue                    # y el blanco de los ojos: asoman del lodo
        i = TEX[(y + t // 3) % 16][x % 16]
        L = lum(c)
        if i != 5: i += -1 if L < lo else (1 if L > hi else 0)
        if rim: i = 4 if (x + t) % 3 else 5
        if stained and i == 5: i = 3
        if simple:                                     # enemigos: sin textura, dos tonos por la luz del cuerpo y el borde
            i = 4 if rim else (3 if L > hi else 2 if L > lo else 1)
        cv.px[(x, y)] = mud_color(i)
    return cv

def mud_state(sprite, level, t=0, simple=False, n=40):
    """Cuadro completo en un lienzo n×n: el cuerpo hundido la mitad del nivel, el anillo detrás y delante, y el lodo
    subiendo por encima (lo que hará el código)."""
    bb = sprite.getbbox()
    feet = bb[3] - 1
    sunk = int(level * SINK_SHARE)
    ring_f, ring_b = fx_mud_ring(max(12, bb[2] - bb[0] - 2))
    out = Image.new('RGBA', (n, n), (0, 0, 0, 0))
    ox, oy = (n - sprite.width) // 2, n - 6 - feet          # los pies en y = n - 6
    rx = ox + (bb[0] + bb[2]) // 2 - ring_f[0].w // 2
    if sunk > 0: out.alpha_composite(ring_b[t % 4].img(), (rx, n - 6 - 4))
    out.alpha_composite(mud_cover(sprite, level, t=t, sunk=sunk, simple=simple).img(), (ox, oy + sunk))
    if sunk > 0: out.alpha_composite(ring_f[t % 4].img(), (rx, n - 6 - 4))
    return out

def placed(img, n=40):
    bb = img.getbbox()
    out = Image.new('RGBA', (n, n), (0, 0, 0, 0))
    out.alpha_composite(img, ((n - img.width) // 2, n - 6 - (bb[3] - 1)))
    return out

def body_states(sprite, simple=False):
    """Fila de revisión: normal, lodo 1/3, 2/3, máximo, manchas, agua, ardiendo."""
    h = sprite.getbbox()[3] - sprite.getbbox()[1]
    mx = int(h * .85)
    row = [placed(sprite)]
    row += [mud_state(sprite, L, t=i, simple=simple) for i, L in enumerate((mx // 3, 2 * mx // 3, mx))]
    if not simple: row.append(placed(mud_cover(sprite, 0, mx, .45).img()))
    row.append(placed(water_cover(sprite, 4, 1).img()))
    burn = placed(burn_tint(sprite, 3))
    size = 'S' if h < 14 else 'M'
    fl = fx_body_flame(size)[2].img()
    burn.alpha_composite(fl, ((40 - fl.width) // 2, 40 - 6 - fl.height + 3))
    row.append(burn)
    return row

BODY_SHEETS = [('floor3-bodies', 'Frog', 'Player_Frog'), ('floor3-bodies', 'Diplocaulus', 'Player_Diplocaulus'),
               ('floor3-bodies', 'Whipscorpion', 'Player_Whipscorpion')]
ENEMY_SHEETS = [('floor3-enemies', 'Beetle', 'Beetle_Move'), ('floor3-enemies', 'Toad', 'Toad_Idle'),
                ('floor3-enemies', 'Spider', 'Spider_Move')]

def fx_mud_bubble():
    """Burbuja de lodo que sale en el borde: se hincha y revienta en dos gotas (7 cuadros, 8×8, pivote abajo)."""
    frames = []
    for k, r in enumerate((1.0, 1.6, 2.2, 2.8, 3.2, -1, -2)):
        cv = Canvas(8)
        if r > 0:
            ellipse(cv, 4, 7.6 - r, r, r, [MUD[1], MUD[2], MUD[3], MUD[4]])
            if r > 2: cv.put(3, 7.6 - r * 1.5, MUD_WET)
        else:
            s = -r
            for (dx, dy) in ((-1.5 * s, -2 * s), (1.5 * s, -2.5 * s + 1)):
                cv.put(4 + dx, 6 + dy, MUD[3]); cv.put(4 + dx, 7 + dy, MUD[1])
            for x in range(2, 6): cv.put(x, 7, MUD[2])
        frames.append(cv)
    return frames

def fx_mud_ring(w=30):
    """Anillo de lodo en la superficie alrededor de lo que se hunde (w×8, 4 cuadros en bucle, pivote en el centro de la
    superficie). Delante del cuerpo: el labio de abajo tapa el corte; el de arriba, detrás (el código dibuja la hoja
    `_Back` detrás del jugador y la normal delante)."""
    front, back = [], []
    for k in range(4):
        f, b = Canvas(w, 8), Canvas(w, 8)
        rx, ry = w / 2 - .5, 2.6
        for y in range(8):
            for x in range(w):
                dx, dy = (x + .5 - w / 2) / rx, (y + .5 - 4) / ry
                wob = .08 * math.sin(x * .9 + k * 1.6)
                d = dx * dx + dy * dy
                if d <= 1 + wob:
                    lip = d > .55 + wob
                    if y + .5 >= 4:                             # mitad de delante
                        c = MUD[4] if lip and dy < .75 else MUD[3] if lip else MUD[2]
                        if lip and dy > .75: c = MUD[1]
                        f.put(x, y, c)
                    else:                                       # mitad de detrás
                        b.put(x, y, MUD[3] if lip else MUD[1])
        for s in range(2):                                     # brillos húmedos que corren por el labio
            x = int((k * 4 + s * w / 2 + 3) % w)
            f.put_in(x, 5, MUD_WET)
        front.append(f); back.append(b)
    return front, back

def fx_mud_drip():
    """Goterón que se desprende del cuerpo y salpica en el suelo (6 cuadros, 6×10, pivote arriba)."""
    frames = []
    for k in range(6):
        cv = Canvas(6, 10)
        if k < 2:
            for j in range(2 + k): cv.put(2, j, MUD[2] if j else MUD[3])
            cv.put(3, 1 + k, MUD[1])
        elif k < 4:
            y = 2 + (k - 2) * 3
            cv.put(2, y, MUD[3]); cv.put(3, y, MUD[2]); cv.put(2, y + 1, MUD[2]); cv.put(3, y + 1, MUD[1])
        else:
            s = k - 4
            for x in range(1 - s, 5 + s): cv.put(x, 9, MUD[1 + (x % 2)])
            cv.put(1 - s, 8 - s, MUD[3]); cv.put(4 + s, 8 - s, MUD[3])
        frames.append(cv)
    return frames

# =================================================================================================
# Puertas: capa delantera del arco y sombra del hueco
# =================================================================================================
DOOR_THEMES = [('environment', t) for t in ('Soup', 'Amoeba', 'Shallows', 'Cambrian', 'Abyss', 'Shore', 'Boss')] + \
              [('floor3-environment', t) for t in ('Swamp', 'Smoke', 'Burnt')]

def arch3(x, y):
    """Forma del hueco del arco del piso 3 (la de arch_shape en generar_tilesets_piso3.py)."""
    if not 6 <= x < 26: return False
    return y >= 5 + 6 * ((x + .5 - 15.5) / 10) ** 2

def hole_mask(im, floor3=False):
    """Hueco del arco. Pisos 1-2: relleno desde abajo en el centro por los píxeles oscuros. Piso 3: la forma del arco,
    menos lo que se dibujó encima (raicillas, frondas), que va delante; la franja del fondo (y >= 23: paso oscuro
    tramado y suelo) es hueco entera."""
    w, h = im.size
    if floor3:
        return {(x, y) for y in range(h) for x in range(w)
                if arch3(x, y) and (y >= 23 or lum(im.getpixel((x, y))) <= 25)}
    seed = (w // 2, h - 6)
    base = lum(im.getpixel(seed))
    lim = max(46, base + 24)
    seen, q = {seed}, deque([seed])
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            p = (x + dx, y + dy)
            if p in seen or not (0 <= p[0] < w and 0 <= p[1] < h): continue
            c = im.getpixel(p)
            if c[3] and lum(c) <= lim:
                seen.add(p); q.append(p)
    # el suelo que asoma al fondo del hueco (piso 3) también es hueco: toda columna del hueco hasta abajo
    for x in range(w):
        ys = [y for y in range(h) if (x, y) in seen]
        if ys:
            for y in range(min(ys), h):
                c = im.getpixel((x, y))
                if (x, y) not in seen and lum(c) <= lim + 40 and y > h - 8: seen.add((x, y))
    return seen

def door_layers(im, floor3=False):
    """Front = el arco sin el hueco (va delante del jugador). Shade = el hueco, más opaco cuanto más arriba
    (va encima del jugador y debajo del Front: lo oscurece al entrar)."""
    hole = hole_mask(im, floor3)
    front = Canvas(im.width, im.height)
    for y in range(im.height):
        for x in range(im.width):
            c = im.getpixel((x, y))
            if c[3] and (x, y) not in hole: front.px[(x, y)] = c
    top = min(y for (_, y) in hole)
    dark = min((im.getpixel(p) for p in hole), key=lum)
    shade = Canvas(im.width, im.height)
    for (x, y) in hole:
        f = 1 - (y - top) / max(1, (im.height - 3 - top))      # 1 arriba, 0 en la boca del hueco
        a = SHADE_STEPS[max(0, min(4, int(f * 5)))]
        if a: shade.px[(x, y)] = with_a(dark, a)
    return front, shade

SHADE_STEPS = [0, 70, 130, 185, 225]

def darken_by(sprite, shade, ox, oy):
    """Lo que hará la sombra del hueco en el juego: oscurece los píxeles del jugador que caen en el hueco."""
    out = sprite.copy()
    for y in range(sprite.height):
        for x in range(sprite.width):
            c = sprite.getpixel((x, y))
            if not c[3]: continue
            p = (x + ox, y + oy)
            if 0 <= p[0] < shade.width and 0 <= p[1] < shade.height:
                s = shade.getpixel(p)
                if s[3]:
                    k = s[3] / 255
                    out.putpixel((x, y), tuple(int(c[i] * (1 - k) + s[i] * k) for i in range(3)) + (c[3],))
    return out

def door_scene(theme_dir, frames_up, steps=10):
    """Escena antes/después: la rana sube y entra por el arco (2×2 tiles), con la cara de pared alrededor."""
    arch = Image.open(os.path.join(theme_dir, 'Doorway_Top.png')).convert('RGBA')
    tiles = os.path.join(theme_dir, 'tiles')
    floor = Image.open(os.path.join(tiles, 'Floor_0.png')).convert('RGBA') if os.path.isdir(tiles) else None
    front, shade = door_layers(arch, 'floor3' in theme_dir)
    front, shade = front.img(), shade.img()
    W, H = 64, 72
    out = []
    for k in range(steps + 4):
        k2 = min(k, steps)
        y = 64 - 32 - k2 * 3.2                                  # la rana sube (cuadro de 32)
        pair = []
        for mode in ('antes', 'después'):
            sc = Image.new('RGBA', (W, H), BG)
            if floor:
                for ty in range(32, H, 16):
                    for tx in range(0, W, 16): sc.alpha_composite(floor, (tx, ty))
            for tx in range(0, W, 16):                          # cara de pared lisa a los lados (oscura)
                for ty in range(0, 32, 1):
                    for xx in range(16):
                        sc.putpixel((tx + xx, ty), (30, 40, 32, 255) if ty > 3 else (50, 70, 50, 255))
            sc.alpha_composite(arch, (16, 0))
            f = frames_up[k % len(frames_up)]
            if mode == 'antes':
                sc.alpha_composite(f, (16, int(y)))
            else:
                # la sombra solo se ve sobre la rana (en el juego, mientras cruza); el arco va delante
                sc.alpha_composite(darken_by(f, shade, 0, int(y)), (16, int(y)))
                sc.alpha_composite(front, (16, 0))
            pair.append(sc)
        both = Image.new('RGBA', (W * 2 + 6, H), (0, 0, 0, 255))
        both.paste(pair[0], (0, 0)); both.paste(pair[1], (W + 6, 0))
        out.append(both)
    return out, front, shade

# =================================================================================================
# Llamas sobre el cuerpo
# =================================================================================================
def flame_frame(n, t, tongues, seed, smoke=False):
    """Hoguera genérica en un lienzo n×n, base abajo en el centro. Cada lengua es una gota que sube y titila; el color
    va por la intensidad (núcleo claro abajo en el centro, puntas rojas)."""
    rnd = random.Random(seed)
    cv = Canvas(n)
    heat = {}
    base_y = n - 1.5
    for i in range(tongues):
        ox = (i - (tongues - 1) / 2) * n / (tongues + 1.2) + rnd.uniform(-1, 1)
        hgt = n * rnd.uniform(.45, .8) * (1 - abs(ox) / n * 1.1)
        ph = rnd.uniform(0, 6.28)
        hgt *= 1 + .18 * math.sin(t * 2 * math.pi / 6 * (1 + i % 2) + ph)
        sway = math.sin(t * 2 * math.pi / 6 + ph) * n * .06
        wid = n * .17 * rnd.uniform(.8, 1.2)
        for y in range(n):
            v = (base_y - (y + .5)) / hgt                      # 0 en la base, 1 en la punta
            if not 0 <= v <= 1: continue
            half = wid * (math.sin(min(1, v * 1.6 + .25) * math.pi) * (1 - v) ** .6 + .15 * (1 - v))
            cx = n / 2 + ox + sway * v * v * 2
            for x in range(n):
                d = abs(x + .5 - cx) / max(.5, half)
                if d <= 1:
                    hv = (1 - v) * (1 - d * .8)
                    heat[(x, y)] = max(heat.get((x, y), 0), hv)
    for (x, y), hv in heat.items():
        i = 0 if hv < .14 else 1 if hv < .3 else 2 if hv < .5 else 3 if hv < .72 else 4
        cv.put(x, y, FIRE[i])
    # chispas que suben
    for s in range(max(1, n // 12)):
        sx = n / 2 + math.sin(seed + s * 2.3) * n * .3
        sy = n - 1 - ((t * n / 6 + s * n / 2.5) % (n * .95))
        cv.put_out(sx + math.sin(t + s) * 1.2, sy, FIRE[3] if (t + s) % 2 else FIRE[2])
    if smoke:                                                  # bocanada de humo arriba (solo la grande)
        sy = n * .18 - (t % 6) * .8
        r = 1.6 + (t % 6) * .25
        a = 200 - (t % 6) * 28
        for y in range(int(sy - r - 1), int(sy + r + 2)):
            for x in range(int(n / 2 - r - 2), int(n / 2 + r + 2)):
                if math.hypot(x + .5 - n / 2 - 1, y + .5 - sy) <= r:
                    cv.put_out(x, y, with_a(CHAR[3] if (x + y) % 3 else CHAR[4], a))
    return cv

FLAME_SIZES = {'S': (16, 3), 'M': (24, 4), 'L': (32, 5)}

def fx_body_flame(size):
    n, tongues = FLAME_SIZES[size]
    return [flame_frame(n, t, tongues, seed=n, smoke=size == 'L') for t in range(6)]

def fx_flame_lick():
    """Lengua suelta (8×10, 5 cuadros, pivote abajo): nace en el contorno, sube y se apaga en una chispa."""
    frames = []
    for k, (h, w) in enumerate(((3, 1.6), (6, 2.2), (8, 2.0), (6, 1.2), (0, 0))):
        cv = Canvas(8, 10)
        for y in range(10):
            v = (9.5 - (y + .5)) / max(h, .1)
            if not 0 <= v <= 1 or h == 0: continue
            half = w * (1 - v) ** .7
            for x in range(8):
                d = abs(x + .5 - 4 - v * v * 1.2) / max(.4, half)
                if d <= 1:
                    hv = (1 - v) * (1 - d * .7)
                    cv.put(x, y, FIRE[1 if hv < .25 else 2 if hv < .5 else 3 if hv < .75 else 4])
        if k >= 3: cv.put(5, 9 - h - 2 if h else 2, FIRE[3])
        frames.append(cv)
    return frames

# =================================================================================================
# Iconos de las barras de estado (lleno y vacío: el código pinta el vacío y encima el lleno recortado por la barra)
# =================================================================================================
FLAME_ICON = [
    "....#.......",
    "....##......",
    "...###......",
    "...####..#..",
    "..#####..##.",
    "..######.##.",
    ".##########.",
    ".##########.",
    "############",
    "############",
    "############",
    ".##########.",
    "..########..",
    "....####....",
]
CLOUD_ICON = [
    "...###...###...",
    "..#####.#####..",
    ".#############.",
    "###############",
    "###############",
    "###############",
    "###############",
    ".#############.",
    "..###.###.###..",
]
SKULL = [            # sobre la nube, centrada (B = hueso, o = cuenca/dientes)
    ".BBBBB.",
    "BBBBBBB",
    "BooBooB",
    "BBBoBBB",
    ".BoBoB.",
]

def icon_from(rows, paint):
    h, w = len(rows), len(rows[0])
    cv = Canvas(w + 2, h + 2)
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch == '#': cv.put(x + 1, y + 1, paint(x, y, w, h))
    cv.outline()
    return cv

def burn_paint(x, y, w, h):
    v = y / (h - 1); d = abs(x + .5 - w / 2 - .5) / (w / 2)
    hv = (v * 1.1 - d * .55)
    i = 4 if hv > .78 else 3 if hv > .55 else 2 if hv > .3 else 1 if hv > .1 else 0
    if y < 3: i = min(i, 1)
    return FIRE[i]

def toxic_paint(x, y, w, h):
    # tres bultos de nube, cada uno sombreado como una cúpula con la luz arriba a la izquierda
    best = None
    for (cx, cy, r) in ((4, 3.5, 4), (10.5, 3.5, 4), (7.5, 5.5, 6.5)):
        nx, ny = (x + .5 - cx) / r, (y + .5 - cy) / r
        if nx * nx + ny * ny <= 1.1:
            i = shade(nx, ny, steps=4)
            best = i if best is None else max(best, i)
    return TOXIC[1 + (best if best is not None else 0)]

def skull(cv, ox, oy, eye, bone):
    for y, r in enumerate(SKULL):
        for x, ch in enumerate(r):
            if ch == 'B': cv.put(ox + x, oy + y, bone)
            elif ch == 'o': cv.put(ox + x, oy + y, eye)

def dark_paint(rows, base):
    return lambda x, y, w, h: base[1] if y > h * .55 else base[2]

def icons():
    full_b = icon_from(FLAME_ICON, burn_paint)
    full_b.put(5, 9, FIRE[4]); full_b.put(6, 8, FIRE[4])
    empty_b = icon_from(FLAME_ICON, dark_paint(FLAME_ICON, CHAR))
    full_t = icon_from(CLOUD_ICON, toxic_paint)
    skull(full_t, 5, 3, TOXIC[0], C('#f0f0d0'))
    empty_t = icon_from(CLOUD_ICON, dark_paint(CLOUD_ICON, CHAR))
    skull(empty_t, 5, 3, CHAR[0], CHAR[3])
    return {'Icon_StatusBurn_Full': full_b, 'Icon_StatusBurn_Empty': empty_b,
            'Icon_StatusToxic_Full': full_t, 'Icon_StatusToxic_Empty': empty_t}

def icon_fill(empty, full, f):
    """Simula la barra: el vacío y encima el lleno de abajo arriba hasta f."""
    e, fu = empty.img(), full.img()
    out = e.copy()
    bb = fu.getbbox(); top = bb[3] - (bb[3] - bb[1]) * f
    for y in range(fu.height):
        if y >= top:
            for x in range(fu.width):
                c = fu.getpixel((x, y))
                if c[3]: out.putpixel((x, y), c)
    return out

# =================================================================================================
# Agua de charca
# =================================================================================================
def fx_water_ripple():
    """Onda en anillo bajo los pies a cada paso (20×10, 5 cuadros, centro)."""
    frames = []
    for k in range(5):
        cv = Canvas(20, 10)
        r = 2.5 + k * 1.8
        a = [255, 230, 190, 130, 70][k]
        for y in range(10):
            for x in range(20):
                d = math.hypot((x + .5 - 10) / r, (y + .5 - 5) / (r / 2.2))
                if .82 <= d <= 1.05:
                    top = y + .5 < 5
                    cv.put(x, y, with_a(WATER[4] if top else WATER[3], a))
        if k < 2:                                              # segundo anillo dentro
            r2 = r * .45
            for y in range(10):
                for x in range(20):
                    d = math.hypot((x + .5 - 10) / r2, (y + .5 - 5) / (r2 / 2.2))
                    if .7 <= d <= 1.1: cv.put_out(x, y, with_a(WATER[3], a))
        frames.append(cv)
    return frames

def fx_water_splash():
    """Salpicadura al entrar en la charca (24×20, 6 cuadros, pivote abajo en el centro): corona de gotas que sube y cae
    y un anillo en el agua."""
    rnd = random.Random(5)
    drops = [(rnd.uniform(-1, 1), rnd.uniform(.6, 1.0)) for _ in range(9)]
    frames = []
    for k in range(6):
        cv = Canvas(24, 20)
        t = (k + .6) / 5.5
        r = 3 + k * 1.6                                        # anillo
        for y in range(14, 20):
            for x in range(24):
                d = math.hypot((x + .5 - 12) / r, (y + .5 - 17) / (r / 2.6))
                if .78 <= d <= 1.08: cv.put(x, y, with_a(WATER[4] if y < 17 else WATER[3], 255 - k * 30))
        if k < 3:                                              # corona
            for x in range(8 - k, 16 + k + 1):
                hgt = (3 - k) * (1.5 + math.sin(x * 1.7) * .9)
                for j in range(int(hgt)): cv.put(x, 16 - j, WATER[3] if j < hgt - 1 else WATER[4])
        for (vx, vy) in drops:                                 # gotas en parábola
            x = 12 + vx * 10 * t
            y = 16 - vy * 15 * t + 14 * t * t
            if y < 17 and k < 5:
                cv.put(x, y, WATER[4]); cv.put(x, y + 1, WATER[3])
        frames.append(cv)
    return frames

def fx_water_drip():
    """Gota que cae del cuerpo al salir del agua (4×8, 4 cuadros, pivote arriba)."""
    frames = []
    for k in range(4):
        cv = Canvas(4, 8)
        if k < 3:
            y = k * 2.5
            cv.put(1, y, WATER[4]); cv.put(1, y + 1, WATER[3])
        else:
            cv.put(0, 7, WATER[4]); cv.put(3, 7, WATER[4]); cv.put(1, 7, WATER[3]); cv.put(2, 7, WATER[3])
        frames.append(cv)
    return frames

def fx_steam_hiss():
    """Vapor con chisporroteo al apagarse el quemado en el agua (32×32, 7 cuadros, pivote abajo en el centro)."""
    rnd = random.Random(9)
    puffs = [(rnd.uniform(-7, 7), rnd.uniform(0, 4), rnd.uniform(2.6, 4.2), rnd.uniform(.8, 1.3)) for _ in range(6)]
    frames = []
    for k in range(7):
        cv = Canvas(32)
        t = k / 6
        for (px, py, r, sp) in puffs:
            cx = 16 + px * (1 + t * .6)
            cy = 27 - py - t * 20 * sp
            rr = r * (.6 + t * .9)
            a = int(235 * (1 - t ** 1.6))
            for y in range(int(cy - rr - 1), int(cy + rr + 2)):
                for x in range(int(cx - rr - 1), int(cx + rr + 2)):
                    nx, ny = (x + .5 - cx) / rr, (y + .5 - cy) / rr
                    if nx * nx + ny * ny <= 1:
                        i = shade(nx, ny, steps=4)
                        cv.put_out(x, y, with_a(STEAM[i], a))
        if k < 3:                                              # chispas que se apagan
            for s in range(4 - k):
                sx = 16 + math.cos(s * 1.9 + k) * (5 + k * 3)
                sy = 25 - k * 3 - s
                cv.put(sx, sy, FIRE[3] if k == 0 else FIRE[1])
        frames.append(cv)
    return frames

def water_cover(sprite, depth, t, feet=None):
    """Simula el shader del agua: la franja de abajo (depth px) se ve a través del agua turbia, con la línea de agua
    clara que titila."""
    cv = to_cv(sprite)
    bb = sprite.getbbox()
    feet = feet if feet is not None else bb[3] - 1
    for (x, y), c in list(cv.px.items()):
        h = feet - y
        if h < depth - 1:
            k = .55
            w = WATER[2]
            cv.px[(x, y)] = (int(c[0] * (1 - k) + w[0] * k), int(c[1] * (1 - k) + w[1] * k), int(c[2] * (1 - k) + w[2] * k), 255)
        elif h == depth - 1:
            cv.px[(x, y)] = WATER[4] if (x + t) % 4 else WATER[3]
    return cv

# =================================================================================================
# Fumarola: ráfaga detrás de lo que empuja
# =================================================================================================
def fx_vent_gust():
    """Ráfaga de vapor y líneas de velocidad (24×16, 4 cuadros, pivote a la derecha en el centro: se pone detrás de la
    criatura empujada, mirando hacia donde va; Flip X para la izquierda)."""
    frames = []
    for k in range(4):
        cv = Canvas(24, 16)
        a = [255, 220, 160, 90][k]
        for j, (yy, L) in enumerate(((4, 12), (8, 18), (12, 10))):
            x1 = 23 - k * 2
            for x in range(max(0, x1 - L + k * 3), x1):
                v = (x1 - x) / L
                if (x + j) % 5 == 0: continue
                cv.put(x, yy + (1 if v > .6 and j == 1 else 0), with_a(STEAM[3] if v < .3 else STEAM[2], a))
        for (px, py, r) in ((6, 6, 2.6), (11, 10, 2.2), (3, 10, 1.8)):
            rr = r * (1 + k * .35)
            for y in range(int(py - rr - 1), int(py + rr + 2)):
                for x in range(int(px - k * 2 - rr - 1), int(px - k * 2 + rr + 2)):
                    nx, ny = (x + .5 - (px - k * 2)) / rr, (y + .5 - py) / rr
                    if nx * nx + ny * ny <= 1: cv.put_out(x, y, with_a(STEAM[1 + shade(nx, ny, 3)], a * .8))
        frames.append(cv)
    return frames

# =================================================================================================
# Escenas
# =================================================================================================
def swamp_floor(W, H):
    sc = Image.new('RGBA', (W, H), BG)
    tiles = os.path.join(MT, 'floor3-environment', 'Swamp', 'tiles')
    fl = [Image.open(os.path.join(tiles, f'Floor_{i}.png')).convert('RGBA') for i in range(6)]
    rnd = random.Random(2)
    for y in range(0, H, 16):
        for x in range(0, W, 16): sc.alpha_composite(fl[rnd.randrange(6)], (x, y))
    return sc

def deep_mud(sc, x0, y0, w, h):
    rnd = random.Random(4)
    for y in range(y0, y0 + h):
        for x in range(x0, x0 + w):
            i = 1 + (TEX[y % 16][x % 16] >= 3) if y > y0 else 3
            sc.putpixel((x, y), MUD[i] if rnd.random() > .05 else MUD[2])

def scene_mud():
    """La rana anda hacia el lodo, se hunde hasta el 90 %, sale y el lodo se limpia mientras anda."""
    move = frog('Move', 'side')
    bub, drip = fx_mud_bubble(), fx_mud_drip()
    ring_f, ring_b = fx_mud_ring()
    W, H = 96, 48
    frames = []
    N = 44
    for k in range(N):
        sc = swamp_floor(W, H)
        deep_mud(sc, 22, 26, 44, 22)
        if k < 6: x, level, stain, clean = 0 + k * 3, 0, 0, 1
        elif k < 24: x, level, stain, clean = 18 + (k - 6) * .9, min(MUD_MAX, (k - 6) * 1.1), 0, 1
        elif k < 30: x, level, stain, clean = 34 + (k - 24) * 2, max(0, MUD_MAX - (k - 24) * 4), 0, 1
        else: x, level, stain, clean = 46 + (k - 30) * 2.4, 0, MUD_MAX, min(1, .25 + (k - 30) * .055)
        if k >= 24 and k < 30: stain = MUD_MAX
        f = move[k % 4]
        sunk = int(level * SINK_SHARE)                         # el cuerpo baja la mitad del nivel; el lodo sube el resto
        cv = mud_cover(f, level, stain, clean if k >= 30 else .25, t=k, sunk=sunk)
        img = cv.img()
        if sunk > 0: sc.alpha_composite(ring_b[k % 4].img(), (int(x) + 1, 16 + 27 - 4))
        sc.alpha_composite(img, (int(x), 16 + sunk))
        if sunk > 0: sc.alpha_composite(ring_f[k % 4].img(), (int(x) + 1, 16 + 27 - 4))
        if 2 < level:                                          # burbujas en el borde (dentro del cuerpo)
            ph = k % 7
            bx = int(x) + 6 + (k // 7 % 3) * 5
            by = 16 + sunk + 27 - int(level) - 5
            sc.alpha_composite(bub[ph].img(), (bx, by))
        if k >= 26:                                            # goterones al salir
            for d in range(2):
                ph = (k - 26 + d * 3) % 6
                if NOISE[d * 7][k % 32] > clean * .8 or k < 34:
                    sc.alpha_composite(drip[ph].img(), (int(x) + 9 + d * 9, 16 + 18))
        frames.append(sc)
    return frames

BURN_TINT = (.08, .18, .12, .22, .10, .16)                 # cuánto brillo naranja lleva el cuerpo en cada cuadro (titila)

def burn_tint(sprite, k):
    """Lo que hará el código al arder: SUMA brillo naranja (FIRE[2]) al cuerpo con un parpadeo suave (sumado y no
    mezclado, para que un cuerpo oscuro no se vuelva gris); el contorno no."""
    a = BURN_TINT[k % len(BURN_TINT)]
    out = sprite.copy()
    for y in range(sprite.height):
        for x in range(sprite.width):
            c = sprite.getpixel((x, y))
            if c[3] and c[:3] != OUTL[:3]:
                out.putpixel((x, y), tuple(min(255, int(c[i] + FIRE[2][i] * a)) for i in range(3)) + (c[3],))
    return out

def scene_flames(move):
    W, H = 72, 44
    flames = {s: fx_body_flame(s) for s in FLAME_SIZES}
    lick = fx_flame_lick()
    out = []
    for k in range(12):
        sc = swamp_floor(W, H)
        f = burn_tint(move[k % 4], k)
        sc.alpha_composite(f, (4, 8))
        sc.alpha_composite(flames['M'][k % 6].img(), (4 + 4, 8 + 3))   # base de la llama en el centro bajo del cuerpo
        sc.alpha_composite(lick[k % 5].img(), (4 + 22, 8 + 6))
        sc.alpha_composite(lick[(k + 2) % 5].img(), (4 + 6, 8 + 8))
        sc.alpha_composite(flames['S'][k % 6].img(), (44, 26))
        out.append(sc)
    return out

def scene_water(burning=False):
    move = frog('Move', 'side')
    W, H = 104, 44
    pim = Image.open(os.path.join(MT, 'floor3-environment', 'Swamp', 'props', 'Puddle_Water_Long.png')).convert('RGBA')
    pw = pim.width // 4
    pud = [pim.crop((i * pw, 0, (i + 1) * pw, pim.height)) for i in range(4)]
    rip, spl, drp, stm = fx_water_ripple(), fx_water_splash(), fx_water_drip(), fx_steam_hiss()
    flames = fx_body_flame('M')
    px0 = (W - pw) // 2
    out = []
    ripples = []
    N = 36
    for k in range(N):
        sc = swamp_floor(W, H)
        sc.alpha_composite(pud[(k // 2) % 4], (px0, H - pim.height - 2))
        x = -4 + k * 2.8
        feet_x = x + 16
        inside = px0 + 4 < feet_x < px0 + pw - 4
        if inside and k % 3 == 0: ripples.append((feet_x, k))
        for (rx, rk) in ripples:
            ph = k - rk
            if 0 <= ph < 5: sc.alpha_composite(rip[ph].img(), (int(rx - 10), H - 12 - 5))
        f = move[k % 4]
        if inside:
            sc.alpha_composite(water_cover(f, 4, k).img(), (int(x), H - 12 - 28))
        else:
            sc.alpha_composite(f, (int(x), H - 12 - 28))
        entered = next((kk for kk in range(N) if px0 + 4 < -4 + kk * 2.8 + 16), N)
        exited = next((kk for kk in range(entered, N) if -4 + kk * 2.8 + 16 >= px0 + pw - 4), N)
        burn_on = burning and k < entered
        if burn_on: sc.alpha_composite(flames[k % 6].img(), (int(x) + 4, H - 12 - 28 + 3))
        if 0 <= k - entered < 6: sc.alpha_composite(spl[k - entered].img(), (int(px0 + 4 - 12 + 6), H - 12 - 18))
        if burning and 0 <= k - entered < 7: sc.alpha_composite(stm[k - entered].img(), (int(x), H - 12 - 34))
        if 0 <= k - exited < 8:
            for d in range(2):
                ph = (k - exited + d * 2) % 4
                sc.alpha_composite(drp[ph].img(), (int(x) + 10 + d * 8, H - 12 - 12))
        out.append(sc)
    return out

def scene_vent():
    """La fumarola revienta y la ráfaga empuja a la rana hacia la derecha (fuera de la columna)."""
    vdir = os.path.join(MT, 'floor3-environment', 'Swamp', 'props')
    blast = load_frames(os.path.join(vdir, 'Vent_Blast.png'), (16, 64))
    warn = load_frames(os.path.join(vdir, 'Vent_Warn.png'), (16, 32))
    idle = frog('Idle', 'side')
    gust = fx_vent_gust()
    W, H = 96, 72
    out = []
    vx, mouth = 20, H - 6                                      # la boca: los 16 px de abajo de la hoja
    for k in range(16):
        sc = swamp_floor(W, H)
        bi = k - 4                                             # 4 cuadros de aviso y luego el chorro
        fx_ = 14
        if bi >= 1: fx_ = 14 + (1 - (1 - min(1, (bi - 1) / 4)) ** 2) * 32   # el cuadro 1 empuja (~2 tiles)
        # la rana, de pie sobre la columna (se dibuja antes que el chorro si está dentro)
        sc.alpha_composite(idle[k % 4], (int(fx_), mouth - 26))
        if bi < 0: sc.alpha_composite(warn[k % 4], (vx, mouth - 32 + 8))
        elif bi < len(blast): sc.alpha_composite(blast[bi], (vx, mouth - 64 + 8))
        if 1 <= bi < 5: sc.alpha_composite(gust[bi - 1].img(), (int(fx_) - 14, mouth - 20))
        out.append(sc)
    return out

def strip(frames, gap=2, bg=BG, cols=8, scale=3):
    """Tira de revisión de una escena (varias filas de cuadros)."""
    frames = [as_img(f) for f in frames]
    w, h = frames[0].size
    rows = (len(frames) + cols - 1) // cols
    im = Image.new('RGBA', (cols * (w + gap), rows * (h + gap)), (0, 0, 0, 255))
    for i, f in enumerate(frames): im.alpha_composite(f, ((i % cols) * (w + gap), (i // cols) * (h + gap)))
    return im.resize((im.width * scale, im.height * scale), Image.NEAREST)

# =================================================================================================
def main():
    os.makedirs(GIFS, exist_ok=True); os.makedirs(TIRAS, exist_ok=True)
    fx = os.path.join(OUT, 'Effects')
    # 1. lodo
    noise_img = Image.new('RGBA', (32, 32))
    for y in range(32):
        for x in range(32):
            v = int(NOISE[y][x] * 255); noise_img.putpixel((x, y), (v, v, v, 255))
    os.makedirs(fx, exist_ok=True)
    noise_img.save(os.path.join(fx, 'Fx_MudBody_Noise.png'))
    tex = Canvas(16)
    for y in range(16):
        for x in range(16): tex.px[(x, y)] = mud_color(TEX[y][x])
    tex.img().save(os.path.join(fx, 'Fx_MudBody_Tex.png'))
    bub, mdrip = fx_mud_bubble(), fx_mud_drip()
    save_sheet(os.path.join(fx, 'Fx_MudBubble.png'), bub)
    save_sheet(os.path.join(fx, 'Fx_MudDrip.png'), mdrip)
    for tag, w in (('S', 16), ('M', 30), ('L', 44)):
        rf, rb = fx_mud_ring(w)
        save_sheet(os.path.join(fx, f'Fx_MudRing_{tag}.png'), rf)
        save_sheet(os.path.join(fx, f'Fx_MudRing_{tag}_Back.png'), rb)
    side = frog('Idle', 'side')[0]
    levels = [mud_cover(side, L) for L in (0, 5, 10, 14, MUD_MAX)]
    stains = [mud_cover(side, 0, MUD_MAX, c) for c in (.15, .4, .6, .8, 1)]
    mud_scene = scene_mud()
    gif(os.path.join(GIFS, 'escena_lodo.gif'), mud_scene, 110, BG, scale=4)
    strip(mud_scene, cols=6).save(os.path.join(TIRAS, 'tira_lodo.png'))

    # 2. puertas
    front_all, shade_all = {}, {}
    for (base, t) in DOOR_THEMES:
        d = os.path.join(MT, base, t)
        arch = Image.open(os.path.join(d, 'Doorway_Top.png')).convert('RGBA')
        fr, sh = door_layers(arch, base == 'floor3-environment')
        front_all[t], shade_all[t] = fr, sh
        os.makedirs(os.path.join(OUT, 'Doors', t), exist_ok=True)
        fr.img().save(os.path.join(OUT, 'Doors', t, 'Doorway_Top_Front.png'))
        sh.img().save(os.path.join(OUT, 'Doors', t, 'Doorway_Top_Shade.png'))
    up = frog('Move', 'up')
    for t in ('Swamp', 'Soup', 'Cambrian'):
        base = 'floor3-environment' if t == 'Swamp' else 'environment'
        dsc, _, _ = door_scene(os.path.join(MT, base, t), up)
        gif(os.path.join(GIFS, f'escena_puerta_{t}.gif'), dsc, 120, BG, scale=4)
        strip(dsc[4:14], cols=5).save(os.path.join(TIRAS, f'tira_puerta_{t}.png'))

    # 3. llamas
    fl = {s: fx_body_flame(s) for s in FLAME_SIZES}
    for s, frames in fl.items(): save_sheet(os.path.join(fx, f'Fx_BodyFlame_{s}.png'), frames)
    lick = fx_flame_lick()
    save_sheet(os.path.join(fx, 'Fx_FlameLick.png'), lick)
    move = frog('Move', 'side')
    fsc = scene_flames(move)
    gif(os.path.join(GIFS, 'escena_llamas.gif'), fsc, 90, BG, scale=4)
    strip(fsc[:6], cols=6, scale=4).save(os.path.join(TIRAS, 'tira_llamas.png'))

    # 4. iconos
    ic = icons()
    os.makedirs(os.path.join(OUT, 'UI'), exist_ok=True)
    for k, v in ic.items(): v.img().save(os.path.join(OUT, 'UI', k + '.png'))
    fills = [.0, .25, .5, .75, 1.0]
    burn_fill = [icon_fill(ic['Icon_StatusBurn_Empty'], ic['Icon_StatusBurn_Full'], f) for f in fills]
    tox_fill = [icon_fill(ic['Icon_StatusToxic_Empty'], ic['Icon_StatusToxic_Full'], f) for f in fills]

    # 5. agua
    rip, spl, wdrip, stm = fx_water_ripple(), fx_water_splash(), fx_water_drip(), fx_steam_hiss()
    for n, fr in (('Fx_WaterRipple', rip), ('Fx_WaterSplash', spl), ('Fx_WaterDrip', wdrip), ('Fx_SteamHiss', stm)):
        save_sheet(os.path.join(fx, n + '.png'), fr)
    wsc, wbsc = scene_water(False), scene_water(True)
    gif(os.path.join(GIFS, 'escena_agua.gif'), wsc, 100, BG, scale=4)
    gif(os.path.join(GIFS, 'escena_agua_ardiendo.gif'), wbsc, 100, BG, scale=4)
    strip(wbsc, cols=6).save(os.path.join(TIRAS, 'tira_agua_ardiendo.png'))

    # 6. fumarola
    gust = fx_vent_gust()
    save_sheet(os.path.join(fx, 'Fx_VentGust.png'), gust)
    vsc = scene_vent()
    gif(os.path.join(GIFS, 'escena_fumarola.gif'), vsc, 90, BG, scale=4)
    strip(vsc, cols=7).save(os.path.join(TIRAS, 'tira_fumarola.png'))

    # vista previa
    preview(os.path.join(OUT, 'vista_previa_pulido.png'), {
        'Lodo: niveles': levels,
        'Lodo: limpiándose': stains,
        'Lodo: escena': mud_scene[8:40:4],
        'Fx MudBubble': bub, 'Fx MudDrip': mdrip,
        'Fx BodyFlame S': fl['S'], 'Fx BodyFlame M': fl['M'], 'Fx BodyFlame L': fl['L'],
        'Fx FlameLick': lick,
        'Icono quemado': burn_fill, 'Icono nube tóxica': tox_fill,
        'Fx WaterRipple': rip, 'Fx WaterSplash': spl, 'Fx WaterDrip': wdrip, 'Fx SteamHiss': stm,
        'Fx VentGust': gust,
    }, BG, scale=3)
    preview(os.path.join(OUT, 'vista_previa_puertas.png'),
            {t: [Image.open(os.path.join(MT, b, t, 'Doorway_Top.png')).convert('RGBA'), front_all[t], shade_all[t]]
             for (b, t) in DOOR_THEMES}, (255, 0, 255, 255), scale=3)
    # los mismos estados sobre los tres cuerpos (3 direcciones) y sobre enemigos (versión simple)
    rows = {}
    for (base, folder, name) in BODY_SHEETS:
        for view in ('side', 'down', 'up'):
            f = load_frames(os.path.join(MT, base, folder, f'{name}_Move_{view}.png'), (32, 32))[0]
            rows[f'{folder} {view}'] = body_states(f)
    for (base, folder, name) in ENEMY_SHEETS:
        f = load_frames(os.path.join(MT, base, folder, f'{name}.png'), (32, 32))[0]
        rows[f'{folder} (enemigo)'] = body_states(f, simple=True)
    preview(os.path.join(OUT, 'vista_previa_cuerpos.png'), rows, BG, scale=3)
    print('Hecho:', OUT)

if __name__ == '__main__':
    main()
