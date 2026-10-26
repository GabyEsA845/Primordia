"""<Recurso> de Primordia (piso N).

<Qué se dibuja y con qué estilo: tamaño del cuerpo, lienzo, de qué piezas vecinas toma la paleta.>
Hecho con la skill primordia-sprites (pixel.py): 16 px por unidad, luz arriba a la izquierda, contorno OUTL.

Uso: python generar_<recurso>.py [carpeta_salida]   (por defecto, la carpeta de este script)
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
BG = C('#0f2a4a')                         # fondo de revisión del piso (ver references/pisos.md)

# --- paleta ----------------------------------------------------------------------------------------
BODY = ramp('#3fa89a')                    # o una rampa del piso; comprobar con ramp_info(BODY)
SPOT = ramp('#e0a040')

# --- un cuadro = una función con parámetros ---------------------------------------------------------
def creature(phase=0.0, squash=0.0, eyes='angry', pal=None):
    """Criatura de ejemplo de unos 18 px en un lienzo de 24×24, mirando a la derecha."""
    body = (pal or {}).get('body', BODY); spot = (pal or {}).get('spot', SPOT)
    cv = Canvas(24)
    cx, cy = 12, 12 + math.sin(phase * 2 * math.pi) * 0.6
    rx, ry = 8.5 * (1 + squash), 7 * (1 - squash)
    # patas finas sin contorno: se dibujan después del contorno, solo fuera del cuerpo
    ellipse(cv, cx, cy, rx, ry, body)
    for k, (sx, sy) in enumerate(((-3, -2), (1, 1), (-1, 3))):
        ellipse(cv, cx + sx, cy + sy, 1.6, 1.2, spot, shift=0)
    cv.outline(); cv.clean()
    for i in range(3):
        a = phase * 2 * math.pi + i * 2.1
        x = cx - 4 + i * 4; y0 = cy + ry
        for t in range(3): cv.put_out(x + round(math.sin(a) * t * 0.5), y0 + t, body[0])
    # ojo de la familia: blanco roto con pupila mirando a la derecha
    ex, ey = int(cx + rx * 0.45), int(cy - ry * 0.35)
    for dx in range(2):
        for dy in range(2): cv.put(ex + dx, ey + dy, EYE)
    cv.put(ex + 1, ey + 1, OUTL)
    if eyes == 'angry': cv.put(ex, ey - 1, OUTL); cv.put(ex + 1, ey - 1, OUTL)
    return cv

def hurt():
    return [creature(pal={'body': WHITE4, 'spot': WHITE4}), creature(pal={'body': RED4, 'spot': RED4})]

# --- salida ----------------------------------------------------------------------------------------
if __name__ == '__main__':
    anims = {
        'Example_Move': [creature(phase=i / 4, squash=0.08 * math.sin(i * math.pi / 2)) for i in range(4)],
        'Example_Hurt': hurt(),
    }
    for name, frames in anims.items():
        save_sheet(os.path.join(OUT, name + '.png'), frames)
    preview(os.path.join(OUT, 'vista_previa_ejemplo.png'), anims, BG, scale=4)
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    gif(os.path.join(OUT, 'gif', 'Example_Move.gif'), anims['Example_Move'], 120, BG, scale=6)
    print('Listo en', OUT)
