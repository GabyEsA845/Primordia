# Primordia: documento de diseño (GameJam 2026)

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
- **Duración de una partida:** objetivo de 10 a 15 minutos para la versión de la jam.

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

**Valores de las mutaciones (versión de la jam)**

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

Para la jam: **1 piso muy pulido**, la sopa primordial. Los pisos 2 y 3 quedan diseñados aquí para continuar después de la jam; cada piso es una etapa de la vida en la Tierra.

| Piso | Etapa | Ambientación | Enemigos | Jefe |
| --- | --- | --- | --- | --- |
| 1 | Sopa primordial | Charco microscópico, colores verdes y burbujas | Bacterias que persiguen, amebas que se dividen al morir | Virus gigante que dispara en espiral |
| 2 | Océano cámbrico | Fondo marino, corales y luz azul | Trilobites que embisten, medusas que flotan y disparan | Anomalocaris (depredador con garras) |
| 3 | Primeros pasos en tierra | Pantano con helechos gigantes | Insectos gigantes, anfibios que escupen | Artrópodo gigante (estilo Arthropleura) |

**Extras si sobra tiempo:** un piso 4 con dinosaurios o mamíferos, y que los enemigos de cada piso también tengan variantes "mutadas".

## Controles y combate

Controles de teclado y ratón, como Enter the Gungeon. Mando opcional si sobra tiempo.

| Acción | Teclado y ratón | Notas |
| --- | --- | --- |
| Moverse | WASD | 8 direcciones |
| Apuntar | Ratón | La criatura mira hacia el cursor |
| Ataque principal | Clic izquierdo | Lo define la mutación de Boca |
| Esquivar | Espacio | Invulnerable durante la esquiva, recarga corta |
| Habilidad especial | Clic derecho | Lo define el Órgano especial |
| Pausa | Esc |  |

**Reglas de combate:**

- Vida en corazones (empieza con 3), como Gungeon: cada golpe quita medio o uno entero.
- Al entrar en una sala de combate, las puertas se cierran hasta matar a todos los enemigos.
- Proyectiles enemigos lentos y de colores fuertes para que se esquiven leyendo la pantalla.

## Alcance para 2 días

La regla: al final del día 1 tiene que existir una partida jugable de principio a fin, aunque sea fea. Todo lo demás se añade encima.

**Mínimo jugable (obligatorio)**

- [ ] Movimiento, apuntado, disparo y esquiva del jugador
- [ ] Vida, daño, muerte y pantalla de reinicio
- [ ] 2 tipos de enemigo con IA simple (perseguir, disparar)
- [ ] Salas conectadas con puertas que se cierran durante el combate
- [ ] 1 piso completo con 5 a 7 salas y 1 jefe
- [ ] 6 mutaciones repartidas en las 4 ranuras, con elección de 1 entre 3 tras el jefe
- [ ] Menú principal, pantalla de victoria y build para la plataforma de la jam

**Deseable (si el mínimo está listo a mitad del día 2)**

- [ ] Más variedad en el piso 1: un tercer enemigo y salas especiales
- [ ] Generación aleatoria del orden de salas
- [ ] Linajes con bonus de sinergia
- [ ] Cambio visual del jugador por cada mutación
- [ ] Música y efectos de sonido

**Solo si sobra tiempo**

- [ ] Tienda con biomasa y salas secretas
- [ ] Soporte de mando y opciones de audio
- [ ] Desbloqueos permanentes entre partidas

## Plan de trabajo

Antes de nada: muchas game jams anuncian un **tema** al empezar y no permiten llevar el juego hecho. Hay que revisar las reglas de esta jam; si es así, esta semana se prepara la base técnica y genérica, y la ambientación se adapta al tema el día 1.

**Esta semana (preparación)**

- [ ] Leer las reglas de la jam: tema, qué se puede llevar hecho, plataforma de entrega
- [ ] Crear el proyecto de Unity (2D URP) y un repositorio en GitHub
- [ ] Prototipo del jugador: mover, apuntar, disparar, esquivar
- [ ] Un enemigo de prueba y el sistema de vida y daño
- [ ] Sistema de salas con puertas que se cierran
- [ ] Estructura de mutaciones con ScriptableObjects (sin contenido final)
- [ ] Hacer una build de prueba para confirmar que se exporta bien

