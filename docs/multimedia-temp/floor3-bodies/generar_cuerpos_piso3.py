"""Cuerpos del jugador del piso 3 de Primordia (pantano carbonífero).

Tres cuerpos de ~28-30 px en lienzos de 32×32 (Death en 40×40), con rampas de 5 tonos y luz rebotada:
- Frog (rana venenosa, Área): amarillo oscuro con manchas azules; Boca = Escupitajo venenoso (hincha la garganta y escupe).
- Diplocaulus (Cuerpo a cuerpo): salmón; Boca = Peonza (se mantiene: gira en plano con la cabeza como aspa).
- Whipscorpion (vinagrillo, Proyectil): caoba; Boca = Chorro de vinagre (se mantiene: alza la cola sobre el lomo).
Cada cuerpo esquiva a su manera (rana: salto plano; Diplocaulus: ondulación; vinagrillo: bola que rueda) y al morir
se hunde en un charco de lodo (el pantano del carbón). Paletas de references/pisos.md (sección del piso 3).
Hecho con la skill primordia-sprites (pixel.py): 16 px por unidad, luz arriba a la izquierda, contorno OUTL.

Uso: python generar_cuerpos_piso3.py [carpeta_salida]   (por defecto, la carpeta de este script)
"""
import math, os, sys

def _skill_scripts():
    d = os.path.dirname(os.path.abspath(__file__))
    while True:
        p = os.path.join(d, '.claude', 'skills', 'primordia-sprites', 'scripts')
        if os.path.isdir(p): return p
        if os.path.dirname(d) == d: raise SystemExit('No encuentro .claude/skills/primordia-sprites/scripts')
        d = os.path.dirname(d)
sys.path.insert(0, _skill_scripts())
from pixel import *
from PIL import Image

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__))
BG = C('#1c2e20')                          # fondo de revisión del piso 3 (musgo oscuro)
TRAMOS = {'Swamp': '#273f2b', 'Smoke': '#30332a', 'Burnt': '#2a2528'}

# --- paletas comunes -------------------------------------------------------------------------------
VENOM = ['#2e5a14', '#5aa020', '#a8e040', '#e8ffc0']        # veneno (reacciones)
MUD = ['#3a2a1c', '#55402a', '#735a3a', '#9a7e52']          # charco de lodo
MOUTH = ['#3a1020', '#6a2034', '#b0485a']                   # interior de la boca: fondo, encía, lengua

def five(r4):
    """Rampa de 4 a 5 tonos (para Hurt y Death con las rampas comunes)."""
    return [r4[0]] + list(r4)

def tri_in(px, py, a, b, c):
    def s(p, q, r): return (p[0] - r[0]) * (q[1] - r[1]) - (q[0] - r[0]) * (p[1] - r[1])
    d1, d2, d3 = s((px, py), a, b), s((px, py), b, c), s((px, py), c, a)
    return not ((d1 < 0 or d2 < 0 or d3 < 0) and (d1 > 0 or d2 > 0 or d3 > 0))

