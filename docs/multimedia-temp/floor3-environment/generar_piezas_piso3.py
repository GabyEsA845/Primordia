"""Piezas de sala del piso 3 de Primordia (tanda 1c) y teletransportes de los tres pisos.

En pieces/: conos-cofre (pequeño y grande), tronco puente sobre el lodo, flores moradas, puerta secreta de raíces,
vaina-almacén de corazones de la tienda, bloque de ámbar de la sala del ámbar y los teletransportes de los pisos 1,
2 y 3. Ver README.md.

Uso: python generar_piezas_piso3.py
"""
import math, os, random
from base_piso3 import *
from generar_tilesets_piso3 import canopy, overhang, floor_base, SEED as SEED_T
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, 'pieces')
CONE = [C(c) for c in ('#24140c', '#432614', '#673c1e', '#8c5a2a', '#b07a3c')]   # escamas leñosas pardas
BIOMASS = [C(c) for c in ('#4e6e1a', '#8eb032', '#c8dc58', '#f4ffb0')]   # la de los cofres del piso 2
AMB = [C(c) for c in AMBER]
PURPLE = [C(c) for c in STROB]             # morado = camino secreto
TP = [C(c) for c in ('#164450', '#2a8a90', '#6ad8d0', '#c8fff4')]       # luz común de los teletransportes
SHADOW = (24, 20, 37, 90)

def ground_shadow(cv, cx, cy, rx, ry):
    for y in range(int(cy - ry), int(cy + ry) + 1):
        for x in range(int(cx - rx), int(cx + rx) + 1):
            if ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 <= 1: cv.put_out(x, y, SHADOW)

def glow(cv, cx, cy, r, col, a):
    """Halo translúcido con trama (luz que no tapa)."""
    for y in range(int(cy - r), int(cy + r) + 1):
        for x in range(int(cx - r), int(cx + r) + 1):
            d = math.hypot(x + .5 - cx, y + .5 - cy) / r
            if d < 1 and (1 - d) * 16 > BAYER[y % 4][x % 4]:
                cv.put(x, y, blend(cv.px.get((x, y)), col, int(a * (1 - d))))

