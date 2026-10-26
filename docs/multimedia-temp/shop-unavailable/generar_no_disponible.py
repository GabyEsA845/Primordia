"""
generar_no_disponible.py - Primordia: señal de "no disponible" de la tienda.

Flota sobre un pedestal vacío cuando el artículo ya no se puede comprar
(la mutación está al nivel máximo). Sin texto: se entiende por la forma y el color.

Reutiliza generar_tienda.py (que carga generar_mapa.py -> generar_menus.py ->
generar_ui.py -> generar_mutaciones.py -> generar_jugador.py y generar_escenario.py):
Cv, contorno #181425, rampas de 4 tonos, el vidrio, el pergamino, la madera de
naufragio, el pedestal y la sombra de los artículos.

Uso:
  python generar_no_disponible.py [carpeta_multimedia_temp] [carpeta_salida]
Por defecto: la carpeta de arriba (donde están shop/, map/, ui/...) y la carpeta de este script.
"""
import importlib.util
import math
import os
import sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, '..')
OUT = sys.argv[2] if len(sys.argv) > 2 else HERE

# --------------------------------------------- cargar generar_tienda.py
_argv = sys.argv
sys.argv = ['generar_tienda.py', ROOT, os.devnull]
_spec = importlib.util.spec_from_file_location('generar_tienda', os.path.join(ROOT, 'shop', 'generar_tienda.py'))
T = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(T)
sys.argv = _argv
U = T.U

C, OUTL, BG = T.C, T.OUTL, T.BG
Cv, col, rect, sheet = T.Cv, T.col, T.rect, T.sheet
shade_v, ramp_idx = T.shade_v, T.ramp_idx
GLASS, PAPER, WOOD, WOOD_TOP = T.GLASS, T.PAPER, T.WOOD, T.WOOD_TOP

# rojo apagado (más oscuro y menos saturado que HEART): se lee "cerrado", no "peligro"
RED = ['#5a1c28', '#8e3040', '#bc5464', '#e8a0a8']
PAINT = ['#6a2030', '#a03a48', '#c85a64']        # pintura roja de la X, algo gastada


# ============================================================ 1. SEÑAL PRINCIPAL
CX, CY = 8.0, 8.0
R_OUT, R_IN = 6.45, 4.7          # aro de vidrio de 2 px; la señal ocupa 13x13 (15x15 con contorno)
BAR_W = 1.1                     # media anchura de la barra diagonal


def unavailable(f):
    """Lente de vidrio rojizo con una barra diagonal (arriba izquierda -> abajo derecha).
    Animación: un brillo recorre el vidrio y el rojo late muy poco. El contorno no cambia."""
    cv = Cv(16)
    pulse = (0.0, 0.06, 0.12, 0.06)[f]
    glass_a = (70, 80, 90, 80)[f]
    for y in range(16):
        for x in range(16):
            px, py = x + .5 - CX, y + .5 - CY
            r = math.hypot(px, py)
            if r > R_OUT:
                continue
            nx, ny = px / R_OUT, py / R_OUT
            on_bar = abs(px - py) / math.sqrt(2) <= BAR_W and r <= R_IN + 0.6
            if r >= R_IN or on_bar:
                # aro y barra: vidrio rojo macizo con luz de arriba a la izquierda
                v = shade_v(nx, ny) + pulse
                if on_bar and r < R_IN:
                    d = (px - py) / math.sqrt(2)          # lado de la barra: el de arriba a la derecha más claro
                    v = 0.45 + pulse + (0.35 if d > 0.4 else -0.25 if d < -0.6 else 0.0)
                cv.put(x, y, RED[ramp_idx(v)])
            else:
                # interior: vidrio translúcido, deja ver lo de detrás
                cv.put(x, y, col(RED[0], glass_a + 60))
    cv.outline()
    # brillo fijo arriba a la izquierda, sobre el aro
    for (x, y, c) in ((4, 3, '#ffffff'), (3, 4, RED[3]), (5, 2, RED[3])):
        cv.put(x, y, c)
    # destello que cruza el vidrio en diagonal (perpendicular a la barra), de arriba a la izquierda hacia abajo
    sweep = (None, 4.0, 7.5, 11.0)[f]
    if sweep is not None:
        for y in range(16):
            for x in range(16):
                p = (x, y)
                if p not in cv.px or cv.px[p][:3] == OUTL[:3]:
                    continue
                s = x + y + 1.0                            # distancia a lo largo de la diagonal de la luz
                if abs(s - sweep * 2 + 1) < 0.6:
                    c = cv.px[p]
                    if c[3] < 255:
                        cv.put(x, y, col(RED[3], 140))
                    else:
                        cv.put(x, y, RED[3])
    return cv


