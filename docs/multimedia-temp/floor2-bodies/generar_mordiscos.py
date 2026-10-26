"""Mordiscos de las Placas mandibulares y marca de Quebrar (rehecho el 2026-10-25 a petición del usuario).

- Effects/Fx_JawPlates_Bite.png       5 cuadros de 32×32: dos placas de hueso con la punta en gancho (la mandíbula de
                                      Dunkleosteus) que se cierran en un arco de 110°. Hacia la derecha, unión en (0, 16).
                                      El alcance del golpe es justo lo dibujado: 2 u (32 px) desde la unión.
- Effects/Fx_JawPlates_Guillotine.png 6 cuadros de 56×56: la segunda dentellada de la Guillotina, más grande (3,5 u) y
                                      con el filo al rojo y una estela. Unión en (0, 28).
- Effects/Status_Shatter.png          4 cuadros de 12×12: marca de Quebrar sobre el enemigo (una placa que se agrieta).
"""
import math
import os
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
OUTL = (24, 20, 37, 255)
PLATE = ['#353a4c', '#5f6880', '#9ea8c0', '#e2e8f4']   # las placas de hueso del pez
HOT = ['#7a2a1a', '#c8502a', '#ff8a5a', '#ffd8b0']     # filo al rojo de la Guillotina
MOUTH = (74, 20, 40)
SPARK = [(255, 255, 255), (255, 246, 192), (255, 224, 128)]


def C(h, a=255):
    h = h.lstrip('#')
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)) + (a,)


def jaw_points(length, upper):
    """Contorno de una placa hacia la derecha: filo sobre el eje con una muesca, lomo grueso y la punta en gancho que
    cruza el eje (la de abajo, más corta)."""
    s = -1 if upper else 1          # arriba = y negativa en la imagen
    L = length
    k = L / 30                      # todo crece con el tamaño
    hook = 3.0 if upper else 1.8
    pts = [(2, 0.3 * s), (L - 7 * k, 0.3 * s), (L - 6 * k, 2.0 * s), (L - 4.5 * k, 0.3 * s),
           (L - 1.5 * k, -hook * k * s), (L, -0.5 * k * s), (L - 0.5 * k, 2.5 * k * s),
           (L - 4 * k, 5.5 * k * s), (L * 0.62, 8.0 * k * s), (L * 0.32, 7.6 * k * s), (L * 0.12, 6.0 * k * s), (2, 4.0 * k * s)]
    return pts


def rotate(pts, deg, ox, oy):
    a = math.radians(deg)
    ca, sa = math.cos(a), math.sin(a)
    return [(ox + x * ca - y * sa, oy + x * sa + y * ca) for x, y in pts]


