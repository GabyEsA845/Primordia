"""Sala de cambio (el laboratorio de Darwin) de Primordia: la cápsula y la metamorfosis corta dentro de ella.

- Capsule_Idle.png (6 × 40×48, bucle): cápsula de vidrio con líquido verde y burbujas, base y tapa de latón.
- Capsule_Glow.png (4 × 40×48, bucle): brillo del líquido en blanco; el juego lo tiñe del color de la familia.
- Capsule_Flash.png (5 × 40×48): destello al salir la criatura nueva, en blanco (también teñido).
- Capsule_Used.png (4 × 40×48, bucle lento): ya usada en este piso: el líquido baja y se apaga.
- Morph_<A>_<B>.png (12 × 32×32): silueta blanca que pasa del cuerpo A al B (campos de distancia interpolados, como
  ../metamorphosis/generar_metamorfosis.py). La revelación con colores reutiliza ../metamorphosis/Reveal_<B>.png.

Uso: python generar_sala_cambio.py   (lee ../floor2-bodies; salida junto a este script)
"""
import math, os
import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
TEMP = os.path.normpath(os.path.join(HERE, '..'))
OUTL = (24, 20, 37, 255)
W, H = 40, 48

def C(h, a=255):
    h = h.lstrip('#'); return tuple(int(h[i:i+2], 16) for i in (0, 2, 4)) + (a,)

BRASS = [C('#3a2a1a'), C('#7a5a2a'), C('#c09040'), C('#f0d080')]
LIQUID = [C('#1f6a4a'), C('#2f9a6a'), C('#5fd09a'), C('#a8f5cf')]
GLASS = C('#bfe8f0', 170)
GLASS_HI = C('#ffffff', 200)

# Geometría (coordenadas del lienzo de 40×48; el apoyo es abajo en el centro, (20, 47)). El interior del vidrio mide
# 28 px de ancho, para que quepa la criatura (24 px) durante la metamorfosis.
TUBE_L, TUBE_R = 5, 34          # paredes del vidrio (incluidas)
TUBE_TOP, TUBE_BOT = 9, 38      # de la tapa a la base
DOME = 5                        # radio de la cúpula de arriba

def inside_tube(x, y):
    """Interior del vidrio (sin las paredes)."""
    if not (TUBE_L < x < TUBE_R and TUBE_TOP < y < TUBE_BOT):
        return False
    cy = TUBE_TOP + DOME
    if y < cy:   # esquinas redondeadas arriba
        for cx in (TUBE_L + DOME, TUBE_R - DOME):
            if (x < TUBE_L + DOME and cx == TUBE_L + DOME) or (x > TUBE_R - DOME and cx == TUBE_R - DOME):
                if math.hypot(x - cx, y - cy) > DOME - 0.5:
                    return False
    return True

def wall(x, y):
    """Paredes del vidrio: el borde del interior."""
    if inside_tube(x, y):
        return False
    return any(inside_tube(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))

def put(im, x, y, c):
    if 0 <= x < W and 0 <= y < H and c[3] > 0:
        im.putpixel((x, y), c)

