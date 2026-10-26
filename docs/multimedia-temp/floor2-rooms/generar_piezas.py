"""Piezas de las salas especiales del piso 2 (lo que en los planos era provisional).

Uso: python generar_piezas.py        (después de generar_salas_especiales.py, que deja los planos en Layouts/)

Salida en Pieces/ (hojas en tira; vista previa en vista_previa_piezas.png y gif/):
  Clam_Small_{Idle,Open,Opened}.png   cofre pequeño: una almeja gigante (16×16). Idle 4 cuadros, Open 6, Opened 1.
  Clam_Big_{Idle,Open,Opened}.png     cofre grande de la Gruta escondida (32×24), mismas animaciones.
  HeartCoral_{Idle,Taken}.png         coral del corazón morado (32×32): el corazón flota y late (6 cuadros); sin él, 1.
  Pickup_HeartPurple.png              el corazón morado suelto, 4 cuadros de 12×12 (por si sale de otra forma).
  UI_Heart_Purple.png                 icono del HUD (16×16), como el medio corazón verde del Herbívoro pero morado.
  WaveVent_{Idle,Active}.png          entrada de oleadas (16×16): grieta del suelo. Idle 4 cuadros, Active 6.
  Shadow_Swim_{S,M,L}.png             sombra separada de los que nadan (12×4, 20×6, 32×8).
  Fossil_Skeleton.png                 el esqueleto del Fósil gigante dibujado sobre sus celdas F del plano,
                                      con el origen en la esquina de arriba a la izquierda de la sala.
  Fossil_Vertebra.png / Fossil_Rib.png  piezas sueltas de 16×16 para otras salas.
"""
import math, os, random, sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
TEMP = os.path.normpath(os.path.join(HERE, '..'))
ART = os.path.normpath(os.path.join(TEMP, '..', '..', 'Assets', '_Primordia', 'Art'))
sys.path.insert(0, os.path.join(TEMP, 'floor2-bosses'))
from base_piso2 import Canvas, C, OUTL, ellipse, rot_ellipse, fill, bubble, sparks, save_sheet, gif

OUT = os.path.join(HERE, 'Pieces')
SHELL = ['#5a4a6a', '#9a7a9a', '#d0aab8', '#f4dce0']      # concha nacarada, rosada
SHELL_IN = ['#a0506a', '#d07a8a', '#f0a8b0', '#ffe0e0']   # interior
BIOMASS = ['#4e6e1a', '#8eb032', '#c8dc58', '#f4ffb0']
BONE = ['#7a6a4e', '#b4a27c', '#dccca4', '#f6eed6']
CORAL = ['#5a1a4a', '#9a3a7a', '#d070b0', '#f8b0e0']
PURPLE = ['#4a1a7a', '#8a40d0', '#c080ff', '#f0d8ff']


def sheet(frames):
    w, h = frames[0].size
    im = Image.new('RGBA', (w * len(frames), h))
    for i, f in enumerate(frames): im.paste(f, (i * w, 0))
    return im


