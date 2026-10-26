"""
generar_formas.py - Primordia, piso 1 (bajo el agua)

Genera 6 tiles de pared nuevos por variante (Soup y Boss) para salas que no
son rectangulares (en L, en cruz, con columnas). No dibuja nada a mano: lee los
tiles existentes de <variante>/tiles/ y copia o modifica sus píxeles.

  Wall_FaceUpper_EndLeft / _EndRight   cara superior que termina en suelo
  Wall_FaceLower_EndLeft / _EndRight   cara inferior que termina en suelo
  Wall_OuterTopLeft / _OuterTopRight   esquina exterior de una masa de pared

Los colores de la franja (oscuro, claro, medio) se toman de Wall_EdgeRight,
así que salen idénticos a los de tus bordes.

Uso:
  python generar_formas.py <carpeta_entrada> <carpeta_salida>
    <carpeta_entrada> contiene Soup/tiles/ y Boss/tiles/ (tus tiles actuales)
    <carpeta_salida>  recibe Soup/tiles/, Boss/tiles/ y vista_previa_formas.png
Por defecto: entrada = ../environment, salida = carpeta de este script.
"""
import os
import sys
from PIL import Image, ImageDraw

T = 16
VARIANTS = ['Soup', 'Boss']


def load(folder, name):
    return Image.open(os.path.join(folder, name + '.png')).convert('RGBA')


