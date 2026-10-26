"""Temas 'Cambrian' (océano cámbrico, piso 2) y 'Shore' (mezcla entre la sopa y el océano, para la sala de paso).

Como generar_tema_ameba.py: reutiliza generar_escenario.py y ../environment-shapes/generar_formas.py sin tocarlos,
añadiendo las paletas y la decoración del piso 2 (esponjas, corales, conchas de trilobite y lirios de mar).
Shore es la mitad exacta entre las paletas de Soup y Cambrian, así que la sala de paso pasa de una a otra en dos saltos.

Uso: python generar_tema_cambrico.py
Salida: environment/Cambrian/, environment/Shore/ y environment-shapes/{Cambrian,Shore}/tiles/.
"""
import os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
SHAPES = os.path.normpath(os.path.join(HERE, '..', 'environment-shapes'))

SOUP = dict(
    floor=['#173a30', '#1d4639', '#245244', '#2e6150'], fleck=['#3a7a5c', '#4f9a70', '#8fd0a0'], algae=['#2f6a3a', '#4a8a40', '#7ab050'],
    cap=['#1e1426', '#2a1c34', '#352442', '#45304f'], face=['#3a2440', '#57345c', '#7a4a78', '#a46a98', '#c890b8'],
    vein='#8a3a5a', lip='#d8a0c8', drip='#c890b8', shadow='#10281f')

# Fondo marino: arena azulada y verdosa, roca de arrecife azul con vetas de coral y gotas de agua clara.
CAMBRIAN = dict(
    floor=['#16344a', '#1b3d55', '#214862', '#2a5772'], fleck=['#3a6e8a', '#5a94b0', '#a8dcec'], algae=['#1e5a52', '#2a7a66', '#4aa080'],
    cap=['#0e1830', '#152240', '#1c2c50', '#283a64'], face=['#1c2c50', '#28406c', '#365890', '#4e78b0', '#80a8d8'],
    vein='#d0705a', lip='#b0e0ec', drip='#80c8e0', shadow='#0a1a2a')


def mix(a, b, t=0.5):
    def one(x, y):
        x, y = x.lstrip('#'), y.lstrip('#')
        return '#' + ''.join(f'{round(int(x[i:i+2], 16) * (1 - t) + int(y[i:i+2], 16) * t):02x}' for i in (0, 2, 4))
    return {k: [one(x, y) for x, y in zip(v, b[k])] if isinstance(v, list) else one(v, b[k]) for k, v in a.items()}


SHORE = mix(SOUP, CAMBRIAN)

PALETTES = f"""
THEMES['Cambrian'] = {CAMBRIAN!r}
THEMES['Shore'] = {SHORE!r}
"""

# Decoración nueva del piso 2. Usa new, P, _outline y math del generador del escenario.
DECO = '''
def deco_sponge(th, k=0):
    """Esponja de barril: tubo anaranjado con poros y la boca oscura arriba."""
    im = new(); ramp = ['#7a3c18', '#b86a28', '#e89a40', '#ffd088']
    cx = 7.5 if k == 0 else 8.5; h = 10 if k == 0 else 7; w = 3.2 if k == 0 else 2.6
    for y in range(T):
        for x in range(T):
            top = 15 - h
            if top <= y <= 14 and abs(x + .5 - cx) <= w + (y - top) * 0.08:
                nx = (x + .5 - cx) / w
                P(im, x, y, ramp[3 if nx < -0.45 else 2 if nx < 0.2 else 1 if nx < 0.7 else 0])
    top = 15 - h
    for x in range(int(cx - w + 1), int(cx + w)):
        P(im, x, top, '#3a1a0c'); P(im, x, top + 1, ramp[0])
    for dx, dy in ((-1, 3), (1, 5), (-2, 6), (0, 8), (2, 2)):
        if dy < h - 1: P(im, int(cx + dx), top + dy, ramp[0])
    _outline(im); return im

def deco_coral(th):
    """Coral ramificado rosa con las puntas claras."""
    im = new(); ramp = ['#902848', '#d05068', '#ff98a0']
    def branch(x, y, ang, length, depth):
        for i in range(int(length)):
            x += math.cos(ang) * 1.0; y -= math.sin(ang) * 1.0
            P(im, round(x), round(y), ramp[1] if i < length - 1 or depth else ramp[2])
            P(im, round(x) + 1, round(y), ramp[0])
        if depth:
            branch(x, y, ang + 0.5, length * 0.7, depth - 1); branch(x, y, ang - 0.55, length * 0.65, depth - 1)
        else:
            P(im, round(x), round(y), ramp[2])
    branch(7, 15, math.pi / 2, 5, 2)
    _outline(im); return im

def deco_trilobite(th):
    """Concha de trilobite en el suelo: cabeza en media luna, cuerpo segmentado en tres lóbulos y cola."""
    im = new(); ramp = ['#4a3a30', '#7a6450', '#a8907a', '#d8c4a8']
    for y in range(T):
        for x in range(T):
            nx, ny = (x + .5 - 8) / 4.2, (y + .5 - 8.5) / 6
            if nx * nx + ny * ny <= 1:
                axial = abs(x + .5 - 8) < 1.3
                seg = (y % 2 == 0) and y > 5
                c = ramp[2] if axial else ramp[1]
                if seg: c = ramp[0] if not axial else ramp[1]
                if y <= 5: c = ramp[2] if nx < 0.3 else ramp[1]
                P(im, x, y, c)
    P(im, 6, 4, ramp[3]); P(im, 9, 4, ramp[3]); P(im, 7, 3, ramp[3])
    _outline(im); return im

def deco_crinoid(th, k):
    """Lirio de mar: tallo fino y una corona de brazos plumosos que se mece (4 cuadros)."""
    im = new(); stalk = ['#6a3a7a', '#9a5aa8']; arm = ['#c888d0', '#f0c8f0']
    sway = (0, 1, 0, -1)[k]
    for i in range(9):
        P(im, 8 + round(sway * i / 9), 15 - i, stalk[i % 2])
    cx, cy = 8 + sway, 5
    for a in (-1.25, -0.6, 0.0, 0.6, 1.25):
        for r in range(1, 5):
            x = cx + math.sin(a + sway * 0.15) * r; y = cy - math.cos(a) * r * 0.9
            P(im, round(x), round(y), arm[0] if r < 4 else arm[1])
            if r % 2 == 0: P(im, round(x + (1 if a >= 0 else -1)), round(y + 1), arm[1])
    P(im, cx, cy, stalk[1])
    _outline(im); return im

'''

