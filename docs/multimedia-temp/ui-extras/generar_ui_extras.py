"""Arte pendiente de las ramas room-hearts, teleports y floor12-fixes (común a todos los pisos).

- Medio corazón rojo que queda en el suelo al limpiar una sala: el medio corazón del HUD (mismos píxeles), sin la
  mitad vacía, flotando sobre una sombra aparte; brillo que lo cruza, recogida y vuelo a la vaina del piso 3.
- Corazón verde entero del HUD (dos medios de Pastar biomasa): el corazón lleno con la paleta de UI_Heart_Barrier.
- Iconos del mapa (7×7 con contorno, como UI_Map_Reward): medio corazón sin recoger, teletransporte (remolino
  aguamarina que gira) y regreso (flecha curva ámbar), con sus variantes apagada y resaltada.
- Corazón morado: resplandor ligero en el suelo bajo el jugador, motas que suben y la onda al parar un golpe.

Hecho con la skill primordia-sprites (pixel.py): 16 px por unidad, luz arriba a la izquierda, contorno OUTL.

Uso: python generar_ui_extras.py [carpeta_salida]   (por defecto, la carpeta de este script)
"""
import math, os, sys

def _skill_scripts():
    d = os.path.dirname(os.path.abspath(__file__))
    while True:
        p = os.path.join(d, '.claude', 'skills', 'primordia-sprites', 'scripts')
        if os.path.isdir(p): return p
        if os.path.dirname(d) == d: raise SystemExit('No encuentro .claude/skills/primordia-sprites/scripts')
        d = os.path.dirname(d)
sys.path.insert(0, _skill_scripts())
from pixel import *
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = sys.argv[1] if len(sys.argv) > 1 else HERE
UI = os.path.join(HERE, '..', 'ui', 'HUD')
BG = C('#16352e')                          # fondo de revisión de la interfaz (generar_ui.py)

HEART = [C('#7a1e2a'), C('#c8384a'), C('#f0706e'), C('#ffd0c0')]   # rampa del corazón del HUD
AQUA = [C('#155a5e'), C('#2aa3a0'), C('#6ee6d2'), C('#d4fff4')]    # luz de los teletransportes
AMBER = [C('#7a4a10'), C('#c98420'), C('#f7c04a'), C('#fff1c2')]   # regreso (oro de las recompensas)
PURPLE = [C('#3a1a5e'), C('#7a3ab8'), C('#b878f0'), C('#ecd4ff')]  # corazón morado
DIMC = [C('#1c332d'), C('#28463e'), C('#36594f'), C('#4a7062')]     # siluetas apagadas del mapa

# Del rojo del HUD al verde de UI_Heart_Barrier (lineages/generar_linajes.py).
GREEN_OF = {
    (110, 40, 56): (34, 90, 52),
    (200, 56, 74): (64, 170, 84),
    (240, 112, 110): (130, 220, 110),
    (255, 208, 192): (210, 255, 180),
    (255, 240, 224): (236, 255, 220),
}


def load(name):
    return Image.open(os.path.join(UI, name + '.png')).convert('RGBA')


def at(cv, x, y):
    return cv.px.get((x, y))


def to_canvas(im):
    cv = Canvas(im.width, im.height)
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            if px[x, y][3]:
                cv.put(x, y, px[x, y])
    return cv


# ============================================================ 1. MEDIO CORAZÓN DEL SUELO
def half_heart_base():
    """El medio corazón del HUD sin la mitad vacía: lo relleno (x < 8) y la costura del corte como contorno."""
    src = load('UI_Heart_Half')
    px = src.load()
    cv = Canvas(16)
    for y in range(16):
        for x in range(9):
            r, g, b, a = px[x, y]
            if not a:
                continue
            if x == 8:                                         # la costura pasa a ser el contorno del corte
                cv.put(x, y, OUTL)
            elif (r, g, b) != (110, 40, 56) and not ((r, g, b) == (24, 20, 37) and a < 255):
                cv.put(x, y, (r, g, b, 255))
    # contorno fuera de lo relleno (el del HUD ya está; esto cierra los huecos del corte)
    cv.outline()
    return cv


