"""Último aliento del Euriptérido (piso 2): cae rendido, jadea, se levanta rugiendo y su pose de furia del bullet hell,
más la gota venenosa que dispara.

Reutiliza la función eurypterid() de generar_euripterido.py (arte aprobado) sin ejecutar ese script: no se regenera
ninguna hoja que ya exista. Mismo lienzo de 128×128, mirando a la derecha, paleta blanda (rosada).
Hecho con la skill primordia-sprites: 16 px por unidad, luz arriba a la izquierda, contorno OUTL.

Uso: python generar_euripterido_final.py [carpeta_salida]   (por defecto, la carpeta de este script)
"""
import math, os, random, sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
OUT = sys.argv[1] if len(sys.argv) > 1 else HERE

# Solo las definiciones (paletas y eurypterid()), sin la parte que guarda las hojas.
_src = open(os.path.join(HERE, 'generar_euripterido.py'), encoding='utf-8').read()
_ns = {'__name__': 'euripterido_base', '__file__': os.path.join(HERE, 'generar_euripterido.py')}
_saved_argv = sys.argv
sys.argv = [sys.argv[0]]
exec(compile(_src[:_src.index('anims = {}')], 'generar_euripterido.py', 'exec'), _ns)
sys.argv = _saved_argv
globals().update({k: v for k, v in _ns.items() if not k.startswith('__')})

N = 128
CAVE = C('#141a24')
VENOM = ['#2a5a1a', '#4a9a2a', '#8ad84a', '#d8ff9a']   # gota venenosa (el verde del veneno de la cola)

