---
name: primordia-floor-docs
description: "Norma del usuario (2026-10-25): cada piso tiene siempre su documento interactivo y se le añaden los cambios relevantes al terminarlos"
metadata:
  type: feedback
---

Desde el 2026-10-25 el usuario quiere que **cada piso tenga siempre su documento interactivo** (Artifact) y que se le añadan los cambios relevantes cuando se terminan. Ejemplo que dio: las mutaciones especiales de las almejas se añaden al documento del piso 2 **cuando terminemos de crearlas** (arte aprobado), no antes.

Documentos actuales: piso 1 https://claude.ai/artifact/1XpHC3PBSzqw1FGXMbScsL (fuente docs/multimedia-temp/documento-linajes/) y piso 2 https://claude.ai/artifact/1Qdqi5ymyBEhETWY8MKmnm (fuente docs/multimedia-temp/documento-piso2/, template.html + build.py). Piso 3 (2026-11-02): https://claude.ai/artifact/9XEf1K1ki9Tm5WaL6BCX27 (fuente docs/multimedia-temp/documento-piso3/: template.html + build.py; GIF de jefes recortadas con CSS; 9,7 MB; sin audio, II/III de las especiales del ámbar y planos de las 10 salas por cerrar). Al actualizar, republicar en la misma url.

**Why:** el usuario usa esos documentos como referencia viva del diseño de cada piso.
**How to apply:** al cerrar un tema con arte aprobado (mutaciones, salas, enemigos…), proponer o hacer la actualización del documento de su piso; al empezar un piso nuevo, crear su documento. Ver [[primordia-floor2-rooms]], [[primordia-workflow]].

**Lección técnica (2026-11-02):** los documentos 1-3 tienen botón de animaciones (CSS `:root[data-motion="off"]`, JS al final de la plantilla); las animaciones ya no dependen solo de `prefers-reduced-motion`. En el documento del piso 3 las GIF de los jefes se recortan con CSS (caja `_gc`), no recodificándolas (engorda); grids con `minmax(0,1fr)` para que no desborden. Para republicar un documento hay que haberlo leído antes (la primera publicación se rechaza y guarda la versión viva en tool-results; compararla con la local).

**2026-11-03:** documento del piso 3 actualizado (versión 5, misma url) con las mutaciones del paso 4 rehechas (Patas de salto y hilanderas con salto cargado y red, Piel anfibia defensiva, Escamas ofensivas, Saco de metano y Feromonas con los valores finales) y los GIF nuevos del salto de los 3 cuerpos, el marcador y la red (`build.py` carga `floor3-jump`; 10 MB). Al republicar: leer antes la versión viva (la primera publicación se rechaza), comparar con un diff que sustituya las líneas largas por su hash y publicar de nuevo.
