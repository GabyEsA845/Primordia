"""Puertas de losas de roca del piso 2 (temas Shallows, Cambrian y Abyss).

Dos losas de la misma piedra que la pared (bloques tallados con la paleta de la cara de la pared de cada tema) salen
de dentro de la pared y se juntan; en la costura se completa un sello que brilla con el color de las vetas del tema.
  - Arco de la pared superior (32×32): salen de los lados del arco, vistas de frente; solo ocupan el hueco del arco.
  - Pared lateral (16×32): salen de arriba y de abajo, vistas desde arriba; conservan la franja del borde de la pared.
  - Pared inferior (32×16): salen de izquierda y derecha, vistas desde arriba.

Cerrar y abrir son animaciones distintas:
  Door_<Pos>_Close  las losas salen, chocan (polvo y burbujas) y se asientan. El último cuadro es la puerta cerrada.
  Door_<Pos>_Open   el sello se enciende, las losas tiemblan y se retiran. El último cuadro está vacío.

Uso: python generar_puertas_piso2.py   (lee <Tema>/tiles/ y Sala_muestra_abierta.png; escribe en <Tema>/)
"""
import math, os, random
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUTL = (24, 20, 37, 255)
THEMES = {   # colores de cada tema (los de su pared): cara, sello que brilla y polvo
    'Shallows': dict(face=['#1b5062', '#266a7e', '#348498', '#52a2b0', '#90d4d4'], glow='#e8c070', dust=['#86d6c4', '#e4fff2']),
    'Cambrian': dict(face=['#1c2c50', '#28406c', '#365890', '#4e78b0', '#80a8d8'], glow='#ff9a70', dust=['#5a94b0', '#a8dcec']),
    'Abyss': dict(face=['#0b101e', '#111a2e', '#182640', '#223456', '#38507e'], glow='#7ae8ff', dust=['#2e6a8a', '#7ae8ff']),
}
CLOSE_GAPS = [None, 0.85, 0.62, 0.38, 0.14, 0.0, 0.0]     # parte de la mitad que sigue abierta en cada cuadro
CLOSE_MS = [70, 60, 60, 60, 60, 110, 140]
OPEN_STEPS = [('glow', 0.0), ('shake', 0.0), ('move', 0.15), ('move', 0.45), ('move', 0.75), (None, None)]
OPEN_MS = [130, 90, 60, 60, 60, 60]


def C(h, a=255):
    if isinstance(h, tuple): return h[:3] + (a,)
    h = h.lstrip('#'); return tuple(int(h[i:i+2], 16) for i in (0, 2, 4)) + (a,)


def darker(c, f=0.78): return (int(c[0] * f), int(c[1] * f), int(c[2] * f), c[3])
def lighter(c, f=0.35): return tuple(int(v + (255 - v) * f) for v in c[:3]) + (c[3],)


def tiles_of(theme):
    d = os.path.join(HERE, theme, 'tiles')
    return {f[:-4]: Image.open(os.path.join(d, f)).convert('RGBA') for f in os.listdir(d)}


