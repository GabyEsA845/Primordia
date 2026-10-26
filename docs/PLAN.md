# Primordia: plan de desarrollo

Plan por módulos y ramas de Primordia, proyecto personal que se publica en itch.io como build web. El diseño del juego está en [el GDD](GDD%20Primordia.md) y el contenido multimedia en [MULTIMEDIA.md](MULTIMEDIA.md).

## Flujo de ramas

- **`main`**: siempre abre en Unity y siempre es jugable. No se trabaja directamente en ella.
- **`feature/<módulo>`**: una rama corta por módulo. Sale de `main` y vuelve a `main` cuando cumple su criterio de "listo".
- **`fix/<error>`**: arreglos puntuales sobre algo ya publicado.
- **Etiquetas de hito**: `v0.1-prototipo`, `v0.2-jugable`, `v0.5-jugable`, `v1.0`. Sirven para volver atrás si algo se rompe y marcan cada versión publicada.

**Regla de escenas:** cada rama prueba su módulo en su propia escena (`Assets/_Primordia/Scenes/Test_<módulo>.unity`) y sus propios prefabs. Las escenas de Unity se mezclan mal entre ramas, así que la escena `Game` solo se toca al integrar en `main`.

## Módulos

| # | Rama | Contenido | Depende de | Listo cuando… |
|---|------|-----------|-----------|---------------|
| 0 | `feature/core` | `GameManager` (estados), `Health`, mapa de controles del Input System, Cinemachine + Pixel Perfect Camera (16 PPU) | — | Escena vacía con la cámara correcta; `Health` recibe daño y muere en una prueba |
| 1 | `feature/player` | `PlayerController`: WASD, apuntado con ratón, esquiva con invulnerabilidad | 0 | El jugador se mueve en 8 direcciones, mira al cursor y esquiva con recarga |
| 2 | `feature/combat` | `Weapon`, `Projectile`, pool de proyectiles, capas de daño | 0, 1 | Clic izquierdo dispara y daña a un objetivo de prueba |
| 3 | `feature/enemies` | `EnemyData` (ScriptableObject) + `EnemyAI` con máquina de estados pequeña | 0, 2 | 2 enemigos que atacan, reciben daño y mueren |
| 4 | `feature/rooms` | `Room` (puertas que se cierran), `Door`, `RoomExit` y piso de prueba | 3 | Entrar → puertas cerradas → limpiar → se abren → siguiente sala |
| 5 | `feature/mutations` | `MutationData` y sus tipos, `PlayerMutations` (4 ranuras), habilidad con recarga, "elige 1 de 3" | 1, 2 | Una mutación de Boca cambia el disparo; otra en la misma ranura la reemplaza |
| 6 | `feature/boss` | Virus gigante con 2 fases, barra de vida y sala del jefe | 3, 4 | Se puede vencer y aparece la salida |
| 7 | `feature/ui-flow` | Menú principal, HUD de corazones, pausa, pantallas de derrota y victoria, reinicio | 0 | Partida completa del menú a victoria/derrota y vuelta a empezar |
| 8 | `feature/build` | Ajustes WebGL para itch.io y menú de build | todos | La build corre en el navegador desde itch.io |

Cada módulo vive en su propia carpeta de `Assets/_Primordia/Scripts/` (`Core`, `Player`, `Combat`, `Mutations`, `Enemies`, `Rooms`, `UI`), así dos ramas casi nunca tocan los mismos archivos.

## Hoja de ruta

### Fase 1: prototipo jugable (terminada)

- [x] `feature/core`
- [x] `feature/player`
- [x] `feature/combat` → etiqueta **`v0.1-prototipo`**
- [x] `feature/enemies` (perseguidor, tirador y división al morir)
- [x] `feature/rooms`
- [x] `feature/ui-flow` → etiqueta **`v0.2-jugable`**
- [x] `feature/mutations` (las 9 mutaciones del GDD y elección 1 de 3)
- [x] `feature/boss` → etiqueta **`v0.5-jugable`** (partida completa con jefe)

