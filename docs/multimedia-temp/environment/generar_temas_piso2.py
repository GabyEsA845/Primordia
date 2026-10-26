"""Temas de las franjas de profundidad del piso 2: 'Shallows' (Someras) y 'Abyss' (Abismo).
El Arrecife es el tema 'Cambrian' que ya existe (generar_tema_cambrico.py); aquí solo se le añaden obstáculos.

Como generar_tema_cambrico.py: reutiliza generar_escenario.py y ../environment-shapes/generar_formas.py sin tocarlos,
con paletas y decoración nuevas. Además genera:
  - Someras: capas de luz encima del escenario (cáusticas en mosaico y rayos diagonales).
  - Abismo: decoración bioluminiscente (para luces 2D suaves, ver README) y piedra negra con vetas que brillan.
  - Obstáculos de cobertura (bloquean) para las tres franjas, también para el Arrecife.
  - Ocean/Particle_MarineSnow.png: copos de nieve marina para un sistema de partículas (más densos al bajar).

Uso: python generar_temas_piso2.py
Salida: environment/{Shallows,Abyss}/, environment/Cambrian/deco/Obstacle_*, environment/Ocean/,
        environment-shapes/{Shallows,Abyss}/tiles/ y environment/vista_previa_franjas_piso2.png
"""
import os, sys, math, random
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
SHAPES = os.path.normpath(os.path.join(HERE, '..', 'environment-shapes'))

# Someras: arena turquesa clara, roca arenisca azul verdosa con vetas doradas, mucha luz.
SHALLOWS = dict(
    floor=['#1e6670', '#23737a', '#2a8286', '#379694'], fleck=['#4fb0a8', '#86d6c4', '#e4fff2'], algae=['#2a8a5a', '#3aa86a', '#7ad08a'],
    cap=['#0f3440', '#154252', '#1b5062', '#286478'], face=['#1b5062', '#266a7e', '#348498', '#52a2b0', '#90d4d4'],
    vein='#e8c070', lip='#c8f4ee', drip='#a0e6e6', shadow='#164e58')
# Abismo: casi negro azulado; las vetas de la roca y las motas del suelo brillan en cian.
ABYSS = dict(
    floor=['#070b16', '#0a1020', '#0e1628', '#141e34'], fleck=['#1c2c4a', '#2e6a8a', '#7ae8ff'], algae=['#12303a', '#1e4a56', '#4adcc8'],
    cap=['#03050b', '#070a14', '#0b101e', '#121a2e'], face=['#0b101e', '#111a2e', '#182640', '#223456', '#38507e'],
    vein='#36d8d0', lip='#4a6aa0', drip='#6ad0f0', shadow='#020409')

PALETTES = f"""
THEMES['Shallows'] = {SHALLOWS!r}
THEMES['Abyss'] = {ABYSS!r}
"""

