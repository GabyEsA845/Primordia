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

Los recursos nuevos (hojas de sprites, GIF de revisión, scripts que los generan) se dejan primero en `docs/multimedia-temp/<recurso>/` y desde ahí los importan a `Assets/_Primordia/Art/` los menús de Unity. La carpeta se sube al repositorio (los PNG por Git LFS) salvo los GIF y las cachés de Python, para poder regenerar el arte desde cualquier equipo.

**Cómo se hace el arte:** con la skill de Claude Code `primordia-sprites` (`.claude/skills/primordia-sprites/`): reglas del estilo y de cada piso, la biblioteca común `pixel.py` (lienzo, rampas con el tono desplazado, sombra, hojas, vistas previas y GIF), una plantilla de generador y `lint_sprites.py`, que revisa las hojas (dibujo cortado, píxeles huérfanos, contorno grueso, colores de más) antes de enseñarlas.

| Recurso | Carpeta temporal | Cómo se importa |
|---------|------------------|-----------------|
| Jugador | `docs/multimedia-temp/player/` (`Player_<Acción>_<dirección>.png`, `Player_Hurt.png`, `Player_Death.png`) | Menú **Primordia > Art > Importar animaciones del jugador**: copia las hojas a `Art/Characters/Player/`, las corta (cuadros del alto de la hoja, pivote centrado), crea los clips y el Animator en `Animations/` y se los pone al prefab del jugador. Se puede repetir cada vez que cambie el arte. |
| Enemigos y jefe | `docs/multimedia-temp/enemies/<Enemigo>/` (`Bacteria`, `Ameba`, `AmebaSmall`, `BossVirus`; hojas `<Enemigo>_<Animación>.png`) | Menú **Primordia > Art > Importar animaciones de enemigos y jefe**: igual que el jugador, en `Art/Characters/Enemies/<Enemigo>/`. Aplica cada uno a su prefab (`Enemy_Chaser`, `Enemy_Shooter`, `Enemy_Small`, `Boss_Virus`) con escala 1, y ajusta los tiempos de juego a las animaciones: aviso de disparo de la ameba, aviso de ráfaga e invocación del jefe, pausa de la transición a la fase 2, división al final de Split y tiempo de muerte. |
| Proyectiles | `docs/multimedia-temp/projectiles/` (`Projectile_<Nombre>.png`, el mismo nombre que su prefab) | Menú **Primordia > Art > Importar animaciones de proyectiles**: copia las hojas a `Art/Projectiles/`, crea un clip y un controlador por proyectil (se reproduce solo al salir del pool) y los aplica a los prefabs de `Prefabs/Combat` con escala 1, conservando el radio de colisión. Las celdas no cuadradas se indican en `ArtSetup` (`Stinger`: 12×6). Mandíbulas no dispara: muerde, y el mordisco se ve en la animación `Attack` de su pieza de Boca; `Projectile_Mandibles.png` ya no se importa. |
| Efectos | `docs/multimedia-temp/effects/` (cada hoja con su nombre, sin prefijo común) | Menú **Primordia > Art > Importar efectos**: crea los prefabs de `Prefabs/Effects` (los que no van en bucle se destruyen solos al acabar, con `OneShotEffect`) y los conecta: impactos en los prefabs de proyectil y en la mordida, veneno en el Aguijón, burbujas en los enemigos, estela en el jugador. La tinta y la onda sustituyen al círculo de `Prefabs/Mutations`; `AbilityMutation.effectRadius` indica a qué radio están dibujadas para no agrandarlas. Necesita haber importado antes enemigos y proyectiles. |
| Piezas de mutación | `docs/multimedia-temp/mutations/<Skin|Mouth|Limbs|Organ>/` (`<Ranura>_<Mutación>_<Animación>.png`) | Menú **Primordia > Art > Importar piezas de mutación**: copia cada mutación a `Art/Mutations/<Mutación>/`, crea sus clips y su controlador y se lo pone a su `Mutation_<Mutación>` (campo *Look*). La Boca se corta con el pivote en su punto de unión (borde izquierdo, a media altura). En las Pieles fija también el radio de colisión (Caparazón 0,55; Piel urticante 0,45) y la distancia de la Boca. Añade al jugador las piezas `Limbs`, `Organ` y `AimPivot/MouthPiece` con `MutationLook`, pone en rojo el destello de daño y borra los `Visual_*` provisionales. Necesita haber importado antes al jugador. |
| Objetos e interfaz | `docs/multimedia-temp/ui/<Items|Icons|HUD|Panels>/` | Menú **Primordia > Art > Importar objetos e interfaz**: importa los sprites sueltos a `Art/UI/` (los de panel, en 9-slice) y pone a cada `Mutation_<Mutación>` sus iconos de 16 y 32. Viste `GameUI` y `MainMenuUI` (escala 1:1 con 16 px por unidad, corazones, botones y tarjetas con un sprite por estado, franja teñida, iconos de mutación y de linaje en las tarjetas y en la lista de la pausa) la fila de ranuras del cuerpo bajo los corazones (marco, icono de la mutación o silueta de la ranura libre) y la mira (con su animación al disparar), y la barra de recarga del jugador (con destello al quedar lista). Crea `Prefabs/Mutations/MutationPickup` (burbuja del linaje en `Art/Items/`, icono encima, sombra y efecto al reventar) y se lo da al jefe con `MutationDrop`. La biomasa tiene su propio menú (fila siguiente). |
| Biomasa | `docs/multimedia-temp/ui/Items/Item_Biomass_*.png` y `ui/HUD/UI_Biomass.png` | Menú **Primordia > Art > Importar biomasa**: importa las animaciones a `Art/Items/Biomass/` (150 ms en reposo, 60 al recoger), crea `Prefabs/Items/Biomass_Small` (vale 1) y `Biomass_Big` (vale 5) y `Prefabs/Effects/Effect_BiomassCollect`, pone las probabilidades de cada `Enemy_*` (perseguidor y tirador 40 % pequeña y 10 % grande, crías 10 % pequeña, jefe 10 fijos; sus invocaciones no sueltan nada), da al jugador `PlayerBiomass`, a `Floor_1` las piezas y el mínimo de 2 por sala, y añade al HUD el contador bajo las ranuras. |
| Tienda | `docs/multimedia-temp/shop/` (`Shop_*.png`, `Item_*.png`, `UI_Key_E.png`, `UI_Map_Shop.png`) | Menú **Primordia > Art > Importar tienda** (después de Importar objetos e interfaz, biomasa y menús): importa a `Art/Shop/` a Darwin (Idle y Buy con sus tiempos, un Animator `Shop_Darwin`), el mostrador, el pedestal y su brillo con el pivote abajo, y los artículos y el efecto de compra a `Art/Shop/Items/`. Monta `Prefabs/Shop/Shop` (mostrador con Darwin detrás y 4 pedestales con sombra, artículo flotante, tecla E y precio en un lienzo del mundo), crea `Prefabs/Effects/Effect_ShopPurchase`, da a `Floor_1` la tienda con la sala pequeña, pone `UI_Map_Shop` al mapa, el cartel `ShopTooltip` al HUD y `PlayerInteractor` al jugador. También importa de `docs/multimedia-temp/shop-unavailable/` a `Art/Shop/Marks/` la señal de vendido (`Shop_Unavailable`) y el letrero de no disponible (`Shop_Unavailable_Sign`), 4 cuadros de 16×16 a 150 ms. |
| Menús de pausa y principal | `docs/multimedia-temp/menus/<Pause|Main>/` | Menú **Primordia > Art > Importar menús**: importa los sprites a `Art/UI/Menus/` (paneles en 9-slice, capas que se desplazan con Wrap Mode = Repeat) y la fuente pixel sin suavizado. **Pausa** (`GameUI`): el título en su cinta, a la izquierda las estadísticas (`PlayerStatsPanel`: vida, daño y velocidad con flecha si cambian respecto a la célula base) y a la derecha la criatura (`CreaturePanel`): lente, retrato animado con sus mutaciones a ×3 o ×2 según quepa (`CreaturePortrait`), líneas en codo hasta los 4 recuadros de ranura (`SlotCallout`) y cuadro de información al pasar el ratón (`MutationTooltip`). Sustituye a la lista de mutaciones anterior. **Menú principal** (`MainMenuUI`): fondo en 4 capas (`ScrollingLayer`, `UISpriteAnimation`), logo con brillo cada 3 s, la célula ×3, botones con marcador y burbuja al pulsar (`MenuSelector`) y la versión. Pone Silkscreen a todos los textos (tamaños 8 y 16). Necesita haber importado antes objetos e interfaz y al jugador. |
| Minimapa y mapa grande | `docs/multimedia-temp/map/` | Menú **Primordia > Art > Importar mapa**: importa los sprites a `Art/UI/Map/` y lee los 8 colores de `UI_Map_Palette` (en el orden de `MapPainter.Swatch`). Añade a `GameUI` el minimapa de 88×68 arriba a la derecha (fondo, ventana recortada de 80×60 y marco) y el mapa grande de TAB (velo al 75 %, panel de 400×220, título "MAPA" sobre `UI_Pause_Title` y reflejo), y configura `MapUI`. El plano lo pinta el juego (`MapPainter`, 1 px por celda): visitadas enteras, vecinas en silueta, sala actual y salas con enemigos con su color; marcas del jugador, el jefe y las mutaciones sin recoger. Necesita haber importado antes los menús. |
| Niveles y evoluciones | `docs/multimedia-temp/evolutions/<Mouth|Skin|Icons|Projectiles|Effects|Status|UI>/` | Menú **Primordia > Art > Importar evoluciones**: Aguijón (cola de avispa) y Caparazón (concha de tortuga) rehechos, con sus iconos y el proyectil nuevo del Aguijón; la púa del Erizo (`Projectile_Quill`); los efectos de las evoluciones en `Prefabs/Effects` (`Effect_Heal`, `_PoisonBurst`, `_Ambush`, `_Stomp`, `_Plates`, `_PlatesBreak`, `_Shockwave_Large`, `_LevelUp`, `_LevelUp_Gold`) y las marcas de estado (`Effect_Status_Bleed`, `_Blind`, `_Stun`, `_Paralyzed`, `_ParalyzedBody`); la nube de tinta en tres fases con `InkCloudArea` (`Effect_InkCloud` radio 3 y `Effect_InkCloud_Large` radio 4) y las insignias de nivel en el HUD, la pausa y las tarjetas. Al final ejecuta **Primordia > Mutations > Configurar niveles y evoluciones**, que pone los valores de cada nivel, sus textos y sus efectos (y crea los ataques `Attack_<Mutación>_2/3`). Los menús antiguos de piezas, iconos, proyectiles y efectos ya no pisan el arte rehecho. |
| Ajustes de las Bocas | `docs/multimedia-temp/mouth-tweaks/` (`generar_bocas.py`: `Mouth/`, `Cap/`, `Projectiles/`, `Icons/`) | Menú **Primordia > Art > Importar ajustes de las Bocas**: el Aguijón pequeño (el script lo copia también a `evolutions/Mouth/`) con su punto de disparo en la punta, el gorrito de las Esporas en `Art/Mutations/Spores/` (`Cap_Spores.controller`, pieza `Cap` del jugador, orden 13) con sus iconos, la altura de la cabeza de cada Piel y la espora crema. Al final ejecuta **Configurar niveles y evoluciones** (bocanada que flota de las Esporas y avance del mordisco). El script copia la espora y los iconos también a `projectiles/` y `ui/Icons/`, así los menús antiguos no vuelven a poner los de antes. |
| Beneficios de linajes | `docs/multimedia-temp/lineages/` (`generar_linajes.py`: `HUD/UI_Heart_Barrier`, `Effects/FungusMat`, `FungusMat_Large`, `StingingTrail`) | Menú **Primordia > Art > Importar beneficios de linajes**: el medio corazón verde de la barrera en los corazones del HUD, y los prefabs `Effect_FungusMat` (anillo de hadas, radio 1,2), `Effect_FungusMat_Large` (radio 1,56) y `Effect_StingingTrail` (radio 0,45) con `MyceliumPatch`, en el suelo (orden 3 y 4). Al final ejecuta **Primordia > Mutations > Crear linajes**, que pone al jugador los valores y efectos de los beneficios, y al HUD el cartel del linaje y su cuadro en la pausa. |
| Evolución de linajes | `docs/multimedia-temp/lineage-evolution/` (`generar_evolucion_linajes.py`: `Limbs/`, `Mouth/`, `Cap/`, `Evolution/`, `Effects/LineageLost`) | Menú **Primordia > Art > Importar evolución de linajes** (después de Crear linajes): las piezas de cada linaje en `Art/Lineages/<Linaje>/` (Extremidades 32×32 con los estados del jugador, Bocas 24×24 con la unión en el borde izquierdo, gorrito del Cuesco 24×24), la animación de la evolución de cada uno (`Prefabs/Effects/Effect_Evolution_<Linaje>`, 48×48, 12 cuadros en unos 2 s) y el pof al perderlo (`Effect_LineageLost`, blanco: el juego lo tiñe). Pone a cada `Lineage_<Linaje>` su aspecto (la cola del Escorpión dispara a 15,6 px de la unión) y al jugador `LineageEvolution`. |
| Audio | `docs/multimedia-temp/audio/` (`generar_audio.py` con `sintesis.py`, `efectos.py` y `musica.py`: `SFX/*.wav`, `Music/*.ogg`, `sonidos.json` y `muestrario.html`; encargo en `PROMPT.md`) | Menú **Primordia > Audio > Importar audio**: copia los archivos a `Audio/SFX` (mono, ADPCM, Decompress On Load; los bucles en Vorbis) y `Audio/Music` (Vorbis, Streaming), crea `Audio/Resources/SoundLibrary` con el volumen y el grupo de cada sonido (de `sonidos.json`), cuántas veces seguidas puede sonar y su variación de tono, y conecta los sonidos que van en los datos: disparo, golpe, avance e impacto de cada `Attack_*`, daño, muerte, división, alerta y aviso de cada `Enemy_*`, la esquiva de `Mutation_Fins` y un `EffectSound` en los efectos que suenan al aparecer (pisotón, placas, subida de nivel, compra, marcas de estado, burbujas que revientan...). El `AudioManager` se crea solo al empezar si existe la biblioteca, pone sonido a los botones y al jugador de cada escena, y su `MusicDirector` elige la música. Se puede repetir cada vez que cambie el audio. |
| Escenario | `docs/multimedia-temp/environment/<Soup|Boss>/` (`tiles/`, `deco/`, `Door_*.png`, `Doorway_Top.png`) y, para salas con formas, `docs/multimedia-temp/environment-shapes/<Soup|Boss>/tiles/` | Menú **Primordia > Art > Importar escenario**: crea un tile por imagen (las paredes con colisión), la decoración (algas y burbujas como tiles animados) y un `RoomTheme` por variante en `Art/Environment/`. Después hay que regenerar el piso (**Primordia > UI > Regenerar escena Game**). Las salas son plantillas de texto (`Data/Rooms/`) que un pintor viste con el tema; las celdas sin pieza salen en magenta. |

