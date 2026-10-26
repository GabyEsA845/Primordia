"""Metamorfosis al pasar de piso (Primordia): la criatura cae por la fosa, brilla y se transforma.

- Morph_<Cuerpo>.png (12 × 32×32): silueta blanca que pasa de la célula al cuerpo nuevo, interpolando los campos de
  distancia de las dos siluetas (cambio de forma suave, no un salto de dibujo).
- Reveal_<Cuerpo>.png (6 × 32×32): de la silueta blanca a los colores del cuerpo (Idle_side, cuadro 0).
- Metamorphosis_Aura.png (4 × 64×64) y Metamorphosis_Burst.png (6 × 96×96): luz en blanco; el juego la tiñe del color
  de la familia de la Boca elegida.
- Abyss_Background.png (480×272), Abyss_Rays.png (128×272) y Abyss_Mote.png (2 × 4×4): el abismo por el que se cae.

Uso: python generar_metamorfosis.py   (lee ../player y ../floor2-bodies; salida junto a este script)
"""
import math, os, random
import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
TEMP = os.path.normpath(os.path.join(HERE, '..'))
N = 32
BODIES = {'Jellyfish': 'Jellyfish', 'Placoderm': 'Placoderm', 'Crinoid': 'Crinoid'}
OUTL = (24, 20, 37, 255)

def C(h, a=255): h = h.lstrip('#'); return tuple(int(h[i:i+2], 16) for i in (0, 2, 4)) + (a,)

def frame(sheet, index, size):
    im = Image.open(sheet).convert('RGBA')
    return im.crop((index * size, 0, index * size + size, size))

