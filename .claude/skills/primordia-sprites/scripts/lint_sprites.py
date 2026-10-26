"""Revisión automática de hojas de sprites de Primordia.

Uso:
    python lint_sprites.py <hoja.png | carpeta> [...] [--celda AxB] [--marcas CARPETA] [--sin-contorno]

Cada hoja se corta en cuadros horizontales; la celda por defecto es cuadrada con el alto de la hoja
(como las cortan los menús de Unity). Con --marcas guarda por cada hoja con avisos una copia ampliada ×8
con los píxeles señalados (rojo: aviso, amarillo: a revisar), para mirarla antes de enseñar el arte.

Comprueba:
  - dibujo cortado: píxeles de cuerpo (no de contorno) en el borde de la celda: el dibujo no cabe.
  - piezas sueltas: grupos de 1-3 píxeles separados del cuerpo (aviso si no son chispas, burbujas o motas a propósito).
  - píxeles huérfanos: un píxel dentro del cuerpo con un color que no tiene ninguno de sus 8 vecinos (ruido).
  - contorno grueso: bloques de 2×2 del color del contorno en el borde de la silueta (el contorno debe tener 1 px).
  - huecos del contorno: píxeles opacos del borde de la silueta que no son contorno (se omite con --sin-contorno).
  - negro o blanco puros, demasiados colores por cuadro y colores que solo aparecen en un cuadro.
"""
import os, sys
from collections import Counter
from PIL import Image

OUTL = (24, 20, 37)
N4 = ((1, 0), (-1, 0), (0, 1), (0, -1))
N8 = N4 + ((1, 1), (-1, -1), (1, -1), (-1, 1))

