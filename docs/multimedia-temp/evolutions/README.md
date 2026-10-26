# Evoluciones: Aguijón y Caparazón rehechos, efectos, marcas de estado e insignias

Arte de los niveles y evoluciones de las mutaciones. Se genera con `generar_evoluciones.py`, que reutiliza `generar_menus.py`, `generar_ui.py`, `generar_mutaciones.py`, `generar_jugador.py` y `generar_efectos.py`: el `Canvas`, el contorno, las rampas, los colores de linaje, la forma de la célula, la animación de la Piel y el anillo de la onda.

Para regenerar: `python generar_evoluciones.py <carpeta multimedia-temp> <carpeta de salida>`. Necesita al lado las carpetas `menus/`, `ui/`, `mutations/`, `player/`, `effects/`, `projectiles/` y `enemies/`.

Ajustes de importación en Unity:
- 16 PPU.
- Filtro Point.
- Sin compresión.
- Las hojas animadas van en fila, con el cuadro 0 a la izquierda.

## Hojas

| Hoja | Cuadros | Lienzo | Centro | Bucle | ms por cuadro |
|---|---|---|---|---|---|
| `Mouth/Mouth_Stinger_Idle` | 4 | 24×24 | unión (0, 12) | Sí | 180 |
| `Mouth/Mouth_Stinger_Attack` | 3 | 24×24 | unión (0, 12) | No | 70 |
| `Skin/Skin_Carapace_Idle_<down/side/up>` | 4 | 24×24 | (12, 12) | Sí | 180 |
| `Skin/Skin_Carapace_Move_<down/side/up>` | 4 | 24×24 | (12, 12) | Sí | 110 |
| `Skin/Skin_Carapace_Dodge_<down/side/up>` | 5 | 24×24 | (12, 12) | No | 60 |
| `Skin/Skin_Carapace_Hurt` | 2 | 24×24 | (12, 12) | No | 90 |
| `Skin/Skin_Carapace_Death` | 7 | 32×32 | (16, 16) | No | 110 |
| `Icons/Icon_Stinger_16` / `_32` | 1 | 16×16 / 32×32 | – | – | – |
| `Icons/Icon_Carapace_16` / `_32` | 1 | 16×16 / 32×32 | – | – | – |
| `Projectiles/Projectile_Stinger` | 3 | 12×6 | (6, 3) | Sí | 90 |
| `Projectiles/Projectile_Quill` | 2 | 10×4 | (5, 2) | Sí | 80 |
| `Effects/Heal` | 6 | 24×24 | (12, 12) | No | 70 |
| `Effects/PoisonBurst` | 6 | 64×64 | (32, 32) | No | 70 |
| `Effects/Ambush` | 4 | 32×32 | (16, 16) | Sí | 90 |
| `Effects/Stomp` | 6 | 64×64 | (32, 32) | No | 60 |
| `Effects/Plates` | 4 | 24×24 | (12, 12) | Sí | 140 |
| `Effects/PlatesBreak` | 5 | 32×32 | (16, 16) | No | 70 |
| `Effects/InkCloud_Start` | 4 | 96×96 | (48, 48) | No | 90 |
| `Effects/InkCloud_Loop` | 6 | 96×96 | (48, 48) | Sí | 150 |
| `Effects/InkCloud_End` | 4 | 96×96 | (48, 48) | No | 110 |
| `Effects/InkCloud_Large_Start` | 4 | 128×128 | (64, 64) | No | 90 |
| `Effects/InkCloud_Large_Loop` | 6 | 128×128 | (64, 64) | Sí | 150 |
| `Effects/InkCloud_Large_End` | 4 | 128×128 | (64, 64) | No | 110 |
| `Effects/Shockwave_Large` | 6 | 192×192 | (96, 96) | No | 60 |
| `Effects/LevelUp` | 6 | 32×32 | (16, 16) | No | 70 |
| `Effects/LevelUp_Gold` (extra) | 6 | 32×32 | (16, 16) | No | 70 |
| `Status/Bleed` | 4 | 12×12 | (6, 6) | Sí | 120 |
| `Status/Blind` | 4 | 12×12 | (6, 6) | Sí | 140 |
| `Status/Stun` | 4 | 12×12 | (6, 6) | Sí | 110 |
| `Status/Paralyzed` | 4 | 12×12 | (6, 6) | Sí | 70 |
| `Status/Paralyzed_Body` | 4 | 32×32 | (16, 16) | Sí | 70 |
| `UI/UI_Level_1` / `_2` / `_3` | 1 | 9×5 | (4, 2) | – | – |
| `UI/UI_Level_Large_1` / `_2` / `_3` | 1 | 15×7 | (7, 3) | – | – |
| `UI/UI_Level_Up` | 1 | 7×7 | (3, 3) | – | – |

