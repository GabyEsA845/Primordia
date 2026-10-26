"""Biblioteca común de pixel art de Primordia.

Reúne lo que cada generador copiaba (Canvas, C, contorno, rellenos sombreados, chispas, burbujas, hojas,
vistas previas y GIF) y añade:
  - ramp(): rampas de 4-5 tonos con el tono desplazado (sombras hacia azul/morado, luces hacia amarillo).
  - shade(): sombra que sale de la luz (arriba a la izquierda) y no del borde (evita el sombreado de almohada).
  - shade_legacy(): la sombra de antes, para regenerar arte ya aprobado sin que cambie.

Mismos nombres y argumentos que floor2-bosses/base_piso2.py, así un generador puede pasar de uno a otro
cambiando solo el import:

    import os, sys
    sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                    '..', '..', '..', '.claude', 'skills', 'primordia-sprites', 'scripts'))
    from pixel import *

Uso directo (para probar rampas):  python pixel.py rampa '#e05a5a' [pasos]
"""
import colorsys, math, os, sys
from PIL import Image, ImageDraw

PPU = 16                              # píxeles por unidad en todo el juego; nunca mezclar tamaños de píxel
OUTL = (24, 20, 37, 255)              # contorno morado muy oscuro (nunca negro puro)
LIGHT = (-0.5, -0.65, 0.57)           # luz arriba a la izquierda y algo de frente (x, y hacia abajo, z hacia la cámara)

def C(h, a=255):
    """'#rrggbb' o tupla -> (r, g, b, a)."""
    if isinstance(h, tuple): return h[:3] + (a,)
    h = h.lstrip('#'); return tuple(int(h[i:i+2], 16) for i in (0, 2, 4)) + (a,)

def hexc(c): return '#%02x%02x%02x' % tuple(c[:3])

EYE = C('#f4f4e8')                    # blanco del ojo (roto, no blanco puro)
SPARK = [C('#ffffff'), C('#fff6c0'), C('#ffe080')]   # el blanco puro solo en el centro de chispas y destellos
BUBBLE = (200, 236, 255)
WHITE4 = ['#ffffff'] * 4              # cuadro blanco de Hurt
RED4 = ['#a33b3b', '#e05a5a', '#ff9a8a', '#fff0e0']  # cuadro rojo de Hurt
PALE4 = ['#6a6450', '#9a9478', '#c4c0a4', '#ecead8']
STONE4 = ['#4a4640', '#6e6a62', '#948f84', '#b8b2a4']
SAND = ['#4a3c28', '#7a6646', '#ad9670', '#dcc9a0']

# --- rampas con el tono desplazado ---------------------------------------------------------------
COLD, WARM = 250.0, 55.0              # hacia dónde giran las sombras (azul-morado) y las luces (amarillo)

def _toward(h, target, deg):
    d = (target - h + 540) % 360 - 180
    return (h + max(-deg, min(deg, d))) % 360

def ramp(base, n=4, hue=14, dark=0.22, light=0.45, base_index=None):
    """Rampa de n tonos de oscuro a claro alrededor de `base`.

    base_index: posición del color base (por defecto (n-1)//2: el 1 de 4, el 2 de 5), que es el tono que más se ve.
    hue: grados que gira el tono en cada paso (10-20 según las guías de pixel art).
    dark: qué parte del brillo pierde cada paso de sombra; light: qué parte de lo que falta hasta el brillo
    máximo gana cada paso de luz (y pierde saturación, para que el brillo no sea blanco puro ni chillón).
    Los grises (saturación < 0,08) no giran: se quedan neutros.
    """
    r, g, b, _ = C(base)
    h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    h *= 360
    bi = (n - 1) // 2 if base_index is None else base_index
    grey = s < 0.08
    out = []
    for i in range(n):
        k = i - bi
        hh, ss, vv = h, s, v
        if k < 0:
            if not grey: hh = _toward(h, COLD, hue * -k)
            vv = v * (1 - dark) ** -k
            ss = min(1, s * 1.1 ** -k)
        elif k > 0:
            if not grey: hh = _toward(h, WARM, hue * k)
            for _ in range(k): vv = vv + (1 - vv) * light
            ss = s * (0.62 ** k)
        rr, gg, bb = colorsys.hsv_to_rgb(hh / 360, ss, vv)
        out.append('#%02x%02x%02x' % (round(rr * 255), round(gg * 255), round(bb * 255)))
    return out

