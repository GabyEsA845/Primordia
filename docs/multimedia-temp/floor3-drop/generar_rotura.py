"""Gota de ámbar (piso 3): GRIETAS (3 fases), ROTURA con esquirlas disparadas y ESTRELLAS de aturdido.

Grietas: 3 imágenes que van encima de la capa Front del bloque; el código cambia de fase según el tiempo que queda.
Rotura: celda 96x96 con el centro del bloque en (48, 48). Cuadro 0 = quiebre (rendijas de luz); desde el 1 el bloque
ya no está (el enemigo queda libre) y 16 esquirlas salen volando hasta el radio de daño (~2,2 casillas = 35 px),
marcado por un anillo de polvo; los trozos grandes caen cerca. CUADRO QUE CUENTA: el 1 (daño de las esquirlas).
Estrellas: 3 estrellitas de ámbar girando en una elipse, encima del bloque (el código las coloca sobre la punta).
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import generar_bloque as gb
import generar_bloque_final as bf
from generar_bloque import AMB, rgba, C, OUTL, Canvas
from PIL import Image

RW = RH = 96; RC = 48; RADIUS = 35                  # celda de la rotura, centro y radio de daño en px
BREAK_MS = [90] + [60] * 6
STAR_N, STAR_MS = 6, 100

# --- grietas -------------------------------------------------------------------------------------------
def crack_paths(b, rnd, n):
    """Caminos de grieta: salen de puntos del borde hacia el centro, con una rama. (fase en que aparecen, puntos)."""
    cy = (b.y0 + b.y1) / 2; edge = [p for p in b.mask if b.depth(*p) == 2]; paths = []
    for i in range(n):
        x, y = rnd.choice(edge); ang = math.atan2(cy - y, gb.CX - x) + rnd.uniform(-.5, .5); pts = []
        L = rnd.randint(9, 14) * (1.5 if gb.W > 50 else 1)
        for s in range(int(L)):
            pts.append((round(x), round(y))); ang += rnd.uniform(-.6, .6); x += math.cos(ang); y += math.sin(ang) * .9
        br = []; bx, by = pts[len(pts) // 2]; ba = ang + rnd.choice((-1.1, 1.1))
        for s in range(int(L * .45)):
            br.append((round(bx), round(by))); ba += rnd.uniform(-.5, .5); bx += math.cos(ba); by += math.sin(ba) * .9
        paths.append((1 + (i >= n // 3) + (i >= 2 * n // 3), pts, br))
    return paths

def crack_phase(b, paths, phase):
    cv = Canvas(gb.W, gb.H)
    for born, pts, br in paths:
        if born > phase: continue
        grow = min(1.0, .45 + .3 * (phase - born))                        # cada fase alarga las grietas que ya había
        seg = pts[:max(2, int(len(pts) * grow))] + (br[:int(len(br) * grow)] if phase >= born + 1 else [])
        for (x, y) in seg:
            if b.ins(x, y) and b.depth(x, y) >= 2:
                cv.put(x, y, rgba(AMB[4], 240))
                if b.ins(x + 1, y + 1) and (x + 1, y + 1) not in seg: cv.put(x + 1, y + 1, rgba(AMB[0], 170))
    if phase == 3:                                                        # a punto de romper: borde con muescas claras
        for (x, y) in b.mask:
            if b.depth(x, y) == 2 and (x * 7 + y * 3) % 11 == 0: cv.put(x, y, rgba(AMB[4], 200))
    return cv

# --- rotura --------------------------------------------------------------------------------------------
def opaque_block(bk, fr):
    im = Image.new('RGBA', (gb.W, gb.H)); im.alpha_composite(bk.img()); im.alpha_composite(fr.img())
    px = im.load()
    def snap(c): return min(AMB, key=lambda a: sum((a[i] - c[i]) ** 2 for i in range(3)))[:3] + (255,)   # paleta del ámbar
    return {(x, y): snap(px[x, y]) for x in range(gb.W) for y in range(gb.H) if px[x, y][3] > 0}

def regions(b, rnd, n):
    seeds = [rnd.choice(sorted(b.mask)) for _ in range(n)]
    return {p: min(range(n), key=lambda i: (p[0] - seeds[i][0]) ** 2 + (p[1] - seeds[i][1]) ** 2 * 1.3) for p in b.mask}, seeds

def break_frames(name):
    bk, fr, b = bf.build(name, 3); rnd = random.Random(12)
    cy = (b.y0 + b.y1) / 2; ox, oy = RC - gb.CX, round(RC - cy)          # el centro del bloque va al centro de la celda
    solid = opaque_block(bk, fr); n_reg = 8 if gb.W < 50 else 11
    reg, seeds = regions(b, rnd, n_reg)
    frames = []; back0 = Canvas(RW, RH)
    for (x, y), c in bk.px.items(): back0.put(x + ox, y + oy, c)
    # cuadro 0: quiebre (capa Front translúcida con rendijas de luz; la Back va en su propia hoja)
    cv = Canvas(RW, RH)
    for (x, y), c in fr.px.items():
        seam = (x, y) in reg and any((x + dx, y + dy) in reg and reg[(x + dx, y + dy)] != reg[(x, y)] for dx, dy in ((1, 0), (0, 1)))
        cv.put(x + ox, y + oy, rgba(AMB[4], 255) if seam else c)
    for k in range(1, 4):                                                 # destello en el centro
        cv.put(RC + k, RC, rgba(AMB[4], 200)); cv.put(RC - k, RC, rgba(AMB[4], 200)); cv.put(RC, RC + k, rgba(AMB[4], 200)); cv.put(RC, RC - k, rgba(AMB[4], 200))
    cv.put(RC, RC, (255, 255, 255, 255)); frames.append(cv)
    # esquirlas: salen del centro, llegan al radio en el cuadro 4 y se apagan
    shards = []
    for i in range(16):
        a = i / 16 * 2 * math.pi + rnd.uniform(-.12, .12); shards.append((a, rnd.uniform(.85, 1.0) * RADIUS, rnd.randint(0, 3)))
    SHAPES = [[(0, 0), (1, 0), (0, 1)], [(0, 0), (1, 1)], [(0, 0), (0, 1), (1, 1)], [(0, 0), (1, 0)]]
    for f in range(1, 7):
        cv = Canvas(RW, RH); t = min(1.0, f / 4.0); e = 1 - (1 - t) ** 2
        # trozos grandes: se separan del centro y caen
        for i in range(n_reg):
            if f > 3: continue
            sx, sy = seeds[i]; dx, dy = sx - gb.CX, sy - cy; d = math.hypot(dx, dy) or 1
            mx = dx / d * (3 + 6 * f); my = dy / d * (2 + 3 * f) + 1.5 * f * f
            piece = {p for p, r in reg.items() if r == i}
            for _ in range(f):                                             # se encogen una capa por cuadro
                piece = {p for p in piece if all((p[0] + ddx, p[1] + ddy) in piece for ddx, ddy in ((1, 0), (-1, 0), (0, 1), (0, -1)))}
            if not piece: continue
            floor = gb.BOT + oy + 3; low = max(p[1] for p in piece) + my
            if low > floor: my -= low - floor                              # se posan en el suelo
            for p in piece: cv.put(round(p[0] + ox + mx), round(p[1] + oy + my), solid[p])
        # esquirlas
        for a, R, s in shards:
            if f > 5: break
            r = R * e; x = RC + math.cos(a) * r; y = RC + math.sin(a) * r * .8
            for k, (sx, sy) in enumerate(SHAPES[(s + f) % 4]):
                cv.put(round(x) + sx, round(y) + sy, rgba(AMB[4] if k == 0 else AMB[3], 255))
        # anillo de polvo: marca hasta dónde llega el daño
        if 3 <= f <= 6:
            al = (150, 120, 80, 40)[f - 3]; rr = RADIUS * (.9 + .04 * (f - 3))
            for k in range(72):
                a = k / 72 * 2 * math.pi
                if (k + f) % 3: cv.put(round(RC + math.cos(a) * rr), round(RC + math.sin(a) * rr * .8), rgba(AMB[3], al))
        # cuadro final: motas sueltas en el suelo
        if f >= 5:
            for i in range(10):
                a = i * 2.4; r = RADIUS * (.4 + .05 * (i % 6))
                cv.put(round(RC + math.cos(a) * r), round(RC + math.sin(a) * r * .8), rgba(AMB[2], 200 if f == 5 else 110))
        for (x, y), c in list(cv.px.items()):                             # contorno a los trozos grandes
            if c[3] == 255 and c[:3] not in (AMB[4][:3], AMB[3][:3]):
                for ddx, ddy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    if (x + ddx, y + ddy) not in cv.px and 0 <= x + ddx < RW and 0 <= y + ddy < RH: cv.px[(x + ddx, y + ddy)] = OUTL
        frames.append(cv)
    return frames, (ox, oy), back0

# --- estrellas de aturdido -----------------------------------------------------------------------------
STAR = ['..X..', '.XXX.', 'XXXXX', '.XXX.', '..X..']
def stars_frames():
    out = []
    for f in range(STAR_N):
        cv = Canvas(32, 16)
        for i in range(3):
            a = (f / STAR_N + i / 3) * 2 * math.pi; x = 16 + math.cos(a) * 11; y = 8 + math.sin(a) * 3.5
            front = math.sin(a) > 0; small = not front
            pat = ['.X.', 'XXX', '.X.'] if small else STAR
            for yy, row in enumerate(pat):
                for xx, ch in enumerate(row):
                    if ch == 'X':
                        cx_, cy_ = round(x) - len(row) // 2 + xx, round(y) - len(pat) // 2 + yy
                        centre = xx == len(row) // 2 and yy == len(pat) // 2
                        cv.put(cx_, cy_, rgba(AMB[4] if centre else AMB[3] if front else AMB[2], 255))
        cv.outline(); out.append(cv)
    return out

# --- maqueta -------------------------------------------------------------------------------------------
def strip(cvs, w, h):
    s = Image.new('RGBA', (w * len(cvs), h))
    for i, c in enumerate(cvs): s.alpha_composite(c.img(), (i * w, 0))
    return s

def scene(base, body, at, bg=C('#2b2318')):
    im = Image.new('RGBA', (RW, RH), bg); im.alpha_composite(Image.new('RGBA', (RW, RH // 2 - 10), C('#33291c')), (0, RH // 2 + 10))
    if body is not None: im.alpha_composite(body, at)
    if base is not None: im.alpha_composite(base, (0, 0))
    return im

if __name__ == '__main__':
    for d in ('grietas', 'rotura', 'aturdido', 'gif'): os.makedirs(os.path.join(HERE, d), exist_ok=True)
    stars = stars_frames(); strip(stars, 32, 16).save(os.path.join(HERE, 'aturdido', 'Fx_AmberDropStun_Stars.png'))
    rows = []
    for name, enemy in (('Normal', 'Beetle'), ('Large', 'Medullosa')):
        bk, fr, b = bf.build(name, 3); paths = crack_paths(b, random.Random(31), 9 if name == 'Normal' else 14)
        cracks = [crack_phase(b, paths, p) for p in (1, 2, 3)]
        strip(cracks, gb.W, gb.H).save(os.path.join(HERE, 'grietas', f'Fx_AmberDropBlock_{name}_Crack.png'))
        body = gb.enemy(enemy)
        held = []
        for k, cr in enumerate([None] + cracks):
            im = gb.compose(bk, fr, body)
            if cr is not None: im.alpha_composite(cr.img())
            if k == 0:                                                     # estrellas sobre la punta, en el bloque sin grietas
                st = stars[1].img(); tip = min(b.mask, key=lambda p: p[1]); im.alpha_composite(st, (gb.CX - 16, max(0, tip[1] - 12)))
            held.append(im)
        bfr, (ox, oy), back0 = break_frames(name); bk, fr, b = bf.build(name, 3)
        strip(bfr, RW, RH).save(os.path.join(HERE, 'rotura', f'Fx_AmberDropBlock_{name}_Break_Front.png'))
        strip([back0] + [Canvas(RW, RH)] * 6, RW, RH).save(os.path.join(HERE, 'rotura', f'Fx_AmberDropBlock_{name}_Break_Back.png'))
        at = (ox + gb.CX - body.width // 2, oy + gb.BOT - 1 - body.height)
        brk = [scene(f.img(), body, at) for f in bfr]
        brk[0] = scene(None, None, at); brk[0].alpha_composite(back0.img()); brk[0].alpha_composite(body, at); brk[0].alpha_composite(bfr[0].img())
        rows.append((held, brk))
        S = 5; seq = []; dur = []
        for im in held: seq.append(scene(None, None, (0, 0))); seq[-1].alpha_composite(im.crop((0, 0, gb.W, gb.H)), (ox, oy)); dur.append(500)
        seq += brk; dur += BREAK_MS; dur[-1] = 600
        big = [i.resize((RW * S, RH * S), Image.NEAREST).convert('RGB') for i in seq]
        big[0].save(os.path.join(HERE, 'gif', f'grietas_rotura_{name}.gif'), save_all=True, append_images=big[1:], duration=dur, loop=0)
    S = 2; cols = 4 + 7
    sheet = Image.new('RGB', ((RW * S + 2) * cols + 2, (RH * S + 2) * 2 + 2), (20, 16, 12))
    for r, (held, brk) in enumerate(rows):
        for c, im in enumerate(held):
            cell = scene(None, None, (0, 0)); cell.alpha_composite(im.crop((0, 0, im.width, im.height - 2)), ((RW - im.width) // 2, RC - (im.height - 2) // 2))
            sheet.paste(cell.resize((RW * S, RH * S), Image.NEAREST).convert('RGB'), (2 + c * (RW * S + 2), 2 + r * (RH * S + 2)))
        for c, im in enumerate(brk):
            sheet.paste(im.resize((RW * S, RH * S), Image.NEAREST).convert('RGB'), (2 + (4 + c) * (RW * S + 2), 2 + r * (RH * S + 2)))
    sheet.save(os.path.join(HERE, 'vista_previa_grietas_rotura.png')); print('ok', sheet.size)
