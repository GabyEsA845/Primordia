"""Gota de ámbar (piso 3): bloque elegido (C «agujas») en sus 2 tallas, sin escalar.

  Normal: celda 48x40, pies del enemigo en y=34 (BOT-1), eje x=24. Escarabajo, sapo, araña, voladores.
  Grande: celda 56x68, pies en y=62, eje x=28. Plantas (Medullosa, planta carnívora, Calamites).
Reutiliza las capas y la textura de generar_bloque.py (cambia su tamaño de celda con set_size).
"""
import os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import generar_bloque as gb
from generar_bloque import Block, C
from PIL import Image

SIZES = {
    #          W   H   CX  BOT  medio ancho, alto del cuerpo, puntas (dx, alto, ancho), pinchos, caja del enemigo (medio ancho, alto)
    'Normal': (48, 40, 24, 35, 16, 18, ((-10, 6, 5), (-3, 9, 6), (5, 7, 5), (12, 5, 4)), 14, (13, 15)),
    'Large':  (56, 68, 28, 63, 19, 46, ((-13, 7, 6), (-5, 11, 7), (3, 8, 6), (11, 12, 6)), 20, (14, 34)),
}

def set_size(name):
    W, H, CX, BOT, *_rest, box = SIZES[name]
    gb.W, gb.H, gb.CX, gb.BOT = W, H, CX, BOT
    gb.ENEMY_BOX = (CX - box[0], BOT - box[1], CX + box[0], BOT - 1)

def shape(name, seed=9):
    W, H, CX, BOT, hw, bh, peaks, spike, _ = SIZES[name]
    rnd = random.Random(seed); tops = {x: BOT - bh - rnd.choice((0, 0, 1)) for x in range(CX - hw, CX + hw + 1)}
    for dx, h, w in peaks:
        cx_ = CX + dx
        for x in range(cx_ - w // 2 - 1, cx_ + w // 2 + 2):
            peak = BOT - bh - round(h * max(0.0, 1 - abs(x - cx_) / (w / 2 + .6)))
            if x in tops: tops[x] = min(tops[x], peak)
    m = gb.mask_from_tops(tops, {0: 1})
    for side in (-1, 1):                             # pinchos que suben desde el suelo, pegados a los lados
        base = CX + side * hw
        for k in range(spike):
            wdt = max(0, round((spike - k) / 4.0))
            for e in range(1, wdt + 1): m.add((base + side * e, BOT - k))
    return m

def build(name, seed):
    set_size(name); gb.STRATA.clear(); b = Block(shape(name))
    big = name == 'Large'
    return gb.back_layer(b), gb.front_layer(b, random.Random(seed), n_fib=22 if big else 7, n_crack=9 if big else 4, n_bub=7 if big else 4, scale=1.6 if big else 1.0), b

if __name__ == '__main__':
    out = os.path.join(HERE, 'bloque'); os.makedirs(out, exist_ok=True)
    rows = []
    for name, enemies in (('Normal', ('Beetle', 'Toad', 'Spider')), ('Large', ('Medullosa', 'CarnivorousPlant', 'Calamites'))):
        bk, fr, b = build(name, 3)
        bk.img().save(os.path.join(out, f'Fx_AmberDropBlock_{name}_Back.png')); fr.img().save(os.path.join(out, f'Fx_AmberDropBlock_{name}_Front.png'))
        empty = Image.new('RGBA', (gb.W, gb.H + 2), C('#2b2318')); empty.alpha_composite(bk.img()); empty.alpha_composite(fr.img())
        rows.append([empty] + [gb.compose(bk, fr, gb.enemy(e)) for e in enemies])
    S = 4; MW, MH = 64, 70
    sheet = Image.new('RGB', ((MW * S + 2) * 4 + 2, (MH * S + 2) * 2 + 2), (20, 16, 12))
    for r, row in enumerate(rows):
        for c, im in enumerate(row):
            cell = Image.new('RGBA', (MW, MH), C('#2b2318')); cell.alpha_composite(im, ((MW - im.width) // 2, MH - im.height))
            sheet.paste(cell.resize((MW * S, MH * S), Image.NEAREST).convert('RGB'), (2 + c * (MW * S + 2), 2 + r * (MH * S + 2)))
    sheet.save(os.path.join(HERE, 'vista_previa_bloque.png')); print('ok', sheet.size)
