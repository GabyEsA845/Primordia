"""
generar_volumen.py - Primordia: piezas del control de volumen de la pausa.

Reutiliza generar_menus.py (paleta del panel de estadísticas, contorno #181425,
rampas de 4 tonos con luz arriba a la izquierda). Escribe en Pause/:
  UI_Slider_Track  24x8  riel hundido (9-slice, borde 4)
  UI_Slider_Fill   24x8  relleno lima (9-slice, borde 4)
  UI_Slider_Knob   8x10  tirador: una burbujita de membrana
  UI_Stat_Music    12x12 icono: nota musical
  UI_Stat_Sfx      12x12 icono: altavoz con ondas
y una vista previa x4 (vista_previa_volumen.png).

Uso: python generar_volumen.py
"""
import importlib.util
import os
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
_argv = sys.argv
sys.argv = ['generar_menus.py', os.path.join(HERE, '..'), HERE]
_spec = importlib.util.spec_from_file_location('generar_menus', os.path.join(HERE, 'generar_menus.py'))
G = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(G)
sys.argv = _argv

Cv, col, OUTL = G.Cv, G.col, G.OUTL
MEM, LIME, AQUA = G.MEM, G.LIME, G.AQUA
PANEL_FILL = '#0b1a16'
TRACK_FILL = '#14302a'
PANEL_RIM = ['#0e201b', '#173a32', '#24524a', '#3f7a68']
NOTE = ['#6a3a8a', '#9a5ac8', '#c890f0', '#f0d8ff']      # morado de las Extremidades, para la música


def rounded(w, h, x, y, r=3.0):
    """True si (x, y) cae dentro de un rectángulo w x h de esquinas redondeadas."""
    px, py = x + .5, y + .5
    dx = max(r - px, 0, px - (w - r))
    dy = max(r - py, 0, py - (h - r))
    return dx * dx + dy * dy <= r * r


def slider_track():
    """Ranura hundida: borde oscuro arriba (sombra) y claro abajo (luz que rebota), fondo casi negro."""
    w, h = 24, 8
    cv = Cv(w, h)
    for y in range(1, h - 1):
        for x in range(w):
            if not rounded(w, h - 2, x, y - 1):
                continue
            if y == 1:
                c = col(PANEL_RIM[1])
            elif y == h - 2:
                c = col(PANEL_RIM[3])
            else:
                c = col(TRACK_FILL)
            cv.put(x, y, c)
    cv.outline()
    return cv


def slider_fill():
    """Relleno de membrana lima con brillo arriba; mismo contorno que el riel para encajar dentro."""
    w, h = 24, 8
    cv = Cv(w, h)
    for y in range(1, h - 1):
        for x in range(w):
            if not rounded(w, h - 2, x, y - 1):
                continue
            ramp = LIME[3] if y == 2 else LIME[2] if y <= 4 else LIME[1]
            cv.put(x, y, col(ramp))
    # burbujitas dentro del relleno, como el resto de membranas
    for (x, y) in ((5, 4), (13, 3), (19, 5)):
        cv.put(x, y, col(LIME[3]))
    cv.outline()
    return cv


def slider_knob():
    """Burbuja de membrana verde (la del HUD) con brillo blanco arriba a la izquierda."""
    w, h = 8, 10
    cv = Cv(w, h)
    for y in range(h):
        for x in range(w):
            px, py = x + .5, y + .5
            if ((px - 4) / 3.4) ** 2 + ((py - 5) / 4.4) ** 2 > 1:
                continue
            v = G.shade_v((px - 4) / 3.4, (py - 5) / 4.4)
            cv.put(x, y, col(G.MEM_HI[G.ramp_idx(v + 0.2)]))
    cv.put(2, 2, '#ffffff')
    cv.put(3, 2, col(G.MEM_HI[3]))
    cv.outline()
    return cv


