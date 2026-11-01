"""Evolución de los 8 linajes del piso 3 de Primordia (pantano carbonífero).

Mismo momento que en los pisos 1 y 2 (12 cuadros, unos 2 s): el cuerpo brilla, se hincha en blanco, toma la silueta
del animal con el color de su clase, destella y vuelve. Celda de 80×80 (los cuerpos del piso 3 son de 32 px; en el
piso 2 eran 64×64 con cuerpos de 24). El cuerpo de partida es el que da la Boca del linaje (Escupitajo = Frog,
Peonza = Diplocaulus, Chorro = Whipscorpion), en reposo mirando abajo.
Hecho con la skill primordia-sprites (pixel.py): 16 px por unidad, luz arriba a la izquierda, contorno OUTL.

Uso: python generar_evolucion_piso3.py
"""
import math, os, sys

def _skill_scripts():
    d = os.path.dirname(os.path.abspath(__file__))
    while True:
        p = os.path.join(d, '.claude', 'skills', 'primordia-sprites', 'scripts')
        if os.path.isdir(p): return p
        if os.path.dirname(d) == d: raise SystemExit('No encuentro .claude/skills/primordia-sprites/scripts')
        d = os.path.dirname(d)
sys.path.insert(0, _skill_scripts())
from pixel import *
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = HERE
BODIES = os.path.join(HERE, '..', 'floor3-bodies')
E = 80; CX = CY = E / 2
BG = C('#1c2e20')

CLASS_RAMP = {
    'carn': ['#5a1a1a', '#a83232', '#e0605a', '#ffb0a0'],
    'ven': ['#3a1060', '#8a3fb8', '#c99af0', '#f4e0ff'],
    'hongo': ['#5a3a10', '#a8701c', '#e9b45c', '#fff0c0'],
    'par': ['#3a0e1e', '#9a2044', '#d04a62', '#f4a0a4'],          # Parásito, nueva: granate
}
LINEAGES = {                                                       # linaje: (clase, cuerpo de partida)
    'Eryops': ('carn', 'Diplocaulus'), 'Xenacanthus': ('carn', 'Whipscorpion'),
    'Pulmonoscorpius': ('ven', 'Whipscorpion'), 'Centipede': ('ven', 'Frog'),
    'TinderFungus': ('hongo', 'Frog'), 'Cordyceps': ('hongo', 'Whipscorpion'),
    'Lamprey': ('par', 'Diplocaulus'), 'Leech': ('par', 'Frog'),
}
NAMES_ES = {'Eryops': 'Eryops', 'Xenacanthus': 'Xenacanthus', 'Pulmonoscorpius': 'Pulmonoscorpius',
            'Centipede': 'Ciempiés', 'TinderFungus': 'Yesquero', 'Cordyceps': 'Cordyceps', 'Lamprey': 'Lamprea',
            'Leech': 'Sanguijuela'}

# --- formas ---------------------------------------------------------------------------------------
def ell(cx, cy, rx, ry, ang=0.0): return ('ell', cx, cy, rx, ry, ang)
def poly(*pts): return ('poly', pts)
def line(pts, w0, w1): return ('line', pts, w0, w1)

def _seg_dist(px, py, a, b):
    ax, ay = a; bx, by = b
    dx, dy = bx - ax, by - ay
    L2 = dx * dx + dy * dy or 1
    t = max(0, min(1, ((px - ax) * dx + (py - ay) * dy) / L2))
    return math.hypot(px - ax - t * dx, py - ay - t * dy), t

