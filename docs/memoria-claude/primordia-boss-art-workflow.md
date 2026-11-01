---
name: primordia-boss-art-workflow
description: "Cómo quiere el usuario el arte de los jefes (desde el piso 3): un jefe cada vez, preguntas antes de cada subtanda, muestra en 1 dirección, escenas de vista previa, comparar versiones si duda"
metadata:
  node_type: memory
  type: feedback
  originSessionId: cf8e577c-459a-4153-bfc2-e53b953b2e8a
  modified: 2026-11-01T19:37:17.185Z
---

Proceso que el usuario aprobó con el Arthropleura y la Meganeura (2026-10-31 / 11-01):

- **Un jefe cada vez**, con Opus. Antes de dibujar, recuperar el diseño del jefe (escenario, fases, flujo) y **hacer todas las preguntas necesarias** con una propuesta en cada una (vista, tamaño, sala, daño por fase, presentación, muerte, barra). Repetir una ronda corta de preguntas **antes de cada subtanda** (6d, 6e, 6f, 6g…).
- Dividir en subtandas: escala → cuerpo (primero **muestra en una dirección**, luego 16 direcciones) → estados y momentos → efectos y crías → ambiente/escenario → barra de vida.
- **Escenas de vista previa en cada subtanda** (comparar el jefe con la rana, ataques, presentación, muerte…), como PNG con cuadros clave + GIF; **los GIF de escenas montadas van en `gif/escenas/`**.
- Si el usuario **duda entre dos diseños**, generar las dos versiones (y escenas con cada una) para que elija.
- Revisar yo mismo cada lámina antes de enseñarla (orden de dibujo, cosas tapadas, efectos apagados por capas) y corregir antes de presentarla.
- Al cerrar el jefe: README de la carpeta, fila en docs/MULTIMEDIA.md, commit en la rama de arte (sin `ProjectSettings.asset`).

**Why:** el usuario quiere decidir cada detalle viendo resultados concretos; le gustaron mucho las escenas.
**How to apply:** seguirlo con la Megarachne, el Lepidodendron ancestral y los jefes de los pisos 4-6. Ver [[primordia-boss-meganeura]], [[primordia-sprites-skill]].
