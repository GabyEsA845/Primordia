# Estilo por piso

Lo común a todos los pisos está en `SKILL.md`. Aquí van tamaños, paletas y temas de cada uno. Al terminar el arte
de un piso nuevo, añadir aquí su sección (la misma estructura).

## Común

- Contorno `#181425` (`OUTL`), ojo `#f4f4e8` (`EYE`), chispas `SPARK`, burbuja `(200, 236, 255)`.
- Hurt: `WHITE4` y `RED4` (`#a33b3b #e05a5a #ff9a8a #fff0e0`). Muerte apagada `PALE4`, fosilizada `STONE4`, arena `SAND`.
- Colores de ranura (tarjetas, burbujas, iconos de ranura; `LineageSetup.SlotColors`): Boca naranja `#f28059`,
  Extremidades morado `#a859d1`, Piel azul `#8cb3e6`, Órgano lima `#8ce659`.
- Tiles de 16×16. Temas: `floor` (4), `fleck` (3), `algae` (3), `cap` (4), `face` (5), `vein`, `lip`, `drip`, `shadow`.
- Interfaz a escala 1:1 con el juego (CanvasScaler 480×270, 16 PPU); fuente Silkscreen a 8 y 16; rótulos de jefe
  en Jersey 10.

## Piso 1: sopa primordial (microscópico)

- **Tamaños:** célula del jugador 16×16 (`Death` 24×24); piezas de mutación: Boca 24×24, Extremidades 32×32,
  Órgano 16×16 (`Activate` 32×32), Piel 24×24 (`Death` 32×32); enemigos 16-24; jefes hasta 64×64.
- **Fondo de revisión:** `#16352e`.
- **Paletas de criaturas:** célula `#2f7d4f #4fb85f #8be36a #d6ff9a`; bacteria `#8a3a2a #d0603a #f2934e #ffd08a`;
  ameba `#5a3a8c #9a5fd0 #c995f0 #f0d8ff`; virus `#7a2a5a #c8487e #ff7aa8 #ffd0e0` (fase 2 más roja).
- **Temas** (`environment/generar_escenario.py`):
  - `Soup`: suelo verde `#173a30 #1d4639 #245244 #2e6150`; pared morada (cara `#3a2440 #57345c #7a4a78 #a46a98 #c890b8`).
  - `Boss`: suelo granate `#2a1a1c … #553034`; pared roja (cara `#3a1420 … #d07080`).
- Generadores de referencia: `player/generar_jugador.py`, `enemies/generar_enemigos.py`, `evolutions/`, `lineage-evolution/`.

## Piso 2: océano cámbrico

- **Tamaños:** cuerpos del jugador 24×24 (unos 20 px de cuerpo; `Death` 32×32), con la Boca dibujada en el cuerpo
  y ataque propio en las 3 direcciones (6 cuadros); enemigos ~24 (Pikaia ~12 en 16×16, esponja ~28 en 32×32);
  evoluciones de linaje 64×64 (12 cuadros, ~2 s); jefes en lienzos de 112-128.
- **Fondo de revisión:** `#0f2a4a` (arrecife). Probar también sobre Someras y Abismo.
- **Paletas de criaturas:** pez acorazado dorado `#7a5424 #b8843a #e3b85e #fff0b4`; medusa del jugador rosa
  `#6a2a7c #a648aa #e07ad2 #ffd2f4`; lirio de mar cáliz dorado `#5a4a1a #8a7a2a #c8b040 #f0e080` con brazos
  morados sin contorno; trilobite óxido `#4a2418 #8a4a2a #c47a42 #f2b674` con céfalo de acero
  `#22243a #40445c #6a7090 #a4aac4`; medusa enemiga **azul translúcida** `#1a3478 #2f62c4 #6aa8ee #c8ecff`
  (alfa 205); Ottoia `#5a1e2e #a43e52 #e07a84 #ffc8c4`; esponja Vauxia `#5a4012 #9a7424 #d4aa44 #fbe58c`;
  Pikaia `#2e4a44 #5e8a7a #9ccaa8 #e4fff0`.
- **Muertes:** los animales del océano se fosilizan (`STONE4`) o se deshacen en copos; la medusa, en gotas.
- **Clase nueva Carroñero:** marrón arena.
- **Tres franjas de profundidad** (temas en `environment/generar_temas_piso2.py` y `generar_tema_cambrico.py`):
  - **Someras** (`Shallows`): suelo turquesa `#1e6670 #23737a #2a8286 #379694`, roca azul verdosa con vetas
    doradas `#e8c070`, cáusticas en el suelo y rayos de luz diagonales.
  - **Arrecife** (`Cambrian`): suelo azul `#16344a #1b3d55 #214862 #2a5772`, roca azul (cara
    `#1c2c50 #28406c #365890 #4e78b0 #80a8d8`) con vetas de coral `#d0705a`.
  - **Abismo** (`Abyss`): casi negro azulado `#070b16 … #141e34`; las vetas y motas brillan en cian `#36d8d0`,
    `#7ae8ff` (bioluminiscencia con luces 2D suaves; oscuridad real solo en la sala del Anomalocaris).
