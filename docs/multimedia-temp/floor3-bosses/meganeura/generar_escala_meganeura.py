"""Láminas de escala de la Meganeura (tanda 6, paso 0): NO es el arte final.

Una Meganeura provisional (picada alta, ~10 u de envergadura = 160 px) sobre salas de la Tierra quemada armadas con los
tiles de la tanda 1a, con la rana del jugador (32 px) y los recuadros de la cámara del juego (480×270 px = 30×16,9 u).
Sirve para elegir el tamaño de la sala y comprobar cómo se lee el jefe frente al jugador.

- Sala A: suelo 40×24 u. Sala B: suelo 48×28 u. En las dos: la Meganeura a ras de suelo junto a la rana (tras un
  picado) y, en otra parte, solo su sombra (vuela alto). Charcas con huevos provisionales, Sigillarias y troncos
  (blancos del picado), helechos quemados, fuegos (los apaga la lluvia de la F3) y niebla de metano.
- Paletas: tres propuestas de color sobre el suelo quemado (hoja de estilo, referencia del usuario y mezcla).

Uso: python generar_escala_meganeura.py
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
MT = os.path.normpath(os.path.join(HERE, '..', '..'))
sys.path.insert(0, os.path.join(MT, 'floor3-environment'))
sys.path.insert(0, os.path.join(HERE, '..', 'arthropleura'))
import generar_tilesets_piso3 as T
import generar_escala_arthropleura as A                # rect_room, frame, font, cam_box
from base_piso3 import *                               # noqa: F401,F403
from PIL import Image, ImageDraw

U = 16
CAM = (480, 270)
PROPS = os.path.join(MT, 'floor3-environment', 'Burnt', 'props')
FIRE_FX = os.path.join(MT, 'floor3-mutations', 'Effects', 'Fx_GroundFire.png')
FROG = os.path.join(MT, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png')
SPAN = 160                                             # envergadura en px (10 u)
SQUASH = .84                                           # picada alta: lo que mira hacia arriba se acorta un poco

# -------------------------------------------------------------------------------------------------
# Paletas provisionales
# -------------------------------------------------------------------------------------------------
PALS = {
    'A': dict(nombre='A · hoja de estilo (tanda 0)',
              body=['#0a2c34', '#124e5a', '#1e7a8a', '#3aa6ae', '#94e0d4'], band='#081e24',
              stripe=None, eye=('#0c2026', '#5ad0c8'),
              wing=(232, 176, 80, 92), vein=(176, 88, 26, 170), stig='#8a3e10', base=None),
    'B': dict(nombre='B · como la referencia',
              body=['#3a1c0e', '#6e3618', '#b0622a', '#d88e3c', '#f2c472'], band='#2a140a',
              stripe=None, eye=('#2a0e0c', '#c04a30'),
              wing=(186, 204, 216, 104), vein=(40, 48, 56, 175), stig='#3a2c24', base=None),
    'C': dict(nombre='C · mezcla: dibujo de la referencia en verde azulado',
              body=['#0a2c34', '#124e5a', '#1e7a8a', '#3aa6ae', '#94e0d4'], band='#081e24',
              stripe='#e8b050', eye=('#1a0e10', '#c04a30'),
              wing=(186, 208, 214, 100), vein=(36, 50, 56, 175), stig='#b0581a', base=(232, 176, 80, 70)),
}

def rgba(c, a=255):
    c = C(c) if isinstance(c, str) else c
    return tuple(c[:3]) + (a,)

# -------------------------------------------------------------------------------------------------
# Cuerpo provisional (local: f = hacia delante, s = hacia un lado; px)
# -------------------------------------------------------------------------------------------------
def to_screen(cx, cy, ang, pts):
    ca, sa = math.cos(ang), math.sin(ang)
    return [(cx + f * ca - s * sa, cy + (f * sa + s * ca) * SQUASH) for (f, s) in pts]

def wing_poly(side, root_f, L, sweep, c0, cm, n=16):
    """Borde de ataque casi recto; borde de salida curvo (más ancho hacia el medio) y punta redondeada."""
    lead, trail = [], []
    for k in range(n + 1):
        t = k / n
        s = side * (7 + t * L)
        fl = root_f + sweep * t + 2 * math.sin(t * math.pi)
        ch = c0 + (cm - c0) * math.sin(min(1, t / .45) * math.pi / 2) - cm * .25 * max(0, t - .45) / .55
        if t > .82: ch *= math.sqrt(max(0, 1 - ((t - .82) / .18) ** 2)) * .75 + .25
        lead.append((fl, s)); trail.append((fl - ch, s))
    return lead, trail

def wings(side):
    half = SPAN / 2 - 7
    return [wing_poly(side, 7, half, 8, 7, 13),               # ala anterior: estrecha, algo hacia delante
            wing_poly(side, -2, half - 5, -12, 18, 15)]       # ala posterior: base ancha, barrida hacia atrás

def body_parts():
    """Abdomen (lista de (f, ancho)), tórax y cabeza."""
    ab = [(-9 - k * 7.5, 6.2 - k * .35) for k in range(9)]
    return ab

def meganeura(im, x, y, ang, pal, alt=0, shadow=True, only_shadow=False):
    """(x, y) = punto del suelo bajo el tórax (px); alt = altura del dibujo sobre su sombra (px)."""
    P = PALS[pal]
    lay = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    ab = body_parts()

    def silhouette(dd, cx, cy, col):
        for side in (-1, 1):
            for lead, trail in wings(side):
                dd.polygon(to_screen(cx, cy, ang, lead + trail[::-1]), fill=col)
        for (f, w) in ab:
            dd.ellipse(_ell(cx, cy, ang, f, 0, 4.5, w / 2), fill=col)
        dd.ellipse(_ell(cx, cy, ang, 0, 0, 11, 8), fill=col)
        dd.ellipse(_ell(cx, cy, ang, 14, 0, 6, 10), fill=col)

    if shadow or only_shadow:                                   # sombra: misma silueta, plana y oscura
        sl = Image.new('RGBA', im.size, (0, 0, 0, 0)); sd = ImageDraw.Draw(sl)
        silhouette(sd, x, y, (6, 4, 8, 255))
        k = 1.0 if only_shadow else .9
        a = 105 if only_shadow else 120
        px = sl.load()
        out = Image.new('RGBA', im.size, (0, 0, 0, 0)); po = out.load()
        for j in range(max(0, int(y - 90)), min(im.height, int(y + 90))):
            for i in range(max(0, int(x - 95)), min(im.width, int(x + 95))):
                if px[i, j][3] and (BAYER[j % 4][i % 4] < 13 or not only_shadow):
                    po[i, j] = (6, 4, 8, a)
        if not only_shadow and k != 1.0:
            w, h = out.size
            out = out.resize((int(w * k), int(h * k)), Image.NEAREST)
            out2 = Image.new('RGBA', im.size, (0, 0, 0, 0))
            out2.alpha_composite(out, (int(x - x * k), int(y - y * k))); out = out2
        im.alpha_composite(out)
        if only_shadow: return
    cy = y - alt
    for side in (-1, 1):                                        # alas: translúcidas con venas y pterostigma
        for lead, trail in wings(side):
            d.polygon(to_screen(x, cy, ang, lead + trail[::-1]), fill=P['wing'])
            if P['base']:
                n = len(lead) // 3
                d.polygon(to_screen(x, cy, ang, lead[:n] + trail[:n][::-1]), fill=P['base'])
            for fr in (.33, .66):
                d.line(to_screen(x, cy, ang, [(l[0] - (l[0] - t[0]) * fr, l[1]) for l, t in zip(lead, trail)]), fill=P['vein'], width=1)
            d.line(to_screen(x, cy, ang, lead), fill=P['vein'], width=1)
            d.line(to_screen(x, cy, ang, trail), fill=P['vein'], width=1)
            for k in range(2, len(lead) - 1, 2):                # venas cruzadas
                d.line(to_screen(x, cy, ang, [lead[k], trail[k]]), fill=P['vein'][:3] + (110,), width=1)
            st = lead[-3]
            d.polygon(to_screen(x, cy, ang, [st, lead[-2], (lead[-2][0] - 3, lead[-2][1]), (st[0] - 3, st[1])]), fill=C(P['stig']))
    B = P['body']
    for i, (f, w) in enumerate(ab[::-1]):                       # abdomen de la punta al tórax
        d.ellipse(_ell(x, cy, ang, f, 0, 4.6, w / 2), fill=C(B[2]), outline=C(B[0]))
        d.line(to_screen(x, cy, ang, [(f - 3.5, -w / 2 + 1), (f - 3.5, w / 2 - 1)]), fill=C(P['band']), width=1)
        d.line(to_screen(x, cy, ang, [(f + 2, -w / 4), (f - 2, -w / 4)]), fill=C(B[3]), width=1)
    tip = ab[-1][0]                                             # cercos
    for s in (-1, 1): d.line(to_screen(x, cy, ang, [(tip - 3, s * 1), (tip - 9, s * 3)]), fill=C(B[1]), width=1)
    d.ellipse(_ell(x, cy, ang, 0, 0, 12, 9), fill=C(B[2]), outline=C(B[0]))   # tórax
    d.ellipse(_ell(x, cy, ang, 2, -2, 6, 4), fill=C(B[3]))
    if P['stripe']:
        for s in (-1, 1): d.line(to_screen(x, cy, ang, [(7, s * 4), (-7, s * 5)]), fill=C(P['stripe']), width=1)
    else:
        for s in (-1, 1): d.line(to_screen(x, cy, ang, [(7, s * 4), (-7, s * 5)]), fill=C(P['band']), width=1)
    for s in (-1, 1):                                           # ojos compuestos grandes
        d.ellipse(_ell(x, cy, ang, 15, s * 4.5, 5.5, 5.5), fill=C(P['eye'][0]), outline=OUTL)
        e = to_screen(x, cy, ang, [(17, s * 5.5)])[0]
        d.point((round(e[0]), round(e[1])), fill=C(P['eye'][1]))
    d.ellipse(_ell(x, cy, ang, 19, 0, 2.5, 3), fill=C(B[1]))     # boca
    im.alpha_composite(lay)

def _ell(cx, cy, ang, f, s, rf, rs):
    """Caja de una elipse local (aproximada: se gira el centro y se toma el radio mayor en pantalla)."""
    (x, y), = to_screen(cx, cy, ang, [(f, s)])
    ca, sa = abs(math.cos(ang)), abs(math.sin(ang))
    rx = rf * ca + rs * sa
    ry = (rf * sa + rs * ca) * SQUASH
    return [x - rx, y - ry, x + rx, y + ry]

# -------------------------------------------------------------------------------------------------
# Sala de la Tierra quemada
# -------------------------------------------------------------------------------------------------
_B = None
def room(rows):
    global _B
    if _B is None: _B = T.build('Burnt')
    T.SAMPLE = rows
    return T.sample_room(_B, 1)[1]

def put(im, spr, x, y, anchor='foot'):
    oy = spr.height - 4 if anchor == 'foot' else spr.height / 2
    im.alpha_composite(spr, (int(x * U - spr.width / 2), int(y * U - oy)))

def pool(im, x, y, kind, eggs=0, seed=0):
    w = {'Island': 50, 'Lobed': 46, 'Kidney': 46, 'Round': 34, 'Long': 58}[kind]
    spr = A.frame(os.path.join(PROPS, f'Puddle_Water_{kind}.png'), 0, w)
    spr = spr.resize((spr.width * 2, spr.height * 2), Image.NEAREST)          # charca grande (cabe un picado)
    put(im, spr, x, y, 'center')
    rnd = random.Random(seed)
    d = ImageDraw.Draw(im)
    for k in range(eggs):                                       # huevos provisionales
        ex = x * U + rnd.uniform(-w * .5, w * .5); ey = y * U + rnd.uniform(-8, 8)
        d.ellipse([ex - 4, ey - 5, ex + 4, ey + 5], fill=C('#c8d88a'), outline=OUTL)
        d.point((round(ex - 1), round(ey - 2)), fill=C('#f4ffd0'))

def mist(im, x, y, w, h, seed):
    spr = A.frame(os.path.join(PROPS, 'Methane_Mist.png'), 0, 32)
    rnd = random.Random(seed)
    for j in range(h):
        for i in range(w):
            if rnd.random() < .85:
                im.alpha_composite(spr, (int((x + i * 2) * U - 16 + rnd.randint(-4, 4)), int((y + j * 2) * U - 16 + rnd.randint(-3, 3))))

def scene(fw, fh, seed):
    W, H, fx0, fy0 = fw + 4, fh + 6, 2, 3
    rows = A.rect_room(W, H, (fx0, fy0, fx0 + fw - 1, fy0 + fh - 1))
    im = room(rows)
    X = lambda t: fx0 + fw * t
    Y = lambda t: fy0 + fh * t
    rnd = random.Random(seed)
    # charcas (2 con huevos) y niebla de metano
    pool(im, X(.2), Y(.3), 'Island', 2, 1)
    pool(im, X(.72), Y(.22), 'Lobed', 2, 2)
    pool(im, X(.55), Y(.78), 'Kidney', 0, 3)
    # helechos quemados, Sigillarias y troncos (blancos del picado), fuegos y ascuas
    spots = [(X(.38), Y(.18), 'tree'), (X(.85), Y(.55), 'tree'), (X(.12), Y(.9), 'tree'), (X(.62), Y(.46), 'trunk'),
             (X(.3), Y(.6), 'log'), (X(.82), Y(.88), 'log'), (X(.47), Y(.95), 'stump')]
    for k in range(int(fw * fh / 70)):
        spots.append((rnd.uniform(X(.03), X(.97)), rnd.uniform(Y(.08), Y(.98)), 'fern'))
    ferns = [('Fern_Leafy_Sway', 84), ('Fern_Tree_Sway', 84), ('Fern_Bent_Sway', 88), ('Fern_Young_Sway', 44), ('Fern_Sparse_Sway', 92)]
    fire = [A.frame(FIRE_FX, i, 32) for i in range(6)]
    for (x, y, k) in sorted(spots, key=lambda p: p[1]):
        if k == 'fern':
            n, w = rnd.choice(ferns); spr = A.frame(os.path.join(PROPS, n + '.png'), 0, w)
            if rnd.random() < .5: spr = spr.transpose(Image.FLIP_LEFT_RIGHT)
            put(im, spr, x, y)
            if rnd.random() < .3: put(im, fire[rnd.randrange(6)], x, y + .2)
        else:
            name = {'tree': 'Obstacle_Sigillaria', 'trunk': 'Obstacle_BrokenTrunk', 'log': 'Obstacle_FallenLog', 'stump': 'Obstacle_Stump'}[k]
            put(im, Image.open(os.path.join(PROPS, name + '.png')).convert('RGBA'), x, y)
    emb = A.frame(os.path.join(PROPS, 'Deco_Embers.png'), 0, 16)
    for k in range(int(fw * fh / 40)):
        put(im, emb, rnd.uniform(X(.03), X(.97)), rnd.uniform(Y(.05), Y(.98)), 'center')
    return im, X, Y

def player(im, x, y):
    put(im, A.frame(FROG, 0, 32), x, y + .75)

# -------------------------------------------------------------------------------------------------
# Anotaciones
# -------------------------------------------------------------------------------------------------
WHITE, YEL, ORA, CYAN = (255, 255, 255), (255, 214, 90), (255, 140, 60), (120, 230, 230)

def tag(d, x, y, text, col, s):
    d.text((x * U * s, y * U * s), text, fill=col, font=A.font(15), stroke_width=3, stroke_fill=(10, 10, 14))

def finish(im, title, notes, cams, s, out, labels=()):
    big = im.resize((im.width * s, im.height * s), Image.NEAREST)
    d = ImageDraw.Draw(big)
    for (cx, cy, z, col, label) in cams: A.cam_box(d, cx, cy, z, col, label, s)
    for (x, y, t, col) in labels: tag(d, x, y, t, col, s)
    by = big.height - 30
    d.rectangle([20, by, 20 + SPAN * s, by + 8], fill=(240, 230, 200))
    d.text((28 + SPAN * s, by - 6), f'10 u = envergadura de la Meganeura ({SPAN} px)', fill=(240, 230, 200), font=A.font(16), stroke_width=3, stroke_fill=(10, 10, 14))
    head = Image.new('RGBA', (big.width, 34 + 22 * len(notes)), (16, 18, 16, 255))
    hd = ImageDraw.Draw(head)
    hd.text((14, 6), title, fill=(255, 240, 200), font=A.font(20))
    for k, n_ in enumerate(notes): hd.text((14, 32 + 22 * k), n_, fill=(200, 210, 200), font=A.font(15))
    full = Image.new('RGBA', (big.width, big.height + head.height), (16, 18, 16, 255))
    full.alpha_composite(head, (0, 0)); full.alpha_composite(big, (0, head.height))
    full.convert('RGB').save(out)
    print('  ', os.path.basename(out), full.size)

# -------------------------------------------------------------------------------------------------
# Láminas
# -------------------------------------------------------------------------------------------------
def lamina_sala(name, fw, fh, s, zooms, seed):
    im, X, Y = scene(fw, fh, seed)
    px, py = X(.5), Y(.55)
    meganeura(im, (px + 4.5) * U, (py - 1.2) * U, math.radians(200), 'C', alt=20)      # a ras de suelo, tras el picado
    sx, sy = X(.22), Y(.62)
    meganeura(im, sx * U, sy * U, math.radians(-30), 'C', only_shadow=True)             # vuela alto: solo sombra
    player(im, px, py)
    cx, cy = fx_c = (X(.5), Y(.5))
    cams = [(px, py, 1, WHITE, 'cámara ×1 (30×16,9 u)'), (px, py, 1.3, YEL, 'cámara ×1,3 (39×22 u)')]
    cams += [(cx, cy, z, c, l) for (z, c, l) in zooms]
    finish(im, f'Meganeura · sala {name}: suelo {fw}×{fh} u (Tierra quemada)',
           [f'Meganeura provisional de {SPAN} px de envergadura (10 u) en picada alta. A la derecha de la rana: a ras de suelo tras el picado',
            '(sombra pequeña debajo). A la izquierda: solo su sombra (vuela alto, invulnerable). Paleta C provisional (ver la lámina de paletas).',
            'Charcas de agua con huevos provisionales, Sigillarias y troncos (blancos del picado), helechos quemados, fuegos y ascuas.'],
           cams, s, os.path.join(HERE, f'vista_previa_escala_sala_{name}.png'),
           labels=[(px + 1.5, py - 6.4, 'a ras de suelo', CYAN), (sx - 3, sy - 4.6, 'solo la sombra (vuela alto)', CYAN)])

def lamina_paletas():
    """Detalle a ×3: rana y Meganeura (a ras de suelo y solo sombra) con las tres paletas, sobre el suelo quemado."""
    cols = []
    for k in 'ABC':
        im, X, Y = scene(16, 9, 21)
        meganeura(im, 11 * U, 8.2 * U, math.radians(-90), k, alt=22)
        put(im, A.frame(FROG, 0, 32), 6, 9.5)
        cols.append(im.crop((2 * U, 3 * U, 18 * U, 12 * U)))
    s = 3
    gap = 12
    w = cols[0].width * s
    full = Image.new('RGBA', (w * 3 + gap * 4, cols[0].height * s + 80), (16, 18, 16, 255))
    d = ImageDraw.Draw(full)
    d.text((14, 6), 'Meganeura · tres paletas sobre la Tierra quemada (detalle ×3, junto a la rana de 32 px)', fill=(255, 240, 200), font=A.font(20))
    for i, (k, c) in enumerate(zip('ABC', cols)):
        x0 = gap + i * (w + gap)
        full.alpha_composite(c.resize((c.width * s, c.height * s), Image.NEAREST), (x0, 70))
        d.text((x0, 42), PALS[k]['nombre'], fill=(200, 230, 210), font=A.font(17))
    out = os.path.join(HERE, 'vista_previa_escala_paletas.png')
    full.convert('RGB').save(out); print('  ', os.path.basename(out), full.size)

def lamina_alturas():
    """Detalle a ×3: la misma Meganeura alta (solo sombra), bajando y a ras de suelo, junto a la rana."""
    im, X, Y = scene(30, 11, 33)
    meganeura(im, 8 * U, 9.5 * U, math.radians(-90), 'C', only_shadow=True)
    meganeura(im, 18 * U, 9.5 * U, math.radians(-90), 'C', alt=40)
    meganeura(im, 27.5 * U, 9.5 * U, math.radians(-90), 'C', alt=18)
    for x in (4.2, 14.2, 23.6): put(im, A.frame(FROG, 0, 32), x, 11.3)
    crop = im.crop((2 * U, 3 * U, 32 * U, 14 * U))
    s = 3
    big = crop.resize((crop.width * s, crop.height * s), Image.NEAREST)
    full = Image.new('RGBA', (big.width, big.height + 50), (16, 18, 16, 255))
    full.alpha_composite(big, (0, 50))
    d = ImageDraw.Draw(full)
    d.text((14, 6), 'Meganeura · alturas: alta (solo la sombra) · bajando · a ras de suelo (detalle ×3, paleta C)', fill=(255, 240, 200), font=A.font(20))
    out = os.path.join(HERE, 'vista_previa_escala_alturas.png')
    full.convert('RGB').save(out); print('  ', os.path.basename(out), full.size)

if __name__ == '__main__':
    lamina_sala('A', 40, 24, 2, [], 5)
    lamina_sala('B', 48, 28, 2, [(1.66, ORA, 'cámara ×1,66 (toda la sala)')], 5)
    lamina_paletas()
    lamina_alturas()
