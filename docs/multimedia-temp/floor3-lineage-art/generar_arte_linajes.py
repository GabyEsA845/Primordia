"""Arte de los toques de los linajes del piso 3 (tanda de Opus, 2026-11-03).

Hecho con la skill primordia-sprites (pixel.py): 16 px por unidad, luz arriba a la izquierda, contorno OUTL.
Piezas: parásito grande (Lamprea), chorro venenoso (Pulmonoscorpius), Escupitajo sangriento (Sanguijuela),
matas de hongo (clase Hongo) y lodo del Eryops (solo Diplocaulus, Peonza).

Uso: python generar_arte_linajes.py [parasite] [jet] [leech] [fungus] [eryops]   (sin argumentos: todo)
"""
import math, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', 'floor3-mutations'))
_argv = sys.argv; sys.argv = [sys.argv[0], 'none']
import generar_mutaciones_piso3 as M                     # parasite(), PARA y los ayudantes del piso 3
sys.argv = _argv
from generar_enemigos_piso3 import *                     # noqa: F401,F403  (C, Canvas, ell5, five, blend, save_sheet...)
from PIL import Image

OUT = HERE
BG = C('#1c2e20')
PARA = M.PARA
TEETH = '#f0e4c8'                                         # dientes del disco: hueso claro


def save(rel, frames):
    path = os.path.join(OUT, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True); save_sheet(path, frames)


def gifs(name, frames, ms, scale=6):
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    gif(os.path.join(OUT, 'gif', name + '.gif'), frames, ms, BG, scale=scale)


# --- Escenas de revisión (no son arte del juego) --------------------------------------------------
import random
from PIL import ImageDraw
ENEMIES = os.path.normpath(os.path.join(HERE, '..', 'floor3-enemies'))
SCENE_MS = 50


