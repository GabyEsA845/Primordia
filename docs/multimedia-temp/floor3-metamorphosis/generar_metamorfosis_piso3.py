"""Metamorfosis del piso 2 al piso 3 y de la sala de cambio del piso 3.

    python generar_metamorfosis_piso3.py      (necesita Pillow, numpy y scipy)

Lee los cuerpos de ../floor2-bodies (24 px) y ../floor3-bodies (32 px) y escribe:
  Morph_<A>_<B>   9 hojas del paso (cuerpo del piso 2 -> cuerpo del piso 3) y 6 de la sala de cambio del piso 3
  Reveal_<B>      de la silueta blanca a los colores de la criatura del piso 3
  Surface_*       fondo de la subida (del abismo a la superficie del pantano), rayos y motas
Mismo método que ../metamorphosis y ../exchange-room: campos de distancia interpolados y cuadros repartidos por cuánto
cambia la forma.
"""
import math, os, random
import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
TEMP = os.path.normpath(os.path.join(HERE, '..'))
N = 32
OUTL = (24, 20, 37, 255)
FLOOR2 = ['Jellyfish', 'Placoderm', 'Crinoid']
FLOOR3 = ['Frog', 'Diplocaulus', 'Whipscorpion']
# Color de la familia de cada cuerpo (lo pone el juego; aquí solo para la vista previa).
PINK, ORANGE, LIME = (255, 138, 216), (255, 154, 74), (140, 230, 120)
FAMILY = {'Jellyfish': PINK, 'Placoderm': ORANGE, 'Crinoid': LIME,
          'Whipscorpion': PINK, 'Diplocaulus': ORANGE, 'Frog': LIME}


def C(h, a=255):
    h = h.lstrip('#')
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)) + (a,)


