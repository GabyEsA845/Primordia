"""Lepidodendron ancestral · subtanda 6n-1: los 3 conos (estróbilos) con su armadura de ramas y la rama que los baja.

Fase final: los conos están arriba, en la copa (fuera de la vista). Tras el aviso, una rama gruesa baja desde la capa
de hojas con el cono en la punta, golpea el suelo y se queda tendida durante la ventana; al acabar, tira de él.
El cono (Lepidostrobus: escamas en espiral) está envuelto en ramas apretadas en espiral (la armadura): 3 estados
(intacta, astillada, rota) y carbonizada; al romperse queda el cono expuesto, que late y se abre.

Uso: python generar_conos_lepidodendron.py [muestra]
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import generar_lepidodendron as G
from generar_lepidodendron import *                    # noqa: F401,F403
from generar_lepidodendron import _h
from PIL import Image, ImageDraw

CONES = os.path.join(HERE, 'Cones')
CW_, CH_ = 72, 104                                     # celda del cono (con su rama arriba)
CX_, CBOT = 36, 98                                     # centro y base del cono (pivote: donde toca el suelo)
RW, RH = 19, 36                                        # semiejes del cono (≈ 2,4 × 4,5 u: alargado, como un puro)
SCALE = [C(c) for c in ('#140c1e', '#221436', '#382254', '#543878', '#7858a0', '#a084c8')]   # escamas (morado apagado, hondo)
CORE = [C(c) for c in ('#5a2c8a', '#8a50c8', '#b888f0', '#e8d4ff')]                        # luz de dentro
BARKA = BARK_OLD                                        # ramas de la armadura: la corteza del árbol
CHARA = [C(c) for c in ('#0b090a', '#141114', '#221c1e', '#352b2a', '#4c3e38', '#6a5848')]
WOOD = [C(c) for c in ('#4a3c2a', '#6e5a3e', '#968060', '#bca684')]
EMB = [C(c) for c in EMBER]

def cone_body(cv, open_=0.0, glow=0.0, sc=1.0):
    """El cono: cuerpo ovalado (más estrecho arriba) de escamas en espiral. open_ 0..1: las escamas se separan y
    asoma la luz de dentro (glow 0..1; por encima de 1, núcleo casi blanco). sc: se hincha (reventón)."""
    rw, rh = RW * sc, RH * sc
    cy = CBOT - rh
    for y in range(int(CBOT - 2 * rh - 2), CBOT + 1):
        for x in range(int(CX_ - rw - 2), int(CX_ + rw + 3)):
            ny = (y - cy) / rh
            w = rw * (1 - max(0, -ny) ** 1.6 * .62) * math.sqrt(max(0, 1 - ny * ny * (.9 if ny > 0 else 1)))   # punta arriba
            if w <= 0 or abs(x - CX_) > w: continue
            ph = (x - CX_) / max(w, 1)                  # -1..1 a lo ancho (el cono es redondo)
            th = math.asin(max(-.999, min(.999, ph)))
            # escamas en espiral: rejilla girada sobre el cilindro (ángulo, altura)
            a = th * 7 + y * .26
            b = th * 7 - y * .26
            fa, fb = (a / math.pi) % 1, (b / math.pi) % 1
            edge = min(fa, 1 - fa, fb, 1 - fb)          # 0 en la junta entre escamas
            lam = .45 - 1.05 * ph - .3 * ny
            tone = 4 if lam > .8 else 3 if lam > .35 else 2 if lam > -.1 else 1 if lam > -.55 else 0
            gap = .07 + .16 * open_
            if edge < gap:                              # entre escamas: oscuro, o la luz de dentro si se abren
                c = CORE[min(3 if glow > 1 else 2, int(glow * 2.6 + (.6 if edge < gap * .45 else 0)))] if (open_ > .05 and glow > .1) else SCALE[0]
                if glow > .1 and ph > .55: c = CORE[0]          # la luz también tiene volumen
            else:
                lower = (fa + fb) > 1                   # mitad de abajo de la escama: la tapa la de encima → sombra
                tip = (not lower) and edge > .28        # la punta que sobresale, con luz
                c = SCALE[max(0, min(5, tone + (1 if tip else 0) - (1 if lower else 0)))]
                if edge < gap + .06: c = SCALE[max(0, tone - 2)]
            cv.put(x, y, c)

def helix_bands(state, seed=7):
    """Ramas de la armadura: 5 bandas en espiral alrededor del cono. state 0 intacta, 1 astillada, 2 rota.
    Devuelve una lista de tramos [(puntos, ancho, roto_al_final)]."""
    rnd = random.Random(seed)
    bands = []
    cy = CBOT - RH
    for k in range(5):
        ph0 = k * 2 * math.pi / 5 + rnd.uniform(-.2, .2)
        pts = []
        for i in range(140):
            t = i / 139
            ang = ph0 + t * 2 * math.pi * 1.35
            y = CBOT - 4 - t * (2 * RH - 6)
            ny = (y - cy) / RH
            w = RW * (1 - max(0, -ny) ** 1.6 * .62) * math.sqrt(max(0, 1 - ny * ny))
            pts.append((CX_ + math.sin(ang) * (w + 3), y + math.cos(ang) * 2.5, math.cos(ang)))   # z>0: por delante
        bands.append(pts)
    segs = []
    for k, pts in enumerate(bands):                     # cortar en tramos según el estado
        cuts = []
        if state >= 1: cuts += [rnd.randint(15, 125) for _ in range(2)]
        if state >= 2: cuts += [rnd.randint(10, 130) for _ in range(2)]
        cuts = sorted(set(cuts))
        start = 0
        for c in cuts + [len(pts)]:
            seg = pts[start:c]
            if state >= 2 and rnd.random() < .45 and c != len(pts): seg = seg[:max(3, len(seg) // 2)]   # trozos que faltan
            if len(seg) > 3: segs.append((seg, rnd.uniform(6.5, 8.5), c != len(pts)))
            start = c + (6 if state >= 1 else 0)
    return segs

def draw_bands(cv, segs, burnt=False, front=True):
    pal = CHARA if burnt else BARKA
    for (seg, w, broken) in segs:
        for j, (x, y, z) in enumerate(seg):
            if (z > 0) != front: continue
            r = w / 2 * (.75 + .25 * abs(z))
            for yy in range(int(y - r), int(y + r) + 1):
                for xx in range(int(x - 1), int(x + 2)):
                    s = (yy - y) / max(r, 1)                # -1 arriba (luz) .. 1 abajo
                    tone = 4 if s < -.45 else 3 if s < .1 else 2 if s < .6 else 1
                    if z < .3: tone = max(0, tone - 1)      # girando hacia atrás: más oscuro
                    c = pal[tone]
                    if burnt and _h(xx, yy, 3) < .05: c = EMB[1]
                    if not burnt and s < -.2 and _h(xx // 2, yy // 2, 9) < .08: c = MOSS[2]
                    cv.put(xx, yy, c)
        if broken and front and seg[-1][2] > 0:          # astillas en la punta rota
            x, y, z = seg[-1]
            for k in range(6):                          # astillas claras, bien visibles
                cv.put(x + k * .7, y - 2 + (k % 3) * 2, (EMB[2] if burnt else WOOD[2 + (k % 2)]))
                cv.put(x + k * .7, y - 1 + (k % 3) * 2, (EMB[1] if burnt else WOOD[1]))

def band_tops(seed=7):
    """Dónde nace cada rama de la armadura (arriba del cono): [(x, z)] (z > 0: por delante)."""
    rnd = random.Random(seed)
    out = []
    cy = CBOT - RH
    for k in range(5):
        ph0 = k * 2 * math.pi / 5 + rnd.uniform(-.2, .2)
        ang = ph0 + 2 * math.pi * 1.35
        y = CBOT - 4 - (2 * RH - 6)
        ny = (y - cy) / RH
        w = RW * (1 - max(0, -ny) ** 1.6 * .62) * math.sqrt(max(0, 1 - ny * ny))
        out.append((CX_ + math.sin(ang) * (w + 3), math.cos(ang)))
    return out

def bark_px(x, y, ph, top_y, burnt=False, moss_from=10):
    """Color de la corteza de la rama en (x, y); ph = -1..1 a lo ancho. Patrón de 12 px de periodo en vertical."""
    pal = CHARA if burnt else BARKA
    th = math.asin(max(-.999, min(.999, ph)))
    sx = th * 8                                         # coordenada sobre el cilindro
    row = (y - top_y) // 6
    lx = (sx * 1.3 + (2.5 if row % 2 else 0)) % 5 - 2.5
    ly = (y - top_y) % 6 - 2.5
    dd = abs(lx) / 2.5 + abs(ly) / 3
    lam = .55 - .85 * ph
    tone = 5 if lam > 1.1 else 4 if lam > .7 else 3 if lam > .3 else 2 if lam > -.15 else 1
    c = pal[max(0, tone - 2)] if dd > .9 else pal[min(len(pal) - 1, tone + 1)] if (dd > .7 and lx < 0 and ly < 0) else pal[tone]
    if not burnt and dd > .9 and _h(x, y, 4) < .25 and y > top_y + moss_from: c = MOSS[1]
    if burnt and dd > .9 and _h(x, y, 6) < .12: c = EMB[1]
    return c

def branch(cv, top_y=0, burnt=False, lobes=True):
    """La rama que sostiene el cono: corteza del árbol (cojinetes en rombo sobre el cilindro, luz de lado, musgo).
    Al llegar al cono se ensancha en un manguito que lo abraza por arriba; de su borde bajan lóbulos de los que nacen
    las ramas de la armadura (así cada una sigue a la rama grande, sin corte). burnt: solo se queman los lóbulos (son
    parte de la armadura); la rama está viva y empapada y no arde, así encaja siempre con el tramo que se repite."""
    top_cone = CBOT - 2 * RH
    sleeve_y = top_cone + 8                             # hasta dónde baja el manguito
    tops = band_tops() if lobes else []
    def half(y):
        if y < top_cone - 16: return 7.5
        t = (y - (top_cone - 16)) / (sleeve_y - (top_cone - 16))
        return 7.5 + 8.5 * t ** 1.6                     # se ensancha suave hasta el ancho de la punta del cono
    for y in range(top_y, sleeve_y + 9):
        hw_ = half(min(y, sleeve_y))
        for x in range(int(CX_ - hw_ - 4), int(CX_ + hw_ + 5)):
            ph = (x - CX_) / hw_
            if abs(ph) > 1:
                continue
            if y > sleeve_y:                            # lóbulos que bajan hacia cada rama de la armadura
                ok = False
                for (bx, z) in tops:
                    if z < -.2: continue
                    if abs(x - bx) < 3.6 * (1 - (y - sleeve_y) / 9) + .5: ok = True
                if not ok: continue
            cv.put(x, y, bark_px(x, y, ph, top_y, burnt and y > sleeve_y))
    for k in range(10):                                 # musgo colgando del manguito (empapado: no arde)
        x = CX_ + (k - 4.5) * 3.2 + math.sin(k) * 1.5
        if (x - CX_) ** 2 > (half(sleeve_y) - 1) ** 2: continue
        L = 2 + int(_h(k, 1, 2) * 5)
        for j in range(L): cv.put(x, sleeve_y + 1 + j, MOSS[2] if j < 2 else MOSS[1])

def armored(state=0, burnt=False, with_branch=True):
    cv = Canvas(CW_, CH_)
    segs = helix_bands(state)
    draw_bands(cv, segs, burnt, front=False)            # las ramas de detrás asoman por los lados
    cone_body(cv)
    draw_bands(cv, segs, burnt, front=True)
    if with_branch: branch(cv, 0, burnt)
    cv.outline(); cv.clean()
    return cv

def exposed(open_=.6, glow=.6, with_branch=True, sc=1.0):
    cv = Canvas(CW_, CH_)
    cone_body(cv, open_, glow, sc)
    if with_branch: branch(cv, 0, lobes=False)
    cv.outline(); cv.clean()
    return cv

def husk():
    """El cono roto (no vuelve a atacar): escamas abiertas y apagadas, sin luz, partido."""
    cv = Canvas(CW_, CH_)
    cone_body(cv, .9, 0.0)
    for (x, y) in list(cv.px):                          # partido por la mitad de arriba abajo
        if abs(x - CX_ - math.sin(y / 6) * 2) < 1.2 and y > CBOT - 2 * RH + 6: del cv.px[(x, y)]
    for (x, y), c in list(cv.px.items()):
        cv.px[(x, y)] = blend(c, C('#2a2430'), .45)
    cv.outline(); cv.clean()
    return cv

# =================================================================================================
def lamina_muestra():
    items = [('armadura intacta', armored(0)), ('astillada', armored(1)), ('rota', armored(2)),
             ('carbonizada', armored(0, True)), ('carbonizada, rota', armored(2, True)),
             ('expuesto', exposed(.3, .35)), ('expuesto: late', exposed(.6, .9)), ('roto (ya no ataca)', husk())]
    s = 3
    W = len(items) * (CW_ + 8) + 8 + 40
    out = Image.new('RGBA', (W * s, (CH_ + 30) * s), C('#2a2d25'))
    d = ImageDraw.Draw(out)
    frog = A.frame(FROG, 0, 32)
    for i, (lab, cv) in enumerate(items):
        im = cv.img().convert('RGBA')
        x = 8 + i * (CW_ + 8)
        sh = Image.new('RGBA', (CW_, 12), (0, 0, 0, 0)); ImageDraw.Draw(sh).ellipse([10, 2, CW_ - 10, 10], fill=(10, 12, 10, 120))
        tile = Image.new('RGBA', (CW_ + 8, CH_ + 30), (0, 0, 0, 0))
        tile.alpha_composite(sh, (0, CBOT - 6)); tile.alpha_composite(im, (0, 0))
        out.alpha_composite(tile.resize((tile.width * s, tile.height * s), Image.NEAREST), (x * s, 24))
        d.text((x * s + 4, 4), lab, fill=(120, 230, 230), font=A.font(15))
    out.alpha_composite(frog.resize((96, 96), Image.NEAREST), (W * s - 110, (CH_ - 4) * s - 60))
    d.text((W * s - 110, (CH_ - 4) * s - 80), 'rana', fill=(120, 230, 230), font=A.font(15))
    G._save(out, 'Lepidodendron ancestral · 6n-1 muestra: el cono con su armadura de ramas, la rama que lo sostiene y el cono expuesto (×3)',
            ['Cono de ~3 × 4,5 u (escamas en espiral, morado apagado), 5 ramas de corteza del árbol en espiral (las de detrás asoman por los lados).',
             'Astillada / rota: tramos que faltan y astillas; carbonizada: con ascuas. Expuesto: las escamas se abren y sale la luz de dentro. Rana a la derecha (×3).'],
            'vista_previa_6n1_muestra.png')

# =================================================================================================
# 6n-1 · Animaciones: rama que se repite, golpe, armadura que se rompe / arde / vuelve a crecer, cono que revienta
# =================================================================================================
GIF = os.path.join(HERE, 'gif')
TILE_H = 24                                            # tramo de rama que se repite (2 periodos del patrón)
SLEEVE_Y = CBOT - 2 * RH + 8
BURN_N, BURN_MS = 8, 150
SHAT_W, SHAT_H, SHAT_PX, SHAT_PY = 128, 112, 64, 104  # celda de la rotura de la armadura y su pivote
BURST_W, BURST_H, BURST_PX, BURST_PY = 128, 120, 64, 114
DUST = [C(c) for c in ('#3e3c2c', '#5a5640', '#7a7556', '#a09a78', '#c6c09c')]
SPORE_L = [C(c) for c in ('#7a5aa8', '#a688d0', '#cdb8ec', '#efe4ff')]   # esporas lila del jefe
IDLE_N, IDLE_MS = 6, 120
CONTACT = 4                                            # cuadro de Fx_ConeShadow que hace de sombra de contacto en el suelo

def _im(x): return x.img().convert('RGBA') if hasattr(x, 'img') else x

def hurt(im):
    """Golpe: silueta blanca y luego roja (solo lo opaco), como las paredes."""
    a = im.split()[3].point(lambda v: 255 if v == 255 else 0)
    white = Image.new('RGBA', im.size, (255, 255, 255, 255)); white.putalpha(a)
    red = Image.new('RGBA', im.size, C(RED4[1])); red.putalpha(a)
    return [white, red]

def branch_tile():
    """Tramo de la rama (TILE_H px) que se repite hacia arriba desde la celda del cono hasta la copa."""
    cv = Canvas(CW_, TILE_H)
    hw_ = 7.5
    for y in range(TILE_H):
        for x in range(int(CX_ - hw_ - 4), int(CX_ + hw_ + 5)):
            ph = (x - CX_) / hw_
            if abs(ph) <= 1: cv.put(x, y, bark_px(x, y, ph, 0, moss_from=-1))
    cv.outline(); cv.clean()
    return cv

def branch_torn():
    """La rama sin el cono (tras el reventón): el manguito desgarrado con fibras que cuelgan y savia morada."""
    cv = Canvas(CW_, CH_)
    branch(cv, 0, lobes=False)
    for (x, y) in list(cv.px):
        if y < SLEEVE_Y - 14: continue
        fib = 7 if _h(x, 3, 3) < .3 else 3 if _h(x, 4, 3) < .5 else 0
        cut = SLEEVE_Y - 9 + int(_h(x // 2, 1, 7) * 4) + fib
        if y > cut: del cv.px[(x, y)]
        elif y > cut - 2 - (fib > 0) * fib:
            cv.px[(x, y)] = WOOD[2 if (fib and y < cut - 1) else 1] if (x + y) % 3 else WOOD[0]   # madera de dentro y fibras
    rnd = random.Random(4)
    for _ in range(5):                                  # savia que gotea
        x = CX_ + rnd.randint(-8, 8)
        ys = [y for (xx, y) in cv.px if xx == x]
        if not ys: continue
        y0 = max(ys) + 1
        for j in range(rnd.randint(2, 5)): cv.put(x, y0 + j, CORE[1] if j < 2 else CORE[0])
    cv.outline(); cv.clean()
    return cv

def _layer(fn):
    cv = Canvas(CW_, CH_); fn(cv); return cv

def burn(state, n=BURN_N):
    """La armadura arde: lo carbonizado sube desde abajo con el frente de llamas (Fx_GroundFire del piso 3, ya en la
    hoja); el último cuadro es la armadura carbonizada (luego ArmorCharred{state} + Fx_RootSmolder)."""
    import generar_paredes_lepidodendron as P
    fire = P.A_fire()
    nb, cb = armored(state).img().convert('RGBA'), armored(state, True).img().convert('RGBA')
    top = SLEEVE_Y - 2
    out = []
    for k in range(n):
        f = (k + 1) / (n - 1)
        yf = CBOT + 3 - min(1, f) * (CBOT + 3 - top)
        im = nb.copy()
        for y in range(CH_):
            for x in range(CW_):
                p = cb.getpixel((x, y))
                if not p[3]: continue
                edge = yf + (_h(x // 2, 5, 1) - .5) * 6
                if y > edge: im.putpixel((x, y), p)
                elif y > edge - 2 and nb.getpixel((x, y))[3] and nb.getpixel((x, y))[:3] != OUTL[:3]:
                    im.putpixel((x, y), EMB[3] if (x + y) % 3 == 0 else EMB[2])   # el frente al rojo
        if k < n - 1:                                   # llamas en el frente, a lo ancho del cono
            xs = [x for x in range(CW_) if im.getpixel((x, int(min(CBOT, yf))))[3]]
            if xs:
                x0, x1 = min(xs), max(xs)
                cs = [(x0 + x1) / 2] if x1 - x0 < 26 else [x0 + (x1 - x0) * .28, x0 + (x1 - x0) * .72]
                for j, xc in enumerate(cs):
                    fl = fire[(k + j * 3) % 6].copy()
                    fx0, fy0 = int(xc - 16), int(yf + 5 - 24)
                    for yy in range(24):                # la base de las llamas no se sale del cono
                        for xx in range(32):
                            X, Y = fx0 + xx, fy0 + yy
                            if Y > yf - 3 and not (0 <= X < CW_ and 0 <= Y < CH_ and nb.getpixel((X, min(CH_ - 1, Y)))[3]):
                                fl.putpixel((xx, yy), (0, 0, 0, 0))
                    im.alpha_composite(fl, (fx0, fy0))
        out.append(im)
    return out

def bands_layer(segs, burnt, front, upto=None, tip=False):
    """Capa con ramas de la armadura; upto 0..1 = cuánto han crecido desde arriba (para la armadura que vuelve)."""
    cv = Canvas(CW_, CH_)
    if upto is not None:
        segs = [(seg[int((1 - upto) * (len(seg) - 1)):], w, b) for (seg, w, b) in segs]
        segs = [s for s in segs if len(s[0]) > 1]
    draw_bands(cv, segs, burnt, front)
    if tip and upto is not None and upto < 1:
        for (seg, w, b) in segs:                        # puntas tiernas, verde claro
            for (x, y, z) in seg[:4]:
                if (z > 0) == front:
                    for dy in range(-2, 3): cv.put(x, y + dy, MOSS[3] if dy < 1 else MOSS[2])
    return cv

def regrow(state, burnt, n=8):
    """La armadura SIN QUEMAR vuelve a crecer (sube sin romperse; la quemada está muerta y no rebrota): ramas tiernas bajan del manguito en espiral y la vieja se
    deshace. Último cuadro = Armor0."""
    old, new = helix_bands(state), helix_bands(0)
    out = []
    for k in range(n):
        if k == n - 1: out.append(armored(0).img().convert('RGBA')); continue
        g = min(1, k / (n - 2) * 1.08)
        keep = 1 - max(0, (k - 2) / (n - 3))
        cv = Canvas(CW_, CH_)
        def merge(src, cond=lambda x, y: True):
            for (x, y), c in src.px.items():
                if cond(x, y): cv.px[(x, y)] = c
        dis = lambda x, y: _h(x // 2, y // 2, 5) < keep
        merge(bands_layer(old, burnt, False), dis)
        merge(bands_layer(new, False, False, g, True))
        cone_body(cv)
        merge(bands_layer(old, burnt, True), dis)
        merge(bands_layer(new, False, True, g, True))
        branch(cv, 0, burnt and k < 2)
        cv.outline(); cv.clean()
        out.append(cv.img().convert('RGBA'))
    return out

def shatter(burnt=False, n=8):
    """Último golpe a la armadura rota: el cono se enciende por dentro y las ramas salen volando a trozos, caen y se
    deshacen. Celda 128×112, pivote (64, 104). Último cuadro = Exposed_Idle cuadro 0."""
    ox, oy = SHAT_PX - CX_, SHAT_PY - CBOT
    A_ = armored(2, burnt).img().convert('RGBA')
    ref = Canvas(CW_, CH_); cone_body(ref); branch(ref, 0, lobes=False); ref.outline(); ref.clean()
    R_ = ref.img().convert('RGBA')
    chunks = {}
    for y in range(CH_):
        for x in range(CW_):
            a = A_.getpixel((x, y))
            if a[3] and a != R_.getpixel((x, y)):
                key = (int((x + 3 * _h(y // 5, 0, 1)) // 7), int((y + 3 * _h(x // 7, 1, 1)) // 6))
                chunks.setdefault(key, []).append((x, y, a))
    rnd = random.Random(3)
    par = {}
    for key, pts in chunks.items():
        mx = sum(p[0] for p in pts) / len(pts); my = sum(p[1] for p in pts) / len(pts)
        par[key] = ((mx - CX_) * .2 + rnd.uniform(-1.3, 1.3), -rnd.uniform(1.2, 3.6), CBOT + rnd.uniform(-1, 7) - my, rnd.random())
    glows = [(.4, 1.15), (.5, 1.0), (.45, .85), (.4, .7), (.36, .55), (.33, .45), (.31, .38), (.3, .35)]
    out = []
    for k in range(n):
        im = Image.new('RGBA', (SHAT_W, SHAT_H), (0, 0, 0, 0))
        o, gl = glows[k]
        im.alpha_composite(exposed(o, gl).img().convert('RGBA'), (ox, oy))
        t = k * 1.0
        fade = 1 if k < 4 else (1 - (k - 3) / 4)
        for key, pts in chunks.items():
            vx, vy, ground, r = par[key]
            dx = vx * t
            dy = vy * t + .55 * t * t
            if dy > ground: dy = ground                 # se queda en el suelo
            if r > fade: continue                       # se deshace
            for (x, y, c) in pts:
                if burnt and k > 0 and _h(x, y, k) < .06: c = EMB[3]
                xx, yy = x + ox + dx, y + oy + dy
                if 0 <= xx < SHAT_W and 0 <= yy < SHAT_H: im.putpixel((int(xx), int(yy)), c)
        d = ImageDraw.Draw(im)
        if 1 <= k <= 3:                                 # polvo de corteza (o ceniza)
            for j in range(16):
                a = j / 16 * 2 * math.pi; rr = 10 + k * 7 + _h(j, k, 2) * 6
                x = SHAT_PX + math.cos(a) * rr * 1.2; y = SHAT_PY - RH + math.sin(a) * rr * .9
                col = (CHARA[3] if burnt else DUST[3])[:3] + (int(200 - k * 50),)
                d.rectangle([x, y, x + 1, y + 1], fill=col)
        if k == 0:                                      # chispas de luz en las grietas
            for j in range(10):
                x = SHAT_PX + (_h(j, 1, 9) - .5) * 30; y = SHAT_PY - 10 - _h(j, 2, 9) * 56
                im.putpixel((int(x), int(y)), CORE[3])
        out.append(im)
    return out

PUFFS = [(j / 11 * 2 * math.pi + random.Random(j).uniform(-.25, .25), random.Random(j + 50).uniform(.6, 1.0),
          random.Random(j + 90).uniform(6, 11)) for j in range(11)]

def puffs(im, t, cy, back):
    """Bocanadas de esporas lila con volumen (luz arriba a la izquierda) que se abren y se deshacen en trama."""
    R = 18 + t * 6.5
    dens = 1 - t / 6.5
    for (a, rf, r0) in PUFFS:
        if (math.sin(a) < 0) != back: continue
        px_, py_ = BURST_PX + math.cos(a) * R * rf, cy + math.sin(a) * R * .6 * rf - t * 2.5
        rr = r0 * (1 + t * .14)
        for y in range(int(py_ - rr), int(py_ + rr) + 1):
            for x in range(int(px_ - rr), int(px_ + rr) + 1):
                nx, ny = (x - px_) / rr, (y - py_) / (rr * .8)
                dd = nx * nx + ny * ny
                if dd > 1 or not (0 <= x < BURST_W and 0 <= y < BURST_H): continue
                if (BAYER[y % 4][x % 4] + .5) / 16 > dens * (1.25 - dd * .5): continue
                lam = -(nx + ny) * .8
                c = SPORE_L[2] if lam > .45 else SPORE_L[1] if lam > -.25 else SPORE_L[0]
                im.putpixel((x, y), c[:3] + (235,))

def burst(n=10):
    """El cono expuesto revienta (último golpe): se hincha y se ilumina, estalla en una nube de esporas lila y
    escamas, y queda el cono roto con la rama desgarrada. Celda 128×120, pivote (64, 114). Cuadro que cuenta: 3."""
    ox, oy = BURST_PX - CX_, BURST_PY - CBOT
    husk_im = husk().img().convert('RGBA'); torn = branch_torn().img().convert('RGBA')
    rnd = random.Random(8)
    spores = [(rnd.uniform(0, 2 * math.pi), rnd.uniform(2.5, 7), rnd.randrange(4), rnd.uniform(-.4, .4)) for _ in range(70)]
    chips = [(rnd.uniform(-3, 3), -rnd.uniform(1.5, 4.5), rnd.uniform(-20, 20), rnd.uniform(-60, -10), rnd.choice((2, 3, 4)), CBOT + rnd.uniform(0, 8)) for _ in range(22)]
    cy = BURST_PY - RH
    out = []
    for k in range(n):
        im = Image.new('RGBA', (BURST_W, BURST_H), (0, 0, 0, 0))
        if k < 3:
            sc = (1.04, 1.08, 1.12)[k]
            im.alpha_composite(exposed((.7, .85, 1.0)[k], (1.0, 1.15, 1.3)[k], sc=sc).img().convert('RGBA'), (ox, oy))
            if k == 2:
                for j in range(12):                     # rayos de luz que se escapan
                    a = j / 12 * 2 * math.pi
                    for r in range(RW + 4, RW + 9):
                        x, y = BURST_PX + math.cos(a) * r, cy + math.sin(a) * r * 1.6
                        if 0 <= x < BURST_W and 0 <= y < BURST_H: im.putpixel((int(x), int(y)), CORE[2])
            out.append(im); continue
        t = k - 3
        puffs(im, t, cy, back=True)                     # nube de esporas: la mitad de arriba, detrás del cono
        im.alpha_composite(husk_im, (ox, oy))
        im.alpha_composite(torn, (ox, oy))
        puffs(im, t, cy, back=False)
        if t == 0:                                       # el estallido: destello en estrella, borde en trama
            for y in range(BURST_H):
                for x in range(BURST_W):
                    dx, dy = (x - BURST_PX) / 30, (y - cy) / 36
                    ang = math.atan2(dy, dx)
                    rr = math.hypot(dx, dy) / (.75 + .25 * abs(math.cos(ang * 4)))
                    if rr > 1: continue
                    if rr > .7 and (BAYER[y % 4][x % 4] + .5) / 16 < (rr - .7) / .3: continue
                    im.putpixel((x, y), (255, 255, 255, 255) if rr < .25 else CORE[3] if rr < .55 else CORE[2])
            out.append(im); continue
        for (ang, v, c, drift) in spores:               # esporas sueltas
            r = v * (1 - .8 ** (t + 1)) / .2
            x = BURST_PX + math.cos(ang) * r; y = cy + math.sin(ang) * r * .7 + t * t * .3
            if t > 5 and c > 1: continue
            if 0 <= x < BURST_W - 1 and 0 <= y < BURST_H - 1:
                col = SPORE_L[min(3, c + (1 if t < 2 else 0))]
                im.putpixel((int(x), int(y)), col)
        for (vx, vy, x0, y0, s, gnd) in chips:          # escamas que saltan y caen
            x = BURST_PX + x0 * .6 + vx * t * 2.2
            y = BURST_PY + y0 * .9 + vy * t * 2.2 + .9 * t * t
            y = min(y, BURST_PY - CBOT + gnd)
            if t > 5 and s < 3: continue
            for dx in range(s // 2 + 1):
                for dy in range(s // 2):
                    xx, yy = int(x + dx), int(y + dy)
                    if 0 <= xx < BURST_W and 0 <= yy < BURST_H: im.putpixel((xx, yy), SCALE[3] if dy == 0 else SCALE[1])
        out.append(im)
    return out

# --- efectos -----------------------------------------------------------------------------------------------------
def fx_shadow(k, n=8):
    """Aviso en el suelo: la sombra del cono que baja crece (72×36, centrada en donde golpeará)."""
    cv = Canvas(72, 36)
    g = (k + 1) / n
    rx = 7 + 19 * g ** 1.3; ry = rx * .42
    for y in range(36):
        for x in range(72):
            d = math.hypot((x - 36) / rx, (y - 18) / ry)
            if d >= 1: continue
            core = d < .55
            if not core and (BAYER[y % 4][x % 4] + .5) / 16 < (d - .55) / .45: continue
            cv.put(x, y, (8, 12, 10, int(70 + 100 * g) if core else int(50 + 70 * g)))
    return cv

def fx_slam(k, n=7):
    """Golpe del cono contra el suelo (128×64, pivote (64, 36) = la base del cono): estallido de polvo, onda que se
    abre, terrones y esporas que saltan. Cuadro que cuenta: 0 (daño en el sitio), la onda empuja hasta el 2."""
    W, H, cx, cy = 128, 64, 64, 36
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    t = k / (n - 1)
    R = 16 + 46 * t ** .65; ry = R * .42
    al = int(255 * (1 - t) ** .6)
    th = 4.5 * (1 - t) + 1.5
    for y in range(H):                                   # la onda: anillo de polvo roto
        for x in range(W):
            dd = math.hypot((x - cx) / R, (y - cy) / ry)
            e = abs(dd - 1) * R
            if e < th and _h(x // 2, y // 2, k) > .18 + .5 * t:
                tone = 3 if y < cy - ry * .3 else 2 if y < cy + ry * .4 else 1
                if e < th * .4: tone += 1
                im.putpixel((x, y), DUST[min(4, tone)][:3] + (al,))
    for j in range(11):                                  # bocanadas de polvo sobre la onda (tamaños y huecos distintos)
        a = j / 11 * 2 * math.pi + .2 + (_h(j, 7, 3) - .5) * .5
        rf = .85 + _h(j, 8, 3) * .25
        px_, py_ = cx + math.cos(a) * R * rf, cy + math.sin(a) * ry * rf - 2 - t * 4
        rr = (2 + 5 * _h(j, 1, 3) ** 1.5) * (1 + t * .8)
        if _h(j, 9, 3) < .2: continue
        d.ellipse([px_ - rr, py_ - rr * .8, px_ + rr, py_ + rr * .8], fill=DUST[2][:3] + (int(al * .85),))
        d.ellipse([px_ - rr * .7, py_ - rr * .8, px_ + rr * .1, py_ - rr * .1], fill=DUST[3][:3] + (int(al * .85),))
    if k <= 1:                                           # el estallido en el centro
        rr = 14 + k * 6
        d.ellipse([cx - rr, cy - rr * .5, cx + rr, cy + rr * .5], fill=DUST[3][:3] + (220 - k * 80,))
        d.ellipse([cx - rr * .6, cy - rr * .35, cx + rr * .3, cy + rr * .05], fill=DUST[4][:3] + (220 - k * 80,))
    rnd = random.Random(2)
    for j in range(14):                                  # terrones y esporas que saltan en arco
        a = rnd.uniform(0, 2 * math.pi); v = rnd.uniform(14, 34); h = rnd.uniform(10, 24)
        x = cx + math.cos(a) * v * t; y = cy + math.sin(a) * v * .42 * t - h * 4 * t * (1 - t)
        if j % 3 == 0:
            col = SPORE_L[2 if t < .5 else 1]; im.putpixel((int(x), int(y)), col)
        else:
            col = SOIL[3] if j % 2 else SOIL[1]
            d.rectangle([x, y, x + 1, y + (1 if j % 4 else 0)], fill=col)
    return im

def fx_crater():
    """Marca del golpe en el suelo (72×32, pivote (36, 16)): hundimiento con el borde levantado y grietas."""
    W, H, cx, cy = 72, 32, 36, 16
    cv = Canvas(W, H)
    rnd = random.Random(6)
    for j in range(8):                                   # grietas
        a = j / 8 * 2 * math.pi + rnd.uniform(-.3, .3)
        x, y = cx + math.cos(a) * 16, cy + math.sin(a) * 7
        L = rnd.randint(6, 13)
        for s in range(L):
            a += rnd.uniform(-.4, .4)
            x += math.cos(a); y += math.sin(a) * .5
            cv.put(x, y, SOIL[0])
            if s < L / 2: cv.put(x + (1 if math.sin(a) > 0 else 0), y + 1, SOIL[0])
    for y in range(H):
        for x in range(W):
            d = math.hypot((x - cx) / 21, (y - cy) / 9.5)
            if d >= 1: continue
            if d > .82:                                  # labio de tierra levantada, que se funde en trama
                if (BAYER[y % 4][x % 4] + .5) / 16 < (d - .82) / .18: continue
                cv.put(x, y, SOIL[3] if y < cy else SOIL[2])
            elif d > .66:
                cv.put(x, y, SOIL[3] if (y < cy and x < cx + 8) else SOIL[2])
            else:                                        # hoyo: la pared de arriba en sombra, la de abajo con luz
                ny = (y - cy) / 9.5
                cv.put(x, y, SOIL[0] if ny < -.1 else GLOOM if ny < .25 else SOIL[1])
    return cv

def fx_lift(k, n=6):
    """El cono se despega del suelo al subir (64×48, pivote (32, 40)): terrones y polvo que caen."""
    W, H, cx, cy = 64, 48, 32, 40
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    rnd = random.Random(12)
    for j in range(16):
        x0 = cx + rnd.uniform(-14, 14); y0 = cy - rnd.uniform(0, 10) - 6
        v = rnd.uniform(.5, 2.0)
        y = min(cy + rnd.uniform(-2, 4), y0 + v * k + .7 * k * k)
        if k > 3 and j % 2: continue
        d.rectangle([x0, y, x0 + (1 if j % 3 else 0), y + 1], fill=SOIL[3] if j % 2 else SOIL[1])
    if k < 4:
        for j in range(6):
            a = j / 6 * math.pi + math.pi; rr = 4 + k * 2
            px_, py_ = cx + math.cos(a) * (8 + k * 4) * 1.4, cy - 2 + math.sin(a) * -2
            d.ellipse([px_ - rr, py_ - rr * .6, px_ + rr, py_ + rr * .6], fill=DUST[2][:3] + (int(170 * (1 - k / 4)),))
    return im

def fx_chip(k, n=5, burnt=False):
    """Golpe a la armadura (40×40, centrado en el punto del golpe): astillas de corteza (o carbón y ascuas) y polvo."""
    S = 40; c0 = S // 2
    im = Image.new('RGBA', (S, S), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    rnd = random.Random(11)
    if k == 0:                                           # el chasquido
        for a in range(8):
            ang = a / 8 * 2 * math.pi
            for r in range(2, 6 if a % 2 else 8):
                im.putpixel((int(c0 + math.cos(ang) * r), int(c0 + math.sin(ang) * r)), (EMB[3] if burnt else WOOD[3]))
        im.putpixel((c0, c0), (255, 255, 255, 255))
    for j in range(12):
        ang = rnd.uniform(-math.pi * .95, math.pi * .2) if j % 3 else rnd.uniform(0, 2 * math.pi)
        v = rnd.uniform(2.2, 4.6)
        x = c0 + math.cos(ang) * v * k; y = c0 + math.sin(ang) * v * k * .8 + .5 * k * k
        if k == n - 1 and j % 2: continue
        if burnt:
            col = EMB[3] if j % 4 == 0 else CHARA[2] if j % 2 else CHARA[3]
        else:
            col = WOOD[3] if j % 3 == 0 else WOOD[2] if j % 3 == 1 else BARKA[3]
        ex, ey = math.cos(ang) * 1.6, math.sin(ang) * 1.6
        d.line([x, y, x + ex, y + ey], fill=col)
    if 1 <= k <= 3:
        for j in range(7):
            a = j / 7 * 2 * math.pi; rr = 3 + k * 3
            x, y = c0 + math.cos(a) * rr, c0 + math.sin(a) * rr * .7
            d.point((x, y), fill=((CHARA[4] if burnt else DUST[3])[:3] + (200 - k * 50,)))
    return im

# =================================================================================================
# Hojas
# =================================================================================================
def build():
    out = {}
    out['Lepido_ConeBranch_Tile'] = [_im(branch_tile())]
    out['Lepido_ConeBranch_Torn'] = [_im(branch_torn())]
    for s in range(3):
        a, c = _im(armored(s)), _im(armored(s, True))
        out[f'Lepido_Cone_Armor{s}'] = [a]
        out[f'Lepido_Cone_Armor{s}_Hurt'] = hurt(a)
        out[f'Lepido_Cone_ArmorCharred{s}'] = [c]
        out[f'Lepido_Cone_ArmorCharred{s}_Hurt'] = hurt(c)
        out[f'Lepido_Cone_Burn{s}'] = burn(s)
    for s in (1, 2): out[f'Lepido_Cone_Regrow{s}'] = regrow(s, False)
    out['Lepido_Cone_Shatter'] = shatter(False)
    out['Lepido_Cone_ShatterCharred'] = shatter(True)
    idle = []
    for k in range(IDLE_N):
        s_ = (1 - math.cos(2 * math.pi * k / IDLE_N)) / 2
        idle.append(_im(exposed(.3 + .3 * s_, .35 + .55 * s_)))
    out['Lepido_Cone_Exposed_Idle'] = idle
    out['Lepido_Cone_Exposed_Hurt'] = hurt(idle[3])
    out['Lepido_Cone_Burst'] = burst()
    out['Lepido_Cone_Husk'] = [_im(husk())]
    out['Fx_ConeShadow'] = [_im(fx_shadow(k)) for k in range(8)]
    out['Fx_ConeSlam'] = [fx_slam(k) for k in range(7)]
    out['Fx_ConeCrater'] = [_im(fx_crater())]
    out['Fx_ConeLift'] = [fx_lift(k) for k in range(6)]
    out['Fx_ArmorChip'] = [fx_chip(k) for k in range(5)]
    out['Fx_ArmorChipCharred'] = [fx_chip(k, burnt=True) for k in range(5)]
    return out

def piezas():
    out = build()
    for name, fr in out.items(): save_sheet(os.path.join(CONES, name + '.png'), fr)
    os.makedirs(GIF, exist_ok=True)
    gifs = {'Lepido_Cone_Burn0': 150, 'Lepido_Cone_Shatter': 90, 'Lepido_Cone_ShatterCharred': 90, 'Lepido_Cone_Burst': 80,
            'Lepido_Cone_Regrow2': 100, 'Lepido_Cone_Exposed_Idle': IDLE_MS,
            'Fx_ConeSlam': 70, 'Fx_ConeShadow': 90}
    for name, ms in gifs.items(): gif(os.path.join(GIF, name + '.gif'), out[name], ms, BG, 3)
    print('   Cones/ escrito:', len(out), 'hojas')
    return out

def _row_sheet(rows, title, notes, name, s=2):
    """Hoja de contacto propia (las piezas tienen tamaños muy distintos): una fila por hoja, ×s."""
    pad = 10
    hs = [max(f.height for f in fr) * s + 26 for _, fr in rows]
    W = max(sum(f.width * s + pad for f in fr) for _, fr in rows) + 2 * pad
    out = Image.new('RGBA', (W, sum(hs) + pad), C('#2a2d25'))
    d = ImageDraw.Draw(out)
    y = pad
    for (lab, fr), h in zip(rows, hs):
        d.text((pad, y), lab, fill=(120, 230, 230), font=A.font(15))
        x = pad
        for f in fr:
            out.alpha_composite(f.resize((f.width * s, f.height * s), Image.NEAREST), (x, y + 20))
            x += f.width * s + pad
        y += h
    G._save(out, title, notes, name)

def laminas(out):
    _row_sheet([('Armor0 · Armor0_Hurt · Armor1 · Armor2 · ArmorCharred0-2 · Husk · ConeBranch_Torn · ConeBranch_Tile ×3',
                 out['Lepido_Cone_Armor0'] + out['Lepido_Cone_Armor0_Hurt'] + out['Lepido_Cone_Armor1'] + out['Lepido_Cone_Armor2'] +
                 out['Lepido_Cone_ArmorCharred0'] + out['Lepido_Cone_ArmorCharred1'] + out['Lepido_Cone_ArmorCharred2'] +
                 out['Lepido_Cone_Husk'] + out['Lepido_ConeBranch_Torn'] + [_tiles(out['Lepido_ConeBranch_Tile'][0])]),
                ('Burn0 (8 × 150 ms): arde de abajo arriba → ArmorCharred0', out['Lepido_Cone_Burn0']),
                ('Burn2', out['Lepido_Cone_Burn2']),
                ('Regrow2 (8 × 100 ms): vuelve a crecer al subir → Armor0 (solo sin quemar)', out['Lepido_Cone_Regrow2']),
                ('Regrow1', out['Lepido_Cone_Regrow1'])],
               'Lepidodendron ancestral · 6n-1: estados del cono, fuego y armadura que vuelve a crecer (×2)',
               ['Pivote de todas las hojas del cono = la base del cono (36, 98). La rama se alarga hasta la copa con ConeBranch_Tile (24 px) encima de la celda.'],
               'vista_previa_6n1_estados.png')
    _row_sheet([('Shatter (8 × 90 ms): la armadura rota salta a trozos → Exposed_Idle', out['Lepido_Cone_Shatter']),
                ('ShatterCharred', out['Lepido_Cone_ShatterCharred']),
                ('Exposed_Idle (6 × 120 ms) · Exposed_Hurt', out['Lepido_Cone_Exposed_Idle'] + out['Lepido_Cone_Exposed_Hurt']),
                ('Burst (10 × 80 ms): revienta (cuadro 3) → Husk + ConeBranch_Torn', out['Lepido_Cone_Burst']),
                ('Fx_ConeShadow (8) · Fx_ConeCrater', out['Fx_ConeShadow'] + out['Fx_ConeCrater']),
                ('Fx_ConeSlam (7 × 70 ms)', out['Fx_ConeSlam']),
                ('Fx_ConeLift (6) · Fx_ArmorChip (5) · Fx_ArmorChipCharred (5)', out['Fx_ConeLift'] + out['Fx_ArmorChip'] + out['Fx_ArmorChipCharred'])],
               'Lepidodendron ancestral · 6n-1: rotura de la armadura, cono expuesto, reventón y efectos (×2)',
               ['Shatter 128×112 (pivote 64, 104), Burst 128×120 (pivote 64, 114). Golpe: Fx_ConeSlam en la base del cono; queda Fx_ConeCrater.'],
               'vista_previa_6n1_animaciones.png')

def _tiles(t, n=3):
    im = Image.new('RGBA', (t.width, t.height * n), (0, 0, 0, 0))
    for j in range(n): im.alpha_composite(t, (0, j * t.height))
    return im

# =================================================================================================
# Escenas en la arena
# =================================================================================================
def _sheetf(folder, name, w):
    im = Image.open(os.path.join(HERE, folder, name + '.png')).convert('RGBA')
    return [im.crop((i * w, 0, i * w + w, im.height)) for i in range(im.width // w)]

def escenas(out):
    W, H = 50, 30
    fx, fy = 25, 15.5
    room = A.room(A.rect_room(W, H, (2, int(fy - 10.5), W - 3, H - 2)))
    X0, Y0 = int((fx - 20) * U), int((fy - 10.25) * U)
    def at(im, spr, px, py, x, y):                      # pega spr con su pivote (px, py) en el punto (x, y) del recorte
        im.alpha_composite(spr, (int(X0 + x - px), int(Y0 + y - py)))
    FX_, FY_ = int(fx * U) - X0, int(fy * U) - Y0       # pie del tronco en el recorte
    at(room, _sheetf('Tree', 'Lepido_BackRoots', 520)[0], 260, 196, FX_, FY_)
    at(room, _sheetf('Tree', 'Lepido_MoundShadow', 480)[0], 240, 96, FX_, FY_)
    at(room, _sheetf('Tree', 'Lepido_Mound', 480)[0], 240, 96, FX_, FY_)
    trunk_im = _sheetf('Tree', 'Lepido_Trunk', 288)[0]
    vrest = _sheetf('Tree', 'Lepido_Veins_Rest', 288)[0]
    vwarn = _sheetf('Tree', 'Lepido_Veins_Warn', 288); vhit = _sheetf('Tree', 'Lepido_Veins_Hit', 288)
    tiles = _sheetf('Canopy', 'Fx_Dapple', 256); shafts = _sheetf('Canopy', 'Fx_LightShaft', 118)
    pools = _sheetf('Canopy', 'Fx_LightPool', 52)
    sway = _sheetf('Canopy', 'Canopy_Arena_Sway', 704); shake = _sheetf('Canopy', 'Canopy_Arena_Shake', 704)
    leaves = _sheetf('Canopy', 'Fx_FallingLeaf', 16); spores = _sheetf('Canopy', 'Fx_FallingSpore', 6)
    smold = _sheetf('Walls', 'Fx_RootSmolder', 24)
    frog = A.frame(FROG, 0, 32)
    tile = out['Lepido_ConeBranch_Tile'][0]
    LX, LY = 176, 300                                   # donde golpea el cono (base), en el recorte
    rnd = random.Random(5)
    drops = [(rnd.uniform(0, 640), rnd.uniform(-120, 30), rnd.uniform(1.6, 3.2), rnd.random() < .3, rnd.randrange(6)) for _ in range(60)]

    def frame(st, t):
        im = room.copy()
        dl = Image.new('RGBA', im.size, (0, 0, 0, 0)); tl = tiles[(t // 2) % 8]
        for ty in range(0, im.height, 256):
            for tx in range(0, im.width, 256): dl.alpha_composite(tl, (tx, ty))
        im.alpha_composite(dl)
        at(im, trunk_im, 144, 352, FX_, FY_)
        vm, vk = st.get('veins', ('rest', 0))
        at(im, vrest if vm == 'rest' else vwarn[vk] if vm == 'warn' else vhit[vk], 144, 352, FX_, FY_)
        if st.get('crater'): at(im, out['Fx_ConeCrater'][0], 36, 16, LX, LY)
        if 'shadow' in st: at(im, out['Fx_ConeShadow'][st['shadow']], 36, 18, LX, LY)
        elif 'cone' in st and st.get('dy', 0) > -4: at(im, out['Fx_ConeShadow'][CONTACT], 36, 18, LX, LY)   # sombra de contacto
        slam = st.get('slam')
        if slam is not None:                            # la mitad de atrás de la onda va detrás del cono
            sl = out['Fx_ConeSlam'][slam]
            at(im, sl.crop((0, 0, 128, 36)), 64, 36, LX, LY)
        objs = []
        if 'cone' in st:
            spr, px, py = st['cone']; dy = st.get('dy', 0); jx = st.get('jx', 0)
            objs.append((LY + dy, 'cone', (spr, px, py, dy, jx, st.get('bare', False))))
        if 'torn' in st: objs.append((LY + st['torn'], 'torn', st['torn']))
        if 'frog' in st: objs.append((st['frog'][1], 'frog', st['frog']))
        for _, kind, data in sorted(objs, key=lambda o: o[0]):
            if kind == 'frog': at(im, frog, 16, 30, data[0], data[1])
            else:
                if kind == 'cone': spr, px, py, dy, jx, bare = data
                else: spr, px, py, dy, jx, bare = out['Lepido_ConeBranch_Torn'][0], 36, 98, data, 0, False
                ty = LY + dy - 98 - TILE_H              # la rama: tramos encima de la celda hasta la copa
                while not bare and ty > -TILE_H * 2:
                    at(im, tile, 36, 0, LX + jx, ty); ty -= TILE_H
                at(im, spr, px, py, LX + jx, LY + dy)
        if slam is not None:
            at(im, out['Fx_ConeSlam'][slam].crop((0, 36, 128, 64)), 64, 0, LX, LY)
        for (spr, x, y) in st.get('fx', []): at(im, spr, spr.width // 2, spr.height // 2, x, y)
        for j, (hx, hr) in enumerate(((9 * U + 32, 26), (30 * U + 32, 22))):
            gx = hx - 32; gy = -10 + int(112 * .58 * .62)
            at(im, shafts[(t + j * 3) % 8], 0, 0, int(gx - (1.4 * U / 2 + 8)), gy)
            pl = pools[(t + j * 2) % 8]
            at(im, pl, 0, 0, int(gx + 4.2 * U - pl.width / 2), int(gy + 15 * U - pl.height / 2))
        if 'leaves' in st:
            age = st['leaves']
            for (x, y0, v, sp, ph) in drops:
                yy = y0 + age * v * 9
                if 0 <= yy <= 330:
                    spr = spores[(age + ph) % 3] if sp else leaves[(age + ph) % 6]
                    at(im, spr, 0, 0, int(x + math.sin(age / 2 + ph) * 6), int(yy))
        cm, ck = st.get('canopy', ('sway', t % 8))
        at(im, sway[ck % 8] if cm == 'sway' else shake[ck % 6], 0, 0, -32, -10)
        return im.crop((X0, Y0, X0 + 640, Y0 + 360))

    C0 = lambda name, k=0: (out[name][k], 36, 98)
    def chip(k, burnt=False, side=-1):
        return (out['Fx_ArmorChipCharred' if burnt else 'Fx_ArmorChip'][k], LX + side * 16, LY - 34)

    # --- escena 1: aviso, bajada, golpe, romper la armadura, cono expuesto, reventón --------------------------
    S, ms, keys = [], [], {}
    def add(st, m, key=None):
        if key: keys[key] = len(S)
        S.append(st); ms.append(m)
    fr0 = (LX + 6, LY + 4)
    for k in range(3): add({'frog': fr0}, 140)
    for k in range(8):
        st = {'veins': ('warn', k), 'frog': (fr0[0] - k * 5, fr0[1] + k), 'shadow': k}
        if k >= 5: st.update(canopy=('shake', k), leaves=k - 5)
        add(st, 80, 'aviso' if k == 6 else None)
    frog_s = (fr0[0] - 40, fr0[1] + 8)
    D = LY - 40
    for j, s_ in enumerate((.25, .5, .7, .87, 1.0)):
        add({'frog': frog_s, 'shadow': 7, 'cone': C0('Lepido_Cone_Armor0'), 'dy': -D * (1 - s_ * s_), 'canopy': ('shake', j), 'leaves': 3 + j}, 50,
            'bajada' if j == 2 else None)
    for k in range(7):
        add({'frog': frog_s, 'cone': C0('Lepido_Cone_Armor0'), 'dy': (2, -2, 0, 0, 0, 0, 0)[k], 'slam': k, 'crater': True,
             'canopy': ('shake', k) if k < 4 else ('sway', k), 'leaves': 8 + k}, 70, 'golpe' if k == 1 else None)
    fh = (LX - 30, LY + 6)
    for s_ in range(3):                                 # tres golpes: intacta → astillada → rota → salta
        add({'frog': fh, 'cone': C0('Lepido_Cone_Armor%d' % s_), 'crater': True}, 160)
        add({'frog': fh, 'cone': C0('Lepido_Cone_Armor%d_Hurt' % s_, 0), 'crater': True, 'fx': [chip(0)]}, 60)
        if s_ < 2:
            add({'frog': fh, 'cone': C0('Lepido_Cone_Armor%d_Hurt' % s_, 1), 'crater': True, 'fx': [chip(1)]}, 60)
            for k in range(2, 5): add({'frog': fh, 'cone': C0('Lepido_Cone_Armor%d' % (s_ + 1)), 'crater': True, 'fx': [chip(k)]}, 70,
                                       'astillada' if (s_ == 0 and k == 3) else None)
    for k in range(8):
        add({'frog': fh, 'cone': (out['Lepido_Cone_Shatter'][k], SHAT_PX, SHAT_PY), 'crater': True}, 90, 'rotura' if k == 2 else None)
    for k in range(6): add({'frog': fh, 'cone': C0('Lepido_Cone_Exposed_Idle', k), 'crater': True}, IDLE_MS, 'expuesto' if k == 3 else None)
    for h in range(2):
        add({'frog': fh, 'cone': C0('Lepido_Cone_Exposed_Hurt', 0), 'crater': True, 'veins': ('hit', 0)}, 60)
        add({'frog': fh, 'cone': C0('Lepido_Cone_Exposed_Hurt', 1), 'crater': True, 'veins': ('hit', 1)}, 60)
        for k in range(4): add({'frog': fh, 'cone': C0('Lepido_Cone_Exposed_Idle', k), 'crater': True}, IDLE_MS)
    for k in range(10):
        add({'frog': fh, 'cone': (out['Lepido_Cone_Burst'][k], BURST_PX, BURST_PY), 'crater': True,
             'veins': ('hit', min(1, k // 2)) if k < 4 else ('rest', 0)}, 80, 'reventón' if k == 4 else None)
    for k in range(7):                                  # el cono roto se queda; la rama sube sola
        add({'frog': fh, 'cone': C0('Lepido_Cone_Husk'), 'bare': True, 'crater': True, 'torn': -k * k * 9}, 90, 'rama sube' if k == 3 else None)
    _escena(frame, S, ms, keys, 'Lepido_cono_romper',
            ['aviso', 'bajada', 'golpe', 'astillada', 'rotura', 'expuesto', 'reventón', 'rama sube'],
            'Lepidodendron ancestral · 6n-1 escena: un cono baja, golpea, le rompen la armadura y revienta',
            'vista_previa_6n1_escena_romper.png')

    # --- escena 2: baja, arde, se carboniza, recibe un golpe y, como no se rompe a tiempo, sube; la madera quemada está
    # muerta y no rebrota: la próxima vez baja carbonizada y con las mismas roturas ---
    S, ms, keys = [], [], {}
    frs = (LX - 46, LY + 10)
    for k in range(3): add({'frog': frs, 'veins': ('warn', 5 + k), 'shadow': 5 + k, 'canopy': ('shake', k), 'leaves': k}, 80)
    for j, s_ in enumerate((.35, .7, 1.0)):
        add({'frog': frs, 'shadow': 7, 'cone': C0('Lepido_Cone_Armor0'), 'dy': -D * (1 - s_ * s_), 'canopy': ('shake', j), 'leaves': 3 + j}, 60)
    for k in range(5):
        add({'frog': frs, 'cone': C0('Lepido_Cone_Armor0'), 'dy': (2, -2, 0, 0, 0)[k], 'slam': k, 'crater': True,
             'canopy': ('shake', k) if k < 3 else ('sway', k)}, 70)
    for k in range(BURN_N):
        add({'frog': frs, 'cone': (out['Lepido_Cone_Burn0'][k], 36, 98), 'crater': True}, BURN_MS, 'arde' if k == 3 else None)
    sm = lambda k: [(smold[k % 6], LX - 8, LY - 40), (smold[(k + 3) % 6], LX + 9, LY - 62)]
    for k in range(4): add({'frog': frs, 'cone': C0('Lepido_Cone_ArmorCharred0'), 'crater': True, 'fx': sm(k)}, 100, 'carbonizada' if k == 1 else None)
    fh = (LX - 30, LY + 6)
    add({'frog': fh, 'cone': C0('Lepido_Cone_ArmorCharred0_Hurt', 0), 'crater': True, 'fx': [chip(0, True)]}, 60)
    add({'frog': fh, 'cone': C0('Lepido_Cone_ArmorCharred0_Hurt', 1), 'crater': True, 'fx': [chip(1, True)]}, 60)
    for k in range(2, 5): add({'frog': fh, 'cone': C0('Lepido_Cone_ArmorCharred1'), 'crater': True, 'fx': [chip(k, True)] + sm(k)}, 70,
                               'golpe (más daño)' if k == 3 else None)
    for k in range(4): add({'frog': fh, 'cone': C0('Lepido_Cone_ArmorCharred1'), 'crater': True, 'fx': sm(k), 'jx': (1, -1)[k % 2] if k >= 2 else 0,
                            'canopy': ('shake', k) if k >= 2 else ('sway', k)}, 90)
    rise = (0, -6, -18, -38, -66, -104, -150, -205)
    for k in range(8):
        add({'frog': fh, 'cone': C0('Lepido_Cone_ArmorCharred1'), 'dy': rise[k], 'crater': True,
             'fx': [(out['Fx_ConeLift'][min(5, k)], LX, LY - 16)] if k < 6 else [], 'canopy': ('shake', k) if k < 5 else ('sway', k),
             'shadow': max(0, 7 - k)}, 90, 'sube (sigue quemada)' if k == 3 else None)
    for k in range(4): add({'frog': fh, 'crater': True}, 140)
    _escena(frame, S, ms, keys, 'Lepido_cono_fuego',
            ['arde', 'carbonizada', 'golpe (más daño)', 'sube (sigue quemada)'],
            'Lepidodendron ancestral · 6n-1 escena: el cono arde y, si no se rompe a tiempo, sube quemado',
            'vista_previa_6n1_escena_fuego.png')

def _escena(frame, S, ms, keys, gname, names, title, pname):
    frames = [frame(st, t) for t, st in enumerate(S)]
    os.makedirs(os.path.join(GIF, 'escenas'), exist_ok=True)
    o = [f.convert('RGB') for f in frames]
    o[0].save(os.path.join(GIF, 'escenas', gname + '.gif'), save_all=True, append_images=o[1:], duration=ms, loop=0)
    cw, ch = 300, 330                                   # recorte alrededor del cono, ×1
    cols = 4
    rows = (len(names) + cols - 1) // cols
    sheet = Image.new('RGBA', ((cw + 8) * cols, (ch + 26) * rows), (16, 18, 16, 255))
    d = ImageDraw.Draw(sheet)
    for j, nm in enumerate(names):
        f = frames[keys[nm]].crop((20, 20, 20 + cw, 20 + ch))
        x, y = (j % cols) * (cw + 8), (j // cols) * (ch + 26)
        sheet.alpha_composite(f, (x, y + 24))
        d.text((x + 6, y + 4), nm, fill=(120, 230, 230), font=A.font(16))
    G._save(sheet, title, ['Recorte de la arena alrededor del cono (×1, cámara ×0,75). GIF: gif/escenas/' + gname + '.gif'], pname)

if __name__ == '__main__':
    which = sys.argv[1:] or ['muestra']
    if 'muestra' in which: lamina_muestra()
    if 'piezas' in which or 'escenas' in which:
        out = piezas()
        laminas(out)
        if 'escenas' in which: escenas(out)
