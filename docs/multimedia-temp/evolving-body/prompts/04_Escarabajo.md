# 4. Forma Escarabajo (Aguijón + Caparazón)

---

Te adjunto `generar_cuerpo_evolutivo.py`, el generador de pixel art del cuerpo evolutivo de mi juego *Primordia*, un roguelike con vista cenital en 3/4 que transcurre bajo el agua. El jugador es una célula verde que evoluciona: su cuerpo toma la forma de la mezcla de las 2 temáticas de sus mutaciones dominantes, en 3 versiones (1 sutil, 2 marcada, 3 plena). Todo está definido como datos: `THEMES`, `MATERIALS`, `FORMS`, `FORM_BY_THEMES`, `MUTATIONS`, `TRAITS` y `COMBOS`. Las mutaciones sin rasgo (`rasgo=None`) ya se toleran.

Necesito que **añadas al script** lo siguiente.

**Temática nueva `tortuga`** (Caparazón, linaje Acorazado): rampas de 4 tonos de concha, en gris azulado con reborde claro, como el Caparazón rehecho de `generar_evoluciones.py`, más una rampa de verde oliva y pardo para la piel.

**Mutación nueva en `MUTATIONS`:** `Carapace` (Caparazón, ranura Piel, temática `tortuga`, linaje Acorazado), **todavía sin rasgo** (`rasgo=None`).

**Forma nueva `Beetle`** (Escarabajo), para `('insecto', 'tortuga')`, con `compartida=2`:
- **Versión 1 (sutil):** la avispa larvaria sutil con el lomo más abombado y duro: una línea central marca dos futuros élitros.
- **Versión 2 (marcada):** un cuerpo ovalado y abombado. Dos élitros rígidos en gris azulado con reborde claro y la línea central, pronoto ancho, cabeza pequeña con antenas cortas en maza y el verde asomando en las juntas.
- **Versión 3 (plena):** un escarabajo blindado y macizo: élitros gruesos con relieve de escudos poligonales (que recuerden a la concha de tortuga), pronoto con un pequeño cuerno, patas cortas y fuertes asomando bajo el borde, y un brillo frío en la concha.

Conserva los ojos claros del jugador y un poco de su verde. Más adelante, el rasgo del Caparazón pondrá un **caparazón pequeño en el anclaje `back`**: déjalo en el centro del lomo, sobre los élitros. Para que no se vea doble, que los élitros de la forma sean más planos y bajos que ese caparazón.

**Vista previa:** sustituye `COMBOS` por estas filas: "AGUIJON I + CAPARAZON I", "AGUIJON I + CAPARAZON III", "AGUIJON III + CAPARAZON I" y "AGUIJON III + CAPARAZON III".

Reglas:
- Respeta el estilo, el contorno `#181425`, la luz arriba a la izquierda y las proporciones y el lienzo de 32×32 de las formas que ya existen.
- **No me devuelvas el script entero.** Dame solo los bloques nuevos o cambiados, cada uno con una línea que diga dónde va.
- No generes imágenes, vistas previas ni GIF, y no ejecutes el script: lo ejecuto yo.
- Si necesitas un acabado nuevo (escudos poligonales), escribe una función `m_…` y regístrala en `MATERIALS`.
