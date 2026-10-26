"""
generar_cuerpo_evolutivo.py - Primordia: cuerpo evolutivo del jugador (piloto).

El aspecto del jugador se monta con dos capas:
  1. Forma del cuerpo: mezcla de las 2 temáticas dominantes (la de la Boca y la
     de la mutación de mayor nivel entre las demás), en 3 versiones.
  2. Rasgos: una capa por mutación, en 3 niveles, que el juego coloca sobre
     cualquier forma con los puntos de anclaje que trae cada cuadro del cuerpo.

Piloto: formas WaspLarva (insecto) y Firefly (insecto + eléctrico), rasgos
Stinger (Aguijón) y ShockGland (Glándula de choque).

Reutiliza los otros generadores (Canvas, contorno #181425, rampas, ojos y brillo
de la célula, parámetros de cada cuadro de la célula, abdomen de avispa...):
  generar_evoluciones.py -> generar_menus.py -> generar_ui.py
  -> generar_mutaciones.py -> generar_jugador.py

Uso:
  python generar_cuerpo_evolutivo.py [carpeta_multimedia_temp] [carpeta_salida]
Por defecto: la carpeta de arriba y la carpeta de este script.

Para añadir una forma o un rasgo nuevos, mira las secciones TEMÁTICAS, FORMAS y
RASGOS (son datos) y el README.md.
"""
import importlib.util
import json
import math
import os
import sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, '..')
OUT = sys.argv[2] if len(sys.argv) > 2 else HERE

# --------------------------------------------- cargar los otros generadores
_argv = sys.argv
sys.argv = ['generar_evoluciones.py', ROOT, os.devnull]
_spec = importlib.util.spec_from_file_location('generar_evoluciones',
                                               os.path.join(ROOT, 'evolutions', 'generar_evoluciones.py'))
E = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(E)
sys.argv = _argv
M = E.M            # generar_mutaciones.py
G = E.G            # generar_menus.py (fuente pixel de la vista previa)
P = M.P            # generar_jugador.py

C, CA, OUTL = M.C, M.CA, M.OUTL
Canvas, body, features, rot = M.Canvas, M.body, M.features, M.rot
shade_v, ramp_idx, blob = M.shade_v, M.ramp_idx, M.blob
GREEN, RED, WHITE, PALE = M.GREEN, M.RED, M.WHITE, M.PALE
LIME, PURP, SHOCK = M.LIME, M.PURP, M.SHOCK
PURP_D = E.PURP_D
EYE = E.EYE
FACINGS = M.FACINGS
FRAMES = M.FRAMES            # parámetros por cuadro de la célula (balanceo)
PANIMS = M.PANIMS            # sprites de la célula (fila "Célula sola")
BG = C('#16352e')
frac = E.frac


# ================================================================ TEMÁTICAS
# Cada temática aporta sus rampas de 4 tonos (oscuro -> claro). Los materiales
# de las formas usan estas rampas por nombre.
THEMES = {
    'celula': dict(nombre='Célula', ramps={
        'membrana': GREEN['ramp'],
    }),
    'insecto': dict(nombre='Insecto', ramps={
        'quitina': ['#20142a', '#3a2438', '#5c3c4c', '#86626c'],   # quitina oscura (tira a morado)
        'ambar': ['#7a3a12', '#c0701e', '#eeaa36', '#ffe08a'],     # ámbar y ocre de avispa
        'ala': ['#6a90a8', '#a8d0e4', '#dcf4ff', '#ffffff'],       # alas traslúcidas
    }),
    'electrico': dict(nombre='Eléctrico', ramps={
        'elitro': ['#121634', '#222c5c', '#384c8e', '#6888c8'],    # élitros azul noche
        'luz': SHOCK,                                              # bioluminiscencia (Glándula)
        'cian': ['#1e6a8a', '#2ab0d0', '#6aecff', '#e0ffff'],
    }),
    'artropodo': dict(nombre='Artrópodo', ramps={
        'placa': ['#1e2433', '#36415a', '#5a6a86', '#8a9cb6'],     # placa gris azulada fría
        'acero': ['#2e3648', '#4e5c76', '#7a8ba6', '#b4c2d4'],     # brillo metálico apagado (sin blanco)
    }),
}
RAMPS = {}
for _t in THEMES.values():
    RAMPS.update(_t['ramps'])

# Versiones apagadas de cada rampa para la muerte (como PALE en la célula)
PALE_RAMPS = {
    'membrana': PALE['ramp'],
    'quitina': ['#4e4656', '#6e6676', '#948c9a', '#bcb4c0'],
    'ambar': ['#7e6e5a', '#a8987e', '#ccbea4', '#ece2cc'],
    'ala': ['#8a9aa4', '#b4c2ca', '#d8e2e6', '#f4f8fa'],
    'elitro': ['#4a5066', '#646c86', '#848ca6', '#aab2c8'],
    'luz': ['#6a7a8a', '#8a9aaa', '#b0bcc8', '#d8e0e8'],
    'cian': ['#6a7a8a', '#8a9aaa', '#b0bcc8', '#d8e0e8'],
    'placa': ['#4a4e58', '#666b76', '#8a8f98', '#b2b6bc'],
    'acero': ['#565b64', '#767b84', '#9ca0a8', '#c6c9ce'],
}
GLOWING = ('luz', 'cian')
LIME_PALE = ['#6e7a58', '#96a27a', '#c0caa4', '#e8eed6']     # Aguijón marchito
PURP_PALE = ['#3e3446', '#5a5064', '#7a7086', '#a49aae']          # rampas que laten con el pulso


def ramp(mat, tint=None):
    if tint == 'white':
        return ['#ffffff'] * 4
    if tint == 'red':
        return RED['ramp']
    if tint == 'pale':
        return PALE_RAMPS[mat]
    return RAMPS[mat]


def col(mat, i, tint=None, pulse=1):
    if mat in GLOWING and tint is None:
        i += pulse - 1
    return ramp(mat, tint)[max(0, min(3, i))]


EYE_F = '#c2c6d6'          # faceta de los ojos compuestos (sigue siendo claro)
EYE_HI = '#ffffff'


# ================================================================ MATERIALES
# Un material decide el color de cada píxel de una parte del cuerpo.
# q es un dict con: nx, ny (coordenadas normalizadas en la parte), v (luz),
# i (tono 0..3 según la luz), ax (eje del cuerpo: -1 atrás, +1 delante),
# dors (-1 vientre, +1 lomo), facing, tint, pulse y la parte (prm).
def m_membrana(q):
    """Membrana verde de la célula con una leve segmentación."""
    i = q['i']
    segs = q['prm'].get('segmentos', 0)
    if segs and abs(q['lat']) < 0.8:
        t = frac((q['ax'] + 1) * segs / 2 + 0.5)
        if t < 0.16:
            i -= 1
    return col('membrana', i, q['tint'])


def m_manchas_luz(q):
    """Membrana segmentada con manchas bioluminiscentes que laten."""
    for (sx, sy) in q['prm']['manchas'][q['facing']]:
        d = math.hypot(q['nx'] - sx, q['ny'] - sy)
        if d < 0.16:
            return col('luz', 2 if d < 0.08 else 1, q['tint'], q['pulse'])
    return m_membrana(q)


def m_bandas(q):
    """Verde con bandas de quitina que bajan desde el lomo."""
    nb = q['prm']['bandas']
    reach = q['prm'].get('alcance', -0.2)
    t = frac((q['ax'] + 1) * nb / 2 + 0.15 * q['dors'] ** 2)
    if q['dors'] > reach + 0.25 * math.sin(q['ax'] * 9) * 0.3 and t < 0.48 and abs(q['ax']) < 0.9:
        return col('quitina', q['i'] + (1 if t < 0.12 else 0), q['tint'])
    return m_membrana(q)


def m_placas(q):
    """Placas de quitina alternas (ámbar / oscura) con la membrana verde en las juntas."""
    nb = q['prm']['bandas']
    if q['dors'] < q['prm'].get('vientre', -0.5):         # vientre blando
        return m_membrana(q)
    s = (q['ax'] + 1) * nb / 2 + 0.25 * q['dors'] ** 2
    t = frac(s)
    if t < 0.16:
        return col('membrana', min(q['i'], 2), q['tint'])     # junta: asoma el verde
    k = int(math.floor(s)) % 2
    mat = 'ambar' if k == 0 else 'quitina'
    i = q['i'] + (1 if t < 0.38 else 0)                       # canto claro de cada placa
    return col(mat, i, q['tint'])


def m_cabeza(q):
    """Cabeza ámbar con la coronilla de quitina oscura."""
    if q['dors'] > q['prm'].get('corona', 0.45):
        return col('quitina', q['i'] + 1, q['tint'])
    return col('ambar', q['i'], q['tint'])


def m_cabeza_oscura(q):
    return col('quitina', q['i'] + 1, q['tint'])


def m_luz_trasera(q):
    """Delante verde; detrás, segmentos que brillan (luciérnaga)."""
    corte = q['prm'].get('corte', -0.15)
    if q['ax'] < corte and q['dors'] < q['prm'].get('techo', 2):
        nb = q['prm']['bandas']
        t = frac((q['ax'] + 1) * nb / 2)
        if t < 0.3:
            return col('elitro', q['i'] + 1, q['tint'])         # junta oscura
        return col('luz', min(2, q['i'] + 1), q['tint'], q['pulse'])
    return m_membrana(q)


def m_elitro(q):
    """Élitro azul noche, con la costura central vista desde arriba."""
    if q['facing'] == 'up' and abs(q['nx']) < 0.09:
        return col('elitro', 0, q['tint'])
    i = q['i']
    if q['facing'] == 'side' and q['ny'] > 0.55:
        i -= 1
    return col('elitro', i, q['tint'])


def m_pronoto(q):
    """Escudo ámbar con una mancha oscura en el centro."""
    if q['nx'] ** 2 * 1.6 + (q['ny'] + 0.15) ** 2 < 0.22:
        return col('quitina', q['i'] + 1, q['tint'])
    return col('ambar', q['i'], q['tint'])


def m_placas_sueltas(q):
    """Membrana verde con placas sueltas de quitina gris azulada en el lomo (Hormiga I).
    prm: bandas (placas a lo largo), umbral (desde dónde es lomo), ancho (franja central)."""
    p = q['prm']
    lomo = q['dors'] > p.get('umbral', 0.45) and (q['facing'] == 'side' or abs(q['lat']) < p.get('ancho', 0.5))
    if lomo:
        t = frac((q['ax'] + 1) * p['bandas'] / 2)
        if 0.22 < t < 0.78:
            return col('placa', q['i'] + (1 if t < 0.36 else 0), q['tint'])
    return m_membrana(q)


