Necesito arte nuevo en pixel art para los **niveles y evoluciones de las mutaciones** de mi juego (Primordia: roguelike de vista cenital en 3/4, piso 1 bajo el agua, en un caldo primordial). El jugador es una célula verde que muta. Cada mutación tiene ahora 3 niveles: el nivel II mejora sus valores y el nivel III desbloquea una **evolución** (un efecto nuevo). Hace falta: rehacer dos mutaciones (Aguijón y Caparazón), efectos de las evoluciones, marcas de estado sobre los enemigos ("debuffs"), efectos sobre el jugador e insignias de nivel para la interfaz.

Te adjunto los generadores y vistas previas que ya existen, para que todo encaje con el estilo, las paletas, el contorno y los tamaños:
- `generar_jugador.py` (la célula), `generar_mutaciones.py` (piezas de mutación), `generar_enemigos.py` (bacteria, ameba, virus).
- `generar_proyectiles.py` y `generar_efectos.py` (proyectiles, impactos, veneno, tinta, onda).
- `generar_ui.py` (iconos de mutación de 16 y 32, interfaz).
- Sus vistas previas.

Hazlo con un script de Python + Pillow (`generar_evoluciones.py`) que **reutilice las funciones y paletas de esos scripts** (`Canvas`, contorno oscuro, rampas de color, colores de linaje, la forma de la célula y de los enemigos).

## Escala y estilo

- 16 px por unidad del juego. Filtro nítido (sin antialiasing), fondo transparente, contorno `#181425` de 1 px como el resto del arte.
- Luz desde arriba a la izquierda, 3 o 4 tonos por color.
- Hojas animadas en **tira horizontal**, cuadro 0 a la izquierda, todos los cuadros del mismo tamaño.
- Colores de linaje (los mismos de siempre):
  - **Depredador** (Mandíbulas, Aletas, Vejiga de tinta): naranjas y rojos cálidos, hueso claro.
  - **Acorazado** (Patas articuladas, Caparazón, Glándula de choque): grises azulados, brillo frío.
  - **Tóxico** (Aguijón, Esporas, Piel urticante): lima ácido y morados.
- Fondo de referencia para las vistas previas: verde azulado oscuro `#16352e` (el suelo del piso 1).
- **Sin texto** dentro de los sprites.

---

## 1. Aguijón rehecho (Boca, Tóxico)

El aguijón actual no convence. Lo quiero como **el aguijón de un insecto, como la cola de una avispa**: un abdomen segmentado con franjas (en los colores Tóxicos: lima ácido y morado oscuro, en vez del amarillo y negro de la avispa) que termina en una púa afilada y brillante, con una gota de veneno morado en la punta.

Mismo formato que las Bocas actuales, para que el código no cambie:

| Hoja | Cuadros | Lienzo | Notas |
|------|---------|--------|-------|
| `Mouth_Stinger_Idle.png` | 4 (bucle) | 24×24 | Dibujado **mirando a la derecha**, unido por el **borde izquierdo a media altura (0, 12)**: ahí se pega al borde de la célula y desde ahí gira hacia el cursor. El abdomen respira o se curva un poco; la gota tiembla |
| `Mouth_Stinger_Attack.png` | 3 | 24×24 | Mismo encuadre. El abdomen se contrae y la púa da una estocada hacia delante (sale el proyectil) y vuelve |
| `Projectile_Stinger.png` | 3 (bucle) | 12×6 | El proyectil: una púa de avispa suelta (aguja morada con la punta lima y una estela de veneno). Mirando a la derecha, centro (6, 3). Brillo que recorre la púa |
| `Icon_Stinger_16.png` / `Icon_Stinger_32.png` | 1 | 16×16 / 32×32 | Iconos nuevos con la pieza nueva, dibujados a mano en cada tamaño (no reescalados), en diagonal como los demás iconos |

## 2. Caparazón rehecho (Piel, Acorazado)

Lo quiero como **un caparazón de tortuga en la espalda de la célula**: una concha abombada con placas (escudos hexagonales) en grises azulados con brillo frío y bordes claros, que cubre la espalda; la membrana verde de la célula asoma por delante y por los lados. La Piel **sustituye al cuerpo entero** en todas sus animaciones, así que cada hoja es la célula completa con su concha:
- Mirando abajo (de frente): se ve la cara de la célula y, detrás, el borde de la concha asomando por arriba y los lados.
- De lado: el perfil abombado de la concha sobre la espalda.
- Mirando arriba (de espaldas): la concha entera, con sus placas.

Mismos nombres, cuadros y lienzos que el Caparazón actual (y el mismo tamaño aproximado de cuerpo, unos 22×18 dentro del lienzo, para que la colisión de 0,55 y la distancia de la Boca de 8,5 px sigan valiendo; si cambias el tamaño, dime en el README las nuevas medidas):

