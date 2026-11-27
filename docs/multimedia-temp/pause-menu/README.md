# Menú de pausa nuevo (estilo membrana) y bestiario

Arte **aprobado** por la usuaria el 2026-11-26 (Opus), **sin importar ni programar**. Sustituye a la pausa actual
(`docs/multimedia-temp/menus/Pause/`, que lo enseñaba todo a la vez). Decisiones y estado en
`docs/memoria-claude/primordia-pause-menu-redesign.md`.

UI a 480×270 con 16 px por unidad: **1 px del sprite = 1 px de la UI**. Textos en Silkscreen 8 (títulos en Bold),
color crema `#f6e8c8` (resaltado `#fffbe8`, secundario `#9ec8b4`, cabeceras `#ffd27a`), con sombra de 1 px `#181425`
debajo. **Silkscreen tiene ¿ ¡ Ñ y tildes**: escribir con la ortografía correcta («CONFIGURACIÓN», «DAÑO»). Las tildes
suben 3 px: el interlineado de varias líneas, 10 px.

## Estructura

- **Raíz:** Reanudar · Configuración · Personaje · Bestiario · Salir (columna centrada sobre el juego oscurecido).
- **Configuración:** música y efectos (deslizador), temblor de pantalla (interruptor), Volver.
- **Personaje** (4 pestañas, Q/E): Criatura (lente, mutaciones y Boca) · Estado (vida con corazones extra, daño,
  velocidad, Inestable, linaje y evolución) · Objetos (reliquias, objetos clave, biomasa) · Partida (piso, semilla,
  tiempo, enemigos derrotados, jefes vencidos).
- **Bestiario** (también desde el **menú principal**; se guarda entre partidas): pestañas Piso 1 · Piso 2 · Piso 3 ·
  Jefes, cuadrícula de retratos y ficha.
- **Salir:** Reiniciar · Menú principal · Volver. Al elegir una de las dos primeras, la confirmación **sustituye** al
  panel (si va encima, el panel translúcido deja ver el de debajo): título de lo elegido, «¿SEGURO?», «PERDERÁS LA
  PARTIDA EN CURSO», [SÍ, SALIR] con el botón de peligro y [NO].
- **Control:** ratón y teclado (flechas o WASD, Q/E para las pestañas, Esc para volver). Sin mando.

## Hojas (`PauseMem/`)

Modo de imagen: **Tiled** donde se indica (el punteado de la membrana tiene periodo 2 y Sliced lo estiraría; las
partes centrales miden un número par de píxeles); en Tiled, `Mesh Type = Full Rect`.

