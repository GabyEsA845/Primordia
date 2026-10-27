"""Documento interactivo del piso 2: mete el arte (base64) en template.html y escribe piso2.html.
Mismo método que ../documento-linajes/build.py (piso 1)."""
import base64, io, json, os, random
from PIL import Image

ROOT = r'D:\Proyectos\Primordia'
TEMP = ROOT + r'\docs\multimedia-temp'
ART = ROOT + r'\Assets\_Primordia\Art'
HERE = os.path.dirname(os.path.abspath(__file__))


def uri(path):
    with open(path, 'rb') as f:
        return 'data:image/png;base64,' + base64.b64encode(f.read()).decode()


def png_uri(im):
    buf = io.BytesIO(); im.save(buf, format='PNG')
    return 'data:image/png;base64,' + base64.b64encode(buf.getvalue()).decode()


assets = {}


def sheet(key, path, cell=None):
    """Hoja horizontal: guarda la imagen, el número de cuadros (_f) y el lado del cuadro (_s)."""
    im = Image.open(path)
    s = cell or im.height
    assets[key] = uri(path); assets[key + '_f'] = im.width // s; assets[key + '_s'] = s


# Iconos de las 9 mutaciones del piso 2 y de las ranuras
for name in ['Cnidocytes', 'JawPlates', 'ArmCrown', 'TailPropulsor', 'HallucigeniaSpines', 'TrilobitePlates',
             'WiwaxiaSclerites', 'BroodSac', 'CompoundEyes']:
    assets['icon_' + name] = uri(rf'{TEMP}\floor2-bodies\Icons\Icon_{name}_32.png')
for key, slot in [('slot_mouth', 'Mouth'), ('slot_limbs', 'Limbs'), ('slot_skin', 'Skin'), ('slot_organ', 'Organ')]:
    assets[key] = uri(rf'{ART}\UI\Icon_Slot_{slot}.png')

# Cuerpos del jugador (uno por familia de Boca)
for body in ('Placoderm', 'Jellyfish', 'Crinoid'):
    sheet(f'body_{body}_idle', rf'{TEMP}\floor2-bodies\{body}\Player_{body}_Idle_down.png')
    sheet(f'body_{body}_atk', rf'{TEMP}\floor2-bodies\{body}\Player_{body}_Attack_side.png')

# Evolución de los 8 linajes (64×64, 12 cuadros) y su silueta (cuadro 7)
for lin in ('Placoderm', 'Opabinia', 'Burgessomedusa', 'Anemone', 'Crinoid', 'Wiwaxia', 'Trilobite', 'Hallucigenia'):
    p = rf'{TEMP}\floor2-lineages\Evolution\Evolution_{lin}.png'
    sheet('evo_' + lin, p)
    im = Image.open(p).convert('RGBA'); assets['sil_' + lin] = png_uri(im.crop((7 * 64, 0, 8 * 64, 64)))

# Enemigos
EN = rf'{TEMP}\floor2-enemies'
for rel in ['Trilobite/Trilobite_Move', 'Trilobite/Trilobite_Warn', 'Trilobite/Trilobite_Charge', 'Trilobite/Trilobite_Stunned',
            'Trilobite/Trilobite_Death', 'BlueJelly/BlueJelly_Move', 'BlueJelly/BlueJelly_Warn', 'BlueJelly/BlueJelly_Shoot',
            'BlueJelly/BlueJelly_Death', 'Ottoia/Ottoia_Burrowed', 'Ottoia/Ottoia_Warn', 'Ottoia/Ottoia_Attack', 'Ottoia/Ottoia_Sink',
            'Ottoia/Ottoia_Death', 'Vauxia/Vauxia_Idle', 'Vauxia/Vauxia_Heal', 'Vauxia/Vauxia_Death', 'Pikaia/Pikaia_Swim',
            'Pikaia/Pikaia_Dash', 'Pikaia/Pikaia_Death', 'Effects/Proj_BlueJelly', 'Effects/Fx_Vauxia_Shield', 'Effects/Fx_Vauxia_HealMotes', 'Effects/Fx_Vauxia_Thread']:
    key = 'en_' + rel.split('/')[-1]
    sheet(key, os.path.join(EN, rel + '.png'), 8 if 'Proj_BlueJelly' in rel else None)

