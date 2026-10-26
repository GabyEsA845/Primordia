"""Evolución de los 8 linajes del piso 2 (océano cámbrico) de Primordia.

Como en el piso 1 (../lineage-evolution/generar_evolucion_linajes.py): el cuerpo brilla, se transforma en la silueta
del animal con el color de su clase y vuelve. Aquí se parte del cuerpo del piso 2 que da la Boca del linaje
(pez acorazado, medusa o lirio de mar) y el lienzo es de 64×64 porque las criaturas del océano son más grandes.

  Evolution/Evolution_<Linaje>.png   12 cuadros de 64×64, unos 2 s (duraciones en EVOLUTION_MS)

Uso: python generar_evolucion_piso2.py [carpeta_multimedia_temp] [carpeta_salida]
"""
import math, os, sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, '..')
OUT = sys.argv[2] if len(sys.argv) > 2 else HERE
sys.path.insert(0, os.path.join(ROOT, 'floor2-bosses'))
from base_piso2 import C, OUTL, Canvas, shade

E = 64
CX, CY = 32, 33
CLASS_RAMP = {
    'carn': ['#5a1a1a', '#a83232', '#e0605a', '#ffb0a0'],
    'herb': ['#1e4a1e', '#3c8d2f', '#7ed060', '#d0ffb0'],
    'ven': ['#3a1060', '#8a3fb8', '#c99af0', '#f4e0ff'],
    'carr': ['#4a3420', '#8a6a40', '#c8a46a', '#f2e0b4'],     # Carroñero, nueva: marrón arena
}
# linaje -> (clase, cuerpo de su Boca)
LINEAGES = {
    'Placoderm': ('carn', 'Placoderm'), 'Opabinia': ('carn', 'Placoderm'),
    'Burgessomedusa': ('ven', 'Jellyfish'), 'Anemone': ('ven', 'Jellyfish'),
    'Crinoid': ('herb', 'Crinoid'), 'Wiwaxia': ('herb', 'Placoderm'),
    'Trilobite': ('carr', 'Placoderm'), 'Hallucigenia': ('carr', 'Crinoid'),
}
NAMES_ES = {'Placoderm': 'Placodermo', 'Opabinia': 'Opabinia', 'Burgessomedusa': 'Burgessomedusa', 'Anemone': 'Anémona',
            'Crinoid': 'Lirio de mar', 'Wiwaxia': 'Wiwaxia', 'Trilobite': 'Trilobite', 'Hallucigenia': 'Hallucigenia'}

# ============================================================ formas
def ell(cx, cy, rx, ry, ang=0.0): return ('ell', cx, cy, rx, ry, ang)
def half(cx, cy, rx, ry): return ('half', cx, cy, rx, ry, 0)
def poly(*pts): return ('poly', pts)
def line(pts, w0, w1): return ('line', pts, w0, w1)

def inside(sh, x, y):
    k = sh[0]
    if k in ('ell', 'half'):
        _, cx, cy, rx, ry, ang = sh
        dx, dy = x - cx, y - cy
        if ang:
            ca, sa = math.cos(ang), math.sin(ang)
            dx, dy = dx * ca + dy * sa, -dx * sa + dy * ca
        return (dx / rx) ** 2 + (dy / ry) ** 2 <= 1 and (k == 'ell' or dy <= 0)
    if k == 'poly':
        pts = sh[1]; res = False
        for i in range(len(pts)):
            (x1, y1), (x2, y2) = pts[i], pts[i - 1]
            if (y1 > y) != (y2 > y) and x < (x2 - x1) * (y - y1) / (y2 - y1) + x1: res = not res
        return res
    if k == 'line':
        pts, w0, w1 = sh[1], sh[2], sh[3]
        total = sum(math.dist(pts[i], pts[i + 1]) for i in range(len(pts) - 1)) or 1
        acc = 0
        for i in range(len(pts) - 1):
            (ax, ay), (bx, by) = pts[i], pts[i + 1]
            vx, vy = bx - ax, by - ay; l2 = vx * vx + vy * vy or 1
            t = max(0, min(1, ((x - ax) * vx + (y - ay) * vy) / l2))
            seg = math.sqrt(l2)
            w = w0 + (w1 - w0) * (acc + t * seg) / total
            if math.hypot(x - ax - vx * t, y - ay - vy * t) <= w / 2: return True
            acc += seg
        return False
    return False

def scaled(sh, s):
    def tr(p): return (CX + (p[0] - CX) * s, CY + (p[1] - CY) * s)
    k = sh[0]
    if k in ('ell', 'half'):
        _, cx, cy, rx, ry, ang = sh; (cx, cy) = tr((cx, cy)); return (k, cx, cy, rx * s, ry * s, ang)
    if k == 'poly': return ('poly', [tr(p) for p in sh[1]])
    return ('line', [tr(p) for p in sh[1]], max(1.0, sh[2] * s), max(1.0, sh[3] * s))