def m_quitina_azul(q):
    """Placas de quitina gris azulada con juntas oscuras y brillo metálico apagado.
    prm: bandas (0 = una sola placa), junta (ancho de la junta), vientre (por debajo asoma
    el verde), reflejo (juntas del lomo en ámbar), pulido (sube un tono: gáster brillante)."""
    p = q['prm']
    if q['dors'] < p.get('vientre', -9):
        return m_membrana(q)                                   # vientre blando: queda algo de verde
    i = q['i'] + p.get('pulido', 0)
    nb = p.get('bandas', 0)
    canto = False
    if nb:
        s = (q['ax'] + 1) * nb / 2 + 0.25 * q['dors'] ** 2
        t = frac(s)
        if t < p.get('junta', 0.14):
            if p.get('reflejo') and q['dors'] > 0 and q['i'] >= 1:
                return col('ambar', min(q['i'] + 1, 3), q['tint'])   # reflejo ámbar en la junta
            return col('placa', 0, q['tint'])                       # junta oscura
        canto = t < 0.34                                            # canto claro de cada placa
    if i >= 3:
        return col('acero', 2 + (1 if canto else 0), q['tint'])
    return col('placa', i + (1 if canto else 0), q['tint'])


def m_cabeza_azul(q):
    """Cabeza gris azulada; con 'corona' lleva un casco metálico con su junta (soldado)."""
    corona = q['prm'].get('corona')
    if corona is not None and q['dors'] > corona:
        if q['dors'] < corona + 0.14:
            return col('placa', 0, q['tint'])                  # junta del casco
        return col('acero', min(q['i'], 2), q['tint'])
    return m_quitina_azul(q)


MATERIALS = {
    'membrana': m_membrana, 'manchas_luz': m_manchas_luz, 'bandas': m_bandas, 'placas': m_placas,
    'cabeza': m_cabeza, 'cabeza_oscura': m_cabeza_oscura, 'luz_trasera': m_luz_trasera,
    'elitro': m_elitro, 'pronoto': m_pronoto,
    'placas_sueltas': m_placas_sueltas, 'quitina_azul': m_quitina_azul, 'cabeza_azul': m_cabeza_azul,
}


# =================================================================== FORMAS
# Cada parte es una elipse: pos[dir] = (dx, dy, rx, ry) respecto al centro
# (16, 16) en reposo y z[dir] el orden de dibujo (menor = detrás).
# rol: 'torso' (manda en los anclajes), 'cabeza' (lleva ojos y antenas) o
# 'cubierta' (élitros, escudos...). Si no hay cabeza, el torso hace de cabeza.
def parte(nombre, rol, mat, side, down, up, z=(1, 1, 1), **prm):
    return dict(nombre=nombre, rol=rol, mat=mat, pos=dict(side=side, down=down, up=up),
                z=dict(zip(FACINGS, z)), prm=prm)


MANCHAS_V1 = dict(side=[(-0.55, 0.15), (-0.2, 0.55), (0.3, 0.6), (-0.7, -0.35)],
                  down=[(-0.6, 0.35), (0.6, 0.35), (0.0, 0.7), (-0.25, -0.65)],
                  up=[(-0.5, 0.2), (0.5, 0.2), (0.0, 0.6), (0.05, -0.35)])

FORMS = {
    'WaspLarva': dict(nombre='Avispa larvaria', tematicas=('insecto', None), versiones={
        1: dict(
            partes=[parte('cuerpo', 'torso', 'membrana', (0, 0, 7.6, 6.6), (0, 0, 7.6, 6.6), (0, 0, 7.6, 6.6),
                          segmentos=4)],
            celula=True, ojos='faceta1',
            antenas=dict(tipo='curva', largo=4.0, punta='ambar'),
            bola=6.0),
        2: dict(
            partes=[parte('torax', 'torso', 'bandas', (-2.6, 0.6, 7.2, 7.6), (0, -1.6, 8.6, 7.4),
                          (0, 0.8, 8.6, 7.6), z=(0, 0, 1), bandas=3.2, segmentos=3),
                    parte('cabeza', 'cabeza', 'cabeza', (5.0, -1.6, 5.2, 5.0), (0, 3.2, 6.2, 5.0),
                          (0, -6.0, 5.6, 4.4), z=(1, 1, 0), corona=0.5)],
            ojos='faceta2', brillo=True,
            antenas=dict(tipo='curva', largo=6.5, punta='ambar'),
            bola=7.0, elastico=0.8),
        3: dict(
            partes=[parte('torax', 'torso', 'placas', (-3.2, 0.4, 8.8, 9.0), (0, -2.4, 10.0, 8.6),
                          (0, 1.0, 10.0, 8.8), z=(0, 0, 1), bandas=3.4, segmentos=3, vientre=-0.42),
                    parte('cabeza', 'cabeza', 'cabeza', (5.6, -2.4, 6.4, 6.2), (0, 3.6, 7.4, 6.0),
                          (0, -6.8, 6.4, 4.6), z=(1, 1, 0), corona=0.4)],
            ojos='faceta3', brillo=True,
            antenas=dict(tipo='acodada', largo=7.5, punta='ambar'),
            alas=dict(largo=8.0, ancho=2.3),
            bola=8.0, elastico=0.6),
    }),
    'Firefly': dict(nombre='Luciérnaga', tematicas=('insecto', 'electrico'), versiones={
        1: dict(
            partes=[parte('cuerpo', 'torso', 'manchas_luz', (0, 0, 7.6, 6.6), (0, 0, 7.6, 6.6), (0, 0, 7.6, 6.6),
                          segmentos=4, manchas=MANCHAS_V1)],
            celula=True, ojos='faceta1',
            antenas=dict(tipo='fina', largo=5.0, punta='luz'),
            bola=6.0),
        2: dict(
            partes=[parte('torax', 'torso', 'luz_trasera', (-3.0, 1.0, 9.4, 6.4), (0, -2.2, 7.6, 7.8),
                          (0, 1.2, 7.6, 8.0), z=(0, 0, 0), bandas=5.0, segmentos=3, corte=-0.25),
                    parte('elitros', 'cubierta', 'elitro', (0.0, -1.8, 6.0, 4.4), (0, -5.0, 6.8, 4.4),
                          (0, -1.8, 6.8, 5.0), z=(1, 1, 1)),
                    parte('cabeza', 'cabeza', 'cabeza', (6.2, 0.2, 4.4, 4.4), (0, 3.4, 5.4, 4.6),
                          (0, -6.8, 4.8, 3.6), z=(2, 2, -1), corona=0.55)],
            ojos='faceta2', brillo=True,
            antenas=dict(tipo='fina', largo=7.5, punta='luz'),
            bola=7.0, elastico=0.8),
        3: dict(
            partes=[parte('torax', 'torso', 'luz_trasera', (-3.0, 1.4, 9.6, 6.8), (0, -2.0, 8.6, 9.4),
                          (0, 2.0, 8.4, 9.0), z=(0, 0, 0), bandas=6.0, segmentos=3, corte=-0.3),
                    parte('elitros', 'cubierta', 'elitro', (-0.6, -1.2, 7.8, 5.2), (0, -4.4, 8.0, 6.6),
                          (0, -1.4, 8.2, 6.6), z=(1, 1, 1)),
                    parte('pronoto', 'cubierta', 'pronoto', (4.6, -1.6, 4.2, 3.8), (0, 2.2, 6.4, 4.2),
                          (0, -6.2, 5.8, 3.4), z=(2, 2, 2)),
                    parte('cabeza', 'cabeza', 'cabeza_oscura', (7.6, 1.4, 3.6, 3.6), (0, 5.4, 5.0, 3.8),
                          (0, -8.2, 4.2, 2.8), z=(3, 3, -1))],
            ojos='faceta2', brillo=True,
            antenas=dict(tipo='fina', largo=7.5, punta='luz'),
            bola=8.0, elastico=0.6),
    }),
}

# Hormiga. Roles nuevos: 'lobulo' (gáster) y 'cintura' (pecíolo); se dibujan como cualquier parte.
# Claves nuevas de versión:
#   anclajes   = {anclaje: parte}  el anclaje sale de esa parte en vez del torso
#   mandibulas = dict(largo, gruesa, punta)
#   munones    = dict(largo)       muñones de patas en los costados del torso
# Sin patas largas: las pondrá el rasgo de las Patas articuladas en 'left' y 'right' (costados del tórax).
FORMS['Ant'] = dict(nombre='Hormiga', tematicas=('insecto', 'artropodo'), compartida=2, versiones={
    1: dict(
        # dos lóbulos: el delantero (cabeza y tórax aún fundidos) y el gáster
        partes=[parte('gaster', 'lobulo', 'placas_sueltas', (-4.2, 0.8, 4.8, 4.6), (0, -4.4, 5.6, 4.0),
                      (0, 2.2, 6.2, 5.0), z=(0, 0, 1), bandas=2, segmentos=3, umbral=0.5),
                parte('frente', 'torso', 'placas_sueltas', (1.8, 0, 6.6, 5.8), (0, 1.2, 6.8, 5.6),
                      (0, -4.0, 6.0, 4.6), z=(1, 1, 0), bandas=3, segmentos=3, umbral=0.45)],
        celula=True, ojos='faceta1',
        antenas=dict(tipo='acodada', largo=4.5, punta='placa', tallo='placa'),
        munones=dict(largo=1.5),
        anclajes=dict(abdomen='gaster', organ='gaster'),
        bola=6.0),
    2: dict(
        partes=[parte('gaster', 'lobulo', 'quitina_azul', (-6.6, 0.6, 5.2, 4.8), (0, -6.2, 5.2, 4.0),
                      (0, 3.4, 5.4, 4.6), z=(0, 0, 3), bandas=3, segmentos=3, vientre=-0.3),
                parte('peciolo', 'cintura', 'quitina_azul', (-1.0, 1.6, 1.3, 1.5), (0, -2.8, 1.4, 1.2),
                      (0, -0.6, 1.4, 1.2), z=(1, 1, 2), sin_brillo=True),
                parte('torax', 'torso', 'quitina_azul', (3.2, 0.6, 3.0, 2.8), (0, -0.2, 3.0, 2.6),
                      (0, -3.2, 3.0, 2.4), z=(2, 2, 1), bandas=2, segmentos=2, vientre=-0.35),
                parte('cabeza', 'cabeza', 'cabeza_azul', (7.6, -1.2, 4.0, 3.8), (0, 4.0, 4.6, 4.0),
                      (0, -7.0, 4.2, 3.2), z=(3, 3, 0), vientre=-0.5)],
        ojos='faceta2', brillo=True,
        antenas=dict(tipo='acodada', largo=6.5, punta='acero', tallo='placa'),
        mandibulas=dict(largo=3.0, punta='acero'),
        anclajes=dict(abdomen='gaster', organ='gaster'),
        bola=7.0, elastico=0.7),
    3: dict(
        # soldado: cabeza grande con casco, cintura muy fina, gáster pulido; se aplasta poco (firme)
        partes=[parte('gaster', 'lobulo', 'quitina_azul', (-8.4, 0.8, 5.2, 5.0), (0, -6.4, 5.8, 4.2),
                      (0, 3.8, 6.0, 5.0), z=(0, 0, 3), bandas=3.4, segmentos=3, vientre=-0.7, junta=0.12,
                      pulido=1),
                parte('peciolo', 'cintura', 'quitina_azul', (-2.6, 2.2, 1.2, 1.4), (0, -2.6, 1.3, 1.1),
                      (0, -0.4, 1.3, 1.1), z=(1, 1, 2), sin_brillo=True),
                parte('torax', 'torso', 'quitina_azul', (1.4, 1.0, 3.0, 3.0), (0, 0.0, 3.4, 2.8),
                      (0, -3.4, 3.4, 2.6), z=(2, 2, 1), bandas=2.4, segmentos=2, vientre=-0.6, reflejo=True),
                parte('cabeza', 'cabeza', 'cabeza_azul', (6.2, -0.6, 4.8, 4.8), (0, 4.6, 6.0, 4.6),
                      (0, -7.6, 5.0, 3.8), z=(3, 3, 0), corona=0.3)],
        ojos='faceta3', brillo=True,
        antenas=dict(tipo='acodada', largo=8.0, punta='ambar', tallo='placa'),
        mandibulas=dict(largo=4.0, gruesa=True, punta='ambar'),
        anclajes=dict(abdomen='gaster', organ='gaster'),
        bola=8.0, elastico=0.4),
})

