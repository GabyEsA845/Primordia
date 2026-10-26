"""Jefe del piso 2: Anomalocaris (sala oscura con plantas luminosas).

Visto desde arriba, mira a la derecha (Flip X para la izquierda). Lienzo 112×112, cuerpo de unos 75 px con la cola,
apéndices frontales que se enroscan, ojos con pedúnculo, aletas laterales que ondulan.
Uso: python generar_anomalocaris.py [carpeta_salida]
"""
import math, sys, os, random
from PIL import Image
from base_piso2 import *

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__))
N = 112
BODY = ['#3a0e1a', '#7a2030', '#b8443e', '#ec7a5a']
FLAP = ['#4a1222', '#94323e', '#d0624e', '#ffa47a']
APP = ['#3e1216', '#8a2c26', '#c8603a', '#ffa860']
EYEB = ['#140c1a', '#32243a', '#5c4a66', '#9a88a8']
SPINE = C('#fff0d8')
GLOW = (232, 255, 154)

def make_pal(base, flap, app, eye=EYEB):
    return dict(body=base, flap=flap, app=app, eye=eye)
PAL = make_pal(BODY, FLAP, APP)
P_WHITE = make_pal(WHITE4, WHITE4, WHITE4, WHITE4)
P_RED = make_pal(RED4, RED4, RED4, ['#5a1a1a', '#a33b3b', '#e05a5a', '#ff9a8a'])
P_PALE = make_pal(PALE4, PALE4, PALE4, ['#3a3830', '#5a5848', '#7a7864', '#9a9478'])
P_STONE = make_pal(STONE4, STONE4, STONE4, STONE4)

def width(u):
    return 3.5 + 5.5 * max(0, (u + 28) / 43) ** 0.8

def appendage(cv, x, y, s, spread, curl, pal, wig=0.0, segs=13):
    """Apéndice frontal: cadena de segmentos que sale hacia delante y se enrosca hacia la línea media.
    s = -1 arriba, 1 abajo. Devuelve los puntos para las espinas."""
    a = s * spread
    pts = []
    for k in range(segs):
        t = k / (segs - 1)
        r = 2.7 - 1.4 * t
        pts.append((x, y, r, a))
        a += -s * (curl / segs) * (0.4 + 1.6 * t * t) + wig * math.sin(k * 0.8) * 0.05
        x += math.cos(a) * 1.9; y += math.sin(a) * 1.9
    for i, (px, py, r, _) in enumerate(pts):
        ellipse(cv, px, py, r, r, pal['app'], shift=-1 if i % 2 else 0)
    return pts

def flap(cv, x, y, rx, ry, ang, ramp, shift):
    """Aleta lateral con su borde trasero oscuro, para que se lean una a una."""
    rot_ellipse(cv, x, y, rx, ry, ang, ramp, shift=shift)
    ca, sa = math.cos(ang), math.sin(ang)
    for yy in range(int(y - ry - rx) - 1, int(y + ry + rx) + 2):
        for xx in range(int(x - ry - rx) - 1, int(x + ry + rx) + 2):
            dx, dy = xx + .5 - x, yy + .5 - y
            u, v = dx * ca + dy * sa, -dx * sa + dy * ca
            e = (u / rx) ** 2 + (v / ry) ** 2
            if 0.55 < e <= 1 and dx < -0.3: cv.put(xx, yy, C(ramp[0]))