def cut(sh): return ('cut', sh)

def mask_of(shapes, s=1.0):
    add = [scaled(sh, s) for sh in shapes if sh[0] != 'cut']
    sub = [scaled(sh[1], s) for sh in shapes if sh[0] == 'cut']
    return {(x, y) for y in range(E) for x in range(E)
            if any(inside(sh, x + .5, y + .5) for sh in add) and not any(inside(sh, x + .5, y + .5) for sh in sub)}

def wave(root, ang, length, phase, amp, steps=10, freq=3.0):
    pts = []
    for i in range(steps + 1):
        t = i / steps
        off = math.sin(t * freq + phase) * amp * length * t
        pts.append((root[0] + math.cos(ang) * length * t - math.sin(ang) * off,
                    root[1] + math.sin(ang) * length * t + math.cos(ang) * off))
    return pts

def arc(cx, cy, r, a0, a1, steps=8):
    return [(cx + math.cos(a0 + (a1 - a0) * i / steps) * r, cy + math.sin(a0 + (a1 - a0) * i / steps) * r) for i in range(steps + 1)]

# ============================================================ siluetas (vistas de lado salvo donde se dice)
def silhouette(name):
    if name == 'Placoderm':   # Dunkleosteus: cabeza acorazada con placas cortantes, cola heterocerca
        return [ell(29, 33, 20, 8.5), ell(42, 31, 11.5, 10.5), poly((46, 35), (58, 36), (52, 41), (42, 42)),
                cut(poly((47, 33.5), (60, 31.5), (60, 35.5), (49, 35.5))),             # la boca entreabierta
                cut(line([(36, 23), (37, 30), (35, 39)], 1.0, 1.0)),                    # costura de las placas
                poly((13, 31), (2, 20), (6, 33)), poly((12, 35), (4, 44), (8, 34)),     # cola
                poly((22, 26), (32, 25), (23, 15)), poly((30, 40), (38, 41), (28, 48))]  # dorsal y pectoral
    if name == 'Opabinia':    # vista de arriba: cinco ojos con pedúnculo, trompa con garra, aletas y cola en V
        sh = [ell(28, 33, 16, 3.8), ell(43, 33, 4.2, 3.8)]
        for k in range(7):
            x = 15 + k * 4.0
            for s_ in (-1, 1): sh.append(ell(x, 33 + s_ * 5.5, 2.0, 3.4, s_ * 0.5))
        sh += [poly((13, 33), (3, 22), (7, 33)), poly((13, 33), (3, 44), (7, 33)), poly((13, 31), (2, 33), (13, 35))]
        for ex, ey in ((43, 21), (38, 23), (48, 23), (38, 43), (48, 43)):
            sh += [line([(43, 33), (ex, ey)], 1.2, 1.0), ell(ex, ey, 2.2, 2.2)]
        sh += [line([(46, 33), (53, 33), (58, 32)], 2.0, 1.4),
               line([(58, 32), (62, 29)], 1.4, 1.0), line([(58, 32), (62, 35)], 1.4, 1.0)]
        return sh
    if name == 'Burgessomedusa':   # campana alta y casi cuadrada, flecos de tentáculos cortos y muchos
        sh = [poly((18, 30), (19, 15), (24, 9), (40, 9), (45, 15), (46, 30)), ell(32, 30, 14, 2.5)]
        for k in range(12):
            x = 19.5 + k * 2.3
            sh.append(line(wave((x, 31), math.pi / 2, 19 - abs(k - 5.5) * 1.2, k * 0.8, 0.12), 1.3, 1.0))
        return sh
    if name == 'Anemone':     # columna ancha y baja y una corona de muchos tentáculos que se abren como una flor
        sh = [poly((22, 56), (24, 38), (40, 38), (42, 56)), ell(32, 57, 12, 3), ell(32, 37, 11, 3.5)]
        for k in range(15):
            a = math.pi + 0.15 + k * (math.pi - 0.3) / 14
            ln = 17 - abs(k - 7) * 0.7
            pts = wave((32 + math.cos(a) * 9, 36 + math.sin(a) * 2), a, ln, k * 1.3, 0.1, steps=8, freq=2.5)
            pts = [(x + (x - 32) * 0.08 * i / 8, y) for i, (x, y) in enumerate(pts)]
            sh.append(line(pts, 2.2, 1.0))
        return sh
    if name == 'Crinoid':     # lirio de mar con su tallo: cáliz y brazos plumosos abiertos como una flor
        sh = [line([(32, 58), (32, 46), (31, 38)], 2.6, 2.4), ell(32, 60, 7, 2), ell(31, 34, 5, 4.2)]
        for k in range(10):
            a = -math.pi / 2 + (k - 4.5) * 0.3
            pts = wave((31, 31), a, 22, k, 0.08, steps=10, freq=2)
            curl = []
            for i, (x, y) in enumerate(pts):   # las puntas se enroscan hacia dentro
                t = i / (len(pts) - 1)
                curl.append((x - (k - 4.5) * t ** 3 * 1.3, y + t ** 3 * 2))
            sh.append(line(curl, 1.8, 1.0))
            for i in range(2, 10, 2):
                x, y = curl[i]
                sh.append(line([(x, y), (x + math.cos(a + 0.9) * 2.5, y + math.sin(a + 0.9) * 2.5)], 1.0, 1.0))
        return sh
    if name == 'Wiwaxia':     # babosa acorazada de escamas con dos filas de espinas largas en el lomo
        sh = [half(32, 44, 20, 13), ell(32, 44, 20, 3.5)]
        for k in range(7):
            x = 16 + k * 5.3
            for lean, top in ((-0.25, 0), (0.25, 3)):
                base_y = 44 - 13 * math.sqrt(max(0, 1 - ((x - 32) / 20) ** 2)) + 1
                h = 14 - abs(k - 3) * 1.5 - top
                sh.append(line([(x, base_y), (x + lean * h + (k - 3) * 0.8, base_y - h)], 2.4, 1.0))
        return sh
    if name == 'Trilobite':   # vista de arriba: céfalo, tórax segmentado, pigidio, espinas genales y antenas
        sh = [ell(45, 32, 8.5, 12.5), poly((43, 19.5), (43, 44.5), (18, 40), (18, 24)), ell(16, 32, 6, 7.5),
              poly((41, 21), (25, 13), (37, 25)), poly((41, 43), (25, 51), (37, 39)),
              line([(52, 28), (58, 24), (61, 18)], 1.4, 1.0), line([(52, 36), (58, 40), (61, 46)], 1.4, 1.0),
              cut(line([(41, 20), (40, 32), (41, 44)], 1.0, 1.0))]                   # borde del céfalo
        for k in range(7):    # segmentos: muescas en los bordes y surcos de los tres lóbulos
            x = 21 + k * 3.0
            for s_ in (-1, 1):
                w = 8 + (x - 18) / 25 * 4.5
                sh.append(cut(poly((x - 0.8, 32 + s_ * (w + 2)), (x + 0.8, 32 + s_ * (w + 2)), (x, 32 + s_ * (w - 2.5)))))
        sh += [cut(line([(20, 28), (40, 27.5)], 1.0, 1.0)), cut(line([(20, 36), (40, 36.5)], 1.0, 1.0))]
        return sh
    if name == 'Hallucigenia':   # gusano gordito: siete púas triangulares en el lomo y patitas tentaculares cortas
        body = [(7, 41), (16, 39), (28, 38), (40, 38), (48, 37)]
        sh = [line(body, 5.0, 7.0), ell(52, 36, 5.2, 4.6), line([(56, 37), (61, 38)], 2.4, 1.2)]
        for k in range(7):
            x = 14 + k * 5.2
            top = 19 + abs(k - 3) * 1.3
            sh.append(poly((x - 2.2, 37), (x + 1.6, 37), (x - 3.5, top)))
            if k % 1 == 0: sh.append(line([(x, 41), (x + 1, 45), (x + 3, 47)], 1.8, 1.4))
        sh.append(line([(49, 40), (50, 44), (52, 46)], 1.8, 1.4))
        return sh
    return []

