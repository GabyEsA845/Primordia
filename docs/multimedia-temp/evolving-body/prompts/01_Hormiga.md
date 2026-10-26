# 1. Forma Hormiga (Aguijón + Patas articuladas)

---

Te adjunto `generar_cuerpo_evolutivo.py`, el generador de pixel art del cuerpo evolutivo de mi juego *Primordia*, un roguelike con vista cenital en 3/4 que transcurre bajo el agua. El jugador es una célula verde que evoluciona: su cuerpo toma la forma de la mezcla de las 2 temáticas de sus mutaciones dominantes, en 3 versiones (1 sutil, 2 marcada, 3 plena). El script ya genera las formas `WaspLarva` (insecto) y `Firefly` (insecto + eléctrico) y los rasgos `Stinger` y `ShockGland`. Todo está definido como datos: `THEMES`, `MATERIALS`, `FORMS`, `FORM_BY_THEMES`, `MUTATIONS`, `TRAITS` y `COMBOS`.

Necesito que **añadas al script** lo siguiente.

**Temática nueva `artropodo`** (Patas articuladas, linaje Acorazado): rampas de 4 tonos en grises azulados fríos de placa, con brillo metálico apagado. Toma como referencia los grises azulados de las Patas y del Caparazón que ya hay en los generadores.

**Mutación nueva en `MUTATIONS`:** `JointedLegs` (Patas articuladas, ranura Extremidades, temática `artropodo`, linaje Acorazado), **todavía sin rasgo** (`rasgo=None`). Asegúrate de que `resolve()`, `compose()` y la vista previa toleran mutaciones sin rasgo: simplemente no dibujan nada encima.

**Forma nueva `Ant`** (Hormiga), para `('insecto', 'artropodo')`, con `compartida=2` (formato reducido: esquiva y muerte una sola vez, sin `Hurt`):
- **Versión 1 (sutil):** la avispa larvaria sutil, pero el cuerpo empieza a estrangularse en dos lóbulos (cabeza y gáster). Antenitas acodadas y unos muñones de patas apenas insinuados en los costados. Los grises azulados entran como placas pequeñas sobre el verde.
- **Versión 2 (marcada):** tres partes claras: cabeza redonda, tórax estrecho y gáster en gota, unidas por una cintura fina (pecíolo). Mandíbulas pequeñas, antenas acodadas más largas y placas de quitina gris azulada con juntas oscuras.
- **Versión 3 (plena):** una hormiga soldado robusta. Cabeza grande y acorazada con mandíbulas marcadas, ojos compuestos y antenas acodadas largas. Tórax con placas azuladas y reflejo ámbar en las juntas, cintura muy fina y gáster brillante. Postura firme y pegada al suelo.

Que las tres versiones conserven los ojos claros del jugador y un poco de su verde, cada vez menos. **No dibujes patas largas en el cuerpo**: las pondrá más adelante el rasgo de las Patas articuladas en los anclajes `left` y `right`. Deja esos anclajes en los costados del tórax.

**Vista previa:** sustituye `COMBOS` por estas filas: "AGUIJON I + PATAS I", "AGUIJON I + PATAS III", "AGUIJON III + PATAS I" y "AGUIJON III + PATAS III".

Reglas:
- Respeta el estilo, el contorno `#181425`, la luz arriba a la izquierda y las proporciones y el lienzo de 32×32 de las formas que ya existen.
- **No me devuelvas el script entero.** Dame solo los bloques nuevos o cambiados, cada uno con una línea que diga dónde va (por ejemplo, "añadir al final de `FORMS`" o "sustituir la función `resolve`").
- No generes imágenes, vistas previas ni GIF, y no ejecutes el script: lo ejecuto yo.
- Si necesitas un acabado nuevo (placas segmentadas, por ejemplo), escribe una función `m_…` y regístrala en `MATERIALS`.
