---
name: primordia-floor3-capsule-redesign
description: "Rediseño de arte de la Cápsula de ámbar (piso 3), 2026-11-06: ARTE CERRADO (forma D, formación, reposo, rotura, curación y halo); falta importar y programar (Sonnet)"
metadata:
  type: project
---

**Estado (2026-11-06, rama feature/floor-3, sesión con Sonnet; todo el arte en `docs/multimedia-temp/floor3-capsule/`, ver su README: tabla de hojas, generadores y pendientes).**

La usuaria no estaba contenta con el arte de las especiales del ámbar (ver [[primordia-floor3-specials-look]]) y empezó por la **Cápsula de ámbar** (la Gota y los Cercos siguen por rediseñar). Decisiones aprobadas:
- **Forma D**: gota alargada hacia arriba, punta roma, pinchos laterales que suben desde el suelo, textura de la foto de ámbar (borde rojizo, centro amarillo, vetas, grietas), con holgura sobre el cuerpo (46×44). Dos capas Back/Front. **Nivel III = la misma cápsula ~20 % más ancha y con más grietas** (se descartaron 3 propuestas de «nivel III distinto»); lo que cambia es la animación.
- **Formación**: sube de abajo arriba + destello y halo dorado; **el tiempo se detiene mientras se forma**; nivel III: vetas de ámbar por el suelo.
- **Rotura**: grietas en todas direcciones → 1 cuadro de quiebre en trozos → estallido. Nivel III: las vetas del suelo se disuelven.
- **Curación** (2 s): pulso dorado por medio corazón curado (+1 final) y un corazón dorado por pulso que sube; tinte dorado de la criatura por shader.
- **Halo con alas levitando sobre la cabeza** = aspecto ANTES de activarse (sustituye a la costra del lomo `Special_AmberCapsule_*`); 1 gema (I-II), 2 (III); se apaga (tono oscuro, alas caídas) al gastarse y se repone al revés.

**Why:** la cápsula anterior (octogonal 48×56) «lucía demasiado grande», forma y efectos de aparición/rotura no le gustaban.

**How to apply / SIGUIENTE (en orden):**
1. **ARTE CERRADO (2026-11-06, Opus):** reposo HECHO y aprobado (`generar_reposo.py` → `reposo/`): va DEBAJO de la curación durante los 2 s dentro; sutil (destello en cuadros 0-4 + 3 burbujas, sin latido propio porque lo pone la curación), bucle corto 8 × 120 ms elegido por la usuaria, Back fija; nivel III: onda de luz por las vetas del suelo (`Fx_AmberFloorVeins_Idle`). Retoques opcionales que la usuaria puede pedir: corazones, gemas del halo (5 px, poco visibles), grietas de los cuadros 1-3 de la rotura, trozos planos.
2. Luego (Sonnet): importar con menú `Primordia > Floor 3 > …` (nuevo `ArtSetup`), sustituir `Fx_AmberShell*` en `AmberShell`/`AmberCapsuleShield`, programar el tiempo detenido en la formación, curación por pulsos, halo en vez de capa del lomo, tinte de curación. **Nada de esto está importado ni programado.** Actualizar `docs/MULTIMEDIA.md`.
3. Después la **Gota de ámbar** y los **Cercos** (rediseño; preguntar qué no gusta y la idea de aspecto, igual que se hizo con la Cápsula), y el contorno dorado/motas común (subtanda 3).

**IMPORTADA Y PROGRAMADA (2026-11-06, Sonnet; PROBAR EN JUEGO, sin commit):** menú `Primordia > Floor 3 > Crear mutaciones especiales del ámbar` (importa a `Art/Effects/Floor3Amber/Capsule` y `/Drop`, borra el arte viejo y las capas `Special_AmberCapsule`/`Special_AmberGland_Rest|Full`). Código: `AmberShell` reescrito (formación/reposo/rotura con las hojas dibujadas, **sin escalar**, cada hoja con su pivote; anclaje Center = centro del cuerpo en la Cápsula, Feet = pies en la Gota), `TimeWarp.Freeze` (tiempo detenido durante la formación; `GameManager.ApplyTimeScale` lo respeta; el bloque corre con tiempo sin escala), `AmberHalo` (halo con alas sobre la cabeza, estado por activaciones que quedan; altura por cuerpo: rana 16 px, Diplocaulus 24, vinagrillo 18 sobre el centro), `AmberCapsuleShield` (curación **por pulsos** en los cuadros de `AmberHealArt.pulseFrames`; tinte dorado multiplicando el color del cuerpo con la curva `tint`, no shader; invulnerable = reposo + rotura hasta el quiebre + 2 s; se libera en el cuadro 4 de la rotura). Pruebas: `AmberSpecialsTests` 10/10. Ya no se cura nada al encerrarte: el primer medio corazón llega con el primer pulso (~0,5 s dentro).
**Falta para cerrar:** probar en `Test_Floor3Specials` (teclas 4 = medio corazón, 2 = Cápsula), ajustar altura del halo y colocación en los 3 cuerpos, sonido (paso 8), `docs/MULTIMEDIA.md`.

**Entorno:** este PC no tenía Python 3: se instaló Python 3.12 con winget (`--scope user`) y Pillow. Los generadores se importan entre sí (`generar_cristalizacion` → `generar_capsula_d`, `generar_capsula_nivel3`; `generar_rotura`/`curacion` → ambos). Los GIF de revisión no van a git: `python generar_<x>.py` los regenera. Había 2 archivos de Unity modificados sin relación (URP settings, learn.iet) que NO se commitearon.

**2026-11-06:** los **Cercos de cucaracha NO se rediseñan**: a la usuaria le gusta el diseño actual. Tras la Cápsula y la Gota, el arte de las especiales del ámbar está cerrado (queda la subtanda 3, contorno dorado y motas, si la usuaria la quiere). La capa de atrás va en `orden - 1` para verse sobre el lodo.
