"""Documento interactivo del piso 3 (Pantano carbonífero): mete el arte (base64) en template.html y escribe piso3.html.
Mismo método que ../documento-piso2/build.py. Las hojas son tiras horizontales; las GIF de los jefes y del escenario
ya vienen animadas y se muestran tal cual."""
import base64, io, json, os
from PIL import Image

ROOT = r'D:\Proyectos\Primordia'
TEMP = ROOT + r'\docs\multimedia-temp'
ART = ROOT + r'\Assets\_Primordia\Art'
HERE = os.path.dirname(os.path.abspath(__file__))
B3 = TEMP + r'\floor3-bodies'
EN = TEMP + r'\floor3-enemies'
MU = TEMP + r'\floor3-mutations'
RE = TEMP + r'\floor3-reactions'
EV = TEMP + r'\floor3-environment'
BO = TEMP + r'\floor3-bosses'
BI = TEMP + r'\boss-intros'

assets = {}


def uri(path, mime='image/png'):
    with open(path, 'rb') as f:
        return f'data:{mime};base64,' + base64.b64encode(f.read()).decode()


def png_uri(im):
    buf = io.BytesIO(); im.save(buf, format='PNG')
    return 'data:image/png;base64,' + base64.b64encode(buf.getvalue()).decode()


def S(key, path, w=None):
    """Hoja horizontal de cuadros de w × alto (w = alto si no se da)."""
    if not path.endswith('.png'): path += '.png'
    im = Image.open(path)
    cw = w or im.height
    assert im.width % cw == 0, (path, im.size, cw)
    assets[key] = uri(path); assets[key + '_f'] = im.width // cw; assets[key + '_s'] = im.height
    if cw != im.height: assets[key + '_w'] = cw


def G(key, path):
    """GIF animada: se guarda tal cual (recodificarla engorda) con la caja de recorte del contenido; la página la recorta
    con CSS, porque las láminas de los jefes vienen en lienzos cuadrados con mucho margen."""
    from PIL import ImageChops
    im = Image.open(path)
    box = None
    for i in range(im.n_frames):
        im.seek(i); f = im.convert('RGBA')
        bg = f.getpixel((0, 0)) if i == 0 else bg
        bb = f.getchannel('A').getbbox() if bg[3] < 255 else ImageChops.difference(f.convert('RGB'), Image.new('RGB', f.size, bg[:3])).getbbox()
        if bb: box = bb if box is None else (min(box[0], bb[0]), min(box[1], bb[1]), max(box[2], bb[2]), max(box[3], bb[3]))
    if box is None: box = (0, 0, im.width, im.height)
    x0, y0 = max(0, box[0] - 3), max(0, box[1] - 3)
    x1, y1 = min(im.width, box[2] + 3), min(im.height, box[3] + 3)
    assets[key] = uri(path, 'image/gif'); assets[key + '_gw'] = im.width; assets[key + '_gh'] = im.height
    assets[key + '_gc'] = [0, 0, im.width, im.height] if '_scene' in key else [x0, y0, x1 - x0, y1 - y0]


