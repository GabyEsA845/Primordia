"""Piezas de la sala del Anomalocaris (piso 2, Abismo) y su burbuja.

- Columnas de algas (marca A del plano): un manojo de laminarias de unas 6 u de alto que se mece. Se dibujan por
  encima de los personajes y tapan la vista sin bloquear el paso (el juego las vuelve translúcidas si el jugador
  está debajo). Celda 64×96, pivote en la base (32, 92).
- Planta luminosa (marca L): tres tallos con bulbos cian que laten; casi toda la luz de la sala sale de ellas
  (luz 2D en el juego). `Off` es el apagado de la escena de los ojos (al revés, se enciende).
- Burbuja del jefe: proyectil lento de la estela y del muro del cerco, y su reventón.

Hecho con la skill primordia-sprites (pixel.py): 16 px por unidad, luz arriba a la izquierda, contorno OUTL.
Paleta del Abismo (references/pisos.md): casi negro azulado y bioluminiscencia cian.

Uso: python generar_sala_anomalocaris.py [carpeta_salida]   (por defecto, la carpeta de este script)
"""
import math, os, random, sys

def _skill_scripts():
    d = os.path.dirname(os.path.abspath(__file__))
    while True:
        p = os.path.join(d, '.claude', 'skills', 'primordia-sprites', 'scripts')
        if os.path.isdir(p): return p
        if os.path.dirname(d) == d: raise SystemExit('No encuentro .claude/skills/primordia-sprites/scripts')
        d = os.path.dirname(d)
sys.path.insert(0, _skill_scripts())
from pixel import *

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = sys.argv[1] if len(sys.argv) > 1 else HERE
ROOM = os.path.join(OUT, 'AnomalocarisRoom')
FX = os.path.join(OUT, 'Effects')
BG = C('#0b1220')                         # suelo del Abismo

# --- paleta ----------------------------------------------------------------------------------------
KELP = ['#0c1a22', '#123028', '#1b463c', '#2a6052']   # laminaria verde azulada muy oscura: silueta que tapa
STIPE = ['#101a1a', '#1c2c26', '#2a3e30']            # tallos, algo más pardos
FLOAT = ['#16241e', '#28402c', '#3e5c38', '#64824a']   # vejigas de flotación, el punto más claro del alga
GLOW = ['#1d6a78', '#36d8d0', '#7ae8ff', '#e4fdff']   # bulbos de la planta luminosa (cian del Abismo)
DIM = ['#122a34', '#1a4450', '#246068', '#2f7a80']   # bulbos apagados
STALK = ['#14202e', '#1f3442', '#2e4e5a']            # tallos de la planta (finos, sin contorno)
ROCK = ['#0e1424', '#18223a', '#26344e', '#3a4c68']  # piedra de la base
BUB = ['#5aa8c8', '#9cd8f0', '#d8f4ff']              # borde de la burbuja

# --- columna de algas --------------------------------------------------------------------------------
BASE = (32, 92)

def _stipes(seed):
    """Tallos de un manojo: (desvío lateral en la punta, alto, fase propia, delante)."""
    rnd = random.Random(seed)
    out = []
    for k in range(6):
        lean = (k - 2.5) * 4.2 + rnd.uniform(-2, 2)
        height = rnd.uniform(60, 80)
        out.append((lean, height, rnd.uniform(0, 2 * math.pi), k % 2 == 1))
    return out

def _stipe_point(lean, height, phase, t, sway):
    """Punto del tallo a la altura relativa t (0 base, 1 punta). El vaivén crece con la altura."""
    x = BASE[0] + lean * t + math.sin(phase + t * 2.2) * sway * t * t
    y = BASE[1] - height * t
    return x, y

def algae(seed, frame, frames=4):
    cv = Canvas(64, 96)
    swing = math.sin(frame / frames * 2 * math.pi)
    stipes = _stipes(seed)
    # primero los de atrás, más oscuros; luego los de delante, separados por valor
    for front in (False, True):
        for lean, height, phase, is_front in stipes:
            if is_front != front: continue
            shift = 0 if front else -1
            sway = 5.5 * swing + 1.2 * math.sin(phase)
            # hojas alternas a lo largo del tallo, más grandes a media altura
            n = 5
            for i in range(n):
                t = 0.2 + i * 0.78 / n
                x, y = _stipe_point(lean, height, phase, t, sway)
                x2, y2 = _stipe_point(lean, height, phase, t + 0.04, sway)
                up = math.atan2(y2 - y, x2 - x)
                side = 1 if (i + int(phase * 3)) % 2 == 0 else -1
                ang = up + side * (0.55 - 0.2 * t) + 0.18 * swing
                length = 9 + 4 * math.sin(t * math.pi)
                cx, cy = x + math.cos(ang) * length * 0.8, y + math.sin(ang) * length * 0.8
                rot_ellipse(cv, cx, cy, length, 1.7 + 0.5 * math.sin(t * math.pi), ang, KELP, shift=shift)
            # el tallo, de 2 px, por encima de sus hojas
            for s in range(0, 80):
                t = s / 80
                x, y = _stipe_point(lean, height, phase, t, sway)
                cv.put(x, y, STIPE[2] if front else STIPE[1])
                cv.put(x + 1, y, STIPE[1] if front else STIPE[0])
            # vejiga de flotación en la punta
            x, y = _stipe_point(lean, height, phase, 1.0, sway)
            ellipse(cv, x, y - 1, 2.6, 2.2, FLOAT, shift=shift)
    # agarre: raíces de piedra en la base
    ellipse(cv, BASE[0], BASE[1], 7, 2.6, ROCK, shift=1)
    cv.outline(); cv.clean()
    return cv

