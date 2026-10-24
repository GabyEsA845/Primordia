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
- **Cada linaje pertenece a una clase**, y la clase decide la base de su beneficio. Además, cada animal suma un **toque propio** pequeño que encaja con sus mutaciones. El beneficio es fijo (no crece con los niveles de las mutaciones) y dura mientras lleves las 4, también al cambiar de piso.

| Clase | Estilo | Beneficio base |
| --- | --- | --- |
| Carnívoro | Ataque y caza | **Rematar:** +100 % de daño a los enemigos heridos, los que tienen la mitad de su vida o menos (+50 % contra el jefe) |
| Herbívoro | Aguante | **Pastar biomasa:** cada 10 de biomasa recogida cura medio corazón. Con la vida llena, da medio corazón verde temporal que absorbe un golpe (como mucho uno a la vez) |
| Venenoso | Daño en el tiempo y estados | **Toxina débil:** los enemigos envenenados reciben +50 % de daño de cualquier fuente |
| Hongo | Área y propagación | **Colonizar la sala:** al entrar en una sala de combate brotan 3 matas de hongo al azar en el suelo (radio 1,2) que ralentizan un 40 % y quitan 1 por segundo, como el micelio; desaparecen al limpiar la sala |

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

- **Pisos siguientes:** sus mutaciones completan linajes nuevos (Lobo, Raptor, Mamut...) y pueden sumarse a los del piso 1. Ahí entran más herbívoros, que en el piso 1 solo tienen la Tortuga porque la única Boca que tritura son las Mandíbulas.
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
- **Cuántas:** 8 por piso por ahora (2 por ranura, con sus linajes); la idea es llegar a 12 (3 por ranura) cuando el arte lo permita. Con 3 Bocas por piso hay una por familia de arma.
- **Escalones de poder:** el nivel I de una mutación del piso N+1 vale como el nivel II de una del piso N (del III del piso N al III del piso N+1 se gana un escalón).
- **Dificultad:** la vida de los enemigos sube ×1,5–2 por piso. El resto de la dificultad viene de más enemigos por sala y patrones más exigentes, no de esponjas de vida.
- **Linajes entre pisos:** un linaje debe seguir funcionando un piso por encima del suyo y quedarse flojo dos pisos por encima (los del piso 1 sirven en el 2, pero rinden mucho menos en el 3). Al cambiar de piso pueden ajustarse los valores de mutaciones y linajes, sin cambiar nada visual.
- **Pendiente** (en "Mejoras a futuro" del plan): cómo seguir consiguiendo mejoras de las mutaciones del piso 1 en pisos posteriores.

Borrador de ideas para los pisos 2 a 6 (una por ranura; faltan las demás hasta llegar a 8 por piso). Cada Boca pertenece a una de las 3 **familias de arma**, que deciden la forma del jugador en ese piso (ver "Forma del jugador en cada piso").

| Piso | Mutación | Ranura | Familia | Efecto (idea) |
| --- | --- | --- | --- | --- |
| 2 | Cnidocitos | Boca | Proyectil | Arpones urticantes que atraviesan a un enemigo y envenenan |
| 2 | Cola propulsora | Extremidades | | Dos esquivas seguidas antes de la recarga |
| 2 | Placas de trilobite | Piel | | +1 corazón; refleja 1 de cada 4 proyectiles que te golpean |
| 2 | Saco de crías | Órgano especial | | Suelta 2 larvas aliadas que persiguen y muerden durante 10 s |
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

**Jefe del piso 1 (virus gigante):** 120 de vida, 3×3 unidades, avanza despacio hacia el centro, medio corazón por contacto. Fase 1: espiral de un brazo y ráfaga circular de 12 proyectiles cada 4 s. Fase 2 (desde el 50 %): espiral de dos brazos más rápida, ráfaga de 16 e invoca 2 bacterias cada 8 s (máximo 4 vivas).

