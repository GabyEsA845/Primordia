# Primordia: documento de diseño

Oct 11, 2026 · @Gaby

## Resumen

**Concepto:** un roguelike de disparos con vista cenital donde una criatura primitiva cruza salas, se come lo que derrota y muta para sobrevivir, hasta evolucionar de la sopa primordial a algo mucho más complejo.

- **Título:** *Primordia*.
- **Género:** roguelike de acción con salas ("room-based twin-stick").
- **Referencias:** Enter the Gungeon (estructura de salas, jefe por piso, esquivar y disparar) y Everything is Crab (evolución por mutaciones, ramas que se excluyen).
- **Diferencia clave:** en lugar de armas, el jugador gana poder con mutaciones de su cuerpo. Las mutaciones cambian cómo ataca, cómo se mueve y cómo resiste.
- **Estilo visual:** pixel art muy parecido al de Enter the Gungeon.
- **Motor:** Unity + C#, 2D con vista cenital.
- **Equipo:** Gaby en solitario; arte y música con ayuda de Claude más adelante.
- **Duración de una partida:** objetivo de 10 a 15 minutos por piso.
- **Publicación:** proyecto personal, publicado en itch.io como juego web.

## Pilares de diseño

La fantasía del jugador es "soy algo diminuto que se vuelve el depredador de la cima". Cada decisión de diseño se revisa contra estos tres pilares:

1. **Eres lo que comes.** Cada mutación se ve en el cuerpo del jugador. Si consigues espinas, se ven las espinas.
2. **Evolucionar es elegir.** Tomar una rama cierra otra. No hay una partida perfecta; hay partidas distintas.
3. **Acción legible y rápida.** Disparar, esquivar y leer al enemigo de un vistazo, como en Enter the Gungeon. Si algo no se entiende en un segundo, se simplifica.

## Bucle de juego

&#91;embedded content: bucle de un piso · 6 pasos, 1 decisión\]

El bucle corto es la sala (entrar, sobrevivir, recoger biomasa) y el bucle largo es el piso, que termina con el jefe y la elección de una mutación mayor. Si el jugador muere, la partida vuelve a empezar desde el piso 1 como célula.

## Sistema de mutaciones

Propuesta: el cuerpo tiene **4 ranuras** y cada ranura solo admite una mutación. Así las exclusiones salen solas: elegir mandíbulas ocupa la boca y cierra el aguijón venenoso, sin reglas extra que programar.

**Ranuras del cuerpo**

| Ranura | Qué controla | Ejemplo de efecto |
| --- | --- | --- |
| Boca | Ataque principal (clic izquierdo) | Tipo de proyectil, cadencia, daño |
| Extremidades | Movimiento y esquiva | Velocidad, tipo de esquiva |
| Piel | Defensa | Vida máxima, reducción de daño, espinas |
| Órgano especial | Habilidad con recarga (clic derecho) | Escudo, explosión, invocación |

**Linajes:** cada mutación pertenece a un linaje (por ejemplo *Depredador*, *Acorazado*, *Tóxico*). Tener 3 mutaciones del mismo linaje activa un bonus de sinergia. Esto empuja al jugador a comprometerse con una rama, igual que en Everything is Crab.

**Mutaciones de ejemplo (borrador)**

| Mutación | Ranura | Linaje | Efecto |
| --- | --- | --- | --- |
| Mandíbulas | Boca | Depredador | Disparo corto y potente, cura un poco al matar |
| Aguijón | Boca | Tóxico | Proyectil que envenena (daño en el tiempo) |
| Esporas | Boca | Tóxico | Disparo en abanico de 3 proyectiles lentos |
| Aletas | Extremidades | Depredador | +30% velocidad, esquiva más larga |
| Patas articuladas | Extremidades | Acorazado | Esquiva que empuja a los enemigos |
| Caparazón | Piel | Acorazado | +2 de vida máxima, −10% velocidad |
| Piel urticante | Piel | Tóxico | Daña a quien te toca |
| Vejiga de tinta | Órgano especial | Depredador | Nube que ciega a los enemigos 3 s |
| Glándula de choque | Órgano especial | Acorazado | Onda que repele proyectiles |

**Cómo se consiguen:** al limpiar una sala cae "biomasa" (moneda) y, de vez en cuando, una mutación menor. Al derrotar al jefe se elige 1 de 3 mutaciones mayores. Cambiar una mutación por otra en la misma ranura la reemplaza para siempre en esa partida.

**Valores de las mutaciones (versión actual)**

Referencia: jugador base con velocidad 6, esquiva de 3 unidades, 3 corazones y disparo de 1 de daño (5 por segundo).

