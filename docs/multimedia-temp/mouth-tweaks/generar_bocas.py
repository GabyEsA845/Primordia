"""
generar_bocas.py - Primordia: ajustes de las Bocas (rama feature/mouth-tweaks).

  Mouth/       Mouth_Stinger_Idle/Attack   Aguijón a 2/3 (se copia también a evolutions/Mouth,
                                           de donde lo importa Unity)
  Cap/         Cap_Spores_<Idle|Move>_<side|down|up>
                                           gorrito de Amanita de las Esporas: 24x24 con el centro
                                           de la célula en (12, 12); sigue el balanceo de la célula
  Projectiles/ Projectile_Spore            espora crema con borde rojizo (10x10)
  Icons/       Icon_Spores_16/32           gorrito con esporas

Reutiliza generar_evoluciones.py (y, a través de él, la célula, las paletas y el contorno).

Uso:
  python generar_bocas.py [carpeta_multimedia_temp] [carpeta_salida]
"""
import importlib.util
import math
import os
import shutil
import sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, '..')
OUT = sys.argv[2] if len(sys.argv) > 2 else HERE

_argv = sys.argv
sys.argv = ['generar_evoluciones.py', ROOT, os.devnull]
_spec = importlib.util.spec_from_file_location('generar_evoluciones', os.path.join(ROOT, 'evolutions', 'generar_evoluciones.py'))
E = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(E)
sys.argv = _argv
M = E.M
U = E.U

C, CA, OUTL = M.C, M.CA, M.OUTL
Canvas, shade_v, ramp_idx, geom = M.Canvas, M.shade_v, M.ramp_idx, M.geom
Cv = U.Cv
FACINGS, FRAMES = M.FACINGS, M.FRAMES
BG = C('#16352e')

# Amanita: rojo con lunares blancos y láminas crema por debajo.
CAP = ['#6a1420', '#b02a2c', '#e8503a', '#ff9070']
DOT = ['#d8c4a8', '#fff4e0']
GILL = ['#c8a888', '#f0dcc0']
# Espora: crema con borde rojizo (las de los enemigos son rosas).
SPORE = ['#a84a3a', '#e8c49a', '#fff0d0', '#ffffff']

N = 24                       # lienzo del gorrito: el de la Piel, con el centro de la célula en (12, 12)
CAP_RX, CAP_RY = 6.8, 5.0    # media anchura y altura de la cúpula
SINK = 1.4                   # cuánto se mete la base en la cabeza (tapa el borde de arriba)

# Lunares (x, y) en la cúpula normalizada: x de -1 a 1, y de -1 (arriba) a 0 (base).
DOTS = {
    'down': [(-0.45, -0.55), (0.4, -0.6), (0.0, -0.2), (-0.85, -0.15), (0.85, -0.15)],
    'side': [(-0.3, -0.6), (0.45, -0.5), (-0.8, -0.18), (0.1, -0.15)],
    'up': [(-0.4, -0.55), (0.4, -0.5), (0.0, -0.2)],
}


def cap(facing, p, squash_extra=0.0, n=N):
    """Gorrito sobre la célula de ese cuadro (p: parámetros del cuadro del jugador)."""
    cv = Canvas(n)
    cx, cy, rx, ry = geom(n, 6.5, 6.0, p['squash'], p['bob'], p.get('grow', 0))
    s = p['squash'] + squash_extra
    w = CAP_RX * (1 + 0.06 * s)
    h = CAP_RY * (1 - 0.08 * s)
    # La inercia (lean) mueve la parte alta de la célula: el gorrito va con ella.
    bx = cx + p.get('lean', 0.0) * 0.8
    if facing == 'side':
        bx += 0.6                                    # algo adelantado, hacia donde mira
    by = cy - ry + SINK                              # base de la cúpula
    for y in range(n):
        for x in range(n):
            nx = (x + .5 - bx) / w
            ny = (y + .5 - by) / h
            if ny > 0.05 or nx * nx + ny * ny > 1:
                continue
            cv.px[(x, y)] = C(CAP[ramp_idx(shade_v(nx, ny * 1.2 + 0.25))])
    # lunares de 1 o 2 px, con un píxel crema a la sombra
    for (dx, dy) in DOTS[facing]:
        x, y = bx + dx * w * 0.86, by + dy * h * 0.95
        big = abs(dx) < 0.6 and dy < -0.35
        cv.put_in(x - .5, y - .5, DOT[1])
        if big:
            cv.put_in(x + .5, y - .5, DOT[1])
            cv.put_in(x - .5, y + .5, DOT[0])
            cv.put_in(x + .5, y + .5, DOT[0] if facing != 'up' else DOT[1])
    # brillo arriba a la izquierda
    cv.put_in(bx - w * 0.45 - .5, by - h * 0.7 - .5, CAP[3])
    # láminas: una franja crema bajo el ala (no se ven desde detrás)
    if facing != 'up':
        gy = int(math.floor(by + 0.05))
        for x in range(int(bx - w + 1.5), int(bx + w - 0.5)):
            if (x, gy - 1) in cv.px:
                cv.put(x, gy, GILL[(x + gy) % 2] if facing == 'down' else GILL[0])
    cv.outline()
    return cv


