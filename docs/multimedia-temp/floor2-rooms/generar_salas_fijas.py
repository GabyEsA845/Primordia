"""Salas fijas del piso 2 (paso 2 de feature/floor-2-structure): tienda, sala de cambio, las dos salas de jefe y la
antesala, en el formato de RoomTemplate, con su comprobación y vistas previas con los tilesets reales.

Uso: python generar_salas_fijas.py   (después de generar_salas_especiales.py, del que toma la comprobación y el dibujo)

Símbolos nuevos (además de los de las salas especiales):
  A  columna de algas: suelo, no bloquea; tapa la vista (sala del Anomalocaris)
  L  planta luminosa: suelo, con luz 2D (sala del Anomalocaris y antesala)
  K  cápsula del laboratorio de Darwin (2×2, bloquea)
  R  roca que se derrumba con la muda del Euriptérido (2×2, bloquea hasta la fase 2)

Salida en Layouts/Room_<sala>.txt, vista_previa_<sala>.png, catalogo_fijas.png e informe_fijas.txt.
"""
import math, os, random
from PIL import Image, ImageDraw

import generar_salas_especiales as base
from generar_salas_especiales import Room, basic, cave_edges, validate, render, kinds, classify, T, HERE

# Los símbolos nuevos, para las paredes y el alcance: A y L se pisan; K y R bloquean.
base.OPEN_MARKS |= set('ALKR')
base.WALK |= set('AL')
base.BLOCK |= set('KR')


def repair(rows):
    """Como la reparación de CaveGenerator: la pared que el arte no puede dibujar pasa a ser suelo (sin tocar el borde)."""
    g = [list(r) for r in rows]
    h, w = len(g), len(g[0])
    for _ in range(60):
        k = kinds([''.join(r) for r in g])
        changed = False
        for y in range(3, h - 1):
            for x in range(1, w - 1):
                if g[y][x] == '#' and classify(k, x, y) == 'Missing':
                    g[y][x] = '.'; changed = True
        if not changed:
            break
    return [''.join(r) for r in g]


# ======================================================================================
# Planos
# ======================================================================================
def shop():
    """Tienda (Arrecife): sala lateral un poco mayor que la pequeña, con el centro despejado de 16×10 para el mostrador
    de Darwin (3 u por encima del centro) y los 4 pedestales (1,5 u por debajo). Puertas en los cuatro lados: una da al
    camino y la de enfrente a la sala de cambio."""
    r = basic(26, 18)
    for x, y in [(2, 4), (22, 4), (2, 14), (23, 15)]:
        r.put(x, y, 'o')
    return r.doors(up=12, down=12, left=9, right=9)


def exchange():
    """Sala de cambio (el laboratorio de Darwin): pequeña y en fondo de saco, con la cápsula en el centro."""
    r = basic(22, 16)
    r.block(10, 8, 'K')
    for x, y in [(3, 4), (18, 4), (3, 13), (18, 13)]:
        r.put(x, y, 'o')
    return r.doors(up=10, down=10, left=8, right=8)


def anomalocaris():
    """Sala del Anomalocaris (Abismo): cueva ovalada de unos 36×26 de suelo, sin fosas ni obstáculos (en el frenesí hace
    falta todo el suelo). 5 columnas de algas que tapan la vista sin bloquear y plantas luminosas por los bordes y alguna
    en el centro, que dan casi toda la luz cuando baja al entrar."""
    w, h = 40, 31
    r = Room(w, h)
    cx, cy, rx, ry = (w - 1) / 2, 3 + (h - 4) / 2, 18.6, 13.6
    for y in range(3, h - 1):
        for x in range(1, w - 1):
            if ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1:
                r.put(x, y, '.')
    # cuellos de las puertas hasta el óvalo
    r.rect(17, 3, 22, 6, '.').rect(17, h - 5, 22, h - 2, '.').rect(1, 14, 4, 19, '.').rect(w - 5, 14, w - 2, 19, '.')
    for x, y in [(11, 9), (28, 9), (8, 20), (31, 21), (20, 16)]:
        r.put(x, y, 'A')
    edge = [(6, 12), (33, 12), (4, 17), (35, 17), (9, 24), (30, 24), (14, 27), (25, 27), (13, 6), (26, 6)]
    for x, y in edge + [(17, 13), (23, 19)]:
        r.put(x, y, 'L')
    return r.doors(up=19, down=19, left=16, right=16)


def eurypterid():
    """Sala del Euriptérido (Abismo): cueva de unos 30×22 con 6 rocas de cobertura (2 se derrumban con la muda) y grietas
    solo junto a las paredes, que en la fase 2 obligan a no pegarse a ellas."""
    r = basic(32, 25)
    for x, y in [(7, 7), (23, 7), (7, 17), (23, 17)]:
        r.block(x, y, 'O')
    r.block(11, 12, 'R').block(19, 12, 'R')
    return r.doors(up=15, down=15, left=12, right=12)


