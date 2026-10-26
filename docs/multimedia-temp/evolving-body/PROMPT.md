Necesito un **encargo piloto** de pixel art para mi videojuego *Primordia*: un roguelike de disparos con vista cenital en 3/4. El piso 1 está bajo el agua, en un caldo primordial. El jugador es una célula verde que **evoluciona** al conseguir mutaciones, y quiero cambiar cómo se ve esa evolución: en vez de añadir piezas sueltas a la célula, **el cuerpo entero irá cambiando de forma**.

Este piloto sirve para fijar el estilo y el formato con una sola mutación, el **Aguijón**, y una combinación. Después te pediré el resto de formas con el mismo formato, así que cuida mucho que sea coherente y fácil de repetir.

Te adjunto:
- `generar_jugador.py`, con su vista previa `vista_previa.png`: la célula actual.
- `generar_mutaciones.py`, con `vista_previa_mutaciones.png`: las piezas de mutación actuales.
- `generar_evoluciones.py`, con `vista_previa_evoluciones.png`: el Aguijón rehecho como cola de avispa, el proyectil del aguijón y los efectos de nivel.

Hazlo con un script de Python + Pillow (`generar_cuerpo_evolutivo.py`) que **reutilice las funciones y paletas de esos scripts**: `Canvas`, el contorno oscuro, las rampas de color, los ojos y el brillo de la célula y el balanceo de cada cuadro. Así todo encaja con lo que ya existe.

## Cómo funciona la evolución en el juego

El jugador tiene **4 ranuras** (Boca = arma, Extremidades, Piel y Órgano), con una mutación en cada una. Cada mutación tiene **3 niveles** (I, II y III; el III es su "evolución"). El aspecto del jugador se monta con **dos capas**:

1. **Forma del cuerpo.** Cada mutación tiene una **temática**: el Aguijón es *insecto*, las Aletas son *marino*, la Glándula de choque es *eléctrico*… El cuerpo toma la forma de la **mezcla de las 2 temáticas dominantes**:
   - La primera es siempre la de la **Boca** (el arma).
   - La segunda es la de la mutación de **mayor nivel** entre las demás.

   Cada forma tiene **3 versiones** (1 sutil, 2 marcada y 3 plena). La versión la decide el nivel más alto de las dos mutaciones que deciden la forma. Así el jugador cambia cada vez que consigue o mejora una mutación, y a la larga deja de parecer una célula.
2. **Rasgos.** Cada mutación pone encima del cuerpo **su propio rasgo**, que crece con su nivel (rasgo 1, 2 y 3): el abdomen con aguijón, un caparazón en el lomo, púas… Los rasgos son capas aparte, y el juego los coloca sobre cualquier forma usando unos **puntos de anclaje** que cada cuerpo trae por cuadro.

Sin mutaciones, el jugador sigue siendo la célula verde actual, que es el punto de partida de la evolución.

## Qué incluye este piloto

### A. Forma "Avispa larvaria" (`WaspLarva`): el Aguijón solo (insecto)

Las 3 versiones del cuerpo:
- **Versión 1, sutil:** sigue siendo claramente la célula, pero ya se ve que tira a insecto:
  - Dos antenitas.
  - Los ojos empiezan a ser compuestos (facetados).
  - Una leve segmentación en la membrana.
- **Versión 2, marcada:** el cuerpo se divide en cabeza y tórax redondeados:
  - Antenas más largas.
  - Ojos compuestos claros.
  - Bandas de quitina que entran sobre el verde.
- **Versión 3, plena:** una criatura de avispa, larval y regordeta, que ya no se lee como célula:
  - Cabeza con ojos compuestos grandes y antenas acodadas.
  - Tórax con placas de quitina.
  - Unas alitas traslúcidas pequeñas y vestigiales que vibran en `Move`.

La **identidad del jugador** se mantiene en todas las versiones con dos detalles:
- Los ojos claros característicos de la célula.
- Un toque de su verde, cada vez menor.

Los colores de la temática *insecto* los decides tú (por ejemplo ámbar, ocre y quitina oscura), pero tienen que convivir con los del Aguijón: lima ácido y morado (linaje Tóxico).

### B. Forma "Luciérnaga" (`Firefly`): Aguijón + Glándula de choque (insecto + eléctrico)

