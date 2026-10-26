import base64, json, os
from PIL import Image

ROOT = r'D:\Proyectos\Primordia'
TEMP = ROOT + r'\docs\multimedia-temp'
ART = ROOT + r'\Assets\_Primordia\Art'
HERE = os.path.dirname(os.path.abspath(__file__))

def uri(path):
    with open(path, 'rb') as f:
        return 'data:image/png;base64,' + base64.b64encode(f.read()).decode()

assets = {}
for name in ['Mandibles', 'Stinger', 'Spores', 'Fins', 'JointedLegs', 'Hyphae', 'Carapace', 'StingingSkin',
             'Camouflage', 'InkSac', 'ShockGland', 'DigestiveVacuole']:
    assets['icon_' + name] = uri(rf'{ART}\UI\Icon_{name}_32.png')
for key, slot in [('slot_mouth', 'Mouth'), ('slot_limbs', 'Limbs'), ('slot_skin', 'Skin'), ('slot_organ', 'Organ')]:
    assets[key] = uri(rf'{ART}\UI\Icon_Slot_{slot}.png')

sheets = {
    'v_idle1': r'enemies\BossVirus\BossVirus_Idle_P1.png',
    'v_burst1': r'enemies\BossVirus\BossVirus_Burst_P1.png',
    'v_trans': r'enemies\BossVirus\BossVirus_Phase2_Transition.png',
    'v_summon': r'enemies\BossVirus\BossVirus_Summon_P2.png',
    'a_idle': r'enemies\BossAmoeba\BossAmoeba_Idle.png',
    'a_aimcalm': r'enemies\BossAmoeba\BossAmoeba_AimCalm_6.png',
    'a_trans': r'enemies\BossAmoeba\BossAmoeba_Angry_Transition.png',
    'a_aim': r'enemies\BossAmoeba\BossAmoeba_Aim_7.png',
    'a_split': r'enemies\BossAmoeba\BossAmoeba_Split.png',
    'h_move': r'enemies\BossAmoebaHalf\BossAmoebaHalf_Move.png',
    'a_rage': r'enemies\BossAmoeba\BossAmoeba_Rage.png',
    'a_cast3': r'enemies\BossAmoeba\BossAmoeba_Cast_3.png',
    'a_tired': r'enemies\BossAmoeba\BossAmoeba_Tired.png',
    'room_virus': r'environment\Boss\Sala_muestra_cerrada.png',
    'room_amoeba': r'enemies\BossAmoeba\vista_previa_sala.png',
}
for key, rel in sheets.items():
    assets[key] = uri(os.path.join(TEMP, rel))

projectiles = {
    'p_vspiral': rf'{ART}\Projectiles\Projectile_BossSpiral.png',
    'p_vburst': rf'{ART}\Projectiles\Projectile_BossBurst.png',
    'p_aring': rf'{ART}\Projectiles\Amoeba\Projectile_AmoebaRing.png',
    'p_aspiral': rf'{ART}\Projectiles\Amoeba\Projectile_AmoebaSpiral.png',
    'p_afan': rf'{ART}\Projectiles\Amoeba\Projectile_AmoebaFan.png',
}
for key, path in projectiles.items():
    w, h = Image.open(path).size
    assets[key] = uri(path)
    assets[key + '_f'] = w // h
    assets[key + '_s'] = h

# ---------- Piezas del piso 1 para el aspecto de la página ----------
import io, random
def png_uri(im):
    buf = io.BytesIO(); im.save(buf, format='PNG')
    return 'data:image/png;base64,' + base64.b64encode(buf.getvalue()).decode()

for key, rel in [('ui_card', 'UI_Card_Normal'), ('ui_card_hi', 'UI_Card_Highlighted'), ('ui_panel', 'UI_Panel'),
                 ('ui_button', 'UI_Button_Normal'), ('ui_button_hi', 'UI_Button_Highlighted'), ('ui_button_down', 'UI_Button_Pressed')]:
    assets[key] = uri(f'{ART}/UI/{rel}.png')
assets['logo'] = uri(f'{ART}/UI/Menus/Menu_Logo.png')
far = Image.open(f'{ART}/UI/Menus/Menu_Bg_Far.png').convert('RGBA')
far.alpha_composite(Image.open(f'{ART}/UI/Menus/Menu_Bg_Near.png').convert('RGBA'))
assets['menu_bg'] = png_uri(far)