# Grietas del Euriptérido: bloques de fosa que se pegan a la pared una vez hecho el contorno de cueva. (x, y) es un punto
# cerca del borde y (dx, dy) la dirección hacia la pared; el bloque mide w×h.
EURYPTERID_CRACKS = [((4, 7), (-1, 0), 2, 3), ((27, 10), (1, 0), 2, 3), ((4, 18), (-1, 0), 2, 3), ((27, 19), (1, 0), 2, 3),
                     ((6, 20), (0, 1), 3, 2), ((24, 20), (0, 1), 3, 2), ((10, 6), (0, -1), 3, 2), ((21, 6), (0, -1), 3, 2)]


def eurypterid_cracks(rows):
    """Pone cada grieta lo más cerca posible de la pared, sin tocar los cuellos de las puertas ni las rocas."""
    g = [list(r) for r in rows]
    h, w = len(g), len(g[0])
    for (x, y), (dx, dy), bw, bh in EURYPTERID_CRACKS:
        fits = lambda x0, y0: all(0 < x0 + i < w - 1 and 2 < y0 + j < h - 1 and g[y0 + j][x0 + i] == '.'
                                  for i in range(bw) for j in range(bh))
        best = None
        while fits(x, y):
            best = (x, y)
            x += dx; y += dy
        if best:
            for i in range(bw):
                for j in range(bh):
                    g[best[1] + j][best[0] + i] = '~'

    # Los rincones de suelo que la grieta deja entre ella y la pared pasan a ser grieta: nada que solo se alcance esquivando.
    start = next((x, 3) for x in range(w) if g[3][x] == '.')
    seen, stack = {start}, [start]
    while stack:
        x, y = stack.pop()
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= nx < w and 0 <= ny < h and (nx, ny) not in seen and g[ny][nx] in '.':
                seen.add((nx, ny)); stack.append((nx, ny))
    for y in range(h):
        for x in range(w):
            if g[y][x] == '.' and (x, y) not in seen:
                g[y][x] = '~'
    return [''.join(r) for r in g]


AFTER_CONTOUR = {'Boss_Eurypterid': eurypterid_cracks}


def antechamber_vertical():
    """Antesala vertical (Abismo): túnel de cueva más largo que un pasillo, de 6 a 8 tiles de ancho. Se entra por arriba y
    se sale por abajo, por la puerta de losas grande con el sello encendido que da a la sala del jefe."""
    r = Room(14, 28)
    rnd = random.Random(7)
    half = 3
    for y in range(3, 27):
        half = max(3, min(4, half + rnd.choice((-1, 0, 0, 1)))) if y % 3 == 0 else half
        r.rect(7 - half, y, 6 + half, y, '.')
    r.rect(4, 3, 9, 5, '.').rect(4, 24, 9, 26, '.')
    for x, y in [(4, 10), (9, 16), (4, 21)]:
        r.put(x, y, 'L')
    return r.doors(up=6, down=6)


def antechamber_horizontal():
    """Antesala horizontal: el mismo túnel, de izquierda a derecha (el jefe al otro lado de la puerta de losas grande)."""
    r = Room(30, 13)
    rnd = random.Random(11)
    top, bottom = 4, 10
    for x in range(1, 29):
        if x % 3 == 0:
            top = max(3, min(4, top + rnd.choice((-1, 0, 1))))
            bottom = max(9, min(11, bottom + rnd.choice((-1, 0, 1))))
        r.rect(x, top, x, bottom, '.')
    r.rect(1, 5, 3, 9, '.').rect(26, 5, 28, 9, '.')
    for x, y in [(8, 4), (15, 10), (22, 4)]:
        r.put(x, y, 'L')
    return r.doors(left=6, right=6)


# (nombre, plano, franja de la vista previa, entrantes máximos del contorno de cueva; 0 = ya es cueva)
ROOMS = [
    ('Shop_Floor2', shop, 'Cambrian', 2),
    ('Exchange', exchange, 'Cambrian', 2),
    ('Boss_Anomalocaris', anomalocaris, 'Abyss', 0),
    ('Boss_Eurypterid', eurypterid, 'Abyss', 2),
    ('Antechamber_Vertical', antechamber_vertical, 'Abyss', 0),
    ('Antechamber_Horizontal', antechamber_horizontal, 'Abyss', 0),
]

# Lo que tiene que quedar libre: el centro de la tienda (mostrador y pedestales) y el de la sala de cambio.
CLEAR = {'Shop_Floor2': (5, 6, 20, 15), 'Exchange': (6, 6, 15, 13)}


