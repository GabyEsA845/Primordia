"""Escenas de revisión de las coronas y los marcos laterales: un trozo de sala de cada tema, antes (izquierda) y después
(derecha). Un jugador del piso entra por la puerta de arriba (pasa por detrás del marco, como en el juego) y otro sale por la
lateral (pasa por debajo del puente). Solo para revisar: gif/escenas/escena_<Tema>.gif.

Uso: python escenas_puertas.py
"""
import os
from PIL import Image
import generar_marcos_puertas as G

HERE = os.path.dirname(os.path.abspath(__file__))
MT = os.path.join(HERE, '..')
POLISH = os.path.join(MT, 'floor3-polish', 'Doors')
PLAYER = {1: os.path.join(MT, 'player', 'Player_Move_{}.png'),
          2: os.path.join(MT, 'floor2-bodies', 'Jellyfish', 'Player_Jellyfish_Move_{}.png'),
          3: os.path.join(MT, 'floor3-bodies', 'Frog', 'Player_Frog_Move_{}.png')}
MS = 80
W, H = 10, 8                                                  # sala de 10×8 tiles
AX, FACE_Y = 80, 16                                           # arco en las columnas 5-6; la cara empieza en la fila 1
SIDE_X, SIDE_Y = 16, 64                                       # hueco lateral: columna 1, filas 4-5

def frames_of(path):
    im = Image.open(path).convert('RGBA'); c = im.height
    return [im.crop((i * c, 0, (i + 1) * c, c)) for i in range(im.width // c)]

def room(th):
    t = th['tiles']
    im = Image.new('RGBA', (W * 16, H * 16))
    for y in range(H):
        for x in range(W):
            if y == 0 or x == 0: k = 'cap'
            elif x == 1: k = 'el' if y >= 3 else 'cap'
            elif y == 1: k = 'fu'
            elif y == 2: k = 'fl'
            elif y == 3: k = 'shadow'
            else: k = 'floor'
            im.alpha_composite(t[k], (x * 16, y * 16))
    for y in (4, 5): im.alpha_composite(t['floor'], (16, y * 16))   # hueco lateral (abierto)
    im.alpha_composite(th['arch'], (AX, FACE_Y))
    return im

def clip(sprite, x0=None, y0=None, ox=0, oy=0):
    """Borra lo que queda a la izquierda de x0 o encima de y0 (coordenadas de la escena)."""
    sp = sprite.copy(); px = sp.load()
    for y in range(sp.height):
        for x in range(sp.width):
            if (x0 is not None and ox + x < x0) or (y0 is not None and oy + y < y0): px[x, y] = (0, 0, 0, 0)
    return sp

def scene(name, style, folder, idx):
    th = G.load_theme(name, folder, idx)
    crown = G.as_img(G.crown(th, style, name))
    front, back = (G.as_img(c) for c in G.side_frame(th, style, name))
    decos = [G.as_img(G.wall_deco(th, style, name, k)) for k in range(3)]
    dfront = Image.open(os.path.join(POLISH, name, 'Doorway_Top_Front.png')).convert('RGBA')
    dshade = Image.open(os.path.join(POLISH, name, 'Doorway_Top_Shade.png')).convert('RGBA')
    up, side = frames_of(PLAYER[style].format('up')), frames_of(PLAYER[style].format('side'))
    n = up[0].width
    base = room(th)
    out = []
    steps = 26
    for f in range(steps + 6):
        k = min(f, steps)
        pair = []
        for after in (False, True):
            im = base.copy()
            if after:
                for k, row in ((0, 3), (1, 6), (2, 7)): im.alpha_composite(decos[k], (SIDE_X, row * 16))
                im.alpha_composite(back, (SIDE_X, SIDE_Y - G.OPEN0))
            # jugador que sube a la puerta de arriba: los pies van de la fila 7 al fondo del arco
            fx, fy = AX + 16, 118 - k * 3.4
            sp = up[f % len(up)]
            ox, oy = int(fx - n / 2), int(fy - n * .8)
            inside = fy < FACE_Y + 32 + 2
            im.alpha_composite(clip(sp, y0=FACE_Y + 6, ox=ox, oy=oy), (ox, oy))
            if inside:
                im.alpha_composite(dshade, (AX, FACE_Y)); im.alpha_composite(dfront, (AX, FACE_Y))
            # jugador que sale por la lateral: hacia la izquierda, a media altura del hueco
            gx, gy = 120 - k * 4.4, SIDE_Y + 26
            sp = side[f % len(side)].transpose(Image.FLIP_LEFT_RIGHT)
            ox, oy = int(gx - n / 2), int(gy - n * .8)
            im.alpha_composite(clip(sp, x0=SIDE_X, ox=ox, oy=oy), (ox, oy))
            if after:
                im.alpha_composite(front, (SIDE_X, SIDE_Y - G.OPEN0))
                im.alpha_composite(crown, (AX + 16 - G.CW // 2, FACE_Y - G.FACE_TOP))
            pair.append(im)
        fr = Image.new('RGBA', (W * 16 * 2 + 6, H * 16), (16, 16, 24, 255))
        fr.alpha_composite(pair[0], (0, 0)); fr.alpha_composite(pair[1], (W * 16 + 6, 0))
        out.append(fr.resize((fr.width * 3, fr.height * 3), Image.NEAREST).convert('RGB'))
    d = os.path.join(HERE, 'gif', 'escenas'); os.makedirs(d, exist_ok=True)
    out[0].save(os.path.join(d, f'escena_{name}.gif'), save_all=True, append_images=out[1:], duration=MS, loop=0)
    return out[12]

if __name__ == '__main__':
    stills = [scene(*t) for t in G.THEMES]
    w, h = stills[0].size
    sheet = Image.new('RGB', (w * 2 + 12, (h + 12) * 5), (16, 16, 24))
    for i, s in enumerate(stills): sheet.paste(s, ((i % 2) * (w + 12), (i // 2) * (h + 12)))
    sheet.save(os.path.join(HERE, 'vista_previa_escenas.png'))
    print('Listo')
