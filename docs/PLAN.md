# Primordia: plan de desarrollo

Plan por módulos y ramas para la versión de la GameJam 2026. El diseño del juego está en [el GDD](GDD%20Roguelike%20de%20evolución%20(GameJam%202026).md) y el contenido multimedia en [MULTIMEDIA.md](MULTIMEDIA.md).

## Flujo de ramas

- **`main`**: siempre abre en Unity y siempre es jugable. No se trabaja directamente en ella.
- **`feature/<módulo>`**: una rama corta por módulo. Sale de `main` y vuelve a `main` cuando cumple su criterio de "listo".
- **`fix/<error>`**: arreglos puntuales (las únicas ramas permitidas en las últimas 4 h de la jam).
- **Etiquetas de hito**: `v0.1-prototipo`, `v0.5-jugable`, `v1.0-jam`. Sirven para volver atrás si algo se rompe.

**Regla de escenas:** cada rama prueba su módulo en su propia escena (`Assets/_Primordia/Scenes/Test_<módulo>.unity`) y sus propios prefabs. Las escenas de Unity se mezclan mal entre ramas, así que la escena `Game` solo se toca al integrar en `main`.

## Módulos

| # | Rama | Contenido | Depende de | Listo cuando… |
|---|------|-----------|-----------|---------------|
| 0 | `feature/core` | `GameManager` (estados), `Health`, mapa de controles del Input System, Cinemachine + Pixel Perfect Camera (16 PPU) | — | Escena vacía con la cámara correcta; `Health` recibe daño y muere en una prueba |
| 1 | `feature/player` | `PlayerController`: WASD, apuntado con ratón, esquiva con invulnerabilidad | 0 | El jugador se mueve en 8 direcciones, mira al cursor y esquiva con recarga |
| 2 | `feature/combat` | `Weapon`, `Projectile`, pool de proyectiles, capas de daño | 0, 1 | Clic izquierdo dispara y daña a un objetivo de prueba |
| 3 | `feature/enemies` | `EnemyData` (ScriptableObject) + `EnemyAI` con máquina de estados pequeña | 0, 2 | 2 enemigos que atacan, reciben daño y mueren |
| 4 | `feature/rooms` | `Room` (puertas que se cierran), `FloorManager`, salas con Tilemap, transición entre salas | 3 | Entrar → puertas cerradas → limpiar → se abren → siguiente sala |
| 5 | `feature/mutations` | `MutationData`, `MutationSlots` (4 ranuras), efectos sobre jugador y arma, interfaz "elige 1 de 3" | 1, 2 | Una mutación de Boca cambia el disparo; otra en la misma ranura la reemplaza |
| 6 | `feature/boss` | Virus gigante con disparo en espiral y sala del jefe | 3, 4 | Se puede vencer y aparece la elección de mutación |
| 7 | `feature/ui-flow` | Menú principal, HUD de corazones, pausa, pantallas de derrota y victoria, reinicio | 0 | Partida completa del menú a victoria/derrota y vuelta a empezar |
| 8 | `feature/build` | Ajustes de build para la plataforma de la jam (probablemente WebGL) | todos | La build corre fuera del editor |

Cada módulo vive en su propia carpeta de `Assets/_Primordia/Scripts/` (`Core`, `Player`, `Combat`, `Mutations`, `Enemies`, `Rooms`, `UI`), así dos ramas casi nunca tocan los mismos archivos.

## Calendario

### Esta semana: preparación

Solo sistemas genéricos, sin ambientación final, por si la jam no permite llevar contenido hecho (revisar las reglas).

- [x] `feature/core`
- [x] `feature/player`
- [x] `feature/combat` → etiqueta **`v0.1-prototipo`**
- [x] `feature/enemies` (perseguidor, tirador y división al morir)
- [x] `feature/rooms`
- [x] `feature/mutations` (las 9 mutaciones del GDD y elección 1 de 3)
- [ ] `feature/build` (build de prueba temprana)

### Durante la jam (48 h)

1. **Primeras 2 h:** leer el tema, ajustar la ambientación y cerrar el mínimo jugable.
2. **Resto del día 1:** `feature/enemies-piso1`, `feature/boss`, `feature/mutations-contenido` (las 6 mutaciones) y `feature/ui-flow` → etiqueta **`v0.5-jugable`** (partida completa, aunque sea fea).
3. **Mañana del día 2:** `feature/art`, `feature/audio`, `feature/balance`. Si el mínimo está listo: `feature/random-rooms`, `feature/lineages`, `feature/mutation-visuals`.
4. **Últimas 4 h:** congelar funciones; solo ramas `fix/*`. Build final → etiqueta **`v1.0-jam`** y subirla con margen.

## Mejoras a futuro

Problemas conocidos o ideas que no entran ahora, para retomarlos cuando el prototipo jugable esté completo.

- [ ] **Enemigos que rodean obstáculos.** Ahora van en línea recta hacia el jugador y se quedan atascados contra paredes y obstáculos. Opciones: pathfinding en rejilla sobre el Tilemap de la sala (A* sencillo) o steering con raycasts para esquivar. Afecta a `EnemyAI`.
- [ ] **Textos con TextMeshPro y fuente pixel.** La interfaz usa ahora la fuente integrada de Unity (`Text` de uGUI) para no depender de importar los recursos de TextMeshPro. Cuando exista la fuente pixel (ver [MULTIMEDIA.md](MULTIMEDIA.md)), cambiar los textos a `TextMeshProUGUI`. Afecta a `UISetup` y a los prefabs de `Prefabs/UI`.
- [ ] **Mapa distinto en cada partida.** Ahora el piso es siempre el mismo (5 salas y el jefe en el mismo orden). Generar el recorrido al empezar cada partida: número y orden de salas al azar, salas de recompensa repartidas y el jefe siempre al final. Es el deseable `feature/random-rooms`. Afecta a `FloorSetup` (que pasaría a ser un generador en tiempo de juego) y a `Room`.
- [ ] **Salas con formas distintas.** Ahora todas las salas son rectángulos de 26×18. Añadir formas (en L, en cruz, con columnas o huecos, pasillos largos, salas grandes) como plantillas de Tilemap que el generador elige al azar. Conviene hacerlo junto con el mapa aleatorio y el arte de las salas.