def anomalocaris(pal=PAL, phase=0.0, amp=1.0, sway=0.8, dx=0, dy=0, spread=0.55, curl=5.6, eyes='angry', red=False,
                 glow=False, flap_ext=1.0, sparks_at=None, bubbles=0, apps=True, alpha_body=255, n=N):
    cv = Canvas(n)
    cx, cy = n / 2 + dx - 6, n / 2 + dy
    def off(u): return sway * math.sin(phase - u * 0.13) * max(0, (18 - u) / 46)
    # cola en abanico: tres palas por lado
    for s in (-1, 1):
        for j in (2, 1, 0):
            a = 0.35 + j * 0.38 + 0.12 * math.sin(phase + j)
            ang = math.pi - s * a
            bx, by = cx - 31 + math.cos(ang) * 5.5, cy + off(-31) + math.sin(ang) * 5.5
            flap(cv, bx, by, 6.5, 2.1, ang, pal['flap'], -1 if j == 1 else 0)
    # aletas laterales, de la cola a la cabeza (las de delante tapan a las de detrás)
    for k in range(10, -1, -1):
        uk = 11 - k * 3.5
        w = 1 + 0.4 * amp * math.sin(phase - k * 0.7)
        ext = (6.8 - k * 0.3) * flap_ext * w
        for s in (-1, 1):
            u = uk + 1.0 * math.cos(phase - k * 0.7) * amp
            vc = s * (width(uk) + ext * 0.45)
            flap(cv, cx + u - 1, cy + off(uk) + vc, 3.3, max(1.5, ext * 0.62), s * 0.35, pal['flap'], -1 if k % 2 else 0)
    # cuerpo
    def body_in(px, py):
        u = px - cx; v = py - cy - off(u)
        if -30 <= u <= 15: return abs(v) <= width(u)
        if u < -30: return (u + 30) ** 2 / 13 + v * v / 13 <= 1
        return False
    fill(cv, body_in, cx - 5, cy, 26, 10, pal['body'], alpha_body)
    for x in range(int(cx - 28), int(cx + 11), 3):   # segmentos
        u = x + .5 - cx
        for y in range(int(cy - 11), int(cy + 12)):
            if (x, y) in cv.px and body_in(x + .5, y + .5) and abs(y + .5 - cy - off(u)) < width(u) - 0.8:
                cv.px[(x, y)] = C(pal['body'][1], alpha_body)
    # cabeza con la placa dorsal
    hx, hy = cx + 16.5, cy + off(16.5)
    ellipse(cv, hx, hy, 9, 9.5, pal['body'])
    ellipse(cv, hx + 0.5, hy - 0.5, 5.6, 4.6, pal['body'], shift=1)
    for yy in range(int(hy - 6), int(hy + 6)):
        for xx in range(int(hx - 6), int(hx + 7)):
            e = ((xx + .5 - hx - 0.5) / 5.6) ** 2 + ((yy + .5 - hy + 0.5) / 4.6) ** 2
            if 0.72 < e <= 1: cv.put(xx, yy, C(pal['body'][1]))
    # ojos con pedúnculo
    eye_pos = []
    for s in (-1, 1):
        ex, ey = cx + 19, cy + off(19) + s * 15
        for k in range(6):
            for w_ in (0, 1): cv.put(ex - 1 + w_, cy + off(19) + s * (8 + k), pal['body'][2 - w_])
        ellipse(cv, ex, ey, 4.2, 3.7, pal['eye'])
        eye_pos.append((ex, ey))
    tip_pts = []
    if apps:
        for s in (-1, 1):
            tip_pts.append(appendage(cv, cx + 24, cy + off(24) + s * 3.5, s, spread, curl, pal, wig=phase))
    for ex, ey in eye_pos:
        big_eye(cv, ex - 2, ey - 2, eyes, red, size=4)
        if eyes not in ('none', 'x', 'closed'): cv.put(ex - 3, ey - 3, C(pal['eye'][3]))
    cv.outline()
    for pts in tip_pts:   # espinas de los apéndices, hacia dentro
        top = pts[0][1] < cy
        for i, (px, py, r, a) in enumerate(pts[1:-1], 1):
            nx, ny = (-math.sin(a), math.cos(a)) if top else (math.sin(a), -math.cos(a))
            cv.put_out(px + nx * (r + 1.2), py + ny * (r + 1.2), SPINE)
            cv.put_out(px + nx * (r + 2.2) - math.cos(a) * 0.6, py + ny * (r + 2.2) - math.sin(a) * 0.6, C('#e8c8a8', 220))
    if glow:   # brillo de los ojos en la oscuridad
        for ex, ey in eye_pos:
            for yy in range(int(ey - 6), int(ey + 7)):
                for xx in range(int(ex - 6), int(ex + 7)):
                    d = math.hypot(xx + .5 - ex, yy + .5 - ey)
                    if 4.2 < d < 6: cv.put_out(xx, yy, GLOW + (int(110 * (6 - d) / 1.8),))
    if sparks_at and tip_pts:
        tx = (tip_pts[0][-1][0] + tip_pts[1][-1][0]) / 2; ty = (tip_pts[0][-1][1] + tip_pts[1][-1][1]) / 2
        sparks(cv, tx + 2, ty, sparks_at)
    for k in range(bubbles):
        rnd = random.Random(k * 7 + int(phase * 10))
        bubble(cv, cx + 26 + rnd.uniform(-3, 4), cy - 8 - k * 5 - rnd.uniform(0, 2), 1.4 + (k % 2) * 0.7)
    return cv


