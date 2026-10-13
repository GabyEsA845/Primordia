| Listo || Listo || Listo || En curso || En curso || Listo || Listo || Listo || En curso |# Primordia: lista de contenido multimedia

Lista viva de sprites, música, efectos y fuentes que necesita el juego. Se actualiza cada vez que se añade, cambia o termina un recurso. El diseño está en [el GDD](GDD%20Primordia.md) y el orden de trabajo en [PLAN.md](PLAN.md).

## Cómo usar esta lista

**Prioridad:**
- **M**: necesario para la primera versión publicada (v1.0).
- **D**: deseable para la v1.0 si da tiempo.
- **S**: más adelante.

**Estado:** `Pendiente` → `Placeholder` (forma o sonido provisional) → `En curso` → `Listo`.

## Especificaciones técnicas

| Tipo | Formato | Ajustes en Unity | Carpeta |
|------|---------|------------------|---------|
| Sprites | `.png` (fuente en `.aseprite`) | 16 píxeles por unidad, filtro *Point*, sin compresión | `Assets/_Primordia/Art/` |
| Personajes | 16×16 a 32×32 px; jefe hasta 64×64 px | Animaciones con Sprite Sheet | `Art/Characters/` |
| Tiles | 16×16 px | Tile Palette + Tilemap | `Art/Tiles/` |
| Música | `.ogg` en bucle | *Streaming*, sin pausas al repetir | `Assets/_Primordia/Audio/Music/` |
| Efectos de sonido | `.wav` cortos | *Decompress On Load* | `Audio/SFX/` |
| Fuentes | `.ttf` / `.otf` pixel | Fuente TextMeshPro sin suavizado | `Art/Fonts/` |

### Carpeta temporal: `docs/multimedia-temp/`

Los recursos nuevos (hojas de sprites, GIF de revisión, scripts que los generan) se dejan primero en `docs/multimedia-temp/<recurso>/`. Esa carpeta **no se sube al repositorio** (está en `.gitignore`); lo que se sube es lo que Unity importa a `Assets/_Primordia/Art/`.

