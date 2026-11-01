"""Ninfa de la Meganeura (tanda 6f-1): las crías que salen de los huevos de la F2. Skill primordia-sprites.

Picada como su madre (cámara a 55°, motor 3D de generar_arthropleura vía generar_meganeura). ~32 px de largo, el
tamaño de la rana: rechoncha y parda verdosa, cabeza ancha, ojos saltones verde azulado como los de la madre,
esbozos de alas verde azulado en el lomo, abdomen ancho por bandas y tres pinchos en la punta, patas largas.
El labio extensible va plegado bajo la cabeza como una máscara; al atacar sale disparado ~2,5 u (40 px) con dos
pinzas en la punta.

Celdas: 64×64 (todo) y 112×112 (Strike, para que quepa el labio). Pivote = centro de la celda = centro del tórax.

Uso: python generar_ninfa_meganeura.py todo      (hojas Meganeura_Nymph_* en Sheets/ y Shadows/, vistas y GIF)
     python generar_ninfa_meganeura.py muestra
"""
import math, os, random, sys
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
_argv = sys.argv; sys.argv = sys.argv[:1]
import generar_meganeura as M                          # motor (AR), ellipsoid(), hurt(), place(), burnt_floor()
sys.argv = _argv
AR = M.AR
from base_piso3 import *                               # noqa: F401,F403
from PIL import Image, ImageDraw

OUT = HERE
BG = M.BG
NC, NS = 64, 112                                        # celda normal y la del golpe
K = .76                                                 # escala del modelo (largo ≈ 32 px)

AR.PALS.update({
    'nbody': ['#1c1c10', '#36341c', '#5a5530', '#86804a', '#b4ac70'],   # pardo verdoso
    'nband': ['#0c0c08', '#18180e', '#2a2816', '#3e3c22', '#54502e'],   # bandas del abdomen
    'npad':  ['#082830', '#0f4a56', '#1a7282', '#34a0aa', '#8edcd2'],   # esbozos de alas (= la madre)
    'neye':  ['#04121a', '#0a2a34', '#14505c', '#3a9a9a', '#c0f0e8'],   # ojos saltones verde azulado
    'nlab':  ['#2a2010', '#4c3a1c', '#7a6030', '#a88a4c', '#d8c088'],   # labio (máscara) ocre
    'nleg':  ['#18180c', '#2e2c16', '#4e4a26', '#76703c', '#a49c60'],
})
LAB_GLOW = ['#5a2a0e', '#9a5418', '#e08a2c', '#ffc860', '#fff0b0']    # el labio brilla en el aviso
CHARN = ['#0e0b0c', '#1c1618', '#2c2426', '#423638', '#5c4c48']
AR.MATS = list(AR.PALS); AR.MI = {m: i for i, m in enumerate(AR.MATS)}
AR.PAL_C = [[C(c) for c in AR.PALS[m]] for m in AR.MATS]
M.MI = AR.MI
MI = AR.MI

def cm(m): return lambda F, L, Z: np.full(len(F), MI[m])

def abd_mats(F, L, Z):
    m = np.full(len(F), MI['nbody'])
    m[((F + 11) / 2.6) % 1 > .74] = MI['nband']
    m[(np.abs(L) < 1.2) & (Z > 2.4)] = MI['nband']            # raya oscura del lomo
    return m

def leg(g, f0, side, fwd, phase, walk, curl, grp):
    """Pata de araña: cadera, rodilla alta, pie. walk = paso (0..1); curl = encogida (muerte)."""
    sw_ = math.sin((walk + phase) * 2 * math.pi) if walk is not None else 0
    lift = max(0, math.cos((walk + phase) * 2 * math.pi)) * 2.5 if walk is not None else 0
    c = curl
    knee = (f0 + fwd * .45 + sw_ * 1.5, side * (11 - 5 * c), 3.5 + 2 * c + lift)
    foot = (f0 + fwd + sw_ * 3.5 - fwd * .6 * c, side * (15 - 9 * c), -4 + lift + 7 * c)
    AR.tube(g, AR.bez([(f0, side * 4, -.5), knee, foot]), 1.5, .7, grp,
            lambda t: 'nband' if (t * 5) % 1 > .78 else 'nleg')

