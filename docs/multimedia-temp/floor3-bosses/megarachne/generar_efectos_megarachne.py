"""Efectos de la Megarachne (tanda 6j). Skill primordia-sprites (pixel.py).

6j-1 (`cables`, `rafaga`):
- Cables: TRAMOS que se repiten a lo largo de la línea (el código los pone con un LineRenderer de textura en mosaico,
  con varios puntos para la caída): Cable_Normal (seda trenzada, 32×14, se repite cada 32 px), Cable_Thin (hilo fino
  que tiende al tejer, 32×4), Cable_Grow (hilo fino → cable en 6 cuadros, 32×14), Cable_Burning (el tramo al rojo bajo
  las llamas, 3 cuadros), Cable_Charred (negro y fino con ascuas, 3 cuadros). Encima, sin girar con el cable:
  Fx_CableFlame (llama 16×24, 6), Fx_CableFront (fogonazo del frente, 24×24, 4), Fx_CableAsh (ceniza y ascuas que
  caen, 16×32, 6), Cable_Stub (cabo chamuscado que cuelga del ancla, 16×40, 4: se deshace), Fx_CableSmoke (el fuego
  se apaga junto al Nido aún empapado, 24×40, 6).
- Ráfaga de seda (Phaser Spider): Proj_MegarachneSilk (bolita de seda 10×10, 4), Fx_MegarachneSilk_Muzzle (en las
  hileras, 24×24, 4), Fx_MegarachneWeb_Appear (3) / _Idle (1) / _Burn (6), 40×36: telaraña del suelo que frena y arde
  (algo más grande que la de la araña de las frondas).

6j-2 (`resto`): Fx_MegarachneNet_Shadow (aviso, 64×48, 6) / _Fall (64×88, pivote abajo 32,64; 5) / _Idle / _Break (5) /
  _Burn (6) / _Trapped (capa 40×40 sobre el jugador, 4); Megarachne_Sac_Idle / _Tremble (4) / _Burst (5, salen las
  crías) / _Break (5, roto antes: sin crías), 24×32 con el hilo atado arriba en el centro; Megarachne_Cocoon_Fall (4) /
  _Open (6) con el contenedor de corazón de la tienda; Fx_MegarachneDrop_Track (ámbar, 4) / _Locked (rojo, 2), 40×28;
  Fx_MegarachneDust (72×36, 6); Megarachne_Brood_Move/Bite/Hurt/Death (24×24, de lado; Flip X).

Uso: python generar_efectos_megarachne.py [cables] [rafaga] [resto]    (sin nada: todo)
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
MT = os.path.normpath(os.path.join(HERE, '..', '..'))
sys.path.insert(0, os.path.join(MT, 'floor3-environment'))
sys.path.insert(0, HERE)
from base_piso3 import *                               # noqa: F401,F403
from PIL import Image, ImageDraw

OUT = os.path.join(HERE, 'Effects')
SILKC = [(150, 144, 168), (196, 190, 212), (226, 222, 236), (250, 248, 255)]
CHARC = [(20, 14, 18), (34, 26, 30), (58, 46, 50), (90, 76, 78)]
EMB = [C(c) for c in EMBER]
FIREC = [C(c) for c in FIRE]
TW = 32                                                # periodo del mosaico (las hebras dan una vuelta cada 32 px)
TH = 14                                                # grosor del cable (~1 u)

# =================================================================================================
# Tramos del cable (horizontales: x = a lo largo, se repite cada 16 px; y = a lo ancho)
# =================================================================================================
def tile(state='normal', k=0, grow=1.0):
    """state: normal | thin | burning | charred. grow (0-1): del hilo fino al cable."""
    im = Image.new('RGBA', (TW, TH), (0, 0, 0, 0)); p = im.load()
    rnd = random.Random(7 + k)
    mid = (TH - 1) / 2
    if state == 'thin':
        im = Image.new('RGBA', (TW, 4), (0, 0, 0, 0)); p = im.load()
        for x in range(TW):
            p[x, 1] = SILKC[2] + (240,); p[x, 2] = SILKC[0] + (170,)
        p[(k * 5) % TW, 1] = SILKC[3] + (255,)          # brillo que recorre el hilo
        return im
    if state == 'charred':
        for x in range(TW):
            y = round(mid + .6 * math.sin(x / TW * 2 * math.pi))
            p[x, y] = CHARC[2] + (255,); p[x, y + 1] = CHARC[0] + (255,)
            if (x + k * 5) % 7 == 0: p[x, y] = EMB[3] if (x + k) % 2 else EMB[2]
        return im
    half = mid * grow
    for x in range(TW):
        for y in range(TH):
            d = abs(y - mid)
            if d > half + .5: continue
            p[x, y] = SILKC[2] + (44,)                    # velo de seda
        for ph, a in ((0, 0), (2 * math.pi / 3, 1), (4 * math.pi / 3, 2)):   # hebras trenzadas (periodo 16)
            y = mid + half * .55 * math.sin(x / TW * 2 * math.pi + ph)
            p[x, round(y)] = SILKC[2 + (a == 0)] + (230,)
        if x % 11 == 3 and grow > .4:                     # travesaños (pocos: es seda, no una rejilla)
            for y in range(round(mid - half), round(mid + half) + 1):
                if (y + x) % 3: p[x, y] = SILKC[1] + (95,)
        if grow > .15:
            p[x, round(mid - half)] = SILKC[3] + (235,)   # bordes: arriba coge luz, abajo en sombra suave
            p[x, round(mid + half)] = SILKC[1] + (225,)
    if state == 'burning':                               # al rojo bajo las llamas: el velo se va, las hebras arden
        for x in range(TW):
            for y in range(TH):
                c = p[x, y]
                if not c[3]: continue
                if c[3] < 100: p[x, y] = (0, 0, 0, 0); continue          # el velo desaparece
                edge = y in (round(mid - half), round(mid + half))
                t = (rnd.random() + k * .37) % 1
                p[x, y] = ((EMB[3] if t < .35 else EMB[2]) if edge else (EMB[2] if t < .3 else EMB[1] if t < .7 else C('#3a2224')))[:3] + (255,)
    if grow == 1.0 and state == 'normal':
        p[(3 + k * 6) % TW, 2] = SILKC[3] + (255,)        # gota de rocío
    return im

def flame(k, n=6, seed=3):
    """Llama vertical (16×24) que el código pone sobre el tramo que arde, siempre derecha."""
    cv = Canvas(16, 24); rnd = random.Random(seed + k * 11)
    for col in range(3):
        cx = 4 + col * 4 + rnd.uniform(-1, 1)
        h = (10, 16, 12)[col] * (.75 + .25 * math.sin((k / n) * 2 * math.pi + col * 2))
        for y in range(int(h)):
            t = y / h
            w = (1 - t) * 2.2 + .4
            for x in range(int(cx - w), int(cx + w) + 1):
                c = FIREC[4] if t < .2 and abs(x - cx) < w * .5 else FIREC[3] if t < .45 else FIREC[2] if t < .75 else FIREC[1]
                cv.put(x, 22 - y + round(math.sin(y * .7 + k) * .6), c)
    for k2 in range(2):
        cv.put(rnd.randint(3, 12), rnd.randint(0, 6), EMB[3])
    return cv

def front(k):
    """Fogonazo del frente de fuego (24×24): chisporroteo brillante donde avanza la mecha."""
    cv = Canvas(24, 24); rnd = random.Random(k * 5 + 1)
    r = (5, 7, 6, 8)[k]
    for y in range(24):
        for x in range(24):
            d = math.hypot(x - 12, (y - 14) * 1.3)
            if d < r: cv.put(x, y, FIREC[4] if d < r * .35 else FIREC[3] if d < r * .7 else FIREC[2])
    for j in range(6):
        a = rnd.uniform(0, 6.28); L = rnd.uniform(r, r + 5)
        cv.put(12 + math.cos(a) * L, 14 + math.sin(a) * L * .8, EMB[3])
    return cv

def ash(k, n=6):
    """Ceniza y ascuas que caen del cable carbonizado (16×32, bucle)."""
    cv = Canvas(16, 32); rnd = random.Random(2)
    for j in range(7):
        x0, sp, kind = rnd.uniform(2, 14), rnd.uniform(.7, 1.2), rnd.random()
        y = ((k / n) * sp * 32 + rnd.uniform(0, 32)) % 32
        x = x0 + math.sin(y / 5 + j) * 1.2
        cv.put(x, y, EMB[2] if kind < .35 else C('#6e6466', 210))
    return cv

def stub(k, n=4):
    """Cabo chamuscado que cuelga del ancla (16×40; el ancla arriba en el centro): seda sana arriba, negro, punta en
    ascua; se deshace."""
    cv = Canvas(16, 40)
    fade = k / (n - 1)
    L = int(34 * (1 - .5 * fade))
    pts = [(8 + 3 * math.sin(t * 4) * t, 2 + L * t ** 1.3) for t in [i / 30 for i in range(31)]]
    for i, (x, y) in enumerate(pts):
        t = i / 30
        if t < .12: cv.put(x, y, C('#e2deec')); cv.put(x + 1, y, C('#9690a8'))
        else: cv.put(x, y, C('#4e3e40')); cv.put(x + 1, y, C('#22161c'))
    x, y = pts[-1]
    if k < n - 1: cv.put(x, y + 1, EMB[3]); cv.put(x, y + 2, EMB[2])
    for j in range(k * 2):                                # trocitos que se desprenden
        cv.put(x + (j % 3) - 1, y + 4 + j * 2, C('#4e3e40', 200))
    return cv

def smoke(k, n=6):
    """El fuego se apaga junto al Nido aún empapado: chisporroteo y bocanadas de humo (24×40)."""
    im = Image.new('RGBA', (24, 40), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    for j in range(4):
        age = k - j
        if age < 0: continue
        r = 2 + age * .8
        x, y = 12 + 2 * math.sin(age + j), 34 - age * 5
        d.ellipse([x - r, y - r, x + r, y + r], fill=(176, 172, 176, max(0, int(170 * (1 - age / (n + 1))))))
    if k < 2:
        for dx in (-3, 0, 3): d.point((12 + dx, 36 - k), fill=EMB[3])
    return im

def grow_frames(n=6):
    return [tile('normal', 0, grow=max(.12, i / (n - 1))) for i in range(n)]

# =================================================================================================
# Ráfaga de seda
# =================================================================================================
def silk_ball(k):
    """Bolita de seda en vuelo (10×10): se enrosca (4 cuadros)."""
    cv = Canvas(10, 10)
    for y in range(10):
        for x in range(10):
            d = math.hypot(x - 4.5, y - 4.5)
            if d < 3.6: cv.put(x, y, C('#c4bed4') if d > 2.4 else C('#e2deec'))
    a = k * math.pi / 2
    for t in range(5):                                    # hebra que la envuelve
        cv.put(4.5 + math.cos(a + t * .5) * 2.4, 4.5 + math.sin(a + t * .5) * 2.4, C('#fafaff'))
    cv.put(3, 3, C('#ffffff'))
    cv.outline(C('#5a5068'))
    return cv

def muzzle(k):
    """Estallido de seda en las hileras al disparar (24×24): hebras blancas que salen y se deshilachan."""
    cv = Canvas(24, 24)
    L = (4, 8, 10, 11)[k]; rnd = random.Random(4)
    for j in range(8):
        a = j * math.pi / 4 + rnd.uniform(-.2, .2)
        for i in range(int(L * (1 if j % 2 else .7))):
            if k == 3 and i < L * .5: continue            # al final solo quedan las puntas
            x, y = 12 + math.cos(a) * i, 12 + math.sin(a) * i * .8
            cv.put(x, y, C('#fafaff') if i < 4 else C('#e2deec') if i < 8 else C('#c4bed4'))
    if k < 2:
        for dx, dy in ((0, 0), (1, 0), (0, 1), (1, 1), (-1, 0), (0, -1)): cv.put(12 + dx, 12 + dy, C('#ffffff'))
    return cv

def ground_web(burn=0.0, appear=1.0, seed=5):
    """Telaraña de la ráfaga en el suelo (40×36, vista desde arriba): radios y espiral; burn la recorre el fuego
    desde el centro y la deja en ceniza."""
    W_, H_ = 40, 36
    cv = Canvas(W_, H_); rnd = random.Random(seed)
    cx, cy, R = 20, 18, 18 * appear
    def col(r):
        if r < burn * 22 - 4: return C('#4a4044', 160) if rnd.random() < .3 else None
        if r < burn * 22: return FIREC[rnd.randint(2, 4)]
        return C('#e2deec', 230) if int(r) % 4 < 2 else C('#c4bed4', 220)
    for k in range(10):
        a = k * math.pi / 5 + .15
        for i in range(int(R)):
            x, y = cx + math.cos(a) * i, cy + math.sin(a) * i * .82
            c = col(i)
            if c: cv.put(x, y, c)
    for ring in range(3, int(R), 3):
        for k in range(64):
            a = k * 2 * math.pi / 64
            rr = ring + .3 * math.sin(k * .7)
            c = col(rr)
            if c and rnd.random() > .12: cv.put(cx + math.cos(a) * rr, cy + math.sin(a) * rr * .82, c)
    if 0 < burn < 1:
        for j in range(5): cv.put(cx + rnd.randint(-10, 10), cy + rnd.randint(-8, 8), C('#7a7276', 150))
    return cv

# =================================================================================================
# Guardar y vistas previas
# =================================================================================================
def strip(frames, name):
    os.makedirs(OUT, exist_ok=True)
    ims = [f.img() if hasattr(f, 'img') else f for f in frames]
    w, h = ims[0].size
    im = Image.new('RGBA', (w * len(ims), h), (0, 0, 0, 0))
    for i, f in enumerate(ims): im.paste(f, (i * w, 0))
    im.save(os.path.join(OUT, name + '.png'))
    return ims

def tiled_line(im, p0, p1, tl, sag=0.0, offset=0.0):
    """Simula el LineRenderer: mapea el tramo a lo largo de la línea p0→p1 (con caída) en la imagen."""
    tp = tl.load(); w, h = tl.size
    L = math.dist(p0, p1); n = int(L)
    for i in range(n):
        t = i / max(1, n - 1)
        x = p0[0] + (p1[0] - p0[0]) * t
        y = p0[1] + (p1[1] - p0[1]) * t + sag * 4 * t * (1 - t)
        dx, dy = (p1[0] - p0[0]) / L, (p1[1] - p0[1]) / L + sag * 4 * (1 - 2 * t) / L
        nn = math.hypot(dx, dy); dx, dy = dx / nn, dy / nn
        u = int((i + offset) % w)
        for v in range(h):
            c = tp[u, v]
            if not c[3]: continue
            px, py = x - dy * (v - h / 2) * 1, y + dx * (v - h / 2) * .82
            q = (int(round(px)), int(round(py)))
            if 0 <= q[0] < im.width and 0 <= q[1] < im.height:
                base = im.getpixel(q)
                a = c[3] / 255
                im.putpixel(q, tuple(int(c[i_] * a + base[i_] * (1 - a)) for i_ in range(3)) + (255,))

def cables():
    sys.path.insert(0, HERE)
    from generar_megarachne import preview, gif, smoke_floor, BG
    rows = {}
    rows['Cable_Normal'] = strip([tile('normal', k) for k in range(1)], 'Cable_Normal')
    rows['Cable_Thin'] = strip([tile('thin', k) for k in range(3)], 'Cable_Thin')
    rows['Cable_Grow'] = strip(grow_frames(), 'Cable_Grow')
    rows['Cable_Burning'] = strip([tile('burning', k) for k in range(3)], 'Cable_Burning')
    rows['Cable_Charred'] = strip([tile('charred', k) for k in range(3)], 'Cable_Charred')
    rows['Fx_CableFlame'] = strip([flame(k) for k in range(6)], 'Fx_CableFlame')
    rows['Fx_CableFront'] = strip([front(k) for k in range(4)], 'Fx_CableFront')
    rows['Fx_CableAsh'] = strip([ash(k) for k in range(6)], 'Fx_CableAsh')
    rows['Cable_Stub'] = strip([stub(k) for k in range(4)], 'Cable_Stub')
    rows['Fx_CableSmoke'] = strip([smoke(k) for k in range(6)], 'Fx_CableSmoke')
    preview(os.path.join(HERE, 'vista_previa_6j1_piezas_cable.png'), rows, BG, scale=4)
    # montaje: un cable en diagonal con la mecha avanzando (como lo hará el código)
    frames = []
    p0, p1 = (20, 30), (300, 120)
    n = 12
    for j in range(n + 8):
        im = smoke_floor(320, 200, seed=3)
        L = math.dist(p0, p1)
        front_s = 40 + j * 22                             # px recorridos por el frente desde el origen (x = 40)
        segs = []
        s = 0
        while s < L:                                      # trozos de 16 px con su estado
            a, b = s, min(L, s + 16)
            ds = abs((a + b) / 2 - 40)
            st = 'normal' if ds > front_s - 40 else 'burning' if ds > front_s - 40 - 22 else 'charred'
            segs.append((a, b, st)); s = b
        for a, b, st in segs:
            t0, t1 = a / L, b / L
            q0 = (p0[0] + (p1[0] - p0[0]) * t0, p0[1] + (p1[1] - p0[1]) * t0 + 10 * 4 * t0 * (1 - t0))
            q1 = (p0[0] + (p1[0] - p0[0]) * t1, p0[1] + (p1[1] - p0[1]) * t1 + 10 * 4 * t1 * (1 - t1))
            if st == 'charred': q0 = (q0[0], q0[1] + 6); q1 = (q1[0], q1[1] + 6)
            tl = tile(st, j)
            tiled_line(im, q0, q1, tl, offset=a)
        for a, b, st in segs:                             # llamas derechas sobre lo que arde, fogonazo en el frente
            if st != 'burning': continue
            t = (a + b) / 2 / L
            x, y = p0[0] + (p1[0] - p0[0]) * t, p0[1] + (p1[1] - p0[1]) * t + 10 * 4 * t * (1 - t)
            im.alpha_composite(flame((j + int(a)) % 6).img(), (int(x - 8), int(y - 20)))
        for sgn in (-1, 1):
            fs = 40 + sgn * (front_s - 40)
            if 0 < fs < L:
                t = fs / L
                x, y = p0[0] + (p1[0] - p0[0]) * t, p0[1] + (p1[1] - p0[1]) * t + 10 * 4 * t * (1 - t)
                im.alpha_composite(front(j % 4).img(), (int(x - 12), int(y - 14)))
        frames.append(im)
    gif(os.path.join(HERE, 'gif', 'Cable_mecha.gif'), frames, 90, BG, scale=2)
    preview(os.path.join(HERE, 'vista_previa_6j1_mecha.png'), {'mecha (montaje)': frames[::3]}, BG, scale=2)
    for name, fr in rows.items():
        if len(fr) > 1: gif(os.path.join(HERE, 'gif', name + '.gif'), fr, 100, BG, scale=4)
    print('  cables')

def rafaga():
    sys.path.insert(0, HERE)
    from generar_megarachne import preview, gif, BG
    rows = {}
    rows['Proj_MegarachneSilk'] = strip([silk_ball(k) for k in range(4)], 'Proj_MegarachneSilk')
    rows['Fx_MegarachneSilk_Muzzle'] = strip([muzzle(k) for k in range(4)], 'Fx_MegarachneSilk_Muzzle')
    rows['Fx_MegarachneWeb_Appear'] = strip([ground_web(appear=a) for a in (.35, .7, 1.0)], 'Fx_MegarachneWeb_Appear')
    rows['Fx_MegarachneWeb_Idle'] = strip([ground_web()], 'Fx_MegarachneWeb_Idle')
    rows['Fx_MegarachneWeb_Burn'] = strip([ground_web(burn=b) for b in (.15, .35, .55, .75, .95, 1.2)], 'Fx_MegarachneWeb_Burn')
    preview(os.path.join(HERE, 'vista_previa_6j1_rafaga.png'), rows, BG, scale=4)
    for name, fr in rows.items():
        if len(fr) > 1: gif(os.path.join(HERE, 'gif', name + '.gif'), fr, 90, BG, scale=4)
    print('  rafaga')


# =================================================================================================
# 6j-2: red que cae, saco de crías, crías, marca del salto, polvo, capullo con el corazón
# =================================================================================================
RED_ = [C('#4a0e12'), C('#a82a2a'), C('#e84a3a'), C('#ff9a7a')]
AMB_ = [C('#6a2e0e'), C('#e89a30'), C('#ffcc5a'), C('#fff0b0')]

def net(cx, cy, R, cv, burn=0.0, torn=0.0, seed=9, alpha=230):
    """Telaraña redonda grande (la red que cae): 14 radios, espiral apretada, borde grueso."""
    rnd = random.Random(seed)
    def col(r, j):
        if r < burn * (R + 6) - 4: return C('#4a4044', 150) if rnd.random() < .25 else None
        if r < burn * (R + 6): return FIREC[rnd.randint(2, 4)]
        if torn and rnd.random() < torn: return None
        return C('#eeeaf6', alpha) if j % 2 else C('#c8c2d8', alpha)
    for k in range(14):
        a = k * 2 * math.pi / 14 + .1
        for i in range(int(R) + 1):
            c = col(i, 1)
            if c: cv.put(cx + math.cos(a) * i, cy + math.sin(a) * i * .82, c)
    for ring in range(3, int(R) + 1, 2):
        for k in range(int(ring * 6)):
            a = k * 2 * math.pi / (ring * 6)
            c = col(ring, k)
            if c: cv.put(cx + math.cos(a) * ring, cy + math.sin(a) * ring * .82, c)
    for k in range(int(R * 7)):                           # borde grueso
        a = k * 2 * math.pi / (R * 7)
        c = col(R, 1)
        if c: cv.put(cx + math.cos(a) * (R + 1), cy + math.sin(a) * (R + 1) * .82, C('#fafaff', alpha) if c[0] > 200 else c)

def net_shadow(k, n=6):
    """Aviso de la red que cae (64×48): sombra tramada que crece y borde rojo que late."""
    cv = Canvas(64, 48)
    R = 8 + 22 * (k + 1) / n
    for y in range(48):
        for x in range(64):
            d = math.hypot(x - 32, (y - 24) / .82)
            if d <= R:
                if d > R - 1.5: cv.put(x, y, RED_[2] if k % 2 else RED_[1])
                elif BAYER[y % 4][x % 4] < 4 + k: cv.put(x, y, C('#08060a', 130))
    return cv

def net_fall(k, n=5):
    """La red baja abriéndose (64×88; pivote = centro del suelo, abajo: 32, 64): alto y pequeña → en el suelo."""
    cv = Canvas(64, 88)
    t = (k + 1) / n
    R = 30 * (.45 + .55 * t)
    cy = 64 - (1 - t) ** 1.4 * 56
    net(32, cy, R, cv, alpha=int(160 + 80 * t))
    return cv

def net_ground(burn=0.0, torn=0.0):
    cv = Canvas(64, 48); net(32, 24, 30, cv, burn=burn, torn=torn); return cv

def trapped(k, n=4):
    """Jugador atrapado (capa 40×40 encima de él, 1 s): hebras que lo envuelven y tiemblan al forcejear."""
    cv = Canvas(40, 40); rnd = random.Random(3)
    j = (0, 1, 0, -1)[k]
    for i in range(9):
        a0 = rnd.uniform(0, math.pi)
        for t in range(26):
            a = a0 + t * .24
            x, y = 20 + j + math.cos(a) * (12 + 3 * math.sin(i)), 22 + math.sin(a) * 9 + (i - 4) * 1.4
            if rnd.random() < .85: cv.put(x, y, C('#eeeaf6', 220) if t % 3 else C('#c8c2d8', 220))
    for (x, y) in ((6, 10), (33, 12), (8, 32), (31, 30)):    # hebras que la sujetan al suelo
        cv.put(x + j, y, C('#fafaff')); cv.put(x + j, y + 1, C('#c8c2d8'))
    return cv

def sac(k=0, state='idle', n=4):
    """Saco de crías (24×32; el hilo se ata arriba en el centro, 12, 0): huevos dentro, tiembla, revienta o se rompe."""
    cv = Canvas(24, 32); rnd = random.Random(6)
    sw = {'idle': (0, 1, 0, -1), 'tremble': (-1, 1, -1, 1)}.get(state, (0,) * 8)[k % 4]
    cx, cy, rx, ry = 12 + sw, 17, 7.5, 10
    if state == 'idle' or state == 'tremble':
        for y in range(32):
            for x in range(24):
                d = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
                if d <= 1:
                    c = C('#eae4ee') if d < .45 else C('#c8bfd2') if d < .8 else C('#9a90a8')
                    if (x - cx) * -.6 + (y - cy) * -.8 > 4: c = C('#f8f4fa')
                    cv.put(x, y, c)
        for (ex, ey) in ((-3, -3), (2, -1), (-1, 3), (3, 4), (-3, 5)):   # sombras de los huevos/crías dentro
            cv.put(cx + ex, cy + ey, C('#6a5a72')); cv.put(cx + ex + 1, cy + ey, C('#8a7a92'))
            if state == 'tremble' and (k + ex) % 2: cv.put(cx + ex, cy + ey - 1, C('#c83a3a'))
        for y in range(0, 8): cv.put(12 + sw * y / 8, y, C('#e2deec'))       # nudo de seda arriba
        cv.outline(C('#3a3044'))
        return cv
    # revienta (salen las crías) o se rompe (el jugador lo rompe antes: sin crías, se deshace)
    t = (k + 1) / n
    for side in (-1, 1):
        for y in range(32):
            for x in range(24):
                d = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
                if d <= 1 and (x - cx) * side > (3 * t if state == 'burst' else 1.5 * t) and y > 7 + 16 * t * (state == 'break'):
                    xx = x + side * (5 * t if state == 'burst' else 2 * t); yy = y + (8 * t * t if state == 'break' else 2 * t)
                    cv.put(xx, yy, C('#c8bfd2') if d > .6 else C('#9a90a8'))
    for y in range(0, 8): cv.put(12, y, C('#e2deec'))
    if state == 'burst':                                  # restos viscosos y la primera cría que asoma
        for j in range(5):
            cv.put(cx + rnd.uniform(-5, 5), cy + 8 + t * 6 + rnd.uniform(-2, 2), C('#d8e0a8', 200))
    else:
        for j in range(int(6 * t)):
            cv.put(cx + rnd.uniform(-6, 6), cy + 6 + t * 10 + rnd.uniform(-3, 3), C('#c8bfd2', 200))
    cv.outline(C('#3a3044'))
    return cv

def cocoon(k, state='fall', n=6):
    """Capullo de seda con el contenedor de corazón (24×32): cae del embudo girando y se abre (el corazón es el de la
    tienda, Item_HeartContainer_Idle, cuadro 0, 16×16)."""
    im = Image.new('RGBA', (24, 32), (0, 0, 0, 0))
    heart = Image.open(os.path.join(MT, 'shop', 'Item_HeartContainer_Idle.png')).convert('RGBA').crop((0, 0, 16, 16))
    cv = Canvas(24, 32)
    open_ = 0 if state == 'fall' else (k + 1) / n
    rot = 0
    for y in range(32):
        for x in range(24):
            u, v = x - 12, y - 17
            uu, vv = u * math.cos(rot) - v * math.sin(rot) * .3, v
            d = (uu / 8) ** 2 + (vv / 11) ** 2
            if d > 1: continue
            if open_ and abs(u) < 7 * open_ and v > -10 + 4 * open_: continue
            band = int((vv + 11) / 2.2 + uu * .3) % 2
            cv.put(x, y, C('#eae4ee') if band else C('#c8bfd2'))
    cv.outline(C('#3a3044'))
    if state == 'fall':                                   # gira mientras cae
        return cv.img().rotate(-k * 35, resample=Image.NEAREST, center=(12, 17))
    im.alpha_composite(cv.img())
    if open_:
        h = heart.copy(); h.putalpha(h.getchannel('A').point(lambda a: int(a * min(1, open_ * 1.6))))
        lay = Image.new('RGBA', (24, 32), (0, 0, 0, 0)); lay.alpha_composite(h, (4, 9 - int(4 * open_)))
        im = Image.alpha_composite(im, lay) if open_ < .6 else Image.alpha_composite(lay, im) if False else Image.alpha_composite(im, lay)
        if open_ > .5:
            d = ImageDraw.Draw(im)
            for j in range(4):
                a = j * math.pi / 2 + k * .4
                d.point((12 + math.cos(a) * 10, 13 + math.sin(a) * 8), fill=(255, 240, 200, 220))
    return im

def drop_mark(k, locked=False):
    """Marca del salto (40×28, en el suelo): ámbar mientras sigue al jugador, roja y quieta cuando se fija. Un anillo
    con radios cortos de seda (se lee como «aquí cae la araña»)."""
    cv = Canvas(40, 28)
    pal = RED_ if locked else AMB_
    R = 15 + (k % 2) * (1 if locked else 0)
    for i in range(80):
        a = i * 2 * math.pi / 80
        if locked or (i + k * 4) % 10 < 7: cv.put(20 + math.cos(a) * R, 14 + math.sin(a) * R * .62, pal[2])
    for j in range(8):
        a = j * math.pi / 4 + (0 if locked else k * .2)
        for r in range(4, 9): cv.put(20 + math.cos(a) * r, 14 + math.sin(a) * r * .62, pal[1])
    cv.put(20, 14, pal[3]); cv.put(19, 14, pal[2]); cv.put(21, 14, pal[2])
    return cv

def dust(k, n=6, seed=2):
    """Polvo al aterrizar o al caer (72×36): anillo de polvo y hojarasca que se abre y se posa."""
    im = Image.new('RGBA', (72, 36), (0, 0, 0, 0)); d = ImageDraw.Draw(im); rnd = random.Random(seed)
    t = (k + 1) / n
    for j in range(16):
        a = j * 2 * math.pi / 16 + rnd.uniform(-.15, .15)
        r = 8 + 26 * t ** .7
        x, y = 36 + math.cos(a) * r, 22 + math.sin(a) * r * .45
        rr = 4 * (1 - t) + 1.5
        d.ellipse([x - rr, y - rr * .8, x + rr, y + rr * .8], fill=(150, 132, 104, int(190 * (1 - t))))
    for j in range(6):                                     # hojarasca
        a = rnd.uniform(0, 6.28); r = 6 + 24 * t
        d.point((36 + math.cos(a) * r, 20 + math.sin(a) * r * .5 - 6 * math.sin(t * math.pi)), fill=(138, 116, 44, int(255 * (1 - t * .7))))
    return im

def brood():
    """Crías (24×24, de lado como la araña de las frondas, Flip X para la izquierda): mini Megarachne gris violácea
    con puntos rojos, ~18 px. Move 4, Bite 4 (el mordisco cuenta en el cuadro 2), Hurt 2, Death 5."""
    sys.path.insert(0, os.path.join(MT, 'floor3-enemies'))
    import generar_enemigos_piso3 as EN
    EN.SPIDER = ramp('#4a3a4a', 5); EN.SK = .86
    orig = EN.spider_model
    LEG = (C('#c4a06a'), C('#9a7444'))                   # patas color arena, como su madre
    EN.spider_model = lambda *a_, **k_: (lambda PL: (PL[0], [(pts, LEG) for pts, _ in PL[1]]))(orig(*a_, **k_))
    def sp(**k):
        cv = EN.spider(n=24, **k)
        im = cv.img().convert('RGBA'); px = im.load()
        cols = {tuple(C(c)[:3]) for c in EN.SPIDER}
        pts = [(x, y) for y in range(24) for x in range(24) if px[x, y][3] and px[x, y][:3] in cols]
        if pts and not k.get('belly_up'):
            xs = sorted(p_[0] for p_ in pts); xm = xs[len(xs) // 3]
            ab = [p_ for p_ in pts if p_[0] <= xm]
            if ab:
                cx = round(sum(p_[0] for p_ in ab) / len(ab)); cy = min(p_[1] for p_ in ab) + 2
                for dx in (0, 2):
                    if px[cx + dx, cy][3]: px[cx + dx, cy] = C('#e04a3a')
        return im
    a = {}
    a['Megarachne_Brood_Move'] = [sp(walk=i / 4) for i in range(4)]
    a['Megarachne_Brood_Bite'] = [sp(crouch=.8), sp(lift=1.2, air=.4), sp(lift=.6, air=.2), sp(crouch=.3)]
    EN.FLAT = EN.WHITE5; w = sp(); EN.FLAT = EN.RED5; r = sp(); EN.FLAT = None
    a['Megarachne_Brood_Hurt'] = [w, r]
    a['Megarachne_Brood_Death'] = ([sp(eye_state='x', crouch=1.0)] +
                                   [sp(belly_up=True, curl=c, fade=t) for c, t in ((0.4, 0.1), (0.7, 0.3), (1.0, 0.6), (1.0, 1.0))])
    return a

def resto():
    sys.path.insert(0, HERE)
    from generar_megarachne import preview, gif, BG
    rows = {}
    rows['Fx_MegarachneNet_Shadow'] = strip([net_shadow(k) for k in range(6)], 'Fx_MegarachneNet_Shadow')
    rows['Fx_MegarachneNet_Fall'] = strip([net_fall(k) for k in range(5)], 'Fx_MegarachneNet_Fall')
    rows['Fx_MegarachneNet_Idle'] = strip([net_ground()], 'Fx_MegarachneNet_Idle')
    rows['Fx_MegarachneNet_Break'] = strip([net_ground(torn=t) for t in (.15, .35, .55, .75, .95)], 'Fx_MegarachneNet_Break')
    rows['Fx_MegarachneNet_Burn'] = strip([net_ground(burn=b) for b in (.15, .35, .55, .75, .95, 1.2)], 'Fx_MegarachneNet_Burn')
    rows['Fx_MegarachneNet_Trapped'] = strip([trapped(k) for k in range(4)], 'Fx_MegarachneNet_Trapped')
    rows['Megarachne_Sac_Idle'] = strip([sac(k, 'idle') for k in range(4)], 'Megarachne_Sac_Idle')
    rows['Megarachne_Sac_Tremble'] = strip([sac(k, 'tremble') for k in range(4)], 'Megarachne_Sac_Tremble')
    rows['Megarachne_Sac_Burst'] = strip([sac(k, 'burst', 5) for k in range(5)], 'Megarachne_Sac_Burst')
    rows['Megarachne_Sac_Break'] = strip([sac(k, 'break', 5) for k in range(5)], 'Megarachne_Sac_Break')
    rows['Megarachne_Cocoon_Fall'] = strip([cocoon(k, 'fall') for k in range(4)], 'Megarachne_Cocoon_Fall')
    rows['Megarachne_Cocoon_Open'] = strip([cocoon(k, 'open') for k in range(6)], 'Megarachne_Cocoon_Open')
    rows['Fx_MegarachneDrop_Track'] = strip([drop_mark(k) for k in range(4)], 'Fx_MegarachneDrop_Track')
    rows['Fx_MegarachneDrop_Locked'] = strip([drop_mark(k, True) for k in range(2)], 'Fx_MegarachneDrop_Locked')
    rows['Fx_MegarachneDust'] = strip([dust(k) for k in range(6)], 'Fx_MegarachneDust')
    for name, fr in brood().items():
        rows[name] = strip(fr, name)
    preview(os.path.join(HERE, 'vista_previa_6j2_piezas.png'), rows, BG, scale=3)
    for name, fr in rows.items():
        if len(fr) > 1: gif(os.path.join(HERE, 'gif', name + '.gif'), fr, 100, BG, scale=3)
    print('  resto')

if __name__ == '__main__':
    which = sys.argv[1:] or ['cables', 'rafaga', 'resto']
    for w in which: {'cables': cables, 'rafaga': rafaga, 'resto': resto}[w]()
