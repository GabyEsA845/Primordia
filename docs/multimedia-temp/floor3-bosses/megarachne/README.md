# Megarachne (minijefe opcional del piso 3)

Euriptérido que durante años se creyó una araña gigante: forma de la maqueta antigua de museo (`referencia_megarachne.png`)
con el guiño del euriptérido (bandas de segmentos en el abdomen, telson corto, vientre de placas al morir). Paleta A
(gris violáceo, marcas rojas, patas color arena con punta negra). Sala lateral grande **el Nido** (Bosque humeante,
suelo 32×20 u, cámara ×1). Todo hecho con la skill `primordia-sprites`; sin importar todavía (va con la rama del piso 3).

## Combate (resumen para el código)

- **Red en estrella**: 5 **cables de seda** (~1 u de ancho) desde Sigillarias del borde hasta las **puntas de las ramas**
  de un **Lepidodendron frondoso y empapado** en el centro; sobre su copa, la telaraña del Nido con un **embudo de ~3 u**
  (su refugio). La copa se dibuja encima del jugador y de las crías y se vuelve translúcida cuando pasan debajo.
- **En los cables es invulnerable**; solo recibe daño en el suelo: tras el salto (~1,5 s), aturdida al caer de un cable
  que arde (~3 s) y mientras teje un cable nuevo (cruza el suelo tendiendo un hilo fino, que al llegar engorda en 1 s).
- **Cable que arde**: el fuego corre como una mecha a **~6 u/s** en los dos sentidos; detrás queda negro y fino, se
  descuelga soltando ceniza y ascuas y al final cae: quedan dos cabos que se deshacen. **Ella cae cuando el frente la
  alcanza** (puede huir). **Las ascuas que caen prenden telarañas y helechos del suelo.** Junto al Nido el fuego se
  apaga con humo mientras el árbol está empapado.
- Ataques: **salto** (marca ámbar que sigue al jugador → roja fija → cae colgada de su hilo), **ráfaga de seda**
  (5-7 bolitas en abanico, como la Phaser Spider; cada una deja una telaraña que frena y arde), **saco de crías**
  (cuelga, tiembla y revienta en 3-4 crías; si se rompe antes, sin crías), **red que cae** (telaraña redonda de ~3 u:
  sombra que crece, cae y atrapa 1 s).
- **Entrada**: tiemblan los cables, se encienden los ojos en el embudo, sale (IntroEmerge), va al borde de la copa, baja
  colgada, aterriza delante del jugador y amenaza (Threat) + rótulo de presentación (`../../boss-intros/`, con el
  sello «MINIJEFE»).
- **Enfado al 50 %**: vuelve al Nido, se mete en el embudo y chilla dentro (Screech + `Fx_Megarachne_Screech`); el
  árbol se sacude y llueve (`Nido_Tree_Shake` + `Fx_Nido_Rain`), la copa se seca (`Nido_Tree_Drying`) y sale
  enfurecida (hojas `Sheets/Rage/`): más rápida, más hilos y crías. **Desde ahí el Nido arde UNA vez**: si el fuego
  llega, arde la copa seca (`Nido_Burn`), caen todos los cables, ella cae aturdida mucho rato y la copa queda
  carbonizada (`Nido_Tree_Charred_Smolder`): teje cables nuevos desde las ramas, pero ya sin refugio.
- **Muerte**: rueda panza arriba (vientre de euriptérido), la red se descuelga (`Nido_WebDecay`) y el cuerpo se queda.
  **Recompensa**: del embudo cae un capullo de seda que se abre con un **contenedor de corazón**.

## Cuerpo (`generar_megarachne.py`)

Picada 55°, motor 3D de `../arthropleura/generar_arthropleura.py`. **Celda 128×128, pivote = centro de la celda = centro
del cuerpo en el suelo** (los pies). 6 u (96 px) de punta a punta de las patas. Hojas de **9 filas** (Up90, Up68, Up45,
Up23, Side, Dn23, Dn45, Dn68, Dn90; Side mira a la derecha) y las otras 7 direcciones con **Flip X**. Cada hoja de
`Sheets/` tiene su sombra en `Shadows/` (negro opaco: el código le da la transparencia, ~110-130, y la pone en el suelo;
si ella está en un cable, la sombra va en el suelo bajo el cable y el sprite en el cable).