# Jefes
BO = rf'{TEMP}\floor2-bosses'
for rel in ['Anomalocaris_Swim', 'Anomalocaris_SwimFast', 'Anomalocaris_ClawWarn', 'Anomalocaris_ClawStrike', 'Anomalocaris_Vulnerable',
            'Anomalocaris_Charge', 'Anomalocaris_EyesDark', 'Anomalocaris_Death']:
    sheet('bo_' + rel, rf'{BO}\Anomalocaris\{rel}.png')
for rel in ['Eurypterid_Walk', 'Eurypterid_PincerStrike', 'Eurypterid_SpineWarn', 'Eurypterid_SpineShoot', 'Eurypterid_Molt',
            'Eurypterid_Exuvia', 'Eurypterid_Walk_Soft', 'Eurypterid_TailWarn', 'Eurypterid_TailSweep', 'Eurypterid_DoublePincer',
            'Eurypterid_Death', 'Eurypterid_Downed', 'Eurypterid_Downed_Idle', 'Eurypterid_Rise', 'Eurypterid_Fury']:
    sheet('bo_' + rel, rf'{BO}\Eurypterid\{rel}.png')
# Sala del Euriptérido: el derrumbe de la cueva
for rel in ['Fx_RockCollapse', 'Fx_RockFallShadow', 'Proj_FallingRock', 'Fx_FloorCrack']:
    sheet('bo_' + rel, rf'{BO}\EurypteridRoom\{rel}.png')
sheet('bo_Obstacle_RockCracked', rf'{BO}\EurypteridRoom\Obstacle_RockCracked.png')
for rel, cell in [('Fx_Anomalocaris_Bubbles', None), ('Fx_Anomalocaris_ClawArea', None), ('Proj_EurypteridSpine', 8),
                  ('Fx_Eurypterid_TailSweep', None), ('Proj_EurypteridVenom', None)]:
    sheet('bo_' + rel, rf'{BO}\Effects\{rel}.png', cell)
# La púa es 16×8: la paso a un lienzo cuadrado de 16×16 por cuadro para animarla igual que el resto
sp = Image.open(rf'{BO}\Effects\Proj_EurypteridSpine.png').convert('RGBA')
sq = Image.new('RGBA', (32, 16), (0, 0, 0, 0))
for i in range(2): sq.paste(sp.crop((i * 16, 0, i * 16 + 16, 8)), (i * 16, 4))
assets['bo_Proj_EurypteridSpine'] = png_uri(sq); assets['bo_Proj_EurypteridSpine_f'] = 2; assets['bo_Proj_EurypteridSpine_s'] = 16
# Barra de vida del Anomalocaris en sus 4 estados (vista previa del generador, ya a ×5)
assets['bar_Anomalocaris'] = uri(rf'{BO}\vista_previa_barra_anomalocaris.png')
# Barra del Euriptérido: acorazado, muda, blando, último aliento y el salto del caparazón (ya a ×5)
assets['bar_Eurypterid'] = uri(rf'{BO}\vista_previa_barra_euripterido.png')
ji = Image.open(rf'{EN}\Effects\Proj_BlueJelly.png').convert('RGBA')
assets['en_Proj_BlueJelly'] = png_uri(ji); assets['en_Proj_BlueJelly_f'] = 2; assets['en_Proj_BlueJelly_s'] = 8

# Aspecto de la página: piezas del juego
for key, rel in [('ui_card', 'UI_Card_Normal'), ('ui_panel', 'UI_Panel'), ('ui_button', 'UI_Button_Normal'),
                 ('ui_button_hi', 'UI_Button_Highlighted')]:
    assets[key] = uri(f'{ART}/UI/{rel}.png')