| Mutación | Valores |
| --- | --- |
| Mandíbulas | Mordisco de unas 4 unidades de alcance, 3 de daño, 2,5 por segundo. Cada 3 enemigos matados cura medio corazón |
| Aguijón | Automático, 4 por segundo, 1 de daño + veneno de 1 por segundo durante 3 s |
| Esporas | 3 proyectiles lentos en abanico de 30°, 1 de daño cada uno, 2 disparos por segundo |
| Aletas | +30 % de velocidad, esquiva un 50 % más larga |
| Patas articuladas | Al esquivar contra un enemigo: empujón, aturdimiento de 0,5 s y 1 de daño |
| Caparazón | +1 corazón de vida máxima (y lo cura), −10 % de velocidad |
| Piel urticante | 2 de daño a quien te toca, como máximo cada 0,5 s por enemigo |
| Vejiga de tinta | Nube de 3 unidades de radio, ciega 3 s; recarga de 8 s |
| Glándula de choque | Onda de 4 unidades de radio que destruye proyectiles enemigos y empuja; recarga de 6 s |

**Jefe del piso 1 (virus gigante):** 60 de vida, 3×3 unidades, avanza despacio hacia el centro, medio corazón por contacto. Fase 1: espiral de un brazo y ráfaga circular de 12 proyectiles cada 4 s. Fase 2 (desde el 50 %): espiral de dos brazos más rápida, ráfaga de 16 e invoca 2 bacterias cada 8 s (máximo 4 vivas).

## Pisos y etapas evolutivas

Primera versión (v1.0): **1 piso muy pulido**, la sopa primordial. Los pisos 2 y 3 quedan diseñados aquí para las versiones siguientes; cada piso es una etapa de la vida en la Tierra.

| Piso | Etapa | Ambientación | Enemigos | Jefe |
| --- | --- | --- | --- | --- |
| 1 | Sopa primordial | Charco microscópico, colores verdes y burbujas | Bacterias que persiguen, amebas que se dividen al morir | Virus gigante que dispara en espiral |
| 2 | Océano cámbrico | Fondo marino, corales y luz azul | Trilobites que embisten, medusas que flotan y disparan | Anomalocaris (depredador con garras) |
| 3 | Primeros pasos en tierra | Pantano con helechos gigantes | Insectos gigantes, anfibios que escupen | Artrópodo gigante (estilo Arthropleura) |

**Extras a futuro:** un piso 4 con dinosaurios o mamíferos, y que los enemigos de cada piso también tengan variantes "mutadas".

## Controles y combate

Controles de teclado y ratón, como Enter the Gungeon. Mando opcional más adelante.

| Acción | Teclado y ratón | Notas |
| --- | --- | --- |
| Moverse | WASD | 8 direcciones |
| Apuntar | Ratón | La criatura mira hacia el cursor |
| Ataque principal | Clic izquierdo | Lo define la mutación de Boca |
| Esquivar | Espacio | Invulnerable durante la esquiva, recarga corta |
| Habilidad especial | Clic derecho | Lo define el Órgano especial |
| Pausa | Esc o P | P sirve también en el navegador, donde Esc sale de la pantalla completa |

**Reglas de combate:**

- Vida en corazones (empieza con 3), como Gungeon: cada golpe quita medio o uno entero.
- Al entrar en una sala de combate, las puertas se cierran hasta matar a todos los enemigos.
- Proyectiles enemigos lentos y de colores fuertes para que se esquiven leyendo la pantalla.

## Alcance de la primera versión (v1.0)

La v1.0 es el piso 1 completo y pulido, publicado en itch.io. El orden de trabajo, por módulos y ramas, está en [PLAN.md](PLAN.md).

**Mínimo jugable (hecho en la v0.5)**

- [x] Movimiento, apuntado, disparo y esquiva del jugador
- [x] Vida, daño, muerte y pantalla de reinicio
- [x] 2 tipos de enemigo con IA simple (perseguir, disparar)
- [x] Salas conectadas con puertas que se cierran durante el combate
- [x] 1 piso completo con 5 a 7 salas y 1 jefe
- [x] 9 mutaciones repartidas en las 4 ranuras, con elección de 1 entre 3 al limpiar algunas salas
- [x] Menú principal y pantallas de victoria y derrota
- [ ] Build web en itch.io

**Para la v1.0**

- [x] Mapa distinto en cada partida y salas con formas distintas
- [ ] Arte del piso 1 y cambio visual del jugador por cada mutación (hecho: jugador, enemigos, jefe, proyectiles, efectos y escenario; falta el aspecto de cada mutación, que aún son marcas provisionales)
- [ ] Música y efectos de sonido
- [ ] Linajes con bonus de sinergia
- [ ] Más variedad en el piso 1: un tercer enemigo y salas especiales

**Más adelante**

- [ ] Pisos 2 y 3
- [ ] Tienda con biomasa y salas secretas
- [ ] Soporte de mando y opciones de audio
- [ ] Desbloqueos permanentes entre partidas

## Arquitectura técnica en Unity

Objetivo: que añadir una mutación, un enemigo o una sala nueva sea crear un archivo de datos en el editor, no escribir código nuevo.