| Hoja | Cuadros | ms | Notas |
|---|---|---|---|
| Idle | 4 | 180 | Tantea con las delanteras |
| Walk | 8 | 80 | Tetrápodo; cable y suelo (en el enfado el código acelera) |
| Weave | 8 | 110 | Teje: lleva un tramo de hilo fino hasta el suelo, detrás; el código lo sigue hasta el ancla desde el punto de suelo (Side (18, 64); por dirección en la salida de `todo`) |
| SilkShoot | 8 | 90 | Alza el abdomen como un escorpión; **la seda sale en el cuadro 4**, desde las hileras (Side (76, 39), Up90 (64, 30), Dn90 (64, 49)…) |
| DropWarn | 3 | 120 | Se agacha en el cable antes del salto |
| Drop / Climb | 4 / 4 | 120 / 100 | **1 dirección** (Dn90). **El hilo NO está en el sprite**: el código lo dibuja del ancla (cable o embudo) a las hileras, píxel **(64, 40)** de la celda. La bajada empieza con las hileras en el cable |
| Land | 3 | 90 | Aterriza (+ `Fx_MegarachneDust`) |
| Fall | 4 | 80 | Cae del cable que arde pataleando |
| Stunned / Recover | 4 / 3 | 140 / 110 | Aturdida, tumbada y ladeada; + `Fx_Megarachne_Stunned` (64×28, 8 cuadros) con el ancla por encima de la silueta: Side (88, 14), Dn90 (64, 32)… |
| Hurt | 2 | 80 | Blanco y rojo (sobre Idle 0) |
| Threat | 6 | 110 | Se alza con las delanteras arriba y las agita (entrada, enfado) |
| Death / Dead | 8 / 1 | 100 | Rueda panza arriba, patas encogidas al cielo; placas ventrales del euriptérido. Sin rojo encendido |
| IntroEmerge | 8 | 110 | **1 dirección**, **pivote = centro del embudo** del árbol (celda del árbol (80, 100)): sale del embudo (al revés: se mete) |
| Screech | 6 | 90 | 1 dirección, sobre el embudo: chilla medio fuera; **se enciende en el cuadro 3** |

**Enfado** (`Sheets/Rage/<Hoja>_Rage`): las mismas hojas y filas con rojo encendido, ojos que brillan y espinas de punta
roja en las patas (Idle, Walk, Weave, SilkShoot, DropWarn, Land, Fall, Stunned, Recover, Hurt, Threat, Drop, Climb).

## Árbol del Nido (`generar_nido_megarachne.py piezas` → `Nido/`)

Celda **160×176, pivote = pie del tronco (80, 164)**; centro de la copa 64 px por encima (80, 100); embudo 48×30 px.
Los cables nacen en las puntas de las ramas: `anchor(nombre)` en el generador (NO, N, NE, SO, SE).

| Hoja | Cuadros | Notas |
|---|---|---|
| Nido_Tree_Wet / _Dry / _Charred | 1 | Empapado (inicio), seco (tras el chillido), carbonizado. Misma geometría (se funden píxel a píxel) |
| Nido_Web | 1 | Capa: la telaraña del Nido con el embudo |
| Nido_Eyes | 6 | Capa: los ojos se encienden en el embudo (entrada) |
| Nido_Tree_Shake | 6 | La copa se sacude (el tronco no se mueve) |
| Fx_Nido_Rain | 8 | Capa en bucle: cae el agua de la copa |
| Nido_Tree_Drying | 6 | Empapado → seco en manchas |
| Nido_Burn | 8 | Árbol + Nido ardiendo desde la rama SO; el último cuadro = carbonizado |
| Nido_Tree_Charred_Smolder | 4 | Copa carbonizada que humea (bucle) |
| Nido_WebDecay | 6 | Capa: la seda se descuelga y desaparece (muerte) |

