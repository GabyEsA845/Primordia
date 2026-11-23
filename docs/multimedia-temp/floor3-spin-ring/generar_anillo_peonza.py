"""Peonza (Diplocaulus, piso 3): anillo de alcance que aparece tras 1 s de giro (2026-11-23, Opus).

Tras girar 1 s, alrededor del jugador se abre un anillo de viento que daña lo que lo toca (mismo daño que el giro):
da algo de alcance para no comerse el daño por contacto. Lenguaje del remolino (Fx_SpinningTop_Whirl): rachas en arco
salmón → crema, en sentido horario, más finas y translúcidas, sobre un borde punteado que marca el área exacta.
Tamaño elegido por la usuaria: **B, radio 2,0 u (32 px), igual en los 3 niveles**; dibujado a su tamaño, sin escalar.

Jugador (50 ms, a la par que el remolino):
  Fx_SpinningTop_Ring_Appear (4): onda que se abre desde el remolino hasta el anillo, con líneas de velocidad.
  Fx_SpinningTop_Ring_Loop   (4, bucle).
  Fx_SpinningTop_Ring_End    (3): al soltar o marearse, las rachas se deshacen hacia fuera.
Clon del nivel III (40 ms, a la par que Fx_SpinClone_Whirl), mismo dibujo y tamaño en la paleta del clon (cremas
luminosas y un destello que recorre el anillo); lo lleva desde que sale:
  Fx_SpinClone_Ring_Appear (4) · Fx_SpinClone_Ring_Loop (4, bucle) · Fx_SpinClone_Ring_End (3).

Uso: python generar_anillo_peonza.py [tamanos]   (tamanos = vuelve a sacar la comparación A/B/C en gif/tamanos/)
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__))
TEMP = os.path.normpath(os.path.join(HERE, '..'))
sys.path.insert(0, os.path.normpath(os.path.join(TEMP, '..', '..', '.claude', 'skills', 'primordia-sprites', 'scripts')))
from pixel import *                                    # noqa: F401,F403
from PIL import Image, ImageDraw

U = 16
DIPLO = ramp('#d8806a', 5)
WIND = [DIPLO[2], DIPLO[3], DIPLO[4], '#fff8ec']       # el mismo viento del remolino
CLONE = ['#e07a64', '#f0947a', '#ffb494', '#ffd4b8', '#fff0e0']   # paleta del clon (floor3-top-clone)
CWIND = [CLONE[2], CLONE[3], CLONE[4], '#fff8ec']
MUD = '#735a3a'
RADIUS = 2.0                                           # tamaño B, elegido
SIZES = {'A': 1.7, 'B': 2.0, 'C': 2.4}                 # la muestra de tamaños (solo con el argumento «tamanos»)
WHIRL_R = 1.3                                          # remolino del nivel I (el código escala el dibujado de 1,5 u)
CLONE_WHIRL_R = 0.7

def cell_for(r_u): return int(math.ceil((r_u * U + 8) * 2 / 8)) * 8

def ring(r_u, i, n=4, grow=1.0, fade=0.0, burst=0.0, clone=False):
    """Un cuadro. grow: radio relativo (onda que se abre); fade: 0..1 se deshace; burst: líneas de velocidad;
    clone: paleta del clon, más luminosa, con un destello que recorre el anillo."""
    W = CWIND if clone else WIND
    size = cell_for(r_u); cv = Canvas(size); c = size / 2
    R = r_u * U * grow
    base = -i * (90 / n)                                # el giro es horario: el patrón avanza en sentido horario
    # borde punteado: el área exacta del daño
    steps = int(2 * math.pi * R)
    for s in range(steps):
        a = 2 * math.pi * s / steps
        if (s + i) % 3 == 0: continue
        cv.put(c + math.cos(a) * (R - .5), c + math.sin(a) * (R - .5), C(W[0], int((170 if clone else 150) * (1 - fade))))
    # rachas en arco: 4 largas por fuera y 4 cortas por dentro, con la cabeza clara
    for k in range(4):
        a0 = base + k * 90 + 25 * fade
        L = 58 * (1 - .6 * fade)
        for s in range(0, int(L), 2):
            a = math.radians(a0 - s)                    # la cabeza va delante (sentido horario)
            head = 1 - s / max(L, 1)
            rr = R - 1.5 + 3 * fade * (1 - head)
            col = W[3] if head > .85 else W[2] if head > .5 else W[1] if head > .2 else W[0]
            al = int((235 if head > .5 else 190 if head > .2 else 140) * (1 - fade))
            if al < 30: continue
            cv.put(c + math.cos(a) * rr, c + math.sin(a) * rr, C(col, al))
            if head > .6 and not fade: cv.put(c + math.cos(a) * (rr - 1), c + math.sin(a) * (rr - 1), C(W[1], 200))
        for s in range(0, 24, 3):
            a = math.radians(a0 + 45 - s)
            rr = R - 4.5
            cv.put(c + math.cos(a) * rr, c + math.sin(a) * rr, C(W[1] if s < 12 else W[0], int(170 * (1 - fade))))
    # líneas de velocidad que salen al abrirse
    if burst:
        for k in range(12):
            a = math.radians(k * 30 + 15 + i * 7)
            for d in range(3):
                rr = R + 1 + d + burst * 3
                cv.put(c + math.cos(a) * rr, c + math.sin(a) * rr, C(W[3] if d == 0 else W[2], int(230 * burst)))
    if clone and fade < .5:                             # destello que recorre el anillo (una vuelta cada 8 cuadros)
        a = math.radians(-i * 45 - 20)
        sx, sy = round(c + math.cos(a) * (R - 1.5)), round(c + math.sin(a) * (R - 1.5))
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)): cv.put(sx + dx, sy + dy, C(W[3], 230))
        cv.put(sx, sy, C(SPARK[0]))
    else:                                               # motas de barro despedidas por fuera (solo el jugador)
        rnd = random.Random(7)
        for k in range(5):
            a = math.radians(rnd.uniform(0, 360) - i * 25)
            d = R + 1 + ((i + k) % 3) + 2 * fade
            cv.put(c + math.cos(a) * d, c + math.sin(a) * d, C(MUD if k % 2 else '#9cc8a0', int(220 * (1 - fade))))
    return cv

def sheets(r_u, clone=False, w_r=WHIRL_R):
    w0 = w_r / r_u
    nl = 8 if clone else 4                              # el destello del clon da una vuelta en 8 cuadros
    return {'Appear': [ring(r_u, i, grow=w0 + (1 - w0) * e, burst=b, clone=clone).img()
                       for i, (e, b) in enumerate(((.35, 0), (.75, 1), (1.0, .7), (1.0, .25)))],
            'Loop': [ring(r_u, i, clone=clone).img() for i in range(nl)],
            'End': [ring(r_u, i, fade=f, clone=clone).img() for i, f in enumerate((.3, .6, .85))]}

# --- escenas -------------------------------------------------------------------------------------
def _frames(path, w=None):
    im = Image.open(path).convert('RGBA'); w = w or im.height
    return [im.crop((i * w, 0, i * w + w, im.height)) for i in range(im.width // w)]

def _ground(W, H, bg):
    im = Image.new('RGBA', (W, H), C(bg))
    for x in range(0, W, 9):
        for y in range((x * 7) % 11, H, 13): im.putpixel((x, y), C('#273f2b') if bg == '#1c2e20' else C('#363033'))
    return im

SPIN = lambda: _frames(os.path.join(TEMP, 'floor3-bodies', 'Diplocaulus', 'Player_Diplocaulus_Spin.png'))
BEETLE = lambda: _frames(os.path.join(TEMP, 'floor3-enemies', 'Beetle', 'Beetle_Move.png'))

def _whirl():
    wh = _frames(os.path.join(TEMP, 'floor3-bodies', 'Effects', 'Fx_SpinningTop_Whirl.png'))
    ws = round(48 * WHIRL_R / 1.5)
    return [f.resize((ws, ws), Image.NEAREST) for f in wh], ws

def scene_player(r_u, S, bg):
    """El Diplocaulus gira 1 s, se abre el anillo, un escarabajo llega y lo toca, suelta."""
    spin, beetle = SPIN(), BEETLE(); whirl, ws = _whirl()
    W, H = 200, 150; px, py = 80, 75
    out = []
    plan = [('spin', None)] * 20 + [('Appear', k) for k in range(4)] + [('Loop', k % 4) for k in range(16)] + \
           [('End', k) for k in range(3)] + [('spin', None)] * 4
    for t, (ph, k) in enumerate(plan):
        im = _ground(W, H, bg)
        bx = max(px + r_u * U + 9, W + 10 - t * 3.2)
        bf = beetle[t % len(beetle)].transpose(Image.FLIP_LEFT_RIGHT)
        im.alpha_composite(bf, (int(bx - bf.width / 2), int(py - bf.height / 2 + 2)))
        if ph != 'spin':
            f = S[ph][k]; im.alpha_composite(f, (int(px - f.width / 2), int(py - f.height / 2)))
        im.alpha_composite(whirl[t % 4], (px - ws // 2, py - ws // 2))
        im.alpha_composite(spin[t % 8], (px - 20, py - 20))
        out.append(im)
    return out, [50] * len(out)

def scene_clone(SP, SC, bg):
    """Nivel III: el jugador gira con su anillo, suelta y sale el clon con el suyo; va 9 u, vuelve y se funde."""
    spin, beetle = SPIN(), BEETLE(); whirl, ws = _whirl()
    CE = os.path.join(TEMP, 'floor3-top-clone', 'Effects')
    cspin = _frames(os.path.join(CE, 'Fx_SpinClone_Spin.png')); cwhirl = _frames(os.path.join(CE, 'Fx_SpinClone_Whirl.png'))
    cecho = _frames(os.path.join(CE, 'Fx_SpinClone_Echo.png')); capp = _frames(os.path.join(CE, 'Fx_SpinClone_Appear.png'))
    cmerge = _frames(os.path.join(CE, 'Fx_SpinClone_Merge.png'))
    W, H = 300, 130; px, py = 60, 65; dist = 9 * U; step = 14 * U * .04         # 14 u/s a 40 ms por cuadro
    n_go = int(math.ceil(dist / step))
    out = []
    def put(im, f, x, y): im.alpha_composite(f, (int(x - f.width / 2), int(y - f.height / 2)))
    t = 0; pos = []
    for k in range(10): pos.append(('player', None, k))                          # gira con su anillo
    for k in range(n_go): pos.append(('go', px + step * (k + 1), k))
    for k in range(n_go): pos.append(('back', px + dist - step * (k + 1), k))
    for k in range(3): pos.append(('merge', None, k))
    for k in range(6): pos.append(('rest', None, k))
    trail = []
    for (mode, cx, k) in pos:
        im = _ground(W, H, bg)
        bf = beetle[t % len(beetle)].transpose(Image.FLIP_LEFT_RIGHT); put(im, bf, px + 120, py + 2)
        if mode == 'player':
            put(im, SP['Loop'][t % 4], px, py); put(im, whirl[t % 4], px, py)
        elif mode in ('go', 'back') and k == 0 and mode == 'go':
            put(im, SP['End'][0], px, py)
        if mode == 'player' or mode == 'merge':
            put(im, spin[t % 8], px, py)
        else:
            put(im, spin[0] if mode == 'rest' else spin[t % 8], px, py)
        if mode in ('go', 'back'):
            ring_f = SC['Appear'][k] if (mode == 'go' and k < 4) else SC['Loop'][t % 8]
            if mode == 'back' and k >= n_go - 3: ring_f = SC['End'][k - (n_go - 3)]
            put(im, ring_f, cx, py)
            for j, (ex, ef) in enumerate(trail[-2:][::-1]):
                e = ef.copy()
                if j == 1: e.putalpha(e.getchannel('A').point(lambda a: int(a * .55)))
                put(im, e, ex, py)
            put(im, cwhirl[t % 4], cx, py)
            put(im, capp[k] if (mode == 'go' and k < 3) else cspin[t % 8], cx, py)
            trail.append((cx, cecho[t % 8]))
        if mode == 'merge': put(im, cmerge[k], px, py)
        out.append(im); t += 1
    return out, [40] * len(out)

def _gif(frames, ms, path, s=3):
    big = [f.resize((f.width * s, f.height * s), Image.NEAREST).convert('RGB') for f in frames]
    os.makedirs(os.path.dirname(path), exist_ok=True)
    big[0].save(path, save_all=True, append_images=big[1:], duration=ms, loop=0)

def main():
    SP, SC = sheets(RADIUS), sheets(RADIUS, clone=True, w_r=CLONE_WHIRL_R)
    for nm, fr in SP.items(): save_sheet(os.path.join(HERE, 'Effects', f'Fx_SpinningTop_Ring_{nm}.png'), fr)
    for nm, fr in SC.items(): save_sheet(os.path.join(HERE, 'Effects', f'Fx_SpinClone_Ring_{nm}.png'), fr)
    preview(os.path.join(HERE, 'vista_previa_anillo.png'),
            {**{f'SpinningTop_Ring_{k}': v for k, v in SP.items()}, **{f'SpinClone_Ring_{k}': v for k, v in SC.items()}},
            '#1c2e20', scale=3)
    shots = []
    for bg, tag in (('#1c2e20', 'musgo'), ('#2a2528', 'quemada')):
        fr, ms = scene_player(RADIUS, SP, bg); _gif(fr, ms, os.path.join(HERE, 'gif', 'escenas', f'anillo_jugador_{tag}.gif'))
        fc, mc = scene_clone(SP, SC, bg); _gif(fc, mc, os.path.join(HERE, 'gif', 'escenas', f'anillo_clon_{tag}.gif'))
        if tag == 'musgo': shots = [fr[22], fr[30], fr[41], fc[5], fc[12], fc[19], fc[26]]
    s = 2; sheet = Image.new('RGBA', (8 + 3 * (200 * s + 8), 8 + (150 * s + 8) + 2 * (130 * s + 8)), (16, 18, 16, 255))
    for j, f in enumerate(shots[:3]): sheet.alpha_composite(f.resize((200 * s, 150 * s), Image.NEAREST), (8 + j * (200 * s + 8), 8))
    for j, f in enumerate(shots[3:]):
        sheet.alpha_composite(f.resize((300 * s, 130 * s), Image.NEAREST),
                              (8 + (j % 2) * (300 * s + 8), 8 + 150 * s + 8 + (j // 2) * (130 * s + 8)))
    sheet.convert('RGB').save(os.path.join(HERE, 'vista_previa_anillo_escenas.png'))

def tamanos():
    for key, r_u in SIZES.items():
        S = sheets(r_u)
        fr, ms = scene_player(r_u, S, '#1c2e20'); _gif(fr, ms, os.path.join(HERE, 'gif', 'tamanos', f'anillo_{key}.gif'))

if __name__ == '__main__':
    main()
    if 'tamanos' in sys.argv[1:]: tamanos()
