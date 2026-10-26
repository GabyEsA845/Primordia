"""Fosas del piso 2: tiles de fosa para las tres franjas (Someras, Arrecife y Abismo).

Uso: python generar_fosas.py

Vista cenital en 3/4, como las paredes: de la fosa se ve la pared del fondo (la que mira hacia la cámara), que cae
desde el suelo de arriba hacia la oscuridad; a los lados, un borde fino; abajo, solo el filo del suelo.

Salida en ../environment/<Tema>/pits/:
  Pit_<máscara>.png   16 tiles, 16×16. La máscara dice qué vecinos son suelo (o pared): N=1, E=2, S=4, O=8.
                      Pit_0 es fosa por los cuatro lados; Pit_1, fosa con suelo encima (se ve la pared que cae)…
  Pit_Corner_NE / NO / SE / SO.png   capas para las esquinas de dentro: la diagonal es suelo y los dos vecinos
                      de ese lado son fosa (se ponen encima del tile de la máscara).
  Pit_Motes.png       6 cuadros de 16×16 en bucle: motas que suben del fondo (capa de efecto, poner en algunas celdas).
  Pit_Tileset.png     los 16 tiles en una hoja de 4×4 (índice = máscara) para revisar.
"""
import math, os, random
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ENV = os.path.normpath(os.path.join(HERE, '..', 'environment'))
T = 16
N, E, S, W = 1, 2, 4, 8

THEMES = {
    'Shallows': dict(face=['#1b5062', '#266a7e', '#348498', '#52a2b0', '#90d4d4'], lip='#c8f4ee', vein='#e8c070',
                     deep=['#020a10', '#04141c', '#082430', '#0e3442'], mote=(160, 240, 230)),
    'Cambrian': dict(face=['#1c2c50', '#28406c', '#365890', '#4e78b0', '#80a8d8'], lip='#b0e0ec', vein='#d0705a',
                     deep=['#02050c', '#050c1a', '#0a1628', '#10203a'], mote=(150, 200, 240)),
    'Abyss': dict(face=['#0b101e', '#111a2e', '#182640', '#223456', '#38507e'], lip='#4a6aa0', vein='#36d8d0',
                  deep=['#000000', '#010206', '#03060e', '#060c18'], mote=(110, 240, 255)),
}


def C(h, a=255):
    h = h.lstrip('#')
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)) + (a,)


def mix(a, b, t):
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3)) + (255,)


def noise(seed):
    """Ruido que se repite cada 16 px en horizontal, para que las caras de las fosas encajen de un tile al siguiente."""
    rnd = random.Random(seed)
    ph = [rnd.uniform(0, math.tau) for _ in range(4)]
    return lambda x, y: (math.sin(x * math.tau / 16 + ph[0] + y * 0.7) * 0.5 + math.sin(x * math.tau / 8 + ph[1] - y * 1.3) * 0.3
                         + math.sin(x * math.tau / 16 * 3 + ph[2] + y * 2.1) * 0.2)