# ---------------------------------------------------------------------------------------- almejas
def clam(n_w, n_h, s, lift=0.0, glow=0.0, bubbles=0, phase=0.0):
    """Almeja vista desde arriba en 3/4: la valva de arriba, un abanico con costillas; al abrirse se levanta
    y deja ver el interior con biomasa. s = escala (1 pequeña, 2 grande)."""
    cv = Canvas(max(n_w, n_h))
    cx, base = n_w / 2, n_h - 2 * s
    rx, ry = 6.6 * s, 5.4 * s
    # valva de abajo (la que queda en el suelo) y su interior
    ellipse(cv, cx, base - ry * 0.55, rx, ry * 0.62, SHELL, shift=-1)
    if lift > 0:
        ellipse(cv, cx, base - ry * 0.62, rx * 0.86, ry * 0.5, SHELL_IN)
        k = min(1, lift)
        for i, dx in enumerate((-2.2, 0, 2.2)):                  # biomasa dentro
            r = (1.6 if i == 1 else 1.2) * s
            ellipse(cv, cx + dx * s, base - ry * 0.62 - 0.5 * s, r, r, BIOMASS, shift=1 if glow > 0.5 else 0)
    # valva de arriba: sube y se inclina hacia atrás al abrirse (se ve más corta)
    top_y = base - ry * 0.75 - lift * 4.2 * s
    sq = 1 - lift * 0.45
    pivot_y = top_y + 0.35 * ry * sq
    def shell_in(x, y):
        dx, dy = (x - cx) / rx, (y - top_y) / (ry * sq)
        a = math.atan2(y - pivot_y, (x - cx) * ry / rx)
        wave = 1 + 0.035 * math.cos(a * 11)
        return dy <= 0.35 and dx * dx + dy * dy <= wave * wave
    fill(cv, shell_in, cx, top_y, rx, ry * sq, SHELL)
    ramp = [C(c_) for c_ in SHELL]
    for (x, y), col in list(cv.px.items()):                       # costillas: gajos alternos un tono más oscuros
        if not shell_in(x + .5, y + .5) or col not in ramp: continue
        a = math.atan2(y + .5 - pivot_y, (x + .5 - cx) * ry / rx)
        f = (a + math.pi / 2) / 0.3 + 0.5
        if math.floor(f) % 2 == 1 and math.hypot(x + .5 - cx, y + .5 - pivot_y) > 1.5 * s:
            cv.px[(x, y)] = ramp[max(0, ramp.index(col) - 1)]
    for dx_ in (-1.6, 1.6):                                        # orejas de la bisagra
        ellipse(cv, cx + dx_ * s, pivot_y + 0.6 * s, 1.4 * s, 1.0 * s, SHELL, shift=-1)
    cv.put_in(cx - rx * 0.45, top_y - ry * sq * 0.45, C('#ffffff'))   # brillo nacarado
    cv.put_in(cx - rx * 0.4, top_y - ry * sq * 0.55, C(SHELL[3]))
    cv.outline()
    if glow > 0:
        for yy in range(int(base - ry * 2), int(base)):
            for xx in range(int(cx - rx), int(cx + rx) + 1):
                d = math.hypot((xx + .5 - cx) / rx, (yy + .5 - (base - ry * 0.62)) / (ry * 0.9))
                if 0.6 < d < 1.15: cv.put_out(xx, yy, (220, 255, 140, int(120 * glow * (1.15 - d) / 0.55)))
    for i in range(bubbles):
        rnd = random.Random(i * 13 + int(phase * 10))
        bubble(cv, cx + rnd.uniform(-2, 3) * s, top_y - 2 * s - i * 2.6 * s - (phase % 1) * 2, 0.9 + 0.4 * (i % 2) * s, 200)
    im = cv.img().crop((0, 0, n_w, n_h)) if n_w != n_h else cv.img()
    if n_w != n_h:   # el lienzo es cuadrado: recolocar abajo
        full = cv.img()
        im = full.crop((0, 0, n_w, n_h))
    return im


def clam_set(w, h, s, prefix):
    idle = [clam(w, h, s, bubbles=1 if i in (1, 2) else 0, phase=i / 4) for i in range(4)]
    opening = [clam(w, h, s, lift=l, glow=g) for l, g in ((0.12, 0), (0.35, 0.2), (0.65, 0.6), (0.9, 1), (1.0, 1), (1.0, 0.6))]
    opened = [clam(w, h, s, lift=1.0, glow=0.3)]
    return {f'{prefix}_Idle': idle, f'{prefix}_Open': opening, f'{prefix}_Opened': opened}


# ---------------------------------------------------------------------------------------- coral del corazón
def heart_shape(cv, cx, cy, r, ramp, outline=True):
    """Corazón pixelado de radio r (dos lóbulos y la punta)."""
    def inside(x, y):
        dx, dy = (x - cx) / r, (y - cy) / r
        lobe = min(math.hypot(dx + 0.5, dy + 0.25), math.hypot(dx - 0.5, dy + 0.25)) <= 0.55
        tip = dy >= -0.2 and abs(dx) <= (1.0 - (dy + 0.2) / 1.2) * 1.0 and dy <= 1.0
        return lobe or tip
    fill(cv, inside, cx, cy - r * 0.2, r, r, ramp)