# --- planta luminosa ---------------------------------------------------------------------------------
PLANT = [(-6, 12, 3.4), (1, 19, 4.3), (8, 10, 3.2)]   # (x desde el centro, alto del tallo, radio del bulbo)

def light_plant(pulse=0.5, lit=1.0):
    """pulse 0..1: latido; lit 1 = encendida, 0 = apagada (la mezcla entre las dos rampas va por pasos)."""
    cv = Canvas(32)
    cx, base = 16, 28
    pal = GLOW if lit >= 0.75 else DIM if lit <= 0.25 else [GLOW[0], GLOW[1], DIM[3], GLOW[2]]
    # base de piedra
    ellipse(cv, cx, base, 6.5, 2.4, ROCK, shift=1)
    for dx, height, r in PLANT:
        top = base - height
        rr = r + (0.5 * pulse if lit > 0.5 else 0)
        ellipse(cv, cx + dx, top, rr, rr * 0.9, pal)
    cv.outline(); cv.clean()
    # tallos finos sin contorno, por detrás de los bulbos
    for dx, height, r in PLANT:
        top = base - height
        for y in range(int(top + r), base - 1):
            t = (y - top) / height
            x = cx + dx * (0.55 + 0.45 * (1 - t))
            cv.put_out(x, y, STALK[1] if dx <= 0 else STALK[2])
    # núcleo que brilla: más grande en lo alto del latido
    if lit >= 0.75:
        for dx, height, r in PLANT:
            x, y = cx + dx, base - height
            cv.put(x - 1, y - 1, GLOW[3])
            if pulse > 0.6:
                cv.put(x, y - 1, GLOW[3])
                cv.put(x - 1, y, GLOW[2])
    # esporas que suben en lo alto del latido
    if lit >= 0.75 and pulse > 0.9:
        cv.put(cx - 2, 3, GLOW[2]); cv.put(cx + 5, 6, GLOW[1])
    return cv

# --- burbuja ------------------------------------------------------------------------------------------
def bubble_proj(frame):
    """Burbuja de 9-10 px que tiembla: borde claro, interior apenas teñido y brillo arriba a la izquierda."""
    cv = Canvas(16)
    sx = 4.6 + 0.4 * math.sin(frame / 4 * 2 * math.pi)
    sy = 4.6 - 0.4 * math.sin(frame / 4 * 2 * math.pi)
    cx = cy = 8
    for y in range(16):
        for x in range(16):
            d = math.hypot((x + .5 - cx) / sx, (y + .5 - cy) / sy)
            if d <= 1:
                ring = d > 0.74
                lit = (x - cx) + (y - cy) < 0
                c = (BUB[2] if lit else BUB[1]) if ring else BUB[0]
                cv.put(x, y, C(c, 255 if ring else 90))
    cv.outline(min_alpha=200)
    cv.put(cx - 2, cy - 3, SPARK[0]); cv.put(cx - 3, cy - 2, C(BUB[2]))
    return cv

def bubble_pop(frame):
    """Reventón: el anillo se abre en 4 gotas que se alejan y se apagan."""
    cv = Canvas(16)
    if frame == 0:
        r = 5.5
        for a in range(0, 360, 12):
            if (a // 45) % 2 == 0:
                cv.put(8 + math.cos(math.radians(a)) * r, 8 + math.sin(math.radians(a)) * r, C(BUB[2]))
        sparks(cv, 8, 8, 1)
        return cv
    d = 3 + frame * 1.6
    col = C(BUB[2 - min(2, frame - 1)], 255 - 50 * frame)
    for k in range(4):
        a = math.radians(45 + 90 * k)
        x, y = 8 + math.cos(a) * d, 8 + math.sin(a) * d
        cv.put(x, y, col)
        if frame < 3: cv.put(x - math.cos(a), y - math.sin(a), C(BUB[0], 200 - 50 * frame))
    return cv

# --- salida ----------------------------------------------------------------------------------------
if __name__ == '__main__':
    os.makedirs(ROOM, exist_ok=True); os.makedirs(FX, exist_ok=True)
    room = {
        'Deco_AlgaeColumn_A': [algae(7, i) for i in range(4)],
        'Deco_AlgaeColumn_B': [algae(23, i) for i in range(4)],
        'Deco_LightPlant': [light_plant(p) for p in (0.0, 0.5, 1.0, 0.5)],
        'Deco_LightPlant_Off': [light_plant(0.5, l) for l in (1.0, 0.5, 0.0)],
    }
    fx = {
        'Proj_AnomalocarisBubble': [bubble_proj(i) for i in range(4)],
        'Fx_AnomalocarisBubble_Pop': [bubble_pop(i) for i in range(4)],
    }
    for name, frames in room.items():
        save_sheet(os.path.join(ROOM, name + '.png'), frames)
    for name, frames in fx.items():
        save_sheet(os.path.join(FX, name + '.png'), frames)
    preview(os.path.join(OUT, 'vista_previa_sala_anomalocaris.png'), {**room, **fx}, BG, scale=4)
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    gif(os.path.join(OUT, 'gif', 'Deco_AlgaeColumn_A.gif'), room['Deco_AlgaeColumn_A'], 200, BG, scale=4)
    gif(os.path.join(OUT, 'gif', 'Deco_LightPlant.gif'), room['Deco_LightPlant'], 220, BG, scale=6)
    gif(os.path.join(OUT, 'gif', 'Proj_AnomalocarisBubble.gif'), fx['Proj_AnomalocarisBubble'], 120, BG, scale=8)
    print('Listo en', OUT)