def centered(im):
    canvas = Image.new('RGBA', (N, N), (0, 0, 0, 0))
    canvas.alpha_composite(im, ((N - im.width) // 2, (N - im.height) // 2))
    return canvas

def mask(im):
    return np.array(im)[:, :, 3] > 0

def sdf(m):
    """Distancia con signo: negativa dentro, positiva fuera."""
    return ndimage.distance_transform_edt(~m) - ndimage.distance_transform_edt(m)

def silhouette(m, rim):
    """Silueta blanca con un borde algo más cálido y el contorno oscuro de siempre."""
    out = np.zeros((N, N, 4), np.uint8)
    out[m] = (255, 255, 255, 255)
    inner = ndimage.binary_erosion(m)
    edge = m & ~inner
    out[edge] = rim
    outline = ndimage.binary_dilation(m) & ~m
    out[outline] = OUTL
    return Image.fromarray(out, 'RGBA')

cell = centered(frame(f'{TEMP}/player/Player_Idle_side.png', 0, 16))
cell_mask = mask(cell)
results = {}
for name, folder in BODIES.items():
    body = centered(frame(f'{TEMP}/floor2-bodies/{folder}/Player_{name}_Idle_side.png', 0, 24))
    # Sin el contorno oscuro: la silueta es lo de dentro, y el contorno se vuelve a poner en cada cuadro.
    arr = np.array(body)
    body_mask = (arr[:, :, 3] > 0) & ~np.all(arr[:, :, :3] == OUTL[:3], axis=2)
    # Los ojos y los contornos de dentro dejarían agujeros: la silueta es la forma entera.
    cell_inner = ndimage.binary_fill_holes(cell_mask & ~np.all(np.array(cell)[:, :, :3] == OUTL[:3], axis=2))
    a, b = sdf(cell_inner), sdf(ndimage.binary_fill_holes(body_mask))

    # La forma solo cambia de verdad en una parte del recorrido: se eligen los 12 cuadros repartidos por cuánto se
    # parece ya cada silueta a la final, para que el cambio avance parejo.
    start, end = a < 0, b < 0
    total = max(1, np.count_nonzero(start != end))
    steps = [((1 - t) * a + t * b) < 0 for t in np.linspace(0, 1, 400)]
    progress = [np.count_nonzero(m != start) / total for m in steps]
    morph = []
    for k in range(12):
        target = k / 11
        m = min(zip(progress, range(len(steps))), key=lambda p: abs(p[0] - target))[1]
        morph.append(silhouette(steps[m], C('#fff2d8')))
    reveal = []
    for k in range(6):
        t = (k + 1) / 6
        out = Image.new('RGBA', (N, N), (0, 0, 0, 0))
        for y in range(N):
            for x in range(N):
                r, g, bl, al = body.getpixel((x, y))
                if al == 0:
                    continue
                w = 1 - t
                out.putpixel((x, y), (round(r + (255 - r) * w), round(g + (255 - g) * w), round(bl + (255 - bl) * w), 255))
        reveal.append(out)
    results[name] = (morph, reveal)

def sheet(frames):
    w, h = frames[0].size
    s = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames): s.paste(f, (i * w, 0))
    return s

for name, (morph, reveal) in results.items():
    sheet(morph).save(f'{HERE}/Morph_{name}.png')
    sheet(reveal).save(f'{HERE}/Reveal_{name}.png')

# --- aura: halo suave con rayos que giran (blanco; el juego lo tiñe) ---
def aura(k):
    n = 64; im = Image.new('RGBA', (n, n), (0, 0, 0, 0))
    for y in range(n):
        for x in range(n):
            dx, dy = x + .5 - n / 2, y + .5 - n / 2
            d = math.hypot(dx, dy) / (n / 2)
            if d >= 1: continue
            ang = math.atan2(dy, dx) + k * math.pi / 16
            ray = max(0.0, math.cos(ang * 6)) ** 6 * (1 - d) * 0.9
            glow = (1 - d) ** 2.2
            a = min(1.0, glow * 0.9 + ray)
            # Escalones de transparencia, como pixel art.
            a = round(a * 6) / 6
            if a > 0: im.putpixel((x, y), (255, 255, 255, int(a * 255)))
    return im

# --- estallido: anillo que se abre con chispas ---
def burst(k):
    n = 96; im = Image.new('RGBA', (n, n), (0, 0, 0, 0))
    r = 6 + k * 7.5; width = 3.5 - k * 0.4; fade = 1 - k / 6
    rnd = random.Random(k)
    for y in range(n):
        for x in range(n):
            d = math.hypot(x + .5 - n / 2, y + .5 - n / 2)
            if abs(d - r) <= width:
                im.putpixel((x, y), (255, 255, 255, int(255 * fade)))
            elif d < r - width and k < 2:
                im.putpixel((x, y), (255, 255, 255, int(200 * (1 - k / 2))))
    for _ in range(14):
        ang = rnd.uniform(0, 2 * math.pi); rr = r + rnd.uniform(2, 10)
        x, y = int(n / 2 + math.cos(ang) * rr), int(n / 2 + math.sin(ang) * rr)
        if 0 <= x < n and 0 <= y < n:
            im.putpixel((x, y), (255, 255, 255, int(255 * fade)))
    return im

sheet([aura(k) for k in range(4)]).save(f'{HERE}/Metamorphosis_Aura.png')
sheet([burst(k) for k in range(6)]).save(f'{HERE}/Metamorphosis_Burst.png')

# --- el abismo: degradado de azul profundo con motas, rayos de luz desde arriba ---
W, H = 480, 272
bg = Image.new('RGBA', (W, H))
top, bottom = np.array(C('#0e2a4c')[:3], float), np.array(C('#02060e')[:3], float)
rnd = random.Random(7)
px = bg.load()
for y in range(H):
    t = (y / (H - 1)) ** 0.8
    base = top * (1 - t) + bottom * t
    for x in range(W):
        # Bandas de 4 px para que el degradado se vea en escalones de pixel art.
        q = (y // 4 * 4) / (H - 1)
        c = top * (1 - q ** 0.8) + bottom * q ** 0.8
        px[x, y] = tuple(int(v) for v in c) + (255,)
for _ in range(260):
    x, y = rnd.randrange(W), rnd.randrange(H)
    c = C('#3a6e8a') if rnd.random() < 0.7 else C('#5a94b0')
    px[x, y] = c
bg.save(f'{HERE}/Abyss_Background.png')

rays = Image.new('RGBA', (128, 272), (0, 0, 0, 0))
for y in range(272):
    for x in range(128):
        a = 0.0
        for cx, w, s in ((30, 10, 0.18), (70, 16, 0.22), (104, 8, 0.15)):
            sx = cx + (y * 0.12)
            a += s * max(0.0, 1 - abs(x - sx) / w)
        a *= (1 - y / 272) ** 1.5
        a = round(a * 8) / 8
        if a > 0:
            rays.putpixel((x, y), (200, 235, 255, int(min(1, a) * 255)))
rays.save(f'{HERE}/Abyss_Rays.png')

motes = []
for k in range(2):
    m = Image.new('RGBA', (4, 4), (0, 0, 0, 0))
    m.putpixel((1, 1), C('#a8dcec')); m.putpixel((2, 1), C('#a8dcec', 160 if k else 255))
    if k == 0: m.putpixel((1, 2), C('#a8dcec', 160))
    motes.append(m)
sheet(motes).save(f'{HERE}/Abyss_Mote.png')

# --- vista previa: la secuencia de cada cuerpo sobre el abismo, con el aura teñida de su familia ---
FAMILY = {'Jellyfish': (255, 138, 216), 'Placoderm': (255, 154, 74), 'Crinoid': (140, 230, 120)}
S = 4
pv = Image.new("RGBA", ((12 + 6) * 40 * S // 4 + 40, len(BODIES) * 80 + 20), C('#02060e'))
row = 0
for name, (morph, reveal) in results.items():
    x = 10
    tint = FAMILY[name]
    a = aura(1)
    ta = Image.new('RGBA', a.size, (0, 0, 0, 0))
    arr = np.array(a); arr[:, :, 0] = tint[0]; arr[:, :, 1] = tint[1]; arr[:, :, 2] = tint[2]
    ta = Image.fromarray(arr, 'RGBA')
    seq = [cell] + morph + reveal
    for f in seq:
        cell_bg = Image.new('RGBA', (64, 64), C('#0e2a4c'))
        cell_bg.alpha_composite(ta)
        cell_bg.alpha_composite(f, (16, 16))
        pv.alpha_composite(cell_bg.resize((40 * S // 2, 40 * S // 2), Image.NEAREST) if False else cell_bg.resize((40, 40), Image.NEAREST), (x, 10 + row * 80))
        x += 42
    row += 1
pv.save(f'{HERE}/vista_previa_metamorfosis.png')

# GIF de cada secuencia, ampliado
os.makedirs(f'{HERE}/gif', exist_ok=True)
for name, (morph, reveal) in results.items():
    tint = FAMILY[name]
    frames = []
    seq = [cell] * 3 + morph + reveal + [reveal[-1]] * 4
    for i, f in enumerate(seq):
        a = aura(i % 4)
        arr = np.array(a); arr[:, :, 0] = tint[0]; arr[:, :, 1] = tint[1]; arr[:, :, 2] = tint[2]
        strength = 1.0 if 3 <= i < 3 + 12 else 0.4
        arr[:, :, 3] = (arr[:, :, 3] * strength).astype(np.uint8)
        b = Image.new('RGBA', (64, 64), C('#0e2a4c'))
        b.alpha_composite(Image.fromarray(arr, 'RGBA'))
        b.alpha_composite(f, (16, 16))
        frames.append(b.resize((256, 256), Image.NEAREST).convert('RGB'))
    frames[0].save(f'{HERE}/gif/Metamorphosis_{name}.gif', save_all=True, append_images=frames[1:], duration=120, loop=0)
print('Listo')
