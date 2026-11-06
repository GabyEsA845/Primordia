---
name: primordia-floor3-drop-redesign
description: "Rediseño de arte de la Gota de ámbar (piso 3), 2026-11-06 con Opus: ARTE CERRADO Y CON COMMIT (bloque C en 2 tallas, formación, grietas, rotura, estrellas, disparo, impacto, cristal del lomo, nivel III en ámbar antiguo); falta importar y programar (Sonnet)"
metadata:
  type: project
---

**Estado (2026-11-06, rama feature/floor-3, sesión con Opus; arte en `docs/multimedia-temp/floor3-drop/`).** Tras cerrar el arte de la Cápsula ([[primordia-floor3-capsule-redesign]]), la usuaria pasa a la **Gota de ámbar** (mecánica en [[primordia-floor3-amber]]; arte anterior en `floor3-specials-look/shell|level3` y `floor3-mutations/Special/Special_AmberGland_*`).

**Decisiones de la usuaria:**
- Se rehace **todo**: bloque sobre el enemigo (grietas, rotura, aturdido), disparo e impacto, glándula en el cuerpo y nivel III (cristales, fósil, cadáver-escudo).
- Qué fallaba: forma geométrica (octógono), poco ámbar (quiere la textura de la Cápsula nueva: borde rojizo, centro translúcido, vetas), demasiado grande, efectos flojos.
- **Forma del bloque:** parecida a la Cápsula, pero en vez de gota, **un rectángulo con bordes irregulares, como si se hubiera formado desde abajo**.

- **Forma elegida: C «agujas»** (propuestas A escalonado y B estratos descartadas; `floor3-drop/generar_bloque.py`): bloque bajo con puntas de cristal arriba y pinchos que suben pegados a los lados, textura de la Cápsula, ~35×20 px para el escarabajo.
- **2 tallas dibujadas, sin escalar:** normal (escarabajo, sapo, araña, voladores) y grande (plantas); el código elige la más cercana (hay que cambiar el escalado de `AmberShell` para la Gota).
- **Bloque C APROBADO** en 2 tallas (`generar_bloque_final.py` → `bloque/Fx_AmberDropBlock_{Normal,Large}_{Back,Front}`; normal 48×40 pies y=34 eje 24; grande **56×68** pies y=62 eje 28; se estrechó a 39 px tras medir bien las plantas: las celdas de Medullosa y Calamites son de 32 px de ancho, no cuadradas).
- **Formación:** sube de abajo arriba como la Cápsula (superficie brillante), **rápida ~0,5 s** (5 × 70 ms subida + 2 × 90 ms remate).
- Formación APROBADA (`generar_formacion.py` → `formacion/`).
- **Grietas en 3 fases** (el código las cambia según el tiempo que queda), **rotura con esquirlas disparadas hasta el radio de daño** (~2,2 casillas), **aturdido = 3 estrellas de ámbar** girando sobre el bloque.

- **Grietas, rotura y estrellas APROBADAS** (2026-11-06): `generar_rotura.py` → `grietas/Fx_AmberDropBlock_{talla}_Crack` (3 fases, encima de Front), `rotura/Fx_AmberDropBlock_{talla}_Break_{Back,Front}` (7 cuadros 96×96, centro del bloque en (48,48), 90+60×6 ms; cuadro 0 quiebre, **cuadro que cuenta = 1** (daño), anillo de polvo a 35 px = radio de daño, trozos que se posan en el suelo), `aturdido/Fx_AmberDropStun_Stars` (6 × 32×16, 100 ms, sobre la punta).

- **Disparo = lágrima de ámbar con estela corta de goterones; impacto = salpicadura** de resina con goterones que caen alrededor del enemigo, de ahí sube el bloque.
- **Disparo e impacto APROBADOS** (color retocado para parecerse al bloque: borde rojizo, interior translúcido, núcleo cálido): `generar_disparo.py` → `disparo/Proj_AmberDrop` (4 × 24×12, 70 ms, pivote (17,6)) y `Fx_AmberDrop_Hit` (6 × 32×32, 60 ms, centro en el enemigo; **la formación empieza en el cuadro 2**). Escena del flujo completo APROBADA: `generar_escena.py` → `gif/escenas/escena_gota_{Normal,Large}.gif`. Estrellas: celda colocada con su borde de arriba 12 px sobre la punta más alta del bloque.
- **Glándula:** estados = **3 niveles (vacía, a medias, llena) + cargando**. Ideas RECHAZADAS: buche que se llena, gotas en el lomo, bolita redibujada, **gema en la frente** (muestra `muestra_gema.py`); no elegidas: gota en la boca, anillo. Quiere un detalle pequeño, reconocible a simple vista y que no sature al personaje.
- **Glándula = CRISTAL EN EL LOMO, APROBADA** (2026-11-06): `generar_glandula.py` → `glandula/Special_AmberGland_{Empty,Half,Full,Charge}_{cuerpo}_{anim}_{dir}` (168 hojas, lienzo 56×56 por cuadro como las capas de floor3-mutations; ancla `back`, de frente en Diplocaulus/vinagrillo va en lo alto del tronco). Full destella en Idle cuadro 1; Charge vibra y suelta motas. Sustituye a `Special_AmberGland_{Rest,Full}_*`.
- **Nivel III:** bloque-cadáver en **ámbar antiguo** (misma forma, paleta más oscura y rojiza, vetas doradas, enemigo fosilizado por shader); al formarse, **destello fuerte + fosilizado en ~0,5 s** (shader) mientras el ámbar se oscurece.

**Datos para dibujar:** enemigos del piso 3 anchos y bajos (escarabajo 26×13, sapo 28×18, araña 23×17; plantas hasta 44×46). Hoy `AmberShell` escala el bloque al enemigo (`DrawnBody` 2,3×2,9 u, ×1,15, entre 0,7 y 3): un escalado no entero rompe el tamaño de píxel → preguntar si se dibujan tallas.

**ARTE CERRADO (2026-11-06):** todo aprobado; tabla de hojas, pivotes y cuadros que cuentan en `floor3-drop/README.md`. SIGUIENTE (Sonnet): importar y programar (ver «Pendiente» del README: elegir talla en vez de escalar `AmberShell`, fases de grietas, glándula de 4 estados, fosilizado por shader al 75 %).

**Orden que se siguió:** forma (propuestas) → formación desde abajo → grietas/rotura → aturdido → disparo/impacto → glándula → nivel III → README, memoria y commit.