| Recurso | Carpeta temporal | Cómo se importa |
|---------|------------------|-----------------|
| Jugador | `docs/multimedia-temp/player/` (`Player_<Acción>_<dirección>.png`, `Player_Hurt.png`, `Player_Death.png`) | Menú **Primordia > Art > Importar animaciones del jugador**: copia las hojas a `Art/Characters/Player/`, las corta (cuadros del alto de la hoja, pivote centrado), crea los clips y el Animator en `Animations/` y se los pone al prefab del jugador. Se puede repetir cada vez que cambie el arte. |
| Enemigos y jefe | `docs/multimedia-temp/enemies/<Enemigo>/` (`Bacteria`, `Ameba`, `AmebaSmall`, `BossVirus`; hojas `<Enemigo>_<Animación>.png`) | Menú **Primordia > Art > Importar animaciones de enemigos y jefe**: igual que el jugador, en `Art/Characters/Enemies/<Enemigo>/`. Aplica cada uno a su prefab (`Enemy_Chaser`, `Enemy_Shooter`, `Enemy_Small`, `Boss_Virus`) con escala 1, y ajusta los tiempos de juego a las animaciones: aviso de disparo de la ameba, aviso de ráfaga e invocación del jefe, pausa de la transición a la fase 2, división al final de Split y tiempo de muerte. |
| Proyectiles | `docs/multimedia-temp/projectiles/` (`Projectile_<Nombre>.png`, el mismo nombre que su prefab) | Menú **Primordia > Art > Importar animaciones de proyectiles**: copia las hojas a `Art/Projectiles/`, crea un clip y un controlador por proyectil (se reproduce solo al salir del pool) y los aplica a los prefabs de `Prefabs/Combat` con escala 1, conservando el radio de colisión. Las celdas no cuadradas se indican en `ArtSetup` (`Stinger`: 12×6). Mandíbulas no dispara: muerde, y el mordisco se ve en la animación `Attack` de su pieza de Boca; `Projectile_Mandibles.png` ya no se importa. |
| Efectos | `docs/multimedia-temp/effects/` (cada hoja con su nombre, sin prefijo común) | Menú **Primordia > Art > Importar efectos**: crea los prefabs de `Prefabs/Effects` (los que no van en bucle se destruyen solos al acabar, con `OneShotEffect`) y los conecta: impactos en los prefabs de proyectil y en la mordida, veneno en el Aguijón, burbujas en los enemigos, estela en el jugador. La tinta y la onda sustituyen al círculo de `Prefabs/Mutations`; `AbilityMutation.effectRadius` indica a qué radio están dibujadas para no agrandarlas. Necesita haber importado antes enemigos y proyectiles. |
| Piezas de mutación | `docs/multimedia-temp/mutations/<Skin|Mouth|Limbs|Organ>/` (`<Ranura>_<Mutación>_<Animación>.png`) | Menú **Primordia > Art > Importar piezas de mutación**: copia cada mutación a `Art/Mutations/<Mutación>/`, crea sus clips y su controlador y se lo pone a su `Mutation_<Mutación>` (campo *Look*). La Boca se corta con el pivote en su punto de unión (borde izquierdo, a media altura). En las Pieles fija también el radio de colisión (Caparazón 0,55; Piel urticante 0,45) y la distancia de la Boca. Añade al jugador las piezas `Limbs`, `Organ` y `AimPivot/MouthPiece` con `MutationLook`, pone en rojo el destello de daño y borra los `Visual_*` provisionales. Necesita haber importado antes al jugador. |
| Objetos e interfaz | `docs/multimedia-temp/ui/<Items|Icons|HUD|Panels>/` | Menú **Primordia > Art > Importar objetos e interfaz**: importa los sprites sueltos a `Art/UI/` (los de panel, en 9-slice) y pone a cada `Mutation_<Mutación>` sus iconos de 16 y 32. Viste `GameUI` y `MainMenuUI` (escala 1:1 con 16 px por unidad, corazones, botones y tarjetas con un sprite por estado, franja teñida, iconos de mutación y de linaje en las tarjetas y en la lista de la pausa) la fila de ranuras del cuerpo bajo los corazones (marco, icono de la mutación o silueta de la ranura libre) y la mira (con su animación al disparar), y la barra de recarga del jugador (con destello al quedar lista). Crea `Prefabs/Mutations/MutationPickup` (burbuja del linaje en `Art/Items/`, icono encima, sombra y efecto al reventar) y se lo da al jefe con `MutationDrop`. La biomasa aún no se importa: se programará con la tienda. |
| Menús de pausa y principal | `docs/multimedia-temp/menus/<Pause|Main>/` | Menú **Primordia > Art > Importar menús**: importa los sprites a `Art/UI/Menus/` (paneles en 9-slice, capas que se desplazan con Wrap Mode = Repeat) y la fuente pixel sin suavizado. **Pausa** (`GameUI`): el título en su cinta, a la izquierda las estadísticas (`PlayerStatsPanel`: vida, daño y velocidad con flecha si cambian respecto a la célula base) y a la derecha la criatura (`CreaturePanel`): lente, retrato animado con sus mutaciones a ×3 o ×2 según quepa (`CreaturePortrait`), líneas en codo hasta los 4 recuadros de ranura (`SlotCallout`) y cuadro de información al pasar el ratón (`MutationTooltip`). Sustituye a la lista de mutaciones anterior. **Menú principal** (`MainMenuUI`): fondo en 4 capas (`ScrollingLayer`, `UISpriteAnimation`), logo con brillo cada 3 s, la célula ×3, botones con marcador y burbuja al pulsar (`MenuSelector`) y la versión. Pone Silkscreen a todos los textos (tamaños 8 y 16). Necesita haber importado antes objetos e interfaz y al jugador. |
| Minimapa y mapa grande | `docs/multimedia-temp/map/` | Menú **Primordia > Art > Importar mapa**: importa los sprites a `Art/UI/Map/` y lee los 8 colores de `UI_Map_Palette` (en el orden de `MapPainter.Swatch`). Añade a `GameUI` el minimapa de 88×68 arriba a la derecha (fondo, ventana recortada de 80×60 y marco) y el mapa grande de TAB (velo al 75 %, panel de 400×220, título "MAPA" sobre `UI_Pause_Title` y reflejo), y configura `MapUI`. El plano lo pinta el juego (`MapPainter`, 1 px por celda): visitadas enteras, vecinas en silueta, sala actual y salas con enemigos con su color; marcas del jugador, el jefe y las mutaciones sin recoger. Necesita haber importado antes los menús. |
| Escenario | `docs/multimedia-temp/environment/<Soup|Boss>/` (`tiles/`, `deco/`, `Door_*.png`, `Doorway_Top.png`) y, para salas con formas, `docs/multimedia-temp/environment-shapes/<Soup|Boss>/tiles/` | Menú **Primordia > Art > Importar escenario**: crea un tile por imagen (las paredes con colisión), la decoración (algas y burbujas como tiles animados) y un `RoomTheme` por variante en `Art/Environment/`. Después hay que regenerar el piso (**Primordia > UI > Regenerar escena Game**). Las salas son plantillas de texto (`Data/Rooms/`) que un pintor viste con el tema; las celdas sin pieza salen en magenta. |

