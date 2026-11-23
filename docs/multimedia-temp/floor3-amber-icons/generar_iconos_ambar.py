"""Iconos de las 3 especiales del ámbar del piso 3 (2026-11-23), a 32 y 16 px. Sustituyen a los de floor3-mutations/Icons.

Marca común de las especiales: contorno dorado de 1 px (en vez del morado OUTL) y un destello en estrella arriba a
la derecha; adelanta el contorno dorado que llevará el cuerpo (subtanda 3 del aspecto de las especiales).
  · Cápsula: la forma D (gota con punta y pinchos que suben) en ámbar facetado, con un corazón cálido dentro.
  · Gota: el cristal del lomo (esquirlas de ámbar) con una gota que asoma de la punta.
  · Cercos: dos cercos segmentados y gruesos en V que salen de la punta del abdomen, con las puntas de ámbar.

Uso: python generar_iconos_ambar.py
"""
import math, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
TEMP = os.path.normpath(os.path.join(HERE, '..'))
sys.path.insert(0, os.path.join(TEMP, 'floor3-mutations'))
import generar_mutaciones_piso3 as gm
from generar_enemigos_piso3 import *                   # noqa: F401,F403  (C, Canvas, OUTL, SPARK, preview, compare...)
from PIL import Image, ImageDraw

AMB = [C(c) for c in gm.AMB]
CAR = [C(c) for c in gm.CARAMEL]
GOLD = C('#f2c24e')                                    # contorno de las especiales
HEART = [C(c) for c in ('#7a1a22', '#c8323a', '#f06a5a', '#ffc0a0')]
ICONS = os.path.join(HERE, 'Icons')

def rgba(c, a=255): return tuple(c[:3]) + (a,)

def finish(cv, n):
    """Contorno dorado + destello en estrella arriba a la derecha."""
    cv.outline(GOLD); cv.clean(GOLD)
    if n >= 32:
        sx, sy = 27, 4
        for d in (1, 2):
            for dx, dy in ((d, 0), (-d, 0), (0, d), (0, -d)):
                cv.px[(sx + dx, sy + dy)] = rgba(SPARK[1] if d == 1 else SPARK[2], 255 if d == 1 else 200)
        cv.px[(sx, sy)] = rgba(SPARK[0])
    else:
        sx, sy = 13, 2
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)): cv.px[(sx + dx, sy + dy)] = rgba(SPARK[2], 220)
        cv.px[(sx, sy)] = rgba(SPARK[0])
    return cv

def rim(cv):
    """Borde rojizo por dentro (como la Cápsula rediseñada): separa el ámbar del contorno dorado."""
    edge = [(x, y) for (x, y) in cv.px if any((x + dx, y + dy) not in cv.px for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for (x, y) in edge:
        c = cv.px[(x, y)]
        if c[:3] in (tuple(AMB[4][:3]), tuple(AMB[3][:3]), tuple(AMB[2][:3])):
            cv.px[(x, y)] = rgba(AMB[1] if c[:3] != tuple(AMB[2][:3]) else AMB[0])
        else: cv.px[(x, y)] = rgba(AMB[0])

def depth_of(mask, x, y, n=4):
    for k in range(1, n):
        if not all((x + dx * k, y + dy * k) in mask for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))): return k
    return n

