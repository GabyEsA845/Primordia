"""Cápsula de ámbar (piso 3): HALO con alas que levita sobre la cabeza del jugador (aspecto de la mutación antes de activarse).

Anillo de ámbar con pinchos pequeños arriba, una o dos gemas en el frente y dos alitas de resina dorada. Es un objeto aparte
(no una capa del cuerpo): el código lo coloca sobre la cabeza (centro del anillo ~5 px por encima del cráneo) y le suma la
levitación de la hoja. Al gastarse pasa a un tono apagado y quieto hasta que se repone la carga.
Hojas (celda 44x28; centro del anillo en (22, 18); el anillo es el pivote):
  Fx_AmberHalo_I            8 cuadros x 120 ms, bucle: levita, aletea, brillo que recorre el aro, motas que caen (1 gema)
  Fx_AmberHalo_III_Full     8, igual con 2 gemas encendidas (nivel III, 2 cargas)
  Fx_AmberHalo_III_Half     8, 2 gemas: una encendida y otra apagada (1 carga gastada)
  Fx_AmberHalo_Off_I / _III 1 cuadro: apagado (gemas oscuras, alas caídas, sin brillo ni movimiento)
  Fx_AmberHalo_Fade_I       4 cuadros x 90 ms: se apaga al activarse la mutación (el reverso = se repone; el código lo mezcla en III)
  Fx_AmberHalo_Glow         8 cuadros (56x36, centro (28, 20)): resplandor dorado tenue que respira, detrás del halo
Uso: python generar_halo.py
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
TEMP = os.path.normpath(os.path.join(HERE, '..'))
sys.path.insert(0, os.path.join(TEMP, 'floor3-environment'))
from base_piso3 import *
from PIL import Image

AMB = [C(c) for c in AMBER]
W, H, CX, CY = 44, 28, 22, 18
NF = 8
RX, RY, RIN_X, RIN_Y = 10.5, 3.8, 8.0, 2.0
OFF = [(150, 108, 74), (112, 78, 52), (86, 58, 40), (62, 42, 30), (44, 30, 24)]        # paleta apagada: hi, light, mid, dark, deep

def lerp(a, b, t): return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))
def pal(heat): return [lerp(OFF[i], tuple(AMB[4 - i][:3]), heat) for i in range(5)]     # hi, light, mid, dark, deep
def rgba(c, a=255): return tuple(c[:3]) + (a,)

def feather(cv, cx, cy, L, th, ang, P):
    """Pluma: elipse alargada con la punta hacia `ang`; luz arriba a la izquierda y raquis claro."""
    ca, sa = math.cos(ang), math.sin(ang)
    for y in range(int(cy - L), int(cy + L) + 2):
        for x in range(int(cx - L), int(cx + L) + 2):
            dx, dy = x + .5 - cx, y + .5 - cy; u = dx * ca + dy * sa; v = -dx * sa + dy * ca
            if (u / (L / 2)) ** 2 + (v / th) ** 2 <= 1:
                k = (-dx - dy) / L
                c = P[1] if k > .15 else P[2] if k > -.25 else P[3]
                if abs(v) < .5 and u > -L * .2: c = P[0]
                cv.put(x, y, rgba(c))

def gem(cv, x, y, heat, P):
    """Gema colgando del aro: rombo de 5 px, brillante si está encendida y oscura si está gastada."""
    for dy in range(-2, 3):
        h = 2 - abs(dy)
        for dx in range(-h, h + 1):
            d = abs(dx) + abs(dy)
            c = lerp(OFF[0], AMB[4][:3], heat) if d == 0 or (dx, dy) == (-1, -1) else lerp(OFF[1], AMB[3][:3], heat) if d <= 1 else lerp(OFF[3], AMB[1][:3], heat)
            cv.put(x + dx, y + dy, rgba(c))

def halo(f, gems, heat=1.0, droop=0.0, sweep=True, motes=True, bob=True):
    """gems: lista de 'calor' de cada gema (1 encendida .. 0 apagada). heat: calor general; droop 0 = alas arriba, 1 = caídas."""
    cv = Canvas(W, H); P = pal(heat); ph = 2 * math.pi * f / NF
    by = round(math.sin(ph) * 1.0) if bob else 0
    flap = math.sin(ph + .7) * (13 if heat > .5 else 0) * math.pi / 180
    cy = CY + by
    for side in (-1, 1):                                                       # alas
        bx = CX + side * (RX - .5)
        for i, (a0, L) in enumerate(((58, 9.5), (32, 11), (6, 9))):
            a = (a0 * (1 - droop) - 22 * droop) * math.pi / 180 + flap
            ang = math.atan2(-math.sin(a), side * math.cos(a))                 # y hacia abajo en pantalla
            L *= 1 - .22 * droop; feather(cv, bx + side * math.cos(a) * L / 2 * .9, cy - math.sin(a) * L / 2 * .9 + 1, L, 1.7, ang, P if i < 2 else [P[1], P[2], P[3], P[4], P[4]])
    for y in range(H):                                                         # anillo: arco de atrás (oscuro) y de delante (claro)
        for x in range(W):
            dx, dy = x + .5 - CX, y + .5 - cy
            o = (dx / RX) ** 2 + (dy / RY) ** 2 <= 1; i_ = (dx / RIN_X) ** 2 + (dy / RIN_Y) ** 2 < 1
            if o and not i_:
                if dy < 0: cv.put(x, y, rgba(P[3] if dx < 0 else P[4]))
                else:
                    edge = (dx / RX) ** 2 + ((dy - 1) / RY) ** 2 > 1
                    cv.put(x, y, rgba(P[1] if dx < -2 and dy < 2.4 else P[2] if not edge else P[3]))
    for k, hh in zip((-8, -4, 0, 4, 8), (3, 4, 5, 4, 3)):                       # pinchos sobre el arco de atrás
        for j in range(hh):
            x0 = CX + k - (1 if k == 0 else 0); w = 2 if j < hh - 2 else 1
            for x in range(x0, x0 + w + (1 if j < hh - 2 else 0)):
                if j >= hh - 1 and x != x0: continue
                cv.put(x, round(cy - RY - j), rgba(P[1] if (x == x0 and heat > .3) else P[2]))
    n = len(gems); xs = (CX,) if n == 1 else (CX - 5, CX + 5)
    for gx, gh in zip(xs, gems): gem(cv, gx, round(cy + RY + 1), gh * heat if heat < 1 else gh, P)
    if sweep and heat > .5:                                                    # brillo que recorre el aro
        x = int(CX - 9 + (f % NF) * 18 / (NF - 1)); dx = x + .5 - CX
        y = round(cy + RY * math.sqrt(max(0, 1 - (dx / RX) ** 2)) - 1)
        for xx in (x, x + 1): cv.put(xx, y, rgba(AMB[4]))
        sp = (f % 5); cv.put(CX + (-8, -4, 0, 4, 8)[sp], round(cy - RY - (3, 4, 5, 4, 3)[sp] + 1), rgba(AMB[4]))
    cv.outline(); cv.clean()
    if motes and heat > .5 and 2 <= f <= 6:                                    # motas doradas que caen
        mx = CX + (-6, 5, -2, 7, -8)[f - 2]; my = round(cy + RY + 2 + (f - 2) * 1.6)
        cv.put(mx, my, rgba(AMB[4], 235 - (f - 2) * 40)); cv.put(mx, my + 1, rgba(AMB[3], 110))
    return cv

def glow(f):
    cv = Canvas(56, 36); k = 1 + .25 * math.sin(2 * math.pi * f / NF)
    for y in range(36):
        for x in range(56):
            r = math.hypot((x + .5 - 28) / 27.0, (y + .5 - 20) / 14.0)
            if r < 1:
                a = int(58 * k * (1 - r) ** 1.4)
                if a > 3: cv.put(x, y, rgba(AMB[3], a))
    return cv

def build():
    S = {}
    S['Fx_AmberHalo_I'] = [halo(f, [1.0]) for f in range(NF)]
    S['Fx_AmberHalo_III_Full'] = [halo(f, [1.0, 1.0]) for f in range(NF)]
    S['Fx_AmberHalo_III_Half'] = [halo(f, [1.0, 0.0]) for f in range(NF)]
    S['Fx_AmberHalo_Off_I'] = [halo(0, [0.0], heat=0.0, droop=1.0, sweep=False, motes=False, bob=False)]
    S['Fx_AmberHalo_Off_III'] = [halo(0, [0.0, 0.0], heat=0.0, droop=1.0, sweep=False, motes=False, bob=False)]
    S['Fx_AmberHalo_Fade_I'] = [halo(0, [t], heat=t, droop=1 - t, sweep=False, motes=False, bob=False) for t in (.75, .5, .25, .0)]
    S['Fx_AmberHalo_Glow'] = [glow(f) for f in range(NF)]
    return S

# --- maqueta ---------------------------------------------------------------------------------------
def body(folder, d='side'):
    im = Image.open(os.path.join(TEMP, 'floor3-bodies', folder, f'Player_{folder}_Idle_{d}.png')).convert('RGBA')
    return im.crop((0, 0, im.height, im.height))

def stage(bimg, hcv, gcv=None, dy=0):
    st = Image.new('RGBA', (56, 56), C('#2b2318')); x0, y0, x1, y1 = bimg.getbbox(); st.alpha_composite(bimg, (12, 22))
    top = y0 + 22; ox = 28 - W // 2; oy = top - 9 - CY + dy
    if gcv is not None: st.alpha_composite(gcv.img(), (28 - 28, oy + CY - 20))
    st.alpha_composite(hcv.img(), (ox, oy)); return st

if __name__ == '__main__':
    from pixel import save_sheet
    S = build(); out = os.path.join(HERE, 'halo'); os.makedirs(out, exist_ok=True)
    for name, fr in S.items(): save_sheet(os.path.join(out, name + '.png'), fr)
    bg = C('#2b2318'); sc = 6
    bodies = [body('Frog'), body('Diplocaulus'), body('Whipscorpion')]
    row1 = [stage(b, S['Fx_AmberHalo_I'][2], S['Fx_AmberHalo_Glow'][2]) for b in bodies]
    fr = bodies[0]
    row2 = [stage(fr, S['Fx_AmberHalo_I'][3], S['Fx_AmberHalo_Glow'][3]), stage(fr, S['Fx_AmberHalo_Off_I'][0]),
            stage(fr, S['Fx_AmberHalo_III_Full'][3], S['Fx_AmberHalo_Glow'][3]), stage(fr, S['Fx_AmberHalo_III_Half'][3], S['Fx_AmberHalo_Glow'][3]),
            stage(fr, S['Fx_AmberHalo_Off_III'][0])]
    sheet = Image.new('RGBA', (56 * sc * 5, 56 * sc * 2), bg)
    for i, im in enumerate(row1): sheet.alpha_composite(im.resize((56 * sc, 56 * sc), Image.NEAREST), (i * 56 * sc, 0))
    for i, im in enumerate(row2): sheet.alpha_composite(im.resize((56 * sc, 56 * sc), Image.NEAREST), (i * 56 * sc, 56 * sc))
    sheet.convert('RGB').save(os.path.join(HERE, 'vista_previa_halo.png'))
    sc2 = 5; frames = [stage(fr, S['Fx_AmberHalo_I'][f], S['Fx_AmberHalo_Glow'][f]) for f in range(NF)] + [stage(fr, S['Fx_AmberHalo_Fade_I'][t]) for t in range(4)]
    strip = Image.new('RGBA', (56 * sc2 * 6, 56 * sc2 * 2), bg)
    for i, im in enumerate(frames): strip.alpha_composite(im.resize((56 * sc2, 56 * sc2), Image.NEAREST), ((i % 6) * 56 * sc2, (i // 6) * 56 * sc2))
    strip.convert('RGB').save(os.path.join(HERE, 'vista_previa_halo_bucle.png'))
    os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True)
    g = [im.resize((56 * 8, 56 * 8), Image.NEAREST).convert('RGB') for im in frames[:NF]]
    g[0].save(os.path.join(HERE, 'gif', 'halo_bucle.gif'), save_all=True, append_images=g[1:], duration=120, loop=0); print('ok')
