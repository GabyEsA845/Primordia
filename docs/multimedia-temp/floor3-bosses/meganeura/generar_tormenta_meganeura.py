"""La tormenta de la F3 de la Meganeura (tanda 6f-2). Skill primordia-sprites (pixel.py).

6f-2a, los rayos y lo que dejan:
- Rayo GRANDE que sigue al jugador: Fx_Bolt_Big_Mark (círculo azul con chispas y corriente que crecen; el cuadro
  es la carga) → Fx_Bolt_Big (cae desde arriba) → Fx_Bolt_Big_Impact (destello, chispas) → Fx_Bolt_Scorch.
- Rayos SUELTOS por la sala: Fx_Bolt_Small_Mark (señal en el terreno) → Fx_Bolt_Small → Fx_Bolt_Small_Impact.
- El primer trueno de la tormenta: Fx_Bolt_Huge (ramificado, enorme; el código ilumina toda la pantalla).
- Fx_Bolt_Ring: la corriente circular que deja un rayo en el suelo: se expande un poco y se apaga en ~4 s.
- Burnt/Puddle_Water_<Forma>_Electric: la charca electrificada entera (mismas formas y celdas que las charcas).
- Burnt/Sigillaria_Struck/_Burning/_Smolder/_Split: la Sigillaria quemada atrae el rayo, se parte y arde; la
  lluvia la va apagando (un solo uso).
(La silueta a contraluz va en generar_meganeura.py silueta; la ninfa electrocutada en generar_ninfa_meganeura.py.)

Uso: python generar_tormenta_meganeura.py rayos      (6f-2a)
     python generar_tormenta_meganeura.py ambiente   (6f-2b: lluvia, llovizna, salpicaduras, ondas, nubes, viento, vapor, charcos nuevos)
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
MT = os.path.normpath(os.path.join(HERE, '..', '..'))
for d in ('floor3-environment', 'floor3-reactions'):
    sys.path.insert(0, os.path.join(MT, d))
_argv = sys.argv; sys.argv = sys.argv[:1]
import generar_escenario_piso3 as S                    # sigillaria(), ground_shadow()
import generar_reacciones_piso3 as RX                  # vflame(), cloud()
sys.argv = _argv
from base_piso3 import *                               # noqa: F401,F403
from PIL import Image

OUT = HERE
BGC = C('#16161e')                                      # fondo de revisión: la sala a oscuras con la tormenta
EL = ['#0a1a3a', '#1a4a9a', '#4aa0f0', '#a8e0ff', '#ffffff']    # electricidad
SCORCH = ['#0e0a0c', '#1c1618', '#2c2426']
PROPS = os.path.join(MT, 'floor3-environment', 'Burnt', 'props')

def Ca(c, a): return C(c)[:3] + (max(0, min(255, int(a))),)

# =================================================================================================
# Rayos
# =================================================================================================
def zigzag(x0, y0, x1, y1, rnd, rough=.35, depth=6):
    """Línea quebrada por desplazamiento del punto medio (aspecto de rayo)."""
    pts = [(x0, y0), (x1, y1)]
    for _ in range(depth):
        out = [pts[0]]
        for (ax, ay), (bx, by) in zip(pts, pts[1:]):
            L = math.hypot(bx - ax, by - ay)
            mx, my = (ax + bx) / 2, (ay + by) / 2
            nx, ny = -(by - ay) / (L or 1), (bx - ax) / (L or 1)
            d = rnd.uniform(-1, 1) * L * rough
            out += [(mx + nx * d, my + ny * d), (bx, by)]
        pts = out
    return pts

def stroke(cv, pts, w, cols):
    """Trazo grueso: halo (cols[0]) de ancho w+2, cuerpo (cols[1]) y núcleo (cols[2])."""
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        n = int(max(abs(bx - ax), abs(by - ay))) + 1
        for k in range(n + 1):
            x = ax + (bx - ax) * k / n; y = ay + (by - ay) * k / n
            for dx in range(-int(w / 2) - 1, int(w / 2) + 2):
                d = abs(dx)
                c = cols[2] if d <= w * .2 else cols[1] if d <= w / 2 else cols[0]
                if d <= w / 2 + 1: cv.put(x + dx, y, c)

def bolt(W, H, i, n, seed, w=5, branches=3, x_top=None):
    """Rayo que cae desde arriba hasta el suelo (abajo-centro, y = H - 8). i: 0 recién caído (blanco) … n-1 se
    apaga; los cuadros se alternan un poco (parpadeo)."""
    cv = Canvas(W, H); rnd = random.Random(seed + (i % 2))
    x1, y1 = W / 2, H - 8
    x0 = x_top if x_top is not None else W / 2 + rnd.uniform(-W * .2, W * .2)
    main = zigzag(x0, 0, x1, y1, rnd, .22 * min(1, W / 120), 7)
    main = [(min(W - 3 - w, max(3 + w, x)), y) for x, y in main]       # que no se salga de la celda
    fade = i / max(1, n - 1)
    cols = [Ca(EL[1], 200 * (1 - fade)), C(EL[3]) if fade < .6 else Ca(EL[2], 220), C(EL[4]) if fade < .5 else C(EL[3])]
    ww = w * (1 - .5 * fade)
    for b in range(branches):                                  # ramas que salen del tronco del rayo
        p = main[rnd.randrange(len(main) // 5, len(main) * 3 // 5)]
        ang = math.pi / 2 + rnd.uniform(-1.1, 1.1); L = rnd.uniform(.15, .35) * H
        br = zigzag(p[0], p[1], p[0] + math.cos(ang) * L * .6, p[1] + math.sin(ang) * L, rnd, .3, 5)
        stroke(cv, br, max(1, ww * .4), [Ca(EL[1], 160 * (1 - fade)), Ca(EL[2], 230), C(EL[3])])
    stroke(cv, main, ww, cols)
    return cv

def impact(W, H, i, n, big=True, seed=0):
    """Destello y chispas donde cae el rayo (centro abajo, el suelo en y = H*.62)."""
    cv = Canvas(W, H); t = i / (n - 1); rnd = random.Random(seed)
    cx, cy = W / 2, H * .62
    R = (W * .45) * (.3 + .7 * t ** .5)
    if t < .5:                                                   # destello: elipse blanca que se abre
        for y in range(int(cy - R * .5), int(cy + R * .5) + 1):
            for x in range(int(cx - R), int(cx + R) + 1):
                d = ((x - cx) / R) ** 2 + ((y - cy) / (R * .45)) ** 2
                if d < 1: cv.put(x, y, Ca(EL[4] if d < .3 else EL[3] if d < .65 else EL[2], 255 * (1 - t * 1.6) * (1 - d * .5)))
    for k in range(18 if big else 10):                          # chispas que saltan
        an = rnd.uniform(math.pi, 2 * math.pi) if k % 3 else rnd.uniform(0, math.pi)
        sp = rnd.uniform(.3, 1) * R * 1.3
        x = cx + math.cos(an) * sp * (.4 + t); y = cy + math.sin(an) * sp * .55 * (.4 + t) - 6 * math.sin(t * math.pi)
        if t < .9:
            cv.put(x, y, C(EL[4] if k % 2 else EL[3])); cv.put(x - math.cos(an), y - math.sin(an) * .55, C(EL[2]))
    if t > .2:                                                   # arcos cortos sobre el suelo
        for k in range(5 if big else 3):
            an = rnd.uniform(0, 2 * math.pi)
            p = zigzag(cx, cy, cx + math.cos(an) * R * .9, cy + math.sin(an) * R * .4, rnd, .4, 3)
            if rnd.random() < 1 - t * .8: stroke(cv, p, 1, [Ca(EL[1], 120), C(EL[2]), C(EL[3])])
    return cv

def scorch(i, n=4, W=64, H=40):
    """Quemadura que deja el rayo (se desvanece): mancha negra en estrella con ascuas azules."""
    cv = Canvas(W, H); rnd = random.Random(4); t = i / (n - 1); cx, cy = W / 2, H / 2
    for y in range(H):
        for x in range(W):
            an = math.atan2(y - cy, x - cx)
            r = 13 * (1 + .35 * math.sin(an * 7 + 1)) * (1 - .25 * t)
            d = math.hypot(x - cx, (y - cy) * 1.8)
            if d < r and rnd.random() < 1 - t * .6: cv.put(x, y, Ca(SCORCH[0 if d < r * .5 else 1], 230 * (1 - t * .5)))
    if t < .7:
        for k in range(6):
            an = k / 6 * 2 * math.pi; cv.put(cx + math.cos(an) * 6, cy + math.sin(an) * 3.3, C(EL[2] if k % 2 else EL[3]))
    return cv

def mark_big(i, n=10, W=96, H=80):
    """Señal del rayo grande que sigue al jugador (centro = el jugador). Cuadros 0-7: la carga (chispas y corriente
    que crecen; el código elige el cuadro según el tiempo que falta). 8-9: se fija, blanco que late (justo antes)."""
    cv = Canvas(W, H); cx, cy = W / 2, H / 2; rnd = random.Random(i * 13 + 1)
    charge = min(1, i / 7); locked = i >= 8
    r = 34 - (6 if locked else 0)
    nseg = 40
    for k in range(nseg):                                        # círculo con corriente que lo recorre
        a = k / nseg * 2 * math.pi
        if not locked and math.sin(a * 3 + i * 1.3) < -.6 + charge: continue
        x = cx + math.cos(a) * r; y = cy + math.sin(a) * r * .55
        cv.put(x, y, C(EL[4] if locked else EL[3] if charge > .5 else EL[2]))
        if charge > .3 or locked: cv.put(x, y + 1, C(EL[1]))
    for k in range(int(3 + 9 * charge) + (6 if locked else 0)):  # chispas y arcos hacia dentro
        a = rnd.uniform(0, 2 * math.pi)
        p0 = (cx + math.cos(a) * r, cy + math.sin(a) * r * .55)
        p1 = (cx + math.cos(a) * r * rnd.uniform(.4, .8), cy + math.sin(a) * r * .55 * rnd.uniform(.4, .8))
        stroke(cv, zigzag(*p0, *p1, rnd, .5, 2), 1, [Ca(EL[1], 100), C(EL[2]), C(EL[4] if locked else EL[3])])
    for k in range(4):                                           # 4 rayitos en cruz (se distingue del picado)
        a = k * math.pi / 2 + i * .2
        for j in range(4 + int(4 * charge)):
            cv.put(cx + math.cos(a) * (r + 3 + j), cy + math.sin(a) * (r + 3 + j) * .55, C(EL[3] if j < 3 else EL[2]))
    return cv

def mark_small(i, n=8, W=48, H=40):
    """Señal de un rayo suelto en el terreno (centro = donde caerá): runa de chispas pequeña que crece y se
    enciende; el último cuadro es justo antes de caer."""
    cv = Canvas(W, H); cx, cy = W / 2, H / 2; rnd = random.Random(i * 7 + 3)
    t = i / (n - 1); r = 10 + 6 * t
    for k in range(24):
        a = k / 24 * 2 * math.pi
        if math.sin(a * 4 + i) < -.2 + t * 1.2:
            cv.put(cx + math.cos(a) * r, cy + math.sin(a) * r * .55, C(EL[3] if t > .6 else EL[2]))
    for k in range(int(2 + 6 * t)):
        a = rnd.uniform(0, 2 * math.pi); d = rnd.uniform(0, r)
        cv.put(cx + math.cos(a) * d, cy + math.sin(a) * d * .55, C(EL[4] if rnd.random() < t else EL[2]))
    cv.put(cx, cy, C(EL[4])); cv.put(cx - 1, cy, C(EL[3])); cv.put(cx + 1, cy, C(EL[3]))
    return cv

def ring(i, n=16, W=96, H=80):
    """Corriente circular (~4 s a 250 ms): anillo de arcos que se expande un poco (de ~1,6 a ~2,6 u) y se apaga."""
    cv = Canvas(W, H); cx, cy = W / 2, H / 2; t = i / (n - 1); rnd = random.Random(i * 5 + 2)
    r = 26 + 16 * t ** .8
    life = 1 - t
    nseg = 14
    pts = []
    for k in range(nseg + 1):
        a = k / nseg * 2 * math.pi
        rr = r + rnd.uniform(-2.5, 2.5)
        pts.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr * .55))
    for (a_, b_) in zip(pts, pts[1:]):
        if rnd.random() < .35 + .6 * life:
            stroke(cv, zigzag(*a_, *b_, rnd, .45, 2), 1 + (1 if life > .6 else 0),
                   [Ca(EL[1], 140 * life + 40), C(EL[2]), C(EL[4] if life > .5 else EL[3])])
    for k in range(int(3 + 6 * life)):
        a = rnd.uniform(0, 2 * math.pi)
        cv.put(cx + math.cos(a) * (r + rnd.uniform(-4, 4)), cy + math.sin(a) * (r + rnd.uniform(-4, 4)) * .55 - rnd.uniform(0, 4), C(EL[4]))
    return cv

# =================================================================================================
# Charca electrificada (sobre las charcas del escenario, misma forma)
# =================================================================================================
PUDDLES = {'Round': 34, 'Kidney': 46, 'Lobed': 46, 'Splash': 44, 'Island': 50, 'Long': 58, 'Chain': 62}

def puddle_electric(shape, i, n=6):
    fw = PUDDLES[shape]
    src = Image.open(os.path.join(PROPS, f'Puddle_Water_{shape}.png')).convert('RGBA')
    src = src.crop((0, 0, fw, src.height))
    a = src.getchannel('A').load(); W, H = src.size
    cv = Canvas(W, H); rnd = random.Random(i * 17 + len(shape))
    inside = [(x, y) for y in range(H) for x in range(W) if a[x, y] > 100]
    edge = [(x, y) for (x, y) in inside if any(not (0 <= x + dx < W and 0 <= y + dy < H) or a[x + dx, y + dy] <= 100
                                                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for (x, y) in inside:                                         # brillo azul sobre el agua
        if (x * 3 + y * 5 + i) % 7 == 0: cv.put(x, y, Ca(EL[2], 90))
    for (x, y) in edge:                                           # orilla que chisporrotea
        if rnd.random() < .45: cv.put(x, y, C(EL[3] if rnd.random() < .6 else EL[2]))
    for k in range(max(2, len(inside) // 120)):                   # arcos que cruzan el agua
        p0 = inside[rnd.randrange(len(inside))]; p1 = inside[rnd.randrange(len(inside))]
        if math.hypot(p1[0] - p0[0], p1[1] - p0[1]) > 26: continue
        pts = [p for p in zigzag(*p0, *p1, rnd, .5, 3) if 0 <= int(p[0]) < W and 0 <= int(p[1]) < H and a[int(p[0]), int(p[1])] > 100]
        if len(pts) > 1: stroke(cv, pts, 1, [Ca(EL[1], 120), C(EL[3]), C(EL[4])])
    return cv

# =================================================================================================
# Sigillaria quemada que hace de pararrayos (un solo uso)
# =================================================================================================
def sig_base():
    t = tram('Burnt')
    return S.sigillaria(t, 380 + 2)                               # la misma que Obstacle_Sigillaria de la quemada

def split_canvas(base, open_, fall, cut_y):
    """Parte el tronco por la mitad desde arriba hasta cut_y: cada mitad se inclina hacia fuera (open_ en px en la
    punta) y fall la tumba más. Celda 72×72 (la de 48×66 centrada abajo)."""
    W, H = 72, 72; ox, oy = (W - base.w) // 2, H - base.h - 2
    cv = Canvas(W, H)
    cx = base.w / 2
    for (x, y), c in base.px.items():
        if y >= cut_y: cv.put(x + ox, y + oy, c); continue
        side = -1 if x < cx else 1
        k = (cut_y - y) / max(1, cut_y)
        dx = side * (open_ * k + fall * k * k)
        dy = fall * .25 * k * k
        cv.put(x + ox + dx, y + oy + dy, c)
    return cv, ox, oy

def sig_struck(i, n=8):
    """El rayo la alcanza: destello en la punta, se raja de arriba abajo y las dos mitades se abren; prende."""
    base = sig_base(); t = i / (n - 1)
    cv, ox, oy = split_canvas(base, 6 * t ** .8, 10 * max(0, t - .4), 44)
    if i < 3:                                                     # destello azul que baja por el tronco
        for y in range(0, int(44 * (i + 1) / 3)):
            cv.put(ox + base.w / 2, oy + y, C(EL[4] if y % 3 else EL[3])); cv.put(ox + base.w / 2 + 1, oy + y, C(EL[2]))
    if t > .4:                                                    # fuego en la herida y en las puntas
        for j, (x, y) in enumerate(((ox + 20, oy + 40), (ox + 28, oy + 38), (ox + 14, oy + 26), (ox + 34, oy + 24))):
            RX.vflame(cv, x + (j % 2) * 2, y, 6 + 8 * t, i * 1.3 + j, w=2.2)
    return cv

def sig_burning(i, k_fire=1.0, n=4):
    """Partida y ardiendo (bucle). k_fire baja con la lluvia: 1 = Burning, .45 = Smolder (llamas pequeñas y humo)."""
    base = sig_base()
    cv, ox, oy = split_canvas(base, 6, 6, 44)
    for j, (x, y) in enumerate(((ox + 20, oy + 40), (ox + 28, oy + 38), (ox + 12, oy + 24), (ox + 36, oy + 22), (ox + 24, oy + 44))):
        h = (8 + 6 * ((i + j) % 3)) * k_fire
        if h > 2: RX.vflame(cv, x, y, h, i * 1.4 + j, w=2.4 * k_fire + .6)
    if k_fire < .8:                                               # humo gris que sube
        RX.cloud(cv, [(ox + 22 + (i % 2) * 3, oy + 12 - i * 2, 4 + i, .6), (ox + 30, oy + 6 - i * 2, 3 + i * .7, .45)],
                 ['#3a3436', '#5a5254', '#847a78', '#b0a6a0'])
    return cv

def sig_split():
    """Apagada: las dos mitades negras con alguna ascua (se queda así)."""
    base = sig_base()
    cv, ox, oy = split_canvas(base, 6, 6, 44)
    for (x, y) in ((ox + 20, oy + 41), (ox + 27, oy + 39)): cv.put(x, y, C(EMBER[1]))
    return cv

# =================================================================================================
def rayos():
    os.makedirs(os.path.join(OUT, 'Effects'), exist_ok=True); os.makedirs(os.path.join(OUT, 'Burnt'), exist_ok=True)
    fx = {
        'Fx_Bolt_Big_Mark': [mark_big(i) for i in range(10)],
        'Fx_Bolt_Big': [bolt(96, 360, i, 5, 11, w=7, branches=4) for i in range(5)],
        'Fx_Bolt_Big_Impact': [impact(128, 96, i, 6, True, 3) for i in range(6)],
        'Fx_Bolt_Huge': [bolt(192, 420, i, 6, 23, w=11, branches=8) for i in range(6)],
        'Fx_Bolt_Small_Mark': [mark_small(i) for i in range(8)],
        'Fx_Bolt_Small': [bolt(48, 360, i, 4, 31, w=3, branches=2) for i in range(4)],
        'Fx_Bolt_Small_Impact': [impact(64, 48, i, 5, False, 5) for i in range(5)],
        'Fx_Bolt_Scorch': [scorch(i) for i in range(4)],
        'Fx_Bolt_Ring': [ring(i) for i in range(16)],
    }
    for name, fr in fx.items(): save_sheet(os.path.join(OUT, 'Effects', name + '.png'), fr)
    sc = {f'Puddle_Water_{sh}_Electric': [puddle_electric(sh, i) for i in range(6)] for sh in PUDDLES}
    sc['Sigillaria_Struck'] = [sig_struck(i) for i in range(8)]
    sc['Sigillaria_Burning'] = [sig_burning(i, 1.0) for i in range(4)]
    sc['Sigillaria_Smolder'] = [sig_burning(i, .45) for i in range(4)]
    sc['Sigillaria_Split'] = [sig_split()]
    for name, fr in sc.items(): save_sheet(os.path.join(OUT, 'Burnt', name + '.png'), fr)
    small = {k: v for k, v in fx.items() if k not in ('Fx_Bolt_Big', 'Fx_Bolt_Huge', 'Fx_Bolt_Small')}
    small.update(sc)
    preview(os.path.join(OUT, 'vista_previa_6f2a_efectos.png'), small, BGC, scale=3)
    preview(os.path.join(OUT, 'vista_previa_6f2a_rayos.png'), {k: fx[k] for k in ('Fx_Bolt_Big', 'Fx_Bolt_Huge', 'Fx_Bolt_Small')}, BGC, scale=1)
    ms = {'Mark': 120, 'Impact': 60, 'Bolt_Big': 50, 'Huge': 50, 'Bolt_Small': 50, 'Scorch': 500, 'Ring': 250, 'Electric': 70,
          'Struck': 80, 'Burning': 110, 'Smolder': 140, 'Split': 500}
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    for name, fr in list(fx.items()) + list(sc.items()):
        if len(fr) > 1:
            m = next((v for k, v in ms.items() if k in name), 90)
            gif(os.path.join(OUT, 'gif', name + '.gif'), fr, m, BGC, scale=2 if 'Bolt_' in name and 'Mark' not in name and 'Impact' not in name else 3)
    print('Listo:', OUT)

# =================================================================================================
# 6f-2b: ambiente de la tormenta
# =================================================================================================
RAIN = [(120, 150, 190, 90), (170, 200, 230, 130), (220, 236, 250, 170)]
DARK = (12, 20, 40)                                              # capa de oscuridad (el código: alfa ~0,5)
FLASH = (232, 244, 255)                                          # destello del relámpago (alfa ~0,85 que baja en 0,25 s)

def rain_tile(i, n=4, T=128, drops=70, seed=1, light=False):
    """Lluvia (128×128, se repite en mosaico; capa de pantalla): gotas diagonales que bajan hacia la izquierda.
    light = llovizna del final (menos gotas y más tenues)."""
    cv = Canvas(T); rnd = random.Random(seed)
    for k in range(drops if not light else drops // 3):
        x0 = rnd.uniform(0, T); y0 = rnd.uniform(0, T); L = rnd.choice((5, 6, 8)); c = RAIN[rnd.randrange(3) if not light else 0]
        y0 = (y0 + i * T / n) % T; x0 = (x0 - i * T / n * .35) % T
        for q in range(L):
            cv.put((x0 - q * .35) % T, (y0 + q) % T, c)
    return cv

def rain_splash(i, n=4):
    """Gota que choca contra el suelo (12×8): el código las reparte al azar."""
    cv = Canvas(12, 8); t = i / (n - 1)
    if i == 0: cv.put(6, 6, RAIN[2]); cv.put(6, 5, RAIN[1])
    else:
        r = 1 + 4 * t
        for s_ in (-1, 1):
            cv.put(6 + s_ * r, 6 - 2 * math.sin(t * math.pi), RAIN[2 if t < .6 else 1])
        cv.put(6, 6, RAIN[0])
    return cv

def ripple(i, n=5):
    """Onda de una gota sobre una charca (16×10)."""
    cv = Canvas(16, 10); t = i / (n - 1); r = 1 + 6 * t
    for k in range(20):
        a = k / 20 * 2 * math.pi
        cv.put(8 + math.cos(a) * r, 5 + math.sin(a) * r * .45, RAIN[2 if t < .4 else 1 if t < .8 else 0])
    return cv

def puddle_grow(shape, i, n=6):
    """Charco nuevo que va creciendo con la lluvia: la charca del escenario recortada por una elipse que crece
    (mismo tamaño de celda; el último cuadro = la charca entera, Puddle_Water_<Forma>)."""
    fw = PUDDLES[shape]
    src = Image.open(os.path.join(PROPS, f'Puddle_Water_{shape}.png')).convert('RGBA')
    src = src.crop((0, 0, fw, src.height)); W, H = src.size
    t = (i + 1) / n; out = Image.new('RGBA', (W, H), (0, 0, 0, 0)); sp = src.load(); op = out.load()
    for y in range(H):
        for x in range(W):
            d = ((x + .5 - W / 2) / (W / 2)) ** 2 + ((y + .5 - H / 2) / (H / 2)) ** 2
            if sp[x, y][3] and d <= t * t * 1.05 + (.06 if (x * 7 + y * 3) % 5 == 0 else 0): op[x, y] = sp[x, y]
    return out

def cloud_shadow(T=256):
    """Sombra de nubes sobre el suelo (256×256, se repite; el código la desliza despacio). Manchas tramadas."""
    cv = Canvas(T); rnd = random.Random(5)
    blobs = [(rnd.uniform(0, T), rnd.uniform(0, T), rnd.uniform(30, 70)) for _ in range(7)]
    for y in range(T):
        for x in range(T):
            v = 0
            for (bx, by, r) in blobs:
                for ox in (-T, 0, T):
                    for oy in (-T, 0, T):
                        d = math.hypot(x - bx - ox, (y - by - oy) * 1.4) / r
                        if d < 1: v = max(v, 1 - d)
            if v > 0 and dither(v * 1.3, x, y): cv.put(x, y, (4, 8, 20, 70))
    return cv

def wind_ash(i, n=4, T=128):
    """Viento con ceniza de la entrada a la tormenta (128×128, mosaico, hacia la derecha): rayas y ceniza, alguna
    ascua."""
    cv = Canvas(T); rnd = random.Random(8)
    for k in range(40):
        x0 = rnd.uniform(0, T); y0 = rnd.uniform(0, T); L = rnd.choice((4, 7, 10))
        x0 = (x0 + i * T / n) % T
        c = C('#ffb24a') if k % 13 == 0 else (150, 140, 130, 150) if k % 2 else (100, 94, 90, 140)
        for q in range(L if k % 13 else 2): cv.put((x0 + q) % T, y0 + math.sin((x0 + q) * .1) * 1.5, c)
    return cv

def steam(i, n=6):
    """Vapor que sube del suelo cuando la lluvia se calma (48×48, bucle)."""
    cv = Canvas(48, 48)
    for j in range(3):
        t = ((i / n) + j / 3) % 1
        RX.cloud(cv, [(14 + j * 10 + math.sin(t * 6 + j) * 2, 42 - 34 * t, 3 + 5 * t, .5 * (1 - t))], ['#5a6070', '#7a8090', '#a0a8b4', '#c8d0d8'])
    return cv

def ambiente():
    os.makedirs(os.path.join(OUT, 'Effects'), exist_ok=True); os.makedirs(os.path.join(OUT, 'Burnt'), exist_ok=True)
    fx = {'Fx_Storm_Rain': [rain_tile(i) for i in range(4)], 'Fx_Storm_Drizzle': [rain_tile(i, light=True) for i in range(4)],
          'Fx_Storm_RainSplash': [rain_splash(i) for i in range(4)], 'Fx_Storm_Ripple': [ripple(i) for i in range(5)],
          'Fx_Storm_CloudShadow': [cloud_shadow()], 'Fx_Storm_WindAsh': [wind_ash(i) for i in range(4)],
          'Fx_Storm_Steam': [steam(i) for i in range(6)]}
    for name, fr in fx.items(): save_sheet(os.path.join(OUT, 'Effects', name + '.png'), fr)
    gr = {f'Puddle_Water_{sh}_Grow': [puddle_grow(sh, i) for i in range(6)] for sh in ('Round', 'Kidney', 'Splash')}
    for name, fr in gr.items(): save_sheet(os.path.join(OUT, 'Burnt', name + '.png'), fr)
    rows = {k: v for k, v in fx.items() if k != 'Fx_Storm_CloudShadow'}; rows.update(gr)
    preview(os.path.join(OUT, 'vista_previa_6f2b_ambiente.png'), rows, C('#2a2a30'), scale=2)
    print('Listo:', OUT)

if __name__ == '__main__':
    if 'rayos' in sys.argv[1:]: rayos()
    elif 'ambiente' in sys.argv[1:]: ambiente()
    else: print(__doc__)