# --- Cápsula ---------------------------------------------------------------------------------------
def capsule(n):
    k = n / 32
    top, cyb, rx, ry = 3 * k, 19.5 * k, 10.5 * k, 8.5 * k        # punta arriba, bulbo abajo
    cx = n / 2
    mask = set()
    for y in range(n):
        yy = y + .5
        e = rx * math.sqrt(max(0, 1 - ((yy - cyb) / ry) ** 2))
        q = (yy - top) / (cyb - top)
        t = (1.2 * k + (rx - 1.2 * k) * q ** 1.1) if top < yy < cyb else 0
        hw = max(e, t)
        if n >= 32 and 8 < y < 25:                                # pinchos que suben pegados a los lados
            ph = (y + 1) % 5; hw += 1.6 * (.9, .6, .3, 0, 0)[ph]
        for x in range(n):
            if hw > 0 and abs(x + .5 - cx) <= hw: mask.add((x, y))
    cv = Canvas(n)
    for (x, y) in mask:
        d = depth_of(mask, x, y); l = ((x - cx) / (2 * rx) + (y - top) / (cyb + ry - top)) - .5   # -: luz, +: sombra
        if d == 1: c = AMB[2] if l < -.25 else AMB[0] if l > .2 else AMB[1]
        elif d == 2: c = AMB[3] if l < -.35 else AMB[1] if l > .15 else AMB[2]
        else: c = AMB[3] if l < -.15 else AMB[2]
        cv.put(x, y, rgba(c))
    # corazón dentro (con un halo cálido alrededor: se ve a través del ámbar)
    hx, hy = int(cx) - (1 if n >= 32 else 0), int(cyb + (1 if n >= 32 else 0))
    heart = (['.##.##.', '#######', '#######', '.#####.', '..###..', '...#...'] if n >= 32 else ['#.#', '###', '.#.'])
    hw_, hh_ = len(heart[0]), len(heart)
    ox, oy = hx - hw_ // 2, hy - hh_ // 2
    if n >= 32:
        for j, row in enumerate(heart):
            for i, ch in enumerate(row):
                if ch != '#': continue
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    p = (ox + i + dx, oy + j + dy)
                    if p in mask and (p[0] - ox, p[1] - oy) not in [(a, b) for b, r in enumerate(heart) for a, cc in enumerate(r) if cc == '#']:
                        cv.put(*p, rgba(AMB[4], 255))
    for j, row in enumerate(heart):
        for i, ch in enumerate(row):
            if ch != '#': continue
            u, v = i / (hw_ - 1), j / (hh_ - 1)
            c = HEART[3] if (n >= 32 and (i, j) in ((1, 1), (2, 1))) else HEART[2] if u + v < .7 else HEART[1] if u + v < 1.3 else HEART[0]
            if n < 32: c = HEART[2] if (i, j) == (0, 0) else HEART[1]
            cv.put(ox + i, oy + j, rgba(c))
    # reflejo largo a la izquierda, vetas y punto de luz
    if n >= 32:
        for (x, y) in ((9, 21), (9, 19), (10, 17), (11, 15), (12, 13), (13, 11)): cv.put(x, y, rgba(AMB[4]))
        for (x, y) in ((19, 9), (20, 11), (21, 13), (22, 16), (23, 18)): cv.put(x, y, rgba(AMB[1]))      # veta
        for (x, y) in ((12, 26), (13, 26), (19, 26), (20, 25)): cv.put(x, y, rgba(AMB[0]))
        cv.put(14, 7, rgba(SPARK[1]))
    else:
        cv.put(5, 9, rgba(AMB[4])); cv.put(6, 7, rgba(AMB[4])); cv.put(7, 4, rgba(SPARK[1]))
    rim(cv)
    return finish(cv, n)

# --- Gota (cristal del lomo) -----------------------------------------------------------------------
def shard(cv, bx, by, h, w, lean, bright=False):
    """Esquirla: base de ancho w en la fila by, punta h px más arriba, inclinada lean px (como la glándula)."""
    for k in range(h + 1):
        t = k / h; half = (w / 2) * (1 - t) ** .85; cx = bx + lean * t; y = by - k
        for x in range(round(cx - half - .01), round(cx + half + .01) + 1):
            if abs(x - cx) > half + .5: continue
            left = x <= cx; edge = x == round(cx - half - .01)
            if bright: c = AMB[4] if (left and edge) else AMB[3] if left else AMB[2]
            elif left: c = AMB[4] if (edge and k > 1) else AMB[3]
            else: c = AMB[1] if t < .55 else AMB[2]
            if k == 0 or (k == 1 and not left): c = AMB[0] if not left else AMB[1]
            cv.put(x, y, rgba(c))

def teardrop(cv, x0, y0, h, w):
    """Gota con la punta arriba: (x0, y0) = punta."""
    for k in range(h):
        t = k / (h - 1); half = (w / 2) * (math.sin(min(1, t * 1.25) * math.pi * .5) if t < .8 else math.sqrt(max(0, 1 - ((t - .8) / .2) ** 2 * .9)))
        for x in range(round(x0 - half), round(x0 + half) + 1):
            if abs(x + .0 - x0) > half + .3: continue
            l = (x - x0) / max(half, .5) + (t - .5)
            cv.put(x, y0 + k, rgba(AMB[4] if l < -.6 else AMB[3] if l < .1 else AMB[2] if l < .6 else AMB[1]))

def drop(n):
    cv = Canvas(n)
    if n >= 32:
        for y in range(24, 29):                          # costra de resina de donde salen las esquirlas
            for x in range(3, 25):
                if ((x + .5 - 14) / 11) ** 2 + ((y + .5 - 26.5) / 2.6) ** 2 <= 1:
                    cv.put(x, y, rgba(AMB[1] if y < 26 else AMB[0]))
        shard(cv, 7, 26, 13, 7, -3)
        shard(cv, 20, 26, 7, 6, 2)
        shard(cv, 13, 26, 21, 10, 1)
        for (x, y) in ((10, 24), (11, 21), (12, 17), (13, 13), (13, 10)): cv.put(x, y, rgba(AMB[4]))   # arista con luz
        rim(cv)
        teardrop(cv, 24, 9, 8, 5)                        # la gota que cae
    else:
        for x in range(2, 13): cv.put(x, 12, rgba(AMB[1])); cv.put(x, 13, rgba(AMB[0]))
        shard(cv, 4, 12, 6, 4, -1)
        shard(cv, 7, 12, 10, 5, 0)
        shard(cv, 10, 12, 3, 3, 0)
        cv.put(6, 9, rgba(AMB[4])); cv.put(6, 6, rgba(AMB[4]))
        rim(cv)
        teardrop(cv, 12, 5, 4, 3)
    return finish(cv, n)

