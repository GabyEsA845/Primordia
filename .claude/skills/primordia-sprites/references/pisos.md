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

## Piso 3 (por diseñar)

- Criaturas de ~32 px (cuerpos en lienzos de 32×32 o 40×40, `Death` mayor). Al ser más grandes: rampas de 5 tonos,
  luz rebotada (`reflect=True`) y algo más de detalle interior. Completar esta sección al cerrar su diseño.
