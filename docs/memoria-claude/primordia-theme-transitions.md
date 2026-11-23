---
name: primordia-theme-transitions
description: Cómo se hacen las transiciones entre temas de escenario (franjas, salas de paso entre pisos): nunca una línea de corte; borde irregular + máscaras de mezcla en los dos sentidos + paredes de frontera + calcomanías
metadata:
  type: feedback
---

**Regla de la usuaria (2026-11-23, sala de paso del piso 3 → 4):** un cambio de tema de escenario (franjas de una sala de paso, tramos de un piso, salas con tema) **nunca puede ser una línea de corte** entre celdas; «luce muy poco natural». Quiere una transición sutil en suelo **y** paredes, aunque haga falta más arte. Con el sistema de abajo dijo «ahora luce mucho mejor».

**Why:** el código elige el tema celda a celda, así que sin piezas de mezcla la frontera sale recta y se ve como dos imágenes pegadas (lo mismo que rechaza en los jefes, [[primordia-boss-art-workflow]]).

**How to apply (receta, ya hecha en `docs/multimedia-temp/floor3-exit/generar_salida_piso3.py`, funciones `blend_sets`, `border_face`, `sand_decal`, `sample_room`):**
1. **Borde irregular (código):** la frontera entre franjas la decide el código con ruido por fila/columna (entra y sale como una costa), no una columna fija. Las caras usan el tema de su fila de arriba para que el par Upper/Lower no se parta.
2. **Mezcla del suelo por máscaras, como el lodo:** capa encima del suelo en la celda «de menos rango» con la máscara de vecinos de la otra franja (N=1, E=2, S=4, O=8; 15 piezas) + 4 esquinas (`Corner_NE/NO/SE/SO`, solo si los dos lados de esa esquina no están ya en la máscara). Borde ondulado con periodo 16 (así casa entre tiles vecinos) y en trama. **En los dos sentidos:** lo nuevo entra en lo viejo (arena) y lo viejo deja restos en lo nuevo (ceniza/carbón); entre franjas parecidas basta una capa de motas/granos con densidad que baja hacia dentro.
3. **Paredes:** capa de tapa por máscaras (la tapa del tema nuevo avanza sobre la vieja con su filo con luz) y **caras de frontera** en pares (viejo→nuevo y nuevo→viejo, 2 variantes) con juntura que ondula y algo del tema viejo medio enterrado; se ponen en la celda del tema viejo.
4. **Calcomanías grandes** (varias celdas, sueltas cerca de la frontera) para romper la cuadrícula.
5. **Franja intermedia** con las paredes del tema viejo y el suelo ya invadido (velo de granos sueltos, no manchas con forma: las manchas periódicas forman cuadrícula).
6. **Revisar con una sala de muestra** de las franjas seguidas y un recorte ampliado de la frontera antes de enseñarlo.

Relacionado: [[primordia-art-session-2026-11-23]], [[primordia-floor2-transition-room]], [[primordia-sprites-skill]].
