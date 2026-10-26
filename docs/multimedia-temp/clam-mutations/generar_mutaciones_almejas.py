"""Arte de las 3 mutaciones especiales de las almejas (piso 2) de Primordia.

- Flagelos de Leanchoilia (Extremidades): sin capa en el cuerpo (excepción aprobada: desentonaba con los 3 cuerpos); el
  latigazo que aparece junto al enemigo y un destello breve sobre el jugador cuando vuelve a estar listo.
- Cutícula de Marrella (Piel): capa iridiscente con dos pares de espinas largas sobre los 3 cuerpos, el destello de la
  esquiva justa, el destello que aturde y el proyectil reflejado.
- Yema clonal (Órgano): el brote de la copia y su disolución en burbujas (la copia es el sprite del jugador teñido).
- Precio Inestable: borde dorado agrietado de los corazones y el medio corazón extra que se rompe.
- Iconos de las tres (16 y 32) y la insignia dorada de mutación especial.

Las capas se calculan desde los sprites reales de los cuerpos (../floor2-bodies), cuadro a cuadro: la silueta del cuerpo
es lo que encierra su contorno (sin tentáculos ni brazos), así la capa sigue el reposo, el movimiento y el ataque.

Uso: python generar_mutaciones_almejas.py   (salida junto a este script; necesita Pillow)
"""
import colorsys, math, os
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
BODIES = os.path.normpath(os.path.join(HERE, '..', 'floor2-bodies'))
UI = os.path.normpath(os.path.join(HERE, '..', '..', '..', 'Assets', '_Primordia', 'Art', 'UI'))
OUTL = (24, 20, 37, 255)
N = 24        # lienzo de los cuerpos
NL = 48       # lienzo de las capas (cuerpo centrado: margen de 12 px para los apéndices y las espinas)
OFF = (NL - N) // 2


def C(h, a=255):
    h = h.lstrip('#'); return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)) + (a,)


def hue(h, s=0.55, v=1.0, a=255):
    r, g, b = colorsys.hsv_to_rgb(h % 1.0, s, v)
    return (int(r * 255), int(g * 255), int(b * 255), a)


def shade_index(nx, ny):
    v = (-0.6 * nx - 0.8 * ny) * 0.8 + 0.35 - max(0, math.hypot(nx, ny) - 0.75) * 1.2
    return 0 if v < -0.15 else 1 if v < 0.35 else 2 if v < 0.75 else 3


