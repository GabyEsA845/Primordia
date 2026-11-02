---
name: primordia-floor3-step3-art-pending
description: "Arte y animaciones pendientes del paso 3 del piso 3 (Bocas) para una sola sesión con Opus, más el rediseño de la barra de recarga de los órganos"
metadata:
  node_type: memory
  type: project
  originSessionId: 970aacfb-d6f0-4f9c-9727-b81377a610b2
  modified: 2026-11-02T22:19:31.381Z
---

Decisión de la usuaria (2026-11-02): hacer las 3 Bocas del paso 3 con la lógica y luego corregir TODAS las animaciones de golpe en una sesión con Opus (skill primordia-sprites, preguntar antes de dibujar).

**Pendiente de arte/animación (Opus):**
- **Escupitajo venenoso (3b, rana):** la bola parece levitar en lugar de rodar → bola viscosa que rueda y se estira, con sombra de contacto y cola pegajosa; el rastro son charcos sueltos → banda de baba continua pegada al suelo (referencia: E de Kog'Maw, Void Ooze), cada tramo se seca a los 3 s (4 s en II) desde que se pinta; NO debe encoger → mantiene tamaño y en los últimos 0,6 s se hincha y parpadea antes de estallar. Código a rehacer tras el arte: banda continua con colisión, quitar el encogimiento, hinchado final, sombra de la bola.
- **Peonza (3c, Diplocaulus), nivel III:** el bumerán lanza ahora los fotogramas de Spin tintados (provisional); dibujar un **clon translúcido** del personaje.
- **Siluetas de la metamorfosis (Morph/Reveal)** de rana, Diplocaulus y vinagrillo (la elección de Boca de la sala de paso las necesita).
- **Barra de recarga de los órganos:** se mueve de debajo del jugador (CooldownBar, UI/CooldownBar.cs) a la parte **inferior izquierda de la pantalla** (HUD); rediseñar su arte y recarga. Motivo: debajo del jugador va ahora la barra de carga del Chorro de vinagre (3d). RECORDÁRSELO a la usuaria.
- **Chorro de vinagre (3d, vinagrillo):** dibujar las puntas del chorro (inicio fino que nace de la cola y punta afinada; ahora se suaviza por código con tamaño y transparencia) y una **animación de salpicadura más detallada** (nivel III, radio ya subido a 2 u; el dibujo actual es de 1 u y se escala). Alcances actuales 5 / 6 / 7 u.
- Más lo que salga de probar el resto.

**Why:** la usuaria prefiere agrupar el arte para ahorrar tokens (una cuenta Pro) y ver las 3 Bocas funcionando antes de pulirlas.
**How to apply:** al cambiar a Opus recordar este listado y [[primordia-door-design-pending]]; avisar del cambio de modelo ([[primordia-token-saving]]).
