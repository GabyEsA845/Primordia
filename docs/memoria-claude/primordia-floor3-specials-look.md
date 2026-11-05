---
name: primordia-floor3-specials-look
description: "Aspecto de las mutaciones especiales del ámbar (decidido 2026-11-04): borde y brillo dorado ligado a Inestable, piezas como están, nivel III con animaciones propias; pendiente de trabajar con calma"
metadata:
  type: project
---

**Decisión de la usuaria (2026-11-04, Opus), PENDIENTE de diseñar y dibujar (se trabajará con calma, más adelante):**

- **Borde y brillo dorado:** cuando el jugador lleva una mutación especial, el cuerpo tiene un **ligero borde dorado** y **desprende brillo dorado** (ámbar común a todas; el brillo crece con el número de especiales, como Inestable). Probablemente shader + partículas, sin redibujar cuerpos.
- **Las piezas se quedan como están planeadas** (Cercos detrás, costra de la Cápsula en el lomo, glándula de la Gota; ver [[primordia-floor3-amber]]).
- **Nivel III con animaciones más vistosas:**
  - Cercos: brillos extra al hacer el quiebro.
  - Cápsula: aspecto más resistente + animación de regenerar la vida del jugador.
  - Gota: animación más vistosa y detallada al aturdir a un enemigo.
- **Posible mejora** de las animaciones y el diseño base de la Cápsula y de la Gota.

Descartado: estética totalmente distinta por especial. Por decidir: si las especiales del piso 2 llevan también el borde dorado.

**How to apply:** antes de programar 2c/2d (o al menos sus estados visuales), revisar esto; el código debe prever el brillo dorado según `PlayerMutations.SpecialCount` y los efectos del nivel III.

**Ronda de preguntas (2026-11-04, Opus):** borde = **contorno dorado de 1 px que late** (1 especial tenue; 2 más claro con pulso; 3 el brillo recorre el contorno; shader en Sonnet, Opus fija colores y referencia); brillo = **motas doradas que suben, más con cada especial** (1 alguna, 2 hilo constante, 3 motas + destellos en estrella); **bloque de ámbar rediseñado** como el de la sala del ámbar (volumen, facetas, borde claro, lo encerrado visible dentro; formación, grietas y estallido con trozos). Orden: **1) bloque base y gota → 2) nivel III de las tres → 3) borde y motas**, cada subtanda con muestra y aprobación. Arte en `docs/multimedia-temp/floor3-specials-look/`.
**Subtanda 1 DIBUJADA Y APROBADA (2026-11-04):** `floor3-specials-look/generar_especiales_v2.py` → `shell/`: bloque en 2 capas (Back detrás de la criatura, Front translúcida delante; el código pone la criatura en medio): `Fx_AmberShell_{Back,Front}_Form` (6), `Back_Hold` (1), `Front_Hold` (8), `Front_Crack` (3 fases), `Shatter` (8, 80×72), `Proj_AmberDrop` (4), `Fx_AmberDrop_Hit` (5); celda 48×56, pieza tallada octogonal. Sustituye a Fx_AmberBlock_*. Las salas Veta/Humo siguen EN REVISIÓN (la usuaria pasó a las especiales sin decir).
**Subtanda 2 (nivel III), respuestas (2026-11-04):** Cercos III = **estallido de chispas en estrella** en el quiebro; Cápsula III = **doble capa** (cáscara exterior gruesa y oscura con vetas doradas, núcleo cálido que late; las grietas solo rompen la exterior) + **pulsos cálidos y medios corazones que suben** (uno por medio curado, 3 en III); Gota III = **cristales que brotan del suelo y cierran el bloque, el enemigo se oscurece hasta silueta fósil** y el bloque queda más cristalino (escudo); en I-II, bloque normal con **estrellas de aturdido**.
**Subtanda 2 DIBUJADA Y APROBADA (2026-11-04):** `level3/`: `Fx_Cerci_Burst_III` (7, 56×56), `Fx_AmberShell3_{Back,Front}_Form` (8), `_Back_Hold` (8, núcleo que late), `_Front_Hold` (8, doble cáscara), `_Front_Crack` (3, solo la exterior), `Fx_CapsuleHeal` (12, corazoncitos en 1/5/9), `Fx_AmberCrystals_Erupt` (6), `Fx_AmberFossil_Front` (8), `Fx_AmberStun_Stars` (6, 32×16); fósil por shader (`#3a1a08` + borde `#e89a30`). README al día.
**PENDIENTE (otra sesión, Opus): subtanda 3** = contorno dorado de 1 px que late + motas doradas según el número de especiales (decisiones arriba). Nada de esto está importado: al programar 2c/2d (Sonnet), usar `shell/` y `level3/` en lugar de Fx_AmberBlock_* (ver README de floor3-specials-look).

**ARTE PENDIENTE (2026-11-05, la usuaria):** tras probar las 3 especiales del ámbar en juego, **no le gusta cómo quedaron los sprites** y los actualizará ella más adelante (no especificó cuáles: capas del cuerpo, bloque/efectos, iconos…; preguntar cuáles al retomarlo). El código no depende del aspecto: al cambiarlos basta repetir `Primordia > Floor 3 > Crear mutaciones especiales del ámbar` (re-importa iconos, capas y efectos desde `floor3-mutations/` y `floor3-specials-look/`). Sigue pendiente también la subtanda 3 (contorno dorado y motas).