def stat_music():
    """Corchea: cabeza redonda abajo a la izquierda, plica y banderín."""
    cv = Cv(12)
    for y in range(12):
        for x in range(12):
            px, py = x + .5, y + .5
            head = ((px - 4.2) / 2.6) ** 2 + ((py - 8.6) / 2.0) ** 2 <= 1
            stem = 6.0 <= px <= 7.0 and 1.5 <= py <= 8.6
            flag = 7.0 <= px <= 9.8 and 1.5 <= py <= 5.5 and (py - 1.5) <= (px - 7.0) * 1.3 + 0.8 and (py - 1.5) >= (px - 7.0) * 1.3 - 1.4
            if head or stem or flag:
                v = G.shade_v((px - 6) / 5, (py - 6) / 5)
                cv.put(x, y, col(NOTE[G.ramp_idx(v + 0.15)]))
    cv.put(3, 7, '#ffffff')
    cv.outline()
    return cv


def stat_sfx():
    """Altavoz (caracola) y dos ondas que salen hacia la derecha."""
    cv = Cv(12)
    for y in range(12):
        for x in range(12):
            px, py = x + .5, y + .5
            box = 0.5 <= px <= 3.2 and 4.3 <= py <= 7.7
            cone = 3.2 <= px <= 6.2 and abs(py - 6) <= 1.7 + (px - 3.2) * 1.15
            if box or cone:
                v = G.shade_v((px - 3.5) / 4, (py - 6) / 4)
                cv.put(x, y, col(AQUA[G.ramp_idx(v + 0.15)]))
    # ondas (sin contorno, para que se lean finas)
    waves = []
    for y in range(12):
        for x in range(7, 12):
            px, py = x + .5, y + .5
            d = ((px - 5.0) ** 2 + (py - 6.0) ** 2) ** 0.5
            if abs(py - 6) <= (px - 5.0) * 1.1 and (abs(d - 3.6) <= 0.5 or abs(d - 5.9) <= 0.5):
                waves.append((x, y, d < 4.7))
    cv.put(2, 5, '#ffffff')
    cv.outline()
    for x, y, inner in waves:
        cv.put(x, y, col(AQUA[3] if inner else AQUA[2]))
    return cv


PIECES = {
    'UI_Slider_Track': slider_track,
    'UI_Slider_Fill': slider_fill,
    'UI_Slider_Knob': slider_knob,
    'UI_Stat_Music': stat_music,
    'UI_Stat_Sfx': stat_sfx,
}


def preview(images):
    """Panel de muestra a x4: icono, riel con relleno al 60 % y tirador, sobre el fondo del panel."""
    w, h = 120, 44
    im = Image.new('RGBA', (w, h), PANEL_FILL)

    def nine_h(src, width):
        """Estira en horizontal un 9-slice de borde 4 (solo hace falta en x)."""
        sw, sh = src.size
        out = Image.new('RGBA', (width, sh), (0, 0, 0, 0))
        out.paste(src.crop((0, 0, 4, sh)), (0, 0))
        mid = src.crop((4, 0, sw - 4, sh)).resize((width - 8, sh), Image.NEAREST)
        out.paste(mid, (4, 0))
        out.paste(src.crop((sw - 4, 0, sw, sh)), (width - 4, 0))
        return out

    for row, (icon, level) in enumerate((('UI_Stat_Music', 0.6), ('UI_Stat_Sfx', 0.85))):
        y = 6 + row * 18
        im.alpha_composite(images[icon], (6, y))
        track = nine_h(images['UI_Slider_Track'], 70)
        im.alpha_composite(track, (22, y + 2))
        fill_w = max(8, int(70 * level))
        im.alpha_composite(nine_h(images['UI_Slider_Fill'], fill_w), (22, y + 2))
        im.alpha_composite(images['UI_Slider_Knob'], (22 + fill_w - 4, y + 1))
    return im.resize((w * 4, h * 4), Image.NEAREST)


def main():
    out = os.path.join(HERE, 'Pause')
    os.makedirs(out, exist_ok=True)
    images = {}
    for name, fn in PIECES.items():
        images[name] = fn().img()
        images[name].save(os.path.join(out, name + '.png'))
        print(name, images[name].size)
    preview(images).save(os.path.join(HERE, 'vista_previa_volumen.png'))


if __name__ == '__main__':
    main()
