"""Lepidodendron ancestral · subtanda 6o: el desplome del árbol y la recompensa.

Idea del usuario (2026-11-02): al morir, la capa de hojas se rasga desde el centro y entra una columna de luz dorada;
el tronco se parte por encima del pie y cae RECTO HACIA EL OESTE (hacia la entrada), apoyado contra el borde norte:
SUSTITUYE A LA PARED NORTE desde el tocón hasta pasada la pared de raíces 3, donde revienta su copa. El mapa queda cambiado
para el resto de la partida (la brecha solo cambia el aspecto; el tronco tendido es un obstáculo con colisión). Del
tocón hueco se retiran las raíces de la cara sur y se abre un arco con luz dorada: la entrada a la cámara de la
recompensa. Por la columna de luz bajan flotando el corazón y la Espora morada.

Uso: python generar_desplome_lepidodendron.py [escala]
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import generar_escala_lepidodendron as E
from generar_escala_lepidodendron import *             # noqa: F401,F403
import generar_lepidodendron as G
from generar_lepidodendron import _h
from PIL import Image, ImageDraw

KP = .72                                               # altura → desplazamiento hacia arriba en pantalla (picada)
GOLD = [C(c) for c in ('#6a3e10', '#a86a1c', '#e0a234', '#ffd468', '#fff2c0')]
WOOD_IN = [C(c) for c in ('#2a1e14', '#4a3624', '#6e5438', '#957452', '#bc9c74')]   # madera de dentro (astillas)
RUBBLE = [C(c) for c in ('#1c1a16', '#2e2a22', '#46402f', '#625a42', '#857a5a')]
FALL_ANG = math.radians(180)                           # dirección de caída: recto al oeste (hacia la entrada)
BREAK_H = 4.5                                          # altura del tocón (u)
LOG_L = 34                                             # largo del tronco caído hasta la copa (u)
R0, R1 = 4.3, 2.8                                      # radio del tronco caído en la rotura y junto a la copa (u)

def fallen_log(L=LOG_L, r0=R0, r1=R1, dir_=FALL_ANG):
    """Tronco tendido en el suelo visto en picada (cilindro con cojinetes en rombo). Devuelve (imagen, origen): el
    origen es el punto del suelo donde empieza (la rotura, junto al tocón) en px de la imagen."""
    dx, dy = math.cos(dir_), math.sin(dir_)
    px_, py_ = -dy, dx                                  # perpendicular en el suelo
    pad = int((r0 * 2 + 4) * U)
    W = int(abs(dx) * L * U + 2 * pad); H = int(abs(dy) * L * U + 2 * pad + r0 * 2 * KP * U)
    ox = pad if dx >= 0 else W - pad; oy = H - pad if dy < 0 else pad + int(r0 * 2 * KP * U)
    zb = {}
    lx, ly, lz = -.45, -.35, .82
    for i in range(int(L * U * 2)):
        s = i / 2                                       # px a lo largo del eje
        t = s / (L * U)
        r = (r0 + (r1 - r0) * t) * U * (1 + .03 * math.sin(s / 13))
        circ = int(math.pi * r * 1.2) + 4
        for j in range(circ):
            th = -math.pi / 2 + (j / circ) * 2 * math.pi  # -90° = abajo (oculto), 90° = arriba
            c_, s_ = math.cos(th), math.sin(th)
            gx = ox + dx * s + px_ * r * c_; gy = oy + dy * s + py_ * r * c_
            h = r * (1 + s_)
            X, Y = int(gx), int(gy - KP * h)
            depth = gy + .4 * h
            if not (0 <= X < W and 0 <= Y < H): continue
            if zb.get((X, Y), (-1e9,))[0] >= depth: continue
            nx, ny, nz = px_ * c_, py_ * c_, s_
            lam = nx * lx + ny * ly + nz * lz
            a = s / 6.0; b = th * r / 5.0
            row = int(a) % 2
            fa, fb = a % 1, (b + (.5 if row else 0)) % 1
            dd = abs(fa - .5) * 2 * .55 + abs(fb - .5) * 2 * .45     # rombo
            tone = 3 if lam > .6 else 2 if lam > .2 else 1 if lam > -.2 else 0
            if dd > .82: tone = max(0, tone - 1)
            elif dd < .28 and lam > .35: tone = min(4, tone + 1)
            col = BARK_OLD[tone]
            if nz > .55 and ((int(s) // 3 + int(b * 2)) % 4 == 0): col = MOSS_OLD[1 if lam > .3 else 0]   # musgo encima
            zb[(X, Y)] = (depth, col)
    cv = Canvas(W, H)
    for p, (_, c) in zb.items(): cv.put(p[0], p[1], c)
    # cara de la rotura (astillada) mirando hacia el tocón
    ex, ey = ox, oy
    for k in range(18):
        a = k / 18 * math.pi
        for rr in range(int(r0 * U * .2), int(r0 * U)):
            x = ex + px_ * rr * math.cos(a) - dx * 3; y = ey + py_ * rr * math.cos(a) - KP * (r0 * U + rr * math.sin(a))
            if (int(x), int(y)) in cv.px: cv.put(x, y, WOOD_IN[2 + (k + rr // 4) % 3])
    cv.outline(); cv.clean()
    return cv.img().convert('RGBA'), (ox, oy), (dx, dy)

def crown(W, H, cx, cy, ang, seed=5):
    """La copa caída en el suelo: ramas en horquilla que se abren desde el final del tronco con penachos."""
    cv = Canvas(W, H)
    rnd = random.Random(seed)
    tips = []
    for k in range(5):
        a = ang + (k - 2) * .38 + rnd.uniform(-.1, .1)
        E.branch(cv, cx, cy, a, rnd.uniform(70, 95), 26, 3, tips, rnd, .5, .55)
    for (x, y, a) in tips: E.tuft(cv, x, y, rnd, 1.2)
    for k in range(40):                                 # hojas y ramitas sueltas alrededor
        x, y = cx + math.cos(ang) * rnd.uniform(40, 200) + rnd.uniform(-90, 90), cy + rnd.uniform(-80, 70)
        frondlet(cv, x, y, rnd.uniform(0, 6.3), rnd.uniform(6, 12), LEAF_OLD, .4, 1.4, None, 0)
    cv.outline(); cv.clean()
    return cv.img().convert('RGBA')

def stump(trunk_png):
    """El tocón: el pie del tronco cortado a BREAK_H u con borde astillado, hueco por dentro y un arco en la cara sur
    con luz dorada (las raíces se han retirado)."""
    im = Image.open(trunk_png).convert('RGBA')
    W, Hh = im.size
    foot = 352
    cut = foot - int(BREAK_H * U)
    rnd = random.Random(4)
    out = im.copy(); px = out.load()
    xs = [x for x in range(W) if im.getpixel((x, cut))[3]]
    x0, x1 = min(xs), max(xs)
    cxm, rx = (x0 + x1) / 2, (x1 - x0) / 2
    ry = rx * .32
    jag = {x: int(cut + (rnd.uniform(-6, 10) if (x // 5) % 2 else rnd.uniform(-16, 2))) for x in range(W)}
    for x in range(W):
        for y in range(0, Hh):
            if y < jag[x] - ry * math.sqrt(max(0, 1 - ((x - cxm) / rx) ** 2)) * 0: px[x, y] = (0, 0, 0, 0)
    d = ImageDraw.Draw(out)
    # boca del hueco: elipse oscura con anillo de madera astillada
    d.ellipse([cxm - rx, cut - ry, cxm + rx, cut + ry], fill=WOOD_IN[3])
    d.ellipse([cxm - rx + 7, cut - ry + 4, cxm + rx - 7, cut + ry - 3], fill=WOOD_IN[1])
    d.ellipse([cxm - rx + 13, cut - ry + 8, cxm + rx - 13, cut + ry - 5], fill=(14, 10, 8, 255))
    for k in range(22):                                 # astillas del borde
        a = k / 22 * 2 * math.pi
        x = cxm + math.cos(a) * rx * .97; y = cut + math.sin(a) * ry * .97
        Lh = rnd.randint(4, 16)
        d.polygon([(x - 3, y), (x + 3, y), (x + rnd.uniform(-2, 2), y - Lh)], fill=WOOD_IN[3 if math.sin(a) > 0 else 2])
    # arco de la entrada en la cara sur, con luz dorada dentro
    aw, ah = 64, 78
    ax0, ay1 = cxm - aw / 2, foot - 4
    arch = Image.new('RGBA', out.size, (0, 0, 0, 0)); ad = ImageDraw.Draw(arch)
    ad.rectangle([ax0, ay1 - ah + aw / 2, ax0 + aw, ay1], fill=GOLD[1])
    ad.ellipse([ax0, ay1 - ah, ax0 + aw, ay1 - ah + aw], fill=GOLD[1])
    ad.rectangle([ax0 + 8, ay1 - ah + aw / 2, ax0 + aw - 8, ay1], fill=GOLD[2])
    ad.ellipse([ax0 + 8, ay1 - ah + 8, ax0 + aw - 8, ay1 - ah + aw - 8], fill=GOLD[2])
    ad.rectangle([ax0 + 18, ay1 - ah + aw / 2, ax0 + aw - 18, ay1], fill=GOLD[3])
    ad.ellipse([ax0 + 18, ay1 - ah + 18, ax0 + aw - 18, ay1 - ah + aw - 18], fill=GOLD[3])
    out.alpha_composite(arch)
    for k in range(9):                                  # raíces retiradas a los lados del arco
        sx = -1 if k % 2 else 1
        x = cxm + sx * (aw / 2 + 2); y = ay1 - rnd.uniform(5, ah - 10)
        for j in range(rnd.randint(5, 12)):
            d.point((x + sx * j * .8, y + j * .3), fill=ROOTP[2])
    return out

def light_column(im, x, y_top, y_bot, w=6.0):
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    for k, (ww, a) in enumerate(((w, 30), (w * .7, 34), (w * .42, 40))):
        d.polygon([(x * U - ww * U * .4, y_top * U), (x * U + ww * U * .4, y_top * U),
                   (x * U + ww * U * .55, y_bot * U), (x * U - ww * U * .55, y_bot * U)], fill=(255, 226, 140, a))
    d.ellipse([x * U - w * U * .7, y_bot * U - w * U * .22, x * U + w * U * .7, y_bot * U + w * U * .22], fill=(255, 230, 150, 40))
    im.alpha_composite(lay)

def rubble(im, x0, x1, y, seed=8):
    """Escombros de la pared norte reventada: rocas y tierra caídas (con luz arriba) sobre el borde."""
    lay = Canvas(im.width, im.height)
    rnd = random.Random(seed)
    for k in range(int((x1 - x0) * 3.2)):
        x = rnd.uniform(x0, x1) * U; yy = (y + rnd.uniform(-2.2, 2.4)) * U
        r = rnd.uniform(3, 9)
        for py in range(int(yy - r), int(yy + r * .7)):
            for px in range(int(x - r), int(x + r)):
                d = ((px - x) / r) ** 2 + ((py - yy) / (r * .7)) ** 2
                if d > 1: continue
                lit = (px - x) * -.5 + (py - yy) * -1
                lay.put(px, py, RUBBLE[4] if lit > r * .4 else RUBBLE[3] if lit > 0 else RUBBLE[2] if lit > -r * .5 else RUBBLE[1])
    lay.outline(); lay.clean()
    im.alpha_composite(lay.img().convert('RGBA'))

# =================================================================================================
# Lámina de escala
# =================================================================================================
def build_after(show_shadow=False):
    W, H = 160, TOP + 30
    im = A.room(A.rect_room(W, H, CORR, gaps=[ARENA]))
    E.mound(im, TC_X, TC_Y + .6)
    E.dapple(im, (30, 0, 160, H), lambda xu: max(0, min(1, (xu - 30) / 80)) * .55)
    foot = (TC_X * U, TC_Y * U)
    lg, (lox, loy), (dx, dy) = fallen_log()
    bx, by = foot[0] - 6.2 * U, foot[1] - 3.5 * U        # la rotura: entera a la izquierda del tocón, contra el borde norte
    end = (bx + dx * LOG_L * U, by + dy * LOG_L * U)
    if show_shadow:
        sh = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(sh)
        px_, py_ = -dy, dx
        pts = [(bx + px_ * R0 * U, by + py_ * R0 * U), (end[0] + px_ * (R1 + 6) * U, end[1] + py_ * (R1 + 6) * U),
               (end[0] + dx * 9 * U, end[1] + dy * 9 * U),
               (end[0] - px_ * (R1 + 6) * U, end[1] - py_ * (R1 + 6) * U), (bx - px_ * R0 * U, by - py_ * R0 * U)]
        d.polygon(pts, fill=(8, 6, 12, 140))
        E.dashed(d, pts + [pts[0]], RED, 3, 10)
        im.alpha_composite(sh)
        return im, foot, (bx, by), end
    # copa caída sobre la pared norte del pasillo, escombros
    rubble(im, end[0] / U - 10, end[0] / U + 6, CORR[1] + .3)
    rubble(im, end[0] / U + 6, bx / U, CORR[1] - 1.2, seed=9)   # la pared aplastada detrás del tronco
    cw = Image.new('RGBA', (560, 420), (0, 0, 0, 0))
    cr = crown(560, 420, 300, 230, FALL_ANG + .0)
    im.alpha_composite(lg, (int(bx - lox), int(by - loy)))
    im.alpha_composite(cr, (int(end[0] - 300 + 20), int(end[1] - 230 - R1 * U * KP * 1.2)))   # la copa tapa el final del tronco
    st = stump(os.path.join(HERE, 'Tree', 'Lepido_Trunk.png'))
    im.alpha_composite(st, (int(foot[0] - 144), int(foot[1] - 352)))
    return im, foot, (bx, by), end

def lamina_escala():
    im, foot, brk, end = build_after()
    # copa rasgada encima de la arena + columna de luz sobre el tocón
    band = E.canopy_band(46 * U, 4 * U, seed=5, holes=((24 * U, 120), (8 * U, 26), (38 * U, 22)))
    light_column(im, TC_X, ARENA[1] - 1, TC_Y + 1.6, 7)
    im.alpha_composite(band, (int(114 * U), int((ARENA[1] - 1) * U)))
    # el corazón y la espora bajando por la luz (provisional)
    heart = A.frame(os.path.join(G.MT, 'shop', 'Item_HeartContainer_Idle.png'), 0, 16)
    im.alpha_composite(heart.resize((32, 32), Image.NEAREST), (int((TC_X - 1.5) * U), int((TC_Y + 3) * U)))
    sp = Canvas(16, 20); ellipse(sp, 8, 9, 5, 7, ramp('#8a3ad0')); sp.outline()
    for x in range(4, 13): sp.put(x, 16, GOLD[3]); sp.put(x, 17, GOLD[2])
    im.alpha_composite(sp.img().convert('RGBA').resize((32, 40), Image.NEAREST), (int((TC_X + 1) * U), int((TC_Y + 2.2) * U)))
    A.player(im, 131, TOP + 24.5)
    d = ImageDraw.Draw(im)
    def T(x, y, t, col=CYAN, n=15):
        d.multiline_text((x * U, y * U), t, fill=col, font=A.font(n), stroke_width=3, stroke_fill=(10, 10, 14))
    T(62, CORR[1] - 5.5, 'la copa revienta pasada la pared 3: escombros y ramas;\nno se pasa; queda así el resto de la partida', (255, 240, 200))
    T(92, TOP + 24.3, 'el tronco tendido en horizontal SUSTITUYE A LA PARED NORTE\n(obstáculo con colisión); el resto del pasillo queda libre para volver')
    T(139.5, TOP + 26.2, 'tocón hueco: arco con luz dorada\n= entrada a la cámara', (255, 214, 90))
    T(140.5, TOP - 3.6, 'copa rasgada: columna de luz\ndorada sobre el tocón', (255, 240, 170))
    T(10, CORR[1] + 1, 'las paredes de raíces ya se retiraron (cicatrices)', (180, 190, 170))
    for (wx, th, tw) in WALLS:
        d.text(((wx - 2) * U, (CORR[3] - 2) * U), 'cicatriz', fill=(150, 150, 130), font=A.font(13), stroke_width=2, stroke_fill=(10, 10, 14))
    G._save(im, 'Lepidodendron ancestral · 6o-1 escala del desplome · la sala DESPUÉS del combate (provisional: forma y tamaño)',
            ['El tronco se parte a ~4,5 u (tocón) y cae recto hacia la entrada: ~36 u tendido contra el borde norte (sustituye a la pared norte), de ~8,5 u de grosor en la rotura a ~5,5 u junto a la copa.',
             'Rana a la izquierda del tocón (2 u). El tronco, la copa, el tocón y los escombros son provisionales: solo composición y tamaño.'],
            'vista_previa_6o1_escala_sala.png')
    # arena: aviso (sombra que recorre el camino de la caída) y después
    sh_im, foot, brk, end = build_after(show_shadow=True)
    foot_px = int(TC_Y * U)
    tr, tox, ends = E.tall_trunk(foot_px + 6, foot_px)
    sh_im.alpha_composite(tr, (int(TC_X * U - tox), 0))
    band0 = E.canopy_band(46 * U, 4 * U, seed=5, holes=((24 * U, 60),))
    sh_im.alpha_composite(band0, (int(114 * U), int((ARENA[1] - 1) * U)))
    A.player(sh_im, 128, TOP + 21)
    d = ImageDraw.Draw(sh_im)
    d.multiline_text(((ARENA[0] + 1) * U, (TOP + 23.5) * U), 'aviso: la sombra del tronco que cae\nrecorre el borde norte hacia la entrada\n(última amenaza); el tronco cruje', fill=RED, font=A.font(15), stroke_width=3, stroke_fill=(10, 10, 14))
    after, _, _, _ = build_after()
    light_column(after, TC_X, ARENA[1] - 1, TC_Y + 1.6, 7)
    after.alpha_composite(band, (int(114 * U), int((ARENA[1] - 1) * U)))
    A.player(after, 131, TOP + 24.5)
    box = (int(100 * U), int((ARENA[1] - 1) * U), int(158 * U), int((ARENA[3] + 1) * U))
    a1, a2 = sh_im.crop(box), after.crop(box)
    out = Image.new('RGBA', (a1.width * 2 + 16, a1.height + 26), (16, 18, 16, 255))
    dd = ImageDraw.Draw(out)
    out.alpha_composite(a1, (0, 26)); out.alpha_composite(a2, (a1.width + 16, 26))
    dd.text((8, 4), 'aviso del desplome', fill=CYAN, font=A.font(16)); dd.text((a1.width + 24, 4), 'después', fill=CYAN, font=A.font(16))
    G._save(out, 'Lepidodendron ancestral · 6o-1 escala del desplome · la arena y el final del pasillo (×1)',
            ['Izquierda: aviso (sombra con borde rojo por donde caerá). Derecha: tocón con el arco, tronco tendido hacia la entrada en lugar de la pared norte, copa rasgada con la luz.'],
            'vista_previa_6o1_escala_arena.png')

# =================================================================================================
# 6o-1b · El tronco que cae (2D: gira sobre la bisagra del lado izquierdo, con la corteza del tronco aprobado)
# =================================================================================================
BRK = int(BREAK_H * U)                                 # altura de la rotura sobre el pie (px)
R_BRK = G.r_main(G.FOOT - BRK)                         # radio del tronco en la rotura (px)
TL = 34 * U                                            # largo del tronco hasta la copa
R_TOP = 2.9 * U
HINGE = (-R_BRK, -BRK)                                 # bisagra: lado izquierdo de la rotura (respecto al pie)
FINAL = (-6.2 * U, -4.5 * U)                           # dónde acaba el centro de la rotura (contra el borde norte)
SHIFT = (FINAL[0] - (HINGE[0] + R_BRK * 0), FINAL[1] - (HINGE[1] - R_BRK))   # desplazamiento al resbalar del tocón
FW, FH = 66 * U, 26 * U                                # celda de la caída: del pie −59 u a +7 u; de −22 u a +4 u
FFX, FFY = 59 * U, 22 * U                              # el pie dentro de la celda (pivote)
LGT = (-.6, -.8)                                       # luz (arriba a la izquierda, en pantalla)

def amin_of(b):
    """Perfil de la rotura (px por encima del corte) en la posición b a lo ancho: común al tronco y al tocón."""
    return (_h(int(b + 300) // 5, 3, 1) - .5) * 16 - (7 if _h(int(b + 300) // 3, 4, 1) < .2 else 0)

def r_at(a):
    base = R_BRK + (R_TOP - R_BRK) * (min(a, TL) / TL) ** .8
    fl = max(0., (a - (TL - 50)) / 50)
    return base * (1 + .26 * fl ** 2)                   # se ensancha donde nace la horquilla

def rot(x, y, ca, sa):
    """Gira (x, y) el ángulo α hacia la izquierda (lo de arriba va a la izquierda)."""
    return (x * ca + y * sa, -x * sa + y * ca)

def _bark(b, R, yl, c0, c1, vL, x, y, sc=1.0):
    """Color de la corteza (la fórmula de Lepido_Trunk) en un cilindro de radio R; b = distancia al eje, yl = altura
    a lo largo; vL = la luz de pantalla sobre el lado b > 0."""
    cw, ch = 16 * sc, 18 * sc                           # en las ramas, cojinetes más pequeños
    ph = max(-.999, min(.999, b / R)); th = math.asin(ph)
    s_ = .5 * b + .5 * th * R
    row = math.floor(yl / ch)
    col = math.floor((s_ + (cw / 2 if row % 2 else 0)) / cw)
    lx = (s_ + (cw / 2 if row % 2 else 0)) % cw - cw / 2
    ly = yl % ch - ch / 2
    dd = abs(lx) / (cw / 2) + abs(ly) / (ch / 2)
    vv = _h(row, col)
    lam = c0 + c1 * vL * ph + (vv - .5) * .14
    tone = 5 if lam > 1.0 else 4 if lam > .62 else 3 if lam > .22 else 2 if lam > -.12 else 1
    if ph * (1 if vL < 0 else -1) > .86: tone = max(tone, 2)        # luz rebotada en el borde en sombra
    eroded = vv < .06
    if dd > .86: c = BARK_OLD[max(0, tone - (2 if eroded else 3))]
    elif dd > .66 and ly > 0 and lx < 0 and not eroded: c = BARK_OLD[min(5, tone + 1)]
    elif dd > .66 and ly < 0 and lx > 0 and not eroded: c = BARK_OLD[max(0, tone - 1)]
    elif dd < .2: c = BARK_OLD[max(0, tone - 2)]
    else: c = BARK_OLD[tone]
    if dd > .86 and _h(int(b), int(yl), 7) < .06: c = G.MOSS[0 if tone < 3 else 1]
    if .955 < vv < .975 and dd < .5: c = G.LICHEN[0 if tone < 3 else 1 if tone < 5 else 2]
    gy = y - FFY                                        # penumbra de la copa (en pantalla: arriba, oscuro)
    gloom = max(0., min(1., (-gy - 150) / 170))
    if gloom > 0 and dither(min(1., gloom * 1.15), x, y): c = blend(c, G.GLOOM, .5 + .45 * gloom)
    return c

def crown_limbs(seed=5):
    """Ramas de la copa en coordenadas del tronco (a a lo largo, b a lo ancho): el tronco se abre en dos y cada rama
    vuelve a abrirse (Lepidodendron: horquillas). Empiezan dentro del ensanche, así no hay corte."""
    rnd = random.Random(seed)
    limbs, tips = [], []
    def rec(a0, b0, ang, L, w, depth):
        a1 = a0 + math.cos(ang) * L; b1 = b0 + math.sin(ang) * L
        w1 = w * (.66 if depth else .4)
        limbs.append((a0, b0, a1, b1, w, w1))
        if depth:
            for s_ in (-1, 1):
                rec(a1 - math.cos(ang) * w1 * .5, b1 - math.sin(ang) * w1 * .5, ang + s_ * rnd.uniform(.26, .42),
                    L * rnd.uniform(.72, .82), w1, depth - 1)
        else: tips.append((a1, b1, ang))
    rT = r_at(TL)
    for s_ in (-1, 1):
        rec(TL - 40, s_ * rT * .3, s_ * rnd.uniform(.16, .24), rnd.uniform(92, 102), rT * .62, 3)
    return limbs, tips

LIMBS, TIPS = crown_limbs()

def trunk_frame(alpha, shift=(0, 0), crown_on=True, seed=5):
    """El tronco por encima de la rotura, girado α sobre la bisagra y desplazado shift (px), con su copa en una sola
    pieza (las ramas nacen del ensanche del tronco y se dibujan con la misma corteza, sin contorno en la unión).
    Celda FW×FH, pivote = el pie del tronco (FFX, FFY)."""
    ca, sa = math.cos(alpha), math.sin(alpha)
    ux, uy = rot(0, -1, ca, sa); vx, vy = rot(1, 0, ca, sa)
    hx, hy = HINGE
    p0 = rot(R_BRK, 0, ca, sa)
    bx, by = FFX + hx + p0[0] + shift[0], FFY + hy + p0[1] + shift[1]
    c0, c1 = .62 - .3 * sa, 1.3 - .45 * sa
    def S(a, b): return (bx + ux * a + vx * b, by + uy * a + vy * b)
    def L_(x, y):
        dx, dy = x - bx, y - by
        return dx * ux + dy * uy, dx * vx + dy * vy
    cv = Canvas(FW, FH)
    # tronco
    corners = [S(0, -R_BRK), S(0, R_BRK), S(TL, -r_at(TL)), S(TL, r_at(TL))]
    xs = [c[0] for c in corners]; ys = [c[1] for c in corners]
    vL = vx * LGT[0] + vy * LGT[1]
    for y in range(max(0, int(min(ys)) - 12), min(FH, int(max(ys)) + 12)):
        for x in range(max(0, int(min(xs)) - 12), min(FW, int(max(xs)) + 12)):
            a, b = L_(x, y)
            if a > TL: continue
            amin = amin_of(b)
            if a < amin: continue
            R = r_at(max(0, a))
            if abs(b) > R: continue
            if a < amin + 4:                            # la madera de dentro en la cara de la rotura
                cv.put(x, y, WOOD_IN[3 if (int(b) // 3) % 2 else 2]); continue
            cv.put(x, y, _bark(b, R, BRK + a, c0, c1, vL, x, y))
    tips_s = []
    if crown_on:
        tx, ty = S(TL, 0)
        if -300 < tx < FW + 300 and ty > -300:
            for (a0, b0, a1, b1, w0, w1) in LIMBS:      # ramas: cilindros con la misma corteza
                la = math.hypot(a1 - a0, b1 - b0); da, db = (a1 - a0) / la, (b1 - b0) / la
                rx_, ry_ = -db, da                          # su lado derecho (en coordenadas del tronco)
                rsx, rsy = ux * rx_ + vx * ry_, uy * rx_ + vy * ry_
                vLl = rsx * LGT[0] + rsy * LGT[1]
                P0, P1 = S(a0, b0), S(a1, b1)
                for y in range(max(0, int(min(P0[1], P1[1]) - w0 - 2)), min(FH, int(max(P0[1], P1[1]) + w0 + 2))):
                    for x in range(max(0, int(min(P0[0], P1[0]) - w0 - 2)), min(FW, int(max(P0[0], P1[0]) + w0 + 2))):
                        a, b = L_(x, y)
                        t = ((a - a0) * da + (b - b0) * db) / la
                        if t < -.15 or t > 1: continue
                        d = (a - a0) * rx_ + (b - b0) * ry_
                        R = w0 + (w1 - w0) * max(0, t)
                        if abs(d) > R: continue
                        if (x, y) in cv.px and t < .1: continue
                        cv.put(x, y, _bark(d, R, BRK + a0 + t * la, c0, c1, vLl, x, y, max(.4, min(1, R / (R_TOP * 1.1)))))
            tips_s = [S(a, b) + (ang,) for (a, b, ang) in TIPS]
    cv.outline(); cv.clean()
    if tips_s:                                          # follaje espeso: penachos y matas de hojas de aguja
        fc = Canvas(FW, FH)
        rnd = random.Random(seed)
        for (x, y, ang) in tips_s:                      # matas grandes en las puntas
            for k in range(48):
                r_ = rnd.uniform(0, 1) ** .6 * 34; q = rnd.uniform(0, 2 * math.pi)
                frondlet(fc, x + math.cos(q) * r_, y + math.sin(q) * r_ * .8, rnd.uniform(0, 2 * math.pi), rnd.uniform(10, 18),
                         LEAF_OLD, .4, 1.9, None, 0 if math.sin(q) > 0 else 1)
            E.tuft(fc, x, y, rnd, 1.5)
        for (a0, b0, a1, b1, w0, w1) in LIMBS:          # y a lo largo de las ramas medianas y finas: copa espesa
            if w0 > 24: continue
            for f_ in (.35, .7, 1.0):
                x, y = S(a0 + (a1 - a0) * f_, b0 + (b1 - b0) * f_)
                for k in range(16):
                    r_ = rnd.uniform(0, 1) ** .6 * 22; q = rnd.uniform(0, 2 * math.pi)
                    frondlet(fc, x + math.cos(q) * r_, y + math.sin(q) * r_ * .8, rnd.uniform(0, 6.3), rnd.uniform(9, 15), LEAF_OLD, .4, 1.7, None, 0 if math.sin(q) > 0 else 1)
        fc.outline(); fc.clean()
        merge_cv(cv, fc)
    return cv

def merge_cv(dst, src):
    for p, c in src.px.items(): dst.px[p] = c

CRACK_PTS = [(4, 0), (1, 7), (5, 15), (0, 24), (3, 33), (-2, 42), (1, 52), (-1, 60), (2, 66)]   # la grieta (x, y desde arriba)

def stump2(crack=0.0, open_=0.0, pulse=0.0, intact=False):
    """El tocón MACIZO (pivote = el pie, celda del tronco 288×384): arriba, la madera rota (anillos, grietas, fibras
    largas en la bisagra de la izquierda). En la cara sur se forma una grieta de arriba abajo (crack 0..1) al caer el
    árbol y se abre ligeramente (open_ 0..1) dejando ver un hilo de luz dorada: la entrada (pulsar E) a la sala de
    dentro del tocón. pulse 0..1: latido morado tenue en los bordes de la grieta."""
    im = Image.open(os.path.join(HERE, 'Tree', 'Lepido_Trunk.png')).convert('RGBA')
    foot, cx = 352, 144
    cut = foot - BRK
    rx = R_BRK + 1; ry = rx * .3
    out = Image.new('RGBA', im.size, (0, 0, 0, 0)); px = out.load(); src = im.load()
    if intact:                                                      # aún unido: macizo hasta el perfil de la rotura
        for y in range(im.height):
            for x in range(im.width):
                p = src[x, y]
                if p[3] and y > cut - amin_of(x - cx): px[x, y] = p
        return out
    def mr(a): return 1 + .05 * math.sin(a * 5 + 1) + .03 * math.sin(a * 13)
    for y in range(im.height):                                      # el cuerpo, por debajo de la cara de la rotura
        for x in range(im.width):
            p = src[x, y]
            if not p[3] or y < cut - ry: continue
            u = (x - cx) / rx
            if y < cut and (u * u + ((y - cut) / ry) ** 2) > mr(math.atan2((y - cut) / ry, u)) ** 2: continue
            px[x, y] = p
    for y in range(int(cut - ry) - 1, int(cut + ry) + 2):          # la cara de la rotura: madera maciza
        for x in range(int(cx - rx * 1.1), int(cx + rx * 1.1) + 1):
            u = (x - cx) / rx; v = (y - cut) / ry
            a = math.atan2(v, u)
            d = math.sqrt(u * u + v * v) / mr(a)
            if d > 1: continue
            if d > .88: c = BARK_OLD[1 if v > 0 else 2]                # el anillo de corteza
            else:
                lam = -u * .5 - v * .7 + .1                         # luz arriba a la izquierda
                c = WOOD_IN[3] if lam > .35 else WOOD_IN[2] if lam > -.25 else WOOD_IN[1]
                rr = d * 9 + .6 * math.sin(a * 3 + d * 4)            # anillos de crecimiento
                if rr % 1 < .14: c = WOOD_IN[max(0, WOOD_IN.index(c) - 1)]
                if (x + int(y * 1.7)) % 5 == 0 and _h(x // 3, y, 2) < .5: c = WOOD_IN[max(0, WOOD_IN.index(c) - 1)]   # fibras
            px[x, y] = c
    rnd = random.Random(4)
    for k in range(6):                                              # grietas que salen del centro
        a = rnd.uniform(0, 2 * math.pi); r0 = rnd.uniform(.15, .3)
        for q in range(26):
            t = r0 + q / 26 * (.85 - r0)
            x = cx + math.cos(a) * rx * t; y = cut + math.sin(a) * ry * t
            if 0 <= int(x) < out.width: px[int(x), int(y)] = (14, 10, 8, 255)
            a += rnd.uniform(-.08, .08)
    for k in range(34):                                             # astillas y fibras rotas de pie
        a = k / 34 * 2 * math.pi + rnd.uniform(-.1, .1)
        u = math.cos(a)
        t = rnd.uniform(.55, .98)
        x = cx + u * rx * t * mr(a); y = cut + math.sin(a) * ry * t * mr(a)
        L = rnd.choice((2, 3, 4, 6, 9, 13)) * (2.2 if u < -.3 else .7) * (1.1 if math.sin(a) < 0 else .6)
        w = rnd.uniform(1.4, 3.0)
        for q in range(int(L)):
            ww = w * (1 - q / L)
            for xx in range(int(x - ww), int(x + ww) + 1):
                if 0 <= xx < out.width and 0 <= int(y - q) < out.height:
                    px[xx, int(y - q)] = WOOD_IN[4 if xx < x else 2] if q > L * .3 else WOOD_IN[3]
    if crack > 0:                                                   # la grieta de la cara sur
        top = cut + ry - 3
        pts = [(cx + x, top + y) for (x, y) in CRACK_PTS]
        seg = []
        for (p0, p1) in zip(pts, pts[1:]):
            n = int(abs(p1[1] - p0[1])) + 1
            for q in range(n): seg.append((p0[0] + (p1[0] - p0[0]) * q / n, p0[1] + (p1[1] - p0[1]) * q / n))
        L = int(len(seg) * crack)
        for i_, (x, y) in enumerate(seg[:L]):
            t = i_ / len(seg)
            hw_ = open_ * 4.5 * math.sin(math.pi * t) ** .6 + .5
            for xx in range(int(x - hw_ - 1), int(x + hw_) + 2):
                e = (xx - x) / max(hw_, .5)
                if abs(e) > 1.25: continue
                if abs(e) > 1:                              # labios de la grieta: corteza rota (izquierda con luz)
                    c = WOOD_IN[3] if e < 0 else (14, 10, 8, 255)
                    if pulse > 0 and _h(xx, int(y), 3) < pulse * .6: c = G.VEIN[2]
                elif open_ > 0:
                    c = GOLD[4] if abs(e) < .35 else GOLD[3] if abs(e) < .7 else GOLD[2]
                else: c = (12, 9, 7, 255)
                px[xx, int(y)] = c
        if open_ > 0:                                       # resplandor que se escapa por la grieta y en el suelo
            glow = Image.new('RGBA', out.size, (0, 0, 0, 0)); gd = ImageDraw.Draw(glow)
            for (x, y) in seg[:L:3]:
                gd.ellipse([x - 6, y - 4, x + 6, y + 4], fill=GOLD[3][:3] + (int(26 * open_),))
            gd.ellipse([cx - 26, foot - 8, cx + 26, foot + 10], fill=GOLD[3][:3] + (int(55 * open_),))
            gd.ellipse([cx - 12, foot - 4, cx + 12, foot + 5], fill=GOLD[4][:3] + (int(60 * open_),))
            out.alpha_composite(glow)
    return out

def gold_column(h=11 * U, w_top=4.5 * U, w_bot=7 * U, k=0):
    """Columna de luz dorada que baja por el rasgón de la copa hasta el tocón (translúcida, bordes en trama, motas)."""
    W = int(w_bot + 16); im = Image.new('RGBA', (W, int(h)), (0, 0, 0, 0)); px = im.load()
    cx = W / 2
    for y in range(int(h)):
        t = y / h
        hw_ = (w_top + (w_bot - w_top) * t) / 2
        for x in range(W):
            e = abs(x - cx) / hw_
            if e > 1: continue
            a = (1 - e) * 60 + 18 + 10 * math.sin(x / 7 + k * .8 + y / 40)
            if e > .7 and not dither((1 - e) / .3, x, y): continue
            px[x, y] = (255, 228, 150, int(a))
    rnd = random.Random(k + 3)
    for j in range(14):                                 # motas que flotan en la luz
        x = cx + rnd.uniform(-.4, .4) * w_bot; y = (rnd.uniform(0, 1) * h - k * 3) % h
        px[int(x), int(y)] = (255, 246, 210, 220)
    return im

def cloud(im, centers, t, pal, alpha=230, seed=1, rise=2.5):
    """Bocanadas con volumen (luz arriba a la izquierda) que se abren y se deshacen en trama. centers: [(x, y, r)]."""
    px = im.load()
    dens = 1 - t / 7
    for (x0, y0, r0) in centers:
        rr = r0 * (1 + t * .18); py_ = y0 - t * rise
        for y in range(int(py_ - rr), int(py_ + rr) + 1):
            for x in range(int(x0 - rr), int(x0 + rr) + 1):
                if not (0 <= x < im.width and 0 <= y < im.height): continue
                nx, ny = (x - x0) / rr, (y - py_) / (rr * .8)
                dd = nx * nx + ny * ny
                if dd > 1 or (BAYER[y % 4][x % 4] + .5) / 16 > dens * (1.25 - dd * .5): continue
                lam = -(nx + ny) * .8
                c = pal[2] if lam > .45 else pal[1] if lam > -.25 else pal[0]
                px[x, y] = c[:3] + (alpha,)

DUST3 = [C(c) for c in ('#4a4636', '#6e6850', '#948c6c')]
SPORE3 = [C(c) for c in ('#4a2a6a', '#7046a0', '#a07ad0')]

# --- mundo y cámara para las láminas ---------------------------------------------------------------------------
def world_base():
    W, H = 160, TOP + 30
    im = A.room(A.rect_room(W, H, CORR, gaps=[ARENA]))
    fx, fy = int(TC_X * U), int(TC_Y * U)
    sf = lambda f, n, w: Image.open(os.path.join(HERE, f, n + '.png')).convert('RGBA').crop((0, 0, w, Image.open(os.path.join(HERE, f, n + '.png')).height))
    for name, w, px_, py_ in (('Lepido_BackRoots', 520, 260, 196), ('Lepido_MoundShadow', 480, 240, 96), ('Lepido_Mound', 480, 240, 96)):
        im.alpha_composite(sf('Tree', name, w), (fx - px_, fy - py_))
    scar = sf('Walls', 'Lepido_Wall3_Scar', 140)
    im.alpha_composite(scar, (94 * U - 70, CORR[3] * U - 341))
    E.dapple(im, (30, 0, 160, H), lambda xu: max(0, min(1, (xu - 30) / 80)))
    return im, fx, fy

SKYSET = set(tuple(c[:3]) for c in G.SKY)

def canopy_over(im, cam_x0, cam_y0, holes_arena, shake=0.0, k=0, front=None):
    """Capa de hojas fija arriba de la pantalla: la de la arena (704 px) y la tira media del pasillo (desde la pared 3).
    front: imagen (del tamaño del mundo) de lo que sube por encima de la copa (el tronco): se ve delante del cielo en
    los huecos."""
    cy = cam_y0 - 10
    strip = A.frame(os.path.join(HERE, 'Canopy', 'Canopy_StripMid_Sway.png'), k % 8, 256)
    x = 94 * U
    while x < (TC_X - 20) * U - 32:
        im.alpha_composite(strip, (x, cam_y0 - 10)); x += 256
    can = G.canopy(G.ARENA_W, G.ARENA_H, holes=holes_arena, sway=.16 * math.sin(k), shake=shake).img().convert('RGBA')
    ox = int((TC_X - 20) * U - 32)
    im.alpha_composite(can, (ox, cy))
    if front is not None:
        cp, fp, ip = can.load(), front.load(), im.load()
        for y in range(can.height):
            for x in range(can.width):
                c = cp[x, y]
                if c[3] and tuple(c[:3]) in SKYSET:
                    f = fp[x + ox, y + cy]
                    if f[3] > 200: ip[x + ox, y + cy] = f

CRACK_MAX = 5                                           # grados que se inclina mientras se abre la grieta

def crack_stump(p):
    """El tocón aún unido mientras cruje (celda del tronco, pivote = el pie): macizo, y la grieta abierta en cuña desde
    la derecha hasta la posición p (0..1) — negra por dentro, con fibras claras que aún unen y astillas."""
    out = stump2(intact=True)
    px = out.load()
    cx, cut = 144, 352 - BRK
    hx = cx - R_BRK
    sa = math.sin(math.radians(CRACK_MAX * p))
    front = cx + R_BRK - p * 2 * R_BRK                  # hasta dónde ha llegado la grieta (de derecha a izquierda)
    for x in range(int(hx), int(cx + R_BRK) + 1):
        if x < front: continue
        y0 = cut - amin_of(x - cx)
        gap = (x - hx) * sa
        for y in range(int(y0 - gap) - 1, int(y0) + 1):
            if 0 <= y < out.height:
                fib = (x % 4 == 0) and _h(x, 7, 7) < .55
                px[x, y] = WOOD_IN[3] if fib else (12, 9, 7, 255)
        if 0 <= int(y0) + 1 < out.height: px[x, int(y0) + 1] = WOOD_IN[1]     # canto de abajo de la grieta
    return out

def front_of(world, cell, fx, fy):
    """Imagen del tamaño del mundo con solo el tronco (para verlo delante del cielo en los huecos de la copa)."""
    f = Image.new('RGBA', world.size, (0, 0, 0, 0))
    f.alpha_composite(cell.img().convert('RGBA'), (fx - FFX, fy - FFY))
    return f

def lamina_muestra():
    base, fx, fy = world_base()
    cam_y0 = int((TC_Y - 10.25) * U)
    arena_x0 = int((TC_X - 20) * U)
    frog = A.frame(FROG, 0, 32)
    st_closed, st_open = stump2(crack=.35), stump2(crack=1.0, open_=1.0, pulse=.6)
    def put_fall(im, fr): im.alpha_composite(fr.img().convert('RGBA'), (fx - FFX, fy - FFY))
    def put_st(im, st): im.alpha_composite(st, (fx - 144, fy - 352))
    def ease(t): return t * t * (3 - 2 * t)
    panels = []
    # 1. grieta: las vetas ya apagadas, se inclina 4° y la grieta se abre por la derecha; la sombra avisa
    im = base.copy()
    sh = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(sh)
    pts = [(fx - 4 * U, fy - 9 * U), (fx - 52 * U, fy - 10 * U), (fx - 52 * U, fy + .2 * U), (fx - 4 * U, fy + .5 * U)]
    d.polygon(pts, fill=(8, 6, 12, 130)); E.dashed(d, pts + [pts[0]], RED, 3, 10)
    im.alpha_composite(sh)
    put_st(im, crack_stump(1.0))
    tf = trunk_frame(math.radians(CRACK_MAX)); put_fall(im, tf)
    im.alpha_composite(frog, (int(fx - 6 * U), int(fy + 5 * U)))
    canopy_over(im, arena_x0, cam_y0, ((352, 84), G.HOLES[0], G.HOLES[1]), shake=1.0, k=0, front=front_of(im, tf, fx, fy))
    panels.append(('las vetas se apagan; grieta de derecha a izquierda; la sombra avisa', im.crop((arena_x0, cam_y0, arena_x0 + 640, cam_y0 + 360))))
    # 2. cae (α 50°), la copa se rasga
    im = base.copy()
    put_st(im, stump2(crack=.2))
    t = ease(.35)
    tf = trunk_frame(math.radians(50), (SHIFT[0] * t * .3, SHIFT[1] * t * .3)); put_fall(im, tf)
    im.alpha_composite(frog, (int(fx - 6 * U), int(fy + 5 * U)))
    canopy_over(im, arena_x0 - 6 * U, cam_y0, ((352, 90), (296, 62), (240, 46), G.HOLES[0], G.HOLES[1]), shake=1.0, k=3, front=front_of(im, tf, fx, fy))
    panels.append(('cae (la cámara lo sigue); la copa se rasga', im.crop((arena_x0 - 6 * U, cam_y0, arena_x0 - 6 * U + 640, cam_y0 + 360))))
    # 3. golpe: la copa revienta la pared norte, polvo y esporas
    im = base.copy()
    put_st(im, stump2(crack=1.0, open_=.35))
    rubble(im, (fx + FINAL[0] - TL - 10 * U) / U, (fx + FINAL[0] - TL + 6 * U) / U, CORR[1] + .3)
    put_fall(im, trunk_frame(math.radians(90), SHIFT))
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0))
    ex = fx + FINAL[0] - TL
    rnd = random.Random(2)
    for j in range(22):                                 # polvo a ras de suelo por el borde sur del tronco
        x = ex + j * 26 + rnd.uniform(-8, 8)
        a_ = max(0, (fx + FINAL[0]) - x)
        ys = fy + FINAL[1] + r_at(min(TL, a_)) - 2
        if rnd.random() < .45: continue
        cloud(lay, [(x + rnd.uniform(-10, 10), ys + rnd.uniform(-4, 6), rnd.uniform(5, 11))], 3, DUST3, 150)
    for j in range(9):                                  # polvo y tierra donde la copa revienta la pared
        cloud(lay, [(ex - 6 * U + rnd.uniform(-70, 70), CORR[1] * U + rnd.uniform(-20, 30), rnd.uniform(6, 12))], 3, DUST3, 160)
    sp = lay.load()
    for j in range(160):                                # esporas moradas en motas que suben (solo ambiente)
        x = int(ex + rnd.uniform(-4 * U, TL + 2 * U)); y = int(fy + FINAL[1] + rnd.uniform(-7 * U, 4 * U))
        if 0 <= x < lay.width and 0 <= y < lay.height: sp[x, y] = SPORE3[rnd.randrange(3)][:3] + (220,)
    im.alpha_composite(lay)
    cx0 = int(ex - 26 * U)
    canopy_over(im, cx0, cam_y0, ((352, 90), (290, 60), (230, 46), (176, 34), G.HOLES[1]), shake=1.0, k=5)
    panels.append(('golpe: la copa revienta la pared norte; polvo y esporas', im.crop((cx0, cam_y0, cx0 + 640, cam_y0 + 360))))
    # 4. después: tocón con el arco abierto, columna de luz, la copa rasgada, el corazón y la espora bajan
    im = base.copy()
    rubble(im, (fx + FINAL[0] - TL - 10 * U) / U, (fx + FINAL[0] - TL + 6 * U) / U, CORR[1] + .3)
    put_fall(im, trunk_frame(math.radians(90), SHIFT))
    put_st(im, st_open)
    col = gold_column()
    im.alpha_composite(col, (fx - col.width // 2, int(fy - 10 * U)))
    heart = A.frame(os.path.join(G.MT, 'shop', 'Item_HeartContainer_Idle.png'), 0, 16)
    im.alpha_composite(heart, (fx - 26, int(fy - 3 * U)))
    sp = Canvas(16, 20); ellipse(sp, 8, 9, 5, 7, ramp('#8a3ad0')); sp.outline()
    for x in range(4, 13): sp.put(x, 16, GOLD[3]); sp.put(x, 17, GOLD[2])
    im.alpha_composite(sp.img().convert('RGBA'), (fx + 10, int(fy - 4.6 * U)))
    im.alpha_composite(frog, (int(fx - 6 * U), int(fy + 5 * U)))
    canopy_over(im, arena_x0, cam_y0, ((352, 90), (290, 60), (230, 46), (176, 34), G.HOLES[1]), k=1)
    panels.append(('después: la grieta del tocón se abre; luz, corazón y espora bajan', im.crop((arena_x0, cam_y0, arena_x0 + 640, cam_y0 + 360))))
    # lámina
    det = [('tocón: la grieta se forma al caer', st_closed.crop((40, 220, 248, 384))), ('la grieta abierta (pulsar E para entrar)', st_open.crop((40, 220, 248, 384)))]
    cw_, ch_ = 640, 360
    out = Image.new('RGBA', (cw_ * 2 + 16, (ch_ + 26) * 2 + 26 + 184 * 2 + 10), (16, 18, 16, 255))
    dd = ImageDraw.Draw(out)
    for j, (nm, f) in enumerate(panels):
        x, y = (j % 2) * (cw_ + 16), (j // 2) * (ch_ + 26)
        out.alpha_composite(f, (x, y + 24)); dd.text((x + 6, y + 4), nm, fill=CYAN, font=A.font(16))
    y0 = (ch_ + 26) * 2
    x = 0
    for nm, f in det:
        big = f.resize((f.width * 2, f.height * 2), Image.NEAREST)
        out.alpha_composite(big, (x, y0 + 24)); dd.text((x + 6, y0 + 4), nm, fill=CYAN, font=A.font(16)); x += big.width + 16
    G._save(out, 'Lepidodendron ancestral · 6o-1b muestra del desplome: momentos clave (×1) y el tocón (×2)',
            ['El tronco gira sobre la bisagra de la izquierda con la corteza del tronco aprobado; al final resbala del tocón y queda contra el borde norte (sustituye a la pared).',
             'Corazón y espora: provisionales (la espora definitiva va en la 6o-2). Polvo y esporas: solo ambiente.'],
            'vista_previa_6o1_muestra.png')

# =================================================================================================
# 6o-1b · Hojas de la cinemática
# =================================================================================================
DEATH = os.path.join(HERE, 'Death')
GIFD = os.path.join(HERE, 'gif')
VEIN_SEQ = [('hit', 0), ('rest', 0), (None, 0), ('hit', 1), (None, 0), ('rest', 0), (None, 0), ('hit', 1), (None, 0), (None, 0)]
VEIN_MS = 70
CRACK_P = (.2, .4, .6, .8, 1.0)
CRACK_MS = 180
# la caída: un árbol tan grande arranca casi suspendido y va acelerando (~1,8 s)
FALL_A = (7, 10, 14, 19, 25, 32, 40, 49, 59, 69, 79, 87)
FALL_MS = (220, 200, 180, 170, 160, 150, 140, 130, 120, 110, 100, 90)
NF = len(FALL_A)
LAND = ((7, 100), (-3, 130), (0, 180))                  # rebote (px hacia abajo) y ms al tocar el suelo: golpe pesado
TORN = ((352, 90), (290, 60), (230, 46), (176, 34), (512, 22))
SH_X0, SH_Y0, SH_W, SH_H = -54 * U, -10.5 * U, 53 * U, 11 * U   # sombra de aviso (respecto al pie)
DU_X0, DU_Y0, DU_W, DU_H = -56 * U, -12 * U, 55 * U, 14 * U     # polvo a lo largo del tronco
CRASH = (FINAL[0] - TL - 3 * U, FINAL[1] - 1 * U)        # donde la copa revienta la pared (respecto al pie)

def ease(t): return t * t * (3 - 2 * t)

def fall_shift(a_deg):
    t = ease(max(0., min(1., (a_deg - 30) / 60)))
    return (SHIFT[0] * t, SHIFT[1] * t)

def _cimg(cv): return cv.img().convert('RGBA') if hasattr(cv, 'img') else cv

def death_veins():
    out = []
    for mode, k in VEIN_SEQ:
        out.append(G.veins(mode, k).img().convert('RGBA') if mode else Image.new('RGBA', (G.TCW, G.TCH), (0, 0, 0, 0)))
    return out

def death_crack():
    """El árbol entero mientras cruje (celda del tronco 288×384, pivote (144, 352)): sustituye a Lepido_Trunk."""
    out = []
    for p_ in CRACK_P:
        im = crack_stump(p_)
        tf = _cimg(trunk_frame(math.radians(CRACK_MAX * p_), crown_on=False)).crop((FFX - 144, FFY - 352, FFX + 144, FFY + 32))
        im.alpha_composite(tf)
        out.append(im)
    return out

def death_fall():
    """El tronco suelto que cae (sin el tocón): FALL_A y luego el golpe con rebote. Celda FW×FH, pivote = el pie."""
    out = [_cimg(trunk_frame(math.radians(a), fall_shift(a))) for a in FALL_A]
    for (dy, ms) in LAND: out.append(_cimg(trunk_frame(math.radians(90), (SHIFT[0], SHIFT[1] + dy))))
    return out

def stump_break():
    n = len(FALL_A) + len(LAND)
    return [stump2(crack=min(.6, (i + 1) / len(FALL_A) * .6)) for i in range(n)]

def stump_open(n=8):
    return [stump2(crack=.6 + .4 * min(1, (k + 1) / 4), open_=ease(max(0, (k - 1) / (n - 2))), pulse=.3 * k / n) for k in range(n)]

def stump_idle(n=6):
    out = []
    for k in range(n):
        ph = .5 + .5 * math.sin(2 * math.pi * k / n)
        out.append(stump2(crack=1.0, open_=.85 + .15 * ph, pulse=.25 + .5 * ph))
    return out

def canopy_tear():
    """La capa de hojas de la arena se rasga: primero encima del tronco (5 cuadros, mientras cruje) y luego hacia la
    izquierda por donde pasa al caer (un cuadro por cuadro de Lepido_Death_Fall), con sacudida violenta."""
    out = []
    for k in range(5 + NF):
        a = FALL_A[k - 5] if k >= 5 else 0
        holes = [G.HOLES[0], G.HOLES[1], (352, 30 + min(60, k * 7))]
        if a > 30: holes.append((290, int(30 + min(30, (a - 30) * .7))))
        if a > 50: holes.append((230, int(26 + min(20, (a - 50) * .7))))
        if a > 65: holes.append((176, 34))
        out.append(_cimg(G.canopy(G.ARENA_W, G.ARENA_H, holes=holes, sway=k * 1.7 + .3, shake=1.0)))
    return out

def canopy_torn():
    return [_cimg(G.canopy(G.ARENA_W, G.ARENA_H, holes=TORN, sway=.16 * math.sin(2 * math.pi * k / 8))) for k in range(8)]

def shadow_alpha(k):
    return math.radians(CRACK_MAX * CRACK_P[k]) if k < 5 else math.radians(FALL_A[k - 5])

def fx_fall_shadow(k):
    """La sombra del tronco en el suelo (sin borde: solo sombra). Se alarga hacia la entrada a medida que el tronco se
    inclina (largo = lo que el tronco ya cubre en planta) y se oscurece al acercarse al suelo; al final, la mancha de
    la copa con huecos de luz. Cuadros 0-4 con Lepido_Death_Crack, 5-16 con Lepido_Death_Fall."""
    im = Image.new('RGBA', (SH_W, SH_H), (0, 0, 0, 0)); px = im.load()
    ox, oy = -SH_X0, -SH_Y0                              # el pie dentro de la celda
    sa = math.sin(shadow_alpha(k))
    xr = ox - 3 * U
    L = (TL + 2 * U) * sa                               # lo que cubre en planta
    xl = xr - L
    al = 45 + 125 * sa ** 1.5
    cy = oy - 4.5 * U
    for y in range(SH_H):
        for x in range(int(max(0, xl - 9 * U * sa)), int(xr) + 1):
            t = (xr - x) / max(1, TL)
            hw = (4.6 - 1.9 * min(1, t)) * U * (.6 + .4 * sa)          # grosor del tronco en el suelo
            dy = abs(y - cy)
            if x >= xl:
                e = dy / hw
                if e > 1.25: continue
                a = al if e < .85 else al * (1.25 - e) / .4
                if e > .85 and not dither((1.25 - e) / .4, x, y): continue
            else:                                       # la copa: mancha más ancha con huecos de luz
                cr = 8.5 * U * sa
                d = math.hypot((x - (xl - cr * .45)) / cr, (y - cy) / (cr * .95))
                if d > 1: continue
                if _h(x // 5, y // 5, 3) < .22: continue
                a = al * .85 * (1 - d * .4)
                if d > .75 and not dither((1 - d) / .25, x, y): continue
            px[x, y] = (8, 6, 12, int(a))
    return im

def fx_splinters(k, n=6):
    """Astillas que saltan de la rotura al soltarse el tronco (160×112, centro = el centro de la rotura)."""
    im = Image.new('RGBA', (160, 112), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    rnd = random.Random(21)
    for j in range(22):
        a = rnd.uniform(-math.pi * .95, -math.pi * .05); v = rnd.uniform(3, 9)
        x = 80 + math.cos(a) * v * k * 1.6 + 40 * (j % 2); y = 56 + math.sin(a) * v * k * 1.2 + .9 * k * k
        if k == n - 1 and j % 2: continue
        L = rnd.uniform(2, 5)
        d.line([x, y, x + math.cos(a + 1) * L, y + math.sin(a + 1) * L], fill=WOOD_IN[3 + (j % 2)])
    if k < 3:
        cloud(im, [(80 + j * 14 - 20, 60 - k * 3, 7 + k * 2) for j in range(4)], k, DUST3, 170)
    return im

def fx_trunk_dust(k, n=8):
    """Polvo a ras de suelo por el borde sur del tronco al caer, y motas de esporas moradas que suben (ambiente)."""
    im = Image.new('RGBA', (DU_W, DU_H), (0, 0, 0, 0))
    ox, oy = -DU_X0, -DU_Y0
    rnd = random.Random(2)
    for j in range(26):
        x = ox + FINAL[0] - TL + j * 22 + rnd.uniform(-8, 8)
        a_ = max(0, (ox + FINAL[0]) - x)
        ys = oy + FINAL[1] + r_at(min(TL, a_)) - 2
        if rnd.random() < .55: continue
        cloud(im, [(x + rnd.uniform(-10, 10) - k * rnd.uniform(0, 2), ys + rnd.uniform(-6, 8), rnd.uniform(3.5, 7))], min(7, k + 2), DUST3, 115)
    px = im.load()
    for j in range(140):
        x = int(ox + FINAL[0] - TL + rnd.uniform(-2 * U, TL)); y0 = oy + FINAL[1] + rnd.uniform(-5 * U, 4 * U)
        y = int(y0 - k * rnd.uniform(1, 4)); a = int(230 * (1 - k / n))
        if 0 <= x < DU_W and 0 <= y < DU_H and rnd.random() < .8: px[x, y] = SPORE3[rnd.randrange(3)][:3] + (a,)
    return im

def fx_crown_crash(k, n=8):
    """La copa revienta la pared norte: rocas y terrones que saltan, polvo y hojas (320×256, centro = el golpe)."""
    im = Image.new('RGBA', (320, 256), (0, 0, 0, 0))
    rnd = random.Random(31)
    cv = Canvas(320, 256)
    for j in range(22):                                 # piedras angulosas y terrones en arco
        a = rnd.uniform(-math.pi * .95, -math.pi * .05); v = rnd.uniform(4, 11)
        x = 160 + math.cos(a) * v * k * 1.4; y = 128 + math.sin(a) * v * k + 1.1 * k * k
        if k > 5 and j % 2: continue
        r = rnd.uniform(1.6, 3.6); spin = rnd.uniform(0, 6.3) + k * rnd.uniform(-.6, .6)
        nv = rnd.choice((3, 4, 5))
        verts = [(math.cos(spin + q * 2 * math.pi / nv) * r * rnd.uniform(.7, 1.2), math.sin(spin + q * 2 * math.pi / nv) * r * rnd.uniform(.7, 1.2)) for q in range(nv)]
        for yy in range(int(y - r * 1.3) - 1, int(y + r * 1.3) + 2):
            for xx in range(int(x - r * 1.3) - 1, int(x + r * 1.3) + 2):
                px_, py_ = xx - x, yy - y
                inside = all(((verts[(q + 1) % nv][0] - verts[q][0]) * (py_ - verts[q][1]) - (verts[(q + 1) % nv][1] - verts[q][1]) * (px_ - verts[q][0])) >= 0 for q in range(nv)) or \
                         all(((verts[(q + 1) % nv][0] - verts[q][0]) * (py_ - verts[q][1]) - (verts[(q + 1) % nv][1] - verts[q][1]) * (px_ - verts[q][0])) <= 0 for q in range(nv))
                if inside:
                    lit = px_ * -.5 + py_ * -1
                    cv.put(xx, yy, RUBBLE[4] if lit > r * .25 else RUBBLE[3] if lit > -r * .3 else RUBBLE[2])
    cv.outline(RUBBLE[0])
    cloud(im, [(160 + rnd.uniform(-90, 90), 128 + rnd.uniform(-40, 30), rnd.uniform(9, 16)) for _ in range(9)], min(7, k + 1), DUST3, 190)
    im.alpha_composite(cv.img().convert('RGBA'))
    lv = Canvas(320, 256)
    for j in range(16):                                 # hojas que vuelan
        x = 160 + rnd.uniform(-120, 120); y = 128 + rnd.uniform(-90, 40) + k * 4
        frondlet(lv, x, y, rnd.uniform(0, 6.3) + k * .4, rnd.uniform(5, 9), LEAF_OLD, .4, 1.2, None, 0)
    im.alpha_composite(lv.img().convert('RGBA'))
    return im

def wall_rubble():
    """Escombros de la pared norte reventada (448×128, centro = el golpe): rocas y tierra caídas sobre el borde."""
    im = Image.new('RGBA', (448, 128), (0, 0, 0, 0))
    rubble(im, 0, 448 / U, 128 / 2 / U, seed=8)
    return im

def shroom_off(kind):
    import generar_pie_lepidodendron as PIE
    im = PIE.shroom_cluster(kind, 0.0)
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a:
                l = int(.3 * r + .59 * g + .11 * b)
                px[x, y] = (int(l * .9 + r * .1), int(l * .9 + g * .1), int(l * .85 + b * .1), a)
    return im

def build_death():
    out = {}
    out['Lepido_Death_Veins'] = death_veins()
    out['Lepido_Death_Crack'] = death_crack()
    out['Lepido_Death_Fall'] = death_fall()
    out['Lepido_FallenTrunk'] = [out['Lepido_Death_Fall'][-1]]
    out['Lepido_Stump_Break'] = stump_break()
    out['Lepido_Stump_Open'] = stump_open()
    out['Lepido_Stump_Idle'] = stump_idle()
    out['Canopy_Arena_Tear'] = canopy_tear()
    out['Canopy_Arena_Torn_Sway'] = canopy_torn()
    out['Fx_FallShadow'] = [fx_fall_shadow(k) for k in range(5 + NF)]
    out['Fx_TrunkSplinters'] = [fx_splinters(k) for k in range(6)]
    out['Fx_TrunkDust'] = [fx_trunk_dust(k) for k in range(8)]
    out['Fx_CrownCrash'] = [fx_crown_crash(k) for k in range(8)]
    out['Lepido_WallRubble'] = [wall_rubble()]
    out['Fx_GoldColumn'] = [gold_column(k=k) for k in range(6)]
    for kd in 'ABC': out[f'Lepido_Shroom{kd}_Off'] = [shroom_off(kd)]
    return out

def piezas_death():
    out = build_death()
    for name, fr in out.items(): save_sheet(os.path.join(DEATH, name + '.png'), fr)
    print('   Death/ escrito:', len(out), 'hojas')
    return out

# --- la cinemática montada (la cámara sigue la caída y vuelve) ----------------------------------------------------
def escena_death(out):
    base, fx, fy = world_base()
    cam_y0 = int((TC_Y - 10.25) * U)
    arena_x0 = int((TC_X - 20) * U)
    crash_cam = int(fx + CRASH[0] - 7 * U)
    frog = A.frame(FROG, 0, 32)
    leaves = [A.frame(os.path.join(HERE, 'Canopy', 'Fx_FallingLeaf.png'), k, 16) for k in range(6)]
    trunk_im = A.frame(os.path.join(HERE, 'Tree', 'Lepido_Trunk.png'), 0, 288)
    vrest = A.frame(os.path.join(HERE, 'Tree', 'Lepido_Veins_Rest.png'), 0, 288)
    sway = [A.frame(os.path.join(HERE, 'Canopy', 'Canopy_Arena_Sway.png'), k, 704) for k in range(8)]
    rnd = random.Random(5)
    drops = [(rnd.uniform(-200, 640), rnd.uniform(-140, 20), rnd.uniform(1.6, 3.2), rnd.randrange(6)) for _ in range(60)]
    frog_at = (int(fx + 2 * U), int(fy + 5.5 * U))
    S, ms, keys = [], [], {}
    def add(st, m, key=None):
        if key: keys[key] = len(S)
        S.append(st); ms.append(m)
    for k in range(4): add(dict(tree=('rest',), can=('sway', k), cam=arena_x0), 120)
    for k in range(10): add(dict(tree=('veins', k), can=('sway', k) if k < 6 else ('tear', 0), cam=arena_x0, leaves=k - 6 if k >= 6 else None), VEIN_MS,
                            'las vetas parpadean y se apagan' if k == 3 else None)
    for k in range(5): add(dict(tree=('crack', k), can=('tear', k), cam=arena_x0, shadow=k, leaves=4 + k), CRACK_MS, 'grieta de derecha a izquierda' if k == 3 else None)
    for k in range(NF):
        a = FALL_A[k]
        ca, sa = math.cos(math.radians(a)), math.sin(math.radians(a))
        f_ = ease(max(0., min(1., (a - 48) / 40)))       # la cámara espera a que el tronco baje y luego lo acompaña
        cam = int(arena_x0 + (crash_cam - arena_x0) * f_)
        add(dict(tree=('fall', k), stump=('break', k), can=('tear', 5 + k), cam=cam, shadow=5 + k, leaves=9 + k,
                 fx=[('Fx_TrunkSplinters', k, (fx, fy - BRK))] if k < 6 else []), FALL_MS[k], {3: 'cae despacio', 9: 'acelera; la cámara lo sigue'}.get(k))
    for k in range(3):
        add(dict(tree=('fall', NF + k), stump=('break', NF + k), can=('torn', k), cam=crash_cam + (5 if k == 0 else -3 if k == 1 else 0), leaves=9 + NF + k,
                 fx=[('Fx_CrownCrash', k, (fx + CRASH[0], fy + CRASH[1])), ('Fx_TrunkDust', k, None)], rubble=True), LAND[k][1],
            'la copa revienta la pared norte' if k == 1 else None)
    for k in range(5):
        add(dict(tree=('fall', NF + 2), stump=('break', NF + 2), can=('torn', 3 + k), cam=crash_cam,
                 fx=[('Fx_CrownCrash', 3 + k, (fx + CRASH[0], fy + CRASH[1])), ('Fx_TrunkDust', 3 + k, None)], rubble=True), 100,
            'polvo y esporas (ambiente)' if k == 2 else None)
    for k in range(8):
        cam = int(crash_cam + (arena_x0 - crash_cam) * ease((k + 1) / 8))
        add(dict(tree=('fall', NF + 2), stump=('open', k), can=('torn', k), cam=cam, rubble=True, column=k), 110, 'la grieta del tocón se abre' if k == 5 else None)
    for k in range(6): add(dict(tree=('fall', NF + 2), stump=('idle', k), can=('torn', k), cam=arena_x0, rubble=True, column=k), 160, 'después (pulsar E en la grieta)' if k == 2 else None)
    can_tear, can_torn = out['Canopy_Arena_Tear'], out['Canopy_Arena_Torn_Sway']
    frames = []
    for t, st in enumerate(S):
        im = base.copy()
        cam = st['cam']
        if st.get('rubble'): im.alpha_composite(out['Lepido_WallRubble'][0], (int(fx + CRASH[0] - 224), int(fy + CRASH[1] - 64 - 2 * U)))
        if 'shadow' in st: im.alpha_composite(out['Fx_FallShadow'][st['shadow']], (int(fx + SH_X0), int(fy + SH_Y0)))
        kind = st['tree'][0]
        front = None
        if 'stump' in st:
            sk, si = st['stump']
            spr = out['Lepido_Stump_Break' if sk == 'break' else 'Lepido_Stump_Open' if sk == 'open' else 'Lepido_Stump_Idle'][si]
            im.alpha_composite(spr, (fx - 144, fy - 352))
        if kind == 'rest':
            im.alpha_composite(trunk_im, (fx - 144, fy - 352)); im.alpha_composite(vrest, (fx - 144, fy - 352))
        elif kind == 'veins':
            im.alpha_composite(trunk_im, (fx - 144, fy - 352)); im.alpha_composite(out['Lepido_Death_Veins'][st['tree'][1]], (fx - 144, fy - 352))
        elif kind == 'crack':
            spr = out['Lepido_Death_Crack'][st['tree'][1]]
            im.alpha_composite(spr, (fx - 144, fy - 352))
            front = Image.new('RGBA', im.size, (0, 0, 0, 0)); front.alpha_composite(spr, (fx - 144, fy - 352))
        else:
            spr = out['Lepido_Death_Fall'][st['tree'][1]]
            im.alpha_composite(spr, (fx - FFX, fy - FFY))
            front = Image.new('RGBA', im.size, (0, 0, 0, 0)); front.alpha_composite(spr, (fx - FFX, fy - FFY))
        for (nm, k, pos) in st.get('fx', []):
            f = out[nm][k]
            if nm == 'Fx_TrunkDust': im.alpha_composite(f, (int(fx + DU_X0), int(fy + DU_Y0)))
            else: im.alpha_composite(f, (int(pos[0] - f.width / 2), int(pos[1] - f.height / 2)))
        im.alpha_composite(frog, frog_at)
        if 'column' in st:
            col = out['Fx_GoldColumn'][st['column'] % 6]
            a_ = min(1, (st['column'] + 1) / 6) if st['stump'][0] == 'open' else 1
            c2 = col.copy(); c2.putalpha(c2.split()[3].point(lambda v: int(v * a_)))
            im.alpha_composite(c2, (fx - col.width // 2, int(fy - col.height + 4)))
        if st.get('leaves') is not None:                # hojas y esporas que caen de la copa (adorno)
            age = st['leaves']
            for (x, y0, v, ph) in drops:
                yy = y0 + age * v * 9
                if 0 <= yy <= 330: im.alpha_composite(leaves[(age + ph) % 6], (int(cam + 220 + x + math.sin(age / 2 + ph) * 6), int(cam_y0 + yy)))
        # la capa de hojas (fija arriba): tira del pasillo + la de la arena
        strip = A.frame(os.path.join(HERE, 'Canopy', 'Canopy_StripMid_Sway.png'), t % 8, 256)
        x = 94 * U
        while x < (TC_X - 20) * U - 32:
            im.alpha_composite(strip, (x, cam_y0 - 10)); x += 256
        cm, ck = st['can']
        can = sway[ck % 8] if cm == 'sway' else can_tear[min(len(can_tear) - 1, ck)] if cm == 'tear' else can_torn[ck % 8]
        ox_ = int((TC_X - 20) * U - 32)
        im.alpha_composite(can, (ox_, cam_y0 - 10))
        if front is not None and cm != 'sway':          # por los huecos (cielo) se ve el tronco delante
            cp, fp, ip = can.load(), front.load(), im.load()
            for y in range(can.height):
                for x in range(can.width):
                    c = cp[x, y]
                    if c[3] and tuple(c[:3]) in SKYSET:
                        f = fp[x + ox_, y + cam_y0 - 10]
                        if f[3] > 200: ip[x + ox_, y + cam_y0 - 10] = f
        frames.append(im.crop((cam, cam_y0, cam + 640, cam_y0 + 360)))
    os.makedirs(os.path.join(GIFD, 'escenas'), exist_ok=True)
    o = [f.convert('RGB') for f in frames]
    o[0].save(os.path.join(GIFD, 'escenas', 'Lepido_desplome.gif'), save_all=True, append_images=o[1:], duration=ms, loop=0)
    names = list(keys)
    cols = 4; cw, ch = 640, 360
    rows = (len(names) + cols - 1) // cols
    sheet = Image.new('RGBA', ((cw + 8) * cols, (ch + 26) * rows), (16, 18, 16, 255)); d = ImageDraw.Draw(sheet)
    for j, nm in enumerate(names):
        x, y = (j % cols) * (cw + 8), (j // cols) * (ch + 26)
        sheet.alpha_composite(frames[keys[nm]], (x, y + 24)); d.text((x + 6, y + 4), nm, fill=CYAN, font=A.font(16))
    G._save(sheet, 'Lepidodendron ancestral · 6o-1b la cinemática del desplome (cuadros clave, ×1)',
            ['~6,5 s; la caída arranca despacio y acelera; la cámara la sigue hacia la entrada y vuelve al tocón. GIF: gif/escenas/Lepido_desplome.gif'],
            'vista_previa_6o1_escena.png')
    print('   cinemática:', len(frames), 'cuadros,', sum(ms), 'ms')

def lamina_piezas(out):
    s = 1; pad = 10
    rows = [('Lepido_Death_Veins (10 × 70 ms) · Lepido_Death_Crack (5 × 140 ms)', out['Lepido_Death_Veins'][::2] + out['Lepido_Death_Crack']),
            ('Lepido_Death_Fall (15: 12 de caída lenta que acelera + 3 de golpe)', [f.crop((0, 0, FW, FH)) for f in out['Lepido_Death_Fall'][2::4]]),
            ('Lepido_Stump_Break (10) · Lepido_Stump_Open (8) · Lepido_Stump_Idle (6)', out['Lepido_Stump_Break'][::4] + out['Lepido_Stump_Open'][::2] + out['Lepido_Stump_Idle'][:2]),
            ('Canopy_Arena_Tear (17) · Canopy_Arena_Torn_Sway (8)', out['Canopy_Arena_Tear'][::5] + out['Canopy_Arena_Torn_Sway'][:1]),
            ('Fx_FallShadow (17: la sombra del tronco) · Fx_TrunkDust (8)', out['Fx_FallShadow'][4::4] + out['Fx_TrunkDust'][1:2]),
            ('Fx_TrunkSplinters (6) · Fx_CrownCrash (8) · Lepido_WallRubble · Fx_GoldColumn (6) · Lepido_Shroom*_Off',
             out['Fx_TrunkSplinters'][::2] + out['Fx_CrownCrash'][::3] + out['Lepido_WallRubble'] + out['Fx_GoldColumn'][:2] +
             [out[f'Lepido_Shroom{k}_Off'][0].resize((48, 40), Image.NEAREST) for k in 'ABC'])]
    hs = [max(f.height for f in fr) * s + 26 for _, fr in rows]
    W = min(4000, max(sum(f.width * s + pad for f in fr) for _, fr in rows) + 2 * pad)
    im = Image.new('RGBA', (W, sum(hs) + pad), C('#2e3128')); d = ImageDraw.Draw(im)
    y = pad
    for (lab, fr), h in zip(rows, hs):
        d.text((pad, y), lab, fill=CYAN, font=A.font(15))
        x = pad
        for f in fr:
            if x + f.width > W: break
            im.alpha_composite(f, (x, y + 20)); x += f.width * s + pad
        y += h
    G._save(im, 'Lepidodendron ancestral · 6o-1b hojas del desplome (×1)', ['Muestra de cuadros de cada hoja; los tamaños y pivotes, en el README.'], 'vista_previa_6o1_piezas.png')

if __name__ == '__main__':
    which = sys.argv[1:] or ['escala']
    if 'escala' in which: lamina_escala()
    if 'muestra' in which: lamina_muestra()
    if 'piezas' in which or 'escena' in which:
        out = piezas_death(); lamina_piezas(out)
        if 'escena' in which: escena_death(out)
