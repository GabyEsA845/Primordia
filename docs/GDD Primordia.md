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
- **Duración de una partida:** objetivo de 10 a 15 minutos por piso, en **6 pisos** (de la sopa primordial a la Edad de hielo).
- **Publicación:** proyecto personal, publicado en itch.io como juego web.

## Pilares de diseño

La fantasía del jugador es "soy algo diminuto que se vuelve el depredador de la cima". Cada decisión de diseño se revisa contra estos tres pilares:

1. **Eres lo que comes.** Cada mutación se ve en el cuerpo del jugador. Si consigues espinas, se ven las espinas. Y en cada piso la criatura entera evoluciona según el arma que lleva.
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

**Linajes**

Un linaje es un animal (Tiburón, Medusa, Tortuga...) que se completa con **4 mutaciones, una por ranura**: el cuerpo entero tiene que coincidir. Al completarlo se activa su beneficio, que se pierde si cambias una de esas 4 mutaciones. Esto empuja al jugador a comprometerse con una rama, igual que en Everything is Crab.

- **Una mutación puede pertenecer a varios linajes.** Las Mandíbulas sirven para el Tiburón, el Calamar y la Tortuga; lo que decide el linaje es la combinación de las 4.
- **Cada linaje pertenece a una clase**, y la clase decide la base de su beneficio. Además, cada animal suma un **toque propio** pequeño que encaja con sus mutaciones. El beneficio es fijo (no crece con los niveles de las mutaciones) y dura mientras lleves las 4. Como cada piso tiene sus propias mutaciones y la Boca nueva es obligatoria al pasar de piso, los linajes de un piso se rompen al llegar al siguiente: en cada piso se empieza de cero con sus propios animales.

| Clase | Estilo | Beneficio base |
| --- | --- | --- |
| Carnívoro | Ataque y caza | **Rematar:** +100 % de daño a los enemigos heridos, los que tienen la mitad de su vida o menos (+50 % contra el jefe) |
| Herbívoro | Aguante | **Pastar biomasa:** cada 10 de biomasa recogida cura medio corazón. Con la vida llena, da medio corazón verde temporal que absorbe un golpe (como mucho uno a la vez) |
| Venenoso | Daño en el tiempo y estados | **Toxina débil:** los enemigos envenenados reciben +50 % de daño de cualquier fuente |
| Hongo | Área y propagación | **Colonizar la sala:** al entrar en una sala de combate brotan 3 matas de hongo al azar en el suelo (radio 1,2) que ralentizan un 40 % y quitan 1 por segundo, como el micelio; desaparecen al limpiar la sala |

- **Clases entre pisos (decidido el 24 de octubre):** las clases son un catálogo que atraviesa toda la partida, y los animales (linajes) son propios de cada piso. En cada piso hay 4 clases activas: las que no encajan con el ambiente duermen y vuelven más tarde (Hongo duerme en el océano y vuelve en el pantano carbonífero), las que siguen se remodelan (su beneficio base se ajusta al piso) y cada piso trae al menos una clase nueva. La tabla de arriba es la del piso 1; la del piso 2 está en "Linajes del piso 2".

- **Linajes del piso 1:** 7 linajes con las 12 mutaciones del piso. Cada mutación está en 2 o 3 linajes. El diagrama interactivo de linajes muestra qué mutaciones activan cada uno.

| Linaje | Clase | Boca | Extremidades | Piel | Órgano especial |
| --- | --- | --- | --- | --- | --- |
| Tiburón | Carnívoro | Mandíbulas | Aletas | Caparazón (piel de dentículos) | Glándula de choque (electrorrecepción) |
| Calamar | Carnívoro | Mandíbulas (pico) | Aletas | Camuflaje | Vejiga de tinta |
| Tortuga | Herbívoro | Mandíbulas (pico) | Patas articuladas | Caparazón | Vacuola digestiva |
| Medusa | Venenoso | Aguijón | Aletas | Piel urticante | Glándula de choque |
| Escorpión | Venenoso | Aguijón | Patas articuladas | Camuflaje | Vacuola digestiva (digiere fuera del cuerpo) |
| Moho | Hongo | Esporas | Hifas | Piel urticante | Vacuola digestiva (descomponedor) |
| Cuesco de lobo | Hongo | Esporas | Hifas | Camuflaje | Vejiga de tinta (nube de esporas) |

**Evolución al completar un linaje**

Al completar un linaje el jugador evoluciona: el juego se congela unos 2 s, la cámara se acerca un poco, la célula brilla, se transforma en la silueta del animal (del color de su clase), sale su nombre y vuelve a ser la célula con las piezas del linaje. Si se completa con un menú abierto (la elección de mutación), la animación espera a volver al juego. Mientras el linaje está activo, sus piezas sustituyen a las de algunas de sus mutaciones (solo el aspecto: la mecánica es la misma; la Piel nunca cambia y los iconos del HUD y de la pausa tampoco). Al perderlo, un pof del color de la clase y vuelven las piezas de las mutaciones.

| Linaje | Pieza que cambia | Queda así |
| --- | --- | --- |
| Tiburón | Extremidades (Aletas) | Aleta dorsal y aletas pectorales |
| Calamar | Extremidades (Aletas) | Tentáculos que ondulan detrás |
| Tortuga | Boca (Mandíbulas) y Extremidades (Patas) | Pico córneo que muerde y patas escamosas |
| Medusa | Extremidades (Aletas) | Tentáculos translúcidos colgando |
| Escorpión | Boca (Aguijón) y Extremidades (Patas) | Cola curvada que dispara desde su punta, y patas con pinzas |
| Moho | Extremidades (Hifas) | Pelusa de moho alrededor del cuerpo |
| Cuesco de lobo | Boca (gorrito de Amanita) | Gorrito de cuesco, una bola blanca con motas |

**Toque propio de cada linaje del piso 1**

| Linaje | Toque | Efecto |
| --- | --- | --- |
| Tiburón | Sangre en el agua | Rematar a un enemigo (matarlo cuando está herido) recarga la esquiva al instante |
| Calamar | Presa a ciegas | Los enemigos cegados cuentan como heridos para Rematar |
| Tortuga | Retraerse | Al usar la Vacuola digestiva eres invulnerable 1,5 s (te escondes en el caparazón) |
| Medusa | Estela urticante | Al esquivar dejas un rastro de tentáculos que envenena a quien lo pisa |
| Escorpión | Picadura oculta | El ataque desde el camuflaje aplica el triple de veneno |
| Moho | Colonia grande | Brotan 5 matas en vez de 3, y un 30 % más grandes |
| Cuesco de lobo | Bocanada al golpe | Al recibir un golpe sueltas una nube cegadora a tu alrededor (como mucho cada 6 s) |

**Linajes del piso 2 (decidido el 24 de octubre)**

Carnívoro, Herbívoro y Venenoso se remodelan, Hongo duerme y entra el **Carroñero**, clase nueva (color marrón arena). Son 8 animales del Cámbrico, dos por clase, hechos con las 9 mutaciones del piso. No hay dos linajes con las mismas 4 mutaciones, cada clase tiene un animal de Boca cuerpo a cuerpo y otro de proyectil o de área, y cada toque hace combo con alguna mutación del propio animal.

| Clase | Beneficio base en el piso 2 |
| --- | --- |
| Carnívoro | **Rematar:** +100 % de daño a los heridos (la mitad de su vida o menos; +50 % contra el jefe). Nuevo: los aturdidos y los que tienen Quebrar también cuentan como heridos |
| Herbívoro | **Pastar biomasa:** cada 12 de biomasa recogida cura medio corazón. Nuevo: con la vida llena puedes guardar hasta 2 medios corazones verdes |
| Venenoso | **Toxina débil:** los envenenados reciben +50 % de daño. Nuevo: además se mueven un 20 % más lentos |
| Carroñero (nueva) | **Carroña:** cada enemigo que muere (también los jefes) deja sus restos durante 4 s, que parpadean el último segundo; al pasar por encima ganas +20 % de daño durante 5 s, acumulable hasta 3 veces (cada vez renueva los 5 s), con una marca de colmillos sobre el jugador. Premia moverse con agresividad entre los enemigos que vas matando |

| Linaje | Clase | Boca | Extremidades | Piel | Órgano especial |
| --- | --- | --- | --- | --- | --- |
| Placodermo | Carnívoro | Placas mandibulares | Cola propulsora | Placas de trilobite (coraza) | Ojos compuestos |
| Opabinia | Carnívoro | Placas mandibulares (garra de la trompa) | Espinas de Hallucigenia | Escleritos de Wiwaxia | Ojos compuestos (sus cinco ojos) |
| Burgessomedusa | Venenoso | Cnidocitos | Cola propulsora | Escleritos de Wiwaxia | Saco de crías (pólipos) |
| Anémona | Venenoso | Cnidocitos | Espinas de Hallucigenia | Placas de trilobite | Saco de crías |
| Lirio de mar | Herbívoro | Corona de brazos | Cola propulsora | Placas de trilobite (cáliz) | Saco de crías |
| Wiwaxia | Herbívoro | Placas mandibulares (rádula que raspa) | Cola propulsora | Escleritos de Wiwaxia | Saco de crías |
| Trilobite | Carroñero | Placas mandibulares | Espinas de Hallucigenia (patas) | Placas de trilobite | Ojos compuestos (ojos de cristal de calcita) |
| Hallucigenia | Carroñero | Corona de brazos | Espinas de Hallucigenia | Escleritos de Wiwaxia | Ojos compuestos |

| Linaje | Toque | Efecto |
| --- | --- | --- |
| Placodermo | Festín | Rematar con un mordisco cura medio corazón (como mucho uno cada 6 s); también cuenta la segunda dentellada de la Guillotina |
| Opabinia | Cinco ojos | Los Ojos compuestos duran 1 s más, y cada remate mientras duran los alarga 0,5 s |
| Burgessomedusa | Pólipos urticantes | Las larvas envenenan al morder |
| Anémona | Lecho urticante | Quien pisa tus púas queda envenenado |
| Lirio de mar | Corriente nutritiva | El remolino atrae también la biomasa del suelo |
| Wiwaxia | Cosecha | Cada tanda de escleritos que alcanza a 3 o más enemigos distintos da medio corazón verde (hasta 2, como el Pastar; se pierden con el linaje) |
| Trilobite | Coraza de carroña | Al tocar unos restos, el siguiente proyectil que te alcance se deshace contra tus placas |
| Hallucigenia | Doble ración | Las púas del suelo que matan a un enemigo dejan restos dobles, dorados (cuentan como dos); las que dispara el Erizo no |