**Durante la jam (48 h)**

1. **Primeras 2 h:** leer el tema, ajustar la ambientación y cerrar la lista del mínimo jugable.
2. **Resto del día 1:** enemigos, jefe del piso 1, mutaciones y flujo completo de inicio a fin.
3. **Mañana del día 2:** arte, sonido y equilibrio. Si el mínimo está listo, añadir los extras deseables al piso 1.
4. **Últimas 4 h:** congelar funciones, solo arreglar errores, hacer la build y subirla con margen.

## Arquitectura técnica en Unity

Objetivo: que añadir una mutación, un enemigo o una sala nueva sea crear un archivo de datos en el editor, no escribir código nuevo.

| Pieza | Responsabilidad | Cómo se implementa |
| --- | --- | --- |
| `PlayerController` | Movimiento, apuntado, esquiva | Input System nuevo, `Rigidbody2D` |
| `Health` | Vida, daño, muerte | Componente reutilizable para jugador y enemigos |
| `MutationData` | Datos de una mutación: ranura, linaje, icono, efectos | `ScriptableObject` |
| `MutationSlots` | Qué mutación hay en cada ranura y aplicar sus efectos | Componente en el jugador |
| `Weapon` / `Projectile` | Disparo según la mutación de Boca | Pool de objetos para los proyectiles |
| `EnemyData` + `EnemyAI` | Estadísticas y comportamiento simple | `ScriptableObject` + máquina de estados pequeña |
| `Room` / `FloorManager` | Puertas, oleadas, orden de salas, jefe | Salas como prefabs con Tilemap |
| `GameManager` | Estados: menú, jugando, elección de mutación, victoria, derrota | Singleton simple |

**Herramientas:** Unity 6 o 2022 LTS con plantilla 2D (URP), Tilemap para salas, Cinemachine para la cámara, TextMeshPro para la interfaz y Git con un `.gitignore` de Unity.

**Pixel art:** sprites de 16 px por unidad (personajes de 16×16 a 32×32), filtro *Point* sin compresión y la *Pixel Perfect Camera* de URP para que los píxeles no se deformen.

## Preguntas abiertas y decisiones

**Pendiente**

- [ ] Reglas de la jam (tema, qué se puede llevar hecho): se anunciarán en los próximos días.
- [ ] Diseño de la forma de la criatura en cada piso (después de la jam).
- [ ] Cuántas mutaciones tiene cada linaje y qué bonus da cada uno.

**Decidido el 11 de octubre**

| Tema | Decisión |
| --- | --- |
| Título | Primordia |
| Equipo | Gaby en solitario; arte y música con Claude más adelante |
| Estilo visual | Pixel art muy parecido a Enter the Gungeon |
| Exclusiones | Por ranuras del cuerpo; revisar si el proyecto continúa tras la jam |
| Linajes | Depredador, Acorazado y Tóxico |
| Alcance de la jam | 1 piso muy pulido; los demás, después |
| Obtención de mutaciones | Con un solo piso, la mutación tras el jefe no se usaría. Al limpiar las salas 2 y 4 aparece "elige 1 de 3": 3 mutaciones al azar, sin las equipadas, con botón Saltar |
| Mutaciones de la jam | Las 9 del borrador, con los valores de la tabla "Valores de las mutaciones" |
| Sinergia de linajes | Cada mutación guarda su linaje; el bonus queda como deseable. Ojo: Tóxico no puede reunir 3 (dos de sus mutaciones son de Boca) |
| Aspecto de las mutaciones | Marcas de color provisionales en el cuerpo por cada ranura, hasta tener arte |
| Recarga del Órgano especial | Barra bajo el jugador que se llena durante la recarga y desaparece al estar lista |
| Jefe del piso 1 | Virus gigante con 2 fases (la segunda desde el 50 % de vida); vencerlo abre la salida y da la victoria |

La evolución entre pisos será **fija**: célula en el piso 1, criatura marina en el 2 y anfibio en el 3.