assets['logo'] = uri(f'{ART}/UI/Menus/Menu_Logo.png')
far = Image.open(f'{ART}/UI/Menus/Menu_Bg_Far.png').convert('RGBA')
far.alpha_composite(Image.open(f'{ART}/UI/Menus/Menu_Bg_Near.png').convert('RGBA'))
# el fondo del menú, teñido de azul océano
px = far.load()
for y in range(far.height):
    for x in range(far.width):
        r, g, b, a = px[x, y]
        px[x, y] = (int(r * 0.45), int(g * 0.7 + b * 0.1), int(min(255, b * 0.9 + g * 0.35 + 20)), a)
assets['menu_bg'] = png_uri(far)


def tiles(theme):
    folder = rf'{TEMP}\environment\{theme}\tiles'
    return {os.path.splitext(f)[0]: Image.open(os.path.join(folder, f)).convert('RGBA') for f in os.listdir(folder)}


def floor_mosaic(theme, n=6, seed=1):
    t = tiles(theme); rnd = random.Random(seed)
    names = ['Floor_0'] * 9 + [k for k in t if k.startswith('Floor_') and k[6:].isdigit() and k != 'Floor_0']
    im = Image.new('RGBA', (16 * n, 16 * n))
    for y in range(n):
        for x in range(n): im.paste(t[rnd.choice(names)], (x * 16, y * 16))
    return png_uri(im)


assets['floor_cambrian'] = floor_mosaic('Cambrian', 8, 3)
assets['floor_shallows'] = floor_mosaic('Shallows', 6, 4)
assets['floor_abyss'] = floor_mosaic('Abyss', 6, 5)
t = tiles('Cambrian')
wall = Image.new('RGBA', (64, 64))
for x in range(4):
    wall.paste(t['Wall_Cap'], (x * 16, 0)); wall.paste(t[f'Wall_FaceUpper_{x % 2}'], (x * 16, 16))
    wall.paste(t[f'Wall_FaceLower_{(x // 2) % 2}'], (x * 16, 32)); wall.paste(t[f'Floor_ShadowTop_{x % 2}'], (x * 16, 48))
assets['wall'] = png_uri(wall)