def lying(cv, tilt, sink):
    """El cuerpo derrumbado: algo girado y aplastado contra el suelo (más ancho y más corto visto desde arriba)."""
    im = as_img(cv).rotate(tilt, resample=Image.NEAREST)
    if sink:
        w, h = im.size
        sq = im.resize((w, max(1, int(h * (1 - sink)))), Image.NEAREST)
        out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
        out.alpha_composite(sq, (0, (h - sq.height) // 2))
        im = out
    return im

def downed():
    """Cae rendido (8 cuadros a 120 ms): se tambalea, le fallan las patas y se derrumba con las pinzas abiertas."""
    fr = [eurypterid(pal=SOFT, eyes='wide', dx=-1, legs='brace', phase=0.4),
          eurypterid(pal=SOFT, eyes='wide', dx=1, legs='brace', phase=1.1, tail=0.2),
          eurypterid(pal=SOFT, eyes='closed', dx=-1, legs='curl', open_=(0.8, 0.8), spread=0.4, tail=-0.2)]
    for i, (tilt, sink) in enumerate(((-3, 0.0), (-6, 0.03), (-9, 0.06), (-10, 0.08), (-10, 0.08))):
        body = eurypterid(pal=SOFT, eyes='closed', legs='curl', open_=(1, 1), spread=0.5 + i * 0.05, tail=0.4 + i * 0.05,
                          dy=min(i, 2))
        fr.append(lying(body, tilt, sink))
    return fr

def downed_idle():
    """Rendido en el suelo (4 cuadros a 250 ms, bucle): jadea, los remos tiemblan."""
    fr = []
    for i in range(4):
        body = eurypterid(pal=SOFT, eyes='closed', legs='curl', open_=(1, 1), spread=0.7, tail=0.6, dy=2,
                          paddle=math.sin(i * math.pi / 2) * 0.6, phase=i * 0.4)
        fr.append(lying(body, -10, 0.08 if i % 2 == 0 else 0.06))
    return fr

def rise():
    """Se levanta rugiendo (8 cuadros a 110 ms): se endereza, abre las pinzas del todo y la cola se enciende."""
    fr = []
    for i, (tilt, sink) in enumerate(((-8, 0.06), (-5, 0.03), (-2, 0.0))):
        body = eurypterid(pal=SOFT, eyes='closed' if i < 2 else 'angry', legs='curl' if i < 2 else 'brace',
                          open_=(0.8, 0.8), spread=0.5, tail=0.4 - i * 0.2, dy=2 - i)
        fr.append(lying(body, tilt, sink))
    for i in range(5):
        roar = i in (1, 2, 3)
        fr.append(eurypterid(pal=SOFT, eyes='angry', red=True, legs='brace', reach=(0.4, 0.4) if roar else (0.2, 0.2),
                             open_=(1, 1), spread=0.75 if roar else 0.5, tail=-0.3 * math.sin(i * 1.4), tail_glow=2 if roar else 1,
                             dx=-1 if roar else 0, phase=i * 0.8))
    return fr

def aura(cv, k):
    """Chispas de veneno alrededor del cuerpo (parpadean de un cuadro a otro)."""
    rnd = random.Random(40 + k)
    body = [p for p, c in cv.px.items() if c[3] > 120]
    xs = [p[0] for p in body]; ys = [p[1] for p in body]
    cx, cy = (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2
    for _ in range(14):
        ang = rnd.uniform(0, 2 * math.pi)
        r = rnd.uniform(0.85, 1.15)
        x = cx + math.cos(ang) * (max(xs) - min(xs)) / 2 * r
        y = cy + math.sin(ang) * (max(ys) - min(ys)) / 2 * r
        if not (2 <= x < N - 3 and 2 <= y < N - 3):
            continue
        cv.put_out(x, y, POISON[:3] + (220,))
        if rnd.random() < 0.4: cv.put_out(x + 1, y, POISON[:3] + (120,))

def fury():
    """Furia del bullet hell (4 cuadros a 120 ms, bucle): plantado, pinzas abiertas que palpitan, cola encendida."""
    fr = []
    for i in range(4):
        body = eurypterid(pal=SOFT, eyes='angry', red=True, legs='brace', open_=(1, 1), spread=0.6 + 0.1 * (i % 2),
                          reach=(0.15, 0.15), tail=0.25 * math.sin(i * math.pi / 2), tail_glow=2 if i % 2 == 0 else 1,
                          phase=i * 0.6)
        aura(body, i)
        fr.append(body)
    return fr

def venom_drop(k):
    """Gota venenosa del bullet hell (12×12, 2 cuadros): bola verde con brillo y un hilo que gotea."""
    cv = Canvas(12)
    ellipse(cv, 6, 5.5, 3.4, 3.4, VENOM)
    cv.outline()
    cv.put(4, 4, C('#ffffff') if k == 0 else C(VENOM[3]))
    cv.put_out(6, 10 - k, C(VENOM[2], 170))
    return cv

if __name__ == '__main__':
    anims = {
        'Eurypterid_Downed': downed(),
        'Eurypterid_Downed_Idle': downed_idle(),
        'Eurypterid_Rise': rise(),
        'Eurypterid_Fury': fury(),
    }
    anims = {k: [as_img(f) for f in v] for k, v in anims.items()}
    drop = [as_img(venom_drop(k)) for k in range(2)]
    os.makedirs(f'{OUT}/Eurypterid', exist_ok=True)
    os.makedirs(f'{OUT}/Effects', exist_ok=True)
    os.makedirs(f'{OUT}/gif', exist_ok=True)
    for k, v in anims.items():
        save_sheet(f'{OUT}/Eurypterid/{k}.png', v)
    save_sheet(f'{OUT}/Effects/Proj_EurypteridVenom.png', drop)

    shown = dict(anims)
    shown['Proj_EurypteridVenom'] = [f.resize((48, 48), Image.NEAREST) for f in drop]
    preview(f'{OUT}/vista_previa_euripterido_final.png', shown, CAVE, scale=2)
    speed = {'Eurypterid_Downed': 120, 'Eurypterid_Downed_Idle': 250, 'Eurypterid_Rise': 110, 'Eurypterid_Fury': 120}
    for k, v in anims.items():
        gif(f'{OUT}/gif/{k}.gif', v, speed[k], CAVE)
    # Secuencia entera: cae, jadea dos veces, se levanta y furia.
    seq = anims['Eurypterid_Downed'] + anims['Eurypterid_Downed_Idle'] * 2 + anims['Eurypterid_Rise'] + anims['Eurypterid_Fury'] * 3
    ms = [120] * 8 + [250] * 8 + [110] * 8 + [120] * 12
    gif(f'{OUT}/gif/Eurypterid_UltimoAliento.gif', seq, ms, CAVE)
    print('Listo:', OUT)
