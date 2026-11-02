"""Escenario del piso 3 de Primordia (tanda 1b): plantas, charcos, obstáculos y animaciones del pantano.

Por tramo (Swamp, Smoke, Burnt), en <Tema>/props/: helechos de 5 tipos que se mecen (y arden), charcos de lodo y de
agua en 7 formas, obstáculos, odres de metano, musgo que gotea, fumarolas, ascuas, humo, metano, bichos y detalles
del suelo. Más una escena de muestra por tramo armada con los tiles de la tanda 1a. Ver README.md.

Uso: python generar_escenario_piso3.py   (antes, generar_tilesets_piso3.py: la escena usa sus tiles)
"""
import math, os, random
from base_piso3 import *
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
KEYS = ('Swamp', 'Smoke', 'Burnt')
SEED = {'Swamp': 500, 'Smoke': 540, 'Burnt': 580}
TRUNK = ['#241a18', '#36281f', '#4c3828', '#664d34']      # manto de raíces de Psaronius
STONE = ['#2e2c34', '#4a4650', '#6a6670', '#8e8a90']
SHADOW = (24, 20, 37, 90)                                  # sombra en el suelo (OUTL translúcido)

def plant_ramp(t): return FERN if t['key'] == 'Swamp' else FERN_DRY if t['key'] == 'Smoke' else CHAR

def ground_shadow(cv, cx, cy, rx, ry):
    for y in range(int(cy - ry), int(cy + ry) + 1):
        for x in range(int(cx - rx), int(cx + rx) + 1):
            if ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 <= 1: cv.put_out(x, y, SHADOW)

# =================================================================================================
# Helechos (de la hoja de estilo, con vaivén)
# =================================================================================================
FERN_KINDS = {          # frondas detrás, delante, ancho de foliolo, paso entre foliolos, largo, caída de la punta
    'Leafy':  (5, 6, 5.5, 2, 1.0, .34),     # frondoso
    'Sparse': (1, 3, 3.0, 3, 1.05, .30),    # ralo
    'Young':  (0, 3, 3.5, 2, .55, .25),     # joven, con báculos
    'Bent':   (2, 4, 4.5, 2, .95, .55),     # inclinado
    'Tree':   (3, 5, 4.5, 2, .72, .75),     # arborescente (Psaronius)
}
FERN_CELL = {'Leafy': (84, 50, 45), 'Sparse': (92, 50, 45), 'Young': (44, 34, 29), 'Bent': (88, 64, 45), 'Tree': (84, 74, 69)}   # ancho, alto, pie

def fiddle(cv, x, y, h, rp, left, sway=0.0):
    """Báculo: fronda joven que sube y se enrolla en espiral en la punta."""
    sd = -1 if left else 1
    for k in range(h):
        cv.put(x + round(math.sin(k / h * 1.4) * 1.5) * sd + sway * k / h, y - k, rp[2] if k % 3 else rp[1])
    tx, ty = x + round(math.sin(1.4) * 1.5) * sd + sway, y - h
    for k in range(13):
        a = k * .7; rr = 2.0 * (1 - k / 14)
        cv.put(tx + sd * (rr - rr * math.cos(a)), ty - rr * math.sin(a), rp[3] if a < 3.3 else rp[2])
    cv.put(tx + sd * 2, ty - 1, rp[4] if len(rp) > 4 else rp[3])

def fern(kind, rmp, seed, sway=0.0, burnt=0.0, fire=-1, flip=False):
    """Helecho en su celda, con el pie en el centro de abajo. sway -1..1 mece las frondas (más las puntas y las de
    delante); burnt 0..1 parte quemada desde las puntas; fire >= 0 pone llamas (cada valor, otras llamas)."""
    w, h, by = FERN_CELL[kind]
    cv = Canvas(w, h); bx = w // 2
    rnd = random.Random(seed)
    nb, nf, lw, step, ls, droop = FERN_KINDS[kind]
    rp = [C(c) for c in rmp]; ch = [C(c) for c in CHAR]
    lean = (.45 if kind == 'Bent' else 0) * (-1 if flip else 1)
    top_y = by
    if kind == 'Tree':
        tr = ch if burnt > .8 else [C(c) for c in TRUNK]
        top_y = by - 22
        for y in range(top_y, by + 1):
            for dx in range(-2, 3):
                k = (y + (dx + 2) * 2) % 5
                i = 0 if dx == 2 else 3 if dx == -2 and k < 3 else 2 if k < 2 else 1
                cv.put(bx + dx, y, tr[i])
        for dx in (-3, -2, 2, 3):
            cv.put(bx + dx, by, tr[1]); cv.put(bx + dx * 1.3, by + 1, tr[0])
        lo, hi = -1.7, 1.7
    else:
        lo, hi = -1.0, 1.0
    def spread(n, a, b):
        if n == 0: return []
        if n == 1: return [rnd.uniform(-.2, .2)]
        return [a + (b - a) * i / (n - 1) + rnd.uniform(-.12, .12) for i in range(n)]
    back = [(a + lean, rnd.uniform(28, 34) * ls) for a in spread(nb, lo, hi)]
    front = [(a + lean, rnd.uniform(34, 42) * ls) for a in spread(nf, lo * .8, hi * .8)]
    fr = random.Random(seed * 101 + max(0, fire))
    tips = []
    for layer, fronds in enumerate((back, front)):
        for fi, (ang, L) in enumerate(fronds):
            side = -1 if ang < 0 else 1
            sw = sway * (.05 + .05 * layer) * (1 + .4 * math.sin(fi * 2.1))    # cada fronda, un poco distinta
            pts = []
            for st in range(int(L)):
                t = st / L
                a = ang + sw * t * 1.6
                pts.append((bx + math.sin(a) * st + side * t * t * L * .22,
                            top_y - math.cos(a) * st + t * t * L * droop, t))
            for j, (x, y, t) in enumerate(pts):
                scorched = t > 1 - burnt
                pal = ch if scorched else rp
                d = 1 - layer
                q = pts[min(j + 1, len(pts) - 1)]
                nx, ny = q[0] - x, q[1] - y
                nn = math.hypot(nx, ny) or 1; nx, ny = nx / nn, ny / nn
                if j % step == 0 and t < .94:
                    ww = 1 + round((1 - t) * lw * (1 - .5 * t))
                    for sgn in (-1, 1):
                        px, py = -ny * sgn, nx * sgn
                        lit = (px * -.6 + py * -.8) > 0
                        for k in range(1, ww + 1):
                            tone = (3 if lit else 1) if k < ww else (2 if lit else 0)
                            cv.put(x + (px * .85 + nx * .5) * k, y + (py * .85 + ny * .5) * k, pal[max(0, tone - d)])
                cv.put(x, y, pal[max(0, 2 - d)] if not scorched else ch[2])
                if scorched and fire >= 0 and fr.random() < .08: tips.append((x, y, 2))
            tips.append(pts[-1])
    if kind == 'Young':
        for k in range(2): fiddle(cv, bx + (k * 2 - 1) * rnd.randrange(2, 5), by, rnd.randrange(8, 13), rp if burnt < .5 else ch, k == 0, sway * .8)
    elif kind == 'Leafy' and rnd.random() < .6:
        fiddle(cv, bx + rnd.randrange(-4, 5), by, rnd.randrange(10, 15), rp if burnt < .5 else ch, rnd.random() < .5, sway * .8)
    if kind != 'Tree':
        for x in range(-2, 3):
            for y in range(-2, 1): cv.put(bx + x, by + y, rp[0] if burnt < .9 else ch[1])
    if fire >= 0:
        for x, y, t in tips:
            if t == 2 or burnt > .3: flame(cv, x, y - 1, fr)
    ground_shadow(cv, bx, by + 1, 6 if kind != 'Young' else 4, 2)
    if burnt >= 1 and fire < 0:                       # quemado: alguna ascua que queda
        for k in range(2):
            x, y = rnd.randrange(bx - 8, bx + 8), rnd.randrange(by - 14, by - 2)
            if (x, y) in cv.px: cv.px[(x, y)] = C(EMBER[2])
    return cv

