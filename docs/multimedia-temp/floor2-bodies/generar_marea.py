"""Marea (Corona de brazos, nivel III): el remolino estalla y empuja hacia fuera.

Effects/Fx_ArmCrown_Tide.png  6 cuadros de 64×64, centrado. Los brazos de corriente del remolino se deshacen en un anillo
                              de agua con espuma que se abre hasta el borde (radio 30 px, unas 2 u) y se desvanece.
Mismos colores que Fx_ArmCrown_Whirl: azul de la corriente y verde claro de las motas.
"""
import math
import os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
CURRENT = (150, 215, 240)
LIGHT = (214, 244, 255)
FOAM = (200, 255, 200)
S = 64
C0 = S / 2


def put(im, x, y, col, a):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < S and 0 <= y < S and a > 0:
        old = im.getpixel((x, y))
        if old[3] < a:
            im.putpixel((x, y), col + (int(a),))


def frame(k):
    im = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    t = k / 5
    # Brazos del remolino que se abren y se estiran hacia fuera (cuadros 0–2).
    if k < 3:
        for arm in range(3):
            base = arm * 2 * math.pi / 3 + k * 0.5
            for i in range(40):
                u = i / 39
                r = 3 + u * (12 + k * 6)
                a = base + u * (2.6 - k * 0.8)
                put(im, C0 + r * math.cos(a), C0 + r * math.sin(a), LIGHT if u < 0.3 else CURRENT, 230 - k * 50)
    # Anillo de agua que se abre (cuadros 1–5), con un borde claro y espuma por fuera.
    if k >= 1:
        radius = 6 + (k - 1) * 6
        fade = 255 if k < 4 else (170 if k == 4 else 90)
        steps = int(2 * math.pi * radius * 1.6)
        for i in range(steps):
            a = 2 * math.pi * i / steps
            wobble = math.sin(a * 9 + k) * 0.35
            put(im, C0 + (radius + wobble) * math.cos(a), C0 + (radius + wobble) * math.sin(a), LIGHT, fade)
            put(im, C0 + (radius - 1.5 + wobble) * math.cos(a), C0 + (radius - 1.5 + wobble) * math.sin(a), CURRENT, fade * 0.7)
            if k >= 2:
                put(im, C0 + (radius - 3.5) * math.cos(a), C0 + (radius - 3.5) * math.sin(a), CURRENT, fade * 0.3)
        # Espuma: gotas sueltas por fuera del anillo.
        for j in range(10 + k * 2):
            a = j * 2.399 + k
            r = radius + 2 + (j % 3)
            put(im, C0 + r * math.cos(a), C0 + r * math.sin(a), FOAM, fade * 0.9)
    return im


if __name__ == '__main__':
    frames = [frame(k) for k in range(6)]
    out = Image.new('RGBA', (S * len(frames), S))
    for i, f in enumerate(frames):
        out.paste(f, (i * S, 0))
    out.save(os.path.join(HERE, 'Effects', 'Fx_ArmCrown_Tide.png'))
    print('ok')