anims = {}
TAU = 2 * math.pi
anims['Anomalocaris_Swim'] = [anomalocaris(phase=i * TAU / 6, curl=5.6 + 0.3 * math.sin(i * TAU / 6)) for i in range(6)]
anims['Anomalocaris_SwimFast'] = [anomalocaris(phase=i * TAU / 4, amp=1.5, sway=1.6, spread=0.35, curl=6.2) for i in range(4)]
# aviso del zarpazo (0,7 s): se echa atrás y abre los apéndices; ojos rojos
anims['Anomalocaris_ClawWarn'] = [anomalocaris(phase=0.3 * i, amp=0.5, dx=-1 - i * 0.8, spread=0.6 + i * 0.22, curl=5.2 - i * 0.8,
                                               red=i >= 1) for i in range(5)]
# zarpazo: se lanza, los apéndices se estiran y se cierran de golpe (daño en el cuadro 1)
anims['Anomalocaris_ClawStrike'] = [
    anomalocaris(phase=1, amp=0.4, dx=3, spread=0.15, curl=0.7, red=True),
    anomalocaris(phase=1.4, amp=0.4, dx=5, spread=-0.05, curl=1.6, red=True, sparks_at=3),
    anomalocaris(phase=1.8, amp=0.4, dx=5, spread=0.0, curl=2.6, red=True, sparks_at=1),
    anomalocaris(phase=2.2, amp=0.6, dx=4, spread=0.3, curl=3.8),
    anomalocaris(phase=2.6, amp=0.8, dx=3, spread=0.45, curl=4.8)]
# vulnerable 1 s: apéndices caídos, jadea burbujas
anims['Anomalocaris_Vulnerable'] = [anomalocaris(phase=i * TAU / 8, amp=0.3, sway=0.3, dx=3, spread=0.12, curl=0.5,
                                                 eyes='tired', bubbles=1 + i % 3, flap_ext=0.85) for i in range(4)]
# fase 3, carga: tenso, tiembla, ojos rojos
anims['Anomalocaris_Charge'] = [anomalocaris(phase=i * 2.2, amp=0.35, sway=0.2, dx=-2 + (i % 2), dy=(i % 2) * 2 - 1 if i % 2 else 0,
                                             spread=1.1, curl=1.9, red=True) for i in range(4)]
anims['Anomalocaris_Hurt'] = [anomalocaris(pal=P_WHITE, eyes='wide', amp=0.3), anomalocaris(pal=P_RED, eyes='wide', amp=0.7, phase=1)]