RUSTLE = (1.9, -1.4, .9, -.5, .22, 0)              # al atravesarlo: sacudida que se calma

def fern_set(t, seed):
    """Por tipo: vaivén de 4 cuadros; en pantano y humeante, también ardiendo (4) y quemado (1)."""
    out = {}
    rm = plant_ramp(t)
    for i, kind in enumerate(FERN_KINDS):
        s = seed + i * 7
        burnt = 1.0 if t['burnt'] else 0.0
        amp = .4 if t['burnt'] else 1.0               # lo quemado está tieso
        out[f'Fern_{kind}_Sway'] = [fern(kind, rm, s, amp * math.sin(f * math.pi / 2), burnt) for f in range(4)]
        rus = [fern(kind, rm, s, amp * a, burnt) for a in RUSTLE]
        bits = [C(c) for c in (FERN_DRY[2:4] if t['key'] == 'Smoke' else ('#6d6466', '#958a88'))]
        if t['key'] != 'Swamp':                       # hojitas secas o ceniza que caen al sacudirse
            w, h, by = FERN_CELL[kind]; r2 = random.Random(s)
            drops = [(w // 2 + r2.randrange(-14, 15), by - r2.randrange(14, 26)) for _ in range(3)]
            for f, cv in enumerate(rus[1:5], 1):
                for k, (x, y) in enumerate(drops):
                    cv.put(x + round(math.sin(f + k) * 1.5), y + f * 3, bits[k % 2])
        out[f'Fern_{kind}_Rustle'] = rus
        if not t['burnt']:
            out[f'Fern_{kind}_Burn'] = [fern(kind, rm, s, .5 * math.sin(f * math.pi / 2), .55 + .1 * f, f) for f in range(4)]
            out[f'Fern_{kind}_Burnt'] = [fern(kind, rm, s, 0, 1.0)]
    return out

# =================================================================================================
# Charcos (lodo que frena, agua de charca) en 7 formas
# =================================================================================================
LIQ = {'Sludge': (SLUDGE, False, True), 'Water': (WATER, True, False)}   # rampa, reflejos, brillo de barro
PUDDLES = [('redondo', 30, 18), ('alargado', 54, 22), ('lobulado', 42, 28), ('cadena', 58, 22),
           ('isla', 46, 28), ('salpicado', 40, 26), ('rinon', 42, 26)]
PUDDLE_NAMES = {'redondo': 'Round', 'alargado': 'Long', 'lobulado': 'Lobed', 'cadena': 'Chain', 'isla': 'Island',
                'salpicado': 'Splash', 'rinon': 'Kidney'}

def puddle(t, kind, shape, w, h, seed, frame=0):
    """Charco en un lienzo de (w+4)×(h+4). Arriba, la sombra del borde; abajo, la orilla mojada que coge luz; dentro,
    reflejos que se mueven (agua) o brillo y burbujas (lodo). En la tierra quemada, con una película de ceniza."""
    rmp, refl, gloss = LIQ[kind]
    r = [C(c) for c in rmp]
    if t['burnt']: r = [blend(c, C('#4a4346'), 140 if kind == 'Water' else 80) for c in r]
    top = len(r) - 1; b = 2 if top >= 4 else 1
    mask = {(x + 2, y + 1) for (x, y) in blob(seed, shape, w, h)}
    cv = Canvas(w + 4, h + 4)
    n = noise(seed + 9); rnd = random.Random(seed)
    dist = {p: 1 for p in mask if any((p[0] + dx, p[1] + dy) not in mask for dx, dy in N4)}
    for k in (2, 3, 4):
        for p in [p for p in mask if p not in dist and any(dist.get((p[0] + dx, p[1] + dy)) == k - 1 for dx, dy in N4)]:
            dist[p] = k
    wet = darker(t['floor'][0], .78)                  # orilla mojada: el suelo justo debajo y a los lados
    for (x, y) in mask:
        for dx, dy in ((0, 1), (1, 0), (-1, 0), (0, 2)):
            if (x + dx, y + dy) not in mask: cv.put(x + dx, y + dy, wet)
    for (x, y) in mask:
        d = dist.get((x, y), 5)
        if (x, y - 1) not in mask: i = 0
        elif (x, y + 1) not in mask: i = top - 1
        elif d == 1: i = max(0, b - 1)
        elif (x, y - 2) not in mask: i = max(0, b - 1)
        else: i = (b - 1 if n(x, y, 5) < .38 else b + 1 if n(x, y, 3) > .78 else b) if gloss else b
        cv.px[(x, y)] = r[i]
    inner = sorted(p for p in mask if dist.get(p, 5) >= 3)
    if refl and inner:                                # reflejos: rayitas que se deslizan (4 cuadros)
        mx = sum(p[0] for p in inner) / len(inner); my = sum(p[1] for p in inner) / len(inner)
        cand = [p for p in inner if p[0] < mx + 2 and p[1] < my + 1] or inner
        for k in range(max(1, len(inner) // 60) + 1):
            x, y = rnd.choice(cand); L = rnd.randrange(2, 5)
            sh = [0, 1, 1, 0][(frame + k) % 4]
            for j in range(L):
                if (x + j + sh, y) in mask: cv.px[(x + j + sh, y)] = r[top] if j and (frame + k) % 4 != 3 else r[top - 1]
        if t['burnt']:                                # motas de ceniza flotando
            for k in range(len(inner) // 25):
                x, y = rnd.choice(inner); cv.px[(x, y)] = C('#6d6466')
    if gloss and inner:                               # brillo pegajoso y una burbuja que sube y revienta
        for k in range(max(1, len(inner) // 70)):
            x, y = rnd.choice(inner)
            cv.px[(x, y)] = r[top - 1]; cv.put(x + 1, y, r[top])
        x, y = rnd.choice(inner)
        p = frame % 4
        if p == 1: cv.px[(x, y)] = r[top]
        elif p == 2: cv.px[(x, y)] = r[top]; cv.put(x + 1, y, r[top - 1]); cv.put(x, y - 1, r[top - 1])
        elif p == 3:
            for j in (-2, -1, 2, 3):
                if (x + j, y + 1) in mask: cv.px[(x + j, y + 1)] = r[b + 1]
    return cv

def puddle_set(t, seed):
    out = {}
    for kind in LIQ:
        for i, (shape, w, h) in enumerate(PUDDLES):
            s = seed + i * 13 + (0 if kind == 'Sludge' else 400)
            out[f'Puddle_{kind}_{PUDDLE_NAMES[shape]}'] = [puddle(t, kind, shape, w, h, s, f) for f in range(4)]
    return out

# =================================================================================================
# Obstáculos (bloquean): roca y tocón (1×1), Sigillaria, tronco partido y montículo de raíces (2×2)
# =================================================================================================
def rock(t, seed):
    cv = Canvas(16, 16); rnd = random.Random(seed)
    ellipse(cv, 8, 10, 6, 4.5, STONE)
    ellipse(cv, 5.5, 11.5, 3, 2.5, STONE, shift=-1)
    for x in range(16):                               # lo de encima: musgo, hojarasca o ceniza
        for y in range(16):
            if (x, y) in cv.px and (x, y - 1) not in cv.px or (x, y) in cv.px and (x, y - 2) not in cv.px and rnd.random() < .5:
                if t['key'] == 'Swamp': cv.px[(x, y)] = C(t['fleck'][1 if (x + y) % 3 else 2])
                elif t['key'] == 'Smoke': cv.px[(x, y)] = C(FERN_DRY[2 if (x + y) % 3 else 3]) if rnd.random() < .6 else cv.px[(x, y)]
                else: cv.px[(x, y)] = C('#6d6466' if (x + y) % 2 else '#958a88')
    cv.outline(); cv.clean(); ground_shadow(cv, 8, 14.5, 7, 1.6)
    return cv

def stump(t, seed):
    """Tocón bajo: anillos arriba, corteza en rombo, raíces al pie. En la tierra quemada, carbonizado con ascuas."""
    cv = Canvas(16, 20); rnd = random.Random(seed)
    bark = [C(c) for c in (CHAR if t['burnt'] else t['bark'] if t['key'] == 'Smoke' else BARK)]
    bark_trunk(cv, 3, 10, 7, 17, bark, EMBER[2] if t['burnt'] else None, seed, ember_p=.04)
    wood = [C(c) for c in (CHAR[2:] + ['#8a7a68'] if t['burnt'] else ['#6a5236', '#8e7048', '#b8955e', '#dcbc84'])]
    ellipse(cv, 8, 7, 5, 2.4, wood)
    for i in range(5, 11): cv.put(i, 7 if i % 2 else 6, wood[1])
    cv.put(8, 7, wood[0])
    if t['burnt']: cv.put(9, 6, C(EMBER[2])); cv.put(6, 7, C(EMBER[1]))
    elif t['key'] == 'Swamp':
        for i in range(4, 12):
            if rnd.random() < .6: cv.put(i, 5 + (i % 2), C(t['fleck'][1 + (i % 2)]))
    for dx, ang in ((2, 2.6), (13, .5)):              # raíces al pie
        root(cv, [(dx + (1 if dx < 8 else -1), 15), (dx, 16), (dx + (-2 if dx < 8 else 2), 17), (dx + (-3 if dx < 8 else 3), 18)], 2.2, 1, [C(c) for c in (CHAR if t['burnt'] else t['root'])])
    cv.outline(); cv.clean(); ground_shadow(cv, 8, 18.5, 7, 1.5)
    return cv

def sigillaria(t, seed):
    """Sigillaria joven (40×64, 2×2 de colisión abajo): tronco con costillas verticales y cicatrices en fila, y un
    penacho grande de hojas largas que se arquean. Quemada: sin hojas, la punta encendida."""
    cv = Canvas(40, 64); rnd = random.Random(seed)
    bark = [C(c) for c in (CHAR if t['burnt'] else BARK if t['key'] == 'Swamp' else t['bark'])]
    x0, x1, y0, y1 = 15, 25, 22, 59
    for y in range(y0, y1):
        fl = 1 if y > y1 - 4 else 0
        for x in range(x0 - fl, x1 + fl):
            u = (x - x0 + .5) / (x1 - x0)
            rib = (x - x0) % 3
            light = 4 if u < .22 else 3 if u < .45 else 2 if u < .8 else 1
            c = bark[max(0, light - 1)] if rib == 2 else bark[light]
            if rib == 1 and (y + x * 2) % 6 == 0: c = bark[max(0, light - 2)]
            if t['burnt'] and rib == 2 and rnd.random() < .05: c = C(EMBER[2])
            cv.put(x, y, c)
    if t['burnt']:
        for x in range(x0, x1):
            for jj in range(rnd.randrange(0, 4)): cv.px.pop((x, y0 + jj), None)
        cv.put(19, y0 + 3, C(EMBER[3])); cv.put(20, y0 + 3, C(FIRE[3])); cv.put(18, y0 + 4, C(EMBER[2]))
        for k in range(3): twig(cv, 20, y0 + 2, -math.pi / 2 + rnd.uniform(-1.2, 1.2), rnd.uniform(5, 8), [C(c) for c in CHAR], None, True, rnd)
    else:
        lp = [C(c) for c in plant_ramp(t)]
        leaves = sorted(((-math.pi / 2 + rnd.gauss(0, .7), rnd.uniform(10, 19)) for _ in range(40)), key=lambda q: -abs(q[0] + math.pi / 2))
        for a, L in leaves:                           # de fuera hacia dentro: las de en medio quedan delante
            front = abs(a + math.pi / 2) < .6
            for st in range(int(L)):
                tt = st / L
                x = 20 + math.cos(a) * st; y = y0 + 1 + math.sin(a) * st + tt * tt * L * (.25 + .5 * abs(math.cos(a)))
                cv.put(x, y, (lp[4] if tt < .25 else lp[3] if tt < .6 else lp[2]) if front else (lp[2] if tt < .5 else lp[1]))
        for x in range(x0, x1): cv.put(x, y0, lp[1]); cv.put(x, y0 + 1, lp[0])
    rp = [C(c) for c in (CHAR if t['burnt'] else t['root'])]
    root(cv, [(x0, 56), (x0 - 2, 58), (x0 - 4, 60), (x0 - 6, 61)], 3, 1.2, rp)
    root(cv, [(x1 - 1, 56), (x1 + 1, 58), (x1 + 3, 60), (x1 + 5, 61)], 3, 1.2, rp)
    cv.outline(); cv.clean(); ground_shadow(cv, 20, 61.5, 11, 2.2)
    return cv

def broken_trunk(t, seed):
    """Tronco partido de Lepidodendron (32×44): corteza en rombo, rotura astillada con la madera clara, raíces que
    se abren al pie. Pantano: musgo en la rotura. Quemada: ascuas en la rotura."""
    cv = Canvas(32, 44); rnd = random.Random(seed)
    bark = [C(c) for c in (CHAR if t['burnt'] else BARK if t['key'] == 'Swamp' else t['bark'])]
    bark_trunk(cv, 8, 16, 10, 38, bark, EMBER[2] if t['burnt'] else None, seed, ember_p=.03)
    wood = [C(c) for c in (CHAR[2:] if t['burnt'] else ['#8e7048', '#b8955e', '#dcbc84'])]
    tops = [10 + round(abs(math.sin(i * 1.7 + 1)) * 6) + (rnd.randrange(0, 2)) for i in range(16)]
    for i in range(16):                               # rotura astillada
        x = 8 + i
        for y in range(10, tops[i]): cv.px.pop((x, y), None)
        cv.put(x, tops[i], wood[2] if i < 6 else wood[1]); cv.put(x, tops[i] + 1, wood[1] if i < 8 else wood[0])
        if i % 4 == 1: cv.put(x, tops[i] - 1, wood[2]); cv.put(x, tops[i] - 2, wood[1])     # astillas
    if t['burnt']:
        for i in (3, 7, 12): cv.put(8 + i, tops[i] + 1, C(EMBER[2])); cv.put(8 + i, tops[i] + 2, C(EMBER[1]))
    elif t['key'] == 'Swamp':
        for i in range(16):
            if rnd.random() < .5: cv.put(8 + i, tops[i] - 1, C(t['vein'][1 if i % 2 else 2]))
        for k in range(3):                            # musgo que cuelga por el tronco
            mx = rnd.randrange(10, 22)
            for j in range(rnd.randrange(3, 8)): cv.put(mx, tops[mx - 8] + 2 + j, C(t['vein'][1 if j < 4 else 0]))
    rp = [C(c) for c in (CHAR if t['burnt'] else t['root'])]
    for dx, s in ((9, -1), (23, 1), (13, -1), (19, 1)):
        root(cv, [(dx, 34), (dx + s * 2, 37), (dx + s * 5, 39), (dx + s * 8, 41)], 3.5 if dx in (9, 23) else 2.5, 1.2, rp)
    cv.outline(); cv.clean(); ground_shadow(cv, 16, 41, 13, 2.4)
    return cv

def fallen_log(t, seed):
    """Tronco caído de Lepidodendron (40×28): corteza en rombo a lo largo, corte con anillos a la derecha, rotura
    astillada a la izquierda. Pantano: musgo encima; humeante: hojarasca; quemada: carbonizado con ascuas."""
    cv = Canvas(40, 28); rnd = random.Random(seed)
    bark = [C(c) for c in (CHAR if t['burnt'] else BARK if t['key'] == 'Swamp' else t['bark'])]
    tmp = Canvas(13, 32)
    bark_trunk(tmp, 0, 13, 0, 32, bark, EMBER[2] if t['burnt'] else None, seed, flare=False, ember_p=.025)
    for (x, y), c in tmp.px.items(): cv.put(3 + y, 8 + x, c)     # girado: la luz de la izquierda queda arriba
    wood = [C(c) for c in (CHAR[1:] if t['burnt'] else ['#4c3828', '#8e7048', '#b8955e', '#dcbc84'])]
    for y in range(8, 21):                            # corte con anillos
        for x in range(33, 38):
            d = math.hypot((x + .5 - 35) / 2.6, (y + .5 - 14.5) / 6.5)
            if d <= 1: cv.px[(x, y)] = wood[3] if d < .25 else wood[1] if d < .5 or .7 < d < .82 else wood[2]
    for y in range(8, 21):                            # rotura astillada
        for k in range(rnd.randrange(0, 3)): cv.px.pop((3 + k, y), None)
        if y % 3 == 0: cv.put(2, y, wood[2])
    if t['burnt']:
        for x in (10, 19, 27): cv.put(x, 8, C(EMBER[2])); cv.put(x + 1, 8, C(EMBER[1]))
        cv.put(35, 14, C(EMBER[3]))
    else:
        top = [C(c) for c in (t['vein'] if t['key'] == 'Swamp' else FERN_DRY[1:4])]
        for x in range(5, 33):                        # musgo u hojarasca encima
            if rnd.random() < .7: cv.put(x, 8, top[2] if x % 3 else top[1]); cv.put(x, 9, top[1] if rnd.random() < .5 else cv.px.get((x, 9)))
            if rnd.random() < .15:
                for jj in range(rnd.randrange(2, 5)): cv.put(x, 10 + jj, top[0])
    cv.outline(); cv.clean(); ground_shadow(cv, 20, 22.5, 18, 2.5)
    return cv

def pad(cv, l, tp, r, b):
    """Margen transparente alrededor (que nada toque el borde de la celda)."""
    out = Canvas(cv.w + l + r, cv.h + tp + b)
    for (x, y), c in cv.px.items(): out.px[(x + l, y + tp)] = c
    return out

def obstacle_set(t, seed):
    o = _obstacles(t, seed)
    return {k: [pad(v[0], 4, 0, 4, 2)] for k, v in o.items()}

def _obstacles(t, seed):
    return {'Obstacle_Rock': [rock(t, seed)], 'Obstacle_Stump': [stump(t, seed + 1)],
            'Obstacle_Sigillaria': [sigillaria(t, seed + 2)], 'Obstacle_BrokenTrunk': [broken_trunk(t, seed + 3)],
            'Obstacle_FallenLog': [fallen_log(t, seed + 4)]}

# =================================================================================================
# Odre de metano: bolsa de membrana llena de gas que respira (explota con cualquier ataque: efecto en la tanda 5)
# =================================================================================================
SAC = ['#2e3020', '#4a4a2c', '#6c6a3c', '#948e52', '#bdb57a']

def methane_sac(t, f):
    cv = Canvas(24, 24)
    k = 1 + .07 * math.sin(f * math.pi / 2)           # respira
    rx, ry = 7.5 * k, 6.5 * (2 - k)
    cx, cy = 12, 13.5 - (k - 1) * 8
    skin = [C(c) for c in SAC] if not t['burnt'] else [blend(C(c), C('#4a4346'), 90) for c in SAC]
    ellipse(cv, cx, cy, rx, ry, skin)
    gas = [C(c) for c in METHANE]
    for (x, y), c in list(cv.px.items()):             # el gas se ve a través de la membrana fina del centro
        d = ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2
        if d < .45 and (x + y + f) % 3 != 0: cv.px[(x, y)] = blend(c, gas[1], 110 + int(50 * (1 - d)))
        if d < .15: cv.px[(x, y)] = blend(c, gas[2], 150)
    for a in (0.6, 2.0, 3.6, 5.0):                    # venas de la membrana
        for st in range(int(rx * .9)):
            x, y = cx + math.cos(a) * st, cy + math.sin(a) * st * ry / rx
            if (int(x), int(y)) in cv.px and st > 1: cv.put(x, y, skin[1])
    cv.put(cx - 3, cy - 3, skin[4]); cv.put(cx - 2, cy - 4, skin[4]); cv.put(cx - 3, cy - 2, gas[3])   # brillo
    cv.put(cx, cy - ry - .5, skin[2]); cv.put(cx + 1, cy - ry - .5, skin[1])                         # boquilla
    rp = [C(c) for c in (CHAR if t['burnt'] else t['root'])]
    for s in (-1, 1): root(cv, [(cx + s * 3, cy + ry - 1), (cx + s * 5, cy + ry), (cx + s * 7, cy + ry + 1), (cx + s * 9, cy + ry + 1.5)], 2, 1, rp)
    cv.outline(); cv.clean(); ground_shadow(cv, 12, 21.5, 9, 1.8)
    return cv

# =================================================================================================
# Musgo que gotea (sobre la cara de la pared; la gota cae al suelo)
# =================================================================================================
def drip_moss(t, f, seed):
    """16×48: cuelga desde lo alto de la cara (2 filas) y la gota cae hasta la primera fila de suelo. 6 cuadros."""
    cv = Canvas(16, 48); rnd = random.Random(seed)
    vn = t['vein']
    strands = [(3, 9), (6, 13), (9, 8), (12, 11)]
    for i, (x, L) in enumerate(strands):
        for j in range(L):
            cv.put(x + round(math.sin(j / 3 + i) * .6), 3 + j, vn[2] if j < 2 else vn[1] if j < L - 2 else vn[0])
        cv.put(x - 1, 3 + L // 2, vn[1])
    tipx, tipy = 6, 3 + 13
    drop = WATER_C = [C(c) for c in WATER]
    if f == 0: cv.put(tipx, tipy, drop[3])
    elif f == 1: cv.put(tipx, tipy, drop[3]); cv.put(tipx, tipy + 1, drop[4])
    elif f == 2: cv.put(tipx, tipy + 8, drop[4]); cv.put(tipx, tipy + 7, drop[3])
    elif f == 3: cv.put(tipx, tipy + 19, drop[4]); cv.put(tipx, tipy + 17, drop[2])
    elif f == 4:                                      # salpica en el suelo
        for dx, dy in ((-2, -1), (2, -1), (-1, -2), (1, -2)): cv.put(tipx + dx, 42 + dy, drop[4] if abs(dx) == 1 else drop[3])
    elif f == 5:
        for dx in (-3, -2, 2, 3): cv.put(tipx + dx, 43, drop[2])
    return cv

# =================================================================================================
# Fumarola: grieta del suelo; avisa (burbujea y brilla) y suelta una columna de vapor
# =================================================================================================
CRUST = ['#4a4430', '#7a6e44', '#b4a462', '#e0d494']   # costra mineral (azufre)
STEAM = ['#8a948c', '#b8c2ba', '#e4ece6']

def vent_base(t, glow=0.0):
    """Boca de la fumarola (16×16): costra mineral clara alrededor de un agujero oscuro; dentro brilla al avisar."""
    cv = Canvas(16, 16)
    ellipse(cv, 8, 9.5, 6.2, 4, CRUST)
    inner = C(EMBER[0]) if t['burnt'] else C('#1a120e')
    for y in range(16):
        for x in range(16):
            d = ((x + .5 - 8) / 4) ** 2 + ((y + .5 - 9.8) / 2.3) ** 2
            if d <= 1:
                c = C('#0e0a0a') if y < 10 else inner
                cv.px[(x, y)] = blend(c, C(FIRE[2] if d < .4 else FIRE[1]), int(255 * glow * (1 - d * .6)))
    for x in range(4, 13): cv.put(x, 7 + (x % 3 == 0), CRUST[3] if x % 2 else CRUST[2])     # labio con luz arriba
    for x, y in ((2, 10), (13, 10), (4, 12), (11, 12)): cv.put(x, y, CRUST[0])          # grietas
    cv.outline(); cv.clean()
    return cv

def steam(cv, x, y, h, a, rnd, wobble=0):
    """Columna o voluta de vapor translúcida con trama (más densa en el centro)."""
    for yy in range(int(y - h), int(y)):
        k = (y - yy) / max(1, h)
        wdt = 1.5 + k * 3.2
        cx = x + math.sin(yy * .35 + wobble) * k * 1.6
        for xx in range(int(cx - wdt), int(cx + wdt) + 1):
            d = abs(xx + .5 - cx) / wdt
            if d > 1: continue
            dens = (1 - d) * (1 - k * .55)
            if dens * 16 > BAYER[yy % 4][xx % 4]:
                c = STEAM[2] if dens > .6 else STEAM[1] if dens > .3 else STEAM[0]
                cv.px[(xx, yy)] = blend(cv.px.get((xx, yy)), C(c), int(a * (.5 + .5 * dens)))

def vent_set(t):
    rnd = random.Random(7)
    idle = []
    for f in range(4):
        cv = vent_base(t, .1)
        big = Canvas(16, 32); paste(big, cv, 0, 16)
        steam(big, 8, 25, 9 + f % 2 * 3, 170, rnd, f)
        idle.append(big)
    warn = []
    for f in range(4):
        cv = vent_base(t, .5 + .5 * (f % 2))
        big = Canvas(16, 32); paste(big, cv, 0, 16)
        for k in range(3):                            # burbujas y bocanadas
            bx = 6 + (k * 3 + f) % 5; by = 25 - ((f + k) % 3) * 3
            big.put(bx, by, C(STEAM[2])); big.put(bx + 1, by, C(STEAM[1]))
        steam(big, 8, 26, 4 + f * 2, 130, rnd, f)
        warn.append(big)
    blast = []
    for f, (h, a) in enumerate(((14, 160), (40, 210), (46, 220), (46, 200), (36, 130), (24, 70))):
        cv = vent_base(t, 1.0 if f < 4 else .4)
        big = Canvas(16, 64); paste(big, cv, 0, 48)
        steam(big, 8, 58, h, a, rnd, f * .8)
        if f in (1, 2, 3): steam(big, 8, 58, h * .9, min(255, a + 30), rnd, f * .8 + 2)
        blast.append(big)
    return {'Vent_Idle': idle, 'Vent_Warn': warn, 'Vent_Blast': blast}

# =================================================================================================
# Ascuas, humo de tocones, metano, bichos y detalles del suelo
# =================================================================================================
def embers(f, seed):
    cv = Canvas(16, 16); rnd = random.Random(seed)
    for k in range(6):
        x, y = rnd.randrange(2, 14), rnd.randrange(4, 14)
        on = (k + f) % 4
        cv.put(x, y, C(EMBER[3] if on == 0 else EMBER[2] if on == 1 else EMBER[1] if on == 2 else EMBER[0]))
        if k % 2 == 0: cv.put(x + 1, y, C(CHAR[2])); cv.put(x - 1, y + 1, C('#6d6466'))
    return cv

def stump_smoke(f):
    """Humo que sube de un tocón quemado: 16×32, 6 cuadros en bucle; se pone encima del tocón."""
    cv = Canvas(16, 32)
    for i in range(3):
        ph = (f + i * 2) % 6 / 6
        y = 30 - ph * 26; x = 8 + math.sin(ph * 6 + i) * 2.2
        a = int(210 * (1 - ph) ** .7)
        r = 1.2 + ph * 2.6
        for yy in range(int(y - r), int(y + r) + 1):
            for xx in range(int(x - r), int(x + r) + 1):
                if math.hypot(xx + .5 - x, yy + .5 - y) <= r and (xx + yy) % 2 == 0:
                    cv.px[(xx, yy)] = blend(cv.px.get((xx, yy)), C('#8a8286'), a)
    return cv

def methane_mist(w, h, seed, phase):
    """Metano a ras de suelo (de la hoja de estilo): niebla baja muy translúcida, sin borde duro; vetas que se
    arrastran, volutas que suben y burbujitas donde brota. phase 0..1 para el bucle."""
    cv = Canvas(w, h); rnd = random.Random(seed)
    mask = {(x + 5, y + 8) for (x, y) in blob(seed, 'alargado', w - 10, h - 11)}
    dist = {q: 1 for q in mask if any((q[0] + dx, q[1] + dy) not in mask for dx, dy in N4)}
    for k in (2, 3, 4):
        for q in [q for q in mask if q not in dist and any(dist.get((q[0] + dx, q[1] + dy)) == k - 1 for dx, dy in N4)]:
            dist[q] = k
    ys = [q[1] for q in mask]; ytop, ybot = min(ys), max(ys)
    drift = phase * 6.283
    def add(x, y, c, a): cv.put(x, y, blend(cv.px.get((x, y)), C(c), a))
    for (x, y) in mask:
        d = dist.get((x, y), 5)
        dens = min(1, d / 4) * (.55 + .45 * (y - ytop) / max(1, ybot - ytop))
        dens *= .8 + .2 * math.sin(x / 5 + drift + y * .7)
        if dens * 16 > BAYER[y % 4][(x + int(phase * 8)) % 4] + .5: add(x, y, METHANE[2], 70 if dens > .6 else 50)
    for i in range(3):
        yl = ytop + 2 + i * max(2, (ybot - ytop - 3) // 3); off = rnd.randrange(0, 16)
        for x in range(w):
            u = (x - phase * 32 + off) % 16
            if u < 7:
                y = yl + round(math.sin((x + phase * 32) / 4 + i) * .7)
                if dist.get((x, y), 0) >= 2: add(x, y, METHANE[3], 150 if 1 < u < 6 else 80)
    for i in range(4):
        wx = rnd.randrange(w // 6, w * 5 // 6); ph = (phase + i / 4) % 1
        col = [q[1] for q in mask if abs(q[0] - wx) < 2]
        base_y = (min(col) + 2) if col else ytop
        for j in range(6):
            yy = base_y - j - round(ph * 5); xx = wx + round(math.sin(j * .9 + ph * 6 + i) * 1.3)
            a = int(150 * (1 - j / 6) * (1 - ph))
            if a > 20: add(xx, yy, METHANE[3], a)
    for i in range(3):
        bx = rnd.randrange(w // 4, w * 3 // 4); by = ybot - 2 - rnd.randrange(0, 3)
        if (phase * 2 + i * .37) % 1 < .45: add(bx, by, METHANE[3], 210); add(bx + 1, by, METHANE[2], 150)
    return cv

def methane_bubble(f):
    """Burbuja de metano que sube del lodo y revienta soltando una voluta cian (salas de burbujeo). 16×24, 6 cuadros."""
    cv = Canvas(16, 24); g = [C(c) for c in METHANE]; m = [C(c) for c in MUD]
    bx, by = 8, 19
    if f == 0: cv.put(bx, by, m[3]); cv.put(bx + 1, by, m[2])
    elif f in (1, 2):
        r = 1.5 + f
        for y in range(int(by - r * .8), by + 1):
            for x in range(int(bx - r), int(bx + r) + 1):
                if ((x + .5 - bx) / r) ** 2 + ((y + .5 - by) / (r * .8)) ** 2 <= 1:
                    cv.px[(x, y)] = m[3] if y < by - r * .4 else m[2]
        cv.put(bx - 1, by - round(r * .6), g[3]); cv.put(bx, by - round(r * .6), m[4])
    elif f == 3:
        for a in range(14):
            x, y = bx + .5 + math.cos(a * .45) * 4, by + .5 + math.sin(a * .45) * 2.4
            cv.put(x, y, m[3] if a % 2 else m[2])
        for j in range(5): cv.put(bx + (j % 2), by - 2 - j, blend(None, g[3], 200 - j * 30))
    else:
        for a in range(16):
            if a % 2: cv.put(bx + .5 + math.cos(a * .4) * 5.5, by + .5 + math.sin(a * .4) * 3, m[2])
        for j in range(7):
            yy = by - 4 - j - (f - 4) * 4; xx = bx + round(math.sin(j * .9 + f) * 1.4)
            cv.put(xx, yy, blend(None, g[2 if j > 3 else 3], max(30, 170 - j * 22 - (f - 4) * 50)))
    return cv

def beetle(f, run):
    """Escarabajo pequeño visto desde arriba (8×8, mira a la derecha): decoración que huye del jugador."""
    cv = Canvas(8, 8)
    body = ['#2a2018', '#4a3622', '#6e5232', '#9a7a4a']
    ellipse(cv, 4, 4, 2.6, 1.8, body)
    cv.put(6, 4, C(body[1])); cv.put(7, 4, C(body[0]))
    ph = f % 2 if run else 0
    for i, x in enumerate((2, 4, 5)):
        up = (i + ph) % 2
        cv.put(x, 1 + up, C(body[0])); cv.put(x, 6 - up, C(body[0]))
    cv.put(3, 3, C(body[3]))
    return cv

def gnats(f, seed):
    """Nube de mosquitos sobre el agua (16×16, 4 cuadros en bucle)."""
    cv = Canvas(16, 16); rnd = random.Random(seed)
    for k in range(6):
        a = rnd.uniform(0, 6.28) + f * 1.3 * (1 if k % 2 else -1); r = rnd.uniform(2, 5)
        x, y = 8 + math.cos(a) * r, 8 + math.sin(a) * r * .7
        cv.put(x, y, C('#1e1a1c'));
        if (k + f) % 3 == 0: cv.put(x, y - 1, C('#8a948c'))
    return cv

DECO_KINDS = {
    'Swamp': ('moss', 'moss', 'leaves', 'pebble', 'root', 'sprout'),
    'Smoke': ('leaves', 'leaves', 'pebble', 'root', 'moss', 'ash'),
    'Burnt': ('twigs', 'twigs', 'pebble', 'ash', 'cinder', 'root'),
}

def deco_tile(t, kind, seed):
    """Detalle pequeño del suelo en un tile de 16×16 (no bloquea)."""
    cv = Canvas(16, 16); rnd = random.Random(seed)
    fl = [C(c) for c in t['fleck']]
    if kind == 'moss':
        cols = fl if t['key'] == 'Swamp' else [C(c) for c in t['vein']]
        w, h = rnd.randrange(8, 12), rnd.randrange(4, 6)
        m = blob(seed, rnd.choice(('redondo', 'lobulado')), w, h)
        ox, oy = (16 - w) // 2, 6
        for (i, j) in m:
            top = (i, j - 1) not in m
            cv.put(ox + i, oy + j, cols[2] if top and (i + j) % 2 == 0 else cols[1] if top or (i * 7 + j * 3) % 5 == 0 else cols[0])
    elif kind == 'leaves':
        cols = [C(c) for c in (FERN[1:4] if t['key'] == 'Swamp' else FERN_DRY[1:4])]
        for k in range(rnd.randrange(3, 5)):
            lx, ly = rnd.randrange(2, 11), rnd.randrange(4, 12); fl_ = rnd.random() < .5
            for (i, j, c) in ((0, 0, 1), (1, 0, 2), (1, 1, 0), (2, 1, 1), (3, 1, 2)):
                cv.put(lx + (3 - i if fl_ else i), ly + j, cols[c])
    elif kind == 'pebble':
        for k in range(rnd.randrange(2, 4)):
            px, py, r = rnd.uniform(4, 12), rnd.uniform(6, 12), rnd.uniform(1.4, 2.4)
            ellipse(cv, px, py, r, r * .7, STONE)
        cv.outline()
    elif kind == 'root':
        fc = t['face'] if not t['burnt'] else [C(c) for c in CHAR]
        L = rnd.randrange(10, 14); ph = rnd.uniform(0, 6); x0 = (16 - L) // 2
        for i in range(L):
            yy = 8 + round(math.sin(i / 4 + ph) * 1.2); end = i < 2 or i >= L - 2
            cv.put(x0 + i, yy, fc[1] if end else fc[3]); cv.put(x0 + i, yy + 1, fc[0] if end else fc[1])
    elif kind == 'sprout':
        fiddle(cv, 7, 13, 6, [C(c) for c in FERN], rnd.random() < .5)
        fiddle(cv, 10, 13, 4, [C(c) for c in FERN], True)
    elif kind == 'ash':
        cols = [C(c) for c in CHAR[2:]] + [C('#8a8484')]
        for k in range(rnd.randrange(8, 13)): cv.put(rnd.randrange(3, 13), rnd.randrange(6, 12), cols[rnd.randrange(len(cols))])
    elif kind == 'twigs':
        ch = [C(c) for c in CHAR]
        for k in range(rnd.randrange(2, 4)):
            sx, sy = rnd.randrange(3, 8), rnd.randrange(5, 11); dx = rnd.choice((1, -1)); L = rnd.randrange(4, 7)
            if dx < 0: sx += 6
            for i in range(L): cv.put(sx + i * dx, sy + (i // 3), ch[3] if i % 2 else ch[2])
            if rnd.random() < .5: cv.put(sx + (L - 1) * dx, sy + (L - 1) // 3, C(EMBER[2]))
    elif kind == 'cinder':                            # trozo de carbón con una brasa
        ellipse(cv, 8, 10, 3.5, 2.2, CHAR[1:])
        cv.put(8, 9, C(EMBER[2])); cv.put(9, 9, C(EMBER[1])); cv.outline()
    return cv

def ambient_set(t, seed):
    out = {}
    out['Deco_Ground'] = [deco_tile(t, k, seed + i * 3) for i, k in enumerate(DECO_KINDS[t['key']])]
    out['Methane_Sac_Idle'] = [pad(methane_sac(t, f), 0, 0, 0, 2) for f in range(4)]
    if not t['burnt']: out['Deco_DripMoss'] = [drip_moss(t, f, seed) for f in range(6)]
    out.update(vent_set(t))
    if t['burnt']:
        out['Deco_Embers'] = [embers(f, seed + k) for k in range(1) for f in range(4)]
        out['Fx_StumpSmoke'] = [stump_smoke(f) for f in range(6)]
    out['Methane_Mist'] = [methane_mist(64, 32, seed + 3, f / 8) for f in range(8)]
    out['Methane_Bubble'] = [methane_bubble(f) for f in range(6)]
    out['Critter_Beetle_Idle'] = [pad(beetle(f, False), 1, 1, 1, 1) for f in range(2)]
    out['Critter_Beetle_Run'] = [pad(beetle(f, True), 1, 1, 1, 1) for f in range(4)]
    if not t['burnt']: out['Critter_Gnats'] = [gnats(f, seed) for f in range(4)]
    return out

# =================================================================================================
# Escena de muestra (suelo y pared de la tanda 1a + todo lo de aquí)
# =================================================================================================
def load_tile(key, name): return Image.open(os.path.join(HERE, key, 'tiles', name + '.png')).convert('RGBA')

def scene(key, props, f=0):
    TW, TH = 24, 13
    im = Image.new('RGBA', (TW * 16, TH * 16), BG)
    h = lambda x, y: (x * 73856093 ^ y * 19349663) & 0x7fffffff
    for x in range(TW):
        im.alpha_composite(load_tile(key, 'Wall_Cap'), (x * 16, 0))
        v = h(x, 1) % 4
        im.alpha_composite(load_tile(key, f'Wall_FaceUpper_{v}'), (x * 16, 16))
        im.alpha_composite(load_tile(key, f'Wall_FaceLower_{v}'), (x * 16, 32))
    for y in range(3, TH):
        for x in range(TW):
            n = f'Floor_ShadowTop_{h(x, y) % 2}' if y == 3 else f'Floor_{1 + (h(x, y) // 3) % 5}' if h(x, y) % 3 == 0 else 'Floor_0'
            im.alpha_composite(load_tile(key, n), (x * 16, y * 16))
    def put(name, x, y, frame=None, anchor='bottom'):
        fr = props[name]; c = fr[(f if frame is None else frame) % len(fr)].img()
        if anchor == 'bottom': im.alpha_composite(c, (int(x - c.width / 2), int(y - c.height)))
        else: im.alpha_composite(c, (int(x), int(y)))
    # charcos y detalles (por debajo)
    put('Puddle_Water_Lobed', 70, 150); put('Puddle_Sludge_Long', 300, 196); put('Puddle_Water_Chain', 210, 120)
    put('Puddle_Sludge_Splash', 120, 200); put('Puddle_Water_Kidney', 330, 100)
    for i, (x, y) in enumerate(((40, 70), (160, 80), (260, 160), (350, 180), (100, 110), (220, 190), (30, 190))):
        put('Deco_Ground', x, y, frame=i, anchor='top')
    if 'Deco_Embers' in props:
        for x, y in ((150, 140), (280, 60)): put('Deco_Embers', x, y, anchor='top')
    put('Methane_Mist', 250, 150)
    # cosas con pie (ordenadas por altura)
    items = [('Fern_Leafy_Sway', 40, 104), ('Fern_Sparse_Sway', 150, 108), ('Fern_Young_Sway', 196, 160),
             ('Fern_Bent_Sway', 270, 120), ('Fern_Tree_Sway', 350, 140), ('Obstacle_Rock', 110, 78),
             ('Obstacle_Stump', 236, 82), ('Obstacle_Sigillaria', 90, 196), ('Obstacle_BrokenTrunk', 170, 206),
             ('Obstacle_FallenLog', 330, 206), ('Methane_Sac_Idle', 300, 80), ('Vent_Idle', 60, 176),
             ('Critter_Beetle_Run', 130, 160)]
    if not props.get('_burnt'): items.append(('Fern_Leafy_Burn', 250, 210))
    for name, x, y in sorted(items, key=lambda it: it[2]): put(name, x, y)
    if 'Fx_StumpSmoke' in props: put('Fx_StumpSmoke', 236, 72)
    if 'Deco_DripMoss' in props: put('Deco_DripMoss', 132, 16, anchor='top'); put('Deco_DripMoss', 290, 16, frame=f + 3, anchor='top')
    if 'Critter_Gnats' in props: put('Critter_Gnats', 210, 100)
    put('Methane_Bubble', 300, 190)
    return im

# =================================================================================================
# Salida
# =================================================================================================
MS = {'Sway': 220, 'Rustle': 70, 'Burn': 110, 'Puddle_Water': 220, 'Puddle_Sludge': 260, 'Methane_Sac_Idle': 260, 'Deco_DripMoss': 150,
      'Vent_Idle': 260, 'Vent_Warn': 120, 'Vent_Blast': 90, 'Deco_Embers': 300, 'Fx_StumpSmoke': 160,
      'Methane_Mist': 140, 'Methane_Bubble': 130, 'Critter_Beetle_Idle': 400, 'Critter_Beetle_Run': 70, 'Critter_Gnats': 120}

def ms_of(name):
    for k, v in MS.items():
        if k in name: return v
    return 200

def build(key):
    t = tram(key); s = SEED[key]
    props = {}
    props.update(fern_set(t, s)); props.update(puddle_set(t, s + 100)); props.update(obstacle_set(t, s + 200))
    props.update(ambient_set(t, s + 300))
    props['_burnt'] = t['burnt']
    return t, props

def write(key, t, props):
    d = os.path.join(HERE, key, 'props'); os.makedirs(os.path.join(HERE, key, 'gif'), exist_ok=True)
    os.makedirs(d, exist_ok=True)
    for name, frames in props.items():
        if name.startswith('_'): continue
        save_sheet(os.path.join(d, name + '.png'), frames)
        if len(frames) > 1 and name != 'Deco_Ground':
            gif(os.path.join(HERE, key, 'gif', name + '.gif'), frames, ms_of(name), t['floor'][1], 3)
    frames = [scene(key, props, f) for f in range(4)]
    frames[0].resize((frames[0].width * 3, frames[0].height * 3), Image.NEAREST).save(os.path.join(HERE, key, 'Escena_x3.png'))
    frames[0].save(os.path.join(HERE, key, 'Escena.png'))
    big = [f.resize((f.width * 2, f.height * 2), Image.NEAREST).convert('RGB') for f in frames]
    big[0].save(os.path.join(HERE, key, 'gif', 'Escena.gif'), save_all=True, append_images=big[1:], duration=220, loop=0)
    return frames[0]

if __name__ == '__main__':
    scenes, allp = [], {}
    for key in KEYS:
        t, props = build(key)
        scenes.append(write(key, t, props))
        allp[key] = props
    W = max(s.width for s in scenes); H = sum(s.height for s in scenes) + 8 * 2
    im = Image.new('RGBA', (W, H), BG); y = 0
    for s in scenes: im.alpha_composite(s, (0, y)); y += s.height + 8
    im.resize((W * 2, H * 2), Image.NEAREST).save(os.path.join(HERE, 'vista_previa_escena.png'))
    rows = {}
    for key, p in allp.items():
        for name, fr in p.items():
            if name.startswith('_') or name.startswith('Puddle') and not name.endswith(('Lobed', 'Island')): continue
            if name.startswith('Fern') and key == 'Smoke' and not name.endswith('Sway'): continue
            rows[f'{key} {name}'] = fr
    preview(os.path.join(HERE, 'vista_previa_props.png'), rows, BG, scale=2, cols=8)
    print('Hecho:', ', '.join(KEYS))