| Hoja | Cuadros | Lienzo | Notas |
|------|---------|--------|-------|
| `Skin_Carapace_Idle_<down/side/up>.png` | 4 (bucle) | 24×24 | Centro del cuerpo (12, 12). Respira despacio |
| `Skin_Carapace_Move_<down/side/up>.png` | 4 (bucle) | 24×24 | La concha se balancea con el paso |
| `Skin_Carapace_Dodge_<down/side/up>.png` | 5 | 24×24 | Se mete en la concha y rueda o se desliza, como una tortuga |
| `Skin_Carapace_Hurt.png` | 2 | 24×24 | Destello del golpe; la concha aguanta |
| `Skin_Carapace_Death.png` | 7 | 32×32 | Centro (16, 16). La concha se agrieta y la célula se deshace |
| `Icon_Carapace_16.png` / `Icon_Carapace_32.png` | 1 | 16×16 / 32×32 | Iconos nuevos: la concha de tortuga vista desde arriba |

`side` está dibujado mirando a la derecha (el juego lo voltea).

## 3. Efectos de las evoluciones y mejoras

Los "efectos de área" están dibujados **a su radio real** (radio en unidades × 16 × 2 = lado en píxeles), porque el pixel art no se puede ampliar sin deformarse. Por eso algunos tienen dos tamaños.

| Hoja | Cuadros | Lienzo | Bucle | Uso |
|------|---------|--------|-------|-----|
| `Heal.png` | 6 | 24×24 | No | **Mandíbulas:** cuando el jugador se cura al matar. Brillo cálido que envuelve la célula y crucecitas o burbujas rojas y blancas que suben. Va centrado sobre el jugador, encima. Centro (12, 12) |
| `PoisonBurst.png` | 6 | 64×64 | No | **Aguijón III (Contagio):** el enemigo envenenado que muere estalla en una nube de veneno (radio 2). Salpicadura lima y morada que se expande y se deshace en burbujas. Centro (32, 32) |
| `Ambush.png` | 4 | 32×32 | Sí | **Aletas III (Emboscada):** mientras dura el bonus de daño (1,5 s tras esquivar), aura de depredador alrededor de la célula: estelas de velocidad naranjas y un brillo rojizo en el contorno. Debe dejar ver la célula en el centro (unos 16×16 libres). Centro (16, 16) |
| `Stomp.png` | 6 | 64×64 | No | **Patas III (Pisotón):** al acabar la esquiva, golpe contra el suelo: anillo de arena y polvo grisáceo que se expande hasta el radio 2, con piedrecitas. Centro (32, 32) |
| `Plates.png` | 4 | 24×24 | Sí | **Caparazón III (Placas):** mientras el escudo está cargado, un brillo frío que recorre el borde de la concha (muy sutil, es un indicador). Centro (12, 12) |
| `PlatesBreak.png` | 5 | 32×32 | No | **Caparazón III:** el escudo se gasta al parar un golpe: placas azuladas translúcidas que saltan en pedazos. Centro (16, 16) |
| `Projectile_Quill.png` | 2 (bucle) | 10×4 | Sí | **Piel urticante III (Erizo):** púa que sale disparada (salen 8 en círculo al recibir un golpe). Morada con la punta lima, mirando a la derecha. Centro (5, 2) |
| `InkCloud_Start.png` | 4 | 96×96 | No | **Vejiga de tinta (nivel I), rehecha:** la nube aparece. Radio 3. Centro (48, 48) |
| `InkCloud_Loop.png` | 6 | 96×96 | Sí | La nube se mantiene (ahora dura **3 s**, lo mismo que la ceguera): se arremolina despacio |
| `InkCloud_End.png` | 4 | 96×96 | No | La nube se disipa en volutas |
| `InkCloud_Large_Start.png` / `_Loop.png` / `_End.png` | 4 / 6 / 4 | 128×128 | — | Lo mismo a radio 4 (nivel II y III). Centro (64, 64) |
| `Shockwave_Large.png` | 6 | 192×192 | No | **Glándula de choque II y III:** la misma onda que `Shockwave.png` (128×128, radio 4), a radio 6. Centro (96, 96) |

**La tinta tiene que ser bastante más transparente que la actual**: ahora tapa demasiado la vista al jugar. Que se vean los enemigos, los proyectiles y el suelo a través de ella (como tinta diluida en agua: volutas oscuras moradas con zonas casi transparentes, opacidad media del 35-45 % y nunca más del 60 % en los bordes de las volutas). Debe seguir leyéndose dónde acaba la nube.

## 4. Marcas de estado sobre los enemigos

Van **encima de la cabeza** del enemigo mientras dura el estado, en bucle, como el `Poison.png` actual (12×12, burbujas verdes). Mismo tamaño y estilo para todas, cada una con un color y una forma que se distingan de un vistazo, incluso si un enemigo tiene dos a la vez (el juego las coloca una al lado de la otra). Comprueba que se leen sobre la bacteria, la ameba y el virus gigante.