def cap_anims():
    out = {}
    for anim in ('Idle', 'Move'):
        for facing in FACINGS:
            out[f'{anim}_{facing}'] = [cap(facing, p) for p in FRAMES[f'{anim}_{facing}']]
    return out


def spore(f):
    """Espora suelta (10x10, centro 5,5): late despacio y un brillo da la vuelta."""
    cv = Cv(10)
    r = (2.3, 2.6, 2.8, 2.6)[f]
    for y in range(10):
        for x in range(10):
            nx, ny = (x + .5 - 5) / r, (y + .5 - 5) / r
            d = nx * nx + ny * ny
            if d > 1:
                continue
            i = 0 if d > 0.62 else ramp_idx(shade_v(nx, ny)) if d > 0.2 else 2
            cv.put(x, y, SPORE[max(i, 1) if d <= 0.62 else 0])
    gx, gy = ((4, 4), (5, 4), (5, 5), (4, 5))[f]
    cv.put_in(gx, gy, SPORE[3])
    cv.outline(170)
    return cv


def icon_spores(n):
    """Gorrito grande con tres esporas que salen hacia la derecha."""
    k = n / 16
    cv = Cv(n)
    bx, by, w, h = 7.0 * k, 10.5 * k, 6.2 * k, 6.0 * k
    for y in range(n):
        for x in range(n):
            nx, ny = (x + .5 - bx) / w, (y + .5 - by) / h
            if ny > 0.05 or nx * nx + ny * ny > 1:
                continue
            cv.put(x, y, CAP[ramp_idx(shade_v(nx, ny * 1.2 + 0.25))])
    for (dx, dy, big) in ((-0.45, -0.5, True), (0.2, -0.75, False), (0.5, -0.35, True), (-0.85, -0.12, False), (0.0, -0.2, False)):
        x, y = bx + dx * w * 0.86, by + dy * h * 0.9
        cv.put_in(x - .5, y - .5, DOT[1])
        if big and n >= 32:
            cv.put_in(x + .5, y - .5, DOT[1]); cv.put_in(x - .5, y + .5, DOT[0]); cv.put_in(x + .5, y + .5, DOT[0])
    for x in range(int(bx - w + 1.5), int(bx + w - 0.5)):
        cv.put(x, int(by + 0.05), GILL[(x % 2)])
    # pie corto
    for y in range(int(by + 1.05), int(by + 1.05 + 3 * k)):
        for x in range(int(bx - 1.5 * k), int(bx + 1.5 * k)):
            cv.put(x, y, GILL[1] if x < bx else GILL[0])
    for (sx, sy, r) in ((12.6, 4.0, 1.4), (14.0, 8.0, 1.1), (12.0, 12.6, 1.2)):
        sx, sy, r = sx * k, sy * k, r * k
        for y in range(n):
            for x in range(n):
                if (x + .5 - sx) ** 2 + (y + .5 - sy) ** 2 <= r * r:
                    cv.put(x, y, SPORE[2] if (x + .5 - sx) + (y + .5 - sy) < 0 else SPORE[1])
    cv.outline()
    return cv


# ================================================================ guardar
SHEETS = {}