`.png`, `.aseprite`, `.wav`, `.ogg`, `.mp3`, `.ttf` y `.otf` se guardan con Git LFS (ya configurado en `.gitattributes`).

## Sprites

### Jugador (célula, piso 1)

| Recurso | Descripción | Animaciones | Prioridad | Estado |
|---------|-------------|-------------|-----------|--------|
| Célula base | Criatura del piso 1, mira hacia el cursor | Reposo, moverse, esquivar, recibir daño, morir | M | Listo |
| Capa de mutación: Mandíbulas | Arma en la boca (pilar "Eres lo que comes") | Reposo, atacar | D | Listo |
| Capa de mutación: Aguijón | | Reposo, atacar | D | Listo |
| Capa de mutación: Esporas | | Reposo, atacar | D | Listo |
| Capa de mutación: Aletas | | Reposo, moverse | D | Listo |
| Capa de mutación: Patas articuladas | | Reposo, moverse | D | Listo |
| Capa de mutación: Caparazón | Sustituye al cuerpo | Todas las del jugador | D | Listo |
| Capa de mutación: Piel urticante | Sustituye al cuerpo | Todas las del jugador | D | Listo |
| Capa de mutación: Vejiga de tinta | | Reposo, activar | D | Listo |
| Capa de mutación: Glándula de choque | | Reposo, activar | D | Listo |

**Piezas de mutación** (encargo en `docs/multimedia-temp/mutations/PROMPT.md`; cambios muy visibles, al estilo de *Everything is Crab*):

- **Piel** (Caparazón, Piel urticante): sustituye al cuerpo en todas sus animaciones (24×24; `Death` 32×32) y agranda la colisión.
- **Boca** (Mandíbulas, Aguijón, Esporas): arma de 24×24 dibujada mirando a la derecha, unida por el borde izquierdo a media altura. Se coloca en el borde del cuerpo (6 px en la célula, 8,5 con Caparazón, 7 con Piel urticante) y gira hacia el cursor. `Idle` (4 cuadros) y `Attack` (3, en cada disparo o mordisco; en Mandíbulas es lo único que se ve del mordisco).
- **Extremidades** (Aletas, Patas articuladas): 32×32 centradas, `Idle` y `Move` en las tres direcciones; van al mismo cuadro que el cuerpo.
- **Órgano** (Vejiga de tinta, Glándula de choque): 16×16 dentro de la célula; `Activate` (32×32) al usar la habilidad, junto con la nube o la onda.
- Orden de dibujo, de atrás hacia delante: Extremidades, cuerpo, Órgano, Boca (mirando hacia arriba, la Boca va detrás del cuerpo). Las piezas se ocultan al esquivar y al morir; al recibir daño destellan en rojo mientras el cuerpo hace `Hurt`.