### Fase 2: primera versión publicada (v1.0)

- [ ] `feature/build`: build web y página en itch.io (primero como borrador privado)
- [x] `feature/random-rooms`: mapa distinto en cada partida (camino con giros, salas laterales, enemigos por presupuesto) y salas con formas
- [ ] `feature/art`: arte del piso 1 (ver [MULTIMEDIA.md](MULTIMEDIA.md))
- [x] `feature/audio`: música (menú, piso 1 con capas de exploración y combate, tienda, los dos jefes y fragmentos) y 113 efectos sintetizados por código, `AudioManager` que se crea solo, director de música por sala y estado, y volumen de música y efectos en la pausa
- [x] `feature/lineages`: linajes nuevos (animales de 4 mutaciones, una por ranura, con su clase), linaje activo en el HUD, linajes de cada mutación en tarjetas, pausa y tienda, y burbujas por ranura. Los beneficios de cada clase, más adelante
- [x] `feature/floor1-mutations`: Hifas, Camuflaje y Vacuola digestiva (con sus 3 niveles, iconos y piezas), que completan los linajes del piso 1
- [x] `feature/mouth-tweaks`: Aguijón más pequeño (los proyectiles salen de su punta), Esporas como gorrito de Amanita con una bocanada de esporas que flotan como minas, y mordisco con avance desde el nivel II de Mandíbulas
- [x] `feature/lineage-benefits`: beneficios de los linajes (base por clase: Rematar, Pastar biomasa, Toxina débil y Colonizar la sala; y el toque propio de cada uno de los 7 animales del piso 1), cartel al completar un linaje y beneficio en la pausa
- [x] `feature/lineage-evolution`: al completar un linaje, animación de evolución (el juego se congela, la célula se transforma en el animal) y piezas del linaje en lugar de las de sus mutaciones
- [x] `feature/boss-amoeba`: ameba gigante, segundo jefe del piso 1 (sale ella o el virus, al 50 %): sala redonda, embestidas, división en dos con la vida compartida, escena final con lluvia de proyectiles de 30 s y agotamiento. Escena de cinemática (franjas, HUD oculto, cámara) reutilizable para otros jefes. Vida de los enemigos y del virus del piso 1 al doble
- [x] `feature/floor-transition`: paso al piso siguiente (diseño del 23 de octubre en el GDD). Mutaciones y linajes por piso (cada piso ofrece solo las suyas), burbujas del jefe sin Bocas y con una mejora segura, sala de paso junto a la del jefe que se abre al vencerlo, ventana obligatoria de elección de Boca del piso siguiente, reparto al azar de los niveles extra de la Boca anterior, metamorfosis según la familia y sin Bocas en las ofertas del piso siguiente. Hasta que exista el piso 2, el paso lleva a la victoria. Orden: (0) sprites de los cuerpos medusa y pez primitivo, (1) sala de paso en cueva con fosa y transición de paleta, (2) el paso (E en el borde, salto), (3) elección de Boca y reparto de niveles, (4) metamorfosis
- [ ] `feature/exchange-room`: sala de cambio (laboratorio de Darwin) pegada a la tienda desde el piso 2: cápsula con E, interfaz propia con la criatura y sus ranuras, ofertas de la misma ranura y el mismo nivel, un cambio gratis por piso, y metamorfosis si cambia la familia de la Boca
- [x] `feature/floor2-design`: diseño del piso 2 cerrado el 24 de octubre y pasado al GDD (mutaciones, linajes con la clase Carroñero, enemigos, jefes y salas). Todo su arte está hecho en `docs/multimedia-temp/` (ver [MULTIMEDIA.md](MULTIMEDIA.md)) y el documento interactivo del piso 2 lo resume con sprites animados
- [x] `feature/floor2-rooms-design`: diseño de las salas del piso 2 (fosas, catálogo de 10 salas especiales, números, pasillos, tienda y salas de los jefes) y de la presentación de los jefes, pasado al GDD. Los planos de las salas especiales y su arte (fosas de las tres franjas, almejas, coral del corazón morado, entradas de oleadas, fósil, sombras de nado) están en `docs/multimedia-temp/floor2-rooms/`, y el arte de las presentaciones en `docs/multimedia-temp/boss-intros/`
- [x] `feature/boss-intro`: presentación de los jefes al entrar en su sala (unos 3 s, siempre, se salta con E): jugador quieto e invulnerable, cámara al jefe, pose, rótulo con nombre en Jersey 10 del color del piso, título y retrato de 176 px, y vuelta de la cámara. Para el virus y la ameba gigante ya, y preparada para el Anomalocaris y el Euriptérido
- [x] `feature/floor-2-structure`: que la fosa lleve de verdad al piso 2 conservando al jugador, sus mutaciones y su biomasa. Por pasos que se prueban uno a uno:
  1. Generador de cuevas en C# (el método de la sala de paso con comprobación y catálogo de reserva), `FloorConfig` del piso 2 (10–12 salas de combate, presupuesto 4 + 1,4 por sala, recompensas en las salas 2, 4, 6, 8 y 10, 2–3 laterales), las tres franjas por tercios (Someras, Arrecife, Abismo) con su decoración, obstáculos de cobertura, luces 2D del Abismo y nieve marina, pasillos irregulares, camino que tiende a bajar y puertas de losas (`Door` con animación de abrir propia y duración por cuadro)
  2. Salas de varias casillas en el generador y salas especiales: importar los 10 planos con sus símbolos nuevos, sorteo de 2 a 4 (nunca en las salas 1–2 ni antes del jefe), el corazón morado siempre en el Abismo, tienda a mitad del piso con la sala de cambio detrás, y antesala de los jefes. Antes, los planos de la tienda, la sala de cambio, las dos salas de jefe y la antesala, con vista previa
  3. Fosas: tiles por vecinos, caída del jugador (−½ corazón y vuelta al último suelo firme), esquiva que las cruza, enemigos que nadan (sombra separada) y que andan (las rodean), y enemigos empujados que caen
  4. Piezas: almejas con biomasa, entradas de oleadas, coral del corazón morado y el corazón que para un golpe y se conserva entre pisos (icono morado en el HUD)

  Enemigos provisionales hasta `feature/floor2-enemies`
