"""Letrero con el nombre de las salas con tema del piso 3 (paso 6, bloque 1; ver README).

Al entrar por primera vez en una sala con tema, baja desde arriba un tronco horizontal de Lepidodendron (corteza de
cojinetes en rombo, puntas rotas, musgo) con el nombre encima, en Jersey 10 con el relleno ámbar en 4 bandas de los
rótulos de jefe del piso 3, y un icono de 16 px del tema sobre cada extremo del tronco. La animación (bajar con un
rebote, quedarse ~2,5 s y subir) la hace el código; aquí solo se dibujan las piezas fijas:
  UI_RoomSign_Log.png            el tronco
  UI_RoomSign_Title_<Tema>.png   el nombre ya rotulado
  UI_RoomSign_Icon_<Tema>.png    el icono (el mismo a los dos lados)
Hecho con la skill primordia-sprites (pixel.py): 16 px por unidad, luz arriba a la izquierda, contorno OUTL.

Uso: python generar_letreros_piso3.py [Tema ...]   (por defecto, todos los temas que ya tienen icono)
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
TEMP = os.path.normpath(os.path.join(HERE, '..'))
sys.path.insert(0, os.path.join(TEMP, 'floor3-environment'))
sys.path.insert(0, os.path.join(TEMP, 'boss-intros'))
from base_piso3 import *                  # importa también pixel.py (C, ramp, Canvas, OUTL...)
from PIL import Image, ImageDraw

BG = C('#1c2e20')
AMBER_RAMP = ['#fff4c0', '#ffd25a', '#e89a30', '#8a3a10']   # el de los rótulos de jefe del piso 3
OUT3 = (34, 12, 6)
TITLE_SIZE = 20                           # Jersey 10 es nítida en múltiplos de 10
MOSS = [C('#2a4228'), C('#3d5e36'), C('#557a3e'), C('#7fa04a')]

THEMES = {                                # clave de archivo: nombre visible (EncounterTheme.name)
    'BeetleNest': 'Nido de bombarderos',
    'CalamitesMeadow': 'Pradera de Calamites',
    'ToadPond': 'Charca de sapos',
    'SpiderLoom': 'Telar de la araña',
    'DragonflySky': 'Cielo de libélulas',
}

# =================================================================================================
# Tronco
# =================================================================================================
LOG_W, LOG_H = 248, 42                    # lienzo
X0, X1 = 9, 238                           # de punta a punta
TOP, BOT = 9, 32                          # cara de arriba y de abajo del tronco (23 px de grosor)

def log_edges(x, rnd_top, rnd_bot):
    """Borde de arriba y de abajo en la columna x: algo combado y con bultos suaves."""
    u = (x - X0) / (X1 - X0)
    bow = math.sin(u * math.pi) * 1.2
    return TOP - bow + rnd_top[x], BOT - bow * .4 + rnd_bot[x]

DISCS = (X0 + 20, X1 - 18)              # centro de los cortes de rama = centro de los iconos

def branch_scar(cv, cx, cy, rx=9.2, ry=9.6):
    """Corte de una rama visto de frente: collar de corteza abultado, madera clara con anillos y la luz arriba a la
    izquierda (el collar se oscurece abajo a la derecha)."""
    for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
        for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
            nx, ny = (x + .5 - cx) / rx, (y + .5 - cy) / ry
            d = math.hypot(nx, ny)
            if d > 1: continue
            lit = -nx * .6 - ny * .8                                # >0 hacia la luz
            if d > .8: c = BARK[3] if lit > .25 else BARK[2] if lit > -.3 else BARK[1]
            elif d > .72: c = OUTL if lit < -.2 else BARK[0]        # borde interior del collar
            else:
                ring = int(d * 7.5) % 2
                k = (3 if ring == 0 else 2) if lit > -.1 else (2 if ring == 0 else 1)
                c = C(BRUSH[k])
                if d < .1: c = C(BRUSH[1])
            cv.put(x, y, c)

def log():
    rnd = random.Random(7)
    cv = Canvas(LOG_W, LOG_H)
    # bultos suaves del borde (ruido de baja frecuencia)
    def smooth(seed, amp):
        r = random.Random(seed); pts = [r.uniform(-amp, amp) for _ in range(LOG_W // 12 + 3)]
        return [pts[x // 12] + (pts[x // 12 + 1] - pts[x // 12]) * (x % 12) / 12 for x in range(LOG_W)]
    rt, rb = smooth(3, .7), smooth(5, .5)
    # astillas de la punta derecha: dientes de largo distinto según la altura
    teeth = [0, 5, 2, 8, 3, 1, 6, 2, 4, 0, 3, 7, 1, 4, 2, 5, 1, 3, 0, 2, 4, 1, 3, 0]
    cw, ch = 7, 6                                                   # cojinete: 7 de largo y 6 de alto
    for x in range(X0, X1 + 9):
        top, bot = log_edges(min(x, X1), rt, rb)
        for y in range(int(top), int(bot) + 1):
            if y < top - .3: continue
            v = (y - top) / max(1, bot - top)                       # 0 arriba, 1 abajo
            # punta derecha astillada
            row = min(len(teeth) - 1, int(v * len(teeth)))
            if x > X1 + teeth[row] - 1: continue
            # punta izquierda: corte inclinado (la sección se dibuja después)
            if x < X0 + 2 + (1 - v) * 2: continue
            # cojinetes en rombo, filas desplazadas medio cojinete
            r_ = int((y - TOP + 2) // ch)
            lx = (x - X0 + (cw // 2 if r_ % 2 else 0)) % cw - (cw - 1) / 2
            ly = (y - TOP + 2) % ch - (ch - 1) / 2
            dd = abs(lx) / (cw / 2) + abs(ly) / (ch / 2)
            light = 4 if v < .16 else 3 if v < .4 else 2 if v < .72 else 1 if v < .9 else 0
            if dd > .95: c = BARK[max(0, light - 2)]                # surco entre cojinetes
            elif dd < .28 and v > .12: c = BARK[max(0, light - 2)]  # cicatriz de la hoja
            elif dd < .62 and ly < 0: c = BARK[min(4, light)]       # mitad de arriba del cojinete, con luz
            else: c = BARK[max(0, light - 1)]
            cv.put(x, y, c)
    # sección de la punta izquierda: corteza gruesa y madera clara con anillos
    cx, cy = X0 + 3, (TOP + BOT) / 2 - .3
    for y in range(TOP - 1, BOT + 2):
        for x in range(X0 - 2, X0 + 8):
            nx, ny = (x - cx) / 3.2, (y - cy) / 11.6
            d = math.hypot(nx, ny)
            if d > 1: continue
            if d > .78: c = BARK[1]
            elif d > .7: c = C(BRUSH[1])
            else:
                ring = int(d * 9) % 2
                c = C(BRUSH[3] if ny < -.15 else BRUSH[2]) if ring == 0 else C(BRUSH[2] if ny < .3 else BRUSH[1])
                if d < .14: c = C(BRUSH[1])                         # médula
            cv.put(x, y, c)
    # cortes de rama (madera clara con anillos y collar de corteza): aquí se apoya el icono de cada lado
    for bx in DISCS: branch_scar(cv, bx, (TOP + BOT) / 2 + .5)
    # musgo encima, en manchas
    for (a, b) in ((22, 58), (88, 104), (131, 170), (196, 214)):
        for x in range(a, b):
            top, _ = log_edges(x, rt, rb)
            u = (x - a) / (b - a)
            hgt = math.sin(u * math.pi) * 2.6 + (1 if (x * 7) % 5 == 0 else 0)
            for k in range(int(hgt) + 2):
                y = int(top) - k + 1
                tone = 3 if k >= hgt else 2 if (x + k) % 3 else 1
                if k == 0: tone = 1
                cv.put(x, y, MOSS[tone])
    cv.outline(); cv.clean()
    # musgo que cuelga por debajo (fino, sin contorno)
    for x, L in ((30, 4), (36, 6), (41, 3), (95, 5), (140, 4), (147, 7), (153, 3), (201, 5), (207, 3)):
        _, bot = log_edges(x, rt, rb)
        for k in range(L):
            cv.put_out(x + (1 if k > L * .6 and x % 2 else 0), int(bot) + 2 + k, MOSS[1] if k < L - 1 else MOSS[0])
    # frondas pequeñas que brotan de los extremos
    pal = [C(c) for c in FERN[1:]]
    frondlet(cv, 25, int(log_edges(25, rt, rb)[0]) - 1, -2.2, 9, pal, droop=.35, lw=1.6)
    frondlet(cv, 27, int(log_edges(27, rt, rb)[0]) - 1, -1.2, 7, pal, droop=.3, lw=1.4)
    frondlet(cv, 211, int(log_edges(211, rt, rb)[0]) - 1, -.9, 8, pal, droop=.35, lw=1.5)
    return cv

# =================================================================================================
# Iconos (16×16)
# =================================================================================================
def icon_beetle():
    """Escarabajo bombardero visto desde arriba, cabeza arriba: cabeza y pronoto naranjas, élitros azul marino con
    sutura y brillo, patas y antenas finas (se leen sobre la madera clara del corte de rama)."""
    cv = Canvas(16)
    NAVY = ramp('#2a3a6a', 5); ORNG = ramp('#e8702a', 5)
    ellipse(cv, 8, 10.2, 3.5, 4.0, NAVY)                            # élitros
    for y in range(4, 7):                                           # pronoto: trapecio redondeado
        hw = 2.1 + (y - 4) * .45
        for x in range(16):
            if abs(x + .5 - 8) <= hw: cv.put(x, y, ORNG[3] if y == 4 else ORNG[2] if x < 8 else ORNG[1])
    for x in (7, 8): cv.put(x, 2, ORNG[2]); cv.put(x, 3, ORNG[2] if x == 7 else ORNG[1])
    cv.put(6, 3, ORNG[1]); cv.put(9, 3, ORNG[0])                    # cabeza
    for y in range(7, 14): cv.put(8, y, NAVY[0])                    # sutura
    cv.put(9, 8, NAVY[1])
    for (x, y) in ((6, 8), (6, 9)): cv.put(x, y, NAVY[4])   # brillo
    cv.put(9, 9, NAVY[3])
    cv.outline(); cv.clean()
    legs = ((11, 5), (12, 4), (12, 9), (13, 9), (11, 13), (12, 14), (9, 1), (10, 0))
    for (x, y) in legs:                                             # lado derecho, reflejado (finas, sin contorno)
        c = OUTL
        cv.put_out(x, y, c); cv.put_out(15 - x, y, c)
    return cv

def icon_calamites():
    """Calamites: tallo verde articulado (3 nudos claros) con verticilos de hojas finas que suben, y el estróbilo
    pardo en la punta."""
    cv = Canvas(16)
    G = ramp('#6a9a3a', 5); T = ramp('#d8d090', 4)
    for y in range(4, 15):                                          # tallo de 3 px, luz a la izquierda
        node = y in (7, 11)
        cv.put(7, y, G[1] if node else G[3]); cv.put(8, y, G[0] if node else G[2]); cv.put(9, y, G[0] if node else G[1])
    ellipse(cv, 8, 2.6, 1.6, 2.2, T, shift=-1)                     # estróbilo
    cv.outline(); cv.clean()
    for ny, L in ((7, 4), (11, 5), (14, 4)):                         # verticilos: hojas finas en diagonal hacia arriba
        for k in range(1, L + 1):
            cv.put_out(9 + k, ny - (k + 1) // 2, G[1]); cv.put_out(6 - k, ny - (k + 1) // 2, G[1] if k < L else G[0])
    return cv

def icon_toad():
    """Sapo de frente, achaparrado: cuerpo pardo ancho, dos ojos saltones arriba, verrugas claras y las manos."""
    cv = Canvas(16)
    B = ramp('#7a5630', 5, light=.22); W = ramp('#c8b070', 4)               # algo más oscuro que el sapo: si no, se pierde en la madera
    ellipse(cv, 8, 9.8, 5.8, 3.8, B, shift=-1)                      # cuerpo, ancho y bajo
    for ex in (4.4, 11.6): ellipse(cv, ex, 5.8, 2.4, 2.2, B, shift=0)   # ojos saltones
    for (x, y) in ((6, 12), (10, 12), (7, 8), (9, 8), (12, 10), (3, 10)): cv.put(x, y, W[0])   # verrugas
    for x in range(4, 13): cv.put(x, 10 if 5 < x < 11 else 9, B[0])  # boca ancha
    cv.outline(); cv.clean()
    for ex in (3, 11):                                              # ojo de la familia (blanco roto con pupila)
        for (dx, dy) in ((0, 0), (1, 0), (0, 1), (1, 1)): cv.put(ex + dx, 5 + dy, EYE)
        cv.put(ex + (1 if ex > 8 else 0), 6, OUTL); cv.put(ex + (0 if ex > 8 else 1), 4, B[0])
    for (x, y) in ((4, 13), (5, 13), (11, 13), (12, 13)): cv.put(x, y, B[2])   # manos
    return cv

def icon_spider():
    """Araña de las frondas desde arriba: abdomen oliva con la marca roja, cefalotórax pequeño y 8 patas finas
    acodadas (en la madera clara se leen bien)."""
    cv = Canvas(16)
    O = ramp('#5a6a2a', 5, light=.25); R = ramp('#e04040', 4)
    ellipse(cv, 8, 10.4, 3.4, 3.8, O, shift=0)                      # abdomen
    ellipse(cv, 8, 5.4, 2.0, 1.8, O, shift=-1)                      # cefalotórax
    for (x, y) in ((8, 9), (8, 10), (8, 11), (8, 12)): cv.put(x, y, R[2] if y < 11 else R[1])
    cv.outline(); cv.clean()
    legs = (((10, 4), (11, 3), (12, 2), (13, 2)), ((10, 5), (11, 5), (12, 4), (13, 4)),
            ((10, 6), (11, 7), (12, 7), (13, 8)), ((10, 7), (11, 8), (12, 9), (12, 10), (13, 11)))
    for leg in legs:                                                # lado derecho, reflejado
        for (x, y) in leg: cv.put_out(x, y, OUTL); cv.put_out(16 - x, y, OUTL)
    return cv

def icon_dragonfly():
    """Libélula desde arriba: cuerpo turquesa fino y largo, cabeza con ojos grandes y 4 alas claras casi
    transparentes con la nervadura marcada."""
    cv = Canvas(16)
    T = ramp('#2aa8a8', 5); Wg = ramp('#c8ecf0', 4)
    wings = ((-1, 4.6, .32), (1, 4.6, -.32), (-1, 6.8, -.18), (1, 6.8, .18))
    for sx, wy, ang in wings:                                       # alas (debajo del cuerpo)
        rot_ellipse(cv, 8 + sx * 3.6, wy, 3.4, 1.1, ang, Wg, alpha=200)
    cv.outline(); cv.clean()
    for y in range(4, 14): cv.put(8, y, T[2] if y % 2 else T[1])    # abdomen segmentado
    cv.put(7, 4, T[3]); cv.put(9, 4, T[1]); cv.put(7, 5, T[2]); cv.put(9, 5, T[1])   # tórax
    for (x, y) in ((7, 2), (9, 2), (7, 3), (9, 3)): cv.put(x, y, T[3] if x < 8 else T[2])   # ojos
    cv.put(8, 2, T[0]); cv.put(8, 3, T[1]); cv.put(8, 14, T[0])
    for x in (6, 10): cv.put(x, 2, OUTL); cv.put(x, 3, OUTL)
    return cv

ICONS = {'BeetleNest': icon_beetle, 'CalamitesMeadow': icon_calamites, 'ToadPond': icon_toad,
         'SpiderLoom': icon_spider, 'DragonflySky': icon_dragonfly}

# =================================================================================================
# Nombre y letrero montado
# =================================================================================================
def title(name):
    import generar_presentaciones as GP
    return GP.title_text(name.upper(), GP.TITLE_FONT, TITLE_SIZE, AMBER_RAMP, out=OUT3)

def assemble(key):
    """Letrero completo tal como lo montará el código: tronco, nombre centrado y apoyado en el tronco, iconos sobre
    los extremos (a 6 px de las puntas)."""
    lg = log().img(); tt = title(THEMES[key]); ic = ICONS[key]().img()
    W = LOG_W; H = LOG_H
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    im.alpha_composite(lg, (0, 0))
    im.alpha_composite(tt, ((W - tt.width) // 2, (TOP + BOT) // 2 - 21 // 2 - 2 - (tt.height - 21)))   # alineado por la línea base (las tildes suben)
    for bx in DISCS: im.alpha_composite(ic, (bx - 8, round((TOP + BOT) / 2 + .5) - 8))
    return im

def screen_bg():
    """Pantalla de 480×270 sobre la sala de muestra del pantano, con unos bombarderos y la rana."""
    bg = Image.new('RGBA', (480, 270), (14, 22, 16, 255))
    room = Image.open(os.path.join(TEMP, 'floor3-environment', 'Swamp', 'Sala_muestra.png')).convert('RGBA')
    bg.alpha_composite(room, (32, 7))
    bee = Image.open(os.path.join(TEMP, 'floor3-enemies', 'Beetle', 'Beetle_Idle.png')).convert('RGBA').crop((0, 0, 32, 32))
    for (x, y, fl) in ((300, 120, True), (340, 175, True), (190, 205, False)):
        bg.alpha_composite(bee.transpose(Image.FLIP_LEFT_RIGHT) if fl else bee, (x, y))
    frog = os.path.join(TEMP, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')
    if os.path.exists(frog):
        fr = Image.open(frog).convert('RGBA'); s = fr.height
        bg.alpha_composite(fr.crop((0, 0, s, s)), (120, 160))
    return bg

def ease_drop(t):
    """Baja en 0,35 s con un rebote pequeño (sobrepasa 5 px y vuelve)."""
    if t < .25: return -1 + (t / .25) ** .6 * 1.07
    if t < .35: return .07 - (t - .25) / .1 * .07
    return 0

def mockups(key, sign):
    bg = screen_bg()
    X, Y = (480 - sign.width) // 2, 18
    st = bg.copy(); st.alpha_composite(sign, (X, Y))
    st.resize((960, 540), Image.NEAREST).save(os.path.join(HERE, f'maqueta_letrero_{key}.png'))
    frames, ms = [], 50
    total = 4.0
    for i in range(int(total * 1000 / ms)):
        t = i * ms / 1000
        if t < .5: k = 1.0                                          # antes de entrar
        elif t < 1.0: k = -ease_drop(t - .5)                        # baja
        elif t < 3.3: k = 0
        else: k = min(1, ((t - 3.3) / .35) ** 2)                    # sube acelerando
        f = bg.copy()
        f.alpha_composite(sign, (X, Y - round(k * (sign.height + Y + 2))))
        frames.append(f.resize((960, 540), Image.NEAREST))
    os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True)
    frames[0].save(os.path.join(HERE, 'gif', f'escena_letrero_{key}.gif'), save_all=True, append_images=frames[1:],
                   duration=ms, loop=0, disposal=2)

if __name__ == '__main__':
    keys = sys.argv[1:] or list(ICONS)
    save_sheet(os.path.join(HERE, 'UI_RoomSign_Log.png'), [log()])
    rows = {'tronco': [log()]}
    for key in keys:
        t = title(THEMES[key]); t.save(os.path.join(HERE, f'UI_RoomSign_Title_{key}.png'))
        save_sheet(os.path.join(HERE, f'UI_RoomSign_Icon_{key}.png'), [ICONS[key]()])
        sign = assemble(key)
        rows[key] = [sign]
        rows[key + ' icono'] = [ICONS[key]()]
        mockups(key, sign)
        print('  ', key, 'nombre', t.size)
    sheet = Image.new('RGBA', (LOG_W * 4, sum((r[0].height if isinstance(r[0], Image.Image) else r[0].h) * 4 + 8 for r in rows.values())), BG)
    y = 0
    for r in rows.values():
        im = r[0] if isinstance(r[0], Image.Image) else r[0].img()
        sheet.alpha_composite(im.resize((im.width * 4, im.height * 4), Image.NEAREST), (0, y)); y += im.height * 4 + 8
    sheet.save(os.path.join(HERE, 'vista_previa_letreros.png'))
    print('Listo en', HERE)
