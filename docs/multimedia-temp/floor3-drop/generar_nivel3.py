"""Gota de ámbar (piso 3): NIVEL III = bloque-cadáver en ámbar antiguo.

El enemigo muere al encerrarse y su cadáver queda de escudo (10 s, rompible). Misma forma C y mismas celdas que el
bloque normal, pero en resina vieja (más oscura y rojiza) con vetas doradas.
  Fx_AmberDropBlock3_{talla}_{Back,Front}: el bloque-cadáver (las grietas por golpes son las mismas Crack de siempre).
  Fx_AmberDropBlock3_{talla}_Form_{Back,Front}: 8 cuadros = 5 de subida (70 ms) + destello fuerte (90) +
      oscurecimiento a medias (90) y completo (110). El código fosiliza al enemigo (shader, mezcla hacia #3a1a08)
      desde el cuadro 5 en ~0,5 s, hasta un 75 % (más se pierde la silueta). Sin estrellas: está muerto.
  Fx_AmberDropBlock3_{talla}_Break_{Back,Front}: la rotura en ámbar viejo; el código oculta el cadáver en el cuadro 1.
A los jefes el nivel III solo los aturde 2 s: usan el bloque normal.
"""
import math, os, random, sys

HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import generar_bloque as gb
import generar_bloque_final as bf
import generar_formacion as gf
import generar_rotura as gr
import generar_disparo as gd
from generar_bloque import AMB, rgba, C, OUTL, Canvas
from PIL import Image

OLD = [C('#4a1a08'), C('#74300f'), C('#a44c18'), C('#d8822a'), AMB[4]]     # resina vieja (el brillo se queda)
GOLD = [AMB[3], AMB[4]]
MS3 = [70] * 5 + [90, 90, 110]
FOSSIL = (0x3a, 0x1a, 0x08)

def remap(cv, k=1.0):
    out = Canvas(cv.w, cv.h) if hasattr(cv, 'w') else Canvas(*cv.img().size)
    idx = {tuple(a[:3]): i for i, a in enumerate(AMB)}
    for p, c in cv.px.items():
        i = idx.get(tuple(c[:3]))
        if i is None: out.px[p] = c; continue
        o = OLD[i]; out.px[p] = tuple(round(c[j] + (o[j] - c[j]) * k) for j in range(3)) + (c[3],)
    return out

def veins(fr, b, k=1.0, seed=17):
    """Vetas doradas que cruzan el ámbar viejo (tenues por delante del enemigo)."""
    out = Canvas(*fr.img().size); out.px = dict(fr.px); rnd = random.Random(seed)
    pool = [p for p in b.mask if b.depth(*p) == 2]
    for i in range(6 if gb.W < 50 else 10):
        x, y = rnd.choice(pool); ang = rnd.uniform(0, 6.28)
        for s in range(rnd.randint(7, 12) * (2 if gb.W > 50 else 1)):
            xi, yi = round(x), round(y)
            if b.ins(xi, yi) and b.depth(xi, yi) >= 2:
                inside = gb.in_enemy(xi, yi); a = round((120 if inside else 235) * k)
                out.px[(xi, yi)] = rgba(GOLD[0] if s % 4 else GOLD[1], a)
            ang += rnd.uniform(-.5, .5); x += math.cos(ang); y += math.sin(ang) * .9
    return out

def flash(cv, b):
    """Destello fuerte del remate: chispa grande en el centro y el borde encendido."""
    out = Canvas(*cv.img().size); out.px = dict(cv.px); cy = round((b.y0 + b.y1) / 2); cx = gb.CX
    for (x, y) in b.mask:
        if b.depth(x, y) == 1: out.px[(x, y)] = rgba(AMB[4], 255)
    out.px[(cx, cy)] = (255, 255, 255, 255)
    for k in range(1, 6):
        a = 255 if k < 4 else 170
        for dx, dy in ((k, 0), (-k, 0), (0, k), (0, -k)): out.px[(cx + dx, cy + dy)] = rgba(AMB[4] if k < 4 else AMB[3], a)
    for dx, dy in ((1, 1), (-1, 1), (1, -1), (-1, -1), (2, 2), (-2, 2), (2, -2), (-2, -2)): out.px[(cx + dx, cy + dy)] = rgba(AMB[4], 200)
    return out

def build3(name):
    bk, fr, b = bf.build(name, 3)
    return remap(bk), veins(remap(fr), b), b

def form3(name):
    B, F = gf.frames(name); bk, fr, b = bf.build(name, 3)
    B3, F3 = B[:5], F[:5]
    B3.append(bk); F3.append(flash(fr, b))
    B3.append(remap(bk, .5)); F3.append(veins(remap(fr, .5), b, .5))
    bk3, fr3, _ = build3(name); B3.append(bk3); F3.append(fr3)
    return B3, F3

def fossil(img, k):
    if k <= 0: return img
    out = img.copy(); px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b_, a = px[x, y]
            if a: px[x, y] = (round(r + (FOSSIL[0] - r) * k), round(g + (FOSSIL[1] - g) * k), round(b_ + (FOSSIL[2] - b_) * k), a)
    return out