DECO = '''
def deco_seagrass(th, k):
    """Pradera de hierba marina: hojas anchas que se mecen (4 cuadros)."""
    im = new(); a = th['algae']
    for s, (bx, hgt) in enumerate(((4, 12), (7, 14), (10, 11), (12, 8))):
        for i in range(hgt):
            sway = round(math.sin(i / 4 + k * 0.8 + s * 1.3) * (i / hgt) * 2.2)
            P(im, bx + sway, 15 - i, a[1] if i < hgt - 3 else a[2])
            P(im, bx + sway + 1, 15 - i, a[0] if i < hgt - 2 else a[1])
    _outline(im); return im

def deco_brachiopod(th):
    """Concha de braquiópodo en abanico, con costillas."""
    im = new(); ramp = ['#8a6a48', '#c09a6a', '#e6c896', '#fff0d0']
    for y in range(T):
        for x in range(T):
            dx, dy = x + .5 - 8, y + .5 - 12
            r = math.hypot(dx, dy); a = math.atan2(dy, dx)
            if r <= 6.5 and -math.pi * 0.95 <= a <= -math.pi * 0.05:
                rib = int((a + math.pi) / (math.pi / 7)) % 2
                P(im, x, y, ramp[2 + rib] if r < 5.5 else ramp[1])
    for x in range(5, 12): P(im, x, 12, ramp[0])
    _outline(im); return im

def deco_ripples(th, k=0):
    """Ondas de arena, sin contorno (marcan el suelo de las Someras)."""
    im = new()
    for row in (3, 8, 13):
        for x in range(T):
            y = row + round(math.sin(x * 2 * math.pi / 16 + row + k) * 1.2)
            P(im, x, y, th['fleck'][1]); P(im, x, y + 1, th['floor'][0])
    return im

def deco_seapen(th, k):
    """Pluma de mar (Thaumaptilon) bioluminiscente: late despacio (4 cuadros). Lleva luz 2D cian."""
    im = new(); lv = (0, 1, 2, 1)[k]
    glow = ['#1e5a6a', '#2e9aa8', '#6ae8f0', '#d0ffff']
    for i in range(10): P(im, 8, 15 - i, '#18303a' if i < 3 else glow[0])
    for i in range(3, 10):
        w = 3.5 - abs(i - 6.5) * 0.45
        for s in (-1, 1):
            for d in range(1, int(w) + 1):
                P(im, 8 + s * d, 15 - i - (d // 2), glow[min(3, 1 + lv if (i + d) % 2 else lv)])
    P(im, 8, 5, glow[min(3, 2 + lv // 2)])
    _outline(im)
    if lv == 2:
        for x, y in ((3, 6), (13, 7), (8, 2)): P(im, x, y, (106, 232, 240, 140))
    return im

def deco_glowmotes(th, k):
    """Motas bioluminiscentes sueltas sobre el suelo, sin contorno (3 cuadros que parpadean)."""
    im = new(); rnd = random.Random(80 + k)
    for i in range(5):
        x, y = rnd.randrange(1, 15), rnd.randrange(1, 15)
        P(im, x, y, '#7ae8ff' if (i + k) % 3 else '#d0ffff')
        if (i + k) % 3 == 0:
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)): P(im, x + dx, y + dy, (122, 232, 255, 90))
    return im

def deco_ctenophore(th, k):
    """Medusa peine diminuta que flota: filas de cilios con destellos de colores que recorren el cuerpo (4 cuadros)."""
    im = new(); bob = (0, -1, -1, 0)[k]
    for y in range(T):
        for x in range(T):
            if ((x + .5 - 8) / 3.6) ** 2 + ((y + .5 - 8 - bob) / 5) ** 2 <= 1: P(im, x, y, (150, 200, 255, 120))
    rainbow = ['#ff6ab0', '#ffd06a', '#6aff9a', '#6ad0ff']
    for row, x in enumerate((6, 8, 10)):
        for i in range(7):
            y = 4 + i + bob
            P(im, x, y, rainbow[(i + k + row) % 4] if (i + k) % 2 == 0 else (200, 230, 255, 200))
    P(im, 7, 4 + bob, '#ffffff')
    return im

def rock_obstacle(th, ramp, big=False, accent=None, seed=0, veins=None):
    n = 32 if big else 16; im = new(n, n); rnd = random.Random(9 + big + seed)
    parts = [(n * 0.5, n * 0.58, n * 0.38)] + ([(n * 0.3, n * 0.65, n * 0.22), (n * 0.7, n * 0.45, n * 0.25)] if big else [(n * 0.32, n * 0.7, n * 0.22)])
    fn = lambda x, y: any(math.hypot(x - cx, (y - cy) * 1.15) <= r for cx, cy, r in parts)
    shade_blob(im, fn, n * 0.5, n * 0.55, n * 0.4, ramp)
    if accent:
        for _ in range(5 if big else 2):
            x, y = rnd.randrange(int(n * 0.25), int(n * 0.75)), rnd.randrange(int(n * 0.25), int(n * 0.5))
            if im.getpixel((x, y))[3]: P(im, x, y, accent)
    if veins:   # vetas que brillan (Abismo)
        for v in range(3 if big else 1):
            x, y = rnd.uniform(n * 0.3, n * 0.7), n * 0.35
            for i in range(int(n * 0.4)):
                x += rnd.choice((-1, 0, 1)) * 0.7; y += 0.8
                if im.getpixel((int(x), int(y)))[3]: P(im, x, y, veins)
    _outline(im); return im

def archaeocyath(th):
    """Arrecife de arqueociatos (Someras): conos con el borde poroso, 32x32. Bloquea."""
    im = new(32, 32); ramp = ['#8a5a2a', '#c08a40', '#e8b860', '#fff0b0']
    cups = ((10, 27, 4.5, 15), (21, 28, 5.5, 18), (15, 29, 3.5, 9), (26, 29, 3, 8))
    for cx, base, w, h in cups:
        top = base - h
        shade_blob(im, lambda x, y, cx=cx, base=base, w=w, top=top: top <= y <= base and abs(x - cx) <= w * (0.45 + 0.55 * (base - y) / (base - top)),
                   cx, (top + base) / 2, max(w, h / 2), ramp)
        for x in range(int(cx - w), int(cx + w) + 1):
            P(im, x, top, '#4a2a10')
            if x % 2 == 0: P(im, x, top - 1, ramp[3])
        for yy in range(int(top) + 2, int(base), 3): P(im, cx + (1 if yy % 2 else -1), yy, ramp[0])
    _outline(im); return im

def coral_big(th):
    """Coral ramificado grande (Arrecife), 32x32. Bloquea."""
    im = new(32, 32); ramp = ['#7a2040', '#b83c5a', '#e8687a', '#ffb0b0']
    def branch(x, y, ang, length, w, depth):
        for i in range(int(length)):
            x += math.cos(ang); y -= math.sin(ang)
            for d in range(-int(w), int(w) + 1):
                P(im, round(x + d * math.sin(ang)), round(y + d * math.cos(ang)), ramp[2 if d < 0 else 1 if d == 0 else 0])
        if depth:
            branch(x, y, ang + 0.55, length * 0.72, max(0.5, w - 0.5), depth - 1)
            branch(x, y, ang - 0.5, length * 0.68, max(0.5, w - 0.5), depth - 1)
        else:
            P(im, round(x), round(y), ramp[3])
    shade_blob(im, lambda x, y: ((x - 16) / 9) ** 2 + ((y - 29) / 2.6) ** 2 <= 1, 16, 29, 9, ['#3a2a3a', '#5a4050', '#7a5a68', '#9a7a88'])
    branch(16, 28, math.pi / 2, 7, 1.5, 3); branch(10, 28, math.pi / 2 + 0.4, 5, 1, 2); branch(22, 28, math.pi / 2 - 0.4, 5, 1, 2)
    _outline(im); return im

def sponge_cluster(th):
    """Grupo de esponjas de barril (Arrecife), 32x32. Bloquea."""
    im = new(32, 32); ramp = ['#7a3c18', '#b86a28', '#e89a40', '#ffd088']
    for cx, w, h in ((9, 4, 16), (18, 5, 22), (26, 3.5, 12)):
        top = 30 - h
        shade_blob(im, lambda x, y, cx=cx, w=w, top=top: top <= y <= 30 and abs(x - cx) <= w + (y - top) * 0.04, cx, (top + 30) / 2, max(w, h / 2), ramp)
        for x in range(int(cx - w + 1), int(cx + w)): P(im, x, top, '#3a1a0c'); P(im, x, top + 1, ramp[0])
        for k in range(4): P(im, cx + (-1, 1, -2, 1)[k], top + 4 + k * 3, ramp[0])
    _outline(im); return im

def spire(th):
    """Aguja de roca negra del Abismo con líquenes que brillan, 32x32. Bloquea. Lleva luz 2D cian tenue."""
    im = new(32, 32); ramp = ['#0e1628', '#1c2c48', '#2e4468', '#4c6894']
    shade_blob(im, lambda x, y: y <= 30 and abs(x - 16) <= 2 + (y - 2) * 0.33 + math.sin(y * 0.8) * 0.8, 16, 18, 12, ramp)
    shade_blob(im, lambda x, y: ((x - 16) / 11) ** 2 + ((y - 29) / 3) ** 2 <= 1, 16, 29, 11, ramp)
    for x, y in ((14, 10), (17, 14), (13, 19), (19, 21), (11, 26), (21, 27), (16, 6)):
        P(im, x, y, '#7ae8ff'); P(im, x + 1, y, '#36d8d0')
    _outline(im); return im

'''

