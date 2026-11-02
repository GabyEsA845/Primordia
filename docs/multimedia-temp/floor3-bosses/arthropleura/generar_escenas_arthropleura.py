"""Escenas de prueba del Arthropleura (GIF a tamaño de cámara, 480×270 ×2): combinan las piezas de la cadena, sus
capas (6a/6b) y los efectos de la 6c-1 sobre salas del Bosque humeante. Solo para revisar; no se importan.

1. F1, presentación (claro de 28×16 cerrado, cámara ×1,3): el bulto recorre la espesura, el borde revienta y la
   cabeza entra por la brecha y se alza.
2. F2, ondas: pisotones que sueltan 3 ondas en semicírculo hacia la rana, que se aparta.
3. F2, embestida: aviso, embestida con polvo y choque contra una Sigillaria, que cae.
4. Fuego: la rana revienta un odre, el fuego salta de pieza en pieza y las piezas quemadas abren las placas.

Uso: python generar_escenas_arthropleura.py
"""
import math, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
_argv = sys.argv; sys.argv = sys.argv[:1]
import generar_arthropleura as A
import generar_escala_arthropleura as SC
sys.argv = _argv
from PIL import Image

MT = A.MT
CAMW, CAMH = 480, 270
SE = math.sin(A.ELEV)
OUT = os.path.join(HERE, 'escenas')

def sheet_frame(path, k, w, h, row=0):
    im = Image.open(path).convert('RGBA')
    return im.crop((k * w, row * h, k * w + w, row * h + h))

FX = os.path.join(HERE, 'Effects')
def fx(name, k, w, h): return sheet_frame(os.path.join(FX, name + '.png'), k, w, h)
FROG = sheet_frame(os.path.join(MT, 'floor3-bodies', 'Frog', 'Player_Frog_Idle_side.png'), 0, 32, 32)
FROG_MOVE = [sheet_frame(os.path.join(MT, 'floor3-bodies', 'Frog', 'Player_Frog_Move_side.png'), k, 32, 32) for k in range(4)]

def put(im, spr, x, y, ax=.5, ay=.5):
    im.alpha_composite(spr, (int(x - spr.width * ax), int(y - spr.height * ay)))

class Path:
    """Polilínea en pantalla; las distancias se miden en el suelo (la vertical va encogida por sen 55°)."""
    def __init__(s, pts):
        s.p = pts; s.acc = [0.0]
        for (ax, ay), (bx, by) in zip(pts, pts[1:]): s.acc.append(s.acc[-1] + math.hypot(bx - ax, (by - ay) / SE))
    @property
    def length(s): return s.acc[-1]
    def at(s, d):
        d = max(0.0, min(s.acc[-1] - 1e-3, d))
        for i in range(len(s.p) - 1):
            if s.acc[i + 1] >= d:
                (ax, ay), (bx, by) = s.p[i], s.p[i + 1]
                t = (d - s.acc[i]) / max(1e-6, s.acc[i + 1] - s.acc[i])
                return ax + (bx - ax) * t, ay + (by - ay) * t, math.degrees(math.atan2(-(by - ay) / SE, bx - ax))
        return s.p[-1] + (0,)

def chain(im, path, head_d, f, cache, n=28, state=None, visible=None, head_pose=None):
    """Dibuja la cadena con la cabeza a head_d sobre path (mira hacia donde crece d). state(i) -> (pose, capas)."""
    items = []
    for i in range(n + 2):
        x, y, a = path.at(head_d - i * A.STEP_PX)
        kind = 'head' if i == 0 else 'tail' if i == n + 1 else 'seg'
        if visible and not visible(x, y): continue
        if kind == 'head': pose, layers = dict(head_pose or A.HEAD_ANIMS['Move'][f % 6], _k=f), ()
        else: pose, layers = dict(A.SEG_ANIMS['Walk'][(f + i) % 6], _k=f), ()
        if state and kind == 'seg':
            r = state(i)
            if r: pose, layers = r
        items.append((i, x, y, A.piece_states(kind, a, cache, pose, layers)))
    for i, x, y, spr in sorted(items, key=lambda t: -t[0]): put(im, spr, x, y)