- [x] `feature/clam-room`: sala de la almeja garantizada en cada piso, aparte del sorteo de especiales (una al azar entre Gruta escondida, Anillo de la sima y Jardín de esponjas, las tres con almeja grande). Después, las 3 mutaciones especiales que solo salen en almejas grandes (diseño cerrado en el GDD, "Mutaciones especiales de las almejas"): Flagelos de Leanchoilia, Cutícula de Marrella y Yema clonal, el comodín de linaje con cartel para elegir, el precio Inestable en el daño y el HUD, y su arte (iconos, 6 capas y efectos)

  Pasos:
  1. Sala de la almeja y lo común de las especiales: precio Inestable (daño extra por golpe, corazones dorados y fila en la pausa con cuadro de información), comodín de linaje con cartel para elegir y cambio desde la pausa, gema dorada en los iconos, almejas que no repiten especial y atajo F5 (hecho)
  2. Flagelos de Leanchoilia: latigazo junto al enemigo con el daño de 1 s de la Boca y sus efectos, recarga, destello de listo y cadena en el nivel III (hecho; con la Corona de brazos, cuando exista esa Boca)
  3. Cutícula de Marrella: esquiva justa que devuelve los proyectiles a quien los lanzó y aturde a quien te golpea, Prisma en el nivel III (hecho; la capa del cuerpo, con los cuerpos del piso 2)
  4. Yema clonal: copia translúcida que ataca sola con la Boca y una parte del daño, señuelo para los enemigos, recarga desde que desaparece y Gemación en el nivel III (hecho)