DECO_BRANCH = (
    "    if theme == 'Shallows':\n"
    "        deco['Deco_Bubbles'] = [deco_bubbles(th, k) for k in range(2)]\n"
    "        deco['Deco_Seagrass'] = [deco_seagrass(th, k) for k in range(4)]\n"
    "        deco['Deco_Brachiopod'] = [deco_brachiopod(th)]\n"
    "        deco['Deco_Ripples'] = [deco_ripples(th)]\n"
    "        deco['Obstacle_Rock'] = [rock_obstacle(th, ['#3a5a5a', '#5a8482', '#86b0a8', '#bcdcd0'], accent='#e8c070')]\n"
    "        deco['Obstacle_RockBig'] = [rock_obstacle(th, ['#3a5a5a', '#5a8482', '#86b0a8', '#bcdcd0'], True, accent='#7ad08a')]\n"
    "        deco['Obstacle_Archaeocyath'] = [archaeocyath(th)]\n"
    "    elif theme == 'Abyss':\n"
    "        deco['Deco_SeaPen'] = [deco_seapen(th, k) for k in range(4)]\n"
    "        deco['Deco_GlowMotes'] = [deco_glowmotes(th, k) for k in range(3)]\n"
    "        deco['Deco_Ctenophore'] = [deco_ctenophore(th, k) for k in range(4)]\n"
    "        deco['Deco_Bubbles'] = [deco_bubbles(th, k) for k in range(2)]\n"
    "        deco['Obstacle_Rock'] = [rock_obstacle(th, ['#0e1628', '#1c2c48', '#2e4468', '#4c6894'], veins='#36d8d0')]\n"
    "        deco['Obstacle_RockBig'] = [rock_obstacle(th, ['#0e1628', '#1c2c48', '#2e4468', '#4c6894'], True, veins='#36d8d0')]\n"
    "        deco['Obstacle_Spire'] = [spire(th)]\n"
    "    elif theme == 'Soup':\n")