- **Evolución:** como en el piso 1, al completar un linaje el juego se congela unos 2 s y el cuerpo brilla y se transforma en la silueta del animal, del color de su clase. Parte del cuerpo que da la Boca del linaje (pez acorazado, medusa o lirio de mar), y las siluetas son de 64×64 (48×48 en el piso 1). Por ahora los linajes del piso 2 no cambian piezas del cuerpo.
- **Pisos siguientes:** cada piso tiene sus propios linajes, hechos con sus mutaciones (más adelante Lobo, Raptor, Mamut...).
- **Interfaz:** las tarjetas de elección y la tienda dicen a qué linajes pertenece cada mutación; en la pausa, el cuadro de cada ranura dice cuántas de las 4 llevas de cada linaje ("Tiburón 3/4"). El linaje activo sale en el HUD, bajo la biomasa, con el color de su clase, y al completarlo hay un destello dorado y un cartel breve con su beneficio ("¡Tiburón! Rematar + Sangre en el agua") durante 2 s. En la pausa, el linaje activo es la última fila del panel de estadísticas (animal, clase y nombres de su beneficio); al pasar el ratón, el cuadro de información explica el beneficio completo. El cuadro de cada ranura dice solo lo que hace ahora (la descripción y la mejora de su nivel, sin las anteriores) y sus linajes en una línea ("Tiburón 4/4 · Calamar 3/4"). Como guía, en la elección de 1 de 3 brilla suavemente la tarjeta que acerca un linaje que ya has empezado más allá del que llevas más avanzado (sin linaje completo no hay brillo que invite a romperlo). Las burbujas y la franja de las tarjetas van ahora del color de la **ranura** (Boca naranja, Extremidades morado, Piel azul, Órgano lima), porque una mutación puede estar en varios linajes.

**Mutaciones del piso 1**

| Mutación | Ranura | Linajes | Efecto |
| --- | --- | --- | --- |
| Mandíbulas | Boca | Tiburón, Calamar, Tortuga | Mordisco corto y potente, cura un poco al matar |
| Aguijón | Boca | Medusa, Escorpión | Proyectil que envenena (daño en el tiempo) |
| Esporas | Boca | Moho, Cuesco de lobo | Bocanada de esporas lentas que se quedan flotando como minas |
| Aletas | Extremidades | Tiburón, Calamar, Medusa | +30% velocidad, esquiva más larga |
| Patas articuladas | Extremidades | Tortuga, Escorpión | Esquiva que empuja a los enemigos |
| Hifas (nueva) | Extremidades | Moho, Cuesco de lobo | Al moverte dejas un rastro de micelio que ralentiza y daña |
| Caparazón | Piel | Tiburón, Tortuga | +2 de vida máxima, −10% velocidad |
| Piel urticante | Piel | Medusa, Moho | Daña a quien te toca |
| Camuflaje (nueva) | Piel | Calamar, Escorpión, Cuesco de lobo | Si pasas 3 s sin atacar ni recibir golpes, te vuelves casi invisible: los enemigos te buscan y tu siguiente ataque hace más daño |
| Vejiga de tinta | Órgano especial | Calamar, Cuesco de lobo | Nube que ciega a los enemigos 3 s |
| Glándula de choque | Órgano especial | Tiburón, Medusa | Onda que repele proyectiles |
| Vacuola digestiva (nueva) | Órgano especial | Tortuga, Escorpión, Moho | Digiere y cura medio corazón; se recarga matando enemigos |

**Cómo se consiguen:** al limpiar ciertas salas aparece la elección de 1 de 3 mutaciones, y en la tienda se compran con biomasa. Al derrotar al jefe caen burbujas de mutación de las otras ranuras (nunca Bocas), y una de ellas es la mejora de una mutación que ya llevas. La Boca del piso siguiente se elige en la sala de paso (ver "Paso al piso siguiente"). Cambiar una mutación por otra en la misma ranura la reemplaza para siempre en esa partida. Desde el piso 2 solo salen las mutaciones de ese piso.

**Valores de las mutaciones (versión actual)**

Referencia: jugador base con velocidad 6, esquiva de 3 unidades, 3 corazones y disparo de 1 de daño (5 por segundo).

| Mutación | Valores |
| --- | --- |
| Mandíbulas | Mordisco de unas 4 unidades de alcance, 3 de daño, 2,5 por segundo. Cada 3 enemigos matados cura medio corazón |
| Aguijón | Automático, 4 por segundo, 1 de daño + veneno de 1 por segundo durante 3 s |
| Esporas | Bocanada de 5 esporas en abanico de 75° (con algo de variación al azar), 1,25 por segundo. Frenan en 0,6 s (unas 2,8 unidades) y siguen flotando 1,5 s más mientras se dispersan (1,5 unidades por segundo, con un leve vaivén, otras 2,2 unidades) hasta desvanecerse; 1 de daño cada una al tocar. Al soltarlas, el cuerpo se adelanta un poco y el gorrito se aplasta y se hincha |
| Aletas | +30 % de velocidad, esquiva un 50 % más larga |
| Patas articuladas | Al esquivar contra un enemigo: empujón, aturdimiento de 0,5 s y 1 de daño |
| Caparazón | +1 corazón de vida máxima (y lo cura), −10 % de velocidad |
| Piel urticante | 2 de daño a quien te toca, como máximo cada 0,5 s por enemigo |
| Vejiga de tinta | Nube de 3 unidades de radio, ciega 3 s; recarga de 8 s |
| Glándula de choque | Onda de 4 unidades de radio que destruye proyectiles enemigos y empuja; recarga de 6 s |
| Hifas | Al moverte, una mancha de micelio cada 0,6 unidades (radio 0,5) que dura 3 s; los enemigos encima van un 40 % más lentos y reciben 1 de daño por segundo (el primero tras un segundo; varias manchas no suman) |
| Camuflaje | Tras 3 s sin atacar, sin usar la habilidad y sin recibir golpes: casi transparente (30 %) y el siguiente ataque hace el doble de daño. Los enemigos no disparan y te buscan por puntos al azar alrededor de donde te vieron por última vez; si uno pasa 1,5 s a menos de 2,5 unidades, sospecha ("?" sobre su cabeza) y te descubre. Atacar, usar la habilidad, recibir un golpe o ser descubierto te deja a la vista |
| Vacuola digestiva | Clic derecho: cura medio corazón. Se recarga matando enemigos (cualquier muerte cuenta): cada 4. Con la vida llena no se gasta. La barra de recarga se llena con cada muerte |


**Niveles y evoluciones**

Cada mutación tiene **3 niveles**. Se sube eligiéndola otra vez en la elección de 1 de 3 (o en las burbujas del jefe); más adelante también se podrá comprar con biomasa en la tienda. Si llevas alguna mutación que aún puede subir, al menos una de las 3 cartas es una mejora. Al reemplazar una mutación por otra de la misma ranura, el nivel se pierde y la nueva empieza en el I. El nivel se ve con una insignia (puntos, gemas doradas en el III) sobre el icono en el HUD, las tarjetas y la pausa.

| Mutación | Nivel II (valores) | Nivel III: evolución |
| --- | --- | --- |
| Mandíbulas | Daño 3 → 4, alcance +15 % y avance: si nadie está a tiro, te lanzas 0,6 unidades hacia el cursor (0,1 s, sin invulnerabilidad, se frena contra paredes y cuerpos) y muerdes al llegar o en cuanto alguien entra en el alcance | **Desgarro:** el mordisco hace sangrar (1 por segundo durante 3 s) y cura medio corazón cada 2 muertes |
| Aguijón | Veneno de 1 → 2 por segundo | **Contagio:** un enemigo envenenado que muere estalla y envenena a los cercanos (radio 2) |
| Esporas | 5 → 7 esporas, que flotan 2,5 s | **Esporas buscadoras:** las que flotan se acercan despacio (1,5 unidades por segundo) al enemigo más cercano en un radio de 3; si no hay ninguno, se dispersan como las demás |
| Aletas | Velocidad +45 % y recarga de la esquiva −25 % | **Emboscada:** tras esquivar, +50 % de daño durante 1,5 s (con aura visible) |
| Patas articuladas | Aturdimiento 0,5 → 1 s y daño 1 → 2 | **Pisotón:** al acabar cada esquiva, una onda de radio 2 aturde a los que haya alrededor |
| Caparazón | +2 corazones en total y velocidad −10 % → −5 % | **Placas:** anula el primer golpe que recibes en cada sala (se recarga al entrar en la siguiente) |
| Piel urticante | Daño al contacto 2 → 3, y además envenena | **Erizo:** al recibir un golpe, disparas 8 púas en círculo (1 de daño más veneno) |
| Vejiga de tinta | Radio 3 → 4 y recarga 8 → 6 s | **Tinta corrosiva:** los enemigos dentro de la nube reciben 1 de daño por segundo |
| Glándula de choque | Radio 4 → 6 y recarga 6 → 4,5 s | **Paralizante:** los enemigos que alcanza la onda quedan paralizados 2 s |
| Hifas | El micelio dura 4,5 s y quita 2 por segundo | **Red micelial:** los enemigos que mueren sobre el micelio sueltan una nube de esporas que envenena a los cercanos (radio 2, 1 por segundo durante 3 s) |
| Camuflaje | Te ocultas a los 2 s y el ataque desde el camuflaje hace el triple | **Pánico:** matar a un enemigo desde el camuflaje asusta a los demás de la sala 2 s: no atacan y se alejan despacio (gotas de sudor sobre la cabeza) |
| Vacuola digestiva | Se recarga cada 3 muertes | **Digestión completa:** cura un corazón entero (sigue cada 3 muertes) |

La nube de tinta ahora permanece los 3 s que dura la ceguera (aparece, se mantiene y se disipa), es más transparente y ciega también a quien entra en ella. Los estados de los enemigos se ven con marcas encima de su cabeza: veneno, sangrado, cegado, aturdido y paralizado (este, además, con rayos por el cuerpo).

**Mutaciones de cada piso (rediseño del 23 de octubre)**

Como en *Enter the Gungeon*, cada piso es más duro que el anterior y trae mutaciones más fuertes que empujan a dejar atrás las viejas:

- **Cada piso tiene sus propias mutaciones y sus propios linajes.** Desde el piso 2, las elecciones de 1 de 3, la tienda y las burbujas del jefe solo ofrecen mutaciones de ese piso (y las mejoras de las que lleves). Las mutaciones de pisos anteriores se conservan con su nivel mientras no las cambies. Los linajes nunca se actualizan: los del piso 1 usan mutaciones del piso 1 y los del piso 2, las suyas.
- **Cuántas:** 8 o 9 por piso por ahora (2 por ranura y 2 o 3 Bocas, con sus linajes); el piso 2 tiene 9, con sus 3 Bocas. La idea es llegar a 12 (3 por ranura) cuando el arte lo permita. Con 3 Bocas por piso hay una por familia de arma.
- **Escalones de poder:** el nivel I de una mutación del piso N+1 vale como el nivel II de una del piso N (del III del piso N al III del piso N+1 se gana un escalón).
- **Dificultad:** la vida de los enemigos sube ×1,5–2 por piso. El resto de la dificultad viene de más enemigos por sala y patrones más exigentes, no de esponjas de vida.
- **Linajes entre pisos:** los linajes de un piso no siguen activos en el siguiente, porque la Boca nueva es obligatoria y todos los linajes usan una Boca de su piso. Sustituye a la regla anterior de que un linaje servía un piso por encima del suyo (decidido el 24 de octubre).
- **Mejoras de las mutaciones de pisos anteriores** (resuelto el 24 de octubre): las elecciones, la tienda y las burbujas del jefe ofrecen mejoras de cualquier mutación que lleves, y con la regla de escalones una mutación del piso anterior al III sigue siendo útil, pero compensa cambiarla.

Borrador de ideas para los pisos 3 a 6 (una por ranura; faltan las demás hasta llegar a 8 por piso). El piso 2 ya está diseñado: ver "Mutaciones del piso 2". Cada Boca pertenece a una de las 3 **familias de arma**, que deciden la forma del jugador en ese piso (ver "Forma del jugador en cada piso").

| Piso | Mutación | Ranura | Familia | Efecto (idea) |
| --- | --- | --- | --- | --- |
| 3 | Escupitajo ácido | Boca | Área | Bola lenta que deja un charco de ácido (daño por segundo) |
| 3 | Patas de salto | Extremidades | | La esquiva es un salto que pasa por encima de obstáculos y daña al caer |
| 3 | Piel venenosa | Piel | | Quien te toca queda envenenado y tus esquivas dejan un rastro venenoso |
| 3 | Croar | Órgano especial | | Onda que aturde 1 s a los enemigos cercanos |
| 4 | Colmillos | Boca | Cuerpo a cuerpo | Embestida corta con mordisco que atraviesa a varios enemigos en línea |
| 4 | Patas de carrera | Extremidades | | La velocidad sube mientras te mueves sin parar (hasta +40 %) |
| 4 | Escamas córneas | Piel | | +1 corazón y más tiempo de invulnerabilidad tras cada golpe |
| 4 | Saco de veneno | Órgano especial | | Nube venenosa a tu alrededor durante 4 s |
| 5 | Escupitajo venenoso | Boca | Proyectil | Proyectil que envenena y ciega un instante |
| 5 | Garras de raptor | Extremidades | | La esquiva salta hacia el cursor y golpea al aterrizar |
| 5 | Placas óseas | Piel | | Los golpes de un corazón entero solo quitan medio; −15 % de velocidad |
| 5 | Cresta resonante | Órgano especial | | Bramido en cono que empuja y aturde |
| 6 | Trompa | Boca | Área | Soplido en cono que daña y empuja |
| 6 | Pezuñas | Extremidades | | La esquiva es una embestida que atraviesa a los enemigos y los daña |
| 6 | Pelaje | Piel | | +1 corazón e inmunidad a ralentizar y congelar (suelo helado del piso 6) |
| 6 | Instinto de manada | Órgano especial | | Invoca 2 lobos aliados durante 10 s |

Sus linajes y sus niveles II y III se diseñan al hacer cada piso.

**Mutaciones del piso 2 (decidido el 24 de octubre)**

Nueve mutaciones: 3 Bocas, una por familia de arma, y 2 por cada otra ranura. El nivel I vale como el nivel II de una mutación del piso 1. Todos los animales en que se inspiran son del Cámbrico.

| Mutación | Ranura | Familia | Nivel I | Nivel II | Nivel III: evolución |
| --- | --- | --- | --- | --- | --- |
| Cnidocitos | Boca | Proyectil → medusa | Arpón que atraviesa a 1 enemigo: 2 de daño, 3 por segundo, veneno de 1 por segundo durante 3 s | Atraviesa a 2 enemigos y el veneno sube a 2 por segundo | **Ramillete:** cada cuarto disparo son 3 arpones en abanico |
| Placas mandibulares | Boca | Cuerpo a cuerpo → pez acorazado | Arco de 110° desde la boca, tan largo como el mordisco dibujado (unas 1,9 unidades), 5 de daño, 2 mordiscos por segundo y un pequeño empujón. **Quebrar:** el enemigo mordido recibe un 25 % más de daño de todo durante 2 s, con una marca encima (una placa que se agrieta) | Daño 5 → 6 y Quebrar dura 3 s | **Guillotina:** cada cuarto mordisco va seguido de una segunda dentellada más grande (su propia animación, con el filo al rojo) que llega más lejos (unas 3,25 unidades), con el daño normal |
| Corona de brazos | Boca | Área → lirio de mar | El clic lanza un remolino donde está el cursor (como mucho a 6 u): dura 2 s, radio 1,6 u, atrae a los enemigos al centro y hace 1 de daño cada 0,4 s. Un remolino por segundo | Radio 2 u, dura 2,5 s y daña cada 0,3 s | **Marea:** al acabar, el remolino estalla, empuja a todos hacia fuera y hace 4 de daño |
| Cola propulsora | Extremidades | | Dos esquivas seguidas antes de la recarga | La segunda esquiva deja 3 burbujas que hacen 2 de daño y empujan | **Torbellino:** las dos esquivas dejan 5 burbujas de 3 de daño |
| Espinas de Hallucigenia | Extremidades | | Al esquivar dejas 2 púas en el suelo durante 4 s; quien las pisa recibe 2 de daño | 3 púas que duran 6 s | **Erizo:** al desaparecer, cada púa sale disparada hacia el enemigo más cercano |
| Placas de trilobite | Piel | | +1 corazón; 1 de cada 4 proyectiles que te alcanzan se deshace contra tus placas sin hacerte daño | Se deshace 1 de cada 3 | **Enrollarse:** te enrollas al esquivar y destruyes los proyectiles contra los que esquivas (devolverlos es cosa de la Cutícula de Marrella) |
| Escleritos de Wiwaxia | Piel | | Cada 9 s sueltas 3 escamas hacia los enemigos cercanos (2 de daño cada una) | Cada 7 s, 5 escamas | **Órbita:** antes de salir disparadas, las escamas giran 2 s a tu alrededor y destruyen los proyectiles enemigos que tocan. Es la única forma del piso de protegerse sin esquivar |
| Saco de crías | Órgano especial | | Clic derecho: 2 larvas aliadas que persiguen y muerden durante 8 s; recarga de 14 s | 3 larvas (8 s) | **Enjambre:** la larva que mata a un enemigo se divide en dos (también 8 s) |
| Ojos compuestos | Órgano especial | | Clic derecho: durante 3 s todo va al 60 % de velocidad salvo tú; recarga de 12 s | Dura 4 s | **Visión total:** mientras dura, tus ataques hacen un 30 % más de daño |

- **Placas mandibulares** se inspira en *Dunkleosteus*, un pez acorazado sin dientes que cortaba con placas de hueso afiladas. Frente a Mandíbulas, muerde menos veces pero más fuerte y en un arco más ancho. Sus placas óseas se abren y se cierran de golpe al morder.
- **Corona de brazos:** los lirios de mar se alimentan agitando los brazos para crear corrientes. Es la Boca de área del piso y agrupa a los enemigos (las Esporas dejan minas y el Escupitajo ácido del piso 3 dejará charcos). Combina con las Espinas de Hallucigenia y los Ojos compuestos.
- **Espinas de Hallucigenia** convierte la esquiva en una forma de poner trampas (Hifas deja un rastro al moverse). **Escleritos de Wiwaxia** devuelve los golpes a distancia (la Piel urticante solo daña al contacto). **Ojos compuestos:** los ojos complejos aparecieron en el Cámbrico.
- **Órganos:** no tienen pieza en el cuerpo, pero sí efectos de activación vistosos (por ejemplo, un destello que tiñe la pantalla durante los Ojos compuestos, o el saco que se abre y suelta las larvas).

**Mutaciones especiales de las almejas (decidido el 25 de octubre)**

Tres mutaciones más, una de Extremidades, una de Piel y una de Órgano (ninguna Boca), que **solo salen de las almejas grandes**: la almeja grande guarda una especial al azar que no lleves, además de su biomasa; la pequeña, solo biomasa. Cada piso tiene al menos una almeja grande (ver "Salas especiales": la sala de la almeja). Son bastante más fuertes que las demás, tienen niveles I–III como cualquier mutación y, una vez equipadas, sus mejoras salen en las elecciones y la tienda; la especial en sí nunca se vende.

- **Comodín de linaje:** una especial cuenta como la mutación de su ranura de cualquier linaje del que lleves las otras 3. Solo hay dos casos en que completa dos a la vez (Extremidad especial con Placas mandibulares, Placas de trilobite y Ojos compuestos: Placodermo o Trilobite; Piel especial con Placas mandibulares, Espinas de Hallucigenia y Ojos compuestos: Opabinia o Trilobite). Entonces sale un cartel para elegir uno, y se puede cambiar después desde la pausa.
- **Inestable (el precio):** con especiales equipadas, cada golpe que recibes quita **medio corazón más por cada especial** (con dos, un golpe de medio corazón quita corazón y medio). Solo los golpes (enemigos, proyectiles y jefes), no cada tick de veneno ni las caídas a las fosas. Lo que para un golpe entero (corazón morado, corazón verde) para también el extra. Como casi todos los golpes quitan medio corazón, una especial dobla el daño que recibes. Las especiales no dan corazones ni escudos, para que el precio no se anule.
- **Aspecto:** con alguna especial, los corazones del HUD llevan un borde dorado agrietado; al recibir un golpe, el medio corazón extra se rompe con un destello dorado. En la pausa: "Inestable: +½ corazón por golpe" (con el número de especiales). Si además llevas el corazón morado, el contorno de los corazones se queda morado y el dorado va solo en las grietas.