**Arte del piso 2 (hecho, sin importar todavía).** Cada carpeta tiene su script generador, un README con tamaños, cuadros y duraciones, y vistas previas. Los menús de importación se escriben en la rama de cada módulo (ver [PLAN.md](PLAN.md)).

| Recurso | Carpeta temporal | Contenido |
|---|---|---|
| Cuerpos del jugador | `docs/multimedia-temp/floor2-bodies/` (`generar_cuerpos_piso2.py`, `generar_iconos_piso2.py`) | Medusa, pez acorazado y lirio de mar (24×24, muerte 32×32) con ataque propio en 3 direcciones; arpón, destello, mordisco y remolino; iconos de 16 y 32 de las 9 mutaciones |
| Enemigos | `docs/multimedia-temp/floor2-enemies/` (`generar_enemigos_piso2.py`) | Trilobite, Medusa azul, Ottoia (pivote 0,5; 0,25), Esponja Vauxia y Pikaia con moverse, aviso, ataque, daño y muerte; carga de la medusa y efectos de curación y escudo de la esponja |
| Jefes | `docs/multimedia-temp/floor2-bosses/` (`base_piso2.py`, `generar_anomalocaris.py`, `generar_euripterido.py`, `generar_sala_anomalocaris.py`, `generar_sala_euripterido.py`, `generar_euripterido_final.py`, `generar_barra_anomalocaris.py`, `generar_barra_euripterido.py`) | Anomalocaris (112×112) y Euriptérido (128×128) con todas sus fases y escenas, la muda vacía, burbujas, marcadores del zarpazo, púa y barrido de cola; columnas de algas, plantas luminosas y burbuja que daña de la sala del Anomalocaris; rocas agrietadas que se derrumban, escombros y derrumbe de la cueva (sombra y piedra que cae, impacto, chinas, suelo que se hunde) de la sala del Euriptérido; barra de vida propia del Anomalocaris (258×32: marco, relleno y ojos por fase, silueta, muescas que se rompen); último aliento del Euriptérido (caer rendido, levantarse, furia y gota venenosa) y su barra propia (marco por fase, caparazón que se agrieta y salta) |
| Evolución de linajes | `docs/multimedia-temp/floor2-lineages/` (`generar_evolucion_piso2.py`) | Las 8 evoluciones (64×64, 12 cuadros, unos 2 s), que parten del cuerpo de la Boca de cada linaje |
| Carroña (Carroñero) | `docs/multimedia-temp/floor2-mutations/Effects/` (`generar_carrona.py`) | Restos (16×16, bucle), restos dobles dorados, trozos al comerlos (24×24) y marca de 1-3 colmillos (12×8). Los importa, con las 8 evoluciones, el menú **Primordia > Mutations > Crear linajes del piso 2**: prefabs `CarrionRemains`, `CarrionRemains_Double`, `Effect_Carrion_Eat`, `Effect_Status_Carrion_1/2/3` y `Effect_Evolution_<Linaje>` |
| Escenario | `docs/multimedia-temp/environment/<Shallows|Cambrian|Abyss>/` y `Ocean/` (`generar_temas_piso2.py`, `generar_puertas_piso2.py`), formas en `environment-shapes/<Shallows|Abyss>/tiles/` | Temas de Someras y Abismo (el Arrecife es `Cambrian`), decoración, obstáculos de cobertura de las tres franjas, cáusticas y rayos de luz, nieve marina y puertas de losas (`Door_<Top|Side|Bottom>_<Close|Open>`) |
| Salas especiales y fosas | `docs/multimedia-temp/floor2-rooms/` (`generar_salas_especiales.py`, `generar_fosas.py`, `generar_piezas.py`), fosas en `environment/<Shallows|Cambrian|Abyss>/pits/` | Los 10 planos en formato de `RoomTemplate` con su comprobación y vistas previas; fosas de las tres franjas (16 tiles por vecinos, esquinas de dentro y motas); almejas pequeña y grande (cofres), coral del corazón morado y su icono del HUD, entradas de oleadas, esqueleto del fósil y sombras de los que nadan |
| Escenario del piso 3 | `docs/multimedia-temp/floor3-environment/` (`base_piso3.py`, `generar_tilesets_piso3.py`, `generar_escenario_piso3.py`, `generar_piezas_piso3.py`), por tramo en `<Swamp|Smoke|Burnt>/`; prueba de estilo en `floor3-style/` | Tres tramos del incendio: tiles con variantes que encajan, copa que cuelga sobre la cara, solapas de espesura, lodo profundo (16 máscaras), arco y puertas de raíces, maleza quemable; helechos que se mecen, se sacuden y arden, charcos de lodo y agua en 7 formas, obstáculos, odres de metano, musgo que gotea, fumarolas, metano, bichos; conos-cofre, tronco puente, flores moradas, puerta secreta, vaina-almacén de corazones, bloque de ámbar y teletransportes de los pisos 1-3 (en `pieces/`). Sin importar todavía |
| Cuerpos del jugador del piso 3 | `docs/multimedia-temp/floor3-bodies/` (`generar_cuerpos_piso3.py`) | Rana venenosa, Diplocaulus y vinagrillo (32×32; ataque, giro y muerte en 40×40): esquiva propia de cada uno, Escupitajo a empujones, Peonza en bucle con mareo, Chorro con el abdomen alzado; mueren hundiéndose en el lodo. Diplocaulus y vinagrillo, modelados en volumen. Efectos de las Bocas (`generar_efectos_bocas_piso3.py`): bola viscosa, charcos de veneno, reventón, remolino, chorro de vinagre que se repite, Salpicadura; iconos de 32 y 16. Sin importar todavía |
| Enemigos y trampas del piso 3 | `docs/multimedia-temp/floor3-enemies/` (`generar_enemigos_piso3.py`) | Calamites madre (32×48) y retoños que persiguen, Medullosa que lanza vainas de veneno, planta carnívora al estilo de los dibujos (trampa, 40×40); escarabajo bombardero, araña de las frondas (escondida = capa sobre un helecho) y sapo de fango; libélula gigante y enjambre de efímeras. Escarabajo, araña y libélula con la cámara 3D del jugador. Efectos: cono ardiente, telarañas que arden, esporas, vainas, lodo, sombra de voladores y línea de aviso. Sin importar todavía |
| Mutaciones del piso 3 | `docs/multimedia-temp/floor3-mutations/` (`generar_mutaciones_piso3.py`) | Por niveles: Extremidades y Órganos solo con efectos (salto, hilo de seda, nube de metano y su explosión, feromonas, parásito), Pieles que retocan el cuerpo por dentro (anfibia, carbonizada), especiales del ámbar que cambian el modelo (cercos, costra, glándula) y el bloque de ámbar compartido; iconos de 32 y 16. Sin importar todavía |
| Reacciones del piso 3 | `docs/multimedia-temp/floor3-reactions/` (`generar_reacciones_piso3.py`) | Nube tóxica en columna mostaza (nace, bucle, se va), charco de veneno que se prende con llamas de base lima, niebla de metano que arde por casillas con retardo y llamaradas sueltas, chispa de ignición, marca de envenenado, fuego que se apaga en vapor; por tramo, odre de metano que revienta y sus restos, y planta venenosa (bulbo que respira, revienta en charco y rebrota). Sin importar todavía |
| Jefe Arthropleura (piso 3) | `docs/multimedia-temp/floor3-bosses/arthropleura/` (`generar_arthropleura.py`, `generar_efectos_arthropleura.py`, `generar_escala_arthropleura.py`, `generar_escenas_arthropleura.py`, `generar_interfaz_arthropleura.py`) | Milpiés gigante de cobre oxidado en picada, por piezas encadenadas (cabeza 160 px, 28 segmentos de 2 placas y cola de 112 px) en 16 direcciones: andar, colmillos, alzarse, embestida, comer; capas de estado (ardiendo, chamuscado, grietas), placas abiertas con la carne rosada, rueda y muerte; efectos del combate (ondas expansivas, pisotón, temblor, embestida, pared y borde que revientan, brecha, Sigillaria derribada) y cresta continua sobre la espesura. Láminas de escala y escenas de prueba. Interfaz: barra de vida de 20 placas (320×32, 4 estados, muescas 70/35 %, efectos de cambio de fase) y flechas de borde (insignias + punteros en 16 direcciones). Sin importar todavía |
| Jefe Meganeura (piso 3) | `docs/multimedia-temp/floor3-bosses/meganeura/` (`generar_meganeura.py`, `generar_ninfa_meganeura.py`, `generar_efectos_meganeura.py`, `generar_tormenta_meganeura.py`, `generar_interfaz_meganeura.py`, `generar_escala_meganeura.py`) | Libélula gigante en picada alta (10 u de envergadura, celda 192, 16 direcciones), verde azulado con alas translúcidas con venas: vuelo, cernido, aviso y picado, remontar, aleteo fuerte, posarse, posada, poner huevos, daño; sombras por animación; 5 variantes de alas (quemadas y rasgadas por fase); golpes (clavada en el tronco, se arranca, electrocutada, cae, boca arriba, se recupera, capa de llamas); presentación con reflejo azulado, chillido de furia, posada furiosa, muerte en espiral con el ala suelta y restos en la charca; silueta a contraluz. Ninfa (16 direcciones, labio extensible, muertes por fuego y electricidad) y racimos de huevos. Escenario: Lepidodendron partido grande, Sigillaria pararrayos que se parte y arde, charcas electrificadas y charcos que crecen. Efectos: marca del picado, impacto, ráfaga, rayos grande/sueltos/primer trueno, corriente circular, lluvia, nubes, viento y vapor. Interfaz: barra de vida «alas» (300×32, rellenos por fase e invulnerable, marco que se rasga, muescas 65/30 %). Láminas de escala y escenas (GIF en `gif/escenas/`). Sin importar todavía |
| Minijefe Megarachne (piso 3) | `docs/multimedia-temp/floor3-bosses/megarachne/` (`generar_megarachne.py`, `generar_nido_megarachne.py`, `generar_efectos_megarachne.py`, `generar_interfaz_megarachne.py`, `generar_escenas_megarachne.py`, `generar_escala_megarachne.py`) | Euriptérido que se creyó araña (referencia de la maqueta de museo), gris violáceo con marcas rojas y patas arena, en picada (6 u de patas, celda 128, 16 direcciones): andar, tejer, ráfaga de seda con el abdomen alzado, salto colgada de su hilo y trepar, aterrizar, caer del cable, aturdida con estrellas, amenaza, salida del embudo, chillido, muerte panza arriba con el vientre de euriptérido; variante de enfado de todo (rojo encendido y espinas); sombras. Árbol del Nido (Lepidodendron empapado / seco / carbonizado, telaraña con embudo, ojos, sacudida y lluvia, secado, Nido ardiendo, humo, seda que se deshace). Efectos: cables de seda en tramos que se repiten (normal, hilo fino, engorde, ardiendo, carbonizado) con llamas, frente, ceniza, cabos y humo; ráfaga y telarañas del suelo; red que cae y jugador atrapado; saco de crías; crías; marca del salto; polvo; capullo con el contenedor de corazón. Interfaz: barra de minijefe «cable de seda» (240×28, normal / invulnerable / enfado, muesca 50 %, chillido y espinas). Láminas de escala y escenas (GIF en `gif/escenas/`). Sin importar todavía |
| Jefe secreto Lepidodendron ancestral (piso 3) | `docs/multimedia-temp/floor3-bosses/lepidodendron/` (`generar_lepidodendron.py`, `generar_pie_lepidodendron.py`, `generar_paredes_lepidodendron.py`, `generar_conos_lepidodendron.py`, `generar_ataques_lepidodendron.py`, `generar_desplome_lepidodendron.py`, `generar_recompensa_lepidodendron.py`, `generar_barra_lepidodendron.py`, `generar_escala_lepidodendron.py`) | Árbol milenario al final de una sala de ~5 pantallas: tronco gigante que se pierde en una capa de hojas fija en pantalla (vaivén y sacudida, rayos de luz, sombra moteada), montículo del pie con raíces, vetas moradas (aviso y golpe) y decorados (hongos, setas que laten, florecillas, bichos, restos casi enterrados). 3 paredes de raíces trenzadas (daño, carbonizadas, nudos que revientan, retirada o ceniza, cicatriz). Fase final: 3 conos con armadura de ramas que bajan colgados de una rama (golpe, astillas, arde, salta a trozos, cono expuesto, reventón, cono roto, rebrota si sube sin quemar), latigazo de raíces con ola de tierra y surco, corro de púas, lluvia de esporas con marca roja. Desplome (cinemática ~6,6 s: vetas que se apagan, grieta, caída lenta que acelera, el tronco sustituye a la pared norte, tocón con grieta de luz). Recompensa: Espora morada, la sala de dentro del tocón, pedestal, reliquia Raíz ancestral, presentación de reliquias con halo e iconos. Barra de vida (rama con savia, nudos, conos colgando) | Arte listo; sin importar (va con la rama del piso 3) |
| Pulido del piso 3 | `docs/multimedia-temp/floor3-polish/` (`generar_pulido_piso3.py`) | Lodo en el cuerpo (textura, ruido de manchas, anillo de superficie delante y detrás, burbujas, goterones: el cuerpo se hunde y el lodo le sube por encima), capas de puerta para pasar por detrás del arco (`Doorway_Top_Front` y `_Shade`, sacadas de los arcos de los 10 temas), llamas genéricas S/M/L y lenguas sueltas, iconos de las barras de quemado y de nube tóxica (lleno y vacío), agua de charca (onda, salpicadura, gotas, vapor al apagarse) y ráfaga de la fumarola. Sin importar todavía |
| Evolución de los linajes del piso 3 | `docs/multimedia-temp/floor3-lineages/` (`generar_evolucion_piso3.py`) | 8 siluetas en 80×80 con el color de su clase (Parásito, granate nuevo): Eryops, Xenacanthus, Pulmonoscorpius, ciempiés, yesquero, Cordyceps, lamprea y sanguijuela. Sin importar todavía |
| Presentación de los jefes | `docs/multimedia-temp/boss-intros/` (`generar_presentaciones.py`, `generar_presentaciones_piso3.py`) | Retratos de 176×176, poses de presentación (8 cuadros) y nombres rotulados de los 4 jefes de los pisos 1 y 2, con maquetas del rótulo; fuente Jersey 10 en `fuentes/`. Piso 3: retratos 3D de Arthropleura, Meganeura y Megarachne (con sello «MINIJEFE»), retrato del Lepidodendron ancestral (con sello «JEFE SECRETO»), nombres en ámbar del incendio y maquetas; sus poses son sus animaciones de entrada |

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
- **Boca** (Mandíbulas, Aguijón): arma de 24×24 dibujada mirando a la derecha, unida por el borde izquierdo a media altura. Se coloca en el borde del cuerpo (6 px en la célula, 8,5 con Caparazón, 7 con Piel urticante) y gira hacia el cursor. `Idle` (4 cuadros) y `Attack` (3, en cada disparo o mordisco; en Mandíbulas es lo único que se ve del mordisco). El Aguijón se redujo a 2/3 y los proyectiles salen de su punta (`MutationData.muzzleDistance`).
- **Esporas**: no son un arma que gira sino un gorrito de Amanita (24×24, centrado en la célula) sobre la cabeza, con los estados de las Extremidades (`Idle` y `Move` en las tres direcciones). Cada Piel dice a qué altura está su cabeza (`headHeight`: Caparazón y Piel urticante, 2 px más). Al atacar, el cuerpo se adelanta 2,5 px y el gorrito se aplasta y se hincha, por código (`MutationLook`).
- **Extremidades** (Aletas, Patas articuladas): 32×32 centradas, `Idle` y `Move` en las tres direcciones; van al mismo cuadro que el cuerpo.
- **Órgano** (Vejiga de tinta, Glándula de choque): 16×16 dentro de la célula; `Activate` (32×32) al usar la habilidad, junto con la nube o la onda.
- Orden de dibujo, de atrás hacia delante: Extremidades, cuerpo, Órgano, Boca, gorrito (mirando hacia arriba, la Boca va detrás del cuerpo). Las piezas se ocultan al esquivar y al morir; al recibir daño destellan en rojo mientras el cuerpo hace `Hurt`.

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
| Biomasa | Moneda que sueltan los enemigos al morir; vuela hacia el jugador al limpiar la sala | M | Hecho |
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
| Corazón de sala, iconos de teletransporte y corazón morado | Medio corazón del suelo (reposo, sombra, recogida, vuelo a la vaina), corazón verde entero del HUD, iconos del mapa (medio corazón sin recoger, teletransporte, regreso; con variantes resaltada y apagada) y aura, mota y onda del corazón morado. `docs/multimedia-temp/ui-extras/` (README) | M | Aprobado, sin importar |
| Volumen en la pausa | Panel "AUDIO" bajo las estadísticas: barras de música (nota) y efectos (altavoz) con su porcentaje. Piezas de `generar_volumen.py` en `docs/multimedia-temp/menus/Pause/` (`UI_Slider_Track`, `UI_Slider_Fill`, `UI_Slider_Knob`, `UI_Stat_Music`, `UI_Stat_Sfx`); lo monta **Primordia > Audio > Añadir volumen a la pausa** (y también Importar menús) | M | Listo |
| Pantallas de victoria y derrota | | M | Pendiente |
| Iconos de linaje | Depredador, Acorazado, Tóxico | D | Pendiente |
| Portada para la página de itch.io | 630×500 px, captura o arte promocional | M | Pendiente |