# Forma según las 2 temáticas dominantes (Boca, segunda). None = sin segunda.
FORM_BY_THEMES = {('insecto', None): 'WaspLarva', ('insecto', 'electrico'): 'Firefly',
                  ('insecto', 'artropodo'): 'Ant'}


# ================================================================ MUTACIONES
SLOTS = ('Boca', 'Extremidades', 'Piel', 'Órgano')
MUTATIONS = {
    'Stinger': dict(nombre='Aguijón', ranura='Boca', tematica='insecto', rasgo='Stinger', linaje='Tóxico'),
    'ShockGland': dict(nombre='Glándula de choque', ranura='Órgano', tematica='electrico', rasgo='ShockGland',
                       linaje='Acorazado'),
    'JointedLegs': dict(nombre='Patas articuladas', ranura='Extremidades', tematica='artropodo', rasgo=None,
                        linaje='Acorazado'),
}


def resolve(levels):
    """levels = {'Stinger': 3, 'ShockGland': 1} -> (forma, versión, [(rasgo, nivel), ...]).
    Misma regla que usará el juego. Sin mutaciones -> (None, 0, []): la célula.
    Las mutaciones sin rasgo (rasgo=None) cuentan para la forma pero no añaden capa."""
    if not levels:
        return None, 0, []
    by_slot = {MUTATIONS[m]['ranura']: m for m in levels}
    first = by_slot.get('Boca') or max(levels, key=lambda m: levels[m])
    others = [m for m in levels if m != first]
    # la de mayor nivel; si empatan, gana la ranura que va antes en SLOTS
    second = max(others, key=lambda m: (levels[m], -SLOTS.index(MUTATIONS[m]['ranura']))) if others else None
    t1 = MUTATIONS[first]['tematica']
    t2 = MUTATIONS[second]['tematica'] if second else None
    if t2 == t1:
        t2 = None
    ver = max(levels[first], levels[second] if second else 0)
    traits = [(MUTATIONS[m]['rasgo'], levels[m])
              for m in sorted(levels, key=lambda m: SLOTS.index(MUTATIONS[m]['ranura']))
              if MUTATIONS[m].get('rasgo')]
    return FORM_BY_THEMES[(t1, t2)], ver, traits


# ============================================================ DIBUJO: CUERPO
def blend(a, b, t):
    """Mezcla dos colores RGBA (t = cuánto de b)."""
    return tuple(int(round(a[k] * (1 - t) + b[k] * t)) for k in range(3)) + (255,)


def put_glass(cv, x, y, c, a):
    """Píxel traslúcido: se mezcla con lo que hay debajo (o queda con alfa)."""
    x, y = int(round(x)), int(round(y))
    if not (0 <= x < cv.n and 0 <= y < cv.n):
        return
    c = C(c) if isinstance(c, str) else c
    old = cv.px.get((x, y))
    if old is None or old[3] == 0:
        cv.px[(x, y)] = c[:3] + (a,)
    elif old[3] == 255:
        cv.px[(x, y)] = blend(old, c, a / 255)
    else:
        cv.px[(x, y)] = c[:3] + (max(a, old[3]),)


def glass_layer(dst, src, alpha_outline=150):
    """Pega una capa traslúcida (alas) con su contorno también traslúcido."""
    src.outline()
    for (x, y), c in src.px.items():
        a = alpha_outline if c == OUTL else c[3]
        put_glass(dst, x, y, c, a)


class Pose:
    """Coloca las partes de una versión para un cuadro (aplastado, bote, inercia)."""

    def __init__(s, spec, facing, squash=0.0, bob=0, lean=0.0, grow=0.0, n=32):
        s.spec, s.facing, s.n, s.lean = spec, facing, n, lean
        c = n / 2
        squash *= spec.get('elastico', 1.0)       # la quitina se aplasta menos que la membrana
        kx, ky = 1 + 0.12 * squash, 1 - 0.12 * squash
        parts = spec['partes']
        bottom = max(p['pos'][facing][1] + p['pos'][facing][3] for p in parts)
        top = min(p['pos'][facing][1] - p['pos'][facing][3] for p in parts)
        s.env_ry = (bottom - top) / 2
        base = c + bottom + 0.5 + bob                  # se apoya abajo, como la célula
        s.place = {}
        for p in parts:
            dx, dy, rx, ry = p['pos'][facing]
            g = grow * 0.8
            rx2, ry2 = max(1.0, (rx + g) * kx), max(1.0, (ry + g) * ky)
            cy = base - (bottom - dy) * ky - g * 0.5
            s.place[p['nombre']] = (c + dx * kx, cy, rx2, ry2)
        s.gcy = base - s.env_ry * ky
        s.c = c
        s.torso = next(p for p in parts if p['rol'] == 'torso')
        s.head = next((p for p in parts if p['rol'] == 'cabeza'), s.torso)

    def shift(s, y):
        """Desplazamiento por inercia (lean) a la altura y, como body()."""
        return s.lean * max(0.0, (s.gcy - y) / max(s.env_ry, 1))

    def T(s):
        return s.place[s.torso['nombre']]

    def H(s):
        return s.place[s.head['nombre']]


def local_axes(facing, nx, ny):
    """(ax, dors, lat): eje del cuerpo (-1 atrás..1 delante), lomo y costado."""
    if facing == 'side':
        return nx, -ny, ny
    if facing == 'down':
        return ny, -ny, nx
    return -ny, 1 - 0.6 * abs(nx), nx


def draw_part(cv, pose, p, tint, pulse):
    cx, cy, rx, ry = pose.place[p['nombre']]
    fn = MATERIALS[p['mat']]
    x0, x1 = int(cx - rx - abs(pose.lean) - 2), int(cx + rx + abs(pose.lean) + 2)
    for y in range(max(0, int(cy - ry - 1)), min(cv.n, int(cy + ry + 2))):
        for x in range(max(0, x0), min(cv.n, x1 + 1)):
            px, py = x + .5, y + .5
            sx = px - cx - pose.shift(py)
            nx, ny = sx / rx, (py - cy) / ry
            if nx * nx + ny * ny > 1:
                continue
            v = shade_v(nx, ny)
            ax, dors, lat = local_axes(pose.facing, nx, ny)
            q = dict(nx=nx, ny=ny, v=v, i=ramp_idx(v), ax=ax, dors=dors, lat=lat, facing=pose.facing,
                     tint=tint, pulse=pulse, prm=p['prm'])
            c = fn(q)
            cv.px[(x, y)] = C(c) if isinstance(c, str) else c


def line_px(cv, pts, colfn, step=0.3):
    """Trazo de 1 px por una polilínea."""
    for k, ((x0, y0), (x1, y1)) in enumerate(zip(pts, pts[1:])):
        L = math.hypot(x1 - x0, y1 - y0)
        nst = max(1, int(L / step))
        for s in range(nst + 1):
            t = s / nst
            cv.put(x0 + (x1 - x0) * t - .5, y0 + (y1 - y0) * t - .5, colfn(k, t))


def antenna_path(kind, base, length, out, up_dir, sway, open_=0.0):
    """Puntos de una antena. out = hacia dónde se abre (+1/-1 en x)."""
    bx, by = base
    if kind == 'acodada':
        a1 = -math.pi / 2 + out * (0.25 + sway + open_)
        e = (bx + math.cos(a1) * length * 0.5, by + math.sin(a1) * length * 0.5)
        a2 = a1 + out * 1.15
        t = (e[0] + math.cos(a2) * length * 0.5, e[1] + math.sin(a2) * length * 0.5)
        return [base, e, t]
    pts = [base]
    bend = 0.7 if kind == 'curva' else 0.45
    a0 = -math.pi / 2 + out * (0.35 + sway + open_)
    seg = length / 4
    x, y = bx, by
    for k in range(4):
        a = a0 + out * bend * k / 3
        x, y = x + math.cos(a) * seg, y + math.sin(a) * seg
        pts.append((x, y))
    return pts


def draw_antennae(cv, pose, spec, tint, pulse, sway=0.0):
    an = spec.get('antenas')
    if not an:
        return
    hx, hy, hrx, hry = pose.H()
    hx += pose.shift(hy - hry)
    L = an['largo']
    tipc = col(an['punta'], 2, tint, pulse)
    tallo = an.get('tallo', 'quitina')                 # rampa del tallo
    shaft = lambda far: (lambda k, t: col(tallo, 1 if far else 2, tint))
    if pose.facing == 'side':
        bases = [((hx + hrx * 0.05, hy - hry * 0.88), True), ((hx + hrx * 0.4, hy - hry * 0.8), False)]
        for (b, far) in bases:
            pts = antenna_path(an['tipo'], b, L * (0.9 if far else 1), 1, -1, sway + (0.15 if far else 0))
            line_px(cv, pts, shaft(far))
            cv.put(pts[-1][0] - .5, pts[-1][1] - .5, tipc)
    else:
        for s in (-1, 1):
            b = (hx + s * hrx * 0.38, hy - hry * 0.85)
            # de espaldas las antenas quedan detrás de la cabeza y se ven más cortas
            Lk = min(L * 0.5, 3.8) if pose.facing == 'up' else L * 1.15
            pts = antenna_path(an['tipo'], b, Lk, s, -1, sway * s, 0.3 if pose.facing == 'down' else 0.55)
            line_px(cv, pts, shaft(True) if pose.facing == 'up' else (lambda k, t: col(tallo, 3, tint)))
            cv.put(pts[-1][0] - .5, pts[-1][1] - .5, tipc)


