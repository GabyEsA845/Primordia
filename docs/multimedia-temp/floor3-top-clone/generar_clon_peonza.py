"""Clon de la Peonza (nivel III, Bumerán) del Diplocaulus, piso 3.

El clon es el mismo Diplocaulus girando (sale de Player_Diplocaulus_Spin, ya aprobado), recoloreado como un doble translúcido
de salmón claro a crema, con el contorno claro en lugar del oscuro, brillo en el centro y un destello que lo recorre.
- Fx_SpinClone_Spin: el clon girando, en bucle (igual que Spin).
- Fx_SpinClone_Whirl: remolino propio pequeño (radio 0,7 u, el radio con el que golpea), en sentido horario como el giro.
- Fx_SpinClone_Appear: se despega del jugador (destello que se vuelve clon). Fx_SpinClone_Merge: se funde al volver.
Los ecos que se desvanecen detrás los pone el código con cuadros anteriores. Escena de revisión en gif/escenas/.

Uso: python generar_clon_peonza.py [carpeta_salida]   (por defecto, la carpeta de este script)
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
SKILL = os.path.join(HERE, '..', '..', '..', '.claude', 'skills', 'primordia-sprites', 'scripts')
sys.path.insert(0, os.path.abspath(SKILL))
from pixel import *
from PIL import Image

OUT = sys.argv[1] if len(sys.argv) > 1 else HERE
BG = C('#1c2e20')
SRC = os.path.join(HERE, '..', 'floor3-bodies', 'Diplocaulus', 'Player_Diplocaulus_Spin.png')
N = 40
DIPLO = ramp('#d8806a', 5)
CLONE = ['#e07a64', '#f0947a', '#ffb494', '#ffd4b8', '#fff0e0']        # de salmón claro a crema
CLONE_OUT = '#ffe2cc'                                                  # contorno claro
GLOW = '#fff8ec'
BODY_A, OUT_A = 195, 240
SPIN_MS, WHIRL_MS, FX_MS = 40, 40, 50

def load_frames(path, cell=N):
    im = Image.open(path).convert('RGBA')
    return [im.crop((i * cell, 0, (i + 1) * cell, cell)) for i in range(im.width // cell)]

def lum(c):
    return .3 * c[0] + .59 * c[1] + .11 * c[2]

def recolor(src, i, flash=0.0):
    """Doble translúcido: contorno -> claro; el resto, por luminancia, a la rampa del clon. flash 0..1 lo acerca al blanco."""
    px = {(x, y): src.getpixel((x, y)) for y in range(N) for x in range(N) if src.getpixel((x, y))[3] > 0}
    body = [p for p, c in px.items() if c[:3] != OUTL[:3]]
    ls = sorted({lum(px[p]) for p in body})
    cv = Canvas(N)
    cx = sum(p[0] for p in body) / max(1, len(body)); cy = sum(p[1] for p in body) / max(1, len(body))
    shimmer = (i * 5) % 24 - 4                                          # franja de destello que baja por el clon
    for p, c in px.items():
        if c[:3] == OUTL[:3]:
            cv.put(p[0], p[1], C(CLONE_OUT, OUT_A)); continue
        k = ls.index(lum(c)) / max(1, len(ls) - 1)                      # 0 oscuro .. 1 claro
        idx = min(4, int(k * 4.2))
        if flash: idx = min(4, idx + int(round(flash * 3)))
        a = BODY_A
        if math.hypot(p[0] - cx, p[1] - cy) < 2.2: idx, a = 4, 235         # brillo en el centro
        if abs((p[1] - cy) - shimmer + (p[0] - cx) * .5) < .8: idx, a = min(4, idx + 1), a + 30
        col = CLONE[idx] if flash < .9 else GLOW
        cv.put(p[0], p[1], C(col, min(255, int(a + flash * 60))))
    return cv

def echo(src, i):
    """Eco que queda detrás: en damero (un píxel sí y otro no) y casi opaco, así no se agrisa sobre el suelo; sin contorno."""
    cv = recolor(src, i, flash=.35)
    for p, c in list(cv.px.items()):
        if c[:3] == C(CLONE_OUT)[:3] or (p[0] + p[1]) % 2: del cv.px[p]
        else: cv.px[p] = c[:3] + (225,)
    return cv

def whirl(i, n=32, r=11):
    """Remolino propio: 3 arcos que se desvanecen por la cola, girando 30° por cuadro en sentido horario (bucle de 120°)."""
    cv = Canvas(n); c = n / 2
    for k in range(3):
        a0 = math.radians(k * 120 + i * 30)
        for t in range(26):
            a = a0 - math.radians(t * 3.4)                             # la cola, detrás del sentido de giro
            rr = r + (.6 if t < 8 else 0) - t * .03
            x, y = c + math.cos(a) * rr, c + math.sin(a) * rr * .9
            col, al = (GLOW, 240) if t < 3 else (CLONE[3], 225) if t < 10 else (CLONE[2], 190) if t < 18 else (CLONE[1], 140)
            cv.put(x, y, C(col, al))
            if t < 6: cv.put(c + math.cos(a) * (rr - 1), c + math.sin(a) * (rr - 1) * .9, C(CLONE[3], 170))
    rnd = random.Random(i)
    for k in range(3):                                                  # motas de velocidad sueltas
        a = rnd.uniform(0, 2 * math.pi); d = r + rnd.uniform(1.5, 3.5)
        cv.put(c + math.cos(a) * d, c + math.sin(a) * d * .9, C(CLONE[2], 140))
    return cv

def appear(i, frames):
    """Se despega del jugador: 0 silueta clara del jugador, 1 a medio camino, 2 ya es el clon."""
    return recolor(frames[i], i, flash=(1.0, .55, .15)[i])

def merge(i, frames):
    """Se funde al volver: 0 se aclara, 1 se recoge en un núcleo con anillo, 2 destello y anillo que se abre."""
    c = N / 2
    if i == 0: return recolor(frames[0], 0, flash=.7)
    cv = Canvas(N)
    rr = (0, 5.5, 2.5)[i]
    ring = (0, 8, 12)[i]
    for k in range(48):
        a = k * 2 * math.pi / 48
        if i == 2 and k % 3 == 0: continue
        cv.put(c + math.cos(a) * ring, c + math.sin(a) * ring * .9, C(CLONE[3] if i == 1 else CLONE[2], 200 if i == 1 else 140))
    if rr:
        t = Canvas(N); ellipse(t, c, c, rr, rr * .9, CLONE[1:5], alpha=215)
        for p, v in t.px.items(): cv.px[p] = v
    sparks(cv, c, c, 2 if i == 1 else 3)
    return cv

# =================================================================================================
# Escena de revisión
# =================================================================================================
def ground(w, h, seed=3, tones=('#1c2e20', '#213626', '#273f2b', '#2f4a32')):
    rnd = random.Random(seed)
    im = Image.new('RGBA', (w, h), C(tones[0]))
    for _ in range(w * h // 18):
        x, y = rnd.randrange(w), rnd.randrange(h)
        t = C(tones[rnd.choice((1, 1, 1, 2, 2, 3))])
        for dx in range(rnd.choice((1, 2, 3))):
            if x + dx < w: im.putpixel((x + dx, y), t)
    return im

def fade(im, k):
    im = im.copy(); a = im.getchannel('A').point(lambda v: int(v * k)); im.putalpha(a); return im

def scene(path, E, player):
    W, H = 220, 90
    bg = ground(W, H)
    px, py = 34, 45                                                     # jugador (girando)
    speed = 14 * 16 * SPIN_MS / 1000                                    # px por cuadro (14 u/s)
    rng = 9 * 16
    pos = []                                                            # x del clon por cuadro: ida, vuelta
    d = 0
    while d < rng: pos.append(d); d += speed
    while d > 0: pos.append(d); d -= speed
    frames = []
    total = 6 + len(pos) + 3 + 8
    for f in range(total):
        im = bg.copy()
        im.alpha_composite(as_img(player[f % 8]), (px - N // 2, py - N // 2))
        k = f - 6
        if 0 <= k < len(pos):
            x = px + pos[k]
            for e, al in ((2, .55), (1, 1.0)):                            # ecos
                if k - e >= 0:
                    im.alpha_composite(fade(as_img(E['Fx_SpinClone_Echo'][(f - e) % 8]), al), (int(px + pos[k - e] - N // 2), py - N // 2))
            im.alpha_composite(as_img(E['Fx_SpinClone_Whirl'][f % 4]), (int(x - 16), py - 16))
            spr = E['Fx_SpinClone_Appear'][k] if k < 3 else E['Fx_SpinClone_Spin'][f % 8]
            im.alpha_composite(as_img(spr), (int(x - N // 2), py - N // 2))
        elif len(pos) <= k < len(pos) + 3:
            im.alpha_composite(as_img(E['Fx_SpinClone_Merge'][k - len(pos)]), (px - N // 2, py - N // 2))
        frames.append(im)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    big = [fr.resize((W * 3, H * 3), Image.NEAREST).convert('RGB') for fr in frames]
    big[0].save(path, save_all=True, append_images=big[1:], duration=SPIN_MS, loop=0)
    return frames

if __name__ == '__main__':
    spin = load_frames(SRC)
    E = {
        'Fx_SpinClone_Spin': [recolor(f, i) for i, f in enumerate(spin)],
        'Fx_SpinClone_Echo': [echo(f, i) for i, f in enumerate(spin)],
        'Fx_SpinClone_Whirl': [whirl(i) for i in range(4)],
        'Fx_SpinClone_Appear': [appear(i, spin) for i in range(3)],
        'Fx_SpinClone_Merge': [merge(i, spin) for i in range(3)],
    }
    MS = {'Echo': SPIN_MS, 'Spin': SPIN_MS, 'Whirl': WHIRL_MS, 'Appear': FX_MS, 'Merge': FX_MS}
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    for name, frames in E.items():
        save_sheet(os.path.join(OUT, 'Effects', name + '.png'), frames)
        gif(os.path.join(OUT, 'gif', name + '.gif'), frames, next(v for k, v in MS.items() if k in name), BG, scale=5)
    E2 = dict(E); E2['Player_Diplocaulus_Spin (original)'] = spin
    preview(os.path.join(OUT, 'vista_previa_clon.png'), E2, BG, scale=5)
    scene(os.path.join(OUT, 'gif', 'escenas', 'escena_bumeran.gif'), E, spin)
    print('Listo:', OUT)
