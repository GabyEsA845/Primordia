"""El Nido de la Megarachne (concepto, tanda 6h → piezas definitivas en la 6j). Skill primordia-sprites (pixel.py).

Idea del usuario: en el centro de la sala, un Lepidodendron vivo y frondoso EMPAPADO (por eso el Nido no arde) y,
sobre su copa, la telaraña del Nido con un embudo grande (~3 u) por el que ella cabe. Los cables de la red en estrella
nacen de las puntas de las ramas del borde de la copa.
- Empapado: corteza oscura con regueros de agua, hojas brillantes de agua, musgo que gotea, charco al pie.
- Seco (enfado, 50 %): el chillido sacude el árbol, cae el agua, el musgo y las hojas amarillean; desde entonces el
  Nido puede arder (una vez).
- La copa se dibuja por encima del jugador y de las crías; cuando alguien pasa debajo, el código la vuelve
  translúcida.

Celda 160×176. Pivote = pie del tronco en el suelo (80, 164). Centro de la copa a 64 px por encima del suelo.

Uso: python generar_nido_megarachne.py          (lámina de concepto: empapado y seco, con y sin Nido)
     python generar_nido_megarachne.py piezas   (6i-2: árbol empapado/seco/carbonizado, ojos, sacudida, lluvia,
                                                 secado, Nido ardiendo, humo, seda que se deshace, ondas del chillido)
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
MT = os.path.normpath(os.path.join(HERE, '..', '..'))
sys.path.insert(0, os.path.join(MT, 'floor3-environment'))
from base_piso3 import *                               # noqa: F401,F403
from PIL import Image, ImageDraw

W_, H_ = 160, 176
FX, FY = 80, 164                                       # pie del tronco
CX, CY = 80, 100                                       # centro de la copa en pantalla (64 px sobre el suelo)
RX, RY = 64, 44                                        # radios de la copa (picada)
HOLE = (24, 15)                                        # semiejes del embudo (48 px ≈ 3 u de ancho)
STAR = {'NO': 205, 'N': 268, 'NE': 330, 'SO': 140, 'SE': 35}   # ángulo (grados, pantalla) de cada cable

WET_LEAF = [C('#12301e'), C('#1c4a2a'), C('#2e7038'), C('#5aa058')]      # hojas mojadas (más oscuras, frescas)
DRY_LEAF = [C('#3a3016'), C('#5e4c20'), C('#8a742c'), C('#b89e48')]      # secas (enfado)
SHINE = C('#bfe8f0')                                                       # brillo de agua
BARKW = [C(c) for c in ('#141a18', '#22302a', '#344638', '#4c6050', '#728a70')]   # corteza mojada
BARKD = [C(c) for c in BARK]
WATERC = [C(c) for c in WATER]
MOSSW = [C('#16301c'), C('#24502a'), C('#3a7a3a')]
MOSSD = [C('#3a2e18'), C('#5a4824'), C('#7e6834')]

def anchor(name):
    """Punta de rama del borde de la copa donde nace cada cable (px de la celda)."""
    a = math.radians(STAR[name])
    return (CX + (RX - 4) * math.cos(a), CY + (RY - 4) * math.sin(a))

def crown_mask(x, y, seed=3):
    a = math.atan2((y - CY) / RY, (x - CX) / RX)
    r = 1 + .09 * math.sin(a * 7 + seed) + .05 * math.sin(a * 13 + seed * 2)
    return ((x - CX) / RX) ** 2 + ((y - CY) / RY) ** 2 <= r * r

CHARB = [C(c) for c in CHAR]

def tree(dry=False, seed=5, mode=None):
    """mode: 'wet' (empapado), 'dry' (seco, tras el chillido) o 'charred' (copa quemada: ramas desnudas). La
    geometría es la MISMA en los tres (los detalles de cada estado usan su propio generador aleatorio), así que se
    pueden fundir píxel a píxel (secado, quemado)."""
    mode = mode or ('dry' if dry else 'wet')
    wet, charred = mode == 'wet', mode == 'charred'
    cv = Canvas(W_, H_)
    rnd = random.Random(seed)                             # geometría
    deco = random.Random(seed * 7 + 1)                    # detalles (agua, ascuas)
    leaf = WET_LEAF if wet else DRY_LEAF
    bark = BARKW if wet else CHARB if charred else BARKD
    # charco al pie (empapado), barro seco o barro con ceniza (quemado)
    for y in range(FY - 8, FY + 9):
        for x in range(FX - 30, FX + 31):
            d = ((x - FX) / 30) ** 2 + ((y - FY) / 8) ** 2
            if d <= 1:
                if wet: cv.put(x, y, WATERC[1] if d > .75 else WATERC[2] if d > .3 else WATERC[3])
                else: cv.put(x, y, C(MUD[1]) if d > .7 else C(MUD[2]))
    for k in range(5):
        x_, y_ = FX + deco.randint(-22, 22), FY + deco.randint(-4, 4)
        if wet: cv.put(x_, y_, WATERC[4])
        elif charred: cv.put(x_, y_, C(CHAR[3]))
    # tronco con cojinetes en rombo (pie ensanchado); regueros de agua o juntas en ascua
    bark_trunk(cv, FX - 9, 18, CY + 10, FY + 1, bark, ember=EMBER[2] if charred else None, seed=seed, ember_p=.12)
    if wet:
        for x0 in (FX - 5, FX + 2, FX + 6):
            for y in range(CY + 14, FY - 2):
                if (y + x0) % 5 < 3: cv.put(x0 + round(math.sin(y / 4) * .6), y, WATERC[3] if (y // 3) % 2 else WATERC[4])
    lit = lambda x, y: -((x - CX) / RX * .55 + (y - CY) / RY * .85)          # >0 hacia la luz
    if not charred:                                       # copa en cúpula
        for y in range(CY - RY - 8, CY + RY + 8):
            for x in range(CX - RX - 8, CX + RX + 8):
                if crown_mask(x, y, seed): cv.put(x, y, leaf[1] if lit(x, y) > .35 else leaf[0])
    tips = []
    def branch(x, y, ang, L, depth):
        w = 2 if charred and depth == 2 else 1
        for st in range(int(L)):
            px, py = x + math.cos(ang) * st, y + math.sin(ang) * st * .7
            for k in range(w): cv.put(px, py - k, bark[2] if not charred else bark[3] if k else bark[2])
            cv.put(px, py + 1, bark[0])
            if charred and deco.random() < .05: cv.put(px, py, C(EMBER[2]))
        ex, ey = x + math.cos(ang) * L, y + math.sin(ang) * L * .7
        if depth:
            for s_ in (-1, 1): branch(ex, ey, ang + s_ * .38, L * .62, depth - 1)
        else: tips.append((ex, ey, ang))
    for k in range(7):
        branch(CX, CY, k / 7 * 2 * math.pi + rnd.uniform(-.2, .2), 26, 2)
    tufts = []
    for gy in range(-RY, RY + 1, 9):                      # rejilla con jitter: penachos por toda la copa
        for gx in range(-RX, RX + 1, 11):
            x, y = CX + gx + rnd.uniform(-4, 4) + (5 if (gy // 9) % 2 else 0), CY + gy + rnd.uniform(-3, 3)
            if crown_mask(x, y, seed): tufts.append((x, y))
    tufts.sort(key=lambda p: p[1])                        # de atrás (arriba) a delante (abajo)
    for (x, y) in tufts:
        l = lit(x, y)
        shade = 0 if l > .3 else 1 if l > -.4 else 2
        nf = rnd.randrange(7, 10); a0 = rnd.uniform(0, 6.28)
        fr = [(a0 + f * 6.283 / nf + rnd.uniform(-.2, .2), rnd.uniform(6, 9)) for f in range(nf)]
        if charred:                                       # quemado: solo quedan muñones de ramitas
            for a, L in fr[::3]: twig(cv, x, y, a, L * .55, CHARB[:4], None, True, deco)
        else:
            for a, L in fr: frondlet(cv, x, y, a, L, leaf, .15, 2, None, shade)
        if wet and deco.random() < .55: cv.put(x + deco.randint(-3, 3), y + deco.randint(-3, 1), SHINE)   # agua que brilla
    # recorta lo que se sale de la copa (salvo el tronco) y musgo que gotea por el borde de abajo
    for (x, y) in list(cv.px):
        if y < CY + 16 and not crown_mask(x, y, seed) and not (FX - 10 <= x <= FX + 10 and y > CY):
            if ((x - CX) / (RX + 6)) ** 2 + ((y - CY) / (RY + 6)) ** 2 > 1: del cv.px[(x, y)]
    moss = MOSSW if wet else MOSSD
    for k in range(9):
        a = math.radians(20 + k * 16)
        x0, y0 = CX + RX * .92 * math.cos(a), CY + RY * .92 * math.sin(a)
        L = rnd.randint(6, 13)
        if charred: continue
        for j in range(L):
            cv.put(x0 + round(math.sin(j / 3 + k) * .6), y0 + j, moss[2] if j < 2 else moss[1] if j < L - 2 else moss[0])
        if wet and k % 2 == 0: cv.put(x0, y0 + L + 2, WATERC[4]); cv.put(x0, y0 + L + 1, WATERC[3])
    cv.outline(); cv.clean()
    return cv.img().convert('RGBA')

def nest_layer(seed=3, fresh=False):
    """Telaraña del Nido sobre la copa: lámina de seda entre las ramas, radios hasta las puntas donde nacen los
    cables, espiras irregulares y el embudo oscuro de ~3 u."""
    im = Image.new('RGBA', (W_, H_), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    rnd = random.Random(seed)
    silk = (232, 228, 240) if fresh else (222, 218, 232)
    R = lambda a, f: (CX + RX * .82 * f * math.cos(a) * (1 + .08 * math.sin(a * 5 + seed)),
                      CY + RY * .82 * f * math.sin(a) * (1 + .08 * math.sin(a * 5 + seed)))
    angs = [k / 60 * 2 * math.pi for k in range(60)]
    d.polygon([R(a, 1) for a in angs], fill=silk + (70,))
    d.polygon([R(a, .7) for a in angs], fill=silk + (60,))
    for k in range(20):                                   # radios
        a = k / 20 * 2 * math.pi + rnd.uniform(-.06, .06)
        d.line([R(a, .3), R(a, 1.05)], fill=silk + (180,), width=1)
    for name in STAR:                                     # hebras gruesas hacia las puntas de los cables
        ax, ay = anchor(name); a = math.atan2((ay - CY) / RY, (ax - CX) / RX)
        d.line([R(a, .32), (ax, ay)], fill=silk + (235,), width=2)
    for f in (.42, .58, .74, .9):                         # espiras
        d.line([R(a, f + .03 * math.sin(a * 7 + f * 9)) for a in angs] + [R(0, f)], fill=silk + (140,), width=1)
    hx, hy = HOLE                                         # embudo: borde grueso, garganta oscura
    d.ellipse([CX - hx - 5, CY - hy - 4, CX + hx + 5, CY + hy + 4], fill=silk + (210,))
    d.ellipse([CX - hx, CY - hy, CX + hx, CY + hy], fill=(30, 24, 34, 250))
    d.ellipse([CX - hx + 6, CY - hy + 5, CX + hx - 4, CY + hy - 1], fill=(14, 10, 16, 255))
    d.ellipse([CX - hx + 14, CY - hy + 10, CX + hx - 10, CY + hy - 2], fill=(6, 4, 8, 255))
    for k in range(10):                                   # hebras que bajan por la garganta
        a = math.pi + k / 9 * math.pi
        d.line([(CX + hx * math.cos(a), CY + hy * math.sin(a)), (CX + hx * .45 * math.cos(a), CY + 3 + hy * .45 * math.sin(a))],
               fill=silk + (90,), width=1)
    return im


# =================================================================================================
# 6i-2: piezas de los momentos del árbol del Nido (misma celda 160×176 y pivote que el árbol)
# =================================================================================================
def _noise(seed=11):
    rnd = random.Random(seed)
    import numpy as np
    n = np.zeros((H_, W_))
    for k in range(5):
        fx, fy, ph = rnd.uniform(.03, .12), rnd.uniform(.03, .12), rnd.uniform(0, 6.28)
        yy, xx = np.mgrid[0:H_, 0:W_]
        n += np.sin(xx * fx + yy * fy + ph)
    return (n - n.min()) / (n.max() - n.min())

def blend_mask(a, b, mask):
    """Píxel a píxel: donde mask es True se toma b, si no a."""
    import numpy as np
    A_, B_ = np.array(a), np.array(b)
    out = np.where(mask[..., None], B_, A_)
    return Image.fromarray(out.astype('uint8'), 'RGBA')

def shake(img, dx, dy):
    """Sacude la copa (lo que está por encima del arranque del tronco) sin mover el tronco."""
    top = CY + 16
    out = img.copy()
    crown = img.crop((0, 0, W_, top))
    ImageDraw.Draw(out).rectangle([0, 0, W_, top - 1], fill=(0, 0, 0, 0))
    out.alpha_composite(crown, (dx, dy))
    return out

def drying(n=6):
    """Empapado → seco: la copa amarillea en manchas que se extienden (de arriba abajo)."""
    import numpy as np
    wet, dry = tree(mode='wet'), tree(mode='dry')
    yy, xx = np.mgrid[0:H_, 0:W_]
    rnd = random.Random(12)
    T = np.full((H_, W_), 9.0)                            # momento en que se seca cada píxel: manchas que crecen
    for k in range(18):
        sx, sy, t0 = CX + rnd.uniform(-RX, RX), CY + rnd.uniform(-RY, RY * .6), rnd.uniform(0, .45)
        T = np.minimum(T, t0 + np.hypot((xx - sx) / RX, (yy - sy) / RY) * .9)
    T[yy > CY + 16] = .5 + (yy[yy > CY + 16] - CY) / H_ * .6          # tronco y pie: se secan al final
    return [blend_mask(wet, dry, T < (i + 1) / n * 1.1) for i in range(n)]

def eyes(k):
    """Ojos en el fondo del embudo (capa): k = 0-5 (se encienden de 2 en 2 y luego laten)."""
    im = Image.new('RGBA', (W_, H_), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    pos = [(-3, 2), (3, 2), (-7, 0), (7, 0), (-2, -1), (2, -1), (-10, 3), (10, 3)]    # 2 grandes + ocelos
    lit = (0, 2, 4, 8, 8, 8)[k]
    glow = (0, 0, 1, 2, 3, 2)[k]
    for i, (dx, dy) in enumerate(pos[:lit]):
        x, y = CX + dx, CY + 3 + dy
        if glow: d.ellipse([x - 2, y - 2, x + 2, y + 2], fill=(255, 60, 40, 50 * glow))
        r = 1 if i < 2 else 0
        d.rectangle([x - r, y, x + r, y], fill=(255, 70, 50, 255))
        if i < 2: d.point((x, y - 1), fill=(255, 200, 160, 255))
    return im

def rain(k, n=8, seed=4):
    """Lluvia de gotas al sacudir el árbol (capa, bucle de 8): caen del borde de la copa al suelo y salpican."""
    im = Image.new('RGBA', (W_, H_), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    rnd = random.Random(seed)
    for j in range(26):
        a = rnd.uniform(.05, .95) * math.pi
        x = CX + RX * .95 * math.cos(a) * rnd.uniform(.4, 1)
        y0 = CY + RY * .7 * math.sin(a)
        L = FY + rnd.randint(-6, 6) - y0
        t = ((k / n) + rnd.random()) % 1
        y = y0 + L * t
        d.line([(x, y - 3), (x, y)], fill=WATERC[3][:3] + (200,), width=1); d.point((x, y), fill=WATERC[4])
        if t > .85:
            for dx in (-2, 2): d.point((x + dx, y0 + L - 1), fill=WATERC[4])
    return im

def screech(k, n=6):
    """Ondas del chillido desde el embudo (192×128, pivote = centro del embudo): anillos que se abren."""
    im = Image.new('RGBA', (192, 128), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    for j in range(3):
        t = ((k / n) + j / 3) % 1
        rx, ry = 24 + 70 * t, (24 + 70 * t) * .62
        a = int(255 * (1 - t) ** .7)
        for w, col in ((5, (255, 90, 80, a // 3)), (2, (255, 200, 190, a))):
            d.ellipse([96 - rx, 64 - ry, 96 + rx, 64 + ry], outline=col, width=w)
    return im

def burn(n=8, origin='SO'):
    """El Nido arde (tras el enfado): el fuego entra por la rama de un cable y recorre la seda y la copa seca;
    detrás queda la copa carbonizada. Hoja completa (árbol + Nido) de n cuadros; el último = carbonizado."""
    import numpy as np
    sys.path.insert(0, os.path.join(MT, 'floor3-mutations'))
    dry = tree(mode='dry'); dry.alpha_composite(nest_layer())
    ch = tree(mode='charred')
    ax, ay = anchor(origin)
    yy, xx = np.mgrid[0:H_, 0:W_]
    dist = np.hypot((xx - ax) / RX, (yy - ay) / RY) / 2.1 + _noise(9) * .18
    crown = (np.array(dry)[..., 3] > 0) | (np.array(ch)[..., 3] > 0)        # todo: copa, seda, tronco y pie
    fire = Image.open(os.path.join(MT, 'floor3-mutations', 'Effects', 'Fx_GroundFire.png')).convert('RGBA')
    fl = [fire.crop((i * 32, 0, i * 32 + 32, 24)) for i in range(fire.width // 32)]
    rnd = random.Random(5)
    out = []
    for i in range(n):
        thr = (i + 1) / (n - 2)                           # el frente; los 2 últimos ya todo quemado
        burnt = crown & (dist < thr - .18)
        burning = crown & (dist >= thr - .18) & (dist < thr)
        im = blend_mask(dry, ch, burnt)
        arr = np.array(im)
        m = burning & (arr[..., 3] > 0)
        arr[m, :3] = (arr[m, :3] * .35 + np.array([90, 30, 14]) * .65).astype('uint8')   # al rojo bajo las llamas
        im = Image.fromarray(arr, 'RGBA')
        pts = list(zip(*np.nonzero(burning & (np.array(dry)[..., 3] > 0))))
        rnd.shuffle(pts)
        for (y, x) in pts[:22]:
            f = fl[rnd.randrange(len(fl))]
            im.alpha_composite(f, (int(x) - 16, int(y) - 20))
        out.append(im)
    out[-1] = ch.copy()                                   # el último, exactamente el árbol carbonizado
    return out

def smolder(k, n=4, seed=6):
    """Copa carbonizada que humea (bucle de 4): hilos de humo que suben de las ramas y ascuas que parpadean."""
    im = tree(mode='charred')
    lay = Image.new('RGBA', (W_, H_), (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    rnd = random.Random(seed)
    for j in range(7):
        x0, y0 = CX + rnd.uniform(-RX * .7, RX * .7), CY + rnd.uniform(-RY * .5, RY * .5)
        for q in range(4):
            t = ((k / n) + q / 4) % 1
            r = 2 + t * 4
            x, y = x0 + 3 * math.sin(t * 5 + j), y0 - 6 - t * 30
            d.ellipse([x - r, y - r, x + r, y + r], fill=(150, 144, 146, int(185 * (1 - t))))
        if (k + j) % 2: d.point((x0, y0), fill=(255, 160, 70, 255))
    im.alpha_composite(lay)
    return im

def web_decay(k, n=6, seed=8):
    """Muerte: la seda del Nido se descuelga y se deshace (capa sobre el árbol; el último cuadro, vacío)."""
    import numpy as np
    web = np.array(nest_layer()).astype(float)
    t = k / (n - 1)
    yy, xx = np.mgrid[0:H_, 0:W_]
    r = np.clip(np.hypot((xx - CX) / RX, (yy - CY) / RY), 0, 1)
    sag = (1 - r ** 2) * 14 * t                           # el centro cede más
    src_y = np.clip((yy - sag).astype(int), 0, H_ - 1)
    out = web[src_y, xx]
    out[..., 3] *= (1 - t) ** 1.3
    im = Image.fromarray(out.astype('uint8'), 'RGBA')
    d = ImageDraw.Draw(im); rnd = random.Random(seed)
    for j in range(10):                                   # jirones que cuelgan
        a = rnd.uniform(0, 6.28); f = rnd.uniform(.3, .9)
        x, y = CX + RX * .8 * f * math.cos(a), CY + RY * .8 * f * math.sin(a) + 14 * t * (1 - f * f)
        L = 4 + 22 * t * rnd.uniform(.5, 1)
        d.line([(x, y), (x + rnd.uniform(-2, 2), y + L)], fill=(222, 218, 232, int(200 * (1 - t))), width=1)
    return im

def piezas():
    """Guarda las piezas de la 6i-2 en Nido/ y Effects/ y la lámina vista_previa_6i2_arbol.png."""
    sys.path.insert(0, HERE)
    from generar_megarachne import grid, gif, preview, BG                           # noqa
    os.makedirs(os.path.join(HERE, 'Nido'), exist_ok=True); os.makedirs(os.path.join(HERE, 'Effects'), exist_ok=True)
    def strip(frames, name, folder='Nido'):
        w, h = frames[0].size
        im = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
        for i, f in enumerate(frames): im.paste(f, (i * w, 0))
        im.save(os.path.join(HERE, folder, name + '.png'))
        return frames
    wet, dry = tree(mode='wet'), tree(mode='dry')
    rows = {}
    rows['Tree_Wet / Dry / Charred'] = strip([wet], 'Nido_Tree_Wet') + strip([dry], 'Nido_Tree_Dry') + strip([tree(mode='charred')], 'Nido_Tree_Charred')
    rows['Web (capa)'] = strip([nest_layer()], 'Nido_Web')
    rows['Eyes (capa)'] = strip([eyes(k) for k in range(6)], 'Nido_Eyes')
    rows['Tree_Shake'] = strip([shake(wet, dx, dy) for dx, dy in ((2, 0), (-2, 1), (2, -1), (-2, 0), (1, 1), (0, 0))], 'Nido_Tree_Shake')
    rows['Rain (capa)'] = strip([rain(k) for k in range(8)], 'Fx_Nido_Rain')
    rows['Tree_Drying'] = strip(drying(), 'Nido_Tree_Drying')
    rows['Burn (árbol + Nido)'] = strip(burn(), 'Nido_Burn')
    rows['Charred_Smolder'] = strip([smolder(k) for k in range(4)], 'Nido_Tree_Charred_Smolder')
    rows['WebDecay (capa)'] = strip([web_decay(k) for k in range(6)], 'Nido_WebDecay')
    sc = strip([screech(k) for k in range(6)], 'Fx_Megarachne_Screech', 'Effects')
    # vista previa: las capas sobre el árbol para que se lean
    view = {}
    for name, fr in rows.items():
        base = wet if name in ('Web (capa)', 'Eyes (capa)', 'Rain (capa)') else dry if name == 'WebDecay (capa)' else None
        if base is not None:
            out = []
            for f in fr:
                b = base.copy()
                if name == 'Eyes (capa)': b.alpha_composite(nest_layer())
                b.alpha_composite(f); out.append(b)
            fr = out
        view[name] = fr
    tw = wet.copy(); tw.alpha_composite(nest_layer())
    view['Screech (sobre el embudo)'] = []
    for f in sc:
        b = Image.new('RGBA', (W_ + 40, H_), (0, 0, 0, 0)); b.alpha_composite(tw, (20, 0))
        b.alpha_composite(f, (20 + CX - 96, CY - 64)); view['Screech (sobre el embudo)'].append(b.crop((20, 0, 20 + W_, H_)))
    preview(os.path.join(HERE, 'vista_previa_6i2_arbol.png'), view, BG, scale=2)
    for name, fr in view.items():
        if len(fr) > 1: gif(os.path.join(HERE, 'gif', 'Nido_' + name.split(' ')[0].replace('/', '') + '.gif'), fr, 100, BG, scale=2)
    print('Listo 6i-2 (árbol)')

def lamina():
    sys.path.insert(0, HERE)
    import generar_megarachne as MG
    import generar_escala_megarachne as E
    cols = []
    for dry in (False, True):
        t = tree(dry)
        both = t.copy(); both.alpha_composite(nest_layer(fresh=dry))
        cols.append((t, both))
    floor = MG.smoke_floor(W_ * 4 + 60, H_ + 20, seed=6)
    for i, (t, both) in enumerate(cols):
        floor.alpha_composite(t, (10 + i * (2 * W_ + 20), 10))
        floor.alpha_composite(both, (10 + i * (2 * W_ + 20) + W_, 10))
    s = 3
    big = floor.resize((floor.width * s, floor.height * s), Image.NEAREST)
    d = ImageDraw.Draw(big)
    from generar_escala_arthropleura import font as F
    labels = ['empapado', 'empapado + Nido', 'seco (enfado)', 'seco + Nido (seda nueva)']
    for i, l in enumerate(labels):
        d.text(((10 + i * W_ + (i // 2) * 20) * s + 8, 8), l, fill=(255, 240, 200), font=F(20), stroke_width=3, stroke_fill=(10, 10, 14))
    out = os.path.join(HERE, 'vista_previa_6h_arbol_nido.png')
    big.convert('RGB').save(out); print('  ', os.path.basename(out), big.size)

if __name__ == '__main__':
    cmd = sys.argv[1] if len(sys.argv) > 1 else 'lamina'
    if cmd == 'piezas': piezas()
    else: lamina()