def tiles(theme):
    folder = f'{ART}/Environment/{theme}/Tiles'
    return {os.path.splitext(f)[0]: Image.open(os.path.join(folder, f)).convert('RGBA')
            for f in os.listdir(folder) if f.endswith('.png')}

def floor_mosaic(theme, n=6, seed=1):
    t = tiles(theme); rnd = random.Random(seed)
    names = ['Floor_0'] * 9 + [k for k in t if k.startswith('Floor_') and k[6:].isdigit() and k != 'Floor_0']
    im = Image.new('RGBA', (16 * n, 16 * n))
    for y in range(n):
        for x in range(n):
            im.paste(t[rnd.choice(names)], (x * 16, y * 16))
    return png_uri(im)

assets['floor_soup'] = floor_mosaic('Soup', 8, 3)
assets['floor_virus'] = floor_mosaic('Boss', 6, 5)
assets['floor_amoeba'] = floor_mosaic('Amoeba', 6, 7)

# Pared de arriba de una sala (relleno, cara superior y cara inferior), en tira horizontal que se repite.
t = tiles('Soup')
wall = Image.new('RGBA', (16 * 4, 16 * 4))
for x in range(4):
    wall.paste(t['Wall_Cap'], (x * 16, 0))
    wall.paste(t[f'Wall_FaceUpper_{x % 2}'], (x * 16, 16))
    wall.paste(t[f'Wall_FaceLower_{(x // 2) % 2}'], (x * 16, 32))
    wall.paste(t[f'Floor_ShadowTop_{x % 2}'], (x * 16, 48))
assets['wall'] = png_uri(wall)

# Evolución de los 7 linajes (48×48, 12 cuadros) y su silueta (cuadro 7)
for lin in ('Shark', 'Squid', 'Turtle', 'Jellyfish', 'Scorpion', 'Mold', 'Puffball'):
    p = f'{ART}/Effects/Evolution/Evolution_{lin}.png'
    assets['evo_' + lin] = uri(p)
    assets['sil_' + lin] = png_uri(Image.open(p).convert('RGBA').crop((7 * 48, 0, 8 * 48, 48)))

# Enemigos normales
for rel in ['Bacteria/Bacteria_Move', 'Bacteria/Bacteria_Hurt', 'Bacteria/Bacteria_Death', 'Ameba/Ameba_Move', 'Ameba/Ameba_Shoot',
            'Ameba/Ameba_Split', 'AmebaSmall/AmebaSmall_Move', 'AmebaSmall/AmebaSmall_Hurt', 'AmebaSmall/AmebaSmall_Death']:
    assets['en_' + rel.split('/')[1]] = uri(os.path.join(TEMP, 'enemies', rel + '.png'))
assets['en_Projectile_Enemy'] = uri(f'{ART}/Projectiles/Projectile_Enemy.png')

# Presentación de los jefes: pose, nombre rotulado y retrato (../boss-intros)
BI = os.path.join(TEMP, 'boss-intros')
for key in ('Virus', 'Amoeba'):
    pose = os.path.join(BI, 'Poses', f'{key}_Intro.png')
    w, h = Image.open(pose).size
    assets[f'intro_{key}_pose'] = uri(pose); assets[f'intro_{key}_pose_f'] = w // h; assets[f'intro_{key}_pose_s'] = h
    assets[f'intro_{key}_portrait'] = uri(os.path.join(BI, 'Portraits', f'Portrait_{key}.png'))
    t = os.path.join(BI, 'Titles', f'Title_{key}.png')
    w, h = Image.open(t).size
    assets[f'intro_{key}_title'] = uri(t); assets[f'intro_{key}_title_w'] = w; assets[f'intro_{key}_title_h'] = h

with open(os.path.join(HERE, 'template.html'), encoding='utf-8') as f:
    page = f.read()
page = page.replace('/*ASSETS*/{}', json.dumps(assets))
with open(os.path.join(HERE, 'linajes.html'), 'w', encoding='utf-8') as f:
    f.write(page)
print('linajes.html', len(page) // 1024, 'KB')