def ramp_info(r):
    """Texto con tono, saturación y luminosidad de cada paso, y aviso si la rampa es plana."""
    rows, hues = [], []
    for c in r:
        rr, gg, bb, _ = C(c)
        h, l, s = colorsys.rgb_to_hls(rr / 255, gg / 255, bb / 255)
        rows.append('%s  tono %3.0f°  sat %.2f  luz %.2f' % (hexc(C(c)), h * 360, s, l))
        if s >= 0.08 and 0.06 < l < 0.94: hues.append(h * 360)
    span = 0
    if len(hues) > 1:
        span = abs((hues[-1] - hues[0] + 540) % 360 - 180)
    rows.append('giro total de tono: %.0f°%s' % (span, '  <- plana: sombras y luces no cambian de tono' if span < 10 and len(hues) > 1 else ''))
    return '\n'.join(rows)

# --- lienzo --------------------------------------------------------------------------------------
class Canvas:
    """Diccionario de píxeles (x, y) -> (r, g, b, a). Canvas(24) o Canvas(16, 8)."""
    def __init__(s, w, h=None):
        s.w, s.h = w, (h or w); s.n = w; s.px = {}
    def _in(s, x, y): return 0 <= x < s.w and 0 <= y < s.h
    def put(s, x, y, c):
        x, y = int(math.floor(x)), int(math.floor(y))
        if s._in(x, y): s.px[(x, y)] = C(c) if isinstance(c, str) else c
    def put_in(s, x, y, c):
        """Solo encima de lo ya dibujado (detalles dentro del cuerpo)."""
        if (int(math.floor(x)), int(math.floor(y))) in s.px: s.put(x, y, c)
    def put_out(s, x, y, c):
        """Solo donde aún no hay nada (cosas detrás o alrededor)."""
        if (int(math.floor(x)), int(math.floor(y))) not in s.px: s.put(x, y, c)
    def outline(s, color=OUTL, min_alpha=120):
        """Contorno de 1 px alrededor de lo opaco (lo translúcido por debajo de min_alpha no lleva contorno)."""
        add = [(x, y) for y in range(s.h) for x in range(s.w) if (x, y) not in s.px and any(
            (x+dx, y+dy) in s.px and s.px[(x+dx, y+dy)] != color and s.px[(x+dx, y+dy)][3] > min_alpha
            for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)))]
        for p in add: s.px[p] = color
    def clean(s, color=OUTL):
        """Quita esquinas dobles del contorno: un píxel de contorno con contorno a ambos lados en L y nada fuera."""
        for (x, y), c in list(s.px.items()):
            if c != color: continue
            n4 = [(x+dx, y+dy) for dx, dy in ((1,0),(-1,0),(0,1),(0,-1))]
            outl = [p for p in n4 if s.px.get(p) == color]
            body = [p for p in n4 if p in s.px and s.px[p] != color]
            if len(outl) == 2 and not body and outl[0][0] != outl[1][0] and outl[0][1] != outl[1][1]:
                del s.px[(x, y)]
    def img(s):
        im = Image.new('RGBA', (s.w, s.h), (0, 0, 0, 0))
        for p, c in s.px.items(): im.putpixel(p, c)
        return im

# --- sombreado ------------------------------------------------------------------------------------
def shade(nx, ny, steps=4, reflect=False):
    """Índice de la rampa (0 = más oscuro) para un punto de una forma redondeada.

    nx, ny: posición normalizada dentro de la forma (-1..1, el borde en radio 1). Se trata como una cúpula:
    la luz viene de arriba a la izquierda, así que el borde de arriba a la izquierda queda iluminado y solo el
    de abajo a la derecha se oscurece (sin sombreado de almohada). reflect=True aclara un tono el borde en sombra
    (luz rebotada del agua), útil en cuerpos grandes de 32 px o más.
    """
    r = math.hypot(nx, ny)
    if r > 1: nx, ny, r = nx / r, ny / r, 1.0
    nz = math.sqrt(max(0.0, 1 - r * r))
    lx, ly, lz = LIGHT
    ll = math.sqrt(lx*lx + ly*ly + lz*lz)
    lam = (nx * lx + ny * ly + nz * lz) / ll           # -1..1
    i = 0 if lam < 0.04 else 1 if lam < 0.6 else 2 if lam < 0.9 else 3
    if steps != 4: i = round(i * (steps - 1) / 3)
    if reflect and r > 0.9 and lam < 0.0: i = min(steps - 1, i + 1)
    return i