def inside(sh, x, y):
    k = sh[0]
    if k == 'ell':
        _, cx, cy, rx, ry, a = sh
        dx, dy = x - cx, y - cy; ca, sa = math.cos(a), math.sin(a)
        return ((dx * ca + dy * sa) / rx) ** 2 + ((-dx * sa + dy * ca) / ry) ** 2 <= 1
    if k == 'poly':
        pts = sh[1]; c = False; j = len(pts) - 1
        for i in range(len(pts)):
            xi, yi = pts[i]; xj, yj = pts[j]
            if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi) + xi: c = not c
            j = i
        return c
    if k == 'line':
        _, pts, w0, w1 = sh
        n = len(pts) - 1
        for i in range(n):
            d, t = _seg_dist(x, y, pts[i], pts[i + 1])
            u = (i + t) / n
            if d <= (w0 + (w1 - w0) * u) / 2: return True
        return False
    return False

def scaled(sh, s):
    """Encoge una forma hacia el centro (para que el animal 'crezca' al formarse)."""
    f = lambda p: (CX + (p[0] - CX) * s, CY + (p[1] - CY) * s)
    if sh[0] == 'ell': return ('ell', CX + (sh[1] - CX) * s, CY + (sh[2] - CY) * s, sh[3] * s, sh[4] * s, sh[5])
    if sh[0] == 'poly': return ('poly', [f(p) for p in sh[1]])
    return ('line', [f(p) for p in sh[1]], sh[2] * s, sh[3] * s)

def mask_of(shapes, s=1.0):
    sh = [scaled(x, s) for x in shapes]
    return {(x, y) for y in range(E) for x in range(E) if any(inside(q, x + .5, y + .5) for q in sh)}

def arc_pts(cx, cy, r, a0, a1, n=8, ry=None):
    ry = ry or r
    return [(cx + math.cos(a0 + (a1 - a0) * i / n) * r, cy + math.sin(a0 + (a1 - a0) * i / n) * ry) for i in range(n + 1)]

