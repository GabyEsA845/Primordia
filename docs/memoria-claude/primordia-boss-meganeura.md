---
name: primordia-boss-meganeura
description: "Jefe Meganeura del piso 3 (libélula gigante): diseño del combate cerrado y ARTE TERMINADO (tandas 6d-6g, 2026-11-01) en floor3-bosses/meganeura/; generadores, hojas, reglas para el código"
metadata:
  node_type: memory
  type: project
  originSessionId: cf8e577c-459a-4153-bfc2-e53b953b2e8a
  modified: 2026-11-01T19:37:09.553Z
---

Jefe 2 del piso 3 ([[primordia-floor3-design]]), con mecánicas de escenario ([[primordia-boss-mechanics]]). Arte hecho y aprobado el 2026-11-01 en `docs/multimedia-temp/floor3-bosses/meganeura/` (README detallado con hojas, cuadros que cuentan, pivotes y tiempos). Sin importar a Unity (va con feature/floor-3).

**Combate (cerrado):** sala de **Tierra quemada, suelo 40×24 u**, cámara ×1,3. **Alto = solo su sombra, invulnerable; solo se le daña abajo.** Umbrales F1 100-65 / F2 65-30 / F3 30-0.
- **Picado:** marca sobre el jugador que lo sigue (ámbar) y luego se fija (roja) unos segundos → cae. Si se clava en un tronco/Sigillaria/Lepidodendron partido queda atrapada ~1,5 s (cabeza en la madera) y se arranca con astillas.
- **F1:** aleteo fuerte que empuja gas/veneno/proyectiles; si la prendes dentro de la niebla de metano cae ardiendo (le quedan agujeros quemados en las alas para siempre).
- **F2:** pone racimos de huevos (3-4 en 2-3 charcas); salen ninfas que persiguen y lanzan el labio (~2,5 u, te tira hacia ella); **hay que romper TODOS los huevos** para que baje furiosa: chillido con ondas y posada furiosa = ventana de daño.
- **F3 tormenta:** entrada con chillido, oscurece, viento con ceniza y **primer trueno espectacular junto al jugador** (destello de pantalla). Lluvia (apaga fuegos), aparecen charcos nuevos. **Dos tipos de rayo:** grande que sigue al jugador (señal azul que carga y se fija un instante) y varios sueltos marcados en el terreno. Tras el picado vuela mucho a ras de suelo para que caiga al menos un rayo seguro: el jugador lleva el rayo grande hasta ella → electrocutada, cae, boca arriba (vulnerable), se recupera. Corriente circular que se expande y se apaga en ~4 s; charca electrificada entera 5-6 s (mata ninfas). **Sigillarias quemadas = pararrayos de un solo uso** (se parten, arden, la lluvia las apaga). Relámpagos revelan su silueta en alto. Música caótica (audio pendiente).
- **Presentación:** sombra que cruza, se posa en el **Lepidodendron partido grande** de la sala, abre las alas con **reflejo azulado**, nombre. **Muerte:** se desprende un ala trasera, cae en espiral, se estrella en la charca, flota; la tormenta amaina (llovizna, vuelve la luz, vapor).
- Barra de vida **abajo de la pantalla** (el jefe vive arriba); **sin flecha de borde** (basta la sombra).

**Aspecto:** picada alta 55° (motor 3D del Arthropleura), 10 u de envergadura (celda 192, pivote = centro del tórax), 16 direcciones (9 + Flip X). Paleta C: cuerpo verde azulado con el dibujo de la referencia del usuario (`referencia_meganeura.png`: bandas oscuras, rayas ámbar en el tórax, ojos oscuros con reflejo rojo, patas oliva) y **alas puramente translúcidas** gris azulado con venas.