def shift(cv, dx):
    out = Canvas(cv.w, cv.h)
    for y in range(cv.h):
        for x in range(cv.w):
            c = at(cv, x, y)
            if c and c[3]:
                out.put(x + dx, y, c)
    return out


BASE = None


def room_heart(f, n=8):
    """Idle: el medio corazón centrado en 16×16 (desplazado 4 px a la derecha) con un brillo diagonal que lo cruza en
    los cuadros 4-6 y un latido de luz muy suave. El vaivén lo hace el código (sobre la sombra, que no se mueve)."""
    cv = shift(BASE, 4)
    band = {4: -2, 5: 2, 6: 6}.get(f)
    if band is not None:
        for y in range(16):
            for x in range(16):
                c = at(cv, x, y)
                if not c or c[3] == 0 or tuple(c[:3]) == OUTL[:3]:
                    continue
                d = (x - 4) + (y - 2) - band
                if d in (0, 1):
                    cv.put(x, y, HEART[3] if d == 0 else HEART[2])
    if f in (2, 3):                                            # latido: el brillo de arriba crece un poco
        cv.put(6, 4, HEART[3])
    return cv


def heart_shadow():
    """Sombra fija bajo el corazón (16×6): óvalo oscuro translúcido."""
    cv = Canvas(16, 6)
    for y in range(6):
        for x in range(16):
            nx, ny = (x + .5 - 8) / 4.5, (y + .5 - 3) / 1.6
            if nx * nx + ny * ny <= 1:
                cv.put(x, y, (24, 20, 37, 110 if nx * nx + ny * ny < .45 else 70))
    return cv


def heart_collect(f):
    """Recogida (6 cuadros, 16×16, 60 ms): se hincha, destella en blanco y estalla en chispas rojas y crema."""
    cv = Canvas(16)
    if f < 2:
        src = shift(BASE, 4)
        for y in range(16):
            for x in range(16):
                c = at(src, x, y)
                if c and c[3]:
                    if f == 1 and tuple(c[:3]) != OUTL[:3]:      # destello blanco antes de estallar
                        c = C(WHITE4[0])
                    cv.put(x, y, c)
        return cv
    r = 2 + (f - 2) * 1.6
    for k in range(8):
        a = k * math.pi / 4 + (f % 2) * math.pi / 8
        x, y = 8 + math.cos(a) * r, 8 + math.sin(a) * r
        col = HEART[2] if k % 2 else HEART[3]
        if f >= 4 and k % 2:
            col = HEART[1]
        cv.put(int(x), int(y), col)
        if f < 4:
            cv.put(int(x + math.cos(a) * -1), int(y + math.sin(a) * -1), HEART[1])
    if f == 2:
        for (x, y) in ((7, 7), (8, 7), (7, 8), (8, 8)):
            cv.put(x, y, SPARK[0])
    if f == 3:
        cv.put(8, 7, SPARK[1]); cv.put(7, 8, SPARK[1])
    return cv


def heart_fly(f):
    """Vuelo hacia la vaina (4 cuadros, 16×16, 70 ms): medio corazón pequeño (5 px) a la derecha con estela de chispas
    detrás (a la izquierda). El código lo gira hacia donde vuela."""
    cv = Canvas(16)
    small = ['.oo..',
             'ohro.',
             'orrro',
             '.orro',
             '..oro',
             '...oo']
    pal = {'o': OUTL, 'h': HEART[3], 'r': HEART[2]}
    for j, row in enumerate(small):
        for i, ch in enumerate(row):
            if ch in pal:
                cv.put(9 + i, 5 + j, pal[ch])
    cv.put(11, 7, HEART[1])
    trail = [(7, 7), (5, 8), (3, 7), (1, 8)]
    for k, (x, y) in enumerate(trail):
        if (k + f) % 4 == 3:
            continue
        cv.put(x + (f % 2), y, HEART[3] if k == 0 else HEART[2] if k < 2 else HEART[1])
    return cv