# =================================================================================================
# Conos-cofre: estróbilos de Lepidodendron, escamas pardas con las puntas ámbar
# =================================================================================================
def cone(big, open_t=0.0, glint=-1, shake=0, orbs=0.0):
    """Cono de pie como una piña: escamas en rombo superpuestas, cada una con la parte de arriba con luz, el borde de
    abajo en sombra y la punta ámbar. open_t 0..1 las filas se separan y dejan ver la biomasa; glint = fila con
    brillo; orbs 0..1 tres bolas de biomasa que salen hacia arriba."""
    W, H = (32, 42) if big else (18, 26)
    cv = Canvas(W, H)
    cx = W / 2 - .5 + shake
    rx, ry = (8.5, 15) if big else (4.8, 8.5)
    cy = H - 4 - ry
    sw, sh = (6, 4) if big else (4, 3)                # tamaño de cada escama
    spread = 1 + .45 * open_t
    if open_t > 0:                                    # biomasa dentro, que brilla
        for y in range(int(cy - ry * spread), int(cy + ry * spread) + 1):
            for x in range(int(cx - rx * .75), int(cx + rx * .75) + 2):
                d = ((x + .5 - cx) / (rx * .75)) ** 2 + ((y + .5 - cy) / (ry * spread * .92)) ** 2
                if d <= 1: cv.put(x, y, BIOMASS[3] if d < .25 * open_t else BIOMASS[2] if d < .55 else BIOMASS[1])
    for y0 in range(int(cy - ry), int(cy + ry) + 1):
        row = int((y0 - (cy - ry)) // sh)
        yy = cy + (y0 + .5 - cy) * spread - .5       # las filas se separan al abrirse
        ly = (y0 - (cy - ry)) % sh                    # 0 arriba de la escama, sh-1 su borde de abajo
        if open_t > .3 and ly == 0: continue          # hueco entre filas: se ve la biomasa
        v = (y0 + .5 - cy) / ry
        if abs(v) >= 1: continue
        hw = rx * math.sqrt(1 - v * v) * (1 + .3 * open_t * (1 - abs(v)))
        for x in range(int(cx - hw - 1), int(cx + hw) + 2):
            u = (x + .5 - cx) / max(hw, 1)
            if abs(u) > 1: continue
            off = (sw // 2) if row % 2 else 0
            lx = (x - int(cx) + off) % sw - (sw - 1) / 2      # posición dentro de la escama
            edge = ly / (sh - 1) + abs(lx) / (sw / 2) * .6     # rombo: el borde de abajo baja hacia el centro
            body = 3 if u < -.5 else 2 if u < .1 else 1 if u < .6 else 0      # sombreado del cono entero
            if edge > 1.05: tone = max(0, body - 1)            # junta en sombra
            elif ly == 0: tone = min(4, body + 2)              # parte de arriba de la escama, con luz
            else: tone = min(4, body + 1)
            col = CONE[tone]
            if ly == sh - 1 and abs(lx) < .6:                  # punta ámbar en el borde de abajo
                col = AMB[4] if row == glint else AMB[3] if u < 0 else AMB[2]
            elif row == glint and ly == 0 and u < .4: col = AMB[3]
            cv.put(x, yy, col)
    for k in range(2 if big else 1):                  # punta y pie
        cv.put(cx, cy - ry * spread - 1 - k, CONE[2]); cv.put(cx, cy + ry * spread + k, CONE[1])
    if orbs > 0:
        for k, dx in enumerate((-1, 0, 1)):
            ox = cx + dx * (5 if big else 3) * orbs; oy = cy - ry - (8 if big else 5) * orbs + abs(dx) * 2
            ellipse(cv, ox, oy, 1.8 if big else 1.3, 1.8 if big else 1.3, BIOMASS)
    cv.outline(); cv.clean()
    ground_shadow(cv, cx + .5, H - 3, rx * .9, 1.6)
    return cv

def cone_set():
    out = {}
    for big, name in ((False, 'Small'), (True, 'Big')):
        out[f'Cone_{name}_Idle'] = [cone(big, glint=g) for g in (-1, -1, 2, 4)]
        out[f'Cone_{name}_Open'] = [cone(big, 0, shake=1), cone(big, 0, glint=3, shake=-1), cone(big, .45),
                                    cone(big, .85), cone(big, 1, orbs=.6), cone(big, 1, orbs=1)]
        out[f'Cone_{name}_Opened'] = [cone(big, .8)]
    return out

# =================================================================================================
# Tronco puente sobre el lodo profundo (se anda por encima), en tiles de 16×16
# =================================================================================================
def log_tile(part, vertical, t, var=0):
    """part: start (pie con raíces, en la orilla), mid, end (copa rota). Horizontal: el tronco va de izquierda a
    derecha (start a la izquierda); vertical: de arriba (start) abajo. Lo de alrededor es transparente (se ve el
    lodo) con la sombra del tronco."""
    cv = Canvas(16, 16)
    bark = [C(c) for c in (CHAR if t['burnt'] else BARK if t['key'] == 'Swamp' else t['bark'])]
    rp = [C(c) for c in (CHAR if t['burnt'] else t['root'])]
    a0, a1 = 3, 13                                    # ancho del tronco (11 px)
    rnd = random.Random(var * 7 + (1 if vertical else 0))
    def P(l, c, col):                                 # l a lo largo, c a lo ancho (0 = lado con luz)
        if vertical: cv.put(a0 + c, l, col)
        else: cv.put(l, a0 + c, col)
    L0 = 4 if part == 'start' else 0; L1 = 12 if part == 'end' else 16
    for l in range(L0, L1):
        for c in range(a1 - a0 + 1):
            u = (c + .5) / (a1 - a0 + 1)
            lx = (c + (3 if (l // 8) % 2 else 0)) % 6 - 2.5     # rombos de 6 a lo ancho y 8 a lo largo (encajan cada 16)
            ly = l % 8 - 3.5
            dd = abs(lx) / 3 + abs(ly) / 4
            light = 4 if u < .2 else 3 if u < .45 else 2 if u < .8 else 1
            col = bark[0] if c in (0, a1 - a0) else bark[max(0, light - 2)] if dd > .95 else bark[light] if dd < .7 else bark[max(1, light - 1)]
            P(l, c, col)
    if part == 'start':                               # pie: raíces que se abren hacia la orilla
        for c in range(a1 - a0 + 1):
            P(3, c, bark[1]); P(4, c, bark[2] if c > 2 else bark[3])
        for k, cc in enumerate((-1, 3, 7, 11)):
            for j in range(4):
                P(3 - j, cc + (j * (1 if cc > 5 else -1)) // 2, rp[2] if j < 2 else rp[1])
    if part == 'end':                                 # rotura astillada con la madera clara
        wood = [C(c) for c in (CHAR[2:] if t['burnt'] else ['#8e7048', '#b8955e', '#dcbc84'])]
        for c in range(a1 - a0 + 1):
            e = 12 + (c * 5 % 3) - 1
            P(e, c, wood[1] if c < 6 else wood[0]);
            if c % 3 == 1: P(e + 1, c, wood[2])
    if part == 'mid' and var == 1:                    # nudo / musgo
        if t['key'] == 'Swamp':
            for l in range(5, 11): P(l, 1 + (l % 2), C(t['vein'][1 + l % 2]))
        elif t['burnt']: P(8, 4, C(EMBER[2])); P(9, 4, C(EMBER[1]))
        else: P(7, 5, bark[0]); P(8, 5, bark[1]); P(7, 6, bark[1])
    for l in range(L0, L1):                           # sombra sobre el lodo, del lado contrario a la luz
        for k in (1, 2):
            if vertical: cv.put_out(a1 + k, l + 1, SHADOW)
            else: cv.put_out(l + 1, a1 + k, SHADOW)
    return cv

def bridge_set(t):
    out = {}
    for vertical, d in ((False, 'H'), (True, 'V')):
        out[f'Bridge_{d}_Start'] = [log_tile('start', vertical, t)]
        out[f'Bridge_{d}_Mid'] = [log_tile('mid', vertical, t, 0), log_tile('mid', vertical, t, 1)]
        out[f'Bridge_{d}_End'] = [log_tile('end', vertical, t)]
    return out

# =================================================================================================
# Flores moradas (camino secreto)
# =================================================================================================
def flower(f=0, pick=-1):
    """16×24. Idle: se mece y el halo late (4 cuadros). pick 0..5: los pétalos estallan en chispas moradas que suben
    y el tallo se dobla."""
    cv = Canvas(16, 24)
    sway = math.sin(f * math.pi / 2) * .8 if pick < 0 else 0
    stem = [C(c) for c in FERN[:3]]
    bend = 0 if pick < 2 else min(3, pick - 1)
    for j in range(10):
        x = 8 + sway * j / 10 + (bend * (j / 10) ** 2 if pick >= 2 else 0)
        cv.put(x, 21 - j, stem[1] if j % 3 else stem[2])
    for s in (-1, 1):                                 # hojitas del pie
        cv.put(8 + s, 20, stem[2]); cv.put(8 + s * 2, 19, stem[1])
    hx, hy = 8 + sway, 10
    if pick < 2:
        a = 160 + (40 if f % 2 else 0) if pick < 0 else 240
        glow(cv, hx, hy, 6.5, PURPLE[3], a // 2)
        for k in range(5):                            # pétalos
            ang = k * 2 * math.pi / 5 - math.pi / 2 + f * .05
            for r in (1, 2, 3):
                x, y = hx + math.cos(ang) * r, hy + math.sin(ang) * r * .85
                cv.put(x, y, PURPLE[4] if r == 1 else PURPLE[3] if r == 2 else PURPLE[2])
        cv.put(hx, hy, C('#fff0b0')); cv.put(hx - 1, hy - 3, PURPLE[4])
        if pick == 1: cv.put(hx, hy, SPARK[0]); glow(cv, hx, hy, 8, PURPLE[4], 200)
    else:
        rnd = random.Random(3)
        for k in range(8):                            # chispas que suben
            ang = rnd.uniform(-math.pi, 0); sp = rnd.uniform(2, 4) * (pick - 1)
            x, y = hx + math.cos(ang) * sp * .7, hy + math.sin(ang) * sp - (pick - 2)
            if pick < 5 or k % 2: cv.put(x, y, PURPLE[4] if (k + pick) % 3 else SPARK[1])
    if pick < 0: ground_shadow(cv, 8.5, 22, 3, 1)
    return cv

# =================================================================================================
# Vaina-almacén de corazones (tienda)
# =================================================================================================
def heart_seed(cv, x, y, lit):
    """Medio corazón rojo como semilla dentro de la vaina."""
    r = [C(c) for c in RED4]
    for dx, dy, i in ((0, 0, 1), (1, 0, 2), (0, 1, 1), (1, 1, 1), (0, 2, 0), (-1, 0, 1), (-1, 1, 0)):
        cv.put(x + dx, y + dy, r[min(3, i + (1 if lit else 0))])
    cv.put(x, y, r[3] if lit else r[2])

def pod(stored, slots=4, pulse=0):
    """Vaina de semillas de pie (32×48) con `slots` huecos; `stored` llevan un medio corazón. pulse 0..3: late al
    recibir uno."""
    cv = Canvas(32, 48)
    sk = ramp('#5e7a3a', 5)
    k = 1 + (.05 if pulse in (1, 2) else 0)
    cx, cy, rx, ry = 16, 22, 7.5 * k, 15 * k
    ellipse(cv, cx, cy, rx, ry, sk)
    for y in range(int(cy - ry), int(cy + ry)):       # costura de la vaina
        cv.put(cx + 2, y, C(sk[1])) if (cx + 2, y) in cv.px else None
    ys = [cy - ry + 2 * ry * (i + 1) / (slots + 1) for i in range(slots)]
    for i, y in enumerate(ys):                        # bultos: semilla o hueco
        for yy in range(int(y - 2.5), int(y + 3.5)):
            for xx in range(int(cx - 4), int(cx + 2)):
                d = ((xx + .5 - (cx - 1)) / 3.2) ** 2 + ((yy + .5 - y) / 2.8) ** 2
                if d <= 1: cv.put(xx, yy, C(sk[3]) if d > .55 and xx < cx - 2 else C(sk[1]) if i >= stored else blend(C(sk[2]), C(RED4[0]), 120))
        if i < stored:
            heart_seed(cv, cx - 1, int(y) - 1, pulse in (1, 2))
            glow(cv, cx - 1, y, 4, C(RED4[2]), 70 + (60 if pulse in (1, 2) else 0))
    leaf = [C(c) for c in FERN]
    for s in (-1, 1):                                 # hojas y tallo al pie
        frondlet(cv, cx, cy + ry - 1, math.pi / 2 + s * 1.0, 9, leaf[:4], .3, 2.5, None, 0)
    for y in range(int(cy + ry), 44): cv.put(cx, y, leaf[1]); cv.put(cx + 1, y, leaf[0])
    cv.put(cx, cy - ry - 1, C(sk[2])); cv.put(cx + 1, cy - ry - 2, C(sk[3]))          # punta
    cv.outline(); cv.clean(); ground_shadow(cv, 16.5, 44.5, 8, 1.8)
    if pulse == 1: glow(cv, cx, cy, 13, C(RED4[2]), 90)
    return cv

def pod_set(slots=4):
    return {'HeartPod_Stored': [pod(n, slots) for n in range(slots + 1)],
            'HeartPod_Receive': [pod(slots // 2, slots, p) for p in range(4)]}

# =================================================================================================
# Bloque de ámbar con insecto (sala del ámbar: guarda las tres especiales)
# =================================================================================================
def amber_shape(x, y):
    """Trozo de ámbar redondeado e irregular (resina que goteó), un poco más ancho abajo."""
    n = math.sin(x * .9 + y * .4) * .06 + math.sin(y * 1.3) * .05
    a = 12.5 + (y - 4) * .08
    return (abs(x - 16) / a) ** 2.4 + (abs(y - 19) / 17) ** 2.4 < 1 + n

def amber_block(glint=-1.0, crack=0.0, burst=-1):
    """40×44. glint 0..1: brillo que cruza en diagonal. crack 0..1: grietas que se extienden. burst 0..2: se rompe
    en esquirlas que saltan."""
    cv = Canvas(40, 44)
    ox, oy = 4, 2
    if burst < 0:
        for y in range(40):
            for x in range(32):
                if not amber_shape(x, y): continue
                u = (x - 4) / 24; v = (y - 2) / 34
                tone = 4 if u + v < .4 else 3 if u + v < .8 else 2 if u + v < 1.35 else 1
                inner = (abs(x - 16) / 9) ** 2 + (abs(y - 20) / 13) ** 2 < 1
                if inner and tone > 1: tone -= 1                       # el centro, más hondo
                cv.put(ox + x, oy + y, AMB[tone])
        ins = [C(c) for c in ('#2a1608', '#4a2a10', '#7a4a1c')]      # cucaracha atrapada, vista desde arriba
        def bug(x, y, i, a=205): cv.put(ox + x, oy + y, blend(cv.px.get((ox + x, oy + y)), ins[i], a))
        for y in range(14, 27):                       # cuerpo: pronoto y élitros
            for x in range(12, 21):
                d = ((x + .5 - 16) / 3.8) ** 2 + ((y + .5 - 21) / 6) ** 2
                if d <= 1: bug(x, y, 1 if d < .5 and x < 16 else 0)
        for x in range(13, 20): bug(x, 16, 2 if x < 16 else 1)             # borde del pronoto
        for y in range(18, 27): bug(16, y, 0, 230)                         # raya entre los élitros
        for s_ in (-1, 1):
            for j, (dx, dy, L) in enumerate(((4, 18, 4), (4, 21, 4), (4, 24, 5))):
                for k in range(L): bug(16 + s_ * (dx + k), dy + (k if j == 2 else -k // 2 if j == 0 else 0), 0, 180)
            for k in range(7): bug(16 + s_ * (1 + k // 2), 14 - k, 0, 170)  # antenas
            for k in range(3): bug(16 + s_ * (1 + k), 27 + k, 0, 160)       # cercos
        if glint >= 0:                                # brillo en diagonal
            for y in range(40):
                for x in range(32):
                    if amber_shape(x, y) and abs((x + y) - (6 + glint * 52)) < 1.5: cv.put(ox + x, oy + y, AMB[4])
        cv.put(ox + 9, oy + 7, SPARK[1]); cv.put(ox + 10, oy + 6, AMB[4]); cv.put(ox + 8, oy + 8, AMB[4])
        if crack > 0:                                 # grietas desde el centro
            rnd = random.Random(5)
            for k in range(6):
                ang = k * math.pi / 3 + rnd.uniform(-.3, .3); x, y = 16, 19
                for st in range(int(17 * crack)):
                    x += math.cos(ang); y += math.sin(ang) * 1.3; ang += rnd.uniform(-.3, .3)
                    if amber_shape(int(x), int(y)): cv.put(ox + x, oy + y, AMB[4] if st % 3 else SPARK[1])
        cv.outline(); cv.clean()
        ground_shadow(cv, 20.5, 41, 13, 2)
    else:                                             # esquirlas que saltan
        rnd = random.Random(9)
        for k in range(14):
            ang = rnd.uniform(0, 2 * math.pi); sp = (4 + rnd.uniform(0, 8)) * (1 + burst)
            x, y = 20 + math.cos(ang) * sp, 20 + math.sin(ang) * sp * .8 + burst * burst * 2
            for dx, dy in ((0, 0), (1, 0), (0, 1)):
                cv.put(x + dx, y + dy, AMB[3 if (k + dx) % 2 else 2])
            if burst < 1: cv.put(x - 1, y, AMB[4])
        if burst < 1: glow(cv, 20, 20, 12, AMB[4], 160)
    return cv

def amber_set():
    return {'AmberBlock_Idle': [amber_block(), amber_block(), amber_block(.2), amber_block(.6)],
            'AmberBlock_Break': [amber_block(crack=.35), amber_block(crack=.7), amber_block(crack=1),
                                 amber_block(burst=0), amber_block(burst=1), amber_block(burst=2)]}

# =================================================================================================
# Teletransportes: piso 1 burbuja de membrana, piso 2 remolino, piso 3 tronco hueco con raíces
# =================================================================================================
def swirl(cv, cx, cy, rx, ry, phase, power, arms=3):
    """Remolino de luz: brazos en espiral que giran (phase 0..1), más brillantes con power 0..1."""
    for a_i in range(arms):
        for st in range(40):
            tt = st / 40
            ang = a_i * 2 * math.pi / arms + phase * 2 * math.pi + tt * 4.2
            r = tt
            x, y = cx + math.cos(ang) * rx * r, cy + math.sin(ang) * ry * r
            c = TP[3] if tt < .25 * power else TP[2] if tt < .6 else TP[1]
            if power < .3 and tt > .6: continue
            cv.put(x, y, c)
    cv.put(cx, cy, TP[3] if power > .5 else TP[2])

def tp_floor1(state, f):
    """Burbuja de membrana (colores de la sopa del piso 1). state: Dormant, Wake, Idle, Warp."""
    cv = Canvas(32, 32)
    mem = [C(c) for c in ('#3a2440', '#57345c', '#7a4a78', '#a46a98', '#c890b8')]
    inflate = {'Dormant': 0, 'Wake': f / 5, 'Idle': 1, 'Warp': 1}[state]
    wob = math.sin(f * math.pi / 3) * .6 if state in ('Idle', 'Warp') else 0
    rx, ry = 10 + wob, 3 + 9 * inflate - wob * .5
    cx, cy = 16, 27 - ry
    inner = []
    for y in range(int(cy - ry), int(cy + ry) + 1):
        for x in range(int(cx - rx), int(cx + rx) + 1):
            d = ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2
            if d <= 1:
                if d > .72: cv.put(x, y, mem[3] if (x < cx and y < cy) else mem[2] if d < .86 else mem[1])
                else: inner.append((x, y)); cv.put(x, y, blend(C('#1c3a30'), TP[0], 100 if inflate > .5 else 40))
    if inflate > .3:
        p = 1.0 if state == 'Warp' else .6 if state == 'Idle' else inflate * .6
        swirl(cv, cx, cy, rx * .65, ry * .65, f / 6, p)
        cv.put(cx - rx * .5, cy - ry * .55, mem[4]); cv.put(cx - rx * .5 + 1, cy - ry * .55, C('#fff0f8'))   # brillo
    else:
        for x in range(int(cx - rx), int(cx + rx)): cv.put(x, cy - ry + (x % 3 == 0), mem[3])          # arrugas
    if state == 'Warp': warp_flash(cv, cx, cy, f)
    cv.outline(); cv.clean(); ground_shadow(cv, 16.5, 28.5, 11, 2)
    return cv

def tp_floor2(state, f):
    """Remolino de agua en un anillo de piedras (arrecife del piso 2)."""
    cv = Canvas(32, 32)
    rock = [C(c) for c in ('#1c2c50', '#28406c', '#365890', '#4e78b0', '#80a8d8')]
    water = [C(c) for c in ('#0e2238', '#16344a', '#214862', '#2a5772')]
    cx, cy, rx, ry = 16, 20, 12, 7.5
    for y in range(32):
        for x in range(32):
            d = ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2
            if d <= 1: cv.put(x, y, water[0] if d < .3 else water[1] if d < .7 else water[2])
    power = {'Dormant': 0, 'Wake': f / 5, 'Idle': .7, 'Warp': 1}[state]
    if power > 0: swirl(cv, cx, cy, rx * .85, ry * .85, -f / 6, power)
    else:
        for x in range(10, 22, 3): cv.put(x, cy - 1, water[3])
    for k in range(9):                                # piedras del borde
        ang = k * 2 * math.pi / 9 + .2
        px, py = cx + math.cos(ang) * (rx + .5), cy + math.sin(ang) * (ry + .5)
        ellipse(cv, px, py, 2.4, 1.8, rock[1:])
    cv.put(9, 15, C('#d0705a')); cv.put(23, 24, C('#d0705a'))                           # vetas de coral
    if state == 'Warp': warp_flash(cv, cx, cy, f)
    cv.outline(); cv.clean()
    return cv

def tp_floor3(state, f, t):
    """Tronco hueco con raíces (piso 3): dentro del hueco, el remolino de luz y motas que suben."""
    cv = Canvas(32, 40)
    bark = [C(c) for c in BARK]
    bark_trunk(cv, 5, 22, 14, 34, bark, None, 3)
    cx, cy, rx, ry = 16, 14, 10, 4                    # boca del hueco, vista desde arriba
    wood = [C(c) for c in ('#4c3828', '#8e7048', '#b8955e', '#dcbc84')]
    power = {'Dormant': 0, 'Wake': f / 5, 'Idle': .7, 'Warp': 1}[state]
    for y in range(int(cy - ry - 1), int(cy + ry) + 2):
        for x in range(int(cx - rx - 1), int(cx + rx) + 2):
            d = ((x + .5 - cx) / (rx + 1)) ** 2 + ((y + .5 - cy) / (ry + 1)) ** 2
            di = ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2
            if di <= 1: cv.put(x, y, blend(C('#0e0a08'), TP[1], int(150 * power)) if y > cy - ry + 1 else C('#1a120e'))
            elif d <= 1: cv.put(x, y, wood[2] if y < cy else wood[1])                    # borde de madera
    if power > 0:
        swirl(cv, cx, cy, rx * .8, ry * .8, f / 6, power)
        for k in range(4):                            # motas que suben del hueco
            ph = (f / 6 + k / 4) % 1
            cv.put(cx - 6 + k * 4, cy - 2 - ph * 12, blend(None, TP[3 if k % 2 else 2], int(230 * (1 - ph) * power)))
    rp = [C(c) for c in t['root']]
    for s, x0 in ((-1, 7), (1, 25), (-1, 11), (1, 21)):
        root(cv, [(x0, 31), (x0 + s * 2, 34), (x0 + s * 4, 36), (x0 + s * 7, 37)], 3.5 if x0 in (7, 25) else 2.6, 1.2, rp)
    vn = [C(c) for c in TRAMOS['Swamp']['vein']]
    for x in range(6, 27, 2): cv.put(x, 15 + (x % 3), vn[1 + x % 2])                    # musgo en el borde
    if state == 'Warp': warp_flash(cv, cx, cy - 4, f)
    cv.outline(); cv.clean(); ground_shadow(cv, 16.5, 37.5, 14, 2)
    return cv

def warp_flash(cv, cx, cy, f):
    """Al usarlo: la luz se aprieta, destello blanco y columna (cuadros 2-3), y se calma."""
    a = (60, 140, 255, 220, 120, 40)[f]
    glow(cv, cx, cy, 6 + f * 2, TP[3], a)
    if f in (2, 3):
        for y in range(0, int(cy)):
            for x in range(int(cx - 2 + (f - 2)), int(cx + 3 - (f - 2))):
                if (x + y) % 2 == 0 or f == 2: cv.put(x, y, blend(cv.px.get((x, y)), TP[3], max(0, int(200 - (cy - y) * 4))))
        cv.put(cx, cy, SPARK[0])

def tp_set():
    t3 = tram('Swamp')
    out = {}
    for name, fn in (('Teleport_Floor1', tp_floor1), ('Teleport_Floor2', tp_floor2), ('Teleport_Floor3', lambda s, f: tp_floor3(s, f, t3))):
        out[f'{name}_Dormant'] = [fn('Dormant', 0)]
        out[f'{name}_Wake'] = [fn('Wake', f) for f in range(6)]
        out[f'{name}_Idle'] = [fn('Idle', f) for f in range(6)]
        out[f'{name}_Warp'] = [fn('Warp', f) for f in range(6)]
    return out

# =================================================================================================
# Muro de raíces de la sala secreta: toda la pared de arriba de la antesala (8×4 casillas), con el hueco del
# corazón morado en un nudo central. Se abre como un telón cuando pones el corazón (o la semilla brotada).
# =================================================================================================
RW_W, RW_H = 128, 72                     # 8 casillas de ancho; 4 de pared + medio tile de pies sobre el suelo
SOCKET = (64, 30)

def heart_shape(x, y, cx, cy, s=1.0):
    """Corazón de unos 11×10 px (s escala)."""
    u, v = (x + .5 - cx) / (5.2 * s), (y + .5 - cy) / (5.0 * s)
    return (u * u + (v + .35 - abs(u) ** .9 * .75) ** 2) < .62

def purple_heart(cv, cx, cy, s=1.0, bright=0):
    for y in range(int(cy - 7 * s), int(cy + 7 * s)):
        for x in range(int(cx - 7 * s), int(cx + 7 * s)):
            if heart_shape(x, y, cx, cy, s):
                u, v = x - cx, y - cy
                tone = 4 if u + v < -5 * s else 3 if u + v < -1 else 2 if u + v < 4 * s else 1
                cv.put(x, y, PURPLE[min(4, tone + bright)])
    cv.put(cx - 2 * s, cy - 2 * s, SPARK[1] if bright else PURPLE[4])

def wall_roots(seed):
    """Raíces del muro: (puntos de Bézier, grosor de arriba, grosor de abajo, capa). Capa 0 detrás, 1 delante,
    2 las que van al nudo."""
    rnd = random.Random(seed)
    roots = []
    for i in range(15):                               # verticales, de la copa al suelo
        x = 16 + i * 96 / 14 + rnd.uniform(-2, 2)
        x3 = x + rnd.uniform(-8, 8)
        roots.append(([(x, -4), (x + rnd.uniform(-7, 7), 22), (x3 + rnd.uniform(-7, 7), 46), (x3, 68)],
                      rnd.uniform(4, 6.5), rnd.uniform(3, 5), i % 2))
    for k in range(8):                                # diagonales que traban el tejido
        l = k % 2 == 0
        y0, y1 = rnd.uniform(4, 30), rnd.uniform(38, 62)
        roots.append(([(14 if l else 114, y0), (40 if l else 88, y0 + 6), (88 if l else 40, y1 - 8), (114 if l else 14, y1)],
                      rnd.uniform(3, 4.5), rnd.uniform(2.5, 3.5), 1))
    for k in range(10):                               # las que convergen en el nudo
        ang = k * 2 * math.pi / 10 + rnd.uniform(-.2, .2)
        ex, ey = SOCKET[0] + math.cos(ang) * 70, SOCKET[1] + math.sin(ang) * 40
        ex = min(118, max(10, ex)); ey = min(70, max(-4, ey))
        roots.append(([(ex, ey), (ex + (SOCKET[0] - ex) * .4, ey + rnd.uniform(-8, 8)),
                       (SOCKET[0] + (ex - SOCKET[0]) * .25, SOCKET[1] + (ey - SOCKET[1]) * .25 + rnd.uniform(-4, 4)), SOCKET],
                      rnd.uniform(4, 5.5), 3.5, 2))
    return roots

def curtain(pt, e, wob, edge):
    """Telón: la raíz entera se va hacia su lado (`edge`); se mueven más los puntos de media altura."""
    x, y = pt
    k = e * (.55 + .45 * math.sin(math.pi * min(1, max(0, (y + 4) / 76))))
    return (x + (edge - x) * k * .85 + math.sin(y * .25 + wob) * 2 * e * (1 - e) * 4, y)

def root_wall(t, seed, cap, socket='empty', glow_a=0, veins=-1.0, open_e=0.0, wob=0.0, heart_y=None, heart_b=0, sprout=-1):
    """socket: empty | heart | seed (y sprout 0..5 = la semilla brota). veins: radio de las vetas moradas (-1 nada).
    open_e 0..1: el telón se abre. heart_y: el corazón flota suelto a esa altura (al abrirse)."""
    cv = Canvas(RW_W, RW_H)
    rp = [C(c) for c in CHAR] if t['burnt'] else t['root']
    back = [darker(c, .65) for c in rp]
    bark = [C(c) for c in (CHAR if t['burnt'] else BARK if t['key'] == 'Swamp' else t['bark'])]
    for y in range(64):                               # detrás: el paso oscuro y la luz morada de la sala secreta
        for x in range(RW_W):
            d = math.hypot((x - 64) / 50, (y - 34) / 34)
            a = int(max(0, 1 - d) * (70 + 150 * open_e))
            cv.px[(x, y)] = blend(PASSAGE, PURPLE[2], a) if a and (x + y) % 2 == 0 or a > 120 else PASSAGE
    ft = floor_base(t, seed)
    if open_e > 0:                                    # al abrirse, se ve el suelo de la sala secreta al fondo
        for y in range(52, 64):
            for x in range(RW_W):
                if (y - 52) / 12 > .3: cv.px[(x, y)] = blend(darker(ft.px[(x % 16, y % 16)], .5), PURPLE[1], int(60 * open_e))
    roots = wall_roots(seed)
    vein_cv = Canvas(RW_W, RW_H)
    for layer in (0, 1, 2):
        for ctrl, w0, w1, ly in roots:
            if ly != layer: continue
            edge = 8 if sum(p[0] for p in ctrl) / 4 < SOCKET[0] else 120
            pts = [curtain(p, open_e, wob + i, edge) for i, p in enumerate(ctrl)]
            if layer == 2 and open_e > 0:             # las del nudo se sueltan del centro
                pts[-1] = (pts[-1][0] + (-20 if ctrl[0][0] < 64 else 20) * open_e, pts[-1][1])
            pal = back if layer == 0 else rp
            if layer == 0: pts = [(x + 3, y + 1) for x, y in pts]
            vertical = abs(ctrl[0][1] - ctrl[-1][1]) > 60 and abs(ctrl[0][0] - ctrl[-1][0]) < 20
            grow = 1.0 if vertical or layer == 0 else max(0, 1 - open_e * 1.25)    # diagonales y las del nudo se recogen
            if layer == 0 and not vertical: grow = max(0, 1 - open_e * 1.25)
            if grow <= 0: continue
            root(cv, pts, w0 * (1.15 if layer == 0 else 1), w1, pal, grow, (0, -8, RW_W, 70))
            if veins >= 0 and layer > 0:                  # veta: un hilo de 1 px por el lado con luz
                root(vein_cv, [(x - 1, y - 1) for x, y in pts], .8, .8, [PURPLE[2]] * 2 + [PURPLE[3]] * 3, grow, None, False)
    for x0 in (0, RW_W - 14):                         # dos troncos enormes que enmarcan el muro
        bark_trunk(cv, x0, 14, -2, 66, bark, EMBER[2] if t['burnt'] else None, seed + x0, ember_p=.03)
    if open_e < .55:                                  # el nudo con el hueco del corazón (se parte al abrirse)
        split = open_e / .55 * 14
        knot = Canvas(RW_W, RW_H)
        cx, cy = SOCKET
        for y in range(cy - 13, cy + 14):
            for x in range(cx - 16, cx + 17):
                d = ((x + .5 - cx) / 15) ** 2 + ((y + .5 - cy) / 12.5) ** 2
                if d > 1: continue
                ring = (math.atan2(y - cy, x - cx) * 3 + d * 9) % 2 < 1          # vueltas de raíz enrolladas
                u = (x - cx) / 15 + (y - cy) / 12.5
                tone = 4 if u < -.9 else 3 if u < -.2 else 2 if u < .6 else 1
                if ring: tone = max(0, tone - 1)
                if d > .82: tone = max(0, tone - 1)
                knot.px[(x, y)] = rp[tone]
        for y in range(cy - 8, cy + 9):               # el hueco con forma de corazón
            for x in range(cx - 8, cx + 9):
                if heart_shape(x, y, cx, cy, 1.25):
                    rim = not all(heart_shape(x + dx, y + dy, cx, cy, 1.25) for dx, dy in N4)
                    knot.px[(x, y)] = rp[0] if rim else blend(C('#0a0610'), PURPLE[2], min(255, glow_a))
        knot.outline()
        for (x, y), c in knot.px.items():
            dx = -split if x < cx else split
            cv.put(x + dx, y, c)
        if socket == 'heart': purple_heart(cv, cx, cy, 1.0, heart_b)
        elif socket == 'seed' and sprout >= 0:
            if sprout < 2:                            # semilla que se hincha
                ellipse(cv, cx, cy + 1, 2 + sprout * .6, 2.6 + sprout * .6, PURPLE[1:])
            elif sprout < 4:                          # brota: dos hojitas y el capullo
                ellipse(cv, cx, cy + 2, 2.4, 2.6, PURPLE[1:])
                for s in (-1, 1):
                    for k in range(3 + sprout): cv.put(cx + s * k, cy - 1 - k // 2 - (sprout - 2), C(FERN[3 - (k > 2)]))
            else:                                     # se vuelve corazón
                purple_heart(cv, cx, cy, .7 + .15 * (sprout - 4), 1)
    if veins >= 0:                                    # vetas moradas que corren desde el nudo
        for (x, y), c in vein_cv.px.items():
            if math.hypot(x - SOCKET[0], (y - SOCKET[1]) * 1.4) < veins and (x, y) in cv.px and cv.px[(x, y)] != PASSAGE:
                cv.px[(x, y)] = c
    if heart_y is not None:                           # el corazón suelto, flotando y apagándose
        glow(cv, SOCKET[0], heart_y, 10, PURPLE[3], 150)
        purple_heart(cv, SOCKET[0], heart_y, 1.0, heart_b)
    if glow_a > 0 and socket == 'empty':
        glow(cv, SOCKET[0], SOCKET[1], 12, PURPLE[3], glow_a // 2)
    overhang(cv, t, cap)                              # la copa cuelga por encima
    for x in range(RW_W):                             # pies de las raíces sobre el suelo (transparente entre ellos)
        for y in range(64, RW_H):
            if (x, y) in cv.px and cv.px[(x, y)] == PASSAGE: del cv.px[(x, y)]
    for y in range(64, RW_H):
        for x in range(RW_W):
            if (x, y) not in cv.px and (x, y - 1) in cv.px and y < 67: cv.put(x, y, SHADOW)
    return cv

def root_wall_set(t, seed):
    cap = canopy(t, SEED_T[t['key']] + 10)
    W = lambda **k: root_wall(t, seed, cap, **k)
    out = {}
    out['RootWall_Locked'] = [W()]
    out['RootWall_Idle'] = [W(glow_a=a) for a in (60, 130, 200, 130)]
    out['RootWall_InsertHeart'] = [W(socket='heart', glow_a=200, heart_b=b) for b in (0, 1, 1, 0, 0)]
    out['RootWall_InsertSeed'] = [W(socket='seed', glow_a=180, sprout=s) for s in range(6)]
    out['RootWall_Open'] = ([W(socket='heart', glow_a=220, veins=r, heart_b=1 if i == 3 else 0) for i, r in enumerate((15, 40, 75, 140))]
                            + [W(veins=140, open_e=e, wob=i, heart_y=SOCKET[1] - 3 * i, heart_b=1) for i, e in enumerate((.12, .3, .5, .7, .88), 1)]
                            + [W(veins=140, open_e=1.0)])
    out['RootWall_Opened'] = [W(open_e=1.0)]
    return out

# =================================================================================================
# Salida
# =================================================================================================
MS = {'Cone_Small_Idle': 220, 'Cone_Big_Idle': 220, 'Open': 90, 'Flower_Purple_Idle': 220, 'Flower_Purple_Pick': 80,
      'RootWall_Idle': 200, 'RootWall_Insert': 110, 'RootWall_Open': [110, 110, 110, 160, 100, 100, 100, 100, 100, 200], 'HeartPod_Receive': 110, 'AmberBlock_Idle': 200,
      'AmberBlock_Break': 90, 'Wake': 110, 'Idle': 130, 'Warp': 80, 'Bridge': 400, 'HeartPod_Stored': 500}

def ms_of(name):
    for k, v in MS.items():
        if k in name: return v
    return 200

def rootwall_preview():
    """Lámina del muro: cerrado, latiendo, con el corazón, abriéndose y abierto, en los tres tramos (a ×2)."""
    rows = []
    for key in ('Swamp', 'Smoke', 'Burnt'):
        sh = Image.open(os.path.join(HERE, key, 'pieces', 'RootWall_Open.png'))
        frames = [Image.open(os.path.join(HERE, key, 'pieces', 'RootWall_Locked.png'))] + [sh.crop((i * RW_W, 0, i * RW_W + RW_W, RW_H)) for i in (2, 5, 7, 9)]
        rows.append(frames)
    im = Image.new('RGBA', (5 * (RW_W * 2 + 6), 3 * (RW_H * 2 + 6)), BG)
    for r, fr in enumerate(rows):
        for c, f in enumerate(fr):
            im.alpha_composite(f.resize((RW_W * 2, RW_H * 2), Image.NEAREST), (c * (RW_W * 2 + 6), r * (RW_H * 2 + 6)))
    im.save(os.path.join(HERE, 'vista_previa_muro_raices.png'))

if __name__ == '__main__':
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    allp = {}
    allp.update(cone_set())
    allp['Flower_Purple_Idle'] = [flower(f) for f in range(4)]
    allp['Flower_Purple_Pick'] = [flower(pick=p) for p in range(6)]
    allp.update(pod_set(4)); allp.update(amber_set()); allp.update(tp_set())
    per_tramo = {}
    for key in ('Swamp', 'Smoke', 'Burnt'):
        t = tram(key)
        p = {}; p.update(bridge_set(t)); p.update(root_wall_set(t, 700))
        per_tramo[key] = p
        d = os.path.join(HERE, key, 'pieces'); os.makedirs(d, exist_ok=True)
        for name, fr in p.items(): save_sheet(os.path.join(d, name + '.png'), fr)
        seq = p['RootWall_Locked'] + p['RootWall_Idle'] * 2 + p['RootWall_InsertHeart'] + p['RootWall_Open'] + p['RootWall_Opened'] * 3
        ms = [500] + [200] * 8 + [110] * 5 + MS['RootWall_Open'] + [400] * 3
        gif(os.path.join(HERE, key, 'gif', 'RootWall.gif'), seq, ms, t['floor'][1], 3)
        gif(os.path.join(HERE, key, 'gif', 'RootWall_Seed.gif'), p['RootWall_Idle'] + p['RootWall_InsertSeed'] + p['RootWall_Open'][:4], 130, t['floor'][1], 3)
    for name, fr in allp.items():
        save_sheet(os.path.join(OUT, name + '.png'), fr)
        if len(fr) > 1: gif(os.path.join(OUT, 'gif', name + '.gif'), fr, ms_of(name), BG, 3)
    rows = dict(allp)
    for key, p in per_tramo.items():
        rows[f'{key} Bridge'] = [p[k][0] for k in ('Bridge_H_Start', 'Bridge_H_Mid', 'Bridge_H_End', 'Bridge_V_Start', 'Bridge_V_Mid', 'Bridge_V_End')] + [p['Bridge_H_Mid'][1]]
    preview(os.path.join(HERE, 'vista_previa_piezas_1c.png'), rows, BG, scale=3, cols=8)
    rootwall_preview()
    print('Hecho')
