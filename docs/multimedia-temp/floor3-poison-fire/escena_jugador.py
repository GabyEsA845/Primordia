"""Escena de revisión: la rana dispara el Escupitajo (nivel I y nivel III, la Camada) con Escamas carbonizadas III
(Llamarada). Reproduce lo que hace hoy el código:
- la bola prende la banda de baba que va dejando (radio 0,6 u); cada sello quemado desaparece y cada 0,5 u de banda
  sale una nube (SlimeTrail.Burn);
- el reventón deja un charco que arde al instante y suelta otra nube en el cuadro 3.
Arriba, antes (columna Fx_ToxicCloud); abajo, ahora (charco salpicado que arde y bruma baja). Sale en gif/escenas/.

Uso: python escena_jugador.py
"""
import math, os, sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import generar_veneno_ardiendo as V
E, R = V.E, V.R
from pixel import as_img

FROG = os.path.join(HERE, '..', 'floor3-bodies', 'Frog')
W, H = 256, 128
MS = 50
RATE, RELEASE = 2.8, 0.56                              # la Boca dispara cada 2,8 s; la bola sale en el cuadro 7 (0,56 s)
CLOUD_SECONDS = 5.0
TRAIL_CLOUD_EVERY = {False: 2, True: 4}                 # hoy: 0,5 u entre nubes (0,25 u entre sellos en la gruesa, 0,125 en la fina)
TRAIL_CLOUD_EVERY_NEW = {False: 4, True: 8}             # propuesta: 1 u entre brumas en la banda, y más pequeñas

