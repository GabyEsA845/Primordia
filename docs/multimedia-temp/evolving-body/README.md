# Cuerpo evolutivo (piloto)

Piloto del nuevo sistema de evolución del jugador: el cuerpo entero cambia de forma según sus mutaciones. Incluye las formas **WaspLarva** (Aguijón solo) y **Firefly** (Aguijón + Glándula de choque), cada una en 3 versiones, y los rasgos **Stinger** y **ShockGland**, cada uno en 3 niveles.

Todo sale de `generar_cuerpo_evolutivo.py`, que reutiliza los generadores anteriores (`Canvas`, contorno `#181425`, rampas, ojos y brillo de la célula, balanceo de cada cuadro y el abdomen de avispa de `generar_evoluciones.py`).

```
python generar_cuerpo_evolutivo.py [carpeta_multimedia_temp] [carpeta_salida]
```
Por defecto usa la carpeta de arriba y la de este script. Necesita las carpetas hermanas `player/`, `mutations/`, `ui/`, `menus/`, `effects/` y `evolutions/` con sus generadores.

## Contenido

| Ruta | Qué es |
|------|--------|
| `Body/WaspLarva/`, `Body/Firefly/` | 33 hojas por forma: 3 versiones × (Idle, Move y Dodge en 3 direcciones, Hurt y Death) |
| `Trait/Stinger/`, `Trait/ShockGland/` | 21 hojas por rasgo: 3 niveles × (Idle y Attack/Activate en 3 direcciones, y Death) |
| `anclajes.json` | Anclajes de cada cuadro de cuerpo, punta del Aguijón y orden delante/detrás |
| `vista_previa_cuerpo_evolutivo.png` | Las 8 combinaciones montadas con los anclajes, a ×4 sobre `#16352e` |
| `gif/` | Un GIF por combinación: Idle, Move y Attack (side, down, up) y Death |

## Cómo se monta (igual que en la vista previa)

1. **Forma:** la temática de la **Boca** y la de la mutación de **mayor nivel** entre las demás (si empatan, la ranura que va antes: Extremidades, Piel, Órgano). Si las dos coinciden, cuenta solo una. La **versión** es el nivel más alto de esas dos mutaciones. Sin mutaciones se usa la célula de `player/`.
2. **Rasgos:** cada mutación pone su rasgo con su nivel. El centro del lienzo del rasgo se coloca sobre el anclaje de su cuerpo en ese cuadro (`Stinger` en `abdomen` y `ShockGland` en `organ`).
3. **Orden:** cada anclaje se dibuja delante o detrás del cuerpo según `order[dirección]` de `anclajes.json`.
4. Para mirar a la izquierda se voltea todo (cuerpo y rasgos) con los anclajes reflejados: `x' = ancho - 1 - x`.
5. En `Dodge` los rasgos se ocultan. En `Hurt` el juego tiñe los rasgos de blanco y rojo igual que el cuerpo.

El script hace lo mismo en `resolve()` y `compose()`, por si sirve de referencia para el código del juego.

## Hojas

Tiras horizontales, cuadro 0 a la izquierda, 16 PPU, filtro Point (sin compresión), pivote en el centro del lienzo.

| Hoja | Cuadros | Lienzo | Centro | Duración por cuadro | Bucle |
|------|---------|--------|--------|---------------------|-------|
| `Body_<Forma>_<V>_Idle_<dir>` | 4 | 32×32 | (16, 16) | 167 ms (6 fps) | sí |
| `Body_<Forma>_<V>_Move_<dir>` | 4 | 32×32 | (16, 16) | 100 ms (10 fps) | sí |
| `Body_<Forma>_<V>_Dodge_<dir>` | 5 | 32×32 | (16, 16) | 50 ms (20 fps) | no |
| `Body_<Forma>_<V>_Hurt` | 2 | 32×32 | (16, 16) | 100 ms (10 fps) | no |
| `Body_<Forma>_<V>_Death` | 7 | 48×48 | (24, 24) | 100 ms (10 fps) | no |
| `Trait_Stinger_<N>_Idle_<dir>` | 4 | 32×32 | (16, 16) | 167 ms (6 fps) | sí |
| `Trait_Stinger_<N>_Attack_<dir>` | 4 | **48×48** | **(24, 24)** | 88 ms (≈0,35 s en total) | no |
| `Trait_Stinger_<N>_Death` | 7 | 48×48 | (24, 24) | 100 ms (10 fps) | no |
| `Trait_ShockGland_<N>_Idle_<dir>` | 4 | 32×32 | (16, 16) | 167 ms (6 fps) | sí |
| `Trait_ShockGland_<N>_Activate_<dir>` | 4 | 32×32 | (16, 16) | 80 ms | no |
| `Trait_ShockGland_<N>_Death` | 7 | 48×48 | (24, 24) | 100 ms (10 fps) | no |

`<Forma>`: `WaspLarva` o `Firefly`. `<V>` y `<N>`: 1, 2 o 3. `<dir>`: `side` (mira a la derecha), `down` (de frente) o `up` (de espaldas).

**Aviso: el `Attack` del Aguijón usa un lienzo de 48×48**, centro (24, 24), porque el abdomen se curva por debajo del cuerpo y pica por delante. El centro sigue yendo sobre el anclaje `abdomen`.