def draw_mandibles(cv, pose, spec, tint, sway=0.0):
    """Mandíbulas en la parte delantera de la cabeza; se abren un poco con el balanceo."""
    md = spec.get('mandibulas')
    if not md:
        return
    hx, hy, hrx, hry = pose.H()
    hx += pose.shift(hy)
    L, abre = md['largo'], md.get('abre', 0.0) + sway * 1.5
    punta = md.get('punta', 'acero')

    def pinza(b, a0, curl, Lk, far, grosor=None):
        pts, (x, y) = [b], b
        for k in range(3):
            a = a0 + curl * k / 2
            x, y = x + math.cos(a) * Lk / 3, y + math.sin(a) * Lk / 3
            pts.append((x, y))
        if grosor and md.get('gruesa'):                    # segunda fila más oscura: mandíbula maciza
            gx, gy = grosor
            line_px(cv, [(px + gx, py + gy) for px, py in pts[:3]], lambda k, t: col('placa', 0 if far else 1, tint))
        line_px(cv, pts, lambda k, t: col('placa', 1 if far else 2, tint))
        cv.put(pts[-1][0] - .5, pts[-1][1] - .5, col(punta, 2 if far else 3, tint))

    if pose.facing == 'side':
        bx = hx + hrx * 0.72
        pinza((bx + 0.5, hy + hry * 0.3), 0.35 - abre, -1.2, L * 0.85, True)       # la lejana
        pinza((bx, hy + hry * 0.45), 0.5 - abre, -1.3, L, False, (0, 1))
    elif pose.facing == 'down':
        for s in (-1, 1):
            pinza((hx + s * hrx * 0.32, hy + hry * 0.72), math.pi / 2 - s * (0.55 + abre), s * 1.4, L, False, (s, 0))
    else:
        # de espaldas solo asoman las puntas por encima de la cabeza
        for s in (-1, 1):
            pinza((hx + s * hrx * 0.3, hy - hry * 0.75), -math.pi / 2 + s * (0.5 + abre), -s * 1.3, L * 0.6, True)


def draw_stubs(cv, pose, spec, tint):
    """Muñones de patas apenas insinuados en los costados del torso (Hormiga I)."""
    mu = spec.get('munones')
    if not mu:
        return
    tx, ty, trx, tr = pose.T()
    if pose.facing == 'side':
        angs, L, ky = (math.pi / 2 - 0.5, math.pi / 2 + 0.6), mu.get('largo', 1.5) * 0.7, 1.4   # por debajo
    else:
        angs, L, ky = (0.15, 0.6, math.pi - 0.15, math.pi - 0.6), mu.get('largo', 1.5), 0.7     # dos a cada lado
    for a in angs:
        ex, ey = tx + math.cos(a) * trx, ty + math.sin(a) * tr          # borde del torso
        ex += pose.shift(ey)
        dx, dy = math.cos(a), math.sin(a) * ky
        k = math.hypot(dx, dy)
        dx, dy = dx / k, dy / k
        line_px(cv, [(ex - dx, ey - dy), (ex + dx * L, ey + dy * L)],
                lambda s, t: col('placa', 2 if t < 0.5 else 1, tint))


EYE_MAPS = {
    # E claro, F faceta, P pupila, H brillo. Mirando a la derecha (side).
    'faceta2': ['HEF', 'EFE', 'FEP', 'EFP'],
    'faceta3': ['.HE.', 'HEFE', 'EFEF', 'FEFP', 'EFPP', '.EF.'],
}
EYE_MAPS_DOWN = {          # ojo de la derecha de la pantalla; el otro va en espejo
    'faceta2': ['HEF', 'EFE', 'PFE'],
    'faceta3': ['.HE.', 'HEFE', 'EFEF', 'PPFE', '.EF.'],
}


