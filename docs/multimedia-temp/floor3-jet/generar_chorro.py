"""Chorro de vinagre del vinagrillo (piso 3), segunda versión.

- Tramo que se repite (16×10, 4 cuadros): núcleo con pulsos que avanzan, borde ondulado, brillos y gotitas que se separan.
- Tramo de inicio (nace fino de la punta de la cola y se ensancha) y tramo de punta (se afila y se deshace en gotas y rocío),
  con el mismo dibujo de fondo que el tramo normal, así encajan sin costura. El código ya no escala ni usa transparencia.
- Salida en la punta del abdomen, impacto cuando algo corta el chorro (girado con el chorro) y Salpicadura del nivel III
  (radio 2 u): salpicón ácido irregular, suelo mojado que burbujea y vaho; 3 variantes para que no se repita.
Escenas de revisión en gif/escenas/. Skill primordia-sprites (pixel.py).

Uso: python generar_chorro.py [carpeta_salida]   (por defecto, la carpeta de este script)
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
SKILL = os.path.join(HERE, '..', '..', '..', '.claude', 'skills', 'primordia-sprites', 'scripts')
sys.path.insert(0, os.path.abspath(SKILL))
from pixel import *
from PIL import Image

OUT = sys.argv[1] if len(sys.argv) > 1 else HERE
BG = C('#1c2e20')
VIN = ['#7a5a30', '#b8904e', '#e2c878', '#f6ecb4', '#fffbe6']     # vinagre: paja clara (lejos del lima y del fuego)
WET = ['#3a3420', '#4a4228']                                     # suelo mojado de vinagre (oscurece el suelo)
TW, TH = 16, 12                                                  # tramo: 1 u de largo; el centro del chorro, en y = 6
STREAM_MS, MUZZLE_MS, HIT_MS, SPLASH_MS = 60, 50, 60, 65
SPEED = 6                                                        # px por cuadro que avanza el dibujo (6 u/s)
PERIOD = 48                                                      # el dibujo se repite cada 3 tramos (A, B, C)
NF = PERIOD // SPEED                                             # 8 cuadros por bucle

def _circ(u, c, period=PERIOD):
    d = (u - c) % period
    return min(d, period - d)

# =================================================================================================
# Tramos
# =================================================================================================
def half_width(u):
    """Medio ancho del chorro en la fase u (periódica en 16 px): onda del borde y un pulso que avanza."""
    w = 2.55 + .4 * math.sin(2 * math.pi * u / PERIOD) + .35 * math.sin(6 * math.pi * u / PERIOD + 1.1) \
        + .2 * math.sin(10 * math.pi * u / PERIOD + 2.3)
    w += 1.0 * math.exp(-(_circ(u, 6) / 2.6) ** 2) + .55 * math.exp(-(_circ(u, 31) / 2.0) ** 2)   # dos pulsos desiguales
    return w

def jet_column(cv, x, u, env=1.0, core_boost=False):
    """Pinta una columna del chorro. env: 0..1, ancho relativo (para el inicio y la punta)."""
    hw = half_width(u) * env
    if hw < .35: return
    cy = TH / 2
    pulse = max(math.exp(-(_circ(u, 6) / 2.2) ** 2), .8 * math.exp(-(_circ(u, 31) / 1.6) ** 2))
    for y in range(TH):
        d = (y + .5 - cy)
        a = abs(d) / hw
        if a > 1: continue
        if a > .78: col = VIN[2] if d < 0 else VIN[1]                       # borde: claro arriba, oscuro abajo
        elif a > .45: col = VIN[3] if d < 0 else VIN[2]
        else: col = VIN[4] if ((pulse > .6 and a < .3) or core_boost) else VIN[3]
        cv.put(x, y, C(col))
    # brillo: rayita en la parte alta que viaja con el chorro
    if (_circ(u, 15) < 1.6 or _circ(u, 40) < 1.1) and hw > 2.2:
        cv.put_in(x, cy - hw * .55, C(VIN[4]))

FRINGE = ((2, -1, 0), (13, 1, 1), (21, -1, 1), (27, 1, 0), (36, -1, 0), (44, 1, 1))

def fringe(cv, i, off=0, xs=range(TW), env=lambda x: 1.0):
    """Gotitas que se separan del borde y viajan con el chorro (off: dónde empieza este tramo dentro del dibujo)."""
    for (u0, side, big) in FRINGE:
        x = (u0 + SPEED * i) % PERIOD - off
        if x not in xs: continue
        e = env(x)
        if e < .4: continue
        hw = half_width(x + off - SPEED * i) * e
        y = TH / 2 + side * (hw + 1.2)
        cv.put_out(x, y, C(VIN[2] if side < 0 else VIN[1]))
        if big: cv.put_out(x - 1, y, C(VIN[1] if side > 0 else VIN[2], 200))

def stream(i, ph):
    """Tramo ph (0 = A, 1 = B, 2 = C): el que va en el hueco k del chorro (k >= 1) es el (k - 1) % 3."""
    off = TW * (ph + 1) % PERIOD
    cv = Canvas(TW, TH)
    for x in range(TW): jet_column(cv, x, x + off - SPEED * i)
    fringe(cv, i, off)
    return cv

def env_start(x):
    t = min(1, (x + .5) / 13)
    return .22 + .78 * (t * t * (3 - 2 * t))

def start(i):
    """Inicio: nace fino de la punta de la cola (x = 0) y se ensancha hasta el ancho normal en x ≈ 13."""
    cv = Canvas(TW, TH)
    for x in range(TW): jet_column(cv, x, x - SPEED * i, env_start(x), core_boost=x < 4)
    fringe(cv, i, 0, xs=range(8, TW), env=env_start)
    return cv

def env_tip(x):
    t = min(1, max(0, x / 10.5))
    return max(0, 1 - t ** 1.6)

def tip(i, ph):
    """Punta libre: se afila hasta x ≈ 10 y se deshace en gotas y rocío que siguen avanzando. ph como en stream()."""
    off = TW * (ph + 1) % PERIOD
    cv = Canvas(TW, TH)
    for x in range(TW): jet_column(cv, x, x + off - SPEED * i, env_tip(x))
    fringe(cv, i, off, xs=range(0, 7), env=env_tip)
    rnd = random.Random(4 + ph)
    drops = [(rnd.uniform(0, 8), rnd.uniform(-2.2, 2.2), rnd.choice((0, 0, 1))) for _ in range(5)]
    for (x0, dy, big) in drops:                                  # gotas que se sueltan de la punta
        x = 8 + (x0 + SPEED * i * .5) % 8
        k = (x - 8) / 8                                           # se abren y se encogen al alejarse
        y = TH / 2 + dy * (1 + k)
        col = VIN[3] if k < .5 else VIN[2]
        cv.put(x, y, C(col, 255 if k < .6 else 190))
        if big and k < .6: cv.put(x - 1, y, C(VIN[2])); cv.put(x, y + 1, C(VIN[1]))
    return cv

# =================================================================================================
# Salida, impacto y Salpicadura
# =================================================================================================
def drop(cv, x, y, r, pal=VIN, alpha=255):
    """Gota de vinagre sin contorno (líquido claro), con brillo."""
    if r < 1.05:
        cv.put(x, y, C(pal[3], alpha)); return
    t = Canvas(cv.w, cv.h)
    ellipse(t, x, y, r, r * .85, pal[1:5], alpha=alpha)
    t.put_in(x - r * .4, y - r * .45, C(pal[4], alpha))
    for p, c in t.px.items(): cv.px[p] = c

def streak(cv, x, y, ang, L, col, alpha=255):
    for k in range(int(L) + 1):
        cv.put(x - math.cos(ang) * k, y - math.sin(ang) * k, C(col, alpha))

def muzzle(i):
    """Salida en la punta del abdomen (centro de la celda), hacia la derecha, en bucle: el vinagre sale a presión
    como un abanico corto con un anillo de rocío y una cuenta brillante."""
    cv = Canvas(16); c = 8
    rnd = random.Random(30 + i)
    r = (1.6, 2.1, 1.8, 2.3)[i % 4]                              # cuenta de vinagre que late en la boca
    t = Canvas(16)
    ellipse(t, c, c, r, r * .9, VIN[1:5])
    t.put_in(c - 1, c - 1, C(VIN[4]))
    for p, col in t.px.items(): cv.px[p] = col
    for k in range(4):                                           # rocío a los lados y algo hacia atrás (no en cono)
        a = math.radians(rnd.choice((-1, 1)) * rnd.uniform(70, 150)); d = r + rnd.uniform(1, 3.2)
        cv.put(c + math.cos(a) * d, c + math.sin(a) * d, C(VIN[3] if k % 2 else VIN[2], 220))
    if i % 2:                                                    # gotita que se suelta y cae
        cv.put(c - 1, c + r + 1.5, C(VIN[2])); cv.put(c - 1, c + r + 2.5, C(VIN[1], 180))
    return cv

def hit(i):
    """Impacto (24×24, centro = punto de impacto), con el chorro llegando desde la IZQUIERDA: hay que girarlo con el chorro.
    Corona de líquido aplastada contra lo que corta y salpicones irregulares hacia atrás y a los lados; en bucle."""
    cv = Canvas(24); c = 12
    rnd = random.Random(60 + i * 7)
    # lámina aplastada contra la superficie (a la derecha del centro), vertical
    for y in range(-5, 6):
        w = 2.4 - abs(y) * .32 + rnd.uniform(-.3, .3) + (.6 if (y + i) % 4 == 0 else 0)
        for x in range(int(-w), 1):
            a = abs(x) / max(.5, w)
            col = VIN[4] if a < .3 and abs(y) < 2 else VIN[3] if a < .7 else VIN[2]
            cv.put(c + 1 + x, c + y, C(col))
    # salpicones: más hacia atrás, de largos y tamaños distintos (cambian cada cuadro)
    for k in range(9):
        side = rnd.choice((-1, 1))
        a = math.pi + side * rnd.uniform(.35, 1.45)
        d = rnd.uniform(3.5, 8.6) * (1 + .1 * (i % 2))
        x, y = c + math.cos(a) * d, c + math.sin(a) * d
        r = rnd.uniform(.6, 1.6)
        if r > 1.1: drop(cv, x, y, r)
        else: cv.put(x, y, C(VIN[3]))
        streak(cv, x - math.cos(a) * (r + .5), y - math.sin(a) * (r + .5), a + math.pi, rnd.uniform(1, 3), VIN[2], 210)
    for k in range(4):                                           # rocío fino
        a = math.pi + rnd.uniform(-1.6, 1.6); d = rnd.uniform(8, 11)
        cv.put(c + math.cos(a) * d, c + math.sin(a) * d, C(VIN[2], 150))
    for k in range(2):                                           # gotas que escurren por la superficie
        y = c + rnd.choice((-1, 1)) * rnd.uniform(5, 7) + i
        cv.put(c + 1, y, C(VIN[2])); cv.put(c + 1, y + 1, C(VIN[1], 200))
    return cv

SPLASH_N = 80                                                    # radio 2 u = 32 px; celda de 80 para las gotas que se pasan

def splash(i, seed):
    """Salpicadura del nivel III (radio 2 u): salpicón ácido irregular. 0 se abomba, 1 revienta (cuadro que cuenta),
    2-4 las gotas vuelan desiguales y se estiran, 5-6 caen: el suelo mojado burbujea y suelta vaho."""
    cv = Canvas(SPLASH_N); c = SPLASH_N / 2
    rnd = random.Random(seed)
    mains = [rnd.uniform(0, 2 * math.pi)]
    mains += [mains[0] + rnd.uniform(1.8, 2.5), mains[0] + rnd.uniform(3.7, 4.5)]
    drops = []
    for k in range(20):
        if k < 13: a = rnd.choice(mains) + rnd.gauss(0, .42); d = rnd.uniform(.65, 1.15)
        else: a = rnd.uniform(0, 2 * math.pi); d = rnd.uniform(.35, .8)
        drops.append((a, d, rnd.uniform(.7, 1.9)))
    R = 30
    if i == 0:                                                   # se abomba
        rr = Canvas(SPLASH_N)
        lobes = [(c, c, 3.2)] + [(c + math.cos(a) * 3.6 * l, c + math.sin(a) * 3 * l, 2.4 * l)
                                 for a, l in zip(mains, (1.15, .85, 1.0))]
        lobes += [(c + math.cos(a + .9) * 3, c + math.sin(a + .9) * 2.4, 1.6) for a in mains[:2]]
        fill(rr, lambda x, y: sum(r * r / max(.01, (x - lx) ** 2 + (y - ly) ** 2) for lx, ly, r in lobes) >= 1, c, c, 6, 5, VIN[1:5],
             box=(c - 12, c - 12, c + 12, c + 12))
        for (a, d, sz) in drops[:4]:                             # primeras gotas que asoman
            drop(rr, c + math.cos(a) * 8, c + math.sin(a) * 6.5, .9)
        for p, v in rr.px.items(): cv.px[p] = v
        return cv
    if i == 1:                                                   # revienta: lenguas que salen y gotas
        for a in mains:
            L = rnd.uniform(9, 14)
            for t in range(int(L)):
                w = 2.6 * (1 - t / L) + .5
                for s in (-1, 0, 1):
                    if abs(s) <= w - .5:
                        x = c + math.cos(a) * t - math.sin(a) * s; y = c + (math.sin(a) * t + math.cos(a) * s) * .82
                        cv.put(x, y, C(VIN[4] if t < 3 else VIN[3] if t < L * .6 else VIN[2]))
        ellipse(cv, c, c, 5, 4, VIN[1:5])
        for (a, d, sz) in drops[:12]:
            drop(cv, c + math.cos(a) * 15 * d, c + math.sin(a) * 13 * d, sz)
        sparks(cv, c - 2, c - 2, 2)
        return cv
    prog = (0, 0, .58, .84, .97, 1.0, 1.0)[i]
    wet = set()
    for (a, d, sz) in drops:
        dist = R * d * prog
        x, y = c + math.cos(a) * dist, c + math.sin(a) * dist * .82
        if i <= 4:
            s_ = max(.8, (1.5 - .25 * (i - 2)) * sz)
            if s_ > 1.6 and i <= 3:                               # las gordas se estiran un poco en vuelo
                rot_ellipse(cv, x, y, s_ * 1.35, s_ * .9, math.atan2(math.sin(a) * .82, math.cos(a)), VIN[1:5])
                cv.put_in(x - s_ * .3, y - s_ * .4, C(VIN[4]))
            else:
                drop(cv, x, y, s_)
            if d * sz > .9:
                streak(cv, x - math.cos(a) * (s_ + .5), y - math.sin(a) * (s_ + .5) * .82, a, 2 + (4 - i), VIN[2], 200)
        else:                                                    # mancha de suelo mojado donde cae
            rr = 1.7 * sz * (1.15 if i == 5 else 1.0)
            for yy in range(int(y - rr) - 1, int(y + rr) + 2):
                for xx in range(int(x - rr * 1.3) - 1, int(x + rr * 1.3) + 2):
                    if ((xx + .5 - x) / (rr * 1.3)) ** 2 + ((yy + .5 - y) / (rr * .8)) ** 2 <= 1: wet.add((xx, yy))
    if i <= 3:                                                   # restos del centro
        for (dx, dy, rr) in ((-2, 1, 3.4), (3, -1, 2.6), (0, 3, 2.0)):
            q = (4.4 - i) / 2.4
            if rr * q > .9: drop(cv, c + dx * (1 + i * .5), c + dy * (1 + i * .5), rr * q)
    if wet:
        a_ = 200 if i == 5 else 140
        for (x, y) in wet:                                       # mancha: oscurece el suelo, borde de vinagre
            edge = any(q not in wet for q in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)))
            cv.put(x, y, C(VIN[0] if edge else WET[1], a_))
        bub = random.Random(seed + i)
        for k in range(10 if i == 5 else 7):                     # burbujas ácidas que revientan
            x, y = bub.choice(sorted(wet))
            if i == 5 or k % 2:
                cv.put(x, y, C(VIN[4])); cv.put(x + 1, y, C(VIN[3])); cv.put(x, y + 1, C(VIN[2]))
            else:
                for (dx, dy) in ((-1, 0), (1, 0), (0, -1), (0, 1)): cv.put(x + dx, y + dy, C(VIN[3], 220))
        for k in range(6):                                       # vaho que sube
            x, y = bub.choice(sorted(wet))
            for t in range(3 + (i - 5) * 2):
                cv.put(x + math.sin(t * 1.3 + k) * .8, y - 2 - t - (i - 5) * 2, C(VIN[3], 120 - t * 14 if i == 5 else 80 - t * 10))
    return cv

# =================================================================================================
# Escenas de revisión
# =================================================================================================
def ground(w, h, seed=3, tones=('#1c2e20', '#213626', '#273f2b', '#2f4a32')):
    rnd = random.Random(seed)
    im = Image.new('RGBA', (w, h), C(tones[0]))
    for _ in range(w * h // 18):
        x, y = rnd.randrange(w), rnd.randrange(h)
        t = C(tones[rnd.choice((1, 1, 1, 2, 2, 3))])
        for dx in range(rnd.choice((1, 2, 3))):
            if x + dx < w: im.putpixel((x + dx, y), t)
    return im

def strip(frame_i, length_px, blocked, sheets):
    """Chorro recto hacia la derecha de length_px: inicio, tramos y punta (o nada si lo corta algo: va el impacto)."""
    n = max(1, math.ceil(length_px / TW))
    im = Image.new('RGBA', (n * TW, TH), (0, 0, 0, 0))
    for k in range(n):
        ab = 'ABC'[(k - 1) % 3]
        if k == 0: t = sheets['Fx_VinegarJet_Start'][frame_i]
        elif k == n - 1 and not blocked: t = sheets[f'Fx_VinegarJet_Tip_{ab}'][frame_i]
        else: t = sheets[f'Fx_VinegarJet_Stream_{ab}'][frame_i]
        im.alpha_composite(as_img(t), (k * TW, 0))
    return im.crop((0, 0, int(length_px), TH))

def paste_rot(base, im, origin, ang_deg, anchor):
    """Pega im girado ang_deg (como Unity, al píxel más cercano) con su punto anchor en origin."""
    w, h = im.size
    big = Image.new('RGBA', (w * 2 + 4, w * 2 + 4), (0, 0, 0, 0))
    cx, cy = big.width // 2, big.height // 2
    big.alpha_composite(im, (cx - anchor[0], cy - anchor[1]))
    r = big.rotate(ang_deg, resample=Image.NEAREST, center=(cx, cy))
    base.alpha_composite(r, (int(origin[0] - cx), int(origin[1] - cy)))

def scene(path, sheets, body):
    W, H = 256, 200
    bg = ground(W, H)
    for x in range(222, 232):                                   # pared que corta el segundo chorro
        for y in range(60, 130): bg.putpixel((x, y), C('#3e5034' if x < 224 else '#2e3a28'))
    frames = []
    splashes = [sheets[f'Fx_VinegarJet_Splash_{k}'] for k in range(3)]
    for f in range(48):
        im = bg.copy()
        si = f % NF
        # 1) chorro libre (nivel I, 5 u) desde el vinagrillo, hacia la derecha
        bx, by = 10, 2
        im.alpha_composite(as_img(body[f % 4]), (bx, by))
        ox, oy = bx + 21, by + 11
        paste_rot(im, strip(si, 80, False, sheets), (ox, oy), 0, (0, TH // 2))
        paste_rot(im, as_img(sheets['Fx_VinegarJet_Muzzle'][f % 4]), (ox, oy), 0, (8, 8))
        # 2) chorro en diagonal cortado por la pared (impacto girado con el chorro)
        ox, oy = 120, 150
        ang = 28
        L = (222 - ox) / math.cos(math.radians(ang))
        ex, ey = ox + math.cos(math.radians(ang)) * L, oy - math.sin(math.radians(ang)) * L
        paste_rot(im, strip(si, L, True, sheets), (ox, oy), ang, (0, TH // 2))
        paste_rot(im, as_img(sheets['Fx_VinegarJet_Muzzle'][f % 4]), (ox, oy), ang, (8, 8))
        paste_rot(im, as_img(sheets['Fx_VinegarJet_Hit'][f % 4]), (ex, ey), ang, (12, 12))
        # 3) nivel III: 7 u, cortado por un enemigo; Salpicadura cada 0,5 s en el final, alternando variantes
        ox, oy = 14, 112
        ex, ey = ox + 7 * 16 - 8, oy
        im.alpha_composite(as_img(dummy()), (int(ex) - 2, int(ey) - 9))
        t = f * STREAM_MS
        k = (t // 500) % 3; sf = (t % 500) // SPLASH_MS
        if sf < 7: im.alpha_composite(as_img(splashes[k][sf]), (int(ex - SPLASH_N / 2), int(ey - SPLASH_N / 2)))
        paste_rot(im, strip(si, ex - ox, True, sheets), (ox, oy), 0, (0, TH // 2))
        paste_rot(im, as_img(sheets['Fx_VinegarJet_Muzzle'][f % 4]), (ox, oy), 0, (8, 8))
        paste_rot(im, as_img(sheets['Fx_VinegarJet_Hit'][f % 4]), (ex, ey), 0, (12, 12))
        frames.append(im)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    big = [fr.resize((W * 3, H * 3), Image.NEAREST).convert('RGB') for fr in frames]
    big[0].save(path, save_all=True, append_images=big[1:], duration=STREAM_MS, loop=0)
    return frames

def dummy():
    """Bulto de enemigo para la escena (no es arte del juego)."""
    cv = Canvas(16, 18)
    ellipse(cv, 8, 10, 6, 7, ['#3a2a3a', '#5a4058', '#7a5a78', '#a080a0'])
    cv.outline()
    return cv

def load_sheet(path, cell):
    im = Image.open(path).convert('RGBA')
    return [im.crop((i * cell, 0, (i + 1) * cell, cell)) for i in range(im.width // cell)]

# =================================================================================================
# Salida
# =================================================================================================
if __name__ == '__main__':
    E = {
        'Fx_VinegarJet_Start': [start(i) for i in range(NF)],
        'Fx_VinegarJet_Muzzle': [muzzle(i) for i in range(4)],
        'Fx_VinegarJet_Hit': [hit(i) for i in range(4)],
    }
    for ph, ab in enumerate('ABC'):
        E[f'Fx_VinegarJet_Stream_{ab}'] = [stream(i, ph) for i in range(NF)]
    for ph, ab in enumerate('ABC'):
        E[f'Fx_VinegarJet_Tip_{ab}'] = [tip(i, ph) for i in range(NF)]
    for k, seed in enumerate((101, 202, 303)):
        E[f'Fx_VinegarJet_Splash_{k}'] = [splash(i, seed) for i in range(7)]
    MS = {'Start': STREAM_MS, 'Stream': STREAM_MS, 'Tip': STREAM_MS, 'Muzzle': MUZZLE_MS, 'Hit': HIT_MS, 'Splash': SPLASH_MS}
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    for name, frames in E.items():
        save_sheet(os.path.join(OUT, 'Effects', name + '.png'), frames)
        gif(os.path.join(OUT, 'gif', name + '.gif'), frames, next(v for k, v in MS.items() if k in name), BG, scale=5)
    preview(os.path.join(OUT, 'vista_previa_chorro.png'), E, BG, scale=4)
    # Chorro montado (inicio + 3 tramos + punta), 4 cuadros, para ver que encaja sin costuras
    mont = {f'Montado_{i}': [strip(i, 112, False, E)] for i in range(0, NF, 2)}
    preview(os.path.join(OUT, 'vista_previa_montado.png'), mont, BG, scale=5, cols=1)
    body = load_sheet(os.path.join(HERE, '..', 'floor3-bodies', 'Whipscorpion', 'Player_Whipscorpion_AttackLoop_side.png'), 40)
    scene(os.path.join(OUT, 'gif', 'escenas', 'escena_chorro.gif'), E, body)
    print('Listo:', OUT)