def save_gif(name, frames, ms=80):
    os.makedirs(OUT, exist_ok=True)
    big = [f.resize((f.width * 2, f.height * 2), Image.NEAREST).convert('RGB') for f in frames]
    big[0].save(os.path.join(OUT, name + '.gif'), save_all=True, append_images=big[1:], duration=ms, loop=0)
    big[len(big) // 2].save(os.path.join(OUT, name + '.png'))
    print('  ', name, len(frames), 'cuadros')

# =================================================================================================
import numpy as np
BAYER4 = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]], float)

def coverage_map(rows, blur=8):
    """Cuánto 'claro' hay en cada píxel (0 = dentro de la espesura, 1 = suelo), con una banda suave de ~1 u."""
    H, W = len(rows), len(rows[0])
    m = np.array([[0.0 if c == '#' else 1.0 for c in r] for r in rows]).repeat(16, 0).repeat(16, 1)
    k = 2 * blur + 1
    for ax in (0, 1):                                                     # desenfoque de caja (dos pasadas)
        c = np.cumsum(np.pad(m, [(blur + 1, blur) if a == ax else (0, 0) for a in (0, 1)], mode='edge'), axis=ax)
        m = ((np.take(c, range(k, c.shape[ax]), axis=ax) - np.take(c, range(0, c.shape[ax] - k), axis=ax)) / k)
    return m

def put_dither(im, spr, x, y, cov):
    """Pega la pieza hundiéndola con trama donde entra en la espesura (fundido de 1 u)."""
    a = np.array(spr); h, w = a.shape[:2]
    x0, y0 = int(x - w / 2), int(y - h / 2)
    ys, xs = np.mgrid[y0:y0 + h, x0:x0 + w]
    inb = (ys >= 0) & (xs >= 0) & (ys < cov.shape[0]) & (xs < cov.shape[1])
    cv_ = np.zeros((h, w)); cv_[inb] = cov[ys[inb], xs[inb]]
    thr = (BAYER4[ys % 4, xs % 4] + .5) / 16
    a[..., 3] = np.where(cv_ >= thr, a[..., 3], 0)
    im.alpha_composite(Image.fromarray(a), (x0, y0))

RIDGE = {}
def ridge_sprite(ang, k, cap=False):
    """Tramo de cresta (o remate) mirando a ang, de las hojas de la 6c (fila por dirección; Flip X a la izquierda)."""
    q = round(ang / 22.5) * 22.5; q = (q + 180) % 360 - 180
    flip = abs(q) > 90
    base = (180 - q if q > 0 else -180 - q) if flip else q
    row = [d for _, d in A.DIRS].index(base)
    key = (cap, row, k)
    if key not in RIDGE:
        name = 'Arthropleura_CanopyRidge_Cap' if cap else 'Arthropleura_CanopyRidge'
        RIDGE[key] = sheet_frame(os.path.join(HERE, 'Sheets', name + '.png'), k, 112, 112, row)
    im = RIDGE[key]
    return im.transpose(Image.FLIP_LEFT_RIGHT) if flip else im

def put_dither_inv(im, spr, x, y, cov):
    """La cresta solo sobre la espesura: máscara tramada invertida."""
    a = np.array(spr); h, w = a.shape[:2]
    x0, y0 = int(x - w / 2), int(y - h / 2)
    ys, xs = np.mgrid[y0:y0 + h, x0:x0 + w]
    inb = (ys >= 0) & (xs >= 0) & (ys < cov.shape[0]) & (xs < cov.shape[1])
    cv_ = np.ones((h, w)); cv_[inb] = cov[ys[inb], xs[inb]]
    thr = (BAYER4[ys % 4, xs % 4] + .5) / 16
    a[..., 3] = np.where(cv_ < thr, a[..., 3], 0)
    im.alpha_composite(Image.fromarray(a), (x0, y0))