def eye_map(cv, x0, y0, rows, eyes, mirror=False, clip=False):
    cols = {'E': EYE, 'F': EYE_F, 'H': EYE_HI, 'P': OUTL}
    h, w = len(rows), len(rows[0])
    if eyes in ('closed', 'x'):
        mid = y0 + h // 2
        if eyes == 'closed':
            for dx in range(w):
                cv.put(x0 + dx, mid, OUTL)
        else:
            for dx, dy in ((0, -1), (2, -1), (1, 0), (0, 1), (2, 1)):
                cv.put(x0 + dx + (w - 3) / 2, mid + dy, OUTL)
        return
    for j, r in enumerate(rows):
        for i, ch in enumerate(r[::-1] if mirror else r):
            if ch == '.':
                continue
            c = cols[ch]
            if eyes == 'wide' and ch == 'P':
                c = EYE
            put = cv.put_in if clip else cv.put
            put(x0 + i, y0 + j, c)
    if eyes == 'wide':
        cv.put(x0 + w // 2, y0 + h // 2, OUTL)


def draw_eyes(cv, pose, spec, eyes):
    kind = spec['ojos']
    if kind == 'faceta1' or pose.facing == 'up' or eyes == 'none':
        return
    hx, hy, hrx, hry = pose.H()
    hx += pose.shift(hy)
    rows = EYE_MAPS[kind]
    w, h = len(rows[0]), len(rows)
    if pose.facing == 'side':
        x0 = round(hx + hrx * 0.1)
        y0 = round(hy - hry * 0.5)
        eye_map(cv, x0, y0, rows, eyes)
        # el ojo lejano asoma por delante de la cabeza
        far = [r[-2:] for r in rows]
        eye_map(cv, round(hx + hrx * 0.62), y0 - 1, far, eyes, clip=True)
    else:
        rows = EYE_MAPS_DOWN[kind]
        w, h = len(rows[0]), len(rows)
        y0 = round(hy - hry * 0.45)
        for s in (-1, 1):
            x0 = round(hx + s * hrx * 0.5 - w / 2 + (0.5 if s > 0 else -0.5))
            eye_map(cv, x0, y0, rows, eyes, mirror=s < 0)


def draw_shine(cv, pose, p, tint):
    """El brillo de la célula (2 píxeles arriba a la izquierda) en cada parte."""
    cx, cy, rx, ry = pose.place[p['nombre']]
    hi = GREEN['hi'] if tint != 'red' else RED['hi']
    hi2 = GREEN['hi2'] if tint is None else ('#ffffff' if tint == 'white' else RED['hi2'] if tint == 'red' else '#e8ffd8')
    x, y = cx - rx * 0.55 + pose.shift(cy - ry * 0.6), cy - ry * 0.62
    cv.put_in(x, y, hi)
    cv.put_in(x + 1, y, hi2)


def draw_wings(cv, pose, spec, tint, flap, front):
    """Alitas vestigiales traslúcidas que vibran al moverse."""
    al = spec.get('alas')
    if not al:
        return
    tx, ty, trx, tryy = pose.T()
    L, W = al['largo'], al['ancho']
    cols = ramp('ala', tint)
    if pose.facing == 'side':
        jx = tx + trx * 0.35 + pose.shift(ty - tryy)            # junto a la cabeza, en el lomo
        roots = [((jx + 1, ty - tryy * 0.82), math.pi + 0.55 - flap, 0.85),   # la lejana
                 ((jx, ty - tryy * 0.72), math.pi + 0.3 - flap * 0.8, 1.0)]
    elif pose.facing == 'down':
        roots = [((tx + s * trx * 0.3, ty - tryy * 0.6), -math.pi / 2 + s * (1.2 + flap), 1.0) for s in (-1, 1)]
    else:
        # plegadas sobre el lomo, en V hacia atrás
        roots = [((tx + s * 1.2, ty - tryy * 0.8), math.pi / 2 - s * (0.35 + flap * 0.5), 1.0) for s in (-1, 1)]
    for (rx0, ry0), a, k in roots:
        wc = Canvas(cv.n)
        d = (math.cos(a), math.sin(a))
        nrm = (-d[1], d[0])
        Lk, Wk = L * k, W * k
        for y in range(cv.n):
            for x in range(cv.n):
                px, py = x + .5 - rx0, y + .5 - ry0
                u, v = px * d[0] + py * d[1], px * nrm[0] + py * nrm[1]
                if u < 0 or u > Lk:
                    continue
                t = u / Lk
                w = Wk * (math.sqrt(max(0.0, 1 - ((t - 0.62) / 0.62) ** 2)) if t > 0.15 else 0.45)
                if abs(v) > w:
                    continue
                if abs(v + w * 0.15) < 0.45 and 0.15 < t < 0.8:
                    c, a_ = cols[1], 200              # nervio
                elif t > 0.75 or abs(v) > w - 0.8:
                    c, a_ = cols[3], 190              # borde claro
                else:
                    c, a_ = cols[2], 185
                wc.px[(x, y)] = CA(c, a_)
        glass_layer(cv, wc, 120)


def draw_ball(spec, angle, grow, n, tint, pulse):
    """Esquiva: el cuerpo enrollado en una bola que rueda (los rasgos se ocultan)."""
    cv = Canvas(n)
    c = n / 2
    r = spec['bola'] + grow
    cx, cy = c, c + 0.5 + spec['bola'] - r
    mats = [p['mat'] for p in spec['partes']]
    for y in range(n):
        for x in range(n):
            nx, ny = (x + .5 - cx) / r, (y + .5 - cy) / (r * 0.95)
            if nx * nx + ny * ny > 1:
                continue
            i = ramp_idx(shade_v(nx, ny))
            ux, uy = rot(nx, ny, -angle)
            t = frac((ux + 1) * 2.0)
            if 'placas' in mats:
                c_ = col('membrana', min(i, 2), tint) if t < 0.18 else col('ambar' if int((ux + 1) * 2) % 2 else 'quitina', i + (1 if t < 0.4 else 0), tint)
            elif 'bandas' in mats:
                c_ = col('quitina', i, tint) if (t < 0.45 and uy < 0.3) else col('membrana', i, tint)
            elif 'elitro' in mats:
                c_ = col('elitro', i, tint) if uy < 0.35 else (col('luz', i + 1, tint, pulse) if t > 0.4 else col('membrana', i, tint))
            elif 'quitina_azul' in mats:
                if uy > 0.6:
                    c_ = col('membrana', i, tint)                         # vientre verde
                elif t < 0.18:
                    c_ = col('placa', 0, tint)                            # junta oscura
                else:
                    c_ = col('acero', 2, tint) if i >= 3 else col('placa', i + (1 if t < 0.4 else 0), tint)
            else:
                c_ = col('membrana', i - (1 if t < 0.14 else 0), tint)
            cv.px[(x, y)] = C(c_)
    if spec.get('celula'):
        pal = {None: GREEN, 'white': WHITE, 'red': RED, 'pale': PALE}[tint]
        features(cv, cx, cy, r, r, pal, 'side', angle, 'none')
    else:
        x, y = rot(-0.55 * r, -0.6 * r, angle)
        cv.put_in(cx + x, cy + y, GREEN['hi'])
        cv.put_in(cx + x + 1, cy + y, GREEN['hi2'])
    cv.outline()
    return cv


def draw_body(form, ver, facing='side', squash=0.0, bob=0, lean=0.0, pal=GREEN, angle=0.0, eyes='open',
              grow=0.0, n=32, tint=None, pulse=1, flap=0.0, sway=0.0):
    """Un cuadro del cuerpo. Acepta los mismos parámetros que cell() del jugador."""
    spec = FORMS[form]['versiones'][ver]
    if tint is None:
        tint = {id(WHITE): 'white', id(RED): 'red', id(PALE): 'pale'}.get(id(pal))
    if eyes == 'none':
        return draw_ball(spec, angle, grow, n, tint, pulse)
    pose = Pose(spec, facing, squash, bob, lean, grow, n)
    cv = Canvas(n)
    if facing == 'down':
        draw_wings(cv, pose, spec, tint, flap, False)
    parts = sorted(spec['partes'], key=lambda p: p['z'][facing])
    for p in parts:
        pc = Canvas(n)
        if p is pose.head:
            draw_antennae(pc, pose, spec, tint, pulse, sway)
            draw_mandibles(pc, pose, spec, tint, sway)
        if p is pose.torso:
            draw_stubs(pc, pose, spec, tint)
        draw_part(pc, pose, p, tint, pulse)
        if spec.get('celula'):
            if p is pose.head:
                # versión 1: sigue siendo la célula, con su núcleo, brillo y ojos
                cx, cy, rx, ry = pose.place[p['nombre']]
                pal_ = {None: GREEN, 'white': WHITE, 'red': RED, 'pale': PALE}[tint]
                features(pc, cx + lean * 0.5, cy, rx * 6.5 / 7.6, ry * 6 / 6.6, pal_, facing, angle, eyes)
                if facing != 'up' and eyes in ('open', 'wide'):
                    k = rx / 7.6
                    ex = {'side': (0, 3), 'down': (-3, 1)}[facing]
                    for e in ex:                                   # empieza a facetarse
                        ox, oy = cx + lean * 0.5 + e * k, cy - 2.5 * (ry / 6.6)
                        pc.put(ox + (0 if facing == 'side' else 1), oy, EYE_F)
                        pc.put(ox, oy + 2 if facing == 'side' else oy + 1, EYE_F)
            else:
                draw_shine(pc, pose, p, tint)                     # el otro lóbulo: solo el brillo
        else:
            if p is pose.head:
                draw_eyes(pc, pose, spec, eyes)
            if not p['prm'].get('sin_brillo') and (spec.get('brillo') and p['rol'] != 'torso'
                                                   or (p['rol'] == 'torso' and facing == 'up')):
                draw_shine(pc, pose, p, tint)
        pc.outline()
        for k_, c_ in pc.px.items():
            cv.px[k_] = c_
    if facing != 'down':
        draw_wings(cv, pose, spec, tint, flap, True)
    return cv


# -------------------------------------------------------------- anclajes
ANCHORS = ('head', 'back', 'abdomen', 'organ', 'left', 'right', 'mouth')
ORDER = {
    'side': dict(head='front', back='front', abdomen='behind', organ='front', left='behind', right='front',
                 mouth='front'),
    'down': dict(head='front', back='behind', abdomen='behind', organ='front', left='behind', right='behind',
                 mouth='front'),
    'up': dict(head='behind', back='front', abdomen='front', organ='front', left='behind', right='behind',
               mouth='behind'),
}


def anchors_of(form, ver, facing, squash=0.0, bob=0, lean=0.0, grow=0.0, n=32, **_):
    spec = FORMS[form]['versiones'][ver]
    pose = Pose(spec, facing, squash, bob, lean, grow, n)
    hx, hy, hrx, hry = pose.H()
    over = spec.get('anclajes', {})

    def E(k):
        """Elipse de la que sale el anclaje k: el torso, salvo que la versión diga otra parte."""
        return pose.place[over[k]] if k in over else pose.T()
    (bx, by, brx, bry), (ax_, ay, arx, ary), (ox, oy, orx, ory) = E('back'), E('abdomen'), E('organ')
    (lx, ly, lrx, lry), (rx_, ry_, rrx, rry) = E('left'), E('right')
    if facing == 'side':
        a = dict(head=(hx, hy), back=(bx + brx * 0.1, by - bry * 0.8), abdomen=(ax_ - arx * 0.78, ay + ary * 0.12),
                 organ=(ox + orx * 0.12, oy + ory * 0.3), left=(lx + lrx * 0.05 - 1, ly + lry * 0.45),
                 right=(rx_ + rrx * 0.15, ry_ + rry * 0.7), mouth=(hx + hrx + 1, hy + hry * 0.3))
    elif facing == 'down':
        a = dict(head=(hx, hy), back=(bx, by - bry * 0.75), abdomen=(ax_ + arx * 0.35, ay - ary * 0.35),
                 organ=(ox, oy - ory * 0.15 if pose.head is not pose.torso else oy + ory * 0.3),
                 right=(rx_ - rrx * 0.85, ry_ + rry * 0.25), left=(lx + lrx * 0.85, ly + lry * 0.25),
                 mouth=(hx, hy + hry + 1))
    else:
        a = dict(head=(hx, hy), back=(bx, by - bry * 0.25), abdomen=(ax_ + arx * 0.1, ay + ary * 0.5),
                 organ=(ox, oy + ory * 0.15), right=(rx_ + rrx * 0.85, ry_ + rry * 0.25),
                 left=(lx - lrx * 0.85, ly + lry * 0.25), mouth=(hx, hy - hry - 1))
    out = {}
    for k in ANCHORS:
        x, y = a[k]
        out[k] = [int(math.floor(x + pose.shift(y))), int(math.floor(y))]
    return out


# ================================================= ANIMACIONES DEL CUERPO
MS = {'Idle': 167, 'Move': 100, 'Dodge': 50, 'Hurt': 100, 'Death': 100, 'Attack': 88, 'Activate': 80}
PULSE_IDLE = (0, 1, 2, 1)


def frame_extra(anim, i):
    """Lo que añade la forma a cada cuadro de la célula: pulso de luz, aleteo y antenas."""
    if anim == 'Idle':
        return dict(pulse=PULSE_IDLE[i], flap=(0.0, 0.1, 0.15, 0.1)[i], sway=(0.0, 0.05, 0.1, 0.05)[i])
    if anim == 'Move':
        return dict(pulse=1, flap=(0.5, -0.3, 0.5, -0.3)[i], sway=(-0.15, -0.05, 0.12, 0.0)[i])
    return dict(pulse=1)


def body_anims(form, ver):
    anims, anchors = {}, {}
    for name, params in FRAMES.items():
        anim = name.split('_')[0]
        frs, ans = [], []
        for i, p in enumerate(params):
            kw = dict(p)
            kw.update(frame_extra(anim, i))
            frs.append(draw_body(form, ver, **kw))
            ans.append(anchors_of(form, ver, **kw))
        anims[name], anchors[name] = frs, ans
    anims['Death'], anchors['Death'] = body_death(form, ver)
    return anims, anchors


def shard(cv, x, y, a, mats, tint=None):
    """Trozo de quitina que sale volando."""
    for dx in (-1, 0, 1):
        for dy in (0, 1):
            xx, yy = rot(dx, dy, a)
            cv.put(x + xx, y + yy, col(mats, 2 if dy == 0 else 1, tint))
    xx, yy = rot(-1, 0, a)
    cv.put(x + xx, y + yy, col(mats, 3, tint))


def body_death(form, ver, n=48):
    """Se pone blanco, se hincha pálido y revienta en gotas y trozos de quitina."""
    c = n / 2
    spec = FORMS[form]['versiones'][ver]
    fr, an = [], []
    seq = [dict(facing='side', squash=-0.5, pal=WHITE, eyes='wide'),
           dict(facing='down', squash=0, pal=PALE, eyes='x', grow=1),
           dict(facing='down', squash=-0.3, bob=-1, pal=PALE, eyes='x', grow=2)]
    for p in seq:
        fr.append(draw_body(form, ver, n=n, pulse=0, **p))
        an.append(anchors_of(form, ver, n=n, **p))
    mats = [p_['mat'] for p_ in spec['partes']]
    shard_mats = ['membrana']
    if ver >= 2:
        if any(m in mats for m in ('quitina_azul', 'cabeza_azul')):
            shard_mats = ['placa', 'acero']
        else:
            shard_mats = ['ambar', 'quitina'] if 'elitro' not in mats else ['elitro', 'ambar']
    glow = any(m in mats for m in ('manchas_luz', 'luz_trasera'))
    k_ = spec['bola'] / 6
    for j, (r, sz, core) in enumerate(((7, 2.0, True), (9.5, 1.6, False), (11, 1.1, False))):
        cv = Canvas(n)
        r *= k_
        if core:
            body(cv, c, c + 1, 3.6 * k_, 3.1 * k_, GREEN)
        for k in range(8):
            a = k * math.pi / 4 + 0.3
            body(cv, c + math.cos(a) * r, c + 1 + math.sin(a) * r * 0.8, sz, sz, GREEN)
        if ver >= 2 and j < 2:
            for k in range(6):
                a = k * math.pi / 3 + 0.75 + j * 0.3
                shard(cv, c + math.cos(a) * (r + 2.5), c + 1 + math.sin(a) * (r + 2.5) * 0.8, a + j,
                      shard_mats[k % len(shard_mats)])
        cv.outline()
        if glow:
            for k in range(5 - j):
                a = k * 2 * math.pi / 5 + j * 0.6
                rr = r + 3 + j * 2
                put_glass(cv, c + math.cos(a) * rr, c + 1 + math.sin(a) * rr * 0.8, SHOCK[3 - j], 230 - j * 50)
        fr.append(cv)
        an.append(an[-1])
    cv = Canvas(n)
    body(cv, c, c + 8, 6 + 1.5 * k_, 1.6 + 0.2 * k_, PALE)
    cv.put(c - 2, c + 7, '#e8ffd8')
    if ver >= 2:
        for k, gx in enumerate((c - 6, c + 5)):
            m = shard_mats[k % len(shard_mats)]
            for dx in (-1, 0, 1):
                cv.put(gx + dx, c + 6, col(m, 2, 'pale')); cv.put(gx + dx, c + 7, col(m, 1, 'pale'))
    cv.outline()
    fr.append(cv)
    an.append(an[-1])
    return fr, an


# ================================================================== RASGOS
def spine(ox, oy, ang0, bend, length, step=0.25):
    """Columna curva (arco): lista de (x, y, dirección) cada step px."""
    pts = []
    x, y = ox, oy
    nst = int(length / step) + 1
    for k in range(nst + 1):
        s = k * step
        a = ang0 + bend * s / max(length, 0.01)
        pts.append((x, y, a, s))
        x += math.cos(a) * step
        y += math.sin(a) * step
    return pts


def abdomen_arc(cv, ox, oy, ang0, bend=0.0, waist=2.0, L=9.0, wmax=3.6, band=None, needle=3.0, drop=1.0,
                drop_dy=0.0, flash=False, vesicle=0.0, drip=None, tint=None, peak=0.4):
    """El abdomen de avispa de generar_evoluciones.abdomen(), sobre una columna
    curva para que pueda curvarse por debajo del cuerpo al picar.
    Devuelve la punta (de donde sale el proyectil)."""
    band = band or L / 3.5
    total = waist + L
    sp = spine(ox, oy, ang0, bend, total + needle)
    lime = {None: LIME, 'pale': LIME_PALE, 'white': ['#ffffff'] * 4, 'red': RED['ramp']}[tint]
    purd = {None: PURP_D, 'pale': PURP_PALE, 'white': ['#ffffff'] * 4, 'red': RED['ramp']}[tint]
    purp = PURP if tint is None else purd

    def half(s):
        if s < waist:
            return 1.25                           # pecíolo (cintura fina)
        t = (s - waist) / L
        if t > 1:
            return -1
        if t < peak:
            w = wmax * math.sqrt(max(0.0, 1 - ((peak - t) / peak) ** 2))
        else:
            w = wmax * (1 - ((t - peak) / (1 - peak)) ** 2.0)
        return max(w, 1.25 if t < 0.5 else 0.0)

    body_pts = [p for p in sp if p[3] <= total]
    xs = [p[0] for p in body_pts]
    ys = [p[1] for p in body_pts]
    for y in range(max(0, int(min(ys) - wmax - 2)), min(cv.n, int(max(ys) + wmax + 3))):
        for x in range(max(0, int(min(xs) - wmax - 2)), min(cv.n, int(max(xs) + wmax + 3))):
            px, py = x + .5, y + .5
            best = None
            for (sx, sy, a, s) in body_pts:
                d2 = (px - sx) ** 2 + (py - sy) ** 2
                if best is None or d2 < best[0]:
                    best = (d2, sx, sy, a, s)
            d2, sx, sy, a, s = best
            if abs((px - sx) * math.cos(a) + (py - sy) * math.sin(a)) > 0.6:
                continue                              # más allá de los extremos de la columna
            nrm = (-math.sin(a), math.cos(a))
            v = (px - sx) * nrm[0] + (py - sy) * nrm[1]
            w = half(s)
            if w <= 0 or abs(v) > w or s <= 0.01:
                continue
            along = 0.0 if s < waist else ((s - waist) / L - 0.38) * 0.9
            across = v / max(w, 0.8)
            d = (math.cos(a), math.sin(a))
            lv = shade_v(along * d[0] + across * nrm[0], along * d[1] + across * nrm[1])
            if s < waist:
                c = purp[ramp_idx(lv)]
            else:
                t = (s - waist) / L
                ss = (s - waist - 0.16 * L - 0.3 * across * across) / band
                rp = purd if (t < 0.1 or t > 0.9 or frac(ss) < 0.42) else lime
                c = rp[ramp_idx(lv)]
            cv.put(x, y, c)
    # brillo sobre el lomo
    for s in (waist + L * 0.22, waist + L * 0.3):
        p_ = min(body_pts, key=lambda p: abs(p[3] - s))
        nrm = (-math.sin(p_[2]), math.cos(p_[2]))
        w = half(s) * 0.55
        cands = [(p_[0] + nrm[0] * w, p_[1] + nrm[1] * w), (p_[0] - nrm[0] * w, p_[1] - nrm[1] * w)]
        top = min(cands, key=lambda q: q[1] + q[0] * 0.3)
        cv.put_in(top[0] - .5, top[1] - .5, lime[3])
    # vesícula de veneno (rasgo 3): brilla y late
    if vesicle > 0:
        p_ = min(body_pts, key=lambda p: abs(p[3] - (waist + L * 0.55)))
        nrm = (-math.sin(p_[2]), math.cos(p_[2]))
        cands = [(p_[0] + nrm[0] * half(p_[3]) * 0.35, p_[1] + nrm[1] * half(p_[3]) * 0.35),
                 (p_[0] - nrm[0] * half(p_[3]) * 0.35, p_[1] - nrm[1] * half(p_[3]) * 0.35)]
        vx, vy = min(cands, key=lambda q: q[1])
        glow = ['#4a7a1a', '#9ae030', '#d8ff6a', '#ffffe0'] if tint is None else lime
        r = 2.1 * vesicle
        for y in range(int(vy - r - 1), int(vy + r + 2)):
            for x in range(int(vx - r - 1), int(vx + r + 2)):
                nx, ny = (x + .5 - vx) / r, (y + .5 - vy) / r
                if nx * nx + ny * ny <= 1:
                    cv.put(x, y, glow[min(3, ramp_idx(shade_v(nx, ny)) + (1 if vesicle > 1.05 else 0))])
        cv.put(vx - r * 0.45 - .5, vy - r * 0.5 - .5, '#ffffff')
    # púa clara y brillante
    tip = (ox, oy)
    for (x, y, a, s) in sp:
        if s < total - 0.6:
            continue
        k = s - (total - 0.6)
        if k > needle:
            break
        cv.put(x - .5, y - .5, '#ffffff' if (k > needle - 1.2 and tint != 'pale') else purp[3] if k > 1 else purp[2])
        tip = (x, y)
    a_end = sp[-1][2]
    if flash:
        d = (math.cos(a_end), math.sin(a_end))
        nrm = (-d[1], d[0])
        for (u, v) in ((1, 0), (2, 0), (0, 1), (0, -1)):
            cv.put(tip[0] + d[0] * u + nrm[0] * v - .5, tip[1] + d[1] * u + nrm[1] * v - .5, '#ffffff')
    if drop > 0:
        gx, gy = int(round(tip[0] + math.cos(a_end) * 0.4 - 0.5)), int(round(tip[1] + 0.3 + drop_dy))
        shape = ['a.', 'bc'] if drop < 0.9 else (['.a.', 'abc', 'bcc', '.c.'] if drop < 1.08 else ['.a.', 'abc', 'bbc', 'bcc', '.c.'])
        cols = {'a': purp[3], 'b': purp[2], 'c': purp[1]}
        for j, row in enumerate(shape):
            for i, ch in enumerate(row):
                if ch in cols:
                    cv.put(gx - 1 + i, gy + j, cols[ch])
        if drop >= 0.9 and tint is None:
            cv.put(gx - 1, gy + 1, '#ffffff')
    if drip:
        # gota de la vesícula que cae: (distancia a lo largo, caída, tamaño)
        s_, fall, sz = drip
        p_ = min(body_pts, key=lambda p: abs(p[3] - (waist + L * s_)))
        nrm = (-math.sin(p_[2]), math.cos(p_[2]))
        bx, by = max([(p_[0] + nrm[0] * half(p_[3]), p_[1] + nrm[1] * half(p_[3])),
                      (p_[0] - nrm[0] * half(p_[3]), p_[1] - nrm[1] * half(p_[3]))], key=lambda q: q[1])
        gc = ['#4a7a1a', '#9ae030', '#d8ff6a'] if tint is None else lime[:3]
        if sz >= 1:
            cv.put(bx - .5, by + fall + 0.5, gc[2]); cv.put(bx - .5, by + fall + 1.5, gc[1])
            if sz >= 2:
                cv.put(bx + .5, by + fall + 1.5, gc[0])
        else:
            cv.put(bx - .5, by + fall + 0.5, gc[1])
    return tip


# --- Aguijón: datos por nivel
STINGER_LEVELS = {
    1: dict(waist=1.6, L=7.8, wmax=3.2, nb=2.2, needle=2.4, drop=0.85),
    2: dict(waist=1.6, L=9.0, wmax=4.0, nb=2.8, needle=2.8, drop=1.0),
    3: dict(waist=1.6, L=9.4, wmax=5.0, nb=2.8, needle=2.8, drop=1.1, vesicle=1.0, peak=0.45),
}
# dirección del abdomen en reposo (hacia atrás) y escorzo en cada dirección
STINGER_DIR = {'side': (math.pi, 1.0), 'down': (-math.pi / 2 + 0.4, 0.8), 'up': (math.pi / 2, 0.75)}
# Ataque: (ángulo de salida, curvatura total, largo, grosor, púa, destello, gota)
STINGER_ATTACK = {
    # de lado: se curva por debajo del cuerpo y pica hacia delante
    'side': [(math.pi - 0.15, -0.5, 0.8, 1.14, 0.8, False, 0.6),
             (math.pi / 2 + 0.7, -1.7, 0.95, 1.0, 1.0, False, 0.0),
             (math.pi / 2 + 0.35, -2.05, 1.12, 0.9, 1.35, True, 0.0),
             (math.pi - 0.5, -0.7, 1.0, 1.0, 1.0, False, 0.5)],
    # de frente: rodea el costado derecho y pica hacia abajo
    'down': [(-math.pi / 2 + 0.12, 0.3, 0.75, 1.14, 0.8, False, 0.6),
             (-0.55, 1.3, 0.95, 1.0, 1.0, False, 0.0),
             (-0.2, 1.75, 1.1, 0.9, 1.35, True, 0.0),
             (-math.pi / 2 + 0.55, 0.6, 0.95, 1.0, 1.0, False, 0.5)],
    # de espaldas: rodea el costado derecho y pica hacia arriba
    'up': [(math.pi / 2 - 0.12, -0.3, 0.75, 1.14, 0.8, False, 0.6),
           (0.55, -1.3, 0.95, 1.0, 1.0, False, 0.0),
           (0.2, -1.75, 1.1, 0.9, 1.35, True, 0.0),
           (math.pi / 2 - 0.55, -0.6, 0.95, 1.0, 1.0, False, 0.5)],
}
# Capa de cada cuadro del Attack cuando cambia la de su anclaje (None = la de "order").
# De frente, el abdomen pasa por delante del costado derecho al picar.
STINGER_ATTACK_LAYER = {'side': [None] * 4, 'down': [None, 'front', 'front', None], 'up': [None] * 4}
TRAIT_N = 32           # lienzo de Idle / Activate
ATTACK_N = 48          # lienzo del Attack del Aguijón (lo avisa el README)
DEATH_N = 48


def stinger_frame(lvl, facing, n=TRAIT_N, breathe=0.0, curl=0.0, drop_dy=0.0, attack=None, tint=None,
                  drip=None, offset=(0, 0), ang_add=0.0, shrink=1.0, glow=1.0):
    d = STINGER_LEVELS[lvl]
    cv = Canvas(n)
    c = n / 2
    ang, fs = STINGER_DIR[facing]
    if attack:
        a0, bend, Ls, ws, nd, flash, drop = attack
        L, wmax, needle = d['L'] * Ls * (fs if facing != 'side' else 1), d['wmax'] * ws, d['needle'] * nd
    else:
        # de frente la punta apunta lejos: la gota no se ve; de espaldas cuelga más pequeña
        a0, bend, flash = ang + ang_add, curl * (1 if facing != 'up' else -1), False
        drop = {'side': d['drop'], 'down': 0.0, 'up': min(d['drop'], 0.85)}[facing]
        L, wmax, needle = d['L'] * fs * shrink, d['wmax'] * (1 + breathe) * shrink, d['needle'] * (fs if fs < 1 else 1)
        if facing == 'side':
            bend = -curl
    tip = abdomen_arc(cv, c + offset[0], c + offset[1], a0, bend, d['waist'], L, wmax, band=L / d['nb'],
                      needle=needle, drop=drop * (1 if shrink >= 1 else 0.6), drop_dy=drop_dy, flash=flash,
                      vesicle=d.get('vesicle', 0) * glow, drip=drip, tint=tint, peak=d.get('peak', 0.4))
    cv.outline()
    return cv, (int(math.floor(tip[0])), int(math.floor(tip[1])))


def stinger_anims(lvl):
    a, tips = {}, {}
    for f in FACINGS:
        drips = [(0.62, 0, 0.5), (0.62, 0.4, 1), (0.62, 1.2, 2), (0.62, 3.0, 1)] if lvl == 3 else [None] * 4
        a[f'Idle_{f}'] = [stinger_frame(lvl, f, breathe=b, curl=cu, drop_dy=dd, drip=dr, glow=g)[0]
                          for b, cu, dd, dr, g in zip((0, 0.04, 0.08, 0.04), (0.0, 0.0, 0.0, 0.0),
                                                      (0, 0.5, 0, -0.5), drips, (1.0, 1.06, 1.12, 1.06))]
        frs = [stinger_frame(lvl, f, n=ATTACK_N, attack=at) for at in STINGER_ATTACK[f]]
        a[f'Attack_{f}'] = [x[0] for x in frs]
        tips[f'Attack_{f}'] = [x[1] for x in frs]
    a['Death'] = stinger_death(lvl)
    return a, tips


def stinger_death(lvl, n=DEATH_N):
    """Sincronizado con Body_..._Death: blanco, se marchita, se desprende y queda seco."""
    seq = [dict(facing='side', tint='white'),
           dict(facing='down', tint='pale', shrink=0.9, curl=0.3),
           dict(facing='down', tint='pale', shrink=0.8, curl=0.6, ang_add=0.4),
           dict(facing='side', tint='pale', shrink=0.8, offset=(-5, -7), ang_add=-1.2, curl=0.4),
           dict(facing='side', tint='pale', shrink=0.75, offset=(-9, 0), ang_add=-2.6, curl=0.5),
           dict(facing='side', tint='pale', shrink=0.7, offset=(-8, 12), ang_add=0.2, curl=0.2),
           dict(facing='side', tint='pale', shrink=0.6, offset=(-6, 14), ang_add=0.22, curl=0.0)]
    return [stinger_frame(lvl, n=n, **k)[0] for k in seq]


# --- Glándula de choque: datos por nivel
GLAND_LEVELS = {
    1: dict(r=1.8, chispas=1, filamentos=0, rayos=0),
    2: dict(r=2.3, chispas=2, filamentos=5, rayos=0),
    3: dict(r=2.6, chispas=3, filamentos=5, rayos=2),
}
# hacia dónde se reparten los filamentos por el cuerpo en cada dirección
GLAND_DIR = {'side': (1.0, 0.55), 'down': (0.8, 0.7), 'up': (0.85, 0.8)}
CYAN = THEMES['electrico']['ramps']['cian']


def zigzag(cv, x, y, a, L, seed, cols, alpha=255):
    """Rayito en zigzag desde (x, y) en la dirección a."""
    px_, py_ = x, y
    for k in range(int(L)):
        j = ((seed * 7 + k * 3) % 3 - 1) * 0.9
        nx_, ny_ = px_ + math.cos(a) + math.cos(a + math.pi / 2) * j * 0.6, py_ + math.sin(a) + math.sin(a + math.pi / 2) * j * 0.6
        put_glass(cv, nx_, ny_, cols[min(len(cols) - 1, k * len(cols) // max(1, int(L)))], alpha)
        px_, py_ = nx_, ny_


def gland_frame(lvl, facing, f, n=TRAIT_N, power=0.0, tint=None, bolts=None, flash=False, dim=0, spread=1.0):
    d = GLAND_LEVELS[lvl]
    cv = Canvas(n)
    c = n / 2
    r = d['r'] + 0.35 * (f % 2) + power
    sx, sy = GLAND_DIR[facing]
    shock = SHOCK if tint is None else (['#ffffff'] * 4 if tint == 'white' else PALE_RAMPS['luz'])
    cyan = CYAN if tint is None else shock
    if dim:
        shock = [shock[0], shock[0], shock[1], shock[2]] if dim == 1 else PALE_RAMPS['luz']
        cyan = shock
    # filamentos conductores (rasgo 2 y 3): salen de la glándula y recorren el cuerpo
    fil = Canvas(n)
    for k in range(d['filamentos']):
        a = k * 2 * math.pi / d['filamentos'] + 0.5
        L = (5.5 + 1.5 * (k % 2)) * spread
        pts = [(c + math.cos(a) * r * sx, c + math.sin(a) * r * sy)]
        for s in range(1, 4):
            aa = a + 0.35 * math.sin(k * 2.1 + s)
            pts.append((pts[-1][0] + math.cos(aa) * L / 3 * sx * 1.2, pts[-1][1] + math.sin(aa) * L / 3 * sy * 1.2))
        lit = (f + k) % 4
        line_px(fil, pts, lambda seg, t, lit=lit: cyan[3] if (int(t * 3 + seg) == lit and not dim) else cyan[1])
    for p, c_ in fil.px.items():
        put_glass(cv, p[0], p[1], c_, 190)
    # halo y glándula
    halo = Canvas(n)
    blob(halo, c, c, r + 1.1, r + 0.7, [shock[0], shock[1], shock[1], shock[2]], alpha=100)
    for p, c_ in halo.px.items():
        put_glass(cv, p[0], p[1], c_, c_[3])
    g = Canvas(n)
    if flash:
        blob(g, c, c, r + 1, r + 1, [shock[2], '#ffffff', '#ffffff', '#ffffff'])
    else:
        blob(g, c, c, r + 0.4, r, shock, alpha=235)
        g.put_in(c - 0.9, c - 0.9, '#ffffff' if not dim else shock[3])
        if lvl >= 2:
            g.put_in(c + 0.6, c + 0.4, shock[1])
    M.translucent_outline(g)
    for p, c_ in g.px.items():
        if c_[:3] == OUTL[:3]:
            put_glass(cv, p[0], p[1], c_, 170)
        else:
            cv.px[p] = c_
    # chispas que giran alrededor
    if not dim or dim == 1:
        for k in range(d['chispas']):
            a = f * math.pi / 4 + k * 2 * math.pi / d['chispas']
            px_, py_ = c + math.cos(a) * (r + 1.8), c + math.sin(a) * (r + 1.2)
            put_glass(cv, px_, py_, shock[3], 255)
            put_glass(cv, px_ + math.cos(a + 1.2), py_ + math.sin(a + 1.2) * 0.8, shock[2], 220)
    # rayitos (rasgo 3, y en la descarga)
    nb = d['rayos'] if bolts is None else bolts
    for k in range(nb):
        a = (f * 1.7 + k * 2.4) % (2 * math.pi)
        L = (4 + (f + k) % 3) * (spread if bolts is not None else 1)
        zigzag(cv, c + math.cos(a) * (r + 0.5), c + math.sin(a) * (r + 0.5), a, L, f + k * 5,
               ['#ffffff', cyan[3], cyan[2]], 235)
    return cv


def gland_anims(lvl):
    a = {}
    for f_ in FACINGS:
        a[f'Idle_{f_}'] = [gland_frame(lvl, f_, i, power=(0, 0.25, 0.45, 0.25)[i]) for i in range(4)]
        a[f'Activate_{f_}'] = [gland_frame(lvl, f_, 0, power=1.1, bolts=0),          # se carga
                               gland_frame(lvl, f_, 1, power=1.3, flash=True, bolts=4, spread=0.8),  # destello: sale la onda
                               gland_frame(lvl, f_, 2, power=0.3, bolts=6, spread=1.8),             # descarga
                               gland_frame(lvl, f_, 3, power=-0.4, dim=1)]                          # se recupera
    a['Death'] = [gland_frame(lvl, 'down', 0, n=DEATH_N, tint='white', power=0.4),
                  gland_frame(lvl, 'down', 1, n=DEATH_N, dim=1, bolts=1),
                  gland_frame(lvl, 'down', 2, n=DEATH_N, dim=1, power=0.3),
                  gland_frame(lvl, 'down', 3, n=DEATH_N, bolts=5, spread=2.0, power=-0.3),
                  gland_frame(lvl, 'down', 0, n=DEATH_N, dim=2, power=-0.6),
                  gland_frame(lvl, 'down', 1, n=DEATH_N, dim=2, power=-1.0),
                  Canvas(DEATH_N)]
    return a


TRAITS = {
    # rasgo -> anclaje, función que hace sus animaciones y su nombre
    'Stinger': dict(nombre='Aguijón (abdomen)', anclaje='abdomen', anims=stinger_anims, accion='Attack'),
    'ShockGland': dict(nombre='Glándula de choque', anclaje='organ', anims=lambda lvl: (gland_anims(lvl), {}),
                       accion='Activate'),
}


# ================================================================== GUARDAR
def to_img(x):
    return x.img() if hasattr(x, 'img') else x


def sheet(frames):
    ims = [to_img(f) for f in frames]
    w, h = ims[0].size
    out = Image.new('RGBA', (w * len(ims), h), (0, 0, 0, 0))
    for i, im in enumerate(ims):
        out.paste(im, (i * w, 0))
    return out


ROWS = []        # (grupo, nombre, cuadros, lienzo, centro, ms)


def save_sheet(group, name, frames, center, ms):
    os.makedirs(os.path.join(OUT, group), exist_ok=True)
    sheet(frames).save(os.path.join(OUT, group, name + '.png'))
    n = to_img(frames[0]).width
    ROWS.append((group, name, len(frames), n, center, ms))


def build():
    bodies, traits, anchors = {}, {}, {}
    for form, fd in FORMS.items():
        # Formato reducido: con 'compartida': N, la esquiva y la muerte se guardan una sola vez (las de la versión N)
        # en Body/<Forma>/Shared/ sin número de versión, y no se guarda Hurt (el juego tiñe el cuerpo de rojo).
        shared_ver = fd.get('compartida')
        for ver in fd['versiones']:
            anims, ans = body_anims(form, ver)
            bodies[(form, ver)] = (anims, ans)
            for name, frs in anims.items():
                anim = name.split('_')[0]
                group, sname = os.path.join('Body', form), f'Body_{form}_{ver}_{name}'
                if shared_ver:
                    if anim == 'Hurt':
                        continue
                    if anim in ('Dodge', 'Death'):
                        if ver != shared_ver:
                            continue
                        group, sname = os.path.join('Body', form, 'Shared'), f'Body_{form}_{name}'
                n = to_img(frs[0]).width
                save_sheet(group, sname, frs, (n // 2, n // 2), MS[anim])
                anchors[sname] = ans[name]
    for tr, td in TRAITS.items():
        for lvl in (1, 2, 3):
            anims, tips = td['anims'](lvl)
            traits[(tr, lvl)] = (anims, tips)
            for name, frs in anims.items():
                anim = name.split('_')[0]
                sname = f'Trait_{tr}_{lvl}_{name}'
                n = to_img(frs[0]).width
                save_sheet(os.path.join('Trait', tr), sname, frs, (n // 2, n // 2), MS[anim])
                if name in tips:
                    lay = STINGER_ATTACK_LAYER[name.split('_')[1]]
                    anchors[sname] = [dict(tip=list(t), **({'layer': l} if l else {})) for t, l in zip(tips[name], lay)]
    anchors['order'] = ORDER
    with open(os.path.join(OUT, 'anclajes.json'), 'w', encoding='utf-8') as fh:
        fh.write(dumps_compact(anchors))
    return bodies, traits


def dumps_compact(d):
    """JSON legible: un cuadro por línea."""
    lines = ['{']
    keys = list(d)
    for i, k in enumerate(keys):
        v = d[k]
        end = ',' if i < len(keys) - 1 else ''
        if isinstance(v, list):
            lines.append(f'  {json.dumps(k)}: [')
            for j, fr in enumerate(v):
                lines.append('    ' + json.dumps(fr, separators=(', ', ': ')) + (',' if j < len(v) - 1 else ''))
            lines.append('  ]' + end)
        else:
            lines.append(f'  {json.dumps(k)}: ' + json.dumps(v, ensure_ascii=False) + end)
    lines.append('}')
    return '\n'.join(lines) + '\n'


# ============================================================ VISTA PREVIA
COMBOS = [
    ('AGUIJON I + PATAS I', {'Stinger': 1, 'JointedLegs': 1}),
    ('AGUIJON I + PATAS III', {'Stinger': 1, 'JointedLegs': 3}),
    ('AGUIJON III + PATAS I', {'Stinger': 3, 'JointedLegs': 1}),
    ('AGUIJON III + PATAS III', {'Stinger': 3, 'JointedLegs': 3}),
]
WIN = 56          # ventana de cada cuadro en la vista previa (centro 28, 28)


def compose(body_im, anc, layers, facing):
    """Monta un cuadro como el juego: rasgos detrás, cuerpo, rasgos delante.
    layers = [(imagen_del_rasgo, nombre_del_anclaje)]; el centro del lienzo del
    rasgo va sobre el anclaje. Las capas sin imagen (mutaciones sin rasgo) se ignoran."""
    layers = [L for L in layers if L[0] is not None]
    out = Image.new('RGBA', (WIN, WIN), (0, 0, 0, 0))
    bo = (WIN - body_im.width) // 2

    def put(im, a):
        x, y = anc[a]
        out.alpha_composite(im, (bo + x - im.width // 2, bo + y - im.height // 2))
    order = ORDER[facing]
    side = lambda L: (L[2] if len(L) > 2 and L[2] else order[L[1]])
    for L in layers:
        if side(L) == 'behind':
            put(L[0], L[1])
    out.alpha_composite(body_im, (bo, bo))
    for L in layers:
        if side(L) == 'front':
            put(L[0], L[1])
    return out


def combo_frames(levels, bodies, traits):
    """Cuadros montados de una combinación: dict anim -> lista de imágenes."""
    form, ver, trs = resolve(levels)
    res = {}
    if form is None:
        for f in FACINGS:
            for anim in ('Idle', 'Move'):
                res[f'{anim}_{f}'] = [compose(cv.img(), None, [], f) for cv in PANIMS[f'{anim}_{f}']]
        res['Death'] = [compose(cv.img(), None, [], 'side') for cv in PANIMS['Death']]
        return res
    anims, ans = bodies[(form, ver)]
    for f in FACINGS:
        for anim in ('Idle', 'Move'):
            name = f'{anim}_{f}'
            frs = []
            for i, cv in enumerate(anims[name]):
                layers = [(traits[(t, l)][0][f'Idle_{f}'][i].img(), TRAITS[t]['anclaje']) for t, l in trs]
                frs.append(compose(cv.img(), ans[name][i], layers, f))
            res[name] = frs
        # ataque del Aguijón con el cuerpo en reposo (la Glándula sigue latiendo)
        if any(t == 'Stinger' for t, _ in trs):
            frs = []
            for i in range(4):
                layers = []
                for t, l in trs:
                    key = f'Attack_{f}' if t == 'Stinger' else f'Idle_{f}'
                    lay = STINGER_ATTACK_LAYER[f][i] if t == 'Stinger' else None
                    layers.append((traits[(t, l)][0][key][i].img(), TRAITS[t]['anclaje'], lay))
                frs.append(compose(anims[f'Idle_{f}'][i].img(), ans[f'Idle_{f}'][i], layers, f))
            res[f'Attack_{f}'] = frs
    frs = []
    for i, cv in enumerate(anims['Death']):
        layers = [(traits[(t, l)][0]['Death'][i].img(), TRAITS[t]['anclaje']) for t, l in trs]
        frs.append(compose(cv.img(), ans['Death'][i], layers, 'down' if i else 'side'))
    res['Death'] = frs
    return res


def _patch_compose():
    """La célula sola no tiene anclajes: compose sin capas."""
    global compose
    _c = compose

    def compose2(body_im, anc, layers, facing):
        if anc is None:
            out = Image.new('RGBA', (WIN, WIN), (0, 0, 0, 0))
            out.alpha_composite(body_im, ((WIN - body_im.width) // 2, (WIN - body_im.height) // 2))
            return out
        return _c(body_im, anc, layers, facing)
    compose = compose2


_patch_compose()

LINE1 = [f'{a}_{f}' for a in ('Idle', 'Move') for f in FACINGS]
LINE2 = [f'Attack_{f}' for f in FACINGS] + ['Death']


def preview(bodies, traits, path):
    S = 4
    cell = WIN * S
    gap = 4
    labw = 300
    groups_w = 24 * (cell + gap) + 5 * 24
    width = labw + groups_w + 20
    head_h = 70
    rowh = 2 * (cell + 34) + 24
    out = Image.new('RGBA', (width, head_h + len(COMBOS) * rowh), BG)
    G.ptext(out, (16, 14), 'CUERPO EVOLUTIVO (PILOTO): FORMA + RASGOS MONTADOS CON LOS ANCLAJES, X4', (214, 255, 154), scale=3)
    for r, (label, levels) in enumerate(COMBOS):
        y0 = head_h + r * rowh
        form, ver, trs = resolve(levels)
        G.ptext(out, (16, y0 + 40), label, (214, 255, 154), scale=3)
        sub = 'CELULA' if form is None else f'{form.upper()} V{ver}'
        G.ptext(out, (16, y0 + 70), sub, (160, 200, 180), scale=2)
        for k, m in enumerate(sorted(levels, key=lambda m: SLOTS.index(MUTATIONS[m]['ranura']))):
            tr = MUTATIONS[m].get('rasgo')
            txt = f'{tr.upper()} {levels[m]}' if tr else f'{m.upper()} {levels[m]} SIN RASGO'
            G.ptext(out, (16, y0 + 92 + k * 18), txt, (160, 200, 180), scale=2)
        frs = combo_frames(levels, bodies, traits)
        for li, line in enumerate((LINE1, LINE2)):
            x = labw
            y = y0 + li * (cell + 34)
            for name in line:
                if name not in frs:
                    continue
                G.ptext(out, (x + 4, y + 6), name.replace('_', ' '), (120, 170, 150), scale=2)
                for im in frs[name]:
                    out.alpha_composite(im.resize((cell, cell), Image.NEAREST), (x, y + 24))
                    x += cell + gap
                x += 24
    out.convert('RGB').save(path)


def preview_gifs(bodies, traits):
    """Un GIF por combinación: Idle, Move, Attack (las 3 direcciones) y Death."""
    os.makedirs(os.path.join(OUT, 'gif'), exist_ok=True)
    sc = 4
    for idx, (label, levels) in enumerate(COMBOS):
        frs = combo_frames(levels, bodies, traits)
        seq = []
        for anim, reps in (('Idle', 2), ('Move', 3), ('Attack', 2)):
            if f'{anim}_side' not in frs:
                continue
            for _ in range(reps):
                for i in range(4):
                    b = Image.new('RGBA', (WIN * 3, WIN), BG)
                    for j, f in enumerate(FACINGS):
                        b.alpha_composite(frs[f'{anim}_{f}'][i], (j * WIN, 0))
                    ms = MS[anim]
                    if anim == 'Attack' and i == 3:
                        ms += 250
                    seq.append((b, ms))
        for i, im in enumerate(frs['Death']):
            b = Image.new('RGBA', (WIN * 3, WIN), BG)
            b.alpha_composite(im, (WIN, 0))
            seq.append((b, MS['Death'] + (600 if i == len(frs['Death']) - 1 else 0)))
        ims = [s[0].resize((WIN * 3 * sc, WIN * sc), Image.NEAREST).convert('RGB') for s in seq]
        name = label.lower().replace(' + ', '_').replace(' ', '_')
        ims[0].save(os.path.join(OUT, 'gif', f'{idx}_{name}.gif'), save_all=True, append_images=ims[1:],
                    duration=[s[1] for s in seq], loop=0)


def main():
    bodies, traits = build()
    preview(bodies, traits, os.path.join(OUT, 'vista_previa_cuerpo_evolutivo.png'))
    preview_gifs(bodies, traits)
    for r in ROWS:
        print(*r)


if __name__ == '__main__':
    main()