def band_colors(tiles):
    """(oscuro, claro, medio) leídos de las 3 columnas izquierdas de Wall_EdgeRight."""
    e = tiles['Wall_EdgeRight']
    return tuple(e.getpixel((x, T // 2)) for x in range(3))


def left_band(im, band, y0, y1):
    """Franja vertical en las columnas 0 (oscura), 1 (clara), 2 (media), filas y0..y1-1."""
    for y in range(y0, y1):
        for x in range(3):
            im.putpixel((x, y), band[x])


def face_end_left(face, band, upper):
    im = face.copy()
    dark, light, mid = band
    if upper:
        # Filas 0 (oscura) y 1 (clara) ya son la línea de arriba de la cara.
        # La franja lateral baja desde la fila 2; la columna 0 es oscura en
        # toda la altura para seguir el contorno del borde que viene de arriba.
        left_band(im, band, 2, T)
        im.putpixel((0, 1), dark)
        # Esquina redondeada de 1 px: la línea clara de arriba y la clara
        # lateral no forman un ángulo recto; el píxel del ángulo (1,1) pasa a
        # oscuro y las dos líneas claras se tocan en diagonal.
        im.putpixel((1, 1), dark)
    else:
        # Cara inferior: franja en toda la altura menos la última fila
        # (oscura, toca el suelo), que se deja tal cual.
        left_band(im, band, 0, T - 1)
    return im


def outer_top_left(cap, band):
    """Wall_Cap con franja arriba y a la izquierda: anillos concéntricos, (0,0) oscuro."""
    im = cap.copy()
    for y in range(T):
        for x in range(T):
            d = min(x, y)
            if d < 3:
                im.putpixel((x, y), band[d])
    return im


def mirror(im):
    return im.transpose(Image.FLIP_LEFT_RIGHT)


def make_new(tiles):
    band = band_colors(tiles)
    fu = face_end_left(tiles['Wall_FaceUpper_0'], band, upper=True)
    fl = face_end_left(tiles['Wall_FaceLower_0'], band, upper=False)
    otl = outer_top_left(tiles['Wall_Cap'], band)
    new = {
        'Wall_FaceUpper_EndLeft': fu, 'Wall_FaceUpper_EndRight': mirror(fu),
        'Wall_FaceLower_EndLeft': fl, 'Wall_FaceLower_EndRight': mirror(fl),
        'Wall_OuterTopLeft': otl, 'Wall_OuterTopRight': mirror(otl),
    }
    return {k: v.convert('RGB') for k, v in new.items()}  # opacos


# ---------------------------------------------------------------- vista previa

ROOM_L = """\
##########
##########
##########
#...######
#...######
#...######
#........#
#........#
##########"""

ROOM_COL = """\
########
########
########
#......#
#..##..#
#..##..#
#..##..#
#......#
#......#
########"""


def pick(grid, x, y):
    """Reglas del juego para elegir el tile de cada celda."""
    h, w = len(grid), len(grid[0])

    def floor(cx, cy):
        return 0 <= cx < w and 0 <= cy < h and grid[cy][cx] == '.'

    def face(cx, cy):
        if not (0 <= cx < w and 0 <= cy < h) or grid[cy][cx] != '#':
            return None
        if floor(cx, cy + 1):
            return 'Wall_FaceLower'
        if floor(cx, cy + 2):
            return 'Wall_FaceUpper'
        return None

    if grid[y][x] == '.':
        return 'Floor_0' if floor(x, y - 1) else 'Floor_ShadowTop_0'  # pared encima -> sombra
    f = face(x, y)
    if f:
        if floor(x - 1, y):
            return f + '_EndLeft'
        if floor(x + 1, y):
            return f + '_EndRight'
        return f + '_0'
    up, left, right = floor(x, y - 1), floor(x - 1, y), floor(x + 1, y)
    if up and left:
        return 'Wall_OuterTopLeft'
    if up and right:
        return 'Wall_OuterTopRight'
    if up:
        return 'Wall_EdgeBottom'
    if left:
        return 'Wall_EdgeRight'
    if right:
        return 'Wall_EdgeLeft'
    if face(x - 1, y):
        return 'Wall_EdgeRight'
    if face(x + 1, y):
        return 'Wall_EdgeLeft'
    if floor(x + 1, y - 1):
        return 'Wall_CornerBL'
    if floor(x - 1, y - 1):
        return 'Wall_CornerBR'
    return 'Wall_Cap'


def build_room(text, tiles):
    grid = text.splitlines()
    h, w = len(grid), len(grid[0])
    im = Image.new('RGB', (w * T, h * T))
    for y in range(h):
        for x in range(w):
            im.paste(tiles[pick(grid, x, y)], (x * T, y * T))
    return im


def preview(all_tiles, out_path, scale=3):
    rooms = [(ROOM_L, 'Sala en L'), (ROOM_COL, 'Sala con columna')]
    pad, label_h = 8, 14
    cols = []
    for v in VARIANTS:
        ims = [build_room(r, all_tiles[v]).resize(
            (len(r.splitlines()[0]) * T * scale, len(r.splitlines()) * T * scale),
            Image.NEAREST) for r, _ in rooms]
        cols.append((v, ims))
    width = pad + sum(max(i.width for i in ims) + pad for _, ims in cols)
    height = pad + label_h + max(sum(i.height + label_h + pad for i in ims) for _, ims in cols)
    out = Image.new('RGB', (width, height), (14, 10, 20))
    d = ImageDraw.Draw(out)
    x = pad
    for v, ims in cols:
        d.text((x, pad), v, fill=(230, 220, 235))
        y = pad + label_h
        for (_, name), im in zip(rooms, ims):
            d.text((x, y), name, fill=(170, 160, 185))
            y += label_h
            out.paste(im, (x, y))
            y += im.height + pad
        x += max(i.width for i in ims) + pad
    out.save(out_path)


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    src = sys.argv[1] if len(sys.argv) > 1 else os.path.join(here, '..', 'environment')
    dst = sys.argv[2] if len(sys.argv) > 2 else here
    all_tiles = {}
    for v in VARIANTS:
        folder = os.path.join(src, v, 'tiles')
        tiles = {os.path.splitext(f)[0]: load(folder, os.path.splitext(f)[0])
                 for f in os.listdir(folder) if f.endswith('.png')}
        new = make_new(tiles)
        os.makedirs(os.path.join(dst, v, 'tiles'), exist_ok=True)
        for name, im in new.items():
            im.save(os.path.join(dst, v, 'tiles', name + '.png'))
        tiles = {k: im.convert('RGB') for k, im in tiles.items()}
        tiles.update(new)
        all_tiles[v] = tiles
    preview(all_tiles, os.path.join(dst, 'vista_previa_formas.png'))
    print('Listo:', dst)


if __name__ == '__main__':
    main()
