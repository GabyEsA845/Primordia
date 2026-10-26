---
name: primordia-floor2-design
description: "Diseño completo del piso 2 (océano cámbrico) cerrado el 2026-10-24 y estado de sus sprites; próximo paso: sprites de enemigos y luego el documento interactivo"
metadata:
  node_type: memory
  type: project
  originSessionId: 58a0d1fb-e8c9-417c-be8d-de83660bbe75
  modified: 2026-10-24T17:39:12.129Z
---

Sesión de diseño del piso 2 (2026-10-24), guiada tema a tema y **todos los temas cerrados**. Al final se monta un documento interactivo como el del piso 1 (https://claude.ai/artifact/1XpHC3PBSzqw1FGXMbScsL, fuente en docs/multimedia-temp/documento-linajes/: template.html + build.py) con las mismas secciones (Beneficios, Arma un cuerpo, Matriz, Mutaciones con niveles, Bestiario) **más una sección de Enemigos (sprites animados + comportamiento)**, y quizá las franjas de profundidad. Nada de esto está aún en el GDD: pasarlo al GDD/PLAN cuando se haga el documento (o en la rama del piso 2). Estamos en main, sin rama abierta; el trabajo de sprites está en docs/multimedia-temp (ignorado por git).

**Mutaciones (9, nivel I ≈ nivel II del piso 1):**
- Boca: Cnidocitos (proyectil → medusa; arpón atraviesa 1, 2 daño, 3/s, veneno 1/s 3 s · II atraviesa 2, veneno 2/s · III Ramillete: cada 4º disparo 3 en abanico); Placas mandibulares (cuerpo a cuerpo → pez acorazado; arco 110°, 5 daño, 2/s, Quebrar +25 % daño recibido 2 s · II daño 6, Quebrar 3 s · III Guillotina: cada 4º mordisco doble, triple de daño, aturde 0,5 s); **Corona de brazos** (área → lirio de mar: remolino en el cursor ≤6 u, 2 s, radio 1,6, atrae y 1 daño/0,4 s, 1/s · II radio 2, 2,5 s, cada 0,3 s · III Marea: estalla, empuja y 4 daño).
- Extremidades: Cola propulsora (I doble esquiva · II la 2ª deja 3 burbujas de 2 daño que empujan · III Torbellino: ambas dejan 5 burbujas de 3); Espinas de Hallucigenia (I 2 púas 4 s, 2 daño · II 3 púas 6 s · III Erizo: al desaparecer salen disparadas al enemigo más cercano).
- Piel: Placas de trilobite (+1 corazón, rebota 1 de 4 proyectiles · II 1 de 3 · III Enrollarse: en la esquiva rebota todos); Escleritos de Wiwaxia (cada 9 s 3 escamas de 2 daño · II cada 7 s, 5 · III **Órbita**: giran 2 s a tu alrededor destruyendo proyectiles antes de salir; la mejor defensa del piso).
- Órgano: Saco de crías (2 larvas 8 s en todos los niveles, recarga 14 · II 3 larvas · III Enjambre: la que mata se divide); Ojos compuestos (3 s todo al 60 % salvo tú, recarga 12 · II 4 s · III Visión total: +30 % de daño).

**Linajes:** clases = catálogo que atraviesa la partida (duermen y vuelven, se remodelan por piso); animales propios de cada piso; ≥1 clase nueva por piso. Los linajes del piso 1 no pueden seguir en el 2 (Boca nueva obligatoria): la regla del GDD "útiles un piso por encima" queda obsoleta. Clases del piso 2: Carnívoro (Rematar +100 % a heridos ≤50 %, +50 % jefe; aturdidos y con Quebrar cuentan como heridos), Herbívoro (Pastar: cada 12 de biomasa medio corazón; con vida llena hasta 2 medios corazones verdes), Venenoso (Toxina: envenenados +50 % daño y −20 % velocidad), **Carroñero, nueva** (Carroña: restos 4 s al morir un enemigo; tocarlos +20 % daño 5 s, acumula ×3; color marrón arena). Hongo duerme (vuelve en el pantano).

| Linaje | Clase | Boca | Extremidades | Piel | Órgano | Toque |
|---|---|---|---|---|---|---|
| Placodermo | Carn | Placas | Cola | Trilobite | Ojos | Rematar con mordisco cura ½ corazón (máx. 1 cada 6 s) |
| Opabinia | Carn | Placas | Hallucigenia | Wiwaxia | Ojos | Ojos +1 s; cada remate durante ellos +0,5 s |
| Burgessomedusa | Ven | Cnidocitos | Cola | Wiwaxia | Saco | Las larvas envenenan |
| Anémona | Ven | Cnidocitos | Hallucigenia | Trilobite | Saco | Pisar tus púas envenena |
| Lirio de mar | Herb | Corona | Cola | Trilobite | Saco | El remolino atrae la biomasa |
| Wiwaxia | Herb | Placas | Cola | Wiwaxia | Saco | Tanda de escleritos que alcanza a ≥3 da ½ corazón verde |
| Trilobite | Carr | Placas | Hallucigenia | Trilobite | Ojos | Tocar restos: el siguiente proyectil rebota |
| Hallucigenia | Carr | Corona | Hallucigenia | Wiwaxia | Ojos | Púas que matan dejan restos dobles |
8 animaciones de evolución nuevas (célula → silueta del animal, como en el piso 1).

**Enemigos (5):** Trilobite (embiste tras aviso 0,6 s, aturdido 1 s panza arriba al chocar, frente blindado ½ daño, 14 vida, coste 2), Medusa enemiga (**azul translúcido**, distinta de la del jugador, que es rosa; anillo de 8 cada 3 s con hueco que gira, 10, coste 2 desde sala 2), Ottoia (gusano enterrado e inmune, onda en la arena + círculo 0,8 s, muerde y se hunde, 9, coste 3 desde sala 2), Esponja Vauxia (inmóvil, cura 2 y da escudo en 4 u cada 3 s, 16, coste 3 desde sala 3, máx. 1 por sala), Cardumen de Pikaia (5 de 3 vida, pasadas en formación, coste 2).

**Jefes (300 de vida los dos, uno al azar):**
- Anomalocaris: sala muy grande y **oscura** (sin cegar del todo) con **plantas luminosas** que dan algo de visión (luces 2D de URP) y columnas de algas que tapan sin bloquear. F1 acecho (zarpazo con aviso 0,7 s y 1 s vulnerable, estela de burbujas), F2 cerco en espiral, **cinemática al 25 %** (solo sus ojos nadando en la oscuridad, inmune), **F3 frenética**: se para, ruido de carga y **marcador 0,8→0,5 s del área y trayectoria de sus dos zarpazos**, que luego son casi inesquivables; pausa corta tras ellos.
- Euriptérido: cueva con rocas; F1 acorazado (pinzas, abanico de 3 púas venenosas, frente ½ daño); **cinemática de muda** (~3 s) a mitad; la muda vacía queda como cobertura; F2 +25 % daño recibido, doble de rápido, barrido de cola 180° con veneno, doble pinza.

**Pendientes (cerrados):** mejoras de mutaciones del piso 1 ya resueltas con las reglas actuales (quitar de "Mejoras a futuro"); piezas en cuerpos: Boca en el cuerpo, Extremidades y Piel como capas (4 × 3 cuerpos = 12 capas por piso), Órgano sin pieza pero con **efectos de activación vistosos**, mutaciones de pisos anteriores no se dibujan; números: 8–10 salas, presupuesto 4 +1,6/sala, recompensas en salas 2/4/6/8, 2–3 laterales; **música y sonidos del piso 2 para después**.

**Salas del piso 2:** 3 franjas de profundidad por avance (Someras turquesa con rayos de luz → Arrecife = Cambrian → Abismo casi negro con bioluminiscencia, hasta el jefe), pasillos que mezclan franjas, nieve marina más densa al bajar, camino que tiende a bajar en la cuadrícula; **cuevas irregulares generadas por semilla** (método de la sala de paso + reparación + rocas/coral de cobertura, catálogo a mano de reserva; tienda, sala de cambio y jefes a mano). El piso 1 sigue con salas regulares (quizá más adelante).

**Escala del arte (decidido 2026-10-24):** se mantiene 16 px por unidad en todo el juego; el detalle viene de que **las criaturas crecen piso a piso** (célula 16, océano 24, siguientes ~32; jefes grandes), paletas, más cuadros, efectos e iluminación. Nunca mezclar tamaños de píxel. Enemigos del piso 2: ~24 px, Pikaia ~12, Esponja ~28.

**Sprites hechos (docs/multimedia-temp/floor2-bodies/):** lirio de mar completo en generar_cuerpos_piso2.py (carpeta Crinoid/, cáliz dorado + **10 brazos finos morados sin contorno**, ataque 6 cuadros, muerte que se fosiliza) y remolino Effects/Fx_ArmCrown_Whirl.png; iconos de las 9 Bocas/mutaciones en generar_iconos_piso2.py (Icons/, Corona en morado). Aprobados por el usuario.

**Enemigos (2026-10-24, APROBADOS por el usuario):** docs/multimedia-temp/floor2-enemies/ con generar_enemigos_piso2.py y README (Trilobite, BlueJelly, Ottoia con pivote (0,5; 0,25), Vauxia, Pikaia + efectos de medusa y esponja).

**Jefes (2026-10-24):** docs/multimedia-temp/floor2-bosses/ (base_piso2.py + generar_anomalocaris.py + generar_euripterido.py, README). Anomalocaris y Euriptérido APROBADOS. Anomalocaris (lienzo 112). Euriptérido (lienzo 128, ~90 px): el usuario pidió cuerpo más largo de una pieza con la cola, pinzas curvas y puntiagudas con pequeña abertura en reposo, y eligió la **opción B: las púas venenosas de la fase 1 salen del caparazón (púas de los segmentos que se erizan), no de la cola**; la cola se estrena en la fase 2. La muda deja al jefe 1 u adelante y la Exuvia en la posición original.

**Evolución de linajes (2026-10-24, APROBADA):** docs/multimedia-temp/floor2-lineages/ (generar_evolucion_piso2.py, README): 64×64, 12 cuadros como el piso 1, parte del cuerpo de la Boca del linaje; Carroñero #4a3420/#8a6a40/#c8a46a/#f2e0b4.

**Pendiente de arte:** sprite de los restos de Carroña (clase Carroñero), con el resto del arte de beneficios del piso 2.

**Tilesets (2026-10-24, APROBADOS):** environment/generar_temas_piso2.py → temas Shallows y Abyss (+ formas) y obstáculos del Arrecife (Cambrian), Ocean/Particle_MarineSnow; README del escenario ampliado. Decisiones del usuario: Abismo oscuro pero sin tapar la vista (bioluminiscencia con luces 2D suaves; la oscuridad real solo en la sala del Anomalocaris); Someras con cáusticas en el suelo + rayos diagonales encima.

**Puertas del piso 2 (2026-10-24, APROBADAS):** el usuario pidió mejorarlas (solo piso 2; el piso 1 conserva la membrana) y eligió **losas de roca**: environment/generar_puertas_piso2.py → Door_{Top,Side,Bottom}_{Close,Open} en Shallows/Cambrian/Abyss; bloques con la paleta de la pared y sello que brilla en la costura. Al integrar: `Door` debe aceptar cuadros de abrir propios y duración por cuadro.

**Documento interactivo del piso 2 (2026-10-24, APROBADO):** https://claude.ai/artifact/1Qdqi5ymyBEhETWY8MKmnm, fuente en docs/multimedia-temp/documento-piso2/ (template.html + build.py → piso2.html). Secciones: Beneficios, Arma un cuerpo (muestra el cuerpo por la Boca y la evolución), Matriz, Mutaciones (con los 3 cuerpos), Enemigos, Bestiario, El océano (franjas y puertas). Nombres de los toques propuestos por mí: Festín, Cinco ojos, Pólipos urticantes, Lecho urticante, Corriente nutritiva, Cosecha, Coraza de carroña, Doble ración. Detalle completo del diseño original: transcripción 58a0d1fb (mensajes 94-103).

**GDD/PLAN (2026-10-24):** diseño pasado al GDD, PLAN (ramas floor-2-structure, floor2-mouths, floor2-enemies, floor2-mutations, boss-anomalocaris, boss-eurypterid, floor2-audio; exchange-room ya estaba) y MULTIMEDIA en main (commit 43139fd, merge d798aa3, 2026-10-24).

**Siguiente:** sesión de diseño de la estructura de salas del piso 2, ver [[primordia-floor2-rooms]]; luego `feature/floor-2-structure` → siluetas/evolución de los 8 linajes → tilesets Someras y Abismo (+ rocas/coral) → vista previa de cada tanda al usuario → montar el documento interactivo (Artifact, reutilizando build.py del piso 1) → pasar el diseño al GDD/PLAN. Después, ramas: feature/floor-2-structure, floor2-mouths, floor2-enemies, floor2-mutations (+ capas), exchange-room, boss-anomalocaris, balance. Ver [[primordia-floor-progression]], [[primordia-workflow]].