def ground(w, h, seed=3, tones=('#1c2e20', '#213626', '#273f2b', '#2f4a32')):
    rnd = random.Random(seed)
    im = Image.new('RGBA', (w, h), C(tones[0]))
    for _ in range(w * h // 18):
        x, y = rnd.randrange(w), rnd.randrange(h)
        t = C(tones[rnd.choice((1, 1, 1, 2, 2, 3))])
        for dx in range(rnd.choice((1, 2, 3))):
            if x + dx < w: im.putpixel((x + dx, y), t)
    return im


def load_sheet(path, cell=None):
    im = Image.open(path).convert('RGBA'); cell = cell or im.height
    return [im.crop((i * cell, 0, (i + 1) * cell, im.height)) for i in range(im.width // cell)]


def paste_c(im, fr, cx, cy):
    fr = as_img(fr); im.alpha_composite(fr, (int(round(cx - fr.width / 2)), int(round(cy - fr.height / 2))))


def save_scene(name, frames, labels, scale=4, ms=SCENE_MS, keys=None):
    """GIF de escena en gif/escenas/, ampliado, con rótulos (texto, x, y en píxeles del juego)."""
    out = []
    for fr in frames:
        big = fr.resize((fr.width * scale, fr.height * scale), Image.NEAREST).convert('RGB')
        d = ImageDraw.Draw(big)
        for txt, x, y in labels: d.text((x * scale, y * scale), txt, fill=(220, 230, 200))
        out.append(big)
    path = os.path.join(OUT, 'gif', 'escenas', name + '.gif')
    os.makedirs(os.path.dirname(path), exist_ok=True)
    out[0].save(path, save_all=True, append_images=out[1:], duration=ms, loop=0)
    keys = keys or [len(out) // 3]                         # cuadros clave en una tira, para revisar sin abrir el GIF
    strip = Image.new('RGB', (out[0].width, out[0].height * len(keys)))
    for i, k in enumerate(keys): strip.paste(out[k], (0, out[0].height * i))
    strip.save(os.path.join(OUT, 'gif', 'escenas', name + '_cuadros.png'))


# =================================================================================================
# 1. Parásito grande de la Lamprea
# =================================================================================================
def lamprey(cv, x, y, ph=0.0, curl=0.0, ang=0.0, disc=1.0, bite=False, step=2.2):
    """Lamprea parásita granate de ~13 px: 5 anillos que laten y un disco oral con anillo de dientes delante.
    disc = cuánto abre el disco (0 cerrado, 1 abierto); bite = clavado (el disco se aplasta contra el huésped)."""
    ca, sa = math.cos(ang), math.sin(ang)
    for j in range(5):                                     # de la cola (j = 0) a la cabeza (j = 4)
        t = j - 2.0
        bend = curl * t * t * 0.45
        px, py = x - ca * -t * step - sa * bend, y - sa * -t * step + ca * bend
        r = 1.5 + j * 0.28 + math.sin(ph + j) * 0.25
        ell5(cv, px, py, r, r * 0.9, five(PARA[1:]) if j < 4 else PARA)
    # disco oral: más ancho que la cabeza, con el borde en granate medio (no el tono más claro: se comía la forma)
    hx, hy = x + ca * step * 2.7, y + sa * step * 2.7
    rd = 2.4 + disc * 0.7                                  # semieje a lo ancho
    ru = rd * (0.45 if bite else 0.75)                     # semieje a lo largo (aplastado si está clavada)
    rell5(cv, hx, hy, ru, rd, ang, [PARA[0], PARA[1], PARA[2], PARA[3], PARA[3]])
    cv.outline(); cv.clean()
    # anillos marcados en el lomo
    for j in range(1, 4):
        t = j - 2.0
        cv.put_in(x + ca * t * step + sa * 0.6, y + sa * t * step - ca * 0.6, C(PARA[0]))
    if not bite:
        # boca abierta: hueco oscuro y un anillo de dientes de hueso en su borde
        def inside(px, py, k):
            dx, dy = px + 0.5 - hx, py + 0.5 - hy
            u, v = dx * ca + dy * sa, -dx * sa + dy * ca
            return (u / (ru * k)) ** 2 + (v / (rd * k)) ** 2 <= 1
        for py in range(int(hy - rd - 1), int(hy + rd + 2)):
            for px in range(int(hx - rd - 1), int(hx + rd + 2)):
                if inside(px, py, 0.62): cv.put_in(px, py, C('#2a0812'))
                elif inside(px, py, 0.9) and (px + py) % 2 == 0: cv.put_in(px, py, C(TEETH))
    else:
        # clavada: el labio se ensancha contra el huésped, con una línea clara en el borde de arriba
        for k in (-1, 0, 1):
            cv.put_in(hx - ca * ru * 0.6 - sa * k * rd * 0.6, hy - sa * ru * 0.6 + ca * k * rd * 0.6, C(PARA[4]))
    # ojo diminuto detrás del disco
    ex, ey = x + ca * 3.6 - sa * -1.4, y + sa * 3.6 + ca * -1.4
    cv.put_in(ex, ey, C(PARA[4]))


def lamprey_attached(i):
    """Pegada al enemigo (16×16, bucle de 4): disco clavado abajo, el cuerpo bombea y se curva."""
    cv = Canvas(16)
    lamprey(cv, 8.6, 6.0, ph=i * math.pi / 2, curl=0.3 * math.sin(i * math.pi / 2), ang=math.pi / 2 + 0.6,
            disc=0.4, bite=True, step=2.05)
    if i % 2 == 0: cv.put(5, 14, C(PARA[3]))             # gota que sorbe
    return cv


def lamprey_leap(i, n=5):
    """Salta al siguiente huésped (20×20, 5 cuadros): se enrosca, gira y abre el disco (el código la mueve en arco)."""
    cv = Canvas(20)
    lamprey(cv, 9.5, 9.5, ph=i, curl=0.95, ang=i * 2 * math.pi / n - 0.4, disc=1.0)
    return cv


def parasite_sheets():
    att = [lamprey_attached(i) for i in range(4)]
    leap = [lamprey_leap(i) for i in range(5)]
    save('Parasite/Parasite_Attached_Lamprey.png', att)
    save('Parasite/Parasite_Leap_Lamprey.png', leap)
    old_att = [M.parasite_attached(i) for i in range(4)]
    old_leap = [M.parasite_leap(i) for i in range(5)]
    preview(os.path.join(OUT, 'vista_previa_parasito.png'),
            {'Normal: pegado 12': old_att, 'Lamprea: pegado 16': att,
             'Normal: salto 16': old_leap, 'Lamprea: salto 20': leap}, BG, scale=8)
    gifs('Parasite_Attached_Lamprey', att, 150, 8)
    gifs('Parasite_Leap_Lamprey', leap, 80, 8)
    parasite_scene(old_att, old_leap, att, leap)


def parasite_scene(att_n, leap_n, att_l, leap_l):
    """Dos filas (normal arriba, Lamprea abajo): el parásito chupa a un escarabajo, el escarabajo muere y salta al siguiente."""
    W, H = 200, 120
    bg = ground(W, H)
    idle = load_sheet(os.path.join(ENEMIES, 'Beetle', 'Beetle_Idle.png'))
    death = load_sheet(os.path.join(ENEMIES, 'Beetle', 'Beetle_Death.png'))
    drain = [M.drain_mote(i) for i in range(2)]
    rows = ((30, att_n, leap_n), (88, att_l, leap_l))
    A, B = 60, 150                                         # x de los dos escarabajos (a 5,6 u)
    N = 96
    frames = []
    for f in range(N):
        im = bg.copy()
        for gy, att, leap in rows:
            dead_t = f - 30                                # el primero muere en el cuadro 30
            # escarabajo A
            if dead_t < 0: paste_c(im, idle[(f // 3) % len(idle)], A, gy)
            elif dead_t // 2 < len(death): paste_c(im, death[dead_t // 2], A, gy)
            paste_c(im, idle[(f // 3 + 2) % len(idle)], B, gy)
            # parásito: pegado a A, salta en arco (12 cuadros = 0,6 s), pegado a B
            hx, hy = 0, -6                                 # dónde se pega sobre el lomo
            if f < 32:
                paste_c(im, att[(f // 3) % len(att)], A + hx, gy + hy)
                if f % 12 < 8: paste_c(im, drain[f % 2], A + hx - 6 - (f % 12) * 2, gy + hy - 4 - (f % 12))
            elif f < 44:
                t = (f - 32) / 12
                x = A + (B - A) * t; y = gy + hy - math.sin(t * math.pi) * 26
                paste_c(im, leap[((f - 32) * SCENE_MS // 80) % len(leap)], x, y)
            else:
                paste_c(im, att[(f // 3) % len(att)], B + hx, gy + hy)
        frames.append(im)
    save_scene('escena_parasito_normal_vs_lamprea', frames,
               [('Parasito normal', 4, 4), ('Linaje Lamprea', 4, 62)], keys=[10, 36, 60])


# =================================================================================================
# 2. Chorro venenoso (Pulmonoscorpius): el vinagre de siempre con una veta verde veneno
# =================================================================================================
_argv = sys.argv; sys.argv = [sys.argv[0], os.path.join(HERE, '_tmp')]
sys.path.insert(0, os.path.join(HERE, '..', 'floor3-jet'))
import generar_chorro as J                                # tramos, impacto y Salpicadura del chorro normal
sys.argv = _argv
POI = ['#2e5a14', '#5aa020', '#a8e040', '#e8ffc0']        # veneno de las reacciones del piso 3
TOX = ['#4a5a1c', '#7c9a2c', '#b4d24c', '#e4f2a0', '#fbffe2']   # vinagre envenenado: paja virada a lima (gotas)
TOX_WET = ['#2c3618', '#36441c']                          # suelo mojado de veneno
VIN0, WET0, COL0 = J.VIN, J.WET, J.jet_column


def poison_column(cv, x, u, env=1.0, core_boost=False):
    """Como J.jet_column, pero el centro lleva una veta verde que serpentea y late con el pulso del chorro."""
    COL0(cv, x, u, env, core_boost)
    hw = J.half_width(u) * env
    if hw < 1.2: return
    cy = J.TH / 2 + 0.7 * math.sin(2 * math.pi * u / 16 + 0.8)          # la veta ondula dentro del chorro
    pulse = max(math.exp(-(J._circ(u, 6) / 2.2) ** 2), .8 * math.exp(-(J._circ(u, 31) / 1.6) ** 2))
    vw = 0.55 + 0.5 * pulse + (0.25 if J._circ(u, 22) < 3 else 0)
    for y in range(J.TH):
        d = abs(y + .5 - cy)
        if d <= vw * env: cv.put_in(x, y, C(POI[3] if pulse > .7 and d < .5 else POI[2]))
        elif d <= vw * env + 0.9 and (y + .5 - cy) > 0: cv.put_in(x, y, C(POI[1]))   # sombra de la veta, debajo
    if J._circ(u, 40) < 0.6 and hw > 2.2:                   # burbujita de veneno que viaja por el borde
        cv.put_in(x, J.TH / 2 - hw * .6, C(POI[2]))


def jet_sheets(poison):
    """Las hojas del chorro, normales o envenenadas (cambia los colores del módulo del chorro mientras dibuja)."""
    J.VIN, J.WET, J.jet_column = (VIN0, WET0, COL0)
    pre = 'Fx_VinegarJet_'
    E = {pre + 'Start': [J.start(i) for i in range(J.NF)]}
    for ph, ab in enumerate('ABC'): E[f'{pre}Stream_{ab}'] = [J.stream(i, ph) for i in range(J.NF)]
    for ph, ab in enumerate('ABC'): E[f'{pre}Tip_{ab}'] = [J.tip(i, ph) for i in range(J.NF)]
    if poison:
        J.jet_column = poison_column
        E = {pre + 'Start': [J.start(i) for i in range(J.NF)]}
        for ph, ab in enumerate('ABC'): E[f'{pre}Stream_{ab}'] = [J.stream(i, ph) for i in range(J.NF)]
        for ph, ab in enumerate('ABC'): E[f'{pre}Tip_{ab}'] = [J.tip(i, ph) for i in range(J.NF)]
        J.VIN, J.WET = TOX, TOX_WET                          # salida, impacto y Salpicadura: gotas verdes
    E[pre + 'Muzzle'] = [J.muzzle(i) for i in range(4)]
    E[pre + 'Hit'] = [J.hit(i) for i in range(4)]
    for k, seed in enumerate((101, 202, 303)): E[f'{pre}Splash_{k}'] = [J.splash(i, seed) for i in range(7)]
    J.VIN, J.WET, J.jet_column = (VIN0, WET0, COL0)
    return E


def jet_scene(EN, EP):
    """Dos filas (normal arriba, Pulmonoscorpius abajo): chorro libre de 5 u y, al lado, chorro III que corta un escarabajo."""
    W, H = 232, 150
    bg = ground(W, H)
    body = load_sheet(os.path.join(HERE, '..', 'floor3-bodies', 'Whipscorpion', 'Player_Whipscorpion_AttackLoop_side.png'), 40)
    beetle = load_sheet(os.path.join(ENEMIES, 'Beetle', 'Beetle_Idle.png'))
    frames = []
    for f in range(48):
        im = bg.copy()
        si = f % J.NF
        for gy, E in ((40, EN), (112, EP)):
            bx, by = 4, gy - 22
            im.alpha_composite(as_img(body[f % 4]), (bx, by))
            ox, oy = bx + 21, by + 11
            ex = ox + 7 * 16 - 8                            # nivel III: 7 u, cortado por el escarabajo
            paste_c(im, beetle[(f // 3) % len(beetle)], ex + 8, oy - 2)
            t = f * J.STREAM_MS
            k = (t // 500) % 3; sf = (t % 500) // J.SPLASH_MS
            if sf < 7: im.alpha_composite(as_img(E[f'Fx_VinegarJet_Splash_{k}'][sf]),
                                          (int(ex - J.SPLASH_N / 2), int(oy - J.SPLASH_N / 2)))
            J.paste_rot(im, J.strip(si, ex - ox, True, E), (ox, oy), 0, (0, J.TH // 2))
            J.paste_rot(im, as_img(E['Fx_VinegarJet_Muzzle'][f % 4]), (ox, oy), 0, (8, 8))
            J.paste_rot(im, as_img(E['Fx_VinegarJet_Hit'][f % 4]), (ex, oy), 0, (12, 12))
            # chorro libre a la derecha (5 u), para ver el tramo y la punta sin nada encima
            fx, fy = 150, gy + 20
            J.paste_rot(im, J.strip(si, 72, False, E), (fx, fy), 0, (0, J.TH // 2))
        frames.append(im)
    save_scene('escena_chorro_normal_vs_venenoso', frames,
               [('Chorro normal', 4, 2), ('Linaje Pulmonoscorpius', 4, 74)], keys=[3, 9, 20])


def jet_all():
    EN, EP = jet_sheets(False), jet_sheets(True)
    out = {}
    for name, frames in EP.items():
        nm = name.replace('Fx_VinegarJet_', 'Fx_VinegarJet_Poison_')
        save('Effects/' + nm + '.png', frames); out[nm] = frames
    show = ['Start', 'Stream_A', 'Tip_A', 'Muzzle', 'Hit', 'Splash_0']
    rows = {}
    for k in show:
        rows['Normal ' + k] = EN['Fx_VinegarJet_' + k]; rows['Venenoso ' + k] = EP['Fx_VinegarJet_' + k]
    preview(os.path.join(OUT, 'vista_previa_chorro_venenoso.png'), rows, BG, scale=4)
    mont = {'Normal': [J.strip(0, 112, False, EN)], 'Venenoso': [J.strip(0, 112, False, EP)],
            'Normal ': [J.strip(4, 112, False, EN)], 'Venenoso ': [J.strip(4, 112, False, EP)]}
    preview(os.path.join(OUT, 'vista_previa_chorro_montado.png'), mont, BG, scale=6, cols=1)
    jet_scene(EN, EP)


# =================================================================================================
# 3. Escupitajo de la Sanguijuela: veneno turbio con nubes de sangre y gusanillos nadando
#    Se hace sobre las hojas aprobadas del Escupitajo (floor3-spit y el charco grande de floor3-bodies).
# =================================================================================================
SPIT = os.path.normpath(os.path.join(HERE, '..', 'floor3-spit', 'Effects'))
BODIES_FX = os.path.normpath(os.path.join(HERE, '..', 'floor3-bodies', 'Effects'))
# verde del Escupitajo -> verde turbio (más oliva y apagado); claves: VENOM, VMID, FLASH, DRY y sombra de la banda
MURK = {'#2e5a14': '#2c4416', '#5aa020': '#5a7a26', '#a8e040': '#96b040', '#e8ffc0': '#dce8b0', '#4a8c1c': '#4a6a20',
        '#1f3812': '#1e2c10', '#d8f890': '#c8dc88', '#f2ffd8': '#eef4d0', '#3f7418': '#40601c', '#284c12': '#263c12',
        '#365e18': '#384e1a', '#3f6e1a': '#3e5c1c', '#30521a': '#30441a', '#223c10': '#20300f'}
DEEP_SPOT = '#24480f'                                     # manchas hondas que giran con la bola: pasan a ser sangre
BLOOD = ['#3e1016', '#6a1c22', '#94302e', '#c05a4a', '#e8a088']
WORM = ['#2a0a14', '#4a1424']                             # gusanillos: granate muy oscuro (cuerpo, cabeza)


def _rgb(h): return tuple(int(h[i:i + 2], 16) for i in (1, 3, 5))
def _lum(c): return 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2]
MURK_RGB = {_rgb(k): _rgb(v) for k, v in MURK.items()}
BLOOD_RGB = [_rgb(b) for b in BLOOD]


def _murk(c):
    """Color turbio para un color del veneno (los que no están en la tabla, por el más parecido). Lo que no es verde, igual."""
    if c in MURK_RGB: return MURK_RGB[c]
    if c == _rgb(DEEP_SPOT): return _rgb(BLOOD[1])
    if not (c[1] > c[0] and c[1] >= c[2]): return c
    k = min(MURK_RGB, key=lambda q: sum((a - b) ** 2 for a, b in zip(q, c)))
    return MURK_RGB[k]


def _bloody(c):
    """El mismo tono de luz, en sangre (los brillos más claros se quedan: la luz no se tiñe)."""
    L = _lum(c)
    if L > 190: return c
    return min(BLOOD_RGB[:4], key=lambda b: abs(_lum(b) - L * 0.92))


def _noise(seed, w, h, cell=7):
    """Ruido suave en dos octavas (valor interpolado) para las nubes de sangre."""
    def octave(sd, cl):
        rnd = random.Random(sd)
        gw, gh = int(w / cl) + 3, int(h / cl) + 3
        g = [[rnd.random() for _ in range(gw)] for _ in range(gh)]
        def at(x, y):
            fx, fy = x / cl, y / cl; ix, iy = int(fx), int(fy); tx, ty = fx - ix, fy - iy
            tx, ty = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
            a = g[iy][ix] * (1 - tx) + g[iy][ix + 1] * tx; b = g[iy + 1][ix] * (1 - tx) + g[iy + 1][ix + 1] * tx
            return a * (1 - ty) + b * ty
        return at
    o1, o2 = octave(seed, cell), octave(seed + 1, max(2, cell * .45))
    return lambda x, y: .62 * o1(x, y) + .38 * o2(x, y)


def leechify(im, seed=5, cover=0.22, cell=7):
    """Verde turbio y vetas de sangre en una parte del líquido (cover = fracción teñida; la mitad, solo mezclada)."""
    im = im.convert('RGBA').copy(); px = im.load()
    nz = _noise(seed, im.width + 2, im.height + 2, cell)
    pts = [(x, y) for y in range(im.height) for x in range(im.width) if px[x, y][3]]
    vals = sorted(nz(x, y) for x, y in pts)
    if not vals: return im
    full = vals[int(len(vals) * (1 - cover * .5))] if cover else 9
    mix = vals[int(len(vals) * (1 - cover))] if cover else 9
    for (x, y) in pts:
        r, g, b, a = px[x, y]
        c = (r, g, b); m = _murk(c)
        if m == c: continue
        v = nz(x, y)
        if v >= full: m = _bloody(m)
        elif v >= mix: m = tuple((p * 3 + q * 2) // 5 for p, q in zip(m, _bloody(m)))
        px[x, y] = m + (a,)
    return im


def _liquid(im):
    px = im.load()
    return {(x, y) for y in range(im.height) for x in range(im.width) if px[x, y][3] > 120}


def worms(im, t, n=3, seed=9, scale=1.0, region=None, alpha=210):
    """Gusanillos que nadan dentro del líquido (solo sobre él). t de 0 a 1 = una vuelta (bucle)."""
    im = im.copy(); px = im.load()
    liq = region if region is not None else _liquid(im)
    if not liq: return im
    xs = [p[0] for p in liq]; ys = [p[1] for p in liq]
    cx, cy = sum(xs) / len(xs), sum(ys) / len(ys)
    rx, ry = (max(xs) - min(xs)) * .28, (max(ys) - min(ys)) * .28
    rnd = random.Random(seed)
    for k in range(n):
        ph = rnd.uniform(0, 2 * math.pi); d = rnd.choice((-1, 1)); kr = rnd.uniform(.5, 1.0)
        L = max(3, int(round(4 * scale)))
        rr = max(1.5, (rx + ry) / 2 * kr)
        seen = set()
        for j in range(L * 2):                            # dos muestras por píxel: el gusano sale seguido, sin huecos
            a = d * (t * 2 * math.pi) + ph - d * j * .5 / rr
            w = math.sin(t * 4 * math.pi + j * .9 + ph) * .7          # se retuerce de lado
            x = cx + math.cos(a) * (rx * kr + w)
            y = cy + math.sin(a) * (ry * kr + w)
            q = (int(round(x)), int(round(y)))
            if q in seen or q not in liq: continue
            seen.add(q)
            col = _rgb(WORM[1] if j < 2 else WORM[0])
            r, g, b, a0 = px[q]
            al = alpha / 255
            px[q] = tuple(int(c0 * (1 - al) + c1 * al) for c0, c1 in zip((r, g, b), col)) + (a0,)
    return im


def sheet_frames(path):
    im = Image.open(path).convert('RGBA'); cw = im.height
    return [im.crop((i * cw, 0, (i + 1) * cw, cw)) for i in range(im.width // cw)]


def clot(i, k):
    """Coágulo de sangre (10×10) con los mismos 8 cuadros que un sello de la banda: 0-1 recién puesto, 2 húmedo, 3-7 se seca.
    Una mancha alargada e irregular (lóbulos en fila, de tamaños distintos) con una gotita suelta."""
    cv = Canvas(10); rnd = random.Random(70 + k)
    ang = rnd.uniform(-.5, .5)
    lobes = [(5 + math.cos(ang) * d, 5 + math.sin(ang) * d + rnd.uniform(-.5, .5), r)
             for d, r in ((-1.8, rnd.uniform(1.1, 1.5)), (0, rnd.uniform(1.7, 2.1)), (1.7, rnd.uniform(1.0, 1.4)))]
    lobes.append((5 + rnd.choice((-1, 1)) * 3.4, 5 + rnd.choice((-1, 1)) * 2.4, .8))
    dry = max(0, i - 2) / 5
    grow = 1.1 if i < 2 else 1.0
    pts = {(x, y) for y in range(10) for x in range(10)
           if any(((x + .5 - lx) / (r * grow * (1 - .35 * dry))) ** 2 + ((y + .5 - ly) / (r * .8 * grow * (1 - .35 * dry))) ** 2 <= 1
                  for lx, ly, r in lobes)}
    a = int(235 - 120 * dry)
    for (x, y) in pts:
        dr = (x + 1, y) not in pts or (x, y + 1) not in pts
        ul = (x - 1, y) not in pts or (x, y - 1) not in pts
        col = BLOOD[0] if dr else BLOOD[2] if ul and dry < .5 else BLOOD[1]
        cv.put(x, y, C(col, a))
    if dry < .4 and pts:
        x, y = min(pts); cv.put(x + 1, y, C(BLOOD[3], a))                    # brillo húmedo
    return cv


def band_worm(i, n=8, sink=0.0):
    """Gusanillo que nada dentro de la banda (12×12, dibujado a lo largo, hacia la derecha: el código lo gira con la banda).
    Bucle de n cuadros: ondula y va y viene ~3 px. sink 0..1: se hunde (se apaga y encoge) cuando la baba se seca."""
    cv = Canvas(12); t = i / n
    drift = 1.5 * math.sin(2 * math.pi * t)
    L = 5 - int(round(sink * 2))
    al = int(255 * (1 - sink * .75))
    for j in range(L):                                      # de la cabeza (j = 0, delante) a la cola
        x = 6 + drift + 2 - j
        y = 6 + 0.9 * math.sin(2 * math.pi * (2 * t) - j * 1.1)
        cv.put(x, y, C(WORM[1] if j == 0 else WORM[0], al))
        if 1 <= j <= 2 and sink < .5: cv.put(x, y - 1, C('#8a3040', int(al * .8)))   # lomo granate: se lee como bicho
    if sink < .3 and i % 4 == 0: cv.put(6 + drift + 3, 6 + 0.9 * math.sin(4 * math.pi * t) - 1, C('#dce8b0', 150))  # burbujita
    return cv


def leech_sheets():
    L = {}
    # bolas (rodar e hincharse): la sombra de un gusanillo que se retuerce dentro; bolitas de la Camada, solo el color
    for v in ('side', 'down', 'up'):
        for kind in ('Roll', 'Swell'):
            fr = sheet_frames(os.path.join(SPIT, f'Proj_PoisonSpit_{kind}_{v}.png'))
            L[f'Proj_PoisonSpit_Leech_{kind}_{v}'] = [worms(leechify(f, 11, .25, 4), i / len(fr), 1, 3, 1.0, alpha=170)
                                                      for i, f in enumerate(fr)]
        fr = sheet_frames(os.path.join(SPIT, f'Proj_PoisonSpitSmall_Roll_{v}.png'))
        L[f'Proj_PoisonSpitSmall_Leech_Roll_{v}'] = [leechify(f, 11, .25, 3) for f in fr]
    # reventón: sangre en las gotas y gusanillos que salen despedidos (los saltos a los enemigos los hace el código)
    out = []
    for i, f in enumerate(sheet_frames(os.path.join(SPIT, 'Fx_PoisonSpit_Burst.png'))):
        g = leechify(f, 21, .3, 6)
        if 2 <= i <= 5:
            cv = Canvas(g.width, g.height); c = g.width / 2
            for k, a in enumerate((.6, 2.4, 4.3)):
                dd = 6 + (i - 2) * 5.5 + k
                x, y = c + math.cos(a) * dd, c + math.sin(a) * dd * .82 - (3 if i < 4 else 1)
                for j in range(4):
                    cv.put(x - math.cos(a) * j * .9 + math.sin(j + i) * .5, y - math.sin(a) * j * .9,
                           C(WORM[1] if j == 0 else WORM[0]))
            g.alpha_composite(cv.img())
        out.append(g)
    L['Fx_PoisonSpit_Leech_Burst'] = out
    # charco salpicado (el que deja el reventón) y charco grande: Idle pasa a 8 cuadros para que los gusanillos naden
    for base, src in (('Fx_PoisonSplat', SPIT), ('Fx_PoisonPuddleBig', BODIES_FX)):
        ap, idle, fade = (sheet_frames(os.path.join(src, f'{base}_{k}.png')) for k in ('Appear', 'Idle', 'Fade'))
        nm = base.replace('Fx_Poison', 'Fx_PoisonLeech')
        region = _liquid(idle[0])
        L[nm + '_Appear'] = [worms(leechify(f, 31, .22), 0, 3, 9, 1.2, region) if i == len(ap) - 1 else leechify(f, 31, .22)
                             for i, f in enumerate(ap)]
        L[nm + '_Idle'] = [worms(leechify(idle[i % len(idle)], 31, .22), i / 8, 3, 9, 1.2, region) for i in range(8)]
        L[nm + '_Fade'] = [worms(leechify(f, 31, .22), 0, 3, 9, 1.2, _liquid(f), alpha=150) if i == 0 else leechify(f, 31, .22)
                           for i, f in enumerate(fade)]
    # banda de baba: la misma, turbia y con hilos de sangre
    for name in sorted(os.listdir(SPIT)):
        if name.startswith('Fx_SlimeTrail'):
            fr = sheet_frames(os.path.join(SPIT, name))
            L[name[:-4].replace('Fx_SlimeTrail', 'Fx_SlimeTrailLeech')] = [leechify(f, 1, 0) for f in fr]
    # coágulos de sangre que se ponen encima de algunos sellos (como el brillo): la banda queda turbia y con grumos sueltos
    for k in range(3):
        L[f'Fx_SlimeTrailLeech_Clot_{k}'] = [clot(i, k) for i in range(8)]
    # gusanillos que nadan en la banda (encima de algunos sellos mientras están húmedos) y cómo se hunden al secarse
    L['Fx_SlimeTrailLeech_Worm'] = [band_worm(i) for i in range(8)]
    L['Fx_SlimeTrailLeech_WormSink'] = [band_worm(i, sink=s_) for i, s_ in enumerate((.35, .7, 1.0))]
    return L


def leech_scene(L):
    """Dos filas (normal arriba, Sanguijuela abajo): la bola rueda dejando la banda, se hincha, revienta y deja el charco."""
    W, H = 216, 136
    bg = ground(W, H)
    rows = []
    for gy, leech in ((36, False), (104, True)):
        def get(name, leech_name):
            return L[leech_name] if leech else sheet_frames(os.path.join(SPIT, name + '.png'))
        rows.append(dict(
            gy=gy, roll=get('Proj_PoisonSpit_Roll_side', 'Proj_PoisonSpit_Leech_Roll_side'),
            swell=get('Proj_PoisonSpit_Swell_side', 'Proj_PoisonSpit_Leech_Swell_side'),
            burst=get('Fx_PoisonSpit_Burst', 'Fx_PoisonSpit_Leech_Burst'),
            ap=get('Fx_PoisonSplat_Appear', 'Fx_PoisonLeechSplat_Appear'),
            idle=get('Fx_PoisonSplat_Idle', 'Fx_PoisonLeechSplat_Idle'),
            rim=[get(f'Fx_SlimeTrail_Rim_{k}', f'Fx_SlimeTrailLeech_Rim_{k}') for k in range(6)],
            body=[get(f'Fx_SlimeTrail_Body_{k}', f'Fx_SlimeTrailLeech_Body_{k}') for k in range(6)],
            clot=[L[f'Fx_SlimeTrailLeech_Clot_{k}'] for k in range(3)] if leech else None,
            worm=L['Fx_SlimeTrailLeech_Worm'], sink=L['Fx_SlimeTrailLeech_WormSink']))
    frames = []
    x0, x1 = 16, 150
    roll_end, swell_end = 2.4, 3.0
    for f in range(120):                                     # 6 s a 50 ms
        im = bg.copy(); t = f * SCENE_MS / 1000
        for R in rows:
            gy = R['gy']
            bx = x0 + (x1 - x0) * min(1, t / roll_end)
            seals = []
            for sx in range(x0, int(bx) - 4, 4):             # banda: un sello cada 4 px; se seca a los 3 s
                age = t - (sx - x0) / (x1 - x0) * roll_end
                rs = random.Random(sx)
                vr = rs.choice((0, 1, 1, 2, 2, 3, 3, 4, 4, 0, 1, 5))
                if age < 3.0: seals.append((sx, vr, 2 if age < 2.5 else min(7, 3 + int((age - 2.5) / .1)), rs.random(), rs.randrange(3)))
            for sx, vr, fi, _, _ in seals: paste_c(im, R['rim'][vr][fi], sx, gy)
            for sx, vr, fi, _, _ in seals: paste_c(im, R['body'][vr][fi], sx, gy)
            if R['clot']:
                for sx, vr, fi, r_, ck in seals:
                    if r_ < .22: paste_c(im, R['clot'][ck][fi], sx + (ck - 1), gy + (1 if ck == 2 else -1))
                    elif r_ < .40:                           # gusanillo: nada mientras el sello está húmedo, se hunde al secarse
                        wf = (f + ck * 3) % 8
                        if fi <= 2: paste_c(im, R['worm'][wf], sx, gy + ck - 1)
                        elif fi <= 5: paste_c(im, R['sink'][fi - 3], sx, gy + ck - 1)
            if t < roll_end:
                im.alpha_composite(R['roll'][int(t * 1000 / 135) % len(R['roll'])], (int(bx - 12), int(gy - 16.5)))
            elif t < swell_end:
                im.alpha_composite(R['swell'][min(5, int((t - roll_end) * 10))], (int(bx - 12), int(gy - 16.5)))
            else:
                k = int((t - swell_end) * 1000 / 90)
                paste_c(im, R['ap'][k] if k < len(R['ap']) else R['idle'][(k - len(R['ap'])) % len(R['idle'])], x1, gy)
                kb = int((t - swell_end) * 1000 / 60)
                if kb < len(R['burst']): paste_c(im, R['burst'][kb], x1, gy - 3)
        frames.append(im)
    save_scene('escena_escupitajo_normal_vs_sanguijuela', frames,
               [('Escupitajo normal', 4, 2), ('Linaje Sanguijuela', 4, 70)], keys=[20, 50, 63, 100])


def leech_all():
    L = leech_sheets()
    for name, frames in L.items(): save('Effects/' + name + '.png', frames)
    old = lambda n: sheet_frames(os.path.join(SPIT, n + '.png'))
    rows = {'Normal Roll side': old('Proj_PoisonSpit_Roll_side'), 'Sanguijuela Roll side': L['Proj_PoisonSpit_Leech_Roll_side'],
            'Normal Swell side': old('Proj_PoisonSpit_Swell_side'), 'Sanguijuela Swell side': L['Proj_PoisonSpit_Leech_Swell_side'],
            'Normal Small': old('Proj_PoisonSpitSmall_Roll_side'), 'Sanguijuela Small': L['Proj_PoisonSpitSmall_Leech_Roll_side']}
    preview(os.path.join(OUT, 'vista_previa_sanguijuela_bola.png'), rows, BG, scale=6)
    rows = {'Normal Burst': old('Fx_PoisonSpit_Burst'), 'Sanguijuela Burst': L['Fx_PoisonSpit_Leech_Burst'],
            'Normal Splat Idle': old('Fx_PoisonSplat_Idle'), 'Sanguijuela Splat Idle': L['Fx_PoisonLeechSplat_Idle'],
            'Sanguijuela Splat Ap+Fade': L['Fx_PoisonLeechSplat_Appear'] + L['Fx_PoisonLeechSplat_Fade'],
            'Sanguijuela Puddle Idle': L['Fx_PoisonLeechPuddleBig_Idle'],
            'Banda Body 1 (0-7)': L['Fx_SlimeTrailLeech_Body_1'],
            'Gusanillo banda + hundirse': L['Fx_SlimeTrailLeech_Worm'] + L['Fx_SlimeTrailLeech_WormSink'],
            'Coagulos 0-2 (0-7)': sum((L[f'Fx_SlimeTrailLeech_Clot_{k}'] for k in range(3)), [])}
    preview(os.path.join(OUT, 'vista_previa_sanguijuela_charco.png'), rows, BG, scale=4)
    gifs('Fx_PoisonLeechSplat_Idle', L['Fx_PoisonLeechSplat_Idle'], 90, 5)
    gifs('Fx_SlimeTrailLeech_Worm', L['Fx_SlimeTrailLeech_Worm'], 110, 8)
    gifs('Proj_PoisonSpit_Leech_Roll_side', L['Proj_PoisonSpit_Leech_Roll_side'], 135, 8)
    leech_scene(L)


# =================================================================================================
# 4. Matas de hongo del piso 3 (Colonizar la sala): alfombra de micelio que brilla y una especie de hongo por mata
# =================================================================================================
GLOW = ['#1a4a44', '#22806e', '#4ac8a4', '#a8f4d8', '#e8fff4']       # bioluminiscencia verde-azulada
HYPHA = ['#6a6a58', '#a8ac94', '#d8dcc4', '#f2f4e4']                 # hilos de micelio
ROT = '#22241a'                                                      # tierra podrida bajo la alfombra
IVORY = ['#4a4438', '#7a725e', '#b4ac92', '#e2dcc4', '#fbf8ea']
OCHRE = ['#5a3a10', '#8a5a18', '#c08a34', '#e9b45c', '#fff0c0']      # color de la clase Hongo
RUST = ['#3a1e14', '#6a3420', '#9a5430', '#c88a58', '#f0c8a0']
STALK = ['#7a7462', '#b8b29a', '#e4e0cc']
SPECIES = ('Bells', 'Puffballs', 'Earthstars', 'Clubs')
MAT_SIZES = {'M': (48, 19.0, 6), 'L': (64, 27.0, 9)}               # celda, radio de la alfombra (px), hongos


def _blend_put(cv, x, y, col, a):
    x, y = int(math.floor(x)), int(math.floor(y))
    if not cv._in(x, y): return
    c = C(col) if isinstance(col, str) else col
    old = cv.px.get((x, y))                                  # sin mezclar colores (la paleta no se dispara): pisa, y la
    cv.px[(x, y)] = c[:3] + (max(a, old[3]) if old else a,)  # opacidad es la mayor de las dos


def mat_plan(species, size, seed):
    """Dónde va cada cosa de una mata (fijo para todos sus cuadros)."""
    n, R, nf = MAT_SIZES[size]
    rnd = random.Random(seed)
    c = n / 2
    # alfombra: unión de manchas, aplastada en perspectiva (0,72)
    blobs = [(c, c, R * .55)] + [(c + math.cos(a) * R * rnd.uniform(.25, .55), c + math.sin(a) * R * rnd.uniform(.25, .55) * .72,
                                  R * rnd.uniform(.28, .42)) for a in [k * 2 * math.pi / 7 + rnd.uniform(-.3, .3) for k in range(7)]]
    # hilos: caminos al azar que salen del centro y pasan del borde (así la mata se funde con el suelo)
    threads = []
    for k in range(10 if size == 'M' else 15):
        a = rnd.uniform(0, 2 * math.pi); x, y = c + rnd.uniform(-3, 3), c + rnd.uniform(-2, 2)
        pts = []
        for s in range(int(R * rnd.uniform(.9, 1.25))):
            a += rnd.uniform(-.45, .45)
            x += math.cos(a); y += math.sin(a) * .72
            pts.append((x, y))
        threads.append(pts)
    specks = [(c + rnd.uniform(-R, R) * .8, c + rnd.uniform(-R, R) * .55, rnd.uniform(0, 2 * math.pi)) for _ in range(9 if size == 'M' else 15)]
    # hongos: 2-3 racimos separados por la mata, con distancia mínima entre pies (no se amontonan), y setillas
    # diminutas sueltas por toda la alfombra para que se lea la zona
    fungi = []
    nc = 2 if size == 'M' else 3
    a0 = rnd.uniform(0, 2 * math.pi)
    clusters = [(c + math.cos(a0 + k * 2 * math.pi / nc) * R * .42, c + math.sin(a0 + k * 2 * math.pi / nc) * R * .42 * .72)
                for k in range(nc)]
    mind = {'Bells': 3.6, 'Puffballs': 8.2, 'Earthstars': 12.0, 'Clubs': 4.5}[species]
    tries = 0
    nf_ = nf if species != 'Earthstars' else (3 if size == 'M' else 5)
    while len(fungi) < nf_ and tries < 600:
        tries += 1
        cx, cy = clusters[len(fungi) % nc]
        fx, fy = cx + rnd.gauss(0, R * .17), cy + rnd.gauss(0, R * .11)
        big = rnd.random() ** 1.6 if len(fungi) >= nc else .75 + rnd.random() * .25   # uno grande por racimo, el resto menores
        sz_ = .5 + big * .8 + (.2 if size == 'L' else 0)
        if abs(fx - c) > n / 2 - 10 or abs(fy - c) > n / 2 - 9: continue          # que no se salga de la celda
        if any((fx - g['x']) ** 2 + ((fy - g['y']) / .72) ** 2 < (mind * (sz_ + g['s']) / 2) ** 2 for g in fungi): continue
        fungi.append(dict(x=fx, y=fy, s=sz_, ph=rnd.uniform(0, 2 * math.pi), lean=rnd.uniform(-.3, .3),
                          pal=rnd.choice((IVORY, OCHRE, OCHRE, RUST)), flip=rnd.random() < .5, tiny=False))
    for k in range(8 if size == 'M' else 14):
        a = rnd.uniform(0, 2 * math.pi); d = R * math.sqrt(rnd.uniform(.05, .8))
        fungi.append(dict(x=c + math.cos(a) * d, y=c + math.sin(a) * d * .72, s=0, ph=rnd.uniform(0, 6.28), lean=0, pal=IVORY,
                          flip=False, tiny=True))
    fungi.sort(key=lambda f: f['y'])
    spores = [(rnd.uniform(-R, R) * .6, rnd.uniform(0, 1), rnd.uniform(0, 2 * math.pi)) for _ in range(6 if size == 'M' else 10)]
    return dict(n=n, R=R, c=c, blobs=blobs, threads=threads, specks=specks, fungi=fungi, spores=spores, species=species)


def draw_carpet(cv, P, grow=1.0, glow=0.0):
    """Alfombra de micelio: tierra podrida translúcida, hilos claros que pasan del borde y motas que brillan."""
    c = P['c']
    inside = lambda x, y: any((x + .5 - bx) ** 2 + ((y + .5 - by) / .72) ** 2 <= (br * grow) ** 2 for bx, by, br in P['blobs'])
    pts = {(x, y) for y in range(P['n']) for x in range(P['n']) if inside(x, y)}
    for (x, y) in pts:
        edge = sum(q not in pts for q in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1), (x + 2, y), (x - 2, y)))
        if edge and (x + y) % 2: continue                    # borde en trama: se funde con el suelo
        _blend_put(cv, x, y, ROT, 120 if not edge else 70)
    felt = _noise(P['n'] * 7 + len(P['fungi']), P['n'] + 2, P['n'] + 2, 5)
    for (x, y) in pts:                                       # fieltro de micelio: velo blanco en trama, más denso al centro
        d = math.hypot(x + .5 - c, (y + .5 - c) / .72) / (P['R'] * grow + .01)
        v = felt(x, y) - d * .35
        if v > .42 and (x + y) % 2 == 0: _blend_put(cv, x, y, HYPHA[2], 110)
        elif v > .52: _blend_put(cv, x, y, HYPHA[1], 120)
    for pts_ in P['threads']:
        L = int(len(pts_) * grow)
        for s, (x, y) in enumerate(pts_[:L]):
            t = s / max(1, len(pts_))
            col = HYPHA[2] if t < .35 else HYPHA[1]
            if s % 3 != 2 or t < .4: _blend_put(cv, c + (x - c) * grow, c + (y - c) * grow, col, int(190 - 140 * t))
    for (x, y, ph) in P['specks']:
        if grow < .7: continue
        v = .5 + .5 * math.sin(ph) + glow
        col = GLOW[4] if v > 1.2 else GLOW[3] if v > .8 else GLOW[2]
        _blend_put(cv, c + (x - c) * grow, c + (y - c) * grow, col, 230)


def fungus(species, f, g=1.0, glow=0.0):
    """Un hongo en su propio lienzo (para contornearlo sin contornear la alfombra). g: lo que ha crecido (0..1,1)."""
    if f.get('tiny'):                                        # setilla diminuta: un pie y una cabeza que brilla
        if g < .6: return None
        cv = Canvas(24)
        cv.put(12, 18, C(STALK[1])); cv.put(12, 17, C(GLOW[min(4, 2 + int(glow * 2 + (.5 + .5 * math.sin(f['ph']))))]))
        return cv.img()
    s = f['s'] * g
    if s < .2: return None
    cv = Canvas(24); bx, by = 12, 18                         # pie en (bx, by)
    lean, pal = f['lean'], f['pal']
    gl = min(4, 2 + int(glow * 2 + (.5 + .5 * math.sin(f['ph'])) * 1.2))
    if species == 'Bells':                                   # campanillas (tipo Mycena): pie fino y sombrero que brilla
        h = 3 + 5 * s; top = (bx + lean * h, by - h)
        for k in range(int(h)):
            cv.put(bx + lean * k, by - k, C(STALK[2] if k > h * .5 else STALK[1]))
        r = 1.4 + 1.6 * s
        cap = Canvas(24)
        fill(cap, lambda x, y: ((x + .5 - top[0]) / r) ** 2 + ((y + .5 - top[1]) / (r * .85)) ** 2 <= 1 and y + .5 <= top[1] + r * .35,
             top[0], top[1], r, r, GLOW[1:] if f['ph'] < 3.6 else IVORY[1:], box=(top[0] - r - 1, top[1] - r - 1, top[0] + r + 1, top[1] + r))
        cap.outline(); cap.clean()
        for p, col in cap.px.items(): cv.px[p] = col
        for dx in range(-int(r) + 1, int(r)):                 # láminas que brillan bajo el sombrero
            cv.put(top[0] + dx, top[1] + r * .35 + .6, C(GLOW[gl], 230))
    elif species == 'Puffballs':                             # bejines: bolas pálidas con verrugas y un poro que brilla
        r = 1.8 + 2.4 * s
        cx, cy = bx + lean * 2, by - r * .8
        ellipse(cv, cx, cy, r, r * .85, pal[1:] if pal is not IVORY else IVORY[1:])
        cv.outline(); cv.clean()
        cv.put_in(cx - r * .4, cy - r * .5, C(pal[4]))
        if r > 2.6:
            cv.put_in(cx + .3, cy - r * .62, C(GLOW[gl])); cv.put_in(cx - .7, cy - r * .62, C(GLOW[max(1, gl - 1)]))
        for k in range(int(r)):
            a = f['ph'] + k * 2.1
            cv.put_in(cx + math.cos(a) * r * .55, cy + math.sin(a) * r * .45, C(pal[2]))
    elif species == 'Earthstars':                            # estrellas de tierra: rayos abiertos sobre el suelo y bola con poro que brilla
        r = 1.3 + 1.3 * s; R_ = r + 2.4 + 2.2 * s
        cx, cy = bx + lean * 2, by - 2
        nr = 6 + int(f['ph'] * 10) % 3
        rays = Canvas(24)
        for k in range(nr):
            a = f['ph'] + k * 2 * math.pi / nr
            for q in range(int(R_ * 2) + 1):
                d = q / 2
                if d < r * .6: continue
                w = max(.0, 1.6 * (1 - d / R_)) + .3
                for sgn in (-1, 0, 1):
                    if abs(sgn) > w: continue
                    x = cx + math.cos(a) * d - math.sin(a) * sgn * .8
                    y = cy + (math.sin(a) * d + math.cos(a) * sgn * .8) * .62
                    tone = 0 if d > R_ - .8 else 3 if (sgn == 0 and d < R_ * .7) else 2 if math.sin(a) < .2 else 1
                    rays.put(x, y, C((OCHRE if pal is not RUST else RUST)[tone]))
        rays.clean()                                         # sin contorno: los rayos son finos y se pegan al suelo
        for p, col in rays.px.items(): cv.px[p] = col
        ball = Canvas(24)
        ellipse(ball, cx, cy - r * .55, r, r * .85, IVORY[1:] if pal is not IVORY else RUST[1:])
        ball.outline(); ball.clean()
        for p, col in ball.px.items(): cv.px[p] = col
        cv.put_in(cx, cy - r * 1.15, C(GLOW[gl])); cv.put_in(cx - r * .45, cy - r * .9, C(IVORY[4]))
    else:                                                    # mazas (como el Cordyceps): tallos con la punta que brilla
        for k in range(1 + int(s > 1.0)):
            h = (4 + 6 * s) * (1 - k * .25); ln = lean + (k - 1) * .35
            x0 = bx + k * 2.4 * (-1 if f['flip'] else 1)
            for q in range(int(h)):
                cv.put(x0 + ln * q, by - q, C(OCHRE[2] if q < h * .6 else OCHRE[3]))
            tx, ty = x0 + ln * h, by - h
            t = Canvas(24)
            ellipse(t, tx, ty, 1.1 + .5 * s, 1.6 + .7 * s, OCHRE[1:] if pal is OCHRE else RUST[1:])
            t.outline()
            for p, col in t.px.items(): cv.px[p] = col
            cv.put_in(tx, ty - 1, C(GLOW[gl])); cv.put_in(tx - 1, ty, C(GLOW[max(1, gl - 1)]))
    # sombra de contacto en el pie
    sh = Canvas(24)
    for dx in range(-2, 3):
        _blend_put(sh, bx + dx, by + 1, OUTL, 110 if abs(dx) < 2 else 60)
    out = sh.img(); out.alpha_composite(cv.img())
    return out


def mat_frame(P, grow=1.0, fgrow=1.0, glow=0.0, t=0.0, burst=0.0):
    """Un cuadro de una mata: alfombra, hongos de atrás adelante y esporas que suben (t de 0 a 1 = un bucle)."""
    cv = Canvas(P['n']); draw_carpet(cv, P, grow, glow)
    im = cv.img(); c = P['c']
    for f in P['fungi']:
        fi = fungus(P['species'], f, fgrow, glow)
        if fi is None: continue
        im.alpha_composite(fi, (int(round(c + (f['x'] - c) * grow - 12)), int(round(c + (f['y'] - c) * grow - 18))))
    if fgrow >= 1:
        sp = Canvas(P['n'])
        for (dx, t0, ph) in P['spores']:                      # esporas: suben despacio, ondulan y se apagan
            tt = (t + t0) % 1
            x, y = c + dx + math.sin(ph + tt * 6.28) * 1.2, c - 2 - tt * P['R'] * .7
            _blend_put(sp, x, y, GLOW[3], int(200 * (1 - tt)))
        if burst:                                            # al dañar: soplo de esporas hacia fuera
            rnd = random.Random(7)
            for k in range(14):
                a = rnd.uniform(0, 2 * math.pi); d = P['R'] * (.3 + .6 * burst) * rnd.uniform(.7, 1.1)
                _blend_put(sp, c + math.cos(a) * d, c + math.sin(a) * d * .72 - 3 * burst, GLOW[3 if k % 3 else 4], int(230 * (1.15 - burst)))
        im.alpha_composite(sp.img())
    return im


def mat_sheets(species, size, seed):
    P = mat_plan(species, size, seed)
    appear = [mat_frame(P, grow=g, fgrow=fg) for g, fg in ((.35, 0), (.65, .3), (.9, .75), (1.0, 1.12), (1.0, 1.0))]
    idle = [mat_frame(P, t=i / 8, glow=.25 * math.sin(i * math.pi / 4)) for i in range(8)]
    pulse = [mat_frame(P, glow=g, t=0, burst=b) for g, b in ((1.0, .25), (.8, .6), (.45, .9), (.15, 1.0))]
    return {'Appear': appear, 'Idle': idle, 'Pulse': pulse}


def snare(i):
    """Hilos de micelio que se enredan en las patas de quien pisa la mata (16×16, bucle): suben, aprietan y se aflojan."""
    cv = Canvas(16); rnd = random.Random(3)
    h = (3, 5, 6, 4)[i]
    for k in range(5):
        x0 = 3 + k * 2.4 + rnd.uniform(-.5, .5)
        for q in range(h + rnd.randrange(2)):
            x = x0 + math.sin(q * 1.3 + k + i * .8) * 1.1
            _blend_put(cv, x, 14 - q, HYPHA[3] if q < 2 else HYPHA[2], 230 - q * 18)
    for k in range(3):
        _blend_put(cv, 4 + k * 4 + (i % 2), 14, GLOW[3], 220)
    return cv


def fungus_scene(M):
    """Una sala: al empezar el combate brotan 4 matas distintas; un escarabajo cruza una despacio y la mata le pulsa."""
    W, H = 240, 150
    bg = ground(W, H)
    old = load_sheet(os.path.join(HERE, '..', 'floor1-mutations', 'Effects', 'Mycelium.png'), 16)
    beetle = load_sheet(os.path.join(ENEMIES, 'Beetle', 'Beetle_Move.png'))
    spots = [(48, 48, 'Bells', 'M'), (168, 40, 'Puffballs', 'M'), (70, 112, 'Earthstars', 'L'), (180, 108, 'Clubs', 'L')]
    frames = []
    for f in range(120):
        im = bg.copy(); t = f * SCENE_MS / 1000
        paste_c(im, old[(f // 3) % len(old)].resize((32, 32), Image.NEAREST), 222, 16)   # el de ahora (piso 1), escalado
        for k, (x, y, sp, sz) in enumerate(spots):
            S = M[(sp, sz)]
            tk = t - .15 * k                                  # brotan una tras otra
            if tk < 0: continue
            a = int(tk * 1000 / 90)
            if a < len(S['Appear']): fr = S['Appear'][a]
            elif sp == 'Earthstars' and 2.6 < t < 5.4:        # el escarabajo pasa por la de repisas: pulsa cada 0,5 s
                q = int((t - 2.6) * 1000 / 90) % 6
                fr = S['Pulse'][q] if q < 4 else S['Idle'][(f // 2) % 8]
            else: fr = S['Idle'][(int(tk * 1000 / 110)) % 8]
            paste_c(im, fr, x, y)
        bx = -10 + (t - 1.2) * 34 if t < 2.6 else 37.6 + (t - 2.6) * 34 * .6 if t < 5.4 else 94.7 + (t - 5.4) * 34
        if t > 1.2:
            paste_c(im, beetle[(f // 2) % len(beetle)], bx, 104)
            if 2.6 < t < 5.4:
                paste_c(im, M['snare'][(f // 3) % 4], bx, 108)
        frames.append(im)
    save_scene('escena_matas_hongo', frames, [('Colonizar la sala (piso 3)', 4, 2), ('ahora', 206, 2)], keys=[6, 40, 70, 100])


def fungus_all():
    M = {}
    seeds = {'Bells': 11, 'Puffballs': 23, 'Earthstars': 37, 'Clubs': 41}
    rows = {}
    for sp in SPECIES:
        for sz in MAT_SIZES:
            S = mat_sheets(sp, sz, seeds[sp] + (100 if sz == 'L' else 0)); M[(sp, sz)] = S
            for anim, frames in S.items():
                save(f'FungusMat/Fx_FungusMat_{sp}_{sz}_{anim}.png', frames)
            rows[f'{sp} {sz}: brota + quieta'] = S['Appear'] + S['Idle'][:4]
            rows[f'{sp} {sz}: pulso'] = S['Pulse']
    M['snare'] = [snare(i) for i in range(4)]
    save('FungusMat/Fx_FungusMat_Snare.png', M['snare'])
    preview(os.path.join(OUT, 'vista_previa_matas_hongo.png'), rows, BG, scale=3)
    for sp in SPECIES: gifs(f'Fx_FungusMat_{sp}_L_Idle', M[(sp, 'L')]['Idle'], 110, 5)
    fungus_scene(M)


# =================================================================================================
# 5. Eryops (solo Diplocaulus, Peonza): acecha medio hundido en el lodo y sale cubierto de barro que se seca en 5 s
#    Se apoya en el lodo pintado por shader del pulido del piso 3 (floor3-polish): aquí van la textura seca, la estela,
#    las salpicaduras del +30 % y las escamas; la simulación del shader sirve para las escenas.
# =================================================================================================
_argv = sys.argv; sys.argv = [sys.argv[0], os.path.join(HERE, '_tmp')]
sys.path.insert(0, os.path.join(HERE, '..', 'floor3-polish'))
import generar_pulido_piso3 as PO                         # mud_cover, mud_state, fx_mud_ring, fx_mud_drip, MUD...
sys.argv = _argv
DRYMUD = ['#2e241c', '#4e3e2c', '#6e5a40', '#8e7a5a', '#b0a07c', '#cfc29e']   # barro seco: pardo grisáceo claro
LURK = .55                                                 # con Eryops, el nivel del lodo se queda en el 55 % del alto
LURK_SINK = .8                                             # y se hunde el 80 % de ese nivel (el lodo normal, el 50 %)


def lurk_state(sprite, level, t=0, n=40):
    """Como PO.mud_state, pero hundido LURK_SINK del nivel: solo asoman el lomo y la cabeza (cazador al acecho)."""
    old = PO.SINK_SHARE; PO.SINK_SHARE = LURK_SINK
    try: return PO.mud_state(sprite, level, t=t, n=n)
    finally: PO.SINK_SHARE = old


def dry_texture(n=16, seed=5):
    """Barro seco tileable (índices de DRYMUD): placas con luz arriba a la izquierda separadas por grietas oscuras."""
    rnd = random.Random(seed)
    cells = [(rnd.uniform(0, n), rnd.uniform(0, n)) for _ in range(7)]
    idx = [[0] * n for _ in range(n)]
    for y in range(n):
        for x in range(n):
            ds = sorted((((x + .5 - cx + n / 2) % n - n / 2) ** 2 + ((y + .5 - cy + n / 2) % n - n / 2) ** 2, k)
                        for k, (cx, cy) in enumerate(cells))
            d1, d2 = math.sqrt(ds[0][0]), math.sqrt(ds[1][0])
            if d2 - d1 < .9: idx[y][x] = 0                     # grieta
            else:
                cx, cy = cells[ds[0][1]]
                dx, dy = (x + .5 - cx + n / 2) % n - n / 2, (y + .5 - cy + n / 2) % n - n / 2
                lam = (-dx - dy) / 5
                idx[y][x] = 4 if lam > .45 else 3 if lam > -.2 else 2
                if d2 - d1 < 1.8 and lam < 0: idx[y][x] = 1    # borde de la placa en sombra
    return idx


DRYTEX = dry_texture()
MUD_SET = {c[:3]: i for i, c in enumerate(PO.MUD)}; MUD_SET[PO.MUD_WET[:3]] = 5


def coat_cover(sprite, level, dry=0.0, clean=0.0, t=0):
    """Simula el shader con Eryops fuera del lodo: el barro cubre hasta level y se seca (dry 0..1: parte ya seca, por
    el ruido del lodo) y al final se desprende (clean 0..1, como las manchas de siempre)."""
    if clean <= 0: cv = PO.mud_cover(sprite, level, t=t)
    else: cv = PO.mud_cover(sprite, 0, stain=level, clean=.2 + .8 * clean, t=t)
    for (x, y), c in list(cv.px.items()):
        i = MUD_SET.get(c[:3])
        if i is None: continue
        if PO.NOISE[(y * 2 + 3) % 32][(x * 3) % 32] < dry:
            k = DRYTEX[y % 16][x % 16]
            if k and i != 5: k = max(1, min(5, k + (1 if i >= 3 else 0) - (1 if i <= 1 else 0)))
            cv.px[(x, y)] = C(DRYMUD[k])
    return cv


def fx_mud_wake(i, n=4):
    """Estela en V del Eryops nadando en el lodo (32×12, bucle de 4, hacia la derecha: pivote en la punta, a la derecha
    en el centro; el código la gira con el movimiento). Dos surcos con labio claro que se abren hacia atrás, a trozos que
    corren hacia atrás, y se apagan al final."""
    cv = Canvas(32, 12)
    M = [PO.hexc(c) for c in PO.MUD]; WET = PO.hexc(PO.MUD_WET)
    for side in (-1, 1):
        for q in range(2, 31):
            u = q - i * 2                                        # 2 px por cuadro hacia atrás: bucle de 8 px en 4 cuadros
            if u % 8 in (6, 7) and q > 6: continue               # ondas a trozos
            x = 31 - q
            y = 6 + side * (q * .16 + .8)
            a = 255 if q < 20 else int(255 - (q - 20) * 18)
            cv.put(x, y, C(M[4] if side < 0 else M[3], a))       # labio
            cv.put(x, y + 1, C(M[1], a))                         # sombra del surco, debajo
            if u % 8 == 1 and q < 18: cv.put(x, y, C(WET, a))   # brillo húmedo que viaja
    for k in range(2):                                           # burbujas en el surco
        bx = 31 - 10 - k * 8 - (i * 2) % 8
        cv.put(bx, 6 + (k * 2 - 1), C(M[4], 220))
    return cv


def fx_mud_kick(i):
    """Marcador del +30 %: a cada paso saltan terrones de barro de las patas hacia atrás (24×24, 6 cuadros de 50 ms,
    dibujado yendo a la derecha: pivote abajo, en x = 16, = las patas; Flip X a la izquierda). Cuadro 0: el golpe."""
    cv = Canvas(24); t = i / 5
    M = [PO.hexc(c) for c in PO.MUD]; WET = PO.hexc(PO.MUD_WET)
    gx, gy = 16, 22
    for k, (vx, vy, big) in enumerate(((-10, -11, 2), (-13, -6, 1), (-6, -14, 1), (-14, -3, 0), (-8, -8, 0))):
        x = gx + vx * t; y = gy + vy * 2.2 * t + 24 * t * t
        if y >= gy + .5 and i > 1:                               # ha caído: manchita en el suelo
            for dx in range(big + 1): cv.put(x + dx, gy + 1, C(M[1 + dx % 2]))
            continue
        t_ = Canvas(24)
        if big == 2:
            for dx, dy, c in ((1, 0, WET), (2, 0, M[4]), (0, 1, M[4]), (1, 1, M[3]), (2, 1, M[3]), (3, 1, M[2]),
                              (0, 2, M[2]), (1, 2, M[2]), (2, 2, M[1]), (3, 2, M[1])):
                t_.put(x + dx, y + dy, C(c))
        elif big == 1:
            for dx, dy, c in ((0, 0, WET), (1, 0, M[3]), (0, 1, M[2]), (1, 1, M[1])): t_.put(x + dx, y + dy, C(c))
        else:
            t_.put(x, y, C(M[3])); t_.put(x + 1, y, C(M[2], 220))
        if big: t_.outline()
        for p_, c in t_.px.items(): cv.px[p_] = c
        if i < 3 and big:                                        # rastro corto
            cv.put(x + 3, y + 2.5, C(M[2], 170)); cv.put(x + 4.5, y + 3.5, C(M[1], 120))
    if i <= 1:                                                   # golpe: lodo que salta del pie
        for dx in range(-4, 3): cv.put(gx + dx, gy + 1, C(M[3] if dx % 2 else M[2]))
        cv.put(gx - 2, gy, C(M[4])); cv.put(gx + 1, gy, C(M[3]))
    return cv


def fx_mud_flake(i):
    """Escama de barro seco que se desprende al acabar el efecto (8×10, 5 cuadros, pivote arriba): cae y se deshace en polvo."""
    cv = Canvas(8, 10)
    y = [0, 2, 5, 8, 9][i]
    if i < 3:
        cv.put(3, y, C(DRYMUD[4])); cv.put(4, y, C(DRYMUD[3])); cv.put(3, y + 1, C(DRYMUD[2])); cv.put(4, y + 1, C(DRYMUD[1]))
        if i == 2: cv.put(5, y - 1, C(DRYMUD[3]))
    else:
        s = i - 3
        for dx in range(-1 - s, 3 + s): cv.put(3 + dx, 9, C(DRYMUD[2 + (dx % 2)], 230 - 80 * s))
        cv.put(2 - s, 8 - s, C(DRYMUD[4], 200 - 60 * s)); cv.put(5 + s, 8, C(DRYMUD[3], 200 - 60 * s))
    return cv


def mud_pool(im, x0, y0, cols, rows):
    """Charco de lodo profundo con los tiles del escenario del piso 3 (máscara: vecinos que NO son lodo, N=1 E=2 S=4 O=8)."""
    d = os.path.join(HERE, '..', 'floor3-environment', 'Swamp', 'mud')
    for r in range(rows):
        for c in range(cols):
            m = (r == 0) * 1 + (c == cols - 1) * 2 + (r == rows - 1) * 4 + (c == 0) * 8
            im.alpha_composite(Image.open(os.path.join(d, f'Mud_{m}.png')).convert('RGBA'), (x0 + c * 16, y0 + r * 16))


def diplo(anim, view, skin=False):
    """Cuadros del Diplocaulus; con skin, con la capa de la Piel anfibia encima (en el juego son dos capas y el shader del
    lodo pinta cada una; componerlas antes da lo mismo)."""
    body = load_sheet(os.path.join(HERE, '..', 'floor3-bodies', 'Diplocaulus', f'Player_Diplocaulus_{anim}_{view}.png'))
    if not skin: return body
    layer = load_sheet(os.path.join(HERE, '..', 'floor3-mutations', 'Skin', f'Skin_AmphibianSkin_Diplocaulus_{anim}_{view}.png'))
    out = []
    for b, l in zip(body, layer):
        c = b.copy(); c.alpha_composite(l); out.append(c)
    return out


def eryops_scene(S):
    """Dos filas: Diplocaulus normal (se hunde despacio y sale con manchas) y con Eryops (acecha medio hundido, va un 30 %
    más rápido con estela, sale cubierto de barro que se seca, salpica al correr y se le cae a los 5 s)."""
    W, H = 256, 236
    bg = ground(W, H)
    rows = [(46, False, False), (126, True, False), (206, True, True)]
    for gy, _, _ in rows: mud_pool(bg, 0, gy - 24, 6, 3)
    drip = PO.fx_mud_drip()
    frames = []
    N = 160                                                    # 8 s a 50 ms
    for f in range(N):
        im = bg.copy(); t = f * SCENE_MS / 1000
        for gy, ery, skin in rows:
            move, idle = diplo('Move', 'side', skin), diplo('Idle', 'side', skin)
            v_mud, v_out = (34 * 1.3, 30 * 1.3) if ery else (34 * .55, 30)
            mud_end = 84
            t_exit = (mud_end - 12) / v_mud
            if t < t_exit: x = 12 + v_mud * t
            else: x = min(232, mud_end + v_out * (t - t_exit))
            moving = x < 232
            fr = move[(f // 2) % len(move)] if moving else idle[(f // 3) % len(idle)]
            h = fr.getbbox()[3] - fr.getbbox()[1]
            if t < t_exit:                                     # en el lodo
                lvl = int(h * LURK) if ery else int(min(h * .85, h * .85 * min(1, t / 2.6)))
                if ery and moving:
                    wk = S['Fx_EryopsMudWake'][(f // 2) % 4].img()
                    im.alpha_composite(wk, (int(x - 30), gy - 7))
                im.alpha_composite((lurk_state if ery else PO.mud_state)(fr, lvl, t=f // 2), (int(x - 20), gy - 34))
                if not ery and f % 6 == 0: pass
            else:                                              # fuera del lodo
                te = t - t_exit
                if ery:
                    lvl = int(h * LURK)
                    if te < 5:
                        cv = coat_cover(fr, lvl, dry=min(1, max(0, (te - .8) / 3.2)), clean=max(0, (te - 4.4) / .6), t=f // 2)
                    else: cv = PO.to_cv(fr)
                    im.alpha_composite(PO.placed(cv.img()), (int(x - 20), gy - 34))
                    if te < 5 and moving:                      # marcador: terrones a cada paso (cada 0,25 s)
                        k = int(te * 1000 / SCENE_MS) % 5
                        im.alpha_composite(S['Fx_EryopsMudKick'][min(5, k)].img(), (int(x - 12 - 16), gy - 23))
                    if te < 1.6 and f % 7 == 0 or 0 < te % 1 < .05 and te < 1.6:
                        pass
                    if te < 1.4:                               # goterones de barro húmedo
                        k = int(te * 1000 / 80) % 6
                        im.alpha_composite(drip[k].img(), (int(x - 4), gy - 9))
                    if 4.2 < te < 5.2:                         # escamas que se caen
                        k = min(4, int((te - 4.2) * 1000 / 200))
                        for dx in (-6, 2, 7):
                            im.alpha_composite(S['Fx_EryopsMudFlake'][k].img(), (int(x + dx - 4), gy - 12))
                else:
                    mx = int(h * .85 * min(1, t_exit / 2.6))
                    cv = PO.mud_cover(fr, 0, stain=mx, clean=min(1, .25 + (x - mud_end) / 40 * .75), t=f // 2)
                    im.alpha_composite(PO.placed(cv.img()), (int(x - 20), gy - 34))
        frames.append(im)
    save_scene('escena_eryops_normal_vs_eryops', frames,
               [('Diplocaulus normal', 4, 2), ('Linaje Eryops', 4, 82), ('Linaje Eryops con Piel anfibia', 4, 162)],
               keys=[25, 50, 75, 110, 150])


def eryops_all():
    S = {'Fx_EryopsMudWake': [fx_mud_wake(i) for i in range(4)],
         'Fx_EryopsMudKick': [fx_mud_kick(i) for i in range(6)],
         'Fx_EryopsMudFlake': [fx_mud_flake(i) for i in range(5)]}
    for name, frames in S.items(): save('Eryops/' + name + '.png', frames)
    tex = Canvas(16)
    for y in range(16):
        for x in range(16): tex.put(x, y, C(DRYMUD[DRYTEX[y][x]]))
    save('Eryops/Fx_MudBody_TexDry.png', [tex])
    # lámina: el Diplocaulus acechando (nivel 55 %) en las 3 direcciones y el barro secándose fuera del lodo
    body = os.path.join(HERE, '..', 'floor3-bodies', 'Diplocaulus')
    rows = {}
    for v, skin in [(v, sk) for sk in (False, True) for v in ('side', 'down', 'up')]:
        fr = diplo('Idle', v, skin)[0]
        h = fr.getbbox()[3] - fr.getbbox()[1]
        lvl = int(h * LURK)
        rows[f'{v}{" + Piel anfibia" if skin else ""}: acecho / sale / se seca / se cae'] = (
            [lurk_state(fr, lvl, t=0), PO.placed(coat_cover(fr, lvl).img())] +
            [PO.placed(coat_cover(fr, lvl, dry=d).img()) for d in (.35, .7, 1.0)] +
            [PO.placed(coat_cover(fr, lvl, dry=1, clean=c).img()) for c in (.4, .8)])
    rows['Estela (32x12)'] = S['Fx_EryopsMudWake']
    rows['Terrones +30 %'] = S['Fx_EryopsMudKick']
    rows['Escamas'] = S['Fx_EryopsMudFlake'] + [tex]
    preview(os.path.join(OUT, 'vista_previa_eryops.png'), rows, BG, scale=5)
    eryops_scene(S)


# =================================================================================================
# 6. Cordyceps (Zombi): el enemigo muerto se levanta 4 s como aliado, pálido, con hongos y estela de esporas
#    El tinte y los ojos los hace un shader; aquí van los hongos (el código los reparte por arriba de la silueta, como
#    Fx_FlameLick), la estela, el brote al levantarse y el desmoronarse. La simulación del shader sirve para revisar.
# =================================================================================================
ZOMBIE_ENEMIES = [('Beetle', 'Move', 32), ('Toad', 'Hop', 32), ('Spider', 'Move', 32), ('Dragonfly', 'Fly', 40),
                  ('Calamites', 'Idle', 32), ('CalamitesSprout', 'Move', 16), ('Medullosa', 'Idle', 32)]   # (enemigo, anim, ancho)
ZTINT = (206, 210, 188)                                   # hacia donde se aclara el cuerpo (marfil verdoso)
ZTINT_K, ZDESAT = .32, .35                                # cuánto se acerca a ese color y cuánto pierde de saturación
EYE_W = (0xf4, 0xf4, 0xe8)
SPIDER_EYES = None                                        # colores rojos de los ojos de la araña (se miran en su hoja)


def zombie_tint(im):
    """Simula el shader: quita saturación, aclara hacia ZTINT (el contorno se queda) y los ojos brillan verde-azulado."""
    im = im.convert('RGBA').copy(); px = im.load()
    eye_hi, eye_lo = _rgb(GLOW[3]), _rgb(GLOW[2])
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if not a: continue
            c = (r, g, b)
            if c == OUTL[:3]: continue
            if c == EYE_W: px[x, y] = eye_hi + (a,); continue
            if SPIDER_EYES and c in SPIDER_EYES: px[x, y] = eye_lo + (a,); continue
            L = _lum(c)
            d = tuple(L + (v - L) * (1 - ZDESAT) for v in c)
            d = tuple(int(min(255, v + (t - v) * ZTINT_K + 10)) for v, t in zip(d, ZTINT))
            px[x, y] = d + (a,)
    return im


def zfungus(k, g=1.0, ph=0.0):
    """Hongo zombi k (8×12, pivote abajo en el centro = punto de la silueta donde brota). g: lo crecido; ph: balanceo."""
    cv = Canvas(8, 12)
    if g <= .05: return cv
    sway = math.sin(ph) * .6
    bx, by = 4, 11
    if k in (0, 1):                                       # maza de Cordyceps: tallo ocre y punta que brilla
        h = (6 if k == 0 else 4.5) * g
        for q in range(int(h)):
            cv.put(bx + sway * q / max(1, h), by - q, C(OCHRE[2] if q < h * .6 else OCHRE[3]))
            if q < h * .4: cv.put(bx + 1 + sway * q / max(1, h), by - q, C(OCHRE[1]))
        tx, ty = bx + sway, by - h
        t = Canvas(8, 12); ellipse(t, tx, ty, 1.5 * g + .2, 2.1 * g + .2, OCHRE[1:]); t.outline()
        for p, c in t.px.items(): cv.px[p] = c
        cv.put_in(tx, ty - 1, C(GLOW[3])); cv.put_in(tx - 1, ty, C(GLOW[2])); cv.put_in(tx, ty - 2, C(GLOW[4]))
    elif k == 2:                                          # dos tallitos finos de distinta altura
        for dx, h in ((-1, 5 * g), (1.5, 3.5 * g)):
            for q in range(int(h)): cv.put(bx + dx + sway * q / 6, by - q, C(STALK[1] if q < h * .5 else STALK[2]))
            cv.put(bx + dx + sway * h / 6, by - h, C(GLOW[3]))
    else:                                                 # sombrerillo pálido
        h = 3 * g
        for q in range(int(h)): cv.put(bx, by - q, C(STALK[2]))
        t = Canvas(8, 12)
        fill(t, lambda x, y: ((x + .5 - bx - sway) / (2.2 * g + .3)) ** 2 + ((y + .5 - (by - h)) / (1.6 * g + .2)) ** 2 <= 1
             and y + .5 <= by - h + .5, bx + sway, by - h, 2.5, 2, IVORY[1:], box=(0, 0, 8, 12))
        t.outline()
        for p, c in t.px.items(): cv.px[p] = c
        cv.put_in(bx + sway, by - h + .5, C(GLOW[2]))
    return cv


def zspore_trail(i):
    """Estela de esporas (12×12, 6 cuadros de 90 ms, pivote abajo en el centro): un soplo que sube, deriva y se apaga."""
    cv = Canvas(12); t = i / 5; rnd = random.Random(12)
    for k in range(6):
        x0, ph = rnd.uniform(3, 9), rnd.uniform(0, 6.28)
        x = x0 + math.sin(ph + t * 5) * 1.2
        y = 10 - t * rnd.uniform(5, 8) - k % 2
        a = int(230 * (1 - t) ** .7)
        if a > 20: cv.put(x, y, C(GLOW[3] if k % 3 else HYPHA[3], a))
    if i < 2:
        for dx in (-1, 0, 1): cv.put(6 + dx * 2, 11, C(HYPHA[2], 160))
    return cv


def zrise(i):
    """Brote al levantarse (48×48, 8 cuadros de 65 ms, pivote abajo en el centro): hilos que suben del suelo, anillo de
    esporas y destello verde-azulado. Mientras, el código pasa la muerte del enemigo al revés, ya con el tinte."""
    cv = Canvas(48); t = i / 7; rnd = random.Random(21); c, gy = 24, 44
    for k in range(9):                                    # hilos de micelio que suben y se retiran
        x0 = c + rnd.uniform(-14, 14); h = rnd.uniform(10, 22) * math.sin(min(1, t * 1.6) * math.pi * .5 + (t > .6) * 0)
        h *= 1 - max(0, (t - .6) / .4)
        for q in range(int(h)):
            x = x0 + math.sin(q * .5 + k) * 1.2 * (q / 10)
            cv.put(x, gy - q, C(HYPHA[3] if q < 3 else HYPHA[2], int(230 - q * 6)))
    R = 4 + t * 18                                        # anillo de esporas que se abre
    for k in range(22):
        a = k * 2 * math.pi / 22 + rnd.uniform(-.1, .1)
        al = int(240 * (1 - t))
        if al > 20: cv.put(c + math.cos(a) * R, gy - 6 + math.sin(a) * R * .45 - t * 6, C(GLOW[3] if k % 2 else GLOW[4], al))
    if i in (1, 2, 3):                                    # destello en el centro
        for dx, dy in ((0, 0), (-1, 0), (1, 0), (0, -1), (0, 1)): cv.put(c + dx, gy - 12 + dy, C(GLOW[4] if (dx, dy) == (0, 0) else GLOW[3]))
    return cv


def zcrumble(i):
    """Cae de verdad (48×48, 8 cuadros de 80 ms, pivote abajo en el centro): el cuerpo se deshace en una nube de esporas
    pálidas que se abre y se apaga, y caen trocitos de hongo. El código apaga el cuerpo en los cuadros 0-2."""
    cv = Canvas(48); t = i / 7; rnd = random.Random(33); c, gy = 24, 44
    for k in range(40):
        a = rnd.uniform(0, 2 * math.pi); d = rnd.uniform(0, 1) ** .6 * (6 + t * 14)
        x = c + math.cos(a) * d; y = gy - 10 + math.sin(a) * d * .6 - t * rnd.uniform(4, 10)
        al = int(230 * (1 - t) ** .8 * rnd.uniform(.6, 1))
        if al > 20: cv.put(x, y, C((HYPHA[2], HYPHA[3], GLOW[3], GLOW[2])[k % 4], al))
    for k in range(5):                                    # trocitos de hongo que caen
        x = c + rnd.uniform(-10, 10); y = gy - 14 + t * 14 + rnd.uniform(-2, 2)
        if y < gy: cv.put(x, y, C(OCHRE[3])); cv.put(x, y + 1, C(OCHRE[1]))
        else: cv.put(x, gy, C(OCHRE[1], int(200 * (1 - t))))
    if i <= 2:                                            # polvo en el suelo
        for dx in range(-8 - i * 2, 9 + i * 2, 2): cv.put(c + dx, gy, C(HYPHA[1], 180 - i * 50))
    return cv


def silhouette_tops(im, n=3, seed=0):
    """Puntos de la parte de arriba de la silueta donde brotan los hongos (lo que hará el código, como con las lenguas de fuego)."""
    px = im.load(); bb = im.getbbox()
    if not bb: return []
    cols = [x for x in range(bb[0] + 2, bb[2] - 2)]
    rnd = random.Random(seed)
    picks = sorted(rnd.sample(cols, min(n, len(cols)))) if cols else []
    out = []
    for x in picks:
        for y in range(bb[1], bb[3]):
            if px[x, y][3]: out.append((x, y + 1)); break
    return out


def zombify(im, fungi, g=1.0, f=0):
    """Enemigo zombi entero: tinte + hongos en la parte de arriba de la silueta (fungi = [(k, fracción x)])."""
    pad = Image.new('RGBA', (im.width, im.height + 10), (0, 0, 0, 0)); pad.alpha_composite(im.convert('RGBA'), (0, 10))
    im = pad                                              # 10 px de aire arriba: los hongos son sprites aparte en el juego
    z = zombie_tint(im); bb = im.getbbox()
    if not bb: return z
    px = im.load()
    for j, (k, fx) in enumerate(fungi):
        x = int(bb[0] + 2 + fx * max(1, bb[2] - bb[0] - 4))
        top = next((y for y in range(bb[1], bb[3]) if px[x, y][3]), bb[1])
        z.alpha_composite(zfungus(k, g, f * .5 + j * 1.7).img(), (x - 4, top + 2 - 12))
    return z


def zombie_scene(S):
    """Muere un escarabajo, se levanta zombi, va a por el otro dejando esporas, le ataca y a los 4 s cae de verdad."""
    W, H = 200, 90
    bg = ground(W, H)
    move = load_sheet(os.path.join(ENEMIES, 'Beetle', 'Beetle_Move.png'))
    spray = load_sheet(os.path.join(ENEMIES, 'Beetle', 'Beetle_Spray.png'))
    death = load_sheet(os.path.join(ENEMIES, 'Beetle', 'Beetle_Death.png'))
    idle = load_sheet(os.path.join(ENEMIES, 'Beetle', 'Beetle_Idle.png'))
    fungi = [(0, .25), (2, .55), (1, .8)]
    gy = 60; B = 160
    frames, trail = [], []
    N = 130
    for f in range(N):
        im = bg.copy(); t = f * SCENE_MS / 1000
        # el otro escarabajo (enemigo de verdad), quieto mirando a la izquierda
        paste_c(im, idle[(f // 3) % len(idle)].transpose(Image.FLIP_LEFT_RIGHT), B, gy)
        x = 40
        if t < .6:                                        # muere
            paste_c(im, death[min(len(death) - 1, int(t / .6 * len(death)))], x, gy - 4)
        elif t < 1.1:                                     # se levanta: muerte al revés, ya pálido, y el brote
            k = int((t - .6) / .5 * len(death)); fr = death[max(0, len(death) - 1 - k)]
            paste_c(im, zombify(fr, fungi, g=(t - .6) / .5, f=f), x, gy - 9)
            ri = min(7, int((t - .6) * 1000 / 65)); im.alpha_composite(S['Fx_ZombieRise'][ri].img(), (x - 24, gy + 12 - 48))
        else:
            tz = t - 1.1
            if tz < 2.2: x = 40 + tz * 40; fr = move[(f // 2) % len(move)]
            elif tz < 4.0: x = 128; fr = spray[(f // 2) % len(spray)]
            else: x = 128; fr = move[0]
            if tz < 4.0 and f % 3 == 0: trail.append((x - 8, gy + 10, f))
            for (tx, ty, f0) in trail:                    # estela de esporas
                k = (f - f0) * SCENE_MS // 90
                if k < 6: im.alpha_composite(S['Fx_ZombieSporeTrail'][k].img(), (int(tx - 6), ty - 12))
            if tz < 4.0: paste_c(im, zombify(fr, fungi, f=f), x, gy - 5)
            else:
                ci = int((tz - 4.0) * 1000 / 80)
                if ci < 3:
                    z = zombify(fr, fungi, f=f); a = z.split()[3].point(lambda v: int(v * (1 - ci / 3)))
                    z.putalpha(a); paste_c(im, z, x, gy - 5)
                if ci < 8: im.alpha_composite(S['Fx_ZombieCrumble'][ci].img(), (int(x - 24), gy + 12 - 48))
        frames.append(im)
    save_scene('escena_cordyceps_zombi', frames, [('Cordyceps: muere, se levanta aliado 4 s y cae', 4, 2)],
               keys=[10, 18, 40, 75, 110, 122])


def zombie_all():
    global SPIDER_EYES
    sp = load_sheet(os.path.join(ENEMIES, 'Spider', 'Spider_Idle.png'))[0]
    SPIDER_EYES = {_rgb(h) for h in ('#e04040',)} | {c[:3] for c in sp.getdata() if c[3] and c[0] > 180 and c[1] < 90}
    S = {'Fx_ZombieSporeTrail': [zspore_trail(i) for i in range(6)],
         'Fx_ZombieRise': [zrise(i) for i in range(8)],
         'Fx_ZombieCrumble': [zcrumble(i) for i in range(8)]}
    for k in range(4):
        S[f'Fx_ZombieFungus_{k}_Sprout'] = [zfungus(k, g) for g in (.3, .6, .9, 1.1)]
        S[f'Fx_ZombieFungus_{k}_Idle'] = [zfungus(k, 1.0, i * math.pi / 2) for i in range(4)]
    for name, frames in S.items(): save('Zombie/' + name + '.png', frames)
    rows = {}
    for en, anim, cw in ZOMBIE_ENEMIES:
        fr = load_sheet(os.path.join(ENEMIES, en, f'{en}_{anim}.png'), cw)
        fungi = [(0, .2), (2, .5), (1, .8)] if fr[0].width >= 32 else [(1, .3), (3, .7)]
        rows[f'{en} {anim}: normal'] = fr
        rows[f'{en} {anim}: zombi'] = [zombify(f_, fungi, f=i * 2) for i, f_ in enumerate(fr)]
    preview(os.path.join(OUT, 'vista_previa_zombis.png'), rows, BG, scale=4)
    fx = {k: [v.img() for v in S[k]] for k in S}
    preview(os.path.join(OUT, 'vista_previa_zombis_efectos.png'), fx, BG, scale=5)
    zombie_scene(S)


# =================================================================================================
if __name__ == '__main__':
    want = set(sys.argv[1:]) or {'parasite', 'jet', 'leech', 'fungus', 'eryops', 'zombie'}
    if 'parasite' in want: parasite_sheets()
    if 'jet' in want: jet_all()
    if 'leech' in want: leech_all()
    if 'fungus' in want: fungus_all()
    if 'eryops' in want: eryops_all()
    if 'zombie' in want: zombie_all()