# El océano: una sala de cada franja (con decoración y obstáculos) y las puertas de losas
big = Image.open(rf'{TEMP}\environment\vista_previa_franjas_piso2.png').convert('RGBA')
h = (big.height // 2 - 8) // 3
for i, theme in enumerate(('Shallows', 'Cambrian', 'Abyss')):
    room = big.resize((big.width // 2, big.height // 2), Image.NEAREST).crop((0, i * (h + 4), big.width // 2, i * (h + 4) + h))
    assets['room_' + theme] = png_uri(room)
for theme in ('Shallows', 'Cambrian', 'Abyss'):
    base = Image.open(rf'{TEMP}\environment\{theme}\Sala_muestra_abierta.png').convert('RGBA').crop((7 * 16, 0, 15 * 16, 4 * 16))
    frames = []
    for kind, n in (('Close', 7), ('Open', 6)):
        sh = Image.open(rf'{TEMP}\environment\{theme}\Door_Top_{kind}.png').convert('RGBA')
        for k in range(n):
            f = base.copy(); f.alpha_composite(sh.crop((k * 32, 0, k * 32 + 32, 32)), (3 * 16, 16)); frames.append(f)
            if (kind, k) in (('Close', 6), ('Open', 5)):
                for _ in range(5): frames.append(f)          # se queda un rato cerrada / abierta
    strip = Image.new('RGBA', (128 * len(frames), 64))
    for k, f in enumerate(frames): strip.paste(f, (k * 128, 0))
    assets['door_' + theme] = png_uri(strip); assets['door_' + theme + '_f'] = len(frames); assets['door_' + theme + '_s'] = 64; assets['door_' + theme + '_w'] = 128

# Presentación de los jefes: pose, nombre rotulado y retrato (../boss-intros)
BI = os.path.join(TEMP, 'boss-intros')
for key in ('Anomalocaris', 'Eurypterid'):
    sheet(f'intro_{key}_pose', rf'{BI}\Poses\{key}_Intro.png')
    assets[f'intro_{key}_portrait'] = uri(rf'{BI}\Portraits\Portrait_{key}.png')
    t = rf'{BI}\Titles\Title_{key}.png'
    w, h = Image.open(t).size
    assets[f'intro_{key}_title'] = uri(t); assets[f'intro_{key}_title_w'] = w; assets[f'intro_{key}_title_h'] = h

# Mutaciones especiales de las almejas (../clam-mutations): iconos con la insignia, efectos sobre un enemigo o el jugador,
# la Cutícula sobre los 3 cuerpos, la copia de la Yema (la misma rampa que el shader del juego) y los corazones de Inestable
CM = rf'{TEMP}\clam-mutations'


def frames(path, cell=None):
    im = Image.open(path).convert('RGBA'); c = cell or im.height
    return [im.crop((i * c, 0, i * c + c, im.height)) for i in range(im.width // c)]


def strip(key, fs):
    w, h = fs[0].size
    out = Image.new('RGBA', (w * len(fs), h))
    for i, f in enumerate(fs): out.paste(f, (i * w, 0))
    assets[key] = png_uri(out); assets[key + '_f'] = len(fs); assets[key + '_s'] = h
    if w != h: assets[key + '_w'] = w


def centered(size, *layers):
    out = Image.new('RGBA', (size, size))
    for im in layers:
        if im is not None: out.alpha_composite(im, ((size - im.width) // 2, (size - im.height) // 2))
    return out


badge = Image.open(rf'{CM}\UI\UI_Special_Badge.png').convert('RGBA')
for name in ('LeanchoiliaFlagella', 'MarrellaCuticle', 'ClonalBud'):
    ic = Image.open(rf'{CM}\Icons\Icon_{name}_32.png').convert('RGBA')
    out = Image.new('RGBA', (36, 36)); out.alpha_composite(ic, (0, 4)); out.alpha_composite(badge, (27, 0))
    assets['sp_icon_' + name] = png_uri(out)

trilo = frames(rf'{EN}\Trilobite\Trilobite_Move.png')
jelly = frames(rf'{TEMP}\floor2-bodies\Jellyfish\Player_Jellyfish_Idle_down.png')
lash = frames(rf'{CM}\Effects\Fx_LeanchoiliaFlagella_Lash.png')
strip('sp_lash', [centered(48, trilo[i % len(trilo)], f) for i, f in enumerate(lash)])
ready = frames(rf'{CM}\Effects\Fx_LeanchoiliaFlagella_Ready.png')
strip('sp_ready', [centered(32, jelly[i % len(jelly)], f) for i, f in enumerate(ready + [None] * 3)])
reflect = frames(rf'{CM}\Effects\Fx_MarrellaCuticle_Reflect.png')
strip('sp_reflect', [centered(32, jelly[i % len(jelly)], f) for i, f in enumerate(reflect + [None] * 2)])
stun = frames(rf'{CM}\Effects\Fx_MarrellaCuticle_Stun.png')
strip('sp_stun', [centered(32, trilo[i % len(trilo)], f) for i, f in enumerate(stun + [None] * 3)])
strip('sp_proj', [centered(16, f) for f in frames(rf'{CM}\Effects\Proj_MarrellaCuticle_Reflected.png')])
for body in ('Placoderm', 'Jellyfish', 'Crinoid'):
    base = frames(rf'{TEMP}\floor2-bodies\{body}\Player_{body}_Idle_down.png')
    layer = frames(rf'{CM}\Skin\Skin_MarrellaCuticle_{body}_Idle_down.png')
    strip('sp_cuticle_' + body, [centered(48, base[i % len(base)], f) for i, f in enumerate(layer)])
for kind in ('Sprout', 'Dissolve'):
    strip('sp_' + kind.lower(), frames(rf'{CM}\Effects\Fx_ClonalBud_{kind}.png'))

RAMP = [(0x1e, 0x6a, 0x6a), (0x3a, 0xa8, 0xa0), (0x7e, 0xe8, 0xcc), (0xd8, 0xff, 0xf0)]
OUTLINE = (0xb8, 0xff, 0xf0)


def clone(im):
    """Como Primordia/ClonalBud: luminancia a la rampa de 4 tonos, 60 % de opacidad y contorno claro."""
    im = centered(im.width + 4, im); src = im.load(); out = Image.new('RGBA', im.size); dst = out.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = src[x, y]
            if a >= 128:
                l = (0.299 * r + 0.587 * g + 0.114 * b) / 255
                dst[x, y] = RAMP[min(3, int(l * 4))] + (153,)
            elif any(0 <= x + dx < im.width and 0 <= y + dy < im.height and src[x + dx, y + dy][3] >= 128
                     for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                dst[x, y] = OUTLINE + (153,)
    return out


strip('sp_body', [centered(28, f) for f in jelly])
strip('sp_clone', [clone(f) for f in jelly])

UI = rf'{ART}\UI'
full = Image.open(rf'{UI}\UI_Heart_Full.png').convert('RGBA')


def hearts(*overlays):
    out = Image.new('RGBA', (52, 16))
    for i in range(3):
        h = full.copy()
        for name in overlays: h.alpha_composite(Image.open(rf'{name}_{i}.png').convert('RGBA'))
        out.alpha_composite(h, (i * 18, 0))
    return png_uri(out)


assets['sp_hearts'] = hearts()
assets['sp_hearts_unstable'] = hearts(rf'{CM}\UI\UI_Heart_UnstableInlay')
assets['sp_hearts_purple'] = hearts(rf'{UI}\UI_Heart_PurpleInlay', rf'{CM}\UI\UI_Heart_UnstableCracks')
strip('sp_break', frames(rf'{CM}\UI\Fx_Heart_UnstableBreak.png') + [Image.new('RGBA', (24, 24))] * 3)
clam = rf'{TEMP}\floor2-rooms\Pieces'
strip('sp_clam', frames(rf'{clam}\Clam_Big_Idle.png', 32) + frames(rf'{clam}\Clam_Big_Open.png', 32))

# Sala de cambio (../exchange-room): la cápsula en reposo, gastada y la metamorfosis de medusa a pez dentro del vidrio.
EX = rf'{TEMP}\exchange-room'
cap_idle = frames(rf'{EX}\Capsule_Idle.png', 40)
cap_glow = frames(rf'{EX}\Capsule_Glow.png', 40)
cap_flash = frames(rf'{EX}\Capsule_Flash.png', 40)
strip('ex_idle', cap_idle)
strip('ex_used', frames(rf'{EX}\Capsule_Used.png', 40))


def tinted(im, rgb):
    out = im.copy(); px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if a: px[x, y] = (r * rgb[0] // 255, g * rgb[1] // 255, b * rgb[2] // 255, a)
    return out


FISH = (255, 158, 77)
morph = frames(rf'{EX}\Morph_Jellyfish_Placoderm.png', 32)
reveal = frames(rf'{TEMP}\metamorphosis\Reveal_Placoderm.png', 32)
seq = []
for k in range(4):   # el líquido empieza a brillar
    f = cap_idle[k % 6].copy(); g = tinted(cap_glow[k % 4], FISH)
    g.putalpha(g.getchannel('A').point(lambda a, k=k: a * (k + 1) // 4)); f.alpha_composite(g); seq.append(f)
for k in range(12):  # la silueta cambia dentro
    f = cap_idle[k % 6].copy(); f.alpha_composite(tinted(cap_glow[k % 4], FISH)); f.alpha_composite(morph[k], (4, 9)); seq.append(f)
for k in range(6):   # se revela con colores
    f = cap_idle[k % 6].copy(); f.alpha_composite(tinted(cap_glow[k % 4], FISH)); f.alpha_composite(reveal[k], (4, 9)); seq.append(f)
for fl in cap_flash:  # destello
    f = cap_idle[0].copy(); f.alpha_composite(tinted(fl, FISH)); seq.append(f)
seq += [cap_idle[k % 6] for k in range(6)]
strip('ex_morph', seq)

# Ataques de las 3 Bocas (../floor2-bodies/Effects) y su metamorfosis (../metamorphosis)
FX = rf'{TEMP}\floor2-bodies\Effects'
harp = frames(rf'{FX}\Proj_Cnidocyte.png', 16)
strip('mo_harpoon', [centered(16, f) for f in harp])
strip('mo_muzzle', frames(rf'{FX}\Fx_Cnidocyte_Muzzle.png', 16) + [Image.new('RGBA', (16, 16))] * 2)
strip('mo_bite', frames(rf'{FX}\Fx_JawPlates_Bite.png', 32) + [Image.new('RGBA', (32, 32))] * 2)
strip('mo_guillotine', frames(rf'{FX}\Fx_JawPlates_Guillotine.png', 56) + [Image.new('RGBA', (56, 56))] * 2)
strip('mo_shatter', frames(rf'{FX}\Status_Shatter.png', 12))
strip('mo_whirl', frames(rf'{FX}\Fx_ArmCrown_Whirl.png', 48))
strip('mo_tide', frames(rf'{FX}\Fx_ArmCrown_Tide.png', 64) + [Image.new('RGBA', (64, 64))] * 2)
MM = rf'{TEMP}\metamorphosis'
for body in ('Placoderm', 'Jellyfish', 'Crinoid'):
    seq = frames(rf'{MM}\Morph_{body}.png', 32) + frames(rf'{MM}\Reveal_{body}.png', 32)
    strip('mo_morph_' + body, seq + [seq[-1]] * 6)

# Mutaciones de Extremidades, Piel y Órganos (../floor2-mutations): capa sobre cada cuerpo y efectos
MU = rf'{TEMP}\floor2-mutations'
for layer, behind in (('TailPropulsor', True), ('HallucigeniaSpines', True), ('TrilobitePlates', False), ('WiwaxiaSclerites', False)):
    folder = 'Limbs' if behind else 'Skin'
    for body in ('Placoderm', 'Jellyfish', 'Crinoid'):
        bf = frames(rf'{TEMP}\floor2-bodies\{body}\Player_{body}_Idle_side.png')
        lf = frames(rf'{MU}\{folder}\{folder}_{layer}_{body}_Idle_side.png')
        seq = [centered(48, lf[i], bf[i]) if behind else centered(48, bf[i], lf[i]) for i in range(min(len(bf), len(lf)))]
        strip(f'ml_{layer}_{body}', seq)
for key, rel, cell in (('mf_bubble', r'Effects\Fx_TailPropulsor_Bubble', None), ('mf_bubbleBig', r'Effects\Fx_TailPropulsor_BubbleBig', None),
                       ('mf_pop', r'Effects\Fx_TailPropulsor_Pop', None), ('mf_spine', r'Effects\Fx_HallucigeniaSpine_Ground', None),
                       ('mf_bounce', r'Effects\Fx_TrilobitePlates_Bounce', None), ('mf_roll', r'Effects\Fx_TrilobitePlates_Roll', None),
                       ('mf_release', r'Effects\Fx_WiwaxiaSclerites_Release', None), ('mf_block', r'Effects\Fx_WiwaxiaSclerite_Block', None),
                       ('mf_sac', r'Effects\Fx_BroodSac_Open', None), ('mf_split', r'Effects\Fx_BroodSac_Split', None),
                       ('mf_larva', r'Larva\Larva_Swim', None), ('mf_larvaBite', r'Larva\Larva_Bite', None),
                       ('mf_eyes', r'Effects\Fx_CompoundEyes_Flash', None),
                       ('carr_remains', r'Effects\Fx_Carrion_Remains', None), ('carr_double', r'Effects\Fx_Carrion_RemainsDouble', None),
                       ('carr_eat', r'Effects\Fx_Carrion_Eat', None)):
    strip(key, frames(rf'{MU}\{rel}.png', cell))
strip('carr_mark', [centered(12, f) for f in frames(rf'{MU}\Effects\Status_Carrion_3.png', 12)])

with open(os.path.join(HERE, 'template.html'), encoding='utf-8') as f:
    page = f.read()
page = page.replace('/*ASSETS*/{}', json.dumps(assets))
with open(os.path.join(HERE, 'piso2.html'), 'w', encoding='utf-8') as f:
    f.write(page)
print('piso2.html', len(page) // 1024, 'KB')
