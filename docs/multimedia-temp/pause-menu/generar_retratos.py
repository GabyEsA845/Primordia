"""Retratos del bestiario (24×24) por tandas: piso 1 → piso 2 → piso 3 (+ trampas y plantas) → jefes.

Reglas (decididas con la usuaria, 2026-11-26): cuerpo entero en los pequeños y primer plano en los grandes y los jefes;
fondo del hábitat de su piso (apagado) con las esquinas redondeadas (radio 3) para caber en la casilla; la criatura
en tres cuartos mirando a la derecha con los ojos enfadados; jefes con un borde fino dorado y trampas o plantas con
uno verdoso. Cada retrato sale también en versión bloqueada (fondo apagado + silueta plana).
Nombres: Portrait_<nombre del EnemyData sin "Enemy_">.png y _Locked.png, en Bestiary/.
Hecho con la skill primordia-sprites (pixel.py): luz arriba a la izquierda, contorno OUTL.

Uso: python generar_retratos.py [carpeta_salida]
"""
import math, os, sys, random
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from generar_pausa_bestiario import *
import generar_pausa_bestiario as B

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__))
PS = 24

# --- fondo del hábitat ------------------------------------------------------------------------------
SOUP = ['#173a30', '#1d4639', '#245244', '#2e6150']

def habitat_soup(seed):
    """Sopa primordial: suelo verde con la luz arriba a la izquierda, motas y un par de burbujas."""
    rnd = random.Random(seed)
    cv = Canvas(PS)
    for y in range(PS):
        for x in range(PS):
            t = (x * 0.55 + y * 0.85) / (PS * 1.4)
            cv.put(x, y, A(SOUP[2] if t < 0.32 else SOUP[1] if t < 0.7 else SOUP[0]))
    for _ in range(7):
        x, y = rnd.randrange(PS), rnd.randrange(PS)
        cv.put(x, y, A(SOUP[3]))
    for x, y, r in ((rnd.randrange(3, 8), rnd.randrange(3, 9), 1.4), (rnd.randrange(16, 21), rnd.randrange(15, 21), 1.0)):
        for yy in range(PS):
            for xx in range(PS):
                dd = math.hypot(xx + 0.5 - x, yy + 0.5 - y)
                if r - 0.8 < dd < r + 0.4: cv.put(xx, yy, A(SOUP[3]))
        cv.put(x - 1, y - 1, A('#8fd0b0', 200))
    return cv


# --- ojos enfadados (2×3, pupila mirando a la derecha) ----------------------------------------------
def angry_eye(cv, x, y, tall=3):
    for dy in range(tall):
        cv.put(x, y + dy, EYE); cv.put(x + 1, y + dy, EYE)
    cv.put(x + 1, y + tall - 1, OUTL); cv.put(x + 1, y + tall - 2, OUTL)     # pupila
    cv.put(x, y, OUTL)                                                       # párpado cortado (enfadado)
    cv.put(x - 1, y - 1, OUTL)                                               # ceja


# --- piso 1 -----------------------------------------------------------------------------------------
BACT = ['#8a3a2a', '#d0603a', '#f2934e', '#ffd08a']
AMEBA = ['#5a3a8c', '#9a5fd0', '#c995f0', '#f0d8ff']

def creature_chaser():
    """Bacteria que persigue: cápsula naranja algo inclinada, flagelo ondulado detrás y cilios."""
    cv = Canvas(PS)
    rot_ellipse(cv, 13.5, 12.5, 8.0, 4.8, math.radians(-12), BACT)
    cv.outline(); cv.clean()
    for x, y in ((9, 11), (12, 14), (15, 10)): cv.put_in(x, y, A(BACT[0]))   # gránulos
    cv.put_in(8, 9, A(BACT[3]))
    angry_eye(cv, 16, 10); angry_eye(cv, 19, 9)
    for i in range(9):                                                       # flagelo (fino, sin contorno)
        x, y = 6 - i * 0.7, 14 + math.sin(i * 1.1) * 1.6 + i * 0.3
        cv.put_out(x, y, A(BACT[0] if i < 6 else BACT[1])); cv.put_out(x, y + 1, A(OUTL[:3] + (150,)) if i < 5 else A(BACT[1], 160))
    for x, y in ((9, 17), (13, 18), (17, 16)): cv.put_out(x, y, A(BACT[2]))  # cilios
    return cv


