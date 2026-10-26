# 3. Forma Zapatero de agua (Aguijón + Vejiga de tinta)

---

Te adjunto `generar_cuerpo_evolutivo.py`, el generador de pixel art del cuerpo evolutivo de mi juego *Primordia*, un roguelike con vista cenital en 3/4 que transcurre bajo el agua. El jugador es una célula verde que evoluciona: su cuerpo toma la forma de la mezcla de las 2 temáticas de sus mutaciones dominantes, en 3 versiones (1 sutil, 2 marcada, 3 plena). Todo está definido como datos: `THEMES`, `MATERIALS`, `FORMS`, `FORM_BY_THEMES`, `MUTATIONS`, `TRAITS` y `COMBOS`. Las mutaciones sin rasgo (`rasgo=None`) ya se toleran.

Necesito que **añadas al script** lo siguiente.

**Temática nueva `cefalopodo`** (Vejiga de tinta, linaje Depredador): rampas de 4 tonos en tinta azul noche y morado oscuro, con un brillo frío. Añade también una rampa de **cromatóforos**, unas manchitas rojizas y naranjas que se encienden y apagan como en la piel de un pulpo.

**Mutación nueva en `MUTATIONS`:** `InkSac` (Vejiga de tinta, ranura Órgano, temática `cefalopodo`, linaje Depredador), **todavía sin rasgo** (`rasgo=None`).

**Forma nueva `WaterStrider`** (Zapatero de agua oscuro), para `('insecto', 'cefalopodo')`, con `compartida=2`:
- **Versión 1 (sutil):** la avispa larvaria sutil, oscurecida por tinta en la parte de atrás, con 2 o 3 cromatóforos que laten en `Idle`.
- **Versión 2 (marcada):** un cuerpo esbelto y oscuro, en forma de quilla, con cabeza pequeña y ojos saltones, y unas patas finas y largas insinuadas, pegadas al cuerpo como remos. Los cromatóforos cambian de color al moverse.
- **Versión 3 (plena):** un zapatero de agua inquietante: cuerpo muy estilizado de tinta casi negra con reflejos morados, patas finas y larguísimas como remos y pequeños palpos curvados como tentáculos bajo la cabeza. La piel moteada de cromatóforos brilla y cambia.

Conserva los ojos claros del jugador y un poco de su verde. Aquí las patas finas sí forman parte de la forma (es lo que define al zapatero), pero mantén la masa del cuerpo centrada: la colisión del juego es un círculo de unos 13 px en el centro. Deja el anclaje `organ` en el centro del cuerpo, donde irá más adelante el rasgo de la Vejiga de tinta.

**Vista previa:** sustituye `COMBOS` por estas filas: "AGUIJON I + TINTA I", "AGUIJON I + TINTA III", "AGUIJON III + TINTA I" y "AGUIJON III + TINTA III".

Reglas:
- Respeta el estilo, el contorno `#181425`, la luz arriba a la izquierda y las proporciones y el lienzo de 32×32 de las formas que ya existen.
- **No me devuelvas el script entero.** Dame solo los bloques nuevos o cambiados, cada uno con una línea que diga dónde va.
- No generes imágenes, vistas previas ni GIF, y no ejecutes el script: lo ejecuto yo.
- Si necesitas un acabado nuevo (cromatóforos), escribe una función `m_…` y regístrala en `MATERIALS`.