| Hoja | Cuadros | Lienzo | Bucle | Uso |
|------|---------|--------|-------|-----|
| `Bleed.png` | 4 | 12×12 | Sí | **Mandíbulas III (Desgarro):** sangrado. Gotas rojas que caen y salpican |
| `Blind.png` | 4 | 12×12 | Sí | **Vejiga de tinta:** cegado. Manchas o gotas de tinta morada oscura que gotean, o un ojo tachado de tinta |
| `Stun.png` | 4 | 12×12 | Sí | **Patas articuladas (nivel I-II y Pisotón):** aturdido. Burbujitas o estrellitas amarillo pálido que giran en círculo |
| `Paralyzed.png` | 4 | 12×12 | Sí | **Glándula de choque III (Paralizante):** paralizado 2 s. Chispas eléctricas azul claro que saltan en zigzag |

Además, para que la parálisis se note en todo el cuerpo y no solo encima:

| Hoja | Cuadros | Lienzo | Bucle | Uso |
|------|---------|--------|-------|-----|
| `Paralyzed_Body.png` | 4 | 32×32 | Sí | Rayitos azules que recorren el contorno del enemigo paralizado, dibujados alrededor de un hueco central de unos 20×20. Centro (16, 16) |

## 5. Insignias de nivel (interfaz)

Una insignia indica el nivel de la mutación sobre su icono, en el HUD (iconos de 16×16 en las ranuras), en las tarjetas de elección (iconos de 32×32) y en la pausa. **Sin texto ni números romanos dibujados con fuente**: usa **puntos o gemas** (1, 2 o 3), y el nivel III (evolucionado) con un aspecto especial (más brillo, un tono dorado o del linaje).

| Hoja | Cuadros | Lienzo | Notas |
|------|---------|--------|-------|
| `UI_Level_1.png` / `_2.png` / `_3.png` | 1 | 9×5 | Para iconos de 16×16: va en la esquina inferior derecha, pisando 1 px el borde del icono. 1, 2 o 3 puntos de 2×2 sobre una pastilla oscura |
| `UI_Level_Large_1.png` / `_2.png` / `_3.png` | 1 | 15×7 | Lo mismo para los iconos de 32×32 de las tarjetas, con puntos de 3×3 |
| `UI_Level_Up.png` | 1 | 7×7 | Flechita hacia arriba (lima) que el juego pone en las tarjetas de mejora junto a "Nivel I → II" |
| `LevelUp.png` | 6 | 32×32 | Efecto sobre el jugador al subir de nivel una mutación: anillo de luz que sube y se cierra con chispas, más dorado en el nivel III. Centro (16, 16) |

---

## Qué me tienes que entregar

```
evolutions/
  Mouth/      (Mouth_Stinger_Idle, Mouth_Stinger_Attack)
  Skin/       (Skin_Carapace_… las 11 hojas)
  Icons/      (Icon_Stinger_16/32, Icon_Carapace_16/32)
  Projectiles/(Projectile_Stinger, Projectile_Quill)
  Effects/    (Heal, PoisonBurst, Ambush, Stomp, Plates, PlatesBreak, InkCloud_…, InkCloud_Large_…, Shockwave_Large, LevelUp)
  Status/     (Bleed, Blind, Stun, Paralyzed, Paralyzed_Body)
  UI/         (UI_Level_…, UI_Level_Large_…, UI_Level_Up)
  gif/        (cada animación por separado)
  vista_previa_evoluciones.png
  generar_evoluciones.py
  README.md   (tabla con cada hoja: cuadros, lienzo, centro, bucle o no, y ms por cuadro recomendados)
```

La **vista previa** (ampliada ×3, sobre `#16352e`) debe mostrar:
1. **Aguijón nuevo:** la célula con el aguijón de avispa en las 4 direcciones de apuntado (derecha, abajo, izquierda volteado, arriba), su ataque cuadro a cuadro y el proyectil; los iconos nuevos junto a los antiguos de Mandíbulas y Esporas para comparar.
2. **Caparazón nuevo:** la célula con la concha de tortuga en reposo de las 3 orientaciones, el ataque con Mandíbulas encima (para ver que la Boca encaja a 8,5 px) y los iconos nuevos.
3. **Efectos:** cada uno sobre la célula o sobre un enemigo para ver su tamaño real: curación, estallido de veneno junto a 3 amebas, emboscada, pisotón, placas y su rotura, las púas del erizo saliendo en círculo, la onda grande junto a la normal.
4. **Tinta:** la nube nueva sobre una sala con 2 bacterias, 2 proyectiles enemigos y el jugador dentro, comparada con la nube actual; tienen que verse claramente a través.
5. **Marcas de estado:** las 5 sobre la bacteria, la ameba y el virus gigante, y una bacteria con sangrado y aturdimiento a la vez.
6. **Insignias:** los 9 iconos de 16×16 con niveles 1, 2 y 3 dentro de `UI_Slot_Frame`, y 3 iconos de 32×32 con sus insignias grandes; la flecha de mejora y el efecto de subir de nivel sobre la célula.

Comprueba que el aguijón se lee como cola de avispa a 24×24, que la concha se reconoce como de tortuga en las 3 orientaciones, que las 5 marcas de estado se distinguen entre sí a 12×12 y que la tinta deja ver el juego.