1 unidad = 16 px. Los efectos de área están dibujados a su radio real, así que van con `localScale` 1, sin ampliar.
- `PoisonBurst` y `Stomp`: radio 2.
- `InkCloud`: radio 3.
- `InkCloud_Large`: radio 4.
- `Shockwave_Large`: radio 6.

## Notas

### Aguijón
- **Formato:** igual que las Bocas actuales. Mira a la derecha y se pega a la célula por (0, 12), así que el código no cambia.
- **Forma:** un pecíolo morado y fino sale de la célula y lleva a un abdomen en gota. El abdomen tiene franjas lima y morado oscuro y termina en una púa morada clara con una gota de veneno.
- **Reposo:** el abdomen respira y se curva un poco, y la gota tiembla.
- **Ataque:**
  - Cuadro 0: el abdomen se encoge.
  - Cuadro 1: estocada con destello en la punta. **Lanza el proyectil en este cuadro.**
  - Cuadro 2: vuelve.

### Caparazón
- **Medidas:** el cuerpo no cambia de tamaño: elipse de radio 9×7, unos 22×18 con contorno. La colisión de 0,55 y la Boca a 8,5 px siguen valiendo.
- **Concha:** escudos poligonales en grises azulados, separados por juntas oscuras, con un reborde claro con marcas alrededor.
  - Probé las juntas claras, pero a este tamaño se veían como ruido.
- **Orientaciones:**
  - Abajo: la cara con la concha asomando detrás.
  - Lado: perfil abombado con la cabeza asomando por delante.
  - Arriba: la concha entera.
- **Esquiva:** la célula se mete en la concha, que rueda o se desliza.
- **Muerte:** la concha se agrieta y la célula se deshace.

### Tinta
- **Opacidad:** la nube está hecha de volutas moradas translúcidas.
  - Opacidad media: unos 36 %.
  - Máximo: 58 %, en el borde de las volutas.
  - La nube actual era opaca.
- **Bordes:** el borde de la nube lleva una línea morada clara, para que se lea dónde acaba.
- **Al disiparse:** las volutas se desvanecen antes de tocar el borde del lienzo.

### Insignias
- **Iconos de 16:** `UI_Level_*` va en (8, 12) respecto a la esquina superior izquierda del icono y pisa 1 px su borde.
- **Iconos de 32:** `UI_Level_Large_*` va en (18, 26).
- **Niveles I y II:** puntos menta.
- **Nivel III:** gemas doradas.
- **Insignia pequeña de nivel III:** 3 puntos de 2×2 con su separación no caben en 9 px, así que las gemas van pegadas.
- **`UI_Level_Up`:** es la misma flecha lima que `UI_Stat_Up` de la pausa.

### Subida de nivel
- `LevelUp` es lima, para los niveles I y II.
- `LevelUp_Gold` es una hoja extra, más dorada, para la evolución (nivel III). Si prefieres una sola hoja, usa `LevelUp` y descarta la dorada.

### Marcas de estado
- **Colocación:** van encima de la cabeza del enemigo.
- **Dos marcas a la vez:** se separan unos 14 px, una al lado de la otra.
- **Sangrado:** gotas rojas.
- **Cegado:** ojo tapado de tinta morada que gotea.
- **Aturdido:** estrellitas amarillo pálido que giran.
- **Paralizado:** rayo azul claro en zigzag.
- **`Paralyzed_Body`:** rayitos alrededor de un hueco de unos 20×20. En el virus gigante queda dentro de su silueta. Si quieres que lo rodee, escálalo ×2 en el juego.

## Vista previa

`vista_previa_evoluciones.png` está ampliada ×3 y tiene 6 secciones:
1. **Aguijón:**
   - En las 4 direcciones de apuntado.
   - El ataque y el proyectil.
   - Los iconos, junto a los antiguos y los de Mandíbulas y Esporas.
2. **Caparazón:**
   - Las 3 orientaciones.
   - Las Mandíbulas a 8,5 px.
   - Esquiva, golpe y muerte.
   - Iconos nuevo y antiguo.
3. **Efectos sobre la célula o los enemigos:**
   - Curación, emboscada y placas.
   - Rotura de placas y erizo.
   - Veneno con 3 amebas y pisotón.
   - La onda normal junto a la grande.
4. **Tinta:** la nube antigua, la nueva y la grande, sobre una sala con 2 bacterias, 2 proyectiles y el jugador.
5. **Marcas de estado:**
   - Las 5 marcas sobre bacteria, ameba y virus.
   - Una bacteria con dos marcas a la vez.
6. **Insignias:**
   - Los 9 iconos de 16 en `UI_Slot_Frame` con los niveles 1 a 3.
   - 3 tarjetas de 32.
   - La flecha de mejora.
   - `LevelUp` y `LevelUp_Gold` sobre la célula.

La fuente de la vista previa es una de 3×5 provisional. `gif/` tiene cada animación por separado.