def sheet(path, cell):
    im = Image.open(path).convert('RGBA')
    return [im.crop((i * cell, 0, (i + 1) * cell, cell)) for i in range(im.width // cell)]

def sc(cv, s):
    im = as_img(cv)
    return im.resize((max(1, round(im.width * s)), max(1, round(im.height * s))), Image.NEAREST)

def cloud_sets(new):
    """(form, idle, fade, ms, ancla) por tamaño: 'big' (charco grande), 'small' (bolitas y banda)."""
    out = {}
    if new:
        hs = V.haze_sheets()
        for key, s in (('big', 1.0), ('small', .75), ('trail', .6)):
            out[key] = ([sc(f, s) for f in hs['Fx_PoisonHaze_Form']], [sc(f, s) for f in hs['Fx_PoisonHaze_Idle']],
                        [sc(f, s) for f in hs['Fx_PoisonHaze_Fade']], (90, 130, 110), lambda im: (im.width / 2, im.height / 2 + 4 * im.height / 32))
    else:
        ts = R.toxic_sheets()                            # hoy la nube no se escala
        c = ([sc(f, 1) for f in ts['Fx_ToxicCloud_Form']], [sc(f, 1) for f in ts['Fx_ToxicCloud_Idle']],
             [sc(f, 1) for f in ts['Fx_ToxicCloud_Fade']], (100, 120, 110), lambda im: (im.width / 2, 58))
        out = {'big': c, 'small': c, 'trail': c}
    return out

def cloud_frame(cs, age):
    form, idle, fade, ms, anchor = cs
    nf, ni = len(form) * ms[0] / 1000, CLOUD_SECONDS - len(fade) * ms[2] / 1000
    if age < nf: return form[int(age * 1000 / ms[0])]
    if age < ni: return idle[int((age - nf) * 1000 / ms[1]) % len(idle)]
    if age < CLOUD_SECONDS: return fade[min(len(fade) - 1, int((age - ni) * 1000 / ms[2]))]
    return None

def render(level, new, tones):
    base = E.ground(W, H, tones=tones)
    frog = sheet(os.path.join(FROG, 'Player_Frog_Attack_side.png'), 40)
    clouds = cloud_sets(new)
    T = {False: E.trail_set(E.TRAIL, E.TRAIL_VARIANTS, True), True: E.trail_set(E.THIN, E.THIN_VARIANTS, False)}
    rolls = {(sm, v): (E.small_frames(v) if sm else E.roll_frames(v, 8, E.BALL_R, 24)) for sm in (False, True) for v in ('side', 'down', 'up')}
    swell = {v: E.swell_frames(v) for v in ('side', 'down', 'up')}
    burst = [E.burst_frame(i) for i in range(7)]
    splat_big = 1.0 / (14 / 16)                          # PuddleRadius 1 u
    splat_small = .45 / (14 / 16)                        # SmallPuddleRadius 0,45 u
    sp_app = E.splat_sheets()['Fx_PoisonSplat_Appear']
    if new:
        ign = {k: [sc(V.splat_ignite(i), s) for i in range(6)] for k, s in (('big', splat_big), ('small', splat_small))}
    else:
        ign = {k: [sc(R.puddle_ignite(i), s) for i in range(6)] for k, s in (('big', 1 / 1.2), ('small', .45 / 1.2))}
    app = {k: [sc(f, s) for f in sp_app] for k, s in (('big', splat_big), ('small', splat_small))}
    px, py = 34, 64
    mouth = (px + 14, py + 4)
    shots = [(RELEASE + k * RATE, d) for k, d in enumerate(((1, -.18), (1, .22)))]
    spits, events, burned, minis_done = [], [], {}, set()   # events: (t, x, y, tipo, tamaño)
    life = 4.0 if level == 3 else 3.0
    seconds = shots[-1][0] + life + 1.0 + CLOUD_SECONDS
    frames = []
    for f in range(int(seconds * 1000 / MS)):
        t = f * MS / 1000
        im = base.copy()
        for (t0, d) in shots:                                # disparos
            if t >= t0 and not any(getattr(s, 'shot', None) == t0 for s in spits):
                s = E.Spit(mouth[0], mouth[1], d[0], d[1], speed=32, life=life, trail_life=3.0); s.t0 = t0; s.shot = t0
                spits.append(s)
        under, top = [], []
        for s in list(spits):
            lt = t - s.t0
            s.update(lt)
            bx, by = s.pos(lt)
            n = s.cfg['n']
            for j, (x, y, born, v, shn) in enumerate(s.stamps):
                key = (id(s), j)
                if key in burned: continue
                if lt - born >= .1 and lt < s.life + .05:    # la Llamarada la prende en la siguiente comprobación
                    burned[key] = t
                    if j % (TRAIL_CLOUD_EVERY_NEW if new else TRAIL_CLOUD_EVERY)[s.small] == 0: events.append((t, x, y, 'cloud', 'trail'))
                    continue
                k = E.stamp_frame(lt - born, s.tl)
                if k is None: continue
                rims, bodies = T[s.small][v]
                under.append((rims[k], x - n / 2, y - n / 2)); under.append((bodies[k], x - n / 2, y - n / 2))
            cs = 12 if s.small else 24
            gx, gy = E.CONTACT[cs]
            view, flip = s.view()
            if lt < s.life:
                if s.small: fr = rolls[(True, view)][int(lt * 1000 / E.SMALL_MS) % 4]
                elif lt >= s.life - .6: fr = swell[view][min(5, int((lt - (s.life - .6)) * 1000 / E.SWELL_MS))]
                else: fr = rolls[(False, view)][int(lt * 1000 / E.ROLL_MS) % 8]
                fr = as_img(fr)
                if flip: fr = fr.transpose(Image.FLIP_LEFT_RIGHT)
                top.append((by, fr, bx - (cs - gx if flip else gx), by - gy))
            elif id(s) not in minis_done:
                minis_done.add(id(s))
                size = 'small' if s.small else 'big'
                events.append((t, bx, by, 'splat', size))
                if not s.small:
                    events.append((t, bx, by, 'burst', size))
                    if level == 3:
                        for a in range(4):
                            m = E.Spit(bx, by, math.cos(a * math.pi / 2), math.sin(a * math.pi / 2), speed=40, life=1.0, trail_life=3.0, small=True)
                            m.t0 = t + .42; spits.append(m)
        for (t0, x, y, kind, size) in events:
            a = t - t0
            if a < 0: continue
            if kind == 'burst':
                k = int(a * 1000 / 60)
                if k < len(burst): top.append((y + 99, as_img(burst[k]), x - 32, y - 35))
            elif kind == 'splat':
                if a < .27: sp = app[size][min(2, int(a / .09))]
                elif a < .27 + .48: sp = ign[size][min(5, int((a - .27) / .08))]
                elif a < 5.5: sp = ign[size][5]
                else: sp = None
                if sp is not None: under.append((sp, x - sp.width / 2, y - sp.height / 2))
                if a >= .27 + .24 and not any(e[0] == t0 + .51 and e[1] == x for e in events):
                    events.append((t0 + .51, x, y, 'cloud', size))
            elif kind == 'cloud':
                cl = cloud_frame(clouds[size], a)
                if cl is not None:
                    ax, ay = clouds[size][4](cl)
                    top.append((y, cl, x - ax, y - ay))
        # la rana: dispara en bucle cada 2,8 s (empieza 0,56 s antes de soltar la bola)
        ft = t % RATE
        fr = frog[min(11, int(ft * 1000 / 80))] if t < shots[-1][0] + 1.0 else frog[0]
        top.append((py + 12, fr, px - 20 + 4, py - 20))
        for sp, x, y in under: im.alpha_composite(as_img(sp), (int(round(x)), int(round(y))))
        for _, sp, x, y in sorted(top, key=lambda q: q[0]): im.alpha_composite(sp, (int(round(x)), int(round(y))))
        frames.append(im)
    return frames

def both(level, tones, path):
    a, b = render(level, False, tones), render(level, True, tones)
    n = min(len(a), len(b))
    out = []
    for i in range(n):
        im = Image.new('RGBA', (W, H * 2 + 2), (0, 0, 0, 255))
        im.paste(a[i], (0, 0)); im.paste(b[i], (0, H + 2))
        out.append(im.resize((W * 3, (H * 2 + 2) * 3), Image.NEAREST).convert('RGB'))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    out[0].save(path, save_all=True, append_images=out[1:], duration=MS, loop=0)
    return out

if __name__ == '__main__':
    esc = os.path.join(HERE, 'gif', 'escenas')
    for level in (1, 3):
        both(level, V.TONES['Swamp'], os.path.join(esc, f'jugador_nivel{level}_antes_arriba_ahora_abajo.gif'))
    print('Listo')