## Música

Veintidós pistas en `docs/multimedia-temp/audio/Music/` (once del piso 1 y once del piso 2), todas en re (cada piso, franja o jefe cambia el modo; la tienda, en su relativo fa mayor) y sintetizadas por `generar_audio.py`. La lista completa, con su duración y su ambiente, está en `docs/multimedia-temp/audio/PROMPT.md` y `PROMPT_piso2.md`. Las elige el `MusicDirector`:

| Pista | Cuándo suena | Prioridad | Estado |
|-------|-------------|-----------|--------|
| `Music_MainMenu` | Menú principal (bucle) | D | Listo |
| `Music_Floor1_Explore` + `Music_Floor1_Combat` | Piso 1: suenan a la vez y en sincronía; con las puertas cerradas se funde al combate | D | Listo |
| `Music_Shop` | Tienda de Darwin | D | Listo |
| `Music_Boss_Virus` / `Music_Boss_Amoeba` | Sala de cada jefe, tras el fragmento `Music_Sting_BossIntro` | D | Listo |
| `Music_Boss_Amoeba_Final` | Lluvia de proyectiles de la ameba (30 s, sin bucle; la pone `AmoebaBoss`) | D | Listo |
| `Music_Sting_LineageEvolution` | Evolución al completar un linaje (tapa la música 2 s) | D | Listo |
| `Music_Sting_Victory` / `Music_Sting_Defeat` | Pantallas de victoria y derrota | D | Listo |
| `Music_Floor2_Shallows_*` / `_Reef_*` / `_Abyss_*` | Piso 2, una pareja exploración + combate por franja (Someras re dórico 100 BPM, Arrecife re eólico 108, Abismo re frigio 80); al cambiar de franja (`Room.DepthBand`) se funden en 3 s | D | Listo |
| `Music_Exchange` | Sala de cambio (laboratorio de Darwin), en todos los pisos | D | Listo |
| `Music_Boss_Anomalocaris` → `_Frenzy` | Anomalocaris: acecho y cerco; frenesí tras la escena de los ojos (la escena deja la música en silencio) | D | Listo |
| `Music_Boss_Eurypterid` → `_LastStand` | Euriptérido: fases acorazada y blanda; último aliento de 25 s exactos (sin bucle) que empieza con la primera salva | D | Listo |