Las mismas 3 versiones, mezclando insecto con eléctrico:
- **Versión 1:** la avispa larvaria sutil, con manchas bioluminiscentes.
- **Versión 2:** un cuerpo más alargado, con élitros incipientes y bandas que brillan.
- **Versión 3:** una luciérnaga clara: élitros, segmentos luminosos y antenas finas.

El brillo usa los azules y cian fríos de la Glándula (linaje Acorazado), que laten en `Idle`.

Tiene que verse **distinta de la avispa larvaria con la misma versión**: ese es el objetivo del sistema.

### C. Rasgo del Aguijón (`Stinger`): abdomen con aguijón, 3 niveles

El Aguijón **ya no es un arma que gira** delante de la célula: ahora es el **abdomen**, que sale de la parte de atrás del cuerpo (anclaje `abdomen`).
- **Rasgo 1:** abdomen pequeño en gota, con franjas lima y morado oscuro y una púa morada clara con su gota de veneno. Toma como referencia el Aguijón rehecho de `generar_evoluciones.py`.
- **Rasgo 2:** abdomen más grande, con más franjas y una púa más larga.
- **Rasgo 3 (Contagio):** abdomen hinchado, con una vesícula de veneno que brilla y gotea. Su evolución hace que los enemigos envenenados estallen.

Animaciones del rasgo:
- **`Idle`:** el abdomen respira y la gota tiembla. Sirve también para moverse; el juego le añade el balanceo del cuerpo con los anclajes.
- **`Attack`** (ataque nuevo): el abdomen **se curva hacia delante**, por debajo o por un lado del cuerpo, como una avispa al picar. El proyectil sale de la punta hacia donde apunta el jugador. Son 4 cuadros:
  1. Se encoge.
  2. Se curva hacia delante.
  3. Estocada con destello en la punta. **El proyectil sale en este cuadro.**
  4. Vuelve.
- **`Death`:** se desprende o se marchita mientras el cuerpo revienta.

Haz el `Attack` en las 3 direcciones. El jugador apunta en cualquier ángulo, pero el cuerpo solo mira a `side`, `down` o `up`, y el juego elige la más cercana.

### D. Rasgo de la Glándula de choque (`ShockGland`): 3 niveles

El órgano ya no va dentro de una célula de 16 px, sino en el anclaje `organ` de cualquier forma:
- **Rasgo 1:** una glándula azul pequeña con una chispa.
- **Rasgo 2:** una glándula más grande, con filamentos conductores por el cuerpo.
- **Rasgo 3 (Paralizante):** una glándula que chisporrotea, con pequeños rayos que saltan.

Animaciones del rasgo:
- **`Idle`:** late.
- **`Activate`:** descarga. Es el momento en que el juego lanza la onda, que ya existe y no tienes que dibujarla.
- **`Death`:** se apaga.

## Formato

**Escala y estilo:**
- 16 píxeles por unidad, filtro nítido (sin antialiasing), fondo transparente.
- Contorno `#181425` de 1 px y luz desde arriba a la izquierda, con 3 o 4 tonos por color, igual que en el arte actual.

**Lienzo del cuerpo:**
- **32×32** con el centro del cuerpo en (16, 16), y **48×48** para `Death`, centro (24, 24).
- La colisión del juego **no cambia**: un círculo de unos 13 px de diámetro en el centro. La "masa" del cuerpo debe quedar centrada ahí; lo que sobresale (antenas, alas) puede salir.
- Crecimiento orientativo: versión 1 ~18×16 px, versión 2 ~22×20 px y versión 3 ~26×24 px.

**Direcciones:**
- `side` mira a la derecha; para la izquierda, el juego voltea el sprite.
- `down` es de frente y `up` de espaldas.

### Hojas del cuerpo (cada forma × cada versión)

Las tiras van en horizontal, con el cuadro 0 a la izquierda.

| Hoja | Cuadros | Lienzo | Notas |
|------|---------|--------|-------|
| `Body_<Forma>_<V>_Idle_<dir>.png` | 4 (bucle) | 32×32 | Reposo, con aplastado y estirado como la célula |
| `Body_<Forma>_<V>_Move_<dir>.png` | 4 (bucle) | 32×32 | Moverse |
| `Body_<Forma>_<V>_Dodge_<dir>.png` | 5 | 32×32 | Rueda encogido. **Los rasgos se ocultan durante la esquiva** |
| `Body_<Forma>_<V>_Hurt.png` | 2 | 32×32 | De lado: blanco y rojo. El juego tiñe los rasgos igual |
| `Body_<Forma>_<V>_Death.png` | 7 | 48×48 | Se infla y revienta, como la célula actual |

