"""Coronas de los arcos de arriba y marcos de las puertas laterales, pisos 1-3 (10 temas).

Todo se pinta con la textura de los tiles de cada tema (tapa, cara, borde), como si la pared siguiera, para que no parezca
pegado encima:
- Doorway_Top_Crown (48×24): sobre el arco de arriba. Pisos 1 y 2: la cara de la pared sube en joroba alrededor del arco y se
  mete en la tapa, con el mismo labio de luz (piso 1, con venas; piso 2, con dovelas de roca y algas). Piso 3: la espesura de la
  tapa se derrama por encima del arco, con puntas de hoja y lianas.
- Doorway_Side_Front / Doorway_Side_Back (24×56): puerta lateral izquierda (la derecha, con Flip X). Front: la pared sigue como
  un puente sobre la parte alta del hueco y enseña su cara debajo; el jugador pasa por debajo. Back: la jamba de abajo acaba en
  redondo con el labio y el borde de luz doblando la esquina, y la sombra del puente en el suelo del hueco.
Antes y después en las vistas previas. Skill primordia-sprites (pixel.py).

Uso: python generar_marcos_puertas.py [carpeta_salida]   (por defecto, la carpeta de este script)
"""
import math, os, random, sys
from collections import Counter

HERE = os.path.dirname(os.path.abspath(__file__))
SKILL = os.path.join(HERE, '..', '..', '..', '.claude', 'skills', 'primordia-sprites', 'scripts')
sys.path.insert(0, os.path.abspath(SKILL))
from pixel import *
from PIL import Image

OUT = sys.argv[1] if len(sys.argv) > 1 else HERE
ENV = os.path.join(HERE, '..', 'environment')
ENV3 = os.path.join(HERE, '..', 'floor3-environment')

IDX12 = dict(cap=8, fu=9, fl=11, el=13, floor=0, shadow=6)
IDX3 = dict(cap=8, fu=12, fl=16, el=20, floor=0, shadow=6)
THEMES = [('Soup', 1, ENV, IDX12), ('Boss', 1, ENV, IDX12), ('Amoeba', 1, ENV, IDX12),
          ('Shore', 2, ENV, IDX12), ('Shallows', 2, ENV, IDX12), ('Cambrian', 2, ENV, IDX12), ('Abyss', 2, ENV, IDX12),
          ('Swamp', 3, ENV3, IDX3), ('Smoke', 3, ENV3, IDX3), ('Burnt', 3, ENV3, IDX3)]

# Acentos apagados (el escenario va apagado: solo lo que importa para jugar va saturado).
ACCENT = {
    'Shore': dict(algae=['#2a4a3a', '#3e6a4c']), 'Shallows': dict(algae=['#1e4a40', '#2e6a54']),
    'Cambrian': dict(algae=['#1e4048', '#2e5a5c']), 'Abyss': dict(algae=['#14283a', '#1e4050']),
    'Swamp': dict(vine='#2e4a26', tip='#4a6e34'), 'Smoke': dict(vine='#4a3e1e', tip='#7a6630'),
    'Burnt': dict(vine='#221c1e', tip='#4c3e38', ember=['#a8341c', '#e8682a']),
}

