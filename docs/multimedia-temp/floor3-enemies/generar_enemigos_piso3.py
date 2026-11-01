"""Enemigos y trampas del piso 3 de Primordia (pantano carbonífero).

Subtanda 3a, plantas:
- Calamites (madre, inmóvil): mata de cañas segmentadas en 32×48 con un rizoma bulboso y ojos enfadados; suelta
  retoños (CalamitesSprout, 16×24) y anillos de esporas desde los estróbilos de las puntas.
- Medullosa: helecho con semillas en 32×40; tronco grueso, corona de frondas colgantes y semillas pardo rojizo;
  hace de mortero: dobla una fronda hacia atrás y lanza una vaina que revienta en un charco de veneno.
- CarnivorousPlant (trampa, no muere), 40×40: al estilo de los dibujos (cabeza carmín con manchas, colmillos y lengua
  sobre un tallo curvo, hojas y zarcillos en la base); atrapa 1 s y se come enemigos; al limpiar la sala se marchita.
Paletas de references/pisos.md (piso 3). Rampas de 5 tonos con luz rebotada (cuerpos de ~32 px).
Hecho con la skill primordia-sprites (pixel.py): 16 px por unidad, luz arriba a la izquierda, contorno OUTL.

Subtanda 3b, bichos de suelo:
- Beetle (escarabajo bombardero), 32×32 de lado con la cámara 3D del vinagrillo (30°): élitros azul noche, cabeza naranja; se da la vuelta
  (Turn, luego Flip X) para apuntar el abdomen y suelta un cono ardiente de 3 u por detrás (Fx_BeetleSpray).
- Spider (araña de las frondas), 32×32 de lado con la misma cámara: camuflaje de helecho, ojos rojos. Escondida es solo una capa
  (Spider_Hidden: sombra y ojos) que va sobre un helecho del escenario que hace Fern_*_Rustle. Telarañas que arden.
- Toad (sapo de fango), 32×32 de perfil: rechoncho y verrugoso, escupe lodo, se hincha y revienta, nada en el lodo.

Subtanda 3c, voladores:
- Dragonfly (libélula gigante), 40×40 de lado con la cámara 3D: cuerpo turquesa, abdomen largo, 4 alas translúcidas;
  vuela sobre todo, fija una línea 0,7 s (Warn), hace un picado (Dive) y se posa 0,8 s (Perch).
- Mayflies (enjambre de efímeras, trampa), nube de una pieza de 40×40 con 12 efímeras; solo muere con fuego (Burn).

Uso: python generar_enemigos_piso3.py [carpeta_salida] [plants] [ground] [flyers]   (por defecto, la carpeta de este script)
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

OUT = sys.argv[1] if len(sys.argv) > 1 and not sys.argv[1].isalpha() else os.path.dirname(os.path.abspath(__file__))
BG = C('#1c2e20')                          # fondo de revisión del piso 3 (musgo oscuro)
TRAMOS = {'Swamp': '#273f2b', 'Smoke': '#30332a', 'Burnt': '#2a2528'}

# --- paletas ---------------------------------------------------------------------------------------
VENOM = ['#2e5a14', '#5aa020', '#a8e040', '#e8ffc0']        # veneno (reacciones)
DRY = ['#3a2e16', '#5e4a20', '#8a6e2a', '#b8963a', '#e0c870']  # helecho reseco
SOIL = ['#22160f', '#33221a', '#4a3324', '#634830']         # tierra removida
MOUTH = ['#3a1020', '#6a2034', '#9a3044', '#d0606a']        # interior de la boca de la carnívora
CAL = ramp('#6a9a3a', 5); CAL_B = ramp('#d8d090', 4)        # Calamites: caña verde, nudos pálidos
MED = ramp('#3a6a3a', 5); MED_S = ramp('#a8502a', 5)        # Medullosa: frondas oscuras, semillas pardo rojizo
BARK = ['#1e2420', '#2e3830', '#435040', '#5e6c52', '#8a9670']  # corteza (tronco de la Medullosa)
TRAP = ['#1c3a20', '#2a5a2a', '#3e8a34', '#6abf48', '#b8e878']  # helecho vivo (lóbulos de la carnívora)
LURE = ['#6a2e0e', '#b0581a', '#e89a30', '#ffcc5a', '#fff0b0']  # ámbar (semilla cargada de la Medullosa)
HEAD = ['#3a0a16', '#6e1424', '#a01e2e', '#cc3a3e', '#ee6e62']  # carnívora: cabeza rojo carmín
SPOT_P = ['#d8505e', '#f4949a']                             # manchas rosadas de la cabeza
LIP = ['#7a1a2a', '#d05a6a', '#f08a98']                     # labios
TONGUE = ['#7a2040', '#c04a6a', '#f07a98', '#ffc0d0']       # lengua
SPORE = ['#8a8a3a', '#c8c870', '#ecec9a', '#fffbd8']         # esporas de la Calamites

def five(r4): return [r4[0]] + list(r4)
WHITE5, RED5, PALE5 = five(WHITE4), five(RED4), five(PALE4)

FLAT = None                                 # Hurt: todo el dibujo de un solo color (silueta blanca o roja)
def P(r):
    return [C(c) if isinstance(c, str) else c for c in (FLAT if FLAT else r)]

# --- formas con rampa de 5 ------------------------------------------------------------------------
def fill5(cv, inside, cx, cy, rx, ry, r, shift=0, box=None, alpha=255):
    r = [C(c, alpha) if isinstance(c, str) else c for c in P(r)]
    x0, y0, x1, y1 = box or (0, 0, cv.w, cv.h)
    for y in range(max(0, int(y0)), min(cv.h, int(y1) + 1)):
        for x in range(max(0, int(x0)), min(cv.w, int(x1) + 1)):
            px, py = x + .5, y + .5
            if inside(px, py):
                i = shade((px - cx) / rx, (py - cy) / ry, steps=len(r), reflect=True)
                cv.px[(x, y)] = r[max(0, min(len(r) - 1, i + shift))]

def ell5(cv, cx, cy, rx, ry, r, shift=0):
    fill5(cv, lambda x, y: ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1, cx, cy, rx, ry, r, shift,
          box=(cx - rx - 1, cy - ry - 1, cx + rx + 1, cy + ry + 1))

def rell5(cv, cx, cy, rx, ry, ang, r, shift=0):
    ca, sa = math.cos(ang), math.sin(ang)
    def inside(x, y):
        dx, dy = x - cx, y - cy
        return ((dx * ca + dy * sa) / rx) ** 2 + ((-dx * sa + dy * ca) / ry) ** 2 <= 1
    m = max(rx, ry)
    fill5(cv, inside, cx, cy, m, m, r, shift, box=(cx - m - 1, cy - m - 1, cx + m + 1, cy + m + 1))

def eye(cv, x, y, look=(0, 1), state='angry', side='L'):
    """Ojo de la familia de 3×3 con pupila de 2×2. 'angry': ceja que baja hacia dentro (side = qué ojo es)."""
    if FLAT: return
    x, y = int(x), int(y)
    if state == 'x':
        for k in range(3): cv.put(x + k, y + k, OUTL); cv.put(x + 2 - k, y + k, OUTL)
        return
    if state == 'shut':
        for k in range(3): cv.put(x + k, y + 1 + (k == 1), OUTL)
        return
    for dx in range(3):
        for dy in range(3): cv.put(x + dx, y + dy, EYE)
    px = x + (1 if look[0] > 0 else 0 if look[0] < 0 else (0 if side == 'R' else 1))
    py = y + (1 if look[1] >= 0 else 0)
    for dx in range(2):
        for dy in range(2): cv.put(px + dx, py + dy, OUTL)
    if state == 'angry':
        if side == 'L': cv.put(x - 1, y - 1, OUTL); cv.put(x, y - 1, OUTL); cv.put(x + 1, y, OUTL); cv.put(x + 2, y, OUTL)
        else: cv.put(x + 3, y - 1, OUTL); cv.put(x + 2, y - 1, OUTL); cv.put(x + 1, y, OUTL); cv.put(x, y, OUTL)

def eyes(cv, cx, y, gap=2, state='angry', look=(0, 1)):
    eye(cv, cx - gap // 2 - 3, y, look, state, 'L'); eye(cv, cx + (gap + 1) // 2, y, look, state, 'R')

def lerp(a, b, t): return a + (b - a) * t
def mix_ramp(a, b, t):
    """Mezcla dos rampas de 5 (para secarse poco a poco)."""
    a, b = [C(c) for c in a], [C(c) for c in b]
    return [tuple(int(round(lerp(p[k], q[k], t))) for k in range(3)) + (255,) for p, q in zip(a, b)]

def specks(cv, pts, col):
    for (x, y) in pts: cv.put_out(x, y, C(col) if isinstance(col, str) else col)

# =================================================================================================
# Calamites (madre) y retoños
# =================================================================================================
def cane(cv, x0, yb, h, bend, r, band, node=5, ph=0, w=3):
    """Caña segmentada de abajo arriba; bend = desplazamiento de la punta (curva cuadrática).
    Devuelve los nudos [(x, y, t)] y la punta (x, y). Cilindro: luz a la izquierda, sombra a la derecha."""
    r, band = P(r), P(band)
    nodes = []
    for k in range(int(h)):
        t = k / max(1, h - 1)
        x = x0 + bend * t * t; y = yb - k
        is_node = (k + ph) % node == 0 and k > 1
        for dx in range(w):
            c = (band[2] if dx == 0 else band[1] if dx == w - 1 else band[2]) if is_node else \
                (r[3] if dx == 0 else r[1] if dx == w - 1 else r[2])
            cv.put(x - w // 2 + dx, y, c)
        if is_node: nodes.append((x, y, t))
    return nodes, (x0 + bend, yb - h + 1)

def whorl(cv, nodes, r, sway=0.0, droop=0.0, length=5):
    """Verticilos de hojas de aguja en los nudos: finas, sin contorno, por detrás de la caña."""
    r = P(r)
    for (x, y, t) in nodes:
        if t < 0.2: continue
        L = max(2, round(length * (1.15 - t * 0.5)))
        for s in (-1, 1):
            for j in range(L):
                lift = (0.55 - droop) * j + sway * s * j * 0.2
                cv.put_out(x + s * (2 + j), y - lift, r[2] if j < L - 1 else r[3])

def calamites(sway=0.0, swell=0.0, squeeze=0.0, glow=0.0, droop=0.0, dry=0.0, shrink=0.0, eye_state='angry',
              n=(32, 48), off=(0, 0), sprout_side=None):
    """Calamites madre mirando a cámara. sway -1..1 mece las cañas; swell hincha el rizoma (retoño);
    squeeze encoge las cañas antes de soltar esporas; glow 0..1 enciende los estróbilos; droop las dobla
    hacia fuera al morir; dry seca los colores (0 verde, 1 reseco, 2 pálido); shrink hunde el conjunto."""
    cv = Canvas(*n)
    ox, oy = off
    body = CAL if dry <= 0 else mix_ramp(CAL, DRY, min(1, dry)) if dry <= 1 else mix_ramp(DRY, PALE5, dry - 1)
    band = five(CAL_B) if dry <= 0 else mix_ramp(five(CAL_B), DRY, min(1, dry)) if dry <= 1 else PALE5
    cx, base = 16 + ox, 41 + oy + round(shrink * 2)
    hs = 1 - squeeze * 0.12 - shrink * 0.55
    # cañas: (x de la base, altura, curva, desfase de nudos, inclinación propia al morir)
    canes = [(cx - 5, 26, -3, 2, -1), (cx + 5, 29, 3, 0, 1), (cx - 1, 33, 0.5, 3, -0.3), (cx + 2, 22, 1.5, 1, 0.6)]
    tips = []
    for (x, h, b, ph, d) in canes:
        bend = b + sway * (1.2 + h / 20) + min(droop, 1.25) * d * 7.5
        nodes, tip = cane(cv, x, base - 4, round(h * hs), bend, body, band, ph=ph)
        tips.append((tip, nodes))
    # estróbilos (conos) en las puntas: pálidos; con glow se encienden
    for (tx, ty), _ in tips:
        cone = five(CAL_B) if glow <= 0 else mix_ramp(five(CAL_B), five(SPORE), glow)
        if dry > 0: cone = band
        ell5(cv, tx, ty - 1, 1.8, 2.8, cone)
    # rizoma bulboso
    rx, ry = 9.5 + swell * 1.6 - shrink * 3, 5.5 + swell * 1.4 - shrink * 2
    ell5(cv, cx, base - 1 - swell * 0.6, rx, ry, body, shift=-1)
    if swell > 0.5 and not FLAT:                         # grieta del rizoma por donde sale el retoño
        sx = cx + (6 if sprout_side == 'R' else -7)
        for k in range(3): cv.put_in(sx + (k if sprout_side == 'R' else -k) * 0.5, base - 3 + k, OUTL)
    cv.outline(); cv.clean()
    for (tx, ty), nodes in tips:
        whorl(cv, nodes, body, sway=sway, droop=droop)
    if shrink < 0.6: eyes(cv, cx, base - 3 - round(swell), gap=3, state=eye_state)
    # raicillas y tierra a los lados
    if not FLAT:
        for s in (-1, 1):
            for j in range(3): cv.put_out(cx + s * (rx + j), base + 2 + j // 2, P(body)[0])
    return cv

def sprout(h=1.0, sway=0.0, hop=0.0, squash=0.0, dry=0.0, eye_state='angry', n=(16, 24), off=(0, 0)):
    """Retoño de Calamites: una caña corta con un bulbo y un verticilo. h 0..1 = cuánto ha brotado."""
    cv = Canvas(*n)
    ox, oy = off
    body = CAL if dry <= 0 else mix_ramp(CAL, DRY, min(1, dry)) if dry <= 1 else mix_ramp(DRY, PALE5, dry - 1)
    band = five(CAL_B) if dry <= 0 else PALE5 if dry > 1 else mix_ramp(five(CAL_B), DRY, dry)
    cx, base = 8 + ox, 21 + oy - round(hop * 3)
    if h > 0.25:
        nodes, (tx, ty) = cane(cv, cx, base - 3, round(12 * h), sway * 1.5 + dry * 3, body, band, node=4, ph=1)
        ell5(cv, tx, ty - 1, 1.4, 2.0, band)
    else: nodes = []
    ell5(cv, cx, base - 1, 4.2 * (1 + squash) * max(0.5, min(1, h * 2)), 3.2 * (1 - squash) * max(0.5, min(1, h * 2)),
         body, shift=-1)
    cv.outline(); cv.clean()
    whorl(cv, nodes, body, sway=sway, droop=dry * 0.5, length=3)
    if h >= 0.6 and not FLAT:                              # ojitos de 2×2 con ceja
        for s, ex in (('L', cx - 3), ('R', cx + 1)):
            ey = base - 2
            if eye_state == 'x':
                cv.put(ex, ey, OUTL); cv.put(ex + 1, ey + 1, OUTL); continue
            for dx in range(2):
                for dy in range(2): cv.put(ex + dx, ey + dy, EYE)
            cv.put(ex + (1 if s == 'L' else 0), ey + 1, OUTL)
            if eye_state == 'angry': cv.put(ex + (1 if s == 'L' else 0), ey, OUTL)
    return cv

def dirt(cv, cx, y, k):
    """Terrones que saltan al brotar."""
    if FLAT: return
    for i, (dx, dy) in enumerate(((-4, -1), (4, -2), (-2, -3), (3, 0), (-5, 1))):
        if i < k: cv.put_out(cx + dx * (1 + k * 0.2), y + dy - k, C(SOIL[2 + i % 2]))

def spore_puff(cv, tips, k):
    """Nubecilla de esporas en las puntas (el anillo de verdad lo lanza el código)."""
    if FLAT: return
    for (tx, ty) in tips:
        for a in range(6):
            ang = a * math.pi / 3 + k * 0.4
            d = 2 + k * 1.6
            col = SPORE[3 - min(3, k)] if k < 3 else SPORE[0]
            y = ty - 2 + math.sin(ang) * d
            if y >= 1: cv.put_out(tx + math.cos(ang) * d, y, C(col))

CAL_TIPS = [(16 - 5 - 3, 41 - 4 - 26), (16 + 5 + 3, 41 - 4 - 29), (16 - 1, 41 - 4 - 33), (16 + 3, 41 - 4 - 22)]

def calamites_anims():
    global FLAT
    sw = [math.sin(i / 4 * 2 * math.pi) for i in range(4)]
    a = {}
    a['Calamites_Idle'] = [calamites(sway=s) for s in sw]
    a['Calamites_Spawn'] = [calamites(swell=v, sprout_side=sd) for v, sd in
                            ((0.3, 'L'), (0.7, 'L'), (1.0, 'L'), (0.2, 'L'), (0.0, 'L'))]
    for i, k in ((3, 1), (4, 3)): dirt(a['Calamites_Spawn'][i], 8, 39, k)
    sp = []
    for (sq, gl, k) in ((0.6, 0.3, None), (1.0, 0.8, None), (0.0, 1.0, 0), (-0.2, 0.5, 1), (0.0, 0.0, 2)):
        f = calamites(squeeze=sq, glow=gl)
        if k is not None: spore_puff(f, CAL_TIPS, k)
        sp.append(f)
    a['Calamites_Spores'] = sp
    FLAT = WHITE5; w = calamites(); FLAT = RED5; r = calamites(); FLAT = None
    a['Calamites_Hurt'] = [w, r]
    d = []
    FLAT = WHITE5; d.append(calamites(n=(48, 56), off=(8, 8))); FLAT = None
    for (dr, dy, sh, es) in ((0.15, 0.2, 0, 'x'), (0.45, 0.6, 0.05, 'x'), (0.8, 1.0, 0.2, 'x'),
                             (1.2, 1.4, 0.45, 'x'), (1.7, 2.0, 0.7, 'x'), (2.2, 2.0, 0.9, 'x')):
        d.append(calamites(droop=dr, dry=dy, shrink=sh, eye_state=es, n=(48, 56), off=(8, 8)))
    a['Calamites_Death'] = d
    # retoños
    a['CalamitesSprout_Emerge'] = [sprout(h=v) for v in (0.15, 0.4, 0.75, 1.0)]
    for i, k in enumerate((1, 2, 3, 0)):
        if k: dirt(a['CalamitesSprout_Emerge'][i], 8, 19, k)
    a['CalamitesSprout_Idle'] = [sprout(sway=s) for s in sw]
    a['CalamitesSprout_Move'] = [sprout(hop=hp, squash=sq, sway=s) for hp, sq, s in
                                 ((0, 0.25, 0.3), (0.6, -0.15, -0.2), (1.0, -0.1, -0.5), (0.4, 0.0, 0.0))]
    a['CalamitesSprout_Wither'] = [sprout(dry=v, eye_state='x' if v > 0.3 else 'angry') for v in (0.3, 0.8, 1.4, 2.0)]
    FLAT = WHITE5; w = sprout(); FLAT = RED5; r = sprout(); FLAT = None
    a['CalamitesSprout_Hurt'] = [w, r]
    a['CalamitesSprout_Death'] = [sprout(dry=0.6, eye_state='x'), sprout(h=0.6, dry=1.2, eye_state='x'),
                                  sprout(h=0.3, dry=1.8), sprout(h=0.15, dry=2.0)]
    return a

def spore_proj():
    out = []
    for k in range(2):
        cv = Canvas(8)
        ell5(cv, 3.5, 3.5, 2.4 + k * 0.3, 2.4 + k * 0.3, five(SPORE))
        cv.outline(C('#5a5a20'))
        cv.put(3, 2, SPARK[1])
        out.append(cv)
    return out

def spore_pop():
    out = []
    for k in range(4):
        cv = Canvas(12)
        for a in range(6):
            ang = a * math.pi / 3 + 0.3
            d = 1.5 + k * 1.3
            cv.put(6 + math.cos(ang) * d, 6 + math.sin(ang) * d, C(SPORE[3 - k]))
        if k == 0: ell5(cv, 6, 6, 2, 2, five(SPORE))
        out.append(cv)
    return out

# =================================================================================================
# Medullosa
# =================================================================================================
def frond_path(root, ang, L, curl, sway=0.0):
    x, y = root; a = ang
    pts = []
    for k in range(int(L)):
        pts.append((x, y, a, k / L))
        x += math.cos(a); y += math.sin(a)
        a += curl / L + sway * 0.02
    return pts

def frond(cv, root, ang, L, curl, r, sway=0.0, seed=None, seed_r=None, back=False):
    """Fronda pinnada que se arquea: raquis de 2 px desde root con ángulo inicial ang (rad, 0 = derecha,
    pi/2 = abajo); curl la dobla. Las pinnas son trazos finos que cuelgan a los dos lados (sin contorno, se
    dibujan con put_out después del contorno; ver frond_pinnae). seed = semilla colgando de la punta."""
    r = P(r)
    pts = frond_path(root, ang, L, curl, sway)
    for (px, py, pa, t) in pts:                         # raquis
        cv.put(px, py, r[1] if back else r[2]); cv.put(px, py + 1, r[0] if back else r[1])
    tip = pts[-1][:2]
    if seed is not None:
        sx, sy = tip[0] + math.cos(pts[-1][2]) * 1.5, tip[1] + 2.5
        ell5(cv, sx, sy, 2.2 * seed, 2.9 * seed, seed_r or MED_S)
        return (sx, sy)
    return tip

def frond_pinnae(cv, root, ang, L, curl, r, sway=0.0, back=False, ph=0):
    """Pinnas de una fronda: pares de trazos de 2-4 px que cuelgan del raquis (claros arriba, en sombra abajo)."""
    r = P(r)
    pts = frond_path(root, ang, L, curl, sway)
    for (px, py, pa, t) in pts[1 + ph % 2::2]:
        ln = round(4 - t * 2.2)
        for s in (-1, 1):
            la = pa + s * 1.2
            hang = 0.45                                  # las pinnas caen un poco por su peso
            for j in range(1, ln + 1):
                x = px + math.cos(la) * j; y = py + math.sin(la) * j + hang * j
                c = (r[1] if back else r[3] if s < 0 and j < ln else r[2]) if j < ln else (r[0] if back else r[4 if s < 0 else 3])
                cv.put_out(x, y, c)

def medullosa(sway=0.0, cock=0.0, fire=0.0, glow=0.0, droop=0.0, dry=0.0, eye_state='angry', n=(32, 40),
              off=(0, 0), seed_on=True):
    """Medullosa mirando a cámara. cock 0..1 dobla hacia atrás la fronda-catapulta (la de la derecha arriba);
    fire 0..1 la suelta hacia delante; glow enciende la semilla cargada; droop/dry al morir."""
    cv = Canvas(*n)
    ox, oy = off
    fr = MED if dry <= 0 else mix_ramp(MED, DRY, min(1, dry)) if dry <= 1 else mix_ramp(DRY, PALE5, dry - 1)
    bk = BARK if dry <= 1 else mix_ramp(BARK, PALE5, dry - 1)
    sd = MED_S if dry <= 0 else mix_ramp(MED_S, DRY, min(1, dry))
    cx, base = 16 + ox, 37 + oy
    top = base - 14 + round(droop * 3)
    dsign = lambda cu: (1 if cu > 0 else -1) * droop * 0.9
    # fronda-catapulta (arriba a la derecha): se echa atrás al cargar y salta adelante al disparar
    ca = -1.15 - cock * 0.9 + fire * 1.2
    cpl = 0.6 + cock * 1.3 - fire * 1.2
    loaded = seed_on and fire < 0.5
    fronds_back = [(-1.95, 13, -0.9), (-1.2, 13, 0.9)]
    fronds_front = [(-2.6, 13, -1.5, True), (-0.55, 13, 1.5, True), (-2.15, 11, -1.1, False)]
    for (ang, L, cu) in fronds_back:
        frond(cv, (cx, top - 2), ang + sway * 0.08, L, cu + dsign(cu), fr, sway, back=True)
    # tronco grueso que se ensancha abajo, con cicatrices de hojas en rombo
    fill5(cv, lambda x, y: abs(x - cx) <= 3.2 + max(0, y - base + 4) * 0.7 and top < y <= base,
          cx, (top + base) / 2, 4.5, 8, bk)
    if not FLAT:
        for k, yy in enumerate(range(top + 3, base - 2, 3)):
            xx = cx - 1 + (k % 2) * 2
            cv.put_in(xx, yy, P(bk)[1]); cv.put_in(xx, yy + 1, P(bk)[0]); cv.put_in(xx - 1, yy, P(bk)[3])
    for (ang, L, cu, has) in fronds_front:
        frond(cv, (cx, top - 1), ang + sway * 0.1, L, cu + dsign(cu), fr, sway,
              seed=0.95 if has and seed_on else None, seed_r=sd)
    tip = frond(cv, (cx, top - 1), ca, 12 - cock * 2, cpl, fr, seed=1.2 if loaded else None,
                seed_r=mix_ramp(sd, LURE, glow) if glow > 0 else sd)
    ell5(cv, cx, top, 5, 3.4, fr, shift=0)                # nudo de la corona
    cv.outline(); cv.clean()
    for (ang, L, cu) in fronds_back:
        frond_pinnae(cv, (cx, top - 2), ang + sway * 0.08, L, cu + dsign(cu), fr, sway, back=True)
    for i, (ang, L, cu, has) in enumerate(fronds_front):
        frond_pinnae(cv, (cx, top - 1), ang + sway * 0.1, L, cu + dsign(cu), fr, sway, ph=i)
    frond_pinnae(cv, (cx, top - 1), ca, 12 - cock * 2, cpl, fr)
    eyes(cv, cx, top - 1, gap=1, state=eye_state)
    if not FLAT:                                         # raíces
        for s in (-1, 1):
            for j in range(3): cv.put_out(cx + s * (6 + j), base - 1 + (j + 1) // 2, P(bk)[1])
    return cv, tip

def seed_proj(i):
    """Vaina de semilla dando vueltas (12×12), sin dirección."""
    cv = Canvas(12)
    ang = i * math.pi / 4
    rell5(cv, 6, 6, 3.6, 2.7, ang, MED_S)
    cv.outline(); cv.clean()
    if not FLAT:
        # costura del cascabillo y brillo verde del veneno de dentro
        for k in (-1, 0, 1):
            cv.put_in(6 + math.cos(ang) * k * 1.5, 6 + math.sin(ang) * k * 1.5, C(VENOM[1]))
    return cv

def seed_target(i):
    """Aviso en el suelo donde caerá la vaina (24×24): anillo que se cierra y parpadea."""
    cv = Canvas(24)
    r = 10 - i * 2
    col = C('#e04a3a', 200 if i % 2 == 0 else 140)
    for a in range(0, 360, 4):
        x, y = 12 + math.cos(math.radians(a)) * r, 12 + math.sin(math.radians(a)) * r * 0.55
        cv.put(x, y, col)
    cv.put(12, 12, col); cv.put(11, 12, col); cv.put(12, 11, col)
    return cv

def seed_burst(i):
    """La vaina cae, se raja y salpica veneno (24×24); el charco lo pone el código (Fx_PoisonPuddleSmall)."""
    cv = Canvas(24)
    if i == 0:
        rell5(cv, 12, 12, 4, 3, 0.2, MED_S); cv.outline()
    elif i == 1:
        for s in (-1, 1): rell5(cv, 12 + s * 2.5, 13, 2.5, 2.2, s * 0.6, MED_S)
        ell5(cv, 12, 12, 2, 2, five(VENOM)); cv.outline()
    else:
        k = i - 2
        for a in range(8):
            ang = a * math.pi / 4 + 0.2
            d = 3 + k * 2.6
            x, y = 12 + math.cos(ang) * d, 12 + math.sin(ang) * d * 0.6 - (2 - k) * (a % 2)
            cv.put(x, y, C(VENOM[3 - k] if k < 3 else VENOM[1])); cv.put(x + 1, y, C(VENOM[2 - min(2, k)]))
        for s in (-1, 1):                                  # cáscaras
            cv.put(12 + s * (4 + k), 13 + k // 2, C(MED_S[1])); cv.put(12 + s * (5 + k), 13 + k // 2, C(MED_S[2]))
    return cv

def medullosa_anims():
    global FLAT
    sw = [math.sin(i / 4 * 2 * math.pi) for i in range(4)]
    a = {}
    a['Medullosa_Idle'] = [medullosa(sway=s)[0] for s in sw]
    a['Medullosa_Aim'] = [medullosa(cock=c, glow=g)[0] for c, g in ((0.3, 0), (0.6, 0.3), (0.9, 0.6), (1.0, 1.0))]
    a['Medullosa_Shoot'] = [medullosa(cock=c, fire=f, glow=g)[0] for c, f, g in
                            ((1.0, 0.6, 0), (0.6, 1.0, 0), (0.2, 0.6, 0), (0.0, 0.1, 0))]
    FLAT = WHITE5; w = medullosa()[0]; FLAT = RED5; r = medullosa()[0]; FLAT = None
    a['Medullosa_Hurt'] = [w, r]
    d = []
    FLAT = WHITE5; d.append(medullosa(n=(40, 48), off=(4, 6))[0]); FLAT = None
    for (dr, dy, es) in ((0.3, 0.2, 'x'), (0.8, 0.7, 'x'), (1.4, 1.1, 'x'), (1.9, 1.6, 'x'), (2.4, 2.0, 'x')):
        d.append(medullosa(droop=dr, dry=dy, eye_state=es, n=(40, 48), off=(4, 6), seed_on=dr < 1)[0])
    a['Medullosa_Death'] = d
    return a

# =================================================================================================
# Planta carnívora (trampa)
# =================================================================================================
def bezier(p0, p1, p2, t):
    u = 1 - t
    return (u * u * p0[0] + 2 * u * t * p1[0] + t * t * p2[0], u * u * p0[1] + 2 * u * t * p1[1] + t * t * p2[1])

def tendril(cv, x, y, s, size, r, ph=0.0):
    """Zarcillo enroscado: sube desde (x, y) y termina en espiral hacia el lado s (sin contorno)."""
    r = P(r)
    up = size * 2.2
    pts = []
    for k in range(12):                                   # tramo recto que sube ondulando
        t = k / 11
        pts.append((x + s * math.sin(t * 3 + ph) * 0.8, y - t * up))
    cx0, cy0 = x + s * 2.6, y - up                        # espiral: centro a un lado de la punta
    for k in range(18):
        u = k / 17
        ang = math.pi + u * 4.4
        rad = 2.6 * (1 - u * 0.7)
        pts.append((cx0 + s * math.cos(ang) * rad, cy0 - math.sin(ang) * rad * -1))
    seen = set()
    for i, (px, py) in enumerate(pts):
        q = (round(px), round(py))
        if q in seen: continue
        seen.add(q)
        cv.put_out(q[0], q[1], r[3] if i % 3 else r[2])

def carnivore(open_=1.0, lunge=0.0, bulge=0.0, wilt=0.0, sway=0.0, tongue=0.0, bob=0.0, swallow=None):
    """Planta carnívora al estilo de las de los dibujos: cabeza carmín grande con manchas sobre un tallo curvo,
    mandíbula que se abre hacia la derecha con colmillos y lengua, cresta de hojas, hojas grandes y zarcillos en
    la base. Mira a la derecha (Flip X para la izquierda). open_ 0..1 abre la boca; lunge 0..1 lanza la cabeza
    adelante al morder; bulge hincha las mejillas al masticar; tongue mueve la lengua; wilt la marchita."""
    cv = Canvas(40)
    head_r = HEAD if wilt <= 0 else mix_ramp(HEAD, DRY, min(1, wilt * 0.8))
    leaf = TRAP if wilt <= 0 else mix_ramp(TRAP, DRY, min(1, wilt))
    bx, by = 17, 33                                        # pie del tallo
    hx = 21 + lunge * 6 + sway * 0.8 - wilt * 6
    hy = 16 + lunge * 2 + bob + wilt * 10
    # hojas grandes de la base (detrás del tallo)
    for (dx, dy, rx, ang, sh) in ((-8, 1, 7, 0.35, -1), (9, 1, 7, -0.35, -1), (-4, 1.5, 5.5, 0.6, 0), (5, 1.5, 5.5, -0.6, 0)):
        rell5(cv, bx + dx, by + dy - wilt, rx * (1 - wilt * 0.1), 2.6, ang + wilt * 0.3 * (1 if dx < 0 else -1), leaf, shift=sh)
    # tallo curvo y grueso
    neck = (hx - 6, hy + 5)
    ctrl = (bx - 7 - lunge * 2 + sway, by - 10 + wilt * 4)
    for k in range(30):
        x, y = bezier((bx, by), ctrl, neck, k / 29)
        w = 2.2 - k / 29 * 0.5
        rell5(cv, x, y, w, w, 0, leaf)
    # cresta de hojas en la nuca
    for (ox, oy, ang) in ((-6, -5, -2.2), (-3, -7.5, -1.9)):
        rell5(cv, hx + ox * (1 - wilt * 0.3), hy + oy, 3.4, 1.4, ang + wilt * 0.6, leaf)
    # cabeza de dos mandíbulas articuladas en la bisagra H (marco local sin inclinar; mira a la derecha)
    tilt = wilt * 0.9 + lunge * 0.12                       # al marchitarse la cabeza cae hacia el suelo
    ct, st = math.cos(tilt), math.sin(tilt)
    Hx, Hy = hx - 6, hy + 1
    au = -(0.04 + 0.52 * open_)                            # mandíbula de arriba: sube
    al = 0.02 + 0.5 * open_                               # mandíbula de abajo: baja (menos)
    bul = 1 + bulge * 0.1
    def local(x, y):
        dx, dy = x - Hx, y - Hy
        return dx * ct + dy * st, -dx * st + dy * ct
    def world(u, v):
        return Hx + u * ct - v * st, Hy + u * st + v * ct
    def jaw(u, v, ang):                                    # coordenadas en el marco de una mandíbula
        ca, sa = math.cos(ang), math.sin(ang)
        return u * ca + v * sa, -u * sa + v * ca
    def in_upper(x, y):
        ju, jv = jaw(*local(x, y), au)
        return jv <= 0.5 and ((ju - 6.5) / 10) ** 2 + ((jv + 1.2) / (7.6 * bul)) ** 2 <= 1
    def in_lower(x, y):
        ju, jv = jaw(*local(x, y), al)
        return jv >= -0.5 and ((ju - 5.5) / 8.5) ** 2 + ((jv - 0.4) / (4.6 * bul)) ** 2 <= 1
    def in_mouth(x, y):
        u, v = local(x, y)
        _, vu = jaw(u, v, au); _, vl = jaw(u, v, al)
        return vu > 0 and vl < 0 and 0 < u and u * u + v * v < 13.5 ** 2
    box = (hx - 16, hy - 16, hx + 16, hy + 16)
    if open_ > 0.08:
        fill5(cv, in_mouth, Hx + 6, Hy, 9, 6, [MOUTH[0]] * 2 + MOUTH[:3], box=box)
    fill5(cv, in_lower, Hx + 5, Hy + 3, 8.5, 5, head_r, shift=-1, box=box)
    fill5(cv, in_upper, Hx + 5, Hy - 3, 10.5, 8, head_r, box=box)
    cv.outline(); cv.clean()
    if FLAT: return cv
    def jw(ju, jv, ang):                                   # de marco de mandíbula a mundo
        ca, sa = math.cos(ang), math.sin(ang)
        return world(ju * ca - jv * sa, ju * sa + jv * ca)
    # manchas rosadas en la mandíbula de arriba
    for (ju, jv, big) in ((1, -5, 1), (5, -6.5, 1), (2.5, -2.2, 0), (8.5, -4.5, 0), (-1.5, -1.5, 0)):
        x, y = jw(ju, jv, au)
        cv.put_in(x, y, C(SPOT_P[1])); cv.put_in(x + 1, y, C(SPOT_P[0]))
        if big: cv.put_in(x, y + 1, C(SPOT_P[0])); cv.put_in(x + 1, y + 1, C(SPOT_P[0])); cv.put_in(x - 1, y, C(SPOT_P[0]))
    if open_ > 0.08:
        # labios: borde interior claro de cada mandíbula
        for k in range(1, 17):
            x, y = jw(k, 0, au)
            if in_upper(x + .5, y + .5): cv.put_in(x, y, C(LIP[2]))
            x, y = jw(k, 0, al)
            if in_lower(x + .5, y + .5): cv.put_in(x, y, C(LIP[1]))
        # colmillos: triángulos de 3 px de base que apuntan al centro de la boca
        for (k, ln) in ((4, 2), (7.5, 3), (11, 3), (14, 2)):
            for j in range(1, ln + 1):
                w = 1 if j < ln else 0
                for d in range(-w, w + 1):
                    x, y = jw(k + d * 0.9, j, au)
                    if in_mouth(x + .5, y + .5): cv.put(x, y, C(EYE) if d <= 0 else C(PALE4[2]))
        for (k, ln) in ((5.5, 2), (9, 2), (12, 2)):
            for j in range(1, ln + 1):
                w = 1 if j < ln else 0
                for d in range(-w, w + 1):
                    x, y = jw(k + d * 0.9, -j, al)
                    if in_mouth(x + .5, y + .5): cv.put(x, y, C(EYE) if d <= 0 else C(PALE4[2]))
        # lengua: sale de la garganta y cuelga por delante de la mandíbula de abajo
        if open_ > 0.45:
            for k in range(11):
                t = k / 10
                u = 2 + t * 13
                v = -0.5 + t * t * (5 + tongue * 1.5) + math.sin(t * 5 + tongue * 2.5) * 0.7
                x, y = jw(u, v, al * 0.6)
                cv.put(x, y, C(TONGUE[2])); cv.put(x, y + 1, C(TONGUE[1]))
                if k == 10: cv.put(x + 1, y + 1, C(TONGUE[1])); cv.put(x, y + 2, C(TONGUE[0])); cv.put(x + 1, y + 2, C(TONGUE[0]))
                if k == 3: cv.put(x, y, C(TONGUE[3]))
    else:                                                  # cerrada: sonrisa de dientes entrelazados
        for k in range(2, 16):
            x, y = jw(k, 0, au)
            if not in_upper(x + .5, y - .5): continue
            cv.put_in(x, y, OUTL)
            if k % 3 == 0: cv.put_in(x, y - 1, C(EYE)); cv.put_in(x + 1, y + 1, C(EYE))
    if swallow is not None:                                # bulto de la presa bajando por el tallo
        x, y = bezier((bx, by), ctrl, neck, 1 - swallow)
        rell5(cv, x, y, 3.0, 2.8, 0, leaf, shift=1); cv.put(x - 1, y - 1, P(leaf)[4])
    if bulge > 0.3:                                        # baba
        x, y = world(rx * 0.7, 2); cv.put_out(x, y + 1, C(VENOM[2])); cv.put_out(x, y + 2, C(VENOM[1]))
    # zarcillos enroscados en la base
    tendril(cv, bx - 6, by - 2, -1, 4.5 - wilt * 1.5, leaf, ph=sway)
    tendril(cv, bx + 7, by - 1, 1, 4.0 - wilt * 1.5, leaf, ph=-sway)
    return cv

def carnivore_anims():
    sw = [math.sin(i / 4 * 2 * math.pi) for i in range(4)]
    a = {}
    a['CarnivorousPlant_Idle'] = [carnivore(open_=0.92 + 0.08 * math.cos(i * math.pi / 2), sway=sw[i], tongue=i / 2,
                                            bob=round(sw[i] * 0.6)) for i in range(4)]
    a['CarnivorousPlant_Snap'] = [carnivore(open_=1.0, lunge=-0.25), carnivore(open_=0.75, lunge=0.5),
                                  carnivore(open_=0.0, lunge=1.0, bulge=0.4), carnivore(open_=0.0, lunge=0.6, bulge=0.2)]
    a['CarnivorousPlant_Chew'] = [carnivore(open_=v, bulge=b, bob=bo, swallow=sw_) for v, b, bo, sw_ in
                                  ((0.0, 0.6, 1, None), (0.18, 0.1, -1, None), (0.0, 0.8, 1, 0.2), (0.0, 0.3, 0, 0.7))]
    a['CarnivorousPlant_Open'] = [carnivore(open_=v, lunge=l) for v, l in ((0.15, 0.4), (0.45, 0.2), (0.8, 0.05), (1.0, 0))]
    a['CarnivorousPlant_Wilt'] = [carnivore(open_=v, wilt=w) for v, w in ((0.7, 0.2), (0.45, 0.5), (0.25, 0.8), (0.15, 1.0))]
    return a

# =================================================================================================
# Bichos de suelo (subtanda 3b): utilidades
# =================================================================================================
BEETLE = ramp('#2a3a6a', 5); BEETLE_H = ramp('#e8702a', 5)  # bombardero: élitros azul noche, cabeza naranja
SPIDER = ramp('#5a6a2a', 5); SPIDER_E = ['#7a1414', '#e04040', '#ff9a8a']  # araña: camuflaje de helecho, ojos rojos
TOAD = ramp('#8a6a3a', 5); TOAD_B = ramp('#c8b070', 5)       # sapo: ocre de lodo, vientre claro
FIRE = ['#8a1e14', '#d4441c', '#f88c2a', '#ffd25a', '#fff4c0']
SILK = ['#8a9a90', '#c8d4cc', '#eef4ee']
MUDB = ['#22160f', '#33221a', '#4a3324', '#634830', '#86683f']  # lodo profundo

def line_pts(x0, y0, x1, y1):
    n = max(1, int(max(abs(x1 - x0), abs(y1 - y0)) + 0.5))
    return [(x0 + (x1 - x0) * k / n, y0 + (y1 - y0) * k / n) for k in range(n + 1)]

def seg(cv, pts, col, behind=True):
    """Polilínea de 1 px (patas, antenas): por detrás del cuerpo si behind."""
    for (a, b) in zip(pts, pts[1:]):
        for (x, y) in line_pts(*a, *b):
            (cv.put_out if behind else cv.put)(round(x - .5), round(y - .5), col)

class Frame2D:
    """Marco de un bicho visto desde arriba: f hacia delante, s hacia su derecha; yaw en radianes (0 = derecha)."""
    def __init__(s, cx, cy, yaw): s.cx, s.cy, s.c, s.s, s.yaw = cx, cy, math.cos(yaw), math.sin(yaw), yaw
    def __call__(s, f, side): return s.cx + f * s.c - side * s.s, s.cy + f * s.s + side * s.c

def bug_eye(cv, x, y, fr, angry=True, red=False, state='open'):
    """Ojo de 2×2 para bichos vistos desde arriba (pupila hacia delante), con ceja enfadada."""
    if FLAT: return
    x, y = int(round(x - .5)), int(round(y - .5))
    if state == 'x':
        cv.put(x, y, OUTL); cv.put(x + 1, y + 1, OUTL); return
    w = C(SPIDER_E[2]) if red else EYE
    for dx in range(2):
        for dy in range(2): cv.put(x + dx, y + dy, w)
    px = x + (1 if fr.c > 0.3 else 0); py = y + (1 if fr.s > 0.3 else 0 if fr.s < -0.3 else 1)
    cv.put(px, py, C(SPIDER_E[0]) if red else OUTL)

def fade_ramp(r, t):
    """De color a apagado (PALE) al morir."""
    return r if t <= 0 else mix_ramp(r, PALE5, min(1, t))

# =================================================================================================
# Escarabajo bombardero (de lado, cámara a 30° como el vinagrillo)
# =================================================================================================
def _bodies():
    """Generador de los cuerpos del jugador: se reutiliza su cámara 3D (Part, trace, camera, draw_lines)."""
    d = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'floor3-bodies')
    if d not in sys.path: sys.path.insert(0, d)
    import generar_cuerpos_piso3 as B
    return B
B3 = _bodies()
SIDE_ELEV = 30                                    # la misma elevación que el vinagrillo
BK, SK = 1.22, 1.15                               # escala del escarabajo y de la araña
_ORIG = {}

def side_origin(key, build, n, yaw=0.0):
    """Origen que centra la pose de referencia (build() -> (parts, lines)) en un lienzo de n×n."""
    if (key, n) not in _ORIG:
        with B3.camera(SIDE_ELEV):
            parts, lines = build()
            cv, info = B3.trace(parts, 80, yaw, (40, 40), {m: ['#000000'] * 5 for m in
                                                            {p.mat for p in parts}})
            B3.draw_lines(cv, info, yaw, (40, 40), lines)
        xs = [x for x, y in cv.px]; ys = [y for x, y in cv.px]
        o = (40 - (min(xs) + max(xs) + 1) / 2 + n / 2, 40 - (min(ys) + max(ys) + 1) / 2 + n / 2)
        _ORIG[(key, n)] = (round(o[0] * 2) / 2, round(o[1] * 2) / 2)
    return _ORIG[(key, n)]

def scaled(model, k):
    """Escala un modelo (parts, lines) entero por k."""
    parts, lines = model
    for p in parts: p.c = tuple(v * k for v in p.c); p.r = tuple(v * k for v in p.r)
    return parts, [([tuple(v * k for v in pt) for pt in pts], cols) for pts, cols in lines]

def flip_u(parts, lines, h):
    """Panza arriba: refleja la altura alrededor de h (el sombreado se recalcula con la luz de siempre)."""
    for p in parts: p.c = (p.c[0], p.c[1], h - p.c[2]); p.pitch = -p.pitch
    return parts, [([(f, l, h - u) for (f, l, u) in pts], cols) for pts, cols in lines]

def visible(info, x, y, d):
    hit = info.get((int(x), int(y)))
    return hit and abs(hit[0] - 60 - d) < 1.8

def side_eye(cv, info, yaw, origin, pt, red=False, state='angry', big=True, mat=None):
    """Ojo en el punto 3D pt, solo si se ve (el píxel es de la pieza mat y el punto mira a la cámara)."""
    if FLAT: return
    x, y, d = B3.screen_of(yaw, origin, *pt)
    hit = info.get((int(x), int(y)))
    if not hit or (mat and hit[1].mat != mat) or (not mat and not visible(info, x, y, d)): return
    if hit[0] - 60 - d < -4: return
    x, y = int(x), int(y)
    if state == 'x':
        cv.put(x, y, OUTL); cv.put(x + 1, y + 1, OUTL); return
    if red:
        cv.put(x, y, C(SPIDER_E[2]));
        if big: cv.put(x + 1, y, C(SPIDER_E[1])); cv.put(x, y + 1, C(SPIDER_E[1])); cv.put(x + 1, y + 1, C(SPIDER_E[0]))
    else:
        for dx in range(2):
            for dy in range(2): cv.put(x + dx, y + dy, EYE)
        fwd = math.cos(math.radians(yaw))
        cv.put(x + (1 if fwd > 0.3 else 0), y + 1, OUTL)
    if state == 'angry' and big: cv.put(x - 1, y - 1, OUTL); cv.put(x, y - 1, OUTL); cv.put(x + 1, y, OUTL) if not red else cv.put(x + 1, y - 1, OUTL)

def beetle_model(walk=None, warn=0.0, open_=0.0, crouch=0.0, kick=None):
    """Escarabajo de ~20 px de largo: élitros en cúpula, pronoto, cabeza naranja y punta del abdomen detrás.
    f hacia delante, l hacia su izquierda (lejos de la cámara), u hacia arriba; el suelo es u = 0."""
    u0 = 1.0 - crouch * 0.5
    P = []
    for sd in (-1, 1):                                                        # élitros: dos medias cúpulas
        e = B3.Part(-1.0, sd * (2.2 + open_ * 0.6), u0 + 2.3 + open_ * 0.5, 6.4, 2.6, 2.6, sd * open_ * 8,
                    'ely', 'body', pitch=open_ * 10)
        P.append(e)
    P.append(B3.Part(4.4, 0, u0 + 1.9, 1.9, 3.3, 1.9, 0, 'pron', 'pron'))      # pronoto
    P.append(B3.Part(6.9, 0, u0 + 1.6, 1.9, 2.3, 1.7, 0, 'head', 'head'))      # cabeza naranja
    P.append(B3.Part(-6.4, 0, u0 + 1.6 + warn * 0.8, 1.7, 1.9, 1.5, 0, 'tip', 'tip', pitch=warn * 25))  # punta
    L = []
    leg = (BEETLE[3], BEETLE[2])
    for k, (hf, ff) in enumerate(((3.6, 6.2), (1.4, 1.2), (-0.8, -4.2))):
        for sd in (-1, 1):
            ph = (walk + (k % 2) * .5 + (.5 if sd > 0 else 0)) if walk is not None else 0
            sw = math.sin(ph * 2 * math.pi) if walk is not None else 0
            lift = max(0, math.cos(ph * 2 * math.pi)) * 0.9 if walk is not None else 0
            if kick is not None: sw = math.sin(kick * 2 * math.pi + k + sd) * 1.2
            L.append(([(hf, sd * 2.4, u0 + 0.8), (hf + (ff - hf) * 0.4 + sw * .6, sd * 5.0, u0 + 2.2 + lift),
                       (ff + sw * 1.3, sd * 6.0, lift * .6)], leg))
    for sd in (-1, 1):                                                          # antenas
        L.append(([(8.3, sd * 1.0, u0 + 2.4), (10.0, sd * 2.4, u0 + 4.6), (11.6, sd * 3.0, u0 + 4.4)], (BEETLE[2], BEETLE[1])))
    return P, L

def beetle(yaw=0.0, walk=None, warn=0.0, recoil=0.0, open_=0.0, eye_state='angry', n=32, crouch=0.0,
           belly_up=False, kick=None, fade=0.0):
    """Escarabajo de lado (mira a la derecha; Flip X para la izquierda). warn enciende la punta del abdomen;
    open_ entreabre los élitros; recoil lo empuja hacia delante al disparar; belly_up lo pone panza arriba."""
    pal = {'body': fade_ramp(BEETLE, fade), 'pron': fade_ramp([BEETLE[0]] + BEETLE[:4], fade),
           'head': fade_ramp(BEETLE_H, fade),
           'tip': fade_ramp(BEETLE_H if warn <= 0 else mix_ramp(BEETLE_H, FIRE, warn), fade)}
    if belly_up: pal['body'] = fade_ramp(mix_ramp(BEETLE_H, BEETLE, 0.55), fade)
    o = side_origin('beetle', lambda: scaled(beetle_model(), BK), n)
    o = (o[0] + recoil, o[1])
    with B3.camera(SIDE_ELEV):
        parts, lines = scaled(beetle_model(walk, warn, open_, crouch, kick), BK)
        if belly_up: parts, lines = flip_u(parts, lines, 6.0 * BK)
        cv, info = B3.trace(parts, n, yaw, o, pal, flat=[C(c) for c in FLAT] if FLAT else None)
        if not FLAT and not belly_up:                                           # costura y brillo de los élitros
            r = [C(c) for c in pal['body']]
            for (x, y), (t, p, q, nw) in info.items():
                if p.mat == 'body' and abs(q[1]) > 0.86 and nw[2] > 0.2 and cv.px[(x, y)] in r:
                    cv.px[(x, y)] = r[0]
        cv.outline(); cv.clean()
        B3.draw_lines(cv, info, yaw, o, lines, flat=FLAT)
        if not belly_up:
            for sd in (-1, 1):
                side_eye(cv, info, yaw, o, (7.9 * BK, sd * 1.5 * BK, (2.8 - crouch * .5) * BK), state=eye_state, mat='head')
            if warn > 0.5 and not FLAT:
                x, y, d = B3.screen_of(yaw, o, -8.0 * BK, 0, (2.2 + warn * 1.4) * BK)
                cv.put(x, y, C(FIRE[4])); cv.put(x + 1, y, C(FIRE[3]))
        elif eye_state == 'x' and not FLAT:
            x, y, d = B3.screen_of(yaw, o, 8.0 * BK, -1.4 * BK, (6.0 - 2.9) * BK)
            cv.put(x, y, OUTL); cv.put(x + 1, y + 1, OUTL)
    return cv

def beetle_spray_origin(n=32):
    """Píxel de la punta del abdomen (de donde sale el cono hacia atrás) en el lienzo de Beetle_Spray."""
    o = side_origin('beetle', lambda: scaled(beetle_model(), BK), n)
    with B3.camera(SIDE_ELEV):
        x, y, d = B3.screen_of(0.0, o, -8.0 * BK, 0, 3.0 * BK)
    return int(x), int(y)

def beetle_spray(i):
    """Chorro ardiente en cono (3 u = 48 px): sale del borde izquierdo a media altura hacia la derecha. 56×28."""
    cv = Canvas(56, 28)
    reach = (0.45, 0.8, 1.0, 1.0, 0.7, 0.3)[i]
    fade = (0, 0, 0, 0.2, 0.6, 0.9)[i]
    import random
    rnd = random.Random(7 + i)
    for x in range(int(50 * reach)):
        t = x / 48
        half = 1.2 + t * 10.5
        for y in range(28):
            d = abs(y + .5 - 14) / half
            if d > 1: continue
            heat = (1 - t) * 0.75 + (1 - d) * 0.6 - fade + rnd.uniform(-0.18, 0.18)
            if fade > 0 and rnd.random() < fade * 0.7: continue
            idx = 4 if heat > 1.05 else 3 if heat > 0.8 else 2 if heat > 0.55 else 1 if heat > 0.3 else 0
            a = 255 if d < 0.8 else 200
            cv.put(x, y, C(FIRE[idx], a))
    # gotas hirviendo que saltan por delante
    for k in range(5):
        x = int(50 * reach) - 2 + rnd.randint(-6, 3); y = 14 + rnd.randint(-9, 9)
        if 0 <= x < 56 and fade < 0.8: cv.put(x, y, C(FIRE[3])); cv.put(x + 1, y, C(FIRE[2]))
    return cv

def beetle_anims():
    global FLAT
    a = {}
    a['Beetle_Idle'] = [beetle(open_=0.12 * (i % 2), crouch=0.2 * (i in (1, 2))) for i in range(4)]
    a['Beetle_Move'] = [beetle(walk=i / 4) for i in range(4)]
    a['Beetle_Turn'] = [beetle(yaw=-d, walk=k / 3) for k, d in enumerate((45, 90, 135))]
    a['Beetle_Warn'] = [beetle(warn=w, open_=o, crouch=c) for w, o, c in ((0.3, 0.2, 0.2), (0.6, 0.5, 0.4),
                                                                          (0.85, 0.7, 0.5), (1.0, 0.7, 0.5))]
    a['Beetle_Spray'] = [beetle(warn=1.0, open_=0.7, recoil=r, crouch=0.5) for r in (1.0, 0.5, 1.0)]
    FLAT = WHITE5; w = beetle(); FLAT = RED5; r = beetle(); FLAT = None
    a['Beetle_Hurt'] = [w, r]
    a['Beetle_Death'] = ([beetle(n=40, eye_state='x', crouch=1.0)] +
                         [beetle(n=40, belly_up=True, eye_state='x', kick=k, fade=t)
                          for t, k in ((0, 0), (0.1, 0.3), (0.3, 0.6), (0.6, 0.8), (1.0, 1.0))])
    return a

# =================================================================================================
# Araña de las frondas (de lado, cámara a 30°)
# =================================================================================================
def spider_model(walk=None, crouch=0.0, air=0.0, spin=0.0, curl=0.0, lift=0.0):
    """Araña de ~26 px de pata a pata: abdomen grande detrás, cefalotórax delante, 8 patas arqueadas."""
    u0 = 2.6 - crouch * 1.2 + lift
    P = [B3.Part(-4.4, 0, u0 + 1.6 + spin * 0.8, 4.6, 4.0, 3.6, 0, 'abd', 'abd', pitch=spin * 25),
         B3.Part(1.9, 0, u0 + 0.6, 3.0, 2.6, 2.0, 0, 'ceph', 'ceph')]
    for sd in (-1, 1):                                                          # quelíceros
        P.append(B3.Part(4.6, sd * 0.9, u0 - 0.3, 1.0, 0.8, 1.3, 0, 'ceph', 'ceph'))
    L = []
    for k, (hf, ff, reach) in enumerate(((3.0, 9.0, 1.0), (2.2, 4.0, 1.05), (1.4, -2.0, 1.0), (0.6, -7.5, 1.1))):
        for sd in (-1, 1):
            ph = (walk + (k % 2) * .5 + (.5 if sd > 0 else 0)) if walk is not None else 0
            sw = math.sin(ph * 2 * math.pi) if walk is not None else 0
            up = max(0, math.cos(ph * 2 * math.pi)) * 1.0 if walk is not None else 0
            R = reach * (1 - curl * 0.6)
            kneeu = u0 + 6.0 - crouch * 1.0 + up - air * 3.5 - curl * 2.5
            foot_u = max(0.0, up * .6 - air * 1.0 + lift * (1 - air * 0.2) - lift) if curl == 0 else u0 + 1.0
            foot = (hf + (ff - hf) * R + sw * 1.2 - air * 1.5 * (1 if ff < 0 else -0.5), sd * (7.5 * R + air * 1.5),
                    foot_u + (lift if air > 0 else 0) - (2.5 * air if air > 0 else 0))
            if curl > 0: foot = (hf + (ff - hf) * 0.25, sd * 2.5, u0 + 3.0 * curl)
            knee = (hf + (ff - hf) * 0.4 * R + sw * .6, sd * 4.0 * R, kneeu)
            L.append(([(hf, sd * 1.6, u0 + 0.4), knee, foot], (SPIDER[3], SPIDER[2])))
    return P, L

def spider(yaw=0.0, walk=None, crouch=0.0, air=0.0, spin=0.0, curl=0.0, lift=0.0, eye_state='angry', n=32,
           belly_up=False, fade=0.0):
    """Araña de lado (mira a la derecha). crouch se agacha antes de saltar; lift la sube (salto) y air estira las
    patas hacia atrás en el aire; spin alza el abdomen y saca seda de las hileras; curl encoge las patas."""
    pal = {'abd': fade_ramp(SPIDER, fade), 'ceph': fade_ramp([SPIDER[0]] + SPIDER[:4], fade)}
    o = side_origin('spider', lambda: scaled(spider_model(), SK), n)
    o = (o[0], o[1] + (n - 32) * 0.25)
    with B3.camera(SIDE_ELEV):
        parts, lines = scaled(spider_model(walk, crouch, air, spin, curl, lift), SK)
        if belly_up: parts, lines = flip_u(parts, lines, 7.0 * SK)
        cv, info = B3.trace(parts, n, yaw, o, pal, flat=[C(c) for c in FLAT] if FLAT else None)
        if not FLAT:                                                            # chevrones de fronda en el abdomen
            r = [C(c) for c in pal['abd']]
            for (x, y), (t, p, q, nw) in info.items():
                if p.mat != 'abd' or cv.px[(x, y)] not in r: continue
                band = ((q[0] * 1.6 + abs(q[1]) * 1.1) % 1) < 0.22 and -0.75 < q[0] < 0.6 and abs(q[1]) < 0.75
                if band and nw[2] > -0.1:
                    cv.px[(x, y)] = r[min(4, r.index(cv.px[(x, y)]) + 1)]
        cv.outline(); cv.clean()
        if FLAT: lines = [(pts, (FLAT[2], FLAT[2])) for pts, cols in lines]
        B3.draw_lines(cv, info, yaw, o, lines if not fade else [(pts, (fade_ramp(SPIDER, fade)[3], fade_ramp(SPIDER, fade)[2])) for pts, c in lines])
        u0 = 2.6 - crouch * 1.2 + lift
        if not belly_up:
            for sd in (-1, 1):                                                  # ojos rojos: 2 grandes y 2 pequeños
                side_eye(cv, info, yaw, o, (4.3 * SK, sd * 1.0 * SK, (u0 + 1.3) * SK), red=True, state=eye_state, mat='ceph')
                side_eye(cv, info, yaw, o, (3.6 * SK, sd * 1.6 * SK, (u0 + 2.0) * SK), red=True, state='open', big=False, mat='ceph')
        if spin > 0.3 and not FLAT:                                             # seda de las hileras
            x, y, d = B3.screen_of(yaw, o, -9.0 * SK, 0, (u0 + 1.6 + spin * 2.2) * SK)
            cv.put(x, y, C(SILK[2])); cv.put(x - 1, y, C(SILK[1])); cv.put(x - 2, y + 1, C(SILK[0]))
    return cv

def spider_hidden(i):
    """Capa para poner sobre un helecho del escenario (que hace Fern_*_Rustle): sombra en la base y ojos rojos
    que se abren y parpadean (24×12). Va encima del helecho, centrada en su base."""
    cv = Canvas(24, 12)
    for y in range(12):
        for x in range(24):
            d = ((x + .5 - 12) / 11) ** 2 + ((y + .5 - 7) / 4.5) ** 2
            if d <= 1: cv.put(x, y, (14, 12, 18, int(170 * (1 - d * 0.6))))
    look = (-1, 0, 1, 0)[i]
    lit = (True, True, False, True)[i]                                   # el cuadro 2 parpadea
    for (x, y, big) in ((8, 6, True), (14, 6, True), (6, 8, False), (17, 8, False)):
        if not lit and big: cv.put(x + look, y + 1, C(SPIDER_E[0])); cv.put(x + 1 + look, y + 1, C(SPIDER_E[0])); continue
        cv.put(x + look, y, C(SPIDER_E[1]));
        if big: cv.put(x + 1 + look, y, C(SPIDER_E[1])); cv.put(x + look, y, C(SPIDER_E[2]))
    return cv

def web(burn=0.0, appear=1.0, gone=0.0):
    """Telaraña en el suelo (32×32): radios y espiral de seda; burn 0..1 la recorre el fuego desde el centro."""
    cv = Canvas(32)
    import random
    rnd = random.Random(3)
    def col(r):
        if r < burn * 15 - 3: return None                                 # ya quemada
        if r < burn * 15: return C(FIRE[rnd.randint(2, 4)])
        return C(SILK[1], 230) if r % 4 < 2 else C(SILK[2], 230)
    R = 14 * appear
    for k in range(8):
        a = k * math.pi / 4 + 0.2
        for (x, y) in line_pts(16, 16, 16 + math.cos(a) * R, 16 + math.sin(a) * R * 0.8):
            r = math.hypot(x - 16, y - 16)
            c = col(r)
            if c and rnd.random() > gone: cv.put(x, y, c)
    for ring in range(3, int(R), 3):
        for k in range(48):
            a = k * 2 * math.pi / 48
            rr = ring + (k % 6) * 0.1
            x, y = 16 + math.cos(a) * rr, 16 + math.sin(a) * rr * 0.8
            c = col(rr)
            if c and rnd.random() > gone + 0.15: cv.put(x, y, C(SILK[0], 200) if c[3] == 230 and c[:3] == C(SILK[1])[:3] else c)
    if burn > 0:                                                            # humito
        for k in range(4):
            cv.put(16 + rnd.randint(-8, 8), 16 + rnd.randint(-7, 7), C('#6a6466', 140))
    return cv

def silk_ball(i):
    """Bola de seda lanzada (10×10)."""
    cv = Canvas(10)
    ell5(cv, 5, 5, 3 + (i % 2) * 0.3, 3, [SILK[0], SILK[0], SILK[1], SILK[2], SILK[2]])
    cv.outline(C('#4a5450'))
    cv.put(4, 3, C(SILK[2]))
    return cv

def spider_anims():
    global FLAT
    a = {}
    a['Spider_Hidden'] = [spider_hidden(i) for i in range(4)]
    a['Spider_Reveal'] = [spider(crouch=1.0), spider(lift=2.5, air=0.5), spider(lift=1.0, air=0.2), spider()]
    a['Spider_Idle'] = [spider(crouch=0.15 * (i in (1, 2)), spin=0.0) for i in range(4)]
    a['Spider_Move'] = [spider(walk=i / 4) for i in range(4)]
    a['Spider_Jump'] = [spider(n=40, **k) for k in (dict(crouch=1.0), dict(lift=3, air=0.6), dict(lift=6, air=1.0),
                                                     dict(lift=4, air=0.7), dict(crouch=0.7), dict())]
    a['Spider_Shoot'] = [spider(spin=s) for s in (0.4, 1.0, 1.0, 0.3)]
    FLAT = WHITE5; w = spider(); FLAT = RED5; r = spider(); FLAT = None
    a['Spider_Hurt'] = [w, r]
    a['Spider_Death'] = ([spider(n=40, eye_state='x', crouch=1.0)] +
                         [spider(n=40, belly_up=True, curl=c, fade=t) for c, t in
                          ((0.4, 0.1), (0.7, 0.3), (1.0, 0.6), (1.0, 1.0))])
    return a

# =================================================================================================
# Sapo de fango (de perfil, mira a la derecha)
# =================================================================================================
def toad(breath=0.0, squash=0.0, lift=0.0, stretch=0.0, mouth=0.0, sac=0.0, puff=0.0, shake=0, sink_=0.0,
         belly_up=False, eye_state='angry', n=32, off=(0, 0), pal=None, bpal=None):
    """Sapo rechoncho y verrugoso, más ancho y chato que la rana del jugador. puff 0..1 lo hincha (aviso de
    reventar); sac 0..1 hincha el saco de la garganta (escupe); mouth abre la boca; sink_ 0..1 lo hunde en el
    lodo (nada en el lodo profundo: solo asoman ojos y lomo)."""
    cv = Canvas(n)
    body = pal or TOAD; belly = bpal or TOAD_B
    ox, oy = off[0] + shake, off[1]
    k = 1 + puff * 0.2
    gy = 27 + oy                                           # suelo
    cx, cy = 15 + ox - stretch * 1.5 - puff * 1.5, gy - 6.5 * k * (1 - squash * 0.25) - lift + breath * 0.3
    # pata trasera plegada (muslo) detrás
    if not belly_up:
        ell5(cv, cx - 6.5 + stretch * -2, gy - 3 - lift * 0.6, 4.2, 3.2 * (1 - stretch * 0.4), body, shift=-1)
    # cuerpo
    ell5(cv, cx, cy, 10.5 * k * (1 + squash * 0.12 + stretch * 0.15), 6.8 * k * (1 - squash * 0.15 - stretch * 0.15), body, shift=-1)
    # cabeza ancha y chata delante
    hx, hy = cx + 8.5 * k, cy - 1.5 + stretch * -1
    ell5(cv, hx, hy, 6.2 * k, 4.6 * k * (1 + mouth * 0.15), body, shift=-1)
    # protuberancia del ojo y glándula parótida detrás
    ell5(cv, hx - 1.5, hy - 4.4 * k, 3.2, 2.6, body)
    ell5(cv, hx - 6, hy - 4 * k, 3.0, 1.8, body, shift=-1)
    # saco de la garganta
    if sac > 0:
        ell5(cv, hx + 1, hy + 3.5 + sac * 1.2, 3 + sac * 2.2, 1.8 + sac * 2.2, belly)
    # vientre claro
    fill5(cv, lambda x, y: ((x - cx - 2) / (9.5 * k)) ** 2 + ((y - cy - 3.5 * k) / (3.5 * k)) ** 2 <= 1 and
          y > cy + 1.5, cx + 2, cy + 3, 10, 4, belly)
    # pata delantera
    if not belly_up:
        fx = hx - 1 + stretch * 3
        for y in range(int(hy + 3), int(gy + 1 - lift)): cv.put(fx, y, P(body)[2]); cv.put(fx + 1, y, P(body)[1])
        cv.put(fx + 2, gy - lift, P(body)[1]); cv.put(fx - 1, gy - lift, P(body)[1])
    # pie trasero (y estirado al saltar)
    if not belly_up:
        fx0 = cx - 6 - stretch * 6
        for x in range(int(fx0 - 2), int(fx0 + 4)): cv.put(x, gy - lift * 0.3 + (stretch > 0.4) * -1, P(body)[1])
    cv.outline(); cv.clean()
    if not FLAT:
        r = P(body)
        # verrugas: bultitos oscuros con brillo arriba a la izquierda
        for (dx, dy, w) in ((-7, -3, 3), (-1, -5, 2), (-4, 0, 3), (2, -2, 2), (-9, 1.5, 2)):   # manchas de lodo
            x, y = cx + dx * k, cy + dy * k
            for j in range(w): cv.put_in(x + j, y, r[0]); cv.put_in(x + j - 1, y + 1, r[0]) if j else None
        for (dx, dy) in ((-5, -5), (-1, -3), (3, -4.5), (-8, -2), (-3, -2), (0, 0), (5, -1.5)):  # verrugas claras
            x, y = cx + dx * k, cy + dy * k
            cv.put_in(x, y, r[3]); cv.put_in(x + 1, y + 1, r[1])
        # glándula con puntos
        cv.put_in(hx - 7, hy - 4.5 * k, r[3]); cv.put_in(hx - 5, hy - 4 * k, r[1])
        # boca: línea larga; abierta, interior oscuro
        mx0, mx1 = hx - 3, hx + 6 * k
        my = hy + 1.2
        if mouth > 0.15:
            for x in range(int(mx0), int(mx1)):
                t = (x - mx0) / max(1, mx1 - mx0)
                h = round(mouth * 3 * math.sin(t * math.pi * 0.9 + 0.2))
                for j in range(h + 1): cv.put_in(x, my + j, C(MOUTH[0] if j < h else MOUTH[1]))
                cv.put_in(x, my - 1, OUTL)
        else:
            for x in range(int(mx0), int(mx1)): cv.put_in(x, my + (1 if x < mx0 + 2 else 0), OUTL)
        if puff > 0.6:                                       # hinchado al límite: venas y brillo tirante
            for (dx, dy) in ((-3, 0), (3, -1), (0, 2)): cv.put_in(cx + dx * k, cy + dy * k, C('#d07a5a'))
            cv.put_in(cx - 5 * k, cy - 4 * k, C(belly[4] if isinstance(belly[4], str) else '#ffffff'))
    # ojo grande encima de la cabeza
    if not belly_up:
        ex, ey = hx - 3.5, hy - 6 * k
        if not FLAT:
            st = 'x' if eye_state == 'x' else 'shut' if eye_state == 'shut' else 'open'
            if st == 'open':
                ex, ey = int(ex), int(ey)
                for dx in range(4):
                    for dy in range(3): cv.put(ex + dx, ey + dy, C('#f0d060' if dy == 0 else '#d8a830'))  # iris dorado
                cv.put(ex + 1, ey + 1, OUTL); cv.put(ex + 2, ey + 1, OUTL); cv.put(ex + 3, ey + 1, OUTL)  # pupila horizontal
                cv.put(ex, ey, C('#fff4c0'))
                if eye_state == 'angry':                                         # párpado grueso que baja hacia delante
                    for dx in range(-1, 5): cv.put(ex + dx, ey - 1, OUTL)
                    cv.put(ex + 3, ey, OUTL); cv.put(ex + 2, ey, P(body)[1])
            else: eye(cv, ex, ey, state=st)
    # hundirse: lo de debajo del nivel del lodo desaparece y queda una superficie de lodo con ondas
    if sink_ > 0:
        level = gy - sink_ * 9
        for (x, y) in list(cv.px):
            if y > level: del cv.px[(x, y)]
        for x in range(int(cx - 13), int(cx + 15)):
            d = abs(x - cx - 1) / 14
            if d > 1: continue
            cv.put(x, level, C(MUDB[3] if d < 0.7 else MUDB[2]))
            cv.put(x, level + 1, C(MUDB[2])); cv.put(x, level + 2, C(MUDB[1]))
            if 0.55 < d < 0.75: cv.put(x, level, C(MUDB[4]))
    return cv

def toad_belly_up(t, n=40):
    """Muerte: panza arriba, patas al aire, se apaga."""
    cv = Canvas(n)
    b = fade_ramp(TOAD_B, t); r = fade_ramp(TOAD, t)
    cx, cy = n / 2, n / 2 + 4
    ell5(cv, cx, cy, 11, 6, r, shift=-1)
    ell5(cv, cx, cy - 1.5, 9, 4.5, b)
    cv.outline(); cv.clean()
    lc = P(r)[1]
    for (x0, s) in ((cx - 7, -1), (cx - 4, -1), (cx + 5, 1), (cx + 8, 1)):
        seg(cv, [(x0, cy - 4), (x0 + s, cy - 8), (x0 + s * 2, cy - 9)], lc)
    if not FLAT:
        eye(cv, cx + 7, cy, state='x')
    return cv

def mud_ball(i):
    """Bola de lodo que tiembla en vuelo (12×12)."""
    cv = Canvas(12)
    ell5(cv, 6, 6, 3.4 + (i % 2) * 0.4, 3.4 - (i % 2) * 0.4, MUDB[1:] + [MUDB[4]])
    cv.outline(); cv.clean()
    cv.put(5, 4, C(MUDB[4])); cv.put_out(2 - i % 2, 7, C(MUDB[3])); cv.put_out(1, 6 + i % 2, C(MUDB[2]))
    return cv

def mud_splat(i, size=24, drops=7, seed=5):
    """Salpicón de lodo al caer la bola o al reventar el sapo (el charco que ralentiza es Puddle_Sludge_*)."""
    import random
    rnd = random.Random(seed)
    cv = Canvas(size)
    c = size / 2
    if i == 0: ell5(cv, c, c, 3.5, 2.5, MUDB[1:] + [MUDB[4]]); cv.outline(); return cv
    t = i / 4
    ell5(cv, c, c + 1, 3 + t * size * 0.18, 1.6 + t * size * 0.1, MUDB[:4] + [MUDB[3]], shift=-1)
    for k in range(drops):
        a = k * 2 * math.pi / drops + rnd.uniform(-0.3, 0.3)
        d = (2 + t * size * 0.33) * rnd.uniform(0.8, 1.1)
        x, y = c + math.cos(a) * d, c + math.sin(a) * d * 0.7 - (1 - t) * 3
        if t < 1: cv.put(x, y, C(MUDB[3])); cv.put(x + 1, y, C(MUDB[2]))
        else: cv.put(x, y + 1, C(MUDB[2]))
    return cv

def toad_anims():
    global FLAT
    a = {}
    a['Toad_Idle'] = [toad(breath=b, sac=s) for b, s in ((0, 0), (1, 0.15), (2, 0.3), (1, 0.15))]
    a['Toad_Hop'] = [toad(squash=0.6), toad(stretch=0.8, lift=4), toad(stretch=0.4, lift=5), toad(squash=0.4, lift=1)]
    a['Toad_Spit'] = [toad(sac=0.5), toad(sac=1.0, squash=0.2), toad(sac=0.6, mouth=0.5), toad(mouth=1.0, stretch=0.3),
                      toad(mouth=0.3)]
    a['Toad_Inflate'] = [toad(puff=p, shake=s, eye_state='shut' if p > 0.7 else 'angry', n=40, off=(4, 6))
                         for p, s in ((0.3, 0), (0.55, 1), (0.8, -1), (1.0, 1))]
    a['Toad_Dive'] = [toad(squash=0.4), toad(sink_=0.4), toad(sink_=0.75)]
    a['Toad_Swim'] = [toad(sink_=0.78 - 0.04 * math.sin(i * math.pi / 2), breath=i % 2) for i in range(4)]
    FLAT = WHITE5; w = toad(); FLAT = RED5; r = toad(); FLAT = None
    a['Toad_Hurt'] = [w, r]
    a['Toad_Death'] = [toad(n=40, off=(4, 6), eye_state='x', squash=0.5), toad_belly_up(0), toad_belly_up(0.2),
                       toad_belly_up(0.5), toad_belly_up(0.8), toad_belly_up(1.0)]
    return a

def ground_fx():
    return {'Fx_BeetleSpray': [beetle_spray(i) for i in range(6)],
            'Fx_SpiderWeb_Appear': [web(appear=v) for v in (0.3, 0.6, 1.0)],
            'Fx_SpiderWeb_Idle': [web()],
            'Fx_SpiderWeb_Burn': [web(burn=v) for v in (0.15, 0.35, 0.6, 0.85, 1.1)],
            'Proj_SpiderSilk': [silk_ball(i) for i in range(2)],
            'Proj_ToadMud': [mud_ball(i) for i in range(2)],
            'Fx_ToadMud_Splat': [mud_splat(i) for i in range(5)],
            'Fx_Toad_Burst': [mud_splat(i, size=48, drops=14, seed=9) for i in range(5)]}

def ground():
    a = {}
    a.update(beetle_anims()); a.update(spider_anims()); a.update(toad_anims())
    write(a, 'suelo')
    fondos(os.path.join(OUT, 'fondos_suelo.png'), a, ['Beetle_Move', 'Spider_Idle', 'Toad_Idle', 'Toad_Swim'])
    write(ground_fx(), 'efectos_suelo', fx=True)
    # prueba: la araña escondida sobre un helecho de verdad del escenario (pantano)
    env = os.path.join(OUT, '..', 'floor3-environment', 'Swamp', 'props', 'Fern_Leafy_Rustle.png')
    if os.path.exists(env):
        fern = Image.open(env); fw = fern.width // 6
        frames = []
        for i in range(4):
            cell = Image.new('RGBA', (fw, fern.height), (0, 0, 0, 0))
            cell.alpha_composite(fern.crop(((i % 6) * fw, 0, (i % 6 + 1) * fw, fern.height)))
            cell.alpha_composite(as_img(spider_hidden(i)), (fw // 2 - 12, fern.height - 14))
            frames.append(cell)
        preview(os.path.join(OUT, 'vista_previa_arana_escondida.png'), {'Spider_Hidden sobre Fern_Leafy_Rustle': frames}, BG, scale=4)
        gif(os.path.join(OUT, 'gif', 'prueba_arana_escondida.gif'), frames, 180, BG, scale=4)

# =================================================================================================
# Voladores (subtanda 3c): libélula gigante y enjambre de efímeras
# =================================================================================================
DRAGON = ramp('#2aa8a8', 5); WING = ramp('#c8ecf0', 4)       # libélula: cuerpo turquesa, alas claras
MAYFLY = ['#8a7a4a', '#d8d0a0', '#f4f0d8']                    # efímeras: cuerpo crema
DK = 1.1                                                      # escala de la libélula

def blend(cv, x, y, col, a):
    """Pinta col con opacidad a (0..1) encima de lo que haya (o sola, translúcida, si no hay nada)."""
    x, y = int(math.floor(x)), int(math.floor(y))
    if not cv._in(x, y): return
    c = C(col) if isinstance(col, str) else col
    old = cv.px.get((x, y))
    if old is None or old[3] == 0: cv.px[(x, y)] = (c[0], c[1], c[2], int(255 * a)); return
    if old[3] < 255:                                         # ala sobre ala: suma opacidad
        cv.px[(x, y)] = (int(old[0] * .5 + c[0] * .5), int(old[1] * .5 + c[1] * .5), int(old[2] * .5 + c[2] * .5),
                         min(255, old[3] + int(255 * a * .5))); return
    cv.px[(x, y)] = (int(old[0] * (1 - a) + c[0] * a), int(old[1] * (1 - a) + c[1] * a), int(old[2] * (1 - a) + c[2] * a), 255)

def dragon_model(pitch=0.0, legs_down=0.0, curl=0.0, bend=0.0):
    """Libélula de ~30 px: cabeza con ojos grandes, tórax y abdomen largo de 7 segmentos (bend lo curva hacia
    abajo, curl lo encoge al morir). pitch inclina todo el cuerpo (picado, en grados, positivo = morro abajo)."""
    P = []
    pr = math.radians(pitch)
    def rot(f, u):                                           # cabeceo alrededor del tórax (f = 3, u = 0)
        df = f - 3
        return 3 + df * math.cos(pr) + u * math.sin(pr) * 0, u - df * math.sin(pr)
    f, u = rot(7.4, 0.5); P.append(B3.Part(f, 0, u + 6, 2.3, 2.9, 2.4, 0, 'head', 'head', pitch=-pitch))
    f, u = rot(3.6, 0.0); P.append(B3.Part(f, 0, u + 6, 2.8, 2.0, 2.3, 0, 'thor', 'body', pitch=-pitch))
    a = 0.0; ff, uu = 0.9, 0.2
    for i in range(7):                                       # abdomen: segmentos que adelgazan
        a += math.radians(bend * 6 + curl * 22)
        step = 2.15
        ff -= step * math.cos(a); uu -= step * math.sin(a)
        f, u = rot(ff, uu)
        r = 1.15 - i * 0.07
        p = B3.Part(f, 0, u + 6, 1.25, r, r, 0, 'abd', 'body', pitch=-pitch - math.degrees(a))
        p.seg = i; P.append(p)
    L = []
    for k, hf in enumerate((4.4, 3.6, 2.8)):                 # patas: recogidas en vuelo, abajo al posarse
        for sd in (-1, 1):
            f0, u0 = rot(hf, -1.6)
            f1, u1 = rot(hf + 1.2 - k * 1.0, -2.6 - legs_down * 2.5)
            f2, u2 = rot(hf + 2.2 - k * 2.0 - legs_down * 0.5, -3.0 - legs_down * 3.6)
            L.append(([(f0, sd * 1.0, u0 + 6), (f1, sd * (1.6 + legs_down * 1.5), u1 + 6), (f2, sd * (1.8 + legs_down * 2.5), u2 + 6)],
                      (DRAGON[1], DRAGON[0])))
    return P, L

def dragon_wings(cv, info, o, flap=0.0, swept=0.0, flat_=0.0, pitch=0.0, alpha=0.55, crumple=0.0):
    """4 alas translúcidas desde el tórax. flap -1..1 = arriba/abajo; swept 0..1 las echa hacia atrás (picado);
    flat_ 0..1 extendidas planas a los lados (posada); crumple las arruga al morir."""
    pr = math.radians(pitch)
    base_u = 6 + 1.6 - 3.6 * math.sin(0)
    wings = []
    for (bf, length, lag) in ((4.2, 12.0, 0.0), (2.6, 11.0, 0.35)):        # par delantero y trasero
        for sd in (1, -1):                                                   # primero el ala lejana
            ph = flap if lag == 0 else -flap * 0.6
            up = (ph * 5.5) * (1 - flat_) - crumple * 4 + (0 if flat_ else 1.0)
            sweep = 0.75 + lag * 0.35 + swept * 0.55 - flat_ * 0.5 + crumple * 0.4    # ángulo hacia atrás
            ln = length * (1 - crumple * 0.5)
            tip = (bf - ln * math.sin(sweep), sd * ln * math.cos(sweep), base_u + up)
            root = (bf, sd * 1.4, base_u)
            # cabeceo
            def r_(p):
                df = p[0] - 3
                return (3 + df * math.cos(pr), p[1], p[2] - df * math.sin(pr))
            wings.append((sd, r_(root), r_(tip)))
    wings.sort(key=lambda w: -w[0])
    body = [C(c) for c in (FLAT if FLAT else DRAGON)]
    for sd, root, tip in wings:
        (x0, y0, d0) = B3.screen_of(0.0, o, *root); (x1, y1, d1) = B3.screen_of(0.0, o, *tip)
        L = math.hypot(x1 - x0, y1 - y0)
        if L < 1: continue
        ux, uy = (x1 - x0) / L, (y1 - y0) / L
        w = 2.4 if L > 5 else 1.5                                           # ancho del ala en pantalla
        far = sd > 0
        for py in range(int(min(y0, y1) - 4), int(max(y0, y1) + 5)):
            for px in range(int(min(x0, x1) - 4), int(max(x0, x1) + 5)):
                vx, vy = px + .5 - x0, py + .5 - y0
                t = vx * ux + vy * uy; n_ = -vx * uy + vy * ux
                if not 0 <= t <= L: continue
                half = w * math.sin(math.pi * min(1, t / L * 0.92 + 0.08)) ** 0.6
                if abs(n_) > half: continue
                edge = abs(n_) > half - 1 or t > L - 1
                vein = abs(n_) < 0.5
                col = (FLAT[2] if FLAT else WING[0] if edge else WING[1] if vein else WING[3 if n_ < 0 else 2])
                a = (0.95 if edge else 0.7 if vein else alpha) * (0.8 if far else 1)
                hit = (px, py) in cv.px and cv.px[(px, py)][3] == 255
                if far and hit: continue                                   # el ala lejana queda detrás del cuerpo
                if hit:                                                    # ala delante del cuerpo: el cuerpo sube un
                    old = cv.px[(px, py)]                                  # tono de su rampa (sin colores mezclados)
                    if old in body: cv.px[(px, py)] = body[min(4, body.index(old) + 1)]
                    elif old == OUTL: cv.px[(px, py)] = C(WING[0])
                    continue
                blend(cv, px, py, col, a)
        # mancha en la punta (pterostigma)
        if (int(x1 - ux * 1.5), int(y1 - uy * 1.5)) not in cv.px or cv.px[(int(x1 - ux * 1.5), int(y1 - uy * 1.5))][3] < 255:
            cv.put(x1 - ux * 1.5, y1 - uy * 1.5, C(FLAT[2] if FLAT else DRAGON[1]))

def dragon(flap=0.0, pitch=0.0, swept=0.0, perch=0.0, warn=0.0, eye_state='angry', n=40, bend=0.0, curl=0.0,
           crumple=0.0, fade=0.0, belly_up=False, streaks=0.0):
    """Libélula gigante de lado (mira a la derecha). flap = fase del aleteo; pitch/swept = picado; perch = posada
    (patas abajo, alas planas); warn 0..1 = fija la línea de ataque (ojos que brillan)."""
    pal = {'body': fade_ramp(DRAGON, fade), 'head': fade_ramp(DRAGON, fade)}
    o = side_origin('dragon', lambda: scaled(dragon_model(), DK), n)
    with B3.camera(SIDE_ELEV):
        parts, lines = scaled(dragon_model(pitch, perch, curl, bend), DK)
        if belly_up: parts, lines = flip_u(parts, lines, 12.0)
        cv, info = B3.trace(parts, n, 0.0, o, pal, flat=[C(c) for c in FLAT] if FLAT else None)
        if not FLAT:                                                           # anillos claros del abdomen
            r = [C(c) for c in pal['body']]
            for (x, y), (t, p, q, nw) in info.items():
                if getattr(p, 'seg', None) is not None and q[0] > 0.55 and cv.px[(x, y)] in r:
                    cv.px[(x, y)] = r[min(4, r.index(cv.px[(x, y)]) + 2)]
        cv.outline(); cv.clean()
        B3.draw_lines(cv, info, 0.0, o, lines, flat=FLAT)
        if True:
            dragon_wings(cv, info, o, flap, swept, perch, pitch, crumple=crumple,
                         alpha=0.55 * (1 - fade * 0.6))
        if not belly_up and not FLAT:
            # ojo de la familia en el ojo compuesto (enfadado); con warn, brillo rojo alrededor
            x, y, d = B3.screen_of(0.0, o, *(lambda f, u: (f * DK, -1.6 * DK, u * DK))(*(
                (3 + (8.4 - 3) * math.cos(math.radians(pitch)), 7.4 - (8.4 - 3) * math.sin(math.radians(pitch))))))
            x, y = int(x), int(y) - 1
            if eye_state == 'x': cv.put(x, y, OUTL); cv.put(x + 1, y + 1, OUTL); cv.put(x + 1, y, OUTL); cv.put(x, y + 1, OUTL)
            else:
                for dx in range(2):
                    for dy in range(2): cv.put(x + dx, y + dy, EYE)
                cv.put(x + 1, y + 1, OUTL)
                if eye_state == 'angry': cv.put(x - 1, y - 1, OUTL); cv.put(x, y - 1, OUTL); cv.put(x + 1, y, OUTL)
                if warn > 0.4: cv.put(x + 2, y, C(FIRE[3])); cv.put(x + 2, y + 1, C(FIRE[2]))
        if streaks > 0 and not FLAT:                                           # rayas de velocidad del picado
            for k, yy in enumerate((12, 17, 22)):
                for xx in range(2 + k * 2, 2 + k * 2 + int(6 * streaks)):
                    blend(cv, xx, yy + (k % 2), WING[2], 0.6)
    return cv

def flyer_shadow(i=0, w=24, h=8):
    """Sombra de un volador en el suelo (la mueve el código; más pequeña cuanto más alto)."""
    cv = Canvas(w, h)
    for y in range(h):
        for x in range(w):
            d = ((x + .5 - w / 2) / (w / 2 - 1)) ** 2 + ((y + .5 - h / 2) / (h / 2 - 0.5)) ** 2
            if d <= 1: cv.put(x, y, (14, 12, 18, int(130 * (1 - d * 0.5))))
    return cv

def aim_line(i):
    """Tramo de la línea de aviso del picado (8×4, se repite a lo largo del recorrido): rayas rojas que avanzan."""
    cv = Canvas(8, 4)
    for x in range(8):
        if (x + i * 2) % 8 < 4:
            cv.put(x, 1, C('#e04a3a', 220)); cv.put(x, 2, C('#a82a24', 200))
    return cv

def dragon_anims():
    global FLAT
    a = {}
    fl = [math.sin(i / 4 * 2 * math.pi) for i in range(4)]
    a['Dragonfly_Fly'] = [dragon(flap=f, bend=0.15 * f) for f in fl]
    a['Dragonfly_Warn'] = [dragon(flap=f * 0.5, warn=w, pitch=-8, bend=-0.3) for f, w in zip(fl, (0.4, 0.7, 1.0, 1.0))]
    a['Dragonfly_Dive'] = [dragon(flap=-0.3 + 0.1 * k, pitch=30, swept=1.0, bend=0.2, streaks=1.0 - k * 0.2) for k in range(3)]
    a['Dragonfly_Perch'] = [dragon(perch=1.0, flap=0.05 * f, bend=-0.1 * f) for f in fl]
    a['Dragonfly_Land'] = [dragon(flap=0.8, perch=0.4), dragon(flap=-0.3, perch=0.8), dragon(perch=1.0)]
    FLAT = WHITE5; w = dragon(); FLAT = RED5; r = dragon(); FLAT = None
    a['Dragonfly_Hurt'] = [w, r]
    a['Dragonfly_Death'] = [dragon(eye_state='x', flap=-0.8, crumple=0.2, pitch=15),
                            dragon(eye_state='x', flap=0.6, crumple=0.5, pitch=35, curl=0.3),
                            dragon(eye_state='x', crumple=0.8, pitch=20, curl=0.6, fade=0.2, perch=1.0),
                            dragon(eye_state='x', crumple=1.0, curl=0.8, fade=0.5, perch=1.0),
                            dragon(eye_state='x', crumple=1.0, curl=1.0, fade=1.0, perch=1.0)]
    return a

# --- enjambre de efímeras (una pieza) ------------------------------------------------------------
SWARM = [(0.9, 11, 7, 0.0), (1.3, 8, 10, 1.1), (1.1, 13, 5, 2.3), (0.7, 6, 8, 3.0), (1.5, 10, 9, 4.2),
         (1.2, 4, 4, 5.1), (0.8, 12, 9, 0.6), (1.4, 7, 6, 2.8), (1.0, 9, 11, 3.7), (1.6, 5, 7, 4.8),
         (0.6, 14, 8, 1.9), (1.1, 3, 6, 5.6)]                                 # (velocidad, radio x, radio y, fase)

def mayfly(cv, x, y, flap, face, col=None, a=1.0, wing=True):
    """Efímera de ~5 px: cuerpo crema, dos colas largas y alas en vela (arriba/abajo con flap)."""
    body = [C(c) for c in (col or MAYFLY)]
    x, y = int(round(x)), int(round(y))
    s = 1 if face > 0 else -1
    cv.put(x, y, body[2]); cv.put(x - s, y, body[1]); cv.put(x - 2 * s, y + 1, body[1])     # cabeza, tórax, abdomen
    cv.put(x - 3 * s, y + 1, body[0]); cv.put(x - 4 * s, y + 2, body[0])                  # colas
    if col is None: cv.put(x - 4 * s, y, body[0])
    if wing:
        wy = -2 if flap else -1
        blend(cv, x - s, y + wy, '#f4fafa', 0.9 * a); blend(cv, x - s, y + wy + 1, '#ffffff', 0.7 * a)
        if flap: blend(cv, x - 2 * s, y - 2, '#eef6f6', 0.5 * a)

def swarm(t, burn=0.0, n=40):
    """Nube de 12 efímeras revoloteando alrededor del centro (bucle de 6 cuadros, t = 0..1). Con burn 0..1 se
    queman: se vuelven ascuas que caen dejando humo."""
    import random
    cv = Canvas(n)
    c = n / 2
    haze = 1 - burn
    for y in range(n):                                                        # neblina de la nube
        for x in range(n):
            d = ((x + .5 - c) / 16) ** 2 + ((y + .5 - c + 2) / 10.5) ** 2
            if d <= 1 and haze > 0:
                cv.put(x, y, (232, 228, 196, int(34 * haze * (1 - d) ** 0.7 + (1 if d < 1 else 0))))
    for i, (sp, rx, ry, ph) in enumerate(SWARM):
        ang = t * 2 * math.pi * round(sp + 0.5) + ph
        x = c + math.cos(ang) * rx * 0.9
        y = c - 2 + math.sin(ang * (1 if i % 2 else -1)) * ry * 0.6 + math.sin(t * 2 * math.pi * 2 + ph) * 1.2
        face = -math.sin(ang) * (1 if i % 2 else -1) or 1
        if burn <= 0:
            mayfly(cv, x, y, (i + int(t * 6)) % 2, face)
            continue
        lit = burn * 1.6 - i / 12 * 0.6                                     # cada una prende un poco después
        if lit <= 0: mayfly(cv, x, y, (i + int(t * 6)) % 2, face); continue
        fall = min(1, lit) ** 2 * 14
        y2 = y + fall
        if lit < 1:
            mayfly(cv, x, y2, 0, face, col=[FIRE[1], FIRE[3], FIRE[4]], wing=False)
            cv.put(x, y2 - 1, C(FIRE[2]));
            for k in range(2): blend(cv, x + (k - 0.5) * 2, y2 - 2 - k * 2, '#6a6466', 0.5)
        elif lit < 1.4 and y2 < n - 1:
            cv.put(x, y2, C(FIRE[1])); blend(cv, x, y2 - 2, '#6a6466', 0.35)
    return cv

def swarm_anims():
    return {'Mayflies_Idle': [swarm(i / 6) for i in range(6)],
            'Mayflies_Burn': [swarm(0.1 + i * 0.05, burn=b) for i, b in enumerate((0.15, 0.35, 0.55, 0.75, 0.9, 1.0))]}

def flyers():
    a = {}
    a.update(dragon_anims()); a.update(swarm_anims())
    write(a, 'voladores')
    fondos(os.path.join(OUT, 'fondos_voladores.png'), a, ['Dragonfly_Fly', 'Dragonfly_Perch', 'Mayflies_Idle'])
    write({'Fx_FlyerShadow': [flyer_shadow()], 'Fx_DragonflyAim': [aim_line(i) for i in range(4)]},
          'efectos_voladores', fx=True)

# =================================================================================================
# Salida
# =================================================================================================
MS = {'Fly': 50, 'Dive': 60, 'Perch': 200, 'Land': 90, 'Burn': 90, 'Aim': 80, 'Turn': 70, 'Warn': 150, 'Spray': 70, 'Hidden': 200, 'Reveal': 70, 'Jump': 80, 'Hop': 90, 'Spit': 90,
      'Inflate': 120, 'Dive': 90, 'Swim': 160, 'Appear': 60, 'Burn': 80, 'Splat': 70, 'BeetleSpray': 60, 'Idle': 180, 'Spawn': 110, 'Spores': 110, 'Hurt': 90, 'Death': 120, 'Emerge': 90, 'Move': 100,
      'Wither': 140, 'Aim': 150, 'Shoot': 70, 'Snap': 50, 'Chew': 130, 'Open': 110, 'Wilt': 160,
      'Proj': 90, 'Pop': 70, 'Target': 120, 'Burst': 70}

def ms_of(name):
    for k, v in MS.items():
        if f'_{k}' in name or name.startswith(k): return v
    return 100

def fondos(path, anims, keys, scale=3):
    """Mismos cuadros sobre los tres tramos del incendio (prueba de legibilidad)."""
    frames = [as_img(anims[k][0]) for k in keys]
    w = max(f.width for f in frames) + 4; h = max(f.height for f in frames) + 4
    im = Image.new('RGBA', (w * len(frames), h * len(TRAMOS)), (0, 0, 0, 255))
    for r, col in enumerate(TRAMOS.values()):
        for i, f in enumerate(frames):
            cell = Image.new('RGBA', (w, h), C(col)); cell.alpha_composite(f, ((w - f.width) // 2, (h - f.height) // 2))
            im.paste(cell, (i * w, r * h))
    im.resize((im.width * scale, im.height * scale), Image.NEAREST).save(path)

def write(anims, tag, sub='', fx=False):
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    for name, frames in anims.items():
        folder = 'Effects' if fx else name.split('_')[0]
        save_sheet(os.path.join(OUT, folder, name + '.png'), frames)
        gif(os.path.join(OUT, 'gif', name + '.gif'), frames, ms_of(name), BG, scale=4)
    preview(os.path.join(OUT, f'vista_previa_{tag}.png'), anims, BG, scale=3)

def plants():
    a = {}
    a.update(calamites_anims()); a.update(medullosa_anims()); a.update(carnivore_anims())
    write(a, 'plantas')
    fondos(os.path.join(OUT, 'fondos_plantas.png'), a,
           ['Calamites_Idle', 'CalamitesSprout_Idle', 'Medullosa_Idle', 'CarnivorousPlant_Idle'])
    fx = {'Proj_CalamitesSpore': spore_proj(), 'Fx_CalamitesSpore_Pop': spore_pop(),
          'Proj_MedullosaSeed': [seed_proj(i) for i in range(4)],
          'Fx_MedullosaSeed_Target': [seed_target(i) for i in range(4)],
          'Fx_MedullosaSeed_Burst': [seed_burst(i) for i in range(5)]}
    write(fx, 'efectos_plantas', fx=True)

if __name__ == '__main__':
    which = [x for x in sys.argv[1:] if x.isalpha()] or ['plants']
    if 'plants' in which: plants()
    if 'ground' in which: ground()
    if 'flyers' in which: flyers()
    print('Listo:', OUT)