def escena_f1():
    """Claro de 28×16 u CERRADO (espesura de 6-7 u), cámara ×1,3: el bulto recorre la espesura, el borde de arriba
    revienta y la cabeza entra por la brecha y se alza."""
    W, H = 42, 30
    floor = (7, 7, 34, 22)
    closed = SC.rect_room(W, H, floor, mud=[(10, 19, 12, 20), (30, 9, 31, 10)])
    bx0, bx1 = 18, 22                                                     # brecha de 5 u en el borde de arriba
    breached = SC.rect_room(W, H, floor, mud=[(10, 19, 12, 20), (30, 9, 31, 10)], gaps=[(bx0, 0, bx1, 6)])
    spots = [(11, 10, 'fern'), (31, 13, 'fern'), (14, 21, 'fern'), (28, 21, 'fern'), (26, 10.5, 'sac'), (9.5, 14, 'sac'),
             (21, 21.5, 'pool'), (33, 18, 'fern')]
    bases = {}
    for key, rows in (('closed', closed), ('breach', breached)):
        b = SC.room(rows); SC.props(b, spots, 3); bases[key] = (b, coverage_map(rows))
    breach_im = Image.open(os.path.join(HERE, 'Smoke', 'Breach_Top.png')).convert('RGBA')
    bulge = [fx('Fx_Arthropleura_CanopyBulge', k, 64, 48) for k in range(6)]
    edge = [fx('Fx_Arthropleura_EdgeBreak', k, 96, 80) for k in range(8)]
    ring = [(v + .5) * 16 for v in (4.0, 4.0, 37.0, 25.0)]
    rx0, ry0, rx1, ry1 = ring
    bxc = (bx0 + bx1 + 1) / 2 * 16
    path = Path([(rx0, ry0), (rx0, ry1), (rx1, ry1), (rx1, ry0), (bxc, ry0), (bxc, 14.0 * 16)])
    s_break = Path([(rx0, ry0), (rx0, ry1), (rx1, ry1), (rx1, ry0), (bxc, ry0)]).length
    zoom = 1.3
    cw, ch = int(CAMW * zoom), int(CAMH * zoom)
    cam = (int(21 * 16 - cw / 2), int(15 * 16 - ch / 2))
    frames, cache = [], {}
    F_BREAK, F_IN, F_REAR = 30, 33, 44
    for f in range(58):
        key = 'closed' if f < F_IN else 'breach'
        base, cov = bases[key]
        im = base.copy()
        if f >= F_IN: put(im, breach_im, bxc, 1.5 * 16, .5, 0)
        if f < 26: head_d = s_break - (26 - f) * 15                           # repta por la espesura
        elif f < F_IN: head_d = s_break                                       # se para junto al borde
        else: head_d = min(path.length, s_break + (f - F_IN) * 16)           # entra por la brecha
        pose = A.HEAD_ANIMS['Rear'][min(5, (f - F_REAR) // 2)] if f >= F_REAR else None
        items = []
        for i in range(30):
            x, y, a = path.at(head_d - i * A.STEP_PX)
            kind = 'head' if i == 0 else 'tail' if i == 29 else 'seg'
            hp = dict(pose or A.HEAD_ANIMS['Move'][f % 6], _k=f)
            pz = hp if kind == 'head' else dict(A.SEG_ANIMS['Walk'][(f + i) % 6], _k=f)
            items.append((i, x, y, A.piece_states(kind, a, cache, pz, ())))
        for i, x, y, spr in sorted(items, key=lambda t: -t[0]): put_dither(im, spr, x, y, cov)
        angs = {i: path.at(head_d - i * A.STEP_PX)[2] for i in range(30)}
        for i, x, y, spr in sorted(items, key=lambda t: -t[0]):            # cresta continua encima de la copa
            c = cov[int(min(cov.shape[0] - 1, max(0, y))), int(min(cov.shape[1] - 1, max(0, x)))]
            if c > .9: continue
            k = (f // 2) % 4
            put_dither_inv(im, ridge_sprite(angs[i], k), x, y, cov)
            if i == 29: put_dither_inv(im, ridge_sprite(angs[i] + 180, k, cap=True), x, y, cov)   # remate de la cola
            if i == 0: put_dither_inv(im, ridge_sprite(angs[i], k, cap=True), x, y, cov)          # remate de la cabeza
        if 24 <= f < F_BREAK:                                                 # el borde tiembla
            put(im, fx('Fx_Arthropleura_CanopyShake', f % 6, 32, 40), bxc - 20, 6 * 16 - 10)
            put(im, fx('Fx_Arthropleura_CanopyShake', (f + 3) % 6, 32, 40), bxc + 20, 6 * 16 - 10)
        if F_BREAK <= f < F_BREAK + 8: put(im, edge[f - F_BREAK], bxc, 7 * 16 - 20, .5, 0)
        if f < F_BREAK + 4:
            for (tx, ty) in ((230, 210), (330, 260), (420, 235), (280, 300), (390, 300), (180, 290)):
                put(im, fx('Fx_Arthropleura_Tremor', (f + tx) % 4, 48, 24), tx, ty)
        put(im, FROG, 21 * 16, 17.5 * 16)
        frames.append(im.crop((cam[0], cam[1], cam[0] + cw, cam[1] + ch)))
    save_gif('Arthropleura_F1_presentacion', frames, 90)

def floor_room(seed=4):
    return A.floor_bg(CAMW, CAMH, seed)

def escena_ola():
    """Pisotones que sueltan 3 ondas expansivas en semicírculo hacia la rana (0,3 s entre ellas)."""
    import generar_efectos_arthropleura as EF
    base = floor_room(4)
    path = Path([(-60, 40), (100, 60), (240, 40), (380, 70), (560, 50)])
    frames, cache = [], {}
    frog = [230.0, 215.0]
    stomps = [(2, 4), (16, 9)]                                                # (cuadro, segmento que pisa)
    for f in range(42):
        im = base.copy()
        head_d = path.length - 60
        layer = EF.Canvas(CAMW, CAMH)
        for f0, seg in stomps:
            sx, sy, _ = path.at(head_d - seg * A.STEP_PX); sy += 22
            if f0 <= f < f0 + 5: put(im, fx('Fx_Arthropleura_Stomp', f - f0, 32, 24), sx, sy)
            aim = math.atan2((frog[1] - sy) / EF.SE_, frog[0] - sx)
            for w in range(3):                                                  # 3 crestas, 0,3 s (4 cuadros) entre ellas
                age = f - f0 - 2 - w * 4
                if age < 0: continue
                r = 10 + age * 9
                life = max(0.0, 1 - r / 230)
                if life > 0: EF.shock_arc(layer, sx, sy, r, aim, life=life, k=f)
        im.alpha_composite(layer.img())                                         # las ondas, en el suelo
        chain(im, path, head_d, f, cache)
        if 9 <= f < 15 or 22 <= f < 28: frog[0] += 6                         # la rana se aparta entre ondas
        put(im, FROG_MOVE[f % 4] if (9 <= f < 15 or 22 <= f < 28) else FROG, frog[0], frog[1])
        frames.append(im)
    save_gif('Arthropleura_F2_ola', frames, 80)

def escena_embestida():
    base = floor_room(6)
    tree_x, tree_y = 380, 150
    fall = [sheet_frame(os.path.join(HERE, 'Smoke', 'Sigillaria_Fall.png'), k, 112, 80) for k in range(9)]
    path = Path([(-900, 150), (60, 150), (tree_x - 40, 150)])
    frames, cache = [], {}
    d0 = path.length - (tree_x - 40 - 60)                                        # cabeza en x = 60
    for f in range(40):
        im = base.copy()
        if f < 10: head_d = d0; pose = A.HEAD_ANIMS['ChargeWarn'][min(3, f // 2)]
        elif f < 20: head_d = d0 + (f - 10) * 26; pose = A.HEAD_ANIMS['Charge'][(f - 10) % 4]
        else: head_d = d0 + 10 * 26; pose = A.HEAD_ANIMS['Open'][min(5, (f - 20) // 2)]
        hit = f >= 19
        k_fall = 0 if not hit else min(8, (f - 19))
        tree = fall[k_fall]
        put(im, tree, tree_x, tree_y, 28 / 112, 70 / 80)
        if f < 10:                                                              # aviso: tramo repetido
            for x in range(70, tree_x - 50, 16):
                im.alpha_composite(fx('Fx_Arthropleura_ChargeAim', f % 4, 16, 8), (x, 146))
        chain(im, path, head_d, f, cache, head_pose=pose)
        if 10 <= f < 16:
            hx, hy, _ = path.at(head_d)
            put(im, fx('Fx_Arthropleura_ChargeDust', f - 10, 48, 32), hx - 60, hy + 6)
        put(im, FROG, 300, 235)
        frames.append(im)
    save_gif('Arthropleura_F2_embestida', frames, 80)

def escena_fuego():
    base = floor_room(8)
    path = Path([(-700, 120), (40, 120), (300, 110), (520, 130)])
    sac = [sheet_frame(os.path.join(MT, 'floor3-reactions', 'Smoke', 'Methane_Sac_Burst.png'), k, 32, 32) for k in range(5)]
    sac_idle = sheet_frame(os.path.join(MT, 'floor3-environment', 'Smoke', 'props', 'Methane_Sac_Idle.png'), 0, 24, 26)
    expl = [sheet_frame(os.path.join(MT, 'floor3-mutations', 'Effects', 'Fx_MethaneSac_Explode.png'), k, 64, 64) for k in range(8)]
    ign = [sheet_frame(os.path.join(MT, 'floor3-reactions', 'Effects', 'Fx_Ignite.png'), k, 16, 16) for k in range(4)]
    frames, cache = [], {}
    head_d = path.length - 40
    sac_x, sac_y = 230, 170
    lit = {}                                                                     # pieza -> cuadro en que prende
    start = 8
    for k, i in enumerate((8, 7, 9, 6, 10)): lit[i] = start + 3 + k * 3
    def state_for(f):
        def st(i):
            if i not in lit or f < lit[i]: return None
            age = f - lit[i]
            walk = A.SEG_ANIMS['Walk'][(f + i) % 6]
            if age < 10: return dict(walk, _k=f), ('Burning',)
            if age < 14: return dict(A.OPEN_ANIMS['PlatesOpen'][age - 10], _k=f), ('Burning',)
            return dict(A.OPEN_ANIMS['OpenWalk'][(f + i) % 6], _k=f), ()
        return st
    for f in range(44):
        im = base.copy()
        if f < start: put(im, sac_idle, sac_x, sac_y, .5, 1)
        elif f < start + 5: put(im, sac[f - start], sac_x, sac_y, .5, 1)
        else: put(im, sac[4], sac_x, sac_y, .5, 1)
        chain(im, path, head_d, f, cache, n=16, state=state_for(f))
        if start + 2 <= f < start + 10: put(im, expl[f - start - 2], sac_x, sac_y - 14)
        for i, f0 in lit.items():
            if f0 <= f < f0 + 4:
                x, y, _ = path.at(head_d - i * A.STEP_PX); put(im, ign[f - f0], x, y - 6)
        put(im, FROG_MOVE[f % 4] if f < start else FROG, 230, 228 - (min(f, start) * 2))
        frames.append(im)
    save_gif('Arthropleura_fuego', frames, 80)

if __name__ == '__main__':
    which = sys.argv[1:] or ['f1', 'ola', 'embestida', 'fuego']
    if 'f1' in which: escena_f1()
    if 'ola' in which: escena_ola()
    if 'embestida' in which: escena_embestida()
    if 'fuego' in which: escena_fuego()
    print('Listo:', OUT)
