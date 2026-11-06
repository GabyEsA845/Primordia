"""Gota de ámbar (piso 3): DISPARO (lágrima con estela) e IMPACTO (salpicadura).

Proj_AmberDrop: 4 cuadros de 24x12, 70 ms, mirando a la derecha (el código la gira con la dirección).
  Pivote = centro de la cabeza (17, 6). Cabeza redonda delante, punta hacia atrás y 2-3 goterones de estela.
Fx_AmberDrop_Hit: 6 cuadros de 32x32, 60 ms, centrado en el enemigo (16, 16). Vista cenital: la gota se aplasta,
  revienta en un anillo de goterones que salen y caen al suelo alrededor. La formación del bloque empieza en el cuadro 2.
"""
import math, os, sys

HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
from generar_bloque import AMB, rgba, C, OUTL, Canvas
from PIL import Image

PW, PH, HX, HY = 24, 12, 17, 6
HIT, HIT_MS, HC = 32, 60, 16

def blob(cv, cx, cy, rx, ry, tail=0, glint=True):
    """Lágrima de ámbar como el bloque: borde rojizo, interior translúcido (claro a la luz, rojizo en la sombra) y
    núcleo cálido en el lado contrario a la luz (la luz atraviesa la resina). 'tail' = punta hacia la izquierda."""
    pts = set()
    for y in range(int(cy - ry - 1), int(cy + ry + 2)):
        for x in range(int(cx - rx - tail - 1), int(cx + rx + 2)):
            u, v = (x + .5 - cx) / rx, (y + .5 - cy) / ry
            inside = u * u + v * v <= 1
            if not inside and tail and x + .5 < cx:                       # la punta: se estrecha hacia atrás
                t = (cx - (x + .5)) / (rx + tail); inside = abs(y + .5 - cy) <= ry * max(0.0, 1 - t) ** 1.3 and t < 1
            if inside: pts.add((x, y))
    small = rx < 2
    for (x, y) in pts:
        u, v = (x + .5 - cx) / rx, (y + .5 - cy) / ry; k = u * .6 + v * .7          # k > 0: lado en sombra
        edge = any((x + dx, y + dy) not in pts for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
        if small: c, a = (AMB[3] if k < -.2 else AMB[0] if k > .3 else AMB[2]), 255
        elif edge: c, a = (AMB[3] if k < -.35 else AMB[0] if k > .25 else AMB[1]), 255
        elif math.hypot(u - .25, v - .3) < .3: c, a = AMB[3], 235                     # núcleo cálido
        elif k < -.65: c, a = AMB[4], 235                                             # reflejo largo
        elif k < -.05: c, a = AMB[2], 215
        else: c, a = AMB[1], 215
        cv.put(x, y, rgba(c, a))
    if glint and not small: cv.put(round(cx - rx * .4), round(cy - ry * .45), (255, 255, 255, 255) if rx >= 3 else rgba(AMB[4], 255))

def proj_frames():
    out = []
    TRAIL = [[(9, 6, 1.4), (5, 5, 1.0), (2, 7, .7)], [(8, 6, 1.4), (4, 7, 1.0), (1, 5, .7)],
             [(9, 7, 1.3), (5, 5, .9), (2, 6, .8)], [(8, 5, 1.4), (4, 6, 1.0), (1, 7, .6)]]
    for f in range(4):
        cv = Canvas(PW, PH)
        wob = (0, .25, 0, -.25)[f]                                        # la cabeza tiembla un poco: es líquida
        blob(cv, HX, HY, 4.2 + wob, 3.6 - wob, tail=4)
        for (x, y, r) in TRAIL[f]:
            if r >= 1.2: blob(cv, x, y, r, r, glint=False)
            else: cv.put(x, y, rgba(AMB[3] if r > .8 else AMB[2], 255))
        cv.outline(); out.append(cv)
    return out

def hit_frames():
    out = []
    drops = [(i / 9 * 2 * math.pi + (.2 if i % 2 else 0), 1.0 if i % 3 else .8) for i in range(9)]
    for f in range(6):
        cv = Canvas(HIT, HIT)
        if f == 0: blob(cv, HC, HC, 5.5, 4.2)                             # se aplasta contra el enemigo
        elif f == 1:
            blob(cv, HC, HC, 6.5, 3.4, glint=False)                       # revienta: anillo
            for a, s in drops: blob(cv, HC + math.cos(a) * 7 * s, HC + math.sin(a) * 6 * s, 1.5, 1.5, glint=False)
        else:
            r = (0, 0, 10, 12.5, 13.5, 14)[f]
            for a, s in drops:                                            # goterones que salen y caen al suelo
                x, y = HC + math.cos(a) * r * s, HC + math.sin(a) * r * .85 * s + (f - 2) * .8
                if f <= 3: blob(cv, x, y, 1.6 if f == 2 else 1.2, 1.4 if f == 2 else 1.0, glint=False)
                else:                                                     # ya en el suelo: manchitas que se apagan
                    al = 255 if f == 4 else 150; cv.put(round(x), round(y), rgba(AMB[2], al)); cv.put(round(x) + 1, round(y), rgba(AMB[1], al))
        if f <= 3: cv.outline()
        out.append(cv)
    return out

def strip(cvs, w, h):
    s = Image.new('RGBA', (w * len(cvs), h))
    for i, c in enumerate(cvs): s.alpha_composite(c.img(), (i * w, 0))
    return s

if __name__ == '__main__':
    out = os.path.join(HERE, 'disparo'); os.makedirs(out, exist_ok=True); os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True)
    P, Hf = proj_frames(), hit_frames()
    strip(P, PW, PH).save(os.path.join(out, 'Proj_AmberDrop.png')); strip(Hf, HIT, HIT).save(os.path.join(out, 'Fx_AmberDrop_Hit.png'))
    # GIF: la gota cruza y revienta sobre un escarabajo
    sys.path.insert(0, HERE); import generar_bloque as gb
    beetle = gb.enemy('Beetle'); W2, H2 = 112, 40; bx = 84; seq = []; dur = []
    for k in range(8):
        im = Image.new('RGBA', (W2, H2), C('#2b2318')); im.alpha_composite(beetle, (bx - beetle.width // 2, 26 - beetle.height // 2))
        im.alpha_composite(P[k % 4].img(), (6 + k * 9 - HX, 22 - HY)); seq.append(im); dur.append(70)
    for f in range(6):
        im = Image.new('RGBA', (W2, H2), C('#2b2318')); im.alpha_composite(beetle, (bx - beetle.width // 2, 26 - beetle.height // 2))
        im.alpha_composite(Hf[f].img(), (bx - HC, 22 - HC)); seq.append(im); dur.append(HIT_MS)
    dur[-1] = 500; S = 6
    big = [i.resize((W2 * S, H2 * S), Image.NEAREST).convert('RGB') for i in seq]
    big[0].save(os.path.join(HERE, 'gif', 'disparo_impacto.gif'), save_all=True, append_images=big[1:], duration=dur, loop=0)
    S = 8; sheet = Image.new('RGB', (max(PW * 4, HIT * 6) * S + 14 * 2, (PH + HIT) * S + 6), (20, 16, 12))
    for i, c in enumerate(P):
        cell = Image.new('RGBA', (PW, PH), C('#2b2318')); cell.alpha_composite(c.img()); sheet.paste(cell.resize((PW * S, PH * S), Image.NEAREST).convert('RGB'), (2 + i * (PW * S + 2), 2))
    for i, c in enumerate(Hf):
        cell = Image.new('RGBA', (HIT, HIT), C('#2b2318')); cell.alpha_composite(c.img()); sheet.paste(cell.resize((HIT * S, HIT * S), Image.NEAREST).convert('RGB'), (2 + i * (HIT * S + 2), PH * S + 4))
    sheet.save(os.path.join(HERE, 'vista_previa_disparo.png')); print('ok', sheet.size)