# ============================================================ pintura
def paint(mask, ramp, white=0.0, glow=None):
    cv = Canvas(E)
    if not mask: return cv
    xs = [p[0] for p in mask]; ys = [p[1] for p in mask]
    cx, cy = (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2
    w, h = max(1, (max(xs) - min(xs)) / 2), max(1, (max(ys) - min(ys)) / 2)
    for (x, y) in mask:
        edge = any((x + dx, y + dy) not in mask for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
        col = C(ramp[3]) if edge else C(ramp[shade((x + .5 - cx) / w, (y + .5 - cy) / h)])
        if white > 0: col = tuple(int(c + (255 - c) * white) for c in col[:3]) + (255,)
        cv.px[(x, y)] = col
    cv.outline()
    if glow:
        for (x, y) in list(cv.px):
            for dx in range(-2, 3):
                for dy in range(-2, 3):
                    d = abs(dx) + abs(dy); q = (x + dx, y + dy)
                    if 0 < d <= 2 and q not in cv.px and 0 <= q[0] < E and 0 <= q[1] < E:
                        cv.px[q] = C(glow, 150 if d == 1 else 70)
        # el halo no debe tapar el contorno: se repintó encima solo donde estaba vacío
    return cv

def sparkles(cv, t, color, count=10, radius=27):
    for k in range(count):
        a = k * 2 * math.pi / count + t * 1.4
        x, y = CX + math.cos(a) * radius, CY + math.sin(a) * radius * 0.85
        for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
            if (dx == 0 and dy == 0) or (k + int(t * 3)) % 2 == 0:
                cv.put(x + dx, y + dy, C(color, 255 if dx == dy == 0 else 170))

def body_image(body):
    im = Image.open(os.path.join(ROOT, 'floor2-bodies', body, f'Player_{body}_Idle_down.png')).convert('RGBA').crop((0, 0, 24, 24))
    out = Image.new('RGBA', (E, E), (0, 0, 0, 0))
    out.alpha_composite(im, (E // 2 - 12, E // 2 - 12))   # centrado, como el pivote del jugador
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
    shapes = silhouette(name)
    bimg = body_image(body)
    body_mask = {(x, y) for y in range(E) for x in range(E) if bimg.getpixel((x, y))[3] > 120}
    blob = mask_of([ell(CX, CY, 13, 12)])
    fr = [bimg, whiten(bimg, 0.5),                                                    # 0-1: el cuerpo empieza a brillar
          paint(body_mask, ramp, white=1.0, glow=glow).img(),                         # 2: blanco, con halo
          paint(blob, ramp, white=1.0, glow=glow).img(),                              # 3: se hincha
          paint(mask_of(shapes, 0.55) | mask_of([ell(CX, CY, 10, 9.5)]), ramp, white=1.0, glow=glow).img(),  # 4: toma forma
          paint(mask_of(shapes, 0.85), ramp, white=0.8, glow=glow).img()]             # 5: casi el animal
    full = mask_of(shapes, 1.0)
    for k in range(3):                                                                # 6-8: el animal, con destellos
        cv = paint(full, ramp, glow=glow); sparkles(cv, k, ramp[3]); fr.append(cv.img())
    fr.append(paint(full, ramp, white=0.9, glow=glow).img())                          # 9: destello
    fr.append(paint(mask_of([ell(CX, CY, 12, 11)]), ramp, white=1.0, glow=glow).img())  # 10: vuelve
    last = bimg.copy(); cv = Canvas(E); sparkles(cv, 3, ramp[3], count=12, radius=17)
    last.alpha_composite(cv.img()); fr.append(last)                                   # 11: el cuerpo, con chispas
    return fr

EVOLUTION_MS = [100, 110, 120, 120, 130, 150, 260, 260, 260, 150, 120, 200]

# ============================================================ guardado y vista previa
BG = C('#0f2a4a')
os.makedirs(f'{OUT}/Evolution', exist_ok=True); os.makedirs(f'{OUT}/gif', exist_ok=True)
sheets = {}
for name in LINEAGES:
    frames = evolution(name)
    sheets[name] = frames
    sh = Image.new('RGBA', (E * len(frames), E), (0, 0, 0, 0))
    for i, f in enumerate(frames): sh.paste(f, (i * E, 0))
    sh.save(f'{OUT}/Evolution/Evolution_{name}.png')
    out = []
    for f in frames:
        b = Image.new('RGBA', (E, E), BG); b.alpha_composite(f); out.append(b.resize((E * 4, E * 4), Image.NEAREST).convert('RGB'))
    out[0].save(f'{OUT}/gif/Evolution_{name}.gif', save_all=True, append_images=out[1:], duration=EVOLUTION_MS, loop=0)

S = 2; cell = E * S
prev = Image.new('RGBA', (150 + cell * 12, len(sheets) * cell), BG); d = ImageDraw.Draw(prev)
for r, (name, frames) in enumerate(sheets.items()):
    d.text((8, r * cell + cell // 2 - 10), f'{NAMES_ES[name]}\n({LINEAGES[name][0]})', fill=C('#bfe6ff'))
    for i, f in enumerate(frames): prev.alpha_composite(f.resize((cell, cell), Image.NEAREST), (150 + i * cell, r * cell))
prev.save(f'{OUT}/vista_previa_evolucion_piso2.png')
# siluetas a tamaño grande para revisar la forma
big = Image.new('RGBA', (E * 4 * 4, E * 4 * 2), BG)
for i, (name, frames) in enumerate(sheets.items()):
    big.alpha_composite(frames[7].resize((E * 4, E * 4), Image.NEAREST), ((i % 4) * E * 4, (i // 4) * E * 4))
big.save(f'{OUT}/vista_previa_siluetas.png')
print('Listo:', OUT)