class Canvas:
    def __init__(s, w, h=None): s.w = w; s.h = h or w; s.px = {}
    def inside(s, x, y): return 0 <= x < s.w and 0 <= y < s.h
    def put(s, x, y, c):
        x, y = int(math.floor(x)), int(math.floor(y))
        if s.inside(x, y): s.px[(x, y)] = C(c) if isinstance(c, str) else c
    def put_out(s, x, y, c):
        if (int(math.floor(x)), int(math.floor(y))) not in s.px: s.put(x, y, c)
    def outline(s, skip=()):
        add = [(x, y) for y in range(s.h) for x in range(s.w) if (x, y) not in s.px and any(
            (x + dx, y + dy) in s.px and s.px[(x + dx, y + dy)] != OUTL and (x + dx, y + dy) not in skip
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
        for p in add: s.px[p] = OUTL
    def img(s):
        im = Image.new('RGBA', (s.w, s.h), (0, 0, 0, 0))
        for p, c in s.px.items(): im.putpixel(p, c)
        return im


def disk(cv, cx, cy, rx, ry, ramp):
    ramp = [C(c) if isinstance(c, str) else c for c in ramp]
    for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
        for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
            nx, ny = (x + .5 - cx) / rx, (y + .5 - cy) / ry
            if nx * nx + ny * ny <= 1: cv.put(x, y, ramp[shade_index(nx, ny)])


def line(cv, x0, y0, x1, y1, col, only_empty=False):
    steps = int(max(abs(x1 - x0), abs(y1 - y0)) * 2) + 1
    for i in range(steps + 1):
        t = i / steps
        x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
        c = col(t) if callable(col) else col
        (cv.put_out if only_empty else cv.put)(x, y, c)


# =========================================================================================
# Silueta de los cuerpos
# =========================================================================================
def frames_of(path):
    sheet = Image.open(path).convert('RGBA')
    h = sheet.height
    return [sheet.crop((i * h, 0, (i + 1) * h, h)) for i in range(sheet.width // h)]


def silhouette(im, enclosed=True):
    """Píxeles del cuerpo. enclosed: los que encierra el contorno oscuro (relleno 4-conexo desde el borde sin cruzarlo),
    para dejar fuera los brazos del lirio de mar, que no tienen contorno. Si no, todo lo opaco que no es contorno (pez y
    medusa: sus tentáculos son translúcidos, y en el ataque el cuerpo se sale del lienzo y el contorno queda abierto)."""
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


BODY_NAMES = ('Placoderm', 'Jellyfish', 'Crinoid')
ANIMS = ('Idle', 'Move', 'Attack')
FACINGS = ('side', 'down', 'up')


# =========================================================================================
# Flagelos de Leanchoilia (Extremidades)
# =========================================================================================
STALK = ['#16323e', '#245a68', '#3a8e96', '#7ad0c8']       # apéndices segmentados, azul verdoso
FLAG = [C('#bff4ea', 240), C('#e8fff8', 240)]               # flagelos finos y claros, sin contorno
FLAG_TIP = C('#ffffff', 230)


def whip(cv, x, y, ang, length, phase, curl=0.0, alpha=240, only_empty=True):
    """Flagelo de 1 px: avanza de píxel en píxel (sin huecos), se abre un poco ('curl' en radianes al final) y ondula."""
    px_prev = None
    for k in range(int(length) + 1):
        t = k / max(1, length)
        a = ang + curl * t + math.sin(phase * math.pi / 2 + k * 0.5) * 0.18 * t
        x += math.cos(a); y += math.sin(a)
        cell = (int(math.floor(x)), int(math.floor(y)))
        if cell == px_prev: continue
        px_prev = cell
        col = (255, 255, 255, alpha) if t > 0.8 else (*FLAG[k % 2][:3], alpha)
        (cv.put_out if only_empty else cv.put)(x, y, col)


# =========================================================================================
# Cutícula de Marrella (Piel)
# =========================================================================================
def spine(cv, x0, y0, dx, dy, length, bend, h0, bright=False):
    """Espina larga y fina que descompone la luz: el color recorre el arco iris a lo largo de ella."""
    ang = math.atan2(dy, dx); x, y = x0, y0
    steps = int(length * 2)
    for k in range(steps):
        t = k / max(1, steps - 1)
        a = ang + math.radians(bend) * t
        x += math.cos(a) * 0.5; y += math.sin(a) * 0.5
        c = hue(h0 + t * 0.35, 0.28 - 0.12 * t, 1.0, 255 if t < 0.75 else 190)
        if bright:   # lirio de mar: más color y base de 2 px para que no se pierdan entre los brazos
            c = hue(h0 + t * 0.35, 0.42 - 0.2 * t, 1.0, 255)
            if t < 0.45: cv.put(x + 0.7, y + 0.7, hue(h0 + t * 0.35, 0.55, 0.9))
        cv.put_out(x, y, c)
    cv.put(x0, y0, OUTL)


def cuticle_layer(body_frame, facing, frame, enclosed):
    inner, edge = silhouette(body_frame, enclosed)
    x0, y0, x1, y1 = bbox(inner | edge)
    cv = Canvas(NL)
    # brillo iridiscente: bandas diagonales que avanzan cuadro a cuadro sobre el cuerpo
    for (x, y) in inner:
        band = (x - y + frame * 2) % 7
        h = (x + y) * 0.045 + frame * 0.09
        if band in (0, 1):
            cv.put(x + OFF, y + OFF, hue(h, 0.45, 1.0, 150))
        elif band == 2:
            cv.put(x + OFF, y + OFF, hue(h + 0.1, 0.35, 1.0, 70))
    # borde: un hilo de luz en la parte de arriba a la izquierda (la luz viene de ahí)
    for (x, y) in edge:
        if (x + 1, y) in inner and (x, y + 1) in inner or (x, y + 1) in inner and x < (x0 + x1) / 2:
            cv.put(x + OFF, y + OFF, hue((x + y) * 0.06 + frame * 0.09, 0.35, 1.0, 230))
    # dos pares de espinas largas que salen del escudo de la cabeza y se echan hacia atrás
    X0, Y0, X1, Y1 = x0 + OFF, y0 + OFF, x1 + OFF, y1 + OFF
    xc = (X0 + X1) / 2
    sway = math.sin(frame * math.pi / 2) * 4
    h0 = frame * 0.09
    more, bright = (4, True) if enclosed else (0, False)
    if facing == 'side':
        spine(cv, X1 - 4, Y0 + 1, -1, -0.55, 11 + more, -18 + sway, h0, bright)
        spine(cv, X1 - 6, Y0 + 2, -1, -0.15, 9 + more, -10 + sway, h0 + 0.3, bright)
    else:
        long_ = 9 if facing == 'down' else 11
        spine(cv, xc - 2, Y0 + 1, -0.6, -1, long_ + more, -25 - sway, h0, bright)
        spine(cv, xc + 1, Y0 + 1, 0.6, -1, long_ + more, 25 + sway, h0 + 0.5, bright)
        spine(cv, X0 + 1, Y0 + 3, -1, -0.4, long_ - 2 + more, -15 - sway, h0 + 0.25, bright)
        spine(cv, X1 - 1, Y0 + 3, 1, -0.4, long_ - 2 + more, 15 + sway, h0 + 0.75, bright)
    return cv.img()


# =========================================================================================
# Hojas de capas
# =========================================================================================
layers = {}      # (mutación, cuerpo, anim, facing) -> cuadros
bodies = {}      # (cuerpo, anim, facing) -> cuadros del cuerpo (para la vista previa)
for body in BODY_NAMES:
    for anim in ANIMS:
        for f in FACINGS:
            frames = frames_of(f'{BODIES}/{body}/Player_{body}_{anim}_{f}.png')
            bodies[(body, anim, f)] = frames
            layers[('MarrellaCuticle', body, anim, f)] = [cuticle_layer(fr, f, i, body == 'Crinoid') for i, fr in enumerate(frames)]


def save_sheet(frames, path):
    w, h = frames[0].size
    sheet = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, im in enumerate(frames): sheet.paste(im, (i * w, 0))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    sheet.save(path)


for (mut, body, anim, f), frames in layers.items():
    save_sheet(frames, f'{HERE}/Skin/Skin_{mut}_{body}_{anim}_{f}.png')


# =========================================================================================
# Efectos
# =========================================================================================
SPARK = [C('#ffffff'), C('#fff6c0'), C('#ffe080')]


def sparks(cv, x, y, size=2):
    cv.put(x, y, SPARK[0])
    for k in range(1, size + 1):
        c = SPARK[min(k, 2)]
        for dx, dy in ((k, 0), (-k, 0), (0, k), (0, -k)): cv.put(x + dx, y + dy, c)
    if size >= 2:
        for dx, dy in ((1, 1), (-1, -1), (1, -1), (-1, 1)): cv.put(x + dx, y + dy, SPARK[1])


def ripple(cv, cx, cy, r, alpha):
    for i in range(24):
        a = i / 24 * math.tau
        if i % 3 == 2: continue
        cv.put_out(cx + math.cos(a) * r, cy + math.sin(a) * r * 0.4, C('#7ad0c8', alpha))


def lash_fx():
    """Latigazo de los Flagelos, 48×48 centrado en el enemigo, 12 cuadros a 0,1 s: a cada lado sale del agua un apéndice,
    echa los flagelos hacia arriba, azota cruzándolos sobre el enemigo (cuadro 4, el del daño) y se hunde."""
    # (subida 0-1, giro de los flagelos: 0 = hacia arriba, 1 = hacia el enemigo, >1 se pasan; estelas; destello)
    plan = [(0.0, 0, 0, 0), (0.4, 0, 0, 0), (0.85, -0.2, 0, 0), (1.0, 0.5, 1, 0), (1.0, 1.0, 2, 3), (1.0, 1.3, 1, 2),
            (0.95, 1.2, 0, 0), (0.85, 0.9, 0, 0), (0.6, 0.6, 0, 0), (0.35, 0.4, 0, 0), (0.12, 0.3, 0, 0), (0.0, 0, 0, 0)]
    frames = []
    for i, (rise, swing, trails, flash) in enumerate(plan):
        cv = Canvas(48)
        for side in (-1, 1):
            bx, by = 24 + side * 13, 36
            ripple(cv, bx, by + 1, 3 + (i % 3) * 0.6 + (1 - rise) * 1.5, 200 if 0 < i < 11 else 120)
            if rise <= 0: continue
            # tallo segmentado: sube del agua y se inclina hacia el enemigo
            pts = []; sx, sy = bx, by
            for k in range(int(22 * rise)):
                a = math.radians(-90 - side * 28 * k / 22)
                sx += math.cos(a) * 0.5; sy += math.sin(a) * 0.5
                pts.append((sx, sy))
            st = Canvas(48)
            for j, (px_, py_) in enumerate(pts):
                st.put(px_, py_, STALK[3 if (j // 3) % 2 == 0 else 2]); st.put(px_ + side * 0.8, py_, STALK[1])
            st.outline()
            for q, c in st.px.items(): cv.px[q] = c
            if len(pts) < 2: continue
            ex, ey = pts[-1]
            up = math.radians(-90 - side * 20)                    # preparado: hacia arriba y un poco hacia fuera
            hit = math.atan2(22 - ey, 24 - ex)                     # hacia el enemigo
            for back in range(trails, -1, -1):
                sw = swing - back * 0.3
                ang = up + (hit - up) * sw
                alpha = 240 if back == 0 else 110 // back
                for j in range(3):
                    whip(cv, ex, ey, ang + (j - 1) * math.radians(9), 15 * min(1, rise + 0.15), i + j, curl=(j - 1) * 0.15,
                         alpha=alpha, only_empty=back == 0 and False)
        if flash: sparks(cv, 24, 22, flash)
        frames.append(cv.img())
    return frames


def ready_fx():
    """Flagelos listos: destello azul verdoso breve sobre el jugador cuando el latigazo vuelve a estar disponible
    (sin capa en el cuerpo: es la excepción de esta Extremidad). 32×32, 5 cuadros, centrado en el jugador."""
    frames = []
    for i in range(5):
        cv = Canvas(32)
        r = 5 + i * 2.2
        alpha = (255, 255, 235, 170, 90)[i]
        for k in range(3):                      # tres flagelos cortos que se abren hacia arriba y se desvanecen
            a = math.radians(-90 + (k - 1) * 32)
            for j in range(int(3 + i)):
                x = 16 + math.cos(a) * (r - 3 + j * 0.9) + math.sin(j * 0.9 + k) * 0.5
                y = 16 + math.sin(a) * (r - 3 + j * 0.9)
                cv.put(x, y, (*FLAG[j % 2][:3], alpha) if j < 2 + i else (255, 255, 255, alpha))
        for k in range(10):                     # anillo de motas
            a = k / 10 * math.tau
            cv.put(16 + math.cos(a) * r, 16 + math.sin(a) * r * 0.8, C(STALK[3], alpha) if (k + i) % 2 else (*FLAG[0][:3], alpha))
        if i < 2: sparks(cv, 16, 16 - 2 - i * 2, 1)
        frames.append(cv.img())
    return frames


def reflect_fx():
    """Esquiva justa: anillo iridiscente que se abre alrededor del jugador, 32×32, 6 cuadros."""
    frames = []
    for i, r in enumerate((5, 7, 9, 11, 12.5, 13.5)):
        cv = Canvas(32)
        alpha = (255, 255, 230, 180, 120, 60)[i]
        for k in range(72):
            a = k / 72 * math.tau
            for dr in ((0, 1) if i < 3 else (0,)):
                cv.put(16 + math.cos(a) * (r - dr), 16 + math.sin(a) * (r - dr), hue(k / 72 + i * 0.08, 0.5, 1.0, alpha))
        for k in range(6):          # chispas en rombo
            a = k / 6 * math.tau + i * 0.3
            x, y = 16 + math.cos(a) * (r - 2), 16 + math.sin(a) * (r - 2)
            if i < 5:
                cv.put(x, y, (255, 255, 255, alpha))
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)): cv.put(x + dx, y + dy, hue(k / 6, 0.4, 1.0, alpha // 2))
        frames.append(cv.img())
    return frames


def stun_fx():
    """Destello que aturde a quien te ataca cuerpo a cuerpo: rayos de prisma, 32×32, 5 cuadros."""
    frames = []
    for i, (length, alpha) in enumerate(((4, 255), (8, 255), (11, 220), (13, 150), (14, 80))):
        cv = Canvas(32)
        for k in range(8):
            a = k / 8 * math.tau + math.radians(22.5) * (k % 2)
            l = length * (1 if k % 2 == 0 else 0.6)
            line(cv, 16, 16, 16 + math.cos(a) * l, 16 + math.sin(a) * l,
                 lambda t, k=k: hue(k / 8 + t * 0.2, 0.55 * t + 0.1, 1.0, alpha))
        if i < 3: sparks(cv, 16, 16, 2 - i // 2)
        frames.append(cv.img())
    return frames


def reflected_proj():
    """Proyectil reflejado: esquirla de cristal que cambia de color, hacia la derecha, 12×12, 4 cuadros."""
    frames = []
    for i in range(4):
        cv = Canvas(12)
        pts = [(10.5, 6), (6, 3.5), (2, 6), (6, 8.5)]
        for y in range(12):
            for x in range(12):
                px, py = x + .5, y + .5
                inside = False
                for j in range(4):
                    (xa, ya), (xb, yb) = pts[j], pts[j - 1]
                    if (ya > py) != (yb > py) and px < (xb - xa) * (py - ya) / (yb - ya) + xa: inside = not inside
                if inside: cv.put(x, y, hue((x - y) * 0.08 + i * 0.25, 0.5, 1.0))
        cv.put(8, 5, C('#ffffff')); cv.put(7, 5, C('#ffffff'))
        cv.outline()
        cv.put_out(1 - (i % 2), 6, hue(i * 0.25 + 0.5, 0.4, 1.0, 160))
        cv.put_out(0, 5 + (i % 2), hue(i * 0.25 + 0.7, 0.4, 1.0, 100))
        frames.append(cv.img())
    return frames


CLONE = (150, 255, 215)       # tinte de la copia (Yema clonal)
# La copia: shader que lleva la luminancia del sprite a esta rampa y la deja translúcida, con el contorno claro.
CLONE_RAMP = [C('#1e6a6a'), C('#3aa8a0'), C('#7ee8cc'), C('#d8fff0')]
CLONE_EDGE = C('#b8fff0')


def bud_fx():
    """Brote de la copia: una yema translúcida crece del suelo, tiembla y revienta (la copia aparece en el cuadro 6).
    32×32, 8 cuadros, centrado donde aparece la copia."""
    frames = []
    plan = [(2, 26, 0), (3.5, 24, 0), (5.5, 21, 0), (7.5, 18, 1), (9, 16, -1), (9.5, 15.5, 1), (0, 16, 2), (0, 16, 3)]
    for i, (r, cy, wob) in enumerate(plan):
        cv = Canvas(32)
        if r:
            rx, ry = r + wob * 0.6, r - wob * 0.6
            for y in range(32):
                for x in range(32):
                    nx, ny = (x + .5 - 16) / rx, (y + .5 - cy) / ry
                    d = nx * nx + ny * ny
                    if d <= 1:
                        a = 90 + int(120 * d)               # más opaca en el borde, como una pompa
                        cv.put(x, y, (*CLONE, a))
            cv.put(16 - r * 0.45, cy - r * 0.5, C('#ffffff'))
            cv.put(16 - r * 0.45 + 1, cy - r * 0.5, (255, 255, 255, 180))
            for k in range(int(26 - cy) + 1):             # tallo que la une al suelo
                cv.put_out(16, cy + r + k, (*CLONE, 160))
        else:
            spread = 10 + (i - 6) * 4
            for k in range(10):
                a = k / 10 * math.tau
                cv.put(16 + math.cos(a) * spread, cy + math.sin(a) * spread, (*CLONE, 230 if i == 6 else 110))
                if i == 6: cv.put(16 + math.cos(a) * (spread - 1), cy + math.sin(a) * (spread - 1), (255, 255, 255, 200))
            if i == 6: sparks(cv, 16, cy, 3)
        frames.append(cv.img())
    return frames


def dissolve_fx():
    """La copia se deshace en burbujas que suben, 32×32, 8 cuadros."""
    import random
    r_ = random.Random(7); rnd = [r_.random() for _ in range(60)]
    frames = []
    for i in range(8):
        cv = Canvas(32)
        for k in range(14):
            bx = 16 + (rnd[k] - 0.5) * 14
            by = 22 - rnd[k + 20] * 10 - i * (1.2 + rnd[k + 40])
            if i < 2 and k > 6 + i * 4: continue
            if by < 1: continue
            size = 1 if rnd[k + 30] < 0.6 else 2
            alpha = max(40, 230 - i * 26)
            if size == 1:
                cv.put(bx, by, (*CLONE, alpha))
            else:
                for dx, dy in ((0, -1), (-1, 0), (1, 0), (0, 1)): cv.put(bx + dx, by + dy, (*CLONE, alpha))
                cv.put(bx - 0.5, by - 0.5, (255, 255, 255, alpha))
        frames.append(cv.img())
    return frames


fx = {
    'Fx_LeanchoiliaFlagella_Lash': lash_fx(),
    'Fx_LeanchoiliaFlagella_Ready': ready_fx(),
    'Fx_MarrellaCuticle_Reflect': reflect_fx(),
    'Fx_MarrellaCuticle_Stun': stun_fx(),
    'Proj_MarrellaCuticle_Reflected': reflected_proj(),
    'Fx_ClonalBud_Sprout': bud_fx(),
    'Fx_ClonalBud_Dissolve': dissolve_fx(),
}
for name, frames in fx.items():
    save_sheet(frames, f'{HERE}/Effects/{name}.png')


# =========================================================================================
# Precio Inestable: corazones
# =========================================================================================
GOLD = [C('#7a4a10'), C('#c88a20'), C('#ffd860'), C('#fff6c8')]
CRACKS = [
    [[(3, 4), (4, 5), (4, 6), (5, 7)], [(12, 3), (11, 4), (11, 5), (10, 6)], [(9, 11), (8, 10), (8, 9)]],
    [[(2, 6), (3, 7), (4, 7), (5, 8)], [(9, 2), (9, 3), (10, 4)], [(12, 9), (11, 9), (10, 10), (9, 10)]],
    [[(5, 2), (5, 3), (6, 4), (6, 5)], [(13, 6), (12, 7), (11, 7)], [(4, 9), (5, 10), (6, 10)]],
]
heart = Image.open(f'{UI}/UI_Heart_Full.png').convert('RGBA')
half = Image.open(f'{UI}/UI_Heart_Half.png').convert('RGBA')
empty = Image.open(f'{UI}/UI_Heart_Empty.png').convert('RGBA')


def unstable_inlay(variant, contour=True):
    w, h = heart.size
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    op = lambda x, y: 0 <= x < w and 0 <= y < h and heart.getpixel((x, y))[3] > 0
    for y in range(h):
        for x in range(w):
            if contour and op(x, y) and not all(op(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                out.putpixel((x, y), GOLD[1] if (x + y) % 4 else GOLD[2])
    for stroke in CRACKS[variant]:
        for i, (x, y) in enumerate(stroke):
            if op(x, y): out.putpixel((x, y), GOLD[3] if i == 0 else GOLD[2] if i == 1 else GOLD[1])
    return out


inlays = [unstable_inlay(v) for v in range(3)]
# Con el corazón morado a la vez: el contorno se queda morado y el dorado va solo en las grietas.
cracks = [unstable_inlay(v, contour=False) for v in range(3)]
os.makedirs(f'{HERE}/UI', exist_ok=True)
for v, im in enumerate(cracks): im.save(f'{HERE}/UI/UI_Heart_UnstableCracks_{v}.png')
for v, im in enumerate(inlays):
    im.save(f'{HERE}/UI/UI_Heart_UnstableInlay_{v}.png') if os.path.isdir(f'{HERE}/UI') else (
        os.makedirs(f'{HERE}/UI'), im.save(f'{HERE}/UI/UI_Heart_UnstableInlay_{v}.png'))


def heart_break():
    """El medio corazón extra se rompe: destello dorado y esquirlas que saltan. 24×24, 6 cuadros, centrado en el corazón."""
    shards = [(-1, -1.2), (1.2, -1), (-1.4, 0.3), (1.3, 0.6), (0.2, 1.3), (-0.4, -1.5)]
    frames = []
    for i in range(6):
        cv = Canvas(24)
        if i == 0:
            sparks(cv, 12, 12, 3)
        elif i == 1:
            sparks(cv, 12, 12, 2)
        for k, (dx, dy) in enumerate(shards):
            if i == 0: continue
            d = 2 + i * 1.8
            x, y = 12 + dx * d, 12 + dy * d + i * i * 0.15
            a = 255 if i < 4 else 140
            cv.put(x, y, (*GOLD[2][:3], a)); cv.put(x + 1, y, (*GOLD[1][:3], a))
            if k % 2 == 0: cv.put(x, y + 1, (*GOLD[0][:3], a))
        frames.append(cv.img())
    return frames


save_sheet(heart_break(), f'{HERE}/UI/Fx_Heart_UnstableBreak.png')


def special_badge():
    """Insignia de mutación especial: gema dorada de 8×8 que va en la esquina del icono (tarjetas, HUD y pausa)."""
    cv = Canvas(8)
    for y in range(8):
        for x in range(8):
            if abs(x - 3.5) + abs(y - 3.5) <= 3.6:
                cv.put(x, y, GOLD[3] if x + y < 6 else GOLD[2] if x + y < 8 else GOLD[1])
    cv.put(3, 2, C('#ffffff'))
    cv.outline()
    return cv.img()


badge = special_badge()
badge.save(f'{HERE}/UI/UI_Special_Badge.png')


# =========================================================================================
# Iconos (16 y 32)
# =========================================================================================
def icon_outline(cv):
    cv.outline()


def icon_leanchoilia(n):
    s = n / 32; cv = Canvas(n)
    # escudo de la cabeza y dos segmentos del cuerpo, abajo a la izquierda
    disk(cv, 9 * s, 24 * s, 7 * s, 5 * s, STALK)
    disk(cv, 4 * s, 28.5 * s, 4.5 * s, 3 * s, STALK)
    cv.put(11 * s, 22 * s, C('#ffe080'))
    if n >= 32: cv.put(12 * s, 22 * s, C('#ffe080')); cv.put(11 * s, 23 * s, OUTL)
    # dos grandes apéndices curvados hacia arriba a la derecha
    for (ax, ay, a0, bend, ln) in ((13, 20, -60, 35, 9), (15, 23, -25, 30, 8)):
        x, y = ax * s, ay * s; ang = math.radians(a0)
        for k in range(int(ln * 2 * s) + 1):
            t = k / max(1, ln * 2 * s)
            a = ang + math.radians(bend) * t
            x += math.cos(a) * 0.5; y += math.sin(a) * 0.5
            cv.put(x, y, C(STALK[3] if (k // max(1, int(3 * s))) % 2 == 0 else STALK[2]))
            if n >= 32: cv.put(x - math.sin(a) * 0.9, y + math.cos(a) * 0.9, C(STALK[1]))
        tips = (x, y, a)
        if n >= 32 or a0 == -60:
            for j in range(3):
                b = tips[2] + (j - 1) * math.radians(30)
                for k in range(int(9 * s * 1.6)):
                    t = k / (9 * s * 1.6)
                    cv.put_out(tips[0] + math.cos(b) * k * 0.62, tips[1] + math.sin(b) * k * 0.62,
                               C('#ffffff') if t > 0.8 else C('#bff4ea'))
    stalk_only = {p for p, c in cv.px.items() if c in (C('#bff4ea'), C('#ffffff'))}
    cv.outline(skip=stalk_only)
    return cv.img()


def icon_marrella(n):
    s = n / 32; cv = Canvas(n)
    body = ['#4a3a5a', '#7a6a8a', '#b8a8c4', '#ece4f2']
    disk(cv, 16 * s, 21 * s, 5.5 * s, 4.5 * s, body)
    # brillo iridiscente sobre el cuerpo
    for (x, y) in list(cv.px):
        if (x - y) % max(2, int(4 * s)) == 0: cv.px[(x, y)] = hue((x + y) / n, 0.45, 1.0)
    cv.outline()
    # dos pares de espinas largas que se echan hacia atrás
    for (x0, y0, dx, dy, ln, bend, h0) in ((14, 17, -0.5, -1, 15, -25, 0.0), (18, 17, 0.5, -1, 15, 25, 0.5),
                                          (12, 19, -1, -0.3, 11, -20, 0.25), (20, 19, 1, -0.3, 11, 20, 0.75)):
        x, y = x0 * s, y0 * s; ang = math.atan2(dy, dx)
        for k in range(int(ln * 2 * s)):
            t = k / max(1, ln * 2 * s - 1)
            a = ang + math.radians(bend) * t
            x += math.cos(a) * 0.5; y += math.sin(a) * 0.5
            cv.put_out(x, y, hue(h0 + t * 0.35, 0.32, 1.0))
            if n >= 32 and t < 0.4: cv.put_out(x + 1, y, hue(h0 + t * 0.35, 0.45, 0.85))
    # patas finas debajo
    for k in range(3):
        cv.put_out((12 + k * 4) * s, 26.5 * s, C(body[1])); cv.put_out((12.5 + k * 4) * s, 27.5 * s, C(body[0]))
    return cv.img()


def icon_bud(n):
    s = n / 32; cv = Canvas(n)
    lime = ['#3a6a1a', '#6aa62a', '#a8dc4a', '#e8ffb0']
    # la copia translúcida, detrás y arriba a la derecha
    ghost = Canvas(n)
    disk(ghost, 21 * s, 12 * s, 7.5 * s, 7.5 * s, [(*CLONE, 120), (*CLONE, 150), (*CLONE, 180), (255, 255, 255, 200)])
    for (x, y), c in ghost.px.items(): cv.px[(x, y)] = c
    # puente de la yema
    for k in range(int(5 * s) + 1):
        cv.put(14 * s + k * 0.8, 17 * s - k * 0.6, (*CLONE, 200))
    # la célula sólida con sus ojos
    disk(cv, 11 * s, 21 * s, 8 * s, 8 * s, lime)
    ex, ey = int(10 * s), int(19 * s)
    for dx in range(2 if n >= 32 else 1):
        for dy in range(2 if n >= 32 else 1):
            cv.put(ex + dx, ey + dy, C('#f4f4e8')); cv.put(ex + dx + int(4 * s), ey + dy, C('#f4f4e8'))
    if n >= 32: cv.put(ex + 1, ey + 1, OUTL); cv.put(ex + 1 + int(4 * s), ey + 1, OUTL)
    cv.outline(skip={p for p, c in cv.px.items() if c[3] < 255})
    if n >= 32: sparks(cv, 26, 5, 2)
    else: cv.put(13, 3, C('#ffffff'))
    return cv.img()


icons = {'LeanchoiliaFlagella': icon_leanchoilia, 'MarrellaCuticle': icon_marrella, 'ClonalBud': icon_bud}
icon_imgs = {}
for name, fn in icons.items():
    for n in (16, 32):
        im = fn(n); icon_imgs[(name, n)] = im
        os.makedirs(f'{HERE}/Icons', exist_ok=True)
        im.save(f'{HERE}/Icons/Icon_{name}_{n}.png')


# =========================================================================================
# Vistas previas y GIF
# =========================================================================================
BG = C('#0f2a4a')
S = 4


def compose(body_im, layer_im, mut):
    """Cuerpo con la capa encima (Extremidades detrás del cuerpo, Piel delante), en el lienzo de 40."""
    out = Image.new('RGBA', (NL, NL), (0, 0, 0, 0))
    out.alpha_composite(body_im, (OFF, OFF))
    if mut == 'MarrellaCuticle': out.alpha_composite(layer_im)
    return out


def preview_layers():
    rows = [(mut, body, anim, f) for mut in ('MarrellaCuticle',) for body in BODY_NAMES for anim in ANIMS for f in FACINGS]
    cell = NL * S + 6; rh = NL * S + 6
    W = 250 + cell * 6; H = 10 + len(rows) * rh
    bg = Image.new('RGBA', (W, H), BG); d = ImageDraw.Draw(bg)
    for r, (mut, body, anim, f) in enumerate(rows):
        y0 = 10 + r * rh
        d.text((10, y0 + rh // 2 - 6), f'{mut} {body} {anim} {f}', fill=C('#bfe6ff'))
        for i, (b, l) in enumerate(zip(bodies[(body, anim, f)], layers[(mut, body, anim, f)])):
            big = compose(b, l, mut).resize((NL * S, NL * S), Image.NEAREST)
            bg.alpha_composite(big, (250 + i * cell, y0))
    bg.save(f'{HERE}/vista_previa_capas.png')


def preview_summary():
    """Hoja corta para revisar de un vistazo: los 3 cuerpos en reposo de lado y de frente con cada capa, la copia teñida,
    los efectos, los corazones e iconos."""
    S2 = 5
    W, H = 1180, 2050
    bg = Image.new('RGBA', (W, H), BG); d = ImageDraw.Draw(bg)
    y = 10
    for mut, title in (('MarrellaCuticle', 'Cutícula de Marrella (Piel): reposo y ataque de lado, de frente y de espaldas'),):
        d.text((10, y), title, fill=C('#ffe080')); y += 18
        for body in BODY_NAMES:
            x = 10
            d.text((x, y + 40), body, fill=C('#bfe6ff')); x += 90
            for anim, f, i in (('Idle', 'side', 0), ('Move', 'side', 1), ('Attack', 'side', 2), ('Attack', 'side', 3),
                               ('Idle', 'down', 1), ('Attack', 'down', 3), ('Idle', 'up', 2), ('Attack', 'up', 3)):
                im = compose(bodies[(body, anim, f)][i], layers[(mut, body, anim, f)][i], mut).resize((NL * 2, NL * 2), Image.NEAREST)
                bg.alpha_composite(im, (x, y)); x += im.width + 4
            y += NL * 2 + 4
        y += 10
    d.text((10, y), 'Yema clonal: la copia es tu sprite pasado a verde agua y translúcido (shader sencillo, sin hojas nuevas)', fill=C('#ffe080')); y += 18
    x = 10
    for body in BODY_NAMES:
        for f in FACINGS:
            b = bodies[(body, 'Idle', f)][0]
            tint = Image.new('RGBA', b.size, (0, 0, 0, 0))
            for yy in range(b.height):
                for xx in range(b.width):
                    r, g, bb, a = b.getpixel((xx, yy))
                    if a:
                        lum = (0.3 * r + 0.59 * g + 0.11 * bb) / 255
                        ramp = CLONE_RAMP[min(3, int(lum * 4.2))] if (r, g, bb, a) != OUTL else CLONE_EDGE
                        tint.putpixel((xx, yy), (*ramp[:3], int(a * (0.85 if ramp is CLONE_EDGE else 0.6))))
            pair = Image.new('RGBA', (52, 26), (0, 0, 0, 0))
            pair.alpha_composite(b, (0, 1)); pair.alpha_composite(tint, (26, 1))
            big = pair.resize((52 * 4, 26 * 4), Image.NEAREST)
            bg.alpha_composite(big, (x, y)); x += big.width + 10
            if x > W - 220: x = 10; y += 26 * 4 + 6
    y += 26 * 4 + 16
    for name, frames in fx.items():
        d.text((10, y), name, fill=C('#bfe6ff'))
        sc = 3 if frames[0].width >= 48 else 4 if frames[0].width >= 24 else 6
        x = 260
        for im in frames:
            big = im.resize((im.width * sc, im.height * sc), Image.NEAREST)
            bg.alpha_composite(big, (x, y)); x += big.width + 4
        y += frames[0].height * sc + 8
    d.text((10, y), 'Inestable: borde dorado sobre lleno, medio y vacío; rotura; insignia', fill=C('#ffe080')); y += 18
    x = 10
    for base in (heart, half, empty):
        for v in range(3):
            im = base.copy(); im.alpha_composite(inlays[v])
            bg.alpha_composite(im.resize((96, 96), Image.NEAREST), (x, y)); x += 100
    for im in heart_break():
        bg.alpha_composite(im.resize((96, 96), Image.NEAREST), (x, y)); x += 100
        if x > W - 100: break
    y += 112
    d.text((10, y), 'Iconos (16 y 32, ×6 y ×3) con la insignia de especial en la esquina', fill=C('#ffe080')); y += 18
    x = 10
    for (name, n), im in icon_imgs.items():
        big = im.resize((96, 96), Image.NEAREST)
        bg.alpha_composite(big, (x, y))
        if n == 32: bg.alpha_composite(badge.resize((24, 24), Image.NEAREST), (x + 72, y))
        x += 112
    bg.alpha_composite(badge.resize((96, 96), Image.NEAREST), (x, y))
    bg.save(f'{HERE}/vista_previa.png')


def gifs():
    os.makedirs(f'{HERE}/gif', exist_ok=True)
    speed = {'Idle': 180, 'Move': 110, 'Attack': 70}
    for mut in ('MarrellaCuticle',):
        for body in BODY_NAMES:
            for anim in ANIMS:
                out = []
                for f in FACINGS:
                    pass
                frames = []
                for i in range(len(bodies[(body, anim, 'side')])):
                    row = Image.new('RGBA', (NL * 3, NL), BG)
                    for j, f in enumerate(FACINGS):
                        row.alpha_composite(compose(bodies[(body, anim, f)][i], layers[(mut, body, anim, f)][i], mut), (j * NL, 0))
                    frames.append(row.resize((NL * 3 * 4, NL * 4), Image.NEAREST).convert('RGB'))
                frames[0].save(f'{HERE}/gif/{mut}_{body}_{anim}.gif', save_all=True, append_images=frames[1:], duration=speed[anim], loop=0)
    for name, frames in fx.items():
        n = frames[0].width
        out = []
        for im in frames:
            b = Image.new('RGBA', (n, n if im.height == n else im.height), BG); b.alpha_composite(im)
            out.append(b.resize((b.width * 6, b.height * 6), Image.NEAREST).convert('RGB'))
        out[0].save(f'{HERE}/gif/{name}.gif', save_all=True, append_images=out[1:], duration=100 if 'Lash' in name else 80, loop=0)


preview_layers()
preview_summary()
gifs()
print('Listo:', HERE, '-', len(layers), 'hojas de capas,', len(fx), 'efectos')
