"""Gota de ámbar (piso 3): ESCENA del flujo completo, montada con las hojas exportadas (comprueba que encajan).

Disparo -> impacto -> formación (empieza en el cuadro 2 del impacto) -> bloque con estrellas -> grietas 1-3 ->
rotura -> enemigo libre. Talla normal con el escarabajo y talla grande con la Medullosa.
Salida: gif/escenas/escena_gota_{Normal,Large}.gif y vista_previa_escena.png (cuadros clave).
"""
import os, sys

HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import generar_bloque as gb
import generar_bloque_final as bf
import generar_disparo as gd
import generar_formacion as gf
import generar_rotura as gr
from generar_bloque import C
from PIL import Image

SW, SH, FEET = 160, 100, 72                         # escenario y fila de los pies del enemigo
HOLD = 6                                            # cuadros de estrellas (100 ms) por fase: sin grietas, 1, 2, 3

def sheet(path, w, h):
    im = Image.open(os.path.join(HERE, path)).convert('RGBA'); return [im.crop((i * w, 0, (i + 1) * w, h)) for i in range(im.width // w)]

def run(name, enemy):
    bf.set_size(name); W, H, CX, BOT = gb.W, gb.H, gb.CX, gb.BOT
    body = gb.enemy(enemy); bx0, by0 = SW - 50 - CX, FEET - (BOT - 1)              # esquina de la celda del bloque
    ex, ey = bx0 + CX - body.width // 2, by0 + BOT - 1 - body.height
    hx, hy = bx0 + CX, ey + body.height // 2                                        # el impacto, en el centro del enemigo
    P = sheet('disparo/Proj_AmberDrop.png', gd.PW, gd.PH); Hh = sheet('disparo/Fx_AmberDrop_Hit.png', gd.HIT, gd.HIT)
    FB = sheet(f'formacion/Fx_AmberDropBlock_{name}_Form_Back.png', W, H); FF = sheet(f'formacion/Fx_AmberDropBlock_{name}_Form_Front.png', W, H)
    BB = Image.open(os.path.join(HERE, f'bloque/Fx_AmberDropBlock_{name}_Back.png')).convert('RGBA')
    BF = Image.open(os.path.join(HERE, f'bloque/Fx_AmberDropBlock_{name}_Front.png')).convert('RGBA')
    CR = sheet(f'grietas/Fx_AmberDropBlock_{name}_Crack.png', W, H); ST = sheet('aturdido/Fx_AmberDropStun_Stars.png', 32, 16)
    RB = sheet(f'rotura/Fx_AmberDropBlock_{name}_Break_Back.png', gr.RW, gr.RH); RF = sheet(f'rotura/Fx_AmberDropBlock_{name}_Break_Front.png', gr.RW, gr.RH)
    top = BF.getbbox()[1]; cy = (top + BOT) / 2; rx0, ry0 = bx0 - (gr.RC - CX), by0 - round(gr.RC - cy)
    frames, dur = [], []
    def add(ms, back=None, back_at=(bx0, by0), front=(), fx=()):
        im = Image.new('RGBA', (SW, SH), C('#2b2318')); im.alpha_composite(Image.new('RGBA', (SW, SH - FEET), C('#33291c')), (0, FEET))
        if back is not None: im.alpha_composite(back, back_at)
        im.alpha_composite(body, (ex, ey))
        for layer, at in list(front) + list(fx): im.alpha_composite(layer, at)
        frames.append(im); dur.append(ms)
    for k in range(8):                                                              # la gota cruza
        add(70, fx=[(P[k % 4], (round(12 + (hx - 12) * k / 8) - gd.HX, hy - gd.HY))])
    hat = (hx - gd.HC, hy - gd.HC)
    add(60, fx=[(Hh[0], hat)]); add(60, fx=[(Hh[1], hat)])
    for f in range(gf.NF):                                                          # formación con el resto de la salpicadura
        fx = [(Hh[2 + f], hat)] if f < 4 else []
        add(gf.MS[f], back=FB[f], front=[(FF[f], (bx0, by0))], fx=fx)
    sat = (bx0 + CX - 16, max(0, by0 + top - 12))
    for phase in range(4):                                                          # atrapado: estrellas y grietas
        for s in range(HOLD):
            front = [(BF, (bx0, by0))] + ([(CR[phase - 1], (bx0, by0))] if phase else [])
            add(100, back=BB, front=front, fx=[(ST[s % len(ST)], sat)])
    for f in range(len(gr.BREAK_MS)):                                               # rotura
        add(gr.BREAK_MS[f], back=RB[f], back_at=(rx0, ry0), front=[(RF[f], (rx0, ry0))])
    add(700)
    return frames, dur

if __name__ == '__main__':
    os.makedirs(os.path.join(HERE, 'gif', 'escenas'), exist_ok=True); rows = []
    for name, enemy in (('Normal', 'Beetle'), ('Large', 'Medullosa')):
        fr, dur = run(name, enemy); S = 5
        big = [i.resize((SW * S, SH * S), Image.NEAREST).convert('RGB') for i in fr]
        big[0].save(os.path.join(HERE, 'gif', 'escenas', f'escena_gota_{name}.gif'), save_all=True, append_images=big[1:], duration=dur, loop=0)
        keys = [4, 8, 9, 11, 14, 17, 17 + HOLD, 17 + 2 * HOLD, 17 + 3 * HOLD, 17 + 4 * HOLD, 18 + 4 * HOLD, 20 + 4 * HOLD]
        rows.append([fr[k] for k in keys if k < len(fr)])
    S = 2; cols = max(len(r) for r in rows)
    out = Image.new('RGB', ((SW * S + 2) * cols + 2, (SH * S + 2) * 2 + 2), (20, 16, 12))
    for r, row in enumerate(rows):
        for c, im in enumerate(row): out.paste(im.resize((SW * S, SH * S), Image.NEAREST).convert('RGB'), (2 + c * (SW * S + 2), 2 + r * (SH * S + 2)))
    out.save(os.path.join(HERE, 'vista_previa_escena.png')); print('ok', out.size)
