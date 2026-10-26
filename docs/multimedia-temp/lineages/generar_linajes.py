"""
generar_linajes.py - Primordia: arte de los beneficios de los linajes (rama feature/lineage-benefits).

  HUD/      UI_Heart_Barrier         medio corazón verde de la barrera (Pastar biomasa), 16x16
  Effects/  FungusMat                mata de hongo de Colonizar la sala (radio 1,2 = 40x40), en bucle
            FungusMat_Large          la de Colonia grande del Moho (radio 1,56 = 52x52)
            StingingTrail            rastro de tentáculos de la Medusa (radio 0,45 = 16x16), en bucle

Reutiliza generar_mutaciones_piso1.py (hifas, paletas) y generar_bocas.py (rojo y lunares de la Amanita).

Uso:
  python generar_linajes.py [carpeta_multimedia_temp] [carpeta_salida]
"""
import importlib.util
import math
import os
import random
import sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, '..')
OUT = sys.argv[2] if len(sys.argv) > 2 else HERE


def _load(name, rel):
    argv = sys.argv
    sys.argv = [name, ROOT, os.devnull]
    spec = importlib.util.spec_from_file_location(name, os.path.join(ROOT, rel))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    sys.argv = argv
    return module


F1 = _load('generar_mutaciones_piso1', 'floor1-mutations/generar_mutaciones_piso1.py')
B = _load('generar_bocas', 'mouth-tweaks/generar_bocas.py')
M = F1.M
C, CA = M.C, M.CA
Cv = F1.Cv
shade_v, ramp_idx = M.shade_v, M.ramp_idx
HYPHA = F1.HYPHA
CAP, DOT, GILL = B.CAP, B.DOT, B.GILL
PURP, LIME = M.PURP, M.LIME
BG = C('#16352e')


# ======================================================= 1. CORAZÓN VERDE
GREEN_OF = {
    (110, 40, 56): (34, 90, 52),
    (200, 56, 74): (64, 170, 84),
    (240, 112, 110): (130, 220, 110),
    (255, 208, 192): (210, 255, 180),
    (255, 240, 224): (236, 255, 220),
}


def barrier_heart():
    """El medio corazón del HUD, en verde (los mismos píxeles, otra paleta)."""
    im = Image.open(os.path.join(ROOT, 'ui', 'HUD', 'UI_Heart_Half.png')).convert('RGBA')
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a and (r, g, b) in GREEN_OF:
                px[x, y] = GREEN_OF[(r, g, b)] + (a,)
    return im


# =========================================================== 2. MATA DE HONGO
def mushroom(cv, x, y, size, sway, red=True):
    """Seta vista desde arriba y un poco de lado: pie crema y sombrero (rojo con lunares o pardo)."""
    w, h = 1.4 * size, 1.0 * size
    stem = max(1, int(round(size * 0.9)))
    for k in range(stem):
        cv.put(x + sway * k / max(1, stem), y - k, GILL[1] if k else GILL[0])
    tx, ty = x + sway, y - stem
    ramp = CAP if red else ['#3e2a1c', '#6a4a2e', '#9a7048', '#c8a070']
    for yy in range(int(ty - h - 1), int(ty + 2)):
        for xx in range(int(tx - w - 1), int(tx + w + 2)):
            nx, ny = (xx + .5 - tx) / w, (yy + .5 - ty) / h
            if ny > 0.25 or nx * nx + ny * ny > 1:
                continue
            cv.put(xx, yy, ramp[ramp_idx(shade_v(nx, ny * 1.2 + 0.25))])
    if red and size >= 2:
        cv.put_in(tx - w * 0.35 - .5, ty - h * 0.5 - .5, DOT[1])
        cv.put_in(tx + w * 0.3 - .5, ty - h * 0.3 - .5, DOT[1])


MOSS = ['#2e3c22', '#3e5028', '#566a32', '#76883e']