def strip(cvs, w, h):
    s = Image.new('RGBA', (w * len(cvs), h))
    for i, c in enumerate(cvs): s.alpha_composite(c.img(), (i * w, 0))
    return s

if __name__ == '__main__':
    out = os.path.join(HERE, 'nivel3'); os.makedirs(out, exist_ok=True); os.makedirs(os.path.join(HERE, 'gif', 'escenas'), exist_ok=True)
    SW, SH, FEET = 160, 100, 72; prev = []
    for name, enemy in (('Normal', 'Beetle'), ('Large', 'Medullosa')):
        bk3, fr3, b = build3(name); W, H, CX, BOT = gb.W, gb.H, gb.CX, gb.BOT
        bk3.img().save(os.path.join(out, f'Fx_AmberDropBlock3_{name}_Back.png')); fr3.img().save(os.path.join(out, f'Fx_AmberDropBlock3_{name}_Front.png'))
        FB, FF = form3(name)
        strip(FB, W, H).save(os.path.join(out, f'Fx_AmberDropBlock3_{name}_Form_Back.png')); strip(FF, W, H).save(os.path.join(out, f'Fx_AmberDropBlock3_{name}_Form_Front.png'))
        brk, (ox, oy), back0 = gr.break_frames(name); brk = [remap(c) for c in brk]; back0 = remap(back0)
        strip(brk, gr.RW, gr.RH).save(os.path.join(out, f'Fx_AmberDropBlock3_{name}_Break_Front.png'))
        strip([back0] + [Canvas(gr.RW, gr.RH)] * 6, gr.RW, gr.RH).save(os.path.join(out, f'Fx_AmberDropBlock3_{name}_Break_Back.png'))
        crack = gr.crack_phase(b, gr.crack_paths(b, random.Random(31), 9 if name == 'Normal' else 14), 2)
        # escena: disparo, impacto, formación III con fosilizado, cadáver-escudo agrietándose, rotura
        body = gb.enemy(enemy); bx0, by0 = SW - 50 - CX, FEET - (BOT - 1)
        ex, ey = bx0 + CX - body.width // 2, by0 + BOT - 1 - body.height; hx, hy = bx0 + CX, ey + body.height // 2
        P = [c.img() for c in gd.proj_frames()]; Hh = [c.img() for c in gd.hit_frames()]; hat = (hx - gd.HC, hy - gd.HC)
        cy = (b.y0 + BOT) / 2; rx0, ry0 = bx0 - (gr.RC - CX), by0 - round(gr.RC - cy)
        frames, dur = [], []
        def add(ms, back=None, back_at=(bx0, by0), front=(), fx=(), fk=0.0, show=True):
            im = Image.new('RGBA', (SW, SH), C('#2b2318')); im.alpha_composite(Image.new('RGBA', (SW, SH - FEET), C('#33291c')), (0, FEET))
            if back is not None: im.alpha_composite(back, back_at)
            if show: im.alpha_composite(fossil(body, fk), (ex, ey))
            for layer, at in list(front) + list(fx): im.alpha_composite(layer, at)
            frames.append(im); dur.append(ms)
        for k in range(8): add(70, fx=[(P[k % 4], (round(12 + (hx - 12) * k / 8) - gd.HX, hy - gd.HY))])
        add(60, fx=[(Hh[0], hat)]); add(60, fx=[(Hh[1], hat)])
        for f in range(8):
            fk = (0, 0, 0, 0, 0, .2, .5, .75)[f]
            add(MS3[f], back=FB[f].img(), front=[(FF[f].img(), (bx0, by0))], fx=[(Hh[2 + f], hat)] if f < 4 else [], fk=fk)
        for s in range(8): add(100, back=bk3.img(), front=[(fr3.img(), (bx0, by0))] + ([(crack.img(), (bx0, by0))] if s >= 4 else []), fk=.75)
        for f in range(7): add(gr.BREAK_MS[f], back=back0.img() if f == 0 else None, back_at=(rx0, ry0), front=[(brk[f].img(), (rx0, ry0))], fk=.75, show=f == 0)
        add(700, show=False)
        S = 5; big = [i.resize((SW * S, SH * S), Image.NEAREST).convert('RGB') for i in frames]
        big[0].save(os.path.join(HERE, 'gif', 'escenas', f'escena_gota_III_{name}.gif'), save_all=True, append_images=big[1:], duration=dur, loop=0)
        prev.append([frames[k] for k in (10, 13, 15, 16, 17, 19, 25, 26, 27, 29)])
    S = 2; cols = len(prev[0]); sheet = Image.new('RGB', ((100 * S + 2) * cols + 2, (80 * S + 2) * 2 + 2), (20, 16, 12))
    for r, row in enumerate(prev):
        for c, im in enumerate(row): sheet.paste(im.crop((60, 12, 160, 92)).resize((100 * S, 80 * S), Image.NEAREST).convert('RGB'), (2 + c * (100 * S + 2), 2 + r * (80 * S + 2)))
    sheet.save(os.path.join(HERE, 'vista_previa_nivel3.png')); print('ok', sheet.size)