# --- siluetas (mirando a la derecha, en un lienzo de 80×80; cuts = líneas interiores oscuras) -----
def silhouette(name):
    S, cuts = [], []
    if name == 'Eryops':                       # anfibio enorme: cabeza ancha y chata, patas abiertas, cola corta
        S += [ell(37, 44, 17, 8), ell(58, 42, 11, 6.5, -0.08), poly((60, 37), (73, 41), (70, 46), (58, 48)),
              ell(56, 36.5, 3, 2.2), line([(22, 45), (10, 48), (3, 46)], 7, 1)]
        for (h, k, f) in (((30, 49), (24, 56), (19, 58)), ((47, 49), (52, 56), (57, 58)),
                          ((26, 47), (31, 55), (35, 57)), ((44, 47), (40, 55), (37, 57))):
            S.append(line([h, k, f], 4, 2.5))
        cuts += [[(70, 44), (54, 45)], [(48, 38), (48, 49)], [(44, 37), (44, 50)]]
    elif name == 'Xenacanthus':                # tiburón de agua dulce: cuerpo de anguila y espina en la nuca
        S += [line([(4, 46), (20, 42), (40, 40), (58, 40), (70, 43)], 2, 11), ell(66, 42, 8, 6),
              line([(57, 36), (48, 28), (40, 24)], 3, 1),                      # espina cefálica
              line([(54, 36), (30, 36), (8, 42)], 4, 2),                       # aleta dorsal larga
              poly((52, 45), (60, 46), (50, 55)), poly((24, 44), (30, 45), (22, 52))]
        cuts += [[(60, 38), (60, 45)], [(62, 38), (62, 45)], [(64, 39), (64, 45)], [(73, 44), (66, 45)]]
    elif name == 'Pulmonoscorpius':            # escorpión gigante: pinzas delante y la cola curvada encima
        S += [ell(38, 49, 15, 6.5), ell(51, 47, 5, 4.5),
              line([(50, 46), (57, 40), (62, 40)], 5, 4), ell(67, 39, 8, 5, -0.15),    # brazo y pinza grande
              line([(26, 47), (19, 40), (18, 31), (23, 24), (31, 21)], 7, 5),
              poly((30, 17), (38, 20), (36, 27), (33, 22))]                    # aguijón
        tail = [(26, 47), (19, 40), (18, 31), (23, 24), (31, 21)]
        for a, b in zip(tail, tail[1:]):
            mx, my = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2; L = math.hypot(b[0] - a[0], b[1] - a[1])
            px, py = -(b[1] - a[1]) / L, (b[0] - a[0]) / L
            cuts.append([(mx - px * 3, my - py * 3), (mx + px * 3, my + py * 3)])
        for x in (30, 36, 42, 47):
            S.append(line([(x, 52), (x - 4, 57), (x - 6, 60)], 2.5, 2))
        cuts += [[(32, 43), (32, 55)], [(38, 42), (38, 55)], [(44, 43), (44, 54)], [(64, 40), (75, 39)]]
    elif name == 'Centipede':                  # ciempiés gigante (Euphoberia), desde arriba: cuerpo ondulado y patas
        pts = [(8 + i * 6.2, 40 + math.sin(i * 0.75) * 6) for i in range(11)]
        for i, (x, y) in enumerate(pts):
            S.append(ell(x, y, 4.2, 4.6 - (i == 0) * 1.5))
            nx, ny = (pts[min(i + 1, 10)][0] - pts[max(i - 1, 0)][0]), (pts[min(i + 1, 10)][1] - pts[max(i - 1, 0)][1])
            L = math.hypot(nx, ny) or 1; px, py = -ny / L, nx / L
            for s in (-1, 1):
                if 0 < i < 10: S.append(line([(x, y), (x + px * s * 8 - nx / L * 2, y + py * s * 8 - ny / L * 2)], 2, 1.5))
            if i < 10: cuts.append([(x + 3.6, y - 3), (x + 3.6, y + 3)])
        hx, hy = pts[-1]
        S += [line([(hx + 3, hy - 2), (hx + 9, hy - 8), (hx + 12, hy - 9)], 2, 1),
              line([(hx + 3, hy + 2), (hx + 9, hy + 8), (hx + 12, hy + 9)], 2, 1)]
    elif name == 'TinderFungus':               # yesquero: tocón con tres repisas de hongo y una brasa encima
        S += [poly((30, 62), (32, 28), (46, 28), (49, 62)),
              ell(48, 31, 17, 5.5), ell(42, 42, 15, 5), ell(50, 52, 13, 4.5),
              line([(40, 26), (41, 21), (39, 17), (42, 13)], 4, 1)]           # llamita de yesca
        for (cx, cy, rx) in ((48, 31, 17), (42, 42, 15), (50, 52, 13)):
            cuts += [arc_pts(cx, cy + 0.5, rx * 0.65, 0.15, math.pi - 0.15, 8, 2.6)]
    elif name == 'Cordyceps':                  # insecto tomado por el hongo: tallos con maza que le brotan del lomo
        S += [ell(36, 52, 13, 6), ell(51, 50, 5.5, 4.5), ell(57, 49, 3.5, 3)]
        for x in (30, 37, 44):
            S.append(line([(x, 55), (x - 3, 61), (x - 5, 63)], 2.5, 2))
        for (pts, tip) in (([(32, 47), (28, 33), (30, 20)], (30, 18)), ([(40, 47), (44, 34), (43, 24)], (43, 22)),
                           ([(26, 49), (17, 38), (12, 32)], (11, 30)), ([(46, 47), (54, 38), (60, 31)], (61, 29))):
            S.append(line(pts, 3, 2)); S.append(ell(tip[0], tip[1], 3, 4))
        cuts += [[(30, 48), (30, 57)], [(38, 47), (38, 58)]]
    elif name == 'Lamprey':                    # lamprea: cuerpo de anguila y el disco de la boca lleno de dientes
        S += [line([(4, 50), (22, 46), (44, 43), (60, 41)], 3, 11), ell(65, 41, 7, 10.5),
              line([(40, 38), (20, 42)], 3, 2), poly((4, 50), (10, 44), (12, 54))]
        cuts += [arc_pts(65, 41, 4.5, 0, 2 * math.pi, 16, 7)]
        cuts += [('L', [(65 + math.cos(a) * 3, 41 + math.sin(a) * 5), (65 + math.cos(a) * 3, 41 + math.sin(a) * 5)])
                 for a in [k * math.pi / 5 for k in range(10)]]                # anillo de dientes claros
        cuts.append(('L', [(64, 39), (64, 43)]))
        for k in range(7): cuts.append([(50 - k * 2.6, 41), (50 - k * 2.6, 42)])          # agujeros branquiales
    elif name == 'Leech':                      # sanguijuela: cuerpo anillado en arco con una ventosa en cada punta
        path = [(13, 55), (19, 40), (31, 28), (46, 26), (58, 36), (63, 52)]
        S += [line(path, 8, 10), ell(12, 57, 6, 3.2), ell(64, 55, 5, 3)]
        for i in range(len(path) - 1):
            (ax, ay), (bx, by) = path[i], path[i + 1]
            for t in (0.35, 0.75):
                x, y = ax + (bx - ax) * t, ay + (by - ay) * t
                L = math.hypot(bx - ax, by - ay); px, py = -(by - ay) / L, (bx - ax) / L
                cuts.append([(x - px * 4, y - py * 4), (x + px * 4, y + py * 4)])
    return S, cuts

