"""Arte de las 6 mutaciones del piso 2 que no son Bocas (Primordia), por tandas.

Tanda 1, Extremidades (detrás del cuerpo):
- Cola propulsora: capa de aleta de dos lóbulos sobre los 3 cuerpos, la burbuja que deja la esquiva (normal y la grande
  del Torbellino) y su estallido.
- Espinas de Hallucigenia: capa de espinas en el lomo sobre los 3 cuerpos, la púa del suelo (sale, espera y se deshace)
  y la púa que sale disparada (Erizo).

Las capas se calculan desde los sprites reales de los cuerpos (../floor2-bodies), cuadro a cuadro, como la Cutícula de
../clam-mutations: lienzo de 48×48 con el cuerpo centrado, solo en Idle, Move y Attack (sin capa en Dodge, Hurt ni
Death). Las Extremidades van detrás del cuerpo: solo se pintan fuera de su silueta.

Uso: python generar_mutaciones_piso2.py   (salida junto a este script; necesita Pillow)
"""
import math, os
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
BODIES = os.path.normpath(os.path.join(HERE, '..', 'floor2-bodies'))
OUTL = (24, 20, 37, 255)
N = 24
NL = 48
OFF = (NL - N) // 2


def C(h, a=255):
    h = h.lstrip('#'); return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)) + (a,)