| Mutación | Ranura | Nivel I | Nivel II | Nivel III: evolución |
| --- | --- | --- | --- | --- |
| Flagelos de Leanchoilia | Extremidades | Cuando tu ataque principal alcanza a un enemigo, unos flagelos aparecen junto a él y lo azotan (animación de 1–1,5 s; el daño entra en el impacto, hacia los 0,4 s). El latigazo hace **el daño de 1 s de tu Boca** con sus efectos (Placas mandibulares 10 y Quebrar; Cnidocitos 6 y vuelve a envenenar; con la Corona de brazos azota a todos los que hay dentro del remolino, unos 2,5 a cada uno). Si el enemigo muere antes, va al más cercano a 2 u. Uno cada 3 s | Uno cada 2,5 s y +25 % de daño | **Latigazo en cadena:** el latigazo salta a los 2 enemigos más cercanos con el mismo daño |
| Cutícula de Marrella | Piel | **Esquiva justa:** si un ataque te alcanza mientras esquivas, tu piel lo refleja con un destello iridiscente. Un proyectil vuelve buscando a quien lo lanzó y le hace 6 de daño; un golpe cuerpo a cuerpo o de contacto aturde 1 s al atacante y le hace 3 de daño | 8 de daño el proyectil y 4 el destello, que aturde 1,5 s | **Prisma:** cada proyectil reflejado se divide en 3 en abanico |
| Yema clonal | Órgano especial | Clic derecho: brota una copia translúcida de ti durante 6 s. Va sola a por los enemigos con tu Boca (se aleja con proyectil o con la Corona de brazos, se acerca con Placas mandibulares) y hace la mitad de tu daño, con los efectos de la Boca pero no los de tus otras mutaciones. No recibe daño, pero los enemigos la atacan como a ti (señuelo). Flota, así que no cae a las fosas. Lo que mata cuenta como tuyo. La recarga de 15 s empieza cuando desaparece (también si se limpia la sala) | Dura 7 s y hace el 65 % de tu daño | **Gemación:** brotan dos copias |

- **Leanchoilia** era un artrópodo con dos grandes apéndices delante de la cabeza acabados en largos flagelos. **Marrella** conserva en sus fósiles la iridiscencia de sus espinas, que descomponían la luz. La **Yema clonal** se inspira en la gemación de los animales coloniales.
- **Arte:** iconos y una insignia dorada de especial en la esquina; la capa de la Cutícula sobre los 3 cuerpos; el latigazo, el destello iridiscente, el que aturde y el proyectil reflejado; el brote y la disolución en burbujas de la copia (que es tu propio sprite pasado a verde agua y translúcido con un shader, sin hojas nuevas); el borde dorado agrietado de los corazones y su rotura. Generador en `docs/multimedia-temp/clam-mutations/`.
- **Excepción (25 de octubre):** los Flagelos de Leanchoilia **no se dibujan en el cuerpo**, aunque sean Extremidades, porque una capa de apéndices desentonaba con los tres cuerpos. Se ven en el latigazo junto al enemigo y en un destello azul verdoso breve sobre el jugador cuando el latigazo vuelve a estar listo.

**Jefe del piso 1 (virus gigante):** 120 de vida, 3×3 unidades, avanza despacio hacia el centro, medio corazón por contacto. Fase 1: espiral de un brazo y ráfaga circular de 12 proyectiles cada 4 s. Fase 2 (desde el 50 %): espiral de dos brazos más rápida, ráfaga de 16 e invoca 2 bacterias cada 8 s (máximo 4 vivas).

**Segundo jefe del piso 1 (ameba gigante):** 180 de vida, unas 4,3 unidades (más grande que el virus), rosa pálido, medio corazón por contacto en todas las fases salvo agotada. En cada partida sale el virus o la ameba, al 50 %. Su sala es redonda (30 tiles de diámetro, borde escalonado), vacía y en azul marino para que contraste con ella. En las fases 1 y 2 no dispara: embiste en línea recta hasta chocar con una pared (se aplasta contra ella) y se queda quieta. Antes de volver a salir avisa durante 0,6 s: tiembla y mira hacia donde irá (la cara entera se desplaza hacia allí); al final se encoge para coger impulso y sale disparada ya a toda velocidad, en la dirección fijada al acabar el aviso. No empuja al jugador: lo atraviesa y le quita medio corazón al tocarlo.
- **Calma (100–80 %):** 1,2 u/s, sale hacia cualquier lado y para 1,5 s. Parece inofensiva.
- **Enojo (80–50 %):** pequeña animación de enfado; desde entonces sale hacia el jugador a 6 u/s (el triple, tan rápida como el jugador) y para 1 s.
- **División (50–10 %):** se parte en dos amebas medianas (60 % del tamaño) que comparten una sola barra de vida. Nada más nacer salen disparadas hacia lados opuestos de la sala; después embisten a 10 u/s: una va directa al jugador y la otra le anticipa (apunta adonde estará cuando llegue, según su dirección y velocidad). Si chocan entre sí, rebotan.
- **Final (10 %):** antes de ese golpe la vida no baja del 10 %. Escena en la que el jugador no puede hacer nada (franjas de cine, sin HUD salvo la barra del jefe, proyectiles fuera): la cámara va despacio al centro de la sala (2 s), las medianas se juntan allí (3 s) y la ameba se enfurece (2 s). Después, inmune (barra gris con candado), llena la sala de proyectiles durante 30 s con la cámara alejada para ver la sala entera, en 3 tramos de 10 s: anillos densos con un hueco que gira; brazos que giran en los dos sentidos y forman una red; y anillos más brazos más abanicos apuntados al jugador. No hay contador: el avance se nota en su animación. Al acabar queda agotada en el centro (pálida, barra roja que palpita) y cae de dos golpes.

## Pisos y etapas evolutivas

El juego completo tiene **6 pisos**, cada uno una etapa de la vida en la Tierra, con enemigos, jefe y mutaciones propios. La v1.0 sale con los pisos 1 a 3 (ver "Alcance").

**Tamaño de los pisos:** las partidas pueden durar de 40 minutos a 1 hora, como en *Enter the Gungeon*. El piso 1 sigue con 4–5 salas de combate y 1–2 laterales; desde el piso 2, 10–12 salas de combate en el camino, de 2 a 4 salas especiales, las laterales, la tienda y la sala de cambio.

Cada piso tiene **2 jefes posibles** y en cada partida sale uno al azar.

**Presentación de los jefes (decidida el 24 de octubre):** al entrar en la sala de cualquier jefe hay una presentación de unos 3 s, como en *Enter the Gungeon*. Sale siempre y se salta con E (un recordatorio pequeño abajo a la derecha: "[E] Saltar").

1. La puerta se cierra y el jugador queda quieto e invulnerable.
2. La cámara va al jefe, que hace su **pose de presentación** (animación propia de unos 8 cuadros; el último se mantiene).
3. Entra un **rótulo**: una banda diagonal oscura de lado a lado con un filo de color, el **nombre** del jefe a la izquierda, su **título** debajo y su **retrato** a la derecha, por encima de la banda.
4. El rótulo sale y la cámara vuelve al jugador; empieza la pelea.

- **Retrato:** una ilustración nueva de cada jefe de 176×176 (un cuarto de la pantalla, más o menos), a la misma escala de píxel que el juego.
- **Letra:** el nombre va en Jersey 10 (licencia OFL), la misma en todos los pisos; lo que cambia por piso es el color: relleno lima y banda granate con filo rosa en el piso 1, relleno turquesa y banda azul en el piso 2. El título va en Silkscreen.
- **Títulos:** Virus gigante, *Plaga primigenia*; Ameba gigante, *La que todo lo engulle*; Anomalocaris, *Terror del Cámbrico*; Euriptérido, *Escorpión de los mares*.
- En la sala del Anomalocaris, la presentación es el momento en que baja la luz.

| Piso | Etapa | Ambientación | Enemigos | Jefes (uno al azar) |
| --- | --- | --- | --- | --- |
| 1 | Sopa primordial | Charco microscópico, colores verdes y burbujas | Bacterias que persiguen, amebas que se dividen al morir | Virus gigante que dispara en espiral · Ameba gigante que se divide |
| 2 | Océano cámbrico | Fondo marino en tres franjas de profundidad, de las aguas someras al abismo | Trilobites que embisten, medusas que disparan anillos, gusanos Ottoia enterrados, esponjas que curan y cardúmenes de Pikaia | Anomalocaris (depredador con garras) · Euriptérido (escorpión marino gigante) |
| 3 | Pantano carbonífero | Pantano con helechos gigantes y niebla | Libélulas gigantes que caen en picado, anfibios que escupen | Artrópodo gigante (estilo Arthropleura) · Meganeura (libélula gigante) |
| 4 | Desierto pérmico | Dunas rojas, rocas y huesos | Lagartos que corren en zigzag, sinápsidos que lanzan rocas | Dimetrodon con vela dorsal que se calienta (fase 2 más rápida) · Scutosaurus (acorazado que embiste) |
| 5 | Selva de dinosaurios | Selva densa, helechos y volcanes al fondo | Compsognathus en manada, pterosaurios en picado, plantas carnívoras que disparan | Tiranosaurio · Espinosaurio |
| 6 | Edad de hielo | Tundra nevada, suelo helado que resbala | Lobos en manada, mamuts que embisten, cazadores que lanzan lanzas | Tigre dientes de sable · Tribu de humanos (el último depredador) |

**Piso 2: océano cámbrico (decidido el 24 de octubre)**

En el piso 1 casi todo se resolvía disparando a lo que se acercaba. En el piso 2 la dificultad viene de leer avisos, posicionarse y elegir a quién atacar primero, además de más vida (×1,5–2) y más enemigos por sala.

