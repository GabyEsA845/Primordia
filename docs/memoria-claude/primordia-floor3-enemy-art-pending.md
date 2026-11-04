---
name: primordia-floor3-enemy-art-pending
description: "Arte de enemigos del piso 3 que hay que mejorar más adelante (Opus): cono del escarabajo bombardero"
metadata:
  type: project
---

**Pendiente de arte (Opus, preguntar al usuario antes de dibujar):**
- **Cono de fuego del escarabajo** (`Fx_BeetleSpray`, `floor3-enemies/Effects`, generador `generar_enemigos_piso3.py`, función `beetle_spray`): se ve como un triángulo naranja liso, no como fuego. Rehacerlo con llamas (lenguas, núcleo claro, chispas) manteniendo la celda 56×28, 6 cuadros, nace en el borde izquierdo a media altura. Detectado el 2026-11-03 al probar el escarabajo; el código no cambia (pivote (0; 0,5), 0,06 s por cuadro, menú «Crear enemigos del piso 3» lo reimporta).

**Why:** el usuario lo vio en juego y no luce; el resto del escarabajo le gusta.
**How to apply:** al llegar a una tanda de arte del piso 3, incluir esto; al acabar, volver a ejecutar Primordia > Floor 3 > Importar todo el arte del piso 3 y Crear enemigos del piso 3. Ver [[primordia-floor3-design]] y [[primordia-provisional-art]].
