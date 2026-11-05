"""Cucaracha ladrona del piso 3 (almacén de corazones y sala Ladronas; ver README).

Cucaracha carbonífera (tipo Archimylacris): plana y ovalada, pronoto en escudo con el borde ámbar que tapa la
cabeza, alas con venas, antenas largas y cercos. Vista desde arriba y mirando a la derecha: el código la gira hacia
donde corre. El botín (medio corazón o biomasa) lo pone el código encima del lomo (punto LOMO).
Hecho con la skill primordia-sprites (pixel.py): 16 px por unidad, luz arriba a la izquierda, contorno OUTL.

Uso: python generar_cucaracha.py
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
TEMP = os.path.normpath(os.path.join(HERE, '..'))
sys.path.insert(0, os.path.join(TEMP, 'floor3-environment'))
from base_piso3 import *
from PIL import Image

W, H = 40, 28
BODY = [C(c) for c in ramp('#7a3e22', 5)]   # pardo rojizo
RIM = [C('#b0581a'), C('#e89a30'), C('#ffcc5a')]   # borde ámbar del pronoto y puntas de los cercos
BELLY = [C(c) for c in ramp('#c8a070', 4)]
LOMO = (16, 14)                          # dónde va el botín (centro de las alas), en la celda de 40×28

def roach(cv=None, ox=0, oy=0, legs=0.0, ant=0.0, crouch=0.0, stretch=0.0, pal=None, belly=False, legs_up=0.0,
          hide=None, jaw=False):
    """La cucaracha mirando a la derecha, centrada en (18, 14) + (ox, oy): un óvalo plano continuo (alas detrás y
    pronoto en escudo delante, más ancho, con el borde ámbar), patas en trípode, antenas largas y cercos.
    legs: fase de la marcha (0..1). ant: vaivén de las antenas. crouch: agacha el pronoto (coger el botín).
    stretch: estira el cuerpo hacia atrás (subírselo al lomo). pal: rampa plana (daño). belly: panza arriba
    (muerte; legs_up = fase del pataleo). hide: función (x, y) -> True si ese píxel está bajo tierra (grieta)."""
    cv = cv or Canvas(W, H)
    tmp = Canvas(cv.w, cv.h)
    cx, cy = W // 2 - 2 + ox, H // 2 + oy
    wx = cx - 2 - stretch; wr = 9 + stretch * .5 - crouch * .4
    px_ = cx + 4.5 + crouch * 1.0
    if belly:
        ellipse(tmp, wx, cy, wr, 4.8, BELLY + [BELLY[3]])
        ellipse(tmp, px_, cy, 4.2, 5.0, BELLY + [BELLY[3]], shift=1)
        for x in range(int(wx - wr) + 2, int(wx + wr) - 1, 2):            # segmentos del vientre
            for y in range(int(cy - 3), int(cy + 4)): tmp.put_in(x, y, BELLY[1])
    else:
        body = pal or BODY
        x0, x1 = wx - wr, px_ + 4.4                                        # de la punta de las alas al borde del escudo
        mid = (x0 + x1) / 2; hl = (x1 - x0) / 2
        def inside(x, y):
            u = (x + .5 - mid) / hl
            if abs(u) >= 1: return False
            h = 6.0 * (1 - u * u) ** .5 * (1 - .28 * max(0, -u)) + (.4 if u > .3 else 0)   # huevo: ancho delante
            return abs(y + .5 - cy) < h
        fill(tmp, inside, mid - 2, cy - 1, hl, 6, body, shift=0 if pal else -1)
        if not pal:
            for (x, y), c in list(tmp.px.items()):
                u = (x + .5 - mid) / hl
                h = 6.0 * (1 - min(1, u * u)) ** .5 * (1 - .28 * max(0, -u)) + (.4 if u > .3 else 0)
                front = x + .5 > px_ - 3.2 - abs(y + .5 - cy) * .25
                if front and (abs(y + .5 - cy) > h - 1.4 or x + .5 > x1 - 1.6): tmp.px[(x, y)] = RIM[1] if y < cy else RIM[0]   # borde ámbar
                elif abs(x + .5 - (px_ - 3.2 - abs(y + .5 - cy) * .25)) < .6: tmp.px[(x, y)] = BODY[0]             # canto trasero del escudo
            for x in range(int(x0) + 2, int(px_ - 4)):                    # sutura y venas de las alas
                tmp.put_in(x, cy, BODY[0])
                if (x - int(wx)) % 3 == 0:
                    for sd in (-1, 1):
                        tmp.put_in(x, cy + sd * 2, BODY[1]); tmp.put_in(x - 1, cy + sd * 3, BODY[1])
            tmp.put_in(px_ - 1, cy - 3, BODY[4]); tmp.put_in(px_, cy - 3, BODY[3]); tmp.put_in(px_ - 1, cy - 2, BODY[3])   # brillo del escudo
            tmp.put_in(wx - 4, cy - 3, BODY[3]); tmp.put_in(wx - 3, cy - 3, BODY[3])
    tmp.outline(); tmp.clean()
    leg_c = pal[1] if pal else BODY[3]; leg_d = pal[0] if pal else BODY[2]; ant_c = pal[0] if pal else BODY[2]
    for k, (bx, fwd) in enumerate(((cx + 4, 3), (cx - 1, 0), (cx - 6, -3))):   # patas: trípode
        for side in (-1, 1):
            if belly:                                                       # patas al aire, pataleando
                tw = math.sin((legs_up * 2.1 + k * 1.3 + side) * 2) * 1.3
                for j in range(4): tmp.put(bx + fwd * .25 * j + tw * (j > 1), cy + side * (1 + j), leg_d if j < 2 else leg_c)
                continue
            grp = (k + (side > 0)) % 2
            sw = math.sin((legs + grp * .5) * 2 * math.pi) * 2.2
            y0 = cy + side * 5.5
            pts = [(bx, y0), (bx + fwd * .3 + sw * .4, y0 + side * 1.4), (bx + fwd * .7 + sw * .8, y0 + side * 2.6),
                   (bx + fwd + sw, y0 + side * 3.6), (bx + fwd * 1.3 + sw, y0 + side * 4.8)]
            for j, (x, y) in enumerate(pts): tmp.put_out(x, y, leg_d if j < 2 else leg_c)
    if not belly:
        for side in (-1, 1):                                               # antenas largas que se curvan hacia fuera
            a0 = side * (.35 + ant * .25) - crouch * side * .2
            x, y = px_ + 4, cy + side * 1.2
            for j in range(11):
                a = a0 + side * j * .085
                x += math.cos(a); y += math.sin(a) * .9
                tmp.put_out(x, y, ant_c)
        for side in (-1, 1):                                               # cercos con la punta ámbar
            for j, c in enumerate((BODY[2], BODY[2], RIM[1])):
                tmp.put_out(wx - wr - j + .5 - stretch * .5, cy + side * (1 + j * .7), pal[1] if pal else c)
        if jaw:
            tmp.put_out(px_ + 5, cy - 1, BODY[0]); tmp.put_out(px_ + 5, cy + 1, BODY[0])
    for p_, c in tmp.px.items():
        if hide and hide(*p_): continue
        cv.px[p_] = c
    return cv

def shadow(cv, cx, cy, rx=9, ry=3):
    for y in range(int(cy - ry), int(cy + ry) + 1):
        for x in range(int(cx - rx), int(cx + rx) + 1):
            if ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 <= 1: cv.put_out(x, y, (24, 20, 37, 70))

def with_shadow(cv, ox=0, oy=0):
    shadow(cv, W // 2 - 3 + ox, H // 2 + 3 + oy); return cv

# --- animaciones ----------------------------------------------------------------------------------
def run():  return [with_shadow(roach(legs=f / 4, ant=math.sin(f * 1.6) * .4)) for f in range(4)]
def idle(): return [with_shadow(roach(legs=0, ant=[0, .8, .2, -.6][f])) for f in range(4)]

def grab():
    """Se para, agacha el pronoto, engancha el botín con las mandíbulas y se lo sube al lomo estirándose hacia atrás.
    Cuadro que cuenta: 3 (el botín aparece en LOMO)."""
    poses = [dict(ant=.6), dict(crouch=1, ant=-.4), dict(crouch=1.6, ant=-.6, jaw=True), dict(stretch=1.4, ant=.8), dict(ant=.2)]
    return [with_shadow(roach(**p)) for p in poses]

def hurt():
    return [with_shadow(roach(pal=WHITE4 + [WHITE4[3]])), with_shadow(roach(pal=RED4 + [RED4[3]]))]

def death():
    """Un respingo, se da la vuelta, patalea panza arriba y se apaga."""
    fr = [with_shadow(roach(ant=1, stretch=.5)), with_shadow(roach(crouch=.6, pal=None, ant=-1))]
    for k in range(3): fr.append(with_shadow(roach(belly=True, legs_up=k)))
    dim = roach(belly=True, legs_up=3)
    for p_, c in list(dim.px.items()):
        if c[3] == 255 and c != OUTL: dim.px[p_] = blend(c, C(PALE4[1]), 140)
    fr.append(with_shadow(dim))
    return fr

# --- grieta del suelo: salir y meterse ------------------------------------------------------------
CW = 50                                  # celda de las hojas con grieta (50×28)
HX, HY = 10, 14                           # centro de la grieta en Emerge (en Hide va en CW - 1 - HX)

def crack(cv, hx, hy):
    """Grieta del suelo (8×5): hueco oscuro con el labio de abajo claro, tierra levantada alrededor."""
    for y in range(hy - 4, hy + 5):
        for x in range(hx - 7, hx + 8):
            d = math.hypot((x + .5 - hx) / 4.6, (y + .5 - hy) / 2.8)
            if d < 1: cv.put(x, y, PASSAGE if y < hy + 1 else OUTL)
            elif d < 1.5: cv.put(x, y, C(EARTH[3]) if y > hy else C(EARTH[1]))
            elif d < 1.8 and (x + y) % 3 == 0: cv.put(x, y, C(EARTH[2]))

EARTH = ['#22180f', '#3a2a1c', '#55402a', '#735a3a', '#94784e']

def emerge():
    """Sale de la grieta corriendo hacia la derecha: primero las antenas, luego el escudo, el cuerpo y se aleja.
    El código la pone en la grieta sin girarla y, al acabar, pasa a Run girada. 6 cuadros."""
    out = []
    for f, x_front in enumerate((-12, -7, -2, 4, 9, 13)):
        cv = Canvas(CW, H); crack(cv, HX, HY)
        hide = lambda x, y: x < HX + 1
        ox = HX + x_front - W // 2 + 4
        roach(cv, ox=ox, legs=f / 4, ant=math.sin(f) * .5, hide=hide)
        if f >= 3: shadow(cv, W // 2 - 3 + ox, H // 2 + 3)
        out.append(cv)
    return out

def hide_anim():
    """Llega corriendo hacia la derecha y se mete de cabeza en la grieta (con el botín). Cuadro que cuenta: 3 (el
    lomo entra en la grieta: el código quita el botín). 6 cuadros."""
    hx = CW - 1 - HX
    out = []
    for f, x_front in enumerate((-14, -9, -4, 1, 6, 30)):
        cv = Canvas(CW, H); crack(cv, hx, HY)
        hide = lambda x, y: x > hx - 1
        ox = hx + x_front - W // 2 - 4
        if f < 5:
            roach(cv, ox=ox, legs=f / 4, ant=-.3, hide=hide)
            shadow(cv, W // 2 - 3 + ox, H // 2 + 3) if f < 3 else None
        else:                                                              # se ha ido: polvo que cae en la grieta
            for (dx, dy) in ((-2, -1), (1, -2), (2, 0)): cv.put(hx + dx, HY + dy, C(EARTH[3]))
        out.append(cv)
    return out

def crack_only():
    cv = Canvas(20, 12); crack(cv, 10, 6); return [cv]

# --- maqueta ---------------------------------------------------------------------------------------
def floor_bg(key='Swamp', seed=4):
    floor = [Image.open(os.path.join(TEMP, 'floor3-environment', key, 'tiles', f'Floor_{k}.png')).convert('RGBA') for k in range(6)]
    bg = Image.new('RGBA', (240, 136)); rnd = random.Random(seed)
    for y in range(0, 144, 16):
        for x in range(0, 240, 16): bg.alpha_composite(floor[0 if rnd.random() < .66 else rnd.randint(1, 5)], (x, y))
    return bg

def loot_imgs():
    heart = Image.open(os.path.join(TEMP, 'ui-extras', 'Items', 'Item_RoomHeart_Idle.png')).convert('RGBA')
    heart = heart.crop((0, 0, heart.height, heart.height))
    bio = Image.open(os.path.join(TEMP, '..', '..', 'Assets', '_Primordia', 'Art', 'Items', 'Biomass', 'Item_Biomass_Big_Idle.png')).convert('RGBA')
    bio = bio.crop((0, 0, bio.height, bio.height))
    return heart, bio

def rotated(frame, loot, ang):
    """Como lo hará el código: la cucaracha girada (vecino más próximo) y el botín encima del lomo, sin girar."""
    im = frame.img().rotate(ang, resample=Image.NEAREST, expand=False)
    if loot is not None:
        lx, ly = LOMO[0] - W / 2, LOMO[1] - H / 2
        a = math.radians(ang); rx, ry = lx * math.cos(a) + ly * math.sin(a), -lx * math.sin(a) + ly * math.cos(a)
        im.alpha_composite(loot, (int(W / 2 + rx - loot.width / 2), int(H / 2 + ry - loot.height / 2) - 2))
    return im

def scene(anims, f):
    bg = floor_bg(); heart, bio = loot_imgs()
    bg.alpha_composite(anims['Roach_Emerge'][min(f, 5)].img(), (20, 20))
    for (x, y, ang, loot, k) in ((120, 30, 20, heart, 0), (160, 80, 150, bio, 1), (60, 92, -60, None, 2), (190, 30, 200, bio, 3)):
        bg.alpha_composite(rotated(anims['Roach_Run'][(f + k) % 4], loot, ang), (x, y))
    g = anims['Roach_Grab'][min(f, 4)]
    bg.alpha_composite(rotated(g, bio if f >= 3 else None, 0), (100, 100))
    if f < 3: bg.alpha_composite(bio, (100 + 33, 100 + 14 - bio.height // 2))
    return bg

if __name__ == '__main__':
    anims = {'Roach_Idle': idle(), 'Roach_Run': run(), 'Roach_Grab': grab(), 'Roach_Hurt': hurt(), 'Roach_Death': death(),
             'Roach_Emerge': emerge(), 'Roach_Hide': hide_anim(), 'Deco_RoachCrack': crack_only()}
    for name, frames in anims.items(): save_sheet(os.path.join(HERE, name + '.png'), frames)
    preview(os.path.join(HERE, 'vista_previa_cucaracha.png'), anims, BG, scale=4)
    os.makedirs(os.path.join(HERE, 'gif'), exist_ok=True)
    for name, ms in (('Roach_Run', 60), ('Roach_Grab', 110), ('Roach_Emerge', 80), ('Roach_Hide', 80), ('Roach_Death', 110), ('Roach_Idle', 160)):
        gif(os.path.join(HERE, 'gif', name + '.gif'), anims[name], ms, BG, scale=6)
    frames = [scene(anims, f).resize((960, 544), Image.NEAREST) for f in range(6)]
    frames[0].save(os.path.join(HERE, 'maqueta_cucaracha.png'))
    frames[0].save(os.path.join(HERE, 'gif', 'escena_cucaracha.gif'), save_all=True, append_images=frames[1:], duration=120, loop=0, disposal=2)
    print('Listo en', HERE)
