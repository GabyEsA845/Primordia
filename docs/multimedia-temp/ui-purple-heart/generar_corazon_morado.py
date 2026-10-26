"""Capa del corazón morado sobre los corazones del HUD: con el corazón morado cogido, cada corazón rojo lleva el contorno
morado y unas grietas moradas que entran desde el borde, como si el morado se incrustara en él.

Uso: python generar_corazon_morado.py   (lee la forma de Assets/_Primordia/Art/UI/UI_Heart_Full.png)

Salida: UI_Heart_PurpleInlay_0..2.png (16×16, tres variantes de grietas) y vista_previa.png (sobre lleno, medio y vacío, ×6).
"""
import os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
UI = os.path.normpath(os.path.join(HERE, '..', '..', '..', 'Assets', '_Primordia', 'Art', 'UI'))

DARK = (74, 26, 122, 255)        # contorno morado (el oscuro del corazón morado)
MID = (138, 64, 208, 255)        # grietas
LIGHT = (192, 128, 255, 255)     # brillo en el arranque de cada grieta

# Grietas por variante: lista de trazos, cada uno una lista de píxeles (x, y) que parte del contorno hacia dentro.
CRACKS = [
    [[(3, 4), (4, 5), (4, 6), (5, 7)], [(12, 3), (11, 4), (11, 5)], [(9, 11), (8, 10), (8, 9), (7, 8)]],
    [[(2, 6), (3, 7), (4, 7), (5, 8)], [(9, 2), (9, 3), (10, 4), (10, 5)], [(12, 9), (11, 9), (10, 10)]],
    [[(5, 2), (5, 3), (6, 4)], [(13, 6), (12, 7), (11, 7), (10, 8)], [(4, 9), (5, 10), (6, 10), (6, 11)]],
]


def inlay(shape, variant):
    w, h = shape.size
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    opaque = lambda x, y: 0 <= x < w and 0 <= y < h and shape.getpixel((x, y))[3] > 0
    # Contorno: los píxeles opacos con algún vecino transparente.
    for y in range(h):
        for x in range(w):
            if opaque(x, y) and not all(opaque(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                out.putpixel((x, y), DARK)
    for stroke in CRACKS[variant]:
        for i, (x, y) in enumerate(stroke):
            if opaque(x, y):
                out.putpixel((x, y), LIGHT if i == 0 else MID)
    return out


if __name__ == '__main__':
    full = Image.open(os.path.join(UI, 'UI_Heart_Full.png')).convert('RGBA')
    layers = []
    for v in range(len(CRACKS)):
        layer = inlay(full, v)
        layer.save(os.path.join(HERE, f'UI_Heart_PurpleInlay_{v}.png'))
        layers.append(layer)

    # Vista previa: una fila sin capa y otra con ella, sobre lleno, lleno, medio y vacío.
    hearts = [Image.open(os.path.join(UI, f'UI_Heart_{n}.png')).convert('RGBA') for n in ('Full', 'Full', 'Half', 'Empty')]
    prev = Image.new('RGBA', (4 * 18 + 2, 2 * 18 + 2), (20, 24, 36, 255))
    for i, heart in enumerate(hearts):
        prev.alpha_composite(heart, (2 + i * 18, 2))
        combo = heart.copy()
        combo.alpha_composite(layers[i % len(layers)])
        prev.alpha_composite(combo, (2 + i * 18, 20))
    prev.resize((prev.width * 6, prev.height * 6), Image.NEAREST).save(os.path.join(HERE, 'vista_previa.png'))
    print('Capas del corazón morado generadas.')
