"""Efectos de la Arthropleura en los lagos de lodo (F2), subtanda D del arte de la sala.

- `Effects/Fx_Arthropleura_MudDouse` (48×48, 7 cuadros, 90 ms, pivote (24, 33)): una pieza ardiendo cae en el lodo:
  salpicadura de barro, ascuas que se apagan y vapor que sube y se deshace. Cuadro que cuenta: 0 (se apaga).
- `Effects/Fx_Arthropleura_MudWake` (128×80, 6 cuadros, 100 ms, pivote centro): onda que deja una pieza al pasar por el
  lodo y se abre más allá del ancho del cuerpo (sin girar; una cada ~0,3 s por pieza en el lodo).

Uso: python generar_lodo_arthropleura.py
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', '..', 'floor3-environment'))
from base_piso3 import *
from PIL import Image

MUDC = [C(c) for c in MUD]                            # #22160f … #86683f
STEAM = [C('#9a988a'), C('#c4c2b4'), C('#e6e4d8')]
EMB = [C(c) for c in EMBER]

def douse(f):
    K = 1.5
    cv = Canvas(48, 48); rnd = random.Random(40)
    cx, cy = 24, 33
    # salpicadura: gotas de barro que suben en corona y caen (cuadros 0-3)
    if f <= 3:
        for i in range(12):
            a = math.pi + i * math.pi / 11 + rnd.uniform(-.1, .1)        # hacia arriba y a los lados
            sp = rnd.uniform(2.5, 3.8) * K; t = (f + 1) * .9
            x = cx + math.cos(a) * sp * t * .9
            y = cy + math.sin(a) * sp * t * .7 + .8 * K * t * t     # sube y cae
            c = MUDC[3] if i % 3 else MUDC[4]
            if y > 43: continue                       # ya ha caído al lodo
            cv.put(x, y, c); cv.put(x + 1, y, MUDC[2]); cv.put(x, y + 1, MUDC[1]); cv.put(x + 1, y + 1, MUDC[0])
            if f == 0: cv.put(x, y - 1, MUDC[4])
    if f <= 2:                                        # anillo de barro levantado en el sitio del golpe
        r = (4 + f * 3.2) * K
        for k in range(56):
            a = k * 2 * math.pi / 56
            x, y = cx + math.cos(a) * r, cy + math.sin(a) * r * .45
            cv.put(x, y, MUDC[4] if math.sin(a) < 0 else MUDC[2])
            cv.put(x, y + (1 if math.sin(a) > 0 else -1), MUDC[0] if math.sin(a) > 0 else MUDC[3])
    if f <= 1:                                        # ascuas que se apagan
        for i in range(12):
            x, y = cx + rnd.uniform(-10, 10), cy - 3 - rnd.uniform(0, 7) - f * 4
            c = EMB[3] if f == 0 and i % 2 else EMB[2] if f == 0 else EMB[1] if i % 2 else EMB[0]
            cv.put(x, y, c)
            if f == 0: cv.put(x, y + 1, EMB[1])
    # vapor: bocanadas que suben, crecen y se deshacen (cuadros 1-6)
    if f >= 1:
        for j, (ox, delay) in enumerate(((-6, 0), (5, .6), (0, 1.2), (-2, 1.8))):
            t = f - 1 - delay
            if t < 0: continue
            px, py = cx + ox * 1.3 + math.sin(t + j) * 1.8, cy - 6 - t * 4.2
            r = (3.2 + t * 1.3) * K
            fade = max(0, 1 - t / 5.2)
            for y in range(48):
                for x in range(48):
                    d = math.hypot((x + .5 - px) / r, (y + .5 - py) / (r * .85))
                    if d < 1:
                        lit = (x - px) + (y - py) < 0
                        k = 2 if lit and d < .55 else 1 if d < .8 else 0
                        a = int(200 * fade * (1 if d < .8 else .6))
                        if a > 30 and ((x + y) % 2 == 0 or d < .7):
                            old = cv.px.get((x, y))
                            if old is None or old[3] < a: cv.put(x, y, STEAM[k][:3] + (a,))
    return cv

def wake(f):
    """Onda que deja una pieza al pasar por el lodo: un anillo que se abre más allá del ancho del cuerpo y se desvanece.
    No se gira; el código suelta una cada ~0,3 s por pieza que avanza por el lodo."""
    cv = Canvas(128, 80); cx, cy = 64, 40
    rx = 26 + f * 6.6; ry = rx * .58; fade = 1 - f / 6.4
    LIP = C('#a88a58')
    n = int(rx * 3.2)
    for k in range(n):
        a = k * 2 * math.pi / n
        if (k // 3) % 4 == 3: continue               # entrecortada
        x, y = cx + math.cos(a) * rx, cy + math.sin(a) * ry
        front = math.sin(a) < 0                       # la mitad de atrás (arriba) recibe la luz
        crest = LIP if fade > .6 and front else MUDC[4] if fade > .35 else MUDC[3]
        al = int(255 * min(1, fade + .25))
        cv.put(x, y, crest[:3] + (al,)); cv.put(x, y - 1 if front else y + 1, MUDC[3][:3] + (al,))
        cv.put(cx + math.cos(a) * (rx - 1.4), cy + math.sin(a) * (ry - 1), MUDC[0][:3] + (int(190 * fade),))   # seno oscuro dentro
    return cv

if __name__ == '__main__':
    E = os.path.join(HERE, 'Effects')
    D = [douse(f) for f in range(7)]; Wk = [wake(f) for f in range(6)]
    save_sheet(os.path.join(E, 'Fx_Arthropleura_MudDouse.png'), D)
    save_sheet(os.path.join(E, 'Fx_Arthropleura_MudWake.png'), Wk)
    mud = Image.open(os.path.join(HERE, '..', '..', 'floor3-environment', 'Smoke', 'mud', 'Mud_0.png')).convert('RGBA')
    def on_mud(cv, w=64, h=64, ox=8, oy=8):
        im = Image.new('RGBA', (w, h))
        for y in range(0, h, 16):
            for x in range(0, w, 16): im.alpha_composite(mud, (x, y))
        im.alpha_composite(cv.img(), (ox, oy)); return im
    os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True)
    frames = [on_mud(f).resize((256, 256), Image.NEAREST) for f in D] + [on_mud(Canvas(48, 48)).resize((256, 256), Image.NEAREST)] * 3
    frames[0].save(os.path.join(HERE, 'gif', 'Fx_Arthropleura_MudDouse.gif'), save_all=True, append_images=frames[1:], duration=[90] * 7 + [300] * 3, loop=0)
    wf = [on_mud(f, 144, 96, 8, 8).resize((432, 288), Image.NEAREST) for f in Wk]
    wf[0].save(os.path.join(HERE, 'gif', 'Fx_Arthropleura_MudWake.gif'), save_all=True, append_images=wf[1:], duration=100, loop=0)
    pv = Image.new('RGBA', (7 * 192 + 20, 200 + 200), BG)
    for i, f in enumerate(D): pv.alpha_composite(on_mud(f).resize((192, 192), Image.NEAREST), (i * 192, 0))
    for i, f in enumerate(Wk): pv.alpha_composite(on_mud(f, 144, 96, 8, 8).resize((216, 144), Image.NEAREST), (i * 220, 200))
    pv.save(os.path.join(HERE, 'vista_previa_lodo.png'))
    print('Hecho')
