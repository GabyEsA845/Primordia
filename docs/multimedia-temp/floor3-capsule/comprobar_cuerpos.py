"""Los 3 cuerpos del piso 3 dentro de la cápsula D (vista de lado y de frente), para comprobar que encajan."""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import generar_capsula_d as g
from PIL import Image
TEMP = g.TEMP; BODIES = [('Frog', 'Rana'), ('Diplocaulus', 'Diplocaulus'), ('Whipscorpion', 'Vinagrillo')]
S = 8; cw, ch = g.W * S, (g.H - 2) * S
bg = g.C('#2b2318')
sheet = Image.new('RGBA', (cw * 3 + 40, ch * 2 + 30), bg)
for r, d in enumerate(('side', 'down')):
    for i, (folder, _) in enumerate(BODIES):
        im = Image.open(os.path.join(TEMP, 'floor3-bodies', folder, f'Player_{folder}_Idle_{d}.png')).convert('RGBA')
        im = im.crop((0, 0, im.height, im.height)); x0, y0, x1, y1 = im.getbbox()
        pos = (round(23 - (x0 + x1) / 2), 41 - y1)                          # centrada en la cápsula, pies en y = 41
        cell = g.encased_at(im, pos).crop((0, 2, g.W, g.H))
        sheet.alpha_composite(cell.resize((cw, ch), Image.NEAREST), (10 + i * (cw + 10), 10 + r * (ch + 10)))
sheet.convert('RGB').save(os.path.join(HERE, 'comprobar_cuerpos_capsula_d.png')); print('ok')
