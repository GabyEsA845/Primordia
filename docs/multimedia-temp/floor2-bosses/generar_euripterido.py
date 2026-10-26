"""Jefe del piso 2: Euriptérido (escorpión marino, cueva con rocas).

Visto desde arriba, mira a la derecha (Flip X para la izquierda). Lienzo 128×128, cuerpo de unos 90 px con la cola.
Fase 1 acorazada (verde bronce), muda (cinemática) y fase 2 blanda (rosada, más rápida). La muda vacía queda de cobertura.
Uso: python generar_euripterido.py [carpeta_salida]
"""
import math, sys, os, random
from PIL import Image
from base_piso2 import *

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__))
N = 128
POISON = C('#a8ff5a')

def P(body, limb, eye=('#140c1a', '#2e2438', '#56486a', '#9a88a8'), spikes=True, alpha=255):
    return dict(body=body, limb=limb, eye=list(eye), spikes=spikes, alpha=alpha)
ARMOR = P(['#16241e', '#2c4a38', '#527a52', '#9cc086'], ['#1e3028', '#3a5c44', '#6a9466', '#b4d498'])
SOFT = P(['#5a2c3c', '#9a5466', '#d8949a', '#ffdcd2'], ['#6a3446', '#aa6070', '#e4a4a4', '#fff0e4'], spikes=False)
WHITE = P(WHITE4, WHITE4, WHITE4)
RED = P(RED4, RED4, ['#5a1a1a', '#a33b3b', '#e05a5a', '#ff9a8a'])
PALE = P(PALE4, PALE4, ['#3a3830', '#5a5848', '#7a7864', '#9a9478'], spikes=False)
STONE = P(STONE4, STONE4, STONE4, spikes=False)
SHED = P(['#5a5040', '#8a7c60', '#b8a888', '#e0d4b8'], ['#5a5040', '#8a7c60', '#b8a888', '#e0d4b8'],
         ['#3a3428'] * 4, spikes=True, alpha=215)

def merge(*layers):
    cv = Canvas(N)
    for l in layers: cv.px.update(l.px)
    return cv

def width(u):
    """Semianchura del abdomen delantero (preabdomen), de u=6 a u=-20: se estrecha poco a poco hasta el ancho de la cola."""
    seg = math.cos(((6 - u) % 2.6) / 2.6 * math.pi - math.pi / 2) * 0.5
    return 5.0 + 6.2 * max(0, 1 - ((u - 2) / 22) ** 2) ** 0.8 + seg

