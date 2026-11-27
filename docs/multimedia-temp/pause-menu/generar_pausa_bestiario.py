"""Menú de pausa nuevo, estilo membrana (subtanda 5a): marco del bestiario.

Casillas de 34×34 (retrato de 24 px dentro) en tres estados, iconos de pestaña por piso, vacuola de la ficha y
maqueta de la pantalla (pestaña Piso 3 con una criatura elegida y otra bloqueada). Los retratos de la maqueta son
provisionales: recortes de los sprites del juego hasta que se dibujen los de verdad (tandas siguientes).
Reutiliza las subtandas 1-4 (en esta carpeta).
Hecho con la skill primordia-sprites (pixel.py): luz arriba a la izquierda, contorno OUTL.

Uso: python generar_pausa_bestiario.py [carpeta_salida]
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import generar_pausa_membrana as M
import generar_pausa_personaje as P
from generar_pausa_opciones import *
from generar_pausa_detalles import _measure

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__))
SHELL = ['#5a3a3a', '#a86a5a', '#e0a888', '#fbe0cc']
FERN = ['#1e4a24', '#3a7a34', '#6aae4a', '#b4e08a']


# --- casilla del bestiario (34×34; membrana de (1,1) a (33,33); retrato de 24 centrado) ------------------
BS_W = 34

def beast_slot(state):
    rim = {'Normal': MEM, 'Highlighted': MEM_HI, 'Locked': DIM}[state]
    cv = Canvas(BS_W)
    for y in range(BS_W):
        for x in range(BS_W):
            d, n = rr(x - 1, y - 1, BS_W - 2, BS_W - 2, 7)
            if d <= 0:
                if state == 'Highlighted' and d > -1: cv.put(x, y, A(AQUA[2], 90))
                continue
            k, l = int(d), lit(n)
            if k == 0: c = OUTL
            elif k == 1: c = A(rim[3] if l > 0 else rim[2] if l == 0 else rim[1])
            elif k == 2: c = A(rim[1])
            elif k == 3: c = A(rim[2] if l >= 0 else rim[1]) if (x + y) % 2 == 0 else A(rim[1], 225)
            else: c = A(MEM_HI[0] if state == 'Highlighted' else '#0a1a16' if state == 'Locked' else FILL, 210)
            cv.put(x, y, c)
    if state != 'Locked': vesicle(cv, 4.2, 4.2, 2.2)
    return cv


# --- iconos de pestaña por piso (12×12) ---------------------------------------------------------------
def icon_floor1():
    """Piso 1, el caldo primordial: una célula translúcida con su núcleo."""
    cv = Canvas(12)
    ellipse(cv, 6, 6, 4.8, 4.4, GLASS, alpha=235)
    cv.outline(); cv.clean()
    ellipse(cv, 7, 7, 1.8, 1.6, MEM)
    cv.put(3, 4, A(GLASS[3])); cv.put(4, 8, A(AQUA[2]))
    return cv

def icon_floor2():
    """Piso 2, el océano: una concha en espiral."""
    cv = Canvas(12)
    ellipse(cv, 6, 6.5, 4.8, 4.4, SHELL)
    cv.outline(); cv.clean()
    for a in range(0, 720, 12):                                       # espiral de la concha (amonites)
        t = math.radians(a); r = 0.4 + a / 720 * 4.2
        cv.put_in(6 + math.cos(t) * r, 6.5 + math.sin(t) * r, A(SHELL[0]))
    for a in range(200, 720, 60):                                     # costillas
        t = math.radians(a); r = 0.4 + a / 720 * 4.2
        cv.put_in(6 + math.cos(t) * (r + 0.9), 6.5 + math.sin(t) * (r + 0.9), A(SHELL[1]))
    cv.put(3, 4, A(SHELL[3]))
    return cv

def icon_floor3():
    """Piso 3, el pantano: una fronda de helecho."""
    cv = Canvas(12)
    for i in range(10):                                               # tallo curvo
        x, y = 3 + i * 0.7, 10 - i * 0.95 + (i * i) * 0.02
        cv.put(x, y, A(FERN[1]))
        if 1 <= i <= 8:
            ln = 3 - abs(i - 4) * 0.45
            for k in range(1, int(ln) + 1):
                cv.put(x - k, y - k * 0.4, A(FERN[2] if k < 2 else FERN[1]))
                cv.put(x + k * 0.6, y + k * 0.7 - 0.3, A(FERN[1] if k < 2 else FERN[0]))
    cv.outline(); cv.clean()
    cv.put(9, 1, A(FERN[3])); cv.put(4, 6, A(FERN[3]))
    return cv

BEAST_TABS = [(icon_floor1, 'PISO 1'), (icon_floor2, 'PISO 2'), (icon_floor3, 'PISO 3'), (icon_bosses, 'JEFES')]


# --- vacuola de la ficha (96×96, interior de radio ~40) -------------------------------------------------
VR, VC = 46.0, 48.0

def beast_vacuole():
    cv = Canvas(96)
    for y in range(96):
        for x in range(96):
            dx, dy = x + 0.5 - VC, y + 0.5 - VC
            r = math.hypot(dx, dy); d = VR - r
            if d <= 0: continue
            k = int(d); l = lit((dx / (r or 1), dy / (r or 1)))
            if k == 0: c = OUTL
            elif k == 1: c = A(MEM[3] if l > 0 else MEM[2] if l == 0 else MEM[1])
            elif k == 2: c = A(MEM[1])
            elif k == 3: c = A(MEM[2] if l >= 0 else MEM[1]) if (x + y) % 2 == 0 else A(MEM[1], 230)
            elif k == 4: c = A(MEM[0])
            elif k < 8: c = A(GLASS[0], 190)
            else: c = A(GLASS[0] if -(dx * 0.55 + dy * 0.85) / 40 < 0.1 else '#22524c', 170)
            cv.put(x, y, c)
    for ang, rr_ in ((212, 3.2), (228, 1.8), (330, 2.6)):
        a = math.radians(ang)
        vesicle(cv, VC + math.cos(a) * (VR - 1), VC + math.sin(a) * (VR - 1), rr_)
    return cv

def beast_glint():
    cv = Canvas(96)
    for y in range(96):
        for x in range(96):
            dx, dy = x + 0.5 - VC, y + 0.5 - VC
            r = math.hypot(dx, dy); a = math.degrees(math.atan2(dy, dx))
            if 34 < r < 36.5 and -160 < a < -114: cv.put(x, y, A(GLASS[3], 140 if r < 35.5 else 80))
    return cv


# --- maqueta ------------------------------------------------------------------------------------------
F3 = 'floor3-enemies'
BEASTS = [  # (carpeta/hoja, nombre, descubierto)
    ('Beetle/Beetle_Idle', 'ESCARABAJO', True), ('Spider/Spider_Idle', 'ARAÑA', True), ('Toad/Toad_Idle', 'SAPO', True),
    ('Calamites/Calamites_Idle', 'CALAMITES', True), ('CalamitesSprout/CalamitesSprout_Idle', 'BROTE', True),
    ('Medullosa/Medullosa_Idle', 'MEDULLOSA', False), ('Dragonfly/Dragonfly_Fly', 'LIBÉLULA', True),
    ('../floor3-bosses/meganeura/Sheets/Meganeura_Nymph_Idle', 'NINFA', False),
    ('CarnivorousPlant/CarnivorousPlant_Idle', 'PLANTA CARNÍVORA', True), ('Mayflies/Mayflies_Idle', 'EFÍMERAS', False),
]

def sheet_frames(rel):
    im = load('%s/%s.png' % (F3, rel)); h = im.height
    return [im.crop((i * h, 0, i * h + h, h)) for i in range(max(1, im.width // h))]

def portrait_placeholder(rel):
    """Retrato provisional: el centro del primer cuadro, recortado a 24×24."""
    f = sheet_frames(rel)[0]
    bb = f.getbbox() or (0, 0, f.width, f.height)
    cx, cy = (bb[0] + bb[2]) // 2, (bb[1] + bb[3]) // 2
    return f.crop((cx - 12, cy - 12, cx + 12, cy + 12))

def silhouette(im):
    im = im.copy(); p = im.load()
    for y in range(im.height):
        for x in range(im.width):
            if p[x, y][3] > 60: p[x, y] = (42, 74, 66, 255)          # silueta: algo más clara que el fondo bloqueado
    return im


CARD = None          # (nombre, [líneas], (vida, daño, veloc., eliminados), criatura 24×24) para las maquetas de los retratos
TRAPS_FROM = 8       # índice desde el que van las trampas y plantas

def bestiary_mockup(frame, sel=0, tab=2, hover=None):
    im = game_backdrop()
    im.alpha_composite(slice9(panel(), PW, PH, PANEL_B, PANEL_B, PANEL_B, PANEL_B), (PX, PY))
    label = 'BESTIARIO  23/38'
    tw = _measure(label, True) + 2 * TITLE_B + 10
    im.alpha_composite(slice9(title(), tw, TITLE_H, TITLE_B, 0, TITLE_B, 0), (240 - tw // 2, 6))
    text(im, label, 240, 17, TEXT_HI, bold=True)
    # pestañas en yema
    widths = [P.tab_width(l) for _, l in BEAST_TABS]
    x = PX + (PW - sum(widths) + 2 * (len(widths) - 1)) // 2; x += (x - PX) % 2
    first = x
    for i, (icf, lab) in enumerate(BEAST_TABS):
        w = widths[i]
        spr = P.tab_active() if i == tab else P.tab_bud(hover == i)
        im.alpha_composite(slice9(spr, w, P.TAB_H, P.TAB_B, 0, P.TAB_B, 0), (x, PY - P.TAB_EDGE))
        top = 0 if i == tab else P.IDLE_TOP
        cy = PY - P.TAB_EDGE + (top + 3 + P.TAB_EDGE) // 2
        ic = as_img(icf())
        if i != tab:
            px_ = ic.load()
            for yy in range(12):
                for xx in range(12):
                    r_, g_, b_, a_ = px_[xx, yy]
                    if a_: px_[xx, yy] = (int(r_ * .6 + 20), int(g_ * .6 + 30), int(b_ * .6 + 26), a_)
        im.alpha_composite(ic, (x + P.BODY_X0 + 4, cy - 6))
        text(im, lab, x + P.BODY_X0 + 19 + (w - P.BODY_X0 - 19 - P.BODY_X0 - 4) / 2, cy, TEXT_HI if i == tab else C('#9ec8b4'))
        x += w - 2
    im.alpha_composite(as_img(P.key('Q', False)), (first - 15, PY - 15))
    im.alpha_composite(as_img(P.key('E', False)), (x + 4, PY - 15))
    # cuadrícula: enemigos (8 = 5 + 3), y debajo la fila de trampas y plantas
    gx, gy, step = PX + 12, PY + 26, 34
    found = sum(1 for b in BEASTS if b[2])
    P2_left(im, 'DESCUBIERTOS %d/%d' % (found, len(BEASTS)), gx + 2, PY + 16, C('#ffd27a'))
    if len(BEASTS) > TRAPS_FROM: P2_left(im, 'TRAMPAS Y PLANTAS', gx + 2, gy + 2 * step + 10, C('#9ec8b4'))
    for i, (rel, name, ok) in enumerate(BEASTS):
        if i < TRAPS_FROM: cx, cy = gx + (i % 5) * step, gy + (i // 5) * step
        else: cx, cy = gx + (i - TRAPS_FROM) * step, gy + 2 * step + 18
        state = 'Highlighted' if i == sel else ('Normal' if ok else 'Locked')
        im.alpha_composite(as_img(beast_slot(state)), (cx, cy))
        if rel:
            pt = portrait_placeholder(rel)
            im.alpha_composite(pt if ok else silhouette(pt), (cx + 5, cy + 5))
    # ficha
    fx = PX + 188
    rel, name, ok = BEASTS[sel]
    vx, vy = fx + 33, PY + 8
    im.alpha_composite(as_img(beast_vacuole()), (vx, vy))
    if CARD:
        big = as_img(CARD[3]).resize((72, 72), Image.NEAREST)
        im.alpha_composite(big, (vx + 12, vy + 12))
    elif rel:
        frames = sheet_frames(rel); f = frames[frame % len(frames)]
        bb = f.getbbox() or (0, 0, f.width, f.height)
        s = max(1, min(3, 64 // max(bb[2] - bb[0], bb[3] - bb[1])))
        big = f.resize((f.width * s, f.height * s), Image.NEAREST)
        im.alpha_composite(big, (vx + 48 - big.width // 2, vy + 50 - big.height // 2))
    im.alpha_composite(as_img(beast_glint()), (vx, vy))
    if CARD: name, ok = CARD[0], True
    text(im, name if ok else '???', fx + 81, vy + 102, TEXT_HI, bold=True)
    notes = CARD[1] if CARD else ['ESCARABAJO DEL CARBONÍFERO.', 'ACORAZADO; EMBISTE CUANDO', 'SE SIENTE ACORRALADO.'] if ok else ['???']
    for j, ln in enumerate(notes): text(im, ln, fx + 81, vy + 113 + j * 10, C("#9ec8b4"))   # 10 px: deja sitio a las tildes
    strip = as_img(row_strip())
    stats = list(zip(('VIDA', 'DAÑO', 'VELOC.', 'ELIMINADOS'), CARD[2])) if CARD else [('VIDA', '4'), ('DAÑO', '1'), ('VELOC.', 'MEDIA'), ('ELIMINADOS', '37')]
    for j, (k, v) in enumerate(stats):
        ry = vy + 143 + (j // 2) * 16
        rx = fx + (j % 2) * 82
        im.alpha_composite(slice9(strip, 80, 14, 6, 0, 6, 0, tiled=False), (rx, ry))
        assert _measure(k) + _measure(v) + 9 <= 80, (k, v)
        P2_left(im, k, rx + 3, ry + 7, TEXT); P2_right(im, v if ok else '?', rx + 77, ry + 7, TEXT_HI)
    return im


if __name__ == '__main__':
    sheets = {
        'UI_PauseMem_BeastSlot_Normal': [beast_slot('Normal')],
        'UI_PauseMem_BeastSlot_Highlighted': [beast_slot('Highlighted')],
        'UI_PauseMem_BeastSlot_Locked': [beast_slot('Locked')],
        'UI_PauseMem_TabIcon_Floor1': [icon_floor1()],
        'UI_PauseMem_TabIcon_Floor2': [icon_floor2()],
        'UI_PauseMem_TabIcon_Floor3': [icon_floor3()],
        'UI_PauseMem_Beast_Vacuole': [beast_vacuole()],
        'UI_PauseMem_Beast_Glint': [beast_glint()],
    }
    for name, frames in sheets.items():
        save_sheet(os.path.join(OUT, 'PauseMem', name + '.png'), frames)
    # la pestaña Jefes usa UI_PauseMem_RunIcon_Bosses

    small = {k: v for k, v in sheets.items() if 'Beast_' not in k}
    preview(os.path.join(OUT, '_piezas5_tmp.png'), small, BG, scale=5)
    with Image.open(os.path.join(OUT, '_piezas5_tmp.png')) as _b: board = _b.convert('RGBA')
    os.remove(os.path.join(OUT, '_piezas5_tmp.png'))
    vac = Image.new('RGBA', (96, 96), C('#0b1512'))
    for f in (beast_vacuole(), beast_glint()): vac.alpha_composite(as_img(f))
    vac = vac.resize((288, 288), Image.NEAREST)
    full = bestiary_mockup(0, sel=0).resize((960, 540), Image.NEAREST)
    locked = bestiary_mockup(0, sel=5).resize((480, 270), Image.NEAREST)
    prev = Image.new('RGBA', (max(board.width + 300, 1460), max(board.height, 300) + 560), BG)
    prev.alpha_composite(board, (0, 0)); prev.alpha_composite(vac, (board.width + 6, 6))
    y = max(board.height, 300) + 10
    prev.alpha_composite(full, (10, y)); prev.alpha_composite(locked, (980, y))
    prev.save(os.path.join(OUT, 'vista_previa_pausa_bestiario.png'))

    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    frames, ms = [], []
    for sel in (0, 1, 2, 5):
        for k in range(8):
            frames.append(bestiary_mockup(k, sel=sel)); ms.append(150)
    frames = [f.resize((960, 540), Image.NEAREST).convert('RGB') for f in frames]
    frames[0].save(os.path.join(OUT, 'gif', 'Pausa_Bestiario.gif'), save_all=True, append_images=frames[1:], duration=ms, loop=0)
    print('Listo en', OUT)