# ======================================================================================
# Vista previa de los símbolos nuevos (aún sin arte: marcas provisionales)
# ======================================================================================
def overlay(im, rows):
    d = ImageDraw.Draw(im)
    done = set()
    for y, row in enumerate(rows):
        for x, c in enumerate(row):
            px, py = x * T, y * T
            if c == 'A':      # columna de algas: verde alto que sube por encima de su casilla
                d.rectangle([px + 5, py - 20, px + 10, py + 14], fill=(46, 120, 86, 220), outline=(18, 50, 40, 255))
                for k in range(4):
                    d.ellipse([px + (1 if k % 2 else 9), py - 18 + k * 8, px + (7 if k % 2 else 15), py - 12 + k * 8], fill=(80, 170, 110, 230))
            elif c == 'L':    # planta luminosa: halo cian y tallo
                d.ellipse([px - 10, py - 10, px + 26, py + 26], fill=(106, 232, 240, 40))
                d.line([px + 8, py + 14, px + 8, py + 6], fill=(60, 140, 150), width=2)
                d.ellipse([px + 4, py + 1, px + 12, py + 8], fill=(150, 250, 255), outline=(40, 120, 140))
            elif c in 'KR' and (x, y) not in done:
                for dx in (0, 1):
                    for dy in (0, 1):
                        done.add((x + dx, y + dy))
                if c == 'K':  # cápsula: vidrio con líquido verde
                    d.rounded_rectangle([px + 3, py - 8, px + 28, py + 30], 8, fill=(120, 220, 170, 150), outline=(220, 240, 255), width=2)
                    d.rectangle([px + 2, py + 26, px + 29, py + 31], fill=(90, 90, 110), outline=(30, 30, 40))
                else:         # roca que se derrumba: roca con grietas rojas
                    d.ellipse([px + 1, py + 2, px + 30, py + 30], fill=(60, 64, 78), outline=(20, 22, 30), width=2)
                    d.line([px + 8, py + 8, px + 16, py + 18, px + 24, py + 10], fill=(230, 90, 80), width=2)
                    d.line([px + 16, py + 18, px + 14, py + 27], fill=(230, 90, 80), width=2)
    return im


# ======================================================================================
if __name__ == '__main__':
    os.makedirs(os.path.join(HERE, 'Layouts'), exist_ok=True)
    report, images = [], []
    for name, make, theme, depth in ROOMS:
        rows = repair(make().text())
        if depth:
            clear = CLEAR.get(name)
            for seed in range(1, 400):
                cand = cave_edges(rows, seed * 101 + len(name), depth)
                if name in AFTER_CONTOUR:
                    cand = AFTER_CONTOUR[name](cand)
                ok = not validate(name, cand)[0]
                if ok and clear:
                    x0, y0, x1, y1 = clear
                    ok = all(cand[y][x] == rows[y][x] for y in range(y0, y1 + 1) for x in range(x0, x1 + 1))
                if ok:
                    rows = cand; break
            else:
                print(f'{name}: ninguna semilla valió, se queda sin contorno de cueva')
        with open(os.path.join(HERE, 'Layouts', f'Room_{name}.txt'), 'w', encoding='utf-8') as f:
            f.write('\n'.join(rows) + '\n')
        problems, notes = validate(name, rows)
        report.append(f'{name} ({len(rows[0])}×{len(rows)}, {theme})')
        report += [f'  ERROR: {p}' for p in problems] + [f'  nota: {n}' for n in notes]
        if not problems:
            report.append('  correcta')
        im = overlay(render(rows, theme), rows)
        im.resize((im.width * 2, im.height * 2), Image.NEAREST).save(os.path.join(HERE, f'vista_previa_{name}.png'))
        images.append((name, im))
    with open(os.path.join(HERE, 'informe_fijas.txt'), 'w', encoding='utf-8') as f:
        f.write('\n'.join(report) + '\n')

    pad = 28
    W = sum(im.width for _, im in images[:3]) + pad * 4
    H = max(im.height for _, im in images[:3]) + max(im.height for _, im in images[3:]) + pad * 3
    cat = Image.new('RGBA', (W, H), (10, 15, 28, 255))
    d = ImageDraw.Draw(cat)
    for row, group in enumerate((images[:3], images[3:])):
        x = pad
        y = pad if row == 0 else pad * 2 + max(im.height for _, im in images[:3])
        for name, im in group:
            d.text((x, y - 14), name, fill=(190, 230, 255))
            cat.alpha_composite(im, (x, y))
            x += im.width + pad
    cat.save(os.path.join(HERE, 'catalogo_fijas.png'))
    print('\n'.join(report))
