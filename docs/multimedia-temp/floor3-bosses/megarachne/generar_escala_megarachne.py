"""Láminas de escala de la Megarachne (tanda 6, paso 0): NO es el arte final.

Una Megarachne provisional (euriptérido con 8 patas de araña, picada 55°, ~6 u = 96 px de punta a punta de las patas)
sobre el Nido del Bosque humeante (suelo 32×20 u) armado con los tiles de la tanda 1a, con la rana del jugador (32 px)
y los recuadros de la cámara del juego (480×270 px = 30×16,9 u). Sirve para comprobar el tamaño del minijefe, de su
sala y de su red de hilos.

- Sala: red de 5 hilos entre Sigillarias y un tronco partido en el centro; la Megarachne corre por un hilo (en alto,
  con su sombra en el suelo); saco de crías colgado de otro hilo; crías; telarañas de la ráfaga de seda en el suelo;
  sombra de la red que cae; helechos (algunos ardiendo), odres de metano y ascuas (el fuego que se renueva).
- Detalle ×3: la rana, una Araña de las frondas, una cría, la Megarachne en el suelo, en un hilo y bajando colgada,
  y el Euriptérido del piso 2 (el guiño).

Uso: python generar_escala_megarachne.py [estrella] [poligono] [nido] [detalle]   (sin nada: todas)
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
MT = os.path.normpath(os.path.join(HERE, '..', '..'))
sys.path.insert(0, os.path.join(MT, 'floor3-environment'))
sys.path.insert(0, os.path.join(HERE, '..', 'arthropleura'))
import generar_escala_arthropleura as A                # room (Smoke), rect_room, frame, font, cam_box
from base_piso3 import *                               # noqa: F401,F403
from PIL import Image, ImageDraw

U = 16
SPAN = 96                                              # de punta a punta de las patas (6 u)
SQUASH = .84                                           # picada: lo que va hacia arriba se acorta
ZK = .6                                                # altura (px) → desplazamiento hacia arriba en pantalla
PROPS = os.path.join(MT, 'floor3-environment', 'Smoke', 'props')
EMBERS = os.path.join(MT, 'floor3-environment', 'Burnt', 'props', 'Deco_Embers.png')
FIRE_FX = os.path.join(MT, 'floor3-mutations', 'Effects', 'Fx_GroundFire.png')
FROG = os.path.join(MT, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')
SPIDER = os.path.join(MT, 'floor3-enemies', 'Spider', 'Spider_Idle.png')
WEB = os.path.join(MT, 'floor3-enemies', 'Effects', 'Fx_SpiderWeb_Idle.png')
EURY = os.path.join(MT, 'floor2-bosses', 'Eurypterid', 'Eurypterid_Walk.png')

BODY = ramp('#4a3a4a', 5)                              # gris violáceo (tanda 0)
RED = ramp('#c83a3a', 4)                               # marcas rojas
SILK = (226, 222, 236)

# -------------------------------------------------------------------------------------------------
# Cuerpo provisional (local: f = hacia delante, s = hacia un lado, z = altura; px a escala 1 = 96 px)
# -------------------------------------------------------------------------------------------------
def to_screen(cx, cy, ang, pts, k=1.0):
    ca, sa = math.cos(ang), math.sin(ang)
    out = []
    for p in pts:
        f, s = p[0] * k, p[1] * k
        z = (p[2] if len(p) > 2 else 0) * k
        out.append((cx + f * ca - s * sa, cy + (f * sa + s * ca) * SQUASH - z * ZK))
    return out

def ell(cx, cy, ang, f, s, rf, rs, k=1.0, n=14):
    return to_screen(cx, cy, ang, [(f + rf * math.cos(2 * math.pi * i / n), s + rs * math.sin(2 * math.pi * i / n)) for i in range(n)], k)

CARAPACE = [(23, 0), (22, 5), (19, 9), (13, 12.5), (6, 13.5), (3, 12), (3, -12), (6, -13.5), (13, -12.5), (19, -9), (22, -5)]
ABDOMEN = [(-1.5 - i * 5.2, w) for i, w in enumerate([12, 12.6, 12.2, 11, 9.4, 7.4, 5.4])]
# patas: (cadera f, s) → rodilla (f, s, z) → pie (f, s); lado +; las del otro lado se reflejan
LEGS = [((18, 9), (30, 20, 16), (40, 30)),
        ((14, 11), (19, 30, 18), (17, 45)),
        ((10, 12), (2, 31, 18), (-6, 45)),
        ((6, 11), (-12, 24, 16), (-27, 35))]

def silhouette(d, x, y, ang, k, col):
    for side in (-1, 1):
        for hip, knee, foot in LEGS:
            pts = [(hip[0], side * hip[1]), (knee[0], side * knee[1], knee[2]), (foot[0], side * foot[1])]
            d.line(to_screen(x, y, ang, pts, k), fill=col, width=max(1, round(3 * k)))
    d.polygon(to_screen(x, y, ang, CARAPACE, k), fill=col)
    for f, w in ABDOMEN: d.polygon(ell(x, y, ang, f, 0, 3.6, w, k), fill=col)
    d.polygon(to_screen(x, y, ang, [(-36, 2), (-48, 0), (-36, -2)], k), fill=col)

def megarachne(im, x, y, ang, k=1.0, alt=0, shadow=True, legs_up=0.0):
    """(x, y) = punto del suelo bajo el cuerpo (px); alt = altura sobre su sombra (px); legs_up = alza las delanteras."""
    if shadow:
        sl = Image.new('RGBA', im.size, (0, 0, 0, 0))
        silhouette(ImageDraw.Draw(sl), x, y, ang, k, (6, 4, 8, 255))
        px = sl.load(); out = Image.new('RGBA', im.size, (0, 0, 0, 0)); po = out.load()
        r = int(60 * k) + 4
        for j in range(max(0, int(y - r)), min(im.height, int(y + r))):
            for i in range(max(0, int(x - r)), min(im.width, int(x + r))):
                if px[i, j][3] and (alt < 6 or BAYER[j % 4][i % 4] < 11):
                    po[i, j] = (6, 4, 8, 110 if alt < 6 else 140)
        im.alpha_composite(out)
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    cy = y - alt * ZK
    lw = max(1, round(3 * k))

    def leg(side, hip, knee, foot, front):
        kz, fz = knee[2], 0
        if front and legs_up:                                  # amenaza: las delanteras se alzan
            kz += 14 * legs_up; fz = 30 * legs_up
        pts = to_screen(x, cy, ang, [(hip[0], side * hip[1], 2), (knee[0], side * knee[1], kz), (foot[0], side * foot[1], fz)], k)
        d.line(pts, fill=OUTL, width=lw + (2 if k > .5 else 0))
        d.line(pts, fill=C(BODY[2]), width=max(1, lw - 1) if k > .5 else lw)
        if k > .5:
            kx, ky = pts[1]
            d.rectangle([kx - 1, ky - 1, kx + 1, ky + 1], fill=C(RED[2]))      # rodilla roja
            d.line([pts[0], pts[1]], fill=C(BODY[3]), width=1)

    for side in (-1, 1):                                        # patas de detrás primero
        for i, (hip, knee, foot) in enumerate(LEGS[::-1]):
            leg(side, hip, knee, foot, front=(i == 3))
    d.polygon(to_screen(x, cy, ang, [(-35, 2), (-48, 0), (-35, -2)], k), fill=C(BODY[1]), outline=OUTL)      # telson
    d.point(to_screen(x, cy, ang, [(-46, 0)], k)[0], fill=C(RED[2]))
    for i, (f, w) in enumerate(ABDOMEN[::-1]):                  # abdomen de la punta al caparazón
        col = BODY[2] if i % 2 else BODY[1]
        d.polygon(ell(x, cy, ang, f, 0, 3.8, w, k), fill=C(col), outline=OUTL)
        if k > .5: d.line(to_screen(x, cy, ang, [(f + 1, -w * .5), (f + 1, w * .5)], k), fill=C(BODY[3]), width=1)
    if k > .5:
        for i in (1, 3):                                        # manchas rojas por pares
            f, w = ABDOMEN[i]
            for s in (-1, 1): d.polygon(ell(x, cy, ang, f, s * w * .55, 1.6, 2, k, 8), fill=C(RED[1]))
        for s in (-1, 1): d.line(to_screen(x, cy, ang, [(-33, s * 1.5), (-36, s * 3)], k), fill=C(BODY[3]), width=1)   # hileras
    else:
        d.point(to_screen(x, cy, ang, [(-10, 0)], k)[0], fill=C(RED[2]))
    d.polygon(to_screen(x, cy, ang, CARAPACE, k), fill=C(BODY[2]), outline=OUTL)                    # caparazón
    if k > .5:
        d.polygon(ell(x, cy, ang, 15, -4, 5, 5, k), fill=C(BODY[3]))
        d.line(to_screen(x, cy, ang, [(6, -7), (12, 0), (6, 7)], k), fill=C(RED[1]), width=2)      # galón rojo
        for s in (-1, 1):
            d.polygon(ell(x, cy, ang, 17, s * 6.5, 2.6, 2.4, k, 10), fill=C('#120c14'), outline=OUTL)  # ojos compuestos
            d.point(to_screen(x, cy, ang, [(18, s * 6.5)], k)[0], fill=C(RED[3]))
            d.point(to_screen(x, cy, ang, [(20, s * 2)], k)[0], fill=C(RED[2]))                       # ocelos
            d.line(to_screen(x, cy, ang, [(22, s * 3), (27, s * 2.5), (28, s * .8)], k), fill=C(BODY[0]), width=2)  # quelíceros
    im.alpha_composite(lay)

def silk_line(im, p0, p1, sag=6, ground=None, alpha=220):
    """Hilo de seda entre dos puntos de pantalla (con caída); ground = (g0, g1) dibuja su sombra punteada en el suelo."""
    d = ImageDraw.Draw(im)
    if ground:
        (gx0, gy0), (gx1, gy1) = ground
        n = int(math.hypot(gx1 - gx0, gy1 - gy0))
        for i in range(0, n, 3):
            t = i / max(1, n)
            d.point((gx0 + (gx1 - gx0) * t, gy0 + (gy1 - gy0) * t), fill=(6, 4, 8, 70))
    pts = []
    for i in range(21):
        t = i / 20
        pts.append((p0[0] + (p1[0] - p0[0]) * t, p0[1] + (p1[1] - p0[1]) * t + sag * 4 * t * (1 - t)))
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); ld = ImageDraw.Draw(lay)
    ld.line(pts, fill=SILK + (alpha,), width=1)
    im.alpha_composite(lay)
    return pts

def sac(im, x, y, alt):
    """Saco de crías provisional colgado a alt px sobre el suelo, con su hilo hacia arriba."""
    d = ImageDraw.Draw(im)
    d.ellipse([x - 5, y - 2, x + 5, y + 2], fill=(6, 4, 8, 90))
    cy = y - alt * ZK
    d.line([(x, cy - 40), (x, cy - 7)], fill=SILK + (230,), width=1)
    d.ellipse([x - 6, cy - 8, x + 6, cy + 8], fill=C('#d8d0c4'), outline=OUTL)
    for (dx, dy) in ((-2, -3), (2, 0), (-1, 3), (3, -4)):
        d.point((x + dx, cy + dy), fill=C(BODY[2]))
    d.point((x - 3, cy - 5), fill=(255, 255, 255))

def web(im, x, y, s=1.5):
    w = Image.open(WEB).convert('RGBA')
    w = w.resize((int(w.width * s), int(w.height * s)), Image.NEAREST)
    im.alpha_composite(w, (int(x - w.width / 2), int(y - w.height / 2)))

def fall_shadow(im, x, y, r):
    """Sombra de la red que cae (crece): círculo tramado con borde rojo provisional."""
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); po = lay.load()
    for j in range(int(y - r), int(y + r) + 1):
        for i in range(int(x - r), int(x + r) + 1):
            dd = math.hypot(i - x, (j - y) * 1.3)
            if dd <= r and 0 <= i < im.width and 0 <= j < im.height:
                if dd > r - 1.5: po[i, j] = (200, 58, 58, 200)
                elif BAYER[j % 4][i % 4] < 6: po[i, j] = (6, 4, 8, 120)
    im.alpha_composite(lay)

def _band_dither(im, poly_pts, a=90, lvl=9):
    m = Image.new('L', im.size, 0); ImageDraw.Draw(m).polygon(poly_pts, fill=255)
    mp = m.load(); lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); po = lay.load()
    xs = [p[0] for p in poly_pts]; ys = [p[1] for p in poly_pts]
    for j in range(max(0, int(min(ys))), min(im.height, int(max(ys)) + 1)):
        for i in range(max(0, int(min(xs))), min(im.width, int(max(xs)) + 1)):
            if mp[i, j] and BAYER[j % 4][i % 4] < lvl: po[i, j] = (6, 4, 8, a)
    im.alpha_composite(lay)

def cable_shadow(im, g0, g1, w=14):
    dx, dy = g1[0] - g0[0], g1[1] - g0[1]; L = math.hypot(dx, dy) or 1
    px, py = -dy / L * w / 2, dx / L * w / 2 * .82
    _band_dither(im, [(g0[0] + px, g0[1] + py), (g1[0] + px, g1[1] + py), (g1[0] - px, g1[1] - py), (g0[0] - px, g0[1] - py)])

def cable(im, p0, p1, w=14, sag=10, seed=0):
    """Cable de seda (~1 u de ancho): varias hebras trenzadas con travesaños, como una pasarela colgante.
    Devuelve la línea central (21 puntos) para colocar encima a la Megarachne."""
    n = 40
    dx, dy = p1[0] - p0[0], p1[1] - p0[1]; L = math.hypot(dx, dy) or 1
    px, py = -dy / L, dx / L * .82
    C_ = [(p0[0] + dx * i / n, p0[1] + dy * i / n + sag * 4 * (i / n) * (1 - i / n)) for i in range(n + 1)]
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    half = w / 2
    edge = lambda k: [(x + px * half * k, y + py * half * k) for (x, y) in C_]
    d.polygon(edge(1) + edge(-1)[::-1], fill=SILK + (70,))
    rnd = random.Random(seed)
    for i in range(2, n - 1, 2):                                # travesaños
        a_, b_ = edge(1)[i], edge(-1)[i + rnd.choice((-1, 0, 1))]
        d.line([a_, b_], fill=SILK + (120,), width=1)
    for k, ph in ((-.25, 0), (.25, 2.1), (0, 4.0)):            # hebras trenzadas por dentro
        pts = [(x + px * half * (k + .3 * math.sin(i * .5 + ph)), y + py * half * (k + .3 * math.sin(i * .5 + ph))) for i, (x, y) in enumerate(C_)]
        d.line(pts, fill=SILK + (210,), width=1)
    d.line(edge(1), fill=(196, 190, 212, 235), width=1)         # bordes
    d.line(edge(-1), fill=(150, 144, 168, 235), width=1)
    for i in range(3, n - 2, 7):                                # gotas de rocío
        x, y = edge(rnd.choice((1, -1)))[i]
        d.point((x, y + 1), fill=(255, 255, 255, 230))
    im.alpha_composite(lay)
    return [C_[i * 2] for i in range(21)]

def nest(im, x, y, h, rx=50, ry=34, seed=3, shadow=True):
    """El Nido: telaraña grande que envuelve la copa del tronco del centro, con embudo oscuro (su refugio).
    (x, y) = pie del tronco; h = altura del centro del nido en pantalla (px)."""
    rnd = random.Random(seed)
    cx, cy = x, y - h
    if shadow:
        sh = [(x + rx * .9 * math.cos(a), y + ry * .55 * math.sin(a)) for a in [k / 24 * 2 * math.pi for k in range(24)]]
        _band_dither(im, sh, a=100, lvl=10)
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    for k in range(9):                                          # seda que envuelve el tronco hacia abajo
        t0 = rnd.uniform(-1, 1)
        d.line([(cx + t0 * 10, cy + ry * .5), (x + rnd.uniform(-12, 12), y - rnd.uniform(4, 14))], fill=SILK + (150,), width=1)
    R = lambda a, f: (cx + rx * f * math.cos(a) * (1 + .1 * math.sin(a * 5 + seed)), cy + ry * f * math.sin(a) * (1 + .1 * math.sin(a * 5 + seed)))
    angs = [k / 48 * 2 * math.pi for k in range(48)]
    d.polygon([R(a, 1) for a in angs], fill=SILK + (60,))
    d.polygon([R(a, .62) for a in angs], fill=SILK + (60,))
    for k in range(18):                                         # radios
        a = k / 18 * 2 * math.pi + rnd.uniform(-.08, .08)
        d.line([R(a, .18), R(a, 1.02)], fill=SILK + (190,), width=1)
    for f in (.35, .55, .75, .93):                              # espiras irregulares
        d.line([R(a, f + .03 * math.sin(a * 7 + f * 9)) for a in angs] + [R(0, f)], fill=SILK + (150,), width=1)
    d.line([R(a, 1) for a in angs] + [R(0, 1)], fill=(196, 190, 212, 220), width=1)
    d.ellipse([cx - 13, cy - 9, cx + 13, cy + 9], fill=(206, 200, 220, 200))   # boca del embudo
    d.ellipse([cx - 10, cy - 6, cx + 10, cy + 7], fill=(20, 16, 24, 245))
    d.ellipse([cx - 6, cy - 2, cx + 6, cy + 6], fill=(8, 6, 10, 255))
    im.alpha_composite(lay)
    return cx, cy

# -------------------------------------------------------------------------------------------------
# El Nido (Bosque humeante)
# -------------------------------------------------------------------------------------------------
def put(im, spr, x, y, anchor='foot'):
    oy = spr.height - 4 if anchor == 'foot' else spr.height / 2
    im.alpha_composite(spr, (int(x * U - spr.width / 2), int(y * U - oy)))

ANCHOR_H = {'tree': 44, 'trunk': 32}                   # altura (px de pantalla) donde se ata el hilo

def thread_version(im, anchors, threads, ground, on, sac_on):
    lines = {}
    for a, b in threads:
        (ax, ay, ak), (bx, by, bk) = anchors[a], anchors[b]
        p0 = (ax * U, ay * U - ANCHOR_H[ak]); p1 = (bx * U, by * U - ANCHOR_H[bk])
        lines[(a, b)] = silk_line(im, p0, p1, sag=8, ground=ground[(a, b)])
    a, b, t = on
    gx0, gy0 = anchors[a][0] * U, anchors[a][1] * U
    gx1, gy1 = anchors[b][0] * U, anchors[b][1] * U
    sx, sy = gx0 + (gx1 - gx0) * t, gy0 + (gy1 - gy0) * t
    hx, hy = lines[(a, b)][round(t * 20)]
    megarachne(im, sx, sy, math.atan2(gy1 - gy0, gx1 - gx0), alt=(sy - hy) / ZK)
    a, b, t = sac_on
    gx0, gy0 = anchors[a][0] * U, anchors[a][1] * U
    gx1, gy1 = anchors[b][0] * U, anchors[b][1] * U
    hx, hy = lines[(a, b)][round(t * 20)]
    sx2, sy2 = gx0 + (gx1 - gx0) * t, gy0 + (gy1 - gy0) * t + 10
    sac(im, sx2, sy2, (sy2 - hy - 40) / ZK)
    return [(sx, sy, sx2, sy2)]

def cable_version(im, anchors, threads, on, nest_h):
    """Cables de seda gruesos hacia el Nido (telaraña grande en la copa del tronco del centro); la Megarachne de la
    6h (paleta A) caminando por encima de un cable."""
    import generar_megarachne as MG
    import generar_nido_megarachne as NG
    cxg, cyg = anchors['C'][0] * U, anchors['C'][1] * U              # pie del árbol del Nido
    ox, oy = cxg - NG.FX, cyg - NG.FY
    ncx, ncy = cxg, cyg - (NG.FY - NG.CY)
    angs = [k / 32 * 2 * math.pi for k in range(32)]                  # sombra de la copa en el suelo
    _band_dither(im, [(cxg + 60 * math.cos(a), cyg - 6 + 28 * math.sin(a)) for a in angs], a=110, lvl=10)
    sac(im, cxg + 34, cyg + 16, 16)                                   # saco colgado del borde de la copa
    im.alpha_composite(NG.tree(), (int(ox), int(oy)))
    im.alpha_composite(NG.nest_layer(), (int(ox), int(oy)))
    lines = {}
    for i, (a, b) in enumerate(threads):                              # los cables nacen en las puntas de las ramas
        ax, ay, ak = anchors[a]
        p0 = (ax * U, ay * U - ANCHOR_H[ak])
        tx, ty = NG.anchor(a)
        p1 = (ox + tx, oy + ty)
        lines[(a, b)] = (cable(im, p0, p1, sag=10, seed=i), p0, p1)
    a, b, t = on
    pts, p0, p1 = lines[(a, b)]
    hx, hy = pts[round(t * 20)]
    gx0, gy0 = anchors[a][0] * U, anchors[a][1] * U
    sx, sy = gx0 + (cxg - gx0) * t, gy0 + (cyg - gy0) * t             # punto del suelo bajo ella
    yaw = math.degrees(math.atan2(-(p1[1] - p0[1]) / .82, p1[0] - p0[0]))
    spr, sh = MG.render(yaw, 'A')
    s_ = sh.copy(); s_.putalpha(s_.getchannel('A').point(lambda v: 120 if v else 0))
    im.alpha_composite(s_, (int(sx - 64), int(sy - 64)))
    im.alpha_composite(spr, (int(hx - 64), int(hy - 64)))
    return [(sx, sy, cxg + 34, cyg + 16),
            (ncx / U - 4.5, (ncy - 52) / U, 'el Nido: Lepidodendron empapado (embudo ~3 u)')]

def lamina_sala(version='estrella'):
    fw, fh = 32, 20
    W, H, fx0, fy0 = fw + 4, fh + 6, 2, 3
    im = A.room(A.rect_room(W, H, (fx0, fy0, fx0 + fw - 1, fy0 + fh - 1)))
    X = lambda t: fx0 + fw * t
    Y = lambda t: fy0 + fh * t
    rnd = random.Random(7)
    cables = version == 'nido'
    if version in ('estrella', 'nido'):                # 5 hilos (o cables) de los bordes al tronco partido del centro
        anchors = {'NO': (X(.07), Y(.16), 'tree'), 'N': (X(.52), Y(.07), 'tree'), 'NE': (X(.93), Y(.15), 'tree'),
                   'SO': (X(.09), Y(.92), 'tree'), 'SE': (X(.92), Y(.9), 'tree'), 'C': (X(.47), Y(.52), 'trunk')}
        threads = [('NO', 'C'), ('N', 'C'), ('NE', 'C'), ('SO', 'C'), ('SE', 'C')]
        on, sac_on = ('NE', 'C', .55), ('NO', 'C', .5)
    else:                                              # polígono: 6 hilos entre los troncos del borde, centro libre
        anchors = {'NO': (X(.07), Y(.16), 'tree'), 'N': (X(.5), Y(.07), 'tree'), 'NE': (X(.93), Y(.15), 'tree'),
                   'SE': (X(.92), Y(.9), 'tree'), 'S': (X(.55), Y(.97), 'trunk'), 'SO': (X(.09), Y(.92), 'tree')}
        threads = [('NO', 'N'), ('N', 'NE'), ('NE', 'SE'), ('SE', 'S'), ('S', 'SO'), ('SO', 'NO')]
        on, sac_on = ('NE', 'SE', .4), ('NO', 'N', .5)
    # suelo: helechos (algunos ardiendo), odres, ascuas
    fire = [A.frame(FIRE_FX, i, 32) for i in range(6)]
    ferns = [('Fern_Leafy_Sway', 84), ('Fern_Tree_Sway', 84), ('Fern_Bent_Sway', 88), ('Fern_Young_Sway', 44), ('Fern_Sparse_Sway', 92)]
    spots = []
    for k in range(22):
        fx, fy = rnd.uniform(X(.03), X(.97)), rnd.uniform(Y(.08), Y(.98))
        if any(math.hypot(fx - ax, fy - ay) < 2.5 for ax, ay, _ in anchors.values()): continue
        if cables and abs(fx - anchors['C'][0]) < 5 and -6 < fy - anchors['C'][1] < 2: continue
        if math.hypot(fx - X(.36), fy - Y(.62)) < 3: continue
        spots.append((fx, fy, 'fern'))
    spots += [(X(.2), Y(.45), 'sac'), (X(.78), Y(.66), 'sac')]
    spots += [(x, y, k) for (x, y, k) in anchors.values() if not (cables and k == 'trunk')]   # en 'nido', el árbol lo pone cable_version
    emb = A.frame(EMBERS, 0, 16)
    for k in range(16): put(im, emb, rnd.uniform(X(.03), X(.97)), rnd.uniform(Y(.05), Y(.98)), 'center')
    # telarañas de la ráfaga (camino hacia la rana) y sombra de la red que cae
    for t in (.0, .5, 1.0):
        web(im, (X(.62) + (X(.44) - X(.62)) * t) * U, (Y(.36) + (Y(.56) - Y(.36)) * t) * U + 18)
    fall_shadow(im, X(.27) * U, Y(.7) * U, 2.5 * U)
    # sombras de los hilos (antes que los props)
    ground = {}
    for a, b in threads:
        ground[(a, b)] = ((anchors[a][0] * U, anchors[a][1] * U - 6), (anchors[b][0] * U, anchors[b][1] * U - 6))
    NEST_H = 54
    if cables:                                                  # sombras de los cables en el suelo
        for a, b in threads: cable_shadow(im, *ground[(a, b)])
    for (x, y, k) in sorted(spots, key=lambda p: p[1]):
        if k == 'fern':
            n, w = rnd.choice(ferns); spr = A.frame(os.path.join(PROPS, n + '.png'), 0, w)
            if rnd.random() < .5: spr = spr.transpose(Image.FLIP_LEFT_RIGHT)
            put(im, spr, x, y)
            if rnd.random() < .25: put(im, fire[rnd.randrange(6)], x, y + .2)
        elif k == 'sac':
            put(im, A.frame(os.path.join(PROPS, 'Methane_Sac_Idle.png'), 0, 24), x, y)
        else:
            name = {'tree': 'Obstacle_Sigillaria', 'trunk': 'Obstacle_BrokenTrunk'}[k]
            put(im, Image.open(os.path.join(PROPS, name + '.png')).convert('RGBA'), x, y)
    # rana, crías (suelo)
    A.player(im, X(.36), Y(.62))
    for (cx, cy, a) in ((X(.16), Y(.8), -20), (X(.2), Y(.86), 10), (X(.13), Y(.74), -60)):
        megarachne(im, cx * U, cy * U, math.radians(a), k=.19)
    if cables:
        lab = cable_version(im, anchors, threads, on, NEST_H)
    else:
        lab = thread_version(im, anchors, threads, ground, on, sac_on)
    sx, sy, sx2, sy2 = lab.pop(0)

    cams = [(X(.36), Y(.6), 1, A.WHITE, 'cámara ×1 (30×16,9 u), sigue a la rana')]
    lab += [(min(sx / U - 2, W - 14), sy / U - 6.2, 'en su ' + ('cable' if cables else 'hilo') + ' (invulnerable; sombra en el suelo)'),
           ((sx2 / U + .8, sy2 / U - .9) if cables else (sx2 / U - 1.5, sy2 / U - 3.4)) + ('saco de crías',), (X(.08), Y(.66), 'crías'), (X(.2), Y(.79) + 1.6, 'red que cae (sombra)'),
           (X(.62) + 1.6, Y(.45), 'telarañas de la ráfaga'), (X(.32), Y(.62) + 1.3, 'rana')]
    s = 2
    big = im.resize((im.width * s, im.height * s), Image.NEAREST)
    d = ImageDraw.Draw(big)
    for (cx, cy, z, col, label) in cams: A.cam_box(d, cx, cy, z, col, label, s)
    for item in lab:
        x, y, tx = item if len(item) == 3 else (item[0][0], item[0][1], item[1])
        d.text((x * U * s, y * U * s), tx, fill=(120, 230, 230), font=A.font(15), stroke_width=3, stroke_fill=(10, 10, 14))
    by = big.height - 30
    d.rectangle([20, by, 20 + SPAN * s, by + 8], fill=(240, 230, 200))
    d.text((28 + SPAN * s, by - 6), f'6 u = Megarachne de punta a punta de las patas ({SPAN} px)', fill=(240, 230, 200), font=A.font(16), stroke_width=3, stroke_fill=(10, 10, 14))
    red = {'estrella': 'red en estrella: 5 hilos de las Sigillarias del borde al tronco partido del centro',
           'poligono': 'red en polígono: 6 hilos entre los troncos del borde; el centro queda libre para pelear en el suelo',
           'nido': 'cables de seda (~1 u) de las Sigillarias a las ramas del Nido: Lepidodendron empapado del centro con la telaraña y el embudo en la copa'}[version]
    quien = ('Megarachne de la 6h (paleta A) caminando por encima de un cable.' if cables else
             'Megarachne provisional (formas planas, paleta de la tanda 0) corriendo por un hilo.')
    notes = [quien, red[0].upper() + red[1:] + '.',
             'En el suelo: crías, telarañas de la ráfaga de seda, sombra de la red que cae, helechos ardiendo, odres de metano y ascuas.',
             'Cámara ×1 (decidido): sigue a la rana y la sala se recorre un poco.']
    _save(big, f'Megarachne · el Nido: suelo 32×20 u (Bosque humeante) · {version}', notes, f'vista_previa_escala_sala_{version}.png')

def lamina_detalle():
    """Detalle ×3 junto a la rana: Araña de las frondas, cría, Megarachne en el suelo / en un hilo / bajando, Euriptérido."""
    fw, fh = 44, 9
    im = A.room(A.rect_room(fw + 4, fh + 6, (2, 3, fw + 1, fh + 2)))
    gy = 9.6
    put(im, A.frame(FROG, 0, 32), 4.2, gy)
    put(im, A.frame(SPIDER, 0, 32), 8.2, gy)
    megarachne(im, 11.6 * U, (gy - .3) * U, math.radians(-90), k=.19)
    megarachne(im, 16.5 * U, (gy - 1.2) * U, math.radians(-90))
    # en un hilo entre dos Sigillarias
    for x in (20.5, 30.5): put(im, Image.open(os.path.join(PROPS, 'Obstacle_Sigillaria.png')).convert('RGBA'), x, gy + .2)
    p = silk_line(im, (20.5 * U, (gy + .2) * U - 44), (30.5 * U, (gy + .2) * U - 44), sag=6,
                  ground=((20.5 * U, (gy + .2) * U - 6), (30.5 * U, (gy + .2) * U - 6)))
    hx, hy = p[10]
    megarachne(im, hx, (gy - .1) * U, 0, alt=((gy - .1) * U - hy) / ZK)
    # bajando colgada (entrada), patas delanteras alzadas
    d = ImageDraw.Draw(im)
    d.line([(35 * U, 3 * U), (35 * U, (gy - 1.2) * U - 52 * ZK - 14)], fill=SILK + (230,), width=1)
    megarachne(im, 35 * U, (gy - .6) * U, math.radians(-90), alt=52, legs_up=1.0)
    eu = A.frame(EURY, 0, 128)
    put(im, eu, 42, gy + 1.4)
    crop = im.crop((2 * U, 3 * U, (fw + 2) * U, (fh + 3) * U))
    s = 3
    big = crop.resize((crop.width * s, crop.height * s), Image.NEAREST)
    d = ImageDraw.Draw(big)
    labels = [(2.2, 'rana 32 px'), (5.2, 'araña frondas'), (8.9, 'cría ~18 px'), (12.6, 'en el suelo'),
              (21.8, 'en un hilo (sombra debajo)'), (30.6, 'entrada: baja colgada'), (36.4, 'Euriptérido del piso 2')]
    for x, t in labels:
        d.text((x * U * s, 8), t, fill=(120, 230, 230), font=A.font(17), stroke_width=3, stroke_fill=(10, 10, 14))
    _save(big, 'Megarachne · detalle ×3 (6 u de patas, paleta de la tanda 0) junto a la rana, una araña, una cría y el Euriptérido',
          ['Euriptérido con 8 patas de araña: caparazón ancho con galón rojo, ojos compuestos y ocelos rojos, quelíceros, abdomen',
           'segmentado con manchas rojas, hileras y telson en pincho. Solo forma y tamaño: el sombreado y el volumen llegan en la 6h.'],
          'vista_previa_escala_detalle.png')

def _save(big, title, notes, name):
    head = Image.new('RGBA', (big.width, 34 + 22 * len(notes)), (16, 18, 16, 255))
    hd = ImageDraw.Draw(head)
    hd.text((14, 6), title, fill=(255, 240, 200), font=A.font(20))
    for k, n_ in enumerate(notes): hd.text((14, 32 + 22 * k), n_, fill=(200, 210, 200), font=A.font(15))
    full = Image.new('RGBA', (big.width, big.height + head.height), (16, 18, 16, 255))
    full.alpha_composite(head, (0, 0)); full.alpha_composite(big, (0, head.height))
    out = os.path.join(HERE, name)
    full.convert('RGB').save(out); print('  ', name, full.size)

if __name__ == '__main__':
    which = sys.argv[1:] or ['estrella', 'poligono', 'nido', 'detalle']
    for w in which:
        if w == 'detalle': lamina_detalle()
        else: lamina_sala(w)