def body_image(name):
    if name in FLOOR2:
        size, path = 24, f'{TEMP}/floor2-bodies/{name}/Player_{name}_Idle_side.png'
    else:
        size, path = 32, f'{TEMP}/floor3-bodies/{name}/Player_{name}_Idle_side.png'
    im = Image.open(path).convert('RGBA').crop((0, 0, size, size))
    canvas = Image.new('RGBA', (N, N), (0, 0, 0, 0))
    canvas.alpha_composite(im, ((N - size) // 2, (N - size) // 2))
    return canvas


def body_mask(name):
    """La forma entera sin el contorno oscuro (ojos y contornos de dentro no dejan agujeros)."""
    arr = np.array(body_image(name))
    m = (arr[:, :, 3] > 0) & ~np.all(arr[:, :, :3] == OUTL[:3], axis=2)
    return ndimage.binary_fill_holes(m)


def sdf(m):
    return ndimage.distance_transform_edt(~m) - ndimage.distance_transform_edt(m)


def silhouette(m):
    out = np.zeros((N, N, 4), np.uint8)
    out[m] = (255, 255, 255, 255)
    out[m & ~ndimage.binary_erosion(m)] = C('#fff2d8')
    out[ndimage.binary_dilation(m) & ~m] = OUTL
    return Image.fromarray(out, 'RGBA')


def morph(a, b):
    ma, mb = body_mask(a), body_mask(b)
    da, db = sdf(ma), sdf(mb)
    steps = [((1 - t) * da + t * db) < 0 for t in np.linspace(0, 1, 400)]
    total = max(1, np.count_nonzero(ma != mb))
    progress = [np.count_nonzero(m != ma) / total for m in steps]
    frames = []
    for k in range(12):
        idx = min(range(len(steps)), key=lambda i: abs(progress[i] - k / 11))
        frames.append(silhouette(steps[idx]))
    # El primer y el último cuadro son exactamente las formas de partida y de llegada.
    frames[0], frames[-1] = silhouette(ma), silhouette(mb)
    return frames


def reveal(name):
    body = np.array(body_image(name)).astype(np.float32)
    frames = []
    for k in range(6):
        w = 1 - (k + 1) / 6
        out = body.copy()
        out[:, :, :3] = out[:, :, :3] + (255 - out[:, :, :3]) * w
        out[:, :, 3] = np.where(body[:, :, 3] > 0, 255, 0)
        frames.append(Image.fromarray(np.round(out).astype(np.uint8), 'RGBA'))
    return frames


def sheet(frames):
    w, h = frames[0].size
    s = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        s.paste(f, (i * w, 0))
    return s


morphs = {}
for a in FLOOR2:
    for b in FLOOR3:
        morphs[(a, b)] = morph(a, b)
for a in FLOOR3:
    for b in FLOOR3:
        if a != b:
            morphs[(a, b)] = morph(a, b)
reveals = {b: reveal(b) for b in FLOOR3}
for (a, b), frames in morphs.items():
    sheet(frames).save(f'{HERE}/Morph_{a}_{b}.png')
for b, frames in reveals.items():
    sheet(frames).save(f'{HERE}/Reveal_{b}.png')

# --- la subida: del abismo del piso 2 a la superficie del pantano ----------------------------------------------
# Dos pantallas de alto: el juego la desplaza hacia abajo durante la cinemática, así la criatura parece subir.
W, H = 480, 544
SURFACE = 58                       # línea del agua (el reflejo ondulado)
# Paradas del degradado de abajo arriba: abismo del piso 2 -> azul -> agua turbia del pantano -> luz verde bajo la tapa.
STOPS = [(H, C('#02060e')), (H - 120, C('#0e2a4c')), (300, C('#163a4a')), (190, C('#1d4440')),
         (110, C('#2a5e54')), (SURFACE + 6, C('#4e8a72'))]


def water(y):
    q = y // 4 * 4                 # bandas de 4 px, como en el abismo
    for (y0, c0), (y1, c1) in zip(STOPS, STOPS[1:]):
        if y1 <= q <= y0:
            t = (y0 - q) / (y0 - y1)
            return tuple(int(c0[i] + (c1[i] - c0[i]) * t) for i in range(3)) + (255,)
    return STOPS[-1][1]


rnd = random.Random(11)
bg = Image.new('RGBA', (W, H))
px = bg.load()
for y in range(H):
    c = water(y)
    for x in range(W):
        px[x, y] = c

# Por encima del agua: el pantano visto desde abajo, deformado por las ondas. Cielo verdoso con luz, troncos de
# Lepidodendron oscuros y frondas que cuelgan.
SKY = [C('#5a8a6a'), C('#76a67c'), C('#98c496'), C('#c0e0ac')]
TRUNK = [C('#1e2a26'), C('#2a3a32'), C('#3a4c3e')]
trunks = [(rnd.randrange(W), rnd.randint(9, 16)) for _ in range(7)]
for y in range(SURFACE):
    wob = math.sin(y * 0.9) * 1.5
    for x in range(W):
        sx = x + wob
        light = 1 - y / SURFACE
        k = min(3, int((light * 0.6 + 0.35 * (0.5 + 0.5 * math.sin(sx * 0.05 + y * 0.2))) * 4))
        c = SKY[k]
        for tx, tw in trunks:
            d = abs(sx - tx) - tw / 2 * (1 - y / SURFACE * 0.3)
            if d < 0:
                c = TRUNK[1] if d > -2 else TRUNK[0]
                if d > -1.2 and sx < tx:   # borde con luz a la izquierda
                    c = TRUNK[2]
        px[x, y] = c
# Frondas: abanicos de líneas que cuelgan desde arriba (colores apagados del helecho vivo, oscurecidos por el agua).
FROND = [C('#1c3a20'), C('#2a5a2a'), C('#3e6a34')]
for _ in range(16):
    cx, cy = rnd.randrange(W), rnd.randint(-4, 14)
    for leaf in range(5):
        ang = math.pi / 2 + (leaf - 2) * 0.35 + rnd.uniform(-0.1, 0.1)
        length = rnd.randint(14, 30)
        for s in range(length):
            x = int(cx + math.cos(ang) * s + math.sin(s * 0.3) * 0.5)
            y = int(cy + math.sin(ang) * s * 0.7)
            if 0 <= x < W and 0 <= y < SURFACE - 3:
                px[x, y] = FROND[min(2, s * 3 // length)]
                if s % 3 == 0 and s > 3:  # foliolos
                    for side in (-1, 1):
                        xx = x + side
                        if 0 <= xx < W:
                            px[xx, y] = FROND[0]

# La línea del agua: banda clara ondulada con brillos y hojas flotando en sombra.
for x in range(W):
    yy = SURFACE + round(math.sin(x * 0.11) * 1.6 + math.sin(x * 0.037 + 1) * 1.2)
    for d in range(-2, 4):
        y = yy + d
        if 0 <= y < H:
            px[x, y] = C('#d8f0c0') if d == 0 else (C('#9cc8a0') if d in (-1, 1) else C('#6a9a82'))
    if (x * 7) % 23 < 3:
        px[x, yy] = (255, 255, 240, 255)
for _ in range(22):            # hojas y semillas flotando: manchas oscuras sobre la línea
    lx, lw = rnd.randrange(W), rnd.randint(3, 8)
    ly = SURFACE + round(math.sin(lx * 0.11) * 1.6 + math.sin(lx * 0.037 + 1) * 1.2)
    for x in range(lx, min(W, lx + lw)):
        for y in (ly - 1, ly):
            px[x, y] = C('#24361e') if y == ly else C('#3a5028')

# Raíces que bajan desde la superficie hasta el agua turbia.
ROOT = [C('#16261e'), C('#22342a')]
for _ in range(9):
    rx, length = rnd.randrange(W), rnd.randint(40, 120)
    for branch in range(rnd.randint(2, 4)):
        x = rx + rnd.uniform(-3, 3)
        for s in range(int(length * rnd.uniform(0.5, 1))):
            y = SURFACE + 3 + s
            x += math.sin(s * 0.15 + branch * 2) * 0.35 + rnd.uniform(-0.2, 0.2)
            if 0 <= int(x) < W and y < H:
                px[int(x), y] = ROOT[0]
                if s < length * 0.4 and 0 <= int(x) + 1 < W:
                    px[int(x) + 1, y] = ROOT[1]

# Partículas en suspensión: frías y escasas en el fondo, pardas y abundantes cerca del pantano.
for _ in range(900):
    x, y = rnd.randrange(W), rnd.randrange(SURFACE + 5, H)
    near = 1 - (y - SURFACE) / (H - SURFACE)
    if rnd.random() > 0.25 + near * 0.75:
        continue
    if near > 0.55:
        c = C('#6a8a5a') if rnd.random() < 0.6 else C('#86683f')
    elif near > 0.3:
        c = C('#3e7266')
    else:
        c = C('#3a6e8a') if rnd.random() < 0.7 else C('#5a94b0')
    px[x, y] = c
bg.save(f'{HERE}/Surface_Background.png')

# Rayos de luz verdosa que entran por la superficie (se colocan pegados a ella y bajan).
RW, RH = 128, 272
rays = Image.new('RGBA', (RW, RH), (0, 0, 0, 0))
for y in range(RH):
    for x in range(RW):
        a = 0.0
        for cx, w, s in ((24, 9, 0.2), (58, 15, 0.26), (96, 7, 0.16), (114, 5, 0.12)):
            a += s * max(0.0, 1 - abs(x - (cx + y * 0.1)) / w)
        a *= (1 - y / RH) ** 1.3
        a = round(a * 8) / 8
        if a > 0:
            rays.putpixel((x, y), (216, 240, 176, int(min(1, a) * 255)))
rays.save(f'{HERE}/Surface_Rays.png')

# Motas de la subida: trocitos de hoja pardos (los de abajo siguen siendo Abyss_Mote, las burbujas del abismo).
motes = []
for k in range(2):
    m = Image.new('RGBA', (4, 4), (0, 0, 0, 0))
    if k == 0:
        m.putpixel((1, 1), C('#a8b870')); m.putpixel((2, 1), C('#6a8a5a')); m.putpixel((2, 2), C('#6a8a5a', 170))
    else:
        m.putpixel((1, 2), C('#a8b870')); m.putpixel((2, 1), C('#6a8a5a')); m.putpixel((1, 1), C('#6a8a5a', 170))
    motes.append(m)
sheet(motes).save(f'{HERE}/Surface_Mote.png')

# --- vistas previas --------------------------------------------------------------------------------------------
from PIL import ImageDraw

BG = C('#1c2e20')
S = 3


def tinted_aura(k, rgb, strength=1.0):
    a = Image.open(f'{TEMP}/metamorphosis/Metamorphosis_Aura.png').convert('RGBA').crop((k * 64, 0, k * 64 + 64, 64))
    arr = np.array(a)
    arr[:, :, 0], arr[:, :, 1], arr[:, :, 2] = rgb
    arr[:, :, 3] = (arr[:, :, 3] * strength).astype(np.uint8)
    return Image.fromarray(arr, 'RGBA')


rows = list(morphs.items())
cw = N * S + 4
pv = Image.new('RGBA', (150 + (1 + 12 + 6) * cw, 10 + len(rows) * (N * S + 8)), BG)
d = ImageDraw.Draw(pv)
for r, ((a, b), frames) in enumerate(rows):
    y = 10 + r * (N * S + 8)
    d.text((8, y + N * S // 2 - 5), f'{a} > {b}', fill=C('#d8f0c0'))
    seq = [body_image(a)] + frames + (reveals[b] if a in FLOOR2 else [])
    for i, f in enumerate(seq):
        pv.alpha_composite(f.resize((N * S, N * S), Image.NEAREST), (150 + i * cw, y))
pv.save(f'{HERE}/vista_previa_transformaciones.png')

# Fondo reducido a la mitad con los rayos y unas motas, para ver la subida entera.
fv = bg.copy()
for x0 in (40, 250):
    fv.alpha_composite(rays, (x0, SURFACE + 3))
fv = fv.resize((W // 2 * 2, H // 2 * 2), Image.NEAREST)
fv.save(f'{HERE}/vista_previa_fondo.png')

# GIF de cada cuerpo del piso 3: sube por el agua (el fondo se desplaza), brilla, cambia y se revela.
os.makedirs(f'{HERE}/gif', exist_ok=True)
VW, VH = 160, 120


def scene(t, sprite, aura):
    """t de 0 (fondo del abismo) a 1 (pegado a la superficie)."""
    top = int((H - VH * 2) * (1 - t))
    view = bg.crop((0, top, VW * 2, top + VH * 2)).resize((VW, VH), Image.NEAREST).copy()
    ry = (SURFACE + 3 - top) // 2      # los rayos van pegados a la superficie, en la escala de la vista
    r = rays.resize((RW // 2, RH // 2), Image.NEAREST)
    if -r.height < ry < VH:
        view.alpha_composite(r.crop((0, max(0, -ry), r.width, r.height)), (30, max(0, ry)))
    if aura is not None:
        view.alpha_composite(aura, (VW // 2 - 32, VH // 2 - 32))
    view.alpha_composite(sprite, (VW // 2 - N // 2, VH // 2 - N // 2))
    return view.resize((VW * 3, VH * 3), Image.NEAREST).convert('RGB')


for a, b in (('Jellyfish', 'Frog'), ('Placoderm', 'Diplocaulus'), ('Crinoid', 'Whipscorpion'), ('Jellyfish', 'Whipscorpion')):
    seq = [body_image(a)] * 6 + morphs[(a, b)] + reveals[b] + [body_image(b)] * 6
    frames = []
    for i, f in enumerate(seq):
        t = i / (len(seq) - 1)
        strength = min(1.0, i / 6) if i < 6 + 12 else max(0.0, 1 - (i - 18) / 6)
        frames.append(scene(t, f, tinted_aura(i % 4, FAMILY[b], strength)))
    frames[0].save(f'{HERE}/gif/Metamorphosis_{a}_{b}.gif', save_all=True, append_images=frames[1:], duration=130, loop=0)
for a in FLOOR3:
    for b in FLOOR3:
        if a == b:
            continue
        frames = []
        for f in [body_image(a)] * 3 + morphs[(a, b)] + reveals[b] + [body_image(b)] * 3:
            c = Image.new('RGBA', (48, 48), BG)
            c.alpha_composite(f, (8, 8))
            frames.append(c.resize((192, 192), Image.NEAREST).convert('RGB'))
        frames[0].save(f'{HERE}/gif/Exchange_{a}_{b}.gif', save_all=True, append_images=frames[1:], duration=110, loop=0)
print('Listo:', HERE)
