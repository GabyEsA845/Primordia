"""Gota de ámbar (piso 3): MUESTRA de la glándula como cristal en el lomo (rana de lado, Idle cuadro 0).

Esquirla de ámbar que asoma del lomo, algo inclinada hacia atrás, como las agujas del bloque. Crece con la recarga:
vacía (puntita apagada) · a medias (esquirla mediana, se enciende de abajo arriba) · llena (cristal con una esquirla
pequeña al lado, brillante; destello que parpadea) · cargando (se ilumina, vibra 1 px y suelta motas).
"""
import os, sys

HERE = os.path.dirname(os.path.abspath(__file__)); TEMP = os.path.normpath(os.path.join(HERE, '..'))
sys.path.insert(0, HERE)
from generar_bloque import AMB, rgba, C, OUTL, Canvas
from PIL import Image

BASE_X = 11                                          # columna del lomo donde nace el cristal

def skin_top(frog, x):
    for y in range(frog.height):
        if frog.getpixel((x, y))[3] > 0: return y
    return None

def shard(cv, bx, by, h, w, lean, lit_rows, bright):
    """Esquirla: base de ancho w en la fila by (dentro de la piel), punta h px más arriba, inclinada 'lean' px."""
    for k in range(h + 1):
        t = k / h; half = (w / 2) * (1 - t) ** .9; cx = bx + lean * t; y = by - k
        for x in range(round(cx - half - .01), round(cx + half + .01) + 1):
            if x - cx > half + .5 or cx - x > half + .5: continue
            left = x <= cx
            on = k < lit_rows
            edge_l = x == round(cx - half - .01)                              # arista iluminada
            if bright: c = AMB[4] if left else AMB[2]
            elif on: c = (AMB[4] if edge_l else AMB[3]) if left else (AMB[1] if k < h * .5 else AMB[2])
            else: c = AMB[1] if left else AMB[0]
            cv.put(x, y, rgba(c, 255))

def crystal(frog, state, f=0):
    cv = Canvas(32, 32); by = skin_top(frog, BASE_X) + 1
    jit = (1 if f % 2 else -1) if state == 'charge' else 0
    if state == 'empty': shard(cv, BASE_X + jit, by, 2, 3, -.5, 0, False)
    elif state == 'half': shard(cv, BASE_X + jit, by, 3, 3, -1, 2, False)
    else:
        bright = state in ('spark', 'charge')
        shard(cv, BASE_X - 3 + jit, skin_top(frog, BASE_X - 3) + 1, 3, 2, -1, 3, bright)       # esquirla pequeña al lado
        shard(cv, BASE_X + jit, by, 5, 4, -1.5, 5, bright)
        cv.put(BASE_X - 1 + jit, by - 2, (255, 255, 255, 255) if bright else rgba(AMB[4], 255))
    cv.outline()
    tip = (round(BASE_X - 1.5) + jit, by - 6)
    if state == 'spark':
        sx, sy = tip[0] - 1, tip[1] - 1; cv.put(sx, sy, (255, 255, 255, 255))
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)): cv.put(sx + dx, sy + dy, rgba(AMB[4], 220))
    if state == 'charge':                                                  # motas que suben
        for i, (dx, dy) in enumerate(((-4, -2), (3, -4), (-2, -6), (4, -1))):
            yy = tip[1] + 4 + dy - (f * 2 + i) % 4
            if (tip[0] + dx, yy) not in cv.px: cv.put(tip[0] + dx, yy, rgba(AMB[4] if i % 2 else AMB[3], 230))
    return cv

if __name__ == '__main__':
    im = Image.open(os.path.join(TEMP, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')).convert('RGBA'); frog = im.crop((0, 0, 32, 32))
    states = [crystal(frog, 'empty'), crystal(frog, 'half'), crystal(frog, 'full'), crystal(frog, 'spark'), crystal(frog, 'charge', 0), crystal(frog, 'charge', 1)]
    S = 8; out = Image.new('RGB', ((32 * S + 4) * len(states) + 4, 32 * S + 80), (20, 16, 12))
    for i, g in enumerate(states):
        cell = Image.new('RGBA', (32, 32), C('#2b2318')); cell.alpha_composite(frog); cell.alpha_composite(g.img())
        out.paste(cell.resize((32 * S, 32 * S), Image.NEAREST).convert('RGB'), (4 + i * (32 * S + 4), 4))
        out.paste(cell.resize((64, 64), Image.NEAREST).convert('RGB'), (4 + i * (32 * S + 4) + 96, 32 * S + 10))
    out.save(os.path.join(HERE, 'muestra_cristal.png')); print('ok', out.size)