DECO_BRANCH = (
    "    if theme == 'Cambrian':\n"
    "        deco['Deco_Bubbles'] = [deco_bubbles(th, k) for k in range(2)]\n"
    "        deco['Deco_Algae'] = [deco_algae(th, k) for k in range(4)]\n"
    "        deco['Deco_Sponge'] = [deco_sponge(th, 0)]\n"
    "        deco['Deco_SpongeSmall'] = [deco_sponge(th, 1)]\n"
    "        deco['Deco_Coral'] = [deco_coral(th)]\n"
    "        deco['Deco_Trilobite'] = [deco_trilobite(th)]\n"
    "        deco['Deco_Crinoid'] = [deco_crinoid(th, k) for k in range(4)]\n"
    "    elif theme == 'Shore':\n"
    "        deco['Deco_Bubbles'] = [deco_bubbles(th, k) for k in range(2)]\n"
    "        deco['Deco_Algae'] = [deco_algae(th, k) for k in range(4)]\n"
    "        deco['Deco_Specks'] = [deco_specks(th, k) for k in range(3)]\n"
    "        deco['Deco_SpongeSmall'] = [deco_sponge(th, 1)]\n"
    "        deco['Deco_Diatom'] = [deco_diatom(th)]\n"
    "    elif theme == 'Soup':\n")


def run_scenery():
    path = os.path.join(HERE, 'generar_escenario.py')
    with open(path, encoding='utf-8') as f:
        src = f.read().replace('\r\n', '\n')
    src = src.replace("def new(w=T, h=T):", PALETTES + "\ndef new(w=T, h=T):", 1)
    src = src.replace("# ---------------- generar ----------------", DECO + "# ---------------- generar ----------------", 1)
    src = src.replace("    if theme == 'Soup':\n", DECO_BRANCH, 1)
    src = src.replace("for theme in ('Soup', 'Boss'):", "for theme in ('Cambrian', 'Shore'):")
    assert src.count("for theme in ('Cambrian', 'Shore'):") == 2 and 'deco_crinoid(th, k)' in src, \
        'generar_escenario.py cambió: revisa este script'
    sys.argv = [path, HERE]
    exec(compile(src, path, 'exec'), {'__name__': '__main__', '__file__': path})


def run_shapes():
    path = os.path.join(SHAPES, 'generar_formas.py')
    with open(path, encoding='utf-8') as f:
        src = f.read().replace('\r\n', '\n')
    src = src.replace("VARIANTS = ['Soup', 'Boss']", "VARIANTS = ['Cambrian', 'Shore']", 1)
    sys.argv = [path, HERE, SHAPES]
    exec(compile(src, path, 'exec'), {'__name__': '__main__', '__file__': path})


if __name__ == '__main__':
    run_scenery()
    run_shapes()
    print('Temas Cambrian y Shore listos.')