`<Forma>` es `WaspLarva` o `Firefly`; `<V>` es `1`, `2` o `3`; `<dir>` es `side`, `down` o `up`.

### Hojas de los rasgos (cada rasgo × cada nivel)

El rasgo se dibuja en su propio lienzo de **32×32**. El punto (16, 16) del lienzo es el que el juego pone sobre el anclaje del cuerpo. Puedes usar más lienzo si el `Attack` lo necesita, pero entonces avísalo en el README.

| Hoja | Cuadros | Notas |
|------|---------|-------|
| `Trait_Stinger_<N>_Idle_<dir>.png` | 4 (bucle) | Sobre el anclaje `abdomen` |
| `Trait_Stinger_<N>_Attack_<dir>.png` | 4 | El cuadro 2 lanza el proyectil |
| `Trait_Stinger_<N>_Death.png` | 7 | 48×48, sincronizado con `Body_…_Death` |
| `Trait_ShockGland_<N>_Idle_<dir>.png` | 4 (bucle) | Sobre el anclaje `organ` |
| `Trait_ShockGland_<N>_Activate_<dir>.png` | 4 | Descarga |
| `Trait_ShockGland_<N>_Death.png` | 7 | 48×48 |

`<N>` es `1`, `2` o `3`.

### Anclajes: `anclajes.json`

Para **cada hoja de cuerpo y cada cuadro**, la posición en píxeles (desde la esquina de arriba a la izquierda del cuadro) de estos puntos:
- **`head`:** cabeza.
- **`back`:** lomo; aquí irán caparazones o aletas dorsales.
- **`abdomen`:** inicio del abdomen o de la cola.
- **`organ`:** donde se ve el órgano.
- **`sides`:** los dos costados, `left` y `right`; aquí irán aletas, patas…
- **`mouth`:** delante de la cabeza; aquí irán armas futuras como las mandíbulas.

En las hojas `Attack` del Aguijón añade también el punto **`tip`** (la punta) de cada cuadro: es de donde sale el proyectil.

Indica además, para cada dirección, si cada anclaje se dibuja **delante o detrás** del cuerpo. Por ejemplo, de frente (`down`) el abdomen queda detrás.

Formato orientativo:
```json
{
  "Body_WaspLarva_1_Idle_side": [
    {"head": [21, 13], "back": [15, 11], "abdomen": [9, 17], "organ": [15, 18], "left": [12, 20], "right": [19, 20], "mouth": [24, 15]}
  ],
  "order": {"side": {"abdomen": "behind"}, "down": {"abdomen": "behind"}, "up": {"abdomen": "front"}}
}
```

Aunque en este piloto solo se usan `abdomen` y `organ`, rellena todos los anclajes: los usarán las siguientes mutaciones.

## Entregables

Guárdalo todo en **`docs/multimedia-temp/evolving-body/`**, la misma carpeta que este PROMPT.md:
1. Todas las hojas PNG de los apartados A, B, C y D.
2. `anclajes.json`.
3. `generar_cuerpo_evolutivo.py`, preparado para añadir formas y rasgos nuevos: temáticas, formas y rasgos definidos como datos, no como código repetido.
4. `vista_previa_cuerpo_evolutivo.png` a ×4 sobre el fondo `#16352e` (el suelo del piso 1), con:
   - Una fila por combinación **montada con los anclajes**, igual que lo hará el juego:
     - Célula sola.
     - Aguijón I, II y III.
     - Aguijón I + Glándula I.
     - Aguijón I + Glándula III (forma Firefly en versión 3, Aguijón con rasgo 1).
     - Aguijón III + Glándula I.
     - Aguijón III + Glándula III.
   - En cada fila: `Idle` y `Move` en las 3 direcciones, el `Attack` del Aguijón y la `Death`.
5. Un GIF por combinación de la vista previa en `gif/`.
6. Un `README.md` con:
   - La tabla de hojas: cuadros, lienzo, punto central y **duración por cuadro**. Toma como referencia los tiempos actuales de la célula: Idle 6 fps, Move 10, Dodge 20, Hurt 10 y Death 10. El `Attack` del Aguijón debe durar unos 0,35 s en total.
   - La paleta de cada temática en hexadecimal.
   - Una explicación corta de cómo añadir una forma o un rasgo nuevos con el script.
