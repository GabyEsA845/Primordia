"""Recuadro del Órgano especial en el HUD (abajo a la izquierda), con su tubo de recarga.

- UI_Organ_Panel (46×36): recuadro de 36×36 (el icono grande del órgano, 32×32, va en (2, 2)) y, a su derecha, un tubo de cristal
  vertical vacío. Mismo marco de membrana que las ranuras del HUD.
- UI_Organ_SapFill (4×32): savia lima del tubo; el código la recorta de abajo arriba (Image Filled, Vertical, Bottom).
- UI_Organ_SapTop (4×2, 4 cuadros): la superficie de la savia, que tiembla; va en la altura del relleno.
- UI_Organ_SapBubbles (4×32, 6 cuadros): burbujas que suben, recortadas igual que el relleno.
- UI_Organ_Notch (4×1): muesca del tubo para los órganos que se recargan matando (una por enemigo, la pone el código).
- UI_Organ_Ready (46×36, 7 cuadros): aviso de listo: el marco destella, un brillo cruza el icono y saltan chispitas.
- UI_Organ_Pulse (46×36, 6 cuadros): pulso de luz suave en bucle mientras está listo.
- UI_Organ_EmptyIcon (32×32): hueco del órgano cuando no hay ninguno (sin tubo).
El icono apagado lo hace el código (luminancia -> tonos DIM del HUD), y el color le vuelve de abajo arriba al ritmo del tubo.
Vista previa con los estados y una escena (gif/escenas/). Skill primordia-sprites (pixel.py).

Uso: python generar_hud_organo.py [carpeta_salida]   (por defecto, la carpeta de este script)
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
SKILL = os.path.join(HERE, '..', '..', '..', '.claude', 'skills', 'primordia-sprites', 'scripts')
sys.path.insert(0, os.path.abspath(SKILL))
from pixel import *
from PIL import Image

OUT = sys.argv[1] if len(sys.argv) > 1 else HERE
BG = C('#16352e')
MEM = ['#173a32', '#2a5e50', '#4a9078', '#8fd0b0']           # membrana de los marcos del HUD (ui/generar_ui.py)
DIM = ['#1c332d', '#28463e', '#36594f', '#4a7062']           # siluetas apagadas del HUD
SAP = ['#2e6a1e', '#5aa83a', '#8ce659', '#d4ffb0']           # savia: el lima de la ranura del Órgano
DIM_ICON = ['#28463e', '#36594f', '#4a7062', '#628a7a']      # icono apagado: los DIM del HUD, un tono más claros
GLASS = ['#1e3e36', '#3a6a5c', '#a8e8d0']                     # tubo de cristal
GLOW = '#f2ffd8'
W, H = 46, 36
BOX = 36
TX, TW = 38, 8                                                # tubo: x 38-45
INNER = (40, 2, 4, 32)                                        # hueco de la savia: x 40-43, y 2-33

def rounded(x, y, w, h, r):
    ax = abs(x + .5 - w / 2); ay = abs(y + .5 - h / 2)
    qx, qy = max(ax - (w / 2 - r), 0), max(ay - (h / 2 - r), 0)
    return math.hypot(qx, qy) <= r

def panel(empty=False):
    cv = Canvas(W, H)
    for y in range(H):                                        # recuadro: borde de 2 px de membrana y fondo translúcido
        for x in range(BOX):
            if not rounded(x, y, BOX, BOX, 4): continue
            if 2 <= x < BOX - 2 and 2 <= y < BOX - 2:
                cv.put(x, y, (16, 36, 31, 170))
            else:
                v = (-0.6 * (x - BOX / 2) - 0.8 * (y - BOX / 2)) / (BOX / 2)
                cv.put(x, y, C(MEM[3] if v > 0.45 else MEM[2] if v > -0.2 else MEM[1]))
    for (x, y) in ((3, 3), (BOX - 4, 3), (3, BOX - 4), (BOX - 4, BOX - 4)):   # remaches en las esquinas (como los paneles)
        cv.put(x, y, C(MEM[3])); cv.put(x + 1, y + 1, C(MEM[1]))
    if not empty:
        for y in range(H):                                    # tubo de cristal con tapas de membrana
            for x in range(TX, TX + TW):
                if not rounded(x - TX, y, TW, H, 3): continue
                if y < 2 or y >= H - 2:
                    cv.put(x, y, C(MEM[2] if y < 2 else MEM[1]))
                elif x in (TX, TX + TW - 1):
                    cv.put(x, y, C(GLASS[1]))
                elif x == TX + 1 or x == TX + TW - 2:
                    cv.put(x, y, C(GLASS[0], 230))
                else:
                    cv.put(x, y, C(GLASS[0], 160))            # hueco (la savia va encima)
        for y in range(4, H - 6, 1):                          # reflejo del cristal, a la izquierda
            if y % 9 < 6: cv.put(TX + 1, y, C(GLASS[2], 150))
        for x in range(BOX - 1, TX):                          # unión del recuadro con el tubo
            for y in (H // 2 - 3, H // 2 + 2): cv.put(x, y, C(MEM[1]))
    cv.outline(); cv.clean()
    return cv

def sap_fill():
    cv = Canvas(4, 32)
    for y in range(32):
        for x in range(4):
            cv.put(x, y, C(SAP[2] if x == 1 else SAP[1] if x in (0, 2) else SAP[0]))
        if y % 7 == 3: cv.put(2, y, C(SAP[2]))                 # vetas que suben
    return cv

def sap_top(i):
    cv = Canvas(4, 2)
    hi = (0, 1, 2, 1)[i]
    for x in range(4): cv.put(x, 1, C(SAP[2]))
    cv.put(hi, 0, C(SAP[3])); cv.put(min(3, hi + 1), 0, C(SAP[2]))
    return cv

def sap_bubbles(i, n=6):
    cv = Canvas(4, 32)
    for (x0, y0, big) in ((1, 4, 0), (2, 15, 1), (0, 24, 0), (2, 29, 0)):
        y = (y0 - i * 32 / n * .9) % 32
        cv.put(x0, y, C(SAP[3]))
        if big: cv.put(x0 + 1, y, C(SAP[3])); cv.put(x0, y + 1, C(SAP[2]))
    return cv

def notch():
    cv = Canvas(4, 1)
    for x in range(4): cv.put(x, 0, C(MEM[0]))
    return cv

def ready(i):
    """0-1 el marco destella en lima, 1-4 un brillo diagonal cruza el icono, 2-6 chispitas que saltan y se apagan."""
    cv = Canvas(W, H)
    flash = (1.0, .8, .5, .3, .15, 0, 0)[i]
    if flash:
        for y in range(H):
            for x in range(BOX):
                if not rounded(x, y, BOX, BOX, 4): continue
                if not (2 <= x < BOX - 2 and 2 <= y < BOX - 2):
                    cv.put(x, y, C(SAP[3] if flash > .6 else SAP[2], int(255 * flash)))
                elif not (3 <= x < BOX - 3 and 3 <= y < BOX - 3):
                    cv.put(x, y, C(SAP[2], int(200 * flash)))
        for y in range(2, H - 2):                             # el tubo también destella
            for x in range(INNER[0], INNER[0] + 4): cv.put(x, y, C(SAP[3], int(180 * flash)))
    if 1 <= i <= 4:                                           # brillo diagonal que cruza el icono
        d0 = -10 + (i - 1) * 13
        for y in range(2, BOX - 2):
            for x in range(2, BOX - 2):
                d = (x - 2) + (BOX - 2 - y) * .6 - 18 - d0
                if -1.6 < d < 1.6: cv.put(x, y, C(GLOW, 200 if abs(d) < .8 else 110))
    rnd = random.Random(3)
    sp = [(rnd.uniform(4, BOX - 4), rnd.uniform(2, 10), rnd.uniform(-1, 1)) for _ in range(4)]
    if 2 <= i <= 6:
        for k, (x, y, dx) in enumerate(sp):
            t = i - 2
            yy = y - t * 1.6; xx = x + dx * t
            if t < 3: sparks(cv, xx, yy, 2 if t < 1 else 1)
            else: cv.put(xx, yy, C(SAP[3], 200 - (t - 3) * 90))
    return cv

def pulse(i, n=6):
    """Luz suave que late en el marco y en las esquinas mientras está listo (en bucle)."""
    cv = Canvas(W, H)
    a = int(70 + 60 * math.sin(i / n * 2 * math.pi))
    for y in range(H):
        for x in range(BOX):
            if not rounded(x, y, BOX, BOX, 4): continue
            if (x in (2, BOX - 3) or y in (2, BOX - 3)) and 2 <= x < BOX - 2 and 2 <= y < BOX - 2:
                cv.put(x, y, C(SAP[2], a))
    for (x, y) in ((3, 3), (BOX - 4, 3), (3, BOX - 4), (BOX - 4, BOX - 4)):
        cv.put(x, y, C(SAP[3], min(255, a + 80)))
    return cv

def empty_icon():
    """Hueco del órgano: contorno punteado de un órgano (bolsa con un conducto), en tonos apagados."""
    cv = Canvas(32)
    c = 16
    for k in range(60):
        a = k / 60 * 2 * math.pi
        if k % 3 == 2: continue
        x = c + math.cos(a) * 9; y = c + 2 + math.sin(a) * 8
        cv.put(x, y, C(DIM[3]))
    for s in range(6):
        if s % 2 == 0: cv.put(c + 5 + s * .7, c - 6 - s, C(DIM[3]))
    for (x, y) in ((c - 3, c), (c + 2, c + 3), (c - 1, c + 5)): cv.put(x, y, C(DIM[2]))
    return cv

# =================================================================================================
# Icono apagado (lo hará el código con un material): luminancia -> tonos DIM; aquí, para la vista previa
# =================================================================================================
def dim(icon):
    im = icon.copy(); px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a == 0: continue
            if (r, g, b) == OUTL[:3]: px[x, y] = OUTL[:3] + (180,); continue
            l = (r * 3 + g * 6 + b) / 10
            px[x, y] = C(DIM_ICON[min(3, int(l / 56))])[:3] + (235,)
    return im

def hud(icon, progress, kills=None, state='charging', fi=0, E=None):
    """El recuadro montado: progress 0..1; kills=(muertos, necesarios) para los que se recargan matando."""
    im = Image.new('RGBA', (W, H))
    if icon is None:
        im.alpha_composite(as_img(E['UI_Organ_Panel_Empty'][0]))
        im.alpha_composite(as_img(E['UI_Organ_EmptyIcon'][0]), (2, 2))
        return im
    im.alpha_composite(as_img(E['UI_Organ_Panel'][0]))
    ic = icon if progress >= 1 else dim(icon)
    if progress < 1:                                          # el color vuelve de abajo arriba
        cut = int(round(32 * (1 - progress)))
        col = icon.crop((0, cut, 32, 32))
        ic = ic.copy(); ic.paste(Image.new('RGBA', (32, 32 - cut)), (0, cut)); ic.alpha_composite(col, (0, cut))
        if 0 < cut < 32:                                      # línea lima en el borde que sube (solo sobre el icono)
            for x in range(32):
                if icon.getpixel((x, cut))[3] > 0 and icon.getpixel((x, cut))[:3] != OUTL[:3]: ic.putpixel((x, cut), C(SAP[2]))
    im.alpha_composite(ic, (2, 2))
    h = int(round(32 * progress))
    if h > 0:
        fill = as_img(E['UI_Organ_SapFill'][0]).crop((0, 32 - h, 4, 32))
        im.alpha_composite(fill, (INNER[0], 2 + 32 - h))
        bub = as_img(E['UI_Organ_SapBubbles'][fi % 6]).crop((0, 32 - h, 4, 32))
        im.alpha_composite(bub, (INNER[0], 2 + 32 - h))
        if progress < 1: im.alpha_composite(as_img(E['UI_Organ_SapTop'][fi % 4]), (INNER[0], 2 + 32 - h - 1))
    if kills:
        n = kills[1]
        for k in range(1, n):
            im.alpha_composite(as_img(E['UI_Organ_Notch'][0]), (INNER[0], 2 + 32 - int(round(32 * k / n))))
    if state == 'ready': im.alpha_composite(as_img(E['UI_Organ_Ready'][fi]))
    if state == 'pulse': im.alpha_composite(as_img(E['UI_Organ_Pulse'][fi % 6]))
    return im

def screen_bg(w, h):
    """Esquina de abajo a la izquierda de una sala del piso 3 (suelo del pantano), para ver el recuadro en su sitio."""
    rnd = random.Random(2)
    tones = ('#1c2e20', '#213626', '#273f2b', '#2f4a32')
    im = Image.new('RGBA', (w, h), C(tones[0]))
    for _ in range(w * h // 14):
        x, y = rnd.randrange(w), rnd.randrange(h)
        im.putpixel((x, y), C(tones[rnd.choice((1, 1, 2, 3))]))
    return im

if __name__ == '__main__':
    E = {
        'UI_Organ_Panel': [panel()], 'UI_Organ_Panel_Empty': [panel(empty=True)],
        'UI_Organ_SapFill': [sap_fill()], 'UI_Organ_SapTop': [sap_top(i) for i in range(4)],
        'UI_Organ_SapBubbles': [sap_bubbles(i) for i in range(6)], 'UI_Organ_Notch': [notch()],
        'UI_Organ_Ready': [ready(i) for i in range(7)], 'UI_Organ_Pulse': [pulse(i) for i in range(6)],
        'UI_Organ_EmptyIcon': [empty_icon()],
    }
    for name, frames in E.items():
        save_sheet(os.path.join(OUT, 'HUD', name + '.png'), frames)
    icon = Image.open(os.path.join(HERE, '..', 'ui', 'Icons', 'Icon_InkSac_32.png')).convert('RGBA')
    icon2 = Image.open(os.path.join(HERE, '..', 'floor2-bodies', 'Icons', 'Icon_BroodSac_32.png')).convert('RGBA')
    states = {'Recargando 15 %': [hud(icon, .15, E=E)], 'Recargando 55 %': [hud(icon, .55, E=E)],
              'Recargando 90 %': [hud(icon, .9, E=E)], 'Listo (aviso)': [hud(icon, 1, state='ready', fi=i, E=E) for i in range(7)],
              'Listo (pulso)': [hud(icon, 1, state='pulse', fi=i, E=E) for i in range(6)],
              'Matando: 2 de 5': [hud(icon2, .4, kills=(2, 5), E=E)], 'Sin órgano': [hud(None, 0, E=E)]}
    preview(os.path.join(OUT, 'vista_previa_hud_organo.png'), states, BG, scale=5)
    # Escena: la esquina de la pantalla (240×100 de 480×272) con el ciclo entero
    os.makedirs(os.path.join(OUT, 'gif', 'escenas'), exist_ok=True)
    frames = []
    seq = [(0.0, 'charging')] * 3
    seq += [(k / 40, 'charging') for k in range(41)]
    seq += [(1, 'ready')] * 7 + [(1, 'pulse')] * 18
    for f, (p, st) in enumerate(seq):
        bg = screen_bg(160, 70)
        fi = (f - 44) if st == 'ready' else f
        bg.alpha_composite(hud(icon, p, state=st, fi=fi, E=E), (8, 70 - H - 8))
        frames.append(bg.resize((160 * 4, 70 * 4), Image.NEAREST).convert('RGB'))
    frames[0].save(os.path.join(OUT, 'gif', 'escenas', 'escena_recarga.gif'), save_all=True, append_images=frames[1:],
                   duration=[60 if st == 'ready' else 120 if st == 'pulse' else 70 for _, st in seq], loop=0)
    frames = []
    for f in range(60):
        kills = min(5, f // 10)
        bg = screen_bg(160, 70)
        st = 'charging' if kills < 5 else ('ready' if f - 50 < 7 else 'pulse')
        bg.alpha_composite(hud(icon2, kills / 5, kills=(kills, 5), state=st, fi=max(0, f - 50) if st == 'ready' else f, E=E), (8, 70 - H - 8))
        frames.append(bg.resize((160 * 4, 70 * 4), Image.NEAREST).convert('RGB'))
    frames[0].save(os.path.join(OUT, 'gif', 'escenas', 'escena_recarga_matando.gif'), save_all=True, append_images=frames[1:], duration=90, loop=0)
    print('Listo:', OUT)