# ============================================================ 2. CORAZÓN VERDE ENTERO
def green_full():
    im = load('UI_Heart_Full')
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a and (r, g, b) in GREEN_OF:
                px[x, y] = GREEN_OF[(r, g, b)] + (a,)
    return im


# ============================================================ 3. ICONOS DEL MAPA (7×7, centro 3,3)
def grid(rows, pal, size=7):
    cv = Canvas(size)
    for j, row in enumerate(rows):
        for i, ch in enumerate(row):
            c = pal.get(ch)
            if c:
                cv.put(i, j, c)
    return cv


def map_heart():
    """Medio corazón, en el lenguaje del HUD: la mitad izquierda roja y la derecha vacía (oscura), con contorno."""
    rows = ['.......',
            '.oo.oo.',
            'ohroeeo',
            'orrree.',
            '.orree.',
            '..ore..',
            '...o...']
    cv = grid(rows, {'o': OUTL, 'h': HEART[3], 'r': HEART[2], 'e': C('#4a2234')})
    for (x, y) in ((6, 3), (5, 4), (4, 5)):                    # contorno de la mitad vacía
        cv.put(x, y, OUTL)
    cv.put(2, 4, HEART[1]); cv.put(3, 4, HEART[1]); cv.put(3, 5, HEART[1])   # sombra abajo
    return cv


def map_teleport(f, pal=AQUA, hover=False):
    if hover:                                                  # resaltado: todo un tono más claro
        pal = [pal[1], pal[2], pal[3], C('#ffffff')]
    """Remolino aguamarina que gira (4 cuadros): disco de 5×5 con un brazo en espiral y el centro claro."""
    cv = Canvas(7)
    for y in range(7):
        for x in range(7):
            dx, dy = x + .5 - 3.5, y + .5 - 3.5
            d = math.hypot(dx, dy)
            if d > 2.7:                                        # octógono: sin esquinas
                continue
            ang = math.atan2(dy, dx)
            v = math.sin(ang * 1 + d * 1.9 - f * math.pi / 2)
            idx = 2 if v > 0.35 else 1 if v > -0.45 else 0
            if d < 0.8:
                idx = 3
            cv.put(x, y, pal[idx])
    cv.outline()
    return cv


def map_return(pal=AMBER, hover=False):
    """Flecha curva ámbar de «volver» (9×9, centro 4,4, como UI_Map_Boss): punta a la izquierda, el trazo sigue a la
    derecha, baja por la curva y vuelve por abajo."""
    if hover:
        pal = [pal[1], pal[2], pal[3], C('#ffffff')]
    rows = ['..t....',
            '.ttttt.',
            'tTtttTt',
            '.tt..mm',
            '..t..mm',
            '....mm.',
            '.dddd..']
    cv = Canvas(9)
    for j, row in enumerate(rows):
        for i, ch in enumerate(row):
            c = {'t': pal[2], 'T': pal[3], 'm': pal[1], 'd': pal[1]}.get(ch)
            if c:
                cv.put(i + 1, j + 1, c)
    cv.put(3, 2, pal[3])
    cv.outline()
    return cv


# ============================================================ 4. CORAZÓN MORADO
def aura(f, n=8):
    """Resplandor ligero en el suelo bajo el jugador (32×16, 8 cuadros, 150 ms): óvalo morado muy translúcido en tres
    anillos que late despacio. Pivote: centro (el código lo pone a los pies del jugador, detrás de él)."""
    cv = Canvas(32, 16)
    k = 0.5 + 0.5 * math.sin(f / n * 2 * math.pi)
    rx, ry = 11.5 + k * 1.5, 4.6 + k * 0.6
    for y in range(16):
        for x in range(32):
            nx, ny = (x + .5 - 16) / rx, (y + .5 - 8) / ry
            d = nx * nx + ny * ny
            if d > 1:
                continue
            if d > 0.72:
                cv.put(x, y, PURPLE[2][:3] + (int(115 + 35 * k),))
            elif d > 0.4:
                cv.put(x, y, PURPLE[2][:3] + (int(60 + 20 * k),))
            else:
                cv.put(x, y, PURPLE[2][:3] + (38,))
    return cv