def run_scenery():
    path = os.path.join(HERE, 'generar_escenario.py')
    with open(path, encoding='utf-8') as f:
        src = f.read().replace('\r\n', '\n')
    src = src.replace("def new(w=T, h=T):", PALETTES + "\ndef new(w=T, h=T):", 1)
    src = src.replace("# ---------------- generar ----------------", DECO + "# ---------------- generar ----------------", 1)
    src = src.replace("    if theme == 'Soup':\n", DECO_BRANCH, 1)
    src = src.replace("for theme in ('Soup', 'Boss'):", "for theme in ('Shallows', 'Abyss'):")
    assert src.count("for theme in ('Shallows', 'Abyss'):") == 2 and 'deco_seapen(th, k)' in src, \
        'generar_escenario.py cambió: revisa este script'
    sys.argv = [path, HERE]
    g = {'__name__': '__main__', '__file__': path}
    exec(compile(src, path, 'exec'), g)
    return g


def run_shapes():
    path = os.path.join(SHAPES, 'generar_formas.py')
    with open(path, encoding='utf-8') as f:
        src = f.read().replace('\r\n', '\n')
    src = src.replace("VARIANTS = ['Soup', 'Boss']", "VARIANTS = ['Shallows', 'Abyss']", 1)
    sys.argv = [path, HERE, SHAPES]
    exec(compile(src, path, 'exec'), {'__name__': '__main__', '__file__': path})


