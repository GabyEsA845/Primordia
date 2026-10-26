# Escenario: sopa primordial (piso 1) y sala del jefe

También hay temas generados aparte con las mismas piezas: `Amoeba/` (sala de la ameba, `generar_tema_ameba.py`) y, para la sala de paso, `Shore/` (mitad sopa, mitad océano) y `Cambrian/` (océano cámbrico del piso 2, con esponjas, corales, conchas de trilobite y lirios de mar), los dos con `generar_tema_cambrico.py`.

Dos variantes con las mismas piezas y nombres: `Soup/` (salas normales) y `Boss/` (sala del jefe, tonos rojizos y decoración infectada). Mira primero `Sala_muestra_cerrada_x3.png` y `Sala_muestra_abierta_x3.png` de cada carpeta: son una sala de 22×15 tiles armada con estas piezas.

**Importar en Unity:** 16 PPU, filtro *Point*, compresión *None*. `Tileset_*.png` en modo *Multiple*, *Slice* → *Grid By Cell Size* 16×16, y se arrastra a una *Tile Palette*. Los tiles sueltos están en `tiles/`.

## Tiles (16×16) en `Tileset_Soup.png` / `Tileset_Boss.png`

Orden en la hoja (8 columnas, de izquierda a derecha y de arriba abajo):

| Tile | Capa | Colisión | Dónde va |
|------|------|----------|----------|
| `Floor_0`…`Floor_5` | Suelo | No | Suelo. `Floor_0` es el base; usa las variantes 1 a 5 de vez en cuando (más o menos 1 de cada 3 tiles) |
| `Floor_ShadowTop_0/1` | Suelo | No | Primera fila de suelo bajo la pared superior (sombra) |
| `Wall_Cap` | Paredes | Sí | Relleno de pared visto desde arriba: encima de la cara superior y fuera de la sala |
| `Wall_FaceUpper_0/1` | Paredes | Sí | Cara frontal de la pared superior, fila de arriba |
| `Wall_FaceLower_0/1` | Paredes | Sí | Cara frontal, fila de abajo (toca el suelo, con gotas) |
| `Wall_EdgeLeft` / `Wall_EdgeRight` | Paredes | Sí | Columna de la pared izquierda / derecha (también tapa los extremos de la cara) |
| `Wall_EdgeBottom` | Paredes | Sí | Fila de la pared inferior |
| `Wall_CornerBL` / `Wall_CornerBR` | Paredes | Sí | Esquinas de abajo a la izquierda / derecha |

Esquema de una sala (como la muestra):

```
Cap  Cap   Cap   …  Cap   Cap
EdgeL FaceUpper  …  FaceUpper EdgeR
EdgeL FaceLower  …  FaceLower EdgeR
EdgeL ShadowTop  …  ShadowTop EdgeR
EdgeL Floor      …  Floor     EdgeR
…
CornerBL EdgeBottom … EdgeBottom CornerBR
```

## Puertas (2 tiles de ancho)

| Archivo | Tamaño | Uso |
|---------|--------|-----|
| `Doorway_Top.png` | 32×32 | Arco abierto en la pared superior: va en lugar de 2×2 tiles de cara. Siempre visible |
| `Door_Top.png` | 5 × 32×32 | Membrana tipo esfínter que tapa el arco |
| `Door_Bottom.png` | 5 × 32×16 | Membrana en la pared inferior (el hueco lleva suelo debajo) |
| `Door_Side.png` | 5 × 16×32 | Membrana en las paredes laterales; para la derecha, *Flip X* |

Cuadros de las membranas: 0 = abierta (solo el borde) → 4 = cerrada. Al cerrar la sala se reproduce 0→4; al abrirla, 4→0 y luego `Door` oculta el objeto como ya hace. Unos 70–90 ms por cuadro. En `gif/` se ven en movimiento.

## Decoración (no bloquea) en `deco/`

| Archivo | Cuadros | Tamaño | Notas |
|---------|---------|--------|-------|
| `Deco_Bubbles.png` | 2 | 16×16 | Burbujas; alternar los cuadros despacio (~400 ms) |
| `Deco_Algae.png` | 4 | 16×16 | Algas que se mecen (~200 ms) |
| `Deco_Diatom.png` / `Deco_DiatomLong.png` | 1 | 16×16 | Diatomeas (solo `Soup`) |
| `Deco_Specks.png` | 3 | 16×16 | Motas sueltas para salpicar el suelo (solo `Soup`) |
| `Deco_InfectedRemains.png` | 2 | 16×16 | Restos de células infectadas (solo `Boss`) |
| `Deco_Goo.png` | 1 | 32×16 | Charco infectado (solo `Boss`) |

Ponlas en una capa de Tilemap encima del suelo (o como sprites sueltos), sin colisión y por debajo de los personajes.

## Obstáculos (bloquean; para más adelante, solo `Soup`)

`Obstacle_Rock.png` (16×16), `Obstacle_RockBig.png` (32×32) y `Obstacle_Colony.png` (32×32, colonia de bacterias). Necesitan colisión y bloquear proyectiles.

`generar_escenario.py` vuelve a generar todo: `python3 generar_escenario.py <carpeta>` (necesita Pillow).

# Piso 2: franjas de profundidad (Someras, Arrecife, Abismo)

`generar_temas_piso2.py` genera `Shallows/` (Someras) y `Abyss/` (Abismo) con las mismas piezas y nombres que los demás temas (tileset, puertas, `tiles/`, y los 6 tiles de formas en `../environment-shapes/<Tema>/tiles/`). El Arrecife es el tema `Cambrian` que ya existía; este script solo le añade obstáculos. Revisa `vista_previa_franjas_piso2.png` (de arriba abajo: Someras, Arrecife, Abismo) y `<Tema>/Sala_franja_x3.png`.

