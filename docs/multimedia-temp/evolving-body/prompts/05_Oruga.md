# 5. Forma Oruga urticante (Aguijón + Piel urticante)

---

Te adjunto `generar_cuerpo_evolutivo.py`, el generador de pixel art del cuerpo evolutivo de mi juego *Primordia*, un roguelike con vista cenital en 3/4 que transcurre bajo el agua. El jugador es una célula verde que evoluciona: su cuerpo toma la forma de la mezcla de las 2 temáticas de sus mutaciones dominantes, en 3 versiones (1 sutil, 2 marcada, 3 plena). Todo está definido como datos: `THEMES`, `MATERIALS`, `FORMS`, `FORM_BY_THEMES`, `MUTATIONS`, `TRAITS` y `COMBOS`. Las mutaciones sin rasgo (`rasgo=None`) ya se toleran.

Necesito que **añadas al script** lo siguiente.

**Temática nueva `erizo`** (Piel urticante, linaje Tóxico): rampas de 4 tonos en lima ácido y morado (los colores Tóxicos de la Piel urticante de `generar_mutaciones.py`), más una de verde oscuro para la piel.

**Mutación nueva en `MUTATIONS`:** `StingingSkin` (Piel urticante, ranura Piel, temática `erizo`, linaje Tóxico), **todavía sin rasgo** (`rasgo=None`).

**Forma nueva `StingingCaterpillar`** (Oruga urticante), para `('insecto', 'erizo')`, con `compartida=2`:
- **Versión 1 (sutil):** la avispa larvaria sutil, algo más rechoncha y segmentada en anillos, con 3 o 4 mechoncitos de pelos lima en el lomo.
- **Versión 2 (marcada):** una oruga regordeta de 4 o 5 anillos con colores de advertencia (bandas lima y morado sobre verde oscuro), cabeza redonda con ojitos y un mechón de pelos urticantes en cada anillo, que se erizan en `Move`.
- **Versión 3 (plena):** una oruga urticante temible: anillos gruesos, penachos de pelos largos lima y morado que se mecen, manchas de advertencia muy marcadas y la cabeza encapuchada. Las pseudopatas se ondulan en `Move`, en una ola que recorre los anillos.

Conserva los ojos claros del jugador y un poco de su verde. Más adelante, el rasgo de la Piel urticante pondrá **púas repartidas por el cuerpo**, así que los pelos de la forma tienen que ser mechones suaves, no púas rígidas, para que no se confundan.

**Vista previa:** sustituye `COMBOS` por estas filas: "AGUIJON I + PIEL I", "AGUIJON I + PIEL III", "AGUIJON III + PIEL I" y "AGUIJON III + PIEL III".

Reglas:
- Respeta el estilo, el contorno `#181425`, la luz arriba a la izquierda y las proporciones y el lienzo de 32×32 de las formas que ya existen.
- **No me devuelvas el script entero.** Dame solo los bloques nuevos o cambiados, cada uno con una línea que diga dónde va.
- No generes imágenes, vistas previas ni GIF, y no ejecutes el script: lo ejecuto yo.
- Si necesitas un acabado nuevo (pelos, anillos), escribe una función `m_…` y regístrala en `MATERIALS`.