def nymph_geo(walk=None, ext=0.0, warn=0.0, bob=0.0, curl=0.0, roll=0.0, lunge=0.0):
    g = AR.Geo(); z = bob
    M.ellipsoid(g, (3, 0, z + .5), (5.5, 5, 3.8), 'thorax', cm('nbody'))
    M.ellipsoid(g, (11 + lunge, 0, z + 1 - warn), (4, 6.5, 3.5), 'head', cm('nbody'))
    for s in (-1, 1): AR.sphere(g, (12.5 + lunge, s * 5.4, z + 2.6 - warn), 2.8, f'eye{s}', 'neye')
    M.ellipsoid(g, (-12, 0, z), (11, 7, 3.6), 'abd', abd_mats)
    for s in (-1, 1):
        M.ellipsoid(g, (-5, s * 2.3, z + 3.3), (4.6, 1.6, .7), f'pad{s}', cm('npad'))
        AR.tube(g, AR.bez([(-22.5, s * 1.5, z), (-25, s * 2.2, z + .4), (-27, s * 2.5, z + .6)]), 1.1, .4, f'spk{s}', lambda t: 'nband')
    AR.tube(g, AR.bez([(-22.5, 0, z + .5), (-25.5, 0, z + 1), (-28, 0, z + 1.2)]), 1.1, .4, 'spk0', lambda t: 'nband')
    # labio: máscara plegada bajo la cabeza; ext 0..1 = sale disparado hacia delante con dos pinzas
    hx = 12 + lunge
    if ext <= 0:
        M.ellipsoid(g, (hx + 1.5, 0, z - 1.8 - warn), (3.6, 4.6, 1.1), 'lab', cm('nlab'))
    else:
        reach = 52 * ext
        tip = (hx + 4 + reach, 0, z - 2)
        AR.tube(g, AR.bez([(hx, 0, z - 2), (hx + 2 + reach * .5, 0, z - 2.5), tip]), 1.8, 1.4, 'lab', lambda t: 'nlab')
        open_ = 3.5 * (1 - abs(ext - .8) * 2) if ext > .5 else 1.5
        for s in (-1, 1):
            AR.tube(g, AR.bez([tip, (tip[0] + 4, s * (open_ + 2.5), tip[2]), (tip[0] + 7, s * open_ * .3, tip[2])]), 1.7, .6,
                    f'palp{s}', lambda t: 'nlab' if t < .6 else 'nband')
    for i, (f0, fwd) in enumerate(((7, 9), (3, 1), (-1, -9))):
        for s in (-1, 1):
            ph = (i + (s > 0)) % 2 * .5                       # trípode: 1.ª y 3.ª de un lado con la 2.ª del otro
            leg(g, f0, s, fwd, ph, walk, curl, f'leg{i}{s}')
    r = math.radians(roll); cr, sr = math.cos(r), math.sin(r)
    for i in range(len(g.P)):
        P, Nn = g.P[i], g.N[i]
        g.P[i] = np.stack([P[:, 0], P[:, 1] * cr - P[:, 2] * sr, P[:, 1] * sr + P[:, 2] * cr], 1) * K
        g.N[i] = np.stack([Nn[:, 0], Nn[:, 1] * cr - Nn[:, 2] * sr, Nn[:, 1] * sr + Nn[:, 2] * cr], 1)
    return g

def render(yaw=0.0, cell=NC, pal=None, **pose):
    p = dict(pal or {})
    if pose.get('warn', 0) > .3 and 'nlab' not in p: p['nlab'] = LAB_GLOW
    return AR.render_geo(nymph_geo(**pose), yaw, cell, pal=p or None).img()

def shadow(yaw=0.0, cell=NC, **pose):
    g = nymph_geo(**pose); P, _, _, _ = g.arrays(); P = P.copy(); P[:, 2] = 0
    xs, ys, _ = M.project(P, yaw, n=cell)
    im = Image.new('RGBA', (cell, cell), (0, 0, 0, 0)); px = im.load()
    for x, y in zip(np.floor(xs).astype(int), np.floor(ys).astype(int)):
        if 0 <= x < cell and 0 <= y < cell: px[x, y] = (10, 6, 12, 255)
    return im

def sw(i, n, k=1.0): return math.sin(i / n * 2 * math.pi * k)

ANIMS = {
    'Idle':   [dict(walk=None, bob=.3 * sw(i, 4)) for i in range(4)],
    'Move':   [dict(walk=i / 6, bob=.4 * sw(i, 6, 2)) for i in range(6)],
    'Warn':   [dict(warn=w, lunge=-l, walk=None, bob=-.6 * w) for w, l in ((.4, .5), (.8, 1.2), (1.0, 1.6))],
    'Strike': [dict(ext=e, lunge=l, walk=None) for e, l in ((.45, 1), (1.0, 2), (.7, 1.5), (.2, .5))],
    'Death':  [dict(roll=r, curl=c, walk=None, bob=-1.5 * c) for r, c in ((40, .2), (100, .45), (150, .7), (180, .9), (180, 1.0))],
}
MS = {'Idle': 200, 'Move': 80, 'Warn': 130, 'Strike': 60, 'Death': 110, 'DeathBurn': 110, 'Hurt': 80}

