---
name: primordia-floor3-thief-roach
description: "Cucarachas ladronas del piso 3 (almacén de corazones del bloque 3 y sala especial Ladronas): decisiones de arte de la usuaria (2026-11-04)"
metadata:
  type: project
---

**Decisiones de la usuaria (2026-11-04, Opus):** cucaracha **carbonífera parda** (tipo Archimylacris, ~20-24 px, plana y ovalada, pronoto que tapa la cabeza, alas con venas, antenas largas, cercos; pardo rojizo con borde ámbar). **Vista desde arriba, girada por código** hacia donde corre (una hoja por animación). El botín (medio corazón o biomasa) **va a cuestas sobre el lomo** (capa del código). **Las que roban corazones NO se pueden matar** (escena del almacén: si sales sin enviar el corazón a la vaina, una lo roba); **las de la sala Ladronas SÍ, y sueltan la biomasa**. Animaciones: correr, reposo, **salir de una grieta, coger el botín, meterse en la grieta**, daño y muerte (las mismas hojas para las dos; el código hace invulnerable a la del corazón).

Diseño del GDD: almacén de corazones (vaina de la tienda, 4 medios en el piso 3), sala lateral «Ladronas» (bloque 4). Ver [[primordia-floor3-design]].

**DIBUJADA Y APROBADA (2026-11-04, Opus):** `docs/multimedia-temp/floor3-roach/generar_cucaracha.py` (README y MULTIMEDIA.md escritos). Celda 40×28 mirando a la derecha (`Idle` 4, `Run` 4, `Grab` 5 —cuadro 3: botín en `LOMO` (16,14)—, `Hurt` 2, `Death` 6); `Emerge`/`Hide` 6 en 50×28 sin girar (grieta en (10,14) / (39,14); Hide cuadro 3 quita el botín); `Deco_RoachCrack` 20×12. Trampa: `ramp()` devuelve textos; para asignar directo a `cv.px` convertir con `C()`.
