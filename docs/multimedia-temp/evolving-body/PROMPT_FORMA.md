# Plantilla: encargar una forma o un rasgo nuevo (formato corto)

Cómo usarla:
1. Abre una **conversación nueva** con Claude.
2. Adjunta **solo** `generar_cuerpo_evolutivo.py`. No adjuntes los otros generadores, imágenes, vistas previas ni GIF.
3. Copia el texto de abajo y rellena lo que va entre `<...>`. Borra el bloque de forma o el de rasgo si no lo necesitas.
4. Pásale la respuesta a Claude Code: él pega el código en el script, lo ejecuta, revisa la vista previa e importa el arte con el menú.

---

Te adjunto `generar_cuerpo_evolutivo.py`, el generador de pixel art del cuerpo evolutivo de mi juego *Primordia*. Ya genera las formas `WaspLarva` y `Firefly` y los rasgos `Stinger` y `ShockGland`. Todo está definido como datos: `THEMES`, `MATERIALS`, `FORMS`, `FORM_BY_THEMES`, `MUTATIONS` y `TRAITS`.

Necesito que **añadas al script**:

**Forma nueva: `<NombreEnInglés>`** (`<nombre en español>`), para las temáticas `<temática 1>` + `<temática 2 o None>`.
- **Versión 1 (sutil):** <cómo se ve>
- **Versión 2 (marcada):** <cómo se ve>
- **Versión 3 (plena):** <cómo se ve>
- Ponle `compartida=2`, el formato reducido: esquiva y muerte una sola vez y sin `Hurt`.

**Rasgo nuevo: `<NombreEnInglés>`** de la mutación `<mutación>` (`<ranura>`), en el anclaje `<head|back|abdomen|organ|left|right|mouth>`, con la acción `<Attack|Activate|ninguna>`.
- **Nivel 1:** <cómo se ve>
- **Nivel 2:** <cómo se ve>
- **Nivel 3:** <cómo se ve>

**Temática nueva** (solo si no existe): `<nombre>`, con estas rampas de 4 tonos: <colores o "elígelos tú">.

Reglas:
- Respeta el estilo, el contorno `#181425`, la luz arriba a la izquierda y las proporciones de las formas que ya existen.
- **No me devuelvas el script entero.** Dame solo los bloques nuevos o cambiados, cada uno con una línea que diga dónde va. Por ejemplo: "añadir al final de `FORMS`" o "nueva función, antes de `TRAITS`".
- No generes imágenes, vistas previas ni GIF, y no ejecutes el script: lo ejecuto yo.
- Si una forma necesita un acabado nuevo (escamas, pelo…), escribe una función `m_…` y regístrala en `MATERIALS`.