Momentos clave:
- `Attack`: cuadro 0 se encoge, 1 se curva hacia delante, **2 estocada con destello (sale el proyectil)**, 3 vuelve.
- `Activate`: cuadro 0 se carga, **1 destello (sale la onda)**, 2 descarga con rayos, 3 se recupera.
- `Death` de los rasgos va sincronizado cuadro a cuadro con `Body_…_Death`. Para mostrar otra animación del cuerpo durante el ataque (por ejemplo `Move`), basta con seguir usando los anclajes del cuadro de cuerpo que se esté dibujando.

## anclajes.json

- `"Body_<Forma>_<V>_<Anim>[_<dir>]"`: lista con un objeto por cuadro con `head`, `back`, `abdomen`, `organ`, `left`, `right` y `mouth` en píxeles desde la esquina de arriba a la izquierda del cuadro (en `Death`, del cuadro de 48×48).
  - `left` y `right` son los costados del propio bicho. De lado (mirando a la derecha) `right` es el costado cercano.
  - En `Death`, los cuadros 3 a 6 (estallido y charco) repiten los anclajes del cuadro 2; los rasgos ya llevan su propio movimiento.
  - En `Dodge` también hay anclajes, aunque los rasgos se ocultan.
- `"Trait_Stinger_<N>_Attack_<dir>"`: un objeto por cuadro con `tip`, la punta en píxeles del lienzo de 48×48 del rasgo. Posición en el mundo: `anclaje_abdomen + (tip - (24, 24))`. Algunos cuadros traen además `"layer": "front"`: ese cuadro se dibuja delante aunque `order` diga detrás (de frente, el abdomen pasa por delante del costado derecho al picar).
- `"order"`: para cada dirección, `front` o `behind` para cada anclaje.

## Paletas (4 tonos, de oscuro a claro)

| Temática | Rampa | Colores |
|----------|-------|---------|
| Célula | membrana (verde del jugador) | `#2f7d4f` `#4fb85f` `#8be36a` `#d6ff9a` |
| Insecto | quitina | `#20142a` `#3a2438` `#5c3c4c` `#86626c` |
| Insecto | ámbar | `#7a3a12` `#c0701e` `#eeaa36` `#ffe08a` |
| Insecto | ala (traslúcida, alfa 160-200) | `#6a90a8` `#a8d0e4` `#dcf4ff` `#ffffff` |
| Eléctrico | élitro | `#121634` `#222c5c` `#384c8e` `#6888c8` |
| Eléctrico | luz (la de la Glándula) | `#2a5a9a` `#4a9ae0` `#9ad8ff` `#ffffff` |
| Eléctrico | cian | `#1e6a8a` `#2ab0d0` `#6aecff` `#e0ffff` |
| Aguijón (Tóxico) | lima / morado oscuro | `#4a7a1a` `#86bc2c` `#c0f040` `#eaff9a` / `#1c0630` `#3e1660` `#6e2a98` `#a85ad0` |
| Ojos | claro / faceta | `#f4f4e8` / `#c2c6d6` |

Contorno `#181425`. En la muerte cada rampa tiene su versión apagada (`PALE_RAMPS` en el script).

## Cómo es cada forma

| | Versión 1 (sutil) | Versión 2 (marcada) | Versión 3 (plena) |
|--|--|--|--|
| **WaspLarva** | La célula con antenitas, ojos que empiezan a facetarse y segmentos leves | Cabeza ámbar y tórax verde con bandas de quitina; ojos compuestos | Cabeza grande, antenas acodadas, placas ámbar y quitina con el verde en las juntas, alitas que vibran en Move |
| **Firefly** | La de la avispa sutil con manchas que brillan y antenas con punta de luz | Cuerpo alargado, élitros incipientes y segmentos traseros que brillan | Élitros, escudo ámbar con mancha, cabeza oscura, antenas finas y segmentos luminosos |

La luz de la Luciérnaga late en `Idle` (pulso 0-1-2-1). Las versiones 2 y 3 se aplastan algo menos al moverse que la célula (`elastico`), para que la quitina no parezca de goma.

## Cómo añadir una forma o un rasgo

Todo está como datos en el script.

**Forma nueva** (por ejemplo, insecto + marino):
1. Si la temática es nueva, añádela a `THEMES` con sus rampas, y a `PALE_RAMPS` con su versión apagada.
2. Si hace falta un acabado nuevo (escamas, aletas...), escribe una función de material `m_…(q)` y regístrala en `MATERIALS`. Recibe la posición del píxel en la parte y la luz, y devuelve un color con `col(rampa, tono, ...)`.
3. Añade la forma a `FORMS` con sus 3 versiones. Cada versión es una lista de `parte(...)`, elipses con su posición y radios en `side`, `down` y `up` y su orden de dibujo. Además lleva `ojos`, `antenas`, `alas` (opcional), `bola` (radio en la esquiva) y `elastico`.
4. Añade la pareja de temáticas a `FORM_BY_THEMES`, y la mutación a `MUTATIONS` con su ranura y su temática.

**Rasgo nuevo** (por ejemplo, el Caparazón en `back`):
1. Escribe una función que devuelva sus animaciones por nivel (`Idle_<dir>`, su acción y `Death`), como `stinger_anims` o `gland_anims`, con sus datos por nivel en un diccionario (`STINGER_LEVELS`, `GLAND_LEVELS`).
2. Regístrala en `TRAITS` con su anclaje y su acción.

Al ejecutar el script se generan las hojas, `anclajes.json`, la vista previa y los GIF. Para añadir combinaciones a la vista previa, edita `COMBOS`.