def tile(sheet, k):
    return sheet.crop(((k % 8) * 16, (k // 8) * 16, (k % 8) * 16 + 16, (k // 8) * 16 + 16))

def lum(c): return .3 * c[0] + .59 * c[1] + .11 * c[2]

def tex(im, x, y):
    return im.getpixel((int(x) % 16, int(y) % 16))

def darker(c, k=.72):
    return (int(c[0] * k), int(c[1] * k), int(c[2] * k * 1.05), 255)

def load_theme(name, folder, idx):
    base = os.path.join(folder, name)
    sheet = Image.open(os.path.join(base, f'Tileset_{name}.png')).convert('RGBA')
    arch = Image.open(os.path.join(base, 'Doorway_Top.png')).convert('RGBA')
    t = {k: tile(sheet, v) for k, v in idx.items()}
    # Hueco y marco del arco: lo que difiere de la cara de la pared en el mismo sitio
    mask = set()
    for y in range(32):
        for x in range(32):
            ref = tex(t['fu'] if y < 16 else t['fl'], x, y)
            p = arch.getpixel((x, y))
            if p[3] > 0 and sum(abs(a - b) for a, b in zip(p[:3], ref[:3])) > 24: mask.add((x, y))
    acnt = Counter(p[:3] for p in arch.getdata() if p[3] > 200)
    rim = max(acnt, key=lambda c: lum(c) if acnt[c] >= 4 else -1)
    lip = max((t['fu'].getpixel((x, 0)) for x in range(16)), key=lum)       # labio de luz de arriba de la cara
    side = None
    for f in ('Door_Side_Close.png', 'Door_Side.png'):
        p = os.path.join(base, f)
        if os.path.exists(p):
            im = Image.open(p).convert('RGBA'); n = im.width // 16
            side = im.crop(((n - 1) * 16, 0, n * 16, 32)); break
    # columna del borde de luz en el tile del borde izquierdo (el más claro de la fila del medio)
    rimx = max(range(16), key=lambda x: lum(t['el'].getpixel((x, 8))))
    return dict(name=name, style=next(st for n, st, *_ in THEMES if n == name), tiles=t, arch=arch, mask=mask, rim=rim + (255,), lip=lip, side=side, rimx=rimx)

def blobs(cv, lobes, pal, outline=True):
    """Masa de metabolas sombreada como cúpula (luz arriba a la izquierda)."""
    inside = lambda x, y: sum(r * r / max(.01, (x - lx) ** 2 + ((y - ly) / .9) ** 2) for lx, ly, r in lobes) >= 1.0
    xs = [l[0] for l in lobes]; ys = [l[1] for l in lobes]; rs = [l[2] for l in lobes]
    cx, cy = sum(xs) / len(xs), sum(ys) / len(ys)
    rx = (max(xs) - min(xs)) / 2 + max(rs); ry = (max(ys) - min(ys)) / 2 + max(rs)
    fill(cv, inside, cx, cy, rx, ry, pal)
    if outline: cv.outline(); cv.clean()
    return cv

def dilate(pts, r):
    out = set()
    for (x, y) in pts:
        for dy in range(-r, r + 1):
            for dx in range(-r, r + 1):
                if dx * dx + dy * dy <= r * r + r * .6: out.add((x + dx, y + dy))
    return out

# =================================================================================================
# Corona del arco de arriba (48×24): el arco (32 de ancho) va centrado; la fila 16 de la corona es la de arriba de la cara.
# =================================================================================================
CW, CH = 48, 24
FACE_TOP = 16

def frond(cv, pal, x0, y0, ang, length, side):
    """Fronda de helecho: raquis que se curva y folíolos a los dos lados, más cortos hacia la punta."""
    pts = []
    for s in range(length):
        t_ = s / length
        a = ang + side * t_ * .9                    # el raquis se curva hacia abajo
        x = x0 + sum(math.cos(ang + side * (k / length) * .9) for k in range(s))
        y = y0 + sum(math.sin(ang + side * (k / length) * .9) for k in range(s))
        pts.append((x, y, a, t_))
    for (x, y, a, t_) in pts:                        # folíolos a los dos lados, más cortos hacia la punta
        fl = 3.2 * (1 - t_) + .8
        for sgn in (-1, 1):
            la = a + sgn * 1.15
            for k in range(1, int(fl) + 1):
                c = pal[4] if (k == 1 and sgn < 0) else pal[3] if k < fl - .5 else pal[2]
                cv.put(x + math.cos(la) * k, y + math.sin(la) * k + k * .35, C(c))
    for (x, y, a, t_) in pts:
        cv.put(x, y, C(pal[4] if t_ < .5 else pal[3]))

def crown(th, style, name):
    cv = Canvas(CW, CH)
    t = th['tiles']
    rnd = random.Random(sum(map(ord, name)))
    M = {(x + 8, y + FACE_TOP) for (x, y) in th['mask']}
    if style in (1, 2):
        hump = {p for p in dilate(M, 10) if p[1] < FACE_TOP and 0 <= p[0] < CW and 0 <= p[1] < CH}
        for (x, y) in hump:                                  # la cara sigue hacia arriba
            cv.put(x, y, tex(t['fu'], x - 8, y - FACE_TOP + 16))
        for (x, y) in hump:                                  # labio de luz por el borde de arriba y sombra en la tapa
            if (x, y - 1) not in hump: cv.put(x, y, th['lip'])
        for (x, y) in hump:
            for (dx, dy) in ((1, 0), (0, -1), (-1, 0), (1, -1), (-1, -1)):
                q = (x + dx, y + dy)
                if q not in hump and 0 <= q[1] < FACE_TOP and 0 <= q[0] < CW:
                    cv.put(q[0], q[1], darker(tex(t['cap'], q[0], q[1]), .6) if dx >= 0 else darker(tex(t['cap'], q[0], q[1]), .8))
        cx = 24
        top = min(y for (_, y) in hump)
        if style == 1:                                       # venas que bajan por la joroba
            vein = darker(tex(t['fu'], 3, 6), .7)
            for a in (-.9, -.35, .35, .9):
                for s in range(2, 8):
                    x, y = cx + math.sin(a) * (7 + s) + rnd.uniform(-.4, .4), FACE_TOP - math.cos(a) * (7 + s) * .55 + 2
                    if (int(x), int(y)) in hump and (int(x), int(y) - 1) in hump: cv.put(x, y, vein)
            for (nx, ny) in ((cx - 9, FACE_TOP - 4), (cx + 8, FACE_TOP - 5)):   # nódulos de la membrana
                for (dx, dy, k) in ((0, 0, 1.1), (1, 0, .95), (0, 1, .8), (1, 1, .7), (0, -1, 1.25)):
                    if (nx + dx, ny + dy) in hump: cv.put(nx + dx, ny + dy, darker(tex(t['fu'], 4, 4), k) if k < 1 else th['lip'])
        else:                                                # dovelas: juntas que salen del arco
            joint = darker(tex(t['fu'], 3, 6), .6)
            for a in (-1.1, -.55, 0, .55, 1.1):
                for s in range(0, 6):
                    x = cx + math.sin(a) * (10 + s); y = FACE_TOP + 1 - math.cos(a) * (8 + s) * .8
                    if (int(x), int(y)) in hump and (int(x), int(y) - 1) in hump: cv.put(x, y, joint)
            for (x, y) in hump:                              # clave del arco, algo más clara
                if abs(x - cx) <= 2 and y > min(yy for (_, yy) in hump) and (x, y - 1) in hump:
                    c = tex(t['fu'], x - 8, y - FACE_TOP + 16); cv.put(x, y, (min(255, int(c[0] * 1.15)), min(255, int(c[1] * 1.15)), min(255, int(c[2] * 1.12)), 255))
            algae = ACCENT[name]['algae']                     # algas que cuelgan del labio a los lados
            for x in (cx - 14, cx - 11, cx + 11, cx + 15):
                ys = [y for (xx, y) in hump if xx == x]
                if not ys: continue
                y0 = min(ys); L = rnd.randint(3, 6)
                for s in range(L): cv.put(x + math.sin(s + x) * .6, y0 + 1 + s, C(algae[1 if s < L - 2 else 0]))
    else:                                                    # la espesura se derrama sobre el arco
        acc = ACCENT[name]
        # Rampa de la tapa (sus propios colores, de oscuro a claro): las copas no se salen de la paleta de la espesura
        cnt = Counter(p[:3] for p in t['cap'].getdata() if p[3] > 200)
        cr = sorted([c for c, _ in cnt.most_common(6)], key=lum)
        cr = [cr[round(i * (len(cr) - 1) / 4)] for i in range(5)]
        pal = ['#%02x%02x%02x' % c for c in cr]
        # Frondas de helecho que cuelgan de la espesura y tapan la unión del arco con la tapa
        fronds = [(21, 10, 2.45, 13, 1), (27, 10, .7, 13, -1), (14, 12, 2.1, 9, 1), (34, 12, 1.05, 9, -1), (24, 8, 1.62, 10, 1)]
        for f in fronds: frond(cv, pal, *f)
        # contorno con el oscuro de la tapa alrededor de las frondas (más marcado por debajo)
        body = set(cv.px)
        for (x, y) in list(body):
            for q in ((x + 1, y), (x - 1, y), (x, y - 1), (x, y + 1)):
                if q not in body and 0 <= q[0] < CW and 0 <= q[1] < CH - 1: cv.put(q[0], q[1], C(pal[0]))
        for (x, y) in list(cv.px):                           # sombra de las frondas sobre la cara
            if y >= FACE_TOP and (x, y + 1) not in cv.px and y + 1 < CH - 1: cv.put(x, y + 1, C(hexc(OUTL), 100))
        over = set(cv.px)
        for x in (11, 15, 33, 37):                           # lianas a los lados del arco
            ys = [y for (xx, y) in over if xx == x]
            if not ys: continue
            y0 = max(ys); L = rnd.randint(3, 6)
            for s in range(L):
                if y0 + 1 + s < CH - 2: cv.put(x + math.sin(s * .8 + x) * .5, y0 + 1 + s, C(acc['vine'] if s < L - 1 else acc['tip']))
        if 'ember' in acc:
            for k in range(4):
                x, y = rnd.choice(sorted(over))
                cv.put(x, y, C(acc['ember'][k % 2]))
    return cv

# =================================================================================================
# Puerta lateral izquierda: CORONA QUE SOBRESALE (40×72). Columna de pared en x 0-15 (la del tile de borde), x 16-39 dentro
# de la sala. Filas 28-59, el hueco (2 celdas). Front: puente con su cara sobre la parte alta del hueco y, encima, una corona
# grande que se sale hacia la sala y cuelga sobre la entrada (el jugador pasa por debajo). Back: remate pequeño al pie del
# hueco y la sombra del puente en el suelo.
# =================================================================================================
SW, SH = 40, 72
OPEN0, OPEN1 = 28, 60
BRIDGE = 9                                                   # alto de la cara del puente
CORAL = {'Shore': ['#6a3a3a', '#9a5a48'], 'Shallows': ['#6a4a2a', '#9a7038'], 'Cambrian': ['#5a2a4a', '#7e4a6a'],
         'Abyss': ['#1a3a50', '#2a6a80']}

KELP = {'Shore': '#4a7a50', 'Shallows': '#3a8a64', 'Cambrian': '#3a7070', 'Abyss': '#2a5a6a'}

def kelp_ribbon(cv, pal, x0, y0, ang, length, side):
    """Cinta de alga: banda de 2 px que se arquea y ondula, clara por un lado; acaba en punta."""
    x, y = x0, y0
    for s in range(length):
        t_ = s / length
        a = ang + side * t_ * 1.1 + .25 * math.sin(s * .9)
        x += math.cos(a); y += math.sin(a) + .35 * t_
        cv.put(x, y, C(pal[3] if t_ < .7 else pal[2]))
        if t_ < .8: cv.put(x - math.sin(a), y + math.cos(a), C(pal[1]))
        if s % 4 == 1: cv.put(x + math.sin(a), y - math.cos(a), C(pal[4]))
    cv.put_out(x + 1, y, C(pal[0]))

def theme_ramp(im, n=5):
    cnt = Counter(px[:3] for px in im.getdata() if px[3] > 200)
    cr = sorted([c for c, _ in cnt.most_common(8)], key=lum)
    return [cr[round(i * (len(cr) - 1) / (n - 1))] + (255,) for i in range(n)]

def textured_mass(cv, lobes, texture, ramp, ox=0, oy=0, edge=None):
    """Masa de metabolas con la textura de un tile y el volumen de una cúpula (luz arriba a la izquierda)."""
    t2 = Canvas(cv.w, cv.h)
    blobs(t2, lobes, [hexc(c) for c in ramp[1:5]], outline=False)
    near = lambda c: min(range(len(ramp)), key=lambda i: sum(abs(a - b) for a, b in zip(c[:3], ramp[i][:3])))
    for p, c in t2.px.items():
        s = near(c)
        tx = near(tex(texture, p[0] + ox, p[1] + oy))
        cv.px[p] = ramp[max(0, min(4, round(.55 * s + .6 * tx - .3)))]
    body = set(t2.px)
    for (x, y) in list(body):                                # contorno con el oscuro del propio tema
        for q in ((x + 1, y), (x - 1, y), (x, y - 1), (x, y + 1)):
            if q not in body and 0 <= q[0] < cv.w and 0 <= q[1] < cv.h and q not in cv.px: cv.put(q[0], q[1], edge or darker(ramp[0], .8))
    return body

def side_frame(th, style, name):
    front, back = Canvas(SW, SH), Canvas(SW, SH)
    t = th['tiles']; rx = th['rimx']
    rnd = random.Random(sum(map(ord, name)) + 7)
    y_end = OPEN0 + BRIDGE
    # Sombra del puente y de la corona en el suelo del hueco y un poco dentro de la sala
    for y in range(y_end, y_end + 5):
        for x in range(0, 26):
            if x > 16 + (y - y_end) * 2: continue
            back.put(x, y, C(hexc(OUTL), 100 - (y - y_end) * 18))
    # Puente: la cara de la pared sobre la parte alta del hueco
    for x in range(0, rx + 1):
        front.put(x, OPEN0, th['lip'])
        for y in range(OPEN0 + 1, y_end):
            front.put(x, y, darker(tex(t['fl'], x + 3, 16 - (y_end - y)), .85))
    for x in range(0, rx + 2): front.put_out(x, y_end, C(hexc(OUTL), 150))
    if style in (1, 2):
        face = theme_ramp(t['fu'])
        if style == 1:
            # Pliegue de membrana: un reborde bajo sobre el hueco y cilios que cuelgan sobre la entrada
            body = textured_mass(front, [(7, OPEN0 - 3, 4.6), (14, OPEN0 - 2, 3.8), (20, OPEN0, 2.8)], t['fu'], face, 3, 2)
            for (x, y) in body:
                if (x, y - 1) not in body: front.put(x, y, th['lip'])
            cil = [hexc(face[3]), hexc(face[2]), hexc(face[1])]
            for k, cx in enumerate((5, 8, 11, 14, 17, 20, 23)):
                bot = max((y for (x, y) in body if x == cx), default=OPEN0 - 1)
                L_ = 5 + (k * 7) % 5
                for s in range(L_):                          # cilio: curva suave hacia la sala, más claro en la punta
                    front.put(cx + s * s * .045 + math.sin(s * .7 + k) * .4, bot + 1 + s, C(cil[0] if s >= L_ - 2 else cil[1 if s > 1 else 2]))
            vein = darker(face[1], .75)
            for s in range(5): front.put_in(9 + s * .8, OPEN0 - 6 + s * .6, vein)
        else:
            # Reborde bajo de roca y cintas de alga que se arquean sobre la entrada; un coral encima
            body = textured_mass(front, [(6, OPEN0 - 3, 4.4), (13, OPEN0 - 2, 3.6)], t['cap'], theme_ramp(t['cap']), 1, 3)
            for (x, y) in body:
                if (x, y - 1) not in body: front.put(x, y, th['rim'])
            kelp = list(ramp(KELP[name], 5))
            for f in ((6, OPEN0 - 4, -.2, 14, 1), (10, OPEN0 - 3, .3, 13, 1), (4, OPEN0 - 5, -.9, 9, 1), (13, OPEN0 - 2, .9, 10, 1)):
                kelp_ribbon(front, kelp, *f)
            coral = CORAL[name]
            for s in range(5):
                front.put(7, OPEN0 - 7 - s, C(coral[1] if s < 4 else coral[0]))
                if s == 2: front.put(8, OPEN0 - 10, C(coral[1])); front.put(6, OPEN0 - 11, C(coral[0]))
        # Remate de abajo, pequeño: un bulto bajo con lo mismo
        body = textured_mass(back, [(7, OPEN1 + 3, 3.8), (12, OPEN1 + 4, 2.8)], t['fu'] if style == 1 else t['cap'],
                             face if style == 1 else theme_ramp(t['cap']), 3, 2)
        for (x, y) in body:
            if (x, y - 1) not in body: back.put(x, y, th['lip'] if style == 1 else th['rim'])
        if style == 2:
            kelp_ribbon(back, list(ramp(KELP[name], 5)), 9, OPEN1 + 2, -1.2, 7, -1)
    else:
        acc = ACCENT[name]
        leaf = theme_ramp(t['cap'])
        pal = [hexc(c) for c in leaf]
        # Mata de la espesura sobre el hueco y frondas que se salen hacia la sala y cuelgan sobre la entrada
        body = textured_mass(front, [(7, OPEN0 - 6, 6.6), (13, OPEN0 - 3, 5.0), (4, OPEN0 - 11, 4.6)], t['cap'], leaf, 2, 1)
        for f in ((10, OPEN0 - 7, .25, 15, 1), (9, OPEN0 - 9, -.35, 13, 1), (11, OPEN0 - 4, .9, 11, 1), (6, OPEN0 - 10, -1.1, 9, 1)):
            frond(front, pal, *f)
        fb = set(front.px)
        for (x, y) in list(fb):
            for q in ((x + 1, y), (x, y + 1), (x - 1, y), (x, y - 1)):
                if q not in fb and 0 <= q[0] < SW and 0 <= q[1] < SH: front.put(q[0], q[1], C(pal[0]))
        for cx in (14, 21, 27):                               # lianas
            bot = max((y for (x, y) in front.px if x == cx and y < OPEN0 + 12), default=None)
            if bot is None: continue
            L = rnd.randint(4, 7)
            for s in range(L): front.put(cx + math.sin(s * .8 + cx) * .5, bot + 1 + s, C(acc['vine'] if s < L - 1 else acc['tip']))
        if 'ember' in acc:
            for k, p in enumerate(sorted(body)[::23][:4]): front.put(p[0], p[1], C(acc['ember'][k % 2]))
        # Remate de abajo: mata pequeña
        textured_mass(back, [(8, OPEN1 + 4, 4.8), (14, OPEN1 + 5, 3.4)], t['cap'], leaf, 2, 1)
        for f in ((10, OPEN1 + 3, .5, 7, 1), (8, OPEN1 + 2, -.4, 6, 1)): frond(back, pal, *f)
    return front, back

# =================================================================================================
# Adornos sueltos para la pared lateral (24×16: x 0-15 sobre el tile del borde izquierdo, 16-23 dentro de la sala;
# la derecha con Flip X). Pequeños: rompen la línea de la pared sin cargarla.
# =================================================================================================
def wall_deco(th, style, name, k):
    cv = Canvas(24, 16)
    t = th['tiles']; rx = th['rimx']
    rnd = random.Random(sum(map(ord, name)) * 3 + k)
    if style == 1:                                           # matita de cilios que asoma por el borde
        face = theme_ramp(t['fu'])
        for j in range(3 + k):
            x0 = rx - 1 + j * 1.3
            L_ = 3 + (j * 5 + k) % 4
            for s in range(L_):
                cv.put(x0 + s * s * .06, 12 - s, C(hexc(face[3]) if s >= L_ - 1 else hexc(face[2])))
        cv.put(rx - 1, 13, th['lip']); cv.put(rx, 13, th['lip']); cv.put(rx + 1, 13, C(hexc(face[1])))
    elif style == 2:                                         # matita de algas (y un coral en una de ellas)
        kelp = list(ramp(KELP[name], 5))
        for j in range(2 + k):
            kelp_ribbon(cv, kelp, rx - 1 + j, 13, -1.35 + j * .35, 6 + j, 1 if j % 2 else -1)
        if k == 2:
            coral = CORAL[name]
            for s in range(4): cv.put(rx - 2, 12 - s, C(coral[1] if s < 3 else coral[0]))
    else:
        leaf = theme_ramp(t['cap']); pal = [hexc(c) for c in leaf]
        textured_mass(cv, [(rx - 2, 8, 3.0 + k * .4)], t['cap'], leaf, 0, 0)
        for f in ((rx - 1, 7, .2 + k * .3, 6 + k, 1), (rx - 2, 6, -.5, 5, 1)): frond(cv, pal, *f)
        b = set(cv.px)
        for (x, y) in list(b):
            for q in ((x + 1, y), (x, y + 1)):
                if q not in b and 0 <= q[0] < 24 and 0 <= q[1] < 16: cv.put(q[0], q[1], C(pal[0]))
        if 'ember' in ACCENT[name]: cv.put(rx - 2, 8, C(ACCENT[name]['ember'][1]))
    return cv
# =================================================================================================
# Maquetas de revisión: un trozo de sala con los tiles del tema, antes y después
# =================================================================================================
def mock_top(th, crown_img=None):
    t = th['tiles']; W, H = 7, 6
    im = Image.new('RGBA', (W * 16, H * 16))
    for x in range(W):
        im.alpha_composite(t['cap'], (x * 16, 0))
        im.alpha_composite(t['fu'], (x * 16, 16)); im.alpha_composite(t['fl'], (x * 16, 32))
        im.alpha_composite(t['shadow'], (x * 16, 48))
        for y in range(4, H): im.alpha_composite(t['floor'], (x * 16, y * 16))
    ax = 40
    im.alpha_composite(th['arch'], (ax, 16))
    if crown_img is not None:
        im.alpha_composite(as_img(crown_img), (ax + 16 - CW // 2, 16 - FACE_TOP))
    return im

def mock_side(th, frame=None, closed=False):
    t = th['tiles']; W, H = 5, 7
    im = Image.new('RGBA', (W * 16, H * 16))
    for y in range(H):
        for x in range(1, W): im.alpha_composite(t['floor'], (x * 16, y * 16))
        im.alpha_composite(t['cap'], (0, y * 16))
        if y not in (3, 4): im.alpha_composite(t['el'], (16, y * 16))
        else: im.alpha_composite(t['floor'], (16, y * 16))
    if closed and th['side'] is not None: im.alpha_composite(th['side'], (16, 48))
    if frame is not None:
        f, b = frame
        for k, row in ((0, 1), (2, 6)): im.alpha_composite(as_img(wall_deco(th, th['style'], th['name'], k)), (16, row * 16))
        im.alpha_composite(as_img(b), (16, 48 - OPEN0))
        im.alpha_composite(as_img(f), (16, 48 - OPEN0))
    return im

if __name__ == '__main__':
    rows_top, rows_side = [], []
    for name, style, folder, idx in THEMES:
        th = load_theme(name, folder, idx)
        cr = crown(th, style, name)
        fr, bk = side_frame(th, style, name)
        d = os.path.join(OUT, name)
        save_sheet(os.path.join(d, 'Doorway_Top_Crown.png'), [cr])
        save_sheet(os.path.join(d, 'Doorway_Side_Front.png'), [fr])
        save_sheet(os.path.join(d, 'Doorway_Side_Back.png'), [bk])
        for k in range(3): save_sheet(os.path.join(d, f'Wall_Side_Deco_{k}.png'), [wall_deco(th, style, name, k)])
        rows_top.append((name, mock_top(th), mock_top(th, cr)))
        rows_side.append((name, mock_side(th, closed=True), mock_side(th, (fr, bk), closed=True), mock_side(th, (fr, bk))))
    for fname, rows in (('vista_previa_coronas.png', rows_top), ('vista_previa_laterales.png', rows_side)):
        s = 3
        w = sum(r.width * s + 8 for r in rows[0][1:]); h = rows[0][1].height * s + 8
        out = Image.new('RGBA', (w, h * len(rows)), C('#101018'))
        for i, (name, *ims) in enumerate(rows):
            x = 0
            for im in ims:
                out.alpha_composite(im.resize((im.width * s, im.height * s), Image.NEAREST), (x, i * h)); x += im.width * s + 8
        out.save(os.path.join(OUT, fname))
    print('Listo:', OUT)