- **Números:** 10–12 salas de combate en el camino (las salas especiales con combate cuentan entre ellas). Presupuesto de enemigos de 4 en la primera sala, que sube 1,4 por sala, igual que en el piso 1 (que empieza en 3). Recompensa de mutación al limpiar las salas 2, 4, 6, 8 y 10, de 2 a 3 salas laterales, de 2 a 4 salas especiales, la tienda y la sala de cambio. Unos 15–20 minutos por partida en el piso.
- **Tres franjas de profundidad** según el avance por el camino, por tercios (con 12 salas: 1–4 Someras, 5–8 Arrecife, 9–12 Abismo), cada una con su tileset y su decoración:
  - **Someras** (primeras salas): arena turquesa y mucha luz, con cáusticas que ondulan sobre el suelo y rayos diagonales que entran desde arriba. Hierba marina, conchas y arrecifes de arqueociatos como cobertura.
  - **Arrecife** (salas del medio): el tema Cambrian de la sala de paso, con esponjas, corales, lirios de mar y conchas de trilobite.
  - **Abismo** (últimas salas y jefe): casi negro pero sin tapar la vista. La bioluminiscencia (plumas de mar, medusas peine, líquenes) da ambiente con luces 2D suaves. La oscuridad de verdad se reserva para la sala del Anomalocaris.
  - Los pasillos y las salas de transición mezclan las franjas, como la sala de paso. La **nieve marina** (partículas que caen despacio) es más densa cuanto más hondo. El generador hace que el camino tienda a bajar en la cuadrícula (abajo a menudo, a los lados a veces, casi nunca hacia arriba), así el minimapa baja hacia el jefe.
- **Cuevas generadas en cada partida:** la forma de cada sala sale de la semilla del piso con el método de la sala de paso (mancha orgánica con ruido, cuellos de las puertas y reparación automática de las celdas que el arte no puede dibujar), con formaciones de roca o coral dentro como cobertura. Si una cueva no pasa la comprobación, se usa una forma de un catálogo hecho a mano. La tienda, la sala de cambio y las de los jefes son formas hechas a mano. Las salas del piso 1 siguen rectangulares.
- **Pasillos:** túneles irregulares de 4 a 6 tiles de largo y 3 o 4 de ancho, con borde de cueva. Sin enemigos, con conchas, algas y nieve marina; cuando unen dos franjas, el suelo y la luz pasan de una a otra a lo largo del túnel.
- **Tienda y sala de cambio:** la tienda sale siempre a mitad del piso, en el Arrecife, como sala lateral; la sala de cambio va detrás, en un fondo de saco (camino → tienda → sala de cambio).
- **Fosas:** grietas y simas del fondo marino, de al menos 2×2 tiles, que salen en las salas especiales (y más adelante en las cuevas).
  - **Andando** sobre una fosa, el jugador cae: animación corta (por código: encoge, se oscurece y suelta burbujas), −½ corazón y vuelve al último suelo firme que pisó.
  - **Esquivando** la cruza: la esquiva recorre 3 u, así que los huecos que se pueden saltar miden 2 tiles como mucho. Si la esquiva termina encima de la fosa, también cae. La Cola propulsora (doble esquiva) deja cruzar huecos más anchos o encadenar dos.
  - Los enemigos que **nadan** (medusa azul, cardumen de Pikaia, Anomalocaris) pasan por encima: flotan con vaivén y su sombra queda separada del cuerpo. Los que **andan** (trilobite, Ottoia, Euriptérido) llevan la sombra pegada y las rodean.
  - Un enemigo **empujado** a una fosa muere al instante (las burbujas de la Cola propulsora y el estallido de Marea de la Corona sirven para eso). Los jefes no caen.
  - Los **proyectiles** pasan por encima.
  - Arte: tiles de 16×16 en la vista 3/4 de las paredes (se ve la pared del fondo que cae a la oscuridad), uno por combinación de vecinos que son suelo, más las esquinas de dentro y motas que suben del fondo.
- **Salas especiales:** salas hechas a mano que salen al azar entre las cuevas, como las salas fijas de *Enter the Gungeon*. Un catálogo de 10 para el piso 2; sirven para cualquier franja y la mecánica sigue en los pisos siguientes. En cada partida salen de 2 a 4, sorteadas entre 9 (la del corazón morado sale siempre). Aparte, sin contar en ese número, sale siempre una **sala de la almeja**: una al azar entre la Gruta escondida, el Anillo de la sima y el Jardín de esponjas, que llevan siempre una almeja grande; así cada piso tiene al menos una almeja grande (con su mutación especial). Si otra de las tres sale además en el sorteo, la partida tiene dos. Nunca en las dos primeras salas ni en la que va justo antes del jefe. Las grandes ocupan dos casillas de la rejilla del generador. Tienen la misma forma en cada partida, con su contorno de cueva (fijo, de una semilla que respeta las reglas del arte y el alcance).

  | Sala | Dónde | Qué tiene |
  | --- | --- | --- |
  | Parkour | Camino, 2 casillas en vertical, sin enemigos (no cuenta como sala de combate) | Se entra siempre por arriba y se sale por abajo. El interior es una fosa gigante con columnas de suelo: se baja saltando de una a otra con la esquiva. Al final, una almeja con 5–15 de biomasa |
  | Corazón morado | Lateral, **siempre en el Abismo del piso 2**, opcional | El doble de enemigos, más fosas y más decoración. En el centro, rodeado de fosa, un coral con un **corazón morado**: para un golpe entero, como el medio corazón verde del Herbívoro, y se gasta con el primero. **Se conserva entre pisos**: abrirá una sala secreta del piso 3 |
  | Anillo de la sima | Camino | Sima redonda con un anillo de suelo; en el centro, una isla con una almeja grande a la que solo se llega esquivando |
  | Puente roto | Camino | Una grieta de 3 tiles, demasiado ancha para la esquiva, con dos puentes. Los enemigos llegan en oleadas desde la otra orilla |
  | Bosque de coral | Camino | Columnas de roca de adorno y corales grandes que tapan la vista; llena de Ottoias |
  | Fósil gigante | Camino | El esqueleto de un pez acorazado gigante: la columna y las costillas son cobertura. Al limpiarla deja restos de Carroña y biomasa extra |
  | Jardín de esponjas | Camino | Sala en U con un estanque. Rompe el límite de una esponja por sala: hay 3 (una en un islote) y matarlas da un premio. En un rincón, una almeja grande |
  | Sala sellada | Camino | Amplia y con pocas coberturas; las puertas se cierran y llegan tres oleadas seguidas. Al final, una recompensa de mutación |
  | Islas | Camino | Plataformas unidas por puentes de un tile sobre la fosa; un enemigo empujado fuera de su isla cae |
  | Gruta escondida | Lateral, sin enemigos | Pequeña, con un laberinto de grietas; en el centro, una almeja grande con biomasa |

  Los **cofres** del piso 2 son almejas gigantes (vieiras) que al abrirse sueltan la biomasa. Las **entradas de oleadas** son grietas del suelo que se abren, brillan en rojo y burbujean al salir los enemigos.
- **Puertas de losas de roca:** dos losas de la misma piedra que la pared salen de dentro y se juntan; en la costura dentada se completa un sello del color de las vetas de cada franja. Cerrar y abrir son animaciones distintas: al cerrar chocan con polvo y burbujas, y al abrir el sello se enciende y las losas se retiran. El piso 1 conserva la membrana.
- **Música y sonido:** exploración y combate del océano y una pista por jefe, con el mismo sistema del piso 1; el ambiente podría ganar graves con la profundidad. Se hacen más adelante.

| Enemigo | Papel | Comportamiento | Lo que exige al jugador | Vida | Coste · desde |
| --- | --- | --- | --- | --- | --- |
| Trilobite | Embestida | Ronda por la sala. Al verte avisa 0,6 s rascando el suelo con los ojos rojos y embiste en línea recta muy rápido. Si choca con una pared queda aturdido 1 s, panza arriba. Su cabeza blindada reduce a la mitad el daño que le entra de frente | Esquivar en el último momento y castigar el aturdimiento, o rodearlo | 14 | 2 · sala 1 |
| Medusa azul | Tiradora de área | Flota despacio, a impulsos, a 3,5–6 u de ti. Cada 3 s, si te ve, se llena (aviso) y al contraerse suelta un anillo de 8 proyectiles lentos con un hueco de 72° por donde pasar, que gira 50° con cada anillo. Hace daño si la tocas. Es azul translúcida para no confundirla con la medusa del jugador, que es rosa | Leer el hueco del anillo y no quedarse quieto | 10 | 2 · sala 2 |
| Ottoia (gusano de las arenas) | Emboscada | Enterrada solo se ve una onda en la arena que se acerca a ti, y no recibe daño ni choca con nada. Al llegar debajo de ti avisa con un círculo de 0,8 s, sale y muerde (un corazón) a quien siga dentro; se queda 1 s asomada y vulnerable y vuelve a hundirse. Tocarla no hace daño, para poder ir a golpearla | Apartarse al ver el círculo y golpearla cuando asoma | 9 | 3 · sala 2 |
| Esponja Vauxia | Apoyo | No se mueve ni ataca, y nada la mueve. Cada 3 s emite un pulso que cura 2 de vida a los demás enemigos a 5,2 u (a sí misma no) y les da un leve escudo (absorbe 2 de daño, 3,5 s); un hilo de luz la une con cada curado, que destella en verde. Como mucho una por sala | Decidir si ir a por ella primero, aunque haya que cruzar el combate | 16 | 3 · sala 3 |
| Cardumen de Pikaia | Enjambre | 5 peces pequeños y rápidos (de los primeros cordados) que salen juntos y cuestan como uno. Nadan en grupo; al verte te rodean en un corro que gira y, de uno en uno, se paran un instante apuntándote y hacen una pasada que te atraviesa | Gestionar varios objetivos a la vez; aquí brillan las Bocas de área y las Espinas | 3 cada uno | 2 · sala 1 |

Cada uno pide una habilidad distinta (reflejos, lectura de patrones, saber esperar, prioridad de objetivos y control de grupos), y juntos crean salas difíciles sin que nadie sea una esponja de vida (salvo la Esponja literal, que no ataca).

**Jefes del piso 2:** 300 de vida cada uno, uno al azar en cada partida. Sus fases cambian la forma de pelear más que la cantidad de daño.