def eyes_dark(i):
    """Cinemática al 25 %: en la oscuridad solo se ven los ojos (y una sombra apenas visible)."""
    shadow = make_pal(['#06040a'] * 4, ['#06040a'] * 4, ['#06040a'] * 4, ['#06040a'] * 4)
    cv = anomalocaris(pal=shadow, phase=i * TAU / 6, glow=False, eyes='none', alpha_body=255)
    im = cv.img()
    px = im.load()
    for y in range(N):
        for x in range(N):
            r, g, b, a = px[x, y]
            if a: px[x, y] = (6, 4, 12, 70)
    base = Canvas(N)
    for s in (-1, 1):
        ex, ey = N / 2 - 6 + 19, N / 2 + s * 15 + 0.8 * math.sin(i * TAU / 6) * 0
        for yy in range(int(ey - 9), int(ey + 10)):
            for xx in range(int(ex - 9), int(ex + 10)):
                d = math.hypot(xx + .5 - ex, yy + .5 - ey)
                if d < 9: base.put(xx, yy, GLOW + (int(80 * ((9 - d) / 9) ** 1.5),))
        if i == 3:
            for k in range(-3, 3): base.put(ex + k, ey, C('#e8ff9a'))
        else:
            ellipse(base, ex, ey, 3.2, 2.6, ['#c8e070', '#e8ff9a', '#ffffd8', '#ffffff'])
            for ddx in (0, 1):
                for ddy in (0, 1): base.put(ex + 0.5 + ddx, ey - 0.5 + ddy, OUTL)
    im.alpha_composite(base.img())
    return im
anims['Anomalocaris_EyesDark'] = [eyes_dark(i) for i in range(4)]

def death():
    fr = [anomalocaris(pal=P_WHITE, eyes='wide', amp=0.2),
          anomalocaris(pal=P_PALE, eyes='x', amp=0.6, spread=0.2, curl=0.8),
          anomalocaris(pal=P_PALE, eyes='x', amp=0.0, sway=0, spread=0.25, curl=0.4, flap_ext=0.75)]
    tilt = anomalocaris(pal=P_PALE, eyes='x', amp=0.0, sway=0, spread=0.3, curl=0.3, flap_ext=0.6).img()
    fr.append(tilt.rotate(12, resample=Image.NEAREST))
    for step in (1, 2):   # se hunde entre arena y burbujas, ya fosilizándose
        cv = anomalocaris(pal=P_STONE if step == 2 else P_PALE, eyes='none', amp=0, sway=0, spread=0.3, curl=0.3,
                          flap_ext=0.6)
        rnd = random.Random(step)
        for k in range(6 + step * 3):
            x, y = rnd.uniform(14, 90), rnd.uniform(42, 72)
            r = rnd.uniform(1.5, 3)
            for yy in range(int(y - r), int(y + r) + 1):
                for xx in range(int(x - r), int(x + r) + 1):
                    if math.hypot(xx - x, yy - y) <= r: cv.put(xx, yy, C(SAND[3 if (xx + yy) % 3 else 2], 230))
        for k in range(4 - step):
            bubble(cv, 46 + k * 11, 36 - k * 3 - step * 4, 1.8)
        fr.append(cv)
    fos = anomalocaris(pal=P_STONE, eyes='none', amp=0, sway=0, spread=0.3, curl=0.3, flap_ext=0.6)
    fr += [fos, fos]
    return fr
anims['Anomalocaris_Death'] = death()
anims = {k: [as_img(f) for f in v] for k, v in anims.items()}

# --- efectos -----------------------------------------------------------------------------
def fx_bubbles():
    fr = []
    for k in range(6):
        cv = Canvas(16)
        for i, (x0, y0, r) in enumerate(((5, 12, 1.6), (10, 13, 1.1), (8, 9, 0.9))):
            y = y0 - k * 1.6 - i
            if k < 5 - i * 0: bubble(cv, x0 + math.sin(k + i) * 0.7, y, r if k < 4 else r * 0.6, 220 - k * 30)
        fr.append(cv.img())
    return fr