**Segundo jefe del piso 1 (ameba gigante):** 180 de vida, unas 4,3 unidades (más grande que el virus), rosa pálido, medio corazón por contacto en todas las fases salvo agotada. En cada partida sale el virus o la ameba, al 50 %. Su sala es redonda (30 tiles de diámetro, borde escalonado), vacía y en azul marino para que contraste con ella. En las fases 1 y 2 no dispara: embiste en línea recta hasta chocar con una pared (se aplasta contra ella) y se queda quieta. Antes de volver a salir avisa durante 0,6 s: tiembla y mira hacia donde irá (la cara entera se desplaza hacia allí); al final se encoge para coger impulso y sale disparada ya a toda velocidad, en la dirección fijada al acabar el aviso. No empuja al jugador: lo atraviesa y le quita medio corazón al tocarlo.
- **Calma (100–80 %):** 1,2 u/s, sale hacia cualquier lado y para 1,5 s. Parece inofensiva.
- **Enojo (80–50 %):** pequeña animación de enfado; desde entonces sale hacia el jugador a 6 u/s (el triple, tan rápida como el jugador) y para 1 s.
- **División (50–10 %):** se parte en dos amebas medianas (60 % del tamaño) que comparten una sola barra de vida. Nada más nacer salen disparadas hacia lados opuestos de la sala; después embisten a 10 u/s: una va directa al jugador y la otra le anticipa (apunta adonde estará cuando llegue, según su dirección y velocidad). Si chocan entre sí, rebotan.
- **Final (10 %):** antes de ese golpe la vida no baja del 10 %. Escena en la que el jugador no puede hacer nada (franjas de cine, sin HUD salvo la barra del jefe, proyectiles fuera): la cámara va despacio al centro de la sala (2 s), las medianas se juntan allí (3 s) y la ameba se enfurece (2 s). Después, inmune (barra gris con candado), llena la sala de proyectiles durante 30 s con la cámara alejada para ver la sala entera, en 3 tramos de 10 s: anillos densos con un hueco que gira; brazos que giran en los dos sentidos y forman una red; y anillos más brazos más abanicos apuntados al jugador. No hay contador: el avance se nota en su animación. Al acabar queda agotada en el centro (pálida, barra roja que palpita) y cae de dos golpes.

## Pisos y etapas evolutivas

El juego completo tiene **6 pisos**, cada uno una etapa de la vida en la Tierra, con enemigos, jefe y mutaciones propios. La v1.0 sale con los pisos 1 a 3 (ver "Alcance").

**Tamaño de los pisos:** las partidas pueden durar de 40 minutos a 1 hora, como en *Enter the Gungeon*. El piso 1 sigue con 4–5 salas de combate y 1–2 laterales; desde el piso 2, 8–10 salas de combate en el camino (más las laterales, la tienda y la sala de cambio).

Cada piso tiene **2 jefes posibles** y en cada partida sale uno al azar.

| Piso | Etapa | Ambientación | Enemigos | Jefes (uno al azar) |
| --- | --- | --- | --- | --- |
| 1 | Sopa primordial | Charco microscópico, colores verdes y burbujas | Bacterias que persiguen, amebas que se dividen al morir | Virus gigante que dispara en espiral · Ameba gigante que se divide |
| 2 | Océano cámbrico | Fondo marino, corales y luz azul | Trilobites que embisten, medusas que flotan y disparan | Anomalocaris (depredador con garras) · Euriptérido (escorpión marino gigante) |
| 3 | Pantano carbonífero | Pantano con helechos gigantes y niebla | Libélulas gigantes que caen en picado, anfibios que escupen | Artrópodo gigante (estilo Arthropleura) · Meganeura (libélula gigante) |
| 4 | Desierto pérmico | Dunas rojas, rocas y huesos | Lagartos que corren en zigzag, sinápsidos que lanzan rocas | Dimetrodon con vela dorsal que se calienta (fase 2 más rápida) · Scutosaurus (acorazado que embiste) |
| 5 | Selva de dinosaurios | Selva densa, helechos y volcanes al fondo | Compsognathus en manada, pterosaurios en picado, plantas carnívoras que disparan | Tiranosaurio · Espinosaurio |
| 6 | Edad de hielo | Tundra nevada, suelo helado que resbala | Lobos en manada, mamuts que embisten, cazadores que lanzan lanzas | Tigre dientes de sable · Tribu de humanos (el último depredador) |

**Paso al piso siguiente**

1. **Burbujas del jefe:** al morir, el jefe deja burbujas de mutación de las ranuras que no son la Boca; una de ellas es siempre la mejora de una mutación equipada.
2. **Sala de paso:** junto a la sala del jefe hay otra que solo se abre al vencerlo. En ella está el paso al piso siguiente. Al usarlo se abre una ventana, más vistosa que la elección de mutación, con las Bocas del piso siguiente (una por familia), y elegir una es obligatorio: nadie llega al piso siguiente sin Boca.
3. **Nivel de la Boca nueva:** empieza en el nivel I. Para no castigar a quien subió su Boca, los niveles que tenía por encima del I se reparten al azar, uno a uno, entre las demás mutaciones equipadas que aún puedan subir. Los que no quepan en ninguna se pierden (no vuelven a la Boca): así no compensa llegar al jefe con solo la Boca mejorada.
4. **Metamorfosis:** cinemática en la que el jugador se transforma en la criatura del piso siguiente según la familia de la Boca elegida, y el juego pasa al piso nuevo.
5. **En el piso siguiente** no se ofrecen otras Bocas (ni en las elecciones ni en la tienda), pero sí las mejoras de la que llevas.