- [x] `feature/floor2-mouths`: Cnidocitos, Placas mandibulares (Quebrar y Guillotina) y Corona de brazos (remolino y Marea), con los 3 cuerpos del jugador (medusa, pez acorazado, lirio de mar) y el golpe sincronizado con el cuadro de la animación de ataque

  Pasos:
  1. Cnidocitos: arpón que atraviesa, veneno, Ramillete, proyectil y destello de su arte (menú Primordia > Mutations > Crear Bocas del piso 2) y atajo F6 para cambiar de Boca (hecho)
  2. Placas mandibulares: mordisco en arco de 110° con el alcance de lo dibujado, empujón, Quebrar con su marca y Guillotina (segunda dentellada más grande), con el arte del mordisco rehecho (hecho)
  3. Golpe sincronizado con el cuadro de la animación de ataque de cada cuerpo, y la capa de la Cutícula de Marrella sobre los cuerpos (hecho)
  4. Corona de brazos y lirio de mar (remolino, Marea, metamorfosis del lirio) y los Flagelos de Leanchoilia con el remolino (hecho)
- [ ] `feature/floor2-enemies`: Trilobite, Medusa azul, Ottoia, Esponja Vauxia y Cardumen de Pikaia, con su arte, sus avisos y su comportamiento

  Pasos:
  1. Base (cerebros de enemigo, EnemyBrain) y Trilobite: ronda, aviso, embestida, aturdido al chocar y cabeza blindada (menú Primordia > Enemies > Crear enemigos del piso 2) (hecho)
  2. Medusa azul: flota a impulsos y suelta un anillo de 8 con un hueco que gira (hecho)
  3. Ottoia: enterrada e inmune, aviso con círculo, sale, muerde y se hunde
  4. Esponja Vauxia: pulso que cura y escuda, una como mucho por sala
  5. Cardumen de Pikaia: 5 peces en formación que rodean y hacen pasadas por turnos
  6. Cierre: costes y sala de inicio en Floor_2 y fuera los enemigos provisionales
- [ ] `feature/floor2-mutations`: las otras 6 mutaciones del piso 2 y sus 8 linajes (beneficios remodelados, Carroñero y su sprite de restos, toques y evoluciones). Incluye el arte que falta: las capas de Extremidades y Piel sobre los 3 cuerpos (12), los efectos de activación de los Órganos, las larvas, las burbujas de la Cola, las púas de Hallucigenia y las escamas de Wiwaxia
- [ ] `feature/boss-anomalocaris`: cueva ovalada muy grande sin fosas, en la que baja la luz al entrar, con plantas luminosas (luces 2D) y columnas de algas; acecho, cerco, escena de los ojos al 25 % y frenesí con los marcadores del zarpazo
- [ ] `feature/boss-eurypterid`: cueva de unos 30×22 con 5–6 rocas y grietas en los bordes, pinzas y púas del caparazón, escena de la muda al 50 % (la muda vacía queda como cobertura y se derrumban 2 rocas) y fase blanda con la cola
- [ ] `feature/floor2-audio`: música (exploración, combate y una pista por jefe) y sonidos del piso 2, con el sistema del piso 1
- [ ] `feature/floor-3`: Pantano carbonífero (lo mismo, con el artrópodo gigante); si se retrasa mucho, la v1.0 sale con 2 pisos
- [ ] `feature/balance`: equilibrio de vida, daño y ritmo. La vida de los enemigos del piso 1 ya se dobló en `feature/boss-amoeba` (6, 8 y 2; virus 120), que también hace notar más Rematar
- [ ] Página de itch.io pública → etiqueta **`v1.0`**

### Fase 3: después de la v1.0

Pisos 4 a 6 (desierto pérmico, selva de dinosaurios y Edad de hielo), uno por actualización. Otras ideas del GDD: tercer enemigo y salas especiales en el piso 1, salas secretas, soporte de mando y opciones de audio, desbloqueos permanentes entre partidas.

## Mejoras a futuro

Problemas conocidos o ideas que no entran ahora, para retomarlos cuando el prototipo jugable esté completo.