def draw_jaws(size, oy, length, opening, ramp, alpha=255, edge=None, mouth=True):
    """Las dos placas abiertas ±opening grados alrededor de la unión (1, oy)."""
    ox = 1
    mask_up = Image.new('L', (size, size), 0)
    mask_dn = Image.new('L', (size, size), 0)
    ImageDraw.Draw(mask_up).polygon(rotate(jaw_points(length, True), -opening, ox, oy), fill=255)
    ImageDraw.Draw(mask_dn).polygon(rotate(jaw_points(length, False), opening, ox, oy), fill=255)
    im = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    px = im.load()
    mu, md = mask_up.load(), mask_dn.load()

    # Boca oscura entre las placas mientras están abiertas.
    if mouth and opening > 4:
        inner = Image.new('L', (size, size), 0)
        r = length * 0.6
        ImageDraw.Draw(inner).polygon([(ox + 3, oy), rotate([(r, 0)], -opening * 0.7, ox, oy)[0], (ox + r * 0.9, oy),
                                       rotate([(r, 0)], opening * 0.7, ox, oy)[0]], fill=255)
        ip = inner.load()
        for y in range(size):
            for x in range(size):
                if ip[x, y] and not mu[x, y] and not md[x, y]:
                    px[x, y] = MOUTH + (int(alpha * 0.4),)

    for m, upper in ((mu, True), (md, False)):
        for y in range(size):
            for x in range(size):
                if not m[x, y]:
                    continue
                border = any(not (0 <= x + dx < size and 0 <= y + dy < size) or not m[x + dx, y + dy]
                             for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
                if border:
                    # El borde del lado del filo brilla; el de fuera va con contorno.
                    toward = (y + 1 < size and m[x, y + 1] == 0) if upper else (y - 1 >= 0 and m[x, y - 1] == 0)
                    col = C(edge or ramp[3]) if toward else OUTL
                else:
                    # Sombreado: claro junto al filo, medio en el centro y oscuro en el lomo; un brillo bajo el lomo
                    # de la placa de arriba (la luz viene de arriba a la izquierda).
                    step = 1 if upper else -1
                    to_edge = next((k for k in range(1, 9) if not (0 <= y + step * k < size) or not m[x, y + step * k]), 9)
                    to_back = next((k for k in range(1, 9) if not (0 <= y - step * k < size) or not m[x, y - step * k]), 9)
                    if to_edge <= 2:
                        col = C(ramp[2])
                    elif to_back == 1 and upper:
                        col = C(ramp[2])
                    elif to_back <= 2:
                        col = C(ramp[0])
                    else:
                        col = C(ramp[1])
                px[x, y] = col[:3] + (int(alpha * col[3] / 255),)
    return im


def bite():
    size, oy, L = 32, 16, 30
    frames = []
    for opening, alpha in ((40, 200), (26, 240), (12, 255), (0, 255), (0, 120)):
        im = draw_jaws(size, oy, L, opening, PLATE, alpha)
        if opening == 0 and alpha == 255:
            # Choque: destello a lo largo del filo cerrado y chispas en la punta.
            for x in range(6, L - 1):
                if x % 2 == 0:
                    im.putpixel((x, oy), SPARK[0] + (255,))
            for dx, dy in ((L + 1, oy - 2), (L + 1, oy + 2), (L - 3, oy - 4), (L - 3, oy + 4)):
                if 0 <= dx < size:
                    im.putpixel((dx, dy), SPARK[2] + (255,))
        if alpha == 120:
            # Astillas que saltan al final.
            for k in range(5):
                x, y = 10 + k * 4, oy + (-1 if k % 2 else 1) * (4 + k)
                if 0 <= y < size:
                    im.putpixel((x, y), C(PLATE[3], 150))
        frames.append(im)
    return frames


def guillotine():
    size, oy, L = 56, 28, 52
    frames = []
    for i, (opening, alpha) in enumerate(((48, 200), (32, 240), (16, 255), (0, 255), (0, 220), (0, 110))):
        im = draw_jaws(size, oy, L, opening, PLATE, alpha, edge=HOT[3])
        px = im.load()
        # Estela roja del cierre: arcos tenues por fuera de las placas.
        if 0 < opening < 40:
            for sgn in (-1, 1):
                for r in range(L - 10, L - 1):
                    a = math.radians(sgn * (opening + 6))
                    x, y = int(1 + r * math.cos(a)), int(oy + r * math.sin(a))
                    if 0 <= x < size and 0 <= y < size and px[x, y][3] == 0:
                        px[x, y] = C(HOT[2], 150)
        if opening == 0 and i == 3:
            # Corte: una línea al rojo vivo a lo largo del cierre y un destello grande en la punta.
            for x in range(4, L + 2):
                if x < size:
                    px[x, oy] = C(HOT[3]) if x % 3 else C('#ffffff')
            for dx, dy in ((0, -3), (0, 3), (-3, 0), (2, 0), (-2, -2), (-2, 2)):
                x, y = L + dx, oy + dy
                if 0 <= x < size and 0 <= y < size:
                    px[x, y] = C(HOT[2])
        if i == 4:
            for x in range(8, L - 2, 2):
                px[x, oy] = C(HOT[1], 180)
        if i == 5:
            for k in range(7):
                x, y = 14 + k * 5, oy + (-1 if k % 2 else 1) * (5 + k)
                if 0 <= y < size:
                    px[x, y] = C(HOT[2], 160)
        frames.append(im)
    return frames


def shatter():
    """12×12: una placa gris con borde naranja que se agrieta; la grieta se abre y brilla, en bucle."""
    frames = []
    cracks = [[(6, 3), (5, 4), (6, 5)], [(6, 3), (5, 4), (6, 5), (5, 6), (7, 6)],
              [(6, 3), (5, 4), (6, 5), (5, 6), (7, 6), (4, 7), (8, 7)], [(6, 3), (5, 4), (6, 5), (5, 6), (7, 6), (4, 7), (8, 7)]]
    for i, crack in enumerate(cracks):
        im = Image.new('RGBA', (12, 12), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        d.polygon([(6, 1), (10, 3), (10, 7), (6, 10), (2, 7), (2, 3)], fill=C(PLATE[2]), outline=OUTL)
        d.line([(3, 4), (6, 2)], fill=C(PLATE[3]))
        d.line([(4, 8), (9, 6)], fill=C(PLATE[1]))
        for x, y in crack:
            im.putpixel((x, y), C(HOT[2]) if i < 3 else C('#ffffff'))
        if i == 3:
            for x, y in ((1, 1), (10, 10), (11, 2)):
                im.putpixel((x, y), C(HOT[3]))
        frames.append(im)
    return frames


def strip(frames, path):
    w, h = frames[0].size
    out = Image.new('RGBA', (w * len(frames), h))
    for i, f in enumerate(frames):
        out.paste(f, (i * w, 0))
    out.save(path)


if __name__ == '__main__':
    fx = os.path.join(HERE, 'Effects')
    strip(bite(), os.path.join(fx, 'Fx_JawPlates_Bite.png'))
    strip(guillotine(), os.path.join(fx, 'Fx_JawPlates_Guillotine.png'))
    strip(shatter(), os.path.join(fx, 'Status_Shatter.png'))
    print('ok')