**Sala de cambio (el laboratorio de Darwin)**

Una por piso desde el piso 2, siempre pegada a la tienda (así es fácil de encontrar). En el centro hay una cápsula llena de líquido; al acercarse y pulsar E se abre su propia interfaz, distinta de las demás:

- Muestra al jugador y lo que lleva en cada ranura, como el panel de la criatura de la pausa.
- Al elegir la mutación de una ranura, aparecen las mutaciones del piso actual de esa ranura por las que se puede cambiar, cada una con su dibujo y su descripción, **al mismo nivel** que la que entregas. Se elige una y se confirma con Aceptar: la tuya desaparece y la nueva entra con ese nivel.
- **Gratis y solo un cambio por piso**, así que hay que pensarlo bien. Es la única forma de cambiar de Boca (y de cuerpo, si cambia la familia; ver "Forma del jugador en cada piso").
- No existe en el piso 1, donde las Bocas salen en las recompensas normales.

**Forma del jugador en cada piso**

Para que el arte sea abarcable, el cuerpo del jugador **no** combina mutaciones: cada piso tiene su criatura en **3 variantes, una por familia de arma**, y las demás mutaciones siguen siendo piezas sencillas encima del cuerpo.

- **Familias de arma:** *Cuerpo a cuerpo* (Mandíbulas, Colmillos), *Proyectil* (Aguijón, Cnidocitos, Escupitajo venenoso) y *Área* (Esporas, Escupitajo ácido, Trompa). Sin mutación de Boca se usa la variante *Proyectil*, porque el disparo base es un proyectil.
- **Cuándo cambia:** al elegir la Boca en la sala de paso tras el jefe (ver "Paso al piso siguiente"), con una cinemática de metamorfosis. En el piso siguiente no se ofrecen Bocas, así que el cuerpo y el arma siempre se corresponden. La única forma de cambiar de Boca es la sala de cambio; si la nueva es de otra familia, hay otra metamorfosis y cambia el cuerpo (si es de la misma, solo cambia la pieza del arma).
- **Mientras un piso tenga 2 Bocas** (8 mutaciones), solo hay 2 familias y 2 cuerpos en ese piso; el tercero llega con la tercera Boca. En el piso 2: Cnidocitos (proyectil → medusa) y una Boca cuerpo a cuerpo por diseñar (→ pez primitivo con mandíbulas); el lirio de mar (área) llegará con la tercera.
- **Piso 1:** la célula actual con sus piezas (ya hecha), sin variantes.
- **Piezas del resto de ranuras:** Extremidades, Piel y Órgano son capas sencillas colocadas en unos pocos puntos fijos de cada cuerpo (lomo, costados, centro). En los pisos 2 a 6 la Piel deja de sustituir al cuerpo y pasa a ser una capa encima (por ejemplo, el caparazón en el lomo).
- **Arte:** 15 cuerpos nuevos (pisos 2 a 6 × 3 familias), cada uno con las animaciones del jugador (reposo, moverse, esquiva y muerte en 3 direcciones).

| Piso | Cuerpo a cuerpo | Proyectil | Área |
| --- | --- | --- | --- |
| 1 | Célula | Célula | Célula |
| 2 | Pez primitivo con mandíbulas | Medusa | Lirio de mar que suelta nubes |
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
- [ ] Sala de cambio (el laboratorio de Darwin), desde el piso 2
- [ ] Piso 2 (Océano cámbrico): escenario, 2 o 3 enemigos, Anomalocaris, 4 mutaciones y los 3 cuerpos del jugador
- [ ] Piso 3 (Pantano carbonífero): lo mismo, con el artrópodo gigante
- [x] Tienda con biomasa

**Más adelante**

- [ ] Pisos 4 a 6
- [ ] Salas secretas
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

- [ ] Revisar los borradores: criaturas del jugador, enemigos y jefes de los pisos 2 a 6, y las 20 mutaciones nuevas (con sus niveles II y III).
- [ ] Ataques y valores de los segundos jefes de los pisos 2 a 6.
- [ ] Si el cuerpo crece de un piso a otro (y con él la colisión) o mantiene el tamaño.
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