def death_burn(yaw=0.0):
    """Arde: se retuerce, se carboniza y queda un montoncito de ceniza con ascuas (6 cuadros, 64×64)."""
    out = []
    for i in range(6):
        t = i / 5
        if i < 4:
            pal = {m: CHARN for m in ('nbody', 'nband', 'nleg', 'nlab', 'npad')} if i >= 2 else None
            im = render(yaw, curl=min(1, .3 + t), roll=0, walk=None, bob=-1 * t, pal=pal)
        else:
            im = Image.new('RGBA', (NC, NC), (0, 0, 0, 0))
            cv = Canvas(NC)
            for y in range(NC // 2 - 3, NC // 2 + 4):          # montoncito de ceniza
                for x in range(NC // 2 - 9, NC // 2 + 10):
                    d = ((x - NC / 2) / 9) ** 2 + ((y - NC / 2 - 1) / 3.5) ** 2
                    if d < 1: cv.put(x, y, C(CHARN[1 + int((1 - d) * 3) if (x + y) % 3 else 1]))
            for j, (x, y) in enumerate(((-4, 0), (3, 1), (0, -1), (6, 0))):
                if j < 4 - (i - 4) * 2: cv.put(NC // 2 + x, NC // 2 + y, C(EMBER[2 + j % 2]))
            cv.outline(); im = cv.img()
        cv = Canvas(NC)                                          # llamas encima
        if i < 5:
            a = np.array(im.getchannel('A')); ys, xs = np.nonzero(a > 100)
            rnd = random.Random(i)
            for j in range(4 if i < 4 else 2):
                if not len(xs): break
                k = rnd.randrange(len(xs))
                AR.RX.vflame(cv, xs[k], ys[k] + 2, (10 - 2 * i) * (.6 + .4 * (j % 2)), i * 1.3 + j, w=1.8)
        im = im.copy(); im.alpha_composite(cv.img()); out.append(im)
    return out

def muestra():
    rows = {}
    for name, poses in ANIMS.items():
        cell = NS if name == 'Strike' else NC
        rows[name] = [render(0, cell=cell, **pz) for pz in poses]
    rows['Hurt'] = M.hurt(rows['Idle'][0])
    rows['DeathBurn'] = death_burn()
    rows['5 direcciones (Move)'] = [render(a, **ANIMS['Move'][1]) for a in (90, 45, 0, -45, -90)]
    big = {k: [f if f.width == NS else _pad(f) for f in v] for k, v in rows.items()}
    preview(os.path.join(OUT, 'vista_previa_6f1_ninfa_muestra.png'), big, BG, scale=3)
    # escala: junto a la rana y a su madre posada en la charca
    sc = M.burnt_floor(360, 220, 7)
    pool = M.pool_img(); sc.alpha_composite(pool, (60, 110))
    for (x, y, a, pz) in ((250, 150, 160, ANIMS['Move'][2]), (290, 100, -150, ANIMS['Idle'][0]), (205, 185, 180, ANIMS['Warn'][2])):
        M.place(sc, render(a, **pz), x, y, 0, shadow(a, **pz), 90)
    # la madre va ENCIMA de los enemigos del suelo (sus alas y su cabeza están en alto): capa propia en el código
    M.place(sc, M.SF('Meganeura_PerchRage', 0, 0, 'Torn1'), 120, 120, 8, M.SF('Meganeura_Shadow_PerchRage', 0, 0, folder='Shadows'))
    sc.alpha_composite(M.frog(), (310, 170))
    sc.resize((sc.width * 3, sc.height * 3), Image.NEAREST).convert('RGB').save(os.path.join(OUT, 'vista_previa_6f1_ninfa_escala.png'))
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    for name in ('Move', 'Strike', 'DeathBurn'):
        gif(os.path.join(OUT, 'gif', f'Ninfa_muestra_{name}.gif'), rows[name], MS[name], BG, scale=4)
    print('Listo:', OUT)

def death_shock(yaw=0.0):
    """Muere en la charca electrificada (5 cuadros, 64×64): destellos azules, se retuerce, cae de lado y se queda
    negra y humeante."""
    out = []
    poses = [dict(elec=1, roll=0, curl=.2), dict(elec=2, roll=20, curl=.4), dict(elec=1, roll=70, curl=.6),
             dict(elec=2, roll=140, curl=.85), dict(elec=0, roll=180, curl=1.0)]
    for i, pz in enumerate(poses):
        e = pz.pop('elec')
        pal = ({m: (M.ELEC if e == 1 else M.ELEC_DK) for m in AR.MATS} if e else {m: CHARN for m in ('nbody', 'nband', 'nleg', 'nlab', 'npad', 'neye')})
        im = render(yaw, walk=None, bob=-1.0 * pz['curl'], pal=pal, **pz)
        if e: M.bolts(im, seed=i * 7 + int(yaw), bright=e == 1, n=3)
        else:
            RX_ = AR.RX; cv = Canvas(NC)
            RX_.cloud(cv, [(NC / 2, NC / 2 - 8, 4, .5), (NC / 2 + 3, NC / 2 - 13, 3, .35)], ['#3a3436', '#5a5254', '#847a78', '#b0a6a0'])
            im.alpha_composite(cv.img())
        out.append(im)
    return out

def todo():
    """Hojas de la ninfa (9 filas = direcciones, como su madre) y sombras."""
    os.makedirs(os.path.join(OUT, 'Sheets'), exist_ok=True); os.makedirs(os.path.join(OUT, 'Shadows'), exist_ok=True)
    side = {}
    def save(rows, name, folder='Sheets'):
        w, h = rows[0][0].size
        im = Image.new('RGBA', (w * max(len(r) for r in rows), h * len(rows)), (0, 0, 0, 0))
        for y, r in enumerate(rows):
            for x, f in enumerate(r): im.paste(f, (x * w, y * h))
        im.save(os.path.join(OUT, folder, name + '.png'))
    hurt_rows, burn_rows = [], []
    for name, poses in ANIMS.items():
        cell = NS if name == 'Strike' else NC
        rows, srows = [], []
        for dname, yaw in M.DIRS:
            rows.append([render(yaw, cell=cell, **pz) for pz in poses]); srows.append([shadow(yaw, cell=cell, **pz) for pz in poses])
            if dname == 'Side': side[name] = rows[-1]
            if name == 'Idle': hurt_rows.append(M.hurt(rows[-1][0]))
        save(rows, f'Meganeura_Nymph_{name}'); save(srows, f'Meganeura_Nymph_Shadow_{name}', 'Shadows')
        print('  ', name, flush=True)
    save(hurt_rows, 'Meganeura_Nymph_Hurt')
    for dname, yaw in M.DIRS: burn_rows.append(death_burn(yaw))
    save(burn_rows, 'Meganeura_Nymph_DeathBurn')
    side['Hurt'] = hurt_rows[4]; side['DeathBurn'] = burn_rows[4]
    preview(os.path.join(OUT, 'vista_previa_6f1_ninfa_animaciones.png'), {k: [f if f.width == NS else _pad(f) for f in v] for k, v in side.items()}, BG, scale=3)
    dirs = {f'{n} (9 direcciones)': [Image.open(os.path.join(OUT, 'Sheets', f'Meganeura_Nymph_{n}.png')).crop((0, r * NC, NC, (r + 1) * NC)) for r in range(9)]
            for n in ('Move', 'Warn')}
    preview(os.path.join(OUT, 'vista_previa_6f1_ninfa_direcciones.png'), dirs, BG, scale=3)
    for name, fr in side.items(): gif(os.path.join(OUT, 'gif', f'Meganeura_Nymph_{name}.gif'), fr, MS[name], BG, scale=4)
    print('Listo:', OUT)

def _pad(f):
    o = Image.new('RGBA', (NS, NS), (0, 0, 0, 0)); o.alpha_composite(f, ((NS - f.width) // 2, (NS - f.height) // 2)); return o

if __name__ == '__main__':
    if 'muestra' in sys.argv[1:]: muestra()
    elif 'shock' in sys.argv[1:]:
        rows = [death_shock(yaw) for _, yaw in M.DIRS]
        im = Image.new('RGBA', (NC * 5, NC * 9), (0, 0, 0, 0))
        for y, r in enumerate(rows):
            for x, f in enumerate(r): im.paste(f, (x * NC, y * NC))
        im.save(os.path.join(OUT, 'Sheets', 'Meganeura_Nymph_DeathShock.png'))
        preview(os.path.join(OUT, 'vista_previa_6f2a_ninfa_shock.png'), {'DeathShock (Side)': rows[4], 'DeathShock (Dn45)': rows[6]}, BG, scale=4)
        gif(os.path.join(OUT, 'gif', 'Meganeura_Nymph_DeathShock.gif'), rows[4], 90, BG, scale=4)
        print('   shock')
    elif 'todo' in sys.argv[1:]: todo()
    else: print(__doc__)