def _blob(cv, parts, ramp):
    """Unión de elipses sombreada como una sola masa."""
    def inside(x, y):
        return any(((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1 for cx, cy, rx, ry in parts)
    xs = [p[0] for p in parts]; ys = [p[1] for p in parts]
    cx, cy = sum(xs) / len(xs), sum(ys) / len(ys)
    rx = max(p[0] + p[2] for p in parts) - cx; ry = max(p[1] + p[3] for p in parts) - cy
    fill(cv, inside, cx, cy, rx, ry, ramp)

def creature_shooter():
    """Ameba que dispara y se divide: masa morada con seudópodos, vacuolas y un núcleo visible."""
    cv = Canvas(PS)
    _blob(cv, [(12.5, 13.5, 7.4, 6.6), (4.8, 15.8, 2.8, 2.4), (17.6, 7.6, 2.6, 2.4), (14.0, 19.6, 3.4, 2.2)], AMEBA)
    cv.outline(); cv.clean()
    ellipse(cv, 9.5, 15.5, 2.4, 2.0, [AMEBA[0], AMEBA[0], AMEBA[1], AMEBA[1]])   # núcleo
    cv.put_in(9, 15, A(AMEBA[2]))
    for x, y in ((14, 17), (6, 13)): cv.put_in(x, y, A(AMEBA[3], 220))           # vacuolas
    angry_eye(cv, 13, 10); angry_eye(cv, 17, 10)
    cv.put_in(8, 9, A(AMEBA[3])); cv.put_in(9, 8, A(AMEBA[3]))
    return cv

def creature_small():
    """Amebita: la cría de la ameba, pequeña y redonda (deja ver más fondo: es pequeña)."""
    cv = Canvas(PS)
    _blob(cv, [(12.5, 14.0, 5.4, 4.8), (17.0, 11.0, 2.2, 2.0), (8.0, 16.5, 2.0, 1.8)], AMEBA)
    cv.outline(); cv.clean()
    angry_eye(cv, 13, 12); angry_eye(cv, 16, 12)
    cv.put_in(10, 11, A(AMEBA[3]))
    for i, (x, y) in enumerate(((4, 9), (20, 19))): cv.put(x, y, A(AMEBA[2], 170))   # gotitas sueltas
    return cv


# --- piso 2: hábitats por franja ----------------------------------------------------------------------
SHALLOWS = ['#1e6670', '#23737a', '#2a8286', '#379694']
REEF = ['#16344a', '#1b3d55', '#214862', '#2a5772']
ABYSS = ['#070b16', '#0b1222', '#0f182c', '#141e34']

def habitat(ramp, seed, flecks=(), caustics=False, sand=False):
    rnd = random.Random(seed)
    cv = Canvas(PS)
    for y in range(PS):
        for x in range(PS):
            t = (x * 0.55 + y * 0.85) / (PS * 1.4)
            cv.put(x, y, A(ramp[2] if t < 0.32 else ramp[1] if t < 0.7 else ramp[0]))
    if caustics:                                                     # redes de luz del agua somera
        for k in range(3):
            y0 = rnd.randrange(2, 20)
            for x in range(PS):
                y = y0 + math.sin(x * 0.7 + k * 2) * 1.5
                if (x + k) % 5: cv.put(x, y, A(ramp[3]))
    for color, n in flecks:
        for _ in range(n): cv.put(rnd.randrange(PS), rnd.randrange(PS), A(color))
    if sand:                                                         # montículo de arena de la madriguera
        for y in range(16, PS):
            for x in range(PS):
                if ((x + 0.5 - 12) / 11) ** 2 + ((y + 0.5 - 24) / 7) ** 2 <= 1:
                    cv.put(x, y, A(SAND[1] if y > 19 else SAND[2]))
    return cv

TRILO = ['#4a2418', '#8a4a2a', '#c47a42', '#f2b674']
STEEL = ['#22243a', '#40445c', '#6a7090', '#a4aac4']
PIKAIA = ['#2e4a44', '#5e8a7a', '#9ccaa8', '#e4fff0']
OTTOIA = ['#5a1e2e', '#a43e52', '#e07a84', '#ffc8c4']
VAUXIA = ['#5a4012', '#9a7424', '#d4aa44', '#fbe58c']
JELLY = ['#1a3478', '#2f62c4', '#6aa8ee', '#c8ecff']

def creature_trilobite():
    """Trilobite de cuerpo entero: cuerpo de óxido con segmentos, céfalo de acero a la derecha, patas y antenas."""
    cv = Canvas(PS)
    ellipse(cv, 10.5, 12.5, 8.2, 6.4, TRILO)
    ellipse(cv, 17.5, 12.5, 4.4, 6.0, STEEL)
    cv.outline(); cv.clean()
    for x in range(5, 15, 2):                                        # surcos de los segmentos
        for y in range(7, 19): cv.put_in(x, y, A(TRILO[0]))
    for x in range(4, 15): cv.put_in(x, 11, A(TRILO[3])); cv.put_in(x, 12, A(TRILO[2]))   # lóbulo central
    angry_eye(cv, 18, 9)
    for i in range(5):                                               # patas (finas, sin contorno)
        cv.put_out(6 + i * 2, 19 + (i % 2), A(TRILO[3])); cv.put_out(5 + i * 2, 20 + (i % 2), A(TRILO[2]))
    for i in range(4):                                               # antenas
        cv.put_out(21 + i * 0.6, 7 - i, A(TRILO[3])); cv.put_out(22 + i * 0.5, 16 + i * 0.6, A(TRILO[3]))
    return cv

def creature_pikaia():
    """Pikaia de cuerpo entero: pez alargado y fino, franja oscura en el lomo, miómeros en V y aleta de la cola."""
    cv = Canvas(PS)
    def inside(x, y):
        px, py = x + 0.5, y + 0.5
        if not 2.5 <= px <= 21: return False
        u = (px - 2.5) / 18.5                                        # 0 = cola, 1 = cabeza
        k = 1 - ((u - 0.62) / 0.42) ** 2
        half = max(0.9, 3.3 * math.sqrt(k)) if k > 0 else 0.9        # cuerpo ancho delante y cabeza roma
        if u < 0.14: half = 0.9 + 2.6 * (0.14 - u) / 0.14             # aleta de la cola
        return abs(py - 12.5 + (px - 12) * 0.1) <= half
    fill(cv, inside, 12, 12.5, 9, 3.5, PIKAIA)
    cv.outline(); cv.clean()
    for x in range(4, 20):                                           # franja del lomo (notocordio)
        cv.put_in(x, 11 - (x - 12) * 0.1, A(PIKAIA[1]))
    for x in range(6, 18, 2):                                        # miómeros en V
        cv.put_in(x, 13 - (x - 12) * 0.1, A(PIKAIA[1])); cv.put_in(x + 1, 14 - (x - 12) * 0.1, A(PIKAIA[1]))
    angry_eye(cv, 17, 10)
    for i in range(3):                                               # tentáculos de la cabeza
        cv.put_out(21 + i, 11 + i * 0.4, A(PIKAIA[3])); cv.put_out(21 + i, 13 + i * 0.4, A(PIKAIA[2]))
    return cv

def creature_ottoia():
    """Ottoia en primer plano: el gusano saliendo de la madriguera, con la trompa erizada de ganchos."""
    cv = Canvas(PS)
    def inside(x, y):
        px, py = x + 0.5, y + 0.5
        cx = 12 + math.sin(py * 0.25) * 1.2
        w = 4.6 if py > 8 else 4.6 - (8 - py) * 0.35
        return abs(px - cx) <= w and 3 <= py <= 21
    fill(cv, inside, 12, 12, 5, 9, OTTOIA)
    cv.outline(); cv.clean()
    for y in range(9, 21, 3):                                        # anillos
        for x in range(6, 18): cv.put_in(x, y, A(OTTOIA[0]))
    for x in range(9, 16, 2): cv.put_out(x, 2, A(OTTOIA[3])); cv.put_out(x - 1, 3, A(OTTOIA[2]))   # ganchos
    angry_eye(cv, 11, 6); angry_eye(cv, 14, 6)
    cv.put_in(8, 9, A(OTTOIA[3]))
    return cv

def creature_vauxia():
    """Vauxia en primer plano: esponja dorada de tres tubos separados, con la boca abierta arriba, poros y ojos."""
    cv = Canvas(PS)
    tubes = ((5.0, 9.0, 2.9, -0.18), (18.8, 7.5, 2.9, 0.16), (12.0, 2.5, 3.7, 0.0))   # (x de la boca, arriba, ancho, inclinación)
    for cx, top, w, lean in tubes:
        t = Canvas(PS)
        def inside(x, y, cx=cx, top=top, w=w, lean=lean):
            px, py = x + 0.5, y + 0.5
            c = cx + (py - top) * -lean
            ww = w * (0.85 + 0.15 * (py - top) / (22 - top))
            if py < top + w:                                          # boca redondeada
                return ((px - c) / ww) ** 2 + ((py - top - w) / w) ** 2 <= 1
            return abs(px - c) <= ww and py <= 22
        fill(t, inside, cx, (top + 22) / 2, w, (22 - top) / 2, VAUXIA)
        t.outline(); t.clean()
        for dx in (-1, 0, 1): t.put_in(round(cx) + dx, round(top + 1.5), A(VAUXIA[0]))
        t.put_in(round(cx), round(top + 2.5), A(VAUXIA[0]))
        cv.px.update(t.px)
    rnd = random.Random(5)
    for _ in range(16):
        x, y = rnd.randrange(3, 22), rnd.randrange(9, 21)
        if (x, y) in cv.px and cv.px[(x, y)] != OUTL: cv.put(x, y, A(VAUXIA[1]))   # poros
    angry_eye(cv, 10, 11); angry_eye(cv, 13, 11)
    return cv

def creature_bluejelly():
    """Medusa azul de cuerpo entero: campana translúcida, tentáculos y puntos que brillan."""
    cv = Canvas(PS)
    def inside(x, y):
        px, py = x + 0.5, y + 0.5
        return ((px - 12) / 8) ** 2 + ((py - 11) / 7) ** 2 <= 1 and py <= 13.5
    fill(cv, inside, 12, 9, 8, 6, JELLY, alpha=215)
    cv.outline(OUTL, 100); cv.clean()
    for x in range(5, 20): cv.put_in(x, 13, A(JELLY[0], 230))       # borde de la campana
    for i, x in enumerate(range(6, 19, 3)):                          # tentáculos
        for y in range(15, 22):
            cv.put(x + round(math.sin(y * 0.8 + i) * 0.8), y, A(JELLY[2], 200 - (y - 15) * 18))
    angry_eye(cv, 11, 8); angry_eye(cv, 14, 8)
    for x, y in ((7, 8), (17, 9), (9, 12)): cv.put_in(x, y, A('#7ae8ff'))   # bioluminiscencia
    cv.put_in(8, 6, A(JELLY[3]))
    return cv


# --- piso 3 ------------------------------------------------------------------------------------------
SWAMP = ['#1c2e20', '#213626', '#273f2b', '#2f4a32']
MUD3 = ['#22160f', '#33221a', '#4a3324', '#634830']
MURK = ['#16302e', '#1d4440', '#2a5e54', '#4e8a72']
SMOKY = ['#22261f', '#2a2d25', '#32352b', '#3a3d31']

BEETLE = ['#121834', '#1f2c62', '#33509a', '#6f92d8']
SPIDER = ['#2a3014', '#4a5420', '#738232', '#a8b860']
TOAD = ['#3a2414', '#6a4426', '#9a6a3c', '#c8a070']
DRAGON = ['#0e4a5a', '#1e8aa0', '#4ac8d8', '#b0f4f8']
NYMPH = ['#2e2214', '#54402a', '#7e6440', '#a88c5c']
CALA = ['#2a4a1c', '#4a7a2a', '#7aac44', '#b8dc80']
BARK = ['#1e2420', '#2e3830', '#435040', '#5e6c52']
FERN3 = ['#1c3a20', '#2a5a2a', '#3e8a34', '#6abf48']
SEED = ['#6a1a14', '#b03024', '#e05a3a', '#ffa080']
MAW = ['#5a1420', '#a02838', '#d84a58', '#ff9aa0']
MAYF = ['#8a8870', '#c8c4a0', '#ece8cc', '#fffff0']

def _part(cv, fn):
    """Dibuja una pieza con su propio contorno y la pega encima (así cada pieza se separa de la de atrás)."""
    t = Canvas(PS); fn(t); t.outline(); t.clean(); cv.px.update(t.px)

def creature_beetle():
    """Escarabajo en primer plano: élitros azules en cúpula, cabeza con el ojo rojo encendido y mandíbulas."""
    cv = Canvas(PS)
    _part(cv, lambda t: ellipse(t, 9.5, 13.5, 10.0, 7.4, BEETLE))
    _part(cv, lambda t: ellipse(t, 19.5, 14.0, 4.4, 4.2, BEETLE))
    for x in range(0, 19): cv.put_in(x, 13 - (x - 9) * 0.05, A(BEETLE[0]))        # costura de los élitros
    for x, y in ((4, 9), (5, 8), (6, 8), (7, 8)): cv.put_in(x, y, A(BEETLE[3]))      # brillo
    cv.put_in(19, 12, A('#e8582a')); cv.put_in(20, 12, A('#ffd25a')); cv.put_in(20, 13, A('#e8582a'))   # ojo
    cv.put_in(18, 11, OUTL); cv.put_in(19, 11, OUTL)                                 # ceño
    for i in range(3): cv.put_out(23, 15 + i, A(BEETLE[3])); cv.put_out(22 + i * 0.4, 18 + i * 0.3, A(BEETLE[2]))  # mandíbulas
    for i in range(4): cv.put_out(21 + i * 0.7, 10 - i, A(BEETLE[3]))               # antena
    for x in (5, 10, 15): cv.put_out(x, 21, A(BEETLE[2])); cv.put_out(x + 1, 22, A(BEETLE[2]))   # patas
    return cv

def creature_spider():
    """Araña en primer plano: abdomen oliva, cefalotórax con un racimo de ojos rojos y patas largas."""
    cv = Canvas(PS)
    for i, (ax, ay) in enumerate(((2, 4), (1, 13), (4, 22), (12, 23), (21, 22), (23, 6), (17, 2), (8, 1))):
        for k in range(10):                                                         # patas (finas, sin contorno)
            t = k / 9
            x, y = 13 + (ax - 13) * t, 13 + (ay - 13) * t - math.sin(t * math.pi) * 2.5
            cv.put(x, y, A('#141808')); cv.put(x, y - 1, A(SPIDER[3] if k % 3 else SPIDER[2]))
    _part(cv, lambda t: ellipse(t, 8.5, 14.0, 6.6, 5.8, SPIDER))
    _part(cv, lambda t: ellipse(t, 16.0, 12.5, 4.6, 4.0, SPIDER))
    for x, y in ((6, 11), (9, 13), (7, 16)): cv.put_in(x, y, A(SPIDER[0]))         # dibujo del abdomen
    for x, y in ((17, 10), (19, 11), (16, 11)): cv.put_in(x, y, A('#e0402a'))      # ojitos
    cv.put_in(18, 12, A('#ff8a5a')); cv.put_in(19, 12, A('#e0402a')); cv.put_in(18, 11, OUTL)
    return cv

def creature_toad():
    """Sapo en primer plano: cuerpo pardo con verrugas, ojo amarillo de pupila rasgada y boca ancha."""
    cv = Canvas(PS)
    _part(cv, lambda t: ellipse(t, 11.0, 15.0, 10.5, 6.8, TOAD))
    _part(cv, lambda t: ellipse(t, 17.0, 10.5, 3.2, 3.0, TOAD))                    # bulto del ojo
    rnd = random.Random(31)
    for _ in range(12):
        x, y = rnd.randrange(2, 20), rnd.randrange(10, 20)
        cv.put_in(x, y, A(TOAD[3] if rnd.random() < 0.5 else TOAD[0]))              # verrugas
    for x in range(10, 22): cv.put_in(x, 17 + (x > 18), OUTL)                      # boca
    for x in range(4, 20): cv.put_in(x, 20, A('#d8c090'))                          # vientre
    cv.put_in(16, 9, A('#ffd25a')); cv.put_in(17, 9, A('#ffd25a')); cv.put_in(16, 10, A('#e8a020'))
    cv.put_in(17, 10, OUTL); cv.put_in(17, 11, A('#e8a020')); cv.put_in(15, 8, OUTL)   # ojo y ceño
    return cv

def creature_dragonfly():
    """Libélula en primer plano: ojos compuestos enormes, tórax, alas translúcidas y abdomen que sale del cuadro."""
    cv = Canvas(PS)
    for (x0, y0, x1, y1) in ((14, 9, 2, 2), (14, 14, 1, 21), (13, 9, 9, 0), (13, 14, 8, 23)):   # alas
        for k in range(14):
            t = k / 13
            for w in (-1, 0, 1):
                cv.put(x0 + (x1 - x0) * t + w * 0.4, y0 + (y1 - y0) * t + w, A('#c8ecff', 90 if w else 130))
    for x in range(0, 13): cv.put(x, 12 + (x < 6), A(DRAGON[1])); cv.put(x, 11 + (x < 6), A(DRAGON[2]))   # abdomen
    _part(cv, lambda t: ellipse(t, 14.5, 11.5, 3.6, 3.0, DRAGON))
    _part(cv, lambda t: ellipse(t, 19.5, 11.0, 3.8, 3.6, DRAGON))
    for x, y in ((18, 9), (19, 9), (20, 10), (18, 10)): cv.put_in(x, y, A(DRAGON[3]))   # brillo de los ojos
    cv.put_in(20, 11, EYE); cv.put_in(21, 11, OUTL); cv.put_in(20, 12, OUTL); cv.put_in(19, 10, OUTL)
    return cv

def creature_nymph():
    """Ninfa de Meganeura en primer plano: larva parda con placas azules, ojos grandes y la máscara bajo la cabeza."""
    cv = Canvas(PS)
    for (sx, sy, ex, ey) in ((8, 15, 3, 21), (12, 16, 10, 22), (15, 16, 19, 22), (9, 9, 4, 3), (13, 9, 14, 2)):
        for k in range(6): cv.put(sx + (ex - sx) * k / 5, sy + (ey - sy) * k / 5, A(NYMPH[2]))   # patas
    _part(cv, lambda t: ellipse(t, 7.5, 13.0, 7.8, 4.8, NYMPH))
    _part(cv, lambda t: ellipse(t, 17.5, 12.5, 4.6, 4.2, NYMPH))
    for x in range(2, 14, 3):
        for y in range(10, 13): cv.put_in(x, y, A('#2a90b8')); cv.put_in(x + 1, y, A('#6ad0ec'))   # placas del lomo
    angry_eye(cv, 17, 10); angry_eye(cv, 20, 10)
    for x in range(17, 23): cv.put_out(x, 17, A(NYMPH[3]))                         # máscara (labio)
    return cv

def creature_calamites():
    """Calamites en primer plano: la base con cara y los tallos segmentados que suben, con las puntas claras."""
    cv = Canvas(PS)
    for x, top in ((5, 3), (10, 0), (15, 2), (19, 6)):
        def stalk(t, x=x, top=top):
            for y in range(top, 16):
                for dx in (0, 1, 2): t.put(x + dx, y, A(CALA[3] if dx == 0 else CALA[2] if dx == 1 else CALA[1]))
        _part(cv, stalk)
        for y in range(top + 3, 16, 4):
            for dx in (0, 1, 2): cv.put_in(x + dx, y, A(CALA[0]))                    # nudos
        for dx in (0, 1, 2): cv.put_in(x + dx, top, A('#e8e0c0'))                    # punta
        cv.put_out(x - 1, top + 5, A(CALA[2])); cv.put_out(x + 3, top + 9, A(CALA[2]))   # ramillas
    _part(cv, lambda t: ellipse(t, 12, 18.5, 9.5, 4.6, FERN3))
    angry_eye(cv, 10, 16); angry_eye(cv, 14, 16)
    return cv

def creature_sprout():
    """Brote de Calamites de cuerpo entero: un tallito con su base con cara."""
    cv = Canvas(PS)
    def stalk(t):
        for y in range(3, 15):
            for dx in (0, 1, 2): t.put(11 + dx, y, A(CALA[3] if dx == 0 else CALA[2] if dx == 1 else CALA[1]))
    _part(cv, stalk)
    for y in (6, 10): [cv.put_in(11 + dx, y, A(CALA[0])) for dx in (0, 1, 2)]
    for dx in (0, 1, 2): cv.put_in(11 + dx, 3, A('#e8e0c0'))
    cv.put_out(10, 8, A(CALA[2])); cv.put_out(14, 11, A(CALA[2]))
    _part(cv, lambda t: ellipse(t, 12, 17.0, 5.6, 3.6, FERN3))
    angry_eye(cv, 10, 15); angry_eye(cv, 13, 15)
    for x, y in ((7, 21), (16, 21)): cv.put(x, y, A(FERN3[2]))                     # patitas
    return cv

def creature_medullosa():
    """Medullosa en primer plano: tronco de corteza con cara, copa de frondas delante y semillas rojas colgando."""
    cv = Canvas(PS)
    def trunk(t):
        for y in range(9, 24):
            w = 3.6 + (y - 9) * 0.14
            for x in range(int(12 - w), int(12 + w) + 1):
                f = 3 if x < 12 - w + 1.5 else 2 if x < 12 else 1 if x < 12 + w - 1 else 0
                t.put(x, y, A(BARK[f]))
    _part(cv, trunk)
    for y in range(13, 23, 3): cv.put_in(10 + (y % 2), y, A(BARK[0]))              # corteza
    for ang in (-165, -125, -90, -55, -15):                                         # copa de frondas (delante)
        a = math.radians(ang)
        for k in range(1, 12):
            x, y = 12 + math.cos(a) * k, 9 + math.sin(a) * k + k * k * 0.035
            cv.put(x, y, A(FERN3[2]))
            if k % 2: cv.put(x, y - 1, A(FERN3[3])); cv.put(x, y + 1, A(FERN3[1]))   # foliolos
    for x, y in ((3, 14), (21, 13), (6, 18), (18, 17)):                             # semillas
        _part(cv, lambda t, x=x, y=y: ellipse(t, x, y, 1.7, 1.9, SEED))
    angry_eye(cv, 10, 15); angry_eye(cv, 13, 15)
    return cv

def creature_carnivorous():
    """Planta carnívora: cabeza roja abierta con dientes y lengua, sobre hojas verdes."""
    cv = Canvas(PS)
    _part(cv, lambda t: [rot_ellipse(t, 6, 19, 5.5, 2.4, math.radians(-20), FERN3), rot_ellipse(t, 18, 19.5, 5.5, 2.4, math.radians(20), FERN3)])
    for y in range(12, 20): cv.put(11 + (y % 3 == 0), y, A(FERN3[2]))               # tallo
    _part(cv, lambda t: ellipse(t, 12.5, 8.5, 8.0, 6.8, MAW))
    for y in range(7, 13):                                                         # boca abierta
        for x in range(9, 21):
            if abs(y - 9.5) <= 2.2 * (x - 8) / 12 + 0.4: cv.put_in(x, y, A('#2a0810'))
    for x in range(10, 21, 2): cv.put_in(x, 8 - (x - 10) * 0.12, EYE); cv.put_in(x + 1, 11 + (x - 10) * 0.1, EYE)   # dientes
    for x, y in ((14, 10), (15, 10), (16, 11)): cv.put_in(x, y, A('#e07a84'))       # lengua
    for x, y in ((7, 5), (9, 4), (8, 6)): cv.put_in(x, y, A(MAW[3]))               # motas
    return cv

def creature_mayflies():
    """Efímeras: un enjambre de insectos pálidos con alas, sobre el agua con un halo."""
    cv = Canvas(PS)
    for y in range(PS):
        for x in range(PS):
            if math.hypot(x + 0.5 - 12, (y + 0.5 - 12) * 1.2) < 10: cv.put(x, y, A(MAYF[2], 40))
    for (x, y) in ((5, 7), (13, 5), (18, 10), (8, 14), (15, 17), (4, 18), (19, 3)):
        t = Canvas(PS)
        for k in range(4): t.put(x - k, y + k * 0.3, A(MAYF[1] if k else MAYF[3]))     # cuerpo y cola
        t.outline(OUTL, 100)
        t.put(x - 4, y + 1, A(MAYF[0])); t.put(x - 5, y + 1, A(MAYF[0]))               # cercos
        t.put(x - 1, y - 1, A('#ffffff', 170)); t.put(x - 1, y - 2, A('#ffffff', 120))   # alas
        cv.px.update(t.px)
    return cv


# --- jefes (primer plano, borde dorado, fondo de su sala) ---------------------------------------------
BOSSROOM1 = ['#2a1a1c', '#35201f', '#432826', '#553034']
BURNT = ['#1d1a1d', '#262124', '#2e282b', '#363033']
NEST = ['#141a14', '#1a221a', '#212b20', '#283426']

PINK = ['#8a3a5a', '#d06a8a', '#f0a0b8', '#ffd8e4']
VIRUS = ['#7a2a5a', '#c8487e', '#ff7aa8', '#ffd0e0']
LIME = ['#4a6a14', '#8ab828', '#c8e050', '#f0ffb0']
ANOMA = ['#5a1e14', '#a8402a', '#e0703e', '#ffb07a']
EURY = ['#1e3424', '#2e5a3a', '#4a8a5a', '#8ac08a']
ARTH = ['#3a2414', '#6a4424', '#9a6a3a', '#c89a62']
MEGAN = ['#0e3a44', '#1a6a78', '#3aa0aa', '#8ad8d8']
MEGRA = ['#241a24', '#3e2e3a', '#5e4a56', '#8a7280']
LEGS = ['#4a3a24', '#7a6040', '#a8885c', '#d4b886']
LEPI = ['#2a3428', '#41503c', '#5e7254', '#8aa07a']
MOSS = ['#1c3014', '#2e4a1e', '#4a6e2a', '#78a03e']
GLOW = ['#5a2a8a', '#9a5ad0', '#c8a0f0', '#f0e0ff']

def _mouth(cv, x0, x1, y, h=2):
    for x in range(x0, x1 + 1):
        for dy in range(h): cv.put_in(x, y + dy, A('#2a0810'))
    for x in range(x0, x1 + 1, 2): cv.put_in(x, y, EYE)
    for x in range(x0 + 1, x1 + 1, 2): cv.put_in(x, y + h - 1, EYE)

def boss_amoeba():
    """Ameba gigante: masa rosa que llena el cuadro, cejas fruncidas, boca abierta con dientes y vacuolas."""
    cv = Canvas(PS)
    _part(cv, lambda t: _blob(t, [(12.5, 13, 10.5, 9.5), (3, 17, 3, 3), (21, 6, 3, 3)], PINK))
    for x, y, r in ((6, 17, 1.6), (17, 18, 1.2), (5, 9, 1.0)): ellipse(cv, x, y, r, r, [PINK[1]] * 2 + [PINK[2]] * 2)
    angry_eye(cv, 10, 9); angry_eye(cv, 15, 9)
    _mouth(cv, 10, 16, 14, 3)
    cv.put_in(6, 5, A(PINK[3])); cv.put_in(7, 5, A(PINK[3]))
    return cv

def boss_virus():
    """Virus: icosaedro rosa con un gran ojo, boca dentada y espinas con bolas lima."""
    cv = Canvas(PS)
    for ang in (-150, -90, -30, 30, 150, 90):                                       # espinas
        a = math.radians(ang)
        for k in range(7, 11): cv.put(12 + math.cos(a) * k, 12 + math.sin(a) * k, A('#6a6a80'))
        _part(cv, lambda t, a=a: ellipse(t, 12 + math.cos(a) * 10.5, 12 + math.sin(a) * 10.5, 1.6, 1.6, LIME))
    def hexa(t):
        def inside(x, y):
            px, py = x + 0.5 - 12, y + 0.5 - 12
            return max(abs(px) * 0.866 + abs(py) * 0.5, abs(py)) <= 7.2
        fill(t, inside, 12, 12, 7.5, 7.5, VIRUS)
    _part(cv, hexa)
    for x, y in ((8, 8), (9, 8), (8, 9)): cv.put_in(x, y, A(VIRUS[3]))
    ellipse(cv, 12.5, 11, 3.2, 2.8, [EYE, EYE, EYE, EYE])                           # ojo
    cv.put(13, 11, A('#3a8a3a')); cv.put(13, 12, OUTL); cv.put(14, 11, OUTL)
    for x in range(9, 17): cv.put_in(x, 8, OUTL) if x > 10 and x < 15 else None     # párpado
    _mouth(cv, 10, 15, 15, 2)
    return cv

def boss_anomalocaris():
    """Anomalocaris: cabeza roja, ojos verdes en pedúnculos y los dos garfios rayados curvados hacia dentro."""
    cv = Canvas(PS)
    for sx in (-1, 1):                                                              # pedúnculos
        for k in range(4): cv.put(12 + sx * (4 + k * 0.8), 8 - k * 0.8, A(ANOMA[1]))
    _part(cv, lambda t: ellipse(t, 12, 10.5, 7.0, 5.0, ANOMA))
    for sx in (-1, 1):
        _part(cv, lambda t, sx=sx: ellipse(t, 12 + sx * 7.6, 4.4, 2.3, 2.3, ['#1a2a14', '#3a6a2a', '#7ac84a', '#d0ff9a']))
        cv.put_in(12 + sx * 7.6, 4.4, OUTL); cv.put_in(12 + sx * 7.6 + 1, 4.4, OUTL); cv.put_in(12 + sx * 7.6 - 1, 3.4, OUTL)
    for sx in (-1, 1):                                                              # garfios: arco grueso rayado
        g = Canvas(PS)
        for k in range(16):
            a = math.radians(-20 + k * 13)                                          # de arriba-fuera a abajo-dentro
            x = 12 + sx * (5.5 + math.cos(a) * 3.2)
            y = 15 + math.sin(a) * 4.2
            col = A('#f0d8b0') if (k // 2) % 2 else A(ANOMA[2])
            g.put(x, y, col); g.put(x + sx * 0.8, y, col)
            if k % 4 == 1: g.put(x - sx * 1, y - 1, A('#f0d8b0'))                    # espinas
        g.outline(); g.clean(); cv.px.update(g.px)
    ellipse(cv, 12, 13, 1.8, 1.4, [OUTL, '#3a1408', '#f0d8b0', '#f0d8b0'])           # boca redonda
    for x in range(8, 17, 2): cv.put_in(x, 8, A(ANOMA[3]))                           # placas
    for sx in (-1, 1): cv.put_in(12 + sx * 2, 9, OUTL)                               # ceño
    return cv

def boss_eurypterid():
    """Euriptérido: caparazón verde con ojos rojos, quelíceros y palas; la cola segmentada se alza detrás."""
    cv = Canvas(PS)
    for k in range(8):                                                              # cola alzada
        _part(cv, lambda t, k=k: ellipse(t, 19 - k * 0.6, 20 - k * 2.2, 2.4 - k * 0.15, 1.3, EURY))
    for sx in (-1, 1):
        _part(cv, lambda t, sx=sx: rot_ellipse(t, 12 + sx * 8.5, 16, 3.6, 1.6, math.radians(sx * 35), EURY))   # palas
    _part(cv, lambda t: ellipse(t, 12, 13, 7.2, 6.0, EURY))
    for x in range(6, 19): cv.put_in(x, 16, A(EURY[0]))
    for sx in (-1, 1):
        cv.put_in(12 + sx * 3, 11, A('#e02a2a')); cv.put_in(12 + sx * 3 + (sx > 0), 11, A('#ff8a6a'))
        cv.put_in(12 + sx * 3, 10, OUTL); cv.put_in(12 + sx * 2, 10, OUTL)           # ceño
    for sx in (-1, 1):
        for k in range(4): cv.put(12 + sx * (1 + k * 0.5), 19 + k, A(EURY[3]))      # quelíceros
    cv.put_in(9, 9, A(EURY[3])); cv.put_in(10, 8, A(EURY[3]))
    return cv

def boss_arthropleura():
    """Arthropleura: cabeza parda en tres cuartos con antenas; los segmentos y las patas se pierden a la izquierda."""
    cv = Canvas(PS)
    for k in range(4):
        x = 4 + k * 4
        for j in range(4): cv.put(x + j * 0.4, 19 + j, A(ARTH[3])); cv.put(x + 1 + j * 0.4, 6 - j, A(ARTH[3]))   # patas
    for k in range(4):                                                              # segmentos
        _part(cv, lambda t, k=k: ellipse(t, 3 + k * 4.4, 12.5, 3.4, 6.4, ARTH))
        cv.put_in(3 + k * 4.4, 8, A(ARTH[3])); cv.put_in(4 + k * 4.4, 8, A(ARTH[3]))
    _part(cv, lambda t: ellipse(t, 19.5, 12.5, 4.4, 5.0, ARTH))
    angry_eye(cv, 19, 10)
    for i in range(5): cv.put_out(22 + i * 0.5, 8 - i * 1.2, A(ARTH[3])); cv.put_out(22 + i * 0.4, 16 + i, A(ARTH[3]))   # antenas
    return cv

def boss_meganeura():
    """Meganeura: cabeza de frente con ojos compuestos rojos enormes, tórax turquesa y las alas abiertas."""
    cv = Canvas(PS)
    for (x1, y1) in ((0, 6), (0, 13), (23, 6), (23, 13)):                           # alas
        for k in range(12):
            t_ = k / 11
            for w in (-1, 0, 1):
                cv.put(12 + (x1 - 12) * t_, 10 + (y1 - 10) * t_ + w, A('#c8ecff', 80 if w else 120))
    for y in range(15, PS): cv.put(12, y, A(MEGAN[2])); cv.put(11, y, A(MEGAN[1]))     # abdomen
    _part(cv, lambda t: ellipse(t, 12, 11, 3.6, 3.4, MEGAN))
    for sx in (-1, 1):
        _part(cv, lambda t, sx=sx: ellipse(t, 12 + sx * 3.4, 6.5, 3.0, 2.8, ['#5a0e10', '#a01e1e', '#e04a3a', '#ff9a7a']))
    cv.put_in(9, 5, A('#ff9a7a')); cv.put_in(15, 5, A('#ff9a7a'))
    for sx in (-1, 1): cv.put_in(12 + sx * 2, 4, OUTL); cv.put_in(12 + sx * 1, 5, OUTL)   # ceño
    for x, y in ((9, 15), (15, 15), (8, 16), (16, 16)): cv.put(x, y, A('#b8d040'))   # patas
    return cv

def boss_megarachne():
    """Megarachne: cuerpo oscuro con púas rojas, racimo de ojos rojos y patas largas de color tostado."""
    cv = Canvas(PS)
    def seg(x0, y0, x1, y1):
        n = int(max(abs(x1 - x0), abs(y1 - y0)) * 2) + 1
        for i in range(n + 1):
            t_ = i / n; x, y = x0 + (x1 - x0) * t_, y0 + (y1 - y0) * t_
            cv.put(x, y + 1, A(LEGS[0])); cv.put(x, y, A(LEGS[3] if i % 4 else LEGS[2]))
    for sx in (-1, 1):
        for kx, ky, fx, fy in ((5, -9, 9, -11), (7, -6, 11, -2), (7, -1, 11, 6), (5, 4, 8, 10)):   # rodilla y pie
            seg(12 + sx * 2, 12, 12 + sx * kx, 12 + ky)
            seg(12 + sx * kx, 12 + ky, 12 + sx * fx, 12 + fy)
            cv.put(12 + sx * kx, 12 + ky - 1, A('#e02a2a'))                          # púa de la rodilla
    _part(cv, lambda t: ellipse(t, 12, 15, 5.2, 4.8, MEGRA))
    _part(cv, lambda t: ellipse(t, 12, 10, 4.0, 3.2, MEGRA))
    for x, y in ((10, 9), (14, 9), (11, 11), (13, 11), (12, 8)): cv.put_in(x, y, A('#e02a2a'))
    cv.put_in(10, 8, OUTL); cv.put_in(14, 8, OUTL)
    for x, y in ((9, 14), (15, 14), (12, 17), (10, 18), (14, 18)): cv.put_in(x, y, A('#e05a3a'))   # púas
    return cv

def boss_lepidodendron():
    """Lepidodendron: tronco con corteza en rombos, copa de musgo arriba, venas moradas que brillan y ojos."""
    cv = Canvas(PS)
    def trunk(t):
        for y in range(6, PS):
            w = 7 + (y - 6) * 0.25
            for x in range(int(12 - w), int(12 + w) + 1):
                f = 3 if x < 12 - w + 2 else 2 if x < 12 else 1 if x < 12 + w - 2 else 0
                t.put(x, y, A(LEPI[f]))
    _part(cv, trunk)
    for y in range(8, PS, 3):                                                       # rombos de la corteza
        for x in range(2, 23, 4): cv.put_in(x + (y // 3) % 2 * 2, y, A(LEPI[0])); cv.put_in(x + 1 + (y // 3) % 2 * 2, y + 1, A(LEPI[0]))
    for x0 in (7, 17):                                                              # venas moradas
        for y in range(10, PS):
            cv.put_in(x0 + math.sin(y * 0.6) * 1.2, y, A(GLOW[2]))
    _part(cv, lambda t: [ellipse(t, 12, 3.5, 11.5, 4.2, MOSS)])
    for x in range(1, 23, 2): cv.put_in(x, 6 + (x % 3 == 0), A(MOSS[0]))             # flecos del musgo
    for sx in (-1, 1):
        cv.put_in(12 + sx * 3, 12, A(GLOW[3])); cv.put_in(12 + sx * 3 + (1 if sx > 0 else -1), 12, A(GLOW[2]))
        cv.put_in(12 + sx * 2, 11, OUTL)
    return cv


# --- montaje -----------------------------------------------------------------------------------------
def rounded(im, r=3):
    p = im.load()
    for y in range(PS):
        for x in range(PS):
            dx = max(r - x - 0.5, x + 0.5 - (PS - r), 0); dy = max(r - y - 0.5, y + 0.5 - (PS - r), 0)
            if dx and dy and math.hypot(dx, dy) > r: p[x, y] = (0, 0, 0, 0)
    return im

FRAME = {'boss': ('#f2c24e', '#a8741c'), 'trap': ('#8ad860', '#4ea040')}

def portrait(bg, creature, kind=None):
    im = as_img(bg).copy(); im.alpha_composite(as_img(creature))
    if kind:                                                                  # borde fino de jefe o de trampa
        hi, lo = FRAME[kind]; p = im.load()
        for i in range(PS):
            for x, y in ((i, 0), (0, i)): p[x, y] = C(hi)
            for x, y in ((i, PS - 1), (PS - 1, i)): p[x, y] = C(lo)
    return rounded(im)

def locked(bg, creature):
    im = as_img(bg).copy(); p = im.load()
    for y in range(PS):
        for x in range(PS):
            r, g, b, a = p[x, y]; p[x, y] = (int(r * .55), int(g * .55), int(b * .55), a)
    cr = as_img(creature); q = cr.load()
    for y in range(PS):
        for x in range(PS):
            if q[x, y][3] > 60: p[x, y] = (9, 20, 17, 255)
    return rounded(im)


FLOOR2 = [
    ('Trilobite', 'TRILOBITE', lambda: habitat(REEF, 21, flecks=(('#d0705a', 4), (REEF[3], 5))), creature_trilobite,
     ['SUS OJOS ERAN CRISTALES', 'DE CALCITA. EMBISTE Y SE', 'ENROSCA SI LO VOLTEAN.'], ('14', '1', 'MEDIA')),
    ('Pikaia', 'PIKAIA', lambda: habitat(SHALLOWS, 22, caustics=True), creature_pikaia,
     ['UNO DE LOS PRIMEROS', 'CORDADOS: UNA ESPINA', 'FLEXIBLE Y MUCHA PRISA.'], ('3', '1', 'RÁPIDA')),
    ('Ottoia', 'OTTOIA', lambda: habitat(REEF, 23, flecks=(('#d0705a', 3),), sand=True), creature_ottoia,
     ['GUSANO PRIAPÚLIDO. ESPERA', 'ENTERRADO Y SACA UNA', 'TROMPA LLENA DE GANCHOS.'], ('9', '1', 'MEDIA')),
    ('Vauxia', 'VAUXIA', lambda: habitat(REEF, 24, flecks=(('#d0705a', 5), (REEF[3], 4))), creature_vauxia,
     ['ESPONJA DEL ARRECIFE. NO', 'SE MUEVE, PERO CURA A', 'TODO LO QUE LA RODEA.'], ('16', '0', 'QUIETA')),
    ('BlueJelly', 'MEDUSA AZUL', lambda: habitat(ABYSS, 25, flecks=(('#36d8d0', 5), ('#7ae8ff', 2))), creature_bluejelly,
     ['MEDUSA DEL ABISMO. SU', 'CAMPANA BRILLA ANTES DE', 'SOLTAR SUS DESCARGAS.'], ('10', '1', 'LENTA')),
]

FLOOR1 = [  # (EnemyData, nombre, fondo, criatura, ficha)
    ('Chaser', 'BACTERIA', lambda: habitat_soup(1), creature_chaser,
     ['BACTERIA FLAGELADA. UN SOLO', 'LÁTIGO LE BASTA PARA', 'CAZAR TODO LO QUE SE MUEVE.'], ('6', '1', 'RÁPIDA')),
    ('Shooter', 'AMEBA', lambda: habitat_soup(2), creature_shooter,
     ['CAMBIA DE FORMA SIN PARAR.', 'ESCUPE LO QUE NO DIGIERE Y', 'AL MORIR SE PARTE EN DOS.'], ('8', '1', 'LENTA')),
    ('Small', 'AMEBITA', lambda: habitat_soup(3), creature_small,
     ['MITAD DE UNA AMEBA. LE', 'FALTA TAMAÑO, PERO NO', 'GANAS DE MORDER.'], ('2', '1', 'MUY RÁPIDA')),
]


def floor_mockup(entries, sel, tab, frame=0, traps_from=None):
    """Maqueta del bestiario con los retratos de una tanda y la ficha de la criatura elegida."""
    saved = (B.BEASTS[:], B.CARD, B.TRAPS_FROM)
    try:
        B.BEASTS[:] = [(None, e[1], True) for e in entries]
        B.TRAPS_FROM = traps_from if traps_from is not None else len(entries)
        e = entries[sel]
        B.CARD = (e[1], e[4], e[5] + ('12',), e[3]())
        im = bestiary_mockup(frame, sel=sel, tab=tab)
    finally:
        B.BEASTS[:], B.CARD, B.TRAPS_FROM = saved
    gx, gy, step = PX + 12, PY + 26, 34
    tf = traps_from if traps_from is not None else len(entries)
    for i, e in enumerate(entries):
        if i < tf: cx, cy = gx + (i % 5) * step, gy + (i // 5) * step
        else: cx, cy = gx + (i - tf) * step, gy + 2 * step + 18
        im.alpha_composite(portrait(e[2](), e[3](), e[6] if len(e) > 6 else None), (cx + 5, cy + 5))
    return im


FLOOR3 = [
    ('Beetle', 'ESCARABAJO', lambda: habitat(SWAMP, 31, flecks=(('#557a3e', 6),)), creature_beetle,
     ['ESCARABAJO DEL CARBONÍFERO.', 'ACORAZADO; EMBISTE CUANDO', 'SE SIENTE ACORRALADO.'], ('12', '1', 'MEDIA')),
    ('Spider', 'ARAÑA', lambda: habitat(SWAMP, 32, flecks=(('#557a3e', 5),)), creature_spider,
     ['SE OCULTA ENTRE LA MALEZA', 'Y SALTA SOBRE LO QUE PASA.', 'SUS PATAS LO SIENTEN TODO.'], ('12', '1', 'MEDIA')),
    ('Toad', 'SAPO', lambda: habitat(MUD3, 33, flecks=(('#735a3a', 6),)), creature_toad,
     ['ANFIBIO DEL PANTANO. SE', 'HUNDE EN EL LODO Y SALTA', 'CUANDO TE ACERCAS.'], ('10', '1', 'MEDIA')),
    ('Dragonfly', 'LIBÉLULA', lambda: habitat(MURK, 34, flecks=(('#9cc8a0', 3),)), creature_dragonfly,
     ['LIBÉLULA DEL CARBONÍFERO.', 'CON TANTO OXÍGENO EN EL', 'AIRE, LOS INSECTOS CRECÍAN.'], ('14', '1', 'MUY RÁPIDA')),
    ('MeganeuraNymph', 'NINFA', lambda: habitat(MURK, 35, flecks=(('#9cc8a0', 3),)), creature_nymph,
     ['NINFA DE MEGANEURA. CAZA', 'BAJO EL AGUA CON UNA', 'MANDÍBULA QUE SE DISPARA.'], ('8', '1', 'MEDIA')),
    ('Calamites', 'CALAMITES', lambda: habitat(SWAMP, 36, flecks=(('#557a3e', 5),)), creature_calamites,
     ['COLA DE CABALLO GIGANTE.', 'SUS TALLOS HUECOS SE', 'ALZABAN COMO ÁRBOLES.'], ('30', '0', 'QUIETA')),
    ('CalamitesSprout', 'BROTE', lambda: habitat(SWAMP, 37, flecks=(('#557a3e', 6),)), creature_sprout,
     ['BROTE DE CALAMITES. CRECE', 'DEPRISA Y CORRE A', 'MORDERTE LOS TOBILLOS.'], ('3', '1', 'MEDIA')),
    ('Medullosa', 'MEDULLOSA', lambda: habitat(SMOKY, 38, flecks=(('#77704f', 6),)), creature_medullosa,
     ['HELECHO CON SEMILLAS. LAS', 'LANZA COMO PROYECTILES', 'CUANDO ALGO SE ACERCA.'], ('16', '1', 'QUIETA')),
    ('CarnivorousPlant', 'PLANTA CARNÍVORA', lambda: habitat(SMOKY, 39, flecks=(('#77704f', 5),)), creature_carnivorous,
     ['NO PERSIGUE: ESPERA A', 'QUE PASES CERCA PARA', 'CERRAR LA BOCA.'], ('—', '1', 'QUIETA'), 'trap'),
    ('Mayflies', 'EFÍMERAS', lambda: habitat(MURK, 40, flecks=(('#9cc8a0', 4),)), creature_mayflies,
     ['VIVEN UN SOLO DÍA Y LO', 'PASAN EN UN ENJAMBRE', 'QUE NO TE DEJA EN PAZ.'], ('—', '1', 'LENTA'), 'trap'),
]

BOSSES = [
    ('AmoebaBoss', 'AMEBA GIGANTE', lambda: habitat(BOSSROOM1, 41, flecks=(('#6a3a40', 6),)), boss_amoeba,
     ['SE HA COMIDO A TODAS SUS', 'HERMANAS. AL PARTIRLA, SE', 'DIVIDE EN DOS MÁS RÁPIDAS.'], ('180', '1', 'LENTA'), 'boss'),
    ('VirusBoss', 'VIRUS', lambda: habitat(BOSSROOM1, 42, flecks=(('#6a3a40', 5),)), boss_virus,
     ['NO ESTÁ VIVO DEL TODO.', 'INYECTA SU CÓDIGO Y', 'MULTIPLICA LO QUE TOCA.'], ('120', '1', 'LENTA'), 'boss'),
    ('AnomalocarisBoss', 'ANOMALOCARIS', lambda: habitat(ABYSS, 43, flecks=(('#36d8d0', 4),)), boss_anomalocaris,
     ['EL GRAN DEPREDADOR DEL', 'CÁMBRICO. ATRAPABA PRESAS', 'CON SUS DOS GARFIOS.'], ('300', '2', 'MUY RÁPIDA'), 'boss'),
    ('EurypteridBoss', 'EURIPTÉRIDO', lambda: habitat(REEF, 44, flecks=(('#d0705a', 4),)), boss_eurypterid,
     ['ESCORPIÓN MARINO. SUS', 'PALAS LE HACÍAN NADAR Y', 'SU COLA, DERRUMBAR ROCAS.'], ('300', '2', 'MEDIA'), 'boss'),
    ('ArthropleuraBoss', 'ARTHROPLEURA', lambda: habitat(SMOKY, 45, flecks=(('#77704f', 6),)), boss_arthropleura,
     ['EL MAYOR ARTRÓPODO DE', 'TIERRA: MÁS DE DOS METROS', 'DE PATAS Y CORAZA.'], ('600', '2', 'MEDIA'), 'boss'),
    ('MeganeuraBoss', 'MEGANEURA', lambda: habitat(BURNT, 46, flecks=(('#e8682a', 3), ('#6d6466', 4))), boss_meganeura,
     ['LIBÉLULA GIGANTE: SETENTA', 'CENTÍMETROS DE ALAS.', 'LLAMA A LA TORMENTA.'], ('450', '2', 'RÁPIDA'), 'boss'),
    ('MegarachneBoss', 'MEGARACHNE', lambda: habitat(NEST, 47, flecks=(('#3a4a34', 6),)), boss_megarachne,
     ['SU NOMBRE SIGNIFICA', 'GRAN ARAÑA, AUNQUE ERA UN', 'EURIPTÉRIDO. AQUÍ TEJE.'], ('150', '1', 'MEDIA'), 'boss'),
    ('LepidodendronBoss', 'LEPIDODENDRON', lambda: habitat(SWAMP, 48, flecks=(('#9a5ad0', 3), ('#557a3e', 4))), boss_lepidodendron,
     ['ÁRBOL ESCAMOSO DEL', 'CARBONÍFERO. SUS RESTOS', 'SON HOY CARBÓN.'], ('600', '2', 'QUIETA'), 'boss'),
]

TANDAS = [('piso1', 'FLOOR1', 0, None), ('piso2', 'FLOOR2', 1, None), ('piso3', 'FLOOR3', 2, 8), ('jefes', 'BOSSES', 3, None)]

if __name__ == '__main__':
    d = os.path.join(OUT, 'Bestiary'); os.makedirs(d, exist_ok=True)
    for tag, var, tab, traps in TANDAS:
        tanda = globals()[var]
        pairs = {}
        for e in tanda:
            key_, bg, cr = e[0], e[2], e[3]
            kind = e[6] if len(e) > 6 else None
            p, l = portrait(bg(), cr(), kind), locked(bg(), cr())
            p.save(os.path.join(d, 'Portrait_%s.png' % key_)); l.save(os.path.join(d, 'Portrait_%s_Locked.png' % key_))
            pairs[key_] = (p, l)
        compare(os.path.join(OUT, '_ret_tmp.png'), pairs, BG, scale=8, labels=('retrato', 'bloqueado'))
        with Image.open(os.path.join(OUT, '_ret_tmp.png')) as _b: board = _b.convert('RGBA')
        os.remove(os.path.join(OUT, '_ret_tmp.png'))
        mock = floor_mockup(tanda, 0, tab, traps_from=traps).resize((960, 540), Image.NEAREST)
        prev = Image.new('RGBA', (max(board.width, 970) + 10, board.height + 560), BG)
        prev.alpha_composite(board, (0, 0)); prev.alpha_composite(mock, (10, board.height + 10))
        prev.save(os.path.join(OUT, 'vista_previa_retratos_%s.png' % tag))
    print('Listo en', d)