def shade_legacy(nx, ny):
    """Sombra de los generadores hasta octubre de 2026 (oscurece todo el borde). Solo para regenerar arte aprobado."""
    v = (-0.6*nx - 0.8*ny) * 0.8 + 0.35 - max(0, math.hypot(nx, ny) - 0.75) * 1.2
    return 0 if v < -0.15 else 1 if v < 0.35 else 2 if v < 0.75 else 3

SHADE = shade                         # se puede cambiar por shade_legacy en un generador antiguo

def fill(cv, inside, cx, cy, rx, ry, ramp, alpha=255, shift=0, box=None):
    """Rellena con la rampa los píxeles donde inside(x, y) es cierto, sombreando como una cúpula de radios rx, ry."""
    ramp = [C(c, alpha) for c in ramp]
    top = len(ramp) - 1
    x0, y0, x1, y1 = box or (0, 0, cv.w, cv.h)
    for y in range(max(0, int(y0)), min(cv.h, int(y1) + 1)):
        for x in range(max(0, int(x0)), min(cv.w, int(x1) + 1)):
            px, py = x + .5, y + .5
            if inside(px, py):
                i = SHADE((px - cx) / rx, (py - cy) / ry)
                if len(ramp) != 4: i = round(i * top / 3)
                cv.px[(x, y)] = ramp[max(0, min(top, i + shift))]

def ellipse(cv, cx, cy, rx, ry, ramp, alpha=255, shift=0):
    fill(cv, lambda x, y: ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1, cx, cy, rx, ry, ramp, alpha, shift,
         box=(cx - rx - 1, cy - ry - 1, cx + rx + 1, cy + ry + 1))

def rot_ellipse(cv, cx, cy, rx, ry, ang, ramp, alpha=255, shift=0):
    ca, sa = math.cos(ang), math.sin(ang)
    def inside(x, y):
        dx, dy = x - cx, y - cy
        u, v = dx * ca + dy * sa, -dx * sa + dy * ca
        return (u / rx) ** 2 + (v / ry) ** 2 <= 1
    r = max(rx, ry)
    fill(cv, inside, cx, cy, r, r, ramp, alpha, shift, box=(cx - r - 1, cy - r - 1, cx + r + 1, cy + r + 1))

# --- detalles comunes -----------------------------------------------------------------------------
def sparks(cv, x, y, size=2):
    cv.put(x, y, SPARK[0])
    for k in range(1, size + 1):
        c = SPARK[min(k, 2)]
        for dx, dy in ((k, 0), (-k, 0), (0, k), (0, -k)): cv.put(x + dx, y + dy, c)
    if size >= 2:
        for dx, dy in ((1, 1), (-1, -1), (1, -1), (-1, 1)): cv.put(x + dx, y + dy, SPARK[1])

def bubble(cv, x, y, r, a=210):
    for yy in range(int(y - r) - 1, int(y + r) + 2):
        for xx in range(int(x - r) - 1, int(x + r) + 2):
            d = math.hypot(xx + .5 - x, yy + .5 - y)
            if r - 0.9 <= d <= r: cv.put_out(xx, yy, BUBBLE + (a,))
    cv.put_out(x - r * 0.4, y - r * 0.4, (255, 255, 255, a))

# --- salida ---------------------------------------------------------------------------------------
def _font():
    from PIL import ImageFont
    for name in ('arial.ttf', 'DejaVuSans.ttf'):     # con tildes (la fuente de Pillow no las tiene)
        try: return ImageFont.truetype(name, 11)
        except OSError: pass
    return ImageFont.load_default()

def as_img(f): return f if isinstance(f, Image.Image) else f.img()

def save_sheet(path, frames):
    """Hoja horizontal: un cuadro por celda, todas del tamaño del primero."""
    frames = [as_img(f) for f in frames]
    w, h = frames[0].size
    sheet = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, im in enumerate(frames): sheet.paste(im, (i * w, 0))
    os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
    sheet.save(path)