### Enemigos y jefe (piso 1)

| Recurso | Descripción | Animaciones | Prioridad | Estado |
|---------|-------------|-------------|-----------|--------|
| Bacteria | Persigue al jugador | Moverse, recibir daño, morir | M | Listo |
| Ameba | Se divide al morir | Moverse, disparar, dividirse, morir | M | Listo |
| Ameba pequeña | Resultado de la división | Moverse, morir | M | Listo |
| Virus gigante (jefe) | Dispara en espiral | Reposo, atacar, recibir daño, morir | M | Listo |
| Tercer enemigo | Por definir | Por definir | D | Pendiente |

### Proyectiles y efectos visuales

| Recurso | Descripción | Prioridad | Estado |
|---------|-------------|-----------|--------|
| Proyectil base del jugador | Disparo sin mutación de Boca | M | Listo |
| Proyectil: Aguijón | Envenena | M | Listo |
| Proyectil: Espora | Lento, en abanico de 3 | M | Listo |
| Proyectil enemigo | Lento, de color fuerte y fácil de leer | M | Listo |
| Proyectil del jefe | Mini virión para la espiral y orbe grande para la ráfaga | M | Listo |
| Impacto | Al golpear un objetivo o una pared: lima los disparos y la mordida del jugador (`Effect_Impact_Player`), rosa los enemigos (`Effect_Impact_Enemy`) | M | Listo |
| Muerte de enemigo | Burbujas encima de la muerte (`Effect_EnemyDeath`); no en la división de la ameba ni en el jefe | M | Listo |
| Estela de esquiva | Una silueta cada 0,05 s mientras esquiva (`Effect_DodgeTrail`, componente `DodgeTrail`) | D | Listo |
| Veneno | Burbujas en bucle sobre el enemigo mientras dura (`Effect_Poison`) | D | Listo |
| Nube de tinta | Habilidad Vejiga de tinta; dibujada a radio 3 (96 px) | M | Listo |
| Onda de choque | Habilidad Glándula de choque; dibujada a radio 4 (128 px) | M | Listo |

### Escenario (sopa primordial)

| Recurso | Descripción | Prioridad | Estado |
|---------|-------------|-----------|--------|
| Tileset de suelo | Charco microscópico, tonos verdes; variantes y sombra bajo la pared | M | Listo |
| Tileset de paredes | Bordes de las salas rectangulares (tapa, cara en 2 filas, bordes y esquinas) | M | Listo |
| Paredes para salas con formas | `Wall_FaceUpper/Lower_EndLeft/Right` y `Wall_OuterTopLeft/Right`, para entrantes y columnas (`docs/multimedia-temp/environment-shapes/`) | M | Listo |
| Puerta abierta / cerrada | Membrana animada de 5 cuadros en los 4 lados y arco fijo arriba | M | Listo |
| Decoración | Algas y burbujas animadas, diatomeas y motas, repartidas al azar (5 % del suelo) | D | Listo |
| Sala del jefe | Variante `Boss` del tileset, con restos infectados | D | Listo |

### Objetos e interfaz