def frames(path, cell):
    im = Image.open(path).convert('RGBA')
    return [im.crop((i * cell, 0, i * cell + cell, im.height)) for i in range(im.width // cell)]


def strip(key, fs):
    w, h = fs[0].size
    out = Image.new('RGBA', (w * len(fs), h))
    for i, f in enumerate(fs): out.paste(f, (i * w, 0))
    assets[key] = png_uri(out); assets[key + '_f'] = len(fs); assets[key + '_s'] = h
    if w != h: assets[key + '_w'] = w


def centered(size, *layers, h=None):
    out = Image.new('RGBA', (size, h or size))
    for im in layers:
        if im is not None: out.alpha_composite(im, ((size - im.width) // 2, ((h or size) - im.height) // 2))
    return out


# ---- Iconos de las 9 mutaciones y de las ranuras
for name in ['PoisonSpit', 'SpinningTop', 'VinegarJet']:
    assets['icon_' + name] = uri(rf'{B3}\Icons\Icon_{name}_32.png')
for name in ['JumpingLegs', 'SpinnerLegs', 'AmphibianSkin', 'CharredScales', 'MethaneSac', 'Pheromones']:
    assets['icon_' + name] = uri(rf'{MU}\Icons\Icon_{name}_32.png')
assets['icon_ClassParasite'] = uri(rf'{MU}\Icons\Icon_ClassParasite_32.png')
for key, slot in [('slot_mouth', 'Mouth'), ('slot_limbs', 'Limbs'), ('slot_skin', 'Skin'), ('slot_organ', 'Organ')]:
    assets[key] = uri(rf'{ART}\UI\Icon_Slot_{slot}.png')

# ---- Cuerpos del jugador (uno por familia de Boca) y lo que se ve de cada Boca
for body, atk in (('Frog', 'Attack_side'), ('Diplocaulus', 'Spin'), ('Whipscorpion', 'AttackLoop_side')):
    S(f'body_{body}_idle', rf'{B3}\{body}\Player_{body}_Idle_down')
    S(f'body_{body}_atk', rf'{B3}\{body}\Player_{body}_{atk}', 40)
FX = B3 + r'\Effects'
S('mo_spit', rf'{FX}\Proj_PoisonSpit', 24)
S('mo_spitBurst', rf'{FX}\Fx_PoisonSpit_Burst', 56)
S('mo_puddle', rf'{FX}\Fx_PoisonPuddleBig_Idle', 48)
S('mo_whirl', rf'{FX}\Fx_SpinningTop_Whirl', 48)
S('mo_deflect', rf'{FX}\Fx_SpinningTop_Deflect', 16)
S('mo_stream', rf'{FX}\Fx_VinegarJet_Stream', 16)
S('mo_splash', rf'{FX}\Fx_VinegarJet_Splash', 32)

# ---- Evolución de los 8 linajes (80×80, 12 cuadros) y su silueta (cuadro 7)
for lin in ('Eryops', 'Xenacanthus', 'Pulmonoscorpius', 'Centipede', 'TinderFungus', 'Cordyceps', 'Lamprey', 'Leech'):
    p = rf'{TEMP}\floor3-lineages\Evolution\Evolution_{lin}.png'
    S('evo_' + lin, p, 80)
    assets['sil_' + lin] = png_uri(Image.open(p).convert('RGBA').crop((7 * 80, 0, 8 * 80, 80)))

# ---- Extremidades, Piel, Órganos
for key, rel, w in [('mf_land', r'Effects\Fx_JumpingLegs_Land', 16), ('mf_takeoff', r'Effects\Fx_JumpingLegs_Takeoff', 32),
                    ('mf_web', r'Effects\Fx_SilkWeb_Idle', 32), ('mf_webNew', r'Effects\Fx_SilkWeb_Appear', 32),
                    ('mf_silkHit', r'Effects\Fx_SilkThread_Hit', 12), ('mf_stun', r'Effects\Fx_Stunned', 16),
                    ('mf_charge', r'Effects\Fx_AmphibianSkin_Charge', 32), ('mf_shield', r'Effects\Fx_AmphibianSkin_Shield', 48),
                    ('mf_absorb', r'Effects\Fx_AmphibianSkin_Absorb', 48), ('mf_reflect', r'Effects\Fx_AmphibianSkin_Reflect', 48),
                    ('mf_primed', r'Effects\Fx_CharredScales_Primed', 32), ('mf_blaze', r'Effects\Fx_CharredScales_Blaze', 48),
                    ('mf_burning', r'Effects\Fx_Burning', 16), ('mf_ground', r'Effects\Fx_GroundFire', 32),
                    ('mf_release', r'Effects\Fx_MethaneSac_Release', 48), ('mf_cloud', r'Effects\Fx_MethaneSac_Cloud', 40),
                    ('mf_explode', r'Effects\Fx_MethaneSac_Explode', 64), ('mf_pher', r'Effects\Fx_Pheromones_Release', 72),
                    ('mf_aura', r'Effects\Fx_Pheromones_Aura', 32), ('mf_mark', r'Effects\Fx_Pheromones_Mark', 14),
                    ('mf_frenzy', r'Effects\Fx_Pheromones_Frenzy', 14),
                    ('par_attached', r'Parasite\Parasite_Attached', 12), ('par_leap', r'Parasite\Parasite_Leap', 16),
                    ('par_drain', r'Effects\Fx_Parasite_Drain', 8), ('par_heal', r'Effects\Fx_Parasite_Heal', 32)]:
    S(key, rf'{MU}\{rel}', w)
# capa de Piel sobre cada cuerpo (el cuerpo y la piel miden lo mismo)
for layer in ('AmphibianSkin', 'CharredScales'):
    for body in ('Frog', 'Diplocaulus', 'Whipscorpion'):
        bf = frames(rf'{B3}\{body}\Player_{body}_Idle_side.png', 32)
        lf = frames(rf'{MU}\Skin\Skin_{layer}_{body}_Idle_side.png', 32)
        strip(f'ml_{layer}_{body}', [centered(48, b, l) for b, l in zip(bf, lf)])

# ---- Especiales del ámbar: iconos con la insignia, capa sobre cada cuerpo y efectos
badge = Image.open(rf'{TEMP}\clam-mutations\UI\UI_Special_Badge.png').convert('RGBA')
for name in ('Cerci', 'AmberCapsule', 'AmberDrop'):
    ic = Image.open(rf'{MU}\Icons\Icon_{name}_32.png').convert('RGBA')
    out = Image.new('RGBA', (36, 36)); out.alpha_composite(ic, (0, 4)); out.alpha_composite(badge, (27, 0))
    assets['sp_icon_' + name] = png_uri(out)
for body, bw in (('Frog', 32), ('Diplocaulus', 32), ('Whipscorpion', 32)):
    bf = frames(rf'{B3}\{body}\Player_{body}_Idle_side.png', bw)
    for kind, tag in (('Cerci', 'Cerci'), ('AmberCapsule', 'AmberCapsule'), ('AmberGland_Full', 'AmberGland')):
        lf = frames(rf'{MU}\Special\Special_{kind}_{body}_Idle_side.png', 56)
        strip(f'sp_{tag}_{body}', [centered(56, bf[i % len(bf)], l) for i, l in enumerate(lf)])
for key, rel, w in [('sp_cerciFlash', r'Effects\Fx_Cerci_Flash', 32), ('sp_cerciReady', r'Effects\Fx_Cerci_Ready', 16),
                    ('sp_ambForm', r'Effects\Fx_AmberBlock_Form', 48), ('sp_ambHold', r'Effects\Fx_AmberBlock_Hold', 48),
                    ('sp_ambCrack', r'Effects\Fx_AmberBlock_Crack', 48), ('sp_ambShatter', r'Effects\Fx_AmberBlock_Shatter', 64),
                    ('sp_ambDrop', r'Effects\Proj_AmberDrop', 10)]:
    S(key, rf'{MU}\{rel}', w)
S('sp_block', rf'{EV}\pieces\AmberBlock_Idle', 40)
S('sp_blockBreak', rf'{EV}\pieces\AmberBlock_Break', 40)

# ---- Reacciones
for key, rel, w in [('re_cloudForm', r'Effects\Fx_ToxicCloud_Form', 48), ('re_cloudIdle', r'Effects\Fx_ToxicCloud_Idle', 48),
                    ('re_cloudFade', r'Effects\Fx_ToxicCloud_Fade', 48), ('re_puddleIgnite', r'Effects\Fx_PoisonPuddle_Ignite', 48),
                    ('re_mistIgnite', r'Effects\Fx_MistIgnite', 32), ('re_mistFlare', r'Effects\Fx_MistFlare', 24),
                    ('re_ignite', r'Effects\Fx_Ignite', 16), ('re_poisoned', r'Effects\Fx_Poisoned', 16), ('re_fireOut', r'Effects\Fx_FireOut', 32),
                    ('re_sacBurst', r'Swamp\Methane_Sac_Burst', 32), ('re_bulbIdle', r'Swamp\PoisonBulb_Idle', 32),
                    ('re_bulbHit', r'Swamp\PoisonBulb_Hit', 32), ('re_bulbBurst', r'Swamp\PoisonBulb_Burst', 32),
                    ('re_bulbRegrow', r'Swamp\PoisonBulb_Regrow', 32)]:
    S(key, rf'{RE}\{rel}', w)
S('re_sac', rf'{EV}\Swamp\props\Methane_Sac_Idle', 24)
S('re_ventIdle', rf'{EV}\Swamp\props\Vent_Idle', 32)
S('re_ventWarn', rf'{EV}\Swamp\props\Vent_Warn', 32)
S('re_ventBlast', rf'{EV}\Swamp\props\Vent_Blast', 32)
G('re_mist', rf'{EV}\Swamp\gif\Methane_Mist.gif')

# ---- Enemigos y trampas
for key, rel, w in [('en_Calamites_Idle', 'Calamites/Calamites_Idle', 32), ('en_Calamites_Hurt', 'Calamites/Calamites_Hurt', 32),
                    ('en_Calamites_Death', 'Calamites/Calamites_Death', 48), ('en_Sprout_Emerge', 'CalamitesSprout/CalamitesSprout_Emerge', 16),
                    ('en_Sprout_Move', 'CalamitesSprout/CalamitesSprout_Move', 16), ('en_Sprout_Wither', 'CalamitesSprout/CalamitesSprout_Wither', 16),
                    ('en_Spore', 'Effects/Proj_CalamitesSpore', 8), ('en_SporePop', 'Effects/Fx_CalamitesSpore_Pop', 12),
                    ('en_Medullosa_Idle', 'Medullosa/Medullosa_Idle', 32), ('en_Medullosa_Aim', 'Medullosa/Medullosa_Aim', 32),
                    ('en_Medullosa_Shoot', 'Medullosa/Medullosa_Shoot', 32), ('en_Medullosa_Death', 'Medullosa/Medullosa_Death', 40),
                    ('en_Seed', 'Effects/Proj_MedullosaSeed', 12), ('en_SeedTarget', 'Effects/Fx_MedullosaSeed_Target', 24),
                    ('en_SeedBurst', 'Effects/Fx_MedullosaSeed_Burst', 24),
                    ('en_Dragonfly_Fly', 'Dragonfly/Dragonfly_Fly', 40), ('en_Dragonfly_Warn', 'Dragonfly/Dragonfly_Warn', 40),
                    ('en_Dragonfly_Dive', 'Dragonfly/Dragonfly_Dive', 40), ('en_Dragonfly_Perch', 'Dragonfly/Dragonfly_Perch', 40),
                    ('en_Dragonfly_Death', 'Dragonfly/Dragonfly_Death', 40),
                    ('en_Beetle_Move', 'Beetle/Beetle_Move', 32), ('en_Beetle_Turn', 'Beetle/Beetle_Turn', 32),
                    ('en_Beetle_Warn', 'Beetle/Beetle_Warn', 32), ('en_Beetle_Spray', 'Beetle/Beetle_Spray', 32),
                    ('en_Beetle_Death', 'Beetle/Beetle_Death', 40), ('en_BeetleCone', 'Effects/Fx_BeetleSpray', 56),
                    ('en_Spider_Hidden', 'Spider/Spider_Hidden', 24), ('en_Spider_Reveal', 'Spider/Spider_Reveal', 32),
                    ('en_Spider_Move', 'Spider/Spider_Move', 32), ('en_Spider_Jump', 'Spider/Spider_Jump', 40),
                    ('en_Spider_Shoot', 'Spider/Spider_Shoot', 32), ('en_Spider_Death', 'Spider/Spider_Death', 40),
                    ('en_Web_Appear', 'Effects/Fx_SpiderWeb_Appear', 32), ('en_Web_Idle', 'Effects/Fx_SpiderWeb_Idle', 32),
                    ('en_Web_Burn', 'Effects/Fx_SpiderWeb_Burn', 32), ('en_Silk', 'Effects/Proj_SpiderSilk', 10),
                    ('en_Toad_Idle', 'Toad/Toad_Idle', 32), ('en_Toad_Hop', 'Toad/Toad_Hop', 32), ('en_Toad_Spit', 'Toad/Toad_Spit', 32),
                    ('en_Toad_Inflate', 'Toad/Toad_Inflate', 40), ('en_Toad_Swim', 'Toad/Toad_Swim', 32), ('en_Toad_Death', 'Toad/Toad_Death', 40),
                    ('en_Mud', 'Effects/Proj_ToadMud', 12), ('en_MudSplat', 'Effects/Fx_ToadMud_Splat', 24), ('en_ToadBurst', 'Effects/Fx_Toad_Burst', 48),
                    ('en_Plant_Idle', 'CarnivorousPlant/CarnivorousPlant_Idle', 40), ('en_Plant_Snap', 'CarnivorousPlant/CarnivorousPlant_Snap', 40),
                    ('en_Plant_Chew', 'CarnivorousPlant/CarnivorousPlant_Chew', 40), ('en_Plant_Wilt', 'CarnivorousPlant/CarnivorousPlant_Wilt', 40),
                    ('en_Mayflies_Idle', 'Mayflies/Mayflies_Idle', 40), ('en_Mayflies_Burn', 'Mayflies/Mayflies_Burn', 40)]:
    S(key, os.path.join(EN, rel), w)

# ---- Jefes: GIF ya animadas
AR, ME, MR, LE = BO + r'\arthropleura', BO + r'\meganeura', BO + r'\megarachne', BO + r'\lepidodendron'
for key, path in [
        ('ar_curva', AR + r'\gif\Arthropleura_curva.gif'), ('ar_rear', AR + r'\gif\Arthropleura_Head_Rear.gif'),
        ('ar_open', AR + r'\gif\Arthropleura_Head_Open.gif'), ('ar_eat', AR + r'\gif\Arthropleura_Head_Eat.gif'),
        ('ar_chargeWarn', AR + r'\gif\Arthropleura_Head_ChargeWarn.gif'), ('ar_charge', AR + r'\gif\Arthropleura_Head_Charge.gif'),
        ('ar_edgeBreak', AR + r'\gif\Fx_Arthropleura_EdgeBreak.gif'), ('ar_stomp', AR + r'\gif\Fx_Arthropleura_Stomp.gif'),
        ('ar_shock', AR + r'\gif\Fx_Arthropleura_Shockwave.gif'), ('ar_wallBreak', AR + r'\gif\Fx_Arthropleura_WallBreak.gif'),
        ('ar_sigFall', AR + r'\gif\Sigillaria_Fall.gif'), ('ar_plates', AR + r'\gif\Arthropleura_Segment_PlatesOpen.gif'),
        ('ar_openWalk', AR + r'\gif\Arthropleura_Segment_OpenWalk.gif'), ('ar_roll', AR + r'\gif\Arthropleura_Roll.gif'),
        ('ar_death', AR + r'\gif\Arthropleura_Head_Death.gif'), ('ar_smoke', AR + r'\gif\Arthropleura_Fx_Arthropleura_DeathSmoke.gif'),
        ('ar_sceneRam', AR + r'\escenas\Arthropleura_F2_embestida.gif'), ('ar_sceneFire', AR + r'\escenas\Arthropleura_fuego.gif'),
        ('ar_barEmber', AR + r'\gif\BossBar_Ember.gif'), ('ar_barFlesh', AR + r'\gif\BossBar_Flesh.gif'),
        ('ar_barPlates', AR + r'\gif\BossBar_PlatesOpen.gif'), ('ar_barRoll', AR + r'\gif\BossBar_Roll.gif'),
        ('ar_arrows', AR + r'\gif\EdgeArrow_Badges.gif'),
        ('me_fly', ME + r'\gif\Meganeura_Fly.gif'), ('me_hover', ME + r'\gif\Meganeura_Hover.gif'),
        ('me_diveWarn', ME + r'\gif\Meganeura_DiveWarn.gif'), ('me_dive', ME + r'\gif\Meganeura_Dive.gif'),
        ('me_perch', ME + r'\gif\Meganeura_Perch.gif'), ('me_gust', ME + r'\gif\Meganeura_Gust.gif'),
        ('me_lay', ME + r'\gif\Meganeura_Lay.gif'), ('me_eggs', ME + r'\gif\Meganeura_Eggs_Idle.gif'),
        ('me_eggsBreak', ME + r'\gif\Meganeura_Eggs_Break.gif'), ('me_nymph', ME + r'\gif\Meganeura_Nymph_Move.gif'),
        ('me_nymphStrike', ME + r'\gif\Meganeura_Nymph_Strike.gif'), ('me_screech', ME + r'\gif\Meganeura_Screech.gif'),
        ('me_perchRage', ME + r'\gif\Meganeura_PerchRage.gif'), ('me_shocked', ME + r'\gif\Meganeura_Shocked.gif'),
        ('me_boltBig', ME + r'\gif\Fx_Bolt_Big.gif'), ('me_boltMark', ME + r'\gif\Fx_Bolt_Big_Mark.gif'),
        ('me_boltSmall', ME + r'\gif\Fx_Bolt_Small.gif'), ('me_diveMark', ME + r'\gif\Fx_Meganeura_DiveMark_Follow.gif'),
        ('me_diveLocked', ME + r'\gif\Fx_Meganeura_DiveMark_Locked.gif'), ('me_deathFall', ME + r'\gif\Meganeura_DeathFall.gif'),
        ('me_sceneStorm', ME + r'\gif\escenas\Meganeura_escena_tormenta_entrada.gif'),
        ('me_sceneEggs', ME + r'\gif\escenas\Meganeura_escena_huevos.gif'),
        ('me_sceneDeath', ME + r'\gif\escenas\Meganeura_escena_muerte.gif'), ('me_bar', ME + r'\gif\Meganeura_BossBar_combate.gif'),
        ('mr_walk', MR + r'\gif\Megarachne_Walk.gif'), ('mr_idle', MR + r'\gif\Megarachne_Idle.gif'),
        ('mr_threat', MR + r'\gif\Megarachne_Threat.gif'), ('mr_idleRage', MR + r'\gif\Megarachne_Idle_Rage.gif'),
        ('mr_silk', MR + r'\gif\Megarachne_SilkShoot.gif'), ('mr_dropWarn', MR + r'\gif\Megarachne_DropWarn.gif'),
        ('mr_land', MR + r'\gif\Megarachne_Land.gif'), ('mr_stunned', MR + r'\gif\Megarachne_Stunned.gif'),
        ('mr_stars', MR + r'\gif\Fx_Megarachne_Stunned.gif'), ('mr_sac', MR + r'\gif\Megarachne_Sac_Tremble.gif'),
        ('mr_brood', MR + r'\gif\Megarachne_Brood_Move.gif'), ('mr_net', MR + r'\gif\Fx_MegarachneNet_Shadow.gif'),
        ('mr_cable', MR + r'\gif\Cable_mecha.gif'), ('mr_web', MR + r'\gif\Fx_MegarachneWeb_Appear.gif'),
        ('mr_dead', MR + r'\gif\Megarachne_Dead.gif'), ('mr_cocoon', MR + r'\gif\Megarachne_Cocoon_Open.gif'),
        
        
        ('mr_sceneNet', MR + r'\gif\escenas\Megarachne_red.gif'), ('mr_sceneSac', MR + r'\gif\escenas\Megarachne_saco.gif'),
        ('mr_bar', MR + r'\gif\Megarachne_BossBar_combate.gif'),
        ('le_veinsWarn', LE + r'\gif\Lepido_Veins_Warn.gif'), ('le_veinsHit', LE + r'\gif\Lepido_Veins_Hit.gif'),
        ('le_whip', LE + r'\gif\Lepido_Whip_Sweep.gif'), ('le_spikes', LE + r'\gif\Lepido_Spikes_Erupt.gif'),
        ('le_shroom', LE + r'\gif\Lepido_Shroom_Warn.gif'), ('le_spore', LE + r'\gif\Proj_LepidoSpore.gif'),
        ('le_slam', LE + r'\gif\Fx_ConeSlam.gif'), ('le_coneShadow', LE + r'\gif\Fx_ConeShadow.gif'),
        ('le_coneExposed', LE + r'\gif\Lepido_Cone_Exposed_Idle.gif'), ('le_coneBurst', LE + r'\gif\Lepido_Cone_Burst.gif'),
        ('le_coneBurn', LE + r'\gif\Lepido_Cone_Burn0.gif'), ('le_coneRegrow', LE + r'\gif\Lepido_Cone_Regrow2.gif'),
        ('le_roach', LE + r'\gif\Critter_Roach_Run.gif'), ('le_shaft', LE + r'\gif\Fx_LightShaft.gif'),
        ('le_sceneWallFire', LE + r'\gif\escenas\Lepido_pared_fuego.gif'), ('le_sceneWallKnot', LE + r'\gif\escenas\Lepido_pared_nudo.gif'),
        
        ('le_sceneWarn', LE + r'\gif\escenas\Lepido_aviso.gif'), 
        ('le_sceneRoom', LE + r'\gif\escenas\Lepido_recompensa_sala.gif'), ('le_bar', LE + r'\gif\Lepido_BossBar_combate.gif'),
]:
    G(key, path)
# Presentación de los jefes: retrato y nombre rotulado (../boss-intros)
for key in ('Arthropleura', 'Meganeura', 'Megarachne', 'Lepidodendron'):
    assets[f'intro_{key}_portrait'] = uri(rf'{BI}\Portraits\Portrait_{key}.png')
    t = rf'{BI}\Titles\Title_{key}.png'
    w, h = Image.open(t).size
    assets[f'intro_{key}_title'] = uri(t); assets[f'intro_{key}_title_w'] = w; assets[f'intro_{key}_title_h'] = h
G('intro_Arthropleura_pose', AR + r'\gif\Arthropleura_Head_Open.gif')
G('intro_Lepidodendron_pose', LE + r'\gif\Lepido_Veins_Warn.gif')
G('intro_Meganeura_pose', ME + r'\gif\Meganeura_Intro.gif')
G('intro_Megarachne_pose', MR + r'\gif\Megarachne_IntroEmerge_Nido.gif')

# ---- Piezas fijas: cono-cofre, almacén, teletransportes, flores moradas
PI = EV + r'\pieces'
for key, rel, w in [('pc_coneIdle', 'Cone_Big_Idle', 32), ('pc_coneOpen', 'Cone_Big_Open', 32), ('pc_coneOpened', 'Cone_Big_Opened', 32),
                    ('pc_podReceive', 'HeartPod_Receive', 32), ('pc_podStored', 'HeartPod_Stored', 32),
                    ('pc_flowerIdle', 'Flower_Purple_Idle', 16), ('pc_flowerPick', 'Flower_Purple_Pick', 16)]:
    S(key, rf'{PI}\{rel}', w)
for n in (1, 2, 3):
    S(f'tp{n}_dormant', rf'{PI}\Teleport_Floor{n}_Dormant', 32)
    S(f'tp{n}_idle', rf'{PI}\Teleport_Floor{n}_Idle', 32)
    S(f'tp{n}_warp', rf'{PI}\Teleport_Floor{n}_Warp', 32)
G('rootwall', EV + r'\Swamp\gif\RootWall.gif')
G('rootwallSeed', EV + r'\Swamp\gif\RootWall_Seed.gif')

# ---- El pantano: las tres escenas, puertas de raíces, maleza, lodo
for theme, key in (('Swamp', 'swamp'), ('Smoke', 'smoke'), ('Burnt', 'burnt')):
    assets['scene_' + key] = uri(rf'{EV}\{theme}\Escena_x3.png')
    G('door_' + key, rf'{EV}\{theme}\gif\Door_Top.gif')
    G('brush_' + key, rf'{EV}\{theme}\gif\Brush_Top.gif')
for key, rel in [('fern_sway', 'Fern_Leafy_Sway'), ('fern_burn', 'Fern_Leafy_Burn'), ('fern_rustle', 'Fern_Leafy_Rustle'),
                 ('mud_bubbles', 'Mud_Bubbles'), ('moss', 'Deco_DripMoss'), ('puddle_sludge', 'Puddle_Sludge_Lobed'),
                 ('puddle_water', 'Puddle_Water_Lobed')]:
    G(key, rf'{EV}\Swamp\gif\{rel}.gif')

# ---- Aspecto de la página: piezas del juego teñidas de pantano
for key, rel in [('ui_card', 'UI_Card_Normal'), ('ui_panel', 'UI_Panel'), ('ui_button', 'UI_Button_Normal'), ('ui_button_hi', 'UI_Button_Highlighted')]:
    assets[key] = uri(f'{ART}/UI/{rel}.png')
assets['logo'] = uri(f'{ART}/UI/Menus/Menu_Logo.png')
far = Image.open(f'{ART}/UI/Menus/Menu_Bg_Far.png').convert('RGBA')
far.alpha_composite(Image.open(f'{ART}/UI/Menus/Menu_Bg_Near.png').convert('RGBA'))
px = far.load()
for y in range(far.height):
    for x in range(far.width):
        r, g, b, a = px[x, y]
        l = (r + g + b) / 3
        px[x, y] = (int(l * 0.5 + 18), int(l * 0.62 + 22), int(l * 0.32 + 10), a)
assets['menu_bg'] = png_uri(far)


def tiles(theme):
    folder = rf'{EV}\{theme}\tiles'
    return {os.path.splitext(f)[0]: Image.open(os.path.join(folder, f)).convert('RGBA') for f in os.listdir(folder)}


import random


def floor_mosaic(theme, n=6, seed=1):
    t = tiles(theme); rnd = random.Random(seed)
    names = ['Floor_0'] * 6 + [k for k in t if k.startswith('Floor_') and k[6:].isdigit() and k != 'Floor_0']
    im = Image.new('RGBA', (16 * n, 16 * n))
    for y in range(n):
        for x in range(n): im.paste(t[rnd.choice(names)], (x * 16, y * 16))
    return png_uri(im)


assets['floor_swamp'] = floor_mosaic('Swamp', 8, 3)
assets['floor_smoke'] = floor_mosaic('Smoke', 6, 4)
assets['floor_burnt'] = floor_mosaic('Burnt', 6, 5)
t = tiles('Swamp')
wall = Image.new('RGBA', (64, 64))
for x in range(4):
    wall.paste(t['Wall_Cap'], (x * 16, 0)); wall.paste(t[f'Wall_FaceUpper_{x % 2}'], (x * 16, 16))
    wall.paste(t[f'Wall_FaceLower_{(x // 2) % 2}'], (x * 16, 32)); wall.paste(t[f'Floor_ShadowTop_{x % 2}'], (x * 16, 48))
assets['wall'] = png_uri(wall)

# ---- Corazones del HUD (para la explicación de los corazones de sala)
UI = rf'{ART}\UI'
for key, rel in [('hud_full', 'UI_Heart_Full'), ('hud_half', 'UI_Heart_Half'), ('hud_empty', 'UI_Heart_Empty')]:
    assets[key] = uri(rf'{UI}\{rel}.png')

with open(os.path.join(HERE, 'template.html'), encoding='utf-8') as f:
    page = f.read()
page = page.replace('/*ASSETS*/{}', json.dumps(assets))
with open(os.path.join(HERE, 'piso3.html'), 'w', encoding='utf-8') as f:
    f.write(page)
print('piso3.html', len(page) // 1024, 'KB')
