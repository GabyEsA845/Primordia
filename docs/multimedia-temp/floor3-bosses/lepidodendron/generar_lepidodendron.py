"""Lepidodendron ancestral (jefe secreto del piso 3, tanda 6, subtanda 6l-1): tronco, pie y vetas.

Concepto C (aprobado el 2026-11-01): el tronco gigante sube hasta perderse en una capa de hojas fija en pantalla;
nunca se ve la copa. Tronco de 11 u en el pie (se estrecha a ~8 u), en el centro de la arena algo al norte; no se
pasa por detrás (las raíces de atrás se meten en la pared norte), así que el tronco va siempre DETRÁS del jugador.

Piezas (pivote = el pie del tronco, centro de la base, en todas):
- Lepido_Trunk: el tronco (se oscurece hacia arriba hasta perderse en la copa), con el pie abierto en 5 lóbulos.
- Lepido_Mound: el montículo del pie (suelo): tierra del color del suelo, musgo y raíces Stigmaria medio enterradas.
- Lepido_BackRoots: las dos raíces de atrás que se meten en la pared norte (cierran el paso por detrás).
- Capas de las vetas (encima del tronco): Lepido_Veins_Warn (el brillo sube hacia la copa antes de bajar un cono),
  Lepido_Veins_Hit (destello al golpe).

Uso: python generar_lepidodendron.py [muestra] [piezas] [escena] [copa]
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
MT = os.path.normpath(os.path.join(HERE, '..', '..'))
sys.path.insert(0, os.path.join(MT, 'floor3-bosses', 'arthropleura'))
sys.path.insert(0, os.path.join(MT, 'floor3-environment'))
import generar_escala_arthropleura as A                # room (Smoke), rect_room, frame, font
from base_piso3 import *                               # noqa: F401,F403
from PIL import Image, ImageDraw

U = 16
FROG = os.path.join(MT, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')

# paletas (las usa también generar_escala_lepidodendron.py)
BARK_OLD = [C(c) for c in ('#121614', '#1e2520', '#2e3830', '#435046', '#66725e', '#8c9478')]
VEIN = [C(c) for c in STROB]
LEAF = [C(c) for c in ('#142014', '#203220', '#30482a', '#4c6438')]
LEAF_HI = C('#6c8048')
MOSS = [C(c) for c in ('#26301e', '#3c4a28', '#5a6a36', '#7c8a48')]
LICHEN = [C(c) for c in ('#6e7660', '#8e9878', '#aab494')]
ROOTP = [C(c) for c in ('#141714', '#252b24', '#38402f', '#525a40', '#767c58')]
ROOT_D = [C(c) for c in ('#121510', '#1e231c', '#2c3326', '#3e4632', '#58603f')]
SOIL = [C(c) for c in ('#24251b', '#2e2f21', '#3b3b29', '#4b4932')]
GLOOM = C('#0c100e')
WHITE_V = C('#f4e8ff')

# medidas del tronco (px)
TW_BOT, TW_TOP = 176, 132                              # 11 u en el pie, ~8 u arriba
FL, FLW = 70, 38                                       # ensanche del pie: alto y cuánto se abre cada lado
TH = 352                                               # alto del tronco por encima del pie (22 u: sale de la cámara)
TCW, TCH = 288, TH + 32                                # celda del tronco; pivote (144, TH)
OX, FOOT = TCW // 2, TH

def r_main(y):
    """Radio del fuste (sin el ensanche) a la altura y de la celda."""
    t = (FOOT - y) / FOOT
    return (TW_TOP + (TW_BOT - TW_TOP) * (1 - t) ** 3) / 2

def hw(y):
    """Medio ancho con el ensanche del pie."""
    return r_main(y) + FLW * max(0., (y - (FOOT - FL)) / FL) ** 2.3

def lobe(ph):
    return abs(math.cos(ph * math.pi * 2.5 + .3 * math.sin(ph * 7 + 1)))   # 5 lóbulos irregulares (contrafuertes)

def bottom(ph):
    """Borde de abajo del pie (px por debajo de FOOT) en la posición ph (-1..1)."""
    return -4 + 6 * (1 - ph * ph) + 16 * lobe(ph) ** 1.5 * (1 - .35 * abs(ph)) * (1 + .35 * math.sin(ph * 9 + 1))

def _h(*a):
    """Ruido fijo por celda (no depende de la semilla de Python)."""
    v = 0
    for n in a: v = (v * 1103515245 + int(n) * 2654435761 + 12345) & 0xffffffff
    v ^= v >> 13; v = (v * 1274126177) & 0xffffffff; v ^= v >> 16
    return (v & 0xffff) / 0xffff

# =================================================================================================
# Tronco
# =================================================================================================
def trunk():
    cv = Canvas(TCW, TCH)
    rnd = random.Random(2)
    cw, ch = 16, 18
    for y in range(0, FOOT + 24):
        h = hw(min(y, FOOT))
        R = r_main(min(y, FOOT))
        k_ = max(0., (y - (FOOT - FL)) / FL)            # 0 arriba del ensanche → 1 en el suelo
        gloom = max(0., min(1., (FOOT - 150 - y) / 170))  # se pierde en la penumbra de la copa
        for x in range(int(OX - h), int(OX + h) + 1):
            ph = max(-.999, min(.999, (x - OX) / h))
            if y > FOOT + bottom(ph): continue
            th = math.asin(ph)                          # ángulo en el cilindro: los rombos se estrechan en los bordes
            s = .5 * (x - OX) + .5 * th * R            # a medias entre plano y cilindro: volumen sin ruido
            row = math.floor((FOOT - y) / ch)
            col = math.floor((s + (cw / 2 if row % 2 else 0)) / cw)
            lx = (s + (cw / 2 if row % 2 else 0)) % cw - cw / 2
            ly = (FOOT - y) % ch - ch / 2
            dd = abs(lx) / (cw / 2) + abs(ly) / (ch / 2)
            v = _h(row, col)                            # cada cojinete, un poco distinto
            lam = .62 - .78 * ph + (v - .5) * .14       # luz arriba a la izquierda
            tone = 5 if lam > 1.0 else 4 if lam > .62 else 3 if lam > .22 else 2 if lam > -.12 else 1
            if ph > .86: tone = max(tone, 2)            # luz rebotada en el borde derecho
            eroded = v < .06                            # cojinetes gastados: casi sin relieve
            if dd > .86: c = BARK_OLD[max(0, tone - (2 if eroded else 3))]              # surco (menos hondo si está gastado)
            elif dd > .66 and ly > 0 and lx < 0 and not eroded: c = BARK_OLD[min(5, tone + 1)]   # canto con luz
            elif dd > .66 and ly < 0 and lx > 0 and not eroded: c = BARK_OLD[max(0, tone - 1)]   # canto en sombra
            elif dd < .2: c = BARK_OLD[max(0, tone - 2)]                                  # cicatriz de la hoja
            else: c = BARK_OLD[tone]
            if dd > .86 and y > FOOT - 110 and _h(x, y, 7) < .05 + .25 * k_:
                c = MOSS[0 if tone < 3 else 1]          # musgo en los surcos, más abajo
            if .955 < v < .975 and dd < .5: c = LICHEN[0 if tone < 3 else 1 if tone < 5 else 2]  # liquen
            if k_ > 0:                                  # canales de sombra entre contrafuertes; lomo iluminado
                lb = lobe(ph)
                if lb < .3 and dither(min(1, k_ * 1.7 * (1 - lb / .3)), x, y): c = blend(c, GLOOM, .6)
                elif lb > .82 and (ph * 2.5) % 1 < .5 and dither(k_ * .7, x, y): c = BARK_OLD[min(5, tone + 1)]
            if gloom > 0 and dither(min(1., gloom * 1.15), x, y): c = blend(c, GLOOM, .5 + .45 * gloom)
            cv.put(x, y, c)
    for (x0, y0, L) in ((-30, 140, 60), (22, 200, 48), (-52, 250, 38), (48, 96, 54)):   # grietas viejas
        x = OX + x0
        for j in range(L):
            x += rnd.choice((-1, 0, 0, 0, 1)) * .8
            cv.put(x, y0 + j, BARK_OLD[0])
            if j % 3 == 0: cv.put(x - 1, y0 + j, BARK_OLD[4])
    for k in range(110):                                # musgo suelto del lado de la luz, abajo
        y = rnd.randint(FOOT - 110, FOOT - 6)
        x = OX - hw(y) + rnd.uniform(1, hw(y) * .9)
        if _h(int(x), y, 3) < .4: cv.put(x, y, MOSS[rnd.choice((1, 1, 2))])
    cv.outline(); cv.clean()
    for (x, y) in list(cv.px):                          # arriba se disuelve en trama (nunca un corte recto)
        if y < 40 and not dither(y / 40, x, y): del cv.px[(x, y)]
    return cv

# =================================================================================================
# Vetas (capa encima del tronco)
# =================================================================================================
VEIN_TH = (-.5, .02, .48)                              # ángulo de cada veta en el cilindro

def vein_pts():
    out = []
    for k, t0 in enumerate(VEIN_TH):
        pts = []
        for y in range(40, FOOT + 4):
            th = t0 + .09 * math.sin(y / 23 + k * 2) + .04 * math.sin(y / 7 + k)
            x = OX + math.sin(th) * hw(min(y, FOOT))
            if y > FOOT + bottom((x - OX) / hw(FOOT)) - 3: break
            pts.append((x, y))
        out.append(pts)
    return out

def veins(mode='rest', k=0, n=8):
    """mode 'rest': en reposo, tenues; 'warn': un brillo sube del pie a la copa (cuadro k de n); 'hit': destello."""
    cv = Canvas(TCW, TCH)
    for pts in vein_pts():
        for i, (x, y) in enumerate(pts):
            gl = max(0., min(1., (FOOT - 150 - y) / 170))          # arriba se pierde en la penumbra
            if mode == 'warn':
                front = FOOT + 10 - (FOOT + 10 - 190) * (k + 1) / n   # sube por la parte visible (~7 u) y se mete en la copa
                d = y - front
                lit = 1 if -26 < d < 8 else .55 if 0 <= d < 90 else 0
            else:
                lit = 1 if mode == 'hit' and k == 0 else .55 if mode == 'hit' else 0
            if gl > .85 and lit < 1: continue
            if lit == 1:
                if mode == 'warn':                                    # halo: el aviso tiene que leerse de lejos
                    for o in (-3, -2, 2, 3):
                        if dither(.5 if abs(o) == 3 else .85, int(x) + o, y): cv.put(x + o, y, VEIN[2] if abs(o) == 2 else VEIN[1])
                cv.put(x - 1, y, VEIN[3]); cv.put(x, y, VEIN[4]); cv.put(x + 1, y, VEIN[3])
                if i % 3 == 0 or mode == 'warn' and i % 2 == 0: cv.put(x, y, WHITE_V)
            elif lit > 0:
                cv.put(x, y, VEIN[3]); cv.put(x + 1, y, VEIN[2])
            else:
                if dither(1 - gl, int(x), y): cv.put(x, y, VEIN[1]); cv.put(x + 1, y, VEIN[2] if (y // 30) % 3 else VEIN[1])
    return cv

# =================================================================================================
# Montículo del pie y raíces de atrás (suelo)
# =================================================================================================
MW, MH = 480, 230
MFX, MFY = 240, 96                                     # pie del tronco dentro del montículo

def mound(seed=4):
    rnd = random.Random(seed)
    cv = Canvas(MW, MH)
    RX, RY = 14 * U, 5.4 * U
    ox, oy = MFX, MFY + 6
    for y in range(MH):
        for x in range(MW):
            d = ((x - ox) / RX) ** 2 + ((y - oy) / RY) ** 2
            if d > 1: continue
            if d > .55 and not dither((1 - d) / .45, x, y): continue          # se funde con el suelo
            lit = -((x - ox) / RX * .5 + (y - oy) / RY * .9)
            cv.put(x, y, SOIL[3] if lit > .45 else SOIL[2] if lit > -.1 else SOIL[1])
    for k in range(260):
        a = rnd.uniform(0, 6.28); r = math.sqrt(rnd.random()) * .92
        cv.put(ox + math.cos(a) * RX * r, oy + math.sin(a) * RY * r, MOSS[rnd.choice((1, 2, 2, 3))])
    hwf = hw(FOOT)
    for k, ph in enumerate((-.85, -.45, 0, .45, .85)):                     # Stigmaria: de cada lóbulo
        sx, sy = MFX + ph * hwf * .92, MFY + bottom(ph) - 6
        ang = math.atan2(1.0 - .6 * abs(ph), ph * 1.4)
        L = rnd.uniform(6, 8.5) * U
        bend = rnd.uniform(-.6, .6)
        P_ = lambda t: (sx + math.cos(ang + bend * t) * t * L, sy + math.sin(ang + bend * t) * t * L * .45)
        root(cv, [P_(t) for t in (0, .33, .66, 1)], 20, 6, ROOT_D)
        for t in (.3, .58, .86):                                               # la tierra la tapa: entra y sale
            bx, by = P_(t); r = 13 * (1 - t * .4)
            for yy in range(int(by - r * .6), int(by + r * .6) + 1):
                for xx in range(int(bx - r), int(bx + r) + 1):
                    dd_ = ((xx - bx) / r) ** 2 + ((yy - by) / (r * .6)) ** 2
                    if dd_ <= 1 and (dd_ < .5 or dither((1 - dd_) / .5, xx, yy)):
                        cv.put(xx, yy, SOIL[2] if yy < by else SOIL[1])
    sh = Canvas(MW, MH)                                                    # sombra de contacto (aparte, translúcida)
    for y in range(MH):
        for x in range(MW):
            if ((x - MFX) / (TW_BOT * .78)) ** 2 + ((y - MFY - 4) / 26) ** 2 <= 1: sh.put(x, y, C('#0a0e0c', 90))
    return cv, sh

BW, BH = 520, 210
BFX, BFY = 260, 196                                    # pie del tronco dentro de la pieza

def back_roots(seed=8):
    """Dos raíces gruesas que salen de los lados del pie y se meten en la pared norte (~10,5 u más arriba), con
    raíces menores y tierra oscura entre ellas (cierran el paso por detrás del tronco)."""
    rnd = random.Random(seed)
    cv = Canvas(BW, BH)
    for y in range(0, BFY - 20):                       # tierra oscura detrás del tronco (casi toda tapada)
        w = 70 + (BFY - y) * .5
        for x in range(int(BFX - w), int(BFX + w)):
            if dither(.85, x, y): cv.put(x, y, SOIL[0])
    for s_ in (-1, 1):
        x0 = BFX + s_ * (TW_BOT / 2 + 4)
        main = [(x0 - s_ * 6, BFY - 18), (x0 + s_ * 96, BFY - 26), (x0 + s_ * 40, BFY - 120), (x0 + s_ * 130, 6)]
        for j in range(2):                              # raíces menores que acompañan
            o = rnd.uniform(20, 46)
            root(cv, [(x0, BFY - 26), (x0 + s_ * (70 + o), BFY - 50), (x0 + s_ * (30 + o), BFY - 130), (x0 + s_ * (110 + o * 1.3), 2)], 16, 12, ROOT_D)
        root(cv, main, 42, 30, ROOT_D)
        P_ = lambda t: bezier(*main, n=40)[int(t * 40)]
        for t in (.5, .82):                             # la tierra la tapa a trozos
            bx, by = P_(t); r = 26
            for yy in range(int(by - r * .55), int(by + r * .55) + 1):
                for xx in range(int(bx - r), int(bx + r) + 1):
                    dd_ = ((xx - bx) / r) ** 2 + ((yy - by) / (r * .55)) ** 2
                    if dd_ <= 1 and (dd_ < .45 or dither((1 - dd_) / .55, xx, yy)): cv.put(xx, yy, SOIL[2] if yy < by else SOIL[1])
        for k in range(40): cv.put(x0 + s_ * rnd.uniform(40, 150), rnd.uniform(10, BFY - 40), MOSS[rnd.choice((1, 2, 3))])
    cv.outline(); cv.clean()
    return cv

# =================================================================================================
# Usadas por generar_escala_lepidodendron.py (escalas A/B/C): rama, penacho y musgo colgante
# =================================================================================================
def limb(cv, x, y, ang, L, w0, w1, rnd, sq=1.0, bend=.0):
    pts = []
    n = max(2, int(L))
    for i in range(n + 1):
        t = i / n; a = ang + bend * (t - .5)
        if i == 0: px, py = x, y
        else: px, py = pts[-1][0] + math.cos(a), pts[-1][1] + math.sin(a) * sq
        pts.append((px, py))
    for i, (px, py) in enumerate(pts):
        r = (w0 + (w1 - w0) * i / n) / 2
        for yy in range(int(py - r - 1), int(py + r + 2)):
            for xx in range(int(px - r - 1), int(px + r + 2)):
                ox_, oy_ = xx + .5 - px, yy + .5 - py
                if math.hypot(ox_, oy_) > r + .25: continue
                s = (ox_ * -.6 + oy_ * -.8) / max(r, .5)
                tone = 4 if s > .5 else 3 if s > 0 else 2 if s > -.55 else 1
                if r > 4 and ((xx % 9 == 0 and yy % 10 == 0) or ((xx + 4) % 9 == 0 and (yy + 5) % 10 == 0)):
                    tone = max(0, tone - 2)
                if r > 3 and s > .55 and rnd.random() < .16: cv.put(xx, yy, MOSS[rnd.choice((2, 3, 3))]); continue
                cv.put(xx, yy, BARK_OLD[tone])
    return pts[-1], ang + bend * .5

def tuft(cv, x, y, rnd, big=1.0, sparse=False):
    fr = [(-math.pi / 2 + rnd.uniform(-2.5, 2.5), rnd.uniform(14, 22) * big) for k in range(rnd.randint(16, 20))]
    fr.sort(key=lambda p: -abs(p[0] + math.pi / 2))
    for a, L in fr: frondlet(cv, x, y, a, L, LEAF, .7, 1.8, None, 0 if a < -1.0 else 1)
    for k in range(6):
        a = -math.pi / 2 + rnd.uniform(-1.1, .4); L = rnd.uniform(6, 12) * big
        cv.put(x + math.cos(a) * L, y + math.sin(a) * L, LEAF_HI)

def hang_moss(cv, x, y, rnd, L=None):
    L = L or rnd.randint(12, 30); ph = rnd.uniform(0, 6)
    for j in range(L):
        c = MOSS[3] if j < 2 else MOSS[2] if j < L * .4 else MOSS[1] if j < L - 3 else MOSS[0]
        xx = x + round(math.sin(j / 4 + ph) * .8)
        cv.put(xx, y + j, c)
        if j % 5 == 2 and j < L - 4: cv.put(xx + (1 if j % 10 else -1), y + j, MOSS[1])

# =================================================================================================
# Láminas
# =================================================================================================
def scene(vmode='rest', vk=0):
    """La arena con las piezas nuevas (y la capa de hojas, la luz y la sombra moteada provisionales de la escala C).
    Devuelve el recorte de la cámara ×0,75 (40×22,5 u) y la posición del pie dentro de él (u)."""
    import generar_escala_lepidodendron as E
    W, H = 50, 30
    fx, fy = 25, 15.5                                  # pie del tronco (u)
    im = A.room(A.rect_room(W, H, (2, int(fy - 10.5), W - 3, H - 2)))
    def put(c, px, py):
        im.alpha_composite(c.img().convert('RGBA') if hasattr(c, 'img') else c, (int(fx * U - px), int(fy * U - py)))
    put(back_roots(), BFX, BFY)
    m, sh = mound()
    put(sh, MFX, MFY); put(m, MFX, MFY)
    E.dapple(im, (0, 0, W, H), lambda xu: 1.0)
    put(trunk(), OX, FOOT)
    put(veins(vmode, vk), OX, FOOT)
    im.alpha_composite(A.frame(FROG, 0, 32), (int((fx - 7) * U), int((fy + 5) * U)))
    cx0, cy0 = fx - 20, fy - 8.75 - 1.5
    for (g, f) in (((cx0 + 9, cy0 + 1.5), (cx0 + 13, cy0 + 15)), ((cx0 + 30, cy0 + 1.5), (cx0 + 34, cy0 + 17))):
        E.light_shaft(im, g[0], g[1], f[0], f[1])
    band = E.canopy_band(int(40 * U) + 64, int(3.6 * U), holes=((9 * U + 32, 22), (30 * U + 32, 18)))
    im.alpha_composite(band, (int(cx0 * U) - 32, int(cy0 * U) - 14))
    return im.crop((int(cx0 * U), int(cy0 * U), int((cx0 + 40) * U), int((cy0 + 22.5) * U))), (fx - cx0, fy - cy0)

def lamina_muestra():
    im, (fx, fy) = scene()
    s = 2
    big = im.resize((im.width * s, im.height * s), Image.NEAREST)
    z = 3                                              # detalle ×3 del pie
    det = im.crop((int((fx - 9) * U), int((fy - 6) * U), int((fx + 9) * U), int((fy + 4.5) * U)))
    det = det.resize((det.width * z, det.height * z), Image.NEAREST)
    full = Image.new('RGBA', (max(big.width, det.width), big.height + det.height + 12), (16, 18, 16, 255))
    full.alpha_composite(big, (0, 0)); full.alpha_composite(det, ((full.width - det.width) // 2, big.height + 12))
    _save(full, 'Lepidodendron ancestral · 6l-1 muestra: tronco, pie y vetas (capa de hojas, luz y sombra moteada: aún las de la escala)',
          ['Arriba: la vista del juego (cámara ×0,75) a ×2. Abajo: el pie a ×3. Rombos sobre el cilindro (se estrechan en los bordes), cada cojinete distinto, líquenes, musgo en los surcos,',
           'grietas, luz rebotada a la derecha; el tronco se pierde en la penumbra. Pie en 5 lóbulos → raíces Stigmaria medio enterradas; raíces de atrás hasta la pared norte. Vetas tenues en reposo.'],
          'vista_previa_6l1_muestra.png')

def lamina_vetas():
    frames, labels = [], []
    base = trunk().img().convert('RGBA')
    for mode, k, lab in (('rest', 0, 'reposo'), ('warn', 1, 'aviso 2/8'), ('warn', 3, 'aviso 4/8'), ('warn', 5, 'aviso 6/8'), ('warn', 7, 'aviso 8/8'), ('hit', 0, 'golpe')):
        tr = base.copy()
        tr.alpha_composite(veins(mode, k).img().convert('RGBA'))
        frames.append(tr.crop((0, 60, TCW, TCH))); labels.append(lab)
    cw, chh = frames[0].size
    s = 2
    sheet = Image.new('RGBA', (len(frames) * (cw + 6) * s, (chh + 14) * s), (28, 46, 32, 255))
    d = ImageDraw.Draw(sheet)
    for i, f in enumerate(frames):
        sheet.alpha_composite(f.resize((cw * s, chh * s), Image.NEAREST), (i * (cw + 6) * s, 14 * s))
        d.text((i * (cw + 6) * s + 8, 4), labels[i], fill=(120, 230, 230), font=A.font(18), stroke_width=3, stroke_fill=(10, 10, 14))
    _save(sheet, 'Lepidodendron ancestral · 6l-1 vetas: tenues en reposo; aviso = el brillo sube del pie a la copa antes de bajar un cono; golpe = destello',
          ['Solo aviso y golpe (la vida va en la barra). Capa aparte encima del tronco, misma celda y pivote.'], 'vista_previa_6l1_vetas.png')

def _save(big, title, notes, name):
    head = Image.new('RGBA', (big.width, 34 + 22 * len(notes)), (16, 18, 16, 255))
    hd = ImageDraw.Draw(head)
    hd.text((14, 6), title, fill=(255, 240, 200), font=A.font(20))
    for k, n_ in enumerate(notes): hd.text((14, 32 + 22 * k), n_, fill=(200, 210, 200), font=A.font(15))
    full = Image.new('RGBA', (big.width, big.height + head.height), (16, 18, 16, 255))
    full.alpha_composite(head, (0, 0)); full.alpha_composite(big, (0, head.height))
    full.convert('RGB').save(os.path.join(HERE, name)); print('  ', name, full.size)

# =================================================================================================
# Hojas definitivas, GIF y escena
# =================================================================================================
TREE = os.path.join(HERE, 'Tree')
GIF = os.path.join(HERE, 'gif')
WARN_N, WARN_MS = 8, 70
HIT_MS = 60

def piezas():
    os.makedirs(GIF, exist_ok=True)
    tr = trunk()
    save_sheet(os.path.join(TREE, 'Lepido_Trunk.png'), [tr])
    m, sh = mound()
    save_sheet(os.path.join(TREE, 'Lepido_Mound.png'), [m])
    save_sheet(os.path.join(TREE, 'Lepido_MoundShadow.png'), [sh])
    save_sheet(os.path.join(TREE, 'Lepido_BackRoots.png'), [back_roots()])
    rest = [veins('rest')]
    warn = [veins('warn', k, WARN_N) for k in range(WARN_N)]
    hit = [veins('hit', k) for k in range(2)]
    save_sheet(os.path.join(TREE, 'Lepido_Veins_Rest.png'), rest)
    save_sheet(os.path.join(TREE, 'Lepido_Veins_Warn.png'), warn)
    save_sheet(os.path.join(TREE, 'Lepido_Veins_Hit.png'), hit)
    base = tr.img().convert('RGBA')
    def on(v):
        im = base.copy(); im.alpha_composite(v.img().convert('RGBA')); return im
    gif(os.path.join(GIF, 'Lepido_Veins_Warn.gif'), [on(v) for v in rest * 4 + warn + rest * 4], WARN_MS, BG, 2)
    gif(os.path.join(GIF, 'Lepido_Veins_Hit.gif'), [on(v) for v in rest * 6 + hit + rest * 6], HIT_MS, BG, 2)
    preview(os.path.join(HERE, 'vista_previa_6l1_piezas.png'),
            {'Lepido_Trunk + Veins_Rest': [on(rest[0])], 'Lepido_Mound': [m], 'Lepido_BackRoots': [back_roots()],
             'Lepido_Veins_Hit': [on(v) for v in hit]}, BG, 1)
    print('   Tree/ y gif/ escritos')

def escena_aviso():
    """Escena: en reposo, las vetas avisan (el brillo sube a la copa), la copa se sacude (provisional) y le pegan."""
    import generar_escala_lepidodendron as E
    W, H = 50, 30
    fx, fy = 25, 15.5
    room = A.room(A.rect_room(W, H, (2, int(fy - 10.5), W - 3, H - 2)))
    def put(im, c, px, py):
        im.alpha_composite(c.img().convert('RGBA') if hasattr(c, 'img') else c, (int(fx * U - px), int(fy * U - py)))
    put(room, back_roots(), BFX, BFY)
    m, sh = mound()
    put(room, sh, MFX, MFY); put(room, m, MFX, MFY)
    E.dapple(room, (0, 0, W, H), lambda xu: 1.0)
    put(room, trunk(), OX, FOOT)
    cx0, cy0 = fx - 20, fy - 8.75 - 1.5
    band = E.canopy_band(int(40 * U) + 64, int(3.6 * U), holes=((9 * U + 32, 22), (30 * U + 32, 18)))
    frog = A.frame(FROG, 0, 32)
    seq = [('rest', 0)] * 5 + [('warn', k) for k in range(WARN_N)] + [('rest', 0)] * 4 + [('hit', 0), ('hit', 1)] + [('rest', 0)] * 4
    vcache = {}
    frames = []
    for i, (mode, k) in enumerate(seq):
        im = room.copy()
        key = (mode, k)
        if key not in vcache: vcache[key] = veins(mode, k, WARN_N).img().convert('RGBA')
        put(im, vcache[key], OX, FOOT)
        im.alpha_composite(frog, (int((fx - 7) * U), int((fy + 5) * U)))
        for (g, f) in (((cx0 + 9, cy0 + 1.5), (cx0 + 13, cy0 + 15)), ((cx0 + 30, cy0 + 1.5), (cx0 + 34, cy0 + 17))):
            E.light_shaft(im, g[0], g[1], f[0], f[1])
        sx = (6 if (i % 2) else -6) if 9 <= i < 13 else 0              # la copa se sacude al final del aviso (provisional)
        im.alpha_composite(band, (int(cx0 * U) - 32 + sx, int(cy0 * U) - 14))
        frames.append(im.crop((int(cx0 * U), int(cy0 * U), int((cx0 + 40) * U), int((cy0 + 22.5) * U))))
    os.makedirs(os.path.join(GIF, 'escenas'), exist_ok=True)
    out = [f.convert('RGB') for f in frames]
    ms = [140] * 5 + [WARN_MS] * WARN_N + [110] * 4 + [HIT_MS * 2] * 2 + [160] * 4
    out[0].save(os.path.join(GIF, 'escenas', 'Lepido_aviso.gif'), save_all=True, append_images=out[1:], duration=ms, loop=0)
    keys = [0, 6, 8, 10, 12, 17]
    names = ['reposo', 'aviso 2/8', 'aviso 4/8', 'aviso 6/8', 'aviso 8/8 + sacudida', 'golpe']
    w_, h_ = frames[0].size
    sheet = Image.new('RGBA', (w_ * 3 + 16, (h_ + 26) * 2), (16, 18, 16, 255))
    d = ImageDraw.Draw(sheet)
    for j, (kf, nm) in enumerate(zip(keys, names)):
        x, y = (j % 3) * (w_ + 8), (j // 3) * (h_ + 26)
        sheet.alpha_composite(frames[kf], (x, y + 24))
        d.text((x + 6, y + 4), nm, fill=(120, 230, 230), font=A.font(16))
    _save(sheet, 'Lepidodendron ancestral · 6l-1 escena: el aviso de las vetas antes de bajar un cono, y un golpe',
          ['Cámara ×0,75 a ×1. La sacudida de la copa es provisional (la capa definitiva llega en la 6l-2). GIF: gif/escenas/Lepido_aviso.gif'],
          'vista_previa_6l1_escena_aviso.png')

# =================================================================================================
# 6l-2 · Capa de hojas (primer plano, arriba de la pantalla), rayos de luz, sombra moteada, hojas que caen
# =================================================================================================
FG = [C(c) for c in ('#0a120b', '#111c11', '#1a2a18', '#283c22', '#3a5230')]   # hojas en primer plano (más oscuras)
FG_HI = C('#55703c')
SKY = [C(c) for c in ('#5a6a3c', '#8a9a5a', '#c8cc8a', '#f0ecc0')]   # luz que se ve por los huecos
LEAF_SUN = [C(c) for c in ('#6a7a40', '#9aa860', '#c4cc84')]           # hojas que caen, al sol
BARK_FG = [C(c) for c in ('#0c100e', '#161c18', '#222a24', '#323c32', '#465244', '#5c6a56')]
LIGHT = (255, 240, 170)
SPORE_C = C('#c8bf98')
CANOPY = os.path.join(HERE, 'Canopy')
SWAY_N, SWAY_MS = 8, 150
SHAKE_N, SHAKE_MS = 6, 60
ARENA_W = 40 * U + 64                                   # la arena (40 u) + 32 px de margen a cada lado
ARENA_H = 112
HOLES = ((9 * U + 32, 26), (30 * U + 32, 22))           # huecos de luz (x en la pieza, radio)
CANOPY_Y = -10                                          # la pieza va 10 px por encima del borde de la pantalla

def _limb_fg(cv, x, y, ang, L, w0, w1, bend):
    """Rama de la copa a contraluz (paleta oscura)."""
    pts = []
    for i in range(int(L) + 1):
        t = i / L; a = ang + bend * (t - .5)
        pts.append((x, y) if i == 0 else (pts[-1][0] + math.cos(a), pts[-1][1] + math.sin(a)))
    for i, (px, py) in enumerate(pts):
        r = (w0 + (w1 - w0) * i / L) / 2
        for yy in range(int(py - r - 1), int(py + r + 2)):
            for xx in range(int(px - r - 1), int(px + r + 2)):
                ox_, oy_ = xx + .5 - px, yy + .5 - py
                if math.hypot(ox_, oy_) > r + .25: continue
                s = (ox_ * -.6 + oy_ * -.8) / max(r, .5)
                cv.put(xx, yy, BARK_FG[3 if s > .55 else 2 if s > 0 else 1 if s > -.5 else 0])   # a contraluz

def canopy(W, H, seed=12, holes=(), sway=0.0, shake=0.0, wrap=False, depth=1.0):
    """Capa de hojas vista desde abajo: masa oscura arriba, ramas cruzando, penachos, y por el borde de abajo frondas
    y musgo que cuelgan. sway (radianes): cuánto se inclina lo que cuelga (vaivén); shake 0..1: sacudida violenta
    (toda la masa tiembla y lo que cuelga se agita mucho, cada pieza a su ritmo). depth: lo honda que baja la masa
    (las tiras del pasillo son más finas). wrap: encaja consigo misma en horizontal (tira en mosaico)."""
    rnd = random.Random(seed)                           # geometría (igual en todos los cuadros)
    jit = random.Random(seed * 31 + int(shake * 1000) + int(sway * 1000))    # temblor del cuadro
    cv = WrapCanvas(W, H, wrapx=wrap) if wrap else Canvas(W, H)
    dx = round(3 * shake * math.sin(sway * 37)) if shake else 0
    dy = round(2 * shake * math.cos(sway * 23)) if shake else 0
    base = H * (.4 if wrap else .58) * depth
    def edge(x):
        f = (2 * math.pi / W) if wrap else (1 / 47)
        return base + 9 * depth * math.sin(x * f * (3 if wrap else 1) + seed) + 6 * depth * math.sin(x * f * (7 if wrap else 2.5) + 1)
    for x in range(W):
        e = edge(x)
        for y in range(int(e)):
            cv.put(x + dx, y + dy, FG[0] if y < e - 10 else FG[1])
    for k in range(int(W / 230) + 1):                   # ramas gruesas que cruzan la copa
        y0 = rnd.uniform(.12, .32) * H * depth
        x0 = rnd.uniform(-40, W * .5)
        _limb_fg(cv, x0 + dx, y0 + dy, rnd.uniform(-.12, .12), rnd.uniform(.4, .7) * W, 20 * depth + 6, 9, rnd.uniform(-.25, .25))
    for k in range(int(W / 9 * depth)):                 # penachos sobre la masa (textura)
        x, y = rnd.uniform(0, W), rnd.uniform(0, base * .95)
        n = rnd.randint(7, 11)
        for j in range(n):
            a = -math.pi / 2 + rnd.uniform(-2.6, 2.6) + jit.uniform(-.25, .25) * shake
            frondlet(cv, x + dx, y + dy, a, rnd.uniform(8, 15), FG[1:], .5, 1.4, None, 0 if a < -1 else 1)
        if rnd.random() < .3: cv.put(x + dx + rnd.randint(-5, 5), y + dy - rnd.randint(3, 8), FG_HI)
    for x in range(0, W, 7):                            # lo que cuelga del borde: frondas y musgo
        ex = edge(x) + rnd.uniform(-6, 2)
        ph = rnd.uniform(0, 6.28)
        sw = sway * math.sin(ph) * .6 + sway * .6 if not shake else (jit.uniform(-1, 1) * .55 * shake + sway * .3)
        for j in range(rnd.randint(3, 6)):
            a = math.pi / 2 + rnd.uniform(-1.2, 1.2) * .8 + sw
            frondlet(cv, x + dx + rnd.uniform(-3, 3), ex + dy, a, rnd.uniform(9, 20) * (.7 + .3 * depth), FG[1:], .4, 1.7, None, rnd.choice((0, 1, 1)))
        if rnd.random() < .35:                          # musgo colgante (se balancea más abajo)
            L = rnd.randint(12, 34)
            for j in range(L):
                off = (sw * 9 if not shake else sw * 14) * (j / L) ** 1.5
                c = FG[3] if j < 2 else FG[2] if j < L * .5 else FG[1]
                cv.put(x + dx + off + math.sin(j / 4 + ph) * .7, ex + dy + 3 + j, c)
    for (hx, hr) in holes:                              # huecos de luz, con un ribete de hojas iluminadas
        cy = base * .62
        for (x, y) in list(cv.px):
            ang = math.atan2(y - cy, x - hx)
            d = ((x - hx) / hr) ** 2 + ((y - cy) / (hr * .6)) ** 2
            d /= (1 + .18 * math.sin(ang * 5 + hx) + .1 * math.sin(ang * 11)) ** 2     # borde irregular
            if d < 1:                                   # por el hueco se ve el cielo encendido, no el suelo
                c = SKY[3] if d < .2 else SKY[2] if d < .5 else SKY[1] if d < .8 else SKY[0]
                if d >= .8 and not dither(.6, x, y): c = SKY[1]
                cv.px[(x, y)] = c
            elif d < 1.5 and cv.px[(x, y)] in (FG[0], FG[1], FG[2]) and (x + y) % 3: cv.px[(x, y)] = FG[3] if y > cy else FG[4]
        hr_ = random.Random(int(hx))
        for j in range(5):                              # frondas a contraluz que cruzan el hueco
            a0 = hr_.uniform(0, 6.28)
            sx_, sy_ = hx + math.cos(a0) * hr * 1.05, cy + math.sin(a0) * hr * .6
            frondlet(cv, sx_ + dx, sy_ + dy, a0 + math.pi + hr_.uniform(-.5, .5) + sway * .5, hr * hr_.uniform(.5, .9), [FG[0], FG[0], FG[1], FG[1]], .3, 1.5, None, 0)
    cv.outline(); cv.clean()
    return cv

def dapple_tile(k=0, n=8, S=256):
    """Sombra moteada en mosaico (S×S, encaja por los 4 lados): oscuro translúcido con manchas de luz; k/n = fase del
    viento (las manchas derivan despacio y respiran)."""
    cv = Canvas(S, S)
    t = 2 * math.pi * k / n
    w = 2 * math.pi / S
    for y in range(S):
        for x in range(S):
            v = (.5 + .17 * math.sin(3 * w * x + math.sin(2 * w * y + t) * 1.6 + t)
                 + .17 * math.sin(4 * w * y + math.cos(1 * w * x - t) * 1.8)
                 + .16 * math.sin(5 * w * (x + y) + math.sin(w * x * 2) + t * 1))
            if v > .76: continue                        # mancha de luz
            a = int(60 + 60 * (1 - v))
            cv.put(x, y, (10, 14, 12, a))
    return cv

def light_shaft(k=0, n=8, L=15 * U, dxu=4.2 * U, w0=1.4 * U, w1=2.2 * U, seed=3):
    """Rayo de luz translúcido desde el hueco (arriba a la izquierda de la celda) hasta el suelo, con motas de esporas
    que flotan en él. Celda (dxu + w1 + 16) × (L + 8); el hueco está en (w0/2 + 8, 0)."""
    Wc, Hc = int(dxu + w1 + 16), int(L + 8)
    cv = Canvas(Wc, Hc)
    x0 = w0 / 2 + 8
    flick = 1 + .08 * math.sin(2 * math.pi * k / n)
    for y in range(int(L)):
        t = y / L
        cx = x0 + dxu * t
        hw_ = (w0 + (w1 - w0) * t) / 2
        for x in range(int(cx - hw_ - 1), int(cx + hw_ + 2)):
            e = abs(x - cx) / hw_
            if e > 1.05: continue
            a = 30 if e < .7 else 18
            a = int(a * flick * (1 - .35 * t))
            if e > .85 and not dither(.5, x, y): continue          # borde en trama
            cv.put(x, y, LIGHT + (a,))
    rnd = random.Random(seed)
    for j in range(14):                                 # motas que flotan y bajan despacio
        t0 = rnd.random(); s0 = rnd.uniform(-.6, .6)
        t = (t0 + k / n * .12) % 1
        cx = x0 + dxu * t + s0 * (w0 + (w1 - w0) * t) / 2 + math.sin(2 * math.pi * (k / n) + j) * 1.2
        y = t * L
        cv.put(cx, y, LIGHT + (210 if j % 3 else 150,))
        if j % 4 == 0: cv.put(cx + 1, y, LIGHT + (110,))
    return cv

def light_pool(k=0, n=8, rx=1.5 * U, ry=.55 * U):
    cv = Canvas(int(rx * 2 + 4), int(ry * 2 + 4))
    ox, oy = cv.w / 2, cv.h / 2
    flick = 1 + .1 * math.sin(2 * math.pi * k / n)
    for y in range(cv.h):
        for x in range(cv.w):
            d = ((x - ox) / rx) ** 2 + ((y - oy) / ry) ** 2
            if d > 1: continue
            if d > .7 and not dither((1 - d) / .3, x, y): continue
            cv.put(x, y, LIGHT + (int((34 if d < .45 else 22) * flick),))
    return cv

def falling_leaf(k, n=6):
    """Hoja de aguja (como hierba) que cae dando vueltas: 12×12."""
    cv = Canvas(16, 16)
    a = 2 * math.pi * k / n
    L = 5.5 * abs(math.cos(a)) + 1.5                    # se ve de canto / de plano al girar
    for i in range(-int(L), int(L) + 1):
        x, y = 8 + i * math.cos(a * .5 + .6), 8 + i * math.sin(a * .5 + .6)
        cv.put(x, y, LEAF_SUN[1] if i < 0 else LEAF_SUN[2])
        if abs(i) < L - 1: cv.put(x + .7, y + .7, LEAF_SUN[0])
    return cv

def falling_spore(k, n=3):
    cv = Canvas(6, 6)
    cv.put(2, 2, SPORE_C); cv.put(3, 2, SPORE_C if k != 1 else C('#a89e78')); cv.put(2, 3, C('#a89e78'))
    if k == 2: cv.put(3, 3, C('#8a8060'))
    return cv

def piezas_copa():
    os.makedirs(CANOPY, exist_ok=True)
    sway = [canopy(ARENA_W, ARENA_H, holes=HOLES, sway=.16 * math.sin(2 * math.pi * k / SWAY_N)) for k in range(SWAY_N)]
    shake = [canopy(ARENA_W, ARENA_H, holes=HOLES, sway=k * 1.7 + .3, shake=1.0) for k in range(SHAKE_N)]
    save_sheet(os.path.join(CANOPY, 'Canopy_Arena_Sway.png'), sway)
    save_sheet(os.path.join(CANOPY, 'Canopy_Arena_Shake.png'), shake)
    for name, dep, sd in (('Thin', .6, 21), ('Mid', 1.1, 22)):
        fr = [canopy(256, 80, seed=sd, sway=.16 * math.sin(2 * math.pi * k / SWAY_N), wrap=True, depth=dep) for k in range(SWAY_N)]
        save_sheet(os.path.join(CANOPY, f'Canopy_Strip{name}_Sway.png'), fr)
    save_sheet(os.path.join(CANOPY, 'Fx_Dapple.png'), [dapple_tile(k) for k in range(8)])
    save_sheet(os.path.join(CANOPY, 'Fx_LightShaft.png'), [light_shaft(k) for k in range(8)])
    save_sheet(os.path.join(CANOPY, 'Fx_LightPool.png'), [light_pool(k) for k in range(8)])
    save_sheet(os.path.join(CANOPY, 'Fx_FallingLeaf.png'), [falling_leaf(k) for k in range(6)])
    save_sheet(os.path.join(CANOPY, 'Fx_FallingSpore.png'), [falling_spore(k) for k in range(3)])
    gif(os.path.join(GIF, 'Canopy_Arena_Sway.gif'), sway, SWAY_MS, BG, 2)
    gif(os.path.join(GIF, 'Canopy_Arena_Shake.gif'), shake * 3, SHAKE_MS, BG, 2)
    gif(os.path.join(GIF, 'Fx_LightShaft.gif'), [light_shaft(k) for k in range(8)], 140, '#2a2d25', 2)
    print('   Canopy/ escrito')
    return sway, shake

def escena_copa():
    """La arena con la capa de hojas definitiva: vaivén (bucle) y sacudida violenta con hojas y esporas que caen."""
    W, H = 50, 30
    fx, fy = 25, 15.5
    room = A.room(A.rect_room(W, H, (2, int(fy - 10.5), W - 3, H - 2)))
    def put(im, c, px, py):
        im.alpha_composite(c.img().convert('RGBA') if hasattr(c, 'img') else c, (int(fx * U - px), int(fy * U - py)))
    put(room, back_roots(), BFX, BFY)
    m, sh = mound()
    put(room, sh, MFX, MFY); put(room, m, MFX, MFY)
    cx0, cy0 = fx - 20, fy - 8.75 - 1.5
    X0, Y0 = int(cx0 * U), int(cy0 * U)
    trunk_im = trunk().img().convert('RGBA'); vrest = veins('rest').img().convert('RGBA')
    tiles = [dapple_tile(k).img().convert('RGBA') for k in range(8)]
    shafts = [light_shaft(k).img().convert('RGBA') for k in range(8)]
    pools = [light_pool(k).img().convert('RGBA') for k in range(8)]
    sway = [canopy(ARENA_W, ARENA_H, holes=HOLES, sway=.16 * math.sin(2 * math.pi * k / SWAY_N)).img().convert('RGBA') for k in range(SWAY_N)]
    shake = [canopy(ARENA_W, ARENA_H, holes=HOLES, sway=k * 1.7 + .3, shake=1.0).img().convert('RGBA') for k in range(SHAKE_N)]
    leaves = [falling_leaf(k).img().convert('RGBA') for k in range(6)]
    spores = [falling_spore(k).img().convert('RGBA') for k in range(3)]
    frog = A.frame(FROG, 0, 32)
    rnd = random.Random(5)
    drops = [(rnd.uniform(0, 640), rnd.uniform(-120, 30), rnd.uniform(1.6, 3.2), rnd.random() < .3, rnd.randrange(6)) for _ in range(70)]
    frames, ms = [], []
    seq = [('sway', k % SWAY_N, k) for k in range(16)] + [('shake', k % SHAKE_N, 16 + k) for k in range(18)] + [('sway', k % SWAY_N, 34 + k) for k in range(10)]
    for i, (mode, k, t) in enumerate(seq):
        im = room.copy()
        dl = Image.new('RGBA', im.size, (0, 0, 0, 0))
        tile = tiles[(t // 2) % 8]
        for ty in range(0, im.height, 256):
            for tx in range(0, im.width, 256): dl.alpha_composite(tile, (tx, ty))
        im.alpha_composite(dl)
        put(im, trunk_im, OX, FOOT); put(im, vrest, OX, FOOT)
        im.alpha_composite(frog, (int((fx - 7) * U), int((fy + 5) * U)))
        for j, (hx, hr) in enumerate(HOLES):            # rayos desde los huecos (posición en mundo)
            gx = X0 - 32 + hx; gy = Y0 + CANOPY_Y + int(ARENA_H * .58 * .62)
            sh_ = shafts[(t + j * 3) % 8]
            im.alpha_composite(sh_, (int(gx - (1.4 * U / 2 + 8)), gy))
            pl = pools[(t + j * 2) % 8]
            im.alpha_composite(pl, (int(gx + 4.2 * U - pl.width / 2), int(gy + 15 * U - pl.height / 2)))
        if mode == 'shake':                             # hojas y esporas que caen (adorno)
            age = t - 16
            for (x, y0, v, sp, ph) in drops:
                yy = y0 + age * v * 9
                if yy < 0 or yy > 330: continue
                spr = spores[(age + ph) % 3] if sp else leaves[(age + ph) % 6]
                im.alpha_composite(spr, (int(X0 + x + math.sin(age / 2 + ph) * 6), int(Y0 + yy)))
        cimg = sway[k] if mode == 'sway' else shake[k]
        im.alpha_composite(cimg, (X0 - 32, Y0 + CANOPY_Y))
        frames.append(im.crop((X0, Y0, X0 + 640, Y0 + 360)))
        ms.append(SWAY_MS if mode == 'sway' else SHAKE_MS * 1.5)
    os.makedirs(os.path.join(GIF, 'escenas'), exist_ok=True)
    out = [f.convert('RGB') for f in frames]
    out[0].save(os.path.join(GIF, 'escenas', 'Lepido_copa.gif'), save_all=True, append_images=out[1:], duration=[int(v) for v in ms], loop=0)
    keys, names = [0, 4, 18, 22, 28, 40], ['vaivén', 'vaivén (otra fase)', 'sacudida', 'sacudida + hojas', 'sacudida + hojas', 'vuelve la calma']
    w_, h_ = frames[0].size
    sheet = Image.new('RGBA', (w_ * 3 + 16, (h_ + 26) * 2), (16, 18, 16, 255))
    d = ImageDraw.Draw(sheet)
    for j, (kf, nm) in enumerate(zip(keys, names)):
        x, y = (j % 3) * (w_ + 8), (j // 3) * (h_ + 26)
        sheet.alpha_composite(frames[kf], (x, y + 24))
        d.text((x + 6, y + 4), nm, fill=(120, 230, 230), font=A.font(16))
    _save(sheet, 'Lepidodendron ancestral · 6l-2 escena: la capa de hojas (vaivén y sacudida violenta), rayos de luz y sombra moteada',
          ['Cámara ×0,75 a ×1. Las hojas y esporas que caen en la sacudida son solo adorno. GIF: gif/escenas/Lepido_copa.gif'],
          'vista_previa_6l2_escena_copa.png')

def lamina_pasillo():
    """El pasillo desde la pared 2: la tira fina, luego la media (pared 3) y la capa de la arena."""
    import generar_escala_lepidodendron as E
    im = E.build_C()
    strips = {'Thin': canopy(256, 80, seed=21, wrap=True, depth=.6).img().convert('RGBA'),
              'Mid': canopy(256, 80, seed=22, wrap=True, depth=1.1).img().convert('RGBA')}
    top = (E.CORR[1] - 1) * U
    for (x0, x1, nm) in ((64, 94, 'Thin'), (94, 118, 'Mid')):
        for x in range(int(x0 * U), int(x1 * U), 256):
            im.alpha_composite(strips[nm].crop((0, 0, min(256, int(x1 * U) - x), 80)), (x, int(top)))
    im.alpha_composite(canopy(46 * U, ARENA_H, holes=()).img().convert('RGBA'), (int(114 * U), int((E.ARENA[1] - 1) * U)))
    crop = im.crop((int(52 * U), int((E.TOP - 2) * U), int(160 * U), im.height))
    d = ImageDraw.Draw(crop)
    for (x, t) in ((64, 'pared 2: empieza la tira fina'), (94, 'pared 3: tira media'), (118, 'arena: capa entera')):
        d.text(((x - 52) * U + 6, 6), t, fill=(120, 230, 230), font=A.font(15), stroke_width=3, stroke_fill=(10, 10, 14))
    _save(crop, 'Lepidodendron ancestral · 6l-2 el pasillo: la copa se espesa hacia el árbol (tiras en mosaico de 256 px)',
          ['Las tiras van fijas arriba de la pantalla y se desplazan con el mundo en horizontal (encajan consigo mismas).'],
          'vista_previa_6l2_pasillo.png')

if __name__ == '__main__':
    which = sys.argv[1:] or ['muestra']
    if 'muestra' in which:
        lamina_muestra(); lamina_vetas()
    if 'piezas' in which: piezas()
    if 'escena' in which: escena_aviso()
    if 'copa' in which: piezas_copa(); escena_copa(); lamina_pasillo()