| Pieza | Responsabilidad | Cómo se implementa |
| --- | --- | --- |
| `PlayerController` | Movimiento, apuntado, esquiva | Input System nuevo, `Rigidbody2D` |
| `Health` | Vida, daño, muerte | Componente reutilizable para jugador y enemigos |
| `MutationData` | Datos de una mutación: ranura, linaje, icono, efectos | `ScriptableObject` |
| `PlayerMutations` | Qué mutación hay en cada ranura y aplicar sus efectos | Componente en el jugador |
| `Weapon` / `Projectile` | Disparo (o mordisco cuerpo a cuerpo) según la mutación de Boca | `AttackData` (`ScriptableObject`) y pool de objetos para los proyectiles |
| `EnemyData` + `EnemyAI` | Estadísticas y comportamiento: perseguir, mantener la distancia y disparar con línea de visión | `ScriptableObject` + máquina de estados pequeña que rodea obstáculos con la rejilla del piso (`NavigationGrid`) |
| `Room` / `FloorGenerator` | Puertas, salas, orden del piso, enemigos y jefe | Formas de sala como texto (`RoomTemplate`); al empezar cada partida, el generador crea un plano al azar con su semilla, lo pinta en un Tilemap (`RoomTheme`) y reparte enemigos por presupuesto, todo según un `FloorConfig` |
| `GameManager` | Estados: menú, jugando, elección de mutación, victoria, derrota | Singleton simple |
| Animaciones | Jugador, enemigos, jefe, proyectiles, efectos y escenario | Menús `Primordia > Art` que importan las hojas de `docs/multimedia-temp` y crean clips, Animator y tiles |

**Herramientas:** Unity 6 (6000.6.2f1) con plantilla 2D (URP), Tilemap para salas, Cinemachine para la cámara (sigue al jugador, adelantada hacia el cursor), interfaz con uGUI (TextMeshPro cuando haya fuente pixel) y Git con un `.gitignore` de Unity y Git LFS para el arte.

**Pixel art:** sprites de 16 px por unidad (personajes de 16×16 a 32×32), filtro *Point* sin compresión y la *Pixel Perfect Camera* de URP para que los píxeles no se deformen.

## Preguntas abiertas y decisiones

**Pendiente**

- [ ] Diseño de la forma de la criatura en cada piso (antes de hacer el arte).
- [ ] Cuántas mutaciones tiene cada linaje y qué bonus da cada uno.

**Decidido el 11 de octubre**

| Tema | Decisión |
| --- | --- |
| Título | Primordia |
| Equipo | Gaby en solitario; arte y música con Claude más adelante |
| Estilo visual | Pixel art muy parecido a Enter the Gungeon |
| Tipo de proyecto | Proyecto personal: la jam no permitía llevar código hecho. Se publica en itch.io como build web |
| Exclusiones | Por ranuras del cuerpo |
| Linajes | Depredador, Acorazado y Tóxico |
| Alcance de la v1.0 | 1 piso muy pulido; los demás, en versiones siguientes |
| Obtención de mutaciones | Con un solo piso, la mutación tras el jefe no se usaría. Al limpiar la 2.ª y la 4.ª sala de combate del camino, y cada sala lateral, aparece "elige 1 de 3": 3 mutaciones al azar, sin las equipadas, con botón Saltar |
| Mutaciones de la v1.0 | Las 9 del borrador, con los valores de la tabla "Valores de las mutaciones" |
| Sinergia de linajes | Cada mutación guarda su linaje; el bonus queda como deseable. Ojo: Tóxico no puede reunir 3 (dos de sus mutaciones son de Boca) |
| Aspecto de las mutaciones | Cambio muy visible por capas, al estilo de *Everything is Crab*: la Piel sustituye al cuerpo entero y lo agranda (también su colisión); la Boca es un arma grande en el borde del cuerpo que gira hacia el cursor y anima cada ataque; las Extremidades se mueven con el cuerpo; el Órgano se ve dentro de la célula y se activa con la habilidad. Las piezas se ocultan al esquivar y al morir |
| Mapa | Camino con giros del inicio al jefe, con 4 o 5 salas de combate y 1 o 2 salas laterales sin salida; enemigos por presupuesto que sube a lo largo del camino. Semilla visible en la pausa |
| Salas | Formas en texto: pequeña 22×15, normal 28×16 (cabe en pantalla), grande 40×24, con columnas, en cruz y en L |
| Mandíbulas | Mordisco cuerpo a cuerpo en arco delante de la boca, no un disparo |
| Recarga del Órgano especial | Barra bajo el jugador que se llena durante la recarga y desaparece al estar lista |
| Jefe del piso 1 | Virus gigante con 2 fases (la segunda desde el 50 % de vida); vencerlo abre la salida y da la victoria |

La evolución entre pisos será **fija**: célula en el piso 1, criatura marina en el 2 y anfibio en el 3.