# ---------------- capas de luz de las Someras ----------------
def caustics(k):
    """64x64 en mosaico (periodo 64), 4 cuadros en bucle: red de luz ondulante y suave sobre la arena. Sin contorno."""
    N = 64; im = Image.new('RGBA', (N, N), (0, 0, 0, 0)); tau = 2 * math.pi
    ph = k * tau / 4
    for y in range(N):
        for x in range(N):
            u, v = x * tau / N, y * tau / N
            n = (math.sin(2 * u + 1.3 * math.sin(v + ph) + math.sin(3 * v - ph) * 0.4)
                 + math.sin(2 * v + 1.3 * math.sin(u * 2 - ph) + ph)
                 + 0.7 * math.sin(3 * u - v + ph) + 0.5 * math.sin(u + 3 * v - 2 * ph))
            if abs(n) < 0.14: im.putpixel((x, y), (220, 255, 245, 75))
            elif abs(n) < 0.26: im.putpixel((x, y), (190, 245, 235, 28))
    return im


def light_ray(k):
    """48x128, 4 cuadros en bucle: tres rayos diagonales translúcidos que se mecen y se desvanecen hacia abajo."""
    im = Image.new('RGBA', (48, 128), (0, 0, 0, 0))
    sway = math.sin(k * math.pi / 2) * 1.5
    for x0, w, a0 in ((6, 5, 70), (20, 8, 90), (36, 4, 60)):
        for y in range(128):
            fade = max(0, 1 - y / 128) ** 0.8
            cx = x0 + sway + y * 0.18
            for x in range(48):
                d = abs(x + .5 - cx)
                if d <= w / 2:
                    a = int(a0 * fade * (1 - (d / (w / 2)) ** 2 * 0.6) * (0.85 + 0.15 * math.sin(k * 1.7 + x0)))
                    if a > 4: im.putpixel((x, y), (230, 255, 240, a))
    return im


def marine_snow():
    """8x8, 4 variantes de copo (no es una animación): para un sistema de partículas que cae despacio."""
    out = []
    for k, pts in enumerate(([(3, 3)], [(3, 3), (4, 3)], [(3, 3), (4, 4)], [(3, 2), (3, 3), (4, 3), (3, 4)])):
        im = Image.new('RGBA', (8, 8), (0, 0, 0, 0))
        for i, (x, y) in enumerate(pts): im.putpixel((x, y), (230, 240, 250, 220 if i == 0 else 150))
        out.append(im)
    return out


def sheet(frames):
    w, h = frames[0].size
    sh = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames): sh.paste(f, (i * w, 0))
    return sh


def sample_band(g, theme, tiles, deco, overlays=None, snow=0, seed=1):
    """Sala de muestra 22x15 con decoración y obstáculos de la franja, para revisar el conjunto."""
    T, W, H = 16, 22, 15
    room = Image.new('RGBA', (W * T, H * T), (0, 0, 0, 255)); rnd = random.Random(seed)
    for y in range(H):
        for x in range(W):
            if y == 0: t = 'Wall_Cap'
            elif y == 1 and 0 < x < W - 1: t = f'Wall_FaceUpper_{x % 2}'
            elif y == 2 and 0 < x < W - 1: t = f'Wall_FaceLower_{(x // 3) % 2}'
            elif y == H - 1: t = 'Wall_CornerBL' if x == 0 else 'Wall_CornerBR' if x == W - 1 else 'Wall_EdgeBottom'
            elif x == 0: t = 'Wall_EdgeLeft'
            elif x == W - 1: t = 'Wall_EdgeRight'
            elif y == 3: t = f'Floor_ShadowTop_{x % 2}'
            else: t = f'Floor_{rnd.choice([0] * 10 + [2, 3, 4, 5] + [1])}'
            room.paste(tiles[t], (x * T, y * T))
    if overlays:
        cau = overlays['Overlay_Caustics'][0]
        for y in range(3 * T, (H - 1) * T, 64):
            for x in range(T, (W - 1) * T, 64):
                room.alpha_composite(cau.crop((0, 0, min(64, (W - 1) * T - x), min(64, (H - 1) * T - y))), (x, y))
    decos = [k for k in deco if k.startswith('Deco_')]
    obs = [k for k in deco if k.startswith('Obstacle_')]
    for i, (x, y) in enumerate(((3, 5), (17, 4), (6, 11), (15, 12), (19, 9), (9, 5), (12, 10), (4, 8), (18, 12))):
        room.alpha_composite(deco[decos[i % len(decos)]][0], (x * T, y * T))
    for i, (x, y) in enumerate(((7, 7), (13, 5), (16, 9), (3, 11))):
        o = deco[obs[i % len(obs)]][0]
        room.alpha_composite(o, (x * T, y * T))
    if overlays:
        ray = overlays['Overlay_LightRay'][0]
        for x in (2 * T, 9 * T, 15 * T): room.alpha_composite(ray, (x, 0))
    if snow:
        flakes = marine_snow(); rnd = random.Random(seed + 9)
        for _ in range(snow): room.alpha_composite(flakes[rnd.randrange(4)], (rnd.randrange(W * T - 8), rnd.randrange(H * T - 8)))
    return room