# --- pintado --------------------------------------------------------------------------------------
def paint(mask, ramp, white=0.0, glow=None, cuts=None):
    cv = Canvas(E)
    if not mask: return cv
    xs = [p[0] for p in mask]; ys = [p[1] for p in mask]
    cx, cy = (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2
    w, h = max(1, (max(xs) - min(xs)) / 2), max(1, (max(ys) - min(ys)) / 2)
    for (x, y) in mask:
        edge = any((x + dx, y + dy) not in mask for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
        col = C(ramp[3]) if edge else C(ramp[shade((x + .5 - cx) / w, (y + .5 - cy) / h)])
        cv.px[(x, y)] = col
    for line_ in (cuts or []):
        col = C(ramp[0])
        if line_ and line_[0] == 'L': col, line_ = C(ramp[3]), line_[1]
        for a, b in zip(line_, line_[1:]):
            n = int(max(abs(b[0] - a[0]), abs(b[1] - a[1]))) + 1
            for k in range(n + 1):
                p = (int(a[0] + (b[0] - a[0]) * k / n), int(a[1] + (b[1] - a[1]) * k / n))
                if p in mask: cv.px[p] = col
    if white > 0:
        for p, col in cv.px.items(): cv.px[p] = tuple(int(c + (255 - c) * white) for c in col[:3]) + (255,)
    cv.outline()
    if glow:
        for (x, y) in list(cv.px):
            for dx in range(-2, 3):
                for dy in range(-2, 3):
                    d = abs(dx) + abs(dy); q = (x + dx, y + dy)
                    if 0 < d <= 2 and q not in cv.px and 0 <= q[0] < E and 0 <= q[1] < E:
                        cv.px[q] = C(glow, 150 if d == 1 else 70)
    return cv

def sparkles(cv, t, color, count=10, radius=34):
    for k in range(count):
        a = k * 2 * math.pi / count + t * 1.4
        x, y = CX + math.cos(a) * radius, CY + math.sin(a) * radius * 0.85
        for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
            if (dx == 0 and dy == 0) or (k + int(t * 3)) % 2 == 0:
                cv.put(x + dx, y + dy, C(color, 255 if dx == dy == 0 else 170))

def body_image(body):
    im = Image.open(os.path.join(BODIES, body, f'Player_{body}_Idle_down.png')).convert('RGBA').crop((0, 0, 32, 32))
    out = Image.new('RGBA', (E, E), (0, 0, 0, 0))
    out.alpha_composite(im, (E // 2 - 16, E // 2 - 16))       # centrado, como el pivote del jugador
    return out

def whiten(im, amount):
    im = im.copy(); px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a: px[x, y] = (int(r + (255 - r) * amount), int(g + (255 - g) * amount), int(b + (255 - b) * amount), a)
    return im

def evolution(name):
    cls, body = LINEAGES[name]
    ramp = CLASS_RAMP[cls]; glow = ramp[2]
    shapes, cuts = silhouette(name)
    bimg = body_image(body)
    body_mask = {(x, y) for y in range(E) for x in range(E) if bimg.getpixel((x, y))[3] > 120}
    fr = [bimg, whiten(bimg, 0.5),                                                    # 0-1: el cuerpo empieza a brillar
          paint(body_mask, ramp, white=1.0, glow=glow).img(),                         # 2: blanco, con halo
          paint(mask_of([ell(CX, CY, 17, 16)]), ramp, white=1.0, glow=glow).img(),    # 3: se hincha
          paint(mask_of(shapes, 0.55) | mask_of([ell(CX, CY, 13, 12.5)]), ramp, white=1.0, glow=glow).img(),  # 4: toma forma
          paint(mask_of(shapes, 0.78), ramp, white=0.8, glow=glow).img()]             # 5: casi el animal
    full = mask_of(shapes, 0.9)
    for k in range(3):                                                                # 6-8: el animal, con destellos
        cv = paint(full, ramp, glow=glow, cuts=cuts); sparkles(cv, k, ramp[3]); fr.append(cv.img())
    fr.append(paint(full, ramp, white=0.9, glow=glow).img())                          # 9: destello
    fr.append(paint(mask_of([ell(CX, CY, 16, 15)]), ramp, white=1.0, glow=glow).img())  # 10: vuelve
    last = bimg.copy(); cv = Canvas(E); sparkles(cv, 3, ramp[3], count=12, radius=22)
    last.alpha_composite(cv.img()); fr.append(last)                                   # 11: el cuerpo, con chispas
    return fr

EVOLUTION_MS = [100, 110, 120, 120, 130, 150, 260, 260, 260, 150, 120, 200]

if __name__ == '__main__':
    os.makedirs(f'{OUT}/Evolution', exist_ok=True); os.makedirs(f'{OUT}/gif', exist_ok=True)
    sheets = {}
    for name in LINEAGES:
        frames = evolution(name)
        sheets[name] = frames
        save_sheet(f'{OUT}/Evolution/Evolution_{name}.png', frames)
        out = []
        for f in frames:
            b = Image.new('RGBA', (E, E), BG); b.alpha_composite(as_img(f)); out.append(b.resize((E * 4, E * 4), Image.NEAREST).convert('RGB'))
        out[0].save(f'{OUT}/gif/Evolution_{name}.gif', save_all=True, append_images=out[1:], duration=EVOLUTION_MS, loop=0)
    S = 2; cell = E * S
    prev = Image.new('RGBA', (150 + cell * 12, len(sheets) * cell), BG); d = ImageDraw.Draw(prev)
    for r, (name, frames) in enumerate(sheets.items()):
        d.text((8, r * cell + cell // 2 - 10), f'{NAMES_ES[name]}\n({LINEAGES[name][0]})', fill=C('#d8e8c8'))
        for i, f in enumerate(frames): prev.alpha_composite(as_img(f).resize((cell, cell), Image.NEAREST), (150 + i * cell, r * cell))
    prev.save(f'{OUT}/vista_previa_evolucion_piso3.png')
    big = Image.new('RGBA', (E * 3 * 4, E * 3 * 2), BG)
    for i, (name, frames) in enumerate(sheets.items()):
        big.alpha_composite(as_img(frames[7]).resize((E * 3, E * 3), Image.NEAREST), ((i % 4) * E * 3, (i // 4) * E * 3))
    big.save(f'{OUT}/vista_previa_siluetas.png')
    print('Listo:', OUT)