def frames_of(im, cw, ch):
    cols, rows = max(1, im.width // cw), max(1, im.height // ch)
    for r in range(rows):
        for c in range(cols):
            yield (c * cw, r * ch), im.crop((c * cw, r * ch, (c + 1) * cw, (r + 1) * ch))

def components(op):
    seen, out = set(), []
    for p in op:
        if p in seen: continue
        stack, comp = [p], []
        seen.add(p)
        while stack:
            x, y = stack.pop(); comp.append((x, y))
            for dx, dy in N8:
                q = (x + dx, y + dy)
                if q in op and q not in seen: seen.add(q); stack.append(q)
        out.append(comp)
    return out

def lint_frame(fr, outline=True, max_colors=24):
    w, h = fr.size
    px = fr.load()
    op = {(x, y): px[x, y] for y in range(h) for x in range(w) if px[x, y][3] > 0}
    solid = {p: c for p, c in op.items() if c[3] >= 128}
    warn, look, notes = [], [], []          # (mensaje, [píxeles])
    if not op: return warn, look, notes, Counter()

    edge = [p for p in op if p[0] in (0, w - 1) or p[1] in (0, h - 1)]
    cut = [p for p in edge if p in solid and solid[p][:3] != OUTL]
    if cut: warn.append(('dibujo cortado por el borde de la celda (%d px sin contorno en el borde)' % len(cut), cut))
    elif edge: notes.append('llega justo al borde de la celda (%d px)' % len(edge))

    comps = sorted(components(set(op)), key=len, reverse=True)
    small = [c for c in comps[1:] if len(c) <= 3]
    if small: look.append(('%d piezas sueltas de 1-3 px (¿chispas o motas a propósito?)' % len(small), [p for c in small for p in c]))

    uses = Counter(c[:3] for c in solid.values())
    orphans = []
    for (x, y), c in solid.items():
        if c[:3] == OUTL: continue
        nb = [solid.get((x + dx, y + dy)) for dx, dy in N8]
        if any(n is None for n in nb): continue           # borde de la silueta: lo mira el contorno
        if all(n[:3] != c[:3] for n in nb) and uses[c[:3]] <= 2: orphans.append((x, y))
    if orphans: look.append(('%d píxeles huérfanos (color casi sin usar, rodeado de otros: ¿ruido?)' % len(orphans), orphans))

    dbl = []
    for (x, y), c in solid.items():
        if c[:3] == OUTL and all(solid.get((x + dx, y + dy), (0, 0, 0, 0))[:3] == OUTL for dx, dy in ((1, 0), (0, 1), (1, 1))):
            block = [(x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1)]
            # solo en el borde de la silueta: dentro del cuerpo son ojos, bocas o rayas dibujados con ese color
            if any((bx + dx, by + dy) not in op for bx, by in block for dx, dy in N4): dbl += block
    if dbl: look.append(('contorno grueso en %d bloques de 2×2 (donde se juntan dos piezas, ¿sobra contorno?)' % (len(dbl) // 4), dbl))

    if outline:
        def body(q): return q in solid and solid[q][:3] != OUTL
        # solo zonas gruesas: las partes de 1-2 px (patas, brazos, antenas) van sin contorno a propósito
        gaps = [p for p, c in solid.items() if c[:3] != OUTL and any(
            (p[0] + dx, p[1] + dy) not in solid for dx, dy in N4)
            and sum(body((p[0] + dx, p[1] + dy)) for dx, dy in N8) >= 5]
        if gaps and len(gaps) > 0.02 * len(solid):
            look.append(('%d px del borde sin contorno (bien si son partes finas o translúcidas a propósito)' % len(gaps), gaps))

    black = [p for p, c in solid.items() if c[:3] == (0, 0, 0)]
    if black: warn.append(('%d px de negro puro (usar el contorno o un oscuro con tono)' % len(black), black))
    white = [p for p, c in solid.items() if c[:3] == (255, 255, 255)]
    if len(white) > 4: notes.append('%d px de blanco puro (solo para el centro de chispas, destellos y el cuadro blanco de Hurt)' % len(white))

    semi = sum(1 for c in op.values() if c[3] < 255)
    if semi: notes.append('%d px translúcidos' % semi)

    colors = Counter(c[:3] for c in solid.values())   # los translúcidos (resplandores, agua) no cuentan
    if len(colors) > max_colors: warn.append(('%d colores opacos en el cuadro (más de %d: ¿paleta disparada?)' % (len(colors), max_colors), []))
    return warn, look, notes, colors

def lint_sheet(path, cell=None, marks=None, outline=True):
    im = Image.open(path).convert('RGBA')
    cw, ch = cell or (im.height, im.height)
    if im.width % cw or im.height % ch:
        print('  ! la hoja (%d×%d) no se divide en celdas de %d×%d' % (im.width, im.height, cw, ch))
    per_frame, total_warn, total_look = [], 0, 0
    mark_px = {}
    all_colors = []
    for i, ((ox, oy), fr) in enumerate(frames_of(im, cw, ch)):
        warn, look, notes, colors = lint_frame(fr, outline)
        all_colors.append(set(colors))
        lines = []
        for m, ps in warn:
            lines.append('    aviso: ' + m); total_warn += 1
            for p in ps: mark_px[(p[0] + ox, p[1] + oy)] = (255, 40, 40, 255)
        for m, ps in look:
            lines.append('    revisar: ' + m); total_look += 1
            for p in ps: mark_px.setdefault((p[0] + ox, p[1] + oy), (255, 220, 0, 255))
        for m in notes: lines.append('    nota: ' + m)
        if lines: per_frame.append('  cuadro %d:\n%s' % (i, '\n'.join(lines)))
    n = len(all_colors)
    if n > 2:
        once = Counter(c for s in all_colors for c in s)
        lonely = [c for c, k in once.items() if k == 1]
        if len(lonely) > 6:
            per_frame.append('  nota: %d colores aparecen en un solo cuadro (¿deriva de paleta entre cuadros?)' % len(lonely))
    print('%s  (%d cuadros de %d×%d): %d avisos, %d a revisar' % (os.path.basename(path), n, cw, ch, total_warn, total_look))
    for l in per_frame: print(l)
    if marks and mark_px:
        os.makedirs(marks, exist_ok=True)
        s = 8
        big = Image.new('RGBA', (im.width * s, im.height * s), (40, 44, 52, 255))
        big.alpha_composite(im.resize((im.width * s, im.height * s), Image.NEAREST))
        from PIL import ImageDraw
        d = ImageDraw.Draw(big)
        for x in range(0, im.width * s, cw * s): d.line((x, 0, x, im.height * s), fill=(90, 90, 110, 255))
        for (x, y), c in mark_px.items(): d.rectangle((x * s, y * s, x * s + s - 1, y * s + s - 1), outline=c, width=2)
        out = os.path.join(marks, 'lint_' + os.path.basename(path))
        big.save(out)
        print('  marcas: ' + out)
    return total_warn, total_look

def main(argv):
    try: sys.stdout.reconfigure(encoding='utf-8')
    except AttributeError: pass
    cell, marks, outline, paths = None, None, True, []
    it = iter(argv)
    for a in it:
        if a == '--celda': w, h = next(it).lower().split('x'); cell = (int(w), int(h))
        elif a == '--marcas': marks = next(it)
        elif a == '--sin-contorno': outline = False
        else: paths.append(a)
    if not paths: print(__doc__); return 1
    files = []
    for p in paths:
        if os.path.isdir(p):
            for root, _, fs in os.walk(p):
                files += [os.path.join(root, f) for f in sorted(fs) if f.lower().endswith('.png') and not f.startswith(('vista_previa', 'lint_'))]
        else: files.append(p)
    tw = tl = 0
    for f in files:
        w, l = lint_sheet(f, cell, marks, outline); tw += w; tl += l
    print('\nTotal: %d hojas, %d avisos, %d a revisar' % (len(files), tw, tl))
    return 0

if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