| Recurso | Descripción | Prioridad | Estado |
|---------|-------------|-----------|--------|
| Biomasa | Moneda que cae al limpiar una sala. Arte hecho; falta programarla (con la tienda) | M | Pendiente |
| Mutación en el suelo | Burbuja del linaje con el icono dentro; solo la suelta el jefe (3, se coge 1) | M | Pendiente |
| Iconos de mutación (×9) | Uno por mutación del GDD | M | Pendiente |
| Corazones | Lleno, medio y vacío | M | Pendiente |
| Mira del ratón | Sustituye al cursor mientras se juega (cursor normal en pausa, elección, derrota y victoria); se contrae en cada disparo o mordisco | M | Pendiente |
| Ranuras del cuerpo en el HUD | Boca, Extremidades, Piel, Órgano especial, bajo los corazones. Saltan al cambiar; la del Órgano se oscurece mientras recarga | M | Pendiente |
| Recarga de habilidad | Indicador del clic derecho | M | Pendiente |
| Tarjetas "elige 1 de 3" | Fondo para la elección de mutación | M | Pendiente |
| Logo de *Primordia* | Título del menú principal, con brillo que lo recorre | M | Listo |
| Fondo del menú principal | Caldo primordial en 4 capas: agua, rayos de luz, partículas que suben y siluetas que pasan | D | Listo |
| Minimapa y mapa grande | Minimapa en el HUD y mapa con TAB: marco, panel de vidrio, paleta del plano e iconos (jugador, jefe, recompensa) | M | Listo |
| Menú de pausa | Estadísticas, criatura en una lente con sus ranuras y cuadro de información | M | Listo |
| Pantallas de victoria y derrota | | M | Pendiente |
| Iconos de linaje | Depredador, Acorazado, Tóxico | D | Pendiente |
| Portada para la página de itch.io | 630×500 px, captura o arte promocional | M | Pendiente |

## Música

| Pista | Uso | Ambiente | Prioridad | Estado |
|-------|-----|----------|-----------|--------|
| Menú principal | Bucle | Calmado y orgánico | D | Pendiente |
| Piso 1: sopa primordial | Bucle en salas | Acuático, burbujeante, con ritmo | D | Pendiente |
| Jefe: virus gigante | Bucle en la sala del jefe | Intenso | D | Pendiente |
| Victoria | Fragmento corto | | D | Pendiente |
| Derrota | Fragmento corto | | D | Pendiente |

## Efectos de sonido

| Efecto | Cuándo suena | Prioridad | Estado |
|--------|-------------|-----------|--------|
| Disparo base | Clic izquierdo sin mutación | D | Pendiente |
| Disparo: Mandíbulas | Mordida | D | Pendiente |
| Disparo: Aguijón | | D | Pendiente |
| Disparo: Esporas | | D | Pendiente |
| Esquiva | Espacio | D | Pendiente |
| Jugador recibe daño | | D | Pendiente |
| Jugador muere | | D | Pendiente |
| Enemigo recibe daño | | D | Pendiente |
| Enemigo muere | | D | Pendiente |
| Ameba se divide | | D | Pendiente |
| Disparo enemigo | | D | Pendiente |
| Ataque en espiral del jefe | | D | Pendiente |
| Puertas se cierran / se abren | Al entrar y al limpiar una sala | D | Pendiente |
| Recoger biomasa | | D | Pendiente |
| Obtener mutación | | D | Pendiente |
| Habilidad: Vejiga de tinta | Clic derecho | D | Pendiente |
| Habilidad: Glándula de choque | Clic derecho | D | Pendiente |
| Interfaz: navegar y confirmar | Menús | D | Pendiente |

## Fuentes

| Recurso | Uso | Prioridad | Estado |
|---------|-----|-----------|--------|
| Fuente pixel | Silkscreen (normal y negrita) en HUD, menús y textos, a tamaño 8 y 16. En `Art/Fonts/` | M | Listo |

## Registro de licencias

Anotar aquí todo recurso que no sea propio (bancos de sonido, fuentes, etc.) para respetar sus licencias y dar créditos en la página de itch.io.

| Recurso | Autor / fuente | Licencia | Enlace |
|---------|---------------|----------|--------|
| Fuente Silkscreen | Jason Kottke (Google Fonts) | SIL Open Font License 1.1 (texto en `Art/Fonts/Silkscreen-OFL.txt`) | https://fonts.google.com/specimen/Silkscreen |