**Anomalocaris:** el gran depredador del Cámbrico, con dos apéndices delante de la boca. Unas 4,7 u de largo. Sala muy grande (cueva ovalada de unos 36×26, sin fosas: en el frenesí hace falta todo el suelo). Al entrar baja la luz y queda en penumbra: se ve, pero poco. Las plantas luminosas, repartidas por los bordes y alguna en el centro (luces 2D), dan casi todo el brillo, y 4–6 columnas de algas tapan la vista sin bloquear el paso. Pide esquivar en el momento justo y castigar la pausa tras cada zarpazo.
- **Acecho (100–60 %):** nada deprisa en círculos junto a las paredes y deja una estela de burbujas lentas. Zarpazo: avisa 0,7 s echándose atrás con los ojos rojos y los apéndices abiertos, golpea en un cono por delante y se queda 1 s quieto y vulnerable.
- **Cerco (60–25 %):** nada en espiral a tu alrededor, cerrándose y dejando un muro de burbujas, y termina el cerco con un zarpazo.
- **Escena al 25 %:** franjas de cine y las plantas se apagan un instante; solo se ven sus ojos nadando en la oscuridad, cruzan la sala y se oye un rugido. Es inmune durante la escena.
- **Frenesí (25–0 %):** se queda quieto y hace un sonido de carga inconfundible. En el suelo aparecen el área y la trayectoria de sus dos zarpazos (franjas rojizas con forma de flecha) durante 0,8 s al empezar la fase, y unos 0,5 s cuando le queda poca vida. Luego lanza los dos zarpazos demasiado rápido para reaccionar: hay que salir de las zonas marcadas mientras se ven. Después, una pausa corta en la que se le puede castigar.

**Euriptérido:** escorpión marino gigante con pinzas y una cola con púa. Unas 5,6 u de largo. Sala: cueva de unos 30×22 con 5–6 rocas de cobertura y grietas solo en los bordes, que en la fase 2 obligan a no pegarse a las paredes. Pide posicionarse y usar la cobertura.
- **Acorazado (100–50 %):** avanza y ataca con las pinzas si estás cerca (golpe corto con aviso). Cada pocos segundos las púas de su caparazón se erizan, se cargan de veneno y suelta un abanico de 3 púas venenosas hacia ti (la cola no se usa en esta fase). Su coraza reduce a la mitad el daño que le entra de frente, como al Trilobite.
- **Escena de la muda (50 %, unos 3 s):** franjas de cine; se queda quieto, le salen grietas, el caparazón se abre y sale un cuerpo nuevo, rosado y blando. La muda vacía se queda en la sala como cobertura y bloquea proyectiles, los suyos y los tuyos; a la vez se derrumban 2 de las rocas, para que la fase 2 no sea más fácil. Es inmune durante la escena.
- **Blando y furioso (50–0 %):** recibe un 25 % más de daño y va el doble de rápido. Estrena la cola: barrido de 180° con estela venenosa, con aviso (la punta se enciende en verde). Sus pinzas encadenan dos golpes.

**Salas de los jefes:** antes de cada una hay una **antesala**, un túnel más largo que acaba en una puerta de losas grande con el sello encendido. Las dos están hechas a mano y los jefes no caen en las fosas.

**Paso al piso siguiente**

1. **Burbujas del jefe:** al morir, el jefe deja burbujas de mutación de las ranuras que no son la Boca; una de ellas es siempre la mejora de una mutación equipada.
2. **Sala de paso:** junto a la sala del jefe hay otra que solo se abre al vencerlo. En ella está el paso al piso siguiente. Al usarlo se abre una ventana, más vistosa que la elección de mutación, con las Bocas del piso siguiente (una por familia), y elegir una es obligatorio: nadie llega al piso siguiente sin Boca.
3. **Nivel de la Boca nueva:** empieza en el nivel I. Para no castigar a quien subió su Boca, los niveles que tenía por encima del I se reparten al azar, uno a uno, entre las demás mutaciones equipadas que aún puedan subir. Los que no quepan en ninguna se pierden (no vuelven a la Boca): así no compensa llegar al jefe con solo la Boca mejorada.
4. **Metamorfosis:** cinemática en la que el jugador se transforma en la criatura del piso siguiente según la familia de la Boca elegida, y el juego pasa al piso nuevo.
5. **En el piso siguiente** no se ofrecen otras Bocas (ni en las elecciones ni en la tienda), pero sí las mejoras de la que llevas.

**Cómo es la sala de paso**

- **Forma:** una cueva orgánica de borde irregular, distinta de todas las demás salas. Se engancha a la sala del jefe que toque (virus o ameba) y sale en el minimapa con un icono propio cuando se abre.
- **Del piso actual al siguiente:** la entrada todavía parece del piso actual, y hacia la fosa el suelo, las paredes, la luz y la decoración pasan poco a poco a la paleta y la ambientación del piso siguiente (para el piso 2: esponjas, corales primitivos, algas, conchas de trilobite y tallos de lirio de mar, que luego se reutilizan en el piso 2).
- **La fosa:** en el centro hay una fosa profunda y tiene que verse a simple vista que hay que saltar en ella. Tiene un borde con un saliente claro, el interior oscuro, una luz azulada que sube del fondo y burbujas o partículas que ascienden. Es un obstáculo sólido: no se puede caer en ella andando ni esquivando.
- **Pasillo largo:** el pasillo del jefe a la sala de paso mide 14 tiles (los demás, unos 4), para que dé tiempo a notar el cambio de sonido.
- **Sonido:** desde la entrada del pasillo (a unas 30 unidades de la fosa) hasta su borde, la música y el ambiente del piso actual se van apagando y crece el ambiente del piso siguiente (para el piso 2, mar profundo con oleaje lento, burbujas grandes y un fondo grave que respira). Durante un combate no cambia. El salto y la caída tienen sus propios efectos (impulso que sube; silbido que baja y un "bloop" grave al llegar al fondo).
- **Activación:** acercándose a cualquier punto del borde aparece la tecla E en el punto más cercano, con el texto "Saltar al piso siguiente" (o "Te dejas mutaciones" si quedan burbujas del jefe sin coger). Al pulsar E no hay vuelta atrás.

**Secuencia del salto**

1. **Salto:** el jugador da un pequeño salto hacia el centro de la fosa.
2. **Elección de Boca:** casi al final del salto, sobre la fosa, se queda congelado en el aire, la pantalla se oscurece (dejando ver a la criatura) y aparece la ventana "Elige tu Boca" con las Bocas del piso siguiente, sin botón para cerrarla ni Esc. Cada Boca es una carta grande con la criatura en que te convertirás, en reposo; con el ratón encima la carta crece un poco y la criatura hace su ataque en bucle. Debajo, icono, nombre, familia y lo que hace en el nivel I. Bajo las cartas, un aviso de lo que pasará con los niveles de tu Boca actual.
3. **Reparto de niveles:** tras elegir, la otra carta se desvanece y aparece una fila con tus ranuras: la Boca vieja deja su sitio a la nueva (nivel I) y cada nivel extra sale de ella como un "+1" que vuela a la mutación que lo recibe (su insignia sube) o, si no cabe en ninguna, se convierte en "Perdido" y se desvanece.
4. **Metamorfosis (unos 6 s, decidida el 23 de octubre):** con franjas de cine y sin HUD. Fundido a un abismo por el que la criatura cae con su aspecto real (degradado azul muy profundo, burbujas y motas que suben deprisa, rayos de luz tenues desde arriba). Empieza a brillar como en la evolución de los linajes, pero **del color de la familia de la Boca elegida** (rosa para proyectil, naranja para cuerpo a cuerpo, lima para área): el aura crece, la silueta se vuelve blanca y las piezas se funden en la luz. La silueta cambia despacio de la célula a la criatura nueva, un estallido de luz la revela con sus colores y hace una vez su ataque como pose de presentación. Fundido a negro.
   - **Saltarla:** desde que se ha visto entera una vez (se guarda entre partidas), Interactuar (E) la salta; mientras se puede, un aviso abajo a la derecha lo dice ("E: saltar escena").
   - **Cambio de cuerpo:** al revelarse, el jugador pasa a tener el cuerpo de la criatura (sus animaciones de reposo, moverse, esquiva, ataque, daño y muerte), que manda sobre la célula y la Piel. Hasta que se diseñen sus capas en el piso 2, las piezas de las demás ranuras no se dibujan sobre el cuerpo de la criatura.
5. **Llegada:** fundido al piso siguiente. Mientras no exista el piso 2, la pantalla de victoria, que enseña la criatura nueva en reposo.

**Sala de cambio (el laboratorio de Darwin)**

Una por piso desde el piso 2, siempre pegada a la tienda (así es fácil de encontrar). En el centro hay una cápsula llena de líquido; al acercarse y pulsar E se abre su propia interfaz, distinta de las demás:

- Muestra al jugador y lo que lleva en cada ranura, como el panel de la criatura de la pausa.
- Al elegir la mutación de una ranura, aparecen las mutaciones del piso actual de esa ranura por las que se puede cambiar, cada una con su dibujo y su descripción, **al mismo nivel** que la que entregas. Se elige una y se confirma con Aceptar: la tuya desaparece y la nueva entra con ese nivel.
- **Gratis y solo un cambio por piso**, así que hay que pensarlo bien. Es la única forma de cambiar de Boca (y de cuerpo, si cambia la familia; ver "Forma del jugador en cada piso"). Después el líquido de la cápsula baja y se apaga.
- **Mutaciones de un piso anterior:** también se pueden entregar, pero la nueva entra **un nivel por debajo** (como mínimo I), porque el nivel I de un piso vale como el II del anterior.
- **Especiales:** las mutaciones especiales de las almejas ni se entregan ni se reciben.
- **Metamorfosis en la cápsula:** si la Boca nueva cambia el cuerpo, la criatura entra en la cápsula, el líquido brilla del color de la familia nueva, la silueta del cuerpo viejo cambia al nuevo, se revela con sus colores y sale delante con un destello (unos 3 s, con las franjas de cine). Si no cambia el cuerpo, el líquido solo brilla un momento.
- No existe en el piso 1, donde las Bocas salen en las recompensas normales.

**Forma del jugador en cada piso**

Para que el arte sea abarcable, el cuerpo del jugador **no** combina mutaciones: cada piso tiene su criatura en **3 variantes, una por familia de arma**, y las demás mutaciones siguen siendo piezas sencillas encima del cuerpo.