def preview(path, anims, bg, scale=2, cols=None):
    """Vista previa: una fila por animación con su nombre. anims = {'Trilobite_Move': [cuadros], ...}"""
    anims = {k: [as_img(f) for f in v] for k, v in anims.items()}
    rows = list(anims)
    cell = max(max(max(f.width, f.height) for f in v) for v in anims.values()) * scale + 8
    cols = cols or max(len(v) for v in anims.values())
    W = 170 + cell * cols; rh = cell
    im = Image.new('RGBA', (W, 10 + len(rows) * rh), C(bg) if isinstance(bg, str) else bg); d = ImageDraw.Draw(im)
    for r, name in enumerate(rows):
        y0 = 10 + r * rh
        d.text((8, y0 + rh // 2 - 6), name.replace('_', ' '), fill=C('#bfe6ff'), font=_font())
        for i, f in enumerate(anims[name]):
            big = f.resize((f.width * scale, f.height * scale), Image.NEAREST)
            im.alpha_composite(big, (170 + i * cell + (cell - big.width) // 2, y0 + (rh - big.height) // 2))
    im.save(path)

def compare(path, pairs, bg, scale=4, labels=('antes', 'después')):
    """Antes y después lado a lado: pairs = {'nombre': (imagen_antes, imagen_después)}."""
    pairs = {k: (as_img(a), as_img(b)) for k, (a, b) in pairs.items()}
    cell = max(max(a.width, a.height, b.width, b.height) for a, b in pairs.values()) * scale + 12
    W = 170 + cell * 2; H = 26 + cell * len(pairs)
    im = Image.new('RGBA', (W, H), C(bg) if isinstance(bg, str) else bg); d = ImageDraw.Draw(im)
    for j, t in enumerate(labels): d.text((170 + j * cell + 6, 8), t, fill=C('#bfe6ff'), font=_font())
    for r, (name, (a, b)) in enumerate(pairs.items()):
        y0 = 26 + r * cell
        d.text((8, y0 + cell // 2 - 6), name.replace('_', ' '), fill=C('#bfe6ff'), font=_font())
        for j, f in enumerate((a, b)):
            big = f.resize((f.width * scale, f.height * scale), Image.NEAREST)
            im.alpha_composite(big, (170 + j * cell + (cell - big.width) // 2, y0 + (cell - big.height) // 2))
    im.save(path)

def gif(path, frames, ms, bg, scale=3):
    """GIF de revisión (no se sube a git). ms: duración de cada cuadro o lista de duraciones."""
    frames = [as_img(f) for f in frames]
    n = max(max(f.width, f.height) for f in frames)
    bgc = C(bg) if isinstance(bg, str) else bg
    out = []
    for f in frames:
        b = Image.new('RGBA', (n, n), bgc); b.alpha_composite(f, ((n - f.width) // 2, (n - f.height) // 2))
        out.append(b.resize((n * scale, n * scale), Image.NEAREST).convert('RGB'))
    out[0].save(path, save_all=True, append_images=out[1:], duration=ms, loop=0)

def ramp_strip(path, ramps, scale=16):
    """Muestrario de rampas: una fila por rampa, un cuadrado por tono. ramps = {'nombre': [colores]}"""
    n = max(len(r) for r in ramps.values())
    im = Image.new('RGBA', (110 + n * scale, len(ramps) * scale + 4), C('#202028')); d = ImageDraw.Draw(im)
    for j, (name, r) in enumerate(ramps.items()):
        d.text((4, j * scale + 4), name, fill=C('#dddddd'), font=_font())
        for i, c in enumerate(r): d.rectangle((110 + i * scale, j * scale + 2, 110 + (i + 1) * scale - 2, (j + 1) * scale), fill=C(c))
    im.save(path)

if __name__ == '__main__':
    try: sys.stdout.reconfigure(encoding='utf-8')
    except AttributeError: pass
    if len(sys.argv) >= 3 and sys.argv[1] == 'rampa':
        n = int(sys.argv[3]) if len(sys.argv) > 3 else 4
        print(ramp_info(ramp(sys.argv[2], n)))
    else:
        print(__doc__)
