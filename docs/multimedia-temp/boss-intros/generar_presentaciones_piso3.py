"""Presentación de los jefes del piso 3 (como en Enter the Gungeon, igual que en los pisos 1 y 2; ver README).

Retratos 176×176 hechos con el motor 3D de los jefes del piso (generar_arthropleura: z-buffer, luz, contorno) a escala
de ilustración, con una cámara frontal ajustada a cada uno (24-46° en vez de 55°) y una pose amenazante:
- Arthropleura «Coloso del Carbonífero»: alzado, colmillos abiertos y el cuerpo detrás.
- Meganeura «Señora de la tormenta»: chillando con las alas abiertas y los ojos encendidos.
- Megarachne «La araña que nunca fue» (minijefe): enfurecida, con las delanteras alzadas; sello «MINIJEFE» en el rótulo.
- Lepidodendron ancestral «Corazón del bosque» (jefe secreto): el tronco en contrapicado con las vetas encendidas
  subiendo a una copa espesa y oscura (montado con sus piezas ya dibujadas, sin el motor 3D); nombre en dos líneas,
  banda más alta y sello «JEFE SECRETO» morado.

Color del piso 3 en el nombre: ámbar del incendio. Las poses de entrada son las animaciones ya hechas de cada jefe
(no hay hoja nueva): Arthropleura_Head_Rear, Meganeura_Intro, Megarachne_IntroEmerge + Megarachne_Threat.

Cada retrato se dibuja en su propio proceso (los generadores de la Meganeura y la Megarachne cambian las paletas
compartidas del motor).

Uso: python generar_presentaciones_piso3.py            (retratos, nombres y maquetas de todos)
     python generar_presentaciones_piso3.py solo <Jefe>  (solo ese jefe)
     python generar_presentaciones_piso3.py retrato <Arthropleura|Meganeura|Megarachne>   (uso interno)
"""
import math, os, random, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
TEMP = os.path.normpath(os.path.join(HERE, '..'))
BOSSES = os.path.join(TEMP, 'floor3-bosses')
PN = 176

AMBER_RAMP = ['#fff4c0', '#ffd25a', '#e89a30', '#8a3a10']
AMBER = (255, 178, 74)
BAND3 = (28, 14, 8)
OUT3 = (34, 12, 6)
INFO = {
    'Arthropleura': ('ARTHROPLEURA', 'Coloso del Carbonífero', 'Smoke', False),
    'Meganeura':    ('MEGANEURA', 'Señora de la tormenta', 'Burnt', False),
    'Megarachne':   ('MEGARACHNE', 'La araña que nunca fue', 'Smoke', True),
    'Lepidodendron': ('LEPIDODENDRON ANCESTRAL', 'Corazón del bosque', 'Smoke', 'secret'),
}
SECRET = (196, 150, 240)                               # lila del camino secreto (sello y filo del jefe secreto)

# =================================================================================================
# Retratos (cada uno en su proceso)
# =================================================================================================
def _cell(im, w):
    return im

def _scale(g, k):
    g.P = [P * k for P in g.P]
    return g

def retrato_megarachne():
    """De frente, cámara baja: alzada con las delanteras arriba, enfurecida (rojo encendido, espinas, ojos)."""
    sys.path.insert(0, os.path.join(BOSSES, 'megarachne'))
    import generar_megarachne as MG
    MG.AR.ELEV = math.radians(24)
    pose = dict(MG.MOMENTS['Threat'][3], rage=True, pitch=30, legs_up=1.0, wave=.1, abd=20)
    g = _scale(MG.body_geo(pose), 1.75)
    cv = MG.AR.render_geo(g, -90, PN, origin=(PN / 2, PN / 2 + 14), pal=MG.RAGE_PAL)
    return cv.img().convert('RGBA')

