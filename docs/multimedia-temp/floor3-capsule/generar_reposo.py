"""Cápsula de ámbar (piso 3): REPOSO, mientras la criatura está dentro (va debajo de la curación).

Bucle corto de 8 cuadros x 120 ms. Sutil: un destello que cruza el cristal (cuadros 0-4) y 3 burbujas que suben
despacio por los lados y la punta, lejos de la criatura. Sin latido propio (lo pone la curación).
Nivel III: la misma animación en la cápsula mayor + una onda de luz que recorre las vetas del suelo hacia fuera.
Celda 58x60 como la formación (pies en y=51, eje x=29); vetas 96x16 (fila 0 = y 44 de la celda de la cápsula).
"""
import os, sys

HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import generar_cristalizacion as cz
import generar_capsula_nivel3 as n3
import generar_curacion as cu
import generar_rotura as rt
from generar_capsula_nivel3 import AMB, rgba, W, H, CX
from base_piso3 import *
from PIL import Image

NF, MS = 8, 120
GLINT = [0.0, .25, .5, .75, 1.0, None, None, None]                      # posición del destello (None = sin destello)
BUBBLES = [(-1, 0), (1, 3), (0, 5)]                                      # (lado: -1 izq, 1 der, 0 punta; desfase de fase)
WAVE = 3.0                                                               # ancho de la onda de luz en las vetas

def soft_glint(front, g_pos, cy):
    if g_pos is None: return cz.copy(front)
    out = cz.copy(front)
    for (x, y), c in list(front.px.items()):
        if c == OUTL or c[3] == 0: continue
        d = abs((x - CX) + (y - cy) * .7 - (g_pos * 80 - 40))
        if d < 1.0: out.px[(x, y)] = rgba(AMB[4], 200)
        elif d < 2.0: out.px[(x, y)] = rgba(AMB[3], max(c[3], 170))
    return out

def bubble_paths(cap):
    """Para cada burbuja, su columna y la altura de salida, dentro del cristal (profundidad >= 2) y fuera de la criatura."""
    paths = []
    for side, ph in BUBBLES:
        if side == 0: x, y0 = CX + 1, cap.y0 + 15
        else:
            x = CX + side * 17
            while not (cap.ins(x, 46) and cap.depth(x, 46) >= 2): x -= side
            y0 = 46
        paths.append((x, y0, ph))
    return paths

def add_bubbles(front, cap, paths, f):
    out = cz.copy(front)
    for x, y0, ph in paths:
        k = (f + ph) % NF; y = y0 - k                                    # sube 1 px por cuadro
        a = (140, 210, 245, 245, 245, 220, 170, 110)[k]                  # aparece y se deshace al llegar arriba
        if cap.ins(x, y) and cap.depth(x, y) >= 2:
            out.px[(x, y)] = rgba(AMB[4], a)                             # brillo de la burbuja
            if k < 7 and cap.ins(x + 1, y + 1): out.px[(x + 1, y + 1)] = rgba(AMB[0], a * 3 // 4)   # sombra rojiza: contraste
    return out

def veins_idle(data, f):
    pts, mx = data; cv = cz.veins_frame(data, 11)                        # vetas ya formadas
    head = (f / NF) * (mx + WAVE * 2) - WAVE                             # la onda sale de la base hacia fuera
    for (s, x, y, kind) in pts:
        d = abs(s - head)
        if d < WAVE * .5: cv.put(x, y, rgba(AMB[4], 255))
        elif d < WAVE: cv.put(x, y, rgba(AMB[3], 255))
    return cv

def cap1_mask():
    b1, f1 = cz.capsule1()
    return n3.Cap(set(p for p, c in b1.px.items() if c[3] > 0 and c != OUTL)), b1, f1

def frames(back, front, cap):
    paths = bubble_paths(cap)
    return [back] * NF, [add_bubbles(soft_glint(front, GLINT[f], 40), cap, paths, f) for f in range(NF)]

def stage(back, front, body, veins):
    return cz.stage(0, back, front, Canvas(W, H), veins, body, veins is not None)

def with_heal(n, t, B, F, V, body):
    """Cuadro t (de 100 ms) de la curación n sobre el reposo (cuadro del reposo = t*100 // 120)."""
    i = (t * cu.MS // MS) % NF; big = n == 3
    bb, ff = rt.wide(B[i]), rt.wide(F[i]); mask = set(bb.px)
    parts = (cu.glow_layer(n, t, big), cu.back_overlay(mask, n, t, 7 + n), cu.front_overlay(mask, n, t), cu.hearts_layer(n, t, big))
    return cu.stage(bb, ff, parts, t, body, cu.TINT[n][t], V[i] if big else None)

if __name__ == '__main__':
    out = os.path.join(HERE, 'reposo'); os.makedirs(out, exist_ok=True)
    body = n3.frog()
    c1, b1, f1 = cap1_mask(); b3, f3, c3 = cz.capsule3()
    B1, F1 = frames(b1, f1, c1); B3, F3 = frames(b3, f3, c3)
    data = cz.floor_veins(); V = [veins_idle(data, f) for f in range(NF)]
    from pixel import save_sheet
    for name, fr in (('Fx_AmberCapsule_Idle_Back', B1[:1]), ('Fx_AmberCapsule_Idle_Front', F1),
                     ('Fx_AmberCapsule3_Idle_Back', B3[:1]), ('Fx_AmberCapsule3_Idle_Front', F3)):
        save_sheet(os.path.join(out, name + '.png'), fr)
    sheet = Image.new('RGBA', (cz.VW * NF, cz.VH)); [sheet.alpha_composite(v.img(), (i * cz.VW, 0)) for i, v in enumerate(V)]
    sheet.save(os.path.join(out, 'Fx_AmberFloorVeins_Idle.png'))
    runs = {'nivel_I-II': [stage(B1[f], F1[f], body, None) for f in range(NF)],
            'nivel_III': [stage(B3[f], F3[f], body, V[f]) for f in range(NF)]}
    heal = {n: [with_heal(n, t, B1 if n < 3 else B3, F1 if n < 3 else F3, V, body) for t in range(cu.NF)] for n in (2, 3)}
    os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True); S = 6
    def save_gif(path, ims, ms):
        big = [i.resize((i.width * S, i.height * S), Image.NEAREST).convert('RGB') for i in ims]
        big[0].save(path, save_all=True, append_images=big[1:], duration=ms, loop=0)
    for name, ims in runs.items(): save_gif(os.path.join(HERE, 'gif', f'reposo_{name}.gif'), ims * 3, MS)
    for n, ims in heal.items(): save_gif(os.path.join(HERE, 'gif', f'reposo_con_curacion_{n}.gif'), ims, cu.MS)
    s2 = 3; cw, ch = cz.VW * s2, H * s2
    contact = Image.new('RGB', (cw * 4 + 10, (ch + 2) * 4 + 2), (20, 16, 12))
    for r, (name, ims) in enumerate(runs.items()):
        for f, im in enumerate(ims):
            contact.paste(im.resize((cw, ch), Image.NEAREST).convert('RGB'), (2 + (f % 4) * (cw + 2), 2 + (r * 2 + f // 4) * (ch + 2)))
    contact.save(os.path.join(HERE, 'vista_previa_reposo.png')); print('ok', contact.size)