def fx_swipe():
    """64×64 hacia la derecha, origen en el borde izquierdo (0, 32): las dos garras se cruzan."""
    fr = []
    for k, (a0, a1, alpha) in enumerate(((-60, -20, 200), (-55, 25, 255), (-40, 50, 255), (-10, 60, 170), (20, 60, 80))):
        im = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
        for s in (-1, 1):
            for r10 in range(180, 300):
                r = r10 / 10
                for t in range(a0, a1):
                    ang = math.radians(t * s)
                    x, y = int(4 + math.cos(ang) * r), int(32 + math.sin(ang) * r * s * s) + int(s * (r - 18) * 0.4)
                    if 0 <= x < 64 and 0 <= y < 64:
                        edge = r > 28.5 or r < 18.8
                        im.putpixel((x, y), (255, 255, 255, alpha) if not edge else (255, 140, 110, alpha // 2))
        fr.append(im)
    return fr

def fx_area():
    """64×64 en bucle: zona donde caerá el zarpazo de la fase 3 (2 u de radio)."""
    fr = []
    for k in range(4):
        im = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
        for y in range(64):
            for x in range(64):
                d = math.hypot(x + .5 - 32, y + .5 - 32)
                a = math.atan2(y + .5 - 32, x + .5 - 32)
                if 29.5 <= d <= 31 and int((a + k * 0.2) / (math.pi / 12)) % 2 == 0: im.putpixel((x, y), (255, 80, 60, 230))
                elif d < 29.5: im.putpixel((x, y), (255, 60, 40, 40 + (20 if k % 2 else 0)))
        for s in (-1, 1):   # las dos garras marcadas
            for t in range(-12, 13):
                x, y = 32 + t, 32 + s * (t * 0.0) + s * 6 + int(abs(t) ** 1.2 * 0.25) * s * -1
                for w in (0, 1):
                    if 0 <= x < 64: im.putpixel((x, int(y) + w), (255, 120, 90, 200))
        fr.append(im)
    return fr

def fx_path():
    """16×8 en mosaico: trayectoria del zarpazo (flechas que avanzan a la derecha)."""
    fr = []
    for k in range(4):
        im = Image.new('RGBA', (16, 8), (0, 0, 0, 0))
        for i in range(2):
            x0 = (i * 8 + k * 2) % 16
            for d in range(4):
                for xx, yy in ((x0 + 3 - d, 4 - d + 0), (x0 + 3 - d, 3 + d)):
                    im.putpixel((xx % 16, max(0, min(7, yy))), (255, 90, 70, 220 - d * 30))
        fr.append(im)
    return fr

fx = {'Fx_Anomalocaris_Bubbles': fx_bubbles(), 'Fx_Anomalocaris_Swipe': fx_swipe(),
      'Fx_Anomalocaris_ClawArea': fx_area(), 'Fx_Anomalocaris_ClawPath': fx_path()}

# --- guardado ----------------------------------------------------------------------------
os.makedirs(f'{OUT}/Anomalocaris', exist_ok=True); os.makedirs(f'{OUT}/Effects', exist_ok=True); os.makedirs(f'{OUT}/gif', exist_ok=True)
for k, v in anims.items(): save_sheet(f'{OUT}/Anomalocaris/{k}.png', v)
for k, v in fx.items(): save_sheet(f'{OUT}/Effects/{k}.png', v)
DARK = C('#0a0f1c')
preview(f'{OUT}/vista_previa_anomalocaris.png', anims, DARK, scale=2)
preview(f'{OUT}/vista_previa_anomalocaris_fx.png', {k: [f if f.width > 16 else f.resize((f.width * 3, f.height * 3), Image.NEAREST) for f in v] for k, v in fx.items()}, DARK, scale=2)
SPEED = {'Swim': 110, 'SwimFast': 70, 'ClawWarn': 140, 'ClawStrike': 60, 'Vulnerable': 250, 'Charge': 90, 'Hurt': 90,
         'EyesDark': 220, 'Death': 140}
for k, v in anims.items(): gif(f'{OUT}/gif/{k}.gif', v, SPEED[k.split('_')[1]], DARK)
for k, v in fx.items(): gif(f'{OUT}/gif/{k}.gif', v, 80, DARK, scale=4)
print('Listo:', OUT)