if __name__ == '__main__':
    g = run_scenery()
    run_shapes()
    # capas de luz de las Someras
    overlays = {'Overlay_Caustics': [caustics(k) for k in range(4)], 'Overlay_LightRay': [light_ray(k) for k in range(4)]}
    for n, fr in overlays.items(): sheet(fr).save(os.path.join(HERE, 'Shallows', 'deco', f'{n}.png'))
    # obstáculos del Arrecife (tema Cambrian ya existente)
    cam = g['THEMES']['Cambrian'] if 'Cambrian' in g['THEMES'] else None
    obs_cam = {'Obstacle_Rock': [g['rock_obstacle'](cam, ['#1c2c50', '#28406c', '#365890', '#6a90c0'], accent='#d0705a')],
               'Obstacle_RockBig': [g['rock_obstacle'](cam, ['#1c2c50', '#28406c', '#365890', '#6a90c0'], True, accent='#4aa080')],
               'Obstacle_CoralBig': [g['coral_big'](cam)], 'Obstacle_SpongeCluster': [g['sponge_cluster'](cam)]}
    for n, fr in obs_cam.items(): sheet(fr).save(os.path.join(HERE, 'Cambrian', 'deco', f'{n}.png'))
    os.makedirs(os.path.join(HERE, 'Ocean'), exist_ok=True)
    sheet(marine_snow()).save(os.path.join(HERE, 'Ocean', 'Particle_MarineSnow.png'))

    # vista previa de las tres franjas, de arriba (Someras) abajo (Abismo)
    def load_theme(theme):
        d = os.path.join(HERE, theme)
        tiles = {f[:-4]: Image.open(os.path.join(d, 'tiles', f)).convert('RGBA') for f in os.listdir(os.path.join(d, 'tiles'))}
        deco = {f[:-4]: [Image.open(os.path.join(d, 'deco', f)).convert('RGBA')] for f in os.listdir(os.path.join(d, 'deco'))}
        for k, v in deco.items():   # primer cuadro de cada hoja
            im = v[0]; w = 32 if k.startswith(('Obstacle_RockBig', 'Obstacle_Arch', 'Obstacle_Coral', 'Obstacle_Sponge', 'Obstacle_Spire')) else 16
            if k.startswith('Overlay_'): w = 64 if 'Caustics' in k else 48
            deco[k] = [im.crop((0, 0, w, im.height))]
        return tiles, deco
    rooms = []
    for theme, snow in (('Shallows', 12), ('Cambrian', 30), ('Abyss', 70)):
        tiles, deco = load_theme(theme)
        ov = {k: deco.pop(k) for k in list(deco) if k.startswith('Overlay_')}
        rooms.append(sample_band(g, theme, tiles, deco, ov or None, snow))
    W, H = rooms[0].size
    pv = Image.new('RGBA', (W, H * 3 + 8), (0, 0, 0, 255))
    for i, r in enumerate(rooms): pv.paste(r, (0, i * (H + 4)))
    pv.resize((pv.width * 2, pv.height * 2), Image.NEAREST).save(os.path.join(HERE, 'vista_previa_franjas_piso2.png'))
    for theme, r in zip(('Shallows', 'Cambrian', 'Abyss'), rooms):
        r.resize((W * 3, H * 3), Image.NEAREST).save(os.path.join(HERE, theme, f'Sala_franja_x3.png'))
    print('Temas Shallows y Abyss listos.')
