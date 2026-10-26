"""Tanda 4 de las mutaciones del piso 2: los restos de Carroña (clase nueva Carroñero, marrón arena).

Restos que deja un enemigo al morir (montón de arena y carne con costillas), restos dobles dorados, el efecto al
comerlos y la marca de colmillos sobre el jugador con el bonus acumulado.
Hecho con la skill primordia-sprites (pixel.py): 16 px por unidad, luz arriba a la izquierda, contorno OUTL.

Uso: python generar_carrona.py [carpeta_salida]   (por defecto, la carpeta de este script)
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

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__))
FX = os.path.join(OUT, 'Effects')
BG = C('#1f6a7a')                          # arena de las Someras: donde peor se lee el marrón

CARR = ramp('#a07a48')                      # marrón arena de la clase Carroñero
GOLD = ramp('#d4a42c')                      # restos dobles (Doble ración)
MEAT = ramp('#b04a48')                      # carne que asoma
BONE = ['#b8ac90', '#e4dcc4', '#f6f0e0']

# --- restos ----------------------------------------------------------------------------------------
def remains(frame, pal=CARR, big=False):
    """Restos, 16×16 (apoyo abajo en el centro): montón con un trozo de carne y costillas, y un destello que lo recorre."""
    cv = Canvas(16)
    cx, cy = 8, 11.5
    s = 1.18 if big else 1.0
    # costillas detrás: dos arcos de hueso que salen del montón curvándose hacia la derecha
    for bx, h in ((-1.0, 7), (2.0, 6)):
        for t in range(int(h * s)):
            x = cx + bx * s + (t * t) / (2.6 * h)
            cv.put(x, cy - 0.5 - t, BONE[2] if t >= int(h * s) - 2 else BONE[1])
    # montón irregular: bulto grande a la izquierda y otro pequeño a la derecha, con la carne asomando
    ellipse(cv, cx - 1 * s, cy, 4.6 * s, 2.9 * s, pal)
    ellipse(cv, cx + 3 * s, cy + 0.6, 2.6 * s, 2.0 * s, pal, shift=-1 if big else 0)
    ellipse(cv, cx - 1.6 * s, cy - 1.0, 2.4 * s, 1.5 * s, MEAT)
    cv.outline(); cv.clean()
    # destello: dos píxeles en diagonal que cruzan el montón de izquierda a derecha
    rx, ry = 5.5 * s, 2.9 * s
    gx = cx - rx + 2 + frame * (2 * rx - 3) / 3
    for d in range(2):
        p = (int(gx + d), int(cy - 1 + d))
        if p in cv.px and cv.px[p] != OUTL: cv.px[p] = C(pal[3])
    if frame in (0, 2):                                          # chispa encima: se puede coger
        sparks(cv, cx + (-5 if frame == 0 else 5), cy - ry - 2, 1)
    return cv

# --- comer -----------------------------------------------------------------------------------------
def chunk(cv, x, y, pal, s):
    """Trozo de carne/arena de s×s con contorno."""
    tmp = Canvas(cv.w, cv.h)
    ellipse(tmp, x, y, s / 2 + .2, s / 2 + .2, pal)
    tmp.outline()
    for p, c in tmp.px.items(): cv.px.setdefault(p, c)

def eat(frame):
    """Al tocarlos, 24×24, 5 cuadros: los trozos giran en espiral, suben y se convierten en chispas."""
    cv = Canvas(24)
    n = 5
    for k in range(n):
        ang = k * 2 * math.pi / n + frame * 0.9
        r = 7.5 - frame * 1.6
        x = 12 + math.cos(ang) * r
        y = 15 + math.sin(ang) * r * 0.55 - frame * 2.2
        if frame < 3:
            chunk(cv, x, y, MEAT if k % 2 else CARR, 3 if frame < 2 else 2)
        elif frame == 3:
            cv.put(x, y, SPARK[2])
    if frame >= 3: sparks(cv, 12, 6, 2 if frame == 3 else 1)
    return cv

# --- marca de colmillos ----------------------------------------------------------------------------
def stack(n, frame):
    """Marca sobre el jugador, 12×8: n colmillos que comparten contorno; en el cuadro 1 brillan las puntas."""
    cv = Canvas(12, 8)
    fang = ramp('#e0c48c')
    x0 = (12 - (3 * n + 1)) // 2 + 1
    for i in range(n):
        x = x0 + i * 3
        for y in range(1, 6):
            w = 2 if y < 4 else 1
            for dx in range(w):
                cv.put(x + dx + (0 if y < 4 else 1 if i % 2 == 0 else 0), y, fang[2 if dx == 0 else 1])
        if frame == 1: cv.put(x, 1, fang[3])
    cv.outline(); cv.clean()
    return cv

anims = {
    'Fx_Carrion_Remains': [remains(k) for k in range(4)],
    'Fx_Carrion_RemainsDouble': [remains(k, GOLD, big=True) for k in range(4)],
    'Fx_Carrion_Eat': [eat(k) for k in range(5)],
    'Status_Carrion_1': [stack(1, k) for k in range(2)],
    'Status_Carrion_2': [stack(2, k) for k in range(2)],
    'Status_Carrion_3': [stack(3, k) for k in range(2)],
}

if __name__ == '__main__':
    for name, frames in anims.items():
        save_sheet(os.path.join(FX, name + '.png'), frames)
    preview(os.path.join(OUT, 'vista_previa_carrona.png'), anims, BG, scale=6)
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    gif(os.path.join(OUT, 'gif', 'Fx_Carrion_Remains.gif'), anims['Fx_Carrion_Remains'], 150, BG, scale=8)
    gif(os.path.join(OUT, 'gif', 'Fx_Carrion_Eat.gif'), anims['Fx_Carrion_Eat'], 60, BG, scale=6)
    print('Carroña lista')