def save(group, name, frames, ms=None, loop=False):
    frames = [f if isinstance(f, Image.Image) else f.img() for f in frames]
    w, h = frames[0].size
    os.makedirs(os.path.join(OUT, group), exist_ok=True)
    sh = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, im in enumerate(frames):
        sh.paste(im, (i * w, 0))
    sh.save(os.path.join(OUT, group, name + '.png'))
    if len(frames) > 1:
        os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
        sc = max(1, min(10, 240 // max(w, h)))
        out = []
        for im in frames:
            b = Image.new('RGBA', (w, h), BG)
            b.alpha_composite(im)
            out.append(b.resize((w * sc, h * sc), Image.NEAREST).convert('RGB'))
        out[0].save(os.path.join(OUT, 'gif', name + '.gif'), save_all=True, append_images=out[1:],
                    duration=ms or 100, loop=0)
    SHEETS[name] = frames


def build():
    st = E.stinger_anims()
    tip = st['Idle'][0].tip
    print(f'Punta del Aguijón: {tip[0]:.1f} px desde la unión')
    save('Mouth', 'Mouth_Stinger_Idle', st['Idle'], 180, True)
    save('Mouth', 'Mouth_Stinger_Attack', st['Attack'], 70)
    ca = cap_anims()
    for name, frames in ca.items():
        save('Cap', 'Cap_Spores_' + name, frames, M.MS[name.split('_')[0]], True)
    save('Projectiles', 'Projectile_Spore', [spore(f) for f in range(4)], 120, True)
    save('Icons', 'Icon_Spores_16', [icon_spores(16)])
    save('Icons', 'Icon_Spores_32', [icon_spores(32)])
    return tip


# ============================================================ vista previa
def sheet_frames(rel, i=None):
    im = Image.open(os.path.join(ROOT, rel)).convert('RGBA')
    h = im.height
    frames = [im.crop((k * h, 0, k * h + h, h)) for k in range(im.width // h)]
    return frames if i is None else frames[i]


def at(scr, im, cx, cy):
    scr.alpha_composite(im, (int(round(cx - im.width / 2)), int(round(cy - im.height / 2))))


# Altura de la cabeza de cada Piel respecto a la célula, en px (la usa MutationLook).
SKINS = [(None, 'player/Player_{}.png', 0), ('Caparazón', 'evolutions/Skin/Skin_Carapace_{}.png', 2),
         ('P. urticante', 'mutations/Skin/Skin_StingingSkin_{}.png', 2), ('Camuflaje', 'floor1-mutations/Skin/Skin_Camouflage_{}.png', 0)]


def preview(path):
    S = 4
    cols = 12
    cw = 30
    rows = []
    # 1. gorrito en reposo y moviéndose, sobre la célula, en las 3 direcciones
    for anim in ('Idle', 'Move'):
        for facing in FACINGS:
            body = sheet_frames(f'player/Player_{anim}_{facing}.png')
            caps = SHEETS[f'Cap_Spores_{anim}_{facing}']
            row = Image.new('RGBA', (cols * cw, cw), BG)
            for i, (b, c) in enumerate(zip(body, caps)):
                at(row, b, i * cw + cw / 2, cw / 2)
                at(row, c, i * cw + cw / 2, cw / 2)
                # la misma, de espaldas a la izquierda (volteada) en lateral
                if facing == 'side':
                    at(row, b.transpose(Image.FLIP_LEFT_RIGHT), (i + 5) * cw + cw / 2, cw / 2)
                    at(row, c.transpose(Image.FLIP_LEFT_RIGHT), (i + 5) * cw + cw / 2, cw / 2)
            rows.append((f'{anim} {facing}', row))
    # 2. con cada Piel (reposo, mirando abajo y de lado)
    row = Image.new('RGBA', (cols * cw, cw), BG)
    for k, (name, rel, lift) in enumerate(SKINS):
        for j, facing in enumerate(('down', 'side')):
            b = sheet_frames(rel.format(f'Idle_{facing}'), 0)
            c = SHEETS[f'Cap_Spores_Idle_{facing}'][0]
            x = (k * 3 + j) * cw + cw / 2
            at(row, b, x, cw / 2)
            at(row, c, x, cw / 2 - lift)
    rows.append(('con cada Piel', row))
    # 3. ataque: el cuerpo se adelanta 2 px y el gorrito se aplasta y se hincha (lo hace el código)
    row = Image.new('RGBA', (cols * cw, cw), BG)
    b0 = sheet_frames('player/Player_Idle_side.png', 0)
    c0 = SHEETS['Cap_Spores_Idle_side'][0]
    for i, (dx, sx, sy) in enumerate(((0, 1, 1), (1, 1.1, 0.8), (2, 0.95, 1.2), (1, 1, 1.05), (0, 1, 1))):
        x = i * cw + cw / 2
        at(row, b0, x + dx, cw / 2)
        cw2, ch2 = max(1, round(c0.width * sx)), max(1, round(c0.height * sy))
        c = c0.resize((cw2, ch2), Image.NEAREST)
        # escala desde la base del gorrito (fila 8 del lienzo)
        at(row, c, x + dx, cw / 2 + (8 - 12) * (1 - sy))
        if i == 2:
            for k, ang in enumerate((-30, -15, 0, 15, 30)):
                a = math.radians(ang)
                at(row, SHEETS['Projectile_Spore'][k % 4], x + dx + 9 + math.cos(a) * 3, cw / 2 - 3 + math.sin(a) * 3)
    for k in range(4):
        at(row, SHEETS['Projectile_Spore'][k], (6 + k) * cw + cw / 2, cw / 2)
    old_spore = os.path.join(SCRATCH_OLD_SPORES, 'Projectile_Spore.png')
    if os.path.exists(old_spore):
        im = Image.open(old_spore).convert('RGBA')
        at(row, im.crop((0, 0, im.height, im.height)), 11 * cw, cw / 2)
    rows.append(('ataque y espora', row))
    # 4. Aguijón: antes y ahora, sobre la célula
    row = Image.new('RGBA', (cols * cw * 2, cw * 2), BG)
    old = os.path.join(SCRATCH_OLD, 'Mouth_Stinger_Idle.png') if SCRATCH_OLD else None
    pairs = []
    if old and os.path.exists(old):
        im = Image.open(old).convert('RGBA')
        pairs.append(('antes', [im.crop((k * 24, 0, k * 24 + 24, 24)) for k in range(im.width // 24)]))
    pairs.append(('ahora', SHEETS['Mouth_Stinger_Idle'] + SHEETS['Mouth_Stinger_Attack']))
    body = sheet_frames('player/Player_Idle_side.png', 0)
    for r, (_, frames) in enumerate(pairs):
        for i, m in enumerate(frames):
            x = i * cw * 1.5 + cw
            y = r * cw + cw / 2
            at(row, body, x, y)
            row.alpha_composite(m, (int(x + 6), int(y - 12)))
    rows.append(('Aguijón antes / ahora', row))
    # 5. iconos
    row = Image.new('RGBA', (cols * cw, 40), BG)
    old16 = os.path.join(SCRATCH_OLD_SPORES, 'Icon_Spores_16.png')
    old32 = os.path.join(SCRATCH_OLD_SPORES, 'Icon_Spores_32.png')
    x = 4
    for p in (old16, old32):
        if os.path.exists(p):
            im = Image.open(p).convert('RGBA')
            row.alpha_composite(im, (x, 4)); x += im.width + 8
    x += 20
    for name in ('Icon_Spores_16', 'Icon_Spores_32'):
        im = SHEETS[name][0]
        row.alpha_composite(im, (x, 4)); x += im.width + 8
    rows.append(('iconos antes / ahora', row))

    lab = 22
    W = max(r.width for _, r in rows) * S + 20
    H = sum(r.height * S + lab + 8 for _, r in rows) + 10
    out = Image.new('RGBA', (W, H), BG)
    y = 8
    for name, r in rows:
        E.G.ptext(out, (10, y + 4), name.upper(), (214, 255, 154), scale=2)
        y += lab
        out.alpha_composite(r.resize((r.width * S, r.height * S), Image.NEAREST), (10, y))
        y += r.height * S + 8
    out.convert('RGB').save(path)


# Copias de lo anterior para la comparación de la vista previa (opcional).
SCRATCH_OLD = os.environ.get('PRIMORDIA_OLD_STINGER', '')
SCRATCH_OLD_SPORES = os.environ.get('PRIMORDIA_OLD_SPORES', '')


def main():
    tip = build()
    preview(os.path.join(OUT, 'vista_previa_bocas.png'))
    # Unity importa el Aguijón desde la carpeta de evoluciones; la espora y los iconos se copian también a sus
    # carpetas de siempre, para que los menús antiguos no vuelvan a poner los de antes.
    copies = [('Mouth', name, 'evolutions/Mouth') for name in ('Mouth_Stinger_Idle', 'Mouth_Stinger_Attack')]
    copies += [('Projectiles', 'Projectile_Spore', 'projectiles'), ('Icons', 'Icon_Spores_16', 'ui/Icons'),
               ('Icons', 'Icon_Spores_32', 'ui/Icons')]
    for group, name, target in copies:
        shutil.copyfile(os.path.join(OUT, group, name + '.png'), os.path.join(ROOT, target, name + '.png'))
    for k, v in SHEETS.items():
        print(k, len(v), f'{v[0].width}x{v[0].height}')


if __name__ == '__main__':
    main()