def zig(i):
    """Dientes de la costura: las dos losas encajan como un engranaje."""
    return 1 if (i // 3) % 2 else 0


def puff(im, x, y, r, cols, a=200):
    for yy in range(int(y - r) - 1, int(y + r) + 2):
        for xx in range(int(x - r) - 1, int(x + r) + 2):
            d = math.hypot(xx + .5 - x, yy + .5 - y)
            if d <= r and 0 <= xx < im.width and 0 <= yy < im.height:
                im.putpixel((xx, yy), C(cols[1] if d < r * 0.5 else cols[0], a))


def bubble(im, x, y, r, a=210):
    for k in range(16):
        xx, yy = round(x + math.cos(k * math.pi / 8) * r), round(y + math.sin(k * math.pi / 8) * r)
        if 0 <= xx < im.width and 0 <= yy < im.height: im.putpixel((xx, yy), (200, 236, 255, a))


# ---------------------------------------------------------------- texturas de las losas cerradas
def blocks(im, th, cw=8, ch=6):
    """Piedra tallada en bloques con la paleta de la cara de la pared: juntas oscuras, arista clara arriba."""
    f = [C(c) for c in th['face']]
    rnd = random.Random(3)
    for y in range(im.height):
        for x in range(im.width):
            off = (cw // 2) if (y // ch) % 2 else 0
            u, v = (x + off) % cw, y % ch
            if u == 0 or v == 0: c = f[0]
            elif v == 1 or u == 1: c = f[3]
            elif v == ch - 1 or u == cw - 1: c = f[1]
            else: c = f[2] if rnd.random() > 0.1 else f[1]
            im.putpixel((x, y), c)


def arch_mask(x, y):
    return ((x + .5 - 16) / 12) ** 2 + ((y + .5 - 32) / 28) ** 2 <= 1


def front_texture(th):
    im = Image.new('RGBA', (32, 32), (0, 0, 0, 0)); blocks(im, th, 8, 6); return im


def top_texture(tile, w, h, axis, th):
    """Vista desde arriba: bloques claros y la franja del borde copiada de la pieza de pared (el labio que da al suelo)."""
    im = Image.new('RGBA', (w, h), (0, 0, 0, 0)); blocks(im, th, 8, 8)
    for y in range(h):
        for x in range(w):
            tx, ty = x % 16, y % 16
            if (axis == 'v' and tx >= 13) or (axis == 'h' and ty <= 2): im.putpixel((x, y), tile.getpixel((tx, ty)))
    return im


def seal(im, cx, cy, th, glow=0):
    """Rombo en la costura con el color de las vetas del tema; glow 1-2 al abrirse."""
    g = C(th['glow'])
    for dy in range(-3, 4):
        for dx in range(-3, 4):
            d = abs(dx) + abs(dy); x, y = cx + dx, cy + dy
            if not (0 <= x < im.width and 0 <= y < im.height and im.getpixel((x, y))[3]): continue
            if d == 3: im.putpixel((x, y), g if glow else darker(g, 0.75))
            elif d <= 1: im.putpixel((x, y), (255, 255, 255, 255) if glow == 2 else lighter(g, 0.5 if glow else 0.1))
    if glow:
        for k in range(4, 6 + glow * 3):   # grietas brillantes que salen del sello
            for x, y in ((cx - k, cy + (k % 2)), (cx + k, cy - (k % 2))):
                if 0 <= x < im.width and im.getpixel((x, y))[3] and (k + glow) % 3: im.putpixel((x, y), g)


# ---------------------------------------------------------------- deslizamiento
def slide(tex, axis, gap, mask=None, shake=0):
    """Cada losa lleva consigo su mitad de la textura cerrada. axis 'h': se mueven en x; 'v': en y.
    gap 0 = cerrada, 1 = abierta del todo. mask: hueco visible (el del arco)."""
    w, h = tex.size; has_mask = mask is not None; mask = mask or (lambda x, y: True)
    im = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    half = (w if axis == 'h' else h) / 2; o = int(round(gap * half))
    for y in range(h):
        for x in range(w):
            if not mask(x, y): continue
            p, q = (x, y) if axis == 'h' else (y, x)
            seam = int(half) + zig(q)
            if p < seam - o: sp = p + o - shake
            elif p >= seam + o: sp = p - o + shake
            else: continue
            sp = max(0, min((w if axis == 'h' else h) - 1, sp))
            im.putpixel((x, y), tex.getpixel((sp, y) if axis == 'h' else (x, sp)))
    out = im.copy()
    for y in range(h):   # filos de las losas y costura en oscuro; sombra bajo el borde del arco
        for x in range(w):
            if not im.getpixel((x, y))[3]: continue
            n = ((x - 1, y), (x + 1, y)) if axis == 'h' else ((x, y - 1), (x, y + 1))
            if any(0 <= a < w and 0 <= b < h and mask(a, b) and im.getpixel((a, b))[3] == 0 for a, b in n):
                out.putpixel((x, y), OUTL)
            elif has_mask and not (mask(x - 1, y) and mask(x + 1, y) and mask(x, y - 1)):
                out.putpixel((x, y), darker(out.getpixel((x, y)), 0.6))
    if o == 0:
        for q in range(h if axis == 'h' else w):
            p = int(half) + zig(q) - 1
            x, y = (p, q) if axis == 'h' else (q, p)
            if out.getpixel((x, y))[3]: out.putpixel((x, y), OUTL)
    return out


def door_close(tex, axis, th, center, dust_at, mask=None):
    fr = []
    for i, g in enumerate(CLOSE_GAPS):
        if g is None: fr.append(Image.new('RGBA', tex.size, (0, 0, 0, 0))); continue
        im = slide(tex, axis, g, mask)
        if g == 0: seal(im, *center, th)
        if i in (5, 6):   # choque: polvo junto a la costura y burbujas
            for (x, y, r) in dust_at: puff(im, x, y, r * (1 if i == 5 else 0.65), th['dust'], 220 if i == 5 else 110)
            if mask: bubble(im, 9, 22 - (i - 5) * 5, 1.4, 200 if i == 5 else 130); bubble(im, 23, 20 - (i - 5) * 5, 1.2, 200 if i == 5 else 130)
        fr.append(im)
    return fr


def door_open(tex, axis, th, center, mask=None):
    fr = []
    for kind, g in OPEN_STEPS:
        if kind == 'glow': im = slide(tex, axis, 0.0, mask); seal(im, *center, th, glow=1)
        elif kind == 'shake': im = slide(tex, axis, 0.0, mask, shake=1); seal(im, *center, th, glow=2)
        elif kind == 'move': im = slide(tex, axis, g, mask)
        else:
            im = Image.new('RGBA', tex.size, (0, 0, 0, 0))
            if mask: bubble(im, 10, 22, 1.4, 150); bubble(im, 21, 17, 1.2, 150)
        fr.append(im)
    return fr


def sheet(frames):
    w, h = frames[0].size
    sh = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames): sh.paste(f, (i * w, 0))
    return sh


def build(theme):
    th = THEMES[theme]; tiles = tiles_of(theme); d = os.path.join(HERE, theme)
    front = front_texture(th)
    side = top_texture(tiles['Wall_EdgeLeft'], 16, 32, 'v', th)
    bottom = top_texture(tiles['Wall_EdgeBottom'], 32, 16, 'h', th)
    doors = {
        'Door_Top_Close': door_close(front, 'h', th, (15, 20), [(11, 30, 2.4), (20, 30, 2.4), (16, 29, 1.6)], arch_mask),
        'Door_Top_Open': door_open(front, 'h', th, (15, 20), arch_mask),
        'Door_Side_Close': door_close(side, 'v', th, (6, 15), [(13, 12, 2.0), (13, 19, 2.0), (11, 16, 1.4)]),
        'Door_Side_Open': door_open(side, 'v', th, (6, 15)),
        'Door_Bottom_Close': door_close(bottom, 'h', th, (15, 9), [(12, 2, 2.0), (19, 2, 2.0), (16, 4, 1.4)]),
        'Door_Bottom_Open': door_open(bottom, 'h', th, (15, 9)),
    }
    for n, fr in doors.items(): sheet(fr).save(os.path.join(d, n + '.png'))
    return doors


def context(theme, doors):
    """Sala de muestra con las puertas encima, cuadro a cuadro: GIF y tira de vista previa."""
    d = os.path.join(HERE, theme)
    base = Image.open(os.path.join(d, 'Sala_muestra_abierta.png')).convert('RGBA')
    T, W, H = 16, 22, 15
    def put(kind, i):
        im = base.copy()
        im.alpha_composite(doors[f'Door_Top_{kind}'][i], (10 * T, 1 * T))
        im.alpha_composite(doors[f'Door_Bottom_{kind}'][i], (10 * T, (H - 1) * T))
        im.alpha_composite(doors[f'Door_Side_{kind}'][i], (0, 7 * T))
        im.alpha_composite(doors[f'Door_Side_{kind}'][i].transpose(Image.FLIP_LEFT_RIGHT), ((W - 1) * T, 7 * T))
        return im
    seq = [('Close', i) for i in range(7)] + [('Close', 6)] * 4 + [('Open', i) for i in range(6)] + [('Open', 5)] * 3
    ms = CLOSE_MS + [300] * 4 + OPEN_MS + [300] * 3
    frames = [put(k, i) for k, i in seq]
    os.makedirs(os.path.join(d, 'gif'), exist_ok=True)
    big = [f.resize((f.width * 2, f.height * 2), Image.NEAREST).convert('RGB') for f in frames]
    big[0].save(os.path.join(d, 'gif', 'Puertas_losas.gif'), save_all=True, append_images=big[1:], duration=ms, loop=0)
    crops = []
    for kind, n in (('Close', 7), ('Open', 6)):
        for i in range(n):
            im = put(kind, i)
            c = Image.new('RGBA', (6 * T + 2 * T + 4, 4 * T + 2 * T + 4), (0, 0, 0, 255))
            c.paste(im.crop((8 * T, 0, 14 * T, 4 * T)), (0, 0))
            c.paste(im.crop((0, 6 * T, 2 * T, 10 * T)), (6 * T + 4, 0))
            c.paste(im.crop((8 * T, (H - 2) * T, 14 * T, H * T)), (0, 4 * T + 4))
            crops.append(c)
    cw, ch = crops[0].size
    strip = Image.new('RGBA', (cw * 7 + 6 * 4, ch * 2 + 8), (0, 0, 0, 255))
    for k, c in enumerate(crops):
        r, col = (0, k) if k < 7 else (1, k - 7)
        strip.paste(c, (col * (cw + 4), r * (ch + 8)))
    return strip


if __name__ == '__main__':
    strips = [context(theme, build(theme)) for theme in THEMES]
    w, h = strips[0].size
    pv = Image.new('RGBA', (w, (h + 12) * len(strips)), (0, 0, 0, 255))
    for i, s in enumerate(strips): pv.paste(s, (0, i * (h + 12)))
    pv.resize((pv.width * 2, pv.height * 2), Image.NEAREST).save(os.path.join(HERE, 'vista_previa_puertas_piso2.png'))
    print('Puertas de losas listas.')
