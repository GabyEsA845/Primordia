---
name: primordia-mutation-visual-tiers
description: "Regla de arte de mutaciones por niveles desde el piso 3 (normales sin cambiar silueta, pieles retocan el cuerpo, especiales lo cambian, linajes transforman)"
metadata:
  node_type: memory
  type: feedback
  originSessionId: bc0e0fd0-88a1-4fab-9e3b-209644969f15
  modified: 2026-11-01T02:30:13.539Z
---

Desde el piso 3 (decidido 2026-10-31), el arte de las mutaciones va **por niveles** para no "ensuciar" el sprite del jugador:

1. **Mutaciones normales (Extremidades y Órganos): nunca cambian la silueta.** Se ven con **efectos al usarlas** (p. ej. Patas de salto y Patas hilanderas = efectos en la esquiva: despegue, aterrizaje, hilo). Sin capas Limbs encima del cuerpo.
2. **Pieles: sí cambian ligeramente el modelo**, pero **solo dentro de la silueta** (color, brillo, textura, grietas, gotas), sin piezas que sobresalgan.
3. **Especiales del ámbar: cambian bastante el modelo** (capa visible, diseñada a medida de cada cuerpo).
4. **Linajes: la transformación grande** (animación de evolución).

Los pisos 1 y 2 **no se tocan** (sus capas se quedan); si desentona al jugar seguido, se revisa en `feature/balance`.

**Why:** con cuerpos de 32 px con volumen, las piezas añadidas (patas de araña sobre la rana) cambian la silueta, desentonan con el diseño y hacen raras las animaciones. El usuario lo detectó al ver la 4a.
**How to apply:** en toda mutación nueva, preguntarse en qué nivel está antes de dibujar; las normales se diseñan como efectos de activación. Ver [[primordia-floor3-design]], [[primordia-sprites-skill]].