class Canvas:
    def __init__(s, w, h=None): s.w = w; s.h = h or w; s.px = {}
    def inside(s, x, y): return 0 <= x < s.w and 0 <= y < s.h
    def put(s, x, y, c):
        x, y = int(math.floor(x)), int(math.floor(y))
        if s.inside(x, y): s.px[(x, y)] = C(c) if isinstance(c, str) else c
    def outline(s):
        add = [(x, y) for y in range(s.h) for x in range(s.w) if (x, y) not in s.px and any(
            (x + dx, y + dy) in s.px and s.px[(x + dx, y + dy)] != OUTL for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
        for p in add: s.px[p] = OUTL
    def img(s):
        im = Image.new('RGBA', (s.w, s.h), (0, 0, 0, 0))
        for p, c in s.px.items(): im.putpixel(p, c)
        return im


def frames_of(path):
    sheet = Image.open(path).convert('RGBA')
    h = sheet.height
    return [sheet.crop((i * h, 0, (i + 1) * h, h)) for i in range(sheet.width // h)]


def silhouette(im, enclosed=True):
    """Igual que en ../clam-mutations: lo que encierra el contorno (lirio de mar) o todo lo opaco (pez y medusa)."""
    w, h = im.size
    px = im.load()
    if not enclosed:
        inner = {(x, y) for y in range(h) for x in range(w) if px[(x, y)][3] == 255 and px[(x, y)] != OUTL}
        edge = {(x, y) for y in range(h) for x in range(w) if px[(x, y)] == OUTL}
        return inner, edge
    outside = set()
    stack = [(x, y) for x in range(w) for y in (0, h - 1)] + [(x, y) for y in range(h) for x in (0, w - 1)]
    while stack:
        p = stack.pop()
        if p in outside or not (0 <= p[0] < w and 0 <= p[1] < h) or px[p] == OUTL: continue
        outside.add(p)
        x, y = p
        stack += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    inner = {(x, y) for y in range(h) for x in range(w) if (x, y) not in outside and px[(x, y)] != OUTL and px[(x, y)][3] == 255}
    edge = {(x, y) for y in range(h) for x in range(w) if px[(x, y)] == OUTL and any(
        (x + dx, y + dy) in inner for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))}
    return inner, edge


def bbox(cells):
    xs = [c[0] for c in cells]; ys = [c[1] for c in cells]
    return min(xs), min(ys), max(xs), max(ys)


def behind(cv, body_cells):
    """Las Extremidades van detrás: se borra lo que cae encima del cuerpo (también su contorno)."""
    for (x, y) in body_cells:
        cv.px.pop((x + OFF, y + OFF), None)


BODY_NAMES = ('Placoderm', 'Jellyfish', 'Crinoid')
ANIMS = ('Idle', 'Move', 'Attack')
FACINGS = ('side', 'down', 'up')

# =========================================================================================
# Cola propulsora: aleta de dos lóbulos turquesa, clara en la punta, con radios (turquesa y no morada, para no
# confundirse con los brazos del lirio de mar)
# =========================================================================================
FIN = [C('#163e48'), C('#227a7a'), C('#46b8a8'), C('#a8f0dc')]


def lobe(cv, x0, y0, ang, length, width, sway):
    """Lóbulo de aleta: se ensancha hacia la punta y se curva con el vaivén; un radio oscuro por el centro."""
    for k in range(int(length * 2) + 1):
        t = k / (length * 2)
        a = ang + math.radians(sway) * t
        cx, cy = x0 + math.cos(a) * t * length, y0 + math.sin(a) * t * length
        half = 0.6 + width * t
        nx, ny = -math.sin(a), math.cos(a)
        for s in range(-int(half * 2), int(half * 2) + 1):
            d = s / 2
            if abs(d) > half: continue
            shade = 3 if d < -half * 0.4 else 2 if d < half * 0.3 else 1
            if t > 0.85: shade = min(3, shade + 1)
            cv.put(cx + nx * d, cy + ny * d, FIN[shade])
        cv.put(cx, cy, FIN[0] if 0.15 < t < 0.9 else FIN[1])


def tail_layer(body_frame, facing, anim, frame, enclosed):
    inner, edge = silhouette(body_frame, enclosed)
    x0, y0, x1, y1 = bbox(inner | edge)
    X0, Y0, X1, Y1 = x0 + OFF, y0 + OFF, x1 + OFF, y1 + OFF
    xc, yc = (X0 + X1) / 2, (Y0 + Y1) / 2
    amp = {'Idle': 8, 'Move': 16, 'Attack': 10}[anim]
    sway = math.sin(frame * math.pi / 2) * amp
    cv = Canvas(NL)
    if facing == 'side':        # mira a la derecha: la cola sale por detrás, a la izquierda
        base, ang = (X0 + 2, yc + 1), math.pi
    elif facing == 'down':      # de frente: la cola asoma por detrás, hacia arriba
        base, ang = (xc, Y0 + 3), -math.pi / 2
    else:                       # de espaldas: la cola viene hacia la cámara, hacia abajo
        base, ang = (xc, Y1 - 3), math.pi / 2
    length = 9 if facing == 'side' else 7
    lobe(cv, base[0], base[1], ang - math.radians(28), length, 2.4, sway)
    lobe(cv, base[0], base[1], ang + math.radians(28), length, 2.4, -sway)
    cv.outline()
    behind(cv, inner | edge)
    return cv.img()


# =========================================================================================
# Espinas de Hallucigenia: espinas de hueso en el lomo
# =========================================================================================
BONE = [C('#5a3a2a'), C('#a0704a'), C('#e0c090'), C('#fff0d0')]


def spike(cv, x0, y0, ang, length):
    """Espina cónica: 2 px de base que se afila hasta la punta clara."""
    for k in range(int(length * 2) + 1):
        t = k / (length * 2)
        x, y = x0 + math.cos(ang) * t * length, y0 + math.sin(ang) * t * length
        cv.put(x, y, BONE[2] if t < 0.7 else BONE[3])
        if t < 0.55:
            cv.put(x + math.sin(ang) * 0.9, y - math.cos(ang) * 0.9, BONE[1])


def top_edge(cells, x):
    ys = [c[1] for c in cells if c[0] == x]
    return min(ys) if ys else None


def side_edge(cells, y, left):
    xs = [c[0] for c in cells if c[1] == y]
    return (min(xs) if left else max(xs)) if xs else None


def spines_layer(body_frame, facing, anim, frame, enclosed):
    inner, edge = silhouette(body_frame, enclosed)
    cells = inner | edge
    x0, y0, x1, y1 = bbox(cells)
    wave = math.sin(frame * math.pi / 2) * (0.12 if anim == 'Move' else 0.06)
    cv = Canvas(NL)
    if enclosed:
        # Lirio de mar: encima del cáliz están los brazos, así que las espinas salen a los lados del cáliz y del tallo,
        # dos pares hacia fuera y un poco hacia abajo.
        for k, frac in enumerate((0.45, 0.75)):
            y = int(y0 + (y1 - y0) * frac)
            for left in (True, False):
                x = side_edge(cells, y, left)
                if x is None: continue
                a = math.pi - 0.25 + wave if left else 0.25 - wave
                spike(cv, x + OFF + (-1 if left else 1), y + OFF, a, 6 - k)
    elif facing == 'side':
        # A lo largo del lomo, echadas hacia atrás.
        xs = [x0 + 3 + i * max(2, (x1 - x0 - 6) // 3) for i in range(4)]
        for i, x in enumerate(xs):
            top = top_edge(cells, x)
            if top is None: continue
            spike(cv, x + OFF, top + OFF + 1, -math.pi / 2 - 0.35 + wave * (1 if i % 2 else -1), 6 if i in (1, 2) else 5)
    else:
        # De frente o de espaldas: en abanico por encima del cuerpo (de espaldas, más largas).
        xc = (x0 + x1) / 2
        n = 5
        for i in range(n):
            a = -math.pi / 2 + (i - (n - 1) / 2) * 0.32 + wave
            x = xc + (i - (n - 1) / 2) * 2.2
            top = top_edge(cells, int(round(x)))
            if top is None: continue
            spike(cv, x + OFF, top + OFF + 1, a, (7 if facing == 'up' else 5) - abs(i - 2))
    cv.outline()
    behind(cv, cells)
    return cv.img()


# =========================================================================================
# Hojas de capas
# =========================================================================================
LAYERS = {'TailPropulsor': tail_layer, 'HallucigeniaSpines': spines_layer}
layers = {}
bodies = {}
for body in BODY_NAMES:
    for anim in ANIMS:
        for f in FACINGS:
            frames = frames_of(f'{BODIES}/{body}/Player_{body}_{anim}_{f}.png')
            bodies[(body, anim, f)] = frames
            for mut, fn in LAYERS.items():
                layers[(mut, body, anim, f)] = [fn(fr, f, anim, i, body == 'Crinoid') for i, fr in enumerate(frames)]


def save_sheet(frames, path):
    w, h = frames[0].size
    sheet = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, im in enumerate(frames): sheet.paste(im, (i * w, 0))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    sheet.save(path)


for (mut, body, anim, f), frames in layers.items():
    save_sheet(frames, f'{HERE}/Limbs/Limbs_{mut}_{body}_{anim}_{f}.png')

# =========================================================================================
# Efectos
# =========================================================================================
BUBBLE = [C('#2a6a8a'), C('#5ab0d0'), C('#a8e8ff'), C('#ffffff')]


def bubble(n, r, frame):
    """Burbuja que daña: aro claro con brillo arriba a la izquierda y un núcleo morado que late (es de la Cola)."""
    cv = Canvas(n)
    c = n / 2
    wob = 0.4 * math.sin(frame * math.pi / 2)
    for y in range(n):
        for x in range(n):
            dx, dy = (x + .5 - c) / (r + wob), (y + .5 - c) / (r - wob)
            d = math.hypot(dx, dy)
            if d <= 1:
                if d > 0.78:
                    cv.put(x, y, BUBBLE[2] if dx + dy < 0 else BUBBLE[1])
                else:
                    # Interior translúcido con un tinte turquesa que late (es de la Cola).
                    cv.put(x, y, (*FIN[2][:3], 70 + int(40 * math.sin(frame * math.pi / 2)) + int(40 * (1 - d))))
    cv.put(c - r * 0.45, c - r * 0.45, BUBBLE[3])
    cv.put(c - r * 0.45 + 1, c - r * 0.45, BUBBLE[3])
    cv.outline()
    return cv.img()


def pop(n, frame):
    """Estallido: el aro se rompe en gotas que se abren."""
    cv = Canvas(n)
    c = n / 2
    r = 3 + frame * 2
    a = [255, 220, 160, 90][frame]
    for k in range(10 if frame > 0 else 0):
        ang = k * math.pi / 5 + frame * 0.2
        cv.put(c + math.cos(ang) * r, c + math.sin(ang) * r, (*BUBBLE[2][:3], a))
        if frame < 2:
            cv.put(c + math.cos(ang) * (r - 1), c + math.sin(ang) * (r - 1), (*BUBBLE[3][:3], a))
    if frame == 0:
        # El aro aún entero, con un hueco por donde revienta.
        for k in range(24):
            ang = k * math.pi / 12
            if 0.6 < ang < 1.2: continue
            cv.put(c + math.cos(ang) * 4, c + math.sin(ang) * 4, (*BUBBLE[3][:3], 230))
    return cv.img()


def ground_spike(frame):
    """Púa del suelo, 12×12: sale (cuadros 0-2), espera (3, en bucle con 4) y se deshace (5-7)."""
    cv = Canvas(12)
    h = [2, 4, 6, 6, 6, 5, 3, 1][frame]
    lean = 0.3 if frame == 4 else 0.0
    if frame <= 4:
        for k in range(h * 2):
            t = k / (h * 2)
            x, y = 6 + lean * t * h, 10 - t * h
            cv.put(x, y, BONE[2] if t < 0.7 else BONE[3])
            if t < 0.5:
                cv.put(x - 1, y, BONE[1])
        for x in range(4, 9):   # grieta del suelo
            cv.put(x, 10, C('#3a2a20'))
        cv.outline()
    else:
        k = frame - 5
        for i in range(3 - k):
            cv.put(4 + i * 2, 10 - (h - i) % 3, (*BONE[2][:3], 200 - k * 60))
        for x in range(5, 8):
            cv.put(x, 10, (*BONE[1][:3], 180 - k * 50))
    return cv.img()


def flying_spike(frame):
    """Púa disparada (Erizo), 12×6, hacia la derecha, con estela."""
    cv = Canvas(12, 6)
    for x in range(3, 11):
        t = (x - 3) / 7
        cv.put(x, 3, BONE[2] if t < 0.8 else BONE[3])
        if t < 0.5:
            cv.put(x, 2, BONE[1])
    cv.outline()
    for x in range(0, 3):
        cv.put(x, 3 if (x + frame) % 2 else 2, (*BONE[3][:3], 90 + x * 40))
    return cv.img()


fx = {
    'Fx_TailPropulsor_Bubble': [bubble(12, 4.5, k) for k in range(4)],
    'Fx_TailPropulsor_BubbleBig': [bubble(16, 6.5, k) for k in range(4)],
    'Fx_TailPropulsor_Pop': [pop(16, k) for k in range(4)],
    'Fx_HallucigeniaSpine_Ground': [ground_spike(k) for k in range(8)],
    'Proj_HallucigeniaSpine': [flying_spike(k) for k in range(2)],
}
for name, frames in fx.items():
    save_sheet(frames, f'{HERE}/Effects/{name}.png')

# =========================================================================================
# Vistas previas
# =========================================================================================
BG = C('#0f2a4a')
S = 3


def composite(body_frame, layer_frame, layer_behind):
    out = Image.new('RGBA', (NL, NL), (0, 0, 0, 0))
    if layer_behind:
        out.alpha_composite(layer_frame)
        out.alpha_composite(body_frame, (OFF, OFF))
    else:
        out.alpha_composite(body_frame, (OFF, OFF))
        out.alpha_composite(layer_frame)
    return out


for mut in LAYERS:
    rows = [(body, anim, f) for body in BODY_NAMES for anim in ANIMS for f in FACINGS]
    W = 200 + 6 * (NL * S + 4)
    pv = Image.new('RGBA', (W, len(rows) * (NL * S + 4) + 8), BG)
    d = ImageDraw.Draw(pv)
    for r, (body, anim, f) in enumerate(rows):
        y = 4 + r * (NL * S + 4)
        d.text((8, y + NL * S // 2 - 6), f'{body} {anim} {f}', fill=C('#bfe6ff'))
        for i, (bf, lf) in enumerate(zip(bodies[(body, anim, f)], layers[(mut, body, anim, f)])):
            pv.alpha_composite(composite(bf, lf, True).resize((NL * S, NL * S), Image.NEAREST), (200 + i * (NL * S + 4), y))
    pv.save(f'{HERE}/vista_previa_capas_{mut}.png')

# Resumen: cada cuerpo de lado con cada capa, y los efectos a ×6.
summary = Image.new('RGBA', (20 + 3 * 2 * (NL * 4 + 8), NL * 4 + 30), BG)
x = 10
for mut in LAYERS:
    for body in BODY_NAMES:
        frame = composite(bodies[(body, 'Idle', 'side')][0], layers[(mut, body, 'Idle', 'side')][0], True)
        summary.alpha_composite(frame.resize((NL * 4, NL * 4), Image.NEAREST), (x, 10))
        x += NL * 4 + 8
summary.save(f'{HERE}/vista_previa_tanda1.png')

ev = Image.new('RGBA', (1000, 10 + len(fx) * 110), BG)
y = 10
for name, frames in fx.items():
    xx = 10
    for f in frames:
        big = f.resize((f.width * 6, f.height * 6), Image.NEAREST)
        ev.alpha_composite(big, (xx, y)); xx += big.width + 8
    y += 110
ev.save(f'{HERE}/vista_previa_efectos_tanda1.png')
print('Listo:', HERE)

# =========================================================================================
# Tanda 2, Piel (delante del cuerpo; se deja a la vista la cara: la parte de delante y de abajo)
# =========================================================================================
STEEL = [C('#232a3a'), C('#46526a'), C('#7888a4'), C('#c8d6ea')]


def face_free(cells, facing, x0, y0, x1, y1):
    """Celdas del cuerpo que puede tapar la Piel: el lomo y la parte de atrás, nunca la cara."""
    w, h = max(1, x1 - x0), max(1, y1 - y0)
    out = set()
    for (x, y) in cells:
        u, v = (x - x0) / w, (y - y0) / h
        if facing == 'side' and u <= 0.66 and v <= 0.62:
            out.add((x, y))
        elif facing == 'down' and v <= 0.34:
            out.add((x, y))
        elif facing == 'up' and v <= 0.86:
            out.add((x, y))
    return out


def plates_layer(body_frame, facing, anim, frame, enclosed):
    """Placas de trilobite: bandas de acero segmentadas, con el filo de cada placa iluminado."""
    inner, edge = silhouette(body_frame, enclosed)
    x0, y0, x1, y1 = bbox(inner | edge)
    area = face_free(inner, facing, x0, y0, x1, y1)
    cv = Canvas(NL)
    shine = frame % 4
    for (x, y) in area:
        # Cada placa ocupa 3 filas (de lado, 3 columnas): la primera es el filo claro, la última la junta oscura.
        k = ((x - x0) if facing == 'side' else (y - y0)) % 3
        base = 2 if k == 0 else 1 if k == 1 else 0
        if k == 0 and ((x + y + shine) % 5 == 0):
            base = 3
        cv.put(x + OFF, y + OFF, STEEL[base])
    # Contorno de la coraza: un borde oscuro donde la placa se acaba sobre el cuerpo.
    for (x, y) in area:
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            p = (x + dx, y + dy)
            if p in inner and p not in area:
                cv.put(x + OFF, y + OFF, STEEL[0])
                break
    return cv.img()


SCALE = [C('#1c2416'), C('#3c5426'), C('#7c9a38'), C('#e2ec98')]
SHEEN = [C('#c89a48'), C('#e8c070'), C('#7ad0b0')]


def sclerites_layer(body_frame, facing, anim, frame, enclosed):
    """Escleritos de Wiwaxia: escamas en hoja solapadas (con un brillo bronce/verde que avanza) y espinas altas."""
    inner, edge = silhouette(body_frame, enclosed)
    cells = inner | edge
    x0, y0, x1, y1 = bbox(cells)
    area = face_free(inner, facing, x0, y0, x1, y1)
    cv = Canvas(NL)
    for (x, y) in area:
        # Escamas de 3×2 al tresbolillo: la punta de cada una (abajo a la derecha) más oscura.
        row = (y - y0) // 2
        col = ((x - x0) + (row % 2) * 2) // 3
        lx, ly = ((x - x0) + (row % 2) * 2) % 3, (y - y0) % 2
        shade = 2 if (lx, ly) == (0, 0) else 1 if ly == 0 else 0
        if (row + col + frame) % 6 == 0 and shade > 0:
            cv.put(x + OFF, y + OFF, SHEEN[(row + col) % 3])
        else:
            cv.put(x + OFF, y + OFF, SCALE[shade + (1 if lx == 0 and ly == 0 else 0)])
    # Espinas: dos filas por el lomo, altas y oscuras, que se mecen un poco.
    sway = math.sin(frame * math.pi / 2) * (0.1 if anim == 'Move' else 0.05)
    spine_cells = Canvas(NL)
    if facing == 'side':
        xs = [x0 + 2 + i * max(2, int((x1 - x0) * 0.66 - 2) // 3) for i in range(3)]
        for i, x in enumerate(xs):
            top = top_edge(cells, x)
            if top is None: continue
            tall_spine(spine_cells, x + OFF, top + OFF, -math.pi / 2 - 0.25 + sway, 6)
    else:
        xc = (x0 + x1) / 2
        for i in (-1, 0, 1):
            x = int(round(xc + i * (x1 - x0) * 0.25))
            top = top_edge(cells, x)
            if top is None: continue
            tall_spine(spine_cells, x + OFF, top + OFF, -math.pi / 2 + i * 0.45 + sway, (6 if facing == 'up' else 4) - abs(i))
    spine_cells.outline()
    for p, c in spine_cells.px.items():
        if (p[0] - OFF, p[1] - OFF) not in cells or c != OUTL:
            cv.px.setdefault(p, c) if c == OUTL else cv.px.__setitem__(p, c)
    return cv.img()


def tall_spine(cv, x0, y0, ang, length):
    """Espina de Wiwaxia: una hoja con 2 px de base que se afila hasta la punta de bronce."""
    nx, ny = -math.sin(ang), math.cos(ang)
    for k in range(int(length * 2) + 1):
        t = k / (length * 2)
        x, y = x0 + math.cos(ang) * t * length, y0 + math.sin(ang) * t * length
        cv.put(x, y, SCALE[2] if t < 0.6 else SHEEN[1])
        if t < 0.55:
            cv.put(x + nx * 0.9, y + ny * 0.9, SCALE[1])


SKIN_LAYERS = {'TrilobitePlates': plates_layer, 'WiwaxiaSclerites': sclerites_layer}
for body in BODY_NAMES:
    for anim in ANIMS:
        for f in FACINGS:
            for mut, fn in SKIN_LAYERS.items():
                frames = [fn(fr, f, anim, i, body == 'Crinoid') for i, fr in enumerate(bodies[(body, anim, f)])]
                layers[(mut, body, anim, f)] = frames
                save_sheet(frames, f'{HERE}/Skin/Skin_{mut}_{body}_{anim}_{f}.png')


def bounce(frame):
    """Rebote de un proyectil en las placas, 16×16: una placa que destella y chispas que salen."""
    cv = Canvas(16)
    a = [255, 230, 170, 110, 50][frame]
    r = 2 + frame * 1.5
    for k in range(6):
        ang = -math.pi / 2 + (k - 2.5) * 0.45
        x, y = 8 + math.cos(ang) * r, 9 + math.sin(ang) * r
        cv.put(x, y, (*STEEL[3][:3], a))
        if frame < 3:
            cv.put(x + math.cos(ang), y + math.sin(ang), (255, 255, 255, a))
    if frame < 2:   # la placa que recibe el golpe
        for x in range(5, 11):
            cv.put(x, 10, STEEL[3 - frame])
            cv.put(x, 11, STEEL[1])
    return cv.img()


def roll(frame):
    """Enrollarse (nivel III): bola de placas que gira mientras dura la esquiva, 32×32, en bucle."""
    cv = Canvas(32)
    for y in range(32):
        for x in range(32):
            dx, dy = x + .5 - 16, y + .5 - 16
            d = math.hypot(dx, dy)
            if d <= 11:
                ang = math.atan2(dy, dx) + frame * math.pi / 8
                band = int((ang / (2 * math.pi)) * 10) % 2
                light = -dx - dy
                shade = 3 if light > 12 else 2 if light > 0 else 1 if light > -10 else 0
                if band == 0 and shade > 0: shade -= 1
                if abs(d - 11) < 0.8: shade = 0
                cv.put(x, y, (*STEEL[shade][:3], 230))
    cv.outline()
    return cv.img()


def sclerite(frame):
    """Escama disparada, 10×6, hacia la derecha: una hoja con la punta clara y brillo que cambia."""
    cv = Canvas(10, 6)
    for x in range(1, 9):
        t = (x - 1) / 7
        half = 2 if 0.2 < t < 0.7 else 1
        for dy in range(-half + 1, half):
            cv.put(x, 3 + dy, SCALE[2] if dy < 0 else SCALE[1])
        cv.put(x, 3 - half + 1, SHEEN[(x + frame) % 3] if t > 0.3 else SCALE[2])
    cv.put(8, 3, SCALE[3])
    cv.outline()
    return cv.img()


def release(frame):
    """Las escamas se erizan y salen: un abanico de destellos verdes y bronce, 24×24."""
    cv = Canvas(24)
    a = [255, 210, 140, 70][frame]
    for k in range(8):
        ang = k * math.pi / 4 + frame * 0.15
        for s in range(2):
            r = 4 + frame * 3 + s
            cv.put(12 + math.cos(ang) * r, 12 + math.sin(ang) * r, (*SHEEN[(k + s) % 3][:3], a))
    return cv.img()


def block(frame):
    """Una escama de la Órbita destruye un proyectil: chispazo de 12×12."""
    cv = Canvas(12)
    a = [255, 200, 120, 60][frame]
    r = 1 + frame * 1.5
    for k in range(8):
        ang = k * math.pi / 4
        cv.put(6 + math.cos(ang) * r, 6 + math.sin(ang) * r, (*SHEEN[1][:3], a) if k % 2 else (255, 255, 255, a))
    if frame == 0:
        cv.put(6, 6, (255, 255, 255))
    return cv.img()


fx2 = {
    'Fx_TrilobitePlates_Bounce': [bounce(k) for k in range(5)],
    'Fx_TrilobitePlates_Roll': [roll(k) for k in range(4)],
    'Proj_WiwaxiaSclerite': [sclerite(k) for k in range(3)],
    'Fx_WiwaxiaSclerites_Release': [release(k) for k in range(4)],
    'Fx_WiwaxiaSclerite_Block': [block(k) for k in range(4)],
}
for name, frames in fx2.items():
    save_sheet(frames, f'{HERE}/Effects/{name}.png')

for mut in SKIN_LAYERS:
    rows = [(body, anim, f) for body in BODY_NAMES for anim in ANIMS for f in FACINGS]
    pv = Image.new('RGBA', (200 + 6 * (NL * S + 4), len(rows) * (NL * S + 4) + 8), BG)
    d = ImageDraw.Draw(pv)
    for r, (body, anim, f) in enumerate(rows):
        y = 4 + r * (NL * S + 4)
        d.text((8, y + NL * S // 2 - 6), f'{body} {anim} {f}', fill=C('#bfe6ff'))
        for i, (bf, lf) in enumerate(zip(bodies[(body, anim, f)], layers[(mut, body, anim, f)])):
            pv.alpha_composite(composite(bf, lf, False).resize((NL * S, NL * S), Image.NEAREST), (200 + i * (NL * S + 4), y))
    pv.save(f'{HERE}/vista_previa_capas_{mut}.png')

summary = Image.new('RGBA', (20 + 3 * 2 * (NL * 4 + 8), 3 * (NL * 4 + 10) + 10), BG)
for r, f in enumerate(FACINGS):
    x = 10
    for mut in SKIN_LAYERS:
        for body in BODY_NAMES:
            frame = composite(bodies[(body, 'Idle', f)][0], layers[(mut, body, 'Idle', f)][0], False)
            summary.alpha_composite(frame.resize((NL * 4, NL * 4), Image.NEAREST), (x, 10 + r * (NL * 4 + 10)))
            x += NL * 4 + 8
summary.save(f'{HERE}/vista_previa_tanda2.png')

ev = Image.new('RGBA', (1000, 10 + len(fx2) * 200), BG)
y = 10
for name, frames in fx2.items():
    xx = 10
    for f in frames:
        big = f.resize((f.width * 6, f.height * 6), Image.NEAREST)
        ev.alpha_composite(big, (xx, y)); xx += big.width + 8
    y += max(f.height for f in frames) * 6 + 12
ev.crop((0, 0, 1000, y)).save(f'{HERE}/vista_previa_efectos_tanda2.png')
print('Tanda 2 lista')

# =========================================================================================
# Tanda 3, Órganos (sin capa en el cuerpo: efectos de activación vistosos)
# =========================================================================================
LARVA = [C('#24461c'), C('#4a8a2a'), C('#8ce65a'), C('#e4ffb8')]   # lima del Órgano: se lee como aliada


def larva(frame, mouth=0.0, squash=0.0, alpha=255):
    """Larva aliada, 16×16, mirando a la derecha: cuerpo de gota con cilios que baten y una boca que se abre al morder."""
    cv = Canvas(16)
    wave = math.sin(frame * math.pi / 2)
    cx, cy = 8 + squash, 8
    rx, ry = 4.6 - squash * 0.4, 3.2 + squash * 0.3
    for y in range(16):
        for x in range(16):
            nx, ny = (x + .5 - cx) / rx, (y + .5 - cy - wave * 0.4 * (x < cx)) / ry
            # Gota: más estrecha por detrás.
            if nx < 0: ny *= 1 + (-nx) * 0.5
            if nx * nx + ny * ny <= 1:
                v = (-0.6 * nx - 0.8 * ny)
                k = 3 if v > 0.55 else 2 if v > -0.1 else 1 if v > -0.6 else 0
                cv.put(x, y, (*LARVA[k][:3], alpha))
    # Boca (delante) y ojo.
    mx = int(cx + rx - 1)
    cv.put(mx - 2, 7, C('#1a1428'))
    cv.put(mx - 2, 6, (255, 255, 255, alpha))
    cv.outline()
    # Boca abierta al morder: un corte oscuro con dientecillos claros, encima del contorno.
    if mouth > 0:
        for d in range(int(mouth) + 1):
            for xx in (mx, mx + 1):
                cv.put(xx, 8 - d, C('#3a1020'))
                cv.put(xx, 8 + d, C('#3a1020'))
        cv.put(mx + 1, 8 - int(mouth) - 1, (255, 255, 255, alpha))
        cv.put(mx + 1, 8 + int(mouth) + 1, (255, 255, 255, alpha))
    # Cilios que baten por detrás (sin contorno).
    for k in range(3):
        x = int(cx - rx - 1 - k)
        y = int(8 + math.sin(frame * math.pi / 2 + k) * 2)
        cv.put(x, y, (*LARVA[3][:3], int(alpha * 0.8)))
    return cv.img()


def larva_death(frame):
    """Se deshace en motas lima que suben."""
    if frame == 0:
        return larva(0, squash=1.0)
    cv = Canvas(16)
    a = [0, 230, 170, 110, 50][frame]
    for k in range(7):
        ang = k * math.pi * 2 / 7
        r = 1 + frame * 1.4
        cv.put(8 + math.cos(ang) * r, 8 + math.sin(ang) * r - frame, (*LARVA[2 + k % 2][:3], a))
    return cv.img()


def sac_open(frame):
    """El saco se abre en el jugador y suelta las larvas: membrana lima que se hincha, se raja y se deshace, 32×32."""
    cv = Canvas(32)
    r = [5, 7, 9, 10, 12, 13][frame]
    a = [230, 230, 220, 170, 110, 50][frame]
    gap = [0, 0, 0.5, 1.0, 1.6, 2.2][frame]
    for k in range(int(2 * math.pi * r * 1.6)):
        ang = k / (r * 1.6)
        if gap and abs(math.sin(ang * 2)) < gap * 0.25:   # se raja en cuatro sitios
            continue
        x, y = 16 + math.cos(ang) * r, 16 + math.sin(ang) * r
        cv.put(x, y, (*LARVA[2][:3], a))
        cv.put(16 + math.cos(ang) * (r - 1), 16 + math.sin(ang) * (r - 1), (*LARVA[3][:3], a // 2))
    if frame < 3:
        for y in range(32):
            for x in range(32):
                if math.hypot(x + .5 - 16, y + .5 - 16) < r - 1:
                    cv.put(x, y, (*LARVA[2][:3], 55))
    return cv.img()


def split(frame):
    """Enjambre: la larva que mata se parte en dos (destello lima en cruz), 16×16."""
    cv = Canvas(16)
    a = [255, 210, 140, 70][frame]
    r = 1 + frame * 2
    for d in range(-r, r + 1):
        cv.put(8 + d, 8, (*LARVA[3][:3], a))
        cv.put(8, 8 + d, (*LARVA[2][:3], a))
    return cv.img()


AMBER = [C('#3a2410'), C('#8a5a20'), C('#e0a040'), C('#ffe8a0')]


def hexagon_cells(w, h, size):
    """Centros de una rejilla de hexágonos (filas desplazadas) que cubre el lienzo."""
    cells = []
    row = 0
    y = 0.0
    while y < h + size:
        x = (size * 0.87) if row % 2 else 0.0
        while x < w + size:
            cells.append((x, y))
            x += size * 1.74
        y += size * 1.5
        row += 1
    return cells


def eyes_flash(frame):
    """Ojos compuestos al activarse, 32×32: un ojo de facetas que se abre y destella."""
    cv = Canvas(32)
    open_ = [0.3, 0.7, 1.0, 1.0, 1.0, 0.8][frame]
    glow = [0, 0, 1, 2, 1, 0][frame]
    for y in range(32):
        for x in range(32):
            nx, ny = (x + .5 - 16) / 10, (y + .5 - 16) / (8 * open_)
            if nx * nx + ny * ny <= 1:
                # Facetas: cada píxel toma el tono de su hexágono (2 px).
                fx, fy = (x // 3 + (y // 3) % 2) % 3, (y // 3) % 2
                k = 2 if (fx + fy) % 2 else 1
                if glow and (x + y + frame) % 4 == 0: k = 3
                cv.put(x, y, AMBER[k])
    cv.outline()
    if glow:
        for k in range(8):
            ang = k * math.pi / 4
            for d in range(11, 11 + glow * 2):
                cv.put(16 + math.cos(ang) * d, 16 + math.sin(ang) * d, (*AMBER[3][:3], 200))
    return cv.img()


def vignette():
    """Marco de facetas en el borde de la pantalla mientras dura la cámara lenta, 480×270 (la interfaz de referencia)."""
    W, H = 480, 270
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = im.load()
    size = 9
    centers = hexagon_cells(W, H, size)
    for y in range(H):
        for x in range(W):
            # Distancia al borde, de 0 (borde) a 1 (dentro).
            d = min(x, W - 1 - x, y, H - 1 - y) / 58
            if d >= 1:
                continue
            cxy = min(centers, key=lambda c: (c[0] - x) ** 2 + (c[1] - y) ** 2) if d < 1 else None
            edge = math.hypot(cxy[0] - x, cxy[1] - y) > size * 0.78
            a = int((1 - d) ** 1.4 * (210 if edge else 95))
            col = C('#ffd070') if edge else C('#e09a30')
            px[x, y] = (*col[:3], a)
    return im


fx3 = {
    'Larva_Swim': [larva(k) for k in range(4)],
    'Larva_Bite': [larva(0, mouth=m, squash=q) for m, q in ((1, -0.5), (2, 1.0), (1, 0.5), (0, 0))],
    'Larva_Death': [larva_death(k) for k in range(5)],
    'Fx_BroodSac_Open': [sac_open(k) for k in range(6)],
    'Fx_BroodSac_Split': [split(k) for k in range(4)],
    'Fx_CompoundEyes_Flash': [eyes_flash(k) for k in range(6)],
}
for name, frames in fx3.items():
    folder = 'Larva' if name.startswith('Larva') else 'Effects'
    save_sheet(frames, f'{HERE}/{folder}/{name}.png')
os.makedirs(f'{HERE}/UI', exist_ok=True)
vig = vignette()
vig.save(f'{HERE}/UI/UI_CompoundEyes_Vignette.png')

ev = Image.new('RGBA', (1100, 10 + len(fx3) * 210), BG)
y = 10
for name, frames in fx3.items():
    xx = 10
    for f in frames:
        big = f.resize((f.width * 6, f.height * 6), Image.NEAREST)
        ev.alpha_composite(big, (xx, y)); xx += big.width + 8
    y += max(f.height for f in frames) * 6 + 12
ev.crop((0, 0, 1100, y)).save(f'{HERE}/vista_previa_efectos_tanda3.png')

# El marco sobre una sala de muestra (fondo del arrecife) a ×2.
scene = Image.new('RGBA', (480, 270), BG)
scene.alpha_composite(vig)
scene.resize((960, 540), Image.NEAREST).save(f'{HERE}/vista_previa_ojos.png')
print('Tanda 3 lista')

# Tanda 4 (restos de Carroña): está en generar_carrona.py, hecho con la skill primordia-sprites.