def outline(im):
    px = im.load()
    add = []
    for y in range(H):
        for x in range(W):
            if px[x, y][3] == 0 and any(0 <= x + dx < W and 0 <= y + dy < H and px[x + dx, y + dy][3] > 0
                                        and px[x + dx, y + dy] != OUTL for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                add.append((x, y))
    for p in add:
        px[p] = OUTL

def brass(im, x0, y0, x1, y1):
    """Bloque de latón con luz arriba a la izquierda y remaches."""
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            t = (y - y0) / max(1, y1 - y0)
            k = 3 if y == y0 else 2 if t < 0.35 else 1 if t < 0.8 else 0
            if x == x0 and k > 0:
                k = min(3, k + 1)
            if x == x1:
                k = max(0, k - 1)
            put(im, x, y, BRASS[k])

def liquid_level(level):
    return TUBE_BOT - 1 - level

def capsule(frame, level=26, bright=1.0, bubbles=True, dull=False):
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    top = liquid_level(level)
    ramp = [C('#18392c'), C('#245a44'), C('#357a5c'), C('#5aa883')] if dull else LIQUID
    # Líquido, con luz de la izquierda y un leve vaivén en la superficie.
    for y in range(TUBE_TOP, TUBE_BOT):
        for x in range(TUBE_L, TUBE_R + 1):
            if not inside_tube(x, y):
                continue
            surf = top + (1 if (x + frame) % 6 < 3 else 0)
            if y < surf:
                # Hueco de aire: vidrio vacío, apenas tintado.
                put(im, x, y, C('#9fd8e0', 40))
                continue
            u = (x - TUBE_L) / (TUBE_R - TUBE_L)
            k = 2 if u < 0.3 else 1 if u < 0.8 else 0
            if y == surf:
                k = 3
            if ((x * 7 + y * 3 + frame) % 11 == 0) and k < 3 and not dull:
                k += 1
            put(im, x, y, ramp[k])
    # Burbujas que suben.
    if bubbles:
        for i, (bx, speed, phase) in enumerate(((10, 2, 0), (18, 3, 5), (27, 2, 11), (14, 1, 17), (24, 3, 23), (30, 1, 8))):
            span = TUBE_BOT - 2 - (top + 1)
            if span <= 2:
                continue
            by = TUBE_BOT - 2 - ((phase + frame * speed) % span)
            r = 1 if i % 2 == 0 else 0
            for dy in range(-r, r + 1):
                for dx in range(-r, r + 1):
                    if abs(dx) + abs(dy) <= r:
                        put(im, bx + dx, by + dy, C('#e8fff4', 220 if not dull else 120))
            if r:
                put(im, bx - 1, by - 1, C('#ffffff'))
    # Vidrio: paredes y reflejos.
    for y in range(H):
        for x in range(W):
            if wall(x, y):
                put(im, x, y, GLASS)
    for y in range(TUBE_TOP + 3, TUBE_BOT - 3):
        put(im, TUBE_L + 2, y, GLASS_HI if (y // 3) % 3 else C('#ffffff', 120))
        if y % 4 < 2:
            put(im, TUBE_L + 3, y, C('#ffffff', 90))
    put(im, TUBE_R - 2, TUBE_TOP + 4, C('#ffffff', 160))
    put(im, TUBE_R - 2, TUBE_TOP + 5, C('#ffffff', 110))
    # Tapa de latón con su tubo, y base con un indicador.
    brass(im, TUBE_L - 1, TUBE_TOP - 3, TUBE_R + 1, TUBE_TOP - 1)
    brass(im, 17, TUBE_TOP - 6, 22, TUBE_TOP - 4)
    brass(im, 19, 0, 20, TUBE_TOP - 7)
    brass(im, TUBE_L - 2, TUBE_BOT, TUBE_R + 2, TUBE_BOT + 2)
    brass(im, TUBE_L, TUBE_BOT + 3, TUBE_R, H - 2)
    for x in (TUBE_L + 1, TUBE_R - 1):
        put(im, x, TUBE_BOT + 5, BRASS[3])
    # Indicador: verde vivo o apagado.
    gauge = C('#7cffb0') if not dull else C('#4a5a52')
    for x in range(17, 23):
        put(im, x, TUBE_BOT + 5, gauge if (x + frame) % 4 != 0 or dull else C('#e8fff4'))
    for x in range(17, 23):
        put(im, x, TUBE_BOT + 6, C('#10261c'))
    outline(im)
    return im

def glow(frame):
    """Brillo del líquido en blanco, para teñir: pulsa y deja rayos que suben."""
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    pulse = 0.75 + 0.25 * math.sin(frame / 4 * 2 * math.pi)
    for y in range(TUBE_TOP, TUBE_BOT):
        for x in range(TUBE_L, TUBE_R + 1):
            if not inside_tube(x, y):
                continue
            ray = (x + frame * 2) % 5 == 0
            a = int((120 + (90 if ray else 0)) * pulse)
            put(im, x, y, (255, 255, 255, min(255, a)))
    # Halo que se sale del vidrio.
    for y in range(TUBE_TOP - 2, TUBE_BOT + 1):
        for x in (TUBE_L - 1, TUBE_R + 1):
            put(im, x, y, (255, 255, 255, int(110 * pulse)))
    return im

def flash(frame):
    """Destello al abrirse: llena el vidrio, se sale como un aro y se apaga."""
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    a = [255, 220, 160, 100, 40][frame]
    fill = [170, 120, 70, 30, 0][frame]
    for y in range(TUBE_TOP, TUBE_BOT):
        for x in range(TUBE_L, TUBE_R + 1):
            if inside_tube(x, y):
                # Rayos verticales más claros sobre un velo: brilla sin tapar del todo lo de dentro.
                put(im, x, y, (255, 255, 255, a if (x + frame) % 4 == 0 else fill))
    r = 8 + frame * 4
    cx, cy = 20, (TUBE_TOP + TUBE_BOT) // 2
    for k in range(64):
        ang = k * 2 * math.pi / 64
        x, y = int(cx + math.cos(ang) * r), int(cy + math.sin(ang) * r * 1.3)
        put(im, x, y, (255, 255, 255, a))
    for k in range(6):   # chispas
        ang = k * math.pi / 3 + frame * 0.3
        d = 4 + frame * 5
        x, y = int(cx + math.cos(ang) * d), int(cy + math.sin(ang) * d * 1.3)
        put(im, x, y, (255, 255, 255, a))
        put(im, x + 1, y, (255, 255, 255, a // 2))
    return im

def save(name, frames):
    w, h = frames[0].size
    sheet = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        sheet.paste(f, (i * w, 0))
    sheet.save(f'{HERE}/{name}.png')
    return sheet

idle = [capsule(k) for k in range(6)]
used = [capsule(k, level=12, bubbles=k % 2 == 0, dull=True) for k in range(4)]
glows = [glow(k) for k in range(4)]
flashes = [flash(k) for k in range(5)]
save('Capsule_Idle', idle)
save('Capsule_Used', used)
save('Capsule_Glow', glows)
save('Capsule_Flash', flashes)

# --- metamorfosis de un cuerpo a otro --------------------------------------------------------
N = 32
BODIES = ['Jellyfish', 'Placoderm', 'Crinoid']

def body_mask(name):
    im = Image.open(f'{TEMP}/floor2-bodies/{name}/Player_{name}_Idle_side.png').convert('RGBA').crop((0, 0, 24, 24))
    canvas = Image.new('RGBA', (N, N), (0, 0, 0, 0))
    canvas.alpha_composite(im, ((N - 24) // 2, (N - 24) // 2))
    arr = np.array(canvas)
    m = (arr[:, :, 3] > 0) & ~np.all(arr[:, :, :3] == OUTL[:3], axis=2)
    return ndimage.binary_fill_holes(m)

def sdf(m):
    return ndimage.distance_transform_edt(~m) - ndimage.distance_transform_edt(m)

def silhouette(m):
    out = np.zeros((N, N, 4), np.uint8)
    out[m] = (255, 255, 255, 255)
    edge = m & ~ndimage.binary_erosion(m)
    out[edge] = C('#fff2d8')
    out[ndimage.binary_dilation(m) & ~m] = OUTL
    return Image.fromarray(out, 'RGBA')

morphs = {}
for a in BODIES:
    for b in BODIES:
        if a == b:
            continue
        ma, mb = body_mask(a), body_mask(b)
        da, db = sdf(ma), sdf(mb)
        steps = [((1 - t) * da + t * db) < 0 for t in np.linspace(0, 1, 400)]
        total = max(1, np.count_nonzero(ma != mb))
        progress = [np.count_nonzero(m != ma) / total for m in steps]
        frames = []
        for k in range(12):
            target = k / 11
            idx = min(zip(progress, range(len(steps))), key=lambda p: abs(p[0] - target))[1]
            frames.append(silhouette(steps[idx]))
        morphs[(a, b)] = frames
        save(f'Morph_{a}_{b}', frames)

# --- vista previa ------------------------------------------------------------------------------
S = 4
BG = C('#0f2a4a')
FAMILY = {'Jellyfish': (255, 138, 216), 'Placoderm': (255, 154, 74), 'Crinoid': (140, 230, 120)}

def tint(im, rgb):
    arr = np.array(im).astype(np.float32)
    arr[:, :, 0] *= rgb[0] / 255; arr[:, :, 1] *= rgb[1] / 255; arr[:, :, 2] *= rgb[2] / 255
    return Image.fromarray(arr.astype(np.uint8), 'RGBA')

rows = [('Reposo', idle), ('Brillo (pez)', [Image.alpha_composite(idle[k % 6], tint(glows[k], FAMILY['Placoderm'])) for k in range(4)]),
        ('Destello', [Image.alpha_composite(idle[0], tint(f, FAMILY['Placoderm'])) for f in flashes]), ('Usada', used)]
pv = Image.new('RGBA', (10 + 6 * (W * S + 10) + 140, 10 + len(rows) * (H * S + 10)), BG)
from PIL import ImageDraw
d = ImageDraw.Draw(pv)
for r, (label, frames) in enumerate(rows):
    y = 10 + r * (H * S + 10)
    d.text((10, y + H * S // 2), label, fill=C('#bfe6ff'))
    for i, f in enumerate(frames):
        pv.alpha_composite(f.resize((W * S, H * S), Image.NEAREST), (140 + i * (W * S + 10), y))
pv.save(f'{HERE}/vista_previa_capsula.png')

# Secuencia de la metamorfosis en la cápsula (medusa → pez): brillo, silueta que cambia dentro, revelación, destello.
reveal = Image.open(f'{TEMP}/metamorphosis/Reveal_Placoderm.png').convert('RGBA')
reveal = [reveal.crop((k * N, 0, k * N + N, N)) for k in range(6)]
seq = []
for k in range(0, 12, 2):
    base = Image.alpha_composite(idle[k % 6], tint(glows[k % 4], FAMILY['Placoderm']))
    base.alpha_composite(morphs[('Jellyfish', 'Placoderm')][k], (4, 9))
    seq.append(base)
for k in (1, 3, 5):
    base = Image.alpha_composite(idle[k % 6], tint(glows[k % 4], FAMILY['Placoderm']))
    base.alpha_composite(reveal[k], (4, 9))
    seq.append(base)
seq.append(Image.alpha_composite(idle[0], tint(flashes[0], FAMILY['Placoderm'])))
sq = Image.new('RGBA', (10 + len(seq) * (W * 3 + 6), H * 3 + 20), BG)
for i, f in enumerate(seq):
    sq.alpha_composite(f.resize((W * 3, H * 3), Image.NEAREST), (10 + i * (W * 3 + 6), 10))
sq.save(f'{HERE}/vista_previa_secuencia.png')

# Todas las transformaciones, a ×3.
mv = Image.new('RGBA', (10 + 12 * (N * 3 + 4) + 10, 10 + len(morphs) * (N * 3 + 8)), BG)
for r, ((a, b), frames) in enumerate(morphs.items()):
    for i, f in enumerate(frames):
        mv.alpha_composite(f.resize((N * 3, N * 3), Image.NEAREST), (10 + i * (N * 3 + 4), 10 + r * (N * 3 + 8)))
mv.save(f'{HERE}/vista_previa_transformaciones.png')
print('Listo:', HERE)
