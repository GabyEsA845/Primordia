"""Gota de ámbar (piso 3): MUESTRA de la glándula como gema en la frente (rana de lado, Idle cuadro 0).

Gemita facetada de 4x3 (+ contorno) incrustada en lo alto de la cabeza, detrás del ojo. Estados:
vacía (apagada) · a medias (se enciende de abajo arriba) · llena (brillante; destello que parpadea cada tanto) ·
cargando (mientras se mantiene el clic derecho: late con un halo y el centro se pone blanco).
"""
import os, sys

HERE = os.path.dirname(os.path.abspath(__file__)); TEMP = os.path.normpath(os.path.join(HERE, '..'))
sys.path.insert(0, HERE)
from generar_bloque import AMB, rgba, C, OUTL, Canvas
from PIL import Image

GEM = ['.XX.', 'XXXX', '.XX.']
AT = (13, 9)                                        # esquina de la gema sobre la celda de la rana (32x32)

def gem(state, glow=0):
    cv = Canvas(32, 32); x0, y0 = AT
    for yy, row in enumerate(GEM):
        for xx, ch in enumerate(row):
            if ch != 'X': continue
            tl = xx + yy <= 1                                              # faceta iluminada (arriba a la izquierda)
            if state == 'empty': c = AMB[1] if tl else AMB[0]
            elif state == 'half': c = (AMB[3] if yy == 2 or (yy == 1 and xx > 1) else AMB[1] if tl else AMB[0])
            else: c = AMB[4] if tl else AMB[3] if yy < 2 else AMB[2]
            cv.put(x0 + xx, y0 + yy, rgba(c, 255))
    if state in ('full', 'spark', 'charge'): cv.put(x0 + 1, y0, (255, 255, 255, 255) if state != 'full' else rgba(AMB[4], 255))
    cv.outline()
    if state == 'spark':                                                   # destello que parpadea
        sx, sy = x0 + 3, y0 - 2
        cv.put(sx, sy, (255, 255, 255, 255))
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)): cv.put(sx + dx, sy + dy, rgba(AMB[4], 220))
    if state == 'charge':                                                  # rayos que laten: rectos / en diagonal
        cx_, cy_ = x0 + 1.5, y0 + 1
        rays = ((0, -1), (0, 1), (-1, 0), (1, 0)) if glow == 0 else ((-1, -1), (1, -1), (-1, 1), (1, 1))
        for dx, dy in rays:
            for k in (3, 4):
                x = round(cx_ + dx * (k + (1 if dx and not dy else 0))); y = round(cy_ + dy * (k - 1))
                if (x, y) not in cv.px: cv.put(x, y, rgba(AMB[4] if k == 3 else AMB[3], 255 if k == 3 else 170))
    return cv

if __name__ == '__main__':
    im = Image.open(os.path.join(TEMP, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')).convert('RGBA'); frog = im.crop((0, 0, 32, 32))
    states = [('Vacía', gem('empty')), ('A medias', gem('half')), ('Llena', gem('full')), ('Llena (destello)', gem('spark')),
              ('Cargando 1', gem('charge', 0)), ('Cargando 2', gem('charge', 1))]
    S = 8; out = Image.new('RGB', ((32 * S + 4) * len(states) + 4, 32 * S * 2 + 8), (20, 16, 12))
    for i, (_, g) in enumerate(states):
        cell = Image.new('RGBA', (32, 32), C('#2b2318')); cell.alpha_composite(frog); cell.alpha_composite(g.img())
        out.paste(cell.resize((32 * S, 32 * S), Image.NEAREST).convert('RGB'), (4 + i * (32 * S + 4), 4))
        small = Image.new('RGBA', (32, 32), C('#2b2318')); small.alpha_composite(frog); small.alpha_composite(g.img())
        out.paste(small.resize((64, 64), Image.NEAREST).convert('RGB'), (4 + i * (32 * S + 4) + 96, 32 * S + 40))
    out.save(os.path.join(HERE, 'muestra_gema.png')); print('ok', out.size)