| Hoja | Cuadros | Tamaño | 9-slice (L T R B) | Modo | Uso |
|---|---|---|---|---|---|
| `UI_PauseMem_Panel` | 1 | 52×52 | 14 14 14 14 | Tiled | Todos los paneles (interior translúcido: deja ver el juego) |
| `UI_PauseMem_Title` | 1 | 64×22 | 20 0 20 0 | Sliced | Cinta del título (membrana entre dos células); se centra sobre el borde de arriba del panel |
| `UI_PauseMem_Button_{Normal,Highlighted,Pressed}` | 1 | 42×17 | 9 0 9 0 | Tiled | Botón célula (la píldora va de (1,1) a (41,16); 1 px de margen para el halo). Pulsado: el texto baja 1 px |
| `UI_PauseMem_Nucleus_On` | 4 (bucle, ~150 ms) | 13×13 | – | – | Núcleo que late; **solo en el botón resaltado**, con su centro en (9,8) del botón. Texto centrado en todo el botón |
| `UI_PauseMem_Button_Danger_*` + `Nucleus_Danger_On` | 1 / 4 | 42×17 / 13×13 | 9 0 9 0 | Tiled | Solo «SÍ, SALIR» |
| `UI_PauseMem_Tab_Active` | 1 | 40×24 | 12 0 12 0 | Tiled | Pestaña activa: yema que se funde con el panel. **La línea de arriba del panel cae en la fila 18** (la pestaña pisa 6 filas del panel); colocar las pestañas a **x par respecto al panel** para que case el punteado |
| `UI_PauseMem_Tab_{Idle,Hover}` | 1 | 40×24 | 12 0 12 0 | Tiled | Pestañas inactivas (yema cerrada de la fila 3 a la 18; su interior, filas 6-17, cabe un icono de 12) |
| `UI_PauseMem_TabIcon_{Creature,State,Items,Run}` | 1 | 12×12 | – | – | Pestañas de Personaje; en las inactivas el código los apaga (≈ ×0,6 + un poco de verde) |
| `UI_PauseMem_TabIcon_Floor{1,2,3}` | 1 | 12×12 | – | – | Pestañas del bestiario (célula, amonites, helecho); Jefes = `RunIcon_Bosses` |
| `UI_PauseMem_Key_{Q,E}` | 2 (normal, pulsada) | 13×13 | – | – | A los lados de la fila de pestañas (estilo de `UI_Key_E` de la tienda) |
| `UI_PauseMem_Lens` + `Lens_Glint` + `Lens_Organelles` | 1 / 1 / 8 (bucle, 160 ms) | 112×112 | – | – | Lente-vacuola de Criatura (centro (56,56), interior de radio 47,5, como la actual). Orden: lente, orgánulos, criatura, reflejo. Sustituyen a `UI_Pause_Lens*` |
| `UI_PauseMem_Slot_{Normal,Highlighted}` | 1 | 26×26 | – | – | Casillas de mutación (membrana de (1,1) a (25,25), icono de 16 centrado). **La casilla pasa de 20 a 26** en `ArtSetup.Menus` |
| `UI_PauseMem_Callout_Line` | 1 | 3×3 | 1 1 1 1 | Sliced | Líneas en codo de `CreaturePanel` (mismos tramos de 3 px que ahora) |
| `UI_PauseMem_Callout_Dot` | 4 (bucle, 180 ms) | 7×7 | – | – | Vesícula donde la línea toca la lente |
| `UI_PauseMem_Tooltip` + `Tooltip_Pod` | 1 / 1 | 32×32 / 12×14 | 10 10 10 10 | Tiled | Cuadro de información; el pseudópodo apunta a la izquierda y **el borde izquierdo del cuadro cae en su columna 8** (lo pisa 4 px). Sustituye a `UI_Tooltip` + `UI_Tooltip_Pointer`. La franja de color sigue siendo `UI_Card_Stripe` teñida |
| `UI_PauseMem_Row` | 1 | 24×14 | 6 0 6 0 | Sliced | Franja suave de cada fila de datos (icono a la izquierda, valor a la derecha) |
| `UI_PauseMem_Heart_Extra_Full` | 8 (bucle, 150 ms) | 16×16 | – | – | Corazón extra en ámbar con destello en estrella (cuadros 4-6) |
| `UI_PauseMem_Heart_Extra_{Half,Empty}` | 1 | 16×16 | – | – | Los del HUD teñidos, misma forma |
| `UI_PauseMem_StatIcon_{Unstable,Shake}` | 1 | 12×12 | – | – | Inestable (corazón con grieta ámbar) y temblor de pantalla. Música, efectos, daño y velocidad reutilizan `menus/Pause/UI_Stat_*` |
| `UI_PauseMem_RunIcon_{Floor,Seed,Enemies,Bosses}` | 1 | 12×12 | – | – | Filas de Partida (el tiempo usa `TabIcon_Run`) |
| `UI_PauseMem_Slider_{Track,Fill}` | 1 | 24×9 | 5 0 5 0 | Sliced | Deslizador (el relleno, sin contorno, va encima de la barra con la misma forma) |
| `UI_PauseMem_Slider_Knob` | 1 | 13×13 | – | – | Tirador quieto; agarrado o resaltado = `Nucleus_On` |
| `UI_PauseMem_Toggle_{Off,On}` | 1 | 24×13 | – | – | Interruptor del temblor |
| `UI_PauseMem_BeastSlot_{Normal,Highlighted,Locked}` | 1 | 34×34 | – | – | Casillas del bestiario; retrato de 24 en (5,5) |
| `UI_PauseMem_Beast_Vacuole` + `Beast_Glint` | 1 / 1 | 96×96 | – | – | Ficha del bestiario: el **sprite animado del enemigo** (no el retrato) a la mayor escala entera que quepa en ~64 px; si no cabe a 1x (jefes enormes), el retrato a 3x |

## Retratos del bestiario (`Bestiary/`)

`Portrait_<nombre del EnemyData sin "Enemy_">.png` (24×24, con el fondo de su hábitat y las esquinas redondeadas) y
`Portrait_<…>_Locked.png` (fondo apagado + silueta plana). 26 criaturas:

- **Piso 1:** Chaser (bacteria), Shooter (ameba), Small (amebita).
- **Piso 2:** Trilobite, Pikaia, Ottoia, Vauxia, BlueJelly («MEDUSA AZUL»).
- **Piso 3:** Beetle, Spider, Toad, Dragonfly, MeganeuraNymph, Calamites, CalamitesSprout, Medullosa; **trampas y
  plantas** (borde verdoso; no tienen EnemyData): CarnivorousPlant, Mayflies.
- **Jefes** (borde dorado): AmoebaBoss («AMEBA GIGANTE»), VirusBoss, AnomalocarisBoss, EurypteridBoss, ArthropleuraBoss,
  MeganeuraBoss, MegarachneBoss (minijefe), LepidodendronBoss (**secreto: sin casilla hasta encontrarlo**).

Los nombres visibles y las **notas de naturalista** (3 líneas de ≤ 27 caracteres) están en las listas `FLOOR1`,
`FLOOR2`, `FLOOR3` y `BOSSES` de `generar_retratos.py`: copiarlas a los datos del bestiario.

## Reglas para el código (Sonnet)

- **Bestiario guardado entre partidas** (hoy solo se guardan las opciones con PlayerPrefs): por criatura, descubierta
  sí/no y **veces eliminado**. Se descubre al **eliminarla por primera vez**; hasta entonces, casilla `Locked` con el
  retrato `_Locked` y ficha «???». Contadores «DESCUBIERTOS n/m» por pestaña y total en el título («BESTIARIO 23/38»).
- **Ficha:** vacuola, nombre (Bold), nota de 3 líneas y 4 franjas `Row` de 80 px en 2×2: VIDA, DAÑO, VELOC.,
  ELIMINADOS. Vida y daño del `EnemyData`; **velocidad por la regla** 0 = QUIETA, ≤ 2 = LENTA, < 3,5 = MEDIA,
  < 4,5 = RÁPIDA, si no MUY RÁPIDA. **Los jefes del piso 3 tienen `moveSpeed` 0 y `contactDamage` 0** (IA propia):
  su daño y su velocidad van escritos a mano en los datos del bestiario. Trampas: vida «—».
- **Cuadrícula:** 5 columnas a paso de 34 px; en el piso 3, los 8 enemigos y debajo la etiqueta «TRAMPAS Y PLANTAS»
  con su fila (la etiqueta no sale si el piso no tiene trampas).
- **Corazones extra** = contenedores ganados por encima de la vida base (no el almacén de corazones): fila de
  corazones con los ganados en ámbar y el texto «+N EXTRA» (`#f2c24e`). Hay que separar en el código la vida base de
  la ganada.
- **Partida:** contadores nuevos de tiempo jugado, enemigos derrotados y jefes vencidos (de la partida).
- **No romper la sala de cambio:** `ExchangeRoomSetup` reutiliza la lente, la cinta y los botones de la pausa.
- Montaje actual que hay que cambiar: `Editor/UISetup.cs` (`CreatePanel` «PausePanel»), `Editor/ArtSetup.Menus.cs`
  (`BuildPauseMenu`, casillas a ±75/±72), `ArtSetup.AudioOptions.cs`, `PlayerStatsPanel`, `CreaturePanel`,
  `PauseRelicsRow` (`Core/RunInventory`), `SeedLabel`. Importar con un menú `Primordia > Art > Importar menú de pausa`
  (Multiple/Single, 16 PPU, Point, sin compresión, bordes de la tabla).

## Regenerar

Con Python 3.12 + Pillow, desde esta carpeta (cada uno importa a los anteriores; la paleta sale de los menús actuales):

```
python generar_pausa_membrana.py    # subtanda 1: panel, botón, núcleo, título; maqueta de la raíz
python generar_pausa_personaje.py   # subtanda 2: pestañas, iconos, teclas, lente, casillas y líneas; Personaje
python generar_pausa_detalles.py    # subtanda 3: corazones extra, iconos, cuadro de información, filas
python generar_pausa_opciones.py    # subtanda 4: deslizador, interruptor, botón de peligro; Configuración y Salir
python generar_pausa_bestiario.py   # 5a: casillas, iconos de piso, vacuola; maqueta del bestiario
python generar_retratos.py          # retratos por tandas (piso 1, 2, 3 y jefes) y sus maquetas
```

Vistas previas: `vista_previa_pausa_*.png`, `vista_previa_retratos_*.png`; GIF de revisión en `gif/` (no van a git).