def eye(cv, x, y, look=(1, 0), state='open', white=EYE, size=3, lid=None):
    """Ojo de la familia: blanco roto de size×size con pupila de 2×2 hacia look. state: open / shut / x / squint
    (entornado por el esfuerzo: la fila de arriba es el párpado, del color lid)."""
    x, y = int(x), int(y)
    if state == 'shut':
        for dx in range(size): cv.put(x + dx, y + size // 2, OUTL)
        return
    if state == 'x':
        for k in range(size): cv.put(x + k, y + k, OUTL); cv.put(x + size - 1 - k, y + k, OUTL)
        return
    for dx in range(size):
        for dy in range(size): cv.put(x + dx, y + dy, white)
    px = x + (size - 2 if look[0] > 0 else 0 if look[0] < 0 else (size - 2) // 2)
    py = y + (size - 2 if look[1] > 0 else 0 if look[1] < 0 else (size - 2) // 2 + (1 if size > 2 else 0))
    for dx in range(2):
        for dy in range(2): cv.put(px + dx, py + dy, OUTL)
    if state == 'squint':
        for dx in range(size): cv.put(x + dx, y, lid or OUTL)
        cv.put(x + (size - 1 if look[0] < 0 else 0), y + 1, lid or OUTL)

def spots(cv, lst, dx, dy, ink):
    """Manchas: (x, y, ancho, alto); arriba a la izquierda un brillo, la fila de abajo en sombra."""
    for (x, y, w, h) in lst:
        x, y = x + dx, y + dy
        for j in range(h):
            last = j == h - 1 and w > 2
            for k in range(w - (1 if last else 0)):
                cv.put_in(x + k + (1 if last else 0), y + j, C(ink[0] if j == h - 1 and h > 1 else ink[1]))
        cv.put_in(x, y, C(ink[2]))

# =================================================================================================
# Hundirse en el lodo (Death común)
# =================================================================================================
def sink(body, level, pal_mud=MUD, rx=13, bubbles=(), ripple=0, n=40, ground=34, feet=28):
    """Cuerpo (lienzo de 32) pegado en un lienzo de n×n con un charco de lodo bajo los pies.
    level: píxeles hundidos (lo que queda por debajo de la superficie no se ve)."""
    cv = Canvas(n)
    ox, oy = (n - body.w) // 2, ground - feet + level    # los pies del cuerpo (y = feet) en el suelo
    surf = ground - 1
    if rx > 0:
        ellipse(cv, n / 2, surf + 1.5, rx + ripple, 3.4 + ripple * 0.3, pal_mud)
        for x in range(n):                                  # borde del charco un tono más claro (labio de lodo)
            ys = [y for y in range(n) if (x, y) in cv.px]
            if ys: cv.put(x, ys[0], C(pal_mud[3]))
    for (x, y), c in body.px.items():
        X, Y = x + ox, y + oy
        if Y < surf or rx <= 0: cv.put(X, Y, c)
    if rx > 0 and level > 0:                                # onda donde el cuerpo corta la superficie
        xs = [x + ox for (x, y) in body.px if y + oy == surf]
        if xs:
            for x in range(min(xs) - 1, max(xs) + 2): cv.put(x, surf, C(pal_mud[3] if x % 3 else pal_mud[2]))
    for (bx, by, r) in bubbles:
        for yy in range(int(by - r) - 1, int(by + r) + 2):
            for xx in range(int(bx - r) - 1, int(bx + r) + 2):
                d = math.hypot(xx + .5 - bx, yy + .5 - by)
                if r - 0.9 <= d <= r: cv.put(xx, yy, C(pal_mud[3]))
                elif d < r - 0.9: cv.put(xx, yy, C(pal_mud[1]))
        cv.put(bx - r * 0.4, by - r * 0.4, C('#c8b088'))
    return cv

# =================================================================================================
# Rana venenosa (Área · Escupitajo venenoso)
# =================================================================================================
FROG = ramp('#c8a028', 5)                    # amarillo oscuro
INK = ['#173a8a', '#2d64c8', '#7aa8e8']       # manchas azules: sombra, base, brillo
SAC = ramp('#e8d48a', 4)                      # garganta hinchada (crema)
FROG_PAL = {'body': FROG, 'ink': INK, 'sac': SAC, 'eye': EYE, 'details': True}
def flat_pal(r4):
    return {'body': five(r4), 'ink': [r4[1]] * 3, 'sac': list(r4), 'eye': r4[3], 'details': False}
WHITE_PAL, RED_PAL = flat_pal(WHITE4), flat_pal(RED4)
PALE_PAL = {'body': five(PALE4), 'ink': ['#4a4a5a', '#6a6a7a', '#9a9aa4'], 'sac': PALE4, 'eye': EYE, 'details': True}

def venom_blob(cv, x, y, r, wob=0.0, strand=None, tail=None, mask=None):
    """Bola de veneno viscosa: masa de 3 bultos (metabolas) con una gota colgando, burbujas dentro y brillo húmedo.
    wob: fase del temblor (0..1). strand: punto (x, y) de la boca del que sale un hilo de baba hasta la bola.
    tail: punto (x, y) hacia el que se estira la bola como un moco (cola que se adelgaza).
    mask: función (x, y) -> bool; solo se pinta donde es cierta (la bola que asoma entre los labios)."""
    if r <= 0.3: return
    a = wob * 2 * math.pi
    lobes = [(x, y, r * .92), (x - r * .55 + .7 * math.sin(a), y + r * .45, r * .6),
             (x + r * .45, y - r * .4 + .7 * math.cos(a), r * .5),
             (x + r * .2, y + r * 1.0 + .4, r * .32), (x + r * .25, y + r * 1.45 + .6, r * .2)]  # gota que cuelga
    if tail:                                                                         # se estira hacia la boca
        for t_, k in ((.4, .5), (.65, .36), (.88, .26)):
            lobes.append((x + (tail[0] - x) * t_, y + (tail[1] - y) * t_ + math.sin(t_ * math.pi) * .8, r * k))
    def inside(px, py):
        return sum(rr * rr / max(.01, (px - cx) ** 2 + (py - cy) ** 2) for cx, cy, rr in lobes) >= 1.0
    if strand:                                                                       # hilo de baba (fino, sin contorno)
        sx, sy = strand
        n = max(2, int(abs(x - sx) + abs(y - sy)))
        for k in range(n + 1):
            t = k / n
            cv.put(sx + (x - sx) * t, sy + (y + r * .6 - sy) * t + math.sin(t * math.pi) * 1.2, C(VENOM[2]))
    t = Canvas(cv.w, cv.h)
    bx0 = min(x - 2 * r, tail[0] - 2) if tail else x - 2 * r; bx1 = max(x + 2 * r, tail[0] + 2) if tail else x + 2 * r
    by0 = min(y - 2 * r, tail[1] - 2) if tail else y - 2 * r; by1 = max(y + 2 * r, tail[1] + 2) if tail else y + 2 * r
    fill(t, inside, x, y, r * 1.2, r * 1.2, VENOM, box=(bx0, by0, bx1, by1))
    t.outline(); t.clean()
    for (bx, by, br) in ((x + r * .25, y + r * .15, 1), (x - r * .45, y + r * .5, 0), (x + r * .55, y - r * .45, 0)):
        t.put_in(bx, by, C(VENOM[0])); t.put_in(bx - 1, by - 1, C(VENOM[3]))             # burbujas dentro
        if br: t.put_in(bx + 1, by, C(VENOM[0]))
    hx, hy = x - r * .5, y - r * .55                                                  # brillo húmedo
    t.put_in(hx, hy, C(VENOM[3])); t.put_in(hx + 1, hy, C(VENOM[3])); t.put_in(hx, hy + 1, C(VENOM[3]))
    t.put_in(hx + 2, hy - 1 + (1 if r < 3 else 0), C(VENOM[2]))
    for p_, c in t.px.items():
        if mask is None or mask(p_): cv.px[p_] = c

def drool(cv, pts):
    """Gotas de baba colgando de la boca: [(x, y, largo)], finas y sin contorno, con la gota más gruesa al final."""
    for (x, y, n) in pts:
        for k in range(n): cv.put(x, y + k, C(VENOM[2] if k < n - 1 else VENOM[1]))
        cv.put(x, y + n, C(VENOM[1])); cv.put(x + 1, y + n - 1, C(VENOM[2]))

def frog_side(breath=0.0, squash=0.0, lift=0.0, stretch=0.0, mouth=0.0, throat=0.0, gulp=0.0, slime=0.0,
              pal=FROG_PAL, eyes='open'):
    """Rana de perfil mirando a la derecha. squash: agachada; stretch: estirada en salto; lift: en el aire."""
    cv = Canvas(32)
    B = pal['body']; body = B[1:]
    s, q = stretch, squash
    oy = -lift + 2.2 * q
    b = breath * 0.3
    bx, by = 15 - 1 * s, 18.5 - b + oy - 0.8 * s             # centro del cuerpo
    hx, hy = 23.2 + 1 * s, 14.5 - b + oy - 1.6 * s - 0.6 * q  # centro de la cabeza
    # pata trasera: plegada (anca) o estirada hacia atrás en el salto
    if s < 0.35:
        rot_ellipse(cv, 10 - s * 3, 22.5 + oy - 0.3 * q, 6.2 + q, 4.4 - q, -0.3, B[:4], shift=-1)
    else:
        rot_ellipse(cv, 7, 22.5 + oy, 4.4, 2.4, 0.42, B[:4], shift=-1)                          # muslo
        rot_ellipse(cv, 4.4, 25.2 + oy, 2.8, 1.4, 0.05, B[:4], shift=-1)                        # pantorrilla
    rot_ellipse(cv, bx, by, 10.5 * (1 + .12 * q) + 1.8 * s, 7.4 * (1 - .18 * q) + breath * 0.4 - 1.3 * s,
                -0.32 - 0.18 * s, body, shift=-1)
    ellipse(cv, hx, hy, 6.0, 5.0 - .3 * q, body, shift=-1)                                    # cabeza
    ellipse(cv, hx - .7, hy - 4.3 + .3 * q, 3.3, 3.1, body, shift=-1)                         # ojo saltón
    if throat > 0.05:                                                                       # saco de la garganta
        ellipse(cv, hx + .4 + 1.6 * gulp, hy + 4.2 - 1.2 * gulp, 1.2 + 3.0 * throat, 0.8 + 2.4 * throat, pal['sac'])
    if mouth > 0.05:                                                                        # mandíbula que baja
        rot_ellipse(cv, hx + 1.8, hy + 2.6 + 1.2 * mouth, 4.6, 1.6, 0.3 * mouth, body, shift=-1)
    if s < 0.35:                                                                            # pata delantera
        rot_ellipse(cv, 21.5 + 2 * s, 23.5 + oy - 0.4 * q, 1.8, 3.6 - .6 * q, 0.25 + .5 * q, B[:4], shift=-1)
    else:
        rot_ellipse(cv, 21.5, 21.8 + oy, 1.5, 3.2, 0.7, B[:4], shift=-1)                        # brazo recogido
    cv.outline(); cv.clean()
    ddx, ddy = round(bx - 15), round(by - 18.5)
    if pal['details']:
        for x in range(round(5 - 3 * s), round(15 - 2 * s)):                                # pliegue del anca
            y = round(20.5 + oy - 0.3 * q + (x - 6) * 0.35)
            if s < 0.35 and cv.px.get((x, y)) not in (None, OUTL): cv.put(x, y, C(B[0]))
        if s >= 0.35:                                                                       # pliegue del muslo estirado
            for x in range(4, 11):
                y = round(20.6 + oy + (x - 4) * 0.42)
                if cv.px.get((x, y)) not in (None, OUTL): cv.put(x, y, C(B[0]))
        spots(cv, ((7, 15, 3, 2), (11, 12, 3, 2), (15, 15, 3, 3), (10, 19, 3, 2), (18, 12, 2, 2),
                   (5, 21, 2, 2), (14, 21, 3, 2)), ddx, ddy, pal['ink'])
        spots(cv, ((25, 13, 2, 1),), round(hx - 23.2), round(hy - 14.5), pal['ink'])
        cv.put_in(11 + ddx, 12 + ddy, C(B[4])); cv.put_in(12 + ddx, 11 + ddy, C(B[4]))     # brillo del lomo
        cv.put_in(13 + ddx, 11 + ddy, C(B[3]))
    # boca: línea cerrada o abierta en cuña
    mx, my = round(hx - 2.2), round(hy + 2.5)
    cv.info = {'hx': hx, 'hy': hy, 'body': set(cv.px), 'mouth': set()}
    if mouth > 0.05:
        a, u, l = (mx - .5, my + .5), (hx + 7.5, hy + 1.2 - 2.2 * mouth), (hx + 6.8, hy + 3.0 + 3.6 * mouth)
        cv.info['mouth'] = {p for p in cv.px if tri_in(p[0] + .5, p[1] + .5, a, u, l)}
        for (x, y) in [p for p in list(cv.px) if tri_in(p[0] + .5, p[1] + .5, a, u, l)]:
            cv.put(x, y, C(MOUTH[0] if not pal['details'] else MOUTH[1] if y > (a[1] + l[1]) / 2 + .5 else MOUTH[0]))
        if not pal['details']:
            for (x, y) in list(cv.px):
                if tri_in(x + .5, y + .5, a, u, l): cv.put(x, y, C(B[0]))
    else:
        for x in range(mx, round(hx + 6)): cv.put_in(x, my + (1 if x < mx + 2 else 0), C(B[0]))
    if slime > 0:                                                                          # baba entre los labios
        lx = round(hx + 5)
        if mouth > .4:
            for y in range(round(hy + 1.5 - 1.5 * mouth), round(hy + 3.5 + 2.5 * mouth)): cv.put(lx, y, C(VENOM[2]))
        drool(cv, [(lx - 1, round(hy + 4.5 + 2.8 * mouth), 1 + round(2 * slime))])
    ex, ey = round(hx - 2.2), round(hy - 5.5 + .3 * q)                                         # ojo
    eye(cv, ex, ey, (1, 0), eyes, pal['eye'], lid=C(B[1]))
    if pal['details']: cv.put_in(ex - 1, ey - 1, C(B[3]))
    # dedos finos (sin contorno)
    if s < 0.35:
        fy = round(27 + oy - 2.2 * q + 0.4 * q)
        for dx in (-1, 1, 2): cv.put_out(21.5 + 2 * s + dx, fy, C(B[1]))
        for dx in (-3, -2, 0): cv.put_out(10 + dx, round(26.6 + oy), C(B[1]))
    else:
        for k in range(3): cv.put_out(1, round(24.5 + oy) + k, C(B[1]))                       # pie trasero estirado
        for k in range(2): cv.put_out(19 - k, round(24.8 + oy) + k, C(B[1]))                   # mano recogida
    return cv

def frog_front(breath=0.0, squash=0.0, lift=0.0, stretch=0.0, mouth=0.0, throat=0.0, gulp=0.0, slime=0.0,
               pal=FROG_PAL, eyes='open', back=False):
    """Rana de frente (down) o de espaldas (back=True, up)."""
    cv = Canvas(32)
    B = pal['body']; body = B[1:]
    s, q = stretch, squash
    oy = -lift + 2 * q
    b = breath * 0.3
    cy = 19.5 - b + oy - .5 * s
    rx, ry = 10.8 * (1 + .12 * q) - 1.2 * s, 7.2 * (1 - .15 * q) + 1.8 * s + breath * .3
    # patas traseras plegadas a los lados (o estiradas en el salto)
    for side in (-1, 1):
        if s < 0.35:
            rot_ellipse(cv, 16 + side * (9.6 + q * .6), 23.5 + oy - .3 * q, 3.8, 3.0 - .5 * q, side * 0.5, B[:4], shift=-1)
        else:
            rot_ellipse(cv, 16 + side * (8 + s * .8), 24 + oy + (1.2 if back else -1) * s, 2.2, 4 + s * .6,
                        side * (0.5 if back else -0.6), B[:4], shift=-1)
    ellipse(cv, 16, cy, rx, ry, body, shift=-1)
    ey0 = cy - ry + 1.2 - .5 * q
    for side in (-1, 1): ellipse(cv, 16 + side * 5.6, ey0, 3.4, 3.1, body, shift=-1)               # ojos saltones
    if throat > 0.05 and not back:
        ellipse(cv, 16, cy + 3.8 + throat - 2 * gulp, 2 + 4.4 * throat, 1.2 + 2.8 * throat, pal['sac'])
    if throat > 0.05 and back:                                                                       # se ve hinchar por los lados
        for side in (-1, 1): ellipse(cv, 16 + side * (8.5 + 1.6 * throat), cy - 1, 1.6 * throat + .6, 2 * throat + .6, pal['sac'])
    if not back:                                                                                     # patas delanteras
        for side in (-1, 1):
            rot_ellipse(cv, 16 + side * (5.5 + q * 1.5), cy + ry - 1.2 - .3 * q, 1.9, 2.8 - .5 * q, side * -0.2, B[:4], shift=-1)
    cv.outline(); cv.clean()
    dy = round(cy - 19.5)
    cv.info = {'ey0': ey0, 'my': round(cy + 1.5), 'body': set(cv.px), 'mouth': set()}
    if pal['details']:
        if back:
            spots(cv, ((9, 15, 3, 2), (15, 13, 2, 2), (20, 15, 3, 2), (12, 19, 3, 2), (18, 20, 3, 2), (8, 22, 2, 1),
                       (22, 22, 2, 2), (15, 23, 2, 1)), 0, dy, pal['ink'])
        else:
            spots(cv, ((8, 17, 2, 2), (23, 17, 2, 2), (12, 13, 2, 1), (19, 13, 2, 1)), 0, dy, pal['ink'])
        cv.put_in(9, round(cy - 3), C(B[4])); cv.put_in(10, round(cy - 4), C(B[4]))
    if back:
        for side in (-1, 1):                                                                         # ojos por detrás
            x0 = round(16 + side * 5.6 - 1)
            cv.put_in(x0, round(ey0 - 1), C(B[4] if side < 0 else B[3]))
    else:
        my = round(cy + 1.5)
        if mouth > 0.05:
            for x in range(9, 24):
                h = max(0, round((1 - ((x - 16) / 7.5) ** 2) * (1 + 3.2 * mouth)))
                for k in range(h):
                    if (x, my + k) in cv.px: cv.info['mouth'].add((x, my + k))
                    cv.put_in(x, my + k, C(MOUTH[0] if k < h - 1 or not pal['details'] else MOUTH[1]))
        else:
            for x in range(9, 24):
                cv.put_in(x, my + (1 if 11 <= x <= 20 else 0) - (1 if x in (9, 23) else 0), C(B[0]))
        for side in (-1, 1):
            eye(cv, round(16 + side * 5.6 - 1), round(ey0 - 1.5), (0, 1), eyes, pal['eye'], lid=C(B[1]))
    if not back and slime > 0:
        my = round(cy + 1.5); h = round(1 + 3.2 * mouth)
        drool(cv, [(13, my + h, 1 + round(2 * slime)), (19, my + h - 1, round(1 + slime))])
    # dedos finos
    if not back:
        for side in (-1, 1):
            fx, fy = 16 + side * (5.5 + q * 1.5), round(cy + ry + 1.2 - .3 * q)
            for dx in (-1, 0, 1): cv.put_out(fx + dx * 1.4, fy + (1 if dx else 0), C(B[1]))
    for side in (-1, 1):
        if s < 0.35:
            fx = 16 + side * (12.3 + q * .6)
            for k in range(3): cv.put_out(fx + side * k * 0.7, round(25.5 + oy - .3 * q) + k % 2, C(B[1]))
        else:
            fx, fy = 16 + side * (8.6 + 1.2 * s), 24 + oy + ((1.2 if back else -1) * s) + (4.4 + s * .6) * (1 if back else -1)
            for dx in (-1, 0, 1): cv.put_out(fx + dx, fy, C(B[1]))
    return cv

def frog_view(view, **k):
    return frog_side(**k) if view == 'side' else frog_front(back=(view == 'up'), **k)

def frog_attack(view, emerge=0.0, glob=0.0, **k):
    """Cuadro del Escupitajo en un lienzo de 40×40 (la rana en el centro, en el mismo sitio que en 32×32).
    emerge: la bola asoma entre los labios (0..1, tapada por el cuerpo salvo en la boca); glob: la bola ya suelta."""
    base = frog_view(view, **k)
    cv, o = Canvas(40), 4
    for (x, y), c in base.px.items(): cv.px[(x + o, y + o)] = c
    inf = base.info
    body = {(x + o, y + o) for (x, y) in inf['body']}
    mouth = {(x + o, y + o) for (x, y) in inf['mouth']}
    lips = lambda p: p not in body or p in mouth
    behind = lambda p: p not in body
    if view == 'side':
        hx, hy = inf['hx'] + o, inf['hy'] + o
        if emerge > 0:
            venom_blob(cv, hx + 2 + 3.2 * emerge, hy + 3.3, 1.0 + 2.3 * emerge, wob=emerge, mask=lips)
        if glob > 0:
            venom_blob(cv, hx + 7.4, hy + 3.4, 3.0, wob=.3, tail=(hx + 4.6, hy + 3))
    elif view == 'down':
        my = inf['my'] + o
        if emerge > 0: venom_blob(cv, 16 + o, my + 1 + 2.4 * emerge, 1.0 + 2.4 * emerge, wob=emerge, mask=lips)
        if glob > 0: venom_blob(cv, 16 + o, my + 7, 3.2, wob=.3, tail=(16 + o, my + 1))
    else:
        ey = inf['ey0'] + o
        if emerge > 0: venom_blob(cv, 16.5 + o, ey + 1 - 3.5 * emerge, 1.0 + 2.2 * emerge, wob=emerge, mask=behind)
        if glob > 0: venom_blob(cv, 16.5 + o, ey - 6, 3.1, wob=.3, tail=(16.3 + o, ey))
    return cv

# Escupitajo: hincha la garganta, entorna los ojos, la hinchazón sube, la bola asoma y sale a empujones (temblando),
# se suelta en el cuadro 7 (el que cuenta) con un hilo de moco y la rana babea y se recupera.
FROG_SPIT = [dict(throat=.3), dict(throat=.7, squash=.1), dict(throat=1, squash=.2, eyes='squint'),
             dict(throat=.8, gulp=.5, squash=.12, mouth=.25, emerge=.12, eyes='squint'),
             dict(throat=.5, gulp=1, squash=.28, mouth=.55, emerge=.35, eyes='squint'),
             dict(throat=.25, gulp=1, squash=.18, mouth=.8, emerge=.6, eyes='squint'),
             dict(squash=.35, mouth=1.05, emerge=.85, eyes='squint'),
             dict(stretch=.2, mouth=1, glob=1),
             dict(stretch=.1, mouth=.75, slime=1), dict(mouth=.45, slime=.7, throat=.15),
             dict(mouth=.15, slime=.35), dict()]
FROG_SPIT_FRAME = 7

def frog_anims():
    A = {}
    for v in ('side', 'down', 'up'):
        A[f'Player_Frog_Idle_{v}'] = [frog_view(v, breath=math.sin(i * math.pi / 2) * 0.8 + 0.8,
                                                throat=(0.3 if i == 1 and v != 'up' else 0)) for i in range(4)]
        A[f'Player_Frog_Move_{v}'] = [frog_view(v, squash=.3), frog_view(v, stretch=.45, lift=1.5),
                                      frog_view(v, stretch=.25, lift=2.5), frog_view(v, squash=.15)]
        A[f'Player_Frog_Dodge_{v}'] = [frog_view(v, squash=.5), frog_view(v, stretch=1, lift=2),
                                       frog_view(v, stretch=1, lift=3.5), frog_view(v, stretch=.6, lift=1.5),
                                       frog_view(v, squash=.35)]
        A[f'Player_Frog_Attack_{v}'] = [frog_attack(v, **f) for f in FROG_SPIT]
    A['Player_Frog_Hurt'] = [frog_side(pal=WHITE_PAL, squash=.2), frog_side(pal=RED_PAL, squash=.1)]
    flat = frog_side(squash=.55, eyes='x')
    pale = frog_side(squash=.55, eyes='x', pal=PALE_PAL)
    A['Player_Frog_Death'] = [sink(frog_side(pal=WHITE_PAL, squash=.3), 0, rx=0), sink(flat, 0, rx=0),
                              sink(pale, 1, rx=9), sink(pale, 6, rx=12, bubbles=((10, 30, 1.2),)),
                              sink(pale, 11, rx=13, bubbles=((29, 29, 1.5), (12, 31, 1))),
                              sink(pale, 15, rx=13, ripple=.5, bubbles=((20, 29, 2),)),
                              sink(pale, 30, rx=11, bubbles=((22, 31, 1.2),))]
    return A

FROG_MS = {'Idle': 160, 'Move': 90, 'Dodge': 60, 'Attack': 80, 'Hurt': 80, 'Death': 120}

# =================================================================================================
# Volumen: elipsoides trazados con rayos (para el Diplocaulus y el vinagrillo)
# =================================================================================================
# Los cuerpos que giran o se ven muy distintos según la vista se modelan con elipsoides en coordenadas del animal
# (f adelante, l izquierda, u arriba) y se proyectan con una cámara ortográfica de 3/4 (elevación ELEV). Cada vista
# (side, down, up) y cada ángulo del giro salen del mismo modelo, con el sombreado de cúpula de pixel.py
# (luz arriba a la izquierda, luz rebotada en el borde en sombra) y las rampas de 5 del piso.
ELEV = math.radians(55)
_RIGHT = (1.0, 0.0, 0.0)
_UP = (0.0, math.sin(ELEV), math.cos(ELEV))         # eje de pantalla hacia arriba, en el mundo
_DIR = (0.0, math.cos(ELEV), -math.sin(ELEV))       # dirección de la mirada (hacia el fondo y abajo)
_L = LIGHT; _LN = math.sqrt(sum(v * v for v in _L)); _LIT = tuple(v / _LN for v in _L)
YAW = {'side': 0.0, 'down': -90.0, 'up': 90.0}

class camera:
    """Cambia la elevación de la cámara mientras dura el bloque: with camera(30): ..."""
    def __init__(s, deg): s.deg = deg
    def __enter__(s):
        global _UP, _DIR
        s.old = (_UP, _DIR); e = math.radians(s.deg)
        _UP, _DIR = (0.0, math.sin(e), math.cos(e)), (0.0, math.cos(e), -math.sin(e))
    def __exit__(s, *a):
        global _UP, _DIR
        _UP, _DIR = s.old

def _dot(a, b): return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]

class Part:
    """Elipsoide: centro (f, l, u) del animal, giro en el plano alpha (grados, 0 = hacia delante), radios
    (rf, rl, ru), grupo (para las líneas interiores entre piezas) y material (clave de la paleta)."""
    def __init__(s, f, l, u, rf, rl, ru, alpha=0.0, group='body', mat='body', pitch=0.0):
        s.c, s.r, s.alpha, s.group, s.mat, s.pitch = (f, l, u), (rf, rl, ru), alpha, group, mat, pitch

def project(p):
    """Punto del mundo -> (x de pantalla, y de pantalla hacia abajo, profundidad)."""
    return _dot(p, _RIGHT), -_dot(p, _UP), _dot(p, _DIR)

def to_world(yaw, f, l, u):
    y = math.radians(yaw); F = (math.cos(y), math.sin(y), 0.0); L = (-math.sin(y), math.cos(y), 0.0)
    return (F[0] * f + L[0] * l, F[1] * f + L[1] * l, u)

def trace(parts, n, yaw, origin, pal, flat=None, reflect=True):
    """Dibuja las piezas en un lienzo de n×n. origin: (x, y) de pantalla del punto (0, 0, 0) del animal.
    pal: {material: rampa de 5}. Devuelve el lienzo y un mapa (x, y) -> (profundidad, pieza, punto local)."""
    W = []
    y = math.radians(yaw); F = (math.cos(y), math.sin(y), 0.0); L = (-math.sin(y), math.cos(y), 0.0)
    for p in parts:
        a, pt = math.radians(p.alpha), math.radians(p.pitch)
        a1 = tuple(F[i] * math.cos(a) + L[i] * math.sin(a) for i in range(3))
        a2 = tuple(-F[i] * math.sin(a) + L[i] * math.cos(a) for i in range(3))
        a3 = (0.0, 0.0, 1.0)
        if pt:                                   # cabeceo: inclina el eje largo hacia arriba
            a1, a3 = (tuple(a1[i] * math.cos(pt) + a3[i] * math.sin(pt) for i in range(3)),
                      tuple(-a1[i] * math.sin(pt) + a3[i] * math.cos(pt) for i in range(3)))
        c = to_world(yaw, *p.c)
        W.append((p, c, (a1, a2, a3)))
    cv = Canvas(n); info = {}
    ox, oy = origin
    for py in range(n):
        for px in range(n):
            u, v = px + .5 - ox, py + .5 - oy
            O = tuple(_RIGHT[i] * u - _UP[i] * v - _DIR[i] * 60 for i in range(3))
            best = None
            for (p, c, ax) in W:
                d0 = tuple(O[i] - c[i] for i in range(3))
                q0 = [_dot(d0, ax[k]) / p.r[k] for k in range(3)]
                qd = [_dot(_DIR, ax[k]) / p.r[k] for k in range(3)]
                A = sum(x * x for x in qd); Bq = 2 * sum(q0[k] * qd[k] for k in range(3)); Cq = sum(x * x for x in q0) - 1
                disc = Bq * Bq - 4 * A * Cq
                if disc < 0: continue
                t = (-Bq - math.sqrt(disc)) / (2 * A)
                if best is None or t < best[0]:
                    q = [q0[k] + qd[k] * t for k in range(3)]
                    best = (t, p, q, ax)
            if not best: continue
            t, p, q, ax = best
            nw = [sum(ax[k][i] * q[k] / p.r[k] for k in range(3)) for i in range(3)]
            m = math.sqrt(sum(x * x for x in nw)) or 1
            nw = [x / m for x in nw]
            info[(px, py)] = (t, p, q, nw)
    # sombreado con la normal suavizada (media de 5×5 del mismo grupo): sin bultos en las uniones de las piezas
    for (x, y), (t, p, q, nw) in info.items():
        acc = [0.0, 0.0, 0.0]
        for dy in range(-2, 3):
            for dx in range(-2, 3):
                o = info.get((x + dx, y + dy))
                if o and o[1].group == p.group and abs(o[0] - t) < 3:
                    wgt = 2.0 if (dx, dy) == (0, 0) else 1.0
                    for i_ in range(3): acc[i_] += o[3][i_] * wgt
        m = math.sqrt(sum(v * v for v in acc)) or 1
        ns_w = [v / m for v in acc]
        ns = (_dot(ns_w, _RIGHT), -_dot(ns_w, _UP), -_dot(ns_w, _DIR))     # normal en pantalla (z hacia la cámara)
        lam = _dot(ns, _LIT)
        i = 0 if lam < -0.2 else 1 if lam < 0.5 else 2 if lam < 0.88 else 3 if lam < 0.97 else 4
        if reflect and lam < 0.0 and ns[2] < 0.35: i += 1                    # luz rebotada en el borde en sombra
        cv.px[(x, y)] = C((flat or pal[p.mat])[max(0, min(4, i))])
    # quita el ruido: un píxel distinto de sus 4 vecinos del mismo grupo, que coinciden, toma su color
    for _ in range(2):
        for (x, y), (t, p, q, nw) in list(info.items()):
            nb = [cv.px[(x + dx, y + dy)] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                  if (x + dx, y + dy) in info and info[(x + dx, y + dy)][1].group == p.group]
            if len(nb) >= 3 and cv.px[(x, y)] not in nb:
                best = max(set(nb), key=nb.count)
                if nb.count(best) >= 3: cv.px[(x, y)] = best
    # líneas interiores donde una pieza tapa a otra de otro grupo con un salto de profundidad
    for (x, y), (t, p, q, nw) in list(info.items()):
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            o = info.get((x + dx, y + dy))
            if o and o[1].group != p.group and t - o[0] > 1.6:
                cv.px[(x, y)] = C((flat or pal[p.mat])[0]); break
    return cv, info

def screen_of(yaw, origin, f, l, u):
    x, y, d = project(to_world(yaw, f, l, u))
    return x + origin[0], y + origin[1], d

# =================================================================================================
# Diplocaulus (Cuerpo a cuerpo · Peonza)
# =================================================================================================
DIPLO = ramp('#d8806a', 5)                    # salmón
DIPLO_D = ramp('#8a3a4a', 5)                  # manchas granate oscuro del lomo
DIPLO_PAL = {'body': DIPLO, 'leg': [DIPLO[0]] + DIPLO[:4], 'spot': DIPLO_D}

def diplo_parts(A=0.0, phase=0.0, walk=None, crouch=0.0, stretch=0.0, curl=0.0, head_yaw=0.0, tuck=0.0,
                breath=0.0, lift=0.0):
    """Modelo del Diplocaulus centrado en el medio del cuerpo (hocico f≈+13, cola f≈-13,5, cuernos ±10).
    A: amplitud de la ondulación lateral; phase: fase de la onda; walk: fase del paso (0..1); crouch: pegado al suelo;
    stretch: estirado hacia delante; curl: curva el cuerpo en C (positivo hacia la izquierda; 10 ≈ cola 5 px a un lado); tuck: patas recogidas."""
    P = []
    k = 0.32
    def lat(f):                                       # desplazamiento lateral de la columna (la cola ondula más)
        env = max(0.0, min(1.0, (9 - f) / 20))
        return A * env * math.sin(k * f + phase * 2 * math.pi) + curl / 600 * (min(f, 5) - 5) ** 2
    def ang(f):
        return math.degrees(math.atan2(lat(f + .5) - lat(f - .5), 1.0))
    u0 = 2.5 - 0.9 * crouch + lift
    sf = 1 + 0.12 * stretch
    for (f, rf, rl, ru) in ((5.0, 3.0, 3.9, 2.4 + .15 * breath), (1.6, 3.0, 4.2, 2.6 + .2 * breath),
                            (-1.8, 3.0, 4.0, 2.5 + .15 * breath), (-5.0, 2.8, 3.3, 2.2)):
        F = f * sf; L = lat(F)
        P.append(Part(F, L, u0, rf, rl, ru, ang(F), 'body', 'body'))
    for (f, rf, rl, ru, du) in ((-8.3, 2.2, 1.6, 2.0, .2), (-10.9, 1.9, 1.15, 1.6, .5), (-13.0, 1.5, .7, 1.1, .8)):
        F = f * sf; L = lat(F)
        P.append(Part(F, L, u0 + du - .4 * crouch, rf, rl, ru, ang(F), 'body', 'body'))   # cola en remo
    # cabeza en bumerán: placa central, hocico y dos cuernos de 2 piezas hacia atrás y afuera
    hy = head_yaw; hf = 8.6 * sf; hl = lat(hf) * .4
    def H(df, dl):                                    # punto de la cabeza girado head_yaw alrededor del cuello
        a = math.radians(hy); return (hf - 2.5 + df * math.cos(a) - dl * math.sin(a), hl + df * math.sin(a) + dl * math.cos(a))
    hu = u0 + .5 - .3 * crouch
    f_, l_ = H(2.5, 0); P.append(Part(f_, l_, hu, 3.6, 4.4, 1.6, hy, 'head', 'body'))
    f_, l_ = H(5.4, 0); P.append(Part(f_, l_, hu - .1, 1.9, 2.7, 1.3, hy, 'head', 'body'))
    for sd in (-1, 1):
        f_, l_ = H(0.4, sd * 5.9); P.append(Part(f_, l_, hu - .1, 4.0, 1.9, 1.05, hy + sd * 118, 'head', 'body'))
        f_, l_ = H(-1.9, sd * 9.3); P.append(Part(f_, l_, hu - .2, 2.9, 1.1, .8, hy + sd * 128, 'head', 'body'))
    # patas cortas (diagonales alternas al andar)
    for (f, sd, ph) in ((4.6, 1, 0), (4.6, -1, .5), (-4.4, 1, .5), (-4.4, -1, 0)):
        w = math.sin((walk + ph) * 2 * math.pi) if walk is not None else 0
        F = f * sf + 1.2 * w; L = lat(F) + sd * (5.0 + .5 * crouch - 2.2 * tuck)
        P.append(Part(F, L, 0.9 + .6 * max(0, w) + lift * .7, 2.0, 1.4, 1.3 - .5 * tuck,
                      ang(F) + sd * 25, 'leg', 'leg'))
    return P

DIPLO_ORIGIN = {'side': (16, 19), 'down': (16, 15.5), 'up': (16, 16.5)}
DIPLO_SPOTS = ((3.4, 1.5, 1.3), (0.0, -1.7, 1.4), (-3.4, 1.4, 1.3), (-6.6, -.8, 1.1), (-9.6, .2, .8), (6.9, 0.0, 1.0))

def diplo_render(view='side', yaw=None, n=32, origin=None, pal=None, flat=None, eyes='open', **pose):
    yaw = YAW[view] if yaw is None else yaw
    origin = origin or (DIPLO_ORIGIN[view] if n == 32 else (n / 2, n / 2 + 1.5))
    parts = diplo_parts(**pose)
    cv, info = trace(parts, n, yaw, origin, pal or DIPLO_PAL, flat=flat)
    if not flat:
        # manchas del lomo y de la cabeza (sobre la cara de arriba de las piezas del cuerpo)
        for (x, y), (t, p, q, nw) in info.items():
            if nw[2] < 0.3 or p.mat != 'body' or cv.px[(x, y)] == C(DIPLO[0]): continue
            fw = p.c[0] + q[0] * p.r[0]
            lw = p.c[1] + q[1] * p.r[1]
            for (sfp, slp, sr) in DIPLO_SPOTS:
                if (fw - sfp) ** 2 * 1.3 + (lw - slp) ** 2 < sr * sr * 0.8:
                    bp = (pal or DIPLO_PAL); lvl = [C(c) for c in bp['body']].index(cv.px[(x, y)]) if cv.px[(x, y)] in [C(c) for c in bp['body']] else 2
                    cv.px[(x, y)] = C(bp['spot'][max(1, min(3, lvl))]); break
    cv.outline(); cv.clean()
    # ojos pequeños encima de la cabeza, cerca del hocico
    hd = [p for p in parts if p.group == 'head'][0]
    a = math.radians(hd.alpha)
    for sd in (-1, 1):
        ef, el = hd.c[0] + 0.3 * math.cos(a) - sd * 2.3 * math.sin(a), hd.c[1] + 0.3 * math.sin(a) + sd * 2.3 * math.cos(a)
        x, y, d = screen_of(yaw, origin, ef, el, hd.c[2] + 1.2)
        hit = info.get((int(x), int(y)))
        if not hit or hit[1].group != "head" or abs(hit[0] - 60 - d) > 1.5 or math.sin(math.radians(yaw)) > 0.7: continue
        x, y = int(x), int(y)
        if eyes == 'shut' or flat:
            cv.put(x, y, OUTL if not flat else C(flat[1])); cv.put(x + 1, y, OUTL if not flat else C(flat[1]))
            continue
        cv.put(x, y, EYE); cv.put(x + 1, y, EYE); cv.put(x, y - 1, EYE); cv.put(x + 1, y - 1, EYE)
        look = math.cos(math.radians(yaw + hd.alpha))
        cv.put(x + (1 if look > -.3 else 0), y, OUTL)
        if eyes == 'dizzy': cv.put(x, y - 1, OUTL); cv.put(x + 1, y, EYE)
    # boca: borde de delante del hocico, si se ve
    sn = [p for p in parts if p.group == 'head'][1]
    a = math.radians(sn.alpha)
    for k in range(-5, 6):
        ang_ = math.radians(k * 14)
        lf, ll = sn.r[0] * math.cos(ang_) * .95, sn.r[1] * math.sin(ang_) * .95
        x, y, d = screen_of(yaw, origin, sn.c[0] + lf * math.cos(a) - ll * math.sin(a),
                            sn.c[1] + lf * math.sin(a) + ll * math.cos(a), sn.c[2] - .3)
        hit = info.get((int(x), int(y)))
        if hit and hit[1] is sn and abs(hit[0] - 60 - d) < 1.0 and not flat:
            cv.put(int(x), int(y), C(DIPLO[0]))
    cv.info = {'origin': origin, 'yaw': yaw}
    return cv

def diplo_trails(cv, yaw, origin, n_tr=3):
    """Estelas del giro: arcos finos detrás de las puntas de los cuernos y de la cola (sin contorno)."""
    for (f, l) in ((-1.0, 10.8), (-1.0, -10.8), (-14.2, 0)):
        for k in range(1, n_tr * 4):
            a = yaw - k * 7
            x, y, d = screen_of(a, origin, f, l, 2.4)
            c = DIPLO[4] if k < 4 else DIPLO[3] if k < 8 else DIPLO[2]
            if k % 4 != 3: cv.put_out(x, y, C(c))

def diplo_spin(i, n=8, speed=1.0, trails=True, pal=None, flat=None):
    yaw = -i * 360 / n
    cv = diplo_render(yaw=yaw, n=40, tuck=1, crouch=.3, pal=pal, flat=flat)
    if trails: diplo_trails(cv, yaw, cv.info['origin'])
    return cv

def dizzy_stars(cv, cx, cy, ph):
    for k in range(3):
        a = ph * 2 * math.pi / 3 + k * 2 * math.pi / 3
        x, y = cx + 6 * math.cos(a), cy + 2 * math.sin(a)
        if math.sin(a) > -0.2: sparks(cv, x, y, 1)
        else: cv.put(x, y, SPARK[2])

def diplo_anims():
    A = {}
    for v in ('side', 'down', 'up'):
        A[f'Player_Diplocaulus_Idle_{v}'] = [diplo_render(v, A=.5, phase=i / 4, breath=math.sin(i * math.pi / 2))
                                             for i in range(4)]
        A[f'Player_Diplocaulus_Move_{v}'] = [diplo_render(v, A=1.6, phase=i / 4, walk=i / 4) for i in range(4)]
        A[f'Player_Diplocaulus_Dodge_{v}'] = [diplo_render(v, crouch=.8, A=.6, phase=0),
                                              diplo_render(v, crouch=.6, A=2.6, phase=.2, stretch=.6),
                                              diplo_render(v, crouch=.6, A=2.6, phase=.7, stretch=.75),
                                              diplo_render(v, crouch=.5, A=1.6, phase=1.2, stretch=.6),
                                              diplo_render(v, crouch=.2, A=.6, phase=1.6)]
        y0 = YAW[v]
        A[f'Player_Diplocaulus_AttackStart_{v}'] = [
            diplo_render(v, n=40, crouch=.6, curl=6),
            diplo_render(yaw=y0 - 25, n=40, crouch=.5, curl=14, tuck=.5),
            diplo_render(yaw=y0 - 70, n=40, crouch=.3, tuck=1)]
    A['Player_Diplocaulus_Spin'] = [diplo_spin(i) for i in range(8)]
    A['Player_Diplocaulus_SpinEnd'] = [diplo_render(yaw=-30, n=40, tuck=.6, crouch=.3, curl=-6),
                                       diplo_render(yaw=8, n=40, tuck=.2, crouch=.2, curl=4, head_yaw=-8),
                                       diplo_render(yaw=0, n=40)]
    dz = []
    for i in range(4):
        cv = diplo_render(yaw=0, n=40, head_yaw=10 * math.sin(i * math.pi / 2), A=.8, phase=i / 4, eyes='dizzy')
        dizzy_stars(cv, 20 + 6, 9, i / 4); dz.append(cv)
    A['Player_Diplocaulus_Dizzy'] = dz
    A['Player_Diplocaulus_Hurt'] = [diplo_render('side', flat=WHITE4 + [WHITE4[0]], crouch=.3),
                                    diplo_render('side', flat=five(RED4), crouch=.2)]
    pale = {'body': five(PALE4), 'leg': five(PALE4), 'spot': five(PALE4)}
    limp = lambda **k: diplo_render('side', crouch=1, A=.4, curl=3, eyes='shut', **k)
    feet = max(y for (x, y) in limp().px)
    A['Player_Diplocaulus_Death'] = [sink(diplo_render('side', flat=WHITE4 + [WHITE4[0]], crouch=.5), 0, rx=0, feet=feet),
                                     sink(limp(), 0, rx=0, feet=feet), sink(limp(pal=pale), 1, rx=14, feet=feet),
                                     sink(limp(pal=pale), 3, rx=15, bubbles=((8, 31, 1.2),), feet=feet),
                                     sink(limp(pal=pale), 6, rx=15, bubbles=((31, 30, 1.5), (12, 32, 1)), feet=feet),
                                     sink(limp(pal=pale), 9, rx=15, ripple=.5, bubbles=((20, 30, 2),), feet=feet),
                                     sink(limp(pal=pale), 30, rx=12, bubbles=((22, 32, 1.2),), feet=feet)]
    return A

DIPLO_MS = {'Idle': 170, 'Move': 100, 'Dodge': 55, 'AttackStart': 70, 'SpinEnd': 80, 'Spin': 40, 'Dizzy': 125,
            'Hurt': 80, 'Death': 120}

# =================================================================================================
# Vinagrillo (escorpión látigo; Proyectil · Chorro de vinagre)
# =================================================================================================
WHIP = ramp('#8a4a2e', 5, light=0.3)          # caoba
TAN = ramp('#e0b070', 5)                      # detalle: látigo, puntas de las pinzas, articulaciones, vinagre
WHIP_PAL = {'body': WHIP, 'palp': [WHIP[0], WHIP[0], WHIP[1], WHIP[2], WHIP[3]], 'tan': TAN}

def line_px(x0, y0, x1, y1):
    n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
    return [(round(x0 + (x1 - x0) * k / n), round(y0 + (y1 - y0) * k / n)) for k in range(n + 1)]

def whip_skeleton(raise_=0.0, tremble=0.0, crouch=0.0):
    """Columna del abdomen: 4 segmentos y el pigidio en arco. raise_ (0..1) lo alza sobre el lomo (hasta 115°).
    Devuelve [(f, u, ángulo)] de cada pieza y la punta del pigidio con su dirección."""
    th = math.radians(105 * raise_)
    pos = (1.0, 2.4 - .5 * crouch); out = []
    for i, (step, w) in enumerate(((1.5, .3), (2.4, .7), (2.5, .95), (2.3, 1.15), (1.8, 1.3))):
        phi = th * w + (math.radians(6) * tremble * (1 if i % 2 else -1) if raise_ > .5 else 0)
        pos = (pos[0] - step * math.cos(phi), pos[1] + step * math.sin(phi))
        out.append((pos[0], pos[1], phi))
    f, u, phi = out[-1]
    tip = (f - 1.1 * math.cos(phi), u + 1.1 * math.sin(phi), phi)
    return out, tip

def whip_parts(walk=None, raise_=0.0, tremble=0.0, crouch=0.0, ball=None, claw=0.0, breath=0.0):
    """Modelo del vinagrillo centrado en el caparazón. ball: ángulo de giro si está hecho una bola (esquiva)."""
    P = []
    if ball is not None:
        b = Part(0, 0, 4.8, 5.4, 5.0, 4.6, 0, 'body', 'body', pitch=-ball); b.stripe = 'ball'; P.append(b)
        for sd in (-1, 1):                                                       # pinzas recogidas delante
            P.append(Part(0, sd * 3.4, 4.8, 1.7, 1.5, 1.4, 0, 'palp', 'palp', pitch=-ball))
        return P
    u0 = 2.4 - .5 * crouch
    P.append(Part(3.6, 0, u0 + .2, 3.2, 3.5, 1.7 + .1 * breath, 0, 'cara', 'body'))           # caparazón
    segs, tip = whip_skeleton(raise_, tremble, crouch)
    for i, ((f, u, phi), (rf, rl, ru)) in enumerate(zip(segs, ((1.5, 3.0, 1.4), (2.0, 4.9, 1.9), (2.0, 4.6, 1.8),
                                                               (1.8, 3.4, 1.5), (1.2, 1.7, 1.0)))):
        p = Part(f + .6 * math.cos(phi), 0, u - .4 * math.sin(phi), rf, rl, ru + .1 * breath, 0, 'body', 'body',
                 pitch=-math.degrees(phi))
        p.stripe = 'seg' if 0 < i < 4 else None; P.append(p)
    for sd in (-1, 1):                                                           # pedipalpos: brazo y pinza
        P.append(Part(6.4 - .4 * claw, sd * 3.0, u0 + .7, 2.4, 1.2, 1.1, sd * 28, 'palp', 'palp'))
        P.append(Part(8.9 - .6 * claw, sd * (3.7 + .5 * claw), u0 + 1.0, 2.3, 1.6, 1.4, -sd * (14 + 20 * claw), 'palp', 'palp'))
    return P

def whip_lines(walk=None, raise_=0.0, tremble=0.0, crouch=0.0, ball=None, claw=0.0, breath=0.0, feel=0.0):
    """Partes finas (1 px, sin contorno): patas, palpos-antena y látigo. [(puntos 3D (f, l, u), colores)]."""
    L = []
    if ball is not None: return L
    u0 = 2.4 - .5 * crouch
    for k, (hf, kf, ff) in enumerate(((4.7, 1.8, 3.0), (3.5, -0.4, -1.4), (2.3, -2.6, -5.2))):   # 3 pares de patas
        for sd in (-1, 1):
            ph = (walk + (k % 2) * .5 + (.5 if sd > 0 else 0)) if walk is not None else 0
            sw = math.sin(ph * 2 * math.pi) if walk is not None else 0
            lift = max(0, math.cos(ph * 2 * math.pi)) * 1.2 if walk is not None else 0
            hip = (hf, sd * 3.0, u0 - .3)
            knee = (hf + kf + .8 * sw, sd * 7.0, 4.4 - .6 * crouch + lift)
            foot = (hf + ff + 1.4 * sw, sd * (9.2 + .5 * crouch), 0.0 + lift * .6)
            L.append(([hip, knee, foot], (WHIP[1], WHIP[2])))
    for sd in (-1, 1):                                                           # palpos largos que hacen de antena
        a = math.sin(feel * 2 * math.pi + (0 if sd < 0 else 1.7)) * .9
        L.append(([(5.6, sd * 2.8, u0), (8.2, sd * 6.4, 4.8 + a * .5), (11.5 + a, sd * 8.8, 3.4 + a), (13.0 + a, sd * 9.6, 2.6 + a)],
                  (WHIP[2], TAN[1])))
    segs, (tf, tu, phi) = whip_skeleton(raise_, tremble, crouch)
    pts = [(tf, 0, tu)]; a = phi + math.radians(15 * (1 - raise_)); f, u = tf, tu   # látigo: sigue el abdomen y se curva
    for k in range(6):
        a += math.radians((14 + 12 * (1 - raise_)) * (1 - 2.2 * raise_)) + math.radians(4 * tremble * (1 if k % 2 else -1))
        f, u = f - 1.9 * math.cos(a), u + 1.9 * math.sin(a)
        pts.append((f, 0.3 * math.sin(k * .9 + breath), max(0.3, u)))
    L.append((pts, (TAN[1], TAN[2])))
    return L

def draw_lines(cv, info, yaw, origin, lines, flat=None, ov=None):
    """Dibuja las partes finas: por detrás del cuerpo si están más lejos que la superficie, si no por encima."""
    for pts, (c0, c1) in lines:
        sp = [screen_of(yaw, origin, *p) for p in pts]
        for j in range(len(sp) - 1):
            (x0, y0, d0), (x1, y1, d1) = sp[j], sp[j + 1]
            seg = line_px(x0, y0, x1, y1)
            for k_, (x, y) in enumerate(seg):
                d = d0 + (d1 - d0) * k_ / max(1, len(seg) - 1)
                hit = info.get((x, y))
                cc = ov or (c0, c1)
                col = flat[2] if flat else (cc[0] if j < len(sp) - 2 else cc[1])
                if (x, y) not in cv.px: cv.put(x, y, C(col))
                elif hit and d < hit[0] - 60 - .8: cv.put(x, y, C(col))

def auto_origin(build, yaw, n, extra=(0, 0)):
    """Origen que centra la silueta de la pose de referencia en un lienzo de n×n."""
    cv = Canvas(80); parts, lines = build()
    tcv, info = trace(parts, 80, yaw, (40, 40), WHIP_PAL)
    draw_lines(tcv, info, yaw, (40, 40), lines)
    xs = [x for x, y in tcv.px]; ys = [y for x, y in tcv.px]
    return (40 - (min(xs) + max(xs) + 1) / 2 + n / 2 + extra[0], 40 - (min(ys) + max(ys) + 1) / 2 + n / 2 + extra[1])

_WHIP_ORIGIN = {}
def whip_origin(view, n):
    if (view, n) not in _WHIP_ORIGIN:
        o = auto_origin(lambda: (whip_parts(), whip_lines()), YAW[view], 32)
        o = (round(o[0] * 2) / 2, round(o[1] * 2) / 2)
        _WHIP_ORIGIN[(view, n)] = (o[0] + (n - 32) / 2, o[1] + (n - 32) / 2)
    return _WHIP_ORIGIN[(view, n)]

WHIP_ELEV = 30                                 # más de perfil: el abdomen alzado se lee como el de un escorpión

def whip_render(view='side', n=32, pal=None, flat=None, eyes='open', mist=None, **pose):
    with camera(WHIP_ELEV):
        return _whip_render(view, n, pal, flat, eyes, mist, **pose)

def _whip_render(view, n, pal, flat, eyes, mist, **pose):
    yaw = YAW[view]; origin = whip_origin(view, n)
    lpose = {k: v for k, v in pose.items() if k != 'feel'}
    parts = whip_parts(**lpose)
    cv, info = trace(parts, n, yaw, origin, pal or WHIP_PAL, flat=flat)
    if not flat:                                                                 # surcos entre los segmentos
        body = [C(c) for c in (pal or WHIP_PAL)['body']]
        for (x, y), (t, p, q, nw) in info.items():
            st = getattr(p, 'stripe', None)
            if not st or cv.px[(x, y)] not in body: continue
            band = q[0] < -.55 if st == 'seg' else ((q[0] * 2.2 + 10) % 1) < .28
            if band and nw[2] > -.2:
                cv.px[(x, y)] = body[max(0, body.index(cv.px[(x, y)]) - 1)]
    cv.outline(); cv.clean()
    draw_lines(cv, info, yaw, origin, whip_lines(**pose), flat, None if pal is None else (pal['body'][1], pal['body'][2]))
    if pose.get('ball') is None:
        for sd in (-1, 1):                                                      # puntas de las pinzas, claras
            claw = pose.get('claw', 0)
            x, y, d = screen_of(yaw, origin, 10.9 - .6 * claw, sd * (3.2 + .5 * claw), 3.6)
            hit = info.get((int(x), int(y)))
            if hit and abs(hit[0] - 60 - d) < 1.6 and not flat: cv.put(x, y, C(TAN[2]))
        # ojos en el borde delantero del caparazón
        for sd in (-1, 1):
            x, y, d = screen_of(yaw, origin, 5.6, sd * 1.1, 3.6)
            hit = info.get((int(x), int(y)))
            if not hit or abs(hit[0] - 60 - d) > 1.6 or yaw > 45: continue
            x, y = int(x), int(y)
            if flat: continue
            if eyes == 'shut': cv.put(x, y, OUTL); cv.put(x + 1, y, OUTL); continue
            cv.put(x, y, EYE); cv.put(x + 1, y, OUTL if yaw == 0 else EYE)
            if yaw != 0: cv.put(x + (1 if sd > 0 else 0), y, OUTL)
    if mist is not None:                                                        # gotitas de vinagre en la punta
        _, (tf, tu, phi) = whip_skeleton(pose.get('raise_', 0), pose.get('tremble', 0), pose.get('crouch', 0))
        for k in range(3):
            r = 1.2 + ((mist + k / 3) % 1) * 3.2
            x, y, d = screen_of(yaw, origin, tf + r * .9, (k - 1) * 1.2, tu + .6 + r * .25)
            cv.put(x, y, C(TAN[3] if k != 1 else TAN[4]))
    cv.info = {'origin': origin, 'yaw': yaw}
    return cv


def whip_tip(view, n=40, **pose):
    """Píxel de la punta del pigidio (de donde sale el chorro) en el lienzo."""
    with camera(WHIP_ELEV):
        _, (tf, tu, phi) = whip_skeleton(pose.get('raise_', 0), pose.get('tremble', 0), pose.get('crouch', 0))
        x, y, d = screen_of(YAW[view], whip_origin(view, n), tf, 0, tu)
    return int(x), int(y)

def whip_anims():
    A = {}
    for v in ('side', 'down', 'up'):
        A[f'Player_Whipscorpion_Idle_{v}'] = [whip_render(v, breath=math.sin(i * math.pi / 2), feel=i / 4) for i in range(4)]
        A[f'Player_Whipscorpion_Move_{v}'] = [whip_render(v, walk=i / 4, feel=i / 4 + .1) for i in range(4)]
        A[f'Player_Whipscorpion_Dodge_{v}'] = [whip_render(v, crouch=1, claw=.6), whip_render(v, ball=0),
                                              whip_render(v, ball=120), whip_render(v, ball=240),
                                              whip_render(v, crouch=.6, claw=.3)]
        A[f'Player_Whipscorpion_AttackStart_{v}'] = [whip_render(v, n=40, raise_=.3, crouch=.3, claw=.4),
                                                    whip_render(v, n=40, raise_=.7, crouch=.4, claw=.7),
                                                    whip_render(v, n=40, raise_=1, crouch=.4, claw=.8)]
        A[f'Player_Whipscorpion_AttackLoop_{v}'] = [whip_render(v, n=40, raise_=1, crouch=.4, claw=.8,
                                                               tremble=(1 if i % 2 else -1), mist=i / 4) for i in range(4)]
        A[f'Player_Whipscorpion_AttackEnd_{v}'] = [whip_render(v, n=40, raise_=.55, crouch=.3, claw=.4),
                                                  whip_render(v, n=40, raise_=.15, crouch=.1)]
    A['Player_Whipscorpion_Hurt'] = [whip_render('side', flat=WHITE4 + [WHITE4[0]], crouch=.3),
                                     whip_render('side', flat=five(RED4), crouch=.2)]
    pale = {'body': five(PALE4), 'palp': five(PALE4), 'tan': five(PALE4)}
    limp = lambda **k: whip_render('side', crouch=1, claw=.2, eyes='shut', **k)
    feet = max(y for (x, y) in limp().px)
    A['Player_Whipscorpion_Death'] = [sink(whip_render('side', flat=WHITE4 + [WHITE4[0]], crouch=.6), 0, rx=0, feet=feet),
                                      sink(limp(), 0, rx=0, feet=feet), sink(limp(pal=pale), 1, rx=14, feet=feet),
                                      sink(limp(pal=pale), 3, rx=15, bubbles=((8, 31, 1.2),), feet=feet),
                                      sink(limp(pal=pale), 5, rx=15, bubbles=((31, 30, 1.5), (12, 32, 1)), feet=feet),
                                      sink(limp(pal=pale), 8, rx=15, ripple=.5, bubbles=((20, 30, 2),), feet=feet),
                                      sink(limp(pal=pale), 30, rx=12, bubbles=((22, 32, 1.2),), feet=feet)]
    return A

WHIP_MS = {'Idle': 170, 'Move': 90, 'Dodge': 60, 'AttackStart': 70, 'AttackLoop': 60, 'AttackEnd': 80,
           'Hurt': 80, 'Death': 120}

# =================================================================================================
# Salida
# =================================================================================================
def ms_of(name, table):
    for k, v in table.items():
        if f'_{k}' in name: return v
    return 100

def fondos(path, anims, keys, scale=3):
    """Mismos cuadros sobre los tres tramos del incendio (prueba de legibilidad)."""
    frames = [as_img(anims[k][0]) for k in keys]
    n = max(f.width for f in frames) + 4
    im = Image.new('RGBA', (n * len(frames), n * len(TRAMOS)), (0, 0, 0, 255))
    for r, col in enumerate(TRAMOS.values()):
        for i, f in enumerate(frames):
            cell = Image.new('RGBA', (n, n), C(col)); cell.alpha_composite(f, ((n - f.width) // 2, (n - f.height) // 2))
            im.paste(cell, (i * n, r * n))
    im.resize((im.width * scale, im.height * scale), Image.NEAREST).save(path)

def write(folder, anims, ms_table, tag):
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    for name, frames in anims.items():
        save_sheet(os.path.join(OUT, folder, name + '.png'), frames)
        gif(os.path.join(OUT, 'gif', name + '.gif'), frames, ms_of(name, ms_table), BG)
    preview(os.path.join(OUT, f'vista_previa_{tag}.png'), anims, BG, scale=3)
    fondos(os.path.join(OUT, f'fondos_{tag}.png'), anims,
           [k for k in anims if '_Idle_' in k] + [k for k in anims if '_Attack_' in k][:1])

if __name__ == '__main__':
    which = sys.argv[2:] or ['frog']
    if 'frog' in which: write('Frog', frog_anims(), FROG_MS, 'rana')
    if 'diplo' in which: write('Diplocaulus', diplo_anims(), DIPLO_MS, 'diplocaulus')
    if 'whip' in which: write('Whipscorpion', whip_anims(), WHIP_MS, 'vinagrillo')
    print('Listo:', OUT)
