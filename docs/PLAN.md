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
- [ ] `feature/audio`: música y efectos
- [ ] `feature/lineages`: bonus de sinergia de linajes (resolver antes el problema de Tóxico)
- [ ] `feature/balance`: equilibrio de vida, daño y ritmo
- [ ] Página de itch.io pública → etiqueta **`v1.0`**

### Fase 3: después de la v1.0

Ideas del GDD para seguir creciendo: pisos 2 y 3, tercer enemigo y salas especiales, tienda con biomasa (hecha en `feature/shop`) y salas secretas, soporte de mando y opciones de audio, desbloqueos permanentes entre partidas.

## Mejoras a futuro

Problemas conocidos o ideas que no entran ahora, para retomarlos cuando el prototipo jugable esté completo.

- [x] **Enemigos que rodean obstáculos.** Hecho en `feature/enemy-ai`: rejilla de navegación del piso (`NavigationGrid`) con mapa de distancias al jugador, línea de visión para los tiradores (no disparan a ciegas y te buscan) y separación entre enemigos. Ideas para más adelante: tirador que se mueve en círculo, embestida del perseguidor con aviso y flanqueo.
- [ ] **Textos con TextMeshPro y fuente pixel.** La interfaz usa ahora la fuente integrada de Unity (`Text` de uGUI) para no depender de importar los recursos de TextMeshPro. Cuando exista la fuente pixel (ver [MULTIMEDIA.md](MULTIMEDIA.md)), cambiar los textos a `TextMeshProUGUI`. Afecta a `UISetup` y a los prefabs de `Prefabs/UI`.
- [x] **Mapa distinto en cada partida.** Hecho en `feature/random-rooms`: `FloorGenerator` monta el piso al cargar la escena con una semilla nueva (visible en la pausa; `fixedSeed` para repetir un mapa). Ideas para más adelante: salas especiales (tienda, secreta), elegir la forma según el papel de la sala y equilibrar el presupuesto de enemigos.
- [ ] **Salas con formas distintas.** Hecho: plantillas de texto (`RoomTemplate`: `#` pared, `.` suelo, `^ v < >` huecos de puerta), catálogo en `RoomLayouts` (pequeña 22×15, normal 28×16, grande 40×24, con columnas, en cruz y en L), piezas de pared para entrantes y columnas, y salas de distinto tamaño alineadas por sus puertas. Falta que el generador del mapa aleatorio elija las formas al azar y ampliar el catálogo (pasillos largos, huecos, más variantes de L). Al diseñar plantillas nuevas: pared de 3 filas encima de cualquier suelo, columnas de al menos 2×3, y las puertas de arriba y abajo en las mismas columnas (y las de los lados en las mismas filas).
- [ ] **Animaciones especiales del jefe.** El virus gigante ya tiene sus animaciones básicas, pero conviene darle más presencia en dos momentos: una **aparición** al entrar el jugador en su sala (ahora simplemente aparece y espera 1,5 s antes de atacar; por ejemplo, que se forme o caiga del techo mientras dura esa espera, con el ataque bloqueado) y un **cambio a la fase 2** más vistoso que la Phase2_Transition actual (más larga o con efecto en pantalla: temblor de cámara, destello, onda que empuja). Necesita hojas nuevas en `docs/multimedia-temp/enemies/BossVirus/` (p. ej. `BossVirus_Intro.png`). Afecta a `BossAI` (`startDelay`, `phase2TransitionDuration`), `BossAnimator` y `ArtSetup`.
- [ ] **Tinta que se diluye en el agua.** El piso 1 está bajo el agua, así que la nube de la Vejiga de tinta debería disolverse poco a poco mientras dura: que se expanda despacio, pierda opacidad y se deshilache en volutas en lugar de desaparecer de golpe al terminar la animación. Puede resolverse con arte (más cuadros de disolución en `InkCloud.png`) o con código (fundido de transparencia y una escala lenta encima de la animación, como hacía el antiguo `FadeOutEffect`). Si la nube dura más, conviene decidir si la ceguera de los enemigos también se alarga. Afecta a `Effect_InkCloud`, `InkCloudMutation` y `ArtSetup`.
- [ ] **Detalles de las piezas de mutación.** Los proyectiles siguen saliendo a 0,75 unidades del centro (`AimPivot/Mouth`), así que el del Aguijón aparece dentro de la púa: mover el punto de disparo a la punta de cada Boca (y más lejos con Caparazón). La estela de la esquiva (`DodgeTrail`) tiene forma de célula aunque se lleve una Piel: hacer una por Piel o copiar el sprite del cuerpo. Afecta a `MutationLook`, `Weapon`, `DodgeTrail` y `ArtSetup`.
- [ ] **Extras de la tienda.** Volver a tirar los artículos pagando biomasa y descuentos por linaje (p. ej. más barato si ya llevas mutaciones de ese linaje). Darwin, de momento decorativo, podría hablar o reaccionar cuando no llega la biomasa.
