"""Lepidodendron ancestral · subtanda 6p-1: la barra de vida (sistema BossBarSkin, abajo, como los jefes).

El marco es una RAMA del árbol con su corteza de rombos; la vida es la SAVIA que corre por una hendidura abierta a lo
largo de la rama. A la izquierda, un trozo del tronco con sus vetas hace de «cabeza». Las 3 muescas (75 / 50 / 25 %:
las 3 paredes de raíces) son NUDOS morados como los de las paredes, que revientan al caer cada pared. En la fase
final (último 25 %) la savia se vuelve morada con brillo dorado y late deprisa, y del último tramo cuelgan los 3
CONOS con su estado (acorazado, armadura quemada, expuesto, roto).

Uso: python generar_barra_lepidodendron.py
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import generar_lepidodendron as G
from generar_lepidodendron import *                    # noqa: F401,F403
from generar_lepidodendron import _h
from PIL import Image, ImageDraw

OUT = os.path.join(HERE, 'BossBar')
PRE = 'UI_BossBar_Lepidodendron_'
W, H = 320, 44
WX0, WY0, WW, WH = 46, 13, 260, 8                      # ventana de la savia (Image Filled, de izquierda a derecha)
BY, BR = 17, 8                                          # eje y radio de la rama
BX0, BX1 = 30, 304                                      # la rama empieza dentro del tronco y acaba en la punta
NOTCHES = (.75, .5, .25)
CONE_X = (62, 80, 98)                                   # dónde cuelgan los conos (en el último tramo)
SAP = [C(c) for c in ('#1a0a2c', '#34145a', '#56268e', '#8048c4', '#b484ee', '#e6d0ff')]
GOLD = [C(c) for c in ('#6a3e10', '#a86a1c', '#e0a234', '#ffd468', '#fff2c0')]
WOOD_IN = [C(c) for c in ('#2a1e14', '#4a3624', '#6e5438', '#957452', '#bc9c74')]
CHAR = [C(c) for c in ('#0b090a', '#1a1517', '#2a2224', '#3e3432')]
EMB = [C(c) for c in EMBER]

def notch_x(t): return WX0 + round(WW * t)

def bark(x, y, cy, r, lit_top=True, cw=10, ch=8):
    """Corteza de rombos de una rama horizontal (eje y = cy, radio r), luz arriba."""
    v = max(-.999, min(.999, (y - cy) / r))
    th = math.asin(v)
    s_ = th * r * .9
    row = math.floor(x / cw)
    lx = (x + (cw / 2 if row % 2 else 0)) % cw - cw / 2
    ly = (s_ + (ch / 2 if row % 2 else 0)) % ch - ch / 2
    row = math.floor((s_ + (ch / 2 if math.floor(x / cw) % 2 else 0)) / ch)
    dd = abs(lx) / (cw / 2) + abs(ly) / (ch / 2)
    lam = .55 - .9 * v + (_h(math.floor(x / cw), row) - .5) * .14
    tone = 5 if lam > 1.15 else 4 if lam > .75 else 3 if lam > .35 else 2 if lam > -.1 else 1
    if dd > .84: c = BARK_OLD[max(0, tone - 2)]
    elif dd > .62 and lx < 0 and ly < 0: c = BARK_OLD[min(5, tone + 1)]
    elif dd < .2: c = BARK_OLD[max(0, tone - 2)]
    else: c = BARK_OLD[tone]
    if dd > .84 and v < -.3 and _h(x, y, 5) < .2: c = G.MOSS[1]
    return c

def frame(final=False):
    """El marco: la rama (con la hendidura de la savia), la punta con un penacho, sin la cabeza (va aparte)."""
    cv = Canvas(W, H)
    for x in range(BX0, BX1 + 1):
        r = BR * (1 - max(0, (x - (BX1 - 14)) / 16) ** 1.4 * .55)
        for y in range(int(BY - r), int(BY + r) + 1):
            if abs(y - BY) > r: continue
            cv.put(x, y, bark(x, y, BY, r))
    if final:                                            # fase final: grietas de la corteza con luz dorada y morada
        rnd = random.Random(4)
        for j in range(16):
            x = rnd.uniform(BX0 + 14, BX1 - 16); y = BY + rnd.choice((-1, 1)) * rnd.uniform(5, 7)
            for q in range(rnd.randint(3, 6)):
                cv.put(x + q, y + (q % 2) * (1 if y > BY else -1), GOLD[3] if q % 3 else G.VEIN[3])
    rnd = random.Random(9)                               # penacho de hojas de aguja en la punta
    for j in range(16):
        a = rnd.uniform(-1.2, 1.2)
        frondlet(cv, BX1 - 2, BY, a, rnd.uniform(8, 14), G.LEAF, .4, 1.3, None, 0 if a > 0 else 1)
    # la hendidura: labios de madera clara arriba, en sombra abajo; dentro, vacío (ahí se ve la savia)
    for x in range(WX0 - 1, WX0 + WW + 1):
        for y in range(WY0 - 1, WY0 + WH + 1):
            if WX0 <= x < WX0 + WW and WY0 <= y < WY0 + WH:
                cv.px.pop((x, y), None); continue
            cv.put(x, y, WOOD_IN[3] if y < WY0 else WOOD_IN[1])
    cv.outline(); cv.clean()
    for x in range(WX0, WX0 + WW):                       # la hendidura no lleva contorno por dentro
        for y in range(WY0, WY0 + WH): cv.px.pop((x, y), None)
    return cv.img().convert('RGBA')

HEAD_W, HEAD_H = 46, 44
def head(mode='calm', k=0):
    """La «cabeza»: un trozo del tronco (con el pie abierto) del que nace la rama, con 3 vetas moradas.
    mode calm (laten despacio), final (moradas y doradas, deprisa), hit (destello)."""
    cv = Canvas(HEAD_W, HEAD_H)
    cx = 19
    for y in range(0, HEAD_H - 1):
        t = y / (HEAD_H - 2)
        hw = 12 + 6 * max(0, (t - .7) / .3) ** 2       # el pie se abre
        for x in range(int(cx - hw), int(cx + hw) + 1):
            ph = max(-.999, min(.999, (x - cx) / hw))
            th = math.asin(ph)
            s_ = .5 * (x - cx) + .5 * th * hw
            cw, ch = 7, 8
            row = math.floor((HEAD_H - y) / ch)
            lx = (s_ + (cw / 2 if row % 2 else 0)) % cw - cw / 2
            ly = (HEAD_H - y) % ch - ch / 2
            dd = abs(lx) / (cw / 2) + abs(ly) / (ch / 2)
            lam = .5 - .78 * ph
            tone = 4 if lam > .7 else 3 if lam > .25 else 2 if lam > -.15 else 1
            c = BARK_OLD[max(0, tone - 2)] if dd > .86 else BARK_OLD[min(5, tone + 1)] if (dd > .66 and lx < 0 and ly > 0) else BARK_OLD[tone]
            if y < 6 and not dither(y / 6, x, y): continue                 # arriba se pierde (sale de la copa)
            cv.put(x, y, c)
    ph = .5 + .5 * math.sin(2 * math.pi * k / (4 if mode == 'calm' else 3))
    for j, t0 in enumerate((-.55, 0, .5)):              # vetas
        for y in range(4, HEAD_H - 4):
            t = y / (HEAD_H - 2)
            hw = 12 + 6 * max(0, (t - .7) / .3) ** 2
            x = cx + math.sin(t0 + .12 * math.sin(y / 6 + j)) * hw
            if mode == 'calm': c = G.VEIN[2 + (1 if ph > .5 and (y + k * 3) % 9 < 4 else 0)]
            elif mode == 'final': c = (GOLD[3] if (y + k * 5) % 7 < 2 else G.VEIN[4]) if ph > .3 else G.VEIN[3]
            else: c = G.WHITE_V if k == 0 else G.VEIN[4]
            cv.put(x, y, c)
            if mode != 'calm': cv.put(x + 1, y, G.VEIN[3])
    cv.outline(); cv.clean()
    return cv.img().convert('RGBA')

def back():
    im = Image.new('RGBA', (WW, WH), (12, 9, 7, 255)); d = ImageDraw.Draw(im)
    d.line([(0, 0), (WW, 0)], fill=(6, 4, 4)); return im

def fill(kind='sap', k=0):
    """La savia: morada que late despacio (sap), morada con brillo dorado que late deprisa (gold) o el rastro."""
    im = Image.new('RGBA', (WW, WH)); p = im.load()
    for x in range(WW):
        for y in range(WH):
            if kind == 'trail':
                p[x, y] = C('#efe4f6') if y < 4 else C('#c8b4d8'); continue
            c = SAP[4] if y == 0 else SAP[3] if y < 3 else SAP[2] if y < 6 else SAP[1]
            if kind == 'sap':
                ph = (x / 40 + k / 4) % 1                 # una gota más clara que corre hacia la cabeza
                if ph < .12 and 1 <= y <= 4: c = SAP[4] if y < 3 else SAP[3]
                if (x * 3 + y * 7) % 23 == 0: c = SAP[2]                     # burbujas
            else:
                ph = (x / 26 + k / 3) % 1
                if ph < .16 and y < 5: c = GOLD[3] if y < 2 else GOLD[2]
                elif (x + y * 5 + k * 7) % 19 == 0: c = GOLD[4]               # chispas doradas
            p[x, y] = c
    return im

def knot(state='idle', k=0):
    """Nudo morado de la muesca (12×20, centrado en la muesca y en el eje de la rama). idle: la luz de sus grietas
    late; hit: destella; broken: la herida que queda (hueco oscuro con savia)."""
    cv = Canvas(12, 20)
    cx, cy = 5.5, 9.5
    for y in range(20):
        for x in range(12):
            u, v = (x - cx) / 5.6, (y - cy) / 9.4
            d = u * u + v * v
            if d > 1: continue
            if state == 'broken' and d < .45:
                c = (14, 10, 16, 255) if d < .3 else SAP[1]
            else:
                lam = -u * .6 - v * .7
                tone = 4 if lam > .45 else 3 if lam > 0 else 2 if lam > -.45 else 1
                ring = (math.sqrt(d) * 3.2) % 1 < .25
                c = BARK_OLD[max(0, tone - (1 if ring else 0))]
            cv.put(x, y, c)
    if state != 'broken':                               # grietas con luz morada (como los nudos de las paredes)
        ph = .5 + .5 * math.sin(2 * math.pi * k / 4)
        hot = state == 'hit' and k == 0
        for pts in (((5, 3), (5, 4), (6, 5), (6, 6), (5, 7), (5, 8)), ((3, 10), (3, 11), (4, 12), (4, 13), (3, 14)),
                    ((7, 10), (8, 11), (8, 12), (7, 13), (8, 14), (8, 15))):
            for i_, (x, y) in enumerate(pts):
                core = G.WHITE_V if hot else G.VEIN[4] if (state == 'hit' or ph > .55) else G.VEIN[3]
                cv.put(x, y, core if i_ % 2 == 0 else G.VEIN[3])
                cv.put(x + 1, y, G.VEIN[2] if not hot else G.VEIN[4])
    else:
        for (x, y) in ((4, 15), (4, 16), (7, 14), (7, 15), (7, 16)): cv.put(x, y, SAP[3])   # savia que gotea
    cv.outline(); cv.clean()
    return cv.img().convert('RGBA')

def fx_knot_burst(k, n=6):
    """El nudo revienta (32×40, centrado en la muesca): destello, salpicadura de savia morada y astillas."""
    im = Image.new('RGBA', (32, 40), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    if k == 0:
        d.ellipse([8, 12, 24, 28], fill=SAP[4][:3] + (220,)); d.ellipse([12, 16, 20, 24], fill=(255, 255, 255, 255))
    rnd = random.Random(5)
    for j in range(16):
        a = rnd.uniform(0, 2 * math.pi); v = rnd.uniform(1.5, 3.5)
        x = 16 + math.cos(a) * v * k * 1.3; y = 20 + math.sin(a) * v * k + .5 * k * k
        if k >= n - 1 and j % 2: continue
        col = SAP[3 + (j % 2)] if j % 3 else WOOD_IN[3]
        d.rectangle([x, y, x + (1 if j % 2 else 0), y + 1], fill=col)
    return im

CONE_W, CONE_H = 12, 20
def cone(state='armor', k=0):
    """Cono pequeño que cuelga de la rama (12×20; arriba en el centro = donde cuelga). armor (ramas en espiral),
    charred (armadura quemada con ascuas), exposed (escamas abiertas, late morado), broken (apagado)."""
    cv = Canvas(CONE_W, CONE_H)
    cx = 5.5
    for y in range(3):                                   # el rabillo
        cv.put(cx, y, BARK_OLD[3]); cv.put(cx + 1, y, BARK_OLD[2])
    for y in range(3, CONE_H - 1):
        t = (y - 3) / (CONE_H - 5)
        hw = 4.6 * math.sin(math.pi * min(1, t * .95 + .05)) ** .7
        for x in range(int(cx - hw), int(cx + hw) + 2):
            u = (x - cx - .5) / max(hw, .5)
            if abs(u) > 1: continue
            lam = -u * .7 - (t - .5) * .5
            tone = 3 if lam > .4 else 2 if lam > -.1 else 1
            if state == 'armor':
                band = ((y + x * .9) % 5) < 2.4
                c = BARK_OLD[tone + 1] if band else G.VEIN[1]
            elif state == 'charred':
                band = ((y + x * .9) % 5) < 2.4
                c = CHAR[tone] if band else G.VEIN[0]
                if band and _h(x, y, 2) < .15: c = EMB[2]
            elif state == 'exposed':
                ph = .5 + .5 * math.sin(2 * math.pi * k / 3)
                sc = ((x + y) % 3 == 0) or ((x - y) % 3 == 0)
                c = (G.VEIN[4] if ph > .5 else G.VEIN[3]) if sc else SAP[2 + (1 if tone > 2 else 0)]
            else:
                sc = ((x + y) % 3 == 0) or ((x - y) % 3 == 0)
                c = (34, 28, 40, 255) if sc else (52, 44, 58, 255)
                if abs(x - cx - .5) < .8 and y > 6: c = (14, 10, 16, 255)    # partido
            cv.put(x, y, c)
    cv.outline(); cv.clean()
    return cv.img().convert('RGBA')

def cone_appear(k, n=4):
    """Los conos bajan de la rama al empezar la fase final (el cono crece hacia abajo)."""
    full = cone('armor')
    h = max(3, int(CONE_H * (k + 1) / n))
    out = Image.new('RGBA', (CONE_W, CONE_H), (0, 0, 0, 0))
    out.alpha_composite(full.crop((0, CONE_H - h, CONE_W, CONE_H)), (0, 0))
    return out

# =================================================================================================
def mock(ratio, kind='sap', final=False, trail=None, broken=(), hmode='calm', k=0, cones=None, burst=None):
    im = Image.new('RGBA', (W + 16, H + 16), (0, 0, 0, 0))
    ox, oy = 8, 8
    im.alpha_composite(back(), (ox + WX0, oy + WY0))
    if trail:
        tr = fill('trail').crop((0, 0, max(1, int(WW * trail)), WH)); im.alpha_composite(tr, (ox + WX0, oy + WY0))
    if ratio > 0:
        f = fill(kind, k).crop((0, 0, max(1, int(WW * ratio)), WH)); im.alpha_composite(f, (ox + WX0, oy + WY0))
    im.alpha_composite(frame(final), (ox, oy))
    im.alpha_composite(head(hmode, k), (ox, oy))
    for i, t in enumerate(NOTCHES):
        st = 'broken' if i in broken else 'idle'
        im.alpha_composite(knot(st, k), (ox + notch_x(t) - 6, oy + BY - 10))
    if burst is not None:
        i, bk = burst
        im.alpha_composite(fx_knot_burst(bk), (ox + notch_x(NOTCHES[i]) - 16, oy + BY - 20))
    if cones:
        for x, c in zip(CONE_X, cones):
            if c is not None: im.alpha_composite(c, (ox + x - CONE_W // 2, oy + BY + BR - 1))
    return im

def hojas():
    os.makedirs(OUT, exist_ok=True)
    def save(frames, name):
        frames = frames if isinstance(frames, list) else [frames]
        save_sheet(os.path.join(OUT, PRE + name + '.png'), frames)
    save(frame(False), 'Frame'); save(frame(True), 'Frame_Final')
    save([head('calm', k) for k in range(4)], 'Head_Calm')
    save([head('final', k) for k in range(3)], 'Head_Final')
    save([head('hit', k) for k in range(2)], 'Head_Hit')
    save(back(), 'Back')
    save([fill('sap', k) for k in range(4)], 'Fill_Sap')
    save([fill('gold', k) for k in range(3)], 'Fill_SapGold')
    save(fill('trail'), 'Fill_Trail')
    save([knot('idle', k) for k in range(4)], 'Knot')
    save([knot('hit', k) for k in range(2)], 'Knot_Hit')
    save(knot('broken'), 'Knot_Broken')
    save([fx_knot_burst(k) for k in range(6)], 'Fx_KnotBurst')
    save(cone('armor'), 'Cone_Armor'); save(cone('charred'), 'Cone_Charred')
    save([cone('exposed', k) for k in range(3)], 'Cone_Exposed'); save(cone('broken'), 'Cone_Broken')
    save([cone_appear(k) for k in range(4)], 'Cone_Appear')
    # vista previa por estados y GIF del combate
    A_, Ch, Ex, Br = cone('armor'), cone('charred'), lambda k: cone('exposed', k), cone('broken')
    rows = {
        'pared 1 (100-75 %): savia morada que late despacio': [mock(r, trail=r + .05, k=i) for i, r in enumerate((1.0, .93, .86, .79))],
        '75 %: cae la pared 1, el nudo revienta': [mock(.75, broken=(0,) if bk > 1 else (), burst=(0, bk), k=bk) for bk in range(0, 6, 2)],
        'paredes 2 y 3 (75-25 %)': [mock(r, trail=r + .05, broken=(0,) if r < .5 else (0,), k=i) for i, r in enumerate((.7, .6))] +
                                   [mock(r, trail=r + .05, broken=(0, 1), k=i) for i, r in enumerate((.45, .3))],
        '25 %: fase de los conos (savia dorada, bajan los conos)': [mock(.25, 'gold', True, broken=(0, 1, 2), hmode='final', k=k,
                                                                         cones=[cone_appear(min(3, k))] * 3) for k in range(4)],
        'conos: acorazado, quemado, expuesto, roto': [mock(r, 'gold', True, broken=(0, 1, 2), hmode='final', k=i, trail=r + .03,
                                                           cones=cs) for i, (r, cs) in enumerate(((.22, [A_, A_, A_]), (.18, [Ch, A_, A_]), (.12, [Br, Ex(1), A_]), (.04, [Br, Br, Ex(2)])))],
    }
    G_preview = preview
    G_preview(os.path.join(HERE, 'vista_previa_6p_barra.png'), rows, C('#0c0a10'), scale=3)
    seq, ms = [], []
    for i in range(8): seq.append(mock(1 - i * .03, trail=1.03 - i * .03, k=i)); ms.append(120)
    for bk in range(6): seq.append(mock(.75, broken=(0,) if bk > 1 else (), burst=(0, bk), k=bk)); ms.append(70)
    for i in range(10): seq.append(mock(.74 - i * .05, trail=.77 - i * .05, broken=(0,) if i < 5 else (0, 1), k=i)); ms.append(110)
    for k in range(6): seq.append(mock(.25, 'gold', True, broken=(0, 1, 2), hmode='final', k=k, cones=[cone_appear(min(3, k))] * 3)); ms.append(90)
    for i, cs in enumerate(([A_, A_, A_], [Ch, A_, A_], [Ex(0), A_, A_], [Br, A_, A_], [Br, Ex(1), A_], [Br, Br, A_], [Br, Br, Ex(2)], [Br, Br, Br])):
        seq.append(mock(max(0, .24 - i * .034), 'gold', True, broken=(0, 1, 2), hmode='final', k=i, cones=cs)); ms.append(160)
    big = [s_.resize((s_.width * 3, s_.height * 3), Image.NEAREST) for s_ in seq]
    os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True)
    bgc = Image.new('RGBA', big[0].size, C('#0c0a10'))
    o = []
    for f in big:
        b = bgc.copy(); b.alpha_composite(f); o.append(b.convert('RGB'))
    o[0].save(os.path.join(HERE, 'gif', 'Lepido_BossBar_combate.gif'), save_all=True, append_images=o[1:], duration=ms, loop=0)
    print('   BossBar/ escrito')

if __name__ == '__main__':
    hojas()