def pit_tile(th, mask, seed=0):
    face = [C(c) for c in th['face']]
    deep = [C(c) for c in th['deep']]
    lip, vein = C(th['lip']), C(th['vein'])
    nz = noise(seed)
    rnd = random.Random(mask * 31 + seed)
    im = Image.new('RGBA', (T, T))
    px = im.load()
    face_h = 10 if mask & N else 0
    for y in range(T):
        for x in range(T):
            # fondo: más claro arriba (cerca del borde) y negro hacia abajo, con un poco de textura
            depth = 0.55 + 0.45 * (y / (T - 1))
            k = min(3, max(0, int((1 - depth) * 4 + nz(x, y) * 0.6)))
            c = deep[k]
            if face_h and y < face_h:
                # pared del fondo: estratos que se oscurecen al bajar, con vetas del color del tema
                t = y / face_h
                band = 4 - min(4, int(t * 4.6 + nz(x, y * 0.6) * 0.8))
                c = face[max(0, band)]
                if y == 0: c = lip
                elif y == 1: c = mix(face[4], lip, 0.4)
                elif y >= face_h - 2: c = mix(face[0], deep[2], 0.5 + (y - face_h + 2) * 0.25)
                if 3 <= y <= 6 and abs(nz(x * 2, 9) - (y - 4.5) * 0.25) < 0.08: c = vein
            px[x, y] = c
    # bordes de los lados: filo de un píxel y sombra
    for y in range(face_h if mask & N else 0, T):
        if mask & W:
            px[0, y] = mix(lip, deep[1], 0.35); px[1, y] = mix(face[1], deep[1], 0.5)
        if mask & E:
            px[T - 1, y] = mix(lip, deep[1], 0.55); px[T - 2, y] = mix(face[0], deep[1], 0.5)
    if mask & S:   # filo del suelo de abajo: luz arriba, sombra corta
        for x in range(T):
            px[x, T - 1] = mix(lip, face[3], 0.5)
            if not (mask & N) or face_h < T - 3: px[x, T - 2] = mix(deep[0], face[0], 0.3)
    # esquinas exteriores: el filo de arriba dobla hacia abajo por el lado
    if mask & N and mask & W:
        for y in range(face_h): px[0, y] = mix(lip, face[2], 0.3)
    if mask & N and mask & E:
        for y in range(face_h): px[T - 1, y] = mix(lip, face[1], 0.5)
    # motas sueltas en el fondo
    for _ in range(2):
        x, y = rnd.randrange(2, T - 2), rnd.randrange(max(face_h + 1, 4), T - 2)
        px[x, y] = mix(deep[3], C('#ffffff'), 0.08)
    return im


def corner(th, which):
    """Esquina de dentro: la diagonal es suelo y los dos vecinos de ese lado son fosa."""
    face = [C(c) for c in th['face']]
    lip = C(th['lip'])
    im = Image.new('RGBA', (T, T), (0, 0, 0, 0))
    px = im.load()
    if which in ('NE', 'NW'):
        # un trocito de la pared del fondo que asoma en la esquina de arriba
        xs = range(T - 4, T) if which == 'NE' else range(0, 4)
        for x in xs:
            for y in range(0, 7):
                c = face[3 - min(3, y // 2)]
                if y == 0: c = lip
                px[x, y] = c
    else:
        xs = range(T - 3, T) if which == 'SE' else range(0, 3)
        for x in xs:
            px[x, T - 1] = mix(lip, face[3], 0.5)
    return im


def motes(th, k):
    im = Image.new('RGBA', (T, T), (0, 0, 0, 0))
    px = im.load()
    r, g, b = th['mote']
    for i, (x0, sp, ph) in enumerate(((4, 1.6, 0), (11, 1.2, 2.5), (8, 2.0, 4))):
        y = (T - 2) - ((k * sp * 2.2 + ph * 2) % (T - 3))
        x = x0 + round(math.sin(k * 0.9 + i) * 0.8)
        a = int(200 * (y / T))
        px[x, int(y)] = (r, g, b, a)
    return im


if __name__ == '__main__':
    for name, th in THEMES.items():
        out = os.path.join(ENV, name, 'pits')
        os.makedirs(out, exist_ok=True)
        sheet = Image.new('RGBA', (T * 4, T * 4))
        for mask in range(16):
            im = pit_tile(th, mask, seed=len(name))
            im.save(os.path.join(out, f'Pit_{mask}.png'))
            sheet.paste(im, ((mask % 4) * T, (mask // 4) * T))
        sheet.save(os.path.join(out, 'Pit_Tileset.png'))
        for w in ('NE', 'NW', 'SE', 'SW'):
            corner(th, w).save(os.path.join(out, f'Pit_Corner_{w.replace("W", "O")}.png'))
        ms = Image.new('RGBA', (T * 6, T))
        for k in range(6): ms.paste(motes(th, k), (k * T, 0))
        ms.save(os.path.join(out, 'Pit_Motes.png'))
    print('Fosas listas')
