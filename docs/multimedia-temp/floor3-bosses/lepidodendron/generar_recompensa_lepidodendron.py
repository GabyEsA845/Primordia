"""Lepidodendron ancestral · subtanda 6o-2: la recompensa.

- Espora morada (objeto clave, llave de la sala secreta del piso 4): esporangio en gota, morado intenso con vetas que
  laten y un aro dorado en la base. Baja flotando por la columna de luz junto con el corazón (Item_HeartContainer).
- La sala de dentro del tocón (se entra pulsando E en la grieta): pequeña y redonda, más grande por dentro (mágica):
  suelo de madera con los anillos del árbol, pared de fibras con raíces que bajan, setas moradas que aún laten, rayos
  finos por las rendijas del techo y la grieta de salida en la pared sur. Luz: la reliquia y el pedestal.
- Pedestal: raíces trenzadas que suben del suelo y arriba se vuelven ámbar y oro formando un cuenco.
- Reliquia Raíz ancestral: raíz retorcida en espiral, corteza oscura con vetas moradas que laten, punta que brilla,
  sujeta por un anillo dorado; flota y gira despacio sobre el cuenco.
- Presentación de reliquias (distinta de la de los jefes): «revelación con halo» con Jersey 10, sin banda diagonal.

Uso: python generar_recompensa_lepidodendron.py [muestra]
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import generar_lepidodendron as G
from generar_lepidodendron import *                    # noqa: F401,F403
from generar_lepidodendron import _h
import generar_desplome_lepidodendron as D
from PIL import Image, ImageDraw, ImageFont

INTROS = os.path.join(G.MT, 'boss-intros')
sys.path.insert(0, INTROS)
import generar_presentaciones as GP

GOLD = D.GOLD
WOOD_IN = D.WOOD_IN
SPORE_P = [C(c) for c in ('#1e0a36', '#3a1266', '#5e1fa0', '#8a3ad0', '#b878f0', '#e4c4ff')]   # morado intenso
AMBER = [C(c) for c in ('#3a200a', '#6a3e12', '#a8681e', '#e0a238', '#ffd468', '#fff2c0')]
ROOTP = [C(c) for c in ('#141714', '#252b24', '#38402f', '#525a40', '#767c58')]
GOLD_RAMP = ['#fff4c8', '#ffd468', '#e0a234', '#a86a1c']                            # relleno del nombre (Jersey 10)
CREAM = (246, 232, 200)

# =================================================================================================
# Espora morada
# =================================================================================================
def purple_spore(k=0, n=6, S=1.0, sq=1.0):
    """Esporangio (16×22 a S=1): gota con la punta arriba, vetas que laten (fase k de n), aro dorado en la base."""
    W, H = int(16 * S + .5), int(22 * S + .5)
    cv = Canvas(W, H)
    cx, cyb = W / 2 - .5, 13 * S
    top, bot, rb = 2 * S, 18.5 * S, 5.6 * S
    pulse = .5 + .5 * math.sin(2 * math.pi * k / n)
    for y in range(int(top), int(bot) + 1):
        if y < cyb: w = rb * (max(0, y - top) / (cyb - top)) ** .75
        else: w = rb * math.sqrt(max(0, 1 - ((y - cyb) / (bot - cyb + .5)) ** 2))
        w *= sq                                          # gira: se estrecha
        for x in range(int(cx - w - 1), int(cx + w + 2)):
            if abs(x - cx) > w + .3: continue
            nx = (x - cx) / max(w, .5); ny = (y - cyb) / (rb * 1.4)
            lam = -nx * .65 - ny * .55 + .15
            tone = 4 if lam > .55 else 3 if lam > .1 else 2 if lam > -.35 else 1
            # vetas: meridianos que laten
            for ph in (-.55, 0, .55):
                vx = cx + w * math.sin(ph + .25 * math.sin(y / (3 * S)))
                if abs(x - vx) < .6 and y > top + 2 * S:
                    tone = 5 if pulse > .55 else 4
            cv.put(x, y, SPORE_P[min(5, tone)])
    cv.put(cx - rb * .45, cyb - rb * .4, SPORE_P[5])  # brillo
    for x in range(int(cx - rb - 1), int(cx + rb + 2)):  # aro dorado en la base
        for dy, c in ((0, GOLD[3]), (1, GOLD[2]), (2, GOLD[1])):
            yy = int(bot - 1 * S) + dy
            if abs(x - cx) <= (rb * .85 + (1 if dy == 1 else 0)) * sq + .3: cv.put(x, yy, c if x > cx - rb * .5 else (GOLD[4] if dy == 0 else c))
    cv.outline(); cv.clean()
    halo = Canvas(W, H)                                  # halo morado (translúcido, sin contorno)
    for y in range(H):
        for x in range(W):
            d = math.hypot((x - cx) / (rb + 2.5 * S), (y - cyb + 1) / (rb * 1.6 + 2 * S))
            if d < 1 and (x, y) not in cv.px: halo.put(x, y, SPORE_P[4][:3] + (int((50 + 40 * pulse) * (1 - d)),))
    out = halo.img().convert('RGBA'); out.alpha_composite(cv.img().convert('RGBA'))
    return out

# =================================================================================================
# Reliquia: Raíz ancestral
# =================================================================================================
def tube(cv, zb, pts, pal, vein=None, glow=1.0):
    """Tubo sombreado (luz arriba a la izquierda) por una lista de puntos (x, y, z, r, t); z > 0 = delante.
    vein: color de la veta que corre por la cara de delante (a trozos)."""
    for (x, y, z, r, t) in pts:
        for yy in range(int(y - r - 1), int(y + r + 2)):
            for xx in range(int(x - r - 1), int(x + r + 2)):
                dx, dy = xx + .5 - x, yy + .5 - y
                if dx * dx + dy * dy > r * r: continue
                depth = z * 10 - (dx * dx + dy * dy) / max(r * r, .5) * .2
                if zb.get((xx, yy), -99) > depth: continue
                lam = (-dx * .6 - dy * .8) / max(r, .5) + z * .3
                tone = 4 if lam > .55 else 3 if lam > .05 else 2 if lam > -.5 else 1
                if z < -.3: tone = max(0, tone - 1)
                c = pal[tone]
                if vein is not None and z > .1 and abs(dx) < .7 and abs(dy + r * .15) < r * .6 and (int(t * 70) % 5) < 3:
                    c = vein
                zb[(xx, yy)] = depth; cv.put(xx, yy, c)

def relic(k=0, n=8, S=1.0, glow=1.0):
    """Raíz ancestral (24×30 a S=1) que gira (fase k de n): raíz retorcida en sacacorchos, corteza oscura con una veta
    morada que corre por delante, la punta que brilla, raicillas abajo y un anillo dorado que la sujeta."""
    W, H = int(24 * S), int(30 * S)
    cx, by = W / 2 - .5, H - 6 * S
    Rh, Hh, turns = 3.1 * S, 19 * S, 2.4
    phase = 2 * math.pi * k / n
    cv = Canvas(W, H); zb = {}
    pts = []
    for i in range(200):
        t = i / 199
        th = phase + t * turns * 2 * math.pi
        rr = Rh * (1 - .45 * t)
        pts.append((cx + rr * math.cos(th), by - t * Hh + rr * .3 * math.sin(th), math.sin(th), (2.5 - 1.3 * t) * S, t))
    vein = G.VEIN[min(4, 2 + int(glow * 2.2))]
    tube(cv, zb, pts, BARK_OLD, vein, glow)
    rnd = random.Random(3)                               # raicillas que salen por abajo
    for q in range(3):
        a = -math.pi / 2 + (q - 1) * .7 + phase * .0
        rp = []
        for i in range(14):
            t = i / 13
            rp.append((cx + (q - 1) * 2.2 * S + math.cos(a + math.pi) * 0 + (q - 1) * t * 3 * S, by + 1 * S + t * 4 * S + math.sin(t * 3 + q) * .6, -.2, (1.2 - .6 * t) * S, t))
        tube(cv, zb, rp, BARK_OLD)
    tx, ty = pts[-1][0], pts[-1][1]                      # la punta que brilla
    for (dx, dy, c) in ((0, 0, (255, 255, 255)), (1, 0, GOLD[4]), (-1, 0, GOLD[4]), (0, -1, GOLD[4]), (0, 1, GOLD[3]), (0, -2, GOLD[3])):
        cv.put(tx + dx, ty + dy, c)
    gx, gy = cx, by + .5 * S                             # anillo dorado que la sujeta
    for a in range(60):
        q = a / 60 * 2 * math.pi
        for w_ in (0, 1):
            x = gx + math.cos(q) * (Rh + 1.6 * S); y = gy + math.sin(q) * (Rh + 1.6 * S) * .32 + w_
            if math.sin(q) < 0 and (int(x), int(y)) in zb: continue                # la parte de atrás, detrás de la raíz
            cv.put(x, y, GOLD[4] if (math.cos(q) < -.2 and w_ == 0) else GOLD[3] if w_ == 0 else GOLD[1])
    cv.outline(); cv.clean()
    halo = Canvas(W, H)
    for y in range(H):
        for x in range(W):
            d = math.hypot((x - tx) / (6 * S), (y - ty) / (6 * S))
            if d < 1 and (x, y) not in cv.px: halo.put(x, y, GOLD[3][:3] + (int(120 * glow * (1 - d)),))
    out = halo.img().convert('RGBA'); out.alpha_composite(cv.img().convert('RGBA'))
    return out

# =================================================================================================
# Pedestal
# =================================================================================================
def pedestal(lit=1.0):
    """Raíces que se enroscan en una columna, abajo se abren en pies por el suelo y arriba en dedos que sujetan un
    cuenco en forma de copa; por encima de la mitad se vuelven ámbar y oro (56×52, pivote = abajo en el centro (28, 48)).
    lit 0..1: brillo del cuenco (1 con la reliquia, se apaga al cogerla)."""
    W, H = 56, 52
    cx, base, bowl = 28, 48, 15
    ys = 31                                              # por encima, ámbar y oro
    cv = Canvas(W, H); zb = {}
    def pal_at(y): return AMBER[1:] if y < ys else ROOTP
    rnd = random.Random(7)
    for (dx, L) in ((-20, 1), (-11, .8), (12, .85), (21, 1), (-3, .6), (5, .55)):   # pies que se abren por el suelo
        root(cv, [(cx + dx * .2, base - 6), (cx + dx * .55, base - 3), (cx + dx * .9, base + 1), (cx + dx * 1.05 * L + (2 if dx > 0 else -2), base + 2)], 6, 2, ROOTP)
    for q in range(3):                                   # la columna: 3 raíces enroscadas
        pts = []
        for i in range(90):
            t = i / 89
            th = q * 2 * math.pi / 3 + t * 1.6 * math.pi
            rr = 4.2 + 2.5 * (1 - t) ** 3 + 2.8 * t ** 4
            pts.append((cx + rr * math.cos(th), base - 3 - t * (base - 3 - bowl - 7) + rr * .3 * math.sin(th), math.sin(th), 3.0 - .6 * t, t))
        lo = [p for p in pts if p[1] >= ys]; hi = [p for p in pts if p[1] < ys]
        tube(cv, zb, lo, ROOTP); tube(cv, zb, hi, AMBER[1:])
    for s_ in (-1, 1):                                   # dedos que sujetan el cuenco
        root(cv, [(cx + s_ * 5, bowl + 9), (cx + s_ * 12, bowl + 7), (cx + s_ * 15, bowl + 3), (cx + s_ * 13, bowl - 1)], 3.5, 1.5, AMBER[1:])
    rx, ry = 13, 4.2                                     # la copa: borde, cara de fuera curvada y el hueco de dentro
    for y in range(int(bowl - ry - 1), int(bowl + 9)):
        for x in range(int(cx - rx - 1), int(cx + rx + 2)):
            u = (x - cx) / rx; v = (y - bowl) / ry
            d = u * u + v * v
            if d <= 1:
                if d > .6: c = GOLD[4] if (u < -.2 and v < .2) else GOLD[3] if v < .4 else GOLD[2]       # el borde
                else:                                    # dentro: la pared de atrás con luz, el fondo en sombra
                    c = AMBER[4] if (v < -.1 and lit > .5) else AMBER[3] if v < .35 else AMBER[2]
                    if lit < .3: c = AMBER[2] if v < 0 else AMBER[1]
                cv.put(x, y, c)
            else:
                yb = bowl + 8 * math.sqrt(max(0, 1 - u * u))                   # cara de fuera de la copa
                if y > bowl and y <= yb and abs(u) <= 1:
                    lam = -u * .7 - (y - bowl) / 8 * .4
                    cv.put(x, y, GOLD[3] if lam > .25 else GOLD[2] if lam > -.25 else GOLD[1] if lam > -.6 else GOLD[0])
    cv.outline(); cv.clean()
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    if lit > 0:                                          # resplandor del cuenco
        gl = Image.new('RGBA', (W, H), (0, 0, 0, 0)); gd = ImageDraw.Draw(gl)
        gd.ellipse([cx - 22, bowl - 13, cx + 22, bowl + 9], fill=GOLD[3][:3] + (int(60 * lit),))
        out.alpha_composite(gl)
    out.alpha_composite(cv.img().convert('RGBA'))
    return out

# =================================================================================================
# La sala de dentro del tocón (480×270, una pantalla)
# =================================================================================================
RW_, RH_ = 480, 270
RCX, RCY, RR = 240, 158, 92                             # suelo redondo (picada: radio en x; en y igual)
WALL_H = 46                                              # alto de la pared del fondo (cara que se ve)

def room_interior(k=0, frog=True, base=False):
    im = Image.new('RGBA', (RW_, RH_), (6, 5, 4, 255))
    px = im.load()
    rnd = random.Random(3)
    def floor_r(x, y): return math.hypot(x - RCX, (y - RCY) * 1.0)
    # suelo: los anillos del árbol, con la luz dorada del pedestal en el centro y penumbra cálida
    for y in range(RCY - RR - 2, RCY + RR + 2):
        for x in range(RCX - RR - 2, RCX + RR + 2):
            r = floor_r(x, y)
            if r > RR: continue
            a = math.atan2(y - RCY, x - RCX)
            rr = r / 7.5 + .8 * math.sin(a * 3 + r / 30) + .4 * math.sin(a * 7)
            ring = rr % 1
            light = max(0., 1 - r / (RR * .85)) ** 1.4
            base = 1 + light * 2.6
            if ring < .16: base -= .9
            if (int(x * .7 + y * 1.3) % 6 == 0) and _h(x // 3, y // 3, 4) < .4: base -= .5                # fibras
            if r > RR - 7: base -= 1.2 * (r - (RR - 7)) / 7                                         # junto a la pared, sombra
            c = WOOD_IN[max(0, min(4, int(round(base))))]
            if light > .6 and ring < .16: c = AMBER[3]
            px[x, y] = c
    for j in range(5):                                   # grietas del suelo que salen del centro
        a = rnd.uniform(0, 2 * math.pi); r = 26
        while r < RR - 6:
            x, y = RCX + math.cos(a) * r, RCY + math.sin(a) * r
            px[int(x), int(y)] = (12, 9, 7, 255)
            r += 1; a += rnd.uniform(-.05, .05)
    # pared: cara de fibras de madera que se ve al fondo (norte) y el canto bajo del sur
    for x in range(RCX - RR - 6, RCX + RR + 7):
        u = (x - RCX) / (RR + 6)
        if abs(u) > 1: continue
        yf = RCY - (RR + 2) * math.sqrt(1 - u * u)       # borde del suelo, arriba
        hh = max(1, WALL_H * math.sqrt(1 - u * u) ** .4)
        for y in range(int(yf - hh), int(yf) + 1):
            t = (yf - y) / hh                            # 0 abajo .. 1 arriba
            tone = 1 + (1 if (x + int(t * 9)) % 4 < 2 else 0) - (1 if t > .8 else 0)
            if abs(u) > .75: tone -= 1
            c = WOOD_IN[max(0, tone)]
            if (x * 7 + int(y / 3)) % 11 == 0: c = WOOD_IN[0]
            px[x, int(y)] = c
        ys = RCY + (RR + 2) * math.sqrt(1 - u * u)       # canto de la pared sur (grosor que se ve desde arriba)
        for y in range(int(ys), int(ys) + 7):
            if 0 <= y < RH_: px[x, y] = WOOD_IN[2] if y < ys + 2 else WOOD_IN[1]
    lay = Canvas(RW_, RH_)
    for j in range(16):                                  # raíces que bajan por la pared
        u = rnd.uniform(-.9, .9); x0 = RCX + u * (RR + 4)
        yt = RCY - (RR + 2) * math.sqrt(1 - u * u) - WALL_H * .95
        L = rnd.uniform(30, 52)
        root(lay, [(x0, yt), (x0 + rnd.uniform(-8, 8), yt + L * .3), (x0 + rnd.uniform(-10, 10), yt + L * .7), (x0 + rnd.uniform(-6, 6), yt + L)], rnd.uniform(3, 6), 1.5, ROOTP)
    lay.outline(); lay.clean()
    im.alpha_composite(lay.img().convert('RGBA'))
    if base: return im                                   # el fondo fijo (suelo, pared y raíces)
    import generar_pie_lepidodendron as PIE
    for j, (u, kind) in enumerate(((-.7, 'A'), (-.35, 'B'), (.3, 'C'), (.62, 'A'), (.85, 'B'), (-.88, 'C'))):   # setas moradas que aún laten
        x = RCX + u * RR * .96; y = RCY - (RR - 4) * math.sqrt(1 - u * u)
        im.alpha_composite(PIE.shroom_cluster(kind, .35 + .2 * math.sin(k + j)), (int(x - 12), int(y - 16)))
    # rayos finos por las rendijas del techo
    sh = Image.new('RGBA', im.size, (0, 0, 0, 0)); sd = ImageDraw.Draw(sh)
    for (x, w) in ((RCX - 62, 5), (RCX + 48, 4), (RCX - 10, 3)):
        sd.polygon([(x - w / 2, 0), (x + w / 2, 0), (x + w * 1.6 + 30, RCY + 30), (x - w * .4 + 30, RCY + 30)], fill=(255, 226, 150, 26))
        sd.ellipse([x + 30 - w * 2, RCY + 24, x + 30 + w * 3, RCY + 36], fill=(255, 226, 150, 30))
    im.alpha_composite(sh)
    # la grieta de salida en la pared sur: hilo de luz
    gx, gy = RCX, RCY + RR + 2
    for dy in range(-2, 9):
        for dx in range(-2, 3):
            c = GOLD[4] if abs(dx) == 0 else GOLD[3] if abs(dx) == 1 else WOOD_IN[3] if dx < 0 else (12, 9, 7, 255)
            px[gx + dx + (1 if dy > 3 else 0), gy + dy] = c
    gl = Image.new('RGBA', im.size, (0, 0, 0, 0)); gd = ImageDraw.Draw(gl)
    gd.ellipse([gx - 14, gy - 10, gx + 14, gy + 2], fill=GOLD[3][:3] + (50,))
    im.alpha_composite(gl)
    # luz del pedestal en el centro
    gl = Image.new('RGBA', im.size, (0, 0, 0, 0)); gd = ImageDraw.Draw(gl)
    gd.ellipse([RCX - 70, RCY - 40, RCX + 70, RCY + 44], fill=GOLD[3][:3] + (22,))
    gd.ellipse([RCX - 40, RCY - 22, RCX + 40, RCY + 26], fill=GOLD[4][:3] + (26,))
    im.alpha_composite(gl)
    ped = pedestal(1.0)
    im.alpha_composite(ped, (RCX - 28, RCY + 6 - 48))
    rl = relic(k)
    im.alpha_composite(rl, (RCX - rl.width // 2, int(RCY + 6 - 48 + 15 - rl.height + 1 + 2 * math.sin(k * .8))))
    if frog: im.alpha_composite(A.frame(FROG, 0, 32), (RCX - 16, RCY + RR - 40))
    return im

# =================================================================================================
# Presentación con halo (reliquias y objetos clave)
# =================================================================================================
def halo_rays(k=0, R=58, n=14):
    """Halo de rayos dorados que giran despacio (translúcido)."""
    S = R * 2 + 4
    im = Image.new('RGBA', (S, S), (0, 0, 0, 0)); px = im.load()
    c = S / 2
    for y in range(S):
        for x in range(S):
            d = math.hypot(x - c, y - c) / R
            if d > 1: continue
            a = math.atan2(y - c, x - c) + k * .06
            ray = (math.cos(a * n) + 1) / 2
            v = (ray ** 3) * (1 - d) * 1.2 + (1 - d) ** 2 * .5
            if v < .08: continue
            if v < .3 and not dither(v / .3, x, y): continue
            px[x, y] = GOLD[4 if v > .7 else 3 if v > .4 else 2][:3] + (int(min(230, 60 + 200 * v)),)
    return im

def name_img(text, size=32):
    return GP.title_text(text, GP.TITLE_FONT, size, GOLD_RAMP, out=(30, 14, 4))

def fillet(w, ornament=True):
    """Filete fino dorado con un adorno de raíz en el centro."""
    im = Image.new('RGBA', (w, 9), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    d.line([0, 4, w, 4], fill=GOLD[2]); d.line([0, 5, w, 5], fill=GOLD[0])
    if ornament:
        c = w // 2
        cv = Canvas(w, 9)
        for s_ in (-1, 1):
            for q in range(12):                          # zarcillos en espiral a cada lado
                a = q / 12 * 1.6 * math.pi
                cv.put(c + s_ * (3 + q * .9), 4 - math.sin(a) * 2.6, GOLD[3])
        for (dx, dy) in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)): cv.put(c + dx, 4 + dy, GOLD[4])
        cv.outline((30, 14, 4)); im.alpha_composite(cv.img().convert('RGBA'))
    return im

def presentation(bg, label, name, effect, item_big, k=0):
    """Pantalla (480×270): la escena atenuada, el objeto en el centro de un halo que gira, la etiqueta pequeña arriba,
    el nombre grande dorado entre dos filetes y el efecto en una línea crema."""
    im = bg.copy()
    dim = Image.new('RGBA', im.size, (6, 4, 10, 175)); im.alpha_composite(dim)
    hr = halo_rays(k)
    hx, hy = RW_ // 2, 98
    im.alpha_composite(hr, (hx - hr.width // 2, hy - hr.height // 2))
    im.alpha_composite(item_big, (hx - item_big.width // 2, hy - item_big.height // 2))
    d = ImageDraw.Draw(im); d.fontmode = '1'
    f8 = GP.font('Silkscreen-Regular.ttf', 8)
    tw = d.textlength(label, font=f8)
    d.text((hx - tw / 2, 28), label, font=f8, fill=GOLD[3])
    nm = name_img(name)
    ny = 166
    im.alpha_composite(nm, (hx - nm.width // 2, ny))
    fw = max(nm.width + 60, 220)
    fl = fillet(fw)
    im.alpha_composite(fl, (hx - fw // 2, ny - 10))
    im.alpha_composite(fillet(fw, False), (hx - fw // 2, ny + nm.height + 1))
    tw = d.textlength(effect, font=f8)
    d.text((hx - tw / 2, ny + nm.height + 14), effect, font=f8, fill=CREAM)
    return im

def big_relic(k=0): return relic(k, S=2.0)

SHROOMS = ((-.7, 'A'), (-.35, 'B'), (.3, 'C'), (.62, 'A'), (.85, 'B'), (-.88, 'C'))
def shroom_pos(u): return (RCX + u * RR * .96, RCY - (RR - 4) * math.sqrt(1 - u * u))

def room_rays(k, n=8):
    """Rayos finos por las rendijas del techo (capa translúcida 480×270 que respira)."""
    sh = Image.new('RGBA', (RW_, RH_), (0, 0, 0, 0)); sd = ImageDraw.Draw(sh)
    for j, (x, w) in enumerate(((RCX - 62, 5), (RCX + 48, 4), (RCX - 10, 3))):
        a = int(20 + 10 * (.5 + .5 * math.sin(2 * math.pi * k / n + j * 2)))
        sd.polygon([(x - w / 2, 0), (x + w / 2, 0), (x + w * 1.6 + 30, RCY + 30), (x - w * .4 + 30, RCY + 30)], fill=(255, 226, 150, a))
        sd.ellipse([x + 30 - w * 2, RCY + 24, x + 30 + w * 3, RCY + 36], fill=(255, 226, 150, a + 4))
    px = sh.load(); rnd = random.Random(k)
    for j in range(10):                                  # motas en los rayos
        x, w = ((RCX - 62, 5), (RCX + 48, 4), (RCX - 10, 3))[j % 3]
        t = ((j * .37 + k / n) % 1)
        xx, yy = int(x + 30 * t + rnd.uniform(-2, 2)), int(t * (RCY + 26))
        if 0 <= xx < RW_ and 0 <= yy < RH_: px[xx, yy] = (255, 244, 205, 210)
    return sh

def room_light(lit=1.0):
    """Luz del pedestal sobre el suelo (capa translúcida 480×270)."""
    gl = Image.new('RGBA', (RW_, RH_), (0, 0, 0, 0)); gd = ImageDraw.Draw(gl)
    gd.ellipse([RCX - 70, RCY - 40, RCX + 70, RCY + 44], fill=GOLD[3][:3] + (int(22 * lit),))
    gd.ellipse([RCX - 40, RCY - 22, RCX + 40, RCY + 26], fill=GOLD[4][:3] + (int(26 * lit),))
    return gl

def room_exit(k, n=6):
    """La grieta de salida en la pared sur, vista desde dentro (24×20, centro = la grieta): hilo de luz que late."""
    im = Image.new('RGBA', (24, 20), (0, 0, 0, 0)); px = im.load()
    ph = .5 + .5 * math.sin(2 * math.pi * k / n)
    gl = Image.new('RGBA', (24, 20), (0, 0, 0, 0)); gd = ImageDraw.Draw(gl)
    gd.ellipse([0, 0, 24, 14], fill=GOLD[3][:3] + (int(30 + 40 * ph),)); im.alpha_composite(gl)
    for dy in range(-2, 9):
        for dx in range(-2, 3):
            c = GOLD[4] if dx == 0 else GOLD[3] if abs(dx) == 1 else WOOD_IN[3] if dx < 0 else (12, 9, 7, 255)
            if dx == 0 and ph > .6: c = (255, 250, 225)
            px[12 + dx + (1 if dy > 3 else 0), 8 + dy] = c + (255,) if len(c) == 3 else c
    return im

def present_frame(bg, label, name, effect, item, k, t, start, text_a=1.0):
    """Un cuadro de la presentación (480×270): t 0..1 = entrada (la pantalla se atenúa, el halo crece y el objeto
    sube desde start hasta el centro); text_a 0..1 = el texto aparece."""
    im = bg.copy()
    im.alpha_composite(Image.new('RGBA', im.size, (6, 4, 10, int(175 * t))))
    hx, hy = RW_ // 2, 98
    if t > .2:
        hr = halo_rays(k)
        a_ = min(1, (t - .2) / .6)
        hr.putalpha(hr.split()[3].point(lambda v: int(v * a_)))
        im.alpha_composite(hr, (hx - hr.width // 2, hy - hr.height // 2))
    e = t * t * (3 - 2 * t)
    x = start[0] + (hx - start[0]) * e; y = start[1] + (hy - start[1]) * e
    im.alpha_composite(item, (int(x - item.width / 2), int(y - item.height / 2)))
    if text_a > 0:
        lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay); d.fontmode = '1'
        f8 = GP.font('Silkscreen-Regular.ttf', 8)
        tw = d.textlength(label, font=f8); d.text((hx - tw / 2, 28), label, font=f8, fill=GOLD[3])
        nm = name_img(name); ny = 166
        lay.alpha_composite(nm, (hx - nm.width // 2, ny))
        fw = max(nm.width + 60, 220)
        lay.alpha_composite(fillet(fw), (hx - fw // 2, ny - 10)); lay.alpha_composite(fillet(fw, False), (hx - fw // 2, ny + nm.height + 1))
        tw = d.textlength(effect, font=f8); d.text((hx - tw / 2, ny + nm.height + 14), effect, font=f8, fill=CREAM)
        if text_a < 1: lay.putalpha(lay.split()[3].point(lambda v: int(v * text_a)))
        im.alpha_composite(lay)
    return im
def big_spore(k=0): return purple_spore(k, S=2.4)

# =================================================================================================
def lamina_muestra():
    s = 3
    sp = [purple_spore(k) for k in (0, 2, 4)]
    rl = [relic(k) for k in (0, 2, 4, 6)]
    icons = [purple_spore(1, S=.75), purple_spore(1, S=1.45), relic(1, S=.6), relic(1, S=1.2)]
    peds = [pedestal(1.0), pedestal(0.0)]
    room = room_interior(1)
    pres_r = presentation(room_interior(1, frog=True), 'R E L I Q U I A', 'RAÍZ ANCESTRAL', 'Al llegar a un piso nuevo recuperas toda la vida', big_relic(1), 2)
    pres_s = presentation(room_interior(1, frog=True), 'O B J E T O   C L A V E', 'ESPORA MORADA', 'Abre un camino secreto en el piso 4', big_spore(1), 2)
    # composición
    W = RW_ * 2 * 2 + 30
    top_h = 52 * s + 40
    out = Image.new('RGBA', (W, top_h + RH_ * 2 * 2 + 70), (16, 18, 16, 255))
    d = ImageDraw.Draw(out)
    x = 10
    def put(im, lab, sc, y0=24):
        nonlocal x
        bg = Image.new('RGBA', (im.width * sc, im.height * sc), C('#2a2232'))
        bg.alpha_composite(im.resize((im.width * sc, im.height * sc), Image.NEAREST))
        out.alpha_composite(bg, (x, y0))
        if lab: d.text((x, 4), lab, fill=(120, 230, 230), font=A.font(15))
        x += bg.width + 10
    for i, im in enumerate(sp): put(im, 'Espora morada (late)' if i == 0 else '', s)
    for i, im in enumerate(rl): put(im, 'Reliquia (gira)' if i == 0 else '', s)
    for i, im in enumerate(icons): put(im, 'iconos 12/24 (espora) · 14/28 (reliquia)' if i == 0 else '', s)
    for i, im in enumerate(peds): put(im, 'pedestal · vacío' if i == 0 else '', s)
    y0 = top_h
    for j, (lab, im) in enumerate((('la sala de dentro del tocón (480×270, ×2)', room), ('', None))):
        if im is None: continue
        out.alpha_composite(im.resize((RW_ * 2, RH_ * 2), Image.NEAREST), (10, y0 + 22)); d.text((10, y0 + 2), lab, fill=(120, 230, 230), font=A.font(15))
    out.alpha_composite(pres_r.resize((RW_ * 2, RH_ * 2), Image.NEAREST), (RW_ * 2 + 20, y0 + 22))
    d.text((RW_ * 2 + 20, y0 + 2), 'presentación: reliquia (×2)', fill=(120, 230, 230), font=A.font(15))
    y1 = y0 + RH_ * 2 + 46
    out.alpha_composite(pres_s.resize((RW_ * 2, RH_ * 2), Image.NEAREST), (RW_ * 2 + 20, y1 + 22))
    d.text((RW_ * 2 + 20, y1 + 2), 'presentación: objeto clave (×2)', fill=(120, 230, 230), font=A.font(15))
    G._save(out, 'Lepidodendron ancestral · 6o-2 muestra de la recompensa: Espora morada, reliquia, pedestal, la sala del tocón y la presentación con halo',
            ['Arriba ×3. La sala: suelo con los anillos del árbol, pared de fibras con raíces y setas que laten, rayos por las rendijas, la grieta de salida abajo (hilo de luz).',
             'Presentación (distinta de la de los jefes): escena atenuada, halo de rayos que gira, etiqueta pequeña, nombre en Jersey 10 dorado entre filetes, efecto en crema.'],
            'vista_previa_6o2_muestra.png')

# =================================================================================================
# Hojas
# =================================================================================================
REWARD = os.path.join(HERE, 'Reward')
GIFR = os.path.join(HERE, 'gif')
SPIN = (1.0, .75, .35, .15, .35, .75)                   # la espora gira al bajar (se estrecha y vuelve)
RELIC_NAME, RELIC_EFFECT = 'RAÍZ ANCESTRAL', 'Al llegar a un piso nuevo recuperas toda la vida'
SPORE_NAME, SPORE_EFFECT = 'ESPORA MORADA', 'Abre un camino secreto en el piso 4'

def bob_cell(im, k, n, W, H, amp=1):
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    dy = round(amp * math.sin(2 * math.pi * k / n))
    out.alpha_composite(im, ((W - im.width) // 2, H - im.height - 1 - amp + dy))
    return out

def icon(im, S):
    out = Image.new('RGBA', (S, S), (0, 0, 0, 0)); out.alpha_composite(im, ((S - im.width) // 2, (S - im.height) // 2)); return out

def reward_shadow(k=0, n=6):
    """Sombrita en el suelo bajo lo que flota (16×6), crece y mengua con el vaivén."""
    cv = Canvas(16, 6)
    rx = 5.5 - .8 * math.sin(2 * math.pi * k / n)
    for y in range(6):
        for x in range(16):
            d = math.hypot((x - 7.5) / rx, (y - 2.5) / (rx * .4))
            if d < 1: cv.put(x, y, (8, 6, 12, int(110 * (1 - d * .5))))
    return cv.img().convert('RGBA')

def gold_glow(k=0, n=4):
    """Brillo dorado detrás del corazón (24×24, centro)."""
    im = Image.new('RGBA', (24, 24), (0, 0, 0, 0)); px = im.load()
    ph = .5 + .5 * math.sin(2 * math.pi * k / n)
    for y in range(24):
        for x in range(24):
            d = math.hypot(x - 11.5, y - 11.5) / (9 + 2 * ph)
            if d < 1: px[x, y] = GOLD[3][:3] + (int(90 * (1 - d) ** 1.5),)
    return im

def build_reward():
    out = {}
    out['Item_PurpleSpore_Idle'] = [bob_cell(purple_spore(k), k, 6, 16, 24) for k in range(6)]
    out['Item_PurpleSpore_Fall'] = [bob_cell(purple_spore(k, sq=q), 0, 6, 16, 24, 0) for k, q in enumerate(SPIN)]
    out['Item_PurpleSpore_Big'] = [purple_spore(k, S=2.4) for k in range(6)]
    out['Fx_RewardShadow'] = [reward_shadow(k) for k in range(6)]
    out['Fx_HeartGlow'] = [gold_glow(k) for k in range(4)]
    out['Relic_RootAncestral_Idle'] = [bob_cell(relic(k), k, 8, 24, 32) for k in range(8)]
    out['Relic_RootAncestral_Big'] = [relic(k, S=2.0) for k in range(8)]
    out['Lepido_Pedestal_Lit'] = [pedestal(.85 + .15 * math.sin(2 * math.pi * k / 4)) for k in range(4)]
    out['Lepido_Pedestal_Fade'] = [pedestal(1 - (k + 1) / 6) for k in range(6)]
    out['Lepido_Pedestal_Empty'] = [pedestal(0.0)]
    out['Lepido_StumpRoom'] = [room_interior(0, frog=False, base=True)]
    out['Fx_StumpRoom_Rays'] = [room_rays(k) for k in range(8)]
    out['Fx_StumpRoom_Light'] = [room_light(1.0)]
    out['Lepido_StumpRoom_Exit'] = [room_exit(k) for k in range(6)]
    out['UI_RelicHalo'] = [halo_rays(k * 2) for k in range(8)]
    fw = max(name_img(RELIC_NAME).width, name_img(SPORE_NAME).width) + 60
    out['UI_RelicFillet_Top'] = [fillet(fw)]
    out['UI_RelicFillet_Bottom'] = [fillet(fw, False)]
    out['Title_Relic_RootAncestral'] = [name_img(RELIC_NAME)]
    out['Title_Key_PurpleSpore'] = [name_img(SPORE_NAME)]
    out['Icon_PurpleSpore_16'] = [icon(purple_spore(1, S=.68), 16)]
    out['Icon_PurpleSpore_32'] = [icon(purple_spore(1, S=1.4), 32)]
    out['Icon_RootAncestral_16'] = [icon(relic(1, S=.52), 16)]
    out['Icon_RootAncestral_32'] = [icon(relic(1, S=1.04), 32)]
    return out

def piezas_reward():
    out = build_reward()
    for name, fr in out.items(): save_sheet(os.path.join(REWARD, name + '.png'), fr)
    print('   Reward/ escrito:', len(out), 'hojas')
    return out

# =================================================================================================
# Escenas (480×270, cámara ×1)
# =================================================================================================
def _sf(folder, name, w, k=0):
    im = Image.open(os.path.join(HERE, folder, name + '.png')).convert('RGBA')
    return im.crop((k * w, 0, k * w + w, im.height))

def escena_bajada(out):
    """En la arena tras el desplome: el corazón y la Espora morada bajan por la columna de luz girando, se quedan
    flotando delante del tocón; la rana coge la espora y sale su presentación pequeña (objeto clave)."""
    base, fx, fy = D.world_base()
    base.alpha_composite(_sf('Death', 'Lepido_WallRubble', 448), (int(fx + D.CRASH[0] - 224), int(fy + D.CRASH[1] - 64 - 2 * U)))
    base.alpha_composite(_sf('Death', 'Lepido_FallenTrunk', D.FW), (fx - D.FFX, fy - D.FFY))
    cx0, cy0 = fx - 240, int(fy - 150)
    heart = [A.frame(os.path.join(G.MT, 'shop', 'Item_HeartContainer_Idle.png'), k, 16) for k in range(4)]
    frog = A.frame(FROG, 0, 32)
    torn = [_sf('Death', 'Canopy_Arena_Torn_Sway', 704, k) for k in range(8)]
    col = [_sf('Death', 'Fx_GoldColumn', 128, k) for k in range(6)]
    stump = [_sf('Death', 'Lepido_Stump_Idle', 288, k) for k in range(6)]
    land = {'heart': (fx - 22, fy + 26), 'spore': (fx + 22, fy + 26)}
    top_y = cy0 + 40
    frames, ms, keys = [], [], {}
    def frame(t, hpos, spos, frog_xy, spore_spr=None, heart_on=True, spore_on=True):
        im = base.copy()
        im.alpha_composite(stump[t % 6], (fx - 144, fy - 352))
        for (on, p) in ((heart_on, hpos), (spore_on, spos)):
            if on and p[1] > top_y + 40:                 # sombrita cuando está cerca del suelo
                im.alpha_composite(out['Fx_RewardShadow'][t % 6], (int(p[0] - 8), int(land['heart'][1] + 6)))
        im.alpha_composite(frog, (int(frog_xy[0] - 16), int(frog_xy[1] - 30)))
        if heart_on:
            g = out['Fx_HeartGlow'][t % 4]; im.alpha_composite(g, (int(hpos[0] - 12), int(hpos[1] - 12)))
            im.alpha_composite(heart[t % 4], (int(hpos[0] - 8), int(hpos[1] - 8)))
        if spore_on:
            sp = spore_spr if spore_spr is not None else out['Item_PurpleSpore_Idle'][t % 6]
            im.alpha_composite(sp, (int(spos[0] - 8), int(spos[1] - 16)))
        c = col[t % 6]; im.alpha_composite(c, (fx - c.width // 2, int(fy - c.height + 4)))
        im.alpha_composite(torn[t % 8], (int((D.TC_X - 20) * U - 32), int((D.TC_Y - 10.25) * U) - 10))
        return im.crop((cx0, cy0, cx0 + RW_, cy0 + RH_))
    frog_xy = (fx + 70, fy + 70)
    N = 18
    for k in range(N):                                   # bajan despacio, girando, con un vaivén lateral
        e = 1 - (1 - (k + 1) / N) ** 2
        hy = top_y + (land['heart'][1] - top_y) * e; sy = top_y - 14 + (land['spore'][1] - top_y + 14) * e
        hx = fx - 6 + (land['heart'][0] - fx + 6) * e + 4 * math.sin(k * .5); sx = fx + 6 + (land['spore'][0] - fx - 6) * e + 4 * math.sin(k * .5 + 2)
        frames.append(frame(k, (hx, hy), (sx, sy), frog_xy, out['Item_PurpleSpore_Fall'][k % 6])); ms.append(100)
        if k == 8: keys['bajan por la luz girando'] = len(frames) - 1
    for k in range(8):                                   # flotan; la rana se acerca a la espora
        fxy = (frog_xy[0] - k * 5, frog_xy[1] - k * 4)
        frames.append(frame(N + k, land['heart'], land['spore'], fxy)); ms.append(120)
        if k == 2: keys['flotan delante del tocón'] = len(frames) - 1
    fxy = (frog_xy[0] - 40, frog_xy[1] - 32)
    bg = frame(N + 8, land['heart'], land['spore'], fxy, spore_on=False)
    start = (land['spore'][0] - cx0, land['spore'][1] - cy0 - 8)
    big = out['Item_PurpleSpore_Big']
    for k in range(6):                                   # la presentación pequeña (objeto clave)
        frames.append(present_frame(bg, 'O B J E T O   C L A V E', SPORE_NAME, SPORE_EFFECT, big[k % 6], k, (k + 1) / 6, start, 0)); ms.append(70)
    for k in range(12):
        frames.append(present_frame(bg, 'O B J E T O   C L A V E', SPORE_NAME, SPORE_EFFECT, big[k % 6], 6 + k, 1, start, min(1, (k + 1) / 3))); ms.append(110)
        if k == 6: keys['presentación: objeto clave'] = len(frames) - 1
    _gif('Lepido_recompensa_bajada', frames, ms)
    return [(n, frames[i]) for n, i in keys.items()]

def escena_sala(out):
    """Fundido dorado al entrar por la grieta; la rana va al pedestal, pulsa E, la reliquia sube con su presentación y
    el cuenco se apaga."""
    room = out['Lepido_StumpRoom'][0]
    import generar_pie_lepidodendron as PIE
    frog = A.frame(FROG, 0, 32)
    def frame(t, frog_xy, relic_on=True, ped='lit', pk=0, show_e=False):
        im = room.copy()
        for j, (u, kind) in enumerate(SHROOMS):
            x, y = shroom_pos(u)
            im.alpha_composite(PIE.shroom_cluster(kind, .25 + .2 * math.sin(2 * math.pi * (t + j) / 6)), (int(x - 12), int(y - 16)))
        im.alpha_composite(out['Fx_StumpRoom_Light'][0] if ped == 'lit' else Image.new('RGBA', (RW_, RH_)), (0, 0))
        p = out['Lepido_Pedestal_Lit'][t % 4] if ped == 'lit' else out['Lepido_Pedestal_Fade'][min(5, pk)] if ped == 'fade' else out['Lepido_Pedestal_Empty'][0]
        im.alpha_composite(p, (RCX - 28, RCY + 6 - 48))
        if relic_on:
            r = out['Relic_RootAncestral_Idle'][t % 8]; im.alpha_composite(r, (RCX - 12, RCY + 6 - 48 + 15 - 32 + 2))
        ex = out['Lepido_StumpRoom_Exit'][t % 6]; im.alpha_composite(ex, (RCX - 12, RCY + RR + 2 - 8))
        im.alpha_composite(frog, (int(frog_xy[0] - 16), int(frog_xy[1] - 30)))
        if show_e:                                       # el aviso de la tecla (lo dibuja la UI del juego)
            d = ImageDraw.Draw(im); d.fontmode = '1'
            d.rectangle([frog_xy[0] - 6, frog_xy[1] - 46, frog_xy[0] + 6, frog_xy[1] - 34], fill=(20, 16, 24), outline=GOLD[3])
            d.text((frog_xy[0] - 2, frog_xy[1] - 45), 'E', font=GP.font('Silkscreen-Regular.ttf', 8), fill=CREAM)
        im.alpha_composite(out['Fx_StumpRoom_Rays'][t % 8])
        return im
    frames, ms, keys = [], [], {}
    gold = Image.new('RGBA', (RW_, RH_), GOLD[3][:3] + (255,))
    start = (RCX, RCY + RR - 6)
    for k in range(4):                                   # fundido dorado de entrada
        im = frame(k, start); g = gold.copy(); g.putalpha(int(255 * (1 - k / 4))); im.alpha_composite(g)
        frames.append(im); ms.append(110)
    keys['entra (fundido dorado)'] = 1
    for k in range(14):                                  # la rana va hacia el pedestal
        fxy = (RCX, start[1] - k * 3.7)
        frames.append(frame(4 + k, fxy, show_e=(k >= 11))); ms.append(110)
        if k == 6: keys['la sala del tocón'] = len(frames) - 1
        if k == 12: keys['junto al pedestal: E'] = len(frames) - 1
    fxy = (RCX, start[1] - 13 * 3.7)
    big = out['Relic_RootAncestral_Big']
    rstart = (RCX, RCY + 6 - 48 + 15 - 16 + 2)
    for k in range(6):                                   # sube la reliquia; el cuenco empieza a apagarse
        bg = frame(18 + k, fxy, relic_on=False, ped='fade', pk=k)
        frames.append(present_frame(bg, 'R E L I Q U I A', RELIC_NAME, RELIC_EFFECT, big[k % 8], k, (k + 1) / 6, rstart, 0)); ms.append(80)
    bg = frame(24, fxy, relic_on=False, ped='empty')
    for k in range(14):
        frames.append(present_frame(bg, 'R E L I Q U I A', RELIC_NAME, RELIC_EFFECT, big[(6 + k) % 8], 6 + k, 1, rstart, min(1, (k + 1) / 3))); ms.append(120)
        if k == 7: keys['presentación: reliquia'] = len(frames) - 1
    for k in range(4):                                   # vuelve el juego: el cuenco vacío
        frames.append(frame(25 + k, fxy, relic_on=False, ped='empty')); ms.append(140)
    keys['el cuenco vacío'] = len(frames) - 2
    _gif('Lepido_recompensa_sala', frames, ms)
    return [(n, frames[i]) for n, i in keys.items()]

def _gif(name, frames, ms):
    os.makedirs(os.path.join(GIFR, 'escenas'), exist_ok=True)
    o = [f.convert('RGB') for f in frames]
    o[0].save(os.path.join(GIFR, 'escenas', name + '.gif'), save_all=True, append_images=o[1:], duration=ms, loop=0)

def lamina_escenas(keys):
    cols = 4
    rows = (len(keys) + cols - 1) // cols
    sheet = Image.new('RGBA', ((RW_ + 8) * cols, (RH_ + 26) * rows), (16, 18, 16, 255)); d = ImageDraw.Draw(sheet)
    for j, (nm, f) in enumerate(keys):
        x, y = (j % cols) * (RW_ + 8), (j // cols) * (RH_ + 26)
        sheet.alpha_composite(f, (x, y + 24)); d.text((x + 6, y + 4), nm, fill=(120, 230, 230), font=A.font(16))
    G._save(sheet, 'Lepidodendron ancestral · 6o-2 escenas de la recompensa (480×270, ×1)',
            ['GIF: gif/escenas/Lepido_recompensa_bajada.gif (la luz, la espora y su presentación) y Lepido_recompensa_sala.gif (la sala y la reliquia).'],
            'vista_previa_6o2_escenas.png')

def lamina_piezas(out):
    s = 2; pad = 10
    rows = [('Item_PurpleSpore_Idle (6) · _Fall (6, gira) · Fx_RewardShadow · Fx_HeartGlow', out['Item_PurpleSpore_Idle'][::2] + out['Item_PurpleSpore_Fall'] + out['Fx_RewardShadow'][:1] + out['Fx_HeartGlow'][:1]),
            ('Relic_RootAncestral_Idle (8, gira y flota) · iconos 16 / 32', out['Relic_RootAncestral_Idle'] + out['Icon_PurpleSpore_16'] + out['Icon_PurpleSpore_32'] + out['Icon_RootAncestral_16'] + out['Icon_RootAncestral_32']),
            ('Lepido_Pedestal_Lit (4) · _Fade (6) · _Empty', out['Lepido_Pedestal_Lit'][:1] + out['Lepido_Pedestal_Fade'][::2] + out['Lepido_Pedestal_Empty']),
            ('Item_PurpleSpore_Big · Relic_RootAncestral_Big · UI_RelicHalo', out['Item_PurpleSpore_Big'][:1] + out['Relic_RootAncestral_Big'][::3] + out['UI_RelicHalo'][:1])]
    hs = [max(f.height for f in fr) * s + 26 for _, fr in rows]
    W = max(sum(f.width * s + pad for f in fr) for _, fr in rows) + 2 * pad
    im = Image.new('RGBA', (W, sum(hs) + pad), C('#2a2232')); d = ImageDraw.Draw(im)
    y = pad
    for (lab, fr), h in zip(rows, hs):
        d.text((pad, y), lab, fill=(120, 230, 230), font=A.font(15))
        x = pad
        for f in fr:
            im.alpha_composite(f.resize((f.width * s, f.height * s), Image.NEAREST), (x, y + 20)); x += f.width * s + pad
        y += h
    G._save(im, 'Lepidodendron ancestral · 6o-2 hojas de la recompensa (×2)', ['Tamaños, pivotes y posiciones en el README.'], 'vista_previa_6o2_piezas.png')

if __name__ == '__main__':
    which = sys.argv[1:] or ['muestra']
    if 'muestra' in which: lamina_muestra()
    if 'piezas' in which or 'escenas' in which:
        out = piezas_reward(); lamina_piezas(out)
        if 'escenas' in which: lamina_escenas(escena_bajada(out) + escena_sala(out))
