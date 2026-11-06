"""Cápsula de ámbar (piso 3): animación de curación mientras el jugador está dentro (20 cuadros x 100 ms = 2 s).

Un PULSO por cada medio corazón curado (1, 2 ó 3: nivel I, II, III) + un pulso FINAL más fuerte justo antes de que se
agriete. Cada pulso es una onda de luz dorada que sube por el cristal, hace latir el borde y la luz del núcleo, tiñe a la
criatura de dorado un instante y suelta un CORAZÓN DORADO que sube ondulando por un lado y se desvanece. Dentro del
cristal suben motas de luz. Nivel III: halo exterior más fuerte y destellos en estrella con cada corazón.
Son capas SUPERPUESTAS a la cápsula en reposo (celda 96x60, eje x = 48, pies en y = 51; la criatura va entre Back y Front):
  Fx_AmberCapsule_Heal{1,2,3}_Back    núcleo cálido y motas (sobre el Back de la cápsula, detrás de la criatura)
  Fx_AmberCapsule_Heal{1,2,3}_Front   ondas de luz y latido del borde (sobre el Front de la cápsula)
  Fx_AmberCapsule_Heal{1,2,3}_Hearts  corazones y destellos (lo más arriba de todo)
  Fx_AmberCapsule_Heal{1,2,3}_Glow    halo exterior (detrás de todo)
Heal1 y Heal2 van con la cápsula de nivel I-II; Heal3 con la de nivel III. Los 20 cuadros se reproducen una vez.
Tinte dorado de la criatura (lo hace el código con un shader, curva `TINT`): mezcla hacia #fff0b0 con esa fuerza por cuadro.
Cuadro que cuenta: cada pulso cura a mitad de subida (el cuadro `s + 3` de cada inicio de pulso `STARTS`).
Uso: python generar_curacion.py
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import generar_rotura as rt
import generar_cristalizacion as cz
import generar_capsula_nivel3 as n3
from generar_capsula_nivel3 import AMB, rgba, H
from base_piso3 import *
from PIL import Image

BW, OX, CX = rt.BW, rt.OX, rt.CX
NF, MS = 20, 100
FINAL = 14
STARTS = {1: [2], 2: [2, 7], 3: [2, 6, 10]}                  # inicios de los pulsos normales (medios corazones curados)
FROG_AT = rt.FROG_AT

def pulses(n): return [(s, False) for s in STARTS[n]] + [(FINAL, True)]

def env(f, n):
    """Intensidad del latido en el cuadro f (0..~1,6): triángulo alrededor de s + 2 de cada pulso."""
    e = 0.0
    for s, fin in pulses(n): e += max(0.0, 1 - abs(f - (s + 2)) / 3.0) * (1.6 if fin else 1.0)
    return e
TINT = {n: [round(min(.45, env(f, n) * .3), 2) for f in range(NF)] for n in (1, 2, 3)}

def depth(mask, x, y, k=3):
    for d in range(1, k):
        if not all((x + dx * d, y + dy * d) in mask for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))): return d
    return k

# --- capas --------------------------------------------------------------------------------------------------------
def back_overlay(mask, n, f, rnd_seed):
    cv = Canvas(BW, H); e = env(f, n)
    for (x, y) in mask:
        r = math.hypot((x - CX) / 15.0, (y - 43) / 11.5)
        if r < 1:
            a = int((45 + 120 * min(e, 1.6)) * (1 - r))
            if a > 8: cv.put(x, y, rgba(AMB[3] if r > .35 else AMB[4], min(a, 215)))
    rnd = random.Random(rnd_seed); cells = [p for p in sorted(mask) if depth(mask, *p) >= 3]
    for i in range(9):                                                       # motas que suben despacio por dentro del cristal
        x0, y0 = rnd.choice(cells); y = y0 - (f * .8 + i * 2.3) % 18
        if (x0, round(y)) in mask: cv.put(x0, round(y), rgba(AMB[4], 220)); cv.put(x0, round(y) + 1, rgba(AMB[3], 110))
    return cv

def front_overlay(mask, n, f):
    cv = Canvas(BW, H); ys = [y for _, y in mask]; y0, y1 = min(ys), max(ys); e = env(f, n)
    for s, fin in pulses(n):
        t = f - s
        if 0 <= t <= 7:                                                      # la onda sube desde la base
            cy = y1 - t / 7.0 * (y1 - y0 + 4); th = 5 if fin else 3
            for (x, y) in mask:
                dy = abs(y - cy)
                if dy <= th and not rt.in_creature(x, y):
                    a = int((235 if fin else 185) * (1 - dy / (th + 1)))
                    cv.put(x, y, rgba(AMB[4] if dy <= 1 else AMB[3], a))
    if e > 0:                                                                # el borde late
        for (x, y) in mask:
            if depth(mask, x, y) <= 2: cv.put(x, y, rgba(AMB[4], int(min(1, e) * 120)))
    if f == FINAL + 2:                                                       # tras el pulso final, el cristal entero brilla
        for (x, y) in mask:
            if not rt.in_creature(x, y): cv.put(x, y, rgba(AMB[4], 40))
    return cv

def glow_layer(n, f, big):
    cv = Canvas(BW, H); e = min(env(f, n), 1.6); base = 55 if big else 30; s = base + (110 if big else 80) * e; rad = 36 if big else 31
    for y in range(H):
        for x in range(BW):
            r = math.hypot((x + .5 - CX) / rad, (y + .5 - 38) / (rad * .95))
            if r < 1:
                a = int(s * (1 - r) ** 1.5)
                if a > 4: cv.put(x, y, rgba(AMB[3], min(a, 200)))
    return cv

# --- corazones dorados ----------------------------------------------------------------------------------------------
HEART = ['.XXX.XXX.', 'XXXXXXXXX', 'XXXXXXXXX', 'XXXXXXXXX', '.XXXXXXX.', '..XXXXX..', '...XXX...', '....X....']
def heart_img():
    im = Image.new('RGBA', (11, 10)); px = im.load(); cells = {(x + 1, y + 1): (x, y) for y, row in enumerate(HEART) for x, ch in enumerate(row) if ch == 'X'}
    for (X, Y), (x, y) in cells.items():
        k = x + y; c = AMB[4] if k <= 3 else AMB[3] if k <= 8 else AMB[2] if k <= 11 else AMB[1]; px[X, Y] = tuple(c[:3]) + (255,)
    for X in range(11):
        for Y in range(10):
            if (X, Y) not in cells and any((X + dx, Y + dy) in cells for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))): px[X, Y] = tuple(AMB[0][:3]) + (255,)
    return im
HEART_IMG = heart_img()

def star(im, x, y, r, a=255):
    px = im.load(); col = tuple(AMB[4][:3]) + (a,)
    for d in range(-r, r + 1):
        for (xx, yy) in ((x + d, y), (x, y + d)):
            if 0 <= xx < BW and 0 <= yy < H: px[xx, yy] = col
    if r >= 2:
        for (dx, dy) in ((1, 1), (-1, 1), (1, -1), (-1, -1)):
            if 0 <= x + dx < BW and 0 <= y + dy < H: px[x + dx, y + dy] = tuple(AMB[3][:3]) + (a,)

def hearts_layer(n, f, big):
    im = Image.new('RGBA', (BW, H)); side0 = 28 if big else 26
    for j, s in enumerate(STARTS[n]):
        t = f - (s + 1)
        if t < 0 or t > 9: continue
        side = -1 if j % 2 == 0 else 1; ph = j * 1.7
        x = CX + side * (side0 + (j // 2) * 3) + math.sin(t * .85 + ph) * 3; y = 50 - t * 4.6 - j * 2
        al = 255 if t <= 6 else int(255 * (9 - t) / 3.0)
        h = HEART_IMG.copy(); h.putalpha(h.getchannel('A').point(lambda v: v * al // 255))
        im.alpha_composite(h, (int(round(x - 5)), int(round(y - 5))))
        if t <= 1: star(im, int(round(x + 6 * side)), int(round(y - 5)), 3 if big else 2, 255 if t == 0 else 170)
        if big and t % 3 == 1: star(im, int(round(x - 6 * side)), int(round(y + 3)), 1, 220)
    return im

# --- montaje -----------------------------------------------------------------------------------------------------
def tint(img, k):
    if k <= 0: return img
    out = img.copy(); px = out.load(); g = AMB[4]
    for y in range(out.height):
        for x in range(out.width):
            r, gg, b, a = px[x, y]
            if a: px[x, y] = (int(r + (g[0] - r) * k), int(gg + (g[1] - gg) * k), int(b + (g[2] - b) * k), a)
    return out

def stage(base_back, base_front, parts, f, body, tk, veins):
    gl, bk, fr, hr = parts
    im = Image.new('RGBA', (BW, H), C('#2b2318')); im.alpha_composite(Image.new('RGBA', (BW, H - 51), C('#33291c')), (0, 51))
    if veins is not None: im.alpha_composite(veins.img(), (0, cz.VOY))
    im.alpha_composite(gl.img())
    sh = Canvas(BW, H)
    for y in range(53, 57):
        for x in range(CX - 22, CX + 23):
            if ((x + .5 - CX) / 21.0) ** 2 + ((y + .5 - 54.5) / 1.8) ** 2 <= 1: sh.put_out(x, y, (24, 20, 37, 90))
    im.alpha_composite(sh.img()); im.alpha_composite(base_back.img()); im.alpha_composite(bk.img())
    im.alpha_composite(tint(body, tk), FROG_AT); im.alpha_composite(base_front.img()); im.alpha_composite(fr.img()); im.alpha_composite(hr)
    return im

if __name__ == '__main__':
    out = os.path.join(HERE, 'curacion'); os.makedirs(out, exist_ok=True)
    body = n3.frog(); b1, f1 = cz.capsule1(); b3, f3, _ = cz.capsule3()
    layers = {False: (rt.wide(b1), rt.wide(f1)), True: (rt.wide(b3), rt.wide(f3))}
    vdata = cz.floor_veins(); vfinal = cz.veins_frame(vdata, 11)
    S = 6; runs = {}
    for n in (1, 2, 3):
        big = n == 3; bb, ff = layers[big]; mask = set(bb.px); frames = []; sheets = {k: [] for k in ('Back', 'Front', 'Hearts', 'Glow')}
        for f in range(NF):
            parts = (glow_layer(n, f, big), back_overlay(mask, n, f, 7 + n), front_overlay(mask, n, f), hearts_layer(n, f, big))
            for k, v in zip(('Glow', 'Back', 'Front', 'Hearts'), parts): sheets[k].append(v)
            frames.append(stage(bb, ff, parts, f, body, TINT[n][f], vfinal if big else None))
        for k, fr in sheets.items():
            s = Image.new('RGBA', (BW * NF, H))
            for i, v in enumerate(fr): s.alpha_composite(v if isinstance(v, Image.Image) else v.img(), (i * BW, 0))
            s.save(os.path.join(out, f'Fx_AmberCapsule_Heal{n}_{k}.png'))
        runs[n] = frames
        os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True)
        big_f = [i.resize((i.width * S, i.height * S), Image.NEAREST).convert('RGB') for i in frames]
        big_f[0].save(os.path.join(HERE, 'gif', f'curacion_{n}.gif'), save_all=True, append_images=big_f[1:], duration=MS, loop=0)
    s2 = 2; cw, ch = BW * s2, H * s2; cols = 10
    contact = Image.new('RGB', (cw * cols + 2 * cols + 2, (ch + 2) * 6 + 2), (20, 16, 12))
    for r, n in enumerate((1, 2, 3)):
        for k in range(NF):
            contact.paste(runs[n][k].resize((cw, ch), Image.NEAREST).convert('RGB'), (2 + (k % cols) * (cw + 2), 2 + (r * 2 + k // cols) * (ch + 2)))
    contact.save(os.path.join(HERE, 'vista_previa_curacion.png')); print('ok', contact.size)