def retrato_meganeura():
    """De frente desde algo más arriba (para que las alas se vean anchas): chillando con los ojos encendidos."""
    sys.path.insert(0, os.path.join(BOSSES, 'meganeura'))
    import generar_meganeura as MN
    MN.AR.ELEV = math.radians(46); MN.ELEV = MN.AR.ELEV
    im = MN.render(-90, fore=10, hind=4, pitch=20, jaw=1, fold=-.6, eye=True)
    from PIL import Image
    out = Image.new('RGBA', (PN, PN), (0, 0, 0, 0))
    out.alpha_composite(im, ((PN - im.width) // 2, (PN - im.height) // 2 + 8))
    return out

def retrato_arthropleura():
    """De 3/4 desde arriba (sus placas y colmillos son lo que impone): la cabeza alzada con los colmillos abiertos y
    el cuerpo de placas cobrizas que se curva detrás llenando el marco."""
    sys.path.insert(0, os.path.join(BOSSES, 'arthropleura'))
    import generar_arthropleura as AT
    from PIL import Image
    AT.ELEV = math.radians(50)
    yaw, k, curve = -30, 1.0, 85
    CW = 260                                                                        # lienzo ancho (se recorta luego)
    out = Image.new('RGBA', (CW, PN), (0, 0, 0, 0))
    hx, hy = CW / 2 + 40, PN / 2 + 34
    pts, x, y_, a = [], hx, hy, math.radians(yaw) + math.pi                       # hacia atrás, curvándose
    for i in range(4):
        a += math.radians(curve) / 4
        x += math.cos(a) * 28 * k; y_ -= math.sin(a) * 28 * k * math.sin(AT.ELEV)
        pts.append((x, y_, math.degrees(a + math.pi)))
    for i, (x, y_, ya) in reversed(list(enumerate(pts))):                          # 3 placas y la cola, dentro del marco
        geo = AT.tail_geo(walk=.3) if i == len(pts) - 1 else AT.segment_geo(walk=.3)
        out.alpha_composite(_cell(AT.render_geo(_scale(geo, k), ya, PN, origin=(PN / 2, y_)).img().convert('RGBA'), CW), (int(x - PN / 2), 0))
    g = _scale(AT.head_geo(rear=.7, open_=1.0, feel=.3), k)
    out.alpha_composite(_cell(AT.render_geo(g, yaw, PN, origin=(PN / 2, hy)).img().convert('RGBA'), CW), (int(hx - PN / 2), 0))
    return out

# =================================================================================================
# Rótulo
# =================================================================================================
def retrato_lepidodendron():
    """El tronco (Lepido_Trunk) en contrapicado con todas las vetas encendidas y una copa espesa y oscura encima.
    176×176, apoyado abajo."""
    from PIL import Image
    lep = os.path.join(BOSSES, 'lepidodendron')
    sys.path.insert(0, lep)
    import generar_lepidodendron as L
    from base_piso3 import frondlet, Canvas
    tr = Image.open(os.path.join(lep, 'Tree', 'Lepido_Trunk.png')).convert('RGBA')
    vh = Image.open(os.path.join(lep, 'Tree', 'Lepido_Veins_Hit.png')).convert('RGBA').crop((0, 0, 288, 384))
    tr.alpha_composite(vh)
    src = tr.crop((144 - 110, 352 - 190, 144 + 110, 352 + 26))  # 220×216: del pie a ~12 u
    out = Image.new('RGBA', (PN, PN), (0, 0, 0, 0))
    sp, op = src.load(), out.load()
    for y in range(PN):                                 # contrapicado: el tronco se estrecha hacia arriba
        t = y / (PN - 1)
        sy = int(t ** .85 * (src.height - 1))
        sc = .52 + .48 * t ** 1.2                       # ancho relativo de la fila
        for x in range(PN):
            sx = 110 + (x - PN / 2) / sc * (176 / 220)
            if 0 <= sx < src.width: op[x, y] = sp[int(sx), sy]
    rnd = random.Random(6)
    cv = Canvas(PN, PN)                                 # la copa oscura que cuelga por arriba
    for x in range(PN):                                 # masa de hojas con el borde de arriba redondeado e irregular
        u = (x - PN / 2) / (PN / 2)
        top = 24 * u ** 4 + 4 * abs(math.sin(x / 7))
        e = 40 + 7 * math.sin(x / 13) + 5 * math.sin(x / 5) - 10 * u * u
        for yy in range(int(max(0, top)), int(e)): cv.put(x, yy, L.FG[0] if yy < e - 8 else L.FG[1])
    for k in range(140):                                # penachos sobre la masa (textura espesa)
        x = rnd.uniform(0, PN); u = (x - PN / 2) / (PN / 2)
        y = rnd.uniform(max(0, 24 * u ** 4), 34 - 10 * u * u)
        for j in range(rnd.randint(5, 8)):
            a = -math.pi / 2 + rnd.uniform(-2.6, 2.6)
            frondlet(cv, x, y, a, rnd.uniform(7, 13), L.FG[1:], .5, 1.4, None, 0 if a < -1 else 1)
    for x in range(0, PN, 5):
        for j in range(rnd.randint(4, 7)):
            a = math.pi / 2 + rnd.uniform(-1.1, 1.1)
            frondlet(cv, x + rnd.uniform(-3, 3), 34 + 6 * math.sin(x / 13) - 10 * ((x - PN / 2) / (PN / 2)) ** 2, a, rnd.uniform(10, 22), L.FG[1:], .4, 1.6, None, rnd.choice((0, 1, 1)))
    cv.outline(); cv.clean()
    out.alpha_composite(cv.img().convert('RGBA'))
    return out

def room(theme, seed=4):
    from PIL import Image, ImageEnhance
    tiles = os.path.join(TEMP, 'floor3-environment', theme, 'tiles')
    fl = [Image.open(os.path.join(tiles, f'Floor_{k}.png')).convert('RGBA') for k in range(6)]
    rnd = random.Random(seed); im = Image.new('RGBA', (480, 270))
    for y in range(0, 270, 16):
        for x in range(0, 480, 16): im.alpha_composite(fl[0 if rnd.random() < .66 else rnd.randint(1, 5)], (x, y))
    return ImageEnhance.Brightness(im).enhance(.55)

def stamp(im, x, y, secret=False):
    """Sello «MINIJEFE» (placa granate con borde ámbar) o «JEFE SECRETO» (placa morada con borde lila): placa
    pequeña inclinada como la banda, junto al nombre."""
    from PIL import Image, ImageDraw
    import generar_presentaciones as GP
    f = GP.font('Silkscreen-Bold.ttf', 8)
    t = 'JEFE SECRETO' if secret else 'MINIJEFE'
    w = int(ImageDraw.Draw(im).textlength(t, font=f)) + 10
    lab = Image.new('RGBA', (w + 2, 14), (0, 0, 0, 0)); d = ImageDraw.Draw(lab); d.fontmode = '1'
    d.rectangle([0, 0, w, 12], fill=(74, 26, 118, 255) if secret else (120, 26, 18, 255), outline=(SECRET if secret else AMBER) + (255,))
    d.text((5, 1), t, font=f, fill=(244, 232, 255, 255) if secret else (255, 236, 200, 255))
    im.alpha_composite(lab.rotate(3.3, resample=Image.NEAREST, expand=True), (x, y))

def mockup3(key, portrait, name_img):
    import generar_presentaciones as GP
    from PIL import Image, ImageDraw
    name, title, theme, mini = INFO[key]
    W, H = 480, 270
    im = room(theme).copy()
    d = ImageDraw.Draw(im)
    y0, y1 = (136, 232) if mini == 'secret' else (150, 214)
    d.polygon([(0, y0 + 14), (W, y0 - 14), (W, y1 - 14), (0, y1 + 14)], fill=BAND3 + (235,))
    d.line([(0, y0 + 14), (W, y0 - 14)], fill=AMBER, width=2)
    d.line([(0, y1 + 14), (W, y1 - 14)], fill=AMBER, width=2)
    im.alpha_composite(portrait, (W - portrait.width - 26, y1 - portrait.height + 10))
    im.alpha_composite(name_img, (36, y0 + 18))
    ty = y0 + 18 + name_img.height + 3 if mini == 'secret' else y0 + 50
    d.text((42, ty), title, font=GP.font('Silkscreen-Regular.ttf', 8), fill=AMBER)
    if mini == 'secret': stamp(im, 42 + int(d.textlength(title, font=GP.font('Silkscreen-Regular.ttf', 8))) + 8, ty - 3, secret=True)
    elif mini: stamp(im, 36 + name_img.width + 4, y0 + 24)
    d.text((W - 78, H - 14), '[E] Saltar', font=GP.font('Silkscreen-Regular.ttf', 8), fill=(200, 200, 210, 200))
    im.resize((W * 2, H * 2), Image.NEAREST).save(os.path.join(HERE, f'maqueta_rotulo_{key.lower()}.png'))

def name_image(GP, name):
    """El nombre rotulado; si es largo (el jefe secreto) va en dos líneas: la primera palabra grande y el resto debajo."""
    from PIL import Image
    if len(name) <= 14: return GP.title_text(name, GP.TITLE_FONT, 40, AMBER_RAMP, out=OUT3)
    a, b = name.split(' ', 1)
    i1 = GP.title_text(a, GP.TITLE_FONT, 40, AMBER_RAMP, out=OUT3)
    i2 = GP.title_text(b, GP.TITLE_FONT, 28, AMBER_RAMP, out=OUT3)
    out = Image.new('RGBA', (max(i1.width, i2.width), i1.height + i2.height - 8), (0, 0, 0, 0))
    out.alpha_composite(i1, (0, 0)); out.alpha_composite(i2, (2, i1.height - 8))
    return out

def main(only=None):
    from PIL import Image
    sys.path.insert(0, HERE)
    import generar_presentaciones as GP
    for sub in ('Portraits', 'Titles'): os.makedirs(os.path.join(HERE, sub), exist_ok=True)
    keys = [only] if only else list(INFO)
    for key in keys:                                    # retratos, cada uno en su proceso
        subprocess.run([sys.executable, os.path.abspath(__file__), 'retrato', key], check=True)
    portraits = {}
    for key in keys:
        name, title, theme, mini = INFO[key]
        p = Image.open(os.path.join(HERE, 'Portraits', f'Portrait_{key}.png')).convert('RGBA')
        portraits[key] = p
        name_img = name_image(GP, name)
        name_img.save(os.path.join(HERE, 'Titles', f'Title_{key}.png'))
        mockup3(key, p, name_img)
        print('  ', key)
    if not only:
        rows = {k: [v] for k, v in portraits.items()}
        GP.preview(os.path.join(HERE, 'vista_previa_piso3_retratos.png'), rows, GP.C('#1c1410'), scale=2)
    print('Listo')

if __name__ == '__main__':
    if len(sys.argv) > 2 and sys.argv[1] == 'retrato':
        key = sys.argv[2]
        im = {'Megarachne': retrato_megarachne, 'Meganeura': retrato_meganeura, 'Arthropleura': retrato_arthropleura,
              'Lepidodendron': retrato_lepidodendron}[key]()
        os.makedirs(os.path.join(HERE, 'Portraits'), exist_ok=True)
        bb = im.getchannel('A').getbbox()                                  # apoyado abajo y centrado (como pisos 1-2)
        print('   ancho del dibujo:', bb[2] - bb[0], 'alto:', bb[3] - bb[1])
        from PIL import Image
        out = Image.new('RGBA', (PN, PN), (0, 0, 0, 0))
        out.alpha_composite(im.crop(bb), ((PN - (bb[2] - bb[0])) // 2, PN - 4 - (bb[3] - bb[1])))
        im = out
        im.save(os.path.join(HERE, 'Portraits', f'Portrait_{key}.png'))
        print('   retrato', key, im.size)
    elif len(sys.argv) > 2 and sys.argv[1] == 'solo':
        main(sys.argv[2])
    else:
        main()