# ============================================================ 2. ALTERNATIVA: LETRERO CON X
def sign(f):
    """Etiqueta de laboratorio de pergamino clavada en un palito de madera de naufragio,
    con una X roja pintada. Pivote abajo en el centro (8, 15). Se mece 1 px."""
    cv = Cv(16)
    dx = (0, 1, 0, -1)[f]
    # palito: fijo abajo, se inclina arriba
    for y in range(8, 16):
        t = (15 - y) / 7
        x = 7.5 + dx * t
        cv.put(x - .5, y, WOOD[2])
        cv.put(x + .5, y, WOOD[1])
    cv.put(7, 15, WOOD[1]); cv.put(8, 15, WOOD[0])
    # etiqueta de pergamino: 12x8, con las esquinas gastadas
    x0, y0, x1, y1 = 2 + dx, 1, 13 + dx, 8
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            corner = (x in (x0, x1)) and (y in (y0, y1))
            if corner:
                continue
            c = PAPER[2]
            if y == y0 or x == x0:
                c = PAPER[3]                               # canto iluminado
            elif y == y1 or x == x1:
                c = PAPER[1]
            elif (x * 7 + y * 3) % 11 == 0:
                c = PAPER[1]                               # manchitas de agua
            cv.put(x, y, c)
    cv.put(x1 - 1, y1, PAPER[0]); cv.put(x1, y1 - 1, PAPER[0])   # esquina doblada
    # X pintada a mano: dos trazos de 2 px, el de arriba izquierda -> abajo derecha encima
    xa, ya = x0 + 3, y0 + 1
    for i in range(6):
        cv.put(xa + 5 - i, ya + i, PAINT[1])
        cv.put(xa + 6 - i, ya + i, PAINT[0])
    for i in range(6):
        cv.put(xa + i, ya + i, PAINT[2])
        cv.put(xa + 1 + i, ya + i, PAINT[1])
    # clavo que la sujeta al palito
    cv.put(7.5 + dx * 0.9 - .5, y1 - 0.0, '#8a7a6a')
    cv.outline()
    return cv


# ============================================================ GUARDAR
MS = {}


def save(name, frames, ms):
    os.makedirs(OUT, exist_ok=True)
    sheet(frames).save(os.path.join(OUT, name + '.png'))
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    out = []
    for cv in frames:
        b = Image.new('RGBA', (16, 16), BG)
        b.alpha_composite(cv.img())
        out.append(b.resize((16 * 12, 16 * 12), Image.NEAREST).convert('RGB'))
    out[0].save(os.path.join(OUT, 'gif', name + '.gif'), save_all=True, append_images=out[1:], duration=ms, loop=0)
    MS[name] = (len(frames), 16, 16, ms)


# ============================================================ VISTA PREVIA
def preview(S1, S2, path):
    A = T.build()
    W, H = 150, 88
    scr = Image.new('RGBA', (W, H), BG)
    ptop = 30
    centers = (26, 70, 114)
    # 1. a la venta
    T.item_on_pedestal(scr, A, centers[0], ptop, U.bubble('Predator', 0), icon='Mandibles')
    # 2. vacío con la señal principal (con su sombra)
    T.item_on_pedestal(scr, A, centers[1], ptop, S1[0])
    # 3. vacío con la alternativa
    T.item_on_pedestal(scr, A, centers[2], ptop, S2[0])
    lab = (214, 255, 154)
    for cx, s in zip(centers, ('A LA VENTA', 'PRINCIPAL', 'LETRERO')):
        T.ptext(scr, (cx - T.tw(s) // 2, 4), s, lab)
    # cuadros de cada animación
    for i, cv in enumerate(S1):
        scr.alpha_composite(cv.img(), (8 + i * 18, 64))
    for i, cv in enumerate(S2):
        scr.alpha_composite(cv.img(), (80 + i * 17, 64))
    scr.resize((W * 4, H * 4), Image.NEAREST).convert('RGB').save(path)


def main():
    S1 = [unavailable(f) for f in range(4)]
    S2 = [sign(f) for f in range(4)]
    save('Shop_Unavailable', S1, 150)
    save('Shop_Unavailable_Sign', S2, 150)
    preview(S1, S2, os.path.join(OUT, 'vista_previa_no_disponible.png'))
    for k, v in MS.items():
        print(k, *v)


if __name__ == '__main__':
    main()
