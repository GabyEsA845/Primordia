"""Menú de pausa nuevo, estilo membrana (subtanda 4): Configuración y Salir.

Deslizador de volumen (filamento de membrana, relleno de citoplasma y núcleo como tirador), interruptor del temblor,
icono de temblor, botón de peligro (rojizo, para confirmar la salida) y maquetas de Configuración, Salir y la
ventana de confirmación. Reutiliza las subtandas 1-3 (en esta carpeta).
Hecho con la skill primordia-sprites (pixel.py): luz arriba a la izquierda, contorno OUTL.

Uso: python generar_pausa_opciones.py [carpeta_salida]
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import generar_pausa_membrana as M
from generar_pausa_detalles import *
from generar_pausa_detalles import _measure

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__))
DANGER = ['#3a1418', '#6a2430', '#a84050', '#e88a8a']
DANGER_HI = ['#5a1c26', '#8a3040', '#d05a68', '#ffc8c0']
DANGER_LIT = ['#a83040', '#e05a5a', '#ff9a8a', '#fff0e0']


# --- deslizador (9-slice 24×9, bordes L5 R5, Sliced; el relleno va dentro de la barra con la misma forma) ---
SL_W, SL_H, SL_B = 24, 9, 5

def _pill(w, h, color_of):
    cv = Canvas(w, h)
    for y in range(h):
        for x in range(w):
            d, n = rr(x, y, w, h, h / 2)
            if d > 0:
                c = color_of(int(d), n, x, y)
                if c: cv.put(x, y, c)
    return cv

def slider_track():
    def col(k, n, x, y):
        if k == 0: return OUTL
        if k == 1: return A(MEM[2] if n[1] > 0.3 else MEM[1])          # tubo: la luz entra por dentro, abajo
        return A(FILL, 230)
    return _pill(SL_W, SL_H, col)

def slider_fill():
    def col(k, n, x, y):
        if k == 0: return None                                         # deja ver el contorno de la barra
        if k == 1: return A(AQUA[0] if n[1] > 0.3 else AQUA[1])
        return A(AQUA[2] if y == 2 else AQUA[1])
    return _pill(SL_W, SL_H, col)


def knob(active, phase=0):
    """Tirador: el núcleo de los botones. Quieto = encendido sin halo; agarrado o resaltado = late con halo."""
    if active: return nucleus(True, phase)
    cv = Canvas(13)
    ellipse(cv, 6, 6, 3.0, 3.0, LIT)
    cv.outline(); cv.clean()
    cv.put(7, 7, A(AQUA[0])); cv.put(5, 5, A(LIT[3]))
    return cv


# --- interruptor del temblor (24×13): píldora con el núcleo dentro ------------------------------------
TG_W, TG_H = 24, 13

def toggle(on):
    def col(k, n, x, y):
        if k == 0: return OUTL
        if k == 1: return A((MEM_HI[3] if on else MEM[3]) if n[1] < -0.3 else (MEM_HI[1] if on else MEM[1]))
        return A(AQUA[0] if on else FILL, 230)
    cv = _pill(TG_W, TG_H, col)
    nx = TG_W - 7 if on else 6
    nuc = Canvas(TG_W, TG_H)
    ellipse(nuc, nx + 0.5, 6.5, 3.2, 3.2, LIT if on else DIM)
    nuc.outline(); nuc.clean()
    nuc.put(nx + 1, 7, A(AQUA[0] if on else DIM[0]))
    if on: nuc.put(nx - 1, 5, SPARK[1])
    cv.px.update(nuc.px)
    return cv


# --- icono de temblor (12×12): una pantalla que vibra -----------------------------------------------
def icon_shake():
    cv = Canvas(12)
    for y in range(3, 9):
        for x in range(3, 9):
            edge = x in (3, 8) or y in (3, 8)
            cv.put(x, y, A(GLASS[3] if edge and (x == 3 or y == 3) else GLASS[1] if edge else GLASS[2] if x + y < 10 else GLASS[1]))
    cv.outline(); cv.clean()
    for x, y in ((0, 4), (1, 5), (0, 6), (11, 5), (10, 6), (11, 7)):  # rayas de vibración
        cv.put(x, y, A(AMBER[2]))
    cv.put(4, 4, A(GLASS[3]))
    return cv


# --- botón de peligro (mismo tamaño y bordes que el botón célula) y su núcleo rojo -------------------
DANGER_STATES = {
    'Normal':      (DANGER[3],    DANGER[1],    A(DANGER[1]),    A('#2a1216', 225), A(DANGER[1], 225), None),
    'Highlighted': (DANGER_HI[3], DANGER_HI[1], A(DANGER_HI[1]), A('#3a161c', 235), A(DANGER[2], 235), A(DANGER_LIT[2], 90)),
    'Pressed':     (DANGER[0],    DANGER[2],    A(DANGER[0]),    A('#1a0a0c', 240), A('#1a0a0c', 240), A(DANGER_LIT[2], 60)),
}

def button_danger(state):
    saved = dict(M.BTN_STATES)
    M.BTN_STATES.update(DANGER_STATES)
    try: return M.button(state)
    finally: M.BTN_STATES.clear(); M.BTN_STATES.update(saved)

def nucleus_danger(phase):
    saved = M.LIT[:]
    M.LIT[:] = DANGER_LIT
    try: return nucleus(True, phase)
    finally: M.LIT[:] = saved


# --- maquetas ---------------------------------------------------------------------------------------
def _title(im, label, cy):
    tw = _measure(label, True) + 2 * TITLE_B + 10
    im.alpha_composite(slice9(title(), tw, TITLE_H, TITLE_B, 0, TITLE_B, 0), (240 - tw // 2, cy - 11))
    text(im, label, 240, cy, TEXT_HI, bold=True)


def _button(im, label, cx, y, w, state='Normal', danger=False, phase=0):
    b = button_danger(state) if danger else M.button(state)
    bx = cx - w // 2 - 1
    im.alpha_composite(slice9(b, w + 2, BTN_H, BTN_B, 0, BTN_B, 0), (bx, y))
    if state != 'Normal':
        nuc = nucleus_danger(phase) if danger else nucleus(True, phase)
        im.alpha_composite(as_img(nuc), (bx + 9 - 6, y + 8 - 6))
    text(im, label, cx, y + 8 + (1 if state == 'Pressed' else 0), TEXT_HI if state != 'Normal' else TEXT)


def _slider(im, x, cy, w, value, active, phase):
    im.alpha_composite(slice9(slider_track(), w, SL_H, SL_B, 0, SL_B, 0, tiled=False), (x, cy - 4))
    fw = max(SL_B * 2, int(w * value))
    im.alpha_composite(slice9(slider_fill(), fw, SL_H, SL_B, 0, SL_B, 0, tiled=False), (x, cy - 4))
    kx = x + int((w - 8) * value) + 4
    im.alpha_composite(as_img(knob(active, phase)), (kx - 6, cy - 6))


def config_mockup(frame, selected=0, music=0.7, shake=True):
    im = game_backdrop()
    pw, ph = 236, 128
    px, py = 240 - pw // 2, 74
    im.alpha_composite(slice9(panel(), pw, ph, PANEL_B, PANEL_B, PANEL_B, PANEL_B), (px, py))
    _title(im, 'CONFIGURACIÓN', py)
    x0 = px + 16
    rowsy = [py + 24, py + 44, py + 64]
    strip = as_img(row_strip())
    for j, y in enumerate(rowsy):
        im.alpha_composite(slice9(strip, pw - 32, 14, 6, 0, 6, 0, tiled=False), (x0, y))
    music_ic, sfx_ic = load('menus/Pause/UI_Stat_Music.png'), load('menus/Pause/UI_Stat_Sfx.png')
    for j, (ic, label, val) in enumerate(((music_ic, 'MÚSICA', music), (sfx_ic, 'EFECTOS', 0.45))):
        y = rowsy[j]
        im.alpha_composite(ic, (x0 + 4, y + 1)); P2_left(im, label, x0 + 20, y + 7, TEXT_HI if selected == j else TEXT)
        _slider(im, x0 + 84, y + 7, 92, val, selected == j, frame % 4)
        P2_right(im, '%d' % round(val * 100), x0 + pw - 38, y + 7, TEXT_HI)
    y = rowsy[2]
    im.alpha_composite(as_img(icon_shake()), (x0 + 4, y + 1)); P2_left(im, 'TEMBLOR', x0 + 20, y + 7, TEXT_HI if selected == 2 else TEXT)
    im.alpha_composite(as_img(toggle(shake)), (x0 + 84, y + 1))
    P2_left(im, 'SÍ' if shake else 'NO', x0 + 114, y + 7, C('#9ec8b4'))
    _button(im, 'VOLVER', 240, py + ph - 28, 104, 'Highlighted' if selected == 3 else 'Normal', phase=frame % 4)
    return im


def exit_mockup(frame, selected=0, confirm=False, confirm_sel=1):
    """Salir; al elegir una opción, la confirmación SUSTITUYE al panel (encima se transparentaría)."""
    im = game_backdrop()
    if not confirm:
        pw, ph = 128, 92
        px, py = 240 - pw // 2, 84
        im.alpha_composite(slice9(panel(), pw, ph, PANEL_B, PANEL_B, PANEL_B, PANEL_B), (px, py))
        _title(im, 'SALIR', py)
        for i, label in enumerate(('REINICIAR', 'MENÚ PRINCIPAL', 'VOLVER')):
            _button(im, label, 240, py + 20 + i * 22, 104, 'Highlighted' if selected == i else 'Normal', phase=frame % 4)
        return im
    cw, ch = 196, 90
    cx, cy = 240 - cw // 2, 88
    im.alpha_composite(slice9(panel(), cw, ch, PANEL_B, PANEL_B, PANEL_B, PANEL_B), (cx, cy))
    _title(im, 'MENÚ PRINCIPAL', cy)
    text(im, '¿SEGURO?', 240, cy + 24, TEXT_HI, bold=True)
    text(im, 'PERDERÁS LA PARTIDA EN CURSO', 240, cy + 38, C('#9ec8b4'))
    _button(im, 'SÍ, SALIR', 240 - 44, cy + 56, 80, 'Highlighted' if confirm_sel == 0 else 'Normal', danger=True, phase=frame % 4)
    _button(im, 'NO', 240 + 44, cy + 56, 80, 'Highlighted' if confirm_sel == 1 else 'Normal', phase=frame % 4)
    return im


def P2_left(im, s, x, cy, color, bold=False):
    text(im, s, x + _measure(s, bold) / 2, cy, color, bold)

def P2_right(im, s, x, cy, color, bold=False):
    text(im, s, x - _measure(s, bold) / 2, cy, color, bold)


if __name__ == '__main__':
    sheets = {
        'UI_PauseMem_Slider_Track': [slider_track()],
        'UI_PauseMem_Slider_Fill': [slider_fill()],
        'UI_PauseMem_Slider_Knob': [knob(False)],
        'UI_PauseMem_Toggle_Off': [toggle(False)],
        'UI_PauseMem_Toggle_On': [toggle(True)],
        'UI_PauseMem_StatIcon_Shake': [icon_shake()],
        'UI_PauseMem_Button_Danger_Normal': [button_danger('Normal')],
        'UI_PauseMem_Button_Danger_Highlighted': [button_danger('Highlighted')],
        'UI_PauseMem_Button_Danger_Pressed': [button_danger('Pressed')],
        'UI_PauseMem_Nucleus_Danger_On': [nucleus_danger(i) for i in range(4)],
    }
    for name, frames in sheets.items():
        save_sheet(os.path.join(OUT, 'PauseMem', name + '.png'), frames)
    # el tirador agarrado usa UI_PauseMem_Nucleus_On (ya existe)

    preview(os.path.join(OUT, '_piezas4_tmp.png'), sheets, BG, scale=5)
    with Image.open(os.path.join(OUT, '_piezas4_tmp.png')) as _b: board = _b.convert('RGBA')
    os.remove(os.path.join(OUT, '_piezas4_tmp.png'))
    shots = [config_mockup(2, 0), exit_mockup(2, 1), exit_mockup(2, 1, confirm=True, confirm_sel=0), exit_mockup(2, 1, confirm=True, confirm_sel=1)]
    shots = [s.resize((720, 405), Image.NEAREST) for s in shots]
    prev = Image.new('RGBA', (max(board.width, 1460), board.height + 830), BG)
    prev.alpha_composite(board, (0, 0))
    for i, s in enumerate(shots): prev.alpha_composite(s, (10 + (i % 2) * 730, board.height + 10 + (i // 2) * 415))
    prev.save(os.path.join(OUT, 'vista_previa_pausa_opciones.png'))

    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    frames, ms = [], []
    for k in range(12):                                     # se arrastra el volumen de la música
        frames.append(config_mockup(k, 0, music=0.7 - 0.03 * k)); ms.append(110)
    for k in range(8):
        frames.append(config_mockup(k, 2, music=0.34, shake=k < 4)); ms.append(160)
    for k in range(8):
        frames.append(exit_mockup(k, 1)); ms.append(140)
    for k in range(8):
        frames.append(exit_mockup(k, 1, confirm=True, confirm_sel=0 if k < 4 else 1)); ms.append(180)
    frames = [f.resize((960, 540), Image.NEAREST).convert('RGB') for f in frames]
    frames[0].save(os.path.join(OUT, 'gif', 'Pausa_Opciones.gif'), save_all=True, append_images=frames[1:], duration=ms, loop=0)
    print('Listo en', OUT)
