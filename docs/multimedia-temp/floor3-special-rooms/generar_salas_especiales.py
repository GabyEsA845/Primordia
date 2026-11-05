"""Arte de dos salas especiales del piso 3 (paso 6, bloque 4; ver README).

- Veta de carbón: franja de carbón que cruza la sala. Tiles por máscara como el lodo profundo (`Coal_<0-15>`,
  `Coal_Corner_*`, en los tres tramos) y capas por casilla para sus estados: prende, arde (muro de fuego), brasas
  (se cruza) y apagada.
- Bosque en humo: dos capas de humo que se repiten sin costura (el código las desplaza despacio y abre un claro
  alrededor del jugador con la máscara), volutas sueltas y ascuas que flotan.
Hecho con la skill primordia-sprites (pixel.py): 16 px por unidad, luz arriba a la izquierda, contorno OUTL.

Uso: python generar_salas_especiales.py
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
TEMP = os.path.normpath(os.path.join(HERE, '..'))
ENV = os.path.join(TEMP, 'floor3-environment')
sys.path.insert(0, ENV)
from base_piso3 import *
import generar_tilesets_piso3 as TS
from PIL import Image

COAL = [C(c) for c in CHAR]                       # #141114 #221c1e #352b2a #4c3e38 #6a5848
SHEEN = [C('#3c4250'), C('#5c6478'), C('#8a94a8')] # brillo frío del carbón (vítreo)
FIREC = [C(c) for c in FIRE]
EMB = [C(c) for c in EMBER]

# =================================================================================================
# Veta de carbón
# =================================================================================================
def coal_surface(seed):
    """Carbón: bloques angulosos (facetas) con aristas que brillan en frío; se repite sin costura."""
    cv = WrapCanvas(16, 16, True, True); rnd = random.Random(seed)
    cells = [(rnd.uniform(0, 16), rnd.uniform(0, 16)) for _ in range(7)]
    def owner(x, y):
        best, bi = 1e9, 0
        for i, (cx, cy) in enumerate(cells):
            for ox in (-16, 0, 16):
                for oy in (-16, 0, 16):
                    d = (x - cx - ox) ** 2 + (y - cy - oy) ** 2
                    if d < best: best, bi = d, i
        return bi
    own = {(x, y): owner(x + .5, y + .5) for y in range(16) for x in range(16)}
    tone = {i: rnd.choice((1, 2, 2, 3)) for i in range(len(cells))}
    for (x, y), i in own.items():
        up = own[(x, (y - 1) % 16)] != i; left = own[((x - 1) % 16, y)] != i
        down = own[(x, (y + 1) % 16)] != i
        if up or left: c = COAL[3] if tone[i] >= 2 else COAL[2]            # arista con luz (arriba a la izquierda)
        elif down: c = COAL[0]
        else: c = COAL[max(0, tone[i] - 1)]
        cv.px[(x, y)] = c
    for (x, y) in ((3, 4), (11, 9), (6, 13)): cv.px[(x, y)] = SHEEN[2]; cv.px[((x + 1) % 16, y)] = SHEEN[0]   # destellos fríos
    return cv

def shade_coal(cv, bank, floor_tile, t):
    """Orilla de la veta: suelo con terrones de carbón sueltos junto al borde; el carbón junto a la orilla de arriba,
    en sombra, y el de abajo con el canto claro."""
    for (x, y) in bank:
        c = floor_tile.px[(x, y)]
        near = any((x + dx, y + dy) not in bank and 0 <= x + dx < 16 and 0 <= y + dy < 16 for dx, dy in N4)
        if near and (x * 3 + y) % 4 == 0: c = COAL[2]                        # terrones sueltos
        elif near: c = darker(c, .8)
        cv.px[(x, y)] = c
    for y in range(16):
        for x in range(16):
            if (x, y) in bank: continue
            if (x, y - 1) in bank or (x - 1, y) in bank: cv.px[(x, y)] = COAL[0]
            elif (x, y + 1) in bank: cv.px[(x, y)] = COAL[3]

def coal_tiles(t):
    sd = TS.SEED[t['key']]; base = TS.floor_base(t, sd); surf = coal_surface(sd + 70)
    tiles = []
    for m in range(16):
        cv = copy_canvas(surf, Canvas); shade_coal(cv, TS.bank_set(m), base, t); tiles.append(cv)
    corners = []
    for name in ('NE', 'NO', 'SE', 'SO'):
        bank = TS.bank_set(0, name.replace('O', 'W'))
        full = copy_canvas(surf, Canvas); shade_coal(full, bank, base, t)
        cv = Canvas(16, 16)
        near = {(x + dx, y + dy) for (x, y) in bank for dx in range(-2, 3) for dy in range(-2, 3)}
        for q in near:
            if q in full.px and full.px[q] != surf.px.get(q): cv.px[q] = full.px[q]
        for q in bank: cv.px[q] = full.px[q]
        corners.append(cv)
    return tiles, corners

# --- capas de estado (una por casilla de veta; 16 de ancho, las llamas suben por encima) -------------
INNER = lambda x, y: 2 <= x < 14 and 2 <= y < 14                          # dentro de la casilla (no pisa la orilla)

def coal_ignite(f):
    """Prende (6 cuadros, 16×32: la casilla es la mitad de abajo): grietas que se encienden desde el centro y las
    primeras llamas bajas."""
    cv = Canvas(16, 32); rnd = random.Random(3)
    r = 1.5 + f * 1.4
    for y in range(16):
        for x in range(16):
            if not INNER(x, y): continue
            d = math.hypot(x + .5 - 8, (y + .5 - 8) * 1.2)
            if d < r and (x * 5 + y * 3) % 7 == 0: cv.put(x, 16 + y, EMB[2] if d < r - 2 else EMB[1])
    for k in range(f):
        x = 8 + rnd.randint(-int(r), int(r)); x = max(3, min(12, x))
        flame(cv, x, 16 + 9 + rnd.randint(-2, 2), random.Random(f * 7 + k), h=2 + f // 2)
    return cv

def coal_burn(f, seed=0):
    """Arde fuerte (6 cuadros en bucle, 16×32; la casilla es la mitad de abajo): lenguas de alturas distintas que
    se rizan y pasan de la casilla hacia arriba (muro de fuego), grietas incandescentes en la base y chispas.
    Hay 3 variantes (seed) para que casillas vecinas no repitan las mismas llamas."""
    cv = Canvas(16, 32); rnd = random.Random(seed * 101 + f * 13 + 1)
    for y in range(16):                                                     # base: grietas al rojo blanco
        for x in range(16):
            if INNER(x, y) and ((x * 3 + y * 5 + seed) % 6 == 0 or (x - y + seed) % 7 == 0):
                cv.put(x, 16 + y, FIREC[4] if (x + f) % 3 == 0 else FIREC[3])
            elif INNER(x, y) and (x + y) % 3 == 0: cv.put(x, 16 + y, FIREC[1])
    tongues = [(random.Random(seed * 7 + k).uniform(1, 13), random.Random(seed * 11 + k).uniform(.6, 1.4), k) for k in range(5)]
    for (x0, hm, k) in tongues:
        h = int((7 + 8 * abs(math.sin(f * 1.05 + k * 2.1 + seed))) * hm)
        base = 16 + 8 + (k * 3 + seed) % 6
        for j in range(h):
            fr = j / max(1, h)
            w = 3 if fr < .3 else 2 if fr < .65 else 1
            xx = x0 + math.sin(f * 1.1 + k * 1.7 + j * .4) * fr * 2.2
            c = FIREC[4] if fr < .12 else FIREC[3] if fr < .38 else FIREC[2] if fr < .72 else FIREC[1]
            for dx in range(w):
                if 1 <= xx + dx - w // 2 < 15 and base - j >= 1: cv.put(xx + dx - w // 2, base - j, c)
    for _ in range(2):
        cv.put(rnd.randrange(2, 14), rnd.randrange(2, 12), FIREC[3])
    return cv

def coal_embers(f):
    """Brasas (6 cuadros en bucle, 16×16): el carbón agrietado brilla y late en naranja; ya se puede cruzar."""
    cv = Canvas(16, 16)
    for y in range(16):
        for x in range(16):
            if not INNER(x, y): continue
            crack = (x * 3 + y * 5) % 6 == 0 or (x - y) % 7 == 0
            if crack:
                ph = math.sin(f / 6 * 2 * math.pi + x * .7 + y * .4)
                cv.put(x, y, EMB[3] if ph > .55 else EMB[2] if ph > -.2 else EMB[1])
            elif (x + y) % 2 == 0: cv.put(x, y, EMB[0][:3] + (120,))
    if f in (1, 4):
        cv.put(6 + f, 3, FIREC[3])                                          # una chispa que salta
    return cv

def coal_out():
    """Apagada (16×16): carbón ceniciento con grietas grises; queda para siempre."""
    cv = Canvas(16, 16); ash = [C(c) for c in ('#4a4346', '#6d6466', '#958a88')]
    for y in range(16):
        for x in range(16):
            if not INNER(x, y): continue
            if (x * 3 + y * 5) % 6 == 0 or (x - y) % 7 == 0: cv.put(x, y, ash[1])
            elif (x * 7 + y * 3) % 5 == 0: cv.put(x, y, ash[0][:3] + (170,))
    cv.put(5, 6, ash[2]); cv.put(10, 11, ash[2])
    return cv

# =================================================================================================
# Bosque en humo
# =================================================================================================
def tnoise(seed, period, cell):
    """Ruido de valor que se repite cada `period` píxeles (cell divide a period)."""
    n = period // cell; rnd = random.Random(seed)
    g = [[rnd.random() for _ in range(n)] for _ in range(n)]
    def f(x, y):
        fx, fy = x / cell, y / cell
        x0, y0 = int(math.floor(fx)), int(math.floor(fy)); tx, ty = fx - x0, fy - y0
        tx, ty = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
        a = g[y0 % n][x0 % n] * (1 - tx) + g[y0 % n][(x0 + 1) % n] * tx
        b = g[(y0 + 1) % n][x0 % n] * (1 - tx) + g[(y0 + 1) % n][(x0 + 1) % n] * tx
        return a * (1 - ty) + b * ty
    return f

SMOKE = [(58, 56, 48), (84, 80, 66), (112, 106, 88), (140, 132, 110)]

def smoke_layer(seed, size=128, dens=1.0, base=.55):
    """Capa de humo de size×size que se repite sin costura. Cubre toda la sala (una base densa, `base`) y encima
    volutas más claras y más opacas; 4 tonos pardo grisáceos en escalones con trama Bayer solo en los cambios de
    tono (pixel art, sin degradado suave)."""
    n1, n2, n3 = tnoise(seed, size, 32), tnoise(seed + 1, size, 16), tnoise(seed + 2, size, 8)
    cv = Canvas(size, size)
    for y in range(size):
        for x in range(size):
            v = n1(x, y) * .55 + n2(x, y) * .3 + n3(x, y) * .15
            v = base + (v - .5) * 1.6 * dens                                 # 0..1 aprox.
            lvl = max(1, min(4, dither(max(0, v) * 4.2, x, y)))
            col = SMOKE[lvl - 1]
            cv.put(x, y, col + ((150, 180, 205, 225)[lvl - 1] if dens >= 1 else (70, 100, 130, 160)[lvl - 1],))
    return cv

def vision_mask(size=160):
    """Máscara del claro alrededor del jugador (size×size): transparente en el centro y opaca fuera, con 4 escalones
    de trama Bayer (borde pixelado, no un degradado). El código la usa para recortar las capas de humo (o la pinta
    encima en negro translúcido)."""
    cv = Canvas(size, size); c = size / 2
    for y in range(size):
        for x in range(size):
            d = math.hypot(x + .5 - c, y + .5 - c) / c                      # 0 centro, 1 borde
            v = max(0, min(1, (d - .45) / .5))
            if v <= 0: continue
            lvl = dither(v * 4, x, y)
            if lvl > 0: cv.put(x, y, (255, 255, 255, min(255, lvl * 64)))
    return cv

def smoke_wisp(f):
    """Voluta suelta que nace, se estira hacia la derecha y se deshace (8 cuadros, 48×24, translúcida)."""
    cv = Canvas(48, 24); ph = f / 8
    a = math.sin(ph * math.pi)
    for y in range(24):
        for x in range(48):
            u = x / 48
            cy = 12 + math.sin(u * 5 + ph * 4) * 3
            w = 5 * math.sin(u * math.pi) * (.6 + .4 * a)
            d = abs(y + .5 - cy) / max(.1, w)
            if d < 1 and u < .2 + ph * .9:
                lvl = dither((1 - d) * 3 * a, x, y)
                if lvl > 0: cv.put(x, y, SMOKE[min(3, lvl)] + ((70, 110, 140)[min(2, lvl - 1)],))
    return cv

def floating_embers(f, seed=5):
    """Ascuas que suben despacio entre el humo (8 cuadros en bucle, 32×48): puntos naranjas que titilan y se
    mecen. Sin contorno."""
    rnd = random.Random(seed)
    motes = [(rnd.uniform(3, 29), rnd.uniform(0, 1), rnd.random() < .35) for _ in range(6)]
    cv = Canvas(32, 48)
    for (x, ph, big) in motes:
        tt = (f / 8 + ph) % 1
        y = 46 - tt * 44; xx = x + math.sin(tt * 6.28 + ph * 5) * 2
        if tt > .9: continue
        c = EMB[3] if (f + int(ph * 8)) % 3 else EMB[2]
        cv.put(xx, y, c)
        if big: cv.put(xx, y + 1, EMB[1])
    return cv

# =================================================================================================
# Salida y maquetas
# =================================================================================================
def floor_bg(key, seed=4):
    floor = [Image.open(os.path.join(ENV, key, 'tiles', f'Floor_{k}.png')).convert('RGBA') for k in range(6)]
    bg = Image.new('RGBA', (480, 272)); rnd = random.Random(seed)
    for y in range(0, 272, 16):
        for x in range(0, 480, 16): bg.alpha_composite(floor[0 if rnd.random() < .66 else rnd.randint(1, 5)], (x, y))
    return bg

def seam_cells():
    """Veta de 2 casillas de ancho que baja serpenteando por la sala de muestra (continua: al desplazarse, la fila
    cubre de la columna anterior a la nueva)."""
    cells = set(); prev = None
    for j in range(17):
        c = 14 + round(2.2 * math.sin(j * .55))
        lo, hi = (min(prev, c), max(prev, c)) if prev is not None else (c, c)
        cells |= {(i, j) for i in range(lo, hi + 2)}
        prev = c
    return cells

def coal_scene(key, T, f):
    bg = floor_bg(key); cells = seam_cells()
    for (i, j) in sorted(cells):
        m = sum(b for (dx, dy, b) in ((0, -1, 1), (1, 0, 2), (0, 1, 4), (-1, 0, 8)) if (i + dx, j + dy) not in cells)
        bg.alpha_composite(T['tiles'][m].img(), (i * 16, j * 16))
        for (dx, dy, k) in ((1, -1, 0), (-1, -1, 1), (1, 1, 2), (-1, 1, 3)):
            if (i + dx, j) in cells and (i, j + dy) in cells and (i + dx, j + dy) not in cells:
                bg.alpha_composite(T['corners'][k].img(), (i * 16, j * 16))
    # estados repartidos a lo largo: arriba apagada, brasas, arde, prende, intacta abajo (lo que se ve en el tiempo)
    for (i, j) in sorted(cells, key=lambda c: c[1]):
        if j < 3: bg.alpha_composite(T['out'].img(), (i * 16, j * 16))
        elif j < 6: bg.alpha_composite(T['embers'][(f + i) % 6].img(), (i * 16, j * 16))
        elif j < 10: bg.alpha_composite(T['burn'][(i + j) % 3][(f + i) % 6].img(), (i * 16, j * 16 - 16))
        elif j < 12: bg.alpha_composite(T['ignite'][min(5, f)].img(), (i * 16, j * 16 - 16))
    return bg

def smoke_scene(key, S, f):
    bg = floor_bg(key, 9)
    ene = Image.open(os.path.join(TEMP, 'floor3-enemies', 'Beetle', 'Beetle_Idle.png')).convert('RGBA').crop((0, 0, 32, 32))
    for (x, y) in ((100, 60), (360, 190), (300, 80), (150, 200)): bg.alpha_composite(ene, (x, y))
    fr = os.path.join(TEMP, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')
    if os.path.exists(fr):
        im = Image.open(fr).convert('RGBA'); bg.alpha_composite(im.crop((0, 0, im.height, im.height)), (224, 120))
    smoke = Image.new('RGBA', bg.size)
    for layer, sp in ((S['A'], 1), (S['B'], 2)):
        im = layer.img()
        for y in range(-128, 272 + 128, 128):
            for x in range(-128, 480 + 128, 128): smoke.alpha_composite(im, (x + f * sp * 2 % 128, y + f * sp % 128)) if 0 <= x + f * sp * 2 % 128 < 480 + 128 else None
    mask = S['mask'].img().getchannel('A')
    hole = Image.new('L', bg.size, 255); hole.paste(mask, (240 - 80, 136 - 80))
    a = smoke.getchannel('A'); from PIL import ImageChops
    smoke.putalpha(ImageChops.multiply(a, hole))
    bg.alpha_composite(smoke)
    for (x, y) in ((60, 100), (400, 60), (320, 200), (130, 30)): bg.alpha_composite(S['embers'][(f + x) % 8].img(), (x, y))
    bg.alpha_composite(S['wisp'][f % 8].img(), (40, 210)); bg.alpha_composite(S['wisp'][(f + 4) % 8].img(), (380, 120))
    return bg

if __name__ == '__main__':
    states = {'ignite': [coal_ignite(f) for f in range(6)], 'burn': [[coal_burn(f, v) for f in range(6)] for v in range(3)],
              'embers': [coal_embers(f) for f in range(6)], 'out': coal_out()}
    sd = os.path.join(HERE, 'coal'); os.makedirs(sd, exist_ok=True)
    save_sheet(os.path.join(sd, 'Coal_Ignite.png'), states['ignite']); [save_sheet(os.path.join(sd, f'Coal_Burn_{v}.png'), states['burn'][v]) for v in range(3)]
    save_sheet(os.path.join(sd, 'Coal_Embers.png'), states['embers']); save_sheet(os.path.join(sd, 'Coal_Out.png'), [states['out']])
    os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True)
    for key in ('Swamp', 'Smoke', 'Burnt'):
        t = tram(key); tiles, corners = coal_tiles(t)
        d = os.path.join(sd, key); os.makedirs(d, exist_ok=True)
        for m, cv in enumerate(tiles): cv.img().save(os.path.join(d, f'Coal_{m}.png'))
        for name, cv in zip(('NE', 'NO', 'SE', 'SO'), corners): cv.img().save(os.path.join(d, f'Coal_Corner_{name}.png'))
        T = dict(states, tiles=tiles, corners=corners)
        frames = [coal_scene(key, T, f).resize((960, 544), Image.NEAREST) for f in range(6)]
        frames[0].save(os.path.join(HERE, f'maqueta_veta_{key}.png'))
        if key == 'Swamp':
            frames[0].save(os.path.join(HERE, 'gif', 'escena_veta.gif'), save_all=True, append_images=frames[1:], duration=110, loop=0, disposal=2)
    S = {'A': smoke_layer(11, 128, 1.0), 'B': smoke_layer(23, 128, .8, base=.35), 'mask': vision_mask(160),
         'wisp': [smoke_wisp(f) for f in range(8)], 'embers': [floating_embers(f) for f in range(8)]}
    md = os.path.join(HERE, 'smoke'); os.makedirs(md, exist_ok=True)
    S['A'].img().save(os.path.join(md, 'Fx_SmokeLayer_A.png')); S['B'].img().save(os.path.join(md, 'Fx_SmokeLayer_B.png'))
    S['mask'].img().save(os.path.join(md, 'UI_SmokeVisionMask.png'))
    save_sheet(os.path.join(md, 'Fx_SmokeWisp.png'), S['wisp']); save_sheet(os.path.join(md, 'Fx_FloatingEmbers.png'), S['embers'])
    frames = [smoke_scene('Smoke', S, f).resize((960, 544), Image.NEAREST) for f in range(8)]
    frames[0].save(os.path.join(HERE, 'maqueta_humo.png'))
    frames[0].save(os.path.join(HERE, 'gif', 'escena_humo.gif'), save_all=True, append_images=frames[1:], duration=140, loop=0, disposal=2)
    print('Listo en', HERE)
