"""Tema 'Amoeba' (sala de la ameba gigante): los mismos tiles que Soup y Boss, en azul marino profundo para que
contraste con la ameba rosa pálido. Reutiliza generar_escenario.py y ../environment-shapes/generar_formas.py sin
tocarlos: carga su código, añade la paleta y lo ejecuta solo para este tema.

Uso: python generar_tema_ameba.py
Salida: environment/Amoeba/ (tiles, puertas, deco) y environment-shapes/Amoeba/tiles/ (piezas de formas).
La sala va vacía: solo burbujas y motas en el suelo, sin obstáculos.
"""
import os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
SHAPES = os.path.normpath(os.path.join(HERE, '..', 'environment-shapes'))

PALETTE = """
THEMES['Amoeba'] = dict(
    floor=['#111a30', '#15213b', '#1a2847', '#213256'], fleck=['#2c4a78', '#3e66a0', '#86b4e8'], algae=['#1e3a64', '#2c5288', '#4a78b0'],
    cap=['#0a0e1c', '#0f1528', '#151d36', '#1e2846'], face=['#16203e', '#1f2d58', '#2a3e78', '#3c5aa0', '#6a8cd0'],
    vein='#3a7ac8', lip='#9cc4f0', drip='#6a8cd0', shadow='#0a1020')
"""


def run_scenery():
    path = os.path.join(HERE, 'generar_escenario.py')
    with open(path, encoding='utf-8') as f:
        src = f.read().replace('\r\n', '\n')
    # La paleta, justo después de las de Soup y Boss.
    src = src.replace("def new(w=T, h=T):", PALETTE + "\ndef new(w=T, h=T):", 1)
    # Decoración: solo burbujas y motas (la sala va vacía).
    src = src.replace("    if theme == 'Soup':\n",
                      "    if theme == 'Amoeba':\n"
                      "        deco['Deco_Bubbles'] = [deco_bubbles(th, k) for k in range(2)]\n"
                      "        deco['Deco_Specks'] = [deco_specks(th, k) for k in range(3)]\n"
                      "    elif theme == 'Soup':\n", 1)
    # Solo este tema (las salas de muestra y la vista previa también).
    src = src.replace("for theme in ('Soup', 'Boss'):", "for theme in ('Amoeba',):")
    assert src.count("for theme in ('Amoeba',):") == 2, 'generar_escenario.py cambió: revisa este script'
    sys.argv = [path, HERE]
    exec(compile(src, path, 'exec'), {'__name__': '__main__', '__file__': path})


def run_shapes():
    path = os.path.join(SHAPES, 'generar_formas.py')
    with open(path, encoding='utf-8') as f:
        src = f.read().replace('\r\n', '\n')
    src = src.replace("VARIANTS = ['Soup', 'Boss']", "VARIANTS = ['Amoeba']", 1)
    sys.argv = [path, HERE, SHAPES]
    exec(compile(src, path, 'exec'), {'__name__': '__main__', '__file__': path})


if __name__ == '__main__':
    run_scenery()
    run_shapes()
    print('Tema Amoeba listo.')