def heart_coral(frame, taken=False):
    cv = Canvas(32)
    rnd = random.Random(5)
    # base de roca y ramas de coral que se abren en abanico
    ellipse(cv, 16, 27, 11, 3.5, ['#1a1a2e', '#2a2a44', '#3a3a5a', '#4e4e72'])
    for a, L, x0 in ((-2.35, 13, 12), (-1.95, 16, 14), (-1.57, 15, 16), (-1.2, 16, 18), (-0.8, 13, 20)):
        x, y = x0, 26
        sway = 0.05 * math.sin(frame * math.tau / 6 + x0)
        for t in range(L):
            r = 2.0 - t * 0.09
            ellipse(cv, x, y, r, r, CORAL, shift=0 if t % 3 else -1)
            a2 = a + sway + 0.12 * math.sin(t * 0.6 + x0)
            x += math.cos(a2) * 1.1; y += math.sin(a2) * 1.1
        ellipse(cv, x, y, 1.6, 1.6, CORAL, shift=1)              # puntas claras
    cv.outline()
    for _ in range(9):                                          # pólipos
        x, y = rnd.randrange(6, 26), rnd.randrange(10, 24)
        cv.put_in(x, y, C(CORAL[3]))
    if not taken:
        bob = math.sin(frame * math.tau / 6) * 1.2
        beat = 1 + 0.12 * max(0, math.sin(frame * math.tau / 6 * 2))
        hc = Canvas(32)
        heart_shape(hc, 16, 9 + bob, 4.6 * beat, PURPLE)
        hc.outline()
        hc.put_in(14, 7 + bob, C('#ffffff'))
        # halo
        for yy in range(0, 20):
            for xx in range(6, 27):
                d = math.hypot(xx + .5 - 16, yy + .5 - (9 + bob))
                if 6 < d < 9.5: hc.put_out(xx, yy, (190, 120, 255, int(90 * (9.5 - d) / 3.5 * (0.8 + 0.2 * beat))))
        if frame % 3 == 0: sparks(hc, 22, 4 + bob, 1)
        base = cv.img(); base.alpha_composite(hc.img()); return base
    return cv.img()


def pickup_heart(frame):
    cv = Canvas(12)
    beat = 1 + 0.15 * max(0, math.sin(frame * math.tau / 4))
    heart_shape(cv, 6, 6.2, 3.6 * beat, PURPLE)
    cv.outline()
    cv.put_in(4, 4, C('#ffffff'))
    return cv.img()


def ui_heart_purple():
    """El medio corazón verde de la barrera, recoloreado a morado."""
    src = Image.open(os.path.join(ART, 'UI', 'UI_Heart_Barrier.png')).convert('RGBA')
    remap = {(130, 220, 110): (192, 128, 255), (64, 170, 84): (138, 64, 208), (210, 255, 180): (232, 200, 255),
             (236, 255, 220): (248, 232, 255), (34, 90, 52): (74, 26, 122)}
    px = src.load()
    for y in range(src.height):
        for x in range(src.width):
            r, g, b, a = px[x, y]
            if (r, g, b) in remap: px[x, y] = remap[(r, g, b)] + (a,)
    return src


