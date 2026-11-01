"""Barra de vida de la Megarachne (tanda 6k, sistema BossBarSkin como el Arthropleura y la Meganeura). Skill primordia-sprites.

Minijefe: barra más corta (240×28) arriba de la pantalla. El marco es un CABLE DE SEDA trenzado (la ventana va por
dentro) con la Megarachne vista desde arriba agarrada a la izquierda con sus 8 patas; a la derecha el cable se ata a
un muñón de rama de Lepidodendron.

Ventana 176×6 en (46, 11). Muesca al 50 % (nudo de seda). Estados:
- Fill_Violet: normal (gris violáceo con bandas de segmentos y manchas rojas).
- Fill_Silk: INVULNERABLE mientras está en su cable (el relleno se cubre de seda gris tramada).
- Fill_Rage (3): enfado, rojo que late.  Fill_Trail: el daño reciente.
- Frame / Frame_Rage: el marco normal y con espinas de punta roja (el icono, enfurecido).
- Eyes_Calm / Eyes_Rage: capa con los ojos del icono.
- Fx_Screech (6, 72×40, centrado en el icono): ondas rojas del chillido del 50 %.
- Fx_Spines (4): las espinas brotan del cable (capa del tamaño del marco), justo tras el chillido.
- Notch / Notch_Broken / Fx_NotchBreak (5, 16×34): el nudo del 50 % y cómo se rompe.

Uso: python generar_interfaz_megarachne.py     (hojas en BossBar/, vista previa y GIF)
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
MT = os.path.normpath(os.path.join(HERE, '..', '..'))
sys.path.insert(0, HERE)
import generar_megarachne as MG
from base_piso3 import *                               # noqa: F401,F403
from PIL import Image, ImageDraw

W, H = 240, 28
WX0, WY0, WW, WH = 46, 11, 176, 6
CAB0, CAB1, CY0, CY1 = 38, 228, 7, 20                  # el cable (x de inicio y fin; y de arriba y abajo)
NOTCH = .5
OUT = os.path.join(HERE, 'BossBar')
PRE = 'UI_BossBar_Megarachne_'
SILK = [C('#2a2234'), C('#6a6078'), C('#9a90a8'), C('#c4bed4'), C('#eeeaf6'), C('#fafaff')]
VIO = [C(c) for c in ('#1c141e', '#30243a', '#4a3a4a', '#6e5a6a', '#9a8494')]
REDS = [C(c) for c in ('#4a0e12', '#a82a2a', '#e84a3a', '#ff9a7a', '#ffd8b0')]
BARK = [C('#1e2420'), C('#2e3830'), C('#435040'), C('#5e6c52')]

# -------------------------------------------------------------------------------------------------
# Icono: la Megarachne vista desde arriba, agarrada al cable (motor 3D a escala de icono)
# -------------------------------------------------------------------------------------------------
_ICON = {}
def icon(rage=False):
    if rage in _ICON: return _ICON[rage]
    import math as m
    MG.AR.ELEV = m.radians(34)                              # de frente: cara con los ojos rojos y patas abiertas
    pose = dict(legs_up=.45, pitch=10, rage=rage, wave=.1 if rage else None)
    g = MG.body_geo(pose)
    g.P = [P * .33 for P in g.P]
    cv = MG.AR.render_geo(g, -90, 48, origin=(24, 27), pal=MG.RAGE_PAL if rage else None)
    MG.AR.ELEV = m.radians(55)
    im = cv.img().convert('RGBA')
    px = im.load()                                        # a esta escala los ojos se pierden: rojos y vivos
    eye = {tuple(C(c)[:3]) for c in (MG.RAGE_PAL['eye'] if rage else MG.PAL_A['eye'])}
    for y in range(48):
        for x in range(48):
            c = px[x, y]
            if c[3] and c[:3] in eye: px[x, y] = (REDS[3] if rage else REDS[2])[:3] + (255,)
    _ICON[rage] = im
    return im

# -------------------------------------------------------------------------------------------------
# Marco: cable de seda opaco (ventana hueca), muñón de rama, icono
# -------------------------------------------------------------------------------------------------
def cable(cv, rage=False, spines=0.0):
    for x in range(CAB0, CAB1 + 1):                       # cuerda de seda: trenzado en diagonal (como una soga)
        for y in range(CY0, CY1 + 1):
            if WX0 - 1 <= x <= WX0 + WW and WY0 - 1 <= y <= WY0 + WH: continue
            t = (y - CY0) / (CY1 - CY0)
            q = (x + (y - CY0) * 1.3) % 6
            c = SILK[4] if q < 1.6 else SILK[1] if q > 4.8 else SILK[3] if .25 < t < .75 else SILK[2]
            if q < .8 and t < .5: c = SILK[5]
            cv.put(x, y, c)
        cv.put(x, CY0, SILK[4] if x % 3 else SILK[5]); cv.put(x, CY1, SILK[1])   # filos
        if x % 13 == 7: cv.put(x, CY1 + 1, SILK[3])       # hilacha que cuelga
    for x in range(WX0 - 1, WX0 + WW + 1):                # marco oscuro de la ventana
        cv.put(x, WY0 - 1, SILK[0]); cv.put(x, WY0 + WH, SILK[0])
    for y in range(WY0 - 1, WY0 + WH + 1):
        cv.put(WX0 - 1, y, SILK[0]); cv.put(WX0 + WW, y, SILK[0])
    if spines:                                            # espinas de punta roja que brotan del cable (enfado)
        for i, x in enumerate(range(CAB0 + 16, CAB1 - 6, 12)):
            L = round(4 * spines) if spines < 1 else 4
            for side, y0, dy in ((0, CY0 - 1, -1), (1, CY1 + 1, 1)):
                xx = x + (6 if side else 0)
                for j in range(L):
                    cv.put(xx + (j // 2) * (1 if i % 2 else -1), y0 + dy * j, REDS[3] if j == L - 1 else REDS[1] if j > 0 else SILK[1])

def stub(cv):
    """Muñón de rama de Lepidodendron a la derecha, con el cable atado (vueltas de seda)."""
    x0, x1, y0, y1 = 226, 238, 3, 25
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            u = (x - x0) / (x1 - x0)
            if abs(u - .5) * 2 > 1 - .25 * abs((y - 14) / 11) ** 3: continue
            lx = (x - x0 + (3 if ((y - y0) // 5) % 2 else 0)) % 6 - 2.5
            ly = (y - y0) % 5 - 2
            dd = abs(lx) / 3 + abs(ly) / 2.5
            light = 3 if u < .3 else 2 if u < .7 else 1
            cv.put(x, y, BARK[max(0, light - 1)] if dd > .9 else BARK[light])
    for k in range(4):                                    # vueltas de seda que atan el cable
        y = CY0 + 2 + k * 3
        for x in range(x0 - 2, x1 + 1, 1):
            if (x + k) % 3: cv.put(x, y, SILK[4] if k % 2 else SILK[3])

def frame(rage=False, spines=None):
    cv = Canvas(W, H)
    cable(cv, rage, spines=(1.0 if rage else 0.0) if spines is None else spines)
    stub(cv)
    cv.outline(SILK[0])
    im = cv.img().convert('RGBA')
    ic = icon(rage)
    im.alpha_composite(ic, (22 - 24, 14 - 24))         # icono a la izquierda, agarrado al cable
    return im

def eyes(rage):
    """Capa con solo los ojos del icono (para que el código los haga brillar/parpadear)."""
    ic = icon(rage)
    lay = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    px = ic.load()
    eye_cols = {tuple(C(c)[:3]) for c in (MG.RAGE_PAL['eye'] if rage else MG.PAL_A['eye'])} | {(192, 48, 42)}
    for y in range(48):
        for x in range(48):
            c = px[x, y]
            if c[3] and c[:3] in eye_cols:
                X, Y = x + 22 - 24, y + 14 - 24
                if 0 <= X < W and 0 <= Y < H: lay.putpixel((X, Y), (REDS[4] if rage else c)[:3] + (255,) if rage and c[:3] != tuple(C(MG.RAGE_PAL['eye'][0])[:3]) else c)
    return lay

# -------------------------------------------------------------------------------------------------
# Ventana: fondo y rellenos (176×6, se recortan por la vida: Image Filled horizontal)
# -------------------------------------------------------------------------------------------------
def back():
    im = Image.new('RGBA', (WW, WH), C('#0c0810')); d = ImageDraw.Draw(im)
    d.line([(0, 0), (WW, 0)], fill=C('#06040a'))
    return im

def fill(kind, k=0):
    im = Image.new('RGBA', (WW, WH)); p = im.load()
    for x in range(WW):
        for y in range(WH):
            if kind in ('violet', 'silk'):
                c = VIO[3] if y == 0 else VIO[2] if y < 3 else VIO[1] if y < 5 else VIO[0]
                if x % 11 == 0: c = VIO[1]                            # bandas de segmentos
                if x % 22 in (5, 6) and y in (2, 3): c = REDS[1] if y == 3 else REDS[2]   # manchas rojas
            elif kind == 'rage':
                ph = (x / 22 - k / 3) % 1
                c = REDS[3] if y == 0 else REDS[2] if y < 3 else REDS[1] if y < 5 else REDS[0]
                if ph < .18 and y < 4: c = REDS[4] if y == 0 else REDS[3]               # latido que recorre la barra
                if x % 11 == 0: c = REDS[0] if y else REDS[1]
            else:                                                     # trail
                c = C('#f0e4ec') if y < 3 else C('#c8b4c4')
            p[x, y] = c
    if kind == 'silk':                                                # seda gris tramada encima: invulnerable
        for x in range(WW):
            for y in range(WH):
                if (x + y) % 4 == 0 or (x - y) % 4 == 0:
                    p[x, y] = SILK[3] if y < 3 else SILK[2]
                elif (x + 2 * y) % 7 == 0: p[x, y] = SILK[4]
    return im

def notch(broken=False):
    """Nudo de seda del 50 % (7×14, se pone centrado en la muesca, sobre el marco)."""
    cv = Canvas(7, 14)
    for y in range(14):
        for x in range(7):
            d = ((x - 3) / 3.4) ** 2 + ((y - 6.5) / 7) ** 2
            if d <= 1:
                if broken and 4 <= y <= 9 and abs(x - 3) < 2: continue
                cv.put(x, y, SILK[4] if d < .4 else SILK[3] if d < .75 else SILK[2])
    if broken:
        for (x, y) in ((1, 10), (5, 4), (2, 3), (4, 11)): cv.put(x, y, SILK[5])
    cv.outline(SILK[0])
    return cv.img()

def fx_notch(k, n=5):
    """El nudo se rompe (16×34): hilachas que saltan."""
    cv = Canvas(16, 34); rnd = random.Random(k + 3)
    t = (k + 1) / n
    for j in range(10):
        a = rnd.uniform(0, 2 * math.pi); r = 2 + 12 * t * rnd.uniform(.5, 1)
        x, y = 8 + math.cos(a) * r, 17 + math.sin(a) * r * 1.4
        cv.put(x, y, SILK[5] if j % 3 else REDS[2])
        if k < 3: cv.put(x + 1, y, SILK[3])
    return cv.img()

def fx_screech(k, n=6):
    """Ondas rojas del chillido desde el icono (72×40, centro = icono)."""
    im = Image.new('RGBA', (72, 40), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    for j in range(3):
        t = ((k / n) + j / 3) % 1
        rx, ry = 8 + 28 * t, (8 + 28 * t) * .5
        a = int(255 * (1 - t) ** .8)
        d.ellipse([36 - rx, 20 - ry, 36 + rx, 20 + ry], outline=(255, 90, 70, a), width=2)
    return im

def fx_spines(k, n=4):
    """Las espinas brotan del cable (capa del tamaño del marco, sin el resto)."""
    cv = Canvas(W, H)
    s_ = (k + 1) / n
    for i, x in enumerate(range(CAB0 + 16, CAB1 - 6, 12)):
        L = max(1, round(4 * s_))
        for side, y0, dy in ((0, CY0 - 1, -1), (1, CY1 + 1, 1)):
            xx = x + (6 if side else 0)
            for j in range(L):
                cv.put(xx + (j // 2) * (1 if i % 2 else -1), y0 + dy * j, REDS[4] if j == L - 1 and k == 0 else REDS[3] if j == L - 1 else REDS[1])
    return cv.img()

# -------------------------------------------------------------------------------------------------
# Montaje y hojas
# -------------------------------------------------------------------------------------------------
def notch_x(t): return WX0 + round(WW * t)

def mock(ratio, kind='violet', rage=False, trail=None, broken=False, k=0, fx=None, spines=None):
    im = Image.new('RGBA', (W + 40, H + 30), (0, 0, 0, 0))
    ox, oy = 20, 14
    im.alpha_composite(back(), (ox + WX0, oy + WY0))
    if trail:
        tr = fill('trail').crop((0, 0, max(1, int(WW * trail)), WH)); im.alpha_composite(tr, (ox + WX0, oy + WY0))
    f = fill(kind, k).crop((0, 0, max(1, int(WW * ratio)), WH)); im.alpha_composite(f, (ox + WX0, oy + WY0))
    im.alpha_composite(frame(rage, spines=spines), (ox, oy))
    nx = notch_x(NOTCH)
    im.alpha_composite(notch(broken), (ox + nx - 3, oy + 7))
    if fx: fx(im, ox, oy)
    return im

def hojas():
    sys.path.insert(0, HERE)
    os.makedirs(OUT, exist_ok=True)
    def save(img, name): img.save(os.path.join(OUT, PRE + name + '.png'))
    def strip(frames, name):
        w, h = frames[0].size; s = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
        for i, f in enumerate(frames): s.paste(f, (i * w, 0))
        save(s, name)
    save(frame(False), 'Frame'); save(frame(True), 'Frame_Rage')
    save(back(), 'Back')
    save(fill('violet'), 'Fill_Violet'); save(fill('silk'), 'Fill_Silk'); save(fill('trail'), 'Fill_Trail')
    strip([fill('rage', k) for k in range(3)], 'Fill_Rage')
    save(eyes(False), 'Eyes_Calm'); save(eyes(True), 'Eyes_Rage')
    save(notch(), 'Notch'); save(notch(True), 'Notch_Broken')
    strip([fx_notch(k) for k in range(5)], 'Fx_NotchBreak')
    strip([fx_screech(k) for k in range(6)], 'Fx_Screech')
    strip([fx_spines(k) for k in range(4)], 'Fx_Spines')
    # vista previa por estados
    rows = {}
    rows['100-50 %, en el suelo (vulnerable)'] = [mock(r, trail=r + .06) for r in (1.0, .86, .72, .58)]
    rows['en su cable (invulnerable: seda gris)'] = [mock(r, kind='silk') for r in (.9, .75, .6, .55)]
    def fx50(k):
        def f(im, ox, oy):
            im.alpha_composite(fx_screech(k), (ox + 22 - 36, oy + 14 - 20))
            if k < 5: im.alpha_composite(fx_notch(k), (ox + notch_x(NOTCH) - 8, oy - 3))
        return f
    rows['50 %: chillido, el nudo se rompe, brotan las espinas'] = [mock(.5, kind='rage' if k > 1 else 'violet', rage=k > 2, broken=True,
                                                                        k=k, fx=fx50(k), spines=min(1, k / 4)) for k in range(6)]
    rows['50-0 %: enfado (rojo que late)'] = [mock(r, kind='rage', rage=True, broken=True, k=i, trail=r + .05) for i, r in enumerate((.45, .3, .18, .06))]
    MG.preview(os.path.join(HERE, 'vista_previa_6k_barra.png'), rows, C('#0c0a10'), scale=3)
    # GIF del combate entero (escala 3)
    seq = [mock(1 - i * .04, trail=1 - i * .04 + .05) for i in range(6)] + [mock(.76 - i * .02, kind='silk') for i in range(4)] + \
          [mock(.62 - i * .02, trail=.66 - i * .02) for i in range(6)] + \
          [mock(.5, kind='rage' if k > 1 else 'violet', rage=k > 2, broken=True, k=k, fx=fx50(k), spines=min(1, k / 4)) for k in range(6)] + \
          [mock(.48 - i * .045, kind='rage', rage=True, broken=True, k=i, trail=.52 - i * .045) for i in range(10)]
    MG.gif(os.path.join(HERE, 'gif', 'Megarachne_BossBar_combate.gif'), seq, 160, C('#0c0a10'), scale=3)
    # maqueta en pantalla (480×270, arriba)
    scr = MG.smoke_floor(480, 270, seed=9)
    scr.alpha_composite(mock(.64, trail=.7), ((480 - W - 40) // 2, 2))
    scr.resize((960, 540), Image.NEAREST).convert('RGB').save(os.path.join(HERE, 'vista_previa_6k_pantalla.png'))
    print('Listo 6k')

if __name__ == '__main__':
    hojas()
