"""Láminas de escala del Lepidodendron ancestral (jefe secreto del piso 3, tanda 6, paso 6l-0): NO es el arte final.

Un árbol provisional (corteza vieja de rombos con vetas moradas, copa de ramas en horquilla con penachos de hojas,
3 conos colgando) en su sala: un pasillo horizontal de ~5 pantallas con las 3 paredes de raíces y la arena final con
el árbol. Dos vistas para elegir:

- A (picada al final): el árbol se alza exento en la arena final, en la misma picada que todo; se puede rodear y la
  copa se sale de la pantalla por arriba (se dibuja encima del jugador). Cae hacia la izquierda (hacia la sala).
- B (pared frontal): el tronco ES la pared norte de la arena; la copa queda siempre cortada por arriba y solo asoman
  las ramas bajas con los conos. Cae hacia abajo (hacia la cámara).

Con la rana del jugador (32 px) y los recuadros de la cámara (480×270 px = 30×16,9 u; ×0,75 alejada = 40×22,5 u).

Uso: python generar_escala_lepidodendron.py [A] [B]   (sin nada: las dos)
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
MT = os.path.normpath(os.path.join(HERE, '..', '..'))
sys.path.insert(0, os.path.join(MT, 'floor3-bosses', 'arthropleura'))
sys.path.insert(0, os.path.join(MT, 'floor3-environment'))
import generar_escala_arthropleura as A                # room (Smoke), rect_room, frame, font, cam_box, player
from base_piso3 import *                               # noqa: F401,F403
from PIL import Image, ImageDraw

U = 16
PROPS = os.path.join(MT, 'floor3-environment', 'Smoke', 'props')
FROG = os.path.join(MT, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')

BARK_OLD = [C(c) for c in ('#121614', '#1e2520', '#2e3830', '#435046', '#66725e', '#8c9478')]  # corteza vieja
VEIN = [C(c) for c in STROB]                           # vetas moradas (camino secreto)
LEAF_OLD = [C(c) for c in ('#162016', '#22321e', '#34482a', '#53663c')]                      # penachos viejos
MOSS_OLD = [C(c) for c in ('#2a3020', '#424a2c', '#626a3c')]
ROOTP = [C(c) for c in ('#141714', '#252b24', '#38402f', '#525a40', '#767c58')]               # raíces
SPORE = C(STROB[3])
RED = (255, 90, 70)
CYAN = (120, 230, 230)

# =================================================================================================
# Árbol provisional
# =================================================================================================
def old_trunk(cv, cx, w0, w1, y0, y1, seed=1, veins=True):
    """Tronco viejo: cojinetes en rombo grandes y hondos, se ensancha hacia abajo (w0 arriba, w1 abajo); vetas
    moradas que serpentean de arriba abajo."""
    rnd = random.Random(seed)
    cw, ch = 12, 14
    for y in range(y0, y1):
        t = (y - y0) / max(1, y1 - y0)
        w = w0 + (w1 - w0) * t ** 2.2
        x0 = cx - w / 2
        row = (y - y0) // ch
        for x in range(int(x0), int(x0 + w) + 1):
            u = (x - x0 + .5) / w
            lx = (x - x0 + (cw / 2 if row % 2 else 0)) % cw - (cw - 1) / 2
            ly = (y - y0) % ch - (ch - 1) / 2
            dd = abs(lx) / (cw / 2) + abs(ly) / (ch / 2)
            light = 5 if u < .14 else 4 if u < .32 else 3 if u < .58 else 2 if u < .84 else 1
            if dd > .9: c = BARK_OLD[max(0, light - 3)]                      # surco hondo
            elif dd < .22: c = BARK_OLD[max(0, light - 2)]                   # cicatriz de la hoja
            else: c = BARK_OLD[light] if dd < .6 else BARK_OLD[max(1, light - 1)]
            cv.put(x, y, c)
    if not veins: return
    for k in range(4):                                  # vetas moradas: bajan serpenteando por los surcos
        xs = cx + (k - 1.5) * w0 * .22 + rnd.uniform(-4, 4)
        ph = rnd.uniform(0, 6)
        for y in range(y0 + rnd.randint(0, 30), y1 - 4):
            t = (y - y0) / (y1 - y0)
            x = xs * (1 + .0) + math.sin(y / 17 + ph) * 5 + (xs - cx) * t ** 2.2 * (w1 / w0 - 1)
            cv.put(x, y, VEIN[3]); cv.put(x + 1, y, VEIN[2])
            if y % 23 == 0: cv.put(x - 1, y, VEIN[4])                     # latido (brillo)

def branch(cv, x, y, ang, L, w, depth, tips, rnd, spread=.42, sq=1.0):
    """Rama en horquilla (Lepidodendron: se divide en dos una y otra vez)."""
    ex, ey = x + math.cos(ang) * L, y + math.sin(ang) * L * sq
    n = int(L)
    for i in range(n + 1):
        t = i / n
        px, py = x + (ex - x) * t, y + (ey - y) * t
        r = max(1, w * (1 - t * .3)) / 2
        for yy in range(int(py - r), int(py + r) + 1):
            for xx in range(int(px - r), int(px + r) + 1):
                if math.hypot(xx + .5 - px, yy + .5 - py) > r + .3: continue
                s = ((xx - px) * -.6 + (yy - py) * -.8) / max(r, .5)
                tone = 4 if s > .45 else 3 if s > -.2 else 2 if s > -.7 else 1
                if r > 3 and ((xx % 9 == 0 and yy % 10 == 0) or ((xx + 4) % 9 == 0 and (yy + 5) % 10 == 0)):
                    tone = max(0, tone - 2)                             # cojinetes (rombos) también en las ramas
                cv.put(xx, yy, BARK_OLD[tone])
    if depth:
        for s_ in (-1, 1):
            branch(cv, ex, ey, ang + s_ * spread * rnd.uniform(.8, 1.2), L * rnd.uniform(.62, .74), w * .66,
                   depth - 1, tips, rnd, spread, sq)
    else: tips.append((ex, ey, ang))

def tuft(cv, x, y, rnd, big=1.0):
    """Penacho de hojas de aguja al final de la rama (las del Lepidodendron son finas como hierba)."""
    fr = []
    for k in range(rnd.randint(15, 19)):
        a = -math.pi / 2 + rnd.uniform(-2.3, 2.3)
        fr.append((a, rnd.uniform(15, 24) * big))
    fr.sort(key=lambda p: -abs(p[0] + math.pi / 2))     # primero las que caen a los lados (detrás)
    for a, L in fr:
        frondlet(cv, x, y, a, L, LEAF_OLD, .6, 2.0, None, 0 if a < -1.0 else 1)

def cone(cv, x, y, w=26, h=42, stalk=12):
    """Cono (estróbilo) colgando, cerrado: escamas apretadas en filas."""
    for j in range(stalk): cv.put(x, y + j, BARK_OLD[2]); cv.put(x + 1, y + j, BARK_OLD[1])
    cy = y + stalk + h / 2
    for yy in range(int(cy - h / 2), int(cy + h / 2) + 1):
        for xx in range(int(x - w / 2), int(x + w / 2) + 1):
            nx, ny = (xx - x) / (w / 2), (yy - cy) / (h / 2)
            if nx * nx + ny * ny > 1: continue
            row = int((yy - cy + h / 2) // 6)
            lx = ((xx - x + (3 if row % 2 else 0)) % 6) - 2.5
            ly = ((yy - cy + h / 2) % 6) - 2.5
            edge = ly > 1.6 or abs(lx) > 2.3
            lit = -(nx * .6 + ny * .8)
            tone = 4 if lit > .55 else 3 if lit > .05 else 2 if lit > -.45 else 1
            cv.put(xx, yy, VEIN[max(0, tone - 2)] if edge else VEIN[tone])
    return (x, cy)

def ancient_tree(view, seed=7):
    """Devuelve (imagen, pie (px), centros de los 3 conos (px), punta de la copa (px)).
    view 'A': exento, en picada; 'B': pared frontal (más ancho, ramas abiertas en horizontal, copa cortada)."""
    rnd = random.Random(seed)
    if view == 'A':
        Wc, Hc = 600, 600
        cx, foot, top = 300, 556, 326
        tw0, tw1 = 64, 112
    else:                                               # el tronco sale por arriba de la cámara: solo ~9 u a la vista
        Wc, Hc = 1000, 330
        cx, foot, top = 500, 250, 120
        tw0, tw1 = 112, 168
    cv = Canvas(Wc, Hc)
    # raíces de detrás (asoman a los lados del pie)
    for s_ in (-1, 1):
        root(cv, [(cx + s_ * tw1 * .3, foot - 22), (cx + s_ * tw1 * .6, foot - 34), (cx + s_ * tw1 * .9, foot - 30),
                  (cx + s_ * tw1 * 1.15, foot - 16)], 24, 7, ROOTP)
    old_trunk(cv, cx, tw0, tw1, top, foot - 6, seed)
    tips = []
    if view == 'A':                                     # copa en horquilla, abierta hacia arriba
        for s_ in (-1, 1):
            branch(cv, cx + s_ * 14, top + 8, -math.pi / 2 + s_ * .62, 92, 36, 3, tips, rnd, .44)
    else:                                               # pared: las ramas bajas salen casi en horizontal por arriba
        for s_ in (-1, 1):
            base_a = math.pi - .1 if s_ < 0 else .1
            branch(cv, cx + s_ * 40, top + 14, base_a, 150, 40, 3, tips, rnd, .3, .7)
    # musgo y raíces aéreas que cuelgan de las ramas
    for (x, y, a) in tips[::2]:
        L = rnd.randint(16, 34)
        for j in range(L): cv.put(x + round(math.sin(j / 4) * .7), y + 8 + j, MOSS_OLD[2 if j < 3 else 1 if j < L - 3 else 0])
    tips_in = [t for t in tips if 0 <= t[0] < Wc]
    for (x, y, a) in tips_in: tuft(cv, x, y, rnd, 1.0 if view == 'A' else 1.1)
    # los 3 conos: punta más a la izquierda, la más central y la más a la derecha (de las más bajas)
    low = sorted(tips_in, key=lambda p: p[1])[len(tips_in) // 3:]
    if view == 'A':                                     # laterales en las puntas más bajas; el central, de la horquilla
        picks = [min(low, key=lambda p: p[0]), (cx, top + 20, 0), max(low, key=lambda p: p[0])]
    else:                                               # B: laterales en las ramas maestras a ±12,5 u; el central, de la horquilla
        picks = [(cx - 200, top + 24, 0), (cx, top + 24, 0), (cx + 200, top + 24, 0)]   # de las ramas maestras
    cones = [cone(cv, int(x), int(y) + 4, stalk=10 if view == 'A' else 22) for (x, y, a) in picks]
    # raíces de delante: contrafuertes gruesos y cortos que se abren por el suelo
    fronts = ((-1, .3), (-1, .75), (1, .35), (1, .8)) if view == 'A' else ((-1, .25), (-1, .7), (-1, 1.15), (1, .3), (1, .75), (1, 1.2))
    for s_, k in fronts:
        reach = tw1 * (.75 + k * .7) if view == 'A' else tw1 * (.55 + k * .45)
        drop = (16 + k * 12) if view == 'A' else (34 + k * 24)
        root(cv, [(cx + s_ * tw1 * .3 * k, foot - 16), (cx + s_ * reach * .5, foot - 16),
                  (cx + s_ * reach * .85, foot + drop * .3), (cx + s_ * reach, foot + drop)], 34, 8, ROOTP)
    cv.outline(); cv.clean()
    img = cv.img().convert('RGBA')
    crown_top = min(y for (x, y) in cv.px)
    return img, (cx, foot), cones, crown_top

# =================================================================================================
# Pared de raíces provisional (cruza el pasillo de norte a sur)
# =================================================================================================
def root_wall(im, x, y0, y1, thick, seed, twist=1.0):
    """Maraña de raíces entrelazadas, de thick u de ancho, con 3 nudos débiles morados."""
    rnd = random.Random(seed)
    H = int((y1 - y0) * U) + 40
    Wd = int(thick * U) + 40
    cv = Canvas(Wd, H)
    cxw = Wd / 2
    hw = thick * U / 2
    for y in range(4, H - 4):                           # fondo oscuro de la maraña (no se ve a través)
        e = 3 * math.sin(y / 7 + seed) + 2 * math.sin(y / 3.1)
        for xx in range(int(cxw - hw + e), int(cxw + hw - e) + 1): cv.put(xx, y, ROOTP[0])
    n = int(10 * twist) + 8
    for k in range(n):                                  # raíces largas repartidas a lo ancho, que bajan retorciéndose
        a = (k / (n - 1) - .5) * 2 * hw * .9 + rnd.uniform(-3, 3)
        b = rnd.uniform(-1, 1) * hw * .5 * twist
        root(cv, [(cxw + a, 4), (cxw + a - b, H * .35), (cxw + a + b, H * .65), (cxw + a * .8 + rnd.uniform(-4, 4), H - 6)],
             rnd.uniform(12, 18), rnd.uniform(9, 14), ROOTP)
    for k in range(int(6 * twist)):                     # lazos cortos que cruzan
        yy = rnd.uniform(.1, .9) * H
        root(cv, [(cxw - thick * U * .5, yy), (cxw - 6, yy - 18), (cxw + 6, yy + 18), (cxw + thick * U * .5, yy + rnd.uniform(-10, 10))],
             7, 5, ROOTP)
    for k in range(3):                                  # nudos débiles (×2 de daño), morados y que laten
        ky = H * (.22 + k * .28) + rnd.uniform(-8, 8)
        kx = cxw + rnd.uniform(-.2, .2) * thick * U
        ellipse(cv, kx, ky, 9, 8, [VEIN[0], VEIN[1], VEIN[2], VEIN[3], VEIN[4]])
        cv.put(kx - 3, ky - 3, VEIN[4])
    cv.outline(); cv.clean()
    im.alpha_composite(cv.img().convert('RGBA'), (int(x * U - Wd / 2), int(y0 * U - 20)))

# =================================================================================================
# Sala completa
# =================================================================================================
TOP = 14                                               # tiles de margen arriba (la copa sale por encima)
CORR = (2, TOP + 6, 117, TOP + 23)                     # pasillo: 116 × 18 u
ARENA = (118, TOP + 2, 157, TOP + 27)                  # arena final: 40 × 26 u
WALLS = [(34, 3.0, 1.0), (64, 3.5, 1.4), (94, 4.0, 1.8)]   # x, grosor (u), retorcimiento

def put(im, spr, x, y, anchor='foot'):
    oy = spr.height - 4 if anchor == 'foot' else spr.height / 2
    im.alpha_composite(spr, (int(x * U - spr.width / 2), int(y * U - oy)))

def dashed(d, pts, col, w=3, dash=8):
    for (a, b) in zip(pts, pts[1:]):
        L = math.hypot(b[0] - a[0], b[1] - a[1]); n = max(1, int(L // dash))
        for i in range(0, n, 2):
            t0, t1 = i / n, min(1, (i + 1) / n)
            d.line([(a[0] + (b[0] - a[0]) * t0, a[1] + (b[1] - a[1]) * t0), (a[0] + (b[0] - a[0]) * t1, a[1] + (b[1] - a[1]) * t1)], fill=col, width=w)

def build(view):
    """Dibuja la sala entera (1 px = 1 px del juego) y devuelve (imagen, datos para anotar)."""
    W, H = 160, TOP + 30
    if view == 'B':                                    # en B la arena es menos alta: el árbol se come el norte
        arena = (ARENA[0], TOP + 12, ARENA[2], ARENA[3])
    else: arena = ARENA
    im = A.room(A.rect_room(W, H, CORR, gaps=[arena]))
    rnd = random.Random(3)
    ferns = [('Fern_Leafy_Sway', 84), ('Fern_Tree_Sway', 84), ('Fern_Bent_Sway', 88), ('Fern_Young_Sway', 44)]
    spots = []
    for (wx, th, tw) in WALLS:                         # odres y helechos junto a cada pared (el fuego la rompe antes)
        spots += [(wx - th / 2 - 2.2, CORR[1] + 4, 'sac'), (wx - th / 2 - 2.6, CORR[3] - 3, 'fern'),
                  (wx - th / 2 - 4.5, CORR[1] + 9, 'fern'), (wx - th / 2 - 2, CORR[1] + 12.5, 'sac')]
    for k in range(26):
        fx, fy = rnd.uniform(CORR[0] + 6, CORR[2] - 2), rnd.uniform(CORR[1] + 1.5, CORR[3])
        if any(abs(fx - wx) < th / 2 + 6 for (wx, th, tw) in WALLS): continue
        spots.append((fx, fy, 'fern'))
    # metano junto al tronco (prendido abre todos los conos)
    mist = A.frame(os.path.join(PROPS, 'Methane_Mist.png'), 0, 48) if os.path.exists(os.path.join(PROPS, 'Methane_Mist.png')) else None
    for (x, y, k) in sorted(spots, key=lambda p: p[1]):
        if k == 'fern':
            n, w = rnd.choice(ferns); spr = A.frame(os.path.join(PROPS, n + '.png'), 0, w)
            if rnd.random() < .5: spr = spr.transpose(Image.FLIP_LEFT_RIGHT)
            put(im, spr, x, y)
        else: put(im, A.frame(os.path.join(PROPS, 'Methane_Sac_Idle.png'), 0, 24), x, y)
    for i, (wx, th, tw) in enumerate(WALLS):
        root_wall(im, wx, CORR[1] - 1.5, CORR[3] + 1, th, 10 + i, tw)
    tree, (fx_, fy_), cones, ctop = ancient_tree(view)
    if view == 'A': base = (143, TOP + 23)              # exento, a la derecha de la arena (se rodea; cámara detrás, al este)
    else: base = (137.5, arena[1] + 1.0)                # en la pared norte
    ox, oy = int(base[0] * U - fx_), int(base[1] * U - fy_)
    if mist:
        for (mx, my) in ((base[0] - 6, base[1] + 2), (base[0] + 5, base[1] + 3.5)) if view == 'A' else ((base[0] - 7, base[1] + 4), (base[0] + 7, base[1] + 4.5)):
            put(im, mist, mx, my, 'center')
    im.alpha_composite(tree, (ox, oy))
    frog_A = (128, TOP + 16) if view == 'A' else (130, TOP + 21)
    A.player(im, 8, TOP + 15)
    A.player(im, *frog_A)
    info = dict(arena=arena, base=base, cones=[((x + ox) / U, (y + oy) / U) for (x, y) in cones],
                crown_top=(ctop + oy) / U, frog=frog_A, tree_h=(fy_ - ctop) / U)
    return im, info

def notes_common():
    return ['Pasillo de 116×18 u (~4 pantallas) + arena final de 40 u: en total ~5 pantallas. Paredes de raíces de 3 / 3,5 / 4 u, cada una más retorcida, con 3 nudos morados (×2).',
            'Junto a cada pared, a su izquierda: 2 odres de metano y helechos (el fuego la rompe mucho más rápido). Árbol, paredes y conos provisionales: solo forma y tamaño.']

def lamina_sala(view):
    im, info = build(view)
    s = 1
    big = im.copy()
    d = ImageDraw.Draw(big)
    A.cam_box(d, 15, TOP + 14.5, 1, A.WHITE, 'cámara ×1 al empezar', s)
    for i, (wx, th, tw) in enumerate(WALLS):
        d.text(((wx - 3) * U, (CORR[1] - 3.2) * U), f'pared {i + 1} (25 %)', fill=CYAN, font=A.font(15), stroke_width=3, stroke_fill=(10, 10, 14))
    bx, by = info['base']
    d.text(((ARENA[0] + 1) * U, (info['crown_top'] - 1.6) * U if view == 'A' else (TOP - 2) * U),
           f'árbol: {info["tree_h"]:.0f} u de alto ({info["tree_h"] * U:.0f} px)' if view == 'A' else 'árbol: se ven ~10 u de tronco; la copa queda siempre fuera, por arriba', fill=CYAN, font=A.font(15), stroke_width=3, stroke_fill=(10, 10, 14))
    d.text((5 * U, (TOP + 16.4) * U), 'rana', fill=CYAN, font=A.font(14), stroke_width=3, stroke_fill=(10, 10, 14))
    titulo = {'A': 'A · picada: el árbol exento al final de la sala', 'B': 'B · pared frontal: el tronco cierra la arena por el norte'}[view]
    _save(big, f'Lepidodendron ancestral · sala entera (Bosque humeante) · {titulo}', notes_common(), f'vista_previa_escala_sala_{view}.png')

def lamina_final(view):
    im, info = build(view)
    ax0, ay0, ax1, ay1 = info['arena']
    x0, x1 = ax0 - 4, ax1 + 2
    cam_cy = (ay0 + ay1) / 2 + .5 if view == 'A' else ay0 - 7.5 + 11.25
    y0 = int(min(info['crown_top'] - 1, ay0 - 2)) if view == 'A' else int(cam_cy - 11.25 - 1); y0 = max(0, y0)
    y1 = ay1 + 2
    crop = im.crop((x0 * U, y0 * U, x1 * U, y1 * U))
    s = 2
    big = crop.resize((crop.width * s, crop.height * s), Image.NEAREST)
    d = ImageDraw.Draw(big)
    P = lambda x, y: ((x - x0) * U * s, (y - y0) * U * s)
    fx, fy = info['frog']
    bx, by = info['base']
    cams = [((ax0 + ax1) / 2 + .5, cam_cy, 4 / 3, (255, 214, 90), 'cámara alejada ×0,75 (40×22,5 u) en la fase final')]
    for (cx, cy, z, col, label) in cams:
        w, h = A.CAM[0] * z * s, A.CAM[1] * z * s
        X, Y = P(cx, cy)
        dashed(d, [(X - w / 2, Y - h / 2), (X + w / 2, Y - h / 2), (X + w / 2, Y + h / 2), (X - w / 2, Y + h / 2), (X - w / 2, Y - h / 2)], col, 3, 10)
        d.text((X + w / 2 - 430, Y + h / 2 - 24), label, fill=col, font=A.font(16), stroke_width=3, stroke_fill=(10, 10, 14))
    # sombra del desplome
    L = info['tree_h']
    if view == 'A':                                     # tronco tumbado hacia la izquierda + copa aplastada al final
        poly = [P(bx - 1, by - 2.2), P(bx - 14, by - 1.6), P(bx - 14, by + 1.6), P(bx - 1, by + 2.2)]
        ell = (P(bx - 27, by - 8), P(bx - 13, by + 3))
        lab = (P(bx - 25.5, by - 3), 'sombra del desplome:\ntronco + copa\n(cae hacia la sala)')
    else:                                               # cae hacia la cámara: columna + copa abajo
        poly = [P(bx - 3.6, by + .5), P(bx + 3.6, by + .5), P(bx + 2.6, ay1 - 6), P(bx - 2.6, ay1 - 6)]
        ell = (P(bx - 8, ay1 - 6.5), P(bx + 8, ay1 + 1.5))
        lab = (P(bx + 9.5, ay1 - 4), 'sombra del desplome\n(cae hacia la cámara;\nla copa cae fuera de la vista)')
    d.ellipse([*ell[0], *ell[1]], outline=RED, width=3)
    dashed(d, poly + [poly[0]], RED, 3, 8)
    d.multiline_text(lab[0], lab[1], fill=RED, font=A.font(16), stroke_width=3, stroke_fill=(10, 10, 14))
    # conos y su abanico de esporas
    for i, (cx, cy) in enumerate(info['cones']):
        X, Y = P(cx, cy)
        d.text((X - 22, Y + 50), f'cono {i + 1}', fill=CYAN, font=A.font(15), stroke_width=3, stroke_fill=(10, 10, 14))
        if i == 1:
            for k in range(7):
                a = math.pi / 2 + (k - 3) * .32
                for r in (3, 5.5):
                    sx, sy = X + math.cos(a) * r * U * s, Y + 40 + math.sin(a) * r * U * s
                    d.ellipse([sx - 7, sy - 7, sx + 7, sy + 7], fill=SPORE[:3], outline=(24, 20, 37), width=2)
    # latigazo de raíz desde un lado (aviso) y raíz que brota bajo la rana
    wy = fy - 3
    d.rectangle([*P(ax0, wy - .7), *P(ax0 + 14, wy + .7)], outline=RED, width=3)
    d.text(P(ax0 + .4, wy + 1), 'latigazo desde el lado (aviso)', fill=RED, font=A.font(15), stroke_width=3, stroke_fill=(10, 10, 14))
    X, Y = P(fx, fy + .4)
    d.ellipse([X - 1.6 * U * s, Y - U * s, X + 1.6 * U * s, Y + U * s], outline=RED, width=3)
    d.text((X - 60, Y + U * s + 4), 'raíz bajo la rana', fill=RED, font=A.font(15), stroke_width=3, stroke_fill=(10, 10, 14))
    # esquinas: enemigos
    for (ex, ey) in ((ax0 + 1, ay0 + (1 if view == 'A' else .5)), (ax0 + 1, ay1 - 1)):
        X, Y = P(ex, ey); d.text((X, Y), '▼ enemigos', fill=(255, 214, 90), font=A.font(15), stroke_width=3, stroke_fill=(10, 10, 14))
    d.text(P(bx + (-12 if view == 'A' else -10), by + (2.4 if view == 'A' else 3.6)), 'metano junto al tronco\n(prendido: abre los 3 conos)', fill=METH, font=A.font(15), stroke_width=3, stroke_fill=(10, 10, 14))
    if view == 'A':
        d.text(P(ax1 - 6, ay0 + 1), 'cámara del pedestal\n(detrás, al este)', fill=(255, 214, 90), font=A.font(15), stroke_width=3, stroke_fill=(10, 10, 14))
    else:
        d.text(P(bx + 5, ay0 - 5.5), 'cámara del pedestal: dentro de la pared,\ndetrás del tronco (se abre al retirarse las raíces)', fill=(255, 214, 90), font=A.font(15), stroke_width=3, stroke_fill=(10, 10, 14))
    by_ = big.height - 30
    d.rectangle([20, by_, 20 + 32 * s, by_ + 8], fill=(240, 230, 200))
    d.text((28 + 32 * s, by_ - 6), '2 u = la rana (32 px)', fill=(240, 230, 200), font=A.font(16), stroke_width=3, stroke_fill=(10, 10, 14))
    if view == 'A':
        notes = [f'Árbol exento: {info["tree_h"]:.0f} u de alto (tronco 4 u arriba, 7 u en el pie, 14 u hasta la horquilla; copa ~26 u de ancho). Se puede rodear; la copa tapa al jugador (translúcida al pasar debajo).',
                 'Incluso con la cámara alejada la copa se sale por arriba: se ve entera solo en la presentación (la cámara sube) y en el desplome. El pie va al sur de la arena para que los conos entren en la vista.',
                 'Los conos laterales cuelgan de las puntas más bajas y el central de la horquilla; desde la arena se les dispara a su proyección en pantalla (como a la Megarachne en sus cables).']
    else:
        notes = [f'Árbol-pared: el tronco (7 u arriba, 11 u en el pie) cierra la arena por el norte; las ramas bajas se abren en horizontal y los 3 conos cuelgan delante.',
                 'La copa nunca se ve entera: solo asoman las ramas. Más fácil de leer (todo el combate mira hacia arriba) y no tapa al jugador, pero se lee menos «árbol».',
                 'Los contrafuertes de raíces bajan a la arena y hacen de coberturas; los latigazos y las raíces que brotan salen de ellos.']
    titulo = {'A': 'A · picada, exento', 'B': 'B · pared frontal'}[view]
    _save(big, f'Lepidodendron ancestral · arena final ×2 · {titulo}', notes, f'vista_previa_escala_final_{view}.png')

METH = C(METHANE[2])[:3]

def _save(big, title, notes, name):
    head = Image.new('RGBA', (big.width, 34 + 22 * len(notes)), (16, 18, 16, 255))
    hd = ImageDraw.Draw(head)
    hd.text((14, 6), title, fill=(255, 240, 200), font=A.font(20))
    for k, n_ in enumerate(notes): hd.text((14, 32 + 22 * k), n_, fill=(200, 210, 200), font=A.font(15))
    full = Image.new('RGBA', (big.width, big.height + head.height), (16, 18, 16, 255))
    full.alpha_composite(head, (0, 0)); full.alpha_composite(big, (0, head.height))
    full.convert('RGB').save(os.path.join(HERE, name)); print('  ', name, full.size)

# =================================================================================================
# C · concepto nuevo (2026-11-01, idea del usuario): el tronco se pierde por arriba, capa de hojas en primer plano,
# conos con armadura de ramas que bajan a golpear, pie robusto que se funde con el suelo, tocón hueco al morir.
# =================================================================================================
import generar_lepidodendron as Lp

TC_X = 138                                             # centro del tronco (centro de la arena)
TC_Y = TOP + 12.5                                      # pie: 2 u al norte del centro de la arena (paso por detrás)
TW_BOT, TW_TOP = 176, 132                              # 11 u en el pie, ~8 u donde se pierde
DARK = (10, 14, 12)

def tall_trunk(H, foot, seed=2, veins=True):
    """Tronco gigante de la vista C en un lienzo de altura H: rombos grandes, se ensancha mucho en el pie y se
    oscurece hacia arriba (se pierde en la penumbra de la copa)."""
    cw, ch = 16, 18
    Wd = TW_BOT + 220
    cv = Canvas(Wd, H + 40)
    ox = Wd / 2
    rnd = random.Random(seed)
    FL = 70                                             # altura del ensanche del pie
    FLW = 38                                            # cuánto se abre cada lado
    def hw(y):
        t = (foot - y) / foot                            # 0 en el pie, 1 arriba
        return (TW_TOP + (TW_BOT - TW_TOP) * (1 - t) ** 3) / 2 + FLW * max(0., (y - (foot - FL)) / FL) ** 2.3
    def lobe(ph):                                        # 5 lóbulos (contrafuertes) en el borde de abajo
        return abs(math.cos(ph * math.pi * 2.5))
    for y in range(0, foot + 30):
        h = hw(min(y, foot))
        dark = max(0., 1 - y / (foot * .55))             # 1 arriba del todo → 0 a media altura
        for x in range(int(ox - h), int(ox + h) + 1):
            ph = (x - ox) / h
            lb = lobe(ph)
            if y > foot - 4 + 6 * (1 - ph * ph) + 16 * lb ** 1.5 * (1 - .35 * abs(ph)): continue
            u = (x - (ox - h)) / (2 * h)
            row = math.floor((foot - y) / ch)
            xs = ph * min(h, TW_BOT / 2)                  # los rombos se estiran con el ensanche (siguen la superficie)
            lx = (xs + (cw / 2 if row % 2 else 0)) % cw - cw / 2
            ly = (foot - y) % ch - ch / 2
            dd = abs(lx) / (cw / 2) + abs(ly) / (ch / 2)
            light = 5 if u < .1 else 4 if u < .28 else 3 if u < .52 else 2 if u < .8 else 1
            if dd > .86: c = Lp.BARK_OLD[max(0, light - 3)]
            elif dd > .7 and lx < 0 and ly > 0: c = Lp.BARK_OLD[min(5, light + 1)]
            elif dd < .2: c = Lp.BARK_OLD[max(0, light - 2)]
            else: c = Lp.BARK_OLD[light]
            k_ = max(0., (y - (foot - FL)) / FL)          # 0 arriba del ensanche → 1 en el suelo
            if k_ > 0:                                   # canales de sombra entre contrafuertes; lomo iluminado
                if lb < .3 and dither(min(1, k_ * 1.6 * (1 - lb / .3)), x, y): c = blend(c, C('#0c100e'), .6)
                elif lb > .8 and ph * 2.5 % 1 < .5 and dither(k_ * .8, x, y): c = Lp.BARK_OLD[min(5, light + 1)]
            if dark > 0 and dither(min(1, dark * 1.1), x, y): c = blend(c, C('#0c100e'), .55 + .4 * dark)
            cv.put(x, y, c)
    cv.ends = [(ph * hw(foot), 4 + 6 * (1 - ph * ph) + 16 * (1 - .35 * abs(ph)) - 4) for ph in (-.8, -.4, 0, .4, .8)]
    for k in range(380):                                 # musgo: lado de la luz y abajo
        y = rnd.randint(int(foot * .35), foot - 4)
        h = hw(y)
        x = ox - h + rnd.uniform(0, h * (.8 if y < foot - 60 else 2))
        cv.put(x, y, Lp.MOSS[rnd.choice((1, 2, 2, 3))])
    if veins:                                            # vetas: solo aviso / golpe (finas, apagadas en reposo)
        for k, u in enumerate((.3, .55, .75)):
            for y in range(int(foot * .3), foot - 10):
                h = hw(y)
                x = ox - h + 2 * h * u + 3 * math.sin(y / 21 + k * 2)
                cv.put(x, y, Lp.VEIN[1]); cv.put(x + 1, y, Lp.VEIN[2] if (y // 40) % 3 else Lp.VEIN[3])
    cv.outline(); cv.clean()
    return cv.img().convert('RGBA'), ox, cv.ends

def mound(im, cx, cy, seed=4):
    """Pie: montículo de tierra con musgo que se funde con el suelo (trama en el borde) y raíces tipo Stigmaria que
    salen en cruz medio enterradas (lomos que entran y salen de la tierra) + sombra de contacto."""
    rnd = random.Random(seed)
    RX, RY = 14 * U, 5.2 * U
    cv = Canvas(int(RX * 2 + 40), int(RY * 2 + 40))
    ox, oy = cv.w / 2, cv.h / 2
    soil = SOIL
    for y in range(cv.h):
        for x in range(cv.w):
            d = ((x - ox) / RX) ** 2 + ((y - oy) / RY) ** 2
            if d > 1: continue
            if d > .55 and not dither((1 - d) / .45, x, y): continue        # se funde con el suelo
            lit = -((x - ox) / RX * .5 + (y - oy) / RY * .9)
            c = soil[3] if lit > .45 else soil[2] if lit > -.1 else soil[1]
            cv.put(x, y, c)
    for k in range(240):
        a = rnd.uniform(0, 6.28); r = math.sqrt(rnd.random()) * .9
        cv.put(ox + math.cos(a) * RX * r, oy + math.sin(a) * RY * r, Lp.MOSS[rnd.choice((1, 2, 2, 3))])
    hwf = TW_BOT / 2 + 38
    starts = [(ph, ox + ph * hwf * .92, oy - 2 + 8 * (1 - abs(ph))) for ph in (-.85, -.45, 0, .45, .85)]
    starts += [(-1.0, ox - hwf * .7, oy - 22), (1.0, ox + hwf * .7, oy - 22)]       # detrás, asoman a los lados
    for k, (ph, sx, sy) in enumerate(starts):
        back = k >= 5
        ang = (math.pi - .2 if ph < 0 else .2) if back else math.atan2(1.0 - .6 * abs(ph), ph * 1.4)
        L = rnd.uniform(8, 11) * U
        bend = rnd.uniform(-.5, .5)
        def P_(t, ang=ang, L=L, bend=bend, sx=sx, sy=sy):
            a_ = ang + bend * t
            return (sx + math.cos(a_) * t * L, sy + math.sin(a_) * t * L * .45)
        root(cv, [P_(t) for t in (0, .33, .66, 1)], 18, 4, ROOT_D)
        for t in (.38, .74):                                               # tierra que la tapa: entra y sale
            bx, by = P_(t)
            r = 13 * (1 - t * .4)
            for yy in range(int(by - r * .6), int(by + r * .6) + 1):
                for xx in range(int(bx - r), int(bx + r) + 1):
                    dd_ = ((xx - bx) / r) ** 2 + ((yy - by) / (r * .6)) ** 2
                    if dd_ <= 1 and (dd_ < .5 or dither((1 - dd_) / .5, xx, yy)):
                        cv.put(xx, yy, soil[2] if yy < by else soil[1])
    shadow = Image.new('RGBA', im.size, (0, 0, 0, 0))
    ImageDraw.Draw(shadow).ellipse([cx * U - TW_BOT * .75, cy * U - 30, cx * U + TW_BOT * .75, cy * U + 22], fill=DARK + (80,))   # sombra de contacto
    im.alpha_composite(shadow)
    cv.outline(); cv.clean()
    im.alpha_composite(cv.img().convert('RGBA'), (int(cx * U - ox), int(cy * U - oy)))

SOIL = [C(c) for c in ('#24251b', '#2e2f21', '#3b3b29', '#4b4932')]   # montículo: tonos del suelo, algo más claros
ROOT_D = [C(c) for c in ('#121510', '#1e231c', '#2c3326', '#3e4632', '#58603f')]

def soil_over(im, pts, seed=6):
    """Tierra y musgo que se tragan el final de los contrafuertes (borde en trama)."""
    rnd = random.Random(seed)
    cv = Canvas(im.width, im.height)
    for (ex, ey) in pts:
        for yy in range(int(ey - 8), int(ey + 10)):
            for xx in range(int(ex - 22), int(ex + 23)):
                d = ((xx - ex) / 22) ** 2 + ((yy - ey - 1) / 8.5) ** 2
                if d <= 1 and (d < .45 or dither((1 - d) / .55, xx, yy)):
                    cv.put(xx, yy, SOIL[2] if yy < ey - 2 else SOIL[1])
        for j in range(12): cv.put(ex + rnd.uniform(-16, 16), ey + rnd.uniform(-5, 4), Lp.MOSS[rnd.choice((1, 2, 3))])
    im.alpha_composite(cv.img().convert('RGBA'))

def dapple(im, box, strength, seed=9):
    """Sombra moteada de la copa sobre el suelo (box en u) con manchas de luz; strength(x en u) → 0..1."""
    x0, y0, x1, y1 = [int(v * U) for v in box]
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0))
    px = lay.load()
    for x in range(max(0, x0), min(im.width, x1)):
        f = strength(x / U)
        if f <= 0: continue
        for y in range(max(0, y0), min(im.height, y1)):
            v = (.5 + .17 * math.sin(x / 11 + math.sin(y / 9) * 2.2) + .17 * math.sin(y / 8 + math.cos(x / 14) * 2.4)
                 + .16 * math.sin((x + y) / 6.5 + math.sin(x / 21)))
            if v > .76: continue                                         # mancha de luz
            a = int((60 + 60 * (1 - v)) * f)
            if a > 0: px[x, y] = DARK + (a,)
    im.alpha_composite(lay)

def light_shaft(im, gx, gy, fx, fy, w=1.6):
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(lay)
    d.polygon([(gx * U - w * U / 2, gy * U), (gx * U + w * U / 2, gy * U), (fx * U + w * U * .9, fy * U), (fx * U - w * U * .9, fy * U)], fill=(255, 240, 170, 26))
    d.ellipse([fx * U - w * U * 1.3, fy * U - w * U * .5, fx * U + w * U * 1.3, fy * U + w * U * .5], fill=(255, 240, 170, 24))
    im.alpha_composite(lay)

def canopy_band(Wpx, Hband, seed=12, holes=()):
    """Capa de hojas en primer plano (fija en pantalla): masa oscura arriba, borde irregular con penachos y musgo
    que cuelgan, ramas gruesas cruzando y huecos (holes: (x px, r px)) por donde entra la luz."""
    rnd = random.Random(seed)
    cv = Canvas(Wpx, Hband + 50)
    edge = [Hband * .62 + 14 * math.sin(x / 47 + seed) + 9 * math.sin(x / 19) for x in range(Wpx)]
    for x in range(Wpx):
        for y in range(int(edge[x])):
            cv.put(x, y, Lp.LEAF[0] if y < edge[x] - 12 else Lp.LEAF[1])
    for k in range(3):                                   # ramas gruesas cruzando la copa
        y0 = rnd.uniform(.15, .45) * Hband
        Lp.limb(cv, -10, y0, rnd.uniform(-.15, .15), Wpx * rnd.uniform(.35, .6), 30, 14, rnd, 1.0, rnd.uniform(-.2, .2))
    for k in range(int(Wpx / 14)):                       # penachos encima de la masa (textura)
        Lp.tuft(cv, rnd.uniform(0, Wpx), rnd.uniform(0, Hband * .55), rnd, 1.0)
    for x in range(0, Wpx, 9):                           # penachos y musgo que cuelgan del borde
        y = edge[min(Wpx - 1, x)] + rnd.uniform(-8, 2)
        for j in range(rnd.randint(4, 7)):
            a = math.pi / 2 + rnd.uniform(-1.3, 1.3)
            frondlet(cv, x + rnd.uniform(-4, 4), y, a, rnd.uniform(10, 22), Lp.LEAF, .5, 1.8, None, rnd.choice((0, 1, 1)))
        if rnd.random() < .3: Lp.hang_moss(cv, x, y + 4, rnd, rnd.randint(14, 40))
    for (hx, hr) in holes:                               # huecos de luz
        for (x, y) in list(cv.px):
            if ((x - hx) / hr) ** 2 + ((y - Hband * .42) / (hr * .6)) ** 2 < 1: del cv.px[(x, y)]
    cv.outline(); cv.clean()
    return cv.img().convert('RGBA')

def armored_cone(im, x, y, arm_top=None, seed=3, s=1.0):
    """Cono gordo (3×4,5 u) envuelto en una armadura de ramas, en el suelo; arm_top: de dónde baja su rama."""
    rnd = random.Random(seed)
    w, h = int(48 * s), int(72 * s)
    cv = Canvas(w + 60, h + 60)
    ox, oy = cv.w / 2, cv.h - 30 - h / 2
    for yy in range(int(oy - h / 2), int(oy + h / 2) + 1):
        for xx in range(int(ox - w / 2), int(ox + w / 2) + 1):
            nx, ny = (xx - ox) / (w / 2), (yy - oy) / (h / 2)
            if nx * nx + ny * ny > 1: continue
            row = int((yy - oy + h / 2) // 8)
            lx = ((xx - ox + (4 if row % 2 else 0)) % 8) - 3.5
            ly = ((yy - oy + h / 2) % 8) - 3.5
            lit = -(nx * .6 + ny * .8)
            tone = 4 if lit > .55 else 3 if lit > .05 else 2 if lit > -.45 else 1
            cv.put(xx, yy, Lp.VEIN[max(0, tone - 2)] if (ly > 2.4 or abs(lx) > 3.2) else Lp.VEIN[tone])
    for k in range(9):                                   # armadura: ramas que lo envuelven en espiral
        a0 = rnd.uniform(0, 6.28)
        pts = []
        for t in (0, .33, .66, 1):
            yy = oy - h / 2 - 6 + t * (h + 10)
            xx = ox + math.sin(a0 + t * 3.2) * (w / 2 + 3) * (math.sqrt(max(0, 1 - ((yy - oy) / (h / 2 + 8)) ** 2)) + .15)
            pts.append((xx, yy))
        root(cv, pts, 9, 7, ROOTP)
    cv.outline(); cv.clean()
    spr = cv.img().convert('RGBA')
    sh = Image.new('RGBA', im.size, (0, 0, 0, 0))
    ImageDraw.Draw(sh).ellipse([x * U - w * .8, y * U - 10, x * U + w * .8, y * U + 10], fill=DARK + (110,))
    im.alpha_composite(sh)
    if arm_top is not None:                              # la rama que lo bajó, desde la copa
        arm = Canvas(im.width, im.height)
        top_y = arm_top * U
        L = (y * U - h - top_y)
        Lp.limb(arm, x * U - L * math.tan(.12), top_y, math.pi / 2 - .12, L / math.cos(.12), 26, 14, rnd)
        arm.outline()
        im.alpha_composite(arm.img().convert('RGBA'))
    im.alpha_composite(spr, (int(x * U - ox), int(y * U - cv.h + 30)))

def build_C():
    W, H = 160, TOP + 30
    im = A.room(A.rect_room(W, H, CORR, gaps=[ARENA]))
    rnd = random.Random(3)
    ferns = [('Fern_Leafy_Sway', 84), ('Fern_Tree_Sway', 84), ('Fern_Bent_Sway', 88), ('Fern_Young_Sway', 44)]
    mound(im, TC_X, TC_Y + .6)
    spots = []
    for (wx, th, tw) in WALLS:
        spots += [(wx - th / 2 - 2.2, CORR[1] + 4, 'sac'), (wx - th / 2 - 2.6, CORR[3] - 3, 'fern'),
                  (wx - th / 2 - 4.5, CORR[1] + 9, 'fern'), (wx - th / 2 - 2, CORR[1] + 12.5, 'sac')]
    for k in range(26):
        fx, fy = rnd.uniform(CORR[0] + 6, CORR[2] - 2), rnd.uniform(CORR[1] + 1.5, CORR[3])
        if any(abs(fx - wx) < th / 2 + 6 for (wx, th, tw) in WALLS): continue
        spots.append((fx, fy, 'fern'))
    for (fx, fy) in ((121, TOP + 24), (155, TOP + 22), (121.5, TOP + 6), (154, TOP + 5)): spots.append((fx, fy, 'fern'))
    for (x, y, k) in sorted(spots, key=lambda p: p[1]):
        if k == 'fern':
            n, w = rnd.choice(ferns); spr = A.frame(os.path.join(PROPS, n + '.png'), 0, w)
            if rnd.random() < .5: spr = spr.transpose(Image.FLIP_LEFT_RIGHT)
            put(im, spr, x, y)
        else: put(im, A.frame(os.path.join(PROPS, 'Methane_Sac_Idle.png'), 0, 24), x, y)
    for i, (wx, th, tw) in enumerate(WALLS):
        root_wall(im, wx, CORR[1] - 1.5, CORR[3] + 1, th, 10 + i, tw)
    # la copa cubre la sala poco a poco desde la pared 1 (sombra moteada) y del todo en la arena
    dapple(im, (30, 0, 160, H), lambda xu: max(0, min(1, (xu - 30) / 80)))
    mist = A.frame(os.path.join(PROPS, 'Methane_Mist.png'), 0, 48)
    for (mx, my) in ((TC_X - 12, TC_Y + 1.5), (TC_X + 11.5, TC_Y + 2)): put(im, mist, mx, my, 'center')
    # aviso del siguiente cono: sombra que crece + hojas que caen
    sh = Image.new('RGBA', im.size, (0, 0, 0, 0))
    ImageDraw.Draw(sh).ellipse([149 * U - 30, (TOP + 22) * U - 12, 149 * U + 30, (TOP + 22) * U + 12], fill=DARK + (120,))
    im.alpha_composite(sh)
    lv = Canvas(im.width, im.height)
    for k in range(9):
        frondlet(lv, 149 * U + rnd.uniform(-30, 30), (TOP + 22) * U - rnd.uniform(20, 120), rnd.uniform(0, 6.28), 6, Lp.LEAF, .3, 1, None, 0)
    im.alpha_composite(lv.img().convert('RGBA'))
    foot_px = int(TC_Y * U)
    tr, tox, ends = tall_trunk(foot_px + 6, foot_px)
    im.alpha_composite(tr, (int(TC_X * U - tox), 0))
    soil_over(im, [(TC_X * U + ex, foot_px + ey) for (ex, ey) in ends])
    armored_cone(im, 126.5, TOP + 21, arm_top=TOP + 3)
    A.player(im, 8, TOP + 15)
    A.player(im, 131, TOP + 23.5)
    return im

def lamina_final_C():
    im = build_C()
    ax0, ay0, ax1, ay1 = ARENA
    cam_cx, cam_cy = (ax0 + ax1) / 2 + .5, (ay0 + ay1) / 2 + .5
    cw, chh = 40, 22.5
    cx0, cy0 = cam_cx - cw / 2, cam_cy - chh / 2
    for (g, f) in (((cx0 + 9, cy0 + 1.5), (cx0 + 13, cy0 + 15)), ((cx0 + 30, cy0 + 1.5), (cx0 + 34, cy0 + 17))):
        light_shaft(im, g[0], g[1], f[0], f[1])
    band = canopy_band(int(cw * U) + 64, int(3.6 * U), holes=((9 * U + 32, 22), (30 * U + 32, 18)))
    im.alpha_composite(band, (int(cx0 * U) - 32, int(cy0 * U) - 14))
    x0, y0, x1, y1 = ax0 - 3, cy0 - .25, ax1 + 2, ay1 + 2
    crop = im.crop((x0 * U, int(y0 * U), x1 * U, y1 * U))
    s = 2
    big = crop.resize((crop.width * s, crop.height * s), Image.NEAREST)
    d = ImageDraw.Draw(big)
    P = lambda x, y: ((x - x0) * U * s, (y - y0) * U * s)
    w_, h_ = cw * U * s, chh * U * s
    X, Y = P(cam_cx, cam_cy)
    dashed(d, [(X - w_ / 2, Y - h_ / 2), (X + w_ / 2, Y - h_ / 2), (X + w_ / 2, Y + h_ / 2), (X - w_ / 2, Y + h_ / 2), (X - w_ / 2, Y - h_ / 2)], (255, 214, 90), 3, 10)
    def T(xy, t, col=CYAN, n=15):
        d.multiline_text(P(*xy), t, fill=col, font=A.font(n), stroke_width=3, stroke_fill=(10, 10, 14))
    T((cx0 + 15.5, cy0 + 1.2), 'capa de hojas en primer plano (fija en pantalla); el tronco se pierde en ella', (255, 240, 200))
    T((TC_X + 7, TC_Y - 4.6), 'tronco: 11 u en el pie, ~8 u arriba;\nse oscurece hacia la copa.\nVetas finas: solo aviso y golpe')
    T((TC_X + 8, TC_Y + 4.2), 'pie: montículo que se funde con el suelo +\nraíces Stigmaria medio enterradas en cruz')
    T((TC_X - 16, TC_Y - 6.5), 'paso por detrás (~10 u):\nel tronco se vuelve\ntranslúcido')
    T((119.5, TOP + 22.6), 'cono con armadura\nde ramas en el suelo:\nromperla y dañarlo', (255, 214, 90))
    T((146.5, TOP + 23), 'aviso: sombra que\ncrece + hojas que caen', RED)
    T((TC_X - 17.5, TC_Y + 2.6), 'metano junto al tronco: prendido,\nlo sacude y bajan los conos que queden', METH)
    T((cx0 + 14, cy0 + 16), 'sombra moteada de la copa\n+ rayos de luz por los huecos', (255, 240, 170))
    T((cam_cx + 6, cam_cy + 10.2), 'cámara alejada ×0,75 (40×22,5 u)', (255, 214, 90), 16)
    by_ = big.height - 30
    d.rectangle([20, by_, 20 + 32 * s, by_ + 8], fill=(240, 230, 200))
    d.text((28 + 32 * s, by_ - 6), '2 u = la rana (32 px)', fill=(240, 230, 200), font=A.font(16), stroke_width=3, stroke_fill=(10, 10, 14))
    _save(big, 'Lepidodendron ancestral · arena final ×2 · C · el tronco se pierde en la copa (concepto nuevo)',
          ['Tronco en el centro algo al norte, 11 u en el pie, sube hasta perderse en la capa de hojas: nunca se ve la copa. La sombra moteada y la luz por los huecos lo meten en la sala.',
           'Fase final: los conos están arriba, fuera de la vista, envueltos en ramas; el árbol baja uno para golpear y se queda en el suelo un rato (ventana de castigo). ×3.',
           'Al morir: se rasga la copa y entra la luz, el tronco se parte y queda un tocón hueco: la entrada a la sala de la recompensa.'],
          'vista_previa_escala_final_C.png')

def lamina_sala_C():
    im = build_C()
    band = canopy_band(46 * U, 4 * U, seed=5)
    im.alpha_composite(band, (int(114 * U), int((ARENA[1] - 1) * U)))
    d = ImageDraw.Draw(im)
    A.cam_box(d, 15, TOP + 14.5, 1, A.WHITE, 'cámara ×1 al empezar', 1)
    for i, (wx, th, tw) in enumerate(WALLS):
        d.text(((wx - 3) * U, (CORR[1] - 3.2) * U), f'pared {i + 1} (25 %)', fill=CYAN, font=A.font(15), stroke_width=3, stroke_fill=(10, 10, 14))
    d.text((40 * U, (CORR[3] + 1.6) * U), 'la sombra de la copa empieza en la pared 1 y se espesa hasta la arena →', fill=(255, 240, 170), font=A.font(15), stroke_width=3, stroke_fill=(10, 10, 14))
    _save(im, 'Lepidodendron ancestral · sala entera · C · el árbol cubre la sala poco a poco', notes_common(), 'vista_previa_escala_sala_C.png')

if __name__ == '__main__':
    for v in (sys.argv[1:] or ['A', 'B', 'C']):
        if v == 'C': lamina_sala_C(); lamina_final_C()
        else: lamina_sala(v); lamina_final(v)
