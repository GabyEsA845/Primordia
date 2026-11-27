"""Menú de pausa nuevo, estilo membrana (subtanda 1): panel, botón célula con núcleo y cinta del título.

Familia verde agua de los menús actuales (MEM, AQUA y GLASS de generar_menus.py / generar_ui.py), con un aspecto más orgánico:
paneles con membrana doble e interior translúcido, botones en forma de píldora con un núcleo que se enciende al
resaltarlos, y el título sobre una membrana estirada entre dos células. Todo es 9-slice salvo el núcleo.
La UI va a 480×270 con 16 px por unidad: 1 px del sprite = 1 px de la UI.
Hecho con la skill primordia-sprites (pixel.py): luz arriba a la izquierda, contorno OUTL.

Uso: python generar_pausa_membrana.py [carpeta_salida]   (por defecto, la carpeta de este script)
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
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = sys.argv[1] if len(sys.argv) > 1 else HERE
REPO = _skill_scripts().split(os.sep + '.claude')[0]
FONTS = os.path.join(REPO, 'Assets', '_Primordia', 'Art', 'Fonts')
BG = C('#16352e')

# --- paleta (la de los menús actuales) -------------------------------------------------------------
MEM = ['#173a32', '#2a5e50', '#4a9078', '#8fd0b0']
MEM_HI = ['#2a5e50', '#4a9078', '#9ae0b8', '#e4ffd8']
AQUA = ['#1e6a5a', '#2f9a80', '#5ad0b0', '#b0f0e0']
GLASS = ['#1e4a46', '#3a7a74', '#7cc4bc', '#d8fff6']
DIM = ['#1c332d', '#28463e', '#36594f', '#4a7062']
LIT = ['#2f9a80', '#5ad0b0', '#b0f0e0', '#eafff4']        # núcleo encendido
FILL = '#0e221d'
TEXT, TEXT_HI = C('#f6e8c8'), C('#fffbe8')


def A(h, a=255):
    return C(h, a) if isinstance(h, str) else (h[0], h[1], h[2], a)


def rr(x, y, w, h, r):
    """Profundidad del centro del píxel dentro de un rectángulo redondeado [0,w)×[0,h) y su normal hacia fuera."""
    px, py = x + 0.5, y + 0.5
    cx, cy = min(max(px, r), w - r), min(max(py, r), h - r)
    qx, qy = px - cx, py - cy
    if qx and qy:
        dist = math.hypot(qx, qy)
        return r - dist, (qx / dist, qy / dist)
    edges = ((px, (-1, 0)), (w - px, (1, 0)), (py, (0, -1)), (h - py, (0, 1)))
    d, n = min(edges, key=lambda e: e[0])
    return d, n


def lit(n):
    """1 = cara a la luz (arriba a la izquierda), -1 = a la sombra, 0 = de lado."""
    v = -(n[0] * 0.55 + n[1] * 0.85)
    return 1 if v > 0.25 else (-1 if v < -0.25 else 0)


def vesicle(cv, cx, cy, r, ramp=GLASS, alpha=235):
    """Vesícula pegada a la membrana: esfera translúcida con su contorno, encima de lo que haya."""
    v = Canvas(cv.w, cv.h)
    ellipse(v, cx, cy, r, r, ramp, alpha=alpha)
    v.outline(); v.clean()
    hx, hy = int(cx - r * 0.45), int(cy - r * 0.45)
    if (hx, hy) in v.px and v.px[(hx, hy)] != OUTL: v.put(hx, hy, A(ramp[3]))
    cv.px.update(v.px)


# --- panel (9-slice 52×52, borde 14, modo Tiled: el centro mide 24 y el punteado tiene periodo 2) -----
PANEL_W, PANEL_R, PANEL_B = 52, 10, 14

def panel():
    cv = Canvas(PANEL_W)
    for y in range(PANEL_W):
        for x in range(PANEL_W):
            d, n = rr(x, y, PANEL_W, PANEL_W, PANEL_R)
            if d <= 0: continue
            k, l = int(d), lit(n)
            if k == 0: c = OUTL
            elif k == 1: c = A(MEM[3] if l > 0 else MEM[2] if l == 0 else MEM[1])
            elif k == 2: c = A(MEM[1])
            elif k == 3: c = A(MEM[2] if l >= 0 else MEM[1]) if (x + y) % 2 == 0 else A(MEM[1], 225)   # cabezas de lípidos
            elif k == 4: c = A(MEM[0], 200)
            elif k == 5: c = A(MEM[0], 175)
            else: c = A(FILL, 155)
            cv.put(x, y, c)
    # yemas en las esquinas (dentro de los 14 px de borde para que el 9-slice no las repita)
    for cx, cy, r in ((5.0, 9.0, 3.0), (10.5, 4.2, 1.9), (46.5, 44.0, 2.6), (42.0, 48.8, 1.6)):
        vesicle(cv, cx, cy, r)
    return cv


# --- botón célula (9-slice 42×17, bordes L9 R9; la píldora va de (1,1) a (41,16)) --------------------
BTN_W, BTN_H, BTN_B = 42, 17, 9
BTN_STATES = {
    #           borde arriba, borde abajo, capa interna, interior,          brillo arriba,    halo
    'Normal':      (MEM[3],    MEM[1],      A(MEM[1]),  A(MEM[0], 225),    A(MEM[1], 225),   None),
    'Highlighted': (MEM_HI[3], MEM_HI[1],   A(AQUA[0]), A(MEM_HI[0], 235), A(AQUA[1], 235),  A(AQUA[2], 90)),
    'Pressed':     (MEM[0],    MEM[2],      A(MEM[0]),  A(FILL, 240),      A(FILL, 240),     A(AQUA[2], 60)),
}

def button(state):
    top, bottom, inner, body, shine, halo = BTN_STATES[state]
    cv = Canvas(BTN_W, BTN_H)
    for y in range(BTN_H):
        for x in range(BTN_W):
            d, n = rr(x - 1, y - 1, BTN_W - 2, BTN_H - 2, (BTN_H - 2) / 2)
            if d <= 0:
                if halo and d > -1: cv.put(x, y, halo)
                continue
            k = int(d)
            if k == 0: c = OUTL
            elif k == 1: c = A(top if n[1] < -0.3 else bottom if n[1] > 0.3 else (top if n[0] < 0 else bottom))
            elif k == 2: c = inner
            elif y == 4: c = shine
            else: c = body
            cv.put(x, y, c)
    return cv


# --- núcleo del botón (13×13, centro (6,6); el código lo pone con su centro en (9,8) del botón) ------
def nucleus(on, phase=0):
    cv = Canvas(13)
    r = 3.0 + (0.4 if on and phase == 2 else 0)
    ellipse(cv, 6, 6, r, r, LIT if on else DIM)
    cv.outline(); cv.clean()
    cv.put(7, 7, A(AQUA[0] if on else DIM[0]))                     # nucléolo
    if on:
        cv.put(5, 5, SPARK[1])
        a = (40, 85, 130, 85)[phase]
        for y in range(13):
            for x in range(13):
                dd = math.hypot(x + 0.5 - 6.5, y + 0.5 - 6.5)
                if 4.3 < dd < 5.6: cv.put_out(x, y, A(LIT[2], a))
    return cv


# --- cinta del título (9-slice 64×22, bordes L20 R20): membrana estirada entre dos células --------------
TITLE_W, TITLE_H, TITLE_B = 64, 22, 20
BAND = {5: OUTL, 6: A(AQUA[3]), 7: A(AQUA[2]), 14: A(AQUA[0]), 15: A(MEM[1]), 16: OUTL}

def cell(cv, cx, cy):
    c = Canvas(cv.w, cv.h)
    ellipse(c, cx, cy, 6.6, 7.0, GLASS, alpha=245)
    c.outline(); c.clean()
    for y in range(cv.h):                                            # membrana doble: anillo interior más oscuro
        for x in range(cv.w):
            dd = math.hypot((x + 0.5 - cx) / 6.6, (y + 0.5 - cy) / 7.0)
            if 0.66 < dd < 0.8 and (x, y) in c.px and c.px[(x, y)] != OUTL:
                c.put(x, y, A(GLASS[1]))
    ellipse(c, cx + 0.8, cy + 0.8, 2.2, 2.0, MEM)                    # núcleo
    c.put(int(cx + 1), int(cy + 1), A(MEM[0]))
    c.put(int(cx - 3), int(cy + 2), A(AQUA[1])); c.put(int(cx + 2), int(cy - 3), A(AQUA[1]))   # orgánulos
    c.put(int(cx - 3), int(cy - 4), A(GLASS[3]))                    # brillo
    cv.px.update(c.px)


def title():
    cv = Canvas(TITLE_W, TITLE_H)
    for x in range(4, TITLE_W - 4):
        edge = min(x, TITLE_W - 1 - x)
        flare = int(min(3, max(0, 19 - edge) * 0.5))                 # la membrana se ensancha al llegar a la célula
        for y in range(5 - flare, 17 + flare):
            if y == 5 - flare: c = OUTL
            elif y == 16 + flare: c = OUTL
            elif y < 5: c = A(AQUA[3])
            elif y > 16: c = A(MEM[1])
            else: c = BAND.get(y, A(AQUA[1]))
            cv.put(x, y, c)
    cell(cv, 9.5, 11.0)
    cell(cv, TITLE_W - 9.5, 11.0)
    return cv


# --- 9-slice y maqueta -----------------------------------------------------------------------------
def _span(src0, src1, dst0, dst1, tiled):
    """Trozos (origen, destino) de un tramo: estirado (Sliced) o repetido (Tiled)."""
    if not tiled or src1 - src0 <= 0: return [(src0, src1, dst0, dst1)]
    out, d, n = [], dst0, src1 - src0
    while d < dst1:
        k = min(n, dst1 - d); out.append((src0, src0 + k, d, d + k)); d += k
    return out


def slice9(im, w, h, L, T, R, B, tiled=True):
    """9-slice como Unity: Tiled repite bordes y centro (el punteado de la membrana no se estira)."""
    im = as_img(im); W, H = im.size
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    xs = [(0, L, 0, L)] + _span(L, W - R, L, w - R, tiled) + [(W - R, W, w - R, w)]
    ys = [(0, T, 0, T)] + _span(T, H - B, T, h - B, tiled) + [(H - B, H, h - B, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if sx1 > sx0 and sy1 > sy0 and dx1 > dx0 and dy1 > dy0:
                out.alpha_composite(im.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.NEAREST), (dx0, dy0))
    return out


def font(bold=False):
    return ImageFont.truetype(os.path.join(FONTS, 'Silkscreen-Bold.ttf' if bold else 'Silkscreen-Regular.ttf'), 8)


def text(im, s, cx, cy, color, bold=False):
    d = ImageDraw.Draw(im); d.fontmode = '1'; f = font(bold)
    x0, y0, x1, y1 = d.textbbox((0, 0), s, font=f)
    _, ry0, _, ry1 = d.textbbox((0, 0), 'A', font=f)                 # altura fija: las tildes no mueven la línea
    x, y = round(cx - (x1 - x0) / 2 - x0), round(cy - (ry1 - ry0) / 2 - ry0)
    d.text((x, y + 1), s, font=f, fill=OUTL)
    d.text((x, y), s, font=f, fill=color)
    return x1 - x0


def game_backdrop():
    """Fondo de juego inventado (suelo del piso 1 con burbujas y algún bicho) y el velo de la pausa encima."""
    import random
    rnd = random.Random(7)
    im = Image.new('RGBA', (480, 270), C('#16352e')); d = ImageDraw.Draw(im)
    d.rectangle((0, 0, 480, 52), fill=C('#120f1c'))
    for x in range(0, 480, 6): d.line((x, 52, x, 58 + (x * 7) % 5), fill=C('#2a1a2a'))
    for _ in range(70):
        x, y, r = rnd.randrange(480), rnd.randrange(60, 270), rnd.choice((2, 3, 4))
        d.ellipse((x - r, y - r, x + r, y + r), outline=C('#1f483e'))
    for x, y in ((150, 205), (330, 120), (90, 150), (400, 230)):
        d.ellipse((x - 7, y - 5, x + 7, y + 5), fill=C('#4a2a20'), outline=OUTL)
    im.alpha_composite(Image.new('RGBA', (480, 270), (8, 16, 14, 150)))
    return im


ROOT_ITEMS = ['REANUDAR', 'CONFIGURACIÓN', 'PERSONAJE', 'BESTIARIO', 'SALIR']

def root_mockup(selected, phase, pressed=False):
    im = game_backdrop()
    pw, ph = 128, 136
    px, py = 240 - pw // 2, 72
    im.alpha_composite(slice9(panel(), pw, ph, PANEL_B, PANEL_B, PANEL_B, PANEL_B), (px, py))
    tw = 30 + 2 * TITLE_B + 8
    im.alpha_composite(slice9(title(), tw, TITLE_H, TITLE_B, 0, TITLE_B, 0), (240 - tw // 2, py - 11))
    text(im, 'PAUSA', 240, py, TEXT_HI, bold=True)
    bw = 104
    for i, s in enumerate(ROOT_ITEMS):
        bx, by = 240 - bw // 2 - 1, py + 20 + i * 22
        state = ('Pressed' if pressed else 'Highlighted') if i == selected else 'Normal'
        im.alpha_composite(slice9(button(state), bw + 2, BTN_H, BTN_B, 0, BTN_B, 0), (bx, by))
        if i == selected: im.alpha_composite(as_img(nucleus(True, phase)), (bx + 9 - 6, by + 8 - 6))   # solo al resaltar
        off = 1 if pressed and i == selected else 0
        text(im, s, bx + 1 + bw / 2, by + 8 + off, TEXT_HI if i == selected else TEXT)
    return im


def pieces_board(scale=4):
    """Tablero de piezas ampliadas: panel, cinta, botones en sus 3 estados y los cuadros del núcleo."""
    items = [('Panel', panel()), ('Titulo', title())] + [(s, button(s)) for s in BTN_STATES] + \
            [('Nucleo on %d' % i, nucleus(True, i)) for i in range(4)]
    W = 1460; x, y, rowh = 12, 12, 0
    im = Image.new('RGBA', (W, 260), BG); d = ImageDraw.Draw(im)
    for name, f in items:
        big = as_img(f).resize((as_img(f).width * scale, as_img(f).height * scale), Image.NEAREST)
        if x + big.width > W - 12: x, y, rowh = 12, y + rowh + 22, 0
        im.alpha_composite(big, (x, y + 14)); d.text((x, y), name, fill=C('#bfe6ff'), font=pixel_font())
        x += big.width + 18; rowh = max(rowh, big.height)
    return im.crop((0, 0, W, y + rowh + 26))


def pixel_font():
    from pixel import _font
    return _font()


if __name__ == '__main__':
    sheets = {
        'UI_PauseMem_Panel': [panel()],
        'UI_PauseMem_Title': [title()],
        'UI_PauseMem_Button_Normal': [button('Normal')],
        'UI_PauseMem_Button_Highlighted': [button('Highlighted')],
        'UI_PauseMem_Button_Pressed': [button('Pressed')],
        'UI_PauseMem_Nucleus_On': [nucleus(True, i) for i in range(4)],
    }
    os.makedirs(os.path.join(OUT, 'PauseMem'), exist_ok=True)
    for name, frames in sheets.items():
        save_sheet(os.path.join(OUT, 'PauseMem', name + '.png'), frames)

    board = pieces_board()
    full = root_mockup(2, 2)
    mock = full.resize((960, 540), Image.NEAREST)
    zoom = full.crop((160, 54, 320, 214)).resize((480, 480), Image.NEAREST)
    prev = Image.new('RGBA', (max(board.width, 1460), board.height + 560), BG)
    prev.alpha_composite(board, (0, 0)); prev.alpha_composite(mock, (10, board.height + 10))
    prev.alpha_composite(zoom, (980, board.height + 10))
    prev.save(os.path.join(OUT, 'vista_previa_pausa_membrana.png'))

    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    frames, ms = [], []
    for sel in range(len(ROOT_ITEMS)):
        for k in range(8):
            frames.append(root_mockup(sel, k % 4)); ms.append(150)
        if sel == 2:
            frames.append(root_mockup(sel, 2, pressed=True)); ms.append(220)
    frames = [f.resize((960, 540), Image.NEAREST).convert('RGB') for f in frames]
    frames[0].save(os.path.join(OUT, 'gif', 'Pausa_Raiz.gif'), save_all=True, append_images=frames[1:], duration=ms, loop=0)
    print('Listo en', OUT)