def fungus_mat(f, n, seed):
    """
    Mata de hongo en el suelo, como un anillo de hadas: musgo translúcido, un corro de setas que se mecen y unas
    hifas que salen hacia fuera; a ratos una seta suelta una espora.
    """
    rnd = random.Random(seed)
    cv = Cv(n)
    c = n / 2
    R = c - 1.5
    ring = R * 0.62
    # musgo: disco translúcido con grano, más oscuro en el anillo
    for y in range(n):
        for x in range(n):
            d = math.hypot(x + .5 - c, (y + .5 - c) / 0.85)
            if d > R * 0.92:
                continue
            noise = rnd.random()
            on_ring = abs(d - ring) < 1.6
            i = 0 if on_ring else (1 if noise < 0.55 else 2 if noise < 0.9 else 3)
            a = 150 if on_ring else int(120 - 50 * d / R)
            cv.put(x, y, CA(MOSS[i], a))
    # hifas que salen del anillo hacia fuera
    for k in range(16):
        a = k * 2 * math.pi / 16 + rnd.uniform(-0.15, 0.15)
        root = (c + math.cos(a) * ring, c + math.sin(a) * ring * 0.85)
        F1.hypha(cv, root, a, R * 0.38 + rnd.uniform(-1, 1), f * 0.6 + k, 0.3, False, 160)
    # corro de setas, de atrás adelante (por y)
    caps = []
    count = 8 if n > 44 else 6
    for k in range(count):
        a = k * 2 * math.pi / count + rnd.uniform(-0.2, 0.2)
        caps.append((c + math.cos(a) * ring, c + math.sin(a) * ring * 0.85 + 2, rnd.choice((2.6, 3.0, 3.4)), k % 3 != 2, k))
    for (x, y, size, red, k) in sorted(caps, key=lambda m: m[1]):
        sway = 0.7 * math.sin(f * math.pi / 2 + k * 1.3)
        mushroom(cv, x, y, size, sway, red)
    # una espora que sube en los cuadros 1 y 2
    if f in (1, 2):
        top = min(caps, key=lambda m: m[1])
        cv.put(top[0] + 1, top[1] - 7 - f * 2, CA(B.SPORE[2], 230))
        cv.put(top[0] + 2, top[1] - 7 - f * 2, CA(B.SPORE[1], 200))
    cv.outline(110)
    return cv


# ====================================================== 3. ESTELA URTICANTE
def stinging_trail(f):
    """Tentáculos urticantes sueltos en el suelo, morado translúcido con nematocistos lima (16x16)."""
    cv = Cv(16)
    rnd = random.Random(7)
    for k in range(5):
        a = k * 2 * math.pi / 5 + rnd.uniform(-0.3, 0.3)
        x, y = 8 + math.cos(a) * 1.2, 8 + math.sin(a) * 1.0
        L = rnd.uniform(4.5, 6.5)
        steps = int(L / 0.5)
        for s in range(steps):
            t = s / steps
            aa = a + 0.5 * math.sin(f * math.pi / 2 + k + t * 4)
            x += math.cos(aa) * 0.5
            y += math.sin(aa) * 0.5
            cv.put(x, y, CA(PURP[2] if t < 0.6 else PURP[3], 200))
            if s % 3 == 2:
                cv.put(x + 1, y, CA(LIME[2], 230))
    for (dx, dy) in ((0, 0), (1, 0), (0, 1), (1, 1)):
        cv.put(7 + dx, 7 + dy, CA(PURP[1], 210))
    return cv


# ================================================================ guardar
SHEETS = {}


def save(group, name, frames, ms=None):
    frames = [f if isinstance(f, Image.Image) else f.img() for f in frames]
    w, h = frames[0].size
    os.makedirs(os.path.join(OUT, group), exist_ok=True)
    sh = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, im in enumerate(frames):
        sh.paste(im, (i * w, 0))
    sh.save(os.path.join(OUT, group, name + '.png'))
    if len(frames) > 1:
        os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
        sc = max(1, min(8, 240 // max(w, h)))
        out = []
        for im in frames:
            b = Image.new('RGBA', (w, h), BG)
            b.alpha_composite(im)
            out.append(b.resize((w * sc, h * sc), Image.NEAREST).convert('RGB'))
        out[0].save(os.path.join(OUT, 'gif', name + '.gif'), save_all=True, append_images=out[1:],
                    duration=ms or 100, loop=0)
    SHEETS[name] = frames


def build():
    save('HUD', 'UI_Heart_Barrier', [barrier_heart()])
    save('Effects', 'FungusMat', [fungus_mat(f, 40, 3) for f in range(4)], 220)
    save('Effects', 'FungusMat_Large', [fungus_mat(f, 52, 5) for f in range(4)], 220)
    save('Effects', 'StingingTrail', [stinging_trail(f) for f in range(4)], 120)


def preview(path):
    S = 4
    floor = Image.new('RGBA', (240, 70), BG)
    heart = Image.open(os.path.join(ROOT, 'ui', 'HUD', 'UI_Heart_Full.png')).convert('RGBA')
    for i in range(3):
        floor.alpha_composite(heart, (4 + i * 16, 4))
    floor.alpha_composite(SHEETS['UI_Heart_Barrier'][0], (52, 4))
    floor.alpha_composite(SHEETS['FungusMat'][0], (76, 14))
    floor.alpha_composite(SHEETS['FungusMat_Large'][1], (120, 9))
    for k in range(5):
        floor.alpha_composite(SHEETS['StingingTrail'][k % 4], (176 + k * 8, 30 + (k % 2) * 3))
    cell = Image.open(os.path.join(ROOT, 'player', 'Player_Idle_side.png')).convert('RGBA').crop((0, 0, 16, 16))
    floor.alpha_composite(cell, (90, 26))
    floor.resize((floor.width * S, floor.height * S), Image.NEAREST).convert('RGB').save(path)


def main():
    build()
    preview(os.path.join(OUT, 'vista_previa_linajes.png'))
    for k, v in SHEETS.items():
        print(k, len(v), f'{v[0].width}x{v[0].height}')


if __name__ == '__main__':
    main()