**Subtandas y generadores (todos con `python <gen> <orden>`):**
- 6d-0 escala `generar_escala_meganeura.py`; 6d cuerpo `generar_meganeura.py todo` (Fly, Hover, DiveWarn, Dive, Climb, Gust, Land, Perch, Lay, Hurt + Shadows/), `muestra`, `poses`, `solo <Anim>`.
- 6e-1 `variantes` (alas _Burnt, _Torn1, _Torn1Burnt, _Torn2, _Torn2Burnt; F2 = Torn1, F3 = Torn2); 6e-2 `golpes [Anim] [Var]` (Stuck, TearFree, Shocked, Fall, Grounded, Recover + capas _Burning); 6e-3 `momentos [Var]` (Intro, Screech, PerchRage, DeathFall, WingDetached, Dead). Efectos/escenario `generar_efectos_meganeura.py golpes|momentos|picado` (Lepido_Split_*, astillas, crash, ondas del chillido, salpicaduras, marcas del picado, impacto, grieta, Gust, WindDrag, Eggs/ racimos).
- 6f-1 ninfa `generar_ninfa_meganeura.py todo|muestra|shock` (Meganeura_Nymph_*: Idle, Move, Warn, Strike 112, Hurt, Death, DeathBurn, DeathShock).
- 6f-2 `generar_tormenta_meganeura.py rayos|ambiente` (Fx_Bolt_*, Fx_Storm_*, charcas _Electric/_Grow, Sigillaria_Struck/Burning/Smolder/Split) y `generar_meganeura.py silueta`.
- 6g barra `generar_interfaz_meganeura.py hojas|comparar`: versión **A «alas»** elegida por el usuario (las alas forman el marco), 300×32, **ventana 241×8 en (43, 12) pegada al tórax** (el usuario no quería hueco), rellenos Teal/Cloud (invulnerable)/Rage/Storm, marco que se rasga (sin quemaduras), muescas 65/30 %, Fx_EggBurst, Fx_BoltStrike, Fx_LowSparks (último 10 %: el código hace temblar el marco).
- Escenas: `generar_meganeura.py escenas6e <nombre>` (clavada, rayo, presentacion, furia, muerte, marca, aleteo, huevos, entrada, tormenta_rayos, silueta, final) → `vista_previa_6e_escena_*.png` + **GIF en `gif/escenas/`**.

**Reglas para el código (en el README):** la Meganeura volando o posada se dibuja **por encima de los enemigos del suelo** (su sombra en la capa del suelo); en la tormenta, oscuridad `#0C1428` alfa ~0,5 y destello `#E8F4FF` que baja en 0,25 s, y **todo lo que brilla (rayos, señales, charcas electrificadas, fuego, jefe electrocutado) va encima de la oscuridad y del destello**.

**Técnica:** las variantes de alas tardan ~1 h en segundo plano (muchos cuadros); la generación completa es lenta, lanzarla con run_in_background.

**CÓDIGO, paso 7a (2026-11-08, Sonnet):** `MeganeuraBoss` (Scripts/Enemies) + `DirectionalArt`/`DirectionalSprite` (hojas con 9 filas de dirección; `PlayBody("Perch", ms, loop)` elige `Meganeura_<Anim>_<Torn1|Torn2><Burnt>` con la sana de reserva) + `MeganeuraBossSetup`/`ArtSetup.BossMeganeura` (Editor). Cuerpo elevado: `Altitude` × 0,57 = subida en Y; sombra en el suelo (orden -3), cuerpo orden 30. El Lepidodendron partido (`Burnt/Lepido_Split_Idle`) lo crea el jefe en `SetArena` en centro + (8, 3); tórax posado a 3 u de altura (AJUSTAR a ojo). Presentación: sombra cruza 1,4 s desde la izquierda → Land → Intro; al acabar el rótulo, Perch en bucle. Ahora, tras la presentación, se queda posada y recibe daño (sin ataques): los ataques llegan en 7b-7d; muerte provisional con `DeathFall` (7e: espiral, ala, charca). Barra de vida: la lisa por defecto hasta 7e.
