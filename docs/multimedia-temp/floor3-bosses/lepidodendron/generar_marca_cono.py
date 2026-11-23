"""Lepidodendron ancestral · marca de aviso del cono (sustituye la marca provisional dibujada por código).

Marca orgánica: un anillo irregular de polvo de esporas rojas, con un velo rojo tenue dentro y motas que flotan.
En picada, como la sombra del cono y Fx_LepidoSporeMark: elipse ligeramente aplastada (alto = Q × ancho). El borde
de fuera es exactamente el área del golpe: semiejes 1,6 × 1,12 u (25,6 × 17,9 px a 16 PPU); el daño del código
debe ser esa misma elipse. Usa el rojo de aviso del juego (255, 80, 60), el mismo que Fx_LepidoSporeMark.

  Fx_ConeMark_Follow (6 × 110 ms, bucle): sigue al jugador durante el aviso; suave, el polvo respira y titila.
  Fx_ConeMark_Lock   (8 × 90 ms, una vez): el punto se fija (fase de sombra, 0,72 s): el anillo se cierra desde
                     1,18 × R hasta R, se espesa y se enciende; el último cuadro destella.
  Fx_ConeMark_Hold   (2 × 60 ms, bucle): mientras el cono baja: encendido / destello.

Celda 64×48, centro (32, 24). Sin giro ni escala por código (romperían el píxel).

Uso: python generar_marca_cono.py [piezas] [escenas]
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import generar_conos_lepidodendron as K
from generar_conos_lepidodendron import *              # noqa: F401,F403
from PIL import Image

CONES = os.path.join(HERE, 'Cones')
CW, CH = 64, 48
CX, CY = 32.0, 24.0
Q = .7                                                 # aplastamiento en picada (alto / ancho)
R = 1.6 * 16                                           # radio del golpe en píxeles
WARN_RED = (255, 80, 60)
CRIM = [C(c)[:3] for c in ('#4a0c16', '#8a1a24', '#c8302e')] + [WARN_RED] + [C(c)[:3] for c in ('#ffa088', '#ffd8c8')]
HAZE = (110, 24, 22)

def _hash(x, y, s):
    return ((x * 73856093) ^ (y * 19349663) ^ (s * 83492791)) % 1000 / 1000

def _wob(a, ph):
    """Ruido angular suave (-1..1 aprox.): el grosor irregular del anillo."""
    return (.5 * math.sin(3 * a + 1.3 + ph) + .3 * math.sin(5 * a + .4 - ph * .7) + .2 * math.sin(11 * a + 2.1)) / 1.0

def mark(radius, ring_a, haze_a, thick, glow, ph, flash=False, motes=0.0, seed=1):
    """Un cuadro de la marca. radius: borde de fuera; ring_a/haze_a: alfa del anillo y del velo; thick: grosor
    (factor); glow 0..1: cuánto se enciende; ph: fase de la animación (0..2π); motes: cuánto caen hacia dentro (0..1)."""
    cv = Canvas(CW, CH)
    for y in range(CH):
        for x in range(CW):
            dx, dy = x + .5 - CX, (y + .5 - CY) / Q    # coordenadas sin aplastar
            d = math.hypot(dx, dy); a = math.atan2(dy, dx)
            px = math.hypot(math.cos(a), Q * math.sin(a))   # píxeles reales por unidad radial en este ángulo
            w = _wob(a, ph)
            ro = radius + .35 * w                         # borde de fuera: casi exacto, apenas ondulado
            clump = max(0., math.sin(7 * a + 1.7 + ph * .3) + .7 * math.sin(13 * a - .6 + ph * .2) - .2)
            th = (2.6 + 1.2 * w + 4.2 * clump) * thick    # hasta dónde llega el polvo: grumos que se meten dentro
            h = _hash(x, y, seed)
            if ro - th <= d <= ro:
                u = (ro - d) / th                         # 0 = borde de fuera, 1 = punta del polvo
                rim = (ro - d) * px < 1.3                 # el borde: siempre 1-2 px reales, también arriba y abajo
                if not rim and h < (u - .15) * 1.15: continue   # el polvo se deshace hacia dentro
                lit = -dx - dy > 0                        # arco de arriba a la izquierda, algo más claro
                if flash: col = CRIM[5] if u < .4 else CRIM[4]
                elif rim: col = CRIM[3] if (glow > .5 or lit) else CRIM[2]
                elif u < .45: col = CRIM[3] if (lit and glow > .2) else CRIM[2]
                else: col = CRIM[2] if glow > .6 else CRIM[1]
                if not flash and h > .94: col = CRIM[4]   # granos que brillan
                cv.put(x, y, col + (ring_a if rim or u < .5 else int(ring_a * .85),))
            elif ro < d <= ro + 2.2 and h < .07 + .05 * glow:
                cv.put(x, y, CRIM[2] + (int(ring_a * .6),))   # esporas que se escapan por fuera
            elif d < ro - th:
                # velo: más denso junto al anillo, a manchas, tramado con Bayer
                g = .08 + .6 * (d / radius) ** 2.4 + .22 * math.sin(dx * .33 + ph) * math.sin(dy * .29 - ph * .6)
                if (BAYER[y % 4][x % 4] + .5) / 16 < g:
                    cv.put(x, y, HAZE + (int(haze_a * (1.25 if d > ro - th - 4 else 1)),))
    rnd = random.Random(seed * 31)
    for i in range(16):                               # motas de espora que flotan sobre la marca
        a0 = rnd.uniform(0, 2 * math.pi); r0 = rnd.uniform(.35, .9) * radius; tw = rnd.uniform(0, 2 * math.pi)
        rr = r0 * (1 - .45 * motes) + 1.6 * math.sin(ph + tw)
        aa = a0 + .18 * math.sin(ph * .5 + tw)
        mx, my = int(CX + rr * math.cos(aa)), int(CY + rr * math.sin(aa) * Q)
        b = .5 + .5 * math.sin(ph + tw)                # titila
        if b < .25: continue
        col = CRIM[5] if flash else CRIM[4] if b > .8 else CRIM[3]
        cv.put(mx, my, col + (min(255, ring_a + 20),))
        if i % 5 == 0 and b > .6: cv.put(mx + 1, my, CRIM[2] + (ring_a,))
    return cv

def follow(k, n=6):
    ph = 2 * math.pi * k / n
    return mark(R, 175, 34, .85, .15, ph)

def lock(k, n=8):
    t = k / (n - 1); e = 1 - (1 - t) ** 2
    ph = 2 * math.pi * k / 6
    return mark(R * (1.18 - .18 * e), int(185 + 60 * e), int(40 + 45 * e), .8 + .5 * e, e, ph,
                flash=(k == n - 1), motes=e)

def hold(k):
    return mark(R, 245, 85, 1.3, 1.0, 2 * math.pi * 7 / 6, flash=(k == 1), motes=1.0)

def piezas():
    out = {'Fx_ConeMark_Follow': [follow(k).img() for k in range(6)],
           'Fx_ConeMark_Lock': [lock(k).img() for k in range(8)],
           'Fx_ConeMark_Hold': [hold(k).img() for k in range(2)]}
    for name, fr in out.items(): save_sheet(os.path.join(CONES, name + '.png'), fr)
    ms = {'Fx_ConeMark_Follow': 110, 'Fx_ConeMark_Lock': 90, 'Fx_ConeMark_Hold': 60}
    os.makedirs(GIF, exist_ok=True)
    for name, fr in out.items(): gif(os.path.join(GIF, name + '.gif'), fr, ms[name], '#273f2b', scale=4)
    preview(os.path.join(HERE, 'vista_previa_marca_cono.png'), out, '#273f2b', scale=3)
    return out

# =================================================================================================
# Escena: la marca sigue a la rana, se fija, la rana se aparta, el cono baja y golpea
# =================================================================================================
def escenas(marks):
    W, H = 50, 30
    fx, fy = 25, 15.5
    room = A.room(A.rect_room(W, H, (2, int(fy - 10.5), W - 3, H - 2)))
    X0, Y0 = int((fx - 20) * U), int((fy - 10.25) * U)
    def at(im, spr, px, py, x, y): im.alpha_composite(spr, (int(X0 + x - px), int(Y0 + y - py)))
    FX_, FY_ = int(fx * U) - X0, int(fy * U) - Y0
    sf = K._sheetf
    at(room, sf('Tree', 'Lepido_BackRoots', 520)[0], 260, 196, FX_, FY_)
    at(room, sf('Tree', 'Lepido_MoundShadow', 480)[0], 240, 96, FX_, FY_)
    at(room, sf('Tree', 'Lepido_Mound', 480)[0], 240, 96, FX_, FY_)
    trunk = sf('Tree', 'Lepido_Trunk', 288)[0]
    vrest = sf('Tree', 'Lepido_Veins_Rest', 288)[0]; vwarn = sf('Tree', 'Lepido_Veins_Warn', 288)
    tiles = sf('Canopy', 'Fx_Dapple', 256)
    sway = sf('Canopy', 'Canopy_Arena_Sway', 704); shake = sf('Canopy', 'Canopy_Arena_Shake', 704)
    shadow = sf('Cones', 'Fx_ConeShadow', 72); slam = sf('Cones', 'Fx_ConeSlam', 128)
    crater = sf('Cones', 'Fx_ConeCrater', 72)[0]; cone = sf('Cones', 'Lepido_Cone_Armor0', 72)[0]
    tile = sf('Cones', 'Lepido_ConeBranch_Tile', 72)[0]
    frog = A.frame(FROG, 0, 32)
    LX, LY = 176, 300

    def frame(st, t):
        im = room.copy()
        dl = Image.new('RGBA', im.size, (0, 0, 0, 0)); tl = tiles[(t // 2) % 8]
        for ty in range(0, im.height, 256):
            for tx in range(0, im.width, 256): dl.alpha_composite(tl, (tx, ty))
        im.alpha_composite(dl)
        at(im, trunk, 144, 352, FX_, FY_)
        vk = st.get('veins')
        at(im, vrest if vk is None else vwarn[vk], 144, 352, FX_, FY_)
        if st.get('crater'): at(im, crater, 36, 16, LX, LY)
        if 'shadow' in st: at(im, shadow[st['shadow']], 36, 18, LX, LY)
        if 'mark' in st:
            spr, mx, my = st['mark']; at(im, spr, 32, 24, mx, my)
        sk = st.get('slam')
        if sk is not None: at(im, slam[sk].crop((0, 0, 128, 36)), 64, 36, LX, LY)
        objs = [(st['frog'][1], 'frog')]
        if 'dy' in st: objs.append((LY + st['dy'], 'cone'))
        for _, kind in sorted(objs):
            if kind == 'frog': at(im, frog, 16, 30, *st['frog'])
            else:
                dy = st['dy']; ty = LY + dy - 98 - K.TILE_H
                while ty > -K.TILE_H * 2: at(im, tile, 36, 0, LX, ty); ty -= K.TILE_H
                at(im, cone, 36, 98, LX, LY + dy)
        if sk is not None: at(im, slam[sk].crop((0, 36, 128, 64)), 64, 0, LX, LY)
        at(im, shake[t % 6] if st.get('shake') else sway[t % 8], 0, 0, -32, -10)
        return im.crop((X0, Y0, X0 + 640, Y0 + 360))

    S, ms, keys = [], [], {}
    def add(st, m, key=None):
        if key: keys[key] = len(S)
        S.append(st); ms.append(m)
    F, L, Hd = marks['Fx_ConeMark_Follow'], marks['Fx_ConeMark_Lock'], marks['Fx_ConeMark_Hold']
    path = [(LX + 70 - 70 * i / 13, LY + 24 - 24 * i / 13) for i in range(14)]   # la rana camina hacia el punto
    for i in range(3): add({'frog': (int(path[0][0]), int(path[0][1]))}, 140)
    for i in range(14):
        fxp, fyp = path[i]; mx, my = path[max(0, i - 2)]             # la marca la sigue con un pelín de retraso
        add({'frog': (int(fxp), int(fyp)), 'mark': (F[i % 6], int(mx), int(my) - 2), 'veins': min(i // 2, 7)},
            110, 'sigue' if i == 6 else None)
    for k in range(8):                                                 # se fija; la rana se aparta
        add({'frog': (LX - 9 * k, LY + 2 * k), 'mark': (L[k], LX, LY), 'shadow': k, 'veins': 7, 'shake': True},
            90, {2: 'se fija', 7: 'destello'}.get(k))
    for j, dy in enumerate((-230, -130, -55, -14)):                    # baja el cono
        add({'frog': (LX - 63, LY + 14), 'mark': (Hd[j % 2], LX, LY), 'shadow': 7, 'dy': dy, 'shake': True},
            80, 'cae' if j == 2 else None)
    for k in range(7):
        add({'frog': (LX - 63, LY + 14), 'shadow': 4, 'dy': 0, 'slam': k, 'crater': True, 'shake': k < 4},
            70, 'golpe' if k == 1 else None)
    for k in range(6): add({'frog': (LX - 63, LY + 14), 'shadow': 4, 'dy': 0, 'crater': True}, 140)
    K._escena(frame, S, ms, keys, 'Lepido_cono_marca', ['sigue', 'se fija', 'destello', 'cae', 'golpe'],
              'Lepidodendron ancestral · marca de aviso del cono: sigue, se fija, la rana se aparta y cae',
              'vista_previa_marca_cono_escena.png')

if __name__ == '__main__':
    which = sys.argv[1:] or ['piezas', 'escenas']
    out = piezas()
    if 'escenas' in which: escenas(out)