## Efectos (`generar_efectos_megarachne.py [cables rafaga resto]` → `Effects/`)

- **Cable** (LineRenderer con textura en mosaico y varios puntos para la caída): `Cable_Normal` 32×14 (se repite cada
  32 px), `Cable_Thin` 32×4 (3), `Cable_Grow` 32×14 (6: hilo fino → cable), `Cable_Burning` (3, bajo las llamas),
  `Cable_Charred` (3, negro y fino). Encima y **sin girar con el cable**: `Fx_CableFlame` 16×24 (6, una cada ~16 px
  del tramo que arde), `Fx_CableFront` 24×24 (4, en cada frente), `Fx_CableAsh` 16×32 (6), `Cable_Stub` 16×40 (4, el
  ancla arriba en el centro), `Fx_CableSmoke` 24×40 (6, junto al Nido empapado).
- **Ráfaga**: `Proj_MegarachneSilk` 10×10 (4), `Fx_MegarachneSilk_Muzzle` 24×24 (4, en las hileras),
  `Fx_MegarachneWeb_Appear` (3) / `_Idle` / `_Burn` (6), 40×36, en el suelo.
- **Red**: `Fx_MegarachneNet_Shadow` 64×48 (6, aviso), `_Fall` 64×88 (5, pivote abajo (32, 64)), `_Idle`, `_Break` (5),
  `_Burn` (6), `_Trapped` 40×40 (4, capa sobre el jugador 1 s).
- **Saco**: `Megarachne_Sac_Idle` / `_Tremble` (4) / `_Burst` (5, salen las crías) / `_Break` (5, roto antes: sin crías),
  24×32, el hilo se ata arriba en el centro (12, 0).
- **Crías**: `Megarachne_Brood_Move` (4) / `_Bite` (4, el mordisco cuenta en el cuadro 2) / `_Hurt` (2) / `_Death` (5),
  24×24, de lado como la araña de las frondas (Flip X para la izquierda).
- **Salto**: `Fx_MegarachneDrop_Track` 40×28 (4, ámbar, sigue al jugador) / `_Locked` (2, rojo, fijo); `Fx_MegarachneDust` 72×36 (6).
- **Momentos**: `Fx_Megarachne_Screech` 192×128 (6, pivote = embudo), `Fx_Megarachne_Stunned` 64×28 (8).
- **Recompensa**: `Megarachne_Cocoon_Fall` (4, gira) / `_Open` (6) 24×32, con el contenedor de corazón de la tienda.

## Barra de vida (`generar_interfaz_megarachne.py` → `BossBar/`)

Sistema BossBarSkin. **240×28, arriba** (más corta que las de jefe). Ventana **176×6 en (46, 11)**, muesca al **50 %**.
Marco = cable de seda trenzado con ella de frente agarrada a la izquierda y atado a un muñón de rama a la derecha.
`Frame` / `Frame_Rage` (con espinas), `Back`, `Fill_Violet` (normal), `Fill_Silk` (**invulnerable en su cable**),
`Fill_Rage` (3, late), `Fill_Trail`, `Eyes_Calm` / `Eyes_Rage`, `Notch` / `Notch_Broken` (7×14, centrado en la
muesca, y 7), `Fx_NotchBreak` (5, 16×34), `Fx_Screech` (6, 72×40, centrado en el icono (22, 14)), `Fx_Spines` (4,
capa del tamaño del marco: brotan tras el chillido).

## Otros generadores y vistas

- `generar_escala_megarachne.py [estrella poligono nido detalle]`: láminas de escala (6h-0) y la sala con el árbol.
- `generar_escenas_megarachne.py [entrada enfado desplome muerte rafaga red saco salto recompensa]`: escenas montadas
  (`vista_previa_6i_escena_*`, `vista_previa_6j_escena_*` y GIF en `gif/escenas/`); posiciones y tiempos orientativos.
- `generar_megarachne.py escenas [salto curva aturdida]`: escenas de la 6h (la del cable que arde es el concepto previo).
- Vistas previas `vista_previa_*.png` y `gif/`: solo para revisar.
