"""Reacciones del piso 3 de Primordia (pantano carbonífero), tanda 5.

Veneno, metano y fuego: gas + ataque = explosión de fuego; fuego + charco de veneno = nube tóxica. Lo que ya estaba
hecho se reutiliza (Fx_Burning, Fx_GroundFire y Fx_MethaneSac_Explode en floor3-mutations; charcos de veneno en
floor3-bodies; niebla, burbujas, fumarolas y helechos que arden en floor3-environment). Aquí va lo que faltaba:

- Nube tóxica (fuego + charco de veneno): columna mostaza espesa que sube del charco (Form, Idle, Fade); no va a ras
  de suelo, para no confundirla con el metano.
- Charco de veneno que se prende: el fuego corre por el charco con llamas de base lima, la nube sale y queda una
  mancha quemada.
- Odre de metano que revienta (por tramo): se hincha, se raja de luz, revienta en jirones y deja los restos.
- Niebla de metano que se prende: casilla que se repite (fogonazo azul → llamas → humo; el código la lanza con retardo
  según la distancia al punto de ignición) y llamaradas sueltas al final.
- Chispa de ignición (fuego que toca gas), marca de envenenado (gotas y burbujas; el código tiñe de lima) y fuego que
  se apaga (vapor; la lluvia de la Meganeura y el final de las llamas).
- Planta venenosa del escenario (por tramo): bulbo lima que respira, se encoge al golpearlo, revienta en un charco de
  veneno, queda la cáscara y rebrota.

Hecho con la skill primordia-sprites (pixel.py): 16 px por unidad, luz arriba a la izquierda, contorno OUTL.
Uso: python generar_reacciones_piso3.py
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
MT = os.path.normpath(os.path.join(HERE, '..'))
for d in ('floor3-environment', 'floor3-bodies', 'floor3-mutations'):
    sys.path.insert(0, os.path.join(MT, d))
_argv = sys.argv; sys.argv = [sys.argv[0], 'none']
import generar_escenario_piso3 as S                    # odre, sombra en el suelo, rampas de plantas por tramo
import generar_efectos_bocas_piso3 as P                # charco de veneno y gotas
import generar_mutaciones_piso3 as M                   # flame(), soft(), SMOKE
sys.argv = _argv
from base_piso3 import *                               # noqa: F401,F403  (pixel.py, paletas, blend, tram...)
from PIL import Image

OUT = HERE
KEYS = ('Swamp', 'Smoke', 'Burnt')
VEN = ['#2e5a14', '#5aa020', '#a8e040', '#e8ffc0']                    # veneno
BULB = ['#1a3a0e', '#2e5a14', '#5aa020', '#a8e040', '#e8ffc0']       # bulbo: veneno con un paso más oscuro
TOX = ['#2a2a12', '#4e4e1a', '#7a7a28', '#b0b03a', '#e4e478']        # nube tóxica: mostaza sucia
FIR = ['#8a1e14', '#d4441c', '#f88c2a', '#ffd25a', '#fff4c0']        # fuego
MET = ['#1e5a78', '#2a90b8', '#6ad0ec', '#d0f8ff']                   # metano
EMB = ['#5a1a14', '#a8341c', '#e8682a', '#ffb24a']                   # ascuas
SMK = M.SMOKE
STEAM = ['#5e6a6c', '#8c989a', '#bac6c6', '#e6eeec']                 # vapor: gris frío, más claro que el humo

def Ca(c, a): return C(c)[:3] + (max(0, min(255, int(a))),)

# =================================================================================================
# Nubes por bolas (cada bola con su opacidad; luz arriba a la izquierda de la bola más cercana)
# =================================================================================================
def cloud(cv, blobs, pal, specks=(), seed=0):
    """blobs: (x, y, r, a). 4 escalones de opacidad como soft() de las mutaciones, sin degradado."""
    if not blobs: return
    x0 = int(min(b[0] - b[2] for b in blobs)) - 1; x1 = int(max(b[0] + b[2] for b in blobs)) + 2
    y0 = int(min(b[1] - b[2] for b in blobs)) - 1; y1 = int(max(b[1] + b[2] for b in blobs)) + 2
    for y in range(max(0, y0), min(cv.h, y1)):
        for x in range(max(0, x0), min(cv.w, x1)):
            d = wa = 0.0; best = None; bw = 0
            for b in blobs:
                q = ((x + .5 - b[0]) ** 2 + (y + .5 - b[1]) ** 2) / (b[2] * b[2])
                if q < 1:
                    w = (1 - q) ** 2; d += w; wa += w * b[3]
                    if w > bw: bw, best = w, b
            if d < 0.08: continue
            a_ = wa / d
            lit = -(x - best[0]) * 0.45 - (y - best[1]) * 0.6
            k = 3 if d > 0.9 and lit > best[2] * 0.3 else 2 if d > 0.45 else 1 if d > 0.2 else 0
            a = (95, 150, 190, 215)[k] * a_
            if a > 10: cv.put(x, y, Ca(pal[k], a))
    for (x, y, c, a) in specks:                                         # motas dentro de la nube
        if (int(x), int(y)) in cv.px: cv.put(x, y, Ca(c, a))

# =================================================================================================
# Llamas
# =================================================================================================
def vflame(cv, x, y, h, ph, pal=None, w=1.6):
    """Llama de h px con su propia rampa de abajo arriba (núcleo, cuerpo, punta). pal None = fuego normal."""
    core, mid, body, tip = pal or (FIR[4], FIR[3], FIR[2], FIR[1])
    for k in range(int(h)):
        t = k / max(1, h)
        ww = (1 - t) * w
        sway = math.sin(ph + t * 3) * t * 1.2
        for d in range(-int(ww + .5), int(ww + .5) + 1):
            col = core if t < 0.25 and d == 0 else mid if t < 0.45 else body if t < 0.75 else tip
            cv.put(x + d + sway, y - k, C(col))

POISON_FIRE = (VEN[3], VEN[2], FIR[2], FIR[1])         # veneno ardiendo: base lima y punta de fuego
BLUE_FIRE = (MET[3], MET[2], MET[1], MET[0])            # metano: fogonazo azul

# =================================================================================================
# 1. Nube tóxica: columna que sube (48×64, pie en y = 58, centro x = 24)
# =================================================================================================
TX, TY = 24, 58
TN = 8                                                  # cuadros del bucle

def toxic_blobs(ph, grow=1.0, fade=0.0):
    """ph: fase del bucle (0..1). grow: lo que ha crecido la columna. fade: se deshace."""
    out = []
    br = math.sin(ph * 2 * math.pi)
    for (dx, dy, r) in ((-10, -2, 7), (10, -2, 7), (0, -6, 9.5), (-4, 0, 6), (5, 0, 6)):   # pie sobre el charco
        out.append((TX + dx * (1 + fade * .5), TY + dy + br * .4 - fade * 3, r * max(.3, min(1, grow * 1.3)) * (1 - fade * .3) + br * .3,
                    1 - fade))
    for k in range(10):                                 # bocanadas que suben, de dos en dos y alternadas
        p = (ph + k // 2 / 5 + (k % 2) * .1) % 1
        if p > grow: continue
        y = TY - 9 - p * 38
        r = 6 + p * 6
        side = (k % 2 * 2 - 1)
        x = TX + side * (2.5 + p * 5) + math.sin(p * 5 + k * 1.3) * 2.5 * p + side * fade * 6 * p
        a = (1 if p < .65 else max(0, (1 - p) / .35)) * (1 - fade)
        out.append((x, y, r * (1 - fade * .2), a))
    return out

def toxic_specks(ph, fade=0.0):
    out = []
    for k in range(6):                                  # motas de veneno que suben más deprisa (es veneno quemado)
        p = (ph * 2 + k / 6) % 1
        x = TX + math.sin(k * 2.3 + p * 4) * 7
        y = TY - 4 - p * 44
        out.append((x, y, VEN[2] if k % 2 else TOX[4], 230 * (1 - p) * (1 - fade)))
        if k % 2: out.append((x, y + 1, VEN[1], 200 * (1 - p) * (1 - fade)))
    for k in range(4):                                  # grumos oscuros: humo espeso y sucio
        p = (ph + k / 4 + .1) % 1
        x = TX + math.sin(k * 1.9 + 1) * 5 + math.sin(p * 6) * 1.5
        y = TY - 8 - p * 38
        for dx, dy in ((0, 0), (1, 0), (0, 1)): out.append((x + dx, y + dy, TOX[1], 200 * (1 - p) * (1 - fade)))
    return out

def toxic(ph, grow=1.0, fade=0.0):
    cv = Canvas(48, 64)
    cloud(cv, toxic_blobs(ph, grow, fade), TOX[1:], toxic_specks(ph, fade) if grow > .5 else ())
    return cv

def toxic_sheets():
    form = [toxic((i - 6) / TN % 1, grow=(i + 1) / 6) for i in range(6)]   # acaba donde empieza el bucle
    idle = [toxic(i / TN) for i in range(TN)]
    fade = [toxic(i / TN, fade=(i + 1) / 5) for i in range(5)]
    return {'Fx_ToxicCloud_Form': form, 'Fx_ToxicCloud_Idle': idle, 'Fx_ToxicCloud_Fade': fade}

# =================================================================================================
# 2. Charco de veneno que se prende (48×48, encima del charco grande y con su mismo centro)
# =================================================================================================
def puddle_ignite(i, n=6):
    """El fuego entra por la izquierda y corre por el charco; las llamas tienen base lima. Cuadro que cuenta: el 3
    (sale la nube tóxica). El último cuadro es la mancha quemada que queda (el código la desvanece)."""
    cv = Canvas(48)
    tmp = Canvas(48)
    pts = P.puddle(tmp, 24, 24, 19.5, 23)
    burnt = (0, 0, .25, .55, .8, 1.0)[i]
    for p, c in tmp.px.items():                          # el veneno se oscurece y se vuelve mostaza al quemarse
        if burnt <= 0: cv.px[p] = c; continue
        lum = (c[0] * .3 + c[1] * .59 + c[2] * .11) / 255
        tgt = C(TOX[0] if lum < .25 else TOX[1] if lum < .45 else TOX[2])
        cv.px[p] = blend(c, tgt, int(255 * burnt))[:3] + (int(c[3] * (1 - .25 * burnt)),)
    ox, oy = 7, 25
    R = (2, 13, 28, 50, 50, 50)[i]
    rnd = random.Random(7)
    sites = sorted([(x + rnd.randint(-1, 1) + (y // 5 % 2) * 3, y) for (x, y) in pts if x % 7 == 3 and y % 5 == 1], key=lambda s: s[1])
    for (x, y) in sites:
        d = math.hypot(x - ox, (y - oy) * 1.3)
        if d > R or not 4 < x < 43 or y < 8: continue
        age = (R - d) / 12                                 # lo recién prendido es bajo
        h = min(1, .4 + age) * (6, 8, 11, 13, 8, 0)[i] + rnd.uniform(-1, 1)
        h = min(h, y - 2)
        if h >= 2: vflame(cv, x, y, h, i * 1.4 + x * .7, POISON_FIRE, w=2.2)
    if i == 0: sparks(cv, ox, oy - 1, 1)
    if i >= 3:                                           # la nube empieza a subir (la de verdad la pone el código)
        s_ = (i - 2) / 3
        cloud(cv, [(18, 15 - s_ * 5, 4 + s_ * 2, .9 - s_ * .4), (29, 14 - s_ * 5, 4 + s_ * 2.5, .8 - s_ * .4)], TOX[1:])
    if i >= n - 2:                                       # ascuas sueltas en la mancha
        for (x, y) in ((14, 22), (22, 27), (31, 21), (27, 31), (18, 32), (35, 26), (10, 28)):
            if (x, y) in pts: cv.put(x, y, C(EMB[3])); cv.put(x + 1, y, C(EMB[2]))
    return cv

# =================================================================================================
# 3. Odre de metano que revienta (por tramo; 32×32 con el pie abajo, como Methane_Sac_Idle)
# =================================================================================================
SAC = S.SAC

def sac_skin(t): return [C(c) for c in SAC] if not t['burnt'] else [blend(C(c), C('#4a4346'), 90) for c in SAC]

def sac(t, s=1.0, glow=0.0, cracks=0):
    """El odre de la tanda 1b a escala s (se hincha), con el gas que brilla (glow) y grietas de luz."""
    cv = Canvas(32)
    rx, ry = 7.5 * s, 6.5 * s
    cx, cy = 16, 27 - ry
    skin = sac_skin(t)
    ellipse(cv, cx, cy, rx, ry, skin)
    gas = [C(c) for c in MET]
    for (x, y), c in list(cv.px.items()):
        d = ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2
        if d < .45 + glow * .3 and (x + y) % 3 != 0: cv.px[(x, y)] = blend(c, gas[1 + int(glow > .5)], 190 if d < .2 else 140)
        if d < .15 + glow * .2: cv.px[(x, y)] = blend(c, gas[2 + int(glow > .5)], 150 + int(80 * glow))
    for a in (0.6, 2.0, 3.6, 5.0):                      # venas (con el gas brillando se ven más)
        for st in range(int(rx * .9)):
            x, y = cx + math.cos(a) * st, cy + math.sin(a) * st * ry / rx
            if (int(x), int(y)) in cv.px and st > 1: cv.put(x, y, gas[0] if glow > .3 else skin[1])
    for k in range(cracks):                             # grietas de luz que se abren
        a = 0.9 + k * 1.7
        for st in range(int(rx * .5), int(rx * 1.05)):
            x, y = cx + math.cos(a) * st, cy + math.sin(a) * st * ry / rx
            cv.put_in(x, y, gas[3]); cv.put_in(x + math.sin(a), y, gas[2])
    cv.put(cx - 3 * s, cy - 3 * s, skin[4]); cv.put(cx - 2 * s, cy - 4 * s, skin[4]); cv.put(cx - 3 * s, cy - 2 * s, gas[3])
    cv.put(cx, cy - ry - .5, skin[2]); cv.put(cx + 1, cy - ry - .5, skin[1])
    sac_roots(cv, t, cx, cy + ry)
    cv.outline(); cv.clean(); S.ground_shadow(cv, 16, 29.5, 9, 1.8)
    return cv

def sac_roots(cv, t, cx, by):
    rp = [C(c) for c in (CHAR if t['burnt'] else t['root'])]
    for s in (-1, 1):
        root(cv, [(cx + s * 3, by - 1), (cx + s * 5, by), (cx + s * 7, by + 1), (cx + s * 9, by + 1.5)], 2, 1, rp)

def sac_remains(t, lift=0.0):
    """Lo que queda: la membrana rota y arrugada sobre las raíces, con la boca desgarrada hacia arriba."""
    cv = Canvas(32)
    skin = sac_skin(t)
    cx, by = 16, 27
    rnd = random.Random(31)
    tops = [by - 2 - 3.5 * abs(math.sin(x * 0.9 + .4)) ** 2 - rnd.random() * 1.5 for x in range(9, 24)]   # borde desgarrado
    def inside(x, y):
        xi = int(x)
        return 9 <= xi < 24 and tops[xi - 9] - lift <= y <= by + .5 - (abs(x - cx) > 6.5) * 1
    fill(cv, inside, cx, by - 2, 7.5, 4, skin)
    for s_ in (-1, 1):                                  # solapas caídas en el suelo
        for k in range(4):
            for j in range(2 - k // 2):
                cv.put(cx + s_ * (8 + k), by - 1 + j + k * .4, skin[2 - j])
    for x in range(11, 22):                             # interior oscuro de la bolsa vacía
        y = int(tops[x - 9] - lift) + 1
        if (x, y) in cv.px: cv.put(x, y, skin[0]); cv.put(x, y + 1, skin[1])
    for x in (12, 15, 19):                              # pliegues
        for y in range(int(by - 2), int(by + 1)): cv.put_in(x, y, skin[1])
    sac_roots(cv, t, cx, by)
    cv.outline(); cv.clean(); S.ground_shadow(cv, 16, 29.5, 9, 1.8)
    return cv

def sac_burst(t, i):
    """Cuadros: 0-1 se hincha y se raja de luz; 2 revienta (el cuadro que cuenta: sale Fx_MethaneSac_Explode);
    3-4 jirones que caen sobre los restos."""
    if i == 0: return sac(t, 1.12, .5, 0)
    if i == 1: return sac(t, 1.24, 1.0, 3)
    cv = sac_remains(t, lift=(2, 1, 0)[i - 2])
    skin = sac_skin(t)
    for k in range(7):                                  # jirones de membrana
        a = -math.pi / 2 + (k - 3) * .45
        d = (9, 12, 11)[i - 2] + (k % 3)
        x = 16 + math.cos(a) * d * (1.1 if i == 2 else .95)
        y = 20 + math.sin(a) * d * .8 + (0, 4, 8)[i - 2] + (k % 2)
        cv.put(x, y, skin[2]); cv.put(x + 1, y, skin[1])
        if i == 2: cv.put(x, y - 1, skin[3])
    if i == 2:                                          # bocanada de gas que sale (la explosión va encima)
        cloud(cv, [(16, 16, 6, .8), (11, 19, 4, .7), (21, 18, 4, .7)], MET)
        sparks(cv, 16, 17, 2)
    return cv

# =================================================================================================
# 4. Niebla de metano que se prende
# =================================================================================================
def mist_ignite(i, n=7):
    """Casilla que se repite (32×24, pie en el borde de abajo): 0 el gas se ilumina, 1 fogonazo azul a ras de suelo,
    2 azul abajo y fuego arriba, 3 fuego alto (el que daña: cuadros 1-3), 4-5 se apaga en humo, 6 humo que se va."""
    cv = Canvas(32, 24)
    rnd = random.Random(5)
    base = [(x, 19 + int(1.5 * math.sin(x * .8 + 1)) + rnd.randint(0, 1)) for x in range(4, 29, 3)]
    if i == 0:                                           # gas que se enciende por dentro
        cloud(cv, [(8, 19, 4.5, .7), (16, 18, 5.5, .8), (24, 19, 4.5, .7)], MET[1:] + [SPARK[0]])
        for x in (6, 15, 24): cv.put(x, 18, C(SPARK[0]))
    hs = {1: (4, 0), 2: (8, .5), 3: (12, 1), 4: (7, 1), 5: (3, 1)}
    if i in hs:
        h, mix = hs[i]
        for k, (x, y) in enumerate(base):
            hh = h + ((k * 7 + i) % 3) - 1
            pal = BLUE_FIRE if mix == 0 or (mix < 1 and k % 2) else None
            vflame(cv, x, y, hh, i * 1.3 + k, pal, w=1.7)
            if mix == .5 and pal is None: vflame(cv, x, y, 3, k, BLUE_FIRE, w=1.2)   # base azul
    if i >= 4:                                           # humo
        s_ = (i - 3) / 3
        cloud(cv, [(9, 13 - s_ * 5, 4 + s_ * 1.5, 1 - s_ * .6), (17, 12 - s_ * 6, 5 + s_ * 1.5, .9 - s_ * .6),
                   (24, 14 - s_ * 4, 3.5 + s_ * 1.5, .9 - s_ * .6)], SMK)
    if i in (4, 5):
        for x in range(3, 30, 2):
            if (x + i) % 3: cv.put(x, 21, C(EMB[1 + (x + i) % 3]))
    return cv

def flare(i, n=6):
    """Llamarada suelta (24×40, pie abajo): lengua de fuego que sube y se apaga en humo."""
    cv = Canvas(24, 40)
    h = (10, 23, 30, 26, 16, 0)[i]
    if h:
        vflame(cv, 12, 37, h, i * 1.1, w=3.6)
        vflame(cv, 8, 37, h * .55, i * 1.1 + 2, w=2.2)
        vflame(cv, 16, 37, h * .6, i * 1.1 + 4, w=2.2)
        if i < 3: vflame(cv, 12, 37, 4, 0, BLUE_FIRE, w=2.5)   # base azul: es gas
    if i >= 3:
        s_ = (i - 2) / 4
        cloud(cv, [(12, 37 - h - 4 - s_ * 8, 4 + s_ * 3, 1 - s_ * .6), (9, 37 - h - s_ * 4, 3 + s_ * 2, .8 - s_ * .5)], SMK)
    if i < 4:
        for k in range(3):                               # chispas que suben
            cv.put(12 + math.sin(k * 2 + i) * 6, max(1, 37 - h - 2 - k * 3), C(FIR[3] if k else FIR[4]))
    return cv

# =================================================================================================
# 5. Chispa de ignición, envenenado y fuego que se apaga
# =================================================================================================
def ignite(i):
    """16×16, 4 cuadros: chispa blanca, estrella con aro de fuego, bocanada y aro que se va. Cuadro que cuenta: 1."""
    cv = Canvas(16)
    if i == 0: sparks(cv, 8, 8, 2); return cv
    r = (0, 3.5, 5.5, 6.5)[i]
    for a in range(0, 360, 20):
        x, y = 8 + math.cos(math.radians(a)) * r, 8 + math.sin(math.radians(a)) * r
        cv.put(x, y, Ca(FIR[3] if i == 1 else FIR[2] if i == 2 else FIR[1], (255, 220, 140)[i - 1]))
    if i == 1: sparks(cv, 8, 8, 3)
    if i == 2: vflame(cv, 8, 11, 6, 0, w=2.2); cv.put(8, 9, C(FIR[4]))
    if i == 3:
        for k in range(4): cv.put(8 + (k - 1.5) * 3, 6 - (k % 2), C(EMB[3]))
    return cv

def poisoned(i, n=4):
    """16×16 en bucle, encima del enemigo (que el código tiñe de lima): 2 gotas que caen y burbujas que suben."""
    cv = Canvas(16)
    for k, (x, off) in enumerate(((4, 0), (11, .5))):
        p = (i / n + off) % 1
        y = 5 + p * 9
        cv.put(x, y, C(VEN[2])); cv.put(x, y + 1, C(VEN[1])); cv.put(x - 1, y + 1, C(VEN[2]))
        cv.put(x, y + 2, C(VEN[0]))
        if p < .25: cv.put(x, y - 1, Ca(VEN[2], 160))  # se estira al soltarse
    for k, (x, off) in enumerate(((8, 0), (6, .5))):
        p = (i / n + off + .25) % 1
        y = 12 - p * 10
        if p < .85:
            for (dx, dy) in ((0, -1), (-1, 0), (1, 0), (0, 1)): cv.put(x + dx, y + dy, Ca(VEN[2], 230 - p * 120))
            cv.put(x - 1, y - 1, Ca(VEN[3], 220))
        else:
            for (dx, dy) in ((-1, -1), (1, -1), (-1, 1), (1, 1)): cv.put(x + dx, y + dy, Ca(VEN[3], 160))   # revienta
    return cv

def fire_out(i, n=6):
    """32×32, 6 cuadros: las llamas bajan y chisporrotean, sale vapor y queda humo; pie abajo como Fx_GroundFire."""
    cv = Canvas(32)
    h = (9, 5, 2, 0, 0, 0)[i]
    for k, (x, hh) in enumerate(((9, .7), (14, 1), (19, .9), (24, .6))):
        if h: vflame(cv, x, 27, h * hh + (k + i) % 2, i + k * 1.7)
    if i in (1, 2):                                       # chisporroteo
        for k in range(5): cv.put(6 + k * 5 + (i % 2), 22 - (k % 3) * 2 - i * 2, C(STEAM[3] if k % 2 else SPARK[0]))
    if i >= 1:
        s_ = (i - 1) / 4
        cloud(cv, [(12 - s_ * 2, 22 - s_ * 12, 4 + s_ * 3, 1 - s_ * .7), (20 + s_ * 2, 21 - s_ * 13, 4 + s_ * 3.5, 1 - s_ * .7),
                   (16, 24 - s_ * 8, 3 + s_ * 2, .9 - s_ * .7)], STEAM)
    for x in range(7, 27):                                # brasas que se apagan
        if (x * 3 + i) % 4 == 0: cv.put(x, 28, C((EMB[2], EMB[1], EMB[1], EMB[0], SMK[1], SMK[0])[i]))
    return cv

# =================================================================================================
# 6. Planta venenosa del escenario (por tramo; 32×32 con el pie abajo)
# =================================================================================================
def leaves(cv, t):
    lp = [C(c) for c in S.plant_ramp(t)][:4]                # un paso más oscuras que los helechos: el bulbo destaca
    for (x, y, rx, ry, a) in ((10, 26, 6, 2, -.35), (22, 26, 6, 2, .35), (12, 24.5, 5, 1.7, -.95), (20, 24.5, 5, 1.7, .95)):
        rot_ellipse(cv, x, y, rx, ry, a, lp)
        for st in range(int(rx * 1.4)):                  # nervio
            cv.put_in(x - math.cos(a) * rx * .7 + math.cos(a) * st * .9, y - math.sin(a) * rx * .7 + math.sin(a) * st * .9, lp[0])

def bulb_body(cv, sx=1.0, sy=1.0, glow=0.0, flash=None, g=1.0, cracks=0):
    """Bulbo lima con venas, manchas oscuras y poro arriba. g = cuánto ha crecido (rebrote)."""
    rx, ry = 7.5 * sx * g, 7 * sy * g
    cx, cy = 16, 23.5 - ry
    if flash: ellipse(cv, cx, cy, rx, ry, [flash] * 4); return cx, cy, rx, ry
    ellipse(cv, cx, cy, rx, ry, BULB[1:])
    if g < .5: return cx, cy, rx, ry
    for u in (-.55, 0, .55):                              # gajos: surcos de arriba abajo, como una calabaza
        for yy in range(int(cy - ry + 2), int(cy + ry - 1)):
            q = 1 - ((yy + .5 - cy) / ry) ** 2
            if q <= 0: continue
            x = cx + u * rx * math.sqrt(q)
            cv.put_in(x, yy, C(BULB[1] if u >= 0 else BULB[2]))
    for (dx, dy) in ((-.5, .2), (.3, -.35), (.25, .45), (-.2, -.55), (.62, .1)):   # manchas de aviso
        x, y = cx + dx * rx, cy + dy * ry
        for (ex, ey) in ((0, 0), (1, 0), (0, 1)): cv.put_in(x + ex, y + ey, C(BULB[0]))
    cv.put(cx - rx * .45, cy - ry * .5, C(BULB[4])); cv.put(cx - rx * .45 + 1, cy - ry * .5, C(BULB[3]))   # brillo
    if glow > 0:                                          # a punto de reventar: el veneno brilla dentro
        for (x, y), c in list(cv.px.items()):
            if ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 < .35 * glow and (x + y) % 2: cv.px[(x, y)] = C(BULB[4])
    for k in range(cracks):
        a = .5 + k * 2.1
        for st in range(int(rx * .4), int(rx * 1.05)):
            cv.put_in(cx + math.cos(a) * st, cy + math.sin(a) * st * ry / rx, C(BULB[4]))
    cv.put(cx, cy - ry - .5, C(BULB[1])); cv.put(cx - 1, cy - ry - .5, C(BULB[2])); cv.put(cx + 1, cy - ry - .5, C(BULB[0]))   # poro
    return cx, cy, rx, ry

def bulb(t, sx=1.0, sy=1.0, glow=0.0, flash=None, drip=-1.0, g=1.0, cracks=0):
    cv = Canvas(32)
    bulb_body(cv, sx, sy, glow, flash, g, cracks)
    leaves(cv, t)
    cv.outline(); cv.clean()
    if drip >= 0:                                        # gota que asoma del poro y resbala
        cx, cy, rx, ry = 16, 23.5 - 7 * sy, 7.5 * sx, 7 * sy
        x = cx + 1 + drip * 3; y = cy - ry - 1 + drip * 6
        cv.put(x, y, C(VEN[2])); cv.put(x, y + 1, C(VEN[1]))
    S.ground_shadow(cv, 16, 28.5, 10, 1.8)
    return cv

def husk(t, lift=0.0, drops=()):
    """Cáscara abierta tras reventar (también la hoja Stump): copa rota con el borde en picos."""
    cv = Canvas(32)
    rnd = random.Random(9)
    tops = [19 - 2.5 * abs(math.sin(x * 1.7)) - rnd.random() - lift for x in range(10, 23)]
    def inside(x, y):
        xi = int(x)
        return 10 <= xi < 23 and tops[xi - 10] <= y <= 23.5 - (abs(x - 16.5) > 5) * 1.5
    fill(cv, inside, 16, 20, 6.5, 4, BULB[:4])
    for x in range(11, 22):                              # interior oscuro
        y = int(tops[x - 10]) + 1
        if (x, y) in cv.px: cv.put(x, y, C(BULB[0])); cv.put(x, y + 1, C(OUTL))
    leaves(cv, t)
    cv.outline(); cv.clean()
    for (x, y, r) in drops: P.drop(cv, x, y, r, VEN)
    S.ground_shadow(cv, 16, 28.5, 10, 1.8)
    return cv

def bulb_sheets(t):
    idle = [bulb(t, 1 + .04 * math.sin(f * math.pi / 2), 1 - .04 * math.sin(f * math.pi / 2), drip=(-1, -1, 0, .6)[f]) for f in range(4)]
    hit = [bulb(t, flash='#ffffff'), bulb(t, 1.15, .85), bulb(t, .92, 1.08)]
    burst = [bulb(t, 1.12, 1.1, glow=.6), bulb(t, 1.2, 1.18, glow=1.0, cracks=3)]
    for k in range(3):                                   # revienta: el cuadro que cuenta es el 2 (sale el charco)
        drops = []
        for j in range(8):
            a = -math.pi / 2 + (j - 3.5) * .42
            d = (6, 10, 12)[k] + (j % 2) * 2
            drops.append((16 + math.cos(a) * d * 1.0, 15 + math.sin(a) * d * .7 + (0, 3, 8)[k], (1.8, 1.4, 1.0)[k]))
        cv = husk(t, lift=(1.5, .5, 0)[k], drops=drops)
        if k == 0:
            cloud(cv, [(16, 13, 5, .9), (12, 15, 3.5, .8), (20, 15, 3.5, .8)], VEN)
            sparks(cv, 16, 12, 1)
        burst.append(cv)
    regrow = [husk(t)]
    for g in (.45, .6, .8, 1.0):                         # rebrota desde la cáscara
        regrow.append(bulb(t, g=g) if g >= .5 else bulb_bud(t, g))
    return {'PoisonBulb_Idle': idle, 'PoisonBulb_Hit': hit, 'PoisonBulb_Burst': burst,
            'PoisonBulb_Stump': [husk(t)], 'PoisonBulb_Regrow': regrow}

def bulb_bud(t, g):
    cv = Canvas(32)
    ellipse(cv, 16, 23.5 - 7 * g, 7.5 * g, 7 * g, BULB[1:])
    cv.put(15, 23.5 - 14 * g + 1, C(BULB[3]))
    leaves(cv, t)
    cv.outline(); cv.clean(); S.ground_shadow(cv, 16, 28.5, 10, 1.8)
    return cv

# =================================================================================================
# Salida
# =================================================================================================
MS = {'Fx_ToxicCloud_Form': 100, 'Fx_ToxicCloud_Idle': 120, 'Fx_ToxicCloud_Fade': 110, 'Fx_PoisonPuddle_Ignite': 80,
      'Methane_Sac_Burst': 60, 'Fx_MistIgnite': 70, 'Fx_MistFlare': 70, 'Fx_Ignite': 50, 'Fx_Poisoned': 120,
      'Fx_FireOut': 90, 'PoisonBulb_Idle': 260, 'PoisonBulb_Hit': 60, 'PoisonBulb_Burst': 70, 'PoisonBulb_Regrow': 140}

def ms_of(name):
    for k, v in MS.items():
        if name.startswith(k): return v
    return 200

def save(path, frames):
    os.makedirs(os.path.dirname(path), exist_ok=True); save_sheet(path, frames)

def build():
    fx = {}
    fx.update(toxic_sheets())
    fx['Fx_PoisonPuddle_Ignite'] = [puddle_ignite(i) for i in range(6)]
    fx['Fx_MistIgnite'] = [mist_ignite(i) for i in range(7)]
    fx['Fx_MistFlare'] = [flare(i) for i in range(6)]
    fx['Fx_Ignite'] = [ignite(i) for i in range(4)]
    fx['Fx_Poisoned'] = [poisoned(i) for i in range(4)]
    fx['Fx_FireOut'] = [fire_out(i) for i in range(6)]
    for name, frames in fx.items(): save(os.path.join(OUT, 'Effects', name + '.png'), frames)
    themed = {}
    for key in KEYS:
        t = tram(key)
        th = {'Methane_Sac_Burst': [sac_burst(t, i) for i in range(5)], 'Methane_Sac_Remains': [sac_remains(t)]}
        th.update(bulb_sheets(t))
        for name, frames in th.items(): save(os.path.join(OUT, key, name + '.png'), frames)
        themed[key] = th
    preview(os.path.join(OUT, 'vista_previa_efectos.png'), fx, BG, scale=3)
    rows = {}
    for key in KEYS:
        for name, frames in themed[key].items():
            if key == 'Swamp' or name in ('Methane_Sac_Remains', 'PoisonBulb_Idle', 'PoisonBulb_Burst'):
                rows[f'{key} {name}'] = frames
    preview(os.path.join(OUT, 'vista_previa_escenario.png'), rows, BG, scale=3)
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    for name, frames in fx.items(): gif(os.path.join(OUT, 'gif', name + '.gif'), frames, ms_of(name), BG, scale=4)
    for name, frames in themed['Swamp'].items():
        if len(frames) > 1: gif(os.path.join(OUT, 'gif', name + '.gif'), frames, ms_of(name), BG, scale=4)
    return fx, themed

if __name__ == '__main__':
    build()
    print('Listo:', OUT)