## Someras (`Shallows/`): arena turquesa, mucha luz

| Archivo | Cuadros | Tamaño | Notas |
|---|---|---|---|
| `deco/Deco_Seagrass.png` | 4 | 16×16 | Hierba marina que se mece (~200 ms) |
| `deco/Deco_Brachiopod.png` | 1 | 16×16 | Concha en abanico |
| `deco/Deco_Ripples.png` | 1 | 16×16 | Ondas de arena, sin contorno |
| `deco/Deco_Bubbles.png` | 2 | 16×16 | Burbujas |
| `deco/Overlay_Caustics.png` | 4 | 64×64 | Luz ondulante sobre el suelo, **en mosaico** (periodo 64). Capa encima del suelo y debajo de personajes, modo de mezcla aditivo o normal; ~180 ms por cuadro |
| `deco/Overlay_LightRay.png` | 4 | 48×128 | Tres rayos diagonales translúcidos que se mecen (~250 ms). Encima de todo, 2-3 por sala, colgando de la pared superior |
| `deco/Obstacle_Rock.png` / `Obstacle_RockBig.png` | 1 | 16 / 32 | Roca arenisca (bloquea) |
| `deco/Obstacle_Archaeocyath.png` | 1 | 32×32 | Arrecife de arqueociatos, conos dorados (bloquea) |

## Arrecife (`Cambrian/`): obstáculos nuevos

`deco/Obstacle_Rock.png` (16), `Obstacle_RockBig.png`, `Obstacle_CoralBig.png` y `Obstacle_SpongeCluster.png` (32×32). Bloquean.

## Abismo (`Abyss/`): casi negro, bioluminiscencia

Oscuro pero sin tapar la vista: la paleta es legible y la decoración que brilla lleva **luces 2D suaves** (URP *Light 2D* de tipo *Point*, sin sombras) para dar ambiente; la oscuridad de verdad se reserva para la sala del Anomalocaris. Las vetas de las paredes y del suelo ya están pintadas en cian.

| Archivo | Cuadros | Tamaño | Luz 2D sugerida |
|---|---|---|---|
| `deco/Deco_SeaPen.png` | 4 | 16×16 | Pluma de mar que late (~250 ms). Luz cian `#6ae8f0`, radio 1,5 u, intensidad 0,4–0,7 al ritmo del latido |
| `deco/Deco_Ctenophore.png` | 4 | 16×16 | Medusa peine con destellos de colores (~150 ms). Luz `#a0c8ff`, radio 1 u, 0,3 |
| `deco/Deco_GlowMotes.png` | 3 | 16×16 | Motas que parpadean (~300 ms), sin luz |
| `deco/Deco_Bubbles.png` | 2 | 16×16 | Burbujas |
| `deco/Obstacle_Rock.png` / `Obstacle_RockBig.png` | 1 | 16 / 32 | Roca negra con vetas cian (bloquea) |
| `deco/Obstacle_Spire.png` | 1 | 32×32 | Aguja de roca con líquenes que brillan (bloquea). Luz cian tenue, radio 2 u, 0,3 |

## Nieve marina (`Ocean/`)

`Particle_MarineSnow.png`: 4 copos de 8×8 (variantes, no animación) para un sistema de partículas que cae despacio y en diagonal. Densidad orientativa por sala: Someras pocos (~10), Arrecife ~30, Abismo ~70.

## Puertas de losas del piso 2 (`Shallows/`, `Cambrian/`, `Abyss/`)

`generar_puertas_piso2.py` hace las puertas nuevas del piso 2: dos losas de la misma piedra que la pared (bloques tallados con la paleta de su cara) salen de dentro de la pared y se juntan; en la costura dentada se completa un **sello** con el color de las vetas del tema. El piso 1 conserva la membrana. Mira `vista_previa_puertas_piso2.png` (una franja por tema: arriba cerrar, abajo abrir) y `<Tema>/gif/Puertas_losas.gif`.

| Archivo | Cuadros | Tamaño | Uso |
|---|---|---|---|
| `Door_Top_Close.png` / `Door_Top_Open.png` | 7 / 6 | 32×32 | En el arco (`Doorway_Top`), mismo sitio que la membrana. Las losas solo ocupan el hueco del arco |
| `Door_Side_Close.png` / `Door_Side_Open.png` | 7 / 6 | 16×32 | Pared lateral izquierda; para la derecha, *Flip X*. Vistas desde arriba, conservan el labio del borde |
| `Door_Bottom_Close.png` / `Door_Bottom_Open.png` | 7 / 6 | 32×16 | Pared inferior |

- **Cerrar** (`_Close`): cuadro 0 vacío → las losas salen → chocan con polvo y burbujas (cuadro 5) → se asientan. El último cuadro es la puerta cerrada. Duraciones: `70, 60, 60, 60, 60, 110, 140` ms.
- **Abrir** (`_Open`): el sello se enciende → brilla en blanco y las losas tiemblan → se retiran. El último cuadro está vacío (luego `Door` oculta el objeto). Duraciones: `130, 90, 60, 60, 60, 60` ms.
- **Código** (al integrarlas): hoy `Door` reproduce los mismos cuadros al revés para abrir y usa una sola duración; hará falta que acepte una lista de cuadros para abrir y duraciones por cuadro. Los `Door_Top/Side/Bottom.png` de membrana de estos temas siguen ahí (la sala de paso los usa) hasta que se cambie.
