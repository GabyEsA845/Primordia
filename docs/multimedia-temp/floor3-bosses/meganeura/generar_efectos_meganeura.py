"""Efectos y escenario del combate de la Meganeura (tanda 6e). Skill primordia-sprites (pixel.py).

6e-2: el Lepidodendron partido grande de la sala (presentación y blanco del picado), las astillas al arrancarse y la
nube de ceniza al caer al suelo.

- Burnt/Lepido_Split_Idle (64×96, 4): tronco partido carbonizado de ~3 u con la rotura llena de ascuas que laten.
  Colisión ≈ 2×1 u al pie. Pivote abajo-centro (el pie del tronco en y = 90).
- Burnt/Lepido_Split_Hit (64×96, 4): el picado se clava: tiembla y saltan trozos de corteza (el cuadro 0 es el golpe).
- Burnt/Lepido_Split_Gash (64×96, 4): después de arrancarse: herida abierta con astillas claras y ascuas (se queda así).
  La herida está a la altura del cuello clavado (y ≈ 50).
- Effects/Fx_Meganeura_Splinters (64×64, 6): astillas y corteza que saltan al arrancarse (TearFree, cuadro 2).
- Effects/Fx_Meganeura_Crash (96×64, 6): cae al suelo (Fall → Grounded): anillo de ceniza y ascuas que se levantan.

Uso: python generar_efectos_meganeura.py golpes     (6e-2)
     python generar_efectos_meganeura.py momentos   (6e-3: Fx_Meganeura_Screech 160×112, Splash 128×96, DeadRipple 160×80)
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
MT = os.path.normpath(os.path.join(HERE, '..', '..'))
for d in ('floor3-environment', 'floor3-reactions'):
    sys.path.insert(0, os.path.join(MT, d))
_argv = sys.argv; sys.argv = sys.argv[:1]
import generar_escenario_piso3 as S                    # ground_shadow()
import generar_reacciones_piso3 as RX                  # cloud()
sys.argv = _argv
from base_piso3 import *                               # noqa: F401,F403
from PIL import Image

OUT = HERE
BGC = C('#242024')
WOOD = ['#2a1c14', '#4c3828', '#8e7048', '#b8955e', '#dcbc84']      # madera astillada (clara: se lee)
ASH = ['#3a3436', '#5a5254', '#847a78', '#b0a6a0']                  # ceniza que se levanta
BARKC = [C(c) for c in CHAR]

# =================================================================================================
# Lepidodendron partido grande
# =================================================================================================
TX, TW, TOP, FOOT = 17, 30, 24, 88                       # tronco: x, ancho, arriba (rotura) y pie

def lepido(k=0, shake=0, gash=False, hit=None):
    """k = cuadro (latido de las ascuas); shake = desplazamiento; gash = herida abierta; hit = 0..1 golpe."""
    cv = Canvas(64, 96); rnd = random.Random(11)
    tx = TX + shake
    bark_trunk(cv, tx, TW, TOP, FOOT, BARKC, EMBER[2], 5, ember_p=.04 + .02 * (k % 2))
    tops = [TOP + round(abs(math.sin(i * 1.3 + 1)) * 11) + rnd.randrange(0, 3) - (6 if 8 < i < 15 else 0) for i in range(TW)]
    for i in range(TW):                                  # rotura astillada, alta en el medio (picos de madera)
        x = tx + i
        for y in range(0, tops[i]): cv.px.pop((x, y), None)
        cv.put(x, tops[i], C(WOOD[2]) if i < 10 else C(WOOD[1])); cv.put(x, tops[i] + 1, C(CHAR[2]))
        if i % 5 == 2:
            for j in range(1, 5): cv.put(x, tops[i] - j, C(WOOD[1] if j < 3 else WOOD[0]))
    for j, i in enumerate((3, 9, 14, 20, 25)):          # ascuas en la rotura que laten
        c = EMBER[min(3, 1 + (k + j) % 3)]
        cv.put(tx + i, tops[i] + 1, C(c)); cv.put(tx + i, tops[i] + 2, C(EMBER[1]))
        if (k + j) % 4 == 0: cv.put(tx + i, tops[i] - 1, C(EMBER[3]))
    rp = BARKC
    for dx, s in ((tx + 2, -1), (tx + TW - 2, 1), (tx + 8, -1), (tx + TW - 8, 1)):
        root(cv, [(dx, FOOT - 6), (dx + s * 3, FOOT - 2), (dx + s * 7, FOOT + 1), (dx + s * 12, FOOT + 4)],
             5 if dx in (tx + 2, tx + TW - 2) else 3.5, 1.4, rp)
    if gash or hit is not None:                          # herida del picado a la altura del cuello (y ≈ 50)
        gx, gy = tx + TW / 2, 50
        r = 8 if gash else 2.5 + 5.5 * hit
        for y in range(int(gy - r * 1.6), int(gy + r * 1.6) + 1):
            for x in range(int(gx - r * 1.6), int(gx + r * 1.6) + 1):
                th = math.atan2(y + .5 - gy, x + .5 - gx)
                re = r * (1 + .28 * math.sin(3 * th + 1) + .16 * math.sin(7 * th + 2))       # boca irregular
                d = math.hypot((x + .5 - gx) / 1.1, y + .5 - gy) / re
                if (x, y) not in cv.px: continue
                if d < .7: cv.px[(x, y)] = C('#0e0a0c') if d < .5 else C(CHAR[0])
                elif d < 1 and (math.sin(th * 11 + x) > -.3): cv.px[(x, y)] = C(WOOD[3] if th < 0 else WOOD[2])
        if gash:
            rnd2 = random.Random(4)
            for a in range(9):                           # astillas sueltas y grietas que salen de la herida
                an = a / 9 * 2 * math.pi + rnd2.uniform(-.3, .3); L = rnd2.randint(3, 7)
                re = r * (1 + .28 * math.sin(3 * an + 1) + .16 * math.sin(7 * an + 2))
                for j in range(L):
                    col = WOOD[3] if j < 2 and a % 2 else CHAR[0]
                    px_ = (round(gx + math.cos(an) * (re + j) * 1.1), round(gy + math.sin(an) * (re + j)))
                    if px_ in cv.px: cv.px[px_] = C(col)
            for j, an in enumerate((.6, 2.2, 3.9, 5.2)):
                cv.put(gx + math.cos(an) * r * .55, gy + math.sin(an) * r * .45, C(EMBER[2 + (k + j) % 2]))
    cv.outline(); cv.clean(); S.ground_shadow(cv, tx + TW / 2, FOOT + 2, 24, 4)
    if hit is not None:                                  # trozos de corteza que saltan
        rnd = random.Random(3)
        for j in range(10):
            an = rnd.uniform(-math.pi, 0); sp = rnd.uniform(8, 20) * (hit + .2)
            x = TX + TW / 2 + math.cos(an) * sp * 1.4; y = 50 + math.sin(an) * sp + 12 * hit * hit
            cv.put(x, y, C(WOOD[3] if j % 2 else WOOD[2])); cv.put(x + 1, y, C(CHAR[3])); cv.put(x, y + 1, C(CHAR[1]))
    return cv

# =================================================================================================
# Astillas al arrancarse y nube de ceniza al caer
# =================================================================================================
def splinters(i, n=6):
    cv = Canvas(64, 64); t = i / (n - 1); rnd = random.Random(5)
    RX.cloud(cv, [(32, 34, 5 + 9 * t, .8 - .7 * t), (26, 30, 3 + 6 * t, .6 - .5 * t)], ASH)
    for j in range(16):
        an = rnd.uniform(0, 2 * math.pi); sp = rnd.uniform(10, 26)
        x = 32 + math.cos(an) * sp * t; y = 32 + math.sin(an) * sp * t * .8 + 14 * t * t
        L = rnd.choice((2, 3, 4)); rot = an + t * rnd.uniform(-4, 4)
        for q in range(L):
            col = WOOD[3] if q == 0 else WOOD[2] if j % 3 else CHAR[3]
            cv.put(x + math.cos(rot) * q, y + math.sin(rot) * q, C(col))
    if t < .7:
        for j in range(4): cv.put(32 + (j - 1.5) * 4 * (1 + t), 32 - 6 * t, C(EMBER[3 - j % 2]))
    return cv

def crash(i, n=6):
    cv = Canvas(96, 64); t = i / (n - 1); rnd = random.Random(8)
    R = 10 + 34 * t ** .7
    blobs = [(48 + math.cos(a) * R, 40 + math.sin(a) * R * .45, 5 + 7 * t, .95 - .85 * t)
             for a in [k / 22 * 2 * math.pi for k in range(22)]]
    blobs.append((48, 38 - 8 * t, 8 + 6 * t, .7 - .6 * t))
    RX.cloud(cv, blobs, ASH)
    for j in range(12):                                  # ascuas que saltan del suelo quemado
        an = rnd.uniform(math.pi, 2 * math.pi); sp = rnd.uniform(10, 34)
        x = 48 + math.cos(an) * sp * (.3 + t); y = 40 + math.sin(an) * sp * .7 * math.sin(min(1, t * 1.4) * math.pi) - 2
        if t < .95: cv.put(x, y, C(EMBER[3] if j % 3 == 0 else EMBER[2]))
    return cv

def golpes():
    os.makedirs(os.path.join(OUT, 'Burnt'), exist_ok=True); os.makedirs(os.path.join(OUT, 'Effects'), exist_ok=True)
    rows = {
        'Lepido_Split_Idle': [lepido(k) for k in range(4)],
        'Lepido_Split_Hit': [lepido(k, shake=s, hit=h) for k, (s, h) in enumerate(((2, 0), (-2, .35), (1, .7), (0, 1.0)))],
        'Lepido_Split_Gash': [lepido(k, gash=True) for k in range(4)],
    }
    for name, fr in rows.items(): save_sheet(os.path.join(OUT, 'Burnt', name + '.png'), fr)
    fx = {'Fx_Meganeura_Splinters': [splinters(i) for i in range(6)], 'Fx_Meganeura_Crash': [crash(i) for i in range(6)]}
    for name, fr in fx.items(): save_sheet(os.path.join(OUT, 'Effects', name + '.png'), fr)
    rows.update(fx)
    preview(os.path.join(OUT, 'vista_previa_6e2_efectos.png'), rows, BGC, scale=3)
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    for name, fr in rows.items(): gif(os.path.join(OUT, 'gif', name + '.gif'), fr, 90 if 'Lepido' in name else 70, BGC, scale=4)
    print('Listo:', OUT)

# =================================================================================================
# 6e-3: chillido, salpicadura y restos en la charca
# =================================================================================================
SCREAM = ['#5a1a14', '#a8341c', '#e05a3a', '#ffb08a', '#fff0e0']    # ondas del chillido (rojo de furia, se lee sobre todo)
WATERC = [C(c) for c in WATER]

def ring(cv, cx, cy, r, sq, col, w=1, gaps=0, ph=0.0):
    n = max(24, int(2 * math.pi * r))
    for k in range(n):
        a = k / n * 2 * math.pi
        if gaps and math.sin(a * gaps + ph) < -.35: continue
        for d in range(w):
            cv.put(cx + math.cos(a) * (r - d), cy + math.sin(a) * (r - d) * sq, col)

def screech(i, n=8):
    """Ondas del chillido (160×112): 3 anillos que salen de la cabeza (centro de la celda) y se abren y apagan.
    La primera sale con el cuadro 2 de Meganeura_Screech."""
    cv = Canvas(160, 112)
    for j in range(3):
        t = (i - j * 2) / (n - 1)
        if t < 0 or t > 1: continue
        r = 8 + 66 * t
        col = C(SCREAM[4 if t < .2 else 3 if t < .5 else 2 if t < .8 else 1])
        ring(cv, 80, 56, r, .62, col, w=2 if t < .5 else 1, gaps=5, ph=j)
        if t < .6: ring(cv, 80, 56, r - 3, .62, C(SCREAM[1]), w=1, gaps=7, ph=j + 1)
    if i < 3:                                            # fogonazo en la boca
        for a in range(8):
            an = a / 8 * 2 * math.pi
            for q in range(2 + i * 2): cv.put(80 + math.cos(an) * (3 + q), 56 + math.sin(an) * (3 + q) * .62, C(SCREAM[4 if q < 2 else 3]))
    return cv

def splash(i, n=7, k=1.0):
    """Se estrella en la charca (128×96 con k = 1; el agua en y = 70·k): columna de agua, corona de gotas gruesas,
    espuma y anillos. k = .55: la del picado sobre una charca (70×52)."""
    W, H = int(128 * k), int(96 * k); X, Y = W / 2, 70 * k
    cv = Canvas(W, H); t = i / (n - 1); rnd = random.Random(12)
    ring(cv, X, Y, (10 + 46 * t) * k, .4, WATERC[4] if t < .5 else WATERC[3], w=2 if t < .6 and k > .7 else 1)
    if t > .2: ring(cv, X, Y, (4 + 30 * (t - .2)) * k, .4, WATERC[3], w=1)
    h = 46 * k * math.sin(min(1, t * 1.6) * math.pi / 2) * (1 - max(0, t - .5) * 1.6)
    for q in range(int(max(0, h))):
        w = (2 + 7 * (1 - q / max(1, h)) ** 1.5) * k + (1 if q % 3 == 0 else 0)
        for x in range(int(-w), int(w) + 1):
            col = WATERC[4] if abs(x) < w * .4 else WATERC[3] if abs(x) < w * .8 else WATERC[2]
            if rnd.random() < .85: cv.put(X + x, Y - 2 - q, col)
    for j in range(int(26 * k)):
        an = rnd.uniform(math.pi * 1.05, math.pi * 1.95); sp = rnd.uniform(16, 50) * k
        hh = math.sin(min(1, t * 1.25) * math.pi) * sp * .85
        x = X + math.cos(an) * sp * .95 * t; y = Y - hh + rnd.uniform(-3, 3) * k
        if y < Y and t > 0:
            c = WATERC[4] if j % 3 else C('#e8f8f0')
            cv.put(x, y, c); cv.put(x + 1, y, c); cv.put(x, y + 1, WATERC[3]); cv.put(x + 1, y + 1, WATERC[2])
    if t > .15:
        for j in range(30):
            an = j / 30 * 2 * math.pi; r = (10 + 40 * t) * k * rnd.uniform(.8, 1.05)
            cv.put(X + math.cos(an) * r, Y + math.sin(an) * r * .4, C('#e8f8f0') if j % 2 else WATERC[4])
    return cv

def dead_ripple(i, n=4):
    """Ondas suaves alrededor de los restos que flotan (160×80, bucle)."""
    cv = Canvas(160, 80)
    for j in range(2):
        t = ((i + j * n / 2) % n) / n
        ring(cv, 80, 40, 52 + 22 * t, .42, WATERC[4 if t < .4 else 3], w=1, gaps=4, ph=j * 2 + i * .4)
    return cv

def momentos():
    os.makedirs(os.path.join(OUT, 'Effects'), exist_ok=True)
    fx = {'Fx_Meganeura_Screech': [screech(i) for i in range(8)], 'Fx_Meganeura_Splash': [splash(i) for i in range(7)],
          'Fx_Meganeura_DeadRipple': [dead_ripple(i) for i in range(4)]}
    for name, fr in fx.items(): save_sheet(os.path.join(OUT, 'Effects', name + '.png'), fr)
    preview(os.path.join(OUT, 'vista_previa_6e3_efectos.png'), fx, BGC, scale=3)
    for name, fr in fx.items(): gif(os.path.join(OUT, 'gif', name + '.gif'), fr, 80 if 'Ripple' not in name else 200, BGC, scale=3)
    print('Listo:', OUT)

# =================================================================================================
# 6f-1: marca del picado, impacto, ráfaga del aleteo y racimos de huevos
# =================================================================================================
MARK_F = ['#5a2a0e', '#9a5418', '#e08a2c', '#ffc860', '#fff0b0']   # sigue a la rana: ámbar
MARK_L = ['#5a0e0e', '#a01c16', '#e83a2a', '#ff8a6a', '#ffe0d0']   # fija: rojo
JELLY = [(40, 60, 24, 210), (96, 132, 52, 215), (150, 190, 90, 120), (196, 226, 130, 105), (250, 255, 220, 235)]
LARVA = ['#0a0c06', '#1a2010', '#2e3820']

def dragonfly_icon(cv, cx, cy, col, k=1.0):
    """Silueta pequeña de la libélula (cruz de 4 alas) dentro de la marca."""
    for x in range(-2, 3):
        for y in range(-9, 8):
            if abs(x) <= (1 if y > -6 else 2): cv.put(cx + x * k, cy + y * k * .82, col)
    for s in (-1, 1):
        for j in range(1, 11):
            for y in (-3, 1):
                w = 1 if j > 8 else 2
                for q in range(w): cv.put(cx + s * (2 + j) * k, cy + (y + q - (j * .08 if y < 0 else -j * .05)) * .82 * k, col)

def dive_mark(i, locked=False, n=4):
    """Marca del picado (64×64, centro = donde caerá): círculo de 3 u en picada, silueta de la libélula y 4 muescas
    que apuntan al centro. Sigue: ámbar, late despacio. Fija: rojo, las muescas se cierran y late más fuerte."""
    cv = Canvas(64, 64); P = MARK_L if locked else MARK_F
    t = (i % n) / n; pulse = .5 + .5 * math.sin(t * 2 * math.pi)
    r = 22 + (1.5 * pulse if not locked else 2.5 * pulse)
    ring(cv, 32, 32, r, .82, C(P[3 if pulse > .5 else 2]), w=2)
    ring(cv, 32, 32, r - 3, .82, C(P[1]), w=1, gaps=8, ph=t * 6)
    close = (4 + 3 * pulse) if locked else 0
    for a in range(4):                                   # muescas
        an = a * math.pi / 2 + math.pi / 4
        for j in range(6):
            rr = r + 3 - j - close
            for w_ in (-1, 0, 1):
                if j < 3 or w_ == 0:
                    cv.put(32 + math.cos(an) * rr - math.sin(an) * w_ * (1 - j / 6),
                           32 + (math.sin(an) * rr + math.cos(an) * w_ * (1 - j / 6)) * .82, C(P[4 if j < 2 else 3]))
    dragonfly_icon(cv, 32, 32, C(P[2] if not locked else P[3 if pulse > .5 else 2]), .9)
    return cv

def dive_impact(i, n=7):
    """Picado contra el suelo (112×72, centro = la marca): onda de ceniza, polvo que sube y ascuas."""
    cv = Canvas(112, 72); t = i / (n - 1); rnd = random.Random(21)
    R = 8 + 40 * t ** .7
    RX.cloud(cv, [(56 + math.cos(a) * R, 40 + math.sin(a) * R * .45, 5 + 6 * t, .9 - .85 * t)
                  for a in [k / 24 * 2 * math.pi for k in range(24)]] + [(56, 36 - 10 * t, 7 + 8 * t, .8 - .75 * t)], ASH)
    for j in range(16):
        an = rnd.uniform(math.pi, 2 * math.pi); sp = rnd.uniform(10, 40)
        x = 56 + math.cos(an) * sp * (.3 + t); y = 40 + math.sin(an) * sp * .8 * math.sin(min(1, t * 1.3) * math.pi)
        if t < .95: cv.put(x, y, C(EMBER[3] if j % 3 == 0 else EMBER[2]))
    return cv

def dive_crack(i, n=5):
    """Grieta que deja el picado (64×40), se desvanece: cuadro 0 recién abierta con ascuas, el último casi nada."""
    cv = Canvas(64, 40); rnd = random.Random(9); t = i / (n - 1)
    for a in range(7):
        an = a / 7 * 2 * math.pi + rnd.uniform(-.3, .3); x, y = 32.0, 20.0
        L = rnd.randint(14, 26) * (1 - .3 * t)
        for j in range(int(L)):
            an += rnd.uniform(-.35, .35); x += math.cos(an); y += math.sin(an) * .55
            if rnd.random() < 1 - t * .7: cv.put(x, y, C('#0e0a0c'))
            if rnd.random() < 1 - t * .7: cv.put(x + 1, y, C('#1c1618'))
            if j < 7 and t < .6 and rnd.random() < .6: cv.put(x, y - 1, C(EMBER[2 if t < .3 else 1]))
    return cv

def gust(i, n=6):
    """Ráfaga del aleteo (112×64, sale del borde izquierdo hacia la derecha, ~6 u): cono de rayas de viento claras,
    ceniza y ascuas arrastradas (bucle). El código la gira hacia donde mira el jefe."""
    cv = Canvas(112, 64); rnd = random.Random(31)
    for j in range(14):
        lane = (j / 13 - .5) * 2                               # -1..1 a lo ancho del cono
        ph = (i / n + j * .37) % 1
        x0 = 4 + ph * 90; L = 10 + 8 * (j % 3)
        for q in range(int(L)):
            x = x0 + q
            if x > 108: break
            y = 32 + lane * (6 + x * .22)
            cv.put(x, y, C(DUST[3] if q > L * .4 else DUST[2]) if (q + j) % 7 else C(DUST[1]))
    for j in range(10):                                         # ceniza y ascuas
        ph = (i / n + j * .29) % 1; lane = rnd.uniform(-1, 1)
        x = 6 + ph * 100; y = 32 + lane * (6 + x * .22) + math.sin(ph * 9 + j) * 2
        cv.put(x, y, C(EMBER[2] if j % 4 == 0 else ASH[2])); cv.put(x - 1, y, C(ASH[1]))
    return cv

DUST = ['#5a5040', '#8a8070', '#c8c0b0', '#eae4d8']

def wind_drag(i, n=4):
    """Rayas de arrastre (16×16, bucle) para poner sobre gas, charcos de veneno o proyectiles que empuja el viento
    (hacia la derecha; el código la gira)."""
    cv = Canvas(16, 16)
    for j, y in enumerate((4, 8, 12)):
        x0 = ((i * 4 + j * 5) % 16)
        for q in range(5):
            cv.put((x0 + q) % 16, y, C(DUST[3] if q > 1 else DUST[2]))
    return cv

def jelly_ball(cv, x, y, r, larva_ph=None, pop=0.0):
    """Huevo gelatinoso translúcido con la larva oscura dentro."""
    for yy in range(int(y - r) - 1, int(y + r) + 2):
        for xx in range(int(x - r) - 1, int(x + r) + 2):
            d = math.hypot(xx + .5 - x, (yy + .5 - y) * 1.1) / r
            if d > 1: continue
            c = JELLY[1] if d > .82 else JELLY[2] if (xx - x + yy - y) > 0 else JELLY[3]
            cv.put(xx, yy, c)
    cv.put(x - r * .4, y - r * .45, JELLY[4])                   # brillo
    if larva_ph is not None and r >= 3:                         # larva enroscada, bien visible a través
        lx = x + math.sin(larva_ph) * r * .18; ly = y + .3
        for dx, dy, c in ((0, 0, 0), (1, 0, 0), (-1, 0, 1), (0, 1, 1), (1, -1, 2), (-1, 1 if math.cos(larva_ph) > 0 else -1, 2)):
            cv.put(lx + dx, ly + dy, C(LARVA[c]))
        cv.put(lx + 1.5, ly - .5, C('#3a9a9a'))                   # ojito verde azulado (como la madre)

CLUSTER = [(-7, 2, 4.2), (0, 3, 4.6), (7, 2, 4.0), (-4, -3, 3.8), (4, -3, 4.0), (0, -7, 3.4), (-9, -4, 3.0)]

def eggs(state, i):
    """Racimo de huevos gelatinosos en la orilla de la charca (40×32, pie del racimo en y = 26).
    Appear (4): crece al ponerlo. Idle (4): late y las larvas se mueven. Hatch (6): revientan y sale la ninfa
    (la ninfa aparece en el cuadro 3). Break (6): lo rompes: gelatina que salpica (no sale nada)."""
    cv = Canvas(40, 32); cx, cy = 20, 22
    for j in range(16):                                       # base mojada bajo el racimo
        cv.put(cx - 9 + j * 1.2, cy + 5, JELLY[0])
    if state == 'Appear':
        k = (i + 1) / 4
        for j, (dx, dy, r) in enumerate(CLUSTER):
            if j <= i * 2 + 1: jelly_ball(cv, cx + dx, cy + dy, r * min(1, k * 1.3), None)
        return cv
    if state == 'Idle':
        for j, (dx, dy, r) in enumerate(CLUSTER):
            jelly_ball(cv, cx + dx, cy + dy + (.5 if (i + j) % 4 == 0 else 0), r + (.4 if (i + j) % 4 == 0 else 0), (i + j) * 1.6)
        return cv
    t = i / 5
    rnd = random.Random(2 if state == 'Hatch' else 4)
    for j, (dx, dy, r) in enumerate(CLUSTER):
        burst = t > .2 + j * .06
        if not burst: jelly_ball(cv, cx + dx, cy + dy, r * (1 + .15 * t), (i + j) * 1.6)
        else:                                                    # cáscara rota: un borde de gelatina
            rr = r * (1 - .4 * t)
            for a in range(10):
                an = a / 10 * 2 * math.pi
                if rnd.random() < .7: cv.put(cx + dx + math.cos(an) * rr, cy + dy + math.sin(an) * rr * .9, JELLY[1 if a % 2 else 2])
    if state == 'Break' and i >= 1:                             # salpicadura de gelatina
        for j in range(14):
            an = rnd.uniform(math.pi * 1.1, math.pi * 1.9); sp = rnd.uniform(6, 16) * min(1, t * 1.6)
            x = cx + math.cos(an) * sp; y = cy - 2 + math.sin(an) * sp * .8 + 8 * t * t
            cv.put(x, y, JELLY[3]); cv.put(x + 1, y, JELLY[2])
    return cv

def picado_fx():
    os.makedirs(os.path.join(OUT, 'Effects'), exist_ok=True); os.makedirs(os.path.join(OUT, 'Eggs'), exist_ok=True)
    fx = {'Fx_Meganeura_DiveMark_Follow': [dive_mark(i) for i in range(4)],
          'Fx_Meganeura_DiveMark_Locked': [dive_mark(i, True) for i in range(4)],
          'Fx_Meganeura_DiveImpact': [dive_impact(i) for i in range(7)],
          'Fx_Meganeura_DiveCrack': [dive_crack(i) for i in range(5)],
          'Fx_Meganeura_DiveSplash': [splash(i, 6, k=.55) for i in range(6)],
          'Fx_Meganeura_Gust': [gust(i) for i in range(6)],
          'Fx_Meganeura_WindDrag': [wind_drag(i) for i in range(4)]}
    eg = {f'Meganeura_Eggs_{st}': [eggs(st, i) for i in range(n)] for st, n in (('Appear', 4), ('Idle', 4), ('Hatch', 6), ('Break', 6))}
    for name, fr in fx.items(): save_sheet(os.path.join(OUT, 'Effects', name + '.png'), fr)
    for name, fr in eg.items(): save_sheet(os.path.join(OUT, 'Eggs', name + '.png'), fr)
    rows = dict(fx); rows.update(eg)
    preview(os.path.join(OUT, 'vista_previa_6f1_efectos.png'), rows, BGC, scale=3)
    ms = {'Follow': 160, 'Locked': 90, 'Impact': 70, 'Crack': 400, 'Splash': 70, 'Gust': 60, 'Drag': 80, 'Appear': 100,
          'Idle': 200, 'Hatch': 90, 'Break': 70}
    for name, fr in rows.items():
        gif(os.path.join(OUT, 'gif', name + '.gif'), fr, next(v for k, v in ms.items() if k in name), BGC, scale=4)
    print('Listo:', OUT)

if __name__ == '__main__':
    if 'golpes' in sys.argv[1:]: golpes()
    elif 'momentos' in sys.argv[1:]: momentos()
    elif 'picado' in sys.argv[1:]: picado_fx()
    else: print(__doc__)
