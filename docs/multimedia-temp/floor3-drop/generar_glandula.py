"""Gota de ámbar (piso 3): GLÁNDULA = cristal de ámbar en el lomo, capa de cuerpo para los 3 cuerpos.

Sustituye a Special_AmberGland_{Rest,Full}_* de floor3-mutations (bolita en la garganta). Mismo formato: un lienzo de
56x56 por cuadro, encima del cuerpo, para cada cuerpo, animación y dirección (anclaje 'back' de floor3-mutations).
Estados (el código elige según la recarga):
  Empty  = puntita apagada (recién disparada)       Half   = esquirla mediana, encendida de abajo arriba
  Full   = cristal con esquirla al lado y brillo; en Idle, destello en el cuadro 1
  Charge = mientras se mantiene el clic derecho: cristal iluminado que vibra 1 px y suelta motas
La esquirla se inclina hacia atrás (de lado); de frente y de espaldas sale recta.
"""
import os, sys

HERE = os.path.dirname(os.path.abspath(__file__)); MUT = os.path.normpath(os.path.join(HERE, '..', 'floor3-mutations'))
sys.path.insert(0, MUT); sys.path.insert(0, HERE)
import generar_mutaciones_piso3 as gm
from generar_bloque import AMB, rgba, C, Canvas
from PIL import Image

STATES = ('Empty', 'Half', 'Full', 'Charge')
SIZE = {'Frog': (5, 4, 3), 'Diplocaulus': (4, 3, 3), 'Whipscorpion': (4, 3, 2)}   # cristal lleno: alto, ancho; esquirla al lado: alto

def shard(cv, bx, by, h, w, lean, lit_rows, bright):
    """Esquirla: base de ancho w en la fila by (dentro de la piel), punta h px más arriba, inclinada 'lean' px."""
    for k in range(h + 1):
        t = k / h; half = (w / 2) * (1 - t) ** .9; cx = bx + lean * t; y = by - k
        for x in range(round(cx - half - .01), round(cx + half + .01) + 1):
            if x - cx > half + .5 or cx - x > half + .5: continue
            left = x <= cx; edge_l = x == round(cx - half - .01)
            if bright: c = AMB[4] if left else AMB[2]
            elif k < lit_rows: c = (AMB[4] if edge_l else AMB[3]) if left else (AMB[1] if k < h * .5 else AMB[2])
            else: c = AMB[1] if left else AMB[0]
            cv.put(x, y, rgba(c, 255))

def layer(body, im, facing, anim, i, state):
    off = (gm.NL - im.height) // 2; cells = gm.cells_of(im, off); a = gm.anchors(body, facing, cells)
    bx, top = a['back']; bx = round(bx); by = round(top) + 1
    if facing == 'up': by = round(top) + 2                                     # de espaldas: sale del centro del lomo
    if facing == 'down' and body != 'Frog':                                    # de frente, lo más alto es la cola: va en el tronco
        x0, y0, x1, y1 = gm.core_box(cells); bx = round((x0 + x1) / 2); by = y0 + 2
    lean = -1.5 if facing == 'side' else 0; side_dx = -3 if facing == 'side' else 3
    h, w, hs = SIZE[body]; cv = Canvas(gm.NL)
    jit = (1 if i % 2 else -1) if state == 'Charge' else 0
    if state == 'Empty': shard(cv, bx, by, 2, 3, lean / 3, 0, False)
    elif state == 'Half': shard(cv, bx, by, h - 2, w - 1 if w > 3 else 3, lean * .66, 2, False)
    else:
        bright = state == 'Charge'
        sx = bx + side_dx
        sy = min((c[1] for c in cells if c[0] == sx), default=by - 1) + 1 if facing != 'up' else by + 1
        shard(cv, sx + jit, sy, hs, 2, lean * .66, hs, bright)                # esquirla pequeña al lado
        shard(cv, bx + jit, by, h, w, lean, h, bright)
        cv.put(bx - 1 + jit, by - 2, (255, 255, 255, 255) if bright else rgba(AMB[4], 255))
    cv.outline()
    tip = (round(bx + lean) + jit, by - h - 1)
    if state == 'Full' and anim == 'Idle' and i == 1:                         # destello
        sx, sy = tip[0] - 1, tip[1] - 1; cv.put(sx, sy, (255, 255, 255, 255))
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)): cv.put(sx + dx, sy + dy, rgba(AMB[4], 220))
    if state == 'Charge':                                                      # motas que suben
        for k, (dx, dy) in enumerate(((-4, -2), (3, -4), (-2, -6), (4, -1))):
            yy = tip[1] + 4 + dy - (i * 2 + k) % 4
            if (tip[0] + dx, yy) not in cv.px: cv.put(tip[0] + dx, yy, rgba(AMB[4] if k % 2 else AMB[3], 230))
    return cv.img()

if __name__ == '__main__':
    out = os.path.join(HERE, 'glandula'); n = 0; rows = {}
    for body, anims in gm.SPEC_ANIMS.items():
        for anim in anims:
            for f in gm.FACINGS:
                frames = gm.frames_of(os.path.join(gm.BODIES, body, f'Player_{body}_{anim}_{f}.png'))
                for st in STATES:
                    lay = [layer(body, fr, f, anim, i, st) for i, fr in enumerate(frames)]
                    gm.save(os.path.join(out, f'Special_AmberGland_{st}_{body}_{anim}_{f}.png'), lay); n += 1
                    if anim in ('Idle', 'Move'):
                        comp = []
                        for j, fr in enumerate(frames):
                            c = Image.new('RGBA', (gm.NL, gm.NL), (0, 0, 0, 0)); o = (gm.NL - fr.height) // 2
                            c.alpha_composite(fr, (o, o)); c.alpha_composite(lay[j]); comp.append(c)
                        rows[f'{st} | {body} {anim} {f}'] = comp
    sel = {k: v for k, v in rows.items() if 'Idle' in k}
    gm.preview(os.path.join(HERE, 'vista_previa_glandula.png'), sel, gm.BG, scale=2)
    os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True)
    for body in gm.SPEC_ANIMS:
        for st in ('Full', 'Charge'):
            gm.gif(os.path.join(HERE, 'gif', f'glandula_{st}_{body}_Move_side.gif'), rows[f'{st} | {body} Move side'], 110, gm.BG, scale=4)
    print('ok', n, 'hojas')