- **Familias de arma:** *Cuerpo a cuerpo* (Mandíbulas, Colmillos), *Proyectil* (Aguijón, Cnidocitos, Escupitajo venenoso) y *Área* (Esporas, Escupitajo ácido, Trompa). Sin mutación de Boca se usa la variante *Proyectil*, porque el disparo base es un proyectil.
- **Cuándo cambia:** al elegir la Boca en la sala de paso tras el jefe (ver "Paso al piso siguiente"), con una cinemática de metamorfosis. En el piso siguiente no se ofrecen Bocas, así que el cuerpo y el arma siempre se corresponden. La única forma de cambiar de Boca es la sala de cambio; si la nueva es de otra familia, hay otra metamorfosis y cambia el cuerpo (si es de la misma, solo cambia la pieza del arma).
- **En el piso 2 hay 3 Bocas y 3 cuerpos:** Cnidocitos (proyectil → medusa), Placas mandibulares (cuerpo a cuerpo → pez acorazado) y Corona de brazos (área → lirio de mar). Un piso con solo 2 Bocas tendría solo 2 cuerpos.
- **Piso 1:** la célula actual con sus piezas (ya hecha), sin variantes.
- **La Boca va en el cuerpo (desde el piso 2):** como en cada piso hay una Boca por familia, cuerpo y Boca siempre van juntos. La Boca se dibuja dentro del cuerpo y cada cuerpo tiene su **animación de ataque propia y trabajada** en las 3 direcciones (preparación, golpe y recuperación), en lugar de la pieza de 3 cuadros que gira hacia el cursor en el piso 1. El efecto del golpe (el arco del mordisco, el destello del disparo) sí gira hacia el cursor.
- **Tamaño (decidido el 24 de octubre):** todo el juego usa 16 píxeles por unidad y nunca se mezclan tamaños de píxel; el detalle viene de que **las criaturas crecen piso a piso**, como la vida se vuelve más compleja: la célula ocupa 16×16, los cuerpos del océano unos 20 dentro de un lienzo de 24, y los de los pisos siguientes unos 32 (los jefes son más grandes). El resto del detalle sale de paletas más ricas, más cuadros de animación, efectos e iluminación 2D. La zona de choque sigue siendo la de la célula, así que la dificultad no cambia.
- **Piezas del resto de ranuras:** Extremidades y Piel son capas sencillas colocadas en unos pocos puntos fijos de cada cuerpo (costados para las extremidades, lomo para la piel). En los pisos 2 a 6 la Piel deja de sustituir al cuerpo y pasa a ser una capa encima (por ejemplo, el caparazón en el lomo). Desde el piso 2 (decidido el 24 de octubre): 4 mutaciones con pieza × 3 cuerpos = 12 capas por piso; el Órgano no tiene pieza, solo efectos de activación vistosos; y las mutaciones de pisos anteriores no se dibujan sobre el cuerpo nuevo (solo salen en el HUD y en la pausa).
- **Arte:** 15 cuerpos nuevos (pisos 2 a 6 × 3 familias), cada uno con las animaciones del jugador (reposo, moverse, esquiva y muerte en 3 direcciones).

| Piso | Cuerpo a cuerpo | Proyectil | Área |
| --- | --- | --- | --- |
| 1 | Célula | Célula | Célula |
| 2 | Pez acorazado (placodermo) | Medusa | Lirio de mar con remolino |
| 3 | Anfibio de fauces anchas | Escorpión de pantano | Rana venenosa |
| 4 | Gorgonopsio (reptil con colmillos de sable) | Lagarto escupidor | Reptil con gola que expulsa polvo |
| 5 | Raptor | Dilofosaurio | Parasaurolophus (bramido con la cresta) |
| 6 | Tigre dientes de sable | Puercoespín gigante | Mamut |

**Extras a futuro:** que los enemigos de cada piso también tengan variantes "mutadas".

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
| Mapa | TAB | Abre el mapa grande y detiene el juego; TAB o Esc lo cierran. Arriba a la derecha hay siempre un minimapa |

**Reglas de combate:**

- Vida en corazones (empieza con 3), como Gungeon: cada golpe quita medio o uno entero.
- Al entrar en una sala de combate, las puertas se cierran hasta matar a todos los enemigos.
- Proyectiles enemigos lentos y de colores fuertes para que se esquiven leyendo la pantalla.

## Alcance de la primera versión (v1.0)

La v1.0 tiene los **pisos 1 a 3** completos y pulidos (si el 3 se retrasa mucho, se publica con 2), y se publica en itch.io. Los pisos 4 a 6 llegan en actualizaciones. El orden de trabajo, por módulos y ramas, está en [PLAN.md](PLAN.md).

**Mínimo jugable (hecho en la v0.5)**

- [x] Movimiento, apuntado, disparo y esquiva del jugador
- [x] Vida, daño, muerte y pantalla de reinicio
- [x] 2 tipos de enemigo con IA simple (perseguir, disparar)
- [x] Salas conectadas con puertas que se cierran durante el combate
- [x] 1 piso completo con 5 a 7 salas y 1 jefe
- [x] 9 mutaciones repartidas en las 4 ranuras, con elección de 1 entre 3 al limpiar algunas salas
- [x] Menú principal y pantallas de victoria y derrota
- [x] Build web en itch.io

**Para la v1.0**

- [x] Mapa distinto en cada partida y salas con formas distintas
- [x] Arte del piso 1 y cambio visual del jugador por cada mutación (jugador, enemigos, jefe, proyectiles, efectos, escenario y piezas de mutación)
- [x] Música y efectos de sonido
- [x] Linajes con su beneficio (base por clase y toque de cada animal)
- [ ] Más variedad en el piso 1: un tercer enemigo y salas especiales
- [ ] Paso de un piso al siguiente: sala de paso con la elección de Boca y la metamorfosis del jugador según su familia de arma
- [x] Sala de cambio (el laboratorio de Darwin), desde el piso 2
- [ ] Piso 2 (Océano cámbrico): 3 franjas de escenario con cuevas generadas, fosas y 10 salas especiales, 5 enemigos, Anomalocaris y Euriptérido, 9 mutaciones, 8 linajes y los 3 cuerpos del jugador
- [ ] Presentación de los jefes al entrar en su sala (los 4 jefes de los pisos 1 y 2)
- [ ] Piso 3 (Pantano carbonífero): lo mismo, con el artrópodo gigante
- [x] Tienda con biomasa

**Más adelante**

- [ ] Pisos 4 a 6
- [ ] Salas secretas (la primera, en el piso 3, se abre con el corazón morado del piso 2)
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

**Pixel art:** sprites de 16 px por unidad en todo el juego (la célula de 16×16; las criaturas crecen piso a piso: unos 24 en el océano y unos 32 después; los jefes, más grandes), filtro *Point* sin compresión y la *Pixel Perfect Camera* de URP para que los píxeles no se deformen.

## Preguntas abiertas y decisiones

**Pendiente**

- [ ] Revisar los borradores: criaturas del jugador, enemigos y jefes de los pisos 3 a 6, y sus mutaciones nuevas (con sus niveles II y III). El piso 2 se cerró el 24 de octubre.
- [ ] Ataques y valores de los segundos jefes de los pisos 3 a 6.
- [ ] Duración total: 6 pisos de 10 a 15 minutos son más de una hora; valorar pisos más cortos en los pisos altos.

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
| Obtención de mutaciones | Al limpiar la 2.ª y la 4.ª sala de combate del camino, y cada sala lateral, aparece "elige 1 de 3": 3 mutaciones al azar, sin las equipadas, con botón Saltar. El jefe, al morir, deja 3 burbujas de mutación en el suelo (las únicas que caen al suelo): al coger una, las otras revientan. La salida se abre igual, así que es opcional |
| Mutaciones de la v1.0 | Las 9 del borrador, con los valores de la tabla "Valores de las mutaciones" |
| Sinergia de linajes | Cada mutación guarda su linaje; el bonus queda como deseable. Ojo: Tóxico no puede reunir 3 (dos de sus mutaciones son de Boca) |
| Aspecto de las mutaciones | Cambio muy visible por capas, al estilo de *Everything is Crab*: la Piel sustituye al cuerpo entero y lo agranda (también su colisión); la Boca es un arma grande en el borde del cuerpo que gira hacia el cursor y anima cada ataque; las Extremidades se mueven con el cuerpo; el Órgano se ve dentro de la célula y se activa con la habilidad. Las piezas se ocultan al esquivar y al morir |
| Mapa | Camino con giros del inicio al jefe, con 4 o 5 salas de combate y 1 o 2 salas laterales sin salida; enemigos por presupuesto que sube a lo largo del camino. Semilla visible en la pausa |
| Salas | Formas en texto: pequeña 22×15, normal 28×16 (cabe en pantalla), grande 40×24, con columnas, en cruz y en L |
| Mandíbulas | Mordisco cuerpo a cuerpo en arco delante de la boca, no un disparo |
| Recarga del Órgano especial | Barra bajo el jugador que se llena durante la recarga, destella y desaparece al estar lista |
| Biomasa | Moneda de la partida (se pierde al morir). Cada enemigo, al morir, suelta biomasa grande (vale 5) con un 10 % de probabilidad o pequeña (vale 1) con un 40 %; las crías, solo pequeña con un 10 %; el jefe, 10 fijos (sus invocaciones no sueltan nada). Cada sala de combate da como mínimo 2. Se recoge al tocarla, con un pequeño imán; al limpiar la sala, toda la que queda vuela hacia el jugador y se recoge sola (como en *Enter the Gungeon*). Contador en el HUD bajo las ranuras de mutación |
| Tienda | Una por piso: una sala lateral más (la pequeña, 22×15), sin salida y sin enemigos, que cuelga de una sala de combate de la segunda mitad del camino. Su icono sale en el mapa al limpiar la sala de la que cuelga. El tendero es un pequeño Charles Darwin tras su mostrador, de momento solo decorativo, que celebra cada compra. 4 pedestales que se surten la primera vez que entras y ya no cambian: 2 mutaciones al azar sin las equipadas (15), la mejora de nivel de una mutación equipada que pueda subir, con su icono y "I > II" (20 a nivel II, 30 a nivel III; si ninguna puede subir, una tercera mutación) y medio corazón (5), que es un corazón extra (25) si entras con la vida llena. Al acercarse aparecen la tecla E sobre el artículo y un cartel con el nombre, los detalles y qué mutación reemplaza; se compra con E (Interactuar, la tecla para todo lo que se use en el futuro). Si no llega la biomasa o no sirve (vida llena), el precio está en rojo y el artículo tiembla. Cada pedestal se vende una sola vez y en su sitio queda flotando una señal de vendido. Los pedestales de mutación ofrecen en cada momento lo que le sirve al jugador: la mutación (15) si no la lleva, su mejora (20 o 30) si la lleva, aunque la consiguiera o subiera fuera de la tienda, y, si ya la tiene al nivel III, el artículo desaparece para siempre y queda clavado un letrero de no disponible. Si el jugador cambia la mutación de la mejora por otra, ese pedestal la vende como mutación nueva |
| Cursor | Mira dibujada por el juego durante la partida; en la pausa y en la elección de mutación, cursor normal |
| Jefe del piso 1 | Virus gigante con 2 fases (la segunda desde el 50 % de vida); vencerlo abre la salida y da la victoria |