# ---------------------------------------------------------------------------------------- entrada de oleadas
def vent(frame, active=False):
    """Grieta del suelo por la que salen enemigos. Activa: se abre, brilla en rojo y suelta burbujas."""
    cv = Canvas(16)
    openk = 0 if not active else min(1, frame / 2)
    # borde de arena levantada
    ellipse(cv, 8, 9, 6.5, 4, ['#1e3a44', '#2a5058', '#3a6a70', '#58908c'], alpha=255)
    # la grieta: zigzag oscuro que se ensancha
    w = 1 + openk * 1.6
    for x in range(3, 13):
        y = 9 + round(math.sin(x * 1.3) * 1.2)
        for dy in range(-int(w), int(w) + 1):
            cv.put(x, y + dy, C('#05080e'))
        if active and openk > 0.5 and x % 3 == frame % 3:
            cv.put(x, y, C('#ff6f80'))
    cv.outline()
    if active:
        for i in range(2 + frame // 2):
            rnd = random.Random(i * 7 + frame)
            bubble(cv, 8 + rnd.uniform(-4, 4), 7 - i * 1.5 - frame * 0.6, 0.8 + 0.5 * (i % 2), 210)
        if frame in (2, 3):
            for yy in range(2, 15):
                for xx in range(1, 16):
                    d = math.hypot(xx + .5 - 8, (yy + .5 - 9) * 1.6)
                    if 6.5 < d < 8: cv.put_out(xx, yy, (255, 111, 128, 120))
    elif frame == 2:
        bubble(cv, 9, 5, 0.9, 160)
    return cv.img()


# ---------------------------------------------------------------------------------------- sombras
def shadow(w, h):
    im = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    px = im.load()
    for y in range(h):
        for x in range(w):
            d = ((x + .5 - w / 2) / (w / 2)) ** 2 + ((y + .5 - h / 2) / (h / 2)) ** 2
            if d <= 1: px[x, y] = (6, 10, 20, 110 if d < 0.55 else 70)
    return im


# ---------------------------------------------------------------------------------------- fósil
def bone_seg(cv, x0, y0, x1, y1, r):
    L = max(1, int(math.hypot(x1 - x0, y1 - y0) * 2))
    for i in range(L + 1):
        t = i / L
        ellipse(cv, x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, r, r, BONE)


def fossil_skeleton(rows):
    """El esqueleto de una criatura enorme sobre las celdas F del plano: vértebras, costillas curvas y un cráneo
    acorazado con la mandíbula de cuchillas. Medio enterrado en la arena."""
    h, w = len(rows), len(rows[0])
    F = {(x, y) for y in range(h) for x in range(w) if rows[y][x] == 'F'}
    W, H = w * 16, h * 16
    cv = Canvas(max(W, H))
    ctr = lambda c: (c[0] * 16 + 8, c[1] * 16 + 8)
    # cráneo: el bloque macizo más a la derecha (las celdas con todos sus vecinos F, más su borde)
    core = {c for c in F if sum((c[0] + dx, c[1] + dy) in F for dx in (-1, 0, 1) for dy in (-1, 0, 1)) >= 9}
    skull = {c for c in F if any(abs(c[0] - k[0]) <= 1 and abs(c[1] - k[1]) <= 1 for k in core)}
    sx0 = min(c[0] for c in skull) * 16; sx1 = (max(c[0] for c in skull) + 1) * 16
    sy0 = min(c[1] for c in skull) * 16; sy1 = (max(c[1] for c in skull) + 1) * 16
    spine_y = max(((y, sum(1 for c in F if c[1] == y)) for y in range(h)), key=lambda t: t[1])[0]
    spine = sorted(c for c in F if c[1] == spine_y and c[0] * 16 < sx0)
    sy = spine_y * 16 + 8
    # costillas: de la columna a la celda F más lejana de cada una, curvadas hacia atrás y cada vez más finas
    ribs = [c for c in F if c not in skull and c[1] != spine_y]
    tips = {}
    for c in ribs:
        side = -1 if c[1] < spine_y else 1
        key = (side, c[0] + (1 if abs(c[1] - spine_y) == 3 else 0))
        if key not in tips or abs(c[1] - spine_y) > abs(tips[key][1] - spine_y): tips[key] = c
    for (side, x_attach), tip in tips.items():
        p0 = (x_attach * 16 + 8, sy)
        p2 = ctr(tip)
        p1 = (p0[0] + 4, p2[1] - side * 4)
        n = 24
        for i in range(n + 1):
            t = i / n
            x = (1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0]
            y = (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]
            ellipse(cv, x, y, 2.4 - 1.1 * t, 2.4 - 1.1 * t, BONE)
    for c in spine:                                        # vértebras: un disco con apófisis arriba y abajo
        x, y = ctr(c)
        bone_seg(cv, x - 8, y, x + 8, y, 2.6)
        ellipse(cv, x, y, 4.2, 5, BONE, shift=1)
        bone_seg(cv, x, y - 5, x + 1, y - 8, 1.3); bone_seg(cv, x, y + 5, x + 1, y + 8, 1.3)
    tx, ty = ctr(spine[0])                                 # cola: abanico de huesecillos
    for a in (-0.5, -0.2, 0.15, 0.45):
        bone_seg(cv, tx - 6, ty, tx - 6 - math.cos(a) * 14, ty + math.sin(a) * 14, 1.3)
    # cráneo acorazado (placodermo): placas, cuenca del ojo y mandíbula de cuchillas mirando a la derecha
    scx, scy = (sx0 + sx1) / 2, (sy0 + sy1) / 2
    hw, hh = (sx1 - sx0) / 2, (sy1 - sy0) / 2
    ellipse(cv, scx - 2, scy - 2, hw + 2, hh - 6, BONE)                     # escudo de la cabeza
    ellipse(cv, scx + hw * 0.35, scy + hh * 0.42, hw * 0.75, hh * 0.32, BONE, shift=-1)   # mandíbula
    for yy in range(int(sy0), int(sy1)):                   # costuras de las placas
        for xx in range(int(sx0) - 4, int(sx1) + 4):
            if abs((xx - scx) * 0.7 + (yy - scy + 6)) < 0.55 or (abs(xx - (scx - hw * 0.3)) < 0.55 and yy < scy + 4):
                cv.put_in(xx, yy, C(BONE[1]))
    ellipse(cv, scx + hw * 0.3, scy - hh * 0.3, 5.5, 4.6, ['#2a2418', '#3a3222', '#4a402c', '#5a4e38'])   # cuenca del ojo
    jaw_y = scy + hh * 0.2
    for i in range(6):                                     # cuchillas de la mandíbula
        x = scx + hw * 0.9 - i * 5
        for k in range(5):
            cv.put(x - k * 0.4, jaw_y + k, C(BONE[3] if k < 2 else BONE[2]))
    cv.outline()
    im = cv.img().crop((0, 0, W, H))
    # sombra en el suelo, para que los huesos no floten
    sh = Image.new('RGBA', im.size, (0, 0, 0, 0))
    sh.paste((4, 10, 20, 90), (2, 3), im.split()[3].point(lambda a: 255 if a > 100 else 0))
    sh.alpha_composite(im)
    return sh


def single_bone(kind):
    cv = Canvas(16)
    if kind == 'vertebra':
        bone_seg(cv, 0, 8, 16, 8, 2.4); ellipse(cv, 8, 8, 4, 4.6, BONE, shift=1)
        bone_seg(cv, 8, 3, 9, 1, 1.2); bone_seg(cv, 8, 13, 9, 15, 1.2)
    else:
        bone_seg(cv, 3, 14, 7, 7, 2); bone_seg(cv, 7, 7, 12, 3, 1.8)
    cv.outline()
    return cv.img()


if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True); os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True)
    anims = {}
    anims.update(clam_set(16, 16, 1, 'Clam_Small'))
    anims.update(clam_set(32, 24, 2, 'Clam_Big'))
    anims['HeartCoral_Idle'] = [heart_coral(i) for i in range(6)]
    anims['HeartCoral_Taken'] = [heart_coral(0, taken=True)]
    anims['Pickup_HeartPurple'] = [pickup_heart(i) for i in range(4)]
    anims['WaveVent_Idle'] = [vent(i) for i in range(4)]
    anims['WaveVent_Active'] = [vent(i, True) for i in range(6)]
    for k, v in anims.items(): sheet(v).save(os.path.join(OUT, f'{k}.png'))
    ui_heart_purple().save(os.path.join(OUT, 'UI_Heart_Purple.png'))
    for n, (w, h) in {'S': (12, 4), 'M': (20, 6), 'L': (32, 8)}.items():
        shadow(w, h).save(os.path.join(OUT, f'Shadow_Swim_{n}.png'))
    fos = [l.rstrip('\n') for l in open(os.path.join(HERE, 'Layouts', 'Room_Special_FosilGigante.txt'), encoding='utf-8')]
    fossil_skeleton(fos).save(os.path.join(OUT, 'Fossil_Skeleton.png'))
    single_bone('vertebra').save(os.path.join(OUT, 'Fossil_Vertebra.png'))
    single_bone('rib').save(os.path.join(OUT, 'Fossil_Rib.png'))
    # vista previa: cada animación sobre la arena del Arrecife, ampliada ×4
    bg = (33, 72, 98, 255)
    rows_ = list(anims.items())
    cellw = max(f.width for v in anims.values() for f in v) * 4 + 8
    W = 220 + cellw * 6
    prev = Image.new('RGBA', (W, sum(max(f.height for f in v) * 4 + 12 for _, v in rows_) + 20), bg)
    from PIL import ImageDraw
    d = ImageDraw.Draw(prev); y = 10
    for name, v in rows_:
        hh = max(f.height for f in v) * 4
        d.text((8, y + hh // 2 - 6), name, fill=(220, 240, 255))
        for i, f in enumerate(v):
            prev.alpha_composite(f.resize((f.width * 4, f.height * 4), Image.NEAREST), (220 + i * cellw, y))
        y += hh + 12
    prev.save(os.path.join(HERE, 'vista_previa_piezas.png'))
    for k, v in anims.items():
        if len(v) > 1: gif(os.path.join(HERE, 'gif', f'{k}.gif'), v, 140, bg, scale=6)
    print('Piezas listas')
