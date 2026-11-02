"""Escenas montadas de la Megarachne (6i): los momentos completos con el árbol del Nido, los cables y la rana.

- entrada:  los cables tiemblan, se encienden los ojos en el embudo, sale, va al borde de la copa, baja colgada, aterriza
            delante de la rana y amenaza (nombre).
- enfado:   al 50 % vuelve al Nido por un cable, se mete en el embudo, chilla dentro (ondas), el árbol se sacude y cae
            el agua, la copa se seca y sale enfurecida.
- desplome: (ya en el enfado) la rana prende un cable junto al árbol; el fuego llega al Nido, arde la copa seca, el
            fuego baja por los otros cables, la alcanza en el suyo y cae aturdida; quedan la copa carbonizada y los cabos.
- muerte:   aturdida en el suelo, último golpe: rueda panza arriba (vientre de euriptérido) y la red se descuelga.

Las piezas son las de las hojas (Sheets/, Sheets/Rage/, Nido/, Effects/); el montaje (posiciones, hilos, sombras,
tiempos) es orientativo para el código.

6j: rafaga (ráfaga de seda desde el cable, telarañas que frenan y arden), red (aviso, cae, atrapa, jirones), saco
(revienta en crías que muerden; otro roto antes), salto (marca ámbar → roja, polvo), recompensa (capullo con el
contenedor de corazón).

Uso: python generar_escenas_megarachne.py [entrada enfado desplome muerte rafaga red saco salto recompensa]   (sin nada: todas)
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import generar_megarachne as MG
import generar_nido_megarachne as NG
import generar_escala_megarachne as E
from PIL import Image, ImageDraw

N = MG.N
W, H = 420, 300
FOOT = (226, 190)                                       # pie del árbol del Nido en la escena
OX, OY = FOOT[0] - NG.FX, FOOT[1] - NG.FY               # desplazamiento de la celda del árbol
CROWN_H = NG.FY - NG.CY                                 # altura de la copa sobre el suelo (64 px)
FUNNEL = (OX + NG.CX, OY + NG.CY)                       # centro del embudo en pantalla
SIG = {'NO': (40, 118), 'NE': (400, 104), 'SO': (64, 292)}    # pie de cada Sigillaria (ancla del cable)
FROG_AT = (120, 236)

# -------------------------------------------------------------------------------------------------
# Sprites (con caché): cuerpo en 16 direcciones con Flip X; lo de 1 dirección (colgada, embudo) tal cual
# -------------------------------------------------------------------------------------------------
_C = {}
def spr(name, k, ang=-90.0, rage=False, shadow=False):
    single = name in MG.HANG or name in MG.INTRO
    table = MG.HANG.get(name) or MG.INTRO.get(name) or MG.ANIMS.get(name) or MG.MOMENTS.get(name)
    pose = dict(table[k % len(table)])
    if rage: pose['rage'] = True
    if single:
        key = (name, k % len(table), rage)
        if key not in _C: _C[key] = MG.render(MG.HANG_YAW, **pose)
        return _C[key][1 if shadow else 0]
    q = round(ang / 22.5) * 22.5
    q = (q + 180) % 360 - 180
    flip = abs(q) > 90
    base = (180 - q if q > 0 else -180 - q) if flip else q
    key = (name, k % len(table), base, rage)
    if key not in _C: _C[key] = MG.render(base, **pose)
    im = _C[key][1 if shadow else 0]
    return im.transpose(Image.FLIP_LEFT_RIGHT) if flip else im

def put(im, sprite, x, y):
    im.alpha_composite(sprite, (int(x - sprite.width // 2), int(y - sprite.height // 2)))

def put_shadow(im, sh, x, y, a=120):
    s_ = sh.copy(); s_.putalpha(s_.getchannel('A').point(lambda v: a if v else 0))
    im.alpha_composite(s_, (int(x - N // 2), int(y - N // 2)))

def ang_to(x0, y0, x1, y1):
    """Ángulo de la dirección (0 = derecha, 90 = arriba) de un vector en pantalla (picada: y se estira)."""
    return math.degrees(math.atan2(-(y1 - y0) / .82, x1 - x0))

# -------------------------------------------------------------------------------------------------
# Escenario: suelo, Sigillarias, cables (normales, que tiemblan, que arden, que se descuelgan), árbol
# -------------------------------------------------------------------------------------------------
_BASE = {}
def base():
    if 'b' not in _BASE:
        im = MG.smoke_floor(W, H, seed=21)
        sig = Image.open(os.path.join(MG.MT, 'floor3-environment', 'Smoke', 'props', 'Obstacle_Sigillaria.png')).convert('RGBA')
        for name, (x, y) in SIG.items():
            im.alpha_composite(sig, (x - sig.width // 2, y - sig.height + 4))
        _BASE['b'] = im
    return _BASE['b'].copy()

def cable_ends(name):
    """(p0 alto en la Sigillaria, p1 alto en la rama del árbol, g0 suelo bajo p0, g1 suelo bajo p1)."""
    sx, sy = SIG[name]
    tx, ty = NG.anchor(name)
    p0, p1 = (sx, sy - 44), (OX + tx, OY + ty)
    return p0, p1, (sx, sy), (p1[0], p1[1] + CROWN_H)

def cable_pts(name, n=40, sag=8, wobble=0.0, j=0):
    p0, p1, _, _ = cable_ends(name)
    return [(p0[0] + (p1[0] - p0[0]) * t, p0[1] + (p1[1] - p0[1]) * t + sag * 4 * t * (1 - t)
             + wobble * math.sin(t * math.pi * 3 + j * 1.7) * math.sin(t * math.pi)) for t in [i / n for i in range(n + 1)]]

def draw_cable_shadows(im, names):
    for nme in names:
        _, _, g0, g1 = cable_ends(nme)
        E.cable_shadow(im, (g0[0], g0[1] - 6), (g1[0], g1[1] - 6))

def draw_cable(im, name, wobble=0.0, j=0, sag=8):
    """Cable normal (seda trenzada) por la línea de cable_pts (permite que tiemble)."""
    pts = cable_pts(name, sag=sag, wobble=wobble, j=j)
    for a, b in zip(pts[::4], pts[4::4]):
        E.cable(im, a, b, sag=0, seed=hash(name) % 7)
    return pts

def on_cable(name, t, n=40):
    """Punto del cable (alto) y del suelo bajo él para t de 0 (Sigillaria) a 1 (árbol)."""
    pts = cable_pts(name, n)
    hx, hy = pts[round(t * n)]
    _, _, g0, g1 = cable_ends(name)
    return (hx, hy), (g0[0] + (g1[0] - g0[0]) * t, g0[1] + (g1[1] - g0[1]) * t)

def tree_img(state='wet', web=True, shake_k=None, eyes_k=None, rain_k=None, decay_k=None, frame=None):
    if frame is not None: t = frame.copy()
    else: t = NG.tree(mode=state) if state in ('wet', 'dry', 'charred') else state
    if shake_k is not None: t = NG.shake(t, *((2, 0), (-2, 1), (2, -1), (-2, 0), (1, 1), (0, 0))[shake_k % 6])
    if web and decay_k is None: t.alpha_composite(NG.nest_layer())
    if decay_k is not None: t.alpha_composite(NG.web_decay(decay_k))
    if eyes_k is not None: t.alpha_composite(NG.eyes(eyes_k))
    if rain_k is not None: t.alpha_composite(NG.rain(rain_k))
    return t

_T = {}
def tree_cached(key, fn):
    if key not in _T: _T[key] = fn()
    return _T[key]

def caption(im, txt):
    d = ImageDraw.Draw(im)
    d.text((6, H - 16), txt, fill=(255, 240, 200), font=MG.F(11), stroke_width=2, stroke_fill=(10, 10, 14))

def banner(im, text, a):
    if a <= 0: return
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    f = MG.F(26)
    w = d.textlength(text, font=f)
    d.rectangle([0, 18, W, 54], fill=(10, 8, 12, int(150 * a)))
    d.text(((W - w) / 2, 20), text, fill=(255, 210, 120, int(255 * a)), font=f, stroke_width=3, stroke_fill=(40, 10, 10, int(255 * a)))
    im.alpha_composite(lay)

def frog(im):
    f = Image.open(os.path.join(MG.MT, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')).convert('RGBA').crop((0, 0, 32, 32))
    im.alpha_composite(f, (FROG_AT[0] - 16, FROG_AT[1] - 24))

def layer_tree(im, t):
    im.alpha_composite(t, (OX, OY))

# =================================================================================================
# Escenas
# =================================================================================================
def escena_entrada():
    frames = []
    wet = tree_cached('wet', lambda: NG.tree(mode='wet'))
    cables = ('NO', 'NE', 'SO')
    edge = (FUNNEL[0], FUNNEL[1] + 34)                  # borde delantero de la copa
    land = (edge[0], edge[1] + CROWN_H)                 # suelo bajo el borde
    for j in range(70):
        im = base(); draw_cable_shadows(im, cables); frog(im)
        wob = 2.0 if j < 16 else 0.0
        eyes_k = min(5, j - 8) if 8 <= j < 17 else None
        txt = ''
        hang = None; on_crown = None; ground = None
        if j < 8: txt = 'entra la rana: se cierran las raíces; los cables tiemblan'
        elif j < 16: txt = 'se encienden los ojos en el fondo del embudo'
        elif j < 24: on_crown = ('IntroEmerge', j - 16); txt = 'asoma y sale del embudo'
        elif j < 30:
            t = (j - 24) / 5
            on_crown = ('Walk', j, (FUNNEL[0], FUNNEL[1] + (edge[1] - FUNNEL[1]) * t)); txt = 'va al borde de la copa'
        elif j < 38:
            t = ((j - 30) / 7) ** 1.5
            d0 = N // 2 - MG.hang_origin(MG.HANG['Drop'][0])[1]
            hang = ('Drop', j, edge[1] + d0 + (land[1] - edge[1] - d0) * t); txt = 'baja colgada de su hilo'
        elif j < 41: ground = ('Land', j - 38, -90); txt = 'aterriza'
        else: ground = ('Threat', min(5, j - 41) if j < 47 else 2 + (j - 47) % 4, ang_to(*land, *FROG_AT)); txt = 'amenaza'
        t_img = tree_cached(('wet', wob > 0, j % 6 if eyes_k is None else 100 + eyes_k),
                            lambda: tree_img('wet', eyes_k=eyes_k)) if eyes_k is not None else tree_cached('wetweb', lambda: tree_img('wet'))
        layer_tree(im, t_img)
        for c in cables: draw_cable(im, c, wobble=wob, j=j)
        if on_crown:
            name, k = on_crown[0], on_crown[1]
            if name == 'IntroEmerge': put(im, spr(name, k), *FUNNEL)
            else: put(im, spr('Walk', k, -90), *on_crown[2])
        if hang:
            name, k, y = hang
            ox_, oy_ = MG.hang_origin(MG.HANG[name][0])
            put_shadow(im, spr(name, k, shadow=True), land[0], land[1], a=int(60 + 70 * (y - edge[1]) / (land[1] - edge[1])))
            MG.draw_thread(im, edge[0], edge[1], edge[0] - N // 2 + ox_, y - N // 2 + oy_)
            put(im, spr(name, k), edge[0], y)
        if ground:
            name, k, a = ground
            put_shadow(im, spr(name, k, a, shadow=True), *land); put(im, spr(name, k, a), *land)
        if j >= 43: banner(im, 'MEGARACHNE', min(1, (j - 43) / 4))
        caption(im, txt); frames.append(im)
    return frames

def escena_enfado():
    frames = []
    cables = ('NO', 'NE', 'SO')
    wet_web = tree_cached('wetweb', lambda: tree_img('wet'))
    drying = tree_cached('drying', NG.drying)
    sc = [NG.screech(k) for k in range(6)]
    dry_web = tree_cached('dryweb', lambda: tree_img('dry'))
    for j in range(72):
        im = base(); draw_cable_shadows(im, cables); frog(im)
        rage = j >= 44
        txt, t_img, shake_k, rain_k = '', wet_web, None, None
        body = None
        if j < 10:                                       # vuelve al Nido por su cable
            t = .25 + .75 * j / 9
            (hx, hy), (gx, gy) = on_cable('NE', t)
            p0, p1, _, _ = cable_ends('NE')
            body = ('cable', spr('Walk', j, ang_to(*p0, *p1)), (hx, hy), (gx, gy), spr('Walk', j, ang_to(*p0, *p1), shadow=True))
            txt = 'al 50 %: vuelve al Nido'
        elif j < 14:
            _, p1, _, _ = cable_ends('NE'); t = (j - 10) / 3
            body = ('crown', spr('Walk', j, ang_to(*p1, *FUNNEL)), (p1[0] + (FUNNEL[0] - p1[0]) * t, p1[1] + (FUNNEL[1] - p1[1]) * t))
            txt = 'al 50 %: vuelve al Nido'
        elif j < 20:
            body = ('crown', spr('IntroEmerge', 7 - (j - 14)), FUNNEL); txt = 'se mete en el embudo'
        elif j < 38:
            k = (j - 20) % 6
            shake_k, rain_k = k, j % 8
            t_img = tree_cached(('shake', k, j % 8), lambda: tree_img('wet', shake_k=k, rain_k=j % 8))
            body = ('crown', spr('Screech', k), FUNNEL)
            txt = 'chilla dentro: el árbol se sacude y cae el agua'
        elif j < 44:
            t_img = tree_cached(('dryingweb', j), lambda: tree_img(frame=drying[j - 38]))
            if j < 41:
                rk = j % 8
                t_img = t_img.copy(); t_img.alpha_composite(NG.rain(rk))
            body = ('crown', spr('Screech', 5, rage=True), FUNNEL); txt = 'la copa se seca'
        elif j < 52:
            t_img = dry_web; body = ('crown', spr('IntroEmerge', j - 44, rage=True), FUNNEL); txt = 'sale enfurecida'
        else:
            t_img = dry_web
            body = ('crown', spr('Threat', 2 + (j - 52) % 4, -90, rage=True), FUNNEL); txt = 'enfado: más rápida, más hilos y crías; el Nido ya puede arder'
        layer_tree(im, t_img)
        for c in cables: draw_cable(im, c)
        if 20 <= j < 38:
            f = sc[(j - 20) % 6]; im.alpha_composite(f, (FUNNEL[0] - 96, FUNNEL[1] - 64))
        if body:
            if body[0] == 'cable':
                _, s, (hx, hy), (gx, gy), sh = body
                put_shadow(im, sh, gx, gy, a=70); put(im, s, hx, hy)
            else: put(im, body[1], *body[2])
        caption(im, txt); frames.append(im)
    return frames

def escena_desplome():
    frames = []
    cables = ('NO', 'NE', 'SO')
    n = 40
    burn = tree_cached('burn', NG.burn)
    smold = [tree_cached(('smold', k), lambda: NG.smolder(k)) for k in range(4)]
    dry_web = tree_cached('dryweb', lambda: tree_img('dry'))
    fx = [MG.fx_stunned(i) for i in range(8)]
    rnd = random.Random(3)
    t_burnt = {c: [None] * (n + 1) for c in cables}
    ign = {'SO': 30, 'NO': n, 'NE': n}                  # SO prende cerca del árbol; los otros, desde el árbol
    start = {'SO': 3}
    step_pts = lambda c: MG.FIRE_PX / (math.dist(*cable_ends(c)[:2]) / n)
    st = 'cable'; sx_t = .82; fall_y = 0.0; k_anim = 0
    gone = {}
    for j in range(80):
        im = base(); draw_cable_shadows(im, [c for c in cables if c not in gone]); frog(im)
        if j < 3:
            d = ImageDraw.Draw(im); p = cable_pts('SO')[30]
            d.line([(FROG_AT[0] + 10, FROG_AT[1] - 10), p], fill=(255, 170, 60, 220), width=1)
        fronts = {}
        for c in cables:
            fronts[c] = -1 if c not in start or j < start[c] else (j - start[c]) * step_pts(c)
        nest_j = None
        if 'SO' in start and fronts['SO'] >= n - ign['SO']:
            start.setdefault('NEST', j)
        if 'NEST' in start:
            nest_j = j - start['NEST']
            for c in ('NO', 'NE'):
                if nest_j >= 6: start.setdefault(c, j)  # el fuego baja del Nido por los otros cables
        # árbol
        if nest_j is None: t_img = dry_web
        elif nest_j < 16: t_img = burn[nest_j // 2]
        else: t_img = smold[j % 4]
        layer_tree(im, t_img)
        # cables
        for c in cables:
            if c in gone:
                kf = max(0.0, min(1.0, 1.6 - (j - gone[c]) / 20))
                p0, p1, _, _ = cable_ends(c)
                MG.stub(im, p0[0], p0[1], 1 if p1[0] > p0[0] else -1, kf)
                MG.stub(im, p1[0], p1[1], 1 if p0[0] > p1[0] else -1, kf)
                continue
            pts = cable_pts(c)
            if fronts[c] < 0: draw_cable(im, c); continue
            for i in range(n + 1):
                if t_burnt[c][i] is None and abs(i - ign[c]) <= fronts[c] - 6: t_burnt[c][i] = j
            MG.burning_cable(im, pts, ign[c], fronts[c], t_burnt[c], j, rnd)
            if fronts[c] > max(ign[c], n - ign[c]) + 6: gone.setdefault(c, j + 5)
        # ella: en el cable NO, huye hacia la Sigillaria; el fuego la alcanza
        if st == 'cable':
            if fronts['NO'] >= 0: sx_t = max(.05, sx_t - .016)
            (hx, hy), (gx, gy) = on_cable('NO', sx_t)
            p0, p1, _, _ = cable_ends('NO')
            a = ang_to(*p1, *p0) if fronts['NO'] >= 0 else ang_to(*p0, *p1)
            put_shadow(im, spr('Walk', j, a, rage=True, shadow=True), gx, gy, a=70); put(im, spr('Walk', j, a, rage=True), hx, hy)
            if fronts['NO'] >= 0 and (n - fronts['NO']) / n <= sx_t + .02:
                st, fall_x, fall_y0, ground_y, k_anim = 'fall', hx, hy, gy, 0
            txt = 'la rana prende un cable junto al árbol' if j < 6 else 'el fuego llega al Nido: arde la copa seca' if nest_j is not None and nest_j < 10 else 'el fuego baja por los cables'
        elif st == 'fall':
            k_anim += 1
            y = min(ground_y, fall_y0 + (ground_y - fall_y0) * (k_anim / 6) ** 1.6)
            put_shadow(im, spr('Fall', min(3, k_anim), 0, rage=True, shadow=True), fall_x, ground_y, a=110)
            put(im, spr('Fall', min(3, k_anim), 0, rage=True), fall_x, y)
            if y >= ground_y: st, k_anim = 'stun', 0
            txt = 'la alcanza: cae'
        else:
            put_shadow(im, spr('Stunned', k_anim, 0, rage=True, shadow=True), fall_x, ground_y, a=130)
            put(im, spr('Stunned', k_anim, 0, rage=True), fall_x, ground_y)
            hx_, hy_ = MG.head_top(MG.ANIMS['Stunned'][0], 0)
            im.alpha_composite(fx[k_anim % 8], (int(fall_x - N // 2 + hx_ - 32), int(ground_y - N // 2 + hy_ - 14)))
            k_anim += 1
            txt = 'aturdida mucho rato (gran ventana); sin Nido ni refugio'
        caption(im, txt); frames.append(im)
    return frames

def escena_muerte():
    frames = []
    cables = ('NO', 'NE', 'SO')
    dry = tree_cached('dry', lambda: NG.tree(mode='dry'))
    at = (FROG_AT[0] + 92, FROG_AT[1] + 2)
    a = ang_to(*at, *FROG_AT)
    for j in range(64):
        im = base(); frog(im)
        dk = None if j < 14 else min(5, (j - 14) // 3)
        t_img = tree_cached(('decay', dk), lambda: tree_img(frame=dry, decay_k=dk) if dk is not None else tree_img(frame=dry))
        layer_tree(im, t_img)
        sagk = 0 if j < 14 else min(1.0, (j - 14) / 16)
        for c in cables:                                  # la red se descuelga: los cables ceden y caen
            if sagk < 1:
                draw_cable(im, c, sag=8 + 50 * sagk ** 1.5)
            else:
                p0, p1, _, _ = cable_ends(c)
                kf = max(0.0, 1 - (j - 30) / 20)
                for (x, y), sd in ((p0, 1 if p1[0] > p0[0] else -1), (p1, 1 if p0[0] > p1[0] else -1)):
                    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
                    pts = [(x + sd * t * 8, y + 30 * t ** 1.4) for t in [i / 8 for i in range(9)]]
                    d.line(pts, fill=(196, 190, 212, int(230 * kf)), width=2); im.alpha_composite(lay)
        if j < 4: name, k, rage = 'Stunned', j, True; txt = 'aturdida'
        elif j < 6: name, k, rage = 'Hurt', 0, True; txt = 'último golpe'
        elif j < 14: name, k, rage = 'Death', j - 6, False; txt = 'muere: rueda panza arriba (vientre de euriptérido)'
        else: name, k, rage = 'Dead', 0, False; txt = 'la red se descuelga y se deshace; el cuerpo se queda'
        if name == 'Hurt':
            s = MG.hurt(spr('Idle', 0, a, rage=True))[j - 4]
            sh = spr('Idle', 0, a, shadow=True)
        else:
            s, sh = spr(name, k, a, rage=rage), spr(name, k, a, rage=rage, shadow=True)
        put_shadow(im, sh, *at, a=130); put(im, s, *at)
        caption(im, txt); frames.append(im)
    return frames


# =================================================================================================
# Escenas de la 6j (efectos y crías)
# =================================================================================================
_FX = {}
CELLS = {'Proj_MegarachneSilk': 10, 'Fx_MegarachneSilk_Muzzle': 24, 'Fx_MegarachneWeb_Appear': 40, 'Fx_MegarachneWeb_Idle': 40,
         'Fx_MegarachneWeb_Burn': 40, 'Fx_MegarachneNet_Shadow': 64, 'Fx_MegarachneNet_Fall': 64, 'Fx_MegarachneNet_Idle': 64,
         'Fx_MegarachneNet_Break': 64, 'Fx_MegarachneNet_Trapped': 40, 'Megarachne_Sac_Idle': 24, 'Megarachne_Sac_Tremble': 24,
         'Megarachne_Sac_Burst': 24, 'Megarachne_Sac_Break': 24, 'Megarachne_Cocoon_Fall': 24, 'Megarachne_Cocoon_Open': 24,
         'Fx_MegarachneDrop_Track': 40, 'Fx_MegarachneDrop_Locked': 40, 'Fx_MegarachneDust': 72,
         'Megarachne_Brood_Move': 24, 'Megarachne_Brood_Bite': 24, 'Fx_CableFlame': 16}
def fx(name, k):
    if name not in _FX:
        im = Image.open(os.path.join(HERE, 'Effects', name + '.png')).convert('RGBA'); w = CELLS[name]
        _FX[name] = [im.crop((i * w, 0, i * w + w, im.height)) for i in range(im.width // w)]
    fr = _FX[name]
    return fr[min(k, len(fr) - 1)] if k >= 0 else fr[k]

def putc(im, sprite, x, y):
    im.alpha_composite(sprite, (int(x - sprite.width / 2), int(y - sprite.height / 2)))

def spinneret_screen(name, k, ang, x, y):
    """Píxel de pantalla de las hileras para el cuadro de la Megarachne en (x, y) mirando a ang."""
    q = round(ang / 22.5) * 22.5; q = (q + 180) % 360 - 180
    flip = abs(q) > 90
    base = (180 - q if q > 0 else -180 - q) if flip else q
    table = MG.ANIMS.get(name) or MG.MOMENTS.get(name)
    sx, sy = MG.project(MG.spinneret(table[k]), base)
    if flip: sx = N - sx
    return x - N // 2 + sx, y - N // 2 + sy

def scene_base(cables=('NO', 'NE', 'SO'), state='wet'):
    im = base(); draw_cable_shadows(im, cables)
    return im

def escena_rafaga():
    frames = []
    wet_web = tree_cached('wetweb', lambda: tree_img('wet'))
    (hx, hy), (gx, gy) = on_cable('NE', .42)
    target = FROG_AT
    a = ang_to(gx, gy, *target)
    rnd = random.Random(5)
    balls, webs = [], []
    frog_x = FROG_AT[0]
    for j in range(64):
        im = base(); draw_cable_shadows(im, ('NO', 'NE', 'SO'))
        for w_ in webs:                                   # telarañas del suelo
            age = j - w_['t']
            if w_.get('burn') is not None:
                b = j - w_['burn']
                if b < 6: putc(im, fx('Fx_MegarachneWeb_Burn', b), w_['x'], w_['y'])
            else: putc(im, fx('Fx_MegarachneWeb_Appear', age) if age < 3 else fx('Fx_MegarachneWeb_Idle', 0), w_['x'], w_['y'])
        # la rana: cruza y queda frenada en una telaraña
        if 20 <= j < 44: frog_x = FROG_AT[0] + min(40, (j - 20) * 2.5 if j < 30 else 25 + (j - 30) * .6)
        f = Image.open(os.path.join(MG.MT, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')).convert('RGBA').crop((0, 0, 32, 32))
        im.alpha_composite(f, (int(frog_x) - 16, FROG_AT[1] - 24))
        layer_tree(im, wet_web)
        for c in ('NO', 'NE', 'SO'): draw_cable(im, c)
        k = min(7, j // 2) if j < 16 else 0
        name = 'SilkShoot' if j < 16 else 'Idle'
        put_shadow(im, spr(name, k if name == 'SilkShoot' else j % 4, a, shadow=True), gx, gy, a=70)
        put(im, spr(name, k if name == 'SilkShoot' else j % 4, a), hx, hy)
        if name == 'SilkShoot' and 8 <= j < 16:          # la seda sale en el cuadro 4 (j = 8): 6 bolitas en abanico
            mx, my = spinneret_screen('SilkShoot', 4, a, hx, hy)
            putc(im, fx('Fx_MegarachneSilk_Muzzle', (j - 8) % 4), mx, my)
            if j in (8, 9, 10):
                for i in range(2):
                    sp = math.radians(rnd.uniform(-18, 18))
                    dx, dy = target[0] - gx, target[1] - gy
                    ang = math.atan2(dy, dx) + sp
                    dist = math.hypot(dx, dy) * rnd.uniform(.55, 1.05)
                    balls.append(dict(x0=mx, y0=my, gx=gx + math.cos(ang) * dist, gy=gy + math.sin(ang) * dist, t0=j, T=8))
        for b in list(balls):                             # vuelan del cable al suelo
            t = (j - b['t0']) / b['T']
            if t >= 1:
                webs.append(dict(x=b['gx'], y=b['gy'], t=j)); balls.remove(b); continue
            x = b['x0'] + (b['gx'] - b['x0']) * t; y = b['y0'] + (b['gy'] - b['y0']) * t
            putc(im, fx('Proj_MegarachneSilk', j % 4), x, y)
        if j == 46 and webs:                              # una chispa prende la telaraña donde está la rana
            near = min(webs, key=lambda w_: abs(w_['x'] - frog_x) + abs(w_['y'] - FROG_AT[1]))
            near['burn'] = j
        txt = ('ráfaga de seda desde su cable (como la Phaser Spider)' if j < 20 else 'las bolitas quedan pegadas como telarañas'
               if j < 30 else 'la rana se queda frenada en una' if j < 46 else 'el fuego quema la telaraña y la libera')
        caption(im, txt); frames.append(im)
    return frames

def escena_red():
    frames = []
    wet_web = tree_cached('wetweb', lambda: tree_img('wet'))
    (hx, hy), (gx, gy) = on_cable('NO', .55)
    for j in range(46):
        im = base(); draw_cable_shadows(im, ('NO', 'NE', 'SO'))
        fxp = (FROG_AT[0] + 4, FROG_AT[1] - 6)
        if j < 6: putc(im, fx('Fx_MegarachneNet_Shadow', j), *fxp)
        elif j < 12: putc(im, fx('Fx_MegarachneNet_Shadow', 5), *fxp)
        frog(im)
        if 12 <= j < 17:
            f = fx('Fx_MegarachneNet_Fall', j - 12); im.alpha_composite(f, (int(fxp[0] - 32), int(fxp[1] - 64)))
        elif 17 <= j < 33:
            putc(im, fx('Fx_MegarachneNet_Idle', 0), *fxp)
            putc(im, fx('Fx_MegarachneNet_Trapped', (j - 17) % 4), FROG_AT[0], FROG_AT[1] - 8)
        elif 33 <= j < 38: putc(im, fx('Fx_MegarachneNet_Break', j - 33), *fxp)
        layer_tree(im, wet_web)
        for c in ('NO', 'NE', 'SO'): draw_cable(im, c)
        a = ang_to(gx, gy, *FROG_AT)
        put_shadow(im, spr('Idle', j % 4, a, shadow=True), gx, gy, a=70); put(im, spr('Idle', j % 4, a), hx, hy)
        caption(im, 'aviso: la sombra crece' if j < 12 else 'cae la red' if j < 17 else 'atrapada 1 s' if j < 33 else 'se rompe en jirones')
        frames.append(im)
    return frames

def escena_saco():
    frames = []
    wet_web = tree_cached('wetweb', lambda: tree_img('wet'))
    sacA = (FUNNEL[0] + 30, FUNNEL[1] + 40)                # colgado del borde de la copa
    (cbx, cby), _ = on_cable('NE', .45)
    sacB = (cbx, cby + 26)                                 # colgado del cable NE
    broods = []
    rnd = random.Random(7)
    for j in range(60):
        im = base(); draw_cable_shadows(im, ('NO', 'NE', 'SO')); frog(im)
        for b in broods:                                  # crías: caen, corren hacia la rana y muerden
            age = j - b['t']
            if age < 4: x, y = b['x'], b['y0'] + (b['gy'] - b['y0']) * (age / 4) ** 2; name, k = 'Megarachne_Brood_Move', 0
            else:
                tx, ty = FROG_AT[0] + b['ox'], FROG_AT[1] + b['oy']      # cada una rodea a la rana por su lado
                d = math.hypot(tx - b['x'], ty - b['gy'])
                if d > 6:
                    b['x'] += (tx - b['x']) / d * b['v']; b['gy'] += (ty - b['gy']) / d * b['v']
                    name, k = 'Megarachne_Brood_Move', age % 4
                else: name, k = 'Megarachne_Brood_Bite', age % 4
                x, y = b['x'], b['gy']
            s_ = fx(name, k)
            if FROG_AT[0] < x: s_ = s_.transpose(Image.FLIP_LEFT_RIGHT)
            putc(im, s_, x, y)
        layer_tree(im, wet_web)
        for c in ('NO', 'NE', 'SO'): draw_cable(im, c)
        for (sx, sy), anchor, kind in ((sacA, (sacA[0], FUNNEL[1] + 22), 'A'), (sacB, (sacB[0], sacB[1] - 26), 'B')):
            if kind == 'A':
                name, k = ('Megarachne_Sac_Idle', j % 4) if j < 10 else ('Megarachne_Sac_Tremble', j % 4) if j < 22 else ('Megarachne_Sac_Burst', j - 22)
                if j >= 27: continue
                if j == 23:
                    for i, (ox, oy) in enumerate(((-16, -6), (14, -10), (-10, 10), (16, 8))):
                        broods.append(dict(x=sx + (i - 1.5) * 12, y0=sy + 6, gy=sy + CROWN_H - 14 + i * 7, t=j + i,
                                           ox=ox, oy=oy, v=rnd.uniform(2.6, 3.8)))
            else:
                name, k = ('Megarachne_Sac_Idle', j % 4) if j < 30 else ('Megarachne_Sac_Break', j - 30)
                if j >= 35: continue
                if 26 <= j < 30:                            # la rana lo rompe antes (escupitajo)
                    d = ImageDraw.Draw(im); d.line([(FROG_AT[0] + 8, FROG_AT[1] - 14), (sx, sy)], fill=(200, 230, 90, 200), width=1)
            MG.draw_thread(im, anchor[0], anchor[1], sx, sy - 16)
            im.alpha_composite(fx(name, k), (int(sx - 12), int(sy - 16)))
        caption(im, 'saco de crías: tiembla' if j < 22 else 'revienta: crías que corren y muerden' if j < 30 else
                'romperlo antes: sin crías' if j < 36 else 'crías: débiles y rápidas')
        frames.append(im)
    return frames

def escena_salto():
    frames = []
    wet_web = tree_cached('wetweb', lambda: tree_img('wet'))
    t_c = .5
    (hx, hy), (gx, gy) = on_cable('NO', t_c)
    fx_, fy_ = FROG_AT
    for j in range(60):
        im = base(); draw_cable_shadows(im, ('NO', 'NE', 'SO'))
        # la rana se mueve un poco; la marca la sigue (ámbar) y luego se fija (roja)
        frog_x = fx_ + 14 * math.sin(j / 6) if j < 16 else fx_ + 14 * math.sin(16 / 6) - min(62, (j - 16) * 6)   # esquiva
        lock = (fx_ + 14 * math.sin(16 / 6), fy_)
        if j < 16: putc(im, fx('Fx_MegarachneDrop_Track', j % 4), frog_x, fy_ + 2)
        elif j < 30: putc(im, fx('Fx_MegarachneDrop_Locked', j % 2), lock[0], lock[1] + 2)
        f = Image.open(os.path.join(MG.MT, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')).convert('RGBA').crop((0, 0, 32, 32))
        im.alpha_composite(f, (int(frog_x) - 16, fy_ - 24))
        layer_tree(im, wet_web)
        for c in ('NO', 'NE', 'SO'): draw_cable(im, c)
        a = ang_to(gx, gy, *lock)
        if j < 16:
            put_shadow(im, spr('Walk', j, a, shadow=True), gx, gy, a=70); put(im, spr('Walk', j, a), hx, hy); txt = 'la marca la sigue (ámbar)'
        elif j < 22:
            put_shadow(im, spr('DropWarn', min(2, j - 16), a, shadow=True), gx, gy, a=70); put(im, spr('DropWarn', min(2, j - 16), a), hx, hy)
            txt = 'se fija (roja) y se agacha'
        elif j < 30:                                      # se descuelga en diagonal hasta la marca
            t = ((j - 22) / 7) ** 1.5
            x = hx + (lock[0] - hx) * t; y = hy + (lock[1] - hy) * t
            d0 = N // 2 - MG.hang_origin(MG.HANG['Drop'][0])[1]
            ox_, oy_ = MG.hang_origin(MG.HANG['Drop'][0])
            gxx = gx + (lock[0] - gx) * t; gyy = gy + (lock[1] - gy) * t
            put_shadow(im, spr('Drop', j, shadow=True), gxx, gyy, a=int(70 + 60 * t))
            MG.draw_thread(im, hx, hy, x - N // 2 + ox_, y + d0 * (1 - t) - N // 2 + oy_)
            put(im, spr('Drop', j), x, y + d0 * (1 - t)); txt = 'se deja caer colgada'
        else:
            k = j - 30
            if k < 6: putc(im, fx('Fx_MegarachneDust', k), lock[0], lock[1] + 2)
            if k < 18:
                name = 'Land' if k < 3 else 'Idle'
                MG.draw_thread(im, hx, hy, lock[0], lock[1] - 24)
                put_shadow(im, spr(name, min(2, k) if name == 'Land' else k % 4, a, shadow=True), *lock)
                put(im, spr(name, min(2, k) if name == 'Land' else k % 4, a), *lock)
                txt = 'aterriza (polvo): ventana de daño'
            else:                                         # trepa de vuelta por el hilo hasta el cable
                t = min(1, (k - 18) / 10)
                ox_, oy_ = MG.hang_origin(MG.HANG['Climb'][0])
                d0 = N // 2 - oy_
                x = lock[0] + (hx - lock[0]) * t; y = lock[1] + (hy + d0 - lock[1]) * t
                put_shadow(im, spr('Climb', k, shadow=True), lock[0] + (gx - lock[0]) * t, lock[1] + (gy - lock[1]) * t, a=int(130 - 60 * t))
                MG.draw_thread(im, hx, hy, x - N // 2 + ox_, y - N // 2 + oy_)
                put(im, spr('Climb', k), x, y)
                txt = 'trepa de vuelta por el hilo'
        caption(im, txt); frames.append(im)
    return frames

def escena_recompensa():
    frames = []
    dry = tree_cached('dry', lambda: NG.tree(mode='dry'))
    at = (FROG_AT[0] + 200, FROG_AT[1] + 14)             # el cuerpo, a un lado
    a = ang_to(*at, *FROG_AT)
    land = (FUNNEL[0], FUNNEL[1] + CROWN_H + 26)
    for j in range(40):
        im = base(); frog(im)
        layer_tree(im, tree_img(frame=dry, web=False))
        put_shadow(im, spr('Dead', 0, a, shadow=True), *at, a=130); put(im, spr('Dead', 0, a), *at)
        if j < 10:                                        # cae del embudo girando
            t = (j / 9) ** 1.6
            y = FUNNEL[1] + (land[1] - FUNNEL[1]) * t
            sh = Image.new('RGBA', (20, 8), (0, 0, 0, 0)); ImageDraw.Draw(sh).ellipse([0, 0, 19, 7], fill=(8, 6, 10, int(60 + 60 * t)))
            im.alpha_composite(sh, (land[0] - 10, land[1] + 10))
            putc(im, fx('Megarachne_Cocoon_Fall', j % 4), land[0], y)
            txt = 'del embudo cae un capullo de seda'
        else:
            putc(im, fx('Megarachne_Cocoon_Open', min(5, (j - 10) // 2)), land[0], land[1])
            txt = 'se abre: contenedor de corazón'
        caption(im, txt); frames.append(im)
    return frames

def guardar(name, frames, ms=90):
    os.makedirs(os.path.join(HERE, 'gif', 'escenas'), exist_ok=True)
    MG.gif(os.path.join(HERE, 'gif', 'escenas', f'Megarachne_{name}.gif'), frames, ms, MG.BG, scale=2)
    pick = [frames[int(i * (len(frames) - 1) / 7)] for i in range(8)]
    sheet = Image.new('RGBA', (W * 4 + 30, H * 2 + 18), tuple(MG.BG[:3]) + (255,))
    for i, f in enumerate(pick): sheet.alpha_composite(f, (6 + (i % 4) * (W + 6), 6 + (i // 4) * (H + 6)))
    sheet = sheet.resize((sheet.width * 2, sheet.height * 2), Image.NEAREST)
    tanda = '6j' if name in ('rafaga', 'red', 'saco', 'salto', 'recompensa') else '6i'
    sheet.convert('RGB').save(os.path.join(HERE, f'vista_previa_{tanda}_escena_{name}.png'))
    print('  escena', name, len(frames), 'cuadros', flush=True)

if __name__ == '__main__':
    todo = {'entrada': escena_entrada, 'enfado': escena_enfado, 'desplome': escena_desplome, 'muerte': escena_muerte,
            'rafaga': escena_rafaga, 'red': escena_red, 'saco': escena_saco, 'salto': escena_salto, 'recompensa': escena_recompensa}
    for name in (sys.argv[1:] or list(todo)):
        guardar(name, todo[name]())