- Puertas de losas de roca con la paleta de la pared y un sello que brilla en la costura.
- Generadores de referencia: `floor2-bodies/`, `floor2-enemies/`, `floor2-bosses/base_piso2.py`, `floor2-mutations/`.

## Piso 3: pantano carbonífero

- **Tamaños:** criaturas de ~32 px (cuerpos en lienzos de 32×32 o 40×40, `Death` mayor). Al ser más grandes: rampas
  de 5 tonos, luz rebotada (`reflect=True`) y algo más de detalle interior.
- **Fondo de revisión:** `#1c2e20` (musgo oscuro). Probar también sobre Humeante y Quemada.
- **Regla del piso:** el escenario va apagado y oscuro (oliva, pardo, ceniza); lo que importa para jugar va saturado y
  claro (veneno lima, fuego naranja, metano cian pálido, ámbar), así las reacciones se leen en cualquier tramo.
  **Mucha variedad:** cada pieza del escenario sale de parámetros (tipo, forma, semilla, volteo, parte quemada).
  Sin flores en el escenario: las únicas son las moradas del camino secreto.
- **Paletas comunes en `floor3-environment/base_piso3.py`** (fuente única; `floor3-style/` es la prueba aprobada).
- **Tres tramos del incendio** (suelo 4 · motas 3 · cara de pared 5 · tapa 4 · vetas 3):
  - **Pantano verde** (`Swamp`): suelo `#1c2e20 #213626 #273f2b #2f4a32`, musgo `#3d5e36 #557a3e #7fa04a`,
    vetas de musgo `#3f6a2e #68973c #a2c85a`.
  - **Bosque humeante** (`Smoke`): suelo `#22261f … #3a3d31`, hojarasca `#55553f #77704f #a0946a`, musgo reseco
    `#5a4a2a #8a6a34 #c09a4a`; humo entre las hojas.
  - **Tierra quemada** (`Burnt`): suelo `#1d1a1d … #363033`, ceniza `#4a4346 #6d6466 #958a88`, ascuas en las grietas
    `#a8341c #e8682a #ffb24a`; fondo con profundidad (humo, resplandor lejano, troncos partidos), no troncos apilados.
- **Paredes de vegetación:** tapa = espesura desde arriba (frondas en 3 capas); cara = troncos de Lepidodendron
  (cojinetes en rombo), cortina de frondas, musgo y raíces; la espesura se sale al suelo (borde no recto). Maleza
  quemable `#3a3018 #5c4c24 #86703a #b49a52 #dcc47a`, clara y enredada, con un paso oscuro detrás.
- **Terreno y plantas:** lodo profundo `#22160f #33221a #4a3324 #634830 #86683f`; charco de lodo
  `#3a2a1c #55402a #735a3a #9a7e52`; agua turbia `#16302e #1d4440 #2a5e54 #4e8a72 #9cc8a0`; helecho vivo
  `#1c3a20 #2a5a2a #3e8a34 #6abf48 #b8e878`; reseco `#3a2e16 #5e4a20 #8a6e2a #b8963a #e0c870`; carbón
  `#141114 #221c1e #352b2a #4c3e38 #6a5848`; corteza `#1e2420 #2e3830 #435040 #5e6c52 #8a9670`; estróbilos (conos-cofre)
  `#2a1a3a #4a2a6a #7046a0 #a07ad0 #d8c0f0`. Charcos con `blob()` en 7 formas; 5 tipos de helecho.
- **Reacciones:** veneno `#2e5a14 #5aa020 #a8e040 #e8ffc0`; metano (niebla baja translúcida, sin borde duro)
  `#1e5a78 #2a90b8 #6ad0ec #d0f8ff`; fuego `#8a1e14 #d4441c #f88c2a #ffd25a #fff4c0`; nube tóxica
  `#2a2a12 … #e4e478`; ámbar `#6a2e0e #b0581a #e89a30 #ffcc5a #fff0b0`; ascuas `#5a1a14 #a8341c #e8682a #ffb24a`.
- **Clase nueva Parásito:** granate `#3a0e1e #6a1630 #9a2044 #d04a62 #f4a0a4`.
- **Criaturas** (rampa de 5 con `ramp()` del color base · detalle): rana `#c8a028` con manchas azules
  `#0e1e50 #173a8a #2d64c8 #7aa8e8`; Diplocaulus `#d8806a`·`#8a3a4a`; vinagrillo `#8a4a2e`·`#e0b070`; Calamites
  `#6a9a3a`·`#d8d090`; Medullosa `#3a6a3a`·`#a8502a`; libélula `#2aa8a8`·`#c8ecf0`; bombardero `#2a3a6a`·`#e8702a`;
  araña `#5a6a2a`·`#e04040`; sapo `#8a6a3a`·`#c8b070`; Arthropleura cobre oxidado `#6e3a22`·cuero `#c8a070`, ojos negros (rediseño del 2026-10-31); Meganeura
  `#1e7a8a`·`#e8b050`; Megarachne `#4a3a4a`·`#c83a3a`; Lepidodendron ancestral: corteza + estróbilos.