**Decidido el 13 de octubre**

| Tema | Decisión |
| --- | --- |
| Número de pisos | 6: sopa primordial, océano cámbrico, pantano carbonífero, desierto pérmico, selva de dinosaurios y Edad de hielo |
| Alcance de la v1.0 | Pisos 1 a 3 (como mínimo 2); los pisos 4 a 6, en actualizaciones. Sustituye a "1 piso muy pulido" |
| Forma del jugador | Una criatura por piso en 3 variantes según la familia de arma (cuerpo a cuerpo, proyectil, área), elegida al pasar de piso. Se descarta el cuerpo que combinaba las temáticas de varias mutaciones (piloto en la rama `feature/evolving-body`, sin integrar) por la cantidad de arte que exigía |
| Mutaciones nuevas | 4 por piso, una por ranura, que se suman a las anteriores |
| Aspecto de las mutaciones | Se mantiene la decisión del 11 de octubre para el piso 1. Desde el piso 2, la Piel es una capa encima del cuerpo en lugar de sustituirlo |
| Linajes | Sustituyen a Depredador, Acorazado y Tóxico. Un linaje es un animal que se activa con sus 4 mutaciones (una por ranura); una mutación puede estar en varios linajes. Cada linaje pertenece a una clase (Carnívoro, Herbívoro, Venenoso u Hongo), que decide el tipo de beneficio. 7 linajes en el piso 1 |
| Mutaciones del piso 1 | 12: las 9 de la v0.5 más Hifas, Camuflaje y Vacuola digestiva |
| Jefes | 2 por piso, uno al azar en cada partida. Piso 1: virus gigante o ameba gigante |

**Decidido el 23 de octubre**

| Tema | Decisión |
| --- | --- |
| Mutaciones por piso | Cada piso tiene sus propias mutaciones (8 por ahora, 12 más adelante) y sus propios linajes. Desde el piso 2 solo se ofrecen las de ese piso. Sustituye a "4 mutaciones por piso que se suman a las anteriores" |
| Dificultad | Vida de los enemigos ×1,5–2 por piso, más densidad y patrones; el nivel I del piso N+1 vale como el II del piso N |
| Linajes entre pisos | Útiles un piso por encima del suyo, flojos dos pisos por encima; se ajustan valores al cambiar de piso, nada visual |
| Boca y cuerpo | La Boca del piso siguiente se elige obligatoriamente en la sala de paso tras el jefe y decide el cuerpo, con una metamorfosis. En el piso siguiente no se ofrecen Bocas; solo la sala de cambio cambia la Boca |
| Sala de cambio | Una por piso desde el piso 2, junto a la tienda, gratis, un solo cambio: das una mutación y recibes otra de la misma ranura y el mismo nivel |
| Tamaño de los pisos | Piso 1: 4–5 salas de combate y 1–2 laterales. Desde el piso 2: 8–10 de combate. Partidas de 40–60 minutos |
| Sala de paso | Cueva orgánica que pasa poco a poco del piso actual al siguiente (paleta, decoración y sonido) con una fosa en el centro. E en el borde: salto, elección de Boca con el jugador congelado en el aire, reparto de niveles visible en la ventana y metamorfosis durante la caída (membrana que revienta). Antes de la sala se hacen el arte de los cuerpos del piso 2 y el diseño de su Boca cuerpo a cuerpo |

**Decidido el 15 de octubre**

| Tema | Decisión |
| --- | --- |
| Aspecto del Aguijón | A 2/3 de su tamaño (unos 13 px de la unión a la punta); los proyectiles salen de la punta |
| Aspecto de las Esporas | Dejan de ser un arma que gira: son un gorrito de Amanita (rojo con lunares blancos) sobre la cabeza de cualquier Piel, que sigue el balanceo del cuerpo. Al atacar, el cuerpo se adelanta y el gorrito se aplasta y se hincha (por código). Esporas crema con borde rojizo |
| Ataque de las Esporas | Bocanada que flota: las esporas frenan y flotan como minas que se dispersan despacio hasta desvanecerse (valores en la tabla) |
| Mordisco con avance | Mandíbulas, desde el nivel II, se lanza un poco hacia delante si no hay nadie a tiro |
| Beneficio de los linajes | Base por clase (Rematar, Pastar biomasa, Toxina débil, Colonizar la sala) más un toque propio de cada animal. Fijo, sin escalar con los niveles; se pierde al cambiar una de las 4 mutaciones, también en pisos siguientes. Rematar cuenta como herido al enemigo con la mitad de su vida o menos (con el 30 % casi no se notaba en el piso 1). Los aumentos de daño que dan decimales (1 × 1,5) se redondean al azar, para que de media sean exactos |
| Evolución de los linajes | Al completar un linaje, animación de unos 2 s con el juego congelado y un pequeño zoom (la célula se transforma en el animal) y, mientras dura el linaje, sus piezas sustituyen a las de algunas de sus mutaciones (tabla de la sección de linajes). Se descartó un cuerpo completo por linaje: demasiado arte y chocaba con la forma por piso |
| Interfaz de los linajes | Cartel breve al completarlo y fila en las estadísticas de la pausa, con el beneficio completo al pasar el ratón; las tarjetas y la tienda solo dicen a qué linajes pertenece cada mutación |

**Decidido el 24 de octubre (diseño del piso 2)**

| Tema | Decisión |
| --- | --- |
| Mutaciones del piso 2 | 9: Cnidocitos, Placas mandibulares y Corona de brazos (las 3 Bocas), Cola propulsora, Espinas de Hallucigenia, Placas de trilobite, Escleritos de Wiwaxia, Saco de crías y Ojos compuestos, con sus niveles (tabla "Mutaciones del piso 2") |
| Clases y linajes | Las clases atraviesan la partida (duermen, vuelven y se remodelan; al menos una nueva por piso) y los animales son de cada piso. Los linajes de un piso no siguen en el siguiente. Piso 2: Carnívoro, Herbívoro y Venenoso remodelados, Carroñero nueva y Hongo dormido; 8 linajes. Sustituye a "útiles un piso por encima" |
| Enemigos del piso 2 | Trilobite, Medusa azul, Ottoia, Esponja Vauxia y Cardumen de Pikaia, cada uno con un papel distinto |
| Jefes del piso 2 | Anomalocaris (sala oscura, escena de los ojos al 25 %, frenesí con marcadores) y Euriptérido (púas del caparazón, escena de la muda al 50 % que deja cobertura, cola en la fase 2), 300 de vida, uno al azar |
| Salas del piso 2 | 8–10 de combate en tres franjas de profundidad (Someras, Arrecife, Abismo), cuevas generadas por semilla con catálogo de reserva, nieve marina, camino que baja y puertas de losas de roca. El Abismo es oscuro sin tapar la vista |
| Escala del arte | 16 px por unidad en todo el juego; las criaturas crecen piso a piso (16, 24, unos 32). Nunca se mezclan tamaños de píxel |
| Piezas sobre los cuerpos | Boca dibujada en el cuerpo; Extremidades y Piel como capas (12 por piso); Órgano sin pieza, con efectos de activación; las mutaciones de pisos anteriores no se dibujan |
| Mejoras de pisos anteriores | Resuelto con las reglas actuales (cualquier mutación que lleves se puede mejorar; la regla de escalones hace que compense cambiarla). Sale de "Mejoras a futuro" |

**Decidido el 24 de octubre (salas del piso 2)**

| Tema | Decisión |
| --- | --- |
| Números | 10–12 salas de combate (las especiales con combate cuentan), recompensas tras las salas 2, 4, 6, 8 y 10, presupuesto 4 + 1,4 por sala, franjas por tercios. Sustituye a "8–10 salas" y "1,6 por sala" |
| Fosas | Andando caes (−½ corazón, vuelves al último suelo firme), esquivando las cruzas (huecos de 2 tiles como mucho). Los que nadan pasan por encima y los que andan las rodean; los enemigos empujados mueren; los jefes no caen; los proyectiles pasan |
| Salas especiales | Catálogo de 10 hechas a mano, de 2 a 4 por partida, con contorno de cueva fijo. La del corazón morado sale siempre en el Abismo; el corazón para un golpe, se conserva entre pisos y abrirá una sala secreta del piso 3 |
| Pasillos, tienda y jefes | Túneles irregulares sin enemigos; tienda a mitad del piso con la sala de cambio detrás; antesala antes de cada jefe; sala del Anomalocaris sin fosas y con la luz que baja al entrar; en la del Euriptérido se derrumban 2 rocas con la muda |
| Presentación de los jefes | Unos 3 s al entrar, siempre, se salta con E: pose, rótulo con nombre (Jersey 10, color por piso), título y retrato de 176 px. Para los 4 jefes de los pisos 1 y 2 |

**Decidido el 25 de octubre (Placas mandibulares, tras probarlas)**

| Tema | Decisión |
| --- | --- |
| Alcance del mordisco | Solo lo que se ve: el arco se mide desde la boca y llega hasta la punta de las placas dibujadas (unas 1,9 u en lugar de 4,6) |
| Guillotina | Sin triple de daño ni aturdimiento: cada cuarto mordisco sale una segunda dentellada más grande, con su animación, que llega más lejos (unas 3,25 u) |
| Quebrar | Lleva una marca sobre el enemigo mientras dura |
| Arte del mordisco | Rehecho: dos placas de hueso con la punta en gancho, como la mandíbula de *Dunkleosteus* |

**Decidido el 25 de octubre (almejas y mutaciones especiales)**

| Tema | Decisión |
| --- | --- |
| Sala de la almeja | En cada partida sale, aparte de las 2–4 especiales, una sala al azar entre la Gruta escondida, el Anillo de la sima y el Jardín de esponjas, que llevan siempre una almeja grande. En cualquier franja |
| Mutaciones especiales | Flagelos de Leanchoilia, Cutícula de Marrella y Yema clonal: solo salen de las almejas grandes, completan cualquier linaje como comodín y tienen el precio Inestable (+½ corazón por golpe y por especial). Tabla "Mutaciones especiales de las almejas" |
