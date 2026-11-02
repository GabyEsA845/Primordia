---
name: primordia-floor3-reactions-decisions
description: "Decisiones de diseño del paso 2 del piso 3 (Reacciones: fuego, metano, veneno): subtandas, quemado tipo Gungeon, explosión del odre, densidad"
metadata:
  node_type: memory
  type: project
  originSessionId: c2e7fcb9-3aa7-479c-9175-1715e21fb25a
  modified: 2026-11-02T17:58:46.697Z
---

Paso 2 del PLAN (feature/floor-3), preguntas hechas el 2026-11-02. Cuatro subtandas: A) fuego y helechos, B) metano (odres,
niebla, explosión, fogonazo), C) veneno (envenenado, charcos, plantas venenosas, nube tóxica), D) fumarolas y escenario vivo
(charcos de lodo/agua, musgo, bichitos, densidad por tramo).

**Fuego en el suelo:** dura ~4 s y se propaga por helechos y maleza a menos de 2 tiles cada ~0,6 s; un helecho quemado queda
como restos y no vuelve a arder. Enemigos de fuego y Escamas carbonizadas, inmunes.

**Quemado del jugador (idea de la usuaria, tipo Enter the Gungeon):** al pisar fuego aparece sobre su cabeza un icono de llama
que se llena como barra (1 s de fuego seguido la llena); al llenarse hace ½ corazón. La barra NO baja sola: al salir del fuego
con la barra a medias se queda y sigue subiendo despacio. Tras el daño la barra se vacía y vuelve a llenarse sola cada ~1,5 s
(otro ½ corazón cada vez) hasta esquivar. Cualquier esquiva (aunque la mutación cambie el dash) quita el 70–80 % de la barra,
así que a veces hacen falta 2 esquivas. Icono provisional dibujado por código; el arte definitivo va a la tanda de arte
(con el lodo del cuerpo y las puertas).

**Odre de metano:** explota con cualquier ataque: 6 de daño a enemigos y 3 (1,5 corazones) al jugador, prende lo inflamable y
enciende la niebla; queda el odre roto el resto de la sala.

**Densidad:** moderada y distinta por tramo (pantano: muchos helechos, 1–2 odres, 1 planta venenosa, algo de niebla; bosque
humeante: menos helechos, más odres y fumarolas; tierra quemada: casi sin helechos, muchas fumarolas y brasas).

**Why:** el paso es grande; cada subtanda se prueba antes de la siguiente.

**How to apply:** ver [[primordia-floor3-structure-decisions]] (estructura, IFlammable, F12) y [[primordia-mud-body-polish]].

**Añadidos tras probar la subtanda A (2026-11-02):** el lodo apaga el quemado casi de golpe (barra entera en ~⅓ s) y quita el
Burn a los enemigos que se hunden; quien arde (jugador con Burning, enemigo con Burn) prende los helechos y la maleza que toca
(radio 0,9, cada 0,25 s). **Futuro (paso 5, enemigos):** los enemigos más inflamables deben dejar un rastro de fuego mientras
caminan en llamas; definirlo al programar los enemigos. **Arte pendiente (tanda Opus):** llamas sobre el cuerpo del jugador
(rana, Diplocaulus, escorpión látigo) y sobre los enemigos que arden, y el icono definitivo de la barra de quemado; hoy el
jugador usa Fx_Burning centrado en el cuerpo y un icono de código, los enemigos Fx_Burning sobre la cabeza.

**Subtanda B (metano) HECHA y probada (2026-11-02):** odres sólidos que revientan con cualquier daño (6 enemigos / 3 jugador, radio
2,5, fuego 2 s en el centro, cadena), niebla fija con fogonazo en cadena (3 a enemigos, 1 al jugador y barra de quemado a la
mitad). Obstáculos de cobertura del piso 3 ya tienen arte (eran invisibles).

**Subtanda C (veneno), decisiones del 2026-11-02:** charco (12 s y se evapora): frena 30 %; a enemigos 1 de veneno/s durante 3 s;
al JUGADOR solo 1 de daño al inicio de cada ventana de 3 s y el mismo frenazo (la esquiva pasa por encima). Bulbo venenoso:
sólido, 1 golpe (o fuego/explosión), suelta charco grande y salpica, rebrota a los 25 s. Nube tóxica (fuego + charco): ~5 s;
enemigos 2 de veneno/s durante 4 s; jugador: barra verde sobre la cabeza que se llena en 2 s dentro de la nube (1 de daño al
llenarse, y vuelve a empezar), queda intoxicado 4 s (1 de daño cada 2 s) aunque salga; fuera de la nube la barra se vacía sola
en ~2 s y esquivar la vacía de golpe. Enemigos envenenados se tiñen de lima (~35 %).

**Subtanda C (veneno) HECHA y probada (2026-11-02)**; la barra de la nube se arregló (la nube avisa cada 0,1 s: ahora se llena
mientras llegue el aviso) y el icono de gota sigue visible durante la intoxicación; odres, bulbos, helechos y rocas guardan
separación mínima (radios compartidos por sala).

**Subtanda D (fumarolas y escenario vivo), decisiones 2026-11-02:** fumarola con ciclo ~6 s (reposo 4,5 s, aviso 1 s, chorro
0,5 s; 1 de daño al jugador y 3 a enemigos en la columna, solo vapor, sin fuego, fase al azar); charco de lodo frena 40 % (la
esquiva pasa por encima), charco de agua decorativo; cantidades distintas por tramo (pantano: varios charcos y musgo, 0–1
fumarolas; humeante: 1–2 fumarolas; quemada: 2–3 fumarolas, brasas y humo de tocones, sin musgo); todo lo decorativo incluido
(escarabajos que huyen, mosquitos sobre el agua, brasas, humo de tocones, musgo goteante).

**Subtanda D HECHA y probada (2026-11-02, fin de sesión).** Pedidos de la usuaria para después: el jugador debe tener efecto o animación al
pisar agua de charca (arte, ver [[primordia-floor3-art-batch-pending]]) y el agua también debe apagar el quemado, como el lodo (código
pendiente). Todo el paso 2 está hecho; lo siguiente es el paso 3 del PLAN.