def mote(f, n=6):
    """Mota que sube (4×8, 6 cuadros, 110 ms): punto morado claro que asciende y se apaga. El código suelta 2-3 de vez
    en cuando en puntos al azar del resplandor."""
    cv = Canvas(4, 8)
    y = 6 - f
    a = 255 if f < 4 else 150 if f == 4 else 80
    cv.put(1, y, PURPLE[3][:3] + (a,))
    if f < 3:
        cv.put(1, y + 1, PURPLE[2][:3] + (a // 2,))
    if f == 1 or f == 2:
        cv.put(2, y, PURPLE[2][:3] + (a // 2,))
    return cv


def block_wave(f, n=8):
    """Onda al parar un golpe (48×48, 8 cuadros, 50 ms): anillo morado fino que se abre y se apaga, con un destello
    blanco en el centro al principio y 4 chispas en cruz. Pivote: centro del jugador."""
    cv = Canvas(48)
    t = f / (n - 1)
    r = 4 + t * 19
    width = 1.6 if f < 5 else 1.1
    for y in range(48):
        for x in range(48):
            d = math.hypot(x + .5 - 24, y + .5 - 24)
            if abs(d - r) <= width:
                inner = d < r
                col = PURPLE[3] if (f < 3 and not inner) else PURPLE[2] if not inner else PURPLE[1]
                a = int(255 * (1 - t * 0.75))
                cv.put(x, y, col[:3] + (a,))
    if f < 2:
        for (x, y) in ((23, 23), (24, 23), (23, 24), (24, 24)):
            cv.put(x, y, SPARK[0])
        for (x, y) in ((22, 23), (25, 24), (23, 22), (24, 25)):
            cv.put(x, y, PURPLE[3])
    if 1 <= f <= 5:
        s = r * 0.62
        for (dx, dy) in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            cv.put(int(24 + dx * s), int(24 + dy * s), PURPLE[3] if f < 4 else PURPLE[2])
    return cv


# ============================================================ SALIDA
def build():
    global BASE
    BASE = half_heart_base()
    A = {}
    A['Items/Item_RoomHeart_Idle'] = ([room_heart(f) for f in range(8)], 130)
    A['Items/Item_RoomHeart_Shadow'] = ([heart_shadow()], None)
    A['Items/Item_RoomHeart_Collect'] = ([heart_collect(f) for f in range(6)], 60)
    A['Items/Item_RoomHeart_Fly'] = ([heart_fly(f) for f in range(4)], 70)
    A['HUD/UI_Heart_BarrierFull'] = ([green_full()], None)
    A['Map/UI_Map_Heart'] = ([map_heart()], None)
    A['Map/UI_Map_Teleport'] = ([map_teleport(f) for f in range(4)], 120)
    A['Map/UI_Map_Teleport_Hover'] = ([map_teleport(f, hover=True) for f in range(4)], 90)
    A['Map/UI_Map_Teleport_Dim'] = ([map_teleport(0, pal=DIMC)], None)
    A['Map/UI_Map_Return'] = ([map_return()], None)
    A['Map/UI_Map_Return_Hover'] = ([map_return(hover=True)], None)
    A['Map/UI_Map_Return_Dim'] = ([map_return(pal=DIMC)], None)
    A['Effects/Fx_PurpleHeart_Aura'] = ([aura(f) for f in range(8)], 150)
    A['Effects/Fx_PurpleHeart_Mote'] = ([mote(f) for f in range(6)], 110)
    A['Effects/Fx_PurpleHeart_Block'] = ([block_wave(f) for f in range(8)], 50)
    return A


# ============================================================ ESCENA DE PRUEBA (solo revisión)
def scene(out_dir, A):
    """GIF y lámina: jugador del piso 2 con el aura y una onda al bloquear, el corazón flotando sobre su sombra y los
    iconos sobre el fondo del mapa."""
    from PIL import Image as _I
    root = os.path.join(HERE, '..')
    floor = C('#24564a')
    body_path = os.path.join(root, 'floor2-bodies', 'Placoderm', 'Player_Placoderm_Idle_side.png')
    body = _I.open(body_path).convert('RGBA') if os.path.exists(body_path) else None
    cell = body.height if body else 24
    frames = []
    aura_f = A['Effects/Fx_PurpleHeart_Aura'][0]
    heart_f = A['Items/Item_RoomHeart_Idle'][0]
    shadow = as_img(A['Items/Item_RoomHeart_Shadow'][0][0])
    wave = A['Effects/Fx_PurpleHeart_Block'][0]
    for t in range(24):
        im = _I.new('RGBA', (128, 72), floor)
        for x in range(0, 128, 9):                              # motas del suelo
            for y in range((x * 7) % 11, 72, 13):
                im.putpixel((x, y), C('#2e6652'))
        px, py = 40, 44
        im.alpha_composite(as_img(aura_f[(t // 2) % 8]), (px - 16, py - 4))
        if body:
            n = body.width // cell
            fr = body.crop(((t // 3 % n) * cell, 0, (t // 3 % n + 1) * cell, cell))
            im.alpha_composite(fr, (px - cell // 2, py - cell + 4))
        if 12 <= t < 20:
            w = as_img(wave[t - 12])
            im.alpha_composite(w, (px - 24, py - 32))
        # corazón con vaivén por código (1 px) sobre la sombra fija
        hx, hy = 92, 40
        im.alpha_composite(shadow, (hx - 8, hy + 6))
        bob = round(math.sin(t / 24 * 2 * math.pi) * 1)
        im.alpha_composite(as_img(heart_f[(t // 2) % 8]), (hx - 8, hy - 8 + bob))
        frames.append(im.resize((im.width * 5, im.height * 5), _I.NEAREST))
    frames[0].save(os.path.join(out_dir, 'gif', 'escena_ui_extras.gif'), save_all=True, append_images=frames[1:],
                   duration=90, loop=0, disposal=2)
    # iconos sobre el fondo y el suelo del mapa
    m = _I.new('RGBA', (80, 24), C('#0d221d'))
    for x in range(0, 20):
        for y in range(4, 20):
            m.putpixel((x, y), C('#2e6652'))
    icons = [A['Map/UI_Map_Heart'][0][0], A['Map/UI_Map_Teleport'][0][0], A['Map/UI_Map_Teleport_Hover'][0][0],
             A['Map/UI_Map_Teleport_Dim'][0][0], A['Map/UI_Map_Return'][0][0], A['Map/UI_Map_Return_Hover'][0][0],
             A['Map/UI_Map_Return_Dim'][0][0]]
    for i, ic in enumerate(icons):
        ic = as_img(ic)
        m.alpha_composite(ic, (2 + i * 11, 12 - ic.height // 2))
    m.resize((m.width * 10, m.height * 10), _I.NEAREST).save(os.path.join(out_dir, 'vista_previa_mapa_iconos.png'))


if __name__ == '__main__':
    A = build()
    for name, (frames, ms) in A.items():
        path = os.path.join(OUT, name + '.png')
        os.makedirs(os.path.dirname(path), exist_ok=True)
        save_sheet(path, frames)
    preview(os.path.join(OUT, 'vista_previa_ui_extras.png'),
            {name.split('/')[-1]: frames for name, (frames, ms) in A.items()}, BG, scale=6)
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    for name, (frames, ms) in A.items():
        if ms and len(frames) > 1:
            gif(os.path.join(OUT, 'gif', name.split('/')[-1] + '.gif'), frames, ms, BG, scale=8)
    scene(OUT, A)
    print('Listo en', OUT)