def eurypterid(pal=ARMOR, phase=0.0, dx=0, dy=0, tail=0.0, tail_glow=0, reach=(0.0, 0.0), open_=(0.6, 0.6),
               spread=0.25, eyes='angry', red=False, legs='walk', paddle=0.0, crack=0, sparks_at=None, sweep_trail=None,
               paddles=True, tail_len=1.0, tail_w=1.0, spike_glow=0, burst=False, n=N):
    cx, cy = n / 2 + dx - 6, n / 2 + dy
    a = pal['alpha']
    back = Canvas(n)
    # remos (sexto par de patas), detrás del cuerpo
    if paddles:
        for s in (-1, 1):
            ang = math.pi - s * (0.75 + 0.25 * math.sin(phase + paddle))
            px, py = cx + 3 + math.cos(ang) * 7, cy + s * 11 + math.sin(ang) * 7
            rot_ellipse(back, px, py, 8, 3.4, ang, pal['limb'], a)
            rot_ellipse(back, px + math.cos(ang) * 8, py + math.sin(ang) * 8, 4.4, 3.4, ang, pal['limb'], a, shift=-1)
    back.outline()
    body = Canvas(n)
    # prosoma (escudo de la cabeza)
    def pro_in(x, y):
        u, v = x - cx, y - cy
        return u >= 4 and abs((u - 13) / 9.5) ** 3 + abs(v / (12.5 - max(0, u - 8) * 0.3)) ** 3 <= 1
    def abd_in(x, y):
        u, v = x - cx, y - cy
        return -20.5 <= u <= 6 and abs(v) <= width(u)
    fill(body, lambda x, y: pro_in(x, y) or abd_in(x, y), cx - 3, cy, 26, 13, pal['body'], a)
    for k in range(1, 11):   # costuras del abdomen
        u = 6 - k * 2.6
        for y in range(int(cy - 13), int(cy + 14)):
            if abs(y + .5 - cy) < width(u) - 0.6: body.put_in(cx + u, y, C(pal['body'][1], a))
    for (x, y) in list(body.px):   # el escudo de la cabeza, un tono más claro y separado del abdomen
        u = x + .5 - cx
        if pro_in(x + .5, y + .5) and u >= 6.5:
            i = [C(c, a) for c in pal['body']].index(body.px[(x, y)]) if body.px[(x, y)] in [C(c, a) for c in pal['body']] else 1
            body.px[(x, y)] = C(pal['body'][min(3, i + 1)], a)
        elif 5 <= u < 6.5: body.px[(x, y)] = C(pal['body'][0], a)
    for (x, y) in list(body.px):   # borde delantero del escudo, más claro (el frente acorazado)
        u, v = x + .5 - cx, y + .5 - cy
        if pro_in(x + .5, y + .5) and not pro_in(x + 2.5, y + .5) and u > 16: body.px[(x, y)] = C(pal['body'][3], a)
    for s in (-1, 1):   # ojos compuestos
        ex, ey = cx + 15.5, cy + s * 6.5
        ellipse(body, ex, ey, 2.6, 2.9, pal['eye'], a)
        big_eye(body, ex - 1.5, ey - 1.5, eyes, red, size=3)
    body.put_in(cx + 12, cy - 0.5, C(pal['body'][3], a)); body.put_in(cx + 12, cy + 0.5, C(pal['body'][3], a))   # ocelos
    if crack:   # la muda: el escudo se raja por delante
        for y in range(int(cy - 10), int(cy + 11)):
            body.put_in(cx + 6.5 + (abs(y - cy) * 0.15), y, OUTL if crack == 1 else C(SOFT['body'][2]))
        if crack >= 2:
            for y in range(int(cy - 9), int(cy + 10)): body.put_in(cx + 7.5 + abs(y - cy) * 0.15, y, C(SOFT['body'][1]))
    # cola: postabdomen + telson, gira alrededor de u=-20; recta, forma una sola pieza con el cuerpo
    tl = Canvas(n)
    px0, py0 = cx - 20, cy
    ca, sa = math.cos(tail), math.sin(tail)
    def loc(x, y):
        ddx, ddy = x - px0, y - py0
        return (ddx * ca + ddy * sa) / tail_len, (-ddx * sa + ddy * ca) / tail_w
    def tail_in(x, y):
        u, v = loc(x, y)
        if -14 <= u <= 1.5: return abs(v) <= 5.0 - max(0, -u) * 0.1 + 0.4 * math.cos(((-u) % 2.8) / 2.8 * math.pi - math.pi / 2)
        if -16.5 <= u < -14: return u * 0 + ((u + 15.5) / 2.2) ** 2 + (v / 3.0) ** 2 <= 1 or abs(v) <= 2.6
        if -26 <= u < -16.5: return abs(v) <= 2.6 * (u + 26) / 9.5
        return False
    tcx, tcy = px0 + math.cos(math.pi + tail) * 10, py0 + math.sin(math.pi + tail) * 10
    fill(tl, tail_in, tcx, tcy, 10, 6, pal['body'], a)
    for (x, y) in list(tl.px):
        u, v = loc(x + .5, y + .5)
        if -14 < u < 0 and int(-u / 2.8) != int((-u + 1) / 2.8): tl.px[(x, y)] = C(pal['body'][1], a)
        if u < -21 and tail_glow: tl.px[(x, y)] = POISON if tail_glow >= 2 or (x + y) % 2 else C(pal['body'][2], a)
    if abs(tail) <= 0.6:
        for q, c in tl.px.items():
            if q not in body.px: body.px[q] = c
        tl.px = {}
    else: tl.outline()
    if tail_glow >= 2:
        tx, ty = px0 + math.cos(math.pi + tail) * 26 * tail_len, py0 + math.sin(math.pi + tail) * 26 * tail_len
        for k in range(8):
            ang = k * math.pi / 4
            tl.put_out(tx + math.cos(ang) * 2.5, ty + math.sin(ang) * 2.5, POISON[:3] + (160,))
    body.outline()
    if pal['spikes']:   # púas en los bordes de los segmentos
        for k in range(1, 11):
            u = 6 - k * 2.6
            for s in (-1, 1):
                bx, by = cx + u - 0.5, cy + s * (width(u) + 1.5)
                if spike_glow:   # las púas del caparazón se erizan y se cargan de veneno
                    ln = 1 + spike_glow
                    for e in range(ln + 1):
                        col = C(pal['body'][3], a) if e < ln - 1 else POISON
                        if e == ln and spike_glow >= 3: col = C('#ffffff')
                        body.put_out(bx - e * 0.45, by + s * e, col)
                    if spike_glow >= 2:   # halo verde en la punta
                        for ddx in (-1, 1): body.put_out(bx - ln * 0.45 + ddx, by + s * ln, POISON[:3] + (110,))
                elif burst:   # el abanico acaba de salir: la púa se ha soltado y deja una nube de veneno
                    body.put_out(bx, by, C(pal['body'][2], a))
                    for e in (3, 4, 5):
                        body.put_out(bx - e * 0.3 + (e % 2), by + s * e, POISON[:3] + (220 - e * 30,))
                else: body.put_out(bx, by, C(pal['body'][3], a))
    # quelíceros: brazo de dos segmentos y pinza
    ch = Canvas(n)
    tips = []
    for i, s in enumerate((-1, 1)):
        r, op = reach[i], open_[i]
        a0 = s * (spread + 0.35 * (1 - r))
        a1 = a0 - s * (1.1 * (1 - r) - 0.15)
        x, y = cx + 22, cy + s * 3.5
        pts = []
        for seg, (ang, ln, rad) in enumerate(((a0, 8, 2.4), (a1, 7 + 3 * r, 2.1))):
            for t in range(int(ln * 2) + 1):
                pts.append((x + math.cos(ang) * t / 2, y + math.sin(ang) * t / 2, rad))
            x += math.cos(ang) * ln; y += math.sin(ang) * ln
        for px, py, rad in pts: ellipse(ch, px, py, rad, rad, pal['limb'], a)
        rot_ellipse(ch, x + math.cos(a1) * 3.5, y + math.sin(a1) * 3.5, 5.2, 3.6, a1, pal['limb'], a)   # palma
        fx, fy = x + math.cos(a1) * 8, y + math.sin(a1) * 8
        fingers = []
        for d in (1, -1):   # dos dedos en hoz que se curvan el uno hacia el otro y acaban en punta
            fa = a1 + d * (0.3 + op * 0.75)
            qx, qy = fx - math.cos(a1) * 1.5, fy - math.sin(a1) * 1.5
            fp = []
            for t in range(26):
                w = 2.2 * (1 - t / 26) ** 0.8
                if w >= 0.7: ellipse(ch, qx, qy, w, w, pal['limb'], a, shift=0 if d > 0 else -1)
                else: ch.put(qx, qy, C(pal['limb'][3], a))
                fp.append((qx, qy, fa, d))
                fa -= d * (0.02 + 0.09 * (t / 26) ** 1.5)
                qx += math.cos(fa) * 0.5; qy += math.sin(fa) * 0.5
            fingers.append(fp)
        tips.append(((fingers[0][-1][0] + fingers[1][-1][0]) / 2, (fingers[0][-1][1] + fingers[1][-1][1]) / 2))
        ch.fingers = getattr(ch, 'fingers', []) + [(s, fingers)]
    ch.outline()
    for s, fingers in getattr(ch, 'fingers', []):   # puntas claras y filo dentado por dentro
        for fp in fingers:
            for qx, qy, fa, d in fp[-3:]: ch.put(qx, qy, C('#fff0d8'))
            for qx, qy, fa, d in fp[4:-5:3]:
                ch.put_out(qx + math.cos(fa - d * math.pi / 2) * 2.3, qy + math.sin(fa - d * math.pi / 2) * 2.3, C('#fff0d8', 230))
    # patas andadoras, finas y sin contorno
    lg = Canvas(n)
    if legs != 'none':
        for j, u0 in enumerate((19, 16, 13, 10)):
            for s in (-1, 1):
                if legs == 'walk': ang = s * (1.0 - j * 0.18) + 0.35 * math.sin(phase + j * 1.6 + (math.pi if s > 0 else 0))
                elif legs == 'curl': ang = s * 2.2
                else: ang = s * (1.3 - j * 0.1)
                e = 0
                while e < 14 and pro_in(cx + u0 + .5, cy + s * (e + .5)): e += 1
                x, y = cx + u0, cy + s * (e + 1.2)
                for t in range(10):
                    k2 = ang + (s * 0.5 if t > 5 and legs == 'walk' else 0)
                    x += math.cos(k2 - s * 0) * 0.5 * (1 if legs != 'curl' else 0.6); y += math.sin(k2) * 0.5
                    lg.put_out(x, y, C(pal['limb'][2 if t < 7 else 3], a))
    for (x, y), c in body.px.items(): lg.px.pop((x, y), None)
    for (x, y), c in ch.px.items(): lg.px.pop((x, y), None)
    over = abs(tail) > 1.9   # cola por encima del cuerpo (pose de escorpión), con su sombra
    if over:
        for (x, y), c in tl.px.items():
            q = (x + 2, y + 3)
            if q in body.px and q not in tl.px:
                bc = body.px[q]; body.px[q] = (bc[0] * 5 // 10, bc[1] * 5 // 10, bc[2] * 6 // 10, bc[3])
    order = [back, lg, body, tl, ch] if abs(tail) > 0.6 else [back, tl, lg, body, ch]
    cv = merge(*order)
    if sweep_trail:   # estela venenosa del barrido de cola
        a_from, a_to = sweep_trail
        for k in range(24):
            t = a_from + (a_to - a_from) * k / 23
            for rr in (24, 25, 26):
                cv.put_out(px0 + math.cos(math.pi + t) * rr, py0 + math.sin(math.pi + t) * rr, POISON[:3] + (60 + k * 8,))
    if sparks_at is not None:
        for i in sparks_at: sparks(cv, tips[i][0] + 1, tips[i][1], 2)
    return cv

anims = {}
TAU = 2 * math.pi
# ---- fase 1: acorazado ----
anims['Eurypterid_Walk'] = [eurypterid(phase=i * TAU / 6, tail=0.12 * math.sin(i * TAU / 6), dy=0) for i in range(6)]
anims['Eurypterid_PincerWarn'] = [eurypterid(reach=(0, 0), open_=(0.4 + i * 0.2,) * 2, spread=0.25 + i * 0.12, dx=-i * 0.7,
                                             red=i >= 1, legs='brace', phase=0.3 * i) for i in range(4)]
anims['Eurypterid_PincerStrike'] = [
    eurypterid(reach=(0.8, 0.8), open_=(1, 1), spread=0.2, dx=2, red=True, legs='brace'),
    eurypterid(reach=(1, 1), open_=(0, 0), spread=0.12, dx=4, red=True, legs='brace', sparks_at=(0, 1)),
    eurypterid(reach=(0.6, 0.6), open_=(0, 0), spread=0.18, dx=3, legs='brace'),
    eurypterid(reach=(0.2, 0.2), open_=(0.3, 0.3), dx=1)]
# púas venenosas: las púas del caparazón se erizan, se cargan de veneno y sale el abanico de 3 (cuadro 0 del disparo)
anims['Eurypterid_SpineWarn'] = [eurypterid(spike_glow=1, red=True, legs='brace'),
                                 eurypterid(spike_glow=2, red=True, legs='brace', dx=-1),
                                 eurypterid(spike_glow=3, red=True, legs='brace', dx=-2, phase=0.5),
                                 eurypterid(spike_glow=3, red=True, legs='brace', dx=-2, dy=1, phase=1)]
anims['Eurypterid_SpineShoot'] = [eurypterid(burst=True, red=True, legs='brace', dx=1),
                                  eurypterid(burst=True, legs='brace', dx=1, phase=0.5), eurypterid()]
anims['Eurypterid_Hurt'] = [eurypterid(pal=WHITE, eyes='wide'), eurypterid(pal=RED, eyes='wide', open_=(0.8, 0.8))]

# ---- muda (cinemática ~3 s, 12 cuadros de 250 ms): el cuerpo blando sale 16 px (1 u) hacia delante ----
def molt():
    fr = []
    for i, (sdx, crk, tr) in enumerate(((0, 0, 1), (0, 0, -1), (0, 1, 1), (0, 2, -1), (2, 2, 0), (5, 2, 0), (8, 2, 0),
                                       (11, 2, 0), (14, 2, 0), (16, 2, 0), (16, 2, 0), (16, 2, 0))):
        shell = eurypterid(pal=ARMOR if i < 4 else SHED, eyes='angry' if i < 2 else 'closed' if i < 4 else 'none',
                           crack=crk, dx=tr, legs='brace' if i < 4 else 'none', open_=(0.2, 0.2), phase=i)
        if i < 4:
            fr.append(shell); continue
        soft = eurypterid(pal=SOFT, dx=sdx, eyes='closed' if i < 9 else 'angry', red=i == 11,
                          reach=(0.9, 0.9) if i == 10 else (0.1, 0.1), open_=(1, 1) if i >= 10 else (0.1, 0.1),
                          spread=0.5 if i == 10 else 0.25, legs='walk', phase=i * 1.3, tail=0.3 * math.sin(i))
        cv = merge(soft, shell) if i < 7 else merge(shell, soft)
        fr.append(cv)
    return fr
anims['Eurypterid_Molt'] = molt()
anims['Eurypterid_Exuvia'] = [eurypterid(pal=SHED, eyes='none', crack=2, legs='none', open_=(0.2, 0.2))]

# ---- fase 2: blando ----
anims['Eurypterid_Walk_Soft'] = [eurypterid(pal=SOFT, phase=i * TAU / 6, tail=0.18 * math.sin(i * TAU / 6)) for i in range(6)]
anims['Eurypterid_TailWarn'] = [eurypterid(pal=SOFT, tail=-t, tail_glow=g, red=True, legs='brace') for t, g in
                                ((0.5, 1), (1.0, 1), (1.4, 2), (1.6, 2))]
anims['Eurypterid_TailSweep'] = [eurypterid(pal=SOFT, tail=t, tail_glow=2, red=True, legs='brace', sweep_trail=(-1.6, t))
                                 for t in (-0.8, 0.0, 0.8, 1.6)] + [eurypterid(pal=SOFT, tail=1.0, tail_glow=1)]
anims['Eurypterid_DoublePincer'] = [
    eurypterid(pal=SOFT, reach=(0.6, 0), open_=(1, 0.4), red=True, dx=1),
    eurypterid(pal=SOFT, reach=(1, 0), open_=(0, 0.6), red=True, dx=3, sparks_at=(0,)),
    eurypterid(pal=SOFT, reach=(0.3, 0.6), open_=(0.2, 1), red=True, dx=2),
    eurypterid(pal=SOFT, reach=(0, 1), open_=(0.3, 0), red=True, dx=4, sparks_at=(1,)),
    eurypterid(pal=SOFT, reach=(0.2, 0.4), open_=(0.3, 0.2), dx=2),
    eurypterid(pal=SOFT, dx=0)]
anims['Eurypterid_Hurt_Soft'] = [eurypterid(pal=WHITE, eyes='wide', paddle=1), eurypterid(pal=RED, eyes='wide', open_=(0.8, 0.8))]

def death():
    fr = [eurypterid(pal=WHITE, eyes='wide'), eurypterid(pal=PALE, eyes='x', open_=(1, 1), legs='brace'),
          eurypterid(pal=PALE, eyes='x', open_=(0.6, 0.6), legs='curl', tail=0.5, spread=0.6)]
    fr.append(eurypterid(pal=PALE, eyes='x', legs='curl', tail=0.7, spread=0.7).img().rotate(-10, resample=Image.NEAREST))
    for step in (1, 2):
        cv = eurypterid(pal=STONE if step == 2 else PALE, eyes='none', legs='curl', tail=0.7, spread=0.7)
        rnd = random.Random(step)
        for k in range(6 + step * 3):
            x, y = rnd.uniform(14, 112), rnd.uniform(46, 82)
            r = rnd.uniform(1.5, 3.2)
            for yy in range(int(y - r), int(y + r) + 1):
                for xx in range(int(x - r), int(x + r) + 1):
                    if math.hypot(xx - x, yy - y) <= r: cv.put(xx, yy, C(SAND[3 if (xx + yy) % 3 else 2], 230))
        fr.append(cv)
    fos = eurypterid(pal=STONE, eyes='none', legs='curl', tail=0.7, spread=0.7)
    fr += [fos, fos]
    return fr
anims['Eurypterid_Death'] = death()
anims = {k: [as_img(f) for f in v] for k, v in anims.items()}

# ---- efectos ----
def spine():
    """16×8 hacia la derecha: púa venenosa, ahusada, con gotas verdes detrás."""
    fr = []
    cols = ['#4a2466', '#7a44a0', '#b07ad8', '#e8c8ff']
    for k in range(2):
        cv = Canvas(16)
        for x in range(3, 15):
            half = 2.0 * (14 - x) / 11 + 0.2
            for y in range(8):
                if abs(y + .5 - 4) <= half + 0.3:
                    cv.put(x, y, C(cols[min(3, (x - 3) // 3)]) if y + .5 >= 4 or x > 11 else C(cols[min(3, (x - 3) // 3 + 1)]))
        cv.put(13, 3, C('#ffffff')); cv.put(14, 3, C('#e8c8ff'))
        cv.outline()
        cv.put_out(1 - k, 3 + k, POISON[:3] + (200,)); cv.put_out(0 + k, 5 - k, POISON[:3] + (130,))
        fr.append(cv.img().crop((0, 0, 16, 8)))
    return fr

def sweep_fx():
    """128×128 centrado en el jefe: media luna venenosa que barre la mitad de atrás (izquierda), 3 u de radio."""
    fr = []
    for k, (a0, a1, alpha) in enumerate(((-90, -30, 200), (-90, 30, 240), (-90, 90, 255), (-60, 90, 170), (0, 90, 80))):
        im = Image.new('RGBA', (128, 128), (0, 0, 0, 0))
        for y in range(128):
            for x in range(128):
                ddx, ddy = x + .5 - 64, y + .5 - 64
                d = math.hypot(ddx, ddy)
                ang = math.degrees(math.atan2(ddy, -ddx))
                if 38 <= d <= 48 and a0 <= ang <= a1:
                    edge = d > 46.8 or d < 39.2
                    t = (ang - a0) / max(1, a1 - a0)
                    im.putpixel((x, y), (255, 255, 255, alpha) if edge and t > 0.8 else (168, 255, 90, int(alpha * (0.4 + 0.6 * t))))
        rnd = random.Random(k)
        for _ in range(10):   # gotas de veneno
            ang = math.radians(rnd.uniform(a0, a1)); d = rnd.uniform(36, 52)
            x, y = int(64 - math.cos(ang) * d), int(64 + math.sin(ang) * d)
            if 0 <= x < 127 and 0 <= y < 127: im.putpixel((x, y), (200, 255, 140, alpha))
        fr.append(im)
    return fr

fx = {'Proj_EurypteridSpine': spine(), 'Fx_Eurypterid_TailSweep': sweep_fx()}

os.makedirs(f'{OUT}/Eurypterid', exist_ok=True); os.makedirs(f'{OUT}/Effects', exist_ok=True); os.makedirs(f'{OUT}/gif', exist_ok=True)
for k, v in anims.items(): save_sheet(f'{OUT}/Eurypterid/{k}.png', v)
for k, v in fx.items(): save_sheet(f'{OUT}/Effects/{k}.png', v)
CAVE = C('#141a24')
preview(f'{OUT}/vista_previa_euripterido.png', anims, CAVE, scale=2)
preview(f'{OUT}/vista_previa_euripterido_fx.png', {k: [f if f.width > 16 else f.resize((48, 24), Image.NEAREST) for f in v] for k, v in fx.items()}, CAVE, scale=2)
SPEED = {'Walk': 100, 'PincerWarn': 150, 'PincerStrike': 60, 'SpineWarn': 150, 'SpineShoot': 80, 'Hurt': 90, 'Molt': 250,
         'Exuvia': 500, 'TailWarn': 150, 'TailSweep': 50, 'DoublePincer': 70, 'Death': 140}
for k, v in anims.items(): gif(f'{OUT}/gif/{k}.gif', v, SPEED[k.split('_')[1]] if k != 'Eurypterid_Walk_Soft' else 60, CAVE)
for k, v in fx.items(): gif(f'{OUT}/gif/{k}.gif', v, 70, CAVE, scale=3)
print('Listo:', OUT)
