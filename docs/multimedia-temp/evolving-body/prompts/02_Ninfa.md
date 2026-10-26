# 2. Forma Ninfa de libélula (Aguijón + Aletas)

---

Te adjunto `generar_cuerpo_evolutivo.py`, el generador de pixel art del cuerpo evolutivo de mi juego *Primordia*, un roguelike con vista cenital en 3/4 que transcurre bajo el agua. El jugador es una célula verde que evoluciona: su cuerpo toma la forma de la mezcla de las 2 temáticas de sus mutaciones dominantes, en 3 versiones (1 sutil, 2 marcada, 3 plena). Todo está definido como datos: `THEMES`, `MATERIALS`, `FORMS`, `FORM_BY_THEMES`, `MUTATIONS`, `TRAITS` y `COMBOS`. Las mutaciones sin rasgo (`rasgo=None`) ya se toleran.

Necesito que **añadas al script** lo siguiente.

**Temática nueva `marino`** (Aletas, linaje Depredador): rampas de 4 tonos en turquesas y verdes de agua, con un acento naranja cálido como el de las Aletas actuales (`generar_mutaciones.py`).

**Mutación nueva en `MUTATIONS`:** `Fins` (Aletas, ranura Extremidades, temática `marino`, linaje Depredador), **todavía sin rasgo** (`rasgo=None`).

**Forma nueva `DragonflyNymph`** (Ninfa de libélula), para `('insecto', 'marino')`, con `compartida=2`:
- **Versión 1 (sutil):** la avispa larvaria sutil, algo más alargada y aplanada, con tonos de agua sobre el verde y unas branquias diminutas que asoman detrás.
- **Versión 2 (marcada):** una larva acuática alargada. Cabeza ancha con ojos compuestos grandes a los lados, tórax con esbozos de estuches alares y un abdomen segmentado que acaba en tres branquias en forma de hoja, que ondean en `Idle` y en `Move`.
- **Versión 3 (plena):** una ninfa de libélula depredadora: cabeza grande con ojos enormes, máscara (labio) plegada bajo la cabeza, estuches alares marcados y abdomen segmentado con franjas turquesa y ámbar, rematado por branquias largas. Tiene aire de cazador acuático.

Conserva los ojos claros del jugador y un poco de su verde. **No dibujes aletas en el cuerpo**: las pondrá más adelante el rasgo de las Aletas en `left` y `right`. Ojo: aquí el rasgo del Aguijón va en el anclaje `abdomen`, así que ponlo donde acaba el abdomen, por encima de las branquias, y que no queden tapadas.

**Vista previa:** sustituye `COMBOS` por estas filas: "AGUIJON I + ALETAS I", "AGUIJON I + ALETAS III", "AGUIJON III + ALETAS I" y "AGUIJON III + ALETAS III".

Reglas:
- Respeta el estilo, el contorno `#181425`, la luz arriba a la izquierda y las proporciones y el lienzo de 32×32 de las formas que ya existen.
- **No me devuelvas el script entero.** Dame solo los bloques nuevos o cambiados, cada uno con una línea que diga dónde va.
- No generes imágenes, vistas previas ni GIF, y no ejecutes el script: lo ejecuto yo.
- Si necesitas un acabado nuevo, escribe una función `m_…` y regístrala en `MATERIALS`.