- [x] **Enemigos que rodean obstáculos.** Hecho en `feature/enemy-ai`: rejilla de navegación del piso (`NavigationGrid`) con mapa de distancias al jugador, línea de visión para los tiradores (no disparan a ciegas y te buscan) y separación entre enemigos. Ideas para más adelante: tirador que se mueve en círculo, embestida del perseguidor con aviso y flanqueo.
- [ ] **Textos con TextMeshPro y fuente pixel.** La interfaz usa ahora la fuente integrada de Unity (`Text` de uGUI) para no depender de importar los recursos de TextMeshPro. Cuando exista la fuente pixel (ver [MULTIMEDIA.md](MULTIMEDIA.md)), cambiar los textos a `TextMeshProUGUI`. Afecta a `UISetup` y a los prefabs de `Prefabs/UI`.
- [x] **Mapa distinto en cada partida.** Hecho en `feature/random-rooms`: `FloorGenerator` monta el piso al cargar la escena con una semilla nueva (visible en la pausa; `fixedSeed` para repetir un mapa). Ideas para más adelante: salas especiales (tienda, secreta), elegir la forma según el papel de la sala y equilibrar el presupuesto de enemigos.
- [ ] **Salas con formas distintas.** Hecho: plantillas de texto (`RoomTemplate`: `#` pared, `.` suelo, `^ v < >` huecos de puerta), catálogo en `RoomLayouts` (pequeña 22×15, normal 28×16, grande 40×24, con columnas, en cruz y en L), piezas de pared para entrantes y columnas, y salas de distinto tamaño alineadas por sus puertas. Falta que el generador del mapa aleatorio elija las formas al azar y ampliar el catálogo (pasillos largos, huecos, más variantes de L). Al diseñar plantillas nuevas: pared de 3 filas encima de cualquier suelo, columnas de al menos 2×3, y las puertas de arriba y abajo en las mismas columnas (y las de los lados en las mismas filas).
- [ ] **Animaciones especiales del jefe.** El virus gigante ya tiene sus animaciones básicas, pero conviene darle más presencia en dos momentos: una **aparición** al entrar el jugador en su sala (ahora simplemente aparece y espera 1,5 s antes de atacar; por ejemplo, que se forme o caiga del techo mientras dura esa espera, con el ataque bloqueado) y un **cambio a la fase 2** más vistoso que la Phase2_Transition actual (más larga o con efecto en pantalla: temblor de cámara, destello, onda que empuja). Necesita hojas nuevas en `docs/multimedia-temp/enemies/BossVirus/` (p. ej. `BossVirus_Intro.png`). Afecta a `BossAI` (`startDelay`, `phase2TransitionDuration`), `BossAnimator` y `ArtSetup`.
- [ ] **Tinta que se diluye en el agua.** El piso 1 está bajo el agua, así que la nube de la Vejiga de tinta debería disolverse poco a poco mientras dura: que se expanda despacio, pierda opacidad y se deshilache en volutas en lugar de desaparecer de golpe al terminar la animación. Puede resolverse con arte (más cuadros de disolución en `InkCloud.png`) o con código (fundido de transparencia y una escala lenta encima de la animación, como hacía el antiguo `FadeOutEffect`). Si la nube dura más, conviene decidir si la ceguera de los enemigos también se alarga. Afecta a `Effect_InkCloud`, `InkCloudMutation` y `ArtSetup`.
- [ ] **Detalles de las piezas de mutación.** ~~Punto de disparo en la punta de la Boca~~: hecho en `feature/mouth-tweaks` (`MutationData.muzzleDistance`, el Aguijón dispara desde su punta, también con Caparazón). La estela de la esquiva (`DodgeTrail`) tiene forma de célula aunque se lleve una Piel: hacer una por Piel o copiar el sprite del cuerpo. Afecta a `MutationLook`, `Weapon`, `DodgeTrail` y `ArtSetup`.
- [ ] **Extras de la tienda.** Volver a tirar los artículos pagando biomasa y descuentos por linaje (p. ej. más barato si ya llevas mutaciones de ese linaje). Darwin, de momento decorativo, podría hablar o reaccionar cuando no llega la biomasa.