La pausa, el mapa y la elección de mutación bajan la música. El jugador elige en la pausa el volumen de la música y el de los efectos (la interfaz va con los efectos); `AudioVolumes` lo guarda en PlayerPrefs (en la web, en el navegador) con un 80 % de fábrica. Debajo suena el ambiente del piso (`Amb_Floor1_Loop`; en el piso 2, `Amb_Floor2_Loop` y `Amb_Floor2_Abyss_Loop` en el Abismo) o el de la sala del jefe (`Amb_BossRoom_Loop`, `Amb_Floor2_BossRoom_Loop`).

## Efectos de sonido

113 efectos del piso 1 y 102 del piso 2 en `docs/multimedia-temp/audio/SFX/`, con la lista completa en `PROMPT.md` y `PROMPT_piso2.md` (qué evento dispara cada uno) y el `muestrario.html` para escucharlos. Cómo llegan al juego:

| Origen | Ejemplos | Dónde se decide |
|--------|----------|-----------------|
| Datos | Disparo, mordisco e impacto de cada ataque; daño, muerte, división, alerta y aviso de cada enemigo; esquiva de las Aletas | Campos de sonido de `AttackData`, `EnemyData` y `MutationData` (los pone el menú) |
| Efectos visuales | Pisotón, Placas, subida de nivel, compra, marcas de estado (veneno, sangrado, cegado, aturdido, paralizado, asustado, sospecha), burbujas que revientan, linaje perdido | `EffectSound` en el prefab del efecto (lo pone el menú) |
| Jugador | Daño, muerte, curación, latido con poca vida, esquiva, barrera, mutación nueva, habilidad lista o aún recargando, linaje completado | `PlayerAudio`, que el `AudioManager` pone al jugador |
| Interfaz | Pasar por un botón, pulsar, elegir tarjeta, Saltar, Jugar; abrir la pausa y el mapa | `ButtonSounds` (en cada botón al cargar la escena) y `MusicDirector` |
| Código | Salas y puertas (losas de roca desde el piso 2), oleadas, fosas, biomasa, jefes y la cueva del Euriptérido, tienda, sala de cambio, mutaciones con efecto propio, beneficios de los linajes, enemigos del piso 2 | Llamadas a `Sfx.Play` con los nombres de `Sounds`; `Sfx.Stop` corta un aviso cuando el juego lo acorta |

