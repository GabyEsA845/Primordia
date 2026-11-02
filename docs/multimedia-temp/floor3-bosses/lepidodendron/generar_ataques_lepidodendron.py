"""Lepidodendron ancestral · subtanda 6n-2: los ataques de la fase final (además de los conos).

- Latigazo de raíces (barrido en franja): aviso = grietas con luz morada que corren por la franja desde el borde de
  la arena + polvo; una raíz gruesa trenzada (paleta de las paredes) sale del suelo junto al borde, barre a ras de
  suelo hasta el centro abriendo un surco de tierra y se recoge hacia atrás. Se dibuja saliendo por la izquierda;
  la derecha es Flip X.
- Corro de puntas (raíces bajo el jugador): el suelo se agrieta en círculo bajo el jugador con luz morada; brota un
  haz de 5 raíces puntiagudas, se quedan un instante y se hunden dejando tierra revuelta.
- Lluvia de esporas lila: espora de 12 px (núcleo oscuro, halo lila suave) que cae sobre su sombrita (se encoge
  mientras cae); al tocar el suelo revienta en una salpicadura pequeña de polvo lila y desaparece.

Las raíces de los ataques: paleta oliva de las paredes y grietas finas de luz morada que se encienden al atacar.
Todo lo que toca el suelo se funde con él (labios de tierra levantada, bordes en trama, sin contorno).

Uso: python generar_ataques_lepidodendron.py [muestra]
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import generar_lepidodendron as G
from generar_lepidodendron import *                    # noqa: F401,F403
from generar_lepidodendron import _h
import generar_paredes_lepidodendron as P
import generar_conos_lepidodendron as K
from PIL import Image, ImageDraw

ATT = os.path.join(HERE, 'Attacks')
ROOTA = P.ROOTW                                        # 7 tonos, oliva grisáceo (las paredes)
CORE, SPORE_L, DUST = K.CORE, K.SPORE_L, K.DUST
FLOORC = P.FLOOR                                       # el suelo de la sala (4 tonos)
SOILA = [C(c) for c in ('#1a1b14', '#24251b', '#2e2f21', '#3b3b29', '#4b4932', '#5e5b40')]   # tierra removida
CRACKC = C('#120c18')

def _dith(v, x, y):
    """True si el píxel se queda con densidad v (0..1), trama Bayer."""
    return (BAYER[y % 4][x % 4] + .5) / 16 < v

def merge(dst, src):
    for p, c in src.px.items(): dst.px[p] = c

# =================================================================================================
# Latigazo
# =================================================================================================
WW, WH, WCY = 352, 56, 34                              # celda; la franja va por y = WCY; pivote (0, WCY) = borde
X0W = 10                                               # donde sale la raíz (junto al borde de la arena)
WEND = 334                                             # hasta dónde llega la punta (≈ 20 u: el centro de la arena)
RMAX = 10.5                                            # radio de la raíz

def path(x):
    """La línea de la franja (fija en el mundo: la raíz se desliza por ella como por un raíl)."""
    return 2.2 * math.sin(x / 41 + 1.3) + 1.2 * math.sin(x / 15 + .4)

def root_radius(x, tip):
    u = tip - x                                         # distancia a la punta
    m = x - tip
    bump = 1 + .1 * math.sin(m / 9.5) * math.sin(m / 23 + 1) + (.12 if (_h(int(m // 26), 4, 4) < .3 and (m % 26) > 18) else 0)
    return min(RMAX, 1.2 + u * .15) * bump * (1 - .12 * max(0, 1 - (x - X0W) / 30))

def root_px(x, y, ymid, a, m, glow, pal=ROOTA):
    """Color de un píxel de la raíz trenzada. v = -1 arriba .. 1 abajo; m = coordenada del material (se desliza)."""
    v = max(-.999, min(.999, (y - ymid) / a))
    th = math.asin(v)
    tw = 15 + 5 * math.sin(m / 37)                      # el trenzado se aprieta y se afloja
    cc = (th / math.pi + .5) * 3 + m / tw + .3 * math.sin(m / 19 + th)   # 3 cordones que se enroscan, irregulares
    fr = cc % 1
    tone = 3.5 - (v + 1) * 1.45 + .75 * math.sin(fr * math.pi) - .3
    if fr < .11 or fr > .94:                            # junta entre cordones
        if glow > 0 and abs(v) < .7 and _h(int(m // 7), int(cc), 3) < .16:
            return CORE[min(3, int(glow * 2.2 + (1 if _h(int(m), 1, 1) < .3 else 0)))]
        tone -= 1.6
    elif v < -.35 and _h(int(m) // 2, int(y) // 2, 11) < .16: return MOSS[1 + (_h(int(m), int(y), 2) < .3)]   # musgo arriba
    elif v < -.2 and _h(int(m) // 3, int(cc * 3), 12) < .06: return SOILA[4]                                   # tierra pegada
    return pal[max(0, min(6, int(round(tone))))]

def clod(cv, x, y, r, pal=None, shadow=True):
    """Terrón sombreado (luz arriba a la izquierda) con su sombrita debajo."""
    pal = pal or SOILA
    ry = r * .62
    for yy in range(int(y - ry - 1), int(y + ry + 2)):
        for xx in range(int(x - r - 1), int(x + r + 2)):
            d = ((xx - x) / r) ** 2 + ((yy - y) / ry) ** 2
            if d > 1: continue
            lit = (xx - x) * -.5 + (yy - y) * -1.0
            cv.put(xx, yy, pal[5] if lit > ry * .55 else pal[4] if lit > ry * .05 else pal[3] if lit > -ry * .45 else pal[2])
    if shadow:
        for xx in range(int(x - r * .7), int(x + r * .7) + 1):
            if (xx, int(y + ry + 1)) not in cv.px: cv.put(xx, y + ry + 1, (10, 12, 10, 150))

def berm_y(x, side):
    """Línea de cada caballón de tierra que levanta la raíz (side -1 norte, 1 sur)."""
    return WCY + path(x) + (-RMAX * 1.0 if side < 0 else RMAX * .05) + (_h(x // 4, side, 5) - .5) * 2.4

def trench(cv, x0, x1):
    """El surco: tierra revuelta (zanja irregular de ancho variable, terrones, piedrecitas, trozos de raíz, grietas a
    los lados); los bordes se funden con el suelo en trama."""
    for x in range(x0, int(x1) + 1):
        y0 = WCY + path(x)
        w = (5.2 + 1.6 * math.sin(x / 17 + 1) + 1.1 * math.sin(x / 6.3)) * min(1, (x1 - x) / 10 + .3)
        for y in range(int(y0 - w - 3), int(y0 + w + 4)):
            dy = (y - y0) / max(w, 1)
            if abs(dy) < .7:
                c = (14, 13, 10, 255) if dy < -.15 else SOILA[0] if dy < .35 else SOILA[1]   # fondo; pared de abajo con algo de luz
            elif abs(dy) < 1.0: c = SOILA[2] if dy < 0 else SOILA[3]
            else:
                e = (abs(dy) - 1) / .6
                if e > 1 or not _dith(1 - e, x, y): continue
                c = SOILA[3] if dy < 0 else SOILA[2]
            cv.put(x, y, c)
    rnd = random.Random(77)
    for k in range(int((x1 - x0) / 9)):                 # terrones caídos dentro, piedrecitas, trozos de raíz
        x = x0 + 6 + k * 9 + rnd.uniform(-3, 3)
        if x > x1 - 6: break
        y = WCY + path(x) + rnd.uniform(-2.5, 2.5)
        kind = rnd.random()
        if kind < .45: clod(cv, x, y, rnd.uniform(1.6, 2.8))
        elif kind < .7:
            cv.put(x, y, (96, 94, 84, 255)); cv.put(x + 1, y, (70, 68, 60, 255)); cv.put(x, y + 1, (44, 42, 36, 255))
        elif kind < .85:
            for q in range(4): cv.put(x + q, y - (q % 2), ROOTA[3] if q < 2 else ROOTA[2])
        else:
            cv.put(x, y, CORE[0]); cv.put(x + 1, y, SAP_A)

SAP_A = C('#4a2a6a')

def berms(cv, x0, x1, side, prog=None):
    """Caballón de terrones a un lado de la raíz (lo que ha levantado al pasar)."""
    for x in range(x0, int(x1) + 1, 4):
        if prog is not None and x > prog: break
        hv = _h(x // 4, side, 9)
        if hv < .14: continue                           # algún hueco
        r = 1.8 + 3.4 * hv ** 1.6
        y = berm_y(x, side) + (1.5 if side > 0 else 0)
        clod(cv, x + (_h(x, 3, 9) - .5) * 2, y, r)
    for x in range(x0, int(x1) + 1):                    # tierra suelta entre los terrones, en trama hacia fuera
        if prog is not None and x > prog: break
        yb = berm_y(x, side)
        for q in range(4):
            y = yb + side * (3 + q)
            if (int(x), int(y)) not in cv.px and _dith(.7 - q * .2, x, int(y)): cv.put(x, y, SOILA[3] if q < 2 else SOILA[2])

def soil_wave(cv, cx, cy, size=1.0, k=0):
    """La ola de tierra delante de la punta: el suelo se abomba, se agrieta y revienta (tapa la punta de la raíz)."""
    rx, ry = 11 * size, 6.5 * size
    for y in range(int(cy - ry - 2), int(cy + ry + 2)):
        for x in range(int(cx - rx - 2), int(cx + rx + 2)):
            d = math.hypot((x - cx) / rx, (y - cy) / ry)
            if d > 1: continue
            if d > .78 and not _dith((1 - d) / .22, x, y): continue
            nx, ny = (x - cx) / rx, (y - cy) / ry
            lam = -(nx * .5 + ny * .9) + .2
            c = SOILA[5] if lam > .6 else SOILA[4] if lam > .15 else SOILA[3] if lam > -.35 else SOILA[2]
            cv.put(x, y, c)
    rnd = random.Random(k * 7 + int(cx) % 5)
    for q in range(5):                                  # grietas sueltas que se abren hacia fuera
        a = rnd.uniform(0, 2 * math.pi)
        x, y = cx + math.cos(a) * rx * .2 + rnd.uniform(-1.5, 1.5), cy + math.sin(a) * ry * .2
        for st in range(int(2 + rnd.uniform(2, 4) * size)):
            a += rnd.uniform(-.5, .5)
            x += math.cos(a) * .9; y += math.sin(a) * .5
            if math.hypot((x - cx) / rx, (y - cy) / ry) < .8: cv.put(x, y, CRACKC)
    for q in range(3):                                  # terrones que se levantan encima
        clod(cv, cx + rnd.uniform(-rx * .5, rx * .4), cy - ry * .3 + rnd.uniform(-2, 2), rnd.uniform(1.8, 3.0) * size)

def whip(tip, glow=0.0, furrow_to=None, mound=1.0, spray=-1, wave=False, k=0):
    """Un cuadro del latigazo. tip = x de la punta; furrow_to = hasta dónde ha llegado (surco y caballones);
    mound 0..1 = montículo de salida; spray = fase de la tierra que salta delante; wave = ola de tierra delante."""
    cv = Canvas(WW, WH)
    furrow_to = tip if furrow_to is None else furrow_to
    reach = int(max(furrow_to, tip))
    # 1. el surco (suelo) y el caballón de atrás
    if reach > X0W + 2:
        trench(cv, X0W, reach)
        berms(cv, X0W, reach, -1)
    # 2. la raíz, más enterrada: solo asoma el lomo entre los dos caballones (contorno solo alrededor de ella)
    lo = max(X0W, tip - 400)
    if tip > X0W + 1:
        rootc = Canvas(WW, WH)
        for x in range(lo, int(tip) + 1):
            r = root_radius(x, tip)
            if r < .6: continue
            ymid = WCY + path(x) - r * .2
            a = r * .85
            for y in range(int(ymid - a), int(ymid + a) + 1):
                rootc.put(x, y, root_px(x, y, ymid, a, x - tip, glow))
        for j in range(int(400 / 30)):                  # espinas de raíz arriba y raicillas abajo, pegadas al material
            m = -(22 + j * 30 + (_h(j, 1, 6) - .5) * 12)
            x = tip + m
            if x < lo + 4: break
            r = root_radius(x, tip); ymid = WCY + path(x) - r * .2
            up = _h(j, 5, 5) < .6
            if not up: continue
            y = ymid - r * .8
            dxr = -1 if _h(j, 2, 6) < .65 else 1        # casi todas apuntan hacia atrás
            L = 6 + int(_h(j, 3, 6) * 5)
            for st in range(L):
                y -= .75; x += dxr * .7
                if (int(x), int(y)) in rootc.px and st > 1: continue
                rootc.put(x, y, ROOTA[3] if st < L - 2 else ROOTA[4])
                if st < L * .45: rootc.put(x + 1, y, ROOTA[1])
        rootc.outline(); rootc.clean()
        merge(cv, rootc)
    # 3. el caballón de delante: tapa la parte de abajo de la raíz
    if reach > X0W + 2: berms(cv, X0W, reach, 1)
    # 4. la ola de tierra que revienta delante de la punta
    if wave: soil_wave(cv, tip + 5, WCY + path(tip) - 1, 1.0, k)
    # 5. montículo de salida junto al borde (la raíz sale de él)
    if mound > 0:
        mx, my = X0W + 4, WCY + path(X0W) - 1
        soil_wave(cv, mx, my, 1.35 * mound, 99)
        rnd = random.Random(3)
        for q in range(int(8 * mound)):                 # terrones sueltos alrededor
            a = rnd.uniform(0, 2 * math.pi); rr = rnd.uniform(1.1, 1.6)
            clod(cv, mx + math.cos(a) * 15 * mound * rr, my + math.sin(a) * 8 * mound * rr, rnd.uniform(1.4, 2.4))
    # 6. tierra que salta delante de la punta
    if spray >= 0 and tip > X0W + 4:
        rnd = random.Random(7)
        for j in range(14):
            t = ((spray * .37 + j / 14) % 1)
            vx, h = rnd.uniform(3, 15), rnd.uniform(8, 18)
            x = tip + 6 + vx * t; y = WCY + path(tip) - 2 - h * 4 * t * (1 - t) + rnd.uniform(-3, 3)
            if j % 4 == 0: clod(cv, x, y, 1.6, shadow=False)
            else:
                cv.put(x, y, SOILA[5] if j % 2 else SOILA[3]); cv.put(x + 1, y, SOILA[2])
    return cv

def whip_warn(f, k, n_glow=4):
    """Aviso del latigazo: grietas con luz morada que corren por la franja desde el borde (f 0..1 = hasta dónde han
    llegado) y bocanadas de polvo cerca del frente. k = cuadro (latido de la luz)."""
    cv = Canvas(WW, WH)
    front = X0W + f * (WEND - X0W)
    pulse = .5 + .5 * math.sin(k * 2 * math.pi / n_glow)
    rnd = random.Random(21)
    # grieta principal a lo largo de la franja, quebrada, y ramitas
    y = WCY + path(X0W)
    segs = []
    x = X0W
    while x < WEND:
        x2 = x + rnd.randint(4, 9); y2 = WCY + path(x2) + rnd.uniform(-2.5, 2.5)
        segs.append((x, y, x2, y2)); x, y = x2, y2
    for j in range(16):
        bx = X0W + j * (WEND - X0W) / 16 + rnd.uniform(0, 12)
        by = WCY + path(bx)
        ang = rnd.choice((-1, 1)) * rnd.uniform(.5, 1.2)
        L = rnd.randint(5, 11)
        segs.append((bx, by, bx + math.cos(ang) * L * .8, by + math.sin(ang) * L * .45))
    for (x1, y1, x2, y2) in segs:
        if x1 > front: continue
        n = int(max(abs(x2 - x1), abs(y2 - y1))) + 1
        for i in range(n + 1):
            xx = x1 + (x2 - x1) * i / n; yy = y1 + (y2 - y1) * i / n
            if xx > front: break
            age = (front - xx) / 60                     # lo de atrás brilla más
            cv.put(xx, yy, CRACKC)
            g = min(1, age) * (.55 + .45 * pulse)
            if g > .25 and _h(int(xx) // 3, 1, 2) < .7: cv.put(xx, yy, CORE[min(2, int(g * 2.6))])
            cv.put(xx, yy + 1, SOILA[2])               # borde de la grieta con algo de sombra
    for j in range(5):                                  # polvo que se levanta cerca del frente
        px_ = front - j * 14 - (k * 3) % 14
        if px_ < X0W: continue
        py_ = WCY + path(px_) - 2 - j
        al = int(150 * (1 - j / 5))
        for (dx, dy) in ((0, 0), (1, 0), (-1, 0), (0, -1), (1, -1), (2, 0), (-2, 1)):
            cv.put(px_ + dx, py_ + dy, DUST[3][:3] + (al,))
    return cv

# =================================================================================================
# Corro de puntas
# =================================================================================================
SW, SH, SPX, SPY = 80, 96, 40, 78                      # celda; pivote = centro del corro en el suelo
SPIKES = [(-13, -4, 30, 6.0, -.4), (12, -5, 33, 6.0, .34), (0, -1, 42, 7.5, .03),
          (-9, 5, 26, 5.5, -.3), (10, 5, 28, 5.5, .38)]   # (dx, dy, alto, radio de la base, inclinación)

def crack_ring(cv, f, glow, k=0):
    """Grietas en círculo bajo el jugador (f 0..1 crecen) con luz morada (glow)."""
    rnd = random.Random(31)
    for j in range(9):
        ang = j / 9 * 2 * math.pi + rnd.uniform(-.2, .2)
        L = rnd.uniform(18, 26) * f
        x, y = SPX + math.cos(ang) * 2, SPY + math.sin(ang) * 1
        a = ang
        for s in range(int(L)):
            a += rnd.uniform(-.35, .35)
            x += math.cos(a); y += math.sin(a) * .45
            cv.put(x, y, CRACKC)
            if glow > .1 and s < L * .8 and _h(int(x), int(y), 4) < .75:
                cv.put(x, y, CORE[min(2, int(glow * 2.6))])
            cv.put(x, y + 1, SOILA[2])
    if glow > .1:                                       # brillo tenue en el suelo (translúcido, sin contorno)
        for y in range(SPY - 12, SPY + 13):
            for x in range(SPX - 26, SPX + 27):
                d = math.hypot((x - SPX) / 24, (y - SPY) / 11)
                if d < 1 and (x, y) not in cv.px and _dith((1 - d) * .55 * f, x, y):
                    cv.put(x, y, CORE[1][:3] + (int(70 * glow),))
    if f > .5:                                          # el suelo se abomba un poco en el centro
        for y in range(SPY - 5, SPY + 6):
            for x in range(SPX - 12, SPX + 13):
                d = math.hypot((x - SPX) / 12, (y - SPY) / 5.5)
                if d < 1 and (x, y) not in cv.px and _dith((1 - d) * (f - .5) * 1.4, x, y):
                    cv.put(x, y, SOILA[4] if y < SPY else SOILA[3])

def spike(cv, bx, by, H, w0, lean, glow, m0=0):
    """Una púa de raíz trenzada, recta e inclinada, con luz a la izquierda."""
    if H < 2: return
    dx, dy = math.sin(lean), -math.cos(lean)
    tipx, tipy = bx + dx * H, by + dy * H
    sc = Canvas(cv.w, cv.h)
    for y in range(int(min(by, tipy) - w0 - 2), int(max(by, tipy) + w0 + 2)):
        for x in range(int(min(bx, tipx) - w0 - 2), int(max(bx, tipx) + w0 + 3)):
            px_, py_ = x - bx, y - by
            t0 = (px_ * dx + py_ * dy) / H
            if abs(lean) > .1: px_ -= math.copysign(3.5, lean) * max(0, t0) ** 2   # se curva hacia fuera
            t = (px_ * dx + py_ * dy) / H
            if t < 0 or t > 1: continue
            d = px_ * (-dy) + py_ * dx                  # distancia al eje (positivo a la derecha)
            r = w0 * (1 - t) ** .85
            if abs(d) > r: continue
            side = d / max(r, .5)
            th = math.asin(max(-.999, min(.999, side)))
            cc = (th / math.pi + .5) * 2.2 + t * H / (11 + 4 * math.sin(t * 5 + m0 * 6)) + m0
            fr = cc % 1
            tone = 3.4 - side * 1.5 + .6 * math.sin(fr * math.pi) - .3 - (1 - t) * .5
            if (fr < .1 or fr > .94) and t < .85:
                if glow > 0 and _h(int(t * H // 5), int(cc), 7) < .3 and abs(side) < .7 and t < .7:
                    sc.put(x, y, CORE[min(3, int(glow * 2.3))]); continue
                tone -= 1.4
            elif t < .3 and side < -.2 and _h(x // 2, y // 2, 13) < .2: sc.put(x, y, MOSS[1]); continue
            if t > .88: tone += .5                      # la punta, algo más clara
            sc.put(x, y, ROOTA[max(0, min(6, int(round(tone))))])
    sc.outline(); sc.clean()
    merge(cv, sc)

def spikes(grow, glow, k=0, burst=False, earth=0.0):
    """El corro: grietas debajo, 5 púas (grow 0..1) con un collar de tierra en la base; burst = estallido de tierra."""
    cv = Canvas(SW, SH)
    crack_ring(cv, 1.0, glow * .6, k)
    if earth > 0:                                       # tierra revuelta al hundirse
        rnd = random.Random(40)
        for j in range(int(26 * earth)):
            x = SPX + rnd.uniform(-17, 17); y = SPY + rnd.uniform(-7, 8)
            if ((x - SPX) / 18) ** 2 + ((y - SPY) / 8) ** 2 > 1: continue
            cv.put(x, y, SOILA[5]); cv.put(x + 1, y, SOILA[4]); cv.put(x, y + 1, SOILA[2]); cv.put(x + 1, y + 1, SOILA[1])
        for (dx, dy, H, w0, lean) in SPIKES:            # hoyos donde estaban las púas
            for x in range(int(-w0 * .7), int(w0 * .7) + 1):
                cv.put(SPX + dx + x, SPY + dy, SOILA[0]); cv.put(SPX + dx + x, SPY + dy - 1, SOILA[1])
    grows = grow if isinstance(grow, (tuple, list)) else [grow] * len(SPIKES)
    for i, (dx, dy, H, w0, lean) in sorted(enumerate(SPIKES), key=lambda s: s[1][1]):
        g = grows[i]
        h = H * g * (1 + (_h(dx, dy, 2) - .5) * .1)
        if h < 2: continue
        bx, by = SPX + dx, SPY + dy
        spike(cv, bx, by, h, w0 * min(1, .55 + g * .6), lean, glow, m0=_h(dx, 9, 9))
        for x in range(int(bx - w0 - 2), int(bx + w0 + 3)):  # collar de tierra: tapa la base
            e = abs(x - bx) / (w0 + 2.5)
            if e > 1: continue
            top = by - 2 + e * 2.5 - (_h(x, by, 3) < .4)
            for y in range(int(top), int(by + 3)):
                if _dith(1 - e * .6, x, y):
                    cv.put(x, y, SOILA[5] if y <= top + .5 else SOILA[4] if y < by + 1 else SOILA[3])
    if burst:                                           # estallido de tierra al brotar
        rnd = random.Random(9)
        for j in range(22):
            a = rnd.uniform(-math.pi, 0); v = rnd.uniform(8, 24)
            x = SPX + math.cos(a) * v; y = SPY - 4 + math.sin(a) * v * .8
            c = SOILA[5] if j % 3 == 0 else SOILA[3] if j % 3 == 1 else DUST[3]
            cv.put(x, y, c); cv.put(x + 1, y, c)
    return cv

# =================================================================================================
# Esporas
# =================================================================================================
def spore(k, n=4):
    """Espora lila (12×12): núcleo oscuro, borde lila y halo suave que late; motitas que giran."""
    cv = Canvas(12)
    ph = k / n * 2 * math.pi
    halo = 5.6 + .5 * math.sin(ph)
    for y in range(12):
        for x in range(12):
            d = math.hypot(x - 5.5, y - 5.5)
            if d <= 3.2:
                nx, ny = (x - 5.5) / 3.2, (y - 5.5) / 3.2
                lam = -(nx + ny) * .7
                c = SCALE_D[2] if lam > .5 else SCALE_D[1] if lam > -.2 else SCALE_D[0]
                if d > 2.4: c = SPORE_L[1] if lam > -.3 else SPORE_L[0]
                cv.put(x, y, c)
            elif d <= halo:
                cv.put(x, y, SPORE_L[2][:3] + (int(150 * (1 - (d - 3.2) / (halo - 3.2)) + 60),))
    for j in range(3):                                  # motitas
        a = ph + j * 2.1
        cv.put(5.5 + math.cos(a) * 4.6, 5.5 + math.sin(a) * 4.6, SPORE_L[3])
    cv.put(4, 4, SPORE_L[2])                            # brillo
    return cv

SCALE_D = [C(c) for c in ('#1a1026', '#2e1c48', '#4a2e70')]

WARN_RED = (255, 80, 60)                               # rojo de aviso del juego (piedra del Euriptérido, Anomalocaris)
MW, MH = 34, 22                                        # celda de la marca; centro (17, 11)

def spore_mark(k, n=6):
    """Marca donde caerá la espora, en el lenguaje de la piedra del piso 2: sombra oscura que crece, halo rojizo y
    borde rojo discontinuo que gira y se cierra hacia la sombra; motitas lila que ya caen; en el último cuadro, justo
    antes del impacto, el borde destella en blanco. En picada: elipse (alto = 0,55 × ancho)."""
    cv = Canvas(MW, MH)
    cx, cy, q = 17, 11, .55
    t = k / (n - 1)
    rr = 15.5 - 4.5 * t                                 # el borde se cierra
    rs = 3.5 + 4.5 * t                                  # la sombra crece
    for y in range(MH):
        for x in range(MW):
            d = math.hypot(x + .5 - cx, (y + .5 - cy) / q)
            if d < rs: cv.put(x, y, (8, 4, 12, int(110 + 100 * t)))
            elif d < rr - 1: cv.put(x, y, (110, 24, 22, int(45 + 60 * t)))
    ring = (255, 255, 255) if k == n - 1 else WARN_RED
    for y in range(MH):
        for x in range(MW):
            d = math.hypot(x + .5 - cx, (y + .5 - cy) / q)
            a = math.atan2((y + .5 - cy) / q, x + .5 - cx)
            if rr - 1.6 <= d <= rr and int((a + math.pi + k * .35) / (math.pi / 7)) % 2 == 0:
                cv.put(x, y, ring + (235,))
    rnd = random.Random(k + 11)
    for _ in range(1 + k // 2):                         # motitas lila que caen antes que la espora
        cv.put(cx + rnd.randint(-int(rr * .6), int(rr * .6)), cy + rnd.randint(-2, 2), SPORE_L[2])
    return cv

def spore_splash(k, n=5):
    """Salpicadura de polvo lila al tocar el suelo (24×16, pivote (12, 11))."""
    im = Image.new('RGBA', (24, 16), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    t = k / (n - 1)
    al = int(230 * (1 - t) ** .7)
    if k == 0:
        d.ellipse([7, 7, 17, 13], fill=SPORE_L[2][:3] + (240,)); d.ellipse([10, 8, 14, 11], fill=SPORE_L[3][:3] + (255,))
    rnd = random.Random(5)
    for j in range(10):
        a = rnd.uniform(math.pi, 2 * math.pi) if j % 3 else rnd.uniform(0, math.pi)
        v = rnd.uniform(5, 10)
        x = 12 + math.cos(a) * v * (t + .2); y = 11 + math.sin(a) * v * .5 * (t + .2) - 3 * t
        d.point((x, y), fill=SPORE_L[1 + (j % 2)][:3] + (al,))
    rr = 4 + 6 * t                                      # anillo de polvo bajo
    for j in range(12):
        a = j / 12 * 2 * math.pi
        d.point((12 + math.cos(a) * rr, 11 + math.sin(a) * rr * .4), fill=SPORE_L[0][:3] + (al,))
    return im

# =================================================================================================
# Arena (para las láminas y escenas)
# =================================================================================================
def arena_layers():
    W, H = 50, 30
    fx, fy = 25, 15.5
    room = A.room(A.rect_room(W, H, (2, int(fy - 10.5), W - 3, H - 2)))
    X0, Y0 = int((fx - 20) * U), int((fy - 10.25) * U)
    sf = K._sheetf
    FX_, FY_ = int(fx * U), int(fy * U)
    for name, w, px, py in (('Lepido_BackRoots', 520, 260, 196), ('Lepido_MoundShadow', 480, 240, 96), ('Lepido_Mound', 480, 240, 96)):
        room.alpha_composite(sf('Tree', name, w)[0], (FX_ - px, FY_ - py))
    L = dict(room=room, X0=X0, Y0=Y0, FX=FX_ - X0, FY=FY_ - Y0,
             trunk=sf('Tree', 'Lepido_Trunk', 288)[0], vrest=sf('Tree', 'Lepido_Veins_Rest', 288)[0],
             vwarn=sf('Tree', 'Lepido_Veins_Warn', 288), dapple=sf('Canopy', 'Fx_Dapple', 256),
             shafts=sf('Canopy', 'Fx_LightShaft', 118), pools=sf('Canopy', 'Fx_LightPool', 52),
             sway=sf('Canopy', 'Canopy_Arena_Sway', 704), shake=sf('Canopy', 'Canopy_Arena_Shake', 704),
             frog=A.frame(FROG, 0, 32))
    return L

def arena_frame(L, t, ground=(), objs=(), over=(), canopy=('sway', 0), veins=None):
    """ground/objs/over: [(img, x, y)] en coordenadas del recorte (esquina de arriba a la izquierda).
    objs = [(base_y, img, x, y)] se ordenan por altura entre sí (el tronco va siempre detrás)."""
    X0, Y0 = L['X0'], L['Y0']
    im = L['room'].copy()
    dl = Image.new('RGBA', im.size, (0, 0, 0, 0)); tl = L['dapple'][(t // 2) % 8]
    for ty in range(0, im.height, 256):
        for tx in range(0, im.width, 256): dl.alpha_composite(tl, (tx, ty))
    im.alpha_composite(dl)
    def at(spr, x, y): im.alpha_composite(spr, (int(X0 + x), int(Y0 + y)))
    at(L['trunk'], L['FX'] - 144, L['FY'] - 352)
    at(veins if veins is not None else L['vrest'], L['FX'] - 144, L['FY'] - 352)
    for (spr, x, y) in ground: at(spr, x, y)
    for (_, spr, x, y) in sorted(objs, key=lambda o: o[0]): at(spr, x, y)
    for (spr, x, y) in over: at(spr, x, y)
    for j, (hx, hr) in enumerate(((9 * U + 32, 26), (30 * U + 32, 22))):
        gx = hx - 32; gy = -10 + int(112 * .58 * .62)
        at(L['shafts'][(t + j * 3) % 8], int(gx - (1.4 * U / 2 + 8)), gy)
        pl = L['pools'][(t + j * 2) % 8]
        at(pl, int(gx + 4.2 * U - pl.width / 2), int(gy + 15 * U - pl.height / 2))
    cm, ck = canopy
    at(L['sway'][ck % 8] if cm == 'sway' else L['shake'][ck % 6], -32, -10)
    return im.crop((X0, Y0, X0 + 640, Y0 + 360))

def _i(cv): return cv.img().convert('RGBA') if hasattr(cv, 'img') else cv

# =================================================================================================
# Muestra
# =================================================================================================
def lamina_muestra():
    L = arena_layers()
    frog = L['frog']
    BY = 286                                            # franja del latigazo (izquierda), en el recorte
    WY2 = 214                                           # franja del aviso (derecha)
    ground, objs, over = [], [], []
    # latigazo en plena extensión desde la izquierda
    wi = _i(whip(WEND - 40, glow=.9, spray=2, wave=True))
    objs.append((BY + 6, wi, 0, BY - WCY))
    # aviso del latigazo desde la derecha (Flip X), a medio camino
    ww = _i(whip_warn(.6, 1)).transpose(Image.FLIP_LEFT_RIGHT)
    ground.append((ww, 640 - WW, WY2 - WCY))
    # corro de puntas brotando bajo un punto; otro corro avisando bajo la rana
    sp = _i(spikes(1.0, .9))
    objs.append((150 + 0, sp, 470 - SPX, 150 - SPY))
    cw = Canvas(SW, SH); crack_ring(cw, .8, .8)
    ground.append((_i(cw), 420 - SPX, 320 - SPY))
    objs.append((326, frog, 420 - 16, 326 - 30))
    # esporas que caen con su sombra, y una salpicadura
    for j, (x, y, h, k) in enumerate(((250, 160, 60, 1), (300, 200, 26, 3), (360, 120, 90, 0), (210, 220, 10, 5), (540, 300, 44, 2))):
        ground.append((_i(spore_mark(k)), x - 17, y - 11))
        over.append((_i(spore(j % 4)), x - 6, y - 6 - h))
    ground.append((spore_splash(1), 150 - 12, 180 - 11))
    scene = arena_frame(L, 0, ground, objs, over)
    # fila de detalles ×3
    s = 3
    det = [('latigazo: salida', _i(whip(WEND - 40, glow=.9, spray=2, wave=True)).crop((0, 0, 150, WH))),
           ('latigazo: punta con la ola de tierra', _i(whip(WEND - 40, glow=.9, spray=2, wave=True)).crop((WEND - 170, 0, WEND - 20, WH))),
           ('surco (tierra revuelta)', _i(whip(X0W, 0, WEND, 0.0)).crop((60, 0, 210, WH))),
           ('aviso (grietas + polvo)', _i(whip_warn(.6, 1)).crop((60, 0, 210, WH))),
           ('corro: aviso', _i(cw)), ('corro: brota', _i(spikes(1.0, .9, burst=True))),
           ('espora · sombra · salpicadura', _cat([_i(spore(0)), _i(spore_mark(1)), _i(spore_mark(4)), _i(spore_mark(5)), spore_splash(0), spore_splash(2)]))]
    dh = max(f.height for _, f in det) * s + 30
    dw = sum(f.width * s + 14 for _, f in det) + 14
    W = max(scene.width * 2, dw)
    out = Image.new('RGBA', (W, scene.height * 2 + dh + 30), (16, 18, 16, 255))
    out.alpha_composite(scene.resize((scene.width * 2, scene.height * 2), Image.NEAREST), (0, 0))
    d = ImageDraw.Draw(out)
    x = 14; y0 = scene.height * 2 + 10
    for lab, f in det:
        bg = Image.new('RGBA', (f.width * s, f.height * s), C('#2e3128'))
        bg.alpha_composite(f.resize((f.width * s, f.height * s), Image.NEAREST))
        out.alpha_composite(bg, (x, y0 + 22))
        d.text((x, y0), lab, fill=(120, 230, 230), font=A.font(15))
        x += f.width * s + 14
    G._save(out, 'Lepidodendron ancestral · 6n-2 muestra: latigazo de raíces, corro de puntas y lluvia de esporas (arena ×2, detalles ×3)',
            ['Izquierda: latigazo en plena extensión (surge junto al borde, se arrastra abriendo surco). Derecha: el aviso de otro latigazo (grietas moradas + polvo).',
             'Rana: el corro de puntas avisa bajo ella; arriba a la derecha, un corro brotado. Esporas lila cayendo sobre su sombrita; a la izquierda, una salpicadura.'],
            'vista_previa_6n2_muestra.png')

def _cat(ims, gap=4):
    w = sum(i.width + gap for i in ims); h = max(i.height for i in ims)
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0)); x = 0
    for i in ims: out.alpha_composite(i, (x, (h - i.height) // 2)); x += i.width + gap
    return out

# =================================================================================================
# Animaciones (hojas)
# =================================================================================================
GIF = os.path.join(HERE, 'gif')
WARN_N, WARN_MS = 8, 100
SWEEP_MS = 55
# cuadros del latigazo: (punta, glow, surco hasta, montículo, tierra que salta)
_EXT = [int(round(X0W + 22 + (WEND - X0W - 22) * (1 - (1 - j / 10) ** 1.8))) for j in range(1, 11)]
# cuadros del latigazo: (punta, glow, surco hasta, montículo, tierra que salta, ola)
SWEEP = ([(X0W, .5, X0W, .45, -1, False), (X0W + 4, .7, X0W, .85, 0, False), (X0W + 22, .9, None, 1.0, 1, True)] +   # revienta y sale
         [(t, .9, None, 1.0, j + 2, True) for j, t in enumerate(_EXT)] +                                       # barre
         [(WEND, 1.0, None, 1.0, -1, False), (WEND, .7, None, 1.0, -1, False)] +                                # se queda
         [(t, .6, WEND, 1.0, -1, False) for t in (292, 236, 172, 108, 52, X0W + 12)] +                         # se recoge
         [(X0W, 0, WEND, .5, -1, False), (X0W, 0, WEND, 0.0, -1, False)])                                       # se cierra
SWEEP_HIT = (2, 19)                                    # cuadros con daño (la raíz fuera), ambos incluidos
SPW_N, SPW_MS = 6, 130
# corro: (crece por púa (la central es la 2), glow, estallido, tierra)
ERUPT = [((0, 0, .55, 0, 0), 1.0, True, 0), ((.45, .4, 1.06, .4, .45), 1.0, True, 0), ((1, 1, 1, 1, 1), 1.0, False, 0),
         ((1, 1, 1, 1, 1), .75, False, 0), ((1, 1, 1, 1, 1), .5, False, .2), ((.6, .6, .65, .6, .6), .3, False, .5),
         ((.25, .25, .3, .25, .25), .1, False, .8), ((0, 0, 0, 0, 0), 0, False, 1.0)]
ERUPT_MS = 80
ERUPT_HIT = (0, 4)

def spore_fall(k, n=4):
    """Espora que cae con su estela corta de motitas (12×24; pivote = centro de la espora (6, 18))."""
    cv = Canvas(12, 24)
    sp = spore(k, n)
    for (x, y), c in sp.px.items(): cv.put(x, y + 12, c)
    for j in range(5):                                  # estela: motitas que se quedan atrás y se apagan
        y = 11 - j * 2.2 - (k % 2)
        x = 5.5 + math.sin(j * 1.7 + k) * 1.5
        a = int(200 * (1 - j / 5))
        cv.put(x, y, SPORE_L[2 if j < 2 else 1][:3] + (a,))
    return cv

def build():
    out = {}
    out['Lepido_Whip_Warn'] = [_i(whip_warn(min(1, (k + 1) / 6), k)) for k in range(WARN_N)]
    out['Lepido_Whip_Sweep'] = [_i(whip(t, g, f, mo, sp, wv, k)) for k, (t, g, f, mo, sp, wv) in enumerate(SWEEP)]
    out['Lepido_Whip_Furrow'] = [out['Lepido_Whip_Sweep'][-1]]
    out['Lepido_Spikes_Warn'] = []
    for k in range(SPW_N):
        cv = Canvas(SW, SH); crack_ring(cv, min(1, .25 + k * .2), .5 + .5 * (k % 2) if k > 2 else .6); out['Lepido_Spikes_Warn'].append(_i(cv))
    out['Lepido_Spikes_Erupt'] = [_i(spikes(g, gl, burst=b, earth=e)) for (g, gl, b, e) in ERUPT]
    out['Lepido_Spikes_Scar'] = [out['Lepido_Spikes_Erupt'][-1]]
    out['Proj_LepidoSpore'] = [_i(spore_fall(k)) for k in range(4)]
    out['Fx_LepidoSporeMark'] = [_i(spore_mark(k)) for k in range(6)]
    out['Fx_LepidoSporeSplash'] = [spore_splash(k) for k in range(5)]
    return out

def piezas():
    out = build()
    for name, fr in out.items(): save_sheet(os.path.join(ATT, name + '.png'), fr)
    for name, ms in (('Lepido_Spikes_Erupt', ERUPT_MS), ('Proj_LepidoSpore', 100), ('Fx_LepidoSporeSplash', 60)):
        gif(os.path.join(GIF, name + '.gif'), out[name], ms, BG, 3)
    gif(os.path.join(GIF, 'Lepido_Whip_Sweep.gif'), out['Lepido_Whip_Sweep'], SWEEP_MS, BG, 2)
    print('   Attacks/ escrito:', len(out), 'hojas')
    return out

def lamina_piezas(out):
    rows = [('Lepido_Whip_Warn (8 × 100 ms): las grietas corren desde el borde', out['Lepido_Whip_Warn'][::2]),
            ('Lepido_Whip_Sweep (23 × 55 ms): revienta y sale (0-2), barre con la ola de tierra (3-12), se queda (13-14)', out['Lepido_Whip_Sweep'][:15:2]),
            ('… se recoge por el surco (15-20) y se cierra (21-22) = Lepido_Whip_Furrow', out['Lepido_Whip_Sweep'][15::2]),
            ('Lepido_Spikes_Warn (6 × 130 ms) · Lepido_Spikes_Erupt (8 × 80 ms)', out['Lepido_Spikes_Warn'][::2] + out['Lepido_Spikes_Erupt']),
            ('Proj_LepidoSpore (4) · Fx_LepidoSporeMark (6) · Fx_LepidoSporeSplash (5)',
             out['Proj_LepidoSpore'] + out['Fx_LepidoSporeMark'] + out['Fx_LepidoSporeSplash'])]
    s = 2; pad = 10
    hs = [max(f.height for f in fr) * s + 26 for _, fr in rows]
    W = max(sum(f.width * s + pad for f in fr) for _, fr in rows) + 2 * pad
    im = Image.new('RGBA', (W, sum(hs) + pad), C('#2e3128')); d = ImageDraw.Draw(im)
    y = pad
    for (lab, fr), h in zip(rows, hs):
        d.text((pad, y), lab, fill=(120, 230, 230), font=A.font(15))
        x = pad
        for f in fr:
            im.alpha_composite(f.resize((f.width * s, f.height * s), Image.NEAREST), (x, y + 20)); x += f.width * s + pad
        y += h
    G._save(im, 'Lepidodendron ancestral · 6n-2: hojas de los ataques (×2)',
            ['Latigazo: celda 352×56, pivote (0, 34) en el borde de la arena (derecha = Flip X). Corro: 80×96, pivote (40, 78). Espora: 12×24, pivote (6, 18).'],
            'vista_previa_6n2_piezas.png')

# =================================================================================================
# Escenas
# =================================================================================================
def escenas(out):
    L = arena_layers()
    frog = L['frog']
    keys = {}
    frames, ms = [], []
    def add(img, m, key=None):
        if key: keys[key] = len(frames)
        frames.append(img); ms.append(m)
    # --- latigazo: aviso (la rana sale de la franja), barre, se recoge, el surco se desvanece
    BY = 270
    fr_y = BY + 4
    fx_ = [300] * 4 + [300 - k * 10 for k in range(8)] + [220] * 40
    fy_ = [fr_y] * 4 + [fr_y + k * 5 for k in range(8)] + [fr_y + 40] * 40
    t = 0
    for k in range(3):
        add(arena_frame(L, t, objs=[(fy_[t], frog, fx_[t] - 16, fy_[t] - 30)]), 140); t += 1
    for k in range(WARN_N):
        add(arena_frame(L, t, ground=[(out['Lepido_Whip_Warn'][k], 0, BY - WCY)], objs=[(fy_[t], frog, fx_[t] - 16, fy_[t] - 30)]),
            WARN_MS, 'latigazo: aviso' if k == 4 else None); t += 1
    for k, fr in enumerate(out['Lepido_Whip_Sweep']):
        cm = ('shake', k) if 3 <= k <= 14 else ('sway', t)
        add(arena_frame(L, t, objs=[(BY + 6, fr, 0, BY - WCY), (fy_[min(t, 51)], frog, fx_[min(t, 51)] - 16, fy_[min(t, 51)] - 30)], canopy=cm),
            SWEEP_MS, {6: 'latigazo: barre (ola de tierra)', 13: 'latigazo: llega al centro', 18: 'latigazo: se recoge'}.get(k)); t += 1
    furrow = out['Lepido_Whip_Furrow'][0]
    for k in range(6):                                  # el surco se desvanece
        f = furrow.copy(); f.putalpha(f.split()[3].point(lambda v, a=1 - k / 6: int(v * a)))
        add(arena_frame(L, t, ground=[(f, 0, BY - WCY)], objs=[(fr_y + 40, frog, 220 - 16, fr_y + 10)]), 160, 'surco que se desvanece' if k == 1 else None); t += 1
    _gif('Lepido_latigazo', frames, ms)
    sheet1 = [(n, frames[i]) for n, i in keys.items()]

    # --- corro de puntas bajo la rana: aviso, la rana se aparta, brota, se hunde, tierra revuelta
    frames, ms, keys = [], [], {}
    CX, CY = 330, 250
    for k in range(SPW_N):
        fxp = CX + (0 if k < 3 else (k - 2) * 9)
        add(arena_frame(L, t, ground=[(out['Lepido_Spikes_Warn'][k], CX - SPX, CY - SPY)], objs=[(CY + 2, frog, fxp - 16, CY - 28)]),
            SPW_MS, 'corro: aviso' if k == 2 else None); t += 1
    for k, fr in enumerate(out['Lepido_Spikes_Erupt']):
        add(arena_frame(L, t, objs=[(CY, fr, CX - SPX, CY - SPY), (CY + 6, frog, CX + 34 - 16, CY - 24)]), ERUPT_MS if k < 5 else 110,
            {0: 'corro: la central', 2: 'corro: brota', 6: 'corro: se hunde'}.get(k)); t += 1
    scar = out['Lepido_Spikes_Scar'][0]
    for k in range(4):
        f = scar.copy(); f.putalpha(f.split()[3].point(lambda v, a=1 - k / 4: int(v * a)))
        add(arena_frame(L, t, ground=[(f, CX - SPX, CY - SPY)], objs=[(CY + 6, frog, CX + 34 - 16, CY - 24)]), 160); t += 1
    _gif('Lepido_corro', frames, ms)
    sheet2 = [(n, frames[i]) for n, i in keys.items()]

    # --- lluvia de esporas: sombras que se encogen, esporas que caen con estela, salpicaduras
    frames, ms, keys = [], [], {}
    rnd = random.Random(14)
    FALL = 10
    drops = [(rnd.uniform(140, 520), rnd.uniform(170, 330), rnd.randint(0, 22)) for _ in range(26)]
    fxp, fyp = 330, 300
    for k in range(34):
        ground, over = [], []
        for (x, y, t0) in drops:
            a = k - t0
            if 0 <= a < FALL:
                h = 220 * (1 - a / FALL) ** 1.3
                ground.append((out['Fx_LepidoSporeMark'][min(5, a * 6 // FALL)], x - 17, y - 11))
                over.append((out['Proj_LepidoSpore'][(k + t0) % 4], x - 6, y - 18 - h))
            elif FALL <= a < FALL + 5:
                over.append((out['Fx_LepidoSporeSplash'][a - FALL], x - 12, y - 11))
        fxp += 3 if (k // 8) % 2 == 0 else -3
        add(arena_frame(L, t, ground=ground, objs=[(fyp, frog, fxp - 16, fyp - 30)], over=over), 70,
            {9: 'esporas: caen sobre su sombra', 16: 'esporas: salpican'}.get(k)); t += 1
    _gif('Lepido_esporas', frames, ms)
    sheet3 = [(n, frames[i]) for n, i in keys.items()]

    allk = sheet1 + sheet2 + sheet3
    cols = 4; cw, ch = 640, 360
    rows = (len(allk) + cols - 1) // cols
    im = Image.new('RGBA', ((cw + 8) * cols, (ch + 26) * rows), (16, 18, 16, 255)); d = ImageDraw.Draw(im)
    for j, (nm, f) in enumerate(allk):
        x, y = (j % cols) * (cw + 8), (j // cols) * (ch + 26)
        im.alpha_composite(f, (x, y + 24)); d.text((x + 6, y + 4), nm, fill=(120, 230, 230), font=A.font(16))
    G._save(im, 'Lepidodendron ancestral · 6n-2 escenas: latigazo, corro de puntas y lluvia de esporas',
            ['Arena entera (×1, cámara ×0,75). GIF: gif/escenas/Lepido_latigazo.gif, Lepido_corro.gif, Lepido_esporas.gif'],
            'vista_previa_6n2_escenas.png')

def _gif(name, frames, ms):
    os.makedirs(os.path.join(GIF, 'escenas'), exist_ok=True)
    o = [f.convert('RGB') for f in frames]
    o[0].save(os.path.join(GIF, 'escenas', name + '.gif'), save_all=True, append_images=o[1:], duration=ms, loop=0)

if __name__ == '__main__':
    which = sys.argv[1:] or ['muestra']
    if 'muestra' in which: lamina_muestra()
    if 'piezas' in which or 'escenas' in which:
        out = piezas(); lamina_piezas(out)
        if 'escenas' in which: escenas(out)
