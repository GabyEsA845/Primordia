"""Gota de ámbar (piso 3): FORMACIÓN del bloque sobre el enemigo, en las 2 tallas.

Como la Cápsula: el ámbar sube de abajo arriba con la superficie brillante, pero rápida (~0,5 s):
5 cuadros de subida (70 ms) + 2 de remate (90 ms: destello que cruza y chispa en la punta más alta).
Al empezar salpican unas gotitas en la base. El enemigo va entre Back y Front (ya aturdido desde el impacto).
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import generar_bloque as gb
import generar_bloque_final as bf
from generar_bloque import AMB, rgba, C, OUTL, Canvas
from PIL import Image

LEVELS = [.15, .35, .55, .75, .95]                 # cuánto ha subido la resina
GLINT = [.3, .85]                                  # remate: posición del destello
NF = len(LEVELS) + len(GLINT)
MS = [70] * len(LEVELS) + [90] * len(GLINT)

def bounds(cv):
    ys = [y for (x, y) in cv.px]; return min(ys), max(ys)

def clip(cv, level, top_cap):
    y0, y1 = bounds(cv); ys = int(round(y1 - level * (y1 - y0)))
    out = Canvas(gb.W, gb.H)
    for (x, y), c in cv.px.items():
        if y >= ys: out.px[(x, y)] = c
    if top_cap:
        for x in range(gb.W):
            c = cv.px.get((x, ys))
            if c is not None and c != OUTL and c[3] > 0:
                out.px[(x, ys)] = rgba(AMB[4] if (x + ys) % 3 == 0 else AMB[3], 255)
                if (x, ys - 1) not in out.px: out.px[(x, ys - 1)] = OUTL
                if (x, ys + 1) in out.px and out.px[(x, ys + 1)] != OUTL and x % 2 == 0: out.px[(x, ys + 1)] = rgba(AMB[3], 235)
    return out

def glint(front, g_pos):
    out = Canvas(gb.W, gb.H); out.px = dict(front.px); y0, y1 = bounds(front); cy = (y0 + y1) / 2; span = gb.W * .9
    for (x, y), c in front.px.items():
        if c != OUTL and c[3] > 0 and abs((x - gb.CX) + (y - cy) * .7 - (g_pos * span - span / 2)) < 1.6:
            out.px[(x, y)] = rgba(AMB[4], 230)
    return out

def spark(cv, x, y, r):
    """Chispa de 4 puntas: centro blanco (única excepción al blanco puro), brazos dorados."""
    cv.put(x, y, (255, 255, 255, 255))
    for k in range(1, r + 1):
        a = 255 if k < r else 170
        for dx, dy in ((k, 0), (-k, 0), (0, k), (0, -k)): cv.put(x + dx, y + dy, rgba(AMB[4] if k < r else AMB[3], a))

def splash(cv, f, rnd_seed=4):
    """Gotitas que saltan en la base en los dos primeros cuadros."""
    if f > 1: return
    rnd = random.Random(rnd_seed); hw = (gb.W - 10) // 2
    for i in range(6):
        side = -1 if i % 2 else 1; x = gb.CX + side * (hw - rnd.randint(0, 4) + f * 2); y = gb.BOT - 2 - rnd.randint(1, 3) - f * 2
        cv.put(x, y, rgba(AMB[3], 255)); cv.put(x, y + 1, rgba(AMB[0], 200))

def frames(name):
    bk, fr, b = bf.build(name, 3)
    tip = min(b.mask, key=lambda p: (p[1], abs(p[0] - gb.CX)))           # punta más alta
    B, F = [], []
    for f in range(NF):
        if f < len(LEVELS):
            B.append(clip(bk, LEVELS[f], False)); cv = clip(fr, LEVELS[f], True); splash(cv, f)
        else:
            B.append(bk); cv = glint(fr, GLINT[f - len(LEVELS)])
            spark(cv, tip[0], tip[1] - 1, 3 if f == len(LEVELS) else 2)
        F.append(cv)
    return B, F

def strip(cvs, w, h):
    s = Image.new('RGBA', (w * len(cvs), h))
    for i, c in enumerate(cvs): s.alpha_composite(c.img(), (i * w, 0))
    return s

if __name__ == '__main__':
    out = os.path.join(HERE, 'formacion'); os.makedirs(out, exist_ok=True); os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True)
    runs = {}
    for name, enemy in (('Normal', 'Beetle'), ('Large', 'Medullosa')):
        B, F = frames(name)                                                 # deja gb en la talla de este bloque
        strip(B, gb.W, gb.H).save(os.path.join(out, f'Fx_AmberDropBlock_{name}_Form_Back.png'))
        strip(F, gb.W, gb.H).save(os.path.join(out, f'Fx_AmberDropBlock_{name}_Form_Front.png'))
        body = gb.enemy(enemy); ims = [gb.compose(B[f], F[f], body) for f in range(NF)]
        hold = gb.compose(B[-1], bf.build(name, 3)[1], body)                # bloque ya formado, para ver el bucle entero
        runs[name] = ims
        S = 6; big = [i.resize((i.width * S, i.height * S), Image.NEAREST).convert('RGB') for i in ims + [hold]]
        big[0].save(os.path.join(HERE, 'gif', f'formacion_{name}.gif'), save_all=True, append_images=big[1:], duration=MS + [600], loop=0)
    S = 3; MW, MH = 64, 70
    sheet = Image.new('RGB', ((MW * S + 2) * NF + 2, (MH * S + 2) * 2 + 2), (20, 16, 12))
    for r, ims in enumerate(runs.values()):
        for c, im in enumerate(ims):
            cell = Image.new('RGBA', (MW, MH), C('#2b2318')); cell.alpha_composite(im, ((MW - im.width) // 2, MH - im.height))
            sheet.paste(cell.resize((MW * S, MH * S), Image.NEAREST).convert('RGB'), (2 + c * (MW * S + 2), 2 + r * (MH * S + 2)))
    sheet.save(os.path.join(HERE, 'vista_previa_formacion.png')); print('ok', sheet.size)