Los sonidos que se piden muchos a la vez (doce proyectiles de una ráfaga, cinco enemigos cegados) suenan una sola vez (`minInterval` en la biblioteca), y la biomasa sube de tono al recoger varias seguidas.

## Fuentes

| Recurso | Uso | Prioridad | Estado |
|---------|-----|-----------|--------|
| Fuente pixel | Silkscreen (normal y negrita) en HUD, menús y textos, a tamaño 8 y 16. En `Art/Fonts/` | M | Listo |
| Fuente de los rótulos de jefe | Jersey 10 a 40 px, sin suavizado, con relleno del color del piso. Copiarla de `docs/multimedia-temp/boss-intros/fuentes/` a `Art/Fonts/` con su licencia en `feature/boss-intro` | M | Elegida |

## Registro de licencias

Anotar aquí todo recurso que no sea propio (bancos de sonido, fuentes, etc.) para respetar sus licencias y dar créditos en la página de itch.io.

| Recurso | Autor / fuente | Licencia | Enlace |
|---------|---------------|----------|--------|
| Fuente Silkscreen | Jason Kottke (Google Fonts) | SIL Open Font License 1.1 (texto en `Art/Fonts/Silkscreen-OFL.txt`) | https://fonts.google.com/specimen/Silkscreen |
| Fuente Jersey 10 | The Soft Type Project Authors, Sarah Cadigan-Fried (Google Fonts) | SIL Open Font License 1.1 (texto en `Jersey10-OFL.txt`) | https://fonts.google.com/specimen/Jersey+10 |
