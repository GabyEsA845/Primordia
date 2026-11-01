"""Barra de vida de la Meganeura (tanda 6g, sistema BossBarSkin como el Arthropleura). Skill primordia-sprites.

Dos versiones para elegir (el usuario decide al verlas):
- A · ALAS: cabeza y tórax a la izquierda; las dos alas de arriba y las dos de abajo, abiertas y translúcidas con
  venas, bordean la ventana de la vida.
- B · CUERPO: cabeza y tórax a la izquierda con las alas cortas echadas atrás; el ABDOMEN, por segmentos, a lo largo
  de toda la barra: la vida rellena el abdomen, que acaba en los cercos.
Las dos: relleno F1 verde azulado iridiscente / F2 rojo de furia / F3 azul eléctrico con chispas; relleno gris
azulado con nubes mientras vuela alta (invulnerable); ojos por fase; marco que se rasga (F2 poco, F3 mucho); muescas
al 65 % (racimo que revienta) y al 30 % (rayo sobre la barra); último 10 %: el código hace temblar el marco y salen
chispas. Va ABAJO de la pantalla (el jefe vive arriba).

ELEGIDA: A (alas). Uso: python generar_interfaz_meganeura.py hojas      (hojas finales en BossBar/, vista y GIF)
                       python generar_interfaz_meganeura.py comparar   (las dos versiones + escenas, para elegir)
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
MT = os.path.normpath(os.path.join(HERE, '..', '..'))
sys.path.insert(0, os.path.join(MT, 'floor3-environment'))
from base_piso3 import *                               # noqa: F401,F403
from PIL import Image, ImageDraw

OUT = HERE
BG = C('#0b1220')
W, H = 300, 32
CY = 16

BODY4 = ['#0f4a56', '#1a7282', '#34a0aa', '#8edcd2']        # verde azulado (= el sprite)
JOINT4 = ['#05161a', '#0a2228', '#103440', '#184a56']
STRIPE4 = ['#9a5418', '#d08a2c', '#f0b850', '#ffe4a0']
EYE4 = ['#0a0608', '#160c10', '#28141a', '#4a2028']
LEG4 = ['#3a4418', '#64702a', '#94a040', '#c8d07a']
MEMB = (170, 196, 214, 150); MEMB_HI = (214, 230, 240, 175); VEIN = (34, 42, 52, 230); CROSS = (70, 84, 98, 170)
STIG = (58, 40, 30, 255)
WINDOW_BACK = ['#06080c', '#0c1018', '#121824']
FILLS = {
    'Teal':   ['#0a3a44', '#1a7282', '#3ab4b8', '#a6f0e4'],
    'Rage':   ['#5a0e0e', '#a01c16', '#e83a2a', '#ff9a7a'],
    'Storm':  ['#0e2a66', '#2a6ad0', '#6ab8ff', '#d0f0ff'],
    'Cloud':  ['#232c38', '#36414e', '#4e5a68', '#76828e'],
    'Trail':  ['#b8b0ac', '#dcd4d0', '#f4ece6', '#fffaf4'],
}
NOTCHES = (0.65, 0.30)
VERSIONS = {'A': dict(win=(43, 284, 12, 20)), 'B': dict(win=(52, 278, 12, 20))}
EL = ['#0a1a3a', '#1a4a9a', '#4aa0f0', '#a8e0ff', '#ffffff']

def mirror(y): return 2 * CY - 1 - y

# =================================================================================================
# Piezas
# =================================================================================================
def head_thorax(cv, x0=0):
    """Cabeza de frente a la izquierda (ojos aparte) y tórax con rayas ámbar."""
    ellipse(cv, x0 + 30, CY, 10, 8.5, BODY4)                    # tórax
    for s in (-1, 1):
        for k in range(6):                                       # rayas ámbar oblicuas
            cv.put(x0 + 27 + k, CY + s * (2 + k * .6), C(STRIPE4[1 + (k < 3)]))
    for x in range(x0 + 22, x0 + 40): cv.put(x, CY, C(JOINT4[1]))   # quilla
    ellipse(cv, x0 + 17, CY, 5, 6.5, BODY4)                      # cuello / cara
    ellipse(cv, x0 + 8.5, CY, 4.5, 5.5, ['#7a4a1e', '#b07a32', '#d8a850', '#f4d890'])   # cara ámbar

def legs(cv, x0=0):
    for s in (-1, 1):
        for k, (dx, L) in enumerate(((22, 6), (28, 7), (34, 6))):
            for j in range(L):
                c = C(LEG4[1] if j % 3 else JOINT4[1])
                cv.put(x0 + dx - j * .5, CY + s * (7 + j), c)

def eyes(state, k=0, x0=0):
    cv = Canvas(W, H)
    ramp = {'Hunt': EYE4, 'Rage': ['#3a0606', '#8a1410', '#e0302a', '#ff9a7a'],
            'Storm': ['#0a1a3a', '#1a4a9a', '#4aa0f0', '#c0ecff'] if k == 0 else ['#06122a', '#0a2a5a', '#2a6ad0', '#6ab8ff']}[state]
    for s in (-1, 1):
        ellipse(cv, x0 + 11, CY + s * 4.6, 5.2, 4.4, ramp)
        hi = C('#e8c8bc') if state == 'Hunt' else C('#fff0e0') if state == 'Rage' else C('#ffffff')
        cv.put(x0 + 9, CY + s * 4.6 - 2, hi); cv.put(x0 + 10, CY + s * 4.6 - 2.5, hi)
        if state == 'Hunt': cv.put(x0 + 11, CY + s * 4.6 - 1.4, C('#c0302a'))
        if state != 'Hunt' and (state == 'Rage' or k == 0):     # halo
            g = C('#ff5a3a', 130) if state == 'Rage' else C('#6ab8ff', 140)
            for a in range(14):
                t = a / 14 * 2 * math.pi
                cv.put_out(x0 + 11 + math.cos(t) * 6.4, CY + s * 4.6 + math.sin(t) * 5.4, g)
    cv.outline()
    return cv

def tears(level, side):
    """Rasgones del ala del marco (x de la muesca, profundidad): pocos en la F2, muchos en la F3."""
    T = {1: {1: [(170, 3)], -1: [(230, 3)]},
         2: {1: [(120, 3), (170, 4), (250, 4)], -1: [(96, 3), (200, 4), (262, 3)]}}
    return T.get(level, {}).get(side, [])

def wing(cv, x_root, x_tip, y_lead, y_trail, side, level=0, short=False, sweep=0.0):
    """Ala translúcida con venas que va de x_root a x_tip; y_lead = borde de ataque (fuera), y_trail = borde de
    salida (junto a la ventana). side = -1 arriba, 1 abajo (se refleja)."""
    L = x_tip - x_root
    cuts = tears(level, side)
    for x in range(int(x_root), int(x_tip) + 1):
        t = (x - x_root) / L
        tipk = math.sqrt(max(0, 1 - ((t - .86) / .14) ** 2)) if t > .86 else 1
        rootk = min(1, .4 + t * 6)
        y0 = y_lead + sweep * t; y1 = y_trail
        h = (y1 - y0) * tipk * rootk
        ya = y1 - h
        cut = 0
        for (cx, d) in cuts:
            if abs(x - cx) < 3: cut = max(cut, d * (1 - abs(x - cx) / 3) + (1 if abs(x - cx) < 1 else 0))
        for yy in range(int(round(ya)), int(round(y1)) + 1):
            dist_in = y1 - yy                                   # desde el borde de salida
            if dist_in < cut: continue
            if yy == int(round(ya)) or x == int(x_tip) or (cut and dist_in < cut + 1): c = VEIN
            elif (x - int(x_root)) % 5 == 0 or (yy - int(ya)) % 3 == 0 and (x % 2 == 0): c = CROSS
            elif .4 < t < .55 and yy - ya < 2: c = MEMB_HI
            else: c = MEMB
            if .84 < t < .9 and yy - ya < 2.2: c = STIG
            Y = yy if side < 0 else mirror(yy)
            cv.put(x, Y, c)

def window_hole(cv, win):
    WX0, WX1, WY0, WY1 = win
    for x in range(WX0, WX1):
        for y in range(WY0, WY1): cv.px.pop((x, y), None)
    for x in range(WX0 - 1, WX1 + 1): cv.put(x, WY0 - 1, OUTL); cv.put(x, WY1, OUTL)
    for y in range(WY0, WY1): cv.put(WX0 - 1, y, OUTL); cv.put(WX1, y, OUTL)

def abdomen(cv, win):
    """Versión B: el abdomen por segmentos envuelve la ventana y acaba en los cercos."""
    WX0, WX1, WY0, WY1 = win
    seg = 22
    for x in range(40, WX1 + 6):
        t = (x - 40) / (WX1 + 6 - 40)
        r = 7.5 - 1.5 * t
        s = (x - 40) % seg
        for y in range(int(CY - r), int(CY + r) + 1):
            dy = (y + .5 - CY) / r
            i = 3 if dy < -.6 else 2 if dy < -.1 else 1 if dy < .5 else 0
            col = JOINT4[min(3, i)] if s >= seg - 3 else BODY4[i]
            if s == 1 and abs(dy) > .5: col = STRIPE4[1]          # manchita ámbar en cada segmento
            cv.put(x, y, C(col))
    for s_ in (-1, 1):                                            # cercos
        for j in range(9): cv.put(WX1 + 6 + j, CY + s_ * (1 + j * .35), C(JOINT4[2] if j < 6 else BODY4[1]))

def frame(version, level=0):
    cv = Canvas(W, H); win = VERSIONS[version]['win']
    if version == 'A':
        for side in (-1, 1):
            wing(cv, 34, 296, 1.5, 10.5, side, level, sweep=1.5)            # ala anterior (fuera)
            wing(cv, 36, 282, 4.5, 10.5, side, level, sweep=.5)             # ala posterior, debajo
        head_thorax(cv)
    else:
        abdomen(cv, win)
        for side in (-1, 1):
            wing(cv, 30, 132, 0.5, 8, side, level, sweep=2.5)
            wing(cv, 32, 116, 3, 8.5, side, level, sweep=1)
        head_thorax(cv)
    cv.outline(); cv.clean()
    legs(cv)
    window_hole(cv, win)
    return cv

def fill(version, kind, k=0):
    WX0, WX1, WY0, WY1 = VERSIONS[version]['win']; w, h = WX1 - WX0, WY1 - WY0
    cv = Canvas(w, h); ramp = FILLS[kind]; rnd = random.Random(k * 31 + len(kind))
    seg = 22 if version == 'B' else 11
    for x in range(w):
        s = (x + (WX0 - 40 if version == 'B' else 0)) % seg
        for y in range(h):
            i = 3 if y == 0 else 2 if y < 3 else 1 if y < 6 else 0
            if s >= seg - 2: i = max(0, i - 1)
            if kind == 'Teal' and abs(((x - k * 24) % 90) - (y * 2)) < 3: i = 3          # brillo iridiscente que corre
            cv.put(x, y, C(ramp[i]))
        if kind == 'Storm' and rnd.random() < .16: cv.put(x, rnd.randint(1, 6), C(EL[4]))
        if kind == 'Cloud' and math.sin(x * .12 + k * 1.3) > .3: cv.put(x, 2 + int(2 * math.sin(x * .3 + k)), C('#9aa6b2'))
    if kind == 'Storm':                                           # chispa que recorre el relleno
        x0 = (k * 37) % w
        for j in range(8): cv.put(x0 + j, 3 + (j % 3 - 1), C(EL[4] if j % 2 else EL[3]))
    return cv

def back(version):
    WX0, WX1, WY0, WY1 = VERSIONS[version]['win']; w, h = WX1 - WX0, WY1 - WY0
    cv = Canvas(w, h)
    for x in range(w):
        for y in range(h): cv.put(x, y, C(WINDOW_BACK[0 if y < 2 else 1 if y < 6 else 2]))
    return cv

def notch(version, broken=False):
    """A: vena del ala que cruza la barra con un nudo ámbar; B: anillo oscuro del abdomen con puntos ámbar."""
    cv = Canvas(8, H)
    for y in range(2, H - 2):
        e = min(y - 2, H - 3 - y)
        if broken and 9 <= y <= 22: continue
        half = 1.2 if version == 'A' else min(2.6, .8 + e * .4)
        for x in range(8):
            if abs(x + .5 - 4) <= half:
                col = JOINT4[2] if x < 4 else JOINT4[1]
                if e < 3: col = STRIPE4[2] if x < 4 else STRIPE4[1]
                cv.put(x, y, C(col))
    cv.outline()
    return cv

def notch_x(version, t):
    WX0, WX1 = VERSIONS[version]['win'][:2]; return WX0 + int((WX1 - WX0) * t)

# =================================================================================================
# Efectos de la barra
# =================================================================================================
JELLY = [(96, 132, 52, 230), (150, 190, 90, 200), (196, 226, 130, 200), (250, 255, 220, 240)]

def fx_eggs(version, k, n=6):
    """65 %: un racimo de huevos revienta sobre la muesca (gelatina que salpica)."""
    cv = Canvas(W, H); x0 = notch_x(version, .65); rnd = random.Random(3); t = k / (n - 1)
    if k < 2:
        for j, (dx, dy) in enumerate(((-3, -2), (0, -4), (3, -2), (-2, 2), (2, 2))):
            ellipse(cv, x0 + dx, CY + dy, 2.4 + k * .6, 2.4 + k * .6, [JELLY[0][:3] + (255,), JELLY[1], JELLY[2], JELLY[3]])
    else:
        for j in range(18):
            an = rnd.uniform(0, 2 * math.pi); sp = rnd.uniform(4, 16) * t
            cv.put(x0 + math.cos(an) * sp * 1.4, CY + math.sin(an) * sp * .8 + 4 * t * t, JELLY[2 if j % 2 else 1])
    return cv

def fx_bolt(version, k, n=5):
    """30 %: un rayo cae sobre la muesca y recorre la barra."""
    cv = Canvas(W, H); x0 = notch_x(version, .30); rnd = random.Random(7 + k)
    if k < 3:
        x, y = x0 + rnd.randint(-3, 3), 0
        while y < CY:
            nx, ny = x + rnd.randint(-2, 2), y + rnd.randint(2, 4)
            for q in range(4):
                xx = x + (nx - x) * q / 4; yy = y + (ny - y) * q / 4
                cv.put(xx, yy, C(EL[4])); cv.put(xx + 1, yy, C(EL[3]))
            x, y = nx, ny
    WX0, WX1, WY0, WY1 = VERSIONS[version]['win']
    reach = (WX1 - WX0) * min(1, k / (n - 2))
    for j in range(int(reach) // 6):                             # chispas que recorren la ventana
        xx = WX0 + j * 6 + rnd.randint(0, 3)
        if xx < WX1: cv.put(xx, rnd.randint(WY0, WY1 - 1), C(EL[4] if j % 2 else EL[3]))
    return cv

def fx_low(version, k):
    """Último 10 %: chispas que saltan del marco (el código además lo hace temblar ±1 px)."""
    cv = Canvas(W, H); rnd = random.Random(k * 11 + 5)
    for j in range(7):
        x = rnd.randint(30, W - 10); y = rnd.choice((rnd.randint(0, 6), rnd.randint(25, 31)))
        sparks(cv, x, y, 1 + (j % 2))
        cv.put(x, y, C(EL[4]))
    return cv

# =================================================================================================
# Montaje y comparación
# =================================================================================================
def mock(version, phase, ratio, trail=None, level=0, k=0, cloud=False, fx=None):
    WX0, WX1, WY0, WY1 = VERSIONS[version]['win']; ww, wh = WX1 - WX0, WY1 - WY0
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    img.alpha_composite(back(version).img(), (WX0, WY0))
    if trail: img.alpha_composite(fill(version, 'Trail').img().crop((0, 0, max(1, int(ww * trail)), wh)), (WX0, WY0))
    kind = 'Cloud' if cloud else {'F1': 'Teal', 'F2': 'Rage', 'F3': 'Storm'}[phase]
    img.alpha_composite(fill(version, kind, k).img().crop((0, 0, max(1, int(ww * ratio)), wh)), (WX0, WY0))
    img.alpha_composite(frame(version, level).img(), (0, 0))
    for t in NOTCHES:
        img.alpha_composite(notch(version, broken=ratio < t).img(), (notch_x(version, t) - 4, 0))
    img.alpha_composite(eyes({'F1': 'Hunt', 'F2': 'Rage', 'F3': 'Storm'}[phase], k).img(), (0, 0))
    if fx: img.alpha_composite(fx.img(), (0, 0))
    return img

STATES = [('F1 · 100-65 % (verde azulado)', dict(phase='F1', ratio=.86, trail=.92)),
          ('F1 · vuela alta: invulnerable (nubes)', dict(phase='F1', ratio=.86, cloud=True)),
          ('65 %: racimo que revienta', dict(phase='F2', ratio=.64, level=1, fx='eggs')),
          ('F2 · 65-30 % (furia, alas rasgadas)', dict(phase='F2', ratio=.48, trail=.55, level=1)),
          ('30 %: rayo sobre la barra', dict(phase='F3', ratio=.29, level=2, fx='bolt')),
          ('F3 · 30-0 % (tormenta, muy rasgada)', dict(phase='F3', ratio=.2, level=2)),
          ('Último 10 %: tiembla con chispas', dict(phase='F3', ratio=.07, level=2, fx='low'))]

def state_img(v, st, k=2):
    st = dict(st); fxk = st.pop('fx', None)
    fx = {'eggs': fx_eggs(v, 3), 'bolt': fx_bolt(v, 1), 'low': fx_low(v, k)}.get(fxk)
    return mock(v, k=k, fx=fx, **st)

def comparar():
    from generar_escala_meganeura import A as AE                 # fuente
    F = AE.font
    s = 3; pad = 14
    cols = []
    for v, title in (('A', 'A · ALAS (las alas abiertas forman el marco)'), ('B', 'B · CUERPO (el abdomen es la barra)')):
        rows = [state_img(v, st) for _, st in STATES]
        col = Image.new('RGBA', (W * s + pad * 2, 40 + len(rows) * (H * s + 34)), BG + (255,) if len(BG) == 3 else BG)
        d = ImageDraw.Draw(col); d.text((pad, 8), title, fill=(255, 240, 200), font=F(20))
        for j, (im, (lab, _)) in enumerate(zip(rows, STATES)):
            y = 40 + j * (H * s + 34)
            d.text((pad, y), lab, fill=(190, 205, 215), font=F(15))
            col.alpha_composite(im.resize((W * s, H * s), Image.NEAREST), (pad, y + 20))
        cols.append(col)
    full = Image.new('RGBA', (sum(c.width for c in cols) + 20, max(c.height for c in cols)), (8, 12, 20, 255))
    x = 0
    for c in cols: full.alpha_composite(c, (x, 0)); x += c.width + 20
    full.convert('RGB').save(os.path.join(OUT, 'vista_previa_6g_comparar.png'))
    escenas()
    print('Listo:', OUT)

# =================================================================================================
# Escenas: la barra ABAJO de la pantalla del juego (480×270), con la cámara ×1,3 la sala se ve igual: la barra es UI
# =================================================================================================
def escenas():
    sys.argv = sys.argv[:1]
    import generar_meganeura as M
    CAMW, CAMH = 480, 270
    def screen(kind, v, k):
        im = M.burnt_floor(CAMW, CAMH, 50 + k)
        gl = Image.new('RGBA', (CAMW, CAMH))
        if kind == 'alta':
            M.place(im, None, 300, 70, 0, M.SF('Meganeura_Shadow_Fly', k, -20, folder='Shadows'), 80, dither=True)
            M.put_c(im, M.fxw('Fx_Meganeura_DiveMark_Follow', k, 64), 200, 170)
            im.alpha_composite(M.frog(), (184, 150))
            bar = state_img(v, STATES[1][1], k)
        elif kind == 'baja':
            M.boss(im, 'Hover', k, 200, 290, 110, 16)
            im.alpha_composite(M.frog(), (150, 170))
            bar = state_img(v, STATES[0][1], k)
        elif kind == 'furia':
            pool = M.pool_img(); im.alpha_composite(pool, (240 - pool.width // 2, 70))
            M.put_c(im, M.fxw('Meganeura_Eggs_Break', 5, 40, 'Eggs'), 160, 120)
            M.place_n(im, 'Move', k, -30, 120, 180)
            im.alpha_composite(M.frog(), (330, 190))
            M.boss(im, 'PerchRage', k, 180, 250, 100, 8, 'Torn1')
            bar = state_img(v, STATES[3][1], k)
        else:
            M.boss(im, 'Hover', k, 200, 300, 100, 16, 'Torn2')
            M.put_c(gl, M.fxw('Fx_Bolt_Big_Mark', 5, 96), 170, 186)
            im.alpha_composite(M.frog(), (154, 162))
            M.overlay(im, M.DARK_C, .45); M.rain_on(im, k)
            bar = state_img(v, STATES[5][1], k)
        im.alpha_composite(gl)
        im.alpha_composite(bar, ((CAMW - W) // 2, CAMH - H - 6))   # ABAJO, centrada
        return im
    for v in ('A', 'B'):
        tiles = [screen(kind, v, 1) for kind in ('alta', 'baja', 'furia', 'tormenta')]
        s = 2
        sheet = Image.new('RGBA', (CAMW * s * 2 + 8, CAMH * s * 2 + 8), (8, 12, 20, 255))
        for j, t in enumerate(tiles):
            sheet.alpha_composite(t.resize((CAMW * s, CAMH * s), Image.NEAREST), ((j % 2) * (CAMW * s + 8), (j // 2) * (CAMH * s + 8)))
        sheet.convert('RGB').save(os.path.join(OUT, 'vista_previa_6g_escenas.png' if v == 'A' else f'vista_previa_6g_escenas_{v}.png'))

# =================================================================================================
# Hojas finales (versión A, elegida por el usuario)
# =================================================================================================
def hojas(v='A'):
    out = os.path.join(OUT, 'BossBar'); os.makedirs(out, exist_ok=True)
    pre = 'UI_BossBar_Meganeura_'
    sheets = {
        'Frame': [frame(v, 0)], 'Frame_Torn1': [frame(v, 1)], 'Frame_Torn2': [frame(v, 2)],
        'Back': [back(v)],
        'Fill_Teal': [fill(v, 'Teal', k) for k in range(4)], 'Fill_Rage': [fill(v, 'Rage', 0)],
        'Fill_Storm': [fill(v, 'Storm', k) for k in range(4)], 'Fill_Cloud': [fill(v, 'Cloud', k) for k in range(4)],
        'Fill_Trail': [fill(v, 'Trail')],
        'Eyes_Hunt': [eyes('Hunt')], 'Eyes_Rage': [eyes('Rage')], 'Eyes_Storm': [eyes('Storm', k) for k in range(2)],
        'Notch': [notch(v)], 'Notch_Broken': [notch(v, True)],
        'Fx_EggBurst': [fx_eggs(v, k) for k in range(6)], 'Fx_BoltStrike': [fx_bolt(v, k) for k in range(5)],
        'Fx_LowSparks': [fx_low(v, k) for k in range(4)],
    }
    for name, fr in sheets.items(): save_sheet(os.path.join(out, pre + name + '.png'), fr)
    # vista y GIF de cada estado
    rows = {lab: [state_img(v, st, k) for k in range(4)] for lab, st in STATES}
    preview(os.path.join(OUT, 'vista_previa_6g_barra.png'), rows, BG, scale=3)
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    seq = []                                                     # la barra a lo largo del combate (para revisar)
    for j, r in enumerate([1, .9, .8, .7, .66]): seq.append(mock(v, 'F1', r, trail=min(1, r + .06), k=j))
    for k in range(6): seq.append(mock(v, 'F2', .64, level=1, k=k, fx=fx_eggs(v, k)))
    for j, r in enumerate([.6, .5, .4, .32]): seq.append(mock(v, 'F2', r, trail=r + .05, level=1, k=j))
    for k in range(5): seq.append(mock(v, 'F3', .29, level=2, k=k, fx=fx_bolt(v, k)))
    for j, r in enumerate([.25, .18, .12, .09, .07, .05, .03]):
        seq.append(mock(v, 'F3', r, trail=r + .04, level=2, k=j, fx=fx_low(v, j) if r < .1 else None))
    gif(os.path.join(OUT, 'gif', 'Meganeura_BossBar_combate.gif'), seq, 160, BG, scale=3)
    print('Listo:', out)

if __name__ == '__main__':
    if 'comparar' in sys.argv[1:]: comparar()
    elif 'hojas' in sys.argv[1:]: hojas('A')
    else: print(__doc__)