# --- Cercos ----------------------------------------------------------------------------------------
def cercus(cv, x0, y0, ang, length, r0, r1, seg):
    """Cerco grueso que se afina, en segmentos; la punta en ámbar."""
    dx, dy = math.cos(ang), math.sin(ang)
    nx, ny = -dy, dx                                    # normal (para el lado con luz)
    steps = int(length * 3)
    for s in range(steps + 1):
        t = s / steps; r = r0 + (r1 - r0) * t
        px_, py_ = x0 + dx * length * t, y0 + dy * length * t
        for yy in range(int(py_ - r - 1), int(py_ + r + 2)):
            for xx in range(int(px_ - r - 1), int(px_ + r + 2)):
                ox, oy = xx + .5 - px_, yy + .5 - py_
                if ox * ox + oy * oy > r * r: continue
                side = (ox * nx + oy * ny) / max(r, .5)        # -1..1 a través del cerco
                lit = side * (1 if ny < 0 else -1)             # el lado que mira arriba-izquierda, con luz
                joint = (length * t) % seg < 1 and t < .82
                if t > .84: c = AMB[3] if lit > 0 else AMB[2]
                elif joint: c = CAR[0]
                else: c = CAR[3] if lit > .35 else CAR[2] if lit > -.4 else CAR[1]
                cv.put(xx, yy, rgba(c))

def cerci(n):
    cv = Canvas(n); k = n / 32
    ax, ay = (6 if n >= 32 else 7) * k, (26 if n >= 32 else 24.5) * k                              # donde nacen (punta del abdomen, fuera del icono)
    cercus(cv, ax, ay, -math.pi * .36, 22 * k, 2.6 * k, .9 * k, 4 * k if n >= 32 else 3)
    cercus(cv, ax + 1 * k, ay, -math.pi * .07, 21 * k, 2.6 * k, .9 * k, 4 * k if n >= 32 else 3)
    for y in range(n):                                  # placa del abdomen que los une
        for x in range(n):
            if ((x + .5 - ax) / (3.2 * k)) ** 2 + ((y + .5 - ay - .5 * k) / (2.4 * k)) ** 2 <= 1:
                cv.put(x, y, rgba(CAR[2] if (x - ax) + (y - ay) < -1 else CAR[1]))
    return finish(cv, n)

def main():
    os.makedirs(ICONS, exist_ok=True)
    new, old = {}, {}
    for name, fn in (('Cerci', cerci), ('AmberCapsule', capsule), ('AmberDrop', drop)):
        for n in (32, 16):
            im = fn(n).img()
            im.save(os.path.join(ICONS, f'Icon_{name}_{n}.png'))
            new[f'{name}_{n}'] = im
            old[f'{name}_{n}'] = Image.open(os.path.join(TEMP, 'floor3-mutations', 'Icons', f'Icon_{name}_{n}.png')).convert('RGBA')
    compare(os.path.join(HERE, 'vista_previa_iconos_antes_despues.png'),
            {k: (old[k], new[k]) for k in new}, '#1c2e20', scale=4, labels=('antes', 'ahora'))
    # junto a las normales del piso 3, a 32 y 16 (×3)
    others = ['JumpingLegs', 'SpinnerLegs', 'AmphibianSkin', 'CharredScales', 'MethaneSac', 'Pheromones', 'ClassParasite']
    sheet = Image.new('RGBA', (10 * 104 + 10, 2 * 110 + 10), C('#1c2e20')); d = ImageDraw.Draw(sheet)
    for r, n in enumerate((32, 16)):
        row = [Image.open(os.path.join(TEMP, 'floor3-mutations', 'Icons', f'Icon_{o}_{n}.png')).convert('RGBA') for o in others]
        row += [new[f'{nm}_{n}'] for nm in ('Cerci', 'AmberCapsule', 'AmberDrop')]
        s = 3 if n == 32 else 5
        for i, im in enumerate(row):
            big = im.resize((im.width * s, im.height * s), Image.NEAREST)
            sheet.alpha_composite(big, (10 + i * 104, 10 + r * 110))
        d.text((10 + 7 * 104, 10 + r * 110 + 98), f'{n} px', fill=C('#bfe6ff'))
    sheet.save(os.path.join(HERE, 'vista_previa_iconos_juntos.png'))

if __name__ == '__main__':
    main()
