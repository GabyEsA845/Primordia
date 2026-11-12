"""Veneno del jugador que arde (piso 3): Escupitajo + Escamas carbonizadas III (Llamarada) o cualquier fuego que prenda
un charco salpicado del jugador.

- Fx_PoisonSplat_Ignite (64×64, 6 cuadros): el fuego nace en el centro (donde revienta la bola) y corre hacia fuera
  por la forma de la salpicadura; llamas bajas con base lima y punta naranja. El veneno se vuelve mostaza al quemarse y
  el último cuadro es la mancha quemada (el código la desvanece). Cuadro que cuenta: el 3 (sale la bruma).
- Fx_PoisonHaze_Form/Idle/Fade (48×32): bruma tóxica baja a ras de suelo, mostaza más clara y translúcida que la
  columna del escenario (Fx_ToxicCloud_*), ancha y plana (~2,5×1 tiles); se arrastra despacio y suelta volutas cortas.
Escenas de revisión en gif/escenas/ (antes y ahora con la Camada). Skill primordia-sprites (pixel.py).

Uso: python generar_veneno_ardiendo.py
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
MT = os.path.normpath(os.path.join(HERE, '..'))
sys.path.insert(0, os.path.join(MT, '..', '..', '.claude', 'skills', 'primordia-sprites', 'scripts'))
for d in ('floor3-reactions', 'floor3-spit'):
    sys.path.insert(0, os.path.join(MT, d))
import generar_reacciones_piso3 as R                   # cloud(), vflame(), POISON_FIRE, TOX, VEN, EMB, columna y charco de antes
import generar_escupitajo as E                         # splat_mask(), splat_paint(), ground()
from pixel import *                                    # noqa: F401,F403
from PIL import Image

OUT = HERE
BG = C('#1c2e20')
VEN, TOX, EMB = R.VEN, R.TOX, R.EMB
HAZE = ['#5a5e22', '#8e9034', '#bcbc4c', '#e8e682']    # mostaza más clara que TOX: sombra algo más fría, luz más cálida
N = E.SPLAT_N
MS = {'Fx_PoisonSplat_Ignite': 80, 'Fx_PoisonHaze_Form': 90, 'Fx_PoisonHaze_Idle': 130, 'Fx_PoisonHaze_Fade': 110}

# =================================================================================================
# 1. Charco salpicado que se prende (64×64, mismo centro y escala que Fx_PoisonSplat_*)
# =================================================================================================
def splat_ignite(i, n=6):
    c = N / 2
    full = E.splat_mask(N, E.SPLAT_R)
    last = i == n - 1
    pts = E.splat_mask(N, E.SPLAT_R, dry=.45) if last else full
    cv = E.splat_paint(N, pts, dry=.3 if last else 0.0)
    R_ = (5, 12, 21, 32, 32, 32)[i]                       # frente del fuego desde el centro
    burnt = (.0, .25, .45, .65, .85, 1.0)[i]
    for p, col in list(cv.px.items()):                    # lo que ya ha prendido se vuelve mostaza y pierde brillo
        d = math.hypot(p[0] + .5 - c, (p[1] + .5 - c) / .72)
        k = burnt * max(0, min(1, (R_ - d) / 6 + .4)) if not last else 1.0
        k = round(k * 3) / 3                              # 3 escalones: pocos colores
        if k <= 0: continue
        lum = (col[0] * .3 + col[1] * .59 + col[2] * .11) / 255
        tgt = C(TOX[0] if lum < .25 or (last and lum < .42) else TOX[1] if lum < .42 or last else TOX[2])   # la mancha final, oscura
        cv.px[p] = R.blend(col, tgt, int(255 * k))[:3] + (int(col[3] * (1 - .2 * k)),)
    if not last:
        rnd = random.Random(11)
        sites = []                                        # sitios sueltos (no en cuadrícula), separados al menos 5 px
        for (x, y) in sorted(full, key=lambda q: random.Random(q[0] * 97 + q[1]).random()):
            if 6 < y and all(abs(x - a) + abs(y - b) >= 6 for a, b in sites): sites.append((x, y))
            if len(sites) >= 14: break
        for (x, y) in sorted(sites, key=lambda q: q[1]):
            d = math.hypot(x - c, (y - c) / .72)
            if d > R_ or (x, y) not in full: continue
            age = (R_ - d) / 10                           # lo recién prendido es bajo; lo de dentro ya se apaga en el 4
            h = min(1, .45 + age) * (5, 7, 8, 9, 6, 0)[i] * random.Random(x * 13 + y).uniform(.6, 1.15)
            if i == 4 and d < 14: h -= 3
            if h >= 2: R.vflame(cv, x, y, min(h, y - 2), i * 1.4 + x * .7, R.POISON_FIRE, w=1.5)
    if i == 0: sparks(cv, c, c - 1, 1)
    if i >= 3:                                            # ascuas en la parte ya quemada
        for (x, y) in ((25, 30), (33, 34), (39, 29), (29, 37), (21, 34), (36, 25)):
            if (x, y) in pts and (i == 5 or math.hypot(x - c, (y - c) / .72) < 12):
                cv.put(x, y, C(EMB[3])); cv.put(x + 1, y, C(EMB[2]))
    return cv

# =================================================================================================
# 2. Bruma tóxica baja (48×32, centro de la bruma en (24, 20); pivote en el centro de la celda +4 px abajo)
# =================================================================================================
HX, HY = 24, 20
HN = 6                                                    # cuadros del bucle

def haze_cloud(cv, blobs, pal, mul=0.8, specks=()):
    """Como R.cloud() pero con bolas aplastadas (x, y, rx, ry, a) y más transparente: es una bruma, no una columna."""
    if not blobs: return
    for y in range(cv.h):
        for x in range(cv.w):
            d = wa = 0.0; best = None; bw = 0
            for b in blobs:
                q = ((x + .5 - b[0]) / b[2]) ** 2 + ((y + .5 - b[1]) / b[3]) ** 2   # b[5] (opcional): tonos más claros
                if q < 1:
                    w = (1 - q) ** 2; d += w; wa += w * b[4]
                    if w > bw: bw, best = w, b
            if d < 0.08: continue
            lit = -(x - best[0]) * 0.3 - (y - best[1]) * 0.9
            k = 3 if d > 0.85 and lit > best[3] * 0.35 else 2 if d > 0.45 else 1 if d > 0.2 else 0
            a = (95, 150, 190, 215)[k] * mul * wa / d
            k = min(3, k + (best[5] if len(best) > 5 else 0))
            if a > 12: cv.put(x, y, R.Ca(pal[k], a))
    for (x, y, col, a) in specks:
        if (int(x), int(y)) in cv.px: cv.put(x, y, R.Ca(col, a))

# Bolas de la bruma: (dx, dy, rx, ry, fase); se mecen de lado y respiran
HBASE = [(-13, 1, 7, 3.6, 0), (-5, -1, 8, 4.6, 1.7), (5, 0, 8, 4.4, 3.1), (13, 1, 7, 3.6, 4.4), (0, 2, 9, 3.4, 2.4)]

def haze_blobs(ph, grow=1.0, fade=0.0):
    out = []
    for (dx, dy, rx, ry, f) in HBASE:
        a_ = ph * 2 * math.pi + f
        x = HX + dx * (.45 + .55 * grow) * (1 + fade * .35) + math.sin(a_) * 1.6
        y = HY + dy + math.cos(a_) * .6 - fade * 1.5
        s = (.35 + .65 * grow) * (1 + .06 * math.sin(a_ * 1.0 + 1))
        out.append((x, y, rx * s * (1 + fade * .25), ry * s * (1 - fade * .3), (1 - fade) * (.6 + .4 * grow)))
    for k in range(3):                                    # volutas cortas que se levantan y se deshacen
        p = (ph + k / 3) % 1
        if grow < .6: continue
        x = HX + (-9, 2, 10)[k] + math.sin(p * 4 + k) * 1.5
        y = HY - 2 - p * 8                                # nacen dentro de la bruma, así no flotan sueltas
        r = 3.4 + p * 1.6
        out.append((x, y, r * 1.4, r * .75, (1 - p) ** 1.5 * .7 * (1 - fade), 1))   # claras: vapor, no grumos
    return out

def haze_specks(ph, fade=0.0):
    out = []
    for k in range(3):                                    # motas de veneno quemado que suben
        p = (ph * 2 + k / 3) % 1
        x = HX + (-6, 3, 9)[k] + math.sin(k * 2.1 + p * 5) * 2
        y = HY - 1 - p * 10
        out.append((x, y, VEN[2] if k % 2 else HAZE[3], 200 * (1 - p) * (1 - fade)))
    return out

def haze(ph, grow=1.0, fade=0.0):
    cv = Canvas(48, 32)
    haze_cloud(cv, haze_blobs(ph, grow, fade), HAZE, specks=haze_specks(ph, fade) if grow > .6 else ())
    return cv

def haze_sheets():
    form = [haze((i - 4) / HN % 1, grow=(i + 1) / 4) for i in range(4)]   # acaba donde empieza el bucle
    idle = [haze(i / HN) for i in range(HN)]
    fade = [haze(i / HN, fade=(i + 1) / 4) for i in range(4)]
    return {'Fx_PoisonHaze_Form': form, 'Fx_PoisonHaze_Idle': idle, 'Fx_PoisonHaze_Fade': fade}

# =================================================================================================
# 3. Escena de revisión: la Camada revienta 6 bolitas con Llamarada. Antes (charco ovalado + columna) y ahora.
# =================================================================================================
def scaled(cv, s):
    im = as_img(cv)
    return im.resize((max(1, round(im.width * s)), max(1, round(im.height * s))), Image.NEAREST)

def scene(path, new, tones, seconds=6.5, ms=80):
    W, H = 176, 120
    bg = E.ground(W, H, tones=tones)
    rnd = random.Random(5)
    hits = [(28 + k * 24 + rnd.randint(-6, 6), 56 + (k % 2) * 34 + rnd.randint(-5, 5), k * .12) for k in range(6)]
    s = .45 / (14 / 16)                                   # escala real del charco pequeño de la Camada (SmallPuddleRadius)
    appear = [scaled(f, s) for f in E.splat_sheets()['Fx_PoisonSplat_Appear']]
    if new:
        ign = [scaled(splat_ignite(i), s) for i in range(6)]
        hs = haze_sheets(); h_ms = (90, 130, 110)
        form, idle, fade = ([scaled(f, .75) for f in hs[k]] for k in ('Fx_PoisonHaze_Form', 'Fx_PoisonHaze_Idle', 'Fx_PoisonHaze_Fade'))
        anchor = lambda im, x, y: (x - im.width / 2, y - im.height / 2 - 4 * s)
    else:
        ign = [scaled(R.puddle_ignite(i), .45 / 1.2) for i in range(6)]        # como lo escala hoy el código
        ts = R.toxic_sheets(); h_ms = (100, 120, 110)
        form, idle, fade = ([scaled(f, 1.0) for f in ts[k]] for k in ('Fx_ToxicCloud_Form', 'Fx_ToxicCloud_Idle', 'Fx_ToxicCloud_Fade'))
        anchor = lambda im, x, y: (x - im.width / 2, y - 58 / 64 * im.height)
    frames = []
    for f in range(int(seconds * 1000 / ms)):
        t = f * ms / 1000
        im = bg.copy(); top = []
        for (x, y, t0) in hits:
            a = t - t0
            if a < 0: continue
            if a < .27:
                sp = appear[min(2, int(a / .09))]
            elif a < .27 + .48:
                sp = ign[min(5, int((a - .27) / .08))]
            else:
                sp = ign[5] if a < 5.5 else None
            if sp is not None: im.alpha_composite(sp, (int(x - sp.width / 2), int(y - sp.height / 2)))
            c0 = a - (.27 + 3 * .08)                       # cuadro 3 del prendido: sale la nube
            if c0 < 0: continue
            nf, ni = len(form) * h_ms[0] / 1000, 5.0 - len(fade) * h_ms[2] / 1000
            if c0 < nf: cl = form[int(c0 * 1000 / h_ms[0])]
            elif c0 < ni: cl = idle[int((c0 - nf) * 1000 / h_ms[1]) % len(idle)]
            elif c0 < 5.0: cl = fade[min(len(fade) - 1, int((c0 - ni) * 1000 / h_ms[2]))]
            else: continue
            top.append((y, cl, anchor(cl, x, y)))
        for _, cl, (px, py) in sorted(top, key=lambda q: q[0]):
            im.alpha_composite(cl, (int(px), int(py)))
        frames.append(im)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    big = [fr.resize((W * 3, H * 3), Image.NEAREST) for fr in frames]
    big[0].save(path, save_all=True, append_images=big[1:], duration=ms, loop=0, disposal=2)

TONES = {'Swamp': ('#1c2e20', '#213626', '#273f2b', '#2f4a32'), 'Burnt': ('#1d1a1d', '#242024', '#2c272a', '#363033')}

def build():
    fx = {'Fx_PoisonSplat_Ignite': [splat_ignite(i) for i in range(6)]}
    fx.update(haze_sheets())
    for name, frames in fx.items():
        os.makedirs(os.path.join(OUT, 'Effects'), exist_ok=True)
        save_sheet(os.path.join(OUT, 'Effects', name + '.png'), frames)
    preview(os.path.join(OUT, 'vista_previa_veneno_ardiendo.png'), fx, BG, scale=3)
    os.makedirs(os.path.join(OUT, 'gif', 'escenas'), exist_ok=True)
    for name, frames in fx.items(): gif(os.path.join(OUT, 'gif', name + '.gif'), frames, MS[name], BG, scale=4)
    for key, tones in TONES.items():
        scene(os.path.join(OUT, 'gif', 'escenas', f'camada_antes_{key}.gif'), False, tones)
        scene(os.path.join(OUT, 'gif', 'escenas', f'camada_ahora_{key}.gif'), True, tones)
    return fx

if __name__ == '__main__':
    build()
    print('Listo:', OUT)
